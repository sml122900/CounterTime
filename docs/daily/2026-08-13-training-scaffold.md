# 2026-08-13 작업 일지 — 훈련 클립 스캐폴드

> `DESIGN_training-draft.md` §5(A) 범위 — 훈련 화면 + 종목 선택 + 능동 렙 입력 +
> 방전 연출 + 종목별 모캡 클립 슬롯. 스탯/숙련 진행 연결(§5-B)은 §H 7 이월, 문
> (호출 지점)만 냈다. 경기 엔진(`MatchEngine`/`Brain`/⚙)은 세션 내내 무수정 —
> 하니스 257/257, 42노드 지문 IDENTICAL.

## 한 줄 요약

지도 "유도장"의 빈 placeholder를 실제 훈련 화면으로 채웠다. 13종목 중 3~4개를
골라 돌아가며, 스페이스/엔터/클릭으로 직접 렙을 치면서 훈련하고, 피로가 쌓이면
동작이 느려지다 방전해 다음 종목으로 넘어가는 루프를 만들었다. 종목별 동작은
`AnimatorOverrideController` 슬롯으로 분리해뒀는데, "모캡만 슬롯에 넣으면 코드
수정 없이 적용된다"는 요구를 가정으로 남겨두지 않고 실제로 클립을 갈아 끼워
확인까지 마쳤다.

## 한 일

### 1. 능동 렙 세션 (`TrainingSessionController`)

렙 입력 1회(버튼 클릭 또는 스페이스/엔터)가 `OnRepInput()` 한 진입점으로 모인다.
피로(⚙ `fatiguePerRep=7`/`fatigueLimit=100`)가 쌓이면 `Animator.speed`를 선형
감속하고, 임계(`strainedStartFatigue=55`)를 넘으면 CrossFade 대상을 "Rep"에서
"Strained"로 바꿔치기해 힘겨워하는 모습을 만든다. 한계에 닿으면 "Collapse"로
전환하고 세션을 끝낸다. 상태 전환은 Animator 파라미터/트랜지션 그래프가 아니라
스크립트가 상태 이름으로 직접 `CrossFade`한다 — 이 프로젝트가 시나리오 모션
바인딩에 이미 쓰던 패턴 그대로라 새 그래프를 설계할 필요가 없었다.

### 2. 저해상도 훈련 스테이지 (`TrainingStageView`)

`ArenaStageView`(경기 화면, 파이터 2명)를 캐릭터 1명으로 축소한 버전. RT 320x180,
12fps 수동 스텝, `SetController()`로 종목이 바뀔 때마다 `AnimatorOverrideController`
를 통째로 갈아 끼운다 — `AnimatorOverrideController`가 `RuntimeAnimatorController`
를 상속하기 때문에 `Animator.runtimeAnimatorController` 대입 한 줄로 스왑이 끝난다.

### 3. 클립 스캐폴드 생성기 (`TrainingAssetBuilder`, 에디터 메뉴)

`AC_Training.controller`(Ready/Rep/Strained/Collapse 4상태, placeholder 클립은
기존 `AC_Fighter_A.controller`에서 상태 이름으로 찾아 재사용) + 종목 13개 각각의
`AnimatorOverrideController` + `TrainingCatalog.asset`을 만든다. **이미 있는
에셋은 절대 안 덮어쓴다** — 재실행이 사람이 채워둔 모캡을 지우는 사고를 코드
가드로 막았다. 판단 근거는 `docs/decisions/training-clip-scaffold-safe-regeneration.md`.

### 4. 화면 배선 (`MetaFlowController`)

13종목 토글 + "선택: N/3~4" + 시작 버튼(3~4개 아니면 비활성) 화면과, RT를 보여주는
훈련 실행 화면을 새로 지었다. 방전마다 결과 배너("종목명: N렙")를 띄우고 "다음
종목"/"훈련 종료" 버튼으로 명시적으로 넘긴다(타이머 자동전환 아님 — 이 프로젝트
전체가 확인 버튼 패턴이라 그것과 통일). 세션 결과는 `RecordTrainingResult`라는
빈 훅으로만 흘러간다 — `SaveData` 스키마는 무수정.

### 5. ★ 핵심 검증 — "모캡만 따면 바로 적용" 실측

`pull_up` 종목의 오버라이드 컨트롤러에 `AnimatorOverrideController.ApplyOverrides`
(인스펙터가 슬롯에 클립을 드롭할 때 쓰는 것과 같은 공개 API)로 RepClip을 다른
클립(Taunt)으로 교체 → **C# 코드를 전혀 안 건드리고** Play 모드에서 그 종목을
실행 → 실제로 바뀐 클립이 재생되는 것을 확인 → 검증 후 원상복구. 요구사항이
"될 것 같다"가 아니라 "된다"는 걸 실측으로 증명했다.

## 겪은 문제

새로 겪은 문제는 없다. CrossFade 직후 같은 동기 호출 안에서 `GetCurrentAnimatorClipInfo`
를 읽으면 이전 클립이 나오는(프레임이 안 지나서) 현상을 다시 만났는데, 이건
2026-08-05 시나리오 러너 세션부터 이미 알려진 Unity 동작이라 새 문서를 만들지
않고 검증 스크립트를 두 호출로 나눠 우회했다.

## 숫자

| 항목 | 값 |
|---|---|
| 하니스 테스트 | 257/257 (무변화 — 이번 세션 파일은 애초에 하니스 컴파일 대상 아님) |
| 42노드 지문 | IDENTICAL |
| 신규 종목 | 13 |
| 신규 스크립트 | 4개(`TrainingCatalog`/`TrainingStageView`/`TrainingSessionController`/`TrainingAssetBuilder`) |
| 신규 에셋 | 15개(`AC_Training.controller` 1 + 오버라이드 13 + `TrainingCatalog.asset` 1) |

## 다음

- §H 7(스탯/숙련 진행 연결)이 `RecordTrainingResult` 자리를 채울 차례.
- ⚙ 5개(피로·속도 관련 수치), "돌아가며 훈련"의 세션 경계 해석(종목별 방전마다
  자동 진행 vs 한 종목만) 등 판단 필요 목록은 `md/STATUS.md` Current State 참조.
