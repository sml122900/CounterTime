using System;

namespace CounterTime.Core
{
    public enum RefereeTell { None, Minor, Major }
    public enum FeintVerdict { NotACandidate, Undetected, Detected }

    public readonly struct ShidoEvent
    {
        public readonly FighterId Who;
        public readonly int Count;          // this fighter's shido count after the call
        public readonly int NewThreshold;   // escalated (§6.2)

        public ShidoEvent(FighterId who, int count, int newThreshold)
        {
            Who = who;
            Count = count;
            NewThreshold = newThreshold;
        }
    }

    /// MATCH_RULES §6. Accumulators are hidden — never surfaced as a number (§6.1/§6.4).
    public sealed class RefereeSystem
    {
        readonly MatchRulesConfig _cfg;
        readonly int[] _acc = new int[2];
        readonly int[] _threshold = new int[2];
        readonly int[] _shido = new int[2];
        readonly int[] _turnsSinceAttempt = { -1, -1 };
        readonly bool[] _feintCandidate = new bool[2];

        public RefereeProfile Profile { get; }

        public RefereeSystem(RefereeProfile profile, MatchRulesConfig cfg, IRandomSource rng)
        {
            _cfg = cfg;
            Profile = profile;
            int baseThreshold = profile switch
            {
                RefereeProfile.Strict  => cfg.ThresholdStrict,
                RefereeProfile.Lenient => cfg.ThresholdLenient,
                _                      => cfg.ThresholdStandard,
            };
            // §6.2 — ±1 at match start, rolled once per fighter-independent referee.
            int jitter = rng.NextInt(-cfg.ThresholdJitter, cfg.ThresholdJitter + 1);
            _threshold[0] = _threshold[1] = baseThreshold + jitter;
        }

        public int Threshold(FighterId who) => _threshold[(int)who];
        public int ShidoCount(FighterId who) => _shido[(int)who];

        /// §6.1 — EXCLUSIVE categories. This is an if/else ladder on purpose: summing
        /// the matching flags would give a turtled fighter 3+2+2 = 7 per turn and a
        /// shido in two turns instead of four.
        public void ObserveTurn(FighterId who, in TurnChoice choice, bool attackEntry, bool realAttempt)
        {
            int i = (int)who;
            _acc[i] = Math.Max(0, _acc[i] + Category(choice, attackEntry, realAttempt));

            // §6.3 feint bookkeeping
            if (realAttempt)
            {
                _turnsSinceAttempt[i] = 0;
                _feintCandidate[i] = false;
                return;
            }
            if (_turnsSinceAttempt[i] < 0) return;

            _turnsSinceAttempt[i]++;
            if (_turnsSinceAttempt[i] <= _cfg.FeintWindowTurns && IsRetreat(choice))
                _feintCandidate[i] = true;
        }

        int Category(in TurnChoice c, bool attackEntry, bool realAttempt)
        {
            if (realAttempt) return _cfg.PassRealAttempt;                        // −4
            if (attackEntry) return _cfg.PassAttackEntry;                        // −2
            if (c.IsExtremeDefense) return _cfg.PassExtremeDefense;              // +3
            if (c.Core == CoreStance.Defensive && c.Legs == LegAction.Defend)
                return _cfg.PassDoubleDefensive;                                 // +2
            if (c.Hand == HandTarget.Evade) return _cfg.PassHandEvade;           // +2
            if (c.Legs == LegAction.CounterReady) return _cfg.PassCounterReady;  // +2
            if (c.IsAllNeutral) return _cfg.PassAllNeutral;                      // +1
            return 0;
        }

        /// CounterReady is a commitment, not a retreat — it must not feed §6.3.
        static bool IsRetreat(in TurnChoice c) =>
            c.IsExtremeDefense
            || c.Hand == HandTarget.Evade
            || (c.Core == CoreStance.Defensive && c.Legs == LegAction.Defend);

        /// §6.2 — at or over threshold: shido, accumulator resets, threshold escalates.
        public ShidoEvent? ConsumeShidoIfDue(FighterId who)
        {
            int i = (int)who;
            if (_acc[i] < _threshold[i]) return null;
            _acc[i] = 0;
            _threshold[i] += _cfg.ThresholdEscalation;
            _shido[i]++;
            return new ShidoEvent(who, _shido[i], _threshold[i]);
        }

        /// §6.3 — detection roll against the profile rate. A detected feint is an
        /// immediate shido to the attacker; undetected is a free escape.
        public FeintVerdict JudgeFeint(FighterId who, IRandomSource rng)
        {
            int i = (int)who;
            if (!_feintCandidate[i]) return FeintVerdict.NotACandidate;
            _feintCandidate[i] = false;
            _turnsSinceAttempt[i] = -1;

            float rate = Profile switch
            {
                RefereeProfile.Strict  => _cfg.DetectStrict,
                RefereeProfile.Lenient => _cfg.DetectLenient,
                _                      => _cfg.DetectStandard,
            };
            if (rng.NextFloat() >= rate) return FeintVerdict.Undetected;

            _shido[i]++;
            _acc[i] = 0;
            _threshold[i] += _cfg.ThresholdEscalation;
            return FeintVerdict.Detected;
        }

        /// §6.4 — the only player-facing signal. Numbers stay hidden.
        public RefereeTell CurrentTell(FighterId who)
        {
            int i = (int)who;
            float ratio = (float)_acc[i] / _threshold[i];
            if (ratio >= _cfg.TellMajorRatio) return RefereeTell.Major;
            if (ratio >= _cfg.TellMinorRatio) return RefereeTell.Minor;
            return RefereeTell.None;
        }
    }
}
