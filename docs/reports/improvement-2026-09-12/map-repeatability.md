# Saved map authoring repeatability

Baseline:0f08e992, ASTRAReworks. Unity6000.5.8f1. MapRepeatabilityCheck.Run calls
NeighborhoodFinishAuthor.FinishLoadedScene twice per map, saves assets/scenes and
reopens each saved scene before capture. Original scene bytes are backed up.

Comparison includes inactive GameObjects, serialized components, transforms,
renderers, colliders, external asset references, mesh/material contents and the
used ambient/fog/sun/reflection settings. Prefab bookkeeping and root order do not
define geometry; references resolve to hierarchy/component identity or asset GUID,
not regenerated Unity file/instance IDs. Embedded materials/meshes are content-hashed.

| Map | Compared rows | Baseline to first run | First to second run |
|---|---:|---:|---:|
| Eskinita | 2389 | 0 changes | 0 changes |
| Bayan Plaza | 3055 | 0 changes | 0 changes |
| Ilalim ng Tulay | 11223 | 0 changes | 0 changes |

Result: **PASS**, Logs/map-repeatability-v3/report.txt. Full before/run1/run2
snapshots and differences remain in that folder. Each run was guarded;17 existing
profile files were restored and hash-verified. Authored art file content is
unchanged. The three scene text diffs contain regenerated IDs; they are restored
to the pre-experiment clean source instead of committing thousands of ID changes.

The verifier itself needed two corrections, preserved in earlier logs: V1 refused
embedded scene materials without an external GUID; V2 descended into the raw-ID
children of already-normalized object references. V3 compares references once and
descends only into structural containers. Postprocessing V2's scene rows to remove
those ID children independently finds zero baseline-to-first-run changes on all
three maps. V2's red result is not evidence of map drift.

This verifies repeatability of the retained-scene finish authoring path, including
its save/reopen behavior. It is not a full wholesale map-builder equivalence test,
art approval, complete route coverage or an ordinary-play qualification.
