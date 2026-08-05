using System;
using UnityEngine;

namespace CounterTime.Game.Scenario
{
    /// <summary>
    /// 시나리오 노드 → 두 파이터의 Animator 상태 이름 매핑.
    /// 매핑이 없거나 칸이 비어 있으면 <see cref="DefaultState"/> 로 떨어진다.
    /// 모션은 표현 계층의 개념이므로 이 타입은 Core 에 두지 않는다.
    /// </summary>
    [CreateAssetMenu(menuName = "CounterTime/Node Motion Map", fileName = "NodeMotionMap")]
    public sealed class NodeMotionMap : ScriptableObject
    {
        public const string DefaultState = "Fight Idle";

        [Serializable]
        public struct Entry
        {
            public string nodeId;
            public string stateA;
            public string stateB;
        }

        [SerializeField] Entry[] entries = Array.Empty<Entry>();

        public int Count => entries.Length;

        public void Resolve(string nodeId, out string stateA, out string stateB)
        {
            foreach (var e in entries)
            {
                if (e.nodeId != nodeId) continue;
                stateA = string.IsNullOrWhiteSpace(e.stateA) ? DefaultState : e.stateA;
                stateB = string.IsNullOrWhiteSpace(e.stateB) ? DefaultState : e.stateB;
                return;
            }
            stateA = DefaultState;
            stateB = DefaultState;
        }
    }
}
