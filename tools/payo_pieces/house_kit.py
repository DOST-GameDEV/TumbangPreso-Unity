"""Payo town buildings: the Blender half of the kit (geometry, UV layout). Blender 5.0, headless.

A building is a list of closed PARTS (lofts of rings: battered blocks, flared thatch, posts, beams). Every part is
bevelled, then unwrapped by a box projection whose pixels map straight back to world metres, so the painter
(tools/payo_pieces/house_paint.py, system Python) paints each stone, plank and thatch layer where it really is, and
the pale bevel lines are drawn along the mesh's own sharp edges. Nothing here tiles.

  1. blender -b --python tools/payo_pieces/house_<name>.py          parts, layout.json, a .blend
  2. python tools/payo_pieces/house_paint.py <name>                 the atlas
  3. blender -b --python tools/payo_pieces/house_finish.py -- <name> the glb and four review pictures
  4. python tools/payo_pieces/house_paint.py sheet <name> <vN>      the review sheet
(tools/payo_pieces/house_run.sh <name> <vN> runs all four.)

Blender axes while building: x right, y back (the FRONT faces -y), z up, metres, origin on the ground at the centre.
"""
import bpy, bmesh, json, math, os, random
from mathutils import Vector, Matrix, Euler

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
WORK = os.path.join(ROOT, "Logs", "payo", "pieces", "house", "work")
PAD = 5


def ring(cx, cy, z, w, d, n=4, rot=0.0, tilt=(0.0, 0.0), power=2.0, phase=0.5):
    """One horizontal outline, counter clockwise from above. n=4 is a rectangle; more is a (super)ellipse."""
    pts = []
    if n == 4:
        base = [(-w / 2, -d / 2), (w / 2, -d / 2), (w / 2, d / 2), (-w / 2, d / 2)]
    else:
        base = []
        for i in range(n):
            a = 2 * math.pi * (i + phase) / n - math.pi / 2
            c, s = math.cos(a), math.sin(a)
            base.append((math.copysign(abs(c) ** (2 / power), c) * w / 2, math.copysign(abs(s) ** (2 / power), s) * d / 2))
    cr, sr = math.cos(math.radians(rot)), math.sin(math.radians(rot))
    for x, y in base:
        pts.append((cx + x * cr - y * sr, cy + x * sr + y * cr, z + tilt[0] * x + tilt[1] * y))
    return pts


def loft(rings):
    n = len(rings[0])
    verts = [p for r in rings for p in r]
    faces = [list(range(n))[::-1]]
    for i in range(len(rings) - 1):
        a, b = i * n, (i + 1) * n
        for j in range(n):
            k = (j + 1) % n
            faces.append([a + j, a + k, b + k, b + j])
    faces.append([(len(rings) - 1) * n + j for j in range(n)])
    return verts, faces


def frame(p0, p1, up=(0, 0, 1)):
    p0, p1 = Vector(p0), Vector(p1)
    ax = (p1 - p0).normalized()
    u = Vector(up)
    if abs(ax.dot(u)) > 0.95:
        u = Vector((1, 0, 0))
    side = ax.cross(u).normalized()
    upv = side.cross(ax).normalized()
    return p0, p1, side, upv


class Kit:
    def __init__(self, name, tex=2048, seed=1):
        self.name, self.tex, self.parts = name, tex, []
        self.deform = None
        self.meta = {}
        self.rng = random.Random(seed)

    # ---------------------------------------------------------------- shapes
    def add(self, kind, verts, faces, bevel=0.0, glow=False, dens=1.0, **params):
        p = dict(kind=kind, verts=[Vector(v) for v in verts], faces=faces, bevel=bevel, glow=glow, dens=dens, params=params)
        self.parts.append(p)
        return p

    def loft(self, kind, rings, **kw):
        v, f = loft(rings)
        return self.add(kind, v, f, **kw)

    def box(self, kind, c, size, rz=0.0, rx=0.0, ry=0.0, top=(1.0, 1.0), shift=(0.0, 0.0), **kw):
        """A block centred at c. top scales the upper face (taper), shift slides it (lean)."""
        w, d, h = size
        v, f = loft([ring(0, 0, -h / 2, w, d), ring(shift[0], shift[1], h / 2, w * top[0], d * top[1])])
        m = Euler((math.radians(rx), math.radians(ry), math.radians(rz))).to_matrix()
        c = Vector(c)
        return self.add(kind, [m @ Vector(p) + c for p in v], f, **kw)

    def cyl(self, kind, p0, p1, r0, r1=None, n=8, mid=None, **kw):
        """A round bar from p0 to p1. mid=(t, r) adds a swelling ring."""
        r1 = r0 if r1 is None else r1
        p0, p1, side, upv = frame(p0, p1)
        stations = [(0.0, r0)] + ([mid] if mid else []) + [(1.0, r1)]
        if mid and isinstance(mid, list):
            stations = [(0.0, r0)] + mid + [(1.0, r1)]
        rings = []
        for t, r in stations:
            c = p0.lerp(p1, t)
            rings.append([tuple(c + side * (math.cos(2 * math.pi * i / n) * r) + upv * (math.sin(2 * math.pi * i / n) * r)) for i in range(n)])
        return self.loft(kind, rings, **kw)

    def beam(self, kind, p0, p1, w, h, up=(0, 0, 1), w1=None, h1=None, **kw):
        p0, p1, side, upv = frame(p0, p1, up)
        w1 = w if w1 is None else w1
        h1 = h if h1 is None else h1
        rings = []
        for c, ww, hh in ((p0, w, h), (p1, w1, h1)):
            rings.append([tuple(c + side * (sx * ww / 2) + upv * (sy * hh / 2)) for sx, sy in ((-1, -1), (1, -1), (1, 1), (-1, 1))])
        return self.loft(kind, rings, **kw)

    def leaf(self, base, tip, width, droop=0.0, **kw):
        """A big thick leaf: narrow at the stalk, wide in the middle, a drooping tip, creased down the midrib."""
        b, t = Vector(base), Vector(tip)
        ax = t - b
        side = ax.cross(Vector((0, 0, 1)))
        if side.length < 1e-4:
            side = Vector((1, 0, 0))
        side.normalize()
        up = side.cross(ax).normalized()
        rings = []
        for s, wf in ((0.0, 0.10), (0.25, 0.75), (0.55, 1.0), (0.82, 0.62), (1.0, 0.06)):
            c = b + ax * s + up * (ax.length * 0.16 * math.sin(s * 2.6)) - Vector((0, 0, droop * s * s))
            w = width * wf / 2
            rings.append([tuple(c - side * w + up * w * 0.35), tuple(c - up * 0.05), tuple(c + side * w + up * w * 0.35), tuple(c + up * 0.03)])
        kw.setdefault("col", [0.30, 0.62, 0.36])
        kw.setdefault("dens", 0.8)
        return self.loft("leaf", rings, **kw)

    def plant(self, at, n=6, size=1.0, seed=0, **kw):
        r = random.Random(seed)
        at = Vector(at)
        for i in range(n):
            a = 2 * math.pi * (i + r.random() * 0.6) / n
            L = size * (0.7 + r.random() * 0.6)
            rise = L * (0.35 + r.random() * 0.5)
            tip = at + Vector((math.cos(a) * L * 0.8, math.sin(a) * L * 0.8, rise))
            g = 0.52 + r.random() * 0.2
            self.leaf(at, tip, L * 0.55, droop=L * 0.3, col=[0.22 + r.random() * 0.12, g, 0.30 + r.random() * 0.14], **kw)

    def vine(self, pts, leaves=6, size=0.35, seed=0):
        r = random.Random(seed)
        pts = [Vector(p) for p in pts]
        for a, b in zip(pts, pts[1:]):
            self.cyl("flat", a, b, 0.035, 0.03, n=4, col=[0.22, 0.42, 0.26], dens=0.5)
        for i in range(leaves):
            t = (i + r.random()) / leaves * (len(pts) - 1)
            k = min(int(t), len(pts) - 2)
            p = pts[k].lerp(pts[k + 1], t - k)
            a = r.random() * 6.28
            tip = p + Vector((math.cos(a) * size, math.sin(a) * size * 0.6 - size * 0.3, -size * (0.5 + r.random() * 0.5)))
            self.leaf(p, tip, size * 0.6, col=[0.26, 0.56 + r.random() * 0.14, 0.34], dens=0.7)

    def rope(self, p0, p1, sag=0.3, n=5, r=0.025, col=(0.36, 0.27, 0.18)):
        p0, p1 = Vector(p0), Vector(p1)
        pts = []
        for i in range(n + 1):
            t = i / n
            p = p0.lerp(p1, t)
            p.z -= sag * 4 * t * (1 - t)
            pts.append(p)
        for a, b in zip(pts, pts[1:]):
            self.cyl("flat", a, b, r, r, n=4, col=list(col), dens=0.4)
        return pts

    # ---------------------------------------------------------------- unwrap
    def _bm(self, p):
        bm = bmesh.new()
        vs = [bm.verts.new(v) for v in p["verts"]]
        for f in p["faces"]:
            try:
                bm.faces.new([vs[i] for i in f])
            except ValueError:
                pass
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        if p["bevel"] > 0:
            es = [e for e in bm.edges if len(e.link_faces) == 2 and e.calc_face_angle(0) > math.radians(30)]
            if es:
                bmesh.ops.bevel(bm, geom=es, offset=p["bevel"], offset_type="OFFSET", segments=1, profile=0.5, affect="EDGES", clamp_overlap=True)
                bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        bm.normal_update()
        return bm

    @staticmethod
    def _dir(n, thr=0.82):
        if abs(n.z) > thr:
            return "z+" if n.z > 0 else "z-"
        if abs(n.x) > abs(n.y):
            return "x+" if n.x > 0 else "x-"
        return "y+" if n.y > 0 else "y-"

    @staticmethod
    def _uv(d, co):
        """Metres across and up, as someone facing that side sees it."""
        if d == "y-": return co.x, co.z
        if d == "y+": return -co.x, co.z
        if d == "x+": return co.y, co.z
        if d == "x-": return -co.y, co.z
        if d == "z+": return co.x, co.y
        return co.x, -co.y

    def build(self):
        os.makedirs(WORK, exist_ok=True)
        groups, bms = [], []
        for pi, p in enumerate(self.parts):
            bm = self._bm(p)
            bms.append(bm)
            lo = Vector((1e9,) * 3); hi = Vector((-1e9,) * 3)
            for v in bm.verts:
                lo = Vector(map(min, lo, v.co)); hi = Vector(map(max, hi, v.co))
            p["lo"], p["hi"] = lo, hi
            by = {}
            thatch = p["kind"] == "thatch"
            for f in bm.faces:
                by.setdefault(self._dir(f.normal, 0.95 if thatch else 0.82), []).append(f)
            p["groups"] = {}
            vs0 = p["params"].get("vscale", 1.0)
            for d, fs in by.items():
                vs = vs0
                if thatch and d[0] != "z":                        # a shallow thatch slope keeps its pixels
                    hz = sum(math.sqrt(max(1 - f.normal.z ** 2, 0.01)) * f.calc_area() for f in fs) / max(sum(f.calc_area() for f in fs), 1e-6)
                    vs = vs0 * min(3.0, 1.0 / max(hz, 0.3))
                us = [self._uv(d, v.co) for f in fs for v in f.verts]
                u0, u1 = min(u for u, _ in us), max(u for u, _ in us)
                v0, v1 = min(v for _, v in us), max(v for _, v in us)
                depth = sum((f.calc_center_median().y if d[0] == "y" else f.calc_center_median().x if d[0] == "x" else f.calc_center_median().z) for f in fs) / len(fs)
                dens = p["dens"] * (0.45 if d == "z-" else 1.0) * (0.3 if d in p["params"].get("hide", ()) else 1.0)
                g = dict(part=pi, dir=d, u0=u0, v1=v1, wm=max(u1 - u0, 0.02), hm=max(v1 - v0, 0.02), dens=dens, vs=vs if d[0] != "z" else 1.0, depth=depth, faces=fs)
                p["groups"][d] = g
                groups.append(g)

        def pack(D):
            x = y = row = 0
            for g in order:
                g["pw"] = int(math.ceil(g["wm"] * D * g["dens"])) + 2 * PAD
                g["ph"] = int(math.ceil(g["hm"] * D * g["dens"] * g["vs"])) + 2 * PAD
            for g in sorted(order, key=lambda g: -g["ph"]):
                if g["pw"] > self.tex:
                    return False
                if x + g["pw"] > self.tex:
                    x, y, row = 0, y + row, 0
                g["px"], g["py"] = x, y
                x += g["pw"]; row = max(row, g["ph"])
                if y + row > self.tex:
                    return False
            return True

        order = groups
        a, b = 4.0, 600.0
        for _ in range(28):
            m = (a + b) / 2
            if pack(m): a = m
            else: b = m
        pack(a)
        D = a
        T = self.tex

        def px(g, co):
            u, v = self._uv(g["dir"], co)
            return (g["px"] + PAD + (u - g["u0"]) * D * g["dens"], g["py"] + PAD + (g["v1"] - v) * D * g["dens"] * g["vs"])

        # the layout the painter reads
        out_parts, edges = [], []
        allbm = bmesh.new()
        uvl = allbm.loops.layers.uv.new("UVMap")
        for pi, (p, bm) in enumerate(zip(self.parts, bms)):
            face_g = {}
            for g in p["groups"].values():
                for f in g["faces"]:
                    face_g[f.index] = g
            bm.faces.index_update()
            face_g = {}
            for g in p["groups"].values():
                for f in g["faces"]:
                    face_g[f] = g
            for e in bm.edges:
                if len(e.link_faces) != 2:
                    continue
                ang = e.calc_face_angle(0)
                if ang < math.radians(22):
                    continue
                kind = 1 if e.is_convex else 0
                up = (e.link_faces[0].normal.z + e.link_faces[1].normal.z) * 0.5
                for f in e.link_faces:
                    g = face_g[f]
                    a0, a1 = px(g, e.verts[0].co), px(g, e.verts[1].co)
                    edges.append([pi, round(a0[0], 1), round(a0[1], 1), round(a1[0], 1), round(a1[1], 1), kind, round(up, 2)])
            vmap = {}
            for v in bm.verts:
                co = v.co.copy()
                if self.deform:
                    co = Vector(self.deform(co))
                vmap[v] = allbm.verts.new(co)
            for f in bm.faces:
                g = face_g[f]
                nf = allbm.faces.new([vmap[v] for v in f.verts])
                nf.material_index = 1 if p["glow"] else 0
                nf.smooth = True
                for lp, v in zip(nf.loops, f.verts):
                    x, y = px(g, v.co)
                    lp[uvl].uv = (x / T, 1 - y / T)
            out_parts.append(dict(kind=p["kind"], params=p["params"], lo=list(p["lo"]), hi=list(p["hi"]), glow=p["glow"],
                                  groups=[dict(dir=g["dir"], px=g["px"], py=g["py"], pw=g["pw"], ph=g["ph"], u0=g["u0"], v1=g["v1"],
                                               ppu=D * g["dens"], ppv=D * g["dens"] * g["vs"], depth=g["depth"]) for g in p["groups"].values()]))
        bpy.ops.wm.read_factory_settings(use_empty=True)
        me = bpy.data.meshes.new(self.name)
        allbm.normal_update()
        allbm.to_mesh(me)
        tris = sum(len(f.verts) - 2 for f in allbm.faces)
        ob = bpy.data.objects.new(self.name, me)
        bpy.context.scene.collection.objects.link(ob)
        me.materials.append(bpy.data.materials.new(self.name + "_main"))
        me.materials.append(bpy.data.materials.new(self.name + "_glow"))
        me.set_sharp_from_angle(angle=math.radians(40))
        lo = [min(v.co[i] for v in me.vertices) for i in range(3)]
        hi = [max(v.co[i] for v in me.vertices) for i in range(3)]
        self.meta.update(tex=T, ppm=round(D, 1), tris=tris, lo=lo, hi=hi)
        json.dump(dict(name=self.name, tex=T, pad=PAD, ppm=D, parts=out_parts, edges=edges, meta=self.meta), open(os.path.join(WORK, self.name + ".json"), "w"))
        bpy.ops.wm.save_as_mainfile(filepath=os.path.join(WORK, self.name + ".blend"))
        print("BUILT %s: %d parts, %d triangles, %.1f px per metre, size %.2f x %.2f x %.2f (z %.2f to %.2f)" % (
            self.name, len(self.parts), tris, D, hi[0] - lo[0], hi[1] - lo[1], hi[2] - lo[2], lo[2], hi[2]))


# -------------------------------------------------------------------- building bits shared by the six houses
DIRS = {"y-": (Vector((0, -1, 0)), Vector((1, 0, 0))), "y+": (Vector((0, 1, 0)), Vector((-1, 0, 0))),
        "x+": (Vector((1, 0, 0)), Vector((0, 1, 0))), "x-": (Vector((-1, 0, 0)), Vector((0, -1, 0)))}
PALESTONE = [[0.67, 0.64, 0.58], [0.58, 0.56, 0.52], [0.62, 0.58, 0.52], [0.70, 0.68, 0.63]]
DARKWOOD = [0.36, 0.24, 0.17]


def others(d):
    return [x for x in ("x+", "x-", "y+", "y-", "z+", "z-") if x != d]


def obox(k, kind, at, d, w, t, h, out=0.0, side=0.0, **kw):
    """A block on a wall: w across, t out of the wall, h up, centred at `at` moved `out` outward and `side` along."""
    o, r = DIRS[d]
    c = Vector(at) + o * out + r * side
    return k.box(kind, c, (w, t, h) if d[0] == "y" else (t, w, h), **kw)


class Wall:
    """A battered storey: where its faces are at a height."""
    def __init__(self, cx, cy, z0, z1, w0, d0, w1, d1, sx=0.0, sy=0.0):
        self.__dict__.update(cx=cx, cy=cy, z0=z0, z1=z1, w0=w0, d0=d0, w1=w1, d1=d1, sx=sx, sy=sy)

    def rings(self):
        return [ring(self.cx, self.cy, self.z0, self.w0, self.d0), ring(self.cx + self.sx, self.cy + self.sy, self.z1, self.w1, self.d1)]

    def at(self, d, z, side=0.0):
        """A point on face d at height z, `side` metres along the face from its middle (to the viewer's right)."""
        t = (z - self.z0) / (self.z1 - self.z0)
        cx, cy = self.cx + self.sx * t, self.cy + self.sy * t
        w, dd = self.w0 + (self.w1 - self.w0) * t, self.d0 + (self.d1 - self.d0) * t
        o, r = DIRS[d]
        half = dd / 2 if d[0] == "y" else w / 2
        return Vector((cx, cy, z)) + o * half + r * side

    def half(self, d, z):
        t = (z - self.z0) / (self.z1 - self.z0)
        return (self.w0 + (self.w1 - self.w0) * t) / 2 if d[0] == "y" else (self.d0 + (self.d1 - self.d0) * t) / 2


def window(k, at, d, w, h, lattice="grid", stone=None, paint=None, sill=True, hood=None, **gk):
    stone = stone or PALESTONE
    at = Vector(at)
    obox(k, "glow", at, d, w, 0.18, h, out=0.03, glow=True, lattice=lattice, hide=others(d), **gk)
    kw = dict(base=stone, bevel=0.05, moss=0.25, foot=False)
    if paint:
        kw["paint"] = paint
    obox(k, "block", at + Vector((0, 0, h / 2 + 0.15)), d, w + 0.62, 0.50, 0.32, out=0.07, top=(0.92, 0.92), seed=1, **kw)
    if sill:
        obox(k, "block", at - Vector((0, 0, h / 2 + 0.10)), d, w + 0.46, 0.56, 0.22, out=0.10, seed=2, **kw)
    for s in (-1, 1):
        obox(k, "block", at, d, 0.24, 0.40, h + 0.02, out=0.05, side=s * (w / 2 + 0.10), seed=3 + s, **kw)
    if hood:
        o, r = DIRS[d]
        c = at + Vector((0, 0, h / 2 + 0.55)) + o * 0.42
        k.box("thatch", c, ((w + 1.0), 0.95, 0.16) if d[0] == "y" else (0.95, (w + 1.0), 0.16),
              rx=(-28 if d == "y-" else 28 if d == "y+" else 0), ry=(28 if d == "x+" else -28 if d == "x-" else 0), col=hood, layer=0.3, dens=0.8)


def doorway(k, at, d, w, h, col, stone=None, decals=None, paint=None, **dk):
    """`at` is the middle of the threshold on the wall face."""
    stone = stone or PALESTONE
    at = Vector(at)
    mid = at + Vector((0, 0, h / 2))
    dd = [dict(x, dir=d) for x in (decals or [])]
    obox(k, "door", mid, d, w, 0.16, h, out=0.04, col=col, face=d, decals=dd, hide=others(d), dens=1.5, bevel=0.0, **dk)
    kw = dict(base=stone, bevel=0.07, moss=0.3, foot=False)
    if paint:
        kw["paint"] = paint
    obox(k, "block", at + Vector((0, 0, h + 0.22)), d, w + 1.0, 0.62, 0.46, out=0.10, top=(0.90, 0.9), seed=1, **kw)
    for s in (-1, 1):
        obox(k, "block", mid - Vector((0, 0, 0.0)), d, 0.38, 0.52, h + 0.04, out=0.08, side=s * (w / 2 + 0.16), top=(0.86, 0.9), seed=3 + s, **kw)
    obox(k, "block", at + Vector((0, 0, 0.02)), d, w + 0.9, 0.80, 0.22, out=0.22, seed=6, **kw)


def quoins(k, wall, corners=((1, -1), (-1, -1)), n=6, base=None, proud=0.07, seed=0, **kw):
    kw.setdefault("dens", 0.6)
    """Long and short corner stones standing proud of a battered wall."""
    r = random.Random(seed)
    h = (wall.z1 - wall.z0) / n
    for sx, sy in corners:
        for i in range(n):
            z = wall.z0 + (i + 0.5) * h
            t = (z - wall.z0) / (wall.z1 - wall.z0)
            cx = wall.cx + wall.sx * t + sx * (wall.w0 + (wall.w1 - wall.w0) * t) / 2
            cy = wall.cy + wall.sy * t + sy * (wall.d0 + (wall.d1 - wall.d0) * t) / 2
            long_x = (i % 2 == 0)
            lx, ly = (1.25, 0.62) if long_x else (0.62, 1.25)
            lx *= 0.85 + r.random() * 0.3; ly *= 0.85 + r.random() * 0.3
            k.box("block", (cx - sx * (lx / 2 - proud), cy - sy * (ly / 2 - proud), z), (lx, ly, h * 0.92), bevel=0.06,
                  base=base or PALESTONE, seed=i * 3 + int(sx * 2 + sy), moss=0.35, foot=False, hide=["z+", "z-"], rz=(r.random() - 0.5) * 3, **kw)


def proud_stones(k, wall, d, n=5, seed=0, base=None, zr=(0.1, 0.9), **kw):
    """A few stones that have worked forward out of the face."""
    r = random.Random(seed)
    for i in range(n):
        z = wall.z0 + (wall.z1 - wall.z0) * (zr[0] + (zr[1] - zr[0]) * r.random())
        s = (r.random() - 0.5) * 2 * (wall.half(d, z) - 0.9)
        w, h = 0.6 + r.random() * 0.5, 0.36 + r.random() * 0.2
        obox(k, "block", wall.at(d, z, s), d, w, 0.34, h, out=-0.06 + r.random() * 0.05, bevel=0.06, base=base or [[0.60, 0.58, 0.54], [0.5, 0.48, 0.45]],
             seed=seed * 7 + i, moss=0.4, foot=False, hide=["z-"], **kw)


def jar(k, at, r=0.34, h=0.8, col=(0.70, 0.40, 0.26), n=10):
    x, y, z = at
    prof = [(0.0, 0.55), (0.12, 0.85), (0.45, 1.0), (0.75, 0.80), (0.90, 0.50), (1.0, 0.62)]
    rings = [[(x + math.cos(2 * math.pi * i / n) * r * s, y + math.sin(2 * math.pi * i / n) * r * s, z + t * h) for i in range(n)] for t, s in prof]
    k.loft("flat", rings, col=list(col), scale=2.5, dens=0.8, edge=0.0)


def banner(k, at, h=6.0, col=(0.80, 0.24, 0.20), lean=(0.3, 0.1), w=0.75, drop=2.6, d="y-", seed=0, stripes=None):
    """A leaning bamboo pole with a long cloth hung from a yard near its top."""
    b = Vector(at); t = b + Vector((lean[0], lean[1], h))
    k.cyl("wood", b - Vector((0, 0, 1.0)), t, 0.09, 0.06, n=6, col=[0.62, 0.52, 0.28], dens=0.5,
          bands=[dict(z0=z, z1=z + 0.07, col=[0.36, 0.30, 0.16]) for z in (b.z + 1.0, b.z + 2.1, b.z + 3.3, b.z + 4.4)])
    o, r = DIRS[d]
    y0 = b.lerp(t, 0.93)
    k.beam("wood", y0 - r * 0.15, y0 + r * (w + 0.25), 0.07, 0.07, col=DARKWOOD, dens=0.5)
    c = y0 + r * (w / 2 + 0.08) - Vector((0, 0, drop / 2 + 0.02))
    k.box("cloth", c, (w, 0.05, drop) if d[0] == "y" else (0.05, w, drop), col=list(col), seed=seed, ry=2 if d[0] == "y" else 0,
          stripes=stripes or [[0.04, 0.09, [0.95, 0.90, 0.78]], [0.12, 0.16, [0.14, 0.14, 0.18]], [0.80, 0.86, [0.95, 0.76, 0.24]], [0.90, 1.0, [0.14, 0.14, 0.18]]])


def lantern(k, at, s=1.0, col=(0.98, 0.60, 0.20)):
    x, y, z = at
    k.box("wood", (x, y, z + 0.34 * s), (0.50 * s, 0.50 * s, 0.14 * s), top=(0.35, 0.35), col=[0.55, 0.20, 0.16], bevel=0.02, dens=0.8)
    k.box("glow", (x, y, z), (0.38 * s, 0.38 * s, 0.56 * s), glow=True, lattice="grid", nx=2, nz=2, col=list(col), top=(1.12, 1.12))
    k.box("wood", (x, y, z - 0.33 * s), (0.46 * s, 0.46 * s, 0.10 * s), col=[0.55, 0.20, 0.16], bevel=0.02, dens=0.8)
    k.cyl("flat", (x, y, z - 0.38 * s), (x, y, z - 0.75 * s), 0.05 * s, 0.015, n=4, col=[0.80, 0.24, 0.20], dens=0.5)
