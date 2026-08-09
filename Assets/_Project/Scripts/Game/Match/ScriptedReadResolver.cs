using CounterTime.Brain;
using CounterTime.Core;
using CounterTime.Core.Scenario;

namespace CounterTime.Game.Match
{
    /// <summary>
    /// 대조 모드(스크립트 경로)의 읽기 출처 — 노드의 <c>[판정식]</c> 줄이 정한 값.
    ///
    /// 그 줄을 실제로 파싱하는 것은 <see cref="ScenarioExchangeScript"/> 다. 여기서 정규식을
    /// 다시 쓰지 않는다 — 읽기 파생이 두 곳이 되는 순간 한 곳만 고치는 사고가 난다.
    ///
    /// 한 노드가 여러 교환으로 전개돼도(m14 의 4교환) 읽기는 노드 하나가 정하므로 값이
    /// 같다. 그래서 진입할 때 한 번 뽑아 든다.
    ///
    /// <b>지금 조립기를 한 번 더 돌린다.</b> 조립기가 기술과 읽기를 한 번에 뽑고 있어서
    /// 읽기만 따로 물을 창구가 없기 때문이다. 1.5 단계에서 <c>IExchangeAssembler</c> 를
    /// 뽑으면 그 창구가 생기고 이 중복은 사라진다. 값이 달라지지는 않는다 — 같은 노드를
    /// 같은 파서에 넣는다.
    /// </summary>
    public sealed class ScriptedReadResolver : IReadResolver
    {
        readonly ReadLevel _read;

        public ScriptedReadResolver(ScenarioExchangeScript script, ScenarioNode node)
        {
            var assembled = script.For(node);
            _read = assembled.Count > 0 ? assembled[0].Read : ReadLevel.Neutral;
        }

        /// 노드가 이미 값을 정해뒀다 — 대조 모드는 커밋에서 읽기를 유도하지 않는다.
        /// 그 유도가 3단계 <c>LiveReadResolver</c> 의 일이다.
        public ReadLevel For(FighterId attacker, TurnChoice a, TurnChoice b) => _read;
    }
}
