# Phaister hallucination refinement

Owner-authorized exception, October1 00:58UTC. Source inspection only so far;
no new runtime film or defect reproduction claimed. Other finalized Phaister
work and all Paete work remain protected. This plan joins the current full-roster
research priority without throwing away the other plans.

## What exists

Curse: Hex reaches for2s, marks, arms after10s, and lets the caster recast within
the mark lifetime. The victim sees phantom slippers for7.5s. Current presenter
copies real slipper meshes/materials into render-only objects, max four, alternates
forward placements with placements beside a real loose shoe, uses0.12s scale pops,
and deliberately casts no shadow. It does not add pickups, colliders or score.
Relevant source: Runtime/Visual/HexedPhantomSlippers.cs and the CurseHex section
of Runtime/Abilities/PhaisterHeroKit.cs. Existing PhaisterKitPlayProbe only includes
a coarse existence witness; it does not prove persuasive hallucination quality.

## Questions the baseline must answer

- Are the copies grounded, correctly oriented and plausible at real court scale?
  The fallback can copy a held shoe's look without its rest pose. Inspect rather
  than assert a defect from that possibility alone.
- Do phantoms appear in walls, off the playable ground or across occluders? Current
  placement samples ground height but does not visibly validate a full safe area.
- Does popping a miniature shoe into view announce the trick too obviously?
- Does selecting the first loose source repeatedly create a monotonous pattern?
- Can the victim still recognize the can, defender and actual retrieval prompts?
- Does the effect disappear on expiry, round exit, spectate, body replacement and
  scene teardown, and stay absent from another player's view?
- Is no-shadow a usable subtle tell on the shipped low-quality lighting setting?
  A tell absent on low settings cannot carry all the counterplay.

## Proposed direction to validate

Make the uncertainty about where the real slipper is, not a screen full of junk.
Prefer two or three convincing grounded alternatives with stable scale, a brief
occlusion-aware arrival outside the central reticle, and a clean dissolve at the
end. Do not invent fake gameplay prompts, fake score, fake teammate messages,
networked objects or phantom tag outcomes. Retain the finalized doll-to-victim
cause. A small local attention cue may suggest peripheral motion, but should not
imitate the real authoritative can/tag sound or hide a real warning.

Do not replace the no-shadow tell before comparing it in native footage. Candidate
alternative: very short frayed-edge departure when approached closely, so attentive
players can reject a phantom without requiring shadows or a colour distinction.
It must not become an obvious permanent purple hologram. Grounded world objects
are preferable to stickers pasted over the view. No camera shake/forced rotation,
rapid flashes, full-screen blur or compulsory loud whisper loop.

## One coherent implementation unit after baseline

Claim exact presenter and focused-test paths in the ledger before editing. Film a
real victim scenario with a real loose slipper, one held source, nearby cover and
low/high graphics, plus observer and round-cleanup controls. Fix placement/pose
and lifecycle first if demonstrated, then compare one refined timing treatment.
Use the current native Phaister rig and court; do not rebuild the entire kit or
replace its authored assets. Keep the cast/recast/cooldown and7.5s status unchanged
unless a separately recorded requirement makes that necessary.

Acceptance: convincing ground contact, bounded object count, no fake interactions,
no effect on observer cameras, no residue after expiry/round, no per-frame scene
search/material churn, and the real can/tag remains legible. Show actual before
and after frames from the victim view. Taste approval remains distinct from a
passing existence test. No runtime edits have been made in this research pass.

## Native baseline findings

The corrected human-seat baseline demonstrates a real view leak: changing the
camera to another body leaves the victim's phantom renderers visible. The victim
centre capture was inspected: copies use plausible real shoe appearance, but
fixed scale pops are an obvious presentation cue. Preserve the real materials
and no-shadow tell rather than replacing them with purple holograms.

The edge phase did not establish an out-of-bounds defect: Teleport calls
BeginSpawnSettle, which legitimately clears Hex. Its empty result is cleanup,
not failed placement. Do not quote it as an observed placement regression.
For final coverage, explicitly assert that cleanup, then apply a fresh Hex at
the edge. No more unchanged baseline runs. The already-proven view leak justifies
the scoped fix; bounded placement and full-size settling are authorized refinement.

## Scoped refinement result

Copies now keep their full authored size and settle a tiny distance onto measured
renderer ground contact, instead of scaling up from miniature shoes. Up to three
copies can coexist. Placement rejects out-of-court, obstructed, offscreen, large
height-jump and overlapping candidates; if there is no plausible location it
creates no copy. Real mesh/materials and the no-shadow tell remain. No invented
pickup, collider, fake HUD prompt or new sound was added.

The presenter tracks the active followed body, hides its copies when leaving the
victim view, resumes them when returning and ages them normally while hidden.
A new local victim replaces an old presenter. Hex duration and all Phaister
cast/mark/recast/ultimate mechanics and authored models are unchanged.

Final native graphics checks:2/2 pass in21.65s. They verify centre copies exist,
view switching hides/restores them, natural expiry removes the presenter/copies,
teleport clears the status as before, and a fresh outward-facing edge Hex rejects
all impossible candidates. The edge case has zero copies by design, not proof of
a visible edge placement. Baseline and final centre/edge captures are retained;
final centre and edge views were inspected.788frozen runtime/Core/test inputs have
no drift. OOM counters remain11/kill6. No additional camera/audio effect introduced.

These are focused native lifecycle/placement checks and still-image review, not
a new whole-player build, actual peer session, all-map qualification, normal-speed
film verdict or human taste approval. Material/skin assignments differ between
fresh test worlds, so the images are not a pixel-diff quality score. Broader
hallucination believability still benefits from representative player review.

The suite planner discovers this new fixture in the match group, but reports
unrelated pre-existing unassigned fixtures elsewhere. No aggregate-suite pass is
claimed and no unrelated fixture-registration cleanup was made.
