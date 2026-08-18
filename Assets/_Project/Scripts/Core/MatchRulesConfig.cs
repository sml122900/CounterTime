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
        // §2 time costs — scripted/habit path only (42노드 지문이 이 세 값에 묶여
        // 있다, 절대 안 건드림).
        public int GripTurnSec = 5, ExchangeSec = 3, TearSec = 3;
        // 라이브 전용 ⚙ — 사람이 입력을 고르는 데 걸리는 실제 체감 시간을 반영해
        // 교환당 시간을 하나로 통일한다(잡기/기술/뜯기 구분 없음). ExchangeSec 등
        // 위 셋과 완전히 별개 — LiveMatchRunner 만 읽고, ScriptedAssembler/
        // MatchRunner(42노드)는 이 필드를 참조조차 안 한다.
        public int LiveExchangeSec = 8;
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
        // revision.
        //
        // BalanceDrainSetup rederived (§H 3단계 후속, "밸런스 ⚙ 확정값 적용"
        // 세션) from a threat-count economy: 게이트 개방 위협 횟수 = 흔들기 1 /
        // 견제 발기술 2 / 무득점 진짜 시도 3 — 흔들기(40) = 2×견제(20), 하나만
        // 맞아도(100−40=60) 임계(70)를 한 번에 뚫는다. 견제·무득점은 2~3번 쌓여야
        // 같은 폭(≈40~45)에 닿는다 — §4.4가 흔들기를 "밸런스를 깎는 전용 행동"
        // 이라 부르는 것과 일치하는 위계(전용 행동 하나가 부수 효과 여러 번보다
        // 세다). 수비 성공(+10)이 흔들기 1회 드레인을 정확히 상쇄한다(60+10=70,
        // BalanceRecoverDefense 참조) — 그쳐(+3)는 그 절반도 안 돼 패시브 회복이
        // 없다시피 하다(끈적함 의도: 한 번 깎이면 시간이 아니라 실제 방어로만
        // 제대로 풀린다). 나머지 6개(견제·무득점·수비·그쳐·임계·최대)는 이번엔
        // 무수정 — 이 값들 전부는 대회 실플레이 재튜닝 대상이지 확정 수치가 아니다.
        public int BalanceMax = 100;
        public int BalanceOnsetThreshold = 70;   // gate: defender's Balance ≥ this → attacker's BigThrow downgrades to Sloppy
        public int BalanceDrainSetup = 40;       // one 흔들기 declare
        public int BalanceDrainFeintFootsweep = 20; // a landed 견제 발기술 (§D.4 floor — v1 has no promotion roll yet, so "landed" = every poke)
        public int BalanceDrainThreatNoScore = 15;  // a genuine BigThrow attempt (realAttempt) that scored None
        public int BalanceRecoverDefense = 10;      // a Clean defense (§5.2 — won the read)
        public int BalanceRecoverMatte = 3;         // 그쳐/굳히기 탈출 부분 회복 — same magnitude as MatteRecovery, same shape

        // §E/§H step 4 — judgment promotion. Deterministic floor (§4's Bracket) stays
        // exactly as-is; this is the upward-only dice layered on top. Live path only —
        // PromotionGate mirrors BalanceGate's contract: off reproduces v1 대조 모드
        // exactly (scripted/habit paths never call JudgementCalculator.Promote, so the
        // 42-node fingerprint is unaffected by this entire feature existing). All ⚙,
        // taken straight from MATCH_RULES §E.1.3's proposed numbers — not confirmed,
        // real-play tuning candidates like every other ⚙ in this file.
        public bool PromotionGate = false;
        public float PromoteBase = 0.10f;         // base chance per link
        public float PromoteSlope = 0.05f;        // + this much per point of (sum − bracket floor)
        public float PromoteChainFactor = 0.25f;  // link N's chance is scaled by this ^ (N-1)
        public int PromoteMaxChain = 2;           // at most 2 tiers up (e.g. Yuko → WazaAri → Ippon)

        // §2 v4.2 — 잡기 개편 1차. Live path only — GripRevisionGate mirrors
        // BalanceGate/PromotionGate's contract exactly: off reproduces the pre-v4.2
        // grip system byte-for-byte (scripted/habit paths never call
        // MatchEngine.ResolveGripExchange, only the old per-fighter ResolveGrip), so
        // the 42-node fingerprint is unaffected by this entire feature existing. All
        // ⚙ below are taken from the task spec's own proposed numbers where given;
        // the two without a spec number (jitter's home) reuse this file's existing
        // convention (IRandomSource, same as PromotionGate/§6.2's threshold jitter) —
        // not confirmed, real-play tuning candidates like every other ⚙ here.
        public bool GripRevisionGate = false;
        // §2 v4.2 — mutual same-tier clash where BOTH declared Core=Defensive this
        // exchange: neither grab succeeds. After this many CONSECUTIVE such clashes,
        // both fighters get a shido (§2: "반복 ⚙2회 시 양쪽 지도").
        public int GripMutualDefenseShidoRepeats = 2;
        // §2 v4.2 — a Core split (one Aggressive, one Defensive) on a grab clash still
        // lets both succeed normally MOST of the time; this is the chance the
        // Aggressive side alone succeeds (and is immediately solidified, +1 — the
        // spec's "공격측만 성립(+1)" read as an immediate 견고화, §2 v4.2 sole reading
        // that reuses an existing value bonus rather than inventing a new one).
        public float GripSplitStanceSoloChance = 0.10f;
        // §2 v4.2 — a newly-established 교차잡기(cross grip) starts a rolling timer
        // this many exchanges long. Any 흔들기 or technique declaration by its holder
        // resets it; if it expires untouched, that fighter gets an immediate shido.
        public int CrossGripTimerTurns = 2;

        // §2b v4.3 — 뜯기(라이브 전용, GripRevisionGate). Basic=견고 시 결정론적
        // 실패, Forceful/Assisted=견고 시 이 확률로 성공(실패면 비견고화만). B
        // 최소 대응이 잡힌 부위 있을 때 이 확률로 기본 뜯기 시도(항상 Basic —
        // 방법 선택 지능은 3차 이월). 둘 다 ⚙, 작업 지시가 준 제안값 그대로.
        public float TearForcefulSuccessChance = 0.5f;
        public float TearFallbackChance = 0.30f;
    }
}
