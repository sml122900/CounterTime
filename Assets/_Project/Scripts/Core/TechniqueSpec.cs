namespace CounterTime.Core
{
    /// Plain technique data for ONE exchange. §H step 2 split this from
    /// <see cref="TechniqueDef"/> (the immutable authored roster entry) — this is the
    /// per-exchange runtime instance built from a TechniqueDef (live path) or straight
    /// from parsed node/habit data (scripted and brain paths, unchanged).
    public sealed class TechniqueSpec
    {
        public readonly string Id;
        public readonly ActionCost Cost;
        /// The scripted and habit-brain paths supply an explicit (authored) class here,
        /// same literal values as before this split. The live path leaves this null so
        /// MatchEngine derives it from mastery instead
        /// (TechniqueRegistry.ClassFromMastery) — see MatchEngine.ResolveTechnique,
        /// the one place this gets resolved into a concrete TechniqueClass.
        public readonly TechniqueClass? AuthoredClass;
        /// §4.4 — exempts this technique from the balance-gate BigThrow downgrade to
        /// Sloppy. For 말아업어치기: its whole point is sharing 업어치기's opening
        /// motion, so the opponent cannot see it coming to set up 흔들기 against.
        public readonly bool SurpriseExempt;

        public TechniqueSpec(string id, ActionCost cost, TechniqueClass? authoredClass,
                             bool surpriseExempt = false)
        {
            Id = id;
            Cost = cost;
            AuthoredClass = authoredClass;
            SurpriseExempt = surpriseExempt;
        }
    }
}
