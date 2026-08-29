# docs: what each file is, and when to read it

⚠️ **This index exists because the project gets handed between sessions and tools, and a
folder of eighteen documents with no map is a folder nobody reads.** If you add a document,
add its row here in the same commit.

---

## Read these, in this order

| File | What it is |
|---|---|
| [`../CLAUDE.md`](../CLAUDE.md) | **First, always.** The rules of the repository: which git repo is live, the engine-free core rule, the build and test commands, the traps on this machine. |
| [`VISION.md`](VISION.md) | **What the game is FOR.** The two modes and why both ship, the readability budget, how a player is meant to learn a power, what is settled. Read before making a design call. |
| [`TODO.md`](TODO.md) | **What is actually open.** What is wrong, where it lives, what done looks like. Check before inventing a task; update in the same commit as the work. |
| [`GAME_OVERVIEW.md`](GAME_OVERVIEW.md) | **The whole game in one file, for a reader rather than for an editor.** Every mode, rule, verb and number a player can feel, all eighteen hero powers, the twelve street characters, the six heroes, the six lata and the ten tsinelas with their trait rows. ⚠️ **It is a map, not a source of truth**: `Design.md` and `Balance.cs` win over it, and where it disagrees with them it is the file to fix. |
| [`Design.md`](Design.md) | **The balance source of truth.** Every number that decides the game, and why. § 13 lists what it does NOT govern. ⚠️ **This copy is the live one**; the Godot repo's is the frozen 2026-08-02 original. |
| [`Hero_Strike_UI.md`](Hero_Strike_UI.md) | **What Hero Strike puts on screen and what it deliberately does not.** The ability bar, the charge readout and the cooldown language, measured against the same `VISION.md` § 2 readability budget the abilities themselves are. |
| [`Hero_Strike_Balance.md`](Hero_Strike_Balance.md) | **What `Design.md` § 13 hands off.** § 1 is the per-ability floor footprint table, measured against the `VISION.md` § 2 readability budget, and it is the only place that table has ever existed. § 2 is the cooldown and ultimate economy as shipped. §§ 3 and 4 are the rework proposal and are **not built**. |
| [`Art_Direction.md`](Art_Direction.md) | **The colour law, the scale and height laws, arena geometry, and which tool produces which asset.** § 1 is the one that never bends: **orange is OFFENSE, blue is DEFENCE**, and nothing else in the frame may sit near those hues. Read before adding anything the player looks at. |
| [`HUMAN.md`](HUMAN.md) | **The standing instructions in his own words**, which is the record of what has already been asked for and what has already been rejected. Check it before proposing something that sounds new. |
| [`art_refs/`](art_refs/) | The reference art the props were drawn from. ⚠️ `Art_Direction.md` § 4a records that the drawing-derived slippers were deleted and must not be rebuilt. |

## What comes after the port

| File | What it is |
|---|---|
| [`FUTURE.md`](FUTURE.md) | **The live-service and esport plan, in eighteen ordered phases, each with a prompt written to brief a whole session.** Accounts, profile and stats, telemetry, progression, cosmetics, social, matchmaking, competitive integrity, ranked, mastery paths, bots and population, modes and maps, seasons, controller, mobile, accessibility and localisation, tournaments and replays, distribution. **All eighteen prompts live in its § 19** with an index, § 0.5 is the standing preamble every prompt in both files inherits, and § 0.6 is the staleness protocol plus a PROMPT ZERO that refreshes both documents against the code. ⚠️ **It is a plan, not a decision that any of it ships**, and where it disagrees with `VISION.md` about what the game is, `VISION.md` wins. Every phase is costed against a free tier, because there is no budget. |
| [`INSPIRATION.md`](INSPIRATION.md) | **What to steal from thirty other games, and what it becomes in a four-player street game with a rotating taya.** The WHY behind `FUTURE.md`'s WHAT. Game by game with a "what it becomes here" for each, plus the queue-versus-mode structure (ranked is its own menu entry, not a third ruleset), the loadout and challenge-unlock design, achievements, and the problems a four-player free for all has that no borrowed system solves: **three of four players lose every match**, and a player far behind at the final round has nothing to play for. Carries ten paste-ready prompts in its § 8, a rejected register in § 10 recording every idea he has killed and why and a combined 27-step order with `FUTURE.md`'s phases in § 8.6. ⚠️ **A plan, not a decision.** |

## Port work

| File | What it is |
|---|---|
| [`Port_Plan.md`](Port_Plan.md) | The phase order for the Godot to Unity port, the exit criteria, and the reasoning. § 8 is the art replacement queue. |
| [`Port_Ledger.md`](Port_Ledger.md) | Every Godot script and scene with a CONVERTED / PARTIAL / MISSING status, measured from both trees. **The definition of done.** |
| [`Design_Drift_Report.md`](Design_Drift_Report.md) | The investigation into the eight places `Design.md` disagreed with the shipping code. All resolved: every one was stale prose. Kept as evidence, not as an open action. |

## Maps

| File | What it is |
|---|---|
| [`Ilalim_Ng_Tulay.md`](Ilalim_Ng_Tulay.md) | **The LRT Gilmore strip, and the only map with a design document.** Why its chalk box is the carriageway, the measured reason the other two maps feel wrong for Hero Strike, where each ultimate wants to be spent, what the train pass does in each mode, and how the map gets the other maps' palette. Read before changing anything in `IlalimNgTulayBuilder`. |

## Testing

| File | What it is |
|---|---|
| [`TESTING.md`](TESTING.md) | How to run each suite, what each probe measures, and what a failure means. |

## Onboarding a player

| File | What it is |
|---|---|
| [`Guided_Training.md`](Guided_Training.md) | The guided training flow: what a first-time player is taught, in what order, and what is deliberately left for them to find. |

## Art and characters

| File | What it is |
|---|---|
| [`Voxel_Person_Guide.md`](Voxel_Person_Guide.md) | How to author the next voxel character. A guide, not a handoff; nothing in it goes stale when a character lands. |
| [`Voxel_Person_Log.md`](Voxel_Person_Log.md) | What building ZACK actually cost, and why the code looks the way it does. |
| [`wearables_catalog.md`](wearables_catalog.md) | The modular voxel wearable catalog and its palette slot contract. |
| [`Godot_Character_Select_References/`](Godot_Character_Select_References/) | The twelve approved Godot captures the Unity character select is measured against. |

## Written for another tool, but useful here

| File | What it is |
|---|---|
| [`CANONICAL_RENDERING_PIPELINE.md`](CANONICAL_RENDERING_PIPELINE.md) | The in-engine render pipeline for character models: the four-step workflow, the two canonical outputs (4-angle turnaround and cast lineup), the versioned-filename rule, and five recorded pitfalls. ⚠️ **It was written for Antigravity and its "MANDATE FOR ALL AGENTS" heading is that tool's, not this one's.** The pipeline itself is correct and worth following; where anything in it disagrees with `CLAUDE.md`, `CLAUDE.md` wins here. `CLAUDE.md` § 6.1 has the short version. |

## A note on where these came from

⚠⚠ **`Design.md`, `Art_Direction.md`, `HUMAN.md` and `art_refs/` began as copies of the
Godot repo's boards and sat in a `docs/godot/` folder under a rule saying to edit them THERE
and copy them here.** That rule inverted the day this repo became the game, and after that the
folder name was telling every reader the opposite of the truth: it read as "the old engine's
paperwork" when it held the live balance document. Flattened into `docs/` on 2026-08-23.

**They are ordinary documents of this project now.** Edit them here. The Godot repo's versions
are frozen at the day it stopped being the game and must never be copied back over these.

Two files were dropped in the same pass: `Handoff_Open_Issues.md`, a Godot-era handoff about a
skin-sync bug that the port has long since passed and which § 2.4 of `CLAUDE.md` bans as a file
anyway, and the folder's own `README.md`, which this file replaces.

## Rules for anything added here

- **A new document gets a row in this table in the same commit.** A folder of documents with
  no map is a folder nobody reads, which is why this file exists.
- ⚠️ **NO HANDOFF PROMPTS.** `CLAUDE.md` § 2.4: a handoff goes in the chat reply, never into
  the repository. Two committed ones were deleted on 2026-08-23 (`ZACK_AND_EXPRESSIONS_HANDOFF.md`
  and `ZACK_HAIR_AND_ELECTRICITY_HANDOFF.md`); a stale handoff in a repo is worse than none,
  because the next session believes it.
- ⚠️⚠️ **AND A THIRD WAS DELETED ON 2026-08-26, ALONG WITH TWO OTHER DEAD FILES.** 🧑 pointed at
  the GitHub listing and said the docs folder was carrying useless files. Removed:
  - `CUSTOMIZATION_SYSTEM_PROMPT.md`, which called itself an "Agent Handoff & Implementation
    Blueprint" for a feature that was never built. It is the same rule as the two above, one
    heading away from being obvious. `wearables_catalog.md` keeps the part that was real: the
    voxel wearable geometry and its palette slot contract.
  - `character_bayan_reference.md`, a spec for a character who is in no roster the game ships,
    whose every reference image and render path pointed into a `.gemini/antigravity/brain/`
    directory that no longer exists. A reference document whose references are all dead links
    is a document that cannot be acted on.
  - `Feature_Audit.txt`, raw counts of GDScript functions "with no Unity counterpart" from an
    early survey. `Port_Ledger.md` answers the same question per script with a status that is
    kept current, and it is the definition of done. A stale second answer to a question that
    already has a maintained one only produces arguments about which is right.
- **Session-specific state belongs in `TODO.md`,** which is written to be ticked and added to.
  A document that needs a "where I left off" section is a `TODO.md` entry wearing a disguise.
