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

        // ---- §4.2 CounterReady: the ATTACKER's read drops ----

        [Test]
        public void CounterReady_LowersAttackerReadOneStep()
        {
            Assert.AreEqual(ReadLevel.Read, JudgementCalculator.StepDownRead(ReadLevel.FullRead));
            Assert.AreEqual(ReadLevel.Neutral, JudgementCalculator.StepDownRead(ReadLevel.Read));
            Assert.AreEqual(ReadLevel.GotRead, JudgementCalculator.StepDownRead(ReadLevel.Neutral));
        }

        [Test]
        public void CounterReady_FlooredAtGotRead()
        {
            Assert.AreEqual(ReadLevel.GotRead, JudgementCalculator.StepDownRead(ReadLevel.GotRead));
            Assert.AreEqual(ReadLevel.GotRead, JudgementCalculator.StepDownRead(ReadLevel.Neutral, 5));
        }

        [Test]
        public void CounterReady_CannotCounterFullRead()
        {
            // The rung is uneven on purpose: FullRead bottoms out at Read (+2), so a
            // fully-read attack keeps a positive read term and cannot reach −3.
            var dropped = JudgementCalculator.StepDownRead(ReadLevel.FullRead);
            Assert.AreEqual(ReadLevel.Read, dropped);

            var r = Eval(-1, dropped, 100, TechniqueClass.Standard, false);
            Assert.AreEqual(2, r.Sum, "−1 + 2 + 0 + 1 = 2");
            Assert.IsFalse(r.IsCounter, "reading is beaten only by reading");
        }

        // ---- §4.1 canonical validations 4 and 5 ----

        [Test]
        public void CounterReadyLands_AtGripMinusOne_IsCounterYuko()
        {
            var dropped = JudgementCalculator.StepDownRead(ReadLevel.Neutral);
            Assert.AreEqual(ReadLevel.GotRead, dropped);

            // posture open — counter-ready is NOT a defensive stance, so no −2
            var r = Eval(-1, dropped, 100, TechniqueClass.Standard, false);
            Assert.AreEqual(-3, r.Sum, "−1 −3 + 0 + 1 = −3");
            Assert.IsTrue(r.IsCounter);
            Assert.AreEqual(FighterId.B, r.ScoredBy, "Yuko to the defender");
            Assert.AreEqual(Judgement.Yuko, r.Judgement);
        }

        [Test]
        public void CounterReadyAlone_AtGripZero_IsNotEnough()
        {
            var dropped = JudgementCalculator.StepDownRead(ReadLevel.Neutral);
            var r = Eval(0, dropped, 100, TechniqueClass.Standard, false);
            Assert.AreEqual(-2, r.Sum, "0 −3 + 0 + 1 = −2");
            Assert.IsFalse(r.IsCounter, "read AND grip must both be true for 되치기");
            Assert.AreEqual(Judgement.None, r.Judgement);
        }

        // ---- §4 defensive stance (v3.2) ----

        static TurnChoice Choice(HandTarget hand, CoreStance core, LegAction legs) =>
            new TurnChoice(hand, core, legs);

        [Test]
        public void DefensiveCore_AloneTriggersPostureTerm()
        {
            var c = Choice(HandTarget.None, CoreStance.Defensive, LegAction.Neutral);
            Assert.IsTrue(JudgementCalculator.IsDefensivePosture(c));

            // §4.1 validation 6: 0 + 0 + 0 + 1 + (−2) = −1 ⇒ no point
            var r = Eval(0, ReadLevel.Neutral, 100, TechniqueClass.Standard, true);
            Assert.AreEqual(-1, r.Sum);
            Assert.AreEqual(Judgement.None, r.Judgement);
        }

        [Test]
        public void DefensiveLegs_AloneDoesNotTriggerPostureTerm()
        {
            var c = Choice(HandTarget.None, CoreStance.Neutral, LegAction.Defend);
            Assert.IsFalse(JudgementCalculator.IsDefensivePosture(c),
                "legs 수비 alone is not a defensive stance");
        }

        [Test]
        public void HandEvade_AloneDoesNotTriggerPostureTerm()
        {
            var c = Choice(HandTarget.Evade, CoreStance.Neutral, LegAction.Neutral);
            Assert.IsFalse(JudgementCalculator.IsDefensivePosture(c),
                "hand 회피 alone is not a defensive stance");
        }

        [Test]
        public void ExtremeDefense_ScoresSamePostureTermAsPlainDefensiveCore()
        {
            var extreme = Choice(HandTarget.Evade, CoreStance.Defensive, LegAction.Defend);
            var plain = Choice(HandTarget.None, CoreStance.Defensive, LegAction.Neutral);
            Assert.IsTrue(extreme.IsExtremeDefense);
            Assert.AreEqual(JudgementCalculator.IsDefensivePosture(plain),
                            JudgementCalculator.IsDefensivePosture(extreme),
                "depth of turtling is charged by §6.1 and §5.2, not a third time here");
        }

        [Test]
        public void CounterReady_SuppressesPostureTermEvenWithDefensiveCore()
        {
            var c = Choice(HandTarget.None, CoreStance.Defensive, LegAction.CounterReady);
            Assert.IsFalse(JudgementCalculator.IsDefensivePosture(c));

            // §4.1 validation 7: 0 + (−3) + 0 + 1 + 0 = −2 ⇒ None.
            var dropped = JudgementCalculator.StepDownRead(ReadLevel.Neutral);
            var r = Eval(0, dropped, 100, TechniqueClass.Standard,
                         JudgementCalculator.IsDefensivePosture(c));
            Assert.AreEqual(-2, r.Sum);
            Assert.AreEqual(Judgement.None, r.Judgement);

            // The exception is load-bearing: applying −2 here would be −4 ⇒ free Yuko.
            var wrong = Eval(0, dropped, 100, TechniqueClass.Standard, true);
            Assert.AreEqual(-4, wrong.Sum);
            Assert.IsTrue(wrong.IsCounter, "this is what the suppression prevents");
        }
    }
}
