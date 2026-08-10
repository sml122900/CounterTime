using System.Linq;
using NUnit.Framework;
using CounterTime.Core;
using CounterTime.Brain;

namespace CounterTime.Brain.Tests
{
    /// <summary>
    /// §4.4 [초안] — <see cref="KuzushiPrecedingCommitSource"/>. 습관 카탈로그·읽기
    /// 사다리는 이 테스트에서도 안 건드린다 — 결정론적 커스텀 <see cref="HabitDefinition"/>
    /// 하나로 데코레이터 자체의 버퍼링·게이트·원장 안전성만 좁혀서 본다. H1(업어치기)
    /// 카탈로그와 실제로 엮이는지는 별도 통합 테스트 하나로 따로 확인한다.
    /// </summary>
    public class KuzushiPrecedingCommitSourceTests
    {
        static readonly TurnChoice Idle = TurnChoice.Neutral;

        /// 조건 없이 매번 발화하는 테스트 전용 습관 — HabitCatalogue.cs 는 안 건드린다.
        static HabitDefinition AlwaysFiresBigThrow() =>
            new HabitDefinition(HabitId.LegTechAfterShido, // id 는 아무거나, 이 테스트가 안 봄
                ctx => true,
                new HabitAction(TurnChoice.Neutral,
                    new TechniqueSpec("test-bigthrow", ActionCost.BigThrow, TechniqueClass.Standard)),
                fireProbability: 1f);

        static (MatchEngine engine, MatchBrainContext ctx, KuzushiPrecedingCommitSource decorator)
            Build(bool enabled, bool grantGrip)
        {
            var cfg = new MatchRulesConfig { ThresholdJitter = 0, KuzushiGateEnabled = true };
            var rng = new SystemRandomSource(1);
            var engine = new MatchEngine(cfg, RefereeProfile.Standard, rng);
            var opponent = new OpponentDefinition("test", new[] { AlwaysFiresBigThrow() });
            var state = new HabitState(opponent);
            var ctx = new MatchBrainContext(engine, FighterId.B);
            var inner = new HabitBrainCommitSource(FighterId.B, state, ctx, ctx, rng);
            var decorator = new KuzushiPrecedingCommitSource(inner, ctx, enabled);

            if (grantGrip)
                // B(둘째 인자)가 A 의 오른소매를 잡는다 — 데코레이터를 거치지 않고 직접.
                engine.ResolveExchange(Idle,
                    new TurnChoice(HandTarget.RightSleeve, CoreStance.Neutral, LegAction.Neutral),
                    new ExchangeScript());

            return (engine, ctx, decorator);
        }

        [Test]
        public void WhenGripGatePasses_FirstCallEmitsKuzushi_NotTheTechnique()
        {
            var (_, ctx, decorator) = Build(enabled: true, grantGrip: true);
            Assert.GreaterOrEqual(ctx.OwnEffectiveGrip, 1, "전제: B 가 그립을 쥐고 있다");

            decorator.TryNext(out var who, out var choice);

            Assert.AreEqual(FighterId.B, who);
            Assert.AreEqual(HandTarget.Kuzushi, choice.Hand, "그립이 있으면 큰기술 앞에 쿠즈시부터 나와야 한다");
            Assert.IsNull(decorator.LastTechnique, "쿠즈시 교환 자체는 기술을 안 든다");
        }

        [Test]
        public void SecondCall_EmitsTheBufferedTechnique_Unmodified()
        {
            var (engine, _, decorator) = Build(enabled: true, grantGrip: true);

            decorator.TryNext(out _, out var kuzushiChoice);
            engine.ResolveExchange(Idle, kuzushiChoice, new ExchangeScript());   // 쿠즈시 교환을 실제로 흘려보낸다

            decorator.TryNext(out var who, out var choice);

            Assert.AreEqual(FighterId.B, who);
            Assert.AreEqual(HandTarget.None, choice.Hand, "원래 습관 커밋(TurnChoice.Neutral) 그대로여야 한다");
            Assert.IsNotNull(decorator.LastTechnique);
            Assert.AreEqual("test-bigthrow", decorator.LastTechnique.Id);
            Assert.AreEqual(ActionCost.BigThrow, decorator.LastTechnique.Cost);
        }

        [Test]
        public void WhenFlagDisabled_PassesThroughImmediately_NoKuzushiInserted()
        {
            var (_, _, decorator) = Build(enabled: false, grantGrip: true);

            decorator.TryNext(out _, out var choice);

            Assert.AreNotEqual(HandTarget.Kuzushi, choice.Hand);
            Assert.IsNotNull(decorator.LastTechnique, "플래그가 꺼져 있으면 감싸인 결정을 그대로 즉시 내보낸다");
        }

        [Test]
        public void WhenNoGrip_FlagEnabled_PassesThroughImmediately_NoInsertion()
        {
            var (_, ctx, decorator) = Build(enabled: true, grantGrip: false);
            Assert.Less(ctx.OwnEffectiveGrip, 1, "전제: B 가 그립이 없다");

            decorator.TryNext(out _, out var choice);

            Assert.AreNotEqual(HandTarget.Kuzushi, choice.Hand,
                "그립 게이트를 못 넘으면 끼워 넣어봐야 Core 에서 no-op 이니 원래 턴을 그대로 보낸다");
            Assert.IsNotNull(decorator.LastTechnique);
        }

        /// 쿠즈시를 내보내는 교환에서 <see cref="ObservationLedger"/> 가 그 발화를
        /// 두 번 세면 안 된다 — 관측 오염의 구체적인 형태다.
        [Test]
        public void ObservationLedger_CountsTheFiringExactlyOnce_NotTwice()
        {
            var (engine, _, decorator) = Build(enabled: true, grantGrip: true);
            var ledger = new ObservationLedger();

            decorator.TryNext(out _, out var kuzushiChoice);
            Assert.AreEqual(default(HabitDecision).Fired, decorator.LastDecision.Fired,
                "쿠즈시 교환의 LastDecision 은 '발화 없음'이어야 원장이 안전하다");
            ledger.Record(decorator.LastDecision);

            engine.ResolveExchange(Idle, kuzushiChoice, new ExchangeScript());

            decorator.TryNext(out _, out _);
            Assert.IsTrue(decorator.LastDecision.Fired, "진짜 기술이 나오는 교환에는 실제 결정이 실려야 한다");
            ledger.Record(decorator.LastDecision);

            Assert.AreEqual(1, ledger.Observations(HabitId.LegTechAfterShido),
                "같은 발화가 쿠즈시 교환과 기술 교환 양쪽에서 세어지면 원장이 오염된다");
        }

        /// H1(업어치기) 카탈로그 습관과 실제로 엮이는지 확인하는 통합 테스트 —
        /// 커스텀 픽스처가 아니라 HabitCatalogue.Build 그대로를 쓴다.
        [Test]
        public void IntegratesWithTheRealBigThrowOnGripEdgeCatalogueHabit()
        {
            var cfg = new MatchRulesConfig { ThresholdJitter = 0, KuzushiGateEnabled = true };
            var rng = new SystemRandomSource(1);
            var engine = new MatchEngine(cfg, RefereeProfile.Standard, rng);
            var habit = HabitCatalogue.Build(HabitId.BigThrowOnGripEdge, cfg, fireProbability: 1f);
            var state = new HabitState(new OpponentDefinition("진도윤 조각", new[] { habit }));
            var ctx = new MatchBrainContext(engine, FighterId.B);
            var inner = new HabitBrainCommitSource(FighterId.B, state, ctx, ctx, rng);
            var decorator = new KuzushiPrecedingCommitSource(inner, ctx, enabled: true);

            engine.ResolveExchange(Idle,
                new TurnChoice(HandTarget.RightSleeve, CoreStance.Neutral, LegAction.Neutral),
                new ExchangeScript());

            decorator.TryNext(out _, out var kuzushiChoice);
            Assert.AreEqual(HandTarget.Kuzushi, kuzushiChoice.Hand);
            engine.ResolveExchange(Idle, kuzushiChoice, new ExchangeScript());

            decorator.TryNext(out _, out _);
            Assert.AreEqual("업어치기", decorator.LastTechnique.Id);
            Assert.AreEqual(HabitId.BigThrowOnGripEdge, decorator.LastDecision.Habit.Id);
        }
    }
}
