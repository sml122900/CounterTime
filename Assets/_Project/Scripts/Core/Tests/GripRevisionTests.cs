using System.Linq;
using NUnit.Framework;
using CounterTime.Core;

namespace CounterTime.Core.Tests
{
    /// <summary>
    /// §2 v4.2 — 잡기 개편 1차. `GripRevisionGate` 기본 off — 이 표를 검증한다: off →
    /// 옛 무조건-성공 경로 그대로 / on → 해결 매트릭스(가슴깃 코어 연동 / 같은 쌍 맞잡기 /
    /// 서로 다른 손 목표 / 방어된 존) + 교차잡기 롤링 타이머 + 견고화.
    /// </summary>
    public class GripRevisionTests
    {
        static readonly TurnChoice Idle = TurnChoice.Neutral;

        static TurnChoice Grab(HandTarget target, CoreStance core, Hand ownHand = Hand.Right) =>
            new TurnChoice(target, core, LegAction.Neutral, ownHand);

        static TurnChoice Evade(DefenseZone? left, DefenseZone? right) =>
            new TurnChoice(HandTarget.Evade, CoreStance.Defensive, LegAction.Defend,
                Hand.Right, left, right);

        static ExchangeScript RealThrow(FighterId who) => new ExchangeScript
        {
            Attacker = who,
            Technique = new TechniqueSpec("밭다리", ActionCost.BigThrow, TechniqueClass.Standard),
            Read = ReadLevel.Neutral,
        };

        MatchEngine NewEngine(IRandomSource rng = null) =>
            new MatchEngine(new MatchRulesConfig { ThresholdJitter = 0, GripRevisionGate = true },
                RefereeProfile.Standard, rng ?? new FixedRandomSource(0.99f));

        // ---- 게이트 off: 옛 경로 그대로 ----

        [Test]
        public void FlagOff_LapelGrab_AlwaysSucceeds_EvenAgainstDefensiveOpponent()
        {
            var cfg = new MatchRulesConfig { ThresholdJitter = 0 };   // GripRevisionGate 기본 false
            var e = new MatchEngine(cfg, RefereeProfile.Standard, new FixedRandomSource(0.99f));

            var ev = e.ResolveExchange(Grab(HandTarget.RightLapel, CoreStance.Neutral),
                new TurnChoice(HandTarget.None, CoreStance.Defensive, LegAction.Neutral),
                new ExchangeScript());

            Assert.IsNotEmpty(ev.OfType<GripChanged>(), "게이트가 꺼져 있으면 매트릭스 없이 무조건 성공한다");
            Assert.IsEmpty(ev.OfType<GripAttemptFailed>());
        }

        // ---- 가슴깃 노림: 상대 코어 연동 ----

        [Test]
        public void Lapel_OpponentDefensive_Fails()
        {
            var e = NewEngine();
            var ev = e.ResolveExchange(Grab(HandTarget.RightLapel, CoreStance.Neutral),
                new TurnChoice(HandTarget.None, CoreStance.Defensive, LegAction.Neutral),
                new ExchangeScript());

            Assert.IsEmpty(ev.OfType<GripChanged>(), "상대가 수비면 가슴깃 노림은 무조건 실패한다");
            var failed = ev.OfType<GripAttemptFailed>().SingleOrDefault();
            Assert.IsNotNull(failed);
            Assert.AreEqual(HandTarget.RightLapel, failed.Target);
        }

        [Test]
        public void Lapel_OpponentAggressive_Succeeds()
        {
            var e = NewEngine();
            var ev = e.ResolveExchange(Grab(HandTarget.RightLapel, CoreStance.Neutral),
                new TurnChoice(HandTarget.None, CoreStance.Aggressive, LegAction.Neutral),
                new ExchangeScript());

            Assert.IsNotEmpty(ev.OfType<GripChanged>(), "상대가 공격이면 가슴깃 노림은 무조건 성공한다");
        }

        // ---- 소매 같은 쌍 맞잡기 ----

        [Test]
        public void SameTier_BothAggressive_BothSucceed_NetsToZero()
        {
            var e = NewEngine();
            var ev = e.ResolveExchange(
                Grab(HandTarget.RightSleeve, CoreStance.Aggressive, Hand.Right),
                Grab(HandTarget.LeftSleeve, CoreStance.Aggressive, Hand.Left),
                new ExchangeScript());

            Assert.AreEqual(2, ev.OfType<GripChanged>().Count(), "둘 다 공격이면 둘 다 성립한다");
            Assert.AreEqual(0, e.Grip.GripScore(FighterId.A), "같은 급 상호 성립은 순변화 0");
        }

        [Test]
        public void SameTier_BothDefensive_BothFail_NoShidoOnFirstClash()
        {
            var e = NewEngine();
            var ev = e.ResolveExchange(
                Grab(HandTarget.RightSleeve, CoreStance.Defensive, Hand.Right),
                Grab(HandTarget.LeftSleeve, CoreStance.Defensive, Hand.Left),
                new ExchangeScript());

            Assert.IsEmpty(ev.OfType<GripChanged>(), "둘 다 수비면 둘 다 실패한다");
            Assert.AreEqual(2, ev.OfType<GripAttemptFailed>().Count());
            Assert.IsEmpty(ev.OfType<ShidoGiven>(), "1회차는 아직 지도가 아니다 (⚙GripMutualDefenseShidoRepeats=2)");
        }

        [Test]
        public void SameTier_BothDefensive_RepeatedTwice_AwardsShidoToBoth()
        {
            var e = NewEngine();
            var clash = new System.Func<TurnChoice, TurnChoice, System.Collections.Generic.IReadOnlyList<MatchEvent>>(
                (a, b) => e.ResolveExchange(a, b, new ExchangeScript()));

            clash(Grab(HandTarget.RightSleeve, CoreStance.Defensive, Hand.Right),
                  Grab(HandTarget.LeftSleeve, CoreStance.Defensive, Hand.Left));
            var ev = clash(Grab(HandTarget.RightSleeve, CoreStance.Defensive, Hand.Right),
                           Grab(HandTarget.LeftSleeve, CoreStance.Defensive, Hand.Left));

            var shidos = ev.OfType<ShidoGiven>().ToList();
            Assert.AreEqual(2, shidos.Count, "2회 연속 정체면 양쪽 다 지도");
            Assert.IsTrue(shidos.Any(s => s.Who == FighterId.A));
            Assert.IsTrue(shidos.Any(s => s.Who == FighterId.B));
        }

        // ---- 공·수 갈림 10% 솔로 롤 ----

        [Test]
        public void SplitStance_RollHits_AggressiveSideSucceedsSolidified_OtherFails()
        {
            var e = NewEngine(new FixedRandomSource(0.05f));   // < 0.10 ⚙
            var ev = e.ResolveExchange(
                Grab(HandTarget.RightSleeve, CoreStance.Aggressive, Hand.Right),
                Grab(HandTarget.LeftSleeve, CoreStance.Defensive, Hand.Left),
                new ExchangeScript());

            var changed = ev.OfType<GripChanged>().ToList();
            Assert.AreEqual(1, changed.Count, "롤이 맞으면 공격측만 성립한다");
            Assert.AreEqual(FighterId.A, changed[0].Who);
            Assert.IsNotEmpty(ev.OfType<GripSolidified>().Where(s => s.Who == FighterId.A),
                "공격측 단독 성립은 즉시 견고화(+1)된다");
            Assert.IsNotEmpty(ev.OfType<GripAttemptFailed>().Where(f => f.Who == FighterId.B));
        }

        [Test]
        public void SplitStance_RollMisses_BothSucceedPlainly()
        {
            var e = NewEngine(new FixedRandomSource(0.99f));   // ≥ 0.10 ⚙, 롤 실패
            var ev = e.ResolveExchange(
                Grab(HandTarget.RightSleeve, CoreStance.Aggressive, Hand.Right),
                Grab(HandTarget.LeftSleeve, CoreStance.Defensive, Hand.Left),
                new ExchangeScript());

            Assert.AreEqual(2, ev.OfType<GripChanged>().Count(), "롤이 안 맞으면 둘 다 성립한다");
            Assert.IsEmpty(ev.OfType<GripSolidified>());
        }

        // ---- 서로 다른 손 목표 ----

        [Test]
        public void DifferentTiers_BothSucceed_RegardlessOfCore()
        {
            var e = NewEngine();
            var ev = e.ResolveExchange(
                Grab(HandTarget.RightSleeve, CoreStance.Defensive, Hand.Right),
                Grab(HandTarget.TwistGrip, CoreStance.Defensive, Hand.Left),
                new ExchangeScript());

            Assert.AreEqual(2, ev.OfType<GripChanged>().Count(),
                "다른 목표는 코어와 무관하게 둘 다 성립한다 — '둘 다 수비=실패'는 같은 쌍 전용");
        }

        // ---- 방어된 존 ----

        [Test]
        public void DefendedZone_BlocksTheMatchingAttempt()
        {
            var e = NewEngine();
            var ev = e.ResolveExchange(
                Grab(HandTarget.RightSleeve, CoreStance.Aggressive, Hand.Right),
                Evade(DefenseZone.RightSleeve, DefenseZone.RightUpper),
                new ExchangeScript());

            Assert.IsEmpty(ev.OfType<GripChanged>(), "방어된 존을 노리면 확률 0으로 실패한다");
            Assert.IsNotEmpty(ev.OfType<GripAttemptFailed>().Where(f => f.Who == FighterId.A));
        }

        [Test]
        public void UndefendedZone_StillSucceeds()
        {
            var e = NewEngine();
            var ev = e.ResolveExchange(
                Grab(HandTarget.LeftSleeve, CoreStance.Aggressive, Hand.Left),
                Evade(DefenseZone.RightSleeve, DefenseZone.RightUpper),
                new ExchangeScript());

            Assert.IsNotEmpty(ev.OfType<GripChanged>(), "방어 안 한 존은 그대로 성립한다");
        }

        // ---- 견고화 ----

        [Test]
        public void Solidify_OnHeldHand_RaisesGripScore()
        {
            var e = NewEngine();
            e.ResolveExchange(Grab(HandTarget.RightSleeve, CoreStance.Neutral, Hand.Right), Idle,
                new ExchangeScript());
            int before = e.Grip.GripScore(FighterId.A);

            var solidify = new TurnChoice(HandTarget.Solidify, CoreStance.Neutral, LegAction.Neutral, Hand.Right);
            var ev = e.ResolveExchange(solidify, Idle, new ExchangeScript());

            Assert.IsNotEmpty(ev.OfType<GripSolidified>());
            Assert.Greater(e.Grip.GripScore(FighterId.A), before, "견고화는 +1 이므로 점수가 올라가야 한다");
        }

        [Test]
        public void Solidify_OnEmptyHand_IsNoOp()
        {
            var e = NewEngine();
            var solidify = new TurnChoice(HandTarget.Solidify, CoreStance.Neutral, LegAction.Neutral, Hand.Right);
            var ev = e.ResolveExchange(solidify, Idle, new ExchangeScript());

            Assert.IsEmpty(ev.OfType<GripSolidified>(), "잡은 게 없으면 견고화할 대상이 없다");
        }

        // ---- 교차잡기 롤링 타이머 ----

        [Test]
        public void CrossGrip_MirrorOrientation_NeverStartsTheTimer()
        {
            var e = NewEngine();
            // OwnHand=Right, target=LeftSleeve(side=Left) → 정(mirror), 교차 아님.
            var ev = e.ResolveExchange(Grab(HandTarget.LeftSleeve, CoreStance.Neutral, Hand.Right), Idle,
                new ExchangeScript());

            Assert.IsEmpty(ev.OfType<CrossGripTimerTicked>());
        }

        [Test]
        public void CrossGrip_Established_StartsTimer_WithoutBurningATurn()
        {
            var e = NewEngine();
            // OwnHand=Right, target=RightSleeve(side=Right) → 교차.
            var ev = e.ResolveExchange(Grab(HandTarget.RightSleeve, CoreStance.Neutral, Hand.Right), Idle,
                new ExchangeScript());

            var tick = ev.OfType<CrossGripTimerTicked>().SingleOrDefault();
            Assert.IsNotNull(tick);
            Assert.AreEqual(2, tick.TurnsRemaining, "⚙CrossGripTimerTurns=2 그대로, 확립 첫 교환은 안 깎인다");
            Assert.IsFalse(tick.Expired);
        }

        [Test]
        public void CrossGrip_ExpiresWithoutSetupOrTechnique_AwardsShido_AndRestarts()
        {
            var e = NewEngine();
            e.ResolveExchange(Grab(HandTarget.RightSleeve, CoreStance.Neutral, Hand.Right), Idle,
                new ExchangeScript());                                          // establish, timer=2
            e.ResolveExchange(Idle, Idle, new ExchangeScript());                // timer=1
            var ev = e.ResolveExchange(Idle, Idle, new ExchangeScript());       // timer=0 → 지도 + 재시작

            var shido = ev.OfType<ShidoGiven>().SingleOrDefault();
            Assert.IsNotNull(shido);
            Assert.AreEqual(FighterId.A, shido.Who);

            var tick = ev.OfType<CrossGripTimerTicked>().Single();
            Assert.IsTrue(tick.Expired);
            Assert.AreEqual(2, tick.TurnsRemaining, "지도 후에도 교차 유지면 타이머가 재시작한다");
        }

        [Test]
        public void CrossGrip_RealTechniqueAttempt_ResetsTimer_AvoidingTheShido()
        {
            var e = NewEngine();
            e.ResolveExchange(Grab(HandTarget.RightSleeve, CoreStance.Neutral, Hand.Right), Idle,
                new ExchangeScript());                                                    // establish, timer=2
            e.ResolveExchange(Idle, Idle, new ExchangeScript());                          // timer=1
            var ev = e.ResolveExchange(Idle, Idle, RealThrow(FighterId.A));               // A 기술 시도 → 리셋

            Assert.IsEmpty(ev.OfType<ShidoGiven>(), "기술을 냈으니 타이머가 리셋돼 지도가 안 나야 한다");
            var tick = ev.OfType<CrossGripTimerTicked>().SingleOrDefault();
            Assert.IsNotNull(tick);
            Assert.AreEqual(2, tick.TurnsRemaining);
            Assert.IsFalse(tick.Expired);
        }
    }
}
