"""Assemble the Ilalim ng Tulay rebuild from its kits into one scene (ILALIM-1.3, assembly).

  blender -b --python tools/author_ilalim_city.py -- [--preview N] [--shots a,b]

Writes ArtSource/ilalim/ilalim_city.blend. With --preview it writes versioned review renders to
Logs/ilalim-blender/city_<shot>_vN.png.

Owner, 2026-09-29: "lets proceed with the rest of the map. spin up parallel agents to develop
different aspects of the map in parallel, same workflow that was done with the lagooncove map".
Eight kits were built in parallel, each in its own script and .blend, and the sari-sari store
after them (owner: "can you put a sari sari store somewhere"):

  kit              script                                 placed collections
  LRT-1 guideway   tools/author_ilalim_lrt.py            guideway over the court
  Rizal Hall       tools/author_ilalim_rizal_hall.py     rizal_hall, rizal_oblation
  heritage (west)  tools/author_ilalim_heritage.py       heritage west of Taft
  east side        tools/author_ilalim_eastside.py       eastside
  streets, ground  tools/author_ilalim_street.py         street ground, markings, median, furniture...
  trees            tools/author_ilalim_trees.py          trees over Ilalim
  gameplay props   tools/author_ilalim_props.py          props (placed), column signs
  sari-sari store  tools/author_ilalim_sarisari.py       sarisari (placed)
  train, vehicles  tools/author_ilalim_train.py, _vehicles.py   lrt_train, veh_*

HOW IT ASSEMBLES. Every kit's top-level PLACED collections are LINKED (library links), so a kit
is fixed in its own file and this scene picks the change up on the next run. Prototype, review
and stand-in collections are left out by name. On top of the links, this script adds only what
no kit owns:
  * the spider lilies in the median planter: the street kit leaves 432 placeholder clumps in one
    object ("median plant placeholders"); each clump becomes a linked duplicate of one of the
    trees kit's tree_lily prototypes, and the placeholders are hidden;
  * the LRT-1 train on the deck, north of the court (a collection instance at the rail head);
  * traffic: the vehicles kit's cars as collection instances in the lanes the street kit leaves
    clear of the pier collars (Taft x about +/-1.9, outside |y| = 16.5; Padre Faura westbound);
  * the light: a warm late-afternoon sun low from the west (guide section 0.4) and a sky.
"""
import math
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource" / "ilalim"
PREVIEWS = ROOT / "Logs" / "ilalim-blender"
KITS = ["lrt_kit", "rizal_hall", "heritage", "eastside", "street", "trees", "props", "sarisari"]
SKIP = ("prototype", "review", "stand-in", "kit (", "(source", "(place on piers)")
# The props kit's pier signs, placed on the piers, live in a review-named collection. Only that
# one: its prototypes, "prop_column_signs (place on piers)", sit at the origin for the Unity
# builder and showed up in the middle of the court (owner: "extra sign in this play area").
ALWAYS = ("review placement (column signs)",)
RAIL_HEAD = 9.19


def link_kit(name, parent):
    path = SOURCE / f"{name}.blend"
    with bpy.data.libraries.load(str(path), link=True, relative=True) as (src, dst):
        dst.collections = list(src.collections)
    linked = [c for c in dst.collections if c is not None]
    children = {ch.name for c in linked for ch in c.children}
    placed = []
    for c in linked:
        lower = c.name.lower()
        keep = c.name in ALWAYS or (c.name not in children and not any(s in lower for s in SKIP))
        if keep:
            parent.children.link(c)
            placed.append(c.name)
    print(f"[ilalim-city] {name}: linked {placed}")
    return linked


def lilies(parent, street_cols, tree_cols):
    """Replace the street kit's placeholder clumps with the trees kit's lily prototypes."""
    holder = None
    for c in street_cols:
        for o in c.all_objects:
            if o.name.startswith("median plant placeholders"):
                holder = o
    protos = [c for c in tree_cols if c.name in ("tree_lily_0", "tree_lily_1", "tree_lily_2")]
    if holder is None or not protos:
        print("[ilalim-city] lilies: placeholder or prototypes missing, skipped")
        return
    bm = bmesh.new()
    bm.from_mesh(holder.data)
    bm.verts.ensure_lookup_table()
    islands, seen = [], set()
    for v in bm.verts:
        if v.index in seen:
            continue
        stack, part = [v], []
        seen.add(v.index)
        while stack:
            cur = stack.pop()
            part.append(cur.co.copy())
            for e in cur.link_edges:
                w = e.other_vert(cur)
                if w.index not in seen:
                    seen.add(w.index)
                    stack.append(w)
        islands.append(part)
    bm.free()
    col = bpy.data.collections.new("median lilies (from placeholders)")
    parent.children.link(col)
    mw = holder.matrix_world
    for k, part in enumerate(islands):
        xs = [p.x for p in part]
        ys = [p.y for p in part]
        zs = [p.z for p in part]
        at = mw @ Vector(((min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2, min(zs)))
        inst = bpy.data.objects.new(f"lily {k}", None)
        inst.instance_type = "COLLECTION"
        inst.instance_collection = protos[k % len(protos)]
        inst.location = at
        inst.rotation_euler = (0, 0, (k * 2.399) % math.tau)
        col.objects.link(inst)
    holder.hide_render = True
    holder.hide_viewport = True
    print(f"[ilalim-city] lilies: {len(islands)} clumps placed")


def instance(parent, source_blend, name, at, rot_z=0.0):
    col = bpy.data.collections.get(name)
    if col is None:
        with bpy.data.libraries.load(str(SOURCE / source_blend), link=True, relative=True) as (src, dst):
            dst.collections = [name]
        col = dst.collections[0]
    inst = bpy.data.objects.new(f"{name} @ {at[0]:.0f},{at[1]:.0f}", None)
    inst.instance_type = "COLLECTION"
    inst.instance_collection = col
    inst.location = at
    inst.rotation_euler = (0, 0, rot_z)
    parent.objects.link(inst)


def traffic(parent):
    col = bpy.data.collections.new("traffic (instances)")
    parent.children.link(col)
    north, south, west = math.pi / 2, -math.pi / 2, math.pi
    # Taft: two lanes between the pier collars, never inside |y| = 16.5.
    instance(col, "vehicles.blend", "veh_jeepney_taft", (1.9, -27.0, 0.0), north)
    instance(col, "vehicles.blend", "veh_bus_liner", (-1.9, -48.0, 0.0), south)
    instance(col, "vehicles.blend", "veh_uv_express", (1.9, -63.0, 0.0), north)
    instance(col, "vehicles.blend", "veh_jeepney_green", (-1.9, 52.0, 0.0), south)
    instance(col, "vehicles.blend", "veh_taxi", (1.9, 58.0, 0.0), north)
    # Padre Faura, one-way west.
    instance(col, "vehicles.blend", "veh_tricycle", (-24.0, 30.5, 0.0), west)
    instance(col, "vehicles.blend", "veh_sedan_grey", (-42.0, 33.0, 0.0), west)
    instance(col, "vehicles.blend", "veh_hatch_maroon", (26.0, 30.0, 0.0), west)


def train(parent):
    col = bpy.data.collections.new("lrt train (instance)")
    parent.children.link(col)
    instance(col, "train.blend", "lrt_train", (2.35, 38.0, RAIL_HEAD), 0.0)


def lighting():
    scene = bpy.context.scene
    world = bpy.data.worlds.new("sky")
    scene.world = world
    if world.node_tree is None:
        world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (0.60, 0.74, 0.92, 1)
    bg.inputs["Strength"].default_value = 0.7
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy, sun.data.angle, sun.data.color = 4.6, math.radians(2.5), (1.0, 0.86, 0.68)
    # Late afternoon, low from the west-south-west: light travels east and a little north.
    sun.rotation_euler = Vector((0.86, 0.22, -0.46)).normalized().to_track_quat("-Z", "Y").to_euler()
    scene.collection.objects.link(sun)
    scene.view_settings.view_transform = "AgX"
    try:
        scene.view_settings.look = "AgX - Medium High Contrast"
    except TypeError:
        pass
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type == "VIEW_3D":
                for space in area.spaces:
                    if space.type == "VIEW_3D":
                        space.shading.type = "MATERIAL"
                        # The road top is z = 0, exactly where the viewport floor grid draws, and a
                        # 0.01 m near clip against a 3 km far clip leaves the depth buffer about
                        # 6 cm of precision at aerial distances: the owner saw the grid and the
                        # ground z-fighting ("z fighting on ground plane"). Renders use the camera
                        # clip and were clean; this fixes the viewport the file opens with.
                        space.clip_start = 0.1
                        space.clip_end = 3000
                        space.overlay.show_floor = False
                        space.overlay.show_axis_x = False
                        space.overlay.show_axis_y = False


def preview(version, only):
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 1000
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    cam.data.clip_end = 3000
    scene.collection.objects.link(cam)
    scene.camera = cam
    eye = 18 / math.tan(math.radians(95 / 2))
    shots = [
        ("spawn_north", Vector((0.0, -9.0, 1.25)), Vector((0, 30, 4)), eye),
        ("taya_south", Vector((0.0, 4.0, 1.25)), Vector((0, -30, 2)), eye),
        ("west_pavement_east", Vector((-9.5, -4.0, 1.46)), Vector((12, 6, 3)), eye),
        ("east_pavement_west", Vector((9.3, 2.0, 1.46)), Vector((-14, -4, 3)), eye),
        ("spawn_to_rizal", Vector((3.0, -9.0, 1.25)), Vector((-88, 60, 9)), eye),
        ("court_to_rizal", Vector((-8.0, 16.0, 1.46)), Vector((-88, 60, 9)), eye),
        ("spawn_to_sarisari", Vector((0.0, -9.0, 1.25)), Vector((15, 40, 2.6)), eye),
        ("court_to_sarisari", Vector((9.3, 14.0, 1.46)), Vector((15, 40, 2.6)), eye),
        ("spawn_tele_sarisari", Vector((0.0, -9.0, 1.25)), Vector((15, 40, 2.6)), 50),
        ("hoop_to_sarisari", Vector((-6.0, -10.0, 1.25)), Vector((15, 40, 2.6)), eye),
        ("taya_tele_sarisari", Vector((0.0, 4.0, 1.25)), Vector((15, 40, 2.6)), 50),
        ("sarisari_close", Vector((11.5, 31.0, 1.6)), Vector((14.8, 39.5, 2.5)), 20),
        ("aerial_nw", Vector((48.0, -62.0, 52.0)), Vector((-30, 30, 4)), 24),
        ("aerial_se", Vector((-70.0, 70.0, 48.0)), Vector((6, -6, 4)), 24),
        ("plan", Vector((-20.0, 10.0, 420.0)), Vector((-20, 10, 0)), None),
    ]
    for name, pos, tgt, lens in shots:
        if only and name not in only:
            continue
        existing = PREVIEWS / f"city_{name}_v{version}.png"
        if existing.exists():
            print("[ilalim-city] exists, skipped", existing)
            continue
        if lens is None:
            cam.data.type, cam.data.ortho_scale = "ORTHO", 300
        else:
            cam.data.type, cam.data.lens = "PERSP", lens
        cam.location = pos
        cam.rotation_euler = (tgt - pos).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = str(existing)
        bpy.ops.render.render(write_still=True)
        print("[ilalim-city] preview", existing)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
    only = set(argv[argv.index("--shots") + 1].split(",")) if "--shots" in argv else set()
    bpy.ops.wm.read_factory_settings(use_empty=True)
    SOURCE.mkdir(parents=True, exist_ok=True)
    out = SOURCE / "ilalim_city.blend"
    # Save first, so the library links are written relative to this file.
    bpy.ops.wm.save_as_mainfile(filepath=str(out))
    root = bpy.data.collections.new("ilalim_city")
    bpy.context.scene.collection.children.link(root)
    linked = {k: link_kit(k, root) for k in KITS}
    lilies(root, linked["street"], linked["trees"])
    train(root)
    traffic(root)
    lighting()
    bpy.ops.wm.save_as_mainfile(filepath=str(out), compress=True, relative_remap=True)
    backup = SOURCE / "ilalim_city.blend1"
    if backup.exists():
        backup.unlink()
    print("[ilalim-city] saved", out)
    if version:
        preview(version, only)


if __name__ == "__main__":
    main()
