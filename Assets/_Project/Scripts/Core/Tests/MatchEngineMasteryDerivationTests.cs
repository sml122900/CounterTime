using System.Linq;
using NUnit.Framework;
using CounterTime.Core;

namespace CounterTime.Core.Tests
{
    /// <summary>
    /// §H 2단계 — <c>TechniqueSpec.AuthoredClass == null</c>(라이브 경로)이 실제로
    /// <see cref="MatchEngine"/> 안에서 <see cref="TechniqueRegistry.ClassFromMastery"/>
    /// 로 파생되는지 검증. 스크립트 경로(<c>ScriptedAssembler</c>)는 항상 AuthoredClass
    /// 를 채워 이 파생 로직을 안 탄다 — 그게 42노드 지문 보존의 계약이라 별도로 확인한다.
    /// </summary>
    public class MatchEngineMasteryDerivationTests
    {
        sealed class FixedMasteryStore : IMasteryStore
        {
            readonly int _level;
            public FixedMasteryStore(int level) => _level = level;
            public int LevelOf(FighterId who, string techId) => _level;
            public void Set(FighterId who, string techId, int level) { }
        }

        static ExchangeScript Throw(TechniqueClass? authoredClass) => new ExchangeScript
        {
            Attacker = FighterId.A,
            Technique = new TechniqueSpec("밭다리걸기", ActionCost.BigThrow, authoredClass),
            Read = ReadLevel.Neutral,
        };

        [Test]
        public void NullAuthoredClass_WithDefaultZeroMasteryStub_DerivesSloppy()
        {
            var cfg = new MatchRulesConfig { ThresholdJitter = 0 };
            var e = new MatchEngine(cfg, RefereeProfile.Standard, new SystemRandomSource(1));

            var ev = e.ResolveExchange(TurnChoice.Neutral, TurnChoice.Neutral, Throw(null));

            Assert.AreEqual((int)TechniqueClass.Sloppy, ev.OfType<JudgementScored>().Single().Terms[3],
                "masteryStore 를 안 넘기면 ZeroMasteryStore 기본값(항상 0)이 쓰이고, 0 은 0-1 경계라 Sloppy 다");
        }

        [Test]
        public void NullAuthoredClass_WithMasteryLevelFour_DerivesSignature()
        {
            var cfg = new MatchRulesConfig { ThresholdJitter = 0 };
            var e = new MatchEngine(cfg, RefereeProfile.Standard, new SystemRandomSource(1),
                new FixedMasteryStore(4));

            var ev = e.ResolveExchange(TurnChoice.Neutral, TurnChoice.Neutral, Throw(null));

            Assert.AreEqual((int)TechniqueClass.Signature, ev.OfType<JudgementScored>().Single().Terms[3]);
        }

        [Test]
        public void NonNullAuthoredClass_IgnoresMasteryStore_EvenIfNonzero()
        {
            var cfg = new MatchRulesConfig { ThresholdJitter = 0 };
            // masteryStore 가 있어도 Signature 로 파생될 수 있는 레벨을 일부러 준다 —
            // AuthoredClass 가 그걸 완전히 가려야 한다(스크립트 경로 지문 보존 계약).
            var e = new MatchEngine(cfg, RefereeProfile.Standard, new SystemRandomSource(1),
                new FixedMasteryStore(5));

            var ev = e.ResolveExchange(TurnChoice.Neutral, TurnChoice.Neutral, Throw(TechniqueClass.Standard));

            Assert.AreEqual((int)TechniqueClass.Standard, ev.OfType<JudgementScored>().Single().Terms[3],
                "AuthoredClass 가 non-null 이면 mastery store 는 안 본다");
        }
    }
}
