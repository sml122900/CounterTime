# 기술 결정: 대회 매치마다 LiveMatch를 다시 짓지 않고 재사용, Awake 경쟁은 순서로 회피

작성 2026-08-13 · 상태: **적용 완료**

## Problem — 문제

연습 대회는 최대 6경기(32강~결승, 또는 4강 패배 시 동메달전까지)를 한
플레이 세션 안에서 연달아 치른다. 기존 `LiveMatch` GameObject(
`LiveMatchRunner`+`ArenaStageView`+`LiveMatchPanel`)는 `PlayModeSetupMenu`가
에디터에서 미리 짓고 에셋(파이터 모델·컨트롤러·폰트)을 `SerializedObject`로
주입해두는 구조다 — 이 배선은 편집 모드 전용 API(`AssetDatabase`)를 쓰므로
런타임(Play 모드 중, 대회 진행 중)에는 다시 할 수 없다.

매 경기 GameObject를 파괴하고 새로 지으면 이 에셋 배선을 런타임에 재현할
방법이 없고, 3D 무대(카메라·바닥·파이터 두 명)를 매번 다시 짓는 비용도
든다.

## Action — 한 것

GameObject를 파괴하지 않고 비활성화만 했다 켜는 방식을 택했다.
`LiveMatchRunner.Rebuild(seed)`(기존 `Build()` 재호출 래퍼)와
`LiveMatchPanel.ResetForNewMatch()`(로그·선택 상태 초기화, `Start()`가
하는 일을 재현)를 최소로 추가했다.

구현 중 발견한 문제: `PlayModeSetupMenu`가 LiveMatch를 **비활성 상태로**
짓는다(대회 시작 전엔 화면에 안 보여야 하므로, `SetActive(false)`). Unity는
비활성 오브젝트의 `Awake()`를 그 오브젝트가 처음 활성화될 때까지 미룬다 —
즉 `LiveMatchPanel.Awake()`(그 안의 `BuildCanvas()`)가 **첫 경기 시작 전까지
한 번도 안 돈다.** `MetaFlowController.StartMatch()`가 순서를 잘못 잡으면
(`Rebuild`/`ResetForNewMatch`를 `SetActive(true)`보다 먼저 부르면)
`ResetForNewMatch()`가 아직 안 지어진 UI 필드(`_logText`, `_osaekomiRoot`
등)를 건드려 `NullReferenceException`이 난다.

```csharp
void StartMatch()
{
    // SetActive(true) 가 먼저다 — 첫 경기 전까지 LiveMatch 는 비활성이라
    // Awake(BuildCanvas 포함)가 아직 안 돌았다.
    _metaCanvasGo.SetActive(false);
    liveMatchRoot.SetActive(true);
    liveMatchRunner.SetOpponent(OpponentFactoryFor(_save.career.practiceCupRound));
    liveMatchRunner.Rebuild(bracketSeed + (int)_save.career.practiceCupRound + 1);
    liveMatchPanel.ResetForNewMatch();
}
```

두 번째 경기부터는 `Awake`가 이미 끝나 있어 순서가 상관없지만, 첫 경기와
같은 코드 경로를 타게 해서 조건 분기를 안 만들었다.

## Result — 결과

Play 모드에서 5개 라운드(32강→16강→8강→4강→동메달전)를 연달아 실제 엔진
매치로 진행 — 매 라운드 `SetActive(true)` 직후 `Rebuild`/`ResetForNewMatch`
호출이 예외 없이 동작함을 확인했다. 라운드마다 다른 opponent factory와
다른 seed로 완전히 새 매치 상태가 시작되면서도, 3D 무대(카메라·파이터
모델·컨트롤러)는 한 번만 지어진 것을 재사용했다.

## 면접 예상 질문

**"차라리 매치마다 새 GameObject를 만드는 게 더 간단하지 않았나?"**
간단해 보이지만 에셋 배선 문제를 못 푼다. `PlayModeSetupMenu`의
`SerializedObject` 기반 주입은 편집 모드 전용이라, 런타임에 새
GameObject를 만들면 파이터 모델·애니메이터 컨트롤러·바닥 머티리얼을
연결할 방법이 없다(런타임에 `AssetDatabase.LoadAssetAtPath`를 쓰는 것은
빌드에서 동작 안 하는 에디터 전용 API를 프로덕션 경로에 넣는 것과
같다). 재사용이 유일하게 실용적인 경로였다.

**"이 타이밍 문제를 어떻게 미리 알았나?"**
몰랐다 — 처음 짤 때는 `Rebuild`를 먼저 부르고 `SetActive`를 나중에
불렀다가 실제로 NRE를 겪었다. 원인(Awake가 아직 안 돌았다)을 확인한
뒤 순서를 바꿨다. "안다"기보다 "겪고 나서 문서화했다"가 정확한 설명이다.

## 말할 가치가 있는 지점

이 문제는 코드 리뷰만으로는 잘 안 보인다 — `Rebuild()`와
`ResetForNewMatch()` 둘 다 각자 읽으면 멀쩡한 메서드다. 문제는 **호출
순서**와 **호출 시점의 GameObject 활성 상태**라는, 두 파일 사이의 암묵적
전제에 있었다. 이런 종류의 결함은 실제로 그 경로를 실행해봐야(Play
모드에서 첫 경기를 실제로 시작해봐야) 드러난다 — 그래서 Play 실측을
빼먹지 않는 것이 단순히 "확인 절차"가 아니라 이런 클래스의 결함을 잡는
유일한 방법이었다.
