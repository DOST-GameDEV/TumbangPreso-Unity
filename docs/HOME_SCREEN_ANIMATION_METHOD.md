# Making a home-screen animation: the method

Read this before making ANY home-screen, menu-background, season or hero-showcase animation
for TUMP. It records how the Zack HOME loop (`ArtSource/home-scene/`,
`docs/reports/home-scene/README.md`) got from "ugly as fuck" to "this is amazing i love it"
in one day, including every approach that failed and why.

⚠️⚠️ **REUSE THE METHOD, NOT THE PIECE.** The Zack loop is one hero at one court. The next one is
a different hero, a different place and a different story; copying its shot list, its timetable,
its poses or its jokes (the tsinelas toss, the chase) produces a second Zack loop with a new
palette, which is the "same template across the portfolio" failure the owner has banned
everywhere. Take the process, the rules and the checks below. Design the content fresh.

---

## 1 · Research before drawing anything

The owner asked for it explicitly: *"thoroughly research valorant main screens and main screens
of other games and figure out what makes them good"*. Do it for real (watch the screens frame by
frame, not articles about them) and write the findings down as rules, then design against them.
What the research gave the first loop, and what still applies to any future one:

1. **One featured hero, in their own world.** A home screen is a character in a place, not a
   wallpaper. Pick the place from `LORE.md`, not from what is easy to draw.
2. **Power shown small at rest, big only in the moment.** A few sparks in the idle; the storm is
   for the reveal.
3. **The frame is built around the UI.** Know the hub's safe areas first (§ 6) and keep anything
   busy out from under buttons.
4. **A reveal, then a long calm.** The player sits on this screen for minutes. The hero moment is
   a few seconds; the rest must be comfortable to look at forever.
5. **Personality in the idle.** Small, characterful, repeatable actions, never a mannequin.
6. **Depth and air.** Parallax layers and weather-slow ambient motion.
7. **Impact frames and lettered sound** for the hit, the way anime and fighting games sell one.
8. **Seamless or nothing.** Every ambient motion periodic on the loop length.

Also collect the owner's own material first (storyboards, sketches, wireframes) and ASK which art
is theirs. ⚠️ The title-street painting in the repo is AI generated and is NOT a style reference;
the owner's hand-drawn art is the TUMP logo, the login buttons and "slipper with hit".

## 2 · The characters are the real models. Never draw them.

This is the lesson that cost the most. Two whole passes drew Zack by hand, first in the logo's
outline style, then as SVG voxel blocks traced off model photos. Both were rejected on the face,
the hair, the movement and the side view: *"his hair for example is weird"*, *"side view looks so
bad"*. A flat drawing of a 3D voxel character is a guess at every angle it was not traced from.

**Render the game's own `team-*.glb` models, posed per frame, and composite them into the
painted set.** `ArtSource/home-scene/src/three/actor.tsx` is the working implementation; reuse it.
What it does, and why each part matters:

- **The game's look, transcribed:** `TumbangPreso/Toon`'s palette remap by atlas cell (per-hero
  `MapSource/materials_persons/person_team-*.tres`), two hard bands, per-FACE flat normals (an
  interpolated normal drew diagonal shadows across flat voxel faces), and the inverted ink hull
  on welded normals in `ToonSkin.Ink`. Add a warm rim matching the scene's light so the figure
  does not look pasted on.
- **Offscreen render, read back as an SVG `<image>`.** WebGL inside an SVG `foreignObject` renders
  blank headless, and an HTML canvas over the SVG cannot sit between two SVG layers.
- **Props are the game's too** (the held tsinelas is `tsinelas_tsinelas.glb`), scaled up a little
  where the shot is about them, and never repainted (`CLAUDE.md` § 6.0).
- **Key light from the camera side of the figure**, even when the story's sun is behind: a hero
  lit only from behind is a silhouette with no face, and near-black hair lit from beyond becomes
  one black blob.

### 2.1 ⚠️⚠️ Never change a character's face

*"the eyes u put looks like shit"*, *"that shit is floating on his face"*, *"if ur gonan do eye
shit js make it glow dont fucking change the actual eyes"*, *"he doesnt have eyeballs too just
black shits"*. Decal eyes, eyebrows the model does not have, and "expression" plates all failed:
they are the wrong style and they float off the face at any angle but the front.

**The model's face is the face.** Effects happen IN the shader, on the model's own texels: the
eyes glow by recolouring the palette slot the eyes use, limited to the eye band. **Find that slot
by reading the glb** (vertex positions, UVs, atlas cell), not by guessing from darkness or depth;
guessing failed twice before the data answered it in one query.

### 2.2 Rig conventions for these models (measured on test boards)

Root, torso, head, two arms, two legs, T-posed. Yaw +90 faces screen right. On a bone, +x tips its
top forward, so a limb's +x swings its end back. **Arms pitch in the shoulder's frame BEFORE the
80° T-pose drop** (Unity's YXZ order, as the game's `PoseKey` does); pitching after the drop only
twists the arm. Arm z positive lifts it out. Clips in the glb (`idle`, `sprint`, `walk`,
`hero-*-sprint` and more) are usable, but a keyed pose you designed usually reads better.

**Always build a test board first** (`src/ActorTest.tsx`): the model at every angle and pose the
piece will use, before touching a shot. Every convention above came off a board in minutes.

## 3 · Directing

- **Silhouette and faces first.** Dead side-on, these models are a wall of hair: turn a runner ~20°
  toward the lens. From straight behind, the head is a black dome: go toward profile, make the
  figure smaller, and let the arm and the action lead.
- **Foreground parallax.** A figure near the lens must move faster than the world when the camera
  moves, or it sits in the corner as a blob.
- **The idle carries the screen.** The calm is most of the loop. Give the hero a small repeating
  characterful action (Zack's was a casual toss, yours must be the next hero's own), on a fixed
  grid, only in whole cycles inside calm windows, never colliding with a beat.
- **Keep the thing the shot is about visible.** A prop that hangs against same-coloured clothing
  vanishes; hold it where it reads.
- **Camera is never still**, but periodic on the loop.

## 4 · Timing: give every moment room to breathe

*"its going so fast i cant comprehhend it anymore"*, *"create more frames for each part happening
and let it flow naturally"*, and NOT *"changing a slider"*. Rules:

- **Re-time, never time-stretch.** Space the beats for reading (hold on the face before the power
  shows, hold the aim before the throw, hold the impact word), and scale every in-beat offset
  through one factor (`bt(n)` in `src/lib/beats.ts`), so each key gets more frames.
- **Do not scale cycles that have a natural rate:** run cadence, sparks, the idle action. Scaling
  those is what makes a re-time read as slow motion.
- **Do not scale cuts.** Whip pans stay 4 to 6 frames. Stretched, they became long blurs that made
  the piece harder to follow, the opposite of the goal.
- **One timetable file.** Every shot reads the beats from it; nothing hard-codes an absolute frame.

## 5 · The review loop that actually found the problems

- **Look at every frame of a transition, not samples.** The eye transition looked fine in a
  half-second contact sheet and was obviously wrong frame by frame.
- **Per section, large:** render contact sheets per beat at 480 px wide, not the whole loop at
  320 px. Most of the fixes came from the per-beat sheets.
- **Check the loop seam numerically:** SSIM of the last frame against the first should match two
  neighbouring frames (about 0.975 here).
- **Test in the game, not only as a video:** a PlayMode test that the clip prepares, the frame
  counter advances, and it behaves under other screens (`HubSceneVideoTests`).
- **Send the owner the video after every meaningful pass**, and take the reaction literally. Every
  big fix in this piece came from one sentence of owner feedback on a render.

## 6 · Shipping it into the game

- The hub's `Scene` layer is where a home background lives (`TumpHub.Install`); a `VideoPlayer`
  into a `RenderTexture` on a `RawImage`, enveloped at 16:9, poster first, reduced motion shows the
  poster, and it is HOME's only (the lobby shows the room's map).
- Safe areas: the hub fits the video by cover, so faces and key action stay inside x 240 to 1680
  and y 150 to 948 of 1920x1080; keep the left column, top band and PLAY area quiet.
- No LFS in this repo: commit a re-encoded game copy (H.264, `-tune animation`, crf ~21, about
  25 MB for 30 to 40 s), keep the crf 17 master out of git.
- `npm run refs`, `npm run master`, `npm run ship` in `ArtSource/home-scene` are the pipeline.

## 7 · Anti-patterns, all of which happened once

| Did this | What went wrong |
|---|---|
| Drew the characters (SVG shapes, traced silhouettes) | Wrong at every angle; face, hair and side view rejected |
| Laid decal eyes and brows on the face | Wrong style, floated off the face at angles |
| Used the AI title painting as a style reference | It is not the owner's art |
| Lit the hero from behind to match the sun | Faceless silhouette; black hair blob |
| Filmed side-on and straight from behind | Walls of hair instead of characters |
| Let the idle stand still | "Mannequin" for most of the loop |
| Ran the whole hero moment in 7 seconds | "Too fast, can't comprehend it" |
| Stretched whip pans with the re-time | Long blurs, harder to follow |
| Guessed which texels are the eyes | Lit the fringe instead; read the glb |
| Three.js default colour management | Every hex darker than written; washes did not match flat fields |
