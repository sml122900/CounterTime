STATUS

Update this file every time a session resets. This is the ONLY copy — the root CLAUDE.md pulls it in with `@md/STATUS.md`, so it loads automatically at session start. Do not create a second copy anywhere. Check "Current State" and "Next Up" before starting work.

Current State (last updated: 2026-08-05)
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
대신 Core 가 순수 C# 이라는 점을 이용해 scratchpad 에 dotnet 콘솔 하니스를 만들어 커밋된 NUnit 테스트 파일을 그대로 컴파일·실행했다 (nunit.framework.dll 은 Library/PackageCache/com.unity.ext.nunit@.../net40/unity-custom/ 에 있다). 하니스는 리포에 안 들어간다 — 커밋된 산출물은 NUnit 테스트 쪽이다. 세션마다 다시 만든다
CounterTime.Core.Tests → 하니스로 돈다. CounterTime.Game.Tests → **안 돈다.** MonoBehaviour·TextAsset 을 써서 UnityEngine 이 필요하다
그래서 하니스 출력 마지막 줄이 스스로 범위를 밝히게 해뒀다: "COVERAGE: CounterTime.Core.Tests only — CounterTime.Game.Tests needs Unity Test Runner"
**"Core N개 초록" 은 Game 테스트가 검증됐다는 뜻이 아니다.** 에러가 안 나면서 안 돌고 있는 부류라 사람이 직접 확인해야 한다: Window > General > Test Runner > EditMode 탭 > Run All

Next Up (in priority order)
step 7 은 이 항목이다 (완료). 남은 것은 아래 우선순위대로
MatchEngine 을 시나리오 러너에 연결 — 지금은 ExchangeScript 가 v1 seam 이고, 42노드 보드 JSON 이 reads/outcomes 를 공급해야 한다. 이번 패스에서는 CounterTime.Game / NodeMotionMap / 시나리오 러너를 일부러 안 건드렸다
잡기 HUD (패치 1.7 로 필수가 됐다) — Core 는 이미 GripChanged 에 부위를 실어 보낸다. Game 쪽에서 RenderTexture 바깥 풀해상도 레이어로 받아야 한다

결정 대기 (내가 판단 안 하고 남겨둔 취향 문제) — 선택지 버튼 높이가 111px → 82px 로 줄었다. 텍스트가 넘쳐서 TextPanel 을 키우느라 ChoiceRow 에서 떼어온 결과다. 라벨(26px)에는 충분하지만 버튼이 얇아 보이면 하단 패널을 1/3보다 키우거나 타이틀 34pt / 본문 24pt 를 줄이는 쪽으로 조정해야 한다
Play 로 시나리오를 실제로 굴려보며 매핑을 다듬어라. 매핑은 title/content 만 읽고 정한 초안이라, 클립이 상황에 맞는지는 사람 눈으로 봐야 안다 — 특히 2026-08-07 에 AI 클립으로 교체한 19개 노드는 아직 Play 확인 전이다
Port the combat logic from the HTML prototype (counter-time-v03.html) into pure C# classes under CounterTime.Core — the two-phase turn (grip-fighting → scoring techniques), matchup judgment, opponent habit-reading system
Design the opponent tendency data structure: POCO in CounterTime.Core + ScriptableObject wrapper in CounterTime.Game (this was the planned "session 3" work)
(Separate, not urgent) Finish the Judo match-1 scenario on the scenario board — three things the user still needs to answer: how the right-sleeve grip exchange actually plays out / timing of the collar-grip transition / the tells that distinguish an ippon-seoi-nage attempt from an ouchi-gari attempt

알려진 공백 (인지하고 남겨둔 것 — 버그 아님)
n10 허리후리기 = ai_osotogari_entry 임시 대체. 기술 상이(하라이고시 ≠ 오소토가리), 모캡 대상
n27/n29/n39/n40 = NodeMotionMap 미등록

Resolved Questions (2026-08-03)
Why Assets/Editor/HubForceResolve.cs disappeared — nobody deleted it. It is Unity Hub's bootstrap script that calls Client.Resolve() and then removes itself and the empty Assets/Editor folder via AssetDatabase.DeleteAsset. The three deleted files match CleanupEmptyEditorFolder() exactly. Not a real incident, not resume material.
Whether to keep the Unity Cloud link (cloudProjectId / organizationId in ProjectSettings.asset) — keep and commit. cloudEnabled is 0, it is a project GUID rather than a secret, ProjectSettings.asset is tracked anyway, and the editor rewrites it if removed.

Session Log (newest first)
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
