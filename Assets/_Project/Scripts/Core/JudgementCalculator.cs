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

        static Judgement Bracket(int s, MatchRulesConfig cfg) =>
            s <= cfg.NoPointMax ? Judgement.None
          : s <= cfg.YukoMax    ? Judgement.Yuko
          : s <= cfg.WazaAriMax ? Judgement.WazaAri : Judgement.Ippon;

        static FighterId Opp(FighterId who) => who == FighterId.A ? FighterId.B : FighterId.A;
    }
}
