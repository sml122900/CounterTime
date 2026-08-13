# Plan — §H step 5: cognition list + predictive counter (MATCH_RULES §C.1 + §D)

> Groundwork document, not an implementation. Written 2026-08-13(밤), overnight
> autonomous session. No code in this cluster was touched — §H 4 (judgment
> promotion) is the only game-code change this session made; everything below is
> planning only, per the session's own absolute lines (no design decisions, no
> code that isn't either §H 4 or a flag-off inert skeleton).

## What this step is

Two things MATCH_RULES bundles together because they share data:

1. **§C.1 — A's cognition list.** When B commits technique X and judgment fires,
   X gets logged (after the exchange resolves) into a per-opponent, permanent
   "A knows about X" set. Before logging, X cannot be named as a §D prediction
   candidate.
2. **§D — Predictive counter.** When A declares CounterReady, A may additionally
   name one technique from the cognition list (or decline — "no prediction").
   - No prediction: unchanged §4.2 (1-rung read drop).
   - 정독 (correct guess): **2-rung drop** — reaches GotRead even from FullRead,
     which the current no-prediction path cannot do (FullRead only falls to Read).
   - 오독 (wrong guess): triple penalty — §4.2's general protection is void (no
     posture exemption), the attacker's sum gets **+1** ⚙, and the CounterReady
     cost is not refunded.

## Current code state (measured, not assumed)

- `IRecognitionSet`/`RecognitionSet` already exist (`Core/IRecognitionSet.cs`,
  landed in §H 3.5a). **Zero consumers today** — nothing calls `Knows`, `Add`, or
  `Snapshot`. It is a real, working container sitting inert.
- `SaveData.OpponentSaveEntry.knownByMe` (`List<string>`) already exists in the
  save schema (`Game/Save/SaveData.cs`) — reserved, unpopulated (STATUS 2026-08-12:
  "opponents[] 는 id/name 만 채운다... knownByMe 실채움은 §H 5/6 이월"). No schema
  change needed to seed/extract this list; the wiring is the whole job.
- `TurnChoice` (`Core/TurnChoice.cs`) has no field for a predicted technique. Its
  `Legs == LegAction.CounterReady` is a plain boolean commitment — there is no
  slot for "and I think it's X."
- `MatchEngine.ResolveTechnique` (`Core/MatchEngine.cs` ~line 469-478) applies the
  no-prediction path unconditionally today:
  ```csharp
  if (defenderCounterReady && script.Technique.Cost == ActionCost.BigThrow)
  {
      ReadLevel lowered = JudgementCalculator.StepDownRead(read, _cfg.CounterReadyReadSteps);
      ...
  }
  ```
  `JudgementCalculator.StepDownRead(r, steps)` already supports an arbitrary step
  count — the 2-rung 정독 case is `steps: 2`, not new plumbing. The mechanism for
  "drop further" already exists; what's missing is the branch that decides
  *which* step count applies, and the +1/no-refund/no-exemption branch for 오독.

## Code touchpoints (for whoever picks this up)

1. **`TurnChoice`**: add an optional field, e.g. `string PredictedTechniqueId`
   (null = no prediction), via a new constructor overload with a default value —
   same pattern as `OwnHand`'s optional 4th parameter. Existing call sites (both
   scripted and live) stay source-compatible without touching them.
2. **`MatchEngine.ResolveTechnique`**: branch on
   `defenderChoice.PredictedTechniqueId`:
   - `null` → today's unconditional `StepDownRead(read, cfg.CounterReadyReadSteps)`.
   - `== script.Technique.Id` (정독) → `StepDownRead(read, cfg.PredictiveCounterCorrectSteps)`
     (new ⚙, value 2 per §D.3).
   - non-null and mismatched (오독) → §D.5's triple penalty: skip the
     `StepDownRead` call entirely (protection void), add `cfg.MisreadSumPenalty`
     ⚙ (+1) to the attacker's `sum` before bracketing, and confirm the existing
     `Stamina.PayCounter`-on-confirm path already denies any refund (it should —
     counter cost is paid only on a *confirmed* counter judgment, and 오독 by
     definition isn't one, so verify this falls out for free rather than needing
     new code).
3. **Fingerprint safety**: because `PredictedTechniqueId` defaults to `null` and
   the `null` branch reproduces today's exact call, **no separate boolean gate
   should be needed** — unlike `BalanceGate`/`PromotionGate`, which guard
   always-on state, this guards an optional per-turn field that scripted mode
   never populates. This is a claim to verify against the 42-node fingerprint
   before trusting it, not a decision already made — if anything about the
   branch structure makes the `null` path not byte-identical to today's, fall
   back to the standard flag-gate pattern instead.
4. **`IRecognitionSet` wiring** (the actual §C.1 half): NOT a Core-injected
   dependency the way `IMasteryStore`/`IAdaptationLedger` are — recognition
   doesn't feed the judgment formula, it only gates what the *input* layer
   offers as nameable predictions. So:
   - **Write side**: a Brain/Game listener on `JudgementScored` (same shape as
     `ObservationLedger`'s existing habit-observation listener) calls
     `recognitionSet.Add(techniqueId)` once, after the exchange resolves, when
     B was the one who committed the technique. Mirrors the existing "listen to
     events, don't have Core call in" pattern already used for habit
     observation — `IMasteryStore`/`IAdaptationLedger` are the exception
     (Core-injected) precisely because they must be read *during* resolution;
     recognition isn't read during resolution at all.
   - **Read side**: whatever UI/assembler builds the CounterReady declare step
     (`LiveMatchPanel` or a new predictive-counter input flow, not designed yet
     — see open questions) filters the offered technique names through
     `recognitionSet.Knows(techId)`.
   - **Persistence**: `SaveSystem`/`MetaFlowController` seed `RecognitionSet`
     from `OpponentSaveEntry.knownByMe` at match start and extract
     `Snapshot()` back into it at match end — same shape as the mastery/
     adaptation seed-and-extract already sketched in CORE_DESIGN's persistence
     seam section, just not yet wired for any of the three interfaces.

## Dependencies / ordering

- Needs §H 4 (judgment promotion) done first only in the sense that both touch
  `MatchEngine.ResolveTechnique` in nearby code — no functional dependency,
  landed first this session, so no conflict risk for whoever does §H 5 next.
- §C.3 decay (fading cognition) is described in the §H 6 plan doc (paired with
  B's adaptation decay) since MATCH_RULES treats it as one symmetric mechanism —
  don't build A's decay without also reading that doc.
- The predictive-counter input UI itself doesn't exist anywhere yet (no
  "name a technique" picker in `LiveMatchPanel`) — this is new UI surface, not a
  retrofit of existing radial-wheel code, and needs its own design pass on
  where it lives (probably a sub-step of the hand/core/legs wheel opening when
  CounterReady is picked, but that's a UI decision, not made here).

## ★ Decisions needed before implementation (not made here)

1. **+1 misread penalty timing/scope** (§D.5.2): does it apply to the sum
   *before* bracketing (shifts which bracket the attacker lands in) or as a
   flat bonus applied after bracket assignment? MATCH_RULES says "the
   opponent's judgment sum gets +1" — read literally this is pre-bracket, but
   it wasn't asked to nail down precedence against §E's promotion roll (does a
   misread's +1 also feed into `effectiveSum` for the promotion dice, or only
   the deterministic floor?). Recommend: pre-bracket, and yes it feeds
   `effectiveSum` too, since promotion's `effectiveSum` is defined as "the
   magnitude the floor bracket was assigned from" — but this is a
   recommendation, not confirmed in MATCH_RULES.
2. **UI location for the technique-name picker.** Not sketched anywhere. Needs
   its own small design pass: modal on CounterReady? A second wheel stage
   (mirrors the existing hand-axis 2-stage picker precedent)? Or deferred to a
   later polish pass with "no prediction" as the only functional path for now?
3. **PredictedTechniqueId's fingerprint-safety claim** (touchpoint #3 above) is
   unverified — someone needs to actually build it and run `--check` before
   trusting "no gate needed."
4. **How `IRecognitionSet` reacts to the once-per-match surprise techniques**
   (말아업어치기 etc.) — §C.1 says surprise unpredictability "falls out on its
   own" since it can't be named before its first appearance, but should its
   *first use* register into `knownByMe` the same way a normal technique does
   (so it's nameable in a hypothetical rematch/next match), or does its
   once-per-match nature mean recognizing it is pointless? MATCH_RULES doesn't
   say; needs a decision.
