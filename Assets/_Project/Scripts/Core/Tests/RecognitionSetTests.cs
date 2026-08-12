using NUnit.Framework;
using CounterTime.Core;

namespace CounterTime.Core.Tests
{
    /// §H 3.5a — pure container tests. Nothing in Core/Brain reads/writes a
    /// RecognitionSet during a match (§C.1's predictive-counter feature is §H step 5).
    public class RecognitionSetTests
    {
        [Test]
        public void Fresh_KnowsNothing_AndSnapshotIsEmpty()
        {
            var set = new RecognitionSet();
            Assert.IsFalse(set.Knows("밭다리걸기"));
            Assert.IsEmpty(set.Snapshot());
        }

        [Test]
        public void Add_MakesItKnown_OnlyThatOne()
        {
            var set = new RecognitionSet();
            set.Add("밭다리걸기");
            Assert.IsTrue(set.Knows("밭다리걸기"));
            Assert.IsFalse(set.Knows("업어치기"));
        }

        [Test]
        public void Add_IsIdempotent()
        {
            var set = new RecognitionSet();
            set.Add("밭다리걸기");
            set.Add("밭다리걸기");
            Assert.AreEqual(1, set.Snapshot().Count, "같은 기술을 두 번 Add 해도 하나로 남아야 한다");
        }

        [Test]
        public void Snapshot_ListsEverythingAdded()
        {
            var set = new RecognitionSet();
            set.Add("밭다리걸기");
            set.Add("업어치기");
            CollectionAssert.AreEquivalent(new[] { "밭다리걸기", "업어치기" }, set.Snapshot());
        }
    }
}
