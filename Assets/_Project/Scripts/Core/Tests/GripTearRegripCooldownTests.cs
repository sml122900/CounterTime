using System.Linq;
using NUnit.Framework;
using CounterTime.Core;

namespace CounterTime.Core.Tests
{
    /// <summary>
    /// §2b v4.3 뜯긴 존 재잡기 쿨다운(사용자 확정). 뜯기 성공으로 해제된 존은
    /// "그 교환 + 다음 1교환"(⚙ <see cref="MatchRulesConfig.TearRegripCooldown"/>,
    /// 기본 1) 동안 원래 홀더가 재잡기 시도해도 매트릭스보다 우선해 자동 실패한다.
    /// </summary>
    public class GripTearRegripCooldownTests
    {
        static MatchEngine NewEngine() =>
            new MatchEngine(new MatchRulesConfig { ThresholdJitter = 0, GripRevisionGate = true },
                RefereeProfile.Standard, new FixedRandomSource(0f));

        static TurnChoice Tear(Hand ownHand, DefenseZone zone, TearMethod method) =>
            new TurnChoice(HandTarget.Tear, CoreStance.Neutral, LegAction.Neutral, ownHand,
                tearZone: zone, tearMethodChoice: method);

        static TurnChoice Grab(HandTarget target, Hand ownHand) =>
            new TurnChoice(target, CoreStance.Neutral, LegAction.Neutral, ownHand);

        [Test]
        public void TornZone_BlocksRegrip_ThatExchangeAndNext_ThenAllowsAgain()
        {
            var e = NewEngine();
            // B.Left holds A's RightSleeve, not solid — Basic 뜯기가 결정론적으로 성공한다.
            e.Grip.Grab(FighterId.B, Hand.Left, Hand.Right, GripKind.Normal);

            // 교환 1 — A가 뜯는 것과 동시에 B가 같은 손으로 같은 존을 즉시 재잡기 시도.
            var ev1 = e.ResolveExchange(
                Tear(Hand.Right, DefenseZone.RightSleeve, TearMethod.Basic),
                Grab(HandTarget.RightSleeve, Hand.Left),
                new ExchangeScript());
            Assert.AreEqual(TearResult.Success, ev1.OfType<GripTearAttempted>().Single().Result);
            Assert.IsTrue(ev1.OfType<GripAttemptFailed>().Any(f => f.Who == FighterId.B),
                "같은 교환 내 재잡기는 쿨다운에 막혀 실패해야 한다");
            Assert.AreEqual(GripKind.None, e.Grip.Held(FighterId.B, Hand.Left));

            // 교환 2 — 뜯기 다음 1교환, 여전히 막혀야 한다.
            var ev2 = e.ResolveExchange(TurnChoice.Neutral,
                Grab(HandTarget.RightSleeve, Hand.Left), new ExchangeScript());
            Assert.IsTrue(ev2.OfType<GripAttemptFailed>().Any(f => f.Who == FighterId.B),
                "다음 1교환도 막혀야 한다");
            Assert.AreEqual(GripKind.None, e.Grip.Held(FighterId.B, Hand.Left));

            // 교환 3 — 쿨다운이 끝나 재잡기가 성립해야 한다.
            var ev3 = e.ResolveExchange(TurnChoice.Neutral,
                Grab(HandTarget.RightSleeve, Hand.Left), new ExchangeScript());
            Assert.IsFalse(ev3.OfType<GripAttemptFailed>().Any(f => f.Who == FighterId.B),
                "그다음 교환부터는 쿨다운이 풀려야 한다");
            Assert.AreEqual(GripKind.Normal, e.Grip.Held(FighterId.B, Hand.Left));
        }

        [Test]
        public void TornZone_DoesNotBlock_UnrelatedZone()
        {
            var e = NewEngine();
            e.Grip.Grab(FighterId.B, Hand.Left, Hand.Right, GripKind.Normal);   // A's RightSleeve
            e.ResolveExchange(
                Tear(Hand.Right, DefenseZone.RightSleeve, TearMethod.Basic),
                TurnChoice.Neutral, new ExchangeScript());

            // 같은 교환 직후, 전혀 다른 존(왼소매)에 대한 B의 그립 시도는 쿨다운과 무관하게 성립해야 한다.
            var ev = e.ResolveExchange(TurnChoice.Neutral,
                Grab(HandTarget.LeftSleeve, Hand.Right), new ExchangeScript());
            Assert.IsFalse(ev.OfType<GripAttemptFailed>().Any(f => f.Who == FighterId.B));
            Assert.AreEqual(GripKind.Normal, e.Grip.Held(FighterId.B, Hand.Right));
        }
    }
}
