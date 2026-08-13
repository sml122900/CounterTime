# Plan — 소극 3인(문준배·서도현·오재훈) 차별화, 코드 접점 조사

> 조사 문서, 구현 아님. 2026-08-14(밤) 야간 자율 실행. 절대선: 습관 방향·⚙
> 수치는 전부 사용자 결정 — 이 문서는 "무엇이 가능한가"만 나열하고
> "무엇을 골라야 하는가"는 정하지 않는다. 코드 변경 0.

## 문제 재확인 (v3 데이터, 2026-08-13 밤 세션)

6상대×500경기 행동 벡터 분석(`docs/overnight-2026-08-13-v3.md`)에서 문준배·
서도현·오재훈 셋이 나머지 대비 서로 가장 안 갈린다 — 특히 문준배-오재훈이
15쌍 중 최근접(거리 0.2388, 15쌍 평균 0.6731). 원인은 셋 다 §4 3단 폴백
(정의상 큰기술 없음)에 크게 의존하는 저작이라, 능동 습관이 뜨는 턴 수 자체가
적어서 행동이 "무공격"으로 수렴하기 때문으로 보인다(v3 §1 데이터, 판단 아니고
관측). 목표: 셋에 능동 습관을 **추가**해 §4 폴백 의존도를 낮추고 서로
갈리게 한다 — **어떤 습관을, 몇 개, 얼마나 자주**는 전부 사용자 몫.

## 저작 패턴 (grep/Read 로 확인, 6상대 전부 동일 구조)

한 상대 = `Assets/_Project/Scripts/Brain/<이름>.cs` 파일 하나, 내용은 항상:
```csharp
public static class <이름>
{
    public static readonly HabitId[] Habits = { HabitId.X, HabitId.Y, ... };

    public static OpponentDefinition Create(MatchRulesConfig cfg)
    {
        var habits = new HabitDefinition[Habits.Length];
        for (int i = 0; i < Habits.Length; i++) habits[i] = HabitCatalogue.Build(Habits[i], cfg);
        return new OpponentDefinition("<표시이름>", habits, scoutingLevel: 0);
    }
}
```
**능동 습관을 하나 추가하는 절차는 정확히 세 단계**(전부 확인함, 문준배로 예시):
1. `Assets/_Project/Scripts/Brain/HabitDefinition.cs`의 `enum HabitId`에 새 값
   추가 (예: `MoonJunbaeSeeksGrip`).
2. `Assets/_Project/Scripts/Brain/HabitCatalogue.cs`의 `Build(HabitId id, ...)`
   switch 문에 그 값의 case 추가 — 조건(`Func<IHabitContext, bool>`)과
   `HabitAction`(TurnChoice + 선택적 TechniqueSpec)을 코드로 정의.
3. `MoonJunbae.cs`의 `Habits` 배열에 그 `HabitId` 추가.

**순서가 규칙이다** — `HabitCatalogue.Build`의 case 순서가 아니라, 배열 안
습관들의 발화 우선순위는 `HabitLadder`가 배열 순서대로 배타적으로 평가한다
(기존 주석: "좁은 조건이 넓은 조건보다 먼저" — 서도현 H1/H2, 진도윤 H1/H2가
이미 이 원칙을 지킴). 새 습관을 몇 번째에 넣을지는 조건의 좁고 넓음에 달려
있어 이것도 저작 판단이지만, 기계적 절차 자체는 "배열 순서 = 사다리 순서"로
고정돼 있다.

## 재사용 가능한 조건 축(`IHabitContext`, 5개 멤버 — 전수 확인)

| 멤버 | 뜻 | 이미 쓰는 상대 |
|---|---|---|
| `OwnEffectiveGrip` | 내 유효 잡기 −2..+2 | 진도윤 H1/H4/H4b, 문준배 H, 임준철 H1(둘 다), 오재훈 H(둘 다) |
| `TurnsSinceOwnShido` | 내가 지도 받은 뒤 교환 수(null=아직 없음) | 진도윤 H2, 임준철 H2 |
| `OwnStamina`/`OpponentStamina` | 내/상대 체력 | 진도윤 H3(예비) |
| `OpponentSetupActive` | 상대가 흔들기(Setup) 중인가 | 서도현 H1 |
| `OwnBalance` | 내 밸런스(§S) | 서도현 H2, 임준철 변칙기 |

**`OpponentBalance`는 `IHabitContext`에 없다** — `IBrainContext`(§4 사다리
1/3단, 게이트·폴백 전용) 멤버다. 습관 조건이 상대 밸런스를 보려면(예: "상대
밸런스가 낮으면 공격") `IHabitContext`에 새 멤버를 추가해야 한다 — 이미
`OpponentSetupActive`/`OwnBalance`가 서도현/임준철을 위해 이렇게 늘어난
전례가 있다(§H 3.5b 로스터 세션). 관문 테스트
(`LiveReadResolverTests.HabitConditionsNeverSeeTheBrainContext_OnlyIHabitContext`)
가 멤버 수를 직접 세므로, 새 멤버를 추가하면 그 테스트도 같이 갱신해야 한다
— 절차는 이미 한 번 밟힌 적 있어 새로 개척하는 길은 아니다.

## 지문 위험 — 구조적으로 0

`Assets/_Project/Scripts/Game/Match/MatchRunner.cs`(42노드 스크립트 경로가
도는 실제 파일)를 grep으로 확인 — `MoonJunbae`/`SeoDohyun`/`OhJaehoon`/
`HabitCatalogue`/`OpponentDefinition` 참조가 **0개**. 스크립트 경로는
`ScriptedCommitSource`/`ScriptedAssembler`만 쓰고 Brain 패키지의 상대 팩토리를
아예 모른다. 즉 이 세 상대의 습관을 몇 개를 어떻게 바꾸든 42노드 지문은
**구조적으로** 안 건드린다(조건부 게이트 필요 없음 — §H4/§H5/§H6 계획서들과
달리 이 클러스터는 지문 위험 자체가 없는 드문 경우). 영향 범위는 라이브 대회
경로(`LiveMatchRunner`가 이 세 상대를 상대로 고를 때)와 하니스의 v3 오버나이트
시뮬레이터(`tools/dotnet-harness/Overnight/*`, 습관 발화를 실측하는 코드)뿐.

## 상대별 선택지 (코드 관점, 2~3개씩 — 방향 선택 안 함)

### 문준배 (16강, ROSTER "안정형 수비") — 현재 습관 1개(CounterThrowOnGripEdge, 35%/쿨다운5)

- **선택지 1 — 그립 고집 (비공격 습관, `SeekGrip` 패턴 재사용).** 조건
  `OwnEffectiveGrip < 1`, 기술 없음(잡기 액션만) — 진도윤 H4/H4b와 완전히
  같은 코드 모양, `HabitCatalogue.cs`의 기존 `SeekGrip(...)` 정적 헬퍼를
  그대로 재호출 가능(부위만 다르게, 예: 깃 계열). **새 `IHabitContext` 멤버
  불필요.** 발화가 그립 표적 분포로 관측 가능해서(v3 §1이 이미 그립 표적
  분포를 계측했다) "구별 안 됨" 문제에 직접 대응하는 축.
- **선택지 2 — 기존 예비 습관 재활용(`HabitId.TurtleWhenTired`).** 코드는
  이미 완성돼 있다 — `MoonJunbae.Habits` 배열에 한 줄 추가하는 것만으로
  끝난다(새 case 불필요). **단서**: 이 `HabitId`는 진도윤의 예비 습관으로
  저작된 것이라 이름·주석이 그 맥락을 담고 있다(폐기 시 점등하는 예비
  체계와 얽힘 — `HabitState`가 경기마다 새로 생성돼 상대 간 충돌은 없지만,
  "진도윤 전용"이라는 서사적 전제를 문준배가 빌려 쓰는 모양이 된다). 코드
  장애물은 없고 저작 판단만 남는다.
- **선택지 3 — 카운터 준비 상시화(`HabitId.CounterReadyOnOpponentSetup` 재사용).**
  서도현이 이미 쓰는 정의를 문준배 배열에도 추가 — 코드는 0줄, 완전
  무료. **다만 목표(차별화)와 정면으로 충돌할 수 있다** — 서도현과 문자
  그대로 같은 습관을 공유하게 되므로, 완전성을 위해 나열하되 방향으로는
  추천 안 함(추천 안 하는 이유까지 명시하는 게 "선택지만 나열, 방향은
  안 정함" 원칙에 맞다고 판단).

### 서도현 (8강, ROSTER "카운터 압박") — 현재 습관 2개(CounterReadyOnOpponentSetup 70%, StayDefensiveWhenBalanceHigh 75%, 조건 없이 쿨다운도 없음)

이미 셋 중 가장 많은 습관을 가졌는데도 v3에서 가장 안 갈린다 — 두 조건
(상대가 흔들기 중 / 내 밸런스 온전) 다 상대적으로 좁아서 실전에 잘 안 서는
것으로 추정된다(관측, 확정 아님). 그래서 후보들은 **더 자주 서는 조건**
위주로 골랐다.

- **선택지 1 — 그립 우위 시 진짜 반격(`OwnEffectiveGrip >= 1` 패턴).** 문준배·
  임준철·오재훈이 이미 쓰는 가장 검증된 조건. 기술 이름만 다르게 고르면
  끝(어떤 기술을 쓸지는 §B 로스터·서사 판단, 여기서 안 정함). **새 멤버
  불필요.**
- **선택지 2 — 지도 후 만회(`TurnsSinceOwnShido < 2` 패턴).** 진도윤 H2/임준철
  H2와 같은 코드 모양. **새 멤버 불필요.**
- **선택지 3 — 쿨다운 교대 2지선다 추가(임준철/오재훈 패턴).** 기존 조건
  (예: `OwnEffectiveGrip >= 1`) 하나에 쿨다운 2로 묶인 두 기술을 나란히
  둬서 "번갈아"를 근사 — 새 `HabitId` 2개 + case 2개, **새 멤버 불필요**,
  다양성(관측 가능한 기술 종류)이 한 번에 2개 늘어난다.

### 오재훈 (동메달전, ROSTER "만능형, 특출난 축 없이 고루") — 현재 습관 2개(MixedBigThrowA/B, 50%/쿨다운2 교대)

ROSTER 자체가 "특출난 축 없음"을 원형으로 못박아서, 차별화와 원형 유지
사이에 내재된 긴장이 있다 — 이건 판단이 아니라 사실만 짚어둔다.

- **선택지 1 — 그립 열세 축 추가(비공격 습관).** 현재 두 습관 다
  `OwnEffectiveGrip >= 1`(그립 우위) 조건이라, 그립이 안 서면 오재훈은 이번에도
  §4 폴백으로 떨어진다. 그 반대(`< 1`)에 `SeekGrip` 스타일 습관을 하나
  추가하면 그립 우위/열세 **양쪽 다** 활동성이 생긴다 — "고루 하는" 원형과
  방향이 맞을 수 있다(추천은 아님, 방향이 서로 안 부딪히는 조합이라는
  사실만). **새 멤버 불필요.**
- **선택지 2 — 지도 후 견제(`HabitId.FeintAfterShido` 재사용).** 임준철이
  이미 쓰는 정의를 오재훈 배열에 추가 — 코드 0줄. 문준배 선택지3과 같은
  종류의 트레이드오프(무료지만 임준철과 습관을 공유하게 됨)를 진다.
- **선택지 3 — 상대 밸런스 기반 습관(신규 `IHabitContext.OpponentBalance`
  필요).** "상대 밸런스가 낮으면 공격"류 — `IBrainContext`엔 이미
  `OpponentBalance`가 있지만 습관 조건(`IHabitContext`)엔 없어서 **새 멤버
  추가가 필요한 유일한 선택지**(위 "재사용 가능한 조건 축" 절 참조). 절차
  자체는 서도현/임준철 때 이미 한 번 밟힌 전례가 있어 낯선 작업은 아니다 —
  관문 테스트 갱신 포함.

## 의존관계 / 순서

- 세 상대 전부 서로 독립 — 한 명만 먼저 손대도 무방.
- 새 `IHabitContext` 멤버가 필요한 선택지(오재훈 선택지3 등)는 그 멤버를
  먼저 추가해야 다른 상대가 같은 멤버를 재사용할 수 있다 — 순서상 그 멤버가
  필요한 선택지를 고르는 사람이 사실상 먼저 인프라를 놓는 셈이다.
- 습관 수 증가는 v3 오버나이트 시뮬레이터(`tools/dotnet-harness/Overnight/`)
  재실행으로 실측 검증 가능 — 기존 6상대×500경기 파이프라인이 이미 있어
  재측정 비용은 낮다(다음 세션이 방향을 정하면 바로 재수집 가능).

## ★ 사용자 결정 필요 목록 (정하지 않음)

1. **세 상대 각각 어느 선택지(또는 새 조합)를 쓸지** — 이 문서의 유일한
   진짜 목적. 문준배 1~3, 서도현 1~3, 오재훈 1~3 중 각각 무엇을, 몇 개.
2. **문준배 선택지2/3, 오재훈 선택지2처럼 "기존 HabitId를 상대 간 공유"
   하는 것이 허용 범위인가** — 코드는 무료지만 차별화 목표와 부분적으로
   충돌한다. 방향성 질문이라 여기서 안 정함.
3. **오재훈: "특출난 축 없이 고루"라는 ROSTER 원형과 "더 갈리게 하라"는
   이번 목표가 어디까지 공존 가능한가** — 오재훈만 다른 둘과 달리 원형
   자체가 무색채를 요구해서, 차별화의 "정도"가 더 조심스러운 질문이 될 수
   있다.
4. **⚙(발화확률·쿨다운) 수치** — 전부 이번 문서에서 제안조차 안 함, 기존
   6상대 값들(0.35~0.90 범위, 쿨다운 2~5)을 참고 범위로만 남겨둔다.
5. **새 `IHabitContext` 멤버가 필요한 선택지를 고를 경우, 그 멤버를 몇 명이
   공유할지** — 예: `OpponentBalance`를 습관 조건에 연다면 오재훈만
   쓸지, 나중에 다른 상대도 재사용할지(재사용 자체는 무료이므로 순수
   저작 판단).
