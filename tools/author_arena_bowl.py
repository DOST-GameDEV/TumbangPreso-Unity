"""The arena's `bowl` kit: the field, the lower bowl and its concourse, the four upper stands (ARENA-1.4).

    blender -b --python tools/author_arena_bowl.py -- --version=v1 [--checker] [--no-render]

Read docs/ARENA_ART_BRIEF.md first. Writes ArtSource/arena/kits/bowl.blend (collection
`arena_bowl`, everything already in the stadium's frame: metres, z up, y north, the can at the
origin), tools/arena_rows.json (the exact rows, for the crowd kit) and the review pictures in
Logs/arena/bowl/. The textures are painted by tools/author_arena_textures_bowl.py; run it first.
The numbers come from tools/arena_kit.py; nothing in the brief's table is moved.

HOW IT IS BUILT
  * `sweep` makes ONE closed solid from a profile carried round the centre, like arena_kit's
    `lathe`, with three things the blockout's lathe could not do:
      - a column may follow a line PARALLEL to a radius (`Plane(a, d)`), so an aisle, a doorway
        or the tunnel keeps one width from the front row to the back instead of fanning out;
      - neighbouring columns may carry DIFFERENT profiles (a seat row beside aisle steps, the
        LED barrier beside its gap, a stand beside its end wall). Where they differ the gap
        between the two profiles is closed by faces in the shared plane, so a row of seats has a
        real end and the solid still has no open edge;
      - every face gets UVs at its texture's world scale as it is made.
  * a seat row is the row's own profile: a floor, the front under the pan, the pan, the back, the
    back's rear. One surface with the aisles, sharing every vertex. Single seats are painted on
    it (arena_bowl_seat). Rows are true size (0.8 m), twice the blockout's count.
  * doorways, the deck gates and the players' tunnel are POCKETS: the wall's own face pushed 2 to
    10 m into the solid, with jambs, a head and a floor, and a painted back. Never a black quad.
  * rails, fins and kiosks are closed solids whose ends stand inside what they join.
`report()` counts triangles, open edges and zero-area faces, and measures the texel density.
"""
import bpy, bmesh, json, math, os, sys
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from arena_kit import (ROOT, PIT_R, FIELD_R, BARRIER_R, DECK_Z, BASE_Z, LOWER_ROWS, UPPER_ROWS, UPPER_ARCS,  # noqa: E402
                       EYE, polar, collection)
import author_arena_textures_bowl as TEX  # noqa: E402  (constants only: tile sizes, atlas regions)

KIT = "arena_bowl"
BLEND = os.path.join(ROOT, "ArtSource", "arena", "kits", "bowl.blend")
LOGS = os.path.join(ROOT, "Logs", "arena", "bowl")
ROWS_JSON = os.path.join(ROOT, "tools", "arena_rows.json")

# ---------------------------------------------------------------- the stand's own numbers
LOW_AISLE_W, UP_AISLE_W = 1.8, 1.6          # metres, the same from the front row to the back
LOW_SECTIONS, UP_SECTIONS = 24, 5           # sections round the lower bowl, per upper stand
LOW_AISLE_0 = 7.5                           # the first lower aisle's bearing: 180 (the tunnel) and 0 are section centres
END_WALL, TUNNEL_HALF, TUNNEL_JAMB = 0.5, 2.5, 0.4
LOW_BASE, UP_BASE = BASE_Z + 0.5, BASE_Z + 1.0   # each solid's own bottom: no two bottoms share a plane
BANKS = {
    "low1": dict(r_in=85.3, r_out=106.0, z_in=3.0, z_out=12.6, n=LOWER_ROWS, back=0.88),
    "low2": dict(r_in=110.6, r_out=130.0, z_in=15.4, z_out=26.0, n=LOWER_ROWS - 2, back=0.92),
    "up": dict(r_in=141.0, r_out=176.0, z_in=32.0, z_out=58.0, n=2 * UPPER_ROWS, back=1.00),
}
for _b in BANKS.values():
    _b["tread"] = (_b["r_out"] - _b["r_in"]) / _b["n"]
    _b["rise"] = (_b["z_out"] - _b["z_in"]) / (_b["n"] - 1)
    _b["floor"] = _b["tread"] / 2                 # the foot space; the aisle's first step is the same depth
SEAT_H, PAN_D = 0.42, 0.36                        # the pan's height over the row's floor, its depth
HOOPS = {"low1": ((2, 5), (10, 13), (18, 21)), "low2": ((2, 5), (9, 12), (17, 20)),
         "up": ((2, 5), (10, 13), (18, 21), (26, 29), (34, 37))}
LOGO_BEARINGS, LOGO_W, LOGO_R = (0.0, 180.0), 22.0, 58.15
LOGO_ASPECT = 1895.0 / 1247.0
CORNERS = (45.0, 135.0, 225.0, 315.0)

MATS = ["turf_a", "turf_b", "marking", "track", "concrete", "seat", "step", "steel", "led", "panel", "door"]
EMIT = {"led": 1.0, "panel": 1.0, "door": 1.6}
VIEW = {"turf_a": (0.05, 0.25, 0.07), "turf_b": (0.08, 0.34, 0.10), "marking": (0.85, 0.88, 0.84),
        "track": (0.03, 0.035, 0.06), "concrete": (0.07, 0.085, 0.14), "seat": (0.02, 0.035, 0.12),
        "step": (0.15, 0.18, 0.26), "steel": (0.10, 0.12, 0.20), "led": (0.02, 0.04, 0.45),
        "panel": (0.015, 0.02, 0.06), "door": (0.004, 0.005, 0.012)}
MI = {m: i for i, m in enumerate(MATS)}
T = dict(TEX.TILE_M)
CHECKER = False


# ---------------------------------------------------------------- materials
def material(surface):
    name = "%s_%s" % (KIT, surface)
    m = bpy.data.materials.get(name)
    if m:
        return m
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    m.diffuse_color = VIEW[surface] + (1.0,)
    nt = m.node_tree
    b = nt.nodes.get("Principled BSDF")
    b.inputs["Roughness"].default_value = 1.0
    b.inputs["Specular IOR Level"].default_value = 0.0

    def image(suffix):
        if CHECKER:
            img = bpy.data.images.get("bowl_checker")
            if img is None:
                img = bpy.data.images.new("bowl_checker", 1024, 1024)
                img.generated_type = "COLOR_GRID"
        else:
            img = bpy.data.images.load(os.path.join(TEX.OUT, name + suffix + ".png"), check_existing=True)
        node = nt.nodes.new("ShaderNodeTexImage")
        node.image = img
        return node

    alb = image("")
    nt.links.new(alb.outputs["Color"], b.inputs["Base Color"])
    if surface == "marking" and not CHECKER:
        nt.links.new(alb.outputs["Alpha"], b.inputs["Alpha"])
        m.surface_render_method = "DITHERED"
    if surface in EMIT and not CHECKER:
        em = image("_emit")
        nt.links.new(em.outputs["Color"], b.inputs["Emission Color"])
        b.inputs["Emission Strength"].default_value = EMIT[surface]
    return m


def finish(bm, name, coll):
    """Weld, face outward, keep only the materials used, and link the object."""
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0004)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    used = sorted({f.material_index for f in bm.faces})
    remap = {old: new for new, old in enumerate(used)}
    for f in bm.faces:
        f.material_index = remap[f.material_index]
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    for old in used:
        me.materials.append(material(MATS[old]))
    ob = bpy.data.objects.new(name, me)
    coll.objects.link(ob)
    return ob


# ---------------------------------------------------------------- the sweep
class Plane:
    """Where a column's edge stands. d = 0: the radius at bearing `a`. Otherwise the line parallel
    to that radius, `d` metres to its clockwise side, so two of them bound a strip of one width."""
    __slots__ = ("a", "d")

    def __init__(self, a, d=0.0):
        self.a, self.d = a, d

    def bearing(self, r):
        return self.a + (math.degrees(math.asin(max(-1.0, min(1.0, self.d / r)))) if self.d else 0.0)


def earclip(pts):
    """Triangles of a simple polygon given as (x, y) points. An ear of no area is never cut, so a
    vertex lying on a straight side still ends up a corner of real triangles."""
    n = len(pts)
    idx = list(range(n))
    area = sum(pts[i][0] * pts[(i + 1) % n][1] - pts[(i + 1) % n][0] * pts[i][1] for i in range(n))
    if area < 0:
        idx.reverse()

    def cross(a, b, c):
        return (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])

    tris = []
    while len(idx) > 3:
        m = len(idx)
        cut = None
        for k in range(m):
            ia, ib, ic = idx[k - 1], idx[k], idx[(k + 1) % m]
            a, b, c = pts[ia], pts[ib], pts[ic]
            if cross(a, b, c) <= 1e-9:
                continue
            for j in idx:
                if j in (ia, ib, ic):
                    continue
                p = pts[j]
                if cross(a, b, p) >= -1e-9 and cross(b, c, p) >= -1e-9 and cross(c, a, p) >= -1e-9:
                    break
            else:
                cut = k
                tris.append((ia, ib, ic))
                break
        if cut is None:
            break
        idx.pop(cut)
    left = 0
    if len(idx) == 3 and abs(cross(pts[idx[0]], pts[idx[1]], pts[idx[2]])) > 1e-9:
        tris.append(tuple(idx))
    elif len(idx) > 3:
        left = len(idx)
    return tris, left


def key(r, z):
    return (round(r, 4), round(z, 4))


def sweep(name, planes, cells, variants, coll, shade, full=True, origin=(0.0, 0.0), cap=None, end_cap="panel",
          post=None):
    """ONE closed solid. `planes` are the column edges in order; `cells[i]` (a dict with "v", the
    name of its profile in `variants`) lies between plane i and plane i + 1. A profile is a closed
    loop of (r, z, tag); the tag names the strip from that point to the next. `shade(tag, cell,
    flat)` answers (material, uv rule). Where two neighbouring cells carry different profiles,
    `cap(left, right)` names the material of the faces that close the difference. `post(bm, uv,
    tagged)` may cut pockets before the mesh is finished."""
    bm = bmesh.new()
    uvl = bm.loops.layers.uv.new("UVMap")
    n = len(planes)
    ox, oy = origin
    table = [dict() for _ in planes]
    axis = {}

    def vert(i, r, z):
        k = key(r, z)
        if r < 1e-6:                                         # a point on the solid's own axis: one vertex for every column
            if k not in axis:
                axis[k] = bm.verts.new((ox, oy, z))
            return axis[k]
        v = table[i].get(k)
        if v is None:
            p = polar(r, planes[i].bearing(r), z)
            v = table[i][k] = bm.verts.new((p.x + ox, p.y + oy, z))
        return v

    def uv_of(rule, cell, i, wrap, r, z, s, end, side, r0, r1, v):
        kind = rule[0]
        b = planes[i].bearing(r) + (360.0 if wrap else 0.0)
        if kind == "xy":
            return (v.co.x / rule[1], v.co.y / rule[1])
        if kind == "sweep":
            return ((r0 + r1) / 2 * math.radians(b) / rule[1] + rule[2], s / rule[1])
        if kind == "band":                                   # an atlas row: u by bearing, v by the strip's two edges
            return (b / 360.0 * rule[1], rule[2] if end == 0 else rule[3])
        if kind == "line":                                   # a painted ring line
            return ((r0 + r1) / 2 * math.radians(b) / rule[1], rule[2] if end == 0 else rule[3])
        if kind == "across":                                 # a painted spoke: u runs out along it
            return (r / rule[1], rule[2] if side == 0 else rule[3])
        if kind == "seat":                                   # whole seats between the section's two aisles
            rs = rule[2]
            lo = planes[cell["sec"][0]].bearing(rs)
            hi = planes[cell["sec"][1]].bearing(rs)
            if hi < lo:
                hi += 360.0
            bb = planes[i].bearing(rs)
            while bb < lo - 1e-6:
                bb += 360.0
            seats = max(1, round(rs * math.radians(hi - lo) / TEX.SEAT_PITCH_M))
            band = TEX.SEAT_BANDS[rule[1]]
            return ((bb - lo) / (hi - lo) * seats / TEX.SEATS_PER_TILE, band[0] if end == 0 else band[1])
        raise ValueError(kind)

    tagged = {}
    count = n if full else n - 1
    for i in range(count):
        j = (i + 1) % n
        wrap = full and j == 0
        cell = cells[i]
        prof = variants[cell["v"]]
        s = 0.0
        for k, (r0, z0, tag) in enumerate(prof):
            r1, z1, _ = prof[(k + 1) % len(prof)]
            seg = math.hypot(r1 - r0, z1 - z0)
            mat, rule = shade(tag, cell, abs(z1 - z0) < 1e-6)
            spec = ((i, False, r0, z0, s, 0, 0), (i, False, r1, z1, s + seg, 1, 0),
                    (j, wrap, r1, z1, s + seg, 1, 1), (j, wrap, r0, z0, s, 0, 1))
            if r0 < 1e-6:                                    # a strip that starts or ends on the axis is a triangle
                spec = spec[:3]
            elif r1 < 1e-6:
                spec = (spec[0], spec[1], spec[3])
            s += seg
            if r0 < 1e-6 and r1 < 1e-6:
                continue
            f = bm.faces.new([vert(q[0], q[2], q[3]) for q in spec])
            f.material_index = MI[mat]
            for loop, (pi, w, r, z, ss, end, side) in zip(f.loops, spec):
                loop[uvl].uv = uv_of(rule, cell, pi, w, max(r, 0.5), z, ss, end, side, r0, r1, loop.vert)
            tagged.setdefault(tag[0], []).append((f, cell))

    def close(i, loop, mat):
        tris, left = earclip(loop)
        if left:
            print("WARNING %s: a closing face at plane %d kept %d points untriangulated" % (name, i, left))
        for a, b, c in tris:
            f = bm.faces.new((vert(i, *loop[a]), vert(i, *loop[b]), vert(i, *loop[c])))
            f.material_index = MI[mat]
            for lp, q in zip(f.loops, (loop[a], loop[b], loop[c])):
                lp[uvl].uv = (q[0] / T[mat], q[1] / T[mat])

    for i in range(n):
        if not full and i in (0, n - 1):
            prof = variants[cells[0 if i == 0 else n - 2]["v"]]
            close(i, [key(r, z) for r, z, _ in prof], end_cap)
            continue
        left, right = cells[(i - 1) % count], cells[i % count]
        if left["v"] == right["v"]:
            continue
        lp = [key(r, z) for r, z, _ in variants[left["v"]]]
        rp = [key(r, z) for r, z, _ in variants[right["v"]]]
        where = {k: q for q, k in enumerate(rp)}
        common, last = [], -1
        for q, k in enumerate(lp):
            w = where.get(k)
            if w is not None and w > last:
                common.append((q, w))
                last = w
        mat = cap(left, right)
        for c in range(len(common)):
            (i0, j0), (i1, j1) = common[c], common[(c + 1) % len(common)]
            lc = lp[i0:i1 + 1] if i1 > i0 else lp[i0:] + lp[:i1 + 1]
            rc = rp[j0:j1 + 1] if j1 > j0 else rp[j0:] + rp[:j1 + 1]
            if len(lc) == 2 and len(rc) == 2:
                continue
            close(i, lc + rc[-2:0:-1], mat)
    if post:
        post(bm, uvl, tagged)
    return finish(bm, name, coll)


def pocket(bm, uvl, face, direction, depth, region, floor="step", side="steel"):
    """A real opening: the wall's face goes `depth` into the solid along `direction`; jambs, head
    and floor join it to the wall; the back wears one region of the door atlas."""
    d = Vector(direction).normalized()
    vs = list(face.verts)
    mi = face.material_index
    bmesh.ops.delete(bm, geom=[face], context="FACES_ONLY")
    new = [bm.verts.new(v.co + d * depth) for v in vs]
    zlo = min(v.co.z for v in vs)
    for k in range(4):
        a, b = vs[k], vs[(k + 1) % 4]
        f = bm.faces.new((a, b, new[(k + 1) % 4], new[k]))
        low = abs(a.co.z - zlo) < 1e-4 and abs(b.co.z - zlo) < 1e-4
        mat = floor if low else side
        f.material_index = MI[mat]
        tile = T.get(mat, 8.0)
        along = [0.0, (b.co - a.co).length]
        for lp, (p, q) in zip(f.loops, ((along[0], 0.0), (along[1], 0.0), (along[1], depth), (along[0], depth))):
            lp[uvl].uv = (p / tile, q / tile)
    back = bm.faces.new(new)
    back.material_index = MI["door"]
    right = d.cross(Vector((0, 0, 1)))
    us = [v.co.dot(right) for v in new]
    zs = [v.co.z for v in new]
    u0, v0, u1, v1 = TEX.region_uv(TEX.DOOR_REGIONS[region])
    for lp in back.loops:
        fu = (lp.vert.co.dot(right) - min(us)) / (max(us) - min(us))
        fv = (lp.vert.co.z - min(zs)) / (max(zs) - min(zs))
        lp[uvl].uv = (u0 + fu * (u1 - u0), v0 + fv * (v1 - v0))
    return mi


# ---------------------------------------------------------------- separate closed solids
class Solids:
    """Rails, fins, kiosks: closed solids in one object, each with box-projected UVs."""

    def __init__(self):
        self.bm = bmesh.new()
        self.uv = self.bm.loops.layers.uv.new("UVMap")

    def _face(self, verts, mat, uvs):
        f = self.bm.faces.new(verts)
        f.material_index = MI[mat]
        for lp, q in zip(f.loops, uvs):
            lp[self.uv].uv = q
        return f

    def box(self, centre, size, yaw=0.0, pitch=0.0, mat="steel", front=None, faces=None):
        """`front` = (material, (u0, v0, u1, v1)) dresses the face that looks at the can. `faces`
        overrides the material of named faces ("top", "back", "left", "right", "bottom")."""
        hx, hy, hz = size[0] / 2, size[1] / 2, size[2] / 2
        cy, sy = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
        cp, sp = math.cos(math.radians(pitch)), math.sin(math.radians(pitch))
        vs = []
        for sx in (-1, 1):
            for syy in (-1, 1):
                for sz in (-1, 1):
                    x, y, z = sx * hx, syy * hy, sz * hz
                    y, z = y * cp - z * sp, y * sp + z * cp
                    x, y = x * cy + y * sy, -x * sy + y * cy
                    vs.append(self.bm.verts.new((centre[0] + x, centre[1] + y, centre[2] + z)))
        # corner order per face: (bottom-left, bottom-right, top-right, top-left) seen from outside
        sides = {"front": ((0, 4, 5, 1), size[0], size[2]), "back": ((6, 2, 3, 7), size[0], size[2]),
                 "left": ((2, 0, 1, 3), size[1], size[2]), "right": ((4, 6, 7, 5), size[1], size[2]),
                 "top": ((1, 5, 7, 3), size[0], size[1]), "bottom": ((2, 6, 4, 0), size[0], size[1])}
        for which, (ids, w, h) in sides.items():
            m = (faces or {}).get(which, mat)
            tile = T.get(m, 8.0)
            uvs = ((0, 0), (w / tile, 0), (w / tile, h / tile), (0, h / tile))
            if which == "front" and front:
                m = front[0]
                u0, v0, u1, v1 = front[1]
                uvs = ((u0, v0), (u1, v0), (u1, v1), (u0, v1))
            self._face([vs[k] for k in ids], m, uvs)
        return self

    def tube(self, p0, p1, radius, sides=4, mat="steel"):
        p0, p1 = Vector(p0), Vector(p1)
        axis = (p1 - p0).normalized()
        ref = Vector((0, 0, 1)) if abs(axis.z) < 0.9 else Vector((1, 0, 0))
        u = axis.cross(ref).normalized()
        v = axis.cross(u)
        ring = [(u * math.cos(t) + v * math.sin(t)) * radius for t in (2 * math.pi * (k + 0.5) / sides for k in range(sides))]
        a = [self.bm.verts.new(p0 + q) for q in ring]
        b = [self.bm.verts.new(p1 + q) for q in ring]
        tile = T[mat]
        length = (p1 - p0).length
        girth = 2 * radius * math.sin(math.pi / sides)
        for k in range(sides):
            j = (k + 1) % sides
            self._face((a[k], a[j], b[j], b[k]), mat, ((k * girth / tile, 0), ((k + 1) * girth / tile, 0),
                                                       ((k + 1) * girth / tile, length / tile), (k * girth / tile, length / tile)))
        cap = [(q.dot(u) / tile, q.dot(v) / tile) for q in ring]
        self._face(a[::-1], mat, cap[::-1])
        self._face(b, mat, cap)
        return self

    def prism(self, outline, bearing, thick, mat="concrete"):
        """A flat fin: `outline` is a convex loop of (r, z) in the upright plane through the centre
        at `bearing`; the fin is `thick` across it."""
        side = polar(thick / 2, bearing + 90.0)
        tile = T[mat]
        a = [self.bm.verts.new(polar(r, bearing, z) - side) for r, z in outline]
        b = [self.bm.verts.new(polar(r, bearing, z) + side) for r, z in outline]
        flat = [(r / tile, z / tile) for r, z in outline]
        self._face(a, mat, flat)
        self._face(b[::-1], mat, flat[::-1])
        s = 0.0
        for k in range(len(outline)):
            j = (k + 1) % len(outline)
            seg = math.hypot(outline[j][0] - outline[k][0], outline[j][1] - outline[k][1])
            self._face((a[k], a[j], b[j], b[k]), mat, ((0, s / tile), (0, (s + seg) / tile), (thick / tile, (s + seg) / tile), (thick / tile, s / tile)))
            s += seg
        return self

    def done(self, name, coll):
        return finish(self.bm, name, coll)


# ---------------------------------------------------------------- shading rules shared by the sweeps
def led_rule(row, radius):
    v0, v1 = TEX.led_v(row)
    return ("led", ("band", max(1, round(2 * math.pi * radius / TEX.LED_TILE_M)), v0, v1))


def common_shade(tag, cell, flat):
    kind = tag[0]
    if kind in ("conc", "hid", "floor", "doorwall", "tunnel", "gate"):
        return ("concrete", ("xy", T["concrete"])) if flat else ("concrete", ("sweep", T["concrete"], 0.0))
    if kind == "steel":
        return ("steel", ("xy", T["steel"])) if flat else ("steel", ("sweep", T["steel"], 0.0))
    if kind in ("walk", "step_t"):
        return ("step", ("xy", T["step"]))
    if kind == "step_r":
        return ("step", ("sweep", T["step"], 0.0))
    if kind == "track":
        return ("track", ("xy", T["track"]))
    if kind == "panel":
        return ("panel", ("sweep", T["panel"], tag[1] if len(tag) > 1 else 0.0))
    if kind == "led":
        rule = led_rule(tag[1], tag[2])
        if flat:                                             # a line lying on a floor: v across its width
            return (rule[0], ("band", rule[1][1], rule[1][2], rule[1][3]))
        return rule
    if kind in ("sfront", "span", "sback", "srear"):
        b = BANKS[tag[1]]
        rs = b["r_in"] + tag[2] * b["tread"] + b["floor"] + PAN_D / 2
        return ("seat", ("seat", {"sfront": "front", "span": "pan", "sback": "back", "srear": "rear"}[kind], rs))
    raise ValueError(tag)


def bank_rows(name, seats):
    """One bank's part of a profile, up to but not including its last point (r_out, z_out). With
    `seats` each row is floor, front, pan, back, rear; without, two steps a row."""
    b = BANKS[name]
    t, rise, fl, n = b["tread"], b["rise"], b["floor"], b["n"]
    pts = []
    for k in range(n):
        r, z = b["r_in"] + k * t, b["z_in"] + k * rise
        if seats:
            pts += [(r, z, ("floor",)), (r + fl, z, ("sfront", name, k)), (r + fl - 0.04, z + SEAT_H, ("span", name, k)),
                    (r + fl - 0.04 + PAN_D, z + SEAT_H, ("sback", name, k)), (r + t + 0.03, z + b["back"], ("srear", name, k))]
        elif k == n - 1:
            pts += [(r, z, ("step_t",)), (r + fl, z, ("step_t",))]
        else:
            pts += [(r, z, ("step_t",)), (r + fl, z, ("step_r",)), (r + fl, z + rise / 2, ("step_t",)), (r + t, z + rise / 2, ("step_r",))]
    return pts


def coping(r_front, r_back, z_top, z_floor):
    """A stand's front rail: a steel coping 0.2 m deep, 8 cm wider than the parapet it caps, and
    the parapet's inner face down to the first row. Starts at the parapet's outer face."""
    return [(r_front, z_top - 0.2, ("steel",)), (r_front - 0.08, z_top - 0.2, ("steel",)), (r_front - 0.08, z_top, ("steel",)),
            (r_back + 0.08, z_top, ("steel",)), (r_back + 0.08, z_top - 0.2, ("steel",)), (r_back, z_top - 0.2, ("conc",))]


def in_upper_arc(bearing, margin=0.0):
    b = bearing % 360.0
    return any(lo - margin <= x <= hi + margin for lo, hi in UPPER_ARCS for x in (b, b - 360.0))


# ---------------------------------------------------------------- 1. the field
def field(coll):
    """One solid from the shaft to the lower bowl: the kerb, the turf mown in a chequer of sixteen
    wedges and three rings, two painted rings and eight spokes cut into it, the track, the LED
    barrier with its gap in front of the players' tunnel."""
    step_a = 3.75
    planes, cells = [], []
    for k in range(96):
        a = k * step_a
        if abs(a % 45.0) < 1e-6:
            group = [Plane(a, -0.1), Plane(a, 0.1)]
            if abs(a - 180.0) < 1e-6:
                group = [Plane(a, -TUNNEL_HALF)] + group + [Plane(a, TUNNEL_HALF)]
            planes += group
        else:
            planes.append(Plane(a))
    for i, p in enumerate(planes):
        q = planes[(i + 1) % len(planes)]
        spoke = p.d == -0.1 and q.d == 0.1
        gap = abs(p.a - 180.0) < 1e-6 and p.d != 0 and (p.d < TUNNEL_HALF)
        mid = (p.bearing(60.0) + (q.bearing(60.0) + (360.0 if i == len(planes) - 1 else 0.0))) / 2
        cells.append({"v": "gap" if gap else "field", "spoke": spoke, "mid": mid, "a": p.a})

    def profile(barrier):
        pts = [(PIT_R, BASE_Z, ("conc",)), (PIT_R, -1.2, ("led", "cyan", PIT_R)), (PIT_R, -1.0, ("conc",)), (PIT_R, 0.35, ("steel",)),
               (PIT_R + 0.5, 0.35, ("led", "cyan", PIT_R)), (PIT_R + 0.75, 0.35, ("steel",)), (PIT_R + 1.3, 0.35, ("steel",)),
               (PIT_R + 2.0, 0.0, ("turf", 0)), (48.0, 0.0, ("ring",)), (48.3, 0.0, ("turf", 1)), (68.0, 0.0, ("ring",)),
               (68.3, 0.0, ("turf", 2)), (75.0, 0.0, ("track",))]
        if barrier:
            pts += [(BARRIER_R - 0.5, 0.0, ("led", "barrier", BARRIER_R)), (BARRIER_R - 0.38, 1.5, ("steel",)),
                    (BARRIER_R, 1.5, ("steel",)), (BARRIER_R, 0.0, ("track",))]
        else:
            pts += [(BARRIER_R - 0.5, 0.0, ("track",)), (BARRIER_R, 0.0, ("track",))]
        return pts + [(FIELD_R, 0.0, ("hid",)), (FIELD_R, BASE_Z, ("hid",))]

    paint = TEX.region_uv(TEX.MARKING_PAINT)
    pv = ((paint[1] + paint[3]) / 2 - 0.01, (paint[1] + paint[3]) / 2 + 0.01)     # 20 px of paint across a 0.3 m line

    def shade(tag, cell, flat):
        if tag[0] == "turf":
            if cell["spoke"] and not (tag[1] == 1 and cell["a"] in LOGO_BEARINGS):
                return ("marking", ("across", 16.0, pv[0], pv[1]))
            wedge = int(((cell["mid"] - 11.25) % 360.0) // 22.5)
            m = "turf_a" if (wedge + tag[1]) % 2 == 0 else "turf_b"
            return (m, ("xy", T[m]))
        if tag[0] == "ring":
            return ("marking", ("line", 16.0, pv[0], pv[1]))
        return common_shade(tag, cell, flat)

    sweep("arena_bowl_field", planes, cells, {"field": profile(True), "gap": profile(False)}, coll, shade,
          cap=lambda a, b: "steel")
    # The logo, twice: field paint on a quad 6 cm over the turf, the only face in its plane there.
    me = bpy.data.meshes.new("arena_bowl_field_logo")
    h = LOGO_W / LOGO_ASPECT
    verts, faces = [], []
    for b in LOGO_BEARINGS:
        c = polar(LOGO_R, b, 0.06)
        for dx, dy in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
            verts.append((c.x + dx * LOGO_W / 2, c.y + dy * h / 2, 0.06))       # its top is north in both places
        faces.append(tuple(range(len(verts) - 4, len(verts))))
    me.from_pydata(verts, [], faces)
    uv = me.uv_layers.new(name="UVMap")
    u0, v0, u1, v1 = TEX.region_uv(TEX.MARKING_LOGO)
    for k in range(len(faces)):
        for c, q in enumerate(((u0, v0), (u1, v0), (u1, v1), (u0, v1))):
            uv.data[k * 4 + c].uv = q
    me.materials.append(material("marking"))
    ob = bpy.data.objects.new("arena_bowl_field_logo", me)
    ob["decal"] = True
    coll.objects.link(ob)


# ---------------------------------------------------------------- 2. the lower bowl
def lower_planes():
    """Round the whole bowl: an aisle strip of one width at every section boundary, three seat
    columns a section, and the tunnel's own columns in the south section."""
    half = LOW_AISLE_W / 2
    width = 360.0 / LOW_SECTIONS
    planes, cells = [], []
    for k in range(LOW_SECTIONS):
        ac = LOW_AISLE_0 + k * width
        first = len(planes)
        planes += [Plane(ac, -half), Plane(ac, half)]
        cells.append({"v": "aisle", "aisle": True, "ac": ac})
        lo = ac + math.degrees(math.asin(half / 107.0))
        hi = ac + width - math.degrees(math.asin(half / 107.0))
        if lo < 180.0 < hi:
            inner = [Plane(180.0, -TUNNEL_HALF), Plane(180.0, -TUNNEL_HALF + TUNNEL_JAMB),
                     Plane(180.0, TUNNEL_HALF - TUNNEL_JAMB), Plane(180.0, TUNNEL_HALF)]
            kinds = [{"v": "seat"}, {"v": "tunnel"}, {"v": "tunnel", "open": True}, {"v": "tunnel"}, {"v": "seat"}]
        else:
            inner = [Plane(lo + (hi - lo) / 3), Plane(lo + 2 * (hi - lo) / 3)]
            kinds = [{"v": "seat"}, {"v": "seat"}, {"v": "seat"}]
        planes += inner
        sec = (first + 1, (first + 2 + len(inner)))
        for c in kinds:
            c["sec"] = sec
            cells.append(c)
    total = len(planes)
    for c in cells:
        if "sec" in c:
            c["sec"] = (c["sec"][0], c["sec"][1] % total)
    return planes, cells


def lower_profile(variant):
    seats = variant != "aisle"
    front = 84.5
    if variant == "tunnel":                                   # a porch 0.6 m proud of the wall, the opening in its face
        pts = [(front, LOW_BASE, ("hid",)), (front - 0.6, LOW_BASE, ("hid",)), (front - 0.6, 0.05, ("tunnel",)),
               (front - 0.6, 2.6, ("conc",)), (front - 0.6, 2.72, ("led", "dash", front)), (front - 0.6, 2.98, ("conc",)),
               (front - 0.6, 3.1, ("conc",))]
    else:
        pts = [(front, LOW_BASE, ("hid",)), (front, 0.0, ("conc",)), (front, 1.9, ("led", "front", front)),
               (front, 2.8, ("conc",))]
    pts += [(front, 3.1, ("conc",))] + coping(front, 85.3, 4.1, 3.0)
    pts += bank_rows("low1", seats)
    pts += [(106.0, 12.6, ("walk",)), (110.0, 12.6, ("doorwall",)), (110.0, 15.0, ("conc",)), (110.0, 15.5, ("led", "dash", 110.0)),
            (110.0, 15.8, ("conc",))] + coping(110.0, 110.6, 16.4, 15.4)
    pts += bank_rows("low2", seats)
    pts += [(130.0, 26.0, ("walk",)), (140.0, 26.0, ("conc",)), (140.0, 27.2, ("steel",)), (141.0, 27.2, ("panel", 0.0)),
            (141.0, 8.0, ("led", "cyan", 141.0)), (141.0, 7.5, ("panel", 0.37)), (141.0, 5.0, ("conc",)), (141.0, 0.85, ("gate",)),
            (141.0, DECK_Z + 0.05, ("hid",)), (141.0, LOW_BASE, ("hid",))]
    return pts


def lower_bowl(coll):
    """The whole lower tier and the concourse behind it: ONE solid. Two banks of true-size seat
    rows, 24 stepped aisles of one width, a front rail on each bank, a doorway behind the cross
    walkway at every aisle, the players' tunnel through the front wall to the south, and a gate at
    deck level at each aisle of the open corners."""
    planes, cells = lower_planes()

    def post(bm, uvl, tagged):
        for f, cell in tagged.get("doorwall", ()):
            if cell.get("aisle"):
                pocket(bm, uvl, f, polar(1.0, cell["ac"]), 3.0, "door")
        for f, cell in tagged.get("gate", ()):
            if cell.get("aisle") and not in_upper_arc(cell["ac"], 1.5):
                pocket(bm, uvl, f, -polar(1.0, cell["ac"]), 2.0, "gate")
        for f, cell in tagged.get("tunnel", ()):
            if cell.get("open"):
                pocket(bm, uvl, f, polar(1.0, 180.0), 10.0, "tunnel", floor="track", side="concrete")

    caps = {frozenset(("seat", "aisle")): "concrete", frozenset(("seat", "tunnel")): "concrete"}
    sweep("arena_bowl_lower", planes, cells, {v: lower_profile(v) for v in ("seat", "aisle", "tunnel")}, coll,
          common_shade, cap=lambda a, b: caps[frozenset((a["v"], b["v"]))], post=post)
    return planes, cells


# ---------------------------------------------------------------- 3. the upper stands
def upper_planes(lo, hi):
    half = UP_AISLE_W / 2
    width = (hi - lo) / UP_SECTIONS
    ref = 158.0
    planes = [Plane(lo), Plane(lo, END_WALL), Plane(lo, END_WALL + UP_AISLE_W)]
    cells = [{"v": "wall"}, {"v": "aisle", "aisle": True, "end": True}]
    edges = [len(planes) - 1]
    for k in range(UP_SECTIONS):
        left = planes[-1]
        if k < UP_SECTIONS - 1:
            ac = lo + (k + 1) * width
            right = Plane(ac, -half)
        else:
            right = Plane(hi, -END_WALL - UP_AISLE_W)
        a, b = left.bearing(ref), right.bearing(ref)
        first = len(planes) - 1
        planes += [Plane(a + (b - a) / 3), Plane(a + 2 * (b - a) / 3), right]
        sec = (first, len(planes) - 1)
        cells += [{"v": "seat", "sec": sec}, {"v": "seat", "sec": sec}, {"v": "seat", "sec": sec}]
        if k < UP_SECTIONS - 1:
            planes.append(Plane(ac, half))
            cells.append({"v": "aisle", "aisle": True, "ac": ac})
    planes += [Plane(hi, -END_WALL), Plane(hi)]
    cells += [{"v": "aisle", "aisle": True, "end": True}, {"v": "wall"}]
    return planes, cells


def upper_profile(variant):
    front, outer = 140.2, 180.0
    pts = [(front, UP_BASE, ("hid",)), (front, 27.3, ("conc",)), (front, 28.2, ("led", "ribbon", front)), (front, 31.4, ("conc",)),
           (front, 32.7, ("conc",))]
    if variant == "wall":                                     # the end wall: a parapet stepping up with the rows
        slope = BANKS["up"]["rise"] / BANKS["up"]["tread"]
        pts += [(front, 32.9, ("steel",)), (front - 0.08, 32.9, ("steel",)), (front - 0.08, 33.1, ("conc",)),
                (front - 0.08, 33.5, ("steel",)), (176.0, 33.5 + (176.0 - front) * slope, ("steel",)),
                (outer, 33.5 + (176.0 - front) * slope, ("panel", 0.0))]
    else:
        pts += [(front, 32.9, ("steel",)), (front - 0.08, 32.9, ("steel",)), (front - 0.08, 33.1, ("steel",)),
                (141.08, 33.1, ("steel",)), (141.08, 32.9, ("steel",)), (141.0, 32.9, ("conc",))]
        pts += bank_rows("up", variant == "seat")
        pts += [(176.0, 58.0, ("walk",)), (179.0, 58.0, ("conc",)), (179.0, 59.2, ("steel",))]
    pts += [(outer, 59.2, ("panel", 0.0)), (outer, 46.0, ("led", "magenta", outer)), (outer, 45.5, ("panel", 0.29)),
            (181.2, 30.0, ("led", "dash", 181.2)), (181.2, 29.5, ("panel", 0.61)), (outer, 9.0, ("led", "cyan", outer)),
            (outer, 8.5, ("panel", 0.13)), (outer, 5.0, ("conc",)), (outer, 0.85, ("gate",)), (outer, DECK_Z + 0.05, ("hid",)), (outer, UP_BASE, ("hid",))]
    return pts


def upper_stands(coll):
    """Four stands with open corners, each ONE solid: forty true-size rows, four stepped aisles
    and one along each end, an end wall stepping up with the rows, the LED ribbon on the front,
    the banded outside wall with a gate at each aisle."""
    built = []
    variants = {v: upper_profile(v) for v in ("seat", "aisle", "wall")}

    def post(bm, uvl, tagged):
        for f, cell in tagged.get("gate", ()):
            if cell.get("aisle") and "ac" in cell:
                pocket(bm, uvl, f, -polar(1.0, cell["ac"]), 2.0, "gate")

    for lo, hi in UPPER_ARCS:
        planes, cells = upper_planes(lo, hi)
        sweep("arena_bowl_upper_%03d" % round(((lo + hi) / 2) % 360), planes, cells, variants, coll, common_shade,
              full=False, cap=lambda a, b: "concrete", end_cap="panel", post=post)
        built.append((lo, hi, planes, cells))
    return built


# ---------------------------------------------------------------- 4. what stands on and against them
def rails(coll, low, ups):
    """A hoop rail down the middle of every aisle, between the same two rows of each run: two
    posts standing in the steps and a rail between their heads."""
    s = Solids()

    def hoops(bank, bearing):
        b = BANKS[bank]
        for k0, k1 in HOOPS[bank]:
            tops = []
            for k in (k0, k1):
                r, z = b["r_in"] + k * b["tread"] + b["floor"] / 2, b["z_in"] + k * b["rise"]
                foot, top = polar(r, bearing, z - 0.12), polar(r, bearing, z + 0.96)
                s.tube(foot, top, 0.035)
                tops.append(polar(r, bearing, z + 0.92))
            s.tube(tops[0], tops[1], 0.035)

    for cell in low[1]:
        if cell.get("aisle"):
            hoops("low1", cell["ac"])
            hoops("low2", cell["ac"])
    for lo, hi, planes, cells in ups:
        for cell in cells:
            if cell.get("aisle") and "ac" in cell:
                hoops("up", cell["ac"])
    return s.done("arena_bowl_rails", coll)


def outside(coll, ups):
    """Buttress fins up the outside of the upper stands (between the roof kit's masts, which stand
    at r 184.5 on bearings lo + 3 + 14.5 k), and pilasters on the lower bowl's wall in the corners."""
    s = Solids()
    for lo, hi, planes, cells in ups:
        masts = [lo + 3.0 + (hi - lo - 6.0) * k / 4 for k in range(5)]
        at = [lo + 0.9, hi - 0.9]
        for a, b in zip(masts, masts[1:]):
            at += [a + (b - a) / 3, a + 2 * (b - a) / 3]
        for a in at:
            s.prism([(179.5, DECK_Z - 1.0), (183.0, DECK_Z - 1.0), (182.0, 30.0), (180.9, 56.0), (179.5, 58.6)], a, 1.0, "concrete")
    for c in CORNERS:
        for off in (-9.4, -3.2, 3.2, 9.4):
            s.prism([(140.5, DECK_Z - 1.0), (143.2, DECK_Z - 1.0), (142.0, 25.6), (140.5, 26.8)], c + off, 0.9, "concrete")
    return s.done("arena_bowl_fins", coll)


def stair_towers(coll, ups):
    """A stair tower closing each end of each upper stand: eight-sided, one flat against the end
    wall and 0.25 m into it, a plinth with a gate at deck level, a steel cornice, a lit lantern."""
    apothem = 4.2 * math.cos(math.radians(22.5))
    gap = math.degrees(math.asin((apothem - 0.25) / 160.0))
    n = 0
    for lo, hi, planes, cells in ups:
        for end, a in ((-1, lo), (1, hi)):
            c = polar(160.0, a + end * gap)
            toward = a + end * 90.0                            # the bearing from the tower's axis AWAY from the stand
            ring = [Plane(toward + 22.5 + 45.0 * k) for k in range(8)]
            prof = [(0.0, DECK_Z - 1.0, ("hid",)), (4.7, DECK_Z - 1.0, ("hid",)), (4.7, DECK_Z + 0.05, ("gate",)), (4.7, 0.85, ("conc",)),
                    (4.7, 1.4, ("conc",)), (4.2, 1.9, ("panel", 0.21 * n)), (3.9, 53.0, ("steel",)), (4.4, 53.5, ("steel",)),
                    (4.4, 54.3, ("steel",)), (3.4, 54.7, ("steel",)), (3.4, 56.2, ("led", "cyan", 3.4)), (3.4, 56.7, ("steel",)),
                    (3.4, 57.4, ("steel",)), (3.9, 57.8, ("steel",)), (3.9, 58.4, ("steel",)), (0.0, 60.4, ("steel",))]
            tcells = [{"v": "t", "k": k} for k in range(8)]

            def post(bm, uvl, tagged, toward=toward):
                for f, cell in tagged.get("gate", ()):
                    if cell["k"] == 7:                         # the face that looks away from the stand
                        pocket(bm, uvl, f, -polar(1.0, toward), 1.6, "tower_gate")

            sweep("arena_bowl_tower_%d" % n, ring, tcells, {"t": prof}, coll, common_shade, origin=(c.x, c.y), post=post)
            n += 1


def kiosks(coll):
    """Two kiosks on the concourse in each open corner, built differently: a long counter under a
    flat roof with a sign, and a hatch under a pitched awning. Their feet are in the floor."""
    s = Solids()
    for c in CORNERS:
        a = c - 3.6                                           # A: 6 x 3 x 3 m
        s.box(polar(136.6, a, 27.4), (6.0, 3.0, 3.2), yaw=a, mat="concrete",
              front=("door", TEX.region_uv(TEX.DOOR_REGIONS["kiosk"])))
        s.box(polar(136.0, a, 29.1), (7.2, 4.6, 0.3), yaw=a, mat="steel")
        s.box(polar(133.95, a, 29.5), (6.0, 0.24, 0.6), yaw=a, mat="steel",
              front=("door", TEX.region_uv(TEX.DOOR_REGIONS["kiosk_sign"])))
        a = c + 3.4                                           # B: 4 x 3 x 2.8 m
        s.box(polar(136.8, a, 27.2), (4.0, 3.0, 2.8), yaw=a, mat="panel",
              front=("door", TEX.region_uv(TEX.DOOR_REGIONS["kiosk_b"])))
        s.box(polar(135.9, a, 28.7), (4.8, 4.6, 0.22), yaw=a, pitch=9.0, mat="steel")
    return s.done("arena_bowl_kiosks", coll)


# ---------------------------------------------------------------- the rows, for the crowd kit
def write_rows(low, ups):
    def spans(planes, cells, rs):
        out, seen = [], set()
        for c in cells:
            if c["v"] in ("seat", "tunnel") and c["sec"] not in seen:
                seen.add(c["sec"])
                lo, hi = planes[c["sec"][0]].bearing(rs), planes[c["sec"][1]].bearing(rs)
                if hi < lo:
                    hi += 360.0
                out.append((lo, hi))
        return out

    def bank(label, name, planes, cells):
        b = BANKS[name]
        rows = []
        for k in range(b["n"]):
            r, z = b["r_in"] + k * b["tread"], b["z_in"] + k * b["rise"]
            rs = r + b["floor"] + PAN_D / 2
            rows.append({"r": round(r, 4), "z": round(z, 4), "seat_r": round(rs, 4), "seat_z": round(z + SEAT_H, 4),
                         "back_z": round(z + b["back"], 4),
                         "spans": [[round(lo, 4), round(hi, 4), max(1, round(rs * math.radians(hi - lo) / TEX.SEAT_PITCH_M))]
                                   for lo, hi in spans(planes, cells, rs)]})
        # `sections` is what holds for EVERY row of the bank: the limits at its front row, where an
        # aisle of one width takes the most degrees. A ring's section that crosses north is split.
        sections = []
        for lo, hi in spans(planes, cells, b["r_in"] + b["floor"]):
            if hi > 360.0:
                sections += [[0.0, round(hi - 360.0, 3)], [round(lo, 3), 360.0]]
            else:
                sections.append([round(lo, 3), round(hi, 3)])
        sections.sort()
        aisles = [{"bearing": round(c["ac"], 4), "width_m": LOW_AISLE_W if name != "up" else UP_AISLE_W}
                  for c in cells if c.get("aisle") and "ac" in c]
        return {"name": label, "tread": round(b["tread"], 4), "rise": round(b["rise"], 4), "floor_depth": round(b["floor"], 4),
                "rows": rows, "sections": sections, "aisles": aisles}

    banks = [bank("lower_front", "low1", *low), bank("lower_back", "low2", *low)]
    for lo, hi, planes, cells in ups:
        banks.append(bank("upper_%03d" % round(((lo + hi) / 2) % 360), "up", planes, cells))
    data = {
        "note": "WRITTEN by tools/author_arena_bowl.py from the bowl kit's own geometry; do not edit. Blender frame: metres, "
                "z up, bearings in degrees clockwise from north (+y), the can at the origin. Same keys as the crowd kit's "
                "fallback file, plus more. A row: r and z are its FRONT edge and its floor; seat_r, seat_z the middle of the "
                "seat pan; back_z the top of the seat back. The rows are TRUE SIZE (tread about 0.8 m), twice the blockout's "
                "count: a crowd on every second row has the blockout's density. sections: [lo, hi] bearings that are seats on "
                "EVERY row of the bank (the aisles are " + str(LOW_AISLE_W) + " m wide in the lower bowl and " + str(UP_AISLE_W) +
                " m in the upper stands at every row, so they take fewer degrees further out; a row's exact limits and its "
                "seat count at 0.55 m a seat are in its spans: [lo, hi, seats]). aisles: each aisle's centre bearing and "
                "width. Each upper stand also has an aisle " + str(UP_AISLE_W) + " m wide inside each end wall. Nothing is cut "
                "out of the seats: the players' tunnel runs under the first rows, so voids is empty.",
        "voids": [],
        "banks": banks,
    }
    with open(ROWS_JSON, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=1)
    print("ROWS", os.path.relpath(ROWS_JSON, ROOT), [(b["name"], len(b["rows"]), len(b["sections"])) for b in banks])


# ---------------------------------------------------------------- checks
def report(coll):
    """Per object: triangles, open edges, edges with more than two faces, faces of no area. Per
    material: the texel density along the two axes of every face (px per metre), 5th to 95th
    percentile, and the worst stretch (the ratio of the two)."""
    total, lines = 0, []
    density = {}
    for ob in sorted(coll.objects, key=lambda o: o.name):
        if ob.type != "MESH":
            continue
        bm = bmesh.new()
        bm.from_mesh(ob.data)
        uvl = bm.loops.layers.uv.active
        open_e = sum(1 for e in bm.edges if len(e.link_faces) == 1)
        many = sum(1 for e in bm.edges if len(e.link_faces) > 2)
        loose = sum(1 for e in bm.edges if len(e.link_faces) == 0)
        thin = sum(1 for f in bm.faces if f.calc_area() < 1e-7)
        tris = sum(len(f.verts) - 2 for f in bm.faces)
        for f in bm.faces:
            name = ob.data.materials[f.material_index].name
            img_w, img_h = (1024.0, 512.0) if name.endswith("_led") else (1024.0, 1024.0)
            lp = f.loops
            p0, p1, p2 = lp[0].vert.co, lp[1].vert.co, lp[-1].vert.co
            e1, e2 = p1 - p0, p2 - p0
            n = e1.cross(e2)
            if n.length < 1e-9:
                continue
            x = e1.normalized()
            y = n.cross(e1).normalized()
            a = (e1.dot(x), e1.dot(y))
            b = (e2.dot(x), e2.dot(y))
            du1, dv1 = lp[1][uvl].uv - lp[0][uvl].uv
            du2, dv2 = lp[-1][uvl].uv - lp[0][uvl].uv
            det = a[0] * b[1] - a[1] * b[0]
            # J maps metres to uv: [du/dx du/dy; dv/dx dv/dy]
            j11 = (du1 * b[1] - du2 * a[1]) / det
            j12 = (-du1 * b[0] + du2 * a[0]) / det
            j21 = (dv1 * b[1] - dv2 * a[1]) / det
            j22 = (-dv1 * b[0] + dv2 * a[0]) / det
            j11, j12, j21, j22 = j11 * img_w, j12 * img_w, j21 * img_h, j22 * img_h
            s = j11 * j11 + j12 * j12 + j21 * j21 + j22 * j22
            d = abs(j11 * j22 - j12 * j21)
            big = math.sqrt(max(0.0, (s + math.sqrt(max(0.0, s * s - 4 * d * d))) / 2))
            small = d / big if big > 1e-12 else 0.0
            density.setdefault(name, []).append((big, small))
        bm.free()
        total += tris
        decal = bool(ob.get("decal"))
        flag = "" if (open_e == 0 or decal) and many == 0 and loose == 0 and thin == 0 else "   <-- OPEN %d MANY %d LOOSE %d THIN %d" % (open_e, many, loose, thin)
        if decal:
            flag += "   (a decal: one face each, open by design)"
        lines.append("MESH %-28s tris %7d  materials %2d%s" % (ob.name, tris, len(ob.data.materials), flag))
    lines.append("TOTAL TRIANGLES %d   MATERIALS %d" % (total, len({m.name for o in coll.objects if o.type == 'MESH' for m in o.data.materials})))
    for name in sorted(density):
        v = density[name]
        hi = sorted(x[0] for x in v)
        lo = sorted(x[1] for x in v)
        ratio = sorted(x[0] / x[1] for x in v if x[1] > 1e-9)
        q = lambda arr, t: arr[min(len(arr) - 1, int(t * len(arr)))]
        lines.append("TEXELS %-22s px/m %6.1f to %6.1f (5th..95th pct)   stretch median %.2f, 95th pct %.2f, worst %.2f" % (
            name, q(lo, 0.05), q(hi, 0.95), q(ratio, 0.5), q(ratio, 0.95), ratio[-1]))
    for ln in lines:
        print(ln)
    return lines


# ---------------------------------------------------------------- pictures
def shoot(name, loc, target, lens=35.0):
    scene = bpy.context.scene
    cam_data = bpy.data.cameras.new(name)
    cam = bpy.data.objects.new(name, cam_data)
    scene.collection.objects.link(cam)
    cam.location = loc
    cam.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
    cam_data.lens = lens
    cam_data.clip_start = 0.2
    cam_data.clip_end = 3000
    scene.camera = cam
    scene.render.filepath = os.path.join(LOGS, name + ".png")
    bpy.ops.render.render(write_still=True)
    return cam


def main():
    global CHECKER
    version, render = "v1", True
    for a in sys.argv:
        if a.startswith("--version="):
            version = a.split("=", 1)[1]
        if a == "--checker":
            CHECKER = True
        if a == "--no-render":
            render = False
    bpy.ops.wm.read_factory_settings()
    for ob in list(bpy.data.objects):
        bpy.data.objects.remove(ob)
    scene = bpy.context.scene
    coll = collection(KIT)
    field(coll)
    low = lower_bowl(coll)
    ups = upper_stands(coll)
    rails(coll, low, ups)
    outside(coll, ups)
    stair_towers(coll, ups)
    kiosks(coll)
    write_rows(low, ups)
    lines = report(coll)
    os.makedirs(LOGS, exist_ok=True)
    with open(os.path.join(LOGS, "bowl_%s_report.txt" % version), "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")

    preview = []
    if render:
        # Four floodlight keys, each from over one stand toward the stand opposite, and a soft top light.
        for k, b in enumerate((0, 90, 180, 270)):
            d = polar(1.0, b) * math.cos(math.radians(38)) + Vector((0, 0, -math.sin(math.radians(38))))
            sun = bpy.data.objects.new("key %d" % b, bpy.data.lights.new("key %d" % b, "SUN"))
            sun.data.energy = 1.5
            sun.data.color = (0.86, 0.93, 1.0)
            sun.data.angle = math.radians(20)
            sun.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
            sun.data.use_shadow = k < 2
            scene.collection.objects.link(sun)
            preview.append(sun)
        top = bpy.data.objects.new("top", bpy.data.lights.new("top", "SUN"))
        top.data.energy = 0.8
        top.data.color = (0.8, 0.88, 1.0)
        scene.collection.objects.link(top)
        preview.append(top)
        world = bpy.data.worlds.new("night")
        scene.world = world
        world.use_nodes = True
        world.node_tree.nodes["Background"].inputs[0].default_value = (0.012, 0.018, 0.065, 1)
        world.node_tree.nodes["Background"].inputs[1].default_value = 0.6
        engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
        eevee = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
        scene.render.engine = eevee
        scene.render.resolution_x, scene.render.resolution_y = 1920, 1080
        scene.view_settings.view_transform = "Standard"
        # People for scale in the close pictures only: 1.7 m, never saved with the kit.
        people = Solids()
        low1, low2, up = BANKS["low1"], BANKS["low2"], BANKS["up"]
        for r, a, z in ((low1["r_in"] + 12 * low1["tread"] + 0.2, 9.5, low1["z_in"] + 12 * low1["rise"]), (108.0, 6.6, 12.6),
                        (78.0, 178.6, 0.0), (up["r_in"] + 14 * up["tread"] + 0.2, 7.2, up["z_in"] + 14 * up["rise"]),
                        (133.0, 43.0, 26.0)):
            people.box(polar(r, a, z + 0.85), (0.5, 0.3, 1.7), yaw=a, mat="step")
        fig = people.done("scale people (preview only)", scene.collection)
        preview.append(fig)
        p = "bowl_%s%s_" % (version, "_checker" if CHECKER else "")

        def cam(name, loc, target, lens):
            preview.append(shoot(p + name, loc, target, lens))

        t = polar(100.0, 0.0, 26.0)
        cam("01_eye_stand", (0, 0, EYE), t, 18)
        cam("02_eye_corner", (0, 0, EYE), polar(100.0, 45.0, 24.0), 18)
        cam("03_eye_tunnel", (0, 0, EYE), polar(85.0, 180.0, 7.0), 30)
        cam("04_top_row", polar(175.6, 8.0, 60.0), polar(40.0, -30.0, 0.0), 20)
        r12, z12 = low1["r_in"] + 14 * low1["tread"], low1["z_in"] + 14 * low1["rise"]
        cam("05_seats", polar(r12 + 0.3, 12.6, z12 + 1.5), polar(r12 - 4.0, 8.4, z12 - 2.2), 26)
        cam("06_aisle", polar(107.6, 7.5, 14.3), polar(88.0, 7.5, 3.6), 24)
        cam("07_aisle_up", polar(87.6, 9.0, 5.4), polar(110.0, 7.5, 14.0), 22)
        cam("08_doorway", polar(107.4, 5.6, 14.3), polar(110.6, 7.6, 13.8), 22)
        cam("08b_doorway_front", polar(106.4, 7.5, 14.3), polar(111.0, 7.5, 13.8), 30)
        cam("09_tunnel", polar(73.0, 175.6, 1.7), polar(84.0, 180.0, 1.9), 24)
        cam("10_upper_aisle", polar(146.0, 6.75, 37.5), polar(170.0, 6.4, 54.0), 22)
        cam("11_concourse", polar(131.5, 36.5, 27.7), polar(137.0, 46.5, 27.6), 20)
        cam("12_air", (170, -270, 300), (0, 10, 0), 28)
        cam("13_outside", (310, -310, 70), (150, -150, 22), 30)
        cam("14_end_wall", polar(150.0, 45.0, 40.0), polar(160.0, 30.0, 40.0), 20)
        if not CHECKER:
            scene.render.engine = "BLENDER_WORKBENCH"
            scene.display.shading.light = "STUDIO"
            scene.display.shading.color_type = "MATERIAL"
            scene.display.shading.show_cavity = True
            cam("20_flat_stand", (60, -60, 50), polar(120.0, 20.0, 20.0), 30)
            cam("21_flat_seats", polar(r12 + 0.3, 12.6, z12 + 1.5), polar(r12 - 4.0, 8.4, z12 - 2.2), 26)
            cam("22_flat_outside", (310, -310, 70), (150, -150, 22), 30)
            cam("23_flat_doorway", polar(106.6, 5.0, 14.2), polar(110.6, 7.6, 13.9), 22)
            scene.render.engine = eevee
    for ob in preview:
        bpy.data.objects.remove(ob)
    if not CHECKER:
        for c in list(bpy.data.collections):                      # only the kit is saved
            if c.name != KIT:
                bpy.data.collections.remove(c)
        for block in (bpy.data.materials, bpy.data.meshes, bpy.data.cameras, bpy.data.lights):
            for d in list(block):
                if d.users == 0:
                    block.remove(d)
        bpy.context.preferences.filepaths.save_version = 0
        os.makedirs(os.path.dirname(BLEND), exist_ok=True)
        bpy.ops.wm.save_as_mainfile(filepath=BLEND)
        bpy.ops.file.make_paths_relative()
        bpy.ops.wm.save_mainfile()
    print("BOWL_OK", version)


if __name__ == "__main__":
    main()
