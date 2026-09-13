# Accessible roof pool, active implementation

This is an unfinished map feature on ASTRAReworks after dbd2344f. The owner
cancelled the handoff and requested continued work here. Maps remain first,
followed by full UI, then animations/skills/play feel. Do not call this a release.

## Owner contract

Larger accessible pool with no pool fence, real swimming and body/FPP/observer
animation. Full outer roof fencing; falls possible over any railing with existing
controls. Mash get-up and approximately10s unavailability for off-roof slippers.
Floating pool stock stays retrievable. Retain both modes, court and approved cast.

## Geometry v1 failure and v2 correction

The initial enlarged pool author compiled, saved the scene and baked80serialized
swimming clips for20rig hierarchies. Its geometry gate failed: the pool floor
appeared24.204m above the distant city street. Baseline saved in
Logs/sa-bubong-swim-v1-checks.txt and Logs/handoff-current-checks.log.

Source inspection found two coupled errors. The floor's underside was-1.8m but
the lower condo cap ended at-1.6m, penetrating the slab. The pool was also a
sibling of the combined building renderer. The check uses actual triangles for
a parent support but otherwise compares renderer bounds; the combined shell's
upper ring caused it to skip the lower cap and report the street below.

V2 lowers the cap to the actual basin underside, keeps the building foot fixed,
and makes the pool a child of its supporting shell. No support exemption or
checker tolerance was changed. The existing support triangle path now inspects
the real basin floor support. The regenerated scene passes all8checks:
Logs/sa-bubong-swim-v2-author.log and Logs/sa-bubong-swim-v2-checks.log.
Both runs restored the2existing files in the named Editor profile.

## Runtime work and remaining acceptance

RooftopPool, CharacterMotor and Slipper contain initial buoyancy, slower water
movement, existing jump and floating stock. SwimmingMotion and authored resource
clips feed CharacterAnimator and matching ViewmodelArms strokes. Actual pool
entry depth, water motion, held grip, interruption and FPP have not yet passed
behavior/visual review. Authoring and8scene checks do not prove those outcomes.

RooftopSwimmingProbe now drives ordinary movement down the steps, swims across
the basin, uses actual grab input on floating stock and exits via steps in both
modes. It records body/eye/velocity/clip traces and actual FPP captures. First
run is Logs/roof-swimming-v1.xml/.log. Record its actual result before acceptance.
RooftopRecoveryProbe's old closed-pool expectation is replaced with off-roof
loss/reset; the east test now walks against a fence then jumps it. All4edges and
new separate-process water/recovery coverage still need completion.

The old route probe excludes the pool and must be extended to water. The old
NetRoofProbe stages an x13 walk-only fall and must be updated. Earlier all-map
58pickups and old3process fall proof predate this owner-requested redesign.
Bird/cat/dog integration, all-map life/detail/quality/ordinary-play critique and
the broader map pass remain open. Current next steps live in EXECUTION_PLAN.md.

## V3 measured result and visual critique

Logs/roof-swimming-v3.xml is2/2, both modes. Entry drag removes the measured basin
impact: recorded minimum root y=-.960 and FPP eye y=.290 in both normal jump
entries, against v2 root-1.520 and eye-.270. Stair entry/exit, swimming pickup,
holding and return to dry motion pass. Profile2existing files restored. The
Classic motion sequence has152 paired owner/body frames at real recorded times;
the Hero sequence is separate in the same output directory.

Inspected Classic frames20/50 and corresponding owner50. Bodies retain their
style and the basin is physically open. The swimming pose remains too upright
to convincingly sell forward swimming. The carried world slipper also sits much
lower than the FPP held presentation. Water reads as a flat turquoise sheet and
has no localized contact wake. Those are visual acceptance failures to iterate,
not reasons to claim the whole feature finished because2tests pass. Improve
posture/grip and restrained water response, then recapture actual motion.

V1 swimming fixture failed because Grab was held before reaching range; source
confirmed pickup requires JustPressed. Only the fixture timing changed. V2 passed
1/1 for both modes: entry, stable treading, floating pickup, stair exit and settling
from a jump. The trace exposed a real fast-entry defect despite that pass: y=-1.52,
FPP eye=-.27, hitting the basin bottom before buoyancy. V3 adds immersed downward
speed damping and an assertion that normal jump entry stays above y=-1.16. It also
records8seconds of body+FPP swim/tread/holding/empty motion per mode. Current run
Logs/roof-swimming-v3.xml/.log; collect before edits. Editor session57557 at launch.

Current2026-09-13 continuation: v4swim author completed and profile restored.
Water-inclusive routev1 found197samples without grid connection, while independent
actual pool entry/exit already passed. The half-metre grid spans two .24m stair
rises; subdivision fixed the first connection, then the descending capsule cast
still counted the previous higher tread as a wall. v3 preserved body-grid.csv
showing connection stops between z0 andz.5. The test now uses intermediate support
and max endpoint support height for descending casts; actual pickup paths remain
required. No production map dimensions or physical tolerances changed for this.
Routev2 had a compile-only failure from missing Visual using in the new recovery
capture, fixed before v3. Core562/562 passed concurrently with verification.
Current Editor: recovery-motion-before-v1, actual fall/mash/get-up capture before
new animation. Existing approved fall camera switches to TPP to show the body,
then returns to FPP; preserve that behavior rather than inventing FPP while down.
Birdv2 and street-animalv1/v2 native studies authored while tests ran; not imported.
V1cat muzzle looked dog-like and tails too angular; v2 revisions still need visual
review. Sources in MapSource/environment/ambient-life, tools/author_street_animals.py.

## Stable source checkpoint scope

Current swimming v5passed2/2 with both modes, actual entry/exit and floating
pickup, and normal-speed paired body/owner captures. Revised forward swimming
extends feet behind the body and derives held-arm placement from the measured
palm. Float depth is.9m; FPP and the held slipper stay above the surface. Water
rendering/contact wakes and full-roster/ability/network review remain open.
Sa author repeatability is0changed/2049rows; all4maps pass the geometry gate.
Core562, fullEdit516(before final v5pose) and14gating source audits passed.
New owner policy now requires focused tests, not routine full suites. Respect it.
Portable selected XML/CSV/images and geometry/repeatability are in
roof-swimming-evidence. Exact runtime DLL/player validation is still outstanding.

Approved18people and purple Kuro retained. Partial Home/Play logo UI and original
PDF/JPG/Figma sources retained. Six cat/aspin models with4exported actions each,
plus bird studies, exist outside Assets and are NOT integrated/approved animals.
The larger map/UI/animation/skill/graphics/network goal is OPEN. No Desktop build
was replaced. Keep implementing after this checkpoint; no handoff is requested.
