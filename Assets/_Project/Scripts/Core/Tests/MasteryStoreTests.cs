using NUnit.Framework;
using CounterTime.Core;

namespace CounterTime.Core.Tests
{
    /// §H 3.5a — MasteryStore (real backing) vs ZeroMasteryStore (permanent stub, tested
    /// already in TechniqueRegistryTests). Nothing in MatchEngine injects a MasteryStore
    /// instance yet — mastery-earning is §H step 7.
    public class MasteryStoreTests
    {
        [Test]
        public void Fresh_LevelOfAnything_IsZero()
        {
            var store = new MasteryStore();
            Assert.AreEqual(0, store.LevelOf(FighterId.A, "밭다리걸기"));
        }

        [Test]
        public void Set_ThenLevelOf_RoundTrips()
        {
            var store = new MasteryStore();
            store.Set(FighterId.A, "밭다리걸기", 4);
            Assert.AreEqual(4, store.LevelOf(FighterId.A, "밭다리걸기"));
        }

        [Test]
        public void Set_IsPerFighter_DoesNotBleedAcross()
        {
            var store = new MasteryStore();
            store.Set(FighterId.A, "밭다리걸기", 4);
            Assert.AreEqual(0, store.LevelOf(FighterId.B, "밭다리걸기"),
                "같은 기술이어도 파이터가 다르면 독립이다 — 저장 스키마도 A(플레이어)만 채운다");
        }

        [Test]
        public void Set_OverwritesPreviousLevel_ForTheSameFighterAndTechnique()
        {
            var store = new MasteryStore();
            store.Set(FighterId.A, "밭다리걸기", 2);
            store.Set(FighterId.A, "밭다리걸기", 5);
            Assert.AreEqual(5, store.LevelOf(FighterId.A, "밭다리걸기"));
        }
    }
}
