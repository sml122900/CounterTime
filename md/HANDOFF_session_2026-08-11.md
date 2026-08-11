# HANDOFF — 2026-08-11 세션 종료 시점 (클로드 코드용)

> 이 문서는 **상태 스냅샷**이다. 작업 지시서가 아니다 — `md/HANDOFF_claude_code_runner.md`,
> `md/HANDOFF_claudecode_opponent-brain.md` 두 문서와 성격이 다르다(그쪽은 "무엇을 하라",
> 이 문서는 "지금 뭐가 어디까지 됐다"). 다음 세션은 이 문서로 현재 위치를 파악하고,
> 실제 작업 지시는 `md/STATUS.md`의 "Next Up"과 사람의 새 지시를 따를 것.
>
> 1행 스탬프는 이 문서엔 없다 — "현재 값"을 스스로 들고 있는 문서가 아니라, 아래 §1의
> md 3종 스탬프를 **그때그때 다시 실측해서 인용**하는 문서이기 때문이다. 스탬프는 절대
> 손으로 옮겨 적지 말 것 — `sh tools/stamp.sh --check md/*.md`로 항상 재확인.

---

## 0. 읽는 순서 · 권위

1. `md/MATCH_RULES.md` — 규칙 단일 출처. **코드와 충돌하면 이 문서가 이긴다.**
2. `md/CORE_DESIGN_MatchEngine.md` — 구현 설계
3. `md/OPPONENT_BRAIN.md` — B(상대) 습관·읽기 사다리 설계
4. `md/STATUS.md` — 세션 로그 유일본. `CLAUDE.md`에서 자동 임포트된다.
5. 이 문서 — 위 넷을 읽고 나서 보는 요약. 새 세션 시작점.

---

## 1. 문서 스탬프 (2026-08-11 세션 종료 시점 실측)

`sh tools/stamp.sh --check md/MATCH_RULES.md md/CORE_DESIGN_MatchEngine.md md/OPPONENT_BRAIN.md`
전부 exit 0 (본문과 스탬프 일치 확인됨).

| 문서 | 스탬프 | 상태 |
|---|---|---|
| `md/MATCH_RULES.md` | `<!-- v3.8 \| body:17a7bbb -->` | **v3.8 — §4.4 흔들기는 여전히 [초안 — 사람 검토 전]**. v3.7→v3.8은 쿠즈시→흔들기/Setup 개명(용어만, 규칙·동작 무변경)이 사유. |
| `md/CORE_DESIGN_MatchEngine.md` | `<!-- v3.2 \| body:a24d23c -->` | 무수정 (이 세션 포함 여러 세션째 그대로) |
| `md/OPPONENT_BRAIN.md` | `<!-- v1.0 \| body:5d5a8c7 -->` | 무수정 |

**v3.8의 [초안] 항목 — §4.4 흔들기 강등 규칙.** `SetupGateEnabled` 플래그 뒤(기본 **off**,
켜지 않으면 대조 모드는 이 절 이전과 완전히 동일하게 동작). 사람 검토 후 확정하거나
되돌릴 것 — STATUS.md "Next Up"에 이미 남겨둔 미결 사항.

`git config core.hooksPath` → `tools/githooks` 확인됨(이 클론). **새 클론에서는 최초 1회**
`git config core.hooksPath tools/githooks` 를 실행할 것 — 안 하면 스탬프 훅이 조용히
아무것도 안 하고, 손으로 편집한 스탬프가 커밋에 그대로 들어간다.

---

## 2. 프로젝트 · 개발 환경

**한 줄 소개**: 격투 스포츠(유도부터)의 수싸움·심리전을 선택지 기반으로 옮긴 게임 —
상대의 습관을 관측해서 읽고 역이용하는 구조. 종목 순서 유도→복싱→킥복싱→MMA
(유도부터 완성). HTML 프로토타입에서 검증한 시스템을 Unity(C#)로 이식하는 단계.

| 항목 | 값 |
|---|---|
| Unity | 6.3.21f1 LTS / URP |
| 리포 경로 | `C:\project_2026\CounterTime` |
| 원격 | `https://github.com/sml122900/CounterTime.git` |
| 현재 브랜치 | `feat/match-engine` (origin에서 7 커밋 앞섬 — 아직 미푸시) |
| Unity AI Assistant (MCP) | 연결됨, `unity-mcp` |

**asmdef 셋 — 실측 (7개 파일, 3개 주요 + 각 Tests + Editor 1개)**:

| asmdef | 경로 | 성격 |
|---|---|---|
| `CounterTime.Core` | `Assets/_Project/Scripts/Core/` | 순수 C#, `noEngineReferences:true`, UnityEngine 참조 0 |
| `CounterTime.Core.Tests` | `Assets/_Project/Scripts/Core/Tests/` | Core 격리·판정 테스트 |
| `CounterTime.Brain` | `Assets/_Project/Scripts/Brain/` | 순수 C#, Core만 참조. B(상대) 습관·읽기 사다리 |
| `CounterTime.Brain.Tests` | `Assets/_Project/Scripts/Brain/Tests/` | 브레인 테스트 |
| `CounterTime.Game` | `Assets/_Project/Scripts/Game/` | UnityEngine.UI/TextMeshPro 참조, MonoBehaviour·UI |
| `CounterTime.Game.Tests` | `Assets/_Project/Scripts/Game/Tests/` | 러너 규칙 테스트(일부는 Unity Test Runner 전용 — §3 참조) |
| `CounterTime.Game.Editor` | `Assets/_Project/Scripts/Game/Editor/` | 에디터 전용 메뉴(`PlayModeSetupMenu`) |

경계 규칙(`CLAUDE.md` 원문): Core에 `using UnityEngine;`이 등장하면 설계가 샌 것 — 즉시
걷어낼 것. Game → Core 단방향 참조만.

**새 클론마다**: `git config core.hooksPath tools/githooks` (위 §1 참조).

---

## 3. 지금까지 — 1~6단계 완료 범위

| 단계 | 내용 | 상태 |
|---|---|---|
| 1~4 | 시나리오 러너(탐색 모드), 노드↔모션, 매치 엔진 코어(잡기싸움·판정·상성표) | 완료 |
| 5 | 상대 브레인(습관 카탈로그·읽기 사다리·관측 원장), §9 굳히기 | 완료 |
| 6 | A 전용 실전 입력 UI(`LiveAssembler`/`LiveMatchRunner`/`LiveMatchPanel`) | 완료, §4 참조 |
| 7 | 회귀 + STATUS 갱신 | 진행 중(이 문서가 그 일부) |

**테스트 수 (2026-08-11 세션 종료 시점 실측, `dotnet run -- --check`)**:

| 어셈블리 | 개수 | 비고 |
|---|---|---|
| `CounterTime.Core.Tests` | 143 (11개 파일) | 하니스 전량 |
| `CounterTime.Brain.Tests` | 48 (5개 파일) | 하니스 전량 |
| `CounterTime.Game.Tests` | 21 (5개 파일) | **18개만 하니스에서 돈다** — `LiveAssemblerTests`(6)/`ResultNarrationTableTests`(8)/`ScenarioTurnChoicesTests`(2)/`ScriptedReadResolverTests`(2). 나머지 `MatchRunnerHandActionTests` **3개**는 `new GameObject`/`AddComponent`/`DestroyImmediate`를 써서 진짜 Unity 오브젝트 모델이 필요 — **Unity Test Runner에서만** 돈다(`Window > General > Test Runner > EditMode > Run All`). |
| **합계** | **212** (하니스 209 + Unity 전용 3) | |

지문(`MatchRunner.Mainline`, 42노드 시나리오 대조 모드): `CHECK: fingerprint matches
fingerprint-baseline.txt` — **IDENTICAL**. 이 세션 전체(이전 세션들 포함, `tools/dotnet-harness/`
커밋된 이후)에 한 번도 의도치 않게 깨진 적 없다.

`tools/dotnet-harness/`는 리포에 커밋돼 있다(2026-08-10 밤 이후 방침 — "세션마다 재구축"
관행은 뒤집혔다). `dotnet run -- --check`로 재구축 없이 재현·비교된다.

---

## 4. 이번 세션(2026-08-10~11)에 확정된 것

### 4.1 흔들기(구 쿠즈시) — 규칙 + 전면 개명

- **규칙**(§4.4, [초안]): 자기 그립이 하나라도 있어야 선언 가능 → 성립하면 1교환 지속,
  판정 미발화(견제 발기술과 동급) → 그 상태 없이 큰기술(`BigThrow`)을 내면 판정식에
  들어가는 기술급이 `Sloppy`로 강등 → `SurpriseExempt=true`인 기술(말아업어치기 등)은
  면제 → 그쳐/굳히기 진입/경기 종료 시 소멸. `JudgementCalculator`는 무수정 — 강등은
  `MatchEngine` 컨텍스트 단계(기술급을 만드는 자리)의 일이지 판정식 자체의 새 항이 아니다.
- **개명**(2026-08-11 오후, 규칙·동작 무변경): "쿠즈시"는 유도 전용 어휘라 타 종목 확장 시
  안 맞는다 — 화면 표시 **"흔들기"**, 코드 식별자 **`Setup*`**(`HandTarget.Setup`,
  `SetupGateEnabled`, `SetupCost`, `SetupDeclared`, `IsSetupActive`,
  `SetupPrecedingCommitSource` 등). 전수 grep으로 잔존 "쿠즈시" 0개 확인(이 파일 이전
  세션 로그·`docs/overnight-*`·`decisions/`·`daily/`는 기록이라 그대로 둠 — 그 시점엔
  그 이름이 맞았다).
- B 쪽 구현: `KuzushiPrecedingCommitSource`(현재는 `SetupPrecedingCommitSource`로 개명)
  — 습관 카탈로그·읽기 사다리는 한 글자도 안 건드리고, 이미 나온 결정을 한 턴 버퍼링해서
  늦춰 내보낼 뿐이다. **B의 흔들기는 현재 100% 정직한 tell**이다(보이면 다음 교환에
  반드시 진짜 큰기술이 온다) — 페이크(설치만 하고 안 던지기)는 미구현, 후속 설계.

### 4.2 §9 굳히기 — v3.5/v3.6 명문화

- **v3.5**: §9.1 "attacker"는 **판정의 득점자**(`JudgementScored.ScoredBy`)를 가리킨다고
  확정 — 되치기 득점도 동일 적용(절반은 무조건 진입, 유효는 득점자 잡기 ≥+1 AND 상대
  체력<50). 이전엔 유효 되치기가 진입 자체를 안 하도록 비워뒀던 것을 이 확정으로 닫았다.
- **v3.6**: §9.1의 "체력 <50"을 §4 판정식의 체력 보너스 항과 **같은 스냅샷(지불 전)**으로
  통일. 이전엔 굳히기 게이트가 스태미나 지불이 끝난 뒤(지불 후) 값을 다시 읽어서, 같은
  교환 안에서 판정식과 게이트가 서로 다른 숫자로 "체력<50"을 물었다 — `EnterViaYuko()`
  테스트 헬퍼의 `DrainB(14)`가 실은 14번째 호출 자체가 이미 득점·진입까지 끝내고 있던
  이중 진입 버그(§6 판단 대기 참조)를 잡는 과정에서 발견해 고쳤다. `DrainB(13)`으로 수정.
- §9.3에 v1 기본 정책 명문화: 실제 입력이 없는 헤드리스/스크립트 구동에서는 **어긋남
  (빠져나가기×따라가기)** 고정 — 홀드는 항상 `escapesNeeded×5초`(5~15초)만에 탈출로
  닫히거나 그전에 점수(10초 합산 한판, 또는 판정)로 매치가 끝난다. **20초 한판 마크에는
  이 정책으로 구조적으로 도달 불가**(escapesNeeded 상한이 3이라 15초가 최대).
- `MatchEngine.ResolveExchange`는 이제 `MatchPhase.Osaekomi` 가드를 갖는다 — 굳히기가
  열린 채로 다시 불리면 `InvalidOperationException`. 굳히기 중 진짜 서기 교환을
  허용해야 하는지는 **여전히 미결**(§6 참조).

### 4.3 결과 노드 서술 사전 — 현재 8키 등록, m18은 등록 해제됨

`ResultNarrationTable.CreateDefault()`가 `docs/step4-narration-worksheet.md` 배정을
등록한다. **실측(이 세션 종료 시점 소스)으로 8키** — m11/m12(업어치기 되치기·정타),
m14×2(밭다리·허리후리기 무득점), m17(밭다리 절반), m32(말아업어치기 절반, 기술
미구현이라 키만), m33(말아업어치기 한판 되치기), m35+m36(밭다리 한판). **m27·m34는
의도적으로 미등록**(폴백 유지 — 사유는 워크시트 원문). **m18도 의도적으로 미등록** —
2026-08-11 아침 세션에서 등록 해제 결정(사유: 문구가 "세 번째"를 명시해 실전 브레인
경로의 첫 되치기에도 그대로 뜨면 오정보가 됨). 시나리오 개정 대기 —
`docs/42-node-audit.md §6`. 엔트리가 없으면 폴백(`"{TechniqueId} — {판정명}"`,
`IsFallback=true`, 화면에서 회색으로 구분)만 낸다 — 문구를 지어내지 않는다.

### 4.4 6단계 UI — LiveAssembler / LiveMatchRunner / LiveMatchPanel

- **`LiveAssembler`**(순수 C#, `CounterTime.Game` 안이지만 MonoBehaviour 아님, 하니스에서
  헤드리스로 돈다): 코어(3)·하체(4)는 게이트 없이 항상 전부, 손은 국면별(기본 5 + 그립
  있으면 변환 둘 + 플래그 on이고 그립 있으면 흔들기). 뜯기·잡기 유지 게이트는 §2 표에
  이제 명문화됨(§4.2 이전 세션에서 추론했던 것을 문서로 확정).
- **`LiveMatchRunner`**(오케스트레이션 전용 MonoBehaviour, `MatchRunner`의 42노드 대조
  경로와 완전 별개): A는 `LiveAssembler`+패널 버튼, B는 `HabitBrainCommitSource`(+
  플래그 on이면 `SetupPrecedingCommitSource`로 감쌈)가 스스로 정한다. 공개 API:
  `Engine`/`A`/`Tendency`/`Config`/`Ledger`/`OpponentHabits`(읽기 전용) +
  `SubmitA`/`SubmitOsaekomi`/`BDefenderPolicy`/`BHolderPolicy` + 이벤트
  `Presented`/`Narrated`/`OsaekomiOpened`/`OsaekomiClosed`/`APreempted`.
- **`LiveMatchPanel`**(런타임 생성 캔버스, 씬 무수정): 이번 세션에 **전면 재설계**했다 —
  상단(IJF 점수판, 사진 레퍼런스 규격: 큰 타이머·골든스코어 표기·IPPON 스트립·
  A/B 절반·유효·지도카드·누르기 카운트) / 좌상(경기 화면 자리 — 지금은 배경+안내문
  placeholder뿐) / 좌하(위상·체력·그립 상태 줄 + 서술 줄 + 최근 5교환 로그) / 우측
  사이드바 3종(**코치의 조언** — 상태 기반 v1 규칙 5종 / **수 노트** — A가 실제
  관측한 습관만 심증(1~2회)/확정(3회+)/폐기됨 3단계로 노출 / **내 성향** — 노출 경고선·
  위험선 상시 라벨) / 하단(입력 3열 + 선택확인줄 + 확정버튼). 전부
  `VerticalLayoutGroup`/`HorizontalLayoutGroup` 계산 배치 — anchorX 상수를 손으로
  안 찍는다(§6.3의 재발 방지 그 자체). B의 선택지·습관·심판 누산기 숫자는 어디에도
  없다 — 읽는 건 A 자신의 면과 A가 관측한 B의 습관뿐.
- 메뉴 `CounterTime > Play Mode Setup`(2클릭 준비: 새 빈 씬 + 컴포넌트 부착 + 흔들기
  on + 한글 폰트 연결) → Play 버튼만 누르면 된다.

### 4.5 골든스코어 배선 수정

`MatchClock.RegulationExpired`/`ScoreBoard.WinnerAtTimeUp()` 둘 다 그 자체론 정확했지만
호출부가 0개였다(격리 단위테스트 밖). `ScoreBoard.FinishAtTimeUp()`(신규, 기존 공개 API
둘만 조합) + `MatchEngine.ResolveExchange` 끝(그쳐/굳히기 진입 판정 뒤)에서
`!Board.IsFinished && !Osaekomi.Active && Clock.Phase!=GoldenScore && Clock.RegulationExpired`
조건으로 호출하는 배선만 추가. 새 이벤트 `GoldenScoreEntered` 1개. **의도적으로 손 안 댄
경우**: 정규시간 만료 순간 이미 굳히기가 막 열려 있으면 시간종료 판정을 안 한다(§1이
"이미 진행 중인 굳히기가 정규시간을 넘겨도 계속 도는가"를 규정 안 함 — `GoldenScoreTests.
OsaekomiJustOpened_AtRegulationExpiry_DoesNotTriggerTimeUp_Unresolved`가 현재 동작을
사실로 고정).

---

## 5. ★ 다음 세션 최우선

### 5.1 좁은 화면 레이아웃 붕괴 수정 — **이 세션 안에서 완료됨**

719px 스크린샷 신고로 시작 → 진단 결과 CanvasScaler 설정(match=0, 겉보기 폭은 실제
화면 폭과 무관하게 항상 1920)은 원인이 아니었고, 진짜 원인은 **해상도와 무관한 구조적
버그**: 교차축(가로 그룹의 세로, 세로 그룹의 가로)에 `preferred/min/flexible` LayoutElement
필드를 아무것도 안 준 요소는 화면 크기와 무관하게 항상 폭/높이 0으로 무너진다(Unity
실측으로 확인한 규칙). `SetFixedHeight`/`MakeButton`/`MakeFlexText`/`SetFlexible` 전체에
교차축 값을 명시하는 것으로 수정, TMP auto-size + 버튼 라벨 줄바꿈 금지 추가.

**검증 완료**: 1920×1080/1280×720/1024×768/719×404 + 여분 600×338/320×180 총 6개 해상도
— 63개 요소·zeroSize=0·쌍별 겹침=0·버튼 라벨 줄바꿈=0 전 구간 동일. 719px 실제 클릭까지
재확인. 하니스 209/209, 지문 IDENTICAL. **권장 최소 지원 폭 960px**(버튼 글자 실측
추정 ≈9px) — 이건 폰트 크기 계산 추정이지 육안 스크린샷 확인이 아니다, 사람이 실제로
보고 판단할 것. 커밋 `2c2fbc2`.

**다음 세션이 할 일**: 이 수치(960px)가 실제로 적당한지 사람이 눈으로 확인. 그 외
후속 조치 불필요 — 구조적으로는 320px까지 실측 확인됐다.

### 5.2 사람 플레이 피드백 수집 (그다음 우선순위)

Stage 6이 완료됐지만 **사람이 Play를 실제로 눌러본 피드백은 아직 거의 없다**(이 세션
직전까지 "화면 실행 확인은 사람 몫"이 반복 기록돼 있었다). `CounterTime > Play Mode
Setup` → Play 버튼 → 실제로 몇 판 돌려보고:

- 3분할+사이드바 레이아웃이 실제로 읽기 쉬운가(특히 수 노트 3단계 표시, 코치 조언
  문구 5종 — 전부 [v1 임시 문구], §7 카피 교체 목록 참조)
- 굳히기 2지선다 전환이 매끄러운가
- 흔들기(§4.4 [초안])를 켜고/끄고 밸런스 체감이 어떤가 — 확정할지 되돌릴지는 사람 몫
- §6 판단 대기 목록(특히 동시 공격 우선순위, B 굳히기 고정 정책)이 실제 플레이에서
  거슬리는지

---

## 6. 판단 대기 · 미결 목록

`docs/overnight-2026-08-10.md`, `docs/overnight-2026-08-11.md` 원문을 읽고 옮긴다.
**이미 해결된 항목과 여전히 열린 항목을 구분**했다 — 2026-08-11 아침 세션에서 5개 중
4개가 결정됐다(STATUS.md 세션 로그 근거).

### 6.1 해결됨 (참고용 — 재작업 불필요)

| 원 항목 | 결정 | 근거 |
|---|---|---|
| m18 서술 키의 TechniqueId가 실제 기술(안뒤축감아치기)과 다름 | **키 등록 해제** — 시나리오 개정 대기로 이관 | §4.3, `docs/42-node-audit.md §6` |
| 습관 관측 알림(`NoteOpponentHabit`) 호출 시점이 결정 시점이라 흔들기 설치 교환에서 먼저 소비됨 | **호출 시점을 "결정 시점"에서 "실제 제출 시점"으로 이동** — 사다리·카탈로그는 무수정 | `LiveMatchRunner.SubmitA`의 `_resolver.NoteOpponentHabit(_bSource.LastDecision)` 호출 위치(커밋 시점) |
| `LiveMatchPanel` 런타임 캔버스에 `GraphicRaycaster`가 빠져 버튼이 안 눌림 | **`BuildCanvas`에 정식 추가**, 에디터 우회 제거 | `LiveMatchPanel.BuildCanvas` — `typeof(GraphicRaycaster)` 확인됨 |
| 뜯기·잡기 유지의 `LiveAssembler` 게이트가 §2 표가 아니라 §3.2 서술에서 추론한 것 | **§2 표에 명문화** | MATCH_RULES.md §2, "Hand — 뜯기"/"Hand — 잡기 유지" 행 |

### 6.2 여전히 열림 — 사람 판단 필요

1. **동시 공격 우선순위 [v1 임시 규칙, 미결로 명문화됨].** 한 교환엔 공격자가 하나뿐 —
   A(Legs=공격)와 B(브레인 발화)가 같은 교환에 겹치면 **v1은 B가 항상 선행**한다(A의
   공격은 성립 안 함). `PlayModeSmokeTests` 선례를 그대로 물려받은 것 — 진짜 해소
   규칙(선제권 판정, 판정으로 흡수 등)은 범위 밖. MATCH_RULES §2에 "미결"로 명문화는
   됐지만 규칙 자체는 아직 안 정해짐.
2. **B의 굳히기 선택이 고정 정책이다 — 진짜 B 굳히기 AI 없음.** `LiveMatchRunner.
   BDefenderPolicy/BHolderPolicy`는 `PlayModeSmokeTests`와 같은 고정 어긋남 정책
   (Escape/Follow)을 그대로 반환한다. A가 UI로 실제 입력하는 건 자기 역할(홀더 또는
   디펜더) 한쪽뿐, 반대쪽은 이 고정값.
3. **열린 굳히기 위 서기 교환이 §9/§4.3 어디에도 없다.** `MatchEngine.ResolveExchange`
   가드(§4.2)는 "무조건 예외"만 할 뿐, 금지해야 하는 입력인지 다른 의미가 있는지는
   미정. `LiveMatchRunner.SubmitA`가 `Osaekomi.Active`면 즉시 no-op이라 러너 층에서
   먼저 막혀 실물로는 안 나타나지만, Core 규칙 차원의 정의는 없다.
4. **§9.1 굳히기 게이트의 "체력<50"을 어느 스냅샷으로 읽어야 하는지 문서 문장만으론
   확정 불가.** v3.6(§4.2)으로 코드는 지불 전 스냅샷으로 통일했지만, MATCH_RULES §9.1
   원문 문장 자체는 "지불 전/후" 구분을 명시하지 않는다 — 코드가 택한 해석을 문서가
   사후 승인한 것이지, 문서가 먼저 못박은 게 아니다.
5. **§9.3 헤드리스 기본 정책(어긋남 고정)은 지시받아 고른 값이 아니다.** 헤드리스/
   스크립트 구동에서 실제 입력이 없을 때의 기본값이고, 문서(§9.3)는 "이 짝은 맞물림,
   저 짝은 어긋남"이라는 판정 규칙만 정의할 뿐 자동 진행 시 기본값 자체는 다루지 않는다
   — v1 스탑갭이라는 사실을 잊지 말 것.
6. **흔들기(§4.4) 규칙 자체가 [초안].** 개명은 끝났지만 규칙 확정(수치·게이트·강등
   조건)은 아직 사람 검토 전이다. 작업6 실측(off/on 6프로필)이 참고 자료 —
   `docs/overnight-2026-08-11.md` 표 참조, 튜닝 없이 그대로 옮긴 숫자다.
7. **경기 길이(교환 수·잔여 시간)는 계속 의도적으로 미결.** §9 적용, 흔들기 on/off
   전후로 숫자가 여러 번 바뀌었지만 전부 실측만 하고 튜닝은 안 했다 — 6단계(사람 플레이)
   이후 사람 판단.

---

## 7. 알아둘 것 — 시뮬레이션이 구조적으로 못 밟는 경로

- **골든스코어**: 배선은 §4.5로 고쳤지만, 스모크 6구성(반복/변주/현실적×흔들기off/on)
  전부 정규시간(240초) 근처에도 안 간다(가장 오래 도는 것도 최대 추정 150~165초) — 그래서
  회귀망이 골든스코어 진입 경로 자체를 실전에서는 한 번도 안 밟는다. `GoldenScoreTests`
  7개가 유일한 커버리지(전부 격리 단위테스트로 직접 구성한 것).
- **지도 3장(한소쿠마케)**: 위와 같은 이유로 스모크에서 못 봄 — `RefereeSystemTests`의
  격리 테스트만 커버.
- **누르기 20초 한판 마크**: §9.3 v1 기본 드레인 정책(항상 어긋남)에서는 홀드가
  `escapesNeeded×5초`(최대 15초)만에 닫히거나 그전에 득점으로 끝나 **구조적으로
  20초에 도달 불가**. 표시 로직 자체는 옳다 — 안 닿는 건 §9.3 기본 정책 쪽 문제.
- **정규 240초 만료 자체**: 위 골든스코어 항목과 같은 이유로 실전 경로에서 미실측.
- **스모크 A는 허수아비다 — 밸런스 자료로 쓰지 말 것.** `PlayModeSmokeTests.RunMatch`는
  B의 습관 브레인이 그 턴에 기술을 하나라도 냈으면(`LastTechnique != null`) B가
  무조건 공격자이고, **아니면 A가 항상 공격자에 고정 기술("밭다리", BigThrow,
  Standard)을 던진다** — A의 그 턴 Core/Legs 선언은 이 배정에 전혀 관여하지 않는다.
  세 프로필(반복/변주/현실적) 이름이 암시하는 "A의 공격 성향 차이"는 실제로는 코드에
  반영 안 됨 — B의 습관 발화 여부만 영향을 준다.
- **`MatchRunner.Mainline` 지문(42노드 대조 경로)이 유효/절반 득점 경로를 안 덮는다.**
  이 경로가 실제로 내는 판정은 m14의 노포인트(None) 3번과 m36의 한판(Ippon) 하나뿐 —
  `TryEnterOsaekomi`가 도는 `Judgement.Yuko`/`WazaAri` 판정을 단 한 번도 안 낸다.
  §9 관련 변경(v3.5/v3.6)이 지문을 안 건드리는 이유가 우연이 아니라 이 구조적 이유다 —
  **지문 IDENTICAL을 §9 로직이 안 바뀌었다는 증거로 오해하지 말 것**, 그 경로를 그냥
  안 지나가는 것뿐이다. §9 로직의 실제 커버리지는 `OsaekomiTests`/`PlayModeSmokeTests`.

---

## 8. 반복된 실패 패턴 + 작업 방식에서 건진 것

**덤프 먼저, 진단은 나중.** 이번 세션의 좁은 화면 버그도, 지난 세션의 `EnterViaYuko`
off-by-one도 전부 "가설을 세우고 코드를 고치기 전에 실제 값을 실측"으로 풀렸다. 좁은
화면 사례: CanvasScaler 설정을 이론으로만 추론했다면 틀렸을 것(match=0이 겉보기 폭을
항상 1920으로 고정한다는 사실은 실측 전엔 반대로 예상했다) — `PlayModeWindow.
SetCustomRenderingResolution` + `RectTransform.rect`/`LayoutElement` 필드 직접 대조로만
진짜 원인(교차축 0폭 붕괴, 해상도 무관)을 찾았다.

**수치가 이상해 보여도 임의로 고치지 말고 보고한다.** 스모크 실측(§9 전후, 흔들기
on/off 전후)에서 교환 수·잔여 시간이 여러 번 크게 바뀌었지만(예: 반복 프로필 27→4
교환) 전부 "그대로 옮겼다, 튜닝 안 했다"고 명시하고 판단을 사람에게 넘겼다. 코드
경로가 실제로 바뀌어서 나는 숫자 변화와, 버그로 나는 숫자 변화를 구분 못 할 땐 **숫자
자체를 고치는 대신 숫자가 왜 그렇게 나오는지 코드 경로를 추적해 보고**한다(예: §6.3의
"유효 게이트를 막는 건 체력이 아니라 잡기" 분석).

**미지시 판단을 사람 지시로 귀속하지 않는다.** §9.3 헤드리스 기본 정책(어긋남 고정),
`PlaceholderATechnique`, 동시공격 시 "B가 이긴다" 우선순위 — 전부 "이렇게 골랐다,
이게 그 이유다, **지시받은 값이 아니다**"라고 명시적으로 구분해 기록했다. 나중에
"왜 이렇게 돼 있냐"고 물었을 때 "그렇게 하라고 하셔서"라는 답이 거짓이 되는 걸
막는다.

**"확인 안 됨"과 "확인해서 문제없음"을 구분해서 기록한다.** `LiveMatchRunner`/
`LiveMatchPanel`은 여러 세션 동안 "컴파일 0/0, 타입 로드 확인"과 "Play 모드 실행
확인"을 계속 분리해서 기록했다 — 전자가 후자를 보장하지 않는다(캔버스 레이아웃은
Unity Test Runner로도 헤드리스로도 검증 불가). 이번 세션의 좁은 화면 버그가 정확히
그 사각지대에서 나왔다 — 지난 세션의 "쌍별 겹침 0"은 사실이었지만 **폭 0인 요소는
애초에 겹칠 수 없어서 통과한 것**이었다. 검사 자체가 무엇을 못 잡는지도 같이 기록할 것.

**파일명·경로를 확인 없이 추측해서 고치지 않는다.** `HANDOFF_session_2026-08-10.md`
갱신 요청이 있었을 때, 그 파일이 리포에 존재한 적이 없다는 걸 `git log --all`로
확인하고 **진행하지 않은 채 보고**했다(엉뚱한 파일을 대신 고치지 않았다). 이 문서
(`HANDOFF_session_2026-08-11.md`)는 그 사례 이후 실제로 지시받아 새로 쓰는 것.

**Core에 필드 하나 추가하는 것도 지문을 깨뜨릴 수 있다.** `JudgementScored`에
`TechniqueId` 필드를 추가하려던 시도(1안)가 테스트는 다 통과했지만 지문 덤프 문자열에
새 필드가 찍혀서 지문이 안 맞게 됐다 — 판정 자체(Sum/Judgement/ScoredBy/IsCounter/
Terms)는 전혀 안 바뀌었는데도 절차상 되돌렸다. Core 이벤트 모양을 건드릴 땐 지문을
먼저 켜보고 확인할 것.

---

## 9. 대화 스타일 규약

- **사실과 해석을 문장 단위로 분리한다.** "~다, 실측으로 확인됨" vs "~로 보인다,
  해석이지 확정 아니다"를 섞지 않는다. 판단 대기 항목마다 이 구분이 반복된다(§6 참조).
- **숫자는 항상 실측 출처를 밝힌다.** "테스트 209개"가 아니라 "하니스 `dotnet run --
  --check` 실측 209/209" 식으로, 언제·어떻게 잰 값인지 남긴다. 기억으로 옮겨 적지
  않는다 — 이 문서의 모든 수치·해시도 작성 시점에 다시 실측한 것이다.
- **완료/미완료/확인 안 됨을 세 가지로 구분한다.** "컴파일 됨"과 "동작 확인됨"은 다른
  말이다 — 특히 UI는 컴파일이 통과해도 Play 모드에서 사람이 눌러봐야 확정된다.
- **규칙·수치 변경은 문서(MATCH_RULES 등)가 최종 권위이고, 코드가 문서와 다르면
  코드를 고치거나 사람에게 확인한다 — 문서를 코드에 맞춰 조용히 고치지 않는다.**
  스탬프가 이 원칙을 기계적으로 강제한다(§1).
- **판단이 필요한 지점에서 조용히 하나를 고르지 않는다.** 정하지 말고 "판단 대기"로
  이름 붙여 목록에 올린다 — 이 문서의 §5.2/§6 전체가 그 산출물이다.
- **작업 지시서(HANDOFF_claude_code_runner.md류)와 상태 스냅샷(이 문서, STATUS.md)의
  성격을 섞지 않는다.** 전자는 "무엇을 하라"고 단계별 커밋 지점을 못박고, 후자는
  "지금 뭐가 어디까지"만 담는다 — 이 문서에 새 작업 지시를 넣지 않았다.
- **한국어 존댓말이 아닌 서술체("~다/~했다/~이다")로 쓴다.** 표·코드 인용을 적극
  쓴다 — 산문으로 풀어 쓰는 것보다 표가 실측값을 더 정확히 전달할 때가 많다.
