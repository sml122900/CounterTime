using System.Collections.Generic;

namespace CounterTime.Core
{
    /// §F persistence seam (CORE_DESIGN_MatchEngine.md's v4.0 revision) — (fighter,
    /// technique) mastery level. Game injects the real backend once the save layer
    /// (§H step 3.5) exists; until then everything reads 0.
    public interface IMasteryStore
    {
        int LevelOf(FighterId who, string techId);
        void Set(FighterId who, string techId, int level);
    }

    /// §H step 2 stub — always 0, never persists. Matches current production exactly:
    /// nothing feeds mastery a nonzero value yet (measured,
    /// DUMP_technique-structure.md §2.5), so wiring this in place of the old
    /// TechniqueSpec.MasteryLevel field is behavior-preserving. THIS remains the default
    /// MatchEngine injects when nothing else is provided — real mastery growth is §H
    /// step 7, not this step, and nothing here changes that.
    public sealed class ZeroMasteryStore : IMasteryStore
    {
        public static readonly ZeroMasteryStore Instance = new ZeroMasteryStore();

        public int LevelOf(FighterId who, string techId) => 0;
        public void Set(FighterId who, string techId, int level) { /* no-op — always reports 0 regardless */ }
    }

    /// §H step 3.5a — real (fighter, technique) mastery backing, in contrast to
    /// ZeroMasteryStore's permanent no-op. META_DESIGN.md §M1: "IMasteryStore's (step 2
    /// stub) real backend gets filled in here." Nothing wires an instance of this into
    /// MatchEngine yet (mastery-EARNING logic is §H step 7) — this exists so the save
    /// layer has something real to seed from `SaveData.mastery` and extract back to it.
    /// Keyed on (FighterId, techId): the save schema itself only ever populates the
    /// player's (FighterId.A's) entries — B has no persistent career in this scope — but
    /// the store stays generic because the interface signature already is.
    public sealed class MasteryStore : IMasteryStore
    {
        readonly Dictionary<(FighterId, string), int> _levels = new Dictionary<(FighterId, string), int>();

        public int LevelOf(FighterId who, string techId) =>
            _levels.TryGetValue((who, techId), out var v) ? v : 0;

        public void Set(FighterId who, string techId, int level) => _levels[(who, techId)] = level;
    }
}
