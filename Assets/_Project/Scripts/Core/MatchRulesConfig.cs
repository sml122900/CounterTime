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
        // §F — mastery level → TechniqueClass boundaries (⚙ proposed initial values).
        // 0..MasteryStandardLevel-1 Sloppy, ..MasterySignatureLevel-1 Standard, rest Signature.
        public int MasteryStandardLevel = 2, MasterySignatureLevel = 4;
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
        // §4.4 흔들기(setup) — declare mechanics (cost, grip gate) are confirmed (v4.0),
        // unaffected by the §H step 3 rename below. Default off — a default-constructed
        // config reproduces v1 대조 모드 exactly.
        public int SetupCost = 1;
        // §4.4/§S — renamed from SetupGateEnabled (§H step 3, was boolean-only: "did the
        // attacker declare 흔들기 last exchange"). Same on/off master switch, same
        // default-off semantics — only the CONDITION it gates changed (see
        // MatchEngine.ResolveTechnique): from a per-attacker declared-flag check to a
        // per-defender Balance-threshold check, generalizing what opens the gate from
        // 흔들기 alone to any of the three Balance-draining threats below.
        public bool BalanceGate = false;

        // §S/§H step 3 — Balance, the third resource (§3 grip, §5 stamina, §S balance).
        // All ⚙ — proposed initial values, not confirmed numbers, same convention as
        // every other tunable in this file. BalanceMax/BalanceOnsetThreshold match the
        // values already named in MATCH_RULES.md/CORE_DESIGN_MatchEngine.md's v4.0
        // revision; the drain/recover magnitudes below are new picks for this step,
        // sized so a single 흔들기 (100−35=65) clears the 70 threshold in one hit and a
        // single 그쳐 (+3, same magnitude as StaminaSystem.MatteRecovery) does NOT
        // undo it alone (65+3=68<70) — two mattes do (65+3+3=71≥70). Pure tuning
        // knobs, not derived from anything — revisit after play.
        public int BalanceMax = 100;
        public int BalanceOnsetThreshold = 70;   // gate: defender's Balance ≥ this → attacker's BigThrow downgrades to Sloppy
        public int BalanceDrainSetup = 35;       // one 흔들기 declare
        public int BalanceDrainFeintFootsweep = 20; // a landed 견제 발기술 (§D.4 floor — v1 has no promotion roll yet, so "landed" = every poke)
        public int BalanceDrainThreatNoScore = 15;  // a genuine BigThrow attempt (realAttempt) that scored None
        public int BalanceRecoverDefense = 10;      // a Clean defense (§5.2 — won the read)
        public int BalanceRecoverMatte = 3;         // 그쳐/굳히기 탈출 부분 회복 — same magnitude as MatteRecovery, same shape
    }
}
