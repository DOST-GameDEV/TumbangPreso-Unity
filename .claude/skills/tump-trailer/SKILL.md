---
name: tump-trailer
description: Direct, render, edit and refine the TUMP game trailer (the PANDAIGDIG concept) from in-engine cinematics, gameplay and title cards. Use for any trailer, animatic, cinematic shot, trailer cut or trailer feedback work on TumbangPreso, on any machine.
---

# TUMP trailer

Owner brief, 2026-10-27: a two-minute trailer, cinematic, nostalgic and fun, mixing new
in-engine cinematics with real gameplay, set in a world where the Philippines made it and
tumbang preso is the world's biggest sport with a yearly world final, PANDAIGDIG. The owner
writes the music; Claude decides where voices go. The owner said: refine, refine, refine; it
must be very well animated; anyone on any PC with their own Claude must be able to continue.

## Read first

1. [docs/trailer/TRAILER_PLAN.md](../../../docs/trailer/TRAILER_PLAN.md): the concept, world, six acts, shot list, labels.
2. [docs/trailer/ANIMATIC.md](../../../docs/trailer/ANIMATIC.md): the pipeline, current version, iteration log,
   owner notes and the exact next step. **Its iteration log is the checkpoint. Update it every round.**
3. The repo's AGENTS.md rules still apply (one heavy Unity job per machine, named profile, author, no em dashes in repo prose).

## The pipeline (three stages, all reproducible from source)

| Stage | Source | Command | Output (gitignored) |
|---|---|---|---|
| Shots | `Assets/TumbangPreso/Tests/PlayMode/Trailer/*.cs` | `python tools/run_unity_guarded.py -batchmode -force-d3d11 -tp-profile trailer-<date> -runTests -testPlatform PlayMode -testFilter "TumbangPreso.PlayTests.Trailer.TrailerRecording.Street;TumbangPreso.PlayTests.Trailer.TrailerRecording.Arena" -testResults Logs/trailer/r.xml -logFile Logs/trailer/r.log` | `Logs/trailer/frames/<film>/frame_####.png` + `shots.txt` |
| Edit | `tools/trailer_animatic.py` (EDL + sound cues) | `py -3 tools/trailer_animatic.py --version N` (`--sound-only` remixes audio fast) | `Logs/trailer/animatic_vN.mp4`, `_sheet.png` |
| Review | contact sheets you build from frames | look at every shot as a director before showing the owner | notes in ANIMATIC.md |

Needs: the Unity Editor version in `ProjectSettings/ProjectVersion.txt` (with graphics, never
`-nographics`), Python 3 with `numpy` and `Pillow`, `ffmpeg` on PATH. Everything else is in the repo.
Rendering the street film takes about 5 minutes; do not edit `Assets/` while Unity runs.

## How a shot is made

- Directors subclass `MapFilm` (Runtime/Map). Each film owns a clock; each shot owns a span
  (`Shots` table); `Stage(age)` poses everything as a pure function of time. Nothing ticks.
- Cast: `Person(...)` builds a roster rig; wrap it in `TrailerBody` and drive it with
  `Perform(actor, at, yaw, clip, time, pose)`: the clip underneath (idle, walk, sit, emote-yes,
  crouch, jump...) plus a keyed `Pose` on top.
- **Acting standard (non-negotiable):** every action has anticipation, a committed peak, overshoot
  and settle (`Track` keys with `Ease.Back` / `Ease.Whip`); nobody is ever a statue (`Alive`);
  crowds are staggered, never in unison; emotion reads in the body from the wide shot (flat faces).
  The throw uses the game's own charge/contact/follow shapes (`Pose.Charge/Contact/Follow`).
- Camera: `Shoot(eye, aim, fov, roll)`; always a purposeful move plus a little `Wobble`; one idea
  per shot; nothing blocking the subject; never end a move inside a body.
- Light: trailer films override `OwnsLight` and call `Tint(mood, 1)`. Match map films never do.
- Props: `Prop` (toon skin, never plain URP/Lit), `Ghost` for drawn-only copies, slippers via
  `RosterBook.SlipperArt(0)`. The four match seats are hidden in trailer films.

## Edit rules

- Aspect ratio tells the story: street and past in 4:3, stadium widens to 2.39, gameplay 16:9.
- Cards: Darumadrop One, warm ink background (never white or pale), short lines, slam in with overshoot.
- Placeholders must be labelled on screen until replaced.
- Voices: only human recordings already in the project or recorded by the team. Never synthesize a voice.
- Music in the animatic is TEMP guide only; the team supplies the score. Give the composer sync points.

## The refine loop

1. Read the iteration log and owner notes in ANIMATIC.md. Pick the weakest shot, not the easiest.
2. Change the director, render, then build a contact sheet of 6 to 10 frames per changed shot and
   look at them as a director: readable emotion, clear silhouette, motion arcs, framing, light.
3. Cut a new version, watch the timing, write what changed and what is still weak in the log.
4. Send the owner the video (SendUserFile), not just a description. Commit and push the source and
   doc changes on the trailer branch so the next person starts from them.
