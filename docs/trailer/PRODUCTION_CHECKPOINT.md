# Trailer production checkpoint

Updated 2026-10-27. Branch `trailer/pandaigdig-plan`; latest pushed checkpoint
`b9b88d1c0`. New source described below remains dirty until the next commit.
[Accepted brief](PRODUCTION_BRIEF.md), [review history](REVIEW_LOG.md),
[reserved gameplay coverage](GAMEPLAY_SHOTS.md).

## Owner's newest constraint

Keep the monitor asleep overnight. Its AC timeout is still 60 seconds. The owner
confirmed sleep resumed after the local video preview was closed. NO UI playback,
browser previews, window activation or input injection. Continue background Unity
captures, frame/sequence analysis and file checks. Do not change power settings.
The one review tab and localhost server PID 29448 were closed/stopped. No task
preview remains. The hourly continuation automation carries this constraint too.

## Current unit and job

- Opening v09 grip probe: 1/1 pass, all 19 frames. Slipper is now clearly outside
  the fist during wind-up, and release orientation is continuous. Opening v10
  then rendered 600/600 frames at 1920x1080/30 and passed 1/1. Its silent encode
  runs/ran as tool session 51497; verify `opening-v10-master/review-report.json`.
- Current Unity job: `classic-v02-master`, guard PID 4856, tool session 17753 at
  launch. Verify actual process and its job.json. It captures 60 seconds at
  1920x1080/30 into fpv/line/side views. Do not edit Assets while Unity is active.
- Native `classic-v01-clean-probe`: 1/1 pass, 360 frames per view at 960x540/30,
  real-eye assertion passed. No knockdown in those 12 seconds. Remaining
  StatusBadge sprites were found and exact-name suppression added. The side
  camera was moved to positive X to avoid an awning. Verify both corrections.
  NEVER mask all children of StatusOverhead: it lives on the character body.
- The arena ending source was rewritten before the current native run. It adds
  a committed feint, grounded pickup/turn and a converging chase with a late reach.
  Its picture is UNREVIEWED. It needs the separate Ending capture next.
- Owner dirt to preserve: SceneryTextureIdentities.asset, tump_logo.png.meta,
  cloud-bank-a.png.meta, GraphicsSettings.asset and ProjectSettings.asset.
- QualitySettings started clean; every completed run restored exact bytes.
  Baseline SHA256 b12f8817b7355d4844c2a7cb0a057982de075131ae223850bc5b51244ad6b191.
- Cleanup: pruned 1782 obsolete raw JPEGs (440,038,285 bytes) from terminal v03,
  v04 and v05 drafts. Kept six critical images per take, full MP4 evidence,
  contact sheets, XML, logs and source. Those retired raw takes are incomplete
  intentionally; do not rerun the generic all-frame review against them.

## Exact next action

1. Finish Classic v02. Read fresh XML and inspect events/telemetry. Review native
   angles with `python tools/trailer_review.py classic-v02-master --view fpv --encode`
   and `--view line` / `--view side`. Check badges are gone and select a complete
   real loop. Do not fill the showcase with arbitrary movement if footage is weak.
2. Record Hero Strike: `python tools/run_trailer_job.py hero-arena --tag hero-arena-v01-master --width 1920 --height 1080 --fps 30 --seconds 75`.
   Other native kinds are `hero-street` and `defender`. Use events for accepted
   abilities, tags, throws and can hits; no state is puppeteered in this fixture.
3. Render `python tools/run_trailer_job.py ending --tag ending-v01-draft --width 960 --height 540 --fps 30`.
   It records 18 seconds of original arena cinema, source ages 20-38. Inspect
   the feint, real hand/slipper contact, turn, chase, late reach and boundary.
4. Author the separate 6-second neighborhood scene. It does not exist yet.
   Never use the excluded arena placeholder or an old map/hero film to fill it.
5. Assemble 20 s original opening + 68 s native gameplay + 24 s original ending
   + 8 s actual TUMP closing graphics. Team authors ALL sound. Produce exact
   frame-based sync notes. No date/platform/store availability may be invented.

## Resume and delivery

Read AGENTS and the accepted brief. Verify source/worktree/dirty boundaries and
any listed process. Use ONE Unity job, graphics and the named trailer profile.
Never edit Assets during a run. `tools/run_trailer_job.py` preserves quality and
writes job identity/input hashes. `tools/trailer_review.py --view fpv|line|side`
handles native camera review. Keep commits/pushes coherent and update this file.

Generated media is `Logs/trailer/bring-it-home/` (ignored). Keep source and exact
commands portable in Git; final viewing file and manual handoff belong in chat,
not Drive. The hourly automation is `tump-trailer-overnight-continuation`; pause
it when the complete silent picture, source and sync handoff are delivered.
No paid media service, new agent/chat or account reset is authorized.
