PROGRESS

Update this file every time a session resets. At the start of a new session, Claude Code reads CLAUDE.md then this file, in that order, and checks "Current State" and "Next Up" before starting work.

Current State (last updated: 2026-08-03)
Unity 6.3.21f1 LTS editor installed
CounterTime project created (Universal 3D template, location C:\project_2026\CounterTime)
Unity AI Assistant package enabled; Claude Code ↔ Unity MCP connected (unity-mcp, 7 tools enabled). Console verified clean — 0 errors / 0 warnings as of 2026-08-03
Git initialized, remote is https://github.com/sml122900/CounterTime.git (branch main). Git LFS installed (3.7.1) and tracking fbx/obj/blend/png/jpg/tga/psd/wav/mp3/ogg/mp4/mov. .gitignore and .gitattributes rewritten for Unity (.meta kept via explicit negation rule, text assets pinned to eol=lf)
KNOWN + ACCEPTED: the first commit (204aefc) landed before the LFS config and is already on the remote, so Assets/TutorialInfo/Icons/URP.png stays in history as a 24KB raw blob. History rewrite was considered and rejected — it would need a force push to reclaim one 24KB tutorial icon. Everything committed from now on goes through LFS. See docs/troubleshooting/lfs-after-first-commit.md
Folder structure (Assets/_Project/Scripts/Core, Scripts/Game) and the two asmdefs (CounterTime.Core, CounterTime.Game) still have not been created — designed in CLAUDE.md and docs/decisions/assembly-split-core-game.md only
Docs scaffolding created: docs/daily/, docs/decisions/, docs/troubleshooting/, docs/par-materials.md

Next Up (in priority order)
Create folder structure + the two asmdefs (ask Claude Code to generate them based on CLAUDE.md). Verify: Core asmdef has noEngineReferences: true, and grep for "UnityEngine" under Scripts/Core returns 0 hits
Port the combat logic from the HTML prototype (counter-time-v03.html) into pure C# classes under CounterTime.Core — the two-phase turn (grip-fighting → scoring techniques), matchup judgment, opponent habit-reading system
Design the opponent tendency data structure: POCO in CounterTime.Core + ScriptableObject wrapper in CounterTime.Game (this was the planned "session 3" work)
(Separate, not urgent) Finish the Judo match-1 scenario on the scenario board — three things the user still needs to answer: how the right-sleeve grip exchange actually plays out / timing of the collar-grip transition / the tells that distinguish an ippon-seoi-nage attempt from an ouchi-gari attempt

Resolved Questions (2026-08-03)
Why Assets/Editor/HubForceResolve.cs disappeared — nobody deleted it. It is Unity Hub's bootstrap script that calls Client.Resolve() and then removes itself and the empty Assets/Editor folder via AssetDatabase.DeleteAsset. The three deleted files match CleanupEmptyEditorFolder() exactly. Not a real incident, not resume material.
Whether to keep the Unity Cloud link (cloudProjectId / organizationId in ProjectSettings.asset) — keep and commit. cloudEnabled is 0, it is a project GUID rather than a secret, ProjectSettings.asset is tracked anyway, and the editor rewrites it if removed.

Session Log (newest first)
2026-08-03
Git/LFS setup finished and CLAUDE.md project rules locked in (Core/Game assembly split). Documented the session: daily log, two decision docs (assembly split, Git LFS), one troubleshooting doc (LFS after first commit), PAR materials, commit message drafts. Resolved both open questions above. Committed and pushed to origin/main.
Note: this session's docs were reconstructed from git log/diff and file timestamps, not from a live conversation record.

2026-08-02
Went through the full initial Unity project setup. Confirmed Unity MCP connection working end to end.
