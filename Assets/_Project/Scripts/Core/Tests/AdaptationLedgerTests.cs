using System.Collections.Generic;
using NUnit.Framework;
using CounterTime.Core;

namespace CounterTime.Core.Tests
{
    /// §H 3.5a — pure container tests. Nothing in Core/Brain reads/writes an
    /// AdaptationLedger during a match (§C.2's actual observation logic is §H step 6) —
    /// these only prove the seam itself round-trips correctly.
    public class AdaptationLedgerTests
    {
        [Test]
        public void Fresh_ExposureOfAnything_IsZero()
        {
            var ledger = new AdaptationLedger();
            Assert.AreEqual(0, ledger.ExposureOf("밭다리걸기"));
        }

        [Test]
        public void Seed_PopulatesExposureOf()
        {
            var ledger = new AdaptationLedger();
            ledger.Seed(new Dictionary<string, int> { ["밭다리걸기"] = 3, ["업어치기"] = 1 });

            Assert.AreEqual(3, ledger.ExposureOf("밭다리걸기"));
            Assert.AreEqual(1, ledger.ExposureOf("업어치기"));
            Assert.AreEqual(0, ledger.ExposureOf("허리후리기"), "안 심은 기술은 여전히 0");
        }

        [Test]
        public void Seed_WithNull_LeavesLedgerEmpty()
        {
            var ledger = new AdaptationLedger();
            ledger.Seed(null);
            Assert.AreEqual(0, ledger.ExposureOf("밭다리걸기"));
        }

        [Test]
        public void Seed_ReplacesPreviousContents_DoesNotMerge()
        {
            var ledger = new AdaptationLedger();
            ledger.Seed(new Dictionary<string, int> { ["밭다리걸기"] = 3 });
            ledger.Seed(new Dictionary<string, int> { ["업어치기"] = 1 });

            Assert.AreEqual(0, ledger.ExposureOf("밭다리걸기"), "두 번째 Seed 가 첫 번째를 덮어써야 한다 — 누적 아님");
            Assert.AreEqual(1, ledger.ExposureOf("업어치기"));
        }

        [Test]
        public void Snapshot_IsAnIndependentCopy()
        {
            var ledger = new AdaptationLedger();
            ledger.Seed(new Dictionary<string, int> { ["밭다리걸기"] = 3 });

            var snapshot = ledger.Snapshot();
            Assert.AreEqual(3, snapshot["밭다리걸기"]);

            ledger.Seed(new Dictionary<string, int> { ["업어치기"] = 1 });
            Assert.AreEqual(3, snapshot["밭다리걸기"], "먼저 뽑은 스냅샷은 그 뒤 Seed 에 안 흔들려야 한다");
        }
    }
}
