using System.Collections.Generic;

namespace CounterTime.Core
{
    /// The v1 seam. Scenario nodes drive reads and outcomes so the board JSON stays
    /// the source of truth until the opponent brain exists; the habit-AI later
    /// replaces this behind the same parameter.
    public sealed class ExchangeScript
    {
        public FighterId? Attacker;                  // null ⇒ no technique this exchange
        public TechniqueSpec Technique;
        public ReadLevel Read = ReadLevel.Neutral;   // attacker's read of the defender
        // No DefenseQuality here — §5.2 keys it off the read, so it is derived in
        // StaminaSystem.DefenseQualityFor. A script field would be a second source.
        public bool AttackEntryA, AttackEntryB;
        /// §6.1 — 이 턴에 그 선수가 실제로 커밋을 선언했는가. false 면 심판이 관찰하지
        /// 않는다: '전부 중립 +1' 은 중립을 <b>고른</b> 턴에만 붙는 항이고, 시나리오가
        /// 언급조차 안 한 선수는 고른 게 아니라 화면 밖이다. 거기에 +1 을 매기면 없는
        /// 데이터를 만들어내는 것이다.
        public bool DeclaredA = true, DeclaredB = true;
        public int DurationA = 3, DurationB = 3;     // §2 time costs, seconds
        public bool CallMatte;
    }

    public sealed class MatchEngine
    {
        readonly MatchRulesConfig _cfg;
        readonly IRandomSource _rng;
        readonly RefereeTell[] _lastTell = new RefereeTell[2];

        public GripSystem Grip { get; }
        public StaminaSystem Stamina { get; }
        public RefereeSystem Referee { get; }
        public ScoreBoard Board { get; }
        public MatchClock Clock { get; }

        public MatchEngine(MatchRulesConfig cfg, RefereeProfile profile, IRandomSource rng)
        {
            _cfg = cfg;
            _rng = rng;
            Grip = new GripSystem();
            Stamina = new StaminaSystem(cfg);
            Referee = new RefereeSystem(profile, cfg, rng);
            Board = new ScoreBoard(cfg);
            Clock = new MatchClock(cfg);
        }

        public IReadOnlyList<MatchEvent> ResolveExchange(TurnChoice a, TurnChoice b, ExchangeScript script)
        {
            var ev = new List<MatchEvent>();
            if (Board.IsFinished) return ev;

            // §2 counter-ready gate: a fighter who declared it cannot enter a technique
            // this turn — no 기술 진입, no judgment fired by them. Enforced here rather
            // than trusted to the script, so the gate cannot be scripted away.
            bool crA = a.Legs == LegAction.CounterReady;
            bool crB = b.Legs == LegAction.CounterReady;
            FighterId? attacker = script.Attacker;
            if ((attacker == FighterId.A && crA) || (attacker == FighterId.B && crB))
                attacker = null;
            // §4 / §5.1 — a poke is not a throw attempt. To the referee it reads as
            // 기술 진입 (−2), deliberately weaker than a real attempt's −4: probing your
            // way out of a passivity call takes twice as many turns and burns stamina.
            bool poke = attacker.HasValue && script.Technique != null
                        && script.Technique.Cost == ActionCost.Poke;
            bool entryA = (script.AttackEntryA || (poke && attacker == FighterId.A)) && !crA;
            bool entryB = (script.AttackEntryB || (poke && attacker == FighterId.B)) && !crB;
            bool realA = !poke && attacker == FighterId.A;
            bool realB = !poke && attacker == FighterId.B;

            // 1. referee observes both commits — but only the ones actually declared.
            if (script.DeclaredA) Referee.ObserveTurn(FighterId.A, a, entryA, realA);
            if (script.DeclaredB) Referee.ObserveTurn(FighterId.B, b, entryB, realB);

            // 2. grip resolution
            ResolveGrip(FighterId.A, a, b, ev);
            ResolveGrip(FighterId.B, b, a, ev);

            // 3 / 3.5 / 4 / 5 — technique
            if (attacker.HasValue && script.Technique != null)
                ResolveTechnique(attacker.Value, script, a, b, ev);

            // 4 (cont.) §5.2 turtling drain
            if (a.IsExtremeDefense) Stamina.DrainExtremeDefense(FighterId.A);
            if (b.IsExtremeDefense) Stamina.DrainExtremeDefense(FighterId.B);

            // 5. clock — §2 the slower action sets the length
            Clock.AdvanceExchange(script.DurationA, script.DurationB);

            // 6. shido / feint / matte
            CheckReferee(ev);
            if (script.CallMatte) CallMatte(ev);

            if (Board.IsFinished) ev.Add(new MatchEnded(Board.Winner));
            return ev;
        }

        void ResolveGrip(FighterId who, in TurnChoice mine, in TurnChoice theirs, List<MatchEvent> ev)
        {
            switch (mine.Hand)
            {
                case HandTarget.RightSleeve:
                case HandTarget.LeftSleeve:
                case HandTarget.LeftLapel:
                case HandTarget.RightLapel:
                    Grip.Grab(who, mine.OwnHand, SleeveToHand(mine.Hand), GripKind.Normal);
                    ev.Add(new GripChanged(who, mine.OwnHand, mine.Hand, GripKind.Normal));
                    break;

                case HandTarget.TwistGrip:
                case HandTarget.BackGrab:
                    // §2 — conversions need the other hand already holding.
                    if (!Grip.CanUseConversion(who)) break;
                    Grip.Grab(who, mine.OwnHand, Hand.None, GripKind.Dominant);
                    ev.Add(new GripChanged(who, mine.OwnHand, mine.Hand, GripKind.Dominant));
                    break;

                case HandTarget.Tear:
                    // §7 — read the holder's hand first; once the tear resolves the
                    // connection is gone and the HUD would have no way to learn it ended.
                    Hand holderHand = Grip.HolderHandGripping(who, mine.OwnHand);
                    var outcome = Grip.ResolveTear(who, mine.OwnHand,
                        theirs.Hand == HandTarget.MaintainGrip);
                    ev.Add(new TearResolved(who, mine.OwnHand, outcome));
                    if (outcome == TearOutcome.Freed && holderHand != Hand.None)
                        ev.Add(new GripChanged(Opp(who), holderHand, HandTarget.None, GripKind.None));
                    Stamina.PayAttack(who, new TechniqueSpec("tear", ActionCost.Tear,
                        TechniqueClass.Sloppy), Grip.EffectiveGrip(who));
                    break;
            }
        }

        void ResolveTechnique(FighterId attacker, ExchangeScript script,
                              in TurnChoice a, in TurnChoice b, List<MatchEvent> ev)
        {
            FighterId defender = Opp(attacker);

            // §4 — the formula is evaluated "at the moment a throw attempt resolves".
            // A poke is not a throw: §5.1 gives it its own row, and letting it fire a
            // judgment would make it strictly dominate big throws (same yuko bracket at
            // a third of the cost, and 기술급 bottoms out at Sloppy=0 so it cannot be
            // priced down). It costs the poker and nothing else — §5.2's "a technique
            // came at you" means a throw came at you.
            // 위장 공격 is NOT this case: §4 lists sloppy·feint at 0, so a feint is a
            // real attempt that scores nothing, and it keeps firing a judgment.
            if (script.Technique.Cost == ActionCost.Poke)
            {
                Stamina.PayAttack(attacker, script.Technique, Grip.EffectiveGrip(attacker));
                return;
            }

            TurnChoice defenderChoice = attacker == FighterId.A ? b : a;
            bool defenderCounterReady = defenderChoice.Legs == LegAction.CounterReady;

            // 3. ReadLevel comes from the script in v1.
            ReadLevel read = script.Read;

            // 3.5 §4.2 — the formula is attacker-relative and single-term, so a
            // defender's read is expressed as a reduction of the ATTACKER's. The rungs
            // are uneven by design: FullRead only falls to Read, so a fully-read attack
            // cannot be countered by CounterReady alone.
            if (defenderCounterReady && script.Technique.Cost == ActionCost.BigThrow)
            {
                ReadLevel lowered = JudgementCalculator.StepDownRead(read, _cfg.CounterReadyReadSteps);
                if (lowered != read) ev.Add(new CounterReadyApplied(defender, read, lowered));
                read = lowered;
            }

            int grip = Grip.EffectiveGrip(attacker);
            int defenderStamina = Stamina.Get(defender);
            // §4 — derived in exactly one place. Do NOT re-derive inline here; an
            // inline `Core == Defensive` is what produced the v3.1 counter-ready bug.
            bool defensive = JudgementCalculator.IsDefensivePosture(defenderChoice);
            var ctx = new JudgementContext(grip, read, defenderStamina, script.Technique.Class, defensive);
            var result = JudgementCalculator.Evaluate(attacker, ctx, _cfg);

            // 4. stamina — attacker pays for the commit …
            Stamina.PayAttack(attacker, script.Technique, grip);
            Stamina.PayDefense(defender, StaminaSystem.DefenseQualityFor(read));
            // … and the COUNTERING fighter pays the flat counter cost only once the
            // judgment is confirmed. The countered attacker gets no refund (§4).
            if (result.IsCounter) Stamina.PayCounter(result.ScoredBy);

            int stamTerm = defenderStamina < _cfg.StaminaBonusExhaustedThreshold ? 3
                         : defenderStamina < _cfg.StaminaBonusTiredThreshold ? 2 : 0;
            var terms = new[] { grip, (int)read, stamTerm, (int)script.Technique.Class,
                                defensive ? _cfg.DefensivePosturePenalty : 0 };

            // 5. scoreboard
            Board.Award(result.ScoredBy, result.Judgement);
            ev.Add(new JudgementScored(result.ScoredBy, result.Judgement, result.Sum,
                                       result.IsCounter, terms));
        }

        void CheckReferee(List<MatchEvent> ev)
        {
            foreach (var who in new[] { FighterId.A, FighterId.B })
            {
                // §6.3 — a detected feint is an immediate shido.
                if (Referee.JudgeFeint(who, _rng) == FeintVerdict.Detected)
                {
                    Board.AddShido(who);
                    ev.Add(new ShidoGiven(who, Referee.ShidoCount(who), true));
                    CallMatte(ev);
                    continue;
                }

                // §6.2 — threshold reached ⇒ matte ⇒ shido.
                var shido = Referee.ConsumeShidoIfDue(who);
                if (shido.HasValue)
                {
                    Board.AddShido(who);
                    ev.Add(new ShidoGiven(who, shido.Value.Count, false));
                    CallMatte(ev);
                }

                // §6.4 — tells, emitted only on change.
                var tell = Referee.CurrentTell(who);
                if (tell == _lastTell[(int)who]) continue;
                _lastTell[(int)who] = tell;
                ev.Add(new RefereeTellChanged(who, tell));
            }
        }

        /// §3.2 / §5.3 — matte frees every grip and gives each fighter one recovery.
        void CallMatte(List<MatchEvent> ev)
        {
            Grip.OnMatte();
            Stamina.RecoverMatte(FighterId.A);
            Stamina.RecoverMatte(FighterId.B);
            ev.Add(new MatteCalled());
        }

        static Hand SleeveToHand(HandTarget t) => t switch
        {
            HandTarget.RightSleeve => Hand.Right,
            HandTarget.LeftSleeve => Hand.Left,
            _ => Hand.None,     // lapels are not a hand, so they carry no lock state
        };

        static FighterId Opp(FighterId f) => f == FighterId.A ? FighterId.B : FighterId.A;
    }
}
