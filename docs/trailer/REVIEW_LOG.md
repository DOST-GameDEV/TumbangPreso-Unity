# Bring It Home: production review

## 2026-10-27

- v01 probe: fixture compile failure from a missing TestTools import. No images.
  Corrected. Guard restored QualitySettings. Retain failure log as evidence.
- v02 probe: 15/15 representative frames, 1/1 native test. The new deck was
  coplanar with the live deck, causing triangular breakup; another competitor
  blocked the throwing view. Raised the set by .045 m and restaged competitors.
- v03 motion: all 600 frames rendered, Opening case passed. Overall XML failed
  because the substring filter Opening ALSO selected OpeningProbe, which safely
  refused to overwrite the completed take. Renamed the motion case OpeningMotion.
  Frame-sequence review found the hit too late and the final problem too distant.
  Silent MP4 has no audio stream and passes full-file decode. Not a final candidate.
- v04 motion: 600/600 frames, fresh 1/1 pass, QualitySettings restored. First hit
  moved to 4.45 s. Added a dedicated contact shot, earlier reveal, defender shot
  and close Rago decision. Floor breakup and foreground competitor obstruction
  were resolved in sampled frames. Dante's pickup doubled the native bend with
  excessive extra lean, bringing the can toward his face. Lowered the overlay
  lean and lift target for the next version. Actual full playback review remains
  separate from these sheet/critical-frame observations.
- v05: 1/1 pass and all 600 frames, lower pickup bending and quieter court material.
  Log inspection caught a malformed new shader GUID, so focus had passed through
  without loading. Fixed the new GUID, added an explicit shader-readiness assertion
  and a pre-launch GUID check. No existing asset GUID was changed.
- v06: 1/1 pass, all 600 frames, focus readiness true, depth-aware focus visible
  in first-object and last-character frames. Added 4x target MSAA. Silent encode
  and complete decode passed. Sampled normal-speed browser playback confirmed
  the movie plays to 20.000 s; this was not exhaustive audiovisual review. Contact
  settling still appeared floaty, so replaced eased prop travel with ballistic
  arcs and short damped ground bounces. Closed the review tab and verified/stopped
  its exact localhost server PID 29448.
- v07: first 1920x1080/30 fps candidate, 1/1 native pass, 600 frames, silent encode
  and full decode pass. Faster ballistic prop settling replaced the floaty eased
  motion. Further still review motivated measuring actual prop support and actor
  geometry rather than guessing floor offsets.
- v08: 1920x1080/30, 1/1 pass, 600 frames, zero-audio encode and full decode pass.
  Actor grounding calibrates the bake scale convention; can/shoe rest height is
  measured from the source geometry. The wind-up frame exposed the next issue:
  the slipper was hidden by the large fist silhouette. v09 changes heel grip,
  toe direction and release orientation continuity and probes the wind-up frames.
- Classic clean probe: 1/1 pass, valid native-eye data and 360 frames per camera.
  No can knockdown in this short take. StatusBadge sprites survived the first mask;
  added exact renderer-name suppression and moved the side camera away from the
  awning. New footage must verify these corrections before selecting showcase clips.
- Owner monitor issue: the AC display timeout remained 60 seconds. Closed preview
  tab/server and stopped UI playback checks. Owner confirmed the monitor again
  turns off and stays off. Continue background-only; no power setting was changed.
- Native camera audit: the original external cameras inherited the spectator's
  POV ShadowsOnly body/carried-item hide, so their watched actor could disappear.
  Added a test-only render scope restoring the observer's cached original modes
  only around external draws, then restoring POV state. Added an initial native
  assertion and external_visibility_scope metadata. classic-v03-world-master
  passed 1/1 at 1080p30 for 35 seconds with this correction. Earlier external views
  are retired from selection. Also masked GroundReticle and the can-set ring.
- Hero-arena-v01: 1/1 pass, 75 seconds, accepted casts and actual event telemetry.
  Portable real-match replay flushed to the take folder. Its original outer
  cameras predate the visibility correction; keep as reference, not final coverage.
- Classic-v02: verified/copied all 58 manifest-declared replay files (2,882,240
  bytes) to its take folder with SHA checks and capture-time/map/mode matching.
  Source named-profile replays were preserved.
- Ending-v01: 1/1 render pass but long idle and an occluded pickup were weak.
  v02 retimed the action, added a grip close view, widened chase separation and
  added a quiet released-object reaction. v02 passed 1/1, but the still sequence
  exposed an unconvincing upright pickup and obscured carried shoe. Own the new
  stoop/reach and display pose before a full-resolution ending render.

All versions are generated from new trailer-only direction. No old map/hero
films or native gameplay have been inserted into these cinematic takes.

## Stable set and real hand geometry, October 28

Arena source now freezes the live Stage and uses one level cinematic deck; bowl
and audience remain native art. Real weighted fingertips replace the short arm
proxy. Slipper grip measures the long X axis and root pivot; flight follows its
center. New opening-v11-stage-probe passed 1/1 and its 19 samples show held prop,
contact, grounded can restore and consistent set. neighborhood-v02-draft passed
1/1 with 180 frames; the foreground obstruction and stair staging are corrected.
Ordered half-second frames show grounded feet and the blue slipper at the lead
hand. Reaction pose lowered for the next ending master. These are image/sequence
findings, not normal-speed owner playback acceptance. All sound remains absent.

Classic-v03 and hero-street-v01 provide qualified native matches and exact event
records. Avoid long POV selections looking away from the contest or external
frames obstructed by architecture. Select actual action after capture.

## Clean final generation, October 28

ending-v11-side-probe passed 1/1 with a reachable shoulder-relative hand target
and camera on that side. Reaction grip error .016-.020 m; recovered blue slipper
is clearly visible. ending-v12-master passed 1/1 with 540 true 1080p30 frames.
Opening-v12 (600) and neighborhood-v03 (180) remain the other final cinema inputs.

Capture mask revision 2 suppresses can protection/down indicator renderers. A
fourth observer follow view tracks the actual watched body without changing
outcomes. classic-v04-final passed 1/1, all four views 1050 frames, real portable
replay tail flushed. No runtime Exception/error CS matches in checked final
cinema/Classic logs. Capture evidence is not a whole-game regression claim.

Classic selections show two actual exchanges: source20-27 includes release638,
canDown660, pickup796 and tag804; source8.8-15.3 includes canDown283, pickup346
and escape425. They are separate plays, not one invented continuous outcome.
Short real-eye setup/release cuts switch to same-event observer views before a
bot looks away from impact. Native screenshot evidence reviewed from extracted
files. No UI player/browser was opened and no sound was generated.

## Complete assembled picture

Final master SHA256 4ec35b24d3226c0191f8410cdfa3a9d1f4fd33594cbdfe55c688b0229ee5d1ad. Exactly 120 seconds / 3600 frames, 1080p30, no audio.
Both master/review copy fully decode with exact counts. Checked 127 ordered final
frames, contact/grip/chase/identity windows and cut-boundary pairs. G12's line
angle was too wide; replaced with closer same-event side view without retiming,
then regenerated the movie, witness, review frames and 161 sync cues. Pickup/lift
and recovered prop remain legible. Native local bot matches and partial replay
archives are accurately identified; no human playtest or full-match result claimed.
No UI playback was reopened. Normal-speed owner review remains pending.

## Goal continuation: reaction camera refinement

A denser C14 review exposed an unintended final-second pullback/orbit. The shot
side was recomputed every frame from a shoulder moved by the native idle clip.
The camera now uses a side chosen once from the actual rest rig in Prepare.
Character performance and all cut timing stay unchanged. Capture telemetry now
records camera position and angles, so a steady shot can be checked directly.
ending-v13-camera-probe passed 1/1 with17 samples and normal exit. Its three C14
camera positions/rotations are identical; grip error .01549-.01969m and the
recovered blue slipper remains visible. Render the full revised ending next.
The delivered v01 picture/bundle stay intact while v02 is validated.

## Opening footwork and framing refinement

Full-duration half-second review showed the defender crowding the can while
moving to his blocking mark. Added a path around the prop, a shared guard mark
for opening/ending continuity and matching curved lunge/recovery paths. The
C06 eye/target now retain the head and show the can beside the feet clearly.

Foot clearance uses actual bone-world times bind-pose skinning matrices and
separate lower-foot bounds against the visible can bounds. The first v15/v16
foot diagnostics were scaled twice and are superseded measurement evidence;
their render passes are not foot-clearance acceptance. Corrected v17 probe
passed 1/1. Same-pose counterfactual old approach reaches -.13436m signed bound
clearance; new sampled approach stays at least .08621m clear. Sparse samples
are not whole-motion proof: render full takes and inspect every traced frame.

## v02 full-motion acceptance

opening-v18-final-master passed 1/1 with 600 true 1080p30 frames. Every frame of
its guard move/hold has measured lower-foot clearance above .05m, minimum .08621m.
The ending's intermediate v18/v19 render tests passed, but their geometry traces
showed a single close pass during recovery/lunge and were not accepted. Added
an eased 1.25 m turn radius during the lunge, preserving continuous entry.
ending-v20-final-master passed 1/1 with 540 frames; every frame has measured feet
and at least .15587m can clearance. All 120 reaction frames keep camera position/
rotation fixed, slipper inside the frame and grip error below .01969m.

v02's master/share fully decode at exactly 3600 frames / 120 seconds with no audio.
Inspected 311 ordered decoded samples: full-duration 2 fps sweep plus contacts and
cut boundaries. All 20 sweep sheets reviewed; changed opening and ending inspected
again after final encoding. All cut times and 161-row cue CSV match v01 exactly;
all gameplay, neighborhood and title encoded inputs are byte-identical. Current
picture SHA is in the encoded witness. Owner normal-speed playback not claimed.
