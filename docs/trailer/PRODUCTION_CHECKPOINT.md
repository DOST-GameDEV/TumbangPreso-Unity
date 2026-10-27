# Trailer production checkpoint

Branch `trailer/pandaigdig-plan`. All final native/cinema recording and picture
assembly are complete. Read DELIVERY.md and PICTURE_EDL.json. No Unity job remains.
Production source is published through d0d851600. Final completion documentation
follows it; consult Git HEAD/status rather than guessing a source revision.

## Current finished picture

- Logs/trailer/bring-it-home/trailer-v01.mp4: 120.000 s, 3600 frames, 1080p30,
  H.264 BT.709 limited range and ZERO audio. Full decode passed. SHA256
  4ec35b24d3226c0191f8410cdfa3a9d1f4fd33594cbdfe55c688b0229ee5d1ad.
- trailer-v01_share.mp4: 720p review copy, same 3600 frames, zero audio, decode pass.
- Original cinema: opening-v12-master (600), ending-v12-master (540),
  neighborhood-v03-master (180). Each 1/1 native pass. No legacy map/hero film.
- Native sources: classic-v04-final (35 s), hero-street-v02-final (30 s),
  hero-arena-v03-final (20 s), defender-v02-final (22 s). All four views complete,
  each 1/1 pass, observer visibility scope and capture_mask_revision=2. Current
  source takes remain intact. All four partial-match replay manifests/sidecars
  verified. Selected encoded clips are portable visual source of truth.
- Checked 127 ordered decoded samples, dense throw/contact/pickup/escape/identity
  windows and both sides of cut boundaries. G12 revised to closer same-event side
  angle after assembled critique. Rechecked final images and regenerated cues.
- Team authors ALL audio. TRAILER_SYNC.csv has 161 frame/timecode cues plus sound
  brief/mux command in edits/trailer-v01. No temporary score, SFX or voice added.

## ONE next action

Owner watches the completed silent picture, then the team creates/mixes all sound.
No production job remains. Do not restart rendering without a requested picture
change. The overnight continuation heartbeat is PAUSED after successful delivery
verification. All source/tools and final edit decisions are published; consult HEAD.

Portable bundle: delivery/TUMP_BringItHome_trailer-v01.zip under the media root,
502129962 bytes, 257 files. OTIO roundtrip: 3600 frames, all relative media resolved,
file hashes and ZIP CRC passed. The supplied rebuild helper ran from a different
working directory and reproduced the ENTIRE master file byte for byte. Production
source commit d0d851600; final completion documentation follows it. See
DELIVERY_VERIFICATION.json and package PORTABILITY_CHECK.json for exact evidence.

## Preserve boundaries and owner work

- Background only overnight. Owner confirmed monitor sleeps after preview closed.
  No UI playback, browser previews, window activation or injected input. None were
  reopened. No display/power settings changed. Owner normal-speed viewing and
  creative acceptance remain separate from encoded/image evidence.
- 0-20 new cinema, 20-88 native gameplay, 88-112 new cinema, 112-120 actual TUMP
  graphics. Do not put gameplay or existing game cutscenes into cinematic blocks.
- No paid services, extra agents/chats, resets, clean or force push. No Drive upload.
- Keep owner dirt excluded: SceneryTextureIdentities.asset, tump_logo.png.meta,
  cloud-bank-a.png.meta, GraphicsSettings.asset and ProjectSettings.asset.
- QualitySettings restored exactly after every job. Current SHA256
  b12f8817b7355d4844c2a7cb0a057982de075131ae223850bc5b51244ad6b191.
- Source/picture changes require a fresh witness, cue sheet and refreshed bundle.
  Use tools/run_trailer_job.py, fresh tags, frozen source and ONE Unity process.

## Cleanup receipt

No task-owned preview/browser tab/server remains. Native/encode/review jobs are
terminal. Task-owned obsolete raw generations retired with kept critical frames,
movies, XML, logs, replay data and source hashes. cleanup-20261028.json reclaimed
3,626,986,904 bytes; cleanup-final-generation-20261028.json reclaimed
14,234,035,995 bytes. Earlier v03-v05 pruning is separately receipted. Preserve
current source takes, final media and pre-existing Claude animatics. No branch
was reset/cleaned/force-pushed or deleted; this unmerged trailer source is needed.
