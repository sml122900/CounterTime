using System.Linq;
using NUnit.Framework;
using CounterTime.Brain;
using CounterTime.Core;

namespace CounterTime.Brain.Tests
{
    /// <summary>
    /// §3 성향 창. 눈금이 둘이라는 것이 요점이다 — 경고선(4/6)과 위험선(3연속)은
    /// 같은 증거가 아니고, 같은 효과를 내지도 않는다.
    /// </summary>
    public class TendencyWindowTests
    {
        static TurnChoice Pair(CoreStance core, LegAction legs) =>
            new TurnChoice(HandTarget.None, core, legs);

        static readonly TurnChoice P = Pair(CoreStance.Aggressive, LegAction.Attack);
        static readonly TurnChoice Q = Pair(CoreStance.Defensive, LegAction.Defend);

        static TendencyWindow WindowOf(params TurnChoice[] oldestFirst)
        {
            var w = new TendencyWindow();
            foreach (var c in oldestFirst) w.Record(c);
            return w;
        }

        [Test]
        public void FourOfSixSamePair_GivesRead()
        {
            var w = WindowOf(P, Q, P, Q, P, P);   // P 가 4회
            Assert.IsTrue(w.MajorityMatches(P), "창 안 4회면 최빈이다");
            Assert.IsFalse(w.MajorityMatches(Q), "2회짜리는 최빈이 아니다");
        }

        [Test]
        public void ThreeOfSix_IsNotYetAMajority()
        {
            var w = WindowOf(P, Q, Q, P, Q, P);   // P 3회 / Q 3회
            Assert.IsFalse(w.MajorityMatches(P), "임계는 4 다 — 3 은 아직 아니다");
        }

        [Test]
        public void ThreeConsecutive_GivesFullRead()
        {
            var w = WindowOf(Q, Q, Q, P, P, P);   // 앞머리 3개가 P
            Assert.IsTrue(w.ConsecutiveMatches(P), "앞머리 3연속이면 위험선이다");
        }

        /// 방금 패턴을 깬 선수를 옛 패턴으로 읽으면 안 된다 — 예고된 위험선이 거짓말이 된다.
        [Test]
        public void PatternBroken_NoLongerCounts_NeitherTier()
        {
            var w = WindowOf(P, P, P, P, P, P);
            Assert.IsTrue(w.ConsecutiveMatches(P), "전제: P 로는 위험선이 선다");

            Assert.IsFalse(w.ConsecutiveMatches(Q), "이번에 Q 를 냈으면 P 패턴으로 읽히지 않는다");
            Assert.IsFalse(w.MajorityMatches(Q), "최빈도 이번 커밋이 그 쌍일 때만이다");
        }

        [Test]
        public void WindowIsSixTurns_SeventhTurnDropsOut()
        {
            // P 를 4번 넣고 Q 를 3번 더 넣으면, P 하나가 창 밖으로 밀려나 3회가 된다.
            var w = WindowOf(P, P, P, P, Q, Q, Q);

            Assert.AreEqual(TendencyWindow.WindowTurns, w.Count, "창은 6턴을 넘지 않는다");
            Assert.IsFalse(w.MajorityMatches(P), "가장 오래된 P 가 떨어져 3회가 됐다 — 최빈이 아니다");
            Assert.IsTrue(w.ConsecutiveMatches(Q), "앞머리 3개는 Q 다");
        }

        [Test]
        public void ScoutingLevel0_StartsEmpty()
        {
            var w = new TendencyWindow();
            Assert.AreEqual(0, w.Count);
            Assert.IsFalse(w.MajorityMatches(P), "빈 창은 아무것도 못 읽는다");
            Assert.IsFalse(w.ConsecutiveMatches(P));
            Assert.AreEqual(0, Jindoyun.Create(new MatchRulesConfig()).ScoutingLevel,
                "진도윤은 0 으로 출하한다 — 창 시드가 없다");
        }

        /// 성향은 (코어, 하체) 쌍이다. 잡는 손은 국면이 정하지 성향이 정하지 않는다.
        [Test]
        public void WindowKeysOnCoreAndLegsOnly_NotTheHand()
        {
            var w = new TendencyWindow();
            for (int i = 0; i < 3; i++)
                w.Record(new TurnChoice(HandTarget.RightSleeve, CoreStance.Aggressive, LegAction.Attack));

            Assert.IsTrue(
                w.ConsecutiveMatches(new TurnChoice(HandTarget.LeftLapel, CoreStance.Aggressive, LegAction.Attack)),
                "손이 달라도 코어·하체가 같으면 같은 성향이다");
        }

        [Test]
        public void SeedingFillsTheWindow_ForHigherScoutingLevels()
        {
            var w = new TendencyWindow(Enumerable.Repeat(P, 3));
            Assert.AreEqual(3, w.Count);
            Assert.IsTrue(w.ConsecutiveMatches(P), "시드만으로도 위험선이 설 수 있다 — ScoutingLevel ≥ 1 용");
        }
    }
}
