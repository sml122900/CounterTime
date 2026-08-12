using System.Linq;
using NUnit.Framework;
using CounterTime.Core;

namespace CounterTime.Core.Tests
{
    /// <summary>
    /// §4.4/§S (v4.1) — 흔들기(setup) 선언과 밸런스 게이트. `BalanceGate` 기본 off, 이
    /// 표를 검증한다: off → 완전 비활성 / on → 그립 게이트, 성립 비용, 상대 밸런스
    /// 드레인, 상대 밸런스가 임계 미만이면 강등 회피(임계 이상이면 강등), 변칙기 면제.
    ///
    /// §H 3단계 — 그쳐로 "소멸"하던 옛 [초안] 동작(<c>_setupActive</c>, 정확히 1교환만
    /// 유지)은 밸런스가 지속되는 자원으로 바뀌면서 사라졌다. 그쳐는 이제 부분 회복만
    /// 준다 — 아래 <c>OneMatte_.../TwoMattes_...</c> 가 그 차이를 직접 고정한다.
    /// 순수 흔들기 선언 자체(그립 게이트·비용·이벤트)는 안 바뀌었다.
    /// </summary>
    public class SetupTests
    {
        MatchRulesConfig _cfg;
        MatchEngine _e;

        [SetUp]
        public void SetUp()
        {
            _cfg = new MatchRulesConfig { ThresholdJitter = 0, BalanceGate = true };
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
            var cfg = new MatchRulesConfig { ThresholdJitter = 0 };   // BalanceGate 기본 false
            var e = new MatchEngine(cfg, RefereeProfile.Standard, new SystemRandomSource(1));
            GripUp(e);

            var ev = e.ResolveExchange(SetupHand, Idle, new ExchangeScript());

            Assert.IsEmpty(ev.OfType<SetupDeclared>(), "플래그가 꺼져 있으면 흔들기는 성립하지 않는다");
            Assert.IsEmpty(ev.OfType<BalanceChanged>(), "성립 안 했으니 밸런스도 안 움직인다");
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
            Assert.IsEmpty(ev.OfType<BalanceChanged>(), "게이트를 못 넘었으니 밸런스도 안 움직인다");
            Assert.AreEqual(100, _e.Stamina.Get(FighterId.A), "게이트를 못 넘었으니 비용도 안 나간다");
        }

        [Test]
        public void FlagOn_WithGrip_SetupSucceeds_PaysSetupCost_DrainsOpponentBalance()
        {
            GripUp(_e);
            int before = _e.Stamina.Get(FighterId.A);

            var ev = _e.ResolveExchange(SetupHand, Idle, new ExchangeScript());

            var declared = ev.OfType<SetupDeclared>().SingleOrDefault();
            Assert.IsNotNull(declared, "그립이 있으면 흔들기가 성립해야 한다");
            Assert.AreEqual(FighterId.A, declared.Who);
            Assert.AreEqual(_cfg.SetupCost, before - _e.Stamina.Get(FighterId.A),
                "§5.1 표가 아니라 SetupCost 를 써야 한다 — Poke 비용(1)과 우연히 같아도 출처가 다르다");

            Assert.AreEqual(_cfg.BalanceMax - _cfg.BalanceDrainSetup, _e.Balance.Get(FighterId.B),
                "§S — 흔들기는 상대(B)의 밸런스를 깎는 전용 행동이다");
            Assert.AreEqual(_cfg.BalanceMax, _e.Balance.Get(FighterId.A),
                "선언한 쪽(A) 자신의 밸런스는 안 움직인다");
            var balanceEvent = ev.OfType<BalanceChanged>().SingleOrDefault();
            Assert.IsNotNull(balanceEvent);
            Assert.AreEqual(FighterId.B, balanceEvent.Who);
        }

        /// 흔들기 없이 낸 큰기술은 강등된다(상대 밸런스가 아직 임계 이상이므로). grip+1,
        /// 기술급만 다르게 해서 브래킷 경계를 넘나들게 한다 — Standard(1)면 sum=2→유효,
        /// Sloppy(0)면 sum=1→노포인트.
        [Test]
        public void BigThrow_AgainstFullBalanceOpponent_DowngradesToSloppyInJudgement()
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
        public void BigThrow_AgainstDrainedOpponent_KeepsOriginalTechniqueClass()
        {
            GripUp(_e);
            _e.ResolveExchange(SetupHand, Idle, new ExchangeScript());   // B: 100 -> 65 (< 70 임계)

            var ev = _e.ResolveExchange(Idle, Idle, Throw(TechniqueClass.Standard));

            var scored = ev.OfType<JudgementScored>().Single();
            Assert.AreEqual((int)TechniqueClass.Standard, scored.Terms[3],
                "흔들기로 상대 밸런스를 임계 밑으로 깎아뒀으면 강등되지 않는다");
            Assert.AreEqual(Judgement.Yuko, scored.Judgement,
                "sum = grip+1 + read0 + stam0 + Standard1 + posture0 = 2 → 유효");
        }

        /// §S(v4.1) — 밸런스는 지속되는 자원이다. 옛 [초안]의 <c>_setupActive</c> 는
        /// "선언한 바로 다음 교환에만" 유효했지만(정확히 1교환), 지금은 회복시키는 일이
        /// 없는 한 몇 교환이 지나도 드레인이 그대로 남는다.
        [Test]
        public void SetupDrain_PersistsAcrossMultipleExchanges_UnlikeTheOldOneExchangeFlag()
        {
            GripUp(_e);
            _e.ResolveExchange(SetupHand, Idle, new ExchangeScript());   // B: 100 -> 65
            _e.ResolveExchange(Idle, Idle, new ExchangeScript());        // 아무 일도 없다 — 회복 없음
            _e.ResolveExchange(Idle, Idle, new ExchangeScript());        // 마찬가지

            var ev = _e.ResolveExchange(Idle, Idle, Throw(TechniqueClass.Standard));

            Assert.AreEqual((int)TechniqueClass.Standard, ev.OfType<JudgementScored>().Single().Terms[3],
                "몇 교환이 지나도 아무것도 밸런스를 회복시키지 않았으면 강등 회피가 계속돼야 한다 — " +
                "옛 '1교환 만료' 규칙이라면 이 시점엔 이미 Sloppy 였을 것");
        }

        [Test]
        public void SurpriseExemptTechnique_NeverDowngraded_EvenAgainstFullBalanceOpponent()
        {
            GripUp(_e);

            var ev = _e.ResolveExchange(Idle, Idle, Throw(TechniqueClass.Standard, surpriseExempt: true));

            Assert.AreEqual((int)TechniqueClass.Standard, ev.OfType<JudgementScored>().Single().Terms[3],
                "말아업어치기 같은 변칙기는 상대 밸런스가 가득해도 강등되지 않는다 — 상대가 대비할 수 없는 게 존재 이유다");
        }

        /// §S(v4.1, RESOLVED §8) — 그쳐는 완전 리셋이 아니라 부분 회복(RecoverMattePartial)
        /// 만 준다. 한 번(+3)으로는 흔들기 드레인(35)을 못 넘어선다(65+3=68<70) — "그쳐가
        /// 흔들기를 지운다"던 옛 [초안] 동작과 다르다.
        [Test]
        public void OneMatte_DoesNotFullyUndoTheSetupDrain_KeepsOriginalTechniqueClass()
        {
            GripUp(_e);
            _e.ResolveExchange(SetupHand, Idle, new ExchangeScript());              // B: 100 -> 65
            _e.ResolveExchange(Idle, Idle, new ExchangeScript { CallMatte = true }); // B: 65 -> 68 (부분 회복)

            GripUp(_e);   // 그쳐가 잡기를 지웠으니 다시 잡아야 큰기술의 그립항을 맞출 수 있다
            var ev = _e.ResolveExchange(Idle, Idle, Throw(TechniqueClass.Standard));

            Assert.AreEqual((int)TechniqueClass.Standard, ev.OfType<JudgementScored>().Single().Terms[3],
                "한 번의 그쳐(+3)로는 흔들기 드레인(35)을 다 못 되돌린다 — 65+3=68 은 여전히 임계(70) 밑");
        }

        [Test]
        public void TwoMattes_RecoverEnoughToReopenTheGate()
        {
            GripUp(_e);
            _e.ResolveExchange(SetupHand, Idle, new ExchangeScript());              // B: 100 -> 65
            _e.ResolveExchange(Idle, Idle, new ExchangeScript { CallMatte = true }); // B: 65 -> 68
            _e.ResolveExchange(Idle, Idle, new ExchangeScript { CallMatte = true }); // B: 68 -> 71

            GripUp(_e);
            var ev = _e.ResolveExchange(Idle, Idle, Throw(TechniqueClass.Standard));

            Assert.AreEqual((int)TechniqueClass.Sloppy, ev.OfType<JudgementScored>().Single().Terms[3],
                "두 번째 그쳐로 68+3=71 이 임계(70)를 다시 넘으면 강등이 돌아온다 — 완전 리셋이 아니라 부분 회복의 누적");
        }
    }
}
