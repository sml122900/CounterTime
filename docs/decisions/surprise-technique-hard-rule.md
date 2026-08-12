# 기술 결정: 변칙기 "하드 규칙"을 처음으로 실제 구현했다

작성 2026-08-13 · 상태: **적용 완료**

## Problem — 문제

작업 지시는 4강 임준철의 변칙기(안오금띄기)를 "SurpriseExempt 하드조건(진도윤
말아업어치기와 같은 방식, 습관 아님)"으로 구현하라고 했다. 근거 문서에도
비슷한 문장이 있었다 — `HabitBrainTests.cs`의 기존 테스트 하나가
"말아업어치기는 습관이 아니라 하드 규칙이다(체력<30, 경기당 1회)"라고
주석에 적어뒀고, `IBrainContext.cs`도 "말아업어치기의 '경기당 1회' 플래그는
여기가 아니라 경기당 상태(HabitState 계열)다"라고 못 박아뒀다.

문제는 이 문장들이 전부 **부재를 증명하는 테스트**였을 뿐, 실제로 그 "하드
규칙"을 발화시키는 코드가 어디에도 없었다는 것이다.
`SurpriseTechIsNotAHabit_AndIsNeverLogged` 테스트는 말아업어치기가 습관
목록에 **없다**는 것만 확인했고, `HabitBrainCommitSource.cs` 전체를 읽어도
체력<30을 확인하는 분기가 없었다. Jindoyun은 5개 습관 중 어느 것도 이 기술을
쓰지 않는다 — 즉 지금까지 B는 말아업어치기를 실전에서 **한 번도 던진 적이
없었다.** 설계 문서가 존재를 전제한 메커니즘이 통째로 미착수였다는 뜻이다.

이걸 그대로 두고 임준철의 안오금띄기만 "같은 방식으로" 만들면, 존재하지
않는 방식을 참조하게 된다. 임준철 것을 발화시키려면 그 방식 자체를 먼저
만들어야 했다.

## Action — 한 것

새 타입 `SurpriseTechnique`(불변, `techniqueId` + `Func<IHabitContext, bool>
condition`)을 만들고 `OpponentDefinition`에 nullable 필드로 붙였다. "경기당
1회" 플래그는 `IBrainContext.cs`의 지시대로 `HabitState`(경기당 상태)에
뒀다 — `HabitId`가 없는 변칙기는 기존 `_lastFiredTurn` 딕셔너리(HabitId로
키잉)에 넣을 자리가 아예 없어서, `_surpriseUsed` bool 하나를 따로 뒀다.

```csharp
public bool TryFireSurprise(IHabitContext ctx)
{
    if (_surpriseUsed) return false;
    if (Opponent.Surprise == null) return false;
    if (!Opponent.Surprise.Condition(ctx)) return false;
    _surpriseUsed = true;
    return true;
}
```

발화 지점은 `HabitBrainCommitSource.TryNext` 안, 사다리(`HabitLadder.Next`)를
**그대로 돌린 뒤**다. 사다리를 건너뛰지 않은 이유는 턴 계수(`BeginTurn`)가
사다리 안에 있어서다 — 변칙기 턴에 사다리를 안 부르면 그 턴만큼 턴 카운터가
안 올라가 이후 모든 쿨다운 계산이 어긋난다. 대신 변칙기가 발화하면 사다리의
결론을 버리고 변칙기 액션으로 덮어쓴다:

```csharp
var decision = _ladder.Next(_state, _habitContext);   // 항상 부른다 — 턴 계수 보존

if (_state.TryFireSurprise(_habitContext))
{
    LastDecision = default;      // 습관이 아니므로 관측 원장에 안 남는다
    LastUsedFallback = false;
    // ... 변칙기 액션으로 choice/LastTechnique 설정
    return true;
}

LastDecision = decision;
// ... 기존 습관/폴백 경로
```

`LastDecision`을 비운 것이 "사전에 관측될 수 없고 수 노트에 남지 않는다"는
계약의 전부다 — `LiveMatchRunner`가 관측 원장에 기록하는 유일한 경로가
`_resolver.NoteOpponentHabit(_bSource.LastDecision)`인데, 여기 `default`가
들어가면 원장이 아무것도 안 본다.

두 조건(사다리의 습관, 변칙기)이 같은 턴에 동시에 서면 — 예: 잡기 우세라서
습관도 발화 조건이 서 있는데 마침 체력도 30 밑인 경우 — 버려진 습관은 이미
`NoteFired`로 쿨다운이 시작된 채로 남는다. 사다리를 건드리지 않는 대가로
받아들인 부작용이다.

## Result — 결과

새 단위테스트 2개로 고정했다. `SurpriseFires_OnlyOnceEvenIfConditionKeepsHolding`은
조건이 계속 서도 두 번째 시도부터 안 나오는 것을, `SurpriseFiring_OverridesTheLadder_
AndNeverRecordsAHabitDecision`은 `HabitBrainCommitSource`를 통해 실제로
`LastTechnique.Id == "안오금띄기"`이면서 `LastDecision.Fired == false`,
`LastDecision.Habit == null`인 것을 확인한다. Play 모드에서도 진도윤전을
80교환, 임준철전을 77교환까지 몰아 각각 체력 16(<30)·밸런스 13(<50)까지
떨어뜨려도 에러 없이 정상 종료함을 확인했다.

기존 `SurpriseTechIsNotAHabit_AndIsNeverLogged` 테스트도 갱신했다 —
`HabitId` 총합이 5→14로 늘어난 게 정상이라는 것과, 그 14개 안에 두 변칙기
(말아업어치기·안오금띄기)가 여전히 없다는 것을 같이 고정했다. 하니스
257/257 그린, 42노드 지문 IDENTICAL(대조 모드는 이 경로를 안 탄다).

## 면접 예상 질문

**"왜 사다리를 건너뛰지 않고 항상 돌린 뒤에 덮어쓰나?"**
턴 계수가 사다리 호출 안에 있어서다. 변칙기가 발화한 턴만 계수를 건너뛰면
이후 모든 습관의 쿨다운이 그 경기 끝까지 한 턴씩 밀린다. 사다리 자체를 고쳐
호출자가 계수를 책임지게 바꾸는 방법도 있었지만, 그러면 `ladder.Next`를
직접 부르는 기존 테스트 다수가 계수를 두 번 세게 된다 — 공유 인프라를
건드리는 대신 호출 순서로 풀었다.

**"버려진 습관의 쿨다운이 이미 시작되는 부작용이 왜 괜찮다고 판단했나?"**
변칙기 조건(체력<30, 밸런스<50)이 소모 상태라 일반 습관 조건(그립 우세,
지도 직후)과 자주 안 겹친다. 겹치는 극단적인 경우도 그 습관의 쿨다운이
1~수 턴 일찍 시작되는 정도의 튜닝 오차지, 관측이 틀리거나 경기가 깨지는
문제가 아니다. 사다리를 건드리는 리스크가 이 오차보다 크다고 판단했다.

## 말할 가치가 있는 지점

"기존 코드/문서가 이미 구현했다고 전제한 것"을 실측으로 확인하는 습관이
없었으면 이 공백을 놓치고 지나갔을 것이다 — 지시 문구("말아업어치기와
같은 방식")를 문자 그대로 따르려면 참조 대상이 존재해야 하는데, 존재
여부 자체를 안 물었으면 안오금띄기도 똑같이 "습관 아님"이라고 주석만
달고 끝났을 것이다(발화 코드 없이). 설계 문서와 테스트 주석은 **의도**를
기록하지 **구현 여부**를 보증하지 않는다 — grep으로 실제 호출부를 찾아
확인하는 것이 그 둘을 가르는 유일한 방법이었다.
