# 기술 결정: 시나리오 러너의 Core / Game 경계 긋기

작성 2026-08-05 · 상태: **구현 완료, Play 검증 완료**

`docs/decisions/assembly-split-core-game.md` 에서 정한 어셈블리 분리 원칙을, 실제 기능 하나(시나리오 탐색 러너)에 처음 적용한 기록이다. 설계 문서는 "규칙 코드는 Core 로"라고만 말한다. 실제로 코드를 쓰다 보면 **어디가 규칙이고 어디가 표현인지 애매한 지점 3개**가 나왔고, 그 판단이 이 문서다.

## Problem — 문제

시나리오 보드가 내보낸 JSON(노드 43 / 엣지 52)을 게임에서 탐색 가능하게 만들어야 한다. 노드마다 title / content / 테두리 색이 있고, 자식 노드가 선택지가 된다.

순진하게 짜면 전부 MonoBehaviour 한 개로 끝난다. `JsonUtility.FromJson` 으로 읽고, 딕셔너리 만들고, 버튼 뿌리고, 애니메이션 재생. 100줄이면 된다.

그런데 그 100줄 안에 **게임 규칙에 해당하는 것**이 섞여 있다.

- 노드 그래프의 구조(누가 누구의 자식인가)
- 테두리 색 → 노드 종류(상황서술 / A선택 / B행동 / 판정) 분류
- 시작 노드가 무엇이고, 종단 노드를 어떻게 판정하는가

이건 나중에 상대 성향 로직·판정 로직이 읽어야 하는 데이터다. UI 안에 있으면 안 된다.

동시에 **명백히 Unity 것**도 섞여 있다.

- `JsonUtility` — `UnityEngine` 소속
- `Color` 파싱 — `ColorUtility`
- 애니메이터 상태 이름

## Action — 한 것

경계를 세 지점에서 명시적으로 그었다.

### 1. JSON 파싱은 Game 에서, 데이터 타입은 Core 에

`JsonUtility` 는 `UnityEngine` 어셈블리에 있다. `Core` 는 `noEngineReferences: true` 라 **부를 수가 없다** — 컴파일러가 막는다.

그래서:

| 위치 | 역할 |
|---|---|
| `Core` : `ScenarioData` / `ScenarioNode` / `ScenarioEdge` | `[System.Serializable]` POCO. 필드명을 JSON 키와 1:1 로 맞춤 |
| `Game` : `ScenarioLoader` | `TextAsset` 을 받아 `JsonUtility.FromJson<ScenarioData>` 로 채우고 Core 에 넘김 |

`[System.Serializable]` 은 `mscorlib` 소속이라 Core 에 들어가도 된다. **타입은 Core 가 소유하고, 그 타입을 채우는 도구만 Game 에 있다.** 나중에 JSON 대신 다른 소스가 와도 Core 는 모른다.

필드명을 `id` `title` `content` `contentHtml` `x` `y` `border` 로 JSON 키 그대로 둔 건 JsonUtility 가 필드명 매칭이라서다. C# 컨벤션(PascalCase)을 지키려면 별도 DTO + 매핑 계층이 필요한데, 단일 사용처에 그건 과하다. (CLAUDE.md "Simplicity First")

### 2. 색은 Core 에서 문자열, Game 에서 Color

`NodeKind` 분류(`#2f6bad` → `ChoiceA`)는 **게임 규칙**이다. A의 선택인지 B의 행동인지는 판정 로직이 알아야 한다. → Core.

하지만 버튼을 그 색으로 칠하는 건 표현이다. `ColorUtility.TryParseHtmlString` 은 UnityEngine. → Game.

그래서 Core 는 `border` 를 **문자열 그대로** 들고 있고 `NodeKind` 만 계산한다. Game 이 필요할 때 그 문자열을 `Color` 로 판다. Core 에 색 타입을 만들지 않았다 — 단일 사용처에 자료형을 새로 파는 건 추상화 낭비다.

### 3. 모션은 Core 가 아예 모른다 — `NodeEntered` 이벤트만

노드에 들어갈 때 캐릭터가 어떤 애니메이션을 재생하는지는 100% 표현이다. 그런데 "언제 노드에 들어갔는가"는 Core 만 안다.

`ScenarioRunner` 에 이벤트 하나만 뒀다.

```csharp
public event Action<ScenarioNode> NodeEntered;
```

Core 는 여기까지다. 모션 이름도, Animator 도, CrossFade 도 모른다.

Game 쪽에:

- `NodeMotionMap` (ScriptableObject) — `{nodeId, stateA, stateB}` 리스트. 매핑이 없으면 둘 다 `Fight Idle`
- `ScenarioMotionBinder` (MonoBehaviour) — 이벤트 구독 → 맵 조회 → `Animator.CrossFade(0.25s)`

**`NodeMotionMap` 이 ScriptableObject 인 게 핵심이다.** assembly-split 문서 4번 규칙("상대 성향 데이터는 Core POCO + Game SO 래퍼")의 첫 실적용이다. 다만 여기선 매핑 자체가 표현 정보라 Core 쪽 POCO 가 아예 없다 — SO 만 Game 에 있다.

### 검토했다가 안 택한 것

- **Core 에 `MotionCue` 같은 추상 신호를 두고 Game 이 해석** — "모션"을 "추상 연출 신호"로 이름만 바꾼 것. Core 가 연출 타이밍을 알게 되는 건 똑같고, 매핑 테이블이 두 곳으로 쪼개진다.
- **`NodeMotionMap` 을 JSON 으로** — 인스펙터에서 못 만진다. 매핑 36개를 손으로 다듬을 텍스트인데 저작 도구를 버릴 이유가 없다.
- **Animator 상태를 enum 으로** — 클립이 추가될 때마다 코드 수정 + 재컴파일. 문자열이면 에셋만 고치면 된다. 대신 오타가 런타임까지 살아남으므로, `Animator.HasState` 로 검사해 경고를 띄우는 안전장치를 넣었다.

## Result — 결과

- `Scripts/Core/` 전체 grep 에 `UnityEngine` **0건** (커밋 시점 확인)
- 모션 연결 커밋(`992a77b`)에서 `Scripts/Core/` diff **0줄** — 표현 기능을 하나 통째로 붙이는데 Core 를 안 건드렸다. 경계가 실제로 작동한다는 뜻
- Core 로직만 에디터에서 단독 실행해 검증 가능했다. Play 없이 JSON 파싱 → 그래프 구성 → n1 에서 종단까지 순회 → 도달 가능 노드 42/43 까지 전부 확인
- 부수 소득: **n0(범례 노드)를 제외하는 코드를 안 짜도 됐다.** 그래프를 데이터 그대로 만들고 러너 시작점만 n1 로 두니, n0 을 가리키는 엣지가 없어서 자연히 도달 불가가 된다. 특수 케이스 분기 0개

### 남은 위험

- Animator 상태 이름이 문자열이라 오타가 컴파일에 안 잡힌다. `HasState` 경고로 완화했지만 런타임까지 간다
- `ScenarioRunner.NodeEntered` 구독자가 둘(UI, 모션)이고 순서 보장이 없다. 지금은 서로 독립이라 문제없지만, 연출이 UI 상태에 의존하기 시작하면 순서를 명시해야 한다
- `NodeMotionMap.Resolve` 는 36개 선형 탐색이다. 노드 수가 수백이 되면 딕셔너리로 바꿔야 한다 (지금 바꾸는 건 조기 최적화)

## 이력서 한 줄

> 시나리오 그래프 러너를 구현하며 JSON 파싱(JsonUtility)·색 파싱·애니메이션 재생 등 엔진 의존 코드를 표현 계층에 격리하고, 도메인 계층은 노드 진입 이벤트만 발행하도록 설계해 연출 기능 추가 시 도메인 코드 변경 0줄을 달성했습니다.
