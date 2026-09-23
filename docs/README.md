# docs: what each file is, and when to read it

Rebuilt in the 2026-09-23 docs cleanup. Every document in `docs/` has exactly one row here,
grouped by status. **If you add a document, add its row in the same commit.** A folder of
documents with no map is a folder nobody reads.

## Start here, in this order

1. [`../AGENTS.md`](../AGENTS.md): current instructions and standing owner contracts.
2. [`ACTIVE_REWORK_LEDGER.md`](ACTIVE_REWORK_LEDGER.md): the exact resume state.
3. [`TODO.md`](TODO.md#current-implementation-queue): the one status queue and priority order,
   plus an index row for every numbered entry.
4. The section of [`NATIONALS_POLISH.md`](NATIONALS_POLISH.md) that owns the design you are
   touching. VISUAL-1 is
   [Visual communication and appeal pass](NATIONALS_POLISH.md#visual-communication-and-appeal-pass-visual-1-2026-09-23).

Everything else is reference. Read it when the task needs it.

## Where work is tracked

| File | Status | What it is |
|---|---|---|
| [`TODO.md`](TODO.md) | **Live queue** | Current queue, priority order, VISUAL-1, active items, then an index row per numbered entry. Only this file carries status. |
| [`TODO_Backlog.md`](TODO_Backlog.md) | **Live detail** | The whole bodies of the open numbered entries (§ 155 down to § 69), split out of TODO.md on 2026-09-23 without edits apart from anchors and moved-file links. A `docs/TODO.md § N` pointer lands on the TODO index row, which links here. |
| [`TODO_Archive.md`](TODO_Archive.md) | Record | Every finished section and batch report, whole, under its original number. Numbers are not unique (§ 53, 63, 64, 65 repeat); search by title too. |
| [`ACTIVE_REWORK_LEDGER.md`](ACTIVE_REWORK_LEDGER.md) | **Live state** | Compact resume pointer: current feature, tested revision, dirty work, jobs, blockers, next step. |
| [`../Attention.md`](../Attention.md) | Live | Work blocked on a human and nothing else. |

## Direction and rules

| File | Status | What it is |
|---|---|---|
| [`VISION.md`](VISION.md) | Live | What the game is for: both modes ship, the readability budget (§ 2, rule 5: every mid-fight screenshot shows the lata, the chalk and every player), how powers are learned (§ 3: no sentences in the match HUD), what is settled. |
| [`NATIONALS_POLISH.md`](NATIONALS_POLISH.md) | **Live design** | Detailed design for presentation and polish. Top section: VISUAL-1 (2026-09-23). Then the 2026-09-21 delivery design and the research-driven direction, then preserved earlier direction. Status stays in TODO. |
| [`Design.md`](Design.md) | Live, source of truth | Every Classic balance number and why. § 13 lists what it does not govern. This copy is live; the Godot repo's is frozen. |
| [`Hero_Strike_Balance.md`](Hero_Strike_Balance.md) | Live | Hero Strike footprint table (§ 1) and economy (§ 2). §§ 3 and 4 are an unbuilt proposal. |
| [`Formats.md`](Formats.md) | Live | The two extra formats played inside either mode, and their rules. |
| [`Art_Direction.md`](Art_Direction.md) | Live | Section 0 first: new models must match the cute blocky style. Then the colour law (Offense orange, Defense blue), scale, arena geometry, the toon pass and asset sources. |
| [`../CLAUDE.md`](../CLAUDE.md) | History and incident reasons | Loaded automatically by Claude sessions. Current routing and owner decisions are in AGENTS.md; CLAUDE.md keeps the reasoning behind every rule. |
| [`HUMAN.md`](HUMAN.md) | Live | The owner's standing instructions in his own words, including what was rejected. Check before proposing something that sounds new. |
| [`GAME_OVERVIEW.md`](GAME_OVERVIEW.md) | Reference | The whole game in one readable file. A map, not a source of truth; Design.md and Balance.cs win. |

## Active topic plans

Queued after older actionable work: [REFINE-2 research/execution plan](reports/map-by-map-refinement-2026-09-23/research-and-execution-plan.md),
[exact owner feedback](reports/map-by-map-refinement-2026-09-23/owner-request.md) and
[initial foundations research](reports/map-by-map-refinement-2026-09-23/foundation-research.md).
Map-by-map, character/movement-by-character/movement; research and implementation remain open.
Older source/evidence reconciliation: [2026-09-23 review](reports/legacy-review-2026-09-23/report.md).

| File | Status | What it is |
|---|---|---|
| [`MAP_FINAL_PASS.md`](MAP_FINAL_PASS.md) | Active (152.4) | Map final pass, reactivated 2026-09-21. Resume from current source and evidence. |
| [`MAP_TRANSFORMATION_PLAN.md`](MAP_TRANSFORMATION_PLAN.md) | Active (152.4) | The 2026-09-12 map transformation: Filipino architecture, shops, signs, paving, town context. |
| [`OWNER_PLAYTEST_REVISION.md`](OWNER_PLAYTEST_REVISION.md) | Active (152.4) | Owner playtest corrections of 2026-09-13: rooftop pool, swimming, throwing. |
| [`BADJAO_EXPANSION.md`](BADJAO_EXPANSION.md) | Active (final integration) | Rafi, the seventh hero, and the lagoon water village. Latest style correction at the top. |
| [`Ilalim_Ng_Tulay.md`](Ilalim_Ng_Tulay.md) | Reference | The LRT Gilmore map's design, and why its box is the carriageway. |
| [`AMBIENT_LIFE_PLAN.md`](AMBIENT_LIFE_PLAN.md) | Active (1.5) | Street animals and birds: gait, contact, fleeing, staged responses. |
| [`PLAY_FEEL_REWORK_PLAN.md`](PLAY_FEEL_REWORK_PLAN.md) | Partly implemented | Movement, throw and pektus, slipper and can identity. Dated "not implemented" lines are history. |
| [`ABILITY_REWORK_PLAN.md`](ABILITY_REWORK_PLAN.md) | Partly implemented | Hero ability rework and its current index. Older OPEN headings are design history. |
| [`HERO_KIT_REWORK_DECISIONS.md`](HERO_KIT_REWORK_DECISIONS.md) | Reference | Kit decisions and distinct loadout jobs; consult ABILITY_REWORK_PLAN's index first. |
| [`PHILIPPINE_ABILITY_DIRECTION.md`](PHILIPPINE_ABILITY_DIRECTION.md) | Live direction | Philippine direction for the hero kits; Dante's shields may be refined only with a clear improvement. |
| [`UI_REMAINING_TODO.md`](UI_REMAINING_TODO.md) | Detail notes | Owner UI revisions (PC loadout, controller styling, Classic people carry no stats). Status lives in TODO § 153 / U8. |
| [`Hero_Strike_UI.md`](Hero_Strike_UI.md) | Reference | What Hero Strike puts on screen and what it deliberately does not. VISUAL-1.4 updates the in-match layout. |
| [`Front_End_Design.md`](Front_End_Design.md) | Reference | The five front-end screens and their journeys. Does not cover the in-match layer. |
| [`OWNER_UI_AUTHORING.md`](OWNER_UI_AUTHORING.md) | Live tooling | How to edit the owner-painted UI. The current composition is not approved; the tooling is. |
| [`FONT_USAGE.md`](FONT_USAGE.md) | Live | Darumadrop, Kawit Extended and Lydian, and the job of each. |
| [`Guided_Training.md`](Guided_Training.md) | Reference | What the guided training teaches, in what order. |
| [`CHARACTER_ORIGINS.md`](CHARACTER_ORIGINS.md) | Reference | Fictional hometowns and court stories for the heroes. See also [`../LORE.md`](../LORE.md). |

## Art, characters and pipeline

| File | Status | What it is |
|---|---|---|
| [`CANONICAL_RENDERING_PIPELINE.md`](CANONICAL_RENDERING_PIPELINE.md) | Live | In-engine render workflow: turnaround, cast lineup, motion strip (step 3b), versioned filenames. Written for another tool; where it disagrees with CLAUDE.md or AGENTS.md, those win. |
| [`Voxel_Person_Guide.md`](Voxel_Person_Guide.md) | Live | How to author a voxel character. Rafi's recipe is a copy (`tools/build_rafi_voxel.py`). |
| [`Voxel_Person_Log.md`](Voxel_Person_Log.md) | Reference | What building ZACK cost and why the code looks the way it does. |
| [`wearables_catalog.md`](wearables_catalog.md) | Reference | Modular wearable catalogue and palette slot contract. |
| [`Asset_Sourcing.md`](Asset_Sourcing.md) | Live | Zero-cost CC0 source list for Hero Strike VFX and SFX. Implementation is TODO § 131. |
| [`../ASTRA.md`](../ASTRA.md) | Reference | Rig and model notes for the ASTRA rework (Kuro and others). |
| [`art_refs/`](art_refs/) | Reference | Prop reference art. The drawing-derived slippers were deleted and must not be rebuilt. |
| [`refs/`](refs/) | Reference | Supplied reference material (Kuro, style, the UI PDF). Not shipped textures. |
| [`Godot_Character_Select_References/`](Godot_Character_Select_References/) | Reference | Twelve approved Godot captures the character select is measured against. |
| [`tooling/`](tooling/) | Reference | Skill reference bundles and manifests. |

## Engineering, testing and machines

| File | Status | What it is |
|---|---|---|
| [`TESTING.md`](TESTING.md) | Live | How to run each suite, what each probe measures, what a failure means. |
| [`WORKSTATION_SETUP.md`](WORKSTATION_SETUP.md) | Reference | Reproducible workflow. Paths in it are historical; discover the real checkout and tools. |
| [`CLAUDE_ENGINEERING_LANE.md`](CLAUDE_ENGINEERING_LANE.md) | Reference | The 2026-09-15 separate-PC engineering lane. Ownership was handed back on 2026-09-21. |
| [`CLAUDE_REQUEST_SAFETY_LANE.md`](CLAUDE_REQUEST_SAFETY_LANE.md) | Reference | The C4 request-safety lane. Handed back 2026-09-21; its 149.4 checks remain in TODO. |
| [`Port_Plan.md`](Port_Plan.md) | Reference | Godot to Unity phase order and exit criteria; § 8 is the art replacement queue. |
| [`Port_Ledger.md`](Port_Ledger.md) | Reference | Every Godot script and scene with a CONVERTED / PARTIAL / MISSING status. |
| [`Design_Drift_Report.md`](Design_Drift_Report.md) | Record | The eight places Design.md disagreed with code; all were stale prose. |
| [`reports/`](reports/) | Evidence | Dated evidence folders and receipts. Never edited after the fact, so an old report may name a moved path; see `archive/README.md`. Entry points indexed before: [`pending-casts-2026-09-15`](reports/pending-casts-2026-09-15/README.md), [`fpp-shadow-version-2026-09-15`](reports/fpp-shadow-version-2026-09-15/README.md), [`movement-joining-2026-09-15`](reports/movement-joining-2026-09-15/README.md), [`claude-engineering-2026-09-15`](reports/claude-engineering-2026-09-15/README.md), and the current [`full-backlog-2026-09-21`](reports/full-backlog-2026-09-21/README.md) and [`presentation-pass-2026-09-21`](reports/presentation-pass-2026-09-21/README.md). |

## Historical, kept because pointers reference them

| File | What it is |
|---|---|
| [`FUTURE.md`](FUTURE.md) | The retired live-service roadmap. Its § references stay valid; its prompts are not the work order. |
| [`INSPIRATION.md`](INSPIRATION.md) | What to take from thirty other games and what it becomes here; historical priorities. |
| [`IMPROVEMENT_PLAN.md`](IMPROVEMENT_PLAN.md) | P1 to P10 reference work from 2026-09-10; the order in it is superseded. |
| [`archive/`](archive/README.md) | Superseded plans and status files moved on 2026-09-23, with an old-path table. Never a task source. |

## Where the ported documents came from

`Design.md`, `Art_Direction.md`, `HUMAN.md` and `art_refs/` began as copies of the Godot
repo's boards and sat in a `docs/godot/` folder under a rule saying to edit them there and
copy them here. That rule inverted the day this repo became the game; the folder was
flattened into `docs/` on 2026-08-23. They are ordinary documents of this project now. The
Godot repo's versions are frozen and must never be copied back over these. Two files were
dropped in that pass: `Handoff_Open_Issues.md` (a Godot-era handoff) and the folder's own
`README.md`, which this file replaces.

## Rules for anything added here

- **A new document gets a row in this index in the same commit.**
- **No handoff prompts.** A handoff goes in the chat reply, never into the repository.
  Removed for that reason: `ZACK_AND_EXPRESSIONS_HANDOFF.md` and
  `ZACK_HAIR_AND_ELECTRICITY_HANDOFF.md` (2026-08-23), and on 2026-08-26
  `CUSTOMIZATION_SYSTEM_PROMPT.md` (a blueprint for a feature never built; the real part lives
  in `wearables_catalog.md`), `character_bayan_reference.md` (a spec for a character in no
  roster, all references dead) and `Feature_Audit.txt` (a stale second answer to what
  `Port_Ledger.md` maintains).
- **Session state belongs in TODO.md and the ledger.** A document that needs a "where I left
  off" section is a TODO entry in disguise.
- **Superseded is moved, not deleted.** Move the file to `archive/`, rewrite links outside
  `reports/`, add a row to `archive/README.md`.
