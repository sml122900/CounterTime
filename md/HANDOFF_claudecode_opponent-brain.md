# 클로드 코드 작업 지시 — 상대 브레인 + 굳히기 (2026-08-09)

선행 문서: `md/MATCH_RULES.md` (v3.3, 스탬프 `9b466ae`) / `md/OPPONENT_BRAIN.md`
(v1.0, 스탬프 `acf2c10`) / `md/CORE_DESIGN_MatchEngine.md` (v3.2, `a24d23c`).
**규칙 충돌 시 언제나 `MATCH_RULES.md`가 이긴다. 문서를 코드에 맞추지 말 것.**

## 전 단계 공통 규칙

- **단계마다 커밋하고 멈춰서 보고한다.** 다음 단계로 자동 진행 금지.
- **판단이 필요하면 정하지 말고 보고한다.** 문서에 없는 값·구조를 임의로 정하지 않는다.
- `CounterTime.Core`는 `UnityEngine` 참조 0을 유지한다. 브레인은 Core에 넣지 않는다.
  예외: **굳히기(§9)는 규칙이므로 Core에 들어간다.**
- `default(TurnChoice)` 금지 — enum 0이 `Aggressive`·`Attack`이다. `TurnChoice.Neutral`을 쓴다.
- 규칙 파생은 한 곳에서만. 인라인 재파생 금지 (`IsDefensivePosture`,
  `DefenseQualityFor`가 이미 그 이유로 단일화돼 있다).
- **대조 모드(스크립트 경로)를 지우거나 대체하지 않는다.** 인터페이스 뒤로 옮기기만 한다.
- `Assets/_Project/Spikes/`는 참조 금지·삭제 금지.
- 기존 러너 규칙 유지: 자세는 다음 선언 또는 그쳐까지 / 손 행동은 선언 턴 1회만 실행
  (`HasHandAction = Hand != None && Hand != Evade`) / 행동 없는 노드는 교환 미소비 /
  선언된 적 없는 선수는 소극 누산 0.

각 단계 완료 조건에 공통으로 포함: **Unity Test Runner EditMode Run All 113개 이상 전부 초록**,
**대조 모드 메인라인이 m36에서 sum 7 / 한판 / winner=A**.

---

## 0. 문서 반영 (코드 변경 없음)

1. `md/MATCH_RULES.md`를 v3.3으로 교체, `md/OPPONENT_BRAIN.md` 추가.
2. `tools/githooks/pre-commit`의 대상 경로 목록에 **`md/OPPONENT_BRAIN.md`를 추가**한다.
   훅이 하드코딩 목록을 쓰기 때문에 추가 안 하면 조용히 스킵된다.
3. `git config core.hooksPath tools/githooks`가 이 클론에 걸려 있는지 확인.
4. 커밋 후 두 파일의 1행 스탬프가 **`9b466ae` / `acf2c10` 그대로인지** 확인한다.
   값이 바뀌면 본문이 훼손된 것이므로 **정지하고 보고**.

커밋: `docs: MATCH_RULES v3.3 (osaekomi §9, read source) + OPPONENT_BRAIN v1.0`

---

## 1. 모드 격리 — 인터페이스 도입 (동작 변경 0)

새 asmdef **`CounterTime.Brain`** 을 만든다. 순수 C#, `CounterTime.Core`만 참조,
`UnityEngine` 참조 0. 이유: Game 테스트는 Unity Test Runner에서만 도는데 브레인은
dotnet 하니스로 빠르게 돌려야 한다. ScriptableObject 래퍼는 Game에 둔다
(`TechniqueSpec` ↔ SO 래퍼와 같은 패턴).

```
Assets/_Project/Scripts/Brain/          ← 신규, CounterTime.Brain
  ICommitSource.cs        TurnChoice Next(...)
  IReadResolver.cs        ReadLevel For(...)
  ScriptedCommitSource.cs 기존 노드 경로를 감싸기만
  ScriptedReadResolver.cs 노드 [판정식] 줄을 읽던 기존 코드를 감싸기만
Assets/_Project/Scripts/Brain/Tests/    ← CounterTime.Brain.Tests
```

**이 단계에서 로직을 옮기거나 고치지 않는다.** 기존 러너가 인터페이스를 경유하게만 한다.
동작이 1비트라도 바뀌면 잘못한 것이다.

커밋: `refactor(brain): route scripted runner through ICommitSource/IReadResolver`

---

## 1.5 조립기 추출

현재 `ExchangeScript` 조립기(`For(node) → IReadOnlyList<ExchangeScript>`)를
`ScriptedAssembler`로 개명·격리하고 `IExchangeAssembler` 인터페이스를 뽑는다.
m14 4교환 전개 규칙(제목에 `·`로 기술 2개 이상 나열되면 전개)은 그대로 유지.

**`MatchEngine`·`ExchangeScript`의 시그니처는 건드리지 않는다.** 이 단계가 서야
3·4가 엔진을 안 고치고 꽂힌다.

커밋: `refactor(brain): extract IExchangeAssembler / ScriptedAssembler`

---

## 2. 습관 데이터 구조 + 관측 원장

`CounterTime.Brain`에 작성. `OPPONENT_BRAIN.md` §4, §4.1, §4.2를 그대로 구현한다.

- `HabitDefinition` — 조건 / 행동 / 발화확률(기본 **0.75**) / 피해 카운터 / 예비 여부
- `OpponentDefinition` — 습관 목록 + `ScoutingLevel`(int 0–3)
- `ObservationLedger` — 습관별 관측 카운터.
  **조건이 성립했고 B가 실제로 그 습관을 실행한 턴만 +1.** 조건만 성립한 턴은 0.
- 진도윤 데이터: H1(자기 그립 ≥ +1 → 업어치기) / H2(지도 받은 뒤 2턴 내 → 발기술) /
  H3(예비, 폐기 시 점등: 체력 열세 → 극단적 방어)
- **말아업어치기는 습관이 아니다.** 체력 <30 1회 한정 하드 규칙, 관측·기록 대상 아님.
- 난수는 반드시 `IRandomSource` 주입. `UnityEngine.Random` 금지.

테스트: `LadderNeverSums_ExactlyOneHabitFiresPerTurn`,
`HabitFiresAtSeventyFivePercent_NotAlways`,
`TwoDamagingHits_RetiresHabit_AndLightsReserve`, `RetiredHabitNeverFiresAgain`,
`SurpriseTechIsNotAHabit_AndIsNeverLogged`,
`ObservationRequiresHabitActuallyFired_NotJustConditionHeld`

커밋: `feat(brain): habit definitions + observation ledger`

---

## 3. LiveReadResolver + HabitBrainCommitSource

`OPPONENT_BRAIN.md` §2(A 공격 시 read) / §3(B 공격 시 성향 창) / §5(ScoutingLevel).

금지 사항 둘, 둘 다 조용히 틀리는 종류다:
- **`§4.2` CounterReady step-down을 리졸버가 선적용하지 말 것.** Core가 소유한다.
- **ReadLevel을 다른 곳에서 다시 파생하지 말 것.** `IReadResolver` 한 곳뿐.

`ScoutingLevel`은 필드로 구현하되 진도윤은 **0**으로 출하한다.

테스트: `HabitMiss_YieldsNeutral_NotGotRead`,
`ThirdObservation_PromotesToFullRead_SecondDoesNot`,
`ResolverNeverPreAppliesCounterReadyStepDown`, `FourOfSixSamePair_GivesRead`,
`ThreeConsecutive_GivesFullRead`, `WindowIsSixTurns_SeventhTurnDropsOut`,
`ScoutingLevel0_StartsEmpty`

커밋: `feat(brain): live read resolver + habit-driven opponent commits`

---

## 4. 결과 노드 격하

결과 노드를 **선택지로 띄우지 않는다.** `(Judgement, technique) → 서술 문구` 사전으로
격하하고, Game은 Core의 `JudgementScored{sum, terms[]}`를 받아 문구를 고른다.

- **시나리오 JSON은 이 단계에서 고치지 않는다.** 개정은 별건이다.
- 대조 모드는 기존대로 결과 노드를 쓴다. 격하는 플레이 모드에만 적용.

커밋: `feat(game): result nodes become presentation only; engine judgement is truth`

---

## 5. 굳히기 — `MATCH_RULES` §9

**여기만 Core에 들어간다.** 기존 `MatchPhase.Osaekomi`·`OsaekomiTick`을 쓴다.

§9.1 진입 게이트 / §9.2 `adv`(진입 시 1회 계산, **중간 재계산 금지**) /
§9.3 5초마다 2지선다 / §9.4 탈출 시 그쳐 없이 서기 복귀·잡기 전부 Free·자세 초기화·
matte 회복 없음.

**착수 전에 확인하고 보고할 것**: 현재 `MatchEngine`에 굳히기 진입 훅이 있는지.
없으면 어디에 뚫을지는 **정하지 말고 보고**한다.

테스트: `Yuko_WithoutGripEdge_DoesNotEnterOsaekomi`, `WazaAri_AlwaysEnters`,
`AdvFrozenAtEntry_DefenderStaminaDropDoesNotRaiseIt`,
`ScoreAwardedBeforeEscapeResolves_AtFiveSeconds`,
`EscapeResetsGripsToFree_ButGrantsNoMatteRecovery`

커밋: `feat(core): osaekomi entry gate and escape per MATCH_RULES §9`

---

## 6. A 전용 입력 UI

- 손: 오른소매 / 왼소매 / 왼가슴 / 오른가슴 / 회피 (한 손만)
- 손 조건부: 틀어잡기·등싸잡기는 **반대 손이 이미 소매·깃을 잡고 있을 때만** 노출 (§2)
- 손 상황부: 뜯기 / 잡기 유지는 해당 상태에서만
- 코어 3택 / 하체 4택
- **플레이 모드에서 B의 선택지(빨강 노드)를 절대 노출하지 않는다.** 이번 작업의 목적 자체다.
- **내 성향 노출 막대를 HUD에 띄운다.** 읽히기 전에 보여야 한다 (`MATCH_RULES` §7).
  심판 누산기는 여전히 숫자로 띄우지 않는다.

커밋: `feat(game): A-only input UI + tendency exposure bar`

---

## 7. 회귀 + STATUS 갱신

- 113개 이상 전부 초록, 메인라인 m36 sum 7 / 한판 / winner=A
- `ScriptedMode_DoesNotInstantiateHabitBrain` 통과 — 대조 모드가 브레인을 안 물어야 한다
- `md/STATUS.md` 갱신 (영어)

커밋: `test: mode isolation regression + STATUS update`
