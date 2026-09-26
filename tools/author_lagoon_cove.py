"""Lagoon Court, second blockout: an ORGANIC boulder cove built the way the reference is built.

  blender -b --python tools/author_lagoon_cove.py -- --preview N

Writes ArtSource/lagoon/lagoon_cove.blend and Logs/lagoon-blender/cove_<shot>_vN.png.

Owner on blockout v5: "i want the mountain not to just be one spiky mountain peak", "notice how
the houses are not just on a single level", "experiment more on something organic until it looks
similar to the reference", "the rocks wont be just like that correct? and actually be more
unique?". What the reference (docs/LAGOON_REWORK_GUIDE.md § 1, § 4) actually does, and so what
this builds:

  * THE MASSIF IS A PILE OF BOULDERS. A height field with several peaks, ridges and a saddle
    (not one cone) is the underlying fill, and it is covered in hundreds of placed boulders,
    bigger low down, smaller up high and on the ledges' rims.
  * THE BOULDERS ARE PILLOW STONES, not crystals: a subdivided cube pulled most of the way to a
    sphere, stretched taller than wide, chopped by a few random planes into broad flat faces,
    softly jittered. Twelve unique shapes, shared as linked meshes and each placed at its own
    size, stretch and turn, so no two neighbours read alike.
  * EVERY HOUSE HAS ITS OWN POCKET: a small flat ledge at its own height (beach, low, mid, high),
    cut into the height field, fenced on the drop side, joined to the next by a stair. Nine
    heights, not three shelves.
  * PLANTS GROW IN THE GAPS: palms and bushes where a boulder meets a pocket or the beach.
  * The court is still the biggest clearing (z = 0, 28 x 26), rock round three sides, open south
    down to the beach and the lagoon tongue.
"""
import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector, noise

sys.path.insert(0, str(Path(__file__).resolve().parent))
import author_kanto_blockout as B      # noqa: E402  box, cylinder, blob, label, _obj, lighting
import author_lagoon_blockout as L     # noqa: E402  colours, gameplay markers, horizon, col

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource" / "lagoon"
PREVIEWS = ROOT / "Logs" / "lagoon-blender"
BEACH, WATER, SEABED = -1.2, -1.8, -3.5

B.COLOURS.update({
    "rock": (0.62, 0.46, 0.28), "rock_light": (0.74, 0.60, 0.40), "rock_fill": (0.36, 0.27, 0.17),
    "grass": (0.38, 0.62, 0.18), "sand": (0.95, 0.80, 0.52), "deck": (0.62, 0.38, 0.20),
})

# POCKETS: (name, x, y, floor z, radius x, radius y, surface). Each is a flat ledge the height
# field is cut down (or built up) to. The court is the biggest; houses get the rest.
POCKETS = [
    ("court", 0, 1, 0.0, 19, 17, "court"),
    ("beach landing west", -22, -24, BEACH, 8, 6, "sand"),
    ("beach landing east", 22, -24, BEACH, 8, 6, "sand"),
    ("low west", -34, -8, 2.5, 7, 6, "grass"),
    ("low east", 35, -12, 3.5, 7, 6, "grass"),
    ("mid north-west", -26, 26, 6.5, 8, 6, "grass"),
    ("mid north", 4, 30, 5.0, 9, 5.5, "grass"),
    ("mid east", 36, 18, 8.5, 7, 6, "grass"),
    ("high west", -40, 38, 12.5, 6.5, 5.5, "grass"),
    ("high north", -8, 48, 13.0, 7, 5, "grass"),
    ("high east", 26, 44, 16.5, 6.5, 5, "grass"),
    ("summit ledge (capilla)", 6, 64, 22.0, 7, 5.5, "grass"),
]
# Stairs: pairs of pockets joined by a straight flight, drawn edge to edge.
STAIRS = [("court", "mid north"), ("court", "low west"), ("court", "low east"), ("low west", "mid north-west"),
          ("mid north", "high north"), ("mid north-west", "high west"), ("low east", "mid east"),
          ("mid east", "high east"), ("high north", "summit ledge (capilla)"), ("court", "beach landing west"),
          ("court", "beach landing east")]
# Peaks of the massif: (x, y, height, radius). Several humps and ridges, a saddle between them.
PEAKS = [(-30, 58, 38, 22), (8, 74, 46, 20), (38, 60, 34, 18), (-58, 26, 24, 18), (58, 20, 26, 18),
         (-4, 44, 22, 22), (-48, -30, 12, 14), (48, -36, 11, 14), (22, 86, 30, 16), (-12, 90, 34, 18)]


def in_lagoon(x, y):
    """The lagoon tongue: open south of the court, between the rock arms."""
    half = 22 + max(0.0, -y - 30) * 0.12
    return y < -20 and abs(x) < half


def peak_height(x, y):
    h = SEABED
    for px, py, ph, pr in PEAKS:
        h = max(h, ph * math.exp(-((x - px) ** 2 + (y - py) ** 2) / (pr * pr) * 0.9) + BEACH)
    return h


def massif(x, y):
    h = SEABED
    for px, py, ph, pr in PEAKS:
        d2 = ((x - px) ** 2 + (y - py) ** 2) / (pr * pr)
        h = max(h, ph * math.exp(-d2 * 0.9) + BEACH)
    # A ring of land round the whole cove that falls to the sea at its outer edge.
    r = math.hypot(x, y * 0.9)
    h = max(h, BEACH + 6 * max(0.0, 1 - abs(r - 60) / 26))
    h += 1.6 * noise.noise(Vector((x * 0.06, y * 0.06, 0.3)))
    if r > 95:
        h = min(h, SEABED + (100 - r) * 0.6)
    if in_lagoon(x, y):
        h = SEABED
    elif y < -14 and abs(x) < 42:
        h = min(h, BEACH)   # a broad curved beach round the lagoon's head (review v1: too thin)
    return h


def pocket_at(x, y):
    for p in POCKETS:
        _n, px, py, pz, rx, ry, _s = p
        d = math.hypot((x - px) / rx, (y - py) / ry)
        if d < 1.0:
            return p, d
    return None, None


def height(x, y):
    h = massif(x, y)
    for _n, px, py, pz, rx, ry, _s in POCKETS:
        d = math.hypot((x - px) / rx, (y - py) / ry)
        if d < 1.0:
            return pz
        if d < 1.6:   # a short rim blend so pockets sit INTO the slope
            t = (d - 1.0) / 0.6
            h = pz + (h - pz) * t if h > pz else max(h, pz - 0.2 - (1 - t) * 0.5)
    return h


# ---------------------------------------------------------------- boulders

def boulder_mesh(i):
    """One unique pillow stone. A subdivided cube, 70 per cent of the way to a sphere, stretched,
    chopped by three to five random planes into broad flat faces, then jittered."""
    rng = random.Random(900 + i)
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=2.0)
    bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=2, use_grid_fill=True)
    for v in bm.verts:
        s = v.co.normalized()
        v.co = v.co.lerp(s * 1.25, 0.7)
    sx, sy, sz = rng.uniform(0.8, 1.2), rng.uniform(0.7, 1.0), rng.uniform(1.0, 1.6)
    bmesh.ops.scale(bm, vec=(sx, sy, sz), verts=bm.verts[:])
    for k in range(rng.randint(3, 5)):
        n = Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), rng.uniform(-0.2, 1))).normalized()
        cut = rng.uniform(0.55, 0.85) * max(sx, sy, sz)
        res = bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], plane_co=n * cut, plane_no=n,
                                     clear_outer=True)
        edges = [e for e in res["geom_cut"] if isinstance(e, bmesh.types.BMEdge)]
        if edges:
            bmesh.ops.holes_fill(bm, edges=edges)
    for v in bm.verts:
        v.co += Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), rng.uniform(-1, 1))) * 0.04
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    # SOFT PLANES (review v1: flat shading made every stone a crystal). Faces are smooth; only
    # an edge between two faces more than 38 degrees apart stays sharp, so a stone reads as a
    # few broad planes with soft rounded shoulders, like the reference's.
    for f in bm.faces:
        f.smooth = True
    for e in bm.edges:
        if len(e.link_faces) == 2 and e.link_faces[0].normal.angle(e.link_faces[1].normal, 0) > math.radians(38):
            e.smooth = False
    me = bpy.data.meshes.new(f"boulder_{i:02d}")
    bm.to_mesh(me)
    bm.free()
    me.materials.append(B.mat("rock"))
    return me


def place_boulders(c, rng):
    kit = [boulder_mesh(i) for i in range(12)]
    placed = 0
    step = 4.8
    y = -100.0
    while y < 100:
        x = -100.0
        while x < 100:
            px, py = x + rng.uniform(-1.6, 1.6), y + rng.uniform(-1.6, 1.6)
            x += step
            pocket, d = pocket_at(px, py)
            if pocket is not None:
                continue
            h = height(px, py)
            if h < BEACH + 0.6 or in_lagoon(px, py):
                continue
            # 1: rock belongs to the massif. Where the peaks contribute little (the outer ring),
            # only one spot in four gets a stone, so the ring reads as sand and grass.
            if peak_height(px, py) < BEACH + 4 and rng.random() < 0.75:
                continue
            # 2: every pocket keeps its DOWNHILL front open (toward the lagoon mouth), so the
            # house on it looks out over the ledge instead of hiding behind a row of stones.
            front = None
            for p in POCKETS:
                dx, dy = (px - p[1]) / p[4], (py - p[2]) / p[5]
                dd = math.hypot(dx, dy)
                if 0.9 < dd < 2.1 and p[0] != "court":
                    to_mouth = Vector((0 - p[1], -70 - p[2], 0)).normalized()
                    if Vector((dx, dy, 0)).normalized().dot(to_mouth) > 0.35:
                        front = p[3] if front is None else min(front, p[3])
            # Rims: the boulders just outside a pocket are smaller, so a ledge edge is a row of
            # stones rather than a wall; the massif's body is bigger stones.
            near = min(math.hypot((px - p[1]) / p[4], (py - p[2]) / p[5]) for p in POCKETS)
            s = rng.uniform(2.2, 3.4) if near < 1.9 else rng.uniform(3.2, 6.0) * (1.15 - min(0.4, h / 80))
            z = h - s * rng.uniform(0.35, 0.7)
            if front is not None:
                # THE LEDGE FACE IS STILL STONE (review v2: clearing it showed smooth dirt cliffs).
                # Stones in front of a pocket stay, but their tops stop 0.3 m under the pocket
                # floor, so the house still looks out over them.
                z = min(z, front - 0.3 - s * 1.35)
            o = bpy.data.objects.new("boulder", kit[rng.randrange(len(kit))])
            o.location = (px, py, z)
            o.scale = (s * rng.uniform(0.85, 1.15), s * rng.uniform(0.85, 1.15), s * rng.uniform(0.8, 1.1))
            o.rotation_euler = (rng.uniform(-0.18, 0.18), rng.uniform(-0.18, 0.18), rng.uniform(0, math.tau))
            c.objects.link(o)
            placed += 1
        y += step
    print("[lagoon-cove] boulders placed:", placed)


# ---------------------------------------------------------------- ground

def ground(c):
    """The height field as one grid mesh (the fill that shows between boulders), with the
    pockets flat. Faces take the pocket's surface colour, else rock fill."""
    n, span = 150, 210.0
    bm = bmesh.new()
    verts = [[bm.verts.new((-span / 2 + i * span / n, -span / 2 + j * span / n, 0)) for i in range(n + 1)] for j in range(n + 1)]
    for row in verts:
        for v in row:
            v.co.z = height(v.co.x, v.co.y)
    mats = ["rock_fill", "court", "grass", "sand"]
    me = bpy.data.meshes.new("ground")
    for j in range(n):
        for i in range(n):
            f = bm.faces.new((verts[j][i], verts[j][i + 1], verts[j + 1][i + 1], verts[j + 1][i]))
            f.normal_update()
            cx, cy = f.calc_center_median().x, f.calc_center_median().y
            pocket, _d = pocket_at(cx, cy)
            if pocket is not None:
                f.material_index = mats.index(pocket[6])
            elif f.calc_center_median().z <= BEACH + 0.3:
                f.material_index = 3
            elif f.normal.z < 0.75:
                f.material_index = 0   # steep ground is rock, never grass (review v3: green cliffs)
            elif peak_height(cx, cy) < BEACH + 4:
                f.material_index = 2   # the outer ring: grass, not bare fill (review v2)
            else:
                f.material_index = 0
    bm.to_mesh(me)
    bm.free()
    for m in mats:
        me.materials.append(B.mat(m))
    o = bpy.data.objects.new("ground", me)
    c.objects.link(o)
    B.box(c, "sea", (0, 0, WATER - 0.05), (500, 500, 0.1), "water")
    B.box(c, "lagoon shallows", (0, -60, WATER - 0.02), (60, 90, 0.1), "shallows")


# ---------------------------------------------------------------- village

def house(c, rng, name, x, y, z, face_to):
    """A blockout Filipino stilt house on its pocket: raised floor on short piles, sawali or
    painted walls, a steep thatch or tin roof, a front deck toward `face_to`."""
    w, d, h = rng.uniform(7, 9.5), rng.uniform(5.5, 7), rng.uniform(3.0, 3.6)
    lift = 0.9
    rot = math.atan2(face_to[1] - y, face_to[0] - x) - math.pi / 2
    rm = Matrix.Rotation(rot, 3, "Z")
    at = lambda lx, ly: Vector((x, y, 0)) + rm @ Vector((lx, ly, 0))  # noqa: E731
    wall = rng.choice(("sawali", "sawali", "wall_paint", "deck"))
    B.box(c, f"{name} floor", (x, y, z + lift), (w + 0.4, d + 0.4, 0.2), "deck", rot_z=rot)
    B.box(c, f"{name} walls", (x, y, z + lift + h / 2), (w, d, h), wall, rot_z=rot)
    p = at(0, d / 2 + 1.1)
    B.box(c, f"{name} front deck", (p.x, p.y, z + lift), (w + 0.4, 2.2, 0.2), "deck", rot_z=rot)
    for lx in (-w / 2, w / 2):
        for ly in (-d / 2, d / 2 + 2.1):
            q = at(lx, ly)
            B.cylinder(c, "pile", (q.x, q.y, z + lift / 2 - 0.3), 0.13, lift + 0.6, "bamboo", sides=6)
    roof = "thatch" if rng.random() < 0.72 else rng.choice(("tin", "tin_red"))
    m = bpy.data.meshes.new("roof")
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=4, radius1=1.0, radius2=0.02, depth=1.0,
                          matrix=Matrix.Rotation(math.pi / 4, 4, "Z"))
    bm.to_mesh(m)
    bm.free()
    o = B._obj(c, f"{name} roof", m, roof)
    o.location = (x, y, z + lift + h + 1.25)
    o.scale = ((w + 2.8) * 0.72, (d + 2.8) * 0.72, 3.6)
    o.rotation_euler = (0, 0, rot)


def village(c, rng):
    for name, px, py, pz, rx, ry, _s in POCKETS:
        if name == "court" or name.startswith("summit") or name.startswith("beach"):
            continue
        # Toward the lagoon mouth, a little randomised, so houses look out over the cove.
        house(c, rng, f"house {name}", px, py + ry * 0.15, pz, (px * 0.3, -60))
        # A fence along the pocket's downhill (south) rim.
        for k in range(7):
            a = math.pi + 0.35 + k * (math.pi - 0.7) / 6
            fx, fy = px + math.cos(a) * rx * 0.95, py + math.sin(a) * ry * 0.95
            B.box(c, "fence post", (fx, fy, pz + 0.5), (0.12, 0.12, 1.0), "deck")
    # Stilt houses over the lagoon's edges, at water level, with walkways.
    for i, (x, y) in enumerate([(-19, -38), (-20, -54), (-21, -70), (19, -40), (20, -56), (21, -72)]):
        house(c, rng, f"stilt house {i}", x, y, WALK_Z - 0.9, (0, y))
        for lx in (-2.8, 2.8):
            for ly in (-2.5, 2.5):
                B.cylinder(c, "stilt", (x + lx, y + ly, (WALK_Z + SEABED) / 2), 0.13, WALK_Z - SEABED, "bamboo", sides=6)
    for sx in (-1, 1):
        B.box(c, "lagoon walkway", (sx * 14.5, -52, WALK_Z), (2.2, 42, 0.2), "deck")
    for i in range(7):
        x, y = rng.uniform(-8, 8), rng.uniform(-32, -84)
        rz = math.pi / 2 + rng.uniform(-0.3, 0.3)
        B.box(c, "bangka hull", (x, y, WATER + 0.25), (5.5, 0.9, 0.5), "boat", rot_z=rz)
        for side in (-1, 1):
            B.box(c, "outrigger", (x - math.sin(rz) * side * 2.2, y + math.cos(rz) * side * 2.2, WATER + 0.12),
                  (4.2, 0.18, 0.18), "bamboo", rot_z=rz)
    # Stairs: a straight flight from the rim of one pocket to the rim of the next.
    lookup = {p[0]: p for p in POCKETS}
    for a, b in STAIRS:
        A, Bp = lookup[a], lookup[b]
        va, vb = Vector((A[1], A[2], A[3])), Vector((Bp[1], Bp[2], Bp[3]))
        d = (vb - va)
        flat = Vector((d.x, d.y, 0)).normalized()
        ea = va + Vector((flat.x * A[4] * 0.85, flat.y * A[5] * 0.85, 0))
        eb = vb - Vector((flat.x * Bp[4] * 0.85, flat.y * Bp[5] * 0.85, 0))
        mid, run = (ea + eb) / 2, eb - ea
        m = Matrix.Translation(mid + Vector((0, 0, 0.2))) @ run.to_track_quat("Y", "Z").to_matrix().to_4x4()
        o = B.box(c, f"stairs {a} to {b}", (0, 0, 0), (1.8, run.length, 0.25), "deck")
        o.matrix_world = m
    # The capilla on the summit ledge.
    _n, sx, sy, sz, _rx, _ry, _s = lookup["summit ledge (capilla)"]
    B.box(c, "capilla", (sx, sy, sz + 2.5), (5.5, 7.5, 5), "chapel")
    B.box(c, "capilla roof", (sx, sy, sz + 5.3), (6.5, 8.5, 0.4), "tin_red")
    B.box(c, "bell tower", (sx, sy - 4.6, sz + 4.5), (2.4, 2.4, 9), "chapel")
    # The landmark rock in the lagoon, facing the court.
    o = bpy.data.objects.new("landmark rock", bpy.data.meshes.get("boulder_03"))
    o.location, o.scale = (1.5, -42, SEABED + 1), (4.2, 3.4, 5.5)
    o.rotation_euler = (0, 0, 0.3)
    c.objects.link(o)


WALK_Z = -0.6


def planting(c, rng):
    """Palms and bushes where rock meets a pocket or the beach, and in the massif's gaps."""
    for p in POCKETS:
        name, px, py, pz, rx, ry, _s = p
        n = 3 if name == "court" else 2
        for k in range(n + rng.randint(0, 2)):
            a = rng.uniform(0, math.tau)
            if name == "court" and math.sin(a) < -0.3:
                continue   # keep the court's open south side clear
            x, y = px + math.cos(a) * rx * 1.02, py + math.sin(a) * ry * 1.02
            h = rng.uniform(5, 8)
            lean = rng.uniform(-0.25, 0.25)
            t = B.cylinder(c, "palm trunk", (x, y, pz + h / 2), 0.2, h, "bamboo", sides=5)
            t.rotation_euler = (lean, rng.uniform(-0.25, 0.25), 0)
            B.blob(c, "palm crown", (x + math.sin(lean) * -h / 2, y, pz + h), 1.9, "palm")
            B.blob(c, "rock-foot bush", (x + rng.uniform(-1.5, 1.5), y + rng.uniform(-1.5, 1.5), pz + 0.4),
                   rng.uniform(0.8, 1.3), "palm")
    for i in range(60):
        x, y = rng.uniform(-80, 80), rng.uniform(-60, 90)
        if pocket_at(x, y)[0] is not None or in_lagoon(x, y):
            continue
        h = height(x, y)
        if h < BEACH:
            continue
        B.blob(c, "crevice bush", (x, y, h + 0.8), rng.uniform(0.9, 1.6), "palm")


def preview(version):
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 900
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    cam.data.clip_end = 2000
    cam.data.sensor_fit = "HORIZONTAL"
    scene.collection.objects.link(cam)
    scene.camera = cam
    eye = 18 / math.tan(math.radians(95 / 2))
    E = 1.3
    shots = [
        ("plan", "ORTHO", (0, 0, 250), (0, 0, 0), 190),
        ("ref_angle", "PERSP", (22, -88, 20), (-4, 12, 8), 24),     # the reference's framing
        ("aerial", "PERSP", (-70, -120, 80), (0, 10, 5), 26),
        ("eye_north", "PERSP", (0, -9, E), (0, 45, 8), eye),
        ("eye_east", "PERSP", (-9, 0, E), (45, 0, 4), eye),
        ("eye_south", "PERSP", (0, 9, E), (0, -45, -1), eye),
        ("eye_west", "PERSP", (9, 0, E), (-45, 0, 4), eye),
    ]
    for name, kind, pos, tgt, lens in shots:
        cam.data.type = kind
        if kind == "ORTHO":
            cam.data.ortho_scale = lens
        else:
            cam.data.lens = lens
        cam.location = pos
        cam.rotation_euler = (Vector(tgt) - Vector(pos)).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = str(PREVIEWS / f"cove_{name}_v{version}.png")
        bpy.ops.render.render(write_still=True)
        print("[lagoon-cove] preview", scene.render.filepath)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
    bpy.ops.wm.read_factory_settings(use_empty=True)
    rng = random.Random(2026)
    root = bpy.data.collections.new("lagoon_cove")
    bpy.context.scene.collection.children.link(root)
    ground(L.col("Ground", root))
    place_boulders(L.col("Boulders", root), rng)
    village(L.col("Village", root), rng)
    planting(L.col("Planting", root), rng)
    L.gameplay(root)
    B.lighting()
    scene = bpy.context.scene
    try:
        scene.view_settings.look = "AgX - Punchy"   # 4: the reference's saturated warmth
    except TypeError:
        pass
    sun = next(o for o in scene.objects if o.type == "LIGHT")
    sun.data.color, sun.data.energy = (1.0, 0.92, 0.8), 4.5
    scene.world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.36, 0.6, 0.92, 1)
    SOURCE.mkdir(parents=True, exist_ok=True)
    out = SOURCE / "lagoon_cove.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(out), compress=True)
    backup = out.with_suffix(".blend1")
    if backup.exists():
        backup.unlink()
    print("[lagoon-cove] saved", out)
    if version:
        preview(version)


if __name__ == "__main__":
    main()
