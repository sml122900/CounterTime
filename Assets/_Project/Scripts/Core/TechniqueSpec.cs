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
        /// §4.4 [초안] — exempts this technique from the setup-less (흔들기 없는) BigThrow
        /// downgrade to Sloppy. For 말아업어치기: its whole point is sharing 업어치기's
        /// opening motion, so the opponent cannot see it coming to set up 흔들기 against.
        public readonly bool SurpriseExempt;

        public TechniqueSpec(string id, ActionCost cost, TechniqueClass techClass,
                             int masteryLevel = 0, bool surpriseExempt = false)
        {
            Id = id;
            Cost = cost;
            Class = techClass;
            MasteryLevel = masteryLevel;
            SurpriseExempt = surpriseExempt;
        }
    }
}
