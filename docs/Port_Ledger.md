# Port Ledger — every Godot source, and where it went

This file exists because the port kept "finishing" while whole features were
missing. It is the authoritative checklist. **Nothing is done until every row
below reads CONVERTED.**

Scope measured 2026-08-15 against `DOST-GameDev` @ Godot 4.7:

- **45 gameplay scripts, 31,314 lines** of GDScript under `scripts/`
- **27 scenes** under `scenes/`
- **14 input actions**, **9 autoload singletons**
- (`tools/` is ~20k more lines of dev probes. NOT game features, NOT in scope,
  except as a reference for how a system is supposed to behave.)

Unity side today: ~13k lines of C#, of which ~1,900 are editor converters.

## 2026-08-16 — the parity pass, and the nine faults it found

Reported from a side-by-side of the two builds. Every one of these is written down
because each was invisible to a test, a compile and a screenshot of the editor, and
several are the SAME failure mode wearing a different hat: something converted, went
unread, and reported success.

1. **The viewmodel seat kept Godot's Z.** `Vector3(0, -0.10, -0.16)` is 16 cm in FRONT
   of a Godot camera and 16 cm BEHIND a Unity one. The arms straddled the near plane
   and drew as slabs across the top of the frame.
2. **`MaterialKit` asked which shader EXISTS, not which pipeline is RUNNING.** The URP
   package is installed and no pipeline asset is assigned, so `Shader.Find` returned a
   URP shader that the built-in pipeline cannot draw: the arms and the tsinelas in the
   player's own hand rendered as the error material, bright magenta, in every build.
3. **The map preview copied the .tscn's placeholder camera** instead of the registry's
   framing, which `map_preview.gd` overwrites every frame. 8.5 m and no pitch where the
   game frames from 16 m looking down.
4. **The FPP field of view was 75 for both cameras**; `CameraRig.tscn` is 95 and 70.
5. **The splash built its canvas as a CHILD** of a converted node, so it inherited that
   node's rect: a hundred-pixel video in the middle of a black screen.
6. **`EnvColourPass` had no roof atlases**, tinted by `Random` rather than by the
   instance name hash, and painted parked cars as houses.
7. **The kill plane was geometry with no behaviour**, and its collider was solid.
8. **`TrajectoryPreview` had never been instantiated by any code path.**
9. **Nothing captured or released the mouse except the title screen**, so a menu
   reached from a match was unclickable while drawing perfectly.

⚠️ **The tool that found half of these is `UiClickProbe`**, a PlayMode test that
raycasts every button on every screen and names what is on top of it. Do not delete it:
"the buttons don't work" is invisible to every other check in this project.

### The boot sting's background is WHITE in both engines, and the grey was a fade

⚠️ **A "colour delta" was recorded here and it was not real.** The reasoning was: the Unity
player renders the sting on white, a screenshot of the Godot build showed it on mid grey,
and `Player.log` warns that `opening_animation.mp4` carries no colour primaries and Media
Foundation may shift the colour. Three true facts and a wrong conclusion.

Both files were then decoded and sampled, which is what should have happened first:

| file | codec | background at t = 2 s |
|---|---|---|
| `Art/video/opening_animation.mp4` (Unity) | h264 | 255, 255, 255 |
| `assets/video/opening_animation.ogv` (Godot) | theora | 255, 255, 255 |

Identical. The grey in the comparison shot is the SPLASH'S OWN FADE: `splash_screen.gd`
starts opaque black and tweens it out over 0.35 s, and a white frame under a black plate at
about 20% alpha is exactly that grey. The screenshot was taken during the fade-in.

⚠️ **The lesson is the one this ledger keeps re-learning: measure the asset, do not infer
it from a screenshot.** A capture carries the frame's animation state, the engine's
tonemap and whatever the chat client did to the PNG. The decoder warning in the log is
still true and still harmless.

⚠️ **And its first two runs lied.** It probed three frames after load, before the
pennants had finished unfurling and before the layout groups had run, and reported six
working controls as unreachable. It waits 120 frames now and reports OFF SCREEN
separately from BLOCKED, because those are different faults with different fixes.

## 2026-08-16 — the look pass: the street, the ink outline and the character screen

Reported from playing the build. Four complaints, and between them they turned out to be
six separate faults. Every one of them was invisible to a compile, a test and the
importer's own success report.

1. **⚠️⚠️ `mesh = ExtResource(...)` WAS NOT HANDLED BY THE MAP IMPORTER AT ALL, AND IT IS
   EVERY HAND-MODELLED PROP ON BOTH MAPS.** A node that INSTANCES a `.glb` came through the
   prefab branch; a node that merely POINTS AT a mesh resource fell through to "make an
   empty GameObject", so it kept its name, its parent and its transform and drew nothing.
   That is **74 nodes on Eskinita and 36 on Bayan Plaza**: every electric post, the laundry
   lines strung between them, both sari-sari stores, the tricycle, the tires, crates, oil
   drums, monobloc chairs and bollards, the corrugated walls, and **all of the chalk** — the
   hopscotch, the base circle, the confinement square and both throwing lines.

   ⚠️ **And the importer reported `0 MISSING` throughout**, because `missing` only ever
   counted the instance branch. It counts meshes now and reports them separately.

2. **The ground slab was rebuilt as a 1x1x1 cube.** A `BoxMesh`'s `size` was applied as a
   scale and then `ApplyGodotTransform` overwrote `localScale` from the node's basis on the
   very next line. The size is carried and multiplied in afterwards now. Its material was
   dropped as well, so Eskinita's asphalt slab was Unity white.

3. **⚠️⚠️ NO TOON MATERIAL AND NO INK OUTLINE EXISTED IN THE PORT.** `character_visual.gd`
   puts a two-band toon material with an inverted-hull outline on every Prop, and a Person
   gets the same two things from its palette `.tres`. Unity rendered the whole cast, both
   hero props and the first-person arms on the stock lit shader: no border, and a warm key
   plus 1.65 ambient washing every colour pale. It is the single largest reason the two
   builds did not read as the same game. `Shaders/Toon.shader` and `ToonSkin.cs` are the port.

   ⚠️ **The outline is a PASS in the shader, not a second material.** Unity's material slots
   map one-to-one onto SUBMESHES, so an extra material redraws only the last submesh: the
   border would have appeared on part of each model and nowhere else.

   ⚠️ **The world is still not allowed to wear it.** `env_toon_pass.gd` records the
   2026-07-29 revert (banding on flat surfaces, and the cost of a hull on every mesh in a
   dressed street), and the CHARACTER outline was confirmed as still wanted on 2026-08-16.
   Characters and props only.

4. **⚠️⚠️ NOTHING EVER SELECTED MOUSE AIM, SO THE GAME WAS UNPLAYABLE.** `AimSource`
   defaults to MOVEMENT, `CameraRig.StepLook` returns on its first line unless it is MOUSE,
   and `SetAimSource` had **no call site at all**. The mouse turned nothing, the body never
   yawed, and `CharacterMotor` compounded it by steering in WORLD space for every unit:
   `character_base.gd:912` is body-relative for a mouse-aimed unit and world-space only for
   one that steers by movement. W therefore walked the player along a fixed compass heading
   for the whole match. Reported as "controls are inverted and most dont work".

   ⚠️ A movement-aimed unit now turns to face its direction, which is `look_at` on the same
   line. Every bot steers that way, and its punch, lunge and shove all fire along the body's
   forward vector.

5. **The CHARACTER screen's model preview was broken four ways at once** — see
   `ModelPreview`'s own remarks. Nothing ever called `Orbit` or `Zoom` while the panel
   printed a hint line promising three controls; the render target was a fixed 512x640
   stretched over a panel of another shape; the framing fitted height alone and ignored the
   aspect; and the subject faced AWAY from the camera because Godot looks down -Z. The FOV
   was 32 against the .tscn's 42.

   ⚠️ **And the preview was lit by the arena behind it.** Godot's SubViewport is
   `own_world_3d`, so the map's sun cannot reach it. Unity has one world and the setup
   screen loads the chosen arena live, so the subject took its own key at 1.35, its own fill
   at 0.45 and Eskinita's key at 1.15 on top, and rendered as a white silhouette inside its
   own ink border.

6. **The BACK button was white.** `CharacterSelect.tscn` spells its look out as three
   `theme_override_styles` StyleBoxFlats instead of a `theme_type_variation`, and the
   importer only read the variation. With none to find it fell through to the theme's plain
   Button, CARD face and INK border, and came out as the only white control on a screen of
   brown wood. It is matched on the stylebox's own colours now, so any other control that
   spells itself out gets the right skin too.

⚠️ **The tool that found the shader fault is `ToonProbe`**, which renders one model on a
plain backdrop in about forty seconds. A player build plus a scripted playthrough is four
minutes per look, and a shading question takes several looks.

## 2026-08-16 — the hand, the lane, and the mesh that lags

Four more, all of them the same shape as everything above: something was built, nothing
called it, and no check could tell.

1. **⚠️⚠️ `Carrier` TOOK ITS HAND FROM A `[SerializeField]` AND NOTHING EVER ASSIGNED IT.**
   `MatchInstaller` installs the component with `AddComponent`, which cannot carry an
   inspector reference (rule 3, again), so the field was null on every unit in every build
   and the one line that keeps a held slipper in the hand never ran once. A picked-up
   tsinelas stayed exactly where the pickup left it and its carrier walked away from it.
   That is the third-person half of *"the slippers just float when you hold it, its
   completely unattached to person"*; the viewmodel fix hid it from the only player who
   could not see it.

   ⚠️ **The anchor is MEASURED off the skin, not transcribed.** The Godot side records eight
   guessed offsets that each landed somewhere wrong, and copying its final number would have
   repeated that: Godot's glTF importer keeps the file's right-handed axes and glTFast
   negates X, so the same three numbers are not the same place. Both engines skin through
   `bindpose[b]`, so the arm's own weighted vertices pushed through it give the hand's
   coordinates in the frame that rides the bone. Only the lift onto the TOP of the hand
   transcribes as a bare number, because Y is the axis neither importer flips.

2. **The seat was its own model root.** `CharacterVisual._modelRoot` defaulted to the seat,
   so `AlignToCapsuleFloor` moved the CharacterController along with the mesh and quietly
   re-sank the capsule it had just measured against. `MatchInstaller` has always built a
   `Visual` child for this and nothing pointed at it.

3. **⚠️⚠️ THE AI NEVER ASKED WHETHER ITS THROW WOULD REACH.** `_lane_blocked()` walks the arc
   the slipper will actually fly and asks the same question the flight asks, sample by
   sample, with the step sized off the SPEED so a body cannot fall between two samples. This
   port released the throw regardless: a bot with somebody between it and the can hit that
   body every single time, which reads as an AI that cannot aim rather than one that has no
   idea anybody is there. And `HasInterceptPoint` answered "is anything in flight", which is
   not a point to run to, so the taya committed to the plan and stood still.

4. **Remote smoothing had no counterpart at all.** A replicated update is written straight
   onto the body because collision, the hitbox offset and every directional verb read the
   body transform; the MESH is what glides. It is off unless a transport is running, so a
   single-player match is bit-for-bit unchanged.

⚠️ **And a test can fail for being right.** The smoothing test waited ninety FRAMES for a
rate expressed per SECOND, and the batch runner renders at over 500 fps: a sixth of a second
is not enough time and the maths was correct throughout. Wait on time.

## 2026-08-16 — the playtest pass: one input bug wearing five hats

Reported from playing the build, not from reading it. The through-line is that
**several complaints that sounded like separate features were one fault each**, and
two of them were the same fault.

1. **Jump and grab both did nothing, and it was ONE bug.** `JustPressed` is a diff
   against the last committed snapshot, and BOTH producers (`PlayerInputReader` and
   `AIController`) took that snapshot at the end of their own `Update`. Unity runs
   FixedUpdate BEFORE Update inside a frame, so by the time `ApplyGravity` asked
   `JustPressed(Verb.Jump)` the answer was always false. Every verb resolved in the
   physics step was unreachable — jump, the shove, the lunge — for a bot as much as a
   human. Godot never had it: a human there reads `Input.is_action_just_pressed`
   straight from the engine, which stays true through `_physics_process`, and only an
   AI keeps a prev table. The snapshot is now taken once, by the consumer, at the end
   of `CharacterMotor.FixedUpdate`.
2. **The whole cast rendered at 42% of its height.** `PERSON_SCALE` 2.38 was never
   applied to the model. It also put the hand anchor at 42% of where it belongs,
   because that anchor is measured off the skin.
3. **The tonemap was on the material instead of the frame.** `Toon.shader` carried
   the ACES curve, so it rolled off the CAST and nothing else — the sky, the fog and
   every world surface stayed raw and clipped. That is the blown band across the top
   of the map preview AND the first-person arms rendering near white beside a
   correctly-lit character. It now lives in `TumbangPreso/ColourGrade`, a camera pass,
   which is where Godot's Environment has it.
4. **The `adjustment_*` colour grade had never been ported at all.** Eskinita runs
   contrast 1.03, Bayan Plaza 1.07, both at saturation 1.18.
5. **Three more Environment fields the import dropped**: `ambient_light_sky_contribution`
   (Eskinita 0.35, so the arena ran 54% hot and 47% brighter than its neighbour for no
   authored reason), `fog_sky_affect` (0.22, why the fog "didn't cover the top" and met
   an unfogged sky along a hard line), and **the whole environment on the setup screen**,
   because ambient, fog and the sky live in the ACTIVE scene's RenderSettings and the
   preview arena is loaded additively.
6. **The full-width band across the menus was never a UI seam.** `MapPreviewSurface`
   loads a dressed street additively at the origin on the default layer, and the menu
   camera's culling mask is every layer, so the road slab was drawn into the frame.
   Seen almost edge-on a slab is a straight horizontal strip, which is why it was
   diagnosed as a UI rect edge twice.
7. **The emote wheel was eight white squares.** Every slice was an `Image` with a
   radial fill and NO sprite, so the fill cut sectors out of a square.
8. **Two sets of arms in FPP**, because the self-hide was still B-73's head-only
   version that `camera_rig.gd` had already reverted.
9. **The held slipper trailed the hand by a frame**, because the carry ran in Update
   and Unity evaluates the Animator between Update and LateUpdate.
10. **The lata's mark was never snapped to the ground** (`_snap_home_to_ground`).

⚠️ **And the capture harness was lying.** `UiRuntimeShots` assigned the render target
AFTER laying out the canvas, so every shot was the batch runner's own resolution
stretched into 16:9 — a 1.33x horizontal stretch on the picture and nothing else. It
was read off the captures TWICE as a fault in `ModelPreview`. A tool that measures the
build is part of the build.

⚠️ **One "fix" here was wrong and the tests caught it.** `ModelPreview.PlayIdle` was
rewritten from `SampleAnimation` to a Playables graph, on the reasoning that a
disabled Animator leaves nothing to bind to. False — `SampleAnimation` binds by PATH —
and the graph animated nothing: *"'arm-left' has not moved in 30 frames"*. Reverted.
The arms-out silhouette that prompted it is what this rig's `idle` actually looks
like, confirmed against the toon bench, which poses nothing and renders the arms down.

### Verified CORRECT while looking, so they were not touched

Written down because "I checked and it was already right" is a result, and re-fixing
a correct thing is how a port acquires a bug.

- **The taya cannot pick up a slipper.** `carrier.gd::_step_grab` returns early on
  `is_defender`. Any ATTACKER can pick up ANY slipper; ownership does not gate it.
- **You cannot throw from inside the box.** `ThrowRules.CanThrow` ends in
  `!Confinement.IsInsideBox(...)`, in the engine-free core, covered by its tests.
- **The tag stun is 5.0 s** (`TagStunTime` → `ApplyStagger`), `CanAct()` goes false and
  the status stack reads STUNNED.

  ⚠️⚠️ **THE SECOND HALF OF THIS ROW WAS WRONG AND IT COST A SESSION.** It read "there is
  no frost VFX in the Godot build either; that is a new feature request, not a port gap."
  There is. It is on `main` at commit `3eed6d3`, in both halves — `frost_vignette.gdshader`
  on the HUD and a `FROST_AMOUNT` term in `toon.gdshader` on the body. The claim came from
  searching a LOCAL MIRROR that was a month stale and sat on `online/dedicated-lobbies`.
  **`git fetch` before writing "does not exist in Godot" about anything**, or read `main`
  directly with `gh api repos/DOST-GameDEV/DOST-GameDev/contents/<path>?ref=main`, which is
  what a full fetch of that 260 MB repo is not worth.
- **The chalk is all there** — piko, tao, bulaklak, gulo and the two repeats.
- **`Main.tscn`'s kill plane row below is STALE.** The plane is not in `Main.tscn` at
  all: both arenas author it at y = -10 with a 260x4x260 box, and the importer binds
  `KillPlane` to it.

## 2026-08-16 — the stun frost, and the four checks that had never been run

The frost landed the session before this one and its three PlayMode tests were written,
committed and **never executed**, because the command they were handed carried
`-nographics`. Running them found one shipped defect and three faults in the tests
themselves. All four are written down because the shape is the one this ledger keeps
recording: something was built, nothing exercised it, and every other check said fine.

1. **⚠️⚠️ `-nographics` CRASHES THE EDITOR ON PLAYMODE AND EXITS 0.** It selects
   `NullGfxDevice`, and the first offscreen camera to render dies inside it
   (`RenderOffscreenCameras → RenderShadowMaps → ShadowMapJob →
   GfxDevice::DrawSharedGeometryJobs`). This project has three such cameras. The run
   died 360 log lines in, wrote no `.xml`, and returned success. **Assert on the result
   file, never on the exit code** — a crash and a failure both come back as 0.

2. **⚠️⚠️ `FrostVignette.shader` HAD NO `_MainTex`, AND A UI MATERIAL MUST HAVE ONE.**
   *"Material 'TumbangPreso/FrostVignette' … doesn't have a texture property '_MainTex'"*,
   logged on every canvas rebuild for as long as the frost was drawing. UGUI hands a
   Graphic's texture to the CanvasRenderer through that exact name and does not care that
   the effect is procedural. The ice rendered correctly throughout, which is why only a
   test that fails on unexpected error lines could see it. Declared and never sampled.

3. **The screen-half test waited on the wrong thing.** `UpdateFrost` enables the Image the
   moment coverage crosses 0.001 and then ramps for `FrostRampIn`; a loop that stopped at
   `frost.enabled` stopped on the first frame of that ramp and then asserted the coverage
   was above 0.5. It read 0.018. Wait on the value the assertion is about.

4. **⚠️ A `CharacterController` OVERWRITES A DIRECT `transform.position` WRITE.** The
   taggable-in-the-box test placed its victim by assignment and got "the harness failed to
   put the victim in the box" — the capsule was back at its spawn before the rule was
   asked. `CharacterMotor.Teleport` disables the controller around the write for exactly
   this reason and is the only supported way to place a unit. `Confine` does the same dance.

5. **⚠️⚠️ THE INTENT TABLE HAS EXACTLY ONE WRITER, AND A TEST THAT WRITES IT IS THAT
   WRITER.** `AJumpPressReachesThePhysicsStep` reported "the capsule never left the ground"
   against a jump that works in the player: `PlayerInputReader.Update` and
   `AIController.Update` both rewrite the WHOLE table every frame, so `Set(Jump, true)` was
   overwritten with `IsPressed()` — false, in a runner with no keyboard — before the next
   physics step could read it. Both producers are disabled on the seat under test now.

⚠️ **The lesson that generalises: a test written and not run is not a test.** Three of
these four were faults in checks that had never executed once, and the fourth was a real
defect that only running them could surface.

### `max_reach` is 0.36 and the ambiguity is CLOSED

Flagged in the last handoff as a guess. It is not one. `frost_vignette.gdshader` defaults
the uniform to `0.15` with a comment saying it was tuned down from `0.36` because the frost
"met in the middle and the arena read as fog" — but `HUD.tscn` sets
`shader_parameter/max_reach = 0.36` on the material, read off `main` and quoted here, and a
scene parameter overrides a uniform default. **0.36 is what the shipped Godot build
renders; the tuning pass never took effect.** The port matches the running game.

### The HUD's transcribed constants were re-checked against `main`, and they all hold

Read off `hud.gd` and `ui_theme.gd` at `main` rather than from memory, because the last session
proved a stale local mirror can be believed for a month: `TEXT_OUTLINE 8`, `CROSSHAIR_OUTLINE 5`,
`STATUS_ROW_LIMIT 4`, `STATUS_BAR_SIZE (190, 8)`, `STATUS_FONT_SIZE 20`, `STATUS_MARGIN (38,
150)`, `STATUS_UNDER_BOARD_GAP 18`, `DANGER_HOLD_ALPHA 0.16`, `FROST_RAMP_IN 0.14`,
`FROST_RAMP_OUT 0.5`, `FROST_THAW_TIME 1.6`, `DOWNED_FLASH_TIME` and `_PEAK 0.45`,
`TAYA_BADGE_FONT_SIZE 15`. **Zero mismatches against `Hud.cs`.**

Font sizes likewise: `HudTimer` 44, `HudBanner` 40, `HudToast` 28, `HudBody` 34, `HudCaption` 32,
`HudScore` 32, plus the two per-node overrides `hud.gd` applies on top — the round line down to
**20**, so the port's 20 is right and is NOT `HudBody`'s 34, and the vulnerable line at 22. The
.tscn's `ScoreTitle` 22, name cell 132, score cell 64, row separation 14, column separation 4 and
`outline_size` 5 all match. The countdown's 40 is `HudBanner` and is correct parity; with the
existing 1.8x pop its effective peak is 72 px, which is a look decision rather than a port gap.

### The roof atlases: the mechanism is identical in both trees, verified end to end

The last handoff left "house colours still do not match" open. Checked as a chain of identities
rather than by comparing two shots that do not even frame the same street:

- **The instance names**, read out of `Eskinita.unity`: `Bahay_*` 10, `Likod_*` 12, `Kanto_*` 9,
  `MalayoX_*` 34, `MalayoZ_*` 34. Those are exactly the five prefixes `IsBuilding` accepts, and
  they total **99** — which is what `[Env] repainted 418 of 494 renderers, 99 with a roof
  variant` reports. Every building on the map gets a roof; none is missed.
- **The hash**: a C# `int` wraps on `value * 31` where GDScript's 64-bit int does not, but
  `& 0x7fffffff` keeps the low 31 bits either way and a two's-complement multiply's low bits are
  unaffected by the overflow. Same answer, and the `long` in `Pick` covers the other half.
- **The pick**: `(hash * 13 + 5) % 6` roof, `(hash * 7) % 6` facade, `(hash * 11 + 3) % 5` foliage.
- **The atlases**: all six PNGs are byte-identical between the two repos (MD5).
- **No warnings**: the pass logs a line per surface whose shader carries no colour property it
  knows, and there are none.

Same names, same hash, same formula, same order, same files: the assignment cannot differ.

⚠️⚠️ **AND THE ASSIGNMENT WAS NEVER THE PROBLEM — THE ATLAS WAS NEVER APPLIED, AND THE PASS
REPORTED SUCCESS THE WHOLE TIME.** With the capture finally un-stretched and framing the same
street as `g04-ready.png`, the comparison could actually be made: the Godot street is red, rust
and slate, and this build had **no red roof anywhere in frame**. Every roof in the game was the
kit's shipped green.

The cause is one list. `Paint` writes the roof texture through `TextureProperties`, which held
`_BaseMap` and `_MainTex` — and these materials arrive through glTFast carrying
**`baseColorTexture`**, which is in neither. The TINT landed, because `baseColorFactor` IS in
`ColourProperties`, and that is what made this so hard to see: the street was visibly being
repainted per instance, so the pass looked like it was working, and only the ROOFS were
untouched — the one thing a tint cannot vary, since green multiplied by anything is still green.

⚠️ **And it failed in exactly the silence rule 5 was written about**: *"a property block writes
a NAMED property, silently doing nothing when the shader has none."* `Paint` warned when no
COLOUR property matched and said nothing when no TEXTURE property did, while
`repainted 418 of 494 renderers, 99 with a roof variant` — a count that was correct and meant
nothing — printed every load. Both glTF names are in the list now and a miss is reported.

The earlier "house colours do not match" was therefore right, and every attempt to confirm it
was defeated by a capture that was stretched, colour-graded and framing a different street.

### The YOU card had a second, permanently empty bar — and one missing meter

Found by putting the fresh 1920x1080 arena capture beside `Logs/shots-godot/g04-ready.png`,
whose YOU card has exactly ONE bar. The port's had two.

`YouCard.Refresh` gated the meter rows on the ROLE alone — `_chargeRow.SetActive(_isAttackerPerson)`
— so every attacker carried an empty `[LMB]` meter from spawn to final whistle and every taya
carried an empty righting bar. `you_card.gd::_update_row_visibility` gates on role AND activity:

```gdscript
charge_row.visible = (_is_attacker_person and _charging) or _bump_charging
reset_channel_row.visible = _is_defender_person and _channeling
```

⚠️ **And the second half of that first line did not exist in this port at all.** The charge row
is SHARED: the attacker's throw charge and **the taya's lunge meter**. Sharing is safe precisely
because the two belong to different roles, so no unit can ever be charging both. The taya's card
showed a righting channel and nothing else, which left the verb that scores their points with no
readout. It has one now.

⚠️⚠️ **`LungeChargeRatio` IS A `Clamp01`, SO `>= 0.0f` AGAINST IT IS A TAUTOLOGY, AND TWO CALL
SITES ASKED IT THAT WAY.** `character_base.gd` keeps `_observed_lunge_charge` at **-1** when
nobody is winding up, and every reader compares against zero for that reason. The port had no
such value, so:

- **`AIController.ShouldEvade` believed a lunge was winding up on every frame of every round**,
  reducing a reaction to a TELL down to a proximity rule and spending the dodge budget on nothing.
- **`YouCard` had no way to ask "is a lunge happening"** and so could not draw the meter at all.

`CombatVerbs.ObservedLungeCharge` is the missing value and both now read it. A ratio and a state
travel in one number here, as they do in the .gd, because two fields can disagree across a peer
boundary.

### The runtime captures are 1920x1080 now

`UiRuntimeShots` shot at 1600x900 while every Godot reference in `Logs/shots-godot` is
1920x1080 and every HUD number in this port is transcribed from a .tscn authored in
1920x1080 space. Every comparison therefore needed a rescale before it could be measured,
which is how "it looks about right" kept standing in for a measurement. At the reference
size a pixel here is a pixel there.

⚠️⚠️ **AND THE ARENA SHOT WAS HORIZONTALLY STRETCHED 1.33x, WHICH IS THE SAME FAULT THIS FILE
ALREADY FIXED ONCE.** `CanvasScaler` recomputes in its own Update from the canvas's rendering
display size, so a capture rendered in the SAME frame the render target is assigned lays the HUD
out at the batch runner's own aspect and then draws it into a 16:9 texture. `Capture` (the menu
path) waits two frames for exactly this and carries a note about it; `CaptureScreen` (the arena
path) was written separately and never got the fix — and the arena is the shot the port is
actually judged on. Measured: the capture's scoreboard is 456 px wide where `HudLayoutProbe`
reads the same panel at 440, and 456/440 is 1920/1440, the runner's 4:3 canvas.

⚠️⚠️ **DO NOT MEASURE A UI COLOUR OFF THESE PNGs EITHER.** Every canvas in the game is
`ScreenSpaceOverlay`, which a real frame composites AFTER post, so the HUD a player sees is
ungraded. `Camera.Render` cannot see an overlay canvas at all, so the harness flips every canvas
to `ScreenSpaceCamera` to photograph it, and that puts the UI THROUGH `ColourGrade` (contrast
1.03 / 1.07, saturation 1.18). An exact-match search for amber `ffba00` in these files returns
ZERO pixels. That is the capture, not the build.

### `HudLayoutProbe` — HUD parity is measured now, not looked at

Every previous "the HUD matches" ended in a screenshot comparison, which cannot see a padding
that failed to apply or a label that measured itself at zero. `HudLayoutProbe` dumps every HUD
element's laid-out rect to `Logs/hud-layout.txt` through the CANVAS rather than off screen
pixels, so no colour grade can touch it and the answer does not depend on the resolution the
runner opened at. `HUD.tscn` is numbers; this makes the port numbers too.

It asserts almost nothing on purpose — only that the elements exist with non-degenerate rects. A
rect that disagrees with the .tscn can be a container legitimately hugging its content, and a
test that guessed which would fail for the wrong reason and be deleted.

⚠️ **Widths and heights are comparable to the .tscn; a centre-anchored X is not**, unless the
canvas width it prints is 1920. Every canvas matches on HEIGHT, so a 4:3 runner gives a
1440-wide canvas and centre-anchored elements land 240 px left of where a 16:9 one puts them.

**Two faults it found on its first run, both invisible to every check that came before:**

1. **⚠️⚠️ `TimerLabel` LAID OUT 196 WIDE AND *ZERO* TALL, SO THE CLOCK CARD COLLAPSED.** 240 x 32
   — the two 16 px margins with nothing between them — against Godot's 240 x 97. The single
   most-read element on the screen was a thin pill with its digits jammed under the top border,
   and the horizontal half of the padding applied perfectly, which is why it read as a font
   problem in the captures. Every neighbouring label already carries a `LayoutElement` for this
   (`ScoreTitle` 30, `RoundLabel` 34); this one inherited whatever the text generator reported.
   Fixed at **64**, measured off `g04-ready.png`: the Godot card's wood edge runs y28 to y124 and
   `_hud_wood_style` sets 16 px top and bottom margins.

2. **The YOU card was 96 tall and should be 132.** `YouCard.tscn` authors a fixed
   `16, -196 to 396, -64` and grows UP from the pinned bottom edge; the port had a
   `ContentSizeFitter` alone, so an ATTACKER — who shows two of the four rows — got a card sized
   to those two rows, sitting 36 px lower with its bar crowding the bottom edge. Confirmed
   against the reference by scanning the capture's own pixel column: the Godot card's
   role-coloured border runs y882 to y1015, which is the .tscn to the pixel. A `minHeight` floor
   plus the fitter is the .tscn's behaviour and keeps the growth the fitter was added for.

**And three more YOU card numbers were invented rather than read**, all from `YouCard.tscn`:
`ClassLabel` is `HudCaption` **32** and `DetailLabel` is `HudBody` **34** (both were 24), the
three meters are `custom_minimum_size` **(160, 26)** (the bar height was 18), and `IdentityRow`
has `separation` **10** (it was 8). The 6 px `GuardDashSpacer` between the name line and the
meters was missing entirely.

## 2026-08-16 — the line-by-line pass against `main`, and what it found

Asked for directly: *"compare thoroughly the logic of everything in godot as well to what is
in unity to make sure its working and well and is almost a one to one copy"*. Read off `main`
through `gh api`, never off the local mirror. Four live faults, and each is a line of GDScript
that did not make it rather than a system that was missing.

1. **⚠️⚠️ NEITHER AUTOLOAD RESET BETWEEN MATCHES, WHICH THE .gd FIXED AS B-14 AND THE PORT DID
   NOT CARRY ACROSS.** `match_manager.gd::reset()` and `round_manager.gd::reset()` both exist
   upstream, both carry the same note — *"nothing reset this autoload between matches, so a
   second match resumed the first one's score and round number"* / *"...the first one's
   timer"* — and neither had a counterpart here. Unity's `GameServices` is `DontDestroyOnLoad`,
   which reproduces an autoload's lifetime exactly, **including the bug it needed a reset for**.

   The symptom is worse here than upstream, because this port's free-roam window happens
   BEFORE `StartMatch`: a second match opened its whole ready phase showing the first match's
   final scores, its round number and its clock, with the LATA card up because `RoundActive`
   was still true, and snapped all of it to zero on "GO!". Caught in the arena capture — a
   freshly-loaded Eskinita opening on four seats holding 900 points each and a 01:11 clock.
   `MatchInstaller` now calls both, in the same place `main.gd::_ready()` calls them.

2. **⚠️⚠️ THE TAYA WAS PAID 100 FOR KNOCKING THEIR OWN CAN OVER.**
   `round_manager.gd::host_note_lata_knocked` is

   ```gdscript
   if by_slot >= 0 and by_slot != MatchManager.defender_slot:
   ```

   and only the `>= 0` half reached `Lata.HostKnockDown`. The defender's own slipper — or
   anything else that credited their slot — scored the attackers' event for them, and since the
   can spends most of a round on its side, standing it up and putting it back down is a loop
   worth 100 a go. The `round_active` guard was missing from the same function.

3. **⚠️⚠️ `AIController` BELIEVED A LUNGE WAS WINDING UP ON EVERY FRAME OF EVERY ROUND.**
   `character_base.gd` rests `_observed_lunge_charge` at **-1** and every reader compares
   `>= 0.0` against it. The port had only `LungeChargeRatio`, a `Clamp01`, so that comparison
   was a tautology: `ShouldEvade` reduced a reaction to a TELL down to a proximity rule and
   spent the whole dodge budget on nothing. `CombatVerbs.ObservedLungeCharge` is the missing
   value. The same absence is why the YOU card could not draw the taya's lunge meter at all.

4. **The seats were not registered until the countdown ended.** `SliceRunner.Begin` is what
   registers them and it does not run until "GO!", so for the entire free-roam window
   `RoundDirector` knew about nobody: no screen-edge arrow to the can, no `Players`-driven
   query answering truthfully. `main.gd` has the four on their marks and registered before it
   waits for R. ⚠️ **And `AiLaneTests` was passing only because a previous test in the same
   batch had left its seats on the persistent director** — it failed the moment that stale
   state was cleared, which is the leak doing the work of the feature.

### § THE SLIPPER STAYS ON THE ARM, NO MATTER WHAT

🧑 2026-08-16: *"make sure the slippers in unity stay on the arm no matter what — for others
and for yourself in ur FPP"*. Two mechanisms, because there are two views, and the report has
already been made twice about two unrelated causes.

⚠️⚠️ **`Carrier.LateUpdate` RETURNED EARLY ON A NULL HAND ANCHOR, WHICH ABANDONED THE SLIPPER.**
The anchor is measured off the skin's own weighted vertices, so it is absent while a model is
missing, mid-swap, or authored with a bone the resolver does not recognise — and in every one of
those the tsinelas simply stopped where it was and its carrier walked away from it. That IS the
reported *"the slippers just float when you hold it, its completely unattached to person"*,
reachable again through any rig whose arm does not resolve. It rides the body now
(`Carrier.CarryAnchor`), which is worse-looking than the measured pose and is not a bug anybody
reports: an object in roughly the right place moving with its owner reads as held. It is a
FALLBACK, re-asked every frame, so a model that finishes loading takes the real anchor back on
the next frame with no state to reset.

The rest was already right and is now asserted rather than assumed:

- **The carry runs in `LateUpdate`**, because Unity evaluates the Animator between Update and
  LateUpdate and a bone read in Update is the previous frame's pose.
- **The remote smoothing runs in `Update`**, so the model root has already moved before the
  Animator and the anchor is final by LateUpdate. The two orders compose; neither is accidental.
- **The viewmodel carries its OWN slipper.** The real hand is below the frustum in first person,
  so the local player's copy is a separate object under the arm pivot, wearing the picked skin
  copied off the world object.

`CarryTests` now covers all three: a slipper drifting from the hand while its carrier walks and
animates, a slipper left behind when the anchor is destroyed outright, and an empty viewmodel
hand after a pickup.

⚠️ **And writing those tests found a fourth thing, which was a TRAP rather than a live bug —
worth being precise about.** `Slipper.HostGrab` set `State`, `Holder` and
`motor.HoldingSlipper` and stopped, leaving `Carrier.Held` for the caller to fill in. All three
shipped callers already did that on the next line, so nothing was broken in the build; but a
grab that skipped it produced a slipper that believed it was held, a motor that reported
holding one, and a carrier with nothing in hand — after which the carry never ran and the
viewmodel stayed empty, because both read `Held`. `slipper.gd` owns this relationship for that
reason and says so: *"Two writers of the same relationship is how it ends up half-cleared."*
The port had two. It has one now, and `NotifyHolding` is idempotent so the existing call sites
are unaffected.

### Checked against the .gd and CORRECT, so they were left alone

Written down because re-fixing a correct thing is how a port acquires a bug.

- **The tag requires the lata standing.** `_sweep_lunge_tag` returns early on a downed can
  (*"a tag requires the lata standing, exactly as the proximity tag did"*), and
  `RoundDirector.ResolveTag` asks the same question. It reads like an invented extra condition
  and is not.
- **The passive tick drains by subtraction, not by zeroing.** `while (accum >= 1.0) accum -= 1.0`
  on both sides, which the .gd notes is the difference between 90 ticks a round and 89.
- **`Rounds` is 4, `PlayerCount` 4, `RoundTime` 90, `IntermissionDuration` 3.0**, and the four
  score values 100 / 50 / 100 / 10 — all matching `match_manager.gd` and `round_manager.gd`.
- **`SabotageWindow` 2.5 and `ThrowRestoreCooldown` 1.25** match, as does the restore cooldown
  being armed by the RESTORE rather than by the knock.
- **The defender-slot derivation** `(max(1, round) - 1) % 4` is the same pure function on both
  sides, never an accumulated counter.

### ⚠️ ONE DELIBERATE DIVERGENCE, AND IT IS A DESIGN CALL RATHER THAN A BUG

`IsTaggable()` asks `RoundActive`; `character_base.gd::is_taggable()` asks `can_act()`, whose
second half is "not stunned". The port changed it on the reading that 🧑 2026-08-06 —
*"a player that has been sabotaged by a player cannot be tagged by the defender. when the
attacker is in a frozen state, it cannot be tagged"* — was a bug REPORT rather than a rule.
That reading also unblocks the 50-point sabotage award, which is otherwise unreachable in both
trees: the shove that earns the credit is the same event that makes the victim refuse the tag
that pays it. **Godot still has `can_act()`.** If the sentence was a rule and not a report, this
is the one line to put back.

## 2026-08-16 — the arm, the wind-up, and two things this pass got wrong first

🧑, after spotting three of these in a row from screenshots: *"theres details like this man that
u missed — can u thoroughly find them"*, and *"make sure my arm moves or does an animation when
i interact with objects like in the real game — raise can, tag someone, etc"*.

### § THE FIRST-PERSON ARM NEVER MOVED FOR ANY VERB

`ViewmodelArms.tscn` ships an `AnimationPlayer` with **three** clips — `idle`, `throw` (0.46 s)
and `grab` (0.40 s) — and the port had only the idle breathe. So throwing, picking up, righting
the can, shoving, punching and lunging all happened with a completely motionless arm in the
corner of the frame, while the third-person body animated correctly for every one of them. The
person performing the gesture was the only one who could not see it.

Both clips are ported at the .tscn's own keyframes. And the second half matters as much:

⚠️⚠️ **A PROCEDURAL KICK FOR EVERY VERB THE ARMS HAVE NO CLIP FOR** — the punch, the shove and
the lunge. In first person the body is `ShadowsOnly`, so those three had NO first-person feedback
at all. The .gd's own note: *"you pressed shove and the screen did not move"*, written for 🧑
2026-08-01: *"add visual cue for first person and for everyone else that shove and sunok and
other skills and abilities shit is happening"*.

⚠️ **AND IT IS DRIVEN FROM EXACTLY ONE CALL SITE, WHICH THIS PASS GOT WRONG ON THE FIRST TRY.**
`character_visual.gd::play_action` opens with `rig.play_viewmodel_action(kind)` *"so the two
views can never disagree about whether a throw happened"*. The first attempt here scattered the
call across `Carrier` and `CombatVerbs` instead — six sites, each free to be forgotten by the
next verb. It now sits at the top of `CharacterAnimator.PlayAction`, before the clip lookup for
the same reason the .gd puts it there: a Prop has no animator, and resolving the body clip first
would return before the rig is told. `DeadFeatureAudit` asserts there is still exactly one caller.

### § THE WIND-UP POSE DID NOT EXIST IN EITHER VIEW

The 2.5 s throw charge, the taya's 0.5 s lunge and the shove all wound up with a motionless arm,
so the commitment the whole counterplay design hangs on was invisible. `Design.md` §4 requires
the wind-up to be *"visible on **every peer** ... so the attacker can dash, jump or throw through
the commitment"*, and §11 answers the charged melee with exactly *"1.35 s of visible wind-up"*.

The .gd carries **two** separate reports about this, both fixed there and neither ported:
*"no hand animation kapag nag tag ka as defender"* and *"is that on purpose theres no taya
animation? can u make sure theres an animation or atleast a hand movement for all movements"*.

`ViewmodelArms.SetCharge` is the first-person half, at the .gd's `VIEWMODEL_WINDUP_RAD` of 0.62
rad — chosen because *"the HUD charge meter is on the YOU card at the bottom corner, which nobody
looks at while aiming"*. It is polled, not evented, and it reads three sources in the .gd's order:
the throw charge, then the lunge. That order is load-bearing — the throw branch requires
something in hand, and **a taya holds nothing**, which is how *"the attacker got an arm; the
defender got a statue"* happened twice upstream.

⚠️ **STILL MISSING: the THIRD-PERSON half.** `character_visual.gd::_drive_charge_pose` writes the
same wind-up onto the `arm-right` bone so OPPONENTS can read it, which is the half the counterplay
actually depends on. It needs skeleton access on a SkinnedMeshRenderer and is the next item.

### Two corrections, because both were stated confidently and both were wrong

1. **"Most SFX never play" was FALSE.** It came from a `grep` truncated at 70 results. Checked
   exhaustively afterwards: **all 37 live cues had a call site and a .wav before this session
   touched anything.** Three cues were then "fixed" on that bad reading — `lata_knockdown`,
   `reset_complete` and `reset_channel_start` — and every one of them was a DUPLICATE that would
   have double-played the game's loudest moment. `Lata.SetUpright` already sounds both directions
   off one boolean, deliberately: *"one state change read two ways, rather than two call sites
   free to drift apart."* All three additions were removed.

2. **The floating slipper fix broke the pickup.** Resting a slipper on the ground needs a
   downward raycast, and every slipper starts at its owner's FEET — so the first thing the cast
   met was the owner's own capsule and the tsinelas was placed on their head, out of its own
   pickup radius. `AnyAttackerCanPickUpAnySlipper` failed immediately. The cast skips bodies,
   slippers and the can now.

⚠️ **The lesson both share is the ledger's oldest one, in a new place: MEASURE, and then measure
what the fix did.** A truncated grep and an unqualified raycast are the same mistake — trusting a
tool's answer without asking what it actually looked at.

### The floating slipper, and the floor that is not at zero

🧑: *"also ur slippers are floating"*. `Land()` clamped to `Mathf.Max(p.y, RestHeight)` and the
flight ended at `position.y <= SlipperRestHeight` — both absolute, and **neither arena's floor is
at y = 0**. The .gd's `REST_HEIGHT` is a height ABOVE THE GROUND, so using it as a world y is
correct only by accident. Both now measure the floor underneath, as does the round-start
placement, which is the other half — a slipper that is never thrown never lands.

### `FppFrameProbe` — what the camera can actually see

Written to answer *"look the hands are floating this still isnt good"* by NAMING the mesh instead
of guessing from pixels. It dumps every renderer in front of the FPP camera with its full
hierarchy path, viewport position, world size and shadow mode, to `Logs/fpp-frame.txt`.

It answered in one run: the two arms are correct and symmetric (viewport x 0.31 and 0.68), the
seats are at exactly the authored 1.80 m spacing, and the pale slab in the corner is **Seat 2's
body**, 0.4 m from the eye with its bounds centre a full screen-width off the left edge.

⚠️ **AND A SKINNED RENDERER'S `bounds` ARE ITS BIND POSE.** These rigs bind arms-out, so
`body-mesh` measures 1.90 m wide against a 1.80 m spawn spacing — every seat's box overlaps its
neighbour's whatever the animation is doing. `TestPlanesAABB` against that box says "possibly
visible" for a body drawn well outside the frame, so the frustum test alone proves nothing about
what a player sees. The probe's first assertion did not know that and flagged the road.

## ⚠️ Unity rules the conversion has to obey, each found by a shipped failure

These are not style. Each one produced a build that looked correct in the editor
and was broken for a player, which is the most expensive kind of bug this port
can produce.

1. **One MonoBehaviour per file, named after the class.** Unity only makes a
   MonoScript asset for the class matching the FILE name. A second MonoBehaviour
   in the same file serialises into a scene as an embedded `--- !u!115` stub, and
   the player then reports `The file 'levelN' is corrupted!` and dies on load.
   Every converted screen carried four to eleven of those. `TscnUiImporter`
   checks this now and fails the import rather than writing such a scene.
2. **A scene may only reference assets on disk.** `Sprite.Create` at import time
   produces an object the scene does not own; same crash. Style boxes and scrim
   gradients are baked to PNG (`StyleBoxBaker`, `BakeGradient`).
3. **`AddComponent` cannot carry an inspector reference.** Anything a
   code-installed component needs must be loaded by the component itself. This is
   what left `PlayerInputReader` with no action asset in every build, so the
   human seat could not move.
4. **A renderer created in code has no material** and draws as a magenta error
   blob, and any MaterialPropertyBlock written at it is discarded.
5. **A property block writes a NAMED property**, silently doing nothing when the
   shader has none. `EnvColourPass` reported 418 of 434 renderers repainted while
   changing nothing on screen. Tint through a material variant and check
   `HasProperty`.
6. **An asset nothing references is stripped from the build.** The 32 animation
   clips per character live inside the `.glb`; without a serialised reference the
   whole cast stood still in the player only.
7. **A surface shader declares `_MainTex_ST` for you.** Naming an Input field
   `uv_MainTex` makes the generator emit that declaration, and a second one is a
   hard "redefinition" error that kills ONLY the lit pass. A hand-written outline
   Pass in the same shader still compiles, so the model draws a perfect ink
   silhouette filled with the error shader: a solid navy blob in a build, with no
   error anywhere except the shader import log.
8. **A generic Animator with no Avatar plays nothing and says nothing.** glTFast
   emits an Animator with a null controller (correct for Playables) and no Avatar,
   and an `AnimationPlayableOutput` bound to one drives no transforms at all.
   `ModelPreview.EnsureAvatar` builds one with `AvatarBuilder.BuildGenericAvatar`.
   The symptom is the whole cast standing in its bind pose, which on these rigs is
   arms out, and it reads as unfinished art rather than as a bug.
9. **A shader only `Shader.Find` reaches is stripped from the player.**
   `TumbangPreso/Toon` is built into materials at runtime and is named in
   `GameBuilder.EnsureRuntimeShaders` for exactly that reason.

## How to read the status column

- **CONVERTED** — ported function-by-function against the .gd, behaviour verified
- **PARTIAL** — a file exists and compiles, but does not do everything the .gd does
- **MISSING** — no counterpart exists at all

Line counts are given for both sides. A large gap on a PARTIAL row is the honest
size of the remaining work. Ratios are a smell test, not a spec: `character_roster.gd`
is CONVERTED at half the lines because GDScript dictionaries became typed records.

## ⚠️ The camera directive — read before touching any camera

A previous session recorded "the game is first person, TPP was a mistake." **That
is wrong and must not be acted on.** `camera_rig.gd` has FOUR third-person cases:

1. **Prop is always TPP.** `camera_rig.gd:5` — "Person is ALWAYS first-person,
   Prop (Can/Slipper) is ALWAYS third-person." The mode is derived from
   `_character.is_person` and is asserted; nothing else may write `_mode`.
2. **Emote view** (`camera_rig.gd:425`). A Person swings to TPP for the duration
   of an emote and returns to FPP. The emote camera ORBITS, it does not steer —
   mouse moves the camera around the body, never the body itself. Pitch clamps
   to -35/+20, separate from the gameplay clamp. **Local only**: the emote is
   replicated, the camera swing is not, or every peer would spin when one
   player danced.
3. **Carried-prop follow** (`_update_tpp_carry_follow`) — ⚠️ **VESTIGIAL, DO NOT
   PORT AS LIVE BEHAVIOUR.** An earlier note here (mine, 2026-08-15) described it
   as a working fourth case. It is not: `camera_rig.gd:750` declares
   `var carrier: CharacterBase = null` and nothing ever assigns it, so every
   branch below that line is unreachable in the shipped build. It dates from when
   props were playable units; §12 deleted playable props and left the function
   behind. The constants (`TPP_CARRY_MOUNT_HEIGHT = 0.6`, the -15° base tilt) are
   real but nothing reads them. Port it only if playable props ever come back.
4. **Spectator** — separate rig, see `spectator_camera.gd` below.

The real earlier mistake was narrower: an *overhead follow* camera was built that
matched none of these. Fix that framing, do not delete TPP.

## 2026-08-18: the player-facing parity pass, and the stale baseline it found

Audited file by file against Godot `main` @ `e353a54` (2026-08-07) rather than from this
ledger, and the first finding was about the ledger itself.

### ⚠️⚠️ THE SCOPE LINE AT THE TOP WAS MEASURED AGAINST AN OLDER TREE

It records 31,314 lines across 45 scripts. Godot `main` is **32,215 across 46**, and the
901-line difference is not spread evenly: it is concentrated in exactly the five files
that took the last feature wave, between 2026-08-01 and 2026-08-07.

| file | ledger | main | delta |
|---|---|---|---|
| `character_visual.gd` | 2182 | 2494 | +312 |
| `slipper.gd` | 1630 | 1881 | +251 |
| `settings_manager.gd` | 703 | 810 | +107 |
| `settings_panel.gd` | 429 | 508 | +79 |
| `hud.gd` | 1587 | 1661 | +74 |

Every gap this pass converted lives in those five files, which is why none of them
appeared as a row here. **Re-measure the baseline before trusting a MISSING/PARTIAL
column again**, and note that neither of the other two Godot clones on the Mac is safe to
diff against: `DOST-PRESENTATION` has its comments stripped, and the copy under
`Documents/GitHub` predates `slipper.gd` existing at all.

### Converted this pass

1. **§ THE LANDED HIGHLIGHT.** A thrown tsinelas that comes to rest is outlined in a
   colour the player picks, for the whole time it lies loose, on every peer. Plus its
   five-row Settings picker and the live repaint that makes the control honest from the
   in-match pause menu.
2. **The owner glow was ported from the approach Godot REJECTED**, and is fixed here. It
   wrote `_EmissionColor`, which `TumbangPreso/Toon` does not have, and never wrote
   `_RimColor` at all, so it lit in the shader's default peach rather than the gold
   `OWNER_RIM_COLOR`. See `Slipper.RefreshHighlight`.
3. **DANCE replaced PLAY DEAD**, wheel entry and clip both. The clip is GENERATED at bind
   time (`DanceClip.cs`) because the rig has seven bones and nothing retargetable exists
   for it, which is the same conclusion Godot reached.
4. **The pennant buttons' inner-stroke hover rim** (`button_outline.gdshader`).
5. **The result card's turned-up corner** (`card_fold.gdshader`).

### ⚠️ AND ONE REPORTED GAP THAT IS NOT ONE. DO NOT "FIX" IT.

`character_visual.gd::_refresh_downed_tilt` (78° over 0.28 s) looks unported, and
`CharacterVisual.cs` even carries a comment saying the downed tilt is somebody else's to
write. **Both are correct and nothing is missing.** That function early-outs unless
`_character.is_can`, i.e. unless the unit IS a playable can, and §12 deleted playable
props. It is vestigial in the same way the carry-follow camera above is. The REAL topple
is `lata.gd::DOWNED_TILT_DEG = 88.0`, which `Lata.cs` already ports through
`Balance.DownedTiltDeg`. The two angles differ because they are two different features,
not because one drifted.

## 2026-08-19: the networking layer audit (N0)

Reconciled the networking architecture against Netcode for GameObjects 2.13.1, Unity Transport
6.5.0, and Unity Gaming Services (Multiplay Hosting, Lobby, Relay, Authentication). The existing
C# networking layer (~1,690 lines) is confirmed functional and structured into clean, tested
components rather than greenfield:

1. **`LobbySession.cs` (337 lines):** Transport-agnostic lobby state. Manages seats, stable
   identity tokens, leader election, and confusable-free 4-character join codes (excluding 0/O,
   1/I/L). Implements the four mid-match arrival rulings (Seat, Reclaim, Spectate, Refuse) in
   strict priority order. Vacated seats are held for disconnected tokens rather than freed.
   Dedicated server peer 1 is enforced as a referee that never leads. Human peer count
   (`PlayingPeerCount`) governs ready gates without counting bot placeholders.
2. **`NetSession.cs` (230 lines):** NGO 2.13.1 and UnityTransport adapter implementing
   `INetProvider`. Manages listening lifecycle, connection events, status broadcasting, and 30s
   disconnect timeout. Scene management is deliberately disabled (`EnableSceneManagement = false`)
   to prevent races with `SceneFlow`. Automatically restores `SoloProvider` on disconnect.
3. **`MatchRpc.cs` (300 lines):** Explicit `NetworkBehaviour` RPC layer using `[ServerRpc]` and
   `[ClientRpc]`. Implements the request, resolve, and broadcast triplet for tag verbs (punch,
   lunge, shove), separate wind-up visual charge broadcasts, grab eligibility, throw execution,
   can reset, emote synchronization, ready declarations, full picks table replication
   (`SyncPicksClientRpc`), and late-join hooks.
4. **`LanBeacon.cs` (238 lines):** Standalone UDP broadcast discovery on port 8911 using the
   `tumbang-preso-lan` magic string to match Godot wire compatibility. 4-second timeout tolerates
   intermittent packet loss.
5. **`ServerQuery.cs` (234 lines):** Transitioning from legacy VPS pool unicast to UGS Lobby query
   while preserving LAN-first join code resolution.
6. **`NetBootstrap.cs` + `NetBootstrapRunner.cs` (176 lines):** CLI startup switches (`-tp-host`,
   `-tp-dedicated`, `-tp-join`, `-tp-map`, `-tp-profile`) for automated multi-process testing and
   headless Linux dedicated server startup before scene load.
7. **`NetAuthority.cs` (104 lines):** Core gameplay seam providing `ShouldResolve()` and
   `ShouldRequest()` guards across combat, scoring, and cans. Ensures clients submit intent only
   while the host resolves outcomes and writes scores.
8. **`NetIdentity.cs` (160 lines, N1):** Player identity management bridging UGS Authentication
   online and stable minted tokens offline. Implements profile switching (via `-tp-profile`) to
   prevent multi-instance session/seat collisions on the same machine.

## Autoload singletons (9)

Godot autoloads are always-on globals. Unity has no equivalent; these become
`GameServices` entries or `RuntimeInitializeOnLoad` singletons. All 9 must exist.

| Godot autoload | Lines | Unity | Status |
|---|---|---|---|
| `audio_manager.gd` | 1125 | `AudioDirector` + `AudioCues` + `MusicDirector` (382) | PARTIAL |
| `round_manager.gd` | 476 | `RoundDirector.cs` (219) | PARTIAL |
| `match_manager.gd` | 217 | `MatchDirector.cs` (97) | PARTIAL |
| `network_manager.gd` | 1413 | `NetSession` + `LobbySession` + `MatchRpc` + `NetAuthority` + `NetBootstrap` + `NetIdentity` (1407) | PARTIAL: N0-N2 complete (NGO+UGS locked, NetIdentity profile/token isolation wired, LanBeacon converted); N3 Relay pending |
| `lan_beacon.gd` | 323 | `LanBeacon.cs` (270) | CONVERTED (N2): multi-interface subnet broadcast via NetworkInterface, signature change events, and joinable/fill sorting |
| `server_query.gd` | 536 | `ServerQuery.cs` (215) | PARTIAL: legacy VPS pool query transitioning to UGS Lobby; LAN-first code resolution preserved |
| `game_launch.gd` | 301 | `GameLaunch.cs` (108) | CONVERTED: map registry, pending action, seating |
| `settings_manager.gd` | 810 | `GameSettings` + `Rebinding` + `SlipperHighlights` (470) | CONVERTED. Sliders, rebinding, applied on load, **§ the landed-highlight palette and its change signal** |
| `debug_player_switcher.gd` | 420 | `DebugPlayerSwitcher.cs` (115) | PARTIAL: seat drive, cycle, readout |

## Characters and objects

| Godot | Lines | Unity | Status |
|---|---|---|---|
| `character_base.gd` | 1981 | `CharacterMotor` + `CombatVerbs` + `StatusStack` (651) | PARTIAL |
| `character_visual.gd` | 2182 | `CharacterVisual` + `CharacterAnimator` + `ImpactBurst` + `ToonSkin` (900) | CONVERTED — clips, flash, burst, toon pass, ink outline, palette remap, measured hand attachment, remote smoothing, **§ the stun frost's body half** |
| `carrier.gd` | 536 | `Carrier.cs` (350) | CONVERTED 2026-08-16 — the 2.5 s wind-up on `Pressed` not `JustPressed`, its sound, the OBSERVED charge every peer can see, the aim cast from the camera, the sight-line throw origin, and the arc |
| `character_nameplate.gd` | 165 | `CharacterNameplate.cs` (155) | CONVERTED — ring, tag, role colour, distance fade |
| `slipper.gd` | 1881 | `Slipper.cs` (400) | PARTIAL. Flight, bounce, spin, void recovery, **§ the landed highlight and the owner glow, both on the real rim and outline**; hand attach and net sync pending |
| `lata.gd` | 534 | `Lata.cs` (175) | PARTIAL — topple, roll, hit window; skins and net sync pending |

## Systems

| Godot | Lines | Unity | Status |
|---|---|---|---|
| `main.gd` | 3595 | `MatchHost.cs` + `MatchInstaller` (370) | PARTIAL — local half; netcode half pending |
| `ai_controller.gd` | 2225 | `AIController` + `AiTuning` + `AiPersonalityRoll` (900) | PARTIAL — tiers, personalities, 13-plan machine, unstick, lane sampling, intercept prediction; per-plan polish pending |
| `camera_rig.gd` | 1111 | `CameraRig` + `ViewmodelArms` (640) | CONVERTED — FPP, prop TPP, emote swing, arms; carry-follow is dead code upstream |
| `spectator_camera.gd` | 431 | `SpectatorCamera.cs` (431) | CONVERTED — call sites pending, see below |
| `character_roster.gd` | 757 | `Roster` + `RosterBook` (411) | CONVERTED (20/20 validated) |
| `env_toon_pass.gd` | 391 | `EnvColourPass.cs` (400) | CONVERTED 2026-08-16 — tints, foliage, laundry sway, **the six roof atlases**, name-hash seeding and the building/car/tree classification |
| `trajectory_preview.gd` | 273 | `TrajectoryPreview.cs` (330) | CONVERTED 2026-08-16 — camera-facing ribbon, both fades, landing mark, physics-tick integration; **and it is finally instantiated** |
| `hazard_zone.gd` | 133 | `HazardZone.cs` (108) | CONVERTED — slow zone, visual disc, round-scoped lifetime |
| `game_version.gd` | 56 | `GameVersion.cs` (80) | CONVERTED — reads `Application.version`, now 4.68; the in-match stamp is bound too |
| `kill_plane.gd` | 26 | `KillPlane.cs` (95) | CONVERTED 2026-08-16 — the real transform (y -10, 260x4x260) read off the MAP, and the importer binds it |

### `spectator_camera.gd` — CONVERTED 2026-08-15, audited line by line

All 19 behaviours of the .gd are present in `SpectatorCamera.cs`, checked against the
source after writing rather than from memory: the three speed constants and their
two human-instruction retunes, the ±88 pitch limit, position smoothing at 14.0 with
rotation deliberately unsmoothed, follow distance and its 0.34 lift ratio, both POV
eye heights and the 0.34 forward offset, FOV 78 / far 400, the wheel meaning two
different things in two modes, Tab / F / V, the every-frame view re-claim, the
free-flight vector with jump and `SpectatorDown`, the follow-list rebuild with its
fallback scan, `ControlsText()`, `StatusText()`, and the legend's role suffix.

Three things changed on purpose, each commented in the file:

- **Start position Z is negated** — `(0, 9, 14)` in Godot is `(0, 9, -14)` here. Same
  handedness flip the map conversion uses. Left alone, the mode opens looking at an
  empty street with the match behind the camera.
- **Pitch signs are negated** — Godot's `rotation.x` is positive looking up, Unity's
  euler X is positive looking down. The .gd's `-26` start is `+26` here.
- **Yaw adds instead of subtracts** — same flip; copying the sign would invert
  mouse-look for spectators only.

Held invariants: it is a plain Transform with a Camera and no collider, so clipping
is structural; it reads hardware directly and never an `InputIntent`, which is what
keeps a bot from flying it now that the AI writes intents exclusively.

**Still pending — its call sites, all of which live in the unported `main.gd`:**
seat -1 / no character spawned, exclusion from the ready gate, the placeholder-AI
fill for the vacated slot, the HUD's spectator branch polling `StatusText()`, and
the on-screen legend. The camera itself is done; nothing selects it yet.

Registration into the followable set was moved onto `CharacterMotor.OnEnable`
rather than waiting for `main.gd`, so Tab works as soon as units exist.

### `spectator_camera.gd` — the control set, for reference

Free-fly camera with three modes: free, follow, POV. Every constant here is from
the .gd, transcribe them rather than re-tuning:

- `BASE_SPEED 3.6`, boost ×`2.5` on `sprint`, speed steps ×`1.35` clamped `1.2`–`40.0`
- Pitch limit `88°`, move smoothing rate `14.0`
- Follow distance `6.5`, clamped `1.2`–`30.0`, lift ratio `0.34`
- POV eye height `1.45` Person / `0.42` Prop, forward offset `0.34`
- `Tab` cycles the follow target, `V` toggles POV — both read in `_input`, NOT
  `_unhandled_input`, because the HUD is a live CanvasLayer that eats Tab first
  (`spectator_camera.gd:233` explains this at length — read it before rewiring)
- Vertical movement uses `jump` up / `spectator_down` down. **Not** `guard_dash`,
  which no longer exists and threw every frame until it was fixed.
- `status_text()` drives the spectator's on-screen readout

## UI (scripts/ui — 21 files)

`.tscn` LAYOUTS for 11 screens are converted (see `TscnUiImporter`). Behaviour is
a separate job, tracked here. A converted layout with no script bound is PARTIAL.

| Godot | Lines | Unity | Status |
|---|---|---|---|
| `match_setup.gd` | 2015 | `ConvertedMatchSetup.cs` (440) | PARTIAL — rows, seats, spectate, live map preview; netcode lobby half pending |
| `hud.gd` | 1587 | `Hud.cs` (900) | CONVERTED 2026-08-16 — wood skin, recessed clock with its urgency colour and pulse, ranked four-row board with the TAYA badge, lata card and its per-role hint, toasts off all five events, ready prompt AND role objective, split status stacks, held danger vignette, VULNERABLE line, role-coloured crosshair, clean feed, spectator strip, version stamp, **§ the stun frost's screen half**, and a clock card that is 97 tall like the original's |
| `multiplayer_setup.gd` | 1015 | `LobbySession.cs` (287) | PARTIAL |
| `character_preview.gd` | 623 | `ModelPreview.cs` (470) + `ModelPreviewInput.cs` | CONVERTED — aspect-correct target, three-term framing, pitch lerp, h_offset, idle clip, drag/zoom/reset, tile framing |
| `ui_theme.gd` | 551 | `UiTheme` + `GodotTheme` + `StyleBoxBaker` (520) | CONVERTED — variations, StyleBox geometry and the baked nine-slices |
| `tutorial.gd` | 462 | `TutorialContent` + `ConvertedTutorialPanel` (350) | CONVERTED 2026-08-16 — all 8 pages, plus page 1's premise strip with the four real models in live 3D |
| `you_card.gd` | 430 | `YouCard.cs` (380) | CONVERTED 2026-08-16 — wood face with the role border, the STAMINA bar it was missing entirely, the FATIGUED read, the ready flash, the meters gated on ACTIVITY as well as role, the taya's LUNGE meter, and the .tscn's own 132/32/34/26/160/10/6 geometry |
| `settings_panel.gd` | 508 | `SettingsPanel` + `Rebinding.cs` (410) | CONVERTED. Rebinding, conflicts, reset, **the landed-tsinelas colour row** |
| `emote_wheel.gd` | 425 | `EmoteWheel.cs` (215) + `Emotes.cs` | CONVERTED. Hold, steer, release, **DANCE in place of PLAY DEAD** |
| `character_select.gd` | 341 | `CharacterSelectScreen` (200) | CONVERTED — tabs, chalk pips, live 3D |
| `match_result.gd` | 339 | `MatchResult.cs` (310) | PARTIAL. Board, single-player rematch, **the card's turned-up corner**; peer vote pending netcode |
| `credits_panel.gd` | 292 | `CreditsContent.cs` + `CreditsPanel` (250) | CONVERTED — CC-BY strings verbatim |
| `role_swap_card.gd` | 274 | `RoleSwapCard.cs` (250) | CONVERTED — intermission timeline, swap, standings |
| `arrow_button.gd` | 262 | `ArrowButtonView.cs` (320) | CONVERTED. Unfurl, hover, press, both cues, **the inner-stroke hover rim** |
| `offscreen_indicators.gd` | 211 | `OffscreenIndicators.cs` (175) | CONVERTED — edge arrows, wired into the HUD |
| `map_preview.gd` | 165 | `MapPreviewSurface.cs` (330) | CONVERTED 2026-08-16 — the registry's yaw/distance/height, the spawn-point pivot, the 7°/26 s sway, the parked-not-unloaded cache with its lights killed, and the silencing |
| `splash_screen.gd` | 107 | `SplashScreen.cs` (154) | CONVERTED |
| `mode_select.gd` | 96 | `ConvertedModeSelect.cs` | CONVERTED |
| `main_menu.gd` | 85 | `ConvertedMainMenu.cs` | CONVERTED — in-place overlays, pennants re-unfurl |
| `debug_bar.gd` | 47 | `DebugBar.cs` (110) | CONVERTED — deliberately unstyled |
| `pause_layer.gd` | 21 | `PauseWatcher` in `MatchInstaller.cs` | CONVERTED — see note |

## Scenes (27) — one row each, so an audit can check them

⚠️ Named individually on purpose. This section used to say "both maps and 11 UI
screens", which no script can verify and no reader can check against the source
tree. A collective count is how a missing scene hides.

| Godot scene | Status |
|---|---|
| `Eskinita.tscn` | CONVERTED — 416 objects, 0 missing, walls at ±8.60, light + fog + sky + colour pass |
| `BayanPlaza.tscn` | CONVERTED — 553 objects, 0 missing, light + fog + sky + colour pass |
| `SplashScreen.tscn` | CONVERTED |
| `MainMenu.tscn` | CONVERTED — real backdrop, logo, arrow buttons |
| `ModeSelect.tscn` | CONVERTED |
| `MatchSetup.tscn` | CONVERTED — rows, seats, spectate, live map behind |
| `MultiplayerSetup.tscn` | PARTIAL |
| `CharacterSelect.tscn` | CONVERTED — tabs, chalk pips, live 3D |
| `MatchResult.tscn` | CONVERTED — board rebuilt in code |
| `SettingsPanel.tscn` | CONVERTED — incl. rebinding |
| `CreditsPanel.tscn` | CONVERTED — CC-BY strings verbatim |
| `HUD.tscn` | PARTIAL — 35 nodes converted; the live HUD is still built in code, on the same theme |
| `ArrowButton.tscn` | CONVERTED — inlined per instance, `ArrowButtonView` drives it |
| `Tutorial.tscn` | CONVERTED — 8 pages; 3D props pending |
| `YouCard.tscn` | CONVERTED |
| `RoleSwapCard.tscn` | CONVERTED |
| `OffscreenIndicators.tscn` | CONVERTED |
| `DebugBar.tscn` | CONVERTED |
| `ViewmodelArms.tscn` | CONVERTED — baked transforms carried across |
| `CameraRig.tscn` | **MISSING** — ⚠️ baked transforms; read `camera_rig.gd:21` first |
| `CharacterBase.tscn` | **MISSING** — built in code by `MatchInstaller` instead |
| `CanVisual.tscn` | **MISSING** |
| `TsinelasVisual.tscn` | **MISSING** |
| `Lata.tscn` | **MISSING** — built in code |
| `Slipper.tscn` | **MISSING** — built in code |
| `Main.tscn` | **MISSING** — holds the kill plane's real transform |
| `PremiseIcon.tscn` | **MISSING** — the tutorial's 3D props |

### `pause_layer.gd` — the one file the port does not need

Its entire 21 lines exist to solve a Godot-specific problem: a node at the default
`PROCESS_MODE_INHERIT` stops receiving `_unhandled_input` once the tree is paused,
including the very Esc press meant to resume. Godot's fix was a dedicated
`PROCESS_MODE_ALWAYS` CanvasLayer whose only job is to survive the pause it causes.

Unity's `Update` is frame-driven and keeps running at `timeScale = 0`, so
`PauseWatcher` reads Escape while paused without any equivalent trick. Marked
CONVERTED rather than dropped, because "we do not need this" is a claim that has to
be written down and checked, not assumed.

## Bot difficulty — the tiers landed 2026-08-15, the plan machine has not

`AiTuning.cs` in the engine-free core holds all three tiers (Bata / Normal / Astig)
with every one of their 17 tuning values, plus the 36 tier-independent geometry and
cadence constants. 7 tests assert them against `ai_controller.gd`.

**Three real divergences were found and fixed, not cosmetic ones:**

- `ArriveSlop` was **0.35**; the .gd has **0.55**. The tighter value makes bots jitter
  on arrival rather than settle on a mark.
- Lunging was gated on `tier != Easy`, so **Bata and Astig lunged identically**. It is
  now range (1.9 / 2.6 / 3.1) AND cone — a half-angle where smaller is stricter, so
  Astig's 28° is the disciplined one and Bata's 55° the wild one.
- Sprinting was gated on `tier == Hard`, so **Normal never sprinted at all**. It is now
  distance past 5.0 m and a stamina reserve the tier holds back (Bata spends
  everything, Astig keeps 0.45 for a chase that matters).

⚠️ **The saved difficulty was being ignored entirely.** The settings panel wrote
`AiDifficulty` and nothing ever read it back, so every bot in every match played at
Normal regardless of what the player picked. `MatchInstaller` now applies it.

**Still missing:** the plan state machine itself — lane sampling, intercept
prediction, stalk patience, stuck detection and unsticking, slipper claim TTL, the
loiter walk, and the deliberate mistake roll. That is the bulk of the 2,225 lines.
The numbers are now in place for it to be written against.

## Ready-up phase — CONVERTED 2026-08-15 (local half)

`ReadyGate.cs`, from the ready-phase half of `main.gd` (~lines 1036-1195). Free-roam
window, "Press [R] when you're ready", the ready gesture other players can see, then
3 · 2 · 1 · GO! at 1.0 s a tick and 0.5 s on GO. The round begins when the countdown
finishes, never on the press, and `_countingDown` stops a second press restarting it.

`SliceRunner.AutoStart` is now off whenever the gate is used, or the round would begin
underneath the countdown. Headless probes set `MatchInstaller.UseReadyGate = false`,
because nobody is there to press R.

⚠️ **The networked half is NOT ported.** Godot's host counts one press per connected
human PEER — never per character, because a 2v2 always has four characters and an AI
cannot press R, so counting characters leaves a solo host waiting forever for three
bots to agree. Spectators are excluded for the same reason. That needs
`NetworkManager.playing_peer_count()`, which is unported. **Do not approximate it by
counting characters.**

## Input actions (14) — all must exist in the Input System asset

`move_left` `move_right` `move_up` `move_down` `jump` `sprint` `grab` `lunge`
`special_ability` `emote_wheel` `ready_up` `spectator_down` `clean_feed`
`toggle_fullscreen`

`ready_up` is the missing ready-up phase. `spectator_down` is spectator descent.
`clean_feed` hides HUD for capture. None of these may be dropped — each is
rebindable through `settings_panel.gd`, and `tools/input_probe.gd` checks them
for conflicts.

## Constant audit — run it again after every balance change

Every `const` in `character_base.gd`, `slipper.gd`, `lata.gd` and `round_manager.gd`
was extracted and compared against every `const` in the Unity runtime on 2026-08-15,
by name (snake → Pascal) and by value.

**Result: 77 constants on each side, and ZERO value mismatches.** The balance layer
is faithful. That is the single most reassuring measurement taken in this port so far,
because it is the layer that cannot be verified by looking at a screenshot.

Twelve Godot constants had no Unity counterpart. All twelve are now in `Balance.cs`
with their original reasoning: `BounceRestitution`, `MinPowerScale`,
`SlipperRestHeight`, `SlipperSpinSpeedDeg`, `SlipperTumbleSpeedDeg`,
`SlipperModelLength`, `VoidY`, `OwnerRimStrength`, `HitstopDuration`,
`HitstopTimeScale`, `LandSfxMinSpeed`, `SlipperSyncInterval`.

⚠️ **The numbers landing is not the feature landing.** Nothing reads most of them yet
— slipper flight, hitstop and the owner rim glow are all still PARTIAL rows above.
They are transcribed first so the port cannot quietly re-derive a number by taste.

⚠️ Two earlier passes of this audit gave WRONG answers and both are worth knowing.
Matching by value alone reports `BounceRestitution` as present because `LungeActiveTime`
happens to also be 0.45. Scanning only the Core package reports `PerchNormalMin` as
missing when it is a private const in `CharacterMotor`. Match on name suffix across the
whole runtime.

## Rules core — the one part that is genuinely done

`Packages/com.tumbangpreso.core/` — engine-free C#, 32 tests green. Every constant
transcribed from the .gd, NOT from `Design.md` (which has drifted; see
`Design_Drift_Report.md` — all 4 discrepancies were stale prose, the code is right).
