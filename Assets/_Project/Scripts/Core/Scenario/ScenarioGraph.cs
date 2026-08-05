using System;
using System.Collections.Generic;

namespace CounterTime.Core.Scenario
{
    /// <summary>노드 사전 + from→to 인접 목록. 자식 순서는 JSON 의 edges 순서를 따른다.</summary>
    public sealed class ScenarioGraph
    {
        static readonly ScenarioNode[] NoChildren = new ScenarioNode[0];

        readonly Dictionary<string, ScenarioNode> _nodes = new Dictionary<string, ScenarioNode>();
        readonly Dictionary<string, List<ScenarioNode>> _children = new Dictionary<string, List<ScenarioNode>>();

        public ScenarioGraph(ScenarioData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            if (data.nodes != null)
            {
                foreach (var node in data.nodes)
                {
                    if (node == null || string.IsNullOrEmpty(node.id)) continue;
                    _nodes[node.id] = node;
                }
            }

            if (data.edges != null)
            {
                foreach (var edge in data.edges)
                {
                    if (edge == null) continue;
                    if (!_nodes.ContainsKey(edge.from) || !_nodes.TryGetValue(edge.to, out var child)) continue;

                    if (!_children.TryGetValue(edge.from, out var list))
                    {
                        list = new List<ScenarioNode>();
                        _children[edge.from] = list;
                    }
                    list.Add(child);
                }
            }
        }

        public IReadOnlyDictionary<string, ScenarioNode> Nodes => _nodes;

        public bool TryGetNode(string id, out ScenarioNode node) => _nodes.TryGetValue(id ?? string.Empty, out node);

        public ScenarioNode GetNode(string id)
        {
            if (!_nodes.TryGetValue(id ?? string.Empty, out var node))
                throw new KeyNotFoundException("시나리오에 없는 노드 id: " + id);
            return node;
        }

        public IReadOnlyList<ScenarioNode> GetChildren(string id)
        {
            return _children.TryGetValue(id ?? string.Empty, out var list)
                ? (IReadOnlyList<ScenarioNode>)list
                : NoChildren;
        }
    }
}
