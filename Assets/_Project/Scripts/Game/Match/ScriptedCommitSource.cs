using CounterTime.Brain;
using CounterTime.Core;
using CounterTime.Core.Scenario;

namespace CounterTime.Game.Match
{
    /// <summary>
    /// 대조 모드(스크립트 경로)의 커밋 출처 — 노드가 밝힌 선언을 그대로 낸다.
    ///
    /// 파싱은 여기서 하지 않는다. <see cref="ScenarioTurnChoices"/> 한 곳뿐이고 이 클래스는
    /// 그 앞에 인터페이스를 씌우기만 한다.
    ///
    /// Game 에 있는 이유: 대상인 <see cref="ScenarioTurnChoices"/> 가 Game 에 있고,
    /// 참조 방향이 Game → Brain 이라 Brain 이 이쪽을 부를 수 없다. 순수 C# 이어야 하는
    /// 것은 <b>브레인 구현</b>(2·3단계)이고, 그건 Brain 에 들어간다.
    /// </summary>
    public sealed class ScriptedCommitSource : ICommitSource
    {
        readonly ScenarioNode _node;

        public ScriptedCommitSource(ScenarioNode node) => _node = node;

        public bool TryNext(out FighterId who, out TurnChoice choice) =>
            ScenarioTurnChoices.TryParse(_node, out who, out choice);
    }
}
