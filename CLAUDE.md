# CLAUDE.md — 프로젝트 컨텍스트

> 이 파일은 Claude Code가 항상 읽는 프로젝트 개요다.

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:

- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:

- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:

- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:

- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:

```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

---
# Role & Tone: Caveman Mode (Extreme Conciseness)
- Speak like a caveman: Remove all fluff, pleasantries, introductory, and concluding remarks.
- Do not say "Sure, I can help with that" or "Here is the solution."
- Eliminate articles (a, an, the) and filler words where possible, but maintain precise technical/coding terms.
- Focus ONLY on the core answer, solution, or code snippet.



- Reduce output token usage by 70%+.
- Deliver immediate value without making me read a textbook.

---


# COUNTER TIME — 프로젝트 규칙

격투 스포츠의 수싸움·심리전을 선택지 기반으로 옮긴 게임. 상대의 습관을 관찰·간파해서
역이용하는 구조. 종목 순서: 유도 → 복싱 → 킥복싱 → MMA (유도부터 완성).
HTML 프로토타입에서 검증한 시스템을 Unity(C#)로 이식하는 단계.

## 아키텍처 (가장 중요한 규칙)

전투 로직과 표현(Unity)을 **어셈블리 수준에서 분리**한다.

- `CounterTime.Core` — **순수 C#. UnityEngine 참조 금지** (`noEngineReferences: true`).
  잡기싸움/본수 판정, 상성표, 상대 성향·습관 로직, 데이터 타입이 전부 여기 산다.
  MonoBehaviour 없음. Unity 없이도 콘솔/테스트로 굴러가야 한다.
- `CounterTime.Game` — MonoBehaviour, UI, 입력, 씬 바인딩 등 **표현·연결 계층**.
  `CounterTime.Core` 를 참조한다. 여기서 Core 의 결정을 화면에 그린다.

**규칙:**
1. 게임 규칙/판정 코드는 절대 MonoBehaviour 안에 두지 않는다. 전부 `Core` 로.
2. `Core` 에 `using UnityEngine;` 이 등장하면 설계가 샌 것. 즉시 걷어낸다.
3. `Game` → `Core` 단방향 참조만. `Core` 는 `Game` 을 모른다.
4. 상대 성향 데이터는 `Core` 의 순수 데이터 타입(POCO)으로 정의하고,
   Unity 에서는 그걸 감싸는 ScriptableObject 를 `Game` 쪽에 두어 에셋으로 저작한다.
   (ScriptableObject 는 UnityEngine 타입이라 Core 에 못 들어감 — 이 경계를 지킬 것)

## 폴더

    Assets/_Project/
      Scripts/Core/   → CounterTime.Core.asmdef (Combat / AI / Model)
      Scripts/Game/   → CounterTime.Game.asmdef (UI / Managers)
      Scenes/Main.unity
      Scenario/            시나리오 원본 (JSON, 시나리오 보드 HTML 등) — 시나리오 파일은 전부 여기
      ScriptableObjects/   상대 성향 데이터 에셋
      Art/ Audio/ Prefabs/ Settings/

## 컨벤션

- 네임스페이스: `CounterTime.Core.*`, `CounterTime.Game.*`
- 새 `.cs` 를 만들면 Unity 가 `.meta` 를 함께 생성한다 — **둘 다 커밋**한다.
- 판정·상성 같은 핵심 로직은 가능하면 EditMode 테스트를 함께 둔다.

## 개발 루프 (Claude Code ↔ Unity, MCP 연결)

이 프로젝트는 Unity 공식 MCP 서버(AI Assistant 패키지)로 Claude Code 와 연결되어 있다.
- Claude Code 는 MCP 도구로 콘솔 로그 확인, 씬 계층 읽기, GameObject/컴포넌트 검사,
  스크립트 편집, 에디터 액션 실행이 가능하다.
- 스크립트 수정 후에는 MCP 로 콘솔을 다시 읽어 컴파일 에러가 없는지 스스로 확인한다.
- Play 실행과 게임플레이 판단은 사람이 한다.
- MCP 연결이 끊겨 있으면(브리지 중지 등) 사람에게 알리고, 그동안은 파일 편집만 진행한다.

## 사용자 컨텍스트

사용자는 Unity 를 처음 쓰는 초보다. 에디터 조작이 필요한 안내를 할 때는
메뉴 경로를 정확히(예: Edit > Project Settings > Editor) 명시한다.
