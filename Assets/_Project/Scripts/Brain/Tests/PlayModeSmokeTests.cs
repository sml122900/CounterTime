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

            /// §9.1 (step 5, 야간 작업) — 굳히기 진입 횟수. 실제 입력 UI 는 6단계 몫이라
            /// 이 루프는 고정 정책(항상 빠져나가기×따라가기 — 어긋남)으로 즉시 턴다.
            /// 숫자만 기록한다, 튜닝은 안 한다.
            public int OsaekomiEntries;
            /// §4.4 [초안] 실측(작업6) — 굳히기가 어떻게 끝났는가. 튜닝 대상 아님, 관찰만.
            public int OsaekomiEndedByEscape, OsaekomiEndedByScore;

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
                sb.AppendLine($"  turns={Turns}  fallback={FallbackTurns} ({100f * FallbackTurns / Turns:0.0}%)  mattes={Mattes}"
                            + $"  osaekomiEntries={OsaekomiEntries} (escape={OsaekomiEndedByEscape} score={OsaekomiEndedByScore})");
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

        /// <summary>한 경기를 굴린다. <paramref name="aCommit"/> 이 A 의 성향을 정한다.
        /// <paramref name="setupEnabled"/> (작업6, §4.4) — true 면
        /// <see cref="MatchRulesConfig.BalanceGate"/> 를 켜고 B 의 커밋을
        /// <see cref="SetupPrecedingCommitSource"/> 로 감싼다. 습관 카탈로그·읽기
        /// 사다리·판정식은 이 매개변수로 아무것도 안 바뀐다 — 켜고 끄는 것은 §4.4 경로
        /// 하나뿐이다.</summary>
        static Metrics RunMatch(Func<int, TurnChoice> aCommit, int seed, bool setupEnabled = false)
        {
            var cfg = new MatchRulesConfig { ThresholdJitter = 0, BalanceGate = setupEnabled };
            var rng = new SystemRandomSource(seed);
            var engine = new MatchEngine(cfg, RefereeProfile.Standard, rng);

            var opponent = Jindoyun.Create(cfg);
            var state = new HabitState(opponent);
            var ledger = new ObservationLedger();
            var window = new TendencyWindow();
            var resolver = new LiveReadResolver(ledger, window);
            var ctx = new MatchBrainContext(engine, FighterId.B);
            var brainInner = new HabitBrainCommitSource(FighterId.B, state, ctx, ctx, rng);
            // 작업4 데코레이터 — off 면 그냥 안 만든다(브레인 호출 지점 자체가 갈린다,
            // 켜져 있지도 않은 경로를 "비활성 상태로 통과"시키는 게 아니라).
            var brainWrapped = setupEnabled
                ? new SetupPrecedingCommitSource(brainInner, ctx, enabled: true)
                : null;

            var m = new Metrics { Ledger = ledger };

            for (int turn = 0; turn < Exchanges && !engine.Board.IsFinished; turn++)
            {
                var a = aCommit(turn);

                TurnChoice b;
                TechniqueSpec brainTechnique;
                HabitDecision brainDecision;
                bool brainUsedFallback;
                if (setupEnabled)
                {
                    brainWrapped.TryNext(out _, out b);
                    brainTechnique = brainWrapped.LastTechnique;
                    brainDecision = brainWrapped.LastDecision;
                    brainUsedFallback = brainWrapped.LastUsedFallback;
                }
                else
                {
                    brainInner.TryNext(out _, out b);
                    brainTechnique = brainInner.LastTechnique;
                    brainDecision = brainInner.LastDecision;
                    brainUsedFallback = brainInner.LastUsedFallback;
                }

                // 커밋이 실제로 이번 교환에 제출되는 시점에 알린다 — 흔들기 설치
                // 교환에서는 brainDecision 이 default(발화 없음)라 알림이 안 나가고,
                // 다음 교환(진짜 기술 제출)에서 실제 결정이 나간다.
                resolver.NoteOpponentHabit(brainDecision);

                // B 가 기술을 들었으면 B 가 공격자, 아니면 A 가 던진다 — 양방향을 다 태운다.
                bool bAttacks = brainTechnique != null;
                var attacker = bAttacks ? FighterId.B : FighterId.A;
                var technique = bAttacks
                    ? brainTechnique
                    : new TechniqueSpec("밭다리", ActionCost.BigThrow, TechniqueClass.Standard);

                var read = resolver.For(attacker, a, b);

                m.Turns++;
                if (brainUsedFallback) m.FallbackTurns++;
                if (brainDecision.Fired) m.Fire(brainDecision.Habit.Id);

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
                                + $" (counter={js.IsCounter}, Bhabit={(brainDecision.ConditionHeld ? brainDecision.Habit.Id.ToString() : "-")}"
                                + $", Bcore={b.Core}, Blegs={b.Legs}, gripA={engine.Grip.EffectiveGrip(FighterId.A)})");
                    }
                }

                // §9 — ResolveExchange 는 굳히기가 열린 채로 다시 불리면 예외를 던진다
                // (MatchEngine 가드, stage 6 배선 전까지의 안전장치). 이 루프엔 아직 실제
                // 입력이 없으므로 고정 정책으로 즉시 턴다: 어긋남(빠져나가기×따라가기)을
                // 계속 선택해 escapesNeeded 이내에 무조건 닫는다 — §9.3 의 5s 마크가
                // 스코어를 낼 수는 있지만(그러면 매치가 끝날 수도 있다), 그건 실제 판정
                // 결과지 이 정책이 만든 인위적 종료가 아니다.
                while (engine.Osaekomi.Active && !engine.Board.IsFinished)
                {
                    var osaekomiEvents = engine.ResolveOsaekomiExchange(
                        OsaekomiDefenderChoice.Escape, OsaekomiHolderChoice.Follow);
                    // 작업6 실측 — 이 마크가 끝을 냈다면 어떻게 끝났는지만 센다. 정책
                    // (어긋남 고정)은 손대지 않는다.
                    foreach (var e in osaekomiEvents)
                        if (e is OsaekomiEnded) m.OsaekomiEndedByEscape++;
                }
                if (engine.Board.IsFinished && m.OsaekomiEntries > m.OsaekomiEndedByEscape + m.OsaekomiEndedByScore)
                    m.OsaekomiEndedByScore++;

                // §4.2 피해 — 관측 기록보다 <b>먼저</b> 본다. 그래야 "A 가 이미 읽을 수
                // 있었던 상태에서 벌준 것" 만 세어진다. 이번 턴의 발화는 아직 원장에
                // 안 들어갔다.
                if (brainDecision.Fired)
                    foreach (var e in events)
                        if (e is JudgementScored j && j.ScoredBy == FighterId.A
                            && (j.IsCounter || j.Judgement != Judgement.None))
                            state.RegisterDamage(brainDecision.Habit.Id, ledger);

                // 관측: 실제로 발화한 턴만 센다 — 구조가 그걸 강제한다.
                ledger.Record(brainDecision);
                if (brainDecision.Fired && ledger.IsConfirmed(brainDecision.Habit.Id)
                    && !m.ConfirmedAt.ContainsKey(brainDecision.Habit.Id))
                    m.ConfirmedAt[brainDecision.Habit.Id] = turn;

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

        /// <summary>
        /// 작업6 실측 — 3프로필(반복/변주/현실적) × 흔들기(off/on) = 6회. 튜닝 안 함,
        /// 숫자를 판정하지 않는다. <c>BalanceGate</c>(§H 3단계 이전엔
        /// <c>SetupGateEnabled</c>) 기본값은 off — 이 테스트가 켜는 건 이 6회 실측
        /// 안에서만이다.
        ///
        /// <b>A 는 여전히 허수아비다.</b> B 가 공격 안 하는 턴에만 A 가 고정 밭다리를
        /// 던진다(§6.4 STATUS 원칙 그대로) — 세 프로필이 A 의 공격 성향을 실제로
        /// 갈라놓지 못하는 것도 그대로다. 이 실측이 보는 것은 B 의 흔들기 선행이
        /// 수치에 미치는 영향 하나뿐이다.
        /// </summary>
        /// §2b v4.2 후속 확인(2026-08-14 밤 5차) — B 습관 브레인이 그립 매트릭스
        /// 아래서 실제로 경기를 성립시킬 수 있는지 조사 전용, 임시. 튜닝 없음, 숫자
        /// 판정 안 함 — "예외 없이 도는가" + "B 의 그립 시도가 실제로 매트릭스를
        /// 타는가"만 본다.
        [Test]
        public void GripRevisionGateOn_HabitBrainStillCompletesAMatch()
        {
            var profiles = new (string name, Func<int, TurnChoice> commit)[]
            {
                ("반복", _ => Same),
                ("변주", t => Varied[t % Varied.Length]),
                ("현실적", t => Realistic[t % Realistic.Length]),
            };

            foreach (var (name, commit) in profiles)
            {
                var cfg = new MatchRulesConfig { ThresholdJitter = 0, GripRevisionGate = true };
                var rng = new SystemRandomSource(1234);
                var engine = new MatchEngine(cfg, RefereeProfile.Standard, rng);
                var opponent = Jindoyun.Create(cfg);
                var state = new HabitState(opponent);
                var ledger = new ObservationLedger();
                var window = new TendencyWindow();
                var resolver = new LiveReadResolver(ledger, window);
                var ctx = new MatchBrainContext(engine, FighterId.B);
                var brain = new HabitBrainCommitSource(FighterId.B, state, ctx, ctx, rng);

                int bGripChanged = 0, bGripFailed = 0, bSolidified = 0, bCrossTicks = 0, bCrossExpired = 0;
                int aGripChanged = 0, aGripFailed = 0;

                Assert.DoesNotThrow(() =>
                {
                    for (int turn = 0; turn < Exchanges && !engine.Board.IsFinished; turn++)
                    {
                        var a = commit(turn);
                        brain.TryNext(out _, out var b);
                        var brainTechnique = brain.LastTechnique;
                        var brainDecision = brain.LastDecision;
                        resolver.NoteOpponentHabit(brainDecision);

                        bool bAttacks = brainTechnique != null;
                        var attacker = bAttacks ? FighterId.B : FighterId.A;
                        var technique = bAttacks ? brainTechnique
                            : new TechniqueSpec("밭다리", ActionCost.BigThrow, TechniqueClass.Standard);
                        var read = resolver.For(attacker, a, b);

                        var events = engine.ResolveExchange(a, b, new ExchangeScript
                        { Attacker = attacker, Technique = technique, Read = read });

                        foreach (var e in events)
                        {
                            if (e is GripChanged gc) { if (gc.Who == FighterId.B) bGripChanged++; else aGripChanged++; }
                            if (e is GripAttemptFailed gf) { if (gf.Who == FighterId.B) bGripFailed++; else aGripFailed++; }
                            if (e is GripSolidified && ((GripSolidified)e).Who == FighterId.B) bSolidified++;
                            if (e is CrossGripTimerTicked ct && ct.Who == FighterId.B)
                            { bCrossTicks++; if (ct.Expired) bCrossExpired++; }
                        }

                        while (engine.Osaekomi.Active && !engine.Board.IsFinished)
                            engine.ResolveOsaekomiExchange(OsaekomiDefenderChoice.Escape, OsaekomiHolderChoice.Follow);

                        ctx.ObserveExchange(events);
                        ledger.Record(brainDecision);
                        window.Record(a);
                    }
                }, $"프로필 '{name}' — GripRevisionGate on 에서 예외 없이 끝까지 돌아야 한다");

                Console.WriteLine($"=== GripRevisionGate on — {name} ===");
                Console.WriteLine($"  B: grabbed={bGripChanged} failed={bGripFailed} solidified={bSolidified}"
                    + $" crossTicks={bCrossTicks} crossExpired(지도)={bCrossExpired}");
                Console.WriteLine($"  A: grabbed={aGripChanged} failed={aGripFailed}");
                Console.WriteLine($"  final grip A={engine.Grip.EffectiveGrip(FighterId.A)} B={engine.Grip.EffectiveGrip(FighterId.B)}"
                    + $"  finished={engine.Board.IsFinished}");
            }
        }

        [Test]
        public void SetupOffVsOn_SixProfileMeasurements()
        {
            var profiles = new (string name, Func<int, TurnChoice> commit)[]
            {
                ("반복", _ => Same),
                ("변주", t => Varied[t % Varied.Length]),
                ("현실적", t => Realistic[t % Realistic.Length]),
            };

            var sb = new StringBuilder();
            foreach (var (name, commit) in profiles)
            {
                var off = RunMatch(commit, seed: 1234, setupEnabled: false);
                var on = RunMatch(commit, seed: 1234, setupEnabled: true);

                sb.AppendLine($"=== {name} — 흔들기 off ===");
                sb.AppendLine(off.ToString());
                sb.AppendLine($"=== {name} — 흔들기 on ===");
                sb.AppendLine(on.ToString());

                Assert.Greater(off.Turns, 0);
                Assert.Greater(on.Turns, 0);
            }

            Console.WriteLine(sb.ToString());
        }
    }
}
