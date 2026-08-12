namespace CounterTime.Core
{
    /// §B — immutable authored data for one technique in the roster. Deliberately does
    /// NOT carry TechniqueClass or mastery level: class is derived per exchange
    /// (TechniqueRegistry.ClassFromMastery) and mastery is a (fighter, technique) pair's
    /// state (IMasteryStore), neither of which belongs to the technique itself.
    public sealed class TechniqueDef
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly AlgoClass Algo;
        public readonly JudoCategory Category;
        public readonly ActionCost BaseCost;
        /// §4.4 — exempts this technique from the balance-gate BigThrow downgrade.
        public readonly bool SurpriseExempt;

        public TechniqueDef(string id, string displayName, AlgoClass algo, JudoCategory category,
                            ActionCost baseCost, bool surpriseExempt = false)
        {
            Id = id;
            DisplayName = displayName;
            Algo = algo;
            Category = category;
            BaseCost = baseCost;
            SurpriseExempt = surpriseExempt;
        }
    }
}
