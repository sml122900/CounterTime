using NUnit.Framework;
using CounterTime.Core;

namespace CounterTime.Core.Tests
{
    public class JudgementCalculatorTests
    {
        MatchRulesConfig _cfg;

        [SetUp]
        public void SetUp() => _cfg = new MatchRulesConfig();

        JudgementResult Eval(int grip, ReadLevel read, int oppStamina,
                             TechniqueClass tech, bool oppDefensive) =>
            JudgementCalculator.Evaluate(FighterId.A,
                new JudgementContext(grip, read, oppStamina, tech, oppDefensive), _cfg);

        // ---- MATCH_RULES §4.1 canonical validations ----

        [Test]
        public void AttackSpam_VsTurtledOpponent_IsNoPoint()
        {
            var r = Eval(+2, ReadLevel.Neutral, 100, TechniqueClass.Standard, true);
            Assert.AreEqual(1, r.Sum);
            Assert.AreEqual(Judgement.None, r.Judgement);
            Assert.IsFalse(r.IsCounter);
        }

        [Test]
        public void BitDangyeochigi_OnRushingOpponent_IsYuko()
        {
            var r = Eval(-1, ReadLevel.Read, 100, TechniqueClass.Standard, false);
            Assert.AreEqual(2, r.Sum);
            Assert.AreEqual(Judgement.Yuko, r.Judgement);
        }

        [Test]
        public void SignatureOnOpponentReachingForGrip_IsIppon()
        {
            var r = Eval(+2, ReadLevel.FullRead, 100, TechniqueClass.Signature, false);
            Assert.AreEqual(7, r.Sum);
            Assert.AreEqual(Judgement.Ippon, r.Judgement);
        }

        // ---- counter cases ----

        [Test]
        public void SumMinusFour_CountersToOpponent_AsYuko()
        {
            var r = Eval(-2, ReadLevel.GotRead, 100, TechniqueClass.Standard, false);
            Assert.AreEqual(-4, r.Sum);
            Assert.IsTrue(r.IsCounter);
            Assert.AreEqual(FighterId.B, r.ScoredBy);
            Assert.AreEqual(Judgement.Yuko, r.Judgement);
        }

        [Test]
        public void SumMinusTwo_IsNotACounter()
        {
            var r = Eval(0, ReadLevel.GotRead, 100, TechniqueClass.Standard, false);
            Assert.AreEqual(-2, r.Sum);
            Assert.IsFalse(r.IsCounter);
            Assert.AreEqual(FighterId.A, r.ScoredBy);
            Assert.AreEqual(Judgement.None, r.Judgement);
        }

        [Test]
        public void BadlyReadCounter_WithSmallMagnitude_ScoresNothing()
        {
            // |sum| ≤ 1 for the opponent is still None — being read isn't a free score.
            var r = JudgementCalculator.Evaluate(FighterId.A,
                new JudgementContext(-2, ReadLevel.GotRead, 100, TechniqueClass.Signature, false), _cfg);
            Assert.AreEqual(-3, r.Sum);
            Assert.IsTrue(r.IsCounter);
            Assert.AreEqual(Judgement.Yuko, r.Judgement);
        }

        // ---- stamina brackets ----

        [Test]
        public void OpponentTired_AddsTwo()
        {
            var r = Eval(0, ReadLevel.Neutral, 49, TechniqueClass.Standard, false);
            Assert.AreEqual(3, r.Sum);   // 0 + 0 + 2 + 1
        }

        [Test]
        public void OpponentExhausted_AddsThree()
        {
            var r = Eval(0, ReadLevel.Neutral, 24, TechniqueClass.Standard, false);
            Assert.AreEqual(4, r.Sum);   // 0 + 0 + 3 + 1
        }

        [Test]
        public void StaminaFifty_IsNotYetTired()
        {
            var r = Eval(0, ReadLevel.Neutral, 50, TechniqueClass.Standard, false);
            Assert.AreEqual(1, r.Sum);   // 0 + 0 + 0 + 1
        }

        // ---- §4.2 CounterReady ----

        [Test]
        public void CounterReady_RaisesReadOneStep()
        {
            Assert.AreEqual(ReadLevel.Read,
                JudgementCalculator.ApplyCounterReady(ReadLevel.Neutral, true, _cfg));
            Assert.AreEqual(ReadLevel.FullRead,
                JudgementCalculator.ApplyCounterReady(ReadLevel.Read, true, _cfg));
        }

        [Test]
        public void CounterReady_CappedAtFullRead()
        {
            Assert.AreEqual(ReadLevel.FullRead,
                JudgementCalculator.ApplyCounterReady(ReadLevel.FullRead, true, _cfg));
        }

        [Test]
        public void CounterReady_NoOpponentAttack_NoChange()
        {
            Assert.AreEqual(ReadLevel.Neutral,
                JudgementCalculator.ApplyCounterReady(ReadLevel.Neutral, false, _cfg));
            Assert.AreEqual(ReadLevel.Read,
                JudgementCalculator.ApplyCounterReady(ReadLevel.Read, false, _cfg));
        }
    }
}
