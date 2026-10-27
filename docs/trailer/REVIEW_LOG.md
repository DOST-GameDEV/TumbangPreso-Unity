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
