namespace CounterTime.Core
{
    /// Every tunable from MATCH_RULES.md. Defaults ARE the spec numbers, so a
    /// default-constructed config reproduces the document exactly.
    public sealed class MatchRulesConfig
    {
        // §1 format
        public int RegulationSeconds = 240;
        public int OsaekomiYukoSec = 5, OsaekomiWazaAriSec = 10, OsaekomiIpponSec = 20;
        public int ShidoLimit = 3;
        // §2 time costs
        public int GripTurnSec = 5, ExchangeSec = 3, TearSec = 3;
        // §4 judgment
        public int StaminaBonusTiredThreshold = 50;     // <50 → +2
        public int StaminaBonusExhaustedThreshold = 25; // <25 → +3
        public int NoPointMax = 1, YukoMax = 4, WazaAriMax = 6; // brackets
        public int CounterThreshold = -3;
        public int DefensivePosturePenalty = -2;
        // §5 stamina
        public int BigThrowCost = 5, PokeCost = 1, CounterCost = 2, TearCost = 1;
        public float[] GripMultiplier = { 2.0f, 1.5f, 1.0f, 0.8f, 0.6f }; // index = grip+2
        public int DefendCleanCost = 2, DefendNeutralCost = 4, DefendPushedCost = 6;
        public int ExtremeDefenseDrainPerTurn = 1;
        public int MatteRecovery = 3; public int StallRecoveryPer10s = 2;
        public float MasteryCostReductionPerLevel = 0.08f;
        public float MasteryReadReductionPerLevel = 0.05f;
        // §6 referee — categories are EXCLUSIVE (highest match only), NEVER summed
        public int PassExtremeDefense = 3, PassDoubleDefensive = 2, PassHandEvade = 2,
                   PassCounterReady = 2, PassAllNeutral = 1,
                   PassAttackEntry = -2, PassRealAttempt = -4;
        public int ThresholdStrict = 9, ThresholdStandard = 12, ThresholdLenient = 16;
        public int ThresholdJitter = 1;            // ±1 at match start
        public int ThresholdEscalation = 2;        // after each shido
        public float DetectStrict = .90f, DetectStandard = .70f, DetectLenient = .45f;
        public int FeintWindowTurns = 2;
        public float TellMinorRatio = .70f, TellMajorRatio = .90f;
        // §2 exchange duration
        public ExchangeDurationRule DurationRule = ExchangeDurationRule.Max;
        // §1 expiry
        public bool ResolveExchangeOnExpiry = true;
        // §4.2 counter-ready
        public int CounterReadyReadSteps = 1;   // capped at FullRead
        // §9 osaekomi
        public int OsaekomiStayCost = 3;          // §9.3 맞물림 — defender pays per stay
        public int OsaekomiEscapeIntervalSec = 5; // §9.3 — one escape exchange per 5s of hold
    }
}
