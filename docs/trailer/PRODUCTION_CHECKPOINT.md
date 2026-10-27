# Trailer production checkpoint

Branch `trailer/pandaigdig-plan`. Use current Git HEAD/status; coherent source
checkpoints include 64521f916, b9b88d1c0 and a5299625f. New changes are listed by
Git. Exact frozen input hashes and native results are in each take's job.json.
Read [accepted brief](PRODUCTION_BRIEF.md), [review log](REVIEW_LOG.md) and
[gameplay coverage](GAMEPLAY_SHOTS.md).

## Do not lose these constraints

- Background only overnight. The owner's AC display timeout is 60 seconds and he
  confirmed sleep resumed after the preview closed. NO browser/native playback,
  window activation, UI automation or input injection. No power settings changed.
- Read [live runner state](PRODUCTION_JOB.json), then verify ALL actual Unity.exe
  processes/commands. One Unity job; never edit Assets during a live run.
- Team authors ALL sound. Silent picture only. New cinema stays separate from
  gameplay: 0-20 cinema, 20-88 gameplay, 88-112 cinema, 112-120 end graphics.
- No paid service, new agents/chats, resets, clean or force push.
- Preserve owner dirt: SceneryTextureIdentities.asset, tump_logo.png.meta,
  cloud-bank-a.png.meta, GraphicsSettings.asset and ProjectSettings.asset.
- QualitySettings baseline SHA256:
  b12f8817b7355d4844c2a7cb0a057982de075131ae223850bc5b51244ad6b191.
  Every completed run restored exact bytes. Keep using run_trailer_job.py.

## Current picture and evidence

- Retained opening candidate: opening-v10-master, 600 frames, 1080p30, 1/1 native
  pass, silent MP4/decode passed. Held slipper visible and orientation continuous.
- Ending-v02-draft: 540 frames at 960x540, 1/1 pass. Pacing and pickup camera improved,
  but the actual hand never reaches the floor convincingly and the reaction's
  blue slipper is obscured. Needs a manually authored stoop/true contact and a
  lower cross-body display pose. Measure the grip error; do not just move the prop.
- Current job is neighborhood-v01-draft (check PRODUCTION_JOB.json). New original
  6-second street director is now in Assets. Its image has not been reviewed yet.
- Qualified native generation: classic-v03-world-master, 35 s, 1080p30, 1/1 pass.
  external_visibility_scope=true means body/carried-item shadow modes are restored
  only for external camera renders. The first-frame assertion passed. Masks now
  include status sprites, aim/ground reticles and the can-set ring. Need image review.
- OLD classic-v02 and hero-arena-v01 have valid FPP data and real replays, but their
  external cameras incorrectly inherited POV ShadowsOnly. Do not select those
  outer views. Prefer entirely new qualified takes to avoid residual marker risk.
- Remaining native coverage: qualified hero-arena (35 s), hero-street (45 s) and
  defender (30 s), same CLI kinds. These have not been captured with the new scope.
- Classic-v02 replay copied/verified: 58 files, 2,882,240 bytes under its take/replays.
  Hero-arena-v01 already has portable replay data. Original profile copies preserved.

## ONE next action

Finish and review neighborhood-v01-draft. Use
`python tools/trailer_review.py neighborhood-v01-draft`. Inspect actual running,
hand/slipper, framing and reactions. Then continue the accepted full production.
The opening is already reviewable; do not rerender it for ending-only changes.

## Commands and remaining delivery

`python tools/run_trailer_job.py KIND --tag FRESH-TAG --width 1920 --height 1080 --fps 30 --seconds N`
Kinds: probe, opening, ending, neighborhood, classic, hero-arena, hero-street, defender.
`python tools/trailer_review.py TAG --view fpv|line|side --encode` handles silent
video and review sheets. Omit --view for cinema. Drafts can use 960x540.

Need: convincing 18-second arena ending + 6-second neighborhood payoff; qualified
native selections; 8-second actual TUMP end card; complete 120-second silent edit;
precise source/frame sync sheet and portable source/media handoff. Actual logos
and PaalalabasDisplayWide.otf are in Resources/UI. bh_studios_logo.png appears blank;
do not place an empty asset. No release date, platform or store page may be invented.

Auto continuation: tump-trailer-overnight-continuation. Pause after complete final
picture/source/sync delivery. Keep this checkpoint short and push coherent units.
Media is Logs/trailer/bring-it-home. No Drive handoffs. v03-v05 obsolete draft raw
frames were pruned; retained their movies, sheets, XML and six critical frames each.
