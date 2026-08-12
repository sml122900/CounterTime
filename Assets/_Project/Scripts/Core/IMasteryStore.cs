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
    /// TechniqueSpec.MasteryLevel field is behavior-preserving.
    public sealed class ZeroMasteryStore : IMasteryStore
    {
        public static readonly ZeroMasteryStore Instance = new ZeroMasteryStore();

        public int LevelOf(FighterId who, string techId) => 0;
        public void Set(FighterId who, string techId, int level) { /* no-op — always reports 0 regardless */ }
    }
}
