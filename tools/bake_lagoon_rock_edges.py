"""Bake each rock's EDGE-WEAR mask from its real geometry into one shared atlas.

  blender -b <file.blend> --python tools/bake_lagoon_rock_edges.py -- [--material rock] [--save]

OWNER, 2026-09-26, on the rock swatches: "the issue with the other textures is the white cell
lines you added ... it doesnt work that way on a tileing texture. you genuinely need to weather
only the edges of the rock instead of imitating it on the tiled texture", and, of the
reference, "the edges are lined with a brighter color compared to the inside plane of the rock
faces". So the tiling texture stays quiet (rock_a) and the bright edge comes from a MASK baked
from each stone's own geometry: 1 along its convex plane breaks, 0 across the planes.

How:
  * Every mesh using the rock material gets a second UV map, "UVBake": its own unique
    unwrap (smart project), packed into its own cell of an N x N grid, so ONE atlas image
    serves every stone and one shared material works in Blender and in Unity (UV2 there).
  * Cycles bakes an emission shader into the atlas: edge = 1 - dot(bevel normal, true normal),
    the standard bevel-normal edge detector, kept only where the surface is convex
    (pointiness), then remapped so a plane break is a narrow band.
  * The mask is baked with the stone at scale 1 in its own units, so the band keeps the same
    proportion on a 2 m cobble and a 12 m feature stone.
Writes ArtSource/lagoon/textures/rock_edges_atlas.png (and saves the .blend with --save).
"""
import math
import sys
from pathlib import Path

import bpy

ROOT = Path(__file__).resolve().parents[1]
ATLAS = ROOT / "ArtSource" / "lagoon" / "textures" / "rock_edges_atlas.png"
ATLAS_PX = 4096        # 1024 a stone: big stones keep a crisp line (2048 blurred it)
BROAD_R = 0.8           # the broad shoulder gradient (G channel)
BEVEL_R = 0.24          # object units, stones ~2.5 across. 0.07 baked a hairline (an outline); 0.14 was
                        # too faint on the final kit, whose own 4 cm bevels soften every break


def rock_meshes(material):
    seen = []
    for me in bpy.data.meshes:
        if me.users and any(m and m.name == material for m in me.materials) and me not in seen:
            seen.append(me)
    return seen


def unwrap_into_cell(me, index, grid):
    """Give `me` a unique "UVBake" unwrap, scaled into cell `index` of a grid x grid atlas. A
    mesh from the rock kit (tools/author_lagoon_rocks.py) already has a checked, overlap-free
    "UVBake" in 0..1; it is kept and only packed into its cell. Others are smart-projected."""
    if "UVBake" in me.uv_layers and me.get("rock_uvbake_angle") is not None:
        _pack(me, index, grid)
        return
    if "UVBake" not in me.uv_layers:
        me.uv_layers.new(name="UVBake")
    tmp = bpy.data.objects.new("_unwrap", me)
    bpy.context.scene.collection.objects.link(tmp)
    for o in bpy.context.view_layer.objects:
        o.select_set(False)
    bpy.context.view_layer.objects.active = tmp
    tmp.select_set(True)
    keep = me.uv_layers.active_index
    me.uv_layers.active = me.uv_layers["UVBake"]
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(50), island_margin=0.03)
    bpy.ops.object.mode_set(mode="OBJECT")
    me.uv_layers.active_index = keep
    bpy.data.objects.remove(tmp)
    _pack(me, index, grid)


def _pack(me, index, grid):
    # ⚠️ IDEMPOTENT: re-baking a SAVED file used to pack an already packed island again, into a
    # sliver of its cell (a blurry atlas). The packing is recorded on the mesh and undone first.
    if me.get("uvbake_cell") is not None:
        pi, pg = me["uvbake_cell"]
        pcx, pcy = pi % pg, pi // pg
        for d in me.uv_layers["UVBake"].data:
            u, v = d.uv
            d.uv = ((u * pg - pcx - 0.04) / (1 - 0.08), (v * pg - pcy - 0.04) / (1 - 0.08))
    me["uvbake_cell"] = (index, grid)
    pad = 0.04
    cx, cy = index % grid, index // grid
    uv = me.uv_layers["UVBake"].data
    for d in uv:
        u, v = d.uv
        d.uv = ((cx + pad + u * (1 - 2 * pad)) / grid, (cy + pad + v * (1 - 2 * pad)) / grid)


def _edge_mask(nt, geo, radius, flat, edge):
    """1 - dot(bevel normal, true normal) remapped so `flat` reads 0 and `edge` reads 1, kept
    only where the surface is convex (pointiness)."""
    bevel = nt.nodes.new("ShaderNodeBevel")
    bevel.samples = 16
    bevel.inputs["Radius"].default_value = radius
    dot = nt.nodes.new("ShaderNodeVectorMath")
    dot.operation = "DOT_PRODUCT"
    nt.links.new(bevel.outputs["Normal"], dot.inputs[0])
    nt.links.new(geo.outputs["Normal"], dot.inputs[1])
    band = nt.nodes.new("ShaderNodeMapRange")
    band.inputs["From Min"].default_value, band.inputs["From Max"].default_value = flat, edge
    nt.links.new(dot.outputs["Value"], band.inputs["Value"])
    convex = nt.nodes.new("ShaderNodeMapRange")
    convex.inputs["From Min"].default_value, convex.inputs["From Max"].default_value = 0.49, 0.53
    nt.links.new(geo.outputs["Pointiness"], convex.inputs["Value"])
    mul = nt.nodes.new("ShaderNodeMath")
    mul.operation = "MULTIPLY"
    nt.links.new(band.outputs["Result"], mul.inputs[0])
    nt.links.new(convex.outputs["Result"], mul.inputs[1])
    return mul.outputs["Value"]


def bake_material(image):
    """THREE MASKS in one atlas (owner on v7: "the weathered edges look really unnatural"; v7
    baked one line along every break, which read as pale piping round each plane):
      R  narrow edge:   the plane breaks themselves (for sparse chips on TOP edges only)
      G  broad shoulder: a wide soft convexity gradient, where a rounded shoulder catches light
      B  occlusion:     the stone's own ambient occlusion, dark in the crevices between planes
    The material combines them; the atlas is linear (Non-Color)."""
    m = bpy.data.materials.new("_edge_bake")
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    emit = nt.nodes.new("ShaderNodeEmission")
    emit.inputs["Strength"].default_value = 1.0
    nt.links.new(emit.outputs["Emission"], out.inputs["Surface"])
    geo = nt.nodes.new("ShaderNodeNewGeometry")
    narrow = _edge_mask(nt, geo, BEVEL_R, 0.9995, 0.80)   # a long near-linear falloff: read as a distance
    broad = _edge_mask(nt, geo, BROAD_R, 0.998, 0.80)
    ao = nt.nodes.new("ShaderNodeAmbientOcclusion")
    ao.only_local = True
    ao.samples = 16
    ao.inputs["Distance"].default_value = 0.8
    rgb = nt.nodes.new("ShaderNodeCombineColor")
    nt.links.new(narrow, rgb.inputs["Red"])
    nt.links.new(broad, rgb.inputs["Green"])
    nt.links.new(ao.outputs["AO"], rgb.inputs["Blue"])
    nt.links.new(rgb.outputs["Color"], emit.inputs["Color"])
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = image
    uvn = nt.nodes.new("ShaderNodeUVMap")
    uvn.uv_map = "UVBake"
    nt.links.new(uvn.outputs["UV"], tex.inputs["Vector"])
    nt.nodes.active = tex
    return m


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    bake(argv[argv.index("--material") + 1] if "--material" in argv else "rock")
    if "--save" in argv:
        bpy.ops.wm.save_mainfile(compress=True)


def bake(material="rock"):
    """Unwrap, pack and bake every mesh using `material`; writes ATLAS. Called by
    tools/author_lagoon_cove.py on every build, and by main() on any saved file."""
    meshes = rock_meshes(material)
    grid = max(1, math.ceil(math.sqrt(len(meshes))))
    print(f"[rock-edges] {len(meshes)} rock meshes, {grid} x {grid} atlas")
    for i, me in enumerate(meshes):
        unwrap_into_cell(me, i, grid)
    image = bpy.data.images.new("rock_edges_atlas", ATLAS_PX, ATLAS_PX, alpha=False)
    image.generated_color = (0, 0, 1, 1)          # no edge, no shoulder, no occlusion
    image.colorspace_settings.name = "Non-Color"  # masks, stored linear
    scene = bpy.context.scene
    engine = scene.render.engine
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 32   # G and B only; R is computed from geometry (distance_to_breaks)
    scene.cycles.device = "CPU"
    bm = bake_material(image)
    for i, me in enumerate(meshes):
        tmp_me = me.copy()
        tmp_me.materials.clear()
        tmp_me.materials.append(bm)
        tmp = bpy.data.objects.new("_bake", tmp_me)
        scene.collection.objects.link(tmp)
        for o in bpy.context.view_layer.objects:
            o.select_set(False)
        tmp.select_set(True)
        bpy.context.view_layer.objects.active = tmp
        bpy.ops.object.bake(type="EMIT", margin=6, use_clear=(i == 0), uv_layer="UVBake")
        bpy.data.objects.remove(tmp)
        bpy.data.meshes.remove(tmp_me)
        print("[rock-edges] baked", me.name)
    scene.render.engine = engine
    bpy.data.materials.remove(bm)
    distance_to_breaks(image, meshes)
    ATLAS.parent.mkdir(parents=True, exist_ok=True)
    image.filepath_raw = str(ATLAS)
    image.file_format = "PNG"
    image.save()
    print("[rock-edges] atlas", ATLAS)


DIST_MAX = 0.25        # stone units: R falls LINEARLY from 1 on a break to 0 at this distance
BREAK_DEG = 20.0       # a convex edge sharper than this is a plane break. 12 caught every
                       # small facet and brought back the rejected paving look (v17)
TOP_NZ = 0.3           # and at least one of its faces must point UP: the owner's paint-over
                       # lines the perimeter of the TOP faces, not side-to-side breaks


def distance_to_breaks(image, meshes):
    """THE R CHANNEL, COMPUTED FROM GEOMETRY. The Cycles bevel-normal detector peaked at a
    different strength on every break (a 40 degree break never reached 1), so it could not be
    read as a distance and a WORLD-size band on it failed (owner on v15: bands "too thick and
    bulky" on big rocks; the first fix then lost them). Here each texel of a stone's UVBake
    island gets its true distance, in stone units, to the nearest convex plane break, and
    R = 1 - distance / DIST_MAX: exactly linear, so the material converts a width in metres
    with the stone's scale. Pure numpy over the UV triangles."""
    import bmesh
    import numpy as np
    W = H = ATLAS_PX
    px = np.empty(W * H * 4, np.float32)
    image.pixels.foreach_get(px)
    px = px.reshape(H, W, 4)
    red = np.zeros((H, W), np.float32)
    covered = np.zeros((H, W), bool)
    for me in meshes:
        bm = bmesh.new()
        bm.from_mesh(me)
        bm.faces.ensure_lookup_table()
        segs = []
        for e in bm.edges:
            if (len(e.link_faces) == 2 and e.is_convex and math.degrees(e.calc_face_angle(0)) > BREAK_DEG
                    and max(f.normal.z for f in e.link_faces) > TOP_NZ):
                segs.append((tuple(e.verts[0].co), tuple(e.verts[1].co)))
        if not segs:
            bm.free()
            continue
        A = np.array([a for a, _b in segs], np.float32)
        D = np.array([b for _a, b in segs], np.float32) - A
        DD = np.maximum((D * D).sum(1), 1e-12)
        uv_layer = bm.loops.layers.uv["UVBake"]
        bmesh.ops.triangulate(bm, faces=bm.faces[:])
        for f in bm.faces:
            uv = np.array([tuple(l[uv_layer].uv) for l in f.loops], np.float32) * [W, H]
            co = np.array([tuple(l.vert.co) for l in f.loops], np.float32)
            x0, y0 = np.floor(uv.min(0)).astype(int) - 1
            x1, y1 = np.ceil(uv.max(0)).astype(int) + 1
            x0, y0, x1, y1 = max(x0, 0), max(y0, 0), min(x1, W - 1), min(y1, H - 1)
            gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
            (ax, ay), (bx, by), (cx, cy) = uv
            den = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
            if abs(den) < 1e-9:
                continue
            l0 = ((by - cy) * (gx - cx) + (cx - bx) * (gy - cy)) / den
            l1 = ((cy - ay) * (gx - cx) + (ax - cx) * (gy - cy)) / den
            l2 = 1 - l0 - l1
            inside = (l0 >= -0.02) & (l1 >= -0.02) & (l2 >= -0.02)
            if not inside.any():
                continue
            P = (l0[inside, None] * co[0] + l1[inside, None] * co[1] + l2[inside, None] * co[2])
            t = np.clip(((P[:, None, :] - A[None]) * D[None]).sum(2) / DD[None], 0, 1)
            near = A[None] + t[..., None] * D[None]
            d = np.sqrt(((P[:, None, :] - near) ** 2).sum(2)).min(1)
            ys, xs = np.nonzero(inside)
            ys, xs = ys + y0, xs + x0
            red[ys, xs] = np.maximum(red[ys, xs], np.clip(1 - d / DIST_MAX, 0, 1))
            covered[ys, xs] = True
        bm.free()
    # Bleed a few texels past each island so bilinear sampling at its border stays correct.
    for _ in range(6):
        grown = red.copy()
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            shifted = np.roll(np.roll(red, dy, 0), dx, 1)
            sc = np.roll(np.roll(covered, dy, 0), dx, 1)
            take = (~covered) & sc
            grown[take] = np.maximum(grown[take], shifted[take])
        newly = (~covered) & (grown > 0)
        covered |= np.roll(covered, 1, 0) | np.roll(covered, -1, 0) | np.roll(covered, 1, 1) | np.roll(covered, -1, 1)
        red = grown
    px[..., 0] = red
    image.pixels.foreach_set(px.ravel())
    print("[rock-edges] distance field written (R)")


if __name__ == "__main__":
    main()
