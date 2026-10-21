"""The arena, assembled: every kit appended into one file, and review pictures of the whole.

    blender -b --python tools/author_arena_assembly.py -- --version=v1 [--layout=tore]

Kits (docs/ARENA_ART_BRIEF.md): bowl, roof, hull, city, holo, stage. Each is already in the stadium's
frame, so assembling is appending. The crowd is drawn in Unity (sprites), not here. Writes
ArtSource/arena/arena_assembly.blend and pictures to Logs/arena/assembly/.
"""
import bpy, math, os, sys
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
KITS = os.path.join(ROOT, "ArtSource", "arena", "kits")
OUT = os.path.join(ROOT, "Logs", "arena", "assembly")
EYE = 3.3


def arg(name, default):
    for a in sys.argv:
        if a.startswith("--%s=" % name):
            return a.split("=", 1)[1]
    return default


def append(kit, names):
    path = os.path.join(KITS, kit + ".blend")
    with bpy.data.libraries.load(path, link=False) as (src, dst):
        have = list(src.collections)
        dst.collections = [n for n in names if n in have]
        missing = [n for n in names if n not in have]
    for c in dst.collections:
        if c is not None and c.name not in [k.name for k in bpy.context.scene.collection.children]:
            bpy.context.scene.collection.children.link(c)
    print("APPEND %-6s %s%s" % (kit, [c.name for c in dst.collections if c], ("  MISSING %s; has %s" % (missing, have)) if missing else ""))
    return [c for c in dst.collections if c]


def polar(r, b, z=0.0):
    a = math.radians(b)
    return Vector((r * math.sin(a), r * math.cos(a), z))


def shoot(name, loc, target, lens):
    scene = bpy.context.scene
    cam_data = bpy.data.cameras.new(name)
    cam = bpy.data.objects.new(name, cam_data)
    scene.collection.objects.link(cam)
    cam.location = loc
    cam.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
    cam_data.lens = lens; cam_data.clip_start = 0.3; cam_data.clip_end = 8000
    scene.camera = cam
    scene.render.filepath = os.path.join(OUT, name + ".png")
    bpy.ops.render.render(write_still=True)


def main():
    version, layout = arg("version", "v1"), arg("layout", "tore")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    append("bowl", ["arena_bowl"]); append("roof", ["arena_roof"]); append("hull", ["arena_hull"])
    append("city", ["arena_city", "review (not the kit)"])
    append("holo", ["arena_holo"])                                # the hologram ads and the slipper: light and one balloon
    for c in append("stage", ["arena_stage"]):
        for sub in c.children:
            show = sub.name in ("stage_" + layout,) or sub.name == "stage_props" and False
            sub.hide_render = not show; sub.hide_viewport = not show
            for deep in sub.children:
                deep.hide_render = not show; deep.hide_viewport = not show
        for ob in c.objects:
            if "preview" in ob.name or "hologram" in ob.name:
                ob.hide_render = True
    tris = 0
    for ob in scene.objects:
        if ob.type == "MESH" and not ob.hide_render:
            tris += sum(len(p.vertices) - 2 for p in ob.data.polygons)
    print("ASSEMBLY objects %d, visible triangles %d, materials %d" % (len(scene.objects), tris, len(bpy.data.materials)))

    if not any(o.type == "LIGHT" for o in scene.objects):
        key = bpy.data.objects.new("floodlight key", bpy.data.lights.new("floodlight key", "SUN"))
        scene.collection.objects.link(key)
    for o in scene.objects:                                       # one cool key, as the floodlights
        if o.type == "LIGHT":
            o.data.type = "SUN"; o.data.energy = 2.6; o.data.color = (0.86, 0.93, 1.0)
            o.rotation_euler = (math.radians(12), math.radians(8), 0)
    if scene.world is None:
        scene.world = bpy.data.worlds.new("night")
    scene.world.use_nodes = True
    bg = scene.world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[0].default_value = (0.010, 0.016, 0.060, 1); bg.inputs[1].default_value = 1.0
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    scene.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1920, 1080
    scene.view_settings.view_transform = "Standard"
    for screen in bpy.data.screens:
        for area in screen.areas:
            for space in area.spaces:
                if space.type == "VIEW_3D":
                    space.clip_start = 1.0; space.clip_end = 8000.0

    os.makedirs(OUT, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT, "ArtSource", "arena", "arena_assembly.blend"))
    p = "arena_%s_" % version
    for label, b in (("eye_n", 0), ("eye_ne", 45), ("eye_e", 90), ("eye_se", 135), ("eye_s", 180), ("eye_sw", 225)):
        t = polar(100.0, b, EYE + 30.0)
        start = polar(9.0, b + 180, EYE + 1.6)                    # just behind a player by the can
        shoot(p + label, start, t, 16)
    shoot(p + "game_attacker", (0, 14.5, 4.6), (0, -4, 1.5), 22)   # roughly the game camera behind an attacker
    shoot(p + "upper_stand", (0, -168, 56), (0, 10, 6), 22)
    shoot(p + "corner_concourse", (96, -96, 31), (-40, 60, 12), 18)
    shoot(p + "shaft_down", (10, -10, 2.0), (2, -2, -70), 16)
    # The city came in (2026-10-05: towers from radius 365): these two cameras stood where towers T09 and
    # T08 now do, so they stand in the south avenue of sky instead (bearing 200, clear from the hull out).
    shoot(p + "air", polar(600.0, 199.5, 300.0), (0, 0, 20), 24)
    shoot(p + "air_far", (900, -1300, 420), (0, 0, -40), 30)
    shoot(p + "under", polar(420.0, 200.0, -210.0), (0, 0, -30), 20)
    shoot(p + "plaza", (150, -190, 16), (60, -120, 20), 20)
    print("ASSEMBLY_OK")


if __name__ == "__main__":
    main()
