"""The arena's HULL kit: the saucer the stadium rides on (ARENA-1.4).

    blender -b --python tools/author_arena_hull.py -- --version=v1 [--checker] [--no-render]

Read docs/ARENA_ART_BRIEF.md first. Paint the textures before building:
    py -3 tools/author_arena_textures_hull.py
Builds ArtSource/arena/kits/hull.blend (every object in the collection `arena_hull`, already in the
stadium's frame: the can is the origin, z up, y north, metres) and review pictures in
Logs/arena/hull/. The review pictures add the blockout's bowl as a plain grey backdrop; the
backdrop is never saved into the kit.

WHAT IS HERE, by who sees it:
  1. THE SHAFT (players look down it all match, and fall into it). One closed liner, radius 41,
     from inside the bowl's kerb solid at z -10 to a lip under the hull at z -80: sixteen ribs
     that are part of the wall's own surface, three ring ledges (catwalks with a parapet, a light
     band on the fascia, a corbel underneath), two steel hoops, and at the bottom the exit ring:
     a flange lit on its upper bevel, so the city is seen through a bright ring. Machinery stands
     on the ledges with its back in the wall: cabinets, tanks, coil emitters, vent stacks, ducts.
     NOTHING of the hull is inside radius 36.6.
  2. THE SILHOUETTE AND THE UNDERSIDE (the break camera and the map's preview). One closed lathe:
     deck, rim, flank, four plated tiers stepped by ring frames, the keel plate. Eight engines at
     r 176: a mount drum, a neck, a turbine bulge, a throat, a bell with a glowing inside and a
     dark plug hanging in the glow, six gussets and six actuator struts each. Eight keel fins (two builds), eight
     hung pods and tank racks (alternating), sixteen vents, sixteen antennas, running lights.
  3. THE PLAZA (seen from the air). Paving laid in rings, a light ring and a drain let into it,
     four gate halls (a service core in the stand's back, a glazed hall, a canopy on columns with
     beams and rooflights, five doors, a turnstile line, a sign), 32 lamps, planters, benches and
     kiosks, a glass balustrade, four landing pads on braced arms, three parked craft (two builds).
     The flank under the rim carries a row of lit windows, sixteen frames and the running lights.

RULES KEPT (brief, "Rules for every model"): rings are one closed profile swept round; a part is
built INTO what it joins; no face shares a plane with another; UVs are laid at 64 px per metre
(16 m per tile) and checked with `--checker`. Eleven materials, arena_hull_<surface>.
"""
import bpy, bmesh, math, os, sys
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from arena_kit import polar, circle, collection, ROOT, PIT_R, DECK_Z, UPPER_ARCS  # noqa: E402

KIT = os.path.join(ROOT, "ArtSource", "arena", "kits")
TEX = os.path.join(ROOT, "Assets", "TumbangPreso", "Art", "Arena", "Textures")
LOGS = os.path.join(ROOT, "Logs", "arena", "hull")
T = 16.0                                      # metres per texture tile
S = 1024.0
SHAFT_R = PIT_R + 1.0                         # the shaft wall, 1 m outside the bowl's kerb
RIBS = 16
BAY = 360.0 / RIBS
SHAFT_T = 2 * math.pi * SHAFT_R / RIBS        # one bay of wall is one tile of arena_hull_shaft

# surface: (emission strength, flat review colour)
SURFACES = {
    "plate_a": (0.0, (0.20, 0.24, 0.36)), "plate_b": (0.0, (0.33, 0.38, 0.52)), "paving": (0.0, (0.45, 0.46, 0.55)),
    "steel": (0.0, (0.55, 0.58, 0.64)), "glass": (0.8, (0.75, 0.68, 0.45)), "light": (2.8, (0.10, 0.30, 0.90)),
    "engine": (0.0, (0.30, 0.30, 0.36)), "glow": (7.0, (0.40, 0.90, 1.00)), "shaft": (1.6, (0.18, 0.21, 0.33)),
    "pad": (3.0, (0.25, 0.28, 0.40)), "trim": (2.0, (0.60, 0.50, 0.30)),
}
HAS_EMIT = ("glass", "light", "glow", "shaft", "pad", "trim")
CHECKER = "--checker" in sys.argv

# Atlas regions in image pixels (x0, y0, x1, y1), top-left origin: the same numbers as in
# tools/author_arena_textures_hull.py.
GLASS = {"curtain": (0, 0, 1024, 640), "ports": (0, 656, 1024, 768), "dark": (0, 784, 1024, 912), "rail": (0, 928, 1024, 1024)}
TRIM = {
    "sign_n": (0, 0, 384, 96), "sign_e": (0, 96, 384, 192), "sign_s": (0, 192, 384, 288), "sign_w": (0, 288, 384, 384),
    "door": (400, 0, 560, 208), "cabinet": (576, 0, 832, 208), "livery": (848, 0, 1024, 208),
    "vent": (400, 224, 1024, 384), "hazard": (0, 400, 1024, 464), "hedge": (0, 480, 1024, 736),
    "shuttle": (0, 752, 640, 944), "coil": (656, 752, 1024, 1008),
}
LIGHT = {"blue": 0, "magenta": 1, "cyan": 2, "white": 3, "blue_dash": 4, "magenta_dash": 5, "cyan_dash": 6, "white_dash": 7}


def region(box, inset=2.0, u=None):
    """An atlas region as (u0, v0, u1, v1). `u` overrides the u range (for rows that tile in u)."""
    x0, y0, x1, y1 = box
    r = [(x0 + inset) / S, 1.0 - (y1 - inset) / S, (x1 - inset) / S, 1.0 - (y0 + inset) / S]
    if u is not None:
        r[0], r[2] = u
    return tuple(r)


def light_v(name):
    k = LIGHT[name]
    return 1.0 - (k * 128 + 104) / S, 1.0 - (k * 128 + 24) / S


# ---------------------------------------------------------------- materials
_mats = {}
_grid = None


def material(surface):
    if surface in _mats:
        return _mats[surface]
    global _grid
    glow, flat = SURFACES[surface]
    m = bpy.data.materials.new("arena_hull_%s" % surface)
    m.use_nodes = True
    m.diffuse_color = (flat[0], flat[1], flat[2], 1.0)
    nt = m.node_tree
    b = nt.nodes.get("Principled BSDF")
    b.inputs["Roughness"].default_value = 0.85
    tex = nt.nodes.new("ShaderNodeTexImage")
    if CHECKER:
        if _grid is None:
            _grid = bpy.data.images.new("checker", 1024, 1024)
            _grid.generated_type = "COLOR_GRID"
        tex.image = _grid
    else:
        tex.image = bpy.data.images.load(os.path.join(TEX, "arena_hull_%s.png" % surface), check_existing=True)
    nt.links.new(tex.outputs["Color"], b.inputs["Base Color"])
    if surface in HAS_EMIT and not CHECKER:
        em = nt.nodes.new("ShaderNodeTexImage")
        em.image = bpy.data.images.load(os.path.join(TEX, "arena_hull_%s_emit.png" % surface), check_existing=True)
        nt.links.new(em.outputs["Color"], b.inputs["Emission Color"])
        b.inputs["Emission Strength"].default_value = glow
    _mats[surface] = m
    return m


# ---------------------------------------------------------------- the mesh builder
def plane_uv(points, tile=T):
    """UVs for a flat face laid in its own plane at world scale: no stretch on any face."""
    n = Vector((0, 0, 0))
    for k in range(len(points)):
        p, q = points[k], points[(k + 1) % len(points)]
        n += Vector(((p.y - q.y) * (p.z + q.z), (p.z - q.z) * (p.x + q.x), (p.x - q.x) * (p.y + q.y)))
    if n.length < 1e-9:
        return [(0.0, 0.0)] * len(points)
    n.normalize()
    if abs(n.z) > 0.92:
        t = Vector((1, 0, 0)); bt = Vector((0, 1, 0))
    else:
        t = Vector((0, 0, 1)).cross(n).normalized(); bt = n.cross(t)
    return [(p.dot(t) / tile, p.dot(bt) / tile) for p in points]


BOX_FACES = {   # corners as (sx, sy, sz), listed bottom-left, bottom-right, top-right, top-left seen from outside
    "+y": ((1, 1, -1), (-1, 1, -1), (-1, 1, 1), (1, 1, 1)), "-y": ((-1, -1, -1), (1, -1, -1), (1, -1, 1), (-1, -1, 1)),
    "+x": ((1, -1, -1), (1, 1, -1), (1, 1, 1), (1, -1, 1)), "-x": ((-1, 1, -1), (-1, -1, -1), (-1, -1, 1), (-1, 1, 1)),
    "+z": ((-1, -1, 1), (1, -1, 1), (1, 1, 1), (-1, 1, 1)), "-z": ((1, -1, -1), (-1, -1, -1), (-1, 1, -1), (1, 1, -1)),
}


class Build:
    """One object: closed solids and swept surfaces, every face with a material and UVs."""

    def __init__(self):
        self.bm = bmesh.new()
        self.uv = self.bm.loops.layers.uv.new("UVMap")
        self.mats = []

    def face(self, verts, surface, uvs=None):
        vs = []
        keep = []
        for k, v in enumerate(verts):
            if v not in vs:
                vs.append(v); keep.append(k)
        if len(vs) < 3:
            return None
        try:
            f = self.bm.faces.new(vs)
        except ValueError:
            return None
        if surface not in self.mats:
            self.mats.append(surface)
        f.material_index = self.mats.index(surface)
        if uvs is None:
            uvs = plane_uv([v.co for v in vs])
        else:
            uvs = [uvs[k] for k in keep]
        for loop, uv in zip(f.loops, uvs):
            loop[self.uv].uv = uv
        return f

    # ---- a box: local x across, y along `yaw` (a compass bearing), z up
    def box(self, centre, size, yaw=0.0, pitch=0.0, surface="steel", skin=None):
        hx, hy, hz = size[0] / 2, size[1] / 2, size[2] / 2
        cy, sy = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
        cp, sp = math.cos(math.radians(pitch)), math.sin(math.radians(pitch))
        vs = {}
        for sx in (-1, 1):
            for syy in (-1, 1):
                for sz in (-1, 1):
                    x, y, z = sx * hx, syy * hy, sz * hz
                    y, z = y * cp - z * sp, y * sp + z * cp          # pitch about x: + lifts the far end
                    x, y = x * cy + y * sy, -x * sy + y * cy         # yaw, clockwise from north
                    vs[(sx, syy, sz)] = self.bm.verts.new((centre[0] + x, centre[1] + y, centre[2] + z))
        for key, corners in BOX_FACES.items():
            surf, rect = (skin or {}).get(key, (surface, None))
            uvs = None
            if rect is not None:
                uvs = [(rect[0], rect[1]), (rect[2], rect[1]), (rect[2], rect[3]), (rect[0], rect[3])]
            self.face([vs[c] for c in corners], surf, uvs)
        return self

    # ---- a closed prism from p0 to p1: a mast, a strut, a pipe, a tank
    def tube(self, p0, p1, radius, sides=8, radius1=None, surface="steel", ends=None):
        p0, p1 = Vector(p0), Vector(p1)
        axis = (p1 - p0).normalized()
        ref = Vector((0, 0, 1)) if abs(axis.z) < 0.9 else Vector((1, 0, 0))
        u = axis.cross(ref).normalized(); v = axis.cross(u)
        ra, rb = radius, radius if radius1 is None else radius1
        ang = [2 * math.pi * i / sides for i in range(sides)]
        a = [self.bm.verts.new(p0 + (u * math.cos(t) + v * math.sin(t)) * ra) for t in ang]
        b = [self.bm.verts.new(p1 + (u * math.cos(t) + v * math.sin(t)) * rb) for t in ang]
        girth = 2 * math.pi * max(ra, rb) / T
        reps = max(1.0, round(girth * 4)) / 4
        length = (p1 - p0).length / T
        for i in range(sides):
            k = (i + 1) % sides
            u0, u1 = reps * i / sides, reps * (i + 1) / sides
            self.face((a[i], a[k], b[k], b[i]), surface, [(u0, 0), (u1, 0), (u1, length), (u0, length)])
        self.face(a[::-1], ends or surface)
        self.face(b, ends or surface)
        return self

    # ---- a body lofted through rings of equal count, capped at both ends
    def loft(self, rings, surf=None, surface="steel"):
        cols = [[self.bm.verts.new(p) for p in ring] for ring in rings]
        n = len(rings[0])
        for s in range(len(cols) - 1):
            for q in range(n):
                k = (q + 1) % n
                quad = (cols[s][q], cols[s][k], cols[s + 1][k], cols[s + 1][q])
                name, fn = surf(s, q) if surf else (surface, None)
                self.face(quad, name, [fn(v.co) for v in quad] if fn else None)
        self.face(cols[0][::-1], surface)
        self.face(cols[-1], surface)
        return self

    # ---- a profile swept round a centre: the lathe, with UVs
    def sweep(self, angles, profile, specs, paint=None, full=True, origin=(0.0, 0.0, 0.0), cap="steel"):
        """`profile(i)` gives column i's closed loop of (r, z, tag); the tag names the strip from
        that point to the next. `specs[tag]` is (surface, mode, param):
            polar  u runs round (param tiles per turn, or None for the count nearest 16 m), v down the profile
            plan   laid flat in x and y
            vrow   u as polar, v from param[0] at this point to param[1] at the next (atlas rows)
            local  u is the quad's own width, v down the profile (narrow radial faces)
            a function(a0, a1, corners, columns) answering four UVs
        `paint(tag, i, a0, a1)` may answer another tag for one column."""
        o = Vector(origin)
        m = len(angles)
        cols, pts, cum = [], [], []
        axis = {}
        for i, a in enumerate(angles):
            prof = profile(i)
            col, cc, d = [], [0.0], 0.0
            for j, (r, z, _) in enumerate(prof):
                if r < 1e-6:
                    if j not in axis:
                        axis[j] = self.bm.verts.new(o + Vector((0, 0, z)))
                    col.append(axis[j])
                else:
                    col.append(self.bm.verts.new(o + polar(r, a, z)))
                r1, z1, _ = prof[(j + 1) % len(prof)]
                d += math.hypot(r1 - r, z1 - z)
                cc.append(d)
            cols.append(col); pts.append(prof); cum.append(cc)
        n = len(pts[0])
        for i in range(m if full else m - 1):
            i1 = (i + 1) % m
            a0 = angles[i]
            a1 = angles[i1] if i + 1 < m else angles[0] + 360.0
            for j in range(n):
                k = (j + 1) % n
                tag = pts[i][j][2]
                if paint:
                    tag = paint(tag, i, a0, a1)
                surface, mode, param = specs[tag]
                quad = (cols[i][j], cols[i][k], cols[i1][k], cols[i1][j])
                rs = (pts[i][j][0], pts[i][k][0], pts[i1][k][0], pts[i1][j][0])
                va, vb, vc, vd = cum[i][j] / T, cum[i][j + 1] / T, cum[i1][j + 1] / T, cum[i1][j] / T
                if callable(mode):
                    uvs = mode(a0, a1, quad, (pts[i][j], pts[i][k], pts[i1][k], pts[i1][j]))
                elif mode == "plan":
                    uvs = [((v.co.x) / T, (v.co.y) / T) for v in quad]
                elif mode == "local":
                    w = ((quad[0].co + quad[1].co) / 2 - (quad[2].co + quad[3].co) / 2).length / T
                    uvs = [(0, va), (0, vb), (w, vc), (w, vd)]
                else:
                    turns = param if (mode == "polar" and param) else max(1, round(2 * math.pi * sum(rs) / 4 / T))
                    u0, u1 = a0 / 360.0 * turns, a1 / 360.0 * turns
                    if mode == "vrow":
                        uvs = [(u0, param[0]), (u0, param[1]), (u1, param[1]), (u1, param[0])]
                    else:
                        uvs = [(u0, va), (u0, vb), (u1, vc), (u1, vd)]
                self.face(quad, surface, uvs)
        if not full:
            self.face(cols[0], cap)
            self.face(cols[-1][::-1], cap)
        return self

    def done(self, name, coll):
        bmesh.ops.remove_doubles(self.bm, verts=self.bm.verts, dist=0.0005)
        bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces)
        me = bpy.data.meshes.new(name)
        self.bm.to_mesh(me); self.bm.free()
        for s in self.mats:
            me.materials.append(material(s))
        ob = bpy.data.objects.new(name, me)
        coll.objects.link(ob)
        return ob


def at(r, a, x=0.0, y=0.0, z=0.0):
    """A point x metres to the right and y metres outward of the point (r, bearing a), at height z."""
    p = polar(r + y, a) + polar(x, a + 90.0)
    return Vector((p.x, p.y, z))


# ---------------------------------------------------------------- 1. the shaft
LEDGES = ((-26.0, 37.6, 3.4, "ringL1"), (-44.0, 36.6, 4.2, "ringL2"), (-62.0, 37.6, 3.4, "ringL3"))
RIB_R = 39.8
RIB_TOP, RIB_FOOT = -15.6, -70.8


def shaft_profile():
    w = SHAFT_R
    p = [(w, -10.0, "wallT"), (w, -12.6, "ringA"), (w, -13.4, "collar"), (w, -14.4, "wallA"), (w, RIB_TOP, "wallA")]
    walls = ("wallB", "wallC", "wallD")
    hoops = (-36.0, -54.5, None)
    for (zf, re, drop, ring), wall, hoop in zip(LEDGES, walls, hoops):
        p += [(w, zf, "floor"), (re + 0.4, zf, "steel"), (re + 0.4, zf + 1.0, "steel"), (re, zf + 1.0, "steel"),
              (re, zf - 0.15, ring), (re, zf - 0.75, "steel"), (re + 0.3, zf - 1.1, "under"), (w, zf - drop, wall)]
        if hoop is not None:
            p += [(w, hoop, "steel"), (w - 0.5, hoop - 0.3, "steel"), (w - 0.5, hoop - 0.9, "steel"), (w, hoop - 1.2, wall)]
    p += [(w, RIB_FOOT, "wallD"), (w, -72.0, "steel"), (40.0, -72.9, "ringC"), (39.3, -73.55, "steel"), (38.8, -74.0, "steel"), (38.6, -74.3, "steel"),
          (38.6, -77.0, "ringD"), (40.0, -79.4, "steel"), (43.0, -80.0, "plate"), (47.0, -78.2, "plate"), (48.0, -76.0, "hid"),
          (45.0, -74.0, "hid"), (45.0, -10.0, "hid")]
    return p


def shaft(coll):
    """The liner: ONE closed surface. Eight columns per bay: a rib (two chamfered sides, a face
    with a light channel down its middle) and three facets of wall."""
    base = shaft_profile()
    rib = [(min(r, RIB_R) if RIB_FOOT <= z <= RIB_TOP and r <= SHAFT_R else r, z, t) for r, z, t in base]
    offsets = (-2.2, -1.5, -0.35, 0.35, 1.5, 2.2, 2.2 + (BAY - 4.4) / 3, 2.2 + 2 * (BAY - 4.4) / 3)
    angles = [k * BAY + o for k in range(RIBS) for o in offsets]

    def profile(i):
        return rib if 1 <= i % 8 <= 4 else base

    def wall_uv(a0, a1, quad, corners):
        k = int((a0 + 2.2) // BAY)
        flip, shift = (k % 3 == 1), (k % 4) / 4.0 + (0.37 if k % 5 == 2 else 0.0)
        out = []
        for a, (r, z, _) in zip((a0, a0, a1, a1), corners):
            u = (a - k * BAY) / BAY
            out.append((1.0 - u if flip else u, z / SHAFT_T + shift))
        return out

    specs = {
        "wall": ("shaft", wall_uv, None), "rib": ("steel", "polar", None), "ribside": ("steel", "local", None),
        "riblight": ("light", "vrow", light_v("blue")), "steel": ("steel", "polar", None), "collar": ("steel", "polar", None),
        "floor": ("steel", "polar", None), "under": ("plate_a", "polar", None), "plate": ("plate_a", "polar", None),
        "hid": ("steel", "polar", None), "ringA": ("light", "vrow", light_v("blue")),
        "ringL1": ("light", "vrow", light_v("blue_dash")), "ringL2": ("light", "vrow", light_v("magenta")),
        "ringL3": ("light", "vrow", light_v("blue_dash")), "ringC": ("light", "vrow", light_v("cyan")),
        "ringD": ("light", "vrow", light_v("blue")),
    }

    def paint(tag, i, a0, a1):
        if not tag.startswith("wall"):
            return tag
        q = i % 8
        if q >= 5:
            return "wall"
        if q in (0, 4):
            return "ribside"
        if q == 2 and tag in ("wallB", "wallC") and (i // 8) % 2 == 0:
            return "riblight"
        return "rib"

    b = Build()
    b.sweep(angles, profile, specs, paint=paint)
    b.done("shaft liner (wall, ribs, ledges, exit ring)", coll)

    # Machinery: every piece stands in a ledge's floor with its back in the wall.
    g = Build()
    for k in range(RIBS):
        a = k * BAY + BAY / 2
        z1, z2, z3 = LEDGES[0][0], LEDGES[1][0], LEDGES[2][0]
        if k % 4 == 0:                                            # ledge 1: a cabinet row
            g.box(at(40.3, a, z=z1 + 1.1), (6.2, 2.2, 2.6), yaw=a, surface="steel",
                  skin={"-y": ("trim", region(TRIM["cabinet"]))})
            g.box(at(40.2, a, x=4.6, z=z1 + 0.7), (1.6, 2.2, 1.8), yaw=a, surface="plate_b")
        elif k % 4 == 2:                                          # ledge 1: two tanks piped into the wall
            for dx in (-2.0, 2.0):
                foot = at(39.9, a, x=dx, z=z1 - 0.2)
                g.tube(foot, foot + Vector((0, 0, 3.4)), 0.95, 10, surface="plate_b", ends="steel")
                g.tube(foot + Vector((0, 0, 2.6)), at(41.6, a, x=dx, z=z1 + 2.4), 0.22, 6)
        else:                                                     # a duct up the wall above ledge 1
            g.box(at(40.75, a, x=(-3.0 if k % 4 == 1 else 3.0), z=(z1 - 13.0) / 2 - 0.1), (1.6, 1.1, 13.4),
                  yaw=a, surface="plate_b")
        if k % 2 == 1:                                            # ledge 2: a coil emitter in every other bay
            g.box(at(39.8, a, z=z2 + 1.7), (7.4, 3.0, 3.8), yaw=a, surface="steel",
                  skin={"-y": ("trim", region(TRIM["coil"]))})
            for dx in (-4.4, 4.4):                                # its two feed pipes, into the wall
                g.tube(at(39.6, a, x=dx, z=z2 + 2.6), at(41.5, a, x=dx, z=z2 + 2.6), 0.3, 6)
                g.tube(at(39.6, a, x=dx * 0.86, z=z2 + 2.6), at(39.6, a, x=dx, z=z2 + 2.6), 0.3, 6)
        else:                                                     # ledge 2: a duct between the ledges, and a cabinet
            g.box(at(40.75, a, x=2.4, z=(z1 - LEDGES[0][2] + z2) / 2 + 0.5), (1.5, 1.1, z1 - LEDGES[0][2] - z2 + 1.6), yaw=a,
                  surface="plate_b")
            g.box(at(40.3, a, x=-2.6, z=z2 + 1.0), (3.2, 2.2, 2.4), yaw=a, surface="steel",
                  skin={"-y": ("trim", region(TRIM["cabinet"], u=(580 / S, 742 / S)))})
        if k % 4 == 1:                                            # ledge 3: a vent stack
            g.box(at(40.2, a, z=z3 + 1.3), (9.75, 2.4, 3.0), yaw=a, surface="steel",
                  skin={"-y": ("trim", region(TRIM["vent"]))})
        elif k % 4 == 3:                                          # ledge 3: three tanks
            for dx in (-2.4, 0.0, 2.4):
                foot = at(39.9, a, x=dx, z=z3 - 0.2)
                g.tube(foot, foot + Vector((0, 0, 2.8)), 0.9, 10, surface="plate_b", ends="steel")
            g.tube(at(39.9, a, x=-2.4, z=z3 + 2.2), at(39.9, a, x=2.4, z=z3 + 2.2), 0.2, 6)
        else:                                                     # a duct between ledges 2 and 3
            g.box(at(40.75, a, x=-2.6, z=(z2 - LEDGES[1][2] + z3) / 2 + 0.5), (1.5, 1.1, z2 - LEDGES[1][2] - z3 + 1.6), yaw=a,
                  surface="plate_b")
    g.done("shaft machinery (cabinets, tanks, coils, vents, ducts)", coll)


# ---------------------------------------------------------------- 2. the hull body
HULL_COLS = 128


def tier2_z(r):
    return -44.6 - (194.0 - r) * (60.5 - 44.6) / (194.0 - 152.6)


def tier3_z(r):
    return -62.6 - (150.6 - r) * (72.2 - 62.6) / (150.6 - 94.2)


def hull_body(coll):
    def run(r0, z0, r1, z1, n, tag):
        return [(r0 + (r1 - r0) * k / n, z0 + (z1 - z0) * k / n, tag) for k in range(n)]

    z = DECK_Z
    p = [(43.0, z, "hid")]
    p += run(139.0, z, 165.0, z, 3, "paveA") + [(165.0, z, "drain")] + run(165.6, z, 196.0, z, 4, "paveB")
    p += [(196.0, z, "glowring")] + run(196.5, z, 229.0, z, 4, "paveC")
    p += [(229.0, z, "steel"), (230.6, z, "steel"), (231.0, -2.5, "shoulder"), (234.2, -3.3, "rimlight"), (236.0, -3.9, "steel"),
          (237.2, -4.5, "nose"), (239.4, -6.2, "steel"), (239.8, -6.8, "steel"), (239.8, -7.15, "noselight"), (239.8, -7.65, "steel"), (239.8, -8.0, "steel"),
          (239.2, -9.2, "nose"),
          (235.5, -11.2, "steel"), (234.2, -11.2, "steel"), (233.6, -13.0, "flank"), (232.9, -15.0, "ports"), (232.3, -16.75, "flank"), (231.5, -19.0, "runlight"),
          (231.33, -19.45, "flank"), (228.6, -26.5, "steel"), (228.9, -27.6, "steel"), (226.6, -28.8, "steel")]
    p += run(225.6, -28.0, 198.6, -41.2, 3, "tier1")
    p += [(198.6, -41.2, "steel"), (198.6, -43.0, "steel"), (197.5, -43.45, "tierlight"), (196.3, -43.95, "steel")]
    p += run(194.0, -44.6, 152.6, -60.5, 4, "tier2")
    p += [(152.6, -60.5, "steel"), (152.6, -62.2, "steel")]
    p += run(150.6, -62.6, 94.2, -72.2, 5, "tier3")
    p += [(94.2, -72.2, "steel"), (94.2, -73.6, "steel")]
    p += run(92.4, -73.9, 62.0, -76.4, 2, "tier4")
    p += [(62.0, -76.4, "keellight"), (60.8, -76.5, "tier4"), (46.0, -76.6, "hid"), (43.0, -76.6, "hid")]
    specs = {
        "hid": ("steel", "polar", None), "paveA": ("paving", "polar", 64), "paveB": ("paving", "polar", 72),
        "paveC": ("paving", "polar", 80), "drain": ("steel", "polar", None), "steel": ("steel", "polar", None),
        "glowring": ("light", "vrow", light_v("blue")), "rimlight": ("light", "vrow", light_v("blue")),
        "noselight": ("light", "vrow", light_v("magenta_dash")), "runlight": ("light", "vrow", light_v("white_dash")),
        "ports": ("glass", "vrow", (1.0 - (GLASS["ports"][3] - 3) / S, 1.0 - (GLASS["ports"][1] + 3) / S)),
        "tierlight": ("light", "vrow", light_v("blue")), "keellight": ("light", "vrow", light_v("cyan")),
        "shoulder": ("plate_b", "polar", 88), "nose": ("plate_a", "polar", 96), "flank": ("plate_a", "polar", 88),
        "a1": ("plate_a", "polar", 80), "b1": ("plate_b", "polar", 80), "a2": ("plate_a", "polar", 64),
        "b2": ("plate_b", "polar", 64), "a3": ("plate_a", "polar", 48), "b3": ("plate_b", "polar", 48),
        "a4": ("plate_a", "polar", 32),
    }

    def paint(tag, i, a0, a1):
        sector = int(((a0 + a1) / 2 % 360.0) // 45.0)
        if tag == "tier1":
            return "b1"
        if tag == "tier2":
            return "a2" if sector % 2 == 0 else "b2"
        if tag == "tier3":
            return "b3" if sector % 2 == 0 else "a3"
        if tag == "tier4":
            return "a4"
        return tag

    b = Build()
    b.sweep(circle(HULL_COLS), lambda i: p, specs, paint=paint)
    b.done("hull body (deck, rim, flank, tiers, keel)", coll)


# ---------------------------------------------------------------- 3. engines, fins, hung gear
def engines(coll):
    prof = [(0, -40.0, "eng"), (21.0, -40.0, "eng"), (21.0, -58.0, "steel"), (21.6, -58.6, "steel"), (21.6, -59.4, "ring"),
            (21.6, -60.2, "steel"), (20.8, -60.8, "eng"), (15.5, -66.0, "eng"), (14.0, -67.0, "eng"), (14.0, -69.5, "eng"),
            (16.5, -71.5, "eng"), (16.5, -75.5, "eng"), (10.5, -82.0, "steel"), (10.5, -84.0, "eng"), (16.0, -92.0, "eng"),
            (20.5, -100.0, "steel"), (19.7, -100.6, "g0"), (15.0, -92.0, "g1"), (9.2, -84.5, "g2"), (5.5, -84.0, "plug"),
            (4.2, -89.0, "plug"), (0, -96.0, "plug")]
    specs = {"eng": ("engine", "polar", None), "steel": ("steel", "polar", None), "ring": ("light", "vrow", light_v("blue")),
             "g0": ("glow", "vrow", (0.30, 0.66)), "g1": ("glow", "vrow", (0.66, 0.93)), "g2": ("glow", "vrow", (0.93, 0.99)),
             "plug": ("engine", "polar", None)}
    struts = Build()
    for k in range(8):
        a = k * 45.0 + 22.5
        c = polar(176.0, a)
        b = Build()
        b.sweep(circle(24), lambda i: prof, specs, origin=(c.x, c.y, 0.0))
        b.done("engine %d" % k, coll)
        for s in range(6):                                        # gussets: the mount is braced back into the hull
            t = a + s * 60.0
            poly = [(20.0, -36.0), (29.0, -36.0), (21.4, -58.4)]
            left = [struts.bm.verts.new(c + polar(r, t) + polar(0.6, t + 90.0) + Vector((0, 0, z))) for r, z in poly]
            right = [struts.bm.verts.new(c + polar(r, t) - polar(0.6, t + 90.0) + Vector((0, 0, z))) for r, z in poly]
            struts.face(left, "plate_b"); struts.face(right[::-1], "plate_b")
            for k in range(3):
                struts.face((left[k], right[k], right[(k + 1) % 3], left[(k + 1) % 3]), "steel")
        for s in range(6):                                        # actuators from the mount flange into the bell
            t = a + 30.0 + s * 60.0
            p0, p1 = c + polar(21.1, t), c + polar(15.6, t + 14.0)
            struts.tube((p0.x, p0.y, -59.6), (p1.x, p1.y, -92.0), 0.55, 6)
    struts.done("engine gussets and actuators", coll)


def fin(b, a, poly, light=True):
    """A keel fin: a blade whose root is inside the underside, thick at the root, thin at the edge."""
    def thick(z):
        return 2.8 + (z + 40.0) * (2.0 / 44.0)

    left = [b.bm.verts.new(at(r, a, x=thick(z) / 2, z=z)) for r, z in poly]
    right = [b.bm.verts.new(at(r, a, x=-thick(z) / 2, z=z)) for r, z in poly]
    b.face(left, "plate_b")
    b.face(right[::-1], "plate_b")
    n = len(poly)
    for k in range(n):
        j = (k + 1) % n
        b.face((left[k], right[k], right[j], left[j]), "steel")
    if light:                                                     # a running light on the blade's heel
        r, z = min(poly, key=lambda q: q[1])
        b.box(at(r + 1.0, a, z=z + 0.9), (1.5, 2.4, 1.0), yaw=a, surface="light",
              skin={k: ("light", (0.1, light_v("white")[0], 0.2, light_v("white")[1])) for k in BOX_FACES})


def capsule(b, centre, yaw, length, radius, surface, sides=8):
    rings = []
    for f, s in ((-0.5, 0.35), (-0.36, 1.0), (0.36, 1.0), (0.5, 0.35)):
        ring = []
        for q in range(sides):
            t = 2 * math.pi * q / sides
            p = polar(f * length, yaw) + polar(math.cos(t) * radius * s, yaw + 90.0)
            ring.append(Vector((centre[0] + p.x, centre[1] + p.y, centre[2] + math.sin(t) * radius * s)))
        rings.append(ring)
    b.loft(rings, surface=surface)


def underside_gear(coll):
    f = Build()
    for k in range(8):
        a = k * 45.0
        if k % 2 == 0:
            fin(f, a, [(96.0, -66.0), (192.0, -40.0), (192.0, -50.5), (150.0, -74.0), (110.0, -85.0), (98.0, -80.0)])
        else:
            fin(f, a, [(122.0, -62.0), (192.0, -40.0), (192.0, -49.5), (164.0, -64.5), (136.0, -75.0), (124.0, -72.0)])
    f.done("keel fins", coll)

    white = {k: ("light", (0.1, light_v("white")[0], 0.2, light_v("white")[1])) for k in BOX_FACES}
    g = Build()
    for k in range(8):                                            # hung under tier 3, inboard of each engine
        a = k * 45.0 + 22.5
        hull_z = tier3_z(124.0)
        if k % 2 == 0:                                            # a sensor gondola on two pylons
            capsule(g, at(124.0, a, z=hull_z - 6.6), a, 15.0, 2.2, "plate_b")
            for dy in (-3.6, 3.6):
                g.box(at(124.0 + dy, a, z=hull_z - 2.4), (0.9, 2.4, 7.6), yaw=a, surface="steel")
            g.box(at(131.6, a, z=hull_z - 6.6), (0.9, 0.9, 0.9), yaw=a, skin=white)
        else:                                                     # a rack of three tanks in two cradles
            for dy in (-3.9, 0.0, 3.9):
                g.tube(at(124.0 + dy, a, x=-6.0, z=hull_z - 3.7), at(124.0 + dy, a, x=6.0, z=hull_z - 3.7), 1.7, 10,
                       surface="engine", ends="steel")
            for dx in (-3.6, 3.6):
                g.box(at(124.0, a, x=dx, z=hull_z - 1.6), (1.0, 12.4, 5.2), yaw=a, surface="steel")
    slope = math.degrees(math.atan2(60.5 - 44.6, 194.0 - 152.6))
    for k in range(8):                                            # vents on tier 2, either side of each fin
        for da in (-8.0, 8.0):
            a = k * 45.0 + da
            g.box(at(171.0, a, z=tier2_z(171.0) + 0.1), (9.75, 2.6, 1.5), yaw=a, pitch=slope, surface="steel",
                  skin={"-z": ("trim", region(TRIM["vent"]))})
    lean = -math.degrees(math.atan2(233.6 - 228.6, 26.5 - 13.0))
    for k in range(16):                                           # frames down the flank, from the nose's recess to the ring frame
        g.box(at(230.9, k * 22.5, z=-19.6), (1.4, 1.8, 18.0), yaw=k * 22.5, pitch=lean, surface="steel")
    for k in range(16):                                           # antennas hung from the flank
        a = k * 22.5 + 11.25
        long = k % 2 == 0
        tip = -62.0 if long else -45.0
        g.tube(at(229.6, a, z=-17.5), at(230.4, a, z=tip), 0.75, 6, 0.14)
        g.box(at(230.4, a, z=tip), (0.7, 0.7, 0.7), yaw=a, skin=white)
        if long:                                                  # a yard and a dish arm on the long ones
            g.box(at(230.0, a, z=-38.0), (6.4, 0.3, 0.3), yaw=a, surface="steel")
            g.tube(at(230.0, a, z=-30.0), at(234.6, a, z=-33.5), 0.2, 6)
            g.tube(at(234.6, a, z=-33.3), at(234.9, a, z=-34.4), 1.5, 10, 0.3, surface="plate_b")
    g.done("underside gear (gondolas, tank racks, vents, antennas)", coll)


# ---------------------------------------------------------------- 4. the plaza
def gate_halls(coll):
    for a, sign in ((0.0, "sign_n"), (90.0, "sign_e"), (180.0, "sign_s"), (270.0, "sign_w")):
        b = Build()
        curtain = GLASS["curtain"]
        v0 = 1.0 - curtain[3] / S + 0.004
        v1 = v0 + 10.0 * 64 / S

        def glazed(width):
            return ("glass", (0.0, v0, width / T, v1))

        # the service core: its back is inside the stand
        b.box(at(184.0, a, z=5.25), (40.0, 10.4, 15.5), yaw=a, surface="plate_b", skin={"+z": ("steel", None)})
        b.box(at(183.4, a, x=-11.0, z=13.9), (7.0, 5.0, 2.2), yaw=a, surface="steel",
              skin={"+y": ("trim", region(TRIM["cabinet"]))})       # plant on the core's roof
        b.box(at(183.4, a, x=(9.0 if a % 180 == 0 else 4.0), z=13.7), (9.75, 2.6, 1.8), yaw=a, surface="steel",
              skin={"+y": ("trim", region(TRIM["vent"]))})
        # the glazed hall
        b.box(at(196.4, a, z=2.85), (46.0, 14.8, 10.3), yaw=a, surface="steel",
              skin={"+y": glazed(46.0), "+x": glazed(14.8), "-x": glazed(14.8)})
        # the canopy: its back in the core, its front on six columns
        b.box(at(200.0, a, z=8.25), (54.0, 26.0, 0.7), yaw=a, surface="plate_a", skin={"-z": ("plate_b", None)})
        wv = light_v("white")
        b.box(at(213.2, a, z=8.45), (54.6, 0.7, 1.5), yaw=a, surface="steel")
        b.box(at(213.5, a, z=7.95), (54.0, 0.36, 0.24), yaw=a, surface="steel", skin={"+y": ("light", (0.0, wv[0], 54.0 / T, wv[1]))})
        for dx in (-22.5, -13.5, -4.5, 4.5, 13.5, 22.5):          # the roof: six beams, five rooflights between them
            b.box(at(200.6, a, x=dx, z=8.8), (0.6, 24.0, 0.6), yaw=a, surface="steel")
        for dx in (-18.0, -9.0, 0.0, 9.0, 18.0):
            b.box(at(203.0, a, x=dx, z=8.8), (6.0, 10.0, 0.5), yaw=a, surface="steel",
                  skin={"+z": ("glass", (0.02, v0 + 0.40, 0.02 + 6.0 / T, v0 + 0.40 + 10.0 * 64 / S * 0.33))})
        b.box(at(213.25, a, z=10.05), (6.4, 0.5, 2.0), yaw=a, surface="steel", skin={"+y": ("trim", region(TRIM[sign]))})
        for dx in (-25.0, -15.0, -5.0, 5.0, 15.0, 25.0):
            b.tube(at(211.4, a, x=dx, z=DECK_Z - 0.4), at(211.4, a, x=dx, z=8.1), 0.45, 8)
        # five doors and the turnstile line
        for dx in (-16.0, -8.0, 0.0, 8.0, 16.0):
            b.box(at(204.0, a, x=dx, z=-0.25), (3.4, 0.9, 3.9), yaw=a, surface="steel",
                  skin={"+y": ("trim", region(TRIM["door"]))})
            for t in (-1.15, 0.0, 1.15):
                b.box(at(208.0, a, x=dx + t, z=-1.55), (0.36, 1.7, 1.1), yaw=a, surface="steel")
        for dx in (-20.0, -12.0, -4.0, 4.0, 12.0, 20.0):
            wdt = 4.4 if abs(dx) < 19 else 3.0
            b.box(at(208.0, a, x=dx + (0.7 if dx > 19 else -0.7 if dx < -19 else 0.0), z=-1.6), (wdt, 0.16, 1.0), yaw=a,
                  surface="plate_b")
        b.done("gate hall %d" % a, coll)


def lamps(coll):
    b = Build()
    wv = light_v("white")
    lit = ("light", (0.1, wv[0], 0.2, wv[1]))
    for k in range(32):
        a = k * 11.25 + 5.625
        b.tube(at(222.0, a, z=DECK_Z - 0.4), at(222.0, a, z=7.0), 0.3, 6, 0.17)
        b.box(at(222.0, a, z=7.25), (0.7, 0.7, 0.6), yaw=a, skin={k: lit for k in BOX_FACES})
        b.box(at(222.0, a, z=7.62), (1.2, 1.2, 0.16), yaw=a, surface="steel")
    b.done("plaza lamps", coll)


def furniture(coll):
    """Planters with a clipped hedge, and benches, each one closed arc."""
    hedge = region(TRIM["hedge"])
    top_v = (hedge[1] + 0.02, hedge[1] + 0.02 + 1.7 * 64 / S)

    def planter(b, r0, r1, lo, hi):
        prof = [(r0, -2.3, "wall"), (r0, -1.25, "cope"), (r0 + 0.35, -1.25, "side"), (r0 + 0.35, -0.45, "top"),
                (r1 - 0.35, -0.45, "side2"), (r1 - 0.35, -1.25, "cope"), (r1, -1.25, "wall"), (r1, -2.3, "wall")]
        specs = {"wall": ("plate_b", "polar", None), "cope": ("steel", "polar", None),
                 "side": ("trim", "vrow", (hedge[1] + 0.01, hedge[1] + 0.01 + 0.8 * 64 / S)),
                 "side2": ("trim", "vrow", (hedge[1] + 0.01 + 0.8 * 64 / S, hedge[1] + 0.01)),
                 "top": ("trim", "vrow", (top_v[0] + 0.06, top_v[0] + 0.06 + (r1 - r0 - 0.7) * 64 / S))}
        n = max(2, int(round((hi - lo) / 2.5)))
        b.sweep([lo + (hi - lo) * k / n for k in range(n + 1)], lambda i: prof, specs, full=False, cap="plate_b")

    def bench(b, r0, lo, hi):
        prof = [(r0 + 0.15, -2.2, "s"), (r0 + 0.15, -1.62, "s"), (r0, -1.62, "s"), (r0, -1.5, "s"), (r0 + 0.9, -1.5, "s"),
                (r0 + 0.9, -1.62, "s"), (r0 + 0.75, -1.62, "s"), (r0 + 0.75, -2.2, "s")]
        n = max(2, int(round((hi - lo) / 2.5)))
        b.sweep([lo + (hi - lo) * k / n for k in range(n + 1)], lambda i: prof, {"s": ("steel", "polar", None)}, full=False)

    b = Build()
    for c in (45.0, 135.0, 225.0, 315.0):                         # the open corners: two rows either side of the way to the pad
        for sgn in (-1, 1):
            lo, hi = sorted((c + sgn * 3.2, c + sgn * 11.0))
            planter(b, 170.0, 173.0, lo, hi)
            bench(b, 175.6, lo + 0.6, hi - 0.6)
            lo, hi = sorted((c + sgn * 2.6, c + sgn * 9.0))
            planter(b, 203.0, 206.0, lo, hi)
            bench(b, 200.4, lo + 0.5, hi - 0.5)
            lo, hi = sorted((c + sgn * 14.0, c + sgn * 22.0))
            planter(b, 189.6, 192.2, lo, hi)                    # clear of the roof kit's mast plinths (r 184.5 and 197)
    v0 = 1.0 - GLASS["curtain"][3] / S + 0.004
    for c in (45.0, 135.0, 225.0, 315.0):                         # two kiosks in each corner: a lit front, an oversailing roof
        for sgn in (-1, 1):
            a = c + sgn * 6.5
            b.box(at(190.0, a, z=-0.3), (7.0, 4.5, 3.8), yaw=a, surface="plate_b",
                  skin={"+y": ("glass", (0.03, v0, 0.03 + 7.0 / T, v0 + 3.6 * 64 / S))})
            b.box(at(190.6, a, z=1.72), (8.6, 6.6, 0.3), yaw=a, surface="plate_a")
            for dx in (-3.9, 3.9):
                b.tube(at(193.3, a, x=dx, z=DECK_Z - 0.3), at(193.3, a, x=dx, z=1.7), 0.14, 6)
    for c in (0.0, 90.0, 180.0, 270.0):                           # either side of each gate hall
        for sgn in (-1, 1):
            lo, hi = sorted((c + sgn * 10.5, c + sgn * 17.5))
            planter(b, 207.0, 210.0, lo, hi)
            bench(b, 212.4, lo + 0.5, hi - 0.5)
            lo, hi = sorted((c + sgn * 21.0, c + sgn * 28.0))
            planter(b, 201.5, 204.5, lo, hi)
    b.done("plaza planters, benches and kiosks", coll)


def balustrade(coll):
    """Glass with a steel foot and rail, in four arcs that stop either side of each pad's arm."""
    rail = GLASS["rail"]
    gv = (1.0 - (rail[3] - 14) / S, 1.0 - (rail[1] + 16) / S)
    prof = [(229.45, -2.3, "steel"), (229.45, -1.78, "glass"), (229.45, -0.92, "steel"), (229.45, -0.78, "steel"),
            (229.65, -0.78, "steel"), (229.65, -0.92, "glass2"), (229.65, -1.78, "steel"), (229.65, -2.3, "steel")]
    specs = {"steel": ("steel", "polar", None), "glass": ("glass", "vrow", (gv[1], gv[0])), "glass2": ("glass", "vrow", gv)}
    step = 360.0 / HULL_COLS
    b = Build()
    for c in (45.0, 135.0, 225.0, 315.0):
        lo, hi = c + 0.86, c + 90.0 - 0.86
        angles = [lo] + [k * step for k in range(int(lo // step) + 1, int(hi // step) + 1)] + [hi]
        b.sweep(angles, lambda i: prof, specs, full=False)
    b.done("rim balustrade", coll)


# ---------------------------------------------------------------- 5. landing pads and craft
PAD_R = 254.0


def pads(coll):
    arms = Build()
    for c in (45.0, 135.0, 225.0, 315.0):
        centre = polar(PAD_R, c)
        ca, sa = math.cos(math.radians(c)), math.sin(math.radians(c))

        def art(a0, a1, quad, corners):                           # the drawing, its top toward the stadium
            out = []
            for v in quad:
                dx, dy = v.co.x - centre.x, v.co.y - centre.y
                right, inward = dx * ca - dy * sa, -(dx * sa + dy * ca)
                out.append((0.5 - right / 20.6, 0.5 + inward / 20.6))
            return out

        prof = [(0, -1.6, "art"), (5.0, -1.6, "art"), (10.0, -1.6, "ring"), (10.5, -1.6, "edge"), (13.0, -1.6, "steel"),
                (13.0, -2.0, "lamp"), (13.0, -2.4, "steel"), (12.6, -3.1, "under"), (6.0, -4.6, "steel"), (4.0, -6.8, "beacon"), (0, -7.8, "beacon")]
        specs = {"art": ("pad", art, None), "ring": ("light", "vrow", light_v("cyan")), "edge": ("plate_a", "plan", None),
                 "steel": ("steel", "polar", None), "lamp": ("light", "vrow", light_v("magenta_dash")),
                 "under": ("plate_b", "polar", None), "beacon": ("light", "vrow", light_v("white"))}
        b = Build()
        b.sweep(circle(32), lambda i: prof, specs, origin=(centre.x, centre.y, 0.0))
        b.done("landing pad %d" % c, coll)
        # the arm: a box girder from the deck into the pad, a glass parapet each side, two braces
        rail = GLASS["rail"]
        gl = ("glass", (0.0, 1.0 - (rail[3] - 14) / S, 12.4 / T, 1.0 - (rail[1] + 16) / S))
        arms.box(at(235.5, c, z=-2.9), (7.0, 17.0, 2.2), yaw=c, surface="plate_b", skin={"+z": ("paving", None)})
        arms.box(at(236.0, c, z=-4.6), (3.0, 14.0, 1.6), yaw=c, surface="steel")
        for dx in (-3.35, 3.35):
            arms.box(at(235.3, c, x=dx, z=-1.35), (0.2, 12.4, 1.1), yaw=c, surface="steel", skin={"+x": gl, "-x": gl})
            arms.tube(at(229.6, c, x=dx * 0.9, z=-21.0), at(250.0, c, x=dx * 0.5, z=-5.0), 0.6, 8)
        arms.tube(at(232.0, c, x=-3.0, z=-18.8), at(232.0, c, x=3.0, z=-18.8), 0.35, 6)
    arms.done("pad arms and braces", coll)


def craft(b, centre, yaw, lifter=False):
    """A parked craft, built on the pad: z is measured up from the pad's deck."""
    o = Vector((centre.x, centre.y, -1.6))

    def P(x, y, z):
        q = polar(y, yaw) + polar(x, yaw + 90.0)
        return Vector((o.x + q.x, o.y + q.y, o.z + z))

    sh = region(TRIM["shuttle"])
    lv = region(TRIM["livery"], inset=30)
    dk = region(GLASS["dark"], inset=30)
    if lifter:                                                    # a cargo lifter: a box hull on four fan pods
        sections = ((-3.6, 1.3, 1.0, 2.6), (-3.0, 1.9, 0.8, 3.1), (2.6, 1.9, 0.8, 3.1), (3.9, 1.5, 1.0, 2.5))
        length, cockpit = 7.5, 2
    else:                                                         # a passenger shuttle
        sections = ((-5.5, 0.9, 1.5, 2.5), (-4.0, 1.6, 0.9, 3.0), (-1.0, 1.9, 0.75, 3.2), (2.0, 1.8, 0.75, 3.1),
                    (4.2, 1.4, 0.95, 2.6), (5.6, 0.6, 1.25, 1.85))
        length, cockpit = 11.1, 3
    rings = []
    for y, w, zb, zt in sections:
        ch = min(0.55, w * 0.4)
        rings.append([P(w, y, zb + ch), P(w, y, zt - ch), P(w - ch, y, zt), P(-(w - ch), y, zt), P(-w, y, zt - ch),
                      P(-w, y, zb + ch), P(-(w - ch), y, zb), P(w - ch, y, zb)])
    y_lo = sections[0][0]

    def side(sign):
        def uv(co):
            d = co - o
            y = d.x * math.sin(math.radians(yaw)) + d.y * math.cos(math.radians(yaw))
            f = (y - y_lo) / length
            if sign < 0:
                f = 1.0 - f
            return (sh[0] + (sh[2] - sh[0]) * f, sh[1] + (sh[3] - sh[1]) * min(1.0, max(0.0, (d.z - 0.6) / 2.7)))
        return uv

    flat = lambda co: ((lv[0] + lv[2]) / 2, (lv[1] + lv[3]) / 2)
    dark = lambda co: ((dk[0] + dk[2]) / 2, dk[1] + (dk[3] - dk[1]) * min(1.0, max(0.0, (co.z - o.z - 2.0) / 1.4)))

    def surf(s, q):
        if q == 0:
            return ("trim", side(1))
        if q == 4:
            return ("trim", side(-1))
        if s >= cockpit and q in (1, 2, 3):
            return ("glass", dark)
        return ("trim", flat)

    b.loft(rings, surf=surf, surface="plate_a")
    for fx, fy in (((-1.2, -2.4), (1.2, -2.4), (0.0, 3.0)) if not lifter else ((-1.3, -2.2), (1.3, -2.2), (-1.3, 2.0), (1.3, 2.0))):
        b.box(P(fx, fy, 0.35), (0.4, 0.4, 1.3), yaw=yaw, surface="steel")
        b.box(P(fx, fy, -0.12), (0.9, 1.4, 0.16), yaw=yaw, surface="steel")
    if lifter:
        for fx, fy in ((-3.3, -2.4), (3.3, -2.4), (-3.3, 2.0), (3.3, 2.0)):
            b.tube(P(fx, fy, 1.5), P(fx, fy, 2.5), 1.25, 10, surface="engine", ends="plate_a")
            b.box(P(fx * 0.72, fy, 2.1), (2.0, 0.6, 0.35), yaw=yaw, surface="steel")
    else:
        for sgn in (-1, 1):
            b.box(P(sgn * 3.0, -1.4, 2.0), (4.0, 2.6, 0.3), yaw=yaw, surface="steel")
            b.tube(P(sgn * 5.0, -3.4, 2.0), P(sgn * 5.0, 0.6, 2.0), 0.85, 8, 0.7, surface="engine", ends="plate_a")
            b.box(P(sgn * 0.8, -4.7, 3.2), (0.25, 1.9, 1.7), yaw=yaw, surface="steel")


def parked(coll):
    b = Build()
    craft(b, polar(PAD_R, 45.0) + polar(1.0, 135.0), 45.0 + 110.0)
    craft(b, polar(PAD_R, 225.0) + polar(1.0, 315.0), 225.0 - 70.0)
    craft(b, polar(PAD_R, 135.0), 135.0 + 40.0, lifter=True)
    b.done("parked craft (two shuttles, a lifter)", coll)


# ---------------------------------------------------------------- checks and pictures
def report(coll):
    total, bad = 0, 0
    for ob in sorted(coll.objects, key=lambda o: o.name):
        bm = bmesh.new(); bm.from_mesh(ob.data)
        open_edges = sum(1 for e in bm.edges if len(e.link_faces) == 1)
        loose = sum(1 for e in bm.edges if len(e.link_faces) == 0)
        tris = sum(len(f.verts) - 2 for f in bm.faces)
        bm.free()
        total += tris; bad += open_edges + loose
        flag = "" if open_edges == 0 and loose == 0 else "   <-- OPEN %d LOOSE %d" % (open_edges, loose)
        print("MESH %-58s tris %6d%s" % (ob.name[:58], tris, flag))
    print("TOTAL TRIANGLES %d   OPEN OR LOOSE EDGES %d   MATERIALS %d   OBJECTS %d" % (total, bad, len(_mats), len(coll.objects)))


def density(coll):
    """Texel density per material: pixels per metre from UV area against world area (tiling
    surfaces should sit near 64; atlas faces that hold a flat colour are left out)."""
    stats = {}
    for ob in coll.objects:
        bm = bmesh.new(); bm.from_mesh(ob.data)
        uv = bm.loops.layers.uv.active
        for f in bm.faces:
            name = ob.data.materials[f.material_index].name
            if name.split("arena_hull_")[1] in ("light", "glow", "trim", "glass", "pad"):
                continue
            co = [l[uv].uv for l in f.loops]
            ua = abs(sum(co[k].x * co[(k + 1) % len(co)].y - co[(k + 1) % len(co)].x * co[k].y for k in range(len(co)))) / 2
            wa = f.calc_area()
            if wa > 0.05 and ua > 0:
                d = math.sqrt(ua / wa) * 1024.0
                s = stats.setdefault(name, [1e9, 0.0, 0.0, 0.0])
                s[0] = min(s[0], d); s[1] = max(s[1], d); s[2] += d * wa; s[3] += wa
        bm.free()
    for name, (lo, hi, acc, area) in sorted(stats.items()):
        print("DENSITY %-22s min %5.1f  mean %5.1f  max %5.1f px/m" % (name, lo, acc / area, hi))


def backdrop():
    """The blockout's stadium in plain grey, and its city, so the kit is judged in place."""
    import author_arena_stadium as st
    c = collection("backdrop (blockout, not part of the kit)")
    fx = collection("backdrop fx")
    st.stage(c); st.field(c); st.lower_bowl(c); st.upper_stands(c); st.canopies(c, fx); st.screens(c)
    grey = bpy.data.materials.new("backdrop_grey")
    grey.use_nodes = True
    grey.diffuse_color = (0.42, 0.42, 0.42, 1)
    grey.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.16, 0.16, 0.17, 1)
    for ob in c.objects:
        for k in range(len(ob.data.materials)):
            ob.data.materials[k] = grey
    for ob in list(fx.objects):
        bpy.data.objects.remove(ob)
    st.city(collection("backdrop city"))


def shoot(name, loc, target, lens=35.0):
    scene = bpy.context.scene
    cam_data = bpy.data.cameras.new(name)
    cam = bpy.data.objects.new(name, cam_data)
    scene.collection.objects.link(cam)
    cam.location = loc
    cam.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
    cam_data.lens = lens; cam_data.clip_start = 0.3; cam_data.clip_end = 6000
    scene.camera = cam
    scene.render.filepath = os.path.join(LOGS, name + ".png")
    bpy.ops.render.render(write_still=True)


def main():
    version = "v1"
    for a in sys.argv:
        if a.startswith("--version="):
            version = a.split("=", 1)[1]
    bpy.ops.wm.read_factory_settings()
    for ob in list(bpy.data.objects):
        bpy.data.objects.remove(ob)
    for c in list(bpy.data.collections):
        bpy.data.collections.remove(c)
    scene = bpy.context.scene
    coll = collection("arena_hull")
    shaft(coll)
    hull_body(coll)
    engines(coll)
    underside_gear(coll)
    gate_halls(coll)
    lamps(coll)
    furniture(coll)
    balustrade(coll)
    pads(coll)
    parked(coll)
    report(coll)
    density(coll)
    os.makedirs(KIT, exist_ok=True)
    os.makedirs(LOGS, exist_ok=True)
    if not CHECKER:
        for m in list(bpy.data.materials):                        # nothing but the kit goes into the file
            if not m.name.startswith("arena_hull_"):
                bpy.data.materials.remove(m)
        bpy.context.preferences.filepaths.save_version = 0
        path = os.path.join(KIT, "hull.blend")
        bpy.ops.wm.save_as_mainfile(filepath=path)
        bpy.ops.file.make_paths_relative()
        bpy.ops.wm.save_mainfile()
        print("SAVED", path)
    if "--no-render" in sys.argv:
        return

    backdrop()
    key = bpy.data.objects.new("floodlight key", bpy.data.lights.new("floodlight key", "SUN"))
    key.data.energy = 2.6; key.data.color = (0.86, 0.93, 1.0); key.data.angle = math.radians(25)
    key.rotation_euler = (math.radians(14), math.radians(9), 0)
    scene.collection.objects.link(key)
    fill = bpy.data.objects.new("under fill", bpy.data.lights.new("under fill", "SUN"))   # the city's glow from below
    fill.data.energy = 1.6; fill.data.color = (0.55, 0.62, 1.0); fill.data.use_shadow = False
    fill.rotation_euler = (math.radians(160), math.radians(20), 0)
    scene.collection.objects.link(fill)
    world = bpy.data.worlds.new("night"); scene.world = world; world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.045, 0.060, 0.150, 1)
    world.node_tree.nodes["Background"].inputs[1].default_value = 1.0
    engines_ = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    eevee = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines_ else "BLENDER_EEVEE"
    scene.render.engine = eevee
    scene.render.resolution_x, scene.render.resolution_y = 1920, 1080
    scene.view_settings.view_transform = "Standard"

    p = "hull_%s_%s" % (version, "checker_" if CHECKER else "")
    eng = polar(176.0, 157.5)
    views = [
        ("01_shaft_from_stage", tuple(polar(24.8, 45.0, 1.9)), tuple(polar(41.0, 118.0, -30.0)), 18),
        ("02_shaft_straight_down", tuple(polar(26.0, 90.0, 1.9)), tuple(polar(10.0, 262.0, -80.0)), 16),
        ("03_falling_z-20", (9.0, 6.0, -20.0), tuple(polar(30.0, 200.0, -78.0)), 15),
        ("04_falling_across", (-6.0, 10.0, -38.0), tuple(polar(41.0, 120.0, -47.0)), 20),
        ("05_air", (250, -360, 250), (0, 0, 10), 20),
        ("06_below", (200, -300, -190), (0, 0, -20), 20),
        ("07_engine", tuple(polar(262.0, 140.0, -112.0)), (eng.x, eng.y, -74.0), 32),
        ("08_gate_hall", (46.0, -268.0, 16.0), (0.0, -202.0, 3.0), 35),
        ("09_landing_pad", tuple(polar(292.0, 128.0, 14.0)), tuple(polar(250.0, 135.0, -2.0)), 32),
        ("10_plaza_corner", tuple(polar(300.0, 152.0, 95.0)), tuple(polar(196.0, 137.0, -2.0)), 35),
        ("11_rim_side", tuple(polar(330.0, 100.0, -22.0)), tuple(polar(200.0, 112.0, -30.0)), 35),
    ]
    only = [a.split("=", 1)[1].split(",") for a in sys.argv if a.startswith("--views=")]
    for name, loc, target, lens in views:
        if only and name[:2] not in only[0]:
            continue
        shoot(p + name, loc, target, lens)
    if not CHECKER:
        scene.render.engine = "BLENDER_WORKBENCH"                  # flat: how the meshes join, with no lighting to hide it
        scene.display.shading.light = "STUDIO"; scene.display.shading.color_type = "MATERIAL"
        scene.display.shading.show_cavity = True
        for name, loc, target, lens in views:
            if name[:2] in ("01", "06", "07", "08", "09"):
                if only and name[:2] not in only[0]:
                    continue
                shoot(p + "flat_" + name, loc, target, lens)
    print("HULL_OK")


if __name__ == "__main__":
    main()
