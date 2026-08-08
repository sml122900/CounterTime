# MATCH_RULES.md — COUNTER TIME judo match system (v3.1)

Single source of truth for match logic. `CounterTime.Core` implements THIS file;
if code and this file disagree, this file wins. All numbers are v1 tunables unless
marked FIXED. Korean technique/UI terms are kept as-is (UI language is Korean).

Companion docs: `CORE_DESIGN_MatchEngine.md` (implementation design),
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
| Core | 공격적 / 중립 / 수비적 | |
| Legs (발기술) | 공격 / 중립 / 수비 / 카운터 준비 | Legs act even while gripping. |

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
| Opponent posture | opponent in defensive stance → **−2** |

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

Reading levels, operationally: *full read* = you predicted the opponent's exact
commitment and they were mid-commitment into you (e.g. reaching for a grip, mid
rotation); *read* = correct anticipation; *got read* = your attempt was predicted.
v1: the scenario/AI layer supplies the ReadLevel; Core only consumes it.

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

### 5.3 Recovery
Matte: **+3** once per matte. Osaekomi stalling (advantaged side, no pin scored):
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
- DEFERRED to the 42-node audit: counter ignition threshold (option ⓐ vs ⓒ), judged
  by distribution of ignition conditions, after the 대파열 presentation is checked.
- Osaekomi escape resolution (v1: scripted by scenario nodes).
- Whether Locked should also raise big-throw cost beyond the grip-cap effect (v1: no).
- Golden-score fatigue floor (v1: none — both <25 stamina makes any yuko end it).
