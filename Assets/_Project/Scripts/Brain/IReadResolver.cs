using CounterTime.Core;

namespace CounterTime.Brain
{
    /// <summary>
    /// 한 교환의 <see cref="ReadLevel"/> 이 나오는 <b>유일한</b> 지점.
    ///
    /// 다른 어디서도 읽기를 다시 파생하지 않는다. 인라인 재파생은 이 리포가 이미
    /// 한 번 데인 실패다 — <c>IsDefensivePosture</c> 가 정확히 그래서 단일화됐다.
    ///
    /// <b>§4.2 카운터 준비 step-down 을 여기서 선적용하지 않는다.</b> 리졸버는 기저
    /// 읽기만 낸다. 사다리를 한 칸 내리는 것은 Core 의 소유다 — 양쪽에서 하면 두 칸
    /// 내려가고, 그건 조용히 틀리는 종류다.
    /// </summary>
    public interface IReadResolver
    {
        /// <param name="attacker">이 교환에서 기술을 내는 쪽. 읽기는 이 선수의 것이다.</param>
        ReadLevel For(FighterId attacker, TurnChoice a, TurnChoice b);
    }
}
