"""The arena's CITY kit: everything outside the stadium's hull (ARENA-1.4).

    py -3 tools/author_arena_textures_city.py            (the textures, first)
    blender -b --python tools/author_arena_city.py -- --version=vN [--shots=eye,air,...] [--no-render]

Builds ArtSource/arena/kits/city.blend (collection `arena_city`), tools/arena_traffic.json and the
review pictures Logs/arena/city/city_<shot>_vN.png. Read docs/ARENA_ART_BRIEF.md first.

Owner, 2026-10-05, of this kit's blockout: "buildings will need actual more detail", "the buildings
arent really visible from the stadium. i need you to think more about sightlines", "i dont want any
buildings to look like that in the final design" (procedural window grids), "theres a few stretched
out textures on the buildings", "need you to be more critical of the work".

WHAT IS HERE
  1. TOWERS, placed by sightline (`TOWERS`): four gates of three at the open corners and eleven
     towers over the stands in two depths. 23 towers of 11 designs that differ in construction
     (the first skyline was 18, further out and lower: see the note over `TOWERS`):
       haligi     square, modelled piers on every face, three setbacks, a stepped crown
       tirahan    a cross-plan condominium, balcony slabs all the way up, tanks on the roof
       magkapatid a PAIR of slabs with lift spines, joined by two skybridges     (landmark, SE)
       bilog      round, ring balconies, three drums, a dome and a needle
       singsing   an octagonal shaft carrying a GREAT RING between two prongs      (landmark, NE)
       korona     a hexagon with a crown of six blades round a HOLOGRAM             (landmark, SW)
       parola     a needle carrying two saucers and a lantern                       (landmark, NW)
       patong     stacked boxes, each shifted off the one below, dark necks between
       talim      a tapering blade with a slanted top and a tall fin
       hagdan     terraces stepping back up the side that faces the stadium, a service spine
       balangkas  glass modules inside an exoskeleton of columns, ring beams and diagonals
     Each tower is ONE closed mesh (`_body`) plus its attached parts (`_parts`): the body is a
     stack of segments, every setback, ledge, slab and pier a face of the same surface, joined
     where the plan changes by a filled ledge. Nothing is a stack of boxes.
  2. UVs. Every face is unwrapped BY FACE at true scale (`Mesh._uv`): u runs along the face's
     horizontal, v up its slope, both in metres over the material's tile, so a tapered or leaning
     face cannot stretch. Window rows line up round a tower because v is the world's height.
  3. SIGNS (`sign`): nine, each a backing box on four brackets that end inside the tower, a frame,
     and a face slab 5 cm proud of the backing. Names are hand-given in the texture author.
  4. THE DEPTHS: the city floor 760 m down, 170 low blocks with painted roofs, fourteen cheap far
     towers, an elevated rail loop and a viaduct with a train, four skybridges low in the gates,
     three layers of haze. THE SKY: a painted panorama (review dome only; Unity draws a skybox).
     SKY TRAFFIC: three craft (kotse, dyip, barge) on four lanes, written to arena_traffic.json.
  5. The SIGHT report: from the player's eye by the can, how many degrees of each tower show above
     the stadium, and the skyline's rhythm round the compass. Printed on every build.

Blender units are metres, z up, y north, the can is the origin. A bearing is degrees clockwise from
north. Unity (glTFast) puts a Blender point (x, y, z) at (-x, z, -y).
"""
import bpy, bmesh, json, math, os, random, sys
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import arena_kit as K                      # noqa: E402  the stadium's numbers, polar(), rim_elevation()
import author_arena_textures_city as T     # noqa: E402  TILE_M, SIGNS, FX, TRIM (constants only)

ROOT = K.ROOT
KITS = os.path.join(ROOT, "ArtSource", "arena", "kits")
LOGS = os.path.join(ROOT, "Logs", "arena", "city")
TEX = str(T.OUT)
FOOT = -760.0                              # the city floor (the brief's table; arena_kit.py does not carry it)
FLOOR = T.FLOOR_M
BAY = T.BAY_M
LOBBY = -30 * FLOOR                        # -160: every tower's sky lobby, where the hazed lower shaft ends
EYE = K.EYE
Z = Vector((0, 0, 1))
CHECKER = False

EMIT = {"glass_a": 0.8, "glass_b": 0.8, "resi": 0.8, "bands": 0.8, "far": 0.7, "base": 0.9, "roofs": 0.9,
        "metal": 1.0, "floor": 1.0, "trim": 1.25, "signs": 0.8, "fx": 1.1, "haze": 1.0}
FLAT = {"glass_a": (0.11, 0.13, 0.27), "glass_b": (0.10, 0.2, 0.24), "resi": (0.23, 0.2, 0.33), "bands": (0.13, 0.15, 0.3),
        "far": (0.17, 0.19, 0.4), "base": (0.15, 0.15, 0.4), "metal": (0.15, 0.18, 0.28), "roofs": (0.12, 0.14, 0.26),
        "floor": (0.1, 0.1, 0.25), "trim": (0.4, 0.8, 0.9), "signs": (0.6, 0.4, 0.5), "fx": (0.4, 0.9, 1.0),
        "haze": (0.2, 0.18, 0.45)}
_mats = {}


def snap(z):
    return round(z / FLOOR) * FLOOR


def material(kind):
    """arena_city_<kind>: the painted albedo, and its emission image where it glows."""
    if kind in _mats:
        return _mats[kind]
    m = bpy.data.materials.new("arena_city_%s" % kind)
    m.use_nodes = True
    m.diffuse_color = (*FLAT[kind], 1.0)
    nt = m.node_tree
    b = nt.nodes.get("Principled BSDF")
    b.inputs["Roughness"].default_value = 0.85
    tiled = kind in T.TILE_M and kind != "haze"
    if CHECKER and not tiled:
        b.inputs["Base Color"].default_value = (0.2, 0.2, 0.22, 1)
        b.inputs["Emission Color"].default_value = (0.2, 0.2, 0.22, 1)
        b.inputs["Emission Strength"].default_value = 0.25
        if kind in ("fx", "haze"):
            b.inputs["Alpha"].default_value = 0.0
            m.surface_render_method = "BLENDED"
    else:
        path = os.path.join(LOGS, "checker.png") if CHECKER else os.path.join(TEX, "arena_city_%s.png" % kind)
        tex = nt.nodes.new("ShaderNodeTexImage")
        tex.image = bpy.data.images.load(path, check_existing=True)
        nt.links.new(tex.outputs["Color"], b.inputs["Base Color"])
        if CHECKER:
            nt.links.new(tex.outputs["Color"], b.inputs["Emission Color"])
            b.inputs["Emission Strength"].default_value = 0.55
        elif kind in EMIT:
            e = nt.nodes.new("ShaderNodeTexImage")
            e.image = bpy.data.images.load(os.path.join(TEX, "arena_city_%s_emit.png" % kind), check_existing=True)
            nt.links.new(e.outputs["Color"], b.inputs["Emission Color"])
            b.inputs["Emission Strength"].default_value = EMIT[kind]
        if kind in ("fx", "haze") and not CHECKER:
            nt.links.new(tex.outputs["Alpha"], b.inputs["Alpha"])
            m.surface_render_method = "BLENDED"
    _mats[kind] = m
    return m


# ---------------------------------------------------------------- plans (local x right, y away from the can)
def rect(w, d, cx=0.0, cy=0.0):
    return [(cx - w / 2, cy - d / 2), (cx + w / 2, cy - d / 2), (cx + w / 2, cy + d / 2), (cx - w / 2, cy + d / 2)]


def ngon(r, n, phase=0.0):
    return [(r * math.cos(phase + i * math.tau / n), r * math.sin(phase + i * math.tau / n)) for i in range(n)]


def offset(poly, d):
    """Offset a counter-clockwise polygon outward by d, mitred (works for right-angled re-entrant corners)."""
    n, out, norms = len(poly), [], []
    for i in range(n):
        (ax, ay), (bx, by) = poly[i], poly[(i + 1) % n]
        L = math.hypot(bx - ax, by - ay)
        norms.append(((by - ay) / L, -(bx - ax) / L))
    for i in range(n):
        n1, n2 = norms[i - 1], norms[i]
        k = 1 + n1[0] * n2[0] + n1[1] * n2[1]
        out.append((poly[i][0] + (n1[0] + n2[0]) / k * d, poly[i][1] + (n1[1] + n2[1]) / k * d))
    return out


def comb(base, pd, cw, pw=0.0, every=0.0):
    """A facade plan with MODELLED PIERS: `base` is the glass line (convex, counter-clockwise);
    a corner post `cw` wide stands `pd` proud at every corner and a pier `pw` wide every `every`
    metres between. Returns (points, tags, fac): tags are "pier" or "bay"; fac says where each
    bay's facade starts, so the painted bays begin at the corner post and the piers land on them."""
    q = offset(base, pd)
    n = len(base)
    pts, tags, fac = [], [], []
    for i in range(n):
        p, pn = Vector(base[i]), Vector(base[(i + 1) % n])
        L = (pn - p).length
        t = (pn - p) / L
        o = Vector((t.y, -t.x)) * pd
        start = len(pts) + 2                                       # the index of (cw, 0): the facade's first glass point
        row = [(Vector(q[i]), "pier"), (p + t * cw + o, "pier"), (p + t * cw, "bay")]
        s = every
        while every and s + pw / 2 < L - cw - 1.0:
            if s - pw / 2 > cw + 1.0:
                row += [(p + t * (s - pw / 2), "pier"), (p + t * (s - pw / 2) + o, "pier"),
                        (p + t * (s + pw / 2) + o, "pier"), (p + t * (s + pw / 2), "bay")]
            s += every
        row += [(pn - t * cw, "pier"), (pn - t * cw + o, "pier")]
        for v, tag in row:
            pts.append((v.x, v.y)); tags.append(tag); fac.append((start, cw))
    return pts, tags, fac


class Frame:
    """A tower's place: its foot at bearing and distance from the can, its local -y facing the can."""

    def __init__(self, bearing, r, turn=0.0, scale=1.0):
        self.bearing, self.r, self.scale = bearing, r, scale
        o = K.polar(r, bearing)
        self.o = Vector((o.x, o.y, 0.0))
        a = math.radians(bearing + turn)
        self.dx = Vector((math.cos(a), -math.sin(a), 0.0))
        self.dy = Vector((math.sin(a), math.cos(a), 0.0))

    def p(self, x, y, z):
        """`scale` widens the PLAN only (the needle, whose rows-only facade has no bay to keep)."""
        return self.o + self.dx * (x * self.scale) + self.dy * (y * self.scale) + Z * z

    def ring(self, plan, z):
        return [self.p(x, y, z) for x, y in plan]


# ---------------------------------------------------------------- the mesh builder
class Mesh:
    """Closed solids with per-face, true-scale UVs. `begin`, `seg`..., `end` builds ONE closed
    solid out of segments (a tower's body); `solid`, `box`, `tube` add separate closed solids
    (attached parts)."""

    def __init__(self, name, origin=None, smooth=False):
        self.name, self.bm, self.slots, self.smooth = name, bmesh.new(), [], smooth
        self.uv = self.bm.loops.layers.uv.new("UVMap")
        self.origin = Vector(origin) if origin is not None else Vector((0, 0, 0))
        self._open = self._first = None

    def _slot(self, mat):
        kind = mat.split(".")[0]
        if kind not in self.slots:
            self.slots.append(kind)
        return self.slots.index(kind)

    def _uv(self, f, mat, anchor=None, u0=0.0):
        """Unwrap ONE face at true scale: u along its horizontal, v up its slope, in metres over the
        tile. A trim face picks its LED row; a base face runs v from the city floor to the lobby."""
        kind, _, sub = mat.partition(".")
        f.material_index = self._slot(mat)
        f.smooth = self.smooth
        f.normal_update()
        n = f.normal
        if n.length < 1e-6:
            return
        pts = [l.vert.co for l in f.loops]
        a = anchor if anchor is not None else self.origin
        if abs(n.z) > 0.999:
            uvs = [((p - a).x, (p - a).y) for p in pts]
        else:
            U = Z.cross(n).normalized()
            V = n.cross(U).normalized()
            low = min(pts, key=lambda p: p.z)
            uvs = [(u0 + (p - a).dot(U), low.z + (p - low).dot(V)) for p in pts]
        if kind == "trim":
            row = T.TRIM.index(sub)
            vs = [v for _, v in uvs]
            lo, span = min(vs), max(max(vs) - min(vs), 1e-6)
            uvs = [(u / 8.0, (row + 0.18 + 0.64 * (v - lo) / span) / 8.0) for u, v in uvs]
        elif kind == "base":
            uvs = [(u / 64.0, (p.z - FOOT) / (LOBBY - FOOT)) for (u, _), p in zip(uvs, pts)]
        else:
            s = 1.0 / T.TILE_M.get(kind, 32.0)
            uvs = [(u * s, v * s) for u, v in uvs]
        for l, uv in zip(f.loops, uvs):
            l[self.uv].uv = uv

    def face(self, pts, mat, anchor=None, u0=0.0):
        f = self.bm.faces.new([self.bm.verts.new(p) for p in pts])
        self._uv(f, mat, anchor, u0)
        return f

    def quad_uv(self, pts, mat, box):
        """A quad with explicit UVs: pts are bottom-left, bottom-right, top-right, top-left."""
        f = self.bm.faces.new([self.bm.verts.new(p) for p in pts])
        f.material_index = self._slot(mat)
        u0, v0, u1, v1 = box
        for l, uv in zip(f.loops, ((u0, v0), (u1, v0), (u1, v1), (u0, v1))):
            l[self.uv].uv = uv
        return f

    # ---- one closed solid out of segments
    def begin(self):
        self._open = self._first = None

    def seg(self, rings, mats, fac=None, span=None, ledge="metal"):
        """Loft `rings` (lists of points, all the same count, counter-clockwise seen from above or
        from ahead). mats[j] is edge j's material; span(i, j, mat) may answer another for the span
        from ring i to i+1; a level span is a ledge. Joins the previous segment with a filled ledge."""
        bm = self.bm
        n = len(rings[0])
        loops = [[bm.verts.new(p) for p in ring] for ring in rings]
        for i in range(len(rings) - 1):
            for j in range(n):
                k = (j + 1) % n
                quad = [loops[i][j], loops[i][k], loops[i + 1][k], loops[i + 1][j]]
                keep = []
                for v in quad:
                    if all((v.co - w.co).length > 1e-4 for w in keep):
                        keep.append(v)
                if len(keep) < 3:
                    continue
                m = mats[j]
                level = max(v.co.z for v in keep) - min(v.co.z for v in keep) < 1e-4
                over = span(i, j, m) if span else None
                m = over if over else (ledge if level else m)
                f = bm.faces.new(keep)
                if fac is None:
                    anchor, u0 = rings[i][j], 0.0
                else:
                    anchor, u0 = rings[i][fac[j][0]], fac[j][1]
                    if u0 == "arc":
                        u0 = j * (rings[i][k] - rings[i][j]).length
                        anchor = rings[i][j]
                self._uv(f, m, anchor, u0)
        if self._open is None:
            self._first = loops[0]
        else:
            self._bridge(self._open, loops[0], ledge)
        self._open = loops[-1]
        return self

    def _bridge(self, lower, upper, mat):
        if len(lower) == len(upper) and all((a.co - b.co).length < 1e-4 for a, b in zip(lower, upper)):
            return                                                 # the same outline: the weld joins them
        edges = []
        for loop in (lower, upper):
            for i in range(len(loop)):
                e = self.bm.edges.get((loop[i], loop[(i + 1) % len(loop)]))
                if e is not None:
                    edges.append(e)
        out = bmesh.ops.triangle_fill(self.bm, use_beauty=True, use_dissolve=False, edges=edges, normal=Z)
        for g in out["geom"]:
            if isinstance(g, bmesh.types.BMFace):
                self._uv(g, mat)

    def _cap(self, loop, mat):
        uniq = []
        for v in loop:
            if all((v.co - w.co).length > 1e-4 for w in uniq):
                uniq.append(v)
        if len(uniq) >= 3:
            self._uv(self.bm.faces.new(uniq), mat)

    def end(self, top="metal", bottom="metal"):
        self._cap(self._first[::-1], bottom)
        self._cap(self._open, top)
        self._open = self._first = None
        return self

    def solid(self, rings, mats, fac=None, span=None, top="metal", bottom="metal"):
        self.begin()
        self.seg(rings, mats, fac, span)
        return self.end(top, bottom)

    def box(self, c, size, xdir=None, zdir=None, mat="metal", mats=None):
        """A closed box at c: size is (along xdir, along ydir, along zdir). mats may name a material
        for "-y" (the face toward -ydir), "+y", "-x", "+x", "-z", "+z"."""
        X = (xdir or Vector((1, 0, 0))).normalized()
        Zd = (zdir or Z).normalized()
        Y = Zd.cross(X).normalized()
        c = Vector(c)
        hx, hy, hz = size[0] / 2, size[1] / 2, size[2] / 2
        P = lambda a, b, d: c + X * (a * hx) + Y * (b * hy) + Zd * (d * hz)
        sides = {"-y": [P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), P(-1, -1, 1)],
                 "+y": [P(1, 1, -1), P(-1, 1, -1), P(-1, 1, 1), P(1, 1, 1)],
                 "-x": [P(-1, 1, -1), P(-1, -1, -1), P(-1, -1, 1), P(-1, 1, 1)],
                 "+x": [P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), P(1, -1, 1)],
                 "-z": [P(-1, 1, -1), P(1, 1, -1), P(1, -1, -1), P(-1, -1, -1)],
                 "+z": [P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1)]}
        vs = {}
        for key, pts in sides.items():
            loop = []
            for p in pts:
                k = (round(p.x, 4), round(p.y, 4), round(p.z, 4))
                if k not in vs:
                    vs[k] = self.bm.verts.new(p)
                loop.append(vs[k])
            self._uv(self.bm.faces.new(loop), (mats or {}).get(key, mat), c)
        return self

    def tube(self, p0, p1, r, sides=8, r1=None, mat="metal", cap=None):
        """A closed prism from p0 to p1: a mast, a strut, a column, a tank."""
        p0, p1 = Vector(p0), Vector(p1)
        axis = (p1 - p0).normalized()
        ref = Z if abs(axis.z) < 0.9 else Vector((1, 0, 0))
        u = axis.cross(ref).normalized()
        v = axis.cross(u)
        rb = r if r1 is None else r1
        ring = lambda p, rr: [p + (u * math.cos(t) + v * math.sin(t)) * rr for t in (math.tau * i / sides for i in range(sides))]
        return self.solid([ring(p0, r), ring(p1, rb)], [mat] * sides, [(0, "arc")] * sides, top=cap or mat, bottom=cap or mat)

    def torus(self, centre, axis, R, r, major=40, minor=8, mats=None):
        """A closed ring round `axis`: minor section k (0 outermost, counted round) takes mats[k]."""
        axis = axis.normalized()
        a = axis.cross(Z).normalized()
        b = axis.cross(a).normalized()
        grid = []
        for i in range(major):
            t = math.tau * i / major
            out = a * math.cos(t) + b * math.sin(t)
            grid.append([self.bm.verts.new(Vector(centre) + out * (R + r * math.cos(math.tau * (k + 0.5) / minor))
                                           + axis * (r * math.sin(math.tau * (k + 0.5) / minor))) for k in range(minor)])
        for i in range(major):
            for k in range(minor):
                i2, k2 = (i + 1) % major, (k + 1) % minor
                f = self.bm.faces.new((grid[i][k], grid[i2][k], grid[i2][k2], grid[i][k2]))
                self._uv(f, (mats or ["metal"] * minor)[k], grid[i][k].co, 0.0)
        return self

    def done(self, coll, weld=True):
        bm = self.bm
        if weld:
            bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0005)
            bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        me = bpy.data.meshes.new(self.name)
        bm.to_mesh(me); bm.free()
        for kind in self.slots:
            me.materials.append(material(kind))
        ob = bpy.data.objects.new(self.name, me)
        coll.objects.link(ob)
        return ob


# ---------------------------------------------------------------- shared tower pieces
def mast(P, at, z, height, r=1.4, arms=2, light="red"):
    """A mast from inside the roof: tapered, cross arms, a light on top."""
    x, y = at.x, at.y
    P.tube((x, y, z - 3.0), (x, y, z + height), r, 6, r * 0.25)
    for k in range(arms):
        zz = z + height * (0.45 + 0.2 * k)
        P.box((x, y, zz), (r * 5.0 - k * r, 0.5, 0.5))
    P.box((x, y, z + height + 0.4), (1.1, 1.1, 1.1), mat="trim." + light)


def plant(P, F, x, y, z, kind=0):
    """Rooftop plant, sunk 0.3 m into the roof: 0 a chiller with two fan drums, 1 a round tank on a
    plinth, 2 a stair bulkhead with a lit door side, 3 a row of three low vents."""
    c = F.p(x, y, z)
    if kind == 0:
        P.box(c + Z * 1.6, (9.0, 6.0, 3.8), F.dx)
        for dx in (-2.2, 2.2):
            q = F.p(x + dx, y, z + 3.2)
            P.tube(q, q + Z * 1.6, 1.7, 10)
    elif kind == 1:
        P.box(c + Z * 0.7, (7.0, 7.0, 2.0), F.dx)
        P.tube(c + Z * 1.4, c + Z * 8.4, 3.0, 12)
        P.tube(c + Z * 8.3, c + Z * 10.0, 3.0, 12, 0.6)
    elif kind == 2:
        P.box(c + Z * 2.6, (7.0, 5.0, 5.8), F.dx, mats={"-y": "trim.amber"})
        P.box(c + Z * 5.7, (8.0, 6.0, 0.6), F.dx)
    else:
        for k in (-1, 0, 1):
            P.box(F.p(x + k * 4.0, y, z + 0.9), (2.6, 5.0, 2.4), F.dx)


def sign(P, F, name, x, wall_y, z, standoff=2.4):
    """A REAL SIGN on the face of a tower that looks at the can: four brackets that end 1.5 m inside
    the wall, a backing box, a frame round it, and the painted face on a slab whose front stands
    5 cm proud of the backing and is the only face in its plane."""
    w, h = T.SIGNS[name]["size"]
    back_y = wall_y - standoff                                    # the backing's rear
    front_y = back_y - 1.0                                        # the backing's front
    P.box(F.p(x, back_y - 0.5, z), (w + 0.8, 1.0, h + 0.8), F.dx)
    for sx in (-1, 1):
        for sz in (-1, 1):
            P.box(F.p(x + sx * (w / 2 - 2.0), (back_y - 0.4 + wall_y + 1.5) / 2, z + sz * (h / 2 - 2.0)),
                  (0.7, wall_y + 1.5 - (back_y - 0.4), 0.7), F.dx)
    for sz in (-1, 1):                                            # the frame: four beams, 0.45 m proud
        P.box(F.p(x, front_y - 0.2, z + sz * (h / 2 + 0.7)), (w + 2.8, 0.9, 0.6), F.dx)
    for sx in (-1, 1):                                            # the uprights run 0.2 m into the rails, and stand a little less proud
        P.box(F.p(x + sx * (w / 2 + 0.7), front_y - 0.1, z), (0.6, 0.7, h + 1.2), F.dx)
    # The face slab: 10 cm thick, half of it sunk in the backing, its front 5 cm proud.
    fy = front_y - 0.05
    slab = [F.p(x - w / 2, fy, z - h / 2), F.p(x + w / 2, fy, z - h / 2), F.p(x + w / 2, fy, z + h / 2), F.p(x - w / 2, fy, z + h / 2)]
    rear = [p + F.dy * 0.1 for p in slab]
    bm = P.bm
    a = [bm.verts.new(p) for p in slab]
    b = [bm.verts.new(p) for p in rear]
    f = bm.faces.new(a)
    f.material_index = P._slot("signs")
    for l, uv in zip(f.loops, _corners(T.atlas_uv(T.SIGNS[name]["box"], T.SIGN_ATLAS))):
        l[P.uv].uv = uv
    P._uv(bm.faces.new(b[::-1]), "metal")
    for i in range(4):
        k = (i + 1) % 4
        P._uv(bm.faces.new((a[k], a[i], b[i], b[k])), "metal")
    SIGN_LOG.append((name, F.bearing, F.r, z, w, h))


def _corners(box):
    u0, v0, u1, v1 = box
    return ((u0, v0), (u1, v0), (u1, v1), (u0, v1))


SIGN_LOG = []


class Tower:
    def __init__(self, tid, design, bearing, r, coll, scale=1.0):
        self.name = "city_%s_%s" % (tid, design)
        self.F = Frame(bearing, r, scale=scale)
        self.body = Mesh(self.name + "_body", self.F.o)
        self.parts = Mesh(self.name + "_parts", self.F.o)
        self.fx = None
        self.coll = coll
        self.objects = []

    def base(self, plan, belt):
        """The lower shaft, city floor to sky lobby, in the hazed base texture; then a belt."""
        F = self.F
        self.body.begin()
        self.body.seg([F.ring(plan, FOOT - 2.0), F.ring(plan, LOBBY - 8.0)], ["base"] * len(plan))
        self.body.seg([F.ring(belt, LOBBY - 8.0), F.ring(belt, LOBBY - 2.0), F.ring(belt, LOBBY)], ["metal"] * len(belt),
                      span=lambda i, j, m: "trim.white" if i == 1 else None)

    def done(self):
        self.objects.append(self.body.done(self.coll))
        self.objects.append(self.parts.done(self.coll))
        if self.fx is not None:
            self.objects.append(self.fx.done(self.coll, weld=False))
        return self


def wall(F, pts, z0, zt):
    """A tier's rings: the wall, a 2.4 m LED line under the parapet, the parapet."""
    return [F.ring(pts, z0), F.ring(pts, zt - 2.4), F.ring(pts, zt), F.ring(pts, zt + 1.6)]


def strip(P, F, x, y, z0, z1, led, w=1.1):
    """AN EDGE LIGHT: a closed LED bar up a corner or a column, standing proud of what it is on
    (its foot and head run 1 m into the ledges it joins). The owner, 2026-10-05: "buildings should
    also be more visible": a lit edge is what cuts a dark tower out of a dark sky."""
    P.box(F.p(x, y, (z0 + z1) / 2), (w, w, z1 - z0), F.dx, mat="trim." + led)


def cap(facade, led):
    return lambda i, j, m: (("trim." + led) if m == facade else None) if i == 1 else ("metal" if i == 2 else None)


# ---------------------------------------------------------------- the eleven designs
def haligi(t, top, w=80.0, facade="glass_a", led="led_blue", sign_name=None, sign_z=None):
    """Square, modelled piers on every face, three setbacks, a stepped crown, a mast."""
    F, b, P = t.F, t.body, t.parts
    t.base(rect(w + 20, w + 20), rect(w + 12, w + 12))
    z0 = LOBBY
    tiers = [(w, snap(top - 176)), (w - 16, snap(top - 74.7)), (w - 32, top)]
    for k, (width, zt) in enumerate(tiers):
        pts, tags, fac = comb(rect(width, width), 3.0, 6.0, 4.0, 16.0)
        mats = [facade if tag == "bay" else "metal" for tag in tags]
        b.seg(wall(F, pts, z0, zt), mats, fac, span=cap(facade, led))
        if k >= 1:                                                # a lit edge up each corner post of the upper two tiers
            for sx in (-1, 1):
                for sy in (-1, 1):
                    strip(P, F, sx * (width / 2 + 3.0), sy * (width / 2 + 3.0), z0 - 1.0, zt + 1.0, led)
        for sx in (-1, 1):                                        # plant on the terrace this tier leaves
            if k < 2:
                plant(P, F, sx * (width / 2 - 4.5), 0.0, zt + 1.3, kind=k)
                plant(P, F, 0.0, sx * (width / 2 - 4.5), zt + 1.3, kind=3)
        z0 = zt + 1.6
    cw = w - 46
    b.seg([F.ring(rect(cw, cw), z0), F.ring(rect(cw, cw), z0 + 18.0), F.ring(rect(cw, cw), z0 + 20.0)], ["metal"] * 4,
          span=lambda i, j, m: "trim.white" if i == 1 else None)
    oc = ngon(cw * 0.36, 8, math.pi / 8)
    b.seg([F.ring(oc, z0 + 20.0), F.ring(oc, z0 + 34.0), F.ring(ngon(cw * 0.2, 8, math.pi / 8), z0 + 44.0)], ["metal"] * 8)
    b.end()
    mast(P, F.o, z0 + 43.0, 62.0, 1.8, 3)
    for sx in (-1, 1):                                            # four finials on the top tier's corner posts
        for sy in (-1, 1):
            q = F.p(sx * (w / 2 - 16 + 0.5), sy * (w / 2 - 16 + 0.5), top)
            P.box(q + Z * 5.0, (5.0, 5.0, 13.0), F.dx)
            P.box(q + Z * 12.0, (2.4, 2.4, 3.0), F.dx, mat="trim." + led)
    if sign_name:
        sign(P, F, sign_name, 0.0, -(w - 32) / 2 - 3.0, sign_z)


def plus(core, arm):
    c, a = core / 2, core / 2 + arm
    return [(-c, -a), (c, -a), (c, -c), (a, -c), (a, c), (c, c), (c, a), (-c, a), (-c, c), (-a, c), (-a, -c), (-c, -c)]


def tirahan(t, top, core=24.0, arm=24.0, facade="resi", sign_name=None, sign_z=None):
    """A cross-plan condominium: balcony slabs every three floors, the core rising over the wings."""
    F, b, P = t.F, t.body, t.parts
    plan = plus(core, arm)
    out = offset(plan, 2.6)
    t.base(offset(plan, 12.0), offset(plan, 7.0))
    rings, z = [], LOBBY
    step = 3 * FLOOR
    while z < top - 1.0:
        zt = min(z + step, top)
        rings += [F.ring(plan, z + (1.2 if rings else 0.0)), F.ring(plan, zt - 0.01 if zt >= top else zt)]
        if zt < top:
            rings += [F.ring(out, zt), F.ring(out, zt + 1.2)]     # the slab: out, up, and the next wall comes back in
        z = zt
    b.seg(rings, [facade] * 12)
    b.seg([F.ring(offset(plan, 1.2), top), F.ring(offset(plan, 1.2), top + 2.4)], ["metal"] * 12)   # the parapet oversails
    cr = rect(core - 2, core - 2)
    b.seg([F.ring(cr, top + 2.4), F.ring(cr, top + 14.0), F.ring(cr, top + 16.0)], ["metal"] * 4,
          span=lambda i, j, m: "trim.amber" if i == 1 else None)
    b.end()
    half = core / 2 + arm / 2
    for sx, sy in ((1, 0), (-1, 0), (0, 1), (0, -1)):             # a lit edge up the middle of each wing's end, upper half
        reach = core / 2 + arm + 3.1
        strip(P, F, sx * reach, sy * reach, (LOBBY + top) / 2, top + 2.0, "amber", 1.0)
    for k, (sx, sy) in enumerate(((1, 0), (-1, 0), (0, 1), (0, -1))):
        plant(P, F, sx * half, sy * half, top + 2.1, kind=(1, 1, 0, 2)[k])
    mast(P, F.p(4.0, 3.0, 0), top + 15.5, 30.0, 1.0, 2)
    P.tube(F.p(-5.0, -4.0, top + 15.5), F.p(-5.0, -4.0, top + 26.0), 0.6, 6, 0.2)
    if sign_name:
        sign(P, F, sign_name, 0.0, -(core / 2 + arm) - 2.6, sign_z)


def slab_plan(w, d, sw, sd):
    """A slab with a lift spine standing proud on both narrow ends."""
    x, y, s = w / 2, d / 2, sw / 2
    return [(-x, -y), (-s, -y), (-s, -y - sd), (s, -y - sd), (s, -y), (x, -y), (x, y), (s, y), (s, y + sd), (-s, y + sd), (-s, y), (-x, y)]


def magkapatid(t, top, top2, facade="glass_b", sign_name=None, sign_z=None):
    """THE BRIDGED PAIR: two slabs end-on to the stadium, one taller, two skybridges between them."""
    F, P = t.F, t.parts
    w, d, gap = 32.0, 80.0, 52.0
    cx = (w + gap) / 2
    mats = [facade, "metal", "metal", "metal", facade, facade, facade, "metal", "metal", "metal", facade, facade]
    bodies = []
    for side, tt in ((-1, top), (1, top2)):
        b = t.body if side < 0 else Mesh(t.name + "_body_b", F.o)
        plan = [(x + side * cx, y) for x, y in slab_plan(w, d, 12.0, 4.0)]
        outer = [(x + side * cx, y) for x, y in rect(w + 14, d + 22)]
        belt = [(x + side * cx, y) for x, y in rect(w + 8, d + 14)]
        b.begin()
        b.seg([F.ring(outer, FOOT - 2.0), F.ring(outer, LOBBY - 8.0)], ["base"] * 4)
        b.seg([F.ring(belt, LOBBY - 8.0), F.ring(belt, LOBBY - 2.0), F.ring(belt, LOBBY)], ["metal"] * 4,
              span=lambda i, j, m: "trim.white" if i == 1 else None)
        fac = [(0, 0.0), (1, 0), (2, 0), (3, 0), (4, 0), (5, 0.0), (6, 0.0), (7, 0), (8, 0), (9, 0), (10, 0), (11, 0.0)]
        inset = [(x + side * cx, y) for x, y in slab_plan(w - 6.0, d - 6.0, 9.0, 4.0)]
        rings, zz = [F.ring(plan, LOBBY)], snap(tt - 16 * FLOOR)
        belts = []
        while zz > LOBBY + 60.0:
            belts.append(zz)
            zz -= 18 * FLOOR
        for zb in belts[::-1]:                                    # a recessed plant floor every eighteen floors: a dark belt
            rings += [F.ring(plan, zb), F.ring(inset, zb), F.ring(inset, zb + FLOOR), F.ring(plan, zb + FLOOR)]
        rings += [F.ring(plan, tt - 1.2), F.ring(plan, tt), F.ring(plan, tt + 1.8)]
        last = len(rings) - 3
        b.seg(rings, mats, fac,
              span=lambda i, j, m, last=last: (("trim.mint" if m == facade else None) if i == last else
                                               ("metal" if (i == last + 1 or (0 < i < last and i % 4 == 2)) else None)))
        sp = [(x + side * cx, y) for x, y in rect(11.0, d + 7.0)]   # the two spines run on up as one fin over the roof
        b.seg([F.ring(sp, tt + 1.8), F.ring(sp, tt + 15.0), F.ring([(x + side * cx, y) for x, y in rect(7.0, d + 3.0)], tt + 19.0)], ["metal"] * 4)
        b.end()
        bodies.append(b)
        for yy, kind in ((-26.0, 0), (-9.0, 3), (26.0, 1)):
            plant(P, F, side * cx + (9.0 if kind != 3 else -8.0) * side, yy, tt + 1.5, kind)
        for sy in (-1, 1):                                        # a lit seam up the face of each lift spine
            P.box(F.p(side * cx, sy * (d / 2 + 4.0), (LOBBY + tt) / 2), (1.6, 0.5, tt - LOBBY - 12.0), F.dx, mat="trim.white")
        mast(P, F.p(side * cx, 0, 0), tt + 18.0, 34.0 if side < 0 else 20.0, 1.2, 2)
    t.extra = bodies[1]
    for zz in (snap(top2 - 34.0), snap(top2 - 34.0 - 64.0)):      # the skybridges, 4 m inside each slab
        x0, x1 = -(gap / 2 + 4.0), gap / 2 + 4.0
        sect = lambda x: [F.p(x, -5.0, zz), F.p(x, 5.0, zz), F.p(x, 5.0, zz + 6.5), F.p(x, 0.0, zz + 8.5), F.p(x, -5.0, zz + 6.5)]
        P.solid([sect(x0), sect(x1)], ["metal", "bands", "metal", "metal", "bands"])
        for sx in (-1, 1):                                        # raking struts from each slab up under the deck
            for sy in (-3.5, 3.5):
                P.tube(F.p(sx * (gap / 2 + 2.0), sy, zz - 17.0), F.p(sx * 4.0, sy, zz + 1.0), 0.8, 6)
        P.box(F.p(0, 0, zz - 0.3), (gap + 4.0, 1.2, 0.5), F.dx, mat="trim.cyan")
    if sign_name:
        sign(P, F, sign_name, cx, -d / 2 - 4.0, sign_z)


def bilog(t, top, R=30.56, facade="bands", led="cyan"):
    """Round: three drums, a ring balcony every six floors, a dome and a needle."""
    F, b, P = t.F, t.body, t.parts
    b.smooth = True
    n = 24
    circ = lambda r: ngon(r, n, -math.pi / 2 + math.pi / n)        # the seam of the painted rows faces the can's far side
    t.base(circ(R + 12.0), circ(R + 7.0))
    rings, z = [], LOBBY
    drums = [(R, snap(top - 150)), (R * 5 / 6, snap(top - 58)), (R * 2 / 3, top)]
    fac = [(0, "arc")] * n
    for k, (r, zt) in enumerate(drums):
        seg, first = [], True
        while z < zt - 1.0:
            zn = min(z + 6 * FLOOR, zt)
            seg += [F.ring(circ(r), z + (0.0 if first else 1.4)), F.ring(circ(r), zn)]
            if zn < zt:
                seg += [F.ring(circ(r + 3.2), zn), F.ring(circ(r + 3.2), zn + 1.4)]
            z, first = zn, False
        seg += [F.ring(circ(r + 1.6), zt), F.ring(circ(r + 1.6), zt + 1.2), F.ring(circ(r + 1.6), zt + 2.2)]   # a cornice, a lit line in it
        b.seg(seg, [facade] * n, fac, span=lambda i, j, m, last=len(seg) - 3: ("trim." + led) if i == last else ("metal" if i == last + 1 else None))
        z = zt + 2.2
        if k < 2:
            for a in (40, 160, 280):
                q = K.polar(r - 4.0, a)
                plant(P, F, q.x * 0.92, q.y * 0.92, zt + 1.9, kind=3 if a != 160 else 1)
    r = R * 2 / 3
    dome = [(r - 3.0, 0.0), (r - 3.0, 5.0), (r * 0.86, 13.0), (r * 0.62, 20.0), (r * 0.34, 25.0), (2.2, 27.5)]
    b.seg([F.ring(circ(rr), z + dz) for rr, dz in dome], ["metal"] * n, fac)
    b.end()
    mast(P, F.o, z + 27.0, 52.0, 1.5, 0)


def singsing(t, top):
    """THE GREAT RING: an octagonal shaft with a spine on each chamfer, a saddle, two prongs, and a
    ring 94 m across standing in them, facing the can. `top` is the ring's centre."""
    F, b, P = t.F, t.body, t.parts
    R, r = 52.0, 6.0
    roof = snap(top - R - r - 18.0)

    def plan(w, c):
        """A chamfered square with a spine 8 m wide standing 4 m proud of each chamfer."""
        h = w / 2
        corners = [((h - c, -h), (h, -h + c)), ((h, h - c), (h - c, h)), ((-h + c, h), (-h, h - c)), ((-h, -h + c), (-h + c, -h))]
        pts, tags = [], []
        for p0, p1 in corners:
            p0, p1 = Vector(p0), Vector(p1)
            tdir = (p1 - p0).normalized()
            o = Vector((tdir.y, -tdir.x)) * 4.0
            mid = (p0 + p1) / 2
            for v, tag in ((p0, "metal"), (mid - tdir * 4.0, "metal"), (mid - tdir * 4.0 + o, "metal"),
                           (mid + tdir * 4.0 + o, "metal"), (mid + tdir * 4.0, "metal"), (p1, "glass")):
                pts.append((v.x, v.y)); tags.append(tag)
        return pts, tags

    t.base(rect(100.0, 100.0), rect(92.0, 92.0))
    z0 = LOBBY
    for w, c, zt in ((80.0, 20.0, snap(roof - 96.0)), (64.0, 16.0, roof)):
        pts, tags = plan(w, c)
        mats = ["glass_a" if tag == "glass" else "metal" for tag in tags]
        b.seg(wall(F, pts, z0, zt), mats, span=cap("glass_a", "cyan"))
        z0 = zt + 1.6
    sad = rect(2 * R + 16.0, 20.0)                                # the saddle the prongs stand in: wider than the shaft
    b.seg([F.ring(rect(48.0, 18.0), z0), F.ring(rect(48.0, 18.0), z0 + 6.0)], ["metal"] * 4)
    b.seg([F.ring(sad, z0 + 6.0), F.ring(sad, z0 + 13.0), F.ring(rect(2 * R + 10.0, 14.0), z0 + 16.0)], ["metal"] * 4,
          span=lambda i, j, m: None)
    b.end()
    P.box(F.p(0, -10.2, z0 + 9.5), (2 * R + 6.0, 0.5, 1.2), F.dx, mat="trim.cyan")   # one lit line along the saddle
    for sx in (-1, 1):                                            # the prongs: from inside the saddle to past the ring's waist
        foot = lambda z, w, d, lean: [F.p(sx * (R + lean) + x, y, z) for x, y in rect(w, d)]
        P.solid([foot(z0 + 8.0, 13.0, 15.0, 0.0), foot(top - 6.0, 9.0, 12.0, 0.0), foot(top + 22.0, 5.0, 8.0, -1.5)], ["metal"] * 4)
        P.box(F.p(sx * (R + 6.9), 0, top + 2.0), (0.5, 3.0, 40.0), F.dx, mat="trim.cyan")
        plant(P, F, sx * 16.0, -18.0, roof + 1.3, kind=0)
        plant(P, F, sx * 18.0, 20.0, roof + 1.3, kind=1)
    P.torus(F.p(0, 0, top), F.dy, R, r, 48, 8,
            ["metal", "metal", "metal", "metal", "trim.cyan", "metal", "metal", "metal"])
    for sx in (-1, 1):                                            # a cradle: two struts from the saddle into the ring's foot
        P.tube(F.p(sx * 16.0, 0, z0 + 14.0), F.p(sx * 9.0, 0, top - R + 1.5), 1.3, 6)
    mast(P, F.p(-R, 0, 0), top + 20.0, 26.0, 0.9, 1)
    mast(P, F.p(R, 0, 0), top + 20.0, 18.0, 0.9, 1)


def korona(t, top, R=1.0):
    """THE HOLOGRAM CROWN: a hexagon in three tiers, six blades leaning out from its roof, and the
    TUMP logo as a hologram standing inside them over an emitter. `top` is the roof; `R` scales the
    plan and the crown (the piers keep their 16 m spacing, so they still land on the painted bays)."""
    F, b, P = t.F, t.body, t.parts
    hexa = lambda r: ngon(r * R, 6, 0.0)                                       # corners left and right, a face toward the can
    t.base(hexa(66.0), hexa(60.0))
    z0 = LOBBY
    for r, zt in ((48.0, snap(top - 150.0)), (40.0, snap(top - 58.0)), (32.0, top)):
        pts, tags, fac = comb(hexa(r), 3.0, 5.0, 3.0, 16.0)
        mats = ["glass_b" if tag == "bay" else "metal" for tag in tags]
        b.seg(wall(F, pts, z0, zt), mats, fac, span=cap("glass_b", "cyan"))
        z0 = zt + 1.6
    b.end()
    H = 118.0 * R
    tips = []
    for k, (x, y) in enumerate(hexa(32.0)):                       # the blades stand in the corner posts and lean out
        out = Vector((x, y)).normalized()
        side = Vector((-out.y, out.x))

        def sect(z, lean, depth, width):
            c = Vector((x, y)) + out * lean * R
            return [F.p(*(c - out * depth / 2 - side * width / 2), z), F.p(*(c + out * depth / 2 - side * width / 2), z),
                    F.p(*(c + out * depth / 2 + side * width / 2), z), F.p(*(c - out * depth / 2 + side * width / 2), z)]

        front = y < -1.0                                          # THE CROWN IS OPEN TOWARD THE CAN: the two front blades are
        tall = 24.0 * R if front else (H if k % 2 == 0 else H * 0.8)  # short horns, so nothing stands in front of the hologram
        P.solid([sect(z0 - 6.0, -2.0, 9.0, 5.0), sect(z0 + tall * 0.6, 8.0 if not front else 4.0, 6.0, 3.6),
                 sect(z0 + tall, 15.0 if not front else 7.0, 1.2, 1.4)], ["metal"] * 4,
                span=lambda i, j, m: "trim.cyan" if (i == 1 and j == 0) else None)
        tips.append((Vector((x, y)) + out * 8.0 * R, z0 + tall * 0.6, front))
    for k in range(6):                                            # a ring beam through the tall blades at their waist
        a, c = tips[k], tips[(k + 1) % 6]
        if not (a[2] or c[2]):
            P.tube(F.p(a[0].x, a[0].y, min(a[1], c[1])), F.p(c[0].x, c[0].y, min(a[1], c[1])), 0.9, 6)
    em = [(12.0 * R, 0.0), (12.0 * R, 3.0), (9.0 * R, 5.0), (4.0 * R, 5.0)]       # the emitter dish on the roof
    P.solid([F.ring(ngon(rr, 16), z0 - 0.4 + dz) for rr, dz in em], ["metal"] * 16, [(0, "arc")] * 16, top="trim.cyan",
            span=lambda i, j, m: "trim.cyan" if i == 1 else None)
    plant(P, F, 0.0, 20.0 * R, z0 - 0.3, kind=0)
    plant(P, F, -17.0 * R, -9.0 * R, z0 - 0.3, kind=3)
    # The hologram: light, not a solid. The logo faces the can; three scanline hoops stand round it.
    t.fx = Mesh(t.name + "_fx", F.o)
    w = 72.0 * R
    h = w / K.LOGO_ASPECT
    zc = z0 + 20.0 + h / 2
    t.fx.quad_uv([F.p(-w / 2, 0, zc - h / 2), F.p(w / 2, 0, zc - h / 2), F.p(w / 2, 0, zc + h / 2), F.p(-w / 2, 0, zc + h / 2)],
                 "fx", T.atlas_uv(T.FX["logo"], T.FX_ATLAS))
    su0, sv0, su1, sv1 = T.atlas_uv(T.FX["scan"], T.FX_ATLAS)
    for rr, zz in ((17.0 * R, z0 + 8.0), (23.0 * R, zc + h / 2 + 6.0)):
        hoop = ngon(rr, 20)
        for k in range(20):
            (x0, y0), (x1, y1) = hoop[k], hoop[(k + 1) % 20]
            t.fx.quad_uv([F.p(x0, y0, zz), F.p(x1, y1, zz), F.p(x1, y1, zz + 3.0), F.p(x0, y0, zz + 3.0)], "fx",
                         (su0 + (su1 - su0) * k / 20, sv0, su0 + (su1 - su0) * (k + 1) / 20, sv1))


def parola(t, top):
    """THE NEEDLE: a slender round shaft carrying two saucers and a lantern. `top` is the lantern."""
    F, b, P = t.F, t.body, t.parts
    b.smooth = True
    n = 24
    circ = lambda r: ngon(r, n, -math.pi / 2 + math.pi / n)
    s1, s2 = snap(top - 112.0), snap(top - 48.0)
    prof = [(30.0, FOOT - 2.0, "base"), (30.0, LOBBY - 8.0, None), (24.0, LOBBY - 8.0, "metal"), (24.0, LOBBY, None),
            (19.0, LOBBY, "bands"), (17.0, s1 - 36.0, "metal"),
            (19.0, s1 - 30.0, "metal"), (50.0, s1 - 5.0, "trim.amber"), (51.0, s1 - 3.8, "bands"), (51.0, s1 + 4.2, "metal"),
            (45.0, s1 + 8.0, "metal"), (20.0, s1 + 14.0, "metal"), (14.0, s1 + 16.0, "bands"), (13.0, s2 - 24.0, "metal"),
            (15.0, s2 - 19.0, "metal"), (35.0, s2 - 4.0, "trim.magenta"), (36.0, s2 - 2.8, "bands"), (36.0, s2 + 4.2, "metal"),
            (31.0, s2 + 7.0, "metal"), (14.0, s2 + 11.0, "metal"), (9.0, s2 + 13.0, "metal"), (8.0, top - 11.0, "metal"),
            (13.0, top - 7.0, "trim.white"), (13.0, top + 1.0, "metal"), (15.0, top + 1.8, "metal"), (5.0, top + 14.0, "metal"),
            (1.2, top + 17.0, None)]
    rings = [F.ring(circ(r), z) for r, z, _ in prof]
    tags = [m for _, _, m in prof]
    b.begin()
    b.seg(rings, ["metal"] * n, [(0, "arc")] * n, span=lambda i, j, m: tags[i])
    b.end()
    for k in range(6):                                            # struts from the shaft out under each saucer
        a = k * 60.0 + 30.0
        for zs, rs, reach, drop in ((s1, 17.0, 40.0, 13.0), (s2, 13.0, 28.0, 9.5)):
            p0, p1 = K.polar(rs - 2.0, a), K.polar(reach, a)
            P.tube(F.p(p0.x, p0.y, zs - 56.0), F.p(p1.x, p1.y, zs - drop + 1.0), 1.1, 6)
    mast(P, F.o, top + 16.0, 52.0, 1.3, 0, light="white")
    for a in (20, 140, 260):                                      # a little plant on the lower saucer's roof
        q = K.polar(31.0, a)
        P.box(F.p(q.x, q.y, s1 + 12.0), (6.0, 5.0, 3.4), F.dx)
    t.fx = Mesh(t.name + "_fx", F.o)                               # the lantern's glow: one soft card facing the can
    t.fx.quad_uv([F.p(20.0, -14.0, top - 23.0), F.p(-20.0, -14.0, top - 23.0), F.p(-20.0, -14.0, top + 17.0), F.p(20.0, -14.0, top + 17.0)],
                 "fx", T.atlas_uv(T.FX["glow"], T.FX_ATLAS))


def patong(t, top, w=56.0, facade="resi", led="magenta", sign_name=None, sign_z=None):
    """Stacked boxes, each shifted off the one below, with a dark recessed neck between."""
    F, b, P = t.F, t.body, t.parts
    t.base(rect(w + 22, w + 22), rect(w + 14, w + 14))
    box_h, neck_h = 12 * FLOOR, 2 * FLOOR
    n = max(3, int((top - LOBBY) // (box_h + neck_h)))
    z = top - n * box_h - (n - 1) * neck_h
    shifts = [(0, 0), (7, 0), (0, 0), (0, 7), (-7, 0), (0, -7), (7, 0), (0, 7), (-7, 0), (0, 0), (0, -7), (7, 0)]
    pts0 = rect(w - 22, w - 22)
    b.seg([F.ring(pts0, LOBBY), F.ring(pts0, z)], ["metal"] * 4)   # a plain plinth up to the first box
    for k in range(n):
        sx, sy = shifts[(k + int(t.F.bearing)) % len(shifts)]
        pts, tags, fac = comb(rect(w, w, sx, sy), 2.0, 5.0)
        mats = [facade if tag == "bay" else "metal" for tag in tags]
        b.seg([F.ring(pts, z), F.ring(pts, z + box_h)], mats, fac)
        z += box_h
        if k < n - 1:
            neck = rect(w - 22, w - 22)
            b.seg([F.ring(neck, z), F.ring(neck, z + 1.6), F.ring(neck, z + neck_h)], ["metal"] * 4,
                  span=lambda i, j, m: ("trim." + led) if i == 0 else None)
            z += neck_h
            for cx_ in (-1, 1):                                    # the terrace the shift leaves: a planter row
                P.box(F.p(sx + cx_ * (w / 2 - 6.0), sy, z - neck_h + 0.8), (4.0, w * 0.5, 2.2), F.dx)
    b.end()
    plant(P, F, sx - 12.0, sy + 10.0, z - 0.3, 0)
    plant(P, F, sx + 14.0, sy - 12.0, z - 0.3, 1)
    plant(P, F, sx + 12.0, sy + 14.0, z - 0.3, 2)
    pad = F.p(sx - w / 2 - 9.0, sy - 8.0, z + 1.0)                 # a landing pad cantilevered off the top box
    P.solid([[pad + Vector((math.cos(a), math.sin(a), 0)) * rr + Z * dz for a in (math.tau * i / 16 for i in range(16))]
             for rr, dz in ((10.0, 0.0), (11.0, 0.8), (11.0, 1.6))], ["metal"] * 16, [(0, "arc")] * 16,
            span=lambda i, j, m: ("trim." + led) if i == 1 else None)
    P.box((pad + F.p(sx - w / 2 + 3.0, sy - 8.0, z + 1.0)) / 2 + Z * 0.2, (16.0, 5.0, 1.4), F.dx)
    for dy in (-2.0, 2.0):
        P.tube(F.p(sx - w / 2 + 2.0, sy - 8.0 + dy, z - 14.0), pad + F.dy * dy + Z * 0.3, 0.6, 6)
    mast(P, F.p(sx + 2.0, sy, 0), z - 1.0, 40.0, 1.3, 2)
    if sign_name:
        sign(P, F, sign_name, sx, sy - w / 2 - 2.0, sign_z)


def talim(t, top, w=84.0, d=28.0, facade="glass_b", sign_name=None, sign_z=None):
    """A blade: a long lens plan that tapers all the way up to a slanted top, and a tall fin."""
    F, b, P = t.F, t.body, t.parts
    lens0 = [(-w / 2, 0.0), (-w * 0.25, -d / 2), (w * 0.25, -d / 2), (w / 2, 0.0), (w * 0.25, d / 2), (-w * 0.25, d / 2)]
    t.base(offset(lens0, 13.0), offset(lens0, 8.0))
    pts, tags, fac = comb(lens0, 2.2, 4.0, 3.0, 16.0)             # the piers are modelled once and taper with the blade
    lens = lambda s: [(x * s, y * s) for x, y in pts]
    mid = snap(top - 260.0)
    mats = [facade if tag == "bay" else "metal" for tag in tags]
    cut = top - 46.0
    s_top = 0.52
    upper = lens(s_top)
    slant = [F.p(x, y, cut + 46.0 * (0.5 - x / (w * s_top))) for x, y in upper]     # high on the left, low on the right
    b.seg([F.ring(lens(1.0), LOBBY), F.ring(lens(1.0), mid - 1.2), F.ring(lens(1.0), mid), F.ring(upper, cut), slant], mats, fac,
          span=lambda i, j, m: ("trim.amber" if m == facade else None) if i == 1 else None)
    b.end(top="metal")
    fx_ = -w / 2 * s_top
    fin = lambda z, wd, dp, x: [F.p(x + a, c, z) for a, c in rect(wd, dp)]
    P.solid([fin(mid + 60.0, 5.0, 9.0, -w / 2 * 0.80 + 2.0), fin(cut, 4.0, 7.0, fx_ + 1.0), fin(top + 70.0, 1.2, 2.0, fx_ + 1.0)], ["metal"] * 4,
            span=lambda i, j, m: "trim.amber" if (i == 1 and j == 0) else None)
    for k in range(3):                                            # three vent fins standing in the slanted roof
        x = (-0.12 + k * 0.14) * w * s_top
        P.box(F.p(x, 0.0, cut + 46.0 * (0.5 - x / (w * s_top)) + 1.0), (2.0, d * s_top * 0.7, 6.0), F.dx)
    if sign_name:
        s = 1.0 - (1.0 - s_top) * (sign_z - mid) / (cut - mid)
        sign(P, F, sign_name, 0.0, -d / 2 * s - 2.2 * s, sign_z, standoff=3.4)


def hagdan(t, top, w=72.0, d=64.0, facade="glass_a", sign_name=None, sign_z=None):
    """Terraces stepping back up the side that faces the stadium; a service spine up the back."""
    F, b, P = t.F, t.body, t.parts
    steps, rise, tread = 5, 6 * FLOOR, d * 0.15
    t.base(rect(w + 20, d + 28, 0, 3), rect(w + 12, d + 20, 3 - 3, 3))

    def plan(front):
        pts = [(-w / 2, front)]
        for x in (-w / 4, 0.0, w / 4):                            # three piers stand 2.4 m proud of the stepping front
            pts += [(x - 2.0, front), (x - 2.0, front - 2.4), (x + 2.0, front - 2.4), (x + 2.0, front)]
        return pts + [(w / 2, front), (w / 2, d / 2), (12.0, d / 2), (12.0, d / 2 + 9.0), (-12.0, d / 2 + 9.0),
                      (-12.0, d / 2), (-w / 2, d / 2)]

    mats = [facade, "metal", "metal", "metal"] * 3 + [facade, facade, facade, "metal", "metal", "metal", facade, facade]
    fac = [(0, 0.0)] * 13 + [(13, 0.0), (14, 0.0), (15, 0), (16, 0), (17, 0), (18, 0.0), (19, 0.0)]
    rings = [F.ring(plan(-d / 2), LOBBY)]
    for k in range(steps + 1):
        zt = top - (steps - k) * rise
        front = -d / 2 + k * tread
        rings += [F.ring(plan(front), zt), F.ring(plan(front), zt + 1.5)]     # the parapet's height, then the terrace steps in
        if k < steps:
            rings.append(F.ring(plan(front + tread), zt + 1.5))
            for sx in (-1, 0, 1):                                 # each terrace: two pergolas and a planter
                if sx:
                    c = F.p(sx * w * 0.3, front + tread / 2, zt + 1.5)
                    for px in (-5.0, 5.0):
                        P.box(c + F.dx * px + Z * 2.2, (0.6, 0.6, 5.0), F.dx)
                    P.box(c + Z * 4.6, (12.0, 5.5, 0.6), F.dx)
                else:
                    P.box(F.p(0.0, front + tread / 2, zt + 2.2), (16.0, 3.4, 2.0), F.dx)
    b.seg(rings, mats, fac,
          span=lambda i, j, m: ("trim.amber" if (j < 13 and m == facade) else "metal") if (i >= 1 and (i - 1) % 3 == 0) else None)
    zr = top + 1.5
    sp = rect(22.0, 16.0, 0.0, d / 2 + 0.0)
    b.seg([F.ring(sp, zr), F.ring(sp, zr + 26.0), F.ring(sp, zr + 28.0)], ["metal"] * 4,
          span=lambda i, j, m: "trim.amber" if i == 1 else None)
    b.end()
    plant(P, F, -22.0, d / 2 - 12.0, zr - 0.3, 0)
    plant(P, F, 22.0, d / 2 - 12.0, zr - 0.3, 1)
    mast(P, F.p(0.0, d / 2, 0), zr + 27.0, 44.0, 1.4, 3)
    if sign_name:
        k = steps - 1
        sign(P, F, sign_name, -w / 8, -d / 2 + steps * tread, sign_z, standoff=3.0)


def balangkas(t, top, w=56.0, facade="glass_b", led="amber", sign_name=None, sign_z=None):
    """Glass modules inside an EXOSKELETON: four corner columns, a ring beam at every plant floor,
    and a pair of diagonals on every face of every module."""
    F, b, P = t.F, t.body, t.parts
    t.base(rect(w + 24, w + 24), rect(w + 16, w + 16))
    mod, neck = 16 * FLOOR, 1.5 * FLOOR
    n = max(3, int((top - LOBBY) // (mod + neck)))
    z = top - n * mod - (n - 1) * neck
    core = rect(w - 10, w - 10)
    rings = [F.ring(core, LOBBY), F.ring(core, z)]
    levels = []
    for k in range(n):
        rings += [F.ring(rect(w, w), z), F.ring(rect(w, w), z + mod)]
        z += mod
        if k < n - 1:
            rings += [F.ring(core, z), F.ring(core, z + neck)]
            levels.append(z + neck / 2)
            z += neck
    tags = ["metal", "metal"]
    for k in range(n):
        tags += [facade, "metal"] + (["metal", "metal"] if k < n - 1 else [])
    b.seg(rings, [facade] * 4, [(j, 0.0) for j in range(4)],
          span=lambda i, j, m: ("trim." + led) if (tags[i] == "metal" and i >= 2 and (i - 2) % 4 == 2) else (tags[i] if tags[i] == "metal" else None))
    b.seg([F.ring(rect(w - 14, w - 14), z), F.ring(rect(w - 14, w - 14), z + 7.0)], ["metal"] * 4)
    b.end()
    c = w / 2 + 3.2
    nodes = [LOBBY + 2.0] + levels + [top + 12.0]
    for sx, sy in ((-1, -1), (1, -1), (1, 1), (-1, 1)):           # the four columns, from the belt to over the roof
        P.tube(F.p(sx * c, sy * c, LOBBY - 4.0), F.p(sx * c, sy * c, top + 18.0), 2.6, 8, 2.0)
        P.box(F.p(sx * c, sy * c, top + 18.6), (1.2, 1.2, 1.2), mat="trim.red")
        strip(P, F, sx * (c + 2.1), sy * (c + 2.1), LOBBY + (top - LOBBY) * 0.35, top + 16.0, led, 0.9)
    corners = [(-c, -c), (c, -c), (c, c), (-c, c)]
    for zz in levels + [top + 12.0]:                              # ring beams, their ends inside the columns, tied to the neck
        for k in range(4):
            (x0, y0), (x1, y1) = corners[k], corners[(k + 1) % 4]
            P.tube(F.p(x0, y0, zz), F.p(x1, y1, zz), 1.3, 6)
            mx, my = (x0 + x1) / 2, (y0 + y1) / 2
            if zz < top:
                P.box(F.p(mx * 0.86, my * 0.86, zz), (3.0 if my else abs(mx) * 0.4, 3.0 if mx else abs(my) * 0.4, 1.6), F.dx)
    for i in range(len(nodes) - 2):                               # the diagonals, node to node, alternating
        z0, z1 = nodes[i] + (0.0 if i else 0.0), nodes[i + 1]
        for k in range(4):
            (x0, y0), (x1, y1) = corners[k], corners[(k + 1) % 4]
            if (i + k) % 2 == 0:
                P.tube(F.p(x0, y0, z0), F.p(x1, y1, z1), 1.0, 6)
            else:
                P.tube(F.p(x1, y1, z0), F.p(x0, y0, z1), 1.0, 6)
    for k, (x, y) in enumerate(((-12.0, -10.0), (12.0, -10.0), (0.0, 12.0))):   # three cooling drums on the roof
        q = F.p(x, y, z + 6.6)
        P.tube(q, q + Z * 9.0, 6.0, 12, 5.0)
        P.tube(q + Z * 8.6, q + Z * 9.6, 5.2, 12, mat="trim." + led, cap="metal")
    mast(P, F.o, z + 6.0, 46.0, 1.3, 2)
    if sign_name:
        sign(P, F, sign_name, 0.0, -w / 2, sign_z, standoff=5.4)


DESIGNS = {"haligi": haligi, "tirahan": tirahan, "magkapatid": magkapatid, "bilog": bilog, "singsing": singsing,
           "korona": korona, "parola": parola, "patong": patong, "talim": talim, "hagdan": hagdan, "balangkas": balangkas}

# THE SKYLINE, by sightline. (id, design, bearing, distance, the elevation its `top` reaches seen
# from the can, keyword arguments).
#
# THE OWNER PLAYED THE FIRST SKYLINE (2026-10-05) AND SAID "buildings should also be more visible".
# It stood 440 to 900 m out and showed 6 to 24 degrees over the stadium, 4.5 to 15.5 wide: in the
# game's wide camera that is a few slim dark towers barely over the canopies. So the whole city has
# come IN and UP, and there is more of it:
#   * NOTHING INSIDE RADIUS 300 (the hull's rim is at 239, the landing pads at 254, and the two
#     orbits of sky traffic now run at 268 and 284, inside every tower).
#   * FOUR GATES, as before: the landmark on each open corner's axis, now 365 to 395 m out, its top
#     45 degrees and more up (25 to 33 shown over the corner screen) and 19 to 21 degrees wide; a
#     flanker either side at 480 to 540 m, 10 to 13 degrees wide, LOWER than its landmark (35 to 40
#     degrees) and of unlike build.
#   * OVER EACH STAND three or four towers at 620 to 900 m, in three depths and stepping down with
#     distance (38 down to 28.5 degrees), with gaps of open sky between them (it is a skyline, not
#     a wall): no canopy has an empty sky, and each side keeps ONE AVENUE of sky from the hull out
#     (about 355 north, 86 east, 200 south, 267 west), so from the air the stadium still floats free.
#     The first pass of this (flankers at 42 to 44 degrees) made a canyon and left 60 degrees of sky.
# The other programmer is adding distance haze in Unity: the towers are lit to sit BEHIND it, their
# light and contrast in their upper halves (lit parapets and edges, the crown halo, the signs).
# `sign_below` is how far under the roof a sign's middle hangs.
TOWERS = [
    ("T01", "singsing",   45.0, 365.0, 42.0, {}),
    ("T02", "balangkas",  31.0, 500.0, 40.0, dict(w=80.0, facade="glass_a", led="cyan")),
    ("T03", "tirahan",    59.0, 540.0, 35.0, dict(core=32.0, arm=32.0)),
    ("T04", "haligi",      8.0, 620.0, 38.0, dict(w=120.0, sign_name="liga", sign_below=40.0)),
    ("T05", "patong",     76.0, 640.0, 31.0, dict(w=88.0, sign_name="isko", sign_below=32.0)),
    ("T06", "tirahan",    97.0, 700.0, 36.0, dict(core=36.0, arm=36.0, facade="glass_a", sign_name="kape", sign_below=44.0)),
    ("T07", "bilog",     123.0, 520.0, 36.0, dict(R=48.0)),
    ("T08", "magkapatid", 135.0, 375.0, 46.0, dict(top2_elev=38.0, sign_name="halo", sign_below=52.0)),
    ("T09", "haligi",    148.5, 500.0, 39.0, dict(w=112.0, facade="resi", led="magenta", sign_name="dyip", sign_below=38.0)),
    ("T10", "balangkas", 171.0, 640.0, 37.0, dict(w=96.0, sign_name="sinag", sign_below=42.0)),
    ("T11", "patong",    212.0, 500.0, 39.0, dict(w=88.0, facade="glass_a", led="cyan")),
    ("T12", "korona",    225.0, 395.0, 33.0, dict(R=1.4)),
    ("T13", "talim",     238.0, 520.0, 40.0, dict(w=124.0, d=40.0, sign_name="pansitan", sign_below=120.0)),
    ("T14", "hagdan",    258.0, 620.0, 38.0, dict(w=112.0, d=80.0, sign_name="bahaghari", sign_below=16.0)),
    ("T15", "talim",     276.0, 680.0, 33.0, dict(w=108.0, d=36.0, facade="glass_a")),
    ("T16", "bilog",     302.0, 520.0, 35.0, dict(R=46.0, led="magenta")),
    ("T17", "parola",    315.0, 368.0, 44.0, dict(scale=1.25)),
    ("T18", "hagdan",    328.0, 480.0, 38.0, dict(w=96.0, d=72.0, facade="resi", sign_name="dely", sign_below=16.0)),
    # The far depth over each stand (new): lower, slimmer, further.
    ("T19", "bilog",     345.0, 700.0, 30.0, dict(R=45.0, led="white")),
    ("T20", "patong",     19.5, 820.0, 29.0, dict(w=80.0, facade="glass_b", led="amber")),
    ("T21", "hagdan",    105.0, 860.0, 29.0, dict(w=96.0, d=72.0, facade="glass_b")),
    ("T22", "bilog",     189.0, 760.0, 30.0, dict(R=44.0, led="magenta")),
    ("T24", "balangkas", 248.0, 900.0, 29.0, dict(w=56.0, facade="resi", led="magenta")),
]


# Skybridges low in each gate, below the hull's deck: the two towers and the height.
BRIDGES = [("T01", "T02", -64.0), ("T08", "T09", -96.0), ("T12", "T11", -48.0), ("T17", "T18", -80.0)]
PLACE = {tid: (bearing, r) for tid, _, bearing, r, _, _ in TOWERS}
LOOP_R = 270.0                             # the rail loop: inside every tower's foot (it was 330, where the landmarks now stand)
VIA_BEARING, VIA_SIDE = 109.7, 40.0        # the viaduct's line: through the gap east (T21 | T07) and the gap west (T15 | T16)


def height_at(r, elev):
    return EYE + r * math.tan(math.radians(elev))


def crown_halo(t):
    """THE CROWN HALO: one faint card of violet light standing 6 m BEHIND the tower's upper third,
    facing the can. The tower hides its middle, so what a player sees is a soft rim of light round
    the crown: the silhouette is cut out of the sky. It is light (the fx material), not a solid."""
    F = t.F
    pts = [v.co for m in (t.body, t.parts) for v in m.bm.verts] + ([v.co for v in t.extra.bm.verts] if t.extra else [])
    high = max(p.z for p in pts)
    upper = [p for p in pts if p.z > high * 0.45]
    xs = [(p - F.o).dot(F.dx) for p in upper]
    back = max((p - F.o).dot(F.dy) for p in upper) + 6.0
    cx, half = (min(xs) + max(xs)) / 2, (max(xs) - min(xs)) / 2
    w, z0, z1 = half * 2.1 + 40.0, high * 0.42, high + half * 0.8 + 30.0
    if t.fx is None:
        t.fx = Mesh(t.name + "_fx", F.o)
    s = 1.0 / F.scale
    t.fx.quad_uv([F.p((cx + w / 2) * s, back * s, z0), F.p((cx - w / 2) * s, back * s, z0),
                  F.p((cx - w / 2) * s, back * s, z1), F.p((cx + w / 2) * s, back * s, z1)],
                 "fx", T.atlas_uv(T.FX["halo"], T.FX_ATLAS))


def towers(coll):
    built = []
    for tid, design, bearing, r, elev, kw in TOWERS:
        kw = dict(kw)
        t = Tower(tid, design, bearing, r, coll, scale=kw.pop("scale", 1.0))
        t.extra = None
        top = snap(height_at(r, elev))
        if "top2_elev" in kw:
            kw["top2"] = snap(height_at(r, kw.pop("top2_elev")))
        if "sign_below" in kw:
            kw["sign_z"] = kw.get("top2", top) - kw.pop("sign_below")
        DESIGNS[design](t, top, **kw)
        crown_halo(t)
        t.done()
        if t.extra is not None:
            t.objects.append(t.extra.done(coll))
        built.append(t)
    return built


# ---------------------------------------------------------------- the far ring, the depths, the sky
def far_towers(coll, rng):
    """Fourteen far towers: cheap silhouettes (about 60 triangles each) in the hazed far texture.
    From the can they stay under the stadium's rim; they are the skyline for the air and the shaft."""
    m = Mesh("city_far_towers")
    spots = []
    for k in range(14):
        b = k * 360.0 / 14 + rng.uniform(-9, 9)
        r = rng.uniform(1350.0, 2100.0)
        F = Frame(b, r, turn=rng.uniform(0, 90))
        w = rng.uniform(90.0, 150.0)
        top = snap(rng.uniform(-260.0, 150.0))
        sides = rng.choice((4, 4, 8))
        shape = (lambda s: rect(w * s, w * s * 0.8)) if sides == 4 else (lambda s: ngon(w * 0.55 * s, 8, math.pi / 8))
        m.begin()
        m.seg([F.ring(shape(1.15), FOOT - 2.0), F.ring(shape(1.15), top - 300.0)], ["base"] * sides)
        m.seg([F.ring(shape(1.0), top - 300.0), F.ring(shape(1.0), top - 90.0), F.ring(shape(0.74), top - 90.0),
               F.ring(shape(0.74), top), F.ring(shape(0.4), top), F.ring(shape(0.4), top + 30.0)], ["far"] * sides)
        m.end()
        m.tube(F.p(0, 0, top + 26.0), F.p(0, 0, top + 110.0), 2.4, 5, 0.5)
        spots.append((F.o.x, F.o.y, w))
    return m.done(coll), spots


def path_points(kind):
    """The rail's two lines, as polylines at rail height."""
    if kind == "loop":
        return [K.polar(LOOP_R, a, -500.0) for a in range(0, 360, 6)]
    d = K.polar(1.0, VIA_BEARING)                                 # the viaduct: a straight line through two gaps in the towers,
    side = K.polar(VIA_SIDE, VIA_BEARING + 90.0)                  # passing 40 m from the axis, so it crosses under the shaft's mouth
    return [Vector((side.x + d.x * s, side.y + d.y * s, -424.0)) for s in range(-1900, 1901, 100)]


def dist_to_path(x, y, pts, closed):
    best = 1e9
    n = len(pts)
    for i in range(n if closed else n - 1):
        a, b = pts[i], pts[(i + 1) % n]
        ab = Vector((b.x - a.x, b.y - a.y))
        tt = max(0.0, min(1.0, (Vector((x - a.x, y - a.y)).dot(ab)) / ab.length_squared))
        best = min(best, math.hypot(x - a.x - ab.x * tt, y - a.y - ab.y * tt))
    return best


def depths(coll, rng, tower_spots):
    """What a player sees down the shaft and past the rim: the floor, low blocks with painted roofs,
    an elevated rail loop and a viaduct on pylons, a train, and three layers of haze."""
    floor = Mesh("city_floor")
    n = 48
    rim = [Vector((2040.0 * math.cos(math.tau * i / n), 2040.0 * math.sin(math.tau * i / n), FOOT)) for i in range(n)]
    f = floor.bm.faces.new([floor.bm.verts.new(p) for p in rim])
    f.material_index = floor._slot("floor")
    for l in f.loops:
        l[floor.uv].uv = (l.vert.co.x / T.FLOOR_SPAN + 0.5, l.vert.co.y / T.FLOOR_SPAN + 0.5)
    floor.done(coll, weld=False)

    loop, via = path_points("loop"), path_points("viaduct")
    blocks = Mesh("city_depths_blocks")
    placed = []
    tries = 0
    while len(placed) < 170 and tries < 6000:
        tries += 1
        r = 120.0 + 1700.0 * rng.random() ** 1.5
        a = rng.uniform(0, 360)
        p = K.polar(r, a)
        w, d = rng.uniform(56, 130), rng.uniform(56, 130)
        reach = max(w, d) * 0.75
        if any(math.hypot(p.x - x, p.y - y) < reach + s for x, y, s in tower_spots):
            continue
        if any(math.hypot(p.x - x, p.y - y) < reach + s + 14.0 for x, y, s in placed):
            continue
        if dist_to_path(p.x, p.y, via, False) < reach + 16.0 or dist_to_path(p.x, p.y, loop, True) < reach + 16.0:
            continue
        # Layers: low round the rotunda, a ridge of taller blocks 300 to 700 m out, low again beyond.
        ridge = math.exp(-((r - 520.0) / 300.0) ** 2)
        h = snap(rng.uniform(70.0, 170.0) + ridge * rng.uniform(40.0, 330.0))
        if r < 330.0:
            h = min(h, 200.0)
        F = Frame(a, r, turn=rng.choice((0.0, 24.0)) - a)
        cut = rng.uniform(0.55, 0.8)
        sx = rng.uniform(0.6, 0.8)
        blocks.begin()
        blocks.seg([F.ring(rect(w, d), FOOT - 2.0), F.ring(rect(w, d), FOOT + h * cut), F.ring(rect(w * sx, d * sx), FOOT + h * cut),
                    F.ring(rect(w * sx, d * sx), FOOT + h)], ["far"] * 4, ledge="roofs")
        blocks.end(top="roofs")
        placed.append((p.x, p.y, reach))
    blocks.done(coll)
    print("DEPTHS %d low blocks" % len(placed))

    rail = Mesh("city_rail")
    for pts, closed, name in ((loop, True, "loop"), (via, False, "viaduct")):
        m = len(pts)
        rings = []
        for i in range(m + (1 if closed else 0)):
            a, c = pts[(i - 1) % m] if (closed or i > 0) else pts[0], pts[(i + 1) % m] if (closed or i < m - 1) else pts[-1]
            p = pts[i % m]
            tdir = Vector((c.x - a.x, c.y - a.y, 0)).normalized()
            side = Vector((tdir.y, -tdir.x, 0))
            rings.append([p + side * 3.0 - Z * 4.0, p + side * 6.0 - Z * 1.0, p + side * 6.0, p - side * 6.0, p - side * 6.0 - Z * 1.0, p - side * 3.0 - Z * 4.0])
        if closed:                                                # a closed loop of girder: no caps, the last ring welds to the first
            rail.begin(); rail.seg(rings, ["metal", "trim.amber", "metal", "trim.amber", "metal", "metal"]); rail._open = rail._first = None
        else:
            rail.solid(rings, ["metal", "trim.amber", "metal", "trim.amber", "metal", "metal"])
        for i in range(0, m, 3 if closed else 2):                 # pylons from under the floor into the girder
            p = pts[i]
            rail.tube((p.x, p.y, FOOT - 2.0), (p.x, p.y, p.z - 2.0), 4.2, 8, 3.0)
    rail.done(coll)

    br = Mesh("city_skybridges")
    for a, c, z in BRIDGES:
        pa, pc = Frame(*PLACE[a]).o, Frame(*PLACE[c]).o
        tdir = (pc - pa).normalized()
        side = Vector((tdir.y, -tdir.x, 0))
        sect = lambda p: [p + side * 5.0 + Z * z, p + side * 5.0 + Z * (z + 6.0), p + Z * (z + 8.2), p - side * 5.0 + Z * (z + 6.0), p - side * 5.0 + Z * z]
        br.solid([sect(pa), sect(pc)], ["bands", "metal", "metal", "bands", "metal"])
        L = (pc - pa).length
        for f in (0.33, 0.67):                                    # two masts through the deck, four stays from each into the deck
            m = pa + tdir * (L * f)
            br.tube(m + Z * (z - 6.0), m + Z * (z + 44.0), 1.6, 6, 0.7)
            br.box(m + Z * (z + 44.6), (1.2, 1.2, 1.2), mat="trim.red")
            for g in (-0.26, -0.13, 0.13, 0.26):
                br.tube(m + Z * (z + 40.0), pa + tdir * (L * (f + g)) + Z * (z + 7.0), 0.45, 5)
    br.done(coll)

    for z, turn in ((-300.0, 0.0), (-470.0, 0.37), (-640.0, 0.71)):
        hz = Mesh("city_haze_%d" % round(-z))
        ring = [Vector((2300.0 * math.cos(math.tau * i / 32), 2300.0 * math.sin(math.tau * i / 32), z)) for i in range(32)]
        f = hz.bm.faces.new([hz.bm.verts.new(p) for p in ring])
        f.material_index = hz._slot("haze")
        for l in f.loops:
            l[hz.uv].uv = (l.vert.co.x / T.TILE_M["haze"] + turn, l.vert.co.y / T.TILE_M["haze"] + turn * 0.6)
        hz.done(coll, weld=False)
    return loop, via


# ---------------------------------------------------------------- sky traffic
def craft_mesh(kind):
    """One craft, nose toward +y, about its own middle. Each is one closed hull plus closed parts."""
    m = Mesh("city_craft_" + kind)

    def sect(y, w, z0, z1, shoulder=0.7):
        return [Vector((-w / 2 * shoulder, y, z0)), Vector((w / 2 * shoulder, y, z0)), Vector((w / 2, y, (z0 + z1) / 2)),
                Vector((w / 2 * shoulder, y, z1)), Vector((-w / 2 * shoulder, y, z1)), Vector((-w / 2, y, (z0 + z1) / 2))]

    if kind == "kotse":                                           # a hover car, 7 m: a wedge, a glass canopy, two pods
        m.solid([sect(-3.5, 2.2, 0.0, 1.0), sect(-2.6, 3.0, -0.3, 1.5), sect(0.6, 3.0, -0.3, 1.7), sect(2.6, 2.4, -0.1, 0.9), sect(3.5, 1.0, 0.1, 0.5)],
                ["metal", "metal", "bands", "metal", "bands", "metal"], top="trim.white", bottom="trim.red",
                span=lambda i, j, mm: "metal" if (i != 1 or j in (0, 1, 3)) else None)
        for sx in (-1, 1):
            m.tube((sx * 1.9, -2.4, -0.2), (sx * 1.9, 1.2, -0.2), 0.55, 6, mat="metal", cap="trim.cyan")
    elif kind == "dyip":                                          # a flying jeepney, 10 m: long hood, boxy cabin, a roof rack
        m.solid([sect(-5.0, 2.6, 0.0, 2.6, 0.9), sect(1.6, 2.6, 0.0, 2.6, 0.9), sect(1.9, 2.4, 0.0, 1.5, 0.9), sect(4.6, 2.2, 0.1, 1.3, 0.85), sect(5.0, 1.8, 0.3, 1.1)],
                ["metal", "metal", "bands", "metal", "bands", "metal"], top="trim.amber", bottom="trim.red",
                span=lambda i, j, mm: "metal" if (i != 0 or j in (0, 1, 3)) else None)
        m.box((0, -1.6, 2.85), (2.2, 5.6, 0.3))
        for sx in (-1, 1):
            m.box((sx * 1.36, -1.6, 0.35), (0.12, 6.4, 0.4), mat="trim.magenta")
            for y in (-3.6, 2.8):
                m.tube((sx * 1.1, y, -0.9), (sx * 1.1, y, 0.2), 0.8, 8, mat="metal", cap="trim.cyan")
    else:                                                         # a cargo barge, 30 m: a flat hull, a cabin, containers
        m.solid([sect(-15.0, 8.0, 0.0, 2.4, 0.85), sect(11.0, 8.0, 0.0, 2.4, 0.85), sect(15.0, 4.0, 0.6, 2.0, 0.8)],
                ["metal"] * 6, top="trim.white", bottom="trim.red")
        m.box((0, 11.0, 3.6), (5.0, 4.0, 3.0), mats={"+y": "bands"})
        for k, y in enumerate((-11.5, -5.0, 1.5)):
            m.box((-1.9, y, 3.6), (3.4, 6.0, 2.6))
            m.box((1.9, y, 3.6 + (1.3 if k == 1 else 0.0)), (3.4, 6.0, 2.6 + (2.6 if k == 1 else 0.0)))
        for sx in (-1, 1):
            m.box((sx * 4.02, -2.0, 1.2), (0.12, 24.0, 0.3), mat="trim.amber")
            for y in (-11.0, 8.0):
                m.tube((sx * 3.0, y, -1.2), (sx * 3.0, y, 0.3), 1.6, 8, mat="metal", cap="trim.cyan")
    bm = m.bm
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0005)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(m.name)
    bm.to_mesh(me); bm.free()
    for kind_ in m.slots:
        me.materials.append(material(kind_))
    return me


def circle_lane(r, z, n=48, clockwise=True):
    pts = [K.polar(r, a * 360.0 / n, z) for a in range(n)]
    return pts if clockwise else pts[::-1]


def traffic(coll, loop, via):
    """Four lanes and the train. A lane is a closed polyline; the craft are placed along it here for the
    pictures and Unity moves them (tools/arena_traffic.json). Lanes keep clear of every tower: both
    orbits run INSIDE the city (every tower stands outside radius 300) and outside the hull's landing
    pads (r 254), above the canopies' sightline."""
    lanes = [
        dict(name="loob", note="inner orbit, clockwise, just over the canopies seen from the can (27 degrees)",
             closed=True, speed=24.0, craft=["kotse", "dyip", "kotse", "kotse", "dyip"], count=9, points=circle_lane(268.0, 140.0)),
        dict(name="labas", note="outer orbit, counter-clockwise, higher (31 degrees)",
             closed=True, speed=18.0, craft=["dyip", "kotse", "barge", "kotse"], count=7, points=circle_lane(284.0, 176.0, clockwise=False)),
        dict(name="ilalim", note="under the hull, a two-way street in the air seen looking down the shaft: eastward 40 m north "
                                 "of the axis, back westward 24 m south of it, turning round far out in the haze",
             closed=True, speed=34.0, craft=["kotse", "kotse", "dyip"], count=12,
             points=[Vector((x, 40.0, -190.0)) for x in range(-1250, 1251, 250)]
                    + [Vector((x, -24.0, -190.0)) for x in range(1250, -1251, -250)]),
        dict(name="taas", note="far and high over the towers, barges, clockwise (29 degrees)",
             closed=True, speed=11.0, craft=["barge"], count=8, points=circle_lane(1060.0, 590.0, 72)),
    ]
    meshes = {k: craft_mesh(k) for k in ("kotse", "dyip", "barge")}
    rng = random.Random(41)
    out = []
    for lane in lanes:
        pts = lane["points"]
        n = len(pts)
        segs = [(pts[i], pts[(i + 1) % n]) for i in range(n if lane["closed"] else n - 1)]
        total = sum((b - a).length for a, b in segs)
        phases = []
        for k in range(lane["count"]):
            s = ((k + rng.uniform(-0.3, 0.3)) / lane["count"]) % 1.0
            phases.append(round(s, 4))
            dist = s * total
            for a, b in segs:
                L = (b - a).length
                if dist <= L:
                    break
                dist -= L
            p = a + (b - a) * (dist / L)
            kind = lane["craft"][k % len(lane["craft"])]
            ob = bpy.data.objects.new("city_craft_%s_%s_%02d" % (kind, lane["name"], k), meshes[kind])
            ob.location = p
            ob.rotation_euler = (0, 0, -math.atan2((b - a).x, (b - a).y))
            coll.objects.link(ob)
        out.append(dict(name=lane["name"], note=lane["note"], closed=lane["closed"], speed_mps=lane["speed"], length_m=round(total, 1),
                        craft=lane["craft"], count=lane["count"], phase=phases,
                        points_blender=[[round(p.x, 2), round(p.y, 2), round(p.z, 2)] for p in pts],
                        points_unity=[[round(-p.x, 2), round(p.z, 2), round(-p.y, 2)] for p in pts]))
    # The train on the rail loop: six cars as one object, its origin on the rail under the first car.
    tr = Mesh("city_train")
    for k in range(6):
        a0 = k * 4.6
        p = K.polar(LOOP_R, a0 + 2.0, -500.0)
        tdir = K.polar(1.0, a0 + 2.0 + 90.0)
        tr.box(p + Z * 2.2, (24.0, 5.0, 4.6), Vector((tdir.x, tdir.y, 0)), mats={"-y": "bands", "+y": "bands", "+x": "trim.white", "-x": "trim.red"})
    tr.done(coll)
    out.append(dict(name="tren", note="the train on the rail loop, 500 m below the can; city_train is built at phase 0",
                    closed=True, speed_mps=30.0, length_m=round(math.tau * LOOP_R, 1), craft=["city_train"], count=1, phase=[0.0],
                    points_blender=[[round(p.x, 2), round(p.y, 2), round(p.z, 2)] for p in loop],
                    points_unity=[[round(-p.x, 2), round(p.z, 2), round(-p.y, 2)] for p in loop]))
    data = dict(about="Sky traffic for the arena's city kit. Written by tools/author_arena_city.py; do not edit by hand.",
                units="metres", frames="points_blender is (x east, y north, z up); points_unity is (-x, z, -y), what glTFast gives",
                models={"kotse": "city_craft_kotse, 7 m, nose +y in Blender", "dyip": "city_craft_dyip, 10 m", "barge": "city_craft_barge, 30 m"},
                how="A craft sits at fraction (phase + time * speed_mps / length_m) mod 1 along its lane's polyline, nose along the travel. "
                    "Every lane is a closed loop: the last point joins the first.",
                lanes=out)
    with open(os.path.join(ROOT, "tools", "arena_traffic.json"), "w", encoding="utf-8") as fh:
        json.dump(data, fh, indent=1)
    return out


def sky_dome(coll):
    """Review only: the painted panorama on a dome, u = bearing / 360."""
    me = bpy.data.meshes.new("review sky dome")
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    R, nu, nv = 9000.0, 48, 24
    for i in range(nu):
        for j in range(nv):
            quad = []
            for di, dj in ((0, 0), (1, 0), (1, 1), (0, 1)):
                b, e = (i + di) * 360.0 / nu, -90.0 + (j + dj) * 180.0 / nv
                p = K.polar(R * math.cos(math.radians(e)), b, R * math.sin(math.radians(e)))
                quad.append((bm.verts.new(p), ((i + di) / nu, (j + dj) / nv)))
            try:
                f = bm.faces.new([q[0] for q in quad])
            except ValueError:
                continue
            for l, (_, c) in zip(f.loops, quad):
                l[uv].uv = c
    bm.to_mesh(me); bm.free()
    m = bpy.data.materials.new("review_sky")
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.remove(nt.nodes.get("Principled BSDF"))
    em = nt.nodes.new("ShaderNodeEmission")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(os.path.join(TEX, "arena_city_sky.png"), check_existing=True)
    nt.links.new(tex.outputs["Color"], em.inputs["Color"])
    nt.links.new(em.outputs[0], nt.nodes["Material Output"].inputs["Surface"])
    m.diffuse_color = (0.03, 0.04, 0.12, 1)
    me.materials.append(m)
    ob = bpy.data.objects.new("review sky dome", me)
    coll.objects.link(ob)
    return ob


# ---------------------------------------------------------------- the SIGHT report
def stadium_rim(b):
    """arena_kit.rim_elevation, plus what it leaves out: the stair tower closing each end of an upper
    stand (a drum 4.2 m in radius at r 160, 1.2 degrees outside the stand, 60 m tall), which fills
    most of the slot between a stand and its corner screen."""
    e = K.rim_elevation(b)
    for lo, hi in K.UPPER_ARCS:
        for a in (lo - 1.2, hi + 1.2):
            if abs(((b - a + 180.0) % 360.0) - 180.0) <= math.degrees(math.atan2(4.2, 160.0)):
                e = max(e, math.degrees(math.atan2(60.0 - EYE, 160.0 - 4.2)))
    return e


def sight(built):
    """From the player's eye by the can: every tower's outline is sampled along its edges into half
    degree columns; a column shows what stands above the stadium's rim there."""
    eye = Vector((0, 0, EYE))
    sky = {}
    rows = []
    for t in built:
        cols = {}
        for ob in t.objects:
            if ob.name.endswith("_fx"):
                continue
            me = ob.data
            for e in me.edges:
                a, b = me.vertices[e.vertices[0]].co, me.vertices[e.vertices[1]].co
                if max(a.z, b.z) < 0.0:
                    continue
                for k in range(9):
                    p = a + (b - a) * (k / 8.0) - eye
                    bear = math.degrees(math.atan2(p.x, p.y)) % 360.0
                    el = math.degrees(math.atan2(p.z, math.hypot(p.x, p.y)))
                    c = round(bear * 2) / 2 % 360.0
                    cols[c] = max(cols.get(c, -90.0), el)
        shown = {c: el - stadium_rim(c) for c, el in cols.items() if el > stadium_rim(c)}
        top = max(cols.values())
        width = len(shown) * 0.5
        rows.append((t, top, max(shown.values()) if shown else 0.0, width))
        for c, el in cols.items():
            if el > sky.get(c, (-90.0, ""))[0]:
                sky[c] = (el, t.name[5:8])
    print("SIGHT from the can (eye z %.1f). The stadium hides the sky up to %.1f degrees over a canopy, %.1f in a corner behind the screen."
          % (EYE, K.rim_elevation(0), K.rim_elevation(45)))
    print("SIGHT %-22s %7s %6s %9s %9s %9s" % ("tower", "bearing", "dist", "top deg", "shows deg", "width deg"))
    for t, top, show, width in rows:
        flag = "" if show >= 2.0 else "   <-- HIDDEN OR BARELY SEEN"
        print("SIGHT %-22s %7.1f %6.0f %9.1f %9.1f %9.1f%s" % (t.name[5:], t.F.bearing, t.F.r, top, show, width, flag))
    seen = [r for r in rows if r[2] >= 2.0]
    print("SIGHT %d of %d towers show 2 degrees or more above the stadium; they show %.1f to %.1f degrees."
          % (len(seen), len(rows), min(r[2] for r in seen), max(r[2] for r in seen)))
    # The rhythm: one character per 3 degrees of bearing, the height shown above the rim.
    line, names = "", ""
    open_sky = 0
    for c in range(0, 360, 3):
        best = max(((sky.get((c + d / 2.0) % 360.0, (-90.0, ""))[0] - stadium_rim(c + d / 2.0)) for d in range(6)), default=-90)
        line += " " if best < 0.5 else ".:-=+*#%@"[min(8, int(best / 2.0))]
        open_sky += best < 0.5
    print("SIGHT skyline round the compass, 3 degrees a character from north clockwise (space is open sky, . to @ is 0 to 16+ degrees shown):")
    print("SIGHT N" + " " * 29 + "E" + " " * 29 + "S" + " " * 29 + "W")
    print("SIGHT " + line)
    print("SIGHT corners " + "".join("^" if any(abs(((c - k + 180) % 360) - 180) <= 13 for k in (45, 135, 225, 315)) else " " for c in range(0, 360, 3)))
    print("SIGHT open sky: %d of 360 degrees" % (open_sky * 3))
    for name, bearing, r, z, w, h in SIGN_LOG:
        el = math.degrees(math.atan2(z - EYE, r))
        print("SIGHT sign %-10s bearing %5.1f  centre %4.1f deg (rim %4.1f)  %2.0f x %2.0f m  = %.1f x %.1f deg"
              % (name, bearing, el, stadium_rim(bearing), w, h, math.degrees(w / r), math.degrees(h / r)))


def report(coll):
    """Open edges (a closed solid has none) and triangles for every mesh in the kit."""
    total = 0
    seen = set()
    bad = 0
    open_by_design = ("city_floor", "city_haze", "_fx")   # flat cards and holograms: light, not solids
    for ob in sorted(coll.objects, key=lambda o: o.name):
        if ob.type != "MESH":
            continue
        bm = bmesh.new(); bm.from_mesh(ob.data)
        op = sum(1 for e in bm.edges if len(e.link_faces) == 1)
        multi = sum(1 for e in bm.edges if len(e.link_faces) > 2)
        tris = sum(len(f.verts) - 2 for f in bm.faces)
        bm.free()
        total += tris
        if ob.data.name in seen and "craft" in ob.name:
            continue
        seen.add(ob.data.name)
        note = ""
        if (op or multi) and not any(k in ob.name for k in open_by_design):
            note = "   <-- OPEN %d, SHARED BY MORE THAN TWO FACES %d" % (op, multi)
            bad += 1
        elif op:
            note = "   (a flat card, open by design)"
        print("MESH %-34s tris %6d%s" % (ob.name[:34], tris, note))
    print("TOTAL TRIANGLES %d in %d objects, %d materials; %d meshes with open edges that should be closed"
          % (total, len(coll.objects), len(_mats), bad))
    return total


# ---------------------------------------------------------------- the review scene and its pictures
def backdrop():
    """The blockout's stadium as a plain dark backdrop: review only, never saved into the kit."""
    import author_arena_stadium as S
    c = K.collection("review stadium backdrop (not the kit)")
    fx = K.collection("review stadium beams (hidden)")
    S.stage(c); S.field(c); S.lower_bowl(c); S.upper_stands(c); S.canopies(c, fx); S.screens(c); S.hull(c)
    dark = bpy.data.materials.new("review_backdrop")
    dark.use_nodes = True
    dark.diffuse_color = (0.07, 0.08, 0.12, 1)
    bs = dark.node_tree.nodes.get("Principled BSDF")
    bs.inputs["Base Color"].default_value = (0.05, 0.055, 0.085, 1)
    bs.inputs["Roughness"].default_value = 0.9
    for ob in list(c.objects) + list(fx.objects):
        if ob.type == "MESH":
            ob.data.materials.clear()
            ob.data.materials.append(dark)
            for mod in list(ob.modifiers):
                ob.modifiers.remove(mod)
    for ob in fx.objects:
        ob.hide_render = True; ob.hide_viewport = True
    return c


def shoot(name, version, loc, target, lens=35.0, size=(1920, 1080)):
    scene = bpy.context.scene
    cam_data = bpy.data.cameras.new(name)
    cam = bpy.data.objects.new("cam " + name, cam_data)
    scene.collection.objects.link(cam)
    cam.location = loc
    cam.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
    cam_data.lens = lens; cam_data.clip_start = 0.5; cam_data.clip_end = 20000
    scene.camera = cam
    scene.render.resolution_x, scene.render.resolution_y = size
    tag = ("check_" if CHECKER else "") + name
    scene.render.filepath = os.path.join(LOGS, "city_%s_%s.png" % (tag, version))
    bpy.ops.render.render(write_still=True)


def pictures(version, built, shots):
    by = {t.name[5:8]: t for t in built}
    first = {}
    for t in built:
        first.setdefault(t.name.split("_", 2)[2], t)
    want = lambda k: (not shots) or k in shots
    if want("eye"):
        for b in (0, 45, 90, 135, 180, 225, 270, 315):
            tgt = K.polar(100.0, b, EYE + 38.0)
            shoot("eye_%03d" % b, version, (0, 0, EYE), tgt, lens=14)
    if want("game"):                                              # a narrower, flatter look: closer to a play camera
        for b in (45, 225, 0):
            tgt = K.polar(100.0, b, EYE + 24.0)
            shoot("game_%03d" % b, version, K.polar(8.0, b + 180.0, EYE + 1.0), tgt, lens=22)
    if want("turf"):
        shoot("turf_045", version, (-40, -40, 1.7), K.polar(100.0, 45, 30.0), lens=16)
        shoot("turf_250", version, (50, 20, 1.7), K.polar(100.0, 250, 34.0), lens=16)
    if want("shaft"):
        shoot("shaft_down", version, K.polar(17.0, 100.0, EYE), (0.0, 6.0, -760.0), lens=16)
        shoot("shaft_slant", version, K.polar(21.0, 280.0, EYE), K.polar(260.0, 100.0, -500.0), lens=20)
    if want("air"):
        shoot("air_free", version, K.polar(1250.0, 192.0, 760.0), (0, 0, -60.0), lens=30)
        shoot("air_side", version, K.polar(1050.0, 358.0, 190.0), (0, 0, 60.0), lens=28)
        shoot("air_top", version, K.polar(300.0, 180.0, 2300.0), (0, 0, 0.0), lens=30)
        shoot("air_under", version, K.polar(700.0, 262.0, -330.0), (0, 0, -120.0), lens=22)
    crafts = [ob for ob in bpy.data.objects if ob.name.startswith("city_craft")]
    for ob in crafts:                                             # the close pictures are of the towers: no craft passing the lens
        ob.hide_render = True
    if want("close"):
        for design, t in first.items():
            F = t.F
            top = max(v.co.z for ob in t.objects for v in ob.data.vertices)
            roof = snap(height_at(F.r, next(e for tid, d, b, r, e, kw in TOWERS if tid == t.name[5:8])))
            shoot("close_%s_body" % design, version, F.p(-70.0, -250.0, roof - 150.0), F.p(0, 0, roof - 110.0), lens=30, size=(1280, 720))
            shoot("close_%s_crown" % design, version, F.p(55.0, -170.0, roof + 40.0), F.p(0, 0, (roof + top) / 2 - 8.0), lens=32, size=(1280, 720))
    if want("sign"):
        for name, bearing, r, z, w, h in SIGN_LOG:
            F = Frame(bearing, r)
            shoot("sign_%s" % name, version, F.p(max(w, h) * 0.5, -max(w, h) * 2.0 - 60.0, z - max(w, h) * 0.3), F.p(0, -30.0, z), lens=40, size=(1280, 720))
    for ob in crafts:
        ob.hide_render = False
    if want("flat"):
        scene = bpy.context.scene
        engine = scene.render.engine
        scene.render.engine = "BLENDER_WORKBENCH"
        scene.display.shading.light = "STUDIO"; scene.display.shading.color_type = "MATERIAL"
        scene.display.shading.show_cavity = True
        for key, tid, off in (("singsing", "T01", (110.0, -260.0, 60.0)), ("haligi", "T04", (100.0, -250.0, -20.0)),
                              ("balangkas", "T10", (90.0, -210.0, -30.0)), ("hagdan", "T14", (120.0, -230.0, 10.0)),
                              ("korona", "T12", (100.0, -230.0, 90.0)), ("talim", "T13", (100.0, -220.0, -60.0))):
            t = by[tid]
            roof = snap(height_at(t.F.r, next(e for i, d, b, r, e, kw in TOWERS if i == tid)))
            shoot("flat_%s" % key, version, t.F.p(off[0], off[1], roof + off[2]), t.F.p(0, 0, roof - 40.0), lens=30, size=(1280, 720))
        shoot("flat_air", version, K.polar(1250.0, 192.0, 760.0), (0, 0, -60.0), lens=30)
        scene.render.engine = engine


def main():
    global CHECKER
    version, shots, render = "v1", [], True
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    for a in args:
        if a.startswith("--version="):
            version = a.split("=", 1)[1]
        elif a.startswith("--shots="):
            shots = a.split("=", 1)[1].split(",")
        elif a == "--no-render":
            render = False
        elif a == "--checker":
            CHECKER = True
    bpy.ops.wm.read_factory_settings()
    for ob in list(bpy.data.objects):
        bpy.data.objects.remove(ob)
    scene = bpy.context.scene
    coll = K.collection("arena_city")
    rng = random.Random(1005)
    built = towers(coll)
    spots = [(t.F.o.x, t.F.o.y, 95.0) for t in built]
    far, far_spots = far_towers(coll, rng)
    loop, via = depths(coll, rng, spots + [(x, y, w) for x, y, w in far_spots])
    traffic(coll, loop, via)
    for name, pts, closed in (("rail loop", loop, True), ("viaduct", via, False)):          # measured to every tower's FOOT, not its axis
        worst = (1e9, "")
        for t in built:
            for ob in t.objects:
                if ob.name.endswith("_fx"):
                    continue
                for v in ob.data.vertices:
                    if v.co.z < -380.0:
                        worst = min(worst, (dist_to_path(v.co.x, v.co.y, pts, closed), t.name[5:8]))
        print("CLEAR %s (12 m wide) passes no nearer than %.0f m to a tower's foot (%s)%s" % (name, worst[0], worst[1], "   <-- TOO NEAR" if worst[0] < 14.0 else ""))
    inner = min((math.hypot(v.co.x, v.co.y), t.name[5:8]) for t in built for ob in t.objects if not ob.name.endswith("_fx") for v in ob.data.vertices)
    print("CLEAR nothing of a tower is nearer the can than %.0f m (%s)%s" % (inner[0], inner[1], "   <-- INSIDE 300" if inner[0] < 300.0 else ""))
    total = report(coll)
    sight(built)

    review = K.collection("review (not the kit)")
    sky_dome(review)
    moon = bpy.data.objects.new("review moonlight", bpy.data.lights.new("review moonlight", "SUN"))
    moon.data.energy = 2.4; moon.data.color = (0.62, 0.72, 1.0); moon.data.angle = math.radians(20)
    moon.data.use_shadow = False
    moon.rotation_euler = (math.radians(52), math.radians(10), math.radians(200))
    review.objects.link(moon)
    world = bpy.data.worlds.new("night"); scene.world = world; world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.06, 0.07, 0.19, 1)
    world.node_tree.nodes["Background"].inputs[1].default_value = 1.0
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    scene.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
    scene.view_settings.view_transform = "Standard"
    for screen in bpy.data.screens:
        for area in screen.areas:
            for space in area.spaces:
                if space.type == "VIEW_3D":
                    space.clip_start = 1.0; space.clip_end = 20000.0
    os.makedirs(KITS, exist_ok=True); os.makedirs(LOGS, exist_ok=True)
    if not CHECKER:                                               # the kit is saved before the backdrop exists: it never holds the stadium
        scene.render.use_file_extension = True
        bpy.context.preferences.filepaths.save_version = 0
        bpy.ops.wm.save_as_mainfile(filepath=os.path.join(KITS, "city.blend"))
        bpy.ops.file.make_paths_relative()
        bpy.ops.wm.save_mainfile()
    if render:
        backdrop()
        pictures(version, built, shots)
    print("CITY_OK %s triangles %d" % (version, total))


if __name__ == "__main__":
    main()
