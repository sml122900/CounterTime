# Plan — training body: coach-pressure axis + cardio disciplines + technique-acquisition rhythm game

> Groundwork document, not an implementation. Written 2026-08-13(밤), overnight
> autonomous session. Planning only — no code in this cluster was touched.
> Covers `DESIGN_training.md` §7 (both bullets) and `DESIGN_future-mechanics.md`
> §7's first bullet (technique acquisition), which belongs here rather than in
> the execution-input plan doc because it's about *learning* a technique, not
> *using* one mid-match — the second §7 bullet (대련 실행 입력 층) is covered in
> `docs/plan-execution-input.md` instead.

## Current code state (measured)

`Assets/_Project/Scripts/Game/Training/` (landed 2026-08-13, §H 5(A) — note this
is a *different* "5" than MATCH_RULES' §H step 5; STATUS numbers it as a
training sub-step, not the cognition step covered in
`docs/plan-cognition-predictive-counter.md`):
- `TrainingCatalog` (ScriptableObject) — `Entry{id, displayName,
  overrideController}` list. Adding a discipline is purely data (§3's own
  design goal: "종목 추가 = 오버라이드 에셋 +클립 슬롯 채우기, 코드 변경 0").
- `TrainingSessionController` — v1 "press = 1 rep" state machine:
  `Ready → Rep(loop) → Strained → Collapse`, driven by two ⚙ numbers
  (`fatiguePerRep`, `fatigueLimit`) plus `strainedStartFatigue` for the blend
  point. `OnRepInput()` is the single entry point (button click or Space/Enter).
  No timing/rhythm scoring exists — input is a bare counter increment.
- `MetaFlowController.RecordTrainingResult(TrainingSessionController.Result)`
  exists as an empty hook, called on every discipline's collapse — this is
  where any new session-result data (coach-pressure outcome, rhythm-game score)
  would flow toward the save layer once §H 7 wires it, per STATUS.

## 1. Cardio disciplines (interval/mountain running) — low-risk, data-only

Per §3's own design, this needs **no controller/session-machine changes** —
just new `TrainingCatalog.Entry` rows plus placeholder Animator Override
Controller assets (`TrainingAssetBuilder.cs`'s existing menu already generates
one per catalog entry, skipping ones that exist — same "never overwrite a
filled slot" guard that protects real mocap). The only actual decision is
where in the §4 discipline table they sit and what their "rep" *means* for a
running motion (a rep-counter doesn't map as cleanly to a running cycle as it
does to a pull-up) — this is content/authoring judgment, not an engineering
problem, and is explicitly left open in DESIGN_training §7's own "판단 필요"
list. Nothing here blocks on any other plan doc.

## 2. Coach-pressure axis — the real design lift

Today's v1 has exactly one failure mode (fatigue hits the limit → collapse →
session ends) and exactly one input signal (did the player press). The
coach-pressure extension adds a **second, opposite failure mode**: being too
passive or off-tempo draws the coach's attention and "scolds" the player. The
source doc frames it as a genuine tension, not a second progress bar to
optimize: "방전(너무 몰아붙임)"과 "코치 눈치(너무 안 함/템포 이탈)" 두 실패
사이를 타며 훈련 세션을 완주" — closer to §2's numeric-brinkmanship shape
(MATCH_RULES' own core tension) than to the current single-axis endurance test.

**What would need to change in `TrainingSessionController`**:
- A second state variable (training-stamina, distinct from `Fatigue` — the
  source doc keeps them conceptually separate: "스페이스 입력이 동작 수행 +
  훈련 체력 소모... 입력을 안 하면 훈련 체력은 회복되지만 코치의... 눈치
  지수형이 강압적으로 압박"). Needs its own recovery-while-idle curve, mirror
  image of `Fatigue`'s climb-while-active curve.
- A tempo/suspicion tracker — needs wall-clock or frame-delta timing between
  `OnRepInput()` calls (nothing in the current controller reads time at all;
  it's purely input-count-driven). "코치의 훈련 지시" implies there's a target
  cadence to miss, which itself needs a data source (per-discipline? per
  difficulty tier? not specified).
- A "scolding" penalty effect — completely unspecified in the source doc
  beyond "혼남." Could be: early session termination (a second collapse-like
  end state, needs its own Animator state or reuses Strained/Collapse?), a
  result-quality debuff (fewer rep credit toward whatever §H 7 eventually
  connects reps to), or purely cosmetic (a scolding line/animation with no
  mechanical cost). Not decided anywhere.
- **Open per DESIGN_training §7's own "판단 필요"**: does this REPLACE v1 for
  every discipline, or do the two coexist per-discipline (e.g. explosive
  disciplines keep the simple "push to the limit" v1, endurance disciplines
  get the coach-pressure version)? This is a scope-defining decision, not an
  implementation detail — recommend resolving it before writing any code,
  since it changes whether `TrainingSessionController` gets a mode flag or a
  wholesale rewrite.

## 3. Technique-acquisition rhythm game (`DESIGN_future-mechanics.md` §7, first bullet)

A materially different subsystem from the rep-training controller above — this
is about *learning a new technique*, not conditioning. Per the source doc:
break a technique into ordered body-part segments (example given: 한팔
업어치기 = 왼팔→오른발→오른팔→왼팔→몸통 + final 매치기), demonstrate each
segment, then require a timed input per segment to "learn" it. Two properties
that matter for planning:

- **No existing data representation.** `TechniqueRegistry`/`TechniqueDef`
  (Core, §H step 2) carry `Id`/`DisplayName`/`AlgoClass`/`JudoCategory`/
  `BaseCost`/`SurpriseExempt` — nothing about a motion breakdown into ordered,
  timed segments. This needs an entirely new data structure (Game-side, since
  it's presentation/input, not match logic — matches the existing Core/Game
  boundary rule), most likely something like an ordered list of
  `(bodyPart, clipSegment, timingWindow)` per technique id, authored
  per-technique the way `NodeMotionMap`/`TrainingCatalog` are (ScriptableObject
  data, code stays generic).
- **Explicitly meant to share its input scheme with match-time execution
  input** — "경기 중 부위 지정 입력(철권식 끊기 계열)과 입력 방식을 통일...
  훈련에서 익힌 순서 자체가 경기로 그대로 전이된다는 게 핵심." That match-time
  system (`docs/plan-execution-input.md`) **does not exist in any form yet
  either** — there is no "철권식 끊기" input scheme anywhere in the current
  codebase to unify with. This means the rhythm game can't just adopt an
  existing input scheme; whoever builds it is *defining* that shared scheme
  for the first time, via this feature.
- **§7's own ordering note (ⓒ) says this should come first**: "기술 습득이
  실행 입력의 예행연습이자 원형이다" — build the acquisition rhythm game,
  validate the input scheme against it, *then* port that scheme into the
  live-match execution layer. `docs/plan-execution-input.md` cross-references
  this same ordering — don't start match-time execution input before this
  piece has a working prototype.

## Dependencies / ordering

- Cardio disciplines: independent, can be done anytime, lowest risk in this
  whole overnight batch.
- Coach-pressure axis: independent of everything else in this document, but
  the scope question (replace vs. coexist with v1) should be resolved before
  writing code, not discovered mid-implementation.
- Technique-acquisition rhythm game: no hard dependency on other clusters, but
  is the *prerequisite* for `docs/plan-execution-input.md`'s match-time system
  per the source doc's own explicit ordering (ⓒ). Should land before, not
  after, execution-input work starts.
- None of this touches Core/Brain or the 42-node fingerprint — everything here
  is `Assets/_Project/Scripts/Game/Training/` and new Game-only data, matching
  the existing §5(A) scaffold's own boundary.

## ★ Decisions needed before implementation (not made here)

1. **Coach-pressure axis: replace v1 or coexist per-discipline?** Blocks
   whether `TrainingSessionController` needs a mode flag or a rewrite.
2. **Scolding penalty's actual effect** — session-ending, result-debuffing, or
   cosmetic-only. Not specified anywhere.
3. **Target-cadence data source** for the tempo/suspicion tracker — per
   discipline, per difficulty tier, or a single global rate. Not specified.
4. **Technique-segment data authoring format** — how many segments per
   technique, whether segment count/complexity should scale with the
   technique's mastery tier (a beginner technique might reasonably need fewer,
   simpler segments than a signature one) or stay uniform. Not specified.
5. **The shared input scheme itself** (button mapping, timing-window
   tolerance, feedback on a near-miss vs. a clean hit) — this is being defined
   for the first time by whichever of {rhythm game, execution input} gets
   built first; per §7(ⓒ) that should be the rhythm game, but the scheme's
   actual shape is not designed in either source document.
