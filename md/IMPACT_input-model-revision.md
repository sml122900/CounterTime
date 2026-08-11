# IMPACT_input-model-revision.md — 입력 모델 개편 영향 조사 (확정 아님)

이 문서는 **조사 결과만** 담는다. 아래 개정안은 검토 중이며 이 문서가 채택을 뜻하지
않는다. 코드·다른 문서는 이 조사 중 변경하지 않았다.

## 조사 대상 개정안 (원문 그대로)

1. 손 축을 왼손/오른손으로 분리하고, 한 턴에 한 손만 지시한다.
2. 기술 탭을 신설해 플레이어가 기술을 직접 고른다(현재는 코어=공격적일 때 시스템이
   자동 선택). 기술을 고른 턴에는 코어·발·손 선택이 없다.
3. 그립·포지션이 불리한 상태로 기술에 들어가면 위장 공격 판정 확률이 높아진다.

## 먼저 — 개정안 (2) 전제 확인 (사실 정정)

개정안 (2)는 "현재는 코어=공격적일 때 시스템이 자동 선택"이라고 전제하지만, 실측
결과 **현재 코드는 `CoreStance.Aggressive`를 전혀 보지 않는다.**

- 실전 러너(`LiveMatchRunner.SubmitA`, `Assets/_Project/Scripts/Game/Match/LiveMatchRunner.cs:109`):
  `bool aAttacks = !bAttacks && a.Legs == LegAction.Attack;` — 트리거는 **Legs(하체) 축의
  `Attack`**이지 Core가 아니다. 기술 자체도 플레이어가 고르지 않고 고정값
  `PlaceholderATechnique`("밭다리", `LiveMatchRunner.cs:35-36`)가 들어간다.
- B의 습관 브레인(`HabitCatalogue.cs`)도 마찬가지다 — H1(`HabitCatalogue.cs:26-33`)의
  액션은 `TurnChoice.Neutral`(Core=Neutral)이고, H2(`HabitCatalogue.cs:41-47`)는
  `CoreStance.Neutral, LegAction.Attack`이다. 즉 습관이 기술을 낼 때도 Core는 관여하지
  않는다.
- `CoreStance.Aggressive`를 참조하는 실제 코드는 `LiveAssembler.cs:17`(옵션 목록에
  넣는 것뿐)과 `LiveMatchPanel.cs:354`(라벨 문자열 "공격적")뿐이다. 판정·공격자 결정
  로직 어디에도 `Core == Aggressive`를 조건으로 쓰는 곳이 없다(grep 확인,
  `CoreStance.Aggressive` 전체 매치 22곳 중 판정 로직은 0곳).

**결론: "코어=공격적 → 자동 선택"이라는 현재 동작은 존재하지 않는다.** 지금 A가
공격자가 되는 유일한 조건은 "Legs=공격을 선언했고 그 교환에 B가 습관을 안 냈을 때"이며,
그때 나가는 기술은 플레이어의 선택과 무관하게 고정된 "밭다리"다. 개정안 (2)를
"기존 로직 대체"로 설계하려면 이 사실을 먼저 반영해야 한다 — 대체 대상은
`a.Legs == LegAction.Attack` 트리거 + `PlaceholderATechnique` 고정값이지, Core 조건이
아니다.

---

## a. TurnChoice 구조 변경이 닿는 범위

### a-1. TurnChoice 자체 (`Core/TurnChoice.cs`)

현재 구조(`TurnChoice.cs:5-21`):
```csharp
public readonly HandTarget Hand;   // 상대 지정 + 손 행동(뜯기/유지/틀어잡기 등) 겸용
public readonly CoreStance Core;
public readonly LegAction Legs;
public readonly Hand OwnHand;      // 내 왼손/오른손 — 이미 존재한다
```

**중요한 선행 사실**: `OwnHand : Hand`(Left/Right) 필드가 **이미 있다**
(`TurnChoice.cs:12`, `Enums.cs:4`). 즉 "내 손이 왼손인지 오른손인지"는 Core 데이터
모델에 이미 표현돼 있고, 필드 자체를 새로 만들 필요는 구조적으로 없어 보인다. 다만
실전 UI에서는 이 필드가 한 번도 노출되지 않는다 — `LiveMatchPanel.cs:269,285`의
`_runner.A.Build(_hand, _core, _legs)` 호출은 `ownHand` 인자를 안 넘겨
`LiveAssembler.Build`의 기본값 `Hand.Right`(`LiveAssembler.cs:74`)로 항상 고정된다.
그래서 지금은 "손 축이 이미 있는데 UI가 항상 오른손으로 못 박아 놓은" 상태다 — 개정안
(1)이 필드 신설이 아니라 **UI 노출 + 게이트 정합성 문제**일 가능성이 있다(아래 a-3).

`HasHandAction`(`TurnChoice.cs:38-39`), `IsExtremeDefense`(`:42-43`),
`IsAllNeutral`(`:51-52`)는 전부 단일 `Hand` 필드 하나로 "손으로 뭘 했는가"를 판단한다.
`Hand`를 왼손 슬롯/오른손 슬롯 둘로 진짜 쪼갠다면(필드를 늘리는 방향) 이 세 프로퍼티는
"두 슬롯 중 하나라도 행동이면 true" 식으로 다시 정의해야 한다. 특히 `Evade`(회피)는
주석(`TurnChoice.cs:33-34`)에 "손으로 뭘 하는 게 아니라 자세다"라고 이미 적혀 있는데,
지금은 그 자세가 `Hand` 슬롯 값 하나로 표현된다 — 손을 두 슬롯으로 쪼개면 "회피는 어느
슬롯에 넣을 것인가"가 새로 생기는 질문이다(둘 다에 넣을지, 별도 축으로 뺄지는 §c의
"코어·발 없는 턴" 문제와 성격이 같다 — 미결로 남겨야 할 사안).

### a-2. MatchEngine

- `ResolveGrip(FighterId who, in TurnChoice mine, in TurnChoice theirs, ...)`
  (`MatchEngine.cs:331-375`)는 `switch (mine.Hand)` 하나로 그 턴의 손 행동을 처리한다.
  손이 왼손/오른손 두 슬롯이 되면 이 메서드는 **슬롯당 한 번씩, 최대 두 번** 불려야
  한다 — "한 턴에 한 손만 지시"가 지켜지면 실질적으로 한 슬롯은 항상 no-op이 되므로
  호출 자체는 그대로 두고 "어느 슬롯이 채워졌는가"만 판단하는 것으로 충분할 수 있다
  (구조 변경 규모는 개정안이 "동시 두 손 지시"까지 여는지 아닌지에 달려 있다 — 지금
  "한 손만"이라는 문구는 후자를 배제하는 것으로 읽힌다).
- `ResolveExchange`의 카운터 준비 게이트(`MatchEngine.cs:104-108`), 포크/진짜 시도
  판정(`:112-117`)은 `a.Legs`만 보고 `a.Hand`는 안 본다 — **영향 없음.**
- `_setupActive`/`_setupDeclaredThisExchange`(흔들기, §4.4)는 `mine.Hand == HandTarget.Setup`
  분기(`MatchEngine.cs:351-360`)로 처리된다 — 손 슬롯이 둘이 되면 "어느 손으로 흔들기를
  거는가"가 무의미한 질문(흔들기는 손 자체가 아니라 그립을 흔드는 행위)이라 이 분기가
  어느 슬롯에 붙을지 애매해진다. §4.4 자체가 아직 [초안]이라 지금 손대도 대조 모드에는
  영향 없다(플래그 기본 off).

### a-3. JudgementCalculator — `IsDefensivePosture`

`JudgementCalculator.cs:77-78`:
```csharp
public static bool IsDefensivePosture(in TurnChoice c) =>
    c.Core == CoreStance.Defensive && c.Legs != LegAction.CounterReady;
```
**Hand를 전혀 읽지 않는다.** MATCH_RULES §4(`md/MATCH_RULES.md:136-137`)도 "Legs와
Hand는 이 판정에 안 들어간다"고 명시한다. 손 축을 왼손/오른손으로 쪼개도 **이 함수는
무수정**이다 — a항에서 유일하게 "안전"이 코드로 이미 보장된 지점.

### a-4. RefereeSystem.ObserveTurn — §6.1 사다리

`RefereeSystem.Category`(`RefereeSystem.cs:74-85`)가 `c.Hand`를 읽는 지점은 둘:
- `c.IsExtremeDefense`(내부적으로 `Hand==Evade` 포함, line 78)
- `c.Hand == HandTarget.Evade`(line 81, +2 회피 rung)

이 둘은 "회피가 어느 손 슬롯에 사는가"에 직접 걸린다(a-1 참조). 나머지 rung(실제 시도
−4, 진입 −2, 극단적방어 +3, 코어+발 수비 +2, 카운터준비 +2, 전원중립 +1)은 손을 안 본다.
`IsRetreat`(`RefereeSystem.cs:88-91`, §6.3 위장 공격 후보 판정)도 `c.Hand == Evade`를
쓴다 — 같은 문제.

**결론**: §6.1/§6.3에서 손 축 분리가 깨는 지점은 정확히 "회피를 어떻게 표현하는가"
하나로 좁혀진다. 나머지 rung은 Core/Legs만 보므로 무수정.

### a-5. GripSystem — 이미 손 단위로 짜여 있다

`GripSystem.Grab(FighterId who, Hand ownHand, Hand opponentHand, GripKind kind)`
(`GripSystem.cs:25-31`)은 **이미** `Hand ownHand` 매개변수를 받고, 내부 배열
`_holding[2, Hands]`/`_lockOn[2, Hands]`(`GripSystem.cs:16-18`)도 이미 손별로 분리돼
있다. `EffectiveGrip`/`AnyHandLocked`/`LockState`/`HolderHandGripping` 전부 손 단위
연산이다. **GripSystem은 손 축 분리로 인한 구조 변경이 필요 없다** — 지금도 "어느
손인지" 개념을 이미 완전히 갖고 있고, 이걸 못 쓰는 건 위에서 확인했듯 `TurnChoice.Hand`
가 단일 슬롯이라 `ResolveGrip`이 한 턴에 한 번만 이 API를 호출하기 때문이다.

다만 `CanUseConversion(FighterId who) => Value(who) > 0`(`GripSystem.cs:68`)은 **"내
그립이 하나라도 있으면"**만 검사한다 — MATCH_RULES §2가 말하는 "**다른** 손이 이미
잡고 있을 때만"(`md/MATCH_RULES.md:43`)의 "다른"을 강제하지 않는다. 즉 같은 손이
잡았다가 그 손으로 바로 틀어잡기 전환을 해도 코드는 막지 않는다. 이건 아래 b항의
숫자에 직접 영향을 준다.

### a-6. StaminaSystem — 영향 없음

`StaminaSystem`의 공개 API(`PayAttack`/`PayDefense`/`PayCounter`/`DrainExtremeDefense`
등, `StaminaSystem.cs:17-58`) 중 `TurnChoice`를 매개변수로 받는 것은 **하나도 없다** —
`TechniqueSpec`, `DefenseQuality`, `effectiveGrip`(int)만 받는다. grep으로도 확인:
`new TurnChoice(` 21개 파일 목록에 `StaminaSystem.cs`는 없다. **손 축 분리·기술 탭 둘
다 StaminaSystem 시그니처에 영향 없음.**

---

## b. 한 턴 한 손 제한이 그립 확보 속도에 주는 영향

### 현재 규칙에서 우세 그립까지 최소 턴 수

"우세"의 두 가지 뜻으로 계산했다 — 문서가 숫자를 안 주므로 둘 다 계산:

**(i) 습관 H1이 쓰는 기준 "유효 잡기 ≥ +1"** (`OPPONENT_BRAIN.md:227`, `HabitCatalogue.cs:28`):
- 1턴: 손 하나로 상대 소매/깃 아무거나 잡는다 → `GripScore = 1 − 0 = +1`
  (`GripSystem.GripScore`, `GripSystem.cs:34-39`). **최소 1턴, 5초**(§2 잡기 턴 5초,
  `MATCH_RULES.md:70`).

**(ii) 최댓값 +2 (완전 우세)**:
- **경로 A — 두 손으로 각각 일반 그립**: 1턴째 손1로 상대 소매(Normal=1), 2턴째
  손2로 상대 다른 부위(Normal=1) → `Value(who) = 1+1 = 2` → `GripScore = 2−0 = +2`.
  **2턴, 10초.**
- **경로 B — 한 손 그립 후 변환**: 1턴째 손1로 일반 그립(Normal=1), 2턴째 같은 손
  또는 다른 손으로 틀어잡기 전환(Dominant=2, `ResolveGrip` 케이스
  `TwistGrip`/`BackGrab`, `MatchEngine.cs:343-349`) → `Value(who)`는 그 손 값이
  1→2로 **덮어써진다**(같은 손이면) 또는 1+2=3(다른 손이면), 어느 쪽이든 캡 2에서
  `GripScore = +2`. **2턴, 10초.**

**두 경로 다 2턴/10초로 동일하다** — 일반 그립 두 개(1+1)와 변환 하나(2) 값이
같기 때문. 이게 지금 룰의 실제 최솟값이다.

### 개정 후 최소 턴 수

개정안 (1)이 "손 축을 왼손/오른손으로 분리"만 하고 "한 턴에 한 손만"이라는 제약
자체는 **현재 이미 지켜지고 있는 규칙**이다(`TurnChoice.cs:3-4` 주석: "committing
both hands in a turn is deliberately not offered", `MATCH_RULES.md:42`도 동일).
즉 위 계산의 전제("1턴 = 손 하나")가 이미 지금 규칙이므로:

- **문자 그대로의 개정안이라면 최소 턴 수는 바뀌지 않는다** — (i)는 1턴, (ii)는
  2턴으로 개정 전후 동일하다.

바뀌는 것은 두 가지 **부수 효과**뿐이다:

1. **a-5에서 발견한 구멍이 막히면(같은 손 자기변환 금지) 경로 B가 사라진다** —
   그래도 경로 A(2턴)가 남아 최솟값 자체는 안 바뀐다. 다만 "1턴째 잡은 손으로 바로
   그 손을 틀어잡기하는" 지금 가능한 변칙 루트가 막혀, **실제로 도달 가능한 경로
   수는 3→2로 준다**(현재: 경로A, 경로B-같은손, 경로B-다른손 / 개정후: 경로A,
   경로B-다른손만).
2. **UI가 `OwnHand`를 실제로 노출하면**(현재는 항상 `Hand.Right` 고정,
   `LiveMatchPanel.cs:269,285`) 플레이어가 "이번엔 왼손으로" 같은 선택을 할 수
   있게 된다 — 이건 그립 확보 *속도*가 아니라 *어느 손이 어느 그립을 갖는지*의
   문제라 턴 수 계산에는 안 들어간다.

**결론: 개정안 (1)을 문자 그대로("한 턴에 한 손만") 구현해도 우세 그립 도달 최소
턴 수는 변하지 않는다(1턴/5초 = 그립 우위, 2턴/10초 = 완전 우세). 속도가 아니라
"자기 손으로 자기 그립을 변환하는 구멍을 막을지"가 실제 쟁점이다.**

---

## c. 기술 턴에 코어·발 값이 없을 때 — 판정식·심판 사다리 처리

기술 탭이 신설되면 그 턴의 `TurnChoice`에 Core/Legs/Hand를 뭘 넣을지 정해야 한다.
아래는 선택지와 각각의 결과 — **확정 아님.**

### 선행 발견: §6.1 사다리는 이미 "안전"하다

`RefereeSystem.Category`(`RefereeSystem.cs:74-85`)는 **`realAttempt`를 첫 줄에서
검사**한다:
```csharp
int Category(in TurnChoice c, bool attackEntry, bool realAttempt)
{
    if (realAttempt) return _cfg.PassRealAttempt;   // −4, 다른 조건 다 무시
    ...
}
```
`realAttempt`는 `TurnChoice`가 아니라 `MatchEngine.ResolveExchange`가 별도로 계산해서
넘기는 bool이다(`attacker.HasValue && !poke`, `MatchEngine.cs:116-117`). 즉 **기술
턴의 Core/Legs 값이 뭐든 §6.1 분류 결과는 똑같이 −4다** — 이미 실제 시도인지 여부가
Core/Legs보다 우선한다. 코드 변경 없이도 이 항목은 막혀 있다.

### 선택지 A — 중립 placeholder(`TurnChoice.Neutral` 패턴)를 쓴다

`HabitCatalogue.cs:26-33`(H1)가 이미 이 패턴이다: 기술은 있는데 Core/Legs는
`TurnChoice.Neutral`(Hand=None, Core=Neutral, Legs=Neutral)로 둔다.

- **판정식**: `OpponentDefensivePosture`는 **공격자가 아니라 방어자**의 TurnChoice로
  계산된다(`MatchEngine.cs:396-397,418`: `defenderChoice = attacker==A ? b : a`,
  `defensive = IsDefensivePosture(defenderChoice)`). 공격자 자신의 Core가
  Neutral이든 뭐든 이 항에 영향 없음 — **무해.**
- **§6.1**: 위에서 확인했듯 `realAttempt`가 먼저 걸려 무해.
- **§3 내 성향(TendencyWindow)**: **여기서 문제가 생긴다.** `TendencyWindow.Key`는
  `(Core, Legs)` 쌍만 본다(`TendencyWindow.cs:76`, "손은 안 본다" 주석 포함). A의
  커밋은 매 교환 무조건 `_window.Record(a)`로 기록된다(`LiveMatchRunner.cs:137`,
  조건 없음). 기술 탭을 쓸 때마다 Core/Legs가 항상 같은 placeholder(예:
  Neutral/Neutral)로 찍히면, **플레이어가 서로 다른 기술을 냈어도 창에는 매번 같은
  쌍이 기록된다** — 4-of-6 경고선(`MajorityCount=4`, `TendencyWindow.cs:30`)이나
  3연속 위험선(`ConsecutiveCount=3`, `:32`)이 "기술을 자주 쓴다"는 사실만으로
  가짜로 켜질 수 있다. §3의 원 설계 의도("같은 (Core,Legs) 쌍을 반복하면 읽힌다")와
  기술 탭 후("기술을 반복하면 항상 켜진다")가 뜻이 달라진다 — **개정안 (2)가 사실상
  §3의 키를 `(Core, Legs)`에서 `(Core, Legs, 기술선택여부)`나 `(기술ID)` 쪽으로
  바꿔야 하는 문제를 만든다. 이건 OPPONENT_BRAIN §3의 키 정의 자체를 건드리는
  변경이라 임의로 못 정한다.**

### 선택지 B — Core/Legs를 아예 없는 값(옵션 타입)으로 바꾼다

`TurnChoice`의 `Core`/`Legs`를 `CoreStance?`/`LegAction?`로 바꾸는 방향. §6.1의
`c.Core == CoreStance.Defensive && c.Legs == LegAction.Defend`(`RefereeSystem.cs:79`)
같은 비교 전부가 null 처리를 새로 타야 하고, `IsDefensivePosture`(§4)도 "값이 없으면
수비 자세인가?"라는 새 질문에 답해야 한다. **선행 발견(§6.1은 realAttempt로 이미
막혀 있음)을 보면 이 방향은 과잉 대응일 가능성이 높다** — 그래도 구조상 가능한
선택지라 배제하지 않고 기록.

### 선택지 C — Core/Legs에 "직전 값을 유지"시킨다(캐리오버)

기술 턴에도 직전 교환의 Core/Legs를 그대로 들고 간다. §3 TendencyWindow 문제는
사라지지만(패턴이 진짜 반영됨), §6.1 `IsAllNeutral`/`PassDoubleDefensive` 같은
rung이 "기술을 냈는데도 방어적 자세로 집계"되는 역설이 생길 수 있다 — 다만 이것도
`realAttempt`가 먼저 걸리므로 §6.1엔 영향 없다. **§3에는 영향 없고 §6.1에도
영향 없어 셋 중 가장 안전해 보이지만, "캐리오버"라는 새 개념 자체가 문서 어디에도
없다.**

**세 선택지 다 §4/§6.1 판정 로직 자체는 안 건드린다(이미 realAttempt·defenderChoice
설계 덕분). 실질적으로 갈리는 지점은 §3 TendencyWindow 하나뿐**이고, 이건 사람이
정할 사안이다.

---

## d. 위장 공격 판정에 "그립·포지션 불리" 얹기 — 필요한 신규 수치

### 현재 구조

`RefereeSystem.JudgeFeint(FighterId who, IRandomSource rng)`(`RefereeSystem.cs:106-125`):
```csharp
public FeintVerdict JudgeFeint(FighterId who, IRandomSource rng)
{
    if (!_feintCandidate[i]) return FeintVerdict.NotACandidate;
    ...
    float rate = Profile switch { Strict => DetectStrict, Lenient => DetectLenient, _ => DetectStandard };
    if (rng.NextFloat() >= rate) return FeintVerdict.Undetected;
    ...
}
```
**`TurnChoice`도 그립도 인자로 안 받는다.** `_feintCandidate[i]`는 `ObserveTurn`이
`realAttempt` 직후 2턴 이내 후퇴 패턴을 보고 세팅한다(`RefereeSystem.cs:60-71`,
`IsRetreat`, `:88-91`). 탐지율은 심판 성향(`Profile`) 하나로만 정해진다
(`MatchRulesConfig.DetectStrict/Standard/Lenient = .90/.70/.45`, `MatchRulesConfig.cs:34`).

### 시점 확인 — "기술에 들어가는 순간"의 그립/포지션은 잡을 수 있다

`MatchEngine.ResolveExchange`의 호출 순서(`MatchEngine.cs:119-129`):
```
1. Referee.ObserveTurn(...)   ← realAttempt 여기서 기록(위장 후보 판정의 기준 턴)
2. ResolveGrip(...)           ← 이번 교환의 그립 변화는 아직 반영 전
3. ResolveTechnique(...)
```
즉 `ObserveTurn` 시점에 `Grip.EffectiveGrip(who)`를 읽으면 **"이번 기술을 들고
들어오기 직전"의 그립**(직전 교환들이 만든 값, 이번 교환 변화는 미반영)을 정확히
얻을 수 있다 — 개정안 (3)이 원하는 "기술에 들어가는 순간의 그립"과 시점이 맞는다.
"포지션"은 코드에 대응 개념이 없다 — 후보는 `IsDefensivePosture`류(자기 Core가
아니라 상대 Core를 보는 함수라 이 용도엔 안 맞음) 또는 `GripSystem.AnyHandLocked`
(§3.2 Locked, 이미 자기 손 기준으로 존재)뿐이다.

### 필요한 신규 값 (값은 비움, 목록만)

1. **"불리"의 정의** — 그립: `EffectiveGrip(attacker) < 0`인지, `≤ 0`인지, 아니면
   `AnyHandLocked(attacker)` 여부인지. 포지션: MATCH_RULES에 "포지션"이라는 용어
   자체가 없다 — 새로 정의해야 한다(자기 Core==Defensive 상태로 기술을 낸다는 게
   말이 되는지부터 확인 필요, §4 정의상 Defensive 자세인 채로 기술을 내는 상황 자체가
   드물다).
2. **탐지율 가산 방식** — 덧셈(`rate += bonus`)인지 곱셈(`rate *= multiplier`)인지.
   현재 세 성향(.90/.70/.45)이 이미 상한에 가까운 값(엄격 .90)이 있어 단순 덧셈은
   1.0 캡 처리가 필요.
3. **가산값 자체** — 그립 불리분/포지션 불리분을 따로 줄지, 하나로 합칠지, 각각의
   크기.
4. **어느 시점 값을 스냅샷할지** — `ObserveTurn` 시점(위 확인대로 가능) 확정 시
   `_feintCandidate`처럼 `_feintGripAtAttempt[2]`류 신규 상태 배열이 필요 —
   지금 `_feintCandidate`도 실제 판정(`JudgeFeint`)까지 최대 `FeintWindowTurns`(=2,
   `MatchRulesConfig.cs:35`)턴 들고 있어야 하므로, 그립 스냅샷도 같은 수명으로
   들고 있어야 한다(교환마다 그립이 바뀌므로 "판정 시점 재조회"는 다른 답을 준다).

### RefereeSystemTests 영향

현재 위장 공격 테스트 6개(`RefereeSystemTests.cs:209-275`,
`Feint_DetectedWithinWindow_GivesShido` 외 5개)는 전부 그립/포지션을 안 주고
`rng`값만으로 임계 통과/미통과를 검증한다 — 그립 항을 추가해도 "불리하지 않음"이
기본값이 되도록 설계하면 기존 6개는 무수정으로 계속 통과할 수 있다(가산값 0 =
현재 동작과 동일). 다만 이건 설계상 그렇게 **되어야** 통과한다는 것이지 지금 코드가
보장하는 사실은 아니다.

---

## e. 회귀 영향 규모

### 209개 중 TurnChoice 구조에 의존하는 테스트 수

repo 전체 `[Test]` 총 212개 (grep 실측, Core 126 + Brain-Tests 61 + Game-Tests 25 =
212 — STATUS의 "209 하니스 그린 + 3 Unity 전용(MatchRunnerHandActionTests)"과 정확히
합이 맞음).

`new TurnChoice(` 를 **직접 생성**하는 테스트 파일과 테스트 수(grep 실측):

| 파일 | 테스트 수 |
|---|---|
| JudgementCalculatorTests.cs | 21 |
| RefereeSystemTests.cs | 21 |
| OralMatchReplayTests.cs | 16 |
| OsaekomiTests.cs | 11 |
| LiveReadResolverTests.cs | 10 |
| SetupTests.cs | 8 |
| TendencyWindowTests.cs | 8 |
| GoldenScoreTests.cs | 7 |
| MatteAfterScoreTests.cs | 7 |
| SetupPrecedingCommitSourceTests.cs | 7 |
| LiveAssemblerTests.cs | 6 |
| PlayModeSmokeTests.cs | 5 |
| MatchRunnerHandActionTests.cs (Unity 전용) | 3 |
| ScriptedReadResolverTests.cs | 2 |
| **합계** | **132** |

이 외에 **간접 의존**(TurnChoice를 직접 안 짓지만 TurnChoice를 만드는 프로덕션
코드를 호출하는 테스트):
- `HabitBrainTests.cs`(18개) — `HabitCatalogue`/`HabitBrainCommitSource`를 통해
  간접적으로 TurnChoice를 소비. `TurnChoice` 필드 자체(Hand/Core/Legs/OwnHand)가
  바뀌면 `HabitCatalogue.cs`의 생성자 호출 8곳이 먼저 깨지고 그 뒤로 이 18개가
  영향권.
- `ScenarioTurnChoicesTests.cs`(2개) — `ScenarioTurnChoices.TryParse`를 통해 간접.

**132(직접) + 18 + 2(간접) = 152 / 212 (72%)** 가 TurnChoice 구조(필드 시그니처)
변경에 직접 노출된다. 나머지 60개(GripSystemTests 14, MatchClockTests 8,
ScoreBoardTests 13, StaminaSystemTests 17, ResultNarrationTableTests 8)는
`new TurnChoice(` 를 쓰지 않는 파일 목록에 전부 없음 — **무관.**

이건 "TurnChoice 생성자 시그니처(필드 목록·순서)가 바뀌면 컴파일이 깨지는 테스트
수"다. 개정안 (1)이 기존 필드를 없애지 않고 순수 추가([다]항 선택지 B처럼 옵션
타입으로 안 바꾸는 이상)라면 대부분은 **컴파일은 유지**되고 값(기댓값) 검증만
깨질 수 있다 — 컴파일 파손과 값 파손은 다른 문제이므로 구분해서 봐야 한다.

### 42노드 지문과 counter-time-match-scenario-v2.json이 새 입력 모델과 어긋나는 지점

`ScenarioTurnChoices.TryParse`(`ScenarioTurnChoices.cs:15-48`)는 **JSON에 축 필드가
없다** — 노드의 `title`/`content` **한국어 문자열**을 패턴 매칭해서 손/코어/발을
역산한다(`HandOf`/`CoreOf`/`LegsOf`, `:53-83`). 실측(`counter-time-match-scenario-v2.json:34`,
`m3` 노드): `"title": "A: 왼손→B 오른소매 / 코어 공격 / 하체 발기술"` — **이미 왼손/
오른손을 문자열로 명시하고 있다**(`OwnHandOf`가 "왼손"/"오른손" 문자열을 읽음,
`ScenarioTurnChoices.cs:66-72`). 즉:

- **개정안 (1)(손 왼/오른 분리)은 JSON·파서 어느 쪽도 새로 안 건드려도 될 가능성이
  높다** — 왼손/오른손 개념이 이미 원문에 있고 `OwnHand` 필드로 이미 파싱된다.
  파서가 "한 턴 한 손"을 위반하는 문장을 만들 일도 없다(원 시나리오가 손 하나만
  서술).
- **개정안 (2)(기술 탭, 코어·발 없는 턴)는 다르다.** 42노드는 손/코어/발 세 축이
  전부 서술된 문장을 가정하고 파싱한다 — "이 노드는 기술만 선언하고 코어·발이
  없다"는 개념이 JSON에도 파서에도 없다. `MatchRunner.cs`(대조 모드 러너, 42노드
  전용)가 `ExchangeScript.Attacker`를 어떻게 정하는지는 이번 조사에서 코드를
  안 열어봤다 — **확인 필요 항목으로 남긴다.** (참고: `md/MATCH_RULES.md:10-11`은
  이 JSON의 `[판정식]` 줄이 §4 공식의 테스트 벡터로 쓰인다고 명시 — 즉 42노드는
  회귀의 정답지 역할이라 입력 모델이 바뀌어도 **그 문서 자체는 안 바뀐다**는 전제가
  있어야 지문이 의미를 유지한다.

### OralMatchReplayTests

이 파일(`Core/Tests/OralMatchReplayTests.cs`, 16개 테스트)은 JSON을 안 읽는다 —
42노드 시나리오의 메인라인을 **손으로 다시 TurnChoice로 옮겨 적은** 것이다
(`SleeveControl()`/`TwistGrip()` 같은 헬퍼, `OralMatchReplayTests.cs:34-49`,
전부 `new TurnChoice(...)` 직접 생성). 따라서:
- TurnChoice 필드가 바뀌면 이 16개는 **컴파일부터 깨진다**(위 e-1의 132개 목록에
  포함됨).
- JSON 파서(`ScenarioTurnChoices`)와는 독립이라 e-2의 파서 문제와 별개로,
  이 파일 자체가 "메인라인 체크포인트"의 두 번째 정답지 역할을 한다 — 개정 후에도
  같은 체크포인트(m4 잡기+1, m9 잡기+2 등)를 재현하려면 이 파일도 새 TurnChoice
  형태로 다시 옮겨 적어야 한다.

---

## f. OPPONENT_BRAIN 습관 H1~H4 — 기술 탭 도입 시 재정의 필요 지점

습관 정의(`HabitCatalogue.cs:26-91`)는 전부 `HabitAction(TurnChoice, TechniqueSpec)`
쌍이다:

| 습관 | 현재 액션 (`HabitCatalogue.cs`) | 기술 탭 도입 시 |
|---|---|---|
| H1 업어치기 | `TurnChoice.Neutral` + 기술(:26-33) | 이미 Core/Legs가 Neutral — **재정의 불필요**, c항 선택지 A와 이미 같은 모양 |
| H2 안뒤축감아치기 | `(None, Neutral, Attack)` + 기술(:41-47) | **Legs=Attack이 남아있다** — c항이 "기술 턴엔 발 선택 없음"으로 정해지면 이 Legs=Attack 값이 사양 위반이 된다. 재정의 필요 |
| H3 극단적 방어 | `(Evade, Defensive, Defend)`, 기술 없음(:53-60) | 기술이 없는 습관이라 **기술 탭과 무관** — 영향 없음 |
| H4/H4b 소매 노림 | `(Sleeve, Neutral, Neutral)`, 기술 없음(:70-91) | 기술이 없는 비공격 습관 — **영향 없음** (§4.1 "기술이 없으므로 비공격 습관" 그대로 유지) |

**H2만 실제로 걸린다.** H2의 Legs=Attack은 지금 §6.1 사다리 분류에 영향이 없다
(위 c항 확인대로 `realAttempt`가 우선). 그런데 H2의 이 TurnChoice는
`TendencyWindow.Record`에도 들어간다(단, B의 습관 커밋이 아니라 **A의** 커밋만
`_window.Record(a)`로 기록되므로 — `LiveMatchRunner.cs:137` — B의 습관 자체는
TendencyWindow에 안 들어간다. **즉 H2의 Legs=Attack이 §3에 미치는 영향은 없다**,
정정: c항에서 지적한 TendencyWindow 오염 문제는 **A의 기술 탭 사용에만 해당하고
B의 습관 카탈로그와는 무관**하다).

정리하면 개정안 (2)가 실제로 요구하는 재정의는:
1. **H2의 `LegAction.Attack`을 지운다**(c항이 "기술 턴엔 발 없음"으로 정해질 경우에만
   — c항이 다른 선택지로 정해지면 불필요할 수 있음).
2. `HabitBrainCommitSource.Fallback()`의 견제 발기술(`ActionFor(Move.Poke)`,
   `HabitBrainCommitSource.cs:170-173`)도 `(None, Neutral, Attack)` + Poke기술
   조합이다 — 견제 발기술은 §5.1에 자기 행(row)이 있는 정식 기술이라 "기술 탭"
   대상인지부터 확인 필요(포크가 기술 탭에 들어가면 여기도 같이 재정의 대상).
3. **개정안 (1)(손 분리)은 H1~H4b 어느 것도 재정의를 요구하지 않는다** — 전부
   `OwnHand`를 명시 안 해서 기본값(`Hand.Right`)을 쓰고 있고(`HabitCatalogue.cs`
   전체에 `Hand.Right`/`Hand.Left` 명시는 `SeekGrip` 호출부의 `ownHand` 매개변수
   (`:71,77`)뿐), 손 슬롯이 둘로 늘어도 "한쪽만 채우고 나머지는 비움"이라는 지금
   패턴이 그대로 옮겨간다.

**개정안 (3)(위장 공격 가중)은 습관 카탈로그와 직접 접점이 없다** — `JudgeFeint`는
`RefereeSystem` 내부에서 `_feintCandidate` 상태로만 도는 로직이라 습관 정의를 안
건드린다. 다만 H2("지도 뒤 만회")처럼 그립·체력이 불리한 채로 무리하게 기술을 내는
습관은 개정안 (3)이 켜지면 **위장 공격으로 오탐지될 확률이 올라가는 상호작용**이
생긴다 — H2는 위장이 아니라 진짜 시도(§4.1: "견제기가 아니라 진짜 시도")인데, 그립
불리 상태에서 반복 발화하면 그립/포지션 가산이 낀 탐지 로직이 실제 위장과
구분을 못 할 수 있다. 수치가 없어 코드로 확인은 못 했고, d항의 "불리 정의"가
정해진 뒤에야 검증 가능 — **확인 필요 항목으로 남긴다.**

---

## 추가 확인 — 42노드 대조 모드의 공격자 결정 (e항 보완)

`ScriptedAssembler.For`(`ScriptedAssembler.cs:53-72`)를 읽어 e항의 미확인 항목을
닫았다. 42노드 대조 모드의 공격자 결정은 **세 번째로 다른 메커니즘**이다:

- 노드 본문에 `[판정식]` 또는 `[판정식/B]` 줄이 있으면, 그 줄의 **정규식 접미사**
  (`FormulaLine`, `ScriptedAssembler.cs:28`: 접미사 없으면 A, `/B`면 B)로 공격자를
  정한다 — `TurnChoice.Legs`도 `CoreStance`도 안 본다.
- `[판정식]` 줄이 없는 노드(잡기싸움 턴)만 `GripTurn`(`:83-96`)에서
  `ScenarioTurnChoices.TryParse`가 만든 `c.Legs == LegAction.Attack`을 공격자
  판단에 쓴다(`:89-94`, 견제기 전용 — m3 같은 "하체 발기술" 노드).

즉 42노드 안에서도 "누가 공격자인가"는 **기술 강도에 따라 서로 다른 소스**(판정식
줄의 A/B 태그 vs Legs=공격 텍스트)에서 나온다 — `LiveMatchRunner`(Legs=공격 하나로
통일, a-6 이전 절 참조)와도, 사용자가 전제한 "코어=공격적"과도 다른 **세 번째
메커니즘**이다. 이건 42노드가 기술 탭과 직접 충돌하는 지점을 하나 더 확정한다:
**`[판정식]` 줄의 A/B 태그는 기술 탭이 생겨도 그대로 남아야 한다** — 이 태그가
없어지면 대조 모드의 공격자 판정 자체가 무너진다(코어·발·손 축과 무관하게 동작하는
경로라서 오히려 기술 탭 도입의 영향을 안 받는다는 뜻이기도 하다).

`GripTurn`의 견제기 경로(`c.Legs == LegAction.Attack`)는 f항에서 지적한 H2와 같은
성격의 문제를 하나 더 만든다 — 이 텍스트 기반 견제기 판정도 "기술 턴엔 발 선택
없음"이 확정되면 재검토 대상이다(m3 같은 특정 노드에 한정된 영향이라 규모는
작다).

## 조사 중 확인 못 한 것 (범위 밖 또는 시간 제약)

- `ScriptedCommitSource.cs`/`ScriptedReadResolver.cs`/`MatchRunner.cs` 자체의
  세부 배선(어셈블러가 만든 `ExchangeScript` 리스트를 러너가 어떻게 순서대로
  소비하는지) — e항 결론(공격자 결정 메커니즘)에는 영향 없음을 위에서 확인했으나,
  코드는 안 열어봄.
- d항의 "포지션 불리" 정의가 결정되기 전에는 그 정의를 쓰는 어떤 코드 경로도
  실측할 수 없다 — 이건 이 조사의 한계가 아니라 개정안 (3) 자체가 아직 미정의라
  구조적으로 못 채운 항목.
