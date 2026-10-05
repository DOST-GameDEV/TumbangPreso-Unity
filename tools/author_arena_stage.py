"""Build the arena STAGE kit: the hover platforms of all five layouts, and the gameplay props.

  py -3 tools/author_arena_layouts.py                 # the layout data (tools/arena_layouts.json)
  py -3 tools/author_arena_textures_stage.py          # the textures
  blender -b --python tools/author_arena_stage.py -- --version=vN [--no-render] [--no-check]

Writes ArtSource/arena/kits/stage.blend and review pictures Logs/arena/stage/stage_<vN>_*.png, and
prints the open-edge and triangle report (also saved as Logs/arena/stage/stage_<vN>_report.txt),
the fighting-face count (stage_<vN>_joints.txt, must be zero) and the walking surface against the
layout data and the collider (stage_<vN>_heights.txt, must stay under 15 mm).
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
        speed_base, speed_chevrons, pickup_base, pickup_cell, pickup_halo, the drone (SAGIP, v6:
        drone_body, drone_fan, drone_antenna, drone_claw_0..2, drone_face_search / _lock / _carry /
        _proud, drone_beam, drone_beam_core, drone_spot; see `drone`), and stage_shaft_rim (in place at radius 40).
  stage_hologram_preview
        the tore layout again wearing the hologram material, lifted 6 cm: how the NEXT layout is
        shown before it turns solid. Review only; the game swaps the material on the real meshes.
  preview_only
        figures at the marks (in the role hues, to judge the deck against them), the can, a slab
        of dark field and the shaft wall. Not part of the kit.

HOW A PLATE IS BUILT. Every piece is ONE closed solid with one cross-section (`section`):
  a pale DECK; a white LINE 7 cm wide, 35 cm in from the edge; a dark band; the LIT RIM, which is
  the last 10 cm of the top and the first 18 cm of the side (so the edge reads from above and
  from the side); a dark side; a skirt cut back 55 cm; a recessed underside of ribs and glowing
  hover cells; and a keel along the middle whose bottom face is the hover emitter. The bands are
  faces of the same surface, told apart by material; nothing is laid on top of anything.
  THE TOP IS FLAT RIGHT OUT TO THE EDGE (v5). The rim used to be a 10 cm chamfer, which put the
  art up to 10 cm under the collider along every edge and left a notch at every joint.
    disc, ring   that section swept round (arena_kit.lathe)
    arc          nested loops of the arc's outline, each inset by the section's distance, so the
                 two ENDS get the same line, rim, skirt and keel as the long edges
    ramp         a grid: rows of one TRUE radius (so both ends are arcs and the height is linear
                 in the true distance from the can, the collider's law), columns of one offset
                 across. The deck is cut into columns no wider than 45 cm.

HOW TWO PIECES JOIN (v5; `joints`, `edge_angles`). A ramp's end and its round neighbour's edge
are THE SAME POLYLINE, corner for corner: the neighbour drops its own regular corners across the
ramp's mouth and takes the ramp's columns instead (theta() gives both the same number, so the
two meshes hold identical coordinates). The two tops are one plane continued, butted along that
line with no overlap, no gap and no step; the neighbour's line and lit rim cross the mouth as
the visible joint. From the line the ramp DIVES at 45 degrees into the neighbour to 12 cm under
its top and runs on 62 cm as a tongue, its underside tapering up, so everything past the joint
is hidden deep inside the neighbour's solid and crosses its faces squarely (no face of one piece
lies along a face of another). `joint_check` measures all of this and must print zero;
`height_check` ray-casts the walking surface against the layout data and against the collider's
own ramp grid (a copy of ArenaStageMesh.Ramp).

MATERIALS (11; textures tools/author_arena_textures_stage.py): arena_stage_deck, _line, _rim,
_hull, _under, _mark, _props, _beam, _beamcore, _spot, _holo.
UVS: deck and holo are a plain top-down projection at 8 m a tile (no stretching, and the hexagons
run on across pieces); line, rim, hull and under are top-down at 4 m on level faces and
(run along the face, height) on upright ones; the mark is 0..1 over 3 m; the props use one atlas.
"""
import bpy, bmesh, json, math, os, sys
import numpy as np
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import arena_kit as K                                             # noqa: E402
from arena_kit import polar, collection, lathe, circle            # noqa: E402

ROOT = K.ROOT
TEX = os.path.join(ROOT, "Assets", "TumbangPreso", "Art", "Arena", "Textures")
KITS = os.path.join(ROOT, "ArtSource", "arena", "kits")
LOGS = os.path.join(ROOT, "Logs", "arena", "stage")
DATA = json.load(open(os.path.join(ROOT, "tools", "arena_layouts.json"), encoding="utf-8"))

SEG = 128                          # a full circle
RIM_W, RIM_SIDE, LINE_IN, LINE_W, SKIRT, LIP, RECESS = 0.10, 0.18, 0.35, 0.07, 0.55, 0.80, 0.12
TONGUE, STEP = 0.62, 0.12          # a ramp runs this far inside its neighbour, this far under its top
COLUMN = 0.45                      # the widest a ramp's deck column may be
ROW = 0.50                         # the longest a sloping ramp's row may be
GUARD = 0.7                        # degrees: no regular corner of a round edge this close to a ramp's mouth
MATS = ["deck", "line", "rim", "hull", "under", "mark"]
GLOW = {"deck": 1.0, "line": 1.0, "rim": 3.2, "under": 2.2, "mark": 1.4, "props": 1.15, "holo": 2.6}

# The props atlas (tools/author_arena_textures_stage.py repeats these): pixels, top-left origin.
REGIONS = {
    "jump_cushion": (0, 0, 256, 256), "jump_base": (256, 0, 512, 256), "speed_top": (512, 0, 768, 512),
    "metal_dark": (768, 0, 1024, 256), "metal_light": (768, 256, 1024, 512), "pickup_cell": (0, 256, 256, 512),
    "drone_top": (256, 256, 512, 512),
    "drone_skin": (0, 640, 512, 832), "drone_buoy": (0, 832, 512, 896), "drone_under": (512, 640, 768, 896),
    "drone_plate": (768, 640, 1024, 704), "sw_crimson": (768, 704, 896, 832), "sw_shell": (896, 704, 1024, 832),
    "sw_brass": (768, 832, 896, 896),
    "drone_face_search": (0, 896, 256, 1024), "drone_face_lock": (256, 896, 512, 1024),
    "drone_face_carry": (512, 896, 768, 1024), "drone_face_proud": (768, 896, 1024, 1024),
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
    material("spot", alpha=True, strength=0.7)
    for key in ("beam", "beamcore"):
        m = material(key, alpha=True, strength=1.6)
        nt = m.node_tree
        b = nt.nodes.get("Principled BSDF")
        tint = nt.nodes.new("ShaderNodeMix"); tint.data_type = "RGBA"; tint.blend_type = "MULTIPLY"; tint.name = "phase tint"
        tint.inputs[0].default_value = 1.0
        tint.inputs[7].default_value = (0.45, 0.95, 1.0, 1.0)
        tex = b.inputs["Base Color"].links[0].from_node
        nt.links.new(tex.outputs["Color"], tint.inputs[6])
        nt.links.new(tint.outputs[2], b.inputs["Emission Color"])
        b.inputs["Base Color"].default_value = (0, 0, 0, 1)


# ---------------------------------------------------------------- the plate's cross-section
def edge(thick):
    """From the deck's boundary out over the edge and back under: (inset, height below the top)
    and the material of each strip between two points."""
    side = min(0.42, thick * 0.5)
    pts = [(LINE_IN + LINE_W, 0.0), (LINE_IN, 0.0), (RIM_W, 0.0), (0.0, 0.0), (0.0, -RIM_SIDE),
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


# ---------------------------------------------------------------- how the pieces join
def theta(bearing, s, radius):
    """The bearing of the point `s` across a ramp's line at true distance `radius`. A ramp and its
    neighbour both ask this, so the two meshes hold the same corner to the last bit."""
    return bearing + math.degrees(math.asin(max(-1.0, min(1.0, s / radius))))


def ramp_section(width, thick):
    """A ramp's cross-section: `section`, with the deck cut into columns."""
    prof = section(-width / 2, width / 2, thick, keel=0.15)
    lo, hi = prof[0][0], prof[1][0]
    n = max(2, int(math.ceil((hi - lo) / COLUMN)))
    return [prof[0]] + [(lo + (hi - lo) * i / n, 0.0, "deck") for i in range(1, n)] + prof[1:]


def joints(lay):
    """Which round edge each ramp end butts. Answers {ramp id: [(neighbour id, edge), (..)]} for
    its near and far end, and the MOUTHS {(neighbour id, "out" or "in"): [(bearing, half angle,
    [the bearing of every corner of the ramp's end, left to right])]}. An end with no round
    neighbour at its radius and height is an error: the kit has no free ramp end."""
    ends, mouths = {}, {}
    for p in lay["pieces"]:
        if p["kind"] != "ramp":
            continue
        cols = sorted(s for s, z, _ in ramp_section(p["width"], p["thick"]) if z == 0.0)
        ends[p["id"]] = []
        for radius, height, edge in ((p["r0"], p["z0"], "out"), (p["r1"], p["z1"], "in")):
            half = math.degrees(math.asin(p["width"] / 2 / radius))
            found = None
            for q in lay["pieces"]:
                if q["kind"] == "ramp" or abs(q["top"] - height) > 1e-6 or (q["kind"] == "disc" and edge == "in"):
                    continue
                at = q["r"] if q["kind"] == "disc" else (q["r1"] if edge == "out" else q["r0"])
                if abs(at - radius) > 1e-6:
                    continue
                if q["kind"] == "arc":
                    rel = (p["bearing"] - q["a0"]) % 360.0
                    if not (half + 2 * GUARD < rel < q["a1"] - q["a0"] - half - 2 * GUARD):
                        continue
                found = q
            if found is None:
                raise ValueError("%s %s: no round piece at radius %.2f, height %.2f for its %s end" % (
                    lay["name"], p["id"], radius, height, "near" if edge == "out" else "far"))
            if found["thick"] < p["thick"] + 0.15:
                raise ValueError("%s %s: %s is too thin to hide its tongue" % (lay["name"], p["id"], found["id"]))
            ends[p["id"]].append((found["id"], edge))
            mouths.setdefault((found["id"], edge), []).append(
                (p["bearing"], half, [theta(p["bearing"], s, radius) for s in cols]))
    return ends, mouths


def _apart(a, b):
    return abs((a - b + 180.0) % 360.0 - 180.0)


def _clear(mouths, what):
    for i, (b1, h1, _) in enumerate(mouths):
        for b2, h2, _ in mouths[i + 1:]:
            if _apart(b1, b2) < h1 + h2 + 2 * GUARD:
                raise ValueError("%s: the ramps at bearings %.0f and %.0f share a stretch of edge" % (what, b1, b2))


def round_angles(mouths, what):
    """The corners of a full circle: the regular ones, but across each ramp's mouth the ramp's."""
    _clear(mouths, what)
    out = [a for a in circle(SEG) if all(_apart(a, b) > h + GUARD for b, h, _ in mouths)]
    for _, _, ts in mouths:
        out += ts
    return sorted(out, key=lambda a: a % 360.0)


def arc_angles(a0, a1, mouths, what):
    """One long edge of an arc: (fraction along it, the bearing to build that corner with)."""
    _clear(mouths, what)
    n = max(3, int(math.ceil((a1 - a0) / (360.0 / SEG))))
    rows = [(0.0, a0), (1.0, a1)]
    for i in range(1, n):
        f = i / n
        if all(abs(f * (a1 - a0) - (b - a0) % 360.0) > h + GUARD for b, h, _ in mouths):
            rows.append((f, a0 + (a1 - a0) * f))
    for b, _, ts in mouths:
        rows += [(((b - a0) % 360.0 + (t - b)) / (a1 - a0), t) for t in ts]
    return sorted(rows)


# ---------------------------------------------------------------- the three builders
def build_round(name, p, coll, mouths, mark=False):
    top, thick = p["top"], p["thick"]
    if p["kind"] == "disc":
        keels = [p["r"] * 0.58] if p["r"] > 7.0 else []
        prof = disc_section(p["r"], thick, mark, keels)
    else:
        prof = section(p["r0"], p["r1"], thick)
    mine = mouths.get((p["id"], "out"), []) + mouths.get((p["id"], "in"), [])
    return lathe(name, [(r, top + z, t) for r, z, t in prof], round_angles(mine, name), MATS, coll)


def build_arc(name, p, coll, mouths):
    r0, r1, a0, a1, top, thick = p["r0"], p["r1"], p["a0"], p["a1"], p["top"], p["thick"]
    edges = (arc_angles(a0, a1, mouths.get((p["id"], "out"), []), name), arc_angles(a0, a1, mouths.get((p["id"], "in"), []), name))
    bm = bmesh.new()
    index = {m: i for i, m in enumerate(MATS)}

    def rows(d, z):
        """The outer and the inner long edge, inset by d. At d = 0 a corner is at its own bearing
        exactly (a ramp's corner must be); further in, the row is squeezed between the two ends."""
        out = []
        for rr, edge in ((r1 - d, edges[0]), (r0 + d, edges[1])):
            dl = math.degrees(math.asin(min(1.0, d / rr))) if d > 0 else 0.0
            out.append([bm.verts.new(polar(rr, a if d == 0 else a0 + dl + (a1 - a0 - 2 * dl) * f, top + z)) for f, a in edge])
        return out

    def fill(r, tag):
        """The strip between the two long edges, which need not have the same corners."""
        (A, B), fa, fb = r, [f for f, _ in edges[0]], [f for f, _ in edges[1]]
        i = j = 0
        while i < len(A) - 1 or j < len(B) - 1:
            if j == len(B) - 1 or (i < len(A) - 1 and fa[i + 1] <= fb[j + 1]):
                f = bm.faces.new((A[i], A[i + 1], B[j])); i += 1
            else:
                f = bm.faces.new((A[i], B[j + 1], B[j])); j += 1
            f.material_index = index[tag]

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
    """Rows of one true radius, columns of one offset across. Past each end: the dive (the top
    12 cm lower, 12 cm inside the neighbour) and the tongue's end, a plain block with no keel."""
    b, r0, r1, z0, z1, thick = p["bearing"], p["r0"], p["r1"], p["z0"], p["z1"], p["thick"]
    prof = ramp_section(p["width"], thick)
    base = -thick + RECESS
    index = {m: i for i, m in enumerate(MATS)}
    n = 1 if abs(z1 - z0) < 1e-9 else max(1, int(math.ceil((r1 - r0) / ROW)))
    stations = [(r0 - TONGUE, 2), (r0 - STEP, 1), (r0, 0)] + [(r0 + (r1 - r0) * i / n, 0) for i in range(1, n)] + \
               [(r1, 0), (r1 + STEP, 1), (r1 + TONGUE, 2)]
    bm = bmesh.new()
    cols = []
    for rho, inside in stations:
        h = z0 + (z1 - z0) * min(1.0, max(0.0, (rho - r0) / (r1 - r0)))
        col = []
        for s, z, _ in prof:
            if inside and z == 0.0:
                z = -STEP
            if inside == 2 and z < base:
                z = base
            col.append(bm.verts.new(polar(rho, theta(b, s, rho), h + z)))
        cols.append(col)
    m = len(prof)
    for i in range(len(cols) - 1):
        for j in range(m):
            k = (j + 1) % m
            bm.faces.new((cols[i][j], cols[i][k], cols[i + 1][k], cols[i + 1][j])).material_index = index[prof[j][2]]
    for col, (rho, _), h in ((cols[0], stations[0], z0), (cols[-1], stations[-1], z1)):      # the two hidden ends: a fan each
        hub = bm.verts.new(polar(rho, b, h - 0.40))
        for j in range(m):
            bm.faces.new((hub, col[j], col[(j + 1) % m])).material_index = index["hull"]
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
           ("skin", region, z0, z1)          u round the axis, v up from z0 to z1
           ("wrap", region, heights)         u round the axis with the seam at the BACK (-y), so the nose is the
                                             region's middle; v by `heights`, ((z, v), ..) top down, read between
           ("torus", region, R, zc)          a ring about z: u round it from the back, v round its tube
           ("arc", region, a0, a1, z0, z1)   a patch facing out between two bearings: u across it as it
                                             is READ from outside (its left at a1), v up from z0 to z1"""

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

    def torus(self, R, r, zc, rule, sides=24, tube=8):
        """A ring about z: major radius R, tube radius r, its middle at height zc."""
        ring = [[self.bm.verts.new(polar(R + r * math.cos(2 * math.pi * k / tube), 360.0 * i / sides, zc + r * math.sin(2 * math.pi * k / tube)))
                 for k in range(tube)] for i in range(sides)]
        for i in range(sides):
            p, q = ring[i], ring[(i + 1) % sides]
            for k in range(tube):
                self.face((p[k], q[k], q[(k + 1) % tube], p[(k + 1) % tube]), rule)
        return self

    def ball(self, centre, r, rule, squash=1.0, sides=10, rows=6):
        o = Vector(centre)
        top, bot = self.bm.verts.new(o + Vector((0, 0, r * squash))), self.bm.verts.new(o - Vector((0, 0, r * squash)))
        cols = [[self.bm.verts.new(o + polar(r * math.sin(math.pi * j / rows), 360.0 * i / sides, r * squash * math.cos(math.pi * j / rows)))
                 for j in range(1, rows)] for i in range(sides)]
        for i in range(sides):
            p, q = cols[i], cols[(i + 1) % sides]
            self.face((top, p[0], q[0]), rule)
            for j in range(rows - 2):
                self.face((p[j], p[j + 1], q[j + 1], q[j]), rule)
            self.face((p[-1], bot, q[-1]), rule)
        return self

    def shell(self, grid, back, rule_front, rule_rest):
        """A closed slab from a grid of points (rows of columns) and the same grid pushed back."""
        A = [[self.bm.verts.new(v) for v in row] for row in grid]
        B = [[self.bm.verts.new(v) for v in row] for row in back]
        rows, cols = len(grid), len(grid[0])
        for j in range(rows - 1):
            for i in range(cols - 1):
                self.face((A[j][i], A[j][i + 1], A[j + 1][i + 1], A[j + 1][i]), rule_front)
                self.face((B[j][i], B[j + 1][i], B[j + 1][i + 1], B[j][i + 1]), rule_rest)
        for i in range(cols - 1):
            self.face((A[0][i], B[0][i], B[0][i + 1], A[0][i + 1]), rule_rest)
            self.face((A[-1][i], A[-1][i + 1], B[-1][i + 1], B[-1][i]), rule_rest)
        for j in range(rows - 1):
            self.face((A[j][0], A[j + 1][0], B[j + 1][0], B[j][0]), rule_rest)
            self.face((A[j][-1], B[j][-1], B[j + 1][-1], A[j + 1][-1]), rule_rest)
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
                elif kind in ("wrap", "torus"):
                    c = f.calc_center_median()
                    ac = (math.atan2(-c.x, -c.y) / (2 * math.pi)) % 1.0                # the face's own way round, from the back
                    a = ac if math.hypot(co.x, co.y) < 1e-6 else (math.atan2(-co.x, -co.y) / (2 * math.pi)) % 1.0
                    a += 1.0 if a - ac < -0.5 else -1.0 if a - ac > 0.5 else 0.0       # a corner on the seam goes with its face
                    if kind == "wrap":
                        fy = rule[2][-1][1]
                        for (z0, v0), (z1, v1) in zip(rule[2], rule[2][1:]):
                            if z1 <= co.z <= z0:
                                fy = v0 + (v1 - v0) * (z0 - co.z) / max(1e-9, z0 - z1)
                                break
                    else:
                        t = (math.atan2(co.z - rule[3], math.hypot(co.x, co.y) - rule[2]) / (2 * math.pi)) % 1.0
                        tc = (math.atan2(c.z - rule[3], math.hypot(c.x, c.y) - rule[2]) / (2 * math.pi)) % 1.0
                        t += 1.0 if t - tc < -0.5 else -1.0 if t - tc > 0.5 else 0.0
                        fy = t * 2 - 1
                    loop[uv].uv = region_uv(rule[1], 1 - a * 2, fy)
                elif kind == "arc":
                    b = math.degrees(math.atan2(co.x, co.y))
                    loop[uv].uv = region_uv(rule[1], (rule[2] + rule[3] - 2 * b) / (rule[3] - rule[2]), (co.z - rule[4]) / (rule[5] - rule[4]) * 2 - 1)
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

    out.update(drone(coll))

    # the shaft's rim light at radius 40: a collar on the shaft wall, just under the field
    rim = lathe("stage_shaft_rim", [(39.30, -0.34, "rim"), (39.30, -0.20, "rim"), (39.42, -0.10, "hull"), (40.25, -0.10, "hull"),
                                    (40.25, -0.75, "hull"), (39.75, -0.75, "hull")], circle(192), MATS, coll)
    dress(rim)
    out["stage_shaft_rim"] = rim
    return out


# ---------------------------------------------------------------- the rescue drone
DRONE_BEAM = 2.5                   # the beam's two cones are this long at scale 1 (ArenaDrone.ModelBeam)
DRONE_BELLY = -0.33                # where they hang from: the lens under the belly
DRONE_CLAWS = (60.0, 180.0, 300.0)  # the bearings of the three prongs, hinged at radius 0.27 under the belly
DRONE_FACES = ("search", "lock", "carry", "proud")
# The body's outline, top to bottom: (radius, height). +y is its nose, the origin its middle.
DRONE_BODY = [(0.0, 0.40), (0.16, 0.39), (0.29, 0.335), (0.385, 0.23), (0.435, 0.08), (0.44, -0.08), (0.41, -0.22), (0.33, -0.32),
              (0.30, -0.36), (0.20, -0.36), (0.17, -0.31), (0.0, -0.31)]


def drone_radius(z):
    for (r0, z0), (r1, z1) in zip(DRONE_BODY, DRONE_BODY[1:]):
        if z1 <= z <= z0 and z0 > z1:
            return r0 + (r1 - r0) * (z0 - z) / (z0 - z1)
    return 0.0


def drone(coll):
    """SAGIP ("rescue"), the catch drone (v6; owner, 2026-10-05: "drone design and ufo effect needs
    to be more stylized". Before this it was a grey quadcopter: four ducts on a puck, no front, no
    face, a grey smudge at game distance). A CHARACTER: a chubby rescue bot wearing a lifebuoy
    (salbabida) that is also its saucer brim, a ceiling fan on a stalk for its rotor, a jeepney
    nameplate on its brow, a screen face that changes with what it is doing, a crane-game claw
    round the lens under its belly, and a beacon on a bent antenna. 1.38 m across the buoy, 1.6 m
    across the fan, 1.16 m from lens to fan cap.

    THE MOVING PARTS ARE THEIR OWN OBJECTS, each with its pivot where it turns:
      drone_fan           spins about the drone's own axis (its origin is the drone's)
      drone_claw_0..2     one mesh three times, each hinged at its origin under the belly, hanging
                          shut; the game opens a prong by turning it about the tangent there
      drone_antenna       pivots at its foot on the back of the head (the game makes it whip)
      drone_face_*        four screens, the same slab with a different drawing; the game shows one
      drone_beam, _core   two open cones hung at the lens (DRONE_BELLY), DRONE_BEAM long; the game
                          scales their length and scrolls their textures (they are sheets of light,
                          the kit's only open meshes, with the mark below)
      drone_spot          the landing mark, a painted sheet 2 m across: the game takes it off the
                          drone, lays it on the deck where the body will stand, and turns it"""
    out = {}
    dark, shell, brass, crimson, gold = ("box", "metal_dark"), ("sw", "sw_shell"), ("sw", "sw_brass"), ("sw", "sw_crimson"), ("sw", "sw_gold")
    # the skin's v runs down the outline by its own LENGTH, so the dome's texels are even
    run = [0.0]
    for (r0, z0), (r1, z1) in zip(DRONE_BODY[:6], DRONE_BODY[1:7]):
        run.append(run[-1] + math.hypot(r1 - r0, z1 - z0))
    skin = ("wrap", "drone_skin", tuple((z, 1.0 - 2.0 * t / run[-1]) for (_, z), t in zip(DRONE_BODY[:7], run)))
    under = ("top", "drone_under", 0.46, 0.46)
    d = Prop()
    d.lathe([(r, z, skin if i < 6 else under) for i, (r, z) in enumerate(DRONE_BODY)], 24)
    d.torus(0.54, 0.15, -0.17, ("torus", "drone_buoy", 0.54, -0.17), tube=10)                 # the lifebuoy, sunk 5 cm into the body
    for sx in (-1, 1):
        d.ball((sx * 0.40, 0.0, 0.17), 0.095, gold, squash=1.0, sides=10, rows=6)             # the ear lamps
    # the brow: the nameplate, a block bent round the head from inside the shell to 8 cm proud
    a0, a1, n = -36.0, 36.0, 8
    plate = ("arc", "drone_plate", a0, a1, 0.215, 0.345)
    rows = ((0.215, 0.475), (0.345, 0.455))
    d.shell([[polar(r, a0 + (a1 - a0) * i / n, z) for i in range(n + 1)] for z, r in rows],
            [[polar(0.24, a0 + (a1 - a0) * i / n, z) for i in range(n + 1)] for z, r in rows], plate, crimson)
    d.lathe([(0, 0.66, dark), (0.04, 0.66, dark), (0.04, 0.37, dark), (0, 0.37, dark)], 8)       # the fan's stalk, from inside the head to inside the hub
    out["drone_body"] = d.done("drone_body", coll, smooth=40)

    # the fan: a hub and three broad paddles, painted from above AND below as rings
    top = ("top", "drone_top", 0.84, 0.84)
    fan = Prop()
    fan.lathe([(0, 0.80, top), (0.075, 0.79, top), (0.135, 0.745, brass), (0.135, 0.67, brass), (0.07, 0.62, dark), (0, 0.62, dark)], 12)
    blade = [(-0.09, 0.10), (0.09, 0.10), (0.17, 0.58), (0.16, 0.71), (0.09, 0.79), (-0.09, 0.79), (-0.16, 0.71), (-0.17, 0.58)]
    for k in range(3):                                                                        # each pitched a little, as a fan's are
        c, sn = math.cos(math.radians(120 * k + 20)), math.sin(math.radians(120 * k + 20))
        lo = [fan.bm.verts.new((x * c + y * sn, -x * sn + y * c, 0.690 + x * 0.30 * min(1.0, (y - 0.10) / 0.2))) for x, y in blade]
        hi = [fan.bm.verts.new((x * c + y * sn, -x * sn + y * c, 0.730 + x * 0.30 * min(1.0, (y - 0.10) / 0.2))) for x, y in blade]
        for i in range(8):
            fan.face((lo[i], lo[(i + 1) % 8], hi[(i + 1) % 8], hi[i]), shell)
        fan.face(hi, top); fan.face(lo[::-1], top)
    out["drone_fan"] = fan.done("drone_fan", coll)

    # one prong of the claw, hinged at its origin: +y is outward, it hangs down and hooks back in
    hook = [(-0.04, 0.03), (0.07, 0.04), (0.165, -0.04), (0.19, -0.13), (0.15, -0.225), (0.045, -0.265),
            (0.035, -0.195), (0.085, -0.17), (0.10, -0.12), (0.08, -0.07), (0.02, -0.04), (-0.04, -0.04)]
    cl = Prop()
    L = [cl.bm.verts.new((-0.06, y, z)) for y, z in hook]
    R = [cl.bm.verts.new((0.06, y, z)) for y, z in hook]
    for i in range(12):
        k = (i + 1) % 12
        cl.face((L[i], L[k], R[k], R[i]), crimson if i in (4, 5, 6) else brass)               # a crimson tip
    for i in range(5):
        cl.face((L[i], L[11 - i], L[10 - i], L[i + 1]), brass)
        cl.face((R[i], R[i + 1], R[10 - i], R[11 - i]), brass)
    out["drone_claw"] = cl.done("drone_claw", coll, smooth=20)

    # the antenna: a bent rod from inside the head, a beacon on its end. Its origin is its foot.
    an = Prop()
    tip = Vector((-0.05, -0.15, 0.20))
    ring0 = [an.bm.verts.new(polar(0.022, 60 * i, -0.06)) for i in range(6)]
    ring1 = [an.bm.verts.new(tip + polar(0.018, 60 * i, 0.0)) for i in range(6)]
    for i in range(6):
        an.face((ring0[i], ring0[(i + 1) % 6], ring1[(i + 1) % 6], ring1[i]), dark)
    an.face(ring0[::-1], dark); an.face(ring1, dark)
    an.ball(tuple(tip + Vector((0, 0, 0.045))), 0.062, crimson, sides=10, rows=6)
    out["drone_antenna"] = an.done("drone_antenna", coll, smooth=40)

    # the four faces: one slab over the screen, 1.2 cm proud of the shell and 6 cm deep into it
    fa0, fa1, fz0, fz1, fn = -34.0, 34.0, -0.05, 0.19, 8
    zs = [fz0 + (fz1 - fz0) * j / 3 for j in range(4)]
    for name in DRONE_FACES:
        fp = Prop()
        fp.shell([[polar(drone_radius(z) + 0.012, fa0 + (fa1 - fa0) * i / fn, z) for i in range(fn + 1)] for z in zs],
                 [[polar(drone_radius(z) - 0.06, fa0 + (fa1 - fa0) * i / fn, z) for i in range(fn + 1)] for z in zs],
                 ("arc", "drone_face_" + name, fa0, fa1, fz0, fz1), ("sw", "sw_black"))
        out["drone_face_" + name] = fp.done("drone_face_" + name, coll, smooth=40)

    # the tractor beam: two open cones, their tops at their origins (which hang at the lens).
    # v runs 0 at the far end to 1 at the drone; u goes six (four) times round.
    def cone(name, key, r0, r1, sides, around, teeth):
        bm = bmesh.new()
        uv = bm.loops.layers.uv.new("UVMap")
        cut = [(0.26 * abs(((i * teeth / sides) % 1.0) * 2 - 1) if teeth else 0.0) for i in range(sides + 1)]   # a zigzag hem
        for i in range(sides):
            k = (i + 1) % sides
            f0, f1 = cut[i] / DRONE_BEAM, cut[i + 1] / DRONE_BEAM
            vs = [bm.verts.new(polar(r0, 360.0 * i / sides, 0.0)), bm.verts.new(polar(r0, 360.0 * k / sides, 0.0)),
                  bm.verts.new(polar(r1 + (r0 - r1) * f1, 360.0 * k / sides, -DRONE_BEAM + cut[i + 1])),
                  bm.verts.new(polar(r1 + (r0 - r1) * f0, 360.0 * i / sides, -DRONE_BEAM + cut[i]))]
            f = bm.faces.new(vs)
            for loop, co in zip(f.loops, ((around * i / sides, 1.0), (around * (i + 1) / sides, 1.0), (around * (i + 1) / sides, f1), (around * i / sides, f0))):
                loop[uv].uv = co
            f.smooth = True
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
        me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
        me.materials.append(K.mat(key))
        ob = bpy.data.objects.new(name, me)
        ob.location = (0, 0, DRONE_BELLY)
        coll.objects.link(ob)
        return ob
    # the landing mark: a flat sheet 2 m across at the origin, which the game lays on the deck
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    f = bm.faces.new([bm.verts.new((x, y, 0.0)) for x, y in ((-1, -1), (1, -1), (1, 1), (-1, 1))])
    for loop in f.loops:
        loop[uv].uv = ((loop.vert.co.x + 1) / 2, (loop.vert.co.y + 1) / 2)
    me = bpy.data.meshes.new("drone_spot"); bm.to_mesh(me); bm.free()
    me.materials.append(K.mat("spot"))
    out["drone_spot"] = bpy.data.objects.new("drone_spot", me)
    coll.objects.link(out["drone_spot"])
    out["drone_beam"] = cone("drone_beam", "beam", 0.24, 1.05, 48, 6, 12)
    out["drone_beam_core"] = cone("drone_beam_core", "beamcore", 0.13, 0.50, 24, 4, 0)
    return out


def drone_set(P, coll, prefix="", at=(0, 0, 0), face="search", beam=True, claws=0.0, lean=None):
    """The drone's parts as the game finds them, in place round `at`: the kit's own (prefix "") or a
    copy for a review picture, with one face shown, the prongs opened by `claws` degrees and the
    whole thing turned by `lean` (a Blender euler) about its middle."""
    from mathutils import Euler, Matrix
    at = Vector(at)
    turn = Euler(lean or (0, 0, 0)).to_matrix().to_4x4()
    made = {}

    def put(key, name, local=Matrix.Identity(4)):
        ob = P[key] if prefix == "" and key != "drone_claw" else bpy.data.objects.new(prefix + name, P[key].data)
        if ob.name not in coll.objects:
            coll.objects.link(ob)
        ob.matrix_world = Matrix.Translation(at) @ turn @ local
        made[name] = ob
        return ob
    put("drone_body", "drone_body"); put("drone_fan", "drone_fan")
    put("drone_antenna", "drone_antenna", Matrix.Translation(polar(0.22, 205.0, 0.35)))
    for k, a in enumerate(DRONE_CLAWS):
        hinge = Matrix.Translation(polar(0.27, a, -0.35)) @ Matrix.Rotation(-math.radians(a), 4, "Z") @ Matrix.Rotation(math.radians(claws), 4, "X")
        put("drone_claw", "drone_claw_%d" % k, hinge)
    for name in DRONE_FACES:
        ob = put("drone_face_" + name, "drone_face_" + name)
        ob.hide_render = prefix != "" and name != face
    for key in ("drone_beam", "drone_beam_core"):
        ob = put(key, key, Matrix.Translation((0, 0, DRONE_BELLY)))
        ob.hide_render = prefix != "" and not beam
    if prefix == "":
        made["drone_spot"] = P["drone_spot"]                      # at the origin: the game takes it off the drone
    return made



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


# ---------------------------------------------------------------- the joints, measured
def _soup(ob):
    """An object's triangles as an (n, 3, 3) array, and the polygon each belongs to."""
    me = ob.data
    me.calc_loop_triangles()
    n = len(me.loop_triangles)
    co = np.empty(len(me.vertices) * 3, dtype=np.float32); me.vertices.foreach_get("co", co)
    idx = np.empty(n * 3, dtype=np.int32); me.loop_triangles.foreach_get("vertices", idx)
    poly = np.empty(n, dtype=np.int32); me.loop_triangles.foreach_get("polygon_index", poly)
    return co.astype(np.float64).reshape(-1, 3)[idx].reshape(n, 3, 3), poly


def _shared(ta, tb, n):
    """The area two triangles share when both are laid in the plane whose normal is n, and the
    middle of that area."""
    ax = (1.0, 0.0, 0.0) if abs(n[0]) < 0.9 else (0.0, 1.0, 0.0)
    e1 = (n[1] * ax[2] - n[2] * ax[1], n[2] * ax[0] - n[0] * ax[2], n[0] * ax[1] - n[1] * ax[0])
    ln = math.sqrt(e1[0] ** 2 + e1[1] ** 2 + e1[2] ** 2)
    e1 = (e1[0] / ln, e1[1] / ln, e1[2] / ln)
    e2 = (n[1] * e1[2] - n[2] * e1[1], n[2] * e1[0] - n[0] * e1[2], n[0] * e1[1] - n[1] * e1[0])

    def flat(t):
        q = [(p[0] * e1[0] + p[1] * e1[1] + p[2] * e1[2], p[0] * e2[0] + p[1] * e2[1] + p[2] * e2[2]) for p in t]
        turn = (q[1][0] - q[0][0]) * (q[2][1] - q[0][1]) - (q[2][0] - q[0][0]) * (q[1][1] - q[0][1])
        return q if turn > 0 else q[::-1]

    A, poly = flat(ta), flat(tb)
    for i in range(3):
        x0, y0 = A[i]
        x1, y1 = A[(i + 1) % 3]
        nxt = []
        for k in range(len(poly)):
            p, q = poly[k], poly[(k + 1) % len(poly)]
            dp = (x1 - x0) * (p[1] - y0) - (y1 - y0) * (p[0] - x0)
            dq = (x1 - x0) * (q[1] - y0) - (y1 - y0) * (q[0] - x0)
            if dp >= 0:
                nxt.append(p)
            if (dp > 0 > dq) or (dp < 0 < dq):
                t = dp / (dp - dq)
                nxt.append((p[0] + t * (q[0] - p[0]), p[1] + t * (q[1] - p[1])))
        poly = nxt
        if len(poly) < 3:
            return 0.0, None
    area = 0.5 * abs(sum(poly[k][0] * poly[(k + 1) % len(poly)][1] - poly[(k + 1) % len(poly)][0] * poly[k][1] for k in range(len(poly))))
    cx, cy = sum(p[0] for p in poly) / len(poly), sum(p[1] for p in poly) / len(poly)
    d = ta[0][0] * n[0] + ta[0][1] * n[1] + ta[0][2] * n[2]
    return area, (e1[0] * cx + e2[0] * cy + n[0] * d, e1[1] * cx + e2[1] * cy + n[1] * d, e1[2] * cx + e2[2] * cy + n[2] * d)


def _faults(pieces, tol=0.03, reach=0.02):
    """Every way two faces can fight, for one layout's pieces [(name, triangles, polygons)].
      SAME PLANE  two faces looking the same way (normals within 1 degree), their planes within
                  `tol`, sharing area. Pairs inside one piece are counted too.
      LYING ON    a face of one piece within `reach` of a face of another that it lies along
                  (normals within 15 degrees, either way round), measured square to that face:
                  'near' looks the same way, 'skim' is buried just under the other's surface,
                  'slot' faces it across a slit, 'touch' lies exactly on it.
    A face that CROSSES another squarely is not a fault: that is how a tongue enters a solid."""
    from mathutils.bvhtree import BVHTree
    P = np.concatenate([p for _, p, _ in pieces])
    owner = np.concatenate([np.full(len(p), i) for i, (_, p, _) in enumerate(pieces)])
    polygon = np.concatenate([g for _, _, g in pieces])
    N = np.cross(P[:, 1] - P[:, 0], P[:, 2] - P[:, 0])
    L = np.linalg.norm(N, axis=1)
    live = L > 1e-10
    N[live] /= L[live][:, None]
    area = L / 2
    lo, hi = P.min(axis=1) - tol, P.max(axis=1) + tol
    cells = {}
    clo, chi = np.floor(lo).astype(int), np.floor(hi).astype(int)
    for i in np.nonzero(live)[0]:
        for x in range(clo[i, 0], chi[i, 0] + 1):
            for y in range(clo[i, 1], chi[i, 1] + 1):
                for z in range(clo[i, 2], chi[i, 2] + 1):
                    cells.setdefault((x, y, z), []).append(i)
    cos1, cos15 = math.cos(math.radians(1.0)), math.cos(math.radians(15.0))
    found = {}

    def note(a, b, kind, face, amount, gap, at):
        g = found.setdefault((pieces[a][0], pieces[b][0], kind), {"faces": set(), "area": 0.0, "gap": 0.0, "z": [1e9, -1e9], "r": [1e9, -1e9], "at": at})
        g["faces"].add(face); g["area"] += amount; g["gap"] = max(g["gap"], gap)
        g["z"] = [min(g["z"][0], at[2]), max(g["z"][1], at[2])]
        r = math.hypot(at[0], at[1])
        g["r"] = [min(g["r"][0], r), max(g["r"][1], r)]

    seen = set()
    for ids in cells.values():
        if len(ids) < 2:
            continue
        I = np.array(ids)
        ok = (N[I] @ N[I].T > cos1)
        for ax in range(3):
            ok &= (lo[I, ax][:, None] <= hi[I, ax][None, :]) & (lo[I, ax][None, :] <= hi[I, ax][:, None])
        for a, b in np.argwhere(np.triu(ok, 1)):
            i, j = int(I[a]), int(I[b])
            if (i, j) in seen or (owner[i] == owner[j] and polygon[i] == polygon[j]):
                continue
            seen.add((i, j))
            side = (P[j] - P[i, 0]) @ N[i]
            if side.min() > tol or side.max() < -tol:
                continue
            shared, at = _shared(P[i].tolist(), P[j].tolist(), N[i].tolist())
            if shared <= 1e-6:
                continue
            gap = abs(float((np.array(at) - P[j, 0]) @ N[j]))
            if gap > tol:
                continue
            note(int(owner[i]), int(owner[j]), "same plane", (int(owner[i]), int(polygon[i]), int(owner[j]), int(polygon[j])), shared, gap, at)

    starts = np.cumsum([0] + [len(p) for _, p, _ in pieces])
    trees = [BVHTree.FromPolygons([Vector(v) for v in p.reshape(-1, 3)], [(3 * k, 3 * k + 1, 3 * k + 2) for k in range(len(p))]) for _, p, _ in pieces]
    weights = ((1 / 3, 1 / 3, 1 / 3), (0.7, 0.15, 0.15), (0.15, 0.7, 0.15), (0.15, 0.15, 0.7))
    for a in range(len(pieces)):
        for b in range(len(pieces)):
            if a == b:
                continue
            blo, bhi = pieces[b][1].reshape(-1, 3).min(axis=0) - reach, pieces[b][1].reshape(-1, 3).max(axis=0) + reach
            for i in range(starts[a], starts[a + 1]):
                if not live[i] or (hi[i] < blo).any() or (lo[i] > bhi).any():
                    continue
                n = Vector(N[i])
                for w in weights:
                    p = Vector(P[i, 0] * w[0] + P[i, 1] * w[1] + P[i, 2] * w[2])
                    co, nb, _, dist = trees[b].find_nearest(p, reach)
                    if co is None:
                        continue
                    v = p - co
                    up = v.dot(nb)
                    if (v - nb * up).length > 1e-4 or abs(n.dot(nb)) < cos15:
                        continue
                    kind = "near" if n.dot(nb) > 0 else ("slot" if up > 1e-5 else ("skim" if up < -1e-5 else "touch"))
                    note(a, b, kind, (a, int(polygon[i])), area[i] / len(weights), dist, tuple(p))
    return found


def _corners(lay, coll):
    """Corners of a ramp's end with no twin on its neighbour's edge, and the other way round."""
    ends, mouths = joints(lay)
    piece = {p["id"]: p for p in lay["pieces"]}
    bad = 0

    def top_corners(ob, radius, height, bearing, half):
        co = np.empty(len(ob.data.vertices) * 3, dtype=np.float32); ob.data.vertices.foreach_get("co", co)
        co = co.reshape(-1, 3)
        keep = (np.abs(np.hypot(co[:, 0], co[:, 1]) - radius) < 2e-5) & (np.abs(co[:, 2] - height) < 2e-5)
        away = np.abs((np.degrees(np.arctan2(co[:, 0], co[:, 1])) - bearing + 180.0) % 360.0 - 180.0)
        return {tuple(v) for v in co[keep & (away < half + 1e-3)].tolist()}

    for rid, pair in ends.items():
        p = piece[rid]
        for (nid, _), radius, height in zip(pair, (p["r0"], p["r1"]), (p["z0"], p["z1"])):
            half = math.degrees(math.asin(p["width"] / 2 / radius))
            a = top_corners(coll.objects["stage_%s_%s" % (lay["name"], rid)], radius, height, p["bearing"], half)
            b = top_corners(coll.objects["stage_%s_%s" % (lay["name"], nid)], radius, height, p["bearing"], half)
            bad += len(a ^ b)
    return bad


def joint_check(layout_colls, path=None):
    """Print every fighting pair of faces in every layout. The count must be zero."""
    lines, total = [], 0
    kinds = ("same plane", "near", "skim", "slot", "touch")
    for lay in DATA["layouts"]:
        coll = layout_colls[lay["name"]]
        pieces = [(ob.name.replace("stage_%s_" % lay["name"], ""),) + _soup(ob) for ob in coll.objects if ob.type == "MESH"]
        found = _faults(pieces)
        stray = _corners(lay, coll)
        faces = sum(len(g["faces"]) for g in found.values())
        total += faces + stray
        lines.append("JOINTS %-10s fighting faces %4d   same plane %d, near %d, skim %d, slot %d, touch %d   ramp-end corners without a twin %d" % (
            (lay["name"], faces) + tuple(sum(len(g["faces"]) for k, g in found.items() if k[2] == kind) for kind in kinds) + (stray,)))
        for (a, b, kind), g in sorted(found.items()):
            lines.append("   %-10s %-7s | %-7s faces %4d  area %8.1f cm2  apart up to %4.1f mm  z %6.2f..%6.2f  r %5.2f..%5.2f  e.g. (%.2f, %.2f, %.2f)" % (
                (kind, a, b, len(g["faces"]), g["area"] * 1e4, g["gap"] * 1e3, g["z"][0], g["z"][1], g["r"][0], g["r"][1]) + tuple(g["at"])))
    lines.append("JOINT FAULTS TOTAL %d" % total)
    text = "\n".join(lines)
    print(text)
    if path:
        with open(path, "w", encoding="utf-8", newline="\n") as f:
            f.write(text + "\n")
    return total


# ---------------------------------------------------------------- the walking surface, measured
def floor_np(lay, X, Y, ramps=True):
    """`floor_height` for arrays: the height the layout data gives (nan off the stage) and the
    index of the piece that gives it."""
    R, B = np.hypot(X, Y), np.degrees(np.arctan2(X, Y))
    best, who = np.full(X.shape, np.nan), np.full(X.shape, -1)
    for i, p in enumerate(lay["pieces"]):
        k = p["kind"]
        if k == "ramp":
            if not ramps:
                continue
            a = math.radians(p["bearing"])
            al, sd = X * math.sin(a) + Y * math.cos(a), X * math.cos(a) - Y * math.sin(a)
            m = (al > 0) & (np.abs(sd) <= p["width"] / 2) & (R >= p["r0"]) & (R <= p["r1"])
            h = p["z0"] + (p["z1"] - p["z0"]) * (R - p["r0"]) / (p["r1"] - p["r0"])
        else:
            m = (R <= p["r"]) if k == "disc" else (R >= p["r0"]) & (R <= p["r1"])
            if k == "arc":
                m &= ((B - p["a0"]) % 360.0) <= p["a1"] - p["a0"]
            h = np.full(X.shape, float(p["top"]))
        take = m & (np.isnan(best) | (h > best))
        best[take] = h[take]; who[take] = i
    return best, who


def collider_ramp(p):
    """The top of a ramp's COLLIDER as ArenaStageMesh.Ramp builds it (8 columns, 6 rows of equal
    true distance, one more row 4 cm past each end at that end's height, each cell split a-b-c,
    a-c-d), in Blender's frame."""
    across_n, along_n, seam = 8, 6, 0.04
    a = math.radians(p["bearing"])
    along, across = Vector((math.sin(a), math.cos(a), 0)), Vector((math.cos(a), -math.sin(a), 0))
    near, far = max(0.0, p["r0"]), max(p["r1"], p["r0"] + 0.05)
    half = max(p["width"], 0.2) * 0.5
    grid = []
    for k in range(across_n + 1):
        x = -half + 2.0 * half * k / across_n
        col = []
        for j in range(along_n + 3):
            t = min(1.0, max(0.0, (j - 1) / along_n))
            dist = near + (far - near) * t
            if j == 0:
                dist = max(0.0, near - seam)
            if j == along_n + 2:
                dist = far + seam
            reach = math.sqrt(max(0.0, dist * dist - x * x))
            col.append(along * reach + across * x + Vector((0, 0, p["z0"] + (p["z1"] - p["z0"]) * t)))
        grid.append(col)
    tris = []
    for k in range(across_n):
        for j in range(along_n + 2):
            q = (grid[k][j], grid[k][j + 1], grid[k + 1][j + 1], grid[k + 1][j])
            tris += [(q[0], q[1], q[2]), (q[0], q[2], q[3])]
    return tris


def height_check(layout_colls, path=None, step=0.04, inset=0.015):
    """Ray-cast the art's walking surface on a `step` grid over every layout (and every centimetre
    along both sides of every joint) and compare it with the height the layout data gives and with
    the collider's top. Points within `inset` of the stage's outer edge are left out: there the
    art's chords and the collider's chords both sit a few millimetres inside the true arc."""
    from mathutils.bvhtree import BVHTree
    lines, worst_all = [], 0.0
    down = Vector((0, 0, -1))
    for lay in DATA["layouts"]:
        coll = layout_colls[lay["name"]]
        verts, tris = [], []
        for ob in coll.objects:
            if ob.type != "MESH":
                continue
            P, _ = _soup(ob)
            base = len(verts)
            verts += [Vector(v) for v in P.reshape(-1, 3)]
            tris += [(base + 3 * k, base + 3 * k + 1, base + 3 * k + 2) for k in range(len(P))]
        art = BVHTree.FromPolygons(verts, tris)
        cverts, ctris = [], []
        for p in lay["pieces"]:
            if p["kind"] == "ramp":
                for t in collider_ramp(p):
                    ctris.append((len(cverts), len(cverts) + 1, len(cverts) + 2)); cverts += list(t)
        col = BVHTree.FromPolygons(cverts, ctris) if ctris else None

        g = np.arange(-22.5, 22.5, step) + step * 0.37
        X, Y = [a.ravel() for a in np.meshgrid(g, g)]
        xs, ys = [X], [Y]
        for p in lay["pieces"]:                                   # the joints, a centimetre apart
            if p["kind"] != "ramp":
                continue
            a = math.radians(p["bearing"])
            s = np.arange(-p["width"] / 2 + 0.005, p["width"] / 2, 0.01)
            for radius in (p["r0"], p["r1"]):
                for d in (-0.05, -0.03, -0.01, -0.003, 0.003, 0.01, 0.03, 0.05):
                    y = np.sqrt((radius + d) ** 2 - s * s)
                    xs.append(y * math.sin(a) + s * math.cos(a)); ys.append(y * math.cos(a) - s * math.sin(a))
        X, Y = np.concatenate(xs), np.concatenate(ys)
        H, who = floor_np(lay, X, Y)
        ok = ~np.isnan(H)
        for k in range(8):
            hh, _ = floor_np(lay, X + inset * math.cos(k * math.pi / 4), Y + inset * math.sin(k * math.pi / 4))
            ok &= ~np.isnan(hh)
        Hr, _ = floor_np(lay, X, Y, ramps=False)
        stats = {i: {"n": 0, "lo": 0.0, "hi": 0.0, "holes": 0, "col": 0.0, "at": None, "cat": None} for i in range(len(lay["pieces"]))}
        for i in np.nonzero(ok)[0]:
            x, y = float(X[i]), float(Y[i])
            st = stats[int(who[i])]
            st["n"] += 1
            hit = art.ray_cast(Vector((x, y, 30.0)), down)
            if hit[0] is None:
                st["holes"] += 1
                continue
            d = hit[0].z - float(H[i])
            if d < st["lo"] or d > st["hi"]:
                if abs(d) > max(-st["lo"], st["hi"]):
                    st["at"] = (x, y, hit[0].z)
                st["lo"], st["hi"] = min(st["lo"], d), max(st["hi"], d)
            hc = None if np.isnan(Hr[i]) else float(Hr[i])
            if col is not None:
                chit = col.ray_cast(Vector((x, y, 30.0)), down)
                if chit[0] is not None and (hc is None or chit[0].z > hc):
                    hc = chit[0].z
            if hc is not None and abs(hit[0].z - hc) > st["col"]:
                st["col"], st["cat"] = abs(hit[0].z - hc), (x, y, hit[0].z)
        worst = max(max(-s["lo"], s["hi"]) for s in stats.values())
        worst_col = max(s["col"] for s in stats.values())
        holes = sum(s["holes"] for s in stats.values())
        worst_all = max(worst_all, worst, worst_col, 9.99 if holes else 0.0)
        lines.append("HEIGHT %-10s samples %7d   art against the data: worst %5.1f mm   art against the collider: worst %5.1f mm   rays that found no art: %d" % (
            lay["name"], sum(s["n"] for s in stats.values()), worst * 1e3, worst_col * 1e3, holes))
        for i, p in enumerate(lay["pieces"]):
            s = stats[i]
            lines.append("   %-6s %-4s samples %7d  art - data %+6.1f..%+5.1f mm%s  |art - collider| %5.1f mm%s  no art %d" % (
                p["id"], p["kind"], s["n"], s["lo"] * 1e3, s["hi"] * 1e3,
                "" if s["at"] is None or max(-s["lo"], s["hi"]) < 0.002 else " at (%.2f, %.2f, %.2f)" % s["at"],
                s["col"] * 1e3, "" if s["cat"] is None or s["col"] < 0.002 else " at (%.2f, %.2f, %.2f)" % s["cat"], s["holes"]))
    lines.append("HEIGHT WORST %.1f mm (the limit is 15.0)" % (worst_all * 1e3))
    text = "\n".join(lines)
    print(text)
    if path:
        with open(path, "w", encoding="utf-8", newline="\n") as f:
            f.write(text + "\n")
    return worst_all


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


EYE = 1.7                          # a player's eye over the deck
JOINT_SHOTS = (                    # layout, name, eye, target, lens: every joint kind at a glancing angle, 4 to 20 m away
    ("plaza", "plaza_drum", polar(5.0, 12, EYE), polar(12.5, 0, 0), 35),
    ("plaza", "plaza_walk", polar(15.25, -28, EYE), polar(12.5, 0, 0), 35),
    ("plaza", "plaza_link", polar(15.25, 22, EYE), polar(17.75, 45, 0), 35),
    ("plaza", "plaza_across", polar(20.0, 45, EYE), polar(0, 0, 0), 35),
    ("plaza", "plaza_far", polar(2.0, 180, EYE), polar(12.5, 0, 0), 50),
    ("hukay", "hukay_up", polar(3.0, 190, -1.2 + EYE), polar(6.6, 0, -0.6), 35),
    ("hukay", "hukay_down", polar(11.5, 12, EYE), polar(5.2, 0, -1.2), 35),
    ("hukay", "hukay_side", polar(10.5, 45, EYE), polar(6.6, 0, -0.6), 35),
    ("hukay", "hukay_link", polar(10.5, 62, EYE), polar(14.25, 45, 0.5), 35),
    ("tore", "tore_ramp", polar(10.5, 25, EYE), polar(6.3, 0, 0.75), 35),
    ("entablado", "entablado_ramp", polar(2.0, 200, EYE), polar(5.7, 50, 0.6), 35),
)


def main():
    version, render, check = "v1", True, True
    for a in sys.argv:
        if a.startswith("--version="):
            version = a.split("=", 1)[1]
        if a == "--no-render":
            render = False
        if a == "--no-check":
            check = False
    bpy.ops.wm.read_factory_settings()
    for ob in list(bpy.data.objects):
        bpy.data.objects.remove(ob)
    scene = bpy.context.scene
    materials()
    root = collection("arena_stage")
    prop_coll = bpy.data.collections.new("stage_props"); root.children.link(prop_coll)
    P = props(prop_coll)
    drone_set(P, prop_coll)                                       # the drone's parts, in place on the drone at the origin
    P["drone_claw"].hide_render = True; P["drone_claw"].hide_viewport = True

    colls, prop_colls = {}, {}
    for lay in DATA["layouts"]:
        name = lay["name"]
        c = bpy.data.collections.new("stage_%s" % name); root.children.link(c)
        pc = bpy.data.collections.new("stage_%s_props" % name); c.children.link(pc)
        _, mouths = joints(lay)
        for p in lay["pieces"]:
            oname = "stage_%s_%s" % (name, p["id"])
            if p["kind"] in ("disc", "ring"):
                ob = build_round(oname, p, c, mouths, mark=(p["id"] == "drum"))
            elif p["kind"] == "arc":
                ob = build_arc(oname, p, c, mouths)
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
    drone = list(drone_set(P, pre, "preview_", face="carry", claws=38.0).values())
    drone_home = [o.matrix_world.copy() for o in drone]
    drone_shown = [not o.hide_render for o in drone]

    os.makedirs(LOGS, exist_ok=True)
    report(colls, prop_coll, os.path.join(LOGS, "stage_%s_report.txt" % version))
    if check:
        faults = joint_check(colls, os.path.join(LOGS, "stage_%s_joints.txt" % version))
        worst = height_check(colls, os.path.join(LOGS, "stage_%s_heights.txt" % version))
        print("STAGE_CHECKS joint faults %d, worst height difference %.1f mm" % (faults, worst * 1e3))

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
        for o, home, shown in zip(drone, drone_home, drone_shown):
            o.hide_render = drone_at is None or not shown
            if drone_at is not None:
                o.matrix_world = Matrix.Translation(drone_at) @ home
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
        for lay_name, tag, e, t, lens in JOINT_SHOTS:             # the joints from a player's eye
            show(lay_name)
            shoot(pfx + "joint_" + tag, e, t, lens=lens)
        show("plaza")
        j = polar(8.0, 120, 0)
        shoot(pfx + "prop_jump", (j.x + 2.2, j.y - 2.6, 1.7), (j.x, j.y, 0.55), lens=35)
        s = polar(15.25, 90, 0)
        shoot(pfx + "prop_speed", (s.x + 3.4, s.y + 3.0, 2.4), (s.x, s.y, 0.0), lens=35)
        k = polar(15.25, 180, 0)
        shoot(pfx + "prop_pickup", (k.x + 1.5, k.y - 1.9, 1.5), (k.x, k.y, 0.6), lens=40)
        show("plaza", drone_at=(3.0, -4.0, 4.3))
        shoot(pfx + "prop_drone", (0.6, 1.2, 4.9), (3.0, -4.0, 3.4), lens=40)      # from its front: +y is its nose
        shoot(pfx + "prop_drone_under", (2.2, -2.2, 1.5), (3.0, -4.0, 4.0), lens=35)
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
        for lay_name, tag, e, t, lens in JOINT_SHOTS:
            if tag in ("plaza_drum", "plaza_link", "hukay_up", "hukay_down"):
                show(lay_name, props_on=False)
                shoot(pfx + "solid_joint_" + tag, e, t, lens=lens)
        scene.render.engine = eevee

    show("plaza")
    for ob in prop_coll.objects:
        ob.hide_render = False
    P["drone_claw"].hide_render = True
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
