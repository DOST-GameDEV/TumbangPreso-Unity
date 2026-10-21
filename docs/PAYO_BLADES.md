# Payo Blades: Rice And Grass On The GPU

Owner, 2026-10-18: "the terraces still look pretty barren, are you planning to do some grass shader thing in unity?"

Rice stands on the paddies and short grass grows on the flat non-paddy ground near the court. The blades are
static meshes baked once in the editor. Every motion happens in the vertex shader from `_Time`. There is no
per-frame CPU work, which follows the owner's rule: animate in the shader on static meshes.

| File | Role |
|---|---|
| `Assets/TumbangPreso/Editor/MapKit/PayoBladesBaker.cs` | The baker. `PayoBladesBaker.Bake(Transform parent)` and the menu item *Tumbang Preso/Sample Map/Bake Payo Blades* |
| `Assets/TumbangPreso/Resources/Shaders/PayoBlades.shader` | `TumbangPreso/PayoBlades`: wind, colour, distance thinning, lighting |
| `Assets/TumbangPreso/Runtime/Map/PayoBlades.cs` | Sits on the blades root. Pushes the wind globals on enable or edit (no Update) and stores the bake's numbers |
| `Assets/TumbangPreso/Art/Payo/Generated/Blades/` | Baked output: `payo_blades_<cx>_<cz>_lod0/1.asset` and `payo_blades.mat` |

## How To Bake

1. Open the built Payo scene (`PayoSceneBuilder` has placed the land under `Dressing`).
2. Call `PayoBladesBaker.Bake(root)`, where `root` is the map root, or use the menu item with the root selected.
   The result goes under `root` as `PayoBlades`, and an earlier bake there is replaced. The call returns a
   one-line summary with the budget numbers, which are also stored on the `PayoBlades` component.
3. Save the scene. The meshes are assets, so commit them along with the scene.

Inputs:
- `rice_density.png` and `.txt` (from `tools/payo_land_tex.py`): 2 px per metre, row 0 is the north edge,
  0 = mud, 255 = rice standing. The baker reads `Art/Payo/Textures/land/` first and falls back to
  `Logs/payo/terrain/tex_calm/`.
- The land in the open scene: any object whose name or ancestor's name passes `PayoSceneBuilder.IsLand`
  (`payo_land*` and similar). The land has no colliders, so the baker adds temporary MeshColliders, raycasts
  down for height, normal, triangle and material, then removes the colliders again.
- The land's vertex colour under each rice clump picks the ripe gold fields. The test is red over green,
  the same one `payo_land_tex.py` uses.

**Axes.** Blender (x, y, z) maps to Unity (x, z, y). `payo_assemble.py` turns the land half a turn about Z on
export, Blender to glTF is (x, z, -y), and glTFast negates x. So the density map's Blender x is Unity x
(east), its Blender y is Unity z (north), and the court is at the origin in both.

## Design

**Where blades grow.**
- **Rice:** on every density pixel within 300 m of the court. The rate is `density x 2.2 clumps/m2 x taper`,
  where the taper falls from 1 to 0.45 between 70 m and 260 m and reaches 0 by 300 m, so the edge never
  shows as a line. The ground must be flat (normal y > 0.90) and must not be rock, wall, paving or water.
- **Grass:** on a 1 m grid within 250 m. The rate is 0.9 tufts/m2, tapering to 0.35 by 170 m and to 0 by
  250 m. Grass needs normal y > 0.85, must be at least 2 m from any rice pixel, and must be outside the
  pit (|x|, |z| < 31).
  - If some land material name contains grass, slope, lawn, garden or meadow (and not paddy), grass
    keeps to those materials.
  - Otherwise grass grows on any land that is not rock, paving, path, mud or paddy by name.
- **Covered ground is skipped.** A second short ray looks for anything solid other than land within 1.2 m
  above the ground (paving, walls, house floors, water), ignoring things within 8 cm of the ground. If it
  finds something, nothing grows there. Every opaque or water renderer within reach gets a temporary
  collider for this test. Mist, cloud, sky, glow and fall surfaces are left out.

**Blades.**
- **A rice clump** has 5 to 8 slender arched blades (4 to 6 beyond 120 m), 0.5 to 0.8 m tall. Ripe fields
  droop more and are a little taller.
- **A grass tuft** has 3 to 5 blades, 0.18 to 0.36 m tall.
- **Segments:** a blade has 2 segments (5 vertices, 3 triangles) within 80 m of the court for rice and
  50 m for grass. Beyond that it has 1 segment (3 vertices, 1 triangle).
- **Width:** blade width grows with distance from the court (x(1 + d/60)), so a far blade stays about
  a pixel wide.

**Chunks and LOD.**
- The world is split into 48 m cells. Each cell is one GameObject with a LODGroup and two MeshRenderers,
  so frustum culling and distance culling work per chunk.
- **LOD0** holds every clump. **LOD1** holds the clumps with rank < 0.35, using exactly the same
  1-segment blades.
- The LOD distances are worked out for a 60 degree vertical FOV at `QualitySettings.lodBias` 1: LOD1 from
  130 m (chunk centre) and culled at 335 m.
- The renderers cast no shadows and use no light probes or reflection probes. They are not static, which
  is not needed: the meshes are already world-placed chunks.

**Shader (built-in pipeline surface shader, structured like LagoonPainted).**
- **Wind:** a quick per-clump sway plus slow world-space gust waves (value noise) rolling across the
  paddies. The lean is weighted by bend squared, from 0 at the root to 1 at the tip.
- **Colour:** the root colour runs to the tip colour, per kind. Ripe fields get gold tips. There is a
  per-clump shade hash, broad colour patches, and a darker root where the blade meets the ground. The
  colours are matched to the land's paddy and bund vertex colours (`tools/payo_land.py` PADDY, BUND_C,
  GRASS).
- **Lighting:** blades are lit as the ground under them (normal up), with wrap diffuse and a little back
  light through the tips. The shader receives the directional shadow and the fog.
- **Distance (camera):**
  - Clumps ranked above the kept share shrink to their root between 45 m and 96 m, leaving the LOD1 share.
    This means no pop at the LOD switch.
  - Every blade shrinks to nothing between 255 m and 300 m. A shrunk blade is a degenerate triangle with no
    pixel cost.
- **Cost-cutting choices:**
  - The blades are solid, with no alpha and no clip, so there is no overdraw from cut-outs.
  - There is no ShadowCaster pass and `FallBack Off`, so blades are not drawn into the shadow map or the
    camera depth texture. Their pixels take the ground's shadow from just behind them.
  - `noforwardadd` means a night lamp does not draw chunks a second time.
- **Vertex format:** 24 bytes per vertex.
  - Position: float3.
  - Colour: unorm8x4 (bend, phase, LOD rank, gold).
  - UV0: half4 (clump root minus vertex, and blade height, negative for grass).
- The meshes are saved non-readable, so there is no CPU copy in the player.

## Budget

These numbers are worked out from the real `rice_density.png` with the baker's own rules. Grass depends
on the land, so the grass figures are an upper bound: they assume every flat non-paddy metre within reach
is grass ground. The real bake prints exact figures, and so does the `PayoBlades` component. Measure them.

| | Rice | Grass (upper bound) | Total |
|---|---|---|---|
| Clumps or tufts | 36,800 | 45,000 | ~82,000 |
| LOD0 triangles | 264,000 | 206,000 | ~470,000 |
| of which within 120 m of the court | 159,000 | 98,000 | ~257,000 |
| LOD1 triangles | 73,000 | 63,000 | ~136,000 |
| Drawn from the court, all directions | 203,000 | 143,000 | ~346,000 |
| Vertices (LOD0 + LOD1) | 897,000 | 757,000 | ~1.65 M |

- **Hard cap:** `MaxLod0Triangles` = 1,200,000. Above it, every clump is thinned evenly and a warning is
  logged.
- **Draw calls:** about 56 chunks hold rice. With grass, expect roughly 70 to 100 chunks, all within the
  cull distance of the court. A 60 to 90 degree view keeps a quarter to a third of them, which is
  **about 20 to 35 draw calls**. Blades add no shadow-map, depth or forward-add draws.
- **Memory:** at most about 40 MB of vertices plus 5 to 7 MB of indices (GPU only). The real figure should
  be lower, because the grass bound is generous. LOD1 accounts for about 25% of it.
- **Vertex work:** at most about 1 M vertex-shader runs per frame from the court, before frustum culling.

## Tuning

All the knobs are constants at the top of `PayoBladesBaker.cs` and need a re-bake. The exceptions are the
shader properties on `payo_blades.mat` and the wind on the `PayoBlades` component, which are live.

| Want | Change |
|---|---|
| Fewer or more triangles everywhere | `RiceClumpsPerM2`, `GrassTuftsPerM2`, the blade counts in `Scatter`, or `MaxLod0Triangles` |
| Less memory | Lower `Lod1Share`, or move the 2-segment radii (80 m and 50 m in `Scatter`) inward |
| Blades reaching farther or nearer | `RiceReach`, `GrassReach`, and keep `LodCull` at roughly reach + 35. The baker sets `_FadeStart`/`_FadeEnd` from `RiceReach` |
| LOD switch | `LodSwitch`. The baker sets `_ThinEnd` = `LodSwitch` - 34 m (half a chunk diagonal) and `_FarKeep` = `Lod1Share` |
| Grass on slopes, rice on the bunds | `GrassFlat` and `RiceFlat` (normal y) |
| Grass away from the paddies | `PaddyClear` |
| Colour | `RiceRoot`/`RiceTip`/`RiceGold`/`GrassRoot`/`GrassTip` (linear, in the baker), or live on the material. `_ShadeVar`, `_PatchAmount` and `_RootDark` vary them |
| Wind | `PayoBlades`: `WindBearing`, `WindStrength`, `SwaySpeed`, `GustScale`, `GustSpeed`. These are globals `_PayoWind` and `_PayoGust`. When unset, the blades stand still |
| Lighting | `_Wrap` and `_Translucency` on the material |

## 2026-10-18 Rework: Grass That Reads As Grass

Owner: "the grass does not look like grass" (with the Stylized Grass Shader's hue-variation and translucency shots).
Built in that spirit, nothing bought. The numbers above are from the first bake; the current bake prints its own.

- **Grass:** 2.4 tufts/m2 by the court (was 0.9), tapering from 55 m, gone by 200 m (`GrassReach`). A tuft is a
  carpet patch: 10 to 14 blades near (6 to 8 beyond 70 m) scattered over a 0.28 to 0.42 m disc on the ground's plane,
  0.26 to 0.5 m tall, tapered to a point, curved. 3 segments within 45 m, 2 to 120 m, 1 beyond. `GrassFlat` 0.78, so
  the slopes near the court grow grass too. LOD1 keeps 60% of the tufts (`GrassLod1Share`).
- **Flowers:** about 7% of tufts in noise patches within 110 m get 1 to 3 flowers: a stem and a five-petalled star
  head tipped toward a random side, pink, white, purple or yellow (one colour a patch, mostly), with a warm centre.
- **Rice:** 3 clumps/m2 (was 2.2), 3 segments within 60 m, LOD1 keeps 55% (was 35%, which left the far terraces
  speckled), far blades wider. Colours moved next to the paddy's own green (the old pale tips read as white specks).
- **Shader:** dark root to light tip; grass tips drift over the field between green, yellow-green and dry gold
  (two octaves of world noise, `_HueScale`, `_HueAmount`); translucency toward the sun (`_Translucency`,
  `_TransColour`, lime); gust waves in two octaves that also brighten the blades they bend (`_GustSheen`); per-blade
  phase and shade. RenderType is `PayoGrass`, so the world outline's depth-normals prepass skips the blades.
- **Cost (measured in Play, `Logs/payo/unity/perf/after.txt`):** about 880k LOD0 triangles in 80 chunks; switching
  the blades off moves the frame by under the run-to-run noise (about 0.3 ms).
