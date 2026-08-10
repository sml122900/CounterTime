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
