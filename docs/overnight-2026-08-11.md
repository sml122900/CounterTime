# 야간 자율 실행 2차 — 2026-08-11 밤

브랜치 `feat/match-engine`. 사람 없음. 작업마다 커밋 + 전체 테스트 + baseline 대비 지문 diff.

## 판단 대기 (사람 확인 필요) — 맨 위, 갱신 예정

1. **[작업2] m18 서술 키의 TechniqueId가 실제 기술과 다르다.** 밤 지시는
   `(?, 밭다리, true) ← m18` 로 못박았지만, m18 본문은 "같은 밭다리, 세 번째. B가
   축발을 걸어 되받는다"이고 판정식은 `[판정식/B] 잡기-2 +수싸움+3(완전간파) +체력0
   +기술+1 = 2 → B 유효`다 — 즉 B가 되치는 기술은 밭다리가 아니라 안뒤축감아치기(H2,
   OPPONENT_BRAIN §2)다. 지시를 문자 그대로 따라 `Judgement.Yuko, TechniqueId="밭다리",
   IsCounter=true` 로 등록했다 — 고치지 않았다. 아침에 TechniqueId를 "안뒤축감아치기"로
   바꿀지, 아니면 "밭다리"(A가 반복한 기술 기준)로 의도된 것인지 확인 필요.

(이후 작업 진행하며 여기 채움)

---

## 작업 0 — 선행 확인

직전 지시(체력 스냅샷 v3.6 / drain 정책 §9.3 명기 / 스모크 허수아비 STATUS 명시 /
서기 교환 공백 이월 표기)는 이미 전부 커밋돼 있었다. 확인 커밋:

- `478a9f1` fix(core): §9.1 굳히기 게이트 체력 스냅샷을 §4 판정식과 통일 (v3.6)
- `585216b` docs: 스모크 재측정 표, drain policy 판단 대기 항목, off-by-one 재확인
- `0f4507d` chore(tools): dotnet-harness 커밋 + 밤 지문 재검증 + 문서 정합 + 계측
- `a036e75` fix(tests): a165872 가드로 레드였던 9개 전부 그린 복구
- `a165872` feat(core): §9 되치기 해석 명문화 + 굳히기 중 서기 교환 가드

작업 시작 전 baseline 확인: `tools/dotnet-harness` `dotnet run -- --check` →
`TESTS: 177/177 passed, 0 failed, 0 skipped`, fingerprint `CHECK: fingerprint matches
fingerprint-baseline.txt` (IDENTICAL). MATCH_RULES.md 스탬프 `v3.6 | body:9e178aa` —
`tools/stamp.sh --check` 통과. 이 상태를 이번 세션의 시작 baseline으로 삼는다.

작업 1(체력 스냅샷)은 이미 완료돼 있었으므로 이번 세션에서 새로 실행하지 않았다.
작업 2부터 진행.

## 작업 2 — 서술 사전 키 등록

`ResultNarrationTable.CreateDefault()` 팩토리를 새로 추가해 워크시트 배정 9건을
그대로 등록했다(문구 원문 무수정). `ResultNarrationPresenter` 의 필드 초기값을
`new ResultNarrationTable()` → `ResultNarrationTable.CreateDefault()` 로 바꿨다.
m27·m34는 워크시트 결정대로 등록하지 않음(폴백 유지).

등록한 9키:
- (Yuko, 업어치기, true) — m11
- (Yuko, 업어치기, false) — m12
- (None, 밭다리, false) — m14
- (None, 허리후리기, false) — m14
- (WazaAri, 밭다리, false) — m17
- (Yuko, 밭다리, true) — m18 (판정값은 docs/step4-result-node-dump.md 의 [판정식/B] 원문에서
  "B 유효"로 읽어 Yuko로 채움 — 판단 대기 #1 참조)
- (WazaAri, 말아업어치기, false) — m32 (기술 미구현, 키만 등록)
- (Ippon, 말아업어치기, true) — m33
- (Ippon, 밭다리, false) — m35+m36

테스트 2개 추가(`CreateDefault_HasAllNineWorksheetEntries_NoneAreFallback`,
`CreateDefault_M27AndM34_StayFallback`). 179/179 green(177→179), 지문 IDENTICAL
(narration table은 MatchRunner.Mainline 지문 경로에 안 들어가므로 예상대로 무변화).

## 작업 3 — 쿠즈시 Core 규칙 (플래그 기본 off)

MATCH_RULES v3.6 → v3.7 **[초안 — 사람 검토 전]**. §2 손 선택지 표에 쿠즈시 행 추가
(초안 표기 포함), §4.4 신설(강등 규칙 전문, 초안 표기). 스탬프는 pre-commit 훅이
커밋 시점에 자동 재생성(`git config core.hooksPath` = `tools/githooks` 확인됨).

코드:
- `MatchRulesConfig`: `KuzushiGateEnabled`(기본 false) + `KuzushiCost`(기본 1).
- `Enums.HandTarget`: 기존 9개 뒤에 `Kuzushi` 추가(순서 유지, 값 안 바뀜).
- `TechniqueSpec`: `SurpriseExempt`(기본 false) 필드 + 생성자 선택 인자 추가.
- `GripSystem.HasAnyGrip`: `CanUseConversion`과 동일한 술어(`Value(who) > 0`)를
  다른 이름으로 노출 — 호출부의 의도(쿠즈시 게이트 vs 변환 게이트)가 다르다.
- `StaminaSystem.PayKuzushi`: `KuzushiCost`를 직접 쓰는 전용 지불 메서드
  (§5.1 `ActionCost` 표를 안 탄다 — Poke 비용과 우연히 같은 값(1)이어도 출처가 다르다).
- `MatchEvent.KuzushiDeclared(Who)` 신설.
- `MatchEngine`: `_kuzushiActive[2]`(이번 교환에 들어올 때의 상태) /
  `_kuzushiDeclaredThisExchange[2]`(이번 교환에 새로 선언됐는지, 스크래치) 두 배열로
  1교환 지속을 구현. `ResolveGrip`의 `HandTarget.Kuzushi` case가 게이트+비용+이벤트를
  처리하고, `ResolveTechnique`는 판정 직전에 `techClass`를 계산해 `JudgementContext`와
  `terms[3]`(판정식 덤프) 양쪽에 일관되게 먹인다 — `JudgementCalculator`는 한 글자도
  안 건드렸다(grep으로 재확인). `ResolveExchange` 끝에서 그쳐/굳히기 진입/경기 종료를
  감지해 그 경우 무조건 소멸시키고, 아니면 이번 교환의 선언만 다음 교환으로 넘긴다.

테스트: `Assets/_Project/Scripts/Core/Tests/KuzushiTests.cs` 8개 — 플래그 off 시
완전 비활성(성립 자체가 안 됨 + 강등 로직 미작동) / 그립 없이 게이트 불통과 / 성립 시
비용·이벤트 / 미선행 강등(그립+1로 브래킷 경계를 넘겨 None↔Yuko로 가시화, 틀린 답도
명시) / 선행 시 급 유지 / 정확히 1교환 후 소멸 / 변칙기 면제 / 그쳐로 소멸.

187/187 green(179→187), fingerprint IDENTICAL(off가 기본이라 메인라인은 전혀 안 건드림).
Unity 에디터(MCP 연결 확인됨, GetConsoleLogs 로 재확인)에서도 0 에러/0 경고 —
`KuzushiTests.cs.meta`는 `AssetDatabase.Refresh()`로 생성 확인.

작은 실수 하나 발견 즉시 수정: 첫 커밋(2e161b4)에서 문서 헤더 버전 태그를
v3.6 그대로 두고 커밋했다 — `tools/stamp.sh`는 본문 해시만 재계산하고 `vX.Y`
숫자는 사람이 올려야 한다는 걸 놓쳤다. 뒤이은 커밋(b9618b2)으로 v3.7 태그 반영.
