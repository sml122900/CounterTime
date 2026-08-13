namespace CounterTime.Core
{
    public readonly struct JudgementContext
    {
        public readonly int EffectiveGrip;        // −2..+2 AFTER lock cap (GripSystem supplies)
        public readonly ReadLevel Read;
        public readonly int OpponentStamina;      // 0..100
        public readonly TechniqueClass Tech;
        public readonly bool OpponentDefensivePosture;

        public JudgementContext(int effectiveGrip, ReadLevel read, int opponentStamina,
                                TechniqueClass tech, bool opponentDefensivePosture)
        {
            EffectiveGrip = effectiveGrip;
            Read = read;
            OpponentStamina = opponentStamina;
            Tech = tech;
            OpponentDefensivePosture = opponentDefensivePosture;
        }
    }

    public readonly struct JudgementResult
    {
        public readonly Judgement Judgement;
        public readonly FighterId ScoredBy;       // attacker, or opponent when countered
        public readonly bool IsCounter;
        public readonly int Sum;                  // expose for logs/tests

        public JudgementResult(Judgement judgement, FighterId scoredBy, bool isCounter, int sum)
        {
            Judgement = judgement;
            ScoredBy = scoredBy;
            IsCounter = isCounter;
            Sum = sum;
        }
    }

    public static class JudgementCalculator
    {
        public static JudgementResult Evaluate(FighterId attacker, in JudgementContext c,
                                               MatchRulesConfig cfg)
        {
            int stam = c.OpponentStamina < cfg.StaminaBonusExhaustedThreshold ? 3
                     : c.OpponentStamina < cfg.StaminaBonusTiredThreshold     ? 2 : 0;
            int sum = c.EffectiveGrip + (int)c.Read + stam + (int)c.Tech
                    + (c.OpponentDefensivePosture ? cfg.DefensivePosturePenalty : 0);
            if (sum <= cfg.CounterThreshold)
                return new JudgementResult(Bracket(-sum, cfg), Opp(attacker), true, sum);
            return new JudgementResult(Bracket(sum, cfg), attacker, false, sum);
        }

        /// §4.2 counter-ready: the ATTACKER's read drops `steps` rungs, floored at
        /// GotRead. The ladder is uneven BY DESIGN — Neutral→GotRead is a 3-point
        /// fall, Read→Neutral is 2, FullRead→Read is only 1. A fully-read attack
        /// cannot be countered by CounterReady alone. Reading is beaten only by reading.
        public static ReadLevel StepDownRead(ReadLevel r, int steps = 1)
        {
            for (int i = 0; i < steps; i++)
                r = r switch
                {
                    ReadLevel.FullRead => ReadLevel.Read,
                    ReadLevel.Read     => ReadLevel.Neutral,
                    _                  => ReadLevel.GotRead,   // Neutral and GotRead floor
                };
            return r;
        }

        /// §E/§H step 4 — upward-only promotion roll layered on the deterministic §4
        /// floor. §E.2 (confirmed): grip resolution, Balance changes, and the floor
        /// itself all stay fully deterministic — probability touches exactly the
        /// promotion roll, referee detection, and habit firing. Nothing about the
        /// floor's own math (this file's `Evaluate`/`Bracket`) changes; this only ever
        /// moves the result UP.
        ///
        /// Live path only. Callers gate this behind `MatchRulesConfig.PromotionGate`
        /// (same "off reproduces v1 대조 모드 exactly" contract as `BalanceGate`) — the
        /// scripted/habit paths and the 42-node fingerprint never call this method.
        ///
        /// `effectiveSum` is the magnitude the floor bracket was assigned from: the raw
        /// §4 sum for a direct score, or |sum| for a counter (§E.1.3 — "applies
        /// symmetrically to counters on |sum|"). A "None" floor may still promote
        /// (§E.1.4, "럭키펀치") — that carve-out is "realAttempt only", which every
        /// caller of this method already satisfies structurally: a poke never reaches
        /// `Evaluate` at all (`MatchEngine.ResolveTechnique` returns before it), so the
        /// only way to have a `JudgementResult` to promote in the first place is a real
        /// (non-poke) attempt.
        ///
        /// `BracketFloor` reads as "the lowest sum that already qualifies for this
        /// tier" — brackets are contiguous, so each tier's floor is exactly one past
        /// the tier below's ceiling, and None's floor (which has no natural lower
        /// bound of its own) is defined the same way one level further down: one past
        /// the counter threshold, the lowest sum that is not itself a counter. This is
        /// a first-cut reading of an ⚙ formula MATCH_RULES only pins down by example at
        /// the Yuko/WazaAri end (§D.4: "full grip advantage + 정독 = floor Yuko") — not
        /// a confirmed mapping, flagged as a tuning candidate like every ⚙ it consumes.
        public static Judgement Promote(Judgement floor, int effectiveSum, MatchRulesConfig cfg, IRandomSource rng)
        {
            Judgement current = floor;
            for (int link = 0; link < cfg.PromoteMaxChain && current != Judgement.Ippon; link++)
            {
                float p = cfg.PromoteBase + cfg.PromoteSlope * (effectiveSum - BracketFloor(current, cfg));
                float chainScale = 1f;
                for (int i = 0; i < link; i++) chainScale *= cfg.PromoteChainFactor;
                p *= chainScale;
                if (p < 0f) p = 0f; else if (p > 1f) p = 1f;

                if (rng.NextFloat() >= p) break;   // chain requires consecutive successes
                current = (Judgement)((int)current + 1);
            }
            return current;
        }

        static int BracketFloor(Judgement j, MatchRulesConfig cfg) => j switch
        {
            Judgement.None    => cfg.CounterThreshold + 1,
            Judgement.Yuko    => cfg.NoPointMax + 1,
            Judgement.WazaAri => cfg.YukoMax + 1,
            Judgement.Ippon   => cfg.WazaAriMax + 1,
            _ => 0
        };

        /// §4 defensive stance — the ONLY place this is derived. The engine fills
        /// JudgementContext.OpponentDefensivePosture from this; nothing else may
        /// re-derive it inline.
        ///
        /// Binary: it asks whether the opponent handed you a throwable posture, not
        /// how deeply they turtled. 극단적 방어 therefore scores the same −2 as a plain
        /// defensive core — depth is already charged by §6.1 and §5.2, and charging it
        /// a third time here would be double-counting. 카운터 준비 suppresses it: a
        /// commitment is not a shell (§4.2).
        public static bool IsDefensivePosture(in TurnChoice c) =>
            c.Core == CoreStance.Defensive && c.Legs != LegAction.CounterReady;

        static Judgement Bracket(int s, MatchRulesConfig cfg) =>
            s <= cfg.NoPointMax ? Judgement.None
          : s <= cfg.YukoMax    ? Judgement.Yuko
          : s <= cfg.WazaAriMax ? Judgement.WazaAri : Judgement.Ippon;

        static FighterId Opp(FighterId who) => who == FighterId.A ? FighterId.B : FighterId.A;
    }
}
