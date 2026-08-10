STATUS

Update this file every time a session resets. This is the ONLY copy — the root CLAUDE.md pulls it in with `@md/STATUS.md`, so it loads automatically at session start. Do not create a second copy anywhere. Check "Current State" and "Next Up" before starting work.

Current State (last updated: 2026-08-11)
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
Stage 6 완료 — **가장 중요한 것: 사람이 Play 를 실제로 눌러야 한다.** 메뉴 CounterTime > Play Mode Setup (2026-08-11 아침 신설, 커밋 7325102)이 새 빈 씬 생성 + LiveMatchRunner/LiveMatchPanel 부착 + 쿠즈시 on + 한글 폰트 연결까지 자동으로 한다 — 메뉴 클릭 → Play 버튼, 이 두 번만 사람이 하면 된다. 캔버스 레이아웃·버튼 클릭·굳히기 2지선다 전환이 전부 미검증. 판단 대기 5개(docs/overnight-2026-08-11.md 맨 위) 중 #1(m18)·#2(쿠즈시 타이밍)는 아침에 해소, #3(뜯기/잡기 유지 게이트)·#4(동시 공격 우선순위)는 §2 에 "v1 임시 규칙"으로 문서화, #5(B 굳히기 고정 정책)는 여전히 미해결
쿠즈시(§4.4) 는 아직 [초안] — 사람이 검토해서 확정하거나 되돌릴 것. on 으로 실측한 결과(작업6)가 교환 수를 프로필에 따라 늘리거나 줄인다 — 밸런스 판단은 사람 몫
Stage 7 — regression + STATUS update (이 파일이 그 결과물이다 — 다음은 실제 Play 검증 이후 마무리 라운드)
Match length is still deliberately unresolved (see step 5 note above for the post-osaekomi numbers). Judgement formula, waza-ari thresholds, cost table and per-exchange seconds all stay untouched until after stage 6
잡기 HUD (패치 1.7 로 필수가 됐다) — Core 는 이미 GripChanged 에 부위를 실어 보낸다. Game 쪽에서 RenderTexture 바깥 풀해상도 레이어로 받아야 한다

결정 대기 (내가 판단 안 하고 남겨둔 취향 문제) — 선택지 버튼 높이가 111px → 82px 로 줄었다. 텍스트가 넘쳐서 TextPanel 을 키우느라 ChoiceRow 에서 떼어온 결과다. 라벨(26px)에는 충분하지만 버튼이 얇아 보이면 하단 패널을 1/3보다 키우거나 타이틀 34pt / 본문 24pt 를 줄이는 쪽으로 조정해야 한다
카피 교체 목록 — `LiveMatchPanel` 의 "B 선행 — A 공격 불발" (동시 공격 시 표시, 2026-08-11 아침)은 v1 임시 시스템 문구다. `PlaceholderATechnique`("밭다리" 고정)와 같은 부류 — 진짜 카피/연출이 붙기 전까지 기계적 문구로 둔다
Play 로 시나리오를 실제로 굴려보며 매핑을 다듬어라. 매핑은 title/content 만 읽고 정한 초안이라, 클립이 상황에 맞는지는 사람 눈으로 봐야 안다 — 특히 2026-08-07 에 AI 클립으로 교체한 19개 노드는 아직 Play 확인 전이다
(Separate, not urgent) Finish the Judo match-1 scenario on the scenario board — three things the user still needs to answer: how the right-sleeve grip exchange actually plays out / timing of the collar-grip transition / the tells that distinguish an ippon-seoi-nage attempt from an ouchi-gari attempt

알려진 공백 (인지하고 남겨둔 것 — 버그 아님)
n10 허리후리기 = ai_osotogari_entry 임시 대체. 기술 상이(하라이고시 ≠ 오소토가리), 모캡 대상
n27/n29/n39/n40 = NodeMotionMap 미등록

Resolved Questions (2026-08-03)
Why Assets/Editor/HubForceResolve.cs disappeared — nobody deleted it. It is Unity Hub's bootstrap script that calls Client.Resolve() and then removes itself and the empty Assets/Editor folder via AssetDatabase.DeleteAsset. The three deleted files match CleanupEmptyEditorFolder() exactly. Not a real incident, not resume material.
Whether to keep the Unity Cloud link (cloudProjectId / organizationId in ProjectSettings.asset) — keep and commit. cloudEnabled is 0, it is a project GUID rather than a secret, ProjectSettings.asset is tracked anyway, and the editor rewrites it if removed.

Session Log (newest first)
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
