# 2026-08-10 밤 — 무인 실행 기록

브랜치 `feat/match-engine`. 사람 없이 진행. `md/STATUS.md` "Next Up" 순서대로
4b 인프라 → §9 굳히기 → 회귀·실측. 각 작업 끝에 165(→175/178) 개 테스트 +
`MatchRunner.Mainline` 52줄 지문 diff로 검증했다. 커밋 4개:
`182f0e5` `979a4f2` `478d46c` `a8e89af`.

## 판단 대기 (사람이 정할 것 — 위에서부터 우선순위)

1. **§9 카운터 판정의 "attacker" 해석.** MATCH_RULES §9.1/§9.2 의 "attacker's
   effective grip" 을 이 야간 작업은 **득점자**(`JudgementScored.ScoredBy`)로
   읽었다 — 되치기가 나면 원래 공격자가 아니라 방어하다 되받아친 쪽이 굳히기의
   "공격자"가 된다는 뜻이다. 문서는 이걸 명시하지 않는다(§4 전체에서 "attacker"
   는 항상 판정식을 선언한 쪽을 가리키는 것과 반대 방향 해석이다). 결과:
   - 되치기로 난 **절반**은 그래도 무조건 굳히기에 들어간다(§9.1 표가 예외를
     안 둠) — adv 계산에 득점자 해석을 그대로 쓴다.
   - 되치기로 난 **유효**는 이 야간 작업이 **굳히기에 안 들어가게** 비워뒀다
     (grip/stamina 게이트를 아예 안 봄, 항상 그쳐로 떨어짐) — 어느 쪽 잡기를
     봐야 할지 확신이 없어서 조용히 고르지 않았다.
   - 코드 위치: `MatchEngine.TryEnterOsaekomi` 의 XML 주석과
     `if (j.Judgement == Judgement.Yuko && !j.IsCounter)` 줄.
   - 이 케이스는 42노드 시나리오에도, 오늘 밤 스모크 3프로필에도 한 번도
     실측되지 않았다(전부 `counters 0`) — 그래서 지금 당장 경기를 깨뜨리진
     않지만, 브레인이 되치기를 더 자주 만들면 언젠가 부딪힌다.
2. **경기 길이가 §9 이후 더 벌어졌다** (아래 §3 표). 이전 실측(현실적 A,
   28교환/156초 남음)보다 이번 실측(26교환/162초 남음)이 오히려 교환 수는
   줄고 남은 시간은 늘었다 — 그쳐 대신 굳히기로 빠지면서 시계가 도는
   구간(§9 는 실시간)과 안 도는 구간(그쳐는 시계가 선다)의 비율이 달라졌기
   때문으로 보인다. 숫자는 손대지 않았다 — 판단은 6단계 이후 사람 몫
   (`md/STATUS.md` 의 기존 방침 그대로).
3. **굳히기 안에서 도는 순수 서기 교환.** `ResolveExchange` 가 `Osaekomi.Active`
   를 전혀 안 본다 — 굳히기가 열린 채로 다음 노드/턴이 평범한 서기 교환으로
   들어오면(오늘 밤 스모크가 실제로 이렇게 돈다, 아래 §3), 엔진은 예외 없이
   처리하지만 그 교환의 "그쳐 대신 굳히기" 판단은 이미 열려 있는 굳히기를
   못 보고 새로 한 번 더 판단한다. 스크립트 경로도 A-전용 입력 UI(6단계)도
   아직 `ResolveOsaekomiExchange` 를 안 부르므로 오늘 밤은 이 상태로 뒀다 —
   6단계 배선 때 반드시 다시 볼 것.
   - **2026-08-11 갱신**: 커밋 `a165872` 가 이 항목의 "조용히 처리한다" 절반을
     닫았다 — `MatchEngine.ResolveExchange` 맨 앞에 `if (Clock.Phase ==
     MatchPhase.Osaekomi) throw new InvalidOperationException(...)` 가드가
     생겼다(§9 — a standing exchange cannot land while a hold is open). 지금은
     "조용히 잘못 처리" 대신 "예외로 무조건 거부" 다 — 하지만 그 거부가 맞는
     규칙인지는 여전히 미정이라 아래 4번으로 남긴다.
4. **열린 굳히기 위에 서기 교환이 오는 경우가 §9/§4.3 어디에도 없다.** 금지인지,
   가능하되 어떻게 처리인지 미정.
   - 코드 사실만: `MatchEngine.ResolveExchange` (Assets/_Project/Scripts/Core/
     MatchEngine.cs) 맨 앞의 가드가 현재 하는 일은 "`Clock.Phase ==
     MatchPhase.Osaekomi` 이면 무조건 `InvalidOperationException`" 하나뿐이다
     — 어떤 종류의 서기 입력이 왜 왔는지는 안 가리고, 예외적으로 허용해야 할
     경우와 진짜 버그를 구분하지 않는다.
   - MATCH_RULES §9 는 `ResolveExchange`(서기)와 `ResolveOsaekomiExchange`
     (탈출)가 서로 다른 진입점이라는 것만 전제할 뿐, 굳히기가 열린 동안
     `ResolveExchange` 가 불리는 경우 자체를 어디서도 다루지 않는다 — 금지해야
     하는 입력(물리적으로 눌려 있는데 새 서기 기술을 던질 수 없다)인지, 아니면
     정의는 필요하지만 언젠가 유효한 입력(예: 관전 판정 재조정 같은 것)인지
     문서가 침묵한다.
   - 이 침묵이 실제로 부딪힌 사례: `MatteAfterScoreTests.MatchEndingScore_
     DoesNotCallMatte` 가 원래 "절반 두 개를 각각 별도 `ResolveExchange` 로
     합산 한판을 낸다"는 시나리오였는데, 첫 절반이 §9.1 표상 게이트 없이
     무조건 굳히기를 열기 때문에 둘째 절반 호출이 정확히 이 미정 케이스였다.
     그 조합 자체의 "진짜" 경로(`ResolveOsaekomiExchange` 로 §9.3 안에서 두
     번째 절반이 나오는 것)는 `OsaekomiTests.
     WazaAriEntryPlusTenSecondHold_CombinesToAwaseteIppon` 이 이미 올바르게
     덮고 있어서, 이번엔 테스트를 단일 교환 직격 한판으로 좁혀 피해갔다
     (2026-08-11) — 하지만 "열린 굳히기 위 서기 교환"이라는 입력 자체가 rules
     차원에서 뭘 의미해야 하는지는 그걸로 답해지지 않는다.

## 1. 작업 1 — 4b 인프라 (`979a4f2`)

- **1안 시도 → 되돌림.** `JudgementScored` 에 `TechniqueId` 필드를 새 생성자
  오버로드로 추가(기존 생성자 시그니처는 안 건드림) → 테스트는 161/161 통과
  했지만 **지문이 IDENTICAL 이 아니었다** — 이벤트 덤프에 `TechniqueId=...`
  가 새로 찍혀서. 판정 자체(Sum/Judgement/ScoredBy/IsCounter/Terms)는 전혀
  안 바뀌었지만, 지시된 절차가 엄격해서 그대로 되돌렸다.
- **2안 채택.** Core 무수정. `MatchRunner` 가 이미 들고 있던 `ExchangeScript`
  지역 변수에서 `Technique.Id` 를 새 이벤트 `JudgementNarrated` 로 짝지어
  낸다. 지문 **IDENTICAL** 확인.
- `ResultNarrationTable`(빈 데이터, (Judgement,TechniqueId,IsCounter)→문구,
  폴백 `"{TechniqueId} — {판정명}"` + IsFallback) + `ResultNarrationPresenter`
  (play mode 전용 구독자, MonoBehaviour) 신설.
- 격리는 "이벤트를 안 쏜다"가 아니라 "표를 쿼리하는 건 이 컴포넌트뿐이고 대조
  모드는 그걸 안 붙인다"로 짰다 — `ModeIsolation_ScriptedPathTypes_...` 테스트가
  대조 모드 5개 타입의 멤버 시그니처에 표 타입이 안 보이는 것을 리플렉션으로
  고정한다.
- `docs/step4-narration-worksheet.md` — 9개 판정식 노드(분할된 이웃 노드
  m12/m15/m32/m35 포함) 원문 + 빈 배정 칸 + 코드에서 긁은 TechniqueId 전수.
- **미해결(문서에 이미 표시)**: m18 의 IsCounter 값, 대조 모드 노드-id 폴백
  Id 를 실명과 어떻게 공존시킬지 — 둘 다 "확인 요"로 남겨뒀다.

## 2. 작업 2 — §9 굳히기 (`478d46c`)

Core 에 §9.1(진입 게이트) / §9.2(adv 1회 계산) / §9.3(5초 탈출 교환,
판정 먼저·탈출 나중) / §9.4(탈출 시 잡기만 Free, 그쳐 회복 없음) 을 그대로
구현. `OsaekomiTests` 9개 — OPPONENT_BRAIN §8 이름 5개(그대로) + 경계 4개
(맞물림은 탈출 카운트 안 올림 / 굳히기 중 한판은 `OsaekomiEnded` 를 안 냄 /
holder 회복은 10초 단위로만 / **절반으로 진입한 굳히기가 10초를 채우면 진입
절반과 합산돼 한판** — 이건 시나리오 m38 원문 "절반 보유 시 10초에 합산
한판"을 그대로 고정한 것이다, 버그가 아니라 발견해서 지킨 규칙).

기존 테스트 하나(`MatchEndingScore_DoesNotCallMatte`)의 전제가 §9 의 의도된
결과와 충돌해서 고쳤다 — 안 끝난 절반은 이제 그쳐가 아니라 굳히기로 간다.
그 이웃 테스트 이름(`ScoreCallsMatte_UnlessOsaekomiWouldFollow`)이 애초에
이 상황을 이름으로 예고해뒀던 걸 그대로 확인한 셈이다.

계측 중 잡은 함정: `EnterViaYuko` 헬퍼가 B 체력을 "전부 중립" 커밋으로
깎으려 했더니 §6.1 소극 누산기가 14턴 만에 문턱을 넘어 그쳐를 불러서
`GripUp()` 으로 세운 잡기를 매트 리셋으로 지워버렸다. B 커밋을
`CoreStance.Aggressive` 로 바꿔 "전부 중립"이 아니게 만들어서 피했다 — 숫자로
찍어보고서야 잡은 것이라 테스트 주석에 그대로 남겨뒀다.

## 3. 작업 3 — 회귀 + 실측

### 3.1 회귀

- 175/175 하니스 테스트(`CounterTime.Core.Tests` 126 + `CounterTime.Brain.Tests`
  40 + `CounterTime.Game.Tests` 9(`ScenarioTurnChoicesTests` 2 +
  `ScriptedReadResolverTests` 2 + `ResultNarrationTableTests` 5)) 전부 초록.
- **`MatchRunnerHandActionTests` 3개는 미실행** — `new GameObject` /
  `AddComponent<MatchRunner>()` 를 써서 실제 Unity 가 필요하다. 하니스로
  못 돌렸다. 총 178개 중 175개만 이 보고의 근거다.
- `MatchRunner.Mainline` 지문: 4커밋 전부 이전 상태와 **byte-identical**
  (작업 1 1안 시도 때 한 번 안 IDENTICAL 이었던 것 제외 — 그건 되돌렸다).
- 메인라인 m36: `Judgement.Ippon sum=7 Terms=[2,3,0,2,0]` — 그대로.
- Unity 에디터 MCP 가 이번 밤 내내 연결돼 있었다(브리지 살아있음, 사람이
  아무도 없었는데도) — 매 커밋 뒤 `AssetDatabase.Refresh()` 로 실제
  컴파일을 확인했다. 전부 0 에러/0 경고. **Test Runner 실행 자체는 여전히
  MCP 로 안 된다**("User interactions are not supported for MCP tool calls",
  STATUS 에 이미 기록된 제약 — 오늘 밤 다시 확인하진 않았다).

### 3.2 스모크 3프로필 (seed 1234, 지터 0, §9 적용 후)

숫자는 그대로 옮겼다 — 이상해 보여도 안 고쳤다. `PlayModeSmokeTests` 가
`ResolveExchange` 를 직접 부르는 헤드리스 루프라 §9 진입 게이트가 이 루프
안에서도 자동으로 걸린다는 점이 이번에 처음 드러났다 — 그 루프는
`ResolveOsaekomiExchange` 를 안 부르므로, 굳히기가 열려도 다음 교환은
평범한 서기 교환으로 계속 처리된다(위 판단 대기 §3 참조).

| 프로필 | 교환 수 | 그쳐 | 굳히기 진입 | 잔여 시간 | 종료 체력 A/B |
|---|---|---|---|---|---|
| 반복(A 한 수 고정) | 27 | 17 | 1 | 159s | 0 / 39 |
| 변주(4주기) | 29 | 11 | 1 | 153s | 28 / 28 |
| 현실적(6주기, 공격 ~1/3) | 26 | 9 | 2 | 162s | 22 / 24 |

**이전 기록(§9 이전, `md/STATUS.md` "Next Up" 인용)**: 현실적 A —
28교환 / 156초 남음. 오늘 밤 하니스를 막 세운 직후(어떤 코드도 안 고친
시점)에 같은 시드로 다시 돌려도 `turns=28, clock=156s left,
stamina end A23/B25` 로 정확히 일치했다 — 그 값이 실제로 §9 이전 상태였다는
근거다.

§9 적용 후 현실적 A 는 **26교환 / 162초 남음** — 교환은 2번 줄고 잔여 시간은
6초 늘었다. 반복·변주 프로필은 §9 이전 기록이 이번 세션에 남아있지 않아
직접 비교는 못 한다(하니스가 세션마다 새로 만들어지고 커밋되지 않는다는
`CLAUDE.md` 관례 그대로 — 다음에 비교하려면 이 문서의 표를 기준선으로 쓸 것).

숫자에 대한 튜닝은 하지 않았다 — 경기 길이 판단은 6단계 이후로 미룬다는
기존 방침(`md/STATUS.md` "Match length is deliberately unresolved") 그대로.

## 4. 다음 세션 우선순위

1. 위 "판단 대기" 3개 확인 — 특히 되치기 굳히기 해석.
2. `docs/step4-narration-worksheet.md` 문구 배정 → `ResultNarrationTable.Add`
   호출 채우기.
3. 6단계(A 전용 입력 UI) 배선 때 `ResolveOsaekomiExchange` 를
   `MatchRunner`/scripted 경로에 실제로 연결 — 그때 "판단 대기 §3" 이 실제
   버그가 되는지 다시 볼 것.
4. `md/STATUS.md` 는 이 문서와 함께 갱신했다(영문, 아래 커밋).

## 5. 후속 (2026-08-11) — 가드 추가 후 사실관계

커밋 `a165872` 가 `MatchEngine.ResolveExchange` 에 `MatchPhase.Osaekomi` 가드를
달았다(굳히기가 열린 채로 다시 불리면 예외). 그 직후 확인한 사실 두 가지.

**§9.3 탈출 교환의 호출 경로.** `ResolveExchange` 내부 라우팅이 아니라 별도
공개 메서드 `MatchEngine.ResolveOsaekomiExchange(OsaekomiDefenderChoice,
OsaekomiHolderChoice)` 다. `ResolveExchange` 와 `ResolveOsaekomiExchange` 는
서로 다른 진입점이고, 후자는 가드 대상이 아니다.

**어제 그린이던 `OsaekomiTests` 4개가 걸린 정확한 지점.** 4개(
`AdvFrozenAtEntry_DefenderStaminaDropDoesNotRaiseIt`,
`EscapeResetsGripsToFree_ButGrantsNoMatteRecovery`,
`IpponDuringHold_EndsMatch_ButDoesNotEmitOsaekomiEnded`,
`HolderRecovery_OnlyOnCompletedTenSecondMarks`) 전부 자기 테스트 본문의
`ResolveOsaekomiExchange` 호출에서 걸린 게 **아니다** — 공유 헬퍼
`EnterViaYuko()` 안에서 걸린다. 그 헬퍼는 `GripUp()`(잡기 +1 고정) →
`DrainB(14)`(무점수 교환 반복) → 마지막 명시적 `ResolveExchange` 한 번(의도된
진입)의 3단 구조였다. 그런데 `DrainB` 루프 내내 잡기가 이미 +1 로 고정돼
있어서 그 교환의 sum 은 `1 + 체력항`이고, 체력항은 B 의 (지불 전) 체력이
50 밑으로 처음 떨어지는 순간 0→+2 로 붙는다 — 그 시점이 정확히 **14번째**
`DrainB` 호출(지불 전 체력 48)이었다. 즉 그 14번째 호출 자체가 이미 무점수가
아니라 유효를 내고(잡기+1·체력<50 게이트를 만족해) 굳히기까지 스스로
연다 — 헬퍼가 "의도된" 진입으로 삼았던 그다음 줄의 명시적 호출은 사실
**이미 열린 굳히기 위에서 두 번째로 `ResolveExchange` 를 부르는 꼴**이었다.
가드가 없던 어제까지는 이게 조용히 통과했다(§9.1 이 아무 오류 없이
`EnterOsaekomi` 를 두 번째로 다시 실행해 `Adv`/`EscapesNeeded` 를 덮어썼을
뿐 — 두 번의 체력 대역이 우연히 같아서 값이 같게 나왔다). 가드가 이 이중
진입을 처음으로 드러냈다.

Root cause 는 위 "판단 대기 §3"(및 실제 지시문의 항목 0)에서 요구한 그대로
"헬퍼가 서기 교환을 굳히기 오픈 상태로 다시 부르고 있었다"는 사실 확인이고,
수정은 `DrainB(14)`→`DrainB(13)` 한 줄 — 루프 전체를 무점수 대역에 묶어두고
그다음 줄의 명시적 호출 한 번만 득점·진입을 내게 했다(2026-08-11, 아래
커밋에서 반영).

`PlayModeSmokeTests` 4개는 다른 원인이다 — 헬퍼 버그가 아니라 헤드리스
루프 자체가 `ResolveOsaekomiExchange` 를 한 번도 안 불렀다(설계상 미배선,
"판단 대기 §3" 원문 그대로). 고정 정책(어긋남 — 빠져나가기×따라가기)으로
굳히기가 열릴 때마다 즉시 턴다는 한 줄을 루프에 추가해 해결했다.

`MatteAfterScoreTests.MatchEndingScore_DoesNotCallMatte` 는 위 둘과 성격이
다르다 — 헬퍼 버그도 미배선도 아니라, 테스트 자신의 명시된 의도가 가드와
직접 충돌한다. 테스트 주석이 "둘째 절반이... 그 시점엔 이미 열려 있어도
`ResolveExchange` 는 그 상태를 안 본다(§9 배선은 6단계 몫)"이라고 못박고
그 재호출 자체를 검증 대상으로 삼는다. 가드는 정확히 이 재호출을 막으려고
만든 것이므로, 테스트를 그린으로 되돌리는 유일한 길은 검증 대상 자체를
바꾸는 것뿐이다 — 그래서 고치지 않고 사람 판단으로 남겼다.

### 5.1 스모크 3프로필 재실행 (seed 1234, 지터 0, 굳히기 실제 해소 후)

`PlayModeSmokeTests.RunMatch` 에 고정 정책(어긋남 반복) 배선을 추가한
결과다. 숫자만 옮긴다 — 튜닝 안 했다. `MatchRunner`/프로덕션 경로는 안
건드렸으므로 이 표는 스모크 하니스 자체의 측정치이지 실제 게임 수치가
바뀐 게 아니다.

| 프로필 | 교환 수 | 그쳐 | 굳히기 진입 | 잔여 시간 |
|---|---|---|---|---|
| 반복(A 한 수 고정) | 4 | 0 | 1 | 218s |
| 변주(4주기) | 27 | 11 | 1 | 149s |
| 현실적(6주기, 공격 ~1/3) | 31 | 12 | 1 | 137s |

**이전 기록(§3.2, 굳히기를 조용히 건너뛰던 상태)**: 반복 27/17/1/159s,
변주 29/11/1/153s, 현실적 26/9/2/162s.

반복 프로필이 27 → 4 교환으로 크게 줄었다 — 이전엔 굳히기가 열려도
`ResolveOsaekomiExchange` 를 아무도 안 불러 그 열림이 사실상 무시됐고
매치가 그냥 계속 갔다. 지금은 실제로 5/10/20초 마크의 §9.3 판정을 받는다:
이 프로필은 절반으로 진입해서 10초째(2틱) 합산 한판으로 바로 끝난다 —
전에는 통과됐던 득점 기회가 처음으로 실제로 잡힌 것이다. 변주·현실적은
거의 그대로(굳히기 진입이 매치 끝물에 한 번뿐이라 조기 종료가 안 걸림).
숫자를 손대진 않았다 — 경기 길이 판단은 여전히 6단계 이후 사람 몫.
