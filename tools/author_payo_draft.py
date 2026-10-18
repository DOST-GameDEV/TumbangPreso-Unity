"""PAYO: the DRAFT of the terrace map, second draft (owner 2026-10-12).

  blender -b --python tools/author_payo_draft.py -- --out=<abs folder> [--v=N] [--layout=0] [--only=air_a,eye_valley]
                                                     [--night] [--mayari=a|b] [--blend]

Reads tools/payo_layouts_draft.json (written by tools/draw_payo_draft_plans.py) and builds ONE layout as plain modelled
forms in flat colours. Writes payo_draft_<view>[_<layout>][_night]_vN.png. NOTHING HERE IS A GAME ASSET.

WHAT CHANGED FROM THE FIRST DRAFT, all from the owner's answers:
  - "i dont think the whole map should be in an enclosed basin space": the closed bowl is gone. The place is on a
    MOUNTAINSIDE. Terraces climb behind the gate (south) and round to the west; north and east fall away to a valley,
    a sea of cloud and far ridges. The pit's north wall is broken open to the valley.
  - THE STAGE STANDS 6 M ABOVE THE GROUND ROUND THE PIT (the lip is at y -6). From 1.7 m on the court that is what
    lets a player look DOWN onto paddy water and out over the valley; with the lip level with the court nothing
    below the horizon could be seen.
  - "you can try having smaller moving platforms": the layout's `movers` are drawn, part way along their travel.
  - "i meant the map changes to night at half time": --night builds the second half: moon, stars, shooting stars,
    fireflies, fire bowls, lit houses, and MAYARI in the sky over the valley (tools/draw_payo_mayari.py paints her).
  - bulul are rounded seated figures now; the gate is a great Ifugao house on carved posts over a dry stone platform.

AXES. The JSON is Unity (x right, y up, z forward). In here everything is Blender: X = Unity x, Y = Unity z (north),
Z = Unity y (up). The gate is SOUTH (-Y, what the attackers face from Y +9); the valley is NORTH and EAST.

HAZE IS IN THE MATERIALS: every material mixes toward the haze colour by distance from the camera, and toward the
mist colour by depth, inside the pit and far down the valley, through one shared node group set per view.
"""
import json
import math
import os
import random
import sys
from pathlib import Path

import bmesh
import bpy
import numpy as np
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parent.parent


def arg(name, default):
    return next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--" + name + "=")), default)


OUT, VER, LAYOUT, ONLY = arg("out", "."), arg("v", "1"), int(arg("layout", "0")), arg("only", "")
NIGHT, MAYARI = "--night" in sys.argv, arg("mayari", "a")
DATA = json.loads((ROOT / "tools/payo_layouts_draft.json").read_text(encoding="utf-8"))
LAY = DATA["layouts"][LAYOUT]
PIT, LIP, FLOOR = DATA["pitHalf"], DATA["pitLipHalf"], DATA["pitFloor"]
PLAZA_Y, LEDGE_Y, BREACH_Y, BREACH_HALF = -6.0, -10.0, -24.0, 12.4
bpy.ops.wm.read_factory_settings(use_empty=True)
rng = random.Random(7)


# ---------------------------------------------------------------- colour
def lin(hexs):
    v = [int(hexs[i:i + 2], 16) / 255.0 for i in (1, 3, 5)]
    return tuple(c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4 for c in v)


HAZE, MIST = (lin("#0f1a30"), lin("#31445f")) if NIGHT else (lin("#cbd8e4"), lin("#c3d2d4"))
fog = bpy.data.node_groups.new("PayoFog", "ShaderNodeTree")
fog.interface.new_socket("dist", in_out="OUTPUT", socket_type="NodeSocketFloat")
fog.interface.new_socket("pit", in_out="OUTPUT", socket_type="NodeSocketFloat")


def _fog():
    N, L = fog.nodes, fog.links
    out = N.new("NodeGroupOutput")
    cam, geo, sep = N.new("ShaderNodeCameraData"), N.new("ShaderNodeNewGeometry"), N.new("ShaderNodeSeparateXYZ")
    vals = {}
    for k in ("d0", "k", "cap"):
        vals[k] = N.new("ShaderNodeValue"); vals[k].name = k

    def m(op, a, b=None, clamp=False):
        n = N.new("ShaderNodeMath"); n.operation = op; n.use_clamp = clamp
        for i, s in enumerate((a, b)):
            if s is None:
                continue
            if isinstance(s, (int, float)):
                n.inputs[i].default_value = s
            else:
                L.new(s, n.inputs[i])
        return n.outputs[0]

    d = m("MAXIMUM", m("SUBTRACT", cam.outputs["View Distance"], vals["d0"].outputs[0]), 0.0)
    f = m("SUBTRACT", 1.0, m("EXPONENT", m("MULTIPLY", m("MULTIPLY", d, vals["k"].outputs[0]), -1.0)))
    L.new(m("MULTIPLY", f, vals["cap"].outputs[0], clamp=True), out.inputs["dist"])
    L.new(geo.outputs["Position"], sep.inputs[0])
    depth = m("MULTIPLY", sep.outputs["Z"], -1.0)
    inside = m("MULTIPLY", m("LESS_THAN", m("ABSOLUTE", sep.outputs["X"]), LIP + 0.4), m("LESS_THAN", m("ABSOLUTE", sep.outputs["Y"]), LIP + 0.4))
    pit = m("MULTIPLY", m("POWER", m("DIVIDE", m("SUBTRACT", depth, 15.0), 22.0, clamp=True), 0.8), inside)
    valley = m("DIVIDE", m("SUBTRACT", depth, 30.0), 34.0, clamp=True)
    L.new(m("MULTIPLY", m("MAXIMUM", pit, valley), 0.97, clamp=True), out.inputs["pit"])


_fog()
MATS = {}


def mat(name, hexs, rough=0.9, glow=0.0, fogged=True):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    N, L = m.node_tree.nodes, m.node_tree.links
    b = N["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*lin(hexs), 1)
    b.inputs["Roughness"].default_value = rough
    if glow:
        b.inputs["Emission Color"].default_value = (*lin(hexs), 1)
        b.inputs["Emission Strength"].default_value = glow
    cur = b.outputs[0]
    if fogged:
        g = N.new("ShaderNodeGroup"); g.node_tree = fog
        for key, col in (("pit", MIST), ("dist", HAZE)):
            e = N.new("ShaderNodeEmission"); e.inputs[0].default_value = (*col, 1)
            mx = N.new("ShaderNodeMixShader")
            L.new(g.outputs[key], mx.inputs[0]); L.new(cur, mx.inputs[1]); L.new(e.outputs[0], mx.inputs[2])
            cur = mx.outputs[0]
    L.new(cur, N["Material Output"].inputs[0])
    MATS[name] = m
    return name


# the stage: value steps so blocks read apart. Tops pale, a paler rim, sides darker in two alternating courses
CAP_A, CAP_B, CAP_C = mat("cap_court", "#b3bcc3"), mat("cap_outer", "#a1acb5"), mat("cap_stone", "#939fa3")
RIM, SHAFT, SHAFT2, DARK = mat("rim", "#d2d8db"), mat("shaft", "#6b7780"), mat("shaft2", "#5e6a74"), mat("band", "#434d56")
LINE, PAVE = mat("inlay", "#6b757d"), mat("pave", "#8e989f")
WALL, WALLM, DIKE = mat("terrace_wall", "#79838a"), mat("terrace_wall_moss", "#66795c"), mat("dike", "#8a949b")
PITW, PITW2, PITD = mat("pit_wall", "#69737a"), mat("pit_wall2", "#5f6a72"), mat("pit_dark", "#48525a")
ROCK = mat("rock", "#737b7f")
MOSS, STREAK, VINE = mat("moss", "#5f7d3a"), mat("wet", "#3c444a"), mat("vine", "#3f6a2e")
WATER = mat("paddy_water", "#b9d3d6", rough=0.1)
RICE, RICE2, RIPE, MUD = mat("rice_young", "#8cc24a"), mat("rice_grown", "#6aa83e"), mat("rice_ripe", "#c2b54c"), mat("mud", "#7d6c52")
GRASS = mat("grass_lip", "#6f9a40")
FALL, FOAM = mat("waterfall", "#cfe3e8", rough=0.4, glow=0.10), mat("foam", "#f2f8f8", rough=0.6, glow=0.22)
TEMPLE, TEMPLE2, PORTAL = mat("gate_stone", "#8a959e"), mat("gate_stone_dark", "#657079"), mat("portal", "#1c1f22")
THATCH, THATCH2, WOOD, WOOD2 = mat("thatch", "#8f7240"), mat("thatch_dark", "#74592f"), mat("wood", "#5b4030"), mat("wood_dark", "#3d2a20")
BULUL = mat("bulul", "#4d4a49")
RED, OCHRE, CREAM, INK = mat("cloth_red", "#9c2f26"), mat("cloth_ochre", "#c9962e"), mat("cloth_cream", "#e6dfca"), mat("cloth_black", "#27262a")
J1, J2, J3, J4, HILL = mat("jungle1", "#2f5a2c"), mat("jungle2", "#3f7034"), mat("jungle3", "#254a2b"), mat("jungle4", "#527f38"), mat("hill", "#4b7a48")
RIDGES = [mat("ridge%d" % i, h) for i, h in enumerate(("#4d7460", "#4f7474", "#587b8c", "#6787a0", "#7a96b0"))]
CLOUD = mat("cloud", "#eef2f4", glow=0.0 if NIGHT else 0.25)
SKIN, CAN, STEAM = mat("skin", "#8a5a3c"), mat("can", "#c9ccd0", rough=0.4), mat("steam", "#e3f0ec", glow=0.4)
CARABAO = mat("carabao", "#3f3c3f")
MOVER, MOVERU = mat("mover_top", "#9fb7ba"), mat("mover_under", "#4f6468")
FIRE, EMBER = mat("fire", "#ffab2e", glow=2.6, fogged=False), mat("window", "#ffa640", glow=1.6, fogged=False)
FLY = mat("firefly", "#b9f23c", glow=3.0, fogged=False)
SHOOT = mat("shooting_star", "#dfe8ff", glow=2.2, fogged=False)
DAYMOON = mat("day_moon", "#e4ebf0", glow=0.55, fogged=False)

# ---------------------------------------------------------------- mesh builder: one mesh per material
GEO = {}
LAMPS = []


def _g(m):
    return GEO.setdefault(m, ([], []))


def quad(m, a, b, c, d):
    v, f = _g(m)
    n = len(v)
    v.extend((tuple(a), tuple(b), tuple(c), tuple(d)))
    f.append((n, n + 1, n + 2, n + 3))


BOXF = ((0, 2, 3, 1), (4, 5, 7, 6), (0, 1, 5, 4), (2, 6, 7, 3), (0, 4, 6, 2), (1, 3, 7, 5))


def box(m, x0, x1, y0, y1, z0, z1, M=None):
    v, f = _g(m)
    n = len(v)
    for x in (x0, x1):
        for y in (y0, y1):
            for z in (z0, z1):
                v.append(tuple(M @ Vector((x, y, z))) if M else (x, y, z))
    f.extend(tuple(n + i for i in q) for q in BOXF)


def cbox(m, cx, cy, sx, sy, z0, z1, M=None):
    box(m, cx - sx / 2, cx + sx / 2, cy - sy / 2, cy + sy / 2, z0, z1, M)


def prism(m, cx, cy, z0, z1, r0, r1=None, n=4, turn=None, sy=1.0, M=None):
    """An n sided prism or (r1 = 0) a pyramid. Square ones sit axis aligned."""
    r1 = r0 if r1 is None else r1
    turn = (math.pi / n) if turn is None else turn
    v, f = _g(m)
    b = len(v)
    for (r, z) in ((r0, z0), (r1, z1)):
        for k in range(n):
            a = turn + 2 * math.pi * k / n
            p = Vector((cx + r * math.cos(a), cy + r * math.sin(a) * sy, z))
            v.append(tuple(M @ p) if M else tuple(p))
    for k in range(n):
        k2 = (k + 1) % n
        f.append((b + k, b + k2, b + n + k2, b + n + k))
    f.append(tuple(b + n + k for k in range(n)))
    f.append(tuple(b + k for k in reversed(range(n))))


def limb(m, p0, p1, r0, r1, M=None, n=8, flat=1.0):
    """A tapered round bar from p0 to p1. `flat` squashes it front to back (local Y)."""
    p0, p1 = Vector(p0), Vector(p1)
    a = (p1 - p0).normalized()
    ref = Vector((1, 0, 0)) if abs(a.x) < 0.9 else Vector((0, 1, 0))
    e1 = a.cross(ref).normalized()
    e2 = a.cross(e1).normalized()
    v, f = _g(m)
    b = len(v)
    for (p, r) in ((p0, r0), (p1, r1)):
        for k in range(n):
            t = 2 * math.pi * k / n
            q = p + (e1 * math.cos(t) + e2 * math.sin(t)) * r
            q.y = p.y + (q.y - p.y) * flat
            v.append(tuple(M @ q) if M else tuple(q))
    for k in range(n):
        k2 = (k + 1) % n
        f.append((b + k, b + k2, b + n + k2, b + n + k))
    f.append(tuple(b + n + k for k in range(n)))
    f.append(tuple(b + k for k in reversed(range(n))))


_ico = bmesh.new()
bmesh.ops.create_icosphere(_ico, subdivisions=2, radius=1.0)
ICO = ([tuple(v.co) for v in _ico.verts], [tuple(v.index for v in f.verts) for f in _ico.faces])
_ico.free()


def blob(m, cx, cy, cz, rx, ry=None, rz=None, M=None):
    ry, rz = rx if ry is None else ry, rx if rz is None else rz
    v, f = _g(m)
    n = len(v)
    for p in ICO[0]:
        q = Vector((cx + p[0] * rx, cy + p[1] * ry, cz + p[2] * rz))
        v.append(tuple(M @ q) if M else tuple(q))
    f.extend(tuple(n + i for i in q) for q in ICO[1])


def flush():
    for m, (v, f) in GEO.items():
        me = bpy.data.meshes.new(m)
        me.from_pydata(v, [], f)
        me.materials.append(MATS[m])
        bpy.context.scene.collection.objects.link(bpy.data.objects.new(m, me))


def place(x, y, z, yaw):
    return Matrix.Translation((x, y, z)) @ Matrix.Rotation(yaw, 4, "Z")


# ---------------------------------------------------------------- the mountainside
# ONE MOUNTAIN, FROM ONE HEIGHT FUNCTION (third pass: the owner on the second, "you plan on fixing that weird cliff
# edge right?"). The second pass built terraces along rays from the can and switched abruptly between climbing and
# falling ground, which left sliced faces. Now the ground is h(x, y):
#   - a plaza round the pit, then a slope that climbs to the south south west and falls to the north north east;
#   - round the uphill side the slope also climbs away from the pit in rings, so the contours there are a horseshoe
#     (the amphitheatre), and straighten out across the slope lower down;
#   - TERRACES ARE THE SAME GROUND, STEPPED: z = h pulled toward a staircase of 2.8 m levels by T, how terraced the
#     place is. T fades to nothing along the terraces' ends, so walls get lower and die into forest; nothing is cut;
#   - under the pit's north wall the ground is a rock face and a gorge, not terraces;
#   - the same function, coarser and without steps, is the far mountain, the valley and the ranges across it.
STEP = 2.8
UPX, UPY = math.cos(math.radians(250.0)), math.sin(math.radians(250.0))
SHELF_X = [35.0, 51.0, 62.0, 73.0, 84.0, 95.0, 106.0]
SHELF_Y = -1.5
CLOUD_Y = -50.0


def shelf_span(x):
    return -13.0 - 0.12 * (x - 35.0), 29.0 + 0.10 * (x - 35.0)


def shelf_z(x):
    k = max(0, min(5, sum(1 for b in SHELF_X[1:6] if x >= b)))
    return SHELF_Y + 0.5 * k


def sm(a, b, x):
    t = np.clip((x - a) / (b - a), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def nz(x, y, k):
    return (np.sin(x + 1.7 * k) * np.cos(y * 1.3 - 0.9 * k) + 0.5 * np.sin(x * 2.3 - y * 1.9 + k) + 0.25 * np.cos(x * 4.1 + y * 3.7 + 2 * k)) / 1.75


def field(x, y, far=False):
    """The ground at (x, y), arrays in, a dict of arrays out: z (the surface), h (before stepping), e (metres outside
    the plaza), T (how terraced), rock, n and f (terrace level and where across its tread)."""
    x, y = np.asarray(x, dtype=float), np.asarray(y, dtype=float)
    r = np.hypot(x, y) + 1e-6
    rs = (np.abs(x) ** 8 + np.abs(y) ** 8) ** 0.125
    e = np.maximum(0.0, rs - 41.0)
    c = (x * UPX + y * UPY) / r                               # +1 straight uphill, -1 straight downhill
    wup = sm(-0.15, 0.55, c)
    wdn = sm(0.05, 0.45, -c)
    g = 0.30 * c + 0.13 * wup
    es = np.where(g > 0, 520.0 * (1.0 - np.exp(-e / 520.0)), e)
    h = PLAZA_Y + es * g * (1.0 + 0.16 * nz(x / 90.0, y / 90.0, 1))
    h = h + 0.26 * np.maximum(0.0, es - 115.0) * wup * (1.0 + 0.5 * nz(x / 260.0, y / 260.0, 2))      # the mountain above
    h = h - 0.0009 * np.maximum(0.0, e - 60.0) ** 2 * sm(0.0, 0.6, -c)                                  # steeper lower down
    q = x * (-UPY) + y * UPX
    h = h - 0.0010 * np.maximum(0.0, np.abs(q) - 70.0) ** 2 * (1.0 - 0.85 * wup)                       # the spur's shoulders fall away
    # the rock under the pit's north wall, and the gorge the breach opens into
    north = y - 36.0
    cliff = sm(0.0, 5.0, north) * sm(78.0, 44.0, np.abs(x))
    h = h - cliff * np.clip(north, 0.0, 70.0) * (1.05 + 0.25 * nz(x / 13.0, y / 17.0, 4))
    gw = 13.0 + 0.22 * np.maximum(0.0, y - 33.0)
    gm = sm(gw + 8.0, gw, np.abs(x)) * sm(33.0, 34.5, y)
    hg = BREACH_Y - 1.7 * np.maximum(0.0, y - 35.0)
    h = h + gm * (np.minimum(h, hg) - h)
    rock = np.maximum(cliff, gm)
    # the gate's court, cut into the slope
    ing = (np.abs(x) < 24.0) & (y < -46.0) & (y > -99.0)
    h = np.where(ing, np.minimum(h, np.where(y > -58.0, -0.4, 5.6)), h)
    # the water shelf's mound, east (see shelf())
    lo, hi = shelf_span(np.clip(x, 35.0, 106.0))
    smask = sm(22.0, 34.0, x) * sm(134.0, 108.0, x) * sm(lo - 24.0, lo, y) * sm(hi + 24.0, hi, y)
    top = SHELF_Y + 0.045 * np.clip(x - 35.0, 0.0, 71.0) - 1.3
    h = np.where(e > 0, h + smask * (np.maximum(h, top) - h), h)
    onshelf = (x > 34.9) & (x < 106.0) & (y > lo) & (y < hi)
    T = np.zeros_like(h)
    hh = (h - PLAZA_Y) / STEP
    n = np.floor(hh)
    f = hh - n
    if far:
        s_down = -(x * UPX + y * UPY)
        m = np.zeros_like(h)
        for (si, wi, ai, k) in ((1080.0, 340.0, 300.0, 1), (1700.0, 430.0, 400.0, 2), (2550.0, 560.0, 560.0, 3), (3550.0, 700.0, 720.0, 4), (4800.0, 900.0, 900.0, 5)):
            ridge = 0.62 + 0.38 * nz(q / (wi * 1.5), s_down / 900.0, k) + 0.10 * nz(q / 160.0, s_down / 190.0, k + 3)
            m = m + ai * np.exp(-((s_down - si) / wi) ** 2) * ridge
        z = np.maximum(h, -230.0) + m
        return {"z": z, "h": h, "e": e, "T": T, "rock": rock, "n": n, "f": f, "s": s_down, "c": c}
    n3 = nz(x / 55.0, y / 55.0, 5)
    tup = wup * (1.0 - sm(86.0, 128.0, e + 22.0 * n3))
    tdn = wdn * (1.0 - sm(200.0, 290.0, e + 25.0 * n3))
    T = sm(0.30, 0.62, np.maximum(tup, tdn) + 0.18 * nz(x / 23.0, y / 23.0, 7))
    T = T * (1.0 - np.clip(rock * 1.6, 0.0, 1.0)) * np.where(ing | onshelf, 0.0, 1.0)
    W = 0.17
    zq = PLAZA_Y + STEP * (n + np.clip((f - (1.0 - W)) / W, 0.0, 1.0))
    z = h + T * (zq - h)
    z = np.where(onshelf, np.minimum(z, top), z)
    return {"z": z, "h": h, "e": e, "T": T, "rock": rock, "n": n, "f": f, "c": c, "shelf": onshelf, "gate": ing}


def gz(x, y):
    return float(field([x], [y])["z"][0])


def probe(x, y):
    F = field([x], [y])
    return {k: float(v[0]) for k, v in F.items()}


def downhill(x, y):
    """Unit vector straight down the slope at (x, y)."""
    F = field([x + 0.6, x - 0.6, x, x], [y, y, y + 0.6, y - 0.6])["h"]
    v = Vector((F[1] - F[0], F[3] - F[2], 0.0))
    return v.normalized() if v.length > 1e-6 else Vector((0, 1, 0))


def grid_mesh(name, xs, ys, Z, mi, keep, mats, tri=None):
    """`tri`, when given, is (flat material index per cell, steep material index per cell, flat threshold): each cell is
    cut in two along the diagonal whose ends are nearer in height (the one that runs along the contour), and each
    half takes the flat or the steep material by its own three corners. That is what keeps terrace walls clean."""
    N, Mx = len(ys), len(xs)
    X, Y = np.meshgrid(xs, ys)
    verts = np.stack([X, Y, Z], -1).reshape(-1, 3)
    idx = np.arange(N * Mx).reshape(N, Mx)
    a, b, c, d = idx[:-1, :-1], idx[:-1, 1:], idx[1:, 1:], idx[1:, :-1]
    me = bpy.data.meshes.new(name)
    if tri is None:
        quads = np.stack([a, b, c, d], -1)[keep]
        me.from_pydata(verts.tolist(), [], quads.tolist())
        index = mi[keep]
    else:
        mflat, msteep, thr = tri
        za, zb, zc, zd = Z[:-1, :-1], Z[:-1, 1:], Z[1:, 1:], Z[1:, :-1]
        ac = np.abs(za - zc) <= np.abs(zb - zd)
        t1 = np.where(ac[..., None], np.stack([a, b, c], -1), np.stack([a, b, d], -1))
        t2 = np.where(ac[..., None], np.stack([a, c, d], -1), np.stack([b, c, d], -1))
        s1 = np.where(ac, np.maximum.reduce([za, zb, zc]) - np.minimum.reduce([za, zb, zc]), np.maximum.reduce([za, zb, zd]) - np.minimum.reduce([za, zb, zd]))
        s2 = np.where(ac, np.maximum.reduce([za, zc, zd]) - np.minimum.reduce([za, zc, zd]), np.maximum.reduce([zb, zc, zd]) - np.minimum.reduce([zb, zc, zd]))
        m1 = np.where(mi >= 0, mi, np.where(s1 < thr, mflat, msteep))
        m2 = np.where(mi >= 0, mi, np.where(s2 < thr, mflat, msteep))
        tris = np.concatenate([t1[keep], t2[keep]], 0)
        index = np.concatenate([m1[keep], m2[keep]], 0)
        me.from_pydata(verts.tolist(), [], tris.tolist())
    for m in mats:
        me.materials.append(MATS[m])
    me.polygons.foreach_set("material_index", index.astype(np.int32))
    me.update()
    bpy.context.scene.collection.objects.link(bpy.data.objects.new(name, me))


def corner_spread(Z):
    c = np.stack([Z[:-1, :-1], Z[:-1, 1:], Z[1:, 1:], Z[1:, :-1]], 0)
    return c.max(0) - c.min(0)


def ground():
    """The near ground: 1.4 m cells, 350 m out. The cell lines fall on the pit's lip (33.6 = 24 cells)."""
    xs = np.arange(-250, 251) * 1.4
    X, Y = np.meshgrid(xs, xs)
    Z = field(X, Y)["z"]
    cx, cy = (X[:-1, :-1] + X[1:, 1:]) / 2, (Y[:-1, :-1] + Y[1:, 1:]) / 2
    F = field(cx, cy)
    dz = corner_spread(Z)
    mats = [PAVE, ROCK, PITW2, WALL, WALLM, WATER, RICE, RICE2, RIPE, MUD, GRASS, HILL, J1, J2, J3, DIKE]
    I = {m: i for i, m in enumerate(mats)}
    flat = dz < 0.5
    T, n = F["T"], F["n"]
    q = cx * (-UPY) + cy * UPX
    along = q / 12.0 + 2.3 * np.sin(n * 1.7)
    fid = np.floor(along)
    fr = along - fid
    hs = np.abs(np.sin(n * 12.9898 + fid * 78.233) * 43758.5453) % 1.0
    hs2 = np.abs(np.sin(n * 39.3 + np.floor(along * 0.6) * 11.1) * 9631.7) % 1.0
    nj = nz(cx / 31.0, cy / 27.0, 9)
    crop = np.select([hs < 0.46, hs < 0.72, hs < 0.86, hs < 0.95], [I[WATER], I[RICE], I[RICE2], I[RIPE]], I[MUD])
    forest_floor = np.select([nj < -0.25, nj < 0.3], [I[J3], I[J1]], I[J2])
    terr = T > 0.5
    thin = (T > 0.2) & ~terr                                   # the last thin paddies, gone to grass
    mflat = np.where(terr, np.where(fr < 0.08, I[DIKE], crop), np.where(thin, np.where(nj > 0, I[GRASS], I[HILL]), forest_floor))
    msteep = np.where(terr, np.where(hs2 < 0.42, I[WALLM], I[WALL]), np.where(thin, I[WALLM], forest_floor))
    rocky = np.where(nz(cx / 6.0, cy / 9.0, 3) > 0.1, I[PITW2], I[ROCK])
    mflat = np.where(F["gate"], I[PAVE], mflat)
    msteep = np.where(F["gate"], I[WALL], msteep)
    msteep = np.where(~terr & ~thin & (dz > 1.5), I[ROCK], msteep)
    fixed = np.full(T.shape, -1)
    fixed = np.where(F["rock"] > 0.3, rocky, fixed)
    fixed = np.where(F["e"] <= 0.0, I[PAVE], fixed)
    keep = ~((np.abs(cx) < 33.6) & (np.abs(cy) < 33.6))
    grid_mesh("ground", xs, xs, Z, fixed, keep, mats, tri=(mflat, msteep, 0.55))


def far_ground():
    """Everything beyond: the mountain above, the valley, five ranges across it. 30 m cells out to 6 km."""
    xs = np.arange(-200, 201) * 30.0
    X, Y = np.meshgrid(xs, xs)
    F = field(X, Y, far=True)
    Z = np.where((np.abs(X) < 340) & (np.abs(Y) < 340), F["z"] - 6.0, F["z"])
    cx, cy = (X[:-1, :-1] + X[1:, 1:]) / 2, (Y[:-1, :-1] + Y[1:, 1:]) / 2
    C = field(cx, cy, far=True)
    dz = corner_spread(Z)
    mats = [J1, J2, J3, J4, ROCK] + RIDGES + [RICE2, WALL, J4]
    I = {m: i for i, m in enumerate(mats)}
    nj = nz(cx / 170.0, cy / 140.0, 11)
    mi = np.select([nj < -0.3, nj < 0.2, nj < 0.6], [I[J3], I[J1], I[J2]], I[J4])
    mi = np.where(dz > 34.0, I[ROCK], mi)
    s = C["s"]
    tone = np.digitize(s, [1400.0, 2100.0, 3050.0, 4150.0])
    across = s > 760.0
    rid = np.array([I[m] for m in RIDGES])[tone]
    zc = (Z[:-1, :-1] + Z[1:, 1:]) / 2
    band = np.floor(zc / 9.0 + 0.6 * nz(cx / 120.0, cy / 120.0, 6)) % 4
    patch = nz(cx / 300.0, cy / 260.0, 8) > -0.15                 # the nearest range is terraced in patches, as ours is
    near = across & (tone == 0) & patch & (dz < 30.0) & (zc > CLOUD_Y)
    mi = np.where(across, rid, mi)
    mi = np.where(near, np.select([band == 0, band == 2], [len(mats) - 3, len(mats) - 1], I[RIDGES[0]]), mi)
    keep = np.ones_like(mi, dtype=bool)
    grid_mesh("far_ground", xs, xs, Z, mi, keep, mats)
    # the sea of cloud in the valley, with banks standing out of it
    for k in range(48):
        a0, a1 = 2 * math.pi * k / 48, 2 * math.pi * (k + 1) / 48
        quad(CLOUD, (0, 0, CLOUD_Y), (7000 * math.cos(a0), 7000 * math.sin(a0), CLOUD_Y), (7000 * math.cos(a1), 7000 * math.sin(a1), CLOUD_Y), (0, 0, CLOUD_Y))
    n = 0
    while n < 260:
        a, r = rng.uniform(0, 6.28), rng.uniform(150, 4200) ** 1.0
        x, y = r * math.cos(a), r * math.sin(a)
        if float(field([x], [y], far=True)["z"][0]) > CLOUD_Y - 6.0:
            n += 0.2
            continue
        sz = rng.uniform(34, 90) * (1 + r / 1300.0)
        blob(CLOUD, x, y, CLOUD_Y + rng.uniform(-3, 5), sz, sz * rng.uniform(0.6, 1.0), sz * rng.uniform(0.16, 0.3))
        n += 1


def tree(x, y, z, s, tones=(J1, J2, J3, J4)):
    """A broad crown in three lobes of different greens on a leaning trunk: reads as a tree, not a ball."""
    lean = rng.uniform(-0.25, 0.25) * s
    limb(WOOD, (x, y, z), (x + lean, y + lean * 0.5, z + s * 1.5), s * 0.14, s * 0.09, n=6)
    for k in range(rng.randint(3, 4)):
        a = rng.uniform(0, 6.28)
        o = s * rng.uniform(0.35, 0.7)
        r = s * rng.uniform(0.55, 0.85)
        blob(rng.choice(tones), x + lean + o * math.cos(a), y + o * math.sin(a), z + s * (1.6 + 0.35 * k), r, r, r * 0.55)


def palm(x, y, z, h):
    """Tree fern or betel palm: a thin leaning trunk, a star of drooping fronds."""
    lx, ly = rng.uniform(-0.12, 0.12) * h, rng.uniform(-0.12, 0.12) * h
    limb(WOOD2, (x, y, z), (x + lx, y + ly, z + h), 0.16, 0.10, n=6)
    top = Vector((x + lx, y + ly, z + h))
    n = 8
    for k in range(n):
        a = 2 * math.pi * k / n + rng.uniform(-0.2, 0.2)
        d = Vector((math.cos(a), math.sin(a), 0))
        side = Vector((-d.y, d.x, 0)) * 0.42
        L = h * 0.42
        p1, p2 = top + d * L * 0.55 + Vector((0, 0, 0.5)), top + d * L - Vector((0, 0, 0.7))
        quad(J4 if k % 2 else J2, top - side * 0.3, top + side * 0.3, p1 + side, p1 - side)
        quad(J2 if k % 2 else J1, p1 - side, p1 + side, p2 + side * 0.2, p2 - side * 0.2)


def bamboo(x, y, z, h):
    for k in range(rng.randint(5, 8)):
        a, o = rng.uniform(0, 6.28), rng.uniform(0.1, 0.7)
        lx, ly = math.cos(a) * h * 0.16, math.sin(a) * h * 0.16
        bx, by = x + o * math.cos(a), y + o * math.sin(a)
        hh = h * rng.uniform(0.75, 1.0)
        limb(J4, (bx, by, z), (bx + lx, by + ly, z + hh), 0.09, 0.04, n=5)
        blob(J2 if k % 2 else J4, bx + lx * 1.1, by + ly * 1.1, z + hh * 0.9, hh * 0.13, hh * 0.13, hh * 0.2)


def forest():
    """Canopy wherever the ground is not terraced: many crowns in four greens, thick where the terraces die out."""
    P = np.array([[rng.uniform(-345, 345), rng.uniform(-345, 345)] for _ in range(42000)])
    F = field(P[:, 0], P[:, 1])
    ok = (F["T"] < 0.3) & (F["e"] > 2.5) & (F["rock"] < 0.25) & ~F["gate"] & ~F["shelf"] & (F["z"] > CLOUD_Y - 12)
    near = 0
    for (x, y), z, good, T in zip(P.tolist(), F["z"].tolist(), ok.tolist(), F["T"].tolist()):
        if not good:
            continue
        d = math.hypot(x, y)
        if d < 150 and near < 320 and rng.random() < 0.5:
            near += 1
            k = rng.random()
            if k < 0.5:
                tree(x, y, z - 0.3, rng.uniform(2.2, 4.4))
            elif k < 0.78:
                palm(x, y, z - 0.2, rng.uniform(5.0, 9.0))
            else:
                bamboo(x, y, z - 0.2, rng.uniform(5.0, 9.0))
            continue
        sz = rng.uniform(2.8, 6.2)
        blob(rng.choice((J1, J2, J3, J4)), x, y, z + sz * 0.35, sz, sz, sz * rng.uniform(0.5, 0.78))
    # boulders and a few last trees where the terraces thin out
    P = np.array([[rng.uniform(-300, 300), rng.uniform(-300, 300)] for _ in range(9000)])
    F = field(P[:, 0], P[:, 1])
    edge = (F["T"] > 0.15) & (F["T"] < 0.6) & (F["e"] > 3)
    k = 0
    for (x, y), z, good in zip(P.tolist(), F["z"].tolist(), edge.tolist()):
        if not good or k > 420:
            continue
        k += 1
        if k % 3 == 0:
            sz = rng.uniform(0.9, 2.6)
            blob(ROCK, x, y, z, sz, sz * rng.uniform(0.7, 1.2), sz * 0.6)
        elif k % 3 == 1:
            tree(x, y, z - 0.3, rng.uniform(1.8, 3.6))
        else:
            palm(x, y, z - 0.2, rng.uniform(4.5, 8.0))
    # palms and trees on the paddies' lips
    P = np.array([[rng.uniform(-200, 200), rng.uniform(-200, 240)] for _ in range(9000)])
    F = field(P[:, 0], P[:, 1])
    lipm = (F["T"] > 0.8) & (F["f"] < 0.07) & (F["e"] > 4)
    k = 0
    for (x, y), z, good in zip(P.tolist(), F["z"].tolist(), lipm.tolist()):
        if not good or k > 110:
            continue
        k += 1
        (palm if k % 2 else tree)(x, y, z, rng.uniform(5.0, 8.0) if k % 2 else rng.uniform(1.6, 2.8))
    # the mountain above and the slopes below, beyond the near ground: bigger crowns
    n = 0
    while n < 9000:
        x, y = rng.uniform(-1300, 1300), rng.uniform(-1300, 1300)
        if abs(x) < 340 and abs(y) < 340:
            continue
        C = field([x], [y], far=True)
        z = float(C["z"][0])
        n += 1
        if z < CLOUD_Y - 4 or float(C["s"][0]) > 700:
            continue
        sz = rng.uniform(7, 15) * (1.0 + math.hypot(x, y) / 1400.0)
        blob(rng.choice((J1, J2, J3, J4)), x, y, z + sz * 0.3, sz, sz, sz * rng.uniform(0.5, 0.75))


def shelf():
    """THE WATER SHELF, east. Level water is only seen from above, and from 1.7 m on the court nothing lower than
    about 1.7 - 0.15 x distance shows past an outer block. So here six broad paddies lie 1.5 m under the court at the
    pit's lip and step UP half a metre each, held by a stone wall on a mound: bands of sky from the court's east edge."""
    for k in range(6):
        x0, x1 = SHELF_X[k], SHELF_X[k + 1]
        z = SHELF_Y + 0.5 * k
        (a0, b0), (a1, b1) = shelf_span(x0), shelf_span(x1)
        nf = 3 + (k % 2)
        for q in range(nf):
            t0, t1 = q / nf, (q + 1) / nf
            crop = WATER if (k < 3 or rng.random() < 0.6) else rng.choice((RICE, RICE2, RIPE))
            if (k, q) in ((1, 1), (2, 0)):
                crop = RICE
            p = [(x0 + 0.6, a0 + (b0 - a0) * t0 + 0.4, z), (x1, a1 + (b1 - a1) * t0 + 0.4, z), (x1, a1 + (b1 - a1) * t1 - 0.4, z), (x0 + 0.6, a0 + (b0 - a0) * t1 - 0.4, z)]
            quad(crop, *p)
            if crop is not WATER:
                quad(crop, *[(a, b, c + 0.6) for (a, b, c) in p])
                for i in range(4):
                    u, v = p[i], p[(i + 1) % 4]
                    quad(crop, u, v, (v[0], v[1], v[2] + 0.6), (u[0], u[1], u[2] + 0.6))
            for tt in (t0, t1):                               # cross dikes
                ya, yb = a0 + (b0 - a0) * tt, a1 + (b1 - a1) * tt
                quad(DIKE, (x0, ya - 0.4, z + 0.3), (x1, yb - 0.4, z + 0.3), (x1, yb + 0.4, z + 0.3), (x0, ya + 0.4, z + 0.3))
                quad(DIKE, (x0, ya - 0.4, z - 0.3), (x1, yb - 0.4, z - 0.3), (x1, yb - 0.4, z + 0.3), (x0, ya - 0.4, z + 0.3))
                quad(DIKE, (x0, ya + 0.4, z + 0.3), (x1, yb + 0.4, z + 0.3), (x1, yb + 0.4, z - 0.3), (x0, ya + 0.4, z - 0.3))
        box(DIKE, x0, x0 + 0.6, a0, b0, z - 0.8, z + 0.3)        # the riser between paddies, its top a dike
        for (ya, yb, sg) in ((a0, a1, -1), (b0, b1, 1)):          # the retaining wall along both sides, down into the mound
            quad(WALL if k % 2 else WALLM, (x0, ya + sg * 0.4, z + 0.3), (x1, yb + sg * 0.4, z + 0.3), (x1, yb + sg * 0.4, z - 9.0), (x0, ya + sg * 0.4, z - 9.0))
    a, b = shelf_span(SHELF_X[6])
    box(WALL, SHELF_X[6], SHELF_X[6] + 0.6, a - 0.4, b + 0.4, SHELF_Y - 9.0, SHELF_Y + 2.8)


def snap_flat(x, y):
    """The nearest spot to (x, y), up or down the slope, in the middle of a paddy. (x, y, z)."""
    d = downhill(x, y)
    best = None
    for k in range(-16, 17):
        px, py = x + d.x * k * 0.5, y + d.y * k * 0.5
        P = probe(px, py)
        score = abs(P["f"] - 0.42) + (0 if P["T"] > 0.7 else 5)
        if best is None or score < best[0]:
            best = (score, px, py, P["z"])
    return best[1], best[2], best[3]


def paddy_points(n, rmax=150.0, uphill=None):
    """n random spots standing in terraced paddies."""
    out = []
    while len(out) < n:
        x, y = rng.uniform(-rmax, rmax), rng.uniform(-rmax, rmax)
        P = probe(x, y)
        if P["T"] > 0.85 and 0.2 < P["f"] < 0.65 and P["e"] > 3 and (uphill is None or (P["c"] > 0.2) == uphill):
            out.append((x, y, P["z"]))
    return out


# ---------------------------------------------------------------- water
def fall(x, y, yaw, ztop, zbot, w, spread=1.0):
    """A waterfall with some body: a thick sheet, brighter ropes of water down it, a lip, foam and spray at the foot.
    Local +Y points away from the wall it falls down."""
    T = place(x, y, 0, yaw)
    h = ztop - zbot
    box(FALL, -w / 2, w / 2, -0.05, 0.4, zbot, ztop, T)
    if spread > 1.0:
        box(FALL, -w * spread / 2, w * spread / 2, 0.1, 0.6, zbot, zbot + h * 0.45, T)
    r = random.Random(int(x * 13 + y * 7 + ztop))
    for k in range(max(2, int(w / 1.1))):
        cx = -w / 2 + (k + 0.5) * w / max(2, int(w / 1.1)) + r.uniform(-0.2, 0.2)
        ww = r.uniform(0.18, 0.4)
        z1 = ztop - r.uniform(0, h * 0.25)
        box(FOAM, cx - ww, cx + ww, 0.38, 0.52, max(zbot, z1 - r.uniform(h * 0.3, h * 0.9)), z1, T)
    blob(FALL, 0, 0.15, ztop, w * 0.5, 0.5, 0.28, T)
    for k in range(max(3, int(w * 1.2))):
        s = r.uniform(0.35, 0.8) * min(1.6, 0.6 + h / 14)
        blob(FOAM, r.uniform(-w * spread / 2, w * spread / 2), r.uniform(0.2, 1.2), zbot + s * 0.3, s, s, s * 0.6, T)


def stream(x, y, width=3.0, to_pit=True, steps=420):
    """Water running straight down the slope from (x, y): wherever the ground steps down a wall, a fall. If it reaches
    the plaza it crosses it and goes into the pit."""
    z0 = gz(x, y)
    for _ in range(steps):
        d = downhill(x, y)
        nx, ny = x + d.x * 0.7, y + d.y * 0.7
        P = probe(nx, ny)
        if P["e"] <= 0.0:
            if to_pit:
                r = math.hypot(nx, ny)
                pit_fall(nx / r, ny / r, width, r)
            return
        if z0 - P["z"] > 1.0:
            fall((x + nx) / 2, (y + ny) / 2, math.atan2(d.y, d.x) - math.pi / 2, z0 + 0.25, P["z"] + 0.06, width)
        elif P["T"] > 0.5:
            box(WATER, nx - width * 0.3, nx + width * 0.3, ny - width * 0.3, ny + width * 0.3, P["z"] - 0.2, P["z"] + 0.05)
        if P["z"] < CLOUD_Y - 8:
            return
        x, y, z0 = nx, ny, P["z"]


def spills(n=14):
    """Paddies overflow to the one below through notches in the dikes: narrow falls on the walls, seen from below."""
    for (x, y, z) in paddy_points(n, 135.0, uphill=True):
        stream(x, y, rng.uniform(1.1, 1.8), to_pit=False, steps=10)


def pit_fall(c, s, width, r_from):
    """A channel across the plaza along direction (c, s), then the fall over the lip, the ledge, and into the mist."""
    h = width / 2
    if abs(c) >= abs(s):
        sg = 1 if c > 0 else -1
        y = s / abs(c) * LIP
        box(WATER, min(sg * LIP, sg * r_from * abs(c)), max(sg * LIP, sg * r_from * abs(c)), y - h, y + h, PLAZA_Y - 0.3, PLAZA_Y + 0.04)
        fall(sg * LIP, y, -sg * math.pi / 2 + math.pi, PLAZA_Y + 0.05, LEDGE_Y, width)
        box(FALL, min(sg * PIT, sg * LIP), max(sg * PIT, sg * LIP), y - h, y + h, LEDGE_Y, LEDGE_Y + 0.06)
        fall(sg * PIT, y, -sg * math.pi / 2 + math.pi, LEDGE_Y + 0.05, FLOOR, width * 0.9)
    else:
        sg = 1 if s > 0 else -1
        x = c / abs(s) * LIP
        box(WATER, x - h, x + h, min(sg * LIP, sg * r_from * abs(s)), max(sg * LIP, sg * r_from * abs(s)), PLAZA_Y - 0.3, PLAZA_Y + 0.04)
        fall(x, sg * LIP, 0.0 if sg < 0 else math.pi, PLAZA_Y + 0.05, LEDGE_Y, width)
        box(FALL, x - h, x + h, min(sg * PIT, sg * LIP), max(sg * PIT, sg * LIP), LEDGE_Y, LEDGE_Y + 0.06)
        fall(x, sg * PIT, 0.0 if sg < 0 else math.pi, LEDGE_Y + 0.05, FLOOR, width * 0.9)


def valley_falls():
    """The water leaves by the north: out of spouts in the broken wall's two ends into the breach, down the gorge
    from the breach's sill, and over the brink either side, all of it going down the rock into the cloud."""
    for sx in (-1, 1):
        x0, x1 = sorted((sx * 35.0, sx * 37.6))
        box(PAVE, x0 - 0.3, x1 + 0.3, -30, 36.6, PLAZA_Y - 1.6, PLAZA_Y - 0.25)       # flumes along the plaza's sides
        box(WATER, x0, x1, -30, 36.6, PLAZA_Y - 0.25, PLAZA_Y - 0.05)
        fall(sx * 36.3, 36.6, 0.0, PLAZA_Y - 0.05, gz(sx * 36.3, 60.0) - 6.0, 2.6, spread=2.4)
        box(DARK, sx * 13.4 - 0.9, sx * 13.4 + 0.9, 31.6, 34.4, 0.2, 1.0)                # a carved spout in each broken end
        fall(sx * 12.6, 33.0, sx * math.pi / 2, 0.9, BREACH_Y, 2.4)
    fall(0.0, LIP + 0.9, 0.0, BREACH_Y + 0.1, CLOUD_Y - 26.0, 9.0, spread=2.4)
    box(WATER, -5.0, 5.0, PIT - 0.5, LIP + 1.0, BREACH_Y - 0.4, BREACH_Y + 0.08)
    for k in range(16):                                    # spray standing in the gorge
        sz = rng.uniform(3, 8)
        yy = rng.uniform(40, 80)
        blob(CLOUD, rng.uniform(-12, 12), yy, gz(0.0, yy) + rng.uniform(2, 9), sz, sz, sz * 0.5)


# ---------------------------------------------------------------- the pit
def pit():
    def wall(horiz, sg, m, a, t0, t1, z0, z1, proud=0.0, thick=0.6):
        lo, hi = sorted((sg * (a - proud), sg * (a + thick)))
        if horiz:
            box(m, lo, hi, t0, t1, z0, z1)
        else:
            box(m, t0, t1, lo, hi, z0, z1)

    for sg in (-1, 1):
        for horiz in (True, False):
            north = (not horiz) and sg > 0
            spans = [(-LIP, -BREACH_HALF), (BREACH_HALF, LIP)] if north else [(-LIP, LIP)]
            for (t0, t1) in spans:
                p0, p1 = max(t0, -PIT), min(t1, PIT)
                wall(horiz, sg, PITW, LIP, t0, t1, LEDGE_Y - 0.6, PLAZA_Y - 0.001)               # the upper step
                wall(horiz, sg, RIM, LIP, t0, t1, PLAZA_Y - 0.35, PLAZA_Y + 0.25, proud=0.25)       # a pale kerb round the lip
                wall(horiz, sg, PAVE, PIT, t0, t1, LEDGE_Y - 0.6, LEDGE_Y, thick=LIP - PIT + 0.3)  # the ledge
                z = LEDGE_Y - 0.001
                k = 0
                while z > FLOOR:                                                                  # the sheer wall, in courses
                    zn = max(FLOOR, z - 3.2)
                    wall(horiz, sg, PITW if k % 2 else PITW2, PIT, p0, p1, zn, z, proud=0.05 if k % 2 else 0.0)
                    z, k = zn, k + 1
                wall(horiz, sg, PITD, PIT, p0, p1, -13.4, -12.2, proud=0.22)                       # carved bands
                wall(horiz, sg, PITD, PIT, p0, p1, -20.6, -19.8, proud=0.2)
                wall(horiz, sg, PITD, LIP, t0, t1, -8.3, -7.6, proud=0.12)
                span = p1 - p0
                for q in range(int(span / 3.0)):                                                  # cloth coloured tiles on the band
                    if q % 3 == 0:
                        c = p0 + 1.5 + q * 3.0
                        wall(horiz, sg, RED if q % 6 == 0 else OCHRE, PIT, c - 0.45, c + 0.45, -13.2, -12.4, proud=0.28)
                n = max(2, int(span / 2.2))
                for _ in range(n):                                                                # moss, vines, wet streaks
                    t = rng.uniform(p0 + 0.5, p1 - 3.3)
                    wall(horiz, sg, MOSS, PIT, t, t + rng.uniform(0.8, 3.2), LEDGE_Y - rng.uniform(1.0, 5.5), LEDGE_Y - 0.6, proud=0.12)
                for _ in range(int(n * 0.7)):
                    t = rng.uniform(p0 + 0.5, p1 - 1)
                    wall(horiz, sg, VINE, PIT, t, t + rng.uniform(0.25, 0.5), LEDGE_Y - rng.uniform(5, 19), LEDGE_Y + 0.1, proud=0.32)
                for _ in range(int(n * 0.5)):
                    t = rng.uniform(p0 + 0.5, p1 - 2)
                    wall(horiz, sg, STREAK, PIT, t, t + rng.uniform(0.5, 1.4), -rng.uniform(26, 40), -13.5, proud=0.08)
                for _ in range(int(n * 0.35)):
                    t = rng.uniform(t0 + 0.5, t1 - 4.5)
                    wall(horiz, sg, MOSS, LIP, t, t + rng.uniform(1, 4), PLAZA_Y - rng.uniform(1.0, 3.2), PLAZA_Y - 0.4, proud=0.1)
    # THE NORTH WALL STANDS ABOVE THE COURT, broken: from the court the lip is below every sight line, so a breach
    # at the lip could never be seen. Two stubs of a great wall rise 8 m over the court either side of the notch, and
    # the valley is seen THROUGH it.
    prof = ((33.0, 28.0, 8.0), (28.0, 24.5, 9.2), (24.5, 21.0, 7.0), (21.0, 18.0, 8.2), (18.0, 15.6, 4.6), (15.6, 13.6, 1.0), (13.6, 12.4, -3.0))
    for sx in (-1, 1):
        for q, (xa, xb, top) in enumerate(prof):
            x0, x1 = sorted((sx * xa, sx * xb))
            z, k = LEDGE_Y, 0
            while z < top:
                zn = min(top, z + 1.7)
                inset = 0.0 if k % 2 else 0.12
                box(PITW if (k + q) % 2 else PITW2, x0, x1, 30.6 + inset, 35.0 - inset, z, zn)
                z, k = zn, k + 1
            box(RIM, x0 - 0.05, x1 + 0.05, 30.45, 35.15, top, top + 0.3)
            if q in (1, 3):
                box(RIM, x0 + 0.6, x1 - 0.6, 30.9, 34.7, top + 0.3, top + 1.5)             # merlons left standing
            for _ in range(3):
                t = rng.uniform(x0, x1 - 1.0)
                w = rng.uniform(0.6, 1.8)
                box(MOSS, t, min(x1, t + w), 30.5, 30.62, top - rng.uniform(0.8, 4.5), top + 0.05)
                tv = rng.uniform(x0, x1 - 0.4)
                box(VINE, tv, tv + 0.3, 30.42, 30.56, top - rng.uniform(3, 10), top + 0.1)
        box(DARK, min(sx * 33.0, sx * 18.0), max(sx * 33.0, sx * 18.0), 30.48, 30.62, 2.2, 3.0)   # a carved band, with cloth tiles
        for t in range(5):
            cxx = sx * (19.5 + t * 3.0)
            box(RED if t % 2 else OCHRE, cxx - 0.4, cxx + 0.4, 30.4, 30.5, 2.3, 2.9)
    # the breach itself: a sill at its floor, and the break's ragged sides stepping down
    box(PITW2, -BREACH_HALF, BREACH_HALF, PIT, LIP + 0.9, FLOOR, BREACH_Y)
    for sx in (-1, 1):
        for k in range(7):
            x0, x1 = sorted((sx * (BREACH_HALF - k * 1.5), sx * (BREACH_HALF - (k + 1) * 1.5 - rng.uniform(0, 0.8))))
            top = PLAZA_Y - 1.0 - k * 2.4 - rng.uniform(0, 1.2)
            box(PITW if k % 2 else PITW2, x0, x1, PIT - rng.uniform(0, 0.4), LIP + 0.9 + rng.uniform(0, 0.6), BREACH_Y - 0.2, top)
        for _ in range(9):                                 # fallen blocks on the sill
            s = rng.uniform(0.7, 1.9)
            bx, by = sx * rng.uniform(5.5, BREACH_HALF - 1), rng.uniform(PIT + 0.5, LIP + 0.6)
            box(PITW if rng.random() < 0.5 else ROCK, bx - s, bx + s, by - s * 0.7, by + s * 0.7, BREACH_Y, BREACH_Y + s * rng.uniform(0.6, 1.3),
                place(0, 0, 0, 0) if True else None)
    ya, yb = shelf_span(35.0)[0] - 0.6, shelf_span(35.0)[1] + 0.6
    box(PITW, LIP, LIP + 2.0, ya, yb, PLAZA_Y - 0.2, SHELF_Y - 0.001)
    box(PITD, LIP - 0.1, LIP + 1.2, ya, yb, SHELF_Y - 1.5, SHELF_Y - 0.9)
    box(RIM, LIP - 0.2, LIP + 2.0, ya, yb, SHELF_Y - 0.3, SHELF_Y + 0.3)
    box(PITD, -PIT, PIT, -PIT, PIT, FLOOR - 1, FLOOR)


# ---------------------------------------------------------------- the monoliths and the movers
def cap_of(pid):
    if pid == "core" or pid.startswith("slab") or pid.startswith("corner"):
        return CAP_A
    return CAP_C if pid.startswith("stone") or pid.startswith("bridge") else CAP_B


def rim_strips(x0, x1, y0, y1, top, w=0.22):
    for (a0, a1, b0, b1) in ((x0, x1, y0, y0 + w), (x0, x1, y1 - w, y1), (x0, x0 + w, y0 + w, y1 - w), (x1 - w, x1, y0 + w, y1 - w)):
        box(RIM, a0, a1, b0, b1, top - 0.3, top + 0.015)


def monolith(p):
    r = random.Random(p["id"])
    x, y, sx, sy, top = p["x"], p["z"], p["sx"], p["sz"], p["top"]
    x0, x1, y0, y1 = x - sx / 2, x + sx / 2, y - sy / 2, y + sy / 2
    big = min(sx, sy) >= 6
    cap = cap_of(p["id"])
    if "stairDown" in p:
        n, low, d = 6, p["low"], p["stairDown"]
        for k in range(n):
            h = top + (low - top) * (k + 0.5) / n + (top - low) / n / 2
            a, b = k / n, (k + 1) / n
            if d == "+x":
                e = (x0 + sx * a, x0 + sx * b, y0, y1)
            elif d == "-x":
                e = (x1 - sx * b, x1 - sx * a, y0, y1)
            elif d == "+z":
                e = (x0, x1, y0 + sy * a, y0 + sy * b)
            else:
                e = (x0, x1, y1 - sy * b, y1 - sy * a)
            box(cap, e[0], e[1], e[2], e[3], h - 0.5, h)
            box(RIM, e[0], e[1], e[2], e[3], h - 0.06, h + 0.012)
        body_top = min(top, low) - 0.2
        box(SHAFT, x0 + 0.1, x1 - 0.1, y0 + 0.1, y1 - 0.1, body_top, max(top, low) - 0.45)
    else:
        box(cap, x0, x1, y0, y1, top - 0.6, top)
        rim_strips(x0, x1, y0, y1, top)
        body_top = top - 0.6
        if big:                                           # an inlaid border on the walking face
            for (a0, a1, b0, b1) in ((x0 + 0.9, x1 - 0.9, y0 + 0.9, y0 + 1.05), (x0 + 0.9, x1 - 0.9, y1 - 1.05, y1 - 0.9),
                                     (x0 + 0.9, x0 + 1.05, y0 + 0.9, y1 - 0.9), (x1 - 1.05, x1 - 0.9, y0 + 0.9, y1 - 0.9)):
                box(LINE, a0, a1, b0, b1, top, top + 0.02)
    # under the cap: a dark groove, then a carved band of raised keys, then the shaft in courses of two greys
    box(DARK, x0 + 0.16, x1 - 0.16, y0 + 0.16, y1 - 0.16, body_top - 0.3, body_top)
    bt = body_top - 0.3
    box(SHAFT2, x0 + 0.04, x1 - 0.04, y0 + 0.04, y1 - 0.04, bt - 1.2, bt)
    accent = big and cap is CAP_B
    for (length, face) in ((sx, 0), (sx, 1), (sy, 2), (sy, 3)):
        n = max(2, int(length / 1.0))
        for t in range(n):
            if t % 2:
                continue
            c = (t + 0.5) * length / n
            m = (RED if t % 8 == 0 else OCHRE) if (accent and t % 4 == 0) else SHAFT
            hw = length / n * 0.36
            if face == 0:
                box(m, x0 + c - hw, x0 + c + hw, y0 - 0.03, y0 + 0.1, bt - 0.95, bt - 0.25)
            elif face == 1:
                box(m, x0 + c - hw, x0 + c + hw, y1 - 0.1, y1 + 0.03, bt - 0.95, bt - 0.25)
            elif face == 2:
                box(m, x0 - 0.03, x0 + 0.1, y0 + c - hw, y0 + c + hw, bt - 0.95, bt - 0.25)
            else:
                box(m, x1 - 0.1, x1 + 0.03, y0 + c - hw, y0 + c + hw, bt - 0.95, bt - 0.25)
    z, k = bt - 1.2, 0
    while z > FLOOR:
        zn = max(FLOOR, z - (3.0 if k else 2.2))
        inset = 0.14 if k % 2 else 0.22
        box(SHAFT if k % 2 else SHAFT2, x0 + inset, x1 - inset, y0 + inset, y1 - inset, zn, z)
        z, k = zn, k + 1
    box(DARK, x0 + 0.08, x1 - 0.08, y0 + 0.08, y1 - 0.08, bt - 9.6, bt - 9.0)
    # moss hanging from the groove and wet streaks down the sides
    base = bt
    for face in range(4):
        length = sx if face < 2 else sy
        for _ in range(max(1, int(length / 2.0))):
            t = r.uniform(0.1, length - 0.9)
            w = r.uniform(0.5, min(2.4, length - t - 0.1))
            m, zlo, zhi = (MOSS, base - r.uniform(0.5, 3.4), base + 0.25) if r.random() < 0.6 else (STREAK, base - r.uniform(6, 26), base - 1.3)
            if m is STREAK:
                w = min(w, 0.7)
            if face == 0:
                box(m, x0 + t, x0 + t + w, y0 - 0.05, y0 + 0.1, zlo, zhi)
            elif face == 1:
                box(m, x0 + t, x0 + t + w, y1 - 0.1, y1 + 0.05, zlo, zhi)
            elif face == 2:
                box(m, x0 - 0.05, x0 + 0.1, y0 + t, y0 + t + w, zlo, zhi)
            else:
                box(m, x1 - 0.1, x1 + 0.05, y0 + t, y0 + t + w, zlo, zhi)


def mover_at(m, f):
    return [m["from"][i] + (m["to"][i] - m["from"][i]) * f for i in range(3)]


def mover(m, f):
    """A small slab that moves during the round, drawn a fraction f along its travel: a pale teal-grey top so it reads
    as the thing that moves, a tapering underside, cloth coloured studs at its corners."""
    ux, uy, uz = mover_at(m, f)
    x, y, top = ux, uz, uy
    sx, sy = m["sx"], m["sz"]
    x0, x1, y0, y1 = x - sx / 2, x + sx / 2, y - sy / 2, y + sy / 2
    box(MOVER, x0, x1, y0, y1, top - 0.45, top)
    rim_strips(x0, x1, y0, y1, top, w=0.18)
    box(DARK, x0 + 0.15, x1 - 0.15, y0 + 0.15, y1 - 0.15, top - 0.7, top - 0.45)
    box(MOVERU, x0 + 0.5, x1 - 0.5, y0 + 0.5, y1 - 0.5, top - 1.1, top - 0.7)
    for cx in (x0 + 0.3, x1 - 0.3):
        for cy in (y0 + 0.3, y1 - 0.3):
            box(OCHRE, cx - 0.14, cx + 0.14, cy - 0.14, cy + 0.14, top, top + 0.03)
    for k in range(3):                                     # roots and drips hanging under it
        limb(VINE, (x + rng.uniform(-0.5, 0.5), y + rng.uniform(-0.5, 0.5), top - 1.4), (x + rng.uniform(-0.6, 0.6), y + rng.uniform(-0.6, 0.6), top - rng.uniform(3, 5)), 0.07, 0.02, n=5)
    # its travel, as a faint line of small stones in the air: only so the still picture says "this one moves"
    a, b = Vector((m["from"][0], m["from"][2], m["from"][1])), Vector((m["to"][0], m["to"][2], m["to"][1]))
    n = max(2, int((b - a).length / 0.9))
    for k in range(n + 1):
        q = a.lerp(b, k / n)
        if abs(k / n - f) * (b - a).length > max(sx, sy) * 0.75:
            box(MOVER, q.x - 0.07, q.x + 0.07, q.y - 0.07, q.y + 0.07, q.z - 0.6, q.z - 0.46)


# ---------------------------------------------------------------- people, animals, guardians
def figure(x, y, z, yaw=0.0, shirt=None, hat=False, s=1.0):
    T = place(x, y, z, yaw) @ Matrix.Scale(s, 4)
    shirt = shirt or CREAM
    for sx in (-0.11, 0.11):
        limb(INK, (sx, 0, 0.0), (sx, 0, 0.9), 0.085, 0.10, T, n=6)
        limb(SKIN, (sx * 2.5, 0, 1.40), (sx * 2.9, 0.02, 0.86), 0.06, 0.05, T, n=6)
    limb(shirt, (0, 0, 0.86), (0, 0, 1.46), 0.19, 0.23, T, n=8, flat=0.62)
    limb(RED, (0, 0, 0.86), (0, 0, 0.97), 0.2, 0.2, T, n=8, flat=0.66)
    limb(SKIN, (0, 0, 1.44), (0, 0, 1.53), 0.06, 0.06, T, n=6)
    blob(SKIN, 0, 0, 1.63, 0.125, 0.135, 0.13, T)
    blob(INK, 0, 0.02, 1.67, 0.132, 0.14, 0.10, T)
    if hat:
        prism(THATCH, 0, 0, 1.70, 1.92, 0.42, 0.02, n=10, M=T)


def carabao(x, y, z, yaw):
    T = place(x, y, z, yaw)
    blob(CARABAO, 0, 0, 1.0, 0.62, 1.15, 0.6, T)
    blob(CARABAO, 0, -1.25, 1.15, 0.3, 0.42, 0.32, T)
    blob(CARABAO, 0, 0.5, 1.2, 0.5, 0.6, 0.5, T)
    for sx in (-0.34, 0.34):
        for sy in (-0.7, 0.75):
            limb(CARABAO, (sx, sy, 0.0), (sx, sy, 0.8), 0.11, 0.15, T, n=6)
        limb(CREAM, (sx * 0.6, -1.2, 1.38), (sx * 2.0, -0.95, 1.5), 0.07, 0.05, T, n=5)      # the swept horns
        limb(CREAM, (sx * 2.0, -0.95, 1.5), (sx * 1.7, -0.45, 1.78), 0.05, 0.015, T, n=5)


def bulul(x, y, z, h, yaw, m=None, cloth=True):
    """A bulul, the Ifugao rice guardian: seated on a stepped block, knees drawn up, forearms folded across the
    knees, a long back, a large calm head with a heavy brow. Faces local -Y. Rounded forms, not stacked boxes."""
    m = m or BULUL
    T = place(x, y, z, yaw) @ Matrix.Scale(h, 4)
    prism(m, 0, 0, 0.0, 0.07, 0.36, 0.33, n=8, M=T)                    # the block it sits on, waisted like a mortar
    prism(m, 0, 0, 0.07, 0.13, 0.24, 0.22, n=8, M=T)
    prism(m, 0, 0, 0.13, 0.18, 0.30, 0.33, n=8, M=T)
    limb(m, (0, 0.07, 0.20), (0, 0.10, 0.66), 0.155, 0.17, T, n=10, flat=0.72)      # hips to shoulders, a long back
    blob(m, 0, 0.07, 0.24, 0.19, 0.16, 0.09, T)                                      # the seat
    for sx in (-1, 1):
        hip, knee, foot = (sx * 0.10, 0.02, 0.26), (sx * 0.115, -0.19, 0.52), (sx * 0.11, -0.21, 0.19)
        limb(m, hip, knee, 0.075, 0.06, T)                                           # thigh, rising to the knee
        limb(m, knee, foot, 0.058, 0.048, T)                                         # shin
        blob(m, knee[0], knee[1], knee[2], 0.064, 0.064, 0.064, T)
        blob(m, foot[0], foot[1] - 0.045, 0.195, 0.05, 0.085, 0.03, T)               # the foot, flat on the block
        sh, el = (sx * 0.185, 0.09, 0.655), (sx * 0.205, -0.07, 0.50)
        blob(m, sh[0], sh[1], sh[2], 0.062, 0.062, 0.06, T)
        limb(m, sh, el, 0.05, 0.044, T)                                              # upper arm, down and forward
        limb(m, el, (-sx * 0.07, -0.215, 0.575 + sx * 0.012), 0.042, 0.036, T)       # forearm, across both knees
    limb(m, (0, 0.085, 0.655), (0, 0.075, 0.74), 0.062, 0.058, T, n=8)                # neck
    blob(m, 0, 0.06, 0.865, 0.132, 0.142, 0.155, T)                                  # the head, long and full
    blob(m, 0, 0.075, 0.975, 0.10, 0.11, 0.045, T)                                   # the crown, a low cap
    limb(m, (-0.105, -0.055, 0.905), (0.105, -0.055, 0.905), 0.026, 0.026, T, n=6)    # the heavy brow
    limb(m, (0, -0.075, 0.895), (0, -0.105, 0.81), 0.02, 0.034, T, n=6)               # the nose, broad at the base
    limb(m, (-0.045, -0.085, 0.775), (0.045, -0.085, 0.775), 0.014, 0.014, T, n=5)    # the mouth, a short straight line
    for sx in (-1, 1):
        blob(m, sx * 0.135, 0.065, 0.865, 0.02, 0.04, 0.055, T)                      # ears
    if cloth:
        limb(RED, (0, 0.07, 0.275), (0, 0.075, 0.33), 0.166, 0.168, T, n=10, flat=0.74)   # a woven band at the waist
        limb(OCHRE, (0, 0.07, 0.33), (0, 0.075, 0.345), 0.168, 0.168, T, n=10, flat=0.74)


def house(x, y, z, s=1.0, yaw=0.0, lit=False):
    """An Ifugao house (bale) or granary (alang): four posts with rat guard discs, a box, a steep four sided thatch
    roof that comes down past the walls, a ladder."""
    T = place(x, y, z, yaw) @ Matrix.Scale(s, 4)
    for sx in (-1.3, 1.3):
        for sy in (-1.3, 1.3):
            limb(WOOD, (sx, sy, 0), (sx, sy, 2.0), 0.2, 0.16, T, n=6)
            prism(WOOD2, sx, sy, 1.8, 1.92, 0.6, n=10, M=T)
    cbox(WOOD, 0, 0, 3.6, 3.6, 2.0, 3.3, T)
    cbox(WOOD2, 0, 0, 3.8, 3.8, 2.0, 2.16, T)
    cbox(EMBER if lit else PORTAL, 0, -1.81, 0.9, 0.06, 2.2, 3.2, T)
    v, f = _g(THATCH)
    n = len(v)
    for (px, py, pz) in ((-3.0, -3.0, 2.7), (3.0, -3.0, 2.7), (3.0, 3.0, 2.7), (-3.0, 3.0, 2.7), (-0.3, -0.3, 7.4), (0.3, -0.3, 7.4), (0.3, 0.3, 7.4), (-0.3, 0.3, 7.4)):
        v.append(tuple(T @ Vector((px, py, pz))))
    f.extend(((n, n + 1, n + 5, n + 4), (n + 1, n + 2, n + 6, n + 5), (n + 2, n + 3, n + 7, n + 6), (n + 3, n, n + 4, n + 7), (n + 4, n + 5, n + 6, n + 7), (n + 3, n + 2, n + 1, n)))
    cbox(THATCH2, 0, 0, 6.2, 6.2, 2.62, 2.78, T)
    cbox(THATCH2, 0, 0, 0.9, 0.9, 7.4, 7.7, T)
    limb(WOOD2, (0.5, -1.9, 2.0), (0.5, -3.0, 0.0), 0.05, 0.05, T, n=5)
    limb(WOOD2, (-0.5, -1.9, 2.0), (-0.5, -3.0, 0.0), 0.05, 0.05, T, n=5)
    if lit:
        p = T @ Vector((0, -2.6, 2.4))
        LAMPS.append((p.x, p.y, p.z, 260.0 * s * s, 4.0))


def hamlet(x, y, on_shelf=False):
    """A few houses and a granary on a paddy, with a small bulul pair and a cloth banner."""
    if on_shelf:
        z, yaw = shelf_z(x) + 0.05, -math.pi / 2
    else:
        x, y, z = snap_flat(x, y)
        d = downhill(x, y)
        yaw = math.atan2(-d.y, -d.x) - math.pi / 2
    T = place(x, y, z, yaw)
    cbox(TEMPLE2, 0, 0, 13.0, 4.8, -1.2, 0.5, T)
    for (ox, sc) in ((-4.0, 0.8), (0.4, 0.68), (4.2, 0.8)):
        p = T @ Vector((ox, 0.2, 0.5))
        house(p.x, p.y, p.z, sc, yaw + rng.uniform(-0.2, 0.2), lit=NIGHT)
    for sx in (-5.9, 5.9):
        q = T @ Vector((sx, -1.7, 0.5))
        bulul(q.x, q.y, q.z, 1.7, yaw, m=WOOD2)
    q = T @ Vector((2.3, -1.9, 0.5))
    limb(WOOD, (q.x, q.y, q.z), (q.x, q.y, q.z + 5.0), 0.06, 0.04, n=5)
    cbox(RED, 2.3 + 0.45, -1.9, 0.8, 0.05, 0.5 + 3.2, 0.5 + 4.9, T)
    cbox(OCHRE, 2.3 + 0.45, -1.9, 0.8, 0.06, 0.5 + 3.9, 0.5 + 4.1, T)


def fire_bowl(x, y, z, s=1.0, power=900.0):
    """A stone bowl on a short post, with a flame when it is night."""
    prism(DARK, x, y, z, z + 0.9 * s, 0.22 * s, 0.16 * s, n=8)
    prism(SHAFT2, x, y, z + 0.9 * s, z + 1.2 * s, 0.2 * s, 0.5 * s, n=8)
    if NIGHT:
        blob(FIRE, x, y, z + 1.42 * s, 0.3 * s, 0.3 * s, 0.42 * s)
        blob(FIRE, x + 0.05 * s, y, z + 1.8 * s, 0.14 * s, 0.14 * s, 0.3 * s)
        LAMPS.append((x, y, z + 1.9 * s, power, 0.5 * s))


# ---------------------------------------------------------------- the gate, south, uphill
def gate():
    """Not a pyramid: a dry stone platform in two low steps, like the terrace walls it grows out of, and on it a great
    Ifugao house raised on four carved posts with rat guard discs, which the way up the mountain passes under.
    Granaries flank it. Behind it the cut in the slope is held by a tall dry stone wall with a fall on each side."""
    S = lambda s: -s
    box(TEMPLE, -27, 27, S(98), S(48), PLAZA_Y - 0.5, 0.0)
    box(TEMPLE, -22, 22, S(98), S(58), 0.0, 6.0)
    for (hw, s, zt) in ((27, 48, 0.0), (22, 58, 6.0)):
        box(TEMPLE2, -hw - 0.25, hw + 0.25, S(98), S(s - 0.25), zt - 1.3, zt - 0.5)
        box(RIM, -hw - 0.12, hw + 0.12, S(98), S(s - 0.12), zt - 0.22, zt + 0.02)
        for t in range(-int(hw / 2.4) + 1, int(hw / 2.4)):
            if abs(t) > 2 and t % 2 == 0:
                box(RED if t % 4 == 0 else OCHRE, t * 2.4 - 0.4, t * 2.4 + 0.4, S(s - 0.25), S(s - 0.33), zt - 1.25, zt - 0.55)
    n, s0, s1 = 30, 38.0, 58.0                              # the stair, wide and easy: 12 m up in 20
    for k in range(n):
        s = s0 + (s1 - s0) * k / n
        box(CAP_A, -5.5, 5.5, S(60), S(s), PLAZA_Y - 0.5, PLAZA_Y + 12.0 * (k + 1) / n)
    for sx in (-1, 1):
        xa, xb = sorted((sx * 5.5, sx * 7.2))
        for k in range(6):
            s = s0 - 0.8 + (s1 - s0) * k / 6
            box(TEMPLE2, xa, xb, S(60), S(s), PLAZA_Y - 0.5, PLAZA_Y + 12.0 * (k + 1) / 6 + 0.9)
        bulul(sx * 10.5, S(43.0), PLAZA_Y + 0.9, 6.5, 0.0)
        prism(TEMPLE2, sx * 10.5, S(43.0), PLAZA_Y, PLAZA_Y + 0.9, 4.0, 3.6, n=8)
        fire_bowl(sx * 6.4, S(37.2), PLAZA_Y, 1.6, 2600.0)
        fire_bowl(sx * 6.4, S(61.5), 6.0, 1.6, 2600.0)
    # the great house: four carved posts, a floor high enough to walk under, cloth bands, the steep roof
    top = 6.0
    for sx in (-1, 1):
        for s in (68.0, 84.0):
            px, py = sx * 9.0, S(s)
            limb(WOOD, (px, py, top), (px, py, top + 13.0), 1.25, 1.0, n=10)
            for (zc, m, rr) in ((1.6, WOOD2, 1.36), (3.4, OCHRE, 1.3), (3.9, RED, 1.3), (4.4, INK, 1.28), (6.2, WOOD2, 1.26), (8.8, WOOD2, 1.2), (9.6, CREAM, 1.16)):
                prism(m, px, py, top + zc, top + zc + 0.45, rr, n=10)
            prism(WOOD2, px, py, top + 11.6, top + 12.1, 3.2, 3.0, n=14)                      # the rat guard, a great disc
            prism(WOOD, px, py, top + 12.1, top + 12.4, 2.2, n=14)
        bulul(sx * 9.0, S(63.2), top, 4.2, 0.0, m=WOOD2)                                      # a wooden pair at the front posts
    z0 = top + 13.0
    box(WOOD2, -13.0, 13.0, S(88.5), S(63.5), z0, z0 + 0.9)                                   # floor beams
    box(WOOD, -11.5, 11.5, S(87), S(65), z0 + 0.9, z0 + 6.4)
    for (zc, m, hh) in ((1.4, INK, 0.5), (1.9, RED, 0.9), (2.8, OCHRE, 0.35), (3.15, CREAM, 0.25), (3.4, RED, 0.5)):
        box(m, -11.6, 11.6, S(87.1), S(64.9), z0 + zc, z0 + zc + hh)                          # woven cloth bands round the walls
    box(EMBER if NIGHT else PORTAL, -1.6, 1.6, S(65.05), S(64.85), z0 + 1.2, z0 + 5.0)        # the door
    for sx in (-1, 1):
        box(EMBER if NIGHT else PORTAL, sx * 6.5 - 0.9, sx * 6.5 + 0.9, S(65.05), S(64.85), z0 + 3.6, z0 + 5.0)
        limb(WOOD2, (sx * 0.9, S(64.6), z0 + 0.9), (sx * 0.9, S(60.5), top), 0.14, 0.14, n=6)   # the ladder's rails
    for k in range(9):
        t = (k + 0.5) / 9
        limb(WOOD2, (-0.9, S(64.6 - 4.1 * t), z0 + 0.9 - (z0 + 0.9 - top) * t), (0.9, S(64.6 - 4.1 * t), z0 + 0.9 - (z0 + 0.9 - top) * t), 0.09, 0.09, n=5)
    v, f = _g(THATCH)
    n = len(v)
    zb, zt = z0 + 4.6, z0 + 27.0
    for (px, py, pz) in ((-17.5, S(93), zb), (17.5, S(93), zb), (17.5, S(59), zb), (-17.5, S(59), zb), (-2.4, S(77.2), zt), (2.4, S(77.2), zt), (2.4, S(74.8), zt), (-2.4, S(74.8), zt)):
        v.append((px, py, pz))
    f.extend(((n, n + 1, n + 5, n + 4), (n + 1, n + 2, n + 6, n + 5), (n + 2, n + 3, n + 7, n + 6), (n + 3, n, n + 4, n + 7), (n + 4, n + 5, n + 6, n + 7), (n + 3, n + 2, n + 1, n)))
    box(THATCH2, -18.0, 18.0, S(93.5), S(58.5), zb - 0.2, zb + 0.5)                           # the eave's thick edge
    for q in range(1, 5):                                                                    # thatch courses
        t = q / 5.0
        hx, hy0, hy1 = 17.5 + (2.4 - 17.5) * t + 0.12, 93 + (77.2 - 93) * t + 0.12, 59 + (74.8 - 59) * t - 0.12
        zz = zb + (zt - zb) * t
        box(THATCH2, -hx, hx, S(hy0), S(hy1), zz - 0.25, zz + 0.1)
    box(WOOD2, -3.0, 3.0, S(77.6), S(74.4), zt, zt + 0.8)
    for sx in (-1, 1):                                                                       # crossed finials, like horns
        limb(WOOD2, (sx * 1.6, S(76), zt + 0.6), (sx * 5.2, S(76), zt + 5.2), 0.3, 0.1, n=6)
        house(sx * 19.5, S(53.0), 0.0, 1.15, 0.0, lit=NIGHT)                                 # granaries on the lower step
        house(sx * 18.0, S(72.0), 6.0, 1.0, sx * 0.3, lit=NIGHT)
        fall(sx * 16.0, S(98.4), 0.0, 17.5, 6.0, 3.6)                                        # falls down the back wall
        box(WATER, sx * 16.0 - 1.8, sx * 16.0 + 1.8, S(98.4), S(58.0), 6.0, 6.06)
        fall(sx * 16.0, S(58.0), 0.0, 6.05, 0.0, 3.6)
        box(WATER, sx * 16.0 - 1.8, sx * 16.0 + 1.8, S(58.0), S(48.0), 0.0, 0.06)
        fall(sx * 16.0, S(48.0), 0.0, 0.05, PLAZA_Y, 3.6)
        pit_fall(sx * 16.0 / 48.0 / math.hypot(16.0 / 48.0, 1.0), -1.0 / math.hypot(16.0 / 48.0, 1.0), 3.6, 48.0 * math.hypot(16.0 / 48.0, 1.0))
    # the way on up the mountain, behind the house
    for k in range(40):
        s = 99.0 + k * 1.1
        box(CAP_C, -2.2, 2.2, S(s + 1.1), S(s), 6.0, 17.0 + k * 0.52)


# ---------------------------------------------------------------- the stage's things
def stage_things():
    for p in LAY["pieces"]:
        monolith(p)
    for m in LAY.get("movers", []):
        same = m["from"][0] == m["to"][0] and m["from"][2] == m["to"][2]
        mover(m, (1.0 if m["phase"] == 0 else 0.0) if same else 0.42)
    y = LAY["canHeight"]
    prism(CAN, 0, 0, y, y + 0.2, 0.11, n=10)
    for k in range(40):
        a0, a1 = 2 * math.pi * k / 40, 2 * math.pi * (k + 1) / 40
        quad(CREAM, (1.0 * math.cos(a0), 1.0 * math.sin(a0), y + 0.025), (1.12 * math.cos(a0), 1.12 * math.sin(a0), y + 0.025),
             (1.12 * math.cos(a1), 1.12 * math.sin(a1), y + 0.025), (1.0 * math.cos(a1), 1.0 * math.sin(a1), y + 0.025))
    for j in LAY["jumpPads"]:                              # a geyser: a carved kerb, a pale throat, a plume
        x, yy, z = j["x"], j["z"], j["y"]
        for (a, b, c, d) in ((-1.1, 1.1, -1.1, -0.8), (-1.1, 1.1, 0.8, 1.1), (-1.1, -0.8, -0.8, 0.8), (0.8, 1.1, -0.8, 0.8)):
            box(DARK, x + a, x + b, yy + c, yy + d, z, z + 0.22)
        box(STEAM, x - 0.8, x + 0.8, yy - 0.8, yy + 0.8, z, z + 0.08)
        prism(STEAM, x, yy, z + 0.08, z + 4.6, 0.3, 0.05, n=8)
        for (h, r) in ((3.3, 0.22), (4.1, 0.3), (4.8, 0.2)):
            blob(STEAM, x + rng.uniform(-0.3, 0.3), yy + rng.uniform(-0.3, 0.3), z + h, r, r, r * 0.7)
    for s in LAY["speedPads"]:
        x, yy, z = s["x"], s["z"], s["y"]
        ax = s["along"][1] == "x"
        hx, hy = (1.5, 0.75) if ax else (0.75, 1.5)
        box(OCHRE, x - hx, x + hx, yy - hy, yy + hy, z, z + 0.05)
        for k in (-1, 0, 1):
            if ax:
                box(INK, x + k * 0.9 - 0.12, x + k * 0.9 + 0.12, yy - 0.6, yy + 0.6, z + 0.05, z + 0.07)
            else:
                box(INK, x - 0.6, x + 0.6, yy + k * 0.9 - 0.12, yy + k * 0.9 + 0.12, z + 0.05, z + 0.07)
    for pk in LAY["pickups"]:
        x, yy, z = pk["x"], pk["z"], pk["y"]
        prism(RED, x, yy, z, z + 0.5, 0.26, 0.32, n=8)
        prism(OCHRE, x, yy, z + 0.5, z + 0.62, 0.34, 0.2, n=8)


def top_of(pid):
    return next((p["top"] for p in LAY["pieces"] if p["id"] == pid), None)


def life():
    """Day only: farmers in salakot hats and carabao, on the water shelf and the paddies nearest the pit."""
    k = 0
    for (x, y) in ((40, 4), (44, 16), (47, -8), (54, 9), (58, 22), (66, -4), (70, 14), (78, 6), (88, 18), (43, 24)):
        lo, hi = shelf_span(x)
        y = max(lo + 2, min(hi - 2, y))
        figure(x, y, shelf_z(x), rng.uniform(0, 6.28), shirt=(RED, INK, CREAM, OCHRE)[k % 4], hat=True)
        if k % 3 == 0:
            carabao(x + 2.6, y + 1.4, shelf_z(x + 2.6), rng.uniform(0, 6.28))
        k += 1
    for (x, y, z) in paddy_points(16, 110.0):
        figure(x, y, z, rng.uniform(0, 6.28), shirt=(RED, INK, CREAM, OCHRE)[k % 4], hat=True)
        if k % 3 == 0:
            carabao(x + 1.8, y + 1.2, gz(x + 1.8, y + 1.2), rng.uniform(0, 6.28))
        k += 1


def night_things():
    """The second half: fire on the stage and the brink, fireflies over the water and the pit, shooting stars."""
    for (x, y, z) in ((10.3, 10.3, top_of("cornerNE")), (-10.3, 10.3, top_of("cornerNW")), (10.3, -10.3, top_of("cornerSE")), (-10.3, -10.3, top_of("cornerSW")),
                      (21.2, 21.2, top_of("towerNE")), (-21.2, 21.2, top_of("towerNW")), (21.2, -21.2, top_of("towerSE")), (-21.2, -21.2, top_of("towerSW"))):
        if z is not None:
            fire_bowl(x, y, z, 1.0, 700.0)
    for sx in (-1, 1):
        for t in (-22, 0, 22):
            fire_bowl(sx * 34.4, t, PLAZA_Y, 1.5, 1500.0)
            fire_bowl(t, -34.4, PLAZA_Y, 1.5, 1500.0)
    for _ in range(520):                                   # fireflies: over the paddies, in the pit, round the court
        k = rng.random()
        if k < 0.45:
            x, y = rng.uniform(-110, 120), rng.uniform(-110, 110)
            if abs(x) < 34 and abs(y) < 34:
                continue
            lo, hi = shelf_span(max(35.0, min(106.0, x)))
            z = (shelf_z(x) if 35 < x < 106 and lo < y < hi else gz(x, y)) + rng.uniform(0.3, 3.0)
            s = 0.1
        elif k < 0.7:
            x, y, z, s = rng.uniform(-28, 28), rng.uniform(-28, 28), rng.uniform(-14, 3), 0.06
        else:
            x, y, z, s = rng.uniform(-14, 14), rng.uniform(6, 34), rng.uniform(0.4, 3.2), 0.03
        blob(FLY, x, y, z, s)
    for (az, el, ln, tilt) in ((66, 30, 300, -28), (97, 33, 240, -150), (108, 21, 200, -160)):   # shooting stars, out of her hair
        a, e = math.radians(az), math.radians(el)
        d = 3000.0
        c = Vector((d * math.cos(e) * math.cos(a), d * math.cos(e) * math.sin(a), d * math.sin(e)))
        side = Vector((-math.sin(a), math.cos(a), 0))
        upv = Vector((-math.sin(e) * math.cos(a), -math.sin(e) * math.sin(a), math.cos(e)))
        t = math.radians(tilt)
        along = side * math.cos(t) + upv * math.sin(t)
        limb(SHOOT, c, c + along * ln, 3.2, 0.2, n=5)
        blob(SHOOT, c.x, c.y, c.z, 5.5)


ground()
far_ground()
shelf()
forest()
pit()
gate()
for (x, y) in ((-58, -112), (-96, -62), (64, -108), (36, -124)):
    stream(x, y, 3.0)
spills()
valley_falls()
hamlet(-52, -66); hamlet(-88, -30); hamlet(58, -78); hamlet(-70, 12); hamlet(92, 6, on_shelf=True)
# the four great guardians: two on the plaza by the gate's stair, two on the corner towers of the broken north wall
for sx in (-1, 1):
    prism(TEMPLE2, sx * 36.6, -36.6, PLAZA_Y, PLAZA_Y + 1.4, 4.4, 4.0, n=8)
    bulul(sx * 36.6, -36.6, PLAZA_Y + 1.4, 9.0, math.atan2(1, -sx) + math.pi / 2)
    box(PITW2, sx * 33 - 3.4, sx * 33 + 3.4, 33 - 3.4, 33 + 3.4, -70.0, 8.6)
    box(PITW, sx * 33 - 3.55, sx * 33 + 3.55, 33 - 3.55, 33 + 3.55, 2.0, 3.2)
    box(RIM, sx * 33 - 3.7, sx * 33 + 3.7, 33 - 3.7, 33 + 3.7, 8.6, 9.0)
    bulul(sx * 33, 33, 9.0, 7.5, math.atan2(-1, -sx) + math.pi / 2)
stage_things()
CH = LAY["canHeight"]
figure(0, -2.5, CH, math.pi)                               # the taya on its mark, 1.75 m
sn = top_of("slabN") or 0.0
for ax in (-1.8, 0.0, 1.8):
    figure(ax, 9.0, sn, 0.0, shirt=INK if ax else OCHRE)   # the attackers on theirs, facing the can
if top_of("midS") is not None and "midS" not in LAY["bonus"]:
    figure(2.0, -17.0, top_of("midS"), 0.4)
if top_of("towerNE") is not None:
    figure(16.5, 16.0, top_of("towerNE"), 2.2)
if top_of("stone7") is not None:
    figure(-16.0, 9.5, top_of("stone7"), -1.2)
if top_of("midE") is not None and "midE" not in LAY["bonus"]:
    figure(19.0, -2.0, top_of("midE"), 1.6)
if NIGHT:
    night_things()
else:
    life()
    _a, _e = math.radians(84.0), math.radians(17.0)       # the day moon: the only hint of Mayari before half time
    _c = Vector((5200 * math.cos(_e) * math.cos(_a), 5200 * math.cos(_e) * math.sin(_a), 5200 * math.sin(_e)))
    blob(DAYMOON, _c.x, _c.y, _c.z, 62.0, 8.0, 62.0)
    blob(mat("day_sky", "#a9bfd6", glow=0.75, fogged=False), _c.x + 26.0, _c.y - 30.0, _c.z + 12.0, 56.0, 8.0, 56.0)
box(mat("mist_floor", "#31445f" if NIGHT else "#c3d2d4", glow=1.0), -PIT, PIT, -PIT, PIT, -39.0, -38.9)
flush()

# ---------------------------------------------------------------- light, sky, cameras
sc = bpy.context.scene
ids = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in ids else "BLENDER_EEVEE"
sc.view_settings.view_transform = "Standard"
for k, v in (("use_shadows", True), ("use_raytracing", True), ("taa_render_samples", 48)):
    try:
        setattr(sc.eevee, k, v)
    except Exception:
        pass
w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
WN, WL = w.node_tree.nodes, w.node_tree.links
tc, sp, ramp = WN.new("ShaderNodeTexCoord"), WN.new("ShaderNodeSeparateXYZ"), WN.new("ShaderNodeValToRGB")
WL.new(tc.outputs["Generated"], sp.inputs[0]); WL.new(sp.outputs["Z"], ramp.inputs[0])
sky = ("#22365a", "#0e1a36", "#050914") if NIGHT else ("#e9e6d6", "#c9d9e4", "#8fb0d0")
ramp.color_ramp.elements[0].position, ramp.color_ramp.elements[0].color = 0.0, (*lin(sky[0]), 1)
ramp.color_ramp.elements[1].position, ramp.color_ramp.elements[1].color = 0.6, (*lin(sky[2]), 1)
mid = ramp.color_ramp.elements.new(0.14); mid.color = (*lin(sky[1]), 1)
WL.new(ramp.outputs[0], WN["Background"].inputs[0])
WN["Background"].inputs[1].default_value = 1.0 if NIGHT else 0.8
sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sc.collection.objects.link(sun)
if NIGHT:
    # stars: a cell pattern on the sky's own directions, only above the horizon
    vor, lt, ab, mul, bg2, add = (WN.new("ShaderNodeTexVoronoi"), WN.new("ShaderNodeMath"), WN.new("ShaderNodeMath"), WN.new("ShaderNodeMath"),
                                  WN.new("ShaderNodeBackground"), WN.new("ShaderNodeAddShader"))
    vor.inputs["Scale"].default_value = 170.0
    WL.new(tc.outputs["Generated"], vor.inputs["Vector"])
    lt.operation = "LESS_THAN"; WL.new(vor.outputs["Distance"], lt.inputs[0]); lt.inputs[1].default_value = 0.045
    ab.operation = "GREATER_THAN"; WL.new(sp.outputs["Z"], ab.inputs[0]); ab.inputs[1].default_value = 0.015
    mul.operation = "MULTIPLY"; WL.new(lt.outputs[0], mul.inputs[0]); WL.new(ab.outputs[0], mul.inputs[1])
    mu2 = WN.new("ShaderNodeMath"); mu2.operation = "MULTIPLY"; WL.new(mul.outputs[0], mu2.inputs[0]); mu2.inputs[1].default_value = 3.2
    bg2.inputs[0].default_value = (*lin("#dfe8ff"), 1); WL.new(mu2.outputs[0], bg2.inputs[1])
    WL.new(WN["Background"].outputs[0], add.inputs[0]); WL.new(bg2.outputs[0], add.inputs[1])
    WL.new(add.outputs[0], WN["World Output"].inputs[0])
    # the moon's light comes from where Mayari's eye is, over the valley
    sun.data.energy, sun.data.color, sun.data.angle = 1.15, lin("#a9c4ff"), math.radians(3.0)
    SUN_DIR = Vector((-0.20, -0.84, -0.50)).normalized()
else:
    sun.data.energy, sun.data.color, sun.data.angle = 4.8, lin("#ffecd2"), math.radians(2.0)
    SUN_DIR = Vector((-0.62, -0.52, -0.52)).normalized()   # Apolaki's sun, out over the valley to the north east
sun.rotation_euler = SUN_DIR.to_track_quat("-Z", "Y").to_euler()
for i, (x, y, z, power, rad) in enumerate(LAMPS if NIGHT else []):
    ld = bpy.data.lights.new("fire%d" % i, "POINT")
    ld.energy, ld.color, ld.shadow_soft_size = power, lin("#ffb060"), rad
    ld.use_shadow = power >= 1400.0
    lo = bpy.data.objects.new("fire%d" % i, ld); sc.collection.objects.link(lo); lo.location = (x, y, z)

if NIGHT:
    # MAYARI: a picture hung far behind the valley's ridges, lit by nothing, drawn over the stars
    img = bpy.data.images.load(os.path.join(OUT, "payo_mayari_%s.png" % MAYARI))
    mm = bpy.data.materials.new("mayari"); mm.use_nodes = True
    N, L = mm.node_tree.nodes, mm.node_tree.links
    N.remove(N["Principled BSDF"])
    tex, em, tr, mx = N.new("ShaderNodeTexImage"), N.new("ShaderNodeEmission"), N.new("ShaderNodeBsdfTransparent"), N.new("ShaderNodeMixShader")
    tex.image = img
    L.new(tex.outputs["Color"], em.inputs[0]); em.inputs[1].default_value = 1.5
    L.new(tex.outputs["Alpha"], mx.inputs[0]); L.new(tr.outputs[0], mx.inputs[1]); L.new(em.outputs[0], mx.inputs[2])
    L.new(mx.outputs[0], N["Material Output"].inputs[0])
    for k, v in (("surface_render_method", "BLENDED"), ("blend_method", "BLEND")):
        try:
            setattr(mm, k, v)
        except Exception:
            pass
    DIST, AZ, SIZE = 6800.0, math.radians(80.0), 4700.0
    c = Vector((DIST * math.cos(AZ), DIST * math.sin(AZ), 2050.0))
    side = Vector((math.sin(AZ), -math.cos(AZ), 0)) * (SIZE / 2)
    upv = Vector((0, 0, SIZE / 2))
    me = bpy.data.meshes.new("mayari")
    me.from_pydata([tuple(c - side - upv), tuple(c + side - upv), tuple(c + side + upv), tuple(c - side + upv)], [], [(0, 1, 2, 3)])
    uv = me.uv_layers.new(name="uv")
    for i, co in enumerate(((0, 0), (1, 0), (1, 1), (0, 1))):
        uv.data[i].uv = co
    me.materials.append(mm)
    sc.collection.objects.link(bpy.data.objects.new("mayari", me))

sc.render.resolution_x, sc.render.resolution_y = 1600, 900
sc.render.image_settings.file_format = "PNG"
tne = top_of("towerNE") or 0.6
FAR = (120, 0.00042, 0.90)
VIEWS = [
    # name, eye, look, vertical fov, (fog start, fog rate, fog cap)
    ("air_a", Vector((250, 300, 150)), Vector((0, -20, -8)), 44, (300, 0.00045, 0.9)),
    ("air_b", Vector((-330, 170, 120)), Vector((0, -10, -6)), 42, (280, 0.00045, 0.9)),
    ("stage", Vector((-52, 74, 40)), Vector((2, -6, -9)), 48, (80, 0.0010, 0.9)),
    ("eye_valley", Vector((0.0, -1.4, CH + 1.7)), Vector((9.0, 90, 12.0 if NIGHT else 1.0)), 66, FAR),
    ("eye_terraces", Vector((10.3, -2.0, (top_of("slabE") or CH) + 1.7)), Vector((90, -10, -7.0)), 64, FAR),
    ("eye_gate", Vector((0.0, 8.0, sn + 1.7)), Vector((0, -40, 9.5)), 66, FAR),
    ("eye_outer", Vector((20.9, 20.9, tne + 1.7)), Vector((-8, -16, 1.5)), 66, FAR),
    ("wide_north", Vector((-30, -176, 96)), Vector((6, 60, 150 if NIGHT else -4)), 58, (260, 0.00045, 0.9)),
]
for (name, eye, look, fov, f) in VIEWS:
    cd = bpy.data.cameras.new(name); co = bpy.data.objects.new("cam_" + name, cd); sc.collection.objects.link(co)
    cd.clip_start, cd.clip_end = 0.1, 12000
    cd.sensor_fit, cd.angle_y = "VERTICAL", math.radians(fov)
    co.location = eye
    co.rotation_euler = (look - eye).to_track_quat("-Z", "Y").to_euler()
os.makedirs(OUT, exist_ok=True)
if "--blend" in sys.argv:
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "payo_draft%s.blend" % ("_night" if NIGHT else "")), compress=True)
for (name, eye, look, fov, f) in VIEWS:
    if ONLY and name not in ONLY.split(","):
        continue
    for key, val in zip(("d0", "k", "cap"), f):
        fog.nodes[key].outputs[0].default_value = val
    sc.camera = bpy.data.objects["cam_" + name]
    tag = ("" if LAYOUT == 0 else "_%s" % LAY["name"]) + ("_night" if NIGHT else "") + ("_mayari_b" if NIGHT and MAYARI == "b" else "")
    sc.render.filepath = os.path.join(OUT, "payo_draft_%s%s_v%s.png" % (name, tag, VER))
    bpy.ops.render.render(write_still=True)
print("done", LAY["name"], "night" if NIGHT else "day")
