using System;
using CounterTime.Core;
using CounterTime.Core.Scenario;
using UnityEngine;

namespace CounterTime.Game.Match
{
    /// <summary>
    /// 씬에서 <see cref="MatchEngine"/> 을 굴린다. 얇게 — 점수·체력·시계·그립·지도를
    /// 필드로 들지 않는다. 전부 엔진에게 묻는다.
    ///
    /// 하는 일은 셋뿐: 입력 수집 → <c>ResolveExchange</c> → 반환된 이벤트를 밖으로.
    ///
    /// 입력은 임시 키보드다(1~4 = 갈래 선택, Space = 외길 진행, R = 재시작).
    /// 사람이 고르는 것은 <b>어느 갈래로 가느냐</b>이고, 그 노드의 커밋 자체는 노드가
    /// 정한다 — 그래야 42노드 대조가 재현 가능해진다.
    /// </summary>
    public sealed class MatchRunner : MonoBehaviour
    {
        [SerializeField] TextAsset matchScenarioJson;
        [SerializeField] string startNodeId = "m1";
        [SerializeField] RefereeProfile refereeProfile = RefereeProfile.Standard;
        [SerializeField] int randomSeed = 12345;
        [SerializeField] bool logEvents = true;

        MatchRulesConfig _cfg;
        ScenarioExchangeScript _script;

        /// 러너가 드는 유일한 상태는 '입력'이다. 말한 적 없는 커밋은 근거가 아니므로
        /// 노드마다 새로 세운다 — 노드가 밝힌 선수만 그 자세를 갖고, 나머지는 중립이다.
        /// 자세를 이어붙이는 것은 없는 데이터를 만들어내는 것이다.
        readonly TurnChoice[] _commit = new TurnChoice[2];

        /// default(TurnChoice) 는 중립이 아니다 — Aggressive/Attack 이 enum 0 이라서
        /// 심판의 §6.1 '전부 중립'(+1) 에도 안 걸린다. 중립은 명시해야 중립이다.
        static readonly TurnChoice Neutral =
            new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.Neutral);

        public MatchEngine Engine { get; private set; }
        public ScenarioRunner Scenario { get; private set; }

        /// 화면·로그·CSV 가 붙는 유일한 지점.
        public event Action<ScenarioNode, MatchEvent> Presented;

        void Awake() => Build(matchScenarioJson);
        void Start() => Begin();

        public void Build(TextAsset json)
        {
            _cfg = new MatchRulesConfig();
            Engine = new MatchEngine(_cfg, refereeProfile, new SystemRandomSource(randomSeed));
            _script = new ScenarioExchangeScript(_cfg);
            Scenario = new ScenarioRunner(new ScenarioGraph(JsonUtility.FromJson<ScenarioData>(json.text)));
            _commit[0] = _commit[1] = Neutral;
        }

        public void Begin() => EnterNode(startNodeId);

        /// <summary>자식이 없으면 false — 종단이다.</summary>
        public bool Advance(int childIndex)
        {
            var kids = Scenario.Choices;
            if (kids.Count == 0) return false;
            EnterNode(kids[Mathf.Clamp(childIndex, 0, kids.Count - 1)].id);
            return true;
        }

        public void EnterNode(string nodeId)
        {
            Scenario.MoveTo(nodeId);
            var node = Scenario.Current;
            if (node == null) return;

            _commit[0] = _commit[1] = Neutral;
            if (ScenarioTurnChoices.TryParse(node, out var who, out var commit))
                _commit[(int)who] = commit;

            var scripts = _script.For(node);
            bool hasTechnique = scripts.Count > 0 && scripts[0].Attacker.HasValue;
            bool isActor = node.Kind == NodeKind.ChoiceA || node.Kind == NodeKind.ActionB;

            // 서술·결과 노드는 교환이 아니다 — 표시만 하고 시계를 태우지 않는다.
            if (!hasTechnique && !isActor) return;

            foreach (var s in scripts)
                foreach (var e in Engine.ResolveExchange(_commit[0], _commit[1], s))
                {
                    Present(node, e);
                    // 그쳐는 그립만 푸는 게 아니다 — 선수가 일어서서 다시 선다.
                    // 자세도 같이 초기화해야 노드 안의 남은 교환이 옛 자세를 물려받지 않는다.
                    if (e is MatteCalled) _commit[0] = _commit[1] = Neutral;
                }
        }

        void Present(ScenarioNode node, MatchEvent e)
        {
            Presented?.Invoke(node, e);
            if (logEvents) Debug.Log($"[{node.id}] {Describe(e)}", this);
        }

        static string Describe(MatchEvent e)
        {
            switch (e)
            {
                case JudgementScored j:
                    return $"판정 {j.Judgement} sum={j.Sum} by={j.ScoredBy}" +
                           (j.IsCounter ? " (되치기)" : "") +
                           $" [{string.Join(",", j.Terms)}]";
                case GripChanged g: return $"잡기 {g.Who} {g.OwnHand}→{g.Part} ({g.Kind})";
                case TearResolved t: return $"뜯기 {t.Who} {t.Hand} → {t.Outcome}";
                case CounterReadyApplied c:
                    return $"카운터 준비 {c.Who}: 공격자 읽기 {c.AttackerReadBefore}→{c.AttackerReadAfter}";
                case ShidoGiven s: return $"지도 {s.Who} #{s.Count}" + (s.FromFeint ? " (위장)" : " (소극)");
                case RefereeTellChanged r: return $"심판 tell {r.Who} → {r.Tell}";
                case MatteCalled _: return "그쳐";
                case MatchEnded m: return $"경기 종료 — 승자 {(m.Winner.HasValue ? m.Winner.ToString() : "무승부")}";
                default: return e.GetType().Name;
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.R)) { Build(matchScenarioJson); Begin(); return; }
            if (Input.GetKeyDown(KeyCode.Space)) { Advance(0); return; }
            for (int i = 0; i < 4; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i)) { Advance(i); return; }
        }
    }
}
