# Plan — §H step 6: B's adaptation to A's technique usage (MATCH_RULES §C.2 + §C.3 decay)

> Groundwork document, not an implementation. Written 2026-08-13(밤), overnight
> autonomous session. Planning only — no code in this cluster was touched.

## What this step is

Symmetric to §H 5's cognition list, but the other direction: **B learns A**, not
A learning B.

- **§C.2**: once A has used technique X ⚙ (proposed 3) times, A's own ReadLevel
  drops one rung on every subsequent use of X — implemented as **an added row
  in the exclusive read ladder**, the §4 formula itself untouched. "Every
  GotRead route against A traces back to something A did" (`OPPONENT_BRAIN §2`)
  — this row is a new instance of that principle, not an exception to it.
- **§C.3 decay** (shared with §H 5's cognition, described once here since
  MATCH_RULES treats it as one symmetric mechanism): if A stops using X for ⚙
  duration, B's adaptation to X decays one rung; A's cognition of B's habits
  decays symmetrically, with a "fading" stage — re-reading a faded technique
  pays out one rung less than the normal §D reward. MATCH_RULES explicitly
  defers the numbers ("Numbers wait for the career design pass") and calls out
  building the actual decay/cognition brain as **its own future design round**,
  not something to wing here.

## Current code state (measured)

- `IAdaptationLedger`/`AdaptationLedger` already exist (`Core/IAdaptationLedger.cs`,
  §H 3.5a). **Zero consumers today.** `ExposureOf`/`Seed`/`Snapshot` all work;
  nothing calls them.
- **Missing a write method.** `AdaptationLedger` only has `Seed` (wholesale
  replace, for loading a save) — there is no `RecordUse(techId)` that increments
  a count during a live match. §C.2 needs per-use incrementing that `Seed`
  doesn't do (`Seed` is explicitly documented as "not a merge" — a fresh match
  seeds once, full stop). This is a small, mechanical gap: one new method on
  the interface and its implementation.
- **The read ladder cannot see the technique id.** This is the real
  touchpoint and the reason this is a planning doc, not a same-session
  implementation. Traced the call site (`LiveMatchRunner.cs` ~line 166-172):
  ```csharp
  TechniqueSpec technique = bAttacks ? _bSource.LastTechnique
                          : aAttacks ? liveTechnique : null;
  ReadLevel read = attacker.HasValue ? _resolver.For(attacker.Value, a, b) : ReadLevel.Neutral;
  var script = new ExchangeScript { Attacker = attacker, Technique = technique, Read = read };
  ```
  `technique` is already known by the time `_resolver.For(attacker, a, b)` runs,
  but `IReadResolver.For`'s signature (`ReadLevel For(FighterId attacker,
  TurnChoice a, TurnChoice b)`) has no slot for it. §C.2's row needs to know
  "is this a use of a technique A has now used ⚙+ times" — which needs the
  technique id inside the resolver.

## Code touchpoints

1. **`IAdaptationLedger`**: add `void RecordUse(string techId)` (increments,
   creates the entry at 1 if absent) alongside the existing `ExposureOf`/`Seed`/
   `Snapshot`. Small, additive, no existing caller breaks.
2. **`IReadResolver.For` signature extension**: needs the technique id (or the
   whole `TechniqueSpec`) added as a parameter. This is a breaking interface
   change, but a mechanical one — both implementations (`LiveReadResolver`,
   `ScriptedReadResolver`) need the new parameter; `ScriptedReadResolver`
   simply ignores it (scripted mode's read comes from the node data, not from
   any live derivation) so **the scripted path's actual return values don't
   change**, only its method signature does. Verified this doesn't need a
   feature flag the way `BalanceGate` does — traced why: `AdaptationLedger`
   starts empty in every existing test and the 42-node harness (nothing calls
   the new `RecordUse` yet outside a §H-6-specific wiring point), so the new
   ladder row's `exposure >= threshold` condition is structurally unreachable
   in every path that doesn't deliberately seed it. This is the same shape of
   argument as §H 5's `PredictedTechniqueId` claim — **unverified until someone
   actually builds it and runs `--check`**, not a decision already proven safe.
3. **`LiveReadResolver.For`**: add the new row to the existing exclusive
   ladder (`Brain/LiveReadResolver.cs`). Order matters — the class-level doc
   comment is explicit that row order IS the rule ("순서가 곧 규칙이다"), so
   where this row slots in among the existing 6 needs care, not just appending
   at the end. Candidate placement: probably belongs near row 5 ("A has become
   predictable" — 성향 창, consecutive-match pattern) since both rows punish A
   for being predictable, but whether adaptation should outrank or be outranked
   by the tendency window is an open call (see below).
4. **Who calls `RecordUse`**: needs a listener on `JudgementScored` (or
   earlier, on attack commitment — MATCH_RULES says "once A has exposed X ⚙
   times", not "once A has scored with X", so likely every attempt counts, not
   just successful ones — mirrors §C.1's parallel wording for cognition, which
   explicitly logs on judgment firing regardless of outcome). Same shape as
   §H 5's recognition-set listener — an external observer reacting to Core's
   emitted events, not a Core-internal call.
5. **Persistence**: `OpponentSaveEntry.adaptation` (`List<AdaptationEntry>`,
   already in the schema per §H 3.5a) seeds `AdaptationLedger.Seed(...)` at
   match start and extracts `Snapshot()` at match end — same shape as §H 5's
   `knownByMe` wiring, not yet done for any of the three interfaces.

## §C.3 decay — shared with §H 5, deliberately not designed here

MATCH_RULES is explicit that decay's numbers and its actual "rival-scouting
brain" logic are future work, not something this pass should invent. What IS
clear enough to plan around:
- Decay happens **at the save layer** (Game), not in Core — "Core only ever
  receives an already-seeded value... keeps any notion of 'how many matches
  have passed' out of Core" (CORE_DESIGN persistence seam section). So decay is
  a `SaveSystem`/`MetaFlowController`-side transform applied to
  `OpponentSaveEntry.adaptation`/`knownByMe` between matches, not a Core
  feature at all.
- The "fading stage" (re-reading a faded technique pays a reduced reward, one
  rung down from §D's normal payoff) implies cognition entries need a
  **tri-state**, not a boolean `Knows`/doesn't-know — fresh / faded / (forgotten
  entirely, if decay can go all the way back to unknown — MATCH_RULES doesn't
  say). `IRecognitionSet.Knows(string) : bool` as it exists today cannot
  represent "faded" — this is a real interface gap, not just a values question.

## Dependencies / ordering

- No functional dependency on §H 4 or §H 5, but shares the `IReadResolver`
  interface surface with §H 5's `LiveReadResolver` changes — whoever picks up
  either should check the other hasn't landed a conflicting edit to the same
  file first.
- §C.2's read-ladder row placement interacts with `OPPONENT_BRAIN §2`'s
  existing 6-row ladder, which is a Brain-package design surface with its own
  history of "measurement changed the row order" (STATUS 2026-08-10: "Ladder
  order is narrowest-condition-first... an exclusive ladder lets every rung
  starve the ones below it"). Recommend re-measuring row-fire frequency after
  adding the new row the same way that stage did, not just assuming the slot
  chosen in code review is right.

## ★ Decisions needed before implementation (not made here)

1. **Ladder row placement** — where §C.2's new row sits relative to the
   existing 6 rows in `LiveReadResolver.For`. Affects which condition wins when
   both are simultaneously true (e.g. A is both in a predictable-tendency
   window AND has over-exposed technique X).
2. **What counts as "exposed"** — every attempt (success or fail, matching
   §C.1's wording for the symmetric cognition side), or only scored attempts?
   MATCH_RULES doesn't disambiguate for §C.2 specifically the way it does for
   §C.1.
3. **§C.3 decay numbers and mechanism** — explicitly deferred by MATCH_RULES
   itself to a later design round; flagged here only so it isn't silently
   forgotten when §H 6 is picked up. Includes the tri-state `Knows`/`Knows-but-
   faded`/`doesn't-know` interface gap noted above.
4. **`RecordUse` trigger point** — on judgment firing (mirrors §C.1) or on
   commitment (before resolution)? Affects whether a countered/failed
   technique still counts toward the ⚙3 exposure threshold.
