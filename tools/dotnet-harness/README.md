# dotnet-harness

Headless test/fingerprint runner for `CounterTime.Core`/`CounterTime.Brain`/
(a subset of) `CounterTime.Game`, without launching Unity. See
`md/STATUS.md` "테스트 실행 경로" for why this exists — Unity MCP cannot
drive the Test Runner (`TestRunnerApi.Execute` is blocked for MCP tool
calls), but `Core`/`Brain` are UnityEngine-free by design, so they run fine
as a plain .NET console app.

## Run

    cd tools/dotnet-harness
    dotnet run                # run all tests, print the Mainline fingerprint
    dotnet run -- --check     # same, plus diff the fingerprint against
                               # fingerprint-baseline.txt, exit 1 on mismatch

## What it covers, what it doesn't

- `CounterTime.Core.Tests` — full, all tests run here.
- `CounterTime.Brain.Tests` — full, all tests run here.
- `CounterTime.Game.Tests` — **partial**. `UnityEngineShim.cs` fakes just
  enough of `UnityEngine` (`MonoBehaviour`, `TextAsset`, `Debug`, `Mathf`,
  `Input`/`KeyCode`, `JsonUtility`) for the Game files that don't need
  Canvas/TMP/Animator/coroutines/real Unity object identity. `Harness.csproj`
  lists exactly which Game files are included and why. `MatchRunnerHandActionTests`
  needs `new GameObject()` / `AddComponent<T>()` / `Object.DestroyImmediate` —
  Unity's real object model, not something this shim fakes — so it stays
  Unity-Test-Runner-only (`Window > General > Test Runner > EditMode > Run All`).
- The final line the harness prints always says which of the above actually
  ran, so a green run here is never mistaken for "everything is covered."

## The fingerprint

`MatchRunner.Mainline` is a fixed 17-node walk through the 42-node reference
scenario (`Assets/_Project/Scenario/counter-time-match-scenario-v2.json`).
The harness drives it directly (via reflection, since the `[SerializeField]`
fields are private) with a fixed seed and dumps every `MatchEvent` plus the
final engine state as text lines — `RunFingerprint()` / `Describe()` in
`Program.cs`. `fingerprint-baseline.txt` is that dump as of the last time it
was intentionally regenerated.

Behavior-preservation refactors are checked by diffing this dump before and
after a change — see `docs/decisions/behavior-fingerprint-refactor.md` and
`docs/overnight-2026-08-10.md`. **The rule that matters:** if a refactor was
meant to be behavior-preserving and the fingerprint changes, revert — don't
"fix" the fingerprint to match the code. If a change was *meant* to alter
scripted-mode behavior, regenerate `fingerprint-baseline.txt` deliberately
(`dotnet run` and copy the `[...]`/`STATE` lines) and say so in the commit,
so the new baseline is a decision, not a side effect.

## Known fragility

- `Harness.csproj`'s `nunit.framework.dll` reference points into
  `Library/PackageCache/com.unity.ext.nunit@<hash>/...` — Unity-generated,
  gitignored, and the `<hash>` can change if the package re-resolves. If the
  build fails on a missing reference, run
  `find Library/PackageCache -iname nunit.framework.dll` and fix the
  `HintPath`.
- `*.csproj` is gitignored repo-wide (Unity regenerates IDE project files
  that shouldn't be committed) — `.gitignore` carries an explicit
  `!/tools/dotnet-harness/*.csproj` negation so *this* one, hand-authored,
  stays tracked. Don't remove that negation without moving this file.
- Relative paths in `Harness.csproj`/`Program.cs` assume `dotnet run` is
  invoked from this directory (`tools/dotnet-harness/`), matching `dotnet
  run`'s default working directory. Running it another way needs adjusted
  paths.
