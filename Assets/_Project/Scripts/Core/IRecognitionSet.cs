using System.Collections.Generic;

namespace CounterTime.Core
{
    /// §C.1/§F persistence seam — A's recognition list (predictive-counter candidates),
    /// added to during a match from observation, persisted per opponent.
    public interface IRecognitionSet
    {
        bool Knows(string techId);
        void Add(string techId);
        IReadOnlyCollection<string> Snapshot();
    }

    /// §H step 3.5a — real (not permanently-empty) backing. Nothing in Core/Brain calls
    /// this yet — §C.1's actual predictive-counter feature is §H step 5, so an instance
    /// just sits empty unless the Game-side save layer seeds it (via repeated `Add`;
    /// unlike IAdaptationLedger there is no bulk `Seed` — the design only specifies
    /// Knows/Add/Snapshot, and Add is idempotent so seeding one id at a time is enough).
    public sealed class RecognitionSet : IRecognitionSet
    {
        readonly HashSet<string> _known = new HashSet<string>();

        public bool Knows(string techId) => _known.Contains(techId);
        public void Add(string techId) => _known.Add(techId);
        public IReadOnlyCollection<string> Snapshot() => new List<string>(_known);
    }
}
