using CounterTime.Core;

namespace CounterTime.Brain
{
    /// <summary>
    /// 한 교환의 <see cref="ReadLevel"/> 이 나오는 <b>유일한</b> 지점.
    ///
    /// 다른 어디서도 읽기를 다시 파생하지 않는다. 인라인 재파생은 이 리포가 이미
    /// 한 번 데인 실패다 — <c>IsDefensivePosture</c> 가 정확히 그래서 단일화됐다.
    ///
    /// <b>계약: 여기서 나오는 값은 BASE 읽기다 — §4.2 카운터 준비 감쇄 <i>이전</i>.</b>
    ///
    /// 사다리를 내리는 것은 Core 가 소유한다(<c>MatchEngine</c> 이 방어자의
    /// <c>LegAction.CounterReady</c> 를 보고 <c>JudgementCalculator.StepDownRead</c> 를
    /// 부른다). 리졸버가 이미 깎은 값을 내놓으면 Core 가 한 번 더 깎아 <b>두 칸</b>
    /// 내려간다 — +2 가 0 이 아니라 −3 이 되고, 에러는 안 난다.
    ///
    /// 대조 모드에서는 노드의 <c>[판정식]</c> 값이 곧 BASE 다. 3단계
    /// <c>LiveReadResolver</c> 도 같은 계약을 진다: §2 습관 관측과 §3 성향 창이 내는
    /// 값은 감쇄 전 값이다.
    /// </summary>
    public interface IReadResolver
    {
        /// <param name="attacker">이 교환에서 기술을 내는 쪽. 읽기는 이 선수의 것이다.</param>
        ReadLevel For(FighterId attacker, TurnChoice a, TurnChoice b);
    }
}
