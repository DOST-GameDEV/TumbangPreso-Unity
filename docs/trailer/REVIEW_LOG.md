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
- v07: first 1920x1080/30 fps candidate rendering at checkpoint. Review the changed
  ballistic contact/landing and cue frame before calling it the retained opening.

All versions are generated from new trailer-only direction. No old map/hero
films or native gameplay have been inserted into these cinematic takes.
