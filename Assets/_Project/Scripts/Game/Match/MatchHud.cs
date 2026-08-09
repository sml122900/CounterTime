using System.Text;
using CounterTime.Core;
using TMPro;
using UnityEngine;

namespace CounterTime.Game.Match
{
    /// <summary>
    /// §7 최소 그립 HUD. 취향이 아니라 하드 요구다 — 320×180 에서 손은 5.8px 라
    /// 접촉이 있다는 건 보여도 <b>뭘 잡았는지</b>는 안 보인다. 이게 없으면 뜯기(§3.2)와
    /// 부위 지정 그립이 3분의 1 찍기로 퇴화한다.
    ///
    /// 저해상도 RenderTexture 는 아직 없지만 배치는 그게 들어올 것을 전제한다:
    /// <b>화면 모서리 고정만 쓴다.</b> 캐릭터에 붙는 배치(말풍선·손 옆 라벨)는 저해상도
    /// 뷰가 RawImage 로 들어오는 순간 레터박스 때문에 스크린 좌표가 어긋나 전부 다시 짜야 한다.
    ///
    /// 표시 금지 — 심판 소극 누산기 숫자(§6.1 "NEVER shown as a number", tell 로만),
    /// 그리고 예비동작(소매·깃·뒷깃 노림). 확정된 그립만 띄운다.
    /// </summary>
    [RequireComponent(typeof(MatchRunner))]
    public sealed class MatchHud : MonoBehaviour
    {
        [SerializeField] TMP_FontAsset koreanFont;   // 기본 LiberationSans 는 한글이 전부 두부다
        [SerializeField] int fontSize = 22;

        MatchRunner _runner;
        TMP_Text _center, _left, _right;

        /// 잡은 부위는 GripSystem 에 없다 — 거기 남는 건 "어느 손을 잡았나"(Hand)까지고
        /// 소매냐 깃이냐는 <see cref="GripChanged"/> 만 싣고 온다. §7 이 Core 에 명시적
        /// 이벤트를 요구한 이유가 이거고, 그래서 여기서 이벤트로 누적한다.
        /// 애니메이션에서 역추론하지 않는다.
        readonly HandTarget[,] _held = new HandTarget[2, 2];   // [fighter, hand-1]

        readonly RefereeTell[] _tell = new RefereeTell[2];

        void Awake()
        {
            _runner = GetComponent<MatchRunner>();
            BuildCanvas();
        }

        void OnEnable() => _runner.Presented += OnPresented;
        void OnDisable() => _runner.Presented -= OnPresented;

        void OnPresented(Core.Scenario.ScenarioNode node, MatchEvent e)
        {
            switch (e)
            {
                case GripChanged g:
                    _held[(int)g.Who, Ix(g.OwnHand)] = g.Kind == GripKind.None ? HandTarget.None : g.Part;
                    break;
                case MatteCalled _:
                    System.Array.Clear(_held, 0, _held.Length);   // §3.2 그쳐는 모든 연결을 푼다
                    break;
                case RefereeTellChanged r:
                    _tell[(int)r.Who] = r.Tell;
                    break;
            }
            Refresh();
        }

        void LateUpdate() => Refresh();   // 시계·점수는 이벤트 없이도 바뀐다

        void Refresh()
        {
            var e = _runner.Engine;
            if (e == null) return;

            int rem = e.Clock.RemainingSeconds;
            _center.text = $"{rem / 60}:{rem % 60:00}";
            _left.text = Panel(e, FighterId.A);
            _right.text = Panel(e, FighterId.B);
        }

        string Panel(MatchEngine e, FighterId who)
        {
            var b = new StringBuilder();
            b.Append(who).Append("   잡기 ").Append(Signed(e.Grip.GripScore(who)));
            int eff = e.Grip.EffectiveGrip(who);
            if (eff != e.Grip.GripScore(who)) b.Append("  (유효 ").Append(Signed(eff)).Append(')');
            b.AppendLine();

            foreach (var h in new[] { Hand.Left, Hand.Right })
                b.Append(' ').Append(h == Hand.Left ? "왼손 " : "오른손")
                 .Append("  ").Append(Part(_held[(int)who, Ix(h)]).PadRight(8))
                 .Append("  ").AppendLine(Lock(e.Grip.LockState(who, h)));

            b.Append(" 유효 ").Append(e.Board.Yuko(who))
             .Append("  절반 ").Append(e.Board.WazaAri(who))
             .Append(e.Board.HasIppon(who) ? "  한판" : "")
             .Append("  지도 ").Append(e.Board.Shido(who));

            // §6.1 — 누산기 숫자는 절대 안 띄운다. tell 만.
            if (_tell[(int)who] != RefereeTell.None)
                b.AppendLine().Append(_tell[(int)who] == RefereeTell.Major ? " 심판이 노려본다" : " 심판이 주시한다");
            return b.ToString();
        }

        static string Signed(int v) => v > 0 ? "+" + v : v.ToString();
        static int Ix(Hand h) => h == Hand.Left ? 0 : 1;

        static string Lock(GripLock l) =>
            l == GripLock.Locked ? "견고" : l == GripLock.Held ? "잡힘" : "놓임";

        static string Part(HandTarget t)
        {
            switch (t)
            {
                case HandTarget.RightSleeve: return "오른소매";
                case HandTarget.LeftSleeve: return "왼소매";
                case HandTarget.LeftLapel: return "왼가슴";
                case HandTarget.RightLapel: return "오른가슴";
                case HandTarget.TwistGrip: return "뒷깃";
                case HandTarget.BackGrab: return "등싸잡기";
                default: return "—";
            }
        }

        // ---- 런타임 UI. 저해상도가 들어와도 Overlay 캔버스는 영향을 안 받는다 ----

        void BuildCanvas()
        {
            var go = new GameObject("MatchHudCanvas",
                typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;                       // 시나리오 캔버스 위
            var scaler = go.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _left = Label(go.transform, "A", new Vector2(0, 1), TextAlignmentOptions.TopLeft,
                          new Vector2(16, -16));
            _right = Label(go.transform, "B", new Vector2(1, 1), TextAlignmentOptions.TopRight,
                           new Vector2(-16, -16));
            _center = Label(go.transform, "Clock", new Vector2(.5f, 1), TextAlignmentOptions.Top,
                            new Vector2(0, -16));
        }

        TMP_Text Label(Transform parent, string name, Vector2 anchor,
                       TextAlignmentOptions align, Vector2 offset)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(anchor.x, 1);
            rt.sizeDelta = new Vector2(420, 200);
            rt.anchoredPosition = offset;

            var t = go.GetComponent<TextMeshProUGUI>();
            if (koreanFont != null) t.font = koreanFont;
            t.fontSize = fontSize;
            t.alignment = align;
            t.raycastTarget = false;      // HUD 가 시나리오 버튼 클릭을 먹으면 안 된다
            return t;
        }
    }
}
