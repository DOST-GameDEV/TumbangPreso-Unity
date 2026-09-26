# Lagoon Court rework guide

The full rework of the shipped map **Lagoon Court** (`GameLaunch.Maps` id `lagoon`, scene
`Scenes/Maps/Lagoon.unity`), directed by the owner from 2026-09-26 (*"i wanna rework lagoon
court"*). Read this whole file before touching anything Lagoon, then `docs/KANTO_DESIGN_GUIDE.md`,
whose model, texture, foliage and working rules ALL apply here too (Kanto is the first map in the
house style). Every rule below came from the owner reacting to a render; the quote is there so
nobody re-litigates it. Status row: `docs/TODO.md` **LAGOON-1**.

⚠️⚠️ **CURRENT STATE (2026-09-26): LAYOUT BLOCKOUT, cove v6, awaiting the owner's verdict.**
Nothing is modelled, textured or in Unity yet; the shipped Lagoon scene is untouched. Next steps
are § 8. This rework supersedes the REFINE-2.6 per-family refinement for this map. The map as
found: `Logs/map-lineup-v1/sheet_lagoon.png` (a flat brown deck ring over flat teal water).

---

## 1 · References (owner-chosen)

| Reference | Take from it |
|---|---|
| [Anastasia Papaioanou, Stylized Fishing Village](https://www.artstation.com/artwork/GvJv5a) (Unreal, sold on FAB; owner: *"seems like a good ref"*) | **The art style and the ground layout.** A rock massif built as a PILE of big rounded boulders the village climbs; saturated turquoise water with a pale band at the sand; stocky plank houses with oversized deep-eaved roofs; dense tropical planting (palms leaning out of rock seams, broad leaves, red and orange accents at boulder feet); piers, stairs and plank walkways at several heights; boats, crates, barrels, nets, lanterns; one landmark at the waterline (the painted octopus rock); warm sun under big painted clouds. Built from a modular kit (plank wall modules, stacked roof modules, stair pieces, a small prop kit). |
| Owner photograph: a Filipino stilt-house village at sunset | **The Filipino subject.** *"we should make it more filipino like too btw, like these stilt houses but following the stylized artstyle"*. Steep nipa/cogon thatch roofs, woven sawali (split bamboo) and plank walls, slender irregular bamboo piles with X bracing, bamboo railings and ladders, fishing nets drying, small painted bangka outriggers, a green palm hill and a beach behind. |
| Owner photograph: a Sama-Bajau water village | **The village is ON THE WATER.** *"we need space for boats and free-standing stilt houses because badjao tribe isnt particularly land based"*. Small free-standing stilt homes over clear shallow turquoise water, narrow plank walks and ladders, tin and thatch roofs, laundry lines, boats both moored and paddled (long lepa hulls, bangkas). |

Earlier primary-source notes (Sama Dilaut stilt homes, lepa houseboats, the Lookan Banaran
photograph): `docs/reports/map-by-map-refinement-2026-09-23/lagoon-reference-notes.md`.

## 2 · Art-style rules (every Lagoon model and texture)

- **House style = Kanto's** (`KANTO_DESIGN_GUIDE.md` § 2, § 3, § 5): real Blender models built
  by `bpy` scripts, one-piece shells, swept trim, **no two surfaces sharing a plane**, live
  bevels; textures FLAT and hand-illustrated (a few large feathered patches, hand-drawn shapes,
  low contrast between a thing and its joints, no grain, no noise, no streaks); foliage as
  shingled see-through leaf cards, two tints only, variation per plant never per leaf.
- ⚠️ **Never repurpose one texture's generator for another surface.** The owner rejected bark and
  roof tiles that were the brick generator in disguise (*"did you just repurpose the brick
  texture?"*, *"it genuinely just looks like a repurposed brick texture"*). Every surface gets its
  own drawing.
- ⚠️ **Research real stylized references before drawing a new texture** (*"look into actual
  stylized hand illustrated textures online and research their style"*). Airbrushed smudges were
  rejected as *"garbage"*.
- ⚠️ **Swatches first, one change per round**: a flat swatch sheet beside approved swatches
  (brick, stone_blocks) and a 3 x 3 repeat, approved by the owner, before it touches a model;
  then one game-distance render (1.25 m eye, 95°) and one close-up; version every filename.
- **Role hues** (`Art_Direction.md` § 1): nothing near offence orange `#f87020` or defence blue
  `#0080e8`. The fishing village's bright turquoise ROOFS are too close to defence blue: roofs are
  thatch, with the odd tin roof in teal-GREEN or red. The water's turquoise is fine.
- **Filipino, not generic tropical**: nipa/cogon thatch, sawali walls, bamboo piles and railings,
  bangka and lepa boats, capiz windows, a capilla, a sari-sari stall, drying nets, laundry.
  Culturally specific motifs (lepa prow carvings, festival flags) are not copied onto every boat.
- Be critical of every render before sending it; the owner expects the lead to spot the flaws.

## 3 · Rocks (owner rejected cones and crystals)

- 🧑 on blockout v5: *"i want the mountain not to just be one spiky mountain peak"*, *"i assume
  the rocks wont be just like that correct? and actually be more unique?"*, *"experiment more on
  something organic until it looks similar to the reference"*.
- **The massif is a PILE of boulders** with several humps, ridges and a saddle, never one cone.
  Underneath it, a height field of several gaussian peaks is the fill between the stones.
- **Boulders are PILLOW STONES**: a subdivided cube pulled ~70 % of the way to a sphere,
  stretched taller than wide, chopped by 3 to 5 random planes into BROAD flat faces, jittered.
  SMOOTH shading with only edges over ~38° between faces kept sharp (flat shading made them read
  as crystals). A kit of unique shapes shared as linked meshes, each placed with its own size,
  stretch and turn.
- **Rock belongs to the massif**; the outer ring and the coast are sand and grass.
- **A ledge's downhill face is still stone**, capped just under the ledge floor so the house
  looks out over it (clearing the face showed smooth dirt cliffs). Steep ground is rock, never
  grass (green cliffs).
- Warm tan rock; lighter tops and darker bases come in the texture pass; AgX Punchy look.

## 4 · Layout (current: cove v6)

- **One asymmetric island weighted north-west**, drawn from a hand-placed coast curve (`COAST`,
  Catmull-Rom smoothed): a long sand SPIT curling south on the west (its own house and a tidal
  pool), rock dropping straight into the sea on the east. Nothing mirrors across x = 0.
- **The beach** is a band inside the coast (`BEACH_BAND` 8 m) with a noise-wobbled landward edge.
- **The court** is the biggest natural CLEARING: 28 x 26 at z = 0 (the current map's tested
  size), boulders on its landward sides with inner faces just outside the play walls, open south
  down to the beach and the water. Heights: beach -1.2, water -1.8, seabed -3.5.
- **Every land house has its own POCKET** (a flat ledge) at its own height: twelve pockets at nine
  heights from the spit up to the summit; a fence on the drop side; stairs between pockets. 🧑:
  *"notice how the houses are not just on a single level"*. The **capilla** (white, bell tower,
  red tin roof) on the summit ledge is the landmark seen from the court.
- **The Sama-Bajau water village** fills the open south and south-east: about 20 SMALL one-room
  stilt homes (decks ~1.9 m over the water) strung in loose clusters along narrow plank walks,
  11 to 14 m house to house, some alone; laundry lines; ~20 boats (lepa houseboats with a
  shelter, bangka outriggers); one long walk to the beach by the court.
- **Landmark rock** in the water in front of the court, to carry a painted emblem or banner.

## 5 · Layout history (rejected, do not return to)

| Version | What | Owner |
|---|---|---|
| blockout v1 to v3 | court on a square rock SHELF 4.5 m over the water, one green hill, a stilt-house ring | *"i was thinking of a more natural platform"* |
| blockout v5 | horseshoe cove, icosphere boulders, one cone spire, three flat green terraces | *"not ... one spiky mountain peak"*, *"houses are not just on a single level"*, rocks not *"unique"* |
| cove v1 to v4 | boulder massif, many pockets, but a mirror-symmetric box lagoon with stilt houses along its sides | *"absurdly symmetrical"*, beach not *"organic"*, *"free up some more space ... for boats and free-standing stilt houses"* |

## 6 · Files and commands

| What | Command | Output |
|---|---|---|
| Organic cove blockout (CURRENT) | `blender -b --python tools/author_lagoon_cove.py -- --preview N` | `ArtSource/lagoon/lagoon_cove.blend`, `Logs/lagoon-blender/cove_<shot>_vN.png` (plan, ref_angle, village, aerial, eye_north/east/south/west) |
| Old shelf blockout (rejected; kept for its helpers) | `tools/author_lagoon_blockout.py` | `lagoon_blockout.blend` |
| Every shipped map photographed the same way (headless) | Unity `-batchmode -executeMethod TumbangPreso.EditorTools.MapKit.MapLineupCapture.Run` (menu Tumbang Preso > Maps > Render Map Lineup) | `Logs/map-lineup-vN/` |

Key names in `author_lagoon_cove.py`: `COAST`, `BEACH_BAND`, `POCKETS` (name, x, y, floor z, rx,
ry, surface), `STAIRS`, `PEAKS`, `coast_distance()`, `height()`, `boulder_mesh()`,
`place_boulders()`, `house(..., small=)`, `water_village()`, `walk()`, `planting()`.
`author_lagoon_blockout.py` supplies `col()`, `gameplay()` (chalk, lata, 1.6 m player stand-ins)
and colour names; `author_kanto_blockout.py` supplies `box/cylinder/blob/label/_obj/lighting`.
Opening a file for the owner: `Start-Process` blender.exe on the `.blend` (never screen-click).

## 7 · Gameplay constraints to settle before Unity

- The current map's court is 28 x 26; its water returns a slipper after 8 s (`LagoonWater`:
  SurfaceY -1.1, FloorY -3, Limit 42). The new court opens onto beach and shallow water on one
  side only; confirm the play walls, where the water starts and whether the return rule stays.
- `MapGeometryCheck`, `ArenaCheck`, the Hero Strike footprint and bot routing must pass on the
  rebuilt scene. The old builder is `Editor/MapKit/LagoonBuilder*.cs`.

## 8 · Plan (in order; each step rendered in Blender and approved before the next)

1. **Finish the layout** (owner verdict on cove v6 pending). Known gaps: flat single-colour
   water (turquoise shallows along the beach and spit, a pale foam band); bare spit (palms,
   beached boats); the water village reads as a scattered blob (a main walkway spine with homes
   branching off, and a few homes near the court so it shows from the court); a few HUGE feature
   boulders and more size variety; planting in rock gaps (grass tufts, broad leaves, red and
   orange accents).
2. **Rock kit**: final pillow-boulder models and a painted rock texture (swatch first: warm tan,
   lighter tops, soft darker seams); rebuild the massif from them.
3. **Stilt house kit**: nipa/cogon thatch (its own texture, swatch first), sawali wall panel
   (swatch first), plank deck, bamboo piles with X bracing, ladders, railings; variants: land
   house on a pocket, small Bajau water home, sari-sari stall, capilla.
4. **Boats**: bangka outrigger and lepa houseboat.
5. **Props**: drying nets, laundry lines, crates, barrels, baskets, lanterns, fish racks.
6. **Foliage**: coconut palms (curved leaning trunks), broad leaves, flowering accents, grass
   tufts.
7. **Water and light**: turquoise water with a shallow-to-deep gradient and a foam band, warm sun,
   big painted clouds.
8. **Assemble** `ArtSource/lagoon/lagoon_city.blend` with linked duplicates (editable, as Kanto),
   full review, owner approval, THEN export to Unity and rebuild `Lagoon.unity` (§ 7, checks, a
   played match).

Split independent kit pieces across parallel agents (owner standing preference), each in its own
new file, integrated and verified by the lead session. Commit locally; never push until the owner
has played it.
