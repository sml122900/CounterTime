using System.Collections.Generic;
using CounterTime.Core;
using CounterTime.Brain;

namespace CounterTime.Harness.Overnight
{
    /// Per-match data collected by <see cref="MatchSimulator"/> for the 2026-08-13
    /// overnight run (docs/overnight-2026-08-13.md). Plain data bag, aggregated by
    /// OvernightRunner — nothing here feeds back into Core/Brain/Game.
    public sealed class MatchResult
    {
        public string OpponentLabel;
        public int Seed;
        public string Exception;
        public bool HitSafetyCap;

        public FighterId? Winner;
        public int ExchangesResolved;
        public int OsaekomiTicks;
        public int OsaekomiEntries;
        public int ClockRemainingSeconds;
        public string TerminationReason = "unknown";

        public readonly Dictionary<Judgement, int> Judgements = new Dictionary<Judgement, int>();
        public readonly Dictionary<FighterId, int> ScoredBy = new Dictionary<FighterId, int>();
        public int Counters;
        public int ShidoA, ShidoB;
        public int WazaAriA, WazaAriB, YukoA, YukoB;

        public readonly Dictionary<string, int> ATechniqueUsage = new Dictionary<string, int>();
        public readonly Dictionary<string, int> BTechniqueUsage = new Dictionary<string, int>();
        public int BSurpriseFirings;
        /// A surprise technique (변칙기) that fired the exchange right after B declared
        /// 흔들기(Setup) — see MatchSimulator's comment on SetupPrecedingCommitSource for
        /// why this is a real interaction gap worth counting, not a simulator bug.
        public int BSurpriseTelegraphedBySetup;

        // §S balance economy (report §3).
        public int GateOpenBigThrowAttemptsA, GateClosedBigThrowAttemptsA;
        public int GateOpenBigThrowAttemptsB, GateClosedBigThrowAttemptsB;
        /// How many exchanges saw B's OWN EffectiveGrip >= 1 (the exact condition every
        /// grip-gated habit in HabitCatalogue.cs reads). See docs/overnight-2026-08-13.md
        /// §1 — this turned out to matter: several opponents' only habits are grip-gated,
        /// and AutoA's own grip strategy structurally suppresses this condition almost
        /// entirely against B's hardcoded single-hand fallback grab.
        public int BTurnsWithGripEdge;
        public int TurnsBalanceBelowThresholdA, TurnsBalanceBelowThresholdB;
        public int LongestBrokenStreakA, LongestBrokenStreakB;
        public int MinBalanceA = 100, MinBalanceB = 100;
        public int FinalBalanceA, FinalBalanceB;

        // AutoA behavioral characterization (report §1), copied out of AutoAPlayer.
        public int A_TotalDecisions, A_DefendTurns, A_CounterReadyDeclares, A_GripBuildTurns,
                   A_SetupDeclares, A_PokeAttacks, A_BigThrowAttempts, A_HeldBackFromLiveCounterReady,
                   A_BigThrowsWhileGateClosedSanity;
        /// 2026-08-13 재수집 — AutoAPlayer.SuspectedGripDeadlock 그대로. 기대값 0(경기당).
        public bool A_SuspectedGripDeadlock;

        /// 2026-08-13 재수집 — B(습관 브레인) 관점 EffectiveGrip==0(파리티)이었던 교환 수,
        /// 그 중 B 가 기술을 못 낸(공격 안 한) 교환 수. 판단·수정 없이 계측만 —
        /// "문준배·서도현 소극" 원인 분석 자료(§H 3.5b-로스터 세션이 관측한 사실).
        public int BTurnsAtGripParity, BIdleTurnsAtGripParity;

        // ── 2026-08-13 v3 (docs/overnight-2026-08-13-v3.md) 작업1 — 로스터 차별성을
        // A 승률(허수아비 A 품질에 오염됨)이 아니라 B 자신의 행동 분포로 재는 지표.
        // A/B 승률·판정과 완전히 독립 — 계측 시점이 판정 이전(브레인 결정 직후)이다.

        /// 습관 발화 횟수(HabitId 별) — `HabitDecision.Fired` 인 교환만 센다(빗나감·
        /// 폐기·쿨다운 중 스킵은 안 셈). 변칙기는 HabitId 가 없어 여기 안 잡힌다
        /// (BSurpriseFirings 가 그 몫).
        public readonly Dictionary<HabitId, int> BHabitFirings = new Dictionary<HabitId, int>();

        /// B 자신의 턴 하나가 흔들기(Setup 표적 선언)/견제기(Poke)/큰기술(BigThrow)/
        /// 그 외(잡기싸움·뜯기·방어·카운터준비 등 기술 없는 턴) 중 무엇이었는지 —
        /// SetupPrecedingCommitSource 가 실제로 내보내는 턴 기준(습관 결정이 아니라
        /// 제출된 커밋 기준이라, 흔들기로 지연된 큰기술은 그 흔들기 턴에서 Setup 으로
        /// 잡히고 실제 기술이 나가는 턴에서 BigThrow 로 또 잡힌다 — 둘 다 실제
        /// 제출이므로 이중 계상이 아니다).
        public int BSetupDeclareCount, BPokeDeclareCount, BBigThrowDeclareCount, BOtherDeclareCount;

        /// B 가 그 턴에 카운터 준비(LegAction.CounterReady)를 선언한 횟수.
        public int BCounterReadyDeclares;

        /// B 의 손 표적 분포(그립류 표적만 — RightSleeve/LeftSleeve/LeftLapel/
        /// RightLapel/TwistGrip/BackGrab) — "어느 손/소매를 노리는가"의 실측.
        public readonly Dictionary<HandTarget, int> BGripTargetCounts = new Dictionary<HandTarget, int>();

        // ── 2026-08-13 v3 작업3 — 그립 파리티(양쪽 EffectiveGrip==0) 동역학. 판단·
        // 수정 없이 계측만. "파리티가 끝난다"의 정의는 두 경로 중 먼저 오는 쪽이다:
        // (a) 이번 교환에 A 또는 B 가 흔들기/기술을 선언함(그립 수치 자체는 0-0 그대로
        //     일 수 있다 — §4.4 흔들기는 그립을 안 바꾼다), (b) 다음 교환에 들어가기
        // 전 그립이 이미 0-0 이 아니게 바뀌어 있음(직전 교환의 그립 액션이 파리티를
        // 깼다). 굳히기 틱 구간은 계측하지 않는다(서기 전용 개념).
        public int ParityStreakCount;
        public long ParityStreakTotalLength;
        public int ParityStreakEndedByADeclare, ParityStreakEndedByBDeclare, ParityStreakEndedByBothDeclare;
        public int ParityStreakEndedByGripResolved;
        public int ParityStreakEndedByMatchEnd;
        /// 파리티 중 흔들기/기술 선언(위 ADeclare+BDeclare+BothDeclare)이 일어난 총
        /// 횟수 — 아래 두 값의 분모.
        public int ParityBreakDeclareTotal;
        /// 그 선언 직후 창(이 교환 포함 3교환) 안에 실제 판정(Judgement != None)이
        /// 하나라도 났는가 — 흔들기→밸런스드레인→득점 경로가 파리티에서 실제로
        /// 완결되는지의 직접 증거. 창이 겹치면 중복 계상될 수 있다(§3 보고서에 명시).
        public int ParityBreakScoreWithinWindow, ParityBreakScoreMissed;
    }
}
