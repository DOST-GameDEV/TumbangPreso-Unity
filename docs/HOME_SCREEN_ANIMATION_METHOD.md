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

## 0 · ⚠️⚠️ THREE STANDING RULES, BEFORE ANYTHING ELSE

🧑 2026-09-24, on Phaister's second loop: *"each scene should have time to breathe and they should be
more expressive / show the persoanlity of the characters and should tell some sort of story"*, after
*"not make it too fast and give each scene time to breathe so they could be pcessed by people"* and
*"make her more expressive and like make it showcase her personaliyt"*. They apply to EVERY home,
menu, season or showcase animation, and a piece that breaks one is not finished however good its
frames look.

1. **EVERY BEAT BREATHES.** A viewer has to see a thing happen, understand it, and see its result
   before the next thing starts. Set up, land, react, then move on. If a beat cannot be named by
   someone watching it once, it is too fast: re-time it (§ 4), and if the loop is too short to hold
   the story, make the loop longer rather than the beats shorter (Phaister's is 54 s against Zack's
   42; `HubSceneVideo` plays any length, and a hero's clock lives in its own `time.ts`).
2. **THE CHARACTERS ACT, AND THE ACTING IS THEIR PERSONALITY.** Do not only show what a character
   DOES; show how they feel about it. Every action gets a reaction, in the body (the face is the
   model's and is never redrawn, § 2.1): pride, a flourish to the audience, a sag when it does not
   land, a stamp of frustration, delight. Take the reactions from the character's own lore
   (`LORE.md`, `docs/CHARACTER_ORIGINS.md`, `ASTRA.md`), not from a generic list. A second character
   with a point of view, or a companion like Kuro, doubles what the acting can say.
3. **IT TELLS A STORY, AND THE STORY IS ABOUT TUMP.** A goal, attempts that escalate, and a payoff
   (a punchline, a reveal, a turn), built from the street game itself: the throw, the can going down,
   the taya resetting it, the retrieval run, the tag. Put it on the hero's real map with its real
   landmarks and let the hero's real kit be the escalation. A power shown with no reason to be used
   is a demo reel, not a home screen.

The Zack loop (a power fantasy with one arc) and Phaister's (a three-attempt joke with a punchline)
are the two worked examples. Neither is a template: the next hero needs their own story.

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

### 4.1 Smoothness: no motion may start, stop or switch on a single frame

🧑 on a push-in that cut to a slightly different close-up: *"WEIRD ASS TRANSITION HERE"*, *"it
wasnt smooth"*. Every one of these was a real hitch, found by the motion audit in § 5:

- **A push-in and the close-up it arrives at are ONE shot.** The push lands exactly on the
  close-up's opening camera, the close-up starts from the pose the push ends on, and the figure is
  rendered with the same figure camera either side. A cut between two near-identical framings is a
  jump cut, the worst transition there is.
- **Ease in AND out.** A move that only eases in arrives at full speed and stops dead.
- **Motions that cross a join are owned by one function** that both shots call, so they cannot
  disagree about where the head is.
- **Layer effects ON TOP of the current camera** (a crash zoom adds to the hold), never as absolute
  values that restart it.
- **Shakes are smooth noise that decays**, not a fresh random offset every two frames, which
  strobes the whole image.
- **Nothing switches:** flashes fade, glows ramp or flicker up like a neon tube, a figure turning
  round spins through the angles instead of swapping its yaw.

## 5 · The review loop that actually found the problems

- **Run the motion audit on every full render:** `python scripts/smooth.py out/<video>.mp4`
  prints each frame whose change from the previous one spikes above its neighbours. Every intended
  cut and flash appears; anything else is a hitch. It found the strobing shake, the popping turn
  and the eye flash that switched off, none of which were visible in contact sheets.

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

## 5b · A second loop is a different film, not a new palette

Phaister's loop (`docs/reports/home-scene/phaister.md`) is the worked example of reusing the method
without the piece. Its first version was a deliberate opposite of Zack's in every row (night, one
unbroken shot, a magic trick) and the owner sent it back: the set was a generic tulay, there was no
game in it, and she never went anywhere. The second version keeps the method and changes the brief:
**a home loop tells a small story about TUMP itself**, on the hero's real map, with the hero's real
kit, and a second character with a point of view. Its set is authored in metres on the map's own
coordinates and projected through a real camera with pitch and near-plane clipping
(`src/phaister/view.ts`), so a lens can follow a slipper, run beside a runner and crane over a
21 m circle. Compare the new loop's frames against the shipped ones side by side before calling it
done: the owner's bar is *"atleast same level or EVEN better"*.

## 6 · Shipping it into the game

- HOME picks one hero's loop AT RANDOM (🧑 2026-09-24): ship `Resources/UI/home/<hero>-home-loop.mp4`
  and `<hero>-home-poster.png` (`npm run ship:<hero>`), then add the id to `HubSceneVideo.Heroes`.
- The hub's `Scene` layer is where a home background lives (`TumpHub.Install`); a `VideoPlayer`
  into a `RenderTexture` on a `RawImage`, enveloped at 16:9, poster first, reduced motion shows the
  poster, and it is HOME's only (the lobby shows the room's map).
- Safe areas: the hub fits the video by cover, so faces and key action stay inside x 240 to 1680
  and y 150 to 948 of 1920x1080; keep the left column, top band and PLAY area quiet.
- No LFS in this repo: commit a re-encoded game copy (H.264, `-tune animation`, crf ~21, about
  25 MB for 30 to 40 s), keep the crf 17 master out of git.
- `npm run refs`, `npm run master`, `npm run ship` in `ArtSource/home-scene` are the pipeline.

### 6.1 ⚠️⚠️ Known bug: a long render freezes with no error

**Symptom.** The render stops advancing and prints nothing, or prints "Target closed" and retries
forever. The headless Chrome tabs sit at full CPU. Rendering any single frame of the frozen range
on its own works, in 2 to 3 s. It hit Phaister's loop four times on 2026-09-24 (frames 1178, 224,
then twice at 1080 to 1349) and cost most of an hour.

**Cause: a memory buildup in the browser tab, not a broken frame.** The models are drawn offscreen and
each figure is read back as a PNG data URL (`three/actor.tsx` `drawActor`), up to 4096 px square on a
close-up. Remotion renders many frames in a row in the same tab, and those images pile up until the
tab spends all its time freeing memory (garbage collection) and never finishes the frame. More
figures, closer shots and more parallel tabs make it sooner.

**Fix.** Never render a long stretch in one browser. `scripts/chunks.mjs` renders each 270-frame
chunk as 45-frame PIECES, each piece in its own fresh browser, at concurrency 2 (~10 to 30 s a piece,
a piece with no new frame for 90 s is killed and retried, up to five times); finished chunks and pieces are kept, so a rerun
resumes. Use it for every hero's draft and master (`master:phaister` does). Do not raise the piece
size or concurrency to go faster without re-measuring a close-up stretch.

**Do not diagnose it by waiting.** If a render has not advanced in two minutes, it will not: kill that
render's own process tree by PID and render the stuck range frame by frame to prove no frame is
broken. Also: never run Unity while a render runs (Unity segfaults, exit 139, and writes no test
results), and set `chrome-headless-shell.exe` to the high-performance GPU in Windows
Graphics settings on a laptop with two GPUs.

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
| Filmed a hatted hero from eye height, and let her bend or lean back (Phaister, 2026-09-24) | The brim's black underside became a slab over the whole figure; the lens went down to 1.35 m and every lean stays under about 20 degrees |
| Raised her arms past about 45 degrees | They vanished behind her hair and hat on the test board; a V to the sides reads, straight up does not |
| Put the train on the guideway right over the lens | From under the deck's edge the deck hides everything on top; the guideway moved 5.3 m off and the train was re-timed to clear her hat |
| Hung the sigil and the KLANG! over her | Both hid her face; effects sit BESIDE the face they belong to |
| Reused the hero's own game clips (sprint, slide, her casts) for a loop (Phaister v2, 2026-09-24) | 🧑: *"make ur own animation"*, *"it wont be as dynamic if u js reuse animations she has"*; every pose is keyed by hand, on a pose TRACK so each hand-over is continuous |
| Drew a set "like" the map from a screenshot | 🧑: *"make sure it actually loooks like the map"*; read the map's builder for its numbers (map.ts) so every landmark stands where a player has thrown past it |
| Typeset the UI font on street signs | 🧑: *"dont outright use fonts its so ugly"*; street words are painted (lettering.tsx, a brush skeleton with wobble, outline and drop shadow); lettered SOUND words may use the display face |
| Framed a running figure from behind, and a pointing one from behind | A wall of hair and a hat; the lens runs ahead of the runner, and a point at something far off is an over-the-shoulder that holds the target in focus |
| Followed a thrown slipper tightly with a whip pan | An unreadable blur for a whole second; hold one wide frame that contains the whole trick (her, the hoop, the column), then whip once, when it turns for home |
| Let a figure behind the lens be projected | Clamped to the near plane it drew as a giant hat over the frame; cull anything whose depth is under the near distance |
| Put a lens behind a column or a pole | Measure the line from the lens to the subject against the map's column and pole positions before choosing it |
| Rendered Kuro from his glb alone | A blank cream box: his restored body is UV-mapped onto the person atlas and painted with his OWNER's palette at runtime (`GhostPetCompanion.ApplyAppearance`) |
