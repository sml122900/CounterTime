using NUnit.Framework;
using CounterTime.Core;

namespace CounterTime.Core.Tests
{
    public class StaminaSystemTests
    {
        MatchRulesConfig _cfg;
        StaminaSystem _s;

        [SetUp]
        public void SetUp()
        {
            _cfg = new MatchRulesConfig();
            _s = new StaminaSystem(_cfg);
        }

        static TechniqueSpec BigThrow() =>
            new TechniqueSpec("batadari", ActionCost.BigThrow, TechniqueClass.Standard);
        static TechniqueSpec Poke() =>
            new TechniqueSpec("balmok", ActionCost.Poke, TechniqueClass.Sloppy);
        static TechniqueSpec Counter() =>
            new TechniqueSpec("counter", ActionCost.Counter, TechniqueClass.Standard);

        // ---- §5.6 validation ----

        [Test]
        public void PhaseOneSpamSequence_CostsTen()
        {
            // grip +2 throughout: 3 + 1 + 3 + 3 = 10 ⇒ 100 → 90
            _s.PayAttack(FighterId.A, BigThrow(), +2);
            _s.PayAttack(FighterId.A, Poke(), +2);
            _s.PayAttack(FighterId.A, BigThrow(), +2);
            _s.PayAttack(FighterId.A, BigThrow(), +2);
            Assert.AreEqual(90, _s.Get(FighterId.A));
        }

        // ---- §5.1 grip multiplier ----

        [Test]
        public void UnfavorableGripBigThrow_CostsEight()
        {
            _s.PayAttack(FighterId.A, BigThrow(), -1);   // 5 × 1.5 = 7.5 → 8
            Assert.AreEqual(92, _s.Get(FighterId.A));
        }

        [Test]
        public void WorstGripBigThrow_CostsTen()
        {
            _s.PayAttack(FighterId.A, BigThrow(), -2);   // 5 × 2.0
            Assert.AreEqual(90, _s.Get(FighterId.A));
        }

        [Test]
        public void BestGripBigThrow_CostsThree()
        {
            _s.PayAttack(FighterId.A, BigThrow(), +2);   // 5 × 0.6
            Assert.AreEqual(97, _s.Get(FighterId.A));
        }

        [Test]
        public void Counter_CostsTwo_RegardlessOfGrip()
        {
            _s.PayAttack(FighterId.A, Counter(), -2);
            _s.PayAttack(FighterId.B, Counter(), +2);
            Assert.AreEqual(98, _s.Get(FighterId.A));
            Assert.AreEqual(98, _s.Get(FighterId.B));
        }

        [Test]
        public void Poke_IgnoresGripMultiplier()
        {
            _s.PayAttack(FighterId.A, Poke(), -2);
            Assert.AreEqual(99, _s.Get(FighterId.A));
        }

        // ---- patch 1.3: who pays the counter ----

        [Test]
        public void Counter_PaidByCounteringFighter_AfterConfirm()
        {
            // A commits a big throw at neutral grip; B counters it.
            _s.PayAttack(FighterId.A, BigThrow(), 0);    // 5
            _s.PayAttack(FighterId.B, Counter(), 0);     // 2, paid by B
            Assert.AreEqual(95, _s.Get(FighterId.A));
            Assert.AreEqual(98, _s.Get(FighterId.B));
        }

        [Test]
        public void Attacker_KeepsBigThrowCost_WhenCountered()
        {
            _s.PayAttack(FighterId.A, BigThrow(), 0);
            int afterCommit = _s.Get(FighterId.A);
            _s.PayAttack(FighterId.B, Counter(), 0);     // the counter resolves
            Assert.AreEqual(afterCommit, _s.Get(FighterId.A), "countered attacker gets no refund");
        }

        // ---- §5.2 defence & turtle drain ----

        [Test]
        public void DefenseCosts_ByQuality()
        {
            _s.PayDefense(FighterId.A, DefenseQuality.Clean);
            Assert.AreEqual(98, _s.Get(FighterId.A));
            _s.PayDefense(FighterId.A, DefenseQuality.Neutral);
            Assert.AreEqual(94, _s.Get(FighterId.A));
            _s.PayDefense(FighterId.A, DefenseQuality.Pushed);
            Assert.AreEqual(88, _s.Get(FighterId.A));
        }

        /// §5.2 "Clean defense (won the read)" — quality is the read seen from the
        /// defender's side, so there is nothing left to script independently.
        [Test]
        public void DefenseQuality_IsDerivedFromTheEffectiveRead()
        {
            Assert.AreEqual(DefenseQuality.Clean, StaminaSystem.DefenseQualityFor(ReadLevel.GotRead));
            Assert.AreEqual(DefenseQuality.Neutral, StaminaSystem.DefenseQualityFor(ReadLevel.Neutral));
            Assert.AreEqual(DefenseQuality.Pushed, StaminaSystem.DefenseQualityFor(ReadLevel.Read));
            Assert.AreEqual(DefenseQuality.Pushed, StaminaSystem.DefenseQualityFor(ReadLevel.FullRead));
        }

        [Test]
        public void ExtremeDefense_DrainsOnePerTurn()
        {
            _s.DrainExtremeDefense(FighterId.A);
            _s.DrainExtremeDefense(FighterId.A);
            Assert.AreEqual(98, _s.Get(FighterId.A));
        }

        // ---- §5.3 recovery ----

        [Test]
        public void MatteRecovers_ThreeAndClampsAtHundred()
        {
            _s.PayDefense(FighterId.A, DefenseQuality.Clean);  // 98
            _s.RecoverMatte(FighterId.A);
            Assert.AreEqual(100, _s.Get(FighterId.A), "clamped at 100, not 101");
        }

        [Test]
        public void StallRecovers_TwoPerCompletedTenSeconds()
        {
            _s.PayDefense(FighterId.A, DefenseQuality.Pushed);   // 94
            _s.RecoverStall(FighterId.A, 25);                    // 2 completed blocks → +4
            Assert.AreEqual(98, _s.Get(FighterId.A));
        }

        [Test]
        public void StallRecovers_NothingUnderTenSeconds()
        {
            _s.PayDefense(FighterId.A, DefenseQuality.Pushed);
            _s.RecoverStall(FighterId.A, 9);
            Assert.AreEqual(94, _s.Get(FighterId.A));
        }

        // ---- §5.4 mastery ----

        [Test]
        public void Mastery_ReducesCost()
        {
            // 5 × 1.0 × (1 − 0.08×5) = 3.0
            _s.PayAttack(FighterId.A, BigThrow(), 0, masteryLevel: 5);
            Assert.AreEqual(97, _s.Get(FighterId.A));
        }

        // ---- §5.5 floor & lockout ----

        [Test]
        public void Stamina_FloorsAtZero_AndLocksOut()
        {
            for (int i = 0; i < 30; i++) _s.PayAttack(FighterId.A, BigThrow(), -2);
            Assert.AreEqual(0, _s.Get(FighterId.A));
            Assert.IsTrue(_s.IsExhaustedLockout(FighterId.A));
        }

        [Test]
        public void NotLockedOut_WhileAnyStaminaRemains()
        {
            _s.PayAttack(FighterId.A, BigThrow(), -2);
            Assert.IsFalse(_s.IsExhaustedLockout(FighterId.A));
        }
    }
}
