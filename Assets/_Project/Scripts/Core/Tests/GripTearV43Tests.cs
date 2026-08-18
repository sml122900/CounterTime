using System.Linq;
using NUnit.Framework;
using CounterTime.Core;

namespace CounterTime.Core.Tests
{
    /// <summary>
    /// §2b v4.3 — 뜯기(라이브 전용, `GripRevisionGate`). Basic=견고 시 결정론적 실패,
    /// Forceful/Assisted=견고 시 확률 성공(실패 시 비견고화만), Assisted 성공 시
    /// 되잡기(단순 소매잡기) 성립. §2a(게이트 off)의 옛 Lock/Maintain 2택 메커니즘은
    /// 이 파일에서 한 번도 안 건드린다 — <see cref="GripRevisionTests"/> 가 이미
    /// 그 경로를 덮는다.
    /// </summary>
    public class GripTearV43Tests
    {
        static MatchEngine NewEngine(float rngValue = 0f) =>
            new MatchEngine(new MatchRulesConfig { ThresholdJitter = 0, GripRevisionGate = true },
                RefereeProfile.Standard, new FixedRandomSource(rngValue));

        static TurnChoice Tear(Hand ownHand, DefenseZone zone, TearMethod method) =>
            new TurnChoice(HandTarget.Tear, CoreStance.Neutral, LegAction.Neutral, ownHand,
                tearZone: zone, tearMethodChoice: method);

        /// B의 왼손이 A의 오른소매를 잡은 기본 셋업(사용자 규칙 (a)의 구체 예시와 동일).
        static void GrabARightSleeveWithBLeft(MatchEngine e, bool solid = false)
        {
            e.Grip.Grab(FighterId.B, Hand.Left, Hand.Right, GripKind.Normal);
            if (solid) e.Grip.Solidify(FighterId.B, Hand.Left);
        }

        static void GrabARightLapelWithBLeft(MatchEngine e, bool solid = false)
        {
            e.Grip.Grab(FighterId.B, Hand.Left, Hand.Right, GripKind.Lapel);
            if (solid) e.Grip.Solidify(FighterId.B, Hand.Left);
        }

        // ---- 소매 3법 × 견고/비견고 ----

        [Test]
        public void Sleeve_Basic_NotSolid_Succeeds()
        {
            var e = NewEngine();
            GrabARightSleeveWithBLeft(e, solid: false);
            var ev = e.ResolveExchange(Tear(Hand.Right, DefenseZone.RightSleeve, TearMethod.Basic),
                TurnChoice.Neutral, new ExchangeScript());
            Assert.AreEqual(TearResult.Success, ev.OfType<GripTearAttempted>().Single().Result);
            Assert.AreEqual(GripKind.None, e.Grip.Held(FighterId.B, Hand.Left));
        }

        [Test]
        public void Sleeve_Basic_Solid_AlwaysFails()
        {
            var e = NewEngine();
            GrabARightSleeveWithBLeft(e, solid: true);
            var ev = e.ResolveExchange(Tear(Hand.Right, DefenseZone.RightSleeve, TearMethod.Basic),
                TurnChoice.Neutral, new ExchangeScript());
            Assert.AreEqual(TearResult.Failed, ev.OfType<GripTearAttempted>().Single().Result);
            Assert.AreEqual(GripKind.Normal, e.Grip.Held(FighterId.B, Hand.Left), "실패해도 그립은 그대로 유지된다");
        }

        [Test]
        public void Sleeve_Forceful_NotSolid_Succeeds()
        {
            var e = NewEngine();
            GrabARightSleeveWithBLeft(e, solid: false);
            var ev = e.ResolveExchange(Tear(Hand.Right, DefenseZone.RightSleeve, TearMethod.Forceful),
                TurnChoice.Neutral, new ExchangeScript());
            Assert.AreEqual(TearResult.Success, ev.OfType<GripTearAttempted>().Single().Result);
        }

        [Test]
        public void Sleeve_Forceful_Solid_RollUnder50Percent_Succeeds()
        {
            var e = NewEngine(rngValue: 0.01f);
            GrabARightSleeveWithBLeft(e, solid: true);
            var ev = e.ResolveExchange(Tear(Hand.Right, DefenseZone.RightSleeve, TearMethod.Forceful),
                TurnChoice.Neutral, new ExchangeScript());
            Assert.AreEqual(TearResult.Success, ev.OfType<GripTearAttempted>().Single().Result);
            Assert.AreEqual(GripKind.None, e.Grip.Held(FighterId.B, Hand.Left));
        }

        [Test]
        public void Sleeve_Forceful_Solid_RollOver50Percent_Unsolidifies_HoldPersists()
        {
            var e = NewEngine(rngValue: 0.99f);
            GrabARightSleeveWithBLeft(e, solid: true);
            var ev = e.ResolveExchange(Tear(Hand.Right, DefenseZone.RightSleeve, TearMethod.Forceful),
                TurnChoice.Neutral, new ExchangeScript());
            Assert.AreEqual(TearResult.Unsolidified, ev.OfType<GripTearAttempted>().Single().Result);
            Assert.AreEqual(GripKind.Normal, e.Grip.Held(FighterId.B, Hand.Left), "쥔 것 자체는 유지된다");
            Assert.IsFalse(e.Grip.IsSolid(FighterId.B, Hand.Left), "견고화 보너스만 사라진다");
        }

        [Test]
        public void Sleeve_Assisted_OtherHandFree_Succeeds_AndFormsCounterGrab()
        {
            var e = NewEngine();
            GrabARightSleeveWithBLeft(e, solid: false);   // B.Left holds A's RightSleeve
            var ev = e.ResolveExchange(Tear(Hand.Left, DefenseZone.RightSleeve, TearMethod.Assisted),
                TurnChoice.Neutral, new ExchangeScript());
            Assert.AreEqual(TearResult.Success, ev.OfType<GripTearAttempted>().Single().Result);
            Assert.AreEqual(GripKind.None, e.Grip.Held(FighterId.B, Hand.Left), "원래 그립은 해제된다");
            Assert.AreEqual(GripKind.Normal, e.Grip.Held(FighterId.A, Hand.Left), "되잡기(단순 소매잡기)가 성립한다");
        }

        [Test]
        public void Sleeve_Assisted_CounterGrab_IsCrossOriented_PerSpecExample()
        {
            // 사용자 규칙 (a) 구체 예시: "내 오른소매를 잡던 상대 왼손 → 내 자유
            // 왼손이 그 왼소매를 잡으면 교차."
            var e = NewEngine();
            GrabARightSleeveWithBLeft(e, solid: false);
            e.ResolveExchange(Tear(Hand.Left, DefenseZone.RightSleeve, TearMethod.Assisted),
                TurnChoice.Neutral, new ExchangeScript());
            Assert.IsTrue(e.Grip.IsCrossOrientation(FighterId.A, Hand.Left),
                "상대의 왼손을 내 왼손으로 되잡으면 같은측 = 교차다");
        }

        [Test]
        public void Sleeve_Assisted_CounterGrab_StartsCrossGripTimer_SameExchange()
        {
            var e = NewEngine();
            GrabARightSleeveWithBLeft(e, solid: false);
            var ev = e.ResolveExchange(Tear(Hand.Left, DefenseZone.RightSleeve, TearMethod.Assisted),
                TurnChoice.Neutral, new ExchangeScript());
            var tick = ev.OfType<CrossGripTimerTicked>().SingleOrDefault(t => t.Who == FighterId.A);
            Assert.IsNotNull(tick, "되잡기가 교차잡기라 같은 교환에 타이머가 즉시 시작돼야 한다");
            Assert.IsFalse(tick.Expired);
        }

        [Test]
        public void Sleeve_Assisted_OtherHandOccupied_Fails_NoCounterGrab()
        {
            var e = NewEngine();
            GrabARightSleeveWithBLeft(e, solid: false);
            e.Grip.Grab(FighterId.A, Hand.Left, Hand.Left, GripKind.Normal);   // 반대(=조력) 손이 이미 점유
            var ev = e.ResolveExchange(Tear(Hand.Left, DefenseZone.RightSleeve, TearMethod.Assisted),
                TurnChoice.Neutral, new ExchangeScript());
            Assert.AreEqual(TearResult.Failed, ev.OfType<GripTearAttempted>().Single().Result);
            Assert.AreEqual(GripKind.Normal, e.Grip.Held(FighterId.B, Hand.Left), "원래 그립도 그대로 남는다");
        }

        // ---- 가슴깃 3법 × 견고/비견고 ----

        [Test]
        public void Lapel_Basic_NotSolid_Succeeds()
        {
            var e = NewEngine();
            GrabARightLapelWithBLeft(e, solid: false);
            var ev = e.ResolveExchange(Tear(Hand.Right, DefenseZone.RightLapel, TearMethod.Basic),
                TurnChoice.Neutral, new ExchangeScript());
            Assert.AreEqual(TearResult.Success, ev.OfType<GripTearAttempted>().Single().Result);
        }

        [Test]
        public void Lapel_Basic_Solid_AlwaysFails()
        {
            var e = NewEngine();
            GrabARightLapelWithBLeft(e, solid: true);
            var ev = e.ResolveExchange(Tear(Hand.Right, DefenseZone.RightLapel, TearMethod.Basic),
                TurnChoice.Neutral, new ExchangeScript());
            Assert.AreEqual(TearResult.Failed, ev.OfType<GripTearAttempted>().Single().Result);
        }

        [Test]
        public void Lapel_Forceful_BothHandsFree_Solid_RollUnder50Percent_Succeeds()
        {
            var e = NewEngine(rngValue: 0.01f);
            GrabARightLapelWithBLeft(e, solid: true);
            var ev = e.ResolveExchange(Tear(Hand.Right, DefenseZone.RightLapel, TearMethod.Forceful),
                TurnChoice.Neutral, new ExchangeScript());
            Assert.AreEqual(TearResult.Success, ev.OfType<GripTearAttempted>().Single().Result);
        }

        [Test]
        public void Lapel_Forceful_BothHandsFree_Solid_RollOver50Percent_Unsolidifies()
        {
            var e = NewEngine(rngValue: 0.99f);
            GrabARightLapelWithBLeft(e, solid: true);
            var ev = e.ResolveExchange(Tear(Hand.Right, DefenseZone.RightLapel, TearMethod.Forceful),
                TurnChoice.Neutral, new ExchangeScript());
            Assert.AreEqual(TearResult.Unsolidified, ev.OfType<GripTearAttempted>().Single().Result);
            Assert.AreEqual(GripKind.Lapel, e.Grip.Held(FighterId.B, Hand.Left));
        }

        [Test]
        public void Lapel_Forceful_HandsNotBothFree_Fails()
        {
            var e = NewEngine();
            GrabARightLapelWithBLeft(e, solid: false);
            e.Grip.Grab(FighterId.A, Hand.Right, Hand.Left, GripKind.Normal);   // 내 오른손 점유
            var ev = e.ResolveExchange(Tear(Hand.Right, DefenseZone.RightLapel, TearMethod.Forceful),
                TurnChoice.Neutral, new ExchangeScript());
            Assert.AreEqual(TearResult.Failed, ev.OfType<GripTearAttempted>().Single().Result);
        }

        [Test]
        public void Lapel_Assisted_BothHandsFree_Succeeds_AndFormsCounterGrab()
        {
            var e = NewEngine();
            GrabARightLapelWithBLeft(e, solid: false);
            var ev = e.ResolveExchange(Tear(Hand.Right, DefenseZone.RightLapel, TearMethod.Assisted),
                TurnChoice.Neutral, new ExchangeScript());
            Assert.AreEqual(TearResult.Success, ev.OfType<GripTearAttempted>().Single().Result);
            Assert.AreEqual(GripKind.None, e.Grip.Held(FighterId.B, Hand.Left));
            Assert.AreEqual(GripKind.Normal, e.Grip.Held(FighterId.A, Hand.Right),
                "가슴깃 되잡기도 단순 소매잡기 1건으로 성립한다");
        }

        [Test]
        public void Lapel_Assisted_HandsNotBothFree_Fails()
        {
            var e = NewEngine();
            GrabARightLapelWithBLeft(e, solid: false);
            e.Grip.Grab(FighterId.A, Hand.Left, Hand.Left, GripKind.Normal);
            var ev = e.ResolveExchange(Tear(Hand.Right, DefenseZone.RightLapel, TearMethod.Assisted),
                TurnChoice.Neutral, new ExchangeScript());
            Assert.AreEqual(TearResult.Failed, ev.OfType<GripTearAttempted>().Single().Result);
        }

        // ---- 존이 실제로 안 잡혀 있을 때 ----

        [Test]
        public void TearingAnUnheldZone_IsANoOp_Fails()
        {
            var e = NewEngine();
            var ev = e.ResolveExchange(Tear(Hand.Right, DefenseZone.RightSleeve, TearMethod.Basic),
                TurnChoice.Neutral, new ExchangeScript());
            Assert.AreEqual(TearResult.Failed, ev.OfType<GripTearAttempted>().Single().Result);
        }

        // ---- 스태미나는 결과 무관 항상 지불(§2 시간·비용표) ----

        [Test]
        public void Tear_AlwaysPaysStamina_RegardlessOfOutcome()
        {
            var e = NewEngine();
            int before = e.Stamina.Get(FighterId.A);
            e.ResolveExchange(Tear(Hand.Right, DefenseZone.RightSleeve, TearMethod.Basic),
                TurnChoice.Neutral, new ExchangeScript());   // 아무것도 안 잡혀있어 실패지만
            Assert.Less(e.Stamina.Get(FighterId.A), before);
        }
    }
}
