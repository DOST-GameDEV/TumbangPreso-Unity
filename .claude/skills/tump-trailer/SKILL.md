---
name: tump-trailer
description: Direct, render, edit and refine the TUMP game trailer (the PANDAIGDIG concept, "Lolo's Tsinelas" story) from real recorded gameplay, in-engine story cinematics and title cards. Use for any trailer, animatic, cinematic shot, trailer cut or trailer feedback work on TumbangPreso, on any machine.
---

# TUMP trailer

Owner brief, 2026-10-27: a two-minute trailer, cinematic, nostalgic and fun, mixing new
in-engine cinematics with real gameplay, set in a world where the Philippines made it and
tumbang preso is the world's biggest sport with a yearly world final, PANDAIGDIG. The owner
writes the music; Claude decides where voices go. "Refine, refine, refine"; "very well animated";
"thoroughly think about every aspect, don't just make some random trailer"; anyone on any PC with
their own Claude must be able to continue.

## Read first, in order

1. [docs/trailer/STORY.md](../../../docs/trailer/STORY.md): the story, beat sheet and energy curve.
   **Every shot must serve a beat there. If it does not, cut it.**
2. [docs/trailer/ANIMATIC.md](../../../docs/trailer/ANIMATIC.md): pipeline, iteration log (the checkpoint),
   owner notes, next step. Update the log every round.
3. [docs/trailer/TRAILER_PLAN.md](../../../docs/trailer/TRAILER_PLAN.md): the whole two minutes and the world.
4. The repo's AGENTS.md rules (one heavy Unity job per machine, named profile, author, no em dashes in repo prose).

## What the owner has rejected (do not repeat)

- A montage of unrelated vignettes with title cards doing the storytelling. Story first: a protagonist
  who wants something, an obstacle, a turn.
- No real gameplay. The game must be on screen, recorded from a real match, not only cutscenes.
- Borrowed footage (the map intro films, HUD visible). Everything is made for the trailer.
- Effect before cause: v2 cut to the stadium, craned out, THEN the can was hit. The hit lands on the cut;
  the reveal is the payoff after it.
- Energy that jumps around. Build in steps with planned valleys (stopdowns), climax, then a high exit.
- The Darumadrop One font. Cards use Paalalabas Display Wide.
- A "signature tsinelas rack" of single flat slippers that read as blobs. Props must read instantly.

## The pipeline (all reproducible from source; outputs are gitignored)

| Stage | Source | Command (one heavy Unity job at a time) | Output |
|---|---|---|---|
| Real gameplay | `Tests/PlayMode/Trailer/TrailerGameplay.cs` | `-testFilter "TumbangPreso.PlayTests.Trailer.TrailerGameplay.Street"` | `Logs/trailer/gameplay/<take>/{fpv,line,side}/f_#####.jpg` + `telemetry.tsv` |
| Story shots | `TrailerStreetFilm.cs`, `TrailerArenaFilm.cs`, `TrailerBody.cs` | `-testFilter "TumbangPreso.PlayTests.Trailer.TrailerRecording.Arena;TumbangPreso.PlayTests.Trailer.TrailerRecording.Street"` (Arena first: the TV shows its frame) | `Logs/trailer/frames/<film>/frame_####.png` + `shots.txt` |
| Edit | `tools/trailer_animatic.py` (EDL by beat id, looks, cards, sound cues) | `py -3 tools/trailer_animatic.py --version N` (`--sound-only` remixes audio) | `Logs/trailer/animatic_vN.mp4` (master) and `_share.mp4` (small, send this) |
| Review | `tools/trailer_sheet.py street` / `arena` | contact sheets per shot | `Logs/trailer/review/` |

Every Unity command is `python tools/run_unity_guarded.py -batchmode -force-d3d11 -tp-profile trailer-<date> -runTests -testPlatform PlayMode -testFilter "..." -testResults Logs/trailer/x.xml -logFile Logs/trailer/x.log`.
Needs: the Unity Editor in `ProjectSettings/ProjectVersion.txt` (graphics, never `-nographics`), Python 3 with
`numpy` and `Pillow`, `ffmpeg` on PATH. A 130 s gameplay take is about 25 minutes; story shots about 10.
Do not edit `Assets/` while Unity runs; draft in a scratch folder and copy in after.

**Gameplay:** bots play a real Classic match as the story's kids (seat 0 is Totoy, taya in round 1, attacking from
round 2 at about frame 2200). `fpv` is the spectator's real first-person view with the game's hands; `line` and
`side` are tracking operators that keep a clear sight line. Pick moments from `telemetry.tsv` (round, defender,
`can_up` 1 to 0 is a knock-down, `flying` > 0 is a throw), then put the frame numbers in the EDL.

## How a story shot is made

- Directors subclass `MapFilm`. Each shot owns a span (`Shots`); `Stage(age)` is a pure function of time.
- Cast with `Person(...)` wrapped in `TrailerBody`; drive with `Perform(actor, at, yaw, clip, time, pose)`.
- **Acting standard:** anticipation, committed peak, overshoot, settle (`Track`, `Ease.Back`, `Ease.Whip`); nobody
  frozen (`Alive`); crowds staggered; emotion in the body (flat faces). The shared throw (`ThrowTrack`, the game's
  charge/contact/follow) is the story's motif: 1974, 1996, Totoy and Rago throw the same way.
- Camera: purposeful move plus a little `Wobble`; one idea per shot; nothing blocking the subject.
- Light: trailer films override `OwnsLight` and `Tint`; match map films never do.
- Words in the world (sole, livery, plaque, TV lower third) are `TextMesh` in Paalalabas.

## Edit rules

- Aspect ratio carries meaning: the past is 4:3 (1974 newsreel, 1996 VHS), the present is full 16:9 (street story
  and real play), the dream opens to 2.39 on the Arena hit.
- Cut on action; match cut on shape and motion (tsinelas into lens, out of lens; the same throw pose).
- One title card per act at most; let the world's own signs and broadcasts speak.
- Voices: only human recordings in the project or recorded by the team. Never synthesize a voice.
- Temp music follows the energy curve; give the composer sync points from the printed segment starts.

## The refine loop

1. Read STORY.md and the log. Find the weakest beat for the STORY, not the easiest fix.
2. Change the director or the EDL, render, build contact sheets, judge every frame as a director.
3. Cut, watch the whole thing for flow and energy, write what changed and what is still weak.
4. Send `_share.mp4` with SendUserFile. Commit and push source and docs on `trailer/pandaigdig-plan`.
