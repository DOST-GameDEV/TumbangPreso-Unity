# Kanto design guide

The sample map **Kanto**: a city park block in the style of Tiny Talisman's "Stylized Modern
City", PEAK and Brainchild's cartoon towns. Read this before touching any Kanto file. Every
rule below came from the owner reacting to a render; the quote is there so nobody re-litigates
it. Status and open items: `docs/TODO.md` **KANTO-1**.

⚠️⚠️ **NEXT STEP, OWNER, 2026-09-24: "i want the map in full in blender first before finalizing
in unity".** The Unity scene exists (commit `7eb8ec862`) but is NOT the place to iterate. Build
and review the whole assembled map in Blender (`ArtSource/kanto/kanto_city.blend`), get it
approved there, and only then re-export to Unity. See § 8.

---

## 1 · References and what each one contributes

| Reference | Take from it |
|---|---|
| [Tiny Talisman, Stylized Modern City](https://tinytalisman.artstation.com/projects/kldmG0) | Chunky bevelled buildings, deep window recesses, stone trim on brick, stacked cornices, sagging power lines, round leafy trees, street clutter, a **clear cool morning** light. Mixed building STYLES side by side (see § 4). |
| PEAK (Aggro Crab / Landfall) | Almost flat colour; shape carries the detail; big simple silhouettes layered into fog for depth (the skyline ring and hills). |
| [Brainchild](https://brainchild.artstation.com/) | Exaggerated trim, eaves that overhang too far, houses that lean a degree or two, bunting. |
| The game's own key art (splash, login) | Flat fills, gouache-like patches with organic edges, warm but clean. **This is the texture style.** |

## 2 · Models: real Blender models, built properly

- 🧑 *"i want the map to actually be real models and not generated via scripts"*, then *"lets
  do a blender design instead"*. Models are authored in **Blender** as `.blend` files (by
  `bpy` scripts, but the output is an editable Blender model, opened and judged in Blender).
  Nothing is built from Unity primitives. The first Unity-primitive draft also rendered
  **inside out** and was deleted.
- 🧑 *"youre only using basic shapes, the walls arent even one piece"* + *"z fighting"*:
  - The **wall shell is ONE welded closed mesh**; windows are cut into it and pushed in, so
    recesses and reveals are part of the wall.
  - **Trim is swept**: cornice, string courses, plinth, fascia, sills, lintels are moulding
    profiles swept along the facade with mitred corners.
  - **Window frames** are single cut-out meshes standing in the recess in front of the glass.
  - ⚠️ **NO TWO SURFACES SHARE A PLANE, ANYWHERE.** Attached parts penetrate; things placed
    on a surface sink 1 to 2 cm into it; the ground is non-overlapping cells, never stacked
    slabs. 🧑 on the blockout's road/pavement z-fight: *"make sure this doesnt survive the
    final pass"*.
- Every piece carries a live **Bevel** modifier (hardened normals): soft chunky edges.
- 🧑 *"the door entrance makes no sense"*: doors go to the ground, recessed ~0.6 m, with a
  step, a stone head, pilasters; the plinth stops either side. Never a frame inside shop glass.
- 🧑 *"the bottom window frames blend in too much with the window blue"*: frames must
  contrast with teal glass (cream or near-black), never dark teal.
- 🧑 *"ac units are just untextured rectangles"*: AC units are modelled (casing, recessed
  louvred front, side vents, band, badge). Same standard for every prop.
- Planters are **hollow** (floor, walls, soil 4 cm below the rim). A solid box with a soil
  slab at rim height z-fought.

## 3 · Textures: flat, hand-illustrated, simple

- Rejected: ready-made stylized packs (*"too detailed and more old-rpg like"*); a soft painted
  set with streaked glass, straight bricks, grainy wood and a noisy roof (*"too realistic and
  rigid"*, *"overcrowded"*, *"too noisy"*).
- **The rules** (`tools/author_kanto_textures.py` docstring has them in full): flat fills; a
  few LARGE patches with **feathered** organic edges (*"the shapes drawn are too harsh"*);
  bricks are **wobbly hand-drawn lozenges** with one flat highlight band; low contrast between
  a thing and its joints; no grain, no streaks, no cracks.
- Grout: three greys were compared; 🧑 *"lets keep the old grout"*, the brick-toned `9c4636`.
- Glass: flat teal with one gentle gradient, **no streaks** (*"the window's streak lines make
  the design look overcrowded"*).
- Repetition: 🧑 *"this roof repeats too much"*. Large-area textures get a bigger tile and an
  **anti-tiling** blend (a second sample rotated 37°, scaled 0.61, mixed by a soft noise mask).
  ⚠️ This exists in the Blender material only; Unity's materials still tile plainly.
- ⚠️⚠️ **BARK IS SETTLED ON VARIANT J. DO NOT REOPEN IT UNLESS THE OWNER ASKS.** J is
  `bark_j()` in `tools/author_kanto_textures.py`, the default (commit `fbb2711ae`): **one warm
  brown with many long, soft, feathered vertical strokes**. As kept on the park trees:
  `Logs/kanto-blender/bark_J_near_v1.png` and `bark_J_eye_v1.png`. **Rejected, in order:**
  A to I (faint strokes; broad shadow strokes; airbrushed layered strokes and knots/lichen,
  *"what is this garbage?"*; drawn plates from the brick generator, *"it genuinely just looks
  like a repurposed brick texture"*; crack lines) and a fewer-wider-strokes version of J. The
  variant code stays in the script for reference only.
- **Photoshop loop** (the owner paints with his own brushes): paint `<name>_albedo.png` and
  `<name>_height.png` in `ArtSource/kanto/textures/`, then
  `py -3 tools/author_kanto_textures.py --normals-only`. ⚠️ Running the script WITHOUT that
  flag repaints everything and overwrites hand-painted work.

## 4 · Variety: styles, not repaints

🧑 *"you're reusing the same materials/textures making it all look the same. the talisman ref
has variety in their buildings' design styles"*. The roster (`author_kanto_city.py`):

| Kind | Construction |
|---|---|
| brick corner (hero) | brick, cut corner, stone trim, keystones, quoins, awnings, water tank, billboard |
| deco corner | cream plaster, rounded corner, curved shop front, mustard fins, stepped turret |
| glass tower | stone podium with shops, teal glass tower, spandrel bands, fins, crown |
| townhouses | narrow, leaning 1-1.5°, tin gables with deep eaves, bay windows |
| loft / loft_ph | red or brown brick, big dark-framed grid windows, painted shop base, optional glazed penthouse |
| stucco | tan/olive stucco, ribbon windows, cantilevered bays, penthouse |
| glassmid | concrete panels, ribbon glazing, projecting floor slabs |
| panel | concrete panels, grid windows in cream frames, balconies |
| shophouse | painted plaster, classic windows, railinged balconies |

New buildings must add a **construction** difference (window system, massing, setbacks, bays,
material), not only a colour.

⚠️ **Role hues** (`Art_Direction.md` § 1): nothing near offence orange `#f87020` or defence
blue `#0080e8`. The reference's bright orange shopfront became ochre; glass is teal, not blue.

## 5 · Foliage: the house pattern for every plant

- Shaped, see-through **leaf cards**, never a plain blob model.
- 🧑 *"all leaves go in random directions rather than a cohesive ball"*: leaves are
  **shingled** on the clump's surface (facing out, tips running down it, ±20° spread), evenly
  spread (Fibonacci sphere). Custom normals point away from the clump centre so it shades as
  one soft ball.
- 🧑 *"is there a need to keep the core visible?"*: **no core**; density closes the gaps.
- 🧑 *"the core leaf design is sharply outlined and not softly painted"*: the leaf drawing is
  soft (stem-dark to tip-light, a soft sunlit patch, faint midrib, no outline).
- 🧑 *"i liked the 3 plant balls idea"*: window boxes get **three round clumps**, sunk into
  the soil, aligned to the wall they sit on.
- ⚠️ **Tried and reverted, do not redo:** per-leaf colour variation and a stepped gradient
  (🧑 *"nevermind, revert back"*). Leaves are two tints: light on top, dark underneath.
- One function, `foliage()` in `author_kanto_models.py`. Trees are several clumps on limbs.

## 6 · Layout (approved blockout)

🧑 *"this looks good, proceed"* on `ArtSource/kanto/kanto_blockout.blend`.

- **The park is the play area**, sized to **Bayan Plaza** (walls at ±13; measured from the
  shipped Bounds colliders, since no two maps share a size). 14 × 14 box on a 20 × 20 paved
  court; lawns, hedge beds, benches, lamps; a tree per corner 11.6 m out; a 0.8 m fence just
  outside the walls; clutter under 1.0 m.
- A 10 m **ring road** (centre lines ±22), 3 m sidewalks both sides; buildings from 30 m
  **facing the park**: brick corner NE (cut corner toward the park corner), deco NW, glass
  tower SE, townhouses SW, walk-ups on the four sides. Grid streets run out, then a skyline
  ring and hills in fog.
- Numbers live in `tools/author_kanto_blockout.py` and are imported, never retyped.

## 7 · Light

- Blender files save their own **sun and sky** (🧑 *"why is it just a full black model when
  the viewport setting is set to rendered? u should add a light source"*) and open in
  Material Preview.
- Target is a **clear cool morning**: white sun, blue sky, cool shadows, saturated paint. The
  shipped maps' warm hazy preset turned Kanto amber in Unity; it is overridden in
  `KantoSceneBuilder`.

## 8 · Pipeline, and the NEXT STEP

| Step | Tool | Output |
|---|---|---|
| Textures | `py -3 tools/author_kanto_textures.py` | `ArtSource/kanto/textures/` |
| Hero building | `blender -b --python tools/author_kanto_models.py -- brick_corner --textured --preview N` | `brick_corner.blend`, previews |
| Layout | `blender -b --python tools/author_kanto_blockout.py -- --preview N` | `kanto_blockout.blend` |
| Other models | `blender -b --python tools/author_kanto_city.py -- --preview-model a,b --preview N` | `<model>.blend`, previews |
| **Whole map** | `blender -b --python tools/author_kanto_city.py -- --assemble --review N` | `kanto_city.blend`, `Logs/kanto-blender/kanto_city_<camera>_vN.png` |
| Re-render a hand-edited map | `blender -b ArtSource/kanto/kanto_city.blend --python tools/author_kanto_city.py -- --review N` | the same renders, from the file as saved |
| Export | `blender -b --python tools/author_kanto_city.py` | `Art/Kanto/Models/*.glb`, `kanto_layout.json` |
| Unity | `KantoSceneBuilder.RunReview` (batch mode) | `Kanto.unity`, `Logs/kanto-unity-vN` |

⚠️⚠️ **THE WHOLE MAP IS IN BLENDER NOW (`--assemble`, 2026-09-24). ITERATE THERE WITH THE
OWNER; ONLY AFTER APPROVAL re-export `.glb` + layout and rebuild the Unity scene.**
`kanto_city.blend` holds every model once, textured, under **Kit** (excluded from the view
layer), and **City** holds collection instances placed from the same `PLACE` list the Unity
layout JSON is written from, plus the ground cells, road markings, court chalk and wires.
The sun, flat sky and a compositor distance fog (Unity's 90 to 360 m, capped at 82 per cent)
are saved in the file, and so are the review cameras: `eye_north/east/south/west` stand on
the attacker spawn ring at the **game's eye** (1.25 m above the court, 95°, 16.5 mm) looking
across the court, plus `aerial`.
- ⚠️ **Edit in the file, or edit the script, not both.** `--assemble` rebuilds the file from
  the scripts and overwrites any hand edits in it; `--review N` alone renders the file as
  saved.
- ⚠️ **The skyline ring and hills (`B.horizon`) exist only in Blender so far.** The Unity
  scene has fog and no horizon; the re-export has to carry them.

## 9 · Working rules learnt this session

- **Show, do not describe**: render after every change and **version every filename**
  (`_v12`), because image caches key on the name.
- **Blender previews use AgX Punchy**; it desaturates bright greens, so judge colour in
  Unity before tuning a palette hard.
- **Coordinates**: glTFast negates X, so a Blender point (x, y, z) lands in Unity at
  (−x, z, −y); yaw is −θ. The layout JSON already applies this.
- **Materials reach Unity by NAME.** `.glb` files carry no textures; `KantoSceneBuilder`
  builds one shared material per name. A new material needs a palette entry; the build log
  warns on any unmatched name.
- ⚠️ **Do not click through Unity's menus with screen automation.** Menu positions shift and
  a missed click ran *Add Mountain Backdrop to Eskinita*, which saved Eskinita (reverted from
  git). Use batch mode (`-executeMethod`), close the interactive editor first, clear a stale
  `Temp/UnityLockfile`, and assert on the log, not the exit code.
- ⚠️ Launching Blender through the screen tool spawned extra empty Blender windows; start it
  with `Start-Process` on the `.blend`, and never kill a Blender window whose title starts
  with `*` (unsaved owner work).
- **Commit locally; do not push** until the owner has played it (standing preference).
- Bash heredocs with nested quotes broke twice here; write patch scripts to a file.

## 10 · Gap review against the references (owner, 2026-09-24, on review v3)

🧑 on the assembled map: *"i dont like to see these really noticeable repeating textures and
colors"*, *"a lot of models are untextured"* (tree trunks, signal and street posts, railings),
*"most of the other buildings are essentially just big rectangles, and a lot of them are
primarily just white"*, *"theres not even any glass buildings/skyscrapers"*, and the trees are
*"all the same size and color and type"*, all facing the same way. Measured from
`kanto_layout.json`, and compared shot by shot with the 15 Tiny Talisman frames and the
Brainchild set:

| Gap | Ours now | The reference |
|---|---|---|
| Skyscrapers | None. The tallest real model is 9 storeys; the "skyline" is 46 untextured grey boxes. `glass_tower` is a grid of punched windows, not a curtain wall. | A downtown of 20 to 40 storey curtain-wall towers: dark sky-reflecting glass, mullion grids, chamfered and faceted crowns, glass pyramids with spires, a round tower, exposed steel lattice. |
| Massing | Each building is one extruded footprint with a parapet. | Stacked volumes: stepped podiums, setbacks, corner towers, bays running full height, rooftop stair and lift houses, chimneys, two or three heights in one block. |
| Colour | Big areas of white `panel` and cream; every roof the same flat grey. | Saturated warm brick, red and green shopfronts, yellow accents, next to dark glass. White is trim, not whole buildings. |
| Repetition | 8 filler models placed 12 times each, 48 identical power poles, 16 identical street trees and 4 identical park trees; placed at one scale. | Modular pieces recombined, so the same kit never reads as the same building. |
| Untextured props | Trunk, poles, railing, metal, signals, bins, lamps and fence wear only the flat `paint` tint. | Every prop has painted wear, bands and colour breaks. |
| Roofs | Empty grey slabs, sometimes a water tank. | HVAC units, vents, antennas, satellite dishes, skylights, water tanks, a helipad. |
| Trees | One dark-green model per use, same size, three yaws. | Big, bright lime clumpy crowns on thick bent trunks; several sizes and shapes. |
| Street life | Benches, bins, lamps, one signal per corner. | Cars, taxis, buses and trucks; food carts; road barriers; hydrants; warning signs; shop names; hanging signs; billboards with art; wires crossing the streets overhead. |
| Windows | Every pane the same teal. | Interiors: some panes lit warm or green, some dark, some with blinds. |
| Park | Rectangles only. | A pond, curved paths, a playground, umbrellas, a pillared fence. |
| Sky | Flat gradient, no clouds. | Big painted clouds, high sun, strong contrast. |
| Brainchild | Nothing from it yet. | Steep tiled roofs with dormers, fire escapes, bunting, big shop lettering, leaning facades. |

⚠️ **Role hues still apply** (§ 4): the reference's orange shopfronts and orange brick sit near
offence orange `#f87020`, so warm brick stays red-brown and shops stay ochre or red.

## 11 · Decisions from the 2026-09-24/25 iteration (read before touching Kanto again)

Blender map: `ArtSource/kanto/kanto_city.blend`, built by `blender -b --python
tools/author_kanto_city.py -- --assemble [--review N] [--cams a,b]`; placements are LINKED
DUPLICATES under an empty per placement (🧑: *"fix this whole map so im actually able to edit
meshes"*): click any piece, Tab, edit, every copy follows. ⚠️ `--assemble` rebuilds the file from
the scripts and overwrites hand edits; `--review N` alone renders the file as saved. Exported to
Unity on 2026-09-25 (`2b69e4f20`, 438 placements, 81 models, textures copied by the export into
`Art/Kanto/Textures`), and `Kanto.unity` rebuilt with **Tumbang Preso > Sample Map > Build Kanto**.

- **Downtown towers** (`skyscraper()`, `TOWERS`): ten curtain-wall towers, each placed twice,
  outside the street blocks and off the sun line through the court. Glass = the street windows'
  teal, matte, one gradient per storey (🧑: *"less reflective ... match the rest of the
  windows"*). Panes at least 2.6 m wide, frames ~0.3 m (🧑: *"less dense ... thicken the
  frames"*). Frames and spandrel bands per tower in **bronze, charcoal or deep teal** (🧑 on five
  options: *"just have CDE"*), never white.
- **Buildings**: 24 generated far buildings (no more 12 copies of 8), coloured panels (no white),
  pitched roofs with dormers, corner turrets, rooftop clutter. **Clay tiles never on a brick
  building** (🧑: *"otherwise it'll all look the same"*); brick gets slate or teal. Roof tile
  textures: `tiles_clay`, `tiles_slate`, `tiles_teal` (glazed barrels), each its own drawing,
  owner-approved on swatch sheets.
- **Trees**: drawn from the owner's stylized references: slender curved trunk splitting into 2 to
  4 thin rising branches under a lifted canopy. SEGMENTED limbs (🧑: *"continue with a segmented
  version"*), POLYGONAL (7-sided trunk, 5-sided branches, flat shaded; 🧑: *"more polygonal tree
  trunk design"*), each limb with its own square-texel cylindrical UVs, a limb running through
  its forks as one tube. Pines (`tools/author_kanto_pine.py`) replace the tall trees.
- **Bark is J** (§ 3). **Metal** for poles/signals/bins is a flat coat with faint light chips.
- **Street life**: vehicles `tools/author_kanto_vehicles.py` (sedans, taxi, van, pickup, bus,
  jeepney, tricycle), shop signs/blade signs/billboards `tools/author_kanto_signage.py` (Filipino
  shop names), traffic both lanes right-hand, a jeepney stop and tricycle rank, street ends closed
  by long buildings, wires across the streets.
- **Traffic signal** rebuilt (heads in front of the pole, curved brace, clamped name blade).
- **Dormers**: glass, frame and face at separate depths (the z-fight the owner found).
- Open: vehicle chrome reads grey; Unity lacks the Blender mist, anti-tiling and hills; the
  vehicle/sign material names need Unity palette entries (watch the build log's unmatched-name
  warning).
