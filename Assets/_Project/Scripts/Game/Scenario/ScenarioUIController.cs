using System.Collections.Generic;
using CounterTime.Core.Scenario;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CounterTime.Game.Scenario
{
    /// <summary>현재 노드를 그리고, 자식 노드마다 선택 버튼을 만든다.</summary>
    public sealed class ScenarioUIController : MonoBehaviour
    {
        const string RestartLabel = "경기 종료 — 처음부터";
        static readonly Color RestartColor = new Color(0.25f, 0.25f, 0.28f);

        [SerializeField] ScenarioLoader loader;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text contentText;
        [SerializeField] RectTransform choiceRoot;
        [SerializeField] Button choiceButtonPrefab;

        readonly List<Button> _spawned = new List<Button>();
        ScenarioRunner _runner;

        void Start()
        {
            _runner = loader.Runner;
            if (_runner == null) return;

            _runner.NodeEntered += Render;
            _runner.Restart();
        }

        void OnDestroy()
        {
            if (_runner != null) _runner.NodeEntered -= Render;
        }

        void Render(ScenarioNode node)
        {
            titleText.text = node.title;
            // contentHtml 은 무시한다. content 의 \n 은 TMP 가 그대로 줄바꿈으로 그린다.
            contentText.text = node.content;

            ClearChoices();

            if (_runner.IsTerminal)
            {
                Spawn(RestartLabel, RestartColor, () => _runner.Restart());
                return;
            }

            foreach (var child in _runner.Choices)
            {
                var target = child.id;
                Spawn(child.title, BorderColor(child.border), () => _runner.MoveTo(target));
            }
        }

        void Spawn(string label, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var button = Instantiate(choiceButtonPrefab, choiceRoot);
            button.image.color = color;
            button.GetComponentInChildren<TMP_Text>().text = label;
            button.onClick.AddListener(onClick);
            _spawned.Add(button);
        }

        void ClearChoices()
        {
            foreach (var button in _spawned)
            {
                // Destroy 는 프레임 끝에 처리된다 — 먼저 떼어내야 이번 프레임의 레이아웃에 안 남는다.
                button.transform.SetParent(null, false);
                Destroy(button.gameObject);
            }
            _spawned.Clear();
        }

        static Color BorderColor(string border)
        {
            return ColorUtility.TryParseHtmlString(border, out var color) ? color : RestartColor;
        }
    }
}
