using System.Linq;
using NUnit.Framework;
using CounterTime.Core;

namespace CounterTime.Core.Tests
{
    /// <summary>
    /// §4.3 (v3.4) — 점수가 난 교환은 그쳐로 착지한다.
    ///
    /// 회복이 그쳐에 묶여 있는데 그쳐를 만드는 규칙이 지도와 위장뿐이라, 실측 경기의
    /// 그쳐가 0~2회였다. §5.3 의 +3 이 사실상 도달 불가라 체력이 30교환에 바닥났고,
    /// 그건 §2 가 상정한 60~80교환의 절반도 안 된다. 규칙 구멍이었다.
    /// </summary>
    public class MatteAfterScoreTests
    {
        MatchRulesConfig _cfg;
        MatchEngine _e;

        [SetUp]
        public void SetUp()
        {
            _cfg = new MatchRulesConfig { ThresholdJitter = 0 };
            _e = new MatchEngine(_cfg, RefereeProfile.Standard, new SystemRandomSource(1));
        }

        static readonly TurnChoice Idle = TurnChoice.Neutral;

        static ExchangeScript Throw(ReadLevel read, TechniqueClass cls = TechniqueClass.Standard) =>
            new ExchangeScript
            {
                Attacker = FighterId.A,
                Technique = new TechniqueSpec("밭다리", ActionCost.BigThrow, cls),
                Read = read,
            };

        /// A 가 소매를 잡아 잡기 우위를 만든다 — 그쳐가 이걸 푸는지 보려면 먼저 서야 한다.
        void GripUp()
        {
            _e.ResolveExchange(new TurnChoice(HandTarget.RightSleeve, CoreStance.Neutral, LegAction.Neutral),
                               Idle, new ExchangeScript());
        }

        [Test]
        public void ScoreCallsMatte_UnlessOsaekomiWouldFollow()
        {
            // 유효(sum 2~4)가 나는 교환.
            var ev = _e.ResolveExchange(Idle, Idle, Throw(ReadLevel.Read));

            var scored = ev.OfType<JudgementScored>().Single();
            Assert.AreEqual(Judgement.Yuko, scored.Judgement, "전제: 점수가 났다");
            Assert.AreEqual(1, ev.OfType<MatteCalled>().Count(), "점수가 나면 그쳐가 한 번 붙는다");
        }

        /// 노포인트는 상황을 리셋하지 않는다 — 그러면 매 교환이 그쳐가 된다.
        [Test]
        public void NoPointExchange_DoesNotCallMatte()
        {
            var ev = _e.ResolveExchange(Idle, Idle, Throw(ReadLevel.Neutral, TechniqueClass.Sloppy));

            Assert.AreEqual(Judgement.None, ev.OfType<JudgementScored>().Single().Judgement, "전제: 노포인트다");
            Assert.IsEmpty(ev.OfType<MatteCalled>(), "노포인트에 그쳐가 붙으면 안 된다");
        }

        [Test]
        public void MatteAfterScore_ResetsGripsToFree_AndBothStances()
        {
            GripUp();
            Assert.AreEqual(1, _e.Grip.GripScore(FighterId.A), "전제: A 가 잡고 있다");

            var ev = _e.ResolveExchange(Idle, Idle, Throw(ReadLevel.Read));

            Assert.IsNotEmpty(ev.OfType<MatteCalled>());
            Assert.AreEqual(0, _e.Grip.GripScore(FighterId.A), "그쳐가 잡기를 전부 푼다");
            Assert.AreEqual(GripLock.Free, _e.Grip.LockState(FighterId.A, Hand.Left));
            Assert.AreEqual(GripLock.Free, _e.Grip.LockState(FighterId.B, Hand.Right));

            // 자세는 Core 가 들지 않는다 — MatteCalled 를 받은 러너가 푼다. 그 계약을
            // 이벤트가 실제로 나간다는 것으로 고정한다 (MatchRunnerHandActionTests 가 뒷면).
            Assert.AreEqual(1, ev.OfType<MatteCalled>().Count());
        }

        /// <summary>
        /// 회복분만 따로 보려면 <b>득점 여부만 다르고 나머지는 같은</b> 두 교환을 견줘야
        /// 한다. 공격 비용과 수비 비용은 어차피 나가고, 수비 등급은 읽기로 정해지므로
        /// (§5.2) 읽기를 고정한 채 수비자세로 합만 노포인트로 떨어뜨린다.
        /// 두 델타의 차이가 곧 그쳐 회복이다 — 비용표를 테스트에 옮겨 적지 않는다.
        /// </summary>
        [Test]
        public void MatteAfterScore_GrantsTheStandardThreeStaminaEach()
        {
            // 상한에 먹히지 않도록 양쪽을 먼저 깎는다.
            void Drain(MatchEngine e)
            {
                for (int i = 0; i < 3; i++)
                    e.ResolveExchange(Idle, Idle, Throw(ReadLevel.Neutral, TechniqueClass.Sloppy));
            }

            // 읽기 +2, 기술 +1 → sum 3 → 유효. 점수가 나므로 그쳐.
            var scoring = new MatchEngine(_cfg, RefereeProfile.Standard, new SystemRandomSource(1));
            Drain(scoring);
            int scoringBefore = scoring.Stamina.Get(FighterId.B);
            var ev = scoring.ResolveExchange(Idle, Idle, Throw(ReadLevel.Read));
            int scoringDelta = scoring.Stamina.Get(FighterId.B) - scoringBefore;

            // 같은 읽기인데 B 가 수비 코어라 −2 가 붙어 sum 1 → 노포인트. 그쳐 없음.
            var quiet = new MatchEngine(_cfg, RefereeProfile.Standard, new SystemRandomSource(1));
            Drain(quiet);
            var defensiveB = new TurnChoice(HandTarget.None, CoreStance.Defensive, LegAction.Neutral);
            int quietBefore = quiet.Stamina.Get(FighterId.B);
            var quietEv = quiet.ResolveExchange(Idle, defensiveB, Throw(ReadLevel.Read));
            int quietDelta = quiet.Stamina.Get(FighterId.B) - quietBefore;

            Assert.IsNotEmpty(ev.OfType<MatteCalled>(), "전제: 득점 쪽은 그쳐가 붙는다");
            Assert.IsEmpty(quietEv.OfType<MatteCalled>(), "전제: 노포인트 쪽은 안 붙는다");

            Assert.AreEqual(_cfg.MatteRecovery, scoringDelta - quietDelta,
                "§5.3 그대로 +3 — 점수 그쳐라고 다른 값을 주지 않는다");
        }

        [Test]
        public void MatteAfterScore_StopsTheClock_ElapsedDoesNotAdvance()
        {
            var script = Throw(ReadLevel.Read);
            int before = _e.Clock.RemainingSeconds;

            _e.ResolveExchange(Idle, Idle, script);

            // §1 — 그쳐 동안 시계는 선다. 교환 자체의 비용만 흐르고 그쳐는 0 이다.
            Assert.AreEqual(before - script.DurationA, _e.Clock.RemainingSeconds,
                "그쳐가 시간을 더 태우면 안 된다 — 교환 비용만큼만 준다");
        }

        [Test]
        public void MatchEndingScore_DoesNotCallMatte()
        {
            // 완전간파 +3 + 시그니처 +2 = 5 → 절반. 두 번이면 합해서 한판이다.
            ExchangeScript WazaAri() => new ExchangeScript
            {
                Attacker = FighterId.A,
                Technique = new TechniqueSpec("밭다리", ActionCost.BigThrow, TechniqueClass.Signature),
                Read = ReadLevel.FullRead,
            };

            var first = _e.ResolveExchange(Idle, Idle, WazaAri());
            Assert.AreEqual(Judgement.WazaAri, first.OfType<JudgementScored>().Single().Judgement);
            Assert.IsFalse(_e.Board.IsFinished, "전제: 첫 절반으로는 안 끝난다");
            Assert.IsNotEmpty(first.OfType<MatteCalled>(), "끝나지 않은 득점에는 그쳐가 붙는다");

            var second = _e.ResolveExchange(Idle, Idle, WazaAri());
            Assert.IsTrue(_e.Board.IsFinished, "전제: 절반 둘이 합해져 경기가 끝났다");
            Assert.IsEmpty(second.OfType<MatteCalled>(), "다시 세울 것이 없으면 그쳐도 없다");
            Assert.IsNotNull(second.OfType<MatchEnded>().SingleOrDefault());
        }

        /// 심판이 이미 그쳐를 불렀으면 두 번 부르지 않는다 — 회복이 두 배가 된다.
        [Test]
        public void ScoreAndRefereeMatte_InTheSameExchange_YieldOnlyOneMatte()
        {
            var script = Throw(ReadLevel.Read);
            script.CallMatte = true;

            var ev = _e.ResolveExchange(Idle, Idle, script);

            Assert.AreEqual(1, ev.OfType<MatteCalled>().Count(),
                "그쳐가 둘이면 §5.3 회복이 +6 이 된다");
        }
    }
}
