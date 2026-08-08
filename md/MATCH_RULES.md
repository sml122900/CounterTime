# MATCH_RULES.md — COUNTER TIME judo match system (v3)

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
- **Time expiry**: an exchange already in progress is **resolved in full** and its score
  counts; regulation then ends and the overflow is discarded. (Mirrors IJF — a technique
  begun before time expires is scored.)
- Win resolution at 4:00: ippon > waza-ari count > yuko count > **golden score**.
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

Time cost per action (clock only advances on actions; matte = stop):

| Action | Seconds |
|---|---|
| Grip turn (hand exchange) | 5 |
| Technique attempt / defense exchange | 3 |
| Tear attempt (뜯기) | 3 |
| Osaekomi | real time held |

An exchange advances the clock by **max(cost of A's action, cost of B's action)**. Both
fighters always exit an exchange at the same timestamp; the slower action sets the length.

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
- **Locked**: tear is impossible. **If either of your hands is Locked**, your *effective*
  grip score for judgment and stamina is `min(gripScore, −1)`.
  (v1 deliberately ignores which hand is the leading one — with one hand slot per turn the
  distinction is marginal, and "lead-side hand" was never defined.)
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

### 4.2 CounterReady (카운터 준비)

Declaring counter-ready for the turn:

- You **cannot** attempt a technique this turn.
- If the opponent commits a **big throw** this turn, your ReadLevel is raised **one step**
  (Neutral→Read, Read→FullRead), **capped at FullRead**.
- If the opponent does not attack, nothing happens — you have spent the turn.
- Referee passivity **+2** (category 2 in §6.1).

Counters therefore have two sources: an **earned** one (this choice) and an **accidental**
one (sum ≤ −3, §4). Both resolve identically.

Design note — this is what makes the 대파열 layer a decision rather than an accident.
Without it, counters only ever happen to you, and the middle tier of the three-tier payoff
design is pure luck.

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
Per turn:
| Behavior | Points |
|---|---|
| 극단적 방어 | +3 |
| Core AND legs defensive | +2 |
| Hand = 회피 | +2 |
| 카운터 준비 (§4.2) | +2 |
| Everything neutral | +1 |
| Attack entry motion (기술 진입) | −2 |
| Real technique attempt (judgment fired) | −4 |
Floor at 0.

**Exactly one positive category applies per turn — the highest that matches:**
극단적 방어 (3) > 이중수비 = 회피 = 카운터 준비 (2) > 전체중립 (1).
Categories never stack. `Attack entry (−2)` and `real attempt (−4)` REPLACE any positive
category rather than adding to it.
Rationale: the accumulator measures how passive a fighter *looks*, not how many defensive
flags are set. Stacking would give a turtled fighter 7/turn and a shido in 2 turns.
Result: a fully turtled fighter draws a shido from a standard referee in **4 turns**,
matching the §5.2 drain pacing.

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
- Osaekomi escape resolution (v1: scripted by scenario nodes).
- Whether Locked should also raise big-throw cost beyond the grip-cap effect (v1: no).
- Golden-score fatigue floor (v1: none — both <25 stamina makes any yuko end it).
