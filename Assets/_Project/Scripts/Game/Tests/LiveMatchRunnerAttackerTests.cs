using NUnit.Framework;
using CounterTime.Core;
using CounterTime.Game.Match;
using UnityEngine;

namespace CounterTime.Game.Tests
{
    /// <summary>
    /// §H 2단계 — 라이브 경로 공격자 판정 통일("기술을 명시적으로 지정한 커밋만
    /// 공격") + §A.2 확정(기술 턴은 (Core,Legs) 성향 채널에서 제외).
    ///
    /// <see cref="LiveMatchRunner"/> 는 MonoBehaviour 라 dotnet 하니스가 못 돌린다
    /// (GameObject/AddComponent 가 필요 — <c>MatchRunnerHandActionTests</c> 와 같은
    /// 사유, Harness.csproj 주석 참조). Unity Test Runner 전용.
    ///
    /// B 는 습관 브레인이라 매 교환 결정에 RNG 가 섞인다. 새 매치 시작 직후에는
    /// H1(그립 우위 필요)·H2(최근 지도 필요) 조건이 안 서고 H4(소매 노림, 기술 없음)만
    /// 서므로 B 가 이 exchange 에서 기술을 내는 경로는 오직 폴백(3단)의 견제기
    /// 뿐이다 — 낮은 확률이지만 0은 아니라서, 각 테스트는 "B 가 이번 교환에
    /// 선행했는가"(<see cref="LiveMatchRunner.APreempted"/> 또는 판정 이벤트 발생
    /// 여부)로 그 교환을 스스로 감지하고 재시도한다. 재시도 상한 안에서 통과하지
    /// 못하면 그 자체가 실패 — 조용히 무한 재시도하지 않는다.
    /// </summary>
    public class LiveMatchRunnerAttackerTests
    {
        const int MaxTries = 15;

        GameObject _go;
        LiveMatchRunner _runner;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("live-runner-under-test");
            _runner = _go.AddComponent<LiveMatchRunner>();
            _runner.Build();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        static readonly TurnChoice AttackNoOtherAxis =
            new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.Attack);

        [Test]
        public void LegsAttack_WithoutTechniqueId_NeverCommitsAnAttackForA()
        {
            for (int i = 0; i < MaxTries && !_runner.Engine.Board.IsFinished; i++)
            {
                bool anyJudgement = false;
                void OnPresented(MatchEvent e) { if (e is JudgementScored) anyJudgement = true; }
                _runner.Presented += OnPresented;

                int before = _runner.Engine.Stamina.Get(FighterId.A);
                _runner.SubmitA(AttackNoOtherAxis);   // techniqueId 생략 — 기본값 null
                int after = _runner.Engine.Stamina.Get(FighterId.A);

                _runner.Presented -= OnPresented;

                if (anyJudgement) continue;   // B 가 이번 교환에 기술을 냈다 — 이 교환으론 못 잰다, 다음 교환 재시도

                Assert.AreEqual(before, after,
                    "기술 미지정이면 A 는 이번 교환에 절대 공격자(또는 그 결과 방어자)가 될 수 없다 — 스태미나가 안 움직여야 한다");
                return;
            }
            Assert.Fail($"{MaxTries}번 안에 B 가 기술을 안 낸 교환을 못 만났다 — 재검토 필요");
        }

        [Test]
        public void LegsAttack_WithValidTechniqueId_CommitsAsAttacker_WhenNotPreempted()
        {
            bool preempted = false;
            _runner.APreempted += () => preempted = true;

            for (int i = 0; i < MaxTries && !_runner.Engine.Board.IsFinished; i++)
            {
                preempted = false;
                int before = _runner.Engine.Stamina.Get(FighterId.A);
                _runner.SubmitA(AttackNoOtherAxis, "밭다리걸기");
                int after = _runner.Engine.Stamina.Get(FighterId.A);

                if (preempted) continue;   // B 가 선행했다 — 이 교환으론 A 의 공격 성립을 못 잰다

                Assert.Less(after, before,
                    "선점당하지 않았다면 A 의 기술 지정 공격이 엔진에 도달해 스태미나를 지불했어야 한다");
                return;
            }
            Assert.Fail($"{MaxTries}번 안에 B 에게 선점당하지 않은 교환을 못 만났다 — 재검토 필요");
        }

        [Test]
        public void LegsAttack_UnknownTechniqueId_BehavesAsNoCommit()
        {
            for (int i = 0; i < MaxTries && !_runner.Engine.Board.IsFinished; i++)
            {
                bool anyJudgement = false;
                void OnPresented(MatchEvent e) { if (e is JudgementScored) anyJudgement = true; }
                _runner.Presented += OnPresented;

                int before = _runner.Engine.Stamina.Get(FighterId.A);
                _runner.SubmitA(AttackNoOtherAxis, "존재하지 않는 기술");
                int after = _runner.Engine.Stamina.Get(FighterId.A);

                _runner.Presented -= OnPresented;

                if (anyJudgement) continue;

                Assert.AreEqual(before, after, "레지스트리에 없는 id 는 기술 미지정과 동일하게 취급돼야 한다");
                return;
            }
            Assert.Fail($"{MaxTries}번 안에 B 가 기술을 안 낸 교환을 못 만났다 — 재검토 필요");
        }

        [Test]
        public void TechniqueDeclareTurn_IsExcludedFromTendencyWindow_ButOrdinaryTurnsAreRecorded()
        {
            Assert.AreEqual(0, _runner.Tendency.Count);

            bool preempted = false;
            _runner.APreempted += () => preempted = true;

            int recordedBeforeAttackAttempts = -1;
            for (int i = 0; i < MaxTries && !_runner.Engine.Board.IsFinished; i++)
            {
                preempted = false;
                int countBefore = _runner.Tendency.Count;
                _runner.SubmitA(AttackNoOtherAxis, "밭다리걸기");
                if (preempted) continue;   // 이 교환은 애초에 기술 턴이 아니게 됐다 — 재시도

                Assert.AreEqual(countBefore, _runner.Tendency.Count,
                    "기술 턴(§A.2)은 성향 창에 기록되면 안 된다");
                recordedBeforeAttackAttempts = countBefore;
                break;
            }
            Assert.AreNotEqual(-1, recordedBeforeAttackAttempts, $"{MaxTries}번 안에 성공한 기술 턴을 못 만났다");

            int beforeNeutral = _runner.Tendency.Count;
            _runner.SubmitA(TurnChoice.Neutral);
            Assert.AreEqual(beforeNeutral + 1, _runner.Tendency.Count,
                "기술 턴이 아닌 보통 턴은 그대로 기록돼야 한다 — 제외 규칙이 전체를 막은 게 아니라 기술 턴만 막은 것인지 확인");
        }

        /// 2026-08-14 밤 7차 — 교환 시간 라이브 전용 ⚙(기본 8초) 확인. 스크립트
        /// 경로(42노드, ExchangeSec=3)는 이 값을 참조조차 안 하므로 지문과 무관 —
        /// 이 테스트는 라이브 전용이라 애초에 하니스 대상도 아니다.
        [Test]
        public void ExchangeAdvancesClockByLiveExchangeSec_NotTheScriptedThreeSeconds()
        {
            Assert.AreEqual(8, _runner.Config.LiveExchangeSec, "기본값 확인");

            int before = _runner.Engine.Clock.RemainingSeconds;
            _runner.SubmitA(TurnChoice.Neutral);
            int after = _runner.Engine.Clock.RemainingSeconds;

            Assert.AreEqual(_runner.Config.LiveExchangeSec, before - after,
                "라이브 교환은 §2 스크립트 경로의 ExchangeSec(3)이 아니라 LiveExchangeSec(8)만큼 시계를 깎아야 한다");
        }
    }
}
