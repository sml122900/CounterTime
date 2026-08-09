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

            /// 판정 발화 — 등급별, 득점자별, 그리고 되치기 몇 건인지.
            public readonly Dictionary<Judgement, int> Judgements = new Dictionary<Judgement, int>();
            public readonly Dictionary<FighterId, int> ScoredBy = new Dictionary<FighterId, int>();
            public int Counters;

            /// §4.3 (v3.4) — 그쳐가 몇 번 났는가. 회복이 여기에 묶여 있다.
            public int Mattes;

            /// §9.1 (step 5, 야간 작업) — 굳히기 진입 횟수. 이 루프는 ResolveOsaekomiExchange
            /// 를 안 부른다(그 배선은 6단계) — 그래서 굳히기가 열려도 다음 교환은 평범한
            /// 서기 교환으로 처리된다. 숫자만 기록한다, 안 고친다.
            public int OsaekomiEntries;

            /// 각 습관이 몇 번째 교환에 확정에 닿았는가. 없으면 그 경기에 못 닿았다.
            public readonly Dictionary<HabitId, int> ConfirmedAt = new Dictionary<HabitId, int>();

            /// 5교환마다, 그리고 종료 시점의 양쪽 체력. §5.6 은 스팸 시퀀스 10점만
            /// 검증했고 한 경기 전체의 체력 경제는 아직 아무도 안 봤다.
            public readonly List<string> StaminaTrace = new List<string>();

            /// 2행(A 공격 + B 비공격 습관 + 확정)이 뜬 교환의 판정식 5항 그대로.
            public readonly List<string> RowTwoTerms = new List<string>();

            public void Fire(HabitId id) => Fires[id] = (Fires.TryGetValue(id, out int n) ? n : 0) + 1;

            public void Score(Judgement j, FighterId by)
            {
                Judgements[j] = (Judgements.TryGetValue(j, out int n) ? n : 0) + 1;
                ScoredBy[by] = (ScoredBy.TryGetValue(by, out int b) ? b : 0) + 1;
            }

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.AppendLine($"  turns={Turns}  fallback={FallbackTurns} ({100f * FallbackTurns / Turns:0.0}%)  mattes={Mattes}  osaekomiEntries={OsaekomiEntries}");
                sb.AppendLine($"  ending: {Ending}");
                foreach (HabitId id in Enum.GetValues(typeof(HabitId)))
                    sb.AppendLine($"  habit {id,-22} fired {(Fires.TryGetValue(id, out int n) ? n : 0)}"
                                + $"  observed {Ledger.Observations(id)}"
                                + $"  confirmed {(ConfirmedAt.TryGetValue(id, out int at) ? "at exchange " + at : "never")}");
                for (int r = 1; r <= 6; r++) sb.AppendLine($"  ladder row {r}: {Rows[r]}");
                sb.Append("  judgements:");
                foreach (Judgement j in Enum.GetValues(typeof(Judgement)))
                    sb.Append($" {j}={(Judgements.TryGetValue(j, out int c) ? c : 0)}");
                sb.AppendLine($"   (counters {Counters})");
                sb.AppendLine($"  scored by: A={(ScoredBy.TryGetValue(FighterId.A, out int sa) ? sa : 0)}"
                            + $"  B={(ScoredBy.TryGetValue(FighterId.B, out int sbb) ? sbb : 0)}");
                sb.AppendLine("  stamina: " + string.Join("  ", StaminaTrace));
                sb.AppendLine($"  row-2 judgements ({RowTwoTerms.Count}):");
                foreach (var t in RowTwoTerms) sb.AppendLine("    " + t);
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

                int row = RowOf(attacker, read);
                m.Rows[row]++;
                if (turn % 5 == 0)
                    m.StaminaTrace.Add($"[{turn}] A{engine.Stamina.Get(FighterId.A)}/B{engine.Stamina.Get(FighterId.B)}");

                var events = engine.ResolveExchange(a, b, new ExchangeScript
                {
                    Attacker = attacker,
                    Technique = technique,
                    Read = read,
                });

                foreach (var e in events)
                {
                    if (e is MatteCalled) m.Mattes++;
                    if (e is OsaekomiEntered) m.OsaekomiEntries++;
                    if (e is JudgementScored js)
                    {
                        m.Score(js.Judgement, js.ScoredBy);
                        if (js.IsCounter) m.Counters++;

                        // 2행이 뜬 교환의 판정식을 항 그대로 남긴다 — FullRead +3 인데
                        // 유효가 안 나면 다섯 항 중 어디서 깎였는지 봐야 한다.
                        if (row == 2)
                            m.RowTwoTerms.Add(
                                $"ex{turn}: terms[grip,read,stam,tech,posture]=[{string.Join(",", js.Terms)}]"
                                + $" sum={js.Sum} -> {js.Judgement} by {js.ScoredBy}"
                                + $" (counter={js.IsCounter}, Bhabit={(brain.LastDecision.ConditionHeld ? brain.LastDecision.Habit.Id.ToString() : "-")}"
                                + $", Bcore={b.Core}, Blegs={b.Legs}, gripA={engine.Grip.EffectiveGrip(FighterId.A)})");
                    }
                }

                // §4.2 피해 — 관측 기록보다 <b>먼저</b> 본다. 그래야 "A 가 이미 읽을 수
                // 있었던 상태에서 벌준 것" 만 세어진다. 이번 턴의 발화는 아직 원장에
                // 안 들어갔다.
                if (brain.LastDecision.Fired)
                    foreach (var e in events)
                        if (e is JudgementScored j && j.ScoredBy == FighterId.A
                            && (j.IsCounter || j.Judgement != Judgement.None))
                            state.RegisterDamage(brain.LastDecision.Habit.Id, ledger);

                // 관측: 실제로 발화한 턴만 센다 — 구조가 그걸 강제한다.
                ledger.Record(brain.LastDecision);
                if (brain.LastDecision.Fired && ledger.IsConfirmed(brain.LastDecision.Habit.Id)
                    && !m.ConfirmedAt.ContainsKey(brain.LastDecision.Habit.Id))
                    m.ConfirmedAt[brain.LastDecision.Habit.Id] = turn;

                ctx.ObserveExchange(events);
                window.Record(a);      // 창은 지난 커밋만 든다 — 해결 뒤에 넣는다
            }

            m.StaminaTrace.Add($"[end] A{engine.Stamina.Get(FighterId.A)}/B{engine.Stamina.Get(FighterId.B)}");

            // 종료 사유는 MatchEnded 가 안 실어 보낸다(Winner 만 든다) — 보드에서 되짚는다.
            if (engine.Board.IsFinished)
            {
                var w = engine.Board.Winner;
                string why = !w.HasValue ? "draw"
                    : engine.Board.HasIppon(w.Value) ? "ippon (or waza-ari x2)"
                    : engine.Board.Shido(w.Value == FighterId.A ? FighterId.B : FighterId.A) >= 3 ? "shido x3"
                    : "time / other";
                m.Ending = $"{why}, winner {(w.HasValue ? w.ToString() : "-")}, clock {engine.Clock.RemainingSeconds}s left"
                         + $" | score A {engine.Board.WazaAri(FighterId.A)}w/{engine.Board.Yuko(FighterId.A)}y/{engine.Board.Shido(FighterId.A)}s"
                         + $"  B {engine.Board.WazaAri(FighterId.B)}w/{engine.Board.Yuko(FighterId.B)}y/{engine.Board.Shido(FighterId.B)}s";
            }

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

        /// <summary>
        /// 현실적인 A — 공격은 6턴 중 2회(약 1/3)고 나머지는 잡기·방어·카운터 준비다.
        ///
        /// 위 <see cref="Varied"/> 는 매 턴 뭔가를 걸어서 판정을 양산했다. 실측에서
        /// 판정 22건 중 A 가 11건이었고, 그러면 수치가 B 의 성질을 측정하지 못한다.
        /// 판단은 이 프로필 기준으로 한다.
        /// </summary>
        static readonly TurnChoice[] Realistic =
        {
            new TurnChoice(HandTarget.None, CoreStance.Aggressive, LegAction.Attack),
            new TurnChoice(HandTarget.RightSleeve, CoreStance.Neutral, LegAction.Neutral),
            new TurnChoice(HandTarget.None, CoreStance.Defensive, LegAction.Defend),
            new TurnChoice(HandTarget.None, CoreStance.Aggressive, LegAction.Attack),
            new TurnChoice(HandTarget.LeftSleeve, CoreStance.Neutral, LegAction.Neutral),
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

            Metrics realistic = null;
            Assert.DoesNotThrow(() => realistic = RunMatch(t => Realistic[t % Realistic.Length], seed: 1234),
                "현실적인 A 로도 예외 없이 돌아야 한다");

            Console.WriteLine("=== A repeats one commit ===");
            Console.WriteLine(repeating.ToString());
            Console.WriteLine("=== A varies (4-cycle) ===");
            Console.WriteLine(varying.ToString());
            Console.WriteLine("=== A realistic (6-cycle, ~1/3 attacks) ===");
            Console.WriteLine(realistic.ToString());

            Assert.Greater(repeating.Turns, 0);
            Assert.Greater(varying.Turns, 0);
            Assert.Greater(realistic.Turns, 0);
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
        /// <b>읽기 사다리가 실제로 도는가.</b> 이 테스트가 지키는 것은 측정으로 한 번
        /// 깨졌던 전제다 — H4 이전에는 변주하는 A 와의 33교환이 <b>전부 6행(Neutral)</b>
        /// 이었다. 판정식에서 폭이 가장 큰 항이 통째로 죽어 있었다는 뜻이고, 그게
        /// H4(잡기 노림)를 넣은 이유다.
        ///
        /// 여기서 0 이 나오면 습관이 관측될 만큼 안 뜬다는 신호다. 숫자를 판정하지는
        /// 않는다 — 0 이냐 아니냐만 본다.
        /// </summary>
        [Test]
        public void ReadLadderActuallyRuns_NotEveryTurnIsNeutral()
        {
            var m = RunMatch(t => Varied[t % Varied.Length], seed: 1234);
            Console.WriteLine(m.ToString());

            int nonNeutral = 0;
            for (int r = 1; r <= 5; r++) nonNeutral += m.Rows[r];

            Assert.Greater(nonNeutral, 0,
                "한 경기가 전부 6행이면 읽기 시스템이 한 번도 안 도는 것이다");
        }

        /// <summary>
        /// 4주기로 변주하는 A 는 성향 창에 안 걸린다 — 6턴 중 최대 2회라 4/6 최빈이
        /// 구조적으로 불가능하고, 3연속도 안 선다.
        ///
        /// 반대쪽(반복하는 A 가 5행에 걸린다)은 <c>LiveReadResolverTests</c> 의
        /// <c>ThreeConsecutiveIdenticalCommits_MakeAAttackReadable_GotRead</c> 가 든다.
        /// 여기서 못 드는 이유가 계측 결과다: 잡기 지향을 넣은 뒤로 <b>반복하는 A 와의
        /// 경기가 5교환 만에 끝난다</b>(B 가 절반 둘로 이긴다). 4연속이 서기 전에 경기가
        /// 끝나서 스모크로는 5행을 관찰할 수 없다.
        /// </summary>
        [Test]
        public void VaryingCommits_NeverTriggerTheTendencyWindow()
        {
            var varying = RunMatch(t => Varied[t % Varied.Length], seed: 1234);

            Assert.AreEqual(0, varying.Rows[4], "4주기는 4/6 최빈이 구조적으로 불가능하다");
            Assert.AreEqual(0, varying.Rows[5], "3연속도 안 서므로 A 가 읽히지 않는다");
        }
    }
}
