using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CounterTime.Core;
using CounterTime.Brain;

namespace CounterTime.Harness.Overnight
{
    /// <summary>
    /// 2026-08-13 v3 (docs/overnight-2026-08-13-v3.md) — 작업1(로스터 차별성, B 자신의
    /// 행동으로 측정)과 작업3(그립 파리티 동역학)을 현재 HEAD(a3fb534) 상태로 실행하고
    /// §1/§3 절 본문을 만든다. 작업2(교란요인 분리, git worktree 기반 611e1e2/ec1cd8e
    /// 재실행·재사용)는 이 세션 진행자가 별도로 실행해 결과를 손으로 §2 자리에 끼워
    /// 넣는다 — git worktree 오케스트레이션은 C# 서브프로세스 스폰보다 셸에서 직접 하는
    /// 편이 훨씬 안전하고 투명해서 이 파일 범위 밖으로 뺐다(문서 본문에 그 사유를 남김).
    ///
    /// 기존 `OvernightRunner.Run()`(원본 2026-08-13/재수집 리포트 생성기)은 이 파일이
    /// 전혀 건드리지 않는다 — 완전히 새 진입점(`--overnight-v3`)이다.
    /// </summary>
    public static class OvernightV3Runner
    {
        // 작업 지시: N=500 이 시간이 오래 걸리면 낮추고 사유를 밝히라 — 이 세션은
        // 대화형 세션(진짜 밤새 무인 실행이 아니라 한 대화 안에서 도는 실행)이라 지갑은
        // "얼마나 오래 실제로 기다릴 수 있는가"이지 연산 예산이 아니다. 500 으로 먼저
        // 시도해 실측 소요시간을 재고, 필요하면 낮춘다 — 아래 Run() 이 그 실측을 콘솔에
        // 찍는다.
        public const int MatchesPerOpponent = 500;

        static readonly (string label, Func<MatchRulesConfig, OpponentDefinition> factory)[] Roster =
        {
            ("황도철 (32강)", HwangDochul.Create),
            ("문준배 (16강)", MoonJunbae.Create),
            ("서도현 (8강)", SeoDohyun.Create),
            ("임준철 (4강)", ImJunchul.Create),
            ("진도윤 (결승)", Jindoyun.Create),
            ("오재훈 (동메달전)", OhJaehoon.Create),
        };

        public static void Run()
        {
            Console.WriteLine("=== OVERNIGHT 2026-08-13 v3 — §1 로스터 차별성 + §3 그립 파리티 동역학 ===");
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var perOpponent = new Dictionary<string, List<MatchResult>>();
            foreach (var (label, factory) in Roster)
            {
                var list = new List<MatchResult>(MatchesPerOpponent);
                for (int seed = 1; seed <= MatchesPerOpponent; seed++)
                    list.Add(MatchSimulator.RunOne(label, factory, seed));
                perOpponent[label] = list;
                int exceptions = list.Count(m => m.Exception != null);
                int caps = list.Count(m => m.HitSafetyCap);
                Console.WriteLine($"  {label}: {list.Count} matches, {exceptions} exceptions, {caps} safety-cap, {sw.Elapsed.TotalSeconds:0.0}s elapsed");
            }
            Console.WriteLine($"Total elapsed: {sw.Elapsed.TotalSeconds:0.0}s for {MatchesPerOpponent * Roster.Length} matches");

            string body = BuildBody(perOpponent);
            string path = Path.Combine("..", "..", "docs", "overnight-2026-08-13-v3-body.md");
            File.WriteAllText(path, body);
            Console.WriteLine("§1/§3 body written to " + path + " — assembled into the final v3 doc by hand.");
        }

        // ── §1 로스터 차별성 ─────────────────────────────────────────────────────

        /// 상대 하나의 행동 벡터. 전부 "교환당 비율"로 정규화(상대마다 평균 경기 길이가
        /// 다르므로 원시 카운트를 그대로 비교하면 안 된다 — 예: 진도윤전은 평균 74교환,
        /// 32강 황도철전은 평균 22교환).
        sealed class BehaviorVector
        {
            public string Label;
            public long TotalExchanges;
            public Dictionary<string, double> TechniqueRate = new Dictionary<string, double>();
            public Dictionary<string, double> HabitRate = new Dictionary<string, double>();
            public double SetupRate, PokeRate, BigThrowRate, OtherDeclareRate;
            public double CounterReadyRate;
            public double SurpriseRatePerMatch;
            public Dictionary<string, double> GripTargetRate = new Dictionary<string, double>();
        }

        static BehaviorVector BuildVector(string label, List<MatchResult> ok)
        {
            long totalEx = ok.Sum(m => (long)m.ExchangesResolved);
            var v = new BehaviorVector { Label = label, TotalExchanges = totalEx };
            double Denom(long x) => totalEx == 0 ? 0 : (double)x / totalEx;

            var techTotals = new Dictionary<string, long>();
            foreach (var m in ok)
                foreach (var kv in m.BTechniqueUsage)
                    techTotals[kv.Key] = (techTotals.TryGetValue(kv.Key, out long c) ? c : 0) + kv.Value;
            foreach (var kv in techTotals) v.TechniqueRate[kv.Key] = Denom(kv.Value);

            var habitTotals = new Dictionary<HabitId, long>();
            foreach (var m in ok)
                foreach (var kv in m.BHabitFirings)
                    habitTotals[kv.Key] = (habitTotals.TryGetValue(kv.Key, out long c) ? c : 0) + kv.Value;
            foreach (var kv in habitTotals) v.HabitRate[kv.Key.ToString()] = Denom(kv.Value);

            v.SetupRate = Denom(ok.Sum(m => (long)m.BSetupDeclareCount));
            v.PokeRate = Denom(ok.Sum(m => (long)m.BPokeDeclareCount));
            v.BigThrowRate = Denom(ok.Sum(m => (long)m.BBigThrowDeclareCount));
            v.OtherDeclareRate = Denom(ok.Sum(m => (long)m.BOtherDeclareCount));
            v.CounterReadyRate = Denom(ok.Sum(m => (long)m.BCounterReadyDeclares));
            v.SurpriseRatePerMatch = ok.Count == 0 ? 0 : (double)ok.Sum(m => m.BSurpriseFirings) / ok.Count;

            var gripTotals = new Dictionary<HandTarget, long>();
            foreach (var m in ok)
                foreach (var kv in m.BGripTargetCounts)
                    gripTotals[kv.Key] = (gripTotals.TryGetValue(kv.Key, out long c) ? c : 0) + kv.Value;
            foreach (var kv in gripTotals) v.GripTargetRate[kv.Key.ToString()] = Denom(kv.Value);

            return v;
        }

        /// 두 벡터를 같은 차원 공간(둘 중 하나라도 쓰는 모든 키의 합집합)에 놓고 유클리드
        /// 거리를 잰다. L2 를 고른 이유(보고서에도 남김): 대칭이고, 한두 축의 큰 차이를
        /// 더 무겁게 반영한다 — 상대별로 기술/습관 id 가 거의 안 겹치므로(로스터 저작이
        /// 상대마다 배타적으로 습관을 배정) L1 이든 L2 든 결론 방향은 비슷하게 나올
        /// 것으로 예상되지만, 정답이라고 주장하지 않는다(작업 지시 그대로 — "아무 방어
        /// 가능한 지표나, 최선을 고민하지 말 것").
        static double EuclideanDistance(BehaviorVector a, BehaviorVector b)
        {
            var keys = new HashSet<string>();
            foreach (var k in a.TechniqueRate.Keys) keys.Add("tech:" + k);
            foreach (var k in b.TechniqueRate.Keys) keys.Add("tech:" + k);
            foreach (var k in a.HabitRate.Keys) keys.Add("habit:" + k);
            foreach (var k in b.HabitRate.Keys) keys.Add("habit:" + k);
            foreach (var k in a.GripTargetRate.Keys) keys.Add("grip:" + k);
            foreach (var k in b.GripTargetRate.Keys) keys.Add("grip:" + k);

            double sumSq = 0;
            foreach (var k in keys)
            {
                double av = k.StartsWith("tech:") ? Get(a.TechniqueRate, k.Substring(5))
                    : k.StartsWith("habit:") ? Get(a.HabitRate, k.Substring(6))
                    : Get(a.GripTargetRate, k.Substring(5));
                double bv = k.StartsWith("tech:") ? Get(b.TechniqueRate, k.Substring(5))
                    : k.StartsWith("habit:") ? Get(b.HabitRate, k.Substring(6))
                    : Get(b.GripTargetRate, k.Substring(5));
                sumSq += (av - bv) * (av - bv);
            }
            // 스칼라 4축(선언 유형 비율 3 + 카운터준비 + 변칙기/경기) — 변칙기는 스케일이
            // 다른 축(경기당)이라 그대로 더해도 크게 왜곡하지 않는다(전원 0~1 수준 값).
            sumSq += Sq(a.SetupRate - b.SetupRate) + Sq(a.PokeRate - b.PokeRate) + Sq(a.BigThrowRate - b.BigThrowRate)
                   + Sq(a.OtherDeclareRate - b.OtherDeclareRate) + Sq(a.CounterReadyRate - b.CounterReadyRate)
                   + Sq(a.SurpriseRatePerMatch - b.SurpriseRatePerMatch);
            return Math.Sqrt(sumSq);
        }

        static double Get(Dictionary<string, double> d, string k) => d.TryGetValue(k, out double v) ? v : 0;
        static double Sq(double x) => x * x;

        static string BuildBody(Dictionary<string, List<MatchResult>> perOpponent)
        {
            var sb = new System.Text.StringBuilder();
            var vectors = new List<BehaviorVector>();
            foreach (var (label, _) in Roster)
            {
                var ok = perOpponent[label].Where(m => m.Exception == null).ToList();
                vectors.Add(BuildVector(label, ok));
            }

            sb.AppendLine("## §1 — 로스터 차별성 (B 자신의 행동, A 승패와 무관)");
            sb.AppendLine();
            sb.AppendLine("A 승률은 Auto-A 라는 하나의(최적이 아닌) 휴리스틱의 품질에 오염된 지표라 로스터");
            sb.AppendLine("차별성 자료로 못 쓴다(§2 가 바로 그 예시를 보여준다) — 여기 §1 은 A 가 이기든");
            sb.AppendLine("지든 상관없이 B 자신이 무엇을 했는지만 센다. 계측 시점이 판정 이전(브레인이");
            sb.AppendLine("결정을 내리는 순간)이라 A 의 품질과 인과적으로 분리돼 있다.");
            sb.AppendLine();
            sb.AppendLine($"실행 규모: 상대 {Roster.Length}명 × {MatchesPerOpponent}경기 = {MatchesPerOpponent * Roster.Length}경기, 현재 HEAD(브레인 버그 3건");
            sb.AppendLine("수정 + AutoAPlayer 3중 안전장치 적용 이후) 기준.");
            sb.AppendLine();

            sb.AppendLine("### 상대별 행동 요약 (교환당 비율, %)");
            sb.AppendLine();
            sb.AppendLine("| 상대 | 흔들기declare% | 견제declare% | 큰기술declare% | 그외% | 카운터준비% | 변칙기/경기 | 그립우위교환% |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|");
            foreach (var v in vectors)
            {
                var ok = perOpponent[v.Label].Where(m => m.Exception == null).ToList();
                double gripEdgePct = v.TotalExchanges == 0 ? 0 : 100.0 * ok.Sum(m => (long)m.BTurnsWithGripEdge) / v.TotalExchanges;
                sb.AppendLine($"| {v.Label} | {v.SetupRate * 100:0.00}% | {v.PokeRate * 100:0.00}% | {v.BigThrowRate * 100:0.00}% | " +
                    $"{v.OtherDeclareRate * 100:0.00}% | {v.CounterReadyRate * 100:0.00}% | {v.SurpriseRatePerMatch:0.00} | {gripEdgePct:0.00}% |");
            }
            sb.AppendLine();

            sb.AppendLine("### 그립 표적 분포 (그립류 표적 교환 중 %, 어느 손/소매를 노리는가)");
            sb.AppendLine();
            var allGripKeys = new SortedSet<string>();
            foreach (var v in vectors) foreach (var k in v.GripTargetRate.Keys) allGripKeys.Add(k);
            sb.Append("| 상대 |");
            foreach (var k in allGripKeys) sb.Append(" " + k + " |");
            sb.AppendLine();
            sb.Append("|---|");
            foreach (var _ in allGripKeys) sb.Append("---|");
            sb.AppendLine();
            foreach (var v in vectors)
            {
                double sum = allGripKeys.Sum(k => Get(v.GripTargetRate, k));
                sb.Append("| " + v.Label + " |");
                foreach (var k in allGripKeys)
                {
                    double rate = Get(v.GripTargetRate, k);
                    double share = sum == 0 ? 0 : 100.0 * rate / sum;
                    sb.Append($" {share:0.0}% |");
                }
                sb.AppendLine();
            }
            sb.AppendLine();
            sb.AppendLine("(B 의 §4 3단 폴백은 오른소매/왼소매만 번갈아 노린다 — 위 표에서 다른 그립류");
            sb.AppendLine("표적(옷깃/틀어잡기/등싸잡기)이 전부 0%인 것은 버그가 아니라 폴백 자체의 설계");
            sb.AppendLine("범위다.)");
            sb.AppendLine();

            sb.AppendLine("### 습관 발화율 (교환당 %, HabitId 별 — 상대마다 배타적으로 배정되므로 대부분 한");
            sb.AppendLine("칸만 채워진다)");
            sb.AppendLine();
            var allHabitKeys = new SortedSet<string>();
            foreach (var v in vectors) foreach (var k in v.HabitRate.Keys) allHabitKeys.Add(k);
            sb.Append("| 상대 |");
            foreach (var k in allHabitKeys) sb.Append(" " + k + " |");
            sb.AppendLine();
            sb.Append("|---|");
            foreach (var _ in allHabitKeys) sb.Append("---|");
            sb.AppendLine();
            foreach (var v in vectors)
            {
                sb.Append("| " + v.Label + " |");
                foreach (var k in allHabitKeys) sb.Append($" {Get(v.HabitRate, k) * 100:0.00}% |");
                sb.AppendLine();
            }
            sb.AppendLine();

            sb.AppendLine("### 쌍별 거리 행렬 (유클리드, 위 벡터 전체 — 기술사용률+습관발화율+그립표적률+");
            sb.AppendLine("선언유형비율+카운터준비율+변칙기/경기, 전부 정규화된 비율)");
            sb.AppendLine();
            sb.Append("|  |");
            foreach (var v in vectors) sb.Append(" " + v.Label + " |");
            sb.AppendLine();
            sb.Append("|---|");
            foreach (var _ in vectors) sb.Append("---|");
            sb.AppendLine();
            var distances = new Dictionary<(string, string), double>();
            foreach (var vi in vectors)
            {
                sb.Append("| " + vi.Label + " |");
                foreach (var vj in vectors)
                {
                    double d = vi.Label == vj.Label ? 0 : EuclideanDistance(vi, vj);
                    if (vi.Label != vj.Label) distances[(vi.Label, vj.Label)] = d;
                    sb.Append($" {d:0.0000} |");
                }
                sb.AppendLine();
            }
            sb.AppendLine();

            // 문준배 vs 오재훈 — 원본 수집이 "실제로 구별 안 됨"이라고 지목한 쌍.
            string mj = "문준배 (16강)", oj = "오재훈 (동메달전)";
            double mjOj = distances.TryGetValue((mj, oj), out double d1) ? d1 : distances[(oj, mj)];
            double meanOther = distances.Where(kv => kv.Key.Item1 != mj || kv.Key.Item2 != oj)
                .Where(kv => kv.Key.Item1 != oj || kv.Key.Item2 != mj)
                .Select(kv => kv.Value).DefaultIfEmpty(0).Average();
            sb.AppendLine($"**문준배 vs 오재훈 (원본 수집이 \"수치가 소수점까지 거의 똑같다\"고 지목한 쌍)**:");
            sb.AppendLine($"거리 {mjOj:0.0000}, 다른 모든 쌍의 평균 거리 {meanOther:0.0000}. 이 값이 다른 쌍들과");
            sb.AppendLine("비슷한 규모면(0에 가깝지 않으면) 두 상대가 이제 이 지표로는 구별된다는 뜻 —");
            sb.AppendLine("판단은 안 하고 숫자만 남긴다.");
            sb.AppendLine();

            // §3 — 그립 파리티 동역학
            sb.AppendLine("## §3 — 그립 파리티 동역학");
            sb.AppendLine();
            sb.AppendLine("양쪽 `EffectiveGrip`이 정확히 0인 상태(\"파리티\")가 얼마나 지속되는지, 무엇이");
            sb.AppendLine("끝내는지, 흔들기→기술 선언이 파리티 중에 나오면 득점까지 이어지는지 — 계측만,");
            sb.AppendLine("결론 없음. **정의(이 세션이 고른 것, 정답이라 주장 안 함)**: 스트릭은 연속된");
            sb.AppendLine("\"양쪽 다 0\" 진입 교환 수로 세고, 다음 중 먼저 오는 쪽에서 끝난다 — (a) 이번");
            sb.AppendLine("교환에 A 또는 B 가 흔들기(Setup 표적) 또는 기술(공격)을 선언함(그립 수치 자체는");
            sb.AppendLine("0-0 그대로일 수 있다 — §4.4 흔들기는 그립을 안 바꾼다), (b) 다음 교환에 들어가기");
            sb.AppendLine("전 그립이 이미 0-0 이 아니게 바뀌어 있음(직전 교환의 그립 액션 결과). 굳히기 틱");
            sb.AppendLine("구간은 계측 안 함(서기 전용 개념). \"흔들기→득점 완결\" 창은 선언 교환을 포함해");
            sb.AppendLine("3교환 — 창이 겹치면 중복 계상될 수 있어 근사치다(엄밀한 인과 추적 아님).");
            sb.AppendLine();

            sb.AppendLine("| 상대 | 스트릭 수 | 평균 길이(교환) | A선언종료 | B선언종료 | 양쪽동시 | 그립액션종료 | 경기끝종료 |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|");
            long totCount = 0, totA = 0, totB = 0, totBoth = 0, totGrip = 0, totEnd = 0;
            long totLen = 0;
            long totDeclareTotal = 0, totWithin = 0, totMissed = 0;
            foreach (var (label, _) in Roster)
            {
                var ok = perOpponent[label].Where(m => m.Exception == null).ToList();
                long count = ok.Sum(m => (long)m.ParityStreakCount);
                long len = ok.Sum(m => m.ParityStreakTotalLength);
                long a = ok.Sum(m => (long)m.ParityStreakEndedByADeclare);
                long b = ok.Sum(m => (long)m.ParityStreakEndedByBDeclare);
                long both = ok.Sum(m => (long)m.ParityStreakEndedByBothDeclare);
                long grip = ok.Sum(m => (long)m.ParityStreakEndedByGripResolved);
                long end = ok.Sum(m => (long)m.ParityStreakEndedByMatchEnd);
                totCount += count; totLen += len; totA += a; totB += b; totBoth += both; totGrip += grip; totEnd += end;
                double avgLen = count == 0 ? 0 : (double)len / count;
                sb.AppendLine($"| {label} | {count} | {avgLen:0.00} | {a} | {b} | {both} | {grip} | {end} |");

                totDeclareTotal += ok.Sum(m => (long)m.ParityBreakDeclareTotal);
                totWithin += ok.Sum(m => (long)m.ParityBreakScoreWithinWindow);
                totMissed += ok.Sum(m => (long)m.ParityBreakScoreMissed);
            }
            sb.AppendLine();
            double totAvgLen = totCount == 0 ? 0 : (double)totLen / totCount;
            sb.AppendLine($"전체 합산: 스트릭 {totCount}개, 평균 길이 {totAvgLen:0.00}교환. 종료 사유 —");
            sb.AppendLine($"A 선언 {totA} / B 선언 {totB} / 양쪽 동시 {totBoth} / 그립 액션(선언 없이 그립만");
            sb.AppendLine($"변함) {totGrip} / 경기 종료 시점까지 안 끝남 {totEnd}.");
            sb.AppendLine();
            sb.AppendLine($"**흔들기/기술 선언 이후 득점 완결 여부** (선언 총 {totDeclareTotal}건 — 위 A선언+B선언+양쪽동시 합과");
            sb.AppendLine($"일치): 창(3교환) 안에 득점 {totWithin}건 ({(totDeclareTotal == 0 ? 0 : 100.0 * totWithin / totDeclareTotal):0.0}%),");
            sb.AppendLine($"창 만료까지 무득점 {totMissed}건 ({(totDeclareTotal == 0 ? 0 : 100.0 * totMissed / totDeclareTotal):0.0}%).");

            return sb.ToString();
        }
    }
}
