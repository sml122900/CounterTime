using System.Collections.Generic;
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

        // 2026-08-12 실전 UI 개편 — 손·코어·하체 상시 노출 3열 폐지, 캐릭터 옆 트리거
        // 버튼 + 위로 펼치는 드롭다운으로 전환(사용자 결정: 주체 기준=전부 A 옆,
        // 화면 고정 위치=본 추적 안 함). 상세 근거는 md/STATUS.md 참조.
        Button _handTrigger, _coreTrigger, _legsTrigger, _confirmButton;
        Transform _handDropdown, _coreDropdown, _legsDropdown;
        GameObject _floatingControlsRoot;
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
            _handChosen = _coreChosen = _legsChosen = false;
            RebuildStandingColumns();
            RefreshExposure();
            RefreshConfirmButton();
        }

        void RebuildStandingColumns()
        {
            // 그립 상태가 바뀌지 않는 한(확정 전) 손 후보는 그대로다 — 선택 강조만 새로 칠한다.
            RebuildAxisOptions(_handDropdown, _runner.A.HandOptions(), HandLabel, o => _hand.Equals(o),
                o => { _hand = (HandTarget)o; _handChosen = true; RebuildStandingColumns(); RefreshExposure(); RefreshConfirmButton(); });
            RebuildAxisOptions(_coreDropdown, _runner.A.CoreOptions(), CoreLabel, o => _core.Equals(o),
                o => { _core = (CoreStance)o; _coreChosen = true; RebuildStandingColumns(); RefreshExposure(); RefreshConfirmButton(); });
            RebuildAxisOptions(_legsDropdown, _runner.A.LegOptions(), LegsLabel, o => _legs.Equals(o),
                o => { _legs = (LegAction)o; _legsChosen = true; RebuildStandingColumns(); RefreshExposure(); RefreshConfirmButton(); });

            // 드롭다운이 닫혀 있어도 3축 상태가 한눈에 보이도록 트리거 라벨에 반영
            // (2026-08-12 사용자 확정 — 옛 "선택: —·중립·중립" 요약 줄을 대체).
            _handTrigger.GetComponentInChildren<TMP_Text>().text = "손: " + (_handChosen ? HandLabel(_hand) : "—");
            _coreTrigger.GetComponentInChildren<TMP_Text>().text = "코어: " + CoreLabel(_core);
            _legsTrigger.GetComponentInChildren<TMP_Text>().text = "하체: " + LegsLabel(_legs);
        }

        void Confirm()
        {
            string myChoiceLabel = $"{HandLabel(_hand)} · {CoreLabel(_core)} · {LegsLabel(_legs)}";
            BeginPending();
            var choice = _runner.A.Build(_hand, _core, _legs);
            _runner.SubmitA(choice);
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
            var candidate = _runner.A.Build(_hand, _core, _legs);
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

        void RebuildAxisOptions<T>(Transform dropdown, IReadOnlyList<T> options, System.Func<T, string> label,
                                   System.Func<object, bool> isSelected, System.Action<object> onPick)
        {
            for (int i = dropdown.childCount - 1; i >= 0; i--)
                Destroy(dropdown.GetChild(i).gameObject);   // 확정 전 재구성뿐이라 즉시 파괴로 충분

            foreach (var opt in options)
            {
                var captured = opt;
                var btn = MakeButton(dropdown, label(opt), () => { onPick(captured); dropdown.gameObject.SetActive(false); });
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
            HandTarget.Setup => "흔들기",
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
            string setup = _runner.Config.SetupGateEnabled
                ? $"  흔들기 {(eng.IsSetupActive(who) ? "유효" : "없음")}"
                : "";
            return $"{FighterLabel(who)}  체력 {stamina}  그립 {grip:+0;-0;0}  " +
                   $"왼손 {left} 오른손 {right}{setup}";
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
            else if (_runner.Config.SetupGateEnabled && eng.IsSetupActive(FighterId.B))
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

        /// 캐릭터 옆 손·코어·하체 드롭다운 트리거 3개를 짓는다. 2026-08-12 사용자
        /// 결정 3건: (a) 주체 기준 — 전부 A 캐릭터 옆(상대를 노리는 손도 포함) —
        /// 조작 버튼이 한 곳에 몰려 실수 적음. (b) 화면 고정 위치 — 본 추적 안 함.
        /// A 의 Fight Idle 1.5초(12fps 18스텝) 실측: 골반 x±5.1/y±2.0px, 머리
        /// x±17.5/y±2.9px, 오른손 x±10.3/y±11.3px — 본을 그대로 따라가면 83ms
        /// 마다 버튼이 튀어 보인다(클릭 대상은 정밀 포즈 추종보다 안정성이 중요).
        /// (c) 위로 펼침 — 코어(144px)·하체(194px) 드롭다운은 A 자신 위로만 펼쳐져
        /// 상대(B)와 안 겹친다(A·B 사이 여백 179px 실측 확인). 손 드롭다운은 그립
        /// 상황에 따라 옵션이 최대 10개(≈460px)까지 늘어날 수 있어 세 트리거 중
        /// 가장 위(어깨 높이)에 두어도 화면 위로 넘칠 수 있다 — 알려진 한계, 코드로
        /// 안 고침(스크롤 등은 범위 밖). 트리거끼리도 가깝게 쌓아 위쪽 드롭다운이
        /// 열리면 아래 트리거를 잠깐 가릴 수 있다 — 한 번에 하나만 열리므로
        /// (<see cref="ToggleDropdown"/>) 치명적이지 않다고 판단.
        /// 좌표는 1920 기준 캔버스 좌표 실측값(카메라 x=0.6 상태에서 A 본 위치) —
        /// CanvasScaler ScaleWithScreenSize 라 이 고정 오프셋은 다른 해상도에서도
        /// 비례적으로 맞는다(BuildScoreboard 의 고정 오프셋과 같은 원리).
        void BuildFloatingControls(Transform canvasRoot)
        {
            var go = new GameObject("FloatingControls", typeof(RectTransform));
            go.transform.SetParent(canvasRoot, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            _floatingControlsRoot = go;

            (_legsTrigger, _legsDropdown) = BuildAxisControl(go.transform, "하체", new Vector2(475, 561));
            (_coreTrigger, _coreDropdown) = BuildAxisControl(go.transform, "코어", new Vector2(475, 647));
            (_handTrigger, _handDropdown) = BuildAxisControl(go.transform, "손", new Vector2(475, 733));
        }

        /// 트리거 버튼 하나 + 그 위로 펼쳐지는 드롭다운 하나. 부모(<paramref name="anchoredPos"/>
        /// 를 쓰는 anchor 오브젝트)는 레이아웃 그룹이 아닌 순수 고정 좌표 앵커라,
        /// 자식들의 위치·크기는 LayoutElement 가 아니라 RectTransform 을 직접 쓴다
        /// (레이아웃 그룹 안에서만 의미가 있는 LayoutElement 헬퍼들과 다른 규칙 —
        /// 주석으로 밝혀둔다, 실수로 SetFlexible 등을 여기 쓰면 아무 효과가 없다).
        (Button trigger, Transform dropdown) BuildAxisControl(Transform parent, string axisId, Vector2 anchoredPos)
        {
            var anchor = new GameObject(axisId + "Control", typeof(RectTransform));
            anchor.transform.SetParent(parent, false);
            var art = (RectTransform)anchor.transform;
            art.anchorMin = art.anchorMax = Vector2.zero;
            art.pivot = new Vector2(0.5f, 0);
            art.anchoredPosition = anchoredPos;
            art.sizeDelta = Vector2.zero;

            var trigger = MakeButton(anchor.transform, axisId, null);
            var trt = (RectTransform)trigger.transform;
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0);
            trt.pivot = new Vector2(0.5f, 0);
            trt.anchoredPosition = Vector2.zero;
            trt.sizeDelta = new Vector2(150, 40);

            var dropdown = MakeVerticalGroup(anchor.transform, axisId + "Dropdown", spacing: 4);
            var drt = (RectTransform)dropdown;
            drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0);
            drt.pivot = new Vector2(0.5f, 0);
            drt.anchoredPosition = new Vector2(0, 44);   // 트리거(40px) 바로 위, 4px 여백
            drt.sizeDelta = new Vector2(170, 0);          // 높이는 ContentSizeFitter 가 정함
            dropdown.gameObject.AddComponent<Image>().color = PanelBg;
            dropdown.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            dropdown.gameObject.SetActive(false);

            trigger.onClick.AddListener(() => ToggleDropdown(dropdown.gameObject));
            return (trigger, dropdown);
        }

        /// 한 번에 하나만 열리게 한다 — 트리거를 눌렀을 때 그게 이미 열려 있었으면
        /// 닫고(토글), 아니면 셋 다 닫은 뒤 그것만 연다.
        void ToggleDropdown(GameObject dropdown)
        {
            bool willOpen = !dropdown.activeSelf;
            _handDropdown.gameObject.SetActive(false);
            _coreDropdown.gameObject.SetActive(false);
            _legsDropdown.gameObject.SetActive(false);
            dropdown.SetActive(willOpen);
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
