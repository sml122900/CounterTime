using System;
using System.Collections.Generic;

namespace CounterTime.Game.Save
{
    /// META_DESIGN.md §M1 — the single save file's schema, verbatim. JsonUtility can't
    /// serialize Dictionary directly (a well-known limitation), so the design doc's
    /// "techId → level" style maps become List&lt;Entry&gt; on disk instead — still the
    /// same data, just Unity-serializable. Every field is a plain public field (not a
    /// property) because JsonUtility only sees those.
    [Serializable]
    public sealed class SaveData
    {
        public string playerName = "";
        public List<MasteryEntry> mastery = new List<MasteryEntry>();
        public List<OpponentSaveEntry> opponents = new List<OpponentSaveEntry>();
        public CareerSaveData career = new CareerSaveData();
    }

    /// §F — player's per-technique mastery (§M1: "mastery : { techId → level }").
    [Serializable]
    public sealed class MasteryEntry
    {
        public string techId;
        public int level;
    }

    /// §M1 opponents[] — one fought-or-known fighter's persistent record.
    [Serializable]
    public sealed class OpponentSaveEntry
    {
        public string id;
        public string name;
        /// §C — how much this fighter knows about ME (0..N ⚙).
        public int scoutingLevel;
        /// §C.2 — accumulated from direct matches (techId → exposureCount).
        public List<AdaptationEntry> adaptation = new List<AdaptationEntry>();
        /// §C.1 — techniques I've observed and 'recognize' on this fighter.
        public List<string> knownByMe = new List<string>();
    }

    [Serializable]
    public sealed class AdaptationEntry
    {
        public string techId;
        public int count;
    }

    [Serializable]
    public sealed class CareerSaveData
    {
        public PracticeCupResult practiceCupResult = PracticeCupResult.None;
    }

    /// META_DESIGN §M1/§M3 — `None` doubles as the design doc's own "| null" case ("no
    /// result yet"). A plain enum default covers that without a nullable wrapper
    /// JsonUtility would round-trip awkwardly.
    [Serializable]
    public enum PracticeCupResult { None, Lost, Bronze, Silver, Gold }
}
