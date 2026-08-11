<!-- v3.8 | body:17a7bbb -->
# MATCH_RULES.md — COUNTER TIME judo match system (v3.8)

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
| Hand (ONE hand only) | target opponent's: 오른소매 / 왼소매 / 왼가슴 / 오른가슴 / 회피 | Committing both hands in one turn is not offered (real-judo suicide). |
| Hand — unlocked conversions | 틀어잡기(back-collar twist), 등싸잡기 | Only available while the OTHER hand already holds a sleeve/lapel. |
| Hand — 뜯기(tear) | resolves per §3.2's simultaneous mind-game | Gate: opponent holds at least one of your hands (`GripLock != Free`). |
| Hand — 잡기 유지(maintain grip) | the defending side of §3.2's tear contest | Gate: you hold at least one grip (`HasAnyGrip`). |
| Hand — **[초안 — 사람 검토 전]** 흔들기 | 상대를 무너뜨려 다음 교환의 큰기술에 정당성을 준다 (§4.4) | `SetupGateEnabled` 플래그 뒤. 자기 그립이 하나라도 있어야 선언 가능. 판정 미발화(견제 발기술과 동급), 별도 상성표 rung 없음. |
| Core | 공격적 / 중립 / 수비적 | |
| Legs (발기술) | 공격 / 중립 / 수비 / 카운터 준비 | Legs act even while gripping. |

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

### 3.1 Grip score (잡기 수치) — FIXED range −2…+2
Each fighter's hold value: normal grip (sleeve/lapel) = 1, dominant grip
(틀어잡기 or 등싸잡기) = 2. Side total capped at 2.
**gripScore(A) = capped(A holds) − capped(B holds)**, clamped to [−2, +2].
Perspective flips sign for B.

### 3.2 Grip lock states (per hand held by opponent)
`Free → Held → Locked(견고)`

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
| Stance | both fighters reset |
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

### 4.4 **[초안 — 사람 검토 전]** 흔들기 — 변칙기 강등 규칙

`SetupGateEnabled` 플래그 뒤(기본 **off** — v1 대조 모드는 이 절 이전과 동일하게 동작한다).
플래그가 꺼져 있으면 아래 규칙은 전부 적용되지 않는다.

- **선언 게이트.** 흔들기는 §2의 손 슬롯을 쓴다. 자기 그립이 하나라도 있어야
  (`GripKind != None`) 선언할 수 있다 — 잡은 것도 없이 무너뜨릴 수는 없다.
- **비용.** `SetupCost` (기본 1) — 견제 발기술과 같은 급의 가벼운 소모다.
- **판정.** 흔들기 자체는 판정을 내지 않는다(견제 발기술과 동급) — §6.1 소극 사다리에도
  별도 rung 을 두지 않는다(0).
- **지속.** 흔들기가 성립하면 "상대에 대한 흔들기 상태"가 **1교환** 지속된다 — 선언한 바로
  다음 교환에만 유효하고, 그 뒤엔 다시 선언해야 한다.
- **강등.** 공격자가 흔들기 상태 없이 큰기술(투기술, `ActionCost.BigThrow`)을 내면 그
  기술의 기술급(`TechniqueClass`)이 §4 판정식에는 `Sloppy`로 들어간다 — 무너뜨리지 않은
  큰기술은 허술하게 들어간 것으로 친다.
- **변칙기 면제.** `TechniqueSpec.SurpriseExempt = true` 인 기술(예: 말아업어치기 — 정석
  업어치기와 초동이 같아 상대가 흔들기로 미리 대비할 수 없는 게 이 기술의 존재 이유다)은
  이 강등을 받지 않는다.
- **소멸.** 그쳐·굳히기 진입·경기 종료가 발생하면 그 교환에서 새로 선언한 흔들기를 포함해
  양쪽 다 소멸한다 — 국면이 리셋되는데 무너뜨린 상태만 남는 것은 앞뒤가 안 맞는다.
- **판정식 자체는 바뀌지 않는다.** §4 공식은 그대로다 — 강등은 판정식에 들어가는 기술급
  값을 만드는 컨텍스트 단계(엔진)의 일이지, 공식 자체의 새 항이 아니다.

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
(§5.3) is granted. All grips reset to **Free** and both fighters' stances reset —
a fighter who went to the mat cannot carry a grip back up. Passivity accumulators
are untouched.
