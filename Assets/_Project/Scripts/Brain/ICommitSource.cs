using CounterTime.Core;

namespace CounterTime.Brain
{
    /// <summary>
    /// 한 턴의 커밋(§2: 손 하나 + 코어 + 하체)이 <b>어디서 오는가</b>.
    ///
    /// 대조 모드의 시나리오 노드, 플레이 모드의 A 입력, B 의 습관 브레인이 전부 이걸
    /// 구현한다. 러너는 어느 쪽이 꽂혔는지 이름으로 알지 못한다 — 그게 모드 격리다.
    /// </summary>
    public interface ICommitSource
    {
        /// <summary>
        /// <c>false</c> = 이 턴에 이 출처가 아무것도 선언하지 않았다.
        ///
        /// <b>선언 없음은 중립을 '고른' 것과 다르다.</b> 심판은 선언한 턴만 관찰하고
        /// (<see cref="ExchangeScript.DeclaredA"/>), 선언된 적 없는 선수는 소극을
        /// 누산하지 않는다. 그래서 <c>TurnChoice</c> 를 그냥 반환하지 않고 성립 여부를
        /// 따로 낸다 — 중립을 반환해 버리면 그 구분이 사라진다.
        ///
        /// 거부할 때 <c>choice</c> 는 <see cref="TurnChoice.Neutral"/> 이어야 한다.
        /// <c>default(TurnChoice)</c> 는 enum 0 이 각각 Aggressive·Attack 이라 중립이 아니다.
        /// </summary>
        bool TryNext(out FighterId who, out TurnChoice choice);
    }
}
