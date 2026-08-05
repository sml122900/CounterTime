# MCP 로 Unity Play 모드를 검증할 때 걸리는 함정 5개

작성 2026-08-05 · **상태: 전부 해결, 우회법 확정**

Claude Code 가 Unity MCP(`Unity_RunCommand` / `Unity_GetConsoleLogs` / `Unity_Camera_Capture`)로 Play 모드 결과를 스스로 확인하려다 걸린 것들. 전부 "제품 버그처럼 보이지만 검증 도구의 한계"였고, 한 번씩은 **없는 버그를 있다고 착각할 뻔했다.**

## 문제 상황

시나리오 러너를 만든 뒤 "정말 동작하는가"를 사람 눈 없이 확인해야 했다. Play 를 누르고 노드를 옮기고 상태를 읽는 걸 전부 MCP 로 해야 하는데, 다섯 군데서 결과가 거짓말을 했다.

---

## 1. 스크린샷 파일이 아예 안 생긴다

```csharp
ScreenCapture.CaptureScreenshot("Temp/shots/n1.png");
// → 파일 없음. 에러도 없음.
```

**원인** — 에디터가 포커스를 잃으면 Game 뷰가 프레임을 안 돌린다. MCP 로 원격 조작 중이라 Unity 창은 항상 백그라운드다. 프레임이 안 도니 캡처 콜백이 실행되지 않는다.

**해결** — `Application.runInBackground = true`.

**다만 이걸 코루틴 첫 줄에 넣으면 소용없다.**

```csharp
// 안 됨 — 이 코루틴의 첫 프레임 자체가 안 돌아서 이 줄에 도달하지 못한다
IEnumerator Start() { Application.runInBackground = true; yield return null; ... }

// 됨 — RunCommand 본문(에디터 스레드)에서 먼저 켜고 코루틴을 시작한다
public void Execute(ExecutionResult result) {
    Application.runInBackground = true;
    new GameObject("Probe").AddComponent<Probe>();
}
```

또 하나: **Play 모드에 재진입하면 이 값이 초기화된다.** 매번 켜야 한다.

## 2. 카메라 → RenderTexture 우회는 쓰지 마라

1번을 만나고 "그럼 카메라를 직접 렌더해서 픽셀을 읽자"로 우회했다. Screen Space Overlay 캔버스는 카메라에 안 잡히니 임시로 Screen Space Camera 로 바꾸고 `cam.Render()` → `ReadPixels`.

**결과물이 거짓이었다.**

- **한글이 전부 두부(□)로 찍혔다.** 동적 TMP 아틀라스가 아직 GPU 에 안 올라간 상태에서 강제 렌더했기 때문. 실제로는 정상 렌더되고 있었다
- **캔버스 내용이 한 프레임 어긋났다.** n30 을 찍었는데 다른 노드 화면이 나옴

이 캡처만 보고 "폰트가 깨졌다"고 판단했으면 멀쩡한 폰트 설정을 갈아엎을 뻔했다. 1번(`runInBackground` + 실제 Game 뷰 캡처)으로 다시 찍으니 전부 정상이었다.

**교훈** — 렌더 파이프라인을 우회해서 만든 이미지는 "실제 화면"이 아니다. 시각 검증은 실제 Game 뷰 프레임으로 해야 한다.

## 3. `MoveTo` 직후 같은 호출에서 Animator 상태를 읽으면 전부 이전 값

```csharp
runner.MoveTo("n9");
animator.GetCurrentAnimatorStateInfo(0);   // → 여전히 이전 상태
```

노드를 9개 옮기며 상태를 읽었더니 **전부 `Fight Idle`** 이 나왔다. 모션 연결이 통째로 안 되는 것처럼 보였다.

**원인** — `Animator.CrossFade` 는 다음 애니메이터 평가(다음 프레임)에 반영된다. 같은 프레임에서 읽으면 당연히 이전 상태다.

**해결** — 코루틴으로 프레임을 넘긴 뒤 읽는다.

```csharp
runner.MoveTo(id);
yield return null;              // 최소 1프레임
Log(id + " -> " + State(a));    // 이제 맞다
```

빠르게 연속 이동하면 CrossFade(0.25초 ≈ 15프레임)가 끝나기 전에 다음 이동이 온다. 이때 `GetCurrentAnimatorStateInfo` 는 옛 상태를 그대로 들고 있고, **`GetNextAnimatorStateInfo` 가 진짜 목표 상태**다. `IsInTransition(0)` 로 갈라 읽어야 한다.

## 4. UI 버튼 레이캐스트도 같은 함정

```csharp
runner.MoveTo("n36");     // 버튼이 이 프레임에 새로 생성됨
graphicRaycaster.Raycast(ped, hits);   // hits.Count == 0
```

"버튼이 클릭 불가"로 보였다. 실제로는 이 프레임에 방금 만들어진 그래픽이라 레이캐스트 대상에 아직 안 올라간 것뿐.

**해결** — 이동과 클릭을 **다른 RunCommand 호출로 나눈다.** 호출 사이에 프레임이 충분히 지나간다.

참고로 `EventSystem.current.RaycastAll` 은 이 상황에서 계속 0을 반환했고 `GraphicRaycaster.Raycast` 는 정상 동작했다. 원인은 안 팠다 — 후자로 검증 가능하니 넘어갔다.

## 5. Play 모드의 `Debug.Log` 가 MCP 콘솔에 안 잡힌다

코루틴 안에서 `Debug.Log` 로 검증 결과를 찍었는데 `Unity_GetConsoleLogs` 가 `totalCount: 0` 을 반환했다. 프레임은 돌고 있었고(`Time.frameCount` 증가 확인) 프로브 오브젝트도 살아 있었다.

**해결** — 검증 결과를 파일로 쓰고 파일을 읽는다.

```csharp
System.IO.File.WriteAllText("Temp/motion-probe.txt", sb.ToString());
```

`Temp/` 는 gitignore 대상이라 저장소도 안 더럽힌다. 이게 가장 확실했다.

---

## 곁다리: RunCommand 샌드박스 제약 2개

`Unity_RunCommand` 는 보낸 코드를 `Unity.AI.Assistant.Agent.Dynamic.Extension.Editor` 네임스페이스 안에 감싸 컴파일한다. 여기서 두 가지가 걸린다.

**(1) 짧은 타입 이름이 `Unity.*` 와 충돌한다**

```
CS0118: 'Image' is a namespace but is used like a type
CS0234: 'GetAssemblies' does not exist in 'Unity.CompilationPipeline'
```

`using UnityEngine.UI;` 를 해도 `Image` 가 `Unity.Image` 네임스페이스로 먼저 잡힌다. → `UnityEngine.UI.Image`, `UnityEditor.Compilation.CompilationPipeline` 처럼 **전체 경로로** 쓴다.

**(2) `System.dll` 이 참조되지 않는다**

```
CS0012: The type 'ISet<>' is defined in an assembly that is not referenced
```

`HashSet<T>` / `ToHashSet()` 처럼 `ISet<>` 을 건드리면 죽는다. `List<T>` + `Contains` 로 우회.

`System.Reflection` 도 막혀 있다 ("unauthorized namespaces"). 콘솔 클리어(`LogEntries.Clear`) 같은 리플렉션 트릭은 못 쓴다.

---

## 최종 정리 — Play 모드 검증 체크리스트

1. `Application.runInBackground = true` 를 **RunCommand 본문**에서 켠다 (Play 재진입마다)
2. 상태 변경 → 확인은 **코루틴으로 프레임을 넘기거나** RunCommand 호출을 나눈다
3. 검증 결과는 `Debug.Log` 말고 **`Temp/` 아래 파일**로 뽑는다
4. 시각 확인은 `ScreenCapture.CaptureScreenshot` (실제 Game 뷰). RenderTexture 우회 금지
5. 코드는 타입 전체 경로로, `HashSet` 대신 `List`
6. 씬을 편집하려면 **Play 모드를 먼저 끈다** (TMP 패키지 임포트도 Play 중엔 실패한다)

## 이력서 한 줄

> Unity 에디터를 MCP 로 원격 제어해 Play 모드 결과를 자동 검증하는 절차를 구축하면서, 프레임 타이밍·렌더 파이프라인 우회·백그라운드 렌더 정지로 인해 검증 결과가 거짓 음성을 내는 사례 5건을 규명하고, 오탐을 걸러내는 검증 체크리스트로 정리했습니다.
