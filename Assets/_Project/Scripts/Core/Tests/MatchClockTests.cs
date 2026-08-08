using NUnit.Framework;
using CounterTime.Core;

namespace CounterTime.Core.Tests
{
    public class MatchClockTests
    {
        MatchRulesConfig _cfg;
        MatchClock _c;

        [SetUp]
        public void SetUp()
        {
            _cfg = new MatchRulesConfig();
            _c = new MatchClock(_cfg);
        }

        // ---- patch 1.5: exchange duration ----

        [Test]
        public void Exchange_AdvancesByMaxCost()
        {
            // 5 s grip turn vs 3 s technique → the slower commitment sets the length
            _c.AdvanceExchange(_cfg.GripTurnSec, _cfg.ExchangeSec);
            Assert.AreEqual(5, _c.ElapsedSeconds);
        }

        [Test]
        public void Exchange_MaxIsSymmetric()
        {
            _c.AdvanceExchange(_cfg.ExchangeSec, _cfg.GripTurnSec);
            Assert.AreEqual(5, _c.ElapsedSeconds, "order of the arguments must not matter");
        }

        [Test]
        public void Exchange_NeverSumsTheTwoCosts()
        {
            _c.AdvanceExchange(3, 3);
            Assert.AreEqual(3, _c.ElapsedSeconds, "3+3 would be 6");
        }

        // ---- §1 matte stops the clock ----

        [Test]
        public void Matte_FreezesTheClock()
        {
            _c.AdvanceExchange(5, 3);
            _c.Phase = MatchPhase.Matte;
            _c.AdvanceExchange(5, 5);
            Assert.AreEqual(5, _c.ElapsedSeconds);
        }

        [Test]
        public void Osaekomi_RunsInRealTime()
        {
            _c.AdvanceOsaekomi(20);
            Assert.AreEqual(20, _c.ElapsedSeconds);
        }

        // ---- patch 1.6: expiry ----

        [Test]
        public void ExchangeBegunAt238_ResolvesFully_ThenElapsedClampsTo240()
        {
            _c.AdvanceOsaekomi(238);
            Assert.IsFalse(_c.RegulationExpired, "time-up is checked AFTER the exchange");

            // a 5 s grip exchange begins with 2 s left — it is played out in full,
            // and only the overshoot is discarded.
            _c.AdvanceExchange(5, 3);

            Assert.AreEqual(240, _c.ElapsedSeconds, "243 would leak into golden score");
            Assert.AreEqual(0, _c.RemainingSeconds);
            Assert.IsTrue(_c.RegulationExpired);
        }

        [Test]
        public void Regulation_ClampsAtFourMinutes()
        {
            for (int i = 0; i < 100; i++) _c.AdvanceExchange(5, 5);
            Assert.AreEqual(240, _c.ElapsedSeconds);
        }

        [Test]
        public void GoldenScore_IsUnlimited()
        {
            _c.AdvanceOsaekomi(_cfg.RegulationSeconds);
            _c.Phase = MatchPhase.GoldenScore;
            _c.AdvanceExchange(5, 3);
            Assert.AreEqual(245, _c.ElapsedSeconds, "golden score is not clamped");
        }
    }
}
