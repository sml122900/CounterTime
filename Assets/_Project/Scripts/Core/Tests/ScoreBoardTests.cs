using NUnit.Framework;
using CounterTime.Core;

namespace CounterTime.Core.Tests
{
    public class ScoreBoardTests
    {
        MatchRulesConfig _cfg;
        ScoreBoard _b;

        [SetUp]
        public void SetUp()
        {
            _cfg = new MatchRulesConfig();
            _b = new ScoreBoard(_cfg);
        }

        // ---- §1 yuko never promotes ----

        [Test]
        public void FiveYuko_LoseToOneWazaAri()
        {
            for (int i = 0; i < 5; i++) _b.Award(FighterId.A, Judgement.Yuko);
            _b.Award(FighterId.B, Judgement.WazaAri);
            Assert.AreEqual(5, _b.Yuko(FighterId.A));
            Assert.IsFalse(_b.HasIppon(FighterId.A), "yuko never promotes");
            Assert.AreEqual(FighterId.B, _b.WinnerAtTimeUp());
        }

        [Test]
        public void ManyYuko_NeverBecomeWazaAri()
        {
            for (int i = 0; i < 10; i++) _b.Award(FighterId.A, Judgement.Yuko);
            Assert.AreEqual(0, _b.WazaAri(FighterId.A));
            Assert.IsFalse(_b.IsFinished);
        }

        // ---- §1 awasete-ippon ----

        [Test]
        public void TwoWazaAri_MakeIppon_AndEndTheMatch()
        {
            _b.Award(FighterId.A, Judgement.WazaAri);
            Assert.IsFalse(_b.IsFinished);
            _b.Award(FighterId.A, Judgement.WazaAri);
            Assert.IsTrue(_b.HasIppon(FighterId.A));
            Assert.IsTrue(_b.IsFinished);
            Assert.AreEqual(FighterId.A, _b.Winner);
        }

        [Test]
        public void WazaAri_FromEachFighter_DoesNotCombine()
        {
            _b.Award(FighterId.A, Judgement.WazaAri);
            _b.Award(FighterId.B, Judgement.WazaAri);
            Assert.IsFalse(_b.IsFinished);
            Assert.IsNull(_b.WinnerAtTimeUp(), "1-1 on waza-ari with no yuko ⇒ golden score");
        }

        [Test]
        public void Ippon_EndsImmediately()
        {
            _b.Award(FighterId.A, Judgement.Ippon);
            Assert.IsTrue(_b.IsFinished);
            Assert.AreEqual(FighterId.A, _b.Winner);
        }

        [Test]
        public void ScoresAfterTheEnd_AreIgnored()
        {
            _b.Award(FighterId.A, Judgement.Ippon);
            _b.Award(FighterId.B, Judgement.WazaAri);
            Assert.AreEqual(0, _b.WazaAri(FighterId.B));
            Assert.AreEqual(FighterId.A, _b.Winner);
        }

        // ---- §1 shido ----

        [Test]
        public void ThreeShido_IsHansokuMake()
        {
            _b.AddShido(FighterId.A);
            _b.AddShido(FighterId.A);
            Assert.IsFalse(_b.IsFinished, "two shido is not yet a loss");
            _b.AddShido(FighterId.A);
            Assert.IsTrue(_b.IsFinished);
            Assert.AreEqual(FighterId.B, _b.Winner, "the penalised fighter loses");
        }

        [Test]
        public void Shido_AwardsNoPoints()
        {
            _b.AddShido(FighterId.A);
            _b.AddShido(FighterId.A);
            Assert.AreEqual(0, _b.Yuko(FighterId.B));
            Assert.AreEqual(0, _b.WazaAri(FighterId.B));
            Assert.IsNull(_b.WinnerAtTimeUp(), "shido alone decides nothing before the third");
        }

        // ---- §1 time-up priority ----

        [Test]
        public void TimeUp_PrefersWazaAriOverYukoCount()
        {
            _b.Award(FighterId.A, Judgement.Yuko);
            _b.Award(FighterId.A, Judgement.Yuko);
            _b.Award(FighterId.B, Judgement.WazaAri);
            Assert.AreEqual(FighterId.B, _b.WinnerAtTimeUp());
        }

        [Test]
        public void TimeUp_FallsThroughToYukoCount()
        {
            _b.Award(FighterId.A, Judgement.Yuko);
            Assert.AreEqual(FighterId.A, _b.WinnerAtTimeUp());
        }

        [Test]
        public void TimeUp_DeadEven_GoesToGoldenScore()
        {
            Assert.IsNull(_b.WinnerAtTimeUp());
        }

        // ---- §1 golden score ----

        [Test]
        public void GoldenScore_AnyYukoEndsIt()
        {
            _b.Award(FighterId.A, Judgement.Yuko);
            _b.Award(FighterId.B, Judgement.Yuko);
            Assert.IsFalse(_b.IsFinished);

            _b.EnterGoldenScore();
            _b.Award(FighterId.B, Judgement.Yuko);
            Assert.IsTrue(_b.IsFinished);
            Assert.AreEqual(FighterId.B, _b.Winner);
        }

        [Test]
        public void GoldenScore_ThirdShidoStillLoses()
        {
            _b.EnterGoldenScore();
            _b.AddShido(FighterId.A);
            _b.AddShido(FighterId.A);
            _b.AddShido(FighterId.A);
            Assert.AreEqual(FighterId.B, _b.Winner);
        }
    }
}
