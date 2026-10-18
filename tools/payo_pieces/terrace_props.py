"""PAYO land kit: the small pieces that live on the terraces (Blender).

  blender -b --python tools/payo_pieces/terrace_props.py

Writes into Logs/payo/pieces/build/terrace: terrace_stair, terrace_pipe, terrace_scarecrow, terrace_rack,
boulder_1..5, cover_fern, cover_taro, cover_grass (.glb each). Blender while building: front is -Y, +Z up; pivots on
the ground under the middle of the piece unless said.

  terrace_stair      the Ifugao way up a wall: flat stones built INTO the wall and standing out of it, climbing
                     sideways. Pivot at the wall's foot under the first step; it lies against a 1.15 m terrace wall.
  terrace_pipe       3 m of split-bamboo water run on crossed sticks, dropping 12 cm; pieces chain end to end.
  terrace_scarecrow  bamboo cross, sack face, straw hat, a woven red shirt.   terrace_rack  bamboo rack hung with sheaves.
"""
import json
import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import terrace_layout as TL   # noqa: E402
import terrace_lib as K       # noqa: E402
import terrace_falls as F     # noqa: E402
import bpy                     # noqa: E402
from mathutils import Vector  # noqa: E402

BAMBOO, TIMBER, STRAW = (0, 1, 0.76, 0.99), (0, 1, 0.51, 0.74), (0, 1, 0.26, 0.49)
stats = {}


def mats():
    return dict(wood=K.material("py_terrace_wood", "py_terrace_wood.png"), plant=K.material("py_terrace_plant", "py_terrace_plant.png"),
                rock=K.material("py_rock", "py_rock.png"), fall=K.material("py_fall", "py_fall.png", alpha=True),
                foam=K.material("py_foam", "py_foam.png"))


def done(name, obs, **meta):
    stats[name] = K.export(name, obs, dict(parts={o.name.replace(name + "_", ""): sum(len(p.vertices) - 2 for p in o.data.polygons) for o in obs}, **meta))


def both(mb, pts, uvs, col=K.WHITE):
    mb.face(pts, uvs, col)
    mb.face(list(reversed(pts)), list(reversed(uvs)), col)


# ------------------------------------------------------------------------------------------------ stair
def stair():
    K.reset(); m = mats(); r = random.Random(12)
    tb = math.tan(TL.BATTER)
    rocks = []
    n = 5
    rise = TL.H / (n + 0.5)
    rocks.append(K.rock_object("r", m["rock"], r, (0.95, 0.75, 0.30), cuts=5, bevel=0.05, moss=0.25, at=(-0.15, -0.45, 0), sink=0.25))   # the foot stone
    for i in range(n):
        z = rise * (i + 1)
        x = 0.42 * (i + 1)
        w, d = r.uniform(0.56, 0.68), r.uniform(0.62, 0.72)
        rocks.append(K.rock_object("r", m["rock"], r, (w, d, 0.22), cuts=4, bevel=0.045, moss=0.18,
                                   at=(x, z * tb - d * 0.5 + 0.16, z - 0.22), sink=0.0))
    rocks.append(K.rock_object("r", m["rock"], r, (0.9, 0.7, 0.2), cuts=4, bevel=0.04, moss=0.3, at=(0.42 * (n + 1) + 0.1, TL.H * tb + 0.22, TL.H - 0.13), sink=0.0))
    stones = F.join(rocks, "terrace_stair_stones", m["rock"])
    pl = K.MB()
    for i in range(4):                                                  # weeds where the steps meet the wall
        z = rise * (i + 1)
        K.tuft(pl, (0.42 * (i + 1) + r.uniform(-0.2, 0.2), z * tb + 0.02, z), r, n=4, h=r.uniform(0.14, 0.22), w=0.05, lean=0.8, cell=3, shade=0.9)
    K.tuft(pl, (-0.55, -0.25, 0.0), r, n=6, h=0.3, w=0.06, lean=0.6, cell=3)
    done("terrace_stair", [stones, pl.obj("terrace_stair_weeds", m["plant"])], pivot="foot of the wall under the first step", climbs=TL.H)


# ------------------------------------------------------------------------------------------------ bamboo water run
def pipe():
    K.reset(); m = mats(); r = random.Random(13)
    wd, fl, pl = K.MB(), K.MB(), K.MB()
    L, z0, z1, rad = 3.0, 0.95, 0.83, 0.11
    # the split culm: a half pipe, open upward
    seg = 6
    for i in range(6):
        xa, xb = L * i / 6, L * (i + 1) / 6
        za, zb = z0 + (z1 - z0) * i / 6, z0 + (z1 - z0) * (i + 1) / 6
        for k in range(seg):
            a0, a1 = math.pi + math.pi * k / seg, math.pi + math.pi * (k + 1) / seg
            pa = lambda x, z, a, rr=rad: Vector((x, math.cos(a) * rr, z + math.sin(a) * rr))
            va, vb = 0.76 + 0.23 * k / seg, 0.76 + 0.23 * (k + 1) / seg
            q = [pa(xa, za, a0), pa(xb, zb, a0), pa(xb, zb, a1), pa(xa, za, a1)]
            wd.face(q[::-1], [(xa / 1.6, va), (xb / 1.6, va), (xb / 1.6, vb), (xa / 1.6, vb)][::-1], smooth=True)
            q2 = [pa(xa, za, a1, rad * 0.82), pa(xb, zb, a1, rad * 0.82), pa(xb, zb, a0, rad * 0.82), pa(xa, za, a0, rad * 0.82)]
            wd.face(q2[::-1], [(xa / 1.6, 0.88), (xb / 1.6, 0.88), (xb / 1.6, 0.90), (xa / 1.6, 0.90)], (0.8, 0.78, 0.6, 1), smooth=True)
        w = rad * 0.72                                                   # the water running in it
        fl.face([(xa, -w, za - rad * 0.35), (xb, -w, zb - rad * 0.35), (xb, w, zb - rad * 0.35), (xa, w, za - rad * 0.35)],
                [(0.30, 1 - xa / 2), (0.30, 1 - xb / 2), (0.42, 1 - xb / 2), (0.42, 1 - xa / 2)])
    prev, pw = Vector((L, 0, z1 - rad * 0.35)), rad * 0.72            # the spill off the low end (leave the object out when chaining)
    sp = K.MB()
    for i in range(1, 6):
        k = i / 5
        p = Vector((L + 0.32 * k ** 0.6, 0, (z1 - rad * 0.35) * (1 - k ** 1.7)))
        w = rad * 0.72 + 0.04 * k
        for ax in (Vector((0, 1, 0)), Vector((1, 0, 0)) * 0.6):
            sp.face([prev - ax * pw, p - ax * w, p + ax * w, prev + ax * pw], [(0.30, 0.9 - 0.4 * (k - 0.2)), (0.30, 0.9 - 0.4 * k), (0.42, 0.9 - 0.4 * k), (0.42, 0.9 - 0.4 * (k - 0.2))], smooth=True)
        prev, pw = p, w
    for x in (0.45, 2.55):                                               # crossed sticks, lashed
        z = z0 + (z1 - z0) * x / L - rad
        for s in (-1, 1):
            K.tube(wd, [(x, s * 0.34, 0.0), (x + r.uniform(-0.03, 0.03), -s * 0.13, z + 0.26)], 0.034, seg=6, uv=BAMBOO, metres_u=0.9)
        K.tube(wd, [(x - 0.04, -0.09, z - 0.03), (x - 0.04, 0.09, z - 0.03), (x + 0.04, 0.09, z - 0.03), (x + 0.04, -0.09, z - 0.03), (x - 0.04, -0.09, z - 0.03)],
               0.018, seg=5, uv=STRAW, cap=False)
        K.tuft(pl, (x + 0.1, 0.2, 0), r, n=5, h=0.25, w=0.05, lean=0.7, cell=3)
    done("terrace_pipe", [wd.obj("terrace_pipe_bamboo", m["wood"]), fl.obj("terrace_pipe_water", m["fall"]), sp.obj("terrace_pipe_spill", m["fall"]), pl.obj("terrace_pipe_weeds", m["plant"])],
         pivot="ground under the high end", next_piece_at=[3.0, 0.0, -0.12])


# ------------------------------------------------------------------------------------------------ scarecrow
def scarecrow():
    K.reset(); m = mats(); r = random.Random(14)
    wd, pl = K.MB(), K.MB()
    K.tube(wd, [(0, 0, -0.1), (0.02, 0, 0.9), (-0.03, 0.02, 1.86)], 0.035, seg=7, uv=BAMBOO, metres_u=1.6)
    K.tube(wd, [(-0.86, 0.0, 1.40), (0, 0.03, 1.47), (0.84, 0.0, 1.52)], 0.028, seg=6, uv=BAMBOO, metres_u=1.6)
    # the head: a stuffed sack, its face a painted patch on the front
    plain = (0.80, 0.02, 0.84, 0.05)
    K.blob(wd, (-0.03, 0.0, 1.78), (0.17, 0.16, 0.19), r, seg=10, rings=7, jitter=0.05, uvbox=plain,
           colour=lambda n, p: (1, 1, 1, 1) if n.z > -0.3 else (0.7, 0.66, 0.6, 1))
    g = 4
    for i in range(g):
        for j in range(g):
            def P(a, b):
                ph = math.radians(-58 + 116 * a / g); th = math.radians(55 + 78 * b / g)
                return Vector((-0.03 + math.sin(ph) * math.sin(th) * 0.178, -math.cos(ph) * math.sin(th) * 0.168, 1.78 + math.cos(th) * 0.198))
            uv = lambda a, b: (0.755 + 0.24 * a / g, 0.245 - 0.24 * b / g)
            wd.face([P(i, j + 1), P(i + 1, j + 1), P(i + 1, j), P(i, j)], [uv(i, j + 1), uv(i + 1, j + 1), uv(i + 1, j), uv(i, j)], smooth=True)
    # the hat: a wide straw cone, tipped back
    K.tube(wd, [(-0.03, 0.03, 1.90), (-0.03, 0.04, 1.97), (-0.03, 0.06, 2.12)], 0.1, seg=10, uv=STRAW, metres_u=0.5, radii=[0.40, 0.24, 0.02], cap=False)
    wd.face([Vector((-0.03 + math.cos(a) * 0.40, 0.03 + math.sin(a) * 0.40, 1.90)) for a in [6.2832 * k / 10 for k in range(10)]][::-1],
            [(0.5, 0.3)] * 10, (0.55, 0.5, 0.4, 1))
    # the shirt: woven red cloth over the arms, hem in rags
    cols = 6
    for side, y in ((-1, -0.06), (1, 0.08)):
        for i in range(cols):
            xa, xb = -0.50 + 1.0 * i / cols, -0.50 + 1.0 * (i + 1) / cols
            za = 1.44 + 0.05 * (xa + 0.5); zb = 1.44 + 0.05 * (xb + 0.5)
            ha, hb = 0.74 + r.uniform(-0.09, 0.07), 0.74 + r.uniform(-0.09, 0.07)
            fl = 1.25
            both(wd, [(xa * fl, y * 2.2, ha), (xb * fl, y * 2.2, hb), (xb, y, zb), (xa, y, za)],
                 [(0.7 * i / cols, 0.01), (0.7 * (i + 1) / cols, 0.01), (0.7 * (i + 1) / cols, 0.24), (0.7 * i / cols, 0.24)],
                 (1, 1, 1, 1) if side < 0 else (0.72, 0.7, 0.7, 1))
    for x in (-0.86, 0.84):                                              # straw hands, straw at the neck and feet
        for k in range(7):
            a = k * 0.9
            K.blade(pl, (x, 0, 1.40 if x < 0 else 1.52), Vector((math.copysign(1, x), math.cos(a) * 0.5, 0)).normalized(), 0.05 + 0.05 * math.sin(a) ** 2, 0.045, 2.2, 7, r, droop=0.6)
    K.tuft(pl, (-0.03, 0, 1.58), r, n=8, h=0.05, w=0.035, lean=3.0, cell=7, droop=1.5)
    K.tuft(pl, (0.1, 0.1, 0), r, n=6, h=0.3, w=0.05, lean=0.6, cell=3)
    done("terrace_scarecrow", [wd.obj("terrace_scarecrow_body", m["wood"]), pl.obj("terrace_scarecrow_straw", m["plant"])], pivot="ground at the pole", height=2.12)


# ------------------------------------------------------------------------------------------------ drying rack
def rack():
    K.reset(); m = mats(); r = random.Random(15)
    wd, pl = K.MB(), K.MB()
    W = 2.4
    for x in (-W / 2, W / 2):
        for s in (-1, 1):
            K.tube(wd, [(x, s * 0.42, 0.0), (x + r.uniform(-0.03, 0.03), -s * 0.06, 1.62)], 0.034, seg=6, uv=BAMBOO, metres_u=1.6)
    rails = [(1.46, 0.0), (0.92, -0.20), (0.92, 0.20)]
    for z, y in rails:
        K.tube(wd, [(-W / 2 - 0.22, y, z), (0, y, z - 0.03), (W / 2 + 0.22, y, z)], 0.028, seg=6, uv=BAMBOO, metres_u=1.6)
    for z, y in rails:                                                   # sheaves thrown over the rails
        n = 7
        for i in range(n):
            x = -W / 2 + 0.18 + (W - 0.36) * i / (n - 1) + r.uniform(-0.04, 0.04)
            if r.random() < 0.12:
                continue
            ln = r.uniform(0.30, 0.40)
            sp = r.uniform(0.07, 0.10)
            K.tube(wd, [(x, y - sp, z - ln), (x, y - sp * 0.8, z - 0.02), (x, y, z + 0.06), (x, y + sp * 0.8, z - 0.02), (x, y + sp, z - ln)], 0.06, seg=6,
                   uv=(r.uniform(0, 1), 1, 0.26, 0.49), metres_u=0.8, cap=False, radii=[0.085, 0.06, 0.05, 0.06, 0.085])
            for s in (-1, 1):                                            # the heavy heads of grain hanging down
                for k in range(5):
                    a = k * 6.283 / 5 + r.uniform(-0.3, 0.3)
                    o = Vector((math.cos(a), math.sin(a) + s * 0.6, 0)).normalized()
                    K.blade(pl, (x + math.cos(a) * 0.04, y + s * sp + math.sin(a) * 0.03, z - ln + 0.03), o, 0.03, 0.12, 1.3, 2, r, droop=r.uniform(3.0, 5.0))
    for x in (-W / 2, W / 2):
        K.tuft(pl, (x + 0.1, 0.3, 0), r, n=5, h=0.25, w=0.05, lean=0.7, cell=3)
    done("terrace_rack", [wd.obj("terrace_rack_bamboo", m["wood"]), pl.obj("terrace_rack_grain", m["plant"])], pivot="ground under the middle", width=W + 0.44)


# ------------------------------------------------------------------------------------------------ boulders and ground cover
def boulders():
    for i, (size, cuts, moss) in enumerate((((0.7, 0.6, 0.5), 8, 0.55), ((1.1, 0.9, 0.75), 9, 0.6), ((1.7, 1.3, 1.0), 10, 0.6),
                                            ((1.5, 1.4, 2.3), 11, 0.5), ((3.2, 2.4, 1.5), 12, 0.65)), 1):
        K.reset(); m = mats(); r = random.Random(40 + i)
        rock = K.rock_object("boulder_%d_rock" % i, m["rock"], r, size, cuts=cuts, bevel=0.05 + 0.035 * max(size), moss=moss, sink=0.14)
        pl = K.MB()
        for k in range(3 + i):
            a = r.uniform(0, 6.28)
            K.tuft(pl, (math.cos(a) * size[0] * 0.5, math.sin(a) * size[1] * 0.5, 0), r, n=5, h=r.uniform(0.18, 0.34), w=0.05, lean=0.7, cell=3, shade=r.uniform(0.85, 1))
        done("boulder_%d" % i, [rock, pl.obj("boulder_%d_weeds" % i, m["plant"])], pivot="ground under the middle")


def covers():
    K.reset(); m = mats(); r = random.Random(50)
    pl = K.MB()
    for k in range(11):                                                  # a fern: fronds arching out of one crown
        a = k * 6.283 / 11 + r.uniform(-0.2, 0.2)
        o = Vector((math.cos(a), math.sin(a), 0))
        ln = r.uniform(0.55, 0.85)
        K.leaf(pl, (o.x * 0.04, o.y * 0.04, 0.02), o, ln, ln * 0.30, 4, r, up=r.uniform(0.35, 0.75), shade=r.uniform(0.85, 1.0))
    for k in range(5):
        a = k * 6.283 / 5 + 0.5
        o = Vector((math.cos(a), math.sin(a), 0))
        K.leaf(pl, (0, 0, 0.03), o, 0.38, 0.13, 4, r, up=1.3, shade=1.0)
    done("cover_fern", [pl.obj("cover_fern_leaves", m["plant"])], pivot="ground at the crown")

    K.reset(); m = mats(); pl = K.MB()
    for k in range(7):                                                   # taro: big heart leaves on tall stems
        a = k * 6.283 / 7 + r.uniform(-0.3, 0.3)
        o = Vector((math.cos(a), math.sin(a), 0))
        h = r.uniform(0.45, 0.95)
        top = Vector((o.x * h * 0.45, o.y * h * 0.45, h))
        K.tube(pl, [(o.x * 0.05, o.y * 0.05, 0), (o.x * h * 0.2, o.y * h * 0.2, h * 0.6), top], 0.03, seg=5, uv=(0, 1, 0.1, 0.3), cap=False,
               col=(0.55, 0.85, 0.5, 1))
        sz = r.uniform(0.55, 0.78)
        K.leaf(pl, top - o * sz * 0.3, o, sz, sz * 0.95, 5, r, up=r.uniform(-0.30, -0.05), shade=r.uniform(0.85, 1.0))
    done("cover_taro", [pl.obj("cover_taro_leaves", m["plant"])], pivot="ground at the crown")

    K.reset(); m = mats(); pl = K.MB()
    for k in range(7):                                                   # grass with red flowers standing in it
        a, d = r.uniform(0, 6.28), r.uniform(0, 0.42)
        p = Vector((math.cos(a) * d, math.sin(a) * d, 0))
        K.tuft(pl, p, r, n=6, h=r.uniform(0.28, 0.5), w=0.055, lean=0.55, cell=3, shade=r.uniform(0.85, 1.0))
    for k in range(5):
        a, d = r.uniform(0, 6.28), r.uniform(0.05, 0.4)
        p = Vector((math.cos(a) * d, math.sin(a) * d, 0))
        top = p + Vector((r.uniform(-0.06, 0.06), r.uniform(-0.06, 0.06), r.uniform(0.42, 0.62)))
        K.blade(pl, p, Vector((1, 0, 0)), top.z, 0.02, (top.x - p.x) / top.z, 3, r, shade=0.8)
        K.blade(pl, p, Vector((0, 1, 0)), top.z, 0.02, (top.y - p.y) / top.z, 3, r, shade=0.8)
        for q in range(5):
            aa = q * 6.283 / 5
            K.blade(pl, top, Vector((math.cos(aa), math.sin(aa), 0)), 0.05, 0.075, 2.2, 6, r)
    done("cover_grass", [pl.obj("cover_grass_leaves", m["plant"])], pivot="ground at the middle")


if __name__ == "__main__":
    stair(); pipe(); scarecrow(); rack(); boulders(); covers()
    path = os.path.join(K.OUT, "terrace_stats.json")
    old = json.load(open(path)) if os.path.exists(path) else {}
    old.update(stats)
    json.dump(old, open(path, "w"), indent=1)
    print("done")
