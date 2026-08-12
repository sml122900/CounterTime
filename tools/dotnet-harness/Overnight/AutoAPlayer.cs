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
    ///      (EffectiveGrip &gt;= 1) is a precondition for every attack branch below.
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

        readonly IRandomSource _rng;
        int _bigIdx, _pokeIdx, _drainTurnCount, _gripBuildTurnCount;
        bool _lastBWasCounterReady;

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
            if (ownGrip < 1)
            {
                GripBuildTurns++;
                bool right = _gripBuildTurnCount % 2 == 0;
                _gripBuildTurnCount++;
                return NonAttack(right
                    ? new TurnChoice(HandTarget.RightSleeve, CoreStance.Neutral, LegAction.Neutral, Hand.Right)
                    : new TurnChoice(HandTarget.LeftSleeve, CoreStance.Neutral, LegAction.Neutral, Hand.Left));
            }

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
