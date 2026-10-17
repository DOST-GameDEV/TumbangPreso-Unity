"""The CALM review renders for a SET of props, and for a group of calm pieces standing together. Blender 5.0 headless.
Same stage, light, ink line and cameras as the pilot's tools/payo_pieces/calm_review.py (its functions are loaded from
that file, which is not changed), so the props and guardians are judged exactly as the pilot was.

  blender -b --python tools/payo_pieces/prop_calm_review.py -- set <letter> <approved prop dir> <calm prop dir> <shots dir>
  blender -b --python tools/payo_pieces/prop_calm_review.py -- group <layout.json> <shots dir>

set: the set's props stood as the approved set review stands them (its setX.json: where, which way, how high), OLD
and CALM at 8, 40 and 120 m from the set's centre, then the calm close shot with the normal map off and the normal map
alone: shots set<x>_old_8m.png ... (the names tools/payo_pieces/calm_sheet.py piece mode reads).
group: layout.json {"prefix": "...", "dists": [[40, 9, 50], ...] (metres back, metres up, fov), "items": [{"old", "calm",
"x", "y", "yaw", "lift"}]}: shots <prefix>_old_<d>m.png and <prefix>_calm_<d>m.png.
"""
import bpy, json, math, os, sys
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
_src = open(os.path.join(HERE, "calm_review.py"), encoding="utf-8").read()
exec(compile(_src.split('\nif args[0] == "piece":')[0], os.path.join(HERE, "calm_review.py"), "exec"))   # stage, ink, load, normals, show, aim, shoot, DIR

argv = sys.argv[sys.argv.index("--") + 1:]


def load_at(path, x, y, lift, yaw):
    meshes, lo, hi = load(path, (x, y, lift))
    if yaw:
        for o in meshes:                                  # each prop object and its ink hull turn about the prop's origin
            if o.parent is None:
                o.rotation_mode = "XYZ"
                o.rotation_euler.z += math.radians(yaw)
        bpy.context.view_layer.update()
        lo, hi = Vector((1e9,) * 3), Vector((-1e9,) * 3)
        for o in meshes:
            for c in o.bound_box:
                p = o.matrix_world @ Vector(c)
                lo = Vector(map(min, lo, p)); hi = Vector(map(max, hi, p))
    return meshes, lo, hi


def do_set(letter, app, calm, shots):
    os.makedirs(shots, exist_ok=True)
    meta = json.load(open(os.path.join(app, "parts", "set%s.json" % letter)))
    sc, cam = stage()
    A, B = [], []
    lo, hi = Vector((1e9,) * 3), Vector((-1e9,) * 3)
    for p in meta["props"]:
        x, y, lift, yaw = p["at"][0], -p["at"][1], p.get("lift", 0.0), p.get("yaw", 0.0)
        a, l1, h1 = load_at(os.path.join(app, p["name"] + ".glb"), x, y, lift, yaw); A += a
        b, _, _ = load_at(os.path.join(calm, p["name"] + ".glb"), x, y, lift, yaw); B += b
        lo = Vector(map(min, lo, l1)); hi = Vector(map(max, hi, h1))
    name = "set" + letter
    Hh = hi.z
    c = Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, 0))
    rad = max(hi.x - lo.x, hi.y - lo.y) / 2
    views = {
        "8m": (c + DIR * (rad + 8.0) + Vector((0, 0, max(2.2, Hh * 0.6))), c + Vector((0, 0, Hh * 0.3)), 50),
        "40m": (c + DIR * 40.0 + Vector((0, 0, 6.0)), c + Vector((0, 0, Hh * 0.35)), 50),
        "120m": (c + DIR * 120.0 + Vector((0, 0, 18.0)), c + Vector((0, 0, Hh * 0.35)), 50),
    }
    for k, (eye, look, fov) in views.items():
        aim(cam, eye, look, fov)
        show(A, True); show(B, False); shoot(sc, os.path.join(shots, "%s_old_%s.png" % (name, k)))
        show(A, False); show(B, True); shoot(sc, os.path.join(shots, "%s_calm_%s.png" % (name, k)))
    # a closer look as well: the props are small, so 8 m from the set's edge is far for a jar
    dist = rad + 3.5
    fov = math.degrees(2 * math.atan(max(rad * 1.15 / 1.5, Hh * 0.6) / dist))   # framed to the set, the frame is 3:2
    eye, look = c + DIR * dist + Vector((0, 0, 1.6 + Hh * 0.3)), c + Vector((0, 0, Hh * 0.3))
    aim(cam, eye, look, fov)
    show(A, True); show(B, False); shoot(sc, os.path.join(shots, "%s_old_4m.png" % name))
    show(A, False); show(B, True); shoot(sc, os.path.join(shots, "%s_calm_4m.png" % name))
    normals(B, on=False); shoot(sc, os.path.join(shots, "%s_calm_4m_nonormal.png" % name))
    normals(B, on=True, grey=True); shoot(sc, os.path.join(shots, "%s_calm_4m_normalonly.png" % name))


def do_group(layout, shots):
    os.makedirs(shots, exist_ok=True)
    L = json.load(open(layout))
    sc, cam = stage(tuple(L.get("res", (1400, 700))))
    A, B = [], []
    for it in L["items"]:
        a, _, _ = load_at(it["old"], it["x"], it["y"], it.get("lift", 0.0), it.get("yaw", 0.0)); A += a
        b, _, _ = load_at(it["calm"], it["x"], it["y"], it.get("lift", 0.0), it.get("yaw", 0.0)); B += b
    look = Vector(L.get("look", (0, 0, 4.0)))
    for d, up, fov in L["dists"]:
        eye = look + Vector((0.25, -1, 0)).normalized() * d + Vector((0, 0, up))
        aim(cam, eye, look, fov)
        show(A, True); show(B, False); shoot(sc, os.path.join(shots, "%s_old_%dm.png" % (L["prefix"], d)))
        show(A, False); show(B, True); shoot(sc, os.path.join(shots, "%s_calm_%dm.png" % (L["prefix"], d)))


if argv[0] == "set":
    do_set(argv[1], argv[2], argv[3], argv[4])
else:
    do_group(argv[1], argv[2])
print("done")
