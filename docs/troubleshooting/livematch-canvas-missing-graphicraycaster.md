# Play 모드 UI 버튼이 전부 안 눌리는 구조로 짜여 있었다 — GraphicRaycaster 누락

작성 2026-08-11 · **상태: 해결**

## 문제 상황

`LiveMatchPanel.BuildCanvas()`는 야간 세션이 만든 런타임 생성 캔버스다 — 씬 파일은
안 건드리고, `Awake()`에서 `Canvas`+`CanvasScaler`를 코드로 만든다.

```csharp
var go = new GameObject("LiveMatchCanvas", typeof(Canvas), typeof(CanvasScaler));
```

이 컴포넌트는 만들어진 뒤로 Unity 에디터에서 `typeof` 로드(컴파일 확인)까지만
검증됐고, **Play 모드로 한 번도 실행되지 않았다.** Play Mode Setup 메뉴를 만들면서
씬에 실제로 붙여 돌려볼 준비를 하다가, 코드를 다시 읽는 과정에서 발견했다 —
`GraphicRaycaster`가 없다. 이게 없으면 `EventSystem`이 화면 좌표를 UI 엘리먼트로
변환(레이캐스트)할 방법이 없어서, **씬에 버튼이 몇 개든 하나도 클릭되지 않는다.**
에러도 경고도 안 난다 — 그냥 조용히 반응 없는 화면이 된다.

## 왜 생겼나

같은 프로젝트의 `Main.unity`(`ScenarioCanvas`)는 손으로 씬 YAML을 작성하면서
`GraphicRaycaster`를 명시적으로 넣었다. 반면 `LiveMatchPanel`은 캔버스를 **코드로**
만든다 — 씬 에디터에서 `Add Component`로 만들면 Unity가 `Canvas`를 추가할 때
`GraphicRaycaster`를 자동으로 같이 붙여주지만(`RequireComponent`류의 편의 동작),
`new GameObject(..., typeof(Canvas), ...)`로 코드에서 만들면 그 자동 부착이 없다.
**에디터의 관습적 편의가 코드 경로에는 없다**는 것을 실행해보기 전까지는
아무도 몰랐다.

## 시도한 것들

1. 그 시점 지시가 "코드 변경은 에디터 메뉴 추가만"이었다 — `LiveMatchPanel.cs`를
   건드릴 권한이 없는 상태에서 먼저 우회책을 짰다. `PlayModeSetupMenu`가
   `EditorApplication.playModeStateChanged`를 구독해, Play 모드 진입 직후
   `GameObject.Find("LiveMatch/LiveMatchCanvas")`로 캔버스를 찾아
   `AddComponent<GraphicRaycaster>()`를 붙였다. 원인 지점을 안 고치고
   증상만 사후에 메우는 방식이라는 걸 보고에 명시했다.
2. 다음 대화에서 사람이 "`LiveMatchPanel.BuildCanvas`에 정식 추가, 에디터
   우회 코드는 제거"로 결정했다.

## 최종 해결법

생성자의 컴포넌트 타입 목록에 하나만 더했다.

```csharp
var go = new GameObject("LiveMatchCanvas",
    typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
```

그리고 `PlayModeSetupMenu`의 `playModeStateChanged` 구독·`AttachGraphicRaycasterOnPlay`
메서드를 통째로 삭제했다 — 에디터에서만 동작하던 우회가 이제 필요 없고,
**빌드에서도 정상 동작**하는 게 원인 지점 수정의 실질적 이득이다(에디터 전용
우회는 빌드된 실행 파일에는 아예 안 들어간다).

## 재발 방지

- **런타임에 코드로 생성하는 UI는 씬 에디터의 "자동 부착" 관습을 대신 챙겨야
  한다.** `Canvas`를 코드로 만들 때는 `GraphicRaycaster`를 항상 같이 넣는다 —
  씬에서 `Add Component`로 만들 때 자동으로 딸려오는 컴포넌트 목록을
  참조할 것.
- **"컴파일된다"와 "동작한다"는 다른 검증이다.** 이 결함은 `typeof` 로드
  확인, 0 컴파일 에러/경고 확인을 전부 통과한 채로 존재했다 — 어느 것도
  런타임 인터랙션을 확인하지 않는다. Play 모드 실행 확인이 사람 몫으로
  미뤄진 컴포넌트는 그 확인이 끝나기 전까지 "동작 미검증"으로 취급한다.
- 코드 변경 범위가 제한된 상황에서 원인 지점을 못 고칠 때는, 증상을
  메우는 우회책이라는 걸 명시적으로 보고하고 범위가 풀리면 원인으로
  되돌아간다 — 우회를 영구 해결책으로 착각하지 않는다.

## 이력서 한 줄

> Play 모드로 한 번도 실행 검증되지 않았던 런타임 생성 UI 캔버스에서
> GraphicRaycaster 누락으로 모든 버튼이 클릭 불가능해지는 구조적 결함을,
> 코드 변경 범위가 제한된 상황에서는 에디터 툴링으로 먼저 안전하게 우회하고
> 제약이 풀린 뒤 원인 지점(런타임 캔버스 생성 코드)에 정식 수정을 반영하는
> 2단계로 처리했습니다.
