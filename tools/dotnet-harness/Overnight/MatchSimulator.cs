using System;
using System.Collections.Generic;
using CounterTime.Core;
using CounterTime.Brain;

namespace CounterTime.Harness.Overnight
{
    /// <summary>
    /// Runs one full headless match (AutoA vs. a real opponent factory from
    /// Assets/_Project/Scripts/Brain/) for the 2026-08-13 overnight data collection
    /// (docs/overnight-2026-08-13.md). Mirrors LiveMatchRunner.SubmitA's wiring
    /// (attacker-priority rule, technique building, tendency-window bookkeeping, habit
    /// observation) as closely as a MonoBehaviour-free harness can — see the inline
    /// comments below for exactly which production behaviors are being reproduced and
    /// why. Touches no Core/Brain/Game production file; this is new tooling code only.
    /// </summary>
    public static class MatchSimulator
    {
        /// Well above anything a real 240s-regulation match should need (existing smoke
        /// data without time-based endings alone ran 26-50 exchanges; this run adds
        /// osaekomi ticks and golden score on top). Hitting this is reported as a
        /// finding, never silently treated as a normal outcome (task Step 2).
        public const int SafetyCap = 3000;

        /// §H step 3's own precedent (PlayModeSmokeTests.RunMatch(setupEnabled: true)) —
        /// BalanceGate=true is a per-instance MatchRulesConfig choice at construction
        /// time, identical in kind to what that existing test already does. It is NOT a
        /// change to the config's default ⚙ values in MatchRulesConfig.cs; without it
        /// the §4.4/§S balance mechanic the task asks to measure is entirely inert
        /// (BalanceGate defaults off), so every requested balance-economy statistic
        /// would be trivially zero. ThresholdJitter=0 for determinism, same as every
        /// existing seeded-match precedent in this repo.
        static MatchRulesConfig NewConfig() => new MatchRulesConfig { ThresholdJitter = 0, BalanceGate = true };

        public static MatchResult RunOne(string label, Func<MatchRulesConfig, OpponentDefinition> opponentFactory, int seed)
        {
            var r = new MatchResult { OpponentLabel = label, Seed = seed };
            try
            {
                RunInner(opponentFactory, seed, r);
            }
            catch (Exception ex)
            {
                r.Exception = ex.GetType().Name + ": " + ex.Message;
            }
            return r;
        }

        static void RunInner(Func<MatchRulesConfig, OpponentDefinition> opponentFactory, int seed, MatchResult r)
        {
            var cfg = NewConfig();
            var rng = new SystemRandomSource(seed);
            var engine = new MatchEngine(cfg, RefereeProfile.Standard, rng);

            var opponent = opponentFactory(cfg);
            var state = new HabitState(opponent);
            var ledger = new ObservationLedger();
            var window = new TendencyWindow();
            var resolver = new LiveReadResolver(ledger, window);
            var bctx = new MatchBrainContext(engine, FighterId.B);
            var bInner = new HabitBrainCommitSource(FighterId.B, state, bctx, bctx, rng);
            // §H step 3 precedent (PlayModeSmokeTests) — wrap B with the 흔들기
            // lead-in decorator whenever BalanceGate is on, so B's own big-throw habits
            // exercise the Setup drain path too (not just A's). 2026-08-13 밤 수집이
            // 발견한 두 상호작용(변칙기가 흔들기에 예고당함 / B가 게이트를 무시하고
            // 던짐)은 SetupPrecedingCommitSource.cs 쪽에서 고쳐졌다 — 이 생성 호출은
            // 이제 그 수정판을 그대로 쓴다(여기 코드 변경 없음).
            var brain = new SetupPrecedingCommitSource(bInner, bctx, enabled: true, cfg.BalanceOnsetThreshold);

            var autoA = new AutoAPlayer(rng);

            HandTarget prevBHand = HandTarget.None;
            int brokenStreakA = 0, brokenStreakB = 0;
            int iterations = 0;

            // 2026-08-13 v3 (docs/overnight-2026-08-13-v3.md) 작업3 — 그립 파리티
            // 동역학 상태. MatchResult.cs 의 Parity* 필드 주석 참조.
            int currentParityStreak = 0;
            bool parityBreakPendingThisExchange = false;
            var pendingParityWindows = new List<int>();
            const int ParityWindowExchanges = 3;

            while (!engine.Board.IsFinished && iterations < SafetyCap)
            {
                iterations++;

                if (engine.Osaekomi.Active)
                {
                    r.OsaekomiTicks++;
                    // Holder presses in to secure the pin most of the time, defender
                    // always tries to escape — see docs/overnight-2026-08-13.md §4 for
                    // why this replaces PlayModeSmokeTests' fixed "always struggle"
                    // policy (Escape x Follow is a MISMATCHED pair per §9.3 — it credits
                    // an escape on literally every tick, so a 20s pin can structurally
                    // never happen under it; this run exists partly to exercise that
                    // exact path, so it needed a policy that can actually reach it).
                    var holderChoice = rng.NextFloat() < 0.65f ? OsaekomiHolderChoice.PressIn : OsaekomiHolderChoice.Follow;
                    var oEvents = engine.ResolveOsaekomiExchange(OsaekomiDefenderChoice.Escape, holderChoice);
                    SampleBalance(engine, r, ref brokenStreakA, ref brokenStreakB, cfg);
                    bool wasGoldenBeforeO = engine.Clock.Phase == MatchPhase.GoldenScore;
                    if (engine.Board.IsFinished)
                        r.TerminationReason = ClassifyTermination(oEvents, engine, wasGoldenBeforeO);
                    continue;
                }

                var aDecision = autoA.Decide(engine, cfg);
                // 2026-08-13 v3 작업3 — 이번 교환에 들어가기 전 그립 상태(양쪽 다
                // EffectiveGrip==0 인지). ResolveExchange 가 아래에서 그립을 바꾸므로
                // 반드시 그 전에 읽어야 "들어갈 때" 값이다.
                bool bothZeroEnteringExchange =
                    engine.Grip.EffectiveGrip(FighterId.A) == 0 && engine.Grip.EffectiveGrip(FighterId.B) == 0;
                if (engine.Grip.EffectiveGrip(FighterId.B) >= 1) r.BTurnsWithGripEdge++;
                brain.TryNext(out _, out var b);
                var brainTechnique = brain.LastTechnique;
                var brainDecision = brain.LastDecision;
                resolver.NoteOpponentHabit(brainDecision);

                bool bAttacks = brainTechnique != null;

                // 2026-08-13 재수집 — 계측만, 판단·수정 없음(작업 지시). B 자신의
                // EffectiveGrip 이 정확히 0(파리티)인 교환에서 B 가 그 턴에 기술을
                // 못 냈는지 센다 — B 의 그립 의존 습관 대부분이 OwnEffectiveGrip>=1
                // 을 요구하므로, 파리티에서는 그 습관들이 구조적으로 못 뜬다.
                if (engine.Grip.EffectiveGrip(FighterId.B) == 0)
                {
                    r.BTurnsAtGripParity++;
                    if (!bAttacks) r.BIdleTurnsAtGripParity++;
                }
                TechniqueSpec aTechnique = !bAttacks && aDecision.TechniqueId != null
                    ? BuildATechnique(aDecision.TechniqueId) : null;
                bool aAttacks = aTechnique != null;

                // 2026-08-13 v3 작업3 — 파리티 스트릭 갱신. "선언"은 흔들기(Setup
                // 표적) 또는 실제 기술(공격) 중 하나 — §4.4 흔들기는 그립을 안 바꾸므로
                // 그립 수치와 별개로 "그립 다지기를 그만두고 다음 단계로 넘어갔다"의
                // 신호로 쓴다.
                if (bothZeroEnteringExchange)
                {
                    currentParityStreak++;
                    bool aBreaks = aAttacks || aDecision.Commit.Hand == HandTarget.Setup;
                    bool bBreaks = bAttacks || b.Hand == HandTarget.Setup;
                    if (aBreaks || bBreaks)
                    {
                        r.ParityStreakCount++;
                        r.ParityStreakTotalLength += currentParityStreak;
                        if (aBreaks && bBreaks) r.ParityStreakEndedByBothDeclare++;
                        else if (aBreaks) r.ParityStreakEndedByADeclare++;
                        else r.ParityStreakEndedByBDeclare++;
                        parityBreakPendingThisExchange = true;
                        currentParityStreak = 0;
                    }
                }
                else if (currentParityStreak > 0)
                {
                    // 그립이 이전 교환의 결과로 이미 0-0 을 벗어났다(이번 교환에 선언은
                    // 없었다) — "그립 액션으로 파리티가 풀렸다"로 집계.
                    r.ParityStreakCount++;
                    r.ParityStreakTotalLength += currentParityStreak;
                    r.ParityStreakEndedByGripResolved++;
                    currentParityStreak = 0;
                }

                FighterId? attacker = bAttacks ? FighterId.B : aAttacks ? FighterId.A : (FighterId?)null;
                TechniqueSpec technique = bAttacks ? brainTechnique : aAttacks ? aTechnique : null;

                // §S gate-attempt bookkeeping — captured BEFORE ResolveExchange, since
                // resolving drains Balance as part of the same call.
                if (technique != null && technique.Cost == ActionCost.BigThrow && !technique.SurpriseExempt && attacker.HasValue)
                {
                    int defenderBalance = engine.Balance.Get(Opp(attacker.Value));
                    bool open = defenderBalance >= cfg.BalanceOnsetThreshold;
                    if (attacker.Value == FighterId.A)
                    {
                        if (open) r.GateOpenBigThrowAttemptsA++; else r.GateClosedBigThrowAttemptsA++;
                    }
                    else
                    {
                        if (open) r.GateOpenBigThrowAttemptsB++; else r.GateClosedBigThrowAttemptsB++;
                    }
                }

                ReadLevel read = attacker.HasValue ? resolver.For(attacker.Value, aDecision.Commit, b) : ReadLevel.Neutral;

                var script = new ExchangeScript { Attacker = attacker, Technique = technique, Read = read };
                bool wasGoldenBefore = engine.Clock.Phase == MatchPhase.GoldenScore;
                var events = engine.ResolveExchange(aDecision.Commit, b, script);
                r.ExchangesResolved++;

                bool hasScoreThisExchange = false;
                foreach (var e in events)
                {
                    if (e is JudgementScored js)
                    {
                        r.Judgements[js.Judgement] = (r.Judgements.TryGetValue(js.Judgement, out int c) ? c : 0) + 1;
                        r.ScoredBy[js.ScoredBy] = (r.ScoredBy.TryGetValue(js.ScoredBy, out int sc) ? sc : 0) + 1;
                        if (js.IsCounter) r.Counters++;
                        if (js.Judgement != Judgement.None) hasScoreThisExchange = true;
                    }
                    if (e is OsaekomiEntered) r.OsaekomiEntries++;
                }

                // 2026-08-13 v3 작업3 — 대기 중인 파리티-선언 창(과거 교환에서 열린 것)
                // 부터 먼저 이번 교환의 점수 유무로 정산하고, 그 다음 이번 교환 자체가
                // 새로 연 창을 등록한다(즉시 득점이면 그 자리에서 바로 성공 처리).
                for (int wi = pendingParityWindows.Count - 1; wi >= 0; wi--)
                {
                    if (hasScoreThisExchange) { r.ParityBreakScoreWithinWindow++; pendingParityWindows.RemoveAt(wi); continue; }
                    pendingParityWindows[wi]--;
                    if (pendingParityWindows[wi] <= 0) { r.ParityBreakScoreMissed++; pendingParityWindows.RemoveAt(wi); }
                }
                if (parityBreakPendingThisExchange)
                {
                    r.ParityBreakDeclareTotal++;
                    if (hasScoreThisExchange) r.ParityBreakScoreWithinWindow++;
                    else pendingParityWindows.Add(ParityWindowExchanges - 1);
                    parityBreakPendingThisExchange = false;
                }

                // 2026-08-13 v3 작업1 — 로스터 차별성을 B 자신의 행동으로 재는 지표
                // (승패와 무관). brainDecision/brainTechnique/b 는 위에서 이미 계산됨.
                if (brainDecision.Fired)
                {
                    var hid = brainDecision.Habit.Id;
                    r.BHabitFirings[hid] = (r.BHabitFirings.TryGetValue(hid, out int hc2) ? hc2 : 0) + 1;
                }
                if (b.Legs == LegAction.CounterReady) r.BCounterReadyDeclares++;
                if (IsGripTarget(b.Hand))
                    r.BGripTargetCounts[b.Hand] = (r.BGripTargetCounts.TryGetValue(b.Hand, out int gc) ? gc : 0) + 1;
                if (b.Hand == HandTarget.Setup) r.BSetupDeclareCount++;
                else if (brainTechnique != null && brainTechnique.Cost == ActionCost.Poke) r.BPokeDeclareCount++;
                else if (brainTechnique != null && brainTechnique.Cost == ActionCost.BigThrow) r.BBigThrowDeclareCount++;
                else r.BOtherDeclareCount++;

                // technique usage tallies — only the fighter who actually got to act
                // this exchange (attacker priority: B wins simultaneous attacks, the
                // existing "v1 임시 규칙" precedent this run reuses as-is, not a new
                // design decision — see docs/overnight-2026-08-13.md observations).
                if (attacker == FighterId.A && aTechnique != null)
                    r.ATechniqueUsage[aTechnique.Id] = (r.ATechniqueUsage.TryGetValue(aTechnique.Id, out int ac) ? ac : 0) + 1;
                if (attacker == FighterId.B && brainTechnique != null)
                {
                    r.BTechniqueUsage[brainTechnique.Id] = (r.BTechniqueUsage.TryGetValue(brainTechnique.Id, out int bc) ? bc : 0) + 1;
                    bool isSurprise = brainDecision.Habit == null && brainTechnique.SurpriseExempt;
                    if (isSurprise)
                    {
                        r.BSurpriseFirings++;
                        if (prevBHand == HandTarget.Setup) r.BSurpriseTelegraphedBySetup++;
                    }
                }

                // §4.2 damage bookkeeping — same ordering contract HabitState.RegisterDamage
                // documents (observe BEFORE this turn's own Record call).
                if (brainDecision.Fired)
                    foreach (var e in events)
                        if (e is JudgementScored j2 && j2.ScoredBy == FighterId.A && (j2.IsCounter || j2.Judgement != Judgement.None))
                            state.RegisterDamage(brainDecision.Habit.Id, ledger);
                ledger.Record(brainDecision);
                bctx.ObserveExchange(events);
                // §A.2 (LiveMatchRunner.SubmitA precedent) — technique turns are excluded
                // from the tendency window, not filled with a neutral placeholder.
                if (!aAttacks) window.Record(aDecision.Commit);
                autoA.ObserveExchangeResult(b);
                prevBHand = b.Hand;

                SampleBalance(engine, r, ref brokenStreakA, ref brokenStreakB, cfg);

                if (engine.Board.IsFinished)
                    r.TerminationReason = ClassifyTermination(events, engine, wasGoldenBefore);
            }

            // 2026-08-13 v3 작업3 — 경기가 끝날 때 아직 진행 중이던 파리티 스트릭을
            // 마감(match-end 로 집계, 다른 이유로 끝난 것과 섞이지 않게 분리).
            if (currentParityStreak > 0)
            {
                r.ParityStreakCount++;
                r.ParityStreakTotalLength += currentParityStreak;
                r.ParityStreakEndedByMatchEnd++;
            }

            r.HitSafetyCap = iterations >= SafetyCap && !engine.Board.IsFinished;
            r.Winner = engine.Board.Winner;
            r.ClockRemainingSeconds = engine.Clock.RemainingSeconds;
            r.ShidoA = engine.Board.Shido(FighterId.A); r.ShidoB = engine.Board.Shido(FighterId.B);
            r.WazaAriA = engine.Board.WazaAri(FighterId.A); r.WazaAriB = engine.Board.WazaAri(FighterId.B);
            r.YukoA = engine.Board.Yuko(FighterId.A); r.YukoB = engine.Board.Yuko(FighterId.B);
            r.FinalBalanceA = engine.Balance.Get(FighterId.A); r.FinalBalanceB = engine.Balance.Get(FighterId.B);
            r.LongestBrokenStreakA = Math.Max(r.LongestBrokenStreakA, brokenStreakA);
            r.LongestBrokenStreakB = Math.Max(r.LongestBrokenStreakB, brokenStreakB);

            r.A_TotalDecisions = autoA.TotalDecisions;
            r.A_DefendTurns = autoA.DefendTurns;
            r.A_CounterReadyDeclares = autoA.CounterReadyDeclares;
            r.A_GripBuildTurns = autoA.GripBuildTurns;
            r.A_SetupDeclares = autoA.SetupDeclares;
            r.A_PokeAttacks = autoA.PokeAttacks;
            r.A_BigThrowAttempts = autoA.BigThrowAttempts;
            r.A_HeldBackFromLiveCounterReady = autoA.HeldBackFromLiveCounterReady;
            r.A_BigThrowsWhileGateClosedSanity = autoA.BigThrowsAttemptedWhileGateClosed;
            r.A_SuspectedGripDeadlock = autoA.SuspectedGripDeadlock;

            if (r.HitSafetyCap) r.TerminationReason = "DID NOT FINISH — hit the " + SafetyCap + "-iteration safety cap";
        }

        static void SampleBalance(MatchEngine engine, MatchResult r, ref int brokenStreakA, ref int brokenStreakB, MatchRulesConfig cfg)
        {
            int balA = engine.Balance.Get(FighterId.A);
            int balB = engine.Balance.Get(FighterId.B);
            r.MinBalanceA = Math.Min(r.MinBalanceA, balA);
            r.MinBalanceB = Math.Min(r.MinBalanceB, balB);

            if (balA < cfg.BalanceOnsetThreshold) { r.TurnsBalanceBelowThresholdA++; brokenStreakA++; }
            else { r.LongestBrokenStreakA = Math.Max(r.LongestBrokenStreakA, brokenStreakA); brokenStreakA = 0; }
            if (balB < cfg.BalanceOnsetThreshold) { r.TurnsBalanceBelowThresholdB++; brokenStreakB++; }
            else { r.LongestBrokenStreakB = Math.Max(r.LongestBrokenStreakB, brokenStreakB); brokenStreakB = 0; }
        }

        /// Mirrors LiveMatchRunner.BuildLiveTechnique exactly (same TechniqueRegistry
        /// lookup, AuthoredClass left null so MatchEngine derives it from mastery) — this
        /// is deliberate, not a simplification: it means A's own techniques resolve
        /// through the SAME mastery-derivation path a real player's do today, which
        /// currently always yields Sloppy (IMasteryStore defaults to ZeroMasteryStore,
        /// §H step 7 not implemented yet). See docs/overnight-2026-08-13.md's
        /// observations section — this is a real, reportable asymmetry against B's
        /// habit-driven throws, which carry an explicit Standard AuthoredClass literal.
        static TechniqueSpec BuildATechnique(string techniqueId)
        {
            var def = TechniqueRegistry.Get(techniqueId);
            return def == null ? null : new TechniqueSpec(def.Id, def.BaseCost, null, def.SurpriseExempt);
        }

        static string ClassifyTermination(IReadOnlyList<MatchEvent> events, MatchEngine engine, bool wasGoldenScoreBefore)
        {
            bool hasIpponPin = false, hasThrownIppon = false, hasWazaAri = false, hasYuko = false, hasShidoOut = false;
            foreach (var e in events)
            {
                if (e is OsaekomiScoreAwarded osa && osa.Judgement == Judgement.Ippon) hasIpponPin = true;
                if (e is JudgementScored js)
                {
                    if (js.Judgement == Judgement.Ippon) hasThrownIppon = true;
                    if (js.Judgement == Judgement.WazaAri) hasWazaAri = true;
                    if (js.Judgement == Judgement.Yuko) hasYuko = true;
                }
                if (e is ShidoGiven) hasShidoOut = true;
            }
            bool hasAnyScore = hasThrownIppon || hasWazaAri || hasYuko;
            string prefix = wasGoldenScoreBefore ? "golden score - " : "";

            if (hasIpponPin) return prefix + "osaekomi pin (20s ippon)";
            if (hasShidoOut && (engine.Board.Shido(FighterId.A) >= 3 || engine.Board.Shido(FighterId.B) >= 3))
                return prefix + "3rd shido (hansoku-make)";
            if (hasThrownIppon) return prefix + "ippon (thrown)";
            if (hasWazaAri) return prefix + (wasGoldenScoreBefore ? "waza-ari (decisive)" : "waza-ari x2 (awasete-ippon)");
            if (hasYuko) return prefix + "yuko (decisive)";
            if (!hasAnyScore) return "regulation expiry - clear leader on waza-ari/yuko count (no score this exchange)";
            return "other/unclassified";
        }

        static FighterId Opp(FighterId f) => f == FighterId.A ? FighterId.B : FighterId.A;

        /// 2026-08-13 v3 작업1 — "그립을 노리는" 손 표적만(뜯기/흔들기/유지/없음/회피는
        /// 제외 — 이건 "새 홀드를 노린다"의 실측이지 손 축 전체 사용량이 아니다).
        static bool IsGripTarget(HandTarget t) => t == HandTarget.RightSleeve || t == HandTarget.LeftSleeve
            || t == HandTarget.LeftLapel || t == HandTarget.RightLapel
            || t == HandTarget.TwistGrip || t == HandTarget.BackGrab;
    }
}
