using CounterTime.Core;

namespace CounterTime.Harness.Overnight
{
    /// <summary>
    /// A tuning-data heuristic for the A side, built for the 2026-08-13 overnight data
    /// collection run (docs/overnight-2026-08-13.md). This is NOT optimal or human-level
    /// play — it exists so headless self-play exercises the balance/technique economy
    /// realistically instead of the "scarecrow" A that PlayModeSmokeTests uses (fixed
    /// technique, ignores its own declared axis — see that file's own doc comment and
    /// STATUS.md's "Smoke A is a scarecrow" note). Lives entirely in the harness tool —
    /// touches no Core/Brain/Game production code.
    ///
    /// Decision order per turn (checked top to bottom, first match wins):
    ///   1. Stamina safety — rest instead of attacking when nearly exhausted.
    ///   2. React to a live opponent tell (§4.4 흔들기/Setup is visible via
    ///      MatchEngine.IsSetupActive, the same public read SeoDohyun's own H1 habit
    ///      uses from the mirror side) by declaring CounterReady.
    ///   3. Contest grip before committing to any technique — an established grip
    ///      (EffectiveGrip &gt;= 1) is preferred, but only for up to
    ///      <see cref="GripBuildPatienceLimit"/> consecutive turns (2026-08-13 재수집 —
    ///      both sides can reach a stable Value=2 grip parity where EffectiveGrip never
    ///      reaches +/-1 for either fighter; waiting forever there is not real-player
    ///      behavior). Past the patience limit, A stops waiting and moves on to 4/5 at
    ///      parity, same as if grip had been won. A hard, whole-match cap
    ///      (<see cref="GripBuildHardCap"/>) backs this up structurally — see
    ///      <see cref="SuspectedGripDeadlock"/>.
    ///   4. If the opponent's Balance is still at/above the onset threshold (§4.4 gate
    ///      closed): drain it instead of wasting a big throw into an automatic Sloppy
    ///      downgrade — a feint/footsweep-class poke (§D.4 floor drain) most of the
    ///      time, an outright 흔들기(Setup) declare on a rotating minority of those
    ///      turns so that mechanic gets exercised too.
    ///   5. Otherwise (gate open) — go for a big throw, unless B declared CounterReady
    ///      last exchange (never force a big throw into a live counter).
    /// </summary>
    public sealed class AutoAPlayer
    {
        public readonly struct Decision
        {
            public readonly TurnChoice Commit;
            /// null = no technique this exchange (grip/defense/counter-ready turn).
            public readonly string TechniqueId;
            public Decision(TurnChoice commit, string techniqueId)
            {
                Commit = commit;
                TechniqueId = techniqueId;
            }
        }

        static readonly string[] BigThrows = { "밭다리걸기", "허리후리기", "안오금띄기", "어깨 매치기" };
        static readonly string[] Pokes = { "모두걸기", "발목받치기", "나오는발차기", "발뒤축걸기" };

        // §H 3.5b 후속 (2026-08-13 재수집) — 2026-08-13 첫 재수집이 실제로 밝힌 문제:
        // 그립 한손 고정 버그(HabitBrainCommitSource)를 고치자 B 도 양손을 다 쥘 수
        // 있게 됐고, 둘 다 Value=2 파리티(GripScore=0)에 도달하면 이 클래스의 옛 조건
        // (ownGrip<1)이 A/B 양쪽에서 동시에 참으로 영원히 남아 그립다지기에서 못
        // 빠져나왔다(실측: 서도현전 seed=5, 3000턴 중 2994턴이 그립다지기). 사람 결정
        // (2026-08-13 재수집 지시): 인내 한계 + 파리티 커밋 + 하드 상한 3중 안전장치.
        const int GripBuildPatienceLimit = 6;   // ⚙ — 이 턴 안에 우위를 못 얻으면 포기
        const int GripBuildHardCap = 50;        // ⚙ — 경기 전체 누적, 넘으면 교착 의심으로 집계하고 이후 영구 우회

        readonly IRandomSource _rng;
        int _bigIdx, _pokeIdx, _drainTurnCount, _gripBuildTurnCount;
        int _gripBuildStreak, _totalGripBuildTurns;
        bool _lastBWasCounterReady;

        /// 경기 전체에서 GripBuildHardCap 을 한 번이라도 넘었는가 — 기대값은 0(정상
        /// 이면 절대 안 걸린다). §4 안전상한(3000교환)과는 다른 지표: 이건 "그립싸움
        /// 하나가 하드 상한을 넘겼다"는 좁은 신호라, 안전상한 도달보다 먼저 걸려야
        /// 정상이고(그립싸움이 원인이면), 안 걸렸는데 안전상한만 걸렸다면 원인이
        /// 그립싸움이 아니라는 뜻이다.
        public bool SuspectedGripDeadlock { get; private set; }

        // ── behavioral characterization counters (report §1) ───────────────────────
        public int TotalDecisions { get; private set; }
        public int DefendTurns { get; private set; }
        public int CounterReadyDeclares { get; private set; }
        public int GripBuildTurns { get; private set; }
        public int SetupDeclares { get; private set; }
        public int PokeAttacks { get; private set; }
        public int BigThrowAttempts { get; private set; }
        public int HeldBackFromLiveCounterReady { get; private set; }
        /// Sanity counter — should always stay 0. A big throw is only ever chosen in
        /// branch 5 above, which is gated on the balance gate already being open (or the
        /// mechanic being off). If this is ever nonzero, the heuristic has a bug.
        public int BigThrowsAttemptedWhileGateClosed { get; private set; }

        public AutoAPlayer(IRandomSource rng) => _rng = rng;

        /// Call once per resolved exchange with the TurnChoice B actually committed, so
        /// next turn's "don't force a big throw into a live counter" check has something
        /// real to read.
        public void ObserveExchangeResult(in TurnChoice bCommit) =>
            _lastBWasCounterReady = bCommit.Legs == LegAction.CounterReady;

        public Decision Decide(MatchEngine engine, MatchRulesConfig cfg)
        {
            TotalDecisions++;

            int ownStamina = engine.Stamina.Get(FighterId.A);
            int ownGrip = engine.Grip.EffectiveGrip(FighterId.A);
            int oppBalance = engine.Balance.Get(FighterId.B);
            bool oppSetupTell = engine.IsSetupActive(FighterId.B);

            // 1 — stamina safety.
            if (ownStamina <= 10)
            {
                DefendTurns++;
                return NonAttack(new TurnChoice(HandTarget.None, CoreStance.Defensive, LegAction.Defend));
            }

            // 2 — react to a live tell.
            if (oppSetupTell)
            {
                CounterReadyDeclares++;
                return NonAttack(new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.CounterReady));
            }

            // 3 — contest grip first. FOUND DURING PILOT RUN, FIXED HERE (harness-only
            // bug, not Core/Brain): a single-hand "always grab RightSleeve with Right"
            // policy produces a permanent 1-vs-1 grip tie against most B opponents,
            // because B's own §4 fallback ALSO always grabs RightSleeve with Right
            // (HabitBrainCommitSource.ActionFor(Move.GripFight), hardcoded, no Left
            // option) — GripScore nets to 0 forever and neither side's EffectiveGrip
            // ever reaches +/-1, so AutoA could never leave this branch (a first pilot
            // run measured 99.4% of ALL decisions stuck here, with matches running
            // thousands of exchanges into the safety cap). Alternating hands across
            // consecutive grip-build turns lets A hold BOTH sleeves (Value=2) against
            // B's capped single hold (Value<=1, since B's fallback never varies hands),
            // which reliably breaks the tie in favor of A within two turns. This is
            // still a heuristic, not optimal play — it does not try to prevent B from
            // achieving a bilateral hold, it just stops A from being the only side
            // permanently limited to one hand.
            if (ownGrip >= 1)
            {
                _gripBuildStreak = 0;   // 우위를 얻었다 — 다음에 다시 못 얻어도 처음부터 K턴 기다린다.
            }
            else if (_gripBuildStreak < GripBuildPatienceLimit && _totalGripBuildTurns < GripBuildHardCap)
            {
                GripBuildTurns++;
                _gripBuildStreak++;
                _totalGripBuildTurns++;
                bool right = _gripBuildTurnCount % 2 == 0;
                _gripBuildTurnCount++;
                return NonAttack(right
                    ? new TurnChoice(HandTarget.RightSleeve, CoreStance.Neutral, LegAction.Neutral, Hand.Right)
                    : new TurnChoice(HandTarget.LeftSleeve, CoreStance.Neutral, LegAction.Neutral, Hand.Left));
            }
            else if (_totalGripBuildTurns >= GripBuildHardCap)
            {
                SuspectedGripDeadlock = true;   // 기대값 0 — 걸리면 그 자체가 보고 대상.
            }
            // 인내 한계(또는 하드 상한) 도달 — 그립 우위를 못 얻었어도 4/5로 넘어간다.
            // 실제 플레이어라면 그립만 붙잡고 무한정 기다리지 않고 파리티에서라도
            // 흔들기/견제/기술을 시도한다는 판단(2026-08-13 재수집 사람 지시).

            // 4 — gate closed (opponent's Balance still high): drain instead of wasting
            // a big throw into an automatic Sloppy downgrade (§4.4). Majority path is a
            // feint/footsweep poke (task wording); every 3rd such turn declares 흔들기
            // (Setup) instead so that drain path gets exercised too — see report §3 for
            // how the two compare once collected.
            bool gateClosed = cfg.BalanceGate && oppBalance >= cfg.BalanceOnsetThreshold;
            if (gateClosed)
            {
                _drainTurnCount++;
                if (_drainTurnCount % 3 == 0)
                {
                    SetupDeclares++;
                    return NonAttack(new TurnChoice(HandTarget.Setup, CoreStance.Neutral, LegAction.Neutral, Hand.Right));
                }
                PokeAttacks++;
                return Attack(Pokes[_pokeIdx++ % Pokes.Length]);
            }

            // 5 — gate open: go for a big throw, unless B is sitting in a live counter.
            if (_lastBWasCounterReady)
            {
                HeldBackFromLiveCounterReady++;
                PokeAttacks++;
                return Attack(Pokes[_pokeIdx++ % Pokes.Length]);
            }

            BigThrowAttempts++;
            if (cfg.BalanceGate && oppBalance >= cfg.BalanceOnsetThreshold)
                BigThrowsAttemptedWhileGateClosed++; // unreachable by construction; kept as a live self-check
            return Attack(BigThrows[_bigIdx++ % BigThrows.Length]);
        }

        static Decision NonAttack(in TurnChoice commit) => new Decision(commit, null);
        static Decision Attack(string techniqueId) =>
            new Decision(new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.Attack), techniqueId);
    }
}
