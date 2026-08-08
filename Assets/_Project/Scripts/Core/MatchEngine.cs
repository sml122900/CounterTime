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
        public DefenseQuality DefenseQuality = DefenseQuality.Neutral;
        public bool AttackEntryA, AttackEntryB;
        public int DurationA = 3, DurationB = 3;     // §2 time costs, seconds
        public bool CallMatte;
    }

    public sealed class MatchEngine
    {
        static readonly TechniqueSpec CounterAction =
            new TechniqueSpec("counter", ActionCost.Counter, TechniqueClass.Standard);

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

            // 1. referee observes both commits
            Referee.ObserveTurn(FighterId.A, a, script.AttackEntryA, script.Attacker == FighterId.A);
            Referee.ObserveTurn(FighterId.B, b, script.AttackEntryB, script.Attacker == FighterId.B);

            // 2. grip resolution
            ResolveGrip(FighterId.A, a, b, ev);
            ResolveGrip(FighterId.B, b, a, ev);

            // 3 / 3.5 / 4 / 5 — technique
            if (script.Attacker.HasValue && script.Technique != null)
                ResolveTechnique(script, a, b, ev);

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
                    var outcome = Grip.ResolveTear(who, mine.OwnHand,
                        theirs.Hand == HandTarget.MaintainGrip);
                    ev.Add(new TearResolved(who, mine.OwnHand, outcome));
                    Stamina.PayAttack(who, new TechniqueSpec("tear", ActionCost.Tear,
                        TechniqueClass.Sloppy), Grip.EffectiveGrip(who));
                    break;
            }
        }

        void ResolveTechnique(ExchangeScript script, in TurnChoice a, in TurnChoice b, List<MatchEvent> ev)
        {
            FighterId attacker = script.Attacker.Value;
            FighterId defender = Opp(attacker);
            TurnChoice defenderChoice = attacker == FighterId.A ? b : a;

            // 3. ReadLevel comes from the script in v1.
            ReadLevel read = script.Read;

            // 3.5 CounterReady — a defender who declared it and is facing a big throw
            // reads the attack better, which shows up as the ATTACKER's Reading term
            // stepping down (the formula only ever evaluates the attacker's sum).
            if (defenderChoice.Legs == LegAction.CounterReady
                && script.Technique.Cost == ActionCost.BigThrow)
            {
                ReadLevel lowered = JudgementCalculator.MirrorCounterReady(read, _cfg.CounterReadyReadSteps);
                if (lowered != read) ev.Add(new CounterReadyRaised(defender, read, lowered));
                read = lowered;
            }

            int grip = Grip.EffectiveGrip(attacker);
            int defenderStamina = Stamina.Get(defender);
            bool defensive = defenderChoice.Core == CoreStance.Defensive;
            var ctx = new JudgementContext(grip, read, defenderStamina, script.Technique.Class, defensive);
            var result = JudgementCalculator.Evaluate(attacker, ctx, _cfg);

            // 4. stamina — attacker pays for the commit …
            Stamina.PayAttack(attacker, script.Technique, grip);
            Stamina.PayDefense(defender, script.DefenseQuality);
            // … and the COUNTERING fighter pays its own cost only after the judgment
            // is confirmed. The countered attacker gets no refund (patch 1.3).
            if (result.IsCounter)
                Stamina.PayAttack(result.ScoredBy, CounterAction, Grip.EffectiveGrip(result.ScoredBy));

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
