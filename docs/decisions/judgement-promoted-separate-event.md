# 기술 결정: 판정 승격 결과를 새 이벤트로 노출했다 (기존 이벤트 필드 확장 대신)

작성 2026-08-14 · 상태: **적용 완료**

## Problem — 문제

MATCH_RULES §E(판정 승격)를 구현하면서, 승격 전 하한(floor)과 승격 후
최종값을 UI/코치 패널이 구분해서 보여줄 수 있어야 했다("판정 하한은
유효였는데, 심판은 더 크게 봤다"). 가장 직관적인 방법은 기존
`JudgementScored` 이벤트에 `FloorJudgement` 필드를 하나 추가하는 것이었다.

문제는 이 프로젝트에 정확히 같은 시도가 실패한 전례가 있었다는 것이다.
2026-08-10 세션(§H 4b 서술 인프라)에서 `JudgementScored`에 `TechniqueId`
필드를 추가했다가, 42노드 지문(그 세션 당시의 52줄 덤프)이 깨졌다 — 아무
것도 다르게 채점되지 않았는데도, 덤프 로직이 새 필드를 매 줄에 찍어버려서
지문이 달라진 것이다. 그 세션은 이 시도를 되돌리고 Game 전용 별도 배선으로
우회했다(STATUS.md 기록).

지금 하니스의 지문 덤프(`tools/dotnet-harness/Program.cs`의 `Describe()`)는
그때와 다르다 — 타입별로 손으로 짠 문자열 매핑이라, 이벤트 타입에 필드를
추가해도 `Describe()`의 `JudgementScored` case를 손대지 않는 한 지문에
안 나타난다. 즉 **그때의 실패 메커니즘이 지금도 유효한지가 불확실했다** —
추측으로 "이번엔 괜찮을 것"이라고 넘어갈 수도, 과거 실패를 그대로 믿고
무조건 회피할 수도 있었다.

## Action — 한 것

추측하지 않고 두 가지를 다 했다.

1. **`Describe()`를 직접 읽어서 확인**: `JudgementScored` case가 정확히
   어떤 필드를 문자열로 조립하는지 보고, 새 필드를 추가해도 그 case를
   같이 안 고치면 지문에 안 나타난다는 것을 코드로 확인했다. 즉 이번엔
   필드를 추가해도 안전했을 것이라는 게 확인 가능한 사실이었다.
2. **그래도 필드 확장 대신 새 이벤트를 골랐다**: `JudgementScored`는
   손대지 않고 `JudgementPromoted`(ScoredBy/Floor/Promoted, 승격이 실제로
   일어난 교환에만 발화)를 새로 만들었다.

```csharp
if (_cfg.PromotionGate)
{
    int effectiveSum = result.IsCounter ? -result.Sum : result.Sum;
    Judgement promoted = JudgementCalculator.Promote(result.Judgement, effectiveSum, _cfg, _rng);
    if (promoted != result.Judgement)
    {
        ev.Add(new JudgementPromoted(result.ScoredBy, result.Judgement, promoted));
        result = new JudgementResult(promoted, result.ScoredBy, result.IsCounter, result.Sum);
    }
}
```

이유는 세 가지였다. 첫째, "이번 하니스는 괜찮다"는 사실이 "다음에 또
괜찮다"를 보장하지 않는다 — `Describe()`가 언젠가 리플렉션 기반 범용
덤프로 리팩터링되면 2026-08-10과 같은 실패가 조용히 재현된다. 둘째,
승격이 안 일어난 절대다수의 교환에서 `JudgementPromoted`는 아예 생성되지
않는다 — `BalanceChanged`/`SetupDeclared` 같은 기존 이벤트들이 이미 쓰는
"실제로 일어났을 때만 발화" 패턴과 같은 모양이라, 코드베이스 관례에서
벗어나지 않는다. 셋째, 기본값 `PromotionGate = false`가 스크립트 경로에서
이 이벤트 자체를 절대 안 만든다 — 새 이벤트든 필드든 게이트가 이미 지문을
지켜주므로, 두 방법의 실제 위험 차이는 "지금 당장"이 아니라 "나중에 누군가
`Describe()`를 바꿀 때"에서만 벌어진다.

## Result — 결과

전용 테스트(`PromotionGate_On_PromotesTheFloor_AndEmitsTheFloorSeparately`)로
`JudgementScored.Judgement`(최종값)와 `JudgementPromoted.Floor`/`.Promoted`가
각각 맞는 값을 담는 것을 확인. 하니스 269/269(신규 12개) 그린, 42노드 지문
IDENTICAL — 필드 확장이었어도 이번엔 안 깨졌을 것을 알면서도 더 안전한
쪽을 택한 결정이라, "이번엔 안전하다"와 "이 방법이 더 안전하다"를 둘 다
검증한 셈이다.

## 면접 예상 질문

**"확인해보니 이번엔 필드를 추가해도 괜찮았다면서 왜 굳이 새 이벤트를
만들었나? 과잉 방어 아닌가?"**
비용이 거의 0이었기 때문이다. 새 이벤트 하나 추가하는 것과 기존 이벤트에
필드 하나 추가하는 것의 구현 비용은 사실상 같다 — 차이는 "지금 안전한가"가
아니라 "이 안전이 무엇에 의존하는가"였다. 필드 확장은 `Describe()`의 현재
구현 방식(타입별 수동 매핑)이라는, 언제든 리팩터링될 수 있는 세부사항에
암묵적으로 기대는 선택이었다. 같은 비용으로 그 의존을 아예 없앨 수 있으면
없애는 쪽을 골랐다.

**"과거 실패 사례를 그냥 안 믿고 새로 확인한 이유는?"**
2026-08-10 실패의 원인("덤프가 새 필드를 찍어버림")과 지금 코드의 실제
구조("타입별 수동 매핑")가 같은지 다른지는 읽어보지 않고는 알 수 없었다.
과거 기록을 맹신해서 매번 새 이벤트로만 가면, 정말 필드 확장이 맞는
상황에서도 불필요하게 이벤트를 늘리게 된다 — 반대로 안 믿고 그냥
진행하면 원인이 아직 살아있는 경우 지문이 깨진다. 둘 다 피하려면 원인
자체를 다시 추적하는 것 말고 방법이 없었다.

## 말할 가치가 있는 지점

이 프로젝트에 이미 있는 원칙("측정 없이 규칙을 확정하지 않는다",
`measurement-driven-rules.md`)을 "과거의 실패 기록을 그대로 믿을 때"에도
똑같이 적용한 사례다 — 기록을 무시하지도, 맹신하지도 않고, 그 기록이
가리키는 원인이 지금 코드에도 있는지를 직접 다시 확인했다. 결과적으로
"이번엔 안전했다"는 결론에 도달했지만, 그 결론과 무관하게 더 안전한
설계를 택했다는 점이 이 결정의 핵심이다 — 안전을 확인하는 것과 안전한
설계를 고르는 것은 별개의 판단이다.
