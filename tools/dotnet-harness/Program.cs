using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using CounterTime.Core;
using CounterTime.Game.Match;

// Headless dotnet console harness for CounterTime.Core.Tests + CounterTime.Brain.Tests
// (full) + a harness-compilable subset of CounterTime.Game.Tests, plus a byte-for-byte
// fingerprint of MatchRunner.Mainline (the 42-node scripted/대조 mode reference walk).
//
// Why this exists: CounterTime.Core and CounterTime.Brain are UnityEngine-free by
// design (see CLAUDE.md), so they can run outside Unity entirely. CounterTime.Game
// needs UnityEngine — UnityEngineShim.cs fakes just enough of it (MonoBehaviour,
// TextAsset, Debug, Mathf, Input/KeyCode, JsonUtility) for the harness-safe subset to
// compile; see Harness.csproj's comments for exactly which Game files are in vs out
// and why. Unity's own Test Runner is still authoritative for CounterTime.Game.Tests
// as a whole (MatchRunnerHandActionTests needs a real GameObject/AddComponent, which
// this shim does not attempt to fake) — this harness only covers what it can.
//
// Usage (run from this directory):
//   dotnet run                    — run all tests, print the Mainline fingerprint
//   dotnet run -- --check         — same, plus compare the fingerprint against
//                                    fingerprint-baseline.txt and exit non-zero on a
//                                    mismatch (does not update the baseline; see
//                                    tools/stamp.sh for the analogous doc-stamp check)
class Program
{
    static int total, passed, failed, skipped;
    static readonly List<string> failures = new List<string>();
    static readonly Dictionary<string, int> perFixture = new Dictionary<string, int>();

    static int Main(string[] args)
    {
        // 2026-08-13 overnight data collection — kept fully separate from the normal
        // test+fingerprint path below so it never slows down or interferes with the
        // existing regression gate. See tools/dotnet-harness/Overnight/OvernightRunner.cs
        // and docs/overnight-2026-08-13.md.
        if (args.Contains("--overnight"))
        {
            CounterTime.Harness.Overnight.OvernightRunner.Run();
            return 0;
        }

        bool check = args.Contains("--check");

        var asm = Assembly.GetExecutingAssembly();
        var types = asm.GetTypes().Where(t => t.IsClass && !t.IsAbstract).OrderBy(t => t.FullName).ToList();

        foreach (var t in types)
        {
            var testMethods = t.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttributes(typeof(TestAttribute), false).Any())
                .ToList();
            if (testMethods.Count == 0) continue;

            var setup = t.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.GetCustomAttributes(typeof(SetUpAttribute), false).Any());
            var teardown = t.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.GetCustomAttributes(typeof(TearDownAttribute), false).Any());

            foreach (var m in testMethods)
            {
                total++;
                perFixture.TryGetValue(t.FullName, out int c);
                perFixture[t.FullName] = c + 1;
                try
                {
                    object instance = Activator.CreateInstance(t);
                    setup?.Invoke(instance, null);
                    try
                    {
                        m.Invoke(instance, null);
                        passed++;
                    }
                    finally
                    {
                        teardown?.Invoke(instance, null);
                    }
                }
                catch (Exception ex)
                {
                    var inner = ex.InnerException ?? ex;
                    // NUnit's Assert.Ignore/Inconclusive throw specific exception types.
                    if (inner.GetType().Name == "IgnoreException" || inner.GetType().Name == "InconclusiveException")
                    {
                        skipped++;
                        continue;
                    }
                    failed++;
                    failures.Add(t.FullName + "." + m.Name + ": " + inner.GetType().Name + ": " + inner.Message
                        + "\n" + inner.StackTrace);
                }
            }
        }

        Console.WriteLine("TESTS: " + passed + "/" + total + " passed, " + failed + " failed, " + skipped + " skipped");
        foreach (var kv in perFixture.OrderBy(k => k.Key))
            Console.WriteLine("  " + kv.Key + " (" + kv.Value + ")");
        if (failures.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("FAILURES:");
            foreach (var f in failures) Console.WriteLine("  FAIL " + f);
        }

        Console.WriteLine();
        Console.WriteLine("COVERAGE: CounterTime.Core.Tests + CounterTime.Brain.Tests (full) + CounterTime.Game.Tests PARTIAL");
        Console.WriteLine("  (ScenarioTurnChoicesTests / ScriptedReadResolverTests / ResultNarrationTableTests / LiveAssemblerTests / SaveSystemTests ran here;");
        Console.WriteLine("   MatchRunnerHandActionTests needs the real Unity Test Runner, not run by this harness)");

        Console.WriteLine();
        Console.WriteLine("=== FINGERPRINT: MatchRunner.Mainline ===");
        var fpLines = RunFingerprint();
        foreach (var l in fpLines) Console.WriteLine(l);
        Console.WriteLine("--- " + fpLines.Count + " lines total ---");

        bool fpOk = true;
        if (check)
        {
            string baselinePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory == null ? "." : ".", "fingerprint-baseline.txt");
            // Resolve relative to the project directory when run via `dotnet run`.
            if (!File.Exists(baselinePath)) baselinePath = "fingerprint-baseline.txt";
            if (!File.Exists(baselinePath))
            {
                Console.WriteLine();
                Console.WriteLine("CHECK: no fingerprint-baseline.txt found next to Program.cs — nothing to compare against.");
                fpOk = false;
            }
            else
            {
                var baseline = File.ReadAllLines(baselinePath);
                fpOk = baseline.SequenceEqual(fpLines);
                Console.WriteLine();
                Console.WriteLine(fpOk ? "CHECK: fingerprint matches fingerprint-baseline.txt"
                                       : "CHECK: fingerprint DIFFERS from fingerprint-baseline.txt — do not silently accept this, report the diff.");
                if (!fpOk)
                {
                    int n = Math.Max(baseline.Length, fpLines.Count);
                    for (int i = 0; i < n; i++)
                    {
                        string b = i < baseline.Length ? baseline[i] : "<missing>";
                        string c = i < fpLines.Count ? fpLines[i] : "<missing>";
                        if (b != c) Console.WriteLine($"  line {i + 1}: baseline=[{b}]  current=[{c}]");
                    }
                }
            }
        }

        return (failed > 0 || !fpOk) ? 1 : 0;
    }

    static object GetField(object obj, string name) =>
        obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(obj);

    static void SetField(object obj, string name, object value) =>
        obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(obj, value);

    /// Walks MatchRunner.Mainline (the 42-node scripted/대조 mode reference match) with
    /// a fixed seed and dumps every emitted MatchEvent plus final engine state as text
    /// lines. Behavior-preservation refactors are checked by diffing this output
    /// before/after — see docs/decisions/behavior-fingerprint-refactor.md.
    static List<string> RunFingerprint()
    {
        string json = File.ReadAllText(
            Path.Combine("..", "..", "Assets", "_Project", "Scenario", "counter-time-match-scenario-v2.json"));

        var runner = new MatchRunner();
        SetField(runner, "matchScenarioJson", new UnityEngine.TextAsset(json));
        SetField(runner, "startNodeId", "m1");
        SetField(runner, "refereeProfile", RefereeProfile.Standard);
        SetField(runner, "randomSeed", 12345);
        SetField(runner, "logEvents", false);
        SetField(runner, "deterministicReplay", true);

        var lines = new List<string>();
        runner.Presented += (node, e) => lines.Add("[" + node.id + "] " + Describe(e));

        runner.Build((UnityEngine.TextAsset)GetField(runner, "matchScenarioJson"));

        try
        {
            runner.RunMainline();
        }
        catch (Exception ex)
        {
            lines.Add("EXCEPTION: " + ex.GetType().Name + ": " + ex.Message);
        }

        var eng = runner.Engine;
        lines.Add(string.Format(
            "STATE: A_stam={0} B_stam={1} clock_remaining={2}s finished={3} winner={4} osaekomi_active={5}",
            eng.Stamina.Get(FighterId.A), eng.Stamina.Get(FighterId.B),
            eng.Clock.RemainingSeconds, eng.Board.IsFinished,
            eng.Board.Winner.HasValue ? eng.Board.Winner.ToString() : "none",
            eng.Osaekomi.Active));

        return lines;
    }

    static string Describe(MatchEvent e)
    {
        switch (e)
        {
            case JudgementScored j:
                return "판정 " + j.Judgement + " sum=" + j.Sum + " by=" + j.ScoredBy +
                       (j.IsCounter ? " (되치기)" : "") + " [" + string.Join(",", j.Terms) + "]";
            case GripChanged g: return "잡기 " + g.Who + " " + g.OwnHand + "→" + g.Part + " (" + g.Kind + ")";
            case TearResolved t: return "뜯기 " + t.Who + " " + t.Hand + " → " + t.Outcome;
            case CounterReadyApplied c:
                return "카운터 준비 " + c.Who + ": 공격자 읽기 " + c.AttackerReadBefore + "→" + c.AttackerReadAfter;
            case ShidoGiven s: return "지도 " + s.Who + " #" + s.Count + (s.FromFeint ? " (위장)" : " (소극)");
            case RefereeTellChanged r: return "심판 tell " + r.Who + " → " + r.Tell;
            case MatteCalled _: return "그쳐";
            case OsaekomiEntered oe: return "굳히기 진입 holder=" + oe.Holder + " adv=" + oe.Adv + " escapesNeeded=" + oe.EscapesNeeded;
            case OsaekomiScoreAwarded os: return "굳히기 판정 " + os.Judgement + " holder=" + os.Holder + " at=" + os.HeldSeconds + "s";
            case OsaekomiEscapeResolved oer: return "굳히기 탈출교환 defender=" + oer.Defender + " credited=" + oer.EscapeCredited + " soFar=" + oer.EscapesSoFar;
            case OsaekomiEnded oen: return "굳히기 종료(탈출) defender=" + oen.Defender;
            case MatchEnded m: return "경기 종료 — 승자 " + (m.Winner.HasValue ? m.Winner.ToString() : "무승부");
            default: return e.GetType().Name;
        }
    }
}
