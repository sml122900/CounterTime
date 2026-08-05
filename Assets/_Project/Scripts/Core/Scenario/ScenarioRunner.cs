using System;
using System.Collections.Generic;

namespace CounterTime.Core.Scenario
{
    /// <summary>
    /// 탐색 모드 러너. 현재 노드를 들고 있고 자식 노드로만 이동한다.
    /// 노드에 들어갈 때마다 <see cref="NodeEntered"/> 를 쏜다 — 나중에 모션·연출을 여기에 건다.
    /// </summary>
    public sealed class ScenarioRunner
    {
        /// <summary>n0 은 보드 범례라 게임에서 쓰지 않는다. 경기는 n1 에서 시작한다.</summary>
        public const string StartNodeId = "n1";

        public ScenarioRunner(ScenarioGraph graph)
        {
            Graph = graph ?? throw new ArgumentNullException(nameof(graph));
        }

        public ScenarioGraph Graph { get; }

        public ScenarioNode Current { get; private set; }

        public event Action<ScenarioNode> NodeEntered;

        /// <summary>자식이 없으면 종단 노드.</summary>
        public bool IsTerminal => Current != null && Graph.GetChildren(Current.id).Count == 0;

        public IReadOnlyList<ScenarioNode> Choices =>
            Current != null ? Graph.GetChildren(Current.id) : new ScenarioNode[0];

        /// <summary>시작 노드로 진입한다. 구독자가 붙은 뒤에 부를 것.</summary>
        public void Restart() => MoveTo(StartNodeId);

        public void MoveTo(string nodeId)
        {
            Current = Graph.GetNode(nodeId);
            NodeEntered?.Invoke(Current);
        }
    }
}
