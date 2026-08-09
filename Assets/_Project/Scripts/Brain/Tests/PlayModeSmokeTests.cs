using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CounterTime.Brain;
using CounterTime.Core;

namespace CounterTime.Brain.Tests
{
    /// <summary>
    /// 헤드리스 스모크 — A 커밋은 테스트가 공급하고 B 는 브레인이 둔다. 러너 배선은
    /// 6단계라 사람이 아직 플레이 모드로 못 돌린다. 여기서 보는 것은 두 가지다:
    /// 예외 없이 끝까지 도는가, 그리고 습관이 <b>실제로</b> 발화하는가.
    ///
    /// 계측 수치는 콘솔로 뽑아 사람이 읽는다. 튜닝은 사람이 정한다 — 이 테스트는
    /// 숫자를 판정하지 않고 '0이 아닌가' 정도만 본다.
    /// </summary>
    public class PlayModeSmokeTests
    {
        const int Exchanges = 50;

        sealed class Metrics
        {
            public readonly Dictionary<HabitId, int> Fires = new Dictionary<HabitId, int>();
            public readonly int[] Rows = new int[7];      // 1..6 사용
            public int Turns, FallbackTurns;
            public string Ending = "ran out of exchanges";
            public ObservationLedger Ledger;

            public void Fire(HabitId id) => Fires[id] = (Fires.TryGetValue(id, out int n) ? n : 0) + 1;

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.AppendLine($"  turns={Turns}  fallback={FallbackTurns} ({100f * FallbackTurns / Turns:0.0}%)  ended: {Ending}");
                foreach (HabitId id in Enum.GetValues(typeof(HabitId)))
                    sb.AppendLine($"  habit {id,-20} fired {(Fires.TryGetValue(id, out int n) ? n : 0)}"
                                + $"  observed {Ledger.Observations(id)}");
                for (int r = 1; r <= 6; r++) sb.AppendLine($"  ladder row {r}: {Rows[r]}");
                return sb.ToString();
            }
        }

        /// <summary>
        /// 사다리 행은 (공격자, ReadLevel) 조합으로 유일하게 결정된다 — 프로덕션 코드에
        /// 계측 필드를 심지 않으려고 여기서 되짚는다.
        /// B+GotRead=1 / A+FullRead=2 / A+Read=3 / B+(Read|FullRead)=4 / A+GotRead=5 / Neutral=6
        /// </summary>
        static int RowOf(FighterId attacker, ReadLevel read)
        {
            if (read == ReadLevel.Neutral) return 6;
            if (attacker == FighterId.B) return read == ReadLevel.GotRead ? 1 : 4;
            return read == ReadLevel.FullRead ? 2 : read == ReadLevel.Read ? 3 : 5;
        }

        /// <summary>한 경기를 굴린다. <paramref name="aCommit"/> 이 A 의 성향을 정한다.</summary>
        static Metrics RunMatch(Func<int, TurnChoice> aCommit, int seed)
        {
            var cfg = new MatchRulesConfig { ThresholdJitter = 0 };
            var rng = new SystemRandomSource(seed);
            var engine = new MatchEngine(cfg, RefereeProfile.Standard, rng);

            var opponent = Jindoyun.Create(cfg);
            var state = new HabitState(opponent);
            var ledger = new ObservationLedger();
            var window = new TendencyWindow();
            var resolver = new LiveReadResolver(ledger, window);
            var ctx = new MatchBrainContext(engine, FighterId.B);
            var brain = new HabitBrainCommitSource(FighterId.B, state, ctx, ctx, rng, resolver);

            var m = new Metrics { Ledger = ledger };

            for (int turn = 0; turn < Exchanges && !engine.Board.IsFinished; turn++)
            {
                var a = aCommit(turn);
                brain.TryNext(out _, out var b);

                // B 가 기술을 들었으면 B 가 공격자, 아니면 A 가 던진다 — 양방향을 다 태운다.
                bool bAttacks = brain.LastTechnique != null;
                var attacker = bAttacks ? FighterId.B : FighterId.A;
                var technique = bAttacks
                    ? brain.LastTechnique
                    : new TechniqueSpec("밭다리", ActionCost.BigThrow, TechniqueClass.Standard);

                var read = resolver.For(attacker, a, b);

                m.Turns++;
                if (brain.LastUsedFallback) m.FallbackTurns++;
                if (brain.LastDecision.Fired) m.Fire(brain.LastDecision.Habit.Id);
                m.Rows[RowOf(attacker, read)]++;

                var events = engine.ResolveExchange(a, b, new ExchangeScript
                {
                    Attacker = attacker,
                    Technique = technique,
                    Read = read,
                });

                // 관측: 실제로 발화한 턴만 센다 — 구조가 그걸 강제한다.
                ledger.Record(brain.LastDecision);

                // §4.2 피해: 습관이 발화한 턴에 A 가 유효 이상을 냈거나 되치기에 성공했다.
                if (brain.LastDecision.Fired)
                    foreach (var e in events)
                        if (e is JudgementScored j && j.ScoredBy == FighterId.A
                            && (j.IsCounter || j.Judgement != Judgement.None))
                            state.RegisterDamage(brain.LastDecision.Habit.Id);

                ctx.ObserveExchange(events);
                window.Record(a);      // 창은 지난 커밋만 든다 — 해결 뒤에 넣는다
            }

            if (engine.Board.IsFinished)
                m.Ending = $"match over, winner {(engine.Board.Winner.HasValue ? engine.Board.Winner.ToString() : "draw")}"
                         + $", clock {engine.Clock.RemainingSeconds}s left";

            return m;
        }

        static readonly TurnChoice Same =
            new TurnChoice(HandTarget.None, CoreStance.Aggressive, LegAction.Attack);

        static readonly TurnChoice[] Varied =
        {
            new TurnChoice(HandTarget.None, CoreStance.Aggressive, LegAction.Attack),
            new TurnChoice(HandTarget.RightSleeve, CoreStance.Neutral, LegAction.Neutral),
            new TurnChoice(HandTarget.None, CoreStance.Defensive, LegAction.Defend),
            new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.CounterReady),
        };

        [Test]
        public void HabitBrainPlaysAFullMatch_WithoutException()
        {
            Metrics repeating = null, varying = null;

            Assert.DoesNotThrow(() => repeating = RunMatch(_ => Same, seed: 1234),
                "A 가 같은 수만 반복해도 예외 없이 돌아야 한다");
            Assert.DoesNotThrow(() => varying = RunMatch(t => Varied[t % Varied.Length], seed: 1234),
                "A 가 변주해도 예외 없이 돌아야 한다");

            Console.WriteLine("=== A repeats one commit ===");
            Console.WriteLine(repeating.ToString());
            Console.WriteLine("=== A varies (4-cycle) ===");
            Console.WriteLine(varying.ToString());

            Assert.Greater(repeating.Turns, 0);
            Assert.Greater(varying.Turns, 0);
        }

        /// <summary>
        /// 습관이 한 경기에 <b>0회</b> 발화하면 읽기 시스템이 사실상 안 도는 것이다.
        /// 관측 3회(확정)가 한 경기 안에 성립 가능한지가 여기서 갈린다.
        /// </summary>
        [Test]
        public void HabitsActuallyFireOverAMatch_NotZeroTimes()
        {
            var m = RunMatch(t => Varied[t % Varied.Length], seed: 1234);
            Console.WriteLine(m.ToString());

            Assert.Greater(m.Fires.Values.Sum(), 0, "한 경기에 습관이 한 번도 안 뜨면 읽을 것이 없다");
        }

        /// <summary>
        /// A 가 같은 수만 반복하면 5행(A 의 공격이 읽힘)이 실제로 발화해야 한다.
        /// 변주하면 안 나와야 한다 — 그 대비가 5행이 의도대로 도는지를 보여준다.
        /// </summary>
        [Test]
        public void RepeatingTheSameCommit_TriggersRowFive_VaryingDoesNot()
        {
            var repeating = RunMatch(_ => Same, seed: 1234);
            var varying = RunMatch(t => Varied[t % Varied.Length], seed: 1234);

            Assert.Greater(repeating.Rows[5], 0, "반복하는 A 는 자기 공격이 읽혀야 한다");
            Assert.AreEqual(0, varying.Rows[5], "4주기로 변주하면 3연속이 안 서므로 5행이 안 나온다");
        }
    }
}
