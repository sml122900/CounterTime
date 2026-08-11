using System;
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

    /// §9.2 — hold state. `Adv`/`EscapesNeeded` are frozen at entry (§9.2: "Nothing
    /// recomputes it mid-hold") and only <see cref="MatchEngine.ResolveOsaekomiExchange"/>
    /// mutates the running counters.
    public sealed class OsaekomiState
    {
        public bool Active { get; internal set; }
        public FighterId Holder { get; internal set; }
        public int Adv { get; internal set; }
        public int EscapesNeeded { get; internal set; }
        public int EscapesSoFar { get; internal set; }
        public int HeldSeconds { get; internal set; }
        internal bool YukoAwarded, WazaAriAwarded;
    }

    public sealed class MatchEngine
    {
        readonly MatchRulesConfig _cfg;
        readonly IRandomSource _rng;
        readonly RefereeTell[] _lastTell = new RefereeTell[2];
        /// §9.1 (v3.6) — the osaekomi gate's "체력 <50" reads the SAME snapshot as
        /// §4's stamina bonus term: pre-payment, captured once in ResolveTechnique
        /// before Stamina.PayAttack/PayDefense/PayCounter run. Set for whichever
        /// fighter ends up as Opp(scorer) — that differs from `defender` when the
        /// score is a counter. Only ResolveTechnique writes it, only TryEnterOsaekomi
        /// (called later in the same ResolveExchange, never across exchanges — see
        /// that method's comment) reads it.
        int _scorerOpponentStaminaPrePayment;

        /// §4.4 [초안] — 흔들기(setup) state carried INTO this exchange (set by 흔들기
        /// declared in the PREVIOUS exchange). Indexed by (int)FighterId.
        readonly bool[] _setupActive = new bool[2];
        /// §4.4 [초안] — 흔들기 declared DURING the exchange currently resolving;
        /// becomes next exchange's `_setupActive` unless 그쳐/굳히기 진입/경기 종료
        /// wipes it first. Scratch state, valid only within one ResolveExchange call.
        readonly bool[] _setupDeclaredThisExchange = new bool[2];

        public GripSystem Grip { get; }
        public StaminaSystem Stamina { get; }
        public RefereeSystem Referee { get; }
        public ScoreBoard Board { get; }
        public MatchClock Clock { get; }
        public OsaekomiState Osaekomi { get; } = new OsaekomiState();

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

        /// §4.4 [초안] — presentation read of whether `who`'s 흔들기 carries into
        /// the exchange about to resolve. ResolveTechnique reads `_setupActive`
        /// directly for judgment purposes; this is the read-only mirror for HUDs.
        public bool IsSetupActive(FighterId who) => _setupActive[(int)who];

        public IReadOnlyList<MatchEvent> ResolveExchange(TurnChoice a, TurnChoice b, ExchangeScript script)
        {
            // §9 — a standing exchange cannot land while a hold is open. Left unguarded,
            // this method would re-evaluate matte-vs-osaekomi as if nothing were open
            // (a known gap, not a design choice — stage 6 wires the real osaekomi input
            // path via ResolveOsaekomiExchange; until then, calling this here is a bug).
            if (Clock.Phase == MatchPhase.Osaekomi)
                throw new InvalidOperationException(
                    "ResolveExchange 는 굳히기 진행 중(MatchPhase.Osaekomi)에는 호출할 수 없다 — " +
                    "ResolveOsaekomiExchange 를 써야 한다.");

            var ev = new List<MatchEvent>();
            if (Board.IsFinished) return ev;

            _setupDeclaredThisExchange[0] = _setupDeclaredThisExchange[1] = false;

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

            // §4.3 (v3.4) — a score resets the situation. Recovery was tied to matte
            // while nothing produced matte, so §5.3's +3 was unreachable and stamina
            // bottomed out around 30 exchanges against the 60–80 §2 assumes.
            //
            // A score that ended the match calls none — there is nothing to restart.
            // Nor do we call a second one when the referee already did this exchange.
            //
            // §9.1 osaekomi entry gate — automatic, never a player choice. Two rows of
            // that table need no extra code here: "Ippon → n/a, match ends" falls out
            // of the !Board.IsFinished guard for free (Board.Award already set it), and
            // "None, or counter with |sum|≤1 → never" is already excluded by
            // LastScoredJudgement filtering on Judgement != None (and with the default
            // CounterThreshold=-3 / NoPointMax=1 a counter can never land at |sum|≤1 in
            // the first place).
            if (!Board.IsFinished && !MatteAlreadyCalled(ev))
            {
                var scored = LastScoredJudgement(ev);
                if (scored != null && !TryEnterOsaekomi(scored, ev))
                    CallMatte(ev);
            }

            // §1 — time-up is checked AFTER the exchange fully resolves (judgment,
            // score, stamina, shido — all already applied above). Gated on
            // !Osaekomi.Active: this method already throws at entry if a hold from
            // an EARLIER exchange were still open, so Active here can only mean
            // THIS exchange just opened one — MATCH_RULES does not say whether a
            // hold in progress should keep running past regulation or be cut off,
            // so that case is deliberately left untouched (unlike a plain standing
            // exchange, which §1 already says plays out in full past 4:00).
            // Once golden score starts, Clock.Phase stays GoldenScore forever, so
            // this block only ever fires once.
            if (!Board.IsFinished && !Osaekomi.Active && Clock.Phase != MatchPhase.GoldenScore
                && Clock.RegulationExpired)
            {
                Board.FinishAtTimeUp();
                if (!Board.IsFinished)
                {
                    Clock.Phase = MatchPhase.GoldenScore;
                    ev.Add(new GoldenScoreEntered());
                }
            }

            // §4.4 [초안] — 그쳐·굳히기 진입·경기 종료가 이 교환에서 일어났으면 흔들기
            // 상태는 국면 리셋과 함께 소멸한다. 그 외에는 이번 교환에 새로 선언된 쪽만
            // 다음 교환으로 넘어간다(누적이 아니라 교체 — 1교환만 지속).
            bool setupReset = MatteAlreadyCalled(ev) || Osaekomi.Active || Board.IsFinished;
            _setupActive[0] = !setupReset && _setupDeclaredThisExchange[0];
            _setupActive[1] = !setupReset && _setupDeclaredThisExchange[1];

            if (Board.IsFinished) ev.Add(new MatchEnded(Board.Winner));
            return ev;
        }

        /// §9.1 entry gate. Waza-ari always enters. Yuko enters only if the scorer's
        /// effective grip ≥ +1 AND the thrown fighter's stamina < 50 — otherwise §4.3
        /// matte applies as before.
        ///
        /// "Attacker" in §9.1/§9.2 is read here as the fighter who just scored
        /// (`j.ScoredBy`), not the original technique declarer (`script.Attacker`
        /// elsewhere in this file) — physically, the scorer is who ends up on top to
        /// hold, which for a counter is the ORIGINAL DEFENDER. §9.1 (v3.5) makes this
        /// explicit and applies it uniformly to countered scores: a countered waza-ari
        /// still always enters, and a countered yuko now uses the SAME gate as an
        /// uncountered one — the scorer's grip and the thrown fighter's stamina,
        /// `j.IsCounter` no longer excludes it.
        ///
        /// §9.1 (v3.6) — "체력 <50" reads the pre-payment snapshot §4 already used
        /// for this exchange's judgment, not a fresh (post-payment) read taken here.
        /// Before v3.6 this method called `Stamina.Get(Opp(j.ScoredBy))` directly,
        /// which runs AFTER ResolveTechnique's PayAttack/PayDefense/PayCounter have
        /// already spent stamina for this same exchange — a different, lower number
        /// than the one that decided the judgment a few lines above. One exchange,
        /// one stamina snapshot now: `_scorerOpponentStaminaPrePayment`.
        bool TryEnterOsaekomi(JudgementScored j, List<MatchEvent> ev)
        {
            if (j.Judgement == Judgement.WazaAri)
            {
                EnterOsaekomi(j.ScoredBy, wazaAriEntry: true,
                    Grip.EffectiveGrip(j.ScoredBy), _scorerOpponentStaminaPrePayment, ev);
                return true;
            }
            if (j.Judgement == Judgement.Yuko)
            {
                int grip = Grip.EffectiveGrip(j.ScoredBy);
                int opponentStamina = _scorerOpponentStaminaPrePayment;
                if (grip >= 1 && opponentStamina < 50)
                {
                    EnterOsaekomi(j.ScoredBy, wazaAriEntry: false, grip, opponentStamina, ev);
                    return true;
                }
            }
            return false;
        }

        /// §9.2 — computed once, frozen at entry.
        void EnterOsaekomi(FighterId holder, bool wazaAriEntry, int attackerGrip,
                           int defenderStamina, List<MatchEvent> ev)
        {
            int adv = (wazaAriEntry ? 2 : 0) + attackerGrip
                    + (defenderStamina < 25 ? 2 : defenderStamina < 50 ? 1 : 0);
            int escapesNeeded = System.Math.Clamp(1 + FloorDiv2(adv), 1, 3);

            Osaekomi.Active = true;
            Osaekomi.Holder = holder;
            Osaekomi.Adv = adv;
            Osaekomi.EscapesNeeded = escapesNeeded;
            Osaekomi.EscapesSoFar = 0;
            Osaekomi.HeldSeconds = 0;
            Osaekomi.YukoAwarded = false;
            Osaekomi.WazaAriAwarded = false;
            Clock.Phase = MatchPhase.Osaekomi;
            ev.Add(new OsaekomiEntered(holder, adv, escapesNeeded));
        }

        static int FloorDiv2(int v) => (int)System.Math.Floor(v / 2.0);

        /// §9.3/§9.4 — one escape exchange, 5 s of real time. Score is awarded FIRST at
        /// each 5/10/20 s mark, the escape resolves after (§9.3's explicit ordering).
        /// No-op (empty list) if osaekomi is not active or the match already ended.
        public IReadOnlyList<MatchEvent> ResolveOsaekomiExchange(
            OsaekomiDefenderChoice defenderChoice, OsaekomiHolderChoice holderChoice)
        {
            var ev = new List<MatchEvent>();
            if (!Osaekomi.Active || Board.IsFinished) return ev;

            FighterId holder = Osaekomi.Holder;
            FighterId defender = Opp(holder);

            Clock.AdvanceOsaekomi(_cfg.OsaekomiEscapeIntervalSec);
            Osaekomi.HeldSeconds += _cfg.OsaekomiEscapeIntervalSec;

            if (!Osaekomi.YukoAwarded && Osaekomi.HeldSeconds >= _cfg.OsaekomiYukoSec)
            {
                Osaekomi.YukoAwarded = true;
                Board.Award(holder, Judgement.Yuko);
                ev.Add(new OsaekomiScoreAwarded(holder, Judgement.Yuko, Osaekomi.HeldSeconds));
            }
            if (!Board.IsFinished && !Osaekomi.WazaAriAwarded && Osaekomi.HeldSeconds >= _cfg.OsaekomiWazaAriSec)
            {
                Osaekomi.WazaAriAwarded = true;
                Board.Award(holder, Judgement.WazaAri);
                ev.Add(new OsaekomiScoreAwarded(holder, Judgement.WazaAri, Osaekomi.HeldSeconds));
            }
            if (!Board.IsFinished && Osaekomi.HeldSeconds >= _cfg.OsaekomiIpponSec)
            {
                Board.Award(holder, Judgement.Ippon);
                ev.Add(new OsaekomiScoreAwarded(holder, Judgement.Ippon, Osaekomi.HeldSeconds));
            }

            if (Board.IsFinished)
            {
                Osaekomi.Active = false;
                ev.Add(new MatchEnded(Board.Winner));
                return ev;
            }

            // §9.3 — 맞물림 (뒤집기×따라가기, 빠져나가기×눌러붙기): hold continues, the
            // defender pays for staying under control. 어긋남 (the cross pairs): one
            // escape credited.
            bool matched = (defenderChoice == OsaekomiDefenderChoice.Reversal && holderChoice == OsaekomiHolderChoice.Follow)
                        || (defenderChoice == OsaekomiDefenderChoice.Escape && holderChoice == OsaekomiHolderChoice.PressIn);
            if (matched)
                Stamina.PayOsaekomiStay(defender);
            else
                Osaekomi.EscapesSoFar++;

            // §5.3 unchanged — holder recovery is +2 per completed 10s stalled. Held
            // time advances in fixed 5s ticks, so this fires on every other exchange.
            if (Osaekomi.HeldSeconds % 10 == 0)
                Stamina.RecoverStall(holder, 10);

            ev.Add(new OsaekomiEscapeResolved(defender, defenderChoice, holderChoice,
                                              !matched, Osaekomi.EscapesSoFar));

            if (Osaekomi.EscapesSoFar >= Osaekomi.EscapesNeeded)
            {
                // §9.4 — standing restart WITHOUT matte: grips reset to Free, but no
                // matte stamina recovery. Stance reset (mirrors CallMatte) is the
                // runner's job once stage 6 wires ResolveOsaekomiExchange in — scripted
                // mode never reaches this method today.
                Grip.OnMatte();
                Osaekomi.Active = false;
                Clock.Phase = MatchPhase.Standing;
                ev.Add(new OsaekomiEnded(defender));
            }

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

                case HandTarget.Setup:
                    // §4.4 [초안] — inert unless the draft flag is on; when on, needs a
                    // grip to lean on and is otherwise a no-op (mirrors the conversion
                    // gate above: CanUseConversion/HasAnyGrip are the same predicate,
                    // named for what each caller is asking).
                    if (!_cfg.SetupGateEnabled || !Grip.HasAnyGrip(who)) break;
                    Stamina.PaySetup(who);
                    _setupDeclaredThisExchange[(int)who] = true;
                    ev.Add(new SetupDeclared(who));
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
            int attackerStamina = Stamina.Get(attacker);
            // §4 — derived in exactly one place. Do NOT re-derive inline here; an
            // inline `Core == Defensive` is what produced the v3.1 counter-ready bug.
            bool defensive = JudgementCalculator.IsDefensivePosture(defenderChoice);
            // §4.4 [초안] — a BigThrow thrown without an active 흔들기(setup) enters the
            // formula as Sloppy. JudgementCalculator itself is untouched; the downgrade
            // is a context decision made here, same boundary as everything else that
            // feeds JudgementContext.
            TechniqueClass techClass = script.Technique.Class;
            if (_cfg.SetupGateEnabled && script.Technique.Cost == ActionCost.BigThrow
                && !script.Technique.SurpriseExempt && !_setupActive[(int)attacker])
                techClass = TechniqueClass.Sloppy;
            var ctx = new JudgementContext(grip, read, defenderStamina, techClass, defensive);
            var result = JudgementCalculator.Evaluate(attacker, ctx, _cfg);

            // §9.1 (v3.6) — same pre-payment snapshot §4 just used above, just
            // re-pointed at whichever fighter Opp(scorer) turns out to be (that's
            // `defenderStamina` unless this is a counter, in which case the scorer
            // is `defender` and Opp(scorer) is `attacker`).
            _scorerOpponentStaminaPrePayment = result.ScoredBy == attacker ? defenderStamina : attackerStamina;

            // 4. stamina — attacker pays for the commit …
            Stamina.PayAttack(attacker, script.Technique, grip);
            Stamina.PayDefense(defender, StaminaSystem.DefenseQualityFor(read));
            // … and the COUNTERING fighter pays the flat counter cost only once the
            // judgment is confirmed. The countered attacker gets no refund (§4).
            if (result.IsCounter) Stamina.PayCounter(result.ScoredBy);

            int stamTerm = defenderStamina < _cfg.StaminaBonusExhaustedThreshold ? 3
                         : defenderStamina < _cfg.StaminaBonusTiredThreshold ? 2 : 0;
            var terms = new[] { grip, (int)read, stamTerm, (int)techClass,
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

        /// §4.3 — 노포인트는 상황을 리셋하지 않는다. 실제로 점수가 난 교환만이다.
        /// §9.1 also keys off this exact judgement (Ippon excluded implicitly since
        /// Board.IsFinished is already true by the time the caller checks it).
        static JudgementScored LastScoredJudgement(List<MatchEvent> ev)
        {
            foreach (var e in ev)
                if (e is JudgementScored j && j.Judgement != Judgement.None) return j;
            return null;
        }

        static bool MatteAlreadyCalled(List<MatchEvent> ev)
        {
            foreach (var e in ev)
                if (e is MatteCalled) return true;
            return false;
        }

        /// §3.2 / §5.3 — matte frees every grip and gives each fighter one recovery.
        /// 자세 초기화는 여기서 안 한다 — Core 는 자세를 들지 않는다. 커밋을 유지하는
        /// 쪽(러너)이 MatteCalled 를 받아 푼다.
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
