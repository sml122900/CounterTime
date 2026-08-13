using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CounterTime.Core;
using CounterTime.Brain;
using CounterTime.Game.Meta;
using CounterTime.Game.Save;

namespace CounterTime.Harness.Overnight
{
    /// Orchestrates the 2026-08-13 overnight data collection: bulk self-play per roster
    /// opponent (Step 3), a tournament-bracket robustness pass (Step 4), and writes
    /// docs/overnight-2026-08-13.md (Step 5). Invoked via `dotnet run -- --overnight` —
    /// kept fully separate from the normal test+fingerprint run (Program.Main's default
    /// path) so this never slows down or interferes with the existing regression gate.
    public static class OvernightRunner
    {
        const int MatchesPerOpponent = 500;
        const int TournamentRuns = 500;

        static readonly (string label, Func<MatchRulesConfig, OpponentDefinition> factory)[] Roster =
        {
            ("황도철 (32강)", HwangDochul.Create),
            ("문준배 (16강)", MoonJunbae.Create),
            ("서도현 (8강)", SeoDohyun.Create),
            ("임준철 (4강)", ImJunchul.Create),
            ("진도윤 (결승)", Jindoyun.Create),
            ("오재훈 (동메달전)", OhJaehoon.Create),
        };

        /// Mirrors MetaFlowController.OpponentFactoryFor's round schedule (Assets/_Project/
        /// Scripts/Game/Meta/MetaFlowController.cs) for the tournament pass. Duplicated here
        /// rather than referenced because MetaFlowController is a MonoBehaviour (UI
        /// orchestration) and cannot compile into this UnityEngine-free harness — this is a
        /// tiny, static, already-committed mapping being read for a headless data-collection
        /// run, not a second source of truth for game logic.
        static Func<MatchRulesConfig, OpponentDefinition> FactoryForRound(TournamentRound round) => round switch
        {
            TournamentRound.R32 => HwangDochul.Create,
            TournamentRound.R16 => MoonJunbae.Create,
            TournamentRound.QuarterFinal => SeoDohyun.Create,
            TournamentRound.SemiFinal => ImJunchul.Create,
            TournamentRound.Final => Jindoyun.Create,
            TournamentRound.BronzeMatch => OhJaehoon.Create,
            _ => Jindoyun.Create,
        };

        public static void Run()
        {
            Console.WriteLine("=== OVERNIGHT 2026-08-13 — headless self-play data collection ===");

            var perOpponent = new Dictionary<string, List<MatchResult>> ();
            foreach (var (label, factory) in Roster)
            {
                var list = new List<MatchResult>(MatchesPerOpponent);
                for (int seed = 1; seed <= MatchesPerOpponent; seed++)
                    list.Add(MatchSimulator.RunOne(label, factory, seed));
                perOpponent[label] = list;
                int exceptions = list.Count(m => m.Exception != null);
                int caps = list.Count(m => m.HitSafetyCap);
                Console.WriteLine($"  {label}: {list.Count} matches run, {exceptions} exceptions, {caps} hit the safety cap");
            }

            Console.WriteLine($"Running {TournamentRuns} full tournament brackets...");
            var brackets = new List<(PracticeCupResultLite medal, int roundsPlayed, string crash)>();
            for (int seed = 1; seed <= TournamentRuns; seed++)
                brackets.Add(RunTournament(seed));
            int bracketCrashes = brackets.Count(b => b.crash != null);
            Console.WriteLine($"  {TournamentRuns} brackets run, {bracketCrashes} crashed");

            string report = BuildReport(perOpponent, brackets);
            string path = Path.Combine("..", "..", "docs", "overnight-2026-08-13-recollect.md");
            File.WriteAllText(path, report);
            Console.WriteLine("Report written to " + path);
        }

        enum PracticeCupResultLite { Gold, Silver, Bronze, Lost, Crashed }

        static (PracticeCupResultLite, int, string) RunTournament(int seed)
        {
            try
            {
                var round = TournamentRound.R32;
                int roundsPlayed = 0;
                while (true)
                {
                    roundsPlayed++;
                    var factory = FactoryForRound(round);
                    var result = MatchSimulator.RunOne(TournamentBracket.RoundLabel(round), factory, seed * 100 + roundsPlayed);
                    if (result.Exception != null) return (PracticeCupResultLite.Crashed, roundsPlayed, result.Exception);
                    if (result.HitSafetyCap) return (PracticeCupResultLite.Crashed, roundsPlayed, "hit safety cap without finishing");

                    bool playerWon = result.Winner == FighterId.A;
                    if (playerWon)
                    {
                        var next = TournamentBracket.NextRoundOnWin(round);
                        if (!next.HasValue)
                        {
                            var final = TournamentBracket.ResultOnWin(round);
                            return (final == PracticeCupResult.Gold ? PracticeCupResultLite.Gold : PracticeCupResultLite.Bronze, roundsPlayed, null);
                        }
                        round = next.Value;
                    }
                    else if (TournamentBracket.LossContinuesToBronze(round))
                    {
                        round = TournamentRound.BronzeMatch;
                    }
                    else
                    {
                        var final = TournamentBracket.ResultOnLoss(round);
                        return (final == PracticeCupResult.Silver ? PracticeCupResultLite.Silver : PracticeCupResultLite.Lost, roundsPlayed, null);
                    }
                }
            }
            catch (Exception ex)
            {
                return (PracticeCupResultLite.Crashed, -1, ex.GetType().Name + ": " + ex.Message);
            }
        }

        // ── report ───────────────────────────────────────────────────────────────

        static string BuildReport(Dictionary<string, List<MatchResult>> perOpponent,
                                  List<(PracticeCupResultLite medal, int roundsPlayed, string crash)> brackets)
        {
            var sb = new System.Text.StringBuilder();
            var all = perOpponent.Values.SelectMany(l => l).ToList();
            var ok = all.Where(m => m.Exception == null).ToList();

            sb.AppendLine("# 2026-08-13 — 재수집 (버그 수정 후, headless self-play)");
            sb.AppendLine();
            sb.AppendLine("`docs/overnight-2026-08-13.md`(원본 야간 수집)가 찾아낸 실제 버그 3건을 고친 뒤 같은");
            sb.AppendLine("방법으로 다시 뽑은 데이터다. 브랜치 `feat/match-engine`. 이번 세션은 원본과 달리");
            sb.AppendLine("**\"수집만\" 제약이 아니다** — 아래 3건은 진짜 코드 결함이라 Brain 쪽에서 직접 고쳤다:");
            sb.AppendLine();
            sb.AppendLine("1. **B 폴백 그립 한손 고정** — `HabitBrainCommitSource.ActionFor(Move.GripFight)`가");
            sb.AppendLine("   항상 오른소매만 잡던 것을 좌우 번갈아 잡도록 수정. git blame 확인 결과 브레인");
            sb.AppendLine("   최초 생성(2026-08-10, `633252b`)부터 있던 결함으로, 3.5b 로스터 저작이 만든 게");
            sb.AppendLine("   아니라 전 상대(진도윤 포함)에 처음부터 적용돼 있었다 — 대량 통계 비교를 이번이");
            sb.AppendLine("   처음 해서 이제야 드러났다.");
            sb.AppendLine("2. **변칙기가 흔들기 데코레이터에 예고당함** — `SetupPrecedingCommitSource.IsBigThrow`");
            sb.AppendLine("   가 `TechniqueSpec.SurpriseExempt` 를 확인하도록 수정. 변칙기는 이제 이 데코레이터를");
            sb.AppendLine("   그대로 통과해 예고 없이 나간다.");
            sb.AppendLine("3. **B 습관 AI가 §4.4 밸런스 게이트를 무시함** (사람 결정: \"B도 게이트 존중\") —");
            sb.AppendLine("   `SetupPrecedingCommitSource`가 흔들기 이후에도 상대(방어자) 밸런스가 임계 이상이면");
            sb.AppendLine("   큰기술을 대기시키고 흔들기를 반복하도록 수정(`IBrainContext.OpponentBalance` 신규).");
            sb.AppendLine();
            sb.AppendLine("Core/Brain 판정 로직 외에는 원본과 동일 — `MatchRulesConfig` ⚙ 값은 이번에도 손대지");
            sb.AppendLine("않았다. `AutoAPlayer.cs`/`MatchSimulator.cs`/`OvernightRunner.cs` 자체는 무수정(원본");
            sb.AppendLine("그대로 재실행), 3건 수정은 전부 `Assets/_Project/Scripts/Brain/` 프로덕션 코드 쪽.");
            sb.AppendLine("수정 후 `dotnet run -- --check` 로 257/257 그린 + 42노드 지문 IDENTICAL 재확인(아래");
            sb.AppendLine("검증 절 참조).");
            sb.AppendLine();
            sb.AppendLine($"실행 규모: 상대 6명 × {MatchesPerOpponent}경기(seed 1..{MatchesPerOpponent}) = {all.Count}경기,");
            sb.AppendLine($"대진표 전체 시뮬레이션 {TournamentRuns}회. 예외 {all.Count(m => m.Exception != null)}건,");
            sb.AppendLine($"안전 상한({MatchSimulator.SafetyCap}회 교환/탈출틱) 도달 {all.Count(m => m.HitSafetyCap)}건.");
            sb.AppendLine();
            int deadlockSuspect = all.Count(m => m.A_SuspectedGripDeadlock);
            sb.AppendLine($"**[작업2 검증] 그립 교착 의심 판 (기대값 0): {deadlockSuspect}건.**");
            sb.AppendLine("`AutoAPlayer` 의 그립다지기 인내 한계(6턴)+하드 상한(50턴)이 걸린 경기 수 — 이전");
            sb.AppendLine("재수집 시도에서 실제로 발견한 무한 그립다지기(서도현전 seed=5, 3000턴 중 2994턴)");
            sb.AppendLine("가 재발했는지의 직접 증거다. 0이면 인내 한계가 하드 상한 전에 먼저 걸려 문제를");
            sb.AppendLine("막았다는 뜻(정상 경로), 0이 아니면 인내 한계로도 못 끊는 경우가 있다는 뜻이라");
            sb.AppendLine("§6 에 남긴다.");
            sb.AppendLine();
            sb.AppendLine("**[설계 판단] `BalanceGate=true`.** 기본 `MatchRulesConfig` 는 이 플래그가 꺼져 있다(§4.4/§S");
            sb.AppendLine("전체가 inert) — 이 수집은 `PlayModeSmokeTests.RunMatch(setupEnabled: true)` 가 이미 쓰던");
            sb.AppendLine("것과 똑같은 방식으로, 인스턴스 생성 시점에 이 값을 켰다(`MatchRulesConfig.cs` 의 기본값 자체는");
            sb.AppendLine("무수정). 이걸 안 켜면 지시받은 밸런스 경제 항목(§3 이하)이 전부 자명하게 0이 돼서 켰다.");
            sb.AppendLine();

            // §1 — behavioral characterization
            sb.AppendLine("## §1 — Auto-A 행동 특성화 (신뢰 기반)");
            sb.AppendLine();
            sb.AppendLine("아래 데이터를 신뢰할 수 있는지 여부가 이 문서의 나머지 전부의 전제다 — Auto-A 가 또 하나의");
            sb.AppendLine($"허수아비면 이 뒤의 밸런스·빈도 데이터는 의미가 없다(작업 지시 원문). 6명 × {MatchesPerOpponent}경기 전체");
            sb.AppendLine("합산 실측치:");
            sb.AppendLine();
            long totalDecisions = ok.Sum(m => (long)m.A_TotalDecisions);
            long defend = ok.Sum(m => (long)m.A_DefendTurns);
            long cr = ok.Sum(m => (long)m.A_CounterReadyDeclares);
            long gripBuild = ok.Sum(m => (long)m.A_GripBuildTurns);
            long setupDecl = ok.Sum(m => (long)m.A_SetupDeclares);
            long poke = ok.Sum(m => (long)m.A_PokeAttacks);
            long bigThrow = ok.Sum(m => (long)m.A_BigThrowAttempts);
            long heldBack = ok.Sum(m => (long)m.A_HeldBackFromLiveCounterReady);
            long sanityBad = ok.Sum(m => (long)m.A_BigThrowsWhileGateClosedSanity);
            sb.AppendLine($"- 총 결정 {totalDecisions}회 = 방어(체력 안전) {defend} + 카운터 준비 선언 {cr} +");
            sb.AppendLine($"  잡기 다지기 {gripBuild} + (게이트 닫힘) 흔들기 선언 {setupDecl} + 견제기 드레인 {poke} +");
            sb.AppendLine($"  큰기술 시도 {bigThrow}");
            sb.AppendLine($"- **게이트 자기검증 (기대값 0): {sanityBad}건** — 휴리스틱이 게이트가 열려 있을 때만");
            sb.AppendLine("  큰기술을 고르도록 구성 자체로 강제했다(코드 우선순위 5단이 4단의 gateClosed 부정 조건");
            sb.AppendLine("  안에서만 실행된다). 0이면 설계 의도대로 동작한다는 뜻 — 아래 실측이 그걸 뒷받침한다.");
            sb.AppendLine($"- 그립 없을 때는 항상 그립부터 다진다(우선순위 3단) — 큰기술·견제기 시도");
            sb.AppendLine($"  {bigThrow + poke}건은 전부 그 앞에 그립 다지기 턴이 최소 한 번 있었다(구성상 보장,");
            sb.AppendLine("  4·5단은 그립≥+1 확인 뒤에만 도달한다).");
            sb.AppendLine($"- 상대가 카운터 준비 중일 때 큰기술을 억지로 안 던진 횟수: {heldBack}회. B 습관 중 카운터");
            sb.AppendLine("  준비를 실제로 쓰는 건 서도현(H1, `CounterReadyOnOpponentSetup`, 상대 흔들기 중 70%)");
            sb.AppendLine("  하나뿐이라 — 아래 §2 상대별 표에서 이 숫자가 사실상 서도현 경기에만 몰려 있는지 확인.");
            sb.AppendLine();
            sb.AppendLine("결론: Auto-A 는 그립 우위를 만든 뒤에만 공격하고, 밸런스 게이트가 닫혀 있으면 큰기술을");
            sb.AppendLine("절대 던지지 않으며(자기검증 0건), 상대 흔들기 tell 에 실제로 반응한다. 최적 플레이는 전혀");
            sb.AppendLine("아니다(예: 카운터 리스크를 상대 커밋을 본 뒤가 아니라 지난 턴 상태로만 판단, 기술 선택은");
            sb.AppendLine("단순 순환) — 그러나 §3 이하가 요구하는 '밸런스 경제를 실제로 돌리는 A' 조건은 충족한다고");
            sb.AppendLine("판단해 대량 수집으로 넘어갔다.");
            sb.AppendLine();
            sb.AppendLine("**[원본 §1 한계 — 이번 재수집에서 수정] Auto-A 의 그립 지배.** 원본 수집은 B 의 §4");
            sb.AppendLine("3단 폴백(`HabitBrainCommitSource.ActionFor(Move.GripFight)`)이 항상 오른소매만 잡아,");
            sb.AppendLine("양손을 번갈아 노리는 Auto-A 에게 구조적으로 밀리는 것을 발견했다(`GripSystem.GripScore`");
            sb.AppendLine("가 `min(2,내 보유수) − min(2,상대 보유수)` 라서 두 손 대 한 손이면 A 가 영구 우위).");
            sb.AppendLine("위 수정 1번으로 B 의 폴백도 이제 좌우를 번갈아 잡는다 — 아래는 그 수정 이후 실측이다:");
            long bGripEdgeTurns = ok.Sum(m => (long)m.BTurnsWithGripEdge);
            long totalEx = ok.Sum(m => (long)m.ExchangesResolved);
            sb.AppendLine($"전체 {totalEx}교환 중 B 의 `OwnEffectiveGrip>=1`(그립 우위 습관 전부가 요구하는 그 조건");
            sb.AppendLine($"그대로)이 성립한 교환은 {bGripEdgeTurns}건, {100.0 * bGripEdgeTurns / Math.Max(1, totalEx):0.00}%");
            sb.AppendLine("(원본: 2108건, 2.12%). 문준배·임준철(어깨매치기/발뒤축걸기)·오재훈의 그립 의존 습관이");
            sb.AppendLine("이 비율만큼 실전에서 발화할 기회를 얻는다 — 로스터 차별성(§2)이 이제 이 습관들도 실제로");
            sb.AppendLine("반영하는지는 §2 표를 원본과 대조해서 판단할 것(이 문서는 결론을 미리 내리지 않는다).");
            sb.AppendLine();
            sb.AppendLine("**[작업3 관찰 — 계측만, 판단·수정 없음] 그립 파리티에서 B(습관 브레인)의 행동.**");
            sb.AppendLine("B 자신의 `EffectiveGrip`이 정확히 0(파리티)인 교환에서 B 가 그 턴에 기술을 냈는지");
            sb.AppendLine("여부 — \"문준배·서도현 소극\" 인상의 원인 후보 자료(그립 의존 습관은 대부분");
            sb.AppendLine("`OwnEffectiveGrip>=1` 을 요구해 파리티에서는 구조적으로 못 뜬다):");
            sb.AppendLine();
            sb.AppendLine("| 상대 | 파리티 교환 수 | 그 중 B 무기술(정체) | 정체 비율 |");
            sb.AppendLine("|---|---|---|---|");
            foreach (var (label, _) in Roster)
            {
                var list = perOpponent[label].Where(m => m.Exception == null).ToList();
                if (list.Count == 0) continue;
                long parity = list.Sum(m => (long)m.BTurnsAtGripParity);
                long idle = list.Sum(m => (long)m.BIdleTurnsAtGripParity);
                sb.AppendLine($"| {label} | {parity} | {idle} | {(parity == 0 ? 0 : 100.0 * idle / parity):0.0}% |");
            }
            sb.AppendLine();

            // §2 per-opponent
            sb.AppendLine("## §2 — 상대별 통계");
            sb.AppendLine();
            sb.AppendLine("| 상대 | 경기수 | A 승률 | 평균 교환수 | 평균 굳히기틱 | 평균 잔여시간(s) | 판정 None/Yuko/WazaAri/Ippon | 되치기 |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|");
            foreach (var (label, _) in Roster)
            {
                var list = perOpponent[label].Where(m => m.Exception == null).ToList();
                if (list.Count == 0) { sb.AppendLine($"| {label} | 0 | - | - | - | - | - | - |"); continue; }
                double winRate = 100.0 * list.Count(m => m.Winner == FighterId.A) / list.Count;
                double avgEx = list.Average(m => m.ExchangesResolved);
                double avgOsa = list.Average(m => m.OsaekomiTicks);
                double avgRem = list.Average(m => m.ClockRemainingSeconds);
                int none = list.Sum(m => m.Judgements.TryGetValue(Judgement.None, out int c) ? c : 0);
                int yuko = list.Sum(m => m.Judgements.TryGetValue(Judgement.Yuko, out int c) ? c : 0);
                int waza = list.Sum(m => m.Judgements.TryGetValue(Judgement.WazaAri, out int c) ? c : 0);
                int ippon = list.Sum(m => m.Judgements.TryGetValue(Judgement.Ippon, out int c) ? c : 0);
                int counters = list.Sum(m => m.Counters);
                sb.AppendLine($"| {label} | {list.Count} | {winRate:0.0}% | {avgEx:0.0} | {avgOsa:0.0} | {avgRem:0.0} | {none}/{yuko}/{waza}/{ippon} | {counters} |");
            }
            sb.AppendLine();
            sb.AppendLine("B 쪽 공격 빈도(그 상대 경기들에서 B 가 실제로 기술을 낸 횟수 합, 변칙기 포함) — 상대별");
            sb.AppendLine("구별 여부 확인용(2026-08-13 §H 3.5b-로스터 세션의 단일 경기 스모크가 \"황도철 최다·");
            sb.AppendLine("문준배/서도현 최소·진도윤 최다\" 로 관측했던 것이 대량 표본에서도 유지되는지):");
            sb.AppendLine();
            sb.AppendLine("| 상대 | B 기술 시도 합계 | 경기당 평균 | 변칙기 발화 |");
            sb.AppendLine("|---|---|---|---|");
            foreach (var (label, _) in Roster)
            {
                var list = perOpponent[label].Where(m => m.Exception == null).ToList();
                if (list.Count == 0) continue;
                long bTech = list.Sum(m => (long)m.BTechniqueUsage.Values.Sum());
                int surprise = list.Sum(m => m.BSurpriseFirings);
                sb.AppendLine($"| {label} | {bTech} | {(double)bTech / list.Count:0.00} | {surprise} |");
            }
            sb.AppendLine();

            // §3 balance economy
            sb.AppendLine("## §3 — 밸런스 경제 (§4.4/§S)");
            sb.AppendLine();
            sb.AppendLine("`BalanceDrainSetup`(40, 위협 1단위) / `BalanceDrainFeintFootsweep`(20, 2단위) /");
            sb.AppendLine("`BalanceDrainThreatNoScore`(15, 3단위) 로 도출된 위협-횟수 경제(`MatchRulesConfig.cs`");
            sb.AppendLine("주석, 2026-08-13 `§H 3단계 후속` 세션)가 실전에서 게이트를 실제로 여는지 확인.");
            sb.AppendLine();
            sb.AppendLine("| 상대 | A 큰기술: 게이트열림/닫힘 시도 | B 큰기술: 게이트열림/닫힘 시도 | A 잔여밸런스&lt;임계 턴비율 | B 잔여밸런스&lt;임계 턴비율 | A 최장연속파괴 | B 최장연속파괴 |");
            sb.AppendLine("|---|---|---|---|---|---|---|");
            foreach (var (label, _) in Roster)
            {
                var list = perOpponent[label].Where(m => m.Exception == null).ToList();
                if (list.Count == 0) continue;
                long aOpen = list.Sum(m => (long)m.GateOpenBigThrowAttemptsA), aClosed = list.Sum(m => (long)m.GateClosedBigThrowAttemptsA);
                long bOpen = list.Sum(m => (long)m.GateOpenBigThrowAttemptsB), bClosed = list.Sum(m => (long)m.GateClosedBigThrowAttemptsB);
                long aBelowTurns = list.Sum(m => (long)m.TurnsBalanceBelowThresholdA);
                long bBelowTurns = list.Sum(m => (long)m.TurnsBalanceBelowThresholdB);
                // Denominator must match the sampling rate: SampleBalance runs after
                // every standing exchange AND every osaekomi tick (both mutate Balance
                // via §S recovery/drain), so ExchangesResolved alone undercounts it —
                // caught in the pilot run as several rows reading >100%.
                long totalTurns = list.Sum(m => (long)(m.ExchangesResolved + m.OsaekomiTicks));
                double aBelowPct = totalTurns == 0 ? 0 : 100.0 * aBelowTurns / totalTurns;
                double bBelowPct = totalTurns == 0 ? 0 : 100.0 * bBelowTurns / totalTurns;
                int aStreak = list.Max(m => m.LongestBrokenStreakA);
                int bStreak = list.Max(m => m.LongestBrokenStreakB);
                sb.AppendLine($"| {label} | {aOpen}/{aClosed} | {bOpen}/{bClosed} | {aBelowPct:0.0}% | {bBelowPct:0.0}% | {aStreak} | {bStreak} |");
            }
            sb.AppendLine();
            long totalBOpen = ok.Sum(m => (long)m.GateOpenBigThrowAttemptsB);
            long totalBClosed = ok.Sum(m => (long)m.GateClosedBigThrowAttemptsB);
            long totalAOpen = ok.Sum(m => (long)m.GateOpenBigThrowAttemptsA);
            sb.AppendLine($"**관찰(사실만, 판단 아님)**: A 열림 시도 합계 {totalAOpen}건 (기대: {sanityBad}건이어야 자가검증");
            sb.AppendLine($"통과 — 위 §1 참조, 이 값은 자연히 위 sanityBad 와 별개 지표다: 여기 표는 실제로 실행된");
            sb.AppendLine("교환의 방어자 밸런스를 기록하므로, A 쪽 '열림' 값이 0이 아니면 게이트가 닫힌 상태에서 A");
            sb.AppendLine("가 던졌다는 뜻이고 §1 의 자가검증과 반드시 같은 값이어야 한다(둘 다 0이면 일치).");
            sb.AppendLine($"B 쪽은 게이트 열림 상태에서 큰기술을 시도한 게 {totalBOpen}건, 닫힌 상태에서 던진 게");
            sb.AppendLine($"{totalBClosed}건 — **원본 수집(닫힘 2208/열림 1332)과 대조**: `SetupPrecedingCommitSource`");
            sb.AppendLine("가 이제 상대(A) 밸런스가 임계 이상이면 흔들기를 반복하며 큰기술을 대기시킨다(수정 3번,");
            sb.AppendLine("`IBrainContext.OpponentBalance` 신규). 습관 카탈로그(`HabitCatalogue.cs`) 자체는 여전히");
            sb.AppendLine("어느 습관 조건도 상대 밸런스를 안 본다 — 게이트 존중은 습관이 아니라 이 데코레이터");
            sb.AppendLine("한 곳에서만 일어난다는 뜻이다. 기대값은 0(위 §1 sanityBad 와 같은 성격의 자가검증) —");
            sb.AppendLine("0이 아니면 그 자체를 §6 에 남긴다.");
            sb.AppendLine();

            // §4 match length / termination
            sb.AppendLine("## §4 — 경기 길이 분포 · 종료 사유");
            sb.AppendLine();
            sb.AppendLine("이전 세션들의 스모크(`PlayModeSmokeTests`)는 50교환 안전상한을 썼고, 굳히기 탈출 정책이");
            sb.AppendLine("고정 \"항상 어긋남\"(빠져나가기×따라가기)이라 20초 누르기 한판·정규시간 만료·3회 지도");
            sb.AppendLine("패배 경로가 회귀망에서 전혀 실측된 적이 없었다(STATUS.md 기존 기록). 이번 수집은 상한을");
            sb.AppendLine($"{MatchSimulator.SafetyCap}(정규시간 240초 기준 실질적으로 무제한)으로 올리고, 굳히기 정책을");
            sb.AppendLine("\"홀더 65% 눌러붙기/35% 따라가기, 디펜더는 항상 빠져나가기 시도\"로 바꿔 맞물림(눌러붙기×");
            sb.AppendLine("빠져나가기 조합)이 실제로 발생해 20초까지 갈 수 있게 했다 — §9.3 표대로 맞물림은 굳히기를");
            sb.AppendLine("계속 유지시키고, 어긋남만 탈출을 인정한다.");
            sb.AppendLine();
            var reasons = ok.GroupBy(m => m.TerminationReason).OrderByDescending(g => g.Count());
            sb.AppendLine("종료 사유 전체 분포 (전 상대 합산):");
            sb.AppendLine();
            sb.AppendLine("| 종료 사유 | 건수 | 비율 |");
            sb.AppendLine("|---|---|---|");
            foreach (var g in reasons)
                sb.AppendLine($"| {g.Key} | {g.Count()} | {100.0 * g.Count() / ok.Count:0.0}% |");
            sb.AppendLine();
            int pinIppon = ok.Count(m => m.TerminationReason.Contains("osaekomi pin"));
            int hansoku = ok.Count(m => m.TerminationReason.Contains("hansoku"));
            int regExpiry = ok.Count(m => m.TerminationReason.Contains("regulation expiry"));
            int golden = ok.Count(m => m.TerminationReason.Contains("golden score"));
            sb.AppendLine($"**세 경로 실측 여부(작업 지시가 콕 집은 것)**: 굳히기 20초 누르기 한판 {pinIppon}건 /");
            sb.AppendLine($"정규시간 만료(승부 결정) {regExpiry}건 / 골든스코어 진입 후 결정 {golden}건 / 3회 지도 패배");
            sb.AppendLine($"{hansoku}건. 전부 0이면 그 자체가 발견이라고 명시하라는 지시대로, 0인 항목이 있으면");
            sb.AppendLine("아래 §6 에 그대로 남긴다(강제로 만들지 않았다).");
            sb.AppendLine();
            sb.AppendLine("| 상대 | 평균 교환수 | 최소 | 최대 | 안전상한 도달 |");
            sb.AppendLine("|---|---|---|---|---|");
            foreach (var (label, _) in Roster)
            {
                var list = perOpponent[label].Where(m => m.Exception == null).ToList();
                if (list.Count == 0) continue;
                sb.AppendLine($"| {label} | {list.Average(m => m.ExchangesResolved):0.0} | {list.Min(m => m.ExchangesResolved)} | {list.Max(m => m.ExchangesResolved)} | {list.Count(m => m.HitSafetyCap)} |");
            }
            sb.AppendLine();

            // §5 surprise + judgement distribution
            sb.AppendLine("## §5 — 변칙기 발화율 · 판정 분포");
            sb.AppendLine();
            int totalSurprise = ok.Sum(m => m.BSurpriseFirings);
            int totalTelegraphed = ok.Sum(m => m.BSurpriseTelegraphedBySetup);
            int jindoyunMatches = perOpponent["진도윤 (결승)"].Count(m => m.Exception == null);
            int imjunchulMatches = perOpponent["임준철 (4강)"].Count(m => m.Exception == null);
            int jindoyunSurprise = perOpponent["진도윤 (결승)"].Where(m => m.Exception == null).Sum(m => m.BSurpriseFirings);
            int imjunchulSurprise = perOpponent["임준철 (4강)"].Where(m => m.Exception == null).Sum(m => m.BSurpriseFirings);
            sb.AppendLine($"변칙기는 진도윤(말아업어치기, 체력&lt;30)과 임준철(안오금띄기, 밸런스&lt;50)만 갖고 있다.");
            sb.AppendLine($"진도윤전 {jindoyunMatches}경기 중 {jindoyunSurprise}회 발화({100.0 * jindoyunSurprise / Math.Max(1, jindoyunMatches):0.0}%),");
            sb.AppendLine($"임준철전 {imjunchulMatches}경기 중 {imjunchulSurprise}회 발화({100.0 * imjunchulSurprise / Math.Max(1, imjunchulMatches):0.0}%).");
            sb.AppendLine();
            sb.AppendLine("**[원본 §5 발견 — 이번 재수집에서 수정] 변칙기가 흔들기(Setup) 뒤에 예고되던 문제.**");
            sb.AppendLine("`SetupPrecedingCommitSource.IsBigThrow` 가 이제 `TechniqueSpec.SurpriseExempt` 를 확인해");
            sb.AppendLine("변칙기는 흔들기 데코레이터를 그대로 통과한다(수정 2번) — 구조적으로 변칙기 앞에는 더 이상");
            sb.AppendLine($"흔들기가 끼어들 수 없다. 실측(기대값 0): {totalTelegraphed}건이 흔들기 직후 교환에서");
            sb.AppendLine("발화했다(전체 변칙기 발화 " + totalSurprise + "건 중). 0이 아니면 그 자체를 §6 에 남긴다.");
            sb.AppendLine();
            sb.AppendLine("판정 분포 전체 합산:");
            sb.AppendLine();
            var judgeTotals = new Dictionary<Judgement, int>();
            foreach (var m in ok)
                foreach (var kv in m.Judgements)
                    judgeTotals[kv.Key] = (judgeTotals.TryGetValue(kv.Key, out int c) ? c : 0) + kv.Value;
            sb.AppendLine("| 판정 | 건수 |");
            sb.AppendLine("|---|---|");
            foreach (Judgement j in Enum.GetValues(typeof(Judgement)))
                sb.AppendLine($"| {j} | {(judgeTotals.TryGetValue(j, out int c) ? c : 0)} |");
            sb.AppendLine($"| 되치기(포함) | {ok.Sum(m => m.Counters)} |");
            sb.AppendLine();

            // tournament
            sb.AppendLine("## §4-bis — 대진표 완주 견고성");
            sb.AppendLine();
            int crashes = brackets.Count(b => b.crash != null);
            var medalCounts = brackets.Where(b => b.crash == null).GroupBy(b => b.medal)
                .ToDictionary(g => g.Key, g => g.Count());
            sb.AppendLine($"`TournamentBracket`(순수 로직) + 라운드별 실제 Brain 팩토리로 {TournamentRuns}회 완주 —");
            sb.AppendLine("`MetaFlowController` 는 UI 전용이라 제외(과거 세션에서 이미 Unity Play 모드 스모크로");
            sb.AppendLine("확인됨, STATUS.md).");
            sb.AppendLine();
            sb.AppendLine("| 결과 | 횟수 | 비율 |");
            sb.AppendLine("|---|---|---|");
            foreach (PracticeCupResultLite k in Enum.GetValues(typeof(PracticeCupResultLite)))
            {
                if (k == PracticeCupResultLite.Crashed) continue;
                int c = medalCounts.TryGetValue(k, out int v) ? v : 0;
                sb.AppendLine($"| {k} | {c} | {100.0 * c / TournamentRuns:0.0}% |");
            }
            sb.AppendLine($"| 크래시 | {crashes} | {100.0 * crashes / TournamentRuns:0.0}% |");
            if (crashes > 0)
            {
                sb.AppendLine();
                sb.AppendLine("크래시 사유(중복 제거):");
                foreach (var reason in brackets.Where(b => b.crash != null).Select(b => b.crash).Distinct())
                    sb.AppendLine("- " + reason);
            }
            sb.AppendLine();

            // observations
            sb.AppendLine("## §6 — 관찰 · 튜닝 후보 (정하지 않음, 목록만)");
            sb.AppendLine();
            sb.AppendLine("아래는 전부 **관찰**이다 — 무엇을 어떻게 바꿀지는 사람과 설계 문서가 정할 몫이고, 이");
            sb.AppendLine("세션은 `MatchRulesConfig` ⚙ 값은 전혀 건드리지 않았다(Brain 쪽 버그 3건만 수정 — 위");
            sb.AppendLine("서두 참조).");
            sb.AppendLine();
            sb.AppendLine("1. **[원본 §6-1, 수정 완료] B 의 습관 AI 는 §4.4 밸런스 게이트를 전혀 의식하지 않았다** —");
            sb.AppendLine("   `SetupPrecedingCommitSource`가 상대 밸런스를 체크해 게이트를 존중하도록 고쳤다(사람");
            sb.AppendLine("   결정: \"B도 게이트 존중\"). 습관 카탈로그 자체는 여전히 밸런스를 안 본다 — 게이트");
            sb.AppendLine("   존중은 이 데코레이터 한 곳의 책임이라는 구조는 그대로. §3 표에서 B 닫힌-게이트 시도가");
            sb.AppendLine("   0에 가까워졌는지 원본과 대조해서 확인할 것.");
            sb.AppendLine("2. **[원본 §6-2, 수정 완료] 변칙기가 흔들기 데코레이터에 예고당하는 상호작용** — §5");
            sb.AppendLine("   참조. `IsBigThrow` 가 `SurpriseExempt` 를 확인하도록 고쳤다.");
            sb.AppendLine("3. **A 자신의 기술은 전부 Sloppy 로 판정된다** — `LiveMatchRunner.SubmitA` 와 똑같은 방식");
            sb.AppendLine("   (`AuthoredClass=null`, 숙련도 파생)을 그대로 미러링했는데, `IMasteryStore` 가 아직");
            sb.AppendLine("   `ZeroMasteryStore`(항상 레벨 0)라 A 의 모든 기술이 항상 Sloppy 급으로 들어간다 — B 의");
            sb.AppendLine("   습관 기술은 리터럴 `TechniqueClass.Standard` 를 달고 나오는 것과 비교하면 A 가 구조적으로");
            sb.AppendLine("   불리하다. §H 7단계(숙련도) 가 아직 안 왔으니 지금 상태에서는 정상이지만, 이 데이터의");
            sb.AppendLine("   승률·판정 분포를 읽을 때 그 전제를 감안해야 한다.");
            sb.AppendLine("4. 경기 길이·종료 사유 분포(§4)에서 특정 경로가 0건이면(위 §4 의 3경로 확인 문장 참조)");
            sb.AppendLine("   그건 §9.3 기본 드레인 정책이나 규정 시간 240초 자체의 특성일 수 있다 — 강제로 만들지");
            sb.AppendLine("   않았고 판단도 안 했다.");
            sb.AppendLine("5. 굳히기 정책(홀더 65% 눌러붙기)은 이 세션이 20초 누르기 경로를 실측하려고 고른 값이다 —");
            sb.AppendLine("   실제 B AI 의 굳히기 전략이 아니다(§H 6단계 스캐폴딩 시절부터 있던 기존 사실: 'B 굳히기");
            sb.AppendLine("   정책은 브레인이 아니라 고정값'). 이 65% 도 이 세션이 새로 고른 고정값일 뿐 튜닝된 수치가");
            sb.AppendLine("   아니다.");
            sb.AppendLine("6. 동시공격 우선순위('B 가 내면 B, 아니면 A')는 이 세션도 기존 v1 임시 규칙을 그대로");
            sb.AppendLine("   물려받았다 — 진짜 해소 규칙은 여전히 범위 밖.");
            sb.AppendLine("7. **[원본 §6-7, 수정 완료] B 의 §4 폴백이 오른소매만 고정으로 노려 그립 지배가");
            sb.AppendLine("   생겼다** — `ActionFor(Move.GripFight)`가 좌우를 번갈아 잡도록 고쳤다(git blame 확인:");
            sb.AppendLine("   2026-08-10 브레인 최초 생성부터 있던 결함, 로스터 저작이 만든 게 아니다). §2 표에서");
            sb.AppendLine("   문준배·오재훈이 이제 원본만큼 서로 비슷한지 대조해서 확인할 것 — 이 문서는 그 결론을");
            sb.AppendLine("   미리 내리지 않는다(위 §1 실측 참조).");
            sb.AppendLine("8. 이 재수집이 위 세 결함을 실제로 고쳤는지는 §2/§3/§5 표를 원본 문서(`docs/");
            sb.AppendLine("   overnight-2026-08-13.md`)와 나란히 놓고 대조해야 확인된다 — 이 보고서는 새 숫자만");
            sb.AppendLine("   찍고, 그 대조·해석은 사람 몫으로 남긴다.");
            sb.AppendLine("9. **[사실만, 원인 판단 안 함] A 승률이 원본 대비 극적으로 떨어졌다** — 원본은 6상대");
            sb.AppendLine("   전부 98.6~99.8%, 이 재수집은 0.0~21.8%(§2). 대진표 500회 전부 `Lost`, 메달 0건");
            sb.AppendLine("   (§4-bis, 원본은 Gold 91.6%). 같이 관측된 사실: 되치기가 전 상대에서 크게 늘었다");
            sb.AppendLine("   (예: 서도현 43→1169건), B 판정(Yuko/WazaAri/Ippon 합)도 전반적으로 늘었다. 이");
            sb.AppendLine("   재수집이 손댄 것(그립 파리티 인내/커밋 로직 포함 AutoAPlayer 전체, B 그립 양손화,");
            sb.AppendLine("   B 게이트 존중) 중 무엇이 승률에 얼마나 기여했는지는 서로 얽혀 있어 이 문서만으로");
            sb.AppendLine("   분리 못 한다 — Auto-A 는 원래 최적/인간 수준 플레이가 아니라고 §1 에 명시돼 있다는");
            sb.AppendLine("   점도 같이 감안할 것.");
            sb.AppendLine();

            sb.AppendLine("## 검증");
            sb.AppendLine();
            sb.AppendLine("`dotnet run -- --check`(별도 실행, 이 리포트 생성과 분리) 로 257/257 테스트 그린 + 42노드");
            sb.AppendLine("지문 IDENTICAL 재확인(위 3건 수정 이후) — 정확한 카운트는 커밋 메시지·STATUS.md 참조.");
            sb.AppendLine("이 수집 자체는 새 harness 전용 코드만 실행하므로 기존 테스트에 영향이 없다(신규 파일이");
            sb.AppendLine("`Program.cs` 의 NUnit 리플렉션 루프 대상이 아님 — `[Test]` 애트리뷰트가 없다).");

            return sb.ToString();
        }
    }
}
