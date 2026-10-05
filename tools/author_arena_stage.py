"""Build the arena STAGE kit: the hover platforms of all five layouts, and the gameplay props.

  py -3 tools/author_arena_layouts.py                 # the layout data (tools/arena_layouts.json)
  py -3 tools/author_arena_textures_stage.py          # the textures
  blender -b --python tools/author_arena_stage.py -- --version=vN [--no-render]

Writes ArtSource/arena/kits/stage.blend and review pictures Logs/arena/stage/stage_<vN>_*.png, and
prints the open-edge and triangle report (also saved as Logs/arena/stage/stage_<vN>_report.txt).
Read docs/ARENA_ART_BRIEF.md first. Blender units are metres, z up, y north, the can at the origin:
a Unity point (x, y, z) is Blender (x, z, y).

THE COLLECTIONS (all inside `arena_stage`, everything at its place in the stadium's frame):
  stage_plaza, stage_tore, stage_krus, stage_hukay, stage_entablado
        one mesh per piece of that layout, named stage_<layout>_<id>. All five sit at the origin
        on top of each other, so only ONE is shown at a time (the file is saved showing plaza).
        Each has a child collection stage_<layout>_props: the pads and pickups of that layout as
        linked copies of the prop meshes, for review only. The game places props from the JSON.
  stage_props
        the props, each at the origin: jump_base, jump_cushion, jump_chevron, jump_ring,
        speed_base, speed_chevrons, pickup_base, pickup_cell, pickup_halo, drone_body,
        drone_rotor, drone_beam, and stage_shaft_rim (in place at radius 40).
  stage_hologram_preview
        the tore layout again wearing the hologram material, lifted 6 cm: how the NEXT layout is
        shown before it turns solid. Review only; the game swaps the material on the real meshes.
  preview_only
        figures at the marks (in the role hues, to judge the deck against them), the can, a slab
        of dark field and the shaft wall. Not part of the kit.

HOW A PLATE IS BUILT. Every piece is ONE closed solid with one cross-section (`section`):
  a pale DECK; a white LINE 7 cm wide, 35 cm in from the edge; a dark band; the edge's 10 cm
  CHAMFER and the first 8 cm of the side, which are the LIT RIM (so the edge reads from above and
  from the side); a dark side; a skirt cut back 55 cm; a recessed underside of ribs and glowing
  hover cells; and a keel along the middle whose bottom face is the hover emitter. The bands are
  faces of the same surface, told apart by material; nothing is laid on top of anything.
    disc, ring   that section swept round (arena_kit.lathe)
    arc          nested loops of the arc's outline, each inset by the section's distance, so the
                 two ENDS get the same line, rim, skirt and keel as the long edges
    ramp         the section swept along the bearing. Each station sits at one TRUE radius, so
                 both ends are arcs flush with their round neighbours. Past each end the ramp
                 runs a 62 cm tongue INTO the neighbour: its top steps down 12 cm there, so it is
                 never in the neighbour's deck plane, and the first 7 cm keeps the full top to
                 cover the neighbour's chamfer.

MATERIALS (9; textures tools/author_arena_textures_stage.py): arena_stage_deck, _line, _rim,
_hull, _under, _mark, _props, _beam, _holo.
UVS: deck and holo are a plain top-down projection at 8 m a tile (no stretching, and the hexagons
run on across pieces); line, rim, hull and under are top-down at 4 m on level faces and
(run along the face, height) on upright ones; the mark is 0..1 over 3 m; the props use one atlas.
"""
import bpy, bmesh, json, math, os, sys
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import arena_kit as K                                             # noqa: E402
from arena_kit import polar, collection, lathe, circle            # noqa: E402

ROOT = K.ROOT
TEX = os.path.join(ROOT, "Assets", "TumbangPreso", "Art", "Arena", "Textures")
KITS = os.path.join(ROOT, "ArtSource", "arena", "kits")
LOGS = os.path.join(ROOT, "Logs", "arena", "stage")
DATA = json.load(open(os.path.join(ROOT, "tools", "arena_layouts.json"), encoding="utf-8"))

SEG = 128                          # a full circle
CHAMFER, LINE_IN, LINE_W, SKIRT, LIP, RECESS = 0.10, 0.35, 0.07, 0.55, 0.80, 0.12
TONGUE, COVER, STEP = 0.62, 0.07, 0.12
MATS = ["deck", "line", "rim", "hull", "under", "mark"]
GLOW = {"deck": 1.0, "line": 1.0, "rim": 3.2, "under": 2.2, "mark": 1.4, "props": 1.15, "holo": 2.6}

# The props atlas (tools/author_arena_textures_stage.py repeats these): pixels, top-left origin.
REGIONS = {
    "jump_cushion": (0, 0, 256, 256), "jump_base": (256, 0, 512, 256), "speed_top": (512, 0, 768, 512),
    "metal_dark": (768, 0, 1024, 256), "metal_light": (768, 256, 1024, 512), "pickup_cell": (0, 256, 256, 512),
    "drone_top": (256, 256, 512, 512),
    "sw_teal": (0, 512, 128, 640), "sw_lime": (128, 512, 256, 640), "sw_violet": (256, 512, 384, 640),
    "sw_ice": (384, 512, 512, 640), "sw_gold": (512, 512, 640, 640), "sw_glass": (640, 512, 768, 640),
    "sw_cream": (768, 512, 896, 640), "sw_black": (896, 512, 1024, 640),
}


# ---------------------------------------------------------------- materials
def material(key, emit=False, alpha=False, strength=1.0):
    name = "arena_stage_%s" % key
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    b = nt.nodes.get("Principled BSDF")
    b.inputs["Roughness"].default_value = 0.85
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(os.path.join(TEX, name + ".png"), check_existing=True)
    nt.links.new(tex.outputs["Color"], b.inputs["Base Color"])
    if alpha:
        nt.links.new(tex.outputs["Alpha"], b.inputs["Alpha"])
        nt.links.new(tex.outputs["Color"], b.inputs["Emission Color"])
        b.inputs["Emission Strength"].default_value = strength
        m.surface_render_method = "BLENDED"
        m.use_backface_culling = False
    elif emit:
        e = nt.nodes.new("ShaderNodeTexImage")
        e.image = bpy.data.images.load(os.path.join(TEX, name + "_emit.png"), check_existing=True)
        nt.links.new(e.outputs["Color"], b.inputs["Emission Color"])
        b.inputs["Emission Strength"].default_value = strength
    K._mats[(key, 1.0)] = m                                       # arena_kit.mat(key) now answers this material
    return m


def materials():
    for key in ("deck", "line", "rim", "under", "mark", "props"):
        material(key, emit=True, strength=GLOW[key])
    material("hull")
    material("holo", alpha=True, strength=GLOW["holo"])
    material("beam", alpha=True, strength=1.0)


# ---------------------------------------------------------------- the plate's cross-section
def edge(thick):
    """From the deck's boundary out over the edge and back under: (inset, height below the top)
    and the material of each strip between two points."""
    side = min(0.42, thick * 0.5)
    pts = [(LINE_IN + LINE_W, 0.0), (LINE_IN, 0.0), (CHAMFER, 0.0), (0.0, -CHAMFER), (0.0, -CHAMFER - 0.08),
           (0.0, -side), (SKIRT, -thick), (LIP, -thick), (LIP, -thick + RECESS)]
    tags = ["line", "hull", "rim", "rim", "hull", "hull", "hull", "hull"]
    return pts, tags


def section(lo, hi, thick, keel=0.20, keels=None):
    """The closed cross-section across a plate from coordinate `lo` to `hi` (a radius, or an offset
    across a ramp): a list of (coordinate, height below the top, material of the strip to the
    next point). It starts at the deck's low-side boundary and runs across the deck first."""
    pts, tags = edge(thick)
    out = [(lo + pts[0][0], 0.0, "deck")]
    for i, (d, z) in enumerate(pts):                              # over the high edge and under
        out.append((hi - d, z, tags[i] if i < len(tags) else "under"))
    base = -thick + RECESS
    for c in sorted(keels if keels is not None else [(lo + hi) / 2], reverse=True):
        out += [(c + 0.25, base, "hull"), (c + 0.12, -thick - keel, "rim"), (c - 0.12, -thick - keel, "hull"), (c - 0.25, base, "under")]
    for i in range(len(pts) - 1, 0, -1):                          # back up over the low edge
        out.append((lo + pts[i][0], pts[i][1], tags[i - 1]))
    return out


def disc_section(r, thick, mark, keels):
    pts, tags = edge(thick)
    base = -thick + RECESS
    out = [(0.0, 0.0, "mark" if mark else "deck")]
    if mark:
        out.append((1.5, 0.0, "deck"))
    for i, (d, z) in enumerate(pts):
        out.append((r - d, z, tags[i] if i < len(tags) else "under"))
    for c in sorted(keels, reverse=True):
        out += [(c + 0.25, base, "hull"), (c + 0.12, -thick - 0.20, "rim"), (c - 0.12, -thick - 0.20, "hull"), (c - 0.25, base, "under")]
    out += [(1.15, base, "hull"), (0.8, -thick - 0.34, "rim"), (0.0, -thick - 0.34, "rim")]     # the centre emitter
    return out


# ---------------------------------------------------------------- the three builders
def build_round(name, p, coll, mark=False):
    top, thick = p["top"], p["thick"]
    if p["kind"] == "disc":
        keels = [p["r"] * 0.58] if p["r"] > 7.0 else []
        prof = disc_section(p["r"], thick, mark, keels)
    else:
        prof = section(p["r0"], p["r1"], thick)
    return lathe(name, [(r, top + z, t) for r, z, t in prof], circle(SEG), MATS, coll)


def build_arc(name, p, coll):
    r0, r1, a0, a1, top, thick = p["r0"], p["r1"], p["a0"], p["a1"], p["top"], p["thick"]
    n = max(3, int(math.ceil((a1 - a0) / (360.0 / SEG))))
    bm = bmesh.new()
    index = {m: i for i, m in enumerate(MATS)}

    def rows(d, z):
        out = []
        for rr in (r1 - d, r0 + d):
            dl = math.degrees(math.asin(min(1.0, d / rr))) if d > 0 else 0.0
            out.append([bm.verts.new(polar(rr, a0 + dl + (a1 - a0 - 2 * dl) * i / n, top + z)) for i in range(n + 1)])
        return out

    def fill(r, tag):
        for i in range(n):
            bm.faces.new((r[0][i], r[0][i + 1], r[1][i + 1], r[1][i])).material_index = index[tag]

    pts, tags = edge(thick)
    half = (r1 - r0) / 2
    base = -thick + RECESS
    stations = list(zip(pts, [None] + tags)) + [((half - 0.25, base), "under"), ((half - 0.12, -thick - 0.20), "hull")]
    prev = None
    for (d, z), tag in stations:
        cur = rows(d, z)
        if prev is None:
            fill(cur, "deck")
        else:
            a, b = prev[0] + prev[1][::-1], cur[0] + cur[1][::-1]
            m = len(a)
            for i in range(m):
                k = (i + 1) % m
                bm.faces.new((a[i], a[k], b[k], b[i])).material_index = index[tag]
        prev = cur
    fill(prev, "rim")
    return K.finish(bm, name, MATS, coll)


def build_ramp(name, p, coll):
    b = math.radians(p["bearing"])
    along, right = Vector((math.sin(b), math.cos(b), 0)), Vector((math.cos(b), -math.sin(b), 0))
    r0, r1, z0, z1, w, thick = p["r0"], p["r1"], p["z0"], p["z1"], p["width"], p["thick"]
    prof = section(-w / 2, w / 2, thick, keel=0.15)
    index = {m: i for i, m in enumerate(MATS)}
    mids = max(1, int(round((r1 - r0) / 2.5)))
    stations = [(r0 - TONGUE, True), (r0 - COVER, True), (r0 - COVER, False)] + \
               [(r0 + (r1 - r0) * i / mids, False) for i in range(mids + 1)] + \
               [(r1 + COVER, False), (r1 + COVER, True), (r1 + TONGUE, True)]
    bm = bmesh.new()
    cols = []
    for rho, low in stations:
        h = z0 + (z1 - z0) * min(1.0, max(0.0, (rho - r0) / (r1 - r0)))
        col = []
        for s, z, _ in prof:
            y = math.sqrt(max(rho * rho - s * s, 0.0))
            zz = z - STEP if (low and z > -1e-6) else z
            col.append(bm.verts.new(along * y + right * s + Vector((0, 0, h + zz))))
        cols.append(col)
    n = len(prof)
    for i in range(len(cols) - 1):
        for j in range(n):
            k = (j + 1) % n
            bm.faces.new((cols[i][j], cols[i][k], cols[i + 1][k], cols[i + 1][j])).material_index = index[prof[j][2]]
    bm.faces.new(cols[0][::-1]).material_index = index["hull"]
    bm.faces.new(cols[-1]).material_index = index["hull"]
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0005)
    dead = [f for f in bm.faces if f.calc_area() < 1e-8]
    if dead:
        bmesh.ops.delete(bm, geom=dead, context="FACES")
    return K.finish(bm, name, MATS, coll)


# ---------------------------------------------------------------- UVs and shading for the plates
def dress(ob, piece=None):
    me = ob.data
    bm = bmesh.new(); bm.from_mesh(me)
    uv = bm.loops.layers.uv.new("UVMap")
    names = [m.name.replace("arena_stage_", "") for m in me.materials]
    kind = piece["kind"] if piece else None
    if kind in ("ring", "arc"):
        r_mid = (piece["r0"] + piece["r1"]) / 2
        tiles = max(1, round(2 * math.pi * r_mid / 4.0))          # whole tiles round the circle, so a ring has no seam
    if kind == "ramp":
        b = math.radians(piece["bearing"])
        along, right = Vector((math.sin(b), math.cos(b), 0)), Vector((math.cos(b), -math.sin(b), 0))
    for f in bm.faces:
        key = names[f.material_index]
        n = f.normal
        c = f.calc_center_median()
        for loop in f.loops:
            co = loop.vert.co
            if key == "mark":
                loop[uv].uv = (co.x / 3.0 + 0.5, co.y / 3.0 + 0.5)
            elif key == "deck":
                loop[uv].uv = (co.x / 8.0, co.y / 8.0)
            elif key == "under" and kind in ("ring", "arc"):
                a0 = math.atan2(c.x, c.y)
                a = a0 + (math.atan2(co.x, co.y) - a0 + math.pi) % (2 * math.pi) - math.pi
                loop[uv].uv = (a / (2 * math.pi) * tiles, math.hypot(co.x, co.y) / 4.0)
            elif key == "under" and kind == "ramp":
                loop[uv].uv = (co.dot(right) / 4.0 + 0.5, co.dot(along) / 4.0)
            elif abs(n.z) > 0.6:
                loop[uv].uv = (co.x / 4.0, co.y / 4.0)
            else:
                t = Vector((-n.y, n.x, 0)).normalized()
                loop[uv].uv = (co.dot(t) / 4.0, co.z / 4.0)
        f.smooth = True
    for e in bm.edges:
        if len(e.link_faces) == 2:
            e.smooth = e.calc_face_angle(0.0) < math.radians(28)
    bm.to_mesh(me); bm.free()


# ---------------------------------------------------------------- the props
def region_uv(name, fx, fy):
    """fx, fy in -1..1 across the region (fy up the picture) to a UV."""
    x0, y0, x1, y1 = REGIONS[name]
    px = (x0 + x1) / 2 + max(-0.96, min(0.96, fx)) * (x1 - x0) / 2
    py = (y0 + y1) / 2 - max(-0.96, min(0.96, fy)) * (y1 - y0) / 2
    return (px / 1024.0, 1.0 - py / 1024.0)


class Prop:
    """One prop mesh: faces carry a paint rule, UVs are written at the end.
    Rules: ("top", region, half_x, half_y)  a top-down drawing
           ("sw", region)                    a flat swatch
           ("box", region)                   painted metal, 1 m across the region
           ("skin", region, z0, z1)          u round the axis, v up from z0 to z1"""

    def __init__(self):
        self.bm = bmesh.new()
        self.rule = {}

    def face(self, verts, rule):
        vs = []
        for v in verts:
            if v not in vs:
                vs.append(v)
        if len(vs) < 3:
            return
        f = self.bm.faces.new(vs)
        self.rule[f.index if False else f] = rule

    def lathe(self, profile, sides, origin=(0, 0, 0), turn=0.0):
        """profile: (r, z, rule) closed loop; rule names the strip to the next point."""
        o = Vector(origin)
        axis = {}
        cols = []
        for i in range(sides):
            a = turn + 360.0 * i / sides
            col = []
            for j, (r, z, _) in enumerate(profile):
                if r < 1e-6:
                    if j not in axis:
                        axis[j] = self.bm.verts.new(o + Vector((0, 0, z)))
                    col.append(axis[j])
                else:
                    col.append(self.bm.verts.new(o + polar(r, a, z)))
            cols.append(col)
        n = len(profile)
        for i in range(sides):
            p, q = cols[i], cols[(i + 1) % sides]
            for j in range(n):
                k = (j + 1) % n
                self.face((p[j], p[k], q[k], q[j]), profile[j][2])
        return self

    def prism(self, outline, z0, z1, rule_top, rule_side, origin=(0, 0, 0), upright=False, quads=None):
        """An outline (x, y) extruded from z0 to z1. `upright` stands it in the XZ plane instead
        (the outline's y becomes z, the extrusion runs along y). `quads` splits the two caps."""
        o = Vector(origin)

        def place(x, y, z):
            return o + (Vector((x, z, y)) if upright else Vector((x, y, z)))

        lo = [self.bm.verts.new(place(x, y, z0)) for x, y in outline]
        hi = [self.bm.verts.new(place(x, y, z1)) for x, y in outline]
        n = len(outline)
        for i in range(n):
            k = (i + 1) % n
            self.face((lo[i], lo[k], hi[k], hi[i]), rule_side)
        for part in (quads or [list(range(n))]):
            self.face([hi[i] for i in part], rule_top)
            self.face([lo[i] for i in part][::-1], rule_top if upright else rule_side)
        return self

    def box(self, centre, size, rule, yaw=0.0):
        hx, hy, hz = size[0] / 2, size[1] / 2, size[2] / 2
        c, s = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
        vs = []
        for sx in (-1, 1):
            for sy in (-1, 1):
                for sz in (-1, 1):
                    x, y = sx * hx, sy * hy
                    vs.append(self.bm.verts.new((centre[0] + x * c + y * s, centre[1] - x * s + y * c, centre[2] + sz * hz)))
        for f in ((0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)):
            self.face([vs[i] for i in f], rule)
        return self

    def done(self, name, coll, smooth=28.0):
        bm = self.bm
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        uv = bm.loops.layers.uv.new("UVMap")
        for f, rule in self.rule.items():
            if not f.is_valid:
                continue
            n = f.normal
            for loop in f.loops:
                co = loop.vert.co
                kind = rule[0]
                if kind == "top":
                    loop[uv].uv = region_uv(rule[1], co.x / rule[2], co.y / rule[3])
                elif kind == "sw":
                    loop[uv].uv = region_uv(rule[1], co.x * 0.3, co.y * 0.3)
                elif kind == "skin":
                    a = (math.atan2(co.x, co.y) / (2 * math.pi)) % 1.0
                    if a < 0.02 and f.calc_center_median().x < 0:
                        a = 1.0
                    loop[uv].uv = region_uv(rule[1], a * 2 - 1, (co.z - rule[2]) / (rule[3] - rule[2]) * 2 - 1)
                else:
                    if abs(n.z) > 0.6:
                        loop[uv].uv = region_uv(rule[1], co.x * 0.9, co.y * 0.9)
                    else:
                        t = Vector((-n.y, n.x, 0)).normalized()
                        loop[uv].uv = region_uv(rule[1], co.dot(t) * 0.9, co.z * 0.9)
            f.smooth = True
        for e in bm.edges:
            if len(e.link_faces) == 2:
                e.smooth = e.calc_face_angle(0.0) < math.radians(smooth)
        me = bpy.data.meshes.new(name)
        bm.to_mesh(me); bm.free()
        me.materials.append(K.mat("props"))
        ob = bpy.data.objects.new(name, me)
        coll.objects.link(ob)
        return ob


def chevron_outline(half_w, drop, thick, tip=0.0):
    """A '^' pointing +y, six corners, with the two quads that make it up."""
    h = thick / 2
    pts = [(0, tip + h), (half_w, tip - drop + h), (half_w, tip - drop - h), (0, tip - h), (-half_w, tip - drop - h), (-half_w, tip - drop + h)]
    return pts, [[0, 1, 2, 3], [0, 3, 4, 5]]


def props(coll):
    out = {}
    dark, light = ("box", "metal_dark"), ("box", "metal_light")
    # ---- the jump pad: the Ilalim pad's four parts and measures, restyled round and teal
    jb = ("top", "jump_base", 0.94, 0.94)
    out["jump_base"] = Prop().lathe([(0, 0.02, jb), (0.585, 0.02, jb), (0.585, 0.085, jb), (0.79, 0.085, jb), (0.85, 0.03, jb),
                                     (0.85, -0.01, dark), (0, -0.01, dark)], 40).done("jump_base", coll)
    jc = ("top", "jump_cushion", 0.66, 0.66)
    out["jump_cushion"] = Prop().lathe([(0, 0.095, jc), (0.22, 0.088, jc), (0.40, 0.068, jc), (0.52, 0.044, jc), (0.575, 0.026, jc),
                                        (0.575, 0.0, jc), (0, 0.0, jc)], 40).done("jump_cushion", coll, smooth=50)
    pts, quads = chevron_outline(0.35, 0.27, 0.15, tip=0.135)
    out["jump_chevron"] = Prop().prism(pts, -0.06, 0.06, ("sw", "sw_cream"), ("sw", "sw_teal"), upright=True, quads=quads).done("jump_chevron", coll)
    ring = ("sw", "sw_cream")
    out["jump_ring"] = Prop().lathe([(0.605, -0.015, ring), (0.695, -0.015, ring), (0.695, 0.015, ring), (0.605, 0.015, ring)], 40).done("jump_ring", coll)

    # ---- the speed pad: a low tray 1.5 by 3.0, travel along +y, three raised chevrons
    def tray(d):
        x, y, c = 0.75 - d, 1.5 - d, 0.14
        return [(-x + c, -y), (x - c, -y), (x, -y + c), (x, y - c), (x - c, y), (-x + c, y), (-x, y - c), (-x, -y + c)]
    sp = Prop()
    st = ("top", "speed_top", 0.80, 1.60)
    loops = [[sp.bm.verts.new((x, y, z)) for x, y in tray(d)] for d, z in ((0.0, -0.01), (0.0, 0.025), (0.045, 0.06))]
    for a, b, rule in ((loops[0], loops[1], dark), (loops[1], loops[2], st)):
        for i in range(8):
            sp.face((a[i], a[(i + 1) % 8], b[(i + 1) % 8], b[i]), rule)
    sp.face(loops[2], st); sp.face(loops[0][::-1], dark)
    out["speed_base"] = sp.done("speed_base", coll)
    ch = Prop()
    for k in range(3):
        pts, quads = chevron_outline(0.47, 0.42 * 0.94, 0.30, tip=-0.62 + k * 0.80)
        ch.prism(pts, 0.035, 0.085, ("sw", "sw_lime"), ("sw", "sw_lime"), quads=quads)
    out["speed_chevrons"] = ch.done("speed_chevrons", coll)

    # ---- the stamina pickup: a small base, a floating six-sided cell with a collar, a halo
    lens = ("sw", "sw_violet")
    out["pickup_base"] = Prop().lathe([(0, 0.085, lens), (0.15, 0.085, dark), (0.17, 0.11, dark), (0.23, 0.11, dark), (0.34, 0.05, dark),
                                       (0.37, 0.0, dark), (0.37, -0.01, dark), (0, -0.01, dark)], 24).done("pickup_base", coll)
    skin = ("skin", "pickup_cell", -0.27, 0.27)
    cell = Prop().lathe([(0, 0.27, skin), (0.13, 0.13, skin), (0.17, 0.0, skin), (0.13, -0.13, skin), (0, -0.27, skin)], 6)
    cell.lathe([(0.15, -0.035, dark), (0.205, -0.035, dark), (0.205, 0.035, dark), (0.15, 0.035, dark)], 6)
    out["pickup_cell"] = cell.done("pickup_cell", coll, smooth=10)
    out["pickup_halo"] = Prop().lathe([(0.30, -0.012, lens), (0.335, -0.012, lens), (0.335, 0.012, lens), (0.30, 0.012, lens)], 32).done("pickup_halo", coll)

    # ---- the catch drone: 1.62 m across the ducts. +y is its nose.
    dt = ("top", "drone_top", 0.50, 0.50)
    ice, gold, glass = ("sw", "sw_ice"), ("sw", "sw_gold"), ("sw", "sw_glass")
    d = Prop()
    d.lathe([(0, 0.17, dt), (0.17, 0.165, dt), (0.24, 0.14, dt), (0.37, 0.07, dt), (0.43, 0.0, light), (0.37, -0.07, light),
             (0.22, -0.12, dark), (0.17, -0.12, dark), (0.155, -0.23, dark), (0.115, -0.23, ice), (0.09, -0.17, ice), (0, -0.17, ice)], 24)
    for k, a in enumerate((45, 135, 225, 315)):
        c = polar(0.55, a, 0.0)
        mid = polar(0.44, a, -0.035)
        d.box((mid.x, mid.y, mid.z), (0.07, 0.36, 0.05), dark, yaw=a)                      # the arm: from inside the shell to inside the hub
        d.lathe([(0.215, -0.07, ice), (0.26, -0.07, light), (0.26, 0.07, light), (0.215, 0.07, dark)], 20, origin=(c.x, c.y, 0))     # the duct: its lower lip is the lifter's light
        d.lathe([(0, 0.02, dark), (0.05, 0.02, dark), (0.05, -0.07, dark), (0, -0.07, dark)], 10, origin=(c.x, c.y, 0))               # the hub
        lamp = polar(0.55 + 0.262, a, 0.0)
        d.box((lamp.x, lamp.y, 0.0), (0.07, 0.03, 0.05), gold if a in (45, 315) else ice, yaw=a)
    d.box((0, 0.40, -0.005), (0.16, 0.12, 0.09), glass)                                    # the camera in the nose
    d.box((0, 0.465, -0.005), (0.07, 0.03, 0.05), gold)
    out["drone_body"] = d.done("drone_body", coll)
    rot = Prop().box((0, 0, 0.045), (0.40, 0.045, 0.012), dark)
    rot.lathe([(0, 0.07, dark), (0.035, 0.06, dark), (0.035, 0.02, dark), (0, 0.02, dark)], 10)
    out["drone_rotor"] = rot.done("drone_rotor", coll)

    # the tractor beam: an open cone 2.5 m long (a sheet, not a solid), v 1 at the drone
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    n = 24
    top = [bm.verts.new(polar(0.11, 360.0 * i / n, -0.21)) for i in range(n)]
    bot = [bm.verts.new(polar(0.62, 360.0 * i / n, -2.71)) for i in range(n)]
    for i in range(n):
        k = (i + 1) % n
        f = bm.faces.new((top[i], top[k], bot[k], bot[i]))
        for loop, co in zip(f.loops, ((i / n, 1.0), ((i + 1) / n, 1.0), ((i + 1) / n, 0.0), (i / n, 0.0))):
            loop[uv].uv = co
        f.smooth = True
    me = bpy.data.meshes.new("drone_beam"); bm.to_mesh(me); bm.free()
    me.materials.append(K.mat("beam"))
    out["drone_beam"] = bpy.data.objects.new("drone_beam", me)
    coll.objects.link(out["drone_beam"])

    # the shaft's rim light at radius 40: a collar on the shaft wall, just under the field
    rim = lathe("stage_shaft_rim", [(39.30, -0.34, "rim"), (39.30, -0.20, "rim"), (39.42, -0.10, "hull"), (40.25, -0.10, "hull"),
                                    (40.25, -0.75, "hull"), (39.75, -0.75, "hull")], circle(192), MATS, coll)
    dress(rim)
    out["stage_shaft_rim"] = rim
    return out


def copy_of(src, name, coll, loc, yaw=0.0):
    ob = bpy.data.objects.new(name, src.data)
    ob.location = loc
    ob.rotation_euler = (0, 0, -math.radians(yaw))
    coll.objects.link(ob)
    return ob


def place_props(lay, P, coll):
    name = lay["name"]
    for i, it in enumerate(lay["jumpPads"]):
        c = polar(it["r"], it["bearing"], it["y"])
        copy_of(P["jump_base"], "%s_jump%d_base" % (name, i), coll, c)
        copy_of(P["jump_cushion"], "%s_jump%d_cushion" % (name, i), coll, c + Vector((0, 0, 0.02)))
        for k, h in enumerate((0.30, 0.72)):
            copy_of(P["jump_ring"], "%s_jump%d_ring%d" % (name, i, k), coll, c + Vector((0, 0, h)))
        for k, h in enumerate((1.05, 1.50)):
            copy_of(P["jump_chevron"], "%s_jump%d_chevron%d" % (name, i, k), coll, c + Vector((0, 0, h)), yaw=180)
    for i, it in enumerate(lay["speedPads"]):
        c = polar(it["r"], it["bearing"], it["y"])
        yaw = it["bearing"] + (90.0 if it["along"] == "tangent" else 0.0)
        copy_of(P["speed_base"], "%s_speed%d_base" % (name, i), coll, c, yaw)
        copy_of(P["speed_chevrons"], "%s_speed%d_chevrons" % (name, i), coll, c, yaw)
    for i, it in enumerate(lay["pickups"]):
        c = polar(it["r"], it["bearing"], it["y"])
        copy_of(P["pickup_base"], "%s_pickup%d_base" % (name, i), coll, c)
        copy_of(P["pickup_cell"], "%s_pickup%d_cell" % (name, i), coll, c + Vector((0, 0, 0.95)), yaw=20)
        copy_of(P["pickup_halo"], "%s_pickup%d_halo" % (name, i), coll, c + Vector((0, 0, 0.95)))


# ---------------------------------------------------------------- the review's stand-ins
def flat(name, rgb, glow=0.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes.get("Principled BSDF")
    b.inputs["Base Color"].default_value = (rgb[0], rgb[1], rgb[2], 1)
    b.inputs["Roughness"].default_value = 0.8
    if glow:
        b.inputs["Emission Color"].default_value = (rgb[0], rgb[1], rgb[2], 1)
        b.inputs["Emission Strength"].default_value = glow
    m.diffuse_color = (rgb[0], rgb[1], rgb[2], 1)
    return m


def figure(name, colour, coll):
    """A 1.8 m figure, feet at the origin."""
    p = Prop()
    body = ("sw", "sw_black")
    p.lathe([(0, 1.8, body), (0.11, 1.76, body), (0.125, 1.66, body), (0.09, 1.55, body), (0.07, 1.50, body), (0.21, 1.44, body),
             (0.23, 1.05, body), (0.19, 0.92, body), (0.17, 0.45, body), (0.13, 0.02, body), (0, 0.0, body)], 12)
    ob = p.done(name, coll, smooth=60)
    ob.data.materials.clear()
    ob.data.materials.append(colour)
    return ob


def preview(coll):
    taya = flat("preview_taya_0080e8", (0.0, 0.216, 0.807), 0.25)
    att = flat("preview_attacker_f87020", (0.939, 0.162, 0.014), 0.25)
    out = {"taya": figure("preview_taya", taya, coll), "attackers": [figure("preview_attacker_%d" % i, att, coll) for i in range(3)]}
    can = Prop().lathe([(0, 0.3, ("sw", "sw_ice")), (0.11, 0.3, ("sw", "sw_ice")), (0.11, 0, ("sw", "sw_ice")), (0, 0, ("sw", "sw_ice"))], 16).done("preview_can", coll)
    out["can"] = can
    turf, wall = flat("preview_turf", (0.012, 0.05, 0.016)), flat("preview_shaft", (0.02, 0.025, 0.045))
    bm = bmesh.new()
    n = 96
    for (ra, za), (rb, zb), mi in (((40.3, -0.02), (85.0, -0.02), 0), ((40.3, -0.02), (40.3, -30.0), 1)):
        a = [bm.verts.new(polar(ra, 360.0 * i / n, za)) for i in range(n)]
        b = [bm.verts.new(polar(rb, 360.0 * i / n, zb)) for i in range(n)]
        for i in range(n):
            bm.faces.new((a[i], a[(i + 1) % n], b[(i + 1) % n], b[i])).material_index = mi
    me = bpy.data.meshes.new("preview_field"); bm.to_mesh(me); bm.free()
    me.materials.append(turf); me.materials.append(wall)
    ob = bpy.data.objects.new("preview_field", me)
    coll.objects.link(ob)
    return out


def floor_height(lay, x, y):
    """The deck's height at Blender (x, y): the same rule as tools/author_arena_layouts.py."""
    r = math.hypot(x, y)
    b = math.degrees(math.atan2(x, y))
    best = None
    for p in lay["pieces"]:
        k, h = p["kind"], None
        if k == "disc" and r <= p["r"]:
            h = p["top"]
        elif k == "ring" and p["r0"] <= r <= p["r1"]:
            h = p["top"]
        elif k == "arc" and p["r0"] <= r <= p["r1"] and (b - p["a0"]) % 360.0 <= p["a1"] - p["a0"]:
            h = p["top"]
        elif k == "ramp":
            a = math.radians(p["bearing"])
            al, sd = x * math.sin(a) + y * math.cos(a), x * math.cos(a) - y * math.sin(a)
            if al > 0 and abs(sd) <= p["width"] / 2 and p["r0"] <= r <= p["r1"]:
                h = p["z0"] + (p["z1"] - p["z0"]) * (r - p["r0"]) / (p["r1"] - p["r0"])
        if h is not None and (best is None or h > best):
            best = h
    return best


# ---------------------------------------------------------------- checks
def report(layout_colls, prop_coll, path):
    lines = []
    for name, coll in layout_colls.items():
        total, bad = 0, 0
        mats = set()
        for ob in coll.objects:
            bm = bmesh.new(); bm.from_mesh(ob.data)
            open_edges = sum(1 for e in bm.edges if len(e.link_faces) != 2)
            tris = sum(len(f.verts) - 2 for f in bm.faces)
            bm.free()
            total += tris; bad += open_edges
            mats.update(m.name for m in ob.data.materials)
            lines.append("MESH %-32s tris %6d%s" % (ob.name, tris, "" if open_edges == 0 else "   <-- OPEN OR NON-MANIFOLD %d" % open_edges))
        ptris = 0
        for ob in coll.children[0].objects:
            ptris += sum(len(p.vertices) - 2 for p in ob.data.polygons)
        lines.append("LAYOUT %-10s pieces %2d  plate triangles %6d  placed props %5d  total %6d  open edges %d" % (
            name, len(coll.objects), total, ptris, total + ptris, bad))
    for ob in prop_coll.objects:
        bm = bmesh.new(); bm.from_mesh(ob.data)
        open_edges = sum(1 for e in bm.edges if len(e.link_faces) != 2)
        tris = sum(len(f.verts) - 2 for f in bm.faces)
        bm.free()
        lines.append("PROP %-32s tris %6d%s" % (ob.name, tris, "" if open_edges == 0 else "   <-- OPEN %d" % open_edges))
    lines.append("MATERIALS " + ", ".join(sorted(m.name for m in bpy.data.materials if m.name.startswith("arena_stage_"))))
    text = "\n".join(lines)
    print(text)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(text + "\n")


def shoot(name, loc, target, lens=35.0, ortho=None):
    scene = bpy.context.scene
    cam_data = bpy.data.cameras.new(name)
    cam = bpy.data.objects.new(name, cam_data)
    scene.collection.objects.link(cam)
    cam.location = loc
    cam.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
    cam_data.lens = lens; cam_data.clip_start = 0.05; cam_data.clip_end = 2000
    if ortho:
        cam_data.type = "ORTHO"; cam_data.ortho_scale = ortho
    scene.camera = cam
    scene.render.filepath = os.path.join(LOGS, name + ".png")
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam)


def main():
    version, render = "v1", True
    for a in sys.argv:
        if a.startswith("--version="):
            version = a.split("=", 1)[1]
        if a == "--no-render":
            render = False
    bpy.ops.wm.read_factory_settings()
    for ob in list(bpy.data.objects):
        bpy.data.objects.remove(ob)
    scene = bpy.context.scene
    materials()
    root = collection("arena_stage")
    prop_coll = bpy.data.collections.new("stage_props"); root.children.link(prop_coll)
    P = props(prop_coll)
    for k, a in enumerate((45, 135, 225, 315)):                   # the four rotors, in place on the drone at the origin
        c = polar(0.55, a, 0.0)
        copy_of(P["drone_rotor"], "drone_rotor_%d" % k, prop_coll, c, yaw=a + 35 * k)
    P["drone_rotor"].hide_render = True; P["drone_rotor"].hide_viewport = True

    colls, prop_colls = {}, {}
    for lay in DATA["layouts"]:
        name = lay["name"]
        c = bpy.data.collections.new("stage_%s" % name); root.children.link(c)
        pc = bpy.data.collections.new("stage_%s_props" % name); c.children.link(pc)
        for p in lay["pieces"]:
            oname = "stage_%s_%s" % (name, p["id"])
            if p["kind"] in ("disc", "ring"):
                ob = build_round(oname, p, c, mark=(p["id"] == "drum"))
            elif p["kind"] == "arc":
                ob = build_arc(oname, p, c)
            else:
                ob = build_ramp(oname, p, c)
            dress(ob, p)
        place_props(lay, P, pc)
        colls[name] = c; prop_colls[name] = pc

    holo = bpy.data.collections.new("stage_hologram_preview"); root.children.link(holo)
    for ob in colls["tore"].objects:
        h = bpy.data.objects.new("holo_" + ob.name, ob.data)
        h.location = (0, 0, 0.06)
        holo.objects.link(h)
        for slot in h.material_slots:
            slot.link = "OBJECT"; slot.material = K.mat("holo")
    pre = bpy.data.collections.new("preview_only"); root.children.link(pre)
    figs = preview(pre)
    drone = [copy_of(P[k], "preview_" + k, pre, (0, 0, 0)) for k in ("drone_body", "drone_beam")]
    drone += [copy_of(P["drone_rotor"], "preview_drone_rotor_%d" % k, pre, polar(0.55, a, 0.0), yaw=a + 35 * k) for k, a in enumerate((45, 135, 225, 315))]
    drone_home = [Vector(o.location) for o in drone]

    os.makedirs(LOGS, exist_ok=True)
    report(colls, prop_coll, os.path.join(LOGS, "stage_%s_report.txt" % version))

    def show(name, holo_on=False, props_on=True, drone_at=None, solid=True):
        for n, c in colls.items():
            for ob in list(c.objects) + list(prop_colls[n].objects):
                on = (n == name) and solid and (ob.name in c.objects or props_on)
                ob.hide_render = not on; ob.hide_viewport = not on
        for ob in holo.objects:
            ob.hide_render = not holo_on; ob.hide_viewport = not holo_on
        for ob in prop_coll.objects:
            ob.hide_render = True
        P["stage_shaft_rim"].hide_render = False
        lay = next(l for l in DATA["layouts"] if l["name"] == name)
        figs["can"].location = (0, 0, lay["canHeight"])
        t = DATA["spawns"]["taya"]
        figs["taya"].location = (t[0], t[2], floor_height(lay, t[0], t[2]))
        for f, m in zip(figs["attackers"], DATA["spawns"]["attackers"]):
            f.location = (m[0], m[2], floor_height(lay, m[0], m[2]))
        for o, home in zip(drone, drone_home):
            o.hide_render = drone_at is None
            if drone_at is not None:
                o.location = home + Vector(drone_at)
        return lay

    key = bpy.data.objects.new("floodlight key", bpy.data.lights.new("floodlight key", "SUN"))
    key.data.energy = 3.6; key.data.color = (0.86, 0.93, 1.0); key.data.angle = math.radians(25)
    key.rotation_euler = (math.radians(38), math.radians(18), 0)
    scene.collection.objects.link(key)
    world = bpy.data.worlds.new("night"); scene.world = world; world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.004, 0.006, 0.022, 1)
    world.node_tree.nodes["Background"].inputs[1].default_value = 1.0
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    eevee = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
    scene.render.engine = eevee
    scene.render.resolution_x, scene.render.resolution_y = 1600, 900
    scene.view_settings.view_transform = "Standard"

    if render:
        pfx = "stage_%s_" % version
        for lay in DATA["layouts"]:
            name = lay["name"]
            show(name)
            ch = lay["canHeight"]
            ah = floor_height(lay, 0.0, 9.0)
            scene.render.resolution_x, scene.render.resolution_y = 1200, 1200
            shoot(pfx + name + "_top", (0, 0, 80), (0, 0.001, 0), ortho=47.0)
            scene.render.resolution_x, scene.render.resolution_y = 1600, 900
            shoot(pfx + name + "_taya", (0.9, -6.6, ch + 2.5), (0, 9, ah + 0.6), lens=20)
            shoot(pfx + name + "_attacker", (1.2, 13.4, ah + 2.5), (0, 0, ch + 0.4), lens=20)
        show("tore", drone_at=(7.5, -12.5, -3.2))
        shoot(pfx + "below", (15, -27, -10), (0, 0, -0.5), lens=24)
        show("hukay")
        shoot(pfx + "below_hukay", (4, -17, -7), (0, 4, 0), lens=18)
        show("plaza")
        e = polar(21.5, 60, 0.0)
        shoot(pfx + "edge", (e.x + 2.6, e.y + 0.4, 1.5), (e.x - 1.2, e.y - 1.4, -0.2), lens=28)
        show("tore")
        shoot(pfx + "ramp_joint", (5.2, 9.6, 3.1), (0.6, 6.4, 0.7), lens=30)
        shoot(pfx + "ramp_side", (7.5, 5.9, 0.9), (0.0, 6.6, 0.6), lens=30)
        show("plaza")
        j = polar(8.0, 120, 0)
        shoot(pfx + "prop_jump", (j.x + 2.2, j.y - 2.6, 1.7), (j.x, j.y, 0.55), lens=35)
        s = polar(15.25, 90, 0)
        shoot(pfx + "prop_speed", (s.x + 3.4, s.y + 3.0, 2.4), (s.x, s.y, 0.0), lens=35)
        k = polar(15.25, 180, 0)
        shoot(pfx + "prop_pickup", (k.x + 1.5, k.y - 1.9, 1.5), (k.x, k.y, 0.6), lens=40)
        show("plaza", drone_at=(3.0, -4.0, 4.3))
        shoot(pfx + "prop_drone", (5.4, -7.2, 3.6), (3.0, -4.0, 3.5), lens=40)
        shoot(pfx + "prop_drone_under", (4.6, -6.6, 1.3), (3.0, -4.0, 4.0), lens=35)
        show("plaza", holo_on=True)
        shoot(pfx + "hologram", (24, -40, 30), (0, 1, 0), lens=30)
        show("plaza", holo_on=True, solid=False)
        shoot(pfx + "hologram_alone", (24, -40, 30), (0, 1, 0), lens=30)
        scene.render.engine = "BLENDER_WORKBENCH"                 # flat grey: how the meshes join
        scene.display.shading.light = "STUDIO"; scene.display.shading.color_type = "SINGLE"
        scene.display.shading.single_color = (0.75, 0.75, 0.75)
        scene.display.shading.show_cavity = True
        show("tore", props_on=True)
        shoot(pfx + "solid_tore", (17, -24, 15), (0, 1, 0), lens=30)
        shoot(pfx + "solid_joint", (5.2, 9.6, 3.1), (0.6, 6.4, 0.7), lens=30)
        shoot(pfx + "solid_under", (10, -15, -6), (0, 0, 0), lens=28)
        show("entablado")
        shoot(pfx + "solid_entablado", (20, -24, 16), (0, 1, 0.5), lens=30)
        scene.render.engine = eevee

    show("plaza")
    for ob in prop_coll.objects:
        ob.hide_render = False
    P["drone_rotor"].hide_render = True
    for n, c in colls.items():                                    # saved showing plaza; the others are one click away
        c.hide_viewport = n != "plaza"; c.hide_render = n != "plaza"
        for ob in list(c.objects) + list(prop_colls[n].objects):
            ob.hide_viewport = False; ob.hide_render = False
    holo.hide_viewport = True; holo.hide_render = True
    for ob in holo.objects:
        ob.hide_viewport = False; ob.hide_render = False
    pre.hide_viewport = True; pre.hide_render = True
    os.makedirs(KITS, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(KITS, "stage.blend"))
    print("STAGE_OK", version)


if __name__ == "__main__":
    main()
