# Plan — §H step 7: mastery, escalation stairs, surprise technique decay, combos, stamina intensification

> Groundwork document, not an implementation. Written 2026-08-13(밤), overnight
> autonomous session. Planning only — no code in this cluster was touched.
> Covers MATCH_RULES §F + `DESIGN_future-mechanics.md` §1–§4, which STATUS.md's
> Next Up already bundles into one step because §3/§4 explicitly depend on §E
> (this session's own §H 4 work) and §H 5's cognition list.

## Why these five are one step

`DESIGN_future-mechanics.md` §5 says so directly: combo power and surprise-
technique decay sit **on top of** §E promotion (landed this session) and §H 5's
cognition list respectively — "그 단계들이 먼저 있어야 얹힌다." Mastery (§F)
is the acquisition-curve backbone all of it grows from. Stamina intensification
is the one piece that's actually independent of the others but is bundled here
because it shares StaminaSystem call sites with combo's cost discount (§3.2).

## 1. Mastery (§F) — acquisition, the piece nothing else can skip

**Current state (measured)**: the *consumption* side is already fully built —
`TechniqueRegistry.ClassFromMastery(level, cfg)` (§H step 2) already maps level
→ `TechniqueClass` via `MasteryStandardLevel`/`MasterySignatureLevel` ⚙, and
`StaminaSystem.PayAttack(..., int masteryLevel = 0)` already applies
`MasteryCostReductionPerLevel`. `MasteryStore` (§H 3.5a) is a real, working
dictionary. **What's missing is entirely the earning side** — nothing anywhere
calls `IMasteryStore.Set` with a nonzero value, and nothing injects a real
`MasteryStore` instance into `MatchEngine` in live play (it still defaults to
`ZeroMasteryStore`).

**Touchpoints**:
- A listener on `JudgementScored` (external, same shape as §H 5/6's listeners —
  not a Core-internal call) computes a score-weighted mastery gain: "weighted
  up on a score (yuko < waza-ari < ippon ⚙)... not gated on success — a failed
  attempt is still practice" (§F, confirmed). So the weight function needs a
  nonzero floor for `Judgement.None` too, not just three tiers.
- Diminishing-returns curve (§F, "정말 미세하게" once polished) — needs an
  actual acquisition-curve shape (log, asymptotic, etc.), not specified.
- `LiveMatchRunner`/`MetaFlowController` needs to actually construct a real
  `MasteryStore`, seed it from `SaveData`'s mastery entries at match start
  (schema already exists per §H 3.5a), and pass it into `MatchEngine`'s
  constructor instead of leaving the `ZeroMasteryStore` default.
- High-tier cap (levels 4-5 gated on body stats): **placeholder only per §F
  itself** — "this rule only writes the cap's existence down," the stat/
  training systems are §G scope. Do not build the cap's actual gating logic in
  this step; just don't let mastery silently reach level 4-5 without it if
  that matters (open question below).

## 2. Escalation stairs (`DESIGN_future-mechanics.md` §1) — direct extension of this session's §E work

The doc is explicit that stair-step growth is realized entirely through §E's
promotion math, not the §4 formula: "위력 구현: TechniqueClass는 그대로 §E
승격의 floor/확률에 반영(판정식 항 아님). 계단 도약 = 승격 floor·확률의
비선형 상승." Concretely, this means `JudgementCalculator.Promote`'s
`PromoteBase`/`PromoteSlope` (or an equivalent per-mastery-level modifier) need
a **mastery-level input**, not just the `TechniqueClass` tier they already
resolve to. Two techniques both classed `Signature` (levels 4 and 5) should
promote at different odds once this lands — today's `Promote` only sees
`Judgement floor` and `effectiveSum`, nothing about mastery level at all.

**Gate confirmed, not a decision to make**: "게이트 우회는 없음(확정) — 시그니처라도
상대 밸런스가 온전하면 §4.4′ 게이트로 Sloppy 강등." So the stair only
affects promotion odds/floor *given* the technique class the Balance gate
already allowed — it must not let a well-drilled technique skip the gate.

## 3. Surprise-technique decay (`DESIGN_future-mechanics.md` §2) — depends on §H 5

**Not the same mechanism as the existing `SurpriseExempt`/`SurpriseTechnique`
code** (`Core/TechniqueSpec.cs`, `Brain/SurpriseTechnique.cs`,
`Brain/SetupPrecedingCommitSource.cs` — already live, gates the §4.4 Balance
downgrade for once-per-match techniques like 말아업어치기/안오금띄기). The doc
calls these out as **orthogonal**: "SurpriseExempt(게이트 우회)와 직교 —
변칙기술이 SurpriseExempt일 수도 아닐 수도." This step's mechanism is a
*second* effect layered on named "변칙기술" — their promotion bonus is gated on
`opponent.knownByMe.Contains(techniqueId)` (max bonus if absent, decayed if
present) rather than on the Balance gate at all.

**Hard dependency**: this cannot be built before §H 5's `IRecognitionSet`
wiring lands (specifically, before B's side has an equivalent — recognizing
that §C.1 as written is A's cognition of B; a B-side "does B recognize A's
irregular technique" mirror isn't explicitly named anywhere and may need its
own small design pass, not just reusing §H 5's A-side structure verbatim).

## 4. Combo system (`DESIGN_future-mechanics.md` §3)

**Touchpoints**:
- New per-(a→b) mastery-like value, separate from individual technique
  mastery, grown "by training" (ties to the training-body plan doc's coach-
  pressure/technique-acquisition extensions, not by match play the way
  individual mastery is).
- "즉각뿐" window (§3, confirmed as Q2): b must be the very next technique
  commitment after a, with no grip turn or other action between. Needs a
  **last-technique-committed-per-fighter** tracker that resets on any
  non-technique turn — this is new `MatchEngine` state, similar shape to the
  existing `_setupActive`/`_setupDeclaredThisExchange` per-fighter scratch
  fields, but tracking technique identity instead of a boolean.
- Effect 1 (b's promotion boost) feeds the same §E `Promote` surface that
  escalation stairs (item 2 above) also feeds — **two different inputs
  (mastery level, combo level) both wanting to modify the same promotion
  curve**. Whoever implements this needs to design how they compose (additive?
  the "다 더하면 사기라 보정 ⚙" line in §3 is explicitly about combo alone
  double-counting with individual mastery, so a three-way composition with
  escalation stairs too needs its own thought, not just chaining the same
  clamp).
- Effect 2 (b's stamina discount) interacts with item 5 (stamina
  intensification) below — a combo discount is meant to partially offset the
  intensification cost curve, not stack multiplicatively into a net-zero or
  net-negative cost.
- **Save schema gap, unlike §H 5/6**: `linkages{"a→b"→level}` is explicitly
  **not yet reserved** in `SaveData` (STATUS: "[선택] — 나중에 채워도 마이그레이션
  없이 되는 이점 vs 지금은 안 쓰는 필드를 미리 넣는 부담, 사람 판단"). Unlike
  `knownByMe`/`adaptation`, this needs an actual schema-extension decision
  before any wiring, not just consumption of an existing field.

## 5. Stamina intensification (`DESIGN_future-mechanics.md` §4) — the one independent piece, but the riskiest for fingerprint

Two axes: escalating per-technique cost since the last matte (first attempt
−5, next −6, next −7... ⚙), and a low-stamina cost multiplier. **The source
document itself flags the exact risk correctly** — "StaminaSystem 변경 =
규칙 변경. MATCH_RULES 개정 + 42노드 지문 영향 반드시 확인(스크립트 경로가
격화 카운터를 밟는지 — 밟으면 지문 깨짐, 기본 off 관행 필요)." Concretely:

- Needs a new persistent per-fighter counter ("exchanges since last matte"),
  incrementing on technique cost payment and resetting in `CallMatte` — same
  shape as `_setupActive`'s reset-on-matte lifecycle, but counting instead of
  flagging.
- `StaminaSystem.PayAttack`/`Spend` need to read this counter to compute the
  escalating cost — this touches the SAME method the scripted 42-node path
  calls on every single technique in the mainline script. **This is exactly
  the kind of change that needs the standard flag-gate pattern
  (`BalanceGate`/`PromotionGate`'s "off reproduces v1 대조 모드 exactly"
  contract)**, not the "optional field defaults to inert" argument used for
  §H 5/6 above — because unlike an optional per-turn field, a stamina-cost
  formula change applies unconditionally to every technique cost calculation
  regardless of what any single call site passes in. Skipping the gate here
  is the most likely way this whole cluster breaks the fingerprint.
- Needs its own MATCH_RULES amendment (rules change, not just an
  implementation detail) before landing, per the source doc's own explicit
  instruction.

## Dependencies / ordering (summary)

```
§H 4 (this session) ──┬─→ §1 escalation stairs ──┐
                       └─→ §3 combo promotion ────┼─→ (compose: open question)
§H 5 (cognition) ──────────→ §2 surprise decay    │
mastery earning (§F, no dependency) ──────────────┘
§4 stamina intensification — independent, but highest fingerprint risk
```

Mastery earning (§F) has no hard dependency and could be picked up first
independent of everything else in this cluster. §1/§2/§3 all specifically need
something else to land first. §4 can be done anytime but should be done
*carefully* given the fingerprint risk noted above.

## ★ Decisions needed before implementation (not made here)

1. **Composition of escalation-stairs + combo, both feeding §E's promotion
   curve** — additive, multiplicative-with-a-cap, or something else. §3's "다
   더하면 사기" line only covers combo-vs-individual-mastery double counting.
2. **Combo acquisition mechanism** — "grown by training," but the training
   plan doc's coach-pressure extension doesn't yet name *which* rep sessions
   would grow a linkage vs. an individual technique's mastery. Needs a
   decision connecting the two plan docs, not made in either alone.
3. **`linkages` schema reservation timing** — reserve the empty field now
   (no migration later) or wait until §H 7 actually starts (don't carry unused
   schema). STATUS has carried this open since 2026-08-13 and it's still open.
4. **Mastery high-tier cap enforcement** — §F says the cap's existence should
   at least be "written down" even before §G's stat system exists. Does that
   mean levels 4-5 should be soft-blocked (capped at 3) until §G lands, or is
   it fine for mastery to freely reach 4-5 with the cap simply unenforced
   until then? Not specified.
5. **Stamina intensification's escalating-cost target** — "기술은 급상승, 다른
   동작(잡기·수비 등)은 미세 상승 ⚙" — needs the actual per-action-type curve
   parameters, and needs a MATCH_RULES amendment pass before code, per the
   source doc's own instruction (not a code-only decision).
6. **Surprise-decay's B-side recognition mirror** — does §H 5's
   `IRecognitionSet` (built for A's cognition of B) get reused generically for
   "does fighter X recognize fighter Y's technique Z," or does B need a
   parallel, separately-scoped instance? Not named anywhere in either design
   doc.
