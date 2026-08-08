<!-- v3.1 | body:a08ed4f -->
# CORE_DESIGN_MatchEngine.md — CounterTime.Core implementation design

Implements `MATCH_RULES.md` (that file wins on any conflict). Target:
`Assets/_Project/Scripts/Core/` — the folder that actually holds the
**CounterTime.Core** asmdef. (Earlier drafts said `Assets/_Project/Core/`; files there
would land outside the asmdef and compile into Assembly-CSharp with engine references,
breaking the purity rule.)

## Hard constraints
- **Pure C#.** No `UnityEngine`, no `MonoBehaviour`, no `ScriptableObject`, no
  `UnityEngine.Random`, no `DateTime.Now`, no statics holding state.
- All randomness through injected `IRandomSource`; all tunables through a
  `MatchRulesConfig` instance ⇒ every test is deterministic and every number is
  balance-patchable without touching logic.
- Engine communicates outward via an event list (`IReadOnlyList<MatchEvent>`)
  returned per exchange. Core never *presents* anything (no strings for UI beyond
  ids/enums); CounterTime.Game maps events to 연출/HUD.
- Tests: Unity Test Framework **EditMode** (plain NUnit), folder
  `Assets/_Project/Core/Tests/` with its own asmdef referencing only
  CounterTime.Core + NUnit.

## File layout
```
Core/
  MatchRulesConfig.cs      // every number from MATCH_RULES, with defaults
  Enums.cs                 // all enums below
  TurnChoice.cs            // input struct
  TechniqueSpec.cs         // technique data (plain class; SO wrapper lives in Game)
  GripSystem.cs
  StaminaSystem.cs
  RefereeSystem.cs
  ScoreBoard.cs
  MatchClock.cs
  JudgementCalculator.cs   // pure static — the formula
  MatchEngine.cs           // facade: ResolveExchange()
  MatchEvent.cs            // event records
  IRandomSource.cs         // + SystemRandomSource, FixedRandomSource (tests)
Core/Tests/
  JudgementCalculatorTests.cs
  StaminaSystemTests.cs
  RefereeSystemTests.cs
  GripSystemTests.cs
  ScoreBoardTests.cs
  OralMatchReplayTests.cs  // full replay of the designer's reference match
```

## Enums (Enums.cs)
```csharp
public enum FighterId { A, B }
public enum Hand { None, Left, Right }
public enum HandTarget { None, RightSleeve, LeftSleeve, LeftLapel, RightLapel, Evade,
                         TwistGrip /*틀어잡기*/, BackGrab /*등싸잡기*/, Tear /*뜯기*/,
                         MaintainGrip /*잡기 유지*/ }
public enum CoreStance { Aggressive, Neutral, Defensive }
public enum LegAction  { Attack, Neutral, Defend, CounterReady }
public enum GripKind   { None, Normal, Dominant }          // value 0 / 1 / 2
public enum GripLock   { Free, Held, Locked }
public enum ReadLevel  { GotRead = -3, Neutral = 0, Read = 2, FullRead = 3 }
public enum TechniqueClass { Sloppy = 0, Standard = 1, Signature = 2 } // judgment term
public enum Judgement  { None, Yuko, WazaAri, Ippon }
public enum RefereeProfile { Strict, Standard, Lenient }
public enum MatchPhase { Standing, Osaekomi, Matte, GoldenScore, Finished }
public enum ExchangeDurationRule { Max }   // Sum/Fixed reserved for later tuning
```

## Config (MatchRulesConfig.cs) — defaults ARE the spec numbers
```csharp
public sealed class MatchRulesConfig {
    // §1 format
    public int RegulationSeconds = 240;
    public int OsaekomiYukoSec = 5, OsaekomiWazaAriSec = 10, OsaekomiIpponSec = 20;
    public int ShidoLimit = 3;
    // §2 time costs
    public int GripTurnSec = 5, ExchangeSec = 3, TearSec = 3;
    // §4 judgment
    public int StaminaBonusTiredThreshold = 50;   // <50 → +2
    public int StaminaBonusExhaustedThreshold = 25; // <25 → +3
    public int NoPointMax = 1, YukoMax = 4, WazaAriMax = 6; // brackets
    public int CounterThreshold = -3;
    public int DefensivePosturePenalty = -2;
    // §5 stamina
    public int BigThrowCost = 5, PokeCost = 1, CounterCost = 2, TearCost = 1;
    public float[] GripMultiplier = { 2.0f, 1.5f, 1.0f, 0.8f, 0.6f }; // index = grip+2
    public int DefendCleanCost = 2, DefendNeutralCost = 4, DefendPushedCost = 6;
    public int ExtremeDefenseDrainPerTurn = 1;
    public int MatteRecovery = 3; public int StallRecoveryPer10s = 2;
    public float MasteryCostReductionPerLevel = 0.08f;
    public float MasteryReadReductionPerLevel = 0.05f;
    // §4.2 counter-ready
    public int CounterReadyReadSteps = 1;   // rungs the ATTACKER's read drops
    // §6.1 ladder is EXCLUSIVE — first matching rung only, never summed.
    public int PassExtremeDefense = 3, PassDoubleDefensive = 2, PassHandEvade = 2,
               PassCounterReady = 2,
               PassAllNeutral = 1, PassAttackEntry = -2, PassRealAttempt = -4;
    public int ThresholdStrict = 9, ThresholdStandard = 12, ThresholdLenient = 16;
    public int ThresholdJitter = 1;            // ±1 at match start
    public int ThresholdEscalation = 2;        // after each shido
    public float DetectStrict = .90f, DetectStandard = .70f, DetectLenient = .45f;
    public int FeintWindowTurns = 2;
    public float TellMinorRatio = .70f, TellMajorRatio = .90f;
    // §2 exchange duration
    public ExchangeDurationRule DurationRule = ExchangeDurationRule.Max;
    // §1 expiry
    public bool ResolveExchangeOnExpiry = true;
}
```

## The formula (JudgementCalculator.cs) — implement EXACTLY
```csharp
public readonly struct JudgementContext {
    public readonly int EffectiveGrip;        // −2..+2 AFTER lock cap (GripSystem supplies)
    public readonly ReadLevel Read;
    public readonly int OpponentStamina;      // 0..100
    public readonly TechniqueClass Tech;
    public readonly bool OpponentDefensivePosture;
    // ctor omitted
}
public readonly struct JudgementResult {
    public readonly Judgement Judgement;
    public readonly FighterId ScoredBy;       // attacker, or opponent when countered
    public readonly bool IsCounter;
    public readonly int Sum;                  // expose for logs/tests
}
public static class JudgementCalculator {
    public static JudgementResult Evaluate(FighterId attacker, in JudgementContext c,
                                           MatchRulesConfig cfg) {
        int stam = c.OpponentStamina < cfg.StaminaBonusExhaustedThreshold ? 3
                 : c.OpponentStamina < cfg.StaminaBonusTiredThreshold     ? 2 : 0;
        int sum = c.EffectiveGrip + (int)c.Read + stam + (int)c.Tech
                + (c.OpponentDefensivePosture ? cfg.DefensivePosturePenalty : 0);
        if (sum <= cfg.CounterThreshold)
            return new JudgementResult(Bracket(-sum, cfg), Opp(attacker), true, sum);
        return new JudgementResult(Bracket(sum, cfg), attacker, false, sum);
    }
    /// §4.2 counter-ready: the ATTACKER's read drops `steps` rungs, floored at
    /// GotRead. The ladder is uneven BY DESIGN — Neutral→GotRead is a 3-point
    /// fall, Read→Neutral is 2, FullRead→Read is only 1. A fully-read attack
    /// cannot be countered by CounterReady alone. Reading is beaten only by reading.
    public static ReadLevel StepDownRead(ReadLevel r, int steps = 1) {
        for (int i = 0; i < steps; i++)
            r = r switch {
                ReadLevel.FullRead => ReadLevel.Read,
                ReadLevel.Read     => ReadLevel.Neutral,
                _                  => ReadLevel.GotRead,   // Neutral and GotRead floor
            };
        return r;
    }
    static Judgement Bracket(int s, MatchRulesConfig cfg) =>
        s <= cfg.NoPointMax ? Judgement.None
      : s <= cfg.YukoMax    ? Judgement.Yuko
      : s <= cfg.WazaAriMax ? Judgement.WazaAri : Judgement.Ippon;
}
```
Note: a counter with |sum| ≤ 1 is `Judgement.None` for the opponent — being read
badly isn't automatically a score against you.

## Subsystem responsibilities (signatures only — implement per MATCH_RULES)

**GripSystem** — owns both fighters' hand connections.
```csharp
int GripScore(FighterId who);                       // raw, from holds (cap ±2)
// EffectiveGrip: AnyHandLocked(who) ? Math.Min(GripScore(who), -1) : GripScore(who)
int EffectiveGrip(FighterId who);
bool AnyHandLocked(FighterId who);
bool CanUseConversion(FighterId who);               // 틀어잡기/등싸잡기 gate (§2)
TearOutcome ResolveTear(FighterId who, Hand hand, bool opponentChoseMaintain);
void OnMatte();                                     // all → Free
```
`ResolveTear`: opponent chose MaintainGrip ⇒ `Locked`; else ⇒ `Free`. No RNG in v1.

**StaminaSystem**
```csharp
int  Get(FighterId who);
void PayAttack(FighterId who, TechniqueSpec spec, int effectiveGrip); // multiplier on big throws only
void PayCounter(FighterId who);   // flat CounterCost, no grip multiplier,
                                  // called only after the counter judgment confirms
void PayDefense(FighterId who, DefenseQuality q);
void DrainExtremeDefense(FighterId who);
void RecoverMatte(FighterId who);
void RecoverStall(FighterId who, int seconds);
bool IsExhaustedLockout(FighterId who);             // ==0 → only defend/poke
```

**RefereeSystem** — hidden accumulators, profile, thresholds, escalation, feint.
```csharp
void ObserveTurn(FighterId who, in TurnChoice choice, bool attackEntry, bool realAttempt);
ShidoEvent? ConsumeShidoIfDue(FighterId who);       // resets acc, threshold += escalation
FeintVerdict JudgeFeint(FighterId who, IRandomSource rng); // uses profile detect rate
RefereeTell CurrentTell(FighterId who);             // None/Minor(70%)/Major(90%)
```
`ObserveTurn` MUST be implemented as an if/else ladder over the exclusive categories
(§6.1), not as a sum of matching flags. The ladder shape is the guarantee — a summing
implementation passes casual inspection and fails `ExtremeDefense_IsThreeNotSeven`.

**ScoreBoard** — 2025 rules. Yuko is a *counter only*; it must be structurally
unable to promote. WazaAri×2 ⇒ Ippon. Shido≥3 ⇒ hansoku-make. `Winner(atTimeUp)`
implements priority ippon > wazaari > yuko > null(→golden). In golden score, any
`Judgement != None` ends the match.

**MatchClock** — advances by action costs; `Matte` freezes; osaekomi real-time;
emits `RegulationExpired` ⇒ engine flips to GoldenScore (or decides winner).
```csharp
void AdvanceExchange(int costA, int costB);  // elapsed += Math.Max(costA, costB)
bool RegulationExpired { get; }              // checked AFTER the exchange resolves
```

**MatchEngine.ResolveExchange(TurnChoice a, TurnChoice b, ExchangeScript script)**
Orchestrates one simultaneous exchange:
1. referee.ObserveTurn ×2 → 2. grip resolution (targets/tear/conversions)
3. technique resolution → JudgementCalculator (ReadLevel comes from `script` in v1;
   the habit-AI replaces it later behind the same parameter)

3.5 **CounterReady adjustment** — after ReadLevel is determined and BEFORE
    JudgementCalculator runs: if the DEFENDER declared CounterReady and the attacker
    committed a big throw, lower the ATTACKER's ReadLevel by `CounterReadyReadSteps`,
    floored at GotRead. A CounterReady fighter cannot have a technique of their own
    resolved this exchange.

4. stamina payments — attacker pays on commit; a countering fighter pays the counter cost
   only AFTER the judgment is confirmed; a countered attacker does NOT get a refund.
5. scoreboard/clock updates → 6. shido/feint checks
7. return `IReadOnlyList<MatchEvent>`.
`ExchangeScript` is the v1 seam: scenario nodes drive reads and outcomes so the
board JSON stays the source of truth until the opponent brain exists.

**MatchEvent** — immutable records, e.g. `GripChanged`, `TearFailedLocked`,
`JudgementScored{sum, terms[]}` (terms exposed for the coach panel to explain),
`ShidoGiven`, `RefereeTellChanged`, `MatteCalled`, `OsaekomiTick`, `MatchEnded`.

## Test plan (write FIRST for the calculator; the rest after wiring)
`JudgementCalculatorTests` — the three canonical vectors from MATCH_RULES §4.1:
```
(+2, Neutral, 100, Standard, defensive)  → sum 1 → None
(−1, Read,    100, Standard, open)       → sum 2 → Yuko
(+2, FullRead,100, Signature, open)      → sum 7 → Ippon
```
plus counter cases: `(−2, GotRead, 100, Standard, open)` → sum −4 → opponent Yuko;
`(0, GotRead, 100, Standard, open)` → sum −2 → None (no counter).
`StaminaSystemTests`: spam sequence 3+1+3+3=10; unfavorable big throw = 8 (5×1.5);
counter always 2 regardless of grip; extreme-defense drain; stamina-0 lockout.
`RefereeSystemTests`: standard ref reaches 12 → shido, threshold 14, acc reset;
tells at 70/90 %; feint detection with `FixedRandomSource`.
`GripSystemTests`: tear vs maintain → Locked; matte clears; lock caps effective grip.
`ScoreBoardTests`: yuko×5 < wazaari; wazaari×2 = ippon; golden-score yuko ends;
shido 3 = loss.
`OralMatchReplayTests`: script the designer's full reference match (sequence list in
`counter-time-match-scenario-v2.json` main line m1→m4→m8→m9→m13→m14→m15→…→m36) and
assert the recorded checkpoints: stamina 90 after phase 1, B shido 1, A yuko at
2:50, A feint-shido at 2:30, final ippon sum 7.

```
JudgementCalculatorTests
  CounterReady_LowersAttackerReadOneStep
  CounterReady_FlooredAtGotRead
  CounterReady_CannotCounterFullRead      // FullRead→Read(+2), sum stays above −3
  (−1, Neutral→StepDown, 100, Standard, open) → sum −3 → counter, Yuko to defender
  ( 0, Neutral→StepDown, 100, Standard, open) → sum −2 → None

RefereeSystemTests
  sustained 극단적 방어 = +3/turn (NOT 7) → standard ref shidos on turn 4
  core Aggressive + legs CounterReady → +2 (was falling through to 0)
  ladder never sums: exactly one rung applies per turn

GripSystemTests
  left hand Locked, right Free, raw grip +2 → effective −1
  either-hand rule symmetric (right Locked gives the same)

MatchClockTests
  grip turn (5) vs technique (3) → exchange advances 5
  exchange begun at 238 s resolves fully and its score counts; elapsed clamps
  to 240 for the time-up decision

StaminaSystemTests
  Counter_PaidByCounteringFighter_AfterConfirm
  Attacker_KeepsBigThrowCost_WhenCountered
```

## Implementation order for Claude Code
1. Enums + Config + JudgementCalculator + its tests (30 min, pure).
2. StaminaSystem + tests. 3. GripSystem + tests. 4. RefereeSystem + tests.
5. ScoreBoard + MatchClock + tests. 6. MatchEngine + ExchangeScript seam +
   OralMatchReplayTests. 7. Update STATUS.md (English) with results.
Commit checkpoint after each numbered step. Do not touch CounterTime.Game,
NodeMotionMap, or the scenario runner in this pass.
