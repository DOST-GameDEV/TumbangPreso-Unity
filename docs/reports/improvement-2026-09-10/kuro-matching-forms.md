# Kuro: matching purple forms and expressive idles

Status: implemented and locally verified; remaining release/network work is
deferred under the owner's subsequent map-only delivery scope.
This supersedes the earlier Kuro personality model report. The owner rejected the
round/crab-armed, realistic reconstructed and noisy blockified giant studies.
None of those meshes is the basis of this version.

The new native Blender giant uses a small set of deliberate chamfered forms, a
deep open maw, floating eye shards, large hands and a curled spirit tail. Its
18-bone rig includes jaw, hand and individual finger motion in a1.8s inhale/grasp
cycle. The authored loop is measured for floor clearance before export. The game
keeps the requested approximately4.8m height, rather than reducing it to avoid
camera problems. Both forms use the same deep violet/lavender family.

Small Kuro echoes the giant's broad head, crown curl and curved lower body, with
friendly graphic features. Three occasional idle gestures add actual distinct
face geometry: a cat grin, crossed eyes with an open little mouth, and a shy
squint/pout. Each has its own head/body/hand gesture. The existing private cosmetic
random stream chooses them alongside the earlier idles after standing still;
movement and casts take priority. Expressions clear before the giant transform.
Thin hands/crown curls do not get an expanded ink hull that creates black seams.

Eleven reusable idle clips are baked from the same deterministic runtime sampler.
No new emote menu or player mechanic is introduced. The giant has a separate
native clip, extracted into Resources/KuroRageInhale.anim for runtime Playables.

## Casting and networking

The following familiar now moves into the existing3.5m forward cast reach during
the0.4s invocation. This deliberately changes the normal cast from the shoulder
location to the shown point ahead, so the giant does not engulf the owner camera.
The position is swept against the existing world collision and clamped to the
court. Possessed Kuro keeps the player's chosen location. Both grow on the same
ground point used by the actual pulling field, face the caster, and retain the
existing4m pull radius/7s duration. Nemu does not teleport when casting.

The accepted aim and ground point remain fixed through windup. Resetting that
windup resumes normal following. Protocol28 carries the accepted familiar yaw
with the reliable effect snapshot, so observers and reconnecting players do not
invent its facing from an owner who has since moved. Wire payload/finite-value
audits cover the extra field.

## Rebuild and review

Run Blender on these scripts in order, with Unity closed:

1. tools/author_clean_kuro_rage.py
2. tools/rig_kuro_rage.py
3. tools/author_kuro.py

The combined persistent pet asset retains its GUID. Native sources are in
MapSource/characters/kuro, with the giant rig in its rage subdirectory. The final
export separates skinned material primitives into child meshes to avoid the
installed glTFast importer's whole-buffer bone-job conflict. Geometry, material
assignments, bind transforms and the authored bone clip are retained.

Then use the guarded Unity launch with KuroIdleClipAuthor.Build. It refreshes the
actual roster reference, extracts the giant clip and bakes all eleven idles.
KuroFormTests compares those clips to runtime poses and checks expression reset,
source-material isolation, actual baked ground contact and giant animation.
KuroIdleReviewProbe captures the real model, and KuroMapClearanceProbe measures
and captures it on all three maps. The three-process familiar matrix checks
controlled and following casts, including accepted yaw and rejoin.

## Verification record

Before the last small fixes: Core562/562; expression/form tests6/6; actual idle
capture1/1; actual three Nemu actions1/1. The full EditMode run found an expected
protocol-number update and an overlong summary; the summary was shortened to fit
the unchanged62-character limit. A reset probe used an invalid1.5m distance bound:
the normal authored shoulder offset already exceeds that. It now checks return
to the authored follow anchor; the fixture's pre-cast pet position could still
be trailing a recent owner teleport. Fresh full EditMode passes489/489, Nemu
complete-cycle PlayMode33/33, and all14 gating source audits pass. The corrected
all-map mesh probe passes1/1 with six positions: actual surface clearance0.035m,
maximum height5.263m, and Ilalim overhead clearance to the8m guideway. Its raw CSV
is Logs/kuro-final-verified/kuro-map-clearance.csv. BakeMesh scale compensation is
required for this nested imported rig; the prior false setting counted bone scale
twice and produced an invalid7.27m measurement, not an actual giant of that size.

Actual eleven-idle and three-action captures each pass1/1 at
Logs/kuro-staged-runtime-v3; the final lower floating eyes are visible in the
all-map captures at Logs/kuro-final-verified. Protocol28 has not yet received its
new three-process staged/yaw/rejoin run; tools/net_familiar_matrix.py and the
runtime diagnostic are prepared. The final18-person sheet also needs refreshing.
These are explicitly deferred non-map checks, not completed networking claims.

No human taste, listening or playtest approval is claimed. The wider all-hero,
alternative-loadout, animation, map, network and Windows release scope remains
open in ACTIVE_REWORK_LEDGER.md and TODO152.4.
