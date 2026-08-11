using System.Linq;
using NUnit.Framework;
using CounterTime.Core;

namespace CounterTime.Core.Tests
{
    /// <summary>
    /// §1 — regulation expiry never reached MatchEngine before this: neither
    /// <see cref="MatchClock.RegulationExpired"/> nor <see cref="ScoreBoard.WinnerAtTimeUp"/>
    /// had a caller outside their own isolated unit tests. Both pieces were already
    /// correct in isolation; this pins the wiring — <see cref="MatchEngine.ResolveExchange"/>
    /// now checks time-up AFTER each exchange fully resolves, decides a winner on a
    /// clear lead, and enters golden score on a full tie.
    /// </summary>
    public class GoldenScoreTests
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

        /// 심판 소극 누적을 안 건드리고 시계만 태우는 패딩 교환 — DeclaredA/B=false 라
        /// ObserveTurn 자체가 안 불린다(§6.1 "화면 밖" 계약). 기술도 없어 점수도 안 난다.
        static ExchangeScript Pad() => new ExchangeScript { DeclaredA = false, DeclaredB = false };

        void PadTo(int seconds)
        {
            while (_e.Clock.ElapsedSeconds < seconds)
                _e.ResolveExchange(Idle, Idle, Pad());
        }

        static ExchangeScript Throw(ReadLevel read, TechniqueClass cls = TechniqueClass.Standard) =>
            new ExchangeScript
            {
                Attacker = FighterId.A,
                Technique = new TechniqueSpec("밭다리", ActionCost.BigThrow, cls),
                Read = read,
            };

        // ---- §1 — dead-even at time-up enters golden score ----

        [Test]
        public void DeadEvenAtRegulationExpiry_EntersGoldenScore_NotFinished()
        {
            PadTo(237);   // 79 × 3s = 237, one more Pad (3s) lands exactly on 240
            Assert.IsFalse(_e.Clock.RegulationExpired, "전제: 아직 만료 전");

            var ev = _e.ResolveExchange(Idle, Idle, Pad());

            Assert.AreEqual(240, _e.Clock.ElapsedSeconds);
            Assert.IsTrue(_e.Clock.RegulationExpired);
            Assert.IsFalse(_e.Board.IsFinished, "다 비겼으면 경기가 안 끝난다 — 골든스코어로 간다");
            Assert.AreEqual(MatchPhase.GoldenScore, _e.Clock.Phase);
            Assert.IsTrue(_e.Board.InGoldenScore);
            Assert.IsNotNull(ev.OfType<GoldenScoreEntered>().SingleOrDefault(),
                "골든스코어 진입이 이벤트로 나가야 화면이 안다");
            Assert.IsEmpty(ev.OfType<MatchEnded>(), "안 끝났으니 MatchEnded 는 아직 아니다");
        }

        [Test]
        public void GoldenScore_ClockNoLongerClamps_UnlimitedTime()
        {
            PadTo(237);
            _e.ResolveExchange(Idle, Idle, Pad());   // → 240, golden score
            Assert.AreEqual(240, _e.Clock.ElapsedSeconds, "전제: 골든스코어 진입 시점");

            _e.ResolveExchange(Idle, Idle, Pad());   // 3초 더

            Assert.AreEqual(243, _e.Clock.ElapsedSeconds,
                "§1 골든스코어는 무제한 — 240 에서 다시 클램프되면 안 된다");
        }

        // ---- §1 — a clear lead at time-up decides the match outright ----

        [Test]
        public void ClearLeadAtRegulationExpiry_EndsMatch_NoGoldenScore()
        {
            // 읽기+2, 기술 표준 → sum 3 → 유효. A 가 하나 앞선다.
            var scoring = _e.ResolveExchange(Idle, Idle, Throw(ReadLevel.Read));
            Assert.AreEqual(Judgement.Yuko, scoring.OfType<JudgementScored>().Single().Judgement,
                "전제: 유효 하나로 A 가 앞선다");
            // 이 교환도 그쳐를 부르므로(§4.3) 그립은 이미 풀렸다 — 패딩만 이어가면 된다.
            PadTo(237);   // 유효 교환(3s) + 78 패딩(234s) = 237

            var ev = _e.ResolveExchange(Idle, Idle, Pad());

            Assert.IsTrue(_e.Board.IsFinished, "유효 하나 차이는 골든스코어 없이 바로 결정된다");
            Assert.AreEqual(FighterId.A, _e.Board.Winner);
            Assert.AreNotEqual(MatchPhase.GoldenScore, _e.Clock.Phase, "결정났으니 골든스코어로 안 간다");
            Assert.IsFalse(_e.Board.InGoldenScore);
            Assert.IsEmpty(ev.OfType<GoldenScoreEntered>(), "결정된 시합에 골든스코어 이벤트가 붙으면 틀렸다");
            Assert.IsNotNull(ev.OfType<MatchEnded>().SingleOrDefault());
        }

        // ---- §1 / CORE_DESIGN — in golden score, ANY judgement ends it immediately ----

        [Test]
        public void GoldenScore_AnyJudgement_EndsMatchImmediately_ThroughFullEngine()
        {
            PadTo(237);
            _e.ResolveExchange(Idle, Idle, Pad());   // → golden score
            Assert.IsTrue(_e.Board.InGoldenScore, "전제: 골든스코어 진입");

            // 읽기+2, 표준 → sum 3 → 유효 하나로 끝나야 한다(카운터가 아니어도).
            var ev = _e.ResolveExchange(Idle, Idle, Throw(ReadLevel.Read));

            Assert.AreEqual(Judgement.Yuko, ev.OfType<JudgementScored>().Single().Judgement);
            Assert.IsTrue(_e.Board.IsFinished, "골든스코어에서는 유효 하나로도 즉시 끝난다");
            Assert.AreEqual(FighterId.A, _e.Board.Winner);
            Assert.IsNotNull(ev.OfType<MatchEnded>().SingleOrDefault());
            Assert.IsEmpty(ev.OfType<OsaekomiEntered>(),
                "경기가 이미 끝났으면 굳히기를 새로 열면 안 된다");
        }

        [Test]
        public void GoldenScore_ThirdShido_EndsMatch_ThroughFullEngine()
        {
            PadTo(237);
            _e.ResolveExchange(Idle, Idle, Pad());   // → golden score

            _e.Board.AddShido(FighterId.A);
            _e.Board.AddShido(FighterId.A);
            Assert.IsFalse(_e.Board.IsFinished, "두 번째까지는 안 끝난다 — 골든스코어라도 같다");
            _e.Board.AddShido(FighterId.A);

            Assert.IsTrue(_e.Board.IsFinished);
            Assert.AreEqual(FighterId.B, _e.Board.Winner, "지도 3개 받은 쪽이 진다");
        }

        // ---- wrong-answer pins — deliberately unresolved / must-not-fire cases ----

        [Test]
        public void TiedScore_BeforeRegulationExpires_DoesNotEnterGoldenScore()
        {
            // 아직 시간이 안 됐다 — 비겼다고 매 교환마다 골든스코어 체크를 하면 틀렸다.
            var ev = _e.ResolveExchange(Idle, Idle, Pad());

            Assert.AreEqual(MatchPhase.Standing, _e.Clock.Phase);
            Assert.IsFalse(_e.Board.InGoldenScore);
            Assert.IsEmpty(ev.OfType<GoldenScoreEntered>());
        }

        /// 의도적으로 안 건드린 경우 — 굳히기가 이 교환에서 막 열렸으면 시간이 다 됐어도
        /// 시간종료 판정을 안 한다. MATCH_RULES 가 "이미 열려 있던 굳히기가 정규시간을
        /// 넘겨도 계속 도는지"를 규정하지 않아서(§1 은 "서기 교환이 4:00 을 넘겨도 끝까지
        /// 간다"만 못 박는다), 이 케이스는 구현하지 않고 현재 동작(굳히기가 그대로 유지되고
        /// 시간종료 판정은 안 걸린다)을 사실로 고정해둔다 — 사람 판단 필요.
        [Test]
        public void OsaekomiJustOpened_AtRegulationExpiry_DoesNotTriggerTimeUp_Unresolved()
        {
            // 양손으로 잡아 grip+2, 체력 보정 없이 완전간파+시그니처로 절반(§9.1 무조건 진입).
            _e.ResolveExchange(
                new TurnChoice(HandTarget.RightSleeve, CoreStance.Neutral, LegAction.Neutral, Hand.Right),
                Idle, new ExchangeScript { DeclaredA = false, DeclaredB = false });
            _e.ResolveExchange(
                new TurnChoice(HandTarget.LeftSleeve, CoreStance.Neutral, LegAction.Neutral, Hand.Left),
                Idle, new ExchangeScript { DeclaredA = false, DeclaredB = false });
            PadTo(237);   // 두 그립 교환(6s, DeclaredA/B=false 지만 시간은 흐른다) + 패딩 → 이후
                          // 마지막 판정 교환(3s)까지 더하면 정확히 240 에 닿는다

            var ev = _e.ResolveExchange(Idle, Idle, new ExchangeScript
            {
                Attacker = FighterId.A,
                Technique = new TechniqueSpec("틀어잡고 밭다리", ActionCost.BigThrow, TechniqueClass.Standard),
                Read = ReadLevel.Read,
            });

            Assert.AreEqual(Judgement.WazaAri, ev.OfType<JudgementScored>().Single().Judgement,
                "전제: 절반 — §9.1 표대로 그립 조건 없이 무조건 굳히기 진입");
            Assert.IsTrue(_e.Osaekomi.Active, "전제: 이번 교환에 굳히기가 열렸다");
            Assert.IsTrue(_e.Clock.RegulationExpired, "전제: 시간은 이미 다 됐다");
            Assert.IsFalse(_e.Board.IsFinished, "현재 동작: 시간종료 판정이 굳히기 진입 교환엔 안 걸린다");
            Assert.AreEqual(MatchPhase.Osaekomi, _e.Clock.Phase, "현재 동작: 골든스코어로도 안 넘어간다");
        }
    }
}
