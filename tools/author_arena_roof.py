"""The arena's ROOF kit (ARENA-1.4): everything over the stands and over the can.

    blender -b --python tools/author_arena_roof.py -- --version=v1 [--shots=all|none]

Read docs/ARENA_ART_BRIEF.md first. This builds ArtSource/arena/kits/roof.blend, every object in
the collection `arena_roof`, already in the stadium's frame (metres, z up, y north, the can at
the origin), and writes tools/arena_lights.json (key "roof") and the review pictures in
Logs/arena/roof/. The textures are painted by tools/author_arena_textures_roof.py; run that first.

WHAT IS IN THE KIT
  * four CANOPIES, one over each upper stand: a wing-section roof plate (ONE closed profile swept
    round: a dark nose with the LED strip and a front gutter, a glazed leading band, a solid
    sheet with glazed skylight bays, a rear gutter, a rear edge with its own LED line, and a box
    ring girder at the mast line), and under it the structure a player sees from the stage:
    seven cantilever trusses and eight plate ribs, five lines of purlins, a hung catwalk;
  * the MASTS: six a canopy at r 184.5, from a plinth on the hull deck up THROUGH the ring girder
    and the roof to a head at z 90, forestays and a backstay down to the roof, a raking back leg
    to the deck, ties to the stand's wall, ring tubes and cross cables between masts;
  * FLOODLIGHT BANKS on the front edge (six a canopy), line-array SPEAKERS and CAMERA heads;
  * the centre-hung SCOREBOARD: an eight-faced drum, its screens leaning to the stage, between a
    white and a deep blue LED ribbon, a rigging frame on top, a glowing emitter underneath, four
    cables to the canopies;
  * four CORNER SCREENS on braced masts, with a real back (girts, stiffeners, service cabinets,
    a catwalk);
  * the HOST'S BOOTH on the north concourse.

THE RULES THIS FILE KEEPS (the brief's): every solid is closed (report() counts open edges);
no two faces share a plane (coplanar() looks for it); every member is built between the two
things it joins and ends inside both; the logo is a cell CUT INTO the screen's face, never a
quad laid over it. UVs are in metres (16 m a tile) and are written with the faces, so texel
density is even by construction; the roof sheet alone is mapped by bay (one tile a bay, its
seams running down the slope like a real ring roof's tapered sheets, at most 12 % off square).
"""
import bpy, bmesh, json, math, os, sys
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import arena_kit as K                                                # noqa: E402
from arena_kit import CANOPY, DECK_Z, EYE, ROOT, UPPER_ARCS, polar   # noqa: E402

TEX = os.path.join(ROOT, "Assets", "TumbangPreso", "Art", "Arena", "Textures")
LOGS = os.path.join(ROOT, "Logs", "arena", "roof")
KITS = os.path.join(K.OUT, "kits")
LIGHTS_JSON = os.path.join(HERE, "arena_lights.json")
Z = Vector((0, 0, 1))
TILE = 16.0                       # metres per texture tile
PLATE_V = 14.8                    # the roof sheet: one tile down the slope
SURFACES = ["skin", "glazing", "steel", "housing", "lamp", "led", "screen", "logo", "booth_wall", "booth_glass"]
MI = {s: i for i, s in enumerate(SURFACES)}
FLAT = {"skin": (0.045, 0.060, 0.110), "glazing": (0.10, 0.22, 0.30), "steel": (0.26, 0.31, 0.44),
        "housing": (0.035, 0.040, 0.065), "lamp": (0.95, 0.97, 1.0), "led": (0.05, 0.12, 0.75),
        "screen": (0.01, 0.012, 0.03), "logo": (0.9, 0.45, 0.15), "booth_wall": (0.22, 0.26, 0.38),
        "booth_glass": (0.55, 0.42, 0.20)}
GLOW = {"glazing": 1.0, "lamp": 8.0, "led": 1.6, "screen": 1.2, "logo": 1.2, "booth_glass": 1.6}
LED_BANDS = {"white": (0.80, 0.95), "blue": (0.55, 0.70), "cyan": (0.30, 0.45), "gold": (0.05, 0.20)}
MATS = {}
LIGHTS = {"floodlight_banks": [], "house_lights": [], "booth_lights": [], "mast_beacons": [], "led_strips": [], "other": []}
NAMES = {0: "north", 90: "east", 180: "south", 270: "west"}

# ---------------------------------------------------------------- the canopy's section
R0, R1 = CANOPY                                                   # 150 and 190
MAST_R = 184.5
MAST_REL = (-29.5, -19.5, -9.5, 9.5, 19.5, 29.5)                  # bearings from the stand's middle
TRUSS_REL = (-29.5, -19.5, -9.5, 0.0, 9.5, 19.5, 29.5)            # a truss at every mast and in the middle
PLATE_REL = (-24.5, -14.5, -4.75, 4.75, 14.5, 24.5)               # a plate rib between trusses
END_REL = (-31.7, 31.7)                                           # a closing rib inside each end
MAIN_REL = sorted(TRUSS_REL + PLATE_REL)                          # thirteen ribs, twelve bays
ALL_REL = sorted(MAIN_REL + list(END_REL))
HALF = 32.0
PURLINS = (156.0, 162.0, 168.0, 174.0, 180.0)
CATWALK_R = 165.0
TOP = [(152.3, 69.98), (154.5, 70.6), (159.0, 71.2), (162.0, 71.1), (174.0, 70.35), (184.6, 69.4)]
MAST_HEAD = 90.0


def interp(pts, x):
    if x <= pts[0][0]:
        (x0, y0), (x1, y1) = pts[0], pts[1]
    elif x >= pts[-1][0]:
        (x0, y0), (x1, y1) = pts[-2], pts[-1]
    else:
        for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
            if x0 <= x <= x1:
                break
    return y0 + (y1 - y0) * (x - x0) / (x1 - x0)


def top(r):
    return interp(TOP, r)


def under(r):                                                     # the roof plate's underside
    return top(r) - 0.45


def zb(r):                                                        # the trusses' bottom chord
    return 68.55 - 0.1125 * (r - 151.8)


def tang(a):                                                      # the way a bearing grows
    return Vector((math.cos(math.radians(a)), -math.sin(math.radians(a)), 0.0))


def rad(a):
    return polar(1.0, a)


def bay_u(rel):
    """Where a bearing (from the stand's middle) falls among the ribs: rib k is u = k."""
    nodes = [(-HALF, -0.5)] + [(b, float(k)) for k, b in enumerate(MAIN_REL)] + [(HALF, len(MAIN_REL) - 0.5)]
    return interp(nodes, rel)


def bay_glazed(rel):
    b = math.floor(bay_u(rel))
    return 0 <= b <= 11 and min(b, 11 - b) % 2 == 1


# ---------------------------------------------------------------- materials
def materials(mode="paint"):
    for s in SURFACES:
        name = "arena_roof_%s" % s
        m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
        m.use_nodes = True
        nt = m.node_tree
        nt.nodes.clear()
        out = nt.nodes.new("ShaderNodeOutputMaterial")
        b = nt.nodes.new("ShaderNodeBsdfPrincipled")
        nt.links.new(b.outputs["BSDF"], out.inputs["Surface"])
        b.inputs["Roughness"].default_value = 0.85
        if "Specular IOR Level" in b.inputs:
            b.inputs["Specular IOR Level"].default_value = 0.15
        c = FLAT[s]
        m.diffuse_color = (c[0], c[1], c[2], 1.0)
        b.inputs["Base Color"].default_value = (c[0], c[1], c[2], 1.0)
        if mode == "checker":
            tex = nt.nodes.new("ShaderNodeTexImage")
            tex.image = bpy.data.images.load(os.path.join(LOGS, "checker.png"), check_existing=True)
            nt.links.new(tex.outputs["Color"], b.inputs["Base Color"])
            nt.links.new(tex.outputs["Color"], b.inputs["Emission Color"])
            b.inputs["Emission Strength"].default_value = 0.5
            nt.nodes.active = tex
        else:
            tex = nt.nodes.new("ShaderNodeTexImage")
            tex.image = bpy.data.images.load(os.path.join(TEX, name + ".png"), check_existing=True)
            nt.links.new(tex.outputs["Color"], b.inputs["Base Color"])
            if s in GLOW:
                glow = nt.nodes.new("ShaderNodeTexImage")
                glow.image = bpy.data.images.load(os.path.join(TEX, name + "_emit.png"), check_existing=True)
                nt.links.new(glow.outputs["Color"], b.inputs["Emission Color"])
                b.inputs["Emission Strength"].default_value = GLOW[s]
            nt.nodes.active = tex
        MATS[s] = m


# ---------------------------------------------------------------- mesh building, UVs written with the faces
class Mesh:
    count = 0

    def __init__(self):
        self.bm = bmesh.new()
        self.uvl = self.bm.loops.layers.uv.new("UVMap")

    def v(self, p):
        return self.bm.verts.new(p)

    def f(self, verts, mat, uvs):
        face = self.bm.faces.new(verts)
        face.material_index = MI[mat]
        for loop, uv in zip(face.loops, uvs):
            loop[self.uvl].uv = uv
        return face

    def off(self):                                                # a different place on the tile for every solid
        Mesh.count += 1
        return ((Mesh.count * 0.371) % 1.0, (Mesh.count * 0.613) % 1.0)

    def done(self, name, coll):
        bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces)
        used = sorted({f.material_index for f in self.bm.faces})
        remap = {old: new for new, old in enumerate(used)}
        for f in self.bm.faces:
            f.material_index = remap[f.material_index]
        me = bpy.data.meshes.new(name)
        self.bm.to_mesh(me)
        self.bm.free()
        for i in used:
            me.materials.append(MATS[SURFACES[i]])
        ob = bpy.data.objects.new(name, me)
        coll.objects.link(ob)
        return ob


def spindle(M, stations, sides=8, mat="steel"):
    """A closed round member along a straight line: stations are (point, radius)."""
    stations = [(Vector(p), r) for p, r in stations]
    ax = (stations[-1][0] - stations[0][0]).normalized()
    ref = Z if abs(ax.z) < 0.9 else Vector((1, 0, 0))
    u = ax.cross(ref).normalized()
    v = ax.cross(u)
    ou, ov = M.off()
    rings = [[M.v(p + (u * math.cos(2 * math.pi * i / sides) + v * math.sin(2 * math.pi * i / sides)) * r)
              for i in range(sides)] for p, r in stations]
    along = 0.0
    for s in range(len(stations) - 1):
        (p0, r0), (p1, r1) = stations[s], stations[s + 1]
        seg = (p1 - p0).length
        girth = 2 * math.pi * (r0 + r1) / 2
        for i in range(sides):
            k = (i + 1) % sides
            ua, ub = ou + girth * i / sides / TILE, ou + girth * (i + 1) / sides / TILE
            M.f((rings[s][i], rings[s][k], rings[s + 1][k], rings[s + 1][i]), mat,
                [(ua, ov + along / TILE), (ub, ov + along / TILE), (ub, ov + (along + seg) / TILE), (ua, ov + (along + seg) / TILE)]).smooth = True
        along += seg
    for ring, r, flip in ((rings[0], stations[0][1], True), (rings[-1], stations[-1][1], False)):
        uvs = [(ou + r * math.cos(2 * math.pi * i / sides) / TILE, ov + r * math.sin(2 * math.pi * i / sides) / TILE) for i in range(sides)]
        M.f(ring[::-1] if flip else ring, mat, uvs[::-1] if flip else uvs)


def tube(M, p0, p1, r, sides=6, r1=None, mat="steel"):
    spindle(M, [(p0, r), (p1, r if r1 is None else r1)], sides, mat)


def polybeam(M, pts, w, d, side, mat="steel"):
    """A closed rectangular member along a polyline lying in the plane across `side`."""
    pts = [Vector(p) for p in pts]
    side = Vector(side).normalized()
    ou, ov = M.off()
    secs = []
    for i, p in enumerate(pts):
        dirs = []
        if i > 0:
            dirs.append((p - pts[i - 1]).normalized())
        if i < len(pts) - 1:
            dirs.append((pts[i + 1] - p).normalized())
        ns = [t.cross(side).normalized() for t in dirs]
        n = sum(ns, Vector()).normalized()
        n = n / max(0.5, n.dot(ns[0]))
        secs.append([M.v(p + side * (sx * w / 2) + n * (sy * d / 2)) for sx, sy in ((-1, -1), (1, -1), (1, 1), (-1, 1))])
    per = (w, d, w, d)
    along = 0.0
    for s in range(len(pts) - 1):
        seg = (pts[s + 1] - pts[s]).length
        u = ou
        for i in range(4):
            k = (i + 1) % 4
            M.f((secs[s][i], secs[s][k], secs[s + 1][k], secs[s + 1][i]), mat,
                [(u, ov + along / TILE), (u + per[i] / TILE, ov + along / TILE),
                 (u + per[i] / TILE, ov + (along + seg) / TILE), (u, ov + (along + seg) / TILE)])
            u += per[i] / TILE
        along += seg
    cap = [(ou, ov), (ou + w / TILE, ov), (ou + w / TILE, ov + d / TILE), (ou, ov + d / TILE)]
    M.f(secs[0][::-1], mat, cap[::-1])
    M.f(secs[-1], mat, cap)


def beam(M, p0, p1, w, d, mat="steel", up=None):
    """A closed rectangular member from p0 to p1: w across (level by default), d the other way."""
    p0, p1 = Vector(p0), Vector(p1)
    ax = (p1 - p0).normalized()
    upv = Vector(up) if up is not None else Z
    if abs(ax.dot(upv.normalized())) > 0.98:
        upv = Vector((1, 0, 0)) if abs(ax.x) < 0.9 else Vector((0, 1, 0))
    polybeam(M, [p0, p1], w, d, ax.cross(upv), mat)


def obox(M, centre, a1, a2, a3, s1, s2, s3, mat="housing"):
    """A closed box on its own axes (unit vectors a1, a2, a3; sizes s1, s2, s3)."""
    c = Vector(centre)
    ax = [Vector(a1).normalized() * (s1 / 2), Vector(a2).normalized() * (s2 / 2), Vector(a3).normalized() * (s3 / 2)]
    size = (s1, s2, s3)
    ou, ov = M.off()
    vs = {}
    for i in (-1, 1):
        for j in (-1, 1):
            for k in (-1, 1):
                vs[(i, j, k)] = M.v(c + ax[0] * i + ax[1] * j + ax[2] * k)
    for fixed in range(3):
        o1, o2 = [x for x in range(3) if x != fixed]
        for sgn in (-1, 1):
            quad, uvs = [], []
            for da, db in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
                key = [0, 0, 0]
                key[fixed], key[o1], key[o2] = sgn, da, db
                quad.append(vs[tuple(key)])
                uvs.append((ou + (da + 1) / 2 * size[o1] / TILE, ov + (db + 1) / 2 * size[o2] / TILE))
            M.f(quad, mat, uvs)


def prism(M, poly, origin, A, B, E, e0, e1, mat="housing", edge_mat=None, edge_uv=None):
    """A closed extrusion: `poly` is (a, b) in the plane of A and B, pushed from e0 to e1 along E."""
    o, A, B, E = Vector(origin), Vector(A), Vector(B), Vector(E)
    ou, ov = M.off()
    lo = [M.v(o + A * a + B * b + E * e0) for a, b in poly]
    hi = [M.v(o + A * a + B * b + E * e1) for a, b in poly]
    n = len(poly)
    per = 0.0
    for i in range(n):
        k = (i + 1) % n
        seg = math.hypot(poly[k][0] - poly[i][0], poly[k][1] - poly[i][1])
        uvs = [(ou + per / TILE, ov), (ou + (per + seg) / TILE, ov), (ou + (per + seg) / TILE, ov + (e1 - e0) / TILE), (ou + per / TILE, ov + (e1 - e0) / TILE)]
        if edge_uv and i in edge_uv:
            uvs = edge_uv[i]
        M.f((lo[i], lo[k], hi[k], hi[i]), (edge_mat or {}).get(i, mat), uvs)
        per += seg
    uvs = [(ou + a / TILE, ov + b / TILE) for a, b in poly]
    M.f(lo[::-1], mat, uvs[::-1])
    M.f(hi, mat, uvs)


def sweep(M, profile, angles, paint, cap="steel", origin=(0.0, 0.0, 0.0), full=False, ufun=None):
    """ONE closed solid: the closed loop `profile` of (r, z, tag) swept through `angles` (degrees,
    0 north, clockwise) about `origin`. It shades smooth round the sweep and creased at the
    profile's corners, so a curved roof is a curve and not a row of facets. The tag names the strip from that point to the next;
    `paint(tag, column, a0, a1)` answers (material, kind): kind "world" maps metres, "plate" maps
    the roof sheet by bay through `ufun`, an LED band's name picks that band of the LED atlas,
    "bglass" is the booth's window band. A partial sweep is capped at both ends."""
    o = Vector(origin)
    n = len(profile)
    cum = [0.0]
    for j in range(n):
        (ra, za), (rb, zb_) = profile[j][:2], profile[(j + 1) % n][:2]
        cum.append(cum[-1] + math.hypot(rb - ra, zb_ - za))
    axis, cols = {}, []
    for a in angles:
        col = []
        for j, (r, z, _) in enumerate(profile):
            if r < 1e-6:
                if j not in axis:
                    axis[j] = M.v(o + Vector((0, 0, z)))
                col.append(axis[j])
            else:
                col.append(M.v(o + polar(r, a, z)))
        cols.append(col)
    m = len(angles)
    ou, ov = M.off()
    for i in range(m if full else m - 1):
        p, q = cols[i], cols[(i + 1) % m]
        a0 = angles[i]
        a1 = angles[i + 1] if i + 1 < m else angles[0] + 360.0
        for j in range(n):
            k = (j + 1) % n
            rj, rk = profile[j][0], profile[k][0]
            if rj < 1e-6 and rk < 1e-6:
                continue
            mat, kind = paint(profile[j][2], i, a0, a1)
            rref = (rj + rk) / 2
            seg = cum[j + 1] - cum[j]

            def uv(a, end):
                if kind == "plate":
                    return (ufun(a), cum[j + end] / PLATE_V)
                if kind in LED_BANDS:
                    return (rref * math.radians(a) / 4.0, LED_BANDS[kind][end])
                if kind == "bglass":
                    return (rref * math.radians(a) / 8.0, end * seg / 8.0)
                return (ou + rref * math.radians(a) / TILE, ov + cum[j + end] / TILE)

            verts, uvs = [], []
            for vert, coord in ((p[j], uv(a0, 0)), (p[k], uv(a0, 1)), (q[k], uv(a1, 1)), (q[j], uv(a1, 0))):
                if vert not in verts:
                    verts.append(vert)
                    uvs.append(coord)
            if len(verts) >= 3:
                M.f(verts, mat, uvs).smooth = True
    for j in range(n):                                            # a crease wherever the profile turns a corner
        (ra, za), (rb, zb_), (rc, zc) = profile[j - 1][:2], profile[j][:2], profile[(j + 1) % n][:2]
        d0, d1 = Vector((rb - ra, zb_ - za)), Vector((rc - rb, zc - zb_))
        if d0.length < 1e-6 or d1.length < 1e-6 or d0.angle(d1) > math.radians(28):
            for i in range(m if full else m - 1):
                if cols[i][j] is cols[(i + 1) % m][j]:
                    continue
                e = M.bm.edges.get((cols[i][j], cols[(i + 1) % m][j]))
                if e is not None:
                    e.smooth = False
    if not full:
        for col, flip in ((cols[0], False), (cols[-1], True)):
            uvs = [(ou + r / TILE, ov + z / TILE) for r, z, _ in profile]
            M.f(col[::-1] if flip else col, cap, uvs[::-1] if flip else uvs)


def world(mat):
    return lambda tag, i, a0, a1: (mat, "world")


# ---------------------------------------------------------------- 1. the canopy's roof plate
def plate(coll, mid):
    """The roof itself, ONE closed solid: nose, front gutter, glazed leading band, sheet with
    skylight bays, rear gutter, rear edge, the ring girder the masts run through, the soffit."""
    u = under
    profile = [
        (152.2, 68.3, "nose"), (150.5, 68.3, "nose"), (150.0, 68.6, "led_front"), (150.0, 69.3, "nose"),
        (150.4, 69.65, "gutter"), (150.9, 69.65, "gutter"), (150.9, 69.25, "gutter"), (151.9, 69.25, "gutter"),
        (151.9, 69.9, "gutter"), (152.3, 69.98, "glass"), (154.5, 70.6, "glass"), (159.0, 71.2, "skin"),
        (162.0, 71.1, "sky"), (174.0, 70.35, "skin"), (184.6, 69.4, "skin"),
        (186.2, 69.3, "gutter"), (186.2, 68.8, "gutter"), (187.4, 68.8, "gutter"), (187.4, 69.2, "gutter"),
        (188.0, 69.2, "skin"), (190.0, 67.9, "edge"), (190.0, 67.5, "led_rear"), (190.0, 67.0, "edge"),
        (190.0, 66.5, "edge"), (189.6, 66.3, "skin"),
        (185.7, 67.9, "girder"), (185.7, 64.45, "girder"), (183.3, 64.7, "girder"),
        (183.3, u(183.3), "skin"), (174.0, u(174.0), "sky"), (162.0, u(162.0), "skin"),
        (159.0, u(159.0), "glass"), (154.5, u(154.5), "glass"), (152.2, 69.5, "nose")]
    rel = [-HALF]
    for b0, b1 in zip(MAIN_REL, MAIN_REL[1:]):                    # a column each side of every bay's middle
        rel += [b0, (b0 + b1) / 2]
    rel += [MAIN_REL[-1], HALF]

    def paint(tag, i, a0, a1):
        r = (a0 + a1) / 2 - mid
        if tag in ("nose",):
            return ("housing", "world")
        if tag in ("gutter", "edge", "girder"):
            return ("steel", "world")
        if tag == "glass":
            return ("glazing", "plate")
        if tag == "skin":
            return ("skin", "plate")
        if tag == "sky":
            return ("glazing" if bay_glazed(r) else "skin", "plate")
        if tag == "led_front":
            return ("led", "white" if bay_glazed(r) else "blue")
        return ("led", "blue")

    M = Mesh()
    sweep(M, profile, [mid + r for r in rel], paint, cap="steel", ufun=lambda a: bay_u(a - mid))
    return M.done("roof_canopy_%s" % NAMES[mid], coll)


# ---------------------------------------------------------------- 2. the structure under and behind it
def truss(M, a):
    """A cantilever truss: its top chord in the plate, its bottom chord straight from the nose to
    the ring girder, round web members between their centre lines."""
    P = lambda r, z: polar(r, a, z)
    tc = lambda r: under(r) - 0.17
    pts = [P(151.6, 68.95)] + [P(r, tc(r)) for r in (152.6, 154.5, 159.0, 162.0, 174.0, 183.7)]
    polybeam(M, pts, 0.45, 0.5, tang(a))
    beam(M, P(151.7, zb(151.7)), P(183.8, zb(183.8)), 0.45, 0.4)
    X = (152.6, 156.0, 162.0, 168.0, 174.0, 180.0, 183.6)
    for r in X[1:-1]:
        tube(M, P(r, zb(r)), P(r, tc(r)), 0.13)
    for r0, r1 in zip(X, X[1:]):
        tube(M, P(r0, zb(r0)), P(r1, tc(r1)), 0.13)


def rib_plate(M, a, width=0.22):
    """A lighter rib between trusses: a tapered plate girder with a bottom flange."""
    poly = [(151.7, 69.1), (152.4, under(152.4) + 0.08)] + [(r, under(r) + 0.08) for r in (154.5, 159.0, 162.0, 174.0, 183.6)]
    poly += [(183.6, 66.9), (151.7, 68.75)]
    prism(M, poly, (0, 0, 0), rad(a), Z, tang(a), -width / 2, width / 2, mat="steel")
    beam(M, polar(151.7, a, 68.75), polar(183.6, a, 66.9), 0.36, 0.14)


def rib_bottom(r):
    return 68.75 + (66.9 - 68.75) * (r - 151.7) / (183.6 - 151.7)


def mast_radius(z):
    if z <= 64.4:
        return 1.35 - 0.35 * (z + 3.0) / 67.4
    return 1.0 - 0.5 * (z - 64.4) / (MAST_HEAD - 64.4)


def plinth(M, at, r, top_z):
    """A foot on the hull deck: its base a metre into the deck."""
    sweep(M, [(0, DECK_Z - 1.0, "h"), (r, DECK_Z - 1.0, "h"), (r, DECK_Z + 0.9, "h"), (r - 0.4, DECK_Z + 1.2, "h"),
              (r - 0.8, DECK_Z + 1.2, "h"), (r - 0.8, top_z, "h"), (0, top_z, "h")],
          [22.5 + 45.0 * i for i in range(8)], world("housing"), origin=(at.x, at.y, 0.0), full=True)


def beacon(M, at, a, name):
    """The lit drum on a mast's head: a stub in the mast, the drum's foot in the stub."""
    at = Vector(at)
    spindle(M, [(at - Z * 0.5, 0.32), (at + Z * 0.7, 0.32)], 8, "housing")
    lo, hi = LED_BANDS["white"]
    ring_a = [M.v(at + Z * 0.5 + rad(a + 45 * i) * 0.42) for i in range(8)]
    ring_b = [M.v(at + Z * 1.4 + rad(a + 45 * i) * 0.42) for i in range(8)]
    for i in range(8):
        k = (i + 1) % 8
        M.f((ring_a[i], ring_a[k], ring_b[k], ring_b[i]), "led", [(i / 8, lo), ((i + 1) / 8, lo), ((i + 1) / 8, hi), (i / 8, hi)])
    disc = [(0.5 + 0.42 * math.cos(math.pi * i / 4) / TILE, 0.5 + 0.42 * math.sin(math.pi * i / 4) / TILE) for i in range(8)]
    M.f(ring_a[::-1], "housing", disc[::-1])
    M.f(ring_b, "housing", disc)
    LIGHTS["mast_beacons"].append({"name": "beacon_%s" % name, "position": [round(c, 2) for c in at + Z * 0.95],
                                   "colour": "white, small, slow blink"})


def mast(M, a, name):
    """One mast: deck to head, through the ring girder and the roof; stays to the roof, a raking
    back leg to the deck, ties into the stand's wall."""
    A = lambda z: polar(MAST_R, a, z)
    foot = A(DECK_Z - 1.0)
    plinth(M, foot, 2.7, 0.3)
    spindle(M, [(A(DECK_Z - 0.6), mast_radius(-3.0)), (A(64.4), 1.0), (A(MAST_HEAD), 0.5)], 10)
    for k in range(4):                                            # gussets from the plinth up the mast
        b = a + 45.0 + 90.0 * k
        prism(M, [(1.05, 0.25), (1.85, 0.25), (1.05, 3.2)], (foot.x, foot.y, 0.0), rad(b), Z, tang(b), -0.07, 0.07, mat="steel")
    spindle(M, [(A(69.0), 1.18), (A(70.3), 1.0)], 10, "housing")  # the flashing collar where it leaves the roof
    beacon(M, A(MAST_HEAD), a, name)
    head = A(MAST_HEAD - 1.4)
    for r in (160.0, 172.0):                                      # forestays, into the plate over a truss
        tube(M, head, polar(r, a, top(r) - 0.22), 0.12)
    tube(M, head, polar(188.9, a, 68.0), 0.12)                    # the backstay, into the rear edge
    spindle(M, [(A(MAST_HEAD - 2.0), 0.82), (A(MAST_HEAD - 0.9), 0.76)], 10)   # the head's collar the stays leave from
    # the back leg and its lacing
    leg0, apex = polar(197.0, a, DECK_Z - 1.0), A(56.0)
    plinth(M, leg0, 1.9, 0.0)
    spindle(M, [(leg0, 0.75), (apex, 0.6)], 8)
    L = lambda z: leg0 + (apex - leg0) * ((z - leg0.z) / (apex.z - leg0.z))
    spindle(M, [(A(55.2), mast_radius(56) + 0.28), (A(56.8), mast_radius(56) + 0.28)], 10)
    for z in (18.0, 37.0):
        tube(M, A(z), L(z), 0.3)
    tube(M, L(0.5), A(18.0), 0.2)
    tube(M, A(18.0), L(37.0), 0.2)
    # ties into the stand's outer wall (r 180 to 181.2)
    tube(M, A(22.0), polar(180.0, a, 22.0), 0.26)
    tube(M, A(52.0), polar(179.3, a, 52.0), 0.26)


def canopy_steel(coll, mid):
    M = Mesh()
    for r in TRUSS_REL:
        truss(M, mid + r)
    for r in PLATE_REL:
        rib_plate(M, mid + r)
    for r in END_REL:
        rib_plate(M, mid + r, 0.3)
    for r in PURLINS:                                             # purlins: straight from rib to rib, their tops in the plate
        z = under(r)
        sweep(M, [(r - 0.2, z - 0.72, "s"), (r + 0.2, z - 0.72, "s"), (r + 0.2, z + 0.10, "s"), (r - 0.2, z + 0.10, "s")],
              [mid + x for x in ALL_REL], world("steel"))
    # the catwalk: a deck on the trusses' bottom chords, hung from the plate ribs, two rails
    zd = zb(CATWALK_R) + 0.14
    rel = list(MAIN_REL)
    rel[0] -= 0.15
    rel[-1] += 0.15
    ang = [mid + x for x in rel]
    sweep(M, [(CATWALK_R - 0.6, zd, "s"), (CATWALK_R + 0.6, zd, "s"), (CATWALK_R + 0.6, zd + 0.14, "s"), (CATWALK_R - 0.6, zd + 0.14, "s")],
          ang, world("steel"))
    zr = zd + 1.14
    for r in (CATWALK_R - 0.5, CATWALK_R + 0.5):
        sweep(M, [(r - 0.05, zr, "s"), (r + 0.05, zr, "s"), (r + 0.05, zr + 0.1, "s"), (r - 0.05, zr + 0.1, "s")], ang, world("steel"))
        for i, x in enumerate(rel):
            if MAIN_REL[i] in PLATE_REL:                          # a hanger from the plate rib, through the rail, into the deck
                tube(M, polar(r, mid + x, rib_bottom(r) + 0.25), polar(r, mid + x, zd + 0.07), 0.05)
            else:
                beam(M, polar(r, mid + x, zd + 0.07), polar(r, mid + x, zr + 0.05), 0.06, 0.06, up=rad(mid + x))
            if i + 1 < len(rel):                                  # and a post at the middle of the bay
                p = (polar(r, mid + x, 0) + polar(r, mid + rel[i + 1], 0)) / 2
                beam(M, (p.x, p.y, zd + 0.07), (p.x, p.y, zr + 0.05), 0.06, 0.06, up=rad(mid + (x + rel[i + 1]) / 2))
    for x in (rel[0], rel[-1]):                                   # the rail closed across each end
        beam(M, polar(CATWALK_R - 0.5, mid + x, zr + 0.05), polar(CATWALK_R + 0.5, mid + x, zr + 0.05), 0.07, 0.06)
    # house lights under the catwalk, one at every rib, turned down onto the lower bowl
    for k, x in enumerate(MAIN_REL):
        a = mid + x
        C = polar(CATWALK_R, a, zd - 0.8)
        target = polar(118.0, a, 20.0)
        n, w, q, half = bank(M, C, target, a, tiles=1, s=0.5, cheeks=False)
        tube(M, C - n * 0.15, polar(CATWALK_R, a, zd + 0.07), 0.06)
        LIGHTS["house_lights"].append({"name": "house_%s_%d" % (NAMES[mid], k + 1), "position": [round(c, 2) for c in C],
                                       "aim_point": [round(c, 2) for c in target], "direction": [round(c, 4) for c in n],
                                       "face_m": 0.95, "colour": "cool white #eef5ff, dim"})
    # the way onto the catwalk: a walkway beside the truss at +9.5, from the ring girder, on brackets off the chord
    a = mid + 9.5
    side = -tang(a)
    W = lambda r, off, dz: polar(r, a, zb(r) + dz) + side * off
    beam(M, W(165.2, 0.85, 0.27), W(183.6, 0.85, 0.27), 1.0, 0.12)
    beam(M, W(165.7, 1.3, 1.37), W(183.5, 1.3, 1.37), 0.1, 0.1)
    for r in (166.0, 169.0, 172.0, 175.0, 178.0, 181.0, 183.2):
        beam(M, W(r, 1.3, 0.27), W(r, 1.3, 1.37), 0.05, 0.05, up=rad(a))
    for r in (168.0, 174.0, 180.0):
        beam(M, W(r, 0.0, 0.16), W(r, 1.36, 0.16), 0.12, 0.12)
    # the masts
    for k, r in enumerate(MAST_REL):
        mast(M, mid + r, "%s_%d" % (NAMES[mid], k + 1))
    A = lambda r, z: polar(MAST_R, mid + r, z)
    for k, (r0, r1) in enumerate(zip(MAST_REL, MAST_REL[1:])):    # ring tubes mast to mast, cross cables in the end bays
        tube(M, A(r0, 60.5), A(r1, 60.5), 0.4, 8)
        if k != 2:                                                # the middle bay stays open over the gate
            tube(M, A(r0, 30.0), A(r1, 30.0), 0.34, 8)
        if k in (0, 4):
            tube(M, polar(MAST_R - 0.2, mid + r0, 30.4), polar(MAST_R - 0.2, mid + r1, 60.1), 0.11)
            tube(M, polar(MAST_R + 0.2, mid + r1, 30.4), polar(MAST_R + 0.2, mid + r0, 60.1), 0.11)
    for r in MAST_REL:                                            # node collars where the ring tubes land
        for z in (30.0, 60.5):
            spindle(M, [(A(r, z - 0.55), mast_radius(z) + 0.2), (A(r, z + 0.55), mast_radius(z) + 0.2)], 10)
    return M.done("roof_canopy_steel_%s" % NAMES[mid], coll)


# ---------------------------------------------------------------- 3. what hangs on the front edge
def bank(M, C, target, a, tiles=5, s=1.0, cheeks=True):
    """A floodlight bank: ONE housing (back, visor, rim) with the lamp face let into it."""
    n = (Vector(target) - C).normalized()
    w = tang(a)
    q = w.cross(n).normalized()
    if q.z < 0:
        q = -q
    poly = [(-0.7, -1.05), (0.2, -1.15), (0.2, -1.0), (0.0, -0.95), (0.0, 0.95), (0.2, 1.0), (0.55, 1.12), (0.55, 1.2), (-0.7, 1.15)]
    poly = [(x * s, y * s) for x, y in poly]
    half = tiles * 1.9 * s / 2
    lamp_uv = [(0.0, 0.0), (0.0, 1.0), (float(tiles), 1.0), (float(tiles), 0.0)]
    prism(M, poly, C, n, q, w, -half, half, mat="housing", edge_mat={3: "lamp"}, edge_uv={3: lamp_uv})
    for sgn in ((-1, 1) if cheeks else ()):                       # an end cheek, the housing's end inside it
        obox(M, C + w * (sgn * (half + 0.05 * s)) + n * (-0.1 * s) + q * (0.025 * s), n, q, w, 1.45 * s, 2.55 * s, 0.3 * s)
    return n, w, q, half


def floods(coll, mid):
    M = Mesh()
    name = NAMES[mid]
    for k, r in enumerate((-27.0, -17.0, -7.125, 7.125, 17.0, 27.0)):
        a = mid + r
        C = polar(151.3, a, 66.75)
        target = polar(14.0, a, 0.5)
        n, w, q, half = bank(M, C, target, a)
        arms = []
        for sgn in (-1, 1):                                       # the yoke: an arm from each cheek up into the nose
            p = C + w * (sgn * (half + 0.05)) + n * -0.1
            hang = Vector((p.x, p.y, 68.62))
            beam(M, p, hang, 0.22, 0.3, up=rad(a))
            arms.append(Vector((p.x, p.y, 68.05)))
        tube(M, arms[0], arms[1], 0.09)                           # a spreader bar between the arms, under the nose
        LIGHTS["floodlight_banks"].append({
            "name": "flood_%s_%d" % (name, k + 1), "canopy": name, "bearing_deg": round(a % 360.0, 3),
            "position": [round(c, 2) for c in C], "aim_point": [round(c, 2) for c in target],
            "direction": [round(c, 4) for c in n], "face_width_m": round(half * 2, 2), "face_height_m": 1.9,
            "lamps": 20, "colour": "cool white #eef5ff", "beam": "a wide flat fan, 9.5 m across at the source"})
    for r in (-22.0, -12.0, 12.0, 22.0):                          # line-array speakers hung from the first purlin
        a = mid + r
        prad = 156.0 * math.cos(math.radians(2.5 if abs(r) > 10 else 2.5))
        topz = 67.45
        centre = polar(prad, a, 0.0)
        inward = -rad(a)
        spine, c, front, back = [], Vector((0.0, topz)), [], []
        for th in (0.0, 5.0, 12.0, 20.0, 30.0, 42.0):
            t = math.radians(th)
            nrm = Vector((math.cos(t), -math.sin(t)))
            front.append(c + nrm * 0.42)
            back.append(c - nrm * 0.34)
            spine.append(c + nrm * 0.04)
            c = c + Vector((-math.sin(t), -math.cos(t))) * 0.72
        spine.append(c + Vector((math.cos(math.radians(42.0)), -math.sin(math.radians(42.0)))) * 0.04)
        spine[0] = spine[0] + Vector((0.0, 0.06))                 # its head in the bumper, its foot just past the last cabinet
        spine[-1] = spine[-1] + (spine[-1] - spine[-2]).normalized() * 0.06
        t = math.radians(42.0)
        nrm = Vector((math.cos(t), -math.sin(t)))
        front.append(c + nrm * 0.42)
        back.append(c - nrm * 0.34)
        poly = [(p.x, p.y) for p in front] + [(p.x, p.y) for p in back[::-1]]
        prism(M, poly, centre, inward, Z, tang(a), -0.8, 0.8, mat="housing")
        for sgn in (-1, 1):                                       # the fly frame's side straps, the cabinets' ends inside them
            polybeam(M, [centre + inward * q.x + Z * q.y + tang(a) * (0.82 * sgn) for q in spine], 0.1, 0.5, tang(a))
        beam(M, centre + Z * (topz + 0.02) - tang(a) * 0.95, centre + Z * (topz + 0.02) + tang(a) * 0.95, 0.5, 0.26, up=Z)
        for sgn in (-1, 1):                                       # two hangers into the purlin
            p = centre + tang(a) * (0.75 * sgn)
            tube(M, (p.x, p.y, topz - 0.06), (p.x, p.y, under(156.0) - 0.1), 0.05)
    for r in (-2.375, 2.375):                                     # a camera head under the nose, looking at the stage
        a = mid + r
        hang = polar(151.3, a, 68.6)
        head = polar(151.3, a, 67.2)
        n = (polar(0.0, 0.0, 2.0) - head).normalized()
        w = tang(a)
        q = w.cross(n).normalized()
        tube(M, hang, head, 0.07)
        obox(M, head, n, q, w, 0.9, 0.55, 0.6)
        tube(M, head + n * 0.2, head + n * 0.85, 0.2, 8, 0.24, mat="housing")
    # the lug the scoreboard's cable ends in, under the nose at the middle truss
    lug = polar(150.95, mid, 68.05)
    obox(M, lug, rad(mid), tang(mid), Z, 1.0, 0.9, 0.7, mat="steel")
    return M.done("roof_floodlights_%s" % name, coll), lug


# ---------------------------------------------------------------- 4. the scoreboard
S8, C8 = math.sin(math.radians(22.5)), math.cos(math.radians(22.5))
BOARD_LUG_R, BOARD_LUG_Z = 7.0, 52.6


def scoreboard(coll, lugs):
    """An eight-faced drum, ONE closed solid. Its screens lean 18 degrees to the stage so a player
    under it can read the far faces. In the middle of every face is
    the CONTENT cell (the logo): faces of the same surface, a different material."""
    profile = [
        (0.0, 52.0, "housing"), (8.2, 52.0, "housing"), (9.4, 51.6, "led_white"), (9.4, 50.9, "housing"),
        (9.05, 50.72, "housing"), (9.0, 50.6, "screen"), (8.444, 48.88, "content"), (7.386, 45.60, "screen"),
        (7.0, 44.4, "housing"), (7.6, 44.1, "led_blue"), (7.6, 43.4, "housing"), (7.2, 43.0, "housing"),
        (6.2, 42.6, "led_white"), (5.8, 42.6, "housing"), (4.7, 42.4, "housing"),
        (4.4, 42.9, "flat_cyan"), (3.4, 42.9, "housing"), (3.2, 42.55, "housing"), (2.3, 42.55, "housing"),
        (2.1, 42.9, "flat_cyan"), (1.2, 42.9, "housing"), (1.0, 41.9, "housing"), (0.5, 41.6, "housing"), (0.0, 41.6, None)]
    M = Mesh()
    rings, tcs = [], []
    cum = [0.0]
    for j, (r, z, _) in enumerate(profile):
        if j:
            r0, z0 = profile[j - 1][:2]
            cum.append(cum[-1] + math.hypot((r - r0) * C8, z - z0))
        if r < 1e-6:
            rings.append([M.v((0, 0, z))])
            tcs.append(None)
            continue
        t = min(2.6, 0.93 * r * S8)
        vs = []
        for s in range(8):
            b = 45.0 * s
            c = polar(r * C8, b, z)
            vs += [M.v(polar(r, b - 22.5, z)), M.v(c - tang(b) * t), M.v(c + tang(b) * t)]
        rings.append(vs)
        tcs.append((-r * S8, -t, t, r * S8))
    for j in range(len(profile) - 1):
        tag = profile[j][2]
        up, dn = rings[j], rings[j + 1]
        rmean = (profile[j][0] + profile[j + 1][0]) / 2
        for s in range(8):
            for k in range(3):
                i0, i1 = s * 3 + k, (s * 3 + k + 1) % 24
                if tag == "content" and k == 1:
                    mat, kind = "logo", "logo"
                elif tag in ("screen", "content"):
                    mat, kind = "screen", "world"
                elif tag.startswith("led_"):
                    mat, kind = "led", tag[4:]
                elif tag == "flat_cyan":
                    mat, kind = "led", "flat"
                else:
                    mat, kind = tag, "world"

                def uv(ring, col, end):
                    t = tcs[ring][col] if tcs[ring] else 0.0
                    if kind == "logo":                           # read from outside: right is the falling bearing
                        return (0.0 if col == 2 else 1.0, 1.0 - end)
                    if kind == "flat":                           # the emitter: one module's flat cyan, no gaps
                        return (0.0625, LED_BANDS["cyan"][end])
                    if kind in LED_BANDS:                        # modules square to the face, in metres
                        return ((t + 2 * s * rmean * S8) / 4.0, LED_BANDS[kind][end])
                    return ((t + s * 5.3) / TILE, cum[ring] / TILE)

                if len(up) == 1:
                    M.f((up[0], dn[i1], dn[i0]), mat, [uv(j, 0, 0), uv(j + 1, k + 1, 1), uv(j + 1, k, 1)])
                elif len(dn) == 1:
                    M.f((up[i0], up[i1], dn[0]), mat, [uv(j, k, 0), uv(j, k + 1, 0), uv(j + 1, 0, 1)])
                else:
                    M.f((up[i0], up[i1], dn[i1], dn[i0]), mat, [uv(j, k, 0), uv(j, k + 1, 0), uv(j + 1, k + 1, 1), uv(j + 1, k, 1)])
    board = M.done("roof_scoreboard", coll)

    R = Mesh()                                                    # the rigging frame on top and the four cables
    spindle(R, [((0, 0, 51.8), 1.5), ((0, 0, 52.6), 1.5)], 8, "housing")
    for s in range(4):
        b = 90.0 * s
        beam(R, polar(0.6, b, 52.2), polar(7.45, b, 52.2), 0.5, 0.6)
        beam(R, polar(0.7, b + 45, 52.07), polar(7.3, b + 45, 52.07), 0.3, 0.4)
        obox(R, polar(BOARD_LUG_R, b, BOARD_LUG_Z), rad(b), tang(b), Z, 0.6, 0.6, 0.7, mat="steel")
        tube(R, polar(BOARD_LUG_R, b, BOARD_LUG_Z), lugs[b], 0.16)
    rig = R.done("roof_scoreboard_rigging", coll)
    LIGHTS["other"].append({"name": "scoreboard_emitter", "position": [0.0, 0.0, 42.9], "direction": [0.0, 0.0, -1.0],
                            "colour": "cyan #3fe0f0", "note": "a ring r 2.3 to 4.3 under the scoreboard; a soft cone down onto the can, no hard beam"})
    return board, rig


# ---------------------------------------------------------------- 5. the corner screens
def corner_screen(coll, b):
    name = {45: "ne", 135: "se", 225: "sw", 315: "nw"}[b]
    nrm, tan = rad(b), tang(b)
    c = polar(152.0, b, 44.0)
    P = lambda s, d, z: c + tan * s + nrm * d + Z * z
    M = Mesh()
    xs = [-20.8, -20.0, -19.9, -12.0, 12.0, 19.9, 20.0, 20.8]
    zs = [-9.8, -9.0, -8.9, -7.9, 7.9, 8.9, 9.0, 9.8]
    ou, ov = M.off()
    grid = [[M.v(P(xs[i], -0.6 if min(i, 7 - i, j, 7 - j) <= 1 else -0.4, zs[j])) for j in range(8)] for i in range(8)]
    for i in range(7):
        for j in range(7):
            ring = min(i, 6 - i, j, 6 - j)
            quad = (grid[i][j], grid[i + 1][j], grid[i + 1][j + 1], grid[i][j + 1])
            if (i, j) == (3, 3):                                  # the content cell, cut into the screen
                M.f(quad, "logo", [(0, 0), (1, 0), (1, 1), (0, 1)])
            else:
                mat = "housing" if ring <= 1 else "screen"
                M.f(quad, mat, [(ou + xs[ii] / TILE, ov + zs[jj] / TILE) for ii, jj in ((i, j), (i + 1, j), (i + 1, j + 1), (i, j + 1))])
    back = {(i, j): M.v(P(xs[i], 0.6, zs[j])) for i in (0, 7) for j in (0, 7)}
    M.f([back[(0, 0)], back[(7, 0)], back[(7, 7)], back[(0, 7)]], "housing",
        [(ou + xs[i] / TILE, ov + 0.3 + zs[j] / TILE) for i, j in ((0, 0), (7, 0), (7, 7), (0, 7))])
    for j in (0, 7):                                              # top and bottom: the front row and the two back corners
        M.f([grid[i][j] for i in range(8)] + [back[(7, j)], back[(0, j)]], "housing",
            [(ou + xs[i] / TILE, ov) for i in range(8)] + [(ou + xs[7] / TILE, ov + 1.2 / TILE), (ou + xs[0] / TILE, ov + 1.2 / TILE)])
    for i in (0, 7):
        M.f([grid[i][j] for j in range(8)] + [back[(i, 7)], back[(i, 0)]], "housing",
            [(ou, ov + zs[j] / TILE) for j in range(8)] + [(ou + 1.2 / TILE, ov + zs[7] / TILE), (ou + 1.2 / TILE, ov + zs[0] / TILE)])
    for s, z in ((-9.0, -6.4), (3.0, -6.4), (10.0, -6.4), (-3.2, 6.3)):   # service cabinets on the back, their fronts in it
        obox(M, P(s, 0.875, z), tan, nrm, Z, 2.2, 0.75, 2.6)
    cab = M.done("roof_corner_screen_%s" % name, coll)

    S = Mesh()
    for z in (-8.5, -4.25, 0.0, 4.25, 8.5):                       # girts: fronts in the cabinet, backs in the masts
        beam(S, P(-20.3, 0.75, z), P(20.3, 0.75, z), 0.5, 0.45, up=Z)
    for s in (-19.8, -6.5, 0.0, 6.5, 19.8):                       # stiffeners between the outer girts
        beam(S, P(s, 0.72, -8.5), P(s, 0.72, 8.5), 0.3, 0.3, up=nrm)
    feet = []
    for s in (-13.0, 13.0):
        base = P(s, 1.65, 0.0)
        foot = Vector((base.x, base.y, DECK_Z - 1.0))
        feet.append(foot)
        plinth(S, foot, 2.3, 0.2)
        spindle(S, [((base.x, base.y, DECK_Z - 0.6), 1.05), ((base.x, base.y, 53.0), 0.75), ((base.x, base.y, 60.0), 0.42)], 10)
        beacon(S, (base.x, base.y, 60.0), b, "screen_%s_%s" % (name, "a" if s < 0 else "b"))
        legp = P(s, 15.0, 0.0)
        leg0 = Vector((legp.x, legp.y, DECK_Z - 1.0))
        apex = Vector((base.x, base.y, 40.0))
        plinth(S, leg0, 1.7, -0.2)
        spindle(S, [(leg0, 0.55), (apex, 0.45)], 8)
        spindle(S, [((base.x, base.y, 39.3), 1.12), ((base.x, base.y, 40.7), 1.12)], 10)
        L = lambda z, leg0=leg0, apex=apex: leg0 + (apex - leg0) * ((z - leg0.z) / (apex.z - leg0.z))
        Mz = lambda z, base=base: Vector((base.x, base.y, z))
        for z in (12.0, 26.0):
            tube(S, Mz(z), L(z), 0.24)
        tube(S, L(0.5), Mz(12.0), 0.16)
        tube(S, Mz(12.0), L(26.0), 0.16)
        beam(S, P(s, 1.65, -9.55), P(s, 3.75, -9.55), 0.25, 0.25, up=Z)   # the catwalk's bracket
    f0, f1 = feet
    Mz = lambda f, z: Vector((f.x, f.y, z))
    for z in (2.0, 17.0, 32.0):                                   # the two masts braced to each other
        tube(S, Mz(f0, z), Mz(f1, z), 0.3, 8)
    for z0, z1 in ((2.0, 17.0), (17.0, 32.0)):
        tube(S, Mz(f0, z0 + 0.3) + nrm * 0.25, Mz(f1, z1 - 0.3) + nrm * 0.25, 0.13)
        tube(S, Mz(f1, z0 + 0.3) - nrm * 0.25, Mz(f0, z1 - 0.3) - nrm * 0.25, 0.13)
    beam(S, P(-14.5, 3.2, -9.4), P(14.5, 3.2, -9.4), 1.2, 0.14, up=Z)    # the service catwalk behind the screen's foot
    for d in (2.65, 3.75):
        beam(S, P(-14.5, d, -8.25), P(14.5, d, -8.25), 0.1, 0.1, up=Z)
        for k in range(7):
            s = -14.4 + 4.8 * k
            beam(S, P(s, d, -9.37), P(s, d, -8.22), 0.06, 0.06, up=nrm)
    steel = S.done("roof_corner_screen_steel_%s" % name, coll)
    return cab, steel


# ---------------------------------------------------------------- 6. the host's booth
def booth(coll):
    """On the north concourse (r 130 to 140, z 26): a glazed room leaning out over the bowl, its
    back in the upper stand's front wall, its floor in the concourse."""
    M = Mesh()
    profile = [(133.0, 25.7, "wall"), (133.0, 26.9, "glass"), (132.4, 29.4, "wall"), (132.4, 29.7, "light"),
               (131.7, 29.75, "trim"), (131.7, 30.15, "roof"), (140.6, 30.75, "wall"), (140.6, 25.7, "wall")]

    def paint(tag, i, a0, a1):
        return {"wall": ("booth_wall", "world"), "glass": ("booth_glass", "bglass"), "light": ("led", "gold"),
                "trim": ("steel", "world"), "roof": ("skin", "world")}[tag]

    ang = [-5.0 + 1.25 * i for i in range(9)]
    sweep(M, profile, ang, paint, cap="booth_wall")
    for a in ang:                                                 # mullions, 10 cm proud of the glass, ends in sill and head
        out = -rad(a)
        beam(M, polar(133.02, a, 26.8) + out * 0.04, polar(132.42, a, 29.5) + out * 0.04, 0.14, 0.2, up=tang(a))
    # on the roof: an antenna, a dish, two small light bars turned to the stage
    roof = lambda r: 30.15 + 0.6 * (r - 131.7) / 8.9
    p = polar(138.6, 3.6, 0.0)
    spindle(M, [((p.x, p.y, roof(138.6) - 0.3), 0.14), ((p.x, p.y, 38.6), 0.05)], 6)
    for k, z in enumerate((34.6, 35.9, 37.2)):
        d = tang(3.6) if k % 2 == 0 else rad(3.6)
        beam(M, Vector((p.x, p.y, z)) - d * (0.9 - 0.15 * k), Vector((p.x, p.y, z)) + d * (0.9 - 0.15 * k), 0.07, 0.07)
    d0 = polar(137.4, -3.2, 0.0)
    tube(M, (d0.x, d0.y, roof(137.4) - 0.3), (d0.x, d0.y, 31.9), 0.13)
    aim = (Vector((-0.35, -0.55, 0.76))).normalized()
    hub = Vector((d0.x, d0.y, 31.9))
    spindle(M, [(hub - aim * 0.12, 0.2), (hub + aim * 0.18, 0.55), (hub + aim * 0.42, 1.0)], 10, "booth_wall")
    tube(M, hub, hub + aim * 1.0, 0.04)
    for k, a in enumerate((-3.9, 3.9)):
        C = polar(132.3, a, 31.15)
        target = polar(10.0, a, 1.0)
        n, w, q, half = bank(M, C, target, a, tiles=3, s=0.3)
        tube(M, C - n * 0.1, polar(132.3, a, roof(132.3) - 0.2), 0.07)
        LIGHTS["booth_lights"].append({"name": "booth_light_%d" % (k + 1), "position": [round(c, 2) for c in C],
                                       "aim_point": [round(c, 2) for c in target], "direction": [round(c, 4) for c in n],
                                       "face_width_m": round(half * 2, 2), "face_height_m": 0.57, "colour": "warm white #ffe6c0"})
    return M.done("roof_host_booth", coll)


# ---------------------------------------------------------------- checks
def report(objs):
    total, bad = 0, 0
    for ob in sorted(objs, key=lambda o: o.name):
        bm = bmesh.new()
        bm.from_mesh(ob.data)
        open_edges = sum(1 for e in bm.edges if len(e.link_faces) == 1)
        loose = sum(1 for e in bm.edges if len(e.link_faces) == 0)
        many = sum(1 for e in bm.edges if len(e.link_faces) > 2)
        tris = sum(len(f.verts) - 2 for f in bm.faces)
        bm.free()
        total += tris
        bad += open_edges + loose + many
        flag = "" if open_edges + loose + many == 0 else "   <-- OPEN %d LOOSE %d NONMANIFOLD %d" % (open_edges, loose, many)
        print("MESH %-40s tris %6d  materials %d%s" % (ob.name, tris, len(ob.data.materials), flag))
    print("TOTAL TRIANGLES %d   OPEN/LOOSE/NONMANIFOLD EDGES %d   MATERIALS %d" % (
        total, bad, len({m.name for ob in objs for m in ob.data.materials})))
    return total, bad


def coplanar(objs, tol=0.012, depth=0.03):
    """Pairs of faces that lie in one plane and overlap: z-fighting. Convex hulls, so a hit
    against a notched cap can be false; every hit is printed with its place."""
    faces = []
    for ob in objs:
        me = ob.data
        for p in me.polygons:
            n = p.normal.copy()
            if n.length < 0.5 or p.area < 1e-4:
                continue
            for c in n:
                if abs(c) > 1e-3:
                    if c < 0:
                        n = -n
                    break
            pts = [me.vertices[i].co.copy() for i in p.vertices]
            faces.append((n, n.dot(pts[0]), pts, ob.name))
    buckets = {}
    q = 12.0
    for idx, (n, d, _, _) in enumerate(faces):
        buckets.setdefault((round(n.x * q), round(n.y * q), round(n.z * q), math.floor(d / 0.05)), []).append(idx)

    def overlap(pa, pb, n):
        e1 = n.cross(Vector((1, 0, 0)) if abs(n.x) < 0.9 else Vector((0, 1, 0))).normalized()
        e2 = n.cross(e1)
        A = [(p.dot(e1), p.dot(e2)) for p in pa]
        B = [(p.dot(e1), p.dot(e2)) for p in pb]
        for poly in (A, B):
            for i in range(len(poly)):
                x0, y0 = poly[i]
                x1, y1 = poly[(i + 1) % len(poly)]
                ax, ay = y0 - y1, x1 - x0
                ln = math.hypot(ax, ay)
                if ln < 1e-9:
                    continue
                ax, ay = ax / ln, ay / ln
                a = [x * ax + y * ay for x, y in A]
                b = [x * ax + y * ay for x, y in B]
                if min(max(a), max(b)) - max(min(a), min(b)) < depth:
                    return False
        return True

    hits = []
    seen = set()
    for idx, (n, d, pts, name) in enumerate(faces):
        kx, ky, kz, kd = round(n.x * q), round(n.y * q), round(n.z * q), math.floor(d / 0.05)
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                for dz in (-1, 0, 1):
                    for dd in (-1, 0, 1):
                        for other in buckets.get((kx + dx, ky + dy, kz + dz, kd + dd), ()):
                            if other <= idx or (idx, other) in seen:
                                continue
                            n2, _, pts2, name2 = faces[other]
                            if n.dot(n2) < 0.9997 or abs(n.dot(pts2[0]) - d) > tol:
                                continue
                            if max(abs(n.dot(p) - d) for p in pts2) > tol * 2:
                                continue
                            if overlap(pts, pts2, n):
                                seen.add((idx, other))
                                c = sum(pts, Vector()) / len(pts)
                                hits.append((name, name2, c))
    print("COPLANAR OVERLAPS %d" % len(hits))
    groups = {}
    for name, name2, c in hits:                                   # one line for each kind of place
        groups.setdefault((name.rsplit("_", 1)[0], name2.rsplit("_", 1)[0], round(math.hypot(c.x, c.y)), round(c.z)), []).append(c)
    for (name, name2, r, z), cs in sorted(groups.items())[:60]:
        c = cs[0]
        print("   %4d x  %s | %s  r %d z %d  e.g. (%.2f, %.2f, %.2f)" % (len(cs), name, name2, r, z, c.x, c.y, c.z))
    return len(hits)


# ---------------------------------------------------------------- the review scene and its pictures
def shoot(name, loc, target, lens=35.0):
    scene = bpy.context.scene
    cam_data = bpy.data.cameras.new(name)
    cam = bpy.data.objects.new(name, cam_data)
    scene.collection.objects.link(cam)
    cam.location = loc
    cam.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
    cam_data.lens = lens
    cam_data.clip_start = 0.3
    cam_data.clip_end = 6000
    scene.camera = cam
    scene.render.filepath = os.path.join(LOGS, name + ".png")
    bpy.ops.render.render(write_still=True)


def backdrop():
    """The blockout's bowl, field, stage and hull as ONE plain grey: only so the kit is judged in
    place. Never saved into the kit's file."""
    import author_arena_stadium as S
    coll = K.collection("backdrop (review only)")
    S.stage(coll); S.field(coll); S.lower_bowl(coll); S.upper_stands(coll); S.hull(coll)
    grey = bpy.data.materials.new("backdrop_grey")
    grey.use_nodes = True
    grey.diffuse_color = (0.20, 0.21, 0.24, 1)
    b = grey.node_tree.nodes.get("Principled BSDF")
    b.inputs["Base Color"].default_value = (0.055, 0.06, 0.075, 1)
    b.inputs["Roughness"].default_value = 0.9
    for ob in coll.objects:
        for i in range(len(ob.data.materials)):
            ob.data.materials[i] = grey
    return coll


def shots(version, which):
    scene = bpy.context.scene
    backdrop()
    key = bpy.data.objects.new("review key", bpy.data.lights.new("review key", "SUN"))
    key.data.energy = 2.2
    key.data.color = (0.86, 0.93, 1.0)
    key.data.angle = math.radians(30)
    key.rotation_euler = (math.radians(18), math.radians(10), 0)
    scene.collection.objects.link(key)
    fill = bpy.data.objects.new("review bounce", bpy.data.lights.new("review bounce", "SUN"))
    fill.data.energy = 1.1                                         # the lit field thrown back up under the roof
    fill.data.color = (0.80, 0.95, 0.88)
    fill.data.use_shadow = False
    fill.rotation_euler = (math.radians(180 - 20), 0, 0)
    scene.collection.objects.link(fill)
    for nm, rot in (("review from north", (math.radians(60), 0, 0)), ("review from south", (math.radians(60), 0, math.radians(180))),
                    ("review from east", (math.radians(60), 0, math.radians(90))), ("review from west", (math.radians(60), 0, math.radians(270)))):
        side_light = bpy.data.objects.new(nm, bpy.data.lights.new(nm, "SUN"))
        side_light.data.energy = 0.8
        side_light.data.color = (0.80, 0.88, 1.0)
        side_light.data.use_shadow = False
        side_light.rotation_euler = rot
        scene.collection.objects.link(side_light)
    world = bpy.data.worlds.new("night")
    scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.012, 0.018, 0.060, 1)
    world.node_tree.nodes["Background"].inputs[1].default_value = 1.0
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    eevee = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1920, 1080
    scene.view_settings.view_transform = "Standard"
    p = "roof_%s_" % version
    eye = (0.0, 0.0, EYE)
    painted = [
        ("01_eye_canopy_north", eye, polar(150.0, 0.0, 60.0), 26),
        ("02_eye_canopy_north_tele", eye, polar(150.0, 6.0, 67.0), 70),
        ("03_eye_scoreboard", (0.0, -3.0, EYE), (0.0, 1.0, 47.0), 20),
        ("04_eye_scoreboard_from_edge", polar(21.0, 200.0, EYE), (0.0, 0.0, 46.0), 30),
        ("05_eye_corner_screen", eye, polar(152.0, 45.0, 42.0), 45),
        ("06_eye_wide_north_east", eye, polar(150.0, 28.0, 50.0), 16),
        ("07_top_row_across", polar(175.0, 180.0, 60.0), (0.0, 30.0, 44.0), 24),
        ("08_top_row_up_at_own_canopy", polar(172.0, 180.0, 58.5), polar(160.0, 166.0, 69.5), 20),
        ("09_plaza_behind_north", polar(262.0, 22.0, 1.0), polar(186.0, 6.0, 36.0), 20),
        ("10_plaza_behind_corner_screen", polar(196.0, 52.0, -0.2), polar(153.0, 45.0, 30.0), 18),
        ("11_mast_foot", polar(207.0, 25.0, 2.5), polar(189.0, 19.5, 5.0), 26),
        ("12_mast_head", polar(206.0, 12.0, 84.0), polar(184.5, 19.5, 76.0), 30),
        ("13_floodlight_bank", polar(138.0, 9.5, 62.5), polar(151.3, 7.125, 67.2), 40),
        ("14_scoreboard_close", (26.0, -30.0, 54.0), (0.0, 0.0, 47.0), 35),
        ("15_scoreboard_under", (9.0, -13.0, 30.0), (0.0, 0.0, 44.0), 28),
        ("16_booth", polar(112.0, 4.0, 30.5), polar(136.0, 0.0, 28.6), 40),
        ("17_air", (250.0, -360.0, 250.0), (0.0, 0.0, 30.0), 20),
        ("18_canopy_from_air", polar(230.0, 20.0, 120.0), polar(170.0, 0.0, 68.0), 30),
    ]
    flat = [
        ("f1_flat_under_canopy", polar(160.0, 190.0, 60.5), polar(168.0, 170.0, 68.0), 22),
        ("f2_flat_mast_and_girder", polar(205.0, 190.0, 50.0), polar(184.5, 180.0, 62.0), 28),
        ("f3_flat_scoreboard", (16.0, -20.0, 36.0), (0.0, 0.0, 46.5), 30),
        ("f4_flat_corner_screen_back", polar(182.0, 54.0, 30.0), polar(153.0, 45.0, 36.0), 24),
        ("f5_flat_nose_and_banks", polar(141.0, 186.0, 63.0), polar(151.5, 176.0, 68.0), 35),
        ("f6_flat_mast_foot", polar(204.0, 205.0, 3.5), polar(189.0, 199.5, 4.0), 26),
    ]
    check = [
        ("c1_checker_canopy_air", polar(215.0, 200.0, 110.0), polar(170.0, 180.0, 68.0), 35),
        ("c2_checker_under_canopy", polar(160.0, 190.0, 60.5), polar(168.0, 170.0, 68.0), 22),
        ("c3_checker_scoreboard", (16.0, -20.0, 40.0), (0.0, 0.0, 46.5), 30),
        ("c4_checker_corner_screen", polar(182.0, 54.0, 30.0), polar(153.0, 45.0, 36.0), 24),
    ]
    os.makedirs(LOGS, exist_ok=True)
    want = lambda n: which == "all" or any(w and w in n for w in which.split(","))
    scene.render.engine = eevee
    for name, loc, tgt, lens in painted:
        if want(name):
            shoot(p + name, loc, tgt, lens)
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.show_cavity = True
    scene.display.shading.show_object_outline = True
    for name, loc, tgt, lens in flat:
        if want(name):
            shoot(p + name, loc, tgt, lens)
    if any(want(n) for n, _, _, _ in check):
        materials("checker")
        scene.display.shading.color_type = "TEXTURE"
        scene.display.shading.light = "FLAT"
        scene.display.shading.show_cavity = False
        for name, loc, tgt, lens in check:
            if want(name):
                shoot(p + name, loc, tgt, lens)


# ---------------------------------------------------------------- main
def build():
    coll = K.collection("arena_roof")
    lugs = {}
    for lo, hi in UPPER_ARCS:
        mid = int(round((lo + hi) / 2)) % 360
        plate(coll, mid)
        canopy_steel(coll, mid)
        _, lug = floods(coll, mid)
        lugs[float(mid)] = lug
    scoreboard(coll, lugs)
    for b in (45, 135, 225, 315):
        corner_screen(coll, b)
    booth(coll)
    for name, where, colour in (
            ("canopy front edge", "r 150.0, z 68.6 to 69.3, on every canopy", "deep blue #0a1a9a and white, alternating by bay"),
            ("canopy rear edge", "r 190.0, z 67.0 to 67.5, on every canopy", "deep blue #0a1a9a"),
            ("scoreboard top ribbon", "r 9.4, z 50.9 to 51.6", "white"),
            ("scoreboard bottom ribbon", "r 7.6, z 43.4 to 44.1", "deep blue #0a1a9a"),
            ("scoreboard soffit ring", "r 5.8 to 6.2, z 42.6", "white"),
            ("booth eave", "r 131.7 to 132.4, z 29.7, bearings -5 to 5", "gold #ffc860")):
        LIGHTS["led_strips"].append({"name": name, "where": where, "colour": colour})
    return coll


def write_lights():
    data = {}
    if os.path.exists(LIGHTS_JSON):
        try:
            with open(LIGHTS_JSON, encoding="utf-8") as f:
                data = json.load(f)
        except ValueError:
            data = {}
    data["roof"] = {
        "frame": "metres, Blender axes: x east, y north, z up; the can is the origin. Unity: x = x, y = z, z = y.",
        "note": "Beams through haze are not geometry of this kit. Each floodlight bank is a lamp face 9.5 m wide by "
                "1.9 m tall whose normal is `direction`; put the light and the beam's near end at `position`.",
        **LIGHTS}
    with open(LIGHTS_JSON, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=1)
    print("LIGHTS %d floodlight banks written to %s" % (len(LIGHTS["floodlight_banks"]), LIGHTS_JSON))


def main():
    version, which = "v1", "all"
    for a in sys.argv:
        if a.startswith("--version="):
            version = a.split("=", 1)[1]
        if a.startswith("--shots="):
            which = a.split("=", 1)[1]
    bpy.ops.wm.read_factory_settings()
    for ob in list(bpy.data.objects):
        bpy.data.objects.remove(ob)
    for c in list(bpy.data.collections):
        bpy.data.collections.remove(c)
    for block in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for item in list(block):
            block.remove(item)
    materials("paint")
    coll = build()
    objs = list(coll.objects)
    report(objs)
    coplanar(objs)
    write_lights()
    os.makedirs(KITS, exist_ok=True)
    path = os.path.join(KITS, "roof.blend")
    bpy.ops.wm.save_as_mainfile(filepath=path)
    bpy.ops.file.make_paths_relative()
    bpy.ops.wm.save_mainfile()
    print("ROOF_SAVED %s" % path)
    if which != "none":
        shots(version, which)
    print("ROOF_OK")


if __name__ == "__main__":
    main()
