# HANDOFF — Core를 러너에 물리기 (클로드 코드용)

> 이 문서는 **작업 지시서**다. 설계 판단은 이미 끝났다. 판단이 필요해 보이면
> 임의로 정하지 말고 **멈추고 물어볼 것.**

---

## 0. 읽는 순서 · 권위

1. `md/MATCH_RULES.md` — 규칙 단일 출처. **코드와 충돌하면 이 문서가 이긴다.**
2. `md/CORE_DESIGN_MatchEngine.md` — 구현 설계
3. 이 문서 — 이번 패스 작업 지시

두 문서 1행의 `<!-- v3.2 | body:xxxxxxx -->` 스탬프를 **지우지 말 것.**
검증: 1행을 제외한 본문에서 CR 제거 후 `git hash-object` → 스탬프와 일치해야 한다.
- `MATCH_RULES.md` → `e11de04`
- `CORE_DESIGN_MatchEngine.md` → `a24d23c`

```bash
tail -n +2 md/MATCH_RULES.md | tr -d '\r' | git hash-object --stdin
```

---

## 1. 환경

| 항목 | 값 |
|---|---|
| Unity | 6.3.21f1 LTS / URP |
| 리포 | `C:\project_2026\CounterTime` |
| 브랜치 | `feat/match-engine` |
| Core 경로 | `Assets/_Project/Scripts/Core/` |
| Core 테스트 | `Assets/_Project/Core/Tests/` |
| Game 경로 | `Assets/_Project/Scripts/Game/` ← **Game/Scripts 아님** |
| asmdef 격벽 | `CounterTime.Core`(순수 C#) / `CounterTime.Game`(UI·표현) |

새 클론이면 1회: `git config core.hooksPath tools/githooks`
(커밋 불가한 로컬 설정이라 자동화 안 됨. 안 하면 스탬프 훅이 조용히 아무것도 안 한다.)

---

## 2. 이번 패스의 목적

**엔진이 한 번도 게임으로 돌아간 적이 없다.**
106개 EditMode 테스트는 통과했지만, 엔진은 테스트 안에서만 산다.
이번 패스의 산출물은 "예쁜 화면"이 아니라 **한 경기가 끝까지 굴러가는 것 + 42노드 대조 로그**다.

---

## 3. 절대 금지 (이번 패스 한정)

| 금지 | 이유 |
|---|---|
| `Core/`에 `UnityEngine` 참조 추가 | 순수성 규칙. 현재 참조 0개 — 유지 |
| `MATCH_RULES.md`의 숫자 변경 | 되치기 임계 −3은 ⓐ로 확정됨. 42노드 로그 나온 뒤 재검토 |
| 판정 로직을 Core 밖에서 재구현 | 판정은 `JudgementCalculator` **한 곳뿐** |
| 수비 자세를 인라인 재파생 | `JudgementCalculator.IsDefensivePosture` 한 곳뿐. 이미 한 번 버그로 잡힌 항목 |
| 연출(대파열/히트스톱) 이식 | 3순위. 로그가 먼저다 (5단계 참조) |
| `NodeMotionMap`, 시나리오 러너 기존 코드 개편 | 이번 패스 범위 밖 |
| 심판 누산기를 화면에 숫자로 출력 | §6.1 "NEVER shown as a number" |
| 예비동작(소매/깃/뒷깃 노림)을 HUD에 표시 | 수싸움이 소멸한다. 확정된 그립만 HUD 허용 |

---

## 4. 작업 단계

각 단계 끝에 **커밋**. 커밋 메시지는 각 단계에 적어뒀다.
한 단계가 끝나면 **다음 단계로 자동 진행하지 말고 결과를 보고할 것.**

---

### 1단계 — `OralMatchReplayTests` 확인 ✅ 완료

전부 초록불 확인됨. **엔진이 이미 한 경기 분량을 완주할 줄 안다.**
따라서 2단계는 "없는 걸 새로 만드는" 게 아니라 **이미 도는 리플레이에 뷰를 붙이는** 작업이다.

먼저 할 것: `OralMatchReplayTests.cs`를 열어 **엔진을 어떤 순서로 호출하는지** 읽을 것.
러너는 그 호출 순서를 그대로 따라가면 된다. 새로 설계하지 말 것.

---

### 2단계 — `ScenarioExchangeScript`

**파일:** `Assets/_Project/Scripts/Game/Match/ScenarioExchangeScript.cs`
**어셈블리:** `CounterTime.Game` (Core 아님 — JSON 파싱이 들어가면 순수성이 깨진다)

**하는 일:** 시나리오 노드 → 엔진에 **읽기 값(ReadLevel)만** 공급.

**착수 전 필수:**
- Core의 `ExchangeScript` 실제 정의를 먼저 읽을 것. 인터페이스인지 추상 클래스인지, 메서드 이름과 시그니처가 뭔지.
- `OralMatchReplayTests`가 `ExchangeScript`를 어떻게 채워 넣는지 확인. 테스트가 쓰는 방식이 정답이다.
- 아래 스케치는 **모양만** 보여주는 것이다. 실제 정의를 따를 것.

```csharp
// 노드 데이터를 읽어 엔진에 "읽기 값"만 공급한다.
// 판정은 여기서 절대 하지 않는다.
sealed class ScenarioExchangeScript : ExchangeScript {
    ScenarioNode _node;
    public void SetNode(ScenarioNode n) => _node = n;
    public ReadLevel ReadFor(FighterId f) => _node.ReadOf(f);
}
```

**함정 — 검증을 중복하지 말 것:**
§2 게이트(`카운터 준비` 선언자를 공격자로 지정하면 무시)는 **엔진이 이미 강제한다.**
어댑터에서 또 검사하지 말 것. 두 곳에서 검사하면 나중에 한 곳만 고치는 사고가 난다.

**완료 조건:** 컴파일 클린. 기존 106개 테스트 여전히 전부 초록.
**커밋:** `feat(game): ScenarioExchangeScript — scenario node → ExchangeScript seam`

---

### 3단계 — `MatchRunner`

**파일:** `Assets/_Project/Scripts/Game/Match/MatchRunner.cs` (MonoBehaviour)

**설계 제약 — 얇게:**
- **상태를 소유하지 않는다.** 점수·체력·시계·그립을 러너의 필드로 들지 말 것. 전부 엔진에게 묻는다.
- 러너가 하는 일은 셋뿐: 입력 수집 → `engine.ResolveExchange(a, b, script)` 호출 → 반환된 이벤트를 화면/로그로 옮김.

```csharp
void ResolveOneExchange(TurnChoice a, TurnChoice b) {
    _script.SetNode(_currentNode);
    var events = _engine.ResolveExchange(a, b, _script);
    foreach (var e in events) _presenter.Play(e);   // 화면 반영은 여기서만
}
```

**입력:** UI 만들지 말 것. **키보드 임시 입력**으로 충분하다
(`BurstFeelTest.cs`가 1/2/Q/W/E/R 쓰던 방식). 목적은 손맛 검증이지 UI 완성이 아니다.

**완료 조건:** Play 모드에서 시나리오 메인라인(m1→m4→m8→m9→m13→m14→m15→…→m36)이 끝까지 진행되고 `MatchEnded` 이벤트가 나온다.
**커밋:** `feat(game): MatchRunner — drive MatchEngine from scene`

---

### 4단계 — `JudgementScored` CSV 덤프

**임시 코드로 충분하다.** 나중에 지울 것을 전제로 짜도 된다.

`JudgementScored{sum, terms[]}` 이벤트를 받아 파일로 덤프:

```
node,grip,read,stamina,tech,posture,sum,judgement,scoredBy,isCounter
m13,2,3,0,2,0,7,Ippon,A,false
m14,-1,2,0,1,0,2,Yuko,A,false
```

**출력 위치:** `Assets/` 바깥. 리포에 커밋되지 않는 곳(`Temp/` 또는 프로젝트 루트의 gitignore된 경로).

**완료 조건:** 한 경기 완주 시 CSV가 노드 수만큼 행을 갖는다.
**커밋:** `chore(game): temp CSV dump of JudgementScored for node audit`

---

### 5단계 — 42노드 대조 ★ 이번 패스의 핵심

`counter-time-match-scenario-v2.json`의 각 노드 `[판정식]` 줄 ↔ 4단계 CSV를 대조.

**보고 형식 — 불일치만, 표로:**

| 노드 | 항목 | 문서 값 | 엔진 값 |
|---|---|---|---|
| m21 | read | +2 | 0 |

**중요 — 임의로 고치지 말 것:**
- 불일치가 나오면 **어느 쪽이 맞는지 판단하지 말고 목록만 보고**한다.
- 문서가 틀렸을 수도, 엔진이 틀렸을 수도, 어댑터가 틀렸을 수도 있다.
- 규칙 문서가 이기는 게 원칙이지만, **문서 자체의 계산 실수**일 가능성이 있어 사람이 봐야 한다.

**커밋:** `docs: 42-node audit result (mismatch list)` — 코드 수정 없이 결과만.

---

### 6단계 — 최소 HUD

**§7의 하드 요구다. 표현 취향이 아니다.**
320×180에서 손은 5.8px. 접촉이 있다는 건 보이지만 뭘 잡았는지는 안 보인다.
HUD 없으면 뜯기(§3.2)와 부위 지정 그립이 3분의 1 찍기로 퇴화한다.

**표시할 것 — 원해상도, RenderTexture 바깥 레이어:**

| 항목 | 형태 |
|---|---|
| 그립 수치 | −2 ~ +2 |
| 락 상태 | 손별로 Free / Held / 견고 |
| 잡은 부위 | 오른소매 / 왼가슴 / 뒷깃 … 텍스트 |
| 시계 / 점수 / 지도 수 | 숫자 |

**표시 금지:**
- 심판 누산기 숫자 → tell 이벤트(`RefereeTellChanged`, 70%/90%)로만
- 예비동작

**부위 정보는 Core 이벤트에서 받을 것.** 애니메이션에서 역추론하지 말 것 —
§7이 "Core must emit grip-part state as explicit events"라고 못 박았다.

**완료 조건:** 한 경기 도는 동안 그립 수치·락·부위가 실시간으로 맞게 갱신된다.
**커밋:** `feat(game): minimal grip HUD (score, lock, held part)`

---

### 7단계 — `md/STATUS.md` 갱신 (영문)

기록할 것:
- 러너 연결 완료, 메인라인 완주 여부
- 42노드 대조 결과 요약 (일치 N / 불일치 M, 불일치 목록은 별도 표)
- 남은 것

`md/STATUS.md`는 **유일본**이다. 새 상태 문서를 만들지 말 것.
`CLAUDE.md` 9행에서 `@md/STATUS.md`로 임포트되고 있다.

**커밋:** `docs: update STATUS with runner integration + node audit`

---

## 5. 완료 후 사람이 판정할 것 (코드로 안 나옴)

러너가 돌면 사람이 직접 플레이하며 아래를 본다. **클로드 코드가 판단하지 말 것.**

| 항목 | 무엇을 보나 |
|---|---|
| 4분 체감 | 교환 3~5초 × 시퀀스 6~8개로 240초가 차나 |
| 소극 압박 | tell만 보고 "움직여야겠다"가 오나 (지도 받고 나서 알면 실패) |
| 체력 death spiral | 조여드나, 그냥 답답하나 |

---

## 6. 조용히 빠지는 함정 (에러가 안 나서 못 알아챈다)

- 문서 사본이 디스크와 어긋난 채 그 위에 작업 → **스탬프 먼저 검증**
- 훅 하드코딩 경로가 문서 이동 후 매치 실패 → 무성 스킵
- `core.quotePath` 기본값이 한글 경로를 이스케이프 → 무성 스킵
- 규칙을 리포가 아닌 어시스턴트 메모리에만 기록 → 디프에 안 보임
- **규칙 정의를 여러 곳에서 인라인 재파생 → 한 곳만 고치면 나머지가 남음**

---

## 7. 곁다리 (여유 있으면)

- `Main.unity` → `Near Miss Seconds`가 `2`면 잔재. **확정값은 `0.5`.** 고쳐 저장
- `BurstFeelTest.cs` 정리는 아직 하지 말 것. 지울 때 순서: **씬에서 오브젝트 먼저 삭제 → 그다음 파일 삭제.** 반대로 하면 Missing Script

---

## 8. 미결 — 정하지 말고 남겨둘 것

- `CounterNearMiss` 발화 조건 (sum 정확히 −2 한정 안이 유력) → **42노드 로그가 나오면 −2가 몇 번 뜨는지 숫자로 보인다. 그거 보고 사람이 정한다**
- 뜯기 성공률 훈련 보정
- 굳히기 탈출 판정
