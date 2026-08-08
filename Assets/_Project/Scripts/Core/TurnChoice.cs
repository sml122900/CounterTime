namespace CounterTime.Core
{
    /// One fighter's commit for one exchange (§2). Hand takes ONE slot — committing
    /// both hands in a turn is deliberately not offered.
    public readonly struct TurnChoice
    {
        public readonly HandTarget Hand;
        public readonly CoreStance Core;
        public readonly LegAction Legs;
        /// Which of MY hands fills the single hand slot. §2 names only the target,
        /// but GripSystem needs the slot to place the hold and to address a tear.
        public readonly Hand OwnHand;

        public TurnChoice(HandTarget hand, CoreStance core, LegAction legs,
                          Hand ownHand = CounterTime.Core.Hand.Right)
        {
            Hand = hand;
            Core = core;
            Legs = legs;
            OwnHand = ownHand;
        }

        /// §2 극단적 방어 — hand 회피 + core 수비 + legs 수비.
        public bool IsExtremeDefense =>
            Hand == HandTarget.Evade && Core == CoreStance.Defensive && Legs == LegAction.Defend;

        public bool IsAllNeutral =>
            Hand == HandTarget.None && Core == CoreStance.Neutral && Legs == LegAction.Neutral;
    }
}
