# docs: what each file is, and when to read it

⚠️ **This index exists because the project gets handed between sessions and tools, and a
folder of eighteen documents with no map is a folder nobody reads.** If you add a document,
add its row here in the same commit.

---

## Read these, in this order

The current compact action plan is [EXECUTION_PLAN.md](EXECUTION_PLAN.md), written
at the owner's request for compaction-safe continuation. Read it with the newest
active-ledger pointer after AGENTS/VISION/TODO.

For the current continuation, read [ACTIVE_REWORK_LEDGER.md](ACTIVE_REWORK_LEDGER.md)
after the required rulebook/VISION/TODO order. [MAP_FINAL_PASS.md](MAP_FINAL_PASS.md)
tracks the unfinished map pass, references and verified iterations. The owner now
permits unfinished maps in the requested wrap-up handoff. Kuro's latest source,
expressions, tests and limitations are in
[kuro-matching-forms.md](reports/improvement-2026-09-10/kuro-matching-forms.md).
The partial map implementation and its explicit limitations are in
[map-checkpoint.md](reports/improvement-2026-09-10/map-checkpoint.md).
The new continuation's isolated flight correction and saved-authoring verification
are in [map-retrieval.md](reports/improvement-2026-09-12/map-retrieval.md) and
[map-repeatability.md](reports/improvement-2026-09-12/map-repeatability.md).

The active 2026-09-09 improvement pass also uses
[IMPROVEMENT_PLAN.md](IMPROVEMENT_PLAN.md) for durable progress,
[CALM_FRONT_END.md](CALM_FRONT_END.md) for the supplied UI direction and flow,
and [LORE.md](../LORE.md) for the sporting world and character/place connections.
[FONT_USAGE.md](FONT_USAGE.md) records the supplied faces, their roles and import fixes.
They preserve the required rulebook, VISION and TODO read order below.

| File | What it is |
|---|---|
| [`../AGENTS.md`](../AGENTS.md) | **First, always.** Self-contained current instructions for ChatGPT/Codex: scope, branch/checkout, gameplay/art contracts, verification, compaction continuity and delivery. |
| [`../CLAUDE.md`](../CLAUDE.md) | Historical incident reasoning and receipts. Consult relevant sections; current instruction routing and owner decisions are in AGENTS.md. |
| [`VISION.md`](VISION.md) | **What the game is FOR.** The two modes and why both ship, the readability budget, how a player is meant to learn a power, what is settled. Read before making a design call. |
| [`TODO.md`](TODO.md) | **What is actually open, and nothing else.** What is wrong, where it lives, what done looks like. Check before inventing a task; update in the same commit as the work. A section lives here while its HEADING says OPEN, IN PROGRESS or NOT DONE. |
| [`TODO_Archive.md`](TODO_Archive.md) | **The record: every finished section and every batch report, whole, under its original number.** Split out on 2026-09-03, when `TODO.md` had reached 22,930 lines and stopped being readable. ⚠️ **Nothing was deleted and nothing was renumbered**, so a `docs/TODO.md` § N pointer written anywhere in this repository still resolves: `TODO.md` keeps an index row for each and sends you here. ⚠️ Numbers are not unique (§ 53, § 63, § 64, § 65 repeat); search by title too. |
| [`GAME_OVERVIEW.md`](GAME_OVERVIEW.md) | **The whole game in one file, for a reader rather than for an editor.** Every mode, rule, verb and number a player can feel, all eighteen hero powers, the twelve street characters, the six heroes, the six lata and the ten tsinelas with their trait rows. ⚠️ **It is a map, not a source of truth**: `Design.md` and `Balance.cs` win over it, and where it disagrees with them it is the file to fix. |
| [`Design.md`](Design.md) | **The balance source of truth.** Every number that decides the game, and why. § 13 lists what it does NOT govern. ⚠️ **This copy is the live one**; the Godot repo's is the frozen 2026-08-02 original. |
| [`Hero_Strike_UI.md`](Hero_Strike_UI.md) | **What Hero Strike puts on screen and what it deliberately does not.** The ability bar, the charge readout and the cooldown language, measured against the same `VISION.md` § 2 readability budget the abilities themselves are. |
| [`Hero_Strike_Balance.md`](Hero_Strike_Balance.md) | **What `Design.md` § 13 hands off.** § 1 is the per-ability floor footprint table, measured against the `VISION.md` § 2 readability budget, and it is the only place that table has ever existed. § 2 is the cooldown and ultimate economy as shipped. §§ 3 and 4 are the rework proposal and are **not built**. |
| [`Front_End_Design.md`](Front_End_Design.md) | **The five front-end screens, designed: one theme, and five screens that are not each other.** `CLAUDE.md` § 6.2, § 6.2b, § 6.2c and § 6.3 plus `FUTURE.md` § 0.5b, applied to the lobby, settings, character select, login and profile for `docs/TODO.md` § 133. Carries the split that resolves 🧑's two pulling briefs (**repeat the chrome, never the composition**), the recurring marks that guide without teaching, each screen's anchor colour and borrowed archetype, the two-face type rule with the measurement that decided where ALL CAPS may go, the palette's six roles, and every journey walked out loud with its press count. ⚠️ **The in-match layer is not in it and must not be touched in that pass.** |
| [`Art_Direction.md`](Art_Direction.md) | **Start with section0: new models must match TUMP's cute blocky style and avoid excessive detail.** Then the colour law, the scale and height laws, arena geometry, and which tool produces which asset.** § 1 is the one that never bends: **orange is OFFENSE, blue is DEFENCE**, and nothing else in the frame may sit near those hues. Read before adding anything the player looks at. |
| [`Asset_Sourcing.md`](Asset_Sourcing.md) | **The verified zero-cost source list for the Hero Strike VFX and SFX replacement pass.** Maps every one of the eighteen abilities to specific CC0 art, lists recorded CC0 sound sources, records public-repository licence limits, and keeps later building/map candidates without touching the existing characters. ⚠️ **The implementation is `TODO.md` § 131**, and three tools carry it: `tools/fetch_asset_sources.py` rebuilds the gitignored download cache without an account, `tools/build_vfx_sheets.py` recolours the art into `UiTheme`'s own hero families, and `tools/build_ability_audio.py` replaces the cues. The licence for what actually ships travels with the art in `Assets/TumbangPreso/Resources/Vfx/SOURCES.txt`. |
| [`HUMAN.md`](HUMAN.md) | **The standing instructions in his own words**, which is the record of what has already been asked for and what has already been rejected. Check it before proposing something that sounds new. |
| [`art_refs/`](art_refs/) | The reference art the props were drawn from. ⚠️ `Art_Direction.md` § 4a records that the drawing-derived slippers were deleted and must not be rebuilt. |

## What comes after the port

| File | What it is |
|---|---|
| [`../AGENTS.md`](../AGENTS.md) | **Small agent routing guide.** Points to the canonical read order, polish roadmap and existing ownership queues; `CLAUDE.md` wins on conflicts. |
| [`NATIONALS_POLISH.md`](NATIONALS_POLISH.md) | **The current strategic roadmap for game feel, presentation and Nationals polish.** What matters next and why, the quality bar, priority tiers and bounded first batch. Execution remains in `TODO.md`, `../ASTRA.md` and `../Attention.md`; UI/HUD design is out of scope. |
| [`FUTURE.md`](FUTURE.md) | **The retired systems-era roadmap.** Historical phases, prompts and reasoning for the live-service and competitive systems. Preserved section references remain valid; its prompts are no longer the default next-work order. See `NATIONALS_POLISH.md` for current priorities. |
| [`INSPIRATION.md`](INSPIRATION.md) | **What to steal from thirty other games, and what it becomes in a four-player street game with a rotating taya.** The WHY behind `FUTURE.md`'s WHAT. Game by game with a "what it becomes here" for each, plus the queue-versus-mode structure (ranked is its own menu entry, not a third ruleset), the loadout and challenge-unlock design, achievements, and the problems a four-player free for all has that no borrowed system solves: **three of four players lose every match**, and a player far behind at the final round has nothing to play for. Carries ten paste-ready prompts in its § 8, a rejected register in § 10 recording every idea he has killed and why and a combined 27-step order with `FUTURE.md`'s phases in § 8.6. ⚠️ **Historical research and priorities, not the current implementation order; see `NATIONALS_POLISH.md`.** |

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
| [`CANONICAL_RENDERING_PIPELINE.md`](CANONICAL_RENDERING_PIPELINE.md) | The in-engine render pipeline for character models: the four-step workflow, the two canonical outputs (4-angle turnaround and cast lineup), **the motion strip that photographs a clip ACROSS its length (step 3b, added 2026-09-09)**, the versioned-filename rule, and five recorded pitfalls. ⚠️ **It was written for Antigravity and its "MANDATE FOR ALL AGENTS" heading is that tool's, not this one's.** The pipeline itself is correct and worth following; where anything in it disagrees with `CLAUDE.md`, `CLAUDE.md` wins here. `CLAUDE.md` § 6.1 has the short version. |

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

- [Substantial map transformation plan](MAP_TRANSFORMATION_PLAN.md): active owner-feedback revision after V9, including nostalgic Filipino architecture, coherent shops/signs, paving and broader town context.

- [Play-feel/equipment plan](PLAY_FEEL_REWORK_PLAN.md): post-map movement, throw/Pektus, slipper/can identity and full action verification.
