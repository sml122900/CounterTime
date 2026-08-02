# 기술 결정: 전투 로직 어셈블리 분리 (CounterTime.Core / CounterTime.Game)

작성 2026-08-03 · 상태: 결정 완료, 구현 전

## Problem — 문제

COUNTER TIME 은 격투 스포츠의 수싸움·심리전을 선택지로 옮긴 게임이다. 게임의 실체는 화면이 아니라 **판정 규칙**이다 — 잡기싸움 → 본수로 이어지는 2단계 턴, 기술 간 상성표, 상대의 습관을 관찰하고 역이용하는 성향 로직.

이 로직을 MonoBehaviour 안에 두면 세 가지가 동시에 막힌다.

1. **검증 불가** — 판정 하나 확인하려고 에디터를 켜고 Play 를 눌러야 한다. 상성표 같은 조합 폭발형 로직은 이 방식으로 감당이 안 된다.
2. **이식 불가** — 종목 확장 계획이 유도 → 복싱 → 킥복싱 → MMA 다. 규칙이 씬·프리팹에 얽혀 있으면 종목마다 UI째로 복사하게 된다.
3. **경계 침식** — Unity 타입(`ScriptableObject`, `Vector3`, `Time.deltaTime`)이 규칙 코드에 한 줄이라도 섞이는 순간, 되돌리는 비용이 계속 오른다.

이미 HTML 프로토타입에서 시스템은 검증했다. 지금은 그걸 Unity 로 **이식**하는 단계이고, 이식 방식이 이후 전부를 결정한다.

## Action — 한 것

컴파일러가 강제하는 경계를 쓰기로 했다. 폴더 규칙이나 팀 합의가 아니라 **asmdef**.

| 어셈블리 | 내용 | 제약 |
|---|---|---|
| `CounterTime.Core` | 잡기싸움/본수 판정, 상성표, 상대 성향·습관 로직, 데이터 타입 | 순수 C#. `noEngineReferences: true` — UnityEngine 참조 자체가 불가능 |
| `CounterTime.Game` | MonoBehaviour, UI, 입력, 씬 바인딩 | `Core` 를 참조. 단방향 |

파생 규칙 4개를 CLAUDE.md 에 명문화:

1. 게임 규칙/판정 코드는 절대 MonoBehaviour 안에 두지 않는다.
2. `Core` 에 `using UnityEngine;` 이 등장하면 설계가 샌 것 — 즉시 걷어낸다.
3. `Game` → `Core` 단방향만. `Core` 는 `Game` 을 모른다.
4. 상대 성향 데이터는 `Core` 에 POCO 로 정의하고, 이를 감싸는 ScriptableObject 를 `Game` 에 두어 에셋으로 저작한다.

4번이 이 결정에서 제일 미묘한 지점이다. ScriptableObject 는 UnityEngine 타입이라 `Core` 에 들어갈 수 없다. 그래서 **데이터 형태(POCO)와 저작 도구(SO)를 분리**하고, `Game` 쪽 SO 가 `Core` 의 POCO 를 들고 있는 래퍼가 된다. 기획자는 인스펙터에서 상대 성향을 만지고, 판정 로직은 그게 SO 로 왔는지 JSON 으로 왔는지 모른다.

### 검토했다가 안 택한 것

- **폴더로만 분리 (asmdef 없이)** — 규칙이 강제되지 않는다. `using UnityEngine;` 한 줄이면 경계가 무너지는데, 그 한 줄은 반드시 언젠가 편의를 위해 추가된다.
- **partial class / interface 로 추상화** — 단일 사용처에 추상화를 얹는 것. CLAUDE.md 의 "Simplicity First" 위반이고, 어셈블리 경계면 이미 충분하다.

## Result — 결과

- (기대) `Core` 는 Unity 없이 콘솔·EditMode 테스트로 굴러간다. 상성표·판정은 Play 없이 검증 가능.
- (기대) 종목 추가 시 `Core` 에 규칙 세트를 더하고 `Game` 은 그대로 둔다.
- (부수 효과) `noEngineReferences` 덕에 `Core` 컴파일이 빨라지고, 도메인 어셈블리가 Unity 리로드에 덜 묶인다.

**아직 구현 전이다.** 현재 프로젝트에 `Assets/_Project/Scripts/Core|Game` 폴더도, asmdef 2개도 없다. 설계만 CLAUDE.md 에 고정된 상태.

### 검증 방법 (구현 후 확인할 것)

- `Core` asmdef 에 `noEngineReferences: true` 가 실제로 들어갔는지
- `Core` 파일 전체 grep 에 `UnityEngine` 이 0건인지
- `Core` 만으로 도는 EditMode 테스트가 최소 1개 통과하는지

## 이력서 한 줄

> 격투 게임의 판정·AI 로직을 Unity 어셈블리(asmdef)로 분리해 엔진 의존성을 컴파일 타임에 차단, 에디터 실행 없이 규칙을 테스트할 수 있는 구조를 설계했습니다.
