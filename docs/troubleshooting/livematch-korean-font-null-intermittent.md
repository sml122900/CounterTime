# 한글이 전부 □로 깨짐 — koreanFont 필드가 간헐적으로 NULL

작성 2026-08-11 · **상태: 증상 제거(fallback), 정확한 트리거는 미확정**

## 문제 상황

`PlayModeSetupMenu.Setup()`은 편집 시점에 `SerializedObject`로
`LiveMatchPanel.koreanFont`에 `NotoSansKR SDF`를 대입한다. 그런데 사람이 실제로
메뉴 → Play로 확인한 세션에서, `LiveMatchPanel`의 실제 인스턴스를 직접 읽어보니
`koreanFont`가 `NULL`이었고, 라벨 18개 전부(한글 텍스트 포함) `LiberationSans SDF`
(TMP 기본 폰트)로 렌더되고 있었다 — 그 폰트엔 한글 글리프가 없어서 □로 나온다.

## 시도한 것들 — 4가지 유발 메커니즘을 직접 재현 시도, 전부 실패(=정상 동작 확인됨)

같은 세션 안에서 `Unity_RunCommand`로 직접 계측(`SerializedObject`로 필드값을
실측)하며 아래 4가지를 통제된 조건에서 재현 시도했다. **전부 코드가 정상
동작함을 확인**했다 — 즉 아래 중 어느 것도 원인이 아니다:

1. **대입 코드 자체 결함** — `Setup()` 실행 직후(Play 진입 전), 에디터 모드에서
   `koreanFont` 값을 바로 읽음 → `NotoSansKR SDF`, 정상.
2. **Play 전환(도메인 리로드)로 유실** — 위 상태에서 바로
   `EditorApplication.isPlaying = true` → Play 모드에서 다시 읽음 →
   `NotoSansKR SDF`, 정상. 라벨 18개 전부 올바른 폰트로 렌더.
3. **에디트 모드 스크립트 재컴파일로 유실** — `Setup()` 직후 Play 없이
   `CompilationPipeline.RequestScriptCompilation()`으로 강제 재컴파일 →
   컴파일 완료 후 값을 읽음 → `NotoSansKR SDF`, 정상.
4. **중복 씬/중복 인스턴스로 엉뚱한 오브젝트를 읽음** —
   `Resources.FindObjectsOfTypeAll<LiveMatchPanel>()`로 전수 조사 →
   씬 1개, 인스턴스 1개, 정상 값.

## 유력하지만 확정 못 한 후보

이번 진단 세션 도중, `PlayModeSetupMenu.Setup()`의
`EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()` 호출이 **실제로
두 번 막혔다**:

- 한 번은 `InvalidOperationException: This cannot be used during play mode`로
  즉시 실패(에디터가 Play 전환 중일 때 호출됨).
- 한 번은 씬이 dirty한 상태에서 호출되어 모달 "저장하시겠습니까?" 대화상자가
  뜨며 Unity 에디터 메인 스레드 전체가 멈췄다(MCP 명령이 전부 타임아웃).

두 경우 모두 `Setup()`이 **아무 표시 없이 조용히 중단**된다 — 새 `LiveMatch`
오브젝트도, 폰트 대입도 일어나지 않는다. 사람이 이 상태를 못 보고(콘솔을 안
봤거나, 대화상자가 떴는지 몰랐거나) 곧바로 Play를 누르면, **이전에 존재하던
(폰트가 없었거나 다른 이유로 설정이 안 된) `LiveMatch` 오브젝트**로 Play가
진행될 수 있다.

이게 실제로 사용자가 겪은 그 세션의 원인이라는 **직접 증거는 없다** — 그
세션의 `koreanFont` NULL 인스턴스가 정확히 어느 경로로 만들어졌는지는 로그가
Play 진입 시 Console "Clear on Play"로 지워져 있어 사후 재구성이 불가능했다.
정황(같은 세션에서 같은 호출이 실제로 두 번 막힘)은 있지만 인과의 확정 증거는
아니다.

## 최종 조치 — 증상 제거(fallback) + 실패를 시끄럽게

원인을 못 박지 못했으므로 코드에서 추측성 원인 수정은 하지 않았다. 대신:

1. **`LiveMatchPanel.Awake()`에 자가복구 안전망 추가.** `koreanFont`가
   `null`이면 `Resources.Load<TMP_FontAsset>("NotoSansKR SDF")`로 직접
   재로드한다. 그래도 `null`이면 `Debug.LogError`로 정확히 무엇을
   시도했는지 남긴다 — 조용히 □로 넘어가지 않는다.
   - `AssetDatabase`(에디터 전용) 대신 `Resources.Load`를 쓴 이유: 이
     안전망은 `CounterTime.Game`(일반 런타임 어셈블리)에 있고, 빌드에서도
     같은 문제가 재발하면 안 된다. `NotoSansKR SDF.asset`을
     `Assets/_Project/Art/Fonts/Resources/`로 옮겨(`AssetDatabase.MoveAsset`,
     GUID 보존) `Resources.Load`가 빌드·에디터 양쪽에서 먹히게 했다.
2. **`PlayModeSetupMenu.Setup()`이 막혔을 때 조용히 안 넘어가게.** 두 실패
   경로(Play 전환 중 호출 / 저장 확인 취소) 각각에 `Debug.LogWarning` +
   조기 `return`을 추가했다. 대화상자 자체는 그대로 둔다 — 없애면 정말
   저장할 가치가 있는 다른 씬의 변경사항을 조용히 버리는 위험이 생긴다.
   여기서 고친 것은 "막혔는데 아무 것도 안 보임"이지 대화상자의 존재
   자체가 아니다.
3. 두 안전망 모두 정상 경로(Setup이 성공하는 보통의 경우)에는 아무 영향이
   없다 — Play 모드 실측으로 확인(`koreanFont` = `NotoSansKR SDF`, 라벨
   전부 같은 폰트).
4. **안전망 자체도 직접 검증했다**: `koreanFont`를 고의로 비운 채(Setup()의
   폰트 대입 단계를 건너뛴) `LiveMatch`를 만들어 Play에 진입 →
   `Awake()`의 `Resources.Load` 경로가 실제로 `koreanFont`를 채우고
   라벨이 `NotoSansKR SDF`로 렌더됨을 실측 확인. 에러 로그 0건(정상
   동작이므로 실패 로그가 안 뜨는 게 맞다).

## 재발 방지

- **편집 시점 `SerializedObject` 대입 한 번에만 의존하지 않는다.** 그
  대입이 실행되는 정확한 타이밍/조건이 이번처럼 재현 안 되는 방식으로 깨질
  수 있다는 게 실측으로 나왔다(비록 어떻게인지는 못 잡았어도). 런타임에
  다시 확인하고 채우는 안전망을 같이 둔다.
- **`AssetDatabase` 기반 로직을 `CounterTime.Game`(빌드에 들어가는 일반
  어셈블리)에 넣지 않는다.** 에디터 전용 API는 `CounterTime.Game.Editor`
  안에만. 런타임에서 에셋을 다시 찾아야 하면 `Resources.Load`(또는
  Addressables) 경로를 쓴다.
- **막히는 코드 경로는 조용히 `return`하지 않는다.** 이번에 정확히 그
  패턴(조건 실패 시 무언의 `return`)이 사람을 오도했다 — "메뉴를 눌렀는데
  왜 하나도 안 바뀌었지"를 콘솔 없이는 알 방법이 없었다.
- Console의 "Clear on Play"가 켜져 있으면, Play 진입 **전**에 무슨 일이
  있었는지는 Play가 시작되는 순간 증거가 사라진다. 진단 중 이 사실을
  놓쳐서 "경고 로그가 없었다 = 정상이었다"라고 잘못 결론지을 뻔했다 —
  실제로는 그 로그가 있었는지조차 알 수 없는 상태였다.

## 이력서 한 줄

> 편집 시점에 정상적으로 대입되는 것으로 확인된 필드가 Play 모드 진입 후
> 간헐적으로 비어 있는 버그를, 4가지 후보 메커니즘을 통제된 재현으로 하나씩
> 배제하면서 진단했다. 정확한 트리거를 못 박지 못한 상태에서 추측성 코드
> 수정 대신 런타임 자가복구 안전망(빌드 호환 `Resources.Load` 경로)과
> 실패를 드러내는 로그를 추가해 증상을 결정적으로 제거하고, 못 잡은
> 원인은 못 잡았다고 명시적으로 기록했다.
