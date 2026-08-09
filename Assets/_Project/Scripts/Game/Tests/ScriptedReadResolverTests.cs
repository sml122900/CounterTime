using System.Linq;
using NUnit.Framework;
using CounterTime.Core;
using CounterTime.Core.Scenario;
using CounterTime.Game.Match;

namespace CounterTime.Game.Tests
{
    /// <summary>
    /// 읽기 값의 <b>소유권 경계</b>: 리졸버는 BASE 를 내고, §4.2 카운터 준비 감쇄는
    /// Core 가 건다. 양쪽에서 걸면 두 칸 내려가고 에러는 안 난다.
    ///
    /// 합성 노드를 쓴다. 42노드 시나리오에는 이 조합(방어자가 카운터 준비 + 공격자가
    /// 큰기술)이 <b>없다</b> — 카운터 준비를 언급하는 노드는 m11 하나뿐이고 거기서는
    /// A 가 선언자이자 공격자라 방어자 쪽 게이트가 열리지 않는다. 메인라인도
    /// CounterReadyApplied 를 한 번도 내지 않는다. 즉 이 계약을 지키는 회귀망은
    /// 시나리오 쪽에 없고 여기가 유일하다.
    ///
    /// MonoBehaviour 를 안 쓰므로 dotnet 하니스에서도 돈다.
    /// </summary>
    public class ScriptedReadResolverTests
    {
        // [판정식/A] 의 수싸움+2 가 BASE. 제목에 '·' 가 없으므로 1교환·큰기술이고,
        // 큰기술이어야 §4.2 게이트가 열린다.
        static ScenarioNode BigThrowReadTwo() => new ScenarioNode
        {
            id = "s1",
            title = "A: 업어치기",
            content = "[판정식/A] 잡기+0 +수싸움+2 +체력0 +기술+1 = 3",
            border = NodeKinds.ChoiceABorder,
        };

        [Test]
        public void ScriptedReadResolver_SuppliesBaseRead_NotStepDownAdjusted()
        {
            var cfg = new MatchRulesConfig();
            var assembled = new ScriptedAssembler(cfg, BigThrowReadTwo()).Assemble();
            var resolver = new ScriptedReadResolver(assembled);

            var attacker = TurnChoice.Neutral;
            var defenderCounterReady =
                new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.CounterReady);

            var supplied = resolver.For(FighterId.A, attacker, defenderCounterReady);

            Assert.AreEqual(ReadLevel.Read, supplied,
                "리졸버는 노드가 적은 BASE 값(+2)을 그대로 내야 한다");
            Assert.AreNotEqual(ReadLevel.Neutral, supplied,
                "이미 깎인 값(+2 → 0)을 내면 Core 가 한 번 더 깎아 이중 적용된다");
        }

        /// 위 계약의 뒷면 — Core 가 실제로 <b>한 번만</b> 깎는다. 리졸버가 선적용했다면
        /// 여기서 −3(읽힘)이 나오고, 그게 이중 적용의 육안 증거다.
        [Test]
        public void CoreAppliesStepDownExactlyOnce_OnTheResolversBaseRead()
        {
            var cfg = new MatchRulesConfig { ThresholdJitter = 0 };
            var assembled = new ScriptedAssembler(cfg, BigThrowReadTwo()).Assemble();
            var resolver = new ScriptedReadResolver(assembled);

            var attacker = TurnChoice.Neutral;
            var defenderCounterReady =
                new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.CounterReady);

            var script = assembled[0];
            script.Read = resolver.For(FighterId.A, attacker, defenderCounterReady);

            var engine = new MatchEngine(cfg, RefereeProfile.Standard, new SystemRandomSource(1));
            var applied = engine.ResolveExchange(attacker, defenderCounterReady, script)
                                .OfType<CounterReadyApplied>()
                                .Single();

            Assert.AreEqual(ReadLevel.Read, applied.AttackerReadBefore, "Core 가 받은 값이 BASE 여야 한다");
            Assert.AreEqual(ReadLevel.Neutral, applied.AttackerReadAfter, "한 칸만 내려간다 — 읽힘(−3)이 나오면 이중 적용이다");
        }
    }
}
