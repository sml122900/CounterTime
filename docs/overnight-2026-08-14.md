# 야간 자율 실행 — 2026-08-14 : 잡정리 + 소극 3인 차별화 계획

작업 지시: 사람 판단이 필요 없는 위생 정리(작업A) + 소극 3인(문준배·서도현·
오재훈) 차별화의 코드 접점 조사(작업B, 조사만·구현 0). 절대선 둘 — 42노드
지문 IDENTICAL 하드 게이트, 설계·게임 감각 결정 금지.

## 결론 요약

- **작업A(위생 정리) — 2개 항목, 둘 다 처리.** `Assets/_Recovery/`(Unity 크래시
  복구 스냅샷)를 `.gitignore`에 등록(파일 자체는 안 지움). Active Input
  Handling(`activeInputHandler: 2` = Both)은 확인 결과 2026-08-11 커밋
  (`54bd5be`)에 이미 반영돼 있어 추가 조치 불필요. 그 외 "불명확한" 순수
  위생 후보는 못 찾았다(아래 "찾아본 것" 참조) — 억지로 만들지 않았다.
- **작업B(소극 3인 차별화) — 조사·계획서만, 습관 코드 변경 0.** 저작 절차·
  재사용 가능한 조건 축·지문 위험(구조적으로 0, grep 으로 확인)·상대별
  선택지 2~3개씩을 `docs/plan-passive-differentiation.md`에 정리했다. 방향
  선택은 전부 사용자 몫으로 남김.

## 작업 A — 위생 정리

### 1. `Assets/_Recovery/` → `.gitignore` 등록 (커밋 `6a68a14`)
2026-08-12 세션부터 untracked 상태로 존재가 확인됐던 폴더 — 파일명(`0.unity`,
`0 (1).unity` 등)과 타임스탬프 패턴이 Unity 에디터 비정상 종료 시 자동
저장하는 씬 복구 스냅샷과 일치한다(재확인: `ls -la`로 4개 씬 + 4개 .meta,
2026-08-12 13:17~15:26 사이 생성). 로컬 캐시라 커밋 대상이 아니다.
`.gitignore`에 `/Assets/_Recovery/` + `/Assets/_Recovery.meta`(폴더 자체의
메타 파일 — 기존 `!/[Aa]ssets/**/*.meta` 부정 규칙이 형제 파일까지 자동으로
안 잡아줘서 `AI Toolkit.meta`와 같은 패턴으로 별도 등록 필요했음, `git status`
로 실제 사라짐 확인) 추가. **파일 자체는 지우지 않았다** — 지시 범위가
gitignore 등록까지였고, 크래시 복구 스냅샷 삭제는 이 세션이 판단할 일이
아니라고 봄.

### 2. Active Input Handling = Both — 이미 반영됨, 조치 불필요
`ProjectSettings/ProjectSettings.asset`의 `activeInputHandler: 2`(2 = Both)를
확인 — `git log`로 2026-08-11 커밋(`54bd5be`)부터 이미 이 값이었고, `git diff`
로 현재 작업 트리와도 차이 없음을 확인. 할 일이 없어서 손 안 댐.

### 그 외 — 찾아본 것, 못 찾은 것
`git status --porcelain --ignored=matching`으로 무시된 파일 목록 전체를 훑어
예상 밖의 것이 있는지 확인(전부 정상 — IDE/.NET 빌드 산출물뿐). `git ls-files`
에서 스크래치/임시 파일 이름 패턴(todo/scratch/temp/.bak 등)도 훑었으나
오탐(`SceneTemplateSettings.json`, "(1)" 패턴에 우연히 걸림) 하나뿐이고
실제 문제는 없었다. STATUS.md의 다른 미결 항목들은 전부 재질/카피 문구
교체·⚙ 튜닝·UI 판단처럼 사람 판단이 걸려 있어 이번 지시("불명확하면 건드리지
말고 목록으로")대로 손 안 댔다 — 새로 만들 목록도 없다, 위 두 개가 이번에
찾은 전부다.

## 작업 B — 소극 3인 차별화 조사

전문: `docs/plan-passive-differentiation.md` (커밋 `d827f9a`). 여기선 조사
결과의 뼈대만.

**저작 절차 확인**: 6상대(황도철~오재훈) 전부 동일한 3단계 패턴 —
`HabitId` enum 추가 → `HabitCatalogue.Build` switch case 추가 → 상대별
`Habits[]` 배열에 등록. 문준배/서도현/오재훈 세 파일을 직접 Read해서 확인,
추측 없음.

**재사용 가능한 조건**: `IHabitContext`의 기존 5개 멤버(`OwnEffectiveGrip`/
`TurnsSinceOwnShido`/`OwnStamina`·`OpponentStamina`/`OpponentSetupActive`/
`OwnBalance`)로 커버되는 습관은 새 인프라 없이 바로 추가 가능. `OpponentBalance`
같은 건 `IBrainContext`(습관이 절대 못 보는 면)에만 있어서, 그걸 조건으로
쓰려면 `IHabitContext`에 새 멤버를 여는 결정이 먼저 필요하다 — 이미
서도현/임준철 저작 때 밟힌 전례가 있는 절차임을 확인.

**지문 위험 — 구조적으로 0**: `Game/Match/MatchRunner.cs`(42노드 스크립트
경로가 실제로 도는 파일)를 grep — `MoonJunbae`/`SeoDohyun`/`OhJaehoon`/
`HabitCatalogue`/`OpponentDefinition`, 참조 0개. 스크립트 경로는 Brain
패키지의 상대 팩토리를 아예 모른다. 즉 §H4/§H5/§H6 계획서들과 달리 이
클러스터는 게이트(`PromotionGate`류) 없이도 지문이 안전하다는 것을 코드로
확인했다 — 추정이 아니라 grep 결과.

**상대별 선택지(각 2~3개, 코드 관점만)**:
- **문준배**: (1) `SeekGrip` 패턴 재사용한 비공격 그립 습관 — 새 멤버 불필요
  (2) 기존 예비 `HabitId.TurtleWhenTired` 재활용 — 코드 0줄이지만 원래
  진도윤 서사에 저작된 것이라는 단서 (3) 서도현의 `CounterReadyOnOpponentSetup`
  공유 — 무료지만 차별화 목표와 충돌 가능(추천 안 함, 이유까지 문서에 명시).
- **서도현**: 이미 습관 2개(70%/75%, 무쿨다운)인데도 v3에서 가장 안 갈림 —
  조건이 좁아서 실전에 잘 안 서는 것으로 추정. (1) `OwnEffectiveGrip>=1`
  패턴 반격 (2) `TurnsSinceOwnShido<2` 패턴 만회 (3) 임준철/오재훈식 쿨다운
  교대 2지선다 — 셋 다 새 멤버 불필요, 기존 조건보다 자주 서는 쪽 위주로 골랐다.
- **오재훈**: ROSTER 원형이 "특출난 축 없이 고루"라 차별화와 내재된 긴장이
  있음을 사실로만 짚음. (1) 그립 열세 축 추가(현재 습관 둘 다 그립 우위
  조건이라 열세일 땐 여전히 폴백) (2) 임준철의 `FeintAfterShido` 공유 —
  무료, 공유 트레이드오프는 문준배(2)(3)과 같은 종류 (3) 상대 밸런스 기반
  습관 — **새 `IHabitContext.OpponentBalance` 멤버가 필요한 유일한 선택지**,
  절차는 전례 있음.

## ★ 사용자가 정해야 할 것 (통합)

`docs/plan-passive-differentiation.md` 끝의 5개 항목 그대로, 우선순위만
다시 정리:

1. **세 상대 각각 어느 선택지를 쓸지** — 이 조사의 유일한 실질 목적.
2. **기존 HabitId를 상대 간 공유하는 게 허용 범위인가** — 여러 선택지가
   "무료지만 공유"인 트레이드오프를 진다.
3. **오재훈: "고루 하는" 원형과 "더 갈리게 하라" 목표가 어디까지 공존
   가능한가** — 다른 둘과 달리 원형 자체가 무색채를 요구.
4. **⚙(발화확률·쿨다운) 수치** — 이번엔 제안조차 안 함(기존 6상대 값
   0.35~0.90/쿨다운 2~5를 참고 범위로만 언급).
5. **새 `IHabitContext` 멤버(예: `OpponentBalance`)를 열 경우 공유 범위**.

## 검증 (체크포인트별)

| 시점 | 하니스 | 지문 | 스탬프 | 비고 |
|---|---|---|---|---|
| 세션 시작 | 269/269 | IDENTICAL | exit 0 (6종) | Unity MCP 0 에러 확인 |
| 작업A 완료 후 | 269/269 | IDENTICAL | — | gitignore 변경뿐, 코드 무관 |
| 작업B 완료 후 | 269/269 | IDENTICAL | exit 0 | 문서만 추가, 코드 무변경 |

`git status --short`로 이번 세션이 건드린 파일이 `.gitignore` + `docs/plan-
passive-differentiation.md` + 이 보고서 + STATUS.md 뿐임을 확인. `Assets/
_Recovery/`는 이제 ignore 규칙에 걸려 `git status`에서 자체가 사라짐(정상,
의도한 결과).

## 다음 세션을 위한 메모

- 작업B의 다섯 선택지 세트 중 방향이 하나라도 정해지면 바로 착수 가능 —
  지문 위험이 구조적으로 0이라 §H4~7 계획서들보다 훨씬 가벼운 착수가 될 것.
- 방향이 정해지면 `tools/dotnet-harness/Overnight/`의 v3 파이프라인을 그대로
  재사용해 재측정 가능(6상대×500경기, 기존 데이터와 직접 대조 가능한
  형태로 이미 세팅돼 있음).
