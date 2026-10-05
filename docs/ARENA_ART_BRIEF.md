# The arena: art brief for the kits (ARENA-1.4)

Written 2026-10-05. The gameplay design is in [ARENA_MAP_BRIEF](ARENA_MAP_BRIEF.md); this is what the
map LOOKS like and how its kits are built. Every kit author reads this whole page first.

## What the owner asked for (verbatim, in order)

- "i want a large arena similar to a soccer or football or baseball stadium. i need the map large."
- "i really like the scale and concept of the skyway stadium in The Finals" (a stadium whose middle
  holds a slice of world, built out of holograms).
- "too cramped. the seats and central area should be more open"; "there better be a good reason for
  why theres random floating cubes and rectangles".
- "less of that blue pink and yellow stuff. more bluelock/rocketleague stadium in aesthetic"; then,
  of a football-pitch redraw: "no, i liked the previous layout, the bluelock/rocket league stuff was
  for the color scheme and additional style references for how i wanted it to feel.. night time".
- "wanna incorporate our logo too".
- "for the crowd i was thinking of 2d animated sprites/gifs of people/generic character models we
  have".
- "idk if thats true z fighting but fix it. the model looks unoptimized and it seems like connected
  pieces are just multiple rectangular prisms placed next to each other than actually being single
  connectedmeshes."
- "concept is floating arena in the sky with cyberpunk-esque aesthetic."
- "buildings will need actual more detail. and you'll need to fix disconnected parts".
- "need you to be more critical of the work."
- "the buildings arent really visible from the stadium. i need you to think more about sightlines.
  and i assume you'll follow the same artistic/illustrated artstyle for the textures correct? i dont
  want any buildings to look like that in the final design" (of placeholder window grids).
- "theres a few stretched out textures on the buildings."

## The concept

A STADIUM FLOATING IN THE NIGHT SKY OVER A NEON CITY. A round saucer hull carries a round stadium:
a hover stage over an open shaft in the middle of a turf field, a full lower bowl, four upper stands
with open corners, a canopy and floodlights over each, a centre-hung scoreboard, a big screen in
each corner. Through the corners and over the canopies stand the city's towers. The feel is a night
match in Blue Lock or Rocket League: dark navy stands, a glittering crowd, white floodlights and
their beams, blue and white LED ribbons, green turf, with cyberpunk accents (magenta and cyan neon,
holograms) used sparingly. It is the street game put on the biggest stage there is.

## The blockout and its numbers (do not move these)

`tools/author_arena_stadium.py` built the approved blockout (`ArtSource/arena/arena_stadium.blend`,
pictures `ArtSource/arena/arena_stadium_v17_*.png`). `tools/arena_kit.py` holds its numbers and mesh
helpers; import from it, do not copy numbers. Blender units are metres, z up, y north, THE CAN IS
THE ORIGIN (the game's origin too).

| Thing | Numbers |
|---|---|
| The shaft the stage hovers in | radius 40 (`PIT_R`), open to the city below |
| The field | turf from r 42 to 75, a track to 85, the LED barrier at r 80, 1.5 m tall; top at z 0 |
| Lower bowl | rows from r 85.3 (z 3.0) to r 106 (z 12.6), a cross walkway r 106 to 110, rows r 110.6 (z 15.4) to r 130 (z 26), concourse r 130 to 140 at z 26, outer wall r 141 |
| Upper stands | four, bearings -32..32, 58..122, 148..212, 238..302 (`UPPER_ARCS`); rows r 141 (z 32) to r 176 (z 58); outer wall r 180 to 181.2 |
| Canopies | one per upper stand, r 150 to 190 (`CANOPY`), underside z 64 to 68.4, top to z 71.2 |
| Scoreboard | hung over the can, z 41.6 to 52.6, radius 9.4 |
| Corner screens | bearings 45, 135, 225, 315 at r 152, 40 x 18 m, centre z 44 |
| Hull | deck at z -2 (`DECK_Z`) out to r 231, rim r 236 to 239, underside down to z -76, eight engines at r 176, four landing pads at r 254 |
| City floor | z -760 (`FOOT`) |
| Slipper flight ceiling | 12 m: nothing solid over the stage below z 30 |
| The play walls | invisible, about 22 m from the can. Players only ever stand on the stage. |

**Sightlines** (`rim_elevation` in `arena_kit.py`): from a player's eye by the can (z 3.3) the
stadium hides the sky up to about 23 degrees over a canopy and 18 degrees in a corner. Anything
outside the stadium that must be SEEN from the stage has to clear that. Check every kit from the
player's eye, not only from the air.

## Rules for every model (each one cost a redo on an earlier map)

1. **Real connected meshes.** A ring-shaped thing is one closed profile swept round (`lathe`).
   Rows, aisles, walls, ledges and light strips are faces of ONE surface told apart by material or
   UV, sharing vertices. No stacks of boxes. Every mesh that should be solid has zero open edges;
   run `report()` and fix what it flags.
2. **No two faces share a plane, anywhere.** A sign, a line, a panel or a decal is either faces cut
   into the surface or stands at least 5 cm proud and is the only face in its plane. No z-fighting
   in Blender's viewport or in Unity.
3. **Nothing floats and nothing stops short.** Every strut, mast, cable, bracket and pipe is built
   between the two things it joins and ends INSIDE both.
4. **Detail is how the thing is built, not noise.** A tower has a base, a body with bays, setbacks,
   a crown, plant on the roof. A stand has seats, steps, rails, doors. Model the big and medium
   forms; paint the small ones.
5. **Variety by construction.** Two neighbours differ in how they are built, not only in colour.
6. **Budget.** The whole map should stay near 600,000 triangles with real LODs, and few materials:
   each kit shares ONE small set of materials across all its objects (aim for 6 to 12 per kit).
   Ilalim reached 694 materials and that was its remaining cost. State your triangle count.
7. **The team colours are reserved.** Nothing on a large surface near offence orange #f87020 or
   defence blue #0080e8. Blues here are deep navy or indigo, or go to white or cyan; LED "blue" is
   deep (about #0a1a9a) and never the pale sky blue the blockout rendered.
8. **Gameplay must read.** The stage, the players and the can are the brightest, cleanest things.
   Stands and city are darker and lower in contrast than the stage; no bright sign competes with
   the play. Signs are few, modest in size, and never pure saturated slabs.

## The texture style (the same as Kanto, Lagoon Cove and Ilalim)

Hand-painted, illustrated, flat. Read `docs/KANTO_DESIGN_GUIDE.md` section 3 and the texture
authors `tools/author_ilalim_textures_*.py` before painting anything, and look at
`Logs/ilalim-unity/v17/*.png`, `Logs/kanto-look-v1/*.png`, `Logs/lagoon-cove-unity-v10/*.png`.
- "flat fills; a few LARGE patches with feathered organic edges"; "no grain, no streaks, no cracks".
- 1024 px tileable painted textures per surface, generated by a script in `tools/` (the Ilalim
  authors show how: numpy and PIL, deterministic, no noise layers). Never reuse one surface's
  generator for another surface.
- Windows, panels, seats and signs are PAINTED shapes with soft irregular edges and a few chosen
  colours, not a procedural grid and not random confetti. Lit windows come in clusters and rows
  that suggest floors and rooms.
- UVs have even texel density and no stretching: check a checker texture on every model before
  painting, on tapered and curved faces above all.
- World shader in Unity: `TumbangPreso/IlalimPainted` (albedo, emission, anti-tiling resamples).
  Give each material an albedo PNG and, where it glows, an emission PNG. Night is carried by
  emission and by the look profile, not by dark albedo alone.
- The logo is `Assets/TumbangPreso/Art/ui/brand/tump_logo.png`.

## How a kit is delivered

- `tools/author_arena_<kit>.py`: builds the kit headless (`blender -b --python ... -- --version=vN`)
  into `ArtSource/arena/kits/<kit>.blend`, every object in a collection named `arena_<kit>`, its
  origin and placement already in the stadium's frame (no later moving).
- `tools/author_arena_textures_<kit>.py`: writes the kit's textures to
  `Assets/TumbangPreso/Art/Arena/Textures/arena_<kit>_<surface>.png` (and `_emit.png`). Material
  names are `arena_<kit>_<surface>` and travel by name into Unity.
- Review pictures, versioned, in `Logs/arena/<kit>/`: from the player's eye by the can, from the
  air, close on the detail, and a flat-shaded view that shows how the meshes join. Look at every
  picture you render. Be your own harshest critic: list what is wrong, fix it, render again. Do
  not stop at the first version that runs; at least three rounds of render, critique and fix.
- A short `Logs/arena/<kit>/REPORT.md`: what was built, triangle and material counts, open-edge
  check, what is still weak, and what the assembly and the Unity builder need to know.
- Do NOT run Unity. Do NOT commit. Do NOT edit another kit's files or `tools/arena_kit.py` (ask
  the coordinator instead). No em dashes anywhere. Blender is
  `C:/Program Files/Blender Foundation/Blender 5.0/blender.exe`.

## The kits

| Kit | Holds |
|---|---|
| `bowl` | the field (turf, lines, track, LED barrier, kerb), the lower bowl and concourse, the four upper stands: seats, steps, rails, walkways, doors, the players' tunnel, the outside walls |
| `roof` | the four canopies, their ribs and masts, floodlight banks, the centre-hung scoreboard and its cables, the four corner screens and masts, the host's booth |
| `hull` | the saucer: deck and plaza, gate halls, lamps, the rim, the underside, eight engines, four landing pads, keel fins, the shaft's inner wall |
| `city` | the towers (by sightline), their signs, the city floor far below, sky traffic, the sky itself |
| `stage` | the hover platforms as a kit of round pieces, the five layouts, jump pad, speed pad, stamina pickup, the drone, the hologram look |
| `crowd` | 2D animated sprites of the game's own characters for the seats, and how Unity draws tens of thousands of them cheaply |
