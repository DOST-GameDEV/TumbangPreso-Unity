"""PAYO land kit: the PIT WALL, the ROCK CLIFF and the GROUND between terraces (Blender). New pieces, new names.

  blender -b --python tools/payo_pieces/terrace_pit.py -- [pit cliff ground]

Blender while building: +X along the wall, the face looks toward -Y, +Z up (in the glb: +x along, +y up, face on +z).

PIT WALL, 12 m wide x 12 m tall, vertical. Pivot: the foot of the face at its LEFT end as you look at it.
  pit_wall, pit_wall_niche, pit_wall_spout   tile side by side every 12 m and stack every 12 m. Each carries a pilaster
      on its left edge that covers the seam, a ledge at 4.4 to 4.8 m standing 0.55 m out, the carved band at 8.0 to 8.9 m.
  pit_corner_in    for a corner you stand INSIDE of: wall A ends at the pivot coming from -x, wall B leaves it toward -y.
  pit_corner_out   for a corner that juts at you: wall A ends at the pivot coming from -x, wall B leaves it toward +y.
  pit_wall_broken  5 m of wall breaking down in steps to nothing at its right (mirror it for the other side of a notch).
  pit_kerb         12 m of coping stones for the pit's edge. Pivot: the TOP of the wall's face, left end.
ROCK CLIFF, 12 x 12 m: cliff_wall (tiles and stacks; rocks run past every edge and bury each other), cliff_corner
  (a jutting corner, same convention as pit_corner_out).
GROUND: ground_grass_tile and ground_forest_tile (12 x 12 m, flat at the edges so they butt; lay them on the slope),
  path_stones (6 m of flat stones, 1.5 m wide, pivot at the middle of its near end, runs toward +y in Blender).
"""
import json
import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import terrace_lib as K           # noqa: E402
import terrace_pit_layout as PL   # noqa: E402
import bmesh                       # noqa: E402
import bpy                         # noqa: E402
from mathutils import Euler, Matrix, Vector  # noqa: E402

stats = {}
BEV = 0.06


def mats():
    return dict(pit=K.material("py_pit_stone", "py_pit_wall.png"), rock=K.material("py_rock", "py_rock.png"),
                plant=K.material("py_terrace_plant", "py_terrace_plant.png"), wet=K.material("py_wet", "py_wet.png", alpha=True),
                grass=K.material("py_ground_grass", "py_ground_grass.png"), forest=K.material("py_ground_forest", "py_ground_forest.png"))


def done(name, obs, **meta):
    obs = [o for o in obs if o is not None and len(o.data.polygons)]
    stats[name] = K.export(name, obs, dict(parts={o.name.replace(name + "_", ""): sum(len(p.vertices) - 2 for p in o.data.polygons) for o in obs}, **meta))


def join(objs, name, mat):
    bm = bmesh.new()
    for o in objs:
        o.data.transform(Matrix.Translation(o.location) @ o.rotation_euler.to_matrix().to_4x4())
        bm.from_mesh(o.data)
        bpy.data.objects.remove(o)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me); bm.free()
    me.materials.append(mat)
    K.activate(me)
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    return ob


def block(mb, lo, hi, r, bev=0.06, tile=2.5, moss=0.0, tint=1.0):
    """A dressed block: a box with every edge chamfered. Vertex colour does the paint: pale chamfers above, dark
    below, moss on top if asked. Wears py_rock."""
    lo, hi = Vector(lo), Vector(hi)
    c = (lo + hi) / 2
    off = (r.uniform(0, 1), r.uniform(0, 1))
    k = tint * r.uniform(0.9, 1.0)

    def pt(sx, sy, sz, axis):
        p = [hi.x if sx > 0 else lo.x, hi.y if sy > 0 else lo.y, hi.z if sz > 0 else lo.z]
        s = (sx, sy, sz)
        for a in range(3):
            if a != axis:
                p[a] -= s[a] * bev
        return Vector(p)

    def add(pts, shade, mossy=0.0):
        n = (pts[1] - pts[0]).cross(pts[2] - pts[0])
        ctr = sum(pts, Vector()) / len(pts)
        if n.dot(ctr - c) < 0:
            pts = pts[::-1]
            n = -n
        n.normalize()
        ax = max(range(3), key=lambda i: abs(n[i]))
        uv = [((p.y, p.z), (p.x, p.z), (p.x, p.y))[ax] for p in pts]
        col = Vector((shade, shade, shade * 0.99))
        if mossy:
            col = col.lerp(Vector((0.60, 0.92, 0.36)) * 0.95, mossy)
        mb.face(pts, [(u / tile + off[0], v / tile + off[1]) for u, v in uv], (min(1, col.x), min(1, col.y), min(1, col.z), 1))

    S = (-1, 1)
    for axis in range(3):
        for s in S:
            quad = []
            for a, b in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
                sg = [0, 0, 0]
                sg[axis] = s
                o = [i for i in range(3) if i != axis]
                sg[o[0]], sg[o[1]] = a, b
                quad.append(pt(sg[0], sg[1], sg[2], axis))
            if axis == 2:
                add(quad, (0.93 if s > 0 else 0.5) * k, moss if s > 0 else 0)
            else:
                add(quad, 0.80 * k)
    for axis in range(3):                                                # the twelve chamfers
        o = [i for i in range(3) if i != axis]
        for a in S:
            for b in S:
                q = []
                for e in S:
                    sg = [0, 0, 0]
                    sg[axis], sg[o[0]], sg[o[1]] = e, a, b
                    q.append((pt(sg[0], sg[1], sg[2], o[0]), pt(sg[0], sg[1], sg[2], o[1])))
                sz = (a if o[0] == 2 else b if o[1] == 2 else 0)
                add([q[0][0], q[1][0], q[1][1], q[0][1]], (1.0 if sz >= 0 else 0.62) * k, moss * 0.6 if sz > 0 else 0)
    for sx in S:
        for sy in S:
            for sz in S:
                add([pt(sx, sy, sz, 0), pt(sx, sy, sz, 1), pt(sx, sy, sz, 2)], (1.0 if sz > 0 else 0.62) * k)


def vine(mb, top, length, r, out=Vector((0, -1, 0))):
    """A hanging vine: a thin stem with heart leaves down it."""
    pts = [Vector(top)]
    n = max(3, int(length / 0.45))
    sway = r.uniform(-0.25, 0.25)
    for i in range(1, n + 1):
        k = i / n
        pts.append(Vector(top) + Vector((math.sin(k * 3 + sway * 9) * 0.18 * k + sway * k, out.y * 0.05 * math.sin(k * 5), -length * k)))
    K.tube(mb, pts, 0.022, seg=3, uv=(0.43, 1, 0.25, 0.55), metres_u=1e6, cap=False, col=(0.6, 0.6, 0.6, 1))
    side = Vector((1, 0, 0))
    for i in range(1, len(pts)):
        for s in (-1, 1):
            if r.random() < 0.85:
                o = (out + side * s * r.uniform(0.5, 1.4)).normalized()
                sz = r.uniform(0.26, 0.42) * (1.15 - 0.4 * i / len(pts))
                K.leaf(mb, pts[i] + Vector((0, 0, r.uniform(-0.1, 0.1))), o, sz * 1.25, sz, 3, r, up=r.uniform(-0.6, -0.15), shade=r.uniform(0.75, 1.0))


# ------------------------------------------------------------------------------------------------ the pit wall
def uvp(s, t):
    return (s / PL.W, t / PL.Hh)


def face_stones(mb, keep, s_shift=0.0):
    P = lambda p, out=0.0: Vector((p[0] + s_shift, -out, p[1]))
    for st in PL.stones():
        if not keep(st):
            continue
        c = PL.corners(st)
        o = st["out"]
        bev = st["bev"]
        cx, cy = sum(p[0] for p in c) / 4, sum(p[1] for p in c) / 4
        front = [(p[0] + (bev if p[0] < cx else -bev), p[1] + (bev if p[1] < cy else -bev)) for p in c]
        mb.face([P(p, o) for p in front], [uvp(*p) for p in front])
        for i in range(4):
            j = (i + 1) % 4
            mb.face([P(c[i]), P(c[j]), P(front[j], o), P(front[i], o)], [uvp(*c[i]), uvp(*c[j]), uvp(*front[j]), uvp(*front[i])])


def ledge(mb, r, x0, x1):
    x = x0
    while x < x1 - 0.05:
        w = r.uniform(1.1, 2.0)
        if x1 - (x + w) < 0.8:
            w = x1 - x
        block(mb, (x + 0.02, -r.uniform(0.50, 0.60), PL.LEDGE[0] + 0.02), (x + w - 0.02, 0.05, PL.LEDGE[1] + r.uniform(-0.02, 0.03)), r, bev=0.07, moss=r.uniform(0.3, 0.9))
        x += w


def pilaster(mb, r, x=0.0, top=PL.Hh):
    z = 0.0
    i = 0
    while z < top - 0.05:
        h = min(1.0, top - z)
        w = 0.50 if i % 2 else 0.42
        block(mb, (x - w, -(0.34 if i % 2 else 0.28), z + 0.015), (x + w, 0.05, z + h - 0.015), r, bev=0.07, moss=0.0, tint=0.97)
        z += h
        i += 1


def greenery(mb, r, x0, x1, top=True):
    for _ in range(max(1, int((x1 - x0) / 1.8))):                         # vines off the ledge
        vine(mb, (r.uniform(x0 + 0.8, x1 - 0.5), -0.56, PL.LEDGE[1] - 0.05), r.uniform(1.4, 3.6), r)
    if top:
        for _ in range(max(1, int((x1 - x0) / 3.2))):                     # and from the wall above
            vine(mb, (r.uniform(x0 + 0.8, x1 - 0.5), -0.16, PL.Hh - 0.02), r.uniform(1.0, 2.8), r)
    x = x0 + 0.3
    while x < x1:                                                         # moss hanging under the ledge's lip
        if r.random() < 0.55:
            for k in range(r.randint(2, 4)):
                ln = r.uniform(0.2, 0.55)
                o = Vector((r.uniform(-0.3, 0.3), -1, 0)).normalized()
                K.blade(mb, (x + r.uniform(-0.15, 0.15), -0.50, PL.LEDGE[0] + 0.06), o, 0.06, r.uniform(0.09, 0.14), 0.3, 3, r, shade=r.uniform(0.6, 0.85), droop=ln / 0.06 + 1)
        x += r.uniform(0.3, 0.6)
    x = x0 + 0.6
    while x < x1:                                                         # ferns and grass on the ledge
        if r.random() < 0.6:
            p = (x, -r.uniform(0.2, 0.45), PL.LEDGE[1] + 0.01)
            if r.random() < 0.5:
                for k in range(6):
                    a = k * 6.283 / 6 + r.uniform(-0.3, 0.3)
                    K.leaf(mb, p, Vector((math.cos(a), math.sin(a), 0)), r.uniform(0.35, 0.55), 0.15, 4, r, up=r.uniform(0.3, 0.7), shade=r.uniform(0.85, 1))
            else:
                K.tuft(mb, p, r, n=5, h=r.uniform(0.2, 0.35), w=0.05, lean=0.7, cell=3, shade=r.uniform(0.85, 1))
        x += r.uniform(0.7, 1.4)


def backing(mb, rects):
    dark = (0.30, 0.30, 0.30, 1)
    for s0, s1, t0, t1 in rects:
        if s1 - s0 > 1e-4 and t1 - t0 > 1e-4:
            mb.face([(s0, 0, t0), (s1, 0, t0), (s1, 0, t1), (s0, 0, t1)], [uvp(s0, t0), uvp(s1, t0), uvp(s1, t1), uvp(s0, t1)], dark)


def pit_wall(variant):
    K.reset(); m = mats(); r = random.Random(dict(plain=31, niche=32, spout=33)[variant])
    hole = PL.hole(variant)
    face, rock, pl, wet = K.MB(), K.MB(), K.MB(), K.MB()

    def keep(st):
        if hole and st["s0"] < hole[1] and st["s1"] > hole[0] and st["t0"] < hole[3] and st["t1"] > hole[2]:
            return False
        return True
    face_stones(face, keep)
    if hole:
        h0, h1, g0, g1 = hole
        backing(face, [(0, PL.W, 0, g0), (0, PL.W, g1, PL.Hh), (0, h0, g0, g1), (h1, PL.W, g0, g1)])
        # fill the ragged gap round the opening with dressed blocks, and frame it
        deep = 0.95 if variant == "niche" else 1.1
        jw = 0.46 if variant == "niche" else 0.34
        lh = 0.40 if variant == "niche" else 0.36
        lo_s = min(st["s0"] for st in PL.stones() if not keep(st)); hi_s = max(st["s1"] for st in PL.stones() if not keep(st))
        lo_t = min(st["t0"] for st in PL.stones() if not keep(st)); hi_t = max(st["t1"] for st in PL.stones() if not keep(st))
        backing(face, [(lo_s, hi_s, lo_t, g0), (lo_s, hi_s, g1, hi_t), (lo_s, h0, g0, g1), (h1, hi_s, g0, g1)])
        for a, b in ((lo_s, h0), (h1, hi_s)):                              # side filler
            if b - a > 0.08:
                z = lo_t
                while z < hi_t - 0.05:
                    hh = min(r.uniform(0.6, 0.9), hi_t - z)
                    block(rock, (a + 0.02, -r.uniform(0.08, 0.14), z + 0.02), (b - 0.02, 0.05, z + hh - 0.02), r, bev=0.05)
                    z += hh
        for a, b in ((lo_t, g0), (g1, hi_t)):                              # filler under and over
            if b - a > 0.08:
                block(rock, (h0 + 0.02, -0.10, a + 0.02), (h1 - 0.02, 0.05, b - 0.02), r, bev=0.05)
        block(rock, (h0 - 0.12, -0.30, g1 - lh), (h1 + 0.12, 0.05, g1 + 0.0), r, bev=0.07, tint=1.0)              # lintel
        for a, b in ((h0, h0 + jw), (h1 - jw, h1)):                                                                # jambs
            z = g0 + (0.34 if variant == "spout" else 0.0)
            while z < g1 - lh - 0.05:
                hh = min(r.uniform(0.7, 1.0), g1 - lh - z)
                block(rock, (a, -0.24, z + 0.01), (b, 0.05, z + hh - 0.01), r, bev=0.06)
                z += hh
        if variant == "spout":
            block(rock, (h0 - 0.10, -0.55, g0), (h1 + 0.10, 0.05, g0 + 0.34), r, bev=0.07, moss=0.3)                # the sill the water runs over
        # the dark inside
        a, b = h0 + jw - 0.02, h1 - jw + 0.02
        z0, z1 = g0 + (0.3 if variant == "spout" else 0.0), g1 - lh + 0.02
        dk = lambda v: (v, v, v * 1.02, 1)
        uvq = [(0.1, 0.1), (0.5, 0.1), (0.5, 0.5), (0.1, 0.5)]
        rock.face([(a, deep, z0), (b, deep, z0), (b, deep, z1), (a, deep, z1)], uvq, dk(0.20))
        rock.face([(a, 0, z0), (a, deep, z0), (a, deep, z1), (a, 0, z1)], uvq, dk(0.34))
        rock.face([(b, deep, z0), (b, 0, z0), (b, 0, z1), (b, deep, z1)], uvq, dk(0.30))
        rock.face([(a, 0, z1), (a, deep, z1), (b, deep, z1), (b, 0, z1)], uvq, dk(0.22))
        rock.face([(a, deep, z0), (a, 0, z0), (b, 0, z0), (b, deep, z0)], uvq, dk(0.5))
        if variant == "spout":                                             # the stain the water leaves
            cx = (h0 + h1) / 2
            wet.face([(cx - 0.9, -0.20, PL.LEDGE[1] + 0.02), (cx + 0.9, -0.20, PL.LEDGE[1] + 0.02), (cx + 0.55, -0.20, g0), (cx - 0.55, -0.20, g0)], [(0, 0.55), (1, 0.55), (0.85, 1), (0.15, 1)])
            wet.face([(cx - 1.3, -0.17, 0.3), (cx + 1.3, -0.17, 0.3), (cx + 0.9, -0.17, PL.LEDGE[0]), (cx - 0.9, -0.17, PL.LEDGE[0])], [(0, 0.0), (1, 0.0), (1, 0.55), (0, 0.55)])
    else:
        backing(face, [(0, PL.W, 0, PL.Hh)])
    ledge(rock, r, 0.5, PL.W - 0.5)
    pilaster(rock, r)
    greenery(pl, r, 0.5, PL.W - 0.5)
    name = "pit_wall" if variant == "plain" else "pit_wall_" + variant
    done(name, [face.obj(name + "_face", m["pit"]), rock.obj(name + "_blocks", m["rock"]), pl.obj(name + "_green", m["plant"]),
                wet.obj(name + "_wet", m["wet"]) if wet.f else None],
         pivot="foot of the face, left end", opening=list(hole) if hole else None)


def pit_corners():
    for kind in ("in", "out"):
        K.reset(); m = mats(); r = random.Random(36 if kind == "in" else 37)
        rock, pl = K.MB(), K.MB()
        z, i = 0.0, 0
        while z < PL.Hh - 0.05:
            h = min(0.75, PL.Hh - z)
            if PL.LEDGE[0] - 0.4 < z < PL.LEDGE[0] + 0.1:                  # the ledge course runs round the corner
                h = PL.LEDGE[1] - z
            big = PL.LEDGE[0] - 0.4 < z < PL.LEDGE[0] + 0.1
            if kind == "in":
                d = 0.62 if big else (0.50 if i % 2 else 0.40)
                block(rock, (-d, -d, z + 0.015), (0.05, 0.05, z + h - 0.015), r, bev=0.07, moss=0.5 if big else 0)
            else:
                o = 0.62 if big else 0.22
                if i % 2:
                    block(rock, (-1.25, -o, z + 0.015), (o, 0.62, z + h - 0.015), r, bev=0.07, moss=0.5 if big else 0)
                else:
                    block(rock, (-0.62, -o, z + 0.015), (o, 1.25, z + h - 0.015), r, bev=0.07, moss=0.5 if big else 0)
            z += h
            i += 1
        if kind == "in":
            vine(pl, (-0.3, -0.64, PL.LEDGE[1] - 0.05), 2.4, r)
        else:
            vine(pl, (-0.5, -0.64, PL.LEDGE[1] - 0.05), 2.8, r)
        name = "pit_corner_" + kind
        done(name, [rock.obj(name + "_blocks", m["rock"]), pl.obj(name + "_green", m["plant"])], pivot="foot of the corner line")


def pit_broken():
    K.reset(); m = mats(); r = random.Random(38)
    face, rock, pl = K.MB(), K.MB(), K.MB()
    lim = {}
    for t0, t1 in PL.course_edges():
        lim[round(t0, 3)] = 5.2 * (1 - (t0 + t1) / 2 / PL.Hh) ** 1.25 + r.uniform(-0.35, 0.45) + 0.4

    def keep(st):
        return st["s1"] <= lim[round(st["t0"], 3)] + 0.25

    face_stones(face, keep)
    rubble = []
    for t0, t1 in PL.course_edges():
        end = max([st["s1"] for st in PL.stones() if keep(st) and abs(st["t0"] - t0) < 1e-6] or [0.0])
        if end > 0:
            backing(face, [(0, end, t0, t1)])
            # the core of the wall shows at each broken step: rough stone, not dressed
            sz = (t1 - t0) * r.uniform(1.0, 1.35)
            rubble.append(K.rock_object("r", m["rock"], r, (sz * 1.2, sz * 1.1, sz), cuts=7, bevel=0.05, moss=0.5,
                                        at=(end + sz * 0.15, sz * 0.28, t0 + 0.02), sink=0.0))
            if PL.LEDGE[0] - 0.01 < t0 < PL.LEDGE[0] + 0.01 and end > 1.2:
                ledge(rock, r, 0.5, end)
    for k in range(5):                                                      # fallen blocks at the foot
        x, y = r.uniform(2.5, 6.5), -r.uniform(0.6, 2.4)
        sz = r.uniform(0.5, 0.95)
        o = K.rock_object("r", m["rock"], r, (sz * 1.5, sz * 1.0, sz * 0.75), cuts=4, bevel=0.06, moss=0.45, at=(x, y, 0), sink=0.12)
        o.rotation_euler = Euler((r.uniform(-0.25, 0.25), r.uniform(-0.25, 0.25), r.uniform(0, 3.1)))
        rubble.append(o)
        K.tuft(pl, (x + 0.5, y, 0), r, n=5, h=0.3, w=0.05, lean=0.7, cell=3)
    pilaster(rock, r)
    vine(pl, (1.4, -0.16, PL.Hh - 0.6), 2.2, r)
    vine(pl, (1.6, -0.56, PL.LEDGE[1] - 0.05), 2.0, r)
    done("pit_wall_broken", [face.obj("pit_wall_broken_face", m["pit"]), rock.obj("pit_wall_broken_blocks", m["rock"]),
                             join(rubble, "pit_wall_broken_rubble", m["rock"]), pl.obj("pit_wall_broken_green", m["plant"])],
         pivot="foot of the face, left end (the whole wall is on its left)")


def pit_kerb():
    K.reset(); m = mats(); r = random.Random(39)
    rock, pl = K.MB(), K.MB()
    x = 0.0
    while x < PL.W - 0.05:
        w = r.uniform(1.0, 1.7)
        if PL.W - (x + w) < 0.8:
            w = PL.W - x
        block(rock, (x + 0.015, -r.uniform(0.18, 0.26), 0.0), (x + w - 0.015, r.uniform(0.70, 0.85), r.uniform(0.34, 0.44)), r, bev=0.07, moss=r.uniform(0.0, 0.5))
        if r.random() < 0.5:
            K.tuft(pl, (x + w, r.uniform(0.2, 0.7), 0.25), r, n=4, h=r.uniform(0.22, 0.34), w=0.05, lean=0.7, cell=3, shade=0.9)
        x += w
    done("pit_kerb", [rock.obj("pit_kerb_blocks", m["rock"]), pl.obj("pit_kerb_green", m["plant"])], pivot="top of the wall face, left end")


# ------------------------------------------------------------------------------------------------ the rock cliff
def crag(m, r, at, size, turn, moss=0.5, cuts=9):
    o = K.rock_object("r", m["rock"], r, size, cuts=cuts, bevel=0.06 + 0.05 * max(size), tile=3.2, moss=moss, at=at, sink=0.0)
    o.rotation_euler = Euler(turn)
    return o


def cliff():
    K.reset(); m = mats(); r = random.Random(44)
    rocks, pl, back = [], K.MB(), K.MB()
    tops = []
    z = -0.4
    row = 0
    while z < 12.0:
        h = r.uniform(2.2, 3.0)
        x = -0.8 + (2.2 if row % 2 else 0.0) + r.uniform(-0.4, 0.4)
        while x < 12.2:
            w = r.uniform(4.4, 6.4)
            d = r.uniform(1.8, 2.6)
            y = r.uniform(0.25, 0.75)
            if x + w * 0.5 < 12.6:
                rocks.append(crag(m, r, (x + w * 0.45, y, z), (w * 1.12, d, h * 1.18), (r.uniform(-0.05, 0.05), r.uniform(-0.06, 0.06), r.uniform(-0.10, 0.10)), moss=0.62))
                tops.append((x + w * 0.45, y - d * 0.42, z + h * 1.02))
            x += w * 0.82
        z += h * 0.86
        row += 1
    for _ in range(6):                                                    # smaller rocks standing proud: ledges to catch the light
        x, zz = r.uniform(0.8, 11.2), r.uniform(0.6, 10.8)
        s = r.uniform(1.0, 1.7)
        rocks.append(crag(m, r, (x, -0.55, zz), (s * 1.6, s * 1.1, s * 0.8), (0, r.uniform(-0.1, 0.1), r.uniform(-0.3, 0.3)), moss=0.7, cuts=8))
        tops.append((x, -0.75, zz + s * 0.72))
    back.face([(0, 1.0, 0), (12, 1.0, 0), (12, 1.0, 12), (0, 1.0, 12)], [(0, 0), (4, 0), (4, 4), (0, 4)], (0.22, 0.22, 0.24, 1))
    r.shuffle(tops)
    for x, y, zz in tops[:15]:
        if 0.3 < x < 11.7 and zz < 11.8:
            if r.random() < 0.55:
                for k in range(7):
                    a = k * 6.283 / 7 + r.uniform(-0.3, 0.3)
                    K.leaf(pl, (x, y, zz), Vector((math.cos(a), math.sin(a), 0)), r.uniform(0.5, 0.8), 0.2, 4, r, up=r.uniform(0.3, 0.7), shade=r.uniform(0.85, 1))
            else:
                vine(pl, (x, y - 0.15, zz), r.uniform(1.5, 3.5), r)
            K.tuft(pl, (x + 0.4, y, zz), r, n=5, h=r.uniform(0.25, 0.4), w=0.06, lean=0.7, cell=3)
    done("cliff_wall", [join(rocks, "cliff_wall_rock", m["rock"]), back.obj("cliff_wall_back", m["rock"]), pl.obj("cliff_wall_green", m["plant"])],
         pivot="foot of the face, left end; the face stands about 0.6 m in front of the pivot plane, rocks reach 1.5 m behind it")

    K.reset(); m = mats(); r = random.Random(45)
    rocks, pl = [], K.MB()
    z = -0.4
    row = 0
    while z < 12.0:
        h = r.uniform(2.7, 3.5)
        for k in range(3):
            a = math.radians(-90 + 45 * k + (22 if row % 2 else 0) + r.uniform(-8, 8))    # -90: facing -y, 0: facing +x
            rad = 1.3
            c = Vector((-rad + math.cos(a) * rad * 1.45, rad + math.sin(a) * rad * 1.45, z))
            rocks.append(crag(m, r, tuple(c), (r.uniform(3.0, 3.8), r.uniform(2.6, 3.2), h * 1.18), (r.uniform(-0.06, 0.06), r.uniform(-0.06, 0.06), a + math.pi / 2 + r.uniform(-0.2, 0.2))))
        if row % 2 == 0:
            vine(pl, (r.uniform(-0.6, 0.3), -0.9, z + h), r.uniform(1.5, 3.0), r)
        z += h * 0.86
        row += 1
    done("cliff_corner", [join(rocks, "cliff_corner_rock", m["rock"]), pl.obj("cliff_corner_green", m["plant"])], pivot="foot of the corner line (as pit_corner_out)")


# ------------------------------------------------------------------------------------------------ ground
def slab(mb, r, at, w, d, tile=2.5):
    """A flat stepping stone: an uneven seven-sided top, a pale chamfer round it, a dark skirt into the ground."""
    n = 7
    a0 = r.uniform(0, 6.28)
    ring = []
    for k in range(n):
        a = a0 + k * 6.283 / n + r.uniform(-0.25, 0.25)
        ring.append(Vector((at[0] + math.cos(a) * w * 0.5 * r.uniform(0.82, 1.08), at[1] + math.sin(a) * d * 0.5 * r.uniform(0.82, 1.08), 0)))
    c = Vector((at[0], at[1], 0))
    h = r.uniform(0.06, 0.10)
    top = [c + (p - c) * 0.86 + Vector((0, 0, h)) for p in ring]
    mid = [p + Vector((0, 0, h - 0.035)) for p in ring]
    low = [c + (p - c) * 1.05 + Vector((0, 0, -0.05)) for p in ring]
    off = (r.uniform(0, 1), r.uniform(0, 1))
    uv = lambda p: (p.x / tile + off[0], p.y / tile + off[1])
    k = r.uniform(0.86, 0.98)
    mossy = r.random() < 0.35
    tc = (k * 0.93, k * 0.93, k * 0.92, 1)
    mb.face(top, [uv(p) for p in top], tc)
    for i in range(n):
        j = (i + 1) % n
        mb.face([mid[i], mid[j], top[j], top[i]], [uv(mid[i]), uv(mid[j]), uv(top[j]), uv(top[i])], (k, k, k, 1))
        lc = (0.38, 0.52, 0.30, 1) if mossy else (0.55 * k, 0.55 * k, 0.55 * k, 1)
        mb.face([low[i], low[j], mid[j], mid[i]], [uv(low[i]), uv(low[j]), uv(mid[j]), uv(mid[i])], [lc, lc, (0.7 * k, 0.7 * k, 0.7 * k, 1), (0.7 * k, 0.7 * k, 0.7 * k, 1)])


def ground():
    for kind in ("grass", "forest"):
        K.reset(); m = mats(); r = random.Random(52 if kind == "grass" else 53)
        mb, pl = K.MB(), K.MB()
        n = 6
        hgt = [[0.0 if i in (0, n) or j in (0, n) else r.uniform(0.02, 0.24) for i in range(n + 1)] for j in range(n + 1)]
        tone = [[r.uniform(0.86, 1.0) for i in range(n + 1)] for j in range(n + 1)]
        P = lambda i, j: Vector((i * 12 / n, j * 12 / n, hgt[j][i]))
        C = lambda i, j: (tone[j][i], tone[j][i], tone[j][i], 1)
        for j in range(n):
            for i in range(n):
                q = [(i, j), (i + 1, j), (i + 1, j + 1), (i, j + 1)]
                mb.face([P(*a) for a in q], [(a[0] * 12 / n / 4, a[1] * 12 / n / 4) for a in q], [C(*a) for a in q], smooth=True)
        rocks = []
        for _ in range(34 if kind == "grass" else 14):
            x, y = r.uniform(0.6, 11.4), r.uniform(0.6, 11.4)
            K.tuft(pl, (x, y, 0.05), r, n=5, h=r.uniform(0.22, 0.42), w=0.06, lean=0.6, cell=3, shade=r.uniform(0.85, 1))
            if kind == "grass" and r.random() < 0.22:
                top = Vector((x + 0.05, y, r.uniform(0.4, 0.55)))
                K.blade(pl, (x, y, 0.05), Vector((1, 0, 0)), top.z, 0.02, 0.1, 3, r, shade=0.8)
                for q in range(5):
                    K.blade(pl, top, Vector((math.cos(q * 1.257), math.sin(q * 1.257), 0)), 0.05, 0.075, 2.2, 6, r)
        if kind == "forest":
            for _ in range(9):
                x, y = r.uniform(0.8, 11.2), r.uniform(0.8, 11.2)
                for k in range(8):
                    a = k * 6.283 / 8 + r.uniform(-0.3, 0.3)
                    ln = r.uniform(0.5, 0.85)
                    K.leaf(pl, (x, y, 0.06), Vector((math.cos(a), math.sin(a), 0)), ln, ln * 0.3, 4, r, up=r.uniform(0.35, 0.75), shade=r.uniform(0.8, 1))
            for _ in range(4):
                s = r.uniform(0.4, 0.9)
                rocks.append(K.rock_object("r", m["rock"], r, (s * 1.3, s, s * 0.7), cuts=7, bevel=0.05, moss=0.7, at=(r.uniform(1, 11), r.uniform(1, 11), 0.05), sink=0.25))
        name = "ground_%s_tile" % kind
        obs = [mb.obj(name + "_ground", m[kind]), pl.obj(name + "_plants", m["plant"])]
        if rocks:
            obs.append(join(rocks, name + "_rocks", m["rock"]))
        done(name, obs, pivot="corner (0, 0); 12 x 12 m, flat at its edges", texture_tile_m=4)

    K.reset(); m = mats(); r = random.Random(54)
    sl, pl = K.MB(), K.MB()
    y = 0.35
    side = 1
    while y < 5.9:
        w, d = r.uniform(0.62, 0.95), r.uniform(0.48, 0.72)
        x = side * r.uniform(0.12, 0.34)
        slab(sl, r, (x, y), w, d)
        if r.random() < 0.6:
            K.tuft(pl, (x - side * 0.55, y + 0.2, 0), r, n=5, h=r.uniform(0.15, 0.28), w=0.05, lean=0.7, cell=3, shade=r.uniform(0.85, 1))
        y += d * r.uniform(0.62, 0.8)
        side = -side
    done("path_stones", [sl.obj("path_stones_stones", m["rock"]), pl.obj("path_stones_weeds", m["plant"])], pivot="middle of the near end; 6 m long, next piece at +6 m", length=6.0)


if __name__ == "__main__":
    words = [a for a in sys.argv[sys.argv.index("--") + 1:]] if "--" in sys.argv else []
    words = words or ["pit", "cliff", "ground"]
    if "pit" in words:
        for v in ("plain", "niche", "spout"):
            pit_wall(v)
        pit_corners(); pit_broken(); pit_kerb()
    if "cliff" in words:
        cliff()
    if "ground" in words:
        ground()
    path = os.path.join(K.OUT, "terrace_stats.json")
    old = json.load(open(path)) if os.path.exists(path) else {}
    old.update(stats)
    json.dump(old, open(path, "w"), indent=1)
    print("done")
