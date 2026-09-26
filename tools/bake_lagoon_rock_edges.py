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
ATLAS_PX = 2048
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
    pad = 0.04
    cx, cy = index % grid, index // grid
    uv = me.uv_layers["UVBake"].data
    for d in uv:
        u, v = d.uv
        d.uv = ((cx + pad + u * (1 - 2 * pad)) / grid, (cy + pad + v * (1 - 2 * pad)) / grid)


def bake_material(image):
    m = bpy.data.materials.new("_edge_bake")
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    emit = nt.nodes.new("ShaderNodeEmission")
    nt.links.new(emit.outputs["Emission"], out.inputs["Surface"])
    geo = nt.nodes.new("ShaderNodeNewGeometry")
    bevel = nt.nodes.new("ShaderNodeBevel")
    bevel.samples = 16
    bevel.inputs["Radius"].default_value = BEVEL_R
    dot = nt.nodes.new("ShaderNodeVectorMath")
    dot.operation = "DOT_PRODUCT"
    nt.links.new(bevel.outputs["Normal"], dot.inputs[0])
    nt.links.new(geo.outputs["Normal"], dot.inputs[1])
    band = nt.nodes.new("ShaderNodeMapRange")        # dot 1 = flat plane, lower = an edge
    band.inputs["From Min"].default_value, band.inputs["From Max"].default_value = 0.995, 0.93   # a soft ramp in, not a line
    nt.links.new(dot.outputs["Value"], band.inputs["Value"])
    convex = nt.nodes.new("ShaderNodeMapRange")      # pointiness > 0.5 is convex
    convex.inputs["From Min"].default_value, convex.inputs["From Max"].default_value = 0.49, 0.53
    nt.links.new(geo.outputs["Pointiness"], convex.inputs["Value"])
    mul = nt.nodes.new("ShaderNodeMath")
    mul.operation = "MULTIPLY"
    nt.links.new(band.outputs["Result"], mul.inputs[0])
    nt.links.new(convex.outputs["Result"], mul.inputs[1])
    nt.links.new(mul.outputs["Value"], emit.inputs["Strength"])
    emit.inputs["Color"].default_value = (1, 1, 1, 1)
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
    image.generated_color = (0, 0, 0, 1)
    scene = bpy.context.scene
    engine = scene.render.engine
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 16
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
    ATLAS.parent.mkdir(parents=True, exist_ok=True)
    image.filepath_raw = str(ATLAS)
    image.file_format = "PNG"
    image.save()
    print("[rock-edges] atlas", ATLAS)


if __name__ == "__main__":
    main()
