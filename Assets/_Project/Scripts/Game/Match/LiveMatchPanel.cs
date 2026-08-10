using System.Collections.Generic;
using CounterTime.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CounterTime.Game.Match
{
    /// <summary>
    /// 6단계 — A 전용 입력 패널. <see cref="MatchHud"/> 전례대로 런타임 생성 캔버스다
    /// (씬 수정 없음). 손/코어/하체 3열 버튼 → 확정 → <see cref="LiveMatchRunner.SubmitA"/>
    /// → 결과 서술 → 다음 턴. 굳히기가 열리면 3열 대신 A 의 역할에 맞는 2지선다로
    /// 바뀐다.
    ///
    /// <b>B 는 여기 안 나온다.</b> B 의 선택지·습관·심판 누산기 숫자는 이 패널 어디에도
    /// 읽거나 그리지 않는다 — 읽을 수 있는 것은 <see cref="LiveMatchRunner"/> 가 공개한
    /// A 자신의 면(<see cref="LiveAssembler"/>, <see cref="LiveMatchRunner.Tendency"/>)뿐이다.
    ///
    /// 기능 우선, 미려함은 뒤로 — 색과 배치는 최소한이다. 화면 실행 확인은 사람 몫
    /// (Unity Test Runner 로도, 헤드리스로도 캔버스는 검증 불가).
    /// </summary>
    [RequireComponent(typeof(LiveMatchRunner))]
    public sealed class LiveMatchPanel : MonoBehaviour
    {
        [SerializeField] TMP_FontAsset koreanFont;   // 기본 LiberationSans 는 한글이 두부다
        [SerializeField] int fontSize = 18;

        static readonly Color Idle = new Color(0.25f, 0.25f, 0.25f);
        static readonly Color Selected = new Color(0.15f, 0.45f, 0.75f);
        static readonly Color Warning = new Color(0.85f, 0.65f, 0.1f);
        static readonly Color Danger = new Color(0.8f, 0.15f, 0.15f);
        static readonly Color Dim = new Color(0.3f, 0.3f, 0.3f, 0.4f);

        LiveMatchRunner _runner;

        HandTarget _hand = HandTarget.None;
        CoreStance _core = CoreStance.Neutral;
        LegAction _legs = LegAction.Neutral;

        Transform _handColumn, _coreColumn, _legsColumn;
        GameObject _standingRoot, _osaekomiRoot;
        TMP_Text _resultText, _exposureText, _preemptedText;
        Image _warningMark, _dangerMark;
        Button _osaekomiOptionA, _osaekomiOptionB;
        TMP_Text _osaekomiLabelA, _osaekomiLabelB;

        void Awake()
        {
            _runner = GetComponent<LiveMatchRunner>();
            BuildCanvas();
        }

        void OnEnable()
        {
            _runner.Narrated += OnNarrated;
            _runner.OsaekomiOpened += OnOsaekomiOpened;
            _runner.OsaekomiClosed += OnOsaekomiClosed;
            _runner.APreempted += OnAPreempted;
        }

        void OnDisable()
        {
            _runner.Narrated -= OnNarrated;
            _runner.OsaekomiOpened -= OnOsaekomiOpened;
            _runner.OsaekomiClosed -= OnOsaekomiClosed;
            _runner.APreempted -= OnAPreempted;
        }

        void Start() => RefreshStanding();

        // ── 판정 서술 ────────────────────────────────────────────────────────────

        void OnNarrated(ResultNarrationTable.Result r)
        {
            _resultText.text = r.IsFallback ? $"<color=#999999>{r.Text}</color>" : r.Text;
        }

        /// [v1 임시 시스템 문구 — 카피 교체 목록에 올릴 것, md/STATUS.md 결정 대기 참조]
        void OnAPreempted()
        {
            _preemptedText.text = "B 선행 — A 공격 불발";
        }

        // ── 굳히기 전환 ──────────────────────────────────────────────────────────

        void OnOsaekomiOpened()
        {
            _standingRoot.SetActive(false);
            _osaekomiRoot.SetActive(true);

            bool aIsHolder = _runner.Engine.Osaekomi.Holder == FighterId.A;
            if (aIsHolder)
            {
                _osaekomiLabelA.text = "따라가기";
                _osaekomiLabelB.text = "눌러붙기";
                _osaekomiOptionA.onClick.RemoveAllListeners();
                _osaekomiOptionA.onClick.AddListener(() =>
                    _runner.SubmitOsaekomi(_runner.BDefenderPolicy(), OsaekomiHolderChoice.Follow));
                _osaekomiOptionB.onClick.RemoveAllListeners();
                _osaekomiOptionB.onClick.AddListener(() =>
                    _runner.SubmitOsaekomi(_runner.BDefenderPolicy(), OsaekomiHolderChoice.PressIn));
            }
            else
            {
                _osaekomiLabelA.text = "뒤집기";
                _osaekomiLabelB.text = "빠져나가기";
                _osaekomiOptionA.onClick.RemoveAllListeners();
                _osaekomiOptionA.onClick.AddListener(() =>
                    _runner.SubmitOsaekomi(OsaekomiDefenderChoice.Reversal, _runner.BHolderPolicy()));
                _osaekomiOptionB.onClick.RemoveAllListeners();
                _osaekomiOptionB.onClick.AddListener(() =>
                    _runner.SubmitOsaekomi(OsaekomiDefenderChoice.Escape, _runner.BHolderPolicy()));
            }
        }

        void OnOsaekomiClosed()
        {
            _osaekomiRoot.SetActive(false);
            _standingRoot.SetActive(true);
            RefreshStanding();
        }

        // ── 3열 입력 ─────────────────────────────────────────────────────────────

        void RefreshStanding()
        {
            _hand = HandTarget.None;
            _core = CoreStance.Neutral;
            _legs = LegAction.Neutral;
            RebuildColumn(_handColumn, _runner.A.HandOptions(), HandLabel, o => _hand.Equals(o),
                o => { _hand = (HandTarget)o; RebuildStandingColumns(); RefreshExposure(); });
            RebuildColumn(_coreColumn, _runner.A.CoreOptions(), CoreLabel, o => _core.Equals(o),
                o => { _core = (CoreStance)o; RebuildStandingColumns(); RefreshExposure(); });
            RebuildColumn(_legsColumn, _runner.A.LegOptions(), LegsLabel, o => _legs.Equals(o),
                o => { _legs = (LegAction)o; RebuildStandingColumns(); RefreshExposure(); });
            RefreshExposure();
        }

        void RebuildStandingColumns()
        {
            // 그립 상태가 바뀌지 않는 한(확정 전) 손 후보는 그대로다 — 선택 강조만 새로 칠한다.
            RebuildColumn(_handColumn, _runner.A.HandOptions(), HandLabel, o => _hand.Equals(o),
                o => { _hand = (HandTarget)o; RebuildStandingColumns(); RefreshExposure(); });
            RebuildColumn(_coreColumn, _runner.A.CoreOptions(), CoreLabel, o => _core.Equals(o),
                o => { _core = (CoreStance)o; RebuildStandingColumns(); RefreshExposure(); });
            RebuildColumn(_legsColumn, _runner.A.LegOptions(), LegsLabel, o => _legs.Equals(o),
                o => { _legs = (LegAction)o; RebuildStandingColumns(); RefreshExposure(); });
        }

        void Confirm()
        {
            // 지난 턴의 선점 표시를 먼저 지운다 — 이번 SubmitA 가 새로 띄우면 그 뒤에
            // 남는다. RefreshStanding 에서 지우면 같은 프레임에 방금 띄운 것까지
            // 지워진다.
            _preemptedText.text = "";
            var choice = _runner.A.Build(_hand, _core, _legs);
            _runner.SubmitA(choice);
            if (!_runner.Engine.Osaekomi.Active) RefreshStanding();
        }

        /// <summary>§3 노출 막대 — 지금 <b>선택 중인</b>(아직 확정 전) 조합이 경고선/위험선에
        /// 걸리는지를 매 선택마다 다시 계산한다. 읽히기 전에 보여야 하므로, 확정 버튼을
        /// 누르기 전에 이미 위험을 알 수 있어야 한다.</summary>
        void RefreshExposure()
        {
            var candidate = _runner.A.Build(_hand, _core, _legs);
            bool warn = _runner.Tendency.MajorityMatches(candidate);
            bool danger = _runner.Tendency.ConsecutiveMatches(candidate);

            _warningMark.color = warn ? Warning : Dim;
            _dangerMark.color = danger ? Danger : Dim;
            _exposureText.text = danger ? "위험 — 3연속 그대로면 읽힌다"
                               : warn ? "경고 — 최근 6수 중 4수 이상 같다"
                               : "";
        }

        void RebuildColumn<T>(Transform column, IReadOnlyList<T> options, System.Func<T, string> label,
                              System.Func<object, bool> isSelected, System.Action<object> onPick)
        {
            for (int i = column.childCount - 1; i >= 0; i--)
                Destroy(column.GetChild(i).gameObject);   // 확정 전 재구성뿐이라 즉시 파괴로 충분

            foreach (var opt in options)
            {
                var captured = opt;
                var btn = MakeButton(column, label(opt), () => onPick(captured));
                var img = btn.GetComponent<Image>();
                img.color = isSelected(captured) ? Selected : Idle;
            }
        }

        static string HandLabel(HandTarget t) => t switch
        {
            HandTarget.RightSleeve => "오른소매",
            HandTarget.LeftSleeve => "왼소매",
            HandTarget.LeftLapel => "왼가슴",
            HandTarget.RightLapel => "오른가슴",
            HandTarget.Evade => "회피",
            HandTarget.TwistGrip => "틀어잡기",
            HandTarget.BackGrab => "등싸잡기",
            HandTarget.Tear => "뜯기",
            HandTarget.MaintainGrip => "잡기 유지",
            HandTarget.Kuzushi => "쿠즈시",
            _ => "—",
        };

        static string CoreLabel(CoreStance c) => c switch
        {
            CoreStance.Aggressive => "공격적",
            CoreStance.Defensive => "수비적",
            _ => "중립",
        };

        static string LegsLabel(LegAction l) => l switch
        {
            LegAction.Attack => "공격",
            LegAction.Defend => "수비",
            LegAction.CounterReady => "카운터 준비",
            _ => "중립",
        };

        // ── 런타임 UI 뼈대 ───────────────────────────────────────────────────────

        void BuildCanvas()
        {
            var go = new GameObject("LiveMatchCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            BuildExposureBar(go.transform);
            BuildResultText(go.transform);
            BuildPreemptedText(go.transform);
            _standingRoot = BuildStandingRoot(go.transform);
            _osaekomiRoot = BuildOsaekomiRoot(go.transform);
            _osaekomiRoot.SetActive(false);
        }

        void BuildExposureBar(Transform parent)
        {
            var root = new GameObject("ExposureBar", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rt = (RectTransform)root.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(500, 40);
            rt.anchoredPosition = new Vector2(0, -16);

            _warningMark = MakeMark(root.transform, new Vector2(-70, 0));
            _dangerMark = MakeMark(root.transform, new Vector2(-30, 0));
            _exposureText = MakeText(root.transform, "내 성향 노출", TextAlignmentOptions.MidlineLeft,
                new Vector2(10, 0), new Vector2(420, 40));
        }

        Image MakeMark(Transform parent, Vector2 offset)
        {
            var go = new GameObject("Mark", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(24, 24);
            rt.anchoredPosition = offset;
            var img = go.GetComponent<Image>();
            img.color = Dim;
            return img;
        }

        void BuildResultText(Transform parent)
        {
            _resultText = MakeText(parent, "", TextAlignmentOptions.Center,
                new Vector2(0, -70), new Vector2(900, 80), new Vector2(0.5f, 1));
        }

        /// [v1 임시 시스템 문구용 슬롯 — 카피 교체 목록에 올릴 것]
        void BuildPreemptedText(Transform parent)
        {
            _preemptedText = MakeText(parent, "", TextAlignmentOptions.Center,
                new Vector2(0, -130), new Vector2(900, 40), new Vector2(0.5f, 1));
            _preemptedText.color = Warning;
        }

        GameObject BuildStandingRoot(Transform parent)
        {
            var root = new GameObject("StandingInput", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rt = (RectTransform)root.transform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.sizeDelta = new Vector2(0, 320);
            rt.anchoredPosition = Vector2.zero;

            _handColumn = BuildColumnRoot(root.transform, "손", new Vector2(0.06f, 0.5f));
            _coreColumn = BuildColumnRoot(root.transform, "코어", new Vector2(0.38f, 0.5f));
            _legsColumn = BuildColumnRoot(root.transform, "하체", new Vector2(0.62f, 0.5f));

            var confirmBtn = MakeButton(root.transform, "확정", Confirm);
            var confirmRt = (RectTransform)confirmBtn.transform.parent;
            confirmRt.anchorMin = confirmRt.anchorMax = new Vector2(0.9f, 0.5f);
            confirmRt.pivot = new Vector2(0.5f, 0.5f);
            confirmRt.anchoredPosition = Vector2.zero;
            confirmRt.sizeDelta = new Vector2(160, 60);

            return root;
        }

        Transform BuildColumnRoot(Transform parent, string title, Vector2 anchorX)
        {
            var go = new GameObject(title + "Column", typeof(RectTransform), typeof(VerticalLayoutGroup));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(anchorX.x - 0.14f, 0.05f);
            rt.anchorMax = new Vector2(anchorX.x + 0.14f, 0.95f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 6;

            return go.transform;
        }

        GameObject BuildOsaekomiRoot(Transform parent)
        {
            var root = new GameObject("OsaekomiInput", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rt = (RectTransform)root.transform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.sizeDelta = new Vector2(0, 160);
            rt.anchoredPosition = Vector2.zero;

            _osaekomiOptionA = MakeButton(root.transform, "", null);
            var aRt = (RectTransform)_osaekomiOptionA.transform.parent;
            aRt.anchorMin = aRt.anchorMax = new Vector2(0.35f, 0.5f);
            aRt.anchoredPosition = Vector2.zero;
            aRt.sizeDelta = new Vector2(280, 80);
            _osaekomiLabelA = _osaekomiOptionA.GetComponentInChildren<TMP_Text>();

            _osaekomiOptionB = MakeButton(root.transform, "", null);
            var bRt = (RectTransform)_osaekomiOptionB.transform.parent;
            bRt.anchorMin = bRt.anchorMax = new Vector2(0.65f, 0.5f);
            bRt.anchoredPosition = Vector2.zero;
            bRt.sizeDelta = new Vector2(280, 80);
            _osaekomiLabelB = _osaekomiOptionB.GetComponentInChildren<TMP_Text>();

            return root;
        }

        Button MakeButton(Transform parent, string label, System.Action onClick)
        {
            var go = new GameObject(string.IsNullOrEmpty(label) ? "Button" : label,
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(220, 44);

            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = 44;

            var img = go.GetComponent<Image>();
            img.color = Idle;

            var btn = go.GetComponent<Button>();
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            MakeText(go.transform, label, TextAlignmentOptions.Center, Vector2.zero,
                new Vector2(220, 44), new Vector2(0.5f, 0.5f), stretch: true);

            return btn;
        }

        TMP_Text MakeText(Transform parent, string text, TextAlignmentOptions align, Vector2 offset,
                          Vector2 size, Vector2? anchor = null, bool stretch = false)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            if (stretch)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
            else
            {
                var a = anchor ?? new Vector2(0.5f, 1);
                rt.anchorMin = rt.anchorMax = a;
                rt.pivot = a;
                rt.sizeDelta = size;
                rt.anchoredPosition = offset;
            }

            var t = go.GetComponent<TextMeshProUGUI>();
            if (koreanFont != null) t.font = koreanFont;
            t.fontSize = fontSize;
            t.alignment = align;
            t.text = text;
            t.raycastTarget = false;
            return t;
        }
    }
}
