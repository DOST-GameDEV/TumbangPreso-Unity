# Repository cleanup, 2026-10-25

Source baseline: BugFix `47487eda7`, with main `2239d8d1b` confirmed as an
ancestor after recovering the local shallow boundary. This report covers the
documentation/asset unit; spectator and network implementation are separate.

## Documentation coverage and decisions

The inventory read all 1,505 tracked Markdown files (14,062,381 bytes) for
structure, role, stale markers and local links. It classified 1,357 reports/archive
documents, 18 vendor documents and 130 instruction/reference documents. Active
instructions, source/configuration and relevant current methods were reviewed for
consistency. This is not a claim of fresh behavioral validation for every report.

- AGENTS is the short authority; CLAUDE is a compatibility pointer. Removed expired
  branch prohibitions, machine reservations and repeated incident narratives.
- README/context/routes use actual Unity/NGO/input/rendering/source facts. Nine
  hero entries and six current unlocks replace the old six-entry/five-unlock claims.
- TODO is a compact current queue. Old IDs/bodies and all useful history remain
  preserved; [retired work](../RETIRED_WORK.md) explains why old unchecked proposals
  no longer assign work. No unperformed peer/device/taste check was marked passed.
- The active ledger holds one current checkpoint rather than a stack of finished
  sessions. Protocol history moved out of the live authoring contract.
- Specialized authoring references, provenance, source art and vendor documents
  remain available on demand. Reports and archives have explicit historical scope.
- `tools/audit_docs.py` inventories every Markdown path and checks live links,
  entry-point size budgets and expired branch commands without loading the corpus.

Complete replaced wording is in the
[snapshot](../archive/cleanup-2026-10-25/README.md). Existing dated evidence was
preserved; new identical snapshots of unchanged reference bodies were not kept.

## Asset and credit scope

The obsolete Kenney environment/kits courtesy row is removed from CreditsContent.
Required sourced-art/font credits remain. The small Kenney controller-prompt
source files remain because `build_pad_prompt_icons.py` still generates the
shipped controller glyph sheet from them; their provenance/license files remain.
Unused-model and retired-preview removal results are recorded after dependency
checks below. No Git history rewrite or source-art quality reduction is part of cleanup.

- Removed 12 video/poster/receipt/metadata files for disabled, nonselectable
  BayanPlaza and SaBubong: 98,377,639 bytes. Their six GUIDs had no external
  serialized/code/JSON references. Current SceneFlow.Maps selects six other maps.
- Audited 575 legacy kit GLBs against serialized GUID references and source/script
  stem uses. Removed 399 unused models plus 399 metadata files: 14,075,420 bytes.
  Dynamic city-building and commercial families were retained conservatively.
  Textures, licenses, used models and authored sources were retained.
  [Exact model removal manifest](repository-cleanup-2026-10-25-removed.txt).
- Total asset removal: 112,453,059 bytes (about 107.2 MiB). This reduces fresh
  checkouts and retired Resources content; existing Git history remains recoverable.
- Seven frequently loaded entry documents dropped from 750,438 to 17,826 bytes,
  a 97.62% reduction. The broader live guides are loaded only by topic.

## Validation

- Fresh .NET Core run: 832 passed, 0 failed, 0 skipped; `Logs/cleanup-1025/core.trx`.
  An overlapping first compiler attempt hit a file lock; the successful run wrote
  fresh results. This is managed rules evidence, not native/multiplayer acceptance.
- `python tools/audit_docs.py`: zero missing live links, size-budget failures or
  expired branch commands. `git diff --check` passes; the audit script compiles.
- Asset dependency review found no GUID roots/source stem uses for removed models;
  dynamic families and active inputs were excluded from deletion.
- Unity 6000.5.8f1 compiled the cleaned source without C# errors in the named-profile
  spectator baseline launch. Existing obsolete-API warnings remain. Asset import
  and the targeted native tests are still running at this checkpoint; no runtime
  or multiplayer acceptance is claimed from compilation.

Published as `aaf96e52e114e362149dbc47ca587dbaeea2ecb1`. Original BugFix
`47487eda7` and previous main `2239d8d1b` are ancestors. The remote main was
fast-forwarded, BugFix deleted after verification, and NETWORKFIX created at
the merged cleanup commit. The old checked-out local branch was renamed to
preserve existing dirty work. Ongoing spectator/network evidence belongs to
its own report rather than expanding this cleanup record.
