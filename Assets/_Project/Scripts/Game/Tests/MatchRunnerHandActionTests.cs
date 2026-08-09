using NUnit.Framework;
using CounterTime.Core;
using CounterTime.Game.Match;
using UnityEngine;

namespace CounterTime.Game.Tests
{
    /// <summary>
    /// 자세는 상태고 손 행동은 사건이다 — 사건은 선언한 교환에서 한 번만 일어난다.
    ///
    /// 이 두 테스트가 §3.2 뜯기 수싸움의 양쪽 절벽을 막는다. 손 행동이 유지되면 뜯어낸
    /// 그립이 같은 교환에서 도로 잡히고, 잡기 유지가 유지되면 그 뒤 모든 뜯기가 영구히
    /// 실패한다. 방향만 반대일 뿐 같은 버그다.
    ///
    /// Core 가 아니라 여기 있는 이유: 유지·소비는 러너의 규칙이고 Core 는 매 교환
    /// 주어진 커밋을 그대로 처리할 뿐이다.
    /// </summary>
    public class MatchRunnerHandActionTests
    {
        // 실제 시나리오에 기대지 않는 최소 그래프. 필드명은 ScenarioData 와 1:1.
        const string Json = @"{
          ""nodes"": [
            { ""id"": ""t1"", ""title"": ""시작"", ""content"": ""하지메."", ""x"": 0, ""y"": 0, ""border"": ""#000000"" },
            { ""id"": ""t2"", ""title"": ""A: 왼손→B 오른소매 / 코어 중립 / 하체 중립"", ""content"": ""소매를 잡는다."", ""x"": 0, ""y"": 1, ""border"": ""#2f6bad"" },
            { ""id"": ""t3"", ""title"": ""B: 손을 걷어 A 왼손 포획"", ""content"": ""A 의 왼손을 낚아챈다."", ""x"": 0, ""y"": 2, ""border"": ""#c9402f"" },
            { ""id"": ""t4"", ""title"": ""B: 잡기 유지"", ""content"": ""버틴다."", ""x"": 0, ""y"": 3, ""border"": ""#c9402f"" },
            { ""id"": ""t5"", ""title"": ""A: 즉시 뜯기"", ""content"": ""잡힌 왼손을 비틀어 뺀다."", ""x"": 0, ""y"": 4, ""border"": ""#2f6bad"" }
          ],
          ""edges"": [
            { ""from"": ""t1"", ""to"": ""t2"" }, { ""from"": ""t2"", ""to"": ""t3"" },
            { ""from"": ""t3"", ""to"": ""t4"" }, { ""from"": ""t4"", ""to"": ""t5"" }
          ]
        }";

        GameObject _go;
        MatchRunner _runner;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("runner-under-test");
            _runner = _go.AddComponent<MatchRunner>();
            _runner.Build(new TextAsset(Json));
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        void Walk(params string[] ids)
        {
            foreach (var id in ids) _runner.EnterNode(id);
        }

        /// B 가 A 의 왼손을 잡고 있고 A 가 뜯어낸다. B 의 잡기 선언은 이전 교환의 사건이므로
        /// 이 교환에서 다시 실행되지 않는다 — 뜯어낸 그립이 같은 교환에서 도로 잡히면
        /// §3.2 가 통째로 무효가 된다.
        [Test]
        public void TearSucceeds_HolderDoesNotReGripInTheSameExchange()
        {
            Walk("t1", "t2", "t3");
            Assert.AreEqual(GripLock.Held, _runner.Engine.Grip.LockState(FighterId.A, Hand.Left),
                "전제: B 가 A 의 왼손을 잡고 있다");
            Assert.AreEqual(0, _runner.Engine.Grip.GripScore(FighterId.A), "전제: 서로 하나씩");

            Walk("t5");   // t4(잡기 유지) 를 건너뛰고 바로 뜯는다

            Assert.AreEqual(GripLock.Free, _runner.Engine.Grip.LockState(FighterId.A, Hand.Left),
                "뜯어냈으면 풀려 있어야 한다");
            Assert.AreEqual(1, _runner.Engine.Grip.GripScore(FighterId.A),
                "B 의 잡기는 사라지고 A 의 소매 잡기만 남는다 — 도로 잡히면 0 이 된다");
        }

        /// 잡기 유지는 뜯기가 오는 그 교환에만 의미가 있는 반응이다. 유지되면 그 뒤 모든
        /// 뜯기가 Locked 로 떨어져 뜯기 수싸움이 반대 방향으로 죽는다.
        [Test]
        public void MaintainGrip_DoesNotPersist_LaterTearCanStillSucceed()
        {
            Walk("t1", "t2", "t3", "t4");   // t4 에서 B 가 잡기 유지를 선언한다
            Assert.AreEqual(GripLock.Held, _runner.Engine.Grip.LockState(FighterId.A, Hand.Left));

            Walk("t5");   // 다음 교환의 뜯기 — 여기엔 유지가 안 걸려 있다

            Assert.AreEqual(GripLock.Free, _runner.Engine.Grip.LockState(FighterId.A, Hand.Left),
                "지난 턴의 잡기 유지가 이번 턴 뜯기를 막으면 안 된다");
            Assert.AreEqual(1, _runner.Engine.Grip.GripScore(FighterId.A));
        }

        /// 회피는 사건이 아니라 자세다 — 유지돼야 극단적 방어가 §6.1 에서 턴당 +3 을 받는다.
        [Test]
        public void Evade_IsStanceNotAction_AndKeepsPersisting()
        {
            Assert.IsFalse(new TurnChoice(HandTarget.Evade, CoreStance.Defensive, LegAction.Defend)
                .HasHandAction, "회피는 손을 쓰지 않는 자세다");
            Assert.IsTrue(new TurnChoice(HandTarget.MaintainGrip, CoreStance.Neutral, LegAction.Neutral)
                .HasHandAction, "잡기 유지는 사건이다");
        }
    }
}
