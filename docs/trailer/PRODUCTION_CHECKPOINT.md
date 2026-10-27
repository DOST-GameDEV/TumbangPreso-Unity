# Trailer production checkpoint

Updated 2026-10-27. Branch: `trailer/pandaigdig-plan`. Initial checkpoint commit:
`64521f916` (pushed). Further source revisions below are in the working tree until
the next coherent review checkpoint is committed. Per-run input hashes live in job.json.
Scope and accepted boundaries: [PRODUCTION_BRIEF](PRODUCTION_BRIEF.md).

## Current unit

Iterating the 20-second new original arena opening. Opening v04 rendered all 600
frames at 960x540/30 fps and passed 1/1 native case. Sequence/still review fixed
floor z-fighting, foreground occlusion and a late first impact. Contact is now
4.45 seconds. New final close-up gives Rago a decision beat. Full motion/aesthetic
acceptance is not yet claimed. See [REVIEW_LOG](REVIEW_LOG.md).
Opening receives priority before gameplay capture and the full trailer assembly.

- No Unity process was running at intake.
- Current job: `opening-v07-master`, guard PID 23216, tool session 58190 at launch.
  Verify the actual process and `Logs/trailer/bring-it-home/opening-v07-master/job.json`.
  Do not edit Assets until its Unity process is terminal. This is the first
  1920x1080/30 fps candidate, with faster ballistic can/slipper settling after
  contact. Do not duplicate the run if still active.
- v06 draft passed 1/1 and produced 600/600 frames. Focus shader now loads and
  visibly separates foreground/background. The silent MP4 passes complete decode.
  Normal-speed local browser playback was sampled, not exhaustively watched.
  The one review tab and localhost server PID 29448 were closed/stopped after
  verifying ownership. No browser/server remains from that review.
- opening-v01-probe stopped at compile because the new fixture omitted the
  UnityEngine.TestTools import. Corrected; no frames were produced. Quality file
  restoration is recorded in its job.json. No visual acceptance is claimed.
- Continuation automation: `tump-trailer-overnight-continuation`, hourly in this
  chat. Stop it once final picture/source/sync delivery is complete.
- Source owners: new trailer-only test code, recording/capture tools, trailer
  documents and the trailer skill. No gameplay balance/network changes.
- Existing owner dirt to preserve: SceneryTextureIdentities.asset, tump_logo.png.meta,
  cloud-bank-a.png.meta, GraphicsSettings.asset and ProjectSettings.asset.
- QualitySettings.asset was clean at intake; preserve exact pre-run bytes around
  each Unity job and restore after its process terminates.

## Exact next action

Inspect v07 after it terminates. Fresh XML must show 1 expected nonzero test.
Run `python tools/trailer_review.py opening-v07-master --encode` to create a silent
MP4, full decode check, statistics and a time-labelled review sheet. Compare the
pickup, can contact and settling against v06 at actual frames. If a defect remains,
fix its cause and use a fresh v08 tag. Current review movie is
`Logs/trailer/bring-it-home/opening-v06-draft.mp4`; it has zero audio streams.

After the opening's motion/framing are strong, record a 1920x1080/30 fps version,
then implement clean native coverage from GAMEPLAY_SHOTS. The existing
TrailerGameplay only captures Classic and still needs the new mask and configurable
Hero Strike coverage. Do not reuse the old v4 edit or insert any native footage
into the opening. Keep the last new cinematic ending separate from gameplay.
The arena director's final 6-second neighborhood slot is deliberately excluded
from the Ending fixture until a separate street scene is authored. Do not fill
that slot with the placeholder arena hold or old street footage. Ending action
is still an unreviewed draft and needs its own motion/contact work.

## Resume on any machine

1. Fetch and inspect branch/worktrees/dirt. Read AGENTS and PRODUCTION_BRIEF.
2. Read this checkpoint and verify the listed job against the actual PID/command.
3. Use Unity from ProjectVersion, Python with Pillow/numpy and ffmpeg/ffprobe.
4. Use the guarded runner, graphics, a named trailer profile and unique Logs output.
5. Preserve/restore QualitySettings.asset bytes; no reset/clean/force push.
6. Update this checkpoint with exact commands, source commit, outputs, review
   findings and the next action. Commit only owned paths and push coherent units.

Generated media belongs under `Logs/trailer/bring-it-home/`. Do not upload source,
QA logs or handoffs to Drive. Keep the final review movie and honest limitations
in chat. This document is live execution state, not a second task queue.
