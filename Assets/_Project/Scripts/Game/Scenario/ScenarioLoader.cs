using CounterTime.Core.Scenario;
using UnityEngine;

namespace CounterTime.Game.Scenario
{
    /// <summary>
    /// Assets/_Project/Scenario/ 의 시나리오 JSON 을 TextAsset 으로 받아 Core 의 그래프·러너로 넘긴다.
    /// JsonUtility 는 UnityEngine 소속이라 Core 에 둘 수 없다 — 파싱은 반드시 여기서 한다.
    /// </summary>
    public sealed class ScenarioLoader : MonoBehaviour
    {
        [SerializeField] TextAsset scenarioJson;

        public ScenarioRunner Runner { get; private set; }

        void Awake()
        {
            if (scenarioJson == null)
            {
                Debug.LogError("ScenarioLoader: scenarioJson 이 비어 있다.", this);
                return;
            }

            var data = JsonUtility.FromJson<ScenarioData>(scenarioJson.text);
            Runner = new ScenarioRunner(new ScenarioGraph(data));
        }
    }
}
