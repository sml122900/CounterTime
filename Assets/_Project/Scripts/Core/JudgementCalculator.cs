using System;

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
        /// Ordered weakest→strongest. The enum values are not evenly spaced, so
        /// "one step" (§4.2) means one position here, not an arithmetic delta.
        static readonly ReadLevel[] ReadLadder =
            { ReadLevel.GotRead, ReadLevel.Neutral, ReadLevel.Read, ReadLevel.FullRead };

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

        /// §4.2 — a fighter who declared CounterReady has their own ReadLevel raised
        /// one step when the opponent commits a big throw. No-op otherwise.
        public static ReadLevel ApplyCounterReady(ReadLevel own, bool opponentCommittedBigThrow,
                                                  MatchRulesConfig cfg)
        {
            if (!opponentCommittedBigThrow) return own;
            int i = Array.IndexOf(ReadLadder, own);
            if (i < 0) return own;
            int raised = i + cfg.CounterReadyReadSteps;
            if (raised > ReadLadder.Length - 1) raised = ReadLadder.Length - 1;
            return ReadLadder[raised];
        }

        static Judgement Bracket(int s, MatchRulesConfig cfg) =>
            s <= cfg.NoPointMax ? Judgement.None
          : s <= cfg.YukoMax    ? Judgement.Yuko
          : s <= cfg.WazaAriMax ? Judgement.WazaAri : Judgement.Ippon;

        static FighterId Opp(FighterId who) => who == FighterId.A ? FighterId.B : FighterId.A;
    }
}
