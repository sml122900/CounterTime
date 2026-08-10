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
   가능하되 어떻게 처리인지 미정. **→ 6단계로 이월(2026-08-11 확인) — 6단계
   배선 시 실물 확인 후 결정.** 코드·문서 규칙은 지금 안 바꾼다.
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
5. **`PlayModeSmokeTests`/계측 스크립트의 §9.3 탈출 정책은 지시 없이 내가
   정한 값이다.** 코드는 그대로 뒀다 — 여기 사실만 남긴다.
   - **정확한 정책**: 굳히기가 열리면 방어자(빠져나가는 쪽)는 항상
     `OsaekomiDefenderChoice.Escape`(빠져나가기), 홀더(누르는 쪽)는 항상
     `OsaekomiHolderChoice.Follow`(따라가기)를 낸다. §9.3 표의 맞물림 목록은
     (뒤집기,따라가기) 와 (빠져나가기,눌러붙기) 둘뿐이라 이 조합은 그 어느
     쪽도 아니다 — 항상 **어긋남**, 즉 매 5초 틱마다 무조건 탈출 카운트가
     오른다.
   - **왜 이 값을 골랐나**: 헤드리스 스모크가 실제 입력 없이 무한정 눌려
     있지 않고 매치가 자연스럽게 진행되도록 하려던 것이다. 반대(맞물림
     고정)를 골랐으면 모든 굳히기가 예외 없이 20초(§9.3 절대 한판 마크)
     까지 버티다 한판으로 강제 종료돼, 습관 발화·사다리 행 관측이 목적인
     스모크(`PlayModeSmokeTests` 자체 주석 참조)가 매번 그 지점에서 잘릴
     뻔했다. 근거는 이 추론뿐이고, 지시받아 고른 값이 아니다.
   - **§9.3 의 어느 선택지인가, 문서 근거가 있는가**: 어긋남의 두 조합
     (뒤집기×눌러붙기, 빠져나가기×따라가기) 중 하나(후자)다. MATCH_RULES
     §9.3 은 "이 짝은 맞물림, 저 짝은 어긋남"이라는 판정 규칙만 정의할 뿐,
     **입력이 없는 자동 진행 상황에서 어느 쪽을 기본값으로 골라야 하는지는
     전혀 다루지 않는다.** 즉 이 선택 자체에는 문서적 근거가 없다 —
     확인 사실이지 내 해석이 아니다.
   - **굳히기 지속시간에 미치는 영향**: `escapesNeeded` 는
     `clamp(1+floor(adv/2), 1, 3)` 로 1~3 범위다. 어긋남 고정이면 매 틱마다
     탈출 카운트가 오르므로, 다른 사유(점수로 매치 종료)가 먼저 안 끼면
     홀드는 정확히 `escapesNeeded × 5초` 만에 탈출로 닫힌다 — **최소 5초
     (escapesNeeded=1), 최대 15초(escapesNeeded=3)**. §9.3 의 절대 한판
     마크(20초)에는 이 정책으로는 도달할 수 없다 — 15초 상한이 그보다
     먼저 걸리거나, 10초 절반(합산 한판 포함)이 먼저 매치를 끝낸다. 아래
     §7.1 실측 3건 중 2건이 실제로는 탈출이 아니라 점수(한판)로 먼저
     끝났고, 1건만 진짜 탈출로 닫혔다 — 셋 다 15초를 넘지 않았다.

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

## 6. 사후 검증 (2026-08-11, a165872/a036e75 커밋 후)

### 6.0 지문 베이스라인 신뢰성 — 사실관계

**52줄 지문 비교의 출처는 리포에 커밋된 파일이 아니다.** `git log --all
--diff-filter=A --name-only` 로 전체 히스토리를 뒤져도 fingerprint/harness
관련 파일이 커밋된 적이 한 번도 없다 — "byte-identical" 언급은 전부 커밋
메시지·문서의 **서술**이지, 대조할 저장된 값이 아니다. `md/STATUS.md`
84행("하니스는 리포에 안 들어간다 — 세션마다 다시 만든다")이 근거다.

밤 세션(182f0e5/979a4f2/478d46c/a8e89af)의 IDENTICAL 주장도, 오늘 세션
전반부(a165872/a036e75)의 IDENTICAL 주장도 둘 다 **세션 내부 자기비교**였다
— 세션이 그 자리에서 하니스를 새로 만들고, 그 세션 안에서만 전/후를
비교했다. 저장된 정답과 비교한 적은 없다. **그래서 "밤 IDENTICAL"은
재검증이 필요했다.**

**재검증 결과(2026-08-11 수행)**: `git worktree add --detach` 로 5개 지점
(182f0e5^, 182f0e5, 979a4f2, 478d46c, a8e89af) 을 격리된 워크트리에 체크아웃
하고, 그때마다 오늘 새로 만든 독립적인 하니스로 지문을 재생성해 인접한
지점끼리 diff 했다. **5개 전부 byte-identical** — `182f0e5^ == 182f0e5 ==
979a4f2 == 478d46c == a8e89af`. 밤 세션의 주장은 사후 재현으로 확인됐다
(그때와 다른 하니스 코드로, 진짜 그 커밋의 파일로 — 자기비교가 아니다).
`a8e89af` 지점 결과는 오늘 세션 전반부가 자체 생성했던 베이스라인
(`fingerprint_before.txt`, git stash 로 재구성)과도 별도로 대조해 일치를
확인했다.

### 6.1 하니스를 리포에 커밋

`tools/dotnet-harness/` (Harness.csproj, Program.cs, UnityEngineShim.cs,
README.md, fingerprint-baseline.txt). "하니스는 리포에 안 들어간다"는 이전
세션들의 관행이었고 `md/STATUS.md` 에 명시돼 있었다(위 6.0 인용) — 지시받은
적은 없이 그 관행을 그대로 따른 것이었다. 이번엔 명시 지시로 뒤집혔다.
`*.csproj` 는 리포 전체에 gitignore 돼 있어서(Unity 재생성 IDE 파일 규칙)
`.gitignore` 에 `!/tools/dotnet-harness/*.csproj` 부정 규칙을 추가해 이
파일만 되살렸다. `dotnet run -- --check` 로 다음 세션이 재구축 없이
`fingerprint-baseline.txt` 와 자동 비교할 수 있다.

### 6.2 스탬프·문서 정합

- `md/MATCH_RULES.md`: `tools/stamp.sh --check` 통과(exit 0), 1행
  `<!-- v3.5 | body:d97f45d -->` 가 본문과 일치. 불일치 없어서 재생성 불필요.
- `md/OPPONENT_BRAIN.md`(v1.0 body:5d5a8c7), `md/CORE_DESIGN_MatchEngine.md`
  (v3.2 body:a24d23c) 도 같이 확인 — 둘 다 현재.
- **`HANDOFF_session_2026-08-10.md` 는 이 리포에 존재하지 않는다** —
  `git log --all` 로도 그런 파일이 만들어진 적이 없다. 존재하는 HANDOFF 문서는
  `md/HANDOFF_claude_code_runner.md` 와 `md/HANDOFF_claudecode_opponent-brain.md`
  둘뿐이고, 둘 다 "현재 값" 스탬프 블록이 없다(작업 지시 문서지, 상태를 들고
  있는 살아있는 참조 문서가 아니다). 추측으로 다른 파일을 대신 고치지 않았다
  — 파일명 확인이 필요하다.
- `md/STATUS.md` 는 a165872/a036e75 이후 상태(테스트 수 128/40/9+3=180,
  MATCH_RULES v3.5, 하니스 커밋됨)로 갱신했다.

### 6.3 굳히기 진입 저조 원인 — 현실적 A, seed 1234, 1회 실행

숫자만이다, 튜닝 없음. 스모크가 실제로 도는 조건(§9 실제 해소 포함) 그대로
재현했다 — RunMatch 와 같은 습관 관측/피해 기록 순서를 그대로 옮겨서 B 의
습관 발화가 진짜 스모크와 갈라지지 않게 했다.

```
judgements: None=11 Yuko=9 WazaAri=0 Ippon=0
WazaAri: 0 scored, 0 entered osaekomi (rule: always — matches)
Yuko: 9 scored (0 countered, 9 uncountered), 1 entered osaekomi
Yuko entry failures (8): grip<+1 only=5  stamina>=50 only=0  both=3
```

읽는 법: 이 실행에선 절반·한판이 한 번도 안 났다(전부 유효 아니면 노포인트)
— 그래서 굳히기 진입 저조의 원인이 "절반이 드물다"가 아니라 "유효 게이트를
못 넘는다"에 있다. 유효 9건 중 8건이 굳히기에 안 들어갔고, 그 8건 전부
잡기<+1 이 걸려 있다(단독 5 + 체력 동반 실패 3) — 체력≥50 단독 실패는
0건. 즉 이 프로필에서 유효 굳히기 진입을 막는 병목은 거의 전부 **잡기**
쪽이지 체력 쪽이 아니다.

### 6.4 스모크 A 프로필 3종 — 실제 행동 정책 (코드 그대로, 손 안 댐)

출처: `Assets/_Project/Scripts/Brain/Tests/PlayModeSmokeTests.cs`.

**손·코어·하체를 정하는 규칙**: 세 프로필 전부 고정 배열을 턴 번호로
순환 인덱싱한다(`array[turn % array.Length]`) — 매치 상태를 보고 고르지
않는다.

```csharp
static readonly TurnChoice Same =
    new TurnChoice(HandTarget.None, CoreStance.Aggressive, LegAction.Attack);

static readonly TurnChoice[] Varied =
{
    new TurnChoice(HandTarget.None, CoreStance.Aggressive, LegAction.Attack),
    new TurnChoice(HandTarget.RightSleeve, CoreStance.Neutral, LegAction.Neutral),
    new TurnChoice(HandTarget.None, CoreStance.Defensive, LegAction.Defend),
    new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.CounterReady),
};

static readonly TurnChoice[] Realistic =
{
    new TurnChoice(HandTarget.None, CoreStance.Aggressive, LegAction.Attack),
    new TurnChoice(HandTarget.RightSleeve, CoreStance.Neutral, LegAction.Neutral),
    new TurnChoice(HandTarget.None, CoreStance.Defensive, LegAction.Defend),
    new TurnChoice(HandTarget.None, CoreStance.Aggressive, LegAction.Attack),
    new TurnChoice(HandTarget.LeftSleeve, CoreStance.Neutral, LegAction.Neutral),
    new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.CounterReady),
};
```

호출부: `RunMatch(_ => Same, seed: 1234)` / `RunMatch(t => Varied[t %
Varied.Length], seed: 1234)` / `RunMatch(t => Realistic[t %
Realistic.Length], seed: 1234)`.

**난수를 쓰는 지점**: A 의 커밋 선택 자체엔 RNG 가 없다 — `aCommit(turn)`
은 `turn` 만의 순수 함수다. RNG(`SystemRandomSource(seed)`) 는 (1)
`MatchEngine` 내부(§6.2 지도 임계 지터 — 이 스모크는 `ThresholdJitter = 0`
이라 사실상 꺼져 있다)와 (2) B 의 `HabitBrainCommitSource`(습관들 사이
동률 처리 등, OPPONENT_BRAIN §4 폴백 가중)에만 쓰인다. **A 의 공격
빈도(현실적=6주기 중 2회≈1/3)는 튜닝된 확률이 아니라 배열에 박힌 고정
패턴이다.**

**"큰기술 시도 빈도"를 실제로 결정하는 것 — 이게 A 의 Core/Legs 선택이
아니다.** `RunMatch` 의 공격자·기술 배정 로직을 그대로 옮긴다:

```csharp
bool bAttacks = brain.LastTechnique != null;
var attacker = bAttacks ? FighterId.B : FighterId.A;
var technique = bAttacks
    ? brain.LastTechnique
    : new TechniqueSpec("밭다리", ActionCost.BigThrow, TechniqueClass.Standard);
```

B 의 습관 브레인이 그 턴에 기술을 하나라도 냈으면(`LastTechnique != null`)
B 가 공격자다. **아니면 A 가 무조건 공격자가 되고, 기술은 항상 고정된
"밭다리"(BigThrow, Standard)다** — `a`(A 의 그 턴 TurnChoice)의 Core/Legs
값은 이 배정에 전혀 관여하지 않는다. 즉 A 가 그 턴에 `LegAction.Defend`나
`CounterReady`를 선언했어도, B 가 기술을 안 냈으면 A 는 여전히 밭다리를
던진다 — "현실적 A 는 6턴 중 2회만 공격"이라는 프로필 설명(주석)은 A 의
*선언*(Core/Legs) 패턴을 가리키는 것이지, A 가 실제로 공격자로 배정되는
빈도를 직접 제어하는 게 아니다. 후자는 순전히 B 의 습관 발화 여부로
갈린다. 코드를 그대로 옮겨 적은 사실이고, 고치지 않았다.

## 7. 사후 검증 후속 (2026-08-11, 같은 세션 이어서)

### 7.1 스모크 재측정 — 표 (seed 1234, 지터 0, 튜닝 없음)

| 프로필 | 교환 수 | 그쳐 | 굳히기 진입 | 굳히기 종료 사유 | 잔여 시간 | 종료 체력 A/B |
|---|---|---|---|---|---|---|
| 반복(A 한 수 고정) | 4 | 0 | 1 | 점수로 종료(한판) ×1 | 218s | 78/86 |
| 변주(4주기) | 27 | 11 | 1 | 점수로 종료(한판) ×1 | 149s | 33/34 |
| 현실적(6주기, 공격 ~1/3) | 31 | 12 | 1 | 탈출 ×1 | 137s | 27/18 |

**어젯밤 값(§3.2, 굳히기 미해결 상태의 무효값 — `ResolveOsaekomiExchange`
를 아무도 안 불러 열린 굳히기를 다음 교환이 그냥 무시하고 지나가던 시절의
수치, 참고용으로만 남긴다)**:

| 프로필 | 교환 수 | 그쳐 | 굳히기 진입 |
|---|---|---|---|
| 반복 | 27 | 17 | 1 |
| 변주 | 29 | 11 | 1 |
| 현실적 | 26 | 9 | 2 |

굳히기 종료 사유·잔여 시간·종료 체력은 어젯밤 값엔 없다 — 그 실행은
`OsaekomiEntered` 이후에도 `ResolveOsaekomiExchange` 를 한 번도 안 불렀으므로
"종료"라는 개념 자체가 그 실행 안에 없었다(홀드가 열린 채로 표에 안 잡히고
다음 교환이 평범한 서기 교환으로 계속 처리됐다). 그래서 저 세 프로필의
어젯밤 교환 수·그쳐 수는 지금 값과 직접 비교할 수 없는, 다른 규칙으로
돈 실행의 결과다.

### 7.2 off-by-one 재확인 — §9.1 유효 게이트와의 맞물림 (사실만)

`OsaekomiTests.EnterViaYuko()` 안에서 판정식(§4) 의 체력 보너스 항과
§9.1 굳히기 진입 게이트의 체력 조건은 **같은 교환 안에서 서로 다른 시점의
체력을 읽는다** — 코드 사실이고, 이번 수정이 만든 게 아니라 원래부터
그랬다:

- §4 판정식의 체력 보너스(`JudgementContext.OpponentStamina`)는
  `ResolveTechnique` 안에서 `Stamina.PayAttack`/`PayDefense` **호출 전**
  값을 쓴다(지불 전).
- §9.1 굳히기 진입 게이트(`TryEnterOsaekomi` 의 `Stamina.Get(Opp(j.ScoredBy))`)
  는 같은 `ResolveExchange` 호출 안에서 그 지불이 **끝난 뒤** 값을 쓴다
  (지불 후) — `ResolveTechnique` 가 먼저 끝나고 그 다음 `TryEnterOsaekomi`
  가 도니까.

`DrainB(13)` 으로 고친 뒤 실측(하니스): 13회 드레인 후 B 체력 48(지불 전) →
그 값으로 판정식 체력항 +2(48 은 25~49) → sum=1+0+2+1+0=4 → 유효,
득점자 A. 그 판정 안에서 B 가 4 더 깎여 44(지불 후) → 굳히기 게이트가
읽는 값은 이 44 — `44 < 50` 통과, adv 의 체력 보너스도 이 44 로 +1 →
adv=0+1+1=2 → escapesNeeded=2. **원래 헬퍼 주석의 "44 는 25~49"는 지불
후 값(게이트가 보는 값)을 가리켰던 것이고, 내가 고친 뒤 주석에 적은
"48 은 25~49"는 지불 전 값(판정식이 보는 값)을 가리킨다 — 둘 다 맞는
말이지만 서로 다른 숫자를 가리킨다.** 두 값이 우연히 같은 대역(25~49)
안에 있어서 결과(sum=4, escapesNeeded=2)는 어느 쪽으로 설명해도 같게
나오지만, 정확히는 이렇게 두 갈래다.

**헬퍼가 틀렸던 것인지, 문서가 애매한 것인지**: 둘 다 사실이다, 서로 다른
층위에서.
- **헬퍼는 틀렸었다** — `DrainB(14)` 는 드레인 루프의 마지막(14번째) 호출
  자체가 이미 지불 전 체력 48(<50 문턱을 그 호출에서 처음 넘음)로 sum=3
  (유효)을 내고, 지불 후 44 로 게이트까지 통과해 굳히기를 열어버렸다 —
  헬퍼가 "무점수만 반복한다"고 가정한 구간에서 실제로는 마지막 반복이
  이미 득점·진입까지 끝내고 있었다(이중 진입, 지난 보고 §6/커밋
  a036e75 참조). `DrainB(13)` 으로 고쳐서 드레인 루프 전체를 진짜
  무점수로 묶고, 득점·진입은 헬퍼 마지막 줄 단 한 번으로 고정했다 —
  실측으로 확인됨(고친 방향이 맞다).
- **문서는 애매하다** — MATCH_RULES §9.1 은 "상대 체력 <50"이라고만 적고,
  그 체력을 **판정식이 보는 지불 전 값**으로 읽으라는 건지 **굳히기
  게이트 자체가 보는 지불 후 값**으로 읽으라는 건지 구분하지 않는다.
  코드는 이미 후자(지불 후)로 구현돼 있고 이번 수정이 그 선택을 바꾸지
  않았다 — 기존 동작 그대로다. 다만 문서 문장만 보면 어느 쪽을 뜻하는지
  실제로 확정할 수 없다는 것은 사실이다. 애매해서, 고치지 않고 사실만
  보고한다.

### 7.3 "밤 작업 사후 검증" 중 아직 안 한 항목

지난 요청(지문 베이스라인 출처 / 하니스·기대값 커밋 / 스탬프·핸드오프
갱신 / 굳히기 진입 저조 원인 계측) 을 항목별로 다시 짚는다:

1. 지문 베이스라인 신뢰성 — **완료** (§6.0, worktree 재검증까지 끝).
2. 하니스 + 기대값 파일 커밋 — **완료** (`tools/dotnet-harness/`, 커밋
   `0f4507d`).
3. 스탬프·문서 정합 — **부분 완료**:
   - `MATCH_RULES.md` 스탬프 확인 — 완료(v3.5, 재생성 불필요).
   - `md/STATUS.md` 갱신 — 완료.
   - **`HANDOFF_session_2026-08-10.md` 의 "현재 값" 블록 갱신 — 미완료,
     막혀 있다.** 그 파일명이 이 리포에 존재한 적이 없다(§6.2 에서 이미
     보고). 존재하는 두 HANDOFF 문서(`HANDOFF_claude_code_runner.md`,
     `HANDOFF_claudecode_opponent-brain.md`) 는 애초에 "현재 값" 스탬프
     블록이 없는 작업 지시 문서라 갱신할 자리 자체가 없다. 어느 파일을
     염두에 두셨는지 확인이 필요하다 — 확인 전에는 진행 못 한다(추측으로
     아무 파일이나 고치지 않는다는 원칙 유지).
4. 굳히기 진입 저조 원인 계측 — **완료** (§6.3).
   (곁들여 요청됐던 스모크 A 프로필 정책 원문도 완료, §6.4.)

**정리**: 유일한 미완료 항목은 3번의 HANDOFF 갱신이고, 원인은 판단 유보가
아니라 대상 파일이 존재하지 않는다는 사실 확인이다 — 다음 지시에 파일을
특정해 주시면 그때 진행한다.

## 8. §9.1 체력 스냅샷 통일 (2026-08-11, MATCH_RULES v3.5 → v3.6)

§9.1 굳히기 게이트의 "체력 <50"을 §4 판정식과 같은 스냅샷(지불 전)으로
통일했다 — §7.2 에서 사실만 보고했던 지불 전/후 불일치를 실제로 고친
것이다. `MatchEngine` 에 `_scorerOpponentStaminaPrePayment` 필드를 추가해
`ResolveTechnique` 가 §4 판정식용 `defenderStamina`/`attackerStamina` 를
캡처하는 바로 그 자리에서(지불 전) 같이 채우고, `TryEnterOsaekomi` 는 더
이상 `Stamina.Get(Opp(j.ScoredBy))` 를 새로 읽지 않고 이 필드를 그대로
쓴다. `MatchEvent` 모양은 안 건드렸다 — step4 에서 이미 배운 교훈
(`JudgementScored` 에 필드를 더했다가 지문이 깨져서 되돌린 사례) 그대로
지킨 것이다.

**의도된 동작 변경이라 지문이 깨질 수 있다고 미리 경고됐다 — 실제 결과는
IDENTICAL 이다, 자동 통과 아니라 직접 확인한 결과다.**

- `MatchRunner.Mainline`(42노드 시나리오의 17노드 대조 경로) 은 이 변경과
  **아예 마주치지 않는다** — 지문을 다시 읽어보면 이 경로가 내는 판정은
  m14 의 노포인트(None) 3번과 m36 의 한판(Ippon) 하나뿐이다. `TryEnterOsaekomi`
  는 `Judgement.Yuko`/`Judgement.WazaAri` 일 때만 도는데 Mainline 은 그 둘을
  단 한 번도 안 낸다(§7.2 이전부터 STATUS.md 에 이미 있던 사실 — "42노드
  시나리오에도 한 번도 실측되지 않았다"). 그래서 지문이 그대로인 건 우연히
  같은 대역에 걸려서가 아니라, 이 경로가 애초에 그 코드를 실행조차 안 하기
  때문이다 — 구조적 이유다.
- 반면 **전체 테스트(177개)는 이 코드를 실제로 여러 번 돌리고도 전부
  그대로 그린이다** — `OsaekomiTests`(유효/절반 진입 다수),
  `MatteAfterScoreTests`, `PlayModeSmokeTests`(습관 브레인이 실제로 만드는
  유효 다수, §6.3 계측이 바로 이 경로) 전부 포함. 이건 구조적 회피가 아니라
  실측이다 — 이 스위트가 실제로 밟은 모든 경우에서 지불 전 체력과 지불 후
  체력이 §9.1 문턱(50) 또는 §9.2 대역(25/50) 기준으로 같은 쪽에 있었다는
  뜻이다. 다르게 나올 조합(그 교환의 스태미나 지불량이 크고 지불 전 체력이
  마침 문턱 바로 위에 있는 경우)이 이 스위트엔 없었을 뿐 — 앞으로 그런
  조합이 실제로 나오면 그때는 관측 가능한 차이가 날 수 있다. 지금은 "지금
  까지 측정한 모든 경우에서 동일"이 사실이고, "앞으로도 항상 동일"은 아니다.
- `fingerprint-baseline.txt` 는 갱신하지 않았다 — 이미 일치해서 갱신할
  이유가 없었다(사람이 정할 문제였는데, 다를 이유 자체가 없어져서 그
  판단이 필요 없어졌다).

검증: `dotnet run -- --check` (tools/dotnet-harness) 177/177 그린,
`CHECK: fingerprint matches fingerprint-baseline.txt`.
