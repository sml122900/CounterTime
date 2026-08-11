STATUS

Update this file every time a session resets. This is the ONLY copy — the root CLAUDE.md pulls it in with `@md/STATUS.md`, so it loads automatically at session start. Do not create a second copy anywhere. Check "Current State" and "Next Up" before starting work.

Current State (last updated: 2026-08-12)
**용어 대응표**: "쿠즈시" = 구 명칭, 2026-08-11 오후 전체 개명 완료 → 화면 표시 "흔들기" / 코드 식별자 `Setup*`(예: `HandTarget.Setup`, `SetupGateEnabled`, `SetupPrecedingCommitSource`). 이 파일의 이전 세션 로그·docs/overnight-*·decisions/·daily/ 는 기록이라 "쿠즈시"로 그대로 남아있다 — 그 시점엔 그게 맞는 이름이었다. 지금 코드/MATCH_RULES.md 를 볼 때는 전부 흔들기/Setup 이다.
Unity 6.3.21f1 LTS editor installed
CounterTime project created (Universal 3D template, location C:\project_2026\CounterTime)
Unity AI Assistant package enabled; Claude Code ↔ Unity MCP connected (unity-mcp, 7 tools enabled). Console verified clean — 0 errors / 0 warnings as of 2026-08-03
Git initialized, remote is https://github.com/sml122900/CounterTime.git (branch main). Git LFS installed (3.7.1) and tracking fbx/obj/blend/png/jpg/tga/psd/wav/mp3/ogg/mp4/mov. .gitignore and .gitattributes rewritten for Unity (.meta kept via explicit negation rule, text assets pinned to eol=lf)
KNOWN + ACCEPTED: the first commit (204aefc) landed before the LFS config and is already on the remote, so Assets/TutorialInfo/Icons/URP.png stays in history as a 24KB raw blob. History rewrite was considered and rejected — it would need a force push to reclaim one 24KB tutorial icon. Everything committed from now on goes through LFS. See docs/troubleshooting/lfs-after-first-commit.md
Folder structure (Assets/_Project/…) and the two asmdefs created. CounterTime.Core has noEngineReferences: true and zero references; CounterTime.Game references CounterTime.Core. No .cs files written yet
Assets/_Project/Scenario/ is the single home for scenario files. Holds counter-time-scenario-A-vs-B.json and counter-time-scenario-board.html (both moved out of the repo root). Rule recorded in CLAUDE.md
Assets/_Project/Scenes/Main.unity is the stage: Floor (Plane, M_Stage_Floor blue URP/Lit), Directional Light, fixed side camera at (0, 1.6, -6) FOV 45 looking down +Z. Main.unity is Build Settings scene 0; SampleScene kept but disabled. The hand-authored scene YAML from earlier this session imported clean — Unity verified it, no errors
Fighters are Mixamo characters, not capsules: Fighter_A = Y Bot at x=-1 facing +X, Fighter_B = Ch30_nonPBR at x=+1 facing -X. Both sit at y=0 (character pivot is at the feet, unlike the capsule pivot which was at the centre). Both models are set to Animation Type = Humanoid with their own avatar generated from the model
Assets/_Project/Art/Animations/Fight Idle.fbx is also Humanoid; its clip was renamed from "mixamo.com" to "Fight Idle" and has Loop Time ON (Loop Pose left OFF). One clip drives both characters via humanoid retargeting
AC_Fighter_A.controller and AC_Fighter_B.controller (Art/Animations/) each hold a single state "Fight Idle" as the default state. Assigned to the two Animators, applyRootMotion off
M_Fighter_Grey.mat is now unused — it was the capsule material. Left in place, not deleted

시나리오 러너 v1 (탐색 모드) — 동작 확인 완료
CounterTime.Core/Scenario/: ScenarioData + ScenarioNode + ScenarioEdge (JSON 키와 필드명 1:1), NodeKind (border 색 → Narration/ChoiceA/ActionB/Judgement), ScenarioGraph (노드 사전 + from→to 인접, GetChildren), ScenarioRunner (n1 시작, Current, MoveTo, IsTerminal, NodeEntered 이벤트). UnityEngine 참조 0 — Core 는 여전히 순수 C#
CounterTime.Game/Scenario/: ScenarioLoader (TextAsset → JsonUtility → Core), ScenarioUIController (TMP 로 title/content 출력, 자식마다 버튼 생성 — 라벨은 자식 title, 색은 자식 border, 종단이면 "경기 종료 — 처음부터")
CounterTime.Game.asmdef 에 UnityEngine.UI / Unity.TextMeshPro 참조 추가
Main.unity 에 ScenarioCanvas (하단 1/3: TextPanel + 그 아래 ChoiceRow), EventSystem, ScenarioSystem(Loader+UIController) 추가. 선택 버튼은 Assets/_Project/Prefabs/ChoiceButton.prefab
JSON 실측: 노드 43, 엣지 52. border 분류가 데이터와 정확히 일치 (검정 4 / 파랑 14 / 빨강 11 / 초록 14). n1 에서 42개 노드 도달 가능, 유일한 미도달 노드가 n0 — 범례 제외가 특별 처리 없이 성립한다. 종단 노드는 n14/n18/n20/n26/n28/n34/n36/n41
한글 폰트: TMP Essential Resources 임포트 + NotoSansKR-VF.ttf (Windows 동봉, SIL OFL) 를 Assets/_Project/Art/Fonts/ 로 복사해 동적 TMP 폰트 에셋 생성. 기본 LiberationSans 는 한글 글리프가 없어서 전부 두부로 나온다. *.ttf/*.otf 를 .gitattributes LFS 규칙에 추가
Play 모드에서 실제 확인: 시작 노드 렌더링, 버튼 클릭 이동, 버튼 색 = 노드 border, 종단 노드의 재시작 버튼 → n1 복귀. 한글 글리프 미해결 0개
레이아웃 시각 검증 통과 (2026-08-05). 8개 노드 캡처로 확인 — 본문 최장 5개(n30/n1/n5/n7/n34), 선택지 최다 3개(n7/n30/n2), 종단 3개(n34/n36/n41). 한글 깨짐 0, 텍스트 넘침 0, 버튼 라벨 전부 1줄, 오토사이즈는 24pt 유지(하한 14pt에 안 닿음). "경기 종료 — 처음부터" 는 GraphicRaycaster 레이캐스트 + pointerDown/Up/Click 실제 포인터 이벤트로 눌러서 n1 복귀 확인
현재 하단 1/3 분할 (ScenarioPanel 기준): ChoiceRow 0.03..0.31, TextPanel 0.34..0.99, Title 0.72..1.0, Content 0..0.72. 1918x879 에서 타이틀 41/50, 본문 최대 115/128 — 여유 있음

노드↔모션 연결 — 동작 확인 완료
NodeMotionMap (ScriptableObject, Game): {nodeId, stateA, stateB} 리스트. 항목이 없거나 칸이 비면 둘 다 "Fight Idle". 에셋은 Assets/_Project/ScriptableObjects/NodeMotionMap.asset, 초기 매핑 36개 (n0 제외 42개 중 n1/n2/n27/n29/n39/n40 은 기본값이라 비워둠)
ScenarioMotionBinder (MonoBehaviour, Game): ScenarioRunner.NodeEntered 구독 → 맵 조회 → 두 Animator 에 CrossFade 0.25초. UI 쪽 Start 가 먼저 돌아 첫 이벤트를 놓치는 경우를 대비해 Start 에서 Current 로 한 번 맞춘다. 없는 상태 이름은 경고 후 무시
Core 는 손대지 않았다 — 모션 개념은 전부 Game 쪽. Scripts/Core 에 UnityEngine 참조 0 유지
AC_Fighter_A/B 각각 14개 상태 (Mixamo 10 + AI 생성 4). 원샷(Reaction / Standing Melee Punch / Stumble Backwards / Shoved Reaction With Spin / Taunt / Jumping / ai_seoinage_entry / ai_osotogari_entry / ai_ukemi_back)은 exitTime 0.9 + 고정 0.25초로 Fight Idle 자동 복귀 — 컨트롤러당 트랜지션 9개, 전부 같은 파라미터. 유지(Dying / Defeated / Victory Idle / ai_ground_pin)는 나가는 트랜지션 없음
클립 Loop: Fight Idle · Victory Idle · ai_ground_pin 만 ON. 나머지는 OFF (원샷은 복귀하고, Dying/Defeated 는 마지막 프레임을 유지)
Jumping.fbx 클립 이름이 mixamo.com 이라 "Jumping" 으로 바꿨다 — 상태 이름으로 써야 해서
화면상 A = Y Bot = 청록색(왼쪽, x=-1), B = Ch30 = 흰색(오른쪽, x=+1)
Y Bot renders white and Ch30 renders teal because neither FBX brought textures in; fine for greyboxing
Docs scaffolding created: docs/daily/, docs/decisions/, docs/troubleshooting/, docs/par-materials.md
2026-08-05 세션 문서: docs/daily/2026-08-05.md · decisions/scenario-runner-core-game-boundary.md · decisions/korean-font-tmp.md · troubleshooting/mcp-play-mode-verification.md · troubleshooting/tmp-text-overflow.md · par-materials.md 항목 4~8
2026-08-07 세션 문서: docs/daily/2026-08-07.md · decisions/ai-generated-motion-clips.md · troubleshooting/unity-asset-commit-boundary.md · par-materials.md 항목 9~10

Match engine core — steps 1–6 done (2026-08-09, branch feat/match-engine, not merged)
MATCH_RULES.md / CORE_DESIGN_MatchEngine.md live in md/ (untracked before today, now committed). The 2026-08-09 patch set is applied — exclusive referee categories, CounterReady, counter cost ownership, either-hand lock cap, exchange duration, time expiry, grip HUD as a requirement
Assets/_Project/Scripts/Core/ (NOT Assets/_Project/Core/ — the design doc said the latter, but that path is outside the asmdef and would compile into Assembly-CSharp with engine references; the doc was corrected)
Files: Enums · MatchRulesConfig · TechniqueSpec · TurnChoice · IRandomSource · JudgementCalculator · StaminaSystem · GripSystem · RefereeSystem · ScoreBoard · MatchClock · MatchEvent · MatchEngine (+ ExchangeScript)
Tests: Assets/_Project/Scripts/Core/Tests/ with CounterTime.Core.Tests.asmdef. 106 EditMode tests, all passing. Run them in Unity via Window > General > Test Runner > EditMode > Run All
Core stayed pure — zero UnityEngine references. That is load-bearing, not cosmetic: it lets the whole suite run outside Unity, which is how these 106 were actually executed (see below)

RULES_PATCH_v3.1 적용 완료 (2026-08-09, 같은 브랜치). 문서 헤더 v3 → v3.1
v3.1 은 원본 v3 기준으로 쓰여 있어서 PATCH 1·3·7 의 OLD 앵커가 안 맞았다 — 그 자리는 08-09 패치셋이 이미 고쳐놓은 상태였다. 문자 그대로 적용하는 대신 v3.1 문구로 교체했다 (v3.1 이 나중 권위)
CounterReady 부호 정정 — v3.1 §4.2 가 "공격자의 ReadLevel 이 한 칸 내려간다" 로 확정했다. 이건 어제 사람이 고른 것과 같은 방향이라 엔진 동작은 안 바뀌었다. 바뀐 것은 이름과 사다리 정의: ApplyCounterReady(올림) 삭제, StepDownRead(내림) 로 대체. 이벤트도 CounterReadyRaised → CounterReadyApplied 로 고쳤다 — 실제로 움직이는 건 공격자의 읽기이고 방향은 아래쪽이라 이름이 거짓말이었다
사다리가 일부러 불균등하다 — FullRead 는 Read 까지만 떨어진다(−1). 즉 완전간파한 공격은 카운터 준비만으로는 절대 되치기당하지 않는다. 읽기는 읽기로만 이긴다. CounterReady_CannotCounterFullRead 가 이 원칙을 고정한다
v3.1 이 새로 넣은 것 3개는 코드에도 반영했다: (1) §2 카운터 준비 게이트 — 선언한 쪽은 그 턴에 기술이 해결되지 않는다. 스크립트가 attacker 로 지목해도 엔진이 막는다 (2) §4.2 카운터 준비는 수비 자세가 아니다 — 코어를 수비적으로 골랐어도 −2 가 안 붙는다 (3) §4 되치기 정산 — PayCounter 로 분리(배수 없음)
시그니처도 v3.1 PATCH 12 에 맞췄다: HasLockedHand → AnyHandLocked, AdvanceExchange 는 void
RULES_PATCH_v3.1.md 파일은 디스크에 없었다 (내용을 대화로 받았다). "적용 후 삭제" 는 지울 대상이 없어서 실행할 게 없었다

문서 스탬프 = 본문 해시 (v3.1 커밋해시 방식 폐기)
`<!-- v3.2 | body:e11de04 -->` — 1행을 뺀 본문의 git hash-object (CR 제거). 스탬프를 쓰는 행위가 스탬프가 재는 대상을 안 바꾸므로 한 번에 수렴한다. 커밋해시 방식은 자기참조라 수렴이 불가능했고, 커밋 안 한 편집을 못 잡아서 존재 이유였던 실패를 정확히 못 잡았다
tools/stamp.sh (rewrite / --check) + tools/githooks/pre-commit 이 자동 유지한다. 손으로 고치지 마라. 클론마다 한 번: `git config core.hooksPath tools/githooks`
용도: 채팅에 붙여넣은 사본이 진짜인지 검증. 사본의 스탬프가 자기 본문과 안 맞으면 그 사본은 손으로 고친 것이고 원본이 아니다 — 그쪽에 맞추지 말고 불일치를 보고할 것
컨벤션은 CLAUDE.md 에 적었다. 세션 메모리에만 있는 규칙은 규칙이 아니다 (diff·다른 도구·다음 세션에서 안 보인다)
.gitattributes 에 *.sh / tools/githooks/* 를 eol=lf 로 고정했다. 안 하면 새 Windows 클론에서 CRLF 로 받아 셔뱅이 깨지고, 훅을 커밋한 의미가 없어진다

rules v3.2 — 수비 자세 정의 (판정식의 마지막 미정의 항)
수비 자세 ⇔ 상대 Core == Defensive. 다리·손은 판정에 안 들어간다. 이분법이고, "얼마나 웅크렸나" 가 아니라 "던질 수 있는 자세를 내줬나" 를 묻는다
극단적 방어도 −2 로 같다. 웅크린 깊이는 §6.1 소극 누적과 §5.2 −1/턴 드레인으로 이미 두 번 청구되고 있어서 여기서 세 번째로 청구하지 않는다
예외: 카운터 준비를 선언한 쪽은 코어가 수비적이어도 수비 자세가 아니다. 이게 없으면 −2 가 붙어 −4 → 방어자에게 공짜 유효가 난다. 테스트가 틀린 답(−4)까지 같이 검증한다
JudgementCalculator.IsDefensivePosture 가 유일한 파생 지점이다. MatchEngine 이 인라인으로 다시 유도하던 걸 걷어냈다 — v3.1 적용 중 나온 버그가 정확히 그 인라인 유도였다. grep 으로 다른 파생 없음 확인 (RefereeSystem/TurnChoice 의 Defensive 는 §6.1 rung 과 §2 극단적 방어 정의로 서로 다른 규칙)

연출 스펙 — 대파열 (2026-08-09 BurstFeelTest 로 실측)
BurstFeelTest.cs 와 Assets/_Project/Spikes/LowResRender/ 는 삭제하지 않고 **커밋했다** — §7 이 HUD 를 하드 요구로 못 박은 근거가 이 스파이크의 실측(320×180 에서 손 5.8px)이라, 측정 장치가 한 대의 디스크에만 있으면 규칙을 리포 밖에 적어둔 것과 같아진다. 2순위 부원 촬영도 이 씬이 파이프라인이다. Main.unity 는 7.6 에서 참조를 끊었다
**스파이크 씬의 nearMissSeconds 는 폐기된 초안값 0.08 이다. 확정 규격은 0.5** — 스파이크는 측정 장치지 사양서가 아니다
되치기 대파열 = 예고 0.8초 @0.25x → 정지 0.25초 → 흑백반전 3×0.05초. 근접 실패 = 0.5x 로 0.5초. 되치기 발화 임계는 **−3 유지 (option ⓐ)**
이걸로 §4.2 / §8 의 "대파열 연출 무게를 먼저 확인하라" 전제가 충족됐다. 확인 결과가 "ⓐ 그대로" 였으므로 §8 의 DEFERRED 항목(ⓐ vs ⓒ)은 42노드 감사까지 그대로 열어둔다 — 숫자는 안 건드린다
구조 주의 — 반전은 정지 "다음" 이 아니라 정지 "안" 에서 돈다. timeScale 0 구간이 총 0.25초이고 그중 앞 0.15초가 흑백 교대, 남은 0.10초가 정지 유지다. 순차로 읽으면 총 정지가 0.40초가 되어 60% 길어진다
근접 실패 지속시간 = **0.5초 확정**. Main.unity 에 남아 있던 nearMissSeconds: 2 는 편집 모드 잔재다 — Play 중 인스펙터로 맞춘 값은 저장되지 않으므로 씬에는 실측 전 값이 남는다. 스크립트 기본값 0.08 도 같은 이유로 무의미하다. 실측값의 출처는 Play 중 화면이지 씬 파일이 아니다

발견한 불일치 (양쪽 다 테스트로 고정해둠)
m15 노드는 A 체력 90 으로 기록돼 있는데 엔진은 93 이 나온다. 그 교환에서 지도가 나오고 → 그쳐 → §5.3 이 +3 을 준다. 문서 우선순위상 MATCH_RULES 가 보드 JSON 을 이긴다 — 93 이 맞고 m15 의 90 은 회복 적용 전 스냅샷이다
극단적 방어 4턴 = 지도는 임계가 정확히 12 일 때만 성립한다. §6.2 의 ±1 지터가 +1 이면 5턴이 된다. 테스트는 지터 0 으로 고정한다
CORE_DESIGN 에 남아 있던 모순 2개도 고쳤다 — EffectiveGrip 주석이 아직 "lead hand" 였고, 대상 폴더가 asmdef 밖이었다

테스트 실행 경로 (중요) — 어셈블리가 둘이고 하니스는 한쪽만 돈다
Unity MCP 로는 EditMode 테스트를 못 돌린다. TestRunnerApi.Execute 가 "User interactions are not supported for MCP tool calls" 로 막힌다
대신 Core 가 순수 C# 이라는 점을 이용해 dotnet 콘솔 하니스를 만들어 커밋된 NUnit 테스트 파일을 그대로 컴파일·실행한다 (nunit.framework.dll 은 Library/PackageCache/com.unity.ext.nunit@.../net40/unity-custom/ 에 있다).
**2026-08-11 부터 하니스가 리포에 있다** — `tools/dotnet-harness/` (Harness.csproj/Program.cs/UnityEngineShim.cs/README.md/fingerprint-baseline.txt). 이전 방침("하니스는 리포에 안 들어간다 — 세션마다 다시 만든다")은 뒤집혔다: `dotnet run` (그 폴더에서) 으로 재구축 없이 재현된다. `*.csproj` 는 리포 전체에 gitignore 돼 있지만(Unity 가 재생성하는 IDE 파일용 규칙) `.gitignore` 에 `!/tools/dotnet-harness/*.csproj` 부정 규칙을 달아 이 파일만 되살렸다 — README 참조
`dotnet run -- --check` 로 지문을 fingerprint-baseline.txt 와 자동 비교한다(불일치면 종료코드 1 + 라인별 diff). 지문이 의도적으로 바뀐 거면 그 파일을 손으로 재생성하고 커밋 메시지에 왜인지 적을 것 — 조용히 덮어쓰지 말 것
CounterTime.Core.Tests + CounterTime.Brain.Tests(전체) + CounterTime.Game.Tests(부분: ScenarioTurnChoicesTests/ScriptedReadResolverTests/ResultNarrationTableTests/LiveAssemblerTests) → 하니스로 돈다. MatchRunnerHandActionTests(3개) → **안 돈다.** new GameObject()/AddComponent<T>()/DestroyImmediate 를 써서 Unity 의 진짜 오브젝트 모델이 필요하다
그래서 하니스 출력 마지막 줄이 스스로 범위를 밝히게 해뒀다: "COVERAGE: CounterTime.Core.Tests + CounterTime.Brain.Tests (full) + CounterTime.Game.Tests PARTIAL ... MatchRunnerHandActionTests needs the real Unity Test Runner"
**"하니스 N개 초록" 은 MatchRunnerHandActionTests 가 검증됐다는 뜻이 아니다.** 그 3개만 사람이 직접 확인해야 한다: Window > General > Test Runner > EditMode 탭 > Run All

Opponent brain — stages 1–3 done (2026-08-10, branch feat/match-engine, not merged)
164 EditMode tests green: CounterTime.Core.Tests 117 / CounterTime.Brain.Tests 40 / CounterTime.Game.Tests 7. Run them in Unity: Window > General > Test Runner > EditMode > Run All
Document stamps (as of 2026-08-11 아침, verified via tools/stamp.sh --check): md/MATCH_RULES.md **v3.7 [초안 — 쿠즈시 §4.4, 사람 검토 전]** body:1d15c97 (§2 표에 뜯기/잡기 유지 행 + 동시 공격 우선순위 노트 추가, 아침 결정 반영) · md/OPPONENT_BRAIN.md v1.0 body:5d5a8c7 (무수정) · md/CORE_DESIGN_MatchEngine.md v3.2 body:a24d23c (무수정)
New asmdef CounterTime.Brain at Assets/_Project/Scripts/Brain/ — pure C#, references CounterTime.Core only, noEngineReferences true. Verified at the assembly level, not by grep: its referenced assemblies are exactly [netstandard, CounterTime.Core]. The brain has to run in the dotnet harness, which is why it is not in Game
Seams (interfaces in Brain, scripted implementations in Game):
  ICommitSource — where one fighter's TurnChoice comes from. bool TryNext(out who, out choice); false means "declared nothing this turn", a distinction §6.1 passivity rides on
  IReadResolver — the ONLY place ReadLevel is produced. Returns the BASE read; Core owns the §4.2 counter-ready step-down and a resolver that pre-applies it causes double reduction
  IExchangeAssembler — this turn's ExchangeScript list. One node can expand to several exchanges (m14 = 4)
  IHabitContext — 4 members, habit conditions only. Frozen surface
  IBrainContext — 6 members, §4 ladder rungs 1 (hard gates) and 3 (fallback weighting) only. Split from IHabitContext on purpose so shido COUNT and the clock never reach habit conditions; enforced by the condition delegate's parameter type and pinned by a reflection test
Scripted mode (대조 모드) was never touched. Every stage was verified with a 52-line fingerprint of the real MatchRunner walking MatchRunner.Mainline — all MatchEvents plus full engine state after each node — diffed against the previous stage. It came back IDENTICAL every single time, including across the v3.4 Core rule change
진도윤 has 5 habits (H1 업어치기 on grip edge / H2 안뒤축감아치기 after a shido / H3 극단적 방어 reserve / H4 오른소매 노림 / H4b 왼소매 노림 reserve) and the read ladder is 6 rows. Both are specified in OPPONENT_BRAIN §2–§4, not in code comments alone
Ladder order is narrowest-condition-first (H2 → H3 → H1 → H4 → H4b). With H1 leading, H1 fired 7 times while H2 and H3 fired zero — an exclusive ladder lets every rung starve the ones below it
Two rules were added because measurement broke a premise, both recorded with their evidence: OPPONENT_BRAIN §4 "the fallback never commits a big throw" (so every big throw B makes comes from a habit and is therefore readable), and MATCH_RULES §4.3 v3.4 "a score calls matte" (recovery was tied to matte while nothing produced matte, so stamina bottomed out at ~30 exchanges against the 60–80 §2 implies)
The dotnet harness now also compiles the real MatchRunner via a ~60-line UnityEngine shim (MonoBehaviour/TextAsset/Debug/Mathf/JsonUtility), so the Game path is exercised outside Unity. MatchRunnerHandActionTests (3) still needs the Unity Test Runner
Do NOT use TestContext.WriteLine in tests meant to run in the harness — the reflection runner sets up no NUnit execution context and it throws NullReferenceException. Console.WriteLine works in both

Step 4 (4b narration infra) + step 5 (osaekomi §9) landed overnight (2026-08-10, branch feat/match-engine, not merged). 164 → 178 tests (126 Core / 40 Brain / 12 Game — 3 of the 12, MatchRunnerHandActionTests, still need the Unity Test Runner and were not run by the harness). See docs/overnight-2026-08-10.md for full detail; summary below
Step 4: JudgementScored was NOT extended with a TechniqueId field — that was tried first (plan 1, additive constructor overload, old constructor untouched) and it made the 52-line fingerprint diff non-identical (the new field shows up on every dump line even though nothing scored differently), so it was reverted per the run's own verification protocol. Landed instead: MatchRunner (Game only, Core untouched) pairs JudgementScored with its ExchangeScript.Technique.Id via a new JudgementNarrated event, consumed only by a new play-mode-only MonoBehaviour (ResultNarrationPresenter) that scripted-mode scenes/tests never attach. ResultNarrationTable is the (Judgement, TechniqueId, IsCounter) → text dictionary from OPPONENT_BRAIN §6, empty until docs/step4-narration-worksheet.md is filled in and its entries wired up by hand
Step 5: MatchEngine.ResolveExchange's §9.1 TODO is now real — waza-ari always enters osaekomi, yuko enters only if the scorer's effective grip ≥ +1 and the thrown fighter's stamina < 50, everything else still lands on matte. §9.2 adv/escapesNeeded frozen at entry. §9.3 ResolveOsaekomiExchange resolves one 5s escape tick (score awarded before escape, matched pairs continue the hold and cost the defender 3 stamina, crossed pairs credit an escape). §9.4 escape resets grips to Free with no matte recovery
UNRESOLVED judgment call from step 5, not a literal reading of MATCH_RULES §9 (flagged in code + overnight doc, needs a human decision): §9's "attacker" is read as the fighter who scored (JudgementScored.ScoredBy), not the original technique declarer — the scorer is who ends up on top to hold. This only matters for a countered score. A countered waza-ari still enters osaekomi (§9.1 gives it no "never" option) using the scorer's grip for the adv term; a countered yuko was deliberately left NEVER entering osaekomi rather than guessing which fighter's grip the gate means. Never exercised by the 42-node scenario or by tonight's 3 smoke profiles (all show counters=0), so it is not currently reachable, but will need a real answer once the habit brain produces more counters
Match length moved after step 5, not tuned: realistic-A smoke profile (seed 1234, jitter 0) was 28 exchanges / 156s remaining before step 5 (matches the "21–34 exchanges, 135–177s" range below) and is now 26 exchanges / 162s remaining after — fewer exchanges finish the match sooner while more clock survives, because osaekomi (real time) replaces some of what used to be matte (clock stopped) mid-match. Numbers only, not touched. See docs/overnight-2026-08-10.md §3.2 for the repeating/varying profiles too
~~KNOWN GAP left for stage 6: ResolveExchange never checks Osaekomi.Active~~ — **closed 2026-08-11** by a guard (see below). What stage 6 still needs to decide is narrower now: not "does the engine notice," but "what should a standing exchange arriving on an open hold even mean" — see the new judgment-call entry in docs/overnight-2026-08-10.md §"판단 대기" #4.

§9 guard + full green recovery (2026-08-11, branch feat/match-engine, commits a165872/a036e75, not merged). Two follow-up requests in the same session, both closed
`MatchEngine.ResolveExchange` now throws `InvalidOperationException` if called while `MatchPhase.Osaekomi` is active (the KNOWN GAP above, made loud instead of silent) — `ResolveOsaekomiExchange` is the only valid entry point once a hold is open. §9.1 also got its interpretation nailed down: MATCH_RULES v3.4 → v3.5, "attacker" in §9.1/§9.2 means the judgement's scorer, and a countered score uses the exact same gate as an uncountered one (previously a countered yuko was hard-coded to never enter osaekomi — that carve-out is gone)
Adding the guard broke 9 tests that were silently relying on the exact gap it closes. All 9 fixed without changing what any of them verify — not by loosening the guard: `OsaekomiTests`' shared `EnterViaYuko()` helper had a real off-by-one (`DrainB(14)` should have been `13` — grip was already fixed at +1 the whole drain loop, so the 14th call was itself already scoring and silently double-entering osaekomi); `PlayModeSmokeTests`' headless loop never called `ResolveOsaekomiExchange` at all, so a fixed drain policy (always 어긋남) was added to actually close holds as they open; `MatteAfterScoreTests.MatchEndingScore_DoesNotCallMatte` couldn't be saved as originally written (waza-ari has no §9.1 gate to dodge, so "make the first score skip osaekomi" is not constructible) — narrowed to a single-exchange direct ippon that still pins the exact §4.3 claim, with the lost combining-path coverage patched onto `WazaAriEntryPlusTenSecondHold_CombinesToAwaseteIppon` instead (2 new assertions, existing ones untouched)
178 → 180 tests (2 new: the guard's own test, and a §9.1 v3.5 countered-yuko-enters-osaekomi test). 177/177 harness-reachable, 3 Unity-only (unchanged)
The overnight session's "byte-identical" fingerprint claims (commits 182f0e5/979a4f2/478d46c/a8e89af) were never backed by a committed artifact — checked git history, no such file was ever added. Independently re-verified 2026-08-11 by checking out all 5 points (parent of 182f0e5 through a8e89af) into an isolated worktree and re-running a freshly-built harness at each: byte-identical throughout, confirming the overnight claims hold up under a from-scratch redo, not just self-comparison within one session
Full detail, including the exact 현실적 A osaekomi-entry-shortfall breakdown and the verbatim smoke-profile A commit policies: docs/overnight-2026-08-10.md §0/§5/§6
**Smoke A is a scarecrow, not a fighter — do not use the 3-profile numbers as balance data.** `PlayModeSmokeTests.RunMatch` assigns the attacker/technique per turn from whether B's habit brain fired one, full stop — A's declared Core/Legs (Attack/Defend/CounterReady/whatever the profile array says) never enters that decision. Whenever B doesn't attack, A is scripted as attacker throwing a fixed 밭다리(BigThrow, Standard) regardless of what A "chose" that turn. So the 3 profiles (반복/변주/현실적) don't actually differentiate A's attack behavior from each other in any way that reaches the engine — only B's habit brain and the read/row bookkeeping see A's declared choice. Exact code + explanation: docs/overnight-2026-08-10.md §6.4.
§9 stamina-snapshot fix (2026-08-11, MATCH_RULES v3.5 → v3.6): the §9.1 osaekomi gate's "체력 <50" now reads the SAME pre-payment stamina snapshot §4's judgment formula already used earlier in the same exchange, instead of a fresh post-payment read taken later in `TryEnterOsaekomi`. Both readings sat in the same [25,50) band in every case measured so far, so no observed behavior changed, but it was two different numbers answering the same question inside one exchange — now it's one. See docs/overnight-2026-08-10.md §8 for the fingerprint-diff result of this change (a deliberate behavior change per §9.1's own new wording, checked and reported, not silently accepted).

야간 자율 실행 2차 (2026-08-11 밤, branch feat/match-engine, not merged) — 작업2~6, 커밋 6bd5a06..a9614cb
200/200 harness green (180→200: 작업2 +2, 작업3 +8, 작업4 +6, 작업5 +6, 작업6 +1 신규 테스트[기존 3개는 시그니처 기본값으로 무변화]), fingerprint IDENTICAL 전 구간. 전체 상세는 docs/overnight-2026-08-11.md — **판단 대기 5개가 그 문서 맨 위에 있다.** 요약:
  작업2 — docs/step4-narration-worksheet.md 배정 9키를 ResultNarrationTable.CreateDefault() 로 등록(문구 무수정). m27·m34는 워크시트 결정대로 폴백 유지. m18 TechniqueId="밭다리"는 지시 문자 그대로이나 실제 기술(안뒤축감아치기)과 달라 판단 대기 #1
  작업3 — MATCH_RULES §4.4 **쿠즈시(무너뜨리기) [초안 — 사람 검토 전]**, v3.6→v3.7. KuzushiGateEnabled 기본 **false**(대조 모드 완전 동일 보장). on일 때: 그립 없이 선언 불가, 성립해도 판정 미발화, 1교환만 지속, 지속 중 아니면 다음 큰기술이 Sloppy 로 강등(JudgementCalculator 자체는 무수정 — 강등은 MatchEngine 컨텍스트 단계), SurpriseExempt 로 말아업어치기류 면제, 그쳐/굳히기 진입/경기 종료로 소멸. HandTarget.Kuzushi 는 enum 끝에 추가(값 안 바뀜)
  작업4 — Brain/KuzushiPrecedingCommitSource.cs: HabitBrainCommitSource 를 감싸 큰기술 앞에 쿠즈시 턴 자동 삽입. 습관 카탈로그·읽기 사다리 무수정 — 이미 나온 결정을 한 턴 버퍼링해서 늦출 뿐. 발견한 미해결 타이밍 이슈(LiveReadResolver.NoteOpponentHabit 이 쿠즈시 교환에서 먼저 불려 진짜 기술 교환엔 새 알림이 없음) → 판단 대기 #2, 지금은 아무 데도 안 배선돼 있어 관측되는 버그 아님. B 쿠즈시는 현재 100% 정직한 tell — 페이크는 후속 설계
  작업5 — 6단계 스캐폴딩 3파일(Game/Match/): LiveAssembler(순수 C#, 하니스 테스트 있음) + LiveMatchRunner(오케스트레이션, MatchRunner 42노드 대조와 완전 별개) + LiveMatchPanel(MatchHud 전례대로 런타임 캔버스, 씬 무수정) — 손/코어/하체 3열, 확정→해결→서술(폴백 시각 구분)→다음 턴, 내 성향 노출 막대(경고선/위험선, 확정 전 실시간), 굳히기 2지선다. B 선택지·심판 누산기 숫자 전혀 노출 안 함. Unity 에디터 typeof 로드까지 확인(0 에러/0 경고) 했으나 **LiveMatchRunner/LiveMatchPanel 은 Play 모드 실행 확인 안 됨 — 사람 몫.** 씬에 컴포넌트를 붙이는 것도 사람 몫(씬 파일 무수정). 판단 대기 #3(뜯기·잡기 유지 게이트는 §2 표에 없어 추론) #4(동시 공격 시 B 우선순위, PlayModeSmokeTests 선례 재사용) #5(B 굳히기 정책은 고정값, 진짜 AI 없음)
  작업6 — PlayModeSmokeTests 에 kuzushiEnabled 매개변수(기본 false) 추가, 3프로필×{off,on} 실측 6회를 docs/overnight-2026-08-11.md 비교표로. 튜닝 없음. 관찰: 반복 프로필 교환수 3.5배 증가(4→14), 변주 프로필은 on 에서 50교환 캡을 소진하고 미종료(off 는 27교환에 종료), 되치기는 반복/on 셀에서만 5건 — 전부 해석이지 확정 아님, 판단은 사람 몫
  Unity Editor + MCP 브리지가 밤새 연결 유지(사람 없음) — 매 작업 후 AssetDatabase.Refresh() + GetConsoleLogs 로 0 에러/0 경고 재확인, RunCommand 안에서 짧은 이름(CompilationPipeline 등)이 Unity.AI.Assistant.* 네임스페이스와 충돌하는 기존 함정 재확인(전체 경로로 우회)

Next Up (in priority order)
**가장 최근 — 실전 UI 개편: 손·코어·하체 상시 3열 폐지, 캐릭터 옆 드롭다운으로
(사용자 결정, 레이아웃 변경).** 목표: 점수판 화면 중앙 하단, 확정 버튼 점수판
바로 위(3축 전부 선택 전 비활성/회색, 전부 선택 시 노란색 활성화), 손·코어·
하체는 각각 캐릭터 옆 트리거 버튼 + 클릭 시 위로 펼치는 드롭다운.
구현 전 3건 조사·보고 후 사용자 확정: **(a) 배치 기준 — 주체 기준**(손·코어·
하체 전부 A 캐릭터 옆, 상대를 노리는 손도 포함 — "조작 버튼이 한 곳에 몰려
실수 적음"). 실측 좌표(1920 기준): 하체 (475,561)/코어 (475,647)/손 (475,733).
**(b) 추적 방식 — 화면 고정 위치**(본 추적 기각). A 의 Fight Idle 1.5초(12fps
18스텝) 실측: 골반 x±5.1/y±2.0px, 머리 x±17.5/y±2.9px, 오른손 x±10.3/y±11.3px —
83ms 마다 버튼이 튀어 보일 위험이 커 정밀 포즈 추종보다 클릭 안정성을 택함.
**(c) 드롭다운 방향 — 위로 펼침**. 코어(144px)·하체(194px)는 A 자신 위로만
펼쳐져 상대(B)와 179px 여백을 사이에 두고 안 겹침(실측 확인). 손은 그립 상황에
따라 옵션이 최대 10개(≈460px)까지 늘어날 수 있어 화면 위로 넘칠 수 있다는
한계를 코드 주석에 남김 — 스크롤 등은 범위 밖, 코드로 안 고침.
구현: `LiveMatchPanel.BuildCanvas` 의 `RootLayout` 순서가 MiddleRow →
OsaekomiRow(굳히기 2지선다, 위치만 이동·배선 무수정) → ConfirmRow → ScoreboardRow
가 됐다. `BuildStandingRoot`/`BuildColumnRoot`(상시 3열)를 `BuildFloatingControls`/
`BuildAxisControl`(트리거+드롭다운)로 교체, `RebuildColumn`→`RebuildAxisOptions`로
개명(선택 시 드롭다운 자동 닫힘 추가). 확정 게이트용 `_handChosen`/`_coreChosen`/
`_legsChosen` bool 3개 추가(코어·하체는 기본값 중립이 이미 "유효한 선택"이라
필드값만으론 "실제로 골랐는지" 구분 불가 — 드롭다운에서 클릭해야 true). 드롭다운은
한 번에 하나만 열림(`ToggleDropdown`). 옛 "선택: —·중립·중립" 요약 줄은 삭제하고
각 트리거 라벨에 접었다(사용자 승인 — 재활용/라벨 반영 둘 다 허용된 선택지 중 후자).
**회귀 검사 중 발견해 사용자 승인받아 예외 처리한 것** — `NarrationLog` 가
`leftColumn` 전체(756px 높이)를 그대로 채우고 있어(지난 세션 "전체화면 배경"
작업의 결과, 이번 세션 변경 아님) A 옆 고정 좌표(y=561~773)의 트리거 버튼과
실제로 겹쳤다(회귀 검사 실측으로 처음 발견). "서술 로그는 범위 밖" 원칙에 대해
이번 한 건만 예외로 승인받아 `NarrationLog` 를 트리거 밴드 위쪽(y=792~1072,
고정 280px)에 앵커 — 아래쪽은 경기 화면이 그대로 보인다. 드롭다운이 열릴 때는
`NarrationLog` 아래쪽과 살짝 겹칠 수 있다(손 최대 221px, 코어 43px, 하체 7px,
전부 실측) — 팝오버가 배경 패널을 잠깐 덮는 것과 같은 종류로 판단해 허용, **닫힌
상태(상시 노출)만 실제 문제였고 그건 0으로 확인됨.**
검증: Unity MCP 컴파일 0/0. Play 모드 실측 — 드롭다운 열기/닫기/선택 3축 전부
동작 확인(손 5~10개 옵션 상황별 정상, 코어 3개·하체 4개 고정 정상), 확정 버튼
0/3→3/3 선택에 따라 interactable=False→True·색 Idle(회색)→ShidoYellow(노랑)
전환 확인, 확정 클릭 시 제출+상태 리셋(다음 턴 대비) 확인. 7개 해상도(1920~320)
닫힌 상태 전부 zeroSize=0·겹침=0·줄바꿈=0. 드롭다운 열린 상태는 위에 적은
NarrationLog 예상 겹침만 확인(B 나 다른 요소와는 안 겹침). 하니스 209/209,
지문 IDENTICAL(전부 프레젠테이션 전용, Core/Brain 무수정). 커밋 `f84f631`.
**가장 최근 (그 앞) — 전체화면 배경 후속 (사이드바 가독성 진단 + 카메라 위치 이동).**
사용자 Play 실측 신고 2건 대응.
**① 사이드바 가독성 — 진단 결과 구조적 결함 없음.** "코치의 조언/수 노트/내 성향
본문이 캐릭터와 겹쳐 안 읽힌다, 배경이 거의 투명해 보인다"는 신고를 실측으로
확인 — 사이드바 3박스(`CoachAdviceBox`/`NotesBox`/`TendencyBox`) 모두 이미
`PanelBg`(알파 0.92)가 적용돼 있고(다른 패널과 이미 통일된 상태였다, 이번에 새로
추가한 게 아님), sibling 순서도 `ArenaBackground`(index 0) 뒤에 `RootLayout`
(index 1)이 와서 정상, 자식 텍스트도 박스 경계를 벗어나는 오버플로 없음(전부
실측 확인), `CanvasGroup`으로 전체 알파를 낮추는 요소도 없음. 즉 "다른 패널과
같은 스크림 처리로 통일"할 게 애초에 없었다 — 코드 변경 없음. 실제 원인으로
추정: ②(카메라 이동) 전에는 B 가 RTx=192~230(사이드바 안전선 207.8 바로
근처/걸침)에 있어 사이드바 바로 뒤에서 밝고 움직이는 캐릭터가 8% 투과율로도
눈에 띄게 비쳐 보였을 가능성 — ②로 B 를 RTx=161~201(사이드바에서 충분히
떨어짐)로 옮기면서 간접적으로 완화됐을 것으로 예상되나, 육안 확인은 사람 몫.
**② 스크림 알파 — 값 변경 없음, 적용 위치만 확인.** `PanelBg`(0.16,0.16,0.18,
0.92) 는 현재 `ScoreboardRow`/`BottomInputRow`/`NarrationLog`(전체화면 배경
세션에서 추가)와 `CoachAdviceBox`/`NotesBox`/`TendencyBox`(원래부터) 총 6곳에
적용돼 있다. 값 조정은 그 상수 한 곳만 고치면 전부 같이 바뀐다 — 사용자 판단
대기, 이번 세션에서 안 건드림.
**③ 카메라 위치 — 두 파이터가 사이드바 밖에 들어오도록 이동.** 신고: 전체화면
전환 후 A는 중앙, B는 우측 사이드바 뒤로 완전히 가려짐. 실측: x=0 기준 결합
RT-x 폭은 169px(RT 폭 320 중)로 사이드바 안전폭(~208px, 사이드바 시작=화면폭의
약 65%)보다 좁아 확대·축소 없이 옆으로 이동만 하면 됐다(세로 100px 규격과
무관 — 좌우 이동은 세로에 영향 없음). x=0.6 으로 이동, 재실측: B 우측 끝
RTx=201.5(여유), A 좌측 끝 RTx=26.0(여유), 세로 A 102.1px/B 100.9px(규격
유지). 16:9(1920x1080/1280x720/960x540/320x180) 및 근사치(719x404/600x338)
확인.
**한계(코드로 안 고침, 보고만) — 4:3(1024x768) 은 이 값으로 안 풀린다.**
`EnvelopeParent`가 좌우 25%를 잘라내는 창이라 A 왼쪽이 화면 밖으로 나가고
(canvas-x=[-58,391], 왼쪽이 음수) B 도 사이드바(1024 폭 기준 665부터 시작)에
다시 걸린다(B canvas-x=[525,688], 오른쪽이 665 초과). 크롭 방식이 종횡비마다
달라 정적 카메라 하나로 모든 창 비율을 동시에 만족시킬 수 없다 — 16:9(주
타깃으로 추정)를 우선했다.
검증: Unity MCP 컴파일 0/0. 7개 해상도 재실행 — zeroSize=0·겹침=0·버튼
줄바꿈=0(구조적 검사는 4:3 포함 전부 통과 — 캐릭터 프레이밍만 별개 이슈).
하니스 209/209, 지문 IDENTICAL. 커밋 `74ad48b`.
**별건 확인 — HANDOFF 2종 상태.** `md/HANDOFF_claude_code_runner.md`/
`md/HANDOFF_claudecode_opponent-brain.md` 는 커밋 `5f65192`(2026-08-11, 이
세션 이전)에서 삭제가 이미 커밋·push 완료된 상태. 실수 삭제가 아니라 그 세션에서
사용자가 명시적으로 "삭제 확정"을 지시해 처리된 것 — 복원 불필요, 현재 상태가
맞다.
**가장 최근 (그 앞) — 경기 화면을 창 전체 배경으로 (씬 스카이박스+RT 이중 배경 정리).**
신고: 창 전체를 덮는 씬 스카이박스와 좌상 박스 안 320x180 RT가 배경 두 겹으로
보여 혼잡. `ArenaStageView`의 RawImage 를 좌상 `ArenaBox`에서 꺼내 캔버스
최하단 sibling(`ArenaBackground`)으로 옮겨 창 전체를 채우는 배경이 되게 하고
그 위에 기존 UI 패널들이 얹히는 구조로 재작성했다. RT 해상도 320x180·MSAA
off·Point 필터·12fps 스텝은 무수정 — 표시 크기만 커진다.
`ArenaBox`/`RectMask2D`는 제거했다 — 예전엔 좁은 박스 안에서 오버사이즈된
RawImage 가 옆 블록을 침범해서 필요했지만, 캔버스 전체가 이제 배경의 영역이라
침범할 이웃이 없다(화면 밖으로 나가는 부분은 렌더 대상 자체를 벗어나 마스크
없이도 안 보임). `EnvelopeParent`는 유지 — 창 비율이 16:9 가 아닌 경우(예:
1024x768=4:3) 그대로 늘리면 찌그러지므로 여전히 필요. 비워진 좌상 자리는
`NarrationLog`(좌하 상태·서술·로그)가 이어받는다 — `leftColumn`에서
`BuildArenaPlaceholder` 호출만 제거, 나머지 구조 무수정. 상단 점수판·우측
사이드바 3종·하단 입력은 위치·크기 무수정.
**가독성** — 텍스트가 배경 위에 얹히므로 점수판(`ScoreboardRow`)·하단 입력
(`BottomInputRow`)·좌하 서술 로그(`NarrationLog`) 셋 다에 스크림(반투명
어두운 판)을 얹었다. 새 색을 만들지 않고 사이드바에 이미 쓰던 `PanelBg`
(알파 0.92)를 재사용 — **판단(지시 아님)**: 알파값은 스크린샷 없이 확정할
수 없어 하나 고른 것이고, `PanelBg` 한 곳만 고치면 넷 다 같이 바뀐다. 스크림은
RT 밖 UI 레이어에만 적용 — RT 내부(경기 장면)는 무수정.
**카메라 프레이밍 재확인** — 전체화면이 되어도 "320x180 RT 안에서 캐릭터
세로 100px" 확정 규격은 RT 공간 값이라 표시 배율과 무관, 카메라는 무수정.
실측: A 96.1px/B 95.4px(Fight Idle 애니메이션 자연 변동 범위, 목표 100px
유지). 배경 크롭 실측(7개 해상도): 1920x1080/1280x720/960x540/320x180(모두
정확히 16:9) 크롭 0%, 719x404/600x338(≈16:9) 크롭 0.1~0.15%(무시 가능),
1024x768(4:3) 만 가로 25%(양쪽 12.5%씩) 크롭 — `EnvelopeParent`의 설계대로
동작.
검증: Unity MCP 컴파일 0/0. 7개 해상도 재실행 — zeroSize=0·겹침=0(의도된
전체화면 배경 `ArenaBackground`는 겹침 검사에서 이름으로 제외 — 모든 걸
덮는 게 설계 의도라 다른 패널과의 "겹침"은 버그가 아님)·버튼 줄바꿈=0.
스크림 3곳 색 실측으로 `PanelBg` 적용 확인. 하니스 209/209, 지문 IDENTICAL.
커밋 `dd56bab`.
**가장 최근 (그 앞) — 경기 화면 UI 도트화 진단·수정 + 캐릭터 확대 (사람 Play 실측 신고 2건 대응).**
신고: ① UI 텍스트까지 도트화되어 안 읽힘 ② 경기 화면(캐릭터)이 너무 작음.
**① 진단 결과 — RT 파이프라인 버그가 아니었다.** 실측으로 `LiveMatchCanvas`가
`renderMode=ScreenSpaceOverlay`/`worldCamera=null`임을 확인 — Overlay 캔버스는
구조적으로 씬 카메라를 거치지 않아 저해상도 RT 경로에 물릴 수 없다
(`ArenaStageCamera.cullingMask` 가 UI 레이어를 포함했던 것도 실측으로 무관함을
확인). 진짜 원인은 **에디터 Game View 커스텀 렌더 해상도가 지난 세션(좁은 화면
레이아웃 테스트)의 719x404 로 그대로 남아있던 것** — `CanvasScaler`(참조
1920x1080)가 이를 반영해 `scaleFactor=0.374`로 낮췄고, 전체 UI가 저해상도로
그려진 뒤 창 크기로 늘어나 보인 것(1920x1080 리셋 즉시 scaleFactor=1로 크리스프,
실측 확인). `PlayModeSetupMenu`가 실행할 때마다 1920x1080 으로 리셋하도록 고쳐
재발을 막았다.
**② 카메라 프레이밍 재실측 확정**: 거리 −6→−4.25, 하향 11° 추가(FOV45 유지) →
320x180 기준 캐릭터 세로 A 100.9px/B 99.6px, 좌우(57~232/0..320)·상하(41~143/
0..180) 클리핑 없음(`WorldToScreenPoint` 직접 실측). RawImage 는
`AspectRatioFitter` `FitInParent`→`EnvelopeParent`로 바꿔 박스를 빈틈없이
채운다(16:9 내용비율 유지, 넘치는 쪽만 크롭) — `ArenaBox`에 `RectMask2D` 추가로
오버사이즈된 RawImage 가 점수판/서술 블록 위로 넘쳐 그려지는 것을 막았다.
`ArenaBox`의 `flexibleHeight`도 1.2→4.5 로 올려 실측 높이를 173→260px 로
키웠다(`NarrationLog`는 바닥 178px 보다 여유 있는 236px 확보).
**판단(지시 아님, 코드 주석에도 남김)** — 좌상 블록 폭(1222px 실측) 대비 16:9
정박스가 되려면 386px 높이가 필요한데, `NarrationLog` 바닥(178px)을 지키는 한
최대 318px 이 한계라 완전한 무크롭·무바 동시 달성은 이 5블록 구조에서 기하학적
으로 불가능함을 실측으로 확인했다. `EnvelopeParent`(캐릭터 머리~19%·발~14%
가벼운 크롭)와 `FitInParent`(크롭 없지만 박스 폭 절반 이상 검은 띠) 중 전자가
더 나은 시각적 결과라고 판단해 골랐다 — `fitter.aspectMode` 한 줄로 쉽게
되돌릴 수 있다.
검증: Unity MCP 컴파일 0/0. 7개 해상도(1920/1280/1024/960/719/600/320) 전부
재실행 — zeroSize=0·겹침=0(`RectMask2D` 인지 겹침 계산으로 `ArenaView` 오버사이즈
오탐 제외)·버튼 줄바꿈=0, `Canvas.scaleFactor`가 각 해상도에 맞게 정상 추적됨
(더는 고정 안 됨). 하니스 209/209, 지문 IDENTICAL. 커밋 `3bc6acd`.
**가장 최근 (그 앞) — 경기 화면 저해상도 렌더 v1 (좌상 placeholder 채움, `ArenaStageView` 신규).**
좌상 "경기 화면 — 연출 준비 전" 자리를 실제 320x180 RenderTexture 로 채웠다 — 연출·모션
매핑(노드별 클립 전환)은 범위 밖, 두 파이터가 Fight Idle 로 서 있는 화면까지만. Main.unity
는 참조/추가로딩하지 않는다 — Fighter_A/B(Y Bot.fbx/Ch30_nonPBR.fbx)가 Main.unity 씬 YAML
실측으로 원본 FBX 의 PrefabInstance 일 뿐임을 확인했으므로, 프리팹화 없이 `ArenaStageView`
가 Play Mode Setup 의 빈 씬 안에서 전용 카메라+바닥+조명+파이터 둘을 처음부터 새로 짓는다.
확정 규격 그대로 구현: RT 320x180/antiAliasing=1(MSAA off), 카메라 allowMSAA=false,
RT.filterMode=Point, `animator.enabled=false` 후 1/12초마다 `animator.Update(1f/12f)` 로
12fps 스텝. `PlayModeSetupMenu` 가 FBX/컨트롤러/바닥 재질을 `SerializedObject` 로 주입
(koreanFont 와 같은 패턴). f241b7b 가 검은화면 방지용으로 추가했던 빈 "Main Camera" 는 이제
씬에 실제 3D 콘텐츠가 생기므로 `cullingMask=0` 으로 막아 화면 전체 중복 렌더를 방지 —
사람 지시가 아니라 구현 중 발견해서 판단한 것. 컴포넌트 간 Awake 순서가 보장되지 않는
문제(LiveMatchPanel 이 stage.Output 을 읽기 전에 ArenaStageView 가 먼저 지어야 함)는
`EnsureBuilt()`(멱등) 를 두고 LiveMatchPanel.Awake 에서 명시적으로 먼저 호출해 해결.
검증: Unity MCP 컴파일 0/0. Play 모드 실측 — RT 320x180/antiAliasing=1/Point 확인, 카메라
allowMSAA=False/FOV45/pos(0,1.6,-6) 확인, 두 파이터 컨트롤러 정상 할당 확인, MainCamera
cullingMask=0 확인. 12fps 스텝 간격은 1초간 13개 스텝 이벤트를 타임스탬프로 찍어 실측 —
간격이 0.083초 전후(≈1/12초)로 균일함을 확인. 하니스 209/209, 지문 IDENTICAL
(`ArenaStageView.cs`/`PlayModeSetupMenu.cs` 는 애초에 하니스 컴파일 대상 아님,
`LiveMatchPanel.cs` 변경도 프레젠테이션 전용). 다음 단계는 범위 밖으로 남겨둠 — 노드별
모션 전환·연출 배선은 아직 정적 Fight Idle 뿐이다.
**가장 최근 (그 앞) — 좁은 화면 레이아웃 붕괴 수정 (719px 스크린샷 신고).** 원인은 CanvasScaler 설정이
아니었다 — 실측 확인 결과 `uiScaleMode=ScaleWithScreenSize, referenceResolution=(1920,1080),
matchWidthOrHeight=0`(Unity 컴포넌트 기본값, 코드가 손댄 적 없음). match=0 이면 캔버스의
**겉보기 폭은 실제 화면 폭과 무관하게 항상 1920** 이다(`scaleFactor=screenWidth/1920` →
`캔버스폭 = screenWidth/scaleFactor = 1920`, 공식으로 확정 + 320~1920px 전 구간 실측으로
재확인) — 그래서 "화면이 좁아지면 레이아웃 계산이 좁아진다"는 가설은 틀렸다.
**진짜 원인은 해상도와 무관한 구조적 버그였다**: `ScoreboardRow`/`BottomInputRow`(레이아웃
그룹이 아닌 순수 슬롯)와 다수의 `MakeFlexText`/`SetFlexible` 호출이 교차축(가로 그룹의
세로, 세로 그룹의 가로)에 `preferredWidth`/`minWidth`/`flexibleWidth` 중 **아무것도** 안
줬다 — Unity 실측(2026-08-11, `LayoutElement` 필드 직접 대조)으로 확인한 규칙: 셋 다 -1(기본)
이면 그 축은 **화면 크기와 무관하게 항상 0** 으로 무너진다. 즉 이 버그는 719px 에서만이
아니라 1920px 에서도 이미 있었다 — 안 걸린 건 지난 세션의 쌍별 겹침 검사(`ox>0.5 && oy>0.5`)
가 폭 0인 요소끼리는 애초에 안 걸리는 사각지대였기 때문이다(폭 0 인 버튼 63개 중 다수가
"안 겹친다"로 통과했지만 실은 전부 안 보이는 크기였다). 손 열 버튼의 실측: `InputColumns.width=24`
(순전히 spacing, 자식 기여 0), `첫 버튼.width=0` — 720p 든 1920p 든 동일했다(직접 대조 확인).
수정: `SetFixedHeight`/`MakeButton` 이 `flexibleWidth=1` 을 기본으로 주고, 모든
`MakeFlexText`/`SetFlexible` 교차축 인자를 0→1 로, `MakeFlexText` 는 `preferredHeight=minHeight`
도 같이 준다(min 단독으로 충분한지 실측 확인 안 했으므로 안전하게 preferred+min 조합으로
맞춤 — 이 조합과 "flexible 단독"만 실측으로 동작 확인됨). 추가로 `MakeText` 에 TMP
auto-size(`enableAutoSizing`, `fontSizeMin=max(8, 크기*0.4)`) 를 전부 켜고, 버튼 라벨엔
`singleLine`(줄바꿈 금지) 도 켰다 — 요청대로 "글자 세로 쌓임" 대신 폰트 축소로 대응.
검증: `PlayModeWindow.SetCustomRenderingResolution` (에디터 공식 API, reflection 아님)로
1920×1080/1280×720/1024×768/719×404 네 해상도 전부 재현 — 63개 요소·zeroSize=0·쌍별
겹침=0·버튼 라벨 줄바꿈=0 동일하게 통과(720p 재발 방지 위해 추가로 600×338/320×180 도
같은 결과, 320px 도 구조적으로는 깨끗함). 719px 에서 실제 클릭(손/코어/하체 선택+확정)까지
재확인 — 서술·상태 텍스트 정상. **최소 지원 해상도**: match=0 특성상 레이아웃 구조(겹침·
줄바꿈)는 이론상 어떤 폭에서도 안 깨진다(실측으로 320px 까지 확인) — 그러나 가독성은 별개다,
캔버스가 항상 1920 기준으로 균등 축소되므로 폭이 줄면 모든 글자가 같이 작아진다(320px 폭에서
버튼 글자 실측 렌더 크기 ≈3px, 사실상 판독 불가). **권장 최소 폭 960px**(버튼 글자 ≈9px) —
이 숫자는 폰트 크기 계산 추정이고 스크린샷으로 육안 확인한 것은 아니다, 사람이 실제로 보고
더 낮춰도 되는지 판단할 것. Unity MCP 컴파일 0/0, 하니스 209/209 그린 · 지문 IDENTICAL
(이 세션이 건드린 `LiveMatchPanel.cs`/`LiveMatchRunner.cs` 는 애초에 `tools/dotnet-harness/Harness.csproj`
컴파일 대상이 아님 — `LiveAssembler.cs` 만 포함, 재확인).
**가장 최근 (그 앞) — 화면 전면 재설계: 3분할 구조 + 사이드바 3종(코치의 조언/수 노트/내 성향), Play 실측 완료.** 문제의식: 수싸움 게임인데 "관측 결과"(상대 습관을 읽어낸 결과)를 보여줄 자리가 화면에 없었다. `LiveMatchPanel.BuildCanvas` 를 전면 재작성 — 상단(점수판, 무수정 이관)/좌상(경기 화면 자리, 지금은 배경+안내문뿐인 placeholder)/좌하(위상·체력·그립 상태 줄 + 서술 줄 + 최근 5교환 로그)/우측(사이드바 3종)/하단(입력 3열+선택확인줄+확정버튼) 5블록. 좌표는 손으로 안 찍는다 — `VerticalLayoutGroup`/`HorizontalLayoutGroup` 이 실제로 계산하게 만들었다(지난 anchorX 반폭 산술 버그, 바로 위 항목의 재발 방지가 이 구조의 이유). `LiveMatchRunner` 에 `Ledger`/`OpponentHabits` 읽기 전용 프로퍼티 2개만 추가(기존 `Tendency`/`Config` 와 같은 패턴) — Core/Brain 무수정.
사이드바 3종: (1) **코치의 조언** — 상태 기반 v1 규칙 5종(굳히기 중/잡기 열세/상대 체력 저하/상대 흔들기/기본), 판정식 합계·심판 누산기 미참조. (2) **수 노트** — `ObservationLedger.Observations(id) >= 1` 인 습관만 행으로 노출(아직 안 본 습관은 존재 자체를 안 보여준다), 심증(1~2회)/확정(3회 이상, `<s>` 취소선 아님·강조색)/폐기됨(`<s>` 취소선, `HabitState.IsRetired`) 3단계. (3) **내 성향** — 기존 노출 막대(`RefreshExposure`, 임계값 로직 무수정) 이관 + 경고선/위험선 라벨 상시 표시로 변경. `_preemptedText` 별도 배너는 제거(서술 줄과 100% 같은 문자열의 3중 중복이라 하나만 정리, 정보 손실 없음).
검증: Unity MCP 컴파일 0/0(새 프로퍼티를 직접 참조하는 RunCommand 로 재컴파일 확인, HabitCount=5 진도윤 정상 로드). Play 모드에서 `Button.onClick.Invoke()` 로 실제 30교환 진행 — 좌표 실측(`GetWorldCorners` 쌍별 AABB, 부모-자식 포함관계 제외)으로 서기 입력 상태 63개 요소/겹침 0, 굳히기 입력 상태(수동 토글) 42개 요소/겹침 0. 값 대조: 코치 조언 폴백 문구 MATCH=True(그립열세/체력저하/흔들기 분기는 이번 세션에 자연 발생 안 해 코드 경로만 확인), 수 노트는 **심증 빼고 확정·폐기됨 둘 다 실측**(SeekGripRightSleeve 폐기됨, SeekGripLeftSleeve 확정 3회 — 화면 문자열이 `Ledger`/`HabitState` 직접 재계산과 일치), 내 성향 경고선 마크가 실제로 Dim→Warning(D9A61A) 전환하는 것도 `TendencyWindow.MajorityMatches` 독립 재계산과 대조해 확인. 하니스는 대상 아님(`tools/dotnet-harness/Harness.csproj` 에 `LiveMatchPanel.cs`/`LiveMatchRunner.cs` 자체가 원래 없음, `LiveAssembler.cs` 만 포함 — 확인 후 재실행 생략).
**가장 최근 (그 앞) — 입력 열 텍스트 겹침 수정 (Play 실측 스크린샷 신고).** 원인은 사용자가 짚은 "쿠즈시로 항목 늘어난 손 열의 행 높이가 항목 수를 반영 못 한다" 가설이 **아니었다** — 실측(GetWorldCorners, 2026-08-11) 결과 Unity `VerticalLayoutGroup` 이 이미 손 열 10개 항목을 열 높이(285.3px) 안에 맞춰 자동으로 23.4px 씩 균등 축소하고 있었고, TMP 텍스트 필요 높이(21.6px)도 그 안에 들어가 겹침이 없었다(OVERFLOWS=False, 10개 전부). 진짜 원인은 전혀 다른 곳: `BuildStandingRoot` 의 세 열 중심 anchorX(0.06/0.38/0.62) 가 열 반폭 0.14 기준으로 균등하지 않았다(중심 간격 0.32 / 0.24 — 원래부터 비대칭, 이번 세션이 만든 버그 아님) — 손-코어는 간격 0.04 로 안 겹쳤지만 **코어-하체는 −0.04(실측 −76px), 두 열의 버튼 배경이 항상 겹쳐 있었다.** 점수판 재배치는 원인이 아니다 — `StandingInput.sizeDelta.y=320` 은 git log 로 생성 이후 한 번도 안 바뀐 걸 확인. 수정: 하체 열 중심만 0.62→0.68 로 이동(코어·손·확정 버튼은 무수정) — 재실측으로 45쌍(손10+코어3+하체4+확정1) 전부 0 겹침, 간격 손-코어 76.1px/코어-하체 38.0px(이전 −76)/하체-확정 72.9px 확인. 하니스 209/209, 지문 IDENTICAL(프레젠테이션 전용 좌표 수정)
**가장 최근 (그 앞) — 골든스코어 도달 불가 수정 (53be917 후속).** `MatchClock.RegulationExpired`/`ScoreBoard.WinnerAtTimeUp()` 둘 다 격리 단위테스트 밖에는 호출부가 0개였다 — `MatchEngine.ResolveExchange` 가 교환 종료 후 시간종료를 한 번도 확인 안 했다(둘 다 그 자체로는 정확했다, CORE_DESIGN 이 이미 "MatchClock 이 RegulationExpired 를 내면 엔진이 GoldenScore 로 넘기거나 승자를 정한다"로 못박아둔 대로). 새 코드 없이 배선만: `ScoreBoard.FinishAtTimeUp()`(신규, WinnerAtTimeUp 로 결정되면 Finish, 다 비겼으면 EnterGoldenScore 호출 — 둘 다 기존 공개 API) 을 `ResolveExchange` 끝(그쳐/굳히기 진입 판정 뒤, 쿠즈시 갱신 앞)에서 `!Board.IsFinished && !Osaekomi.Active && Clock.Phase!=GoldenScore && Clock.RegulationExpired` 조건으로 호출. 새 이벤트 `GoldenScoreEntered` 1개 추가(결정 안 나고 골든스코어로 넘어갈 때만). `LiveMatchPanel.EventSummary` 에 표시 문구 한 줄 추가
의도적으로 손 안 댄 경우 — **정규시간이 만료되는 순간 이미 굳히기가 막 열려 있으면(그 교환 자체가 절반/한판을 내며 굳히기를 연 경우) 시간종료 판정을 안 한다.** MATCH_RULES §1 은 "서기 교환은 4:00 을 넘겨도 끝까지 간다"만 규정하고 "이미 진행 중인 굳히기가 정규시간을 넘겨도 계속 도는가"는 규정하지 않는다 — 애매해서 구현 안 함, `GoldenScoreTests.OsaekomiJustOpened_AtRegulationExpiry_DoesNotTriggerTimeUp_Unresolved` 가 현재 동작(그대로 굳히기 유지, 시간종료도 골든스코어도 안 걸림)을 사실로 고정해둔다. 사람 판단 필요
검증: `GoldenScoreTests.cs` 신규 7개(결정된 시간종료가 즉시 끝남 / 다 비긴 시간종료가 골든스코어로 넘어가고 시계가 무제한이 됨 / 골든스코어 중 판정 하나로 즉시 종료 — 풀 엔진 경로로 / 골든스코어 중 지도 3개도 동일 — 풀 엔진 경로로 / 틀린 답 두 개: 시간 되기 전엔 안 넘어간다, 굳히기 막 연 교환엔 안 걸린다). 202→209 그린, 지문 IDENTICAL(42노드 시나리오는 정규시간 근처도 안 간다). 스모크 6개 구성(반복/변주/현실적 × 쿠즈시 off/on) 전부 재확인 — 가장 오래 도는 것(변주 on, 50교환 캡 소진)도 최대 추정 150~165초로 240초 근처도 안 간다(50교환×고정 3초 + 굳히기 진입 1회의 이탈 틱 최대 15초). "ran out of exchanges" 는 하니스 자체의 50교환 캡이지 인게임 240초 시계와 무관 — 회귀망이 이 경로를 하나도 안 밟는다는 사실 그대로 남겨둠. Unity MCP 컴파일 확인(0/0), `ScoreBoard.FinishAtTimeUp()` 을 직접 참조하는 RunCommand 로 재컴파일 확인
**가장 최근 (그 앞) — 실전 점수판 규격으로 재배치, Play 실측 완료.** 2026-08-11 오후 2차: 시계·절반(W)·유효(Y)·지도(S/H, 카드 아이콘)·누르기 카운트를 사진 레퍼런스 그대로의 점수판 블록(A=흰 배경 위 행, B=파란 배경 아래 행, IPPON 강조 스트립, 골든스코어 표기)으로 옮겼다. 잡기·체력·위상·쿠즈시·교환 로그·선택 확인은 기존 구현 그대로 점수판 아래로만 재배치(로직 무수정). Play 모드에서 실제로 버튼을 눌러(메뉴로 씬 구성 → Play → `Button.onClick.Invoke()` 로 손/코어/하체 선택+확정 2턴 실행) 화면에 뜬 텍스트를 엔진 실측값으로 독립 재계산한 기댓값과 문자열째로 비교했다 — 타이머·양쪽 체력/그립/양손 잠김상태 전부 MATCH=True, 로그는 `docs/../play_verification.log` 및 이 세션 기록 참조. **아직 실측으로 못 본 것**: 실제 득점(W/Y 값이 0이 아닌 상태), 지도 카드 아이콘 렌더(로직상 개수만 확인, 색·표시는 미관찰), 골든스코어 표기(아래 항목 참조 —애초에 지금 아무도 안 켠다), "그쳐" 위상 플래시. 로직은 동일 코드 경로라 확률적으로 문제 없다고 보지만 시각적으로는 미확인
**골든스코어가 절대 안 켜진다 — 알려진 공백, 규칙은 안 건드림.** `ScoreBoard.EnterGoldenScore()`/`MatchClock.Phase = MatchPhase.GoldenScore` 를 호출하는 코드가 `LiveMatchRunner`/`MatchEngine` 어디에도 없다(grep 으로 확인, 2026-08-11 오후). 정규시간 만료(`Clock.RegulationExpired`) 자체를 실전 러너가 아직 아무데서도 확인 안 한다 — "Match length is still deliberately unresolved"(아래) 항목과 같은 뿌리. HUD 는 `Board.InGoldenScore || Clock.Phase==GoldenScore` 를 그대로 읽어 표시하도록 배선해놨지만, 트리거하는 코드가 없어 지금은 절대 뜰 일이 없다. 규칙/러너를 고치지 말라는 지시라 그대로 뒀다 — 트리거를 넣을지, 넣는다면 시점을 언제로 할지는 사람 판단
**누르기 카운트 20초 도달 불가 — 기존에 기록된 사실, 재확인만.** §9.3 v1 기본 드레인 정책(`PlayModeSmokeTests` 의 고정 정책, 항상 어긋남)에서는 5~15초 구간에서 대부분 굳히기가 끝나 유효/절반까지는 실전에서 종종 보이지만 한판(20초) 문턱은 구조적으로 잘 안 닿는다. 누르기 카운트 표시 자체는 국제룰 그대로(5/10/20초 경계) 구현했고 옳다 — 안 닿는 건 표시 로직이 아니라 §9.3 기본 정책 쪽 문제, 규칙은 손 안 댐
Stage 6 완료 — **가장 중요한 것: 사람이 Play 를 실제로 눌러야 한다.** 메뉴 CounterTime > Play Mode Setup (2026-08-11 아침 신설, 커밋 7325102)이 새 빈 씬 생성 + LiveMatchRunner/LiveMatchPanel 부착 + 쿠즈시 on + 한글 폰트 연결까지 자동으로 한다 — 메뉴 클릭 → Play 버튼, 이 두 번만 사람이 하면 된다. 캔버스 레이아웃·버튼 클릭·굳히기 2지선다 전환이 전부 미검증. 판단 대기 5개(docs/overnight-2026-08-11.md 맨 위) 중 #1(m18)·#2(쿠즈시 타이밍)는 아침에 해소, #3(뜯기/잡기 유지 게이트)·#4(동시 공격 우선순위)는 §2 에 "v1 임시 규칙"으로 문서화, #5(B 굳히기 고정 정책)는 여전히 미해결
쿠즈시(§4.4) 는 아직 [초안] — 사람이 검토해서 확정하거나 되돌릴 것. on 으로 실측한 결과(작업6)가 교환 수를 프로필에 따라 늘리거나 줄인다 — 밸런스 판단은 사람 몫
Stage 7 — regression + STATUS update (이 파일이 그 결과물이다 — 다음은 실제 Play 검증 이후 마무리 라운드)
Match length is still deliberately unresolved (see step 5 note above for the post-osaekomi numbers). Judgement formula, waza-ari thresholds, cost table and per-exchange seconds all stay untouched until after stage 6
잡기 HUD (패치 1.7 로 필수가 됐다) — Core 는 이미 GripChanged 에 부위를 실어 보낸다. Game 쪽에서 RenderTexture 바깥 풀해상도 레이어로 받아야 한다

해결됨 (2026-08-12) — `md/HANDOFF_claude_code_runner.md`/`md/HANDOFF_claudecode_opponent-brain.md`
두 파일이 세션 시작 시점부터 unstaged 삭제 상태였던 것을 발견해 보고했고, 사용자가 삭제를
확정(해당 단계 완료로 일회성 지시서 폐기)해 커밋했다. `md/HANDOFF_session_2026-08-11.md` §0
이 이 두 파일을 현존 문서로 전제하던 문장도 폐기 사실을 반영해 최소 수정.

결정 대기 (내가 판단 안 하고 남겨둔 취향 문제) — 선택지 버튼 높이가 111px → 82px 로 줄었다. 텍스트가 넘쳐서 TextPanel 을 키우느라 ChoiceRow 에서 떼어온 결과다. 라벨(26px)에는 충분하지만 버튼이 얇아 보이면 하단 패널을 1/3보다 키우거나 타이틀 34pt / 본문 24pt 를 줄이는 쪽으로 조정해야 한다
카피 교체 목록 — `LiveMatchPanel` 의 "B 선행 — A 공격 불발" (동시 공격 시 표시, 2026-08-11 아침)은 v1 임시 시스템 문구다. `PlaceholderATechnique`("밭다리" 고정)와 같은 부류 — 진짜 카피/연출이 붙기 전까지 기계적 문구로 둔다. 2026-08-11 오후에 추가된 상태 HUD·교환 로그의 기계적 문구도 같은 부류다: "서기"/"굳히기 — {who} 누름 {n}초 (이탈 {a}/{b})", "변화 없음"(교환 로그 기본값), "{who} {부위} 확보"/"{who} 뜯기 — {결과}"/"{who} 쿠즈시 설치"/"{who} 카운터 준비 적중 — 상대 읽기 하락"(EventSummary 폴백 서술) — 전부 `LiveMatchPanel.EventSummary`/`FighterStatus` 에 있다.
2026-08-11 오후(화면 재설계) 추가분, 전부 `LiveMatchPanel` — 경기 화면 자리: "경기 화면 — 연출 준비 전". 코치의 조언 5종(`RefreshCoachAdvice`): "굳히기 중 — 버티면 점수가 오르고, 빠져나가면 다시 선다" / "잡기 열세 — 손 싸움부터 다시 잡아라" / "상대 체력이 떨어졌다 — 밀어붙일 타이밍" / "상대가 흔들기를 걸었다 — 큰 기술을 조심해라" / "그립을 먼저 잡고 흐름을 만들어라"(기본값). 수 노트 습관 라벨 5종(`HabitLabel`): "업어치기 (그립 우세)" / "안뒤축감아치기 (지도 직후)" / "극단적 방어 (체력 열세)" / "오른소매 노림" / "왼소매 노림", 단계 문구 "심증 (N회)"/"확정 (N회)"/"폐기됨", 빈 상태 "아직 관측된 습관 없음". 내 성향 라벨: "경고선 — 최근 6수 중 4수 이상 같음" / "위험선 — 3연속 완전동일"
Play 로 시나리오를 실제로 굴려보며 매핑을 다듬어라. 매핑은 title/content 만 읽고 정한 초안이라, 클립이 상황에 맞는지는 사람 눈으로 봐야 안다 — 특히 2026-08-07 에 AI 클립으로 교체한 19개 노드는 아직 Play 확인 전이다
(Separate, not urgent) Finish the Judo match-1 scenario on the scenario board — three things the user still needs to answer: how the right-sleeve grip exchange actually plays out / timing of the collar-grip transition / the tells that distinguish an ippon-seoi-nage attempt from an ouchi-gari attempt

알려진 공백 (인지하고 남겨둔 것 — 버그 아님)
n10 허리후리기 = ai_osotogari_entry 임시 대체. 기술 상이(하라이고시 ≠ 오소토가리), 모캡 대상
n27/n29/n39/n40 = NodeMotionMap 미등록

Resolved Questions (2026-08-03)
Why Assets/Editor/HubForceResolve.cs disappeared — nobody deleted it. It is Unity Hub's bootstrap script that calls Client.Resolve() and then removes itself and the empty Assets/Editor folder via AssetDatabase.DeleteAsset. The three deleted files match CleanupEmptyEditorFolder() exactly. Not a real incident, not resume material.
Whether to keep the Unity Cloud link (cloudProjectId / organizationId in ProjectSettings.asset) — keep and commit. cloudEnabled is 0, it is a project GUID rather than a secret, ProjectSettings.asset is tracked anyway, and the editor rewrites it if removed.

Session Log (newest first)
2026-08-12
작업0 — 세션 시작 확인: `md/HANDOFF_session_2026-08-11.md`→MATCH_RULES→CORE_DESIGN→
OPPONENT_BRAIN→STATUS 순으로 읽음. 스탬프 3종 `tools/stamp.sh --check` exit 0,
`git config core.hooksPath`=tools/githooks 확인. 하니스 209/209 그린+지문 IDENTICAL,
Unity MCP 컴파일 0/0 — 전부 통과해 작업 중단 없이 진행.
작업1 — `feat/match-engine` 을 origin 에 그대로 푸시(9커밋, force 없음). 푸시는 성공했으나
`git status` 가 clean 하지 않음을 발견 — 위 "결정 대기" 항목 참조(이번 세션이 만든 변경
아님, 그대로 보고).
작업2 — 경기 화면 저해상도 렌더 v1(`ArenaStageView` 신규). 상세는 위 Next Up "가장 최근"
항목 참조. 커밋 3233a13.
작업3 — `docs/play-checklist-2026-08-12.md` 신설 — `md/HANDOFF_session_2026-08-11.md`
§5.1/§5.2/§6.2 를 체크박스로 옮겨 정리만 함(새 내용 없음). 커밋 3080078.
작업4 — 이 STATUS 갱신이 그 결과물.
후속 지시(작업4 이후) — 위 "결정 대기"(HANDOFF 2종 삭제)를 사용자가 확정: 두 파일 삭제
커밋 + `HANDOFF_session_2026-08-11.md` §0 의 전제 문장 최소 수정, 이번 세션 미푸시 커밋
전부 origin 에 푸시(force 없음).
2026-08-12 (5차 — 실전 UI 개편: 손·코어·하체 캐릭터 옆 드롭다운)
사용자 지시(레이아웃 변경): 점수판 화면 중앙 하단, 확정 버튼 그 바로 위(3축 전부
선택 전 비활성), 손·코어·하체 상시 3열 폐지 → 캐릭터 옆 트리거+드롭다운. 구현
전 3건(배치 기준/추적 방식/드롭다운 방향) 실측·근거와 함께 보고만 하고 사용자
확정 받은 뒤 진행 — AskUserQuestion 으로 각각 미리보기 포함 선택지 제시.
확정 결과: (a) 주체 기준(전부 A 옆) (b) 화면 고정 위치(본 추적 기각, 12fps 스텝
지터 최대 17.5px 실측 근거) (c) 위로 펼침(코어·하체는 상대와 179px 여백 두고
안 겹침 확인, 손은 최대 10개 옵션이라 화면 밖으로 넘칠 수 있는 한계는 코드
주석에만 남기고 안 고침).
구현: BuildCanvas 순서 재배치(MiddleRow→OsaekomiRow→ConfirmRow→ScoreboardRow),
BuildStandingRoot/BuildColumnRoot 를 BuildFloatingControls/BuildAxisControl 로
교체, 확정 게이트 bool 3개 추가, 드롭다운 단일 열림 토글.
Play 모드 실측 검증 중 회귀 검사가 NarrationLog(leftColumn 전체를 채우고
있던 것 — 지난 세션 결과, 이번 세션 아님)와 새 트리거 버튼의 실제 겹침을
잡아냄. "서술 로그 무수정" 원칙에 걸렸지만 AskUserQuestion 으로 사용자에게
다시 물어 "이번에 한해 예외 허용" 확정 받고 NarrationLog 를 트리거 밴드
위쪽(고정 280px)으로 앵커 재배치 — 닫힌 상태 겹침 0 달성, 드롭다운 열린
상태의 잔여 겹침(손 최대 221px)은 팝오버 성격으로 판단해 허용, 실측값과
함께 STATUS 에 기록.
겪은 테스트 방법론 함정: Unity Destroy() 프레임 지연으로 같은 프레임 안에서
연속 클릭하면 드롭다운 자식이 일시적으로 배로 보임(예: childCount=6, 실제는
3) — 이 코드베이스에 이미 기록된 현상, 프레임을 나눠 재측정해 실제로는
정상임을 확인. GameObject.Find 는 비활성 오브젝트를 못 찾는다는 것도 재확인
(OsaekomiRow 등 SetActive(false) 대상은 계층 재귀 탐색으로 확인).
7개 해상도 닫힌 상태 전부 zeroSize=0·겹침=0·줄바꿈=0, 드롭다운 열린 상태(대표
해상도 1920)는 위 NarrationLog 예상 겹침만 확인. 하니스 209/209, 지문
IDENTICAL. 커밋 f84f631.
2026-08-12 (4차 — 전체화면 배경 후속: 사이드바 진단 + 카메라 이동)
사용자 신고 2건: ① 사이드바 본문이 캐릭터와 겹쳐 안 읽힘, 배경이 거의 투명해 보임
② 전체화면 전환 후 B가 우측 사이드바 뒤로 완전히 가려짐. "추측 수정 금지" 지시대로
①부터 실측 진단 — Image color/alpha/sibling순서/텍스트 오버플로/CanvasGroup 전부
확인했지만 구조적 결함을 못 찾음(사이드바는 이미 다른 패널과 같은 PanelBg 처리가
돼 있었다). 코드 변경 없이 "이미 통일돼 있다"고 보고, 대신 ③(카메라 이동)이 B를
사이드바에서 멀리 옮기면서 간접 완화될 걸로 예상된다고 STATUS/보고에 명시.
②는 실측으로 풀렸다: 결합 RT-x 폭(169px)이 사이드바 안전폭(~208px)보다 좁아서
확대·축소 없이 카메라를 옆으로(x=0→0.6) 이동하는 것만으로 해결 — 세로 100px
규격은 수평 이동과 무관해 그대로 유지됨을 재확인(A 102.1px/B 100.9px).
4:3(1024x768)에서는 이 값으로 안 풀린다는 것도 실측으로 확인해 보고만 하고 코드로
안 고침(EnvelopeParent 크롭이 종횡비마다 달라 정적 카메라 하나로 전부 못 만족) —
사용자가 지시한 "임의로 어느 쪽도 희생하지 말고 사용자 판단으로 넘긴다" 원칙 그대로
따름.
5번(별건 확인) — HANDOFF 2종은 지난 세션(커밋 5f65192)에서 사용자가 이미 명시적으로
삭제를 확정·지시해 처리된 상태임을 git log 로 재확인, 복원 불필요라고 보고.
7개 해상도 재실행 — zeroSize=0·겹침=0·줄바꿈=0(4:3 포함 구조적 검사는 전부 통과,
캐릭터 프레이밍만 별개). 하니스 209/209, 지문 IDENTICAL. 커밋 74ad48b.
2026-08-12 (3차 — 경기 화면을 창 전체 배경으로)
사용자 신고: 씬 스카이박스와 좌상 박스 안 RT 가 배경 두 겹으로 보여 혼잡. RawImage 를
좌상 `ArenaBox`에서 캔버스 최하단 sibling(`ArenaBackground`, 전체 stretch)으로 옮기고
`ArenaBox`/`RectMask2D` 를 통째로 제거(더 이상 침범할 이웃이 없어 불필요해짐 확인 후
제거), `EnvelopeParent`는 유지(16:9 아닌 창에서 안 찌그러지려면 여전히 필요). 비워진
좌상 자리는 `NarrationLog`가 이어받음(호출 한 줄 제거로 자연히 확장).
점수판·하단 입력·서술 로그 3곳에 `PanelBg`(기존 사이드바 색 재사용) 스크림 추가 —
알파값은 스크린샷 없이 정할 수 없어 하나 골랐다고 코드 주석·STATUS 양쪽에 명시.
카메라는 "RT 안 캐릭터 100px" 규격이 표시 배율과 무관한 RT 공간 값이라 무수정 확인
(재실측 A 96.1px/B 95.4px, 애니메이션 자연 변동 범위).
겹침 검사 방법론 갱신: 의도된 전체화면 배경(`ArenaBackground`)은 이름으로 제외하고
나머지끼리만 검사 — "덮는 게 목적인 배경과의 겹침"과 "실수로 겹친 두 패널"은 다른
범주라는 걸 지난 세션의 RectMask2D-clip 처리에 이어 다시 확인.
7개 해상도 재실행 — zeroSize=0·겹침=0·줄바꿈=0 전 구간, 배경 크롭 실측: 정확히 16:9인
1920x1080/1280x720/960x540/320x180 은 크롭 0%, ≈16:9인 719x404/600x338 은 0.1~0.15%
(무시 가능), 4:3인 1024x768 만 가로 25%(양쪽 12.5%) 크롭 — 설계대로. 하니스 209/209,
지문 IDENTICAL. 커밋 dd56bab.
2026-08-12 (2차 — 경기 화면 UI 도트화 진단·수정 + 캐릭터 확대)
사용자가 Play 실측으로 2건 신고: UI 텍스트 도트화, 캐릭터가 너무 작음. "추측 수정 금지,
진단 먼저" 지시대로 캔버스 renderMode/worldCamera/scaleFactor, ArenaStageCamera
cullingMask/targetTexture 를 먼저 실측 — Canvas 는 ScreenSpaceOverlay/worldCamera=null 로
구조적으로 카메라와 무관함을 확인, 대신 Game View 커스텀 렌더 해상도가 지난 세션의
719x404 로 남아 있어 CanvasScaler scaleFactor=0.374 로 낮아진 게 진짜 원인임을 확인
(1920 리셋 즉시 scaleFactor=1). `PlayModeSetupMenu` 에 해상도 리셋을 추가해 재발 방지.
카메라 프레이밍은 z=-6→-4.25 + 하향 11° 로 재실측(A 100.9px/B 99.6px, 클리핑 없음).
RawImage AspectRatioFitter 를 FitInParent→EnvelopeParent 로 바꾸고 ArenaBox 에
RectMask2D 추가, flexibleHeight 1.2→4.5 로 박스 확대(173→260px). 상세는 위 Next Up
"가장 최근" 항목 참조 — 무크롭·무바 동시 달성이 이 5블록 구조에서 기하학적으로 불가능함을
실측으로 확인하고 EnvelopeParent(캐릭터 소폭 크롭)를 판단해서 골랐다고 코드 주석·STATUS
둘 다에 남겼다.
검증 중 발견한 방법론 함정 2개, 기록: (1) 좌표 겹침 검사에 `RectMask2D` 로 잘리는 오버사이즈
RawImage 를 그대로 넣으면 raw RectTransform 크기가 잡혀 점수판/서술 블록과 대량의 가짜
겹침이 나온다(22건) — 마스크를 인지해 클립한 rect 로 다시 계산하니 0건. (2) `result.Log`
가 `{0:F1}` 같은 `.NET` 서식 지정자를 그대로 문자열로 찍어버린다(치환 안 됨) — 서식 없이
문자열 접합으로 우회.
7개 해상도(1920/1280/1024/960/719/600/320) 재실행 — zeroSize=0·겹침=0·줄바꿈=0, 매
해상도에서 scaleFactor 가 화면폭/1920 그대로 정상 추적(더는 고정 안 됨). 하니스 209/209,
지문 IDENTICAL. 커밋 3bc6acd.
2026-08-11 (오후, 7차 — 좁은 화면 레이아웃 붕괴 수정)
사용자 지시: 719px 스크린샷 신고("추측 수정 금지, 원인 진단 먼저") + 4개 해상도 재검증 + 최소 지원 해상도 명기.
진단 과정: CanvasScaler 라이브 값 확인(match=0, Unity 기본값) → 공식으로 "겉보기 폭은 실제 화면폭과 무관하게 항상 1920"임을 도출 → `PlayModeWindow.SetCustomRenderingResolution`(공식 API, reflection 아님 — `System.Reflection` 은 RunCommand 샌드박스에서 막혀 있어 처음엔 GameView 리사이즈로 우회하려다 발견)로 실제 719×404 를 재현해 `RectTransform.rect`/`LayoutElement` 필드를 직접 대조 → `ScoreboardRow`/`BottomInputRow`/버튼/여러 텍스트가 폭 0으로 무너져 있는 것을 확인 → 720p 로 되돌려도 동일하게 0(해상도 무관, 구조적 버그)임을 대조 실측으로 확정. 근본 원인: 교차축에 `preferred/min/flexible` 셋 다 안 준 `LayoutElement` 는 Unity 가 무조건 0으로 접는다 — 지난 세션의 겹침 검사가 이 사각지대(폭 0 요소는 애초에 안 겹친다)를 못 잡았다.
수정: `SetFixedHeight`/`MakeButton` 기본값에 `flexibleWidth=1` 추가, `MakeFlexText`/`SetFlexible` 교차축 인자 전부 0→1, `MakeFlexText` 는 `preferredHeight=minHeight` 도 같이 준다(min 단독 충분성은 실측 안 함 — 확인된 조합만 씀). `MakeText` 에 TMP auto-size 상시 적용 + 버튼 라벨은 줄바꿈 금지(`textWrappingMode=NoWrap`, 구 API `enableWordWrapping` 은 obsolete 경고가 나서 신 API 로 씀).
검증: 1920×1080/1280×720/1024×768/719×404 네 해상도 + 여분으로 600×338/320×180 까지 총 6개 — 63개 요소·zeroSize=0·쌍별 겹침=0·버튼 라벨 줄바꿈=0 전 구간 동일. 719px 에서 실제 클릭(손/코어/하체+확정)까지 재확인. 최소 지원 폭은 "구조적으로는 하한 없음(320px 도 안 깨짐), 가독성 기준 권장 960px"로 STATUS 에 명기 — 후자는 폰트 크기 계산 추정이지 육안 확인이 아니라고 밝혀둠(스크린샷 캡처가 이 MCP 환경에서 신뢰 안 된다는 기존 troubleshooting 문서와 같은 이유로 생략, 좌표·폰트 크기 실측으로 대체).
Unity MCP 컴파일 0/0(obsolete 경고 1개 있었다가 API 교체로 제거, 최종 0/0). 하니스 209/209, 지문 IDENTICAL(LiveMatchPanel.cs/LiveMatchRunner.cs 는 하니스 컴파일 대상이 아님 — 재확인).
2026-08-11 (오후, 6차 — 화면 전면 재설계)
사용자 지시: 현재 레이아웃을 버리고 상단(점수판)/좌상(경기 화면 자리)/좌하(서술+로그)/우측(사이드바 3종: 코치의 조언/수 노트/내 성향)/하단(입력) 5블록 구조로. 절대 표시 금지 재확인(심판 누산기/B 선택지·의도/판정식 합계/A 가 관측 안 한 B 습관). 겹침 재발 방지 조건 명시(좌표를 손으로 안 찍고 영역 비율·항목 수에서 계산).
계획 단계에서 3가지를 스스로 판단하고 명시했다(사용자 재확인 없이 진행, 전부 표시 정보의 취지를 안 바꾸는 배치 판단이라 판단): 기존 위상/체력/그립/손 잠김 상태 HUD 는 스펙에 자리가 명시 안 돼 있었다 — 좌하 "상태 줄"로 흡수(문자열 조립 로직 무수정). 코치 조언은 자기 노출 위험선을 다루지 않는다(3번 사이드바와 중복 방지). `_preemptedText` 별도 배너는 정보 손실 없이 제거(서술 줄과 100% 동일 문자열의 3중 표시 중 하나 정리).
구현: `LiveMatchRunner`에 `Ledger`/`OpponentHabits` 프로퍼티 2줄 추가. `LiveMatchPanel.BuildCanvas` 전면 재작성 — `VerticalLayoutGroup`/`HorizontalLayoutGroup` 중첩으로 5블록 + 사이드바 3박스 + 입력 3열을 전부 계산 배치(anchorX 상수 완전 제거). `MakeVerticalGroup`/`MakeHorizontalGroup`/`SetFlexible`/`SetFixedHeight`/`SetFixedWidth`/`MakeFlexText`/`MakeSidebarBox` 헬퍼 신설. 기존 판정 서술·로그·노출막대 임계값·점수판·굳히기 2지선다 로직은 전부 무수정(자리만 이동). Core/Brain 무수정.
검증 상세는 위 Next Up 항목 참조. 하니스 대상 아님(확인만, 재실행 없음) — Unity MCP 컴파일 0/0 + Play 모드 30교환 실측(좌표 겹침 0 두 상태, 코치조언/수노트/내성향 값 대조)으로 대체.
2026-08-11 (오후, 5차 — 쿠즈시 → 흔들기/Setup 개명)
사유: 쿠즈시는 유도 전용 어휘라 타 종목(복싱·킥복싱·MMA) 확장 시 안 맞는다. 규칙·동작은 일절 무변경, 이름만. 화면 표시는 "흔들기", 코드 식별자는 `Setup*`.
전수 조사 먼저 — grep 으로 24개 파일 확인 후 목록을 보고하고 진행. 주의 사항 셋 다 확인 완료: (1) Feint/페인트/위장공격(§6.1)과 텍스트·의미 둘 다 안 겹침 (2) 예비동작(tell 모캡 클립 이름)과 무관, 재사용 안 함 (3) 이름 기반 직렬화 — `.unity`/`.asset`/`.prefab` 전체를 grep 해서 저장된 참조가 0 개임을 확인, 유일한 이름 기반 조회는 `PlayModeSetupMenu.cs` 의 `SerializedProperty.FindProperty("kuzushiEnabled")` 뿐이라 필드 개명과 같이 고침. 지문(`fingerprint-baseline.txt`)도 grep 으로 "쿠즈시"/"Kuzushi" 0 개 확인 — 42노드 대조 시나리오가 플래그 기본 off 라 애초에 이 경로를 안 밟는다.
바꾼 것: 파일 3개 git mv(.meta 동반, GUID 보존) — `KuzushiPrecedingCommitSource.cs`→`SetupPrecedingCommitSource.cs`, 그 테스트, `Core/Tests/KuzushiTests.cs`→`SetupTests.cs`. 식별자 전부(`HandTarget.Kuzushi`→`Setup`, `KuzushiGateEnabled`→`SetupGateEnabled`, `KuzushiCost`→`SetupCost`, `PayKuzushi`→`PaySetup`, `KuzushiDeclared`→`SetupDeclared`, `IsKuzushiActive`→`IsSetupActive`, 사설 필드 2개, 테스트 메서드 12개). 화면 표시 3곳(손 옵션 라벨, 교환 로그 "흔들기 설치", HUD "흔들기 유효/없음"). MATCH_RULES.md §2/§4.4 갱신 + v3.7→v3.8(스탬프 해시는 pre-commit 훅이 자동 갱신). 남은 "쿠즈시" 0개(Assets 전체 grep 확인) — docs/overnight-*·decisions/·daily/ 는 과거 기록이라 그대로 뒀고, 이 파일엔 위 대응표 한 줄만 추가.
검증: Unity MCP 컴파일 0/0(파일 리네임 후 재컴파일 확인), dotnet 하니스 209/209, 지문 IDENTICAL(원래 예상대로 — 플래그가 기본 off 라 대조 경로는 흔들기를 아예 안 지나간다).
2026-08-11 (오후, 4차 — 입력 열 텍스트 겹침 수정)
사용자가 Play 스크린샷으로 신고: 손 열(쿠즈시 포함)과 코어/하체 열 텍스트가 겹쳐 안 읽힌다. "추측 수정 금지" 지시대로 Play 모드에서 실제 버튼을 눌러(같은 프레임 안에서 연달아 클릭하면 `RebuildColumn` 의 `Destroy()` 가 그 프레임엔 안 지워져 자식이 70개까지 쌓이는 걸 먼저 발견 — 실제 사용자는 클릭 사이에 항상 프레임이 끼니까 안 겪는 테스트 아티팩트였다. MCP 호출을 클릭 1개당 1개로 쪼개 프레임 경계를 실제로 두는 방법으로 우회) 손 열 10개 항목(쿠즈시 포함, 실제로 도달)의 `RectTransform.GetWorldCorners` 를 찍었다.
가설 3개 검증 결과: (1) 손 열 행 높이가 항목 수 반영 안 하는가 — **아니다.** `VerticalLayoutGroup` 이 이미 10개를 285.3px 안에 23.4px 씩 자동 축소하고 있었고 TMP 필요 높이(21.6px)도 그 안에 들어가 겹침 없음(OVERFLOWS=False 10/10). (2) 하이라이트 행이 행 간격보다 큰가 — 해당 없음, 애초에 안 겹친다. (3) 점수판 재배치가 원인인가 — **아니다,** git log 로 `StandingInput.sizeDelta.y=320` 이 생성 후 한 번도 안 바뀌었음을 확인.
진짜 원인은 별개: `BuildStandingRoot` 세 열의 anchorX 중심(0.06/0.38/0.62)이 열 반폭 0.14 기준으로 원래부터 비대칭이었다(간격 0.32/0.24, 둘 다 필요한 0.28에 못 미치거나 겨우 넘음) — 코어-하체 간격이 −0.04(실측 −76px)라 두 열의 버튼 배경이 상시 겹쳐 있었다. 이번 세션이 만든 버그가 아니라 6단계 최초 구현부터 있던 것 — 쿠즈시로 손 열이 붐벼 보이면서 사용자가 처음 알아챈 것뿐.
수정: 하체 열 중심만 0.62→0.68 (코어·손·확정 버튼 무수정). 재검증은 이전과 같은 좌표 실측 방식으로 — 손10+코어3+하체4+확정1=18개 버튼 45쌍 전부 계산으로 0 겹침 확인(간격 손-코어 76.1px, 코어-하체 38.0px, 하체-확정 72.9px). 하니스 209/209, 지문 IDENTICAL.
2026-08-11 (오후, 3차 — 골든스코어 도달 불가 수정)
사실 확인부터: MATCH_RULES §1 원문("Win resolution at 4:00: ippon > waza-ari count > yuko count > golden score", "Time-up is checked AFTER an exchange fully resolves", "Golden score: unlimited time, first score of any kind wins immediately; third shido loses") 과 CORE_DESIGN 의 "MatchClock 이 RegulationExpired 를 내면 엔진이 GoldenScore 로 넘기거나 승자를 정한다"를 확인. grep 으로 `MatchEngine.cs` 안에 `RegulationExpired`/`WinnerAtTimeUp`/`EnterGoldenScore` 호출이 정말 0개임을 재확인 — 둘 다 그 자체 로직은 격리 테스트로 이미 맞았고(`MatchClockTests`/`ScoreBoardTests`), 빠진 건 순수히 "누가 부르느냐"였다.
구현: `ScoreBoard.FinishAtTimeUp()`(신규, 기존 공개 API 두 개만 조합) + `MatchEngine.ResolveExchange` 끝에 조건부 호출 + `GoldenScoreEntered` 이벤트 1개 + `LiveMatchPanel.EventSummary` 한 줄. 의도적으로 규칙을 새로 안 만들고, 애매한 한 갈래(정규시간 만료 순간에 막 열린 굳히기)는 구현하지 않고 테스트로 현재 동작을 사실로만 고정했다.
검증: `GoldenScoreTests.cs` 7개 신규(결정된 시간종료 / 다 비긴 시간종료→골든스코어+시계 무제한 / 골든스코어 중 즉시종료 판정·지도 3개 둘 다 풀 엔진 경로로 / 틀린 답 2개). 202→209 그린, 지문 IDENTICAL. 스모크 6구성(반복/변주/현실적×on/off) 전부 다시 봤다 — 가장 오래 도는 것도 최대 추정 165초로 240초 근처도 안 가서 회귀망이 이 경로를 하나도 안 밟는다는 사실을 기록. Unity MCP 컴파일 0/0, `ScoreBoard.FinishAtTimeUp()` 직접 참조로 재컴파일 확인.
2026-08-11 (오후, 2차 — 실전 점수판 재배치)
사용자 지시: 신설이 아니라 재배치. 시계·절반·유효·지도·누르기 카운트·골든스코어만 사진 6장 레퍼런스(특히 마지막 사진 — -63kg/4강전, 골든스코어 표시본) 규격의 점수판으로 옮기고, 잡기·체력·위상·쿠즈시·교환 로그·선택 확인은 기존 구현을 그대로 점수판 아래로 재배치. `LiveMatchPanel` 내부 좌표만 다시 잡았고 이벤트 배선·요약 로직(`BeginPending`/`BuildOutcome`/`EventSummary`/`PushLog`)은 무수정.
점수판 구조: 큰 타이머(위) + 골든스코어 라벨(타이머 아래, 금색, `Board.InGoldenScore` 읽음) + IPPON 세로 스트립(한판 나면 강조) + A(흰 배경)/B(파란 배경) 두 행, 각 행에 W(절반)/Y(유효)/S·H(지도 카드 아이콘 — 1·2개 노란, 3개째 빨강 하나로 전환)/누르기 카운트(2자리, `Osaekomi.HeldSeconds`를 그 교환의 홀더 행에만 표시). 국제룰 5/10/20초 경계는 기존 `MatchRulesConfig.OsaekomiYukoSec/WazaAriSec/IpponSec` 기본값과 이미 일치 — 새 상수 없음.
Play 모드 실측 검증(사용자 요청 6번, 이번엔 실제로 함): `CounterTime > Play Mode Setup` 메뉴를 `EditorApplication.ExecuteMenuItem` 로 실행 → `EditorApplication.isPlaying = true` → 다음 MCP 호출에서 씬이 이미 Play 상태로 로드되고 `LiveMatchPanel.Awake()` 가 이미 캔버스를 지은 것을 확인 → `Button.onClick.Invoke()` 로 A 의 손/코어/하체 선택 + 확정을 2턴 실제로 실행 → 화면에 뜬 각 TMP_Text.text 를 엔진 게터(`eng.Clock/.Board/.Grip/.Stamina/.Osaekomi`)로 독립 재계산한 기댓값과 문자열째 비교. 타이머 3회, 양쪽 체력·그립·양손 잠김상태 라벨 3회 전부 MATCH=True(00:00 형식까지 정확히 일치). 로그는 스크래치패드 `play_verification.log` 에 저장 + 세션에 전문 출력. 이번 2턴에서는 득점·지도·골든스코어가 실제로 안 났으므로 그 칸들의 렌더링은 "0 값이 맞게 나온다"까지만 실측했다 — 비제로 값 렌더는 코드 경로 동일성으로 신뢰하되 육안 미확인.
검증 중 발견(수정 안 함, 기록만): (1) 같은 프레임 안에서 손→코어→하체를 연달아 클릭하면(자동화 테스트라 프레임 경계 없이 동기 실행됨) `RebuildColumn` 의 `Destroy()` 가 그 프레임 끝까지 지연돼 손 옵션 목록에 중복이 쌓인다. 실제 사람이 클릭할 때는 클릭 사이에 항상 프레임이 끼어서 발생 안 함 — 테스트 방법론의 산물이지 제품 결함 아님, 안 고쳤다. (2) 골든스코어 진입 코드가 어디에도 없다는 것을 grep 으로 재확인(Next Up 참조) — HUD 는 배선해뒀지만 지금은 절대 안 뜬다.
컴파일: Unity MCP AssetDatabase.Refresh 후 0/0, `LiveMatchRunner.Build()`+`LiveMatchPanel` 인스턴스로 `Scoreboard`/`RowA`/`RowB` 계층이 실제로 생성됨을 참조 확인. dotnet 하니스 202/202 그린, 지문 IDENTICAL — 전부 프레젠테이션 레이어라 엔진 무변화.
2026-08-11 (오후, 1차)
LiveMatchPanel 상태 표시 추가 — 클릭은 이미 정상(6단계 배선 성공)이었지만 화면에 현재 상황이 안 보여서 뭘 고르는지 판단할 수 없다는 사용자 지적으로 시작. 상단 HUD(시계, 위상[서기/굳히기+누른시간/이탈수], 점수판 A·B, 잡기[유효값+양손 놓임/잡힘/잠김], 체력, 쿠즈시 게이트 켜졌을 때만 유효/없음), 최근 5교환 로그(내 선택 → 결과, 폴백 서술은 회색 유지), 확정 전 자기 선택 한 줄 확인을 추가했다. 심판 누산기·B 선택지/의도·판정식 합계는 그대로 노출 안 함(설계 확정 재확인).
구현: `MatchEngine.IsKuzushiActive(FighterId)` 추가(Core, `_kuzushiActive` 읽기 전용 미러) — Core 판정 로직 자체는 무수정. `LiveMatchRunner.Config` 추가(Game, `_cfg` 노출 — 쿠즈시 게이트 on/off 판단용). `LiveMatchPanel` 은 `Presented` 이벤트를 새로 구독해 한 번의 Submit* 호출 동안 발생한 이벤트를 모으고(`BeginPending`/`_pendingEvents`), 판정 서술이 있으면 그걸, 없으면 이벤트 목록에서 뜻 있는 첫 번째를 요약(`EventSummary`)해 로그 한 줄로 만든다. 굳히기 2지선다 버튼도 같은 경로(`SubmitOsaekomiChoice`)로 옮겨 로그·HUD 가 굳히기 턴에도 갱신되게 했다.
검증: Unity MCP로 컴파일 확인 — AssetDatabase.Refresh 후 0 에러/0 경고, 추가로 `IsKuzushiActive`/`Config` 를 직접 참조하는 RunCommand 스크립트가 컴파일된다는 사실로 두 어셈블리가 실제로 재컴파일됐음을 확인(단순 GetConsoleLogs 만으론 컴파일이 스킵됐을 가능성을 못 배제하기 때문). dotnet 하니스 202/202 그린, 지문 IDENTICAL — 이번 변경은 전부 프레젠테이션 레이어 읽기라 엔진 동작에 영향 없음을 재확인.
**Play 모드 실측(사용자 요청 6번)은 아직 안 했다** — 화면에 실제로 값이 맞게 뜨는지는 사람이 Play 를 눌러 봐야 한다. 새 v1 임시 문구들은 STATUS 카피 교체 목록에 등록.
2026-08-11 (아침)
어젯밤 판단 대기 5건 브리핑(원문 출력 + m18 실측: play 모드 브레인 경로에서 실제 발화하는 값이 등록된 키와 일치함을 코드로 확인) + Play 모드 2클릭 준비 메뉴(CounterTime > Play Mode Setup) 신설 (커밋 7325102). 이 과정에서 LiveMatchPanel 의 런타임 생성 캔버스에 GraphicRaycaster 가 빠져 있어 버튼이 하나도 안 눌리는 결함을 발견 — 처음엔 에디터 우회로 막았다.
브리핑을 본 사람이 5개 결정: m18 키 등록 해제(문구가 횟수를 명시해 오정보) / 습관 관측 알림 호출 시점을 결정 시점에서 실제 제출 시점으로 이동(사다리·카탈로그 무수정) / GraphicRaycaster 를 BuildCanvas 에 정식 추가하고 에디터 우회 제거 / MATCH_RULES §2 에 뜯기·잡기 유지 게이트 문서화 + 동시 공격 우선순위 "v1 은 B 선행, 미결" 명시 / "B 선행 — A 공격 불발" UI 표시 추가. 전부 반영 (커밋 54bd5be, e01d960).
세션 시작 전부터 미커밋이던 폰트(NotoSansKR SDF.asset)·ProjectSettings.asset 도 diff 로 섞인 변경 없음을 확인하고 사유 명시해 커밋.
200 → 202 harness green, fingerprint IDENTICAL 유지. Unity 에디터 0 에러/0 경고. 상세: docs/daily/2026-08-11-morning.md, docs/decisions/habit-observation-call-site-ownership.md, docs/troubleshooting/livematch-canvas-missing-graphicraycaster.md
2026-08-11 (밤, 2차 야간 자율 실행, unattended)
작업2~6 완료 (커밋 6bd5a06..a9614cb) — 서술 사전 9키 등록, 쿠즈시 §4.4 [초안] Core 규칙(플래그 기본 off), B 브레인 쿠즈시 선행 데코레이터, 6단계 LiveAssembler+입력 UI 스캐폴딩, off/on 스모크 실측 6회. 전체 요약은 위 "야간 자율 실행 2차" 항목, 상세는 docs/overnight-2026-08-11.md.
200/200 harness green, fingerprint IDENTICAL 전 구간 유지. Unity 에디터 0 에러/0 경고 매 작업 후 재확인(MCP 연결 밤새 유지).
판단 대기 5개 남김(문서 맨 위) — 손대지 않고 전부 문서화. 가장 중요한 것: LiveMatchRunner/LiveMatchPanel 은 Play 모드에서 아직 한 번도 실행 확인이 안 됐다 — 화면·클릭·레이아웃은 전부 사람이 아침에 볼 몫이다.
작은 자기 교정 1건: MATCH_RULES 첫 커밋(2e161b4)에서 버전 태그를 v3.6 에 두고 커밋했다가(스탬프 해시는 자동 갱신되지만 vX.Y 숫자는 사람 몫이라는 걸 놓침) 다음 커밋(b9618b2)에서 바로 v3.7 로 고쳤다.
2026-08-11
§9 osaekomi guard landed (a165872) then its fallout fixed (a036e75) — see "§9 guard + full green recovery" above for the full story. Also: dotnet-harness committed to tools/dotnet-harness (was scratchpad-only, rebuilt every session, per the old STATUS line this replaces); overnight's fingerprint IDENTICAL claims independently re-verified from a clean worktree rebuild, not just self-comparison; MATCH_RULES stamp confirmed current (v3.5, tools/stamp.sh --check, exit 0); this file updated.
Asked to update HANDOFF_session_2026-08-10.md's "현재 값" stamp block — that file does not exist anywhere in this repo's history (checked git log --all). Only md/HANDOFF_claude_code_runner.md and md/HANDOFF_claudecode_opponent-brain.md exist, and neither has a "현재 값" stamp block. Not touched; flagged instead of guessed.
2026-08-10 (overnight, unattended)
Step 4 (4b narration infra) and step 5 (osaekomi §9) landed, gated the same way stages 1–3 were: fingerprint diff + full test run after every change, revert on mismatch. See docs/overnight-2026-08-10.md for full detail and the open judgment call.
Tried adding TechniqueId directly to JudgementScored first — compiled, tests passed, but the fingerprint diff came back non-identical (the new field appears on every dump line), so it was reverted in favor of a Game-only pairing that leaves Core untouched. The revert-on-mismatch protocol did exactly what it was for.
Osaekomi §9 implementation surfaced one real interpretation gap in the rules doc (whose grip counts for a countered score's entry gate) — resolved it for waza-ari (no other option, table says it always enters) and deliberately left countered-yuko never entering osaekomi rather than guess. Flagged in code and in the overnight doc for a human decision.
Unity Editor + MCP bridge stayed connected the whole night with nobody at the machine — used it after every change to confirm 0 compile errors/warnings in the real editor, on top of the dotnet harness. Test Runner execution itself is still blocked over MCP (known, unchanged).
164 → 178 tests. Match length shifted after osaekomi landed (28 exchanges/156s → 26 exchanges/162s on the same realistic-A smoke profile) — recorded, not tuned; judged by a human after stage 6 per existing policy.
2026-08-10
Opponent brain stages 1–3. Isolated the scripted path behind interfaces, extracted the assembler, built habit data + observation ledger, then the live read resolver and the habit-driven commit source. 130 → 164 tests.
Every stage was gated on a fingerprint diff of the real MatchRunner over the mainline, and it stayed IDENTICAL throughout — including across a Core rule change. That is the whole reason 대조 모드 survived a rewrite of what feeds it.
Measurement drove three rule changes that no amount of reading would have found: habits fired 1–4 times a match and ladder rows 1–3 fired ZERO (added H4 잡기 노림), half of all judgements came from the fallback (fallback pokes only), and stamina bottomed out at ~30 exchanges because nothing called matte (MATCH_RULES v3.4 §4.3).
Two of my diagnoses were wrong and the instrumentation caught both before I "fixed" anything: row 2 was scoring normally, and retirement WAS happening (H4b firing is the proof). Dump before diagnosing.
2026-08-07
AI anim 4 clips generated (5cr each, 20cr total), mapped to 19 nodes. n10 = placeholder, mocap needed.
클립: ai_seoinage_entry / ai_osotogari_entry / ai_ukemi_back / ai_ground_pin. A·B 컨트롤러 양쪽에 State 로 들어가 있다.
NodeMotionMap 19개 노드 · 25개 State 를 AI 클립으로 교체. 던지는 쪽/당하는 쪽을 A·B 로 나눠 지정했다 — n41 은 B 가 누르는 쪽이라 A/B 가 반대다.
n10(허리후리기)은 ai_osotogari_entry 임시 대체다. 하라이고시 ≠ 오소토가리 — 모캡 대상.
n18/n20/n26/n28 은 종료 연출(Victory Idle / Dying / Defeated) 그대로 뒀다. n27/n29/n39/n40 은 아직 매핑 자체가 없다.
문서 작업 중 발견해서 고친 것: AI 클립 4종이 전부 Loop ON 이고 8개 State 에 나가는 트랜지션이 없었다 (생성 도구 기본값). 08-05 컨벤션대로 원샷 3종(seoinage/osotogari/ukemi)만 Loop OFF + 6개 State 에 Fight Idle 복귀 트랜지션(exitTime 0.9 / 고정 0.25초) 추가, ai_ground_pin 은 유지 동작이라 Loop ON·트랜지션 없음 유지. MCP 로 처리했고 A 컨트롤러 트랜지션 9개 전부 같은 파라미터·같은 목적지(Fight Idle)로 확인. Play 검증은 아직.

2026-08-05 (5)
노드↔모션 연결 구현. 43개 노드의 title/content 를 읽고 36개 매핑을 직접 채웠다 (A=파랑 주체, B=빨강 관점 구분). 10개 상태 전부 최소 한 번은 쓰인다.
Play 실측: 대표 11개 노드에서 CrossFade 대상이 매핑과 100% 일치, 원샷 자동 복귀(n9 +2.0s → Fight Idle), 유지 상태 지속(n14 +7.9s 에도 Victory Idle / Dying) 확인. 포즈 캡처로도 확인 — n14 는 A 서 있고 B 엎어짐, n41 은 정확히 반대.
함정 두 개 더: (1) MoveTo 직후 같은 호출에서 GetCurrentAnimatorStateInfo 를 읽으면 전부 이전 상태로 나온다. CrossFade 는 다음 프레임에 반영된다 — 코루틴으로 프레임을 넘겨야 한다. (2) Play 모드에서 찍은 Debug.Log 가 MCP GetConsoleLogs 에 안 잡힌다. 검증 결과는 파일로 써서 읽는 게 확실하다.
RunCommand 샌드박스는 System.dll 을 참조하지 않는다 — HashSet/ToHashSet 처럼 ISet 을 건드리는 코드는 CS0012 로 죽는다. List 로 우회할 것.

2026-08-05 (4)
레이아웃 시각 검증. 실제 결함 2개 잡아서 고침 — (1) Title rect 가 34px 인데 34pt 폰트는 41px 필요, 전 노드에서 rect 밖으로 그려지고 있었다 (TMP overflowMode=Overflow 라 눈에 안 띄었을 뿐, 2줄 제목이면 패널 밖으로 샜다). (2) 본문 최장 노드 n30 이 rect 97px 에 107.5px 로 렌더 — 오토사이즈가 22.4pt 에서 멈추고 더 안 줄어든다 (ForceMeshUpdate 6회 돌려도 수렴 안 함). ChoiceRow 를 줄여 TextPanel 을 190px 로 키워 해결.
캡처 방법 기록: ScreenCapture.CaptureScreenshot 은 에디터가 포커스를 잃으면 프레임이 안 돌아 파일이 안 생긴다. Application.runInBackground = true 를 **RunCommand 본문에서** 켜야 한다 — 코루틴 첫 줄에 넣으면 그 첫 프레임 자체가 안 돌아서 소용없다. 카메라→RenderTexture 로 직접 렌더하는 우회는 쓰지 마라: 폰트 아틀라스가 아직 GPU 에 안 올라가 한글이 전부 두부로 찍히고 캔버스 상태도 한 프레임 어긋난다.
또 하나: 노드를 옮긴 직후 같은 호출에서 버튼을 레이캐스트하면 못 맞춘다. 버튼이 그 프레임에 새로 생겨서 그렇다 — 이동과 클릭을 다른 호출로 나눠야 한다. 제품 결함 아님.
NotoSansKR SDF.asset 이 런타임에 글리프를 구워 넣으면서 커졌다(+6700줄). 정상 동작이고 커밋했지만, 새 문자가 나올 때마다 계속 변경된다.

2026-08-05 (3)
시나리오 러너 v1 구현. Core/Game 경계는 지켰다 — JsonUtility 는 Game 의 ScenarioLoader 안에만 있고 Core 는 UnityEngine 을 모른다.
Play 모드 테스트에서 실제 버그 하나 잡음: ScenarioUIController.ClearChoices 가 Destroy 만 부르는데 Destroy 는 프레임 끝에 처리돼서, 같은 프레임 안에 새 버튼을 만들면 옛 버튼이 레이아웃에 그대로 남는다 (테스트에서 버튼이 1→3→5→7 로 누적됐다). Destroy 전에 SetParent(null) 로 먼저 떼어내는 것으로 수정.
막힌 곳 두 군데 기록: (1) TMP Essential Resources 는 Play 모드에서 임포트가 안 된다 ("Cannot import package in play mode") — 에디터가 Play 중이면 먼저 세워야 한다. (2) RunCommand 코드는 Unity.AI.Assistant... 네임스페이스 안에 삽입되므로 UI 의 Image, UnityEditor.Compilation.CompilationPipeline 같은 짧은 이름은 Unity.* 쪽과 충돌한다 — 전체 경로로 써야 컴파일된다.

2026-08-05 (2)
Swapped the two greybox capsules for the Mixamo characters (Y Bot, Ch30_nonPBR), set all three FBX to Humanoid, looped Fight Idle, and built one Animator Controller per fighter. Done over MCP with Unity_RunCommand; console ends at 0 project errors. The only two console errors in the session came from Unity_Camera_Capture with an explicit cameraInstanceID — that tool cannot resolve a camera by instance ID here and always fails; capture with no argument (scene view) works and was used instead. Not a project problem, just a dead end to skip next time.

2026-08-05 (1)
Moved the scenario files into Assets/_Project/Scenario/ and made that the standing home for scenario assets (CLAUDE.md folder section updated). Built the Main.unity greyboxing stage plus two URP/Lit materials by hand-editing the scene YAML — the Unity editor was closed and the MCP bridge was down, so the usual "edit, then re-read the console over MCP" loop could not run. The scene YAML was checked structurally (29 unique anchors, no dangling local fileID references) but never imported by Unity. Console verification is deferred to the next Unity launch.

2026-08-03
Git/LFS setup finished and CLAUDE.md project rules locked in (Core/Game assembly split). Documented the session: daily log, two decision docs (assembly split, Git LFS), one troubleshooting doc (LFS after first commit), PAR materials, commit message drafts. Resolved both open questions above. Committed and pushed to origin/main.
Note: this session's docs were reconstructed from git log/diff and file timestamps, not from a live conversation record.

2026-08-02
Went through the full initial Unity project setup. Confirmed Unity MCP connection working end to end.
