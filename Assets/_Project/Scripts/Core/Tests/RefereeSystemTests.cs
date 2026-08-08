using NUnit.Framework;
using CounterTime.Core;

namespace CounterTime.Core.Tests
{
    public class RefereeSystemTests
    {
        MatchRulesConfig _cfg;

        [SetUp]
        public void SetUp() => _cfg = new MatchRulesConfig();

        RefereeSystem Ref(RefereeProfile p = RefereeProfile.Standard, int jitter = 0) =>
            new RefereeSystem(p, _cfg, new FixedRandomSource(i: jitter));

        static readonly TurnChoice Turtle =
            new TurnChoice(HandTarget.Evade, CoreStance.Defensive, LegAction.Defend);
        static readonly TurnChoice AllNeutral =
            new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.Neutral);
        static readonly TurnChoice CounterReady =
            new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.CounterReady);
        static readonly TurnChoice Grabbing =
            new TurnChoice(HandTarget.RightSleeve, CoreStance.Aggressive, LegAction.Neutral);

        // Accumulator is hidden by design, so it is probed through the tell ratio.
        static int AccViaShido(RefereeSystem r, FighterId who, TurnChoice c, int turns)
        {
            for (int i = 0; i < turns; i++) r.ObserveTurn(who, c, false, false);
            return r.ConsumeShidoIfDue(who) != null ? 1 : 0;
        }

        // ---- patch 1.1: exclusive categories ----

        [Test]
        public void ExtremeDefense_IsThreeNotSeven()
        {
            // 극단적 방어 matches ExtremeDefense(3), DoubleDefensive(2) and HandEvade(2)
            // simultaneously. Summed that is 7 and shido lands on turn 2.
            var r = Ref();
            Assert.AreEqual(0, AccViaShido(r, FighterId.A, Turtle, 2),
                "two turtled turns must not reach the standard threshold of 12");
            Assert.AreEqual(0, AccViaShido(r, FighterId.A, Turtle, 1), "turn 3 is still under");
        }

        [Test]
        public void ExtremeDefense_StandardRef_ShidoAtFourTurns()
        {
            var r = Ref();
            Assert.AreEqual(12, r.Threshold(FighterId.A));
            for (int i = 0; i < 3; i++) r.ObserveTurn(FighterId.A, Turtle, false, false);
            Assert.IsNull(r.ConsumeShidoIfDue(FighterId.A), "3 turns = 9, under 12");

            r.ObserveTurn(FighterId.A, Turtle, false, false);   // 12
            var shido = r.ConsumeShidoIfDue(FighterId.A);
            Assert.IsNotNull(shido);
            Assert.AreEqual(1, shido.Value.Count);
            Assert.AreEqual(14, shido.Value.NewThreshold, "threshold escalates by 2");
            Assert.AreEqual(14, r.Threshold(FighterId.A));
        }

        [Test]
        public void CounterReady_CountsAsTwo()
        {
            var r = Ref();
            for (int i = 0; i < 5; i++) r.ObserveTurn(FighterId.A, CounterReady, false, false);
            Assert.IsNull(r.ConsumeShidoIfDue(FighterId.A), "5 × 2 = 10, under 12");
            r.ObserveTurn(FighterId.A, CounterReady, false, false);   // 12
            Assert.IsNotNull(r.ConsumeShidoIfDue(FighterId.A));
        }

        [Test]
        public void AttackEntry_ReplacesPositiveCategory_NotAdded()
        {
            // A turtled posture that is also an attack entry scores −2, not 3 + (−2) = 1.
            var r = Ref();
            for (int i = 0; i < 4; i++) r.ObserveTurn(FighterId.A, Turtle, true, false);
            Assert.IsNull(r.ConsumeShidoIfDue(FighterId.A),
                "four attack-entry turns must leave the accumulator floored at 0");
        }

        [Test]
        public void RealAttempt_OutranksAttackEntry()
        {
            var r = Ref();
            for (int i = 0; i < 4; i++) r.ObserveTurn(FighterId.A, Turtle, false, false);  // 12
            r.ObserveTurn(FighterId.A, Grabbing, true, true);   // −4, not −2
            Assert.IsNull(r.ConsumeShidoIfDue(FighterId.A), "12 − 4 = 8, back under threshold");
        }

        [Test]
        public void AllNeutral_IsOne()
        {
            var r = Ref();
            for (int i = 0; i < 11; i++) r.ObserveTurn(FighterId.A, AllNeutral, false, false);
            Assert.IsNull(r.ConsumeShidoIfDue(FighterId.A));
            r.ObserveTurn(FighterId.A, AllNeutral, false, false);   // 12
            Assert.IsNotNull(r.ConsumeShidoIfDue(FighterId.A));
        }

        [Test]
        public void Accumulator_FloorsAtZero()
        {
            var r = Ref();
            for (int i = 0; i < 5; i++) r.ObserveTurn(FighterId.A, Grabbing, false, true);  // −20
            for (int i = 0; i < 4; i++) r.ObserveTurn(FighterId.A, Turtle, false, false);   // +12
            Assert.IsNotNull(r.ConsumeShidoIfDue(FighterId.A),
                "floor at 0 means the −20 cannot be banked as credit");
        }

        // ---- §6.2 profiles ----

        [Test]
        public void Profiles_HaveDistinctThresholds()
        {
            Assert.AreEqual(9, Ref(RefereeProfile.Strict).Threshold(FighterId.A));
            Assert.AreEqual(12, Ref(RefereeProfile.Standard).Threshold(FighterId.A));
            Assert.AreEqual(16, Ref(RefereeProfile.Lenient).Threshold(FighterId.A));
        }

        [Test]
        public void ThresholdJitter_ShiftsByOne()
        {
            Assert.AreEqual(11, Ref(RefereeProfile.Standard, jitter: -1).Threshold(FighterId.A));
            Assert.AreEqual(13, Ref(RefereeProfile.Standard, jitter: +1).Threshold(FighterId.A));
        }

        [Test]
        public void Shido_ResetsAccumulator()
        {
            var r = Ref();
            for (int i = 0; i < 4; i++) r.ObserveTurn(FighterId.A, Turtle, false, false);
            r.ConsumeShidoIfDue(FighterId.A);
            Assert.AreEqual(RefereeTell.None, r.CurrentTell(FighterId.A), "accumulator is back at 0");
        }

        [Test]
        public void Shido_IsPerFighter()
        {
            var r = Ref();
            for (int i = 0; i < 4; i++) r.ObserveTurn(FighterId.A, Turtle, false, false);
            Assert.IsNotNull(r.ConsumeShidoIfDue(FighterId.A));
            Assert.IsNull(r.ConsumeShidoIfDue(FighterId.B), "B was never passive");
        }

        // ---- §6.4 tells ----

        [Test]
        public void Tells_FireAtSeventyAndNinetyPercent()
        {
            var r = Ref();
            Assert.AreEqual(RefereeTell.None, r.CurrentTell(FighterId.A));

            for (int i = 0; i < 3; i++) r.ObserveTurn(FighterId.A, Turtle, false, false);  // 9/12 = .75
            Assert.AreEqual(RefereeTell.Minor, r.CurrentTell(FighterId.A));

            r.ObserveTurn(FighterId.A, CounterReady, false, false);   // 11/12 = .917
            Assert.AreEqual(RefereeTell.Major, r.CurrentTell(FighterId.A));
        }

        // ---- §6.3 feint ----

        [Test]
        public void Feint_DetectedWithinWindow_GivesShido()
        {
            var r = Ref();
            r.ObserveTurn(FighterId.A, Grabbing, false, true);   // real attempt
            r.ObserveTurn(FighterId.A, Turtle, false, false);    // retreat next turn
            var verdict = r.JudgeFeint(FighterId.A, new FixedRandomSource(f: 0.1f)); // < .70
            Assert.AreEqual(FeintVerdict.Detected, verdict);
            Assert.AreEqual(1, r.ShidoCount(FighterId.A));
        }

        [Test]
        public void Feint_Undetected_IsAFreeEscape()
        {
            var r = Ref();
            r.ObserveTurn(FighterId.A, Grabbing, false, true);
            r.ObserveTurn(FighterId.A, Turtle, false, false);
            var verdict = r.JudgeFeint(FighterId.A, new FixedRandomSource(f: 0.9f)); // ≥ .70
            Assert.AreEqual(FeintVerdict.Undetected, verdict);
            Assert.AreEqual(0, r.ShidoCount(FighterId.A));
        }

        [Test]
        public void Feint_OutsideWindow_IsNotACandidate()
        {
            var r = Ref();
            r.ObserveTurn(FighterId.A, Grabbing, false, true);
            r.ObserveTurn(FighterId.A, AllNeutral, false, false);   // turn 1, no retreat
            r.ObserveTurn(FighterId.A, AllNeutral, false, false);   // turn 2, no retreat
            r.ObserveTurn(FighterId.A, Turtle, false, false);       // turn 3 — too late
            Assert.AreEqual(FeintVerdict.NotACandidate,
                r.JudgeFeint(FighterId.A, new FixedRandomSource(f: 0.1f)));
        }

        [Test]
        public void Feint_CounterReadyIsNotARetreat()
        {
            var r = Ref();
            r.ObserveTurn(FighterId.A, Grabbing, false, true);
            r.ObserveTurn(FighterId.A, CounterReady, false, false);
            Assert.AreEqual(FeintVerdict.NotACandidate,
                r.JudgeFeint(FighterId.A, new FixedRandomSource(f: 0.1f)),
                "counter-ready is a commitment, not a retreat");
        }

        [Test]
        public void Feint_NoPriorAttempt_IsNotACandidate()
        {
            var r = Ref();
            r.ObserveTurn(FighterId.A, Turtle, false, false);
            Assert.AreEqual(FeintVerdict.NotACandidate,
                r.JudgeFeint(FighterId.A, new FixedRandomSource(f: 0.1f)));
        }

        [Test]
        public void Feint_StrictRefDetectsWhereLenientDoesNot()
        {
            var strict = Ref(RefereeProfile.Strict);
            strict.ObserveTurn(FighterId.A, Grabbing, false, true);
            strict.ObserveTurn(FighterId.A, Turtle, false, false);
            Assert.AreEqual(FeintVerdict.Detected,
                strict.JudgeFeint(FighterId.A, new FixedRandomSource(f: 0.8f)));  // < .90

            var lenient = Ref(RefereeProfile.Lenient);
            lenient.ObserveTurn(FighterId.A, Grabbing, false, true);
            lenient.ObserveTurn(FighterId.A, Turtle, false, false);
            Assert.AreEqual(FeintVerdict.Undetected,
                lenient.JudgeFeint(FighterId.A, new FixedRandomSource(f: 0.8f))); // ≥ .45
        }
    }
}
