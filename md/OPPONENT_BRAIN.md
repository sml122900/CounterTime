<!-- v1.0 | body:08dd6b2 -->
# OPPONENT_BRAIN.md — where commits and ReadLevel come from (v1.0)

`MATCH_RULES.md` wins on any conflict. This file owns exactly one thing:
**who produces each fighter's `TurnChoice`, and where the `ReadLevel` fed to
`JudgementCalculator` comes from.** Nothing here is match logic, so none of it
lives in `CounterTime.Core`.

## Hard constraints

- **`MatchEngine` and `ExchangeScript` signatures do not change.** The v1 seam
  stays where `CORE_DESIGN_MatchEngine.md` put it; only what plugs into it changes.
- The brain lives in `CounterTime.Game` (or a new `CounterTime.Brain` asmdef that
  references Core only). Core keeps zero UnityEngine references and zero knowledge
  of habits.
- **대조 모드 must keep passing.** 113 green tests and the 42-node audit stand on
  the scripted path. It is not refactored away, it is isolated behind an interface.
- ReadLevel derivation exists in **one** place (`IReadResolver`). Inline
  re-derivation is the failure that produced the `IsDefensivePosture` bug; do not
  repeat it here.

## 1. Structure

```
ICommitSource            bool TryNext(out FighterId who, out TurnChoice choice)
                         false = no declaration this turn. Do NOT collapse this
                         into a neutral TurnChoice — MATCH_RULES §6.1 "a fighter
                         who never declared accumulates 0" rides on the distinction.
  ScriptedCommitSource     scenario node          (대조 모드)
  PlayerCommitSource       A-only input UI        (플레이 모드)
  HabitBrainCommitSource   B algorithm            (플레이 모드)

IReadResolver            ReadLevel For(FighterId attacker, TurnChoice a, TurnChoice b)
                         Returns the BASE read, before §4.2 CounterReady step-down.
                         Core applies the step-down; a resolver that pre-applies it
                         causes double reduction.
  ScriptedReadResolver     node's [판정식] line   (대조 모드)
  LiveReadResolver         §2 + §3 below          (플레이 모드)

ExchangeScript assembler
  ScriptedAssembler        existing For(node) → IReadOnlyList<ExchangeScript>
  LiveAssembler            two ICommitSource + one IReadResolver → ExchangeScript
```

| Mode | A | B | Read | Use |
|---|---|---|---|---|
| 대조 (script) | Scripted | Scripted | Scripted | regression, 42-node audit — **keep** |
| 플레이 (game) | Player | HabitBrain | Live | the game |
| 디버그 | Player | Scripted | Scripted | falls out for free; handy for repro |

`§4.2` CounterReady step-down still happens **inside Core**, after the resolver
supplies the base ReadLevel. The resolver must not pre-apply it.

## 2. ReadLevel — habit knowledge (both directions)

Knowledge is per-habit and per-match (see §5 for career carry-over).

| Observations | Knowledge | 수 노트 |
|---|---|---|
| 0 | 미지 | nothing |
| 1–2 | 심증 | tentative line |
| 3+ | 확정 | logged |

An **observation** is counted only when the habit's condition held AND B actually
fired it. A condition that held while B did something else counts nothing.

**Reference point — the ledger records after the exchange resolves, so the read
on any given firing sees only the observations from *earlier* firings.** Three
sightings put the habit in the 수 노트; the **fourth** firing is the first one
that gets read. Written the way the player experiences it:

> Watch the same habit three times and it goes into your 수 노트.
> **From then on you read it.**

This is not merely for symmetry with §3. The note has to appear *before* the
reading starts. If the third firing were itself read, the player would already
be reading a habit the note had not yet logged, and the "logged automatically on
the third sighting" beat would become an after-the-fact notification of
something that already happened.

The read term is always **attacker-relative**, so habit knowledge attaches to
whoever threw the predicted habit. The same knowledge merely changes sign
depending on who is attacking this exchange.

**Read ladder — exclusive. First match wins, never summed** (same shape as §6.1,
for the same reason):

| # | Condition | Attacker's read |
|---|---|---|
| 1 | Attacker is **B**, and A knows the habit B just fired at **확정** (3+) | **GotRead −3** |
| 2 | Attacker is **A**, B fired a non-attacking habit, and A knows it at **확정** | **FullRead +3** |
| 3 | As row 2, but the knowledge is only **심증** (1–2) | **Read +2** |
| 4 | Attacker is **B** and the tendency window (§3) hits — a 4-of-6 majority | **Read +2**, or **FullRead +3** on three identical in a row |
| 5 | Attacker is **A** and A's own last **three commits were identical** | **GotRead −3** |
| 6 | anything else | **Neutral 0** |

Row 5 is the reason the tendency window exists at all: the player has to *feel*
being read. Without it the window only ever helps B attack, and A never pays for
becoming predictable.

The evidence deliberately splits into two tiers:

| Evidence | Effect |
|---|---|
| 4-of-6 majority in the window | B's attacks read A — **+2**, row 4, that side only |
| **three identical in a row** | that, **and A's own attacks become readable** — row 5 |

Handing GotRead straight off the 4-of-6 majority would make the same evidence
worth +2 to B but −3 to A — mismatched magnitudes — and it would fire far too
often. Putting the danger line at three-in-a-row gives the 내 성향 bar **two
marks**: a warning line (4-of-6) and a danger line (three consecutive). §6 HUD
consumes those directly.

Where this goes wrong quietly:

- **심증 cannot read B.** Only row 1 accepts 확정, so a tentative line never
  becomes a counter. Countering demands certainty, and that is what makes the
  third observation a real goal rather than a formality.
- **A counter is not free.** B sitting at GotRead −3 only reaches sum ≤ −3 if A
  has also won the grip — the same structure as canonical case 5 in §4.
  Knowledge alone does not score.
- **This does not stack with §4.2.** If A also declared 카운터 준비, B is already
  on the GotRead floor and drops no further. **The resolver still must not
  pre-apply the step-down** — it supplies the BASE read and Core lowers it. That
  is the contract recorded on `IReadResolver` in §1.
- **Habit knowledge outranks the tendency window.** Rows 1–3 sitting above rows
  4–5 is the whole statement of that priority: knowledge A actively earned over
  three observations beats knowledge handed to B by A's own laziness.
- **A habit that missed its 25 % yields Neutral.** Knowing a habit is worth
  nothing on a turn B did not actually fire it — there is no read without a tell.

**A habit that was not fired never yields GotRead — to either side.** Knowledge
is worth nothing on a turn B did not fire, and declining to fire is not itself
punishable. There are exactly two routes to GotRead against A, and both are
things A did: §4.2 (A let B declare 카운터 준비) and row 5 (A became
predictable). That is the shape the rule wants — reading is beaten only by
reading, and predictability is its own separate mistake.

## 3. ReadLevel — B attacking (tendency window)

Symmetric source, from the data the prototype's 내 성향 panel already collects.
**This is rows 4–5 of the §2 ladder** — consulted only when rows 1–3 found
nothing. If A has knowledge of the habit B just fired, that settles the exchange
and the window is never asked.

- Window: **last 6 turns** of A's commits, keyed on the `(Core, Legs)` pair.
- Same pair appears **≥ 4 times** in the window ⇒ that pair is dominant.
  A's current commit matches it ⇒ **Read +2** when B attacks (row 4).
  This tier does **not** make A's own attacks readable.
- Same pair **3 times consecutively** ⇒ **FullRead +3** when B attacks, and
  **GotRead −3 on A's own attacks** (row 5). This is the danger line.
- Otherwise Neutral.

**Reference point — the window holds PAST commits, and the current one is
compared against it.** So "three consecutive" means three already sitting in the
window and you throw a fourth: the commit that gets punished is the fourth, not
the third. The name and the count look off by one until you read it the way the
player experiences it, which is how it should be written down:

> Throw the same commit three times in a row and the bar lights its danger mark.
> **Throw it again while the mark is lit and you get read.** Change your commit
> and the mark goes out.

The warning has to arrive before the punishment — that was the condition on
adopting the window at all (§7: unannounced reads are indistinguishable from bad
RNG). Counting the current commit into the window instead would punish the very
move that completes the pattern, leaving no turn in which to avoid it. Both
tiers use this same reference point, so one window never carries two different
ones.

**The exposure bar is public** (`MATCH_RULES` §7). A player must be able to see
they are becoming readable *before* it costs them a waza-ari; unannounced reads
are indistinguishable from bad RNG. 수 노트 = the window into B. 내 성향 = the
window into what B sees of A.

## 4. B's algorithm — rule-based + habits

Per turn, an **exclusive ladder** — first match wins, never summed (same shape as
`MATCH_RULES` §6.1, for the same reason).

```
1. hard gates    stamina-0 lockout (§5.5), Locked hands, CounterReady follow-up
                 → remove illegal commits
2. habits        condition holds → fire with probability 0.75, first match only
3. fallback      weighted random over grip / stamina / shido count / clock
```

**0.75, not 1.0.** At 1.0 a 확정 habit makes B deterministic and the match ends as
a lookup. The 25% miss is what keeps a known habit tense.

### 4.1 진도윤 — habits

| | Condition | Action | Counterplay it invites |
|---|---|---|---|
| H1 | own effective grip ≥ +1 | 업어치기 (BigThrow) | concede grip, declare 카운터 준비 |
| H2 | within 2 turns of receiving a shido | 안뒤축감아치기 to make up — a **real attempt**: BigThrow cost, `TechniqueClass.Standard` | wait it out |
| H3 | *reserve — lights up on retirement*<br>own stamina **< 50** AND below the opponent's | 극단적 방어 to burn clock | §6.1 +3/turn and §5.2 −1/turn do the work |
| H4 | own effective grip **< +1** | 오른소매 노림 — a fixed target, no technique | **read the grip approach and throw into it** |
| H4b | *reserve — replaces H4 on retirement*<br>same condition | 왼소매 노림 | observe it again |

**H4 is structural, not balance.** Without it nothing in B's repertoire is
observable early: H1 needs a grip edge B never reaches, H2 needs a shido, H3 is
a reserve. Measured over a 50-exchange smoke match, habits fired 1–4 times total
and ladder rows 1–3 never fired once — the read system was not running at all.
H4 fixes three things with one habit:

1. Its condition holds through the whole opening, so it is **the first habit
   that actually accumulates observations**.
2. B has to contest the grip to reach +1, which is **what makes H1's condition
   reachable** in the first place.
3. It restores `MATCH_RULES` §4.1 canonical case 3 — *"the moment the opponent
   reaches for the grip, 틀어잡기 into 밭다리: grip +2, full read +3, signature
   +2 = 7 = ippon."* That case is **unreachable in play without H4**, because
   there is no way to read B going for a grip. It is exactly ladder row 2:
   attacker A, B fired a non-attacking habit, knowledge at 확정.

H4 retirement is **replacement, not deletion**. A B that stops contesting grips
destroys itself, so the damage counter moves it to H4b and the other sleeve. It
also keeps the read loop alive: the player has to observe the new target from
zero.

H2 has to be a real attempt, not a probe. **A poke never reaches a judgement, so a
habit that pokes can be observed but never punished** — and being punishable is the
entire reason a habit exists. H1 invites 카운터 준비; a poking H2 would invite
nothing but waiting. The technique is the one m0 already gives B: 안뒤축감아치기,
the 2지선다 partner of 업어치기 off the same entry. A real attempt also resets
passivity harder (§6.1 −4, against −2 for a mere attack entry), which is what
"making up for the shido" actually means, and it bills B the BigThrow stamina cost
(5 × grip multiplier). That predictable burn is the habit's price.

H3's floor is deliberate. Plain "behind on stamina" is true in roughly half of all
turns, so a lit H3 would turtle B permanently — and §6.1 (+3/turn) plus §5.2
(−1/turn) would then grind B into a sandbag rather than an opponent. The 50 is
**not a new number**: it is `MatchRulesConfig.StaminaBonusTiredThreshold`, the
same tier §4 already scores from. Both halves of the condition are v1 tunables.

말아업어치기 (stamina < 30, once per match) is **not a habit**: it is a hard rule,
never observable in advance, never logged (`MATCH_RULES` §7).

### 4.2 Retirement and replacement

- Per-habit **damage counter**: +1 whenever the habit fired on a turn where A
  scored yuko-or-better, or countered successfully.
- Counter reaches **2** ⇒ habit is retired permanently for that match.
- Retirement lights up the reserve habit (H3). Without a replacement, a corrected
  opponent degenerates into noise and there is nothing left to read.
- The 수 노트 entry is **not deleted** — it is marked 폐기됨. It is the player's
  scoreboard for the mind game.

## 5. Scouting — what B knows before the first bell

`ScoutingLevel ∈ 0…3`, a field on the opponent definition, derived from the
player's rank/fame. Its only effect is seeding the §3 tendency window.

| Lv | B starts with | Feel |
|---|---|---|
| 0 | empty window | unknown fighter — 진도윤, match 1 |
| 1 | top career pattern preloaded | readable from turn 1 |
| 2 | two patterns + FullRead needs 2 consecutive, not 3 | regional contender |
| 3 | full habit note preloaded | national level — read from the start |

The counter-axis already exists: `MATCH_RULES` §5.4 mastery lowers the opponent's
chance to read that technique by 5 %p per level. Scouting up vs mastery up is the
career's push-and-pull; no new system is needed.

**v1: implement the field, ship it at 0.** 진도윤 = 0. Career work later changes
data, not code.

## 6. Result nodes

Scenario result nodes stop being choices. They become a **presentation
dictionary**: `(Judgement, technique) → 서술 문구`, consumed by Game when Core
emits `JudgementScored{sum, terms[]}`. Core's judgement is the only truth; a node
that disagrees is a stale node, not a second opinion.

Osaekomi escape is no longer scripted either — see `MATCH_RULES` §9.

## 7. Order of work

```
1   isolate the existing scripted path behind ICommitSource / IReadResolver
1.5 extract the ExchangeScript assembler (rename current node adapter →
    ScriptedAssembler) — 3 and 4 cannot plug in without this
2   habit data structure (ScriptableObject candidate) + observation counters
3   LiveReadResolver (§2, §3) + HabitBrainCommitSource (§4)
4   demote result nodes (§6)
5   osaekomi entry/escape per MATCH_RULES §9
6   A-only input UI — hand 5 / core 3 / legs 4
7   regression: 113 green + mainline m36 sum 7
```

Commit and stop after each step. If a decision is needed, report it — do not pick.

## 8. Tests to add

Names state the intent, including the wrong answer, so a deleted rule fails loudly.

```
LiveReadResolverTests
  HabitMiss_YieldsNeutral_NotGotRead
  ThirdObservation_PromotesToFullRead_SecondDoesNot
  ObservationRequiresHabitActuallyFired_NotJustConditionHeld
  ResolverNeverPreAppliesCounterReadyStepDown      // Core §4.2 owns that

TendencyWindowTests
  FourOfSixSamePair_GivesRead
  ThreeConsecutive_GivesFullRead
  WindowIsSixTurns_SeventhTurnDropsOut
  ScoutingLevel0_StartsEmpty

HabitBrainTests
  LadderNeverSums_ExactlyOneHabitFiresPerTurn
  HabitFiresAtSeventyFivePercent_NotAlways
  TwoDamagingHits_RetiresHabit_AndLightsReserve
  RetiredHabitNeverFiresAgain
  SurpriseTechIsNotAHabit_AndIsNeverLogged

OsaekomiTests
  Yuko_WithoutGripEdge_DoesNotEnterOsaekomi
  WazaAri_AlwaysEnters
  AdvFrozenAtEntry_DefenderStaminaDropDoesNotRaiseIt
  ScoreAwardedBeforeEscapeResolves_AtFiveSeconds
  EscapeResetsGripsToFree_ButGrantsNoMatteRecovery

ModeIsolationTests
  ScriptedMode_MainlineStillEndsAt_m36_Sum7
  ScriptedMode_DoesNotInstantiateHabitBrain
```
