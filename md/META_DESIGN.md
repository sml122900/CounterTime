<!-- v1.0 | body:e64fc96 -->
# META_DESIGN.md — meta-game minimal scaffold + save format (Plan A) (v1.0)

Purpose: open the **save layer that §C adaptation's and §F mastery's "permanent per
opponent" state needs to sit on** — nothing more. This is not a full career mode; it is
the scaffold the next step (training system) will build on top of. Match logic and rules
are out of scope here (`MATCH_RULES.md` v4.1 / `CORE_DESIGN_MatchEngine.md` are the
authority there).

⚙ marks a tunable value. [설계 판단] marks a proposed structure subject to review/approval.
[간단히] marks a spot the user explicitly said to keep thin/simple — deliberately shallow,
not an oversight.

---

## M0. Scope boundary (what this does NOT build)

Builds: 3 map locations · one tournament (연습 대회, the practice cup) · a 32-slot bracket
· running a match from the bracket · a save file · reputation spread (direct + indirect).

**Does NOT build** (scaffold only):
- Actual training/rest content — an empty template screen only (next step). [간단히]
- Multiple tournaments/seasons/a calendar — only the one practice cup. [간단히]
- Real simulation of the bracket's other matches — see §M3. [간단히]
- Money, rankings, NPC narrative — none of it.

---

## M1. Save format (the core of Plan A)

### Current state (measured)
No serialization layer exists in the project. Start from the simplest thing that works.

### Design [설계 판단]
- **One JSON file**, `Application.persistentDataPath/save.json`. Unity `JsonUtility` or
  an equivalently lightweight serializer. One slot (multi-slot later). [간단히]
- **Save points**: immediately after a match result is confirmed + on returning to the
  map + on quit. Atomic write (temp file → rename) to survive a mid-write crash.
- **Schema (minimal):**
```
SaveData {
  playerName : string
  mastery    : { techId → level }          // §F: player's per-technique mastery
  opponents  : [
    {
      id            : string
      name          : string
      scoutingLevel : int      // §C: how much this fighter knows about ME (0..N ⚙)
      adaptation    : { techId → exposureCount }  // §C.2: accumulated from direct matches
      knownByMe     : [ techId ]  // §C.1: techniques I've observed and 'recognize' on this fighter
    }
  ]
  career : {
    practiceCupResult : enum { None, Lost, Bronze, Silver, Gold } | null
  }
}
```
- **Core purity preserved**: serialization is a **Game-layer** concern. Core's systems
  (mastery, adaptation, recognition) hold pure state only; Game injects `SaveData` into
  Core at match start and extracts `SaveData` from Core at match end. Core knows nothing
  of files or `JsonUtility` — the no-`UnityEngine`-in-Core rule is unchanged.
- Connects to: `OpponentAsset` (currently dead code) and `TendencyWindow`'s unused
  `ScoutingLevel`-seeding constructor are this schema's attachment points —
  `SaveData.opponents[].scoutingLevel` feeds that seed.
- **Interaction with match-lifetime state**: `HabitState`/`ObservationLedger` remain
  match-lifetime (untouched). `SaveData` sits above that layer — at match start it reads
  `opponents[id]`'s `scoutingLevel`/`adaptation` to seed this match's state, and at match
  end it accumulates this match's observations back into `opponents[id]`.

### §H steps this opens
- §F mastery (permanent level): the `mastery` map. `IMasteryStore`'s (step 2 stub) real
  backend.
- §C.2 adaptation (permanent per opponent): `opponents[].adaptation`. The ledger row for
  B's adaptation.
- §C.1 recognition (permanent per opponent): `opponents[].knownByMe`. Storage for the
  predictive-counter candidate list.
- §C.3 decay: the rule that lowers stored counts/levels as matches pass elapses sits on
  top of this.

---

## M2. Map (3 locations)

### Design [설계 판단] [간단히]
- One simple screen, 3 clickable spots: **judo gym · home · arena**. Background is
  placeholder (pixel art/solid color). Spots are buttons/hotspots — no real map art
  needed.
- **Judo gym** click → training template screen (empty panel + "훈련" title + return-to-
  map button). No content, a slot for the next step.
- **Home** click → rest template screen (same structure, "휴식").
- **Arena** click → §M3's tournament flow.
- Map ↔ each location is a scene transition or panel toggle [설계 판단 — which fits the
  existing scene structure is a build-time measurement call, matched to whether the
  existing LiveMatch is a scene or a panel].

---

## M3. Tournament — practice cup · 32-slot bracket

### Design [설계 판단]
- Arena → tournament list (one entry: **"연습 대회"**) → click → 32-slot bracket screen.
- **Bracket**: a standard 32-slot tournament. **username is highlighted**. The other 31
  are filler names (from a placeholder list, byes allowed — a slot may be empty).
  [간단히]
- **Match-start button placement**: in the bracket, placed **in the space between the
  opponent's name and my name** (user-specified). That button → runs the existing
  LiveMatch. Only shows on the match I'm actually in.
- **Only my path is a real match**: the other 15 first-round matches are not played.

### Other-match resolution (confirmed: Plan A)
- **Other matches are filled by a seeded coin-flip that decides only the winner** — no
  match simulation, only names advance. The bracket looks "alive" at zero cost. Only my
  match runs the real engine.

### Progression (medal boundaries confirmed)
- Win my match → advance to the next round's bracket, with a start button next to my next
  opponent there.
- 32 → 16 → 8 → **semifinal** → final. Medal rule (confirmed):
  - Semifinal win → final. Final win = **gold**, final loss = **silver**.
  - Semifinal loss → **bronze-medal match** (against the other semifinal loser). Win =
    **bronze**, loss = no medal (4th place).
  - No repechage — a plain single-elimination bracket. [간단히]
- `career.practiceCupResult ∈ {None, Lost, Bronze, Silver, Gold}`. The bronze-medal match
  appears as a separate bracket entry, with its own start button, if I lose the
  semifinal.
- A loss (no medal) ends the tournament. Whether a retry is offered is [간단히] — for now
  it's just a return to the map.

---

## M4. Reputation spread (seed of the rivalry structure)

User confirmed: an opponent I've fought holds data on me, and if I perform well, fighters
I haven't fought also accumulate a little data.

### Design [설계 판단]
- **Direct (fought opponents)**: at match end, accumulate this match's observed-technique
  usage into that opponent's `opponents[id].adaptation` (permanent form of the §C.2
  ledger). Meeting that fighter again seeds the next match from the stored adaptation —
  the match starts with them already reading my techniques to some degree. This is what
  "an opponent who has learned more about me" actually is.
- **Indirect (unfought fighters)**: on winning a medal/the tournament, other fighters in
  the pool (= bracket entrants) who I didn't fight get a small `scoutingLevel` bump ⚙.
  Bigger performance (bronze < silver < gold) → bigger bump ⚙. `scoutingLevel` seeds the
  tendency window (§C.1's brain input), so a higher value means that fighter reads me
  better the next time we meet.
- **Boundary**: for this scaffold, the "pool" is the 32 practice-cup entrants. Spread is
  confined to this pool [간단히] — wide rankings/national rumor are career-scope, later.

### Where it's stored
- Both of the above write directly into `SaveData.opponents[]` — no separate layer.

---

## M5. Decided / remaining

**Confirmed**:
- §M3's other-match resolution = Plan A (seeded coin-flip fill).
- Medals = semifinal win→final (win gold/lose silver), semifinal loss→bronze-medal match
  (win bronze/lose no medal). Spread magnitudes are ⚙.

**To report at implementation time (not a user decision)**:
- Whether map↔location and match transitions are scenes or panels — measured at build
  time against the existing LiveMatch structure.
- Whether a retry is offered on elimination — for now, map return only. [간단히]

**Document links**:
- `CORE_DESIGN_MatchEngine.md`'s persistence-seam section is filled in against these
  decisions — the save schema lives here (META), the Core injection/extraction
  interfaces live there.

---

## M6. Implementation order

`md/STATUS.md`'s Next Up tracks the live sequencing; recorded here as this document's own
summary of how it reshapes §H:

```
0 bug fix (done) → 1 hand split UI (done) → 2 technique registry/tab/attacker unification →
3 Balance axis → 3.5 save layer (§M1) + map/tournament scaffold (§M2–M4) →
4 judgment promotion → 5 recognition/predictive counter (→ knownByMe persists) →
6 B adaptation (→ adaptation persists) → 7 mastery (→ mastery persists) → 8 feint weighting.
```

The save layer sits before step 5 specifically so that steps 5, 6, and 7 have somewhere
to persist to when they land.
