"""PAYO land kit: THE WATERFALLS (Blender).

  blender -b --python tools/payo_pieces/terrace_falls.py

Writes Logs/payo/pieces/build/terrace/fall_small.glb (2 m), fall_medium.glb (8 m), fall_great.glb (40 m).

ONE FALL: pivot at the foot of the cliff straight under the middle of the lip. Blender while building: the water pours
toward -Y, +Z up (in the glb: y up, it pours toward +z). Objects, each its own so the game can treat them apart:
  sheet (py_fall)   a curved sheet leaving the lip, widening as it drops, with separate strands standing off it.
                    UV: v falls as the water falls (scroll +v to run it), u across; vertex colour goes from glassy
                    blue-green at the lip to white at the foot.
  foam (py_foam)    the lumpy white skirt at the foot.       mist (py_mist)  rounded puffs, meant soft and drifting.
  wet (py_wet)      a dark stain decal for the rock behind.  pool (py_water) the plunge pool surface.
  lip, rim (py_rock) the rocks the water leaves from, and the ring round the pool. Grey stone, moss on top.
"""
import json
import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import terrace_lib as K       # noqa: E402
import bmesh                   # noqa: E402
import bpy                     # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402


def join(objs, name, mat):
    bm = bmesh.new()
    for o in objs:
        o.data.transform(Matrix.Translation(o.location))
        bm.from_mesh(o.data)
        bpy.data.objects.remove(o)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me); bm.free()
    me.materials.append(mat)
    K.activate(me)
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    return ob


def ribbon(mb, Hf, throw, w0, w1, uc, frac, q0, q1, off, urange, tile, cols, rows, r, bulge=0.12, ragged=0.0, lip_run=0.0):
    """One sheet of falling water. uc: where across the fall it hangs (-1..1), frac: its share of the fall's width,
    q0..q1: the part of the drop it covers, off: how far it stands in front of the main sheet."""
    def centre(q):
        qq = max(0.0, q)
        return -throw * math.sqrt(qq) - off * min(1.0, qq * 6 + 0.15), Hf * (1 - qq)

    def width(q):
        return w0 + (w1 - w0) * max(0.0, q) ** 0.8

    qs = [q0 + (q1 - q0) * (i / rows) ** 1.5 for i in range(rows + 1)]
    ends = [q1 - r.uniform(0, ragged) for _ in range(cols + 1)]
    dist = [0.0]
    for i in range(1, rows + 1):
        a, b = centre(qs[i - 1]), centre(qs[i])
        dist.append(dist[-1] + math.hypot(a[0] - b[0], a[1] - b[1]))
    grid = []
    for i, q in enumerate(qs):
        row = []
        for j in range(cols + 1):
            u = -1 + 2 * j / cols
            qq = min(q, ends[j]) if i == rows else q
            y, z = centre(qq)
            wf = width(qq)
            x = (uc + u * frac) * wf / 2
            y -= bulge * wf * frac * (1 - u * u)                      # curved across: it bellies toward you
            k = max(0.0, min(1.0, (qq - 0.0) / 1.0))
            tint = (0.62 + 0.38 * k ** 0.6, 0.86 + 0.14 * k ** 0.6, 0.90 + 0.10 * k ** 0.6, 1.0)
            if i == 0 and q0 <= 0.001:
                tint = (0.50, 0.80, 0.86, 1.0)
            row.append((Vector((x, y, z)), (urange[0] + (urange[1] - urange[0]) * j / cols, 1.0 - dist[i] / tile), tint))
        grid.append(row)
    if lip_run and q0 <= 0.001:                                         # the water lying on the lip before it goes
        back = [(p + Vector((0, lip_run, 0.0)), (uv[0], uv[1] + lip_run / tile), (0.40, 0.72, 0.80, 1.0)) for p, uv, c in grid[0]]
        grid.insert(0, back)
    for i in range(len(grid) - 1):
        for j in range(cols):
            a, b, c, d = grid[i][j], grid[i][j + 1], grid[i + 1][j + 1], grid[i + 1][j]
            mb.face([a[0], d[0], c[0], b[0]], [a[1], d[1], c[1], b[1]], [a[2], d[2], c[2], b[2]], smooth=True)


def puff(mb, at, size, r, lumps=3, seg=8, rings=5):
    for k in range(lumps):
        c = (at[0] + r.uniform(-0.5, 0.5) * size, at[1] + r.uniform(-0.4, 0.4) * size, at[2] + r.uniform(-0.15, 0.3) * size)
        s = size * r.uniform(0.55, 0.9)
        zc = c[2]
        K.blob(mb, c, (s, s * 0.9, s * 0.75), r, seg=seg, rings=rings, jitter=0.08,
               colour=lambda n, p: (1, 1, 1, 1) if n.z > 0.15 else (0.80, 0.93, 0.96, 1) if n.z > -0.4 else (0.62, 0.84, 0.90, 1))


def fall(name, Hf, w0, w1, throw, seed, strands, cols, rows, tile, pool=True, ledges=(), rivulets=False):
    r = random.Random(seed)
    K.reset()
    m_fall = K.material("py_fall", "py_fall.png", alpha=True)
    m_foam = K.material("py_foam", "py_foam.png")
    m_mist = K.material("py_mist", "py_foam.png")
    m_wet = K.material("py_wet", "py_wet.png", alpha=True)
    m_rock = K.material("py_rock", "py_rock.png")
    m_water = K.material("py_water", None, colour=(0.30, 0.62, 0.66, 0.85))
    obs = []
    # ---- the water
    sh = K.MB()
    uw = max(0.3, w1 / (tile / 4.0) * 0.5)
    ribbon(sh, Hf, throw, w0, w1, 0.0, 1.0, 0.0, 1.0, 0.0, (0.0, uw), tile, cols, rows, r, ragged=0.05, lip_run=max(0.4, w0 * 0.5))
    for k in range(strands):
        uc = -0.8 + 1.6 * (k + r.uniform(0.2, 0.8)) / strands
        fr = r.uniform(0.14, 0.26)
        u0 = r.uniform(0, 1)
        ribbon(sh, Hf, throw * r.uniform(1.0, 1.12), w0, w1 * r.uniform(1.0, 1.15), uc, fr, r.uniform(0.0, 0.06), r.uniform(0.82, 1.0),
               (0.02 + 0.012 * k) * w1 * r.uniform(1.0, 1.5), (u0, u0 + uw * fr * 1.3), tile * r.uniform(0.8, 1.0),
               max(2, cols // 3), rows, r, bulge=0.22, ragged=0.10)
    if rivulets:                                                        # thin side falls that find their own way down
        for side in (-1, 1):
            ribbon(sh, Hf * 0.97, throw * 0.55, w0, w1 * 0.8, side * r.uniform(1.25, 1.45), 0.07, 0.0, 1.0, 0.0, (0.2, 0.32), tile, 2, rows, r, bulge=0.3, ragged=0.03)
    obs.append(sh.obj(name + "_sheet", m_fall))
    ledge_rocks, ledge_foam = [], []
    for q, uc in ledges:                                                # rocks standing in the fall: the water breaks on them
        wq = w0 + (w1 - w0) * q ** 0.8
        sz = wq * 0.22
        c = Vector((uc * wq / 2, -throw * math.sqrt(q) + sz * 0.25, Hf * (1 - q) - sz * 0.5))
        ledge_rocks.append((c, sz))
        ledge_foam.append(c + Vector((0, -sz * 0.3, sz * 0.75)))
    # ---- foam skirt at the foot
    foot = Vector((0, -throw, 0))
    fo = K.MB()
    n = max(7, int(w1 * 4))
    for k in range(n):
        a = math.pi * (k + 0.5) / n
        rx, ry = w1 * 0.62, w1 * 0.36 + 0.1
        c = foot + Vector((math.cos(a) * rx, -math.sin(a) * ry + 0.05 * w1, 0))
        s = w1 * r.uniform(0.13, 0.22) + 0.03
        K.blob(fo, (c.x, c.y, s * 0.2), (s, s * 0.9, s * 0.62), r, seg=8, rings=5, jitter=0.10, flat_bottom=-0.02,
               colour=lambda nn, p: (1, 1, 1, 1) if nn.z > 0.1 else (0.74, 0.90, 0.94, 1))
    for k in range(max(3, n // 2)):                                     # the boil right under the sheet
        c = foot + Vector((r.uniform(-0.42, 0.42) * w1, r.uniform(-0.05, 0.15) * w1, 0))
        s = w1 * r.uniform(0.16, 0.26) + 0.03
        K.blob(fo, (c.x, c.y, s * 0.3), (s, s * 0.8, s * 0.8), r, seg=8, rings=5, jitter=0.10, flat_bottom=-0.02,
               colour=lambda nn, p: (1, 1, 1, 1) if nn.z > 0.1 else (0.74, 0.90, 0.94, 1))
    for c in ledge_foam:
        for k in range(3):
            s = w1 * r.uniform(0.07, 0.11)
            K.blob(fo, (c.x + r.uniform(-1, 1) * s, c.y - r.uniform(0, 0.5) * s, c.z + r.uniform(-0.2, 0.3) * s), (s, s * 0.8, s * 0.7), r, seg=8, rings=5, jitter=0.1,
                   colour=lambda nn, p: (1, 1, 1, 1) if nn.z > 0.1 else (0.74, 0.90, 0.94, 1))
    obs.append(fo.obj(name + "_foam", m_foam))
    # ---- mist puffs
    mi = K.MB()
    for k in range(max(3, int(3 + Hf ** 0.6))):
        a = math.pi * r.uniform(0.05, 0.95)
        d = w1 * r.uniform(0.5, 0.95)
        c = foot + Vector((math.cos(a) * d, -math.sin(a) * d * 0.6 + 0.25 * w1, w1 * r.uniform(0.25, 0.75) + Hf * r.uniform(0.0, 0.06)))
        puff(mi, c, w1 * r.uniform(0.22, 0.36) + 0.05, r)
    for c in ledge_foam:
        puff(mi, (c.x + r.uniform(-0.2, 0.2) * w1, c.y - 0.12 * w1, c.z - 0.05 * w1), w1 * 0.16, r)
    obs.append(mi.obj(name + "_mist", m_mist))
    # ---- wet stain on the rock behind
    we = K.MB()
    ww = w1 * 1.25
    we.face([(-ww / 2, 0.04, -0.0), (ww / 2, 0.04, 0.0), (w0 * 0.9, 0.04, Hf), (-w0 * 0.9, 0.04, Hf)], [(0, 0), (1, 0), (0.85, 1), (0.15, 1)])
    obs.append(we.obj(name + "_wet", m_wet))
    # ---- the lip: a ledge under the water, a shoulder of rock each side
    rocks = []
    s0 = max(0.5, w0)
    rocks.append(K.rock_object("r", m_rock, r, (s0 * 1.9, s0 * 1.5, s0 * 0.55 + 0.1), cuts=7, bevel=0.05 + 0.03 * s0, moss=0.0, at=(0, s0 * 0.55, Hf - s0 * 0.5 - 0.12), sink=0.0))
    for side in (-1, 1):
        sz = s0 * r.uniform(0.75, 1.05)
        rocks.append(K.rock_object("r", m_rock, r, (sz * 1.1, sz * 1.3, sz * 1.25), cuts=9, bevel=0.05 + 0.04 * s0, moss=0.75,
                                   at=(side * (w0 / 2 + sz * 0.42), -sz * 0.05, Hf - sz * 0.62), sink=0.0))
        sz2 = sz * 0.6
        rocks.append(K.rock_object("r", m_rock, r, (sz2, sz2, sz2 * 0.9), cuts=8, bevel=0.04 + 0.03 * s0, moss=0.75,
                                   at=(side * (w0 / 2 + sz * 1.05), -sz * 0.02, Hf - sz2 * 0.55), sink=0.0))
    for c, sz in ledge_rocks:
        rocks.append(K.rock_object("r", m_rock, r, (sz * 1.5, sz * 1.2, sz * 1.0), cuts=9, bevel=0.04 + 0.05 * sz, moss=0.3, at=(c.x, c.y, c.z), sink=0.0))
    obs.append(join(rocks, name + "_lip", m_rock))
    if pool:
        R = w1 * 1.15 + 0.35
        rocks = []
        n = max(7, int(R * 2.2) + 5)
        for k in range(n):
            a = math.radians(-12 + 204 * k / (n - 1)) + r.uniform(-0.06, 0.06)
            sz = R * r.uniform(0.30, 0.46) * (0.75 if k % 2 else 1.0)
            c = Vector((math.cos(a) * R * 1.08, -throw * 0.55 - math.sin(a) * R * 0.95, 0))
            rocks.append(K.rock_object("r", m_rock, r, (sz * r.uniform(1.0, 1.4), sz * r.uniform(0.9, 1.2), sz * r.uniform(0.6, 0.85)), cuts=9,
                                       bevel=0.04 + 0.05 * sz, moss=0.42, at=(c.x, c.y, 0), sink=0.22))
        obs.append(join(rocks, name + "_rim", m_rock))
        po = K.MB()
        seg = 14
        ring = [Vector((math.cos(6.2832 * k / seg) * R * 1.12, -throw * 0.55 + math.sin(6.2832 * k / seg) * R * 1.0, 0.02)) for k in range(seg)]
        po.face(ring, [(p.x / 4, p.y / 4) for p in ring])
        obs.append(po.obj(name + "_pool", m_water))
    return K.export(name, obs, dict(height=Hf, pivot="foot of the cliff under the middle of the lip",
                                    parts={o.name.replace(name + "_", ""): sum(len(p.vertices) - 2 for p in o.data.polygons) for o in obs}))


if __name__ == "__main__":
    stats = {}
    stats["fall_small"] = fall("fall_small", 2.0, 0.34, 0.52, 0.42, 2, strands=2, cols=4, rows=7, tile=2.0)
    stats["fall_medium"] = fall("fall_medium", 8.0, 1.7, 2.9, 1.5, 3, strands=5, cols=8, rows=12, tile=4.0)
    stats["fall_great"] = fall("fall_great", 40.0, 5.0, 10.5, 5.5, 4, strands=9, cols=12, rows=22, tile=10.0, ledges=((0.42, -0.45), (0.66, 0.38), (0.80, -0.15)), rivulets=True)
    path = os.path.join(K.OUT, "terrace_stats.json")
    old = json.load(open(path)) if os.path.exists(path) else {}
    old.update(stats)
    json.dump(old, open(path, "w"), indent=1)
    print("done")
