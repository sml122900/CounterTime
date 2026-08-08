namespace CounterTime.Core
{
    /// One fighter's commit for one exchange (§2). Hand takes ONE slot — committing
    /// both hands in a turn is deliberately not offered.
    public readonly struct TurnChoice
    {
        public readonly HandTarget Hand;
        public readonly CoreStance Core;
        public readonly LegAction Legs;

        public TurnChoice(HandTarget hand, CoreStance core, LegAction legs)
        {
            Hand = hand;
            Core = core;
            Legs = legs;
        }

        /// §2 극단적 방어 — hand 회피 + core 수비 + legs 수비.
        public bool IsExtremeDefense =>
            Hand == HandTarget.Evade && Core == CoreStance.Defensive && Legs == LegAction.Defend;

        public bool IsAllNeutral =>
            Hand == HandTarget.None && Core == CoreStance.Neutral && Legs == LegAction.Neutral;
    }
}
