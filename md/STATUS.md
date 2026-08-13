STATUS

Update this file every time a session resets. This is the ONLY copy — the root CLAUDE.md pulls it in with `@md/STATUS.md`, so it loads automatically at session start. Do not create a second copy anywhere. Check "Current State" and "Next Up" before starting work.

Current State (last updated: 2026-08-13 밤, v3 후속 측정)
**v3 후속 측정 완료 — 승률 붕괴 원인 분리 + 로스터 차별성 재측정(B 행동 기준) +
그립 파리티 동역학.** 브랜치 `feat/match-engine`, HEAD `a3fb534`(무수정, 그대로).
`docs/overnight-2026-08-13-v3.md` 신규. 앞선 두 야간 수집(원본/재수집)이 남긴 두
미해결 질문에 대한 측정 전용 후속 — 이번에도 설계·⚙·Core/Brain/Game 무수정,
`tools/dotnet-harness/Overnight/AutoAPlayer.cs`도 한 글자도 안 건드렸다(git diff
로 확인). **작업1(로스터 차별성, A 승패와 무관하게 B 자신의 행동만으로 재측정)**:
`MatchResult.cs`에 습관별 발화 카운트·흔들기/견제/큰기술 선언 분포·그립 표적
분포·카운터준비 선언 카운트 신규 필드 추가(전부 순수 추가, 기존 필드·로직
무변경) + `MatchSimulator.cs`에 그 필드들을 채우는 계측 라인만 삽입(기존 로직
비변경, 지문 IDENTICAL 로 확인). 6상대×500경기 각각의 행동 벡터(교환당 비율)를
만들어 15쌍 유클리드 거리 행렬 산출 — **원본이 "실제로 구별 안 됨"이라 지목한
문준배-오재훈 쌍**은 이번 측정에서 수치 자체는 달라졌지만(더 이상 소수점까지
동일하지 않음) 15쌍 중 여전히 가장 가까운 축(거리 0.2388, 15쌍 평균 0.6731,
최단쌍은 서도현-문준배 0.2194)에 남아 있다 — "완전 미구별"에서는 벗어났으나
"가장 덜 구별됨"이라는 상대적 지위는 유지, 판단 없이 사실만.
**작업2(승률 붕괴 원인 분리)**: 재수집이 분리 못 했던 두 동시 변경(B 브레인
버그 3건 수정 `611e1e2` / AutoAPlayer 3중 안전장치 `a3fb534`)을 실제 커밋
3개(원본이 실행한 ec1cd8e 계열/이 세션이 새로 실행한 611e1e2/재수집이 실행한
a3fb534)로 갈라 비교. **611e1e2 실행은 이 세션이 `git worktree add`로 읽기전용
체크아웃해 그 커밋에 이미 있던(무수정) `dotnet run -- --overnight`를 그대로
돌려 얻은 신규 데이터**(nunit.framework.dll 빌드 종속성만 메인 트리에서 복사,
git 추적 파일은 워크트리 안에서 전혀 편집 안 함, 실행 후 `git worktree remove`
로 정리) — 콤보1/콤보3은 기존 발행 문서 수치 재사용(재실행 없음). **4번째
조합(new AutoA × old B)은 실제 존재한 적 없는 조합이라 격리 불가로 명시하고
구성하지 않음**(작업 지시가 명시적으로 허용한 경로 — 워크트리를 읽기전용으로
쓰라는 제약과 상충해서). 6상대 평균 A 승률: 콤보1(구B×구AutoA) 99.1% →
콤보2(신B×구AutoA) 77.7% → 콤보3(신B×신AutoA, 현재 HEAD) 4.3%. **콤보2 가
새로 밝힌 것**: B 그립 수정 단독으로도(AutoA 안전장치 없이) 서도현전 평균
1615.5교환(최대 3000, 안전상한 도달 205/500=41%)까지 치솟는 그립 교착이
실제로 재현된다(6상대 평균 안전상한 도달 11.8%) — STATUS 가 서술해온 "서도현전
seed=5, 2994/3000턴 그립다지기" 현상의 재현. 낙폭 분해: 콤보1→2(B 수정)
21.4%p, 콤보2→3(AutoA 수정) 73.4%p — 총 낙폭(94.7%p) 중 약 77%가 AutoA 개편
구간에서 나왔다. **다만 이게 "B가 강해졌다"인지 "AutoA가 원래 갖고 있던
인위적 그립 우위를 안전장치가 걷어냈을 뿐"인지는 Auto-A가 처음부터 최적이
아니라는 전제 때문에 이 데이터만으로 더 못 쪼갠다** — 판단 없이 숫자만.
**작업3(그립 파리티 동역학, 판단 없이 계측만)**: 양쪽 `EffectiveGrip==0`
스트릭 33174개 실측(현재 HEAD, 6×500) — 평균 길이 **1.47교환**(장기 정체가
아니라 대부분 한두 교환 안에 끝남), 종료 사유는 그립 액션(14.4%)보다
흔들기/기술 선언(85.3%: A선언 51.3%+B선언 33.7%+양쪽동시 0.3%)이 압도적으로
많음(즉 파리티는 대개 저절로 안 풀려서가 아니라 누군가 다지기를 그만두고
다음 단계로 넘어가서 끝난다). 선언 직후 3교환 창 안에 실제 득점까지 이어지는
비율은 28.5%(나머지 70.8%는 창 만료까지 무득점) — 흔들기→밸런스드레인→득점
경로가 파리티에서 완전히 죽은 경로는 아니지만 자주 완결되지도 않는다는
직접 증거.
**검증**: `dotnet run -- --check` 257/257 그린 + 42노드 지문 IDENTICAL(새
계측 필드·라인은 전부 순수 추가라 기존 값 무변화 재확인). `AutoAPlayer.cs`
git diff 로 무변경 확인. `sh tools/stamp.sh --check md/*.md` exit 0(이 세션은
스탬프 대상 문서 무수정 — `docs/*.md`는 애초에 스탬프 대상 아님). 새 코드는
전부 `tools/dotnet-harness/Overnight/OvernightV3Runner.cs`(신규, `--overnight-v3`
진입점) + `MatchResult.cs`/`MatchSimulator.cs`/`Program.cs`/`Harness.csproj`
추가 라인뿐 — 기존 `OvernightRunner.cs`(원본/재수집 리포트 생성기)는 무수정.
상세 표·전문은 `docs/overnight-2026-08-13-v3.md` 참조.

**야간 수집이 찾은 버그 3건 수정 + AutoAPlayer 그립 교착 해소 + 재수집 완료.** 브랜치
`feat/match-engine`, 커밋 `611e1e2`(버그 3건, 이미 푸시) + 이 세션의 체크포인트(아래).
**작업1(버그 3건, Brain 프로덕션 코드, 커밋·푸시 완료)**: (①` HabitBrainCommitSource.
ActionFor(Move.GripFight)`가 오른소매만 고정 잡던 것을 좌우+`TurnChoice.OwnHand`
페어링으로 수정 — git blame 확인 결과 2026-08-10 브레인 최초 생성부터 있던 결함, 3.5b
로스터 저작이 만든 게 아니다. ②`SetupPrecedingCommitSource.IsBigThrow`가 `SurpriseExempt`
를 확인 안 해 변칙기가 흔들기에 예고당하던 것 수정. ③B 습관 AI가 §4.4 밸런스 게이트를
무시하던 것 수정(사람 결정: "B도 게이트 존중", `IBrainContext.OpponentBalance` 신규,
기존 `OpponentStamina` 페어와 같은 패턴). 하니스 257/257+지문 IDENTICAL.
**세션 중 발견 — 첫 재수집 시도가 진짜 no-op 이었다.** 그립 표적(`HandTarget`)만
번갈았더니 수치가 원본과 토씨 하나 안 틀리고 똑같이 나왔다(완전 재빌드로도 재현) —
원인은 `GripSystem.Grab`이 실제로 채우는 슬롯이 `TurnChoice.OwnHand`(내 손)이지
`TurnChoice.Hand`(상대 소매 표적)가 아니었던 것: 3-인자 생성자가 `OwnHand`를 기본값
`Right`로 고정해 표적을 아무리 바꿔도 매번 같은 내 손 슬롯만 덮어썼다. `AutoAPlayer.cs`
자신의 어젯밤 픽스(표적+`OwnHand` 같이 페어링)와 대조해 같은 패턴으로 재수정.
**작업2 — `AutoAPlayer.cs`(하니스 전용, 게임 코드 무관) 그립 교착 해소.** 진짜로
고치자 이번엔 서도현전 seed=5 가 3000턴 중 2994턴을 그립다지기에 갇혀 안전상한에
도달 — 진단 결과 `AutoAPlayer`의 그립다지기 종료조건(`ownGrip<1`)이 "B는 한손만 써서
Value=2 에 절대 못 닿는다"는 어젯밤 파일럿의 전제 위에 설계돼 있었는데, ①번 수정으로
B도 양손을 다 쥘 수 있게 되면서 둘 다 Value=2(GripScore=0, EffectiveGrip=0) 파리티에
도달하면 양쪽 다 `<1` 조건이 영원히 참으로 남아 못 빠져나왔다 — 어젯밤 수정이 의존하던
비대칭을 오늘 밤 진짜 버그 수정이 없앤 결과. 사람 승인 방향대로 3중 안전장치 추가:
그립다지기 인내 한계(`GripBuildPatienceLimit=6`⚙, 연속 실패 스트릭)를 넘으면 그립
우위 없이도 §4/5(밸런스 게이트 기반 파리티 커밋)로 넘어감 + 하드 상한(`GripBuildHardCap
=50`⚙, 경기 전체 누적)을 넘으면 `SuspectedGripDeadlock=true`로 집계하고 이후 영구
우회. `MatchResult`에 `A_SuspectedGripDeadlock`/`BTurnsAtGripParity`/
`BIdleTurnsAtGripParity`(작업3 관찰용) 신규.
**작업3 — 재수집(같은 시드 배열, docs/overnight-2026-08-13-recollect.md).** 안전상한
0건, 그립 교착 의심 0건(3중 안전장치 검증 통과), 대진표 크래시 0건. 문준배·오재훈이
이제 원본처럼 소수점까지 동일하진 않다(②가 원인이었다는 것과 일치 — 완전한 통계적
"구별"까지 되는지는 판단 없이 표만 남김). 밸런스 경제: B 닫힌게이트 시도가 원본
2208건에서 10341건으로 오히려 늘었다(A가 이제 파리티에서도 흔들기/견제로 밸런스를
계속 깎아서 B가 볼 게이트-닫힘 기회 자체가 늘어난 것으로 추정 — 판단 안 하고 사실만).
변칙기-흔들기 예고 0건(③ 수정 확인). 3경로(20초 한판/정규시간만료/3회지도패배) 전부
실측(1294/831/676건). **관찰(계측만, 지시대로 판단 안 함)**: 그립 파리티에서 B가
기술을 못 낸 비율이 상대별로 50.4%(황도철)~88.2%(진도윤) — "문준배·서도현 소극" 인상의
원인 후보 자료. **가장 중요한 관찰(사실만, 원인 미분리)**: A 승률이 원본(98.6~99.8%)
대비 재수집에서 0.0~21.8%로 붕괴, 대진표 500회 전부 Lost(메달 0건, 원본은 Gold
91.6%) — 되치기도 전 상대에서 크게 증가(예: 서도현 43→1169). 이 세션이 AutoAPlayer
전체(그립 파리티 로직 포함)와 B 양손화·게이트존중을 동시에 바꿔서 무엇이 얼마나
기여했는지 이 데이터만으로 분리 불가, Auto-A가 애초에 최적 플레이가 아니라는 점도
같이 감안 필요 — 사람 판단 대기.
**작업4 — 검증.** 하니스 257/257 그린 + 42노드 지문 IDENTICAL(버그 3건 커밋 이후 —
AutoAPlayer.cs 는 [Test] 없는 순수 툴링이라 애초에 테스트 대상 아님, 재확인만). Core/
Brain/⚙ 무수정 — 이번 세션이 건드린 파일은 전부 `tools/dotnet-harness/Overnight/*.cs`.
Unity MCP는 이 세션 내내 도구 자체가 안 잡혀 연결 확인 불가(하니스 전용 세션이라 코드
변경은 전부 Brain/harness — Unity 컴파일 재확인은 다음 세션 Unity 접속 시 필요).
**판단 필요 (정하지 않고 목록)**:
1. 승률 붕괴(위 참조)의 정확한 원인 분해 — AutoAPlayer 그립 파리티 로직/B 양손화/B
   게이트 존중 중 무엇이 얼마나 기여했는지.
2. 그립 파리티에서 B의 정체 비율(50~88%)이 실제 상대 성격("소극적")과 얼마나 일치하는지,
   이게 로스터 설계 의도인지 아니면 §4 폴백이 더 다양한 대응을 해야 하는지.
3. `GripBuildPatienceLimit=6`/`GripBuildHardCap=50`은 이번 세션이 고른 값 — 튜닝 대상.
**야간 무인 데이터 수집 완료 — 헤드리스 하니스로 6상대×500경기+대진표 500회.** 상세
보고서 `docs/overnight-2026-08-13.md`(230줄). 설계·⚙ 변경 없음, 수집 전용 — 새 코드는
전부 `tools/dotnet-harness/Overnight/`(신규: `AutoAPlayer.cs`/`MatchSimulator.cs`/
`OvernightRunner.cs`) 안에만 있다. **핵심 발견**: (1) Auto-A 행동 검증 — 밸런스 게이트가
닫혀 있을 때 A 가 큰기술을 던진 자가검증 위반 0건, 그립 우위 확보 뒤에만 공격 —
"허수아비 아님" 판정. (2) **Auto-A 의 그립 지배가 로스터 차별성 데이터를 오염시킨다** —
B 의 §4 폴백(`Move.GripFight`)이 항상 오른소매만 노려 A(양손 번갈아 확보)에게 구조적으로
밀리고, 그 결과 문준배·오재훈이 통계상 거의 구별 안 됨(그립 우위 습관이 거의 안 뜸).
(3) 3경로(20초 누르기 한판/정규시간 만료/3회 지도 패배) 전부 실측 성공(1898/1059/27건) —
기존 50교환 스모크 상한 때문에 회귀망에서 한 번도 안 밟혔던 경로들. (4) B 습관 AI 는
§4.4 밸런스 게이트를 전혀 안 본다(습관 조건 어디에도 상대 밸런스 없음) — 의도된 비대칭인지
사람 판단 필요. (5) 변칙기가 흔들기(Setup) 선행 데코레이터에 의해 예고당할 수 있는
설계 불일치 발견(`SurpriseExempt` 의도와 `SetupPrecedingCommitSource` 무조건 삽입이
충돌) — 코드 수정 안 하고 관찰만 남김. 전부 §6 "관찰 — 튜닝 후보"에 7개 항목으로
목록화(정하지 않음). **검증**: 하니스 `dotnet run -- --check` 독립 재실행으로 257/257
그린 + 42노드 지문 IDENTICAL 확인(에이전트 자체 보고와 별개로 이 세션이 재확인).
안전상한(3000회) 도달 0건, 대진표 크래시 0건. **세션 중 발견해 하니스 전용 코드로 고친
버그 1건**(엔진 무관): 파일럿 실행에서 Auto-A 가 항상 오른소매만 잡는 단일 손 전략이라
B 의 동일 전략과 맞물려 그립이 0으로 영구 고착, 매치의 99.4%가 안전상한에 걸림 —
Auto-A 가 잡기 다지기 턴마다 좌우를 번갈아 양손을 다 잡도록 수정해 해결.
커밋 `ec1cd8e`(하니스)+`2828b85`(보고서), origin 에 푸시 완료.
**§H 3.5b-로스터 완료 — 구별되는 상대 6명 실저작(습관만).** ROSTER.md 근거. 3.5b-뼈대의
진도윤 임시 배치를 6명으로 교체 — 콤보([step7])·예측 되치기[step5]·적응[step6]·평판
확산은 전부 이월, 지금은 습관만으로 싸운다(작업 지시 그대로). 경기 엔진 무수정 — 전부
Brain 계층(`Assets/_Project/Scripts/Brain/`) 확장.
**신규 파일**: `HwangDochul.cs`(32강)/`MoonJunbae.cs`(16강)/`SeoDohyun.cs`(8강, ROSTER
초안 placeholder "서강윤"에서 개명)/`ImJunchul.cs`(4강, "임세라"에서 개명)/`OhJaehoon.cs`
(동메달전) — 전부 `Jindoyun.cs` 패턴 그대로(코드 팩토리, `OpponentAsset` 안 씀). 결승은
`Jindoyun.cs` 자체를 "결승 상향"으로 수정(아래).
**신규 습관 9개**(`HabitId` 5→14): 황도철 `BigThrowAlways`(조건 없음, 90%, 쿨다운2) /
문준배 `CounterThrowOnGripEdge`(그립 우세, 35%, 쿨다운5 — "가끔"만, 나머지는 §4 3단
폴백이 큰기술 없이 자연히 방어적으로 만든다) / 서도현 `CounterReadyOnOpponentSetup`
(상대 흔들기 중, 70%) + `StayDefensiveWhenBalanceHigh`(밸런스 온전, 75%, 좁은→넓은
순서) / 임준철 `ShoulderThrowOnGripEdge`+`HeelHookOnGripEdge`(그립 우세, 60%, 쿨다운2
씩 나란히 — 아래 "번갈아" 근사 참조)+`FeintAfterShido`(지도 후 2턴, 75%, 견제기라
진도윤 H2와 다른 Id) / 오재훈 `MixedBigThrowA`+`MixedBigThrowB`(그립 우세, 50%,
쿨다운2 나란히 — "특출난 축 없이").
**[설계 판단] "두 갈래를 번갈아"(ROSTER §R4/§R6) 근사**: 정확한 매 순간 50/50 무작위
대신, 같은 조건에 쿨다운 2를 나란히 준 두 습관으로 구현 — 첫 습관이 뜨면 쿨다운에
걸려 둘째 차례가 오고, 그 다음엔 다시 첫째 차례(그립 우세가 매 턴 유지된다는 전제하에
엄격한 A-B-A-B). `HabitAction`/`HabitLadder` 를 안 건드리고 기존 쿨다운 메커니즘만
재사용한 것 — 진짜 매 순간 무작위 50/50 을 원하면 그 둘을 확장해야 한다(다음 판단
필요 목록 참조). 단위테스트로 A-B-A-B 를 직접 고정해뒀다.
**변칙기(하드 규칙, 습관 아님) 실제 배선** — 이전 세션까지 "말아업어치기는 습관이
아니라 하드 규칙"이라는 문서/테스트만 있고 실제 트리거 코드가 어디에도 없었다(재확인
후 신규 구현). `SurpriseTechnique.cs`(신규, 불변: techniqueId + 조건) +
`OpponentDefinition.Surprise`(nullable 필드) + `HabitState.TryFireSurprise`(경기당
1회 플래그 — HabitId 가 없는 변칙기는 기존 `_lastFiredTurn` 딕셔너리에 넣을 자리가
없어 별도 bool). `HabitBrainCommitSource.TryNext` 가 사다리를 그대로 돌린 뒤(턴 계수
보존) 변칙기가 발화하면 결과를 덮어쓴다 — `LastDecision` 은 비워 관측 원장에 절대
안 남는다(습관이 아니므로). 진도윤 말아업어치기(체력<30) + 임준철 안오금띄기(밸런스
<50 ⚙) 둘 다 실제로 배선. **부작용 1건(문서화)**: 사다리 결과와 변칙기가 같은 턴에
동시에 조건이 서면(드묾 — 둘 다 소모 상태 조건이라 자주 안 겹침) 버려진 습관의
쿨다운이 이미 시작된 채로 남는다 — `HabitLadder`의 "부르는 쪽이 턴 증가를 잊을 수
없다" 계약을 지키는 대가로 받아들인 사소한 튜닝 오차.
**결승 진도윤 상향**(ROSTER §R5): 습관 신뢰도 5개 전부 기본값(0.75)→0.65(더 섞음 ⚙),
scoutingLevel 0→1 시드("결승쯤 오면 내 앞선 경기를 봤다" 서사 — 3.5a 관행대로 필드만
채우고 효과는 §5/6 배선 전까지 inert). 습관 목록 자체(H1~H4b)와 순서는 무수정.
**IHabitContext 확장(2개, 보고 후)**: `OpponentSetupActive`(서도현 H1 — "예고"=독심이
아니라 §4.4 흔들기의 실제 게임 상태, 인과상 이미 지나간 사실) / `OwnBalance`(서도현
H2 + 임준철 변칙기 — 이미 공개된 `MatchEngine.Balance.Get` 을 그대로 읽음). 둘 다
`IBrainContext` 전용 개념(지도 개수·시계·게이트)이 아니라 습관 면에 새는 게 아니다.
`MatchBrainContext` 에 두 줄만 추가. 관문 테스트
(`LiveReadResolverTests.HabitConditionsNeverSeeTheBrainContext_OnlyIHabitContext`)를
넷→여섯으로 갱신 — 이게 "새 습관이 실제로 요구할 때만" 규약을 지킨 흔적이다.
**대진표 배선**: `LiveMatchRunner`에 `_opponentFactory`(기본 `Jindoyun.Create`, 기존
호출부 전부 무수정 컴파일) + `SetOpponent(Func&lt;...&gt;)` 최소 추가.
`MetaFlowController.OpponentFactoryFor(round)` 가 라운드→팩토리 매핑(엔진 쪽), `Tourna
mentBracket`의 `RoundOpponentName`/신규 `RoundOpponentId` 가 이름/세이브id 매핑(표시
쪽) — 두 매핑이 정확히 같은 스케줄(ROSTER 순서)을 공유. `TournamentBracket`은 Brain
어셈블리 참조를 안 늘리려고 일부러 엔진 팩토리 매핑을 안 갖는다(그건 이미
LiveMatchRunner 와 얽혀 있는 MetaFlowController 쪽 책임). `ConfirmMatchResult`의
`RecordOpponentFought` 호출도 하드코딩된 진도윤 상수 대신
`TournamentBracket.PlayerOpponentId/Name(round)` 를 쓰도록 교체.
**ROSTER.md 갱신(v1.0→v1.1)**: 8강/4강 placeholder 이름을 서강윤→서도현, 임세라→
임준철로 확정(작업 지시가 3.5b-뼈대 세션의 ROSTER 초안과 다른 이름을 줘서, 코드와
문서를 동시에 맞췄다). §R9 확인 필요 4건 중 이름·동메달전 스텁 여부·⚙ 초기치 3건을
해소로 갱신.
**검증**: Unity MCP 컴파일 0/0(전 과정 재확인 — 새 Brain 파일 6개+수정 파일 다수).
하니스 253→**257/257 그린**(신규 단위테스트 4개: 변칙기 경기당 1회 / 변칙기가
`HabitBrainCommitSource` 를 통해 실제로 사다리를 덮어쓰고 관측 원장에 안 남는 것 /
임준철 A-B-A-B 교대 / 서도현 좁은→넓은 순서) **+ 42노드 지문 IDENTICAL**(엔진 무수정
재확인). 기존 테스트 4개 갱신(값이 실제로 바뀌어서 — `HabitFiresAtSeventyFivePercent_
NotAlways`→`HabitFiresBelowThreshold_NotAlways`(0.65 로), `Jindoyun_ShipsAtScoutingLe
velZero`→`...LevelOne`, `TendencyWindowTests.ScoutingLevel0_StartsEmpty`→
`TendencyWindow_StartsEmpty_RegardlessOfShippedScoutingLevel`, `SurpriseTechIsNotAHabi
t_AndIsNeverLogged` 의 5→14 카운트 + 변칙기 2종 직접 확인 추가).
**Play 실측** — 6상대 전부 `LiveMatchRunner.SetOpponent`로 실제 전환되는 것 확인
(이름 일치 6/6), 각 40교환 스모크: 황도철 judgements=3(가장 공격적, B 체력 100→10)
/ 문준배·서도현 judgements=0(가장 소극적, 설계 의도와 일치) / 임준철 judgements=1 /
진도윤 judgements=5(가장 활발, "완성형"과 일치) / 오재훈 judgements=1. **변칙기
발화 문턱 실측 통과**: 진도윤전 80교환까지 몰아 B 체력 16(<30 문턱 통과, 정상 종료)
/ 임준철전 77교환에서 B 체력 3·밸런스 13(둘 다 문턱<50 통과, 정상 종료) — 둘 다
에러 없이 완주해 변칙기 경로가 실전에서도 안 깨진다는 걸 확인. B 의 구체적 기술
선택 자체는 설계상 공개 API로 안 새므로(플레이어에게 숨기는 게 의도) 조건 로직의
정확성은 위 단위테스트가, 파이프라인이 안 깨진다는 건 이 스모크가 각각 담당한다.
**세션 중 발견**: RunCommand 스크립트에서 `GameObject.Find`는 비활성 오브젝트를 못
찾는다(재확인, 지난 세션에도 걸렸던 함정) — `SceneManager.GetActiveScene().
GetRootGameObjects()` 순회로 우회. `CommandScript`에 헬퍼 메서드를 추가했을 때도
헬퍼 없이 인라인했을 때도 초반 NRE가 재현됐다가, 원인이 사실은 `GameObject.Find`
문제였음이 이번에 밝혀졌다 — 지난 세션에 "헬퍼 메서드 탓"으로 잠정 결론 냈던 것은
정정이 필요할 수 있다(다음 세션 참고, 근본 원인 조사는 범위 밖).
**판단 필요 (정하지 않고 목록)**:
1. "번갈아" 근사(쿨다운 기반 A-B-A-B) 대 진짜 매 순간 50/50 무작위 — 그립 우세가
   연속되지 않으면 엄격한 교대가 안 보일 수 있다. 진짜 무작위를 원하면 `HabitAction`
   에 대안 기술 슬롯을 추가하고 `HabitLadder`가 발화 시점에 고르게 해야 한다(공유
   Brain 인프라 변경, 이번 세션은 안 건드림).
2. 문준배·서도현의 "웬만하면 방어" 를 별도 습관 없이 §4 3단 폴백(정의상 무공격)에만
   의존하는 설계 — 스모크에서 judgements=0 으로 의도대로 보였지만, 이게 "너무
   답답하다"는 인상을 줄지는 실플레이 판단 필요.
3. 임준철 변칙기 임계(밸런스<50)와 신뢰도 수치들(문준배 35%/쿨다운5, 진도윤 결승
   0.65) 전부 이번 세션이 새로 고른 ⚙ — ROSTER 가 숫자를 준 것(90%/70%/60%)은
   그대로 따랐지만 안 준 것은 전부 실플레이 튜닝 대상.
4. ROSTER.md 이름 변경(서강윤→서도현, 임세라→임준철)이 3.5b-뼈대 세션에 이미 커밋된
   각종 대화·아직 커밋 안 된 코드에서 문서만 갱신됐다 — 커밋 시점에 STATUS.md 옛
   세션 로그(3.5b-뼈대)는 기록이라 옛 이름 그대로 남겨뒀다(그 시점엔 그게 맞는
   이름이었다), 헷갈리면 이 갱신 항목을 기준으로 볼 것.
**§H 3.5b-뼈대 완료 — 지도·연습 대회 대진표·경기 흐름 (진도윤 임시 배치).** META_DESIGN
§M2~M4 근거. 경기 엔진(MatchEngine/규칙) 무수정 — 새 코드는 전부 Game 메타 계층
(`Assets/_Project/Scripts/Game/Meta/`). 구별되는 6명 저작은 다음 세션(§H 3.5b 본작업),
이번엔 대회 흐름이 실제로 도는지를 진도윤 하나로 검증했다.
**신규 파일**: `TournamentBracket.cs`(순수 로직 — 32강 그리드 생성, 코인플립 진출자,
승/패 라운드 전환 규칙) + `MetaFlowController.cs`(오케스트레이션 + 런타임 캔버스 UI,
`LiveMatchPanel` 과 같은 "씬 무수정, 전부 코드로 짓는다" 패턴). **확장**: `SaveData.cs`
에 `TournamentRound` enum + `CareerSaveData.practiceCupRound` 필드(원래 META_DESIGN
§M1 스키마엔 없던 필드 — "진행 중 라운드"가 저장 안 되면 재접속 시 대회가 어디였는지
잃어버려서 추가, `practiceCupResult != None` 이면 stale로 문서화). **최소 추가**:
`LiveMatchRunner.Rebuild(int seed)`(공개 `Build()` 재호출 래퍼) + `LiveMatchPanel.
ResetForNewMatch()`(공개, 로그·선택 상태 초기화) — LiveMatch GameObject 를 매 라운드
새로 짓지 않고 비활성화만 했다 재사용한다(3D 무대 재구성 비용 회피). `PlayModeSetupMenu.cs`
개편: 진입점이 지도로 바뀌었다 — LiveMatch 는 에디터에서 미리 `SetActive(false)` 로
지어두고 "경기 시작" 클릭 시에만 켜진다(Awake 타이밍 경쟁을 코드가 아니라 비활성
상태로 원천 차단).
**[설계 판단] 상대 정체성**: 대진표 화면 이름은 ROSTER.md 확정 이름(32강 황도철→16강
문준배→8강 서강윤→4강 임세라→결승 진도윤, 동메달전 오재훈)을 그대로 쓰지만, 실제
엔진에 붙는 상대는 라운드와 무관하게 항상 진도윤이다(`LiveMatchRunner.Build()`가
이미 `Jindoyun.Create()` 고정, 손 안 댐 — 작업 지시 그대로). **[설계 판단] 다른 31명**:
32강 화면에서만 16쌍 실제 렌더 + 코인플립 진출자 캡션 한 줄. 16강부터는 다른 대진을
다시 그리지 않는다 — `TournamentBracket` 이 애초에 player 대진만 라운드별 고정 스케줄로
추적하고(ROSTER 순서 그대로), 나머지 31명은 32강 한 번만 코스메틱으로 계산되고 그 이후
라운드로 이어지는 실제 데이터 구조가 없다(정직하게 트리를 만들지 않은 단순화 — 아래
판단 필요 #2 참조).
**세이브 시점**: 경기 결과 확정 직후(`ConfirmMatchResult`) + 지도 복귀(`ShowMap`) +
앱종료(`OnApplicationQuit`). **세션 중 발견**: 유니티 에디터에서 Play 모드 종료가
실제로 `OnApplicationQuit()`을 호출한다 — 세이브 리셋 테스트가 계속 무효화되는 원인이었다
(직전 인스턴스의 메모리 상태가 재기록됨). 우회: Play 종료 → Edit 모드에서 세이브 파일
새로 쓰기 → Play 재진입 순서를 지켜야 리셋이 실제로 먹힌다. **opponents[] 기록**은 이번
세션은 id/name 만 채운다(작업 지시 "필드만 채워지고 경기 영향 0") — adaptation/
knownByMe/scoutingLevel 실채움은 §H 5/6 이월.
**검증**: Unity MCP 컴파일 0/0(전 과정 재확인). 하니스 253/253 그린 + 42노드 지문
IDENTICAL(엔진 무수정 재확인). **Play 실측 — 양쪽 경로 다 실제 엔진 매치로 완주**:
(1) 지도→유도장/집 왕복, 지도→경기장→연습 대회→32강 대진표(16쌍 렌더, 내 대진
하이라이트+"경기 시작" 버튼이 이름 사이에 정확히 배치됨 확인) → 기술 휠 클릭 1회
실측(스태미나 100→99 정확) 후 나머지는 `LiveMatchRunner.SubmitA` 공개 API 루프로
자동 진행(같은 프로덕션 코드 경로, 클릭 반복만 생략) → **패배 경로**: 32강 패배 →
`Lost`, "메달 없음" 터미널 화면 + "경기 시작" 버튼 소멸 + 대회 목록 상태줄 "결과: 메달
없음" 전부 확인. (2) **승리 경로**: 32강(황도철)→16강(문준배)→8강(서강윤)→4강(임세라,
**일부러 패배**)→**동메달전(오재훈) 자동 전환 확인**(`practiceCupResult` 는 `None`
유지, `practiceCupRound` 만 `BronzeMatch` 로 바뀜 — 터미널 아님을 실측 확인)→동메달전
승리→**`Bronze` 저장 확인**. 다섯 라운드 전부 실제 엔진 매치(각 12~49교환)로
완주했고, 매 라운드 대진표 표시 이름·"경기 시작" 버튼 위치·라운드 전환이 ROSTER.md
스케줄과 정확히 일치했다. `opponents[]` 는 5경기 내내 1개(jindoyun/진도윤)로 유지 —
`RecordOpponentFought` 의 멱등성(중복 안 쌓임) 확인.
**세션 중 겪은 것 2건(제품 결함 아님, RunCommand 샌드박스/에디터 특성 — 기록만)**:
(1) `System.Reflection` 은 `using` 뿐 아니라 완전정규화 이름(`System.Reflection.
BindingFlags...`)까지 정적 분석으로 차단됨(기존에 알려졌던 제약의 더 엄격한 형태
재확인) — 리플렉션으로 실행 중 인스턴스 상태를 조작하는 우회는 이번 세션에 전혀 안
됐다, 그래서 라운드 전환 테스트는 전부 "진짜 매치를 진짜로 이겨서" 진행했다(승리
전략 탐색: 무작정 공격 스팸은 지더라, 공격/카운터 준비/그립 확보를 3턴 주기로 섞는
전략에 seed=1 로 이겼다). (2) RunCommand 스크립트의 `CommandScript` 클래스에
`Execute()` 말고 별도 헬퍼 메서드를 추가하면 정체불명의 이른 단계 NullReferenceException
이 재현됐다(2회) — 헬퍼 없이 `Execute()` 안에 전부 인라인하면 해결. 원인 불명(Unity
AI Assistant 패키지 쪽으로 추정), 다음 세션 RunCommand 스크립트 작성 시 참고할 것.
**부수 수정 1건**: `MetaFlowController.ShowBracket()`의 자식 정리를 `Destroy()`→
`DestroyImmediate()`로 바꿨다 — 자동화 테스트 중 같은 프레임 재진입으로 보이는 자식
중복(6→9개)을 실측으로 잡아 방어적으로 강화한 것, 실제 사람의 단일 클릭 흐름에서
재현되는 결함인지는 불확실하다(자동화 스크립트의 빠른 연속 invoke 아티팩트일 가능성이
더 높다고 판단) — 화면이 드물게만 재구성되는 곳이라 즉시 파괴로 바꿔도 성능 비용은
없다.
**판단 필요 (정하지 않고 목록)**:
1. 대진표 표시 이름(ROSTER 확정 이름)과 실제 엔진 상대(항상 진도윤)의 불일치를 화면에
   아무 표시 없이 그대로 둘지 — 다음 세션에 6명이 실제 저작되면 자연히 해소되지만, 그
   전에 이 스켈레톤을 오래 쓸 경우 혼동 소지가 있다.
2. 32강 이후 라운드에서 다른 참가자 대진을 재구성하지 않고 정적 캡션만 보여주는 것 —
   `TournamentBracket` 이 player 라운드만 고정 스케줄로 추적하고 나머지 31명은 32강
   1회만 코스메틱 계산하는 단순화 트레이드오프(화면 몰입감 vs 구현 비용), 실제 트리를
   원하면 재설계 필요.
3. 세이브 저장 시점(경기 결과 확정 직후/지도 복귀/앱종료)이 META_DESIGN §M1 문구
   그대로인지, 훈련/휴식 화면 방문 자체도 저장 트리거로 넣어야 하는지 — 스캐폴딩이라
   최소로만 구현했다.
4. 패배 후 재도전(새 대회 시작) UI가 없다 — 터미널 화면은 "지도로 돌아가기"만 제공
   (META_DESIGN §M5 "[간단히] 지도 복귀만"과 일치하나, 실플레이에선 답답할 수 있다).
**MATCH_RULES.md 는 v4.1** (구 v3.8→v4.0→v4.1). §S 밸런스(제3 자원, 구 명칭
"스탠스" — v4.1에서 개명, `CoreStance` enum 과의 충돌 회피) 신설, §4.4 흔들기가
[초안]에서 밸런스 기반 확정판으로, §A~§G(입력 모델·기술 로스터·인지와 적응·
예측 되치기·판정 승격·숙련도·이월 목록) 신설 — 서기 국면 대형 개정. v4.0 반영
시 남았던 판단 대기 2건(자원 명칭 충돌 / TechniqueClass·흔들기 강등 우선순위)은
**v4.1에서 해소 완료** — 밸런스는 그쳐/굳히기 탈출 시 부분 회복만(완전 리셋은
경기 사이에만), 우선순위는 숙련도 결정 → 밸런스 게이트 최종 강등(게이트가
이긴다). §8/§S/§4.4/§F 참조. 3단계(밸런스 축 구현) 착수 전 더 이상 사람 결정
대기 없음.
**§H 구현 0~1단계 완료** (다음 항목): `GripSystem.CanUseConversion` 반대 손
버그 수정, 손 축 2단 선택 UI(손→타겟). 하니스 212/212, 지문 IDENTICAL, 7해상도
0/0, Play 실클릭 전체 흐름 확인. 2~8단계는 아직 착수 전 — Next Up 참조.
**용어 대응표**: "쿠즈시" = 구 명칭, 2026-08-11 오후 전체 개명 완료 → 화면 표시 "흔들기" / 코드 식별자 `Setup*`(예: `HandTarget.Setup`, `SetupGateEnabled`, `SetupPrecedingCommitSource`). 이 파일의 이전 세션 로그·docs/overnight-*·decisions/·daily/ 는 기록이라 "쿠즈시"로 그대로 남아있다 — 그 시점엔 그게 맞는 이름이었다. 지금 코드/MATCH_RULES.md 를 볼 때는 전부 흔들기/Setup 이다.
Unity 6.3.21f1 LTS editor installed
CounterTime project created (Universal 3D template, location C:\project_2026\CounterTime)
Unity AI Assistant package enabled; Claude Code ↔ Unity MCP connected (unity-mcp, 7 tools enabled). Console verified clean — 0 errors / 0 warnings as of 2026-08-03
Git initialized, remote is https://github.com/sml122900/CounterTime.git (branch main). Git LFS installed (3.7.1) and tracking fbx/obj/blend/png/jpg/tga/psd/wav/mp3/ogg/mp4/mov. .gitignore and .gitattributes rewritten for Unity (.meta kept via explicit negation rule, text assets pinned to eol=lf)
KNOWN + ACCEPTED: the first commit (204aefc) landed before the LFS config and is already on the remote, so Assets/TutorialInfo/Icons/URP.png stays in history as a 24KB raw blob. History rewrite was considered and rejected — it would need a force push to reclaim one 24KB tutorial icon. Everything committed from now on goes through LFS. See docs/troubleshooting/lfs-after-first-commit.md
Folder structure (Assets/_Project/…) and the two asmdefs created. CounterTime.Core has noEngineReferences: true and zero references; CounterTime.Game references CounterTime.Core. No .cs files written yet
Assets/_Project/Scenario/ is the single home for scenario files. Holds counter-time-scenario-A-vs-B.json and counter-time-scenario-board.html (both moved out of the repo root). Rule recorded in CLAUDE.md
Assets/_Project/Scenes/Main.unity is the stage: Floor (Plane, M_Stage_Floor blue URP/Lit), Directional Light, fixed side camera at (0, 1.6, -6) FOV 45 looking down +Z. Main.unity is Build Settings scene 0; SampleScene kept but disabled. The hand-authored scene YAML from earlier this session imported clean — Unity verified it, no errors
Fighters are Mixamo characters, not capsules: Fighter_A = Y Bot at x=-1 facing +X, Fighter_B = Ch30_nonPBR at x=+1 facing -X. Both sit at y=0 (character pivot is at the feet, unlike the capsule pivot which was at the centre). Both models are set to Animation Type = Humanoid with their own avatar generated from the model
Assets/_Project/Art/Animations/Fight Idle.fbx is also Humanoid; its clip was renamed from "mixamo.com" to "Fight Idle" and has Loop Time ON (Loop Pose left OFF). One clip drives both characters via humanoid retargeting
AC_Fighter_A.controller and AC_Fighter_B.controller (Art/Animations/) each hold a single state "Fight Idle" as the default state. Assigned to the two Animators, applyRootMotion off
M_Fighter_Grey.mat is now unused — it was the capsule material. Left in place, not deleted

시나리오 러너 v1 (탐색 모드) — 동작 확인 완료
CounterTime.Core/Scenario/: ScenarioData + ScenarioNode + ScenarioEdge (JSON 키와 필드명 1:1), NodeKind (border 색 → Narration/ChoiceA/ActionB/Judgement), ScenarioGraph (노드 사전 + from→to 인접, GetChildren), ScenarioRunner (n1 시작, Current, MoveTo, IsTerminal, NodeEntered 이벤트). UnityEngine 참조 0 — Core 는 여전히 순수 C#
CounterTime.Game/Scenario/: ScenarioLoader (TextAsset → JsonUtility → Core), ScenarioUIController (TMP 로 title/content 출력, 자식마다 버튼 생성 — 라벨은 자식 title, 색은 자식 border, 종단이면 "경기 종료 — 처음부터")
CounterTime.Game.asmdef 에 UnityEngine.UI / Unity.TextMeshPro 참조 추가
Main.unity 에 ScenarioCanvas (하단 1/3: TextPanel + 그 아래 ChoiceRow), EventSystem, ScenarioSystem(Loader+UIController) 추가. 선택 버튼은 Assets/_Project/Prefabs/ChoiceButton.prefab
JSON 실측: 노드 43, 엣지 52. border 분류가 데이터와 정확히 일치 (검정 4 / 파랑 14 / 빨강 11 / 초록 14). n1 에서 42개 노드 도달 가능, 유일한 미도달 노드가 n0 — 범례 제외가 특별 처리 없이 성립한다. 종단 노드는 n14/n18/n20/n26/n28/n34/n36/n41
한글 폰트: TMP Essential Resources 임포트 + NotoSansKR-VF.ttf (Windows 동봉, SIL OFL) 를 Assets/_Project/Art/Fonts/ 로 복사해 동적 TMP 폰트 에셋 생성. 기본 LiberationSans 는 한글 글리프가 없어서 전부 두부로 나온다. *.ttf/*.otf 를 .gitattributes LFS 규칙에 추가
Play 모드에서 실제 확인: 시작 노드 렌더링, 버튼 클릭 이동, 버튼 색 = 노드 border, 종단 노드의 재시작 버튼 → n1 복귀. 한글 글리프 미해결 0개
레이아웃 시각 검증 통과 (2026-08-05). 8개 노드 캡처로 확인 — 본문 최장 5개(n30/n1/n5/n7/n34), 선택지 최다 3개(n7/n30/n2), 종단 3개(n34/n36/n41). 한글 깨짐 0, 텍스트 넘침 0, 버튼 라벨 전부 1줄, 오토사이즈는 24pt 유지(하한 14pt에 안 닿음). "경기 종료 — 처음부터" 는 GraphicRaycaster 레이캐스트 + pointerDown/Up/Click 실제 포인터 이벤트로 눌러서 n1 복귀 확인
현재 하단 1/3 분할 (ScenarioPanel 기준): ChoiceRow 0.03..0.31, TextPanel 0.34..0.99, Title 0.72..1.0, Content 0..0.72. 1918x879 에서 타이틀 41/50, 본문 최대 115/128 — 여유 있음

노드↔모션 연결 — 동작 확인 완료
NodeMotionMap (ScriptableObject, Game): {nodeId, stateA, stateB} 리스트. 항목이 없거나 칸이 비면 둘 다 "Fight Idle". 에셋은 Assets/_Project/ScriptableObjects/NodeMotionMap.asset, 초기 매핑 36개 (n0 제외 42개 중 n1/n2/n27/n29/n39/n40 은 기본값이라 비워둠)
ScenarioMotionBinder (MonoBehaviour, Game): ScenarioRunner.NodeEntered 구독 → 맵 조회 → 두 Animator 에 CrossFade 0.25초. UI 쪽 Start 가 먼저 돌아 첫 이벤트를 놓치는 경우를 대비해 Start 에서 Current 로 한 번 맞춘다. 없는 상태 이름은 경고 후 무시
Core 는 손대지 않았다 — 모션 개념은 전부 Game 쪽. Scripts/Core 에 UnityEngine 참조 0 유지
AC_Fighter_A/B 각각 14개 상태 (Mixamo 10 + AI 생성 4). 원샷(Reaction / Standing Melee Punch / Stumble Backwards / Shoved Reaction With Spin / Taunt / Jumping / ai_seoinage_entry / ai_osotogari_entry / ai_ukemi_back)은 exitTime 0.9 + 고정 0.25초로 Fight Idle 자동 복귀 — 컨트롤러당 트랜지션 9개, 전부 같은 파라미터. 유지(Dying / Defeated / Victory Idle / ai_ground_pin)는 나가는 트랜지션 없음
클립 Loop: Fight Idle · Victory Idle · ai_ground_pin 만 ON. 나머지는 OFF (원샷은 복귀하고, Dying/Defeated 는 마지막 프레임을 유지)
Jumping.fbx 클립 이름이 mixamo.com 이라 "Jumping" 으로 바꿨다 — 상태 이름으로 써야 해서
화면상 A = Y Bot = 청록색(왼쪽, x=-1), B = Ch30 = 흰색(오른쪽, x=+1)
Y Bot renders white and Ch30 renders teal because neither FBX brought textures in; fine for greyboxing
Docs scaffolding created: docs/daily/, docs/decisions/, docs/troubleshooting/, docs/par-materials.md
2026-08-05 세션 문서: docs/daily/2026-08-05.md · decisions/scenario-runner-core-game-boundary.md · decisions/korean-font-tmp.md · troubleshooting/mcp-play-mode-verification.md · troubleshooting/tmp-text-overflow.md · par-materials.md 항목 4~8
2026-08-07 세션 문서: docs/daily/2026-08-07.md · decisions/ai-generated-motion-clips.md · troubleshooting/unity-asset-commit-boundary.md · par-materials.md 항목 9~10

Match engine core — steps 1–6 done (2026-08-09, branch feat/match-engine, not merged)
MATCH_RULES.md / CORE_DESIGN_MatchEngine.md live in md/ (untracked before today, now committed). The 2026-08-09 patch set is applied — exclusive referee categories, CounterReady, counter cost ownership, either-hand lock cap, exchange duration, time expiry, grip HUD as a requirement
Assets/_Project/Scripts/Core/ (NOT Assets/_Project/Core/ — the design doc said the latter, but that path is outside the asmdef and would compile into Assembly-CSharp with engine references; the doc was corrected)
Files: Enums · MatchRulesConfig · TechniqueSpec · TurnChoice · IRandomSource · JudgementCalculator · StaminaSystem · GripSystem · RefereeSystem · ScoreBoard · MatchClock · MatchEvent · MatchEngine (+ ExchangeScript)
Tests: Assets/_Project/Scripts/Core/Tests/ with CounterTime.Core.Tests.asmdef. 106 EditMode tests, all passing. Run them in Unity via Window > General > Test Runner > EditMode > Run All
Core stayed pure — zero UnityEngine references. That is load-bearing, not cosmetic: it lets the whole suite run outside Unity, which is how these 106 were actually executed (see below)

RULES_PATCH_v3.1 적용 완료 (2026-08-09, 같은 브랜치). 문서 헤더 v3 → v3.1
v3.1 은 원본 v3 기준으로 쓰여 있어서 PATCH 1·3·7 의 OLD 앵커가 안 맞았다 — 그 자리는 08-09 패치셋이 이미 고쳐놓은 상태였다. 문자 그대로 적용하는 대신 v3.1 문구로 교체했다 (v3.1 이 나중 권위)
CounterReady 부호 정정 — v3.1 §4.2 가 "공격자의 ReadLevel 이 한 칸 내려간다" 로 확정했다. 이건 어제 사람이 고른 것과 같은 방향이라 엔진 동작은 안 바뀌었다. 바뀐 것은 이름과 사다리 정의: ApplyCounterReady(올림) 삭제, StepDownRead(내림) 로 대체. 이벤트도 CounterReadyRaised → CounterReadyApplied 로 고쳤다 — 실제로 움직이는 건 공격자의 읽기이고 방향은 아래쪽이라 이름이 거짓말이었다
사다리가 일부러 불균등하다 — FullRead 는 Read 까지만 떨어진다(−1). 즉 완전간파한 공격은 카운터 준비만으로는 절대 되치기당하지 않는다. 읽기는 읽기로만 이긴다. CounterReady_CannotCounterFullRead 가 이 원칙을 고정한다
v3.1 이 새로 넣은 것 3개는 코드에도 반영했다: (1) §2 카운터 준비 게이트 — 선언한 쪽은 그 턴에 기술이 해결되지 않는다. 스크립트가 attacker 로 지목해도 엔진이 막는다 (2) §4.2 카운터 준비는 수비 자세가 아니다 — 코어를 수비적으로 골랐어도 −2 가 안 붙는다 (3) §4 되치기 정산 — PayCounter 로 분리(배수 없음)
시그니처도 v3.1 PATCH 12 에 맞췄다: HasLockedHand → AnyHandLocked, AdvanceExchange 는 void
RULES_PATCH_v3.1.md 파일은 디스크에 없었다 (내용을 대화로 받았다). "적용 후 삭제" 는 지울 대상이 없어서 실행할 게 없었다

문서 스탬프 = 본문 해시 (v3.1 커밋해시 방식 폐기)
`<!-- v3.2 | body:e11de04 -->` — 1행을 뺀 본문의 git hash-object (CR 제거). 스탬프를 쓰는 행위가 스탬프가 재는 대상을 안 바꾸므로 한 번에 수렴한다. 커밋해시 방식은 자기참조라 수렴이 불가능했고, 커밋 안 한 편집을 못 잡아서 존재 이유였던 실패를 정확히 못 잡았다
tools/stamp.sh (rewrite / --check) + tools/githooks/pre-commit 이 자동 유지한다. 손으로 고치지 마라. 클론마다 한 번: `git config core.hooksPath tools/githooks`
용도: 채팅에 붙여넣은 사본이 진짜인지 검증. 사본의 스탬프가 자기 본문과 안 맞으면 그 사본은 손으로 고친 것이고 원본이 아니다 — 그쪽에 맞추지 말고 불일치를 보고할 것
컨벤션은 CLAUDE.md 에 적었다. 세션 메모리에만 있는 규칙은 규칙이 아니다 (diff·다른 도구·다음 세션에서 안 보인다)
.gitattributes 에 *.sh / tools/githooks/* 를 eol=lf 로 고정했다. 안 하면 새 Windows 클론에서 CRLF 로 받아 셔뱅이 깨지고, 훅을 커밋한 의미가 없어진다

rules v3.2 — 수비 자세 정의 (판정식의 마지막 미정의 항)
수비 자세 ⇔ 상대 Core == Defensive. 다리·손은 판정에 안 들어간다. 이분법이고, "얼마나 웅크렸나" 가 아니라 "던질 수 있는 자세를 내줬나" 를 묻는다
극단적 방어도 −2 로 같다. 웅크린 깊이는 §6.1 소극 누적과 §5.2 −1/턴 드레인으로 이미 두 번 청구되고 있어서 여기서 세 번째로 청구하지 않는다
예외: 카운터 준비를 선언한 쪽은 코어가 수비적이어도 수비 자세가 아니다. 이게 없으면 −2 가 붙어 −4 → 방어자에게 공짜 유효가 난다. 테스트가 틀린 답(−4)까지 같이 검증한다
JudgementCalculator.IsDefensivePosture 가 유일한 파생 지점이다. MatchEngine 이 인라인으로 다시 유도하던 걸 걷어냈다 — v3.1 적용 중 나온 버그가 정확히 그 인라인 유도였다. grep 으로 다른 파생 없음 확인 (RefereeSystem/TurnChoice 의 Defensive 는 §6.1 rung 과 §2 극단적 방어 정의로 서로 다른 규칙)

연출 스펙 — 대파열 (2026-08-09 BurstFeelTest 로 실측)
BurstFeelTest.cs 와 Assets/_Project/Spikes/LowResRender/ 는 삭제하지 않고 **커밋했다** — §7 이 HUD 를 하드 요구로 못 박은 근거가 이 스파이크의 실측(320×180 에서 손 5.8px)이라, 측정 장치가 한 대의 디스크에만 있으면 규칙을 리포 밖에 적어둔 것과 같아진다. 2순위 부원 촬영도 이 씬이 파이프라인이다. Main.unity 는 7.6 에서 참조를 끊었다
**스파이크 씬의 nearMissSeconds 는 폐기된 초안값 0.08 이다. 확정 규격은 0.5** — 스파이크는 측정 장치지 사양서가 아니다
되치기 대파열 = 예고 0.8초 @0.25x → 정지 0.25초 → 흑백반전 3×0.05초. 근접 실패 = 0.5x 로 0.5초. 되치기 발화 임계는 **−3 유지 (option ⓐ)**
이걸로 §4.2 / §8 의 "대파열 연출 무게를 먼저 확인하라" 전제가 충족됐다. 확인 결과가 "ⓐ 그대로" 였으므로 §8 의 DEFERRED 항목(ⓐ vs ⓒ)은 42노드 감사까지 그대로 열어둔다 — 숫자는 안 건드린다
구조 주의 — 반전은 정지 "다음" 이 아니라 정지 "안" 에서 돈다. timeScale 0 구간이 총 0.25초이고 그중 앞 0.15초가 흑백 교대, 남은 0.10초가 정지 유지다. 순차로 읽으면 총 정지가 0.40초가 되어 60% 길어진다
근접 실패 지속시간 = **0.5초 확정**. Main.unity 에 남아 있던 nearMissSeconds: 2 는 편집 모드 잔재다 — Play 중 인스펙터로 맞춘 값은 저장되지 않으므로 씬에는 실측 전 값이 남는다. 스크립트 기본값 0.08 도 같은 이유로 무의미하다. 실측값의 출처는 Play 중 화면이지 씬 파일이 아니다

발견한 불일치 (양쪽 다 테스트로 고정해둠)
m15 노드는 A 체력 90 으로 기록돼 있는데 엔진은 93 이 나온다. 그 교환에서 지도가 나오고 → 그쳐 → §5.3 이 +3 을 준다. 문서 우선순위상 MATCH_RULES 가 보드 JSON 을 이긴다 — 93 이 맞고 m15 의 90 은 회복 적용 전 스냅샷이다
극단적 방어 4턴 = 지도는 임계가 정확히 12 일 때만 성립한다. §6.2 의 ±1 지터가 +1 이면 5턴이 된다. 테스트는 지터 0 으로 고정한다
CORE_DESIGN 에 남아 있던 모순 2개도 고쳤다 — EffectiveGrip 주석이 아직 "lead hand" 였고, 대상 폴더가 asmdef 밖이었다

테스트 실행 경로 (중요) — 어셈블리가 둘이고 하니스는 한쪽만 돈다
Unity MCP 로는 EditMode 테스트를 못 돌린다. TestRunnerApi.Execute 가 "User interactions are not supported for MCP tool calls" 로 막힌다
대신 Core 가 순수 C# 이라는 점을 이용해 dotnet 콘솔 하니스를 만들어 커밋된 NUnit 테스트 파일을 그대로 컴파일·실행한다 (nunit.framework.dll 은 Library/PackageCache/com.unity.ext.nunit@.../net40/unity-custom/ 에 있다).
**2026-08-11 부터 하니스가 리포에 있다** — `tools/dotnet-harness/` (Harness.csproj/Program.cs/UnityEngineShim.cs/README.md/fingerprint-baseline.txt). 이전 방침("하니스는 리포에 안 들어간다 — 세션마다 다시 만든다")은 뒤집혔다: `dotnet run` (그 폴더에서) 으로 재구축 없이 재현된다. `*.csproj` 는 리포 전체에 gitignore 돼 있지만(Unity 가 재생성하는 IDE 파일용 규칙) `.gitignore` 에 `!/tools/dotnet-harness/*.csproj` 부정 규칙을 달아 이 파일만 되살렸다 — README 참조
`dotnet run -- --check` 로 지문을 fingerprint-baseline.txt 와 자동 비교한다(불일치면 종료코드 1 + 라인별 diff). 지문이 의도적으로 바뀐 거면 그 파일을 손으로 재생성하고 커밋 메시지에 왜인지 적을 것 — 조용히 덮어쓰지 말 것
CounterTime.Core.Tests + CounterTime.Brain.Tests(전체) + CounterTime.Game.Tests(부분: ScenarioTurnChoicesTests/ScriptedReadResolverTests/ResultNarrationTableTests/LiveAssemblerTests) → 하니스로 돈다. MatchRunnerHandActionTests(3개) → **안 돈다.** new GameObject()/AddComponent<T>()/DestroyImmediate 를 써서 Unity 의 진짜 오브젝트 모델이 필요하다
그래서 하니스 출력 마지막 줄이 스스로 범위를 밝히게 해뒀다: "COVERAGE: CounterTime.Core.Tests + CounterTime.Brain.Tests (full) + CounterTime.Game.Tests PARTIAL ... MatchRunnerHandActionTests needs the real Unity Test Runner"
**"하니스 N개 초록" 은 MatchRunnerHandActionTests 가 검증됐다는 뜻이 아니다.** 그 3개만 사람이 직접 확인해야 한다: Window > General > Test Runner > EditMode 탭 > Run All

Opponent brain — stages 1–3 done (2026-08-10, branch feat/match-engine, not merged)
164 EditMode tests green: CounterTime.Core.Tests 117 / CounterTime.Brain.Tests 40 / CounterTime.Game.Tests 7. Run them in Unity: Window > General > Test Runner > EditMode > Run All
Document stamps (as of 2026-08-11 아침, verified via tools/stamp.sh --check): md/MATCH_RULES.md **v3.7 [초안 — 쿠즈시 §4.4, 사람 검토 전]** body:1d15c97 (§2 표에 뜯기/잡기 유지 행 + 동시 공격 우선순위 노트 추가, 아침 결정 반영) · md/OPPONENT_BRAIN.md v1.0 body:5d5a8c7 (무수정) · md/CORE_DESIGN_MatchEngine.md v3.2 body:a24d23c (무수정)
New asmdef CounterTime.Brain at Assets/_Project/Scripts/Brain/ — pure C#, references CounterTime.Core only, noEngineReferences true. Verified at the assembly level, not by grep: its referenced assemblies are exactly [netstandard, CounterTime.Core]. The brain has to run in the dotnet harness, which is why it is not in Game
Seams (interfaces in Brain, scripted implementations in Game):
  ICommitSource — where one fighter's TurnChoice comes from. bool TryNext(out who, out choice); false means "declared nothing this turn", a distinction §6.1 passivity rides on
  IReadResolver — the ONLY place ReadLevel is produced. Returns the BASE read; Core owns the §4.2 counter-ready step-down and a resolver that pre-applies it causes double reduction
  IExchangeAssembler — this turn's ExchangeScript list. One node can expand to several exchanges (m14 = 4)
  IHabitContext — 4 members, habit conditions only. Frozen surface
  IBrainContext — 6 members, §4 ladder rungs 1 (hard gates) and 3 (fallback weighting) only. Split from IHabitContext on purpose so shido COUNT and the clock never reach habit conditions; enforced by the condition delegate's parameter type and pinned by a reflection test
Scripted mode (대조 모드) was never touched. Every stage was verified with a 52-line fingerprint of the real MatchRunner walking MatchRunner.Mainline — all MatchEvents plus full engine state after each node — diffed against the previous stage. It came back IDENTICAL every single time, including across the v3.4 Core rule change
진도윤 has 5 habits (H1 업어치기 on grip edge / H2 안뒤축감아치기 after a shido / H3 극단적 방어 reserve / H4 오른소매 노림 / H4b 왼소매 노림 reserve) and the read ladder is 6 rows. Both are specified in OPPONENT_BRAIN §2–§4, not in code comments alone
Ladder order is narrowest-condition-first (H2 → H3 → H1 → H4 → H4b). With H1 leading, H1 fired 7 times while H2 and H3 fired zero — an exclusive ladder lets every rung starve the ones below it
Two rules were added because measurement broke a premise, both recorded with their evidence: OPPONENT_BRAIN §4 "the fallback never commits a big throw" (so every big throw B makes comes from a habit and is therefore readable), and MATCH_RULES §4.3 v3.4 "a score calls matte" (recovery was tied to matte while nothing produced matte, so stamina bottomed out at ~30 exchanges against the 60–80 §2 implies)
The dotnet harness now also compiles the real MatchRunner via a ~60-line UnityEngine shim (MonoBehaviour/TextAsset/Debug/Mathf/JsonUtility), so the Game path is exercised outside Unity. MatchRunnerHandActionTests (3) still needs the Unity Test Runner
Do NOT use TestContext.WriteLine in tests meant to run in the harness — the reflection runner sets up no NUnit execution context and it throws NullReferenceException. Console.WriteLine works in both

Step 4 (4b narration infra) + step 5 (osaekomi §9) landed overnight (2026-08-10, branch feat/match-engine, not merged). 164 → 178 tests (126 Core / 40 Brain / 12 Game — 3 of the 12, MatchRunnerHandActionTests, still need the Unity Test Runner and were not run by the harness). See docs/overnight-2026-08-10.md for full detail; summary below
Step 4: JudgementScored was NOT extended with a TechniqueId field — that was tried first (plan 1, additive constructor overload, old constructor untouched) and it made the 52-line fingerprint diff non-identical (the new field shows up on every dump line even though nothing scored differently), so it was reverted per the run's own verification protocol. Landed instead: MatchRunner (Game only, Core untouched) pairs JudgementScored with its ExchangeScript.Technique.Id via a new JudgementNarrated event, consumed only by a new play-mode-only MonoBehaviour (ResultNarrationPresenter) that scripted-mode scenes/tests never attach. ResultNarrationTable is the (Judgement, TechniqueId, IsCounter) → text dictionary from OPPONENT_BRAIN §6, empty until docs/step4-narration-worksheet.md is filled in and its entries wired up by hand
Step 5: MatchEngine.ResolveExchange's §9.1 TODO is now real — waza-ari always enters osaekomi, yuko enters only if the scorer's effective grip ≥ +1 and the thrown fighter's stamina < 50, everything else still lands on matte. §9.2 adv/escapesNeeded frozen at entry. §9.3 ResolveOsaekomiExchange resolves one 5s escape tick (score awarded before escape, matched pairs continue the hold and cost the defender 3 stamina, crossed pairs credit an escape). §9.4 escape resets grips to Free with no matte recovery
UNRESOLVED judgment call from step 5, not a literal reading of MATCH_RULES §9 (flagged in code + overnight doc, needs a human decision): §9's "attacker" is read as the fighter who scored (JudgementScored.ScoredBy), not the original technique declarer — the scorer is who ends up on top to hold. This only matters for a countered score. A countered waza-ari still enters osaekomi (§9.1 gives it no "never" option) using the scorer's grip for the adv term; a countered yuko was deliberately left NEVER entering osaekomi rather than guessing which fighter's grip the gate means. Never exercised by the 42-node scenario or by tonight's 3 smoke profiles (all show counters=0), so it is not currently reachable, but will need a real answer once the habit brain produces more counters
Match length moved after step 5, not tuned: realistic-A smoke profile (seed 1234, jitter 0) was 28 exchanges / 156s remaining before step 5 (matches the "21–34 exchanges, 135–177s" range below) and is now 26 exchanges / 162s remaining after — fewer exchanges finish the match sooner while more clock survives, because osaekomi (real time) replaces some of what used to be matte (clock stopped) mid-match. Numbers only, not touched. See docs/overnight-2026-08-10.md §3.2 for the repeating/varying profiles too
~~KNOWN GAP left for stage 6: ResolveExchange never checks Osaekomi.Active~~ — **closed 2026-08-11** by a guard (see below). What stage 6 still needs to decide is narrower now: not "does the engine notice," but "what should a standing exchange arriving on an open hold even mean" — see the new judgment-call entry in docs/overnight-2026-08-10.md §"판단 대기" #4.

§9 guard + full green recovery (2026-08-11, branch feat/match-engine, commits a165872/a036e75, not merged). Two follow-up requests in the same session, both closed
`MatchEngine.ResolveExchange` now throws `InvalidOperationException` if called while `MatchPhase.Osaekomi` is active (the KNOWN GAP above, made loud instead of silent) — `ResolveOsaekomiExchange` is the only valid entry point once a hold is open. §9.1 also got its interpretation nailed down: MATCH_RULES v3.4 → v3.5, "attacker" in §9.1/§9.2 means the judgement's scorer, and a countered score uses the exact same gate as an uncountered one (previously a countered yuko was hard-coded to never enter osaekomi — that carve-out is gone)
Adding the guard broke 9 tests that were silently relying on the exact gap it closes. All 9 fixed without changing what any of them verify — not by loosening the guard: `OsaekomiTests`' shared `EnterViaYuko()` helper had a real off-by-one (`DrainB(14)` should have been `13` — grip was already fixed at +1 the whole drain loop, so the 14th call was itself already scoring and silently double-entering osaekomi); `PlayModeSmokeTests`' headless loop never called `ResolveOsaekomiExchange` at all, so a fixed drain policy (always 어긋남) was added to actually close holds as they open; `MatteAfterScoreTests.MatchEndingScore_DoesNotCallMatte` couldn't be saved as originally written (waza-ari has no §9.1 gate to dodge, so "make the first score skip osaekomi" is not constructible) — narrowed to a single-exchange direct ippon that still pins the exact §4.3 claim, with the lost combining-path coverage patched onto `WazaAriEntryPlusTenSecondHold_CombinesToAwaseteIppon` instead (2 new assertions, existing ones untouched)
178 → 180 tests (2 new: the guard's own test, and a §9.1 v3.5 countered-yuko-enters-osaekomi test). 177/177 harness-reachable, 3 Unity-only (unchanged)
The overnight session's "byte-identical" fingerprint claims (commits 182f0e5/979a4f2/478d46c/a8e89af) were never backed by a committed artifact — checked git history, no such file was ever added. Independently re-verified 2026-08-11 by checking out all 5 points (parent of 182f0e5 through a8e89af) into an isolated worktree and re-running a freshly-built harness at each: byte-identical throughout, confirming the overnight claims hold up under a from-scratch redo, not just self-comparison within one session
Full detail, including the exact 현실적 A osaekomi-entry-shortfall breakdown and the verbatim smoke-profile A commit policies: docs/overnight-2026-08-10.md §0/§5/§6
**Smoke A is a scarecrow, not a fighter — do not use the 3-profile numbers as balance data.** `PlayModeSmokeTests.RunMatch` assigns the attacker/technique per turn from whether B's habit brain fired one, full stop — A's declared Core/Legs (Attack/Defend/CounterReady/whatever the profile array says) never enters that decision. Whenever B doesn't attack, A is scripted as attacker throwing a fixed 밭다리(BigThrow, Standard) regardless of what A "chose" that turn. So the 3 profiles (반복/변주/현실적) don't actually differentiate A's attack behavior from each other in any way that reaches the engine — only B's habit brain and the read/row bookkeeping see A's declared choice. Exact code + explanation: docs/overnight-2026-08-10.md §6.4.
§9 stamina-snapshot fix (2026-08-11, MATCH_RULES v3.5 → v3.6): the §9.1 osaekomi gate's "체력 <50" now reads the SAME pre-payment stamina snapshot §4's judgment formula already used earlier in the same exchange, instead of a fresh post-payment read taken later in `TryEnterOsaekomi`. Both readings sat in the same [25,50) band in every case measured so far, so no observed behavior changed, but it was two different numbers answering the same question inside one exchange — now it's one. See docs/overnight-2026-08-10.md §8 for the fingerprint-diff result of this change (a deliberate behavior change per §9.1's own new wording, checked and reported, not silently accepted).

야간 자율 실행 2차 (2026-08-11 밤, branch feat/match-engine, not merged) — 작업2~6, 커밋 6bd5a06..a9614cb
200/200 harness green (180→200: 작업2 +2, 작업3 +8, 작업4 +6, 작업5 +6, 작업6 +1 신규 테스트[기존 3개는 시그니처 기본값으로 무변화]), fingerprint IDENTICAL 전 구간. 전체 상세는 docs/overnight-2026-08-11.md — **판단 대기 5개가 그 문서 맨 위에 있다.** 요약:
  작업2 — docs/step4-narration-worksheet.md 배정 9키를 ResultNarrationTable.CreateDefault() 로 등록(문구 무수정). m27·m34는 워크시트 결정대로 폴백 유지. m18 TechniqueId="밭다리"는 지시 문자 그대로이나 실제 기술(안뒤축감아치기)과 달라 판단 대기 #1
  작업3 — MATCH_RULES §4.4 **쿠즈시(무너뜨리기) [초안 — 사람 검토 전]**, v3.6→v3.7. KuzushiGateEnabled 기본 **false**(대조 모드 완전 동일 보장). on일 때: 그립 없이 선언 불가, 성립해도 판정 미발화, 1교환만 지속, 지속 중 아니면 다음 큰기술이 Sloppy 로 강등(JudgementCalculator 자체는 무수정 — 강등은 MatchEngine 컨텍스트 단계), SurpriseExempt 로 말아업어치기류 면제, 그쳐/굳히기 진입/경기 종료로 소멸. HandTarget.Kuzushi 는 enum 끝에 추가(값 안 바뀜)
  작업4 — Brain/KuzushiPrecedingCommitSource.cs: HabitBrainCommitSource 를 감싸 큰기술 앞에 쿠즈시 턴 자동 삽입. 습관 카탈로그·읽기 사다리 무수정 — 이미 나온 결정을 한 턴 버퍼링해서 늦출 뿐. 발견한 미해결 타이밍 이슈(LiveReadResolver.NoteOpponentHabit 이 쿠즈시 교환에서 먼저 불려 진짜 기술 교환엔 새 알림이 없음) → 판단 대기 #2, 지금은 아무 데도 안 배선돼 있어 관측되는 버그 아님. B 쿠즈시는 현재 100% 정직한 tell — 페이크는 후속 설계
  작업5 — 6단계 스캐폴딩 3파일(Game/Match/): LiveAssembler(순수 C#, 하니스 테스트 있음) + LiveMatchRunner(오케스트레이션, MatchRunner 42노드 대조와 완전 별개) + LiveMatchPanel(MatchHud 전례대로 런타임 캔버스, 씬 무수정) — 손/코어/하체 3열, 확정→해결→서술(폴백 시각 구분)→다음 턴, 내 성향 노출 막대(경고선/위험선, 확정 전 실시간), 굳히기 2지선다. B 선택지·심판 누산기 숫자 전혀 노출 안 함. Unity 에디터 typeof 로드까지 확인(0 에러/0 경고) 했으나 **LiveMatchRunner/LiveMatchPanel 은 Play 모드 실행 확인 안 됨 — 사람 몫.** 씬에 컴포넌트를 붙이는 것도 사람 몫(씬 파일 무수정). 판단 대기 #3(뜯기·잡기 유지 게이트는 §2 표에 없어 추론) #4(동시 공격 시 B 우선순위, PlayModeSmokeTests 선례 재사용) #5(B 굳히기 정책은 고정값, 진짜 AI 없음)
  작업6 — PlayModeSmokeTests 에 kuzushiEnabled 매개변수(기본 false) 추가, 3프로필×{off,on} 실측 6회를 docs/overnight-2026-08-11.md 비교표로. 튜닝 없음. 관찰: 반복 프로필 교환수 3.5배 증가(4→14), 변주 프로필은 on 에서 50교환 캡을 소진하고 미종료(off 는 27교환에 종료), 되치기는 반복/on 셀에서만 5건 — 전부 해석이지 확정 아님, 판단은 사람 몫
  Unity Editor + MCP 브리지가 밤새 연결 유지(사람 없음) — 매 작업 후 AssetDatabase.Refresh() + GetConsoleLogs 로 0 에러/0 경고 재확인, RunCommand 안에서 짧은 이름(CompilationPipeline 등)이 Unity.AI.Assistant.* 네임스페이스와 충돌하는 기존 함정 재확인(전체 경로로 우회)

Next Up (in priority order)
**가장 최근 — §H 3.5b-로스터: 구별되는 상대 6명 실저작(습관만).** 상세는 위 Current
State 항목 참조. 요약: `Assets/_Project/Scripts/Brain/`에 5개 신규 상대 팩토리
(`HwangDochul`/`MoonJunbae`/`SeoDohyun`/`ImJunchul`/`OhJaehoon`) + `SurpriseTechnique`
(변칙기 하드 규칙, 신규) + `HabitId` 5→14 + `IHabitContext` 2개 확장(보고 후) +
진도윤 결승 상향(신뢰도↓+scoutingLevel 1)+변칙기 실배선. 대진표(`TournamentBracket`/
`MetaFlowController`)가 라운드마다 실제로 다른 Brain 팩토리를 골라 쓰도록 배선.
ROSTER.md v1.0→v1.1(8강/4강 이름 확정: 서강윤→서도현, 임세라→임준철). 하니스
253→257/257+지문 IDENTICAL. Play 스모크로 6상대 전부 이름 일치 확인 + 상대별로
확연히 다른 공격 빈도(황도철 최다·문준배/서도현 0·진도윤 최다) + 변칙기 두 종
(말아업어치기/안오금띄기) 문턱을 실제로 넘겨도 안 깨짐 확인. 판단 필요 4건은
Current State 항목 끝에 목록으로 남김.
**다음 — §H 4: 판정 승격(§E).**
**가장 최근 (그 앞) — §H 3.5b-뼈대: 지도·연습 대회 대진표·경기 흐름 (진도윤 임시 배치).**
상세는 Session Log 참조. 요약: `Assets/_Project/Scripts/Game/Meta/`
신설(`TournamentBracket.cs`/`MetaFlowController.cs`), `SaveData.cs`에 `TournamentRound`+
`practiceCupRound` 추가, `LiveMatchRunner.Rebuild`/`LiveMatchPanel.ResetForNewMatch`
최소 추가로 LiveMatch GameObject 재사용, `PlayModeSetupMenu.cs` 진입점을 지도로 전환.
Play 실측으로 패배 경로(32강 패배→Lost)와 승리 경로(32강→16강→8강→4강 패배→
동메달전→Bronze) 둘 다 실제 엔진 매치 5~6판으로 완주 확인. 하니스 253/253+지문
IDENTICAL(엔진 무수정).
**가장 최근 (그 앞 그 앞) — ROSTER + DESIGN_future-mechanics 정본화 (문서 전용, 코드 무수정).**
사용자가 md/에 넣어둔 초안 2종(`ROSTER_DESIGN-draft.md`, `DESIGN_future-mechanics-
draft.md`)을 검토·편입. 내용은 초안 그대로(지어넣기 없음) — 헤더만 "초안, 검토용"
지위에서 정본 지위로 교체하고 1행 스탬프를 붙였다.
**`md/ROSTER.md` 신규 정본(v1.0, body:f8c24e5).** 연습 대회 6상대(32강 황도철~
결승 진도윤+동메달전 오재훈, 전부 placeholder 이름) — §R0 설계 원리(습관+주력기+
난이도 손잡이 3개), §R1~§R6 라운드별 상대, §R7 난이도 커브 표, §R8/§R8b 저작
메모+콤보·변칙기술 접점, §R9 확인 필요 4건. 단계 점등 표기([now]/[step5]/[step6]/
[step7]) 그대로 보존 — 진도윤=결승 최종보스/변칙기는 4강·결승만/초반 2명
튜토리얼/4강=어깨매치기·발뒤축걸기(안뒤축은 진도윤 콤보로 제외) 확정 전제 유지.
**정정 1건**: 초안 본문은 [step7] 마킹을 실제로 쓰면서도(§R4/§R5 시그니처 콤보)
맨 위 표기 범례엔 [now]/[step5]/[step6]까지만 정의돼 있었다 — 범례에 [step7]
(콤보·변칙기술 성장, `DESIGN_future-mechanics.md` 붙은 뒤)을 추가해 본문과
맞췄다. 내용 재해석이 아니라 이미 쓰이던 마킹을 범례에 반영한 것으로 판단해
진행.
**`md/DESIGN_future-mechanics.md` 신규 정본(v1.0, body:4112f17).** §1 계단형
시그니처 성장, §2 변칙기술(노출 전 파괴적, 관측되면 감쇠), §3 콤보(연계 숙련도,
즉각 창만, §E 승격 위력+체력 할인), §4 체력 격화(경합 누적+저체력 배율), §5
구현 순서·의존, §6 확인 필요 5건. **[편집 판단]** 문서 첫머리의 "전부 §H
7단계·훈련 시스템 이월" 명시에 더해, §1~§4 네 절 각각에도 `**[§H 7단계 이월 —
지금 구현 대상 아님.]**` 한 줄을 넣었다 — 지시가 "문서 첫머리와 각 절에 명시"를
요구했는데 초안엔 §5에만 있었다. 내용을 새로 지어낸 게 아니라 이미 문서
전체를 지배하는 전제를 각 절 서두에도 반복한 것. 판정식 무수정·게이트 우회
없음(§1)·지문 주의(§4, StaminaSystem 변경은 규칙 변경이라 42노드 지문 확인
필수)는 원문 그대로 보존.
**초안 삭제**: `md/ROSTER_DESIGN-draft.md`/`md/DESIGN_future-mechanics-draft.md`
삭제 — `git log`로 확인한 결과 애초에 커밋된 적 없는 파일이라(사용자가 이번
세션에 직접 넣어둔 미추적 파일) git 상 "삭제"로 안 잡히고 그냥 없어짐, 내용은
전부 정본 2개로 이동 완료.
**Next Up 3.5 항목을 3.5a(완료)/3.5b(다음)로 분리**하고, 3.5b에 "지도·대회
뼈대 + `ROSTER.md` 6상대 습관 저작"을 명기. §H 7단계 항목에 "숙련도 + 콤보 +
계단형 시그니처 + 변칙기술 + 체력 격화(`DESIGN_future-mechanics.md`)"를 묶어
재작성. 세이브 `linkages` 필드 예약은 [선택]으로 3.5a 항목 안에 후속 메모로
남김(지금 정하지 않음).
**검증**: `sh tools/stamp.sh --check md/*.md` exit 0(이제 5종: MATCH_RULES·
CORE_DESIGN·META_DESIGN·ROSTER·DESIGN_future-mechanics). 하니스
`dotnet run -- --check` 253/253 그린 + 지문 IDENTICAL(문서만 바꿨으므로 당연한
결과 — 재확인 목적). Unity MCP 컴파일 0/0(세션 시작 확인, 문서 전용이라
이후 재확인 불필요). `git status --short`로 의도한 파일(신규 정본 2개, 삭제된
초안 2개는 미추적이라 표시 자체가 없음, STATUS.md)만 바뀌었음을 확인.
**판단 필요 지점 (정하지 않고 목록으로)**:
- `ROSTER.md` §R9: (1) placeholder 이름 6개 교체 여부 (2) 동메달전을 3.5b에서
  스텁으로 둘지 실저작할지 (3) ⚙ 초기치(신뢰도·적응·scouting) 방향만 제안,
  실플레이 튜닝 필요.
- `DESIGN_future-mechanics.md` §6: (1) 계단 도약 지점이 급 경계(4~5)와 같은지
  (2) 변칙기술 감쇠 폭·회복 정도 (3) 콤보 위력 보정 함수 형태(로그/캡/체감)
  (4) 체력 격화 증가폭·저체력 배율 곡선 (5) `linkages` 필드를 지금 예약할지
  (위 3.5a 후속 메모와 동일 항목).
**가장 최근 (그 앞) — §H 3.5a: 세이브 계층 (데이터 기반, 스펙 구현. Core/Brain·판정 무수정).**
Core 순수성 유지 — 새 영속 시임 3개를 CORE_DESIGN "Persistence seam" 절 원안 그대로
Core 에 추가: `IMasteryStore`(기존 stub 인터페이스에 실구현 `MasteryStore` 신설, `Set`
자체는 §H 2단계부터 이미 있었음), `IAdaptationLedger`(신규, §C.2용), `IRecognitionSet`
(신규, §D 인지 목록용, CORE_DESIGN 원안대로 `Seed` 없이 `Knows/Add/Snapshot`만 —
`Add`가 멱등이라 반복 호출로 시딩 가능, 별도 API 불필요하다고 판단). 셋 다
Dictionary/HashSet 기반 **실동작** 컨테이너다 — "이번 단계는 빈 값/0 유지" 지시를
"컨테이너 자체가 영구 무동작이어야 한다"가 아니라 "MatchEngine/Brain 어디서도 아직
안 불러야 한다"는 뜻으로 해석했다(둘 다 자연히 빈 상태로 시작해 지시 문구 자체는
만족, 실제 채움은 아래 이월 항목). `ZeroMasteryStore`는 손 안 댐 — §H 2단계부터
이미 살아있는 컨슈머(MatchEngine 기본 주입)가 있어 그건 영구 무동작이 계약이고,
`MasteryStore`는 그와 별개로 §H 7단계가 실제로 쓸 새 구현체.
세이브 파일(Game 전용, Core 는 파일/JsonUtility 여전히 모름): `Game/Save/SaveData.cs`
(META_DESIGN §M1 스키마 1:1 — JsonUtility 가 Dictionary 를 못 실어서 `{techId,level}`/
`{techId,count}` 같은 `List<Entry>` 래퍼로 표현) + `SaveSystem.cs`(경로 인자 오버로드 +
`Application.persistentDataPath` 편의 오버로드, 원자적 쓰기 — 임시파일에 쓰고 대상
있으면 지우고 rename).
새 테스트 18개: `AdaptationLedgerTests`(5)/`RecognitionSetTests`(4)/`MasteryStoreTests`
(4)/`SaveSystemTests`(5 — 스키마 전 필드 개별 왕복·파일없음 시 빈 SaveData·임시파일
안 남음·덮어쓰기·폴더 자동 생성).
하니스 확장: `UnityEngineShim.cs`에 `JsonUtility.ToJson`(object / object+pretty 두
오버로드, 실제 Unity 처럼 런타임 타입으로 직렬화) + `UnityEngine.Application.
persistentDataPath`(OS 임시 폴더) 추가, `Harness.csproj`에 `Game/Save/*.cs` +
`SaveSystemTests.cs` 컴파일 대상 등록, `Program.cs` COVERAGE 문구에 SaveSystemTests
추가.
검증: Unity MCP 컴파일 0/0 — 새 타입 5개(`MasteryStore`/`AdaptationLedger`/
`RecognitionSet`/`SaveData`/`SaveSystem`)를 직접 참조·실행하는 RunCommand 로 재확인.
**세션 중 발견(제품 결함 아님, RunCommand 샌드박스 전용 제약)**: `System.IO.File.Delete`
를 RunCommand 안에서 단독 호출하면 "User interactions are not supported for MCP tool
calls" 로 막힌다 — 이분법 격리로 원인 확정(Write+Read 만 있으면 통과, 그 뒤
`File.Delete` 한 줄만 추가하면 실패, `SaveSystem.Write`가 내부적으로 하는
"기존 파일 있으면 지우고 rename"의 그 Delete 도 같은 제약을 탄다). `SaveSystem.cs`
자체는 결함이 아니다 — 같은 delete 경로가 dotnet 하니스(진짜 프로세스, RunCommand
샌드박스 아님)의 `Write_Overwrites_PreviousSave`/`Write_DoesNotLeaveTempFileBehind`
로 이미 통과 검증됐다. RunCommand 로 Play 모드 이외 파일 삭제를 확인해야 할 다음
세션을 위해 기록만 해두고 코드는 안 고침. 하니스 235→253(+18) 그린, **42노드 지문
IDENTICAL**(어느 인터페이스도 아직 MatchEngine/Brain 에 안 물렸다는 뜻 — 지시가
경고한 "물리면 지문이 깨진다"의 반증).
**이월**: 실제 채움(디코딩한 세이브를 매치에 반영)은 §H 5단계(`IRecognitionSet`)/
6단계(`IAdaptationLedger`)/7단계(`IMasteryStore`)로 넘어간다 — 3.5a 는 자리만.
**가장 최근 (그 앞) — 밸런스 ⚙ 확정값 적용: `BalanceDrainSetup` 35→40.** 밸런스 경제를
엄밀히 도출한 결과 흔들기 드레인만 조정, 나머지 6개(견제 −20/무득점 −15/
그쳐 +3/수비 +10/임계 70/최대 100)는 무수정.
**작업0**: 스탬프 exit 0, 하니스 235/235·지문 IDENTICAL, Unity MCP 컴파일
0/0 확인 후 진행.
**작업1 — 값 적용 + 도출 근거 주석.** `MatchRulesConfig.BalanceDrainSetup`
35→40. 도출 근거(지시받은 문구 그대로, `MatchRulesConfig`의 밸런스 상수
정의부에): 게이트 개방 위협 횟수 = 흔들기 1 / 견제 2 / 무득점 3(흔들기
40=2×견제20, 무득점15×3=45로 비슷한 폭에 닿음 — §4.4가 흔들기를 "밸런스를
깎는 전용 행동"이라 부르는 것과 맞는 위계). 수비 성공(+10)이 흔들기 1회를
정확히 상쇄(60+10=70). 그쳐(+3)는 그 절반도 안 돼 패시브 회복이 거의 없다
— 끈적함 의도. 이 7개 전부 대회 실플레이 재튜닝 대상이라는 문구도 그대로
남김.
**영향받은 테스트 갱신.** `_cfg.BalanceDrainSetup`을 상수로 참조하던 곳(예:
`FlagOn_WithGrip_SetupSucceeds_...`)은 무수정으로 자동 반영됨. 하드코딩된
계산값(주석·assert 메시지)이 있던 4개 테스트를 60 기준으로 갱신
(`BigThrow_AgainstDrainedOpponent_...`/`SetupDrain_Persists...`/
`OneMatte_...`) — 결론(강등 회피 유지)은 안 바뀜, 숫자만. **`TwoMattes_
RecoverEnoughToReopenTheGate`는 결론이 실제로 뒤집혔다** — 60+3+3=66 은
이제 임계(70) 밑이라 그쳐 두 번으론 게이트가 안 열린다. 네 번(60+3×4=72)으로
필요 횟수를 맞춰 `FourMattes_RecoverEnoughToReopenTheGate`로 개명 —
"패시브 회복만으론 잘 안 풀린다"는 이번 도출 의도를 오히려 더 분명히
보여주는 방향이라 판단해 삭제 대신 횟수만 재조정. 테스트 주석에 "실제
정석은 수비 성공(한 번으로 정확히 상쇄)이지 그쳐 반복이 아니다"도 남겨둠.
**작업2 — 검증.** Unity MCP 컴파일 0/0. 하니스 235/235 그린(테스트 개수
무변화 — TwoMattes→FourMattes 1:1 치환) + 42노드 지문 IDENTICAL(스크립트
경로는 `BalanceGate` off 로 이 값 변경과 무관하게 동작).
**가장 최근 (그 앞) — §H 3단계 후속: 밸런스 HUD 노출 (Game, Core·판정 무수정).**
**작업0**: 스탬프 exit 0, 하니스 235/235·지문 IDENTICAL, Unity MCP 컴파일 0/0
확인 후 진행.
**작업1 — 표시 배선.** `LiveMatchPanel.FighterStatus`(체력·그립을 이미 순수
숫자 텍스트로 내는 그 줄)에 `밸런스 {값}`을 같은 급으로 추가 — 새 바 위젯
없음, 기존 패턴 그대로. Core 읽기는 `eng.Balance.Get(who)`(3단계에서 이미
공개 프로퍼티로 나와 있어 새 게터 불필요 — "이미 있으면 재사용" 지시 그대로,
Core 무수정). **[설계 판단]** 온전 임계(70) 이상이면 `Warning`색(내 성향
경고선과 같은 색 재사용, 새 색 추가 안 함) — 이 값 이상이면 "지금 상대에게
큰기술을 던지면 강등된다"는 실전 정보라 강조할 근거가 있다고 판단, 그 외
바 형태 등 추가 시각화는 안 함(체력·그립도 순수 숫자라 같은 급 유지).
`BalanceChanged`를 `EventSummary`에도 추가(`GripChanged`/`SetupDeclared`와
같은 자리) — "이벤트를 구독해 갱신"을 상시 폴링 상태줄과는 별도로 교환
로그 쪽에서 이벤트 기반으로 만족시킨다. 상태줄 자체는 기존 체력·그립처럼
폴링 갱신 유지(매 턴 `RefreshStatusHud`가 이미 전부 다시 그림) — 별도
구독을 안 단 이유를 주석에 남김: 폴링 시점(턴 끝)엔 그 턴의 모든 밸런스
변화가 이미 `eng.Balance.Get()`에 반영돼 있어 이벤트 구독이 결과에 차이를
안 만든다, 체력·그립도 구독 없이 이 폴링만으로 정확하다.
**작업2 — Play 실측(⚙ 튜닝 근거).** CounterTime>Play Mode Setup(흔들기 on)
+ 실제 UI 클릭(`Button.onClick.Invoke()`)으로 4/5 드레인·회복 지점을 직접
관측, HUD 색·숫자 갱신도 같이 확인:
- 흔들기 선언 → 상대 −35(`BalanceDrainSetup`) 정확히 일치, HUD 100→65로
  갱신되며 임계(70) 아래로 내려가자 강조색이 즉시 사라짐(실시간 확인).
- 견제 발기술(모두걸기) 적중 → −20(`BalanceDrainFeintFootsweep`) 정확히
  일치. **B의 자체 공격으로 관측됨** — A의 기술 선택이 B의 습관 브레인에
  선점당해([v1 임시 규칙] 동시 공격 우선순위, 기존에 이미 알려진 사안) B가
  대신 포크를 냈다. 같은 코드 경로(어느 쪽이 공격자든 `Drain(defender,...)`)
  라 관측치는 동일하게 유효.
- 그쳐(강제 발동) → 양쪽 +3(`BalanceRecoverMatte`) 정확히 일치, 상한(100)
  근처에서 클램프도 확인.
- 무득점 진짜 시도 → −15(`BalanceDrainThreatNoScore`) 정확히 일치. **같은
  교환에 두 드레인이 동시에 관측됨**: A의 밭다리걸기가 노포인트로 끝나며
  B −15, 동시에 B의 습관 브레인이 흔들기를 먼저 삽입(`SetupPrecedingCommitSource`,
  큰기술 앞 자동 흔들기)해서 A −35 — 한 교환 안에서 두 자원 흐름이 독립적으로
  겹칠 수 있다는 걸 실측으로 처음 확인(⚙ 튜닝 시 참고할 사실 — 한 교환 최대
  변동폭이 지금 산정한 개별 값보다 커질 수 있다).
- **수비 성공(+10, `BalanceRecoverDefense`) 은 Play UI로 직접 못 봄** — 되치기
  성립 조건(grip열세+GotRead)을 B 습관 브레인 상대로 재현 못 함(시간 관계상
  포기). Core 단위테스트(`BalanceDrainTests.CleanDefense_RecoversDefenderBalance`)
  로는 정확히 일치 확인됨 — 같은 코드 경로라 신뢰도는 높으나 UI 종단 확인은
  아니라는 차이를 그대로 기록.
**작업 결과 요약(⚙ 튜닝 근거로 남김)**: 7개 값 중 6개(Max/Threshold 제외
5개 드레인·회복 값 중 4개)가 Play 실측과 정확히 일치, 1개(수비 성공)만
단위테스트로만 확인. 숫자 자체는 손 안 댐(제안 초기치 그대로) — 실제
게임감(밸런스가 너무 빨리/느리게 움직이는지)은 사람이 몇 판 더 돌려보고
판단할 몫.
**작업3 — 검증.** Unity MCP 컴파일 0/0. 7해상도(1920~320) 닫힌 상태
zeroSize=0/overlaps=0/offscreenLeft=0(밸런스 문구로 길어진 상태줄 포함해
재확인). 하니스 235/235 그린 + 42노드 지문 IDENTICAL(Game HUD만 바뀌었으니
당연한 결과 — 재확인 목적).
**남은 판단 대기 (정하지 않고 목록으로)**:
1. 동시 공격 우선순위([v1 임시 규칙], 기존 기록) — 이번 실측에서 다시
   걸림. 수비 성공(+10)을 UI로 관측하려면 이 규칙이 먼저 해소돼야 재현이
   쉬워진다.
2. 한 교환에 여러 밸런스 드레인이 겹칠 수 있다는 이번 발견이 ⚙ 7개 숫자
   자체를 다시 손볼 근거가 되는지 — 사람 판단.
**가장 최근 (그 앞) — §H 3단계 완료: 밸런스 축 (Core 배관, 화면 무수정).** CORE_DESIGN
v4.0 §3단계 근거. §H 2b ③에서 발견한 "포크가 이벤트 0" 공백이 여기서 정리됨.
**작업0**: 스탬프 exit 0, 하니스 221/221·지문 IDENTICAL, Unity MCP 컴파일 0/0
확인 후 진행.
**작업1 — `BalanceSystem`(Core 순수, StaminaSystem 미러).** Get/Drain(하한 0)/
RecoverDefenseSuccess/RecoverMattePartial/ResetBetweenMatches(유일한 완전
리셋, 현재 호출부 없음 — §H 3.5 세이브 계층 몫). `MatchRulesConfig`에 전부
⚙(제안 초기치, 튜닝 전) 추가 — `BalanceMax=100`·`BalanceOnsetThreshold=70`
(문서에 이미 있던 값)·`BalanceDrainSetup=35`·`BalanceDrainFeintFootsweep=20`·
`BalanceDrainThreatNoScore=15`·`BalanceRecoverDefense=10`·
`BalanceRecoverMatte=3`(StaminaSystem.MatteRecovery와 동일 크기, "같은 방식"
지시 그대로). 숫자는 서로 맞물리게 골랐다 — 흔들기 한 번(100−35=65)이 임계
70을 한 번에 넘기고, 그쳐 한 번(+3=68)으론 못 넘지만 두 번(+3+3=71)이면
다시 넘는다(아래 테스트가 이 계산을 직접 고정).
**작업2 — `BalanceGate` 재배선.** `SetupGateEnabled`(불리언 단독)를
`BalanceGate`로 개명 — 파일 이동은 없었다(필드지 타입명이 아니라 git mv
대상이 없음, 확인 후 판단). 게이트 조건을 "공격자 판정에 들어가기 직전,
**방어자**의 밸런스가 임계 이상이면 큰기술을 Sloppy로 강등"으로 교체 —
옛 [초안]의 "이 공격자가 지난 교환에 흔들기를 선언했는가"(`_setupActive`,
공격자별 1교환 플래그)에서 "방어자의 현재 밸런스 수치"(지속되는 자원, 누가
깎았는지 안 따짐)로 일반화됐다. 순서는 `ClassFromMastery` 먼저, `BalanceGate`
최후 강등, `SurpriseExempt`는 게이트 자체를 면제 — §F 문서 그대로.
`_setupActive`/`_setupDeclaredThisExchange`는 **존치**(제거 아님) — 클로드
코드 판단, 근거는 코드 주석: 게이트 판정에는 더 이상 안 쓰이지만
`SetupDeclared` 이벤트·`IsSetupActive()` HUD 읽기(`LiveMatchPanel`이 여전히
참조)가 걸려 있고, 이번 단계는 "화면 아님"이라 그쪽을 건드리려면 범위를
넘는다 — 지우면 Game 쪽도 같이 고쳐야 했다.
**작업3 — 드레인 5지점(+1 추가 발견).** 흔들기 declare(상대 드레인)·견제
발기술 적중(§D.4 floor, 상대 드레인 — **여기서 §H 2b ③의 "포크가 이벤트 0"
공백이 닫힌다**, v1엔 승격 굴림이 없어 "적중"은 곧 모든 포크)·판정 무득점
(None, IsCounter 무관 — 되치기가 fizzle 해서 None 이 나온 경우도 "진짜
시도, 무득점"의 정의를 똑같이 만족한다고 판단)·수비 성공(Clean, §5.2 정의
재사용)·그쳐(양쪽 부분 회복). **작업 지시 5개 목록에 없던 +1**: 굳히기
탈출(§9.4)도 그쳐와 같은 방식으로 부분 회복을 준다 — MATCH_RULES.md 가 이미
v4.1로 확정해둔 문장("§9.4 밸런스는 여기서도 부분 회복 ⚙만 받는다")을 그대로
따른 것이지 새 판단이 아니다(문서가 이긴다는 기존 원칙).
**세션 중 실제로 잡은 버그 — 지문 위험을 실행 전에 발견.** 드레인 호출을
전부 `_cfg.BalanceGate` 조건 없이 무조건 실행하도록 짰다면, 하니스를 돌리기
전 `tools/dotnet-harness/Program.cs`의 `Describe(MatchEvent)`를 먼저 읽어보고
알았다 — 이 함수는 인식 못 하는 이벤트 타입을 `default: e.GetType().Name`
으로 그냥 찍어버린다. 42노드 스크립트 경로엔 포크(m14 발목받치기)와 무득점
큰기술(m14 "판정 None" 3회)이 실제로 있어서, `BalanceGate` 기본값이 꺼져
있어도 드레인 호출 자체가 무조건 실행되면 새 `BalanceChanged` 줄이 지문에
그냥 끼어들 뻔했다. **하니스를 돌려서 깨진 걸 보고 고친 게 아니라, 실행 전에
읽어서 미리 막았다** — 5지점(+1) 전부에 `if (_cfg.BalanceGate)` 가드를
둘러 "꺼지면 완전 비활성"을 §4.4 흔들기 선언과 똑같은 계약으로 통일했다.
**작업4 — `MatchEvent.BalanceChanged` + 관문 테스트.** HUD 배선은 이번에
안 함(잔여 항목) — 이벤트만 낸다. 신규 테스트 3파일 22개(`BalanceSystemTests`
7 — 순수 단위, `BalanceDrainTests` 6 — 포크/무득점/수비성공 통합,
`SetupTests` 재작성 9 — 선언·게이트·그쳐 부분회복). 옛 `SetupTests`의
"1교환만 지속" 테스트는 **삭제하지 않고 반대로 뒤집어 다시 썼다** —
`SetupDrain_PersistsAcrossMultipleExchanges...`가 이제 "몇 교환이 지나도
회복이 없으면 드레인이 안 풀린다"를 고정한다(밸런스가 지속 자원이 됐으니
당연한 결과이지만, 옛 테스트를 그대로 뒀으면 거짓 초록이 아니라 실패로
잡혔을 것 — 미리 재설계). "그쳐가 지운다"던 옛 테스트도 `OneMatte_.../
TwoMattes_...`로 나눠 "부분 회복은 한 번으론 안 되고 쌓여야 한다"는 §S
확정 사실을 직접 검증하도록 바꿨다.
검증: Unity MCP 컴파일 0/0. 하니스 **235/235 그린 + 42노드 지문 IDENTICAL**
(BalanceGate 스크립트 경로 기본 off 관행 그대로 유지). `git status --short`로
의도한 파일만 바뀌었음을 확인(`Assets/_Recovery/`는 여전히 이전 세션부터
있던 것, 무관).
**가장 최근 (그 앞) — §H 2b 후속: Play 실측 3건 (레이아웃 버그 2건 수정 + 견제기 결과
로직 조사·보고).**
**1. 기술 휠 화면 왼쪽 잘림 — 진단 후 수정.** 추측 수정 금지 지시대로 실측부터.
**트리거 버튼 자신이(휠 항목이 아니라) 이미 화면 밖이었다** — 트리거 X=25,
폭 74(절반 37) 라 25−37=−12, 요약 텍스트(폭 90)는 −20. 이건 §H 2b 가 만든 문제가
아니라 2026-08-12 6차(방사형 전환) 세션부터 있던 것 — 그때는 A 겹침만 재고
화면 자체 경계는 실측 안 했다. 휠 항목 중 정확히 ±90°(각 축 처음/끝 항목 —
각도 공식이 반지름과 무관하게 항상 그 자리에 놓는다)도 오프셋이 0이라 트리거와
똑같이 잘렸다 — **반지름을 조정해도 이 항목들은 절대 안 풀린다**(오프셋이
반지름과 무관하게 늘 0). 손 축도 실측으로 같은 잘림 확인(10개 목록 포함).
수정: `TriggerX` 를 25→55 로 올려 트리거 자신·요약·±90° 휠 항목 셋 다 화면
안에 들어오게 함(각 최소 10px 이상 여유). 반지름(130/90/100/120, 임의 변경
금지)·각도 공식은 무수정 — 호를 좁히는 대안은 손 축 스테이지1(왼손/오른손
2항목)이 정확히 ±90°에 있어야 하는 "cos90°=0 이라 A 와 안 겹친다"는 §A.1
성질을 깨뜨리므로 기각. A 여유가 그만큼(30px) 줄지만(예: 기술 휠 최대 도달점
179→209) 이미 "겹침 허용+딤 처리"가 확정 원칙이라 새 문제가 아니다(손 축
자체가 이미 실측 "최대 145px" 겹침으로 승인받은 전례).
**2. 트리거 라벨↔현재값 세로 겹침 — 기하학적으로는 안 겹쳤으나 육안엔 너무
빡빡함.** 실측: 트리거 하단↔요약 상단 간격 4px(전 축 동일, 코드 주석에도
"4px 여유를 둔다"고 의도적으로 적혀 있던 값 — 버그가 아니라 너무 타이트하게
잡은 설계였다). 2b 보고에 썼던 "세로 여유 아래145/위63"은 **다른 지표**(휠
전체가 ConfirmRow/NarrationLog 경계를 안 넘는지)라 이 신고와 무관 — 혼동
가능성을 STATUS에 명시. 요약 Y 오프셋을 −32→−38 로 내려 간격을 4→10px 로
넓힘(위 축과의 간격도 재확인 — 최소 13px, 안 겹침).
**3. 견제 발기술(모두걸기) 결과가 "B 오른소매 확보"로 뜨는 이유 — 추적 후 보고만,
수정 안 함.** Play 모드에서 직접 이벤트를 찍어 확인: A 가 모두걸기(Poke)를 낸
교환에 실제로 발화한 이벤트는 **`GripChanged` 단 1개뿐**이고, 이건 A 의 포크와
무관한 **B 자신의 습관 브레인 그립 액션**이다. `MatchEngine.ResolveTechnique`
의 Poke 분기(`if (script.Technique.Cost == ActionCost.Poke) { Stamina.PayAttack(...);
return; }`)는 공격자 스태미나 차감 말고 **아무 이벤트도 안 낸다** — 그립도
안 바꾸고 판정도 안 연다. 화면에 뜬 "B 오른소매 확보"는 `LiveMatchPanel.
BuildOutcome()`의 폴백("발생한 이벤트 중 뜻이 있는 첫 번째")이, A 의 포크
자체는 보여줄 게 없어서 같은 교환의 B 그립 이벤트를 대신 보여준 것 — A 의
포크가 B 의 그립을 유발한 게 아니다. **판단: 이건 §H 3단계 전 과도기 상태다,
별개 문제 아님.** MATCH_RULES §D.4 는 견제 발기술 floor 를 "상대 밸런스
하락"으로 정의하는데, §S 밸런스 축(§H 3단계)이 아직 없어서 지금은 그 floor
효과 자체가 통째로 미배선이다 — 포크가 스태미나 비용 말고 정말 아무 것도
안 하는 게 정확히 예상되는 현재 상태(3단계가 밸런스 드레인 호출을 그 자리에
꽂으면 자연히 해소됨). A 가 실제로 포크를 쓸 수 있게 된 건 2b 가 처음이라
이 공백이 이번에 처음 눈에 띈 것뿐, 2b 가 만든 문제는 아니다(B 의 폴백
포크도 처음부터 같은 무동작이었다).
**검증**: Unity MCP 컴파일 0/0. Play 모드 실클릭 재확인(밭다리걸기 확정 →
스태미나 100→95 정확, 손·코어 잠금/해제 정상). 7해상도(1920~320) 닫힌 상태
zeroSize=0/overlaps=0/**offscreenLeft=0**(신규 지표, `PlayModeWindow.
SetCustomRenderingResolution` 직접 호출로 실측 — 리플렉션이 이 세션 샌드박스
에서 막혀 있어 직접 호출로 우회). 기술 휠 열린 상태 8항목 전부 xMin≥18(화면
안), 손 축 stage2 10항목 전부 xMin≥18 확인. 하니스 221/221 그린 + 지문
IDENTICAL(전부 프레젠테이션 레이어, Core/Brain 무수정).
**가장 최근 (그 앞) — §H 2b 완료: 기술 탭 방사형 UI (조사·확정·구현).** 배관은 2단계에서
끝나 있었다 — 이번엔 그 지정을 실제 UI에 붙였다.
**작업0**: 스탬프 exit 0, 하니스 221/221·지문 IDENTICAL, Unity MCP 컴파일 0/0 확인
후 진행.
**작업1(조사) — 실측 후 4개 질문으로 사용자 확정**: (a) 진입 구조는 처음엔 하체
2단 방사형(손 축과 같은 메커니즘)을 권장했으나 **사용자가 별도 4번째 트리거를
선택**. (b) 반지름은 실측(곡률 vs 세로 안전구간 트레이드오프 — R=130 여유 2px/
R=120 여유 12px/R=110 여유 22px) 후 **R=120 확정**. (c) 기술 턴의 손·코어
피드백은 **손·코어 트리거 비활성화(§A.2 의도를 화면에 반영)** 확정. (d) 견제기/
큰기술 구분은 **각도 절반 그룹 분리**(앞 4개 견제기·뒤 4개 큰기술, 배치만으로
구분·새 색 로직 불필요) 확정.
**후속 확인 1건** — (a)+(b)를 실제로 계산해보니 충돌이 나왔다: R=120 을 4개
트리거 모두에 지키려면 세로 안전구간(ConfirmRow 상단 y=308 / NarrationLog
상단 y=792)이 244px 폭(트리거 중심이 있어야 하는 [428,672])뿐인데, 기존
3트리거(440/550/660, 110px 간격)를 그대로 두고 4번째만 추가하면 남는 반지름이
22px 안팎이라 R=120이 안 들어간다 — **"기존 3트리거 y좌표도 같이 4등분
재배치" vs "기술 반지름을 그만큼 줄임"을 다시 물어 전자로 확정**받았다.
**작업2(APreempted 재검토) — 좁히는 쪽으로 권장, 반영.** 기존 조건
(`a.Legs==공격`만 봄)은 2단계로 공격 성립 기준이 "기술 명시 지정"으로 좁혀진
지금도 안 따라와 있어서, **지금 당장도** 오정보를 낸다 — 기술 탭이 아직 없던
2단계 완료 직후 상태에서도, 하체=공격만 고르고 B가 그 턴 기술을 내면 "B 선행 —
A 공격 불발"이 떴다(A 는 애초에 기술을 안 냈으니 불발될 공격 자체가 없었다).
조건을 `bAttacks && liveTechnique != null`(이미 있는 변수 재사용, 새 파생
아님)로 좁혀 이 오정보를 없앴다 — `LiveMatchRunner.SubmitA`.
**작업3(구현) — 확정 4건 그대로 반영.**
- `LiveAssembler.TechniqueOptions()` 신규 — A 의 8기술(§B Owner=A 전부, 견제기
  4+큰기술 4) 하드코딩 목록. `TechniqueDef` 에 Owner 필드가 없어서(CORE_DESIGN
  v4.0 이 의도적으로 뺌) 소유권은 Game 쪽 이 목록으로만 표현.
- `LiveMatchPanel`: 트리거 4개로 재배치(하체 428·손 509·기술 591·코어 672,
  110px→81px 간격, 반지름 130/90/100 은 "임의 변경 금지" 그대로 안 건드림 —
  바뀐 건 Y 좌표뿐). `_techniqueId`/`_techniqueChosen` 신설. 기술을 고르면
  `_legs=공격/_hand=None/_core=중립` 강제 + 세 확정 플래그 모두 true(확정 즉시
  가능) + `RefreshHandCoreLock()`(손·코어 트리거 `interactable=false`+`Dim`
  틴트). 하체 휠에서 직접 공격이 아닌 값을 고르면 기술 지정을 지우고 잠금 해제
  (안 지우면 "수비를 골랐는데 화면엔 기술이 남아있다"는 모순 상태가 생김).
  `RefreshStanding()`이 매 턴 기술 상태도 리셋. `Confirm()`이 `_techniqueId`를
  `SubmitA`에 그대로 전달.
- 8종 방사형 배치 순서: 모두걸기·발목받치기·나오는발차기·발뒤축걸기(견제기,
  −90°~−12.9°) → 밭다리걸기·허리후리기·안오금띄기·어깨 매치기(큰기술,
  +12.9°~+90°) — (d) 확정을 배치만으로 구현, 색 로직 추가 없음.
**작업4(검증)**: Unity MCP 컴파일 0/0. **Play 모드 실클릭**(CounterTime>Play
Mode Setup 메뉴 + `EditorApplication.isPlaying=true`, `Button.onClick.Invoke()`
직접 호출) — 기술 휠 열기(8개 정확한 그룹 순서 확인) → 모두걸기(견제기) 선택 →
손·코어 트리거 `interactable=False` + 확정 버튼 즉시 `interactable=True`+노랑
확인 → 확정 클릭 → 엔진 스태미나 100→99(Poke 비용 1, 정확) → 다음 턴 손·코어
`interactable=True`로 복원 확인. 별도 흐름: 기술 선택 후 하체 휠에서 "수비"를
직접 고르면 기술 요약이 "—"로 지워지고 손 트리거 잠금 해제되는 것도 확인. 딤
처리(휠 열림 45%/닫힘 100%)와 트리거 숨김/복원도 기술 축에서 정상 동작 확인.
**7해상도(1920~320) 닫힌 상태 4트리거 전부 zeroSize=0/overlaps=0**(`PlayModeWindow.
SetCustomRenderingResolution` 실측 — 리플렉션이 이번 세션 샌드박스에서 막혀
있어 직접 호출로 우회, 직접 호출도 되는 걸 이번에 확인). **열린 상태 실측**:
기술 휠 반지름 120 의 실제 렌더 좌표 확인 결과 세로 폭이 버튼 자체 높이
(±18px)까지 포함해 [453,729](중심 591 기준 ±138) — 안전구간(308~792) 안에
여유 있게(아래 145px/위 63px) 들어간다. **후속 정정**: 작업1 조사 때 "세로 여유
12px"라고 보고했던 계산은 버튼 자기 높이를 안 더한 것이었다 — 실제 여유는
그보다 크다(구성 자체는 안 바뀜, 계산 설명만 정정). 가로 최대 도달점은 179(캔버스
단위) — A 왼쪽 끝의 두 추정치(기존 179 vs 이번 세션 재실측 111) 중 어느 쪽이든
겹침 자체는 손 축과 이미 같은 급(딤 처리로 흡수, 새 문제 아님).
하니스 221/221 그린 + 지문 IDENTICAL(전부 프레젠테이션 레이어, Core/Brain
무수정 — `LiveAssembler.cs`만 하니스 대상이라 `TechniqueOptions()` 추가만
재확인용으로 돌았다).
**가장 최근 (그 앞) — §H 2단계 완료: 기술 레지스트리 + 라이브 공격자 통일 (Core/Game
배관, UI는 2b로 이월).** CORE_DESIGN v4.0 §2단계-A 근거. 세션 시작에 조사 지시
세션이 이미 8개 판단 대기를 전부 종결(요지: 레지스트리는 라이브 경로 전용,
스크립트 경로 무수정, OpponentAsset 무수정)했음을 확인하고 진행.
**작업1 — 기술 레지스트리(Core 순수).** `TechniqueDef`(Id/DisplayName/Algo/
Category/BaseCost/SurpriseExempt) 신설, `AlgoClass{FeintFootsweep,BigThrow}`·
`JudoCategory{Hand,Foot,Hip,Sacrifice,Variant}` 를 `Enums.cs`에 추가(후자는
교육 UI 전용 — 실측 근거 없는 최선 추정 분류, 경기 로직이 안 읽으니 엔진
리스크 0). `TechniqueRegistry`(신규)가 §B 12기술(신규 8종 포함: 모두걸기·
나오는발차기·발뒤축걸기·안오금띄기·어깨 매치기·업어치기·안뒤축감아치기·
허리 감아치기 — 이전엔 4개만 코드에 존재) 을 하드코딩, `Get(id)` 조회 +
`ClassFromMastery(level,cfg)`(0–1 Sloppy/2–3 Standard/4–5 Signature, 경계는
`MatchRulesConfig.MasteryStandardLevel`/`MasterySignatureLevel` ⚙). `IMasteryStore`
인터페이스(`LevelOf`/`Set`) + `ZeroMasteryStore`(항상 0 반환) stub 신설 —
CORE_DESIGN 영구 저장 절과 같은 시그니처. `TechniqueSpec`에서 `MasteryLevel`
필드 삭제, `Class`→`AuthoredClass`(nullable `TechniqueClass?`)로 개명: 스크립트/
습관 경로는 여전히 리터럴을 실어 보내(암묵적 `TechniqueClass→TechniqueClass?`
변환이라 15곳 넘는 기존 생성 호출부 전부 무수정 컴파일), 라이브 경로만 null 을
남겨 `MatchEngine.ResolveTechnique`가 `AuthoredClass ?? ClassFromMastery(...)`
로 파생한다(§4.4 밸런스 게이트 강등과 같은 자리 — "한 곳에서만 파생" 원칙,
`IsDefensivePosture` 교훈과 같은 이유). `MatchEngine` 생성자에 `IMasteryStore
masteryStore = null`(기본 `ZeroMasteryStore`) 추가 — 기존 17개 호출부 전부
무수정. `StaminaSystem.PayAttack`도 `spec.MasteryLevel` 대신 명시적
`int masteryLevel = 0` 인자로 바꿔(§5.4 비용 절감 공식은 그대로, 소비처만 이동)
`StaminaSystemTests.Mastery_ReducesCost`를 고쳐 살렸다. SO 약속 주석 제거.
**작업2 — 공격자 통일 + 하드코딩 제거(라이브 경로만).** `LiveMatchRunner`의
`PlaceholderATechnique`("밭다리" 고정, `TechniqueClass.Standard`) 하드코딩을
제거 — `SubmitA(TurnChoice a, string techniqueId = null)`로 시그니처 확장(기존
유일 호출부 `LiveMatchPanel.cs:302`는 무수정 컴파일, 기본값 null). 공격자 판정을
"기술을 명시적으로 지정한 커밋만 공격"으로 통일 — B 쪽은 이미 이 기준이었다
(`LastTechnique != null`만 봄), A 쪽만 `Legs==공격`에 `techniqueId≠null`(레지스트리
조회 성공까지) 조건을 더해 맞췄다. **기술 탭 UI(§H 2b)가 아직 없어 `LiveMatchPanel`
은 이 인자를 아무도 채우지 않는다** — 그래서 이 커밋 이후 실전 Play 화면에서
손 축=공격을 골라도 기술 미지정으로 공격 커밋이 안 열리는 게 지금의 실제
동작이다(알고 있는 전환기적 공백, §H 2b 가 UI 를 붙이면 해소됨 — 데이터·엔진
배선은 이미 끝나 있다). 스크립트 경로(`ScriptedAssembler`)는 정말로 무수정 —
grep 으로 재확인. §A.2 확정대로 기술 턴을 `TendencyWindow`(Core,Legs) 채널에서
제외(중립 placeholder로 안 채우고 그냥 기록 안 함) — 실제 공격 성립(`aAttacks`)
을 기준으로 삼아 "attacker 판정"과 "성향 창 제외" 가 같은 불리언 하나를
공유하게 했다(따로 재유도하면 또 하나의 `IsDefensivePosture` 류 버그가 될 수
있어서). `APreempted`(B 선행 알림) 발화 조건은 이번에 안 건드렸다 — 여전히
`a.Legs==공격`만 보고 `techniqueId` 유무는 안 본다, 의미상 재검토 여지가 있지만
2b 몫으로 남겨뒀다(판단 대기는 아니고, 그냥 이번 범위 밖으로 명시).
**작업3 — 검증.** Unity MCP 컴파일 0/0(과정에서 발견해 고친 것 2개 —
`Brain/Tests/HabitBrainTests.cs:158`가 `tech.Class`를 직접 읽고 있어서
`tech.AuthoredClass`로 교체, `CounterTime.Game.Tests.asmdef`가 `CounterTime.Brain`
을 참조 안 해서 새 테스트의 `TendencyWindow` 접근이 CS0012로 막혀 있던 것을
참조 추가로 해결). 하니스 209→**221/221 그린 + 42노드 지문 IDENTICAL**(신규
`TechniqueRegistryTests`/`MatchEngineMasteryDerivationTests` 는 Core 순수라
하니스에서 돈다). 신규 `Game/Tests/LiveMatchRunnerAttackerTests.cs`(4개)는
`LiveMatchRunner`가 MonoBehaviour라 `MatchRunnerHandActionTests`와 같은 사유로
하니스 불가 — **Unity 전용**(228 = 하니스 221 + Unity 전용 7[기존 3+신규 4],
사람이 Test Runner로 재확인 필요). Unity MCP RunCommand로 같은 로직을 직접
재현해 실측 확인은 이미 했다 — 기술 미지정 시 A 스태미나 불변, 유효 기술
지정 시 선점 안 당하면 스태미나 감소(첫 시도 100→92, 예상 5가 아니라 8인 이유를
추적해 확인: B 가 같은 교환에 H4로 A 소매를 잡아 A 의 유효그립이 −1 로 떨어져
그립 배율 1.5×가 적용된 것 — 내 변경과 무관한 기존 엔진 순서(A 그립→B 그립→
기술판정) 때문, 버그 아님), 기술 턴은 성향 창에 안 남고 보통 턴은 정상 기록.
`git status --short`로 `Assets/_Recovery/`(이전 세션부터 있던 untracked, 이번
세션과 무관, 손 안 댐) 외 전부 의도한 변경 파일임을 확인.
**가장 최근 (그 앞) — CORE_DESIGN v4 + META_DESIGN 정본 편입 (문서 전용, 코드 무수정).**
사용자가 md/에 넣어둔 초안 2종(`CORE_DESIGN_v4-draft.md`, `META_DESIGN-draft.md`)을
검토·편입. `CORE_DESIGN_MatchEngine.md`에 새 최상위 절 "v4.0 revision — technique
registry, Balance axis, persistence seam (§H steps 2–3)"을 추가 — §2단계-A(TechniqueDef/
TechniqueRegistry/AlgoClass·JudoCategory/ClassFromMastery/IMasteryStore stub, 저작
데이터·런타임 상태 분리, 스크립트 경로는 AuthoredClass로 지문 보존), §3단계
(BalanceSystem, StaminaSystem 미러 + MatchRulesConfig 밸런스 상수 전부 ⚙ +
`SetupGateEnabled`→`BalanceGate` 재배선 + `MatchEvent.BalanceChanged`), §영구 저장
(Core는 `IMasteryStore`/`IAdaptationLedger`/`IRecognitionSet` 주입·추출 인터페이스만,
감쇠는 Game 계층)만 편입 — 초안의 §2단계-B(기술 탭 UI·공격자 통일)는 덤프의 8개
판단 대기와 대조가 아직 안 끝나 **의도적으로 제외**하고 그 사실을 문서에 명기.
덤프 실측 사실(`MasteryReadReductionPerLevel` 소비자 0, 12기술 중 코드 4개뿐, SO
약속 주석은 근거 없음) 그대로 반영, ⚙ 표기는 "제안 초기치"로 계속 표시(확정값
아님). 버전 v3.2→v4.0, `sh tools/stamp.sh`로 해시 재계산(`381212e`).
`md/META_DESIGN-draft.md`를 `md/META_DESIGN.md`(신규 정본, v1.0, 1행 스탬프
`e64fc96`)로 승격 — §M0~M6(세이브 스키마 JSON 1개·슬롯 1개, 지도 3곳[유도장/집/
경기장], 연습 대회 32강 대진표[다른 매치는 시드 동전던지기로 채움]+동메달
결정전[4강 패 → 동메달전, 승 동/패 노메달], 평판 확산[직접=상대별 adaptation
누적, 간접=대회 성과에 따른 미대전 상대 scoutingLevel 소폭 상승]) 그대로.
초안 2개는 내용이 정본으로 이동해 삭제, 커밋 메시지에 "정본 편입 완료로 초안
폐기, 사용자 확인" 명시(아래 STATUS 갱신과 같은 커밋으로 진행).
검증: `sh tools/stamp.sh --check md/*.md` exit 0(5종 — 새 META_DESIGN.md 포함),
하니스 `dotnet run -- --check` **212/212 그린 + 지문 IDENTICAL**(문서만 바꿨으므로
당연한 결과 — 재확인 목적), `git config core.hooksPath`=tools/githooks 확인. 문서
전용 세션이라 Unity MCP 컴파일 확인은 스킵(CLAUDE.md 원칙 그대로).
**가장 최근 (그 앞) — §H 구현 0~1단계 완료 (CanUseConversion 버그 수정 + 손 분리 UI).**
세션 시작에 Unity MCP 컴파일 0/0 확인(지난 세션은 문서 전용이라 스킵, 이번엔
코드를 건드리기 전에 재확인 필수 지시대로 진행).
**0단계 — `GripSystem.CanUseConversion` 버그 수정.** 반대 손 조건이 아예 없었다
(`Value(who) > 0`, 즉 "내 그립이 하나라도 있으면"만 검사 — 자기 손으로 자기
그립을 틀어잡기 전환하는 게 막혀 있지 않았다, MATCH_RULES §2 "다른 손이 이미
잡고 있을 때만" 위반). 시그니처를 `CanUseConversion(FighterId who, Hand
convertingHand)`로 바꿔 반대 손(`Other(h)`)의 그립만 검사하도록 수정 — 콜사이트
2곳(`MatchEngine.cs`는 `mine.OwnHand` 그대로 전달, `LiveAssembler.cs`는 아직
UI가 손을 안 물어봐서 `Hand.Right` 고정 — 1단계에서 진짜 값으로 교체됨).
새 테스트 2개(`GripSystemTests`): 반대 손 없음→불가, 반대 손 있음→가능(같은
손 자기 그립으로는 불가인 것도 같이 고정). **지문 확인 — 안 깨졌다.** 42노드
`[m3] A Left→RightSleeve` → `[m8] A Right→TwistGrip`이 원래도 다른 손으로
잡고 다른 손으로 전환하는 올바른 패턴이라 스크립트 경로는 이 버그에 애초에
안 걸려 있었다(실측 확인, 재고정 불필요). `LiveAssemblerTests`의 기존 테스트
1개는 정확히 이 버그를 밟고 있었다(같은 손 그립으로 전환 옵션이 열리는 걸
"정상"으로 검증하던 테스트) — 의도를 살려 다른 손으로 잡도록 고치고, 반대
케이스(같은 손만 잡았을 때 전환 안 열림) 테스트를 추가했다. 209→211.
**1단계 — 손 분리 UI.** 구현 전 조사·보고 후 사용자 확정(AskUserQuestion,
실측 근거 제시): **(b) 손 트리거 1개 유지 + 2단 선택(손→타겟)**을 골랐다(트리거
2개 분리안 (a)는 기각 — 트리거가 3개→4개로 늘어 기존 검증된 220px 세로
공간이 부족해지는 문제가 있었다). 결정적 근거: 방사형 메뉴 배치 공식
(`angleDeg = -90 + idx*(180/(n-1))`)에서 항목이 정확히 2개일 때 −90°/+90°에
배치되고 그 각도의 cos 는 0이라 **반지름과 무관하게 x축 돌출이 0** —
1단계(손 고르기, 2항목)는 A 캐릭터와 거의 안 겹친다(실측 확인, WorldToViewportPoint
투영). 2단계(타겟 고르기)는 기존 손 휠과 완전히 같은 위치·반지름·겹침(이미
승인된 범위).
구현: `LiveMatchPanel`에 `_ownHand`/`_pendingOwnHand`/`_handStageTarget` 필드
추가. `RebuildAxisOptions`에 `closeOnPick` 매개변수 신설(기본 true, 손
1단계만 false — 골라도 안 닫히고 2단계로 이어짐). `BuildAxisControl`에
`onOpening` 콜백 신설(손 트리거만 사용 — 열 때마다 1단계로 리셋, 닫힌 채
남아있던 2단계 내용이 다음에 열 때 먼저 뜨는 걸 방지). `Confirm()`/
`RefreshExposure()`가 `_runner.A.Build(...)`에 `_ownHand`를 실제로 넘기도록
수정(전엔 기본값 `Hand.Right` 고정). **`LiveAssembler.HandOptions()`도
`Hand ownHand` 매개변수를 받도록 고쳤다** — 이게 이번 작업의 핵심: 전환
게이트(0단계에서 고친 반대손 검사)와 뜯기 게이트(전엔 "아무 손이나 잡혔으면"
이었는데 손이 둘로 나뉜 이상 "이 손이 잡혔으면"이어야 함, `LockState(_me,
ownHand)`로 교체)가 이제 실제로 손마다 다른 답을 낸다. 새 테스트
`HandOptions_TearOnlyOffered_ForTheSpecificHeldHand`가 이걸 직접 고정한다.
Core 규칙 자체는 무수정 — 전부 이미 손 단위로 짜여 있던 `GripSystem` API를
실제로 손 인자와 함께 부르는 배선 작업이었다. 211→212.
**검증**: Unity MCP 컴파일 0/0(경고 1개는 Unity AI Assistant 패키지 자체의
네트워크 폴링 스레드 중단 메시지 — 프로젝트 코드와 무관, 아래 MCP 불안정과
같은 맥락으로 보임). 하니스 212/212, 지문 IDENTICAL. **Play 모드 실클릭
전체 흐름 성공**: 손 트리거 열기 → 왼손/오른손 2항목 → 왼손 선택 → 타겟
5항목(그립 없는 초기 상태라 전환·뜯기·유지·흔들기 전부 안 뜸, 기대대로) →
왼소매 선택 → 휠 닫힘+트리거 복원+요약 "왼손 · 왼소매" 정확 → 코어/하체
중립 선택 → 확정 버튼 활성화 → 확정 클릭 → 판정까지 정상 진행(그립 스코어 0 —
같은 교환에 B 도 습관 브레인으로 A 의 반대쪽을 잡아서 상쇄, 기대대로).
7해상도(1920~320) 닫힌 상태 전부 zeroSize=0/겹침=0(트리거·요약 텍스트 —
이번 세션이 그 좌표를 안 건드려서 당연한 결과지만 재확인).
**세션 중 겪은 일 — Unity MCP 브리지가 여러 차례 멈췄다(무한루프 아님).**
Play 모드 진행 중 특정 조작(주로 코어 축 클릭) 직후 MCP 호출이 120초+
응답 없이 백그라운드로 넘어가는 일이 세 번 있었다. 재연결하면 풀렸고, 한
번은 지연 실행된 명령이 그사이 Play 모드가 꺼진 상태를 못 찾아
NullReferenceException 을 냈다(코드 버그 아님 — Play 모드가 바뀐 채로 오래
대기하던 명령이 뒤늦게 실행된 결과). 이후 세션 종료 시점 콘솔에서 확인한
"Network polling encountered an error: Thread was being aborted"
(Unity.AI.Toolkit.Accounts 쪽 경고)가 같은 현상의 원인으로 보인다 — 이
프로젝트 코드가 아니라 Unity AI Assistant 패키지 자체의 네트워크 폴링
스레드 문제. 여러 클릭을 한 RunCommand 안에 묶어 왕복 횟수를 줄이는 방식으로
우회해 끝까지 검증을 마쳤다.
**다음 우선순위 — §H 2~8단계 (2026-08-12 CORE_DESIGN v4/META_DESIGN 편입 세션에서
META_DESIGN §M6대로 재배치, 3.5단계 신설).** 판단 대기 2건은 앞선 2026-08-12 문서
세션에서 이미 해소됨 — 자원명 밸런스/Balance 확정 + §F↔§4.4 게이트 우선순위 확정,
"해결됨 (2026-08-12) — v4.0 판단 대기 2건" 블록 및 MATCH_RULES §S/§4.4/§8 참조.
설계는 `CORE_DESIGN_MatchEngine.md`의 "v4.0 revision" 절(2·3단계·영구 저장 인터페이스)
과 `META_DESIGN.md`(3.5단계 세이브·지도·대회 뼈대)에 확정돼 있다 — 3단계 착수 전
더 이상 사람 결정 대기 없음:
0. `CanUseConversion` 버그 수정 — **완료**
1. 손 분리 UI(§A.1) — **완료**
2. 기술 레지스트리 + 공격자 통일(§A.2–A.3, §B) — **완료** (데이터·엔진 2단계 +
   2b 기술 탭 방사형 UI). 손 축=공격을 고르는 대신 별도 "기술" 트리거(4번째,
   R=120, 견제기/큰기술 그룹 분리 배치)에서 8기술 중 하나를 고르면 그 자체로
   공격이 성립한다 — 손·코어는 자동 잠금(§A.2). 하니스 221/221+지문 IDENTICAL,
   Play 모드 실클릭 확인 완료.
3. 밸런스 축(§S) + 흔들기 게이트 재배선(§4.4) — **완료**(Core 배관 + HUD
   노출까지). BalanceSystem·`SetupGateEnabled`→`BalanceGate` 개명·5+1 드레인
   지점·`BalanceChanged` 이벤트(Core), 상태줄 밸런스 표시+임계 강조색(Game
   HUD). 하니스 235/235+지문 IDENTICAL. **⚙ 7개 중 흔들기 드레인(40)은
   위협-횟수 경제로 엄밀 도출해 확정, 나머지 6개는 여전히 튜닝 전 제안값**
   — 실제 게임감 판단은 사람이 몇 판 더 돌려보고 할 몫.
3.5a. **세이브 계층(META_DESIGN §M1)** — **완료**. `IMasteryStore`/`IAdaptationLedger`/
   `IRecognitionSet`(Core, 실동작이나 아직 아무 데도 안 물림) + `SaveData`/`SaveSystem`
   (Game, 원자적 쓰기). 하니스 253/253+지문 IDENTICAL. [선택, 후속] `DESIGN_future-
   mechanics.md` §3의 `linkages{"a→b"→레벨}` 필드를 지금 스키마에 값 없이 미리
   예약해둘지는 아직 안 정함 — 나중에 채워도 마이그레이션 없이 되는 이점 vs 지금은
   안 쓰는 필드를 미리 넣는 부담, 사람 판단.
3.5b-뼈대. **지도·대회 뼈대(META_DESIGN §M2~M4)** — **완료**(2026-08-13). 지도 3곳
   (유도장/집/경기장 → 훈련/휴식 템플릿 + 연습 대회), 32강 대진표(코인플립 진출자),
   경기 시작→LiveMatch→결과→라운드 전환→세이브까지 전부 실제 엔진 매치로 승리·
   동메달전 경로 양쪽 실측 확인. 플레이어 경로 상대는 전부 진도윤 임시 배치(작업
   지시 그대로) — 상세는 위 Current State 항목 참조.
3.5b. **로스터 6상대 습관 저작(`ROSTER.md`).** 3.5b-뼈대가 진도윤으로 임시 채워둔
   자리에 `ROSTER.md`가 정의한 6상대(32강 황도철~결승 진도윤+동메달전 오재훈, 이름
   전부 placeholder)를 [now] 마킹된 습관·주력기·변칙기·신뢰도로 실제 저작(코드
   팩토리, OpponentAsset 안 씀 — 진도윤 `Jindoyun.Create()` 패턴 확장). 3.5b-뼈대의
   대진표 표시 이름은 이미 라운드별로 정확하니, 이 단계는 그 이름 뒤에 실제로 다른
   엔진 상대(습관·주력기)를 붙이는 작업이다(`LiveMatchRunner.Build()`의 고정
   `Jindoyun.Create()` 호출을 라운드별 분기로 교체). 예측 되치기[step5]·적응[step6]·
   콤보/변칙기술 성장[step7]은 이 단계에서 안 붙는다 — 로스터 문서의 단계 마킹 그대로.
4. 판정 승격(§E)
5. 인지 목록 + 예측 되치기(§D — n30~n37 재현 테스트, 시나리오 보드 확장 필요) →
   3.5a단계의 `knownByMe` 영구화와 연결, `ROSTER.md` 4강/결승 상대의 [step5] 점등
6. B 기술 빈도 관측 + 적응(§C.2) → 3.5a단계의 `adaptation` 영구화와 연결, `ROSTER.md`
   8강~결승 상대의 [step6] 점등
7. **숙련도(§F) + 콤보·계단형 시그니처·변칙기술·체력 격화(`DESIGN_future-mechanics.md`)
   + 훈련 시스템.** §F 개별 숙련도(3.5a단계의 `mastery` 영구화와 연결)에 더해
   `DESIGN_future-mechanics.md`의 4개 축을 같이 구현: §1 계단형 성장(급 경계에서
   도약, 게이트 우회 없음 확정), §2 변칙기술(노출 전 파괴적 → `knownByMe` 등록되면
   감쇠, §C.1/§C.3과 연결), §3 콤보(연계 숙련도, 즉각 창만, §E 승격에 위력 태움
   + 체력 할인, `linkages` 신규 저장), §4 체력 격화(경합 누적 + 저체력 배율,
   StaminaSystem 변경 = 규칙 변경이라 42노드 지문 영향 반드시 확인). 판정식 자체는
   전면 무수정이 전제 — 위력은 전부 §E 승격 롤·체력 비용에 태운다. `ROSTER.md`
   §R8b가 정의한 상대별 시그니처 콤보(진도윤 업어치기→안뒤축 / 임세라 어깨매치기→
   발뒤축걸기)도 이 단계에서 점등.
8. 위장 가중(§A.4)

**가장 최근 (그 앞) — MATCH_RULES v4.0 반영 완료 (문서 전용 세션, 코드 무수정).**
`md/MATCH_RULES_revision-v4-final.md`(사용자 확정본, [결정 대기] 없음)를
`md/MATCH_RULES.md` 본문에 반영. §S 스탠스(체력·그립에 이은 제3의 자원, HUD
노출) 신설, §4.4 [초안]을 §S 기반 확정판으로 교체(강등 게이트를 "흔들기
선언"에서 "상대 스탠스가 임계 이상"으로 일반화 — 흔들기·견제 발기술·무득점
진짜 시도 전부가 게이트를 여는 경로가 됨), §A(손 분리·기술 턴·공격자 통일·
위장 기준)~§F(숙련도)를 새 섹션으로 편입, §G 이월 목록(굳히기 확장·스카우팅·
훈련 시스템·인지 알고리즘·페이크 흔들기) 명기. 버전 v3.8→v4.0, `tools/stamp.sh`
로 해시 재계산(`b3957a9`), `--check` exit 0 확인. §H 구현 순서(0~8단계, 아래)는
이 문서가 아니라 STATUS 에만 등재 — MATCH_RULES 는 "무엇"을, STATUS 는 "다음에
뭘"을 담는다는 기존 문서 역할 분리를 따름.
**반영 중 발견해 정정한 사실 1건**: 개정본 §4.4′ 원문은 게이트 플래그를
`KuzushiGateEnabled`→`StanceGate` 로 적었으나, 그 플래그는 2026-08-11 오후
세션에서 이미 `Kuzushi*`→`Setup*` 로 전면 개명되어 현재 이름은
`SetupGateEnabled` 다 — 개정본 작성자가 개명 사실을 놓친 것으로 보인다.
MATCH_RULES.md 에는 정정된 이름(`SetupGateEnabled`→`StanceGate`)으로 반영하고
이 사실을 본문에도 남겨뒀다. 이름 정정은 의미 재해석이 아니라 현재 코드와
일치시킨 것이라 판단해 사용자 확인 없이 진행 — 필요하면 되돌릴 것.
**판단 대기로 남긴 것 2건(MATCH_RULES §8에도 기록)**:
1. §4.3/§9.4에 이미 있던 "Stance" 리셋 행(그쳐/이탈 시 초기화)이 원래는 Core
   자세 선택(공격적/중립/수비적) 리셋을 뜻했는데, 신설 §S 스탠스(수치 자원)와
   이름이 겹친다 — 각주로 구분만 표시(동작 무변경). 그쳐 시 §S 스탠스가 100
   으로 완전 리셋되는지, §S 자체 회복량(⚙)만 적용되는지는 개정본에 명시가
   없다.
2. §F 숙련도가 정하는 `TechniqueClass`(레벨 기반)와 §4.4 흔들기 게이트의
   Sloppy 강등이 같은 교환·같은 큰기술에 동시에 걸릴 때 우선순위/적용 순서가
   개정본에도 이 문서에도 없다.
검증: 반영 전후 `sh tools/stamp.sh --check md/*.md` exit 0(3종 전부), 하니스
`dotnet run -- --check` 209/209 그린 + 지문 IDENTICAL(문서만 바꿨으므로 당연한
결과 — 재확인 목적), `git status --short` 로 `md/MATCH_RULES.md` 외에 다른
파일이 안 바뀌었음을 확인. Unity MCP 는 이 세션 내내 "연결 중" 상태로 도구가
안 뜸(코드를 안 건드리는 세션이라 컴파일 확인 자체가 불필요했음, CLAUDE.md의
"MCP 끊기면 파일 편집만 진행" 원칙대로 진행) — 다음 세션에서 코드를 건드리기
전에는 재확인 필요.
**다음 우선순위 — §H 구현 순서 (문서 개정 1회, 구현은 단계별. 각 단계 회귀+
지문 확인, 지문 깨지는 단계는 사유 커밋 후 baseline 1회 재고정):**
0. `CanUseConversion` 버그 수정(반대 손 조건 누락, §2 위반 — 즉시 가능, 개정과 무관)
1. 손 분리 UI 노출(§A.1)
2. 기술 레지스트리 + 기술 탭 + 공격자 통일(§A.2–A.3, §B)
3. 스탠스 축(§S) + 흔들기 게이트 재배선(§4.4)
4. 판정 승격(§E)
5. 인지 목록 + 예측 되치기(§D — n30~n37 재현 테스트, 아직 없는 보드라 시나리오
   보드 확장도 같이 필요)
6. B 기술 빈도 관측 + 적응(§C.2)
7. 숙련도(§F — 영구 저장 포맷 포함)
8. 위장 가중(§A.4 — 스탠스가 3단계에서 먼저 생겨야 하므로 마지막)
위 판단 대기 2건은 0단계 착수 전 사람이 먼저 결정하거나, 최소한 3단계(스탠스
축 구현) 전에는 결정이 필요하다(1번 항목이 3단계 구현 자체에 영향, 2번 항목은
3+4단계 구현에 영향).
**해소됨(2026-08-12, 같은 날 후속 문서 세션) — 아래 "해결됨 (2026-08-12) — v4.0
판단 대기 2건" 블록 참조.** 자원명은 "스탠스"가 아니라 **밸런스/Balance**로
확정(코드 식별자도 `Balance`, `CoreStance` 와 충돌 회피 목적), 3단계 구현 시
`SetupGateEnabled`→`BalanceGate` 개명도 같이 한다. 우선순위는 숙련도 결정 →
밸런스 게이트 최종 강등(게이트가 이긴다).

**가장 최근 (그 앞) — 선택 UI를 방사형(휠) 메뉴로 전환 (위 드롭다운이 A 를 가리고 손
목록이 화면 밖으로 잘린다는 Play 실측 신고 후속).** LoL 핑 휠/오버쿡드 감정표현/
스파이더맨 웹 기술 계열의 배치 패턴(트리거는 화면 왼쪽 끝 세로, 선택지는
오른쪽으로 호를 그리며 펼침)만 차용 — 특정 게임 에셋·아이콘 복제 없음.
구현 전 조사·보고 후 사용자 확정: 트리거는 축 이름만 표시하는 컴팩트 버튼
(74x36)으로 줄이고 현재 선택은 트리거 아래 별도 요약 텍스트로 뺐다(트리거
자체가 "손: 오른소매"급 긴 라벨이면 그 절반 폭이 승인된 반지름 여유를
깎아먹는다는 걸 구현 중 발견해 설계 변경). 옵션은 극좌표(반지름 R, -90°~+90°
균등 분배)로 배치 — 손 R=130(항목 최대 10개라 가장 완만한 곡률)/코어 R=90/
하체 R=100, 사용자가 "안② 단일호 R=130" 으로 승인. 휠은 한 번에 하나만 열리고
(`ToggleDropdown`/`OpenWheel`/`CloseAllWheels`), 열린 동안 나머지 두 축의
트리거+요약은 숨긴다(세로 110px 간격 트리거끼리 반지름 130 휠이 옆 트리거
자리까지 닿아서). 확정 게이트(`_handChosen`/`_coreChosen`/`_legsChosen`)와
제출·리셋 배선은 기존 그대로.
**열린 상태 A/B 겹침을 카메라 투영으로 정밀 실측한 결과, 승인 당시 계산("24px
여유")이 틀렸다는 게 드러났다** — 그 계산은 트리거를 폭 0인 점으로 취급했다.
실제로는 A 의 화면상 바운딩 박스가 x=47~806(1920 폭의 약 42%)로 매우 넓고
트리거(x=25)에 거의 붙어 있어서, 부채꼴의 ±90° 끝점(반지름과 무관하게 항상
x≈25 근처)조차 A 왼쪽 끝과 15px 겹치고 각도 0°(반지름 방향 그대로)는 최대
145px 겹친다 — **반지름을 얼마로 조정해도 구조적으로 피할 수 없다**(±90° 점을
클리어하려면 버튼 폭을 44px 이하로 줄여야 하는데 손 10개 항목엔 너무 빡빡함).
사용자에게 실측치와 기하학적 불가능성을 보고하고 AskUserQuestion 으로 처리
방안을 물어 **"겹침 허용 + 휠이 열린 동안 배경 딤 처리"** 선택받음(참고 사례인
LoL 핑 휠/스파이더맨 웹 기술 휠도 게임 화면 위에 그대로 겹쳐 뜬다는 점과 부합).
`ArenaBackground.color` 를 휠 열림 시 45% 밝기(0.45,0.45,0.45,1)로, 닫히면
100% 로 복원 — 반지름·각도·트리거 위치는 전부 무수정.
검증: Unity MCP 컴파일 0/0. Play 모드 실클릭 — 3축 각각 열기(딤 적용 확인)/
선택(휠 자동 닫힘+딤 해제)/닫기, 확정 0/3→3/3 시 interactable=False→True·
Idle→ShidoYellow 전환, 제출 후 다음 턴 리셋(요약 "—"/"중립"/"중립" 복귀) 전부
확인. 7개 해상도(1920~320) 닫힌 상태 전부 zeroSize=0/overlaps=0/wrapped=0 —
1024x768 에서 한 번 "IPPON 스트립 x RowA/RowB" 겹침 경고가 떴으나 고정밀
재측정 결과 -0.00003px(부동소수점 경계에서 완전히 맞닿은 것뿐, 실제 시각적
겹침 아님, 스코어보드는 이번 범위 밖이라 무수정) 로 확인해 오탐으로 처리.
**미결로 등재**: 손 축(항목 최대 10개)은 겹침이 다른 두 축보다 크다(최대
145px vs 코어/하체 최대 ~105px) — 딤 처리로 시각적 계층은 분리했지만 항목이
많을수록 A 를 더 많이 가리는 근본 구조는 그대로다. 딤 처리가 실제로 "선택
가능하다"는 인상을 주는지는 사람이 Play 로 직접 봐야 안다. 하니스 209/209,
지문 IDENTICAL. 커밋 `64ddaf3`.
**가장 최근 (그 앞) — 실전 UI 개편: 손·코어·하체 상시 3열 폐지, 캐릭터 옆 드롭다운으로
(사용자 결정, 레이아웃 변경).** 목표: 점수판 화면 중앙 하단, 확정 버튼 점수판
바로 위(3축 전부 선택 전 비활성/회색, 전부 선택 시 노란색 활성화), 손·코어·
하체는 각각 캐릭터 옆 트리거 버튼 + 클릭 시 위로 펼치는 드롭다운.
구현 전 3건 조사·보고 후 사용자 확정: **(a) 배치 기준 — 주체 기준**(손·코어·
하체 전부 A 캐릭터 옆, 상대를 노리는 손도 포함 — "조작 버튼이 한 곳에 몰려
실수 적음"). 실측 좌표(1920 기준): 하체 (475,561)/코어 (475,647)/손 (475,733).
**(b) 추적 방식 — 화면 고정 위치**(본 추적 기각). A 의 Fight Idle 1.5초(12fps
18스텝) 실측: 골반 x±5.1/y±2.0px, 머리 x±17.5/y±2.9px, 오른손 x±10.3/y±11.3px —
83ms 마다 버튼이 튀어 보일 위험이 커 정밀 포즈 추종보다 클릭 안정성을 택함.
**(c) 드롭다운 방향 — 위로 펼침**. 코어(144px)·하체(194px)는 A 자신 위로만
펼쳐져 상대(B)와 179px 여백을 사이에 두고 안 겹침(실측 확인). 손은 그립 상황에
따라 옵션이 최대 10개(≈460px)까지 늘어날 수 있어 화면 위로 넘칠 수 있다는
한계를 코드 주석에 남김 — 스크롤 등은 범위 밖, 코드로 안 고침.
구현: `LiveMatchPanel.BuildCanvas` 의 `RootLayout` 순서가 MiddleRow →
OsaekomiRow(굳히기 2지선다, 위치만 이동·배선 무수정) → ConfirmRow → ScoreboardRow
가 됐다. `BuildStandingRoot`/`BuildColumnRoot`(상시 3열)를 `BuildFloatingControls`/
`BuildAxisControl`(트리거+드롭다운)로 교체, `RebuildColumn`→`RebuildAxisOptions`로
개명(선택 시 드롭다운 자동 닫힘 추가). 확정 게이트용 `_handChosen`/`_coreChosen`/
`_legsChosen` bool 3개 추가(코어·하체는 기본값 중립이 이미 "유효한 선택"이라
필드값만으론 "실제로 골랐는지" 구분 불가 — 드롭다운에서 클릭해야 true). 드롭다운은
한 번에 하나만 열림(`ToggleDropdown`). 옛 "선택: —·중립·중립" 요약 줄은 삭제하고
각 트리거 라벨에 접었다(사용자 승인 — 재활용/라벨 반영 둘 다 허용된 선택지 중 후자).
**회귀 검사 중 발견해 사용자 승인받아 예외 처리한 것** — `NarrationLog` 가
`leftColumn` 전체(756px 높이)를 그대로 채우고 있어(지난 세션 "전체화면 배경"
작업의 결과, 이번 세션 변경 아님) A 옆 고정 좌표(y=561~773)의 트리거 버튼과
실제로 겹쳤다(회귀 검사 실측으로 처음 발견). "서술 로그는 범위 밖" 원칙에 대해
이번 한 건만 예외로 승인받아 `NarrationLog` 를 트리거 밴드 위쪽(y=792~1072,
고정 280px)에 앵커 — 아래쪽은 경기 화면이 그대로 보인다. 드롭다운이 열릴 때는
`NarrationLog` 아래쪽과 살짝 겹칠 수 있다(손 최대 221px, 코어 43px, 하체 7px,
전부 실측) — 팝오버가 배경 패널을 잠깐 덮는 것과 같은 종류로 판단해 허용, **닫힌
상태(상시 노출)만 실제 문제였고 그건 0으로 확인됨.**
검증: Unity MCP 컴파일 0/0. Play 모드 실측 — 드롭다운 열기/닫기/선택 3축 전부
동작 확인(손 5~10개 옵션 상황별 정상, 코어 3개·하체 4개 고정 정상), 확정 버튼
0/3→3/3 선택에 따라 interactable=False→True·색 Idle(회색)→ShidoYellow(노랑)
전환 확인, 확정 클릭 시 제출+상태 리셋(다음 턴 대비) 확인. 7개 해상도(1920~320)
닫힌 상태 전부 zeroSize=0·겹침=0·줄바꿈=0. 드롭다운 열린 상태는 위에 적은
NarrationLog 예상 겹침만 확인(B 나 다른 요소와는 안 겹침). 하니스 209/209,
지문 IDENTICAL(전부 프레젠테이션 전용, Core/Brain 무수정). 커밋 `f84f631`.
**가장 최근 (그 앞) — 전체화면 배경 후속 (사이드바 가독성 진단 + 카메라 위치 이동).**
사용자 Play 실측 신고 2건 대응.
**① 사이드바 가독성 — 진단 결과 구조적 결함 없음.** "코치의 조언/수 노트/내 성향
본문이 캐릭터와 겹쳐 안 읽힌다, 배경이 거의 투명해 보인다"는 신고를 실측으로
확인 — 사이드바 3박스(`CoachAdviceBox`/`NotesBox`/`TendencyBox`) 모두 이미
`PanelBg`(알파 0.92)가 적용돼 있고(다른 패널과 이미 통일된 상태였다, 이번에 새로
추가한 게 아님), sibling 순서도 `ArenaBackground`(index 0) 뒤에 `RootLayout`
(index 1)이 와서 정상, 자식 텍스트도 박스 경계를 벗어나는 오버플로 없음(전부
실측 확인), `CanvasGroup`으로 전체 알파를 낮추는 요소도 없음. 즉 "다른 패널과
같은 스크림 처리로 통일"할 게 애초에 없었다 — 코드 변경 없음. 실제 원인으로
추정: ②(카메라 이동) 전에는 B 가 RTx=192~230(사이드바 안전선 207.8 바로
근처/걸침)에 있어 사이드바 바로 뒤에서 밝고 움직이는 캐릭터가 8% 투과율로도
눈에 띄게 비쳐 보였을 가능성 — ②로 B 를 RTx=161~201(사이드바에서 충분히
떨어짐)로 옮기면서 간접적으로 완화됐을 것으로 예상되나, 육안 확인은 사람 몫.
**② 스크림 알파 — 값 변경 없음, 적용 위치만 확인.** `PanelBg`(0.16,0.16,0.18,
0.92) 는 현재 `ScoreboardRow`/`BottomInputRow`/`NarrationLog`(전체화면 배경
세션에서 추가)와 `CoachAdviceBox`/`NotesBox`/`TendencyBox`(원래부터) 총 6곳에
적용돼 있다. 값 조정은 그 상수 한 곳만 고치면 전부 같이 바뀐다 — 사용자 판단
대기, 이번 세션에서 안 건드림.
**③ 카메라 위치 — 두 파이터가 사이드바 밖에 들어오도록 이동.** 신고: 전체화면
전환 후 A는 중앙, B는 우측 사이드바 뒤로 완전히 가려짐. 실측: x=0 기준 결합
RT-x 폭은 169px(RT 폭 320 중)로 사이드바 안전폭(~208px, 사이드바 시작=화면폭의
약 65%)보다 좁아 확대·축소 없이 옆으로 이동만 하면 됐다(세로 100px 규격과
무관 — 좌우 이동은 세로에 영향 없음). x=0.6 으로 이동, 재실측: B 우측 끝
RTx=201.5(여유), A 좌측 끝 RTx=26.0(여유), 세로 A 102.1px/B 100.9px(규격
유지). 16:9(1920x1080/1280x720/960x540/320x180) 및 근사치(719x404/600x338)
확인.
**한계(코드로 안 고침, 보고만) — 4:3(1024x768) 은 이 값으로 안 풀린다.**
`EnvelopeParent`가 좌우 25%를 잘라내는 창이라 A 왼쪽이 화면 밖으로 나가고
(canvas-x=[-58,391], 왼쪽이 음수) B 도 사이드바(1024 폭 기준 665부터 시작)에
다시 걸린다(B canvas-x=[525,688], 오른쪽이 665 초과). 크롭 방식이 종횡비마다
달라 정적 카메라 하나로 모든 창 비율을 동시에 만족시킬 수 없다 — 16:9(주
타깃으로 추정)를 우선했다.
검증: Unity MCP 컴파일 0/0. 7개 해상도 재실행 — zeroSize=0·겹침=0·버튼
줄바꿈=0(구조적 검사는 4:3 포함 전부 통과 — 캐릭터 프레이밍만 별개 이슈).
하니스 209/209, 지문 IDENTICAL. 커밋 `74ad48b`.
**별건 확인 — HANDOFF 2종 상태.** `md/HANDOFF_claude_code_runner.md`/
`md/HANDOFF_claudecode_opponent-brain.md` 는 커밋 `5f65192`(2026-08-11, 이
세션 이전)에서 삭제가 이미 커밋·push 완료된 상태. 실수 삭제가 아니라 그 세션에서
사용자가 명시적으로 "삭제 확정"을 지시해 처리된 것 — 복원 불필요, 현재 상태가
맞다.
**가장 최근 (그 앞) — 경기 화면을 창 전체 배경으로 (씬 스카이박스+RT 이중 배경 정리).**
신고: 창 전체를 덮는 씬 스카이박스와 좌상 박스 안 320x180 RT가 배경 두 겹으로
보여 혼잡. `ArenaStageView`의 RawImage 를 좌상 `ArenaBox`에서 꺼내 캔버스
최하단 sibling(`ArenaBackground`)으로 옮겨 창 전체를 채우는 배경이 되게 하고
그 위에 기존 UI 패널들이 얹히는 구조로 재작성했다. RT 해상도 320x180·MSAA
off·Point 필터·12fps 스텝은 무수정 — 표시 크기만 커진다.
`ArenaBox`/`RectMask2D`는 제거했다 — 예전엔 좁은 박스 안에서 오버사이즈된
RawImage 가 옆 블록을 침범해서 필요했지만, 캔버스 전체가 이제 배경의 영역이라
침범할 이웃이 없다(화면 밖으로 나가는 부분은 렌더 대상 자체를 벗어나 마스크
없이도 안 보임). `EnvelopeParent`는 유지 — 창 비율이 16:9 가 아닌 경우(예:
1024x768=4:3) 그대로 늘리면 찌그러지므로 여전히 필요. 비워진 좌상 자리는
`NarrationLog`(좌하 상태·서술·로그)가 이어받는다 — `leftColumn`에서
`BuildArenaPlaceholder` 호출만 제거, 나머지 구조 무수정. 상단 점수판·우측
사이드바 3종·하단 입력은 위치·크기 무수정.
**가독성** — 텍스트가 배경 위에 얹히므로 점수판(`ScoreboardRow`)·하단 입력
(`BottomInputRow`)·좌하 서술 로그(`NarrationLog`) 셋 다에 스크림(반투명
어두운 판)을 얹었다. 새 색을 만들지 않고 사이드바에 이미 쓰던 `PanelBg`
(알파 0.92)를 재사용 — **판단(지시 아님)**: 알파값은 스크린샷 없이 확정할
수 없어 하나 고른 것이고, `PanelBg` 한 곳만 고치면 넷 다 같이 바뀐다. 스크림은
RT 밖 UI 레이어에만 적용 — RT 내부(경기 장면)는 무수정.
**카메라 프레이밍 재확인** — 전체화면이 되어도 "320x180 RT 안에서 캐릭터
세로 100px" 확정 규격은 RT 공간 값이라 표시 배율과 무관, 카메라는 무수정.
실측: A 96.1px/B 95.4px(Fight Idle 애니메이션 자연 변동 범위, 목표 100px
유지). 배경 크롭 실측(7개 해상도): 1920x1080/1280x720/960x540/320x180(모두
정확히 16:9) 크롭 0%, 719x404/600x338(≈16:9) 크롭 0.1~0.15%(무시 가능),
1024x768(4:3) 만 가로 25%(양쪽 12.5%씩) 크롭 — `EnvelopeParent`의 설계대로
동작.
검증: Unity MCP 컴파일 0/0. 7개 해상도 재실행 — zeroSize=0·겹침=0(의도된
전체화면 배경 `ArenaBackground`는 겹침 검사에서 이름으로 제외 — 모든 걸
덮는 게 설계 의도라 다른 패널과의 "겹침"은 버그가 아님)·버튼 줄바꿈=0.
스크림 3곳 색 실측으로 `PanelBg` 적용 확인. 하니스 209/209, 지문 IDENTICAL.
커밋 `dd56bab`.
**가장 최근 (그 앞) — 경기 화면 UI 도트화 진단·수정 + 캐릭터 확대 (사람 Play 실측 신고 2건 대응).**
신고: ① UI 텍스트까지 도트화되어 안 읽힘 ② 경기 화면(캐릭터)이 너무 작음.
**① 진단 결과 — RT 파이프라인 버그가 아니었다.** 실측으로 `LiveMatchCanvas`가
`renderMode=ScreenSpaceOverlay`/`worldCamera=null`임을 확인 — Overlay 캔버스는
구조적으로 씬 카메라를 거치지 않아 저해상도 RT 경로에 물릴 수 없다
(`ArenaStageCamera.cullingMask` 가 UI 레이어를 포함했던 것도 실측으로 무관함을
확인). 진짜 원인은 **에디터 Game View 커스텀 렌더 해상도가 지난 세션(좁은 화면
레이아웃 테스트)의 719x404 로 그대로 남아있던 것** — `CanvasScaler`(참조
1920x1080)가 이를 반영해 `scaleFactor=0.374`로 낮췄고, 전체 UI가 저해상도로
그려진 뒤 창 크기로 늘어나 보인 것(1920x1080 리셋 즉시 scaleFactor=1로 크리스프,
실측 확인). `PlayModeSetupMenu`가 실행할 때마다 1920x1080 으로 리셋하도록 고쳐
재발을 막았다.
**② 카메라 프레이밍 재실측 확정**: 거리 −6→−4.25, 하향 11° 추가(FOV45 유지) →
320x180 기준 캐릭터 세로 A 100.9px/B 99.6px, 좌우(57~232/0..320)·상하(41~143/
0..180) 클리핑 없음(`WorldToScreenPoint` 직접 실측). RawImage 는
`AspectRatioFitter` `FitInParent`→`EnvelopeParent`로 바꿔 박스를 빈틈없이
채운다(16:9 내용비율 유지, 넘치는 쪽만 크롭) — `ArenaBox`에 `RectMask2D` 추가로
오버사이즈된 RawImage 가 점수판/서술 블록 위로 넘쳐 그려지는 것을 막았다.
`ArenaBox`의 `flexibleHeight`도 1.2→4.5 로 올려 실측 높이를 173→260px 로
키웠다(`NarrationLog`는 바닥 178px 보다 여유 있는 236px 확보).
**판단(지시 아님, 코드 주석에도 남김)** — 좌상 블록 폭(1222px 실측) 대비 16:9
정박스가 되려면 386px 높이가 필요한데, `NarrationLog` 바닥(178px)을 지키는 한
최대 318px 이 한계라 완전한 무크롭·무바 동시 달성은 이 5블록 구조에서 기하학적
으로 불가능함을 실측으로 확인했다. `EnvelopeParent`(캐릭터 머리~19%·발~14%
가벼운 크롭)와 `FitInParent`(크롭 없지만 박스 폭 절반 이상 검은 띠) 중 전자가
더 나은 시각적 결과라고 판단해 골랐다 — `fitter.aspectMode` 한 줄로 쉽게
되돌릴 수 있다.
검증: Unity MCP 컴파일 0/0. 7개 해상도(1920/1280/1024/960/719/600/320) 전부
재실행 — zeroSize=0·겹침=0(`RectMask2D` 인지 겹침 계산으로 `ArenaView` 오버사이즈
오탐 제외)·버튼 줄바꿈=0, `Canvas.scaleFactor`가 각 해상도에 맞게 정상 추적됨
(더는 고정 안 됨). 하니스 209/209, 지문 IDENTICAL. 커밋 `3bc6acd`.
**가장 최근 (그 앞) — 경기 화면 저해상도 렌더 v1 (좌상 placeholder 채움, `ArenaStageView` 신규).**
좌상 "경기 화면 — 연출 준비 전" 자리를 실제 320x180 RenderTexture 로 채웠다 — 연출·모션
매핑(노드별 클립 전환)은 범위 밖, 두 파이터가 Fight Idle 로 서 있는 화면까지만. Main.unity
는 참조/추가로딩하지 않는다 — Fighter_A/B(Y Bot.fbx/Ch30_nonPBR.fbx)가 Main.unity 씬 YAML
실측으로 원본 FBX 의 PrefabInstance 일 뿐임을 확인했으므로, 프리팹화 없이 `ArenaStageView`
가 Play Mode Setup 의 빈 씬 안에서 전용 카메라+바닥+조명+파이터 둘을 처음부터 새로 짓는다.
확정 규격 그대로 구현: RT 320x180/antiAliasing=1(MSAA off), 카메라 allowMSAA=false,
RT.filterMode=Point, `animator.enabled=false` 후 1/12초마다 `animator.Update(1f/12f)` 로
12fps 스텝. `PlayModeSetupMenu` 가 FBX/컨트롤러/바닥 재질을 `SerializedObject` 로 주입
(koreanFont 와 같은 패턴). f241b7b 가 검은화면 방지용으로 추가했던 빈 "Main Camera" 는 이제
씬에 실제 3D 콘텐츠가 생기므로 `cullingMask=0` 으로 막아 화면 전체 중복 렌더를 방지 —
사람 지시가 아니라 구현 중 발견해서 판단한 것. 컴포넌트 간 Awake 순서가 보장되지 않는
문제(LiveMatchPanel 이 stage.Output 을 읽기 전에 ArenaStageView 가 먼저 지어야 함)는
`EnsureBuilt()`(멱등) 를 두고 LiveMatchPanel.Awake 에서 명시적으로 먼저 호출해 해결.
검증: Unity MCP 컴파일 0/0. Play 모드 실측 — RT 320x180/antiAliasing=1/Point 확인, 카메라
allowMSAA=False/FOV45/pos(0,1.6,-6) 확인, 두 파이터 컨트롤러 정상 할당 확인, MainCamera
cullingMask=0 확인. 12fps 스텝 간격은 1초간 13개 스텝 이벤트를 타임스탬프로 찍어 실측 —
간격이 0.083초 전후(≈1/12초)로 균일함을 확인. 하니스 209/209, 지문 IDENTICAL
(`ArenaStageView.cs`/`PlayModeSetupMenu.cs` 는 애초에 하니스 컴파일 대상 아님,
`LiveMatchPanel.cs` 변경도 프레젠테이션 전용). 다음 단계는 범위 밖으로 남겨둠 — 노드별
모션 전환·연출 배선은 아직 정적 Fight Idle 뿐이다.
**가장 최근 (그 앞) — 좁은 화면 레이아웃 붕괴 수정 (719px 스크린샷 신고).** 원인은 CanvasScaler 설정이
아니었다 — 실측 확인 결과 `uiScaleMode=ScaleWithScreenSize, referenceResolution=(1920,1080),
matchWidthOrHeight=0`(Unity 컴포넌트 기본값, 코드가 손댄 적 없음). match=0 이면 캔버스의
**겉보기 폭은 실제 화면 폭과 무관하게 항상 1920** 이다(`scaleFactor=screenWidth/1920` →
`캔버스폭 = screenWidth/scaleFactor = 1920`, 공식으로 확정 + 320~1920px 전 구간 실측으로
재확인) — 그래서 "화면이 좁아지면 레이아웃 계산이 좁아진다"는 가설은 틀렸다.
**진짜 원인은 해상도와 무관한 구조적 버그였다**: `ScoreboardRow`/`BottomInputRow`(레이아웃
그룹이 아닌 순수 슬롯)와 다수의 `MakeFlexText`/`SetFlexible` 호출이 교차축(가로 그룹의
세로, 세로 그룹의 가로)에 `preferredWidth`/`minWidth`/`flexibleWidth` 중 **아무것도** 안
줬다 — Unity 실측(2026-08-11, `LayoutElement` 필드 직접 대조)으로 확인한 규칙: 셋 다 -1(기본)
이면 그 축은 **화면 크기와 무관하게 항상 0** 으로 무너진다. 즉 이 버그는 719px 에서만이
아니라 1920px 에서도 이미 있었다 — 안 걸린 건 지난 세션의 쌍별 겹침 검사(`ox>0.5 && oy>0.5`)
가 폭 0인 요소끼리는 애초에 안 걸리는 사각지대였기 때문이다(폭 0 인 버튼 63개 중 다수가
"안 겹친다"로 통과했지만 실은 전부 안 보이는 크기였다). 손 열 버튼의 실측: `InputColumns.width=24`
(순전히 spacing, 자식 기여 0), `첫 버튼.width=0` — 720p 든 1920p 든 동일했다(직접 대조 확인).
수정: `SetFixedHeight`/`MakeButton` 이 `flexibleWidth=1` 을 기본으로 주고, 모든
`MakeFlexText`/`SetFlexible` 교차축 인자를 0→1 로, `MakeFlexText` 는 `preferredHeight=minHeight`
도 같이 준다(min 단독으로 충분한지 실측 확인 안 했으므로 안전하게 preferred+min 조합으로
맞춤 — 이 조합과 "flexible 단독"만 실측으로 동작 확인됨). 추가로 `MakeText` 에 TMP
auto-size(`enableAutoSizing`, `fontSizeMin=max(8, 크기*0.4)`) 를 전부 켜고, 버튼 라벨엔
`singleLine`(줄바꿈 금지) 도 켰다 — 요청대로 "글자 세로 쌓임" 대신 폰트 축소로 대응.
검증: `PlayModeWindow.SetCustomRenderingResolution` (에디터 공식 API, reflection 아님)로
1920×1080/1280×720/1024×768/719×404 네 해상도 전부 재현 — 63개 요소·zeroSize=0·쌍별
겹침=0·버튼 라벨 줄바꿈=0 동일하게 통과(720p 재발 방지 위해 추가로 600×338/320×180 도
같은 결과, 320px 도 구조적으로는 깨끗함). 719px 에서 실제 클릭(손/코어/하체 선택+확정)까지
재확인 — 서술·상태 텍스트 정상. **최소 지원 해상도**: match=0 특성상 레이아웃 구조(겹침·
줄바꿈)는 이론상 어떤 폭에서도 안 깨진다(실측으로 320px 까지 확인) — 그러나 가독성은 별개다,
캔버스가 항상 1920 기준으로 균등 축소되므로 폭이 줄면 모든 글자가 같이 작아진다(320px 폭에서
버튼 글자 실측 렌더 크기 ≈3px, 사실상 판독 불가). **권장 최소 폭 960px**(버튼 글자 ≈9px) —
이 숫자는 폰트 크기 계산 추정이고 스크린샷으로 육안 확인한 것은 아니다, 사람이 실제로 보고
더 낮춰도 되는지 판단할 것. Unity MCP 컴파일 0/0, 하니스 209/209 그린 · 지문 IDENTICAL
(이 세션이 건드린 `LiveMatchPanel.cs`/`LiveMatchRunner.cs` 는 애초에 `tools/dotnet-harness/Harness.csproj`
컴파일 대상이 아님 — `LiveAssembler.cs` 만 포함, 재확인).
**가장 최근 (그 앞) — 화면 전면 재설계: 3분할 구조 + 사이드바 3종(코치의 조언/수 노트/내 성향), Play 실측 완료.** 문제의식: 수싸움 게임인데 "관측 결과"(상대 습관을 읽어낸 결과)를 보여줄 자리가 화면에 없었다. `LiveMatchPanel.BuildCanvas` 를 전면 재작성 — 상단(점수판, 무수정 이관)/좌상(경기 화면 자리, 지금은 배경+안내문뿐인 placeholder)/좌하(위상·체력·그립 상태 줄 + 서술 줄 + 최근 5교환 로그)/우측(사이드바 3종)/하단(입력 3열+선택확인줄+확정버튼) 5블록. 좌표는 손으로 안 찍는다 — `VerticalLayoutGroup`/`HorizontalLayoutGroup` 이 실제로 계산하게 만들었다(지난 anchorX 반폭 산술 버그, 바로 위 항목의 재발 방지가 이 구조의 이유). `LiveMatchRunner` 에 `Ledger`/`OpponentHabits` 읽기 전용 프로퍼티 2개만 추가(기존 `Tendency`/`Config` 와 같은 패턴) — Core/Brain 무수정.
사이드바 3종: (1) **코치의 조언** — 상태 기반 v1 규칙 5종(굳히기 중/잡기 열세/상대 체력 저하/상대 흔들기/기본), 판정식 합계·심판 누산기 미참조. (2) **수 노트** — `ObservationLedger.Observations(id) >= 1` 인 습관만 행으로 노출(아직 안 본 습관은 존재 자체를 안 보여준다), 심증(1~2회)/확정(3회 이상, `<s>` 취소선 아님·강조색)/폐기됨(`<s>` 취소선, `HabitState.IsRetired`) 3단계. (3) **내 성향** — 기존 노출 막대(`RefreshExposure`, 임계값 로직 무수정) 이관 + 경고선/위험선 라벨 상시 표시로 변경. `_preemptedText` 별도 배너는 제거(서술 줄과 100% 같은 문자열의 3중 중복이라 하나만 정리, 정보 손실 없음).
검증: Unity MCP 컴파일 0/0(새 프로퍼티를 직접 참조하는 RunCommand 로 재컴파일 확인, HabitCount=5 진도윤 정상 로드). Play 모드에서 `Button.onClick.Invoke()` 로 실제 30교환 진행 — 좌표 실측(`GetWorldCorners` 쌍별 AABB, 부모-자식 포함관계 제외)으로 서기 입력 상태 63개 요소/겹침 0, 굳히기 입력 상태(수동 토글) 42개 요소/겹침 0. 값 대조: 코치 조언 폴백 문구 MATCH=True(그립열세/체력저하/흔들기 분기는 이번 세션에 자연 발생 안 해 코드 경로만 확인), 수 노트는 **심증 빼고 확정·폐기됨 둘 다 실측**(SeekGripRightSleeve 폐기됨, SeekGripLeftSleeve 확정 3회 — 화면 문자열이 `Ledger`/`HabitState` 직접 재계산과 일치), 내 성향 경고선 마크가 실제로 Dim→Warning(D9A61A) 전환하는 것도 `TendencyWindow.MajorityMatches` 독립 재계산과 대조해 확인. 하니스는 대상 아님(`tools/dotnet-harness/Harness.csproj` 에 `LiveMatchPanel.cs`/`LiveMatchRunner.cs` 자체가 원래 없음, `LiveAssembler.cs` 만 포함 — 확인 후 재실행 생략).
**가장 최근 (그 앞) — 입력 열 텍스트 겹침 수정 (Play 실측 스크린샷 신고).** 원인은 사용자가 짚은 "쿠즈시로 항목 늘어난 손 열의 행 높이가 항목 수를 반영 못 한다" 가설이 **아니었다** — 실측(GetWorldCorners, 2026-08-11) 결과 Unity `VerticalLayoutGroup` 이 이미 손 열 10개 항목을 열 높이(285.3px) 안에 맞춰 자동으로 23.4px 씩 균등 축소하고 있었고, TMP 텍스트 필요 높이(21.6px)도 그 안에 들어가 겹침이 없었다(OVERFLOWS=False, 10개 전부). 진짜 원인은 전혀 다른 곳: `BuildStandingRoot` 의 세 열 중심 anchorX(0.06/0.38/0.62) 가 열 반폭 0.14 기준으로 균등하지 않았다(중심 간격 0.32 / 0.24 — 원래부터 비대칭, 이번 세션이 만든 버그 아님) — 손-코어는 간격 0.04 로 안 겹쳤지만 **코어-하체는 −0.04(실측 −76px), 두 열의 버튼 배경이 항상 겹쳐 있었다.** 점수판 재배치는 원인이 아니다 — `StandingInput.sizeDelta.y=320` 은 git log 로 생성 이후 한 번도 안 바뀐 걸 확인. 수정: 하체 열 중심만 0.62→0.68 로 이동(코어·손·확정 버튼은 무수정) — 재실측으로 45쌍(손10+코어3+하체4+확정1) 전부 0 겹침, 간격 손-코어 76.1px/코어-하체 38.0px(이전 −76)/하체-확정 72.9px 확인. 하니스 209/209, 지문 IDENTICAL(프레젠테이션 전용 좌표 수정)
**가장 최근 (그 앞) — 골든스코어 도달 불가 수정 (53be917 후속).** `MatchClock.RegulationExpired`/`ScoreBoard.WinnerAtTimeUp()` 둘 다 격리 단위테스트 밖에는 호출부가 0개였다 — `MatchEngine.ResolveExchange` 가 교환 종료 후 시간종료를 한 번도 확인 안 했다(둘 다 그 자체로는 정확했다, CORE_DESIGN 이 이미 "MatchClock 이 RegulationExpired 를 내면 엔진이 GoldenScore 로 넘기거나 승자를 정한다"로 못박아둔 대로). 새 코드 없이 배선만: `ScoreBoard.FinishAtTimeUp()`(신규, WinnerAtTimeUp 로 결정되면 Finish, 다 비겼으면 EnterGoldenScore 호출 — 둘 다 기존 공개 API) 을 `ResolveExchange` 끝(그쳐/굳히기 진입 판정 뒤, 쿠즈시 갱신 앞)에서 `!Board.IsFinished && !Osaekomi.Active && Clock.Phase!=GoldenScore && Clock.RegulationExpired` 조건으로 호출. 새 이벤트 `GoldenScoreEntered` 1개 추가(결정 안 나고 골든스코어로 넘어갈 때만). `LiveMatchPanel.EventSummary` 에 표시 문구 한 줄 추가
의도적으로 손 안 댄 경우 — **정규시간이 만료되는 순간 이미 굳히기가 막 열려 있으면(그 교환 자체가 절반/한판을 내며 굳히기를 연 경우) 시간종료 판정을 안 한다.** MATCH_RULES §1 은 "서기 교환은 4:00 을 넘겨도 끝까지 간다"만 규정하고 "이미 진행 중인 굳히기가 정규시간을 넘겨도 계속 도는가"는 규정하지 않는다 — 애매해서 구현 안 함, `GoldenScoreTests.OsaekomiJustOpened_AtRegulationExpiry_DoesNotTriggerTimeUp_Unresolved` 가 현재 동작(그대로 굳히기 유지, 시간종료도 골든스코어도 안 걸림)을 사실로 고정해둔다. 사람 판단 필요
검증: `GoldenScoreTests.cs` 신규 7개(결정된 시간종료가 즉시 끝남 / 다 비긴 시간종료가 골든스코어로 넘어가고 시계가 무제한이 됨 / 골든스코어 중 판정 하나로 즉시 종료 — 풀 엔진 경로로 / 골든스코어 중 지도 3개도 동일 — 풀 엔진 경로로 / 틀린 답 두 개: 시간 되기 전엔 안 넘어간다, 굳히기 막 연 교환엔 안 걸린다). 202→209 그린, 지문 IDENTICAL(42노드 시나리오는 정규시간 근처도 안 간다). 스모크 6개 구성(반복/변주/현실적 × 쿠즈시 off/on) 전부 재확인 — 가장 오래 도는 것(변주 on, 50교환 캡 소진)도 최대 추정 150~165초로 240초 근처도 안 간다(50교환×고정 3초 + 굳히기 진입 1회의 이탈 틱 최대 15초). "ran out of exchanges" 는 하니스 자체의 50교환 캡이지 인게임 240초 시계와 무관 — 회귀망이 이 경로를 하나도 안 밟는다는 사실 그대로 남겨둠. Unity MCP 컴파일 확인(0/0), `ScoreBoard.FinishAtTimeUp()` 을 직접 참조하는 RunCommand 로 재컴파일 확인
**가장 최근 (그 앞) — 실전 점수판 규격으로 재배치, Play 실측 완료.** 2026-08-11 오후 2차: 시계·절반(W)·유효(Y)·지도(S/H, 카드 아이콘)·누르기 카운트를 사진 레퍼런스 그대로의 점수판 블록(A=흰 배경 위 행, B=파란 배경 아래 행, IPPON 강조 스트립, 골든스코어 표기)으로 옮겼다. 잡기·체력·위상·쿠즈시·교환 로그·선택 확인은 기존 구현 그대로 점수판 아래로만 재배치(로직 무수정). Play 모드에서 실제로 버튼을 눌러(메뉴로 씬 구성 → Play → `Button.onClick.Invoke()` 로 손/코어/하체 선택+확정 2턴 실행) 화면에 뜬 텍스트를 엔진 실측값으로 독립 재계산한 기댓값과 문자열째로 비교했다 — 타이머·양쪽 체력/그립/양손 잠김상태 전부 MATCH=True, 로그는 `docs/../play_verification.log` 및 이 세션 기록 참조. **아직 실측으로 못 본 것**: 실제 득점(W/Y 값이 0이 아닌 상태), 지도 카드 아이콘 렌더(로직상 개수만 확인, 색·표시는 미관찰), 골든스코어 표기(아래 항목 참조 —애초에 지금 아무도 안 켠다), "그쳐" 위상 플래시. 로직은 동일 코드 경로라 확률적으로 문제 없다고 보지만 시각적으로는 미확인
**골든스코어가 절대 안 켜진다 — 알려진 공백, 규칙은 안 건드림.** `ScoreBoard.EnterGoldenScore()`/`MatchClock.Phase = MatchPhase.GoldenScore` 를 호출하는 코드가 `LiveMatchRunner`/`MatchEngine` 어디에도 없다(grep 으로 확인, 2026-08-11 오후). 정규시간 만료(`Clock.RegulationExpired`) 자체를 실전 러너가 아직 아무데서도 확인 안 한다 — "Match length is still deliberately unresolved"(아래) 항목과 같은 뿌리. HUD 는 `Board.InGoldenScore || Clock.Phase==GoldenScore` 를 그대로 읽어 표시하도록 배선해놨지만, 트리거하는 코드가 없어 지금은 절대 뜰 일이 없다. 규칙/러너를 고치지 말라는 지시라 그대로 뒀다 — 트리거를 넣을지, 넣는다면 시점을 언제로 할지는 사람 판단
**누르기 카운트 20초 도달 불가 — 기존에 기록된 사실, 재확인만.** §9.3 v1 기본 드레인 정책(`PlayModeSmokeTests` 의 고정 정책, 항상 어긋남)에서는 5~15초 구간에서 대부분 굳히기가 끝나 유효/절반까지는 실전에서 종종 보이지만 한판(20초) 문턱은 구조적으로 잘 안 닿는다. 누르기 카운트 표시 자체는 국제룰 그대로(5/10/20초 경계) 구현했고 옳다 — 안 닿는 건 표시 로직이 아니라 §9.3 기본 정책 쪽 문제, 규칙은 손 안 댐
Stage 6 완료 — **가장 중요한 것: 사람이 Play 를 실제로 눌러야 한다.** 메뉴 CounterTime > Play Mode Setup (2026-08-11 아침 신설, 커밋 7325102)이 새 빈 씬 생성 + LiveMatchRunner/LiveMatchPanel 부착 + 쿠즈시 on + 한글 폰트 연결까지 자동으로 한다 — 메뉴 클릭 → Play 버튼, 이 두 번만 사람이 하면 된다. 캔버스 레이아웃·버튼 클릭·굳히기 2지선다 전환이 전부 미검증. 판단 대기 5개(docs/overnight-2026-08-11.md 맨 위) 중 #1(m18)·#2(쿠즈시 타이밍)는 아침에 해소, #3(뜯기/잡기 유지 게이트)·#4(동시 공격 우선순위)는 §2 에 "v1 임시 규칙"으로 문서화, #5(B 굳히기 고정 정책)는 여전히 미해결
쿠즈시(§4.4) 는 아직 [초안] — 사람이 검토해서 확정하거나 되돌릴 것. on 으로 실측한 결과(작업6)가 교환 수를 프로필에 따라 늘리거나 줄인다 — 밸런스 판단은 사람 몫
Stage 7 — regression + STATUS update (이 파일이 그 결과물이다 — 다음은 실제 Play 검증 이후 마무리 라운드)
Match length is still deliberately unresolved (see step 5 note above for the post-osaekomi numbers). Judgement formula, waza-ari thresholds, cost table and per-exchange seconds all stay untouched until after stage 6
잡기 HUD (패치 1.7 로 필수가 됐다) — Core 는 이미 GripChanged 에 부위를 실어 보낸다. Game 쪽에서 RenderTexture 바깥 풀해상도 레이어로 받아야 한다

해결됨 (2026-08-12) — `md/HANDOFF_claude_code_runner.md`/`md/HANDOFF_claudecode_opponent-brain.md`
두 파일이 세션 시작 시점부터 unstaged 삭제 상태였던 것을 발견해 보고했고, 사용자가 삭제를
확정(해당 단계 완료로 일회성 지시서 폐기)해 커밋했다. `md/HANDOFF_session_2026-08-11.md` §0
이 이 두 파일을 현존 문서로 전제하던 문장도 폐기 사실을 반영해 최소 수정.

해결됨 (2026-08-12) — MATCH_RULES v4.0 §8 판단 대기 2건 (문서 전용 세션, 코드·테스트 무수정)
사용자 확정 2건을 `md/MATCH_RULES.md` 에 반영, v4.0→v4.1. (1) **자원 명칭**: §S 자원을
"Stance/스탠스"가 아니라 **밸런스/Balance**로 통일(HUD 표시명 포함) — §4.3/§9.4의 Core
자세 선택 리셋은 **자세(Core)**로 구분 표기해 이름 충돌 해소. 코드 식별자도 `Balance`로
잡는다 — 기존 `CoreStance` enum(Assets/_Project/Scripts/Core/Enums.cs)과의 충돌 회피가
목적, 실제 코드 개명(`SetupGateEnabled`→`BalanceGate` 포함)은 §H 3단계에서. 그쳐/굳히기
탈출 시 밸런스는 **부분 회복 ⚙**(체력 `MatteRecovery` 와 같은 방식) 만 받고, **완전
리셋(100)은 경기 사이에만** — §S 에 명문화. (2) **게이트 우선순위**: §F 숙련도가
`TechniqueClass` 를 먼저 정하고, §4 판정식 직전 최종 단계에서 §4.4 밸런스 게이트가
검사되어 상대 밸런스가 온전 임계 이상이면 그 값을 다시 `Sloppy` 로 강등한다(게이트가
이긴다) — 단 숙련의 연속 보정(`MasteryCostReduction`/`MasteryReadReduction`)은 강등과
무관하게 항상 적용. §4.4 끝 및 §F 에 반영, §8 두 항목 모두 RESOLVED v4.1 로 마감.
검증: `sh tools/stamp.sh md/MATCH_RULES.md` 로 해시 재계산(`dd48c35`) + `--check
md/*.md` exit 0(3종), 하니스 `dotnet run -- --check` 212/212 그린 + 지문 IDENTICAL,
`git status --short` 로 `md/MATCH_RULES.md` 외 무변경 확인(코드 세션이 아니라 Unity MCP
컴파일 확인은 스킵 — CLAUDE.md 문서 전용 세션 원칙).

결정 대기 (내가 판단 안 하고 남겨둔 취향 문제) — 선택지 버튼 높이가 111px → 82px 로 줄었다. 텍스트가 넘쳐서 TextPanel 을 키우느라 ChoiceRow 에서 떼어온 결과다. 라벨(26px)에는 충분하지만 버튼이 얇아 보이면 하단 패널을 1/3보다 키우거나 타이틀 34pt / 본문 24pt 를 줄이는 쪽으로 조정해야 한다
카피 교체 목록 — `LiveMatchPanel` 의 "B 선행 — A 공격 불발" (동시 공격 시 표시, 2026-08-11 아침)은 v1 임시 시스템 문구다. `PlaceholderATechnique`("밭다리" 고정)와 같은 부류 — 진짜 카피/연출이 붙기 전까지 기계적 문구로 둔다. 2026-08-11 오후에 추가된 상태 HUD·교환 로그의 기계적 문구도 같은 부류다: "서기"/"굳히기 — {who} 누름 {n}초 (이탈 {a}/{b})", "변화 없음"(교환 로그 기본값), "{who} {부위} 확보"/"{who} 뜯기 — {결과}"/"{who} 쿠즈시 설치"/"{who} 카운터 준비 적중 — 상대 읽기 하락"(EventSummary 폴백 서술) — 전부 `LiveMatchPanel.EventSummary`/`FighterStatus` 에 있다.
2026-08-11 오후(화면 재설계) 추가분, 전부 `LiveMatchPanel` — 경기 화면 자리: "경기 화면 — 연출 준비 전". 코치의 조언 5종(`RefreshCoachAdvice`): "굳히기 중 — 버티면 점수가 오르고, 빠져나가면 다시 선다" / "잡기 열세 — 손 싸움부터 다시 잡아라" / "상대 체력이 떨어졌다 — 밀어붙일 타이밍" / "상대가 흔들기를 걸었다 — 큰 기술을 조심해라" / "그립을 먼저 잡고 흐름을 만들어라"(기본값). 수 노트 습관 라벨 5종(`HabitLabel`): "업어치기 (그립 우세)" / "안뒤축감아치기 (지도 직후)" / "극단적 방어 (체력 열세)" / "오른소매 노림" / "왼소매 노림", 단계 문구 "심증 (N회)"/"확정 (N회)"/"폐기됨", 빈 상태 "아직 관측된 습관 없음". 내 성향 라벨: "경고선 — 최근 6수 중 4수 이상 같음" / "위험선 — 3연속 완전동일"
Play 로 시나리오를 실제로 굴려보며 매핑을 다듬어라. 매핑은 title/content 만 읽고 정한 초안이라, 클립이 상황에 맞는지는 사람 눈으로 봐야 안다 — 특히 2026-08-07 에 AI 클립으로 교체한 19개 노드는 아직 Play 확인 전이다
(Separate, not urgent) Finish the Judo match-1 scenario on the scenario board — three things the user still needs to answer: how the right-sleeve grip exchange actually plays out / timing of the collar-grip transition / the tells that distinguish an ippon-seoi-nage attempt from an ouchi-gari attempt

알려진 공백 (인지하고 남겨둔 것 — 버그 아님)
n10 허리후리기 = ai_osotogari_entry 임시 대체. 기술 상이(하라이고시 ≠ 오소토가리), 모캡 대상
n27/n29/n39/n40 = NodeMotionMap 미등록

Resolved Questions (2026-08-03)
Why Assets/Editor/HubForceResolve.cs disappeared — nobody deleted it. It is Unity Hub's bootstrap script that calls Client.Resolve() and then removes itself and the empty Assets/Editor folder via AssetDatabase.DeleteAsset. The three deleted files match CleanupEmptyEditorFolder() exactly. Not a real incident, not resume material.
Whether to keep the Unity Cloud link (cloudProjectId / organizationId in ProjectSettings.asset) — keep and commit. cloudEnabled is 0, it is a project GUID rather than a secret, ProjectSettings.asset is tracked anyway, and the editor rewrites it if removed.

Session Log (newest first)
2026-08-13 (밤, v3 후속 측정 — 승률 붕괴 원인 분리 + 로스터 차별성 재측정 + 그립 파리티 동역학)
작업 지시: 이날 앞선 두 야간 수집이 남긴 두 미해결 질문을 후속 측정. (1) 재수집
headline("A 승률 붕괴")이 같은 커밋에서 동시에 일어난 B 브레인 버그 수정과
AutoAPlayer 안전장치 추가가 뒤섞인 결과라 원인 분리가 안 됐던 것을 실제 커밋
3개(ec1cd8e 계열/611e1e2/a3fb534)로 갈라 분리. (2) 로스터 6명 차별성을
지금까지 전부 A 승률(Auto-A 휴리스틱 품질에 오염된 지표)로만 봤던 것을 B
자신의 행동 분포로 재측정. (3, 덤) 그립 파리티(양쪽 EffectiveGrip==0)가 실제
정체 구간인지 정상 결정 지점인지 직접 계측. 세 작업 모두 측정 전용 — 튜닝·
설계 변경 금지, `AutoAPlayer.cs`도 이번엔 바이트 단위로 무수정 유지(지난 두
세션과 달리 이번엔 크래시가 나도 그 파일은 못 고치는 제약).
세션 시작 확인: `git status --short`(untracked `Assets/_Recovery/`만, 이전
세션부터 있던 것 무관), `git log --oneline -5`로 HEAD `a3fb534` 확인, STATUS.md
최신 항목을 디스크에서 직접 재확인(시스템 프롬프트 요약이 아니라).
작업1 — 기존 하니스 구조 조사(`MatchResult.cs`/`MatchSimulator.cs`/
`AutoAPlayer.cs`/`OvernightRunner.cs`/`Program.cs` 통독) + Brain 쪽
`HabitDecision`/`HabitId`/`IBrainContext`/`SetupPrecedingCommitSource`/
`HabitBrainCommitSource` 통독해 습관 발화·선언 유형·그립 표적을 어디서 읽을
수 있는지 확인. `MatchResult.cs`에 신규 필드(습관별 발화 카운트 dict, 흔들기/
견제/큰기술/기타 선언 카운트, 카운터준비 카운트, 그립 표적 카운트 dict,
그립 파리티 스트릭 통계 필드 일습) 추가 — 작업 지시가 "MatchResult 확장을
허용"이라고 명시해서 이 경로를 택함(완전히 별도 시뮬레이터를 새로 짜는 대신
검증된 기존 배선을 재사용 — 중복·불일치 위험 최소화 판단). `MatchSimulator.cs`
에는 그 필드들을 채우는 순수 추가 라인만 삽입(기존 로직·순서·값 무변경) —
습관 발화(`brainDecision.Fired`)/선언 유형(B 커밋의 `Hand`/기술 `Cost`)/
그립 표적(`IsGripTarget` 신규 헬퍼)/그립 파리티 스트릭(교환 진입 전 양쪽
`EffectiveGrip` 읽어 스트릭 갱신, 종료 사유를 A선언/B선언/양쪽동시/그립액션/
경기끝 5가지로 분류, 흔들기·기술 선언 직후 3교환 창 안 득점 여부를 대기
큐(`pendingParityWindows`)로 추적). 빌드 확인 후 `dotnet run -- --check` 로
257/257 그린 + 지문 IDENTICAL 즉시 재확인(순수 추가라는 주장을 실행으로
검증).
작업2 — `OvernightV3Runner.cs` 신규(`--overnight-v3` 진입점, 기존
`OvernightRunner.cs`/`--overnight`는 무수정) — 작업1·작업3(로스터 차별성 +
그립 파리티)을 현재 HEAD 로 6×500 실행하고 §1/§3 절 본문을 만든다. 6×500=3000
경기가 0.2~0.3초에 끝나(하니스가 순수 C# 이라 매우 빠름) N=500 을 낮출 필요가
전혀 없었다. 행동 벡터(교환당 비율로 정규화 — 상대마다 평균 경기 길이가 달라
원시 카운트 비교는 부적절 판단)를 만들어 15쌍 유클리드 거리 산출, 메트릭
선택 근거(대칭·소수축 큰 차이 반영, "정답"이라 주장 안 함)를 문서에 명기.
작업3 — Task2(승률 붕괴 원인 분리)는 git worktree 오케스트레이션이 필요해
C# 서브프로세스 스폰보다 셸에서 직접 하는 게 안전·투명하다고 판단해 범위
밖으로 분리. `git worktree add <scratch>/wt-611e1e2 611e1e2` 로 읽기전용
체크아웃 → `Library/PackageCache`(git 미추적 Unity 빌드 산출물, nunit.framework.dll
필요) 를 메인 트리에서 파일 복사만(워크트리 안 git 추적 파일은 전혀 편집 안
함) → 그 커밋에 이미 있던 `dotnet run -- --overnight` 그대로 실행(무수정
재현) → 결과 문서(워크트리 안의 `docs/overnight-2026-08-13-recollect.md`,
611e1e2 시점엔 그 경로가 이미 그 이름이었다 — 최종 확정은 아님, 6-08-13 세션의
중간 산출물) 를 읽어 필요한 수치(승률/평균교환수/종료사유/안전상한 도달)만
추출 → `git worktree remove` 로 정리. **이 실행에서 실제로 서도현전 평균
1615.5교환(최대 3000, 안전상한 도달 205/500)이라는 심각한 그립 교착이
나왔다** — STATUS 가 서술해온 "서도현전 seed=5, 2994/3000턴" 현상이 이
커밋(브레인 버그 수정 직후, AutoA 안전장치 적용 전) 시점에 로스터 전체
규모로 재현됨을 처음으로 실측 확인.
작업4 — 콤보1(원본 문서)/콤보2(위 워크트리 실행)/콤보3(재수집 문서) 3콤보
비교. 4번째 조합(new AutoA×old B)은 실제 커밋으로 존재한 적 없어 워크트리로
만들려면 611e1e2 의 Brain/Core 를 체크아웃한 채 harness 파일만 손으로
바꿔치기해야 하는데, 그건 "워크트리는 읽기전용" 제약을 어기는 편집이라
판단해 구성하지 않고 "격리 불가" 로 명시(작업 지시가 명시적으로 허용한 경로).
평균 승률 계산에서 최초 산술 실수(콤보1 평균을 95.7%로 잘못 계산)를 `awk`
직접 재계산으로 발견·수정(99.1%가 맞음) — 이후 관련 낙폭·비율 문장 전체를
재계산값으로 다시 맞춤. 종료사유 표에서도 "3rd shido"와 "golden score 계"
두 열이 겹친다(golden score 3rd shido 131건이 양쪽에 들어감)는 걸 발견해
표 아래에 명시.
작업5 — `docs/overnight-2026-08-13-v3.md` 작성(§1 로스터 차별성/§2 승률 붕괴
원인 분리/§3 그립 파리티 동역학/§4 관찰·튜닝 후보/§5 검증). C# 이 만든 §1/§3
본문(`docs/overnight-2026-08-13-v3-body.md`, 중간 산출물)을 손으로 §2(워크트리
실행 결과)와 합쳐 최종본으로 완성 후 중간 산출물 파일은 삭제.
검증: `dotnet run -- --check` 257/257 그린 + 42노드 지문 IDENTICAL(최종
재확인). `git diff a3fb534 -- tools/dotnet-harness/Overnight/AutoAPlayer.cs`
빈 출력으로 무변경 확인. `sh tools/stamp.sh --check md/*.md` exit 0(스탬프
대상 5종 문서는 이 세션이 하나도 안 건드림 — `docs/*.md`는 스탬프 대상 아님).
`git worktree list` 로 워크트리가 깨끗이 정리됐음을 확인(메인 트리만 남음).
`git status --short` 로 이 세션이 `tools/dotnet-harness/Overnight/`(신규
`OvernightV3Runner.cs` + 기존 3파일 추가 라인만)·`Program.cs`·`Harness.csproj`·
`docs/overnight-2026-08-13-v3.md`·`md/STATUS.md` 외에 아무것도 안 건드렸음을
확인 — Core/Brain/Game/`MatchRulesConfig` ⚙ 무수정.
커밋·푸시는 작업 지시가 "완료 시 커밋·푸시"를 사전 승인.
2026-08-13 (밤, 후속 — 버그 3건 수정 + AutoAPlayer 그립 교착 해소 + 재수집)
전날 밤 데이터 수집 세션이 §6에 남긴 관찰 3건(②B 폴백 그립 한손 고정/④B 게이트 무시
/⑤변칙기 흔들기 예고)을 사용자가 검토 후 지시. ②의 이력(git blame)과 ④의 방향("B도
게이트 존중")을 먼저 확인·결정받은 뒤 진행.
작업1 — 3버그 수정(Brain 프로덕션): `HabitBrainCommitSource.ActionFor`(그립 좌우
번갈기)/`SetupPrecedingCommitSource`(SurpriseExempt 확인 + 게이트 재확인 후 큰기술
방출)/`IBrainContext.OpponentBalance` 신규. 하니스 257/257+지문 IDENTICAL. 커밋
`611e1e2`, 즉시 푸시(교착 문제와 독립적이라 먼저 확정).
작업2 — 재수집 1차 시도에서 그립 표적만 바꾼 게 완전한 no-op임을 발견(수치가 원본과
토씨 하나 안 다름, 클린 리빌드로도 재현) → `TurnChoice.OwnHand` 미지정이 원인임을
`AutoAPlayer.cs`의 기존 수정과 대조해 확인 → 재수정 → 이번엔 실제로 바뀌었으나
서도현전 seed=5 가 3000턴 중 2994턴 그립다지기에 갇히는 새 교착 발견(임시 진단 코드로
확인 후 되돌림) → 원인: `AutoAPlayer`의 그립 종료조건이 "B는 한손 고정이라 못 이긴다"는
전제 위에 있었는데 방금 그 전제를 없앤 것. 사용자에게 AskUserQuestion 으로 처리방향
질문("원인만 더 집어서 보고") → git blame + 임시 진단으로 정확한 인과관계 확인 후
보고, 판단은 안 하고 사용자 지시 대기.
작업3 — 사용자가 3중 안전장치(인내 한계 6턴/파리티 커밋/하드 상한 50턴) 방향을 직접
승인 지시. `AutoAPlayer.cs`에 구현(게임 코드 무관, harness 전용). 재수집 재실행 —
안전상한 0/그립교착의심 0/브래킷크래시 0. `docs/overnight-2026-08-13-recollect.md`
작성 — 원본 대비 대조 서술로 재작성(원본 버그 서술을 그대로 재실행하면 자기모순 문서가
된다는 걸 발견해 프롤로그·§1·§3·§5·§6을 "수정 완료" 서술로 교체, 출력 경로도 별도
파일로 분리해 원본 보존). 작업3이 요구한 그립파리티 B행동 계측(`BTurnsAtGripParity`/
`BIdleTurnsAtGripParity`)과 교착의심 집계(`A_SuspectedGripDeadlock`)를 `MatchResult`/
`MatchSimulator`/`OvernightRunner`에 추가 반영.
**세션 중 발견 — 승률 붕괴.** 재수집 A 승률이 원본(98.6~99.8%) 대비 0.0~21.8%로
폭락, 대진표 500회 전부 Lost. 원인 미분리(AutoAPlayer 전체 개편과 B 양손화가 동시에
일어나 이 데이터만으론 기여도 분해 불가) — 판단 없이 §6에 사실만 기록.
검증: 하니스 257/257+지문 IDENTICAL(전 단계 재확인, AutoAPlayer.cs는 애초에 비테스트
툴링이라 지문에 안 걸림). `git status --short`로 이번 세션이 harness Overnight/ 4파일
+ STATUS.md + 재수집 리포트만 건드렸음을 확인(Core/Brain/⚙ 무수정). Unity MCP는 이
세션 내내 도구가 안 잡혀 컴파일 재확인 불가 — 다음 Unity 세션에서 필요.
커밋·푸시는 이 STATUS 갱신과 같은 체크포인트로 진행.
2026-08-13 (밤, 무인 데이터 수집 세션 — 튜닝 데이터, 설계·⚙ 변경 금지)
작업 지시: §H 3.5 완료 후 첫 실측 튜닝 데이터를 헤드리스 하니스로 수집(Unity Play 아님,
느려서). 자동-A 휴리스틱 구축→행동 실측으로 허수아비 여부 먼저 검증→50교환 상한 제거→
6상대×N판 대량 수집→대진표 견고성→`docs/overnight-2026-08-13.md` 보고. 엔진·규칙·⚙
일절 무수정, 크래시 버그만 하니스 쪽에서 수정 허용.
백그라운드 에이전트로 위임해 무인 실행. 완료 후 "600초 무진전, 스트림 워치독 미복구"로
`failed` 상태 알림을 받았으나, 실제로는 작업이 끝난 뒤 git commit 까지는 성공하고 push
직전/중에 멈춘 것으로 드러남(아래 검증 참조) — "failed" 알림이 실제 작업 실패를 뜻하지
않을 수 있다는 사례로 기록.
검증(에이전트 자체 보고를 그대로 신뢰하지 않고 이 세션이 독립 재확인): `git log`로 로컬이
origin 보다 커밋 2개(`ec1cd8e`/`2828b85`) 앞서 있음을 확인(푸시 안 됨) → `cd tools/
dotnet-harness && dotnet run -- --check` 직접 재실행으로 **257/257 그린 + 42노드 지문
IDENTICAL** 확인(에이전트가 자기 보고에 남긴 수치와 일치, 독립 검증 완료) → `docs/
overnight-2026-08-13.md`(230줄) 통독 — §1 Auto-A 행동 특성화(자가검증 위반 0건)부터
§6 관찰 7개 항목까지 지시받은 구조 그대로, 자기 한계(그립 지배로 문준배/오재훈 통계상
비구별)를 숨기지 않고 명시한 걸 확인. `sh tools/stamp.sh --check md/*.md` exit 0(이번
세션은 스탬프 문서 무수정).
상세 데이터·발견 사항은 위 Current State 항목 요약 및 `docs/overnight-2026-08-13.md`
원문 참조. STATUS.md 갱신(이 항목 + Current State) 후 커밋 2개를 origin/feat/match-engine
에 푸시(사용자의 원 지시 "커밋 체크포인트 + 푸시"가 사전 승인).
2026-08-13 (§H 구현 세션 — 3.5b-로스터: 구별되는 상대 6명 저작)
작업 지시: ROSTER.md 근거로 3.5b-뼈대의 진도윤 임시 배치를 6명으로 교체. 이번엔
습관만(콤보·예측 되치기·적응·평판 확산 이월). 코드 팩토리 저작, 경기 엔진 무수정.
작업0 — 스탬프 --check exit 0(6종), 하니스 253/253·지문 IDENTICAL, Unity MCP 컴파일
0/0 확인 후 진행.
작업1(습관 저작) — 기존 `HabitCatalogue`/`HabitId` 구조를 실측: 5개 기존 습관 전부
진도윤 전용으로 기술명이 하드코딩돼 있어(§ "업어치기" 등), 새 상대마다 새 `HabitId`
값 + `HabitCatalogue.Build` case 가 필요하다고 판단(9개 추가, 5→14). 각 습관을
ROSTER 조건 문장에서 그대로 옮겼다 — 대부분 기존 `IHabitContext`(그립·체력·지도
타이밍)로 표현됐지만 둘은 안 됐다:
  - 서도현 H1 "내가 큰 기술을 예고하면"을 문자 그대로 읽으면 동시 턴 커밋 시스템에서
    독심이 되므로, §4.4 흔들기(Setup)의 실제 게임 상태(인과상 이미 지나간 사실)로
    재해석 — `IHabitContext.OpponentSetupActive` 신규(`MatchEngine.IsSetupActive`
    그대로 읽음).
  - 서도현 H2 "밸런스 온전 시 방어" + 임준철 변칙기 "밸런스가 낮을 때"는 `OwnBalance`
    신규(`MatchEngine.Balance.Get` 그대로 읽음).
  둘 다 `IHabitContext.cs` 자체 문서의 "새 습관이 실제로 요구할 때만" 규약을 따라
  추가하고, 그 사실을 관문 테스트(`LiveReadResolverTests.
  HabitConditionsNeverSeeTheBrainContext_OnlyIHabitContext`)에 반영해 "보고"를
  실제로 diff 에 남겼다.
작업2("두 갈래를 번갈아") — 임준철(어깨매치기/발뒤축걸기)과 오재훈(허리후리기/
밭다리걸기) 둘 다 ROSTER 가 "번갈아"를 요구. 정확한 매 순간 50/50 무작위는
`HabitAction`/`HabitLadder`(공유 Brain 인프라) 확장이 필요해 이번 세션 범위 밖으로
판단, 대신 같은 조건에 쿨다운 2를 나란히 준 두 습관으로 근사(쿨다운 계산을 손으로
검증: 2 면 정확히 한 턴 걸러 서로를 밀어내 A-B-A-B 가 나온다) — 단위테스트
(`ImJunchul_AlternatesShoulderAndHeelHook_WhenGripStaysHigh`)로 직접 고정.
작업3(변칙기 실배선, 계획에 없던 발견) — ROSTER/작업 지시가 "말아업어치기와 같은
방식"이라고 전제했지만, 실측 결과 그 "하드 규칙"이 애초에 어디에도 구현돼 있지
않았다(테스트 주석·설계 문서에만 존재, `HabitBrainCommitSource` 어디에도 트리거
코드 없음). 임준철 안오금띄기를 실제로 발화시키려면 진도윤 것도 같이 만들어야
했다 — `SurpriseTechnique.cs`(신규, techniqueId+조건) + `OpponentDefinition.Surprise`
+ `HabitState.TryFireSurprise`(경기당 1회, HabitId 가 없어 기존 쿨다운 딕셔너리와
분리) + `HabitBrainCommitSource.TryNext` 가 사다리를 그대로 돌린 뒤(턴 계수 보존)
변칙기 발화 시 결과를 덮어쓰고 `LastDecision` 을 비운다(관측 원장에 절대 안
남도록). 두 조건이 같은 턴에 겹치면 버려진 습관의 쿨다운이 이미 시작된 채로 남는
부작용을 문서화하고 받아들였다(HabitLadder 계약을 안 건드리는 대가).
작업4(결승 상향) — ROSTER §R5 그대로: 진도윤 신뢰도 0.75→0.65(전 습관), scoutingLevel
0→1(효과는 여전히 inert, 3.5a 관행). 영향받은 테스트 3개(`HabitFiresAtSeventyFive
Percent_NotAlways`→`HabitFiresBelowThreshold_NotAlways`, `Jindoyun_ShipsAtScoutingLe
velZero`→`...LevelOne`, `TendencyWindowTests.ScoutingLevel0_StartsEmpty`→
`TendencyWindow_StartsEmpty_RegardlessOfShippedScoutingLevel`)를 실제 값을 따라
갱신 — 결론(1.0 이 아니다/inert 하다)은 안 바뀜, 숫자만.
작업5(대진표 배선) — `LiveMatchRunner._opponentFactory`(기본 `Jindoyun.Create`)+
`SetOpponent` 최소 추가, `MetaFlowController.OpponentFactoryFor(round)` 가
라운드→Brain 팩토리 매핑. `TournamentBracket`은 Brain 어셈블리를 안 늘리려고
이름/세이브id 매핑만 갖고(신규 `RoundOpponentId`), 엔진 팩토리 매핑은
`MetaFlowController`(이미 `LiveMatchRunner` 와 얽혀 있음)에 뒀다. `ConfirmMatchResult`
의 상대 기록도 하드코딩 진도윤 상수 대신 `TournamentBracket.PlayerOpponentId/
Name(round)`로 교체.
작업6(문서 동기화) — 작업 지시의 8강/4강 이름(서도현/임준철)이 ROSTER.md 초안의
placeholder(서강윤/임세라)와 달라서, 코드와 문서가 어긋나지 않도록 ROSTER.md 도
같이 갱신(v1.0→v1.1, `sh tools/stamp.sh md/ROSTER.md` 로 해시 재계산) — §R9 확인
필요 4건 중 3건(이름/동메달전 스텁 여부/⚙ 초기치)을 해소로 갱신.
작업7(검증) — Unity MCP 컴파일 0/0(신규 파일 6개 포함 재확인). 하니스 253→257/257
그린(신규 단위테스트 4개, 갱신 3개) + 42노드 지문 IDENTICAL. Play 실측: LiveMatch
GameObject 를 직접 조작(SetOpponent+Rebuild)해 6상대 각 40교환 스모크 — 이름 일치
6/6, 공격 빈도가 확연히 다름(황도철 judgements=3·B체력 100→10 최다 공격/문준배·
서도현 judgements=0 최소 공격/진도윤 judgements=5 최다). 변칙기 두 종을 실제
문턱(체력<30, 밸런스<50) 너머까지 몰아붙여도(80교환/77교환) 에러 없이 정상
종료함을 확인 — B 의 구체적 기술 선택 자체는 설계상 공개 API로 안 새므로(의도된
정보 은닉) 조건 로직 정확성은 단위테스트가, 파이프라인 무결성은 이 스모크가 각각
담당한다고 판단.
**세션 중 발견 — 지난 세션 "RunCommand 헬퍼 메서드 추가 시 NRE" 결론 재검토 필요.**
이번 세션도 초반에 같은 NRE 를 겪었으나, 헬�퍼 메서드를 지워도 여전히 났고 원인이
사실 `GameObject.Find` 가 비활성 오브젝트를 못 찾는 것(LiveMatch 가 비활성 상태였음)
이었음을 확인 — `SceneManager.GetActiveScene().GetRootGameObjects()` 순회로 해결.
지난 세션 STATUS 기록("헬퍼 메서드가 원인")이 틀렸을 가능성을 남겨둔다 — 이번
세션은 재현 조건이 달라 확정적으로 반증하진 못했다(다음 세션에서 필요하면 재확인).
`git status --short`로 의도한 파일(Brain 6개 신규+다수 수정, Game Meta 2개 무수정,
md 2개)만 바뀌었음을 확인(`Assets/_Recovery/`는 이전 세션부터 있던 untracked,
무관). 커밋·푸시는 사용자 확인 후 진행.
2026-08-13 (§H 구현 세션 — 3.5b-뼈대: 지도·연습 대회·경기 흐름)
작업 지시: META_DESIGN §M2~M4 근거로 대회 흐름을 세우고, 구별 6명 저작 전에 진도윤을
플레이어 경로 모든 매치에 임시 배치해 흐름을 검증. 경기 엔진 무수정, 새 코드는 Game
메타 계층.
작업0 — 스탬프 --check exit 0(6종), 하니스 253/253·지문 IDENTICAL, Unity MCP 컴파일
0/0 확인 후 진행.
작업1(지도) — `MetaFlowController.cs` 신설(런타임 캔버스, `LiveMatchPanel` 과 같은
"씬 무수정, 코드로 짓는다" 패턴). 지도 화면(유도장/집/경기장 3버튼) + 훈련/휴식
템플릿(제목+지도 복귀 버튼, 내용 없음) 구현.
작업2(대회) — `TournamentBracket.cs` 신설(순수 로직). 32강 그리드(플레이어+ROSTER
32강 상대 황도철+필러 30명 시드 셔플), 필러끼리 매치는 코인플립으로 진출자만 계산
(경기 시뮬 없음). "경기 시작" 버튼을 상대 이름과 내 이름 사이 공간에 배치(작업
지시 그대로). **[설계 판단]** 플레이어의 라운드별 상대는 ROSTER 순서(32강→16강→
8강→4강→결승/동메달전)를 고정 스케줄로 추적하고, 나머지 31명의 코스메틱 대진은
32강 1회만 계산 — 두 트랙이 서로 안 얽힌다(진짜 대진 트리를 만들지 않은 단순화,
STATUS 판단 필요 목록에 기록).
작업3(매치 연동) — LiveMatch GameObject 를 매 라운드 새로 안 짓고 재사용하기 위해
`LiveMatchRunner.Rebuild(int seed)`(공개 `Build()` 재호출)와 `LiveMatchPanel.
ResetForNewMatch()`(공개, 로그/선택 초기화) 최소 추가. `PlayModeSetupMenu.cs` 를
지도가 진입점이 되도록 개편 — LiveMatch 는 에디터에서 미리 비활성으로 지어두고
"경기 시작" 클릭 시에만 `SetActive(true)`(Awake 타이밍 경쟁 회피, 상세는 코드 주석).
`MetaFlowController` 가 `LiveMatchRunner.Presented` 를 구독해 `MatchEnded` 를 잡아
결과 오버레이("승리!"/"패배...") 를 띄우고, "확인" 클릭 시 `ConfirmMatchResult` 가
라운드 전환·메달 판정·세이브를 처리한다.
작업4(세이브) — `SaveData.cs` 에 `TournamentRound` enum + `CareerSaveData.
practiceCupRound` 필드 추가(원 스키마엔 없던 필드, 진행 중 라운드가 저장 안 되면
재접속 시 대회 진행 상태를 잃어버려서 추가). 저장 시점: 경기 결과 확정 직후 + 지도
복귀 + 앱종료(`OnApplicationQuit`). `opponents[]` 는 id/name 만 기록(작업 지시
"필드만 채워지고 경기 영향 0" — adaptation/scouting 실채움은 §H 5/6 이월).
작업5(검증) — Unity MCP 컴파일 0/0(전 과정 재확인). 하니스 253/253 그린 + 42노드
지문 IDENTICAL. Play 실측을 사람 클릭 대신 `LiveMatchRunner.SubmitA`/
`SubmitOsaekomi` 공개 API 루프로 자동 진행(같은 프로덕션 코드 경로, 왕복 횟수만
줄임) — 기술 휠 클릭 1회는 직접 실측(스태미나 100→99 정확)해 UI 입력 경로 자체도
안 건드렸음을 재확인. **패배 경로**: 32강 패배 → `Lost`, 터미널 화면("메달 없음",
"경기 시작" 버튼 소멸)·대회 목록 상태줄 전부 정확. **승리 경로**: 32강(황도철)→
16강(문준배)→8강(서강윤)→4강(임세라, 일부러 패배)→**동메달전(오재훈) 자동 전환**
(`practiceCupResult` 는 `None` 유지, `practiceCupRound` 만 `BronzeMatch` — 터미널
아님을 실측 확인)→동메달전 승리→`Bronze` 저장 확인. 다섯 라운드 전부 실제 엔진
매치(12~49교환)로 완주, 매 라운드 표시 이름·버튼 위치가 ROSTER.md 스케줄과 정확히
일치. `opponents[]` 는 5경기 내내 1개로 유지(멱등 확인).
**세션 중 겪은 것들(기록만, 제품 결함 아님)**: (1) RunCommand 샌드박스가
`System.Reflection` 을 `using` 뿐 아니라 완전정규화 이름까지 정적 차단 — 라운드
전환 테스트를 리플렉션 없이 "진짜로 이겨서" 진행(승리 전략: 공격/카운터 준비/그립
확보를 3턴 주기로 섞어 seed=1 에서 승리 확보 — 무작정 공격 스팸은 40개 시드
전부 패배). (2) `CommandScript` 에 `Execute()` 외 헬퍼 메서드를 추가하면 이른
단계 NullReferenceException 재현(2회) — 인라인으로 우회, 원인 불명(다음 세션
참고). (3) 유니티 에디터의 Play 모드 종료가 `OnApplicationQuit()` 을 실제로 호출
한다는 걸 재확인(세이브 리셋 시도가 계속 무효화된 원인) — Play 종료 → Edit 모드
저장 → Play 재진입 순서로 우회.
**부수 수정**: `MetaFlowController.ShowBracket()` 자식 정리를 `Destroy()`→
`DestroyImmediate()` 로 변경 — 자동화 테스트 중 같은 프레임 재진입으로 보이는 자식
중복(6→9개)을 실측으로 잡아 방어적으로 강화(실사용 결함인지는 불확실, 화면이
드물게만 재구성돼 성능 비용 없음).
`git status --short` 로 의도한 파일만 바뀌었음을 확인(`Assets/_Recovery/` 는 이전
세션부터 있던 untracked 상태, 무관). 커밋·푸시는 사용자 확인 후 진행.
2026-08-13 (문서 세션 — ROSTER + DESIGN_future-mechanics 정본화)
작업 지시: 사용자가 md/에 넣어둔 초안 2종을 정본화(문서 전용, 코드 무수정).
작업0 — 스탬프 --check exit 0(5종 당시 3종, ROSTER/DESIGN_future-mechanics는
아직 미등록이라 정상 스킵), 하니스 253/253+지문 IDENTICAL, Unity MCP 컴파일
0/0(GetConsoleLogs 로 확인) — 전부 통과해 진행.
작업1 — `md/ROSTER_DESIGN-draft.md`/`md/DESIGN_future-mechanics-draft.md` 통독.
작업2 — `md/ROSTER.md` 신규 정본. 초안 내용을 그대로 옮기고 헤더(초안 지위 →
Purpose 문단)만 정본 스타일로 교체, 1행 스탬프 추가. 범례에 없던 [step7] 표기를
추가한 정정 1건 — 상세는 위 Next Up 항목 참조.
작업3 — `md/DESIGN_future-mechanics.md` 신규 정본. 마찬가지로 헤더만 교체, §1~§4
각 절 서두에 "§H 7단계 이월" 한 줄씩 추가(지시가 요구한 "각 절에 명시"를
문서 전체 전제 반복으로 구현) — 상세는 위 Next Up 항목 참조.
작업4 — 이 STATUS 갱신. Next Up 3.5 항목을 3.5a(완료)/3.5b(다음, 지도·대회
뼈대+로스터 저작)로 분리, §H 7단계 항목에 숙련도+콤보+계단형 시그니처+변칙기술
+체력 격화 묶어 재작성, `linkages` 필드 예약을 [선택]으로 3.5a 후속 메모에.
작업5 — 초안 2개 삭제(둘 다 git 미추적 상태였던 걸 `git log` 로 확인 — 삭제가
git diff 에 안 잡히는 게 정상, 내용은 정본 2개로 이동 완료). 스탬프 재확인
(`sh tools/stamp.sh md/ROSTER.md md/DESIGN_future-mechanics.md` 로 실해시 계산 →
f8c24e5/4112f17, `--check md/*.md` exit 0, 이제 5종). 하니스 재확인 253/253+
지문 IDENTICAL(문서만 바꿨으므로 당연한 결과 — 재확인 목적). `git status --short`
로 의도한 파일만 바뀌었음을 확인. 커밋·푸시는 사용자 확인 후 진행.
2026-08-12 (§H 구현 세션 — 3.5a: 세이브 계층)
작업 지시: §H 3.5a — 세이브 계층(데이터 기반, META_DESIGN §M1 스펙 구현). 세션이
컨텍스트 압축으로 이어졌다 — 파일 작성(Core 시임 3개, Game 세이브 계층, 테스트
18개, 하니스 확장)은 이미 끝난 상태에서 재개해 검증부터 진행했다.
작업1 — 검증. 하니스 `dotnet run -- --check`: 253/253 그린(235→253), 42노드 지문
IDENTICAL. Unity MCP: `AssetDatabase.Refresh()` 로 새 파일(.meta 자동 생성 확인) 반영
후 컴파일 로그 0 에러/0 경고, 새 타입 5개를 직접 참조하는 RunCommand 로 실행까지
재확인. 그 과정에서 `System.IO.File.Delete` 단독 호출이 RunCommand 샌드박스에서
막히는 것을 발견해 이분법으로 원인을 좁히고(Write+Read 는 통과, Delete 추가 시
실패) — 코드 결함이 아니라 하니스가 이미 통과시킨 동작(SaveSystem 내부 delete 경로
포함)이라는 것까지 확인, 기록만 하고 코드는 안 고침. 상세는 위 Next Up 항목 참조.
작업2 — 이 STATUS 갱신이 그 결과물. 3.5a 완료 명기, 실채움은 §H 5/6/7 로 이월.
커밋·푸시는 사용자 확인 후 진행.
2026-08-12 (문서 세션 — v4.0 §8 판단 대기 2건 확정 반영, v4.0→v4.1)
사용자 지시: v4.0 반영 세션이 §8 에 남겨둔 판단 대기 2건을 사용자가 확정한 결정대로
MATCH_RULES.md 에 반영(문서만, 코드 무수정).
작업0 — 세션 시작 확인: `git status --short`(untracked `Assets/_Recovery/` 폴더 발견 —
Unity 에디터 자동복구 스냅샷으로 추정, 이번 세션이 만든 게 아니고 작업 범위 밖이라
손 안 댐, 아래 참조) + `git config core.hooksPath`=tools/githooks 확인 + 스탬프
`sh tools/stamp.sh --check md/*.md` exit 0 확인.
작업1 — 결정 1(자원 명칭): §S 헤더·본문·§2 표·§4.4·§6.3·§7·§A.4·§B·§D.4·§E.2 전체에서
"Stance/스탠스" → **"Balance/밸런스"**로 통일(원문 인용구 `원문: "..."` 는 훼손하지 않고
그대로 둠). §S 에 **명칭 (v4.1, 확정)** 문단 신설 — 코드 식별자도 `Balance`로 잡는
이유(`CoreStance` enum 과의 충돌 회피, 실측: `Assets/_Project/Scripts/Core/Enums.cs:8`
의 `public enum CoreStance { Aggressive, Neutral, Defensive }`)를 명문화, 실제 개명은
§H 3단계로 위임. §4.3/§9.4의 "Core stance selection" 리셋 행은 **"자세(Core) 선택"**으로
표기 변경해 §S 자원과 완전히 분리. **Recovers 문단 재작성**(판단 대기 #1 해소): 그쳐/
굳히기 탈출 시 밸런스는 체력 `MatteRecovery`(§5.3, +3)와 같은 성격의 부분 회복 ⚙만
받고, 완전 리셋(100)은 경기 사이에만 — 이걸 §4.3/§9.4 표·문단에도 각각 각주로 남겼다.
작업2 — 결정 2(게이트 우선순위): §4.4 강등 게이트 불릿 뒤에 **"§F 숙련도와의 적용
순서 (v4.1, 확정)"** 불릿 신설 — 숙련도가 `TechniqueClass` 를 먼저 정하고, §4 판정식
직전 최종 단계에서 밸런스 게이트가 검사돼 상대 밸런스가 온전 임계 이상이면 그 값을
다시 `Sloppy`로 강등(게이트가 이긴다). 숙련의 연속 보정(`MasteryCostReduction`/
`MasteryReadReduction`)은 강등과 무관하게 항상 적용된다는 단서도 명기. §F "Judgment
reflection" 불릿에 §4.4 참조 한 줄 추가(중복 서술 대신 교차 참조).
작업3 — §8 두 판단 대기 항목을 RESOLVED v4.1 로 재작성(결정 내용 요약 포함), STATUS.md
Next Up 최상단·§H 구현 순서 히스토리 블록·Current State 를 전부 갱신. 새 "해결됨
(2026-08-12) — MATCH_RULES v4.0 §8 판단 대기 2건" 블록을 기존 HANDOFF 해결 블록
바로 아래에 추가(같은 패턴).
검증: 버전 태그 v4.0→v4.1 로 수동 변경 후 `sh tools/stamp.sh md/MATCH_RULES.md` 로
해시 재계산(`dd48c35`), `--check md/*.md` exit 0(3종) 재확인. `tools/dotnet-harness`
`dotnet run -- --check` **212/212 그린 + 지문 IDENTICAL**(문서만 바꿨으므로 당연한
결과 — 재확인 목적). `git status --short` 로 `md/MATCH_RULES.md` 외 추적 대상 파일이
안 바뀌었음을 확인(`Assets/_Recovery/` 는 이 세션 이전부터 있던 untracked 상태, 아래
참조). Unity MCP 는 문서 전용 세션이라 컴파일 확인 스킵(CLAUDE.md "MCP 끊기면 파일
편집만" 및 문서 세션 선례 그대로).
**세션 중 발견해 보고만 하고 손 안 댄 것 — `Assets/_Recovery/` untracked 폴더.**
`0.unity`/`0 (1).unity`/`0 (2).unity`/`0 (3).unity`(+ 각 .meta), 전부 2026-08-12
13:17~15:28 사이 생성. 이름·타임스탬프 패턴이 Unity 에디터 크래시/비정상 종료 시
자동 저장하는 복구 씬과 일치하는 것으로 보이나, 이번 세션은 문서만 다루고 Unity
에디터를 실행하지 않아 직접 확인은 못 했다 — 사람이 Unity 를 열어 실제 원인(크래시
여부)과 내용 확인 필요, 이 폴더가 필요 없으면 삭제도 사람 판단.
커밋·푸시는 사용자 확인 후 진행.
2026-08-12 (§H 구현 세션 — 0~1단계: CanUseConversion 버그 수정 + 손 분리 UI)
작업0 — 세션 시작 확인: 문서 통독(이전 세션에서 이미 전체 통독한 문맥 유지) +
`git status`/`core.hooksPath` 확인 + 스탬프 3종 exit 0 + 하니스 209/209 그린+
지문 IDENTICAL. **Unity MCP 컴파일 0/0 재확인**(지시대로 — 지난 세션은 문서
전용이라 스킵했던 걸 이번엔 코드를 건드리기 전에 반드시 확인). 세션 시작
시점엔 Unity MCP 도구가 아예 안 잡혀서(연결 중 표시조차 없음) 사용자에게
확인을 물었고, 사용자가 Unity 에디터를 확인한 뒤 도구가 연결됨.
작업1 — §H 0단계. `GripSystem.CanUseConversion(FighterId who)` → `(FighterId
who, Hand convertingHand)`로 시그니처 변경, 내부적으로 `Other(convertingHand)`
(반대 손) 헬퍼를 새로 두고 그 손의 그립만 검사하도록 수정 — 기존엔
`Value(who) > 0`(내 그립이 하나라도 있으면)만 봐서 자기 손으로 자기 그립을
전환하는 게 안 막혀 있었다(MATCH_RULES §2 "다른 손이 이미 잡고 있을 때만"
위반, 이전 세션 §H 설계 시점에 발견된 버그). 콜사이트 2곳 수정
(`MatchEngine.cs`는 `mine.OwnHand`, `LiveAssembler.cs`는 이 시점엔 UI가 아직
손을 안 물어봐서 `Hand.Right` 임시 고정 — 작업2에서 진짜 값으로 교체).
`GripSystemTests`에 반대 손 없음/있음 테스트 2개 추가, 기존
`Conversion_RequiresAnExistingHold`는 이름·내용을 반대 손 시나리오로 교체.
`LiveAssemblerTests`의 기존 테스트 1개(`HandOptions_AfterGrippingWithTheOtherHandFree_...`)
가 정확히 이 버그(같은 손 그립으로 전환이 열리는 것)를 정상으로 검증하고
있었던 걸 발견 — 의도(다른 손이 잡았을 때 전환이 열린다)를 살려 그립을
다른 손으로 옮기고, 반대 케이스(같은 손만 잡았을 때 전환 안 열림) 테스트를
새로 추가했다. 하니스 209→211, **지문 IDENTICAL** — 42노드 스크립트 경로
실측 확인 결과(`[m3] A Left→RightSleeve` 후 `[m8] A Right→TwistGrip`) 원래도
다른 손으로 잡고 다른 손으로 전환하는 올바른 패턴이라 이 버그에 애초에
안 걸려 있었다.
작업2 — §H 1단계. 구현 전 조사: 방사형 메뉴에서 손 축을 손 분리로 확장하는
두 안((a) 트리거를 왼손/오른손 2개로 분리 vs (b) 트리거 1개 유지, 2단
선택(손→타겟))을 Play 모드 실측(카메라 투영으로 A 캐릭터 화면 바운딩 박스
재확인, 기존 세션 값과 일치 — x=[77,806]/y=[227,842] at 1920)과 코드상 배치
공식(`angleDeg = -90 + idx*(180/(n-1))`) 분석으로 비교해 보고. 결정적 사실:
항목이 정확히 2개일 때 이 공식은 −90°/+90°에 배치하고 그 각도의 cos 는 0이라
**반지름과 무관하게 x축 돌출이 0** — (b)안의 1단계(손 고르기, 2항목)는 A 와
거의 안 겹친다. (a)안은 트리거가 3개→4개로 늘어 기존에 검증된 220px 세로
공간(간격 110px×3)이 부족해지는 문제가 있음도 같이 보고. AskUserQuestion 으로
제시 → 사용자가 **(b) 안**(권장안) 확정.
구현: `LiveMatchPanel`에 `_ownHand`/`_pendingOwnHand`/`_handStageTarget` 필드,
`RebuildAxisOptions`에 `closeOnPick` 매개변수(기본 true, 손 1단계만 false),
`BuildAxisControl`에 `onOpening` 콜백(손 트리거 전용 — 열 때마다 1단계로
리셋). `Confirm()`/`RefreshExposure()`가 `_ownHand`를 실제로 `Build()`에
넘기도록 수정(전엔 기본값 `Hand.Right` 고정이라 왼손이 UI에서 한 번도 노출된
적이 없었다). **`LiveAssembler.HandOptions()`도 `Hand ownHand` 매개변수를
받도록 변경** — 전환 게이트(0단계에서 고친 반대손 검사)와 뜯기 게이트(전엔
`AnyHandHeldOnMe()`로 "아무 손이나 잡혔으면"이었는데, 손이 실제로 둘로
나뉘는 이상 "이 손이 잡혔으면"이 맞는 질문이라 `LockState(_me, ownHand)`로
교체, `AnyHandHeldOnMe()` 헬퍼는 이 변경으로 안 쓰여 삭제)가 이제 손마다
실제로 다른 답을 낸다 — 이게 이 작업의 실질적 핵심이다. Core 규칙은 무수정,
전부 이미 손 단위로 짜여 있던 `GripSystem` API(3.1/3.2 설계 당시부터 손별
배열이었다)를 실제로 손 인자와 함께 부르는 배선 작업. 새 테스트
`HandOptions_TearOnlyOffered_ForTheSpecificHeldHand`가 손마다 다른 답이
나오는 것 자체를 고정한다. 211→212.
작업3 — 검증. Unity MCP 컴파일 0/0(경고는 Unity AI Assistant 패키지 자체의
네트워크 폴링 스레드 경고, 프로젝트 코드 무관). 하니스 212/212, 지문
IDENTICAL. Play 모드 실클릭 — 손 트리거 열기→왼손/오른손 2항목 확인→왼손
선택→타겟 5항목(그립 없는 초기 상태라 전환·뜯기·유지·흔들기 없음, 기대대로)
확인→왼소매 선택→휠 닫힘+트리거 복원+요약 "왼손 · 왼소매" 정확→코어/하체
중립 선택→확정 버튼 활성화 확인→확정 클릭→판정 진행(그립 스코어 0, 같은
교환에 B 도 습관 브레인으로 A 의 반대쪽을 잡아 상쇄 — 기대대로) 전부 확인.
7해상도(1920~320) 닫힌 상태 zeroSize=0/겹침=0(이번 세션이 트리거·요약 좌표를
안 건드려서 당연한 결과지만 재확인).
**세션 중 겪은 일 — Unity MCP 브리지 불안정(무한루프 아님으로 판명).** Play
모드 진행 중 특정 조작(주로 코어 축 클릭 직후) 뒤에 MCP 호출이 120초+ 응답
없이 백그라운드로 넘어가는 일이 세 차례 있었다. 재연결로 매번 풀렸고, 한
번은 지연 실행되던 명령이 그 사이 Play 모드가 꺼진 걸 못 찾아
`NullReferenceException`을 냈다(내 코드 버그가 아니라 Play 모드 상태가 바뀐
채로 뒤늦게 실행된 결과 — `EditorApplication.isPlaying`이 그 사이 False로
바뀌어 있었다). 세션 종료 시점 콘솔에 뜬 "Network polling encountered an
error: Thread was being aborted"(`Unity.AI.Toolkit.Accounts` 쪽)가 같은
현상의 원인으로 보인다 — CounterTime 코드가 아니라 Unity AI Assistant
패키지 자체의 네트워크 폴링 스레드 문제로 추정(확신은 아님). 여러 클릭을
한 `RunCommand` 안에 순서대로 묶어 왕복 횟수를 줄이는 방식으로 우회해
끝까지 검증을 마쳤다.
2026-08-12 (문서 세션 — MATCH_RULES v4.0 반영)
작업0 — 세션 시작 확인: HANDOFF(`HANDOFF_session_2026-08-11.md`, 2종 구
HANDOFF 는 전 세션에서 삭제 확인됨)→MATCH_RULES→CORE_DESIGN→OPPONENT_BRAIN→
STATUS 순으로 통독. `git config core.hooksPath`=tools/githooks 확인,
`sh tools/stamp.sh --check` 3종 exit 0, 하니스 `dotnet run -- --check` 209/209
그린+지문 IDENTICAL. Unity MCP 는 세션 내내 연결 중 상태로 도구가 안 떠서
컴파일 확인은 스킵(문서 전용 세션이라 불필요 — CLAUDE.md "MCP 끊기면 파일
편집만" 원칙).
작업1 — `md/MATCH_RULES_revision-v4-final.md`(사용자가 세션 시작 전에 이미
넣어둔 확정본, [결정 대기] 없음) 통독.
작업2 — `md/MATCH_RULES.md` 본문 반영. §S 스탠스 신설(§3 Grip 과 §4 Judgment
formula 사이에 배치), §4.4 [초안]을 스탠스 기반 확정판으로 교체(강등 게이트
일반화: "흔들기 선언" → "상대 스탠스가 임계 이상"), §2 표의 Hand/Legs 행과
§6.3/§7/§8 을 새 섹션 참조로 업데이트, 파일 끝에 §A~§G 를 새 최상위 섹션으로
추가(§H 구현 순서는 STATUS 에만 등재 — 역할 분리 유지). 버전 v3.8→v4.0,
`sh tools/stamp.sh md/MATCH_RULES.md` 로 해시 재계산(`b3957a9`).
반영 중 두 가지를 자체 판단 없이 처리: (1) 개정본이 `KuzushiGateEnabled`→
`StanceGate` 로 적은 것을, 그 사이 이미 있었던 `Kuzushi*`→`Setup*` 개명
(2026-08-11 오후)을 반영해 `SetupGateEnabled`→`StanceGate` 로 정정 — 의미
재해석이 아니라 현재 이름과 맞춘 것으로 판단해 진행. (2) 기존 §4.3/§9.4 의
"Stance"(Core 자세 선택 리셋)와 신설 §S 스탠스(수치 자원)의 이름 충돌을
발견 — 동작은 안 바꾸고 각주로만 구분, 실제 상호작용(그쳐 시 §S 도 완전
리셋되는가)은 정하지 않고 §8/STATUS 에 판단 대기로 등재. 그 외에 §F 숙련도의
TechniqueClass 와 §4.4 흔들기 강등이 같은 교환에 동시 적용될 때 순서도 개정본에
없어 판단 대기로 등재.
작업3 — 무수정 확인: `git status --short` 로 이번 세션에 `md/MATCH_RULES.md`
외 어떤 파일도 안 바뀌었음을 확인(CORE_DESIGN/OPPONENT_BRAIN/시나리오 JSON/
코드/테스트 전부 그대로), 하니스 재실행 209/209 그린 + 지문 IDENTICAL(문서만
바꿨으니 당연한 결과지만 재확인).
작업4 — 이 STATUS 갱신이 그 결과물. §H 구현 0~8단계를 Next Up 최상단에 등재.
커밋·푸시는 사용자 확인 후 진행.
2026-08-12
작업0 — 세션 시작 확인: `md/HANDOFF_session_2026-08-11.md`→MATCH_RULES→CORE_DESIGN→
OPPONENT_BRAIN→STATUS 순으로 읽음. 스탬프 3종 `tools/stamp.sh --check` exit 0,
`git config core.hooksPath`=tools/githooks 확인. 하니스 209/209 그린+지문 IDENTICAL,
Unity MCP 컴파일 0/0 — 전부 통과해 작업 중단 없이 진행.
작업1 — `feat/match-engine` 을 origin 에 그대로 푸시(9커밋, force 없음). 푸시는 성공했으나
`git status` 가 clean 하지 않음을 발견 — 위 "결정 대기" 항목 참조(이번 세션이 만든 변경
아님, 그대로 보고).
작업2 — 경기 화면 저해상도 렌더 v1(`ArenaStageView` 신규). 상세는 위 Next Up "가장 최근"
항목 참조. 커밋 3233a13.
작업3 — `docs/play-checklist-2026-08-12.md` 신설 — `md/HANDOFF_session_2026-08-11.md`
§5.1/§5.2/§6.2 를 체크박스로 옮겨 정리만 함(새 내용 없음). 커밋 3080078.
작업4 — 이 STATUS 갱신이 그 결과물.
후속 지시(작업4 이후) — 위 "결정 대기"(HANDOFF 2종 삭제)를 사용자가 확정: 두 파일 삭제
커밋 + `HANDOFF_session_2026-08-11.md` §0 의 전제 문장 최소 수정, 이번 세션 미푸시 커밋
전부 origin 에 푸시(force 없음).
2026-08-12 (6차 — 선택 UI 방사형(휠) 메뉴 전환)
사용자 지시: 5차의 위로 펼치는 드롭다운이 A 를 가리고 손 목록이 화면 밖으로
잘린다는 Play 실측 신고 후속 — 방사형(휠) 메뉴로 교체. 확정 사항: 트리거는
화면 왼쪽 끝 세로 배치(캐릭터 안 가리는 게 목적), 선택지는 오른쪽으로 호를
그리며 펼침, 완전한 원 불필요(적당한 호로 충분), 항목 많은 축(손)은 곡률을
완만하게. 참고 구조는 LoL 핑 휠/오버쿡드 감정표현/스파이더맨 웹 기술 계열 —
배치 패턴만 차용, 특정 게임 에셋·아이콘 복제 금지.
구현 전 조사·보고(코드 변경 없이): (a) 호가 A 와 겹치는지 반지름·각도 후보별
실측 (b) 손 10개 세로 오버플로 여부 (c) 눌러서 끌기(롤 방식) 조작의 구조적
가능성만 검토. 보고 후 사용자가 "안② 단일호 R=130" 승인 — 당시 계산은 A
겹침 없이 "24px 여유"로 제시했으나, 이는 트리거를 폭 0인 점으로 취급한
틀린 계산이었다(나중에 구현 중 자체 발견, 아래 참조).
구현: `BuildAxisControl` 을 트리거(축 이름만, 74x36 컴팩트)+요약 텍스트(트리거
아래 별도)+휠 컨테이너 구조로 재작성 — 트리거 자체가 "손: 오른소매"급 라벨을
달면 그 절반 폭이 승인된 반지름 여유를 깎아먹는다는 걸 깨닫고 설계 변경.
`RebuildAxisOptions` 는 극좌표(반지름 R, -90°~+90° 균등 분배)로 옵션 버튼
배치 — 손 R=130/코어 R=90/하체 R=100. `ToggleDropdown`/`OpenWheel`/
`CloseAllWheels` 로 한 번에 휠 하나만 열리게 하고 나머지 두 축 트리거+요약은
숨김(세로 110px 간격 트리거끼리 반지름 130 휠이 옆 트리거까지 닿아서).
확정 게이트·제출·리셋 배선은 5차 그대로 재사용.
컴파일 후 재검증(Play 모드) 중 두 가지 발견: (1) 요약 텍스트 Y 오프셋(-22)이
트리거 하단(-18)과 6px 자체 겹침 — 실측으로 잡아 -32 로 수정, 재검증 통과.
(2) **열린 상태 A/B 겹침을 카메라 투영(WorldToViewportPoint→RawImage 스크린
좌표)으로 정밀 실측한 결과, 승인 당시 "24px 여유" 계산이 완전히 틀렸다는 게
드러남** — A 의 실측 화면 바운딩 박스가 x=47~806(1920 폭의 42%)로 매우 넓고
트리거(x=25)에 거의 붙어 있어서, 부채꼴 ±90° 끝점(반지름과 무관하게 항상
x≈25 근처)조차 A 왼쪽 끝과 15px 겹치고, 각도 0°(반지름 방향)는 최대 145px
겹친다. 손·코어·하체 세 축 전부(옵션 전체 15개) 겹침 확인 — 반지름 조정으로
피할 수 없는 구조적 문제임을 판단(±90° 클리어하려면 버튼 폭 44px 이하 필요,
손 10개 항목엔 너무 빡빡함).
AskUserQuestion 으로 실측치와 기하학적 불가능성을 그대로 보고하고 처리 방안을
물음 — "겹침 허용 + 휠 열린 동안 배경 딤 처리"(참고 사례인 LoL 핑 휠 등도
게임 화면 위에 겹쳐 뜬다는 점과 부합) 선택받음, 반지름·각도·트리거 위치
재조정 없음. `ArenaBackground.color` 를 휠 열림 시 45% 밝기, 닫히면 100% 로
복원하는 것만 추가.
검증: Unity MCP 컴파일 0/0(요약 오프셋 수정 후, 딤 처리 추가 후 두 차례).
Play 모드 실클릭 — 3축 각각 열기(딤 확인)/선택(자동 닫힘+딤 해제)/닫기,
확정 게이트 전환(Idle→ShidoYellow), 제출 후 리셋(요약 "—"/"중립"/"중립"
복귀) 전부 확인. 7개 해상도 닫힌 상태 zeroSize=0/overlaps=0/wrapped=0 —
1024x768 에서 뜬 "IPPON 스트립 x RowA/RowB" 겹침 경고는 고정밀 재측정으로
-0.00003px(부동소수점 경계 접촉) 확인, 오탐 처리(스코어보드는 범위 밖,
무수정). 하니스 209/209, 지문 IDENTICAL. 커밋 64ddaf3.
2026-08-12 (5차 — 실전 UI 개편: 손·코어·하체 캐릭터 옆 드롭다운)
사용자 지시(레이아웃 변경): 점수판 화면 중앙 하단, 확정 버튼 그 바로 위(3축 전부
선택 전 비활성), 손·코어·하체 상시 3열 폐지 → 캐릭터 옆 트리거+드롭다운. 구현
전 3건(배치 기준/추적 방식/드롭다운 방향) 실측·근거와 함께 보고만 하고 사용자
확정 받은 뒤 진행 — AskUserQuestion 으로 각각 미리보기 포함 선택지 제시.
확정 결과: (a) 주체 기준(전부 A 옆) (b) 화면 고정 위치(본 추적 기각, 12fps 스텝
지터 최대 17.5px 실측 근거) (c) 위로 펼침(코어·하체는 상대와 179px 여백 두고
안 겹침 확인, 손은 최대 10개 옵션이라 화면 밖으로 넘칠 수 있는 한계는 코드
주석에만 남기고 안 고침).
구현: BuildCanvas 순서 재배치(MiddleRow→OsaekomiRow→ConfirmRow→ScoreboardRow),
BuildStandingRoot/BuildColumnRoot 를 BuildFloatingControls/BuildAxisControl 로
교체, 확정 게이트 bool 3개 추가, 드롭다운 단일 열림 토글.
Play 모드 실측 검증 중 회귀 검사가 NarrationLog(leftColumn 전체를 채우고
있던 것 — 지난 세션 결과, 이번 세션 아님)와 새 트리거 버튼의 실제 겹침을
잡아냄. "서술 로그 무수정" 원칙에 걸렸지만 AskUserQuestion 으로 사용자에게
다시 물어 "이번에 한해 예외 허용" 확정 받고 NarrationLog 를 트리거 밴드
위쪽(고정 280px)으로 앵커 재배치 — 닫힌 상태 겹침 0 달성, 드롭다운 열린
상태의 잔여 겹침(손 최대 221px)은 팝오버 성격으로 판단해 허용, 실측값과
함께 STATUS 에 기록.
겪은 테스트 방법론 함정: Unity Destroy() 프레임 지연으로 같은 프레임 안에서
연속 클릭하면 드롭다운 자식이 일시적으로 배로 보임(예: childCount=6, 실제는
3) — 이 코드베이스에 이미 기록된 현상, 프레임을 나눠 재측정해 실제로는
정상임을 확인. GameObject.Find 는 비활성 오브젝트를 못 찾는다는 것도 재확인
(OsaekomiRow 등 SetActive(false) 대상은 계층 재귀 탐색으로 확인).
7개 해상도 닫힌 상태 전부 zeroSize=0·겹침=0·줄바꿈=0, 드롭다운 열린 상태(대표
해상도 1920)는 위 NarrationLog 예상 겹침만 확인. 하니스 209/209, 지문
IDENTICAL. 커밋 f84f631.
2026-08-12 (4차 — 전체화면 배경 후속: 사이드바 진단 + 카메라 이동)
사용자 신고 2건: ① 사이드바 본문이 캐릭터와 겹쳐 안 읽힘, 배경이 거의 투명해 보임
② 전체화면 전환 후 B가 우측 사이드바 뒤로 완전히 가려짐. "추측 수정 금지" 지시대로
①부터 실측 진단 — Image color/alpha/sibling순서/텍스트 오버플로/CanvasGroup 전부
확인했지만 구조적 결함을 못 찾음(사이드바는 이미 다른 패널과 같은 PanelBg 처리가
돼 있었다). 코드 변경 없이 "이미 통일돼 있다"고 보고, 대신 ③(카메라 이동)이 B를
사이드바에서 멀리 옮기면서 간접 완화될 걸로 예상된다고 STATUS/보고에 명시.
②는 실측으로 풀렸다: 결합 RT-x 폭(169px)이 사이드바 안전폭(~208px)보다 좁아서
확대·축소 없이 카메라를 옆으로(x=0→0.6) 이동하는 것만으로 해결 — 세로 100px
규격은 수평 이동과 무관해 그대로 유지됨을 재확인(A 102.1px/B 100.9px).
4:3(1024x768)에서는 이 값으로 안 풀린다는 것도 실측으로 확인해 보고만 하고 코드로
안 고침(EnvelopeParent 크롭이 종횡비마다 달라 정적 카메라 하나로 전부 못 만족) —
사용자가 지시한 "임의로 어느 쪽도 희생하지 말고 사용자 판단으로 넘긴다" 원칙 그대로
따름.
5번(별건 확인) — HANDOFF 2종은 지난 세션(커밋 5f65192)에서 사용자가 이미 명시적으로
삭제를 확정·지시해 처리된 상태임을 git log 로 재확인, 복원 불필요라고 보고.
7개 해상도 재실행 — zeroSize=0·겹침=0·줄바꿈=0(4:3 포함 구조적 검사는 전부 통과,
캐릭터 프레이밍만 별개). 하니스 209/209, 지문 IDENTICAL. 커밋 74ad48b.
2026-08-12 (3차 — 경기 화면을 창 전체 배경으로)
사용자 신고: 씬 스카이박스와 좌상 박스 안 RT 가 배경 두 겹으로 보여 혼잡. RawImage 를
좌상 `ArenaBox`에서 캔버스 최하단 sibling(`ArenaBackground`, 전체 stretch)으로 옮기고
`ArenaBox`/`RectMask2D` 를 통째로 제거(더 이상 침범할 이웃이 없어 불필요해짐 확인 후
제거), `EnvelopeParent`는 유지(16:9 아닌 창에서 안 찌그러지려면 여전히 필요). 비워진
좌상 자리는 `NarrationLog`가 이어받음(호출 한 줄 제거로 자연히 확장).
점수판·하단 입력·서술 로그 3곳에 `PanelBg`(기존 사이드바 색 재사용) 스크림 추가 —
알파값은 스크린샷 없이 정할 수 없어 하나 골랐다고 코드 주석·STATUS 양쪽에 명시.
카메라는 "RT 안 캐릭터 100px" 규격이 표시 배율과 무관한 RT 공간 값이라 무수정 확인
(재실측 A 96.1px/B 95.4px, 애니메이션 자연 변동 범위).
겹침 검사 방법론 갱신: 의도된 전체화면 배경(`ArenaBackground`)은 이름으로 제외하고
나머지끼리만 검사 — "덮는 게 목적인 배경과의 겹침"과 "실수로 겹친 두 패널"은 다른
범주라는 걸 지난 세션의 RectMask2D-clip 처리에 이어 다시 확인.
7개 해상도 재실행 — zeroSize=0·겹침=0·줄바꿈=0 전 구간, 배경 크롭 실측: 정확히 16:9인
1920x1080/1280x720/960x540/320x180 은 크롭 0%, ≈16:9인 719x404/600x338 은 0.1~0.15%
(무시 가능), 4:3인 1024x768 만 가로 25%(양쪽 12.5%) 크롭 — 설계대로. 하니스 209/209,
지문 IDENTICAL. 커밋 dd56bab.
2026-08-12 (2차 — 경기 화면 UI 도트화 진단·수정 + 캐릭터 확대)
사용자가 Play 실측으로 2건 신고: UI 텍스트 도트화, 캐릭터가 너무 작음. "추측 수정 금지,
진단 먼저" 지시대로 캔버스 renderMode/worldCamera/scaleFactor, ArenaStageCamera
cullingMask/targetTexture 를 먼저 실측 — Canvas 는 ScreenSpaceOverlay/worldCamera=null 로
구조적으로 카메라와 무관함을 확인, 대신 Game View 커스텀 렌더 해상도가 지난 세션의
719x404 로 남아 있어 CanvasScaler scaleFactor=0.374 로 낮아진 게 진짜 원인임을 확인
(1920 리셋 즉시 scaleFactor=1). `PlayModeSetupMenu` 에 해상도 리셋을 추가해 재발 방지.
카메라 프레이밍은 z=-6→-4.25 + 하향 11° 로 재실측(A 100.9px/B 99.6px, 클리핑 없음).
RawImage AspectRatioFitter 를 FitInParent→EnvelopeParent 로 바꾸고 ArenaBox 에
RectMask2D 추가, flexibleHeight 1.2→4.5 로 박스 확대(173→260px). 상세는 위 Next Up
"가장 최근" 항목 참조 — 무크롭·무바 동시 달성이 이 5블록 구조에서 기하학적으로 불가능함을
실측으로 확인하고 EnvelopeParent(캐릭터 소폭 크롭)를 판단해서 골랐다고 코드 주석·STATUS
둘 다에 남겼다.
검증 중 발견한 방법론 함정 2개, 기록: (1) 좌표 겹침 검사에 `RectMask2D` 로 잘리는 오버사이즈
RawImage 를 그대로 넣으면 raw RectTransform 크기가 잡혀 점수판/서술 블록과 대량의 가짜
겹침이 나온다(22건) — 마스크를 인지해 클립한 rect 로 다시 계산하니 0건. (2) `result.Log`
가 `{0:F1}` 같은 `.NET` 서식 지정자를 그대로 문자열로 찍어버린다(치환 안 됨) — 서식 없이
문자열 접합으로 우회.
7개 해상도(1920/1280/1024/960/719/600/320) 재실행 — zeroSize=0·겹침=0·줄바꿈=0, 매
해상도에서 scaleFactor 가 화면폭/1920 그대로 정상 추적(더는 고정 안 됨). 하니스 209/209,
지문 IDENTICAL. 커밋 3bc6acd.
2026-08-11 (오후, 7차 — 좁은 화면 레이아웃 붕괴 수정)
사용자 지시: 719px 스크린샷 신고("추측 수정 금지, 원인 진단 먼저") + 4개 해상도 재검증 + 최소 지원 해상도 명기.
진단 과정: CanvasScaler 라이브 값 확인(match=0, Unity 기본값) → 공식으로 "겉보기 폭은 실제 화면폭과 무관하게 항상 1920"임을 도출 → `PlayModeWindow.SetCustomRenderingResolution`(공식 API, reflection 아님 — `System.Reflection` 은 RunCommand 샌드박스에서 막혀 있어 처음엔 GameView 리사이즈로 우회하려다 발견)로 실제 719×404 를 재현해 `RectTransform.rect`/`LayoutElement` 필드를 직접 대조 → `ScoreboardRow`/`BottomInputRow`/버튼/여러 텍스트가 폭 0으로 무너져 있는 것을 확인 → 720p 로 되돌려도 동일하게 0(해상도 무관, 구조적 버그)임을 대조 실측으로 확정. 근본 원인: 교차축에 `preferred/min/flexible` 셋 다 안 준 `LayoutElement` 는 Unity 가 무조건 0으로 접는다 — 지난 세션의 겹침 검사가 이 사각지대(폭 0 요소는 애초에 안 겹친다)를 못 잡았다.
수정: `SetFixedHeight`/`MakeButton` 기본값에 `flexibleWidth=1` 추가, `MakeFlexText`/`SetFlexible` 교차축 인자 전부 0→1, `MakeFlexText` 는 `preferredHeight=minHeight` 도 같이 준다(min 단독 충분성은 실측 안 함 — 확인된 조합만 씀). `MakeText` 에 TMP auto-size 상시 적용 + 버튼 라벨은 줄바꿈 금지(`textWrappingMode=NoWrap`, 구 API `enableWordWrapping` 은 obsolete 경고가 나서 신 API 로 씀).
검증: 1920×1080/1280×720/1024×768/719×404 네 해상도 + 여분으로 600×338/320×180 까지 총 6개 — 63개 요소·zeroSize=0·쌍별 겹침=0·버튼 라벨 줄바꿈=0 전 구간 동일. 719px 에서 실제 클릭(손/코어/하체+확정)까지 재확인. 최소 지원 폭은 "구조적으로는 하한 없음(320px 도 안 깨짐), 가독성 기준 권장 960px"로 STATUS 에 명기 — 후자는 폰트 크기 계산 추정이지 육안 확인이 아니라고 밝혀둠(스크린샷 캡처가 이 MCP 환경에서 신뢰 안 된다는 기존 troubleshooting 문서와 같은 이유로 생략, 좌표·폰트 크기 실측으로 대체).
Unity MCP 컴파일 0/0(obsolete 경고 1개 있었다가 API 교체로 제거, 최종 0/0). 하니스 209/209, 지문 IDENTICAL(LiveMatchPanel.cs/LiveMatchRunner.cs 는 하니스 컴파일 대상이 아님 — 재확인).
2026-08-11 (오후, 6차 — 화면 전면 재설계)
사용자 지시: 현재 레이아웃을 버리고 상단(점수판)/좌상(경기 화면 자리)/좌하(서술+로그)/우측(사이드바 3종: 코치의 조언/수 노트/내 성향)/하단(입력) 5블록 구조로. 절대 표시 금지 재확인(심판 누산기/B 선택지·의도/판정식 합계/A 가 관측 안 한 B 습관). 겹침 재발 방지 조건 명시(좌표를 손으로 안 찍고 영역 비율·항목 수에서 계산).
계획 단계에서 3가지를 스스로 판단하고 명시했다(사용자 재확인 없이 진행, 전부 표시 정보의 취지를 안 바꾸는 배치 판단이라 판단): 기존 위상/체력/그립/손 잠김 상태 HUD 는 스펙에 자리가 명시 안 돼 있었다 — 좌하 "상태 줄"로 흡수(문자열 조립 로직 무수정). 코치 조언은 자기 노출 위험선을 다루지 않는다(3번 사이드바와 중복 방지). `_preemptedText` 별도 배너는 정보 손실 없이 제거(서술 줄과 100% 동일 문자열의 3중 표시 중 하나 정리).
구현: `LiveMatchRunner`에 `Ledger`/`OpponentHabits` 프로퍼티 2줄 추가. `LiveMatchPanel.BuildCanvas` 전면 재작성 — `VerticalLayoutGroup`/`HorizontalLayoutGroup` 중첩으로 5블록 + 사이드바 3박스 + 입력 3열을 전부 계산 배치(anchorX 상수 완전 제거). `MakeVerticalGroup`/`MakeHorizontalGroup`/`SetFlexible`/`SetFixedHeight`/`SetFixedWidth`/`MakeFlexText`/`MakeSidebarBox` 헬퍼 신설. 기존 판정 서술·로그·노출막대 임계값·점수판·굳히기 2지선다 로직은 전부 무수정(자리만 이동). Core/Brain 무수정.
검증 상세는 위 Next Up 항목 참조. 하니스 대상 아님(확인만, 재실행 없음) — Unity MCP 컴파일 0/0 + Play 모드 30교환 실측(좌표 겹침 0 두 상태, 코치조언/수노트/내성향 값 대조)으로 대체.
2026-08-11 (오후, 5차 — 쿠즈시 → 흔들기/Setup 개명)
사유: 쿠즈시는 유도 전용 어휘라 타 종목(복싱·킥복싱·MMA) 확장 시 안 맞는다. 규칙·동작은 일절 무변경, 이름만. 화면 표시는 "흔들기", 코드 식별자는 `Setup*`.
전수 조사 먼저 — grep 으로 24개 파일 확인 후 목록을 보고하고 진행. 주의 사항 셋 다 확인 완료: (1) Feint/페인트/위장공격(§6.1)과 텍스트·의미 둘 다 안 겹침 (2) 예비동작(tell 모캡 클립 이름)과 무관, 재사용 안 함 (3) 이름 기반 직렬화 — `.unity`/`.asset`/`.prefab` 전체를 grep 해서 저장된 참조가 0 개임을 확인, 유일한 이름 기반 조회는 `PlayModeSetupMenu.cs` 의 `SerializedProperty.FindProperty("kuzushiEnabled")` 뿐이라 필드 개명과 같이 고침. 지문(`fingerprint-baseline.txt`)도 grep 으로 "쿠즈시"/"Kuzushi" 0 개 확인 — 42노드 대조 시나리오가 플래그 기본 off 라 애초에 이 경로를 안 밟는다.
바꾼 것: 파일 3개 git mv(.meta 동반, GUID 보존) — `KuzushiPrecedingCommitSource.cs`→`SetupPrecedingCommitSource.cs`, 그 테스트, `Core/Tests/KuzushiTests.cs`→`SetupTests.cs`. 식별자 전부(`HandTarget.Kuzushi`→`Setup`, `KuzushiGateEnabled`→`SetupGateEnabled`, `KuzushiCost`→`SetupCost`, `PayKuzushi`→`PaySetup`, `KuzushiDeclared`→`SetupDeclared`, `IsKuzushiActive`→`IsSetupActive`, 사설 필드 2개, 테스트 메서드 12개). 화면 표시 3곳(손 옵션 라벨, 교환 로그 "흔들기 설치", HUD "흔들기 유효/없음"). MATCH_RULES.md §2/§4.4 갱신 + v3.7→v3.8(스탬프 해시는 pre-commit 훅이 자동 갱신). 남은 "쿠즈시" 0개(Assets 전체 grep 확인) — docs/overnight-*·decisions/·daily/ 는 과거 기록이라 그대로 뒀고, 이 파일엔 위 대응표 한 줄만 추가.
검증: Unity MCP 컴파일 0/0(파일 리네임 후 재컴파일 확인), dotnet 하니스 209/209, 지문 IDENTICAL(원래 예상대로 — 플래그가 기본 off 라 대조 경로는 흔들기를 아예 안 지나간다).
2026-08-11 (오후, 4차 — 입력 열 텍스트 겹침 수정)
사용자가 Play 스크린샷으로 신고: 손 열(쿠즈시 포함)과 코어/하체 열 텍스트가 겹쳐 안 읽힌다. "추측 수정 금지" 지시대로 Play 모드에서 실제 버튼을 눌러(같은 프레임 안에서 연달아 클릭하면 `RebuildColumn` 의 `Destroy()` 가 그 프레임엔 안 지워져 자식이 70개까지 쌓이는 걸 먼저 발견 — 실제 사용자는 클릭 사이에 항상 프레임이 끼니까 안 겪는 테스트 아티팩트였다. MCP 호출을 클릭 1개당 1개로 쪼개 프레임 경계를 실제로 두는 방법으로 우회) 손 열 10개 항목(쿠즈시 포함, 실제로 도달)의 `RectTransform.GetWorldCorners` 를 찍었다.
가설 3개 검증 결과: (1) 손 열 행 높이가 항목 수 반영 안 하는가 — **아니다.** `VerticalLayoutGroup` 이 이미 10개를 285.3px 안에 23.4px 씩 자동 축소하고 있었고 TMP 필요 높이(21.6px)도 그 안에 들어가 겹침 없음(OVERFLOWS=False 10/10). (2) 하이라이트 행이 행 간격보다 큰가 — 해당 없음, 애초에 안 겹친다. (3) 점수판 재배치가 원인인가 — **아니다,** git log 로 `StandingInput.sizeDelta.y=320` 이 생성 후 한 번도 안 바뀌었음을 확인.
진짜 원인은 별개: `BuildStandingRoot` 세 열의 anchorX 중심(0.06/0.38/0.62)이 열 반폭 0.14 기준으로 원래부터 비대칭이었다(간격 0.32/0.24, 둘 다 필요한 0.28에 못 미치거나 겨우 넘음) — 코어-하체 간격이 −0.04(실측 −76px)라 두 열의 버튼 배경이 상시 겹쳐 있었다. 이번 세션이 만든 버그가 아니라 6단계 최초 구현부터 있던 것 — 쿠즈시로 손 열이 붐벼 보이면서 사용자가 처음 알아챈 것뿐.
수정: 하체 열 중심만 0.62→0.68 (코어·손·확정 버튼 무수정). 재검증은 이전과 같은 좌표 실측 방식으로 — 손10+코어3+하체4+확정1=18개 버튼 45쌍 전부 계산으로 0 겹침 확인(간격 손-코어 76.1px, 코어-하체 38.0px, 하체-확정 72.9px). 하니스 209/209, 지문 IDENTICAL.
2026-08-11 (오후, 3차 — 골든스코어 도달 불가 수정)
사실 확인부터: MATCH_RULES §1 원문("Win resolution at 4:00: ippon > waza-ari count > yuko count > golden score", "Time-up is checked AFTER an exchange fully resolves", "Golden score: unlimited time, first score of any kind wins immediately; third shido loses") 과 CORE_DESIGN 의 "MatchClock 이 RegulationExpired 를 내면 엔진이 GoldenScore 로 넘기거나 승자를 정한다"를 확인. grep 으로 `MatchEngine.cs` 안에 `RegulationExpired`/`WinnerAtTimeUp`/`EnterGoldenScore` 호출이 정말 0개임을 재확인 — 둘 다 그 자체 로직은 격리 테스트로 이미 맞았고(`MatchClockTests`/`ScoreBoardTests`), 빠진 건 순수히 "누가 부르느냐"였다.
구현: `ScoreBoard.FinishAtTimeUp()`(신규, 기존 공개 API 두 개만 조합) + `MatchEngine.ResolveExchange` 끝에 조건부 호출 + `GoldenScoreEntered` 이벤트 1개 + `LiveMatchPanel.EventSummary` 한 줄. 의도적으로 규칙을 새로 안 만들고, 애매한 한 갈래(정규시간 만료 순간에 막 열린 굳히기)는 구현하지 않고 테스트로 현재 동작을 사실로만 고정했다.
검증: `GoldenScoreTests.cs` 7개 신규(결정된 시간종료 / 다 비긴 시간종료→골든스코어+시계 무제한 / 골든스코어 중 즉시종료 판정·지도 3개 둘 다 풀 엔진 경로로 / 틀린 답 2개). 202→209 그린, 지문 IDENTICAL. 스모크 6구성(반복/변주/현실적×on/off) 전부 다시 봤다 — 가장 오래 도는 것도 최대 추정 165초로 240초 근처도 안 가서 회귀망이 이 경로를 하나도 안 밟는다는 사실을 기록. Unity MCP 컴파일 0/0, `ScoreBoard.FinishAtTimeUp()` 직접 참조로 재컴파일 확인.
2026-08-11 (오후, 2차 — 실전 점수판 재배치)
사용자 지시: 신설이 아니라 재배치. 시계·절반·유효·지도·누르기 카운트·골든스코어만 사진 6장 레퍼런스(특히 마지막 사진 — -63kg/4강전, 골든스코어 표시본) 규격의 점수판으로 옮기고, 잡기·체력·위상·쿠즈시·교환 로그·선택 확인은 기존 구현을 그대로 점수판 아래로 재배치. `LiveMatchPanel` 내부 좌표만 다시 잡았고 이벤트 배선·요약 로직(`BeginPending`/`BuildOutcome`/`EventSummary`/`PushLog`)은 무수정.
점수판 구조: 큰 타이머(위) + 골든스코어 라벨(타이머 아래, 금색, `Board.InGoldenScore` 읽음) + IPPON 세로 스트립(한판 나면 강조) + A(흰 배경)/B(파란 배경) 두 행, 각 행에 W(절반)/Y(유효)/S·H(지도 카드 아이콘 — 1·2개 노란, 3개째 빨강 하나로 전환)/누르기 카운트(2자리, `Osaekomi.HeldSeconds`를 그 교환의 홀더 행에만 표시). 국제룰 5/10/20초 경계는 기존 `MatchRulesConfig.OsaekomiYukoSec/WazaAriSec/IpponSec` 기본값과 이미 일치 — 새 상수 없음.
Play 모드 실측 검증(사용자 요청 6번, 이번엔 실제로 함): `CounterTime > Play Mode Setup` 메뉴를 `EditorApplication.ExecuteMenuItem` 로 실행 → `EditorApplication.isPlaying = true` → 다음 MCP 호출에서 씬이 이미 Play 상태로 로드되고 `LiveMatchPanel.Awake()` 가 이미 캔버스를 지은 것을 확인 → `Button.onClick.Invoke()` 로 A 의 손/코어/하체 선택 + 확정을 2턴 실제로 실행 → 화면에 뜬 각 TMP_Text.text 를 엔진 게터(`eng.Clock/.Board/.Grip/.Stamina/.Osaekomi`)로 독립 재계산한 기댓값과 문자열째 비교. 타이머 3회, 양쪽 체력·그립·양손 잠김상태 라벨 3회 전부 MATCH=True(00:00 형식까지 정확히 일치). 로그는 스크래치패드 `play_verification.log` 에 저장 + 세션에 전문 출력. 이번 2턴에서는 득점·지도·골든스코어가 실제로 안 났으므로 그 칸들의 렌더링은 "0 값이 맞게 나온다"까지만 실측했다 — 비제로 값 렌더는 코드 경로 동일성으로 신뢰하되 육안 미확인.
검증 중 발견(수정 안 함, 기록만): (1) 같은 프레임 안에서 손→코어→하체를 연달아 클릭하면(자동화 테스트라 프레임 경계 없이 동기 실행됨) `RebuildColumn` 의 `Destroy()` 가 그 프레임 끝까지 지연돼 손 옵션 목록에 중복이 쌓인다. 실제 사람이 클릭할 때는 클릭 사이에 항상 프레임이 끼어서 발생 안 함 — 테스트 방법론의 산물이지 제품 결함 아님, 안 고쳤다. (2) 골든스코어 진입 코드가 어디에도 없다는 것을 grep 으로 재확인(Next Up 참조) — HUD 는 배선해뒀지만 지금은 절대 안 뜬다.
컴파일: Unity MCP AssetDatabase.Refresh 후 0/0, `LiveMatchRunner.Build()`+`LiveMatchPanel` 인스턴스로 `Scoreboard`/`RowA`/`RowB` 계층이 실제로 생성됨을 참조 확인. dotnet 하니스 202/202 그린, 지문 IDENTICAL — 전부 프레젠테이션 레이어라 엔진 무변화.
2026-08-11 (오후, 1차)
LiveMatchPanel 상태 표시 추가 — 클릭은 이미 정상(6단계 배선 성공)이었지만 화면에 현재 상황이 안 보여서 뭘 고르는지 판단할 수 없다는 사용자 지적으로 시작. 상단 HUD(시계, 위상[서기/굳히기+누른시간/이탈수], 점수판 A·B, 잡기[유효값+양손 놓임/잡힘/잠김], 체력, 쿠즈시 게이트 켜졌을 때만 유효/없음), 최근 5교환 로그(내 선택 → 결과, 폴백 서술은 회색 유지), 확정 전 자기 선택 한 줄 확인을 추가했다. 심판 누산기·B 선택지/의도·판정식 합계는 그대로 노출 안 함(설계 확정 재확인).
구현: `MatchEngine.IsKuzushiActive(FighterId)` 추가(Core, `_kuzushiActive` 읽기 전용 미러) — Core 판정 로직 자체는 무수정. `LiveMatchRunner.Config` 추가(Game, `_cfg` 노출 — 쿠즈시 게이트 on/off 판단용). `LiveMatchPanel` 은 `Presented` 이벤트를 새로 구독해 한 번의 Submit* 호출 동안 발생한 이벤트를 모으고(`BeginPending`/`_pendingEvents`), 판정 서술이 있으면 그걸, 없으면 이벤트 목록에서 뜻 있는 첫 번째를 요약(`EventSummary`)해 로그 한 줄로 만든다. 굳히기 2지선다 버튼도 같은 경로(`SubmitOsaekomiChoice`)로 옮겨 로그·HUD 가 굳히기 턴에도 갱신되게 했다.
검증: Unity MCP로 컴파일 확인 — AssetDatabase.Refresh 후 0 에러/0 경고, 추가로 `IsKuzushiActive`/`Config` 를 직접 참조하는 RunCommand 스크립트가 컴파일된다는 사실로 두 어셈블리가 실제로 재컴파일됐음을 확인(단순 GetConsoleLogs 만으론 컴파일이 스킵됐을 가능성을 못 배제하기 때문). dotnet 하니스 202/202 그린, 지문 IDENTICAL — 이번 변경은 전부 프레젠테이션 레이어 읽기라 엔진 동작에 영향 없음을 재확인.
**Play 모드 실측(사용자 요청 6번)은 아직 안 했다** — 화면에 실제로 값이 맞게 뜨는지는 사람이 Play 를 눌러 봐야 한다. 새 v1 임시 문구들은 STATUS 카피 교체 목록에 등록.
2026-08-11 (아침)
어젯밤 판단 대기 5건 브리핑(원문 출력 + m18 실측: play 모드 브레인 경로에서 실제 발화하는 값이 등록된 키와 일치함을 코드로 확인) + Play 모드 2클릭 준비 메뉴(CounterTime > Play Mode Setup) 신설 (커밋 7325102). 이 과정에서 LiveMatchPanel 의 런타임 생성 캔버스에 GraphicRaycaster 가 빠져 있어 버튼이 하나도 안 눌리는 결함을 발견 — 처음엔 에디터 우회로 막았다.
브리핑을 본 사람이 5개 결정: m18 키 등록 해제(문구가 횟수를 명시해 오정보) / 습관 관측 알림 호출 시점을 결정 시점에서 실제 제출 시점으로 이동(사다리·카탈로그 무수정) / GraphicRaycaster 를 BuildCanvas 에 정식 추가하고 에디터 우회 제거 / MATCH_RULES §2 에 뜯기·잡기 유지 게이트 문서화 + 동시 공격 우선순위 "v1 은 B 선행, 미결" 명시 / "B 선행 — A 공격 불발" UI 표시 추가. 전부 반영 (커밋 54bd5be, e01d960).
세션 시작 전부터 미커밋이던 폰트(NotoSansKR SDF.asset)·ProjectSettings.asset 도 diff 로 섞인 변경 없음을 확인하고 사유 명시해 커밋.
200 → 202 harness green, fingerprint IDENTICAL 유지. Unity 에디터 0 에러/0 경고. 상세: docs/daily/2026-08-11-morning.md, docs/decisions/habit-observation-call-site-ownership.md, docs/troubleshooting/livematch-canvas-missing-graphicraycaster.md
2026-08-11 (밤, 2차 야간 자율 실행, unattended)
작업2~6 완료 (커밋 6bd5a06..a9614cb) — 서술 사전 9키 등록, 쿠즈시 §4.4 [초안] Core 규칙(플래그 기본 off), B 브레인 쿠즈시 선행 데코레이터, 6단계 LiveAssembler+입력 UI 스캐폴딩, off/on 스모크 실측 6회. 전체 요약은 위 "야간 자율 실행 2차" 항목, 상세는 docs/overnight-2026-08-11.md.
200/200 harness green, fingerprint IDENTICAL 전 구간 유지. Unity 에디터 0 에러/0 경고 매 작업 후 재확인(MCP 연결 밤새 유지).
판단 대기 5개 남김(문서 맨 위) — 손대지 않고 전부 문서화. 가장 중요한 것: LiveMatchRunner/LiveMatchPanel 은 Play 모드에서 아직 한 번도 실행 확인이 안 됐다 — 화면·클릭·레이아웃은 전부 사람이 아침에 볼 몫이다.
작은 자기 교정 1건: MATCH_RULES 첫 커밋(2e161b4)에서 버전 태그를 v3.6 에 두고 커밋했다가(스탬프 해시는 자동 갱신되지만 vX.Y 숫자는 사람 몫이라는 걸 놓침) 다음 커밋(b9618b2)에서 바로 v3.7 로 고쳤다.
2026-08-11
§9 osaekomi guard landed (a165872) then its fallout fixed (a036e75) — see "§9 guard + full green recovery" above for the full story. Also: dotnet-harness committed to tools/dotnet-harness (was scratchpad-only, rebuilt every session, per the old STATUS line this replaces); overnight's fingerprint IDENTICAL claims independently re-verified from a clean worktree rebuild, not just self-comparison; MATCH_RULES stamp confirmed current (v3.5, tools/stamp.sh --check, exit 0); this file updated.
Asked to update HANDOFF_session_2026-08-10.md's "현재 값" stamp block — that file does not exist anywhere in this repo's history (checked git log --all). Only md/HANDOFF_claude_code_runner.md and md/HANDOFF_claudecode_opponent-brain.md exist, and neither has a "현재 값" stamp block. Not touched; flagged instead of guessed.
2026-08-10 (overnight, unattended)
Step 4 (4b narration infra) and step 5 (osaekomi §9) landed, gated the same way stages 1–3 were: fingerprint diff + full test run after every change, revert on mismatch. See docs/overnight-2026-08-10.md for full detail and the open judgment call.
Tried adding TechniqueId directly to JudgementScored first — compiled, tests passed, but the fingerprint diff came back non-identical (the new field appears on every dump line), so it was reverted in favor of a Game-only pairing that leaves Core untouched. The revert-on-mismatch protocol did exactly what it was for.
Osaekomi §9 implementation surfaced one real interpretation gap in the rules doc (whose grip counts for a countered score's entry gate) — resolved it for waza-ari (no other option, table says it always enters) and deliberately left countered-yuko never entering osaekomi rather than guess. Flagged in code and in the overnight doc for a human decision.
Unity Editor + MCP bridge stayed connected the whole night with nobody at the machine — used it after every change to confirm 0 compile errors/warnings in the real editor, on top of the dotnet harness. Test Runner execution itself is still blocked over MCP (known, unchanged).
164 → 178 tests. Match length shifted after osaekomi landed (28 exchanges/156s → 26 exchanges/162s on the same realistic-A smoke profile) — recorded, not tuned; judged by a human after stage 6 per existing policy.
2026-08-10
Opponent brain stages 1–3. Isolated the scripted path behind interfaces, extracted the assembler, built habit data + observation ledger, then the live read resolver and the habit-driven commit source. 130 → 164 tests.
Every stage was gated on a fingerprint diff of the real MatchRunner over the mainline, and it stayed IDENTICAL throughout — including across a Core rule change. That is the whole reason 대조 모드 survived a rewrite of what feeds it.
Measurement drove three rule changes that no amount of reading would have found: habits fired 1–4 times a match and ladder rows 1–3 fired ZERO (added H4 잡기 노림), half of all judgements came from the fallback (fallback pokes only), and stamina bottomed out at ~30 exchanges because nothing called matte (MATCH_RULES v3.4 §4.3).
Two of my diagnoses were wrong and the instrumentation caught both before I "fixed" anything: row 2 was scoring normally, and retirement WAS happening (H4b firing is the proof). Dump before diagnosing.
2026-08-07
AI anim 4 clips generated (5cr each, 20cr total), mapped to 19 nodes. n10 = placeholder, mocap needed.
클립: ai_seoinage_entry / ai_osotogari_entry / ai_ukemi_back / ai_ground_pin. A·B 컨트롤러 양쪽에 State 로 들어가 있다.
NodeMotionMap 19개 노드 · 25개 State 를 AI 클립으로 교체. 던지는 쪽/당하는 쪽을 A·B 로 나눠 지정했다 — n41 은 B 가 누르는 쪽이라 A/B 가 반대다.
n10(허리후리기)은 ai_osotogari_entry 임시 대체다. 하라이고시 ≠ 오소토가리 — 모캡 대상.
n18/n20/n26/n28 은 종료 연출(Victory Idle / Dying / Defeated) 그대로 뒀다. n27/n29/n39/n40 은 아직 매핑 자체가 없다.
문서 작업 중 발견해서 고친 것: AI 클립 4종이 전부 Loop ON 이고 8개 State 에 나가는 트랜지션이 없었다 (생성 도구 기본값). 08-05 컨벤션대로 원샷 3종(seoinage/osotogari/ukemi)만 Loop OFF + 6개 State 에 Fight Idle 복귀 트랜지션(exitTime 0.9 / 고정 0.25초) 추가, ai_ground_pin 은 유지 동작이라 Loop ON·트랜지션 없음 유지. MCP 로 처리했고 A 컨트롤러 트랜지션 9개 전부 같은 파라미터·같은 목적지(Fight Idle)로 확인. Play 검증은 아직.

2026-08-05 (5)
노드↔모션 연결 구현. 43개 노드의 title/content 를 읽고 36개 매핑을 직접 채웠다 (A=파랑 주체, B=빨강 관점 구분). 10개 상태 전부 최소 한 번은 쓰인다.
Play 실측: 대표 11개 노드에서 CrossFade 대상이 매핑과 100% 일치, 원샷 자동 복귀(n9 +2.0s → Fight Idle), 유지 상태 지속(n14 +7.9s 에도 Victory Idle / Dying) 확인. 포즈 캡처로도 확인 — n14 는 A 서 있고 B 엎어짐, n41 은 정확히 반대.
함정 두 개 더: (1) MoveTo 직후 같은 호출에서 GetCurrentAnimatorStateInfo 를 읽으면 전부 이전 상태로 나온다. CrossFade 는 다음 프레임에 반영된다 — 코루틴으로 프레임을 넘겨야 한다. (2) Play 모드에서 찍은 Debug.Log 가 MCP GetConsoleLogs 에 안 잡힌다. 검증 결과는 파일로 써서 읽는 게 확실하다.
RunCommand 샌드박스는 System.dll 을 참조하지 않는다 — HashSet/ToHashSet 처럼 ISet 을 건드리는 코드는 CS0012 로 죽는다. List 로 우회할 것.

2026-08-05 (4)
레이아웃 시각 검증. 실제 결함 2개 잡아서 고침 — (1) Title rect 가 34px 인데 34pt 폰트는 41px 필요, 전 노드에서 rect 밖으로 그려지고 있었다 (TMP overflowMode=Overflow 라 눈에 안 띄었을 뿐, 2줄 제목이면 패널 밖으로 샜다). (2) 본문 최장 노드 n30 이 rect 97px 에 107.5px 로 렌더 — 오토사이즈가 22.4pt 에서 멈추고 더 안 줄어든다 (ForceMeshUpdate 6회 돌려도 수렴 안 함). ChoiceRow 를 줄여 TextPanel 을 190px 로 키워 해결.
캡처 방법 기록: ScreenCapture.CaptureScreenshot 은 에디터가 포커스를 잃으면 프레임이 안 돌아 파일이 안 생긴다. Application.runInBackground = true 를 **RunCommand 본문에서** 켜야 한다 — 코루틴 첫 줄에 넣으면 그 첫 프레임 자체가 안 돌아서 소용없다. 카메라→RenderTexture 로 직접 렌더하는 우회는 쓰지 마라: 폰트 아틀라스가 아직 GPU 에 안 올라가 한글이 전부 두부로 찍히고 캔버스 상태도 한 프레임 어긋난다.
또 하나: 노드를 옮긴 직후 같은 호출에서 버튼을 레이캐스트하면 못 맞춘다. 버튼이 그 프레임에 새로 생겨서 그렇다 — 이동과 클릭을 다른 호출로 나눠야 한다. 제품 결함 아님.
NotoSansKR SDF.asset 이 런타임에 글리프를 구워 넣으면서 커졌다(+6700줄). 정상 동작이고 커밋했지만, 새 문자가 나올 때마다 계속 변경된다.

2026-08-05 (3)
시나리오 러너 v1 구현. Core/Game 경계는 지켰다 — JsonUtility 는 Game 의 ScenarioLoader 안에만 있고 Core 는 UnityEngine 을 모른다.
Play 모드 테스트에서 실제 버그 하나 잡음: ScenarioUIController.ClearChoices 가 Destroy 만 부르는데 Destroy 는 프레임 끝에 처리돼서, 같은 프레임 안에 새 버튼을 만들면 옛 버튼이 레이아웃에 그대로 남는다 (테스트에서 버튼이 1→3→5→7 로 누적됐다). Destroy 전에 SetParent(null) 로 먼저 떼어내는 것으로 수정.
막힌 곳 두 군데 기록: (1) TMP Essential Resources 는 Play 모드에서 임포트가 안 된다 ("Cannot import package in play mode") — 에디터가 Play 중이면 먼저 세워야 한다. (2) RunCommand 코드는 Unity.AI.Assistant... 네임스페이스 안에 삽입되므로 UI 의 Image, UnityEditor.Compilation.CompilationPipeline 같은 짧은 이름은 Unity.* 쪽과 충돌한다 — 전체 경로로 써야 컴파일된다.

2026-08-05 (2)
Swapped the two greybox capsules for the Mixamo characters (Y Bot, Ch30_nonPBR), set all three FBX to Humanoid, looped Fight Idle, and built one Animator Controller per fighter. Done over MCP with Unity_RunCommand; console ends at 0 project errors. The only two console errors in the session came from Unity_Camera_Capture with an explicit cameraInstanceID — that tool cannot resolve a camera by instance ID here and always fails; capture with no argument (scene view) works and was used instead. Not a project problem, just a dead end to skip next time.

2026-08-05 (1)
Moved the scenario files into Assets/_Project/Scenario/ and made that the standing home for scenario assets (CLAUDE.md folder section updated). Built the Main.unity greyboxing stage plus two URP/Lit materials by hand-editing the scene YAML — the Unity editor was closed and the MCP bridge was down, so the usual "edit, then re-read the console over MCP" loop could not run. The scene YAML was checked structurally (29 unique anchors, no dangling local fileID references) but never imported by Unity. Console verification is deferred to the next Unity launch.

2026-08-03
Git/LFS setup finished and CLAUDE.md project rules locked in (Core/Game assembly split). Documented the session: daily log, two decision docs (assembly split, Git LFS), one troubleshooting doc (LFS after first commit), PAR materials, commit message drafts. Resolved both open questions above. Committed and pushed to origin/main.
Note: this session's docs were reconstructed from git log/diff and file timestamps, not from a live conversation record.

2026-08-02
Went through the full initial Unity project setup. Confirmed Unity MCP connection working end to end.
