# Trailer production checkpoint

Branch `trailer/pandaigdig-plan`. All final native/cinema recording and picture
assembly are complete. Read DELIVERY.md and PICTURE_EDL.json. No Unity job remains.
Current source is published through ceb59323c plus the final source commit being
prepared; consult Git HEAD/status rather than guessing a source revision.

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

Run `python tools/trailer_handoff.py trailer-v01` after publishing final source.
It creates delivery/TUMP_BringItHome_trailer-v01 folder and ZIP beneath the media
root, validates relative OTIO media and 3600-frame timeline, runs the supplied
picture-rebuild helper from a different working directory, compares video streams,
checks copied file hashes and tests ZIP CRCs. Use PORTABILITY_CHECK.json and the
successful command result to establish completion. If these already exist and
are verified, do not build/render again: owner playback and team sound are next.
Then pause heartbeat tump-trailer-overnight-continuation and deliver final links.

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
