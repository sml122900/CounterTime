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

            Assert.AreEqual(HabitId.BigThrowOnGripEdge, d.Habit.Id, "목록에서 먼저인 H1 이 이긴다");
            Assert.IsTrue(d.Fired);
            Assert.AreEqual("업어치기", d.Habit.Action.Technique.Id,
                "H1 의 행동만 나온다 — H2 의 기술과 섞이지 않는다");
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

            var missed = AlwaysMisses().Next(s, ctx);
            Assert.IsTrue(missed.ConditionHeld, "조건은 섰다");
            Assert.IsFalse(missed.Fired, "그런데 25% 쪽에 걸렸다 — 항상 발화하면 안 된다");

            Assert.IsTrue(AlwaysFires().Next(s, ctx).Fired, "같은 조건에서 통과하면 발화한다");

            // 경계: 0.75 는 통과가 아니다. 0.75 미만이라야 발화다.
            Assert.IsFalse(new HabitLadder(new FixedRandomSource(0.75f)).Next(s, ctx).Fired,
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

            Assert.IsTrue(h3.IsReserve, "전제: H3 은 예비다");
            Assert.IsFalse(s.IsLit(h3), "전제: 예비는 처음엔 꺼져 있다");
            Assert.IsTrue(s.IsActive(h1), "전제: H1 은 처음부터 산다");

            s.RegisterDamage(HabitId.BigThrowOnGripEdge);
            Assert.IsFalse(s.IsRetired(HabitId.BigThrowOnGripEdge), "1회로는 폐기되지 않는다 — 임계는 2 다");
            Assert.IsFalse(s.IsLit(h3), "폐기 전에 예비가 켜지면 안 된다");

            s.RegisterDamage(HabitId.BigThrowOnGripEdge);
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

            Assert.AreEqual(HabitId.BigThrowOnGripEdge, AlwaysFires().Next(s, ctx).Habit.Id,
                "전제: 폐기 전에는 H1 이 나온다");

            s.RegisterDamage(HabitId.BigThrowOnGripEdge);
            s.RegisterDamage(HabitId.BigThrowOnGripEdge);

            Assert.IsTrue(Habit(s, HabitId.BigThrowOnGripEdge).Condition(ctx),
                "조건 자체는 여전히 선다 — 막는 것은 폐기다");

            Assert.IsFalse(AlwaysFires().Next(s, ctx).ConditionHeld,
                "폐기된 습관은 조건이 서도 나오지 않는다 (다른 습관 조건은 이 컨텍스트에서 안 선다)");

            // 세 번째 피해가 카운터를 더 올려 되살아나거나 하지 않는다.
            s.RegisterDamage(HabitId.BigThrowOnGripEdge);
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

            Assert.AreEqual(3, opp.Habits.Count, "진도윤의 습관은 H1·H2·H3 셋뿐이다");
            CollectionAssert.AreEquivalent(
                new[] { HabitId.BigThrowOnGripEdge, HabitId.LegTechAfterShido, HabitId.TurtleWhenTired },
                opp.Habits.Select(h => h.Id).ToArray());

            Assert.IsFalse(opp.Habits.Any(h => h.Action.Technique != null
                                            && h.Action.Technique.Id.Contains("말아업어치기")),
                "기습기가 습관 행동으로 들어가 있으면 안 된다");

            // 원장은 HabitId 로만 센다 — 기습기를 셀 칸이 애초에 없다.
            Assert.AreEqual(3, Enum.GetValues(typeof(HabitId)).Length,
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
            var ctx = new Ctx { OwnEffectiveGrip = 2 };

            var missed = AlwaysMisses().Next(s, ctx);
            ledger.Record(missed);
            Assert.IsTrue(missed.ConditionHeld, "전제: 조건은 섰다");
            Assert.AreEqual(0, ledger.Observations(HabitId.BigThrowOnGripEdge),
                "빗나간 턴은 관측이 아니다 — 조건 성립만으로 세면 안 된다");

            ledger.Record(AlwaysFires().Next(s, ctx));
            Assert.AreEqual(1, ledger.Observations(HabitId.BigThrowOnGripEdge), "실행된 턴만 +1");

            ledger.Record(AlwaysFires().Next(s, ctx));
            Assert.AreEqual(2, ledger.Observations(HabitId.BigThrowOnGripEdge));

            // 조건이 아무것도 안 서는 턴을 기록해도 아무 일도 안 난다.
            ledger.Record(AlwaysFires().Next(s, new Ctx()));
            Assert.AreEqual(2, ledger.Observations(HabitId.BigThrowOnGripEdge));
            Assert.AreEqual(0, ledger.Observations(HabitId.LegTechAfterShido));
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
            match1.RegisterDamage(HabitId.BigThrowOnGripEdge);
            match1.RegisterDamage(HabitId.BigThrowOnGripEdge);
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

        /// §5 — 필드는 있고 값은 0 이다. 커리어 작업은 데이터를 바꾸지 코드를 바꾸지 않는다.
        [Test]
        public void Jindoyun_ShipsAtScoutingLevelZero()
        {
            Assert.AreEqual(0, Jindoyun.Create(Cfg).ScoutingLevel);
        }
    }
}
