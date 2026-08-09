using System.Linq;
using NUnit.Framework;
using CounterTime.Core;

namespace CounterTime.Core.Tests
{
    /// <summary>
    /// MATCH_RULES §9 — 굳히기 진입·탈출. OPPONENT_BRAIN §8 이 이름 붙인 5개(그대로) +
    /// 구현하며 읽어낸 경계 케이스.
    /// </summary>
    public class OsaekomiTests
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

        static ExchangeScript BigThrow(ReadLevel read, TechniqueClass cls) =>
            new ExchangeScript
            {
                Attacker = FighterId.A,
                Technique = new TechniqueSpec("밭다리", ActionCost.BigThrow, cls),
                Read = read,
            };

        void GripUp() =>
            _e.ResolveExchange(new TurnChoice(HandTarget.RightSleeve, CoreStance.Neutral, LegAction.Neutral),
                               Idle, new ExchangeScript());

        /// B 체력을 노포인트 교환으로만 깎는다 — 그쳐가 안 붙어서 회복이 안 낀다.
        /// B 는 매번 '전부 중립'이 아니어야 한다 — 안 그러면 §6.1 소극 누산기가 14번
        /// 만에 문턱을 넘어 그쳐를 불러 버리고, 그 그쳐가 GripUp() 으로 세운 A 의 잡기를
        /// 도로 0 으로 되돌린다(발견한 함정 — 처음엔 이걸로 계측이 어긋났다).
        static readonly TurnChoice NotIdle = new TurnChoice(HandTarget.None, CoreStance.Aggressive, LegAction.Neutral);

        void DrainB(int times)
        {
            for (int i = 0; i < times; i++)
                _e.ResolveExchange(Idle, NotIdle, BigThrow(ReadLevel.Neutral, TechniqueClass.Sloppy));
        }

        /// 절반으로 진입 — 잡기 0, 체력 100(<50 아님) 이어도 절반은 무조건 들어간다.
        /// adv = 2(절반 진입) + 0(잡기) + 0(체력 보너스) = 2 → escapesNeeded = 2.
        System.Collections.Generic.IReadOnlyList<MatchEvent> EnterViaWazaAri() =>
            _e.ResolveExchange(Idle, Idle, BigThrow(ReadLevel.FullRead, TechniqueClass.Signature));

        /// 유효로 진입 — 잡기 +1, B 체력을 [25,50) 대역까지 깎아 게이트를 통과시킨다.
        /// sum = 잡기1 + 수싸움0 + 체력+2(44는 25~49) + 기술+1 + 자세0 = 4 → 유효.
        /// adv = 0(절반 아님) + 1(잡기) + 1(체력<50 대역) = 2 → escapesNeeded = 2.
        /// waza-ari 진입과 달리, 굳히기 자체가 10초째 주는 절반이 진입 절반과 합산
        /// (awasete-ippon)되어 버리는 충돌이 없다 — 그 충돌은 별도 테스트로 고정한다.
        System.Collections.Generic.IReadOnlyList<MatchEvent> EnterViaYuko()
        {
            GripUp();
            DrainB(14);
            return _e.ResolveExchange(Idle, Idle, BigThrow(ReadLevel.Neutral, TechniqueClass.Standard));
        }

        [Test]
        public void Yuko_WithoutGripEdge_DoesNotEnterOsaekomi()
        {
            // 잡기 우위 없이(0) 유효 — 읽음+2, 표준+1 = sum 3.
            var ev = _e.ResolveExchange(Idle, Idle, BigThrow(ReadLevel.Read, TechniqueClass.Standard));

            Assert.AreEqual(Judgement.Yuko, ev.OfType<JudgementScored>().Single().Judgement);
            Assert.IsFalse(_e.Osaekomi.Active, "잡기 +1 미만이면 굳히기로 안 들어간다");
            Assert.IsNotEmpty(ev.OfType<MatteCalled>(), "대신 그쳐로 착지한다");
        }

        [Test]
        public void WazaAri_AlwaysEnters()
        {
            var ev = EnterViaWazaAri();

            Assert.AreEqual(Judgement.WazaAri, ev.OfType<JudgementScored>().Single().Judgement);
            Assert.IsTrue(_e.Osaekomi.Active, "잡기·체력 조건 없이도 절반은 무조건 들어간다");
            Assert.AreEqual(FighterId.A, _e.Osaekomi.Holder);
            Assert.IsEmpty(ev.OfType<MatteCalled>());
        }

        [Test]
        public void AdvFrozenAtEntry_DefenderStaminaDropDoesNotRaiseIt()
        {
            EnterViaYuko();
            Assert.IsTrue(_e.Osaekomi.Active, "전제: 굳히기 진입");
            int advAtEntry = _e.Osaekomi.Adv;
            int neededAtEntry = _e.Osaekomi.EscapesNeeded;

            // 굳히기가 열린 뒤 시간이 흘러도(맞물림으로 스테이 비용까지 나가도)
            // adv/escapesNeeded 는 그대로여야 한다 — §9.2 "frozen at entry".
            _e.ResolveOsaekomiExchange(OsaekomiDefenderChoice.Reversal, OsaekomiHolderChoice.Follow);

            Assert.AreEqual(advAtEntry, _e.Osaekomi.Adv, "adv 는 진입 시 1회 계산, 재계산 없음");
            Assert.AreEqual(neededAtEntry, _e.Osaekomi.EscapesNeeded);
        }

        [Test]
        public void ScoreAwardedBeforeEscapeResolves_AtFiveSeconds()
        {
            EnterViaWazaAri();
            int yukoBefore = _e.Board.Yuko(FighterId.A);

            var ev = _e.ResolveOsaekomiExchange(OsaekomiDefenderChoice.Reversal, OsaekomiHolderChoice.PressIn);

            // §9.3: 판정이 먼저, 탈출은 그 다음. 리스트 순서로 고정한다.
            var list = ev.ToList();
            int scoreIdx = list.FindIndex(e => e is OsaekomiScoreAwarded);
            int escapeIdx = list.FindIndex(e => e is OsaekomiEscapeResolved);
            Assert.GreaterOrEqual(scoreIdx, 0);
            Assert.GreaterOrEqual(escapeIdx, 0);
            Assert.Less(scoreIdx, escapeIdx, "판정 이벤트가 탈출 이벤트보다 먼저 나와야 한다");
            Assert.AreEqual(yukoBefore + 1, _e.Board.Yuko(FighterId.A), "5초째 유효가 실제로 붙었다");
        }

        [Test]
        public void EscapeResetsGripsToFree_ButGrantsNoMatteRecovery()
        {
            EnterViaYuko(); // escapesNeeded == 2 (§ 헬퍼 주석)
            Assert.AreEqual(2, _e.Osaekomi.EscapesNeeded, "전제: 이 시나리오는 어긋남 2회로 끝난다");
            int bStaminaBeforeEscapes = _e.Stamina.Get(FighterId.B);

            // 어긋남을 두 번 낸다 — (뒤집기, 눌러붙기) 와 (빠져나가기, 따라가기) 는 둘 다 교차쌍.
            _e.ResolveOsaekomiExchange(OsaekomiDefenderChoice.Reversal, OsaekomiHolderChoice.PressIn);
            var last = _e.ResolveOsaekomiExchange(OsaekomiDefenderChoice.Escape, OsaekomiHolderChoice.Follow);

            Assert.IsFalse(_e.Osaekomi.Active, "escapesNeeded 도달로 굳히기가 끝났다");
            Assert.AreEqual(0, _e.Grip.GripScore(FighterId.A), "탈출은 잡기를 전부 Free 로 되돌린다");
            Assert.IsNotEmpty(last.OfType<OsaekomiEnded>());

            // 그쳐라면 +3(MatteRecovery)이 붙지만 탈출엔 그게 없다 — 어긋남만 냈으니
            // §9.3 스테이 비용(맞물림 전용)도 안 나갔다. B 체력은 그대로여야 한다.
            Assert.AreEqual(bStaminaBeforeEscapes, _e.Stamina.Get(FighterId.B),
                "탈출은 그쳐 회복(+3)을 안 준다");
        }

        // ---- 경계 케이스 ----

        [Test]
        public void MatchedPair_ContinuesHold_DoesNotCreditEscape()
        {
            EnterViaWazaAri();

            var ev = _e.ResolveOsaekomiExchange(OsaekomiDefenderChoice.Reversal, OsaekomiHolderChoice.Follow); // 맞물림

            Assert.AreEqual(0, _e.Osaekomi.EscapesSoFar, "맞물림은 탈출 카운트를 안 올린다");
            Assert.IsTrue(_e.Osaekomi.Active, "맞물림은 유지가 계속된다");
            var resolved = ev.OfType<OsaekomiEscapeResolved>().Single();
            Assert.IsFalse(resolved.EscapeCredited);
        }

        [Test]
        public void IpponDuringHold_EndsMatch_ButDoesNotEmitOsaekomiEnded()
        {
            EnterViaYuko();

            // 맞물림으로만 20초(4틱)까지 버틴다 — 어긋남이 나면 escapesNeeded(2)에 먼저
            // 걸려 탈출로 끝나 버린다.
            _e.ResolveOsaekomiExchange(OsaekomiDefenderChoice.Reversal, OsaekomiHolderChoice.Follow); // 5s
            _e.ResolveOsaekomiExchange(OsaekomiDefenderChoice.Reversal, OsaekomiHolderChoice.Follow); // 10s
            _e.ResolveOsaekomiExchange(OsaekomiDefenderChoice.Reversal, OsaekomiHolderChoice.Follow); // 15s
            var ev = _e.ResolveOsaekomiExchange(OsaekomiDefenderChoice.Reversal, OsaekomiHolderChoice.Follow); // 20s

            Assert.IsTrue(_e.Board.IsFinished);
            Assert.IsTrue(_e.Board.HasIppon(FighterId.A));
            Assert.IsNotEmpty(ev.OfType<MatchEnded>());
            Assert.IsEmpty(ev.OfType<OsaekomiEnded>(), "한판으로 끝난 굳히기는 '탈출'이 아니다");
        }

        [Test]
        public void HolderRecovery_OnlyOnCompletedTenSecondMarks()
        {
            EnterViaYuko();

            int aStaminaAt0 = _e.Stamina.Get(FighterId.A);
            _e.ResolveOsaekomiExchange(OsaekomiDefenderChoice.Reversal, OsaekomiHolderChoice.Follow); // 5s, 맞물림
            int aStaminaAt5 = _e.Stamina.Get(FighterId.A);
            _e.ResolveOsaekomiExchange(OsaekomiDefenderChoice.Reversal, OsaekomiHolderChoice.Follow); // 10s, 맞물림
            int aStaminaAt10 = _e.Stamina.Get(FighterId.A);

            Assert.AreEqual(aStaminaAt0, aStaminaAt5, "5초째엔 아직 회복이 없다");
            Assert.AreEqual(aStaminaAt0 + _cfg.StallRecoveryPer10s, aStaminaAt10, "10초 완주에서 +2");
        }

        /// m38 원문("절반 보유 시 10초에 합산 한판")을 그대로 고정한다. 절반으로 진입한
        /// 굳히기가 10초를 채우면, 굳히기 자체의 §9.3 절반과 진입 절반이 합산돼 한판이
        /// 된다 — 굳히기 안쪽 20초 임계까지 갈 필요가 없다.
        [Test]
        public void WazaAriEntryPlusTenSecondHold_CombinesToAwaseteIppon()
        {
            EnterViaWazaAri();
            Assert.AreEqual(1, _e.Board.WazaAri(FighterId.A), "전제: 진입 절반 1개");

            _e.ResolveOsaekomiExchange(OsaekomiDefenderChoice.Reversal, OsaekomiHolderChoice.Follow); // 5s
            var ev = _e.ResolveOsaekomiExchange(OsaekomiDefenderChoice.Reversal, OsaekomiHolderChoice.Follow); // 10s

            Assert.IsTrue(_e.Board.HasIppon(FighterId.A), "절반 두 개가 합쳐져 한판");
            Assert.IsTrue(_e.Board.IsFinished);
            Assert.IsNotEmpty(ev.OfType<MatchEnded>());
        }
    }
}
