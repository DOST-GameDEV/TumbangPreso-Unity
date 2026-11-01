# HOME screen films: how to edit them

Everything that makes the HOME films is in this folder and in git. Change a line, render, and the game has it. Nothing
here needs remaking from scratch, on any branch that has this folder.

| Hero | Film | Source | Notes |
|---|---|---|---|
| Zack | "Green Light", BGC at a warm late afternoon | `src/bgc/` | [zack-bgc.md](../../docs/reports/home-scene/zack-bgc.md) |
| Phaister | "Showtime", Ermita at night under the LRT | `src/ermita/` | [phaister-ermita.md](../../docs/reports/home-scene/phaister-ermita.md) |

Each hero has two clips on one clock: a 45 s film at 60 fps that plays once per run of the game, and a 24 s idle at
30 fps that loops. The idle's first frame continues the film's last, so the join cannot show.

## Set up once

You need Node (24 is what this was made with) and `ffmpeg` on your PATH. From this folder:

```
npm ci
npm run refs
```

`refs` copies the game's models, can and slipper from `Assets/` into `public/ref/`. Run it again if a model changes.
`node_modules/`, `public/ref/`, `out/` and `scratch/` are ignored by git on purpose: all of them can be rebuilt.

## The everyday loop

1. Edit the source (the table below says which file).
2. Look at frames, which takes seconds and needs no full render:
   ```
   node scripts/look.mjs PhaisterIntro try 300 1000 2699
   ```
   The sheet is `scratch/try_sheet.png`. Film frame numbers are at 60 fps, so a time in seconds times 60 is the frame.
   Use `ZackIntro`, `ZackIdle`, `PhaisterIntro` or `PhaisterIdle`. `npm run studio` scrubs in a browser.
3. Check it under the menu:
   ```
   node scripts/still1.mjs PhaisterIdle 0 scratch/hold.png
   node scripts/uiover.mjs scratch/hold.png scratch/ui.png
   ```
4. Render everything and put it in the game:
   ```
   node scripts/film.mjs phaister
   ```
   Rendering takes a while, and a rerun after an interruption carries on where it stopped. Add `--keep` to keep finished
   pieces; only do that when you know the edit does not touch them, or the old frames stay in.
5. Commit the source and `Assets/TumbangPreso/Resources/UI/home/<hero>-home-*`, then build with
   `python tools/build_windows.py --launch`.

To get a smaller copy to watch on a phone:

```
ffmpeg -i out/phaister_intro_1080p60.mp4 -vf scale=960:-2 -c:v libx264 -crf 24 out/phone.mp4
```

## Where to change what

The same files exist in `src/bgc/` and `src/ermita/`.

| To change | Edit | How |
|---|---|---|
| When anything happens | `beats.ts` | The one timetable, in seconds. Everything else reads these names, so moving a beat moves its camera, effects and acting |
| How someone moves | `perform.ts` | Each hero has a track of the model's own clips and keyed poses (`Z_TRACK`, `P_TRACK`). Add `{ at, fade, clip, rate, pose }`. Walking paths and facing are the `place` and `yaw` functions below it |
| A shot | `camera.ts` | `SHOTS` is the shot list: `at` the start, `in` the blend (0 is a cut), `p` the lens, `t` what it looks at, `f` the zoom |
| The frame the player lives with | `camera.ts`, `landing` | Keep the hero in the gap between the tiles and the mode card |
| Effects | `BgcScene.tsx`, `ErmitaScene.tsx` | Burst, rings, body arcs and Backtrack's after-images (`lib/bolt.tsx` draws the bolts); Phaister's sigils, rifts and the blast |
| The place | `set.tsx` | Buildings, lamps, trees, colours. The palette is the `C` block at the top |

`pose` values are degrees added to a bone: `root`, `torso`, `head`, `armL`, `armR`, `foreL`, `foreR`, `legL`, `legR`.
The clips each model has can be listed from its glb; the common ones are `idle`, `walk`, `sprint`, `slide`, `pick-up`,
`emote-yes`, `emote-no`, `holding-right-shoot`, `attack-melee-right`, `sit` and each hero's `hero-*`.

## Rules that came from the owner's reviews

- Use the game's models and their own clips; tweak them. Never redraw a face.
- Feet must look planted. `figure` takes its angle from the floor while the feet are in frame, or low lenses float people.
- Check a turn's keys. A key that goes from 372 to 12 is a slow full spin, not a small settle.
- In any shot about two people, keep both in frame. The taya reacts to the thrower and follows them.
- Blocks, not blobs: draw scenery from `box`, so it matches the cast and holds from every lens.
- After `landing` there are no cuts, and everything in the idle repeats exactly on its 24 s clock.
- Nothing important goes under a button, in any shot.
- Tagalog names are not used for skills, ghosts or statuses.

## Add another hero

1. Copy `src/ermita/` to `src/<place>/`, and change the models in its scene file. Add the new `*-redesign.glb` to `scripts/refs.mjs`, and to the model list in `useActor` in `src/three/actor.tsx`.
2. Register `<Hero>Intro` and `<Hero>Idle` in `src/Root.tsx`, as the others are.
3. Rewrite `beats.ts` first, then `camera.ts` starting from `landing`, then `perform.ts`.
4. Run `node scripts/film.mjs <hero>`, and add the hero to `HubSceneVideo.Heroes` if it is new.

The game plays `<hero>-home-intro` once, then `<hero>-home-loop`. A hero with only a loop just loops.

## Check it in the game

Run only the exact test, once (`AGENTS.md`). Each one also saves the film and hold under the real menu to
`Logs/shots-native-ui/`:

```
python tools/run_unity_guarded.py -batchmode -force-d3d11 -tp-profile presentation-validation-20260921 -runTests -testPlatform PlayMode -testFilter "TumbangPreso.PlayTests.HubSceneVideoTests.PhaistersLoopPlaysWhenPicked" -testResults Logs/home-video.xml -logFile Logs/home-video.log
```

Zack's are `HomePlaysTheAnimatedSceneAndHidesItUnderOtherScreens` and `ZacksFilmPlaysOnceThenHoldsTheIdle`. The long
background and history are in [HOME_SCREEN_ANIMATION_METHOD.md](../../docs/HOME_SCREEN_ANIMATION_METHOD.md).
