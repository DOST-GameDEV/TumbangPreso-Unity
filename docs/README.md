# docs: what each file is, and when to read it

⚠️ **This index exists because the project gets handed between sessions and tools, and a
folder of thirteen documents with no map is a folder nobody reads.** If you add a document,
add its row here in the same commit.

---

## Read these, in this order

| File | What it is |
|---|---|
| [`../CLAUDE.md`](../CLAUDE.md) | **First, always.** The rules of the repository: which git repo is live, the engine-free core rule, the build and test commands, the traps on this machine. |
| [`VISION.md`](VISION.md) | **What the game is FOR.** The two modes and why both ship, the readability budget, how a player is meant to learn a power, what is settled. Read before making a design call. |
| [`TODO.md`](TODO.md) | **What is actually open.** What is wrong, where it lives, what done looks like. Check before inventing a task; update in the same commit as the work. |
| [`godot/Design.md`](godot/Design.md) | **The balance source of truth.** Every number that decides the game, and why. § 13 lists what it does NOT govern. ⚠️ **This copy is the live one**; the Godot repo's is the frozen 2026-08-02 original. |

## Port work

| File | What it is |
|---|---|
| [`Port_Plan.md`](Port_Plan.md) | The phase order for the Godot to Unity port, the exit criteria, and the reasoning. § 8 is the art replacement queue. |
| [`Port_Ledger.md`](Port_Ledger.md) | Every Godot script and scene with a CONVERTED / PARTIAL / MISSING status, measured from both trees. **The definition of done.** |
| [`Design_Drift_Report.md`](Design_Drift_Report.md) | The investigation into the eight places `Design.md` disagreed with the shipping code. All resolved: every one was stale prose. Kept as evidence, not as an open action. |
| [`Feature_Audit.txt`](Feature_Audit.txt) | Raw audit output from the port survey. Reference only. |

## Testing

| File | What it is |
|---|---|
| [`TESTING.md`](TESTING.md) | How to run each suite, what each probe measures, and what a failure means. |

## Art and characters

| File | What it is |
|---|---|
| [`Voxel_Person_Guide.md`](Voxel_Person_Guide.md) | How to author the next voxel character. A guide, not a handoff; nothing in it goes stale when a character lands. |
| [`Voxel_Person_Log.md`](Voxel_Person_Log.md) | What building ZACK actually cost, and why the code looks the way it does. |
| [`character_bayan_reference.md`](character_bayan_reference.md) | Bayan's visual specification and iteration notes. |
| [`wearables_catalog.md`](wearables_catalog.md) | The modular voxel wearable catalog and its palette slot contract. |
| [`CUSTOMIZATION_SYSTEM_PROMPT.md`](CUSTOMIZATION_SYSTEM_PROMPT.md) | A standalone specification for the not-yet-built character customization feature. |
| [`Godot_Character_Select_References/`](Godot_Character_Select_References/) | The twelve approved Godot captures the Unity character select is measured against. |

## Written for another tool, but useful here

| File | What it is |
|---|---|
| [`CANONICAL_RENDERING_PIPELINE.md`](CANONICAL_RENDERING_PIPELINE.md) | The in-engine render pipeline for character models: the four-step workflow, the two canonical outputs (4-angle turnaround and cast lineup), the versioned-filename rule, and five recorded pitfalls. ⚠️ **It was written for Antigravity and its "MANDATE FOR ALL AGENTS" heading is that tool's, not this one's.** The pipeline itself is correct and worth following; where anything in it disagrees with `CLAUDE.md`, `CLAUDE.md` wins here. `CLAUDE.md` § 6.1 has the short version. |

## Rules for anything added here

- **A new document gets a row in this table in the same commit.** A folder of documents with
  no map is a folder nobody reads, which is why this file exists.
- ⚠️ **NO HANDOFF PROMPTS.** `CLAUDE.md` § 2.4: a handoff goes in the chat reply, never into
  the repository. Two committed ones were deleted on 2026-08-23 (`ZACK_AND_EXPRESSIONS_HANDOFF.md`
  and `ZACK_HAIR_AND_ELECTRICITY_HANDOFF.md`); a stale handoff in a repo is worse than none,
  because the next session believes it.
- **Session-specific state belongs in `TODO.md`,** which is written to be ticked and added to.
  A document that needs a "where I left off" section is a `TODO.md` entry wearing a disguise.
