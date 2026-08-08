using System;

namespace CounterTime.Core
{
    /// MATCH_RULES §1–§2. The clock stops during matte; osaekomi runs in real time.
    public sealed class MatchClock
    {
        readonly MatchRulesConfig _cfg;

        public MatchClock(MatchRulesConfig cfg) => _cfg = cfg;

        public int ElapsedSeconds { get; private set; }
        public MatchPhase Phase { get; set; } = MatchPhase.Standing;

        public int RemainingSeconds => Math.Max(0, _cfg.RegulationSeconds - ElapsedSeconds);
        public bool RegulationExpired => ElapsedSeconds >= _cfg.RegulationSeconds;

        /// §2 — an exchange lasts as long as the SLOWER of the two commitments, so
        /// both fighters always exit at the same timestamp.
        /// §1 — the exchange itself is played out in full; only the clock clamps, so
        /// overshoot past 240 s is discarded rather than carried into golden score.
        public void AdvanceExchange(int costA, int costB)
        {
            if (Phase == MatchPhase.Matte) return;
            Apply(_cfg.DurationRule switch
            {
                ExchangeDurationRule.Max => Math.Max(costA, costB),
                _ => throw new NotSupportedException(_cfg.DurationRule.ToString()),
            });
        }

        /// §1 — osaekomi is real time and runs even though it is not a standing exchange.
        public void AdvanceOsaekomi(int seconds) => Apply(seconds);

        void Apply(int seconds)
        {
            ElapsedSeconds += seconds;
            // §1 expiry: regulation overflow is discarded. Golden score is unlimited.
            if (Phase != MatchPhase.GoldenScore && ElapsedSeconds > _cfg.RegulationSeconds)
                ElapsedSeconds = _cfg.RegulationSeconds;
        }
    }
}
