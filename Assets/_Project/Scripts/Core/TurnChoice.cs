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

        /// §2 — 이 손이 실제로 무언가를 하는가. 시간 비용표에는 잡기 5 / 기술·방어 3 /
        /// 뜯기 3 만 있고 <b>자세 변경 항목이 없다</b>: 자세만 바꾸는 것은 교환이 아니다.
        /// 회피와 잡기 유지는 손을 쓰지만 아무것도 일으키지 않으므로 행동이 아니다.
        public bool HasHandAction =>
            Hand != HandTarget.None && Hand != HandTarget.Evade && Hand != HandTarget.MaintainGrip;

        /// §2 극단적 방어 — hand 회피 + core 수비 + legs 수비.
        public bool IsExtremeDefense =>
            Hand == HandTarget.Evade && Core == CoreStance.Defensive && Legs == LegAction.Defend;

        /// default(TurnChoice) 는 중립이 아니다 — CoreStance.Aggressive 와 LegAction.Attack
        /// 이 각각 enum 0 이라서 '공격적·공격' 이 나온다. enum 순서는 값에 의존하는 코드가
        /// 있어 바꾸지 않는다. 중립이 필요하면 반드시 이 값을 쓸 것.
        public static readonly TurnChoice Neutral =
            new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.Neutral);

        public bool IsAllNeutral =>
            Hand == HandTarget.None && Core == CoreStance.Neutral && Legs == LegAction.Neutral;
    }
}
