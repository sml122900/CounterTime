using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using CounterTime.Brain;
using CounterTime.Core;

namespace CounterTime.Brain.Tests
{
    /// <summary>
    /// §4 사다리(2단 = 습관)와 §4.2 폐기·교체. 이름에 틀린 답을 드러내 둔다 —
    /// 규칙이 지워지면 시끄럽게 실패해야 한다.
    ///
    /// 난수는 전부 <see cref="FixedRandomSource"/> 주입이다. UnityEngine.Random 은
    /// 이 어셈블리에서 애초에 보이지도 않는다.
    /// </summary>
    public class HabitBrainTests
    {
        /// 조건 넷을 그대로 먹이는 테스트용 컨텍스트. 습관이 보는 것이 이게 전부다.
        sealed class Ctx : IHabitContext
        {
            public int OwnEffectiveGrip { get; set; }
            public int? TurnsSinceOwnShido { get; set; }
            public int OwnStamina { get; set; } = 100;
            public int OpponentStamina { get; set; } = 100;
        }

        static readonly MatchRulesConfig Cfg = new MatchRulesConfig();

        /// 0.0 은 어떤 확률에도 통과한다 — "발화한다"를 뜻한다.
        static HabitLadder AlwaysFires() => new HabitLadder(new FixedRandomSource(0f));
        /// 0.8 은 0.75 를 넘는다 — "빗나간다"를 뜻한다.
        static HabitLadder AlwaysMisses() => new HabitLadder(new FixedRandomSource(0.8f));

        static HabitState FreshMatch() => new HabitState(Jindoyun.Create(Cfg));

        static HabitDefinition Habit(HabitState s, HabitId id) =>
            s.Opponent.Habits.Single(h => h.Id == id);

        /// §4.2 피해는 확정 뒤에만 센다 — 그 전제를 만드는 원장.
        static ObservationLedger ConfirmedLedger(HabitState s, HabitId id)
        {
            var l = new ObservationLedger();
            var d = new HabitDecision(Habit(s, id), fired: true);
            for (int i = 0; i < ObservationLedger.CertainObservations; i++) l.Record(d);
            return l;
        }

        // ── §4 사다리 ────────────────────────────────────────────────────────────

        /// <summary>
        /// 사다리는 첫 매치에서 끝난다. H1 과 H2 의 조건이 동시에 서 있어도 이번 턴에
        /// 나오는 것은 <b>H1 하나</b>다. 둘 다 반환하거나 행동을 합치면 틀린 답이다.
        /// </summary>
        [Test]
        public void LadderNeverSums_ExactlyOneHabitFiresPerTurn()
        {
            var s = FreshMatch();
            var ctx = new Ctx { OwnEffectiveGrip = 2, TurnsSinceOwnShido = 0 };

            Assert.IsTrue(Habit(s, HabitId.BigThrowOnGripEdge).Condition(ctx), "전제: H1 조건이 선다");
            Assert.IsTrue(Habit(s, HabitId.LegTechAfterShido).Condition(ctx), "전제: H2 조건도 동시에 선다");

            var d = AlwaysFires().Next(s, ctx);

            Assert.AreEqual(HabitId.LegTechAfterShido, d.Habit.Id, "조건이 좁은 H2 가 이긴다");
            Assert.IsTrue(d.Fired);
            Assert.AreEqual("안뒤축감아치기", d.Habit.Action.Technique.Id,
                "H2 의 행동만 나온다 — H1 의 업어치기와 섞이지 않는다");
        }

        /// <summary>
        /// 배타 사다리는 <b>조건이 좁은 것부터</b> 본다. 넓은 조건이 위에 있으면 좁은
        /// 것을 굶긴다 — H1 을 맨 앞에 뒀을 때 H1 이 7회 도는 동안 H2·H3 이 0회였다.
        /// </summary>
        [Test]
        public void LadderChecksNarrowConditionsFirst_H2BeatsH1WhenBothHold()
        {
            var s = FreshMatch();
            var both = new Ctx { OwnEffectiveGrip = 2, TurnsSinceOwnShido = 0 };

            Assert.IsTrue(Habit(s, HabitId.BigThrowOnGripEdge).Condition(both), "전제: H1 조건도 선다");
            Assert.IsTrue(Habit(s, HabitId.LegTechAfterShido).Condition(both), "전제: H2 조건도 선다");

            Assert.AreEqual(HabitId.LegTechAfterShido, AlwaysFires().Next(s, both).Habit.Id,
                "지도 직후 2턴이라는 좁은 조건이 잡기 축 절반보다 먼저다");

            // 좁은 조건이 안 서면 넓은 쪽으로 내려간다.
            Assert.AreEqual(HabitId.BigThrowOnGripEdge,
                AlwaysFires().Next(FreshMatch(), new Ctx { OwnEffectiveGrip = 2 }).Habit.Id);

            // H4 는 잡기 축의 나머지 절반이라 가장 뒤다.
            Assert.AreEqual(HabitId.SeekGripRightSleeve,
                AlwaysFires().Next(FreshMatch(), new Ctx { OwnEffectiveGrip = 0 }).Habit.Id);

            CollectionAssert.AreEqual(
                new[] { HabitId.LegTechAfterShido, HabitId.TurtleWhenTired, HabitId.BigThrowOnGripEdge,
                        HabitId.SeekGripRightSleeve, HabitId.SeekGripLeftSleeve },
                s.Opponent.Habits.Select(h => h.Id).ToArray(),
                "목록 순서가 곧 규칙이다 — 좁은 조건부터");
        }

        /// <summary>
        /// 발화는 0.75 이지 1.0 이 아니다. 1.0 이면 확정 습관이 B 를 결정론으로 만들고
        /// 경기가 조회표가 된다.
        /// </summary>
        [Test]
        public void HabitFiresAtSeventyFivePercent_NotAlways()
        {
            var s = FreshMatch();
            var ctx = new Ctx { OwnEffectiveGrip = 2 };

            Assert.AreEqual(0.75f, Habit(s, HabitId.BigThrowOnGripEdge).FireProbability,
                "기본 발화확률은 0.75 다");

            // 상태를 매번 새로 만든다 — 같은 상태로 연속 호출하면 H1 의 쿨다운이 끼어든다.
            var missed = AlwaysMisses().Next(FreshMatch(), ctx);
            Assert.IsTrue(missed.ConditionHeld, "조건은 섰다");
            Assert.IsFalse(missed.Fired, "그런데 25% 쪽에 걸렸다 — 항상 발화하면 안 된다");

            Assert.IsTrue(AlwaysFires().Next(FreshMatch(), ctx).Fired, "같은 조건에서 통과하면 발화한다");

            // 경계: 0.75 는 통과가 아니다. 0.75 미만이라야 발화다.
            Assert.IsFalse(new HabitLadder(new FixedRandomSource(0.75f)).Next(FreshMatch(), ctx).Fired,
                "임계값 자체는 발화가 아니다");
        }

        /// <summary>
        /// H2 의 창은 <b>정확히 두 턴</b>이다. 지도 직후가 0 이므로 0·1 에서 서고
        /// 2 에서는 안 선다 — <c>&lt;= 2</c> 로 쓰면 세 턴이 되어 틀린다.
        /// </summary>
        [Test]
        public void H2Window_IsExactlyTwoTurns_ThirdTurnDoesNotFire()
        {
            var h2 = Habit(FreshMatch(), HabitId.LegTechAfterShido);

            Assert.IsTrue(h2.Condition(new Ctx { TurnsSinceOwnShido = 0 }), "지도 직후 = 첫 턴");
            Assert.IsTrue(h2.Condition(new Ctx { TurnsSinceOwnShido = 1 }), "그 다음 = 둘째 턴");
            Assert.IsFalse(h2.Condition(new Ctx { TurnsSinceOwnShido = 2 }), "셋째 턴은 창 밖이다");
            Assert.IsFalse(h2.Condition(new Ctx { TurnsSinceOwnShido = null }), "지도를 받은 적 없으면 안 선다");
        }

        /// <summary>
        /// H2 는 <b>진짜 시도</b>여야 한다 — 견제기가 아니다.
        ///
        /// 견제기는 판정을 발화하지 않는다. 그러면 읽어도 벌 방법이 없는 습관이 되고,
        /// 공략 대상이 되는 것이 습관의 존재 이유다. H1 은 카운터 준비로 되칠 수 있는데
        /// 견제기 H2 는 기다리는 것 외에 아무것도 초대하지 않는다.
        /// </summary>
        [Test]
        public void H2_FiresARealAttempt_NotAPoke()
        {
            var tech = Habit(FreshMatch(), HabitId.LegTechAfterShido).Action.Technique;

            Assert.IsNotNull(tech, "기술이 없으면 판정 자체가 안 난다");
            Assert.AreEqual(ActionCost.BigThrow, tech.Cost, "견제기(Poke)면 판정이 안 뜨고 공략 불가가 된다");
            Assert.AreNotEqual(ActionCost.Poke, tech.Cost);
            Assert.AreEqual(TechniqueClass.Standard, tech.Class, "어설픈(0) 급이면 만회가 안 된다");
            Assert.AreEqual("안뒤축감아치기", tech.Id, "m0 이 B 에게 준 2지선다 짝이다");
        }

        /// <summary>
        /// H3 은 단순 열세로 서지 않는다. 단순 열세는 턴의 절반쯤 참이라 B 가 영구히
        /// 틀어박히고 상대가 샌드백이 된다.
        /// </summary>
        [Test]
        public void H3_NeedsTiredFloor_NotMerelyBehindOnStamina()
        {
            var h3 = Habit(FreshMatch(), HabitId.TurtleWhenTired);

            Assert.IsFalse(h3.Condition(new Ctx { OwnStamina = 80, OpponentStamina = 90 }),
                "뒤지긴 했지만 50 위다 — 서면 안 된다");
            Assert.IsFalse(h3.Condition(new Ctx { OwnStamina = 40, OpponentStamina = 30 }),
                "지쳤지만 상대가 더 지쳤다 — 서면 안 된다");
            Assert.IsTrue(h3.Condition(new Ctx { OwnStamina = 40, OpponentStamina = 60 }),
                "50 아래이고 상대보다 낮을 때만 선다");
            Assert.AreEqual(50, Cfg.StaminaBonusTiredThreshold,
                "문턱은 §4 가 이미 쓰는 구간이다 — 새 숫자를 만들지 않았다");
        }

        /// <summary>
        /// §2 극단적 방어는 <b>세 축 전부</b>다 — 손 회피 + 코어 수비 + 하체 수비.
        /// 하나라도 빠지면 §6.1 사다리에서 +3 rung 이 아니라 다른 칸으로 떨어지고,
        /// H3 이 시계를 태우려던 목적 자체가 사라진다.
        /// </summary>
        [Test]
        public void H3_CommitIsTheFullExtremeDefenseTriple_NotJustADefensiveCore()
        {
            var commit = Habit(FreshMatch(), HabitId.TurtleWhenTired).Action.Commit;

            Assert.AreEqual(HandTarget.Evade, commit.Hand);
            Assert.AreEqual(CoreStance.Defensive, commit.Core);
            Assert.AreEqual(LegAction.Defend, commit.Legs);
            Assert.IsTrue(commit.IsExtremeDefense, "세 축이 다 서야 §6.1 +3 rung 이다");
            Assert.IsNull(Habit(FreshMatch(), HabitId.TurtleWhenTired).Action.Technique,
                "틀어박히는 턴에 기술을 내면 안 된다");
        }

        // ── §4.2 폐기와 교체 ────────────────────────────────────────────────────

        /// <summary>
        /// 피해 2회에 폐기되고 <b>그 순간 예비가 점등</b>된다. 교체가 없으면 교정당한
        /// 상대가 잡음으로 퇴화하고 읽을 것이 사라진다.
        /// </summary>
        [Test]
        public void TwoDamagingHits_RetiresHabit_AndLightsReserve()
        {
            var s = FreshMatch();
            var h1 = Habit(s, HabitId.BigThrowOnGripEdge);
            var h3 = Habit(s, HabitId.TurtleWhenTired);
            var confirmed = ConfirmedLedger(s, HabitId.BigThrowOnGripEdge);

            Assert.IsTrue(h3.IsReserve, "전제: H3 은 예비다");
            Assert.IsFalse(s.IsLit(h3), "전제: 예비는 처음엔 꺼져 있다");
            Assert.IsTrue(s.IsActive(h1), "전제: H1 은 처음부터 산다");

            s.RegisterDamage(HabitId.BigThrowOnGripEdge, confirmed);
            Assert.IsFalse(s.IsRetired(HabitId.BigThrowOnGripEdge), "1회로는 폐기되지 않는다 — 임계는 2 다");
            Assert.IsFalse(s.IsLit(h3), "폐기 전에 예비가 켜지면 안 된다");

            s.RegisterDamage(HabitId.BigThrowOnGripEdge, confirmed);
            Assert.IsTrue(s.IsRetired(HabitId.BigThrowOnGripEdge), "2회에 폐기된다");
            Assert.IsTrue(s.IsLit(h3), "폐기가 예비를 점등한다");
            Assert.IsTrue(s.IsActive(h3), "점등된 예비는 이제 사다리에 오른다");
        }

        /// 폐기는 영구다. 조건이 다시 서도 사다리에 오르지 않는다.
        [Test]
        public void RetiredHabitNeverFiresAgain()
        {
            var s = FreshMatch();
            var ctx = new Ctx { OwnEffectiveGrip = 2 };
            var confirmed = ConfirmedLedger(s, HabitId.BigThrowOnGripEdge);

            Assert.AreEqual(HabitId.BigThrowOnGripEdge, AlwaysFires().Next(s, ctx).Habit.Id,
                "전제: 폐기 전에는 H1 이 나온다");

            s.RegisterDamage(HabitId.BigThrowOnGripEdge, confirmed);
            s.RegisterDamage(HabitId.BigThrowOnGripEdge, confirmed);

            Assert.IsTrue(Habit(s, HabitId.BigThrowOnGripEdge).Condition(ctx),
                "조건 자체는 여전히 선다 — 막는 것은 폐기다");

            Assert.IsFalse(AlwaysFires().Next(s, ctx).ConditionHeld,
                "폐기된 습관은 조건이 서도 나오지 않는다 (다른 습관 조건은 이 컨텍스트에서 안 선다)");

            // 세 번째 피해가 카운터를 더 올려 되살아나거나 하지 않는다.
            s.RegisterDamage(HabitId.BigThrowOnGripEdge, confirmed);
            Assert.IsTrue(s.IsRetired(HabitId.BigThrowOnGripEdge));
            Assert.AreEqual(HabitState.RetireAtDamage, s.Damage(HabitId.BigThrowOnGripEdge),
                "폐기 후에는 카운터도 더 오르지 않는다");
        }

        /// <summary>
        /// 말아업어치기는 습관이 아니라 하드 규칙이다 (체력 &lt;30, 경기당 1회).
        /// 사전에 관측될 수 없고 수 노트에 남지 않는다 — 습관 목록에 있으면 그 순간
        /// 읽기의 대상이 되어버린다 (<c>MATCH_RULES §7</c>).
        /// </summary>
        [Test]
        public void SurpriseTechIsNotAHabit_AndIsNeverLogged()
        {
            var opp = Jindoyun.Create(Cfg);

            Assert.AreEqual(5, opp.Habits.Count, "진도윤의 습관은 H1·H2·H3·H4·H4b 다섯이다");
            CollectionAssert.AreEquivalent(
                new[] { HabitId.BigThrowOnGripEdge, HabitId.LegTechAfterShido, HabitId.TurtleWhenTired,
                        HabitId.SeekGripRightSleeve, HabitId.SeekGripLeftSleeve },
                opp.Habits.Select(h => h.Id).ToArray());

            Assert.IsFalse(opp.Habits.Any(h => h.Action.Technique != null
                                            && h.Action.Technique.Id.Contains("말아업어치기")),
                "기습기가 습관 행동으로 들어가 있으면 안 된다");

            // 원장은 HabitId 로만 센다 — 기습기를 셀 칸이 애초에 없다.
            Assert.AreEqual(5, Enum.GetValues(typeof(HabitId)).Length,
                "HabitId 에 기습기 항목이 생기면 그 순간 관측 대상이 된다");
        }

        // ── §2 관측 원장 ────────────────────────────────────────────────────────

        /// <summary>
        /// 관측은 <b>실제로 실행된</b> 턴만 센다. 조건만 서고 B 가 딴짓을 한 턴은 0 이다 —
        /// 안 일어난 일을 세면 플레이어가 없는 습관을 읽게 된다.
        /// </summary>
        [Test]
        public void ObservationRequiresHabitActuallyFired_NotJustConditionHeld()
        {
            var s = FreshMatch();
            var ledger = new ObservationLedger();
            // 잡기 0 → H4(쿨다운 없음)가 후보다. H1 은 쿨다운 3 이라 연속 호출에 안 맞는다.
            var ctx = new Ctx { OwnEffectiveGrip = 0 };

            var missed = AlwaysMisses().Next(s, ctx);
            ledger.Record(missed);
            Assert.AreEqual(HabitId.SeekGripRightSleeve, missed.Habit.Id, "전제: H4 가 후보다");
            Assert.IsTrue(missed.ConditionHeld, "전제: 조건은 섰다");
            Assert.AreEqual(0, ledger.Observations(HabitId.SeekGripRightSleeve),
                "빗나간 턴은 관측이 아니다 — 조건 성립만으로 세면 안 된다");

            ledger.Record(AlwaysFires().Next(s, ctx));
            Assert.AreEqual(1, ledger.Observations(HabitId.SeekGripRightSleeve), "실행된 턴만 +1");

            ledger.Record(AlwaysFires().Next(s, ctx));
            Assert.AreEqual(2, ledger.Observations(HabitId.SeekGripRightSleeve));
            Assert.IsFalse(ledger.IsConfirmed(HabitId.SeekGripRightSleeve), "2회는 아직 심증이다");

            ledger.Record(AlwaysFires().Next(s, ctx));
            Assert.IsTrue(ledger.IsConfirmed(HabitId.SeekGripRightSleeve), "3회면 수 노트에 오른다");

            // 다른 습관 칸은 하나도 안 올랐다.
            Assert.AreEqual(0, ledger.Observations(HabitId.BigThrowOnGripEdge));
            Assert.AreEqual(0, ledger.Observations(HabitId.LegTechAfterShido));
        }

        /// <summary>
        /// §4.1 쿨다운 — 매 턴 나오는 신호는 읽을 값이 없다. H1 은 3턴에 한 번까지다.
        /// 쿨다운 중에는 <b>건너뛰고 사다리 다음 칸으로 간다</b> — 합산도, 정지도 아니다.
        /// </summary>
        [Test]
        public void HabitOnCooldown_IsSkipped_LadderFallsThroughToTheNextRung()
        {
            var s = FreshMatch();
            var ladder = AlwaysFires();
            var gripEdge = new Ctx { OwnEffectiveGrip = 2 };

            Assert.AreEqual(3, Habit(s, HabitId.BigThrowOnGripEdge).MinTurnsBetweenFirings);

            Assert.AreEqual(HabitId.BigThrowOnGripEdge, ladder.Next(s, gripEdge).Habit.Id, "턴 0: 발화");
            Assert.IsFalse(ladder.Next(s, gripEdge).ConditionHeld, "턴 1: 쿨다운 — 다른 후보도 없다");
            Assert.IsFalse(ladder.Next(s, gripEdge).ConditionHeld, "턴 2: 아직 쿨다운");
            Assert.AreEqual(HabitId.BigThrowOnGripEdge, ladder.Next(s, gripEdge).Habit.Id, "턴 3: 다시 뜬다");

            // 쿨다운은 막을 뿐 사다리를 멈추지 않는다 — 아래 칸이 조건을 만족하면 그게 나온다.
            // 잡기가 우위면 H1 이 후보인데, 쿨다운에 걸리면 H4 로 안 내려간다(조건 배타).
            // 대신 잡기를 내주면 H4 가 받는다.
            var fresh = FreshMatch();
            Assert.AreEqual(HabitId.BigThrowOnGripEdge,
                AlwaysFires().Next(fresh, new Ctx { OwnEffectiveGrip = 2 }).Habit.Id, "턴 0: H1");
            Assert.AreEqual(HabitId.SeekGripRightSleeve,
                AlwaysFires().Next(fresh, new Ctx { OwnEffectiveGrip = 0 }).Habit.Id,
                "턴 1: H1 은 쿨다운이고 잡기도 열세다 — H4 가 받는다");
        }

        /// <summary>
        /// §4.2 선행조건 — <b>확정 전의 피해는 안 센다.</b>
        ///
        /// 없으면 루프가 산술적으로 완주 불가다: 확정은 4번째 발화가 필요한데 폐기는
        /// 2회면 되므로, 벌줄 수 있는 습관은 읽히기 전에 먼저 사라진다. 실측에서 H4 가
        /// 정확히 그렇게 관측 3회에서 폐기됐다.
        /// </summary>
        [Test]
        public void DamageBeforeConfirmation_DoesNotCount_OtherwiseTheLoopCannotFinish()
        {
            var s = FreshMatch();
            var unknown = new ObservationLedger();

            s.RegisterDamage(HabitId.BigThrowOnGripEdge, unknown);
            s.RegisterDamage(HabitId.BigThrowOnGripEdge, unknown);
            s.RegisterDamage(HabitId.BigThrowOnGripEdge, unknown);
            Assert.AreEqual(0, s.Damage(HabitId.BigThrowOnGripEdge),
                "A 가 모르는 습관은 벌받아도 카운터가 안 오른다 — 읽은 게 아니라 운이다");
            Assert.IsFalse(s.IsRetired(HabitId.BigThrowOnGripEdge), "따라서 폐기되지도 않는다");

            // 심증(2회)까지도 아직 아니다.
            var suspected = new ObservationLedger();
            var d = new HabitDecision(Habit(s, HabitId.BigThrowOnGripEdge), fired: true);
            suspected.Record(d);
            suspected.Record(d);
            s.RegisterDamage(HabitId.BigThrowOnGripEdge, suspected);
            Assert.AreEqual(0, s.Damage(HabitId.BigThrowOnGripEdge), "심증만으로는 B 가 읽혔다고 못 느낀다");

            // 확정된 뒤부터 센다.
            suspected.Record(d);
            Assert.IsTrue(suspected.IsConfirmed(HabitId.BigThrowOnGripEdge), "전제: 이제 확정이다");
            s.RegisterDamage(HabitId.BigThrowOnGripEdge, suspected);
            s.RegisterDamage(HabitId.BigThrowOnGripEdge, suspected);
            Assert.IsTrue(s.IsRetired(HabitId.BigThrowOnGripEdge), "확정 뒤 피해 2회면 폐기된다");
        }

        // ── 정의 / 상태 경계 ────────────────────────────────────────────────────

        /// <summary>
        /// 정의 타입에 상태를 넣을 자리가 <b>없어야</b> 한다.
        ///
        /// Unity 의 SO 는 플레이 모드 중 변경이 에셋에 남으므로, 정의가 피해 카운터를
        /// 들면 지난 경기의 폐기가 에러 없이 다음 경기로 따라붙는다. 테스트로 잡는 것보다
        /// 못 쓰게 만드는 쪽이 낫고, 이 테스트는 그 구조가 유지되는지를 본다.
        /// </summary>
        [Test]
        public void DefinitionTypesAreImmutable_SoASharedAssetCannotCarryStateBetweenMatches()
        {
            foreach (var t in new[] { typeof(HabitDefinition), typeof(OpponentDefinition), typeof(HabitAction) })
            {
                var writableFields = t.GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .Where(f => !f.IsInitOnly).Select(f => t.Name + "." + f.Name).ToArray();
                CollectionAssert.IsEmpty(writableFields,
                    "정의 타입의 공개 필드는 전부 readonly 여야 한다 — 여기가 열리면 경기 간 오염이 산다");

                var settableProps = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.GetSetMethod() != null).Select(p => t.Name + "." + p.Name).ToArray();
                CollectionAssert.IsEmpty(settableProps, "정의 타입에 공개 setter 가 있으면 안 된다");
            }
        }

        /// <summary>
        /// 같은 정의를 물려 써도 새 경기는 깨끗하다. SO 를 그대로 재사용하는 경우가
        /// 이것이다 — <c>Create</c> 를 다시 부르는 규약에 기대지 않는다.
        /// </summary>
        [Test]
        public void CreateReturnsFreshState_RetirementDoesNotLeakBetweenMatches()
        {
            var shared = Jindoyun.Create(Cfg);   // SO 한 장이 여러 경기에 물린 상황

            var match1 = new HabitState(shared);
            var confirmed = ConfirmedLedger(match1, HabitId.BigThrowOnGripEdge);
            match1.RegisterDamage(HabitId.BigThrowOnGripEdge, confirmed);
            match1.RegisterDamage(HabitId.BigThrowOnGripEdge, confirmed);
            Assert.IsTrue(match1.IsRetired(HabitId.BigThrowOnGripEdge), "전제: 첫 경기에서 폐기됐다");

            var match2 = new HabitState(shared);
            Assert.IsFalse(match2.IsRetired(HabitId.BigThrowOnGripEdge),
                "같은 정의를 물려 써도 다음 경기는 깨끗하게 시작한다");
            Assert.AreEqual(0, match2.Damage(HabitId.BigThrowOnGripEdge));
            Assert.IsFalse(match2.IsLit(Habit(match2, HabitId.TurtleWhenTired)),
                "예비도 다시 꺼져 있어야 한다");

            // 그리고 첫 경기 쪽은 영향을 안 받는다.
            Assert.IsTrue(match1.IsRetired(HabitId.BigThrowOnGripEdge));
        }

        // ── §4 3단 폴백 ─────────────────────────────────────────────────────────

        /// 폴백·게이트 면. 습관 면과 한 객체가 구현해도 되지만 타입은 둘이다.
        sealed class BrainCtx : IHabitContext, IBrainContext
        {
            public int OwnEffectiveGrip { get; set; }
            public int? TurnsSinceOwnShido { get; set; }
            public int OwnStamina { get; set; } = 100;
            public int OpponentStamina { get; set; } = 100;
            public int OwnShidoCount { get; set; }
            public int SecondsRemaining { get; set; } = 240;
            public bool OwnStaminaLockout { get; set; }
            public bool OwnHasLockedHand { get; set; }
        }

        /// <summary>
        /// <b>폴백은 큰기술을 내지 않는다.</b> 판정을 발화하는 수는 이유가 있는 수여야
        /// 하는데, 폴백은 정의상 고를 이유가 없는 턴이다. 거기서 나온 던지기는 아무도
        /// 읽을 수 없고, 실측에서 그런 던지기가 판정의 절반을 만들고 있었다.
        /// </summary>
        [Test]
        public void FallbackNeverCommitsABigThrow_OnlyGripWorkPokeOrNeutral()
        {
            // 습관이 하나도 없는 상대 — 사다리가 언제나 폴백까지 내려간다.
            var noHabits = new HabitState(new OpponentDefinition("no-habits", new HabitDefinition[0]));

            for (int roll = 0; roll < 40; roll++)
                foreach (var ctx in new[]
                {
                    new BrainCtx { OwnEffectiveGrip = -2 },
                    new BrainCtx { OwnEffectiveGrip = 0, OwnShidoCount = 2 },
                    new BrainCtx { OwnEffectiveGrip = 2, SecondsRemaining = 10 },
                    new BrainCtx { OwnEffectiveGrip = 1, OwnStamina = 10 },
                })
                {
                    var brain = new HabitBrainCommitSource(FighterId.B, noHabits, ctx, ctx,
                                                           new FixedRandomSource(0f, roll));
                    brain.TryNext(out _, out _);

                    Assert.IsTrue(brain.LastUsedFallback, "전제: 습관이 없으니 폴백이다");
                    if (brain.LastTechnique == null) continue;
                    Assert.AreEqual(ActionCost.Poke, brain.LastTechnique.Cost,
                        $"폴백이 큰기술을 냈다 (roll {roll}) — 읽을 수 없는 던지기가 된다");
                }
        }

        /// <summary>
        /// 위 규칙의 <b>진짜 목적</b>: B 의 모든 큰기술이 습관에서 나온다. 그래야 B 의
        /// 던지기가 전부 원리적으로 읽을 수 있는 대상이 된다.
        /// </summary>
        [Test]
        public void EveryBigThrowByB_ComesFromAHabit()
        {
            var s = FreshMatch();
            var rng = new SystemRandomSource(4242);
            int bigThrows = 0;

            for (int turn = 0; turn < 120; turn++)
            {
                var ctx = new BrainCtx
                {
                    OwnEffectiveGrip = (turn % 5) - 2,          // −2 … +2 를 훑는다
                    TurnsSinceOwnShido = turn % 7 == 0 ? 0 : (int?)null,
                    OwnStamina = 100 - turn % 60,
                    OwnShidoCount = turn % 11 == 0 ? 1 : 0,
                    SecondsRemaining = 240 - turn * 2,
                };
                var brain = new HabitBrainCommitSource(FighterId.B, s, ctx, ctx, rng);
                brain.TryNext(out _, out _);

                if (brain.LastTechnique == null || brain.LastTechnique.Cost != ActionCost.BigThrow) continue;

                bigThrows++;
                Assert.IsTrue(brain.LastDecision.Fired,
                    "큰기술이 습관 밖에서 나왔다 — 읽을 수 없는 던지기다");
                Assert.IsFalse(brain.LastUsedFallback);
            }

            Assert.Greater(bigThrows, 0, "전제: 이 구간에서 큰기술이 나오긴 했다");
        }

        /// §5 — 필드는 있고 값은 0 이다. 커리어 작업은 데이터를 바꾸지 코드를 바꾸지 않는다.
        [Test]
        public void Jindoyun_ShipsAtScoutingLevelZero()
        {
            Assert.AreEqual(0, Jindoyun.Create(Cfg).ScoutingLevel);
        }
    }
}
