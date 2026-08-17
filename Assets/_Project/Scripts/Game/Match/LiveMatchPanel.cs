using System.Collections.Generic;
using System.Linq;
using CounterTime.Brain;
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
    /// 화면 골격은 상단(점수판)/좌상(경기 화면 자리)/좌하(서술+로그)/우측(사이드바 3종:
    /// 코치의 조언/수 노트/내 성향)/하단(입력) 5블록이다. 좌표를 손으로 찍지 않고
    /// Unity <c>VerticalLayoutGroup</c>/<c>HorizontalLayoutGroup</c> 이 실제로 계산하게
    /// 만든다 — 지난 anchorX 산술 버그(2026-08-11 오후 4차, 반폭 기준 안 맞았던 것)의
    /// 재발 방지가 이 구조의 이유다.
    ///
    /// <b>B 는 여기 안 나온다.</b> B 의 선택지·습관·심판 누산기 숫자는 이 패널 어디에도
    /// 읽거나 그리지 않는다 — 읽을 수 있는 것은 <see cref="LiveMatchRunner"/> 가 공개한
    /// A 자신의 면(<see cref="LiveAssembler"/>, <see cref="LiveMatchRunner.Tendency"/>)과
    /// A 가 실제로 관측한 B 의 습관(<see cref="LiveMatchRunner.Ledger"/>)뿐이다.
    ///
    /// 기능 우선, 미려함은 뒤로 — 색과 배치는 최소한이다. 화면 실행 확인은 사람 몫
    /// (Unity Test Runner 로도, 헤드리스로도 캔버스는 검증 불가).
    /// </summary>
    [RequireComponent(typeof(LiveMatchRunner))]
    [RequireComponent(typeof(ArenaStageView))]
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
        static readonly Color PanelBg = new Color(0.16f, 0.16f, 0.18f, 0.92f);

        LiveMatchRunner _runner;

        HandTarget _hand = HandTarget.None;
        CoreStance _core = CoreStance.Neutral;
        LegAction _legs = LegAction.Neutral;
        // §A.1 손 분리(v4.0) — 손 축은 2단 선택이다: 먼저 왼손/오른손을 고르고(스테이지1),
        // 그 손 기준 타겟 목록을 고른다(스테이지2). _ownHand 는 확정된 값(Build 에 실제로
        // 들어감), _pendingOwnHand 는 스테이지1에서 고른 뒤 스테이지2 목록을 계산하는 동안의
        // 임시값 — 스테이지2에서 타겟을 고를 때 비로소 _ownHand 로 승격된다. 미지시 손은
        // §A.1 확정대로 자동으로 그립을 유지한다(Core 가 원래 그렇게 동작한다 — 그 턴에
        // TurnChoice 가 안 건드리는 손은 GripSystem 상태가 그대로 남는다. 새 배선 불필요).
        Hand _ownHand = Hand.Right;
        Hand _pendingOwnHand = Hand.Right;
        bool _handStageTarget;

        // §2b v4.2 — GripRevisionGate 켜졌을 때만 쓰는 손 축 상태기계. off일 때의
        // 흐름(_handStageTarget 기반, 위)은 손 안 댔다 — 완전히 별도 분기.
        // 종착점은 4항목(오른손/왼손/회피/뜯기[2차 예약, 숨김)이라 손 자체가
        // 아니라 "무엇을 할지" 최상위 선택이 됐다(회피가 손 하나가 아니라 양손을
        // 동시에 쓰는 별도 개념이라 §A.1의 손 목록 안에 못 넣는다 — IMPACT_grip-
        // revision.md 참조).
        //
        // 2026-08-18 — PickTarget(대상 6종 평면 목록)을 PickGripTier(대상 3종)→
        // PickOrientation(정/교차 2종)으로 쪼갰다. MATCH_RULES §2b 가 "소매/가슴깃/
        // 틀어잡기 3종 × 정/교차"라고 명시한 그 2단 구조를 그대로 따른 것 — 이전
        // 구현은 이 6개를 좌우 존 이름이 그대로 드러나는 평면 목록으로 냈는데, 그게
        // 애초에 잡기 개편이 없애려던 "내 기준/상대 기준 좌우 혼동"을 화면에 그대로
        // 되살렸다(사용자 신고). PickHandOrEvade→(PickGripTier→PickOrientation |
        // PickEvadeZoneLeft→PickEvadeZoneRight) 순서.
        enum HandWheelStage { PickHandOrEvade, PickGripTier, PickOrientation, PickEvadeZoneLeft, PickEvadeZoneRight }
        HandWheelStage _handStage = HandWheelStage.PickHandOrEvade;
        DefenseZone _evadeZoneLeft, _evadeZoneRight;

        // 2026-08-18 — MATCH_RULES §2b "대상 3종"(소매/가슴깃/틀어잡기). 스테이지2가
        // 이 중 하나를 고르면 스테이지3(정/교차)로 넘어가고, 그때 비로소
        // <see cref="ResolveGripTarget"/> 이 구체적인 <see cref="HandTarget"/>(예:
        // LeftSleeve)으로 기계적 환산한다 — 좌우 존 이름은 그 환산의 출력일 뿐,
        // 화면 어디에도 직접 노출하지 않는다(사용자 지시).
        enum GripTier { Sleeve, Lapel, Twist }
        GripTier _pendingGripTier;
        enum GripOrientation { Mirror, Cross }

        enum HandTopChoice { Right, Left, Evade, Tear }
        // 2026-08-14 밤 8차 — 고정 각도표(95°/180°/265°, 반지름 40) 폐기. 실플레이
        // 스크린샷으로 "회피"가 화면 왼쪽 끝에서 잘린다는 신고를 받아 확인한 결과,
        // 지난 세션이 받아들인 22px 클립이 실제 체감상 불가 판정 — 겹침 0·화면
        // 이탈 0을 항목 수와 무관하게 보장하는 실측 기반 배치(아래 §"극좌표 배치")
        // 로 교체했다. 손 축 스테이지1도 이제 다른 모든 축과 완전히 같은
        // <see cref="RebuildAxisOptions{T}"/> 를 그대로 쓴다 — 고정 각도표 자체가
        // 필요 없어졌다: 새 알고리즘이 항목이 3개든(지금) 4개든(뜯기 2차 추가 후)
        // 실측 라벨 폭으로 매번 다시 계산하므로 "항목 수 무관 불변식"이 저절로
        // 성립한다. 2026-08-18 — 전용 아크(옛 arcStartDeg=90, "A 반대편 안전 반원")
        // 특례도 제거했다: A 겹침은 이미 딤 처리로 전 축 공통 허용된 상태라 손
        // 스테이지1만 따로 A 를 피할 이유가 없었고, 오히려 이 특례가 트리거의
        // 화면 왼쪽 끝 방향(θ=180)을 아크 한가운데 놓아 세이프 마진과 정면충돌하는
        // 원인이었다(9차 세션이 결국 이 축만 세로 스택으로 떨어뜨린 이유). 지금은
        // 모든 축이 <see cref="SolvePolarLayout"/> 하나가 관리하는 "θ=180 원뿔만
        // 비우는 원형 배치"를 공유한다.
        const float HandTopBaseRadius = 40f;
        static string HandTopLabel(HandTopChoice c) => c switch
        {
            HandTopChoice.Right => "오른손",
            HandTopChoice.Left => "왼손",
            HandTopChoice.Evade => "회피",
            HandTopChoice.Tear => "뜯기",
            _ => "—",
        };

        // 2026-08-12 실전 UI 개편 — 손·코어·하체 상시 노출 3열 폐지 → 캐릭터 옆
        // 트리거+드롭다운(주체 기준=전부 A 옆, 화면 고정 위치=본 추적 안 함) →
        // 같은 날 후속: 위로 펼치는 드롭다운이 A 모델을 가리고 손 목록이 화면 밖으로
        // 잘린다는 Play 실측 신고로 방사형(휠) 메뉴로 재전환. 상세 근거는 STATUS.md.
        Button _handTrigger, _coreTrigger, _legsTrigger, _techniqueTrigger, _confirmButton;
        // 트리거 자체는 축 이름만 표시하는 작은 버튼(휠 반지름 여유 확보용, 아래
        // BuildAxisControl 참조) — 현재 선택은 트리거 바로 아래 별도 텍스트로 낸다.
        TMP_Text _handSummary, _coreSummary, _legsSummary, _techniqueSummary;
        Transform _handDropdown, _coreDropdown, _legsDropdown, _techniqueDropdown;
        GameObject _floatingControlsRoot;
        // §H 2b — 기술 지정. null = 미지정(§H 2단계 "공격자 판정 통일" 그대로: 하체=공격을
        // 골라도 기술을 안 골랐으면 공격 커밋 없음). 기술을 고르면 그 턴은 손·코어 입력이
        // 없다(§A.2) — 손·코어를 강제로 None/중립으로 접고 손·코어 트리거를 잠근다.
        string _techniqueId;
        bool _techniqueChosen;
        // 2026-08-12 방사형 메뉴 실측 후속 — 부채꼴이 오른쪽으로 펼쳐지는 이상 A 의
        // 실측 화면 폭(x 47~806, 1920 폭의 약 40%)과 트리거 위치(x=25) 특성상(±90도
        // 지점조차 반지름과 무관하게 x≈25 부근이라 A 의 왼쪽 끝과 항상 겹친다) 겹침 자체를
        // 반지름 조정으로 없앨 수 없다는 게 확인됐다. 사용자 결정: 겹침을 인정하고 휠이
        // 열린 동안만 배경을 어둡게 해 위 계층(선택지)을 시각적으로 분리한다 — LoL
        // 핑 휠/스파이더맨 웹 기술 휠도 게임 화면 위에 그대로 겹쳐 뜬다(참고 사례와
        // 같은 방식). 반지름·각도는 무수정.
        RawImage _arenaBackground;
        static readonly Color ArenaBright = Color.white;
        static readonly Color ArenaDim = new Color(0.45f, 0.45f, 0.45f, 1f);
        // 확정 버튼 활성화 게이트 — 코어·하체는 기본값(중립)이 이미 "유효한 선택"이라
        // _core/_legs 필드만으로는 "사용자가 실제로 골랐는지"를 못 가른다. 드롭다운에서
        // 뭔가 하나라도 클릭해야(같은 값을 다시 골라도) true 가 된다.
        bool _handChosen, _coreChosen, _legsChosen;
        GameObject _standingRoot, _osaekomiRoot;
        Transform _notesColumn;
        TMP_Text _resultText, _logText, _adviceText;
        TMP_Text _phaseText, _statusTextA, _statusTextB;
        Image _warningMark, _dangerMark;
        TMP_Text _warningLabel, _dangerLabel;
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
            // ArenaStageView 도 같은 GameObject 의 컴포넌트라 Awake 순서가 보장되지
            // 않는다 — BuildCanvas 가 stage.Output 을 읽기 전에 직접 완성시켜둔다.
            GetComponent<ArenaStageView>().EnsureBuilt();
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

        /// §H 3.5b — called by MetaFlowController right after <see cref="LiveMatchRunner.Rebuild"/>,
        /// once per bracket match. Clears the previous match's log/result text and re-runs
        /// the same refresh <see cref="Start"/> does — the panel itself is never destroyed
        /// between matches, so nothing here resets on its own.
        public void ResetForNewMatch()
        {
            _log.Clear();
            _logText.text = "";
            _resultText.text = "";
            if (_osaekomiRoot.activeSelf) OnOsaekomiClosed();
            RefreshStanding();
            RefreshStatusHud();
        }

        // ── 판정 서술 ────────────────────────────────────────────────────────────

        void OnPresented(MatchEvent e) => _pendingEvents.Add(e);

        /// 서술 문구는 여기서 화면에 바로 안 그린다 — <see cref="BuildOutcome"/> 이
        /// 우선순위(선점 > 판정 서술 > 이벤트 요약)를 정해 서술 줄/로그에 같은 문자열을
        /// 한 번에 낸다. 여기서 두 번 칠하면 두 표시가 서로 다른 순간에 갱신될 수 있다.
        void OnNarrated(ResultNarrationTable.Result r)
        {
            _pendingNarrationText = r.Text;
            _pendingNarrationFallback = r.IsFallback;
        }

        /// [v1 임시 시스템 문구 — 카피 교체 목록에 올릴 것, md/STATUS.md 결정 대기 참조]
        /// 화면 표시는 <see cref="BuildOutcome"/> 을 통해 서술 줄/로그로만 나간다 —
        /// 별도 배너는 같은 문장을 세 번째로 중복 표시하는 것이라 뺐다.
        void OnAPreempted() => _pendingPreempted = true;

        // ── 굳히기 전환 ──────────────────────────────────────────────────────────

        void OnOsaekomiOpened()
        {
            _standingRoot.SetActive(false);
            _floatingControlsRoot.SetActive(false);
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
            _floatingControlsRoot.SetActive(true);
            RefreshStanding();
            RefreshStatusHud();
        }

        /// §9.3 — 5초 틱 하나 제출. 결과 이벤트를 모아 서술 줄·교환 로그에 반영하고
        /// HUD 를 새로고침한다. 굳히기가 계속 열려 있으면(<see cref="OnOsaekomiOpened"/>
        /// 가 다시 안 불린다) 홀더가 안 바뀌므로 버튼 라벨은 그대로 둔다.
        void SubmitOsaekomiChoice(OsaekomiDefenderChoice defenderChoice, OsaekomiHolderChoice holderChoice,
                                  string myChoiceLabel)
        {
            BeginPending();
            _runner.SubmitOsaekomi(defenderChoice, holderChoice);
            string outcome = BuildOutcome();
            _resultText.text = outcome;
            PushLog(myChoiceLabel, outcome);
            RefreshStatusHud();
        }

        // ── 캐릭터 옆 드롭다운 입력 ──────────────────────────────────────────────

        void RefreshStanding()
        {
            _hand = HandTarget.None;
            _core = CoreStance.Neutral;
            _legs = LegAction.Neutral;
            _ownHand = Hand.Right;
            _pendingOwnHand = Hand.Right;
            _handStageTarget = false;
            _handStage = HandWheelStage.PickHandOrEvade;
            _pendingGripTier = default;
            _evadeZoneLeft = _evadeZoneRight = default;
            _handChosen = _coreChosen = _legsChosen = false;
            _techniqueId = null;
            _techniqueChosen = false;
            RebuildStandingColumns();
            RefreshExposure();
            RefreshConfirmButton();
            RefreshHandCoreLock();
        }

        // 손 축 반지름 130 은 A(캔버스 x=179 실측)와 24px 여유를 두도록 확정된 값
        // (2026-08-12 사용자 확정) — 트리거를 축 이름만 표시하는 작은 버튼으로 만들어
        // 이 여유를 그대로 지켰다(트리거가 넓으면 휠 원점이 A 쪽으로 밀려 여유가
        // 줄어든다 — 아래 BuildAxisControl 주석 참조). 코어·하체는 항목이 적어
        // 반지름을 더 좁혀도 간격이 넉넉하다(실측: 코어 90→64px 여유, 하체 100→54px).
        // HandSideRadius(스테이지1, 왼손/오른손 2개)는 항목이 정확히 2개일 때 배치
        // 공식(-90°~+90° 균등분배)이 각 항목을 정확히 ±90°에 놓는다는 실측에 근거한다
        // — cos(90°)=0 이라 반지름을 얼마로 잡든 A 쪽(x축)으로는 전혀 안 뻗는다.
        // 70 은 두 버튼(세로 36) 사이에 여유를 두기 위한 값일 뿐 A 와의 겹침과 무관하다.
        const float HandRadius = 130f, HandSideRadius = 70f, CoreRadius = 90f, LegsRadius = 100f;
        // §H 2b — 기술 트리거(별도 4번째 트리거, 사용자 확정). 8개(모두걸기·발목받치기·
        // 나오는발차기·발뒤축걸기·밭다리걸기·허리후리기·안오금띄기·어깨 매치기)에
        // "곡률 완만함 vs 세로 안전구간 여유" 트레이드오프를 실측·보고 후 R=120 확정
        // (인접 버튼 겹침 22px, 세로 여유 12px — 기존 손 축의 인접 겹침 28px보다 작다).
        const float TechniqueRadius = 120f;

        static readonly Hand[] HandSideOptions = { Hand.Left, Hand.Right };
        static string HandSideLabel(Hand h) => h == Hand.Left ? "왼손" : "오른손";

        // 2026-08-18 — MATCH_RULES §2b "대상 3종 × 정/교차" 2단 선택의 라벨·환산.
        static string GripTierLabelOf(GripTier t) => t switch
        {
            GripTier.Sleeve => "소매",
            GripTier.Lapel => "가슴깃",
            GripTier.Twist => "틀어잡기",
            _ => "—",
        };

        static string GripOrientationLabel(GripOrientation o) =>
            o == GripOrientation.Cross ? "교차잡기" : "정잡기";

        /// <paramref name="tier"/>+<paramref name="ownHand"/>+<paramref name="orientation"/>
        /// → 구체적인 <see cref="HandTarget"/>(예: LeftSleeve). <see cref="CounterTime.
        /// Core.GripSystem.IsCrossOrientation(Hand,HandTarget)"/> 의 side 매핑을 그대로
        /// 뒤집어 쓴다 — 정잡기(내 오른손→상대 왼쪽) = mirror, 교차잡기(같은측) = cross
        /// (§2b 원문 그대로, GripSystem 의 static 헬퍼가 이미 검증해둔 매핑).
        /// GripSystem.Other 는 private 라 못 재사용한다 — 이 파일의 다른 작은 매핑들
        /// (예: HandLabel)과 같은 이유로 한 줄 중복을 그대로 둔다.
        static HandTarget ResolveGripTarget(GripTier tier, Hand ownHand, GripOrientation orientation)
        {
            bool cross = orientation == GripOrientation.Cross;
            Hand targetSide = cross ? ownHand : (ownHand == Hand.Left ? Hand.Right : Hand.Left);
            return tier switch
            {
                GripTier.Sleeve => targetSide == Hand.Right ? HandTarget.RightSleeve : HandTarget.LeftSleeve,
                GripTier.Lapel => targetSide == Hand.Right ? HandTarget.RightLapel : HandTarget.LeftLapel,
                GripTier.Twist => targetSide == Hand.Right ? HandTarget.TwistGrip : HandTarget.BackGrab,
                _ => HandTarget.None,
            };
        }

        /// 역방향 — 이미 확정된 <see cref="HandTarget"/> 에서 요약 표기용 대상 급 이름을
        /// 되짚는다(§2b 흐름 전용, 견고화·흔들기·회피는 null — 그쪽은 급 개념이 없다).
        static string GripTierLabelFor(HandTarget t) => t switch
        {
            HandTarget.RightSleeve or HandTarget.LeftSleeve => "소매",
            HandTarget.RightLapel or HandTarget.LeftLapel => "가슴깃",
            HandTarget.TwistGrip or HandTarget.BackGrab => "틀어잡기",
            _ => null,
        };

        void RebuildStandingColumns()
        {
            // 손 축은 2단 선택(§A.1, v4.0) — 스테이지1(손 고르기)은 고른 즉시 닫지 않고
            // (closeOnPick: false) 스테이지2(타겟 고르기)로 넘어간다. 스테이지2에서 타겟을
            // 고르면 그제서야 _ownHand 가 확정되고 휠이 닫힌다(기본 closeOnPick: true).
            //
            // §2b v4.2 — GripRevisionGate 켜지면 완전히 다른 상태기계(HandWheelStage,
            // 위 필드 선언부 참조)를 쓴다. off일 때의 흐름은 아래 그대로 무수정.
            if (_runner.A.RevisedFlowActive)
            {
                switch (_handStage)
                {
                    case HandWheelStage.PickHandOrEvade:
                        // 뜯기는 2차 예약 — 지금은 목록 자체에서 뺀다(HandTopSlots 고정
                        // 각도표가 하던 "숨김"을 목록 필터링으로 대체, 위 필드 선언부 참조).
                        RebuildAxisOptions(_handDropdown, HandTopBaseRadius,
                            new[] { HandTopChoice.Right, HandTopChoice.Left, HandTopChoice.Evade },
                            HandTopLabel, o => false, o => PickHandTop((HandTopChoice)o),
                            closeOnPick: false);
                        break;

                    case HandWheelStage.PickGripTier:
                        // 2026-08-18 — §2b "대상 3종"만 낸다(좌우 존 이름은 화면에 안
                        // 낸다, 사용자 지시). 견고화·흔들기는 급 개념이 없는 독립 선택지라
                        // 같은 목록에 섞이지만, 고르면 스테이지3(정/교차)으로 안 넘어가고
                        // 그 자리에서 바로 확정된다 — 급 선택(closeOnPick:false, 아래
                        // 목록 전체에 적용)과 독립 선택(즉시 닫힘)이 한 목록에 섞여 있어
                        // RebuildAxisOptions 의 균일한 closeOnPick 하나로는 표현이 안 돼,
                        // PickGripStage2 가 독립 선택 분기에서 직접 CloseAllWheels 를 부른다.
                        var revisedOptions = _runner.A.HandOptionsRevised(_pendingOwnHand);
                        var stage2Items = new List<object> { GripTier.Sleeve, GripTier.Lapel, GripTier.Twist };
                        if (revisedOptions.Contains(HandTarget.Solidify)) stage2Items.Add(HandTarget.Solidify);
                        if (revisedOptions.Contains(HandTarget.Setup)) stage2Items.Add(HandTarget.Setup);
                        RebuildAxisOptions(_handDropdown, HandRadius, stage2Items,
                            o => o is GripTier gt ? GripTierLabelOf(gt) : HandLabel((HandTarget)o),
                            o => false, PickGripStage2, closeOnPick: false);
                        break;

                    case HandWheelStage.PickOrientation:
                        // 정/교차 2택 — 교차만 노란 표기(사용자 지시, "존별로 노랑"이던
                        // 옛 방식 대신 이 축 자체가 교차인지만 표시). ResolveGripTarget 이
                        // 여기서 비로소 구체적인 HandTarget(예: LeftSleeve)으로 환산한다.
                        RebuildAxisOptions(_handDropdown, HandSideRadius,
                            new[] { GripOrientation.Mirror, GripOrientation.Cross },
                            GripOrientationLabel, o => false,
                            o => { _hand = ResolveGripTarget(_pendingGripTier, _pendingOwnHand, (GripOrientation)o);
                                   _ownHand = _pendingOwnHand; _handChosen = true;
                                   RebuildStandingColumns(); RefreshExposure(); RefreshConfirmButton(); },
                            tintOverride: o => o == GripOrientation.Cross ? (Color?)ShidoYellow : null);
                        break;

                    case HandWheelStage.PickEvadeZoneLeft:
                        RebuildAxisOptions(_handDropdown, LegsRadius, _runner.A.EvadeZoneOptions(Hand.Left),
                            DefenseZoneLabel, o => false,
                            o => { _evadeZoneLeft = (DefenseZone)o; _handStage = HandWheelStage.PickEvadeZoneRight;
                                   RebuildStandingColumns(); },
                            closeOnPick: false);
                        break;

                    case HandWheelStage.PickEvadeZoneRight:
                        // §2b — "중복 불가": 왼손이 이미 고른 존은 오른손 목록에서 뺀다.
                        var rightZones = _runner.A.EvadeZoneOptions(Hand.Right)
                            .Where(z => z != _evadeZoneLeft).ToList();
                        RebuildAxisOptions(_handDropdown, LegsRadius, rightZones, DefenseZoneLabel, o => false,
                            o => { _evadeZoneRight = (DefenseZone)o; _hand = HandTarget.Evade; _handChosen = true;
                                   RebuildStandingColumns(); RefreshExposure(); RefreshConfirmButton(); });
                        break;
                }
            }
            else if (_handStageTarget)
            {
                RebuildAxisOptions(_handDropdown, HandRadius, _runner.A.HandOptions(_pendingOwnHand), HandLabel,
                    o => _hand.Equals(o),
                    o => { _hand = (HandTarget)o; _ownHand = _pendingOwnHand; _handChosen = true;
                           RebuildStandingColumns(); RefreshExposure(); RefreshConfirmButton(); });
            }
            else
            {
                RebuildAxisOptions(_handDropdown, HandSideRadius, HandSideOptions, HandSideLabel, o => false,
                    o => { _pendingOwnHand = (Hand)o; _handStageTarget = true; RebuildStandingColumns(); },
                    closeOnPick: false);
            }
            RebuildAxisOptions(_coreDropdown, CoreRadius, _runner.A.CoreOptions(), CoreLabel, o => _core.Equals(o),
                o => { _core = (CoreStance)o; _coreChosen = true; RebuildStandingColumns(); RefreshExposure(); RefreshConfirmButton(); });
            RebuildAxisOptions(_legsDropdown, LegsRadius, _runner.A.LegOptions(), LegsLabel, o => _legs.Equals(o),
                o =>
                {
                    _legs = (LegAction)o;
                    _legsChosen = true;
                    // §H 2b — 하체 휠에서 직접 다른 값(중립/수비/카운터 준비)을 고르면
                    // 그건 더 이상 기술 턴이 아니다. 남아있던 기술 지정을 지우고 손·코어
                    // 잠금도 풀어야 한다 — 안 지우면 "수비를 골랐는데 화면엔 여전히
                    // 기술이 지정돼 있다"는 앞뒤 안 맞는 상태가 남는다. 다시 "공격"을
                    // 고르는 것만으로는(기술 트리거를 거치지 않았으면) 지워질 게 없다.
                    if (_legs != LegAction.Attack) { _techniqueId = null; _techniqueChosen = false; }
                    RebuildStandingColumns(); RefreshExposure(); RefreshConfirmButton(); RefreshHandCoreLock();
                });
            // §H 2b — 별도 4번째 트리거(사용자 확정, §H 2단계-A 안②). A 의 8기술
            // (LiveAssembler.TechniqueOptions, §B Owner=A 전부) 중 하나를 고르면 그 자체로
            // "공격을 선언하고 그 기술을 지정한다"는 뜻이다(§A.2) — 하체 축을 따로 다시
            // 방문해 "공격"을 고를 필요가 없다. 손·코어는 §A.2("기술 턴은 손·코어 입력이
            // 없다")대로 None/중립으로 접고 잠근다(RefreshHandCoreLock). 확정 게이트 3개를
            // 전부 true 로 만들어 "확정 즉시 가능"을 만족시킨다(2026-08-12 §H 2b 사용자 확정).
            RebuildAxisOptions(_techniqueDropdown, TechniqueRadius, _runner.A.TechniqueOptions(),
                (TechniqueDef d) => d.DisplayName, o => ((TechniqueDef)o).Id == _techniqueId,
                o =>
                {
                    var def = (TechniqueDef)o;
                    _techniqueId = def.Id;
                    _techniqueChosen = true;
                    _legs = LegAction.Attack; _legsChosen = true;
                    _hand = HandTarget.None; _handChosen = true;
                    _core = CoreStance.Neutral; _coreChosen = true;
                    RebuildStandingColumns(); RefreshExposure(); RefreshConfirmButton(); RefreshHandCoreLock();
                });

            // 휠이 닫혀 있어도 축 상태가 한눈에 보이도록 트리거 아래 요약 텍스트에
            // 반영한다(2026-08-12 방사형 개편 — 트리거 자체는 축 이름만 표시하는
            // 작은 버튼이라 선택 내용은 별도 줄로 뺐다. 여전히 "닫힌 상태에서도
            // 트리거 쪽만 보면 축이 다 읽힌다"는 요구는 만족한다).
            _handSummary.text = !_handChosen ? "—" : HandChoiceSummary();
            _coreSummary.text = CoreLabel(_core);
            _legsSummary.text = LegsLabel(_legs);
            _techniqueSummary.text = _techniqueChosen ? TechniqueRegistry.Get(_techniqueId).DisplayName : "—";
        }

        /// 손 축 확정 표기 — 요약 텍스트·확정 로그가 공유한다(출처 하나, 2026-08-18
        /// 사용자 지시 "왼손 · 가슴깃 정잡기" 형식). §2b 흐름의 소매/가슴깃/틀어잡기
        /// 대상만 급+정/교차 형식을 쓰고(<see cref="GripTierLabelFor"/> 가 null 이 아닐
        /// 때만), 회피·견고화·흔들기·게이트 off 구식 흐름은 기존 형식 그대로 — 이
        /// 조건이 게이트 off 흐름(같은 HandTarget 값들을 그대로 재사용)을 실수로
        /// 같이 바꾸지 않게 막는다.
        string HandChoiceSummary()
        {
            if (_hand == HandTarget.Evade && _runner.A.RevisedFlowActive)
                return $"회피 · {DefenseZoneLabel(_evadeZoneLeft)}/{DefenseZoneLabel(_evadeZoneRight)}";
            string tier = _runner.A.RevisedFlowActive ? GripTierLabelFor(_hand) : null;
            if (tier != null)
            {
                bool cross = CounterTime.Core.GripSystem.IsCrossOrientation(_ownHand, _hand);
                return $"{HandSideLabel(_ownHand)} · {tier} {(cross ? "교차잡기" : "정잡기")}";
            }
            return $"{HandSideLabel(_ownHand)} · {HandLabel(_hand)}";
        }

        /// §2b v4.2 — 손 축 스테이지1 선택 처리. closeOnPick 개념은 없다 — 이 메서드
        /// 자체가 CloseAllWheels 를 안 부르므로 자연히 안 닫힌다(스테이지2로 이어짐,
        /// 기존 stage1→stage2 관행과 동일). 배치 자체는 이제 다른 모든 축과 완전히
        /// 같은 <see cref="RebuildAxisOptions{T}"/>(위 호출부)가 맡는다.
        void PickHandTop(HandTopChoice choice)
        {
            if (choice == HandTopChoice.Evade)
            {
                _handStage = HandWheelStage.PickEvadeZoneLeft;
            }
            else
            {
                _pendingOwnHand = choice == HandTopChoice.Right ? Hand.Right : Hand.Left;
                _handStage = HandWheelStage.PickGripTier;
            }
            RebuildStandingColumns();
        }

        /// §2b 스테이지2(<see cref="HandWheelStage.PickGripTier"/>) 선택 처리 — 목록에
        /// 급(<see cref="GripTier"/>)과 독립 선택지(견고화·흔들기)가 섞여 있어 분기한다.
        /// 급을 고르면 스테이지3(정/교차)으로 넘어가고(휠 안 닫힘), 독립 선택지는 그
        /// 자리에서 바로 확정하고 닫는다.
        void PickGripStage2(object choice)
        {
            if (choice is GripTier tier)
            {
                _pendingGripTier = tier;
                _handStage = HandWheelStage.PickOrientation;
                RebuildStandingColumns();
            }
            else if (choice is HandTarget standalone)
            {
                _hand = standalone;
                _ownHand = _pendingOwnHand;
                _handChosen = true;
                RebuildStandingColumns();
                RefreshExposure();
                RefreshConfirmButton();
                CloseAllWheels();
            }
        }

        /// §H 2b — 기술을 지정한 턴은 손·코어 입력이 없다(§A.2). 손·코어 트리거를
        /// 잠가(interactable=false) 열 수 없게 하고 색을 죽여 "지금은 못 만진다"를
        /// 표시한다. 잠금은 트리거의 <c>interactable</c>/색만 건드린다 — 휠 열림/닫힘
        /// (<see cref="OpenWheel"/>/<see cref="CloseAllWheels"/>) 이 쓰는
        /// <c>SetActive</c> 와 다른 속성이라 서로 안 부딪힌다.
        void RefreshHandCoreLock()
        {
            _handTrigger.interactable = !_techniqueChosen;
            _coreTrigger.interactable = !_techniqueChosen;
            var lockedColor = _techniqueChosen ? Dim : Idle;
            _handTrigger.GetComponent<Image>().color = lockedColor;
            _coreTrigger.GetComponent<Image>().color = lockedColor;
        }

        void Confirm()
        {
            bool evading = _hand == HandTarget.Evade && _runner.A.RevisedFlowActive;
            string myChoiceLabel = _techniqueChosen
                ? $"기술: {TechniqueRegistry.Get(_techniqueId).DisplayName}"
                : $"{HandChoiceSummary()} · {CoreLabel(_core)} · {LegsLabel(_legs)}";
            BeginPending();
            // §2b v4.2 — 회피는 손 하나가 아니라 손별 존 2개를 동시에 싣는다(§2b).
            // Build()의 단일 hand/ownHand 시그니처로는 존을 못 실어서 BuildEvade 를
            // 따로 쓴다.
            var choice = evading
                ? _runner.A.BuildEvade(_evadeZoneLeft, _evadeZoneRight, _core, _legs)
                : _runner.A.Build(_hand, _core, _legs, _ownHand);
            _runner.SubmitA(choice, _techniqueId);
            string outcome = BuildOutcome();
            bool matte = _pendingEvents.Exists(e => e is MatteCalled);
            _resultText.text = outcome;
            PushLog(myChoiceLabel, outcome);
            if (!_runner.Engine.Osaekomi.Active) RefreshStanding();
            RefreshStatusHud(matte);
        }

        /// <summary>§3 노출 막대 — 지금 <b>선택 중인</b>(아직 확정 전) 조합이 경고선/위험선에
        /// 걸리는지를 매 선택마다 다시 계산한다. 읽히기 전에 보여야 하므로, 확정 버튼을
        /// 누르기 전에 이미 위험을 알 수 있어야 한다. 임계값 로직은 무수정 — 우측
        /// 사이드바로 이관하며 라벨을 상시 표시로 바꾼 것뿐이다.</summary>
        void RefreshExposure()
        {
            var candidate = _runner.A.Build(_hand, _core, _legs, _ownHand);
            bool warn = _runner.Tendency.MajorityMatches(candidate);
            bool danger = _runner.Tendency.ConsecutiveMatches(candidate);

            _warningMark.color = warn ? Warning : Dim;
            _warningLabel.color = warn ? Warning : Dim;
            _dangerMark.color = danger ? Danger : Dim;
            _dangerLabel.color = danger ? Danger : Dim;
        }

        /// 확정 버튼 — 3축(손·코어·하체) 전부 선택되기 전엔 비활성+Idle 회색(다른
        /// 비활성 버튼과 시각 언어 통일 — 이 앱에서 회색은 이미 "아직 안 고른 상태"를
        /// 뜻한다). 전부 선택되면 활성화+ShidoYellow(2026-08-12 사용자 확정 — 노란색.
        /// 새 색 상수를 안 만들고 지도 카드에 이미 쓰는 순수 노랑을 재사용).
        void RefreshConfirmButton()
        {
            bool allChosen = _handChosen && _coreChosen && _legsChosen;
            _confirmButton.interactable = allChosen;
            _confirmButton.GetComponent<Image>().color = allChosen ? ShidoYellow : Idle;
        }

        /// 항목을 반지름 <paramref name="radius"/> 짜리 원(트리거 중심)에 배치한다 —
        /// 트리거 자신이 놓인 방향(θ=180, 화면 왼쪽 끝)만 원뿔 모양으로 비우고
        /// 나머지 거의 전체 원을 쓴다. 자세한 알고리즘은 <see cref="SolvePolarLayout"/>.
        ///
        /// 2026-08-14 밤 8차 — 항목 수 기준 균등분배(옛 <c>-90+idx*(180/(n-1))</c>)를
        /// 실측 라벨 폭 기반 배치로 교체했다. 균등분배는 항목마다 라벨 길이가 달라도
        /// 같은 각도 폭을 준다는 전제라, 긴 라벨은 옆 항목과 겹치고 짧은 라벨은 공간을
        /// 낭비했다(기술 휠 8항목의 "모두걸기·발목받치기" 겹침 신고, 손 스테이지1의
        /// "회피" 화면 밖 클립 신고 — 둘 다 이 전제가 원인). 새 방식은
        /// <see cref="SolvePolarLayout"/> 이 (1) 각 라벨의 실측 폭(TMP
        /// GetPreferredValues, 추측 아님)을 재고 (2) 반지름을 필요한 만큼 늘려 각도
        /// 겹침을 없애고 (3) 그래도 화면 세이프 영역을 벗어나면 라벨 폭 상한을 좁혀
        /// (자동 폰트 축소가 뒤따른다) 재시도한다 — 항목 수·라벨 길이가 나중에
        /// 바뀌어도(뜯기 2차 추가, 다른 기술 목록 등) 매번 다시 계산되므로 불변식이
        /// 저절로 유지된다.
        ///
        /// <paramref name="closeOnPick"/> 이 false 면 고른 뒤에도 휠을 안 닫는다 — 손 축
        /// 스테이지1(손 고르기)이 스테이지2(타겟 고르기)로 이어지는 데 쓴다. 기본값
        /// true 는 기존 동작(고르면 즉시 닫힘) 그대로다.
        /// <paramref name="tintOverride"/> — §2b v4.2 추가, 순수 선택 사항(기본 null).
        /// 선택되지 않은 항목의 기본 Idle 색 대신 다른 색을 주고 싶을 때만 쓴다(예:
        /// 교차 방향 대상의 노란 표기). null 을 반환하면 기존 Idle 그대로라 나머지
        /// 호출부는 이 인자를 아예 안 넘겨도 무수정.
        void RebuildAxisOptions<T>(Transform dropdown, float radius, IReadOnlyList<T> options,
                                   System.Func<T, string> label, System.Func<object, bool> isSelected,
                                   System.Action<object> onPick, bool closeOnPick = true,
                                   System.Func<T, Color?> tintOverride = null)
        {
            for (int i = dropdown.childCount - 1; i >= 0; i--)
                Destroy(dropdown.GetChild(i).gameObject);   // 확정 전 재구성뿐이라 즉시 파괴로 충분

            int n = options.Count;
            var labels = new string[n];
            for (int idx = 0; idx < n; idx++) labels[idx] = label(options[idx]);

            // "*Wheel"(dropdown)의 부모가 "*Control" — BuildAxisControl 이 그 anchoredPosition
            // 을 트리거 좌표(TriggerX, Y)로 이미 잡아뒀다. FloatingControls 자체가 캔버스
            // 전체를 덮는 anchorMin=0/anchorMax=1 이라, 그 anchoredPosition 이 곧 캔버스
            // 절대좌표(화면 픽셀, 좌하단 원점) — 세이프 영역 검사에 그대로 쓸 수 있다.
            var origin = ((RectTransform)dropdown.parent).anchoredPosition;
            var layout = SolvePolarLayout(origin, radius, labels);

            for (int idx = 0; idx < n; idx++)
            {
                var captured = options[idx];

                var btn = MakeButton(dropdown, labels[idx], () => { onPick(captured); if (closeOnPick) CloseAllWheels(); });
                var brt = (RectTransform)btn.transform;
                brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
                brt.pivot = new Vector2(0.5f, 0.5f);
                brt.anchoredPosition = layout.LocalPositions[idx];   // origin(트리거) 기준 상대 오프셋
                brt.sizeDelta = new Vector2(layout.Widths[idx], PolarButtonHeight);

                var img = btn.GetComponent<Image>();
                img.color = isSelected(captured) ? Selected : (tintOverride?.Invoke(captured) ?? Idle);
            }
        }

        // ── 극좌표 배치: 실측 기반, "겹침 0 · 화면 이탈 0" 불변식 ──────────────────
        //
        // 2026-08-14 밤 8차. 캔버스가 1920×1080 기준 해상도(CanvasScaler)라 이
        // 좌표계에서 계산한 값은 실제 해상도와 무관하게 유효하다 — TriggerX=55 등
        // 기존 상수들도 전부 이 기준으로 이미 정의돼 있다(재확인, 새 관례 아님).

        const float PolarButtonHeight = 36f;
        const float PolarMinWidth = 56f, PolarMaxWidth = 150f, PolarGapDeg = 4f;
        const float CanvasRefWidth = 1920f, CanvasRefHeight = 1080f, SafeMargin = 10f;
        // 트리거 버튼 크기(BuildAxisControl 의 sizeDelta(74,36))의 절반 — 모든 축이
        // 공유하는 값이라 상수로 뺐다. 휠이 열린 동안 "자기 축"의 트리거는 계속 화면에
        // 남는다(OpenWheel 이 숨기는 건 "다른" 축뿐) — 항목이 원점(=트리거 중심)에
        // 너무 가까우면 트리거 라벨을 가린다. 2026-08-14 밤 9차, 손 스테이지1의
        // "왼손"이 "손" 트리거를 가린 실측 재발견 이후 세이프 영역 검사에 추가.
        const float TriggerHalfWidth = 37f, TriggerHalfHeight = 18f;
        // 2026-08-17 — 세로 스택 방어선(FallbackStackedLayout, 아래) 전용 안전 구간.
        // <see cref="BuildFloatingControls"/> 가 네 트리거를 배치할 때 이미 실측으로
        // 확정해둔 값(ConfirmRow 상단=308 / NarrationLog 하단=792) 그대로 재사용 —
        // 이 구간 밖(위로는 NarrationLog, 아래로는 ScoreboardRow)은 휠이 열려도 안
        // 숨는 상시 노출 패널이라, 스택 항목이 거기 놓이면 그 패널의 텍스트와 겹쳐
        // "글자 깨짐"으로 보인다(실측으로 확인 — 아래 FallbackStackedLayout 주석 참조).
        const float FloatingSafeBandBottom = 308f, FloatingSafeBandTop = 792f;

        /// <see cref="LocalPositions"/> — origin(트리거) 기준 상대 오프셋(anchoredPosition
        /// 에 그대로 쓴다). 대부분의 경로에서 전 항목이 같은 반지름을 공유하지만,
        /// 최후 방어선(개별 클램프)에서는 항목마다 실제 반지름이 달라질 수 있어 애초에
        /// 위치를 직접 저장한다 — 공유 스칼라 반지름 하나로는 그 경우를 표현 못 한다.
        struct PolarLayoutResult { public Vector2[] LocalPositions; public float[] Widths; }

        /// 측정 전용 — 화면에 안 그려진다(Canvas 조상이 없는 트랜스폼에 붙여 렌더
        /// 자체가 안 됨). <see cref="TMP_Text.GetPreferredValues(string,float,float)"/>
        /// 는 실제 렌더와 무관하게(한 프레임 기다릴 필요 없이) 폰트 메트릭만으로
        /// 계산되는 값이라, 라벨 폭을 "재는" 용도로 안전하게 쓸 수 있다.
        TextMeshProUGUI _measureScratch;

        float MeasureLabelWidth(string text, float fontSize)
        {
            if (_measureScratch == null)
            {
                var go = new GameObject("MeasureScratch(안 그려짐)", typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(transform, false);   // LiveMatchPanel 자신 — Canvas 밖이라 렌더 안 됨
                _measureScratch = go.GetComponent<TextMeshProUGUI>();
                if (koreanFont != null) _measureScratch.font = koreanFont;
                _measureScratch.textWrappingMode = TextWrappingModes.NoWrap;
                _measureScratch.raycastTarget = false;
            }
            _measureScratch.fontSize = fontSize;
            return _measureScratch.GetPreferredValues(text, 10000f, 0f).x;
        }

        /// 2026-08-18 재설계 — 트리거 킵아웃을 "이 축은 반원만 쓴다"는 사전 제약이
        /// 아니라 원 위의 <b>θ=180(화면 왼쪽 끝, 트리거 자신이 놓인 방향) 부근 금지
        /// 원뿔</b>로 명시한다. 옛 <c>TryPackAngles</c>(9차 세션, 이제 삭제)는
        /// 아크를 항상 −90°~+90° 반원(또는 손 스테이지1만 90°~270°)으로 미리 고정해두고
        /// 그 안에서 "양 끝 앵커링"으로 남는 여유를 중앙에 몰았다 — 손 스테이지1처럼
        /// 아크의 중앙이 θ=180 인 축에서는 이게 우연히 안전했지만, 나머지 축(중앙이
        /// θ=0, 가장 안전한 정면 방향)에서는 항목을 하필 가장 위험한 양끝(±90°, 트리거
        /// 바로 옆)에 몰아넣고 가장 안전한 정면을 텅 비워 "두 뭉치로 갈라진" 모양이
        /// 됐다(2026-08-18 재신고 — 기술 휠만 정상, 나머지는 트리거에 밀착되거나 화면
        /// 끝으로 튐). 이번엔 축마다 다른 아크를 안 고르고, <b>모든 축이 같은 규칙</b>
        /// (θ=180 원뿔만 비우고 나머지 거의 전체 원을 슬랙 균등분배로 채운다)을 쓴다 —
        /// 원뿔 자체가 이미 위험 방향을 피하므로 8차 세션의 원안(여유를 모든 간격에
        /// 고르게 분배, 중앙을 특별 취급 안 함)으로 되돌아가도 안전하다.
        ///
        /// <paramref name="keepoutHalfDeg"/> 는 이 반지름에서 θ=180 부근 얼마나 넓게
        /// 비워야 화면 왼쪽 끝 세이프 마진을 넘지 않는지 <see cref="ComputeKeepoutHalfDeg"/>
        /// 가 미리 계산해 넘겨준다 — 이 함수는 그 값을 그대로 신뢰하고 각도만 채운다.
        /// 반지름이 작아도 정확하도록 폭을 반각으로 atan2 환산한다. 다 안 들어가면 null.
        static float[] TryPackAroundKeepout(float[] widths, float radius, float keepoutHalfDeg)
        {
            int n = widths.Length;
            var halfAnglesDeg = new float[n];
            for (int i = 0; i < n; i++)
                halfAnglesDeg[i] = Mathf.Atan2(widths[i] / 2f, radius) * Mathf.Rad2Deg;

            // 항목 하나뿐이면 원뿔과 정반대인 θ=0(가장 안전한 정면)에 그대로 둔다.
            if (n == 1) return new[] { 0f };

            float arcStartDeg = 180f + keepoutHalfDeg;   // 원뿔 한쪽 경계(아래쪽)에서 출발
            float arcSpanDeg = 360f - 2f * keepoutHalfDeg;   // θ=180 원뿔만 뺀 나머지 전체
            if (arcSpanDeg <= 0f) return null;

            float needed = PolarGapDeg * (n - 1);
            for (int i = 0; i < n; i++) needed += halfAnglesDeg[i] * 2f;
            if (needed > arcSpanDeg) return null;

            // 8차 세션 원안대로 슬랙을 모든 간격에 고르게 나눈다 — 정면(θ=0)이 특별
            // 취급 없이 자연스럽게 채워지므로, 항목이 적을 때도 "두 뭉치+빈 중앙"이
            // 아니라 원뿔 한쪽에서 반대쪽까지 고르게 이어진 부채꼴이 나온다.
            float gapBonus = (arcSpanDeg - needed) / (n - 1);

            var angles = new float[n];
            float cursor = arcStartDeg;
            for (int i = 0; i < n; i++)
            {
                cursor += halfAnglesDeg[i];
                angles[i] = cursor;
                cursor += halfAnglesDeg[i] + PolarGapDeg + gapBonus;
            }
            return angles;
        }

        /// <paramref name="radius"/>에서 θ=180 부근 얼마나 넓게 비워야 화면 왼쪽 끝
        /// 세이프 마진(<see cref="SafeMargin"/>)을 넘지 않는지 계산한다. 트리거 중심
        /// (<paramref name="originX"/>)에서 왼쪽으로 허용된 최대 거리는
        /// <c>maxLeftReach = originX − SafeMargin − maxHalfWidth</c>(항목 중 가장 넓은
        /// 것 기준 — 좁은 항목이 원뿔 경계 쪽에 배정돼도 안전하도록 보수적으로 잡는다).
        /// 위험 조건은 <c>radius·cosθ &lt; −maxLeftReach</c> — 이 경계각 θᵦ 를 역코사인으로
        /// 구하면 원뿔 반각은 <c>180−θᵦ</c>. maxLeftReach 가 음수(항목이 원점만으로도
        /// 이미 세이프 마진을 넘길 만큼 넓음)여도 식은 그대로 성립(θᵦ 가 90° 밑으로
        /// 내려가 원뿔이 반원보다 넓어질 뿐) — 별도 분기 불필요, Clamp 로 정의역만 보장.
        static float ComputeKeepoutHalfDeg(float originX, float radius, float maxHalfWidth)
        {
            float maxLeftReach = originX - SafeMargin - maxHalfWidth;
            float cosBoundary = Mathf.Clamp(-maxLeftReach / radius, -1f, 1f);
            float thetaB = Mathf.Acos(cosBoundary) * Mathf.Rad2Deg;
            return 180f - thetaB;
        }

        /// 2026-08-14 밤 9차 재발견 — 각도 간격(<see cref="PolarGapDeg"/>)만으로는 인접
        /// 항목의 AABB(축 정렬 사각형) 비겹침을 보장 못 한다: 그 간격은 원둘레를 따라
        /// 잰 호 길이 여유일 뿐, 접선 방향이 화면 x/y 축과 안 맞으면(예: 45° 부근)
        /// 유클리드 거리로 환산됐을 때 실제 사각형 간 간격이 그보다 작아질 수 있다
        /// (하체 4항목의 "공격"↔"중립" 5px 겹침으로 실측 확인 — 손 스테이지1 때와
        /// 달리 트리거가 아니라 항목끼리였다). 그래서 항목 간 실제 AABB 겹침도 이
        /// 안전 게이트에 직접 넣는다 — 실패하면 바깥 루프가 반지름을 키우거나 폭을
        /// 줄여 재시도한다(트리거 검사와 같은 계약).
        bool AllWithinSafeArea(Vector2 origin, float radius, float[] anglesDeg, float[] widths)
        {
            int n = anglesDeg.Length;
            var centers = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float rad = anglesDeg[i] * Mathf.Deg2Rad;
                var offset = new Vector2(radius * Mathf.Cos(rad), radius * Mathf.Sin(rad));
                centers[i] = origin + offset;
                float halfW = widths[i] / 2f, halfH = PolarButtonHeight / 2f;
                if (centers[i].x - halfW < SafeMargin || centers[i].x + halfW > CanvasRefWidth - SafeMargin) return false;
                if (centers[i].y - halfH < SafeMargin || centers[i].y + halfH > CanvasRefHeight - SafeMargin) return false;

                // 트리거 자체(원점 중심, TriggerHalfWidth/Height)와의 겹침도 검사한다 —
                // 분리축 판정(둘 중 한 축이라도 안 겹치면 사각형 전체가 안 겹친다).
                bool clearsTriggerX = Mathf.Abs(offset.x) > TriggerHalfWidth + halfW;
                bool clearsTriggerY = Mathf.Abs(offset.y) > TriggerHalfHeight + halfH;
                if (!clearsTriggerX && !clearsTriggerY) return false;
            }

            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                {
                    float halfWi = widths[i] / 2f, halfWj = widths[j] / 2f, halfH = PolarButtonHeight / 2f;
                    bool clearsX = Mathf.Abs(centers[i].x - centers[j].x) > halfWi + halfWj;
                    bool clearsY = Mathf.Abs(centers[i].y - centers[j].y) > halfH + halfH;
                    if (!clearsX && !clearsY) return false;
                }
            return true;
        }

        /// §2b v4.2 후속(2026-08-14 밤 8차, 2026-08-18 재설계) — 실측 기반 극좌표
        /// 배치의 핵심. 우선순위 고정: (1) 화면 세이프 영역 이탈 0(절대 원칙) (2) 항목
        /// 간(트리거 포함) 겹침 0(라벨 폭을 줄여서라도 지킨다) (3) 방사형으로 보일 것
        /// (트리거 방향 θ=180 원뿔만 비우고 나머지는 고르게 채운다, 폴백은 최후 수단)
        /// (4) 그 안에서 최대한 읽기 좋은 폭.
        ///
        /// 알고리즘: 라벨 폭 상한을 넓은 값에서 좁은 값으로 단계적으로 낮춰가며(폭이
        /// 좁아지면 TMP autosize 가 폰트를 자동으로 줄인다 — MakeText 가 이미
        /// enableAutoSizing 을 켜둔다, 새로 켜는 게 아니다), 각 상한에서 반지름을
        /// <paramref name="baseRadius"/> 부터 키워간다. 반지름마다 <see cref="ComputeKeepoutHalfDeg"/>
        /// 로 그 반지름에서 필요한 θ=180 원뿔 폭을 다시 계산하고(반지름이 커질수록
        /// 원뿔도 넓어진다 — 고정 아님), <see cref="TryPackAroundKeepout"/> 로 그 원뿔만
        /// 뺀 나머지 원에 항목을 채워본다. 각도가 나오면 <see cref="AllWithinSafeArea"/>
        /// 로 최종 검증(트리거 근접 등 원뿔 계산이 못 잡는 잔여 위험도 여기서 걸린다) —
        /// 통과하면 채택, 실패하면 반지름을 계속 키운다(원뿔 모델에서는 반지름이 커져도
        /// 항상 나빠지기만 하는 게 아니라서 — 트리거 자신과의 근접 실패는 반지름이
        /// 커지면 오히려 풀린다 — 9차 세션의 "실패하면 즉시 다음 폭으로" 단축은 더 이상
        /// 안전하지 않아 걷어냈다).
        ///
        /// 최소 폭 · 최대 반지름에서도 원 위에 못 앉히는 극단적 경우에만 최후 수단으로
        /// <see cref="FallbackStackedLayout"/>(세로 스택)로 넘어간다 — 이 경로가 실제로
        /// 밟히면 방사형으로 안 보이므로, 밟히는지 자체를 Play 실측으로 확인해야 한다
        /// (2026-08-18 재설계의 목적이 바로 이 경로를 정상 화면에서 없애는 것).
        PolarLayoutResult SolvePolarLayout(Vector2 origin, float baseRadius, string[] labels)
        {
            int n = labels.Length;
            const float maxRadius = 500f;

            for (float widthCap = PolarMaxWidth; widthCap >= PolarMinWidth; widthCap -= 10f)
            {
                var widths = new float[n];
                float maxHalfWidth = 0f;
                for (int i = 0; i < n; i++)
                {
                    widths[i] = Mathf.Clamp(MeasureLabelWidth(labels[i], fontSize) + 18f, PolarMinWidth, widthCap);
                    maxHalfWidth = Mathf.Max(maxHalfWidth, widths[i] / 2f);
                }

                for (float radius = baseRadius; radius <= maxRadius; radius *= 1.08f)
                {
                    float keepoutHalfDeg = ComputeKeepoutHalfDeg(origin.x, radius, maxHalfWidth);
                    var angles = TryPackAroundKeepout(widths, radius, keepoutHalfDeg);
                    if (angles == null) continue;
                    if (AllWithinSafeArea(origin, radius, angles, widths))
                        return new PolarLayoutResult { LocalPositions = ToLocalPositions(radius, angles), Widths = widths };
                }
            }

            // 마지막 방어선 — 원형으로는 못 맞춘 진짜 극단적 경우에만(2026-08-18 재설계
            // 이후 정상 화면에서는 도달하지 않아야 한다 — Play 실측으로 확인할 것).
            return FallbackStackedLayout(origin, labels);
        }

        /// 2026-08-17 재발견 — 세로 스택 밴드가 "캔버스 세로 전체가 비어 있다"고
        /// 가정했지만 거짓이었다. 좌측 열(트리거가 놓인 x=<see cref="TriggerX"/> 열)에는
        /// 상시 노출되는 두 불투명 패널이 있다 — <c>NarrationLog</c>(y∈[792,1072],
        /// 최근 교환 서술·로그 텍스트로 항상 채워져 있음)와 <c>ScoreboardRow</c>
        /// (y∈[8,252], 점수판 배경). 이 두 패널은 휠이 열려도 안 숨는다(트리거·요약만
        /// <see cref="OpenWheel"/> 이 숨긴다) — 그래서 밴드가 그 영역까지 뻗으면
        /// 항목 버튼이 그 패널 "위에" 그려지고, 특히 NarrationLog 는 항상 텍스트가
        /// 차 있어 항목 라벨과 로그 텍스트가 같은 픽셀에 겹쳐 그려져 "글자가 깨졌다"로
        /// 보인다(손 스테이지1의 "왼손"이 y=1034~1070, 즉 NarrationLog 내부에 렌더돼
        /// 로그 텍스트와 겹친 것을 실측으로 확인 — 라벨 문자열 자체는 온전했다, 겹쳐
        /// 그려진 두 텍스트가 뒤섞여 보였을 뿐). 하체(다리) 축도 같은 경로로 "중립"이
        /// NarrationLog 에, "수비"가 ScoreboardRow 에 겹치는 것도 함께 확인했다.
        ///
        /// 수정: 밴드 상/하한을 캔버스 전체가 아니라 <see cref="FloatingSafeBandBottom"/>/
        /// <see cref="FloatingSafeBandTop"/>(트리거 네 개를 배치할 때 이미 실측으로 확정해둔
        /// "두 패널 사이의 진짜 빈 구간", <see cref="BuildFloatingControls"/> 참조)로 좁힌다
        /// — 이 구간은 NarrationLog·ScoreboardRow 어느 쪽과도 안 겹치는 걸로 이미
        /// 검증된 값이라 새로 재기 아닌 재사용이다. 다른 세 축의 트리거·요약은 휠이
        /// 열리는 동안 숨겨지므로(<see cref="OpenWheel"/>) 이 구간 안에 있어도 걱정할
        /// 대상이 아니다.
        ///
        /// 세로 스택 방어선 — <see cref="SolvePolarLayout"/> 이 원형 배치를 못 찾았을
        /// 때만 쓴다. 항목을 입력 순서 앞쪽 절반은 트리거 위 밴드에, 뒤쪽 절반은
        /// 트리거 아래 밴드에 나눠 각 밴드 안에서 균등 간격으로 세로로 쌓는다 —
        /// 밴드 자체가 트리거의 세로 범위(±<see cref="TriggerHalfHeight"/>)를 제외하고
        /// 시작하므로 트리거와 겹칠 수 없고(Y 축만으로 이미 분리), 같은 밴드 안
        /// 항목끼리는 균등 간격이 겹침을 막는다(이 앱의 실제 항목 수(최대 8, 밴드당
        /// 최대 2~4개)에서 축소된 밴드 안에서도 여유가 남는 것을 실측으로 확인 —
        /// 아래 검증 참조).
        PolarLayoutResult FallbackStackedLayout(Vector2 origin, string[] labels)
        {
            int n = labels.Length;
            var widths = new float[n];
            for (int i = 0; i < n; i++)
                widths[i] = Mathf.Clamp(MeasureLabelWidth(labels[i], fontSize) + 18f, PolarMinWidth, PolarMaxWidth);
            float maxHalfW = 0f;
            for (int i = 0; i < n; i++) maxHalfW = Mathf.Max(maxHalfW, widths[i] / 2f);
            float x = Mathf.Clamp(origin.x, SafeMargin + maxHalfW, CanvasRefWidth - SafeMargin - maxHalfW);

            int nStart = (n + 1) / 2;
            int nEnd = n - nStart;
            float halfH = PolarButtonHeight / 2f;

            var positions = new Vector2[n];
            StackBand(positions, 0, nStart, x,
                origin.y + TriggerHalfHeight + halfH, FloatingSafeBandTop - halfH, origin);
            StackBand(positions, nStart, nEnd, x,
                FloatingSafeBandBottom + halfH, origin.y - TriggerHalfHeight - halfH, origin);

            return new PolarLayoutResult { LocalPositions = positions, Widths = widths };
        }

        /// [<paramref name="startIdx"/>, +<paramref name="count"/>) 항목을 y ∈ [lo,hi] 안에
        /// 균등 간격으로 세로 배치한다(x 는 고정). count==1 이면 밴드 중앙에.
        static void StackBand(Vector2[] positions, int startIdx, int count, float x, float lo, float hi, Vector2 origin)
        {
            if (count <= 0) return;
            if (count == 1)
            {
                positions[startIdx] = new Vector2(x, (lo + hi) / 2f) - origin;
                return;
            }
            float step = (hi - lo) / (count - 1);
            for (int k = 0; k < count; k++)
                positions[startIdx + k] = new Vector2(x, lo + step * k) - origin;
        }

        static Vector2[] ToLocalPositions(float radius, float[] anglesDeg)
        {
            var positions = new Vector2[anglesDeg.Length];
            for (int i = 0; i < anglesDeg.Length; i++)
            {
                float rad = anglesDeg[i] * Mathf.Deg2Rad;
                positions[i] = new Vector2(radius * Mathf.Cos(rad), radius * Mathf.Sin(rad));
            }
            return positions;
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
            HandTarget.Setup => "흔들기",
            HandTarget.Solidify => "견고화",
            _ => "—",
        };

        static string DefenseZoneLabel(DefenseZone z) => z switch
        {
            DefenseZone.LeftSleeve => "왼소매",
            DefenseZone.RightSleeve => "오른소매",
            DefenseZone.LeftLapel => "왼가슴깃",
            DefenseZone.RightLapel => "오른가슴깃",
            DefenseZone.LeftUpper => "왼상단",
            DefenseZone.RightUpper => "오른상단",
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

        /// 수 노트 — 습관 한국어 라벨. [v1 임시 문구 — 카피 교체 목록에 올릴 것]
        static string HabitLabel(HabitId id) => id switch
        {
            HabitId.BigThrowOnGripEdge => "업어치기 (그립 우세)",
            HabitId.LegTechAfterShido => "안뒤축감아치기 (지도 직후)",
            HabitId.TurtleWhenTired => "극단적 방어 (체력 열세)",
            HabitId.SeekGripRightSleeve => "오른소매 노림",
            HabitId.SeekGripLeftSleeve => "왼소매 노림",
            _ => id.ToString(),
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
        /// 일어난 교환). 서술 줄과 로그가 둘 다 이 문자열을 그대로 쓴다 — 출처가 하나다.
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
            SetupDeclared sd => $"{FighterLabel(sd.Who)} 흔들기 설치",
            TearResolved tr => $"{FighterLabel(tr.Who)} 뜯기 — {TearLabel(tr.Outcome)}",
            CounterReadyApplied cr => $"{FighterLabel(cr.Who)} 카운터 준비 적중 — 상대 읽기 하락",
            OsaekomiEscapeResolved er => er.EscapeCredited ? "이탈 성공" : "눌림 유지 — 이탈 실패",
            GripChanged g => $"{FighterLabel(g.Who)} {HandLabel(g.Part)} 확보",
            GripAttemptFailed gf => $"{FighterLabel(gf.Who)} {HandLabel(gf.Target)} 시도 실패",
            GripSolidified gs => $"{FighterLabel(gs.Who)} {(gs.Hand == Hand.Left ? "왼손" : "오른손")} 견고화",
            // §S/§H 3단계 후속 — 한 교환에 여러 개가 뜰 수 있어도(예: 그쳐 시 A·B 둘 다)
            // BuildOutcome 의 "발생한 이벤트 중 뜻이 있는 첫 번째" 관행 그대로 첫 것만
            // 보여준다 — 다른 이벤트들과 같은 한계(GripChanged 등도 동일).
            BalanceChanged bc => $"{FighterLabel(bc.Who)} 밸런스 {bc.NewValue}",
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

        // ── 좌하: 상태 줄 / 서술 줄 / 로그 ───────────────────────────────────────

        /// 점수판에 없는 나머지(잡기·체력·위상·흔들기) + 이번 교환 서술. 시계·절반·
        /// 유효·지도·누르기 카운트는 <see cref="RefreshScoreboard"/> 담당.
        /// <paramref name="matteJustCalled"/> — 이번 Confirm() 호출에서 그쳐가
        /// 났으면 true. 그쳐는 <c>MatchClock.Phase</c> 에 남지 않는 순간 상태라
        /// (그쳐 직후 곧장 서기로 돌아간다) 호출부가 이번 교환의 이벤트를 보고
        /// 판단해서 넘겨줘야 한다 — 안 넘기면 "그쳐"가 화면에 뜰 기회가 없다.
        void RefreshStatusHud(bool matteJustCalled = false)
        {
            var eng = _runner.Engine;
            RefreshScoreboard(eng);
            RefreshCoachAdvice(eng);
            RefreshNotes();

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
            string setup = _runner.Config.BalanceGate
                ? $"  흔들기 {(eng.IsSetupActive(who) ? "유효" : "없음")}"
                : "";
            // §2b v4.2 — 손별 그립 상태(대상·정/교차·견고) 양측 + 교차 타이머 잔여.
            // BalanceGate 흔들기 표시줄과 같은 패턴 — 게이트가 꺼져 있으면 이 개념
            // 자체가 없으므로(§2a 그대로) 아예 안 붙인다.
            string gripDetail = _runner.Config.GripRevisionGate
                ? $"  [{GripHandDetail(eng, who, Hand.Left)} / {GripHandDetail(eng, who, Hand.Right)}]" +
                  (eng.CrossGripTimerRemaining(who) > 0 ? $" 교차타이머 {eng.CrossGripTimerRemaining(who)}턴" : "")
                : "";
            return $"{FighterLabel(who)}  체력 {stamina}  그립 {grip:+0;-0;0}  밸런스 {BalanceText(eng, who)}  " +
                   $"왼손 {left} 오른손 {right}{setup}{gripDetail}";
        }

        /// §2b v4.2 — 한 손의 상태를 "대상·정/교차·견고" 한 조각으로. 아무것도 안
        /// 잡았으면 "—"만.
        static string GripHandDetail(MatchEngine eng, FighterId who, Hand hand)
        {
            var tier = eng.Grip.Held(who, hand);
            if (tier == GripKind.None) return "—";
            string target = tier switch
            {
                GripKind.Normal => "소매",
                GripKind.Lapel => "가슴깃",
                GripKind.BackGrip => "틀어잡기",
                GripKind.Dominant => "전환",   // §2a 구식 경로 표시용(게이트 off일 땐 이 줄 자체가 안 뜬다)
                _ => "?",
            };
            string orientation = eng.Grip.IsCrossOrientation(who, hand) ? "교차" : "정";
            string solid = eng.Grip.IsSolid(who, hand) ? "·견고" : "";
            return $"{target}·{orientation}{solid}";
        }

        /// §S — 밸런스는 확정 수치라 체력·그립과 같은 급으로 상시 노출한다(예비동작과
        /// 달리 숨길 정보 아님, §7). `BalanceGate`가 꺼져 있으면 밸런스 시스템 자체가
        /// 완전 비활성(§H 3단계 — 모든 드레인/회복이 그 플래그로 게이트돼 있다)이라
        /// 값이 항상 BalanceMax(100)로 고정 표시된다 — 흔들기 표시줄과 달리 이 줄은
        /// 플래그로 안 숨긴다, 정확한 값(설령 늘 100이어도)이라는 원칙을 지키기 위해.
        ///
        /// [설계 판단] 온전 임계(BalanceOnsetThreshold) 이상이면 강조색(Warning,
        /// 내 성향 경고선과 같은 색 재사용— 새 색 추가 안 함). 근거: 그 값 이상이면
        /// "지금 이 상대에게 큰기술을 던지면 §4.4에 의해 Sloppy로 강등된다"는 뜻이라
        /// 공격 타이밍을 정할 때 직접 참고하는 실전 정보다. 정확한 임계 숫자를 안
        /// 외워도 이분법만으로 판단 가능하게 하는 목적 — 그 외 시각 구분(바 형태 등)은
        /// 안 함, 기존 체력·그립도 순수 숫자 표시라 같은 급을 유지한다.
        string BalanceText(MatchEngine eng, FighterId who)
        {
            int value = eng.Balance.Get(who);
            bool full = value >= _runner.Config.BalanceOnsetThreshold;
            return full ? $"<color=#{ColorUtility.ToHtmlStringRGB(Warning)}>{value}</color>" : value.ToString();
        }

        // ── 우측 사이드바 1: 코치의 조언 ─────────────────────────────────────────

        /// [v1 임시 문구 전부 — 카피 교체 목록에 올릴 것] 판정식 합계·심판 누산기는
        /// 참조하지 않는다 — 전부 이미 화면에 나가는 값(그립/체력/흔들기)에서만 고른다.
        /// 자기 노출 위험선은 여기서 다루지 않는다 — 그 몫은 내 성향 박스가 전담한다
        /// (같은 경고를 두 곳에서 다른 말로 하면 헷갈린다).
        void RefreshCoachAdvice(MatchEngine eng)
        {
            string advice;
            if (eng.Osaekomi.Active)
                advice = "굳히기 중 — 버티면 점수가 오르고, 빠져나가면 다시 선다";
            else if (eng.Grip.EffectiveGrip(FighterId.B) - eng.Grip.EffectiveGrip(FighterId.A) >= 2)
                advice = "잡기 열세 — 손 싸움부터 다시 잡아라";
            else if (eng.Stamina.Get(FighterId.B) < 40)
                advice = "상대 체력이 떨어졌다 — 밀어붙일 타이밍";
            else if (_runner.Config.BalanceGate && eng.IsSetupActive(FighterId.B))
                advice = "상대가 흔들기를 걸었다 — 큰 기술을 조심해라";
            else
                advice = "그립을 먼저 잡고 흐름을 만들어라";
            _adviceText.text = advice;
        }

        // ── 우측 사이드바 2: 수 노트 ─────────────────────────────────────────────

        /// §2 — A 가 실제로 관측한(<c>Observations &gt;= 1</c>) 습관만 행으로 만든다.
        /// 아직 한 번도 안 본 습관은 존재 자체를 노출하지 않는다(요구사항). 단계는
        /// 3개: 심증(1~2회)/확정(3회 이상)/폐기됨(퇴역 — 관측 횟수와 무관하게 최우선).
        /// [v1 임시 문구 — 카피 교체 목록에 올릴 것]
        void RefreshNotes()
        {
            for (int i = _notesColumn.childCount - 1; i >= 0; i--)
                Destroy(_notesColumn.GetChild(i).gameObject);

            var ledger = _runner.Ledger;
            var habits = _runner.OpponentHabits;
            bool any = false;

            foreach (var habit in habits.Opponent.Habits)
            {
                int obs = ledger.Observations(habit.Id);
                if (obs < 1) continue;
                any = true;

                bool retired = habits.IsRetired(habit.Id);
                bool confirmed = ledger.IsConfirmed(habit.Id);
                string stage = retired ? $"<s>{HabitLabel(habit.Id)}</s> — 폐기됨"
                             : confirmed ? $"{HabitLabel(habit.Id)} — 확정 ({obs}회)"
                             : $"{HabitLabel(habit.Id)} — 심증 ({obs}회)";

                var row = MakeFlexText(_notesColumn, stage, TextAlignmentOptions.TopLeft, 1, 0,
                    minHeight: 20, fontSizeOverride: 15);
                row.color = retired ? Dim : confirmed ? Warning : Color.white;
            }

            if (!any)
                MakeFlexText(_notesColumn, "아직 관측된 습관 없음", TextAlignmentOptions.TopLeft, 1, 0,
                    minHeight: 20, fontSizeOverride: 15).color = Dim;
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

            // 경기 화면을 창 전체 배경으로 — RootLayout(패널들)보다 먼저 만들어야
            // sibling 순서상 더 뒤(=아래)에 그려진다.
            BuildArenaBackground(go.transform);

            var root = MakeVerticalGroup(go.transform, "RootLayout", spacing: 8);
            var rootRt = (RectTransform)root;
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = rootRt.offsetMax = Vector2.zero;
            var rootLg = root.GetComponent<VerticalLayoutGroup>();
            rootLg.padding = new RectOffset(12, 12, 8, 8);

            // ① 중단 — 좌(서술/로그) : 우(사이드바 3종) = 2:1. 이제 RootLayout 의
            // 첫 슬롯이다 — 점수판이 하단으로 옮겨가며(2026-08-12 실전 UI 개편,
            // 사용자 지시) 옛 상단 244px 자리까지 이 블록이 자동으로 흡수한다
            // (VerticalLayoutGroup 의 flexible 배분이라 여기 코드는 안 바뀐다).
            var middleRow = MakeHorizontalGroup(root, "MiddleRow", spacing: 12);
            SetFlexible(middleRow, 1, 1);

            var leftColumn = MakeVerticalGroup(middleRow, "LeftColumn", spacing: 8);
            SetFlexible(leftColumn, 2, 1);
            BuildNarrationLog(leftColumn);

            var sidebar = MakeVerticalGroup(middleRow, "RightSidebar", spacing: 8);
            SetFlexible(sidebar, 1, 1);
            BuildCoachAdviceBox(sidebar);
            BuildNotesBox(sidebar);
            BuildTendencyBox(sidebar);

            // ② 굳히기 2지선다 — 서기 중엔 비활성(레이아웃에서 제외돼 공간을 안 씀).
            // 옛 BottomInputRow 안에 있던 걸 그대로 옮겼을 뿐 — 내부 배선
            // (SubmitOsaekomi 등, BuildOsaekomiRoot)은 무수정.
            var osaekomiRow = new GameObject("OsaekomiRow", typeof(RectTransform), typeof(LayoutElement), typeof(Image));
            osaekomiRow.transform.SetParent(root, false);
            SetFixedHeight(osaekomiRow.transform, 200);
            osaekomiRow.GetComponent<Image>().color = PanelBg;
            BuildOsaekomiRoot(osaekomiRow.transform);
            _osaekomiRoot = osaekomiRow;
            _osaekomiRoot.SetActive(false);

            // ③ 확정 버튼 — 화면 중앙, 점수판 바로 위(사용자 지시). 손·코어·하체
            // 3열은 더 이상 여기 없다 — 캐릭터 옆 드롭다운으로 옮겨졌다(§⑤).
            var confirmRow = MakeHorizontalGroup(root, "ConfirmRow", spacing: 0);
            SetFixedHeight(confirmRow, 48);
            confirmRow.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            _confirmButton = MakeButton(confirmRow, "확정", Confirm);
            SetFixedWidth(_confirmButton.transform, 200);
            _confirmButton.GetComponent<LayoutElement>().flexibleHeight = 0;
            _standingRoot = confirmRow.gameObject;

            // ④ 점수판 — 화면 중앙 하단(사용자 지시). 안쪽은 기존 BuildScoreboard
            // 그대로(무수정) — 슬롯 위치만 상단에서 하단으로 옮겼다.
            var scoreboardRow = new GameObject("ScoreboardRow", typeof(RectTransform), typeof(LayoutElement), typeof(Image));
            scoreboardRow.transform.SetParent(root, false);
            SetFixedHeight(scoreboardRow.transform, 244);
            // 경기 화면이 배경으로 깔리면서 점수판 글자가 하늘/바닥 위에 그대로 떠
            // 안 읽힌다 — 사이드바에 이미 쓰던 PanelBg(반투명 어두운 판)를 재사용.
            // 알파(0.92)는 스크린샷 없이 확정할 수 없어 하나 고른 값이다 — PanelBg
            // 한 곳만 고치면 점수판·사이드바·확정 버튼 행·서술 로그 스크림이 다 같이 바뀐다.
            scoreboardRow.GetComponent<Image>().color = PanelBg;
            BuildScoreboard(scoreboardRow.transform);

            // ⑤ 캐릭터 옆 손·코어·하체 드롭다운 — RootLayout 의 세로 흐름 밖, 캔버스
            // 최상단 sibling(모든 패널 위)에 고정 좌표로 떠 있는 별도 레이어.
            BuildFloatingControls(go.transform);
        }

        /// 창 전체 배경 — <see cref="ArenaStageView"/> 의 320x180 RenderTexture 를 캔버스
        /// 전체에 편다. RT 해상도·MSAA off·Point 필터·12fps 스텝은 무수정(§ArenaStageView) —
        /// 화면에 늘려 보여주는 표시 크기만 커질 뿐 렌더 자체는 그대로다. EnvelopeParent 로
        /// 창 비율이 16:9 가 아니어도 찌그러뜨리지 않고 빈틈없이 덮는다(16:9 창이면 크롭도
        /// 없다 — 2026-08-12 실측: 1920x1080/1280x720 은 정확히 16:9라 크롭 0, 나머지
        /// 해상도는 소폭 크롭 — STATUS.md 참조).
        /// RectMask2D 는 이제 안 쓴다 — 예전엔 이 RT 가 좁은 박스 안에 있어서 박스보다
        /// 커진 RawImage 가 옆(점수판/서술) 블록을 침범했지만, 지금은 캔버스 전체가 이
        /// 배경의 영역이라 침범할 이웃이 없다(화면 밖으로 나가는 부분은 그 자체로 렌더
        /// 대상 밖이라 마스크 없이도 안 보인다).
        void BuildArenaBackground(Transform canvasRoot)
        {
            var stage = GetComponent<ArenaStageView>();
            var go = new GameObject("ArenaBackground", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
            go.transform.SetParent(canvasRoot, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var raw = go.GetComponent<RawImage>();
            raw.texture = stage.Output;
            raw.raycastTarget = false;
            raw.color = ArenaBright;
            _arenaBackground = raw;

            var fitter = go.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = (float)ArenaStageView.StageWidth / ArenaStageView.StageHeight;
        }

        /// 좌하 — 위상/체력·그립·손 잠김 상태 줄 + 이번 교환 서술 줄 + 최근 5교환 로그.
        /// 경기 화면이 배경으로 깔린 뒤로는 흰 글자가 하늘/바닥 위에 그대로 떠 안 읽혀서
        /// PanelBg 스크림을 얹는다(위 ScoreboardRow 주석 참조 — 알파는 그쪽과 같은 값 재사용).
        void BuildNarrationLog(Transform parent)
        {
            var box = MakeVerticalGroup(parent, "NarrationLog", spacing: 4);
            // 2026-08-12 실전 UI 개편 후속(범위 밖이지만 이번에 한해 예외로 허용됨,
            // 사용자 확정) — NarrationLog 가 leftColumn 전체(756px 높이)를 그대로
            // 채우면 A 옆 손·코어·하체 트리거(고정 좌표 x≈475, 닫힌 상태 y=561~773)
            // 와 겹친다는 게 회귀 검사 실측으로 드러났다. 트리거 밴드 위쪽에 붙는
            // 고정 높이(280px)로 줄이고 leftColumn 상단에 앵커한다 — 아래쪽 빈
            // 공간은 경기 화면 배경이 그대로 보인다. 드롭다운이 열릴 때(예: 코어
            // 드롭다운은 최대 y=831 까지)는 NarrationLog 아래쪽과 살짝 겹칠 수
            // 있다 — 팝오버가 열려 있는 동안 뒤 콘텐츠를 잠깐 덮는 것과 같은
            // 종류라 허용(닫힌 상태 겹침만 실제 문제였다).
            var le = box.GetComponent<LayoutElement>();
            le.preferredHeight = le.minHeight = 280;
            le.flexibleHeight = 0;
            le.flexibleWidth = 1;
            parent.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperLeft;
            box.gameObject.AddComponent<Image>().color = PanelBg;

            _phaseText = MakeFlexText(box, "", TextAlignmentOptions.TopLeft, 1, 0, minHeight: 24);
            _statusTextA = MakeFlexText(box, "", TextAlignmentOptions.TopLeft, 1, 0, minHeight: 24);
            _statusTextB = MakeFlexText(box, "", TextAlignmentOptions.TopLeft, 1, 0, minHeight: 24);
            _resultText = MakeFlexText(box, "", TextAlignmentOptions.TopLeft, 1, 0, minHeight: 30,
                fontSizeOverride: 20);
            _logText = MakeFlexText(box, "", TextAlignmentOptions.TopLeft, 1, 1, minHeight: 60);
        }

        Transform MakeSidebarBox(Transform parent, string name, float flexHeight)
        {
            var go = new GameObject(name + "Box",
                typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = PanelBg;
            var lg = go.GetComponent<VerticalLayoutGroup>();
            lg.spacing = 4;
            lg.padding = new RectOffset(10, 10, 8, 8);
            lg.childForceExpandWidth = false;
            lg.childForceExpandHeight = false;
            var le = go.GetComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.flexibleHeight = flexHeight;
            return go.transform;
        }

        /// 사이드바 1 — 코치의 조언.
        void BuildCoachAdviceBox(Transform parent)
        {
            var box = MakeSidebarBox(parent, "CoachAdvice", 0.7f);
            MakeFlexText(box, "코치의 조언", TextAlignmentOptions.TopLeft, 1, 0, minHeight: 22,
                fontSizeOverride: 16).color = Warning;
            _adviceText = MakeFlexText(box, "", TextAlignmentOptions.TopLeft, 1, 1, minHeight: 40,
                fontSizeOverride: 16);
        }

        /// 사이드바 2 — 수 노트(상대 습관 관측). 행은 <see cref="RefreshNotes"/> 가
        /// 관측이 생길 때마다 다시 그린다 — 지금은 헤더만 짓는다.
        void BuildNotesBox(Transform parent)
        {
            var box = MakeSidebarBox(parent, "Notes", 1.6f);
            MakeFlexText(box, "수 노트", TextAlignmentOptions.TopLeft, 1, 0, minHeight: 22,
                fontSizeOverride: 16).color = Warning;
            _notesColumn = MakeVerticalGroup(box, "NotesRows", spacing: 4);
            SetFlexible(_notesColumn, 1, 1);
        }

        /// 사이드바 3 — 내 성향(노출 막대 이관). 경고선/위험선 라벨을 항상 보이게
        /// 고정하고, 걸렸을 때만 색을 올린다 — 값은 <see cref="RefreshExposure"/> 가 낸다.
        void BuildTendencyBox(Transform parent)
        {
            var box = MakeSidebarBox(parent, "Tendency", 1.0f);
            MakeFlexText(box, "내 성향", TextAlignmentOptions.TopLeft, 1, 0, minHeight: 22,
                fontSizeOverride: 16).color = Warning;

            var warnRow = MakeHorizontalGroup(box, "WarnRow", spacing: 8);
            SetFixedHeight(warnRow, 26);
            _warningMark = MakeMark(warnRow);
            _warningLabel = MakeFlexText(warnRow, "경고선 — 최근 6수 중 4수 이상 같음",
                TextAlignmentOptions.MidlineLeft, 1, 1, fontSizeOverride: 14);

            var dangerRow = MakeHorizontalGroup(box, "DangerRow", spacing: 8);
            SetFixedHeight(dangerRow, 26);
            _dangerMark = MakeMark(dangerRow);
            _dangerLabel = MakeFlexText(dangerRow, "위험선 — 3연속 완전동일",
                TextAlignmentOptions.MidlineLeft, 1, 1, fontSizeOverride: 14);
        }

        Image MakeMark(Transform parent)
        {
            var go = new GameObject("Mark", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = le.minWidth = 20;
            le.preferredHeight = le.minHeight = 20;
            le.flexibleWidth = 0;
            var img = go.GetComponent<Image>();
            img.color = Dim;
            return img;
        }

        /// 캐릭터 옆 손·코어·하체 방사형(휠) 메뉴 트리거 3개를 짓는다.
        ///
        /// 2026-08-12 1차 개편(트리거+위로 펼치는 드롭다운) 이후 같은 날 후속 —
        /// Play 실측 신고 두 건: 위로 펼치는 드롭다운이 A 모델을 가리고, 손 목록
        /// (최대 10개)은 세로로 화면 밖까지 잘렸다. 방사형 메뉴로 교체해 둘 다
        /// 구조적으로 없앴다 — 반원(−90°~+90°, 트리거 중심 기준 오른쪽으로 열림)
        /// 의 세로 도달폭은 ±반지름뿐이라 옛 드롭다운(최대 460px 이상 위로 직선
        /// 확장)보다 훨씬 작고, 트리거가 화면 왼쪽 끝에 있어 반지름을 A 실측 위치
        /// (캔버스 x=179)보다 작게 잡으면 A 와도 안 겹친다.
        ///
        /// 사용자 확정 사항(임의 변경 금지): 트리거는 화면 왼쪽 끝에 세로로(§H 2b —
        /// 기술 트리거 추가로 4개가 됐다), 선택지는 오른쪽으로 펼침, 완전한 원 아닌
        /// 적당한 호, 손·기술처럼 항목 많은 축은 곡률을 완만하게(=반지름을 키워
        /// 항목 간격을 넉넉히).
        /// 반지름은 2026-08-12 실측·보고 후 사용자 확정: 손(최대 10개) 130,
        /// 코어(3개) 90, 하체(4개) 100 — 항목이 적어 더 좁혀도 간격이 넉넉해 같은
        /// 여유 원칙으로 판단해 정함(사용자가 직접 지정한 값은 손뿐, 코어·하체는
        /// 이 판단에 포함). §H 2b 에서 기술(8개) 120 을 같은 원칙으로 추가 확정.
        /// B(캔버스 x=974~)와는 어떤 반지름에서도 안 겹친다(최대 반지름 130 이어도
        /// 500px 이상 여유). A(캔버스 x≈111~179, 두 측정치 사이 — 재실측해도 정확한
        /// 값이 안 갈렸다, STATUS 참조)와는 이미 겹침을 허용하고 딤 처리로 흡수하는
        /// 게 확정된 원칙이다.
        ///
        /// 트리거 자체는 축 이름만 표시하는 작은 버튼(74x36)이다 — 트리거가 넓으면
        /// 그 중심(=휠 원점)이 A 쪽으로 밀려 여유가 줄어든다(실측 중 발견해 트리거를
        /// 축 이름만 표시하도록 좁힌 이유). 현재 선택은 트리거 바로 아래 별도
        /// 텍스트(<see cref="_handSummary"/> 등)로 낸다 — "닫힌 상태에서도 축이
        /// 트리거 쪽만 보면 다 읽힌다"는 요구는 그대로 만족.
        ///
        /// <b>§H 2b 후속(Play 실측 신고) — TriggerX=55.</b> A 쪽 여유만 챙기다가
        /// 반대쪽(화면 왼쪽 경계)을 안 쟀다 — 트리거 X=25 는 트리거 자신(폭 74,
        /// 절반 37)조차 25−37=−12 로 화면 밖이었다(요약 텍스트는 폭 90 이라 더
        /// 심하게 −20, 휠 항목 중 정확히 ±90°에 놓이는 것들 — 모든 축의 첫/끝
        /// 항목, 각도 공식이 반지름과 무관하게 그 자리에 놓는다 — 도 오프셋이
        /// 0이라 트리거와 똑같이 잘렸다). 반지름을 조정해도 이 항목들은 안 풀린다
        /// (오프셋이 애초에 반지름과 무관하게 0). TriggerX 를 55로 올려 셋 다(트리거
        /// 37+18 여유, 요약 45+10 여유, ±90° 휠 항목 37+18 여유) 화면 안에 들어오게
        /// 했다 — 반지름·각도 공식은 무수정(호를 좁히면 손 축 스테이지1의 "cos90°=0
        /// 이라 A 와 안 겹친다" 성질이 깨진다, 아래 참조).
        ///
        /// 세로 배치 — §H 2b 에서 4트리거로 재배치(사용자 확정, "4개 전체 재배치").
        /// 기술(8개, R=120)을 별도 트리거로 넣기로 하면서, 기존 3트리거(110px 간격,
        /// 440/550/660)로는 새 트리거가 들어갈 자리가 없었다(위/아래 어느 쪽에 붙여도
        /// 남는 반지름이 22px 안팎뿐 — R=120 은커녕 R=100 도 못 들어간다, 실측·보고).
        /// 그래서 4개를 안전 구간 안에서 다시 4등분했다: 아래쪽 경계 ConfirmRow 상단
        /// (y=308), 위쪽 경계 NarrationLog 상단(y=792, 둘 다 2026-08-12 세션 실측
        /// 확정값 — 이번에 바뀐 적 없음) 사이 484px 를 81px 간격으로 나눈다
        /// (244/3 — 각 축이 자기 반지름만큼 열렸을 때 그 두 경계를 안 넘는 축만
        /// 이 폭 안에 놓일 수 있다는 계산 결과, 사람이 지정한 정확한 값은 아니다).
        /// 반지름이 큰 축일수록 중앙에 가깝게: 손(130)·기술(120) 이 가운데 두 자리,
        /// 코어(90)·하체(100) 이 바깥 두 자리 — 기존 "반지름 큰 축을 가운데" 원칙
        /// 그대로. 상대적 순서(하체 아래·손 그다음·코어 맨 위)도 기존과 같게 유지하고
        /// 기술만 손과 코어 사이에 끼워 넣었다.
        ///
        /// **주의 — 반지름 130/90/100(손/코어/하체) 은 "임의 변경 금지"로 확정된 값
        /// 이라 이번에도 안 건드렸다.** 바뀐 건 Y 좌표뿐이다(애초에 "사람이 지정한 게
        /// 아니라 실측한 안전 구간 안에서 고른 판단"이라고 명시돼 있던 값 — 그 판단을
        /// 4트리거 조건으로 다시 계산했을 뿐).
        void BuildFloatingControls(Transform canvasRoot)
        {
            var go = new GameObject("FloatingControls", typeof(RectTransform));
            go.transform.SetParent(canvasRoot, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            _floatingControlsRoot = go;

            (_legsTrigger, _legsDropdown, _legsSummary) = BuildAxisControl(go.transform, "하체", new Vector2(TriggerX, 428));
            // 손 트리거를 열 때마다 스테이지1(손 고르기)로 되돌린다 — 지난번에 스테이지2
            // (타겟 고르기)까지 갔다가 확정 없이 닫았어도, 다시 열면 항상 손부터 다시
            // 고른다(§A.1, v4.0. 안 하면 닫혀 있던 동안 유지된 드롭다운 내용이 그대로
            // 남아 스테이지2가 먼저 뜬다).
            (_handTrigger, _handDropdown, _handSummary) = BuildAxisControl(go.transform, "손", new Vector2(TriggerX, 509),
                onOpening: () => { _handStageTarget = false; _handStage = HandWheelStage.PickHandOrEvade;
                                    RebuildStandingColumns(); });
            (_techniqueTrigger, _techniqueDropdown, _techniqueSummary) =
                BuildAxisControl(go.transform, "기술", new Vector2(TriggerX, 591));
            (_coreTrigger, _coreDropdown, _coreSummary) = BuildAxisControl(go.transform, "코어", new Vector2(TriggerX, 672));
        }

        // §H 2b 후속(사용자 Play 실측 신고) — 트리거 X=25 는 트리거 자체(74px 폭,
        // 절반 37)조차 왼쪽으로 12px 화면 밖(25−37=−12)이었다. 이건 이번 세션이 만든
        // 문제가 아니라 원래 3트리거 방사형 설계(2026-08-12 6차 세션)부터 있던
        // 것 — 그때는 트리거 자체의 화면 경계를 실측하지 않고 A 겹침만 쟀다. 요약
        // 텍스트(90px 폭, 절반 45)는 더 심해서 25−45=−20. 휠 항목 중 정확히 ±90°에
        // 놓이는 항목(모든 축의 첫/끝 항목 — 각도 공식이 n 과 무관하게 인덱스 0/끝을
        // 항상 ±90°에 놓기 때문)도 오프셋이 0이라 트리거와 똑같이 잘린다 — 반지름을
        // 아무리 조정해도 이 항목들은 안 풀린다(오프셋이 애초에 반지름과 무관하게 0).
        // 손 축 스테이지1(왼손/오른손, 2항목)이 정확히 ±90°에 있는 것은 §A.1 에서
        // "cos(90°)=0 이라 A 와 안 겹친다"고 의도적으로 고른 성질이었다 — 트리거를
        // 오른쪽으로 옮기면 이 항목들은 여전히 오프셋 0(A 쪽으로 더 안 뻗는다)이라
        // 그 성질은 그대로 유지된다. TriggerX=55 는 셋 다(트리거 37+여유 18, 요약
        // 45+여유 10, ±90° 휠 항목 37+여유 18) 화면 안에 들어오는 최소값 위에 여유를
        // 얹은 값 — 반지름(130/90/100/120, 임의 변경 금지)은 무수정, 각도 공식도
        // 무수정(호를 좁히면 손 스테이지1 의 위 성질이 깨진다). A 겹침은 이미 딤
        // 처리로 허용된 상태라 트리거가 30px 오른쪽으로 옮겨가며 그만큼 커져도(예:
        // 기술 휠 최대 도달점 179→209) 새 문제가 아니라 기존에 이미 승인된 겹침
        // 범주 안이다(손 축 자체가 이미 실측 "최대 145px" 겹침을 승인받은 전례).
        const float TriggerX = 55f;

        /// 트리거 버튼 하나 + 그 아래 선택 요약 텍스트 + 오른쪽으로 펼치는 휠(방사형
        /// 메뉴) 컨테이너. 부모(<paramref name="anchoredPos"/> 를 쓰는 anchor
        /// 오브젝트)는 레이아웃 그룹이 아닌 순수 고정 좌표 앵커라, 자식들의 위치·
        /// 크기는 LayoutElement 가 아니라 RectTransform 을 직접 쓴다(레이아웃 그룹
        /// 안에서만 의미가 있는 LayoutElement 헬퍼들과 다른 규칙 — 실수로
        /// SetFlexible 등을 여기 쓰면 아무 효과가 없다). anchor 의 원점 자체가
        /// 휠의 각도 계산 기준점이다(<see cref="RebuildAxisOptions"/> 참조).
        ///
        /// <paramref name="onOpening"/> 은 휠이 닫힌 상태에서 트리거를 눌러 "열릴" 때만
        /// (닫힐 때는 안) 호출된다 — 열림 여부를 SetActive 가 바뀌기 전에 판정해서 넘기므로
        /// 순서 걱정 없이 쓸 수 있다. 손 축이 스테이지를 리셋하는 데 쓴다(§A.1).
        (Button trigger, Transform dropdown, TMP_Text summary) BuildAxisControl(
            Transform parent, string axisId, Vector2 anchoredPos, System.Action onOpening = null)
        {
            var anchor = new GameObject(axisId + "Control", typeof(RectTransform));
            anchor.transform.SetParent(parent, false);
            var art = (RectTransform)anchor.transform;
            art.anchorMin = art.anchorMax = Vector2.zero;
            art.pivot = new Vector2(0.5f, 0.5f);
            art.anchoredPosition = anchoredPos;
            art.sizeDelta = Vector2.zero;

            var trigger = MakeButton(anchor.transform, axisId, null);
            var trt = (RectTransform)trigger.transform;
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.pivot = new Vector2(0.5f, 0.5f);
            trt.anchoredPosition = Vector2.zero;
            trt.sizeDelta = new Vector2(74, 36);

            // 트리거(36 높이, 중심 기준 ±18)와 6px 겹쳤던 첫 시도(-22, 실측으로 발견해
            // 고침)를 -32 로 내려 4px 여유를 뒀었다 — §H 2b 후속(사용자 Play 실측
            // 신고): 기하학적으로는 안 겹쳤지만(트리거 하단 -18, 요약 상단 -32+10=-22,
            // 4px 간격 실측 확인) 육안으로는 너무 붙어 읽기 어려웠다. -38 로 다시
            // 내려 10px 여유로 넓혔다(요약 텍스트 자신의 높이 20, 중심 기준 ±10은
            // 그대로) — 위 축과의 간격(81px 트리거 간격 기준 최소 13px)도 재확인해
            // 여전히 안 겹친다.
            var summary = MakeText(anchor.transform, "", TextAlignmentOptions.Top, new Vector2(0, -38),
                new Vector2(90, 20), new Vector2(0.5f, 0.5f), fontSizeOverride: 13, singleLine: true);

            var dropdown = new GameObject(axisId + "Wheel", typeof(RectTransform));
            dropdown.transform.SetParent(anchor.transform, false);
            var drt = (RectTransform)dropdown.transform;
            drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f);
            drt.pivot = new Vector2(0.5f, 0.5f);
            drt.anchoredPosition = Vector2.zero;
            drt.sizeDelta = Vector2.zero;
            dropdown.SetActive(false);

            trigger.onClick.AddListener(() =>
            {
                if (!dropdown.activeSelf) onOpening?.Invoke();   // about to open, not close
                ToggleDropdown(dropdown);
            });
            return (trigger, dropdown.transform, summary);
        }

        /// 한 번에 하나만 열리게 한다 — 트리거를 눌렀을 때 그게 이미 열려 있었으면
        /// 닫고(토글), 아니면 전부 닫은 뒤 그것만 연다.
        void ToggleDropdown(GameObject dropdown)
        {
            bool willOpen = !dropdown.activeSelf;
            CloseAllWheels();
            if (willOpen) OpenWheel(dropdown);
        }

        /// 휠 셋 다 닫고 트리거·요약 텍스트를 전부 다시 보이게 한다. 항목을
        /// 골랐을 때(<see cref="RebuildAxisOptions"/> 의 onPick 클로저)도 이 경로를
        /// 타야 숨겨뒀던 다른 두 축 트리거가 되살아난다 — 단순히 연 휠만 닫으면
        /// 그 복구가 빠진다.
        void CloseAllWheels()
        {
            _handDropdown.gameObject.SetActive(false);
            _coreDropdown.gameObject.SetActive(false);
            _legsDropdown.gameObject.SetActive(false);
            _techniqueDropdown.gameObject.SetActive(false);
            _handTrigger.gameObject.SetActive(true);
            _coreTrigger.gameObject.SetActive(true);
            _legsTrigger.gameObject.SetActive(true);
            _techniqueTrigger.gameObject.SetActive(true);
            _handSummary.gameObject.SetActive(true);
            _coreSummary.gameObject.SetActive(true);
            _legsSummary.gameObject.SetActive(true);
            _techniqueSummary.gameObject.SetActive(true);
            _arenaBackground.color = ArenaBright;
        }

        /// <paramref name="dropdown"/> 을 열고 나머지 두 축의 트리거+요약은 숨긴다.
        /// 트리거끼리 세로로 가깝게(110px 간격) 쌓여 있어 휠이 열리면(반지름 최대
        /// 130) 위아래로 그 옆 트리거 자리까지 닿는다 — 숨기지 않으면 휠의 양끝
        /// 항목과 시각적으로 겹친다(2026-08-12 계산: 손 휠(중심 y=550, 반지름
        /// 130)이 y=420~680 을 덮어 하체(440)·코어(660) 트리거 자리를 그대로
        /// 지나간다). 셋 다 겹칠 걱정 없이 한 번에 하나씩만 조작하게 만드는 목적도
        /// 겸한다(다른 축을 실수로 못 누르게).
        void OpenWheel(GameObject dropdown)
        {
            dropdown.SetActive(true);
            _arenaBackground.color = ArenaDim;
            if (dropdown != _handDropdown.gameObject)
            {
                _handTrigger.gameObject.SetActive(false);
                _handSummary.gameObject.SetActive(false);
            }
            if (dropdown != _coreDropdown.gameObject)
            {
                _coreTrigger.gameObject.SetActive(false);
                _coreSummary.gameObject.SetActive(false);
            }
            if (dropdown != _legsDropdown.gameObject)
            {
                _legsTrigger.gameObject.SetActive(false);
                _legsSummary.gameObject.SetActive(false);
            }
            if (dropdown != _techniqueDropdown.gameObject)
            {
                _techniqueTrigger.gameObject.SetActive(false);
                _techniqueSummary.gameObject.SetActive(false);
            }
        }

        GameObject BuildOsaekomiRoot(Transform parent)
        {
            var t = MakeHorizontalGroup(parent, "OsaekomiInput", spacing: 24);
            var rt = (RectTransform)t;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var lg = t.GetComponent<HorizontalLayoutGroup>();
            lg.padding = new RectOffset(60, 60, 40, 40);
            lg.childAlignment = TextAnchor.MiddleCenter;

            _osaekomiOptionA = MakeButton(t, "", null);
            SetFlexible(_osaekomiOptionA.transform, 1, 1);
            _osaekomiLabelA = _osaekomiOptionA.GetComponentInChildren<TMP_Text>();

            _osaekomiOptionB = MakeButton(t, "", null);
            SetFlexible(_osaekomiOptionB.transform, 1, 1);
            _osaekomiLabelB = _osaekomiOptionB.GetComponentInChildren<TMP_Text>();

            return t.gameObject;
        }

        // ── 레이아웃 그룹 헬퍼 — 좌표를 손으로 찍는 대신 Unity 가 계산하게 만드는
        // 자리. flexibleWidth/Height 는 명시적으로 0 이 아니면 그 축의 남는 공간을
        // 그 비율로 나눠 갖고, 0(=SetFixed*)이면 고정 크기를 유지한다. 두 그룹 다
        // childForceExpandWidth/Height 를 꺼둔다 — 모든 자식에 이미 명시적으로 값을
        // 주므로, Unity 의 "비어있으면 강제로 늘린다" 규칙에 기대지 않기 위해서다.

        Transform MakeVerticalGroup(Transform parent, string name, float spacing = 4)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var lg = go.GetComponent<VerticalLayoutGroup>();
            lg.spacing = spacing;
            lg.childForceExpandWidth = false;
            lg.childForceExpandHeight = false;
            return go.transform;
        }

        Transform MakeHorizontalGroup(Transform parent, string name, float spacing = 4)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var lg = go.GetComponent<HorizontalLayoutGroup>();
            lg.spacing = spacing;
            lg.childForceExpandWidth = false;
            lg.childForceExpandHeight = false;
            return go.transform;
        }

        void SetFlexible(Transform t, float flexW, float flexH)
        {
            var le = t.GetComponent<LayoutElement>();
            le.flexibleWidth = flexW;
            le.flexibleHeight = flexH;
        }

        /// <paramref name="t"/> 는 세로 그룹의 자식으로 쓰인다 — 그 관계에서 폭은 교차축이다.
        /// 실측(2026-08-12)으로 확인한 사실: preferred/min/flexible 셋 다 안 건드리면(전부
        /// 기본값 −1) 그 축은 화면 크기와 무관하게 <b>항상</b> 0으로 무너진다 — 넓은 화면에서도
        /// 마찬가지였다(겹침 검사만 통과했을 뿐 실제로는 0폭이었다, 쌍별 겹침 계산이 0폭
        /// 요소끼리는 애초에 안 걸리는 사각지대). 셋 중 하나만 양수면 그 축은 정상 동작한다 —
        /// 여기서는 flexibleWidth=1 로 "남는 폭을 가져간다"를 명시한다.
        void SetFixedHeight(Transform t, float h)
        {
            var le = t.GetComponent<LayoutElement>();
            le.preferredHeight = h;
            le.minHeight = h;
            le.flexibleHeight = 0;
            le.flexibleWidth = 1;
        }

        void SetFixedWidth(Transform t, float w)
        {
            var le = t.GetComponent<LayoutElement>();
            le.preferredWidth = w;
            le.minWidth = w;
            le.flexibleWidth = 0;
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
            // 세로 그룹(열) 안에서 폭은 교차축이다 — 위 SetFixedHeight 주석과 같은 이유로
            // 명시하지 않으면 버튼 폭이 0으로 무너진다. 확정/굳히기 버튼처럼 고정 폭이
            // 필요하면 생성 후 SetFixedWidth/SetFlexible 로 덮어쓴다(이 기본값보다 나중에 적용됨).
            le.flexibleWidth = 1;

            var img = go.GetComponent<Image>();
            img.color = Idle;

            var btn = go.GetComponent<Button>();
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            MakeText(go.transform, label, TextAlignmentOptions.Center, Vector2.zero,
                new Vector2(220, 44), new Vector2(0.5f, 0.5f), stretch: true, singleLine: true);

            return btn;
        }

        /// 레이아웃 그룹 안에 들어가는 텍스트 한 줄. 축마다 flexible 값을 명시적으로
        /// 받는다 — 부모가 세로 그룹이면 flexH 가 "그 축(주축)에서 남는 공간을 갖는가",
        /// 부모가 가로 그룹이면 flexW 가 그 역할이다(반대 축은 childControlWidth/Height
        /// 가 이미 항상 꽉 채운다, MakeButton 이 이미 검증한 동작).
        TMP_Text MakeFlexText(Transform parent, string text, TextAlignmentOptions align, float flexW, float flexH,
                              float minHeight = 0, float? fontSizeOverride = null)
        {
            var t = MakeText(parent, text, align, Vector2.zero, new Vector2(10, 10), new Vector2(0, 1),
                fontSizeOverride: fontSizeOverride);
            var le = t.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = flexW;
            le.flexibleHeight = flexH;
            le.minHeight = minHeight;
            // preferred 도 같이 준다 — min 만으로 그 축이 채워지는지 실측으로 확인하지
            // 않았다(확인된 조합은 "preferred+min 같이" 또는 "flexible 단독" 둘뿐이다).
            // flexH>0 인 줄(로그·조언 등)은 이 값이 바닥값이 되고 남는 공간은 flexible 이 가져간다.
            le.preferredHeight = minHeight;
            return t;
        }

        /// 실전 유도 점수판 규격 (사진 레퍼런스). 좌표는 전부 이 루트 안의 로컬
        /// 오프셋이다 — 캔버스 기준 절대 좌표를 매 칸마다 다시 계산하지 않으려고
        /// 자체 컨테이너를 둔다. 이 루트의 부모(ScoreboardRow)가 이제는 RootLayout 의
        /// 자식이지만, ScoreboardRow 자체는 레이아웃 그룹이 아닌 고정 높이 슬롯이라
        /// 안쪽 좌표(anchorMin/Max=(0.5,1), anchoredPosition 등)는 예전과 동일하게 먹힌다.
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

        /// <paramref name="singleLine"/> — 좁은 화면에서 줄바꿈으로 세로 쌓이면 안 되는
        /// 라벨(버튼 글자 등)용. 줄바꿈을 끄고 TMP auto-size 로 폭에 맞춰 글자 크기만
        /// 줄인다. 로그·서술처럼 원래도 여러 줄일 수 있는 텍스트는 false(기본) 로 두면
        /// 줄바꿈은 그대로 두되 auto-size 로 "줄일 수 있으면 먼저 줄이고, 그래도 안 되면
        /// 그때 줄바꿈"이 되어 불필요한 조기 줄바꿈을 줄인다.
        TMP_Text MakeText(Transform parent, string text, TextAlignmentOptions align, Vector2 offset,
                          Vector2 size, Vector2? anchor = null, bool stretch = false,
                          float? fontSizeOverride = null, bool singleLine = false)
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
            float nominalSize = fontSizeOverride ?? fontSize;
            t.fontSize = nominalSize;
            t.enableAutoSizing = true;
            t.fontSizeMax = nominalSize;
            t.fontSizeMin = Mathf.Max(8, nominalSize * 0.4f);
            if (singleLine) t.textWrappingMode = TextWrappingModes.NoWrap;
            t.alignment = align;
            t.text = text;
            t.raycastTarget = false;
            return t;
        }
    }
}
