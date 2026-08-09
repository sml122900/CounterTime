# CLAUDE.md — 프로젝트 컨텍스트

> 이 파일은 Claude Code가 항상 읽는 프로젝트 개요다.

**작업을 이어갈 때는 진행상황부터 확인할 것.** 진행상황은 전부 `md/STATUS.md`
**한 곳에만** 기록한다 — 루트나 다른 폴더에 사본을 만들지 마라. 아래 임포트로
매 세션 자동으로 읽힌다.

@md/STATUS.md

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
      Scripts/Core/Tests/ → CounterTime.Core.Tests.asmdef
      Scripts/Game/   → CounterTime.Game.asmdef (UI / Managers)
      Scripts/Game/Tests/ → CounterTime.Game.Tests.asmdef
      Scenes/Main.unity
      Scenario/            시나리오 원본 (JSON, 시나리오 보드 HTML 등) — 시나리오 파일은 전부 여기
      ScriptableObjects/   상대 성향 데이터 에셋
      Spikes/              검증 장치 (아래 참조)
      Art/ Audio/ Prefabs/ Settings/

    md/                    설계 문서. STATUS.md · MATCH_RULES.md · CORE_DESIGN_MatchEngine.md
    CLAUDE.md              프로젝트 루트 (이 파일)

## Spikes — 검증 장치이지 프로덕션이 아니다

`Assets/_Project/Spikes/` 는 규칙 문서가 **인용하는 실측을 재현하는 장치**다.

- **프로덕션 코드가 여기를 참조하면 안 된다.** `Main.unity` 도 참조하지 않는다.
- **지우지 말 것.** `MATCH_RULES §7` 이 HUD 를 하드 요구로 못 박은 근거가
  `spike/lowres-render` 의 실측("320×180 에서 손은 5.8px")이다. 측정 장치가 한 대의
  디스크에만 있으면, 규칙을 리포 밖에만 적어둔 것과 같은 실패가 된다.
- **스파이크의 인스펙터 값은 사양이 아니다.** 예: 스파이크 씬의 `nearMissSeconds` 는
  폐기된 초안값 `0.08` 이다. 확정 규격은 `0.5` 이고 출처는 `md/STATUS.md` 다.

## 테스트 — 어셈블리가 둘이다

| 어셈블리 | 무엇 | 어떻게 돌리나 |
|---|---|---|
| `CounterTime.Core.Tests` | 순수 판정·시스템 | Unity Test Runner **또는** dotnet 콘솔 하니스 |
| `CounterTime.Game.Tests` | 러너 규칙(자세 유지·손 행동 소비 등) | **Unity Test Runner 에서만** |

`Core` 는 UnityEngine 을 안 쓰므로 scratchpad 의 dotnet 하니스로 Unity 없이 돌릴 수 있다.
`Game` 은 MonoBehaviour·TextAsset 을 쓰므로 **그 하니스로는 못 돈다.**

> **"Core N개 초록"은 Game 테스트가 검증됐다는 뜻이 아니다.**
> 에러가 안 나면서 안 돌고 있는 부류라, 사람이 직접 확인해야 한다:
> `Window > General > Test Runner > EditMode 탭 > Run All`

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

## Document stamps

`md/MATCH_RULES.md` and `md/CORE_DESIGN_MatchEngine.md` carry a body stamp on line 1:

    <!-- v3.1 | body:0a1b2c3 -->

- The hash is `git hash-object` over the file WITHOUT line 1, with CR stripped,
  so writing the stamp never changes what the stamp measures.
- Maintained by `tools/githooks/pre-commit`. Never edit the stamp by hand.
- The hook keeps no list of paths. Any staged `*.md` whose line 1 already matches
  the stamp format is maintained; everything else is ignored. Adding the
  placeholder `<!-- vX.Y | body:0000000 -->` to a doc IS the registration step,
  and deleting the line deregisters it. Moving a doc therefore cannot break it.
- Enabling the hook is a per-clone step — see **Fresh clone** below.
- Verify any copy: `sh tools/stamp.sh --check <file>`
- Purpose: a doc pasted into a chat or copied anywhere else can be checked
  against the repo. If a copy's stamp does not match its own body, that copy was
  hand-edited and is NOT the source of truth. Do not reconcile against it —
  report the mismatch.
- Conventions belong in this file, in the repo. A rule kept only in an
  assistant's session memory is not a rule: it is invisible to diffs, to other
  tools, and to future sessions.

## Fresh clone

Run once: `git config core.hooksPath tools/githooks`

At session start, verify it: `git config core.hooksPath` must print `tools/githooks`.
If it prints nothing, the stamp hook is inert — set it before committing any
stamped doc, and run `sh tools/stamp.sh --check md/*.md` to catch stamps that
went stale while it was off.

A missing hook makes a stamp **stale, never wrong**, and a stale stamp is caught
the next time the doc is read. That is a delay surfaced at point of use, not a
silent error — so this check is the whole mitigation it needs.
