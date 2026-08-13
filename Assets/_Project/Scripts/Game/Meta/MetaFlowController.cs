using System;
using System.Collections.Generic;
using CounterTime.Brain;
using CounterTime.Core;
using CounterTime.Game.Match;
using CounterTime.Game.Save;
using CounterTime.Game.Training;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CounterTime.Game.Meta
{
    /// <summary>
    /// §H 3.5b — META_DESIGN.md §M2~M4 뼈대: 지도(유도장/집/경기장) → 연습 대회 32강
    /// 대진표 → 매치 → 세이브. 경기 엔진(<see cref="MatchEngine"/>/판정 규칙)은 무수정 —
    /// 이 컴포넌트는 이미 있는 <see cref="LiveMatchRunner"/>/<see cref="LiveMatchPanel"/>
    /// 을 그대로 갖다 쓰는 오케스트레이션·메타 UI 계층이다.
    ///
    /// [설계 판단] 지도↔장소·경기 전환은 씬이 아니라 <b>패널</b>이다(META_DESIGN §M5가
    /// "빌드 시점 실측으로 판단" 이라 명시한 지점). LiveMatch 는 이미 씬 수정 없이 코드로
    /// 런타임 캔버스를 짓는 패턴을 쓰고 있어서(<see cref="LiveMatchPanel"/>,
    /// <see cref="ArenaStageView"/> 둘 다 씬에 아무것도 안 남긴다) 같은 패턴을 그대로
    /// 따르는 게 자연스럽다. 매치가 끝나도 LiveMatch GameObject 는 파괴하지 않고
    /// 비활성화만 한다 — <see cref="LiveMatchRunner.Rebuild"/> 로 엔진만 새로 만들고
    /// <see cref="LiveMatchPanel.ResetForNewMatch"/> 로 화면만 새로고쳐 재사용한다(3D
    /// 무대를 매 매치 다시 짓는 것보다 훨씬 저렴하고, 에셋 배선을 런타임에 다시 할
    /// 필요도 없다).
    ///
    /// [설계 판단] 상대 정체성 — §H 3.5b-로스터부터 6명이 실제로 구별되는 습관으로
    /// 싸운다(<see cref="OpponentFactoryFor"/>). 대진표 화면에 보이는 라운드별 상대
    /// 이름(<see cref="TournamentBracket.PlayerOpponentName"/>)과 실제로 붙는 엔진
    /// 상대(<see cref="LiveMatchRunner.SetOpponent"/>)가 이제 같은 스케줄
    /// (<see cref="TournamentBracket"/>)을 공유한다.
    /// </summary>
    public sealed class MetaFlowController : MonoBehaviour
    {
        const string DefaultPlayerName = "플레이어";

        [SerializeField] TMP_FontAsset koreanFont;
        [SerializeField] LiveMatchRunner liveMatchRunner;
        [SerializeField] LiveMatchPanel liveMatchPanel;
        [SerializeField] GameObject liveMatchRoot;
        [SerializeField] int bracketSeed = 777;
        [SerializeField] TrainingSessionController trainingSession;
        [SerializeField] TrainingCatalog trainingCatalog;

        static readonly Color ScreenBg = new Color(0.12f, 0.14f, 0.18f);
        static readonly Color PanelBg = new Color(0.18f, 0.18f, 0.20f, 0.95f);
        static readonly Color ButtonBg = new Color(0.25f, 0.25f, 0.28f);
        static readonly Color Highlight = new Color(0.95f, 0.85f, 0.1f);
        static readonly Color Dim = new Color(0.6f, 0.6f, 0.6f);
        static readonly Color OverlayBg = new Color(0, 0, 0, 0.75f);

        SaveData _save;
        TournamentBracket _bracket;

        GameObject _metaCanvasGo;
        GameObject _mapRoot, _trainingRoot, _restRoot, _tournamentListRoot, _bracketRoot, _trainingRunRoot;
        TMP_Text _mapStatusText, _tournamentListStatusText;
        Transform _bracketContent;

        GameObject _overlayCanvasGo, _matchResultRoot;
        TMP_Text _matchResultText;
        bool _pendingPlayerWon;

        // ── 훈련(DESIGN_training §5-A) ─────────────────────────────────────────
        readonly List<string> _trainingSelectedIds = new List<string>();
        readonly Dictionary<string, Button> _trainingToggleButtons = new Dictionary<string, Button>();
        readonly List<TrainingSessionController.Result> _trainingResults = new List<TrainingSessionController.Result>();
        List<string> _trainingQueue = new List<string>();
        int _trainingQueueIndex;
        TMP_Text _trainingSelectCountText, _trainingDisciplineLabel, _trainingRepText, _trainingFatigueText, _trainingResultText;
        Button _trainingStartButton, _trainingRepButton, _trainingResultButton;
        RawImage _trainingStageImage;

        void Awake()
        {
            if (koreanFont == null)
                koreanFont = Resources.Load<TMP_FontAsset>(LiveMatchPanel.KoreanFontResourceName);

            _save = SaveSystem.Read();
            if (string.IsNullOrEmpty(_save.playerName)) _save.playerName = DefaultPlayerName;
            _bracket = new TournamentBracket(_save.playerName, bracketSeed);

            BuildCanvas();
            liveMatchRoot.SetActive(false);
            liveMatchRunner.Presented += OnMatchEvent;
            trainingSession.gameObject.SetActive(false);
            trainingSession.SessionEnded += OnTrainingDisciplineEnded;

            ShowMap();
        }

        void OnDestroy()
        {
            liveMatchRunner.Presented -= OnMatchEvent;
            trainingSession.SessionEnded -= OnTrainingDisciplineEnded;
        }

        void Update()
        {
            if (_trainingRunRoot != null && _trainingRunRoot.activeSelf &&
                (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
            {
                OnTrainingRepInput();
            }
        }

        void OnApplicationQuit() => SaveNow();

        void SaveNow() => SaveSystem.Write(_save);

        // ── 매치 시작/종료 ───────────────────────────────────────────────────────

        void StartMatch()
        {
            // SetActive(true) 가 먼저다 — LiveMatch GameObject 는 첫 매치 전까지
            // 비활성 상태라 LiveMatchRunner/LiveMatchPanel 의 Awake(그 안의
            // BuildCanvas 포함)가 아직 한 번도 안 돌았다. Rebuild/ResetForNewMatch 를
            // 먼저 부르면 캔버스가 아직 없어 NullReferenceException 이 난다 — 활성화로
            // Awake 를 먼저 완주시킨 뒤에야 이 둘을 안전하게 부를 수 있다(두 번째
            // 매치부터는 Awake 가 이미 끝나 있어 이 순서가 없어도 되지만, 첫 매치와
            // 같은 경로를 타야 조건 분기가 안 생긴다).
            _metaCanvasGo.SetActive(false);
            liveMatchRoot.SetActive(true);
            liveMatchRunner.SetOpponent(OpponentFactoryFor(_save.career.practiceCupRound));
            // 라운드마다 다른 시드를 줘 매 경기가 완전히 같은 난수열을 반복하지 않게 한다
            // — 엔진/판정 규칙은 무수정, LiveMatchRunner.Rebuild 가 이미 공개돼 있는
            // Build() 재호출일 뿐이다.
            liveMatchRunner.Rebuild(bracketSeed + (int)_save.career.practiceCupRound + 1);
            liveMatchPanel.ResetForNewMatch();
        }

        /// §H 3.5b-로스터 — ROSTER.md 스케줄(TournamentBracket 이 이미 이름/id 로
        /// 들고 있는 것과 정확히 같은 라운드 순서)을 실제 Brain 팩토리로 매핑한다.
        /// 이름/id 매핑은 TournamentBracket 에 두고 엔진 팩토리 매핑은 여기 두는 이유 —
        /// TournamentBracket 은 Brain 어셈블리를 몰라도 되게 유지하고 싶어서다(이
        /// 컨트롤러는 이미 LiveMatchRunner 를 통해 Brain 과 얽혀 있다).
        static Func<MatchRulesConfig, OpponentDefinition> OpponentFactoryFor(TournamentRound round) => round switch
        {
            TournamentRound.R32 => HwangDochul.Create,
            TournamentRound.R16 => MoonJunbae.Create,
            TournamentRound.QuarterFinal => SeoDohyun.Create,
            TournamentRound.SemiFinal => ImJunchul.Create,
            TournamentRound.Final => Jindoyun.Create,
            TournamentRound.BronzeMatch => OhJaehoon.Create,
            _ => Jindoyun.Create,
        };

        void OnMatchEvent(MatchEvent e)
        {
            if (e is MatchEnded ended) ShowMatchResult(ended.Winner == FighterId.A);
        }

        void ShowMatchResult(bool playerWon)
        {
            _pendingPlayerWon = playerWon;
            _matchResultText.text = playerWon ? "승리!" : "패배...";
            _matchResultText.color = playerWon ? Highlight : Dim;
            _matchResultRoot.SetActive(true);
        }

        /// "확인" 버튼 — 경기 결과를 대회 진행 상태에 반영하고 세이브한 뒤 대진표로
        /// 돌아간다. META_DESIGN §M1 "경기 결과가 확정된 직후" 저장 시점이 바로 여기다.
        void ConfirmMatchResult(bool playerWon)
        {
            var round = _save.career.practiceCupRound;
            if (playerWon)
            {
                var next = TournamentBracket.NextRoundOnWin(round);
                if (next.HasValue) _save.career.practiceCupRound = next.Value;
                else _save.career.practiceCupResult = TournamentBracket.ResultOnWin(round);
            }
            else if (TournamentBracket.LossContinuesToBronze(round))
            {
                _save.career.practiceCupRound = TournamentRound.BronzeMatch;
            }
            else
            {
                _save.career.practiceCupResult = TournamentBracket.ResultOnLoss(round);
            }

            // §M4 direct reputation — 이번 세션은 "상대 id 기록"까지만(작업 지시). 실제
            // adaptation/knownByMe 누적은 §H 6/5 이월 — 필드는 채워지되 지금은 아무 데도
            // 안 읽혀 경기에 영향이 없다. round 는 위에서 이미 뮤테이션 전 값을 캡처해
            // 뒀다 — 방금 싸운 상대가 누구였는지는 항상 이 round 다.
            RecordOpponentFought(TournamentBracket.PlayerOpponentId(round), TournamentBracket.PlayerOpponentName(round));
            SaveNow();

            _matchResultRoot.SetActive(false);
            liveMatchRoot.SetActive(false);
            _metaCanvasGo.SetActive(true);
            ShowBracket();
        }

        void RecordOpponentFought(string id, string name)
        {
            var entry = _save.opponents.Find(o => o.id == id);
            if (entry == null)
            {
                entry = new OpponentSaveEntry { id = id, name = name };
                _save.opponents.Add(entry);
            }
        }

        // ── 화면 전환 ────────────────────────────────────────────────────────────

        void SetScreen(GameObject active)
        {
            _mapRoot.SetActive(active == _mapRoot);
            _trainingRoot.SetActive(active == _trainingRoot);
            _restRoot.SetActive(active == _restRoot);
            _tournamentListRoot.SetActive(active == _tournamentListRoot);
            _bracketRoot.SetActive(active == _bracketRoot);
            _trainingRunRoot.SetActive(active == _trainingRunRoot);
        }

        void ShowMap()
        {
            SetScreen(_mapRoot);
            _mapStatusText.text = StatusLine();
            // META_DESIGN §M1 저장 시점 — "지도로 돌아가기"마다.
            SaveNow();
        }

        void ShowTraining() => SetScreen(_trainingRoot);
        void ShowRest() => SetScreen(_restRoot);

        // ── 훈련(DESIGN_training §5-A) ─────────────────────────────────────────

        void ToggleDiscipline(string id)
        {
            if (_trainingSelectedIds.Contains(id))
                _trainingSelectedIds.Remove(id);
            else if (_trainingSelectedIds.Count < 4)
                _trainingSelectedIds.Add(id);
            RefreshTrainingSelectUI();
        }

        void RefreshTrainingSelectUI()
        {
            foreach (var kv in _trainingToggleButtons)
            {
                bool selected = _trainingSelectedIds.Contains(kv.Key);
                kv.Value.GetComponent<Image>().color = selected ? Highlight : ButtonBg;
            }
            int n = _trainingSelectedIds.Count;
            _trainingSelectCountText.text = $"선택: {n} / 3~4";
            _trainingStartButton.interactable = n >= 3 && n <= 4;
        }

        void StartTrainingRun()
        {
            if (_trainingSelectedIds.Count < 3 || _trainingSelectedIds.Count > 4) return;

            _trainingQueue = new List<string>(_trainingSelectedIds);
            _trainingQueueIndex = 0;
            _trainingResults.Clear();

            // Training GameObject 는 LiveMatch 와 같은 이유로 첫 훈련 전까지 비활성 —
            // 활성화가 그 Awake(TrainingStageView.EnsureBuilt 포함)를 이 시점에 동기로
            // 완주시킨다(SetActive(true) 안에서 Awake 가 즉시 돈다), Output 은 그 다음 줄부터 유효.
            trainingSession.gameObject.SetActive(true);
            _trainingStageImage.texture = trainingSession.Output;

            SetScreen(_trainingRunRoot);
            BeginTrainingDiscipline();
        }

        void BeginTrainingDiscipline()
        {
            _trainingResultText.gameObject.SetActive(false);
            _trainingResultButton.gameObject.SetActive(false);
            _trainingRepButton.gameObject.SetActive(true);

            var entry = trainingCatalog.Find(_trainingQueue[_trainingQueueIndex]);
            trainingSession.BeginSession(entry);
            _trainingDisciplineLabel.text = entry.displayName;
            RefreshTrainingRunHud();
        }

        void OnTrainingRepInput()
        {
            if (trainingSession == null || !trainingSession.Active) return;
            trainingSession.OnRepInput();
            RefreshTrainingRunHud();
        }

        void RefreshTrainingRunHud()
        {
            _trainingRepText.text = $"렙: {trainingSession.RepCount}";
            _trainingFatigueText.text = $"피로: {Mathf.RoundToInt(trainingSession.Fatigue)} / {Mathf.RoundToInt(trainingSession.FatigueLimit)}";
        }

        /// <see cref="TrainingSessionController.SessionEnded"/> — 한 종목이 방전으로
        /// 끝났다. 저장 훅 자리 — 값 연결은 §H 7 이월(DESIGN_training §5-B), 지금은
        /// 집계만 한다.
        void OnTrainingDisciplineEnded(TrainingSessionController.Result result)
        {
            _trainingResults.Add(result);
            RecordTrainingResult(result);
            RefreshTrainingRunHud();

            _trainingRepButton.gameObject.SetActive(false);
            bool more = _trainingQueueIndex + 1 < _trainingQueue.Count;
            if (more)
            {
                _trainingResultText.text = $"{_trainingDisciplineLabel.text}: {result.RepCount}렙";
                _trainingResultButton.GetComponentInChildren<TMP_Text>().text = "다음 종목";
            }
            else
            {
                int total = 0;
                foreach (var r in _trainingResults) total += r.RepCount;
                _trainingResultText.text = $"훈련 종료 — 총 {total}렙 ({_trainingResults.Count}종목)";
                _trainingResultButton.GetComponentInChildren<TMP_Text>().text = "지도로 돌아가기";
            }
            _trainingResultText.gameObject.SetActive(true);
            _trainingResultButton.gameObject.SetActive(true);
        }

        void OnTrainingResultButtonClicked()
        {
            _trainingQueueIndex++;
            if (_trainingQueueIndex < _trainingQueue.Count) BeginTrainingDiscipline();
            else ShowMap();
        }

        /// §H 7 이월(DESIGN_training §5-B) — 스탯/숙련 반영이 붙을 자리. SaveData 스키마는
        /// 무수정, 값 연결 없음 — 작업 지시 그대로 훅 자리만.
        void RecordTrainingResult(TrainingSessionController.Result result) { }

        void ShowTournamentList()
        {
            SetScreen(_tournamentListRoot);
            _tournamentListStatusText.text = StatusLine();
        }

        string StatusLine()
        {
            var c = _save.career;
            if (c.practiceCupResult != PracticeCupResult.None)
                return $"연습 대회 — 결과: {MedalLabel(c.practiceCupResult)}";
            return $"연습 대회 — 진행 중 ({TournamentBracket.RoundLabel(c.practiceCupRound)})";
        }

        static string MedalLabel(PracticeCupResult r) => r switch
        {
            PracticeCupResult.Gold => "금메달",
            PracticeCupResult.Silver => "은메달",
            PracticeCupResult.Bronze => "동메달",
            _ => "메달 없음",
        };

        void ShowBracket()
        {
            SetScreen(_bracketRoot);
            // DestroyImmediate, not Destroy — this rebuild can run twice in the same
            // frame in practice (뒤로가기 → 다시 들어가기가 한 프레임 안에 겹치는 자동화
            // 테스트에서 실측으로 확인됨). Destroy()는 프레임 끝까지 자식을 살려둬 그
            // 사이 두 번째 호출이 새 자식 3개를 겹쳐 쌓는다 — 즉시 파괴로 그 창을 없앤다.
            for (int i = _bracketContent.childCount - 1; i >= 0; i--)
                DestroyImmediate(_bracketContent.GetChild(i).gameObject);

            var result = _save.career.practiceCupResult;
            if (result != PracticeCupResult.None)
            {
                BuildBracketResultScreen(result);
                return;
            }

            var round = _save.career.practiceCupRound;
            if (round == TournamentRound.R32)
                BuildRound1Grid();
            else
                BuildLaterRoundView(round);
        }

        void BuildBracketResultScreen(PracticeCupResult result)
        {
            MakeText(_bracketContent, "연습 대회 종료", TextAlignmentOptions.Center, 26);
            var label = MakeText(_bracketContent, MedalLabel(result), TextAlignmentOptions.Center, 34);
            label.color = result == PracticeCupResult.Lost ? Dim : Highlight;
            MakeButton(_bracketContent, "지도로 돌아가기", ShowMap, 240, 48);
        }

        /// META_DESIGN §M3 — 32강 전체 그리드. 내 대진(0·1번 슬롯)만 "경기 시작" 버튼이
        /// 붙는다, 나머지 15경기는 이름만 나란히("vs")고 코인플립으로 이미 정해진 진출자를
        /// 캡션 한 줄로 보여준다(§M3 "대진표가 살아있어 보인다").
        void BuildRound1Grid()
        {
            MakeText(_bracketContent, "32강", TextAlignmentOptions.Center, 26);
            var grid = _bracket.Round1Grid();
            for (int i = 0; i < grid.Count; i += 2)
            {
                bool mine = i == 0;
                BuildBracketRow(_bracketContent, grid[i], grid[i + 1], mine, rowHeight: 32);
            }

            var others = _bracket.ResolveOtherFirstRoundWinners();
            var caption = MakeText(_bracketContent, "다른 32강 진출자: " + string.Join(", ", others),
                TextAlignmentOptions.Center, 13);
            caption.color = Dim;

            MakeButton(_bracketContent, "지도로 돌아가기", ShowMap, 240, 44);
        }

        /// [설계 판단] 16강부터는 다른 참가자들의 대진을 다시 그리지 않는다 — 그 이름들은
        /// 코스메틱 전용(§TournamentBracket 클래스 주석)이라 라운드마다 다시 계산해 보여줄
        /// 근거 있는 데이터가 없다. 내 대진 한 줄 + 안내 문구만으로 진행 상태를 보여준다.
        void BuildLaterRoundView(TournamentRound round)
        {
            MakeText(_bracketContent, TournamentBracket.RoundLabel(round), TextAlignmentOptions.Center, 26);
            BuildBracketRow(_bracketContent, _save.playerName, TournamentBracket.PlayerOpponentName(round), true);
            var caption = MakeText(_bracketContent, "다른 대진은 이미 진출자가 정해졌습니다.",
                TextAlignmentOptions.Center, 13);
            caption.color = Dim;
            MakeButton(_bracketContent, "지도로 돌아가기", ShowMap, 240, 44);
        }

        /// <paramref name="mine"/> 이면 이름 사이에 "경기 시작" 버튼(작업 지시: 상대
        /// 이름과 내 이름 사이 공간에 배치), 아니면 그냥 "vs".
        void BuildBracketRow(Transform parent, string left, string right, bool mine, float rowHeight = 40)
        {
            var row = MakeHorizontalGroup(parent, spacing: 12, rowHeight: rowHeight);

            var leftText = MakeText(row, left, TextAlignmentOptions.Right, mine ? 18 : 15, 200);
            if (mine) leftText.color = Highlight;

            if (mine) MakeButton(row, "경기 시작", StartMatch, 140, 34);
            else MakeText(row, "vs", TextAlignmentOptions.Center, 13, 40);

            var rightText = MakeText(row, right, TextAlignmentOptions.Left, mine ? 18 : 15, 200);
            if (mine) rightText.color = Highlight;
        }

        // ── 런타임 UI 뼈대 ───────────────────────────────────────────────────────

        void BuildCanvas()
        {
            _metaCanvasGo = new GameObject("MetaCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _metaCanvasGo.transform.SetParent(transform, false);
            var canvas = _metaCanvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50; // LiveMatchCanvas 는 100 — 매치 화면이 이 위에 온다.
            var scaler = _metaCanvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _mapRoot = BuildScreenRoot(_metaCanvasGo.transform, "MapRoot");
            BuildMapContent(_mapRoot.transform);

            _trainingRoot = BuildScreenRoot(_metaCanvasGo.transform, "TrainingRoot");
            BuildTrainingSelectContent(_trainingRoot.transform);

            _trainingRunRoot = BuildScreenRoot(_metaCanvasGo.transform, "TrainingRunRoot");
            BuildTrainingRunContent(_trainingRunRoot.transform);

            _restRoot = BuildScreenRoot(_metaCanvasGo.transform, "RestRoot");
            BuildTemplateContent(_restRoot.transform, "휴식");

            _tournamentListRoot = BuildScreenRoot(_metaCanvasGo.transform, "TournamentListRoot");
            BuildTournamentListContent(_tournamentListRoot.transform);

            _bracketRoot = BuildScreenRoot(_metaCanvasGo.transform, "BracketRoot");
            // 32강 그리드는 16줄 + 제목/캡션/버튼이 들어가야 한다 — 기본 14px 간격이면
            // 900px 안에 다 안 들어가 패널 배경 밖으로 흘러넘친다(실측 계산: 40px 행
            // 16개 + 기본 간격이면 총 필요 높이가 900 을 넘는다). 간격만 좁힌다 — 다른
            // 화면(지도/훈련/휴식/대회 목록)은 항목이 적어 기본 spacing(14)을 그대로 쓴다.
            _bracketContent = BuildCenteredPanel(_bracketRoot.transform, 640, 900, spacing: 6);

            // 매치 결과 오버레이 — LiveMatchCanvas(정렬순서 100) 보다 위에 떠야 하므로
            // 별도 캔버스(150)에 둔다. MetaCanvas 자식이면 LiveMatch 활성 중엔
            // _metaCanvasGo 자체가 SetActive(false) 라 같이 꺼져버린다.
            _overlayCanvasGo = new GameObject("MetaOverlayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _overlayCanvasGo.transform.SetParent(transform, false);
            var overlayCanvas = _overlayCanvasGo.GetComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 150;
            var overlayScaler = _overlayCanvasGo.GetComponent<CanvasScaler>();
            overlayScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            overlayScaler.referenceResolution = new Vector2(1920, 1080);

            _matchResultRoot = BuildScreenRoot(_overlayCanvasGo.transform, "MatchResultRoot");
            _matchResultRoot.GetComponent<Image>().color = OverlayBg;
            var resultPanel = BuildCenteredPanel(_matchResultRoot.transform, 420, 220);
            _matchResultText = MakeText(resultPanel, "", TextAlignmentOptions.Center, 30);
            MakeButton(resultPanel, "확인", () => ConfirmMatchResult(_pendingPlayerWon), 160, 48);
            _matchResultRoot.SetActive(false);
        }

        void BuildMapContent(Transform parent)
        {
            var panel = BuildCenteredPanel(parent, 480, 420);
            MakeText(panel, "지도", TextAlignmentOptions.Center, 30);
            _mapStatusText = MakeText(panel, "", TextAlignmentOptions.Center, 16);
            _mapStatusText.color = Dim;
            MakeButton(panel, "유도장", ShowTraining, 280, 56);
            MakeButton(panel, "집", ShowRest, 280, 56);
            MakeButton(panel, "경기장", ShowTournamentList, 280, 56);
        }

        void BuildTemplateContent(Transform parent, string title)
        {
            var panel = BuildCenteredPanel(parent, 480, 260);
            MakeText(panel, title, TextAlignmentOptions.Center, 30);
            MakeButton(panel, "지도로 돌아가기", ShowMap, 240, 48);
        }

        /// DESIGN_training §4/§5-A — 종목 목록 + 3~4개 선택.
        void BuildTrainingSelectContent(Transform parent)
        {
            var panel = BuildCenteredPanel(parent, 520, 740, spacing: 6);
            MakeText(panel, "훈련 종목 선택", TextAlignmentOptions.Center, 26);
            _trainingSelectCountText = MakeText(panel, "", TextAlignmentOptions.Center, 15);
            _trainingSelectCountText.color = Dim;

            if (trainingCatalog != null)
            {
                foreach (var entry in trainingCatalog.Disciplines)
                {
                    var id = entry.id;
                    var btn = MakeButton(panel, entry.displayName, () => ToggleDiscipline(id), 460, 30);
                    _trainingToggleButtons[id] = btn;
                }
            }
            else
            {
                var warn = MakeText(panel, "훈련 카탈로그 미배선", TextAlignmentOptions.Center, 14);
                warn.color = Dim;
            }

            _trainingStartButton = MakeButton(panel, "훈련 시작", StartTrainingRun, 280, 48);
            MakeButton(panel, "지도로 돌아가기", ShowMap, 240, 44);
            RefreshTrainingSelectUI();
        }

        /// DESIGN_training §2/§3 — 능동 렙 세션 화면. 3D 렌더는
        /// <see cref="TrainingSessionController"/>/<see cref="TrainingStageView"/> 가
        /// 만드는 RenderTexture 를 그대로 표시만 한다(LiveMatchPanel 의 ArenaStageView
        /// RawImage 표시와 같은 패턴).
        void BuildTrainingRunContent(Transform parent)
        {
            var panel = BuildCenteredPanel(parent, 520, 560, spacing: 10);
            _trainingDisciplineLabel = MakeText(panel, "", TextAlignmentOptions.Center, 22);

            var imgGo = new GameObject("StageImage", typeof(RectTransform), typeof(RawImage), typeof(LayoutElement));
            imgGo.transform.SetParent(panel, false);
            var le = imgGo.GetComponent<LayoutElement>();
            le.preferredWidth = 480; le.preferredHeight = 270; le.minWidth = 480; le.minHeight = 270;
            _trainingStageImage = imgGo.GetComponent<RawImage>();

            _trainingRepText = MakeText(panel, "", TextAlignmentOptions.Center, 18);
            _trainingFatigueText = MakeText(panel, "", TextAlignmentOptions.Center, 15);
            _trainingFatigueText.color = Dim;

            _trainingRepButton = MakeButton(panel, "훈련! (Space/Enter)", OnTrainingRepInput, 320, 56);

            _trainingResultText = MakeText(panel, "", TextAlignmentOptions.Center, 18);
            _trainingResultText.color = Highlight;
            _trainingResultText.gameObject.SetActive(false);

            _trainingResultButton = MakeButton(panel, "", OnTrainingResultButtonClicked, 280, 48);
            _trainingResultButton.gameObject.SetActive(false);
        }

        void BuildTournamentListContent(Transform parent)
        {
            var panel = BuildCenteredPanel(parent, 480, 320);
            MakeText(panel, "대회", TextAlignmentOptions.Center, 30);
            MakeButton(panel, "연습 대회", ShowBracket, 320, 64);
            _tournamentListStatusText = MakeText(panel, "", TextAlignmentOptions.Center, 14);
            _tournamentListStatusText.color = Dim;
            MakeButton(panel, "지도로 돌아가기", ShowMap, 240, 48);
        }

        GameObject BuildScreenRoot(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = ScreenBg;
            go.SetActive(false);
            return go;
        }

        Transform BuildCenteredPanel(Transform parent, float width, float height, float spacing = 14)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, height);
            go.GetComponent<Image>().color = PanelBg;
            var lg = go.GetComponent<VerticalLayoutGroup>();
            lg.spacing = spacing;
            lg.childAlignment = TextAnchor.MiddleCenter;
            lg.childForceExpandWidth = false;
            lg.childForceExpandHeight = false;
            lg.padding = new RectOffset(24, 24, 24, 24);
            return go.transform;
        }

        Transform MakeHorizontalGroup(Transform parent, float spacing = 8, float rowHeight = 40)
        {
            var go = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var lg = go.GetComponent<HorizontalLayoutGroup>();
            lg.spacing = spacing;
            lg.childAlignment = TextAnchor.MiddleCenter;
            lg.childForceExpandWidth = false;
            lg.childForceExpandHeight = false;
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = rowHeight;
            le.minHeight = rowHeight;
            le.flexibleWidth = 1;
            return go.transform;
        }

        Button MakeButton(Transform parent, string label, System.Action onClick, float w = 280, float h = 56)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = w;
            le.preferredHeight = h;
            le.minWidth = w;
            le.minHeight = h;
            go.GetComponent<Image>().color = ButtonBg;

            var btn = go.GetComponent<Button>();
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            MakeText(go.transform, label, TextAlignmentOptions.Center, Mathf.Min(20, h * 0.4f), 0, stretch: true);
            return btn;
        }

        TMP_Text MakeText(Transform parent, string text, TextAlignmentOptions align, float fontSize,
                          float preferredWidth = 400, bool stretch = false)
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
                var le = go.AddComponent<LayoutElement>();
                le.preferredWidth = preferredWidth;
                le.preferredHeight = fontSize + 10;
                le.minHeight = fontSize + 10;
                le.flexibleWidth = 0;
            }

            var t = go.GetComponent<TextMeshProUGUI>();
            if (koreanFont != null) t.font = koreanFont;
            t.fontSize = fontSize;
            t.enableAutoSizing = true;
            t.fontSizeMax = fontSize;
            t.fontSizeMin = Mathf.Max(8, fontSize * 0.4f);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.alignment = align;
            t.text = text;
            t.raycastTarget = false;
            return t;
        }
    }
}
