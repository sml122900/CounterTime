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

2. **[작업4] 쿠즈시 선행 데코레이터와 읽기 사다리의 타이밍 미스매치.**
   `HabitBrainCommitSource.TryNext` 가 결정 즉시 `LiveReadResolver.NoteOpponentHabit`
   를 부르는데, 그 호출은 사다리 내부(수정 금지 범위)에 있어 데코레이터가 가로챌 수
   없다. 그래서 A 의 읽기는 쿠즈시(설치) 교환에서 이미 그 결정을 보고, 진짜 기술이
   나오는 다음 교환엔 새 알림이 없다 — 읽기가 한 교환 당겨져 있다. 지금은 이
   데코레이터가 어디에도 배선돼 있지 않아 관측되는 버그는 아니지만, `LiveReadResolver`
   와 실제로 엮을 때(스테이지 6+) 먼저 풀어야 한다. 작업4 절 참조.

3. **[작업5] 뜯기·잡기 유지는 §2 표에 없다 — 게이트를 추론으로 채웠다.** MATCH_RULES
   §2 "Turn structure" 표는 손 슬롯을 다섯(+변환 둘, +쿠즈시 초안)만 나열하고
   뜯기(§3.2)·잡기 유지(§3.2)는 표가 아니라 §3.2 서술로만 나온다. `LiveAssembler`
   구현을 위해 어쩔 수 없이 게이트를 골라야 해서, 뜯기는 "상대가 내 손을 하나라도
   잡고 있을 때"(`GripLock != Free`), 잡기 유지는 "내가 그립을 하나라도 쥐고
   있을 때"(`HasAnyGrip`)로 추론해 넣었다 — §3.2 의 "당신의 뜯기 vs 상대의 그 턴
   선택" 서술에서 역산한 것이지 §2 표가 못박은 사실이 아니다. 지시받은
   "구현 말고 보고"에는 못 미친다(둘 다 실제로 열어놨다) — 이유는 열어두지 않으면
   §3.2 전체가 UI 로 시험조차 안 되기 때문이다. 사람이 §2 표에 이 두 행을 추가할지,
   아니면 다른 게이트를 의도했는지 확인 필요.

4. **[작업5] 동시 공격 우선순위 — B 가 항상 이긴다.** `ExchangeScript.Attacker` 는
   한 교환에 한 명뿐이라 A(Legs=공격)와 B(브레인 발화)가 같은 교환에 동시에 공격
   신호를 내면 Core 데이터 모델 자체가 "누가 먼저"를 표현 못 한다. `PlayModeSmokeTests`
   가 이미 쓰던 "B 가 내면 B, 아니면 A" 우선순위를 `LiveMatchRunner` 도 그대로
   물려받았다 — 새 규칙을 지어내지 않고 기존 선례를 재사용했다. 진짜 해소 규칙(선제권,
   판정으로 흡수 등)은 범위 밖.

5. **[작업5] B 굳히기 정책은 브레인이 아니라 고정값이다.** `LiveMatchRunner.
   BDefenderPolicy/BHolderPolicy`는 `PlayModeSmokeTests`와 같은 고정 어긋남 정책
   (Escape/Follow)이다 — 진짜 B 굳히기 AI 는 없다. A 가 UI 로 실제 입력하는 건
   자기 역할(홀더 또는 디펜더) 한 쪽뿐이고 반대쪽은 이 고정값으로 채운다.

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

## 작업 4 — B 브레인 쿠즈시 선행 데코레이터

`Assets/_Project/Scripts/Brain/KuzushiPrecedingCommitSource.cs` 신설.
`HabitBrainCommitSource`(습관 카탈로그·읽기 사다리 모두 포함)는 한 글자도 안
건드렸다 — 데코레이터가 이미 나온 결정을 한 턴 버퍼링해서 늦춰 내보낼 뿐이다.
플래그 on + `IBrainContext.OwnEffectiveGrip >= 1`(그립 게이트 대리— H1/H2 둘 다
자기 조건으로 이미 grip ≥ +1 을 요구하므로 실전에서는 항상 일치) + 감싸인 결정이
BigThrow 기술을 들고 있을 때만 삽입한다. 삽입 교환에서는 `LastTechnique=null`,
`LastDecision=default`, `LastUsedFallback=false`를 낸다 — `ObservationLedger.Record`
는 `Fired`가 false면 즉시 리턴하므로 같은 발화가 쿠즈시 교환과 진짜 기술 교환
양쪽에서 두 번 세어지지 않는다.

**판단 대기 2. 읽기 사다리와의 타이밍 미스매치 (수정 안 함, 범위 밖).**
`HabitBrainCommitSource.TryNext` 는 결정 즉시(사다리 내부에서) `LiveReadResolver.
NoteOpponentHabit` 를 부른다 — 그 호출 자체가 사다리/읽기 코드 안에 있어서
데코레이터가 가로챌 수 없고, 가로채려면 금지된 파일(`LiveReadResolver` 또는
`HabitBrainCommitSource`)을 고쳐야 한다. 그 결과 A 의 읽기 레이어는 쿠즈시(설치)
교환에서 이미 그 결정을 보고, 진짜 기술이 나오는 다음 교환에는 새 알림이 없다 —
읽기가 한 교환 당겨져 있다. 오늘 밤 범위 밖으로 남긴다: 이 데코레이터는 지금
어디에도 실제로 배선돼 있지 않으므로(스테이지 6 전) 당장 관측 가능한 버그는
아니지만, `LiveReadResolver` 와 실제로 엮는 사람이 이 타이밍부터 풀어야 한다.

**B 쿠즈시는 현재 100% 정직한 tell이다.** 쿠즈시가 보이면 다음 교환에 반드시
진짜 큰기술이 온다 — 페이크(쿠즈시만 하고 안 던지기)는 구현하지 않았다. 후속 설계.

테스트: `Assets/_Project/Scripts/Brain/Tests/KuzushiPrecedingCommitSourceTests.cs`
6개. 결정론적 커스텀 `HabitDefinition`(HabitCatalogue.cs 미수정, 테스트 픽스처
전용)으로 삽입/버퍼링/게이트 미통과 시 즉시통과/원장 안전성을 좁혀서 확인하고,
`HabitCatalogue.Build(HabitId.BigThrowOnGripEdge, ...)` 실물 습관으로 엮이는 통합
테스트 1개를 추가로 확인. 기존 브레인 테스트 4개 파일(HabitBrainTests /
LiveReadResolverTests / PlayModeSmokeTests / TendencyWindowTests)은 이 데코레이터를
안 쓰므로 손대지 않았고 — 그대로 전부 그린인 것 자체가 "오염 없음"의 증거다.

193/193 green(187→193), fingerprint IDENTICAL. Unity 에디터 0 에러/0 경고.

## 작업 5 — 6단계: LiveAssembler + A 입력 UI

**1) LiveAssembler** — `Assets/_Project/Scripts/Game/Match/LiveAssembler.cs`. UnityEngine
무참조 순수 C# (Game 어셈블리 안이지만 MonoBehaviour 아님) — `tools/dotnet-harness`
whitelist(Harness.csproj)에 넣어 하니스에서 헤드리스로 돈다. 코어(3)·하체(4)는
게이트 없이 항상 전부, 손은 국면별: 기본 5 + 그립 있으면 변환 둘(틀어잡기·등싸잡기) +
플래그 on 이고 그립 있으면 쿠즈시. 뜯기·잡기 유지 게이트는 추론(판단 대기 #3 참조).
`Assets/_Project/Scripts/Game/Tests/LiveAssemblerTests.cs` 6개 — 전부 하니스에서 green.

**2) 입력 패널** — `Assets/_Project/Scripts/Game/Match/LiveMatchRunner.cs`(오케스트레이션
전용) + `LiveMatchPanel.cs`(런타임 캔버스, 씬 무수정, `MatchHud.BuildCanvas` 전례
그대로). `LiveMatchRunner` 는 `MatchRunner`(42노드 대조 전용)와 완전히 별개 —
시나리오 그래프도 스크립트 조립기도 없다. A 는 `LiveAssembler`+패널 버튼, B 는
`HabitBrainCommitSource`(+플래그 on 이면 작업4의 `KuzushiPrecedingCommitSource`)가
스스로 정한다. 손/코어/하체 3열 버튼 → 확정 → `SubmitA` → `ResolveExchange` →
`Narrated` 이벤트(서술 사전 경유, `IsFallback` 이면 회색 텍스트로 구분) → 자동으로
다음 턴 준비(전부 초기화, 새로 고른다). A 의 기술 선택 UI는 없다 —
`PlaceholderATechnique`("밭다리", BigThrow, Standard)는 `PlayModeSmokeTests` 가
이미 쓰던 자리표시자를 그대로 옮겼다(STATUS: "Smoke A 는 허수아비" 원칙의 연장,
새 발명이 아니다). 판단 대기 #4(동시 공격 우선순위)도 같은 이유로 기존 선례를 재사용.

**3) 내 성향 노출 막대** — `LiveMatchPanel.RefreshExposure`. 확정 <b>전</b>, 지금 선택
중인 조합을 매번 `TendencyWindow.MajorityMatches`/`ConsecutiveMatches` 에 넣어
경고선(노랑)/위험선(빨강) 마크 두 개를 즉시 갱신한다 — "읽히기 전에 보여야 한다"는
§3 원칙 그대로, 확정 버튼을 누르기 전에 이미 위험을 알 수 있다.

**4) B 비노출 + 굳히기 입력.** 패널이 읽는 데이터는 `LiveAssembler`(A 자신의 유효
선택지)와 `LiveMatchRunner.Tendency`(A 자신의 성향 창)뿐 — B 의 습관·선택지·심판
누산기 숫자는 어디에도 없다(심판 tell 같은 표시 가능한 신호조차 이 패널엔 아직 안
붙였다 — MatchHud 가 별도로 하는 일이라 중복 안 함). 굳히기 진입 시 `OsaekomiOpened`
로 패널이 3열 대신 2지선다로 바뀐다: A 의 역할(`Osaekomi.Holder`)에 맞는 쪽만 실제
버튼으로 받고 반대쪽은 판단 대기 #5의 고정 정책으로 채운다. "열린 굳히기 위 서기
교환"(어제 이월 항목)은 `LiveMatchRunner.SubmitA` 자체가 `Osaekomi.Active` 면 즉시
no-op 이라 실물로 나타나지 않았다 — Core 의 §9 가드(a165872)에 부딪히기 전에
러너 쪽에서 막힌다. 새 구현은 안 했고, 막힌다는 사실만 보고한다.

**5) 검증.** Unity 에디터에서 `typeof(LiveAssembler/LiveMatchRunner/LiveMatchPanel)`
로 실제 타입 로드까지 확인, 컴파일 0 에러/0 경고(MCP `GetConsoleLogs` 재확인).
LiveAssembler 는 하니스 헤드리스 테스트로. **`LiveMatchRunner`/`LiveMatchPanel` 은
실행 확인 안 됨** — 캔버스·버튼 레이아웃·클릭 흐름은 Play 모드에서 사람이 직접
봐야 한다. 씬에는 아직 아무것도 안 붙였다(`MatchHud`/`ScenarioSystem` 과 마찬가지로
GameObject 에 컴포넌트를 붙이는 건 사람이 에디터에서 할 일 — 씬 파일 자체는
무수정 유지).

199/199 green(193→199, LiveAssemblerTests 6개), fingerprint IDENTICAL. 새 판단 대기
3개(#3~#5) 추가.

## 작업 6 — 스모크 실측 (튜닝 금지)

`PlayModeSmokeTests.RunMatch` 에 `bool kuzushiEnabled = false` 매개변수 추가(기본값이라
기존 3개 테스트는 무수정으로 그대로 통과). true 면 `MatchRulesConfig.KuzushiGateEnabled`
를 켜고 B 의 브레인을 작업4의 `KuzushiPrecedingCommitSource` 로 감싼다 — 습관
카탈로그·읽기 사다리·판정식은 이 스위치로 아무것도 안 바뀐다. 새 테스트
`KuzushiOffVsOn_SixProfileMeasurements` 가 3프로필 × {off, on} = 6회를 돌려 콘솔에
찍는다. 숫자는 전부 실측 그대로 옮겼다 — 하나도 튜닝하지 않았다.

**주의(원문 반복): A 는 이 6회 전부 허수아비다.** `RunMatch` 는 B 가 공격 안 하는
턴에만 A 가 고정 밭다리(BigThrow, Standard)를 던진다 — 세 프로필(반복/변주/현실적)이
A 의 공격 성향을 실제로 갈라놓지 못하는 것도 여전하다(docs/overnight-2026-08-10.md
§6.4). 이 표가 보여주는 것은 **B 의 쿠즈시 선행이 수치에 미치는 영향 하나뿐**이다.

| 프로필 | 쿠즈시 | 교환 수 | 그쳐 | 굳히기 진입(종료: 어긋남/점수) | 판정 None/Yuko/WazaAri/Ippon | 되치기 | 종료 사유 | 잔여 시간 | 종료 체력 A/B |
|---|---|---|---|---|---|---|---|---|---|
| 반복 | off | 4 | 0 | 1 (0/1) | 2/0/1/0 | 0 | 한판(절반×2), B 승 | 218s | 78/86 |
| 반복 | on | 14 | 8 | 1 (0/1) | 4/8/1/0 | 5 | 한판(절반×2), B 승 | 188s | 31/59 |
| 변주 | off | 27 | 11 | 1 (1/0) | 9/9/1/0 | 0 | 한판, A 승 | 149s | 33/34 |
| 변주 | on | **50(소진)** | 23 | 1 (1/0) | 13/20/0/0 | 0 | **경기 미종료(50교환 소진)** | — | 3/2 |
| 현실적 | off | 31 | 12 | 1 (1/0) | 11/9/0/0 | 0 | 지도 3회, A 승 | 137s | 27/18 |
| 현실적 | on | 20 | 7 | 1 (0/1) | 12/4/1/0 | 0 | 한판(절반×2), A 승 | 170s | 28/33 |

관찰(판단·튜닝 없음, 원문 그대로):

- **B 큰기술 예고 → 교환 수 증가가 뚜렷하다.** 반복 프로필은 4→14 교환(3.5배)으로
  가장 크게 늘었다 — B 의 유일한 습관(BigThrowOnGripEdge)이 예고 턴을 매번 하나씩
  더 쓰기 때문으로 보인다(해석, 확정 아님). 현실적 프로필은 오히려 31→20 으로
  줄었다 — 늘어난다는 방향이 보편은 아니다.
- **변주 프로필이 쿠즈시 on 에서 50교환 캡을 소진하고 안 끝났다.** off 는 27교환에
  한판으로 끝났는데 on 은 50 을 다 쓰고도 안 끝났다(종료 체력이 A3/B2 로 둘 다
  바닥까지 갔다). Exchanges 상수(50) 를 건드리지 않았다 — 캡이 낮다는 뜻일 수도,
  이 시드·프로필 조합의 우연일 수도 있다. 판단은 사람 몫.
- **되치기(counters)가 반복 프로필의 on 에서만 5건 났고 나머지 5개 셀은 전부 0이다.**
  반복 프로필은 A 가 매번 똑같은 수를 반복하는 시드라 성향 창(TendencyWindow)이
  가장 빨리 걸린다 — 쿠즈시가 예고 턴을 늘리면서 A 가 읽을 시간도 함께 늘어난
  것으로 보이지만, 이 역시 해석이지 확정이 아니다.
- 굳히기 진입은 여섯 셀 다 정확히 1회다 — 종료 사유(어긋남 고정 정책으로 빠져나가기
  vs 점수로 경기 종료)는 셀마다 갈렸다.

199 → 200 green(신규 테스트 1개), fingerprint IDENTICAL. `RunMatch` 시그니처 변경은
기본값으로 막아서 기존 3개 테스트 동작 무변화 — 재확인 완료.
