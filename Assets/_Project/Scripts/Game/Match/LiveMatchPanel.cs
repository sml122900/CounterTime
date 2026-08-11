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
        /// Resources.Load 이름(확장자 없음) — Assets/_Project/Art/Fonts/Resources/ 아래.
        /// PlayModeSetupMenu 의 편집 시점 대입이 어떤 이유로든 비어 있게 도착했을 때
        /// Awake 에서 직접 재로드하는 안전망의 유일한 진입점이라, 빌드에서도 먹혀야
        /// 한다 — AssetDatabase(에디터 전용) 대신 Resources 를 쓰는 이유.
        public const string KoreanFontResourceName = "NotoSansKR SDF";

        [SerializeField] TMP_FontAsset koreanFont;   // 기본 LiberationSans 는 한글이 두부다
        [SerializeField] int fontSize = 18;

        static readonly Color Idle = new Color(0.25f, 0.25f, 0.25f);
        static readonly Color Selected = new Color(0.15f, 0.45f, 0.75f);
        static readonly Color Warning = new Color(0.85f, 0.65f, 0.1f);
        static readonly Color Danger = new Color(0.8f, 0.15f, 0.15f);
        static readonly Color Dim = new Color(0.3f, 0.3f, 0.3f, 0.4f);
        static readonly Color Gold = new Color(0.85f, 0.7f, 0.15f);
        static readonly Color ShidoYellow = new Color(0.95f, 0.85f, 0.1f);
        static readonly Color RowWhiteBg = new Color(0.92f, 0.92f, 0.92f);
        static readonly Color RowBlueBg = new Color(0.1f, 0.35f, 0.75f);

        LiveMatchRunner _runner;

        HandTarget _hand = HandTarget.None;
        CoreStance _core = CoreStance.Neutral;
        LegAction _legs = LegAction.Neutral;

        Transform _handColumn, _coreColumn, _legsColumn;
        GameObject _standingRoot, _osaekomiRoot;
        TMP_Text _resultText, _exposureText, _preemptedText, _selectionText, _logText;
        TMP_Text _phaseText, _statusTextA, _statusTextB;
        Image _warningMark, _dangerMark;
        Button _osaekomiOptionA, _osaekomiOptionB;
        TMP_Text _osaekomiLabelA, _osaekomiLabelB;

        // ── 실전 점수판 (사진 레퍼런스 규격) ────────────────────────────────────────
        TMP_Text _bigTimerText, _goldenScoreLabel, _ipponStrip;
        TMP_Text _wazaAriTextA, _yukoTextA, _holdTextA;
        TMP_Text _wazaAriTextB, _yukoTextB, _holdTextB;
        Transform _shidoIconsA, _shidoIconsB;

        // ── 교환 로그 집계용 임시 버퍼 — Confirm()/osaekomi 제출 한 번(=Submit* 한 번
        // 호출) 동안만 유효하다. Presented/Narrated/APreempted 는 그 호출 안에서
        // 동기로 발화하므로, 호출 전에 비우고 호출 뒤에 바로 읽으면 된다.
        readonly List<MatchEvent> _pendingEvents = new List<MatchEvent>();
        readonly List<string> _log = new List<string>();
        string _pendingNarrationText;
        bool _pendingNarrationFallback;
        bool _pendingPreempted;

        void Awake()
        {
            _runner = GetComponent<LiveMatchRunner>();
            if (koreanFont == null)
            {
                koreanFont = Resources.Load<TMP_FontAsset>(KoreanFontResourceName);
                if (koreanFont == null)
                    Debug.LogError($"[LiveMatchPanel] 한글 폰트 로드 실패 — " +
                        $"Resources.Load<TMP_FontAsset>(\"{KoreanFontResourceName}\") 가 null. " +
                        "Resources 폴더 아래 그 이름의 TMP_FontAsset 이 있는지 확인할 것. " +
                        "이 상태로 진행하면 한글이 전부 □로 보인다.");
            }
            BuildCanvas();
        }

        void OnEnable()
        {
            _runner.Presented += OnPresented;
            _runner.Narrated += OnNarrated;
            _runner.OsaekomiOpened += OnOsaekomiOpened;
            _runner.OsaekomiClosed += OnOsaekomiClosed;
            _runner.APreempted += OnAPreempted;
        }

        void OnDisable()
        {
            _runner.Presented -= OnPresented;
            _runner.Narrated -= OnNarrated;
            _runner.OsaekomiOpened -= OnOsaekomiOpened;
            _runner.OsaekomiClosed -= OnOsaekomiClosed;
            _runner.APreempted -= OnAPreempted;
        }

        void Start()
        {
            RefreshStanding();
            RefreshStatusHud();
        }

        // ── 판정 서술 ────────────────────────────────────────────────────────────

        void OnPresented(MatchEvent e) => _pendingEvents.Add(e);

        void OnNarrated(ResultNarrationTable.Result r)
        {
            _pendingNarrationText = r.Text;
            _pendingNarrationFallback = r.IsFallback;
            _resultText.text = r.IsFallback ? $"<color=#999999>{r.Text}</color>" : r.Text;
        }

        /// [v1 임시 시스템 문구 — 카피 교체 목록에 올릴 것, md/STATUS.md 결정 대기 참조]
        void OnAPreempted()
        {
            _preemptedText.text = "B 선행 — A 공격 불발";
            _pendingPreempted = true;
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
                    SubmitOsaekomiChoice(_runner.BDefenderPolicy(), OsaekomiHolderChoice.Follow, "따라가기"));
                _osaekomiOptionB.onClick.RemoveAllListeners();
                _osaekomiOptionB.onClick.AddListener(() =>
                    SubmitOsaekomiChoice(_runner.BDefenderPolicy(), OsaekomiHolderChoice.PressIn, "눌러붙기"));
            }
            else
            {
                _osaekomiLabelA.text = "뒤집기";
                _osaekomiLabelB.text = "빠져나가기";
                _osaekomiOptionA.onClick.RemoveAllListeners();
                _osaekomiOptionA.onClick.AddListener(() =>
                    SubmitOsaekomiChoice(OsaekomiDefenderChoice.Reversal, _runner.BHolderPolicy(), "뒤집기"));
                _osaekomiOptionB.onClick.RemoveAllListeners();
                _osaekomiOptionB.onClick.AddListener(() =>
                    SubmitOsaekomiChoice(OsaekomiDefenderChoice.Escape, _runner.BHolderPolicy(), "빠져나가기"));
            }

            RefreshStatusHud();
        }

        void OnOsaekomiClosed()
        {
            _osaekomiRoot.SetActive(false);
            _standingRoot.SetActive(true);
            RefreshStanding();
            RefreshStatusHud();
        }

        /// §9.3 — 5초 틱 하나 제출. 결과 이벤트를 모아 교환 로그에 한 줄 남기고 HUD 를
        /// 새로고침한다. 굳히기가 계속 열려 있으면(<see cref="OnOsaekomiOpened"/> 가 다시
        /// 안 불린다) 홀더가 안 바뀌므로 버튼 라벨은 그대로 둔다.
        void SubmitOsaekomiChoice(OsaekomiDefenderChoice defenderChoice, OsaekomiHolderChoice holderChoice,
                                  string myChoiceLabel)
        {
            BeginPending();
            _runner.SubmitOsaekomi(defenderChoice, holderChoice);
            PushLog(myChoiceLabel, BuildOutcome());
            RefreshStatusHud();
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
            string myChoiceLabel = $"{HandLabel(_hand)} · {CoreLabel(_core)} · {LegsLabel(_legs)}";
            BeginPending();
            var choice = _runner.A.Build(_hand, _core, _legs);
            _runner.SubmitA(choice);
            string outcome = BuildOutcome();
            bool matte = _pendingEvents.Exists(e => e is MatteCalled);
            PushLog(myChoiceLabel, outcome);
            if (!_runner.Engine.Osaekomi.Active) RefreshStanding();
            RefreshStatusHud(matte);
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

            _selectionText.text = $"선택: {HandLabel(_hand)} · {CoreLabel(_core)} · {LegsLabel(_legs)}";
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

        // ── 교환 로그 ────────────────────────────────────────────────────────────

        void BeginPending()
        {
            _pendingEvents.Clear();
            _pendingNarrationText = null;
            _pendingPreempted = false;
        }

        /// 이번 Submit* 호출에서 무슨 일이 났는지 한 줄로 요약한다. 우선순위:
        /// 선점(B 선행) → 판정 서술(있으면, 폴백 구분 유지) → 발생한 이벤트 중 뜻이
        /// 있는 첫 번째 → "변화 없음"(그립·틀어잡기처럼 화면에 안 잡히는 사소한 것만
        /// 일어난 교환).
        string BuildOutcome()
        {
            if (_pendingPreempted) return "B 선행 — A 공격 불발";
            if (_pendingNarrationText != null)
                return _pendingNarrationFallback
                    ? $"<color=#999999>{_pendingNarrationText}</color>"
                    : _pendingNarrationText;

            foreach (var e in _pendingEvents)
            {
                var s = EventSummary(e);
                if (s != null) return s;
            }
            return "변화 없음";
        }

        static string EventSummary(MatchEvent e) => e switch
        {
            GoldenScoreEntered => "골든스코어 진입",
            OsaekomiScoreAwarded sa => $"{JudgementLabel(sa.Judgement)} 획득 — 누름 {sa.HeldSeconds}초",
            OsaekomiEnded => "굳히기 종료 — 다시 서기",
            MatchEnded m => m.Winner.HasValue ? $"경기 종료 — {FighterLabel(m.Winner.Value)} 승" : "경기 종료 — 무승부",
            ShidoGiven sg => $"지도 {sg.Count} — {FighterLabel(sg.Who)} ({(sg.FromFeint ? "위장 공격 적발" : "소극 누적")})",
            MatteCalled => "그쳐",
            KuzushiDeclared kd => $"{FighterLabel(kd.Who)} 쿠즈시 설치",
            TearResolved tr => $"{FighterLabel(tr.Who)} 뜯기 — {TearLabel(tr.Outcome)}",
            CounterReadyApplied cr => $"{FighterLabel(cr.Who)} 카운터 준비 적중 — 상대 읽기 하락",
            OsaekomiEscapeResolved er => er.EscapeCredited ? "이탈 성공" : "눌림 유지 — 이탈 실패",
            GripChanged g => $"{FighterLabel(g.Who)} {HandLabel(g.Part)} 확보",
            _ => null,
        };

        void PushLog(string myChoiceLabel, string outcome)
        {
            _log.Insert(0, $"{myChoiceLabel} → {outcome}");
            if (_log.Count > 5) _log.RemoveRange(5, _log.Count - 5);
            _logText.text = string.Join("\n", _log);
        }

        static string FighterLabel(FighterId f) => f == FighterId.A ? "A" : "B";

        static string JudgementLabel(Judgement j) => j switch
        {
            Judgement.Yuko => "유효",
            Judgement.WazaAri => "절반",
            Judgement.Ippon => "한판",
            _ => "무득점",
        };

        static string TearLabel(TearOutcome o) => o switch
        {
            TearOutcome.Freed => "해제 성공",
            TearOutcome.Locked => "실패 — 오히려 잠김",
            _ => "뜯을 그립 없음",
        };

        static string GripLockLabel(GripLock l) => l switch
        {
            GripLock.Held => "잡힘",
            GripLock.Locked => "잠김",
            _ => "놓임",
        };

        // ── 실전 점수판 ──────────────────────────────────────────────────────────

        /// 사진 레퍼런스 규격 — 큰 타이머, 골든스코어 표기, IPPON 강조 스트립,
        /// A(위, 흰 배경)/B(아래, 파란 배경) 두 행에 절반(W)·유효(Y)·지도(S/H, 카드
        /// 아이콘)·누르기 카운트(2자리, 국제룰 5/10/20초 경계). 심판 누산기 숫자,
        /// B 의 선택지·의도, 판정식 합계는 여기 어디에도 없다(설계 확정).
        void RefreshScoreboard(MatchEngine eng)
        {
            int rem = eng.Clock.RemainingSeconds;
            bool golden = eng.Board.InGoldenScore || eng.Clock.Phase == MatchPhase.GoldenScore;
            _bigTimerText.text = $"{rem / 60:00}:{rem % 60:00}";
            _bigTimerText.color = golden ? Gold : Color.white;
            _goldenScoreLabel.text = golden ? "GOLDEN SCORE" : "";

            bool anyIppon = eng.Board.HasIppon(FighterId.A) || eng.Board.HasIppon(FighterId.B);
            _ipponStrip.color = anyIppon ? Danger : Dim;

            RefreshScoreRow(eng, FighterId.A, _wazaAriTextA, _yukoTextA, _shidoIconsA, _holdTextA);
            RefreshScoreRow(eng, FighterId.B, _wazaAriTextB, _yukoTextB, _shidoIconsB, _holdTextB);
        }

        void RefreshScoreRow(MatchEngine eng, FighterId who, TMP_Text wazaAriText, TMP_Text yukoText,
                             Transform shidoIcons, TMP_Text holdText)
        {
            wazaAriText.text = eng.Board.WazaAri(who).ToString();
            yukoText.text = eng.Board.Yuko(who).ToString();
            RefreshShidoIcons(shidoIcons, eng.Board.Shido(who));
            int held = eng.Osaekomi.Active && eng.Osaekomi.Holder == who ? eng.Osaekomi.HeldSeconds : 0;
            holdText.text = $"{held:00}";
        }

        /// §1 지도 — 1·2 개는 노란 카드 누적, 3개째(=한소쿠마케로 즉시 종료)는 노란
        /// 카드가 아니라 빨간 카드 하나로 바뀐다.
        void RefreshShidoIcons(Transform container, int shidoCount)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);

            if (shidoCount <= 0) return;
            if (shidoCount >= 3) { MakeShidoIcon(container, 0, Danger); return; }
            for (int i = 0; i < shidoCount; i++)
                MakeShidoIcon(container, i, ShidoYellow);
        }

        void MakeShidoIcon(Transform parent, int index, Color color)
        {
            var go = new GameObject("Shido", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot = new Vector2(0, 0.5f);
            rt.sizeDelta = new Vector2(16, 16);
            rt.anchoredPosition = new Vector2(index * 20, 0);
            go.GetComponent<Image>().color = color;
        }

        // ── 점수판 아래 상태 HUD ─────────────────────────────────────────────────

        /// 잡기·체력·위상·쿠즈시 — 점수판에 없는 항목만 여기 남는다. 시계·절반·유효·
        /// 지도·누르기 카운트는 <see cref="RefreshScoreboard"/> 로 옮겨갔다.
        /// <paramref name="matteJustCalled"/> — 이번 Confirm() 호출에서 그쳐가
        /// 났으면 true. 그쳐는 <c>MatchClock.Phase</c> 에 남지 않는 순간 상태라
        /// (그쳐 직후 곧장 서기로 돌아간다) 호출부가 이번 교환의 이벤트를 보고
        /// 판단해서 넘겨줘야 한다 — 안 넘기면 "그쳐"가 화면에 뜰 기회가 없다.
        void RefreshStatusHud(bool matteJustCalled = false)
        {
            var eng = _runner.Engine;
            RefreshScoreboard(eng);

            _phaseText.text = "위상: " + (eng.Osaekomi.Active
                ? $"굳히기 — {FighterLabel(eng.Osaekomi.Holder)} 누름 {eng.Osaekomi.HeldSeconds}초 " +
                  $"(이탈 {eng.Osaekomi.EscapesSoFar}/{eng.Osaekomi.EscapesNeeded})"
                : matteJustCalled ? "그쳐" : "서기");

            _statusTextA.text = FighterStatus(eng, FighterId.A);
            _statusTextB.text = FighterStatus(eng, FighterId.B);
        }

        string FighterStatus(MatchEngine eng, FighterId who)
        {
            int stamina = eng.Stamina.Get(who);
            int grip = eng.Grip.EffectiveGrip(who);
            string left = GripLockLabel(eng.Grip.LockState(who, Hand.Left));
            string right = GripLockLabel(eng.Grip.LockState(who, Hand.Right));
            string kuzushi = _runner.Config.KuzushiGateEnabled
                ? $"  쿠즈시 {(eng.IsKuzushiActive(who) ? "유효" : "없음")}"
                : "";
            return $"{FighterLabel(who)}  체력 {stamina}  그립 {grip:+0;-0;0}  " +
                   $"왼손 {left} 오른손 {right}{kuzushi}";
        }

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

            BuildScoreboard(go.transform);
            BuildPhaseText(go.transform);
            BuildExposureBar(go.transform);
            BuildFighterStatusTexts(go.transform);
            BuildResultText(go.transform);
            BuildPreemptedText(go.transform);
            BuildSelectionText(go.transform);
            BuildExchangeLog(go.transform);
            _standingRoot = BuildStandingRoot(go.transform);
            _osaekomiRoot = BuildOsaekomiRoot(go.transform);
            _osaekomiRoot.SetActive(false);
        }

        /// 실전 유도 점수판 규격 (사진 레퍼런스). 좌표는 전부 이 루트 안의 로컬
        /// 오프셋이다 — 캔버스 기준 절대 좌표를 매 칸마다 다시 계산하지 않으려고
        /// 자체 컨테이너를 둔다(기존 ExposureBar 와 같은 패턴).
        void BuildScoreboard(Transform parent)
        {
            var root = new GameObject("Scoreboard", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rt = (RectTransform)root.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(460, 236);
            rt.anchoredPosition = new Vector2(0, -8);

            _bigTimerText = MakeText(root.transform, "0:00", TextAlignmentOptions.Center,
                new Vector2(0, 0), new Vector2(300, 60), new Vector2(0.5f, 1), fontSizeOverride: 44);
            _goldenScoreLabel = MakeText(root.transform, "", TextAlignmentOptions.Center,
                new Vector2(0, -62), new Vector2(300, 22), new Vector2(0.5f, 1), fontSizeOverride: 16);
            _goldenScoreLabel.color = Gold;

            _ipponStrip = MakeText(root.transform, "I\nP\nP\nO\nN", TextAlignmentOptions.Center,
                new Vector2(0, -90), new Vector2(40, 138), new Vector2(0f, 1), fontSizeOverride: 13);
            _ipponStrip.color = Dim;

            MakeText(root.transform, "W", TextAlignmentOptions.Center,
                new Vector2(104, -90), new Vector2(64, 20), new Vector2(0f, 1), fontSizeOverride: 13);
            MakeText(root.transform, "Y", TextAlignmentOptions.Center,
                new Vector2(176, -90), new Vector2(64, 20), new Vector2(0f, 1), fontSizeOverride: 13);
            MakeText(root.transform, "S/H", TextAlignmentOptions.Center,
                new Vector2(248, -90), new Vector2(90, 20), new Vector2(0f, 1), fontSizeOverride: 13);
            MakeText(root.transform, "누르기", TextAlignmentOptions.Center,
                new Vector2(346, -90), new Vector2(90, 20), new Vector2(0f, 1), fontSizeOverride: 13);

            (_wazaAriTextA, _yukoTextA, _shidoIconsA, _holdTextA) =
                BuildScoreRow(root.transform, "A", RowWhiteBg, Color.black, -112);
            (_wazaAriTextB, _yukoTextB, _shidoIconsB, _holdTextB) =
                BuildScoreRow(root.transform, "B", RowBlueBg, Color.white, -172);
        }

        /// 행 하나(A 또는 B) — 배경색 있는 바 + 라벨 + W/Y/누르기 값 + 지도 카드
        /// 칸. 절대 오프셋은 <see cref="BuildScoreboard"/> 의 칸 배치와 맞춰뒀다
        /// (헤더 x 좌표와 값 x 좌표가 같은 자리를 써야 줄이 맞는다).
        (TMP_Text wazaAri, TMP_Text yuko, Transform shidoIcons, TMP_Text hold) BuildScoreRow(
            Transform parent, string label, Color bg, Color fg, float yOffset)
        {
            var bar = new GameObject($"Row{label}", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(parent, false);
            var rt = (RectTransform)bar.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1);
            rt.pivot = new Vector2(0f, 1);
            rt.sizeDelta = new Vector2(420, 56);
            rt.anchoredPosition = new Vector2(40, yOffset);
            bar.GetComponent<Image>().color = bg;

            var labelText = MakeText(bar.transform, label, TextAlignmentOptions.Center,
                new Vector2(8, -8), new Vector2(50, 40), new Vector2(0f, 1), fontSizeOverride: 22);
            labelText.color = fg;

            var wazaAri = MakeText(bar.transform, "0", TextAlignmentOptions.Center,
                new Vector2(64, -8), new Vector2(64, 40), new Vector2(0f, 1), fontSizeOverride: 22);
            wazaAri.color = fg;

            var yuko = MakeText(bar.transform, "0", TextAlignmentOptions.Center,
                new Vector2(136, -8), new Vector2(64, 40), new Vector2(0f, 1), fontSizeOverride: 22);
            yuko.color = fg;

            var shidoRoot = new GameObject("ShidoIcons", typeof(RectTransform));
            shidoRoot.transform.SetParent(bar.transform, false);
            var shidoRt = (RectTransform)shidoRoot.transform;
            shidoRt.anchorMin = shidoRt.anchorMax = new Vector2(0f, 0.5f);
            shidoRt.pivot = new Vector2(0f, 0.5f);
            shidoRt.sizeDelta = new Vector2(90, 20);
            shidoRt.anchoredPosition = new Vector2(208, 0);

            var hold = MakeText(bar.transform, "00", TextAlignmentOptions.Center,
                new Vector2(306, -8), new Vector2(90, 40), new Vector2(0f, 1), fontSizeOverride: 22);
            hold.color = fg;

            return (wazaAri, yuko, shidoRoot.transform, hold);
        }

        /// §4.4 [초안]/§9 — 점수판 아래, 잡기·체력·위상·쿠즈시 (점수판으로 옮기지
        /// 않은 나머지). "그 외 표시" 요구사항 목록 그대로 별도 영역.
        void BuildPhaseText(Transform parent)
        {
            _phaseText = MakeText(parent, "", TextAlignmentOptions.Center,
                new Vector2(0, -252), new Vector2(700, 26), new Vector2(0.5f, 1));
        }

        void BuildFighterStatusTexts(Transform parent)
        {
            _statusTextA = MakeText(parent, "", TextAlignmentOptions.TopLeft,
                new Vector2(16, -334), new Vector2(430, 70), new Vector2(0f, 1));
            _statusTextB = MakeText(parent, "", TextAlignmentOptions.TopRight,
                new Vector2(-16, -334), new Vector2(430, 70), new Vector2(1f, 1));
        }

        void BuildExposureBar(Transform parent)
        {
            var root = new GameObject("ExposureBar", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rt = (RectTransform)root.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(500, 40);
            rt.anchoredPosition = new Vector2(0, -286);

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
                new Vector2(0, -412), new Vector2(900, 80), new Vector2(0.5f, 1));
        }

        /// [v1 임시 시스템 문구용 슬롯 — 카피 교체 목록에 올릴 것]
        void BuildPreemptedText(Transform parent)
        {
            _preemptedText = MakeText(parent, "", TextAlignmentOptions.Center,
                new Vector2(0, -502), new Vector2(900, 40), new Vector2(0.5f, 1));
            _preemptedText.color = Warning;
        }

        /// 4) 자기 선택 확인 — 확정 전에 지금 고른 손·코어·하체가 한 줄로 보인다.
        void BuildSelectionText(Transform parent)
        {
            _selectionText = MakeText(parent, "", TextAlignmentOptions.Center,
                new Vector2(0, -552), new Vector2(900, 30), new Vector2(0.5f, 1));
        }

        /// 3) 교환 로그 — 최근 5교환, 새 항목이 위로 쌓인다. 폴백 서술은
        /// <see cref="BuildOutcome"/> 이 판정 서술에 붙여둔 색 태그로 구분된다.
        void BuildExchangeLog(Transform parent)
        {
            _logText = MakeText(parent, "", TextAlignmentOptions.TopLeft,
                new Vector2(0, -592), new Vector2(900, 150), new Vector2(0.5f, 1));
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

            // 각 열의 anchorX 는 중심이고 폭은 BuildColumnRoot 안에서 ±0.14 로 고정이다 —
            // 이웃 열과 안 겹치려면 중심 간격이 0.28 이상이어야 한다. 손-코어는 0.32(여유
            // 0.04)라 괜찮았지만 코어-하체는 0.24(부족 −0.04)였다 — 실측(2026-08-11,
            // Play 모드 GetWorldCorners)으로 확인한 실제 겹침이다. 하체만 0.68 로 밀어
            // 간격을 0.02(코어-하체)·0.06(하체-확정버튼, anchorX 0.9) 으로 만든다.
            _handColumn = BuildColumnRoot(root.transform, "손", new Vector2(0.06f, 0.5f));
            _coreColumn = BuildColumnRoot(root.transform, "코어", new Vector2(0.38f, 0.5f));
            _legsColumn = BuildColumnRoot(root.transform, "하체", new Vector2(0.68f, 0.5f));

            var confirmBtn = MakeButton(root.transform, "확정", Confirm);
            var confirmRt = (RectTransform)confirmBtn.transform;
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
            var aRt = (RectTransform)_osaekomiOptionA.transform;
            aRt.anchorMin = aRt.anchorMax = new Vector2(0.35f, 0.5f);
            aRt.anchoredPosition = Vector2.zero;
            aRt.sizeDelta = new Vector2(280, 80);
            _osaekomiLabelA = _osaekomiOptionA.GetComponentInChildren<TMP_Text>();

            _osaekomiOptionB = MakeButton(root.transform, "", null);
            var bRt = (RectTransform)_osaekomiOptionB.transform;
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
                          Vector2 size, Vector2? anchor = null, bool stretch = false,
                          float? fontSizeOverride = null)
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
            t.fontSize = fontSizeOverride ?? fontSize;
            t.alignment = align;
            t.text = text;
            t.raycastTarget = false;
            return t;
        }
    }
}
