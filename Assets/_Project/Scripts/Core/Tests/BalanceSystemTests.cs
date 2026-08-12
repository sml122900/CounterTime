using NUnit.Framework;
using CounterTime.Core;

namespace CounterTime.Core.Tests
{
    /// <summary>§S/§H 3단계 — <see cref="BalanceSystem"/> 단독 단위 테스트. StaminaSystem
    /// 미러 — 같은 모양의 계약(하한 0, 상한 BalanceMax, 완전 리셋은 별도 메서드)만
    /// 검증한다. 게이트·드레인 호출부의 통합 동작은 <c>BalanceDrainTests</c>/
    /// <c>SetupTests</c> 쪽.</summary>
    public class BalanceSystemTests
    {
        MatchRulesConfig _cfg;
        BalanceSystem _b;

        [SetUp]
        public void SetUp()
        {
            _cfg = new MatchRulesConfig();
            _b = new BalanceSystem(_cfg);
        }

        [Test]
        public void StartsAtBalanceMax_ForBothFighters()
        {
            Assert.AreEqual(_cfg.BalanceMax, _b.Get(FighterId.A));
            Assert.AreEqual(_cfg.BalanceMax, _b.Get(FighterId.B));
        }

        [Test]
        public void Drain_ReducesOnlyTheNamedFighter()
        {
            _b.Drain(FighterId.A, 30);
            Assert.AreEqual(_cfg.BalanceMax - 30, _b.Get(FighterId.A));
            Assert.AreEqual(_cfg.BalanceMax, _b.Get(FighterId.B), "B 는 안 건드렸으니 그대로");
        }

        [Test]
        public void Drain_FloorsAtZero()
        {
            _b.Drain(FighterId.A, _cfg.BalanceMax + 50);
            Assert.AreEqual(0, _b.Get(FighterId.A));
        }

        [Test]
        public void RecoverDefenseSuccess_AddsTheConfiguredAmount()
        {
            _b.Drain(FighterId.A, 50);
            _b.RecoverDefenseSuccess(FighterId.A);
            Assert.AreEqual(_cfg.BalanceMax - 50 + _cfg.BalanceRecoverDefense, _b.Get(FighterId.A));
        }

        [Test]
        public void RecoverMattePartial_AddsTheConfiguredAmount()
        {
            _b.Drain(FighterId.A, 50);
            _b.RecoverMattePartial(FighterId.A);
            Assert.AreEqual(_cfg.BalanceMax - 50 + _cfg.BalanceRecoverMatte, _b.Get(FighterId.A));
        }

        [Test]
        public void Recovery_CapsAtBalanceMax_NeverOvershoots()
        {
            _b.Drain(FighterId.A, 2);
            _b.RecoverDefenseSuccess(FighterId.A);   // +10 by default, would overshoot 100 by 8
            Assert.AreEqual(_cfg.BalanceMax, _b.Get(FighterId.A), "상한을 넘어가면 안 된다");
        }

        [Test]
        public void ResetBetweenMatches_IsTheOnlyFullReset()
        {
            _b.Drain(FighterId.A, 90);
            _b.RecoverMattePartial(FighterId.A);   // partial only — proves it's NOT this method
            Assert.Less(_b.Get(FighterId.A), _cfg.BalanceMax, "부분 회복은 완전 리셋이 아니다");

            _b.ResetBetweenMatches(FighterId.A);
            Assert.AreEqual(_cfg.BalanceMax, _b.Get(FighterId.A), "이 메서드만 완전 리셋한다");
        }
    }
}
