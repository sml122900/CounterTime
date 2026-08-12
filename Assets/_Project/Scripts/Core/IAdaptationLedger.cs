using System.Collections.Generic;

namespace CounterTime.Core
{
    /// §C.2/§F persistence seam (CORE_DESIGN_MatchEngine.md's v4.0 revision) — B's
    /// adaptation to A's technique usage, seeded from saved counts at match start and
    /// extracted for persistence at match end.
    public interface IAdaptationLedger
    {
        int ExposureOf(string techId);
        void Seed(IReadOnlyDictionary<string, int> saved);
        IReadOnlyDictionary<string, int> Snapshot();
    }

    /// §H step 3.5a — real (not permanently-zero) backing. Nothing in Core/Brain calls
    /// this yet — §C.2's actual per-match observation-counting logic is §H step 6, so an
    /// instance just sits empty unless the Game-side save layer seeds it. Distinct from
    /// Brain.ObservationLedger (match-lifetime, unrelated) — this is the layer above it
    /// (CORE_DESIGN: "seed and extract from a layer above it, not replace it").
    public sealed class AdaptationLedger : IAdaptationLedger
    {
        readonly Dictionary<string, int> _exposure = new Dictionary<string, int>();

        public int ExposureOf(string techId) => _exposure.TryGetValue(techId, out var v) ? v : 0;

        /// Replaces the ledger's contents — not a merge. A fresh match seeds once from
        /// whatever was saved for that opponent; there is no "add to what's already here".
        public void Seed(IReadOnlyDictionary<string, int> saved)
        {
            _exposure.Clear();
            if (saved == null) return;
            foreach (var kv in saved) _exposure[kv.Key] = kv.Value;
        }

        /// Independent copy — mutating the ledger after taking a snapshot must not
        /// change values the caller already read out.
        public IReadOnlyDictionary<string, int> Snapshot() => new Dictionary<string, int>(_exposure);
    }
}
