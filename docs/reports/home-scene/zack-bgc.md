# HOME background scene: Zack at BGC, "Green Light"

Replaces the Sa Bubong loop: that map is disabled, the old loop predates his current kit (it showed
Magnet) and the owner asked for a film that settles instead of a loop. Source is
`ArtSource/home-scene/src/bgc/`, compositions `ZackIntro` (45 s at 60 fps, plays once) and `ZackIdle` (24 s, loops).
[Research and rules](research-2026-11-01.md). Method: `docs/HOME_SCREEN_ANIMATION_METHOD.md`.

## Why BGC

The owner: Sa Bubong is gone, so Zack's map has to change. BGC (scene id `Kanto`) is selectable on
main, sits next to his Pasig home, and its signals, lamps and power lines suit an electric hero. The
owner confirmed it on 2026-11-02, and `LORE.md`, `docs/CHARACTER_ORIGINS.md` and `character-stories.json`
now name BGC as his home court.

## The story

Rago is taya and stands square in front of the can. Zack waits for the traffic light. On green a curve
draws itself in the air, and he throws: the slipper bends in one huge electric arc round Rago and takes the
can from behind him. Rago's whole body goes. Zack sprints with Momentum and scoops the slipper. Rago rights
the can, creeps up and lunges for the tag; Backtrack snaps Zack home at the last instant, and Rago's foot
catches the can he just set up. He gets up, rights it again, raises one arm and chalks one up. He keeps the
score. Both stay on screen.

The owner's notes that shaped it, 2026-11-01: use the redesigned models; "we alr have animations", so build on
the models' own clips; "hindi shocked ung taya", shocked by the curved shot; communicate the curve and the
moment of impact; "he's abt to tag zack but he trips"; dynamic cameras and VFX when the moment warrants them.

| Moment | How it is made legible |
|---|---|
| The curve | It arcs in depth, so a low lens sees a flat line: the camera cranes up, the dashed path draws first, the ribbon and its shadow stay on the court |
| The impact | Cut low behind the can; 8 frame hit-stop: flash, ink frame with the burst, rings along the floor, sparks, shake; the can goes up slowly; lettered TUMP! |
| The shock | Crash zoom, hop, arms flung out, action lines, a "!" beside his head, sweat. The face is the model's and is never redrawn |
| The tag and the trip | Low side shot with the can between them, speed lines, Backtrack's flash and thin contours, the can kicked, dust, stars |

| Canon | Where | Used as |
|---|---|---|
| Zack finds the angle; makes the difficult look casual | `LORE.md`, `CHARACTER_ORIGINS.md` | The curve, and turning away from his own throw |
| "Zack calls him slow. Rago keeps the score" | `CHARACTER_ORIGINS.md` | The pairing, and the chalk stroke |
| Momentum's three stages, Backtrack's thin electric contour | `ZackHeroKit.cs`, `zack-kit-2026-10-25` | The run and the snap home |
| The taya rights the can before tagging; the thrower fetches the slipper | `docs/Design.md` | The whole second act |

Characters are `Art/CharacterRedesign/{zack,sean}/*-redesign.glb`, on a track of their own clips (`idle`,
`sprint`, `slide`, `pick-up`, `walk`, `emote-no`, `hero-zack-charge`) with keyed tweaks, crossfaded in
`layersAt`. Rago's shock, reach, trip and sprawl and Zack's throw are keyed.

`src/bgc/beats.ts` is the one timetable.

## Owner's review of v4, 2026-11-01

- "Looks like he is floating": a figure is one image at one angle, measured from the hips, so a low lens showed his soles over a floor seen from above. While the feet are in frame the angle now comes from the floor under them (`figure` in `BgcScene.tsx`).
- "More fluid": the film is 60 fps on the same timetable; each bone is a gaussian of its neighbouring frames, the head and hands trailing the hips (`sampledBones`); paths and turns are rounded. The idle stays 30 fps.
- "Why is Rago just staring": as in a match he now goes straight to the can, rights it, and turns with Zack from then on, shadowing him along his side of the can before the lunge.
- Backtrack is a 12 frame yank along a route across the frame with after-images burning off where he passed and a taut current ahead of him, not a teleport. It no longer retraces the run, which swung him into the lens.
- "What is dis blob": the park's trees are real boxes now, like the cast and the buildings.
- The stare and the run are shot so Rago stays in frame beside Zack. `HomePlaysTheAnimatedSceneAndHidesItUnderOtherScreens` captures the film and hold under the real board to `Logs/shots-native-ui/Hub-HomeScene-Film-*` and `-Hold`.

## The real map, 2026-11-01

The owner overrode the 2026-09-24 note about a drawn set: "USE THE ACTUAL 3D ASSET OF CURRENT MAPS WE HAVE", with
"EVERYTHING na makikita sa game" and "kita ren ung shit na nagalaw". `HomeFilmPlateRecorder` records Kanto through
`camAt` in Play mode, and `BgcScene` composites the cast and effects over those plates. The hour is therefore the map's
own clear morning, and the cast's light was retuned to match it. The shots of the signal now watch the map's real signal, which goes
from red straight to green: its amber comes after green, as in the game.

Backtrack follows the kit's own spec (`zack-kit-2026-10-25/plan.md`): a gold streak running back along his path, ghost
frames with the thin contour, and a landing ring that holds and breaks into four sparks. The star-burst on his body
went, because it read as him being hit. On the hold the current appears three times in 24 s and lights him and the court
(`light.ts`); the owner: "from time to time", "dont spam it".

## The hold

Worked backward from the frame the player lives with: long lens, Zack large in the gap between HOME's
tiles and its mode card, Rago hidden behind him. After the landing there are no
cuts. The idle is three passes of each hero's own 8 s `idle` clip, 720 frames, so it tiles exactly. The map's traffic and signals run on behind him.

## In the game

`HubSceneVideo` plays `zack-home-intro` once, holds its last frame while `zack-home-loop` (the idle) opens
on its own decoder, and swaps on the idle's first decoded frame. A later hub in the same run skips the
film. Heroes with no intro loop as before. `zack-home-poster` is the idle's frame 0, for reduced motion.

## Working on it

From `ArtSource/home-scene/`:

```
npm run refs
node scripts/plates.mjs                               # the real map, once; see the README
node scripts/look.mjs ZackIntro tag 100 200 300       # a contact sheet in scratch/
node scripts/still1.mjs ZackIdle 0 scratch/hold.png   # one full-resolution frame
node scripts/uiover.mjs scratch/hold.png scratch/ui.png   # HOME's real board art over it
node scripts/chunks.mjs ZackIntro out/zack_intro_1080p60.mp4 1 17 270 2 45
node scripts/chunks.mjs ZackIdle out/zack_idle_1080p30.mp4 1 17 270 2 45
node scripts/ship-zack.mjs
```

Delete `scratch/chunks_*` after changing the source, or a rerun resumes stale pieces.

## Choices the owner may want to change

- The film plays once per run of the game, per hero, rather than on every return to HOME.
- Phaister's loop is unchanged and still in the random pick, so Zack's film appears about half the time.
- TUMP! and the "!" use painted lettering (`phaister/lettering.tsx`); the old loop's lamp bounce idea was dropped for the curve.
