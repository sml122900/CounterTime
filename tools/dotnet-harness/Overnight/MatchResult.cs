using System.Collections.Generic;
using CounterTime.Core;

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
    }
}
