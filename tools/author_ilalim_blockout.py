"""Block out the Ilalim ng Tulay rebuild in Blender, at game scale, before any detail (ILALIM-1.2).

  blender -b --python tools/author_ilalim_blockout.py -- [--preview N]

Writes ArtSource/ilalim/ilalim_blockout.blend. With --preview it also writes versioned renders
to Logs/ilalim-blender/blockout_<shot>_vN.png: plans, aerials, and views from inside the play
area at the game's eye (1.25 m, 95 degrees).

THE PLACE (docs/ILALIM_REWORK_GUIDE.md section 0). Taft Avenue at the Padre Faura corner,
Ermita, under LRT-1.

THE LAYOUT IS THE REAL ONE (owner, v3 review, 2026-09-29: "you should take a look at street map
to see how the place is actually laid out", then "the models are pretty much accurate but the
positioning, zoning and lack of sidewalks arent"). Everything outside the play area comes from
ArtSource/ilalim/osm_layout.json, which tools/ilalim_osm_layout.js converts from OpenStreetMap
(ODbL): the building footprints and storeys, the streets with their lanes, the lawns, parking,
walls and fences, the mapped trees, the Oblation, the flagpoles, the statues and the bus stops.
Only the road and the east frontage band are squeezed to fit the game's 14 m box; see that
script's header. Every street gets sidewalks with a kerb step.

What that puts around the court:
  * West: PGH's fenced frontage (parking under trees), then the PGH Nurses Home and the OPD.
  * North across Padre Faura: the Supreme Court corner (Centennial Building, Old Supreme
    Court, with the Lady Justice and Moses statues facing Taft).
  * North-west, behind the Supreme Court: Rizal Hall, a quadrangle whose south front faces
    Padre Faura across a lawn, with the Oblation. ⚠️ For the sightline, sightline_override()
    thins the Supreme Court and moves the Rizal Hall compound east into the space that frees.
    This is the owner's markup and the one deliberate departure from the real map.
  * East: the Astral Tower and West East Center podium, with KFC and Vista GL Taft south of
    them; Manila Science High School across Padre Faura.

COORDINATES. Blender is Z-up. Blender X is the game's x (east), and Blender Y is the game's z
(north, toward UN Avenue). All numbers are metres. The export step owns the glTF axis change.

THE GAMEPLAY CONTRACT (guide section 1). These numbers are copied from the live builder
(Editor/MapKit/IlalimNgTulayBuilder.cs) and must not drift:
  * The chalk box is the 14 m carriageway: road top 0.000, kerb |x| 6.65..7.0 with its top at
    0.150 (painted white, the east and west chalk), and pavements |x| 7..11 with their top at 0.212.
  * The walls are at |x| = 11 and |y| = 16.5. Nothing solid stands inside the box.
  * The live piers are 1.4 m square at (+/-4.45, +/-10), the structural pairs are at +/-19, the
    soffit is 8.0, and the deck is 10.5 wide with tracks at x +/-2.35.
  * The spawns are at y = -9 (x -3, 0, 3), the lata is at the origin, and the throwing lines
    are at |y| = 8.
  * The bridge hoop is at (-8.9, -10), a basketball ring 3.05 m up on the west pavement.
  * The overclock pad is a 1.8 m square. It moves to the EAST pavement at (9.0, 5.5), outside
    PC Express, and mirrors its old west spot.
  * The pisonet, the pares cart at (8.8, -5) and the pisonet cord trip hazard stay on the east
    pavement, against the shopfront edge. The potholes stay flat at |x| = 3.4.
  * Every car stays outside |y| = 16.5.
"""
import json
import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource" / "ilalim"
LAYOUT = SOURCE / "osm_layout.json"
PREVIEWS = ROOT / "Logs" / "ilalim-blender"

BOX = 7.0             # Balance.ConfinementRadius
THROW = BOX + 1.0     # Confinement.ThrowingLine()
SPAWN = BOX + 2.0     # Confinement.AttackerSpawnRing()
KERB_IN = 6.65
KERB_TOP = 0.150
PAVE_TOP = 0.212
PAVE_OUT = 11.0       # PavementOuterX, the wall faces
WALL_Y = 16.5         # WallHalfZ
SOFFIT = 8.0          # ViaductSoffit
DECK_TOP = SOFFIT + 1.04
DECK_HALF = 10.5 / 2
TRACK_X = 2.35
PIER_X = 4.45
PIER_HALF = 0.70
LIVE_PIERS = (-10.0, 10.0)
STRUCT_PIERS = (-19.0, 19.0, -44.0, 44.0, -69.0, 69.0, -94.0, 94.0, -119.0, 119.0, -144.0, 144.0)
EYE = 1.25

# The ground grid (outside the Taft corridor): classes, their top heights, and their colours.
EXTENT = 210          # half-size of the detailed ground, metres
CELL = 0.5           # fine enough that diagonal kerbs do not read as stairs
LOT_TOP = 0.24
GROUND = {            # class: (top, colour)
    "road":     (0.000, "road_cross"),
    "sidewalk": (PAVE_TOP, "sidewalk"),
    "drive":    (LOT_TOP, "drive"),
    "parking":  (LOT_TOP, "parking"),
    "lawn":     (0.26, "lawn"),
    "lot":      (LOT_TOP, "lot"),
}
PRIORITY = ["road", "sidewalk", "drive", "parking", "lawn", "lot"]
STREET_KINDS = {"primary", "secondary", "tertiary", "residential", "unclassified", "living_street"}
SIDEWALK = 2.5

COLOURS = {
    "road":        (0.22, 0.22, 0.24),
    "road_cross":  (0.25, 0.25, 0.27),
    "kerb":        (0.95, 0.95, 0.92),
    "pavement":    (0.62, 0.58, 0.52),
    "sidewalk":    (0.86, 0.84, 0.78),
    "drive":       (0.72, 0.70, 0.66),
    "parking":     (0.50, 0.50, 0.50),
    "lot":         (0.64, 0.64, 0.56),
    "chalk":       (1.00, 1.00, 1.00),
    "throw":       (0.95, 0.85, 0.25),
    "bounds":      (0.90, 0.15, 0.12),
    "spawn":       (0.30, 0.55, 0.80),
    "lata":        (0.80, 0.30, 0.20),
    "pothole":     (0.12, 0.12, 0.13),
    "concrete":    (0.60, 0.60, 0.58),
    "soffit":      (0.40, 0.40, 0.40),
    "rail":        (0.30, 0.28, 0.26),
    "train":       (0.95, 0.80, 0.20),
    "train_band":  (0.16, 0.22, 0.36),
    "lawn":        (0.36, 0.58, 0.22),
    "tree":        (0.24, 0.50, 0.14),
    "trunk":       (0.40, 0.27, 0.16),
    "fence":       (0.12, 0.16, 0.14),
    "wall":        (0.78, 0.74, 0.66),
    "heritage":    (0.93, 0.89, 0.78),
    "heritage_w":  (0.97, 0.96, 0.93),
    "red_roof":    (0.62, 0.24, 0.17),
    "rizal":       (0.88, 0.78, 0.58),
    "rizal_type":  (0.45, 0.10, 0.12),
    "court_white": (0.95, 0.95, 0.93),
    "hospital":    (0.90, 0.85, 0.74),
    "shop":        (0.80, 0.66, 0.56),
    "shop_b":      (0.70, 0.74, 0.66),
    "shop_c":      (0.86, 0.78, 0.58),
    "pcx":         (0.62, 0.64, 0.70),
    "pad":         (0.55, 0.30, 0.65),
    "pisonet":     (0.40, 0.70, 0.70),
    "pares":       (0.70, 0.45, 0.25),
    "cord":        (0.95, 0.85, 0.15),
    "awning":      (0.55, 0.62, 0.40),
    "midrise":     (0.74, 0.72, 0.68),
    "church":      (0.86, 0.84, 0.80),
    "school":      (0.70, 0.78, 0.84),
    "tower":       (0.94, 0.90, 0.82),
    "tower_band":  (0.86, 0.58, 0.50),
    "skyline":     (0.62, 0.68, 0.74),
    "pole":        (0.28, 0.27, 0.25),
    "wire":        (0.08, 0.08, 0.08),
    "jeepney":     (0.85, 0.30, 0.22),
    "vehicle":     (0.90, 0.90, 0.88),
    "bus":         (0.30, 0.55, 0.45),
    "umbrella":    (0.90, 0.50, 0.30),
    "railing":     (0.95, 0.80, 0.20),
    "hoop":        (0.55, 0.25, 0.60),
    "signal":      (0.15, 0.15, 0.15),
    "shelter":     (0.55, 0.62, 0.62),
}


def mat(name):
    m = bpy.data.materials.get(name)
    if m:
        return m
    m = bpy.data.materials.new(name)
    r, g, b = COLOURS[name]
    m.diffuse_color = (r, g, b, 1)
    if m.node_tree is None:
        m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (r, g, b, 1)
    bsdf.inputs["Roughness"].default_value = 0.85
    if name == "bounds":
        bsdf.inputs["Alpha"].default_value = 0.06
        m.diffuse_color = (r, g, b, 0.06)
        if hasattr(m, "surface_render_method"):
            m.surface_render_method = "BLENDED"
    if name == "pad":
        bsdf.inputs["Emission Color"].default_value = (r, g, b, 1)
        bsdf.inputs["Emission Strength"].default_value = 1.5
    return m


def collection(name, parent):
    c = bpy.data.collections.new(name)
    parent.children.link(c)
    return c


def _obj(col, name, mesh, colour, smooth=False):
    mesh.materials.append(mat(colour))
    if smooth:
        for p in mesh.polygons:
            p.use_smooth = True
    o = bpy.data.objects.new(name, mesh)
    col.objects.link(o)
    return o


def box(col, name, center, size, colour, rot_z=0.0):
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Diagonal((*size, 1)))
    bm.to_mesh(mesh)
    bm.free()
    o = _obj(col, name, mesh, colour)
    o.location = center
    o.rotation_euler = (0, 0, rot_z)
    return o


def slab(col, name, x0, x1, y0, y1, top, thick, colour):
    """A box given by its plan extents and its TOP height, which is how the contract is written."""
    return box(col, name, ((x0 + x1) / 2, (y0 + y1) / 2, top - thick / 2), (x1 - x0, y1 - y0, thick), colour)


def cylinder(col, name, center, radius, depth, colour, sides=16):
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=sides, radius1=radius, radius2=radius, depth=depth)
    bm.to_mesh(mesh)
    bm.free()
    o = _obj(col, name, mesh, colour, smooth=sides > 10)
    o.location = center
    return o


def cone(col, name, center, radius, depth, colour, sides=12):
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=sides, radius1=radius, radius2=0.05, depth=depth)
    bm.to_mesh(mesh)
    bm.free()
    o = _obj(col, name, mesh, colour)
    o.location = center
    return o


def blob(col, name, center, radius, colour):
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=2, radius=radius)
    bm.to_mesh(mesh)
    bm.free()
    o = _obj(col, name, mesh, colour, smooth=True)
    o.location = center
    return o


def text(col, body, at, size, colour, rot=(math.pi / 2, 0, 0), extrude=0.04, render=True):
    curve = bpy.data.curves.new(body, "FONT")
    curve.body = body
    curve.size = size
    curve.align_x = "CENTER"
    curve.align_y = "CENTER"
    curve.extrude = extrude
    o = bpy.data.objects.new("text " + body, curve)
    o.data.materials.append(mat(colour))
    o.location = at
    o.rotation_euler = rot
    o.hide_render = not render
    col.objects.link(o)
    return o


def label(col, body, at, size=1.4):
    """A viewport label so the plan explains itself. Hidden in renders."""
    return text(col, body, at, size, "chalk", rot=(0, 0, 0), extrude=0.0, render=False)


# ------------------------------------------------------------------ the sightline override

# THE ONE DELIBERATE DEPARTURE FROM THE REAL MAP (owner, v5 review, 2026-09-29: "we still wanna
# focus on sightlines, so even though the model is now accurate, we need RH to be more visible",
# with a markup moving Rizal Hall east and striking out what blocks it):
#   * the Supreme Court is THINNED, not removed (owner: "i only asked you to thin it down to
#     make more room to move RH"). Every Supreme Court building on the corner is cut at
#     SC_CUT_X and keeps its Taft side, together with the Moses and Lady Justice statues;
#   * the whole Rizal Hall compound moves RIZAL_SHIFT metres east into the freed space: the
#     hall, its courtyard, its front lawn, fence, compound wall, the Oblation and flagpole, and
#     the Gat Andres Bonifacio block behind it. That leaves a 6 m gap to the thinned court;
#   * the small PGH block beside Taft south of Padre Faura, which sits in the court's view line,
#     is removed.
# osm_layout.json stays the true map; only this blockout departs from it.
RIZAL_SRC = (-142.0, -79.0, 38.0, 147.0)      # x0, x1, y0, y1 in game metres
RIZAL_SHIFT = 26.0
SC_CUT_X = -50.0
REMOVE_NAMES = ()
REMOVE_NEAR = [(-31.0, 14.3)]                  # the PGH block in the view line


def clip_east(poly, cut):
    """Sutherland-Hodgman clip of a polygon to the half-plane x >= cut."""
    out = []
    for k in range(len(poly)):
        (ax, ay), (bx, by) = poly[k - 1], poly[k]
        a_in, b_in = ax >= cut, bx >= cut
        if b_in:
            if not a_in:
                out.append((cut, ay + (by - ay) * (cut - ax) / (bx - ax)))
            out.append((bx, by))
        elif a_in:
            out.append((cut, ay + (by - ay) * (cut - ax) / (bx - ax)))
    return out


def _inside(x, y, b):
    return b[0] <= x <= b[1] and b[2] <= y <= b[3]


def _centre(pts):
    return sum(p[0] for p in pts) / len(pts), sum(p[1] for p in pts) / len(pts)


def sightline_override(layout):
    src = RIZAL_SRC
    dst = (src[0] + RIZAL_SHIFT, src[1] + RIZAL_SHIFT, src[2], src[3])
    moved = lambda pts: all(_inside(x, y, src) for x, y in pts)
    shift = lambda pts: [(x + RIZAL_SHIFT, y) for x, y in pts]
    near = lambda x, y: any(math.hypot(x - a, y - b) < 3.0 for a, b in REMOVE_NEAR)

    def keep(pts, name=""):
        cx, cy = _centre(pts)
        return not (any(n in name for n in REMOVE_NAMES) or near(cx, cy) or _inside(cx, cy, dst))

    out = dict(layout)
    # Thin the Supreme Court: any unmoved building on the corner that reaches west of the cut
    # keeps only its part east of it.
    thinned = []
    for b in layout["buildings"]:
        cx, cy = _centre(b["poly"])
        if not moved(b["poly"]) and cx > SC_CUT_X and src[2] <= cy <= src[3] and min(x for x, _ in b["poly"]) < SC_CUT_X:
            poly = clip_east(b["poly"], SC_CUT_X)
            if len(poly) >= 3:
                thinned.append(dict(b, poly=poly))
            continue
        thinned.append(b)
    out["buildings"] = [dict(b, poly=shift(b["poly"])) if moved(b["poly"]) else b
                        for b in thinned if moved(b["poly"]) or keep(b["poly"], b["name"])]
    out["areas"] = [dict(a, poly=shift(a["poly"])) if moved(a["poly"]) else a
                    for a in layout["areas"] if moved(a["poly"]) or keep(a["poly"], a["name"])]
    out["roads"] = [dict(r, line=shift(r["line"])) if moved(r["line"]) and "Padre Faura" not in r["name"] else r
                    for r in layout["roads"]
                    if r["kind"] in STREET_KINDS or moved(r["line"]) or not all(_inside(x, y, dst) for x, y in r["line"])]
    out["trees"] = [((x + RIZAL_SHIFT, y) if _inside(x, y, src) else (x, y)) for x, y in layout["trees"]
                    if _inside(x, y, src) or keep([(x, y)])]
    out["points"] = [dict(p, at=shift([p["at"]])[0]) if _inside(*p["at"], src) else p
                     for p in layout["points"] if _inside(*p["at"], src) or keep([p["at"]], p["name"])]
    # Barriers: a wholly moved line moves; any other line loses the segments on the new corner.
    barriers = []
    for b in layout["barriers"]:
        if moved(b["line"]):
            barriers.append(dict(b, line=shift(b["line"])))
            continue
        run = []
        for p, q in zip(b["line"], b["line"][1:]):
            if _inside(*p, dst) and _inside(*q, dst):
                if len(run) > 1:
                    barriers.append(dict(b, line=run))
                run = []
                continue
            run = run or [p]
            run.append(q)
        if len(run) > 1:
            barriers.append(dict(b, line=run))
    out["barriers"] = barriers
    return out


def point_in_poly(x, y, poly):
    inside = False
    j = len(poly) - 1
    for i in range(len(poly)):
        xi, yi = poly[i]
        xj, yj = poly[j]
        if (yi > y) != (yj > y) and x < (xj - xi) * (y - yi) / (yj - yi + 1e-12) + xi:
            inside = not inside
        j = i
    return inside


def in_corridor(x, y):
    """The Taft corridor the contract owns: carriageway, kerbs and pavements, |x| <= 11."""
    return abs(x) <= PAVE_OUT


# ------------------------------------------------------------------ ground

def classify(layout):
    """Rasterise the real streets, sidewalks, driveways, lawns and parking on a 1 m grid.
    Taft itself is left out: the corridor is built exactly by corridor()."""
    n = int(2 * EXTENT / CELL)
    rank = {c: i for i, c in enumerate(PRIORITY)}
    grid = [[rank["lot"]] * n for _ in range(n)]

    def paint(cls, x, y):
        i, j = int((x + EXTENT) / CELL), int((y + EXTENT) / CELL)
        if 0 <= i < n and 0 <= j < n and rank[cls] < grid[j][i]:
            grid[j][i] = rank[cls]

    def ribbon(line, half, cls):
        for (x0, y0), (x1, y1) in zip(line, line[1:]):
            lo_x, hi_x = min(x0, x1) - half, max(x0, x1) + half
            lo_y, hi_y = min(y0, y1) - half, max(y0, y1) + half
            dx, dy = x1 - x0, y1 - y0
            seg = dx * dx + dy * dy or 1e-9
            y = math.floor(lo_y / CELL) * CELL + CELL / 2
            while y <= hi_y:
                x = math.floor(lo_x / CELL) * CELL + CELL / 2
                while x <= hi_x:
                    t = max(0.0, min(1.0, ((x - x0) * dx + (y - y0) * dy) / seg))
                    if math.hypot(x - (x0 + t * dx), y - (y0 + t * dy)) <= half:
                        paint(cls, x, y)
                    x += CELL
                y += CELL

    def fill(poly, cls):
        xs, ys = [p[0] for p in poly], [p[1] for p in poly]
        y = math.floor(min(ys)) + CELL / 2
        while y <= max(ys):
            x = math.floor(min(xs)) + CELL / 2
            while x <= max(xs):
                if point_in_poly(x, y, poly):
                    paint(cls, x, y)
                x += CELL
            y += CELL

    for a in layout["areas"]:
        if a["kind"] in ("village_green", "grass", "park", "garden"):
            fill(a["poly"], "lawn")
        elif a["kind"] == "parking":
            fill(a["poly"], "parking")
    for r in layout["roads"]:
        if "Taft" in r["name"]:
            continue
        if r["kind"] in STREET_KINDS:
            half = max(3.0, r["lanes"] * 3.2) / 2
            ribbon(r["line"], half, "road")
            ribbon(r["line"], half + SIDEWALK, "sidewalk")
        elif r["kind"] == "service":
            ribbon(r["line"], 2.0, "drive")
        elif r["kind"] in ("footway", "pedestrian", "path"):
            ribbon(r["line"], 0.9, "drive")
    return grid, [PRIORITY[i] for i in range(len(PRIORITY))]


def ground(root, layout):
    col = collection("Ground", root)
    grid, names = classify(layout)
    n = len(grid)
    # ONE CLEAN SURFACE PER CLASS (owner, v6: "fix the plane.."). The first version laid one box
    # per row run, hundreds of thousands of them, which drew as a solid black sheet in
    # wireframe. Now each class is a single top surface on shared grid vertices, dissolved into
    # large flat faces, plus kerb walls only where two classes of different height meet. Cells
    # inside the Taft corridor are skipped; corridor() lays them exactly.
    top_of = [GROUND[c][0] for c in names]
    skip = lambda i: abs(-EXTENT + (i + 0.5) * CELL) < PAVE_OUT
    parts = {c: ([], {}, []) for c in GROUND}           # verts, index, faces

    def vert(cls, x, y, z):
        verts, index, _ = parts[cls]
        key = (round(x, 3), round(y, 3), round(z, 3))
        if key not in index:
            index[key] = len(verts)
            verts.append(key)
        return index[key]

    for j in range(n):
        y0, y1 = -EXTENT + j * CELL, -EXTENT + (j + 1) * CELL
        for i in range(n):
            if skip(i):
                continue
            cls = names[grid[j][i]]
            z = top_of[grid[j][i]]
            x0, x1 = -EXTENT + i * CELL, -EXTENT + (i + 1) * CELL
            parts[cls][2].append((vert(cls, x0, y0, z), vert(cls, x1, y0, z), vert(cls, x1, y1, z), vert(cls, x0, y1, z)))
            # Kerb walls on the east and north edges, owned by the higher side.
            for ni, nj, ax, ay, bx, by in ((i + 1, j, x1, y0, x1, y1), (i, j + 1, x0, y1, x1, y1)):
                if ni >= n or nj >= n or skip(ni):
                    continue
                z2 = top_of[grid[nj][ni]]
                if abs(z - z2) < 1e-4:
                    continue
                hi, lo = (cls, z2) if z > z2 else (names[grid[nj][ni]], z)
                zt = max(z, z2)
                parts[hi][2].append((vert(hi, ax, ay, lo), vert(hi, bx, by, lo), vert(hi, bx, by, zt), vert(hi, ax, ay, zt)))
    for cls, (verts, _, faces) in parts.items():
        if not faces:
            continue
        mesh = bpy.data.meshes.new("ground " + cls)
        mesh.from_pydata(verts, [], faces)
        bm = bmesh.new()
        bm.from_mesh(mesh)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        bmesh.ops.dissolve_limit(bm, angle_limit=0.0005, verts=bm.verts, edges=bm.edges,
                                 delimit={"NORMAL"})
        bm.to_mesh(mesh)
        bm.free()
        _obj(col, "ground " + cls, mesh, GROUND[cls][1])
    # Beyond the detailed grid: a plain plate under the fog.
    slab(col, "outer plate", -600, 600, -600, 600, -0.3, 0.5, "lot")
    return grid, names


def corridor(root, grid, names):
    """Taft, exactly: the 14 m carriageway, the painted kerbs and the 4 m pavements, row by row,
    with the pavements and kerbs dropped to road level wherever a real cross street meets Taft."""
    col = collection("Taft corridor", root)
    slab(col, "Taft carriageway", -KERB_IN, KERB_IN, -EXTENT, EXTENT, 0.0, 0.4, "road")
    n = len(grid)

    def is_road(x, y):
        i, j = int((x + EXTENT) / CELL), int((y + EXTENT) / CELL)
        return 0 <= i < n and 0 <= j < n and names[grid[j][i]] == "road"

    for s in (-1, 1):
        runs, start, state = [], -EXTENT, None
        y = -EXTENT
        while y <= EXTENT:
            here = is_road(s * (PAVE_OUT + 1.5), y + 0.5) if y < EXTENT else None
            if here != state:
                if state is not None:
                    runs.append((start, y, state))
                start, state = y, here
            y += CELL
        for y0, y1, road in runs:
            xs = sorted((s * KERB_IN, s * BOX))
            xp = sorted((s * BOX, s * PAVE_OUT))
            if road:
                slab(col, "cross-street mouth", *sorted((s * KERB_IN, s * PAVE_OUT)), y0, y1, 0.0, 0.4, "road_cross")
            else:
                # Kerb, painted white on top: the east and west chalk.
                slab(col, "kerb", *xs, y0, y1, KERB_TOP, 0.4, "kerb")
                slab(col, "pavement", *xp, y0, y1, PAVE_TOP, 0.4, "pavement")


# ------------------------------------------------------------------ gameplay markers

def gameplay(root):
    col = collection("Gameplay", root)
    w = 0.12
    for s in (-1, 1):
        slab(col, "chalk N/S", -BOX, BOX, s * BOX - w / 2, s * BOX + w / 2, 0.012, 0.03, "chalk")
        slab(col, "throwing line", -BOX, BOX, s * THROW - w / 2, s * THROW + w / 2, 0.012, 0.03, "throw")
        cylinder(col, "pothole", (s * 3.4, -s * 3.0, 0.004), 0.55, 0.01, "pothole", 12)
        box(col, "wall E/W", (s * (PAVE_OUT + 0.2), 0, 2.0), (0.4, 2 * WALL_Y, 4.0), "bounds")
        box(col, "wall N/S", (0, s * (WALL_Y + 0.2), 2.0), (2 * PAVE_OUT, 0.4, 4.0), "bounds")
    cylinder(col, "lata", (0, 0, 0.16), 0.08, 0.32, "lata", 12)
    for x in (-3.0, 0.0, 3.0):
        cylinder(col, "spawn", (x, -SPAWN, 0.01), 0.35, 0.02, "spawn", 12)
    hx, hy = -8.9, -10.0
    cylinder(col, "hoop post", (hx - 0.9, hy, PAVE_TOP + 1.8), 0.08, 3.6, "pole", 8)
    box(col, "hoop board", (hx - 0.55, hy, PAVE_TOP + 3.3), (0.06, 1.2, 0.8), "heritage_w")
    cylinder(col, "hoop ring", (hx - 0.2, hy, PAVE_TOP + 3.05), 0.25, 0.04, "hoop", 16)
    label(col, "HOOP", (hx, hy - 1.5, 4.5), 0.8)
    slab(col, "overclock pad", 8.1, 9.9, 4.6, 6.4, PAVE_TOP + 0.03, 0.03, "pad")
    label(col, "PAD", (9.0, 5.5, 1.0), 0.8)


def street_business(root):
    """The businesses the contract names, on the Astral Tower and West East Center podium."""
    col = collection("East podium shops", root)
    base = PAVE_TOP
    fronts = [(-16.5, -9.5, "shop_c", "print xerox bind"), (-9.5, -1.5, "shop", "carinderia"),
              (-1.5, 9.0, "pcx", "PC EXPRESS"), (9.0, 15.0, "shop_c", "pisonet"), (15.0, 20.0, "shop_b", "pharmacy")]
    for y0, y1, colour, name in fronts:
        slab(col, "shopfront " + name, PAVE_OUT, PAVE_OUT + 0.6, y0 + 0.1, y1 - 0.1, base + 4.2, 4.2, colour)
        slab(col, "awning", PAVE_OUT - 1.2, PAVE_OUT, y0 + 0.4, y1 - 0.4, base + 3.2, 0.12, "awning")
        label(col, name, (PAVE_OUT + 1, (y0 + y1) / 2, base + 5), 0.7)
    slab(col, "PC Express fascia", PAVE_OUT - 0.25, PAVE_OUT, -1.2, 8.7, base + 5.0, 1.0, "train_band")
    for i in range(3):
        box(col, "pisonet terminal", (9.7, 10.0 + i * 1.75, base + 0.8), (0.7, 0.9, 1.6), "pisonet")
    slab(col, "pisonet cord TRIP", 7.7, 9.1, 10.0, 12.6, base + 0.03, 0.03, "cord")
    box(col, "pares cart", (8.8, -5.0, base + 1.3), (1.72, 2.03, 2.6), "pares")
    box(col, "pares A-board", (8.05, -3.75, base + 0.4), (0.2, 0.9, 0.8), "shop_c")
    # Vendors against the PGH fence on the west pavement, never mid-pavement.
    for y in (12.5, 14.6, -14.2):
        box(col, "vendor stall", (-PAVE_OUT + 0.65, y, base + 0.45), (1.1, 1.6, 0.9), "shop_c")
        cylinder(col, "umbrella pole", (-PAVE_OUT + 0.65, y, base + 1.3), 0.03, 2.6, "pole", 6)
        cone(col, "umbrella", (-PAVE_OUT + 0.65, y, base + 2.55), 1.2, 0.5, "umbrella")
    # The shopfront-edge cables along the east pavement.
    y = -118.0
    while y < 118:
        cylinder(col, "power pole", (10.65, y, base + 4.8), 0.16, 9.6, "pole", 8)
        y += 12
    for k, z in enumerate((8.6, 8.1, 7.6, 7.2)):
        slab(col, "cable run", 10.55 + k * 0.08, 10.6 + k * 0.08, -118, 118, base + z, 0.04, "wire")


# ------------------------------------------------------------------ LRT-1

def guideway(root):
    col = collection("LRT-1", root)
    length = 2 * EXTENT
    slab(col, "deck", -DECK_HALF, DECK_HALF, -length / 2, length / 2, DECK_TOP, DECK_TOP - SOFFIT, "concrete")
    slab(col, "soffit", -DECK_HALF + 0.3, DECK_HALF - 0.3, -length / 2, length / 2, SOFFIT + 0.01, 0.02, "soffit")
    for s in (-1, 1):
        slab(col, "parapet", s * DECK_HALF - 0.25, s * DECK_HALF + 0.25, -length / 2, length / 2, DECK_TOP + 1.1, 1.3, "concrete")
        for rail in (-0.72, 0.72):
            slab(col, "rail", s * TRACK_X + rail - 0.05, s * TRACK_X + rail + 0.05, -length / 2, length / 2, DECK_TOP + 0.18, 0.18, "rail")
    y = -length / 2 + 10
    while y < length / 2:
        cylinder(col, "catenary mast", (0, y, DECK_TOP + 2.6), 0.12, 5.2, "pole", 8)
        box(col, "catenary arm", (0, y, DECK_TOP + 5.0), (6.4, 0.15, 0.15), "pole")
        y += 20
    # Twin-leg piers under one cap (guide 0.4): real LRT-1 piers are single; the game needs two.
    for y in LIVE_PIERS + STRUCT_PIERS:
        for s in (-1, 1):
            box(col, "pier leg" + (" LIVE" if y in LIVE_PIERS else ""), (s * PIER_X, y, (SOFFIT - 0.9) / 2),
                (2 * PIER_HALF, 2 * PIER_HALF, SOFFIT - 0.9), "concrete")
        slab(col, "pier cap", -PIER_X - 1.0, PIER_X + 1.0, y - 0.9, y + 0.9, SOFFIT, 0.9, "concrete")
    ty = 60.0
    for i, (y0, y1) in enumerate(((ty - 7.8, ty - 2.7), (ty - 2.55, ty + 2.55), (ty + 2.7, ty + 7.8))):
        slab(col, f"train car {i}", TRACK_X - 1.3, TRACK_X + 1.3, y0, y1, DECK_TOP + 3.6, 3.3, "train")
        slab(col, f"train band {i}", TRACK_X - 1.32, TRACK_X + 1.32, y0 + 0.1, y1 - 0.1, DECK_TOP + 2.3, 0.7, "train_band")


# ------------------------------------------------------------------ the real buildings

HERITAGE = ("Rizal", "Supreme Court Main", "Old Supreme", "PGH", "Out-Patient", "Nurses", "Bonifacio",
            "Damian", "Museum", "Hospital", "Calderon", "College", "Hall")


def style(b):
    """(storey height, wall colour, red roof?) from what the building is."""
    name = b["name"]
    if "Rizal Hall" in name:
        return 4.4, "rizal", True
    if "Centennial" in name:
        return 3.6, "court_white", False
    if "Supreme" in name:
        return 4.4, "court_white", True
    if "Astral" in name:
        return 3.3, "tower", False
    if "Science High" in name or b["use"] == "school":
        return 3.4, "school", False
    if b["use"] in ("church",) or "Church" in name or "Cathedral" in name:
        return 4.0, "church", False
    if any(k in name for k in HERITAGE) or b["use"] in ("hospital", "university", "dormitory"):
        return 4.2, "hospital", True
    return 3.2, "midrise", False


def extrude(col, name, poly, base, height, colour, roof=None):
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    verts = [bm.verts.new((x, y, base)) for x, y in poly]
    try:
        face = bm.faces.new(verts)
    except ValueError:
        bm.free()
        return None
    bmesh.ops.recalc_face_normals(bm, faces=[face])
    if face.normal.z < 0:
        face.normal_flip()
    ext = bmesh.ops.extrude_face_region(bm, geom=[face])
    top = [g for g in ext["geom"] if isinstance(g, bmesh.types.BMFace)]
    bmesh.ops.translate(bm, vec=(0, 0, height), verts=[v for g in ext["geom"] if isinstance(g, bmesh.types.BMVert) for v in [g]])
    face.normal_flip()
    mesh_mats = [colour]
    if roof:
        # A hipped-looking roof: the top face inset and raised, in red.
        res = bmesh.ops.inset_region(bm, faces=top, thickness=roof[0], depth=roof[1], use_even_offset=True)
        mesh_mats.append("red_roof")
        for f in top + res["faces"]:
            f.material_index = 1
    bm.to_mesh(mesh)
    bm.free()
    o = bpy.data.objects.new(name, mesh)
    for m in mesh_mats:
        mesh.materials.append(mat(m))
    col.objects.link(o)
    return o


def buildings(root, layout):
    col = collection("Buildings (OSM)", root)
    rng = random.Random(5)
    for k, b in enumerate(layout["buildings"]):
        poly = b["poly"]
        cx = sum(p[0] for p in poly) / len(poly)
        cy = sum(p[1] for p in poly) / len(poly)
        if max(abs(cx), abs(cy)) > EXTENT + 40:
            continue
        # Nothing may stand in the contract's corridor.
        if any(abs(x) < PAVE_OUT - 0.05 for x, _ in poly):
            poly = [(math.copysign(max(abs(x), PAVE_OUT), cx), y) for x, y in poly]
        storey, colour, red = style(b)
        levels = b["levels"] or rng.choice((2, 2, 3, 3, 4, 5))
        height = max(3.5, levels * storey)
        roof = (1.6, 2.4) if red else None
        name = b["name"] or f"building {k}"
        o = extrude(col, name, poly, LOT_TOP - 0.05, height, colour, roof)
        if o is None:
            continue
        if b["name"]:
            label(col, b["name"], (cx, cy, height + 4), 2.0)
        if "Astral" in b["name"]:
            # The cream and salmon bands of the real tower.
            xs, ys = [p[0] for p in poly], [p[1] for p in poly]
            for s in range(1, int(levels)):
                slab(col, "Astral band", min(xs) - 0.2, max(xs) + 0.2, min(ys) - 0.2, max(ys) + 0.2,
                     LOT_TOP + s * storey + 0.7, 0.7, "tower_band")


def rizal_hall(root, layout):
    """Rizal Hall's portico on its real south front, facing Padre Faura across the lawn and the
    Oblation. The footprint itself comes from OSM in buildings()."""
    col = collection("Rizal Hall portico", root)
    hall = next(b for b in layout["buildings"] if "Rizal Hall" in b["name"])
    front_y = min(p[1] for p in hall["poly"])
    oblation = next(p["at"] for p in layout["points"] if "Oblation" in p["name"])
    px = oblation[0]
    base = LOT_TOP
    x0, x1 = px - 7.0, px + 7.0
    slab(col, "portico floor", x0, x1, front_y - 3.2, front_y + 0.2, base + 1.2, 1.2, "heritage")
    for i in range(6):
        x = x0 + 0.8 + i * (x1 - x0 - 1.6) / 5
        cylinder(col, "Ionic column", (x, front_y - 2.6, base + 1.2 + 5.8), 0.5, 11.6, "heritage_w", 16)
    slab(col, "entablature", x0 - 0.3, x1 + 0.3, front_y - 3.4, front_y + 0.2, base + 14.2, 1.6, "heritage_w")
    for k in range(3):
        slab(col, "front step", x0 + 1, x1 - 1, front_y - 3.2 - 0.45 * (k + 1), front_y - 3.2 - 0.45 * k,
             base + 1.2 - 0.4 * (k + 1), 0.3, "heritage")
    text(col, "RIZAL HALL", (px, front_y - 3.45, base + 13.4), 1.0, "rizal_type")
    ox, oy = oblation
    box(col, "Oblation plinth", (ox, oy, base + 0.9), (1.6, 1.6, 1.8), "heritage")
    cylinder(col, "Oblation figure", (ox, oy, base + 2.8), 0.28, 2.0, "heritage_w", 10)


def details(root, layout):
    col = collection("Street details (OSM)", root)
    rng = random.Random(9)
    for p in layout["points"]:
        x, y = p["at"]
        if in_corridor(x, y) and abs(y) < WALL_Y + 1:
            continue
        kind = p["kind"]
        if kind == "monument":
            box(col, "statue plinth " + p["name"], (x, y, LOT_TOP + 0.8), (1.4, 1.4, 1.6), "heritage")
            cylinder(col, "statue " + p["name"], (x, y, LOT_TOP + 2.6), 0.3, 2.0, "heritage_w", 10)
            label(col, p["name"], (x, y, 6), 1.0)
        elif kind == "flagpole":
            cylinder(col, "flagpole", (x, y, LOT_TOP + 5), 0.08, 10, "pole", 8)
        elif kind == "traffic_signals":
            cylinder(col, "signal pole", (x, y, 3.0), 0.14, 6.0, "signal", 8)
            box(col, "signal head", (x, y, 5.4), (0.4, 0.4, 1.0), "signal")
        elif kind == "bus_stop":
            slab(col, "bus shelter roof", x - 3, x + 3, y - 1.1, y + 1.1, 2.9, 0.15, "shelter")
            for dx in (-2.8, 2.8):
                cylinder(col, "bus shelter post", (x + dx, y - 0.9, 1.5), 0.05, 2.8, "pole", 6)
        elif kind == "pole":
            cylinder(col, "power pole", (x, y, 4.8), 0.16, 9.6, "pole", 8)
    # Walls and fences, segment by segment, kept off the contract's pavements. Concrete walls
    # are solid; fences are see-through iron, posts and two rails, as on Padre Faura and Taft.
    for b in layout["barriers"]:
        wall = b["kind"] == "wall"
        for (x0, y0), (x1, y1) in zip(b["line"], b["line"][1:]):
            if abs(x0) < PAVE_OUT - 0.3 and abs(x1) < PAVE_OUT - 0.3:
                continue
            L = math.hypot(x1 - x0, y1 - y0)
            if L < 0.05:
                continue
            mid, ang = ((x0 + x1) / 2, (y0 + y1) / 2), math.atan2(y1 - y0, x1 - x0)
            if wall:
                box(col, "wall", (*mid, LOT_TOP + 1.5), (L, 0.18, 3.0), "wall", rot_z=ang)
                continue
            for z in (0.25, 1.35):
                box(col, "fence rail", (*mid, LOT_TOP + z), (L, 0.05, 0.05), "fence", rot_z=ang)
            for k in range(int(L / 2.4) + 1):
                t = min(1.0, k * 2.4 / L)
                box(col, "fence post", (x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, LOT_TOP + 0.72), (0.07, 0.07, 1.44), "fence")
            k = 0.3
            while k < L:
                t = k / L
                box(col, "fence picket", (x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, LOT_TOP + 0.8), (0.025, 0.025, 1.1), "fence")
                k += 0.3
    # Trees: every mapped tree, plus shade trees scattered over the lawns and parking.
    spots = [tuple(t) for t in layout["trees"]]
    for a in layout["areas"]:
        if a["kind"] not in ("village_green", "parking"):
            continue
        xs, ys = [p[0] for p in a["poly"]], [p[1] for p in a["poly"]]
        for _ in range(int((max(xs) - min(xs)) * (max(ys) - min(ys)) / 140)):
            x, y = rng.uniform(min(xs), max(xs)), rng.uniform(min(ys), max(ys))
            if point_in_poly(x, y, a["poly"]):
                spots.append((x, y))
    polys = [b["poly"] for b in layout["buildings"]]
    # Keep the portico readable from the court: no tree within 4 m of the view lines from the
    # spawn and from both pavements to the Rizal Hall portico.
    hall = next(b for b in layout["buildings"] if "Rizal Hall" in b["name"])
    ox = next(p["at"][0] for p in layout["points"] if "Oblation" in p["name"])
    portico = (ox, min(p[1] for p in hall["poly"]) - 2.6)
    eyes = [(0.0, -SPAWN), (-8.0, 16.0), (8.0, 0.0), (-8.0, -10.0)]

    def on_view_line(x, y):
        for ax, ay in eyes:
            bx, by = portico
            dx, dy = bx - ax, by - ay
            t = max(0.0, min(1.0, ((x - ax) * dx + (y - ay) * dy) / (dx * dx + dy * dy)))
            if math.hypot(x - (ax + t * dx), y - (ay + t * dy)) < 4.0:
                return True
        return False

    for x, y in spots:
        if abs(x) < PAVE_OUT + 0.8 or max(abs(x), abs(y)) > EXTENT:
            continue
        if on_view_line(x, y):
            continue
        if any(point_in_poly(x, y, p) for p in polys):
            continue
        h = 5 + rng.random() * 4
        cylinder(col, "tree trunk", (x, y, LOT_TOP + h / 2), 0.25, h, "trunk", 8)
        blob(col, "tree canopy", (x, y, LOT_TOP + h + 1.2), 2.6 + rng.random() * 1.6, "tree")


def traffic(root, layout):
    """Traffic on the real streets, all outside |y| = 16.5 (the contract's car rule)."""
    col = collection("Traffic", root)
    box(col, "jeepney (Taft)", (3.5, 24.0, 1.2), (2.2, 6.5, 2.4), "jeepney")
    box(col, "jeepney (Taft)", (-3.5, -26.0, 1.2), (2.2, 6.5, 2.4), "jeepney")
    box(col, "bus (Taft)", (3.5, 70.0, 1.6), (2.5, 11.0, 3.2), "bus")
    box(col, "UV Express van", (-3.5, -52.0, 1.0), (1.9, 4.8, 2.0), "vehicle")
    faura = [r for r in layout["roads"] if "Padre Faura" in r["name"]]
    rng = random.Random(4)
    placed = 0
    for r in faura:
        for (x0, y0), (x1, y1) in zip(r["line"], r["line"][1:]):
            L = math.hypot(x1 - x0, y1 - y0)
            t = 12.0
            while t < L and placed < 9:
                x, y = x0 + (x1 - x0) * t / L, y0 + (y1 - y0) * t / L
                if abs(x) > PAVE_OUT + 3 and abs(y) > WALL_Y + 2:
                    kind = rng.choice(("vehicle", "vehicle", "jeepney"))
                    size = (6.5, 2.2, 2.4) if kind == "jeepney" else (4.4, 1.9, 1.5)
                    box(col, "car (Padre Faura)", (x, y, size[2] / 2), size, kind, rot_z=math.atan2(y1 - y0, x1 - x0))
                    placed += 1
                t += rng.uniform(18, 34)


def horizon(root):
    col = collection("Horizon", root)
    rng = random.Random(11)
    for i in range(70):
        a = rng.random() * math.tau
        d = 260 + rng.random() * 160
        s = 16 + rng.random() * 18
        h = 30 + rng.random() * (70 if d < 330 else 120)
        box(col, f"skyline {i}", (math.cos(a) * d, math.sin(a) * d, h / 2), (s, s, h), "skyline", rot_z=rng.random())


def lighting():
    scene = bpy.context.scene
    world = bpy.data.worlds.new("sky")
    scene.world = world
    if world.node_tree is None:
        world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (0.78, 0.80, 0.86, 1)
    bg.inputs["Strength"].default_value = 0.55
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy, sun.data.angle = 3.6, math.radians(3)
    sun.data.color = (1.0, 0.86, 0.68)
    direction = Vector((0.80, -0.25, -0.55)).normalized()
    sun.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    scene.collection.objects.link(sun)
    scene.view_settings.view_transform = "AgX"
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type == "VIEW_3D":
                for space in area.spaces:
                    if space.type == "VIEW_3D":
                        space.shading.type = "MATERIAL"
                        space.clip_end = 3000
                        space.clip_start = 0.1


def preview(version, layout):
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 1000
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    cam.data.clip_end = 3000
    scene.collection.objects.link(cam)
    scene.camera = cam
    eye = 18 / math.tan(math.radians(95 / 2))
    e = EYE
    hall = next(b for b in layout["buildings"] if "Rizal Hall" in b["name"])
    ox, oy = next(p["at"] for p in layout["points"] if "Oblation" in p["name"])
    front_y = min(p[1] for p in hall["poly"])
    # The review stand for Rizal Hall: Padre Faura's south sidewalk, straight across from the portico.
    faura_pts = [pt for r in layout["roads"] if "Padre Faura" in r["name"] for pt in r["line"]]
    near = min(faura_pts, key=lambda pt: abs(pt[0] - ox))
    faura_south = near[1] - 3 * 3.2 / 2 - SIDEWALK / 2
    shots = [
        ("plan", "ORTHO", Vector((-40, 20, 400)), Vector((-40, 20, 0)), 300),
        ("plan_court", "ORTHO", Vector((0, 0, 250)), Vector((0, 0, 0)), 42),
        ("aerial_rizal", "PERSP", Vector((60, -70, 70)), Vector((-70, 50, 6)), 24),
        ("aerial_north", "PERSP", Vector((-20, -110, 60)), Vector((-10, 40, 4)), 24),
        ("spawn_north", "PERSP", Vector((0.0, -SPAWN, e)), Vector((0, 30, 4)), eye),
        ("taya_south", "PERSP", Vector((0.0, 4.0, e)), Vector((0, -30, 2)), eye),
        ("west_pavement_east", "PERSP", Vector((-9.5, -4.0, PAVE_TOP + e)), Vector((12, 6, 3)), eye),
        ("court_to_rizal", "PERSP", Vector((-8.0, 16.0, PAVE_TOP + e)), Vector((ox, front_y, 8)), eye),
        ("spawn_to_rizal", "PERSP", Vector((3.0, -SPAWN, e)), Vector((ox, front_y, 8)), eye),
        ("rizal_front", "PERSP", Vector((ox + 6, faura_south, PAVE_TOP + 1.7)), Vector((ox, front_y, 7)), eye),
    ]
    for name, kind, pos, tgt, lens in shots:
        cam.data.type = kind
        if kind == "ORTHO":
            cam.data.ortho_scale = lens
        else:
            cam.data.lens = lens
        cam.location = pos
        cam.rotation_euler = (tgt - pos).to_track_quat("-Z", "Y").to_euler()
        # The court plan is taken without the viaduct, which would otherwise cover the box.
        bpy.data.collections["LRT-1"].hide_render = name == "plan_court"
        scene.render.filepath = str(PREVIEWS / f"blockout_{name}_v{version}.png")
        bpy.ops.render.render(write_still=True)
        print("[ilalim] preview", scene.render.filepath)
    bpy.data.collections["LRT-1"].hide_render = False


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
    layout = sightline_override(json.loads(LAYOUT.read_text(encoding="utf-8")))
    bpy.ops.wm.read_factory_settings(use_empty=True)
    root = bpy.data.collections.new("ilalim_blockout")
    bpy.context.scene.collection.children.link(root)
    grid, names = ground(root, layout)
    corridor(root, grid, names)
    gameplay(root)
    street_business(root)
    guideway(root)
    buildings(root, layout)
    rizal_hall(root, layout)
    details(root, layout)
    traffic(root, layout)
    horizon(root)
    lighting()
    SOURCE.mkdir(parents=True, exist_ok=True)
    out = SOURCE / "ilalim_blockout.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(out), compress=True)
    backup = SOURCE / "ilalim_blockout.blend1"
    if backup.exists():
        backup.unlink()
    print("[ilalim] blockout saved", out)
    if version:
        preview(version, layout)


if __name__ == "__main__":
    main()
