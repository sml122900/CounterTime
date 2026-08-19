<!-- v4.3 | body:7ccb523 -->
# MATCH_RULES.md — COUNTER TIME judo match system (v4.3)

Single source of truth for match logic. `CounterTime.Core` implements THIS file;
if code and this file disagree, this file wins. All numbers are v1 tunables unless
marked FIXED. Korean technique/UI terms are kept as-is (UI language is Korean).

Companion docs: `CORE_DESIGN_MatchEngine.md` (implementation design),
`OPPONENT_BRAIN.md` (where ReadLevel comes from once B is an algorithm),
`counter-time-match-scenario-v2.json` (42-node reference match on the board format;
its `[판정식]` lines are computed with the formulas below and double as test vectors).

---

## 1. Match format (FIXED — follows 2025 IJF SOR)

- Regulation time **4:00**. Clock **stops during matte (그쳐)**; osaekomi runs in real time.
- Scores: **Ippon(한판) > Waza-ari(절반) > Yuko(유효)**.
  - Waza-ari ×2 = Ippon (awasete-ippon).
  - **Yuko NEVER converts upward**, no matter how many. 10 yuko < 1 waza-ari.
- Osaekomi (굳히기): **5 s = Yuko, 10 s = Waza-ari, 20 s = Ippon.** If holder already
  has a waza-ari, 10 s ⇒ cumulative ippon.
- **Shido(지도) 3 = hansoku-make** (loss). Shido never awards points to the opponent
  before that.
- Win resolution at 4:00: ippon > waza-ari count > yuko count > **golden score**.
- **Time-up is checked AFTER an exchange fully resolves** (judgment, score,
  stamina, shido). An exchange that began before 4:00 is played out and its score
  counts — this matches IJF, where a technique started before time is valid.
  Overshoot past 240 s is discarded, not carried into golden score.
- **Golden score**: unlimited time, first score of any kind (yuko included) wins
  immediately; third shido loses.
- 2025-legal techniques exist in the pool: 소매 안쪽 잡기, 말아업어치기, head-assisted
  attack/defense. (말아업어치기 is opponent B's hidden surprise tech in match 1.)

## 2. Turn structure

A match is a sequence of **exchanges (교환)**; each exchange is one simultaneous
commit by both fighters. Per-turn choice =

| Slot | Options | Notes |
|---|---|---|
| Hand — **손 → 오른손\|왼손\|회피\|뜯기 (확정, v4.2/v4.3 — §2b)** | 오른손/왼손을 고르면 그 손으로 소매/가슴깃/틀어잡기 3종 × 정/교차 중 하나(빈 손) + 이미 잡았으면 견고화(§2b) 추가. 회피를 고르면 손별로 방어 존 1개씩, 총 2존을 지정한다(§2b). 뜯기(v4.3, 잡힌 부위가 있을 때만)를 고르면 부위→방법 2단으로 이어진다(§2b 뜯기 절 참조). | `GripRevisionGate` 플래그 뒤(기본 off, `BalanceGate`/`PromotionGate`와 같은 계약 — 꺼지면 §2a[구식]가 그대로 적용된다). 명령 안 한 손은 자동으로 그립을 유지한다(§6.1 미신고로 안 잡힘) — v4.0 그대로. |
| Hand — 흔들기 (확정, v4.0 — §4.4) | 상대 밸런스(§S)를 깎아 다음 교환의 큰기술에 정당성을 준다 | `BalanceGate` 플래그 뒤(구 `SetupGateEnabled` — 2026-08-11 오후 세션에서 `Kuzushi*`→`Setup*`로 이미 개명됐던 그 플래그. `SetupGateEnabled`→`BalanceGate` 개명은 §H 3단계에서, §S 참조). §2b의 그립 매트릭스와 독립 — 손 슬롯은 같지만 서로 다른 코드 경로(게이트도 다르다). 자기 그립이 하나라도 있어야 선언 가능. 판정 미발화(견제 발기술과 동급), 별도 상성표 rung 없음. |
| Core | 공격적 / 중립 / 수비적 | §2b에서 그립 매트릭스가 이 값을 직접 읽는다(가슴깃 노림·같은 쌍 맞잡기·공수 갈림) — 아래 §2b 참조. |
| Legs (발기술) | 공격(= **technique selection**, confirmed v4.0 — §A.2/§B) / 중립 / 수비 / 카운터 준비(+ §D.1 예측 입력) | Legs act even while gripping. A technique-declare turn carries no Core/Hand input that turn and is treated as Aggressive posture (§A.2). |

**동시 공격 우선순위 [v1 임시 규칙].** 한 교환에는 공격자가 하나뿐이다 — A가 Legs=공격을
선언한 같은 교환에 상대(B)도 기술을 낸다면, v1은 **B가 항상 선행**한다: A의 공격은 그
교환에서 성립하지 않는다(판정 자체가 안 열린다). 선제권을 별도로 판정할지, 다른 해소
규칙(예: 판정으로 흡수)을 둘지는 **미결**이다.

**극단적 방어** = hand 회피 + core 수비 + legs 수비 (all-in defense). Special
handling in referee (§6) and stamina (§5).

**카운터 준비 (counter-ready)** — legs slot only, and the point where the player
*decides* to counter rather than receiving one by accident. Gate: that fighter
cannot enter a technique this turn — no 기술 진입, no judgment fired by them, and
no technique of their own resolved this exchange. Costs no stamina. If the
opponent does not attack, the turn is spent for nothing and the passivity
accumulator takes +2 (§6.1); that risk is the price of the gamble. Judgment
effect: see §4.2.

Time cost per action (clock only advances on actions; matte = stop):

| Action | Seconds |
|---|---|
| Grip turn (hand exchange) | 5 |
| Technique attempt / defense exchange | 3 |
| Tear attempt (뜯기) | 3 |
| Osaekomi | real time held |

Both fighters commit simultaneously, so the clock advances by
**max(cost_A, cost_B)** per exchange — the slower commitment sets the length of
the exchange. (Grip turn 5 vs technique 3 ⇒ the exchange costs 5.)

Target feel: one hajime→matte sequence = 30–60 s; 6–8 sequences per regulation match.

## 3. Grip system

### §2a — grip resolution before v4.2 (kept, `GripRevisionGate` off)

#### 3.1 Grip score (잡기 수치) — FIXED range −2…+2
Each fighter's hold value: normal grip (sleeve/lapel) = 1, dominant grip
(틀어잡기 or 등싸잡기) = 2. Side total capped at 2.
**gripScore(A) = capped(A holds) − capped(B holds)**, clamped to [−2, +2].
Perspective flips sign for B.

#### 3.2 Grip lock states (per hand held by opponent)
`Free → Held → Locked(잠김)`

- Opponent grabs your hand/sleeve ⇒ that connection is **Held**.
- **Tear (뜯기)** resolution is a simultaneous mind-game: your `뜯기` vs opponent's
  choice that turn.
  - Opponent did NOT pick `잡기 유지` ⇒ tear succeeds ⇒ Free.
  - Opponent picked `잡기 유지` (they read you) ⇒ tear fails ⇒ connection becomes
    **Locked**.
- **Locked**: tear is impossible. While **either** of your hands is Locked, your
  *effective* grip score for judgment and stamina is `min(gripScore, −1)`.
  Harsh but not a dead end: 위장 공격 (§6.3) always remains as the exit.
- Only escape from Locked: **feint attack** (§6.3) forcing matte — all grips reset
  to Free on matte.
- Training can unlock special tear moves (higher tear success vs `잡기 유지`);
  hook exists, values TBD.

**Naming note (v4.2)**: this section used to gloss `Locked` as "(견고)" in Korean.
§2b below gives 견고(화) a different, unrelated meaning (reinforcing a hand you
already hold) — the gloss here is renamed to "(잠김)", matching what the live HUD
already displayed (`LiveMatchPanel`), to avoid the two ideas reading as the same
thing. No behavior changed, ASCII/code identifiers (`GripLock.Locked`) untouched.

### §2b — grip resolution, v4.2 확정 (`GripRevisionGate`, 기본 off)

**대상 3종 × 정/교차.** 정잡기(내 오른손→상대 왼쪽) = mirror, 교차잡기(같은측) =
cross — 손·타겟 조합 하나로 이미 표현된다(별도 축 아님). 뜯기는 **§2b 뜯기
(v4.3 확정)**(아래)로 구현됐다 — §2a의 옛 Lock/Maintain 2택 뜯기는 게이트 off일
때만 여전히 유효하고, 이 절 흐름과는 완전히 별개 경로다.

| 대상 | 가치 ⚙ | 비고 |
|---|---|---|
| 소매 | 1 | §2a의 Normal과 값이 같다 (`GripKind.Normal`) |
| 가슴깃 | 2 | §2a의 Dominant와 값은 같으나 별개 개념(`GripKind.Lapel`) — 아래 매트릭스가 따로 다룬다 |
| 틀어잡기(뒷깃, 조지아그립) | 3 | §2a의 "conversion" 전제조건(다른 손이 이미 잡고 있어야 함) 없음 — **빈 손에서 바로 선택 가능** (v4.2가 §2a를 대체하는 지점) |
| 견고화 | +1 | 이미 잡은 손에만. 손이 갱신(재지정)되면 사라진다. |

Side total 캡 ⚙4(한 손이 낼 수 있는 최댓값 = 틀어잡기+견고). 최종
`gripScore = clamp(capped(mine) − capped(theirs), −2, +2)` — §2a와 같은 모양,
숫자만 갱신.

**해결 매트릭스 (양쪽 커밋을 함께 본다):**

1. **가슴깃 노림** — 상대 Core가 공격적이면 무조건 성공, 수비적이면 무조건 실패.
   중립은 "수비 아님"으로 읽어 성공 쪽(⚙, 문서에 명시 안 된 갭 — 구현 세션의 판단).
   다른 기(모든 다른 손 목표)와 무관하게 매 시도마다 독립적으로 이 규칙만 적용된다.
2. **같은 쌍 맞잡기** (가슴깃이 아닌 두 시도가 같은 급(소매/소매, 틀어잡기/틀어잡기)일 때):
   - 둘 다 공격 → 상호 성립(순변화 0, GripScore 차감 상쇄로 자연히 나온다).
   - 둘 다 수비 → 상호 실패. 연속 ⚙2회 반복되면 양쪽에 지도(§6과 별개 경로 —
     §6.2 문턱 축적기를 안 거치고 바로 지도).
   - 갈림(하나만 공격적) → 상호 성립하되 ⚙10%로 공격측만 성립 + 즉시 견고화(+1).
3. **서로 다른 손 목표** (급이 다르거나 가슴깃이 이미 걸러진 나머지) — 둘 다 성립.
   갈림이면 2번과 같은 ⚙10% 솔로 롤이 적용된다.
4. **방어된 존** — 상대가 그 존을 회피로 지정했으면 확률 0. 회피만 반복하면
   기존 §6.1 사다리(Hand==Evade, +2 rung)로 자연히 흡수된다 — 새 규칙 아님.
5. **노그립 즉시 기술** — 상대 Core가 공격적이면 판정 성공률 보정↑, 수비적이면
   희박(⚙). **v4.2 1차 구현에는 반영되지 않았다** — 판정식(§4)·승격(§E) 어느
   항을 조정하는지 특정이 안 돼 있어(코드 접점이 불명확) 관측만 남긴다. §H
   이후 §4/§E 논의 시 재검토.

**회피 (v4.2)** — 손마다 존 1개, 총 2존. 존 6개: 왼소매/오른소매(손 전용)·
왼가슴깃/오른가슴깃(양손 공용)·왼상단/오른상단(손 전용, 틀어잡기 방어). 왼손이
고를 수 있는 4개 = {왼소매, 왼가슴깃, 오른가슴깃, 왼상단}, 오른손 4개 =
{오른소매, 왼가슴깃, 오른가슴깃, 오른상단}. 양손이 같은 존을 중복 지정할 수
없다(선택 UI 단의 제약).

**교차잡기 롤링 타이머** — 교차 방향으로 잡기가 확립되면(그 즉시는 안 깎임)
타이머 ⚙2턴 시작. 매 교환마다: 그 턴에 흔들기를 선언했거나 실제 기술 시도를
했으면 타이머가 ⚙2로 리셋, 아니면 1씩 감소. 0에 닿으면 즉시 지도 후 타이머가
다시 ⚙2로 재시작(지도 후에도 계속 교차를 유지하면 또 돈다 — 하드 캡 없음).
정(mirror) 방향 잡기는 이 타이머와 무관하다.

**견고화** — 이미 잡은 손을 대상으로 선택하면 +1(위 표). 확률·조건 없음(항상
성공, 잡은 게 있으면). 그 손을 다른 대상으로 재지정하면 사라진다.

### §2b 뜯기 (v4.3 확정, `GripRevisionGate`)

**진입.** 손 스테이지1의 예약 슬롯 "뜯기"는 내 부위 중 잡힌 곳이 1곳 이상일
때만 목록에 나타난다. 없으면 목록 자체에서 뺀다(배치 불변식 유지가 목적 —
항목 개수가 3↔4를 오가도 극좌표 배치가 매번 다시 계산되므로 저절로 유지된다).

**흐름.** 뜯기 → 잡힌 부위 목록(실제 잡힌 것만 동적, 견고 여부 병기: 예
"오른소매(견고)") → 방법 선택. 틀어잡기(상단) 존은 목록엔 표시되지만
비활성이다 — 틀어잡기 대응 서브게임은 3차 이월, 범위 밖.

**소매 방법 3종:**

| 방법 | 비견고 | 견고 |
|---|---|---|
| ① 돌려서 뜯기 | 성공 | 실패 |
| ② 팔꿈치로 뜯기 | 성공 | ⚙50% 성공 / ⚙50% 비견고화(견고 보너스만 제거, 그립 자체는 유지) |
| ③ 다른 손으로 뜯기(내 반대 손 자유일 때만) | ②와 동일 판정 | ②와 동일 판정 |

**가슴깃 방법 3종:**

| 방법 | 비견고 | 견고 |
|---|---|---|
| ① 한손 뜯기 | 성공 | 실패 |
| ② 양손 뜯기(내 양손 자유일 때만) | 성공 | ⚙50% 성공 / ⚙50% 비견고화 |
| ③ 양손 툭툭 내리고 뜯기(내 양손 자유일 때만) | ②와 동일 판정 | ②와 동일 판정 |

각 방법의 급 3종(소매①②③, 가슴깃①②③)은 존 타입에 따라 라벨만 다를 뿐 판정
구조는 동일 — Basic(①)=견고 시 결정론적 실패, Forceful(②)=견고 시 ⚙50%/50%,
Assisted(③)=Forceful과 같은 판정 + 성공 시 되잡기가 성립한다.

**소결정 (a) — 뜯으면서 잡기.** ③(소매·가슴깃 공용)이 성공하면, 그 손(소매는
반대 손, 가슴깃은 존의 명목상 같은 쪽)이 상대의 (방금까지 쥐던) 팔을 단순
소매잡기로 되잡은 상태로 성립한다. 정/교차는 새로 계산하지 않고 기존
정/교차 판정(§2b 위 절)이 실제 존 기준으로 그대로 적용된다 — 예: 내 오른소매를
잡던 상대 왼손 → 내 자유 왼손이 그 왼소매를 잡으면 교차. 교차로 성립하면
교차잡기 롤링 타이머도 그대로 적용(같은 교환에 즉시 시작).

**소결정 (b) — 방법 취득.** 훈련 연동은 3차 이월 — 2차는 전 방법 기본 개방
(승급·해금 없음).

**소결정 (c) — 뜯긴 존 재잡기 쿨다운(사용자 확정).** 뜯기 성공으로 해제된 존은
"그 교환 + 다음 ⚙1교환"(`TearRegripCooldown`, 기본 1) 동안 원래 홀더가 그 존을
재잡기 시도해도 매트릭스 진입 전에 자동 실패한다 — 위 §2b 해결 매트릭스의 모든
갈래(가슴깃 노림·같은 쌍 맞잡기·서로 다른 손 목표 등)보다 먼저 걸리는 우선
규칙이다. 대상 손을 바꿔 시도해도(예: 오른소매를 왼손으로) 막힌 것은 존
자체이므로 마찬가지로 막힌다. 소결정 (a)로 성립하는 되잡기는 매트릭스를 거치지
않는 별개 경로라 이 쿨다운의 적용 대상이 아니다. 그쳐(matte)가 불리면 모든
그립이 리셋되는 것과 같은 타이밍에 쿨다운도 함께 풀린다.

**엔진.** 뜯기는 그 턴의 손 액션 1개(§2 시간·비용표 그대로, 결과 무관 항상
스태미나 지불). 성공 시 그립이 완전히 해제되고(§3.2 잠김 캡도 함께 풀린다),
"비견고화" 결과는 견고 보너스만 제거하고 그립 자체는 유지한다. 50% 확률은
기존 `IRandomSource` 규약과 동일 방식(`FixedRandomSource` 로 결정론적 테스트).

**B 최소 대응(기본 포함).** B 폴백은 잡힌 부위(소매/가슴깃만, 틀어잡기는
3차 이월이라 후보에서 제외)가 있으면 ⚙확률(기본 30%)로 기본 뜯기(소매=①
돌려서, 가슴깃=①한손, 즉 항상 `TearMethod.Basic`)를 시도한다 — 방법 선택
지능(견고 여부를 보고 ②·③을 고르는 판단)은 3차 이월.

**내레이션·상태창.** 뜯기 결과(성공/실패/비견고화)는 교환 로그에 반영되고,
상태창의 "잡힘" 표시(§2b 상태창 절)는 매 리프레시마다 그립 상태를 라이브로
다시 읽으므로 뜯기 성공 시 자동으로 갱신된다(별도 배선 불필요).

## S. Balance (밸런스) — third resource axis (v4.0)

A third fighter resource alongside grip (§3) and stamina (§5): **balance / center
stability**. Range ⚙ (proposed 0–100, 100 = untouched). The axis itself is confirmed;
the range and every rate below is a v1 tunable (`MatchRulesConfig` candidate), not a
fixed number — marked ⚙ throughout.

**명칭 (v4.1, 확정).** 최초 개정본은 이 자원을 "Stance / 스탠스"라 불렀으나, §4.3/§9.4에
이미 있던 Core 축의 공격적/중립/수비적 *선택*(코드상 `CoreStance` enum)과 이름이 겹쳐
혼동을 낳았다. v4.1부터 문서 전체에서 이 자원은 **밸런스 / Balance**로, Core의 그 선택은
**자세(Core)**로 구분해 부른다 (§4.3/§9.4 참조). 코드 식별자도 `Stance`가 아니라
**`Balance`**로 잡는다 — 기존 `CoreStance` enum과의 충돌을 피하는 게 목적이다. 실제 코드
개명(`SetupGateEnabled`→`BalanceGate` 포함)은 §H 3단계(밸런스 축 구현)에서 이뤄진다 —
이 문서 갱신 시점의 코드는 여전히 옛 이름 그대로다.

**Drains on:**
1. Landing a 흔들기 (§4.4) — draining the opponent's balance is 흔들기's entire job.
2. A 견제성 발기술 (poke-kick) that lands (§D.4's floor case).
3. Any genuine attempt (realAttempt) that fires judgment but scores no point still
   drains the opponent's balance by ⚙ — a real threat that did not convert to a score.
   (원문: "포인트는 못 냈지만 위협이 된 공격" — 판정이 발화한 진짜 시도가 무득점으로
   끝나도 상대 밸런스를 깎는다.)

**Recovers (v4.1, 확정 — §8 판단 대기 #1 해소).** +⚙ on a successful defense. **그쳐
(matte, §4.3) / 굳히기 탈출(§9.4)에서는 체력과 같은 방식으로 부분 회복만 한다** — 체력의
`MatteRecovery`(§5.3, +3)와 같은 성격의 별도 튜너블 ⚙이며, 스태미나 회복 트랙과는 독립이다.
**완전 리셋(100)은 경기와 경기 사이에만 일어난다.** §4.3/§9.4의 "자세(Core) 리셋" 행은
Core 축 선택을 Neutral로 완전히 되돌리지만, 그 옆에서 밸런스는 이 절의 부분 회복 ⚙만 받을
뿐 값 자체가 100으로 리셋되지는 않는다 — 두 리셋은 별개의 트랙이다.

**Principle — the §4 judgment formula is not touched.** Balance reaches judgment
through exactly three paths, never as a new term of its own: the big-throw gate
(§4.4, via `TechniqueClass`), the feint criterion (§A.4), and the 견제성 발기술
special case's floor/promotion scaling (§D.4). §4's sum, brackets, and `terms[]`
are unchanged by this section.

Balance changes are themselves deterministic — no dice roll (§E.2).

**HUD:** shown exactly, on the same tier as grip (§7). Unlike a tell (예비동작),
balance is a confirmed number and is not information withheld from the player.

## 4. Judgment formula (판정식)

Evaluated at the moment a throw attempt resolves. Sum five terms
(all measure "how defenseless was the opponent"):

| Term | Values |
|---|---|
| Grip | effective gripScore, −2…+2 |
| Reading (수싸움) | full read **+3** / read **+2** / neutral **0** / got read **−3** |
| Opponent stamina | ≥50 → **0** / 25–49 → **+2** / <25 → **+3** |
| Technique class | signature **+2** / standard **+1** / sloppy·feint **0** |
| Opponent posture | opponent in **defensive stance** (defined below) → **−2** |

| Sum | Result |
|---|---|
| ≤ 1 | No point (노포인트) |
| 2–4 | Yuko |
| 5–6 | Waza-ari |
| ≥ 7 | Ippon |
| **≤ −3** | **Counter (되치기)**: judgment goes to the opponent with score \|sum\| on the same brackets |

**Counter settlement.** The countered attacker has ALREADY paid the big-throw
cost including the grip multiplier — force spent, thrown anyway, which is the
real sensation. The countering side pays the flat counter cost (2, never
multiplied, §5.1) only once the counter judgment is confirmed. A counter with
|sum| ≤ 1 is `None`: nobody scores and nobody pays.

**Defensive stance, defined.** The posture term is binary. It asks whether the
opponent handed you a throwable posture, NOT how deeply they turtled.

- Defensive stance ⇔ the opponent's **Core == Defensive** this turn. Legs and
  hand do not enter the test.
- `극단적 방어` therefore scores −2, exactly the same as a plain defensive core.
  Depth of turtling is already charged twice — passivity accumulator (§6.1) and
  the −1/turn extreme-defense drain (§5.2) — and is not charged a third time here.
- Legs == 수비 alone is NOT a defensive stance. Hand == 회피 alone is NOT a
  defensive stance.
- **Exception (§4.2): a fighter who declared 카운터 준비 is never in a defensive
  stance**, even with a defensive core. Counter-ready is a commitment, not a
  shell, and it already pays its own price in §6.1.

Reading levels, operationally: *full read* = you predicted the opponent's exact
commitment and they were mid-commitment into you (e.g. reaching for a grip, mid
rotation); *read* = correct anticipation; *got read* = your attempt was predicted.
**Core never derives ReadLevel.** It is supplied per exchange by the
commit-source layer and Core only consumes it — this is unchanged from v3.2, but
the supplier is no longer only the scenario. Derivation rules (habit observation,
player-tendency window, scouting) live in `OPPONENT_BRAIN.md`. The scripted
regression path (대조 모드) keeps supplying it from scenario nodes.

### 4.1 Canonical validations (MUST pass as unit tests — from the designer's oral match)
1. **Attack spam vs turtled opponent**: grip+2, read 0, stam 0, standard +1,
   posture −2 ⇒ **1 ⇒ no point** (only shido pressure results).
2. **빗당겨치기 on rushing opponent**: grip −1, read +2, stam 0, standard +1
   ⇒ **2 ⇒ Yuko**.
3. **Signature 틀어잡고 밭다리 on opponent reaching for a grip**: grip +2, full
   read +3, stam 0, signature +2 ⇒ **7 ⇒ Ippon** (no stamina bonus needed).
4. **Counter-ready lands**: opponent attacks at grip −1 into counter-ready.
   grip −1, read Neutral→**GotRead −3**, stam 0, standard +1, posture open
   (counter-ready is NOT a defensive stance, no −2) ⇒ **−3 ⇒ counter, |3| ⇒
   Yuko to the defender**.
5. **Counter-ready alone is not enough**: same exchange at grip 0
   ⇒ **−2 ⇒ None**. Counter-ready must be paired with a grip edge to break −3.
   Reading and gripping both have to be true for 되치기 to fire.
6. **Defensive core alone triggers the term**: grip 0, read 0, stam 0, standard
   +1, opponent Core Defensive with neutral legs ⇒ 1 + (−2) = **−1 ⇒ no point**.
7. **Counter-ready suppresses it**: grip 0, read Neutral→**GotRead −3**, stam 0,
   standard +1, opponent Core Defensive but 카운터 준비 declared ⇒ no posture term
   ⇒ **−2 ⇒ None**. Applying −2 here would give −4 ⇒ a free Yuko to the defender,
   so the exception is load-bearing, not cosmetic.

### 4.2 CounterReady and the read term

- Legs slot `카운터 준비` is declared like any other action and resolves before
  the judgment. The fighter who declared it cannot have a technique of their own
  resolved this exchange (§2).
- If the opponent commits a **big throw** this turn, **the attacker's** ReadLevel drops
  **one step** (FullRead→Read→Neutral→GotRead, floored at GotRead).
  The judgment formula is attacker-relative and single-term, so a defender's read is
  expressed as a reduction of the attacker's — there is no defender read term to raise.
- CounterReady is not a defensive stance: the opponent-posture term (−2, §4) does
  NOT apply to a fighter who declared it.

**The ladder is intentionally uneven** (−1 / −2 / −3 in score terms). A fully-read attack
(FullRead) bottoms out at Read and can NOT be countered by CounterReady alone; a careless
attack (Neutral) drops all the way to GotRead. Reading can only be beaten by reading —
a turn invested in reading the opponent's habit must not be erased by their single
defensive pick.

**Counter ignition threshold is deliberately high (option ⓐ).** CounterReady alone
reaches −2 at grip 0; a counter needs read AND grip advantage together. This matches judo
(counters come off a grip) but may fire too rarely for the 대파열 tier. Revisit ONLY after
the 42-node scenario audit, and judge by the *distribution* of ignition conditions, not
the raw count: if every counter fires on the same "grip advantage + read" pattern,
CounterReady is an accessory to signature techniques rather than an independent choice,
and option ⓒ (flat −1 modifier on CounterReady) becomes necessary. Check the 대파열
presentation weight before touching any number.

### 4.3 Matte after a score (v3.4)

The exchange in which a score resolves is followed immediately by **matte**, unless
the osaekomi entry gate (§9.1) sends it to the ground instead.

| | On a scoring matte |
|---|---|
| Grips | all reset to **Free** |
| 자세(Core) 선택 | both fighters reset to Neutral — the Core Aggressive/Neutral/Defensive *choice* (renamed from "Core stance selection" in v4.1 to stop colliding with §S's Balance resource, see §S). §S 밸런스는 이 행과 별도로 §S의 부분 회복 ⚙만 받는다 — Neutral 리셋과 달리 100으로 완전히 되돌지 않는다 (RESOLVED v4.1, §8) |
| Stamina | §5.3 unchanged — **+3 each**, once per matte |
| Clock | stops (§1) |
| Passivity accumulators | untouched |

A score that ends the match (ippon, or the second waza-ari) is not followed by
matte — there is nothing to restart.

**Why this exists.** Recovery was tied to matte while nothing was producing matte:
§5.3 pays +3 per matte, but the only rules that called one were shido and the
detected feint, so measured matches saw **0–2 matte in a whole match**. Stamina
therefore drained roughly 3–4 per exchange with no meaningful recovery, bottoming
out at about **30 exchanges** — against the 60–80 that §2's "6–8 sequences of
30–60 s" implies at 3–5 s per exchange. §2 and §5 were describing two different
matches. This is a gap in the rules, not a consequence of how aggressively either
fighter plays.

It settles four things at once:

1. **Stamina.** Roughly one matte per score, so a match with 7 scores returns +21
   to each fighter and the exchange budget moves toward what §2 assumes.
2. **The read loop.** Grips reset, so grip-approach habits fire again and the
   opponent's tells come back around several times a match instead of once
   (`OPPONENT_BRAIN` §4.1 H4).
3. **Rhythm.** It creates the 하지메 → 그쳐 unit §2 is written around. Without it
   the regulation four minutes is one unbroken scramble.
4. **Presentation.** 대파열 already resolves into a pause (§8); matte is the
   landing that beat was written for.

### 4.4 흔들기 — 확정 (v4.0, §S 밸런스 기반. 구 [초안] 대체)

`BalanceGate` 플래그 뒤(구 `SetupGateEnabled` — 2026-08-11 오후 `Kuzushi*`→`Setup*` 개명을
거친 바로 그 플래그; `SetupGateEnabled`→`BalanceGate` 개명은 §H 3단계에서, §S 참조). 구현 시
기본 **on**으로 승격 예정 — 이 문서 갱신 시점의 실제 코드는 여전히 그 이름으로 기본 **off**
다. 플래그가 꺼져 있으면 아래 규칙은 전부 적용되지 않고, v1 대조 모드는 이 절 이전과 동일하게
동작한다.

- **선언 게이트.** 흔들기는 §2의 손 슬롯을 쓴다. 자기 그립이 하나라도 있어야
  (`GripKind != None`) 선언할 수 있다 — 잡은 것도 없이 무너뜨릴 수는 없다.
- **비용.** `SetupCost` ⚙(제안 1) — 견제 발기술과 같은 급의 가벼운 소모다.
- **효과 — 밸런스를 깎는다.** 흔들기는 **상대 밸런스(§S)를 깎는 전용 행동**이다. 그 자체로는
  판정을 내지 않는다(포인트가 나지 않는다, 견제 발기술과 동급) — §6.1 소극 사다리에도 별도
  rung 을 두지 않는다(0).
- **강등 게이트 — 일반화.** 구 초안의 "흔들기 없이 큰기술 = Sloppy 강등"을 **"상대 밸런스가
  온전 임계 ⚙(제안 70) 이상일 때 큰기술 = Sloppy 강등"**으로 일반화한다. 즉 게이트를 여는
  방법이 흔들기 하나가 아니라 **밸런스를 깎는 모든 선행 위협**(흔들기 · 견제 발기술 적중 ·
  무득점 진짜 시도, 전부 §S)이 된다 — 공격자가 상대 밸런스를 온전 임계 밑으로 깎아두지 않은
  채 큰기술(투기술, `ActionCost.BigThrow`)을 내면 그 기술의 기술급(`TechniqueClass`)이 §4
  판정식에는 `Sloppy`로 들어간다. (원문: "위장의 기준은 기술로 상대방의 중심이 흐트러졌는가"의
  역방향 적용.)
- **§F 숙련도와의 적용 순서 (v4.1, 확정 — §8 판단 대기 #2 해소).** 이 게이트와 §F 숙련도의
  `TechniqueClass` 결정은 같은 교환·같은 큰기술에 동시에 걸릴 수 있다. 순서는 **숙련도가
  먼저 `TechniqueClass`(Sloppy/Standard/Signature)를 정하고, §4 판정식에 들어가기 직전
  최종 단계에서 이 밸런스 게이트가 검사된다** — 상대 밸런스가 온전 임계 이상이면 §F가 무엇을
  정했든 그 값을 다시 `Sloppy`로 강등한다(게이트가 이긴다). 단, 숙련의 **연속 보정**
  (`MasteryCostReduction`/`MasteryReadReduction`, §5.4/§F)은 이 강등과 무관하게 항상 그대로
  적용된다 — 강등당해도 숙련자는 여전히 싸게 던지고 덜 읽힌다. 강등이 깎는 것은 §4 판정식에
  들어가는 `TechniqueClass` 값 하나뿐이다.
- **변칙기 면제.** `TechniqueSpec.SurpriseExempt = true` 인 기술(예: 말아업어치기 — 정석
  업어치기와 초동이 같아 상대가 미리 대비할 수 없는 게 이 기술의 존재 이유다)은 이 강등을
  받지 않는다.
- **소멸.** 그쳐·굳히기 진입·경기 종료가 발생하면 그 교환에서 새로 선언한 흔들기를 포함해
  양쪽 다 소멸한다 — 국면이 리셋되는데 무너뜨린 상태만 남는 것은 앞뒤가 안 맞는다.
- **판정식 자체는 바뀌지 않는다.** §4 공식은 그대로다 — 강등은 판정식에 들어가는 기술급
  값을 만드는 컨텍스트 단계(엔진)의 일이지, 공식 자체의 새 항이 아니다.
- **B 브레인.** "큰기술 전 흔들기 선행" 데코레이터(`SetupPrecedingCommitSource`)는 "밸런스
  깎기 선행"으로 일반화한다. 페이크 흔들기(설치만 하고 안 던지는 기만)는 여전히 후속 설계
  (§G 5항).

## 5. Stamina (체력, 0–100 each)

### 5.1 Attacker costs
| Action | Base cost |
|---|---|
| Big throw (투기술) | 5 |
| 견제 발기술 (poke) | 1 |
| Counter (되치기) | 2 |
| Tear (뜯기) | 1 |

**Grip multiplier applies to big throws ONLY** (counters use the opponent's force):
| effective grip | ×
|---|---|
| +2 | 0.6 |
| +1 | 0.8 |
| 0 | 1.0 |
| −1 | 1.5 |
| −2 | 2.0 |

Ordering sanity (designer intent): unfavorable-grip throw (10) > favorable-grip
throw (3–4) ≫ counter (2) > poke (1).

Counter cost (2) is paid by the **countering** fighter, **after** the judgment is
confirmed. The original attacker keeps the big-throw cost already paid — spending
everything and still being thrown is the intended feel.

### 5.2 Defender costs (per exchange where a technique came at you)
Clean defense (won the read) 2 / neutral 4 / pushed but no score 6.
**극단적 방어: additional −1 per turn held** (turtling drains you into the
opponent's stamina bonus — intended death spiral).
Osaekomi escape exchanges have their own cost — see §9.3.

### 5.3 Recovery
Matte: **+3** once per matte — and since v3.4 a score calls one (§4.3), which is
what makes this term reachable often enough to matter. Osaekomi stalling (advantaged side, no pin scored):
**+2 per 10 s** burned.

### 5.4 Training / mastery (hook)
Per-technique mastery L ∈ 0–5: cost ×(1 − 0.08·L); opponent's chance to read that
technique −5 %p per L. Core exposes the parameters; progression itself is out of
scope for Core.

### 5.5 Stamina 0
Not a KO. Fighter can only choose defense and poke (cost 1); opponent's judgment
formula gets the permanent +3. Collapse emerges from the rules, no special case.

### 5.6 Validation
Phase-1 spam sequence (grip +2): 밭다리 3 + 발목받치기 1 + 밭다리 3 + 허리후리기 3
= **10** ⇒ stamina 100 → 90, matching the oral match.

## 6. Referee & shido (심판)

### 6.1 Hidden passivity accumulator — one per fighter, NEVER shown as a number
Evaluated once per turn as an **EXCLUSIVE ladder: the first matching rung
applies, and only that one.** Never sum rungs.

| Rung (checked in this order) | Points |
|---|---|
| Real technique attempt (judgment fired) | −4 |
| Attack entry motion (기술 진입) | −2 |
| 극단적 방어 | +3 |
| Core AND legs defensive | +2 |
| Hand = 회피 | +2 |
| Legs = 카운터 준비 | +2 |
| Everything neutral | +1 |
| anything else | 0 |

Floor at 0. Sanity: sustained 극단적 방어 against a standard referee (12) is
**4 turns to shido**, which is the same length as the −1/turn stamina drain in
§5.2 — the two pressures are meant to arrive together.

### 6.2 Thresholds & profiles
Reaching threshold ⇒ matte ⇒ shido ⇒ accumulator resets to 0 and
**threshold += 2** (refs dislike repeating shido).

| Profile | Initial threshold (±1 random at match start) | Feint detection |
|---|---|---|
| 엄격 (strict) | 9 | 90 % |
| 표준 (standard) | 12 | 70 % |
| 관대 (lenient) | 16 | 45 % |

Profile is assigned pre-match; UI shows referee NAME only — profile is meta
knowledge the player learns across a career.

### 6.3 Feint attack (위장 공격)
A technique attempt followed within 2 turns by turtle/retreat is a feint candidate
⇒ roll detection ⇒ detected: immediate shido to the attacker. Undetected: free
escape. This is the intended (only) way out of a Locked grip: risk a shido to
force matte.

**v4.0 addendum (§A.4, confirmed).** A technique thrown at own grip ≤ −1 AND
opponent Balance (§S) ≥ the full-Balance threshold adds +⚙ to this same suspicion
accumulator — it does not add a second detection roll.

### 6.4 Player-facing tells (numbers stay hidden)
At 70 % of threshold: referee stares at that fighter (short cut-in).
At 90 %: referee steps closer + crowd murmur SE. Coach panel drops hints
("슬슬 움직여야 해"). These tells are Core events; presentation is Game-side.

## 7. Visibility rules

| Info | Player sees |
|---|---|
| Own stamina | exact |
| Opponent stamina | coarse 3-state only (fresh / tired ≈25–49 / exhausted <25) — matches the judgment brackets |
| Grip score & lock states | exact, on a **full-resolution HUD layer outside the RenderTexture** |
| Which body part each hand holds | exact, same HUD layer — **REQUIRED** |
| Referee accumulator | never — tells only (§6.4) |
| Opponent habits | only what the 수 노트 has logged |
| Opponent surprise tech | hidden until first use |
| Own tendency exposure (내 성향) | **exact** — the bar the opponent reads is public (§4) |
| Opponent's scouting level | pre-match notice, never a live number |
| Own & opponent Balance (§S, v4.0) | **exact** — confirmed a HUD number, same tier as grip; unlike a tell, not withheld |

**Grip HUD is a hard requirement, not a presentation preference.** Measured on
`spike/lowres-render`: a hand is 5.8 px wide at 320×180, so the player can see *that*
contact exists but not *what* is held. The §3.2 tear system and the part-targeted grip
break both ask the player to name a body part; without the HUD those choices degrade to a
1-in-3 guess. Core must therefore emit grip-part state as explicit events, never leaving
it implicit in the animation.

## 8. Open questions / tunables (not blocking implementation)
- Exact tear success math before training modifiers (v1: pure read-based, §3.2).
- RESOLVED v3.1: lock cap trigger (either hand), passivity ladder exclusivity,
  counter-ready effect and its sign (§4.2), simultaneous clock cost, time-up boundary.
- RESOLVED v3.2: defensive stance is Core == Defensive only, suppressed by
  카운터 준비 (§4). Derived in exactly one place, JudgementCalculator.IsDefensivePosture.
- RESOLVED v3.2 (by 360-combination sweep, not the 42-node audit): counter ignition
  threshold **ⓐ (−3)** — ignition splits into two families (read-led / grip-led) and
  CounterReady already fires at grip +1.
- RESOLVED v3.3: ReadLevel supplier generalised out of the scenario layer (§4,
  `OPPONENT_BRAIN.md`); osaekomi entry gate and escape resolution (§9).
- Escape choice set is 2×2 in v1; a third option and the `adv` constants are
  tunables to revisit after play.
- Whether Locked should also raise big-throw cost beyond the grip-cap effect (v1: no).
- Golden-score fatigue floor (v1: none — both <25 stamina makes any yuko end it).
- **v4.0 revision (§S, §4.4, §A–§F below) is confirmed** — no [결정 대기] left in the
  source revision doc (`MATCH_RULES_revision-v4-final.md`). Two interaction
  questions surfaced while merging it into this file; both were **RESOLVED v4.1**
  (2026-08-12, user decision, documentation-only session — code and tests untouched):
  1. **자원 명칭 충돌.** §4.3/§9.4's pre-existing "Stance reset" row was the Core
     stance *selection* resetting to Neutral, not the new §S numeric resource — the
     two shared a name. Resolved by renaming: §S's resource is now **밸런스/Balance**
     (not Stance), and the Core selection is now **자세(Core)** (§S, §4.3, §9.4).
     Code identifier follows suit — `Balance`, not `Stance`, to avoid colliding with
     the existing `CoreStance` enum; the actual rename happens in §H step 3. And:
     **§S Balance does NOT fully reset (to 100) on matte/escape** — it only receives
     its own partial recovery amount (⚙), the same shape as stamina's `MatteRecovery`
     (§5.3). A full reset to 100 happens only between matches.
  2. **§F ↔ §4.4 gate priority.** §F's mastery-driven `TechniqueClass` and §4.4's
     balance-gate downgrade-to-Sloppy can both apply to the same big throw in the
     same exchange. Resolved: mastery decides `TechniqueClass` first; the balance
     gate is checked last, immediately before the value enters §4's formula, and
     wins — a gate-triggered downgrade to Sloppy overrides a mastery-earned
     Signature/Standard. Mastery's continuous corrections
     (`MasteryCostReduction`/`MasteryReadReduction`) are unaffected by the downgrade
     and always apply. Full text: §4.4's closing bullet.
- Carried-over scope for a later round: §G below.
- **§2b (v4.2, 잡기 개편 1차) — open items, not yet coded, kept here so they don't
  get lost:**
  1. **가슴깃 노림 vs 중립 Core.** The spec names only 공격적(succeeds)/수비적(fails);
     Neutral was read as "not defensive → succeeds" during implementation — a
     judgment call, not a spec answer. Retune if that reading feels wrong in play.
  2. **노그립 즉시 기술.** "상대 Core 공격=성공률↑/수비=희박" is NOT implemented —
     it names an effect on technique judgment (§4/§E), not on grip resolution
     itself, and which term it should adjust is unspecified. Deferred until §4/§E
     work resumes; see `IMPACT_grip-revision.md` for the reasoning.
  3. **틀어잡기 side mapping.** TwistGrip/BackGrab have no canonical left/right in
     the pre-v4.2 model — v4.2 assigns them Right/Left respectively (for 정/교차
     and defended-zone purposes) as an implementation judgment call, not a spec
     answer.
  4. **선택 UI (`LiveMatchPanel`) — 완료 (후속 세션).** 손 축 스테이지1(오른손/왼손/
     회피/뜯기[2차 예약])은 새 고정 각도표(안전 반원 90°~270°, 전용 반지름
     `HandTopRadius=40`)로 구현·Play 실측 확인(A 겹침 0, 자체 클립은 회피
     하나뿐 22px — 근거는 `LiveMatchPanel.cs`의 `HandTopSlots` 주석 참조). 회피
     2존 선택(손별 1존, 중복 제외)·정/교차 노란 표기·손별 그립 상태+교차 타이머
     HUD 전부 구현·Play 실측 확인(GetWorldCorners 직접 측정, Confirm()까지
     예외 없이 완주 확인).

## 9. Osaekomi — entry & escape (굳히기)

Osaekomi is **not** a free follow-up to any score. Entry is gated by the throw
that preceded it: a yuko-grade throw rarely leaves the thrower in a position to
hold, while a waza-ari-grade one usually does. Grip at entry and the defender's
stamina decide the rest. The hold itself contributes nothing on its own.

### 9.1 Entry gate — automatic, never a player choice

| Judgement that just resolved | Osaekomi |
|---|---|
| Ippon | n/a (match ends) |
| **Waza-ari** | enters |
| **Yuko** | enters ONLY if attacker's effective grip ≥ +1 AND defender stamina < 50 |
| None, or counter with \|sum\| ≤ 1 | never |

§9.1의 '공격자'는 그 판정의 득점자를 가리킨다. 되치기 득점도 동일하게 적용한다 —
절반은 이행, 유효는 득점자 잡기 ≥ +1 AND 상대 체력 < 50일 때만. (v3.5)

체력 <50 은 **그 교환의 지불 전 체력**을 가리킨다 — §4 판정식의 체력 보너스
항이 읽는 것과 같은 스냅샷이다. 한 교환 안에서 공격·방어 스태미나 지불(§5.1)이
끝난 뒤 다시 읽은 값이 아니다 — 판정과 굳히기 진입은 같은 순간의 상태를 봐야
하고, 지불 전/후로 갈리면 같은 교환의 판정식과 굳히기 게이트가 서로 다른 두
숫자로 "체력 <50"을 묻는 모순이 생긴다. (v3.6)

### 9.2 Hold advantage — computed once, at entry

```
adv = (waza-ari entry ? +2 : 0)
    + attacker effective grip at entry        (−2 … +2)
    + (defender stamina <25 ? +2 : <50 ? +1 : 0)

escapesNeeded = clamp(1 + floor(adv / 2), 1, 3)
```

`adv` is **frozen at entry**. Nothing recomputes it mid-hold — the hold is decided
by how you got there, which is the whole point of this section.

### 9.3 Escape exchange — one per 5 s of hold

Simultaneous 2-choice commit, same shape as a standing exchange:

| Defender (escaping) | Holder |
|---|---|
| 뒤집기 | 따라가기 |
| 빠져나가기 | 눌러붙기 |

- **맞물림** (뒤집기×따라가기, 빠져나가기×눌러붙기): hold continues, defender pays
  **3 stamina**. Holder recovery is §5.3 unchanged (+2 per 10 s stalled).
- **어긋남** (the two cross pairs): one escape credited. At `escapesNeeded` the
  hold breaks.
- **Ordering at each 5 / 10 / 20 s mark: the osaekomi score is awarded FIRST, the
  escape exchange resolves after.** Same principle as the time-up boundary in §1 —
  an escape landing exactly on 5 s does not erase the yuko.
- **v1 default when there is no real input** (headless/scripted drivers with
  nobody actually choosing 뒤집기/빠져나가기/따라가기/눌러붙기): 어긋남 —
  specifically 빠져나가기×따라가기. A stand-in only; a real player picks for
  real once stage 6 wires actual input. Under this default a hold always
  resolves via escape at `escapesNeeded × 5` s (5–15 s) unless an absolute
  score mark (10 s awasete-ippon, 20 s ippon) ends the match first — it can
  never itself reach the 20 s mark, since escapesNeeded caps at 3. (v3.6)

### 9.4 On escape

Standing restart **without matte**: the clock does not stop and no matte recovery
(§5.3) is granted. All grips reset to **Free** and both fighters' 자세(Core)
selections reset to Neutral — a fighter who went to the mat cannot carry a grip
back up. (As in §4.3, this is the Core selection, renamed from "Core stance
selection" in v4.1 to stop colliding with §S's Balance resource — see §S. §S
밸런스는 여기서도 부분 회복 ⚙만 받는다 — RESOLVED v4.1, §8.) Passivity accumulators
are untouched.

---

# v4.0 revision — 서기 국면 확장 (§A–§G)

Everything below is new as of v4.0, confirmed with no `[결정 대기]` remaining in
the source revision (`MATCH_RULES_revision-v4-final.md`). Scope: 서기 (standing)
phase only — osaekomi expansion, scouting, and training are carried to §G. ⚙
marks a proposed initial value, a `MatchRulesConfig` candidate — not a confirmed
number, same convention as the rest of this file. Quoted Korean passages preserve
the confirmed original wording where a translation could drift.

## A. Input model

### A.1 Hand split (confirmed)
- The Hand slot splits into left/right; one turn commands exactly one hand (also
  reflected in §2's Hand row above).
- **The uncommanded hand**: auto-maintains its grip if it holds one, stays released
  if it does not (confirmed). §6.1 passivity does not count an auto-maintained hand
  as "undeclared."
- **Companion bug, unrelated to this revision, fix immediately once code work
  resumes**: `GripSystem.CanUseConversion` is missing its opposite-hand check (a
  hand can self-convert without the other hand holding a grip) — a §2 violation.
  Scheduled as implementation step 0 (see STATUS.md Next Up); not touched in this
  documentation-only session.

### A.2 Technique turn (confirmed)
- Legs "공격" (attack) = **technique selection**: pick one entry from the technique
  list (§B). The hardcoded 밭다리 fallback is retired.
- A technique-declare turn carries no Core or Hand input. That turn's posture is
  treated as **Aggressive** (confirmed) — the defensive penalty (−2) does not apply,
  but the fighter is more exposed to being countered. This is the built-in risk of
  committing to a technique.
- 내 성향 exposure window (§7): a technique-declare turn is not logged into the
  (Core, Legs) channel (no neutral placeholder substituted). It logs into the new
  **technique-frequency channel** instead (§C.2).

### A.3 Attacker determination, unified
"Attacker" now uniformly means whoever explicitly committed a technique this
exchange. Applies to the live path only — the 42-node scripted path (대조 모드)
keeps its existing tag-based determination, preserving the fingerprint.

### A.4 Feint (위장 공격) — confirmed criterion: "위장의 기준은 기술로 상대방의
중심이 흐트러졌는가" ("does the technique disturb the opponent's center?")
- **"불리" (disadvantaged) defined as: own grip ≤ −1 ⚙ AND opponent Balance (§S) ≥
  the full-Balance threshold** — a technique thrown on a sloppy grip with no
  preceding threat (no Balance drop) to back it.
- Effect: the referee's feint-suspicion accumulator gains +⚙ (proposed +2). Adds
  to the existing realAttempt / FeintWindow machinery (§6.3) — not a new detection
  roll.
- v1 uses only these two axes. Other axes (e.g. stamina) are added only once a need
  for them is confirmed.

## B. Technique roster and classification (confirmed)

Two classification systems, kept separate:
- **Teaching / acquisition screen**: the five standard judo categories — 손기술
  (hand) · 발기술 (leg) · 허리기술 (hip) · 누우며 메치기 (sacrifice) · 변칙
  (irregular). Used only to explain a technique to the player when learning it.
- **Match algorithm**: a binary split — **견제성 발기술** (poke-kick) / **큰 기술**
  (big throw). §D.4's special case applies only to the former.

| Technique | Owner | Algorithm class | Note |
|---|---|---|---|
| 모두걸기 | A | 견제성 발기술 | **new** |
| 발목받치기 | A | 견제성 발기술 | |
| 나오는발차기 | A | 견제성 발기술 | **new** |
| 발뒤축걸기 | A | 견제성 발기술 | **new** |
| 밭다리걸기 | A | 큰 기술 | main technique |
| 허리후리기 | A | 큰 기술 | |
| 안오금띄기 | A | 큰 기술 | ambush character |
| 어깨 매치기 (변칙 어깨) | A | 큰 기술 | |
| 업어치기 | B | 큰 기술 | habit H1 |
| 안뒤축감아치기 | B | 큰 기술 | habit H2 · doubles as a counter |
| 허리 감아치기 | B | 큰 기술 | counter-flavored |
| 말아업어치기 | B | 큰 기술 | irregular, once per match |

- Confirmed character of 견제성 발기술 (원문): "상황에 따라 100% 견제용(포인트가 날
  확률이 0에 수렴)이거나, 조건이 합쳐지면 유효에서 한판까지" — a family with a deep
  split between floor and ceiling. Stacking conditions = grip advantage · low
  opponent Balance · opponent misreads (오독) · high mastery.
- Whether B is given any 견제성 발기술 is the opponent-definition's (`OpponentAsset`)
  call — this rule defines only the technique family.
- Implementation ask, delegated to `CORE_DESIGN_MatchEngine.md`: `TechniqueSpec`
  needs a display name, an algorithm-class field, a 5-category field, and a
  registry.

## C. Cognition and adaptation (confirmed: per-opponent, permanent + decay)

### C.1 A's cognition list
When the opponent commits technique X and judgment fires, it is logged after that
exchange resolves. Before logging, X cannot be named as a §D prediction candidate.
**Stored per-opponent, permanently** — the rivalry structure of "training and
drilling to beat an opponent who has learned more about you." 말아업어치기 (the
once-per-match irregular technique) is structurally unpredictable on its first
appearance — no special-case rule needed; the surprise guarantee falls out on its
own.

### C.2 B's adaptation (symmetric)
The technique-frequency channel observes A's technique usage. Once A has exposed X
⚙ times (proposed 3), A's ReadLevel drops one rung whenever A uses X afterward —
implemented as **an added read-ladder row** (the formula itself unchanged). The
"every GotRead route against A traces back to something A did" principle
(`OPPONENT_BRAIN` §2) holds here too. Stored per-opponent, permanently.

### C.3 Decay (confirmed: "봉인했다 꺼내는 플레이" — "sealed away, brought back out")
If A does not use X for ⚙ duration, B's adaptation to X decays one rung. A's
cognition decays symmetrically — a **fading stage**: re-reading a faded technique
gets a reduced reward, one rung down from §D's normal payoff. Numbers wait for the
career design pass; this revision only writes decay's existence into the rules.
**Logged as a separate design task**: build cognition/decay "진짜 라이벌 분석을
하는 사람의 뇌처럼" ("the way a real rival-scouting brain would think") in a later
brain-design round, after this rule lands.

## D. Predictive counter (confirmed)

Canon: new board n30–n37 ('업어치기다' / '안뒤축이다' / '안 읽는다' → 정독/오독).

### D.1 Input
On declaring counter-ready, choose **[no prediction]** or name one technique from
the cognition list.

### D.2 No prediction
Unchanged §4.2 (one-rung drop, cannot break through FullRead).

### D.3 정독 (read correctly) = **2-rung drop** (confirmed — overrides GotRead)
Intent (원문): "기술을 예측해서 회피한 것만 해도 엄청난 성과 — 완벽한 되치기의
필요조건이지 충분조건이 아니다. 완벽한 되치기와 한판은 정말 흔치 않아서 더
아름답다." Deliberate pairing with the uneven ladder: reading a technique exposed
at Read or below drops it to GotRead (−3) — **counter-eligible**. Reading a
technique held at FullRead (fully hidden) only drops it to Neutral (0) —
**neutralized, but not counterable**. The reward for a successful read and the
rarity of a counter both fall out of the ladder's existing shape, no extra rule
needed.

### D.4 견제성 발기술 special case (confirmed — unified with Balance)
Applies only to the four 견제성 발기술 (§B). 큰 기술 (밭다리 etc.) are exempt.
- **Default (imperfect grip)**: floor = **drop the opponent's Balance** ⚙ (no point
  — 원문: "보통은 상대 자세가 흐트러져서 잡기 싸움이 유리해지는 정도"), promotion
  tier 1 = Yuko ("가끔 유효"). Absent a counter use, poking defaults to this.
- **Jackpot path**: the more stacking conditions (grip advantage · low opponent
  Balance · opponent misread · high mastery) line up, the higher the floor and the
  better the promotion odds ⚙: full grip advantage (+2) + 정독 = floor Yuko
  ("보통 유효") → half-chance promotion ("적은 확률") → chained promotion to Ippon
  ("희박한 확률" — "인생 하이라이트 매치기").
- Condition→floor/probability mapping is a `MatchRulesConfig` implementation detail
  (all ⚙). Only the structure is confirmed here.

### D.5 오독 (misread) — confirmed, triple penalty, subject to retuning after play
1. §4.2's general protection is void — a failed prediction is worse than no
   defense at all.
2. That exchange counts as having no defensive-posture value, AND the opponent's
   judgment sum gets **+1** ⚙ (the misreader's own balance broke).
3. The counter-ready cost is not refunded.

## E. Judgment promotion — deterministic floor + upward dice (confirmed)

> 원문: "결정론 게임은 공략과 파훼법이 나오는 순간 흥미가 식는다. 실제 유도는
> 변수가 언제나 있다. 단, 모든 조건이 딱 떨어졌는데 노포인트면 힘이 빠지니 —
> 하한선은 보장하고 그 위를 정교한 확률로."

### E.1 Structure
1. §4's formula → sum → bracket = **guaranteed floor**. No downgrade below it —
   everything up to here stays deterministic and fully explained by `terms[]`.
2. A promotion roll above the floor: success moves it up one tier. Chained up to 2
   ⚙.
3. Probability curve ⚙: `p = PromoteBase(10%) + PromoteSlope(5%p/point) × (sum −
   bracket floor)`, each chain link ×`PromoteChainFactor(0.25)`. Applies
   symmetrically to counters on `|sum|`.
4. **A "None" result may promote — realAttempt only** (confirmed) — 원문: "열심히
   전장에 뛰어드는 유저에게 럭키펀치." 견제 (poke) never rolls.
5. Anti-spam for pure poking (confirmed requirement — 원문: "의미 없는 견제만 하면
   결국 패배하는 시나리오"): no new mechanism added. The existing triple pressure
   already does this job — §6.1's passivity accumulation (repeated poking is a
   penalized ladder path), stamina cost, and §A.4's feint weighting. **Whether this
   triple pressure actually herds spam into a loss is a play-verification item** —
   reinforce only if it proves insufficient.

### E.2 Where there is no dice roll (confirmed)
Grip resolution · Balance changes (§S) · judgment floor — all deterministic.
Probability touches exactly three things: the promotion roll, referee detection,
and habit firing.

### E.3 Regression / fingerprint policy
Every roll goes through `IRandomSource`. `FixedRandomSource` (no promotion) is
designed to leave existing tests and the 42-node fingerprint behaviorally
unchanged. `JudgementScored` exposes floor/promotion separately, so the coach
panel can say "the judgment floor was yuko, and the referee saw it bigger."

## F. Mastery (confirmed)

> Goal (원문): "완전 미완성인 기술을 맨땅에 헤딩으로 시작해서, 유효를 만들어내는
> 서브 기술이 되었다가, 결국 한판을 만들어낼 수 있는 주특기 중 하나가 되는 경험."

- **Judgment reflection**: per-technique mastery level decides `TechniqueClass` ⚙
  (levels 0–1 Sloppy / 2–3 Standard / 4–5 Signature). The §4 formula itself is
  unchanged. **When §4.4's balance gate also applies to the same big throw, the
  gate is checked last and wins** — see §4.4's closing bullet (RESOLVED v4.1, §8).
- **Continuous reflection**: the level feeds straight into the existing
  `MasteryCostReduction` (stamina, §5.4) and `MasteryReadReduction` (how readable
  the technique is) hooks — a level-up that doesn't cross a class boundary still
  feels like something.
- **Acquisition** (confirmed): mastery rises just from use, **weighted up on a
  score** (yuko < waza-ari < ippon ⚙). Not gated on success — a failed attempt is
  still practice.
- **Diminishing returns** (confirmed): once a technique is fairly polished, further
  gains get 원문 "정말 미세하게" — an acquisition curve that tapers ⚙.
- **High-tier cap** (confirmed, placeholder only): the top levels ⚙ (4–5) require
  body stats (base strength/conditioning); the stat and training systems themselves
  are carried to §G. This rule only writes the cap's existence down.
- Scope: **permanent** (career). Pairs with §C adaptation — "use it more, it gets
  stronger (mastery) vs. use it more, it gets read (adaptation)" landing on the
  same technique in opposite directions is the core of the growth loop.

## G. Carried over (out of scope for this revision)

1. **Osaekomi expansion** — three pin variants · pancaking · armbar · chokes (needs
   a new submission judgment) · two escape variants. A separate round, after the
   standing-phase revision above is verified in play.
2. **Pre-match scouting** — `ScoutingLevel` 0–3 wiring into "seed the cognition
   list" (`OPPONENT_BRAIN` §5).
3. **Training system / body stats** — confirmed roadmap: once the match system
   feels satisfying → build the training system → **self-mocap from the user's own
   training footage** (builds the mocap-pipeline skill with no second person
   needed, since it is one person) → THEN match-technique mocap (two people, high
   volume). Training mocap precedes match mocap.
4. **Detailed cognition/decay algorithm** — 원문 "라이벌 분석을 하는 사람의 뇌"
   level. A later brain-design round.
5. **페이크 흔들기** (Feint 흔들기 — setting up 흔들기 without ever throwing, a
   bluff). After §C/§D have settled in play.

---

*Implementation order for §S/§A–§F (steps 0–8) is tracked in `md/STATUS.md`'s
Next Up, not here — this file states what the rules are, not the build sequence.*
