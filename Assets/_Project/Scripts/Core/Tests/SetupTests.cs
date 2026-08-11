using System.Linq;
using NUnit.Framework;
using CounterTime.Core;

namespace CounterTime.Core.Tests
{
    /// <summary>
    /// §4.4 [초안] — 흔들기(setup). `SetupGateEnabled` 기본 off, 이 표를 검증한다:
    /// off → 완전 비활성 / on → 그립 게이트, 성립 비용, 1교환 지속, 미선행 강등,
    /// 변칙기 면제, 그쳐로 소멸.
    /// </summary>
    public class SetupTests
    {
        MatchRulesConfig _cfg;
        MatchEngine _e;

        [SetUp]
        public void SetUp()
        {
            _cfg = new MatchRulesConfig { ThresholdJitter = 0, SetupGateEnabled = true };
            _e = new MatchEngine(_cfg, RefereeProfile.Standard, new SystemRandomSource(1));
        }

        static readonly TurnChoice Idle = TurnChoice.Neutral;
        static readonly TurnChoice SetupHand =
            new TurnChoice(HandTarget.Setup, CoreStance.Neutral, LegAction.Neutral);

        static ExchangeScript Throw(TechniqueClass cls, bool surpriseExempt = false) =>
            new ExchangeScript
            {
                Attacker = FighterId.A,
                Technique = new TechniqueSpec("밭다리", ActionCost.BigThrow, cls,
                    surpriseExempt: surpriseExempt),
                Read = ReadLevel.Neutral,
            };

        /// A 가 오른소매를 잡아 grip+1 을 만든다 — 흔들기 게이트와 판정식의 그립항 양쪽에 쓴다.
        void GripUp(MatchEngine e) =>
            e.ResolveExchange(new TurnChoice(HandTarget.RightSleeve, CoreStance.Neutral, LegAction.Neutral),
                              Idle, new ExchangeScript());

        [Test]
        public void FlagOff_SetupHandAction_IsFullyInert()
        {
            var cfg = new MatchRulesConfig { ThresholdJitter = 0 };   // SetupGateEnabled 기본 false
            var e = new MatchEngine(cfg, RefereeProfile.Standard, new SystemRandomSource(1));
            GripUp(e);

            var ev = e.ResolveExchange(SetupHand, Idle, new ExchangeScript());

            Assert.IsEmpty(ev.OfType<SetupDeclared>(), "플래그가 꺼져 있으면 흔들기는 성립하지 않는다");
            Assert.AreEqual(100, e.Stamina.Get(FighterId.A), "성립 안 했으니 비용도 없다");

            var thrown = e.ResolveExchange(Idle, Idle, Throw(TechniqueClass.Standard));
            Assert.AreEqual((int)TechniqueClass.Standard, thrown.OfType<JudgementScored>().Single().Terms[3],
                "플래그가 꺼져 있으면 강등 로직 자체가 돌지 않는다 — 기존 대조 모드와 동일해야 한다");
        }

        [Test]
        public void FlagOn_WithoutAnyGrip_SetupIsNoOp()
        {
            var ev = _e.ResolveExchange(SetupHand, Idle, new ExchangeScript());

            Assert.IsEmpty(ev.OfType<SetupDeclared>(), "그립이 하나도 없으면 흔들기를 선언할 수 없다");
            Assert.AreEqual(100, _e.Stamina.Get(FighterId.A), "게이트를 못 넘었으니 비용도 안 나간다");
        }

        [Test]
        public void FlagOn_WithGrip_SetupSucceeds_PaysSetupCost_EmitsEvent()
        {
            GripUp(_e);
            int before = _e.Stamina.Get(FighterId.A);

            var ev = _e.ResolveExchange(SetupHand, Idle, new ExchangeScript());

            var declared = ev.OfType<SetupDeclared>().SingleOrDefault();
            Assert.IsNotNull(declared, "그립이 있으면 흔들기가 성립해야 한다");
            Assert.AreEqual(FighterId.A, declared.Who);
            Assert.AreEqual(_cfg.SetupCost, before - _e.Stamina.Get(FighterId.A),
                "§5.1 표가 아니라 SetupCost 를 써야 한다 — Poke 비용(1)과 우연히 같아도 출처가 다르다");
        }

        /// 흔들기 없이 낸 큰기술은 강등된다. grip+1, 기술급만 다르게 해서 브래킷 경계를
        /// 넘나들게 한다 — Standard(1)면 sum=2→유효, Sloppy(0)면 sum=1→노포인트.
        [Test]
        public void BigThrow_WithoutPrecedingSetup_DowngradesToSloppyInJudgement()
        {
            GripUp(_e);

            var ev = _e.ResolveExchange(Idle, Idle, Throw(TechniqueClass.Standard));

            var scored = ev.OfType<JudgementScored>().Single();
            Assert.AreEqual((int)TechniqueClass.Sloppy, scored.Terms[3],
                "판정식에 들어간 기술급 항이 실제로 Sloppy 로 강등돼 있어야 한다");
            Assert.AreEqual(Judgement.None, scored.Judgement,
                "sum = grip+1 + read0 + stam0 + Sloppy0 + posture0 = 1 → 노포인트");
            // 틀린 답: 강등이 없었다면 Standard 그대로라 sum=2 → 유효였을 것.
            Assert.AreNotEqual(Judgement.Yuko, scored.Judgement);
        }

        [Test]
        public void BigThrow_PrecededBySetup_KeepsOriginalTechniqueClass()
        {
            GripUp(_e);
            _e.ResolveExchange(SetupHand, Idle, new ExchangeScript());

            var ev = _e.ResolveExchange(Idle, Idle, Throw(TechniqueClass.Standard));

            var scored = ev.OfType<JudgementScored>().Single();
            Assert.AreEqual((int)TechniqueClass.Standard, scored.Terms[3],
                "흔들기를 선행했으면 강등되지 않는다");
            Assert.AreEqual(Judgement.Yuko, scored.Judgement,
                "sum = grip+1 + read0 + stam0 + Standard1 + posture0 = 2 → 유효");
        }

        /// 지속은 정확히 1교환이다 — 흔들기 다음다음 교환에서 낸 큰기술은 이미 강등된다.
        [Test]
        public void SetupState_ExpiresAfterExactlyOneExchange()
        {
            GripUp(_e);
            _e.ResolveExchange(SetupHand, Idle, new ExchangeScript());
            _e.ResolveExchange(Idle, Idle, new ExchangeScript());   // 아무 일도 없는 교환 하나 소모

            var ev = _e.ResolveExchange(Idle, Idle, Throw(TechniqueClass.Standard));

            Assert.AreEqual((int)TechniqueClass.Sloppy, ev.OfType<JudgementScored>().Single().Terms[3],
                "흔들기는 선언한 바로 다음 교환에만 유효하다 — 그 다음은 이미 소멸했어야 한다");
        }

        [Test]
        public void SurpriseExemptTechnique_NeverDowngraded_EvenWithoutSetup()
        {
            GripUp(_e);

            var ev = _e.ResolveExchange(Idle, Idle, Throw(TechniqueClass.Standard, surpriseExempt: true));

            Assert.AreEqual((int)TechniqueClass.Standard, ev.OfType<JudgementScored>().Single().Terms[3],
                "말아업어치기 같은 변칙기는 흔들기 없이도 강등되지 않는다 — 상대가 대비할 수 없는 게 존재 이유다");
        }

        [Test]
        public void SetupState_ClearedWhenMatteIsCalled()
        {
            GripUp(_e);
            _e.ResolveExchange(SetupHand, Idle, new ExchangeScript());

            // 실제 판정과 무관하게 그쳐만 강제로 부른다 — 상황 리셋이 흔들기도 지우는지만 본다.
            _e.ResolveExchange(Idle, Idle, new ExchangeScript { CallMatte = true });

            GripUp(_e);   // 그쳐가 잡기도 지웠으니 다시 잡아야 큰기술의 그립항을 맞출 수 있다
            var ev = _e.ResolveExchange(Idle, Idle, Throw(TechniqueClass.Standard));

            Assert.AreEqual((int)TechniqueClass.Sloppy, ev.OfType<JudgementScored>().Single().Terms[3],
                "그쳐가 국면을 리셋했으면 흔들기 상태도 함께 소멸했어야 한다");
        }
    }
}
