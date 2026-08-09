using NUnit.Framework;
using CounterTime.Core;
using CounterTime.Core.Scenario;
using CounterTime.Game.Match;

namespace CounterTime.Game.Tests
{
    /// <summary>
    /// 노드 → 커밋 파싱의 <b>거부 경로</b>. 성공 경로는 MatchRunnerHandActionTests 가 이미 탄다.
    ///
    /// MonoBehaviour 를 안 쓰므로 dotnet 하니스에서도 돈다. Game.Tests 에 있는 이유는
    /// 대상인 ScenarioTurnChoices 가 Game 어셈블리에 살기 때문이지 Unity 가 필요해서가 아니다.
    /// </summary>
    public class ScenarioTurnChoicesTests
    {
        static ScenarioNode Node(string title, string border) =>
            new ScenarioNode { id = "x1", title = title, content = "본문", border = border };

        /// <summary>
        /// 파싱이 거부되면 out 커밋은 <b>중립</b>이어야 한다 — 틀린 답은 공격적·공격이다.
        ///
        /// <c>default(TurnChoice)</c> 가 그 틀린 답을 낸다: CoreStance.Aggressive 와
        /// LegAction.Attack 이 각각 enum 0 이다. 지금은 호출부 둘이 전부 bool 로 가드해서
        /// 새지 않지만, 가드 하나를 놓치는 날 그 값이 조용히 심판 누산기(§6.1)와
        /// 수비 자세(§4)에 들어간다. 에러가 안 나면서 틀리는 종류라 여기서 못 박는다.
        /// </summary>
        [Test]
        public void TryParse_OnRejection_LeavesNeutralChoice_NotAggressiveAttack()
        {
            // 행위자를 못 밝히는 노드 — 검정(서술) 테두리에 A:/B: 접두사도 없다.
            var rejected = Node("하지메", NodeKinds.NarrationBorder);

            Assert.IsFalse(ScenarioTurnChoices.TryParse(rejected, out _, out var choice),
                "전제: 이 노드는 행위자를 밝히지 않으므로 파싱이 거부된다");

            Assert.AreEqual(CoreStance.Neutral, choice.Core, "거부된 파스가 코어를 공격적으로 내면 안 된다");
            Assert.AreEqual(LegAction.Neutral, choice.Legs, "거부된 파스가 하체를 공격으로 내면 안 된다");
            Assert.AreEqual(HandTarget.None, choice.Hand);
            Assert.IsTrue(choice.IsAllNeutral);
        }

        /// 제목이 아예 없는 노드도 같은 거부 경로다 — 이른 return 이 하나 더 있다.
        [Test]
        public void TryParse_OnEmptyTitle_AlsoLeavesNeutralChoice()
        {
            Assert.IsFalse(ScenarioTurnChoices.TryParse(Node("", NodeKinds.ChoiceABorder), out _, out var choice));
            Assert.IsTrue(choice.IsAllNeutral, "제목 없는 노드도 중립을 내야 한다");
        }
    }
}
