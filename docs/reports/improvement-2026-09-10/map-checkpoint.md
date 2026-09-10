# Map refinement checkpoint

This is a partial map checkpoint, not completion of the whole map/game pass. The
owner explicitly requested wrapping up and a continuation handoff, including
unfinished maps. The active continuation plan is ../../MAP_FINAL_PASS.md.

## Implemented

- Three original Blender broadleaf models replace the installed cone/conifer
  silhouettes: street-broadleaf, plaza-shade and courtyard-tree. The final pass
  places33 on Eskinita,29 on Bayan and1 on Ilalim; seven reachable trunks have
  narrow collision. Source assets remain available and the new native sources are
  under MapSource/environment/urban-trees.
- Non-solid chairs, stools and crate stacks are grouped beyond the playable walls
  in Eskinita and Bayan. Excess seating remains retained but inactive. The solid
  Bayan monument and Ilalim supports/kiosks/carts retain their existing contracts.
- Bayan gains church nave/roof/buttress mass, municipal roof end walls and a
  continuous stone court with restrained variation. The first bright paving gaps
  were removed, and the surface sits below the existing chalk.
- Ilalim's oversized wire bundles are replaced by78 conductor spans joining the
  existing crossarms, with44mm visual diameter. The original shop/jeepney livery
  remains. Generic extra shop canopies were reviewed and removed from the scene.
- All three maps receive a more neutral hemispherical light balance. Builder and
  serialized grade values agree: brightness1, contrast1.02, saturation1, exposure1,
  white1.9. The original neighborhood/civic/guideway modeling pass is retained.

MapFinalPassAuthor is hooked into NeighborhoodFinishAuthor, which is already used
by the scene editing path, TSCN importer and Ilalim builder. It owns its named
MapFinalPass group and generated materials/meshes. Run it through the guarded
Unity launcher with Unity otherwise closed. Verify repeatability before treating
the generator as fully qualified; a semantic two-run comparison remains open.

## Evidence and qualification

Before: Logs/maps-before-final-pass. First pass: Logs/maps-final-v1. Refined
checkpoint: Logs/maps-final-v2. These use the same OrdinaryPlayOnEveryMap probe:
eight seconds of Classic play and four1.65m witness-camera views per map. Captures are
automated observations, not human taste approval.

V1 compiled, passed all8 editor checks and the ordinary-play capture1/1. A fresh
wrap-up EditMode run caught one real authoring mismatch: the Ilalim builder still
declared contrast1.03 while the scene used1.02. The builder/intermediate author
were corrected to match the final scene without weakening the assertion.
Final fresh verification: EditMode489/489, all8 editor checks, all14 gating source
audits, and OrdinaryPlayOnEveryMap1/1. Core was unchanged from its562/562 run.
The final XML is live-tests-20260910-134206-5222.xml for EditMode and
live-tests-20260910-134605-9205.xml for map playback. Checks return True in
Logs/map-checkpoint-checks-v2.txt. Source-audit logs are in
Logs/map-checkpoint-audits. Final silent clips preserve capture timestamps:
Eskinita8.02s/156frames, Bayan7.98s/158frames, Ilalim8.01s/157frames.

The final Editor exited normally and the guard restored17 existing profile files.
No Unity/Blender/player process is left running by the task. Known test-only arm
tangent serialization and quality-setting changes were restored before commit.
These captures do not replace exact-player first-person review or prove every
route/edge case. Their camera/probe is shared; strict quality-profile comparison
and the broader visual review remain continuation work.

| Map | Before | Current partial checkpoint |
|---|---|---|
| Eskinita | [Before](map-checkpoint-images/Eskinita-before.png) | [After](map-checkpoint-images/Eskinita-after.png) |
| Bayan Plaza | [Before](map-checkpoint-images/BayanPlaza-before.png) | [After](map-checkpoint-images/BayanPlaza-after.png) |
| Ilalim ng Tulay | [Before](map-checkpoint-images/IlalimNgTulay-before.png) | [After](map-checkpoint-images/IlalimNgTulay-after.png) |

## Explicitly unresolved

- Full side/back composition review, broader retrieval/edge/kerb/obstacle route
  coverage, the historical Ilalim48-idle-penalty seed, and normal-speed player
  assessment remain open. These maps are not declared finished.
- The slipper ground query can return the9.04m guideway top from a point3.6m
  above the road but still underneath the bridge. However, changing only the
  flight query did NOT resolve the exploratory HostThrow test's reset near the
  owner. That runtime attempt was reverted. The experiment and patch are retained
  locally in Logs/viaduct-flight-investigation and as non-compiled text under
  [map-investigation](map-investigation/README.md); its failing test is not in the
  committed gate. Trace the actual position writer and isolate the fixture before
  claiming a fix or attributing the old idle outlier to it.
- Switching a resident Editor from PlayMode tests to a full EditMode run reached
  recursive GenericPadBridge.Sync/InputSystem.RemoveDevice callbacks. The Editor
  stopped answering Pipeline commands and was terminated after checking its exact
  project/log identity. The guard restored17 profile files. Controller source was
  untouched; inspect its ownership instructions and this lifecycle case separately.
  Logs/map-wrap-validation.log retains the trace. Fresh-editor verification is
  distinct from that stalled run; the latter is not a pass.
- No new final Windows player is delivered by this partial checkpoint. The
  previous Desktop/internal executables do not represent the latest source.

Kuro forms/expressions were previously pushed at864cead. Their remaining
protocol28 multi-process qualification, roster-sheet refresh, broader all-hero/
alternative/animation/SFX/VFX work, network matrices and final release checks
remain open in the active ledger and related plans.
