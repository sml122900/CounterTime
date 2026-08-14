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
        /// §2 v4.2 — which zone each of MY hands guards when <see cref="Hand"/> is
        /// Evade. Both null unless evading; a plain additive field (default null) so
        /// every pre-v4.2 call site keeps compiling unchanged. Not read anywhere
        /// outside the GripRevisionGate path.
        public readonly DefenseZone? EvadeZoneLeft;
        public readonly DefenseZone? EvadeZoneRight;

        public TurnChoice(HandTarget hand, CoreStance core, LegAction legs,
                          Hand ownHand = CounterTime.Core.Hand.Right,
                          DefenseZone? evadeZoneLeft = null, DefenseZone? evadeZoneRight = null)
        {
            Hand = hand;
            Core = core;
            Legs = legs;
            OwnHand = ownHand;
            EvadeZoneLeft = evadeZoneLeft;
            EvadeZoneRight = evadeZoneRight;
        }

        /// §2 — 이 손이 <b>사건</b>을 일으키는가. 자세는 상태고 손 행동은 사건이다.
        ///
        /// 사건(소매·깃·틀어잡기·등싸잡기·뜯기·잡기 유지)은 선언한 턴에 한 번 일어나고
        /// 그 결과는 GripSystem 이 든다. 원인까지 매 교환 재실행하면 이중 모델링이고,
        /// 그 결과가 "뜯자마자 도로 잡히는" 뜯기 무효화다.
        ///
        /// 잡기 유지가 특히 사건이다 — 유지되면 그 뒤 모든 뜯기가 영구히 Locked 로
        /// 떨어져 뜯기 수싸움이 반대 방향으로 똑같이 죽는다. 뜯기가 오는 그 교환에만
        /// 의미가 있는 반응이다.
        ///
        /// 회피는 상태다. 손으로 뭘 하는 게 아니라 손을 쓰지 않는 자세이고, 극단적 방어가
        /// §6.1 에서 턴당 +3 을 받으려면 유지돼야 한다.
        ///
        /// 시간 비용표(잡기 5 / 기술·방어 3 / 뜯기 3)에 자세 변경 항목이 없다는 것도
        /// 같은 경계다 — 자세만 바꾸는 것은 교환이 아니다.
        public bool HasHandAction =>
            Hand != HandTarget.None && Hand != HandTarget.Evade;

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
