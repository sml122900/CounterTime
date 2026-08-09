using System;
using CounterTime.Brain;
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
        /// 42노드 대조는 재현 가능해야 한다. §6.2 의 임계 ±1 지터는 실플레이에서만 살린다.
        /// 규칙 문서가 아니라 config 값이라 여기서 끈다 — MATCH_RULES 의 숫자는 그대로다.
        [SerializeField] bool deterministicReplay = true;

        MatchRulesConfig _cfg;
        ScenarioExchangeScript _script;

        /// 러너가 드는 유일한 상태는 '입력'이다.
        ///
        /// 자세는 <b>다음 선언 또는 그쳐까지 유지된다</b> — m13 의 극단적 방어가 m14 의 네
        /// 교환 내내 살아 있어야 문서의 상대수비 −2 가 나온다. 끊는 지점은 그쳐다:
        /// 그립이 풀릴 때 선수도 일어서서 다시 선다.
        ///
        /// null = 아직 선언된 적이 없다(화면 밖). 중립을 '고른' 것과 다르므로 심판이
        /// 관찰하지 않는다 — <see cref="ExchangeScript.DeclaredA"/> 참조.
        readonly TurnChoice?[] _commit = new TurnChoice?[2];

        public MatchEngine Engine { get; private set; }
        public ScenarioRunner Scenario { get; private set; }

        /// 화면·로그·CSV 가 붙는 유일한 지점.
        public event Action<ScenarioNode, MatchEvent> Presented;

        void Awake() => Build(matchScenarioJson);
        void Start() => Begin();

        public void Build(TextAsset json)
        {
            _cfg = new MatchRulesConfig();
            if (deterministicReplay) _cfg.ThresholdJitter = 0;
            Engine = new MatchEngine(_cfg, refereeProfile, new SystemRandomSource(randomSeed));
            _script = new ScenarioExchangeScript(_cfg);
            Scenario = new ScenarioRunner(new ScenarioGraph(JsonUtility.FromJson<ScenarioData>(json.text)));
            _commit[0] = _commit[1] = null;
        }

        public void Begin() => EnterNode(startNodeId);

        /// <summary>
        /// 대조용 메인라인. CORE_DESIGN 은 <c>m15→…→m36</c> 으로 가운데를 비워뒀는데,
        /// 최단 경로를 고르면 그쳐로 풀린 그립을 다시 안 잡아 m36 의 잡기 항이 0 이 된다.
        ///
        /// m2 가 스스로 말한다 — "모든 '그쳐' 후 재개는 여기로 회귀". 다시 잡는 것이
        /// 허브가 있는 이유다. 그래서 재개 후 첫 회차와 같은 잡기 경로(m3→m4→m8→m9)를
        /// 한 번 더 탄다. §2 의 전제조건(소매·깃 먼저, 그다음 틀어잡기)도 그 경로가
        /// 이미 만족시킨다.
        /// </summary>
        public static readonly string[] Mainline =
        {
            "m1", "m2", "m3", "m4", "m8", "m9", "m13", "m14", "m15",
            "m2", "m3", "m4", "m8", "m9",     // 그쳐 후 재개 — 잡기 +2 복구
            "m31", "m35", "m36",
        };

        /// <summary>사람 입력 없이 <see cref="Mainline"/> 을 그대로 걷는다 — 42노드 대조용.</summary>
        public void RunMainline()
        {
            foreach (var id in Mainline) EnterNode(id);
        }

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

            // 러너는 커밋과 읽기가 어디서 오는지 이름으로 모른다 — 인터페이스만 안다.
            // 여기 꽂힌 것이 대조 모드의 스크립트 출처이고, 플레이 모드에서는 A 입력과
            // B 습관 브레인이 같은 자리에 꽂힌다.
            ICommitSource commits = new ScriptedCommitSource(node);
            IReadResolver read = new ScriptedReadResolver(_script, node);

            bool declaredNow = commits.TryNext(out var who, out var commit);
            if (declaredNow) _commit[(int)who] = commit;

            var scripts = _script.For(node);
            bool hasTechnique = scripts.Count > 0 && scripts[0].Attacker.HasValue;

            // §2 — 교환은 최소 한 명이 행동해야 성립한다. 자세 선언만 있는 노드(m13)와
            // 서술·결과 노드는 시계를 태우지 않고 소극도 누산하지 않는다. 선언한 자세는
            // 위에서 이미 반영됐고 규칙 1대로 다음 선언이나 그쳐까지 유지된다.
            //
            // 행동 여부는 '이 노드가 선언한' 커밋으로만 본다. 유지 중인 잡기는 이미
            // GripSystem 에 서 있으므로 노드마다 다시 잡는 것이 아니다.
            if (!hasTechnique && !(declaredNow && commit.HasHandAction)) return;

            foreach (var s in scripts)
            {
                // 기술을 던지는 쪽은 그 자체가 선언이다 — 자세를 따로 안 밝힌 판정 노드
                // (m36 같은)에서도 심판은 실제 시도(§6.1 −4)를 봐야 한다.
                s.DeclaredA = _commit[0].HasValue || s.Attacker == FighterId.A;
                s.DeclaredB = _commit[1].HasValue || s.Attacker == FighterId.B;

                // 읽기는 리졸버 한 곳에서만 나온다. 대조 모드에서는 노드가 정한 값이라
                // 조립기가 넣어둔 값과 같다 — 3단계에서 여기 LiveReadResolver 가 꽂힌다.
                if (s.Attacker.HasValue)
                    s.Read = read.For(s.Attacker.Value, _commit[0] ?? TurnChoice.Neutral,
                                                        _commit[1] ?? TurnChoice.Neutral);

                foreach (var e in Engine.ResolveExchange(_commit[0] ?? TurnChoice.Neutral,
                                                         _commit[1] ?? TurnChoice.Neutral, s))
                {
                    Present(node, e);
                    // 그쳐가 끊는 지점이다. 그립이 풀리듯 자세도 풀린다 — 아무도 아직
                    // 아무것도 선언하지 않은 상태로 돌아간다.
                    if (e is MatteCalled) _commit[0] = _commit[1] = null;
                }

                // 손 행동은 사건이라 선언한 교환에서 한 번만 일어난다. 자세는 남는다.
                // 이게 없으면 유지 중인 잡기가 매 교환 다시 실행돼, 상대가 뜯어낸 그립을
                // 같은 교환 안에서 도로 잡아버린다(§3.2 무효화).
                for (int i = 0; i < 2; i++)
                    if (_commit[i].HasValue && _commit[i].Value.HasHandAction)
                        _commit[i] = StanceOnly(_commit[i].Value);
            }
        }

        /// 사건은 떼고 상태만 남긴다 — 코어·하체는 다음 선언이나 그쳐까지 그대로 간다.
        static TurnChoice StanceOnly(in TurnChoice c) =>
            new TurnChoice(HandTarget.None, c.Core, c.Legs, c.OwnHand);

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
