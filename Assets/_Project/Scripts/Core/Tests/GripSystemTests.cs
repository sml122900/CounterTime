using NUnit.Framework;
using CounterTime.Core;

namespace CounterTime.Core.Tests
{
    public class GripSystemTests
    {
        GripSystem _g;

        [SetUp]
        public void SetUp() => _g = new GripSystem();

        // ---- §3.1 scoring ----

        [Test]
        public void NoGrips_IsZeroBothSides()
        {
            Assert.AreEqual(0, _g.GripScore(FighterId.A));
            Assert.AreEqual(0, _g.GripScore(FighterId.B));
        }

        [Test]
        public void OneNormalGrip_IsPlusOneAndMinusOne()
        {
            _g.Grab(FighterId.A, Hand.Left, Hand.Right, GripKind.Normal);
            Assert.AreEqual(1, _g.GripScore(FighterId.A));
            Assert.AreEqual(-1, _g.GripScore(FighterId.B), "perspective flips sign");
        }

        [Test]
        public void SideTotal_CapsAtTwo()
        {
            _g.Grab(FighterId.A, Hand.Left, Hand.Right, GripKind.Dominant);  // 2
            _g.Grab(FighterId.A, Hand.Right, Hand.Left, GripKind.Dominant);  // 2 → capped 2
            Assert.AreEqual(2, _g.GripScore(FighterId.A));
        }

        [Test]
        public void MutualGrips_Cancel()
        {
            _g.Grab(FighterId.A, Hand.Left, Hand.Right, GripKind.Normal);
            _g.Grab(FighterId.B, Hand.Left, Hand.Right, GripKind.Normal);
            Assert.AreEqual(0, _g.GripScore(FighterId.A));
            Assert.AreEqual(0, _g.GripScore(FighterId.B));
        }

        // ---- §3.2 tear ----

        [Test]
        public void GrabbingASleeve_MakesItHeld()
        {
            _g.Grab(FighterId.A, Hand.Left, Hand.Right, GripKind.Normal);
            Assert.AreEqual(GripLock.Held, _g.LockState(FighterId.B, Hand.Right));
        }

        [Test]
        public void TearVsMaintain_Locks()
        {
            _g.Grab(FighterId.A, Hand.Left, Hand.Right, GripKind.Normal);
            var outcome = _g.ResolveTear(FighterId.B, Hand.Right, opponentChoseMaintain: true);
            Assert.AreEqual(TearOutcome.Locked, outcome);
            Assert.AreEqual(GripLock.Locked, _g.LockState(FighterId.B, Hand.Right));
        }

        [Test]
        public void TearVsAnythingElse_Frees_AndDropsTheGrip()
        {
            _g.Grab(FighterId.A, Hand.Left, Hand.Right, GripKind.Normal);
            var outcome = _g.ResolveTear(FighterId.B, Hand.Right, opponentChoseMaintain: false);
            Assert.AreEqual(TearOutcome.Freed, outcome);
            Assert.AreEqual(GripLock.Free, _g.LockState(FighterId.B, Hand.Right));
            Assert.AreEqual(0, _g.GripScore(FighterId.A), "A's hold is gone, not just unlocked");
        }

        [Test]
        public void TearOnLocked_IsImpossible()
        {
            _g.Grab(FighterId.A, Hand.Left, Hand.Right, GripKind.Normal);
            _g.ResolveTear(FighterId.B, Hand.Right, opponentChoseMaintain: true);
            var again = _g.ResolveTear(FighterId.B, Hand.Right, opponentChoseMaintain: false);
            Assert.AreEqual(TearOutcome.Impossible, again);
            Assert.AreEqual(GripLock.Locked, _g.LockState(FighterId.B, Hand.Right));
        }

        // ---- §3.2 lock cap (patch 1.4) ----

        [Test]
        public void EitherHandLocked_CapsEffectiveGripAtMinusOne()
        {
            // B is dominant on the score sheet …
            _g.Grab(FighterId.B, Hand.Left, Hand.None, GripKind.Dominant);
            Assert.AreEqual(2, _g.GripScore(FighterId.B));

            // … but A has locked B's right hand.
            _g.Grab(FighterId.A, Hand.Right, Hand.Right, GripKind.Normal);
            _g.ResolveTear(FighterId.B, Hand.Right, opponentChoseMaintain: true);

            Assert.IsTrue(_g.HasLockedHand(FighterId.B));
            Assert.AreEqual(-1, _g.EffectiveGrip(FighterId.B),
                "a locked hand caps effective grip regardless of raw score");
        }

        [Test]
        public void LockCap_NeverRaisesAWorseScore()
        {
            _g.Grab(FighterId.A, Hand.Left, Hand.Right, GripKind.Dominant);
            _g.ResolveTear(FighterId.B, Hand.Right, opponentChoseMaintain: true);
            Assert.AreEqual(-2, _g.GripScore(FighterId.B));
            Assert.AreEqual(-2, _g.EffectiveGrip(FighterId.B), "min(raw,-1) keeps -2, not -1");
        }

        [Test]
        public void NoLock_LeavesEffectiveGripAlone()
        {
            _g.Grab(FighterId.A, Hand.Left, Hand.None, GripKind.Dominant);
            Assert.AreEqual(2, _g.EffectiveGrip(FighterId.A));
        }

        // ---- §2 conversion gate ----

        [Test]
        public void Conversion_RequiresAnExistingHold()
        {
            Assert.IsFalse(_g.CanUseConversion(FighterId.A));
            _g.Grab(FighterId.A, Hand.Left, Hand.Right, GripKind.Normal);
            Assert.IsTrue(_g.CanUseConversion(FighterId.A));
        }

        // ---- §3.2 matte reset ----

        [Test]
        public void Matte_ResetsEverythingIncludingLocks()
        {
            _g.Grab(FighterId.A, Hand.Left, Hand.Right, GripKind.Dominant);
            _g.ResolveTear(FighterId.B, Hand.Right, opponentChoseMaintain: true);
            Assert.IsTrue(_g.HasLockedHand(FighterId.B));

            _g.OnMatte();

            Assert.IsFalse(_g.HasLockedHand(FighterId.B));
            Assert.AreEqual(GripLock.Free, _g.LockState(FighterId.B, Hand.Right));
            Assert.AreEqual(0, _g.GripScore(FighterId.A));
            Assert.AreEqual(0, _g.EffectiveGrip(FighterId.B));
        }
    }
}
