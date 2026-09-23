# Title street weather, 2026-09-24

The owner's request: *"fix the improve the animation of the shadows, subtle particles, clouds and
leaves for our main menu with the click to start"*, researched against *"dynamic and subtle main
menu's ... like slay the spire 2"*, thought through *"before starting how to make it pretty"*.
The TODO entry is `docs/TODO_Backlog.md` § 153.23. Code: `Runtime/UI/OwnerMenuWind.cs`,
`OwnerMenuAir.cs` with `Resources/UI/OwnerMenuAir.shader`, `OwnerMenuLeaves.cs`, `OwnerRoadDust.cs`.

## 1 · Research: what Slay the Spire 2's menu actually does

Its menu is three Spine 4.2 rigs, `animations/backgrounds/mainmenu/{bottom,top,logo}`. The
timelines were decoded with `@esotericsoftware/spine-core` rather than guessed from a video:

| Layer | Motion | Numbers |
|---|---|---|
| cloud bank (`clouds` to `clouds9`) | each segment rocks about its own pivot, plus a mesh deform | 0.26 to 1.54 degrees, eased, 5.33 s cycle; deform up to 116 units on a 5,187 wide rig (2.2 %) |
| `cloud light pulse`, `small spire windows shine` | opacity pulse | 0 to 0.41 / 0.96, 4 s cycle |
| `Stars 1..3` | twinkle | alpha 1 to 0.55 in 0.1 to 0.3 s flickers, in bursts several seconds apart |
| `star shine` | slow pulse | 0 to 1, 4 s cycle |
| `city lights`, reflections | shimmer | 1 to 0.81 (0.61 reflected) every 0.17 s |
| boats 1 to 4 | bob, each with its own shadow and lights | 3.5 to 10.6 units on 2, 3.3 and 4 s |
| water texture | bob | 16 units over 3.4 s, then rest |
| logo | flame flipbook | 10 frames at 10 fps |

The whole thing is one 160 second loop. **Nothing large travels across the screen.** Its life is
many small motions in place, most of them light, each on its own period, and the small moving
objects carry their own shadows. That is the rule this pass follows.

## 2 · What was wrong with ours

Measured on `prototype/air.py`, a Python transcription of the shipped shader and meshes, with the
old design kept alongside for comparison (`frame(t, old=True)`).

- **Two winds.** Clouds and dust travelled right; leaves travelled left.
- **The shadow swam.** Her whole mask slid 46 px on a 74 s sine. A shadow cast by leaves stays
  where the tree is. The mirror at the border also folded a visible crease into the shade at the
  foot of the wall (23 px in at half resolution, see the before clip).
- **The sky was empty half the time.** Banks wrapped over 1920 px while her opening spans x 975 to
  1800 and is mostly canopy: empty 51 % of 25 simulated minutes, once for 205 s straight; her
  painting is 61 % cloud. The far bank floated at y -10 and showed the flat underside of her mass.
- **Leaves rained.** Five on one 11.4 s loop kept three or four in the air at every moment on
  parallel diagonals (`motion-heat-before.png`: ten seconds of motion), faded out in mid-air, and
  the quad mapped v = 0 to its top corner, so every leaf was drawn upside down.
- **Dust was a conveyor.** 78 grains and 11 puffs sliding in straight lines at 11 to 33 px/s,
  blind to the light and to any wind change.

## 3 · The design

- **One breeze** (`OwnerMenuWind`), right to left: off the canopy, across the road, towards the
  graffiti. A gust in every 31 s window, at a hashed moment inside it (rise 2.6 s, hold 1.6 s,
  fall 7 s), rustles the shadow, lifts the sand and carries leaves further.
- **Shadow**: anchored. Two slow warps (wavelengths 300 to 480 px) move neighbouring patches on
  their own, and a quicker small one shimmers the leaf edges: 1.5 px in the calm, 7 px in a gust.
  A cloud's shadow, her cloud at mip 5, crosses the whole street every 150 s at 30 px/s, about a
  tenth darker at its heart. Light is the motion StS2 leans on most, and this is ours.
- **Clouds**: his speeds (8.4 near, 4.4 far) and his 2 : 1 depth ratio, plus a middle bank at 6.3.
  Five instances of her one mass, wrapping just outside the opening behind the roof and the
  canopy, all resting on the skyline, churning like the StS2 rig (1.8 % billow on 11 s, two lobe
  warps). Never empty over a simulated hour: median cover 35 %, 90th percentile 55 %
  (`prototype/skycov.py`).
- **Leaves**: three slots on a 27 s cycle, each fall planned from a hash. 1.1 in the air on average,
  never more than 2, at least one always on screen. Pendulum flutter (40 to 100 px swings on 2.2
  to 2.9 s, fastest through the bottom, tilted like a pendulum bob), then they land, rest 3.5 to
  5.5 s and fade, casting their own silhouette as a shadow in sunlight. Every path is checked
  against the can, slipper, caption, bush and her painted road leaves before it starts
  (`leaf-paths-prototype.png`).
- **Dust**: 84 grains creeping at 5 to 18 px/s on an R2 low-discrepancy layout, lifting in gusts;
  7 wisps that rise, spread and dissolve; 26 motes that glint in short bursts, only where her
  street is in sun.

## 4 · Measurements

| | Before | After |
|---|---|---|
| changed px between frames 0.8 s apart (1920x1080) | ~87,000 | ~54,000 |
| frame-to-frame motion spikes (60 s) | 0 | 0 |
| sky opening empty | 51 %, longest 205 s | 0 %, over an hour |
| leaves in the air | 3 to 4, always | 1.1 average, max 2 |
| `DustCoversSand...` simulated, 120 windows, worst zone (needs > 30) | fails 2 of 120 | 57, fails 0 |

The in-engine results (the three title fixtures and a 30 s in-engine reel) are recorded in
§ 153.23.

## 5 · Re-running the prototype

```bash
cd docs/reports/title-weather-2026-09-24/prototype
python render.py new 0 60        # frames_new/ and new_0_60.mp4, half resolution
python render.py old 0 60        # the design before this pass
python audit.py new              # motion per frame and spikes
python dusttest.py new           # the dust fixture, simulated over 120 windows
python skycov.py                 # sky cover over 25 minutes
```

Needs numpy, scipy, opencv-python, pillow and ffmpeg. Everything in it is a pure function of time,
as the game is, so a number tuned here is the number the game runs.
