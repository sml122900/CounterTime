# 기술 결정: 훈련 클립 스캐폴드 — 재생성 시 무손실 + "모캡만 넣으면 적용"을 가정 대신 실측

작성 2026-08-13 · 상태: **적용 완료**

## Problem — 문제

훈련 시스템(`DESIGN_training-draft.md` §3)의 핵심 요구는 "사용자가 QuickMagic으로
종목 클립을 따오면, 인스펙터에서 슬롯에 넣기만 하면 동작"이다. 이걸 구현하려면
두 가지 서로 다른 리스크를 각각 막아야 했다.

**리스크 1 — 재생성이 작업을 지운다.** 종목별 `AnimatorOverrideController` 13개는
`TrainingAssetBuilder`(에디터 메뉴)가 생성한다. 이 메뉴는 앞으로도 다시 실행될
일이 반드시 생긴다 — 종목이 추가되거나, 카탈로그를 새로 손보거나, 실수로 다시
누르거나. 그 시점에 아티스트가 이미 오버라이드 슬롯에 채워 넣은 진짜 모캡
클립이 있다면, 순진하게 다시 만들면 그 자리가 placeholder로 되돌아간다 —
작업이 통째로 사라진다.

**리스크 2 — "될 것 같다"와 "된다"는 다르다.** 이 요구가 실제로 성립하려면
여러 가정이 동시에 맞아야 한다: `TrainingSessionController`가 CrossFade에
쓰는 상태 이름(`"Rep"`/`"Strained"` 등)이 오버라이드 컨트롤러에서도 그대로
유효한지, `AnimatorOverrideController`가 `RuntimeAnimatorController`를
상속해 `Animator.runtimeAnimatorController` 대입에 그대로 들어가는지, 인스펙터의
슬롯 교체가 런타임에 실제로 반영되는지. 코드 리뷰만으로는 이 가정들이 전부
맞는지 확신할 수 없다 — 이 프로젝트는 이미 "측정 없이 규칙을 확정하지 않는다"는
원칙(`docs/decisions/measurement-driven-rules.md`)을 갖고 있고, 스캐폴드 검증도
같은 기준을 적용해야 했다.

## Action — 한 것

**리스크 1**: `TrainingAssetBuilder`의 모든 생성 함수(`BuildBaseController`/
`BuildOverride`/`BuildCatalog`) 앞에 동일한 가드를 뒀다.

```csharp
static RuntimeAnimatorController BuildOverride(string id, AnimatorController baseController)
{
    var path = $"{OverrideFolder}/AC_Training_{id}.overrideController";
    var existing = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
    if (existing != null) return existing;   // 있으면 절대 안 건드린다

    var over = new AnimatorOverrideController(baseController);
    AssetDatabase.CreateAsset(over, path);
    return over;
}
```

"있으면 스킵"이 아니라 "있으면 그대로 반환"이라 호출부(`Build()`)는 신경 쓸 게
없다 — 처음 실행이든 100번째 실행이든 결과가 같은 13개 에셋 참조를 돌려받는다.

**리스크 2**: 인스펙터가 슬롯에 클립을 드롭할 때 내부적으로 쓰는 것과 같은
공개 API(`AnimatorOverrideController.ApplyOverrides`)로 실제 교체를 재현했다.

```csharp
var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
over.GetOverrides(overrides);
for (int i = 0; i < overrides.Count; i++)
    if (overrides[i].Key.name == "Standing Melee Punch")
        overrides[i] = new(overrides[i].Key, tauntClip);   // 다른 클립으로 교체
over.ApplyOverrides(overrides);
```

이걸 **C# 코드를 전혀 안 건드리고** Play 모드에서 실행해, 그 종목 렙 입력 시
실제로 바뀐 클립(Taunt)이 재생되는지 확인했다. 확인 후 override를 다시 `null`
로 되돌려 스캐폴드를 pass-through 상태로 커밋했다.

## Result — 결과

재생성 안전성은 코드 가드로, "모캡 슬롯만 바꾸면 적용된다"는 핵심 요구는 실측
으로 각각 확정됐다. 다음에 종목이 추가되거나 스캐폴드 메뉴가 다시 실행돼도
기존에 채워진 모캡은 안전하다는 것, 그리고 아티스트가 실제로 겪을 워크플로우
(클립 드롭 → 코드 재컴파일 없이 바로 반영)가 실제로 동작한다는 것 둘 다
가정이 아니라 확인된 사실로 남았다.

## 면접 예상 질문

**"왜 굳이 API로 시뮬레이션했나? 코드 보고 판단하면 안 됐나?"**
CrossFade 상태 이름 매칭, `RuntimeAnimatorController` 다형성, 인스펙터 조작이
런타임에 반영되는 시점 — 이 세 가정이 코드 상으로는 다 맞아 보여도, 실제로
실행해보지 않으면 "될 것 같다"에 머문다. 스캐폴드를 만드는 이유 자체가 "나중에
모캡만 넣으면 되게" 하려는 건데, 그 전제를 확인 안 하고 넘기면 스캐폴드의
존재 이유가 검증 안 된 채로 남는다.

**"덮어쓰기 가드가 없으면 실제로 무슨 일이 나나?"**
`AnimatorOverrideController`를 새로 만들면 그 안의 오버라이드 목록은 전부
비어서 시작한다(base controller 클립으로 pass-through). 즉 아티스트가 채워둔
모캡 클립 참조가 사라지는 게 아니라, 그 자리를 가리키던 오버라이드 항목 자체가
새 빈 인스턴스로 통째로 교체된다 — 파일을 열어봐도 복구할 방법이 없다(에셋이
새로 생성돼 이전 버전을 덮어쓴 것이라, `AssetDatabase.CreateAsset`이 기존 GUID
를 재사용하며 내용만 초기화된 새 객체로 갈아치운다).

## 말할 가치가 있는 지점

이 두 리스크는 서로 다른 종류다 — 하나는 "미래의 나 또는 다른 사람이 실수로
이미 한 작업을 지우는 것"을 막고, 다른 하나는 "설계가 문서상으로는 맞아
보이는데 실제로는 안 되는 것"을 막는다. 둘 다 사후에 발견하면 비용이 훨씬
크다(모캡 재작업, 아티스트 워크플로우 재설계). 스캐폴드를 "일단 돌아가는
것처럼 보이면 끝"이 아니라, 이 스캐폴드가 실제로 서비스해야 할 사람(모캡을
붙일 아티스트, 나중에 종목을 추가할 개발자)의 관점에서 두 실패 모드를 각각
따져 막은 것이 이번 작업의 실질적 내용이었다.
