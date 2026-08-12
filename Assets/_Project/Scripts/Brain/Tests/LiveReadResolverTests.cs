using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using CounterTime.Brain;
using CounterTime.Core;

namespace CounterTime.Brain.Tests
{
    /// <summary>
    /// <c>OPPONENT_BRAIN §2</c> 읽기 사다리 6행. 이름에 틀린 답을 드러내 둔다.
    ///
    /// 사다리가 배타적이라는 것 — 첫 매칭에서 끝나고 합산하지 않는다 — 이 이 파일의
    /// 절반이다. 나머지 절반은 소유권 경계: 리졸버는 BASE 를 내고 감쇄는 Core 가 한다.
    /// </summary>
    public class LiveReadResolverTests
    {
        static readonly MatchRulesConfig Cfg = new MatchRulesConfig();

        static TurnChoice Pair(CoreStance core, LegAction legs) =>
            new TurnChoice(HandTarget.None, core, legs);

        static readonly TurnChoice P = Pair(CoreStance.Aggressive, LegAction.Attack);
        static readonly TurnChoice Q = Pair(CoreStance.Defensive, LegAction.Defend);

        static HabitDefinition HabitOf(HabitId id) =>
            Jindoyun.Create(Cfg).Habits.Single(h => h.Id == id);

        /// 공격 습관(기술 있음) — H1.
        static HabitDecision FiredAttacking() =>
            new HabitDecision(HabitOf(HabitId.BigThrowOnGripEdge), fired: true);

        /// 비공격 습관(기술 없음) — H3 극단적 방어.
        static HabitDecision FiredNonAttacking() =>
            new HabitDecision(HabitOf(HabitId.TurtleWhenTired), fired: true);

        /// 조건은 섰지만 25% 에 걸렸다.
        static HabitDecision Missed() =>
            new HabitDecision(HabitOf(HabitId.BigThrowOnGripEdge), fired: false);

        static ObservationLedger LedgerWith(HabitId id, int observations)
        {
            var l = new ObservationLedger();
            var d = new HabitDecision(HabitOf(id), fired: true);
            for (int i = 0; i < observations; i++) l.Record(d);
            return l;
        }

        static LiveReadResolver Resolver(ObservationLedger ledger, TendencyWindow window,
                                         HabitDecision noted)
        {
            var r = new LiveReadResolver(ledger, window ?? new TendencyWindow());
            r.NoteOpponentHabit(noted);
            return r;
        }

        // ── 1행 ─────────────────────────────────────────────────────────────────

        /// B 가 공격하는데 A 가 그 습관을 확정으로 안다 → B 가 읽힌다.
        [Test]
        public void KnownHabitFiredByB_WhenBAttacks_GivesAttackerGotRead()
        {
            var r = Resolver(LedgerWith(HabitId.BigThrowOnGripEdge, 3), null, FiredAttacking());

            Assert.AreEqual(ReadLevel.GotRead, r.For(FighterId.B, P, Q),
                "확정 관측이 B 의 공격을 되치기 사거리로 끌어내린다");
        }

        // ── 2·3행 ───────────────────────────────────────────────────────────────

        /// A 가 공격하고 B 는 비공격 습관(극단적 방어)을 냈다 → A 가 완전간파.
        [Test]
        public void KnownHabitFiredByB_WhenAAttacks_GivesAttackerFullRead()
        {
            var r = Resolver(LedgerWith(HabitId.TurtleWhenTired, 3), null, FiredNonAttacking());

            Assert.AreEqual(ReadLevel.FullRead, r.For(FighterId.A, P, Q),
                "같은 지식이 공격자가 바뀌면 부호를 바꾼다");
        }

        /// <summary>
        /// 심증(1~2회)은 A 의 공격을 돕지만 <b>B 를 읽지는 못한다</b>. 되치기에는
        /// 확신이 필요하고, 그래야 3회 관측이 실질 목표가 된다.
        /// </summary>
        [Test]
        public void Suspicion_HelpsAAttacking_ButNeverReadsBAsAttacker()
        {
            var suspected = LedgerWith(HabitId.TurtleWhenTired, 2);
            Assert.AreEqual(ReadLevel.Read, Resolver(suspected, null, FiredNonAttacking()).For(FighterId.A, P, Q),
                "심증은 A 의 공격에 +2 를 준다");

            // 같은 심증으로 B 를 되치려 하면 아무것도 안 나온다.
            var suspectedH1 = LedgerWith(HabitId.BigThrowOnGripEdge, 2);
            Assert.AreEqual(ReadLevel.Neutral, Resolver(suspectedH1, null, FiredAttacking()).For(FighterId.B, P, Q),
                "심증만으로 B 가 되치기당하면 안 된다 — 1행은 확정만 받는다");

            // 1회도 마찬가지.
            Assert.AreEqual(ReadLevel.Neutral,
                Resolver(LedgerWith(HabitId.BigThrowOnGripEdge, 1), null, FiredAttacking()).For(FighterId.B, P, Q));
        }

        // ── 발화 없음 ───────────────────────────────────────────────────────────

        /// <summary>
        /// 안 낸 습관은 <b>어느 쪽에도</b> 읽기를 주지 않는다. 지식이 있어도 B 가 안 내면
        /// tell 이 없고, 안 내는 것 자체가 벌받을 일도 아니다.
        /// </summary>
        [Test]
        public void HabitNotFired_YieldsNeutral_ToEitherSide()
        {
            var known = LedgerWith(HabitId.BigThrowOnGripEdge, 5);

            // 25% 에 걸린 턴 — 조건은 섰다.
            var missed = Resolver(known, null, Missed());
            Assert.AreEqual(ReadLevel.Neutral, missed.For(FighterId.B, P, Q), "빗나간 턴은 GotRead 가 아니다");
            Assert.AreEqual(ReadLevel.Neutral, missed.For(FighterId.A, P, Q), "A 쪽에도 아무것도 안 준다");

            // 아무 습관도 후보에 안 오른 턴.
            var nothing = Resolver(known, null, default);
            Assert.AreEqual(ReadLevel.Neutral, nothing.For(FighterId.B, P, Q));
            Assert.AreEqual(ReadLevel.Neutral, nothing.For(FighterId.A, P, Q));
        }

        // ── 배타성 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 1~3행이 4·5행을 이긴다. A 가 능동적으로 번 지식이 A 자신의 게으름보다 위다.
        /// 합산해서 −3 과 +2 를 섞으면 틀린 답이다.
        /// </summary>
        [Test]
        public void LadderIsExclusive_HabitKnowledgeBeatsTendencyWindow()
        {
            // A 가 P 를 계속 내서 위험선까지 갔고(4행이면 B 에게 FullRead),
            // 동시에 A 는 B 의 습관을 확정으로 안다(1행이면 B 에게 GotRead).
            var window = new TendencyWindow();
            for (int i = 0; i < 3; i++) window.Record(P);
            Assert.IsTrue(window.ConsecutiveMatches(P), "전제: 창만 보면 4행이 선다");

            var r = Resolver(LedgerWith(HabitId.BigThrowOnGripEdge, 3), window, FiredAttacking());

            Assert.AreEqual(ReadLevel.GotRead, r.For(FighterId.B, P, Q),
                "1행이 먼저 잡는다 — 창의 FullRead 로 덮이거나 합산되면 안 된다");
        }

        /// 위험선(3연속)에서만 A 의 공격이 읽힌다.
        [Test]
        public void ThreeConsecutiveIdenticalCommits_MakeAAttackReadable_GotRead()
        {
            var window = new TendencyWindow();
            for (int i = 0; i < 3; i++) window.Record(P);

            var r = Resolver(new ObservationLedger(), window, default);

            Assert.AreEqual(ReadLevel.GotRead, r.For(FighterId.A, P, Q),
                "예측 가능해진 A 는 자기 공격이 읽힌다 (5행)");
            Assert.AreEqual(ReadLevel.FullRead, r.For(FighterId.B, P, Q),
                "같은 증거가 B 의 공격에는 +3 이다 (4행)");
        }

        /// <summary>
        /// 경고선(4/6)은 <b>아직</b> A 의 공격을 읽히게 하지 않는다. 같은 증거로 B 는 +2,
        /// A 는 −3 이면 크기가 안 맞고 너무 자주 터진다.
        /// </summary>
        [Test]
        public void FourOfSixMajority_DoesNotYetMakeAAttackReadable()
        {
            // P 가 4회지만 앞머리 3연속은 아니다.
            var window = new TendencyWindow();
            foreach (var c in new[] { P, P, Q, P, Q, P }) window.Record(c);
            Assert.IsTrue(window.MajorityMatches(P), "전제: 경고선은 섰다");
            Assert.IsFalse(window.ConsecutiveMatches(P), "전제: 위험선은 아직이다");

            var r = Resolver(new ObservationLedger(), window, default);

            Assert.AreEqual(ReadLevel.Neutral, r.For(FighterId.A, P, Q),
                "경고선만으로 A 가 되치기당하면 안 된다 — 위험선은 3연속이다");
            Assert.AreEqual(ReadLevel.Read, r.For(FighterId.B, P, Q),
                "같은 증거가 B 에게는 +2 를 준다");
        }

        // ── 소유권 경계 ─────────────────────────────────────────────────────────

        /// <summary>
        /// 리졸버는 BASE 를 낸다. §4.2 감쇄는 Core 가 걸고, 여기서 선적용하면 두 칸
        /// 내려가 +2 가 0 이 아니라 −3 이 된다 — 에러는 안 난다.
        ///
        /// <b>자체 완결로 쓴다.</b> 42노드 시나리오는 §4.2 를 한 번도 안 밟으므로
        /// 대조 모드 회귀가 이걸 잡아주지 않는다.
        /// </summary>
        [Test]
        public void ResolverNeverPreAppliesCounterReadyStepDown()
        {
            var r = Resolver(LedgerWith(HabitId.TurtleWhenTired, 2), null, FiredNonAttacking());

            var attackerCommit = TurnChoice.Neutral;
            var defenderCounterReady = Pair(CoreStance.Neutral, LegAction.CounterReady);

            var supplied = r.For(FighterId.A, attackerCommit, defenderCounterReady);
            Assert.AreEqual(ReadLevel.Read, supplied,
                "방어자가 카운터 준비를 선언했어도 리졸버는 깎지 않은 +2 를 낸다");

            // 그리고 Core 가 정확히 한 칸 내린다. 두 칸 내려가면 GotRead 가 보인다.
            var engine = new MatchEngine(new MatchRulesConfig { ThresholdJitter = 0 },
                                         RefereeProfile.Standard, new SystemRandomSource(1));
            var applied = engine.ResolveExchange(attackerCommit, defenderCounterReady, new ExchangeScript
            {
                Attacker = FighterId.A,
                Technique = new TechniqueSpec("업어치기", ActionCost.BigThrow, TechniqueClass.Standard),
                Read = supplied,
            }).OfType<CounterReadyApplied>().Single();

            Assert.AreEqual(ReadLevel.Read, applied.AttackerReadBefore, "Core 가 받은 값이 BASE 여야 한다");
            Assert.AreEqual(ReadLevel.Neutral, applied.AttackerReadAfter, "한 칸만 — 읽힘(−3)이면 이중 적용이다");
        }

        /// <summary>
        /// 되치기는 지식만으로 나오지 않는다. B 가 GotRead −3 이어도 sum ≤ −3 이려면
        /// A 가 잡기까지 이겨야 한다 (§4 카논 케이스 5 와 같은 구조).
        /// </summary>
        [Test]
        public void CounterStillNeedsGripEdge_KnowledgeAloneDoesNotScore()
        {
            var r = Resolver(LedgerWith(HabitId.BigThrowOnGripEdge, 3), null, FiredAttacking());
            var read = r.For(FighterId.B, P, Q);
            Assert.AreEqual(ReadLevel.GotRead, read, "전제: B 는 읽혔다");

            var cfg = new MatchRulesConfig();
            var tech = TechniqueClass.Standard;

            // 잡기가 팽팽하면: 0 + (−3) + 0 + 1 = −2. 임계 −3 에 못 미친다.
            var even = JudgementCalculator.Evaluate(
                FighterId.B, new JudgementContext(0, read, 100, tech, false), cfg);
            Assert.AreEqual(-2, even.Sum);
            Assert.IsFalse(even.IsCounter, "읽기만으로는 되치기가 안 난다 — 지식은 공짜 점수가 아니다");

            // A 가 잡기를 이겨 B 의 유효 잡기가 −1 이면: −1 −3 +0 +1 = −3. 이제 발화한다.
            var gripped = JudgementCalculator.Evaluate(
                FighterId.B, new JudgementContext(-1, read, 100, tech, false), cfg);
            Assert.AreEqual(-3, gripped.Sum);
            Assert.IsTrue(gripped.IsCounter, "잡기까지 이겨야 되치기가 선다");
            Assert.AreEqual(FighterId.A, gripped.ScoredBy);
        }

        // ── 면 경계 ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 습관 조건은 폴백·게이트 면(<see cref="IBrainContext"/>)을 <b>절대</b> 안 본다.
        ///
        /// 타입으로 막혀 있다: 조건의 인자가 <see cref="IHabitContext"/> 이고, 그 인터페이스에
        /// 지도 개수·시계·게이트 멤버가 없다. 그래서 습관이 지도 <i>개수</i>로 분기하려면
        /// 누군가 면을 넓혀야 하고, 그건 diff 에 보인다.
        ///
        /// §H 3.5b — 넷에서 여섯으로 늘었다(보고 후 확장, IHabitContext.cs 의 XML 문서
        /// 참조): <c>OpponentSetupActive</c>(8강 서도현 H1 — 상대 흔들기 상태, 인과상
        /// 이미 지나간 사실이라 독심 아님)와 <c>OwnBalance</c>(서도현 H2 + 4강 임준철
        /// 변칙기 — 이미 공개된 <c>MatchEngine.Balance.Get</c> 을 그대로 읽을 뿐).
        /// 둘 다 지도 개수·시계·게이트 같은 <see cref="IBrainContext"/> 전용 개념이
        /// 아니다.
        /// </summary>
        [Test]
        public void HabitConditionsNeverSeeTheBrainContext_OnlyIHabitContext()
        {
            var condition = typeof(HabitDefinition).GetField("Condition");
            Assert.AreEqual(typeof(Func<IHabitContext, bool>), condition.FieldType,
                "조건의 인자가 IBrainContext 로 넓어지면 안 된다");

            var habitMembers = typeof(IHabitContext).GetProperties().Select(p => p.Name).ToArray();
            CollectionAssert.AreEquivalent(
                new[] { "OwnEffectiveGrip", "TurnsSinceOwnShido", "OwnStamina", "OpponentStamina",
                        "OpponentSetupActive", "OwnBalance" },
                habitMembers, "습관 면은 여섯으로 고정이다(§H 3.5b 확장) — 더 늘리려면 보고 후에 늘린다");

            foreach (var brainOnly in new[] { "OwnShidoCount", "SecondsRemaining",
                                              "OwnStaminaLockout", "OwnHasLockedHand" })
                CollectionAssert.DoesNotContain(habitMembers, brainOnly,
                    brainOnly + " 는 폴백·게이트 전용이다 — 습관 면에 새면 습관이 상태 평가형으로 흐른다");

            // 그리고 진도윤의 조건들은 습관 면만 있는 객체로 전부 돈다. 누가 안에서
            // IBrainContext 로 캐스팅했다면 여기서 터진다.
            var habitOnly = new HabitOnlyContext();
            foreach (var h in Jindoyun.Create(Cfg).Habits)
                Assert.DoesNotThrow(() => h.Condition(habitOnly), h.Id + " 조건이 다른 면을 요구한다");
        }

        /// IHabitContext 만 구현한다. IBrainContext 는 일부러 구현하지 않는다.
        sealed class HabitOnlyContext : IHabitContext
        {
            public int OwnEffectiveGrip => 1;
            public int? TurnsSinceOwnShido => 0;
            public int OwnStamina => 40;
            public int OpponentStamina => 60;
            public bool OpponentSetupActive => false;
            public int OwnBalance => 100;
        }
    }
}
