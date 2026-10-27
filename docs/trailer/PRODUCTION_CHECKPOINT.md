# Trailer production checkpoint

Updated 2026-10-27. Branch: `trailer/pandaigdig-plan`. Base: `a2220e32e`.
Scope and accepted boundaries: [PRODUCTION_BRIEF](PRODUCTION_BRIEF.md).

## Current unit

New original arena director, render-time cue mask, configurable recording fixture
and preservation runner are implemented. Render the first cheap probe to inspect
framing, anatomy, grip and timing. Compilation/render quality remain unverified.
Opening receives priority before gameplay capture and the full trailer assembly.

- No Unity process was running at intake.
- Current job: none. Do not infer a running job from an old log.
- Continuation automation: `tump-trailer-overnight-continuation`, hourly in this
  chat. Stop it once final picture/source/sync delivery is complete.
- Source owners: new trailer-only test code, recording/capture tools, trailer
  documents and the trailer skill. No gameplay balance/network changes.
- Existing owner dirt to preserve: SceneryTextureIdentities.asset, tump_logo.png.meta,
  cloud-bank-a.png.meta, GraphicsSettings.asset and ProjectSettings.asset.
- QualitySettings.asset was clean at intake; preserve exact pre-run bytes around
  each Unity job and restore after its process terminates.

## Exact next action

Run `python tools/run_trailer_job.py probe --tag opening-v01-probe` from the repo.
It captures 15 representative 960x540 frames. Inspect its fresh nonzero XML,
`Logs/trailer/bring-it-home/opening-v01-probe/job.json`, Unity log and images.
Fix concrete compile/framing defects, then render a 30 fps motion draft with:
`python tools/run_trailer_job.py opening --tag opening-v01-draft --width 960 --height 540 --fps 30`.
The arena director's final 6-second neighborhood slot is deliberately excluded
from the Ending fixture until a separate street scene is authored. Do not fill
that slot with the placeholder arena hold or old street footage.

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
