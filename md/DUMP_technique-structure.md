# DUMP_technique-structure.md — §H 2단계·3단계용 현재 구조 조사

**이 문서는 조사 결과만 담는다. 설계·결정 없음, 코드·다른 문서 무수정.** 목적은
`CORE_DESIGN_MatchEngine.md`에 기술 레지스트리·밸런스 자료구조를 설계하기 전에
"지금 실제로 뭐가 있고 뭐가 없는지"를 파일·줄 인용으로 고정해두는 것이다. 이 문서
자체는 스탬프 대상이 아니다(MATCH_RULES.md/CORE_DESIGN_MatchEngine.md만 스탬프
관리 대상 — CLAUDE.md 참조).

조사일: 2026-08-12. 조사 시점 기준 사실이며, 이후 코드가 바뀌면 이 문서는 오래된
스냅샷이 된다 — 다음에 참조할 때는 grep으로 재확인할 것.

---

## 0. 세션 시작 확인

- `git status --short`: `Assets/_Recovery/`(untracked, 이전 세션에서 이미 보고된
  것과 동일 — 이번 세션이 만든 게 아님) 외 무변경.
- `git config core.hooksPath` = `tools/githooks`.
- `sh tools/stamp.sh --check md/*.md` → exit 0.
- `tools/dotnet-harness`: `dotnet run -- --check` → **212/212 통과, 지문
  IDENTICAL**.
- Unity MCP: `Unity_GetConsoleLogs` 로 연결 확인 — 0 에러 / 1 경고(기존에
  이미 기록된 `Unity.AI.Toolkit.Accounts` 네트워크 폴링 경고, 프로젝트 코드
  무관). 이번 세션은 조사만 하므로 그 이상 건드리지 않았다.

---

## 1. 기술(Technique) 관련 현재 구조

### 1.1 `TechniqueSpec.cs` 전문

`Assets/_Project/Scripts/Core/TechniqueSpec.cs` (28줄, 전문):

```csharp
namespace CounterTime.Core
{
    /// Plain technique data. The ScriptableObject wrapper lives in CounterTime.Game.
    /// MasteryLevel rides on the spec because §5.4 makes mastery per-technique and
    /// StaminaSystem.PayAttack takes no separate mastery parameter.
    public sealed class TechniqueSpec
    {
        public readonly string Id;
        public readonly ActionCost Cost;
        public readonly TechniqueClass Class;
        public readonly int MasteryLevel;   // §5.4, 0..5
        /// §4.4 [초안] — exempts this technique from the setup-less (흔들기 없는) BigThrow
        /// downgrade to Sloppy. For 말아업어치기: its whole point is sharing 업어치기's
        /// opening motion, so the opponent cannot see it coming to set up 흔들기 against.
        public readonly bool SurpriseExempt;

        public TechniqueSpec(string id, ActionCost cost, TechniqueClass techClass,
                             int masteryLevel = 0, bool surpriseExempt = false)
        {
            Id = id;
            Cost = cost;
            Class = techClass;
            MasteryLevel = masteryLevel;
            SurpriseExempt = surpriseExempt;
        }
    }
}
```

**필드는 이 4개뿐이다**: `Id`(문자열, 표시명이자 식별자 겸용), `Cost`
(`ActionCost` — BigThrow/Poke/Counter/Tear, §5.1 비용표 행 선택용), `Class`
(`TechniqueClass` — Sloppy/Standard/Signature, §4 판정식 항), `MasteryLevel`
(int 0..5, §5.4), `SurpriseExempt`(bool, §4.4 흔들기 강등 면제). **§B가 요구하는
필드(표시명, 알고리즘 클래스, 5대 분류, 레지스트리)는 하나도 없다** — `Id`가
표시명과 식별자를 겸하고 있고, 알고리즘 클래스(견제성 발기술/큰 기술)는 `Cost`
(BigThrow vs Poke)로 우회 판별되고 있으며(§1.2 참조), 5대 분류(손기술/발기술/
허리기술/누우며 메치기/변칙)는 필드 자체가 없다.

주석 "The ScriptableObject wrapper lives in CounterTime.Game"은 §1.3에서 보듯
**아직 실재하지 않는다** — 작성 시점의 의도 표명이고 구현은 없다.

### 1.2 `TechniqueClass` enum과 실제 세팅 지점

`Assets/_Project/Scripts/Core/Enums.cs:13`:
```csharp
public enum TechniqueClass { Sloppy = 0, Standard = 1, Signature = 2 } // judgment term
```

**`new TechniqueSpec(...)` 생성 지점 전수** (프로덕션 코드만, 테스트 제외):

| 파일:줄 | Id | Class | 비고 |
|---|---|---|---|
| `Game/Match/LiveMatchRunner.cs:36` | `"밭다리"` | `Standard` (리터럴) | A의 유일한 실전 기술 — 하드코딩 placeholder, §1.4 |
| `Core/MatchEngine.cs:371-372` | `"tear"` | `Sloppy` (리터럴) | 뜯기(§3.2)를 스태미나 지불용으로만 감싼 내부 스펙, 판정에는 안 쓰임 |
| `Game/Match/ScriptedAssembler.cs:93` | `node.id + ":발기술"` | `Sloppy` (리터럴) | 42노드 견제기 턴(§1.4) |
| `Game/Match/ScriptedAssembler.cs:104` | `techId`(노드 제목에서 파싱) | `techClass`(노드 본문 `[판정식]` 줄에서 정규식으로 파싱) | 42노드 경로 — §1.4 |
| `Brain/HabitBrainCommitSource.cs:173` | `"견제 발기술"` | `Sloppy` (리터럴) | B의 폴백(견제만, OPPONENT_BRAIN §4 "폴백은 큰 기술을 절대 안 던진다") |
| `Brain/HabitCatalogue.cs:31` | `"업어치기"` | `Standard` (리터럴) | H1 |
| `Brain/HabitCatalogue.cs:46` | `"안뒤축감아치기"` | `Standard` (리터럴) | H2 |

**모든 지점에서 `TechniqueClass`는 호출부가 직접 박아 넣는 리터럴이다.**
`MasteryLevel`을 실제로 조회해서 `TechniqueClass`를 계산하는 코드는 프로덕션
어디에도 없다(§2.5 참조 — `StaminaSystemTests.cs`의 테스트 변수 하나가
유일하게 0이 아닌 `masteryLevel`을 넘긴다). §F가 말하는 "숙련도가
`TechniqueClass`를 결정한다"는 아직 규칙 문서에만 있고 코드에는 없다 —
**이 파생 로직 자체를 새로 만들어야 한다.**

### 1.3 Game 쪽 SO 래퍼 실태

기술 전용 ScriptableObject는 **존재하지 않는다**(`TechniqueSpecAsset`,
`TechniqueAsset` 등 이름으로 grep — 0건). `Assets/_Project/Scripts/Game`
안에서 `ScriptableObject`를 상속하는 클래스는 둘뿐이다:

- `Game/Brain/OpponentAsset.cs` — 상대(습관 셋 + 발화확률 + `scoutingLevel`)
  저작용. 기술과 무관. **§2.2에서 다루듯 이것도 실제로는 미사용(dead code)이다.**
- `Game/Scenario/NodeMotionMap.cs` — 노드→모션 클립 매핑. 기술과 무관.

`Assets/_Project/ScriptableObjects/` 디렉토리 실제 내용물:
```
Assets/_Project/ScriptableObjects/.gitkeep
Assets/_Project/ScriptableObjects/NodeMotionMap.asset
```
`Opponent.asset` 류의 실제 에셋 파일도 없다 — `OpponentAsset` 클래스는 있지만
그걸로 만든 `.asset` 인스턴스는 리포에 없다.

`CORE_DESIGN_MatchEngine.md:29`의 폴더 구조 주석도 같은 문구를 반복한다
(`TechniqueSpec.cs // technique data (plain class; SO wrapper lives in Game)`)
— 설계 의도 표명이지 구현 현황 서술이 아니다.

### 1.4 기술 인스턴스가 만들어지는 모든 지점

**A. 라이브 러너의 "밭다리" 하드코딩** — `Game/Match/LiveMatchRunner.cs`:

```
35: static readonly TechniqueSpec PlaceholderATechnique =
36:     new TechniqueSpec("밭다리", ActionCost.BigThrow, TechniqueClass.Standard);
```
정적 readonly 필드로 **한 번만** 생성되고 경기 내내 재사용된다. 실제로 쓰이는
지점은 `SubmitA`(같은 파일):
```
108: bool bAttacks = _bSource.LastTechnique != null;
109: bool aAttacks = !bAttacks && a.Legs == LegAction.Attack;
...
113: TechniqueSpec technique = bAttacks ? _bSource.LastTechnique
114:                         : aAttacks ? PlaceholderATechnique : null;
```
A가 `Legs == Attack`을 선언하고 B가 그 교환에 기술을 안 냈으면(동시공격
우선순위 §2 참조), A가 뭘 골랐든 상관없이 이 고정 스펙 하나가 쓰인다.
클래스 주석(18-20줄)이 이유를 명시: "A의 기술 선택 UI는 아직 없다."

**B. 42노드 스크립트 경로** — `Game/Match/ScriptedAssembler.cs`. 판정을 직접
하지 않고 시나리오 JSON의 `[판정식]` 텍스트 줄을 정규식으로 파싱해 그 자리에서
`TechniqueSpec`을 조립한다:
- `GripTurn()` (83-96줄): `[판정식]` 줄이 없는 노드가 하체 공격을 선언하면
  견제기 취급 — `new TechniqueSpec(node.id + ":발기술", ActionCost.Poke,
  TechniqueClass.Sloppy)` (93줄).
- `Exchange()` (98-107줄): `[판정식]` 줄이 있으면 `new TechniqueSpec(techId,
  cost, techClass)` (104줄) — `techId`는 노드 제목에서 마지막 `—` 뒤를 `·`로
  끊은 이름(111-118줄), `techClass`는 본문의 `기술+N`/`시그니처+N` 패턴을
  정규식(`TechTerm`, 32줄)으로 뽑아 그 숫자를 그대로 enum 캐스팅(138-146줄).
- `CostOf()` (124-125줄): 견제기 이름 목록이 **하드코딩 배열 하나**뿐이다:
  ```csharp
  static readonly string[] Pokes = { "발목받치기" };
  static ActionCost CostOf(string techName) =>
      Array.IndexOf(Pokes, techName) >= 0 ? ActionCost.Poke : ActionCost.BigThrow;
  ```
  §B 로스터가 견제성 발기술로 4개(모두걸기·발목받치기·나오는발차기·
  발뒤축걸기)를 정의하는데, 이 배열엔 1개만 있다 — 나머지 3개는 아직 존재
  자체가 없으므로(§1.5) 이 목록에 오를 일도 없었다.

이 경로는 기술 데이터를 **어디서도 조회하지 않는다** — 텍스트에서 즉석으로
재구성한다. 42노드 지문(fingerprint)이 이 파싱 결과에 의존하므로, 앞으로
레지스트리를 만들어도 **이 경로를 건드리면 지문이 깨진다** — 레지스트리는
이 경로와 별개로 얹히거나, 최소한 이 경로의 파싱 결과와 100% 같은 값을
내야 한다.

**C. 습관 브레인(B)** — `Brain/HabitCatalogue.cs`(§1.2 표 참조)와
`Brain/HabitBrainCommitSource.cs:173`(폴백 견제기). 전부 `case` 문 안의
리터럴 생성이다 — 조회 대상 자료구조가 없다.

**D. MatchEngine 내부의 임시 스펙** — `Core/MatchEngine.cs:371-372`, 뜯기
행동을 스태미나 지불 계산에 태우기 위해서만 즉석으로 만드는 스펙
(`"tear"`, `ActionCost.Tear`, `TechniqueClass.Sloppy`). 판정식에는 들어가지
않는다(§4 판정식은 `ResolveTechnique`에서만 돈다, `ResolveHandAction`이 아니라).

**LiveAssembler.cs(§H 1단계에서 이미 손 축을 2단 선택으로 바꾼 그 파일)에는
`TechniqueSpec`/`TechniqueClass` 참조가 0건이다** — A의 다리(기술) 선택 UI가
정말로 존재하지 않는다는 뜻이고, §A.2 "Legs 공격 = 기술 선택"을 구현하려면
이 파일에 새 선택지 노출부터 필요하다.

### 1.5 §B 로스터 대비 코드 실재 여부

MATCH_RULES §B 표의 12개 기술을 코드에서 문자열로 grep한 결과:

| 기술 | §B 분류 | 코드에 존재? |
|---|---|---|
| 모두걸기 | 견제성 발기술 (new) | 없음 |
| 발목받치기 | 견제성 발기술 | `ScriptedAssembler.Pokes`에 이름만 (§1.4B) |
| 나오는발차기 | 견제성 발기술 (new) | 없음 |
| 발뒤축걸기 | 견제성 발기술 (new) | 없음 |
| 밭다리걸기 | 큰 기술 | `"밭다리"`로 하드코딩 존재 (§1.4A) — 이름 표기가 "밭다리걸기"가 아니라 "밭다리" |
| 허리후리기 | 큰 기술 | 42노드 시나리오 제목에만 등장(모션은 `ai_osotogari_entry` 임시 대체, STATUS "알려진 공백"), `TechniqueSpec`으로 만들어진 적은 없음 |
| 안오금띄기 | 큰 기술 | 없음 |
| 어깨 매치기 (변칙 어깨) | 큰 기술 | 없음 |
| 업어치기 | 큰 기술 (B, H1) | `HabitCatalogue.cs:31` |
| 안뒤축감아치기 | 큰 기술 (B, H2) | `HabitCatalogue.cs:46` |
| 허리 감아치기 | 큰 기술 | 없음 |
| 말아업어치기 | 큰 기술 (B, 변칙) | 코드 없음 — `TechniqueSpec.SurpriseExempt` 필드만 이 기술을 염두에 두고 존재(§1.1), 실제 인스턴스는 아직 없음 |

12개 중 코드에 문자열로라도 존재하는 것은 4개(밭다리·업어치기·
안뒤축감아치기·발목받치기), 그중 판정 가능한 `TechniqueSpec`으로 실제
만들어지는 것은 3개(발목받치기는 42노드 경로에서 이름만 알려져 있고 실제
`TechniqueSpec` 생성은 노드 데이터가 있어야 일어난다). **단일 출처
레지스트리는 없고, 이름·클래스·비용이 파일 4곳(LiveMatchRunner/
ScriptedAssembler/HabitCatalogue/HabitBrainCommitSource)에 흩어져 각자
리터럴로 박혀 있다.**

---

## 2. 영구 저장 계층 현황

### 2.1 저장 원시 기능 자체가 프로젝트에 없다

`Assets/` 전체에서 다음 패턴을 grep — **전부 0건**:
`PlayerPrefs`, `File.Write*`, `File.ReadAll*`, `StreamWriter`,
`BinaryFormatter`, `JsonUtility.ToJson`, `Application.persistentDataPath`,
`SaveGame`, `SaveSystem`.

**세이브 파일 포맷·경로: 없음.** 런타임 상태를 디스크에 쓰는 코드가 프로젝트
어디에도 없다. "플레이어"라는 영속적 정체성 개념 자체(세이브 슬롯, 플레이어
ID 등)도 코드에 없다.

### 2.2 `OpponentAsset` / `OpponentDefinition` / `ScoutingLevel`

`Game/Brain/OpponentAsset.cs`(64줄, ScriptableObject, `[CreateAssetMenu]`)가
"상대 한 명"을 저작하는 자리로 설계돼 있다 — 습관 목록(`HabitEntry[]`,
`habit` + `fireProbability`)과 `scoutingLevel`(0..3, 32줄)을 인스펙터에서
편집해 `ToDefinition(cfg)`(43-57줄)로 불변 `OpponentDefinition`을 만든다.

**하지만 이 클래스는 프로젝트 어디에서도 참조되지 않는다** — `OpponentAsset`
이름으로 grep하면 자기 자신의 정의 파일(20줄) 외엔 0건. 대응하는 `.asset`
에셋 파일도 리포에 없다(§1.3). 실제 상대 정의는 `Brain/Jindoyun.cs:38-46`의
**하드코딩 팩토리**가 만든다:
```csharp
public static OpponentDefinition Create(MatchRulesConfig cfg)
{
    var habits = new HabitDefinition[Habits.Length];
    for (int i = 0; i < Habits.Length; i++) habits[i] = HabitCatalogue.Build(Habits[i], cfg);
    return new OpponentDefinition("진도윤", habits, scoutingLevel: 0);
}
```
`LiveMatchRunner.cs:87`이 `Jindoyun.Create(_cfg)`를 직접 부른다 —
`OpponentAsset`을 거치지 않는다. 즉 **`OpponentAsset`은 만들어졌지만 배선
안 된 죽은 코드다.** (설계도 아니고 버그도 아니고, 이 조사에서 처음
확인된 사실이라 여기 기록만 해둔다 — 판단은 §4.)

`OpponentDefinition`(`Brain/OpponentDefinition.cs`) 자체는 **불변**이고
"여러 경기가 공유해도 안전"(클래스 주석 6줄)하도록 설계돼 있다 — 이게 세이브
데이터가 아니라 **경기 시작마다 다시 만들어지는 정적 정의**라는 뜻이다.
`ScoutingLevel`(22줄, `public readonly int`)은 필드로 존재하지만 소비처가
`TendencyWindow`뿐이고(§2.4), 진도윤은 0으로 고정 출하된다(주석 43-44줄:
"필드는 구현하되 0으로 출하한다").

### 2.3 `HabitState` / `ObservationLedger` — 명시적으로 "한 경기" 수명

`Brain/HabitState.cs:6-7`(클래스 주석, 원문 그대로): "한 경기 동안 습관들에
쌓이는 것 — 피해 카운터와 폐기·점등. 수명은 `ObservationLedger`와 같다:
경기 하나." §4.2 폐기(2회 피해로 습관 소멸 + 예비 점등)는 **경기 내부
상태**이고, 저장되지 않으면 다음 경기에서 사라진다.

`Brain/ObservationLedger.cs`(§2 수 노트 관측 카운터)도 마찬가지 — 생성자가
빈 `Dictionary`뿐이고, 어디에서도 이전 경기의 카운트를 불러와 채우는 코드가
없다. `LiveMatchRunner.Awake()`(83줄)가 `new ObservationLedger()`로 매번
새로 만든다.

**결론: §C.1(A의 인지 목록, "상대별 영구")과 §C.2(B의 적응, "상대별
영구") 둘 다 요구하는 "영구"를 지금 지탱하는 자료구조가 없다.** 지금 있는
`ObservationLedger`/`HabitState`는 정확히 §C가 영구화해야 한다고 말하는
바로 그 데이터(관측 횟수, 폐기 여부)를 담고 있지만, 경기가 끝나면 통째로
버려진다.

### 2.4 `TendencyWindow`의 시드 생성자 — 배선 안 된 훅

`Brain/TendencyWindow.cs:38-43`:
```csharp
/// §5 ScoutingLevel 0 = 빈 창으로 시작. 시딩은 이 생성자로 들어온다.
public TendencyWindow(IEnumerable<TurnChoice> seed = null)
{
    if (seed == null) return;
    foreach (var c in seed) Record(c);
}
```
`ScoutingLevel`로 미리 창을 채우는 **자리(생성자 오버로드)는 있다.** 하지만
실제 호출부 `LiveMatchRunner.cs:84`는 `_window = new TendencyWindow();` —
인자 없이 부른다. `ScoutingLevel`이 몇이든 시드는 항상 비어 있다. 테스트
(`TendencyWindowTests.cs:96-100`)는 이 생성자 자체가 동작한다는 것만
검증하고, 실전 배선(OpponentDefinition.ScoutingLevel → 실제 seed 데이터가
뭔지)은 아무 데도 없다 — `OpponentDefinition.cs:18`의 주석이 이미 인정하고
있다: "그 배선은 3단계다."

### 2.5 `MasteryLevel` / `MasteryReadReductionPerLevel` — 절반만 연결됨

`TechniqueSpec.MasteryLevel`(§1.1)은 **소비처가 실재한다**:
`StaminaSystem.cs:22`, `cost *= 1f - _cfg.MasteryCostReductionPerLevel *
spec.MasteryLevel;` — 비용 절감 공식은 이미 동작한다. 하지만 이 값을 0이
아닌 걸로 채워 넣는 **프로덕션 호출부가 하나도 없다**(§1.2 표 전체가 리터럴
0 또는 생략 기본값 0). 유일하게 0이 아닌 값을 넘기는 곳은
`Core/Tests/StaminaSystemTests.cs:19`의 테스트 변수.

`MatchRulesConfig.MasteryReadReductionPerLevel`(`MatchRulesConfig.cs:26`)은
**소비처가 0개다** — 필드만 있고 어디서도 읽지 않는다. §F "읽히는 정도
−5%p/레벨"에 대응하는 계산식 자체가 아직 없다는 뜻.

### 2.6 §C·§F를 영구로 저장하려면 어디에 얹어야 하는가 — 후보만 (구현 아님)

아래는 **위치 후보 나열**이지 설계가 아니다. 실제로 뭘 선택할지는 다음
CORE_DESIGN 작업에서 사람이 정한다.

- **저장 원시 기능 자체가 없다(§2.1)** — 어느 후보를 고르든 그 전에
  직렬화·파일 I/O 계층부터 새로 만들어야 한다. Unity 표준 후보는
  `JsonUtility` + `Application.persistentDataPath` (프로젝트에 선례 없음,
  완전 신규) 또는 `PlayerPrefs`(작은 값에나 적합, 구조화 데이터엔 부적합).
- **상대별 식별 키**: 지금 유일한 상대 식별자는 `OpponentDefinition.Name`
  (문자열, `"진도윤"`) — 별도 GUID/ID 필드가 없다. §C가 "상대별 영구"라고
  할 때 그 키를 이름 문자열로 쓸지 별도 ID를 신설할지는 결정 안 됨.
- **§C.1 A의 인지 목록**: 지금 `ObservationLedger`(경기 스코프)가 구조적으로
  가장 가깝다 — 상대 ID로 색인된 `Dictionary<opponentKey,
  ObservationLedger>`류를 경기 밖에 두고 매치 시작 시 해당 상대 항목을
  주입/회수하는 형태가 자연스러운 확장 지점으로 보인다(§2.3의 "경기 하나"
  수명 설계를 뒤집어야 한다는 뜻).
- **§C.2 B의 적응(기술-빈도 채널)**: 아직 코드가 없다(§A.2의 "새 기술-빈도
  채널"은 §H 2단계 이후 항목, 이 조사 시점엔 미착수) — 새로 만들 때
  처음부터 영구 저장을 염두에 둔 자료구조로 설계할지, 아니면 §H 6단계
  구현 후 나중에 영구화를 얹을지가 선택지.
- **§F 숙련도**: `TechniqueSpec.MasteryLevel`은 "그 기술을 쓸 때의 현재
  레벨"을 실어 나르는 필드일 뿐, 레벨 자체를 보관하는 저장소가 아니다.
  레벨은 상대별이 아니라 **플레이어 전체(career, §F "Scope: permanent
  (career)")** 스코프라 §C와 키 체계가 다르다 — `Dictionary<techniqueId,
  level>` 형태가 자연스러워 보이지만, "플레이어"라는 개념 자체가 코드에
  없어서(§2.1) 이것도 신규다.
- **`OpponentAsset`을 살릴지**: 지금 죽어 있는 이 SO(§2.2)를 §C/§F 영구
  저장의 저작 도구로 되살릴지, 아예 별도 시스템을 새로 만들지도 미결.
  `OpponentAsset`은 "설계 시점 저작"(습관 on/off, 발화확률) 대상이지 "런타임
  진행 데이터"(관측 횟수, 숙련도) 대상으로 설계되지 않았다는 점은 사실로
  확인해뒀다 — 이 SO에 진행 데이터를 얹으면 에셋이 세이브 파일을 겸하게
  되는 것이라 통상적인 Unity 패턴과 어긋난다(플레이 모드 변경이 에셋에
  남는 문제, `OpponentAsset.cs:59-60`의 `NewMatchState` 주석이 정확히 이
  함정을 피하려고 지금 이렇게 설계됐다고 밝히고 있다).

---

## 3. 밸런스(§H 3단계) 자료구조가 앉을 자리 예비 조사

### 3.1 체력·그립이 지금 어느 시스템에 어떻게 들어있는가

**`StaminaSystem`** (`Core/StaminaSystem.cs`, 95줄) — 패턴:
- `readonly int[] _stamina = { 100, 100 };` (9줄) — `FighterId`로 인덱싱하는
  고정 크기 2배열.
- 생성자가 `MatchRulesConfig cfg`만 받는다(11줄) — 모든 수치는 `_cfg`에서
  읽는다(`BigThrowCost`, `MatteRecovery` 등, `MatchRulesConfig.cs:20-26`).
- 지불류 메서드(`PayAttack`/`PayCounter`/`PayDefense`/`DrainExtremeDefense`/
  `PayOsaekomiStay`/`PaySetup`)와 회복류 메서드(`RecoverMatte`/
  `RecoverStall`) — 전부 내부 `Spend`/`Gain` 헬퍼(79-89줄)를 통해
  `Math.Clamp`류로 0/100 경계를 지킨다.
- 조회는 `Get(FighterId)`(13줄) 하나.

**`GripSystem`** (`Core/GripSystem.cs`, 119줄) — 같은 패턴이지만 손 축이
있어 2차원 배열(`[fighter, hand]`)이다: `_holding`/`_target`/`_lockOn`
(16-18줄). `GripScore`/`EffectiveGrip`(35-47줄)이 매턴 계산되는 파생값,
`Grab`/`ResolveTear`(26-32줄, 79-94줄)가 변형 메서드, `OnMatte()`(97-102줄)가
그쳐 시 전체 리셋(`Array.Clear`).

**`MatchEngine`이 두 시스템을 조립하는 방식** (`Core/MatchEngine.cs`):
```
62: public GripSystem Grip { get; }
63: public StaminaSystem Stamina { get; }
...
73: Grip = new GripSystem();
74: Stamina = new StaminaSystem(cfg);
```
생성자(69-78줄)에서 `readonly` 자동 프로퍼티로 노출하고 그 자리에서
`new`한다 — Balance를 같은 자리에 얹는다면 이 두 줄과 똑같은 모양
(`public BalanceSystem Balance { get; }` + `Balance = new
BalanceSystem(cfg);`)이 구조적으로 대칭이다. **이건 관찰이지 결정이
아니다** — 실제로 별도 클래스로 뺄지, 두 값(A/B)을 어디에 둘지는 §H 3단계의
몫.

**그쳐(matte) 처리 대칭성**: `CallMatte()`(`MatchEngine.cs:504-510`)가
`Grip.OnMatte()` + `Stamina.RecoverMatte(A)` + `Stamina.RecoverMatte(B)`를
한 자리에서 호출한다. **주목할 점** — 바로 그 메서드의 주석(502줄):
> "자세 초기화는 여기서 안 한다 — Core는 자세를 들지 않는다. 커밋을 유지하는
> 쪽(러너)이 MatteCalled를 받아 푼다."

즉 **Core는 "Core stance selection"(공격적/중립/수비적) 상태 자체를 안
갖고 있다** — 그건 순전히 Game 쪽(`LiveMatchPanel._core` 필드,
`LiveMatchPanel.cs:57`)이 UI 입력값으로 들고 있다가 `MatteCalled` 이벤트를
보고 스스로 Neutral로 되돌린다(`RefreshStanding()`,
`LiveMatchPanel.cs:237-244`, 실측: 이 메서드는 매 교환 종료 후 호출되고
그쳐 여부와 무관하게 항상 리셋한다 — UI가 다음 턴 입력을 준비하는
동작이지 §4.3 그쳐 규칙의 직접 구현은 아니다).

**이 발견이 시사하는 것(사실 서술, 설계 아님)**: MATCH_RULES §7이 밸런스를
"grip과 같은 티어의 HUD, 정확한 숫자"로 요구하고 §S가 결정론적 드레인·부분
회복을 요구하는 것은 Stamina/Grip과 완전히 같은 성격이다 — 반면 "자세
(Core) 선택"은 Core가 아예 상태를 안 갖는 UI 전용 개념이다. 지금 코드
구조를 그대로 따르면 **Balance는 (자세 선택이 아니라) Stamina/Grip과
같은 방식으로 Core에 실재하는 시스템이어야 앞뒤가 맞는다** — MATCH_RULES
§S가 이미 그렇게 쓰여 있다는 것과도 일치한다.

### 3.2 §4.4 흔들기의 기존 구현 상태 — 선언·게이트·비용은 있으나 밸런스 드레인 효과는 없음

`Core/MatchEngine.cs`에 이미 있는 것:
- `readonly bool[] _setupActive = new bool[2];`(56줄) / `readonly bool[]
  _setupDeclaredThisExchange = new bool[2];`(60줄) — "이번 교환에
  선언했는가" / "다음 교환까지 지속되는가"를 나누는 2단계 상태, 정확히
  §4.4의 "1교환만 지속" 규칙을 구현해둔 자리.
- `IsSetupActive(FighterId)`(83줄) — HUD가 읽는 공개 게터.
- 선언 처리(`ResolveHandAction`, 351-360줄):
  ```csharp
  case HandTarget.Setup:
      if (!_cfg.SetupGateEnabled || !Grip.HasAnyGrip(who)) break;
      Stamina.PaySetup(who);
      _setupDeclaredThisExchange[(int)who] = true;
      ev.Add(new SetupDeclared(who));
      break;
  ```
- 강등 적용(`ResolveTechnique`, 419-426줄):
  ```csharp
  TechniqueClass techClass = script.Technique.Class;
  if (_cfg.SetupGateEnabled && script.Technique.Cost == ActionCost.BigThrow
      && !script.Technique.SurpriseExempt && !_setupActive[(int)attacker])
      techClass = TechniqueClass.Sloppy;
  ```
- 리셋(187-189줄): 그쳐/굳히기 진입/경기 종료 시 `_setupActive`를 지운다.

**이게 전부 구현하고 있는 것은 v4.0 이전 초안 규칙("흔들기 선언 자체가
다음 큰기술의 게이트를 연다")이다.** v4.0이 일반화한 "밸런스 수치가 임계
이상이면 강등"은 **아직 코드에 없다** — `!_setupActive[(int)attacker]`가
지금은 유일한 게이트 조건이고, 여기엔 밸런스 수치를 읽는 코드가 전혀 없다.
"흔들기를 맞으면 상대 밸런스가 깎인다"는 효과 자체도 코드 어디에도 없다
(§4.4 강등 검사 지점에서 `Grip.EffectiveGrip`/`Stamina.Get`은 읽지만
`Balance.Get`에 대응하는 호출은 존재하지 않는다 — 애초에 그런 시스템이
없으므로).

**§H 3단계가 정확히 건드려야 할 자리는 이 두 지점이다**: (1) `case
HandTarget.Setup:`(351-360줄) 안에 상대 밸런스 드레인 호출 추가, (2)
419-426줄의 `!_setupActive[(int)attacker]` 조건을 "상대 밸런스 ≥ 임계"
조건으로 교체(또는 병행). `_setupActive`/`_setupDeclaredThisExchange`
필드 자체를 계속 쓸지, 밸런스 수치 하나로 대체할지는 §H 3단계의 판단
대상 — 지금은 그 두 배열이 "게이트가 열려 있는가"라는 **이진값**만
표현하고, v4.0은 **연속값(0-100)**을 요구하므로 자료구조 형태 자체가
바뀌어야 할 가능성이 있다(사실 확인만, 설계 아님).

### 3.3 `CoreStance`와의 이름 충돌 실측

`Core/Enums.cs:8`: `public enum CoreStance { Aggressive, Neutral, Defensive
}`. 사용처는 40곳 이상(`TurnChoice.Core` 필드, `JudgementCalculator`,
`RefereeSystem`, `HabitCatalogue`, `LiveMatchPanel`, 테스트 다수 — grep
결과 전수는 이 문서 앞부분에서 이미 확인, 생략).

`MatchRulesConfig.cs`(§3.1에 전문 인용된 파일) 안에 `Stance`나 `Balance`
문자열을 포함한 필드는 **0개다** — §S가 요구하는 범위(0-100)·드레인 폭·
회복 폭·임계값(온전 임계 ⚙70 제안)에 대응하는 튜너블이 지금 하나도 없다.
`SetupGateEnabled`/`SetupCost`(48-49줄)만 있고, 이것도 §4.4 옛 초안용이지
§S 자원 자체의 파라미터는 아니다.

즉 **"Balance를 CoreStance와 충돌 없이 넣을 자리"는 사실 이미 비어 있다**
— `CoreStance`는 `TurnChoice`가 들고 있는 열거형이고, 새 `BalanceSystem`은
`GripSystem`/`StaminaSystem`과 나란히 `MatchEngine`에 새 프로퍼티로
추가하면 되므로 기존 어떤 타입과도 이름이 겹치지 않는다(타입 이름
`Balance`/`BalanceSystem`으로 grep — 프로젝트 전체 0건, 완전히 비어 있는
이름공간).

---

## 4. 판단이 필요한 지점 (목록만 — 정하지 않음)

1. **`OpponentAsset`을 살릴지 버릴지.** 죽은 코드(§2.2)로 방치할지, §C/§F
   영구 저장의 저작 도구로 재활용할지, 아예 다른 접근(코드 팩토리 계속
   유지 + 별도 세이브 시스템)을 쓸지.
2. **영구 저장 계층 자체를 언제 만들지.** §H 6·7단계(B 적응/숙련도)
   착수 전에 저장 원시 기능(직렬화·파일 I/O)부터 별도로 준비해야 하는데,
   이걸 6·7단계 안에 포함시킬지 그 전에 독립 작업으로 뺄지.
3. **상대 식별 키를 이름 문자열로 쓸지 별도 ID를 신설할지** (§2.6).
4. **§C.1 인지 목록의 저장 단위** — `ObservationLedger`를 상대별로 여러 개
   보관하는 구조로 확장할지, 아예 새 타입을 만들지.
5. **§F 숙련도 레벨의 저장 위치와 키 체계** — 플레이어 전체 스코프인데
   "플레이어"라는 개념이 코드에 없다(§2.6) — 이걸 신설하는 작업의 범위를
   §F 자체에 포함시킬지 더 이른 단계로 뺄지.
6. **기술 레지스트리의 형태** — 지금 4곳에 흩어진 리터럴(§1.2/§1.4)을
   하나의 정적 카탈로그(`HabitCatalogue`와 같은 `switch`형)로 모을지,
   ScriptableObject 기반(§1.1 주석이 예고한 SO 래퍼)으로 갈지, 아니면 둘을
   병행(코드는 규칙, SO는 저작 파라미터만 — `HabitCatalogue`/`OpponentAsset`
   조합과 같은 패턴)할지.
7. **레지스트리가 42노드 스크립트 경로(§1.4B)와 어떤 관계를 가질지** —
   완전히 별개로 두어 지문을 절대 안 건드릴지, 아니면 `ScriptedAssembler`의
   파싱 결과를 레지스트리 조회로 조용히 교체(값이 100% 같다는 걸 지문으로
   증명)할지.
8. **§4.4 강등 게이트의 자료구조 전환** — 지금의 이진 `_setupActive`
   배열을 그대로 두고 밸런스 임계 검사를 추가만 할지, 밸런스 수치
   하나로 통합해 이진 배열을 걷어낼지(§3.2).
