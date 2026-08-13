# Plan — live-match execution input layer (`DESIGN_future-mechanics.md` §7, second bullet)

> Groundwork document, not an implementation. Written 2026-08-13(밤), overnight
> autonomous session. Planning only — no code in this cluster was touched.
> Companion to `docs/plan-training-body.md`, which covers the first §7 bullet
> (technique-acquisition rhythm game) — read that doc first. The source
> document itself orders these two (ⓒ): the acquisition rhythm game should be
> built and validated before this one, since it's meant to be this feature's
> prototype and the source of its shared input scheme.

## What this is

Once a technique's condition is met mid-match — a successful §D prediction, or
(per one of the open questions below) possibly any committed technique at
all — the player must additionally execute a timed body-part button sequence
for it to actually fire. Not a single QTE press; a sequence, matching whatever
scheme the acquisition rhythm game (companion doc) establishes. Fail the input
and the attempt is void. The doc frames this as **stacking on top of** §D, not
replacing it: "단순 QTE 1회 입력이 아니라 §D 예측 되치기의 조건 충족 위에
얹는 추가 관문." It also gives the player's own hand-skill a growth axis
independent of character stats — "플레이어 본인의 손 실력이... 반복할수록
성장한다... 캐릭터 숙련도와는 다른 축."

## The key architectural point (recommendation, not yet verified)

Every other plan doc in this overnight batch (§H 5/6/7's mastery/adaptation/
combo pieces) needed real `MatchEngine`/`Core` surgery, because they change
values that feed the judgment formula or promotion roll. **This feature does
not have to.** What it decides is binary and upstream of resolution: did the
input succeed, so does a technique commitment actually reach the engine at
all. That decision can be made entirely in the Game/UI layer, *before*
`LiveMatchRunner.SubmitA(...)` is called — if input fails, submit the turn as
if no technique had been declared (or downgrade it, per the open question
below), exactly the same shape as any other player input decision. Core never
needs to know execution-input gating exists.

If this holds up under actual implementation, it means this specific feature
carries close to zero fingerprint risk (no `MatchEngine`/`JudgementCalculator`
changes at all) — a meaningfully different risk profile than every other
cluster planned tonight, worth flagging explicitly so whoever picks this up
doesn't over-engineer a Core-side gate that isn't needed. This is a
recommendation based on reading the current `LiveMatchRunner.SubmitA` call
shape (`Core/MatchEngine.cs`'s `ResolveExchange` takes a fully-formed
`ExchangeScript` — nothing stops the Game layer from deciding what goes into
that script before the call), not a verified fact — build a small spike before
committing to it as the final architecture.

## Code touchpoints (if the above architecture holds)

- A new Game-layer input-collection step between "player declares intent to
  use technique X" (today: picking X from the technique wheel, per §H step
  2b) and `SubmitA` actually being called. This is new UI flow, not a
  retrofit — nothing like a timed multi-step input sequence exists anywhere
  in `LiveMatchPanel` today (existing input is all single-click wheel
  selections, no timing component).
- Needs the same segment/timing data structure the acquisition rhythm game
  defines (companion doc) — this is the whole point of unifying the two, so
  building this feature's data format independently of that one would be
  redoing the same design twice.
- A miss-handling decision (see open questions) — does a failed input mean
  "no technique this turn" (submit `Attacker = null`) or "technique still
  attempted, but degraded" (submit with `AuthoredClass = Sloppy` regardless of
  actual mastery)? These have different `ExchangeScript` shapes and different
  gameplay feel (the latter still burns the opponent's exposure/read state per
  §C even on a failed input, the former doesn't).

## The three open design questions, as the source document leaves them

The source document is explicit these are undecided and should stay that way
until deliberately resolved — reproduced here rather than summarized, since
compressing them risks losing the reasoning:

**ⓐ Coupling with §D predictive counter.** Chaining "read succeeds → counter
window opens → execution input must also succeed" would make execution input
the mechanical gate that makes "완벽한 되치기" rare — which the doc notes is
*already* §D's own stated philosophy ("회피만 해도 성과, 완벽한 되치기는
흔치 않아서 아름답다"), so this coupling would reinforce rather than fight the
existing design. This is the doc's own strongest hint at a preferred
direction, but it is explicitly not confirmed as the decision.

**ⓑ Balance risk.** Requiring execution input on *every* technique risks
displacing the game's actual core loop (수싸움 — reading, setup, the
mind-game) with a reflex/rhythm-game skill check. "언제 요구하나" (every big
throw and counter, or only decisive moments) is called the central open
question, deliberately left undirected. Recommend whoever picks this up reads
it alongside `docs/plan-cognition-predictive-counter.md`'s and
`docs/plan-mastery-and-growth.md`'s notes on how many other systems already
gate on "did a technique commit, and how" — stacking execution input on top of
all of them simultaneously (Balance gate, promotion roll, cognition/adaptation
ladder rows, now a manual input check) is a lot of gates for one commitment,
and might argue for restricting execution input's scope narrowly (e.g. counter
attempts only, per ⓐ) rather than broadly.

**ⓒ Build order.** Match stability first (Balance axis — already done;
passivity/3-referee-category judgment — already done), then the acquisition
rhythm game (companion doc) as the prototype, *then* port its validated input
scheme into this match-time layer. Already reflected in this doc's framing and
in the companion training-body doc's cross-reference — restated here as the
authoritative source, not a new conclusion.

## Dependencies / ordering

- **Hard prerequisite**: `docs/plan-training-body.md`'s technique-acquisition
  rhythm game should exist and have a validated input scheme before this
  starts, per §7(ⓒ). Do not design this feature's input scheme from scratch
  independent of that one.
- **Soft interaction**: `docs/plan-cognition-predictive-counter.md`'s §D work
  (predictive counter naming) is the most likely coupling point per open
  question ⓐ — whoever resolves ⓐ should read that plan doc's own open
  questions first, since a `PredictedTechniqueId`-shaped mechanism and an
  execution-input-shaped mechanism would both be layering onto the same
  CounterReady declaration.
- No dependency on §H 4 (promotion, this session), §H 6 (adaptation), or §H 7's
  mastery/combo/stamina pieces — this cluster is architecturally closer to a
  pure input/UI feature than a rules feature, assuming the architectural
  recommendation above holds.

## ★ Decisions needed before implementation (not made here)

1. **ⓐ** — does execution input couple to §D's predictive-counter success, or
   is it independent/broader?
2. **ⓑ** — scope: every technique, big-throws-and-counters-only, or
   counters-only? This is the single highest-leverage decision in this whole
   document for whether the feature enhances or undermines the game's core
   loop.
3. **Miss-handling** — void the attempt entirely, or degrade it (Sloppy-forced,
   still costs stamina/still exposes the technique to opponent adaptation)?
   Not addressed by the source document at all; purely an implementation-time
   question raised by trying to plan this concretely.
4. **The architectural claim itself** (Game-layer-only, zero Core changes) —
   stated here as a recommendation worth spiking before trusting, not a
   verified fact.
