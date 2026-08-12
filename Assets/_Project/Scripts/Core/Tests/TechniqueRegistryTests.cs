using NUnit.Framework;
using CounterTime.Core;

namespace CounterTime.Core.Tests
{
    /// <summary>§H 2단계 — §B 12기술 레지스트리, ClassFromMastery 경계, IMasteryStore stub.</summary>
    public class TechniqueRegistryTests
    {
        static readonly string[] AllTwelveIds =
        {
            "모두걸기", "발목받치기", "나오는발차기", "발뒤축걸기",
            "밭다리걸기", "허리후리기", "안오금띄기", "어깨 매치기",
            "업어치기", "안뒤축감아치기", "허리 감아치기", "말아업어치기",
        };

        [Test]
        public void Get_ReturnsAllTwelveRosterTechniques()
        {
            foreach (var id in AllTwelveIds)
                Assert.IsNotNull(TechniqueRegistry.Get(id), $"{id} 가 레지스트리에 없다");
        }

        [Test]
        public void Get_UnknownId_ReturnsNull()
        {
            Assert.IsNull(TechniqueRegistry.Get("존재하지 않는 기술"));
            Assert.IsNull(TechniqueRegistry.Get(null));
        }

        [Test]
        public void FourFeintFootsweeps_AreClassifiedPoke()
        {
            foreach (var id in new[] { "모두걸기", "발목받치기", "나오는발차기", "발뒤축걸기" })
            {
                var def = TechniqueRegistry.Get(id);
                Assert.AreEqual(AlgoClass.FeintFootsweep, def.Algo, id);
                Assert.AreEqual(ActionCost.Poke, def.BaseCost, id);
            }
        }

        [Test]
        public void EightBigThrows_AreClassifiedBigThrow()
        {
            foreach (var id in new[] { "밭다리걸기", "허리후리기", "안오금띄기", "어깨 매치기",
                                        "업어치기", "안뒤축감아치기", "허리 감아치기", "말아업어치기" })
            {
                var def = TechniqueRegistry.Get(id);
                Assert.AreEqual(AlgoClass.BigThrow, def.Algo, id);
                Assert.AreEqual(ActionCost.BigThrow, def.BaseCost, id);
            }
        }

        [Test]
        public void OnlyMalaupeochigi_IsSurpriseExempt()
        {
            foreach (var id in AllTwelveIds)
            {
                var def = TechniqueRegistry.Get(id);
                Assert.AreEqual(id == "말아업어치기", def.SurpriseExempt, id);
            }
        }

        // ---- ClassFromMastery boundaries (§F: 0–1 Sloppy / 2–3 Standard / 4–5 Signature) ----

        [TestCase(0, TechniqueClass.Sloppy)]
        [TestCase(1, TechniqueClass.Sloppy)]
        [TestCase(2, TechniqueClass.Standard)]
        [TestCase(3, TechniqueClass.Standard)]
        [TestCase(4, TechniqueClass.Signature)]
        [TestCase(5, TechniqueClass.Signature)]
        public void ClassFromMastery_FollowsConfiguredBoundaries(int level, TechniqueClass expected)
        {
            var cfg = new MatchRulesConfig();
            Assert.AreEqual(expected, TechniqueRegistry.ClassFromMastery(level, cfg));
        }

        [Test]
        public void ZeroMasteryStore_AlwaysReturnsZero()
        {
            IMasteryStore store = ZeroMasteryStore.Instance;
            Assert.AreEqual(0, store.LevelOf(FighterId.A, "밭다리걸기"));
            store.Set(FighterId.A, "밭다리걸기", 5);
            Assert.AreEqual(0, store.LevelOf(FighterId.A, "밭다리걸기"), "Set 이후에도 항상 0");
        }
    }
}
