---
name: tump-trailer
description: Build and refine the original Bring It Home TUMP trailer with separate cinematic and gameplay blocks, silent picture and portable production state.
---

# TUMP trailer production

Read `docs/trailer/PRODUCTION_BRIEF.md`, `PRODUCTION_CHECKPOINT.md` and
`GAMEPLAY_SHOTS.md` first, then AGENTS.md. New owner instructions always win.
The older Lolo story and v1-v4 are rejected history, not the current assignment.

## Owner authorization and boundaries

On 2026-10-27 the owner authorized overnight production, prioritizing the new
cinematic opening, repeated review and iteration, then gameplay capture and the
finished silent two-minute trailer. Existing animation clips or new animation
are allowed. New cinematic performances, staging, lighting and cameras are
required. Do not reuse map/hero films or recorded gameplay as cinematic scenes.

- 0:00-0:20 new cinema; 0:20-1:28 native gameplay; 1:28-1:52 new cinema;
  1:52-2:00 TUMP identity and truthful final copy.
- The team authors ALL music/sound. No temp music, generated voices or SFX.
- Clean image: no taya rings, aim lines, nameplates, status badges or hit text.
- TUMP blocky cast, flat faces, one 16:9 frame and the supplied logo/accepted font.
- Cause before effect. Planted feet, prop contact, readable intent and geography.
- The story's immediate goal is slipper retrieval and escape, not an invented
  match win, permanent taya punishment or magical inherited object.

## Current tools

- `TrailerOpeningFilm.cs`: new arena scene and choreography, test assembly only.
- `TrailerProductionRecording.cs`: new original-cinema capture/probe.
- `TrailerCaptureMask.cs`: render-time suppression of gameplay overlays.
- `tools/run_trailer_job.py`: guarded Unity launch, source hashes, unique outputs
  and exact QualitySettings.asset restoration.

Start cheaply:
`python tools/run_trailer_job.py probe --tag opening-v01-probe`
Then a motion draft:
`python tools/run_trailer_job.py opening --tag opening-v01-draft --width 960 --height 540 --fps 30`
A final run uses `--width 1920 --height 1080 --fps 30` with a fresh tag.

Use one heavy Unity job per machine. Never edit Assets while Unity is active.
Keep the monitor asleep overnight. The owner's AC display timeout is 60 seconds;
the owner confirmed sleep resumed after the video preview closed. Do not use UI
playback, browser previews, window activation or input injection overnight. Use
background capture and extracted frame/sequence analysis with honest review limits.
Verify PID and command before stopping a job. Preserve existing dirty files,
profiles and source. No reset, clean or force push. No paid tools without approval.

## Review and continuity

Inspect actual frames and motion sequences, plus the complete assembled cut.
Contact sheets are framing evidence only; do not claim full playback review from
stills. Identify a concrete defect, fix it and recheck the changed boundaries.
Keep one current review candidate plus necessary failure/acceptance evidence.

Update PRODUCTION_CHECKPOINT after each coherent unit with source revision,
exact commands, running job identity, outputs, review limits and ONE next action.
Commit only owned paths as M4tyu633 and push coherent units so another PC can
continue. Final viewing file and concise manual handoff belong in chat, not Drive.
