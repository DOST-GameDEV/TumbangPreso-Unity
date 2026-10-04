"""The arena grey-box, drawn in Blender from the same numbers Unity builds from.

    blender -b --python tools/view_arena_greybox.py -- --version=v1   (writes the .blend and PNGs)
    blender ArtSource/arena/arena_greybox.blend                       (to look at it)

Reads tools/arena_greybox_layout.json (Unity coordinates: x right, y up, z forward, metres) and
lays the layouts side by side, one collection each. Nothing here is exported: the game's scene is
built by `ArenaSceneBuilder` from that same file, so this view cannot drift from what is played.
"""
import bpy, bmesh, json, math, os, sys
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DATA = json.load(open(os.path.join(ROOT, "tools", "arena_greybox_layout.json"), encoding="utf-8"))
OUT = os.path.join(ROOT, "ArtSource", "arena")
GAP = 66.0                                  # metres between the layouts' centres (the stadium is 58 across)
COLUMNS = 3

COLOURS = {
    "slab": (0.55, 0.56, 0.58), "dais": (0.72, 0.70, 0.62), "ramp": (0.38, 0.47, 0.60),
    "bridge": (0.36, 0.40, 0.44), "pit": (0.05, 0.05, 0.07), "wall": (0.9, 0.9, 0.9),
    "jump": (0.20, 0.85, 0.75), "speed": (0.95, 0.85, 0.15), "pickup": (0.45, 0.95, 0.35),
    "can": (0.80, 0.80, 0.84), "taya": (0.85, 0.20, 0.55), "attacker": (0.55, 0.30, 0.85),
    "box": (1.0, 1.0, 1.0),
    "stand": (0.42, 0.36, 0.33), "screen": (0.12, 0.16, 0.30), "booth": (0.65, 0.50, 0.25),
    "rig": (0.22, 0.22, 0.25), "tower": (0.30, 0.30, 0.34),
}
_mats = {}
RIG = []                                    # the overhead truss: hidden in the shots it would cover


def mat(name):
    if name not in _mats:
        m = bpy.data.materials.new("arena_" + name)
        c = COLOURS[name]
        m.diffuse_color = (c[0], c[1], c[2], 1.0)
        m.use_nodes = True
        b = m.node_tree.nodes.get("Principled BSDF")
        b.inputs["Base Color"].default_value = (c[0], c[1], c[2], 1.0)
        b.inputs["Roughness"].default_value = 0.9
        _mats[name] = m
    return _mats[name]


def unity_rot(v, euler):
    """Unity's Euler order: z, then x, then y, in degrees."""
    x, y, z = v
    ax, ay, az = (math.radians(a) for a in euler)
    c, s = math.cos(az), math.sin(az); x, y = c * x - s * y, s * x + c * y
    c, s = math.cos(ax), math.sin(ax); y, z = c * y - s * z, s * y + c * z
    c, s = math.cos(ay), math.sin(ay); x, z = c * x + s * z, -s * x + c * z
    return x, y, z


def to_blender(p, offset):
    return Vector((p[0] + offset[0], p[2] + offset[1], p[1]))        # Unity (x, y, z) is Blender (x, z, y)


def box(name, center, size, rotation, material, coll, offset):
    hx, hy, hz = (s * 0.5 for s in size)
    corners = [(sx * hx, sy * hy, sz * hz) for sx in (-1, 1) for sy in (-1, 1) for sz in (-1, 1)]
    verts = []
    for c in corners:
        r = unity_rot(c, rotation)
        verts.append(to_blender((r[0] + center[0], r[1] + center[1], r[2] + center[2]), offset))
    faces = [(0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)]
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.update()
    bm = bmesh.new(); bm.from_mesh(me)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me)
    me.materials.append(material)
    coll.objects.link(ob)
    return ob


def label(text, at, size, coll, offset):
    cu = bpy.data.curves.new(text, "FONT")
    cu.body = text; cu.size = size; cu.align_x = "CENTER"
    ob = bpy.data.objects.new("label " + text, cu)
    ob.location = to_blender(at, offset)
    cu.materials.append(mat("box"))
    coll.objects.link(ob)
    return ob


def centre_of(i):
    return (i % COLUMNS) * GAP, -(i // COLUMNS) * GAP


def build():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    top, half, pit = DATA["stageTop"], DATA["wallHalf"], DATA["pitY"]
    for i, lay in enumerate(DATA["layouts"]):
        off = centre_of(i)
        coll = bpy.data.collections.new("layout %d %s" % (i, lay["name"]))
        scene.collection.children.link(coll)
        box("pit", (0, pit - 0.1, 0), (2 * half, 0.2, 2 * half), (0, 0, 0), mat("pit"), coll, off)
        for sx, sz, w, d in ((1, 0, 0.1, 2 * half), (-1, 0, 0.1, 2 * half), (0, 1, 2 * half, 0.1), (0, -1, 2 * half, 0.1)):
            wall = box("wall", (sx * half, top + 1.0, sz * half), (w, 2.0, d), (0, 0, 0), mat("wall"), coll, off)
            wall.display_type = "WIRE"
            wall.hide_render = True
        for p in lay["pieces"]:
            box("%s (%s)" % (p["id"], p["kind"]), p["center"], p["size"], p.get("rotation", (0, 0, 0)),
                mat(p["kind"] if p["kind"] in COLOURS else "slab"), coll, off)
        can_y = top + lay.get("canHeight", 0.0)
        box("can", (0, can_y + 0.15, 0), (0.22, 0.3, 0.22), (0, 0, 0), mat("can"), coll, off)
        # The taya's box (`Confinement`): 7 m either side of the can, drawn as an outline.
        for sx, sz, w, d in ((1, 0, 0.08, 14), (-1, 0, 0.08, 14), (0, 1, 14, 0.08), (0, -1, 14, 0.08)):
            box("taya box", (sx * 7, can_y + 0.03, sz * 7), (w, 0.02, d), (0, 0, 0), mat("box"), coll, off)
        for s in DATA.get("surround", []):
            ob = box("%s (%s)" % (s["id"], s["kind"]), s["center"], s["size"], s.get("rotation", (0, 0, 0)), mat(s["kind"]), coll, off)
            if s["id"].startswith("rig") and not s["id"].endswith("Barrier"):
                RIG.append(ob)
        for j in lay.get("jumpPads", []):
            j = j["center"] if isinstance(j, dict) else j
            box("jump pad", (j[0], j[1] + 0.06, j[2]), (1.7, 0.12, 1.7), (0, 0, 0), mat("jump"), coll, off)
        for s in lay.get("speedPads", []):
            c = s["center"]; h = s["halfSize"]
            box("speed pad", (c[0], c[1] + 0.04, c[2]), (2 * h[0], 0.08, 2 * h[1]), (0, s.get("yaw", 0), 0), mat("speed"), coll, off)
        for k in lay.get("pickups", []):
            box("stamina pickup", (k[0], k[1] + 0.9, k[2]), (0.5, 0.5, 0.5), (45, 45, 0), mat("pickup"), coll, off)
        sp = DATA["spawns"]
        t = sp["taya"]
        box("taya spawn", (t[0], can_y + 0.9, t[2]), (0.6, 1.8, 0.6), (0, 0, 0), mat("taya"), coll, off)
        for a in sp["attackers"]:
            box("attacker spawn", (a[0], top + 0.9, a[2]), (0.6, 1.8, 0.6), (0, 0, 0), mat("attacker"), coll, off)
        label("%d  %s" % (i, lay["name"].upper()), (0, top + 0.1, -half + 0.3), 1.4, coll, off)

    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy = 3.0; sun.rotation_euler = (math.radians(50), 0, math.radians(30))
    scene.collection.objects.link(sun)
    world = bpy.data.worlds.new("w"); scene.world = world; world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.35, 0.37, 0.4, 1)
    return scene


def shoot(scene, name, loc, target, ortho=None):
    cam_data = bpy.data.cameras.new(name)
    cam = bpy.data.objects.new(name, cam_data)
    scene.collection.objects.link(cam)
    cam.location = loc
    d = Vector(target) - Vector(loc)
    cam.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
    cam_data.clip_end = 500
    if ortho:
        cam_data.type = "ORTHO"; cam_data.ortho_scale = ortho
    scene.camera = cam
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    scene.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1920, 1080
    scene.render.filepath = os.path.join(OUT, name + ".png")
    bpy.ops.render.render(write_still=True)


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    scene = build()
    n = len(DATA["layouts"])
    version = "v1"
    for a in sys.argv:
        if a.startswith("--version="):
            version = a.split("=", 1)[1]
    rows = (n + COLUMNS - 1) // COLUMNS
    mx, my = (COLUMNS - 1) * GAP * 0.5, -(rows - 1) * GAP * 0.5
    for ob in RIG:
        ob.hide_render = True
    shoot(scene, "arena_greybox_%s_plan" % version, (mx, my, 120), (mx, my, 0), ortho=COLUMNS * GAP)
    for i in range(n):
        cx, cy = centre_of(i)
        shoot(scene, "arena_greybox_%s_stage%d" % (version, i), (cx + 19, cy - 30, 24), (cx, cy + 1, -1))
    for ob in RIG:
        ob.hide_render = False
    cx, cy = centre_of(0)
    shoot(scene, "arena_greybox_%s_stadium" % version, (cx + 38, cy - 52, 34), (cx, cy + 4, 0))
    shoot(scene, "arena_greybox_%s_player" % version, (cx, cy - 11, 2.4), (cx, cy + 10, 3.0))
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "arena_greybox.blend"))
    print("ARENA_VIEW_OK")
