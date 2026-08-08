namespace CounterTime.Core
{
    /// Plain technique data. The ScriptableObject wrapper lives in CounterTime.Game.
    /// MasteryLevel rides on the spec because §5.4 makes mastery per-technique and
    /// StaminaSystem.PayAttack takes no separate mastery parameter.
    public sealed class TechniqueSpec
    {
        public readonly string Id;
        public readonly ActionCost Cost;
        public readonly TechniqueClass Class;
        public readonly int MasteryLevel;   // §5.4, 0..5

        public TechniqueSpec(string id, ActionCost cost, TechniqueClass techClass, int masteryLevel = 0)
        {
            Id = id;
            Cost = cost;
            Class = techClass;
            MasteryLevel = masteryLevel;
        }
    }
}
