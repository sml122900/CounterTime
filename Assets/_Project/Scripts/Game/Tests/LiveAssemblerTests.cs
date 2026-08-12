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
        static MatchEngine NewEngine(bool setupEnabled = false)
        {
            var cfg = new MatchRulesConfig { ThresholdJitter = 0, BalanceGate = setupEnabled };
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

            var options = a.HandOptions(Hand.Right);

            CollectionAssert.AreEquivalent(
                new[] { HandTarget.RightSleeve, HandTarget.LeftSleeve, HandTarget.LeftLapel,
                        HandTarget.RightLapel, HandTarget.Evade },
                options);
        }

        [Test]
        public void HandOptions_AfterGrippingWithTheOtherHand_OpensConversionsAndMaintainGrip()
        {
            var engine = NewEngine();
            var cfg = new MatchRulesConfig();

            // A 의 왼손으로 오른소매를 잡는다 — 전환을 수행할 손(LiveAssembler 가 확인하는
            // 오른손, Build() 기본값)은 아직 아무것도 안 잡고 있다. §2 는 "다른 손"이 이미
            // 잡고 있을 때만 전환을 허용한다 — CanUseConversion 버그 수정(§H 0단계) 반영.
            engine.ResolveExchange(
                new TurnChoice(HandTarget.RightSleeve, CoreStance.Neutral, LegAction.Neutral, Hand.Left),
                TurnChoice.Neutral, new ExchangeScript());

            var a = new LiveAssembler(engine, cfg, FighterId.A);
            var options = a.HandOptions(Hand.Right);

            Assert.Contains(HandTarget.TwistGrip, options.ToList());
            Assert.Contains(HandTarget.BackGrab, options.ToList());
            Assert.Contains(HandTarget.MaintainGrip, options.ToList());
            Assert.IsFalse(options.Contains(HandTarget.Tear), "A 를 잡은 사람이 없으니 뜯을 것도 없다");
            Assert.IsFalse(options.Contains(HandTarget.Setup), "플래그가 꺼져 있으면 그립이 있어도 안 열린다");
        }

        [Test]
        public void HandOptions_SameHandHoldingOnly_DoesNotOpenConversions()
        {
            var engine = NewEngine();
            var cfg = new MatchRulesConfig();

            // A 의 오른손으로 오른소매를 잡는다 — 오른손 스스로 잡은 그립은 §2 의
            // "다른 손" 조건을 못 채운다. 고치기 전에는 이 상태에서도 전환이 열렸다
            // (반대 손 검사 없이 "그립이 하나라도 있는가"만 봤기 때문).
            engine.ResolveExchange(
                new TurnChoice(HandTarget.RightSleeve, CoreStance.Neutral, LegAction.Neutral, Hand.Right),
                TurnChoice.Neutral, new ExchangeScript());

            var a = new LiveAssembler(engine, cfg, FighterId.A);
            var options = a.HandOptions(Hand.Right);

            Assert.IsFalse(options.Contains(HandTarget.TwistGrip), "같은 손의 그립으로는 전환 불가");
            Assert.IsFalse(options.Contains(HandTarget.BackGrab));
            Assert.Contains(HandTarget.MaintainGrip, options.ToList(), "그립 자체는 여전히 유지 가능");
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

            Assert.Contains(HandTarget.Tear, a.HandOptions(Hand.Right).ToList());
        }

        [Test]
        public void HandOptions_TearOnlyOffered_ForTheSpecificHeldHand()
        {
            var engine = NewEngine();
            var cfg = new MatchRulesConfig();

            // B 가 A 의 오른소매를 잡는다 → A 의 오른손만 Held, 왼손은 여전히 Free —
            // §A.1 손 분리 후 "뜯기"는 "아무 손이나 잡혔으면"이 아니라 "이 손이
            // 잡혔으면"이어야 한다.
            engine.ResolveExchange(TurnChoice.Neutral,
                new TurnChoice(HandTarget.RightSleeve, CoreStance.Neutral, LegAction.Neutral),
                new ExchangeScript());

            var a = new LiveAssembler(engine, cfg, FighterId.A);

            Assert.Contains(HandTarget.Tear, a.HandOptions(Hand.Right).ToList(),
                "오른손이 잡혔으니 오른손으로는 뜯을 수 있다");
            Assert.IsFalse(a.HandOptions(Hand.Left).Contains(HandTarget.Tear),
                "왼손은 안 잡혔으니 왼손으로 뜯을 게 없다");
        }

        [Test]
        public void HandOptions_WithGripAndFlagOn_OpensSetup()
        {
            var engine = NewEngine(setupEnabled: true);
            var cfg = new MatchRulesConfig { BalanceGate = true };

            engine.ResolveExchange(
                new TurnChoice(HandTarget.RightSleeve, CoreStance.Neutral, LegAction.Neutral),
                TurnChoice.Neutral, new ExchangeScript());

            var a = new LiveAssembler(engine, cfg, FighterId.A);

            Assert.Contains(HandTarget.Setup, a.HandOptions(Hand.Right).ToList());
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
