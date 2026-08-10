# 기술 결정: 습관 관측 알림의 호출 시점을 결정자에서 제출자로 옮긴다

작성 2026-08-11 · 상태: **적용 완료**

## Problem — 문제

`HabitBrainCommitSource.TryNext`는 사다리(`HabitLadder`)가 이번 턴 결정을 내리는
즉시 `LiveReadResolver.NoteOpponentHabit(decision)`을 불렀다. 이 코드는 "결정
시점 = 그 결정이 교환에 제출되는 시점"이라는 암묵의 가정 위에 서 있었다 — 처음
그렇게 짰을 때는 실제로 같은 시점이었으니 문제가 없었다.

가정이 깨진 건 §4.4 [초안] 쿠즈시를 넣으면서다. `KuzushiPrecedingCommitSource`가
`HabitBrainCommitSource`를 감싸 "이미 나온 결정을 한 턴 버퍼링해서 늦게 내보내는"
데코레이터로 들어왔다. 감싸인 안쪽에서는 여전히 결정 즉시 알림이 나가는데,
바깥에서 보이는 결정(쿠즈시 설치 교환)은 그 알림이 가리키는 진짜 기술 결정과
다른 교환이다. 결과: A의 읽기 레이어가 쿠즈시 설치 교환에서 이미 다음 수를
알고, 정작 그 수가 나오는 교환에는 새 알림이 없다.

관측값 자체는 틀리지 않았다(같은 `HabitDecision` 인스턴스) — **틀린 건 시점**이다.
에러도, 테스트 실패도 없었다. 야간 세션이 코드를 읽다가 발견했지만
"사다리·카탈로그는 수정 금지"라는 그날 밤의 범위 제약 때문에 판단 대기로
남기고 넘어갔다.

## Action — 한 것

사람이 아침에 "사다리 로직·카탈로그 무수정, 호출 위치만" 옮기라고 결정했다.
`HabitBrainCommitSource`에서 `LiveReadResolver` 필드와 생성자 파라미터를
통째로 제거했다 — 이 클래스는 더 이상 "언제 알릴지"를 모른다. 대신 이미
있던 `LastDecision` 프로퍼티로 "이번 호출의 결정이 무엇인지"만 노출한다.

알림 호출은 그 결정이 **실제로 소비되는 지점**으로 옮겼다 — `LiveMatchRunner.SubmitA`와
`PlayModeSmokeTests.RunMatch` 둘 다, `TryNext` 직후·읽기(`resolver.For`)를 계산하기
직전에 `resolver.NoteOpponentHabit(source.LastDecision)`을 부른다.

```csharp
_bSource.TryNext(out _, out var b);
_resolver.NoteOpponentHabit(_bSource.LastDecision);   // 결정 시점이 아니라 제출 시점
...
ReadLevel read = attacker.HasValue ? _resolver.For(attacker.Value, a, b) : ReadLevel.Neutral;
```

쿠즈시 데코레이터는 한 글자도 안 건드렸다 — 소비자가 그 교환에 실제로 반환된
`LastDecision`(설치 교환엔 `default`, 진짜 기술 제출 교환엔 실제 결정)을 그대로
넘기기만 하면, 데코레이터가 이미 정확히 계산해두고 있던 값이 자동으로 맞는
시점에 도착한다.

## Result — 결과

새 테스트(`KuzushiPrecedingCommitSourceTests.ObservationTiming_NoHabitNoteOnKuzushiExchange_NoteArrivesOnRealSubmission`)가
쿠즈시 설치 교환에서는 `resolver.For(FighterId.B, ...)`가 `Neutral`(확정 관측
3회가 있어도 안 읽힘), 다음 교환에서는 `GotRead`가 나오는 것을 실제 반환값으로
고정했다. 202/202 그린, 지문(fingerprint) IDENTICAL — 대조 모드는 이 경로 자체를
안 탄다. `HabitLadder.cs`·`HabitCatalogue.cs`는 diff 0줄.

## 면접 예상 질문

**"왜 `HabitBrainCommitSource`가 알림을 직접 안 부르게 했나?"**
그 클래스는 자기 반환값이 언제 실제로 쓰이는지 모른다 — 데코레이터가 감쌀 수
있다는 걸 그 클래스 내부에서는 알 방법이 없다. 부수효과의 타이밍을, 그
부수효과가 언제 유효한지 모르는 곳에 두면 누가 감싸는 순간 조용히 깨진다.
"언제 제출되는지 아는 쪽"이 알림 책임을 져야 한다 — 소유권을 결정자에서
소비자로 옮긴 것이 이 수정의 전부다.

**"이게 왜 테스트로 안 잡혔나?"**
기존 테스트들은 전부 "쿠즈시가 안 켜진" 경로 또는 "감싸지 않은" 경로만
확인했다. 데코레이터 자체의 버퍼링 테스트는 있었지만, 그 버퍼링이
`NoteOpponentHabit` 호출 시점에 미치는 영향까지 확인하는 테스트는 없었다 —
두 관심사(무엇을 버퍼링하는가, 그 버퍼링이 부수효과 타이밍에 무슨 영향을
주는가)가 서로 다른 파일에 걸쳐 있어서 어느 한쪽 테스트도 이 틈을 못 봤다.

## 말할 가치가 있는 지점

이 버그는 실행 계측이 아니라 **설계 재검토**로 잡혔다 — 08-10 세션의 "계측이
전제를 깨뜨렸다"(스모크 실행 결과가 이상해서 원인을 팠다)와는 다른 종류다.
여기는 200개 테스트가 전부 그린인 채로, 코드를 다시 읽다가 "이 부수효과가
호출되는 시점이 이 클래스의 책임 범위 밖 사건(데코레이터가 감싸는지 여부)에
의존한다"는 것을 발견한 경우다. 실행 결과로는 안 드러나는 클래스지만
정적으로 읽으면 드러나는 결함이 있다는 뜻이고, 그래서 "계측 우선"과 "설계
재검토"를 둘 다 정기적으로 해야 한다는 근거로 쓸 수 있다.
