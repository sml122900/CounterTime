using System.Linq;
using NUnit.Framework;
using CounterTime.Core;
using CounterTime.Game.Match;

namespace CounterTime.Game.Tests
{
    /// <summary>
    /// 6단계 — <see cref="LiveAssembler"/>. UnityEngine 이 없으므로 하니스에서 돈다.
    /// UI 없이 가짜 입력(여기서는 직접 메서드 호출)만으로 전부 검증된다는 것 자체가
    /// "UI 없이도 테스트 가능한 구조"의 증거다.
    /// </summary>
    public class LiveAssemblerTests
    {
        static MatchEngine NewEngine(bool kuzushiEnabled = false)
        {
            var cfg = new MatchRulesConfig { ThresholdJitter = 0, KuzushiGateEnabled = kuzushiEnabled };
            return new MatchEngine(cfg, RefereeProfile.Standard, new SystemRandomSource(1));
        }

        [Test]
        public void CoreAndLegOptions_AreAlwaysAllFour_RegardlessOfState()
        {
            var engine = NewEngine();
            var a = new LiveAssembler(engine, new MatchRulesConfig(), FighterId.A);

            CollectionAssert.AreEquivalent(
                new[] { CoreStance.Aggressive, CoreStance.Neutral, CoreStance.Defensive },
                a.CoreOptions());
            CollectionAssert.AreEquivalent(
                new[] { LegAction.Attack, LegAction.Neutral, LegAction.Defend, LegAction.CounterReady },
                a.LegOptions());
        }

        [Test]
        public void HandOptions_WithNothingHeld_IsExactlyTheBaseFive()
        {
            var engine = NewEngine();
            var cfg = new MatchRulesConfig();
            var a = new LiveAssembler(engine, cfg, FighterId.A);

            var options = a.HandOptions();

            CollectionAssert.AreEquivalent(
                new[] { HandTarget.RightSleeve, HandTarget.LeftSleeve, HandTarget.LeftLapel,
                        HandTarget.RightLapel, HandTarget.Evade },
                options);
        }

        [Test]
        public void HandOptions_AfterGrippingWithTheOtherHandFree_OpensConversionsAndMaintainGrip()
        {
            var engine = NewEngine();
            var cfg = new MatchRulesConfig();

            // A 가 오른소매를 잡는다 — 데코레이터 없이 엔진에 직접.
            engine.ResolveExchange(
                new TurnChoice(HandTarget.RightSleeve, CoreStance.Neutral, LegAction.Neutral),
                TurnChoice.Neutral, new ExchangeScript());

            var a = new LiveAssembler(engine, cfg, FighterId.A);
            var options = a.HandOptions();

            Assert.Contains(HandTarget.TwistGrip, options.ToList());
            Assert.Contains(HandTarget.BackGrab, options.ToList());
            Assert.Contains(HandTarget.MaintainGrip, options.ToList());
            Assert.IsFalse(options.Contains(HandTarget.Tear), "A 를 잡은 사람이 없으니 뜯을 것도 없다");
            Assert.IsFalse(options.Contains(HandTarget.Kuzushi), "플래그가 꺼져 있으면 그립이 있어도 안 열린다");
        }

        [Test]
        public void HandOptions_WhenOpponentHoldsMe_OpensTear()
        {
            var engine = NewEngine();
            var cfg = new MatchRulesConfig();

            // B 가 A 의 오른소매를 잡는다 → A 의 오른손이 Held.
            engine.ResolveExchange(TurnChoice.Neutral,
                new TurnChoice(HandTarget.RightSleeve, CoreStance.Neutral, LegAction.Neutral),
                new ExchangeScript());

            var a = new LiveAssembler(engine, cfg, FighterId.A);

            Assert.Contains(HandTarget.Tear, a.HandOptions().ToList());
        }

        [Test]
        public void HandOptions_WithGripAndFlagOn_OpensKuzushi()
        {
            var engine = NewEngine(kuzushiEnabled: true);
            var cfg = new MatchRulesConfig { KuzushiGateEnabled = true };

            engine.ResolveExchange(
                new TurnChoice(HandTarget.RightSleeve, CoreStance.Neutral, LegAction.Neutral),
                TurnChoice.Neutral, new ExchangeScript());

            var a = new LiveAssembler(engine, cfg, FighterId.A);

            Assert.Contains(HandTarget.Kuzushi, a.HandOptions().ToList());
        }

        [Test]
        public void Build_AssemblesExactlyTheGivenFields()
        {
            var engine = NewEngine();
            var a = new LiveAssembler(engine, new MatchRulesConfig(), FighterId.A);

            var choice = a.Build(HandTarget.LeftSleeve, CoreStance.Defensive, LegAction.CounterReady, Hand.Left);

            Assert.AreEqual(HandTarget.LeftSleeve, choice.Hand);
            Assert.AreEqual(CoreStance.Defensive, choice.Core);
            Assert.AreEqual(LegAction.CounterReady, choice.Legs);
            Assert.AreEqual(Hand.Left, choice.OwnHand);
        }
    }
}
