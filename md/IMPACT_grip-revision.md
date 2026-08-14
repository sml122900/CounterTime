# IMPACT_grip-revision.md — 잡기 개편 v4.2 영향 조사 (확정 아님)

이 문서는 **조사 결과만** 담는다. 개정안은 작업 지시(잡기 개편 1차)에 이미 수치까지
포함해 상세히 나와 있다 — 이 문서는 그 지시를 실제 코드에 매핑하고, 지시가 명시하지
않은 지점·지시와 현재 코드가 충돌하는 지점만 골라 보고한다. 조사 중 코드는 변경하지
않았다.

## 결론 먼저

- **소매·가슴깃 2종은 이미 구조가 있다.** 현재 `HandTarget`(`Enums.cs:5-7`)에
  `RightSleeve/LeftSleeve/LeftLapel/RightLapel`가 이미 존재하고, `TurnChoice.OwnHand`
  (내 손)와 `TurnChoice.Hand`(상대 목표)를 조합하면 정/교차가 **이미 표현 가능**하다
  — 새 enum이 필요 없다. UI 레이블·게이팅만 새로 짜면 된다.
- **틀어잡기(3번째 대상)는 현재 코드와 충돌한다.** 지금은 "다른 손이 이미 잡고 있어야만"
  가능한 전환(conversion) 행동(`CanUseConversion`, `GripSystem.cs:71-72`)인데, 작업
  지시의 흐름("빈 손 선택 → 대상 3택[소매깃/가슴깃/틀어잡기] → 정/교차")은 **빈 손에서
  바로 틀어잡기를 선택**하는 걸 요구한다 — 이건 애매함이 아니라 명시된 규칙 변경으로
  읽었다: 전제조건 게이트를 없앤다.
- **해결 매트릭스는 `ResolveGrip`의 구조 자체를 바꿔야 한다.** 지금은
  `ResolveGrip(A, a, b)` / `ResolveGrip(B, b, a)`를 각각 독립적으로, 무조건 성공으로
  처리한다(`MatchEngine.cs:142-143, 367-419`) — 성공/실패 확률도, 상대 참조도 없다.
  새 매트릭스("둘 다 공격=상호 성립", "공·수 갈림=10%로 공격측만" 등)는 양쪽 커밋을
  동시에 보고 확률 롤까지 도는 **완전히 새로운 해결 함수**가 필요하다. 이 변경을
  기존 enum 값(RightSleeve 등)이 지나는 경로에 무조건 걸면 **42노드 지문이 깨진다** —
  42노드는 지금 잡기 턴이 항상 무조건 성공한다는 전제로 쓰여 있다(`m3`,
  `ScenarioTurnChoices.cs`). `BalanceGate`/`PromotionGate`와 같은 패턴으로 신규
  플래그(가칭 `GripRevisionGate`, 기본 off) 뒤에 새 해결 함수를 두고, 게이트가 꺼지면
  **정확히 지금 동작**(무조건 성공, 매트릭스 없음)으로 폴백해야 지문이 산다.
- **회피 2존은 `TurnChoice`에 없는 개념이다.** 지금 회피(`HandTarget.Evade`)는 손
  슬롯 하나가 "회피 자세"를 표현하는 단일 값이고, §6.1 극단적 방어·§4 판정 어디서도
  "어느 존을 방어하는가"를 안 본다. 작업 지시의 "손마다 1존, 총 2존"은 한 턴에 존
  2개(왼손 담당/오른손 담당)를 동시에 표현해야 하는데, 지금 `TurnChoice`는 손 슬롯이
  하나뿐이다(`TurnChoice.cs:3-4`, "committing both hands in a turn is deliberately
  not offered"). **신규 필드 추가가 필요하다** — 순수 추가라 기존 생성자·42노드엔
  영향 없음(아래 c항).
- **`견고` 용어가 이미 다른 뜻으로 쓰인다.** `GripLock.Locked`가 `MatchHud.cs:104`에서
  "견고"로 표시된다(뜯기에 실패해 상대 그립이 안 풀리는 상태 — §3.2). 작업 지시의
  "견고화"(내가 이미 잡은 손을 강화하는 액션)는 다른 개념이다. `MatchHud.cs`는 스크립트
  대조 모드 전용(라이브 `LiveMatchPanel`은 `GripLock.Locked`를 이미 "잠김"으로 표시,
  `LiveMatchPanel.cs:565`)이라 실제 화면 충돌은 없다 — 문서(MATCH_RULES §3.2)의
  괄호 표기 "(견고)"만 이번 v4.2에서 다른 문구로 바꿔 혼동을 없앨 것.

**판단 필요 사항은 없다고 결론**(작업 지시가 이미 구체 수치·흐름까지 줬고, 남은 선택은
전부 "조사 결과에 따라 제안 조정 가능"으로 위임된 ⚙ 튜닝값뿐) — 아래 접점 목록대로
착수한다.

---

## a. 데이터 모델 접점

### a-1. `HandTarget` (`Enums.cs:5-7`)

현재 값: `None, RightSleeve, LeftSleeve, LeftLapel, RightLapel, Evade, TwistGrip,
BackGrab, Tear, MaintainGrip, Setup`.

- 소매(정/교차) = 기존 `RightSleeve`/`LeftSleeve` + `OwnHand` 조합. **재사용, 무수정.**
- 가슴깃(정/교차) = 기존 `LeftLapel`/`RightLapel` + `OwnHand` 조합. **재사용, 무수정.**
- 틀어잡기(정/교차) = 기존 `TwistGrip`/`BackGrab` 재사용하되, 게이트 제거(위 참조)
  + 빈 손에서 직접 선택 가능하도록 `LiveAssembler.HandOptions`/스크립트 무관 로직 수정.
  "정/교차"는 `OwnHand`와 `Hand` 값의 대응으로 표현(소매·가슴깃과 같은 패턴).
- 견고화 = **신규** `HandTarget.Solidify` 필요 (기존 값에 없음). 이미 그 손으로 뭔가
  잡고 있을 때만 선택 가능한 손 액션.
- 42노드 파서(`ScenarioTurnChoices.cs`)는 이 enum 값들을 한국어 텍스트 매칭으로
  만든다 — **신규 값(`Solidify`)을 추가해도 파서가 절대 만들어내지 않는다**(매칭
  키워드가 없으므로). 순수 추가는 안전.

### a-2. `GripKind` (`Enums.cs:10`, 현재 `None=0, Normal=1, Dominant=2`)

작업 지시의 가치표(소매1/가슴깃2/틀어잡기3, 견고+1)는 현재 3단 enum과 안 맞는다
(가슴깃=2가 현재 `Dominant`와 값이 겹치고, 틀어잡기=3은 표현 불가). **정수 값 자체를
다시 매핑해야 한다** — enum을 값 기반(`None=0,Sleeve=1,Lapel=2,BackGrip=3`)으로
확장하고 견고는 손 단위 별도 `bool`(또는 비트)로 얹는 것을 제안(아래 b항 근거).
`GripKind`를 참조하는 곳: `GripSystem.cs`(정의 지점), `MatchEngine.cs:375,384`
(`Grab` 호출), 테스트 다수(`GripSystemTests.cs`). **컴파일 파손 범위 = 이 파일들.**

### a-3. `TurnChoice` (`TurnChoice.cs`)

- `Hand`/`OwnHand`는 위 a-1 매핑으로 3+1(견고화) 액션 전부 표현 가능 — **필드 자체는
  무수정.**
- 회피 2존은 **불가능** — 신규 필드 필요. 제안: `EvadeZoneLeft`/`EvadeZoneRight`
  (nullable 존 enum, 기본 null)를 순수 추가. 기존 생성자는 그대로 두고 회피 전용
  보조 생성자 또는 `with`형 헬퍼를 추가 — 기존 15곳 이상의 `new TurnChoice(...)`
  호출부는 무수정 컴파일 유지(추가 필드 기본값 null).
- `IsExtremeDefense`/`HasHandAction`(`TurnChoice.cs:38-43`)이 `Hand ==
  HandTarget.Evade` 하나만 본다 — 2존 필드가 채워진 새 회피 표현과 병행하려면 이
  프로퍼티들의 조건에 신규 필드 존재 여부를 or 로 추가해야 한다(순수 확장, 기존
  분기 무변화).

---

## b. `GripSystem` 개편 범위

`GripSystem.cs`는 이미 손 단위(§3 시절 설계)로 배열이 짜여 있어(`_holding[2,2]`,
`_target[2,2]`, `_lockOn[2,2]`) 구조 자체는 살아남는다. 바뀌는 지점:

1. `Value(who)`(`:104-105`, 현재 `Normal(1)+Normal(1)` 식 단순 합)를 새 가치표
   (1/2/3 + 견고 +1)로 재정의. 견고 상태를 담을 자리가 없어 `_holding` 옆에
   `_solid[2,2]`(bool) 배열 신규 필요.
2. `GripScore`(`:35-40`)의 중간 캡(`Math.Min(2, Value(who))`)은 기존 최대값(2)에
   맞춰진 상수 — 새 최대값(틀어잡기+견고=4)에서 이 캡을 그대로 두면 매번 캡에
   걸려 값이 뭉개진다. 최종 클램프(`Clamp(mine-theirs, -2, 2)`)가 이미 결과를
   ±2로 묶으므로, 중간 캡은 **제거하거나 새 최대값(4)으로 올리는** 두 선택지 중
   하나 — 최종 출력 범위(±2, `JudgementContext`/`EffectiveGrip` 소비자 전부가
   이 범위를 전제)는 안 바뀐다는 걸 테스트로 고정할 것.
3. `CanUseConversion`(`:71-72`)은 이번 개편으로 **호출부가 사라진다**(틀어잡기가
   더 이상 "conversion"이 아니라 1차 목표 선택이 되므로) — 삭제할지 그대로
   둘지는 "사용자 요청 없는 리팩터 금지" 원칙상 **호출부만 정리하고 메서드 자체는
   당장 안 지운다**(죽은 코드로 남기고 보고, CLAUDE.md §3 "코드 정리는 자기 몫만").
4. 신규 `TryUseCrossGripTiming`류 API 불필요 — 교차잡기 롤링 타이머(2턴)는 그립
   자체가 아니라 **경기 상태**(교환 카운터)라 `MatchEngine`에 신규 필드로 두는
   게 맞다(`_setupActive` 패턴과 동일 위치).

---

## c. `MatchEngine.ResolveGrip`/`ResolveExchange` 개편 범위

- `ResolveGrip`(`MatchEngine.cs:367-419`)은 지금 **한 선수씩 독립 호출**(`:142-143`)
  된다. 새 매트릭스는 양쪽 커밋을 동시에 봐야 하므로 시그니처를
  `ResolveGripExchange(TurnChoice a, TurnChoice b, ev)` 하나로 합치고 내부에서
  양쪽을 함께 판정하는 편이 매트릭스 표(같은 쌍/다른 손/방어존 등)를 한 곳에
  담기 쉽다. **게이트 설계**: `if (!_cfg.GripRevisionGate) { 기존 ResolveGrip(A)/
  ResolveGrip(B) 그대로 호출; return; }` 형태로 완전히 분리 — 기존 코드 경로는
  단 한 줄도 안 바뀐 채로 남아야 지문이 산다(`BalanceGate`가 이미 증명한 패턴,
  `MatchEngine.cs:512-514`의 §4.4 강등 분기와 동일한 "게이트 뒤 완전 별도 로직"
  구조 재사용).
- 교차잡기 롤링 타이머(2턴, 흔들기/기술 없으면 지도)는 `_setupActive`처럼
  `MatchEngine`에 `_crossGripTurns[2]`(int, 잔여 턴) 신규 필드로 구현 가능 —
  매 `ResolveExchange` 끝에서 감소, 흔들기/기술 선언 시 리셋, 0 도달 시
  `Referee.AddShido` 직접 호출(§6.2 경로 재사용) 또는 신규 지도 사유. **게이트
  뒤에서만** 동작해야 함(§6 사다리 자체는 무수정 유지 조건).
- 방어존 판정(§2 "방어된 존 공격: 확률 0")은 `ResolveTechnique`이 아니라
  `ResolveGripExchange` 쪽에서 상대의 회피 존 필드(위 a-3)를 읽어 목표가
  그 존과 겹치면 그립 자체가 성립 안 하는 것으로 처리 — 기술 판정식(§4)엔
  손을 안 대는 것으로 충분(§4는 `Hand`를 안 읽음, `IMPACT_input-model-
  revision.md` a-3 재확인 — 이번 조사에서도 `JudgementCalculator.
  IsDefensivePosture`가 여전히 `Hand`를 안 읽는 것 재확인).
- 확률 롤(⚙10% 등)은 `IRandomSource.NextFloat()` 기존 규약 그대로 — 생성자에서
  `IRandomSource rng`를 이미 `MatchEngine`이 들고 있다(`:43,87`), 신규 주입
  불필요.

---

## d. 42노드 지문 안전성 (핵심 확인)

- `ScenarioTurnChoices.HandOf`(`ScenarioTurnChoices.cs:53-64`)는 한국어 키워드
  매칭으로 기존 enum 값만 만든다 — 신규 값(`Solidify`)이나 신규 필드(회피 2존)를
  **절대 채우지 않는다**(매칭 키워드 없음). 42노드 JSON 원문에 "견고화"/2존 관련
  문구가 없음을 확인(grep 0건).
- 위험은 오직 `MatchEngine.ResolveGrip`/`ResolveGripExchange`의 **동작 자체가
  바뀌는 것**이다 — 이건 게이트(`GripRevisionGate` 기본 off)로 막는다. 게이트가
  꺼지면 `ResolveGrip`은 지금 코드와 바이트 단위로 동일해야 한다(새 로직을 아예
  안 타는 방식, `if(gate) 새로직 else 기존그대로`).
- 하니스 지문 덤프(`tools/dotnet-harness/Program.cs`의 `Describe(MatchEvent)`)는
  인식 못 하는 이벤트를 타입명으로 그냥 찍는다(§H 3단계 세션이 이미 실측 확인한
  함정) — 신규 이벤트(예: `GripResolved`, `CrossGripTimerExpired`)를 게이트 없이
  아무 데서나 `ev.Add`하면 42노드 지문이 깨진다. **신규 이벤트는 전부 게이트
  안쪽에서만 생성.**

---

## e. Brain 접점

`HabitCatalogue.cs`의 `SeekGrip`(`:70-91`, `SeekGripRightSleeve`/`SeekGripLeftSleeve`)
과 `HabitBrainCommitSource.Fallback`(`:190-216`, `_nextGripHand` 좌우 교대)은 전부
기존 `HandTarget.RightSleeve/LeftSleeve` + `OwnHand`만 쓴다 — **컴파일 안 깨짐**
(신규 enum 값 무관). 다만 작업 지시의 "B도 동일 규칙 적용(교차 타이머 포함)"을
만족하려면:
- `HabitCatalogue`가 만드는 `TurnChoice`가 이제 새 매트릭스(양쪽 동시 참조)를
  통과해야 하므로, B의 그립 시도도 게이트 on일 때는 무조건 성공이 아니라 매트릭스
  결과를 받는다 — **코드 추가 불필요**(게이트가 `ResolveGripExchange` 안에서
  A/B 구분 없이 동일하게 적용되므로), 다만 스모크·회귀 테스트에서 B 쪽 관측치가
  달라질 수 있다는 점만 인지.
- 교차잡기(같은측) 여부 판정도 A/B 대칭으로 이미 `OwnHand`+`Hand` 조합만 보면 되므로
  Brain 쪽 신규 분기 불필요.
- **행동 품질 개선 안 함**(지시 명시) — `SeekGrip`이 새 3택 중 "가슴깃"/"틀어잡기"를
  전략적으로 고르게 하는 것은 이번 범위 밖, 기존 소매 타겟팅 패턴 유지.

---

## f. 회귀 영향 규모 (컴파일 파손 기준)

`new TurnChoice(` 직접 생성 132개(§`IMPACT_input-model-revision.md` e항 표 재사용,
이번 조사에서 재확인 — 파일 구성 무변화) — **필드를 순수 추가**(기본값 있는 신규
파라미터 또는 별도 생성자)로 가면 이 132개는 전부 무수정 컴파일 유지된다. `GripKind`
enum 값 재정의(a-2)는 `GripSystemTests.cs`(14개)의 기댓값을 깨뜨릴 수 있어 재확인 필요
— 컴파일은 안 깨짐(값 이름 유지하며 정수만 바뀌는 방향으로 설계 가능).

## 조사 중 확인 못 한 것

- HUD 쪽 정확한 문구·배치(§1 항목 "손별 그립 상태 양측 표시 + 교차 타이머 잔여")는
  `LiveMatchPanel.cs`의 기존 상태줄 조립 로직을 그대로 확장하는 것으로 충분해 보이나,
  실제 좌표·겹침은 구현 후 Play 실측 필요(이번 조사는 코드 리딩만, 이전 세션들의
  선례와 동일하게 사람 확인 항목으로 남긴다).
