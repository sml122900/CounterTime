namespace CounterTime.Brain
{
    /// <summary>
    /// §4 사다리의 <b>1단(하드 게이트)</b>과 <b>3단(폴백 가중 무작위)</b>이 보는 면.
    /// 브레인 주인 한 명의 시점이다.
    ///
    /// <b>2단 습관 조건은 이 면을 절대 보지 않는다.</b> 습관은 <see cref="IHabitContext"/>
    /// 넷만 본다 — 그 경계가 이 파일이 따로 있는 이유 전부다. 지도 <i>개수</i>와 시계가
    /// 습관 조건에 들어가는 순간 습관이 상태 평가형 AI 로 흐르고, 그건 §2 에서 배제된
    /// 안이다. 타입으로도 막혀 있다: <c>HabitDefinition.Condition</c> 의 인자는
    /// <see cref="IHabitContext"/> 다.
    ///
    /// 게이트는 사다리 <b>전체</b>에 걸린다 — 습관도 게이트를 못 뚫는다. 게이트가 여기
    /// 사는 것은 게이트를 <i>거는</i> 쪽(<c>HabitBrainCommitSource</c>)이 읽기 때문이지
    /// 폴백 전용이라서가 아니다.
    ///
    /// <b>멤버 추가는 넣지 말고 보고할 것.</b> 지금 여섯은 각각 아래 용도가 있어서 있다.
    /// 말아업어치기의 '경기당 1회' 플래그는 여기가 아니라 경기당 상태
    /// (<see cref="HabitState"/> 계열)다 — 그건 상황이 아니라 이력이다.
    /// </summary>
    public interface IBrainContext
    {
        /// 폴백 가중 — §3 유효 잡기, −2 … +2.
        int OwnEffectiveGrip { get; }
        /// 폴백 가중.
        int OwnStamina { get; }
        /// 폴백 가중.
        int OpponentStamina { get; }
        /// §H 3.5b 후속(밸런스 게이트 존중, 2026-08-13 밤 데이터 수집 §6 항목1) —
        /// <see cref="SetupPrecedingCommitSource"/> 가 큰기술을 실제로 내보내도 되는지
        /// 판단할 때만 쓴다. §4.4 그대로 <c>MatchEngine.Balance.Get</c> 을 상대(방어자)
        /// 기준으로 읽는다 — 파생 없음.
        int OpponentBalance { get; }
        /// 폴백 가중 — 받은 지도 <b>개수</b>. 습관(H2)이 보는 것은 개수가 아니라 최근성이다.
        int OwnShidoCount { get; }
        /// 폴백 가중 — 남은 정규 시간(초).
        int SecondsRemaining { get; }

        /// 하드 게이트 — §5.5 체력 0 락아웃. 참이면 큰기술을 낼 수 없다.
        bool OwnStaminaLockout { get; }
        /// 하드 게이트 — §3.2 어느 한 손이라도 Locked 인가.
        bool OwnHasLockedHand { get; }
    }
}
