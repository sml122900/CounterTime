using CounterTime.Core.Scenario;
using UnityEngine;

namespace CounterTime.Game.Scenario
{
    /// <summary>
    /// ScenarioRunner.NodeEntered 를 받아 두 파이터의 Animator 를 전환한다.
    /// Core 는 모션을 모른다 — 노드 진입 이벤트만 던지고, 해석은 전부 여기서 한다.
    /// </summary>
    public sealed class ScenarioMotionBinder : MonoBehaviour
    {
        [SerializeField] ScenarioLoader loader;
        [SerializeField] NodeMotionMap motionMap;
        [SerializeField] Animator fighterA;
        [SerializeField] Animator fighterB;
        [SerializeField] float crossFadeSeconds = 0.25f;

        ScenarioRunner _runner;

        void Start()
        {
            _runner = loader.Runner;
            if (_runner == null) return;

            _runner.NodeEntered += OnNodeEntered;
            // UI 쪽 Start 가 먼저 돌아 첫 노드 진입을 놓쳤을 수 있다 — 현재 노드로 한 번 맞춰준다.
            if (_runner.Current != null) OnNodeEntered(_runner.Current);
        }

        void OnDestroy()
        {
            if (_runner != null) _runner.NodeEntered -= OnNodeEntered;
        }

        void OnNodeEntered(ScenarioNode node)
        {
            motionMap.Resolve(node.id, out var stateA, out var stateB);
            Play(fighterA, stateA, node.id);
            Play(fighterB, stateB, node.id);
        }

        void Play(Animator animator, string stateName, string nodeId)
        {
            int hash = Animator.StringToHash(stateName);
            if (!animator.HasState(0, hash))
            {
                Debug.LogWarning($"{animator.name}: '{stateName}' 상태가 없다 (노드 {nodeId})", animator);
                return;
            }
            animator.CrossFade(hash, crossFadeSeconds, 0);
        }
    }
}
