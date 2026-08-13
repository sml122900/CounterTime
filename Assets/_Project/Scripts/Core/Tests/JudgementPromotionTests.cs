using System.Linq;
using NUnit.Framework;
using CounterTime.Core;

namespace CounterTime.Core.Tests
{
    /// <summary>
    /// MATCH_RULES §E — deterministic floor + upward promotion dice, §H step 4.
    /// Two layers: <see cref="JudgementCalculator.Promote"/>'s own chain/probability
    /// math (pure, no engine needed), and <see cref="MatchEngine"/>'s wiring of it
    /// (gate default-off contract, floor-vs-promoted event split, |sum| symmetry for
    /// counters).
    /// </summary>
    public class JudgementPromotionTests
    {
        MatchRulesConfig _cfg;

        [SetUp]
        public void SetUp() => _cfg = new MatchRulesConfig();

        // ---- JudgementCalculator.Promote — pure chain/probability math ----

        [Test]
        public void Ippon_NeverPromotesFurther_EvenWithAGuaranteedSuccessRoll()
        {
            var result = JudgementCalculator.Promote(Judgement.Ippon, effectiveSum: 100, _cfg,
                new FixedRandomSource(f: 0f)); // would always succeed if it rolled at all

            Assert.AreEqual(Judgement.Ippon, result, "이미 최상단 브래킷 — 위가 없다");
        }

        /// Yuko 브래킷 하한(effectiveSum=2, BracketFloor(Yuko)=NoPointMax+1=2, diff=0)
        /// ⇒ p = PromoteBase(0.10) 그대로. §D.4 예시("floor Yuko → 적은 확률로 승격")와
        /// 같은 지점.
        [Test]
        public void AtBracketFloor_RollExactlyAtP_Fails()
        {
            var result = JudgementCalculator.Promote(Judgement.Yuko, effectiveSum: 2, _cfg,
                new FixedRandomSource(f: 0.10f)); // f < p 여야 성공 — 경계값(f==p)은 실패

            Assert.AreEqual(Judgement.Yuko, result,
                "f == p 는 실패다 — RefereeSystem.JudgeFeint 등 기존 관례(f < p 성공)와 동일한 경계");
        }

        [Test]
        public void AtBracketFloor_RollJustBelowP_Succeeds()
        {
            var result = JudgementCalculator.Promote(Judgement.Yuko, effectiveSum: 2, _cfg,
                new FixedRandomSource(f: 0.09f));

            Assert.AreEqual(Judgement.WazaAri, result);
        }

        /// effectiveSum=5 는 WazaAri 의 하한과 같다 — Yuko 기준으론 diff=3(p1=0.25),
        /// 한 단 올라간 WazaAri 기준으론 diff=0(p2_base=0.10, ×PromoteChainFactor
        /// 0.25 = 0.025). 세 개의 f 값으로 체인 길이 0/1/2 전부 고정한다.
        [Test]
        public void Chain_NoPromotion_WhenFirstRollMisses()
        {
            var result = JudgementCalculator.Promote(Judgement.Yuko, effectiveSum: 5, _cfg,
                new FixedRandomSource(f: 0.30f)); // p1=0.25 미만이어야 성공 — 0.30 은 실패

            Assert.AreEqual(Judgement.Yuko, result);
        }

        [Test]
        public void Chain_SinglePromotion_WhenFirstHitsSecondMisses()
        {
            var result = JudgementCalculator.Promote(Judgement.Yuko, effectiveSum: 5, _cfg,
                new FixedRandomSource(f: 0.03f)); // p1=0.25 통과, p2=0.025 미달

            Assert.AreEqual(Judgement.WazaAri, result);
        }

        [Test]
        public void Chain_DoublePromotion_WhenBothRollsHit()
        {
            var result = JudgementCalculator.Promote(Judgement.Yuko, effectiveSum: 5, _cfg,
                new FixedRandomSource(f: 0.02f)); // p1=0.25, p2=0.025 둘 다 통과

            Assert.AreEqual(Judgement.Ippon, result, "Yuko → WazaAri → Ippon, 연쇄 2단");
        }

        [Test]
        public void Chain_CappedAtPromoteMaxChain_EvenWithGuaranteedRolls()
        {
            // PromoteBase 를 1로 올려 diff 부호와 무관하게 매 링크가 반드시 성공하도록
            // 만든 뒤, None 에서 시작해 상한(기본 2)을 넘어 Ippon 까지 못 가는지 확인.
            var cfg = new MatchRulesConfig { PromoteBase = 1f, PromoteMaxChain = 2 };
            var result = JudgementCalculator.Promote(Judgement.None, effectiveSum: 1, cfg,
                new FixedRandomSource(f: 0f));

            Assert.AreEqual(Judgement.WazaAri, result,
                "None → Yuko → WazaAri 딱 2단, Ippon 까지는 세 번째 링크가 필요한데 상한이 막는다");
        }

        /// §E.1.4 — "None 결과도 승격할 수 있다, realAttempt 한정" ("럭키펀치"). 이
        /// 메서드 레벨에서는 realAttempt 여부를 별도 인자로 안 받는다 — 유일한 호출부인
        /// MatchEngine.ResolveTechnique 가 이미 견제(poke)는 Evaluate 자체를 안 태워서
        /// 구조적으로 항상 realAttempt 다(주석 참조). 여기서는 순수하게 None 도
        /// 메커니즘상 승격 가능함만 고정한다.
        [Test]
        public void NoneFloor_CanPromote()
        {
            var result = JudgementCalculator.Promote(Judgement.None, effectiveSum: 1, _cfg,
                new FixedRandomSource(f: 0f));

            Assert.AreNotEqual(Judgement.None, result);
        }

        // ---- MatchEngine wiring — gate contract + counter symmetry ----

        static ExchangeScript SloppyThrow(FighterId attacker, ReadLevel read) => new ExchangeScript
        {
            Attacker = attacker,
            Technique = new TechniqueSpec("밭다리", ActionCost.BigThrow, TechniqueClass.Sloppy),
            Read = read,
        };

        [Test]
        public void PromotionGate_Off_LeavesTheFloorUntouched_EvenWithAGuaranteedSuccessRng()
        {
            // 기본 cfg — PromotionGate 는 기본 false. grip0+read Neutral(0)+stam0+
            // Sloppy(0)+자세0 = 0 → None. f=0 은 게이트가 켜져 있었다면 반드시 승격했을
            // 값이다 — 꺼져 있으면 Promote 자체가 안 불린다는 걸 이 rng 선택으로 증명한다.
            var e = new MatchEngine(_cfg, RefereeProfile.Standard, new FixedRandomSource(f: 0f));

            var ev = e.ResolveExchange(TurnChoice.Neutral, TurnChoice.Neutral,
                SloppyThrow(FighterId.A, ReadLevel.Neutral));

            var scored = ev.OfType<JudgementScored>().Single();
            Assert.AreEqual(Judgement.None, scored.Judgement);
            Assert.IsEmpty(ev.OfType<JudgementPromoted>(),
                "PromotionGate 기본 off — 42노드 지문 보존 계약(BalanceGate 와 동일 패턴)");
        }

        [Test]
        public void PromotionGate_On_PromotesTheFloor_AndEmitsTheFloorSeparately()
        {
            var cfg = new MatchRulesConfig { PromotionGate = true };
            var e = new MatchEngine(cfg, RefereeProfile.Standard, new FixedRandomSource(f: 0f));

            // grip0+read Neutral(0)+stam0+Sloppy(0)+자세0 = 0 → None floor.
            // BracketFloor(None)=CounterThreshold+1=-2, diff=0-(-2)=2, p1=0.10+0.05*2=0.20.
            // f=0 < 0.20 이므로 반드시 승격.
            var ev = e.ResolveExchange(TurnChoice.Neutral, TurnChoice.Neutral,
                SloppyThrow(FighterId.A, ReadLevel.Neutral));

            var scored = ev.OfType<JudgementScored>().Single();
            var promoted = ev.OfType<JudgementPromoted>().Single();

            Assert.AreNotEqual(Judgement.None, scored.Judgement,
                "판정식 결과(JudgementScored.Judgement)는 승격 후 최종값이어야 실제로 득점된다");
            Assert.AreEqual(Judgement.None, promoted.Floor);
            Assert.AreEqual(scored.Judgement, promoted.Promoted, "이벤트의 Promoted 와 최종 득점이 같아야 한다");
            Assert.AreEqual(FighterId.A, scored.ScoredBy);
            Assert.AreEqual(FighterId.A, promoted.ScoredBy);
        }

        [Test]
        public void PromotionGate_On_ButRollMisses_LeavesFloorUnchanged_NoEvent()
        {
            var cfg = new MatchRulesConfig { PromotionGate = true };
            var e = new MatchEngine(cfg, RefereeProfile.Standard, new FixedRandomSource(f: 0.99f));

            var ev = e.ResolveExchange(TurnChoice.Neutral, TurnChoice.Neutral,
                SloppyThrow(FighterId.A, ReadLevel.Neutral));

            Assert.AreEqual(Judgement.None, ev.OfType<JudgementScored>().Single().Judgement);
            Assert.IsEmpty(ev.OfType<JudgementPromoted>(), "실패한 롤은 이벤트를 안 남긴다");
        }

        /// §E.1.3 — "카운터에도 |합| 기준 대칭 적용." 되치기는 JudgementResult.Sum 이
        /// 원래 부호(음수)를 그대로 들고 있으므로, MatchEngine 이 Promote 에 넘기는
        /// effectiveSum 이 부호를 안 뒤집으면(버그) 이 시나리오의 diff 가 크게 음수로
        /// 나와 절대 승격하지 않는다 — 이 테스트는 그 배선 실수를 정확히 잡아낸다.
        ///
        /// 잡기 없이도 sum=-3 이 나온다: grip0 + GotRead(-3, Neutral 선언이 카운터
        /// 준비로 한 단 떨어짐) + stam0 + Sloppy(0) + 자세0(카운터 준비 예외) = -3 →
        /// 되치기, |3| → Bracket(3)=Yuko, 득점자 B. MATCH_RULES §4.1 검증 #4/#5 의 grip
        /// 항만 뺀 변형.
        [Test]
        public void PromotionGate_On_CounterUsesAbsoluteSum_NotRawNegativeSum()
        {
            var cfg = new MatchRulesConfig { PromotionGate = true };
            var e = new MatchEngine(cfg, RefereeProfile.Standard, new FixedRandomSource(f: 0.1f));

            var ev = e.ResolveExchange(
                TurnChoice.Neutral,
                new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.CounterReady),
                SloppyThrow(FighterId.A, ReadLevel.Neutral));

            var scored = ev.OfType<JudgementScored>().Single();
            Assert.AreEqual(-3, scored.Sum, "전제 확인 — 원래 부호 그대로인 판정식 합");
            Assert.IsTrue(scored.IsCounter);
            Assert.AreEqual(FighterId.B, scored.ScoredBy);

            // effectiveSum=|−3|=3, BracketFloor(Yuko)=2, diff=1, p1=0.10+0.05=0.15.
            // f=0.1 < 0.15 → 승격. (부호를 안 뒤집었다면 diff=-3-2=-5 → p 음수 클램프
            // 0 → f=0.1 이 0 을 못 이겨 승격 실패, Yuko 그대로 남았을 것이다.)
            Assert.AreEqual(Judgement.WazaAri, scored.Judgement,
                "effectiveSum 이 |sum| 이어야 이 확률로 승격한다 — 부호 버그면 절대 승격 못 한다");
            var promoted = ev.OfType<JudgementPromoted>().Single();
            Assert.AreEqual(Judgement.Yuko, promoted.Floor);
            Assert.AreEqual(Judgement.WazaAri, promoted.Promoted);
            Assert.AreEqual(FighterId.B, promoted.ScoredBy, "승격 이벤트의 주체도 득점자(되치기 성공자) 기준");
        }
    }
}
