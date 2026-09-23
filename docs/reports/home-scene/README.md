# HOME background scene: Zack at Sa Bubong

The animated scene behind the HOME hub (`docs/TODO.md` UX-1.1 reserves "a clean full-bleed
animated-scene layer" for it). Source is `ArtSource/home-scene/`, a Remotion project that
renders one seamless 1920x1080, 30 fps, 42 s loop. The reusable method behind it is
`docs/HOME_SCREEN_ANIMATION_METHOD.md`. This file is the research, the design that came
out of it, and the numbers the HOME lane needs to put it on screen.

## 1 · What the owner supplied, and what it is not

| Source | What it is | How it is used |
|---|---|---|
| `Untitled19.mov` (owner, 2026-09-23, 3.1 s, 34 drawings) | Rough storyboard: calm face with wind in the hair, eyes snap open, a black frame with glowing eyes, a yellow flash, powered-up face with sparks, the hand on the slipper, the arm aimed at a far lata, the hit, **TUMP!** | The spine of the hero moment. Every one of its beats is in the loop, in its order. He called it about 20 per cent of the finished piece. |
| HOME wireframe (the attached image, same as `ArtSource/front-end-flow-20260923/home-final-wireframe.png`) | The hub's layout | Sets the UI zones the scene must keep quiet under. |
| `zip-sketches/20.png`, the earlier HOME variant | Its background is labelled **"video(?)"**: a big over-the-shoulder thrower in the foreground, a small figure by the can down the court, a sun top right | The reverse shot of the throw is this composition. |
| Her TUMP logo, her `slipper with hit` drawing, her login buttons | **The only hand-drawn art in the front end.** 🧑 2026-09-23: the title-screen street painting is AI generated and is not a reference, nor is any other art in the repo | The whole visual language, section 3. |
| `LORE.md` and `ASTRA.md` on Zack | Sa Bubong is his condo roofdeck in Pasig (residents' court, shade, laundry, the city beyond). He is bladed and side-on, pauses before the snap, then chatters instead of settling. Narrow branching electricity. Magnet's readable moment is the tsinelas ARRIVING in his hand | Setting, acting and effects. |
| `UiTheme.HeroElectric` `e8f53a` / `HeroElectricBright` `f6ffa0`, `ViewmodelArms.SkinZack` `c77a45` | His colours in the shipping game | His electricity and skin match the game exactly. |

## 2 · Research: what makes a home screen good, and what TUMP takes from each

Looked at directly: Valorant's home screens from Episode 1 to V25 (Yoru, Neon, Gekko, Clove,
Vyse among them), plus Brawl Stars, Fortnite, Apex, Hades, Street Fighter 6, Hi-Fi Rush and
the owner's own Slay the Spire 2 note (`docs/TODO.md`, the clouds pass: *"subtle like slay the
spire 2's main menu"*).

1. **One featured hero, in their own world, per season.** Every Valorant act's home screen is
   the act's new agent in their place (Clove, Vyse, Tejo, Waylay), not a menu wallpaper.
   **TUMP: Zack at his own home court, Sa Bubong.** The next season is another hero at their
   court, and the title street stays the title's.
2. **The power is shown small and contained.** Neon, Valorant's electric agent, is a wrist
   glowing and crackling, not a storm. **TUMP: at rest, Zack's electricity is a few narrow
   branching sparks on the slipper in his hand.** The big electricity is saved for the moment.
3. **The frame is built around the UI.** Gekko's Wingman stands in the gap between the left
   list and the right cards, on a blurred street. **TUMP: Zack stands in the gap between the
   left column (HERO, LOADOUT, SHOP) and the mode card and PLAY, his face inside every aspect
   the hub supports, and nothing busy sits under a button.**
4. **A reveal, then a long calm.** Clove's screen opens in darkness and blooms. Vyse's is a
   close-up on a glowing visor. After the reveal the loop is quiet enough to sit in for
   minutes. **TUMP: the owner's storyboard is the reveal, then about 20 seconds of calm.** A
   three-second loop of the storyboard alone would be exhausting behind a menu.
5. **Personality over action in the idle.** Wingman sips a boba. Brawl Stars and Fortnite
   idles carry small fidgets, and a tap gets a reaction. **TUMP: Zack makes a difficult thing
   look casual (LORE), so his idles are a slipper flip caught without looking, a spin on one
   finger by the strap, and static he shakes off his hand.**
6. **Depth and air.** Valorant uses shallow focus and parallax; Slay the Spire 2 keeps its
   sky moving so slowly it reads as weather, not animation. **TUMP: skyline, clouds, laundry
   and banderitas all move, all slowly.**
7. **Impact frames and lettered sound.** Hi-Fi Rush and Street Fighter 6 sell a hit with a
   flat-colour frame, a freeze and a lettered sound; that is exactly what the owner drew
   (the black frame, the yellow flash, TUMP!). **TUMP: the flat frames stay flat, the hit
   freezes for three frames, and TUMP! is lettered in the game's display face.**
8. **Seamless or nothing.** A visible seam every 30 seconds is the cheapest-looking thing a
   home screen can do. **TUMP: every ambient motion is periodic on the loop length, so frame
   900 is frame 0.**

## 3 · The look: the game's own models, in her painted set

⚠️⚠️ **THE CHARACTERS ARE NOT DRAWN. THEY ARE `team-zack.glb`, `team-sean.glb` AND
`tsinelas_tsinelas.glb`, POSED AND RENDERED PER FRAME.** Two hand-drawn passes (first in her
outline style, then as SVG voxel blocks traced off the models) were rejected on the face, the
hair, the movement and the side view: 🧑 2026-09-23, *"that video still loooks ugly as fuck"*,
*"his hair for example is weird"*, *"side view looks so bad"*. A flat drawing of a 3D model is a
guess at every angle it was not traced from, so `src/three/actor.tsx` renders the real models
offscreen with Three.js and places each one into the SVG shot as an image. The set stays hers.

- **The shading is the game's `TumbangPreso/Toon`, transcribed**: the per-hero palette remap by
  atlas cell (`person_team-*.tres`), two hard bands, and an inverted ink hull on welded normals in
  `ToonSkin.Ink`. Faces are shaded flat, one normal per voxel face. A warm rim from the sunset
  behind him keeps him from reading as pasted on.
- **Poses are keyed on the real rig** (root, torso, head, two arms, two legs), per beat, in
  `src/three/zackWide.ts` and in each shot. Conventions measured on test boards: yaw +90 faces
  screen right; a limb's +x swings its end back; arms pitch in the shoulder frame before the
  T-pose drop (the game's `PoseKey` order).
- **Expressions are decals on the head bone.** His texture's own face (half-lidded, smirking) is
  the resting look; `open`, `sharp`, `glow` and `grit` lay thin plates over it so they turn with
  the head. The impact frames wash the whole figure to the flat field's colour with no hull, so
  only his eyes remain.
- **No fingers anywhere**, because the models have none.
- **Her palette still rules the set** (`src/lib/palette.ts`): the logo's seven, her burst for
  impacts, **no blue in anything drawn in code** (`CLAUDE.md` § 6.4). The tsinelas keeps the
  game model's own colours: it is authored art, and § 6.0 forbids repainting it.
- **The camera is never still**, even at rest (🧑: *"dynamic camera movement"*): idle drift,
  a dutch push-in on the wind, whip pans between shots, a whip-in on the run home.

## 4 · The loop, shot by shot (30 fps, 1260 frames, 42.0 s)

⚠️ **`src/lib/beats.ts` is the one timetable**, and every in-beat offset goes through `bt(n)`
(1.6x the original cut's frames). 🧑 2026-09-23: *"give each moment time to breathe"*. If this
table and that file ever disagree, the file is right.

| Time (s) | Beat | Shot | What happens |
|---|---|---|---|
| 0.0 to 2.5 | | WIDE | Calm idle while the hub's UI arrives: breath, weight, the casual toss |
| 2.5 | `pushIn` | WIDE, pushing in | The wind rises; he squares up, chin into it, arms float out as the charge builds |
| 4.9 | `cu` | CLOSE-UP, face | His resting face in the gust, held long enough to read |
| 6.7 | `eyesOpen` | CLOSE-UP, crash zoom | His own eyes ignite electric yellow (never redrawn) |
| 7.2 | `black` | IMPACT FRAME | Flat black, only crackling arcs |
| 7.45 | `yellow` | IMPACT FRAME | Flat gold, his own eyes in ink |
| 7.6 | `powered` | CLOSE-UP, powered | Glowing eyes, arcs off his head |
| 8.7 | `hand` | CLOSE-UP, hand | Low and in front: fist and tsinelas crackling, eyes behind; the grip tightens |
| 9.8 | `reverse` | OVER THE SHOULDER | He aims at the far lata and HOLDS |
| 11.0 | `snap` | REVERSE | Wind-up and snap, the bolt to the can |
| 11.35 | `hit` | REVERSE, hit-stop | Frozen frames, her burst behind the can |
| 11.5 | `tump` | REVERSE | The can flies, Sean flinches, **TUMP!** held to read |
| 13.4 | `run` | SIDE-ON tracking | He sprints for his tsinelas, turned toward us |
| 15.2 | `scoop` | SIDE-ON, slow motion | The slide; Magnet snaps it into his hand |
| 16.1 | `turn` | SIDE-ON | He plants and turns back |
| 16.8 | `lunge` | SIDE-ON, slow motion | Sean dives and closes on his afterimage |
| 17.9 | `arrive` | WIDE, whip-in | He runs home onto the throw line |
| 18.9 | `settle` | WIDE | Snap turn to face us, dead stop, eyes lit through the chatter |
| 0 to 42 | calm | WIDE | Between beats: the casual toss (`toss` in `zackWide.ts`) |
| 22.5 | `peek` | WIDE | A glance to camera |
| 27.0 | `flip` | WIDE | Flip and catch without looking |
| 32.0 | `spin` | WIDE | Strap spin, stops dead |
| 37.0 | `shake` | WIDE | Static builds in his hair, his eyes flare, he shakes it off |

Whip pans between shots stay 4 to 6 frames on purpose: cuts are not re-timed.

## 5 · Safe areas (1920x1080 source pixels)

- **Every aspect the hub supports**: the video is fitted by COVER. 4:3 keeps x 240 to 1680;
  the owner's 1600x680 window keeps y 132 to 948. **His face, the can and TUMP! are inside
  x 240 to 1680, y 150 to 948 in every shot.**
- **Under the UI, quiet**: left column x 0 to 560; top band y 0 to 190; mode card and PLAY
  x 1480 to 1860, y 590 to 1030.

## 6 · How it is on the HOME layer

`Assets/TumbangPreso/Runtime/UI/Hub/HubSceneVideo.cs`, installed by `TumpHub.Install` into the
hub's reserved `Scene` layer, above the live court (which stays underneath as the fallback).

- Loads `Resources/UI/home/zack-home-loop.mp4` (the game copy: H.264, crf 21, `-tune
  animation`, ~23 MB, no audio) and `zack-home-poster.png` (frame 0).
- Poster first, so the screen is never empty while the decoder prepares, and the poster stays
  if the clip fails to decode. `ReducedUiMotion` shows the poster and never starts the decoder.
- Enveloped at 16:9 (`AspectRatioFitter.EnvelopeParent`), never stretched.
- ⚠️ **HOME only.** Under every other hub screen it is hidden and paused, so the lobby shows the
  room's map (the live court), never a frozen HOME frame. It resumes where it paused.
- Guarded by `Tests/PlayMode/HubSceneVideoTests.cs` (in the `screens` group of
  `tools/playmode_suite.py`): the clip prepares, the frame counter advances, it is photographed
  at 1920x1080 and 1600x680, and it hides and pauses behind HERO and resumes on Back.
  The same fixture holds the BH Studios mark at its true 445x370 (`nPOTScale: 0`).

Sound cues, if the hub wants them: wind rise 2.5 s, eyes ignite 6.7 s, impact frame 7.2 s, snap
11.0 s, hit 11.35 s, TUMP! 11.5 s, run 13.4 s, Magnet catch 15.2 s, tag miss 16.8 s, run home 17.9 s.

## 7 · Working on it (any machine)

Needs Node 20+ and `ffmpeg` on `PATH`. From `ArtSource/home-scene/`:

```
npm install          # if sharp or esbuild fail to load: npm approve-scripts --allow-scripts-pending
npm run refs         # copies team-zack.glb, team-sean.glb, colormap.png and tsinelas_tsinelas.glb out of Assets/
npm run studio       # live editor at localhost:3000, scrub the HomeScene composition
npm run typecheck
node scripts/stills.mjs look 0 210 300 540   # stills to scratch/, one bundle (WebGL via --gl=angle)
node scripts/still.mjs ActorTest board 0      # the model test board: poses, faces, the held tsinelas
node scripts/sheet.mjs look 4                 # contact sheet of them
npm run draft        # half-res preview, out/draft_half.mp4, ~5 min
npm run master       # full 1080p30 master, out/zack_home_loop_1080p30.mp4, ~25 min
npm run ship         # game copy + poster into Assets/TumbangPreso/Resources/UI/home/
python scripts/smooth.py out/zack_home_loop_1080p30.mp4   # motion audit: hitches vs intended cuts
```

Then run the guard: `Unity.exe -batchmode -runTests -projectPath . -testPlatform PlayMode
-testFilter HubSceneVideoTests -testResults Logs/homescene.xml`, and read the XML, not the exit
code. `out/`, `scratch/`, `node_modules/` and `public/ref/` are not committed; everything else
is, including her storyboard and layout sketch in `reference/owner/`.
