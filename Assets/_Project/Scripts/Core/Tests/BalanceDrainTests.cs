using System.Linq;
using NUnit.Framework;
using CounterTime.Core;

namespace CounterTime.Core.Tests
{
    /// <summary>
    /// §S/§H 3단계 — 흔들기 이외의 나머지 밸런스 드레인/회복 지점: 견제 발기술 floor,
    /// 무득점 진짜 시도, 수비 성공. 흔들기 자체(선언·게이트·부분 회복)는
    /// <c>SetupTests</c> 쪽. 전부 <c>BalanceGate</c> 로 게이트된다 — 꺼져 있으면 완전
    /// 비활성(42노드 지문 보존 전제, MatchEngine 주석 참조).
    /// </summary>
    public class BalanceDrainTests
    {
        MatchRulesConfig _cfg;
        MatchEngine _e;

        [SetUp]
        public void SetUp()
        {
            _cfg = new MatchRulesConfig { ThresholdJitter = 0, BalanceGate = true };
            _e = new MatchEngine(_cfg, RefereeProfile.Standard, new SystemRandomSource(1));
        }

        static readonly TurnChoice Idle = TurnChoice.Neutral;

        static ExchangeScript Poke(string id = "발목받치기") => new ExchangeScript
        {
            Attacker = FighterId.A,
            Technique = new TechniqueSpec(id, ActionCost.Poke, TechniqueClass.Sloppy),
            Read = ReadLevel.Neutral,
        };

        static ExchangeScript BigThrow(TechniqueClass cls, ReadLevel read = ReadLevel.Neutral) => new ExchangeScript
        {
            Attacker = FighterId.A,
            Technique = new TechniqueSpec("밭다리", ActionCost.BigThrow, cls),
            Read = read,
        };

        // ---- §D.4 floor — 견제 발기술 ----

        [Test]
        public void LandedPoke_DrainsDefenderBalance_ButFiresNoJudgement()
        {
            var ev = _e.ResolveExchange(Idle, Idle, Poke());

            Assert.IsEmpty(ev.OfType<JudgementScored>(), "견제 발기술은 판정을 발화하지 않는다 — §4/§5.1");
            Assert.AreEqual(_cfg.BalanceMax - _cfg.BalanceDrainFeintFootsweep, _e.Balance.Get(FighterId.B));
            var changed = ev.OfType<BalanceChanged>().SingleOrDefault();
            Assert.IsNotNull(changed);
            Assert.AreEqual(FighterId.B, changed.Who);
        }

        [Test]
        public void LandedPoke_WithGateOff_DoesNothingToBalance()
        {
            var cfg = new MatchRulesConfig { ThresholdJitter = 0 };   // BalanceGate 기본 false
            var e = new MatchEngine(cfg, RefereeProfile.Standard, new SystemRandomSource(1));

            var ev = e.ResolveExchange(Idle, Idle, Poke());

            Assert.IsEmpty(ev.OfType<BalanceChanged>(), "게이트가 꺼져 있으면 견제기도 밸런스를 안 건드린다");
            Assert.AreEqual(cfg.BalanceMax, e.Balance.Get(FighterId.B));
        }

        // ---- §S — 무득점 진짜 시도 ----

        [Test]
        public void RealAttemptScoringNone_DrainsDefenderBalance()
        {
            // grip 0, read Neutral, stam 0, Sloppy(0), posture 0 → sum=0 ⇒ None.
            var ev = _e.ResolveExchange(Idle, Idle, BigThrow(TechniqueClass.Sloppy));

            var scored = ev.OfType<JudgementScored>().Single();
            Assert.AreEqual(Judgement.None, scored.Judgement, "전제: 이 교환은 무득점이어야 한다");
            Assert.AreEqual(_cfg.BalanceMax - _cfg.BalanceDrainThreatNoScore, _e.Balance.Get(FighterId.B),
                "무득점이어도 진짜 시도였으니 상대(방어자) 밸런스가 깎여야 한다");
        }

        [Test]
        public void ScoringAttempt_DoesNotDrainViaTheNoScorePath()
        {
            // grip 0, FullRead(+3), stam 0, Standard(1), posture 0 → sum=4 ⇒ Yuko.
            var ev = _e.ResolveExchange(Idle, Idle, BigThrow(TechniqueClass.Standard, ReadLevel.FullRead));

            var scored = ev.OfType<JudgementScored>().Single();
            Assert.AreNotEqual(Judgement.None, scored.Judgement, "전제: 이번엔 실제로 득점해야 한다");
            Assert.AreEqual(_cfg.BalanceMax, _e.Balance.Get(FighterId.B),
                "득점했으면 '무득점 진짜 시도' 드레인 경로를 안 탄다 — 방어자 밸런스는 그대로");
        }

        [Test]
        public void RealAttemptScoringNone_WithGateOff_DoesNothingToBalance()
        {
            var cfg = new MatchRulesConfig { ThresholdJitter = 0 };
            var e = new MatchEngine(cfg, RefereeProfile.Standard, new SystemRandomSource(1));

            var ev = e.ResolveExchange(Idle, Idle, BigThrow(TechniqueClass.Sloppy));

            Assert.IsEmpty(ev.OfType<BalanceChanged>(), "게이트가 꺼져 있으면 무득점 드레인도 없다");
        }

        // ---- §S — 수비 성공 ----

        [Test]
        public void CleanDefense_RecoversDefenderBalance()
        {
            // 먼저 방어자(B)의 밸런스를 깎아둔다 — 회복이 실제로 값을 올리는지 보려면
            // 상한(100)에 막혀 있으면 안 된다.
            _e.ResolveExchange(Idle, Idle, Poke());   // B: 100 -> 80 (BalanceDrainFeintFootsweep=20)

            // B 가 A 의 소매를 잡아 A 의 유효그립을 -1 로 만든다 — MATCH_RULES §4.1 canonical
            // case 4 그대로: grip 0 만으로는 카운터 준비(GotRead −3)가 −3 문턱을 못 넘는다
            // (case 5), grip −1 이 같이 있어야 −3 이하로 떨어져 실제 되치기가 난다.
            _e.ResolveExchange(Idle,
                new TurnChoice(HandTarget.RightSleeve, CoreStance.Neutral, LegAction.Neutral), new ExchangeScript());

            int beforeRecovery = _e.Balance.Get(FighterId.B);

            var counterReady = new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.CounterReady);
            var ev = _e.ResolveExchange(Idle, counterReady, BigThrow(TechniqueClass.Standard));

            var scored = ev.OfType<JudgementScored>().Single();
            Assert.IsTrue(scored.IsCounter, "전제: grip−1 + GotRead 로 −3 문턱을 넘겨 이 교환이 되치기여야 한다");
            // §4.3 "a score calls matte" — 되치기로 유효가 나면(§9.1 굳히기 진입 조건인
            // "방어자 체력<50"을 A 가 아직 못 채워 굳히기로는 안 들어간다) 이 교환 끝에
            // 그쳐가 자동으로 걸려 두 선수 다 BalanceRecoverMatte(+3)도 받는다 — 그래서
            // 기대값이 수비 성공분(+10) 하나가 아니라 둘의 합이다. 이건 실측으로 발견한
            // 사실이지 처음부터 노린 시나리오가 아니다.
            Assert.AreEqual(beforeRecovery + _cfg.BalanceRecoverDefense + _cfg.BalanceRecoverMatte,
                _e.Balance.Get(FighterId.B),
                "Clean 방어(읽기를 이겼다)로 방어자(B) 밸런스가 회복되고, 같은 교환의 자동 그쳐로 부분 회복도 더해진다");
            Assert.AreEqual(_cfg.BalanceMax, _e.Balance.Get(FighterId.A),
                "A 는 이번 교환에 안 깎였으니 그쳐로 +3 을 더 받아도 상한(100)에 막혀 그대로다");
        }
    }
}
