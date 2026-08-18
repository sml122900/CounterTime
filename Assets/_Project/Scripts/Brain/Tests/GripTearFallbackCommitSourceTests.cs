using System.Linq;
using NUnit.Framework;
using CounterTime.Core;
using CounterTime.Brain;

namespace CounterTime.Brain.Tests
{
    /// <summary>
    /// §2b v4.3 — B 의 최소 뜯기 대응(<see cref="GripTearFallbackCommitSource"/>).
    /// 습관 카탈로그는 안 건드린다 — 습관이 하나도 없는 <see cref="OpponentDefinition"/>
    /// 으로 항상 3단 폴백까지 내려가게 해서, 이 데코레이터 자체의 게이트·확률·존
    /// 선택만 좁혀서 본다.
    /// </summary>
    public class GripTearFallbackCommitSourceTests
    {
        static readonly TurnChoice Idle = TurnChoice.Neutral;

        static (MatchEngine engine, GripTearFallbackCommitSource decorator) Build(
            bool gripRevisionGate, float chance, float rngValue)
        {
            var cfg = new MatchRulesConfig { ThresholdJitter = 0, GripRevisionGate = gripRevisionGate };
            var rng = new FixedRandomSource(rngValue);
            var engine = new MatchEngine(cfg, RefereeProfile.Standard, rng);
            var opponent = new OpponentDefinition("no-habits", new HabitDefinition[0]);
            var state = new HabitState(opponent);
            var ctx = new MatchBrainContext(engine, FighterId.B);
            var inner = new HabitBrainCommitSource(FighterId.B, state, ctx, ctx, rng);
            var setup = new SetupPrecedingCommitSource(inner, ctx, enabled: false);
            var decorator = new GripTearFallbackCommitSource(setup, engine, FighterId.B,
                gripRevisionGate, chance, rng);
            return (engine, decorator);
        }

        /// A 의 오른손이 B 의 오른소매를 잡는다(B 시점 "잡힌 부위") — 데코레이터를
        /// 거치지 않고 엔진에 직접.
        static void GrabBRightSleeveWithARight(MatchEngine e) =>
            e.Grip.Grab(FighterId.A, Hand.Right, Hand.Right, GripKind.Normal);

        [Test]
        public void NoHeldZones_NeverFires_EvenWithGoodRoll()
        {
            var (_, decorator) = Build(gripRevisionGate: true, chance: 0.30f, rngValue: 0f);

            decorator.TryNext(out var who, out var choice);

            Assert.AreEqual(FighterId.B, who);
            Assert.AreNotEqual(HandTarget.Tear, choice.Hand, "잡힌 부위가 없으면 뜯기를 시도할 이유가 없다");
        }

        [Test]
        public void HeldZone_RollUnderChance_Fires_WithBasicMethod()
        {
            var (engine, decorator) = Build(gripRevisionGate: true, chance: 0.30f, rngValue: 0.01f);
            GrabBRightSleeveWithARight(engine);

            decorator.TryNext(out var who, out var choice);

            Assert.AreEqual(FighterId.B, who);
            Assert.AreEqual(HandTarget.Tear, choice.Hand);
            Assert.AreEqual(DefenseZone.RightSleeve, choice.TearZone);
            Assert.AreEqual(TearMethod.Basic, choice.TearMethodChoice, "방법 선택 지능은 3차 이월 — 항상 Basic");
            Assert.IsNull(decorator.LastTechnique, "뜯기 교환은 기술을 안 든다");
            Assert.IsFalse(decorator.LastDecision.Fired, "뜯기는 습관이 아니다 — 관측 원장에 안 남는다");
        }

        [Test]
        public void HeldZone_RollOverChance_PassesThroughUnchanged()
        {
            var (engine, decorator) = Build(gripRevisionGate: true, chance: 0.30f, rngValue: 0.99f);
            GrabBRightSleeveWithARight(engine);

            decorator.TryNext(out _, out var choice);

            Assert.AreNotEqual(HandTarget.Tear, choice.Hand, "확률 롤을 못 넘으면 원래 결정을 그대로 낸다");
        }

        [Test]
        public void GateDisabled_NeverFires_EvenWithHeldZoneAndGoodRoll()
        {
            var (engine, decorator) = Build(gripRevisionGate: false, chance: 0.30f, rngValue: 0f);
            GrabBRightSleeveWithARight(engine);

            decorator.TryNext(out _, out var choice);

            Assert.AreNotEqual(HandTarget.Tear, choice.Hand, "GripRevisionGate 가 꺼져 있으면 이 폴백 자체가 죽어있다");
        }

        [Test]
        public void HeldTwistGripZone_IsExcluded_MethodIntelligenceIsThirdPhase()
        {
            // 틀어잡기는 "방법 선택 지능 3차 이월" 범위 밖 — B 폴백 후보에서 제외돼야 한다.
            var (engine, decorator) = Build(gripRevisionGate: true, chance: 1f, rngValue: 0f);
            engine.Grip.Grab(FighterId.A, Hand.Right, Hand.Right, GripKind.BackGrip);   // TwistGrip on B

            decorator.TryNext(out _, out var choice);

            Assert.AreNotEqual(HandTarget.Tear, choice.Hand, "틀어잡기만 잡혀 있으면 폴백 뜯기 후보가 없다");
        }
    }
}
