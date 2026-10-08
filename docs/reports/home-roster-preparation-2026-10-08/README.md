# Prepare the current roster before Home is ready

The new login/main-menu startup route loaded the roster references but omitted
the outline and authored motion preparation from the retired splash. Its actual
`PreloadHomeAssets` coroutine could report ready while readable body, alternate
arm, pet, can and slipper meshes still required their first outline bake.

The cold-cache native regression reproduces this failure on the original route.
The corrected route uses the existing yielded outline and generated-motion caches
before readiness. It does not instantiate actors or animation graphs, change
authored materials or reopen the illustrated startup loading screen. The existing
five-second minimum and completion gate remain authoritative. Progress includes
the added work. Opt-in timing reports resource wait and visual preparation as
separate coroutine wall durations rather than combining them under an async label.

The first candidate additionally exposed a missing-data defect: Paete's current
rig had no baked RootedAnimations set. The targeted authoring pass found the same
missing binding-key sets on all nine current hero rigs. These are serialized
struggle, plant heave, breakout and feared-flee clips built by the existing shared
authoring code. A player cannot generate the necessary curves itself. The new
`RootedAnimationAuthor.ExecuteMissing`/`RunMissing` entry points repair absent
sets without regenerating existing authored motion. All 23 existing asset files
retain their exact bytes; nine missing sets were added. Existing bake entry points
keep their full-bake behavior. Mechanics, source models, kit definitions, rig
references, old-map art and authored cinematic direction are unchanged.

Evidence is deliberately scoped:

- Original PID68016: one actual cold-preload failure, preserved in `original`.
- First candidate PID48600: two passes and two missing-rooted-data failures,
  preserved in `candidate`. The unchanged generated-motion control is retained.
- Authoring PID52720: terminal exit0 with nine targeted additions and exact
  preservation of the 23 existing sets. This is an authoring receipt, not a test.
- Candidate2 PID70708: all five native controls pass, including the original
  preload invariant, unchanged motion/lifetime controls and actual startup.
- Final opt-in timing run: the same five controls exercise the corrected
  diagnostic stage boundaries. Exact final PID and observations are in
  [evidence.json](evidence.json); final source and generated set hashes are included.

The controls prepare 41 actual models, 145 readable meshes and 84 retained motion
sets before readiness. They verify no actor/animation-graph creation, unchanged
source materials and no cold outline work remaining. Native clip sampling drives
each current hero hierarchy through all four new serialized clips: 36 non-bind
poses across nine rigs. The startup check observes BH Studios, login, visible
main-menu preparation for at least five seconds and first Home playback advancing
from frame4 to40. Fresh 1080p/720p UI captures are retained in `startup`.
Editor startup does not render the built player's Unity splash; that configured
player stage retains its previous package qualification.

Original and initial candidate runs restore all 21,427 frozen inputs; final runs
include the nine new sets and metadata and restore all 21,445. Isolated profiles,
shared input/editor preferences and private source work are preserved. Owned
native players, import workers and preservation helpers are terminal.

No new player build or Desktop replacement was made for this unit. Current
Desktop remains ordinary001d/runtime2005. Updated Windows first-use/frame-pacing,
natural status/carry/replay behavior, all-map/kit AI efficacy, creative direction
and actual peer/human acceptance remain open. Five native controls and serialized
pose samples do not establish tournament readiness or fix the measured loading
and Hero-menu wall-clock stalls.
