"""PAYO market and gallery pieces for the basin's south terrace (2026-10-19). Blender 5.0, headless.

  blender -b --python tools/payo_pieces/market_kit.py -- <ABS out folder>      (market_run.sh build does it)

Owner, 2026-10-19 (the wide paved terrace between the pit's south lip and the tall south wall, with the two carabao
statues): "this central area of the basin wall needs some work too. its still barren, with the same sparsity of
elements as the side walls, but it works for the sidewalls because its already pretty small in area. this one is
large in area so it needs something more. maybe like some audience watching, or a marketplace area."

So a small market lane and a viewing gallery go on it (PayoSceneBuilder.Market.cs places them). The pieces:

  market_stall_thatch   an open stall: four bamboo posts, a flared hipped thatch roof in the town's hut manner
                        (house_granary's golden straw, darker underside), a bamboo slat counter at the front, a
                        bunch of bananas and a rice sheaf hung under the eave. 3.4 x 2.6 m, 3.4 m tall.
  market_stall_mat      a lean-to: a sagging woven-mat roof (the red-black-white band at its ends) high at the front,
                        low at the back, on four bamboo posts, a woven mat on the ground under it, a low bamboo bench.
  market_cloth_line     two bamboo poles, a sagging rope, four Ifugao blankets hung over it.
  market_rice           tied rice sheaves standing in a cluster, two lying.
  market_bananas        hands of ripe and green bananas on a banana leaf.
  market_pots           three terracotta pots and a dark clay one.
  market_baskets        three open baskets of areca nuts, betel leaves and tubers.
  market_cloth_stack    folded woven blankets in a stack.
  market_mat            a woven ground mat (goods are set on it).
  market_stool          a low log stool.
  market_bench          a low bamboo bench.
  market_gallery        stone viewing steps (two tiers, 0.30 and 0.60 m, 6 x 1.8 m) in the court's own stone and atlas,
                        the woven band on its top edge, as plaza_kit's planter and plinth are built.

Every market piece wears ONE material `py_market` and one painted atlas (market_paint.py: CALM style, one colour and
one soft gradient per surface, normals from a height field). UVs stretch each face over its swatch (the gradient runs
down the whole face, top light). The gallery wears `py_court_stone` (the court platform's atlas and normal map).
Blender x east, y north, z up, metres; a piece's FRONT is +y (it faces Unity +z at yaw 0, as the art's figures do);
origins on the ground at the middle of the footprint. Kept cheap: low sides, no bevel segments beyond one chamfer.
"""
import json
import math
import os
import random
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
OUT = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else os.path.join(HERE, "..", "..", "Logs", "payo", "pieces", "market")
REG = json.load(open(os.path.join(OUT, "market_regions.json")))
ATLAS = os.path.join(OUT, "market_albedo.png")
SW = list(REG.keys())
PAD = 6
Z = Vector((0, 0, 1))


class Kit:
    """One piece: a bmesh whose faces carry a swatch, a part (the UV frame) and a smooth flag."""

    def __init__(self, name):
        self.name = name
        self.bm = bmesh.new()
        self.lsw = self.bm.faces.layers.int.new("sw")
        self.lpt = self.bm.faces.layers.int.new("pt")
        self.n = 0

    def part(self):
        self.n += 1
        return self.n

    def poly(self, pts, swatch, part, smooth):
        vs = [self.bm.verts.new(p) for p in pts]
        f = self.bm.faces.new(vs)
        f[self.lsw] = SW.index(swatch)
        f[self.lpt] = part
        f.smooth = smooth
        return f

    def grid(self, rows, swatch, smooth=True, closed=False, part=None, flip=False):
        """Quads between successive rows of points (each row the same count); `closed` wraps each row round."""
        part = part or self.part()
        V = [[self.bm.verts.new(p) for p in r] for r in rows]
        n = len(rows[0])
        for i in range(len(rows) - 1):
            for j in range(n if closed else n - 1):
                k = (j + 1) % n
                q = [V[i][j], V[i][k], V[i + 1][k], V[i + 1][j]]
                if flip:
                    q = q[::-1]
                try:
                    f = self.bm.faces.new(q)
                except ValueError:
                    continue
                f[self.lsw] = SW.index(swatch); f[self.lpt] = part; f.smooth = smooth
        return V, part

    def cap(self, ring_verts, swatch, part, up=True, smooth=False):
        vs = ring_verts if up else ring_verts[::-1]
        try:
            f = self.bm.faces.new(vs)
        except ValueError:
            return
        f[self.lsw] = SW.index(swatch); f[self.lpt] = part; f.smooth = smooth

    # ---------------------------------------------------------------- shapes
    def lathe(self, prof, at=(0, 0, 0), sides=10, swatch="terracotta", top=None, bottom=None, tilt=(0.0, 0.0), rz=0.0, sx=1.0):
        """A turned form: prof = [(radius, z)] bottom to top. `top`/`bottom`: a swatch to cap that end with."""
        M = Matrix.Translation(at) @ Matrix.Rotation(rz, 4, "Z") @ Matrix.Rotation(tilt[0], 4, "X") @ Matrix.Rotation(tilt[1], 4, "Y")
        rows = []
        for r, z in prof:
            rows.append([M @ Vector((r * sx * math.cos(2 * math.pi * k / sides), r * math.sin(2 * math.pi * k / sides), z)) for k in range(sides)])
        V, part = self.grid(rows, swatch, True, True)
        if bottom:
            self.cap(V[0], bottom, self.part(), up=False)
        if top:
            self.cap(V[-1], top, self.part(), up=True)
        return V

    def tube(self, path, r, sides=6, swatch="bamboo", caps=True, r1=None):
        """A tube along a polyline, radius r (to r1 at the far end)."""
        path = [Vector(p) for p in path]
        rows = []
        for i, p in enumerate(path):
            d = (path[min(i + 1, len(path) - 1)] - path[max(i - 1, 0)]).normalized()
            a = Z if abs(d.dot(Z)) < 0.9 else Vector((1, 0, 0))
            u = d.cross(a).normalized(); w = d.cross(u).normalized()
            rr = r if r1 is None else r + (r1 - r) * i / (len(path) - 1)
            rows.append([p + (u * math.cos(2 * math.pi * k / sides) + w * math.sin(2 * math.pi * k / sides)) * rr for k in range(sides)])
        V, part = self.grid(rows, swatch, True, True)
        if caps:
            self.cap(V[0], swatch, part, up=False)
            self.cap(V[-1], swatch, part, up=True)
        return V

    def box(self, c, s, swatch, rz=0.0, chamfer=0.025, top=None, tilt=0.0):
        """A chamfered box: centre c, size s; `top` a different swatch for the top face."""
        bm = bmesh.new()
        bmesh.ops.create_cube(bm, size=1.0)
        for v in bm.verts:
            v.co = Vector((v.co.x * s[0], v.co.y * s[1], v.co.z * s[2]))
        if chamfer > 0:
            bmesh.ops.bevel(bm, geom=list(bm.edges), offset=min(chamfer, min(s) * 0.4), segments=1, affect="EDGES", profile=0.5)
        M = Matrix.Translation(c) @ Matrix.Rotation(rz, 4, "Z") @ Matrix.Rotation(tilt, 4, "X")
        part = self.part()
        for f in bm.faces:
            pts = [M @ v.co for v in f.verts]
            n = (M.to_3x3() @ f.normal).normalized()
            self.poly(pts, top if (top and n.z > 0.7) else swatch, part, False)
        bm.free()

    def blob(self, c, r, swatch, seg=(6, 4), squash=1.0, part=None):
        """A low ellipsoid (nuts, tubers, mounds)."""
        part = part or self.part()
        rings = []
        for i in range(1, seg[1]):
            a = math.pi * i / seg[1]
            rings.append([Vector((c[0] + r[0] * math.sin(a) * math.cos(2 * math.pi * k / seg[0]),
                                  c[1] + r[1] * math.sin(a) * math.sin(2 * math.pi * k / seg[0]),
                                  c[2] + r[2] * squash * math.cos(a))) for k in range(seg[0])])
        rings = rings[::-1]
        V, _ = self.grid(rings, swatch, True, True, part=part)
        bot = self.bm.verts.new((c[0], c[1], c[2] - r[2] * squash)); top = self.bm.verts.new((c[0], c[1], c[2] + r[2] * squash))
        n = seg[0]
        for k in range(n):
            j = (k + 1) % n
            for tri in ((V[0][j], V[0][k], bot), (V[-1][k], V[-1][j], top)):
                try:
                    f = self.bm.faces.new(tri)
                    f[self.lsw] = SW.index(swatch); f[self.lpt] = part; f.smooth = True
                except ValueError:
                    pass

    def panel(self, P, swatch, under=None, thick=0.0, edge=None):
        """A sheet from a grid of points P[row][col] (rows run from the panel's top edge down); with `thick` a solid
        slab: the top in `swatch`, the underside in `under`, the rim in `edge`."""
        rows = [[Vector(p) for p in r] for r in P]
        if thick <= 0:
            # a cloth: the front sheet, and the back sheet 12 mm behind it (its normal the other way)
            V, part = self.grid(rows, swatch, True, False)
            self.bm.faces.ensure_lookup_table()
            self.bm.normal_update()
            n = self.bm.faces[-1].normal.copy()
            back = [[p - n * 0.012 for p in r] for r in rows]
            self.grid(back, under or swatch, True, False, flip=True)
            return
        f0 = len(self.bm.faces)
        V, part = self.grid(rows, swatch, True, False)
        down = [[p - Vector((0, 0, thick)) for p in r] for r in rows]
        self.grid(down, under or swatch, True, False, flip=True)
        e = edge or swatch
        R, C = len(rows), len(rows[0])
        border = [rows[0][j] for j in range(C)] + [rows[i][C - 1] for i in range(1, R)] + \
                 [rows[R - 1][j] for j in range(C - 2, -1, -1)] + [rows[i][0] for i in range(R - 2, 0, -1)]
        lo = [p - Vector((0, 0, thick)) for p in border]
        self.grid([border, lo], e, False, True, flip=False)
        # a closed slab: every face turned outward (the grids' winding depends on the order of the given rows)
        self.bm.faces.ensure_lookup_table()
        bmesh.ops.recalc_face_normals(self.bm, faces=[self.bm.faces[i] for i in range(f0, len(self.bm.faces))])

    # ---------------------------------------------------------------- uv and export
    def finish(self):
        bm = self.bm
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
        bm.normal_update()
        uv = bm.loops.layers.uv.new("UVMap")
        ext = {}
        for f in bm.faces:
            p = f[self.lpt]
            for v in f.verts:
                lo, hi = ext.get(p, (Vector((1e9,) * 3), Vector((-1e9,) * 3)))
                ext[p] = (Vector(map(min, lo, v.co)), Vector(map(max, hi, v.co)))
        for f in bm.faces:
            x, y, w, h = REG[SW[f[self.lsw]]]
            u0, u1 = (x + PAD) / 1024.0, (x + w - PAD) / 1024.0
            vt, vb = 1 - (y + PAD) / 1024.0, 1 - (y + h - PAD) / 1024.0
            lo, hi = ext[f[self.lpt]]
            n = f.normal
            if abs(n.z) > 0.75:
                us = [(l.vert.co.x - lo.x) / max(hi.x - lo.x, 1e-4) for l in f.loops]
                vs = [(l.vert.co.y - lo.y) / max(hi.y - lo.y, 1e-4) for l in f.loops]
                if (hi.y - lo.y) > (hi.x - lo.x) * 1.4:                     # a long thing lying north-south
                    us, vs = vs, us
            else:
                hdir = Z.cross(n).normalized()
                a = [l.vert.co.dot(hdir) for l in f.loops]
                amin, amax = min(a), max(a)
                us = [(t - amin) / max(amax - amin, 1e-4) for t in a]
                vs = [(l.vert.co.z - lo.z) / max(hi.z - lo.z, 1e-4) for l in f.loops]
            for l, uu, vv in zip(f.loops, us, vs):
                l[uv].uv = (u0 + uu * (u1 - u0), vb + vv * (vt - vb))
        me = bpy.data.meshes.new(self.name)
        bm.to_mesh(me)
        bm.free()
        me.set_sharp_from_angle(angle=math.radians(48))
        o = bpy.data.objects.new(self.name, me)
        bpy.context.scene.collection.objects.link(o)
        me.materials.append(market_material())
        return o


def market_material():
    m = bpy.data.materials.get("py_market")
    if m:
        return m
    m = bpy.data.materials.new("py_market")
    m.use_nodes = True
    nt = m.node_tree
    b = nt.nodes["Principled BSDF"]
    t = nt.nodes.new("ShaderNodeTexImage")
    t.image = bpy.data.images.load(ATLAS)
    nt.links.new(t.outputs["Color"], b.inputs["Base Color"])
    b.inputs["Roughness"].default_value = 0.9
    return m


def export(o, path):
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    bpy.ops.export_scene.gltf(filepath=path, use_selection=True, export_format="GLB", export_yup=True, export_apply=True,
                              export_normals=True, export_tangents=False, export_texcoords=True)
    tris = sum(len(p.vertices) - 2 for p in o.data.polygons)
    print("MARKET %s: %d triangles -> %s" % (o.name, tris, path))
    return tris


# ==================================================================================================== the pieces
def rrect(w, d, z, n=20, power=5.0, droop=0.0, at=(0, 0)):
    """A rounded rectangle ring (CCW from above); `droop` lowers the corners (a flared eave)."""
    pts = []
    for i in range(n):
        a = 2 * math.pi * (i + 0.5) / n
        c, s = math.cos(a), math.sin(a)
        x = math.copysign(abs(c) ** (2 / power), c) * w / 2
        y = math.copysign(abs(s) ** (2 / power), s) * d / 2
        corner = (abs(c) * abs(s)) * 2.0
        pts.append(Vector((at[0] + x, at[1] + y, z - droop * corner)))
    return pts


def thatch_roof(k, w, d, z0, h, at=(0, 0)):
    """The town's flared hipped thatch: a thick rounded eave, a concave rise to a short ridge, a darker underside."""
    tops = [rrect(w + 0.10, d + 0.10, z0 - 0.04, droop=0.10, at=at),
            rrect(w, d, z0 + 0.14, droop=0.10, at=at),
            rrect(w * 0.80, d * 0.76, z0 + h * 0.42, power=4.0, at=at),
            rrect(w * 0.55, d * 0.46, z0 + h * 0.74, power=3.5, at=at),
            rrect(w * 0.26, d * 0.12, z0 + h * 0.96, power=3.0, at=at)]
    V, part = k.grid(tops, "thatch", True, True)
    k.cap(V[-1], "thatch_dark", k.part(), up=True, smooth=True)
    unders = [rrect(w + 0.10, d + 0.10, z0 - 0.04, droop=0.10, at=at),
              rrect(w * 0.86, d * 0.84, z0 + 0.02, power=4.0, at=at),
              rrect(w * 0.45, d * 0.40, z0 + h * 0.55, power=3.0, at=at)]
    U, _ = k.grid(unders, "thatch_dark", True, True, flip=True)
    k.cap(U[-1], "thatch_dark", k.part(), up=False, smooth=True)
    # the ridge roll and a red tie band near the top, as the granary has
    k.tube([(at[0] - w * 0.15, at[1], z0 + h * 0.98), (at[0] + w * 0.15, at[1], z0 + h * 0.98)], 0.09, 6, "thatch_dark")
    band = [rrect(w * 0.40, d * 0.27, z0 + h * 0.84 - 0.06, power=3.2, at=at), rrect(w * 0.37, d * 0.24, z0 + h * 0.84 + 0.06, power=3.2, at=at)]
    for r in band:
        for p in r:
            p.xy = (Vector(p.xy) - Vector(at)) * 1.04 + Vector(at)
    k.grid(band, "red", True, True)


def banana_hand(k, at, n=5, ripe=True, yaw=0.0, droop=1.0):
    """A hand of bananas: curved fingers fanning from one crown."""
    sw = "banana" if ripe else "banana_green"
    for i in range(n):
        a = yaw + (i - (n - 1) / 2) * 0.32
        d = Vector((math.cos(a), math.sin(a), 0))
        p0 = Vector(at)
        path = [p0, p0 + d * 0.07 + Vector((0, 0, 0.03 * droop)), p0 + d * 0.15 + Vector((0, 0, 0.09 * droop)), p0 + d * 0.20 + Vector((0, 0, 0.17 * droop))]
        k.tube(path, 0.03, 5, sw, True, r1=0.018)
    k.blob(at, (0.045, 0.045, 0.04), "wood")


def rice_sheaf(k, at, tilt=(0.0, 0.0), s=1.0, rz=0.0, heads=5):
    """A tied sheaf of harvested rice: a straight bundle of stalks, a cord tie, and the grain heads drooping out over
    the top all round (the silhouette that says rice, not a vase)."""
    M = Matrix.Translation(at) @ Matrix.Rotation(rz, 4, "Z") @ Matrix.Rotation(tilt[0], 4, "X") @ Matrix.Rotation(tilt[1], 4, "Y")
    k.lathe([(0.055 * s, 0.0), (0.065 * s, 0.28 * s), (0.05 * s, 0.42 * s), (0.075 * s, 0.6 * s)], at, 7, "rice", top="rice", bottom="rice", tilt=tilt, rz=rz)
    k.lathe([(0.07 * s, 0.36 * s), (0.07 * s, 0.44 * s)], at, 7, "rope", tilt=tilt, rz=rz)
    for i in range(heads):
        a = 2 * math.pi * (i + 0.3) / heads
        d = Vector((math.cos(a), math.sin(a), 0))
        pts = [Vector((0, 0, 0.56 * s)), d * 0.09 * s + Vector((0, 0, 0.72 * s)), d * 0.19 * s + Vector((0, 0, 0.6 * s))]
        k.tube([M @ p for p in pts], 0.035 * s, 4, "rice", True, r1=0.014 * s)


def stall_thatch():
    k = Kit("market_stall_thatch")
    hx, hy, zf, zb = 1.55, 1.1, 2.25, 2.15
    for x in (-hx, hx):
        k.tube([(x, hy, 0), (x, hy, zf)], 0.075, 6, "bamboo")
        k.tube([(x, -hy, 0), (x, -hy, zb)], 0.075, 6, "bamboo")
        k.tube([(x, hy + 0.05, zf - 0.12), (x, -hy - 0.05, zb - 0.12)], 0.06, 6, "wood")      # the side beams
    for y, z in ((hy, zf - 0.08), (-hy, zb - 0.08)):
        k.tube([(-hx - 0.12, y, z), (hx + 0.12, y, z)], 0.065, 6, "wood")
    thatch_roof(k, 4.2, 3.4, 2.2, 1.25)
    # the counter: a bamboo slat top on legs along the front, a woven mat on it
    k.box((0, hy - 0.42, 0.62), (2.8, 0.82, 0.08), "bamboo", top="bamboo")
    for x in (-1.3, 1.3):
        for y in (hy - 0.08, hy - 0.76):
            k.tube([(x, y, 0), (x, y, 0.58)], 0.04, 5, "wood_light")
    k.box((0, hy - 0.42, 0.675), (2.3, 0.62, 0.025), "mat", chamfer=0.008)
    # under the eave: a hand of bananas and a rice sheaf hung on cords from the front beam
    k.tube([(-1.0, hy, zf - 0.1), (-1.0, hy, 1.75)], 0.01, 3, "rope", caps=False)
    banana_hand(k, (-1.0, hy, 1.72), 5, True, -math.pi / 2, droop=-1.0)
    k.tube([(1.05, hy, zf - 0.1), (1.05, hy, 1.85)], 0.01, 3, "rope", caps=False)
    rice_sheaf(k, (1.05, hy, 1.86), tilt=(math.pi, 0.0), s=0.7)
    # a low back shelf with two jars' worth of space (goods are added in the game)
    k.box((0, -hy + 0.3, 0.42), (2.6, 0.45, 0.06), "wood_light")
    for x in (-1.2, 1.2):
        k.tube([(x, -hy + 0.3, 0), (x, -hy + 0.3, 0.4)], 0.035, 5, "wood")
    return k.finish()


def stall_mat():
    k = Kit("market_stall_mat")
    hx, yf, yb, zf, zb = 1.6, 1.0, -1.1, 2.35, 1.55
    for x in (-hx, hx):
        k.tube([(x, yf, 0), (x, yf, zf)], 0.07, 6, "bamboo")
        k.tube([(x, yb, 0), (x, yb, zb)], 0.07, 6, "bamboo")
    k.tube([(-hx - 0.25, yf + 0.05, zf), (hx + 0.25, yf + 0.05, zf)], 0.06, 6, "bamboo")
    k.tube([(-hx - 0.25, yb - 0.05, zb), (hx + 0.25, yb - 0.05, zb)], 0.06, 6, "bamboo")
    # the woven mat roof: 5 x 4, sagging between the poles, overhanging front and back
    P = []
    for i in range(4):
        t = i / 3.0
        y = (yf + 0.3) + (yb - 0.25 - yf - 0.3) * t
        z = (zf + 0.14) + (zb + 0.0 - zf - 0.14) * t - math.sin(t * math.pi) * 0.10
        P.append([(-hx - 0.3 + (2 * hx + 0.6) * j / 4.0, y, z - math.sin(j / 4.0 * math.pi) * 0.07 + 0.06) for j in range(5)])
    k.panel(P, "mat", under="mat", thick=0.045, edge="wood_light")
    # the ground mat and a low bench at the back
    k.box((0, -0.05, 0.012), (2.7, 1.7, 0.024), "mat", chamfer=0.008)
    k.box((0, yb + 0.35, 0.36), (2.2, 0.36, 0.06), "bamboo")
    for x in (-1.0, 1.0):
        for y in (yb + 0.22, yb + 0.48):
            k.tube([(x, y, 0), (x, y, 0.33)], 0.03, 5, "wood_light")
    return k.finish()


def cloth_line():
    k = Kit("market_cloth_line")
    half, top = 2.2, 2.25
    for x in (-half, half):
        k.tube([(x, 0, 0), (x, 0, top + 0.12)], 0.06, 6, "bamboo")
    rope = [(-half + 2 * half * i / 8.0, 0, top - math.sin(math.pi * i / 8.0) * 0.22) for i in range(9)]
    k.tube(rope, 0.014, 4, "rope", caps=False)

    def rope_z(x):
        t = (x + half) / (2 * half)
        return top - math.sin(math.pi * t) * 0.22

    for x0, w, drop, sw in ((-1.75, 0.75, 1.15, "cloth_red"), (-0.85, 0.8, 1.0, "cloth_indigo"), (0.12, 0.72, 1.2, "cloth_ochre"), (1.02, 0.8, 1.05, "cloth_cream")):
        rows = []
        for i in range(4):
            t = i / 3.0
            rows.append([(x0 + w * j / 3.0, 0.04 * math.sin(j * 1.7 + x0) + 0.02 * t, rope_z(x0 + w * j / 3.0) - drop * t + 0.01) for j in range(4)])
        k.panel(rows, sw)
    return k.finish()


def rice():
    k = Kit("market_rice")
    rnd = random.Random(5)
    # a stook: the sheaves leaning in on each other, one lying in front
    for (x, y), s in (((0, 0), 1.0), ((0.2, 0.06), 0.92), ((-0.1, 0.19), 0.95), ((0.08, -0.19), 0.9), ((-0.2, -0.08), 0.88)):
        lean = Vector((-x, -y, 0)) * 0.6
        rice_sheaf(k, (x, y, 0), tilt=(-lean.y, lean.x), s=s, rz=rnd.uniform(0, 6.28))
    rice_sheaf(k, (0.5, 0.0, 0.07), tilt=(math.pi / 2, 0), s=0.85, rz=0.4)
    return k.finish()


def bananas():
    k = Kit("market_bananas")
    P = []
    for i in range(3):
        t = i / 2.0
        P.append([(-0.55 + 1.1 * j / 3.0, -0.3 + 0.6 * t, 0.015 + 0.05 * abs(j - 1.5) / 1.5 + 0.03 * abs(t - 0.5) * 2) for j in range(4)])
    k.panel(P, "leaf")
    banana_hand(k, (-0.22, -0.02, 0.06), 6, True, 0.3)
    banana_hand(k, (0.2, 0.05, 0.06), 5, True, 2.6)
    banana_hand(k, (0.0, -0.15, 0.2), 5, False, 1.2)
    return k.finish()


def pots():
    k = Kit("market_pots")
    def pot(at, s, sw):
        prof = [(0.10 * s, 0.0), (0.20 * s, 0.07 * s), (0.25 * s, 0.20 * s), (0.20 * s, 0.34 * s), (0.13 * s, 0.40 * s), (0.15 * s, 0.45 * s)]
        V = k.lathe(prof, at, 10, sw, bottom=sw)
        k.lathe([(0.15 * s, 0.45 * s), (0.11 * s, 0.43 * s), (0.08 * s, 0.30 * s)], at, 10, "clay_dark", top="clay_dark")
    pot((0, 0, 0), 1.25, "terracotta")
    pot((0.42, 0.12, 0), 0.95, "terracotta")
    pot((-0.36, 0.18, 0), 0.85, "clay_dark")
    pot((0.12, -0.38, 0), 0.75, "terracotta")
    return k.finish()


def baskets():
    k = Kit("market_baskets")
    def basket(at, r, h, fill, n_nuts=0):
        k.lathe([(r * 0.75, 0.0), (r, h * 0.55), (r * 1.06, h)], at, 10, "basket", bottom="basket")
        k.lathe([(r * 1.06, h), (r * 0.98, h - 0.03)], at, 10, "rope")
        x, y, _ = at
        k.blob((x, y, h - 0.04), (r * 0.97, r * 0.97, 0.13), fill, (10, 4))
        rnd = random.Random(int(r * 100))
        for i in range(n_nuts):
            a = rnd.uniform(0, 6.28); d = rnd.uniform(0, r * 0.55)
            k.blob((x + math.cos(a) * d, y + math.sin(a) * d, h + 0.08), (0.05, 0.05, 0.045), fill, (6, 4))
    basket((0, 0, 0), 0.30, 0.28, "areca", 4)
    basket((0.62, 0.05, 0), 0.26, 0.24, "betel")
    basket((0.28, 0.5, 0), 0.28, 0.26, "tuber", 3)
    return k.finish()


def cloth_stack():
    k = Kit("market_cloth_stack")
    sws = ["cloth_red", "cloth_cream", "cloth_indigo", "cloth_ochre", "cloth_red"]
    z = 0.0
    for i, sw in enumerate(sws):
        h = 0.075
        k.box((0.02 * math.sin(i * 2.1), 0.02 * math.cos(i * 1.3), z + h / 2), (0.62, 0.42, h), sw, rz=0.08 * math.sin(i * 1.9), chamfer=0.03)
        z += h
    k.box((0.75, 0.05, 0.04), (0.6, 0.42, 0.08), "cloth_indigo", rz=0.3, chamfer=0.03)
    return k.finish()


def mat():
    k = Kit("market_mat")
    k.box((0, 0, 0.012), (2.0, 1.3, 0.024), "mat", chamfer=0.008)
    return k.finish()


def stool():
    k = Kit("market_stool")
    k.lathe([(0.17, 0.0), (0.2, 0.05), (0.19, 0.3), (0.2, 0.36)], (0, 0, 0), 8, "wood", top="wood_light")
    return k.finish()


def bench():
    k = Kit("market_bench")
    for y in (-0.1, 0.0, 0.1):
        k.tube([(-0.9, y, 0.4), (0.9, y, 0.4)], 0.05, 6, "bamboo")
    for x in (-0.75, 0.75):
        for y in (-0.12, 0.12):
            k.tube([(x, y, 0), (x, y * 0.6, 0.38)], 0.035, 5, "wood_light")
        k.tube([(x, -0.16, 0.36), (x, 0.16, 0.36)], 0.03, 5, "wood")
    return k.finish()


# ==================================================================================================== the gallery
def gallery(path):
    """Stone viewing steps in the court's stone (plaza_kit's helpers, court atlas): a 0.30 front tier and a 0.60
    back tier, each 0.9 deep, 6 m long, the woven band round the back tier's top edge. Front is +y."""
    import court_build as C
    import plaza_kit as P
    mb = C.MB("market_gallery")
    hw = 3.0
    for (y0, y1, z1, s0) in ((0.0, 0.9, 0.30, 0.0), (-0.9, 0.0, 0.60, 13.0)):
        mb.new_island()
        ring = [(hw, y0), (hw, y1), (-hw, y1), (-hw, y0)]                   # CCW from above
        s = s0
        for i in range(4):
            a, b = ring[i], ring[(i + 1) % 4]
            if z1 == 0.30 and i == 3:
                pass
            P.side(mb, a, b, -0.3, z1 - (0.12 if z1 > 0.4 else 0.0), z1 + 0.6, s, P.foot_dark(-0.3, z1, 0.86, 1.02), plain_rows=True)
            s += math.dist(a, b) + 1.3
        if z1 > 0.4:
            P.row_sides(mb, 0, z1 - 0.12, z1, "cap_out", P.lit(1.03), ring=ring)
        # the top in paving
        q = [(-hw, y0, z1), (hw, y0, z1), (hw, y1, z1), (-hw, y1, z1)]
        mb.face(q, [C.pave_uv(p[0] + 7.0 * (z1 > 0.4), p[1] + 3.0, 0) for p in q], [P.lit(1.04)(0)] * 4)
    P.export_stone([mb], "market_gallery", path)


def main():
    os.makedirs(os.path.join(OUT, "build"), exist_ok=True)
    made = {}
    for make in (stall_thatch, stall_mat, cloth_line, rice, bananas, pots, baskets, cloth_stack, mat, stool, bench):
        bpy.ops.wm.read_factory_settings(use_empty=True)
        o = make()
        made[o.name] = export(o, os.path.join(OUT, "build", o.name + "_raw.glb"))
    bpy.ops.wm.read_factory_settings(use_empty=True)
    gallery(os.path.join(OUT, "build", "market_gallery_raw.glb"))
    json.dump(made, open(os.path.join(OUT, "build", "tris.json"), "w"), indent=1)


if __name__ == "__main__":
    main()
