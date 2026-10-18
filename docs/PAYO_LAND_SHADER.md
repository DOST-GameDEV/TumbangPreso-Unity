# Payo land shader

`Assets/TumbangPreso/Resources/Shaders/PayoLand.shader` (`TumbangPreso/PayoLand`) is the game's copy of the Payo land
look the owner approved in Blender on 2026-10-18. The source is `calm_paint()`, `earth_wall_grass()` and `paddy_rows()`
in `tools/payo_land_blend.py`. The version ported is the QoLUpdates one: valley walls are earth with grass grown over
them, and the paddies are green with painted rows (`Logs/payo/terrain/tex_calm/before_after_v4.png`, "paddies back to
green"). The astra-merge-prep side's `mud_and_rice()` (muddy paddies with rice patches) is NOT ported.

The materials are built by `PayoSceneBuilder.LandMaterial` from a `materials_payo.json` entry with
`"kind": "payo_land"` and a `"surface"`. The pictures are read from `Art/Payo/Textures/land/`, which holds copies of
`Logs/payo/terrain/tex_calm/`.

## Base

The shader uses the same base as LagoonPainted, IlalimPainted and LagoonGround: a built-in pipeline surface shader,
`Standard` lighting, `fullforwardshadows addshadow`, Unity fog, and `LagoonNoise.cginc` for Perlin noise and box
weights (its `ToBlender` is NOT used, see Axes below). It does not use Toon and has no outline pass. The world outline is a
post effect and covers it anyway.

Inputs:
- The vertex colour (linear, the face's own colour, Blender's `Col`).
- The world position and the world normal.
- No UVs and no tangents. The vertex function builds a tangent frame if the mesh has none, but the frame is only used
  to hand the bumped normal back.

`_BlenderOrigin` is the Unity position of the Blender land origin. It defaults to 0. Every projection, noise field,
rows map and terrace row (`floor(z / 5)`) is computed in Blender object space: `PayoToBlender(world - _BlenderOrigin)`.

**Axes (verified 2026-10-18).** Payo's land is NOT in the Lagoon's frame. `tools/payo_export_unity.py` writes the land
chunks so that Blender (x, y, z) is Unity (x, z, y): Blender x is Unity x (east), Blender y is Unity z (north), Blender
z is Unity y (up). Checked on the head fall (Blender (0, -208), Unity z -208 in payo_art.json) and on the Blender
cameras moved into Unity (`PayoSceneBuilder.Compare.cs`, `Logs/payo/unity/v1/compare.png`). So the shader uses its own
`PayoToBlender(u) = (u.x, u.z, u.y)`, not LagoonNoise's `ToBlender` (Unity from Blender (-x, z, -y)), which would
mirror the rows map and every noise field onto the wrong side of the valley.

## Surfaces (keyword `_SURFACE_*`, one per material)

| surface | Blender surface | projection | pictures | tile m | relief m | anti-tiling | far fade m | smoothness |
|---|---|---|---|---|---|---|---|---|
| slope | grass | box | grass_value, grass_height | 20 | 0.12 | rotate | 120..320 | 0 |
| wall | earth (valley terrace walls) | level + box | earth_color, earth_height (level); grass_value at 20 m (box) | 6 | 0.06 | offset only | 80..240 | 0 |
| paddy | paddy | top | paddy_value, paddy_height, paddy_rows | 12 | 0.02 (+0.05 rows) | offset only | 120..320 | 0 |
| rock | rock | box | rock_value, rock_height | 16 | 0.22 | rotate | 120..320 | 0 |
| paving | paving | top | paving_value, paving_height | 10 | 0.05 | offset only | 120..320 | 0 |
| flooded | flooded | top | flooded_value, flooded_height | 14 | 0.025 | rotate | 120..320 | 0 (the water is drawn, see Watered paddies) |
| town_wall | bank (town tier walls) | level | bank_color, bank_shade, bank_height | 12, 12, 5 | 0.097 | offset only | 80..240 | 0 |
| water | river | box | water_value, water_height | 24 | 0.10 | rotate | 120..320 | 0.85 |
| foam | river rapids | box | foam_value, foam_height | 10 | 0.10 | rotate | 120..320 | 0.6 |

All surfaces also use `macro_value` (96 m tiles, box projection, never anti-tiled or faded).

The water and foam take their colour from `_BaseColour`: (0.56, 0.74, 0.82) for water and (0.86, 0.94, 0.96) for
foam, both sRGB. They glow at 0.25 and 0.35 times that colour, as Blender's river materials do. The water is opaque
and static, like Blender's.

## How it maps to the Blender nodes

| Blender | here |
|---|---|
| Texture Coordinate Object + Mapping scale 1/metres | `pos / _TileMetres` (Blender object space) |
| Image BOX projection, blend 0.35 | `BoxWeights(n, 0.35)`. Faces read (y,z), (x,z), (x,y). Each face is sampled only when its weight > 0.001 |
| `level` side projection (two FLAT images, (x,z) and (y,z), mixed by smoothstep(-0.12..0.12) of \|nx\|-\|ny\|) | `Level2` and `Level3`, with the same feather |
| `ilalim_antitile.SAMPLES` variants (37 deg x0.61 +(0.37,0.71); -71 deg x1.43 +(0.13,0.52)); offset only when CALM_ROTATE is false; z not scaled for level | `_AT0` and `_AT1`, `_AntiTileRotate`, `Variant()` |
| Feather masks: noise detail 0 of (p/metres + seed) x mask scale, smoothstep 0.5 +- 0.08 (rotating) or 0.03 (directional) | `LayerWeights()`, `_AT0Mask` and `_AT1Mask`, `_Feather`. As in Blender, the wall's grass reuses the earth's masks |
| value map: x2, gamma 2.2, mixed toward 1 by the far fade, multiplied | `Paint()`, `lerp(1, paint, near)` |
| `far_fade`: Camera Data View Distance mapped near..far to 1..0 | `near` from `_FarFade` |
| `hue_drift`: 70 m and 45 m noise fields (detail 1), smoothstep 0.5 +- width/2, mixing warm^-1.1 -> warm^2.2 -> cool^2.2 | `HueDrift()`, `_HueWarm`, `_HueCool`, `_HueFields` |
| `paddy_rows`: rows map over its box (EXTEND), phase = r x dmax x 2pi / 1.2 + noise(0.06) x 2.5, cos, smoothstep 0.15..0.55, fade 35..110 m, value (1 + (rows - 0.3 fade) x 0.16)^2.2, its own 0.05 m bump | the `_SURFACE_PADDY` block, `_RowsBounds`, `_RowsShape`, `_RowsFade` |
| bank: stone picture x (lum(vertex colour) / lum(TOWN_WALL)), far -> bank_mean; grass (alpha 0) = vertex colour x shade^2.2 | the `_SURFACE_TOWNWALL` block, `_WallMean`, `_WallLum` |
| `earth_wall_grass` | the `_SURFACE_WALL` block, `WallGrass()`, `GrassField()` |
| Bump node (Distance = relief), rows Bump stacked on it | `Bumped()`: the same screen-space surface gradient. Stacked bumps add, so the heights are summed in metres |
| Noise Texture 4D (W = row x 7.31 + seed) | `Noise4()`: 3D Perlin, with W turned into an offset along a skew direction, scaled like the position |

Deliberate differences:
1. **The relief fades with the painted shapes.** Blender keeps it. Past the far fade it is sub-pixel and would only
   shimmer in a moving camera.
2. **paddy, flooded and paving use top projection.** Blender projects them BOX, but they are all floors, so one tap
   does the job of three.
3. **The wall grass's `under` and `over` samples** (the wall 16 cm lower and 10 cm higher) re-evaluate only the tufts,
   rows and lip terms. The warp (9 m), drifts (7 m) and regions (34 m) are shared from the centre sample. Those two
   samples only shade the grass's lower edge and the shadow it casts.
4. **The noise is Blender's recipe, not Blender's pattern** (as in LagoonNoise). The patches fall in different places
   but have the same size, spread and edges, and they never repeat.
5. **Value maps are read from the red channel.** They are grey; grass_value's channels differ by up to 5% at most.

## Cost

These counts are texture taps per pixel inside the far fade. "Typical" means one anti-tiling layer and one face,
which covers most of the land. "Mask edge" is where two anti-tiling layers blend. "Worst" is all three layers on a
three-way box corner. Every tap is a `tex2Dgrad` inside a `[branch]` that skips it when its weight is zero.

| surface | typical | mask edge | worst | past far fade |
|---|---|---|---|---|
| slope, rock, water, foam | 3 | 5 | 21 | 1 (macro) |
| paddy | 4 (incl. rows) | 6 | 8 | 1 (rows stop at 110 m) |
| flooded, paving | 3 | 5 | 7 | 1 |
| town_wall | 4 | 7 | 21 | 1 |
| wall | 4 | 7 | 24 | 1 |

ALU in Perlin evaluations (8 gradient corners each):
- Anti-tiling masks: 2 (only inside the far fade).
- Hue drift: 4 (all surfaces except the river).
- Paddy wobble: 1.
- Valley wall grass: 16 (only inside 240 m).

The wall is the expensive surface. Measure it first if the frame budget is tight. Cheaper options, in order:
1. Drop the warp's second octave (saves 3).
2. Reuse the tuft field for `over` (saves 2).
3. Shorten `_FarFade` on walls.

There is no per-frame CPU work. The shader reads only material properties set once by the builder.

Memory: value and height maps import as uncompressed data (`TexUse.Data`). The heights and the rows map get a
Standalone R16 override, because their 16-bit source matters. At 8 bits the rows' 0..130 m distance steps every
0.5 m, which is coarser than the 1.2 m row pitch.

## Tuning without recompiling

Every number above is a material property: tile metres, relief, anti-tiling variants and masks, feather, far fade,
hue tints and field sizes, row pitch, amplitude, fade and relief, the wall means and luminance, terrace step,
smoothness and glow. The builder writes Blender's values. A spec entry can override `tile`, `relief`, `smoothness`,
`far`, `hue_warm`, `hue_cool`, `origin`, `vertex_colour`, `colour`, `glow`, `step`, `rows_bounds`, `wall_mean`, and
any picture file (`value`, `height`, `colour`, `shade`, `grass`, `macro`, `rows`, top level or inside `textures`).

## Watered paddies (2026-10-18)

Owner: "add patches reflective water puddles across the terraces as to show some watered rice fields". Not a Blender
port, and the approved land is not re-exported.

- WHICH FIELDS: `tools/payo_pieces/paddy_flood.py` reads `Logs/payo/terrain/land_v59.npz` and `rice_density.png`,
  finds the fields exactly as `rice_density()` does (paddy faces minus the dilated walls, labelled), and floods whole
  fields in patches: a broad 70 m noise picks where, only fields whose planting is already sparse (mean rice density
  under 0.42, so the blades there stand as young sprouts) and never the ripe gold ones. 286 fields, 21 per cent of the
  paddy area, 22 per cent inside 320 m of the court (52 fields); the export's own 185 flooded fields stay water. It
  writes `Textures/land/paddy_flood.png` (1 px a metre, the edge softened over about a metre) and `paddy_flood.txt`
  (the box), and a review map `Logs/payo/unity/paddies/flood_map.png`.
- THE SHADER: `paddy` and `flooded` read the map (`_FloodTex`, `_FloodBounds`, one sharpened tap: a terrace strip seen
  almost edge on otherwise mips into its dry neighbours); `flooded` is always water. Where it is water: the sky rebuilt
  along the reflected ray (the live sky's gradient, soft drifting clouds in its cloud tones, the sun glow, disc and
  halo), leaned 35 per cent toward the map's water blue (`_WaterTint`) so a field reads as the same water as the river,
  a stylised fresnel (`_WaterFresnel` 0.6 straight down, power 3), four travelling ripples faded by the pixel footprint,
  a sun glint and a sun path, a darker shallow edge, wet mud along the dikes and young rice dots. The reflection is
  emission; Unity fog and the WorldOutline haze still apply.
- THE SKY'S COLOURS: `Runtime/Map/PayoPaddySky.cs` (on the Night holder) copies the live skybox's zenith, horizon, sun,
  disc, halo, direction and cloud tones into globals each frame, so the water follows `PayoNight`'s half-time sky.
  Without it the shader falls back to the fog colour, the ambient sky and the key light.
- COST: in one Play, flood map on against off (`Editor/MapKit/PayoPaddyShots.cs`, `shots ab LABEL`): GPU +0.00 to
  +0.11 ms at the views with the most water, no new material, set-pass unchanged.
- JUDGING: `Temp/paddy.request` `shots LABEL` (views over the terraces from the court's rim and above, day and night,
  `off` in the label turns the map off, `ab` times it) writes `Logs/payo/unity/paddies/LABEL_VIEW.png`.
