using System.Linq;
using NUnit.Framework;
using CounterTime.Core;

namespace CounterTime.Core.Tests
{
    /// Replays the main line of counter-time-match-scenario-v2.json
    /// (m1 → m4 → m8 → m9 → m13 → m14 → m15 → … → m36) and asserts the checkpoints
    /// recorded on those nodes.
    public class OralMatchReplayTests
    {
        MatchRulesConfig _cfg;
        MatchEngine _e;

        [SetUp]
        public void SetUp()
        {
            _cfg = new MatchRulesConfig();
            // jitter 0 so the standard threshold is exactly 12, as node m15 records.
            _e = new MatchEngine(_cfg, RefereeProfile.Standard, new FixedRandomSource(f: 0.99f, i: 0));
        }

        static TechniqueSpec BigThrow(string id, TechniqueClass c = TechniqueClass.Standard) =>
            new TechniqueSpec(id, ActionCost.BigThrow, c);
        static TechniqueSpec Poke(string id) =>
            new TechniqueSpec(id, ActionCost.Poke, TechniqueClass.Standard);

        static readonly TurnChoice Idle =
            new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.Neutral);
        static readonly TurnChoice Turtle =
            new TurnChoice(HandTarget.Evade, CoreStance.Defensive, LegAction.Defend);

        /// m1 → m4: A takes B's right sleeve. m4 records 잡기 +1.
        void SleeveControl()
        {
            _e.ResolveExchange(
                new TurnChoice(HandTarget.RightSleeve, CoreStance.Neutral, LegAction.Neutral, Hand.Left),
                Idle,
                new ExchangeScript { DurationA = _cfg.GripTurnSec, DurationB = _cfg.GripTurnSec });
        }

        /// m8 → m9: right hand converts to 틀어잡기. m9 records 잡기 +2.
        void TwistGrip()
        {
            _e.ResolveExchange(
                new TurnChoice(HandTarget.TwistGrip, CoreStance.Aggressive, LegAction.Neutral, Hand.Right),
                Idle,
                new ExchangeScript { DurationA = _cfg.GripTurnSec, DurationB = _cfg.GripTurnSec });
        }

        [Test]
        public void M4_SleeveControl_GivesGripPlusOne()
        {
            SleeveControl();
            Assert.AreEqual(1, _e.Grip.GripScore(FighterId.A));
            Assert.AreEqual(GripLock.Held, _e.Grip.LockState(FighterId.B, Hand.Right),
                "B's right hand is dead — m4");
        }

        [Test]
        public void M9_TwistGrip_GivesGripPlusTwo_AndCheapensBigThrows()
        {
            SleeveControl();
            TwistGrip();
            Assert.AreEqual(2, _e.Grip.GripScore(FighterId.A), "m9: 잡기 +2 지배 국면");

            int before = _e.Stamina.Get(FighterId.A);
            _e.Stamina.PayAttack(FighterId.A, BigThrow("batadari"), 2);
            Assert.AreEqual(3, before - _e.Stamina.Get(FighterId.A), "×0.6 at grip +2");
        }

        /// m13 → m14: B sits behind a defensive shell, A spams four attacks.
        /// m14 records −10 stamina and a judgment sum of 1 (no point).
        ///
        /// B defends without going all-in here so the accumulator stays under the
        /// threshold: a full 극단적 방어 would draw the m15 shido on the fourth turn,
        /// and the matte that follows grants §5.3's +3, which is a separate event.
        /// See M15_ShidoMatteRecovery_ExplainsTheNinetyThreeSnapshot.
        [Test]
        public void M14_AttackSpamOnDefensiveOpponent_CostsTenAndScoresNothing()
        {
            SleeveControl();
            TwistGrip();
            Assert.AreEqual(100, _e.Stamina.Get(FighterId.A));

            var shell = new TurnChoice(HandTarget.None, CoreStance.Defensive, LegAction.Defend);
            var attacks = new[] { BigThrow("batadari"), Poke("balmok-batchigi"),
                                  BigThrow("batadari"), BigThrow("harai-goshi") };
            JudgementScored last = null;
            foreach (var t in attacks)
            {
                var ev = _e.ResolveExchange(Idle, shell, new ExchangeScript
                {
                    Attacker = FighterId.A,
                    Technique = t,
                    Read = ReadLevel.Neutral,
                });
                last = ev.OfType<JudgementScored>().LastOrDefault() ?? last;
            }

            Assert.AreEqual(90, _e.Stamina.Get(FighterId.A), "m14: 3+1+3+3 = 10");
            Assert.AreEqual(1, last.Sum, "m14: 잡기+2 +수싸움0 +체력0 +기술+1 +상대수비-2 = 1");
            Assert.AreEqual(Judgement.None, last.Judgement, "굳힌 상대에겐 점수가 안 난다");
        }

        /// The m15 snapshot records A90, but that is the value BEFORE the matte that
        /// the shido triggers. §5.3 grants +3 on matte, so the engine lands on 93.
        /// MATCH_RULES wins over the board JSON, so 93 is correct and the snapshot is
        /// a pre-recovery reading.
        [Test]
        public void M15_ShidoMatteRecovery_ExplainsTheNinetyThreeSnapshot()
        {
            SleeveControl();
            TwistGrip();

            var attacks = new[] { BigThrow("batadari"), Poke("balmok-batchigi"),
                                  BigThrow("batadari"), BigThrow("harai-goshi") };
            foreach (var t in attacks)
                _e.ResolveExchange(Idle, Turtle, new ExchangeScript
                {
                    Attacker = FighterId.A,
                    Technique = t,
                    Read = ReadLevel.Neutral,
                });

            Assert.AreEqual(1, _e.Board.Shido(FighterId.B), "fourth turtled turn hits 12");
            Assert.AreEqual(93, _e.Stamina.Get(FighterId.A),
                "100 − 10 spam + 3 matte recovery");
        }

        /// m15: four turtled turns reach the standard threshold of 12 ⇒ B shido 1,
        /// accumulator resets, threshold rises to 14.
        [Test]
        public void M15_TurtlingDrawsShidoOne_AndEscalatesThreshold()
        {
            for (int i = 0; i < 4; i++)
                _e.ResolveExchange(Idle, Turtle, new ExchangeScript());

            Assert.AreEqual(1, _e.Board.Shido(FighterId.B), "m15: B 소극 지도 1");
            Assert.AreEqual(14, _e.Referee.Threshold(FighterId.B), "임계 14로 상승");
            Assert.AreEqual(0, _e.Board.Shido(FighterId.A));
        }

        [Test]
        public void M15_ShidoCallsMatte_WhichFreesEveryGrip()
        {
            SleeveControl();
            TwistGrip();
            Assert.AreEqual(2, _e.Grip.GripScore(FighterId.A));

            for (int i = 0; i < 4; i++)
                _e.ResolveExchange(Idle, Turtle, new ExchangeScript());

            Assert.AreEqual(0, _e.Grip.GripScore(FighterId.A), "matte resets grips to Free");
        }

        /// The 2:50 checkpoint: A scores a yuko from an unfavourable grip.
        /// [판정식] 잡기-1 +수싸움+2 +체력0 +기술+1 = 2 → 유효.
        [Test]
        public void Yuko_AtUnfavourableGrip_SumIsTwo()
        {
            _e.ResolveExchange(
                new TurnChoice(HandTarget.None, CoreStance.Aggressive, LegAction.Neutral),
                new TurnChoice(HandTarget.RightSleeve, CoreStance.Neutral, LegAction.Neutral, Hand.Left),
                new ExchangeScript { DurationA = _cfg.GripTurnSec, DurationB = _cfg.GripTurnSec });
            Assert.AreEqual(-1, _e.Grip.EffectiveGrip(FighterId.A));

            var ev = _e.ResolveExchange(Idle, Idle, new ExchangeScript
            {
                Attacker = FighterId.A,
                Technique = BigThrow("bit-danggyeo-chigi"),
                Read = ReadLevel.Read,
            });

            var scored = ev.OfType<JudgementScored>().Single();
            Assert.AreEqual(2, scored.Sum);
            Assert.AreEqual(Judgement.Yuko, scored.Judgement);
            Assert.AreEqual(1, _e.Board.Yuko(FighterId.A), "A 유효1");
        }

        /// The 2:30 checkpoint: A attacks then retreats inside the feint window and
        /// a standard referee detects it ⇒ shido to A.
        [Test]
        public void FeintShido_IsGivenToTheAttacker()
        {
            var detecting = new MatchEngine(_cfg, RefereeProfile.Standard,
                                            new FixedRandomSource(f: 0.1f, i: 0));  // < .70
            detecting.ResolveExchange(
                new TurnChoice(HandTarget.None, CoreStance.Aggressive, LegAction.Attack),
                Idle,
                new ExchangeScript { Attacker = FighterId.A, Technique = BigThrow("feint") });

            var ev = detecting.ResolveExchange(Turtle, Idle, new ExchangeScript());

            var shido = ev.OfType<ShidoGiven>().Single();
            Assert.AreEqual(FighterId.A, shido.Who);
            Assert.IsTrue(shido.FromFeint);
            Assert.AreEqual(1, detecting.Board.Shido(FighterId.A));
        }

        /// m36: [판정식] 잡기+2 +완전간파+3 +체력0 +시그니처+2 = 7 → 한판. Match over.
        [Test]
        public void M36_SignatureOnAFullRead_IsIpponAndEndsTheMatch()
        {
            SleeveControl();
            TwistGrip();

            var ev = _e.ResolveExchange(Idle, Idle, new ExchangeScript
            {
                Attacker = FighterId.A,
                Technique = BigThrow("tsurikomi-ashi-barai", TechniqueClass.Signature),
                Read = ReadLevel.FullRead,
            });

            var scored = ev.OfType<JudgementScored>().Single();
            Assert.AreEqual(7, scored.Sum, "m36: 2 + 3 + 0 + 2 = 7");
            Assert.AreEqual(Judgement.Ippon, scored.Judgement);
            Assert.IsTrue(_e.Board.IsFinished);
            Assert.AreEqual(FighterId.A, _e.Board.Winner);
            Assert.IsNotNull(ev.OfType<MatchEnded>().SingleOrDefault());
        }

        [Test]
        public void M36_IpponNeedsNoStaminaBonus()
        {
            SleeveControl();
            TwistGrip();
            Assert.AreEqual(100, _e.Stamina.Get(FighterId.B), "B is still fresh");

            var ev = _e.ResolveExchange(Idle, Idle, new ExchangeScript
            {
                Attacker = FighterId.A,
                Technique = BigThrow("sig", TechniqueClass.Signature),
                Read = ReadLevel.FullRead,
            });
            var terms = ev.OfType<JudgementScored>().Single().Terms;
            Assert.AreEqual(0, terms[2], "stamina term contributes nothing — '읽어서' 만드는 것");
        }

        // ---- §4.2 wiring, per the resolved ambiguity ----

        [Test]
        public void CounterReady_LowersAttackerRead_AndCanFlipIntoACounter()
        {
            // A at grip −1 attacks into a declared counter: −1 + (−3) + 1 = −3 ⇒ counter.
            _e.ResolveExchange(
                Idle,
                new TurnChoice(HandTarget.RightSleeve, CoreStance.Neutral, LegAction.Neutral, Hand.Left),
                new ExchangeScript { DurationA = _cfg.GripTurnSec, DurationB = _cfg.GripTurnSec });
            Assert.AreEqual(-1, _e.Grip.EffectiveGrip(FighterId.A));

            var ev = _e.ResolveExchange(
                Idle,
                new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.CounterReady),
                new ExchangeScript
                {
                    Attacker = FighterId.A,
                    Technique = BigThrow("batadari"),
                    Read = ReadLevel.Neutral,
                });

            Assert.IsNotNull(ev.OfType<CounterReadyApplied>().SingleOrDefault(),
                "the declaration bit");
            var scored = ev.OfType<JudgementScored>().Single();
            Assert.AreEqual(-3, scored.Sum);
            Assert.IsTrue(scored.IsCounter);
            Assert.AreEqual(FighterId.B, scored.ScoredBy);
        }

        /// §2 gate — a counter-ready fighter cannot have a technique of their own
        /// resolved, even if the script names them as the attacker.
        [Test]
        public void CounterReady_GatesTheDeclarersOwnTechnique()
        {
            var ev = _e.ResolveExchange(
                new TurnChoice(HandTarget.None, CoreStance.Aggressive, LegAction.CounterReady),
                Idle,
                new ExchangeScript
                {
                    Attacker = FighterId.A,
                    Technique = BigThrow("batadari"),
                    Read = ReadLevel.FullRead,
                    AttackEntryA = true,
                });

            Assert.IsEmpty(ev.OfType<JudgementScored>(),
                "the declarer cannot fire a judgment this exchange");
            Assert.AreEqual(100, _e.Stamina.Get(FighterId.A), "and pays nothing for it");
        }

        /// §4.2 — a fully-read attack bottoms out at Read and survives counter-ready.
        [Test]
        public void CounterReady_CannotCounterAFullRead()
        {
            _e.ResolveExchange(
                Idle,
                new TurnChoice(HandTarget.RightSleeve, CoreStance.Neutral, LegAction.Neutral, Hand.Left),
                new ExchangeScript { DurationA = _cfg.GripTurnSec, DurationB = _cfg.GripTurnSec });
            Assert.AreEqual(-1, _e.Grip.EffectiveGrip(FighterId.A));

            var ev = _e.ResolveExchange(
                Idle,
                new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.CounterReady),
                new ExchangeScript
                {
                    Attacker = FighterId.A,
                    Technique = BigThrow("batadari"),
                    Read = ReadLevel.FullRead,
                });

            var scored = ev.OfType<JudgementScored>().Single();
            Assert.AreEqual(2, scored.Sum, "−1 + 2 + 0 + 1 = 2");
            Assert.IsFalse(scored.IsCounter, "reading is beaten only by reading");
        }

        /// §4.2 — counter-ready is not a defensive stance, so it draws no −2 even
        /// when the fighter also picked a defensive core.
        [Test]
        public void CounterReady_DrawsNoDefensivePosturePenalty()
        {
            var ev = _e.ResolveExchange(
                Idle,
                new TurnChoice(HandTarget.None, CoreStance.Defensive, LegAction.CounterReady),
                new ExchangeScript
                {
                    Attacker = FighterId.A,
                    Technique = BigThrow("batadari"),
                    Read = ReadLevel.Read,
                });

            var scored = ev.OfType<JudgementScored>().Single();
            Assert.AreEqual(0, scored.Terms[4], "no −2 posture term for a counter-ready fighter");
            Assert.AreEqual(1, scored.Sum, "0 + 0 + 0 + 1 + 0 — Read stepped down to Neutral");
        }

        [Test]
        public void CounterReady_DoesNothingAgainstAPoke()
        {
            var ev = _e.ResolveExchange(
                Idle,
                new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.CounterReady),
                new ExchangeScript
                {
                    Attacker = FighterId.A,
                    Technique = Poke("kensei"),
                    Read = ReadLevel.Neutral,
                });

            Assert.IsEmpty(ev.OfType<CounterReadyApplied>(), "only a big throw feeds a counter");
        }

        [Test]
        public void CounterReady_CounterCostIsPaidByTheCounterer_NoRefund()
        {
            _e.ResolveExchange(
                Idle,
                new TurnChoice(HandTarget.RightSleeve, CoreStance.Neutral, LegAction.Neutral, Hand.Left),
                new ExchangeScript { DurationA = _cfg.GripTurnSec, DurationB = _cfg.GripTurnSec });

            _e.ResolveExchange(
                Idle,
                new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.CounterReady),
                new ExchangeScript
                {
                    Attacker = FighterId.A,
                    Technique = BigThrow("batadari"),
                    Read = ReadLevel.Neutral,
                });

            // A paid 5 × 1.5 = 8 for the throw and keeps it despite being countered;
            // B paid 2 defence + 2 counter. Both then take the §4.3 matte recovery
            // (+3, v3.4) — a counter that scores is a score, so it lands on matte like
            // any other. That is a flat term on top and does NOT refund the throw:
            // the gap between the two sides is unchanged at 8 − 4.
            Assert.AreEqual(92 + _cfg.MatteRecovery, _e.Stamina.Get(FighterId.A),
                "no refund for the countered attacker");
            Assert.AreEqual(96 + _cfg.MatteRecovery, _e.Stamina.Get(FighterId.B));
        }
    }
}
