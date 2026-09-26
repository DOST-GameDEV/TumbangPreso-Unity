"""Block out the reworked Lagoon Court in Blender: layout and massing only, flat colours.

  blender -b --python tools/author_lagoon_blockout.py -- --preview N

Writes ArtSource/lagoon/lagoon_blockout.blend and Logs/lagoon-blender/blockout_<shot>_vN.png.
Direction and references: docs/LAGOON_REWORK_GUIDE.md. Owner decisions, 2026-09-26:
  * the court sits on a ROCK SHELF partway up the island, the village below and around it;
  * one green HILL behind (north), open water to the south, a sand beach at the hill's foot;
  * a Filipino stilt-house village (thatch roofs, bamboo piles, walkways, bangka boats) in
    the fishing-village art style.
Coordinates: Blender, +Z up, +Y north. The court floor is z = 0 so the Unity scene's gameplay
heights carry over; the water is 4.5 m below it.
"""
import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import author_kanto_blockout as B   # noqa: E402  (box, cylinder, blob, label, mat, lighting)

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource" / "lagoon"
PREVIEWS = ROOT / "Logs" / "lagoon-blender"

BOX = B.BOX            # 7: chalk box half-size (Balance.ConfinementRadius)
THROW = B.THROW        # 8
COURT_W, COURT_D = 28.0, 26.0   # the current map's tested court, kept
SHELF_W, SHELF_D = 34.0, 32.0   # rock shelf top, a margin round the court
WATER = -4.5           # water surface, 4.5 m under the court
WALK = -2.7            # village walkway deck, 1.8 m over the water
SEA = 220.0

B.COLOURS.update({
    "water": (0.10, 0.62, 0.62), "shallows": (0.40, 0.82, 0.74), "sand": (0.93, 0.83, 0.58),
    "rock": (0.62, 0.55, 0.46), "rock_dark": (0.46, 0.40, 0.34), "hill": (0.33, 0.58, 0.22),
    "hill_dark": (0.22, 0.44, 0.16), "deck": (0.62, 0.42, 0.24), "bamboo": (0.78, 0.66, 0.36),
    "thatch": (0.76, 0.60, 0.34), "sawali": (0.86, 0.74, 0.50), "wall_paint": (0.46, 0.66, 0.56),
    "tin": (0.30, 0.52, 0.46), "tin_red": (0.64, 0.24, 0.18), "boat": (0.90, 0.88, 0.80),
    "boat_trim": (0.78, 0.20, 0.18), "net": (0.30, 0.52, 0.42), "chapel": (0.95, 0.93, 0.86),
    "palm": (0.30, 0.62, 0.14), "court": (0.80, 0.68, 0.50),
})


def col(name, parent):
    c = bpy.data.collections.new(name)
    parent.children.link(c)
    return c


def lump(c, name, center, radii, colour, sides=7, seed=0, top=0.35):
    """A faceted rock or hill: a low-poly cone frustum, jittered, so it reads as chunky stylized
    stone rather than a smooth blob."""
    rng = random.Random(seed)
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=sides, radius1=1.0, radius2=top, depth=1.0)
    for v in bm.verts:
        v.co.x *= 1 + rng.uniform(-0.18, 0.18)
        v.co.y *= 1 + rng.uniform(-0.18, 0.18)
        v.co.z += rng.uniform(-0.06, 0.06)
    bmesh.ops.subdivide_edges(bm, edges=[e for e in bm.edges if abs(e.verts[0].co.z - e.verts[1].co.z) > 0.5], cuts=1)
    for v in bm.verts:
        if -0.4 < v.co.z < 0.4:
            v.co.x *= 1 + rng.uniform(0.0, 0.2)
            v.co.y *= 1 + rng.uniform(0.0, 0.2)
    bm.to_mesh(mesh)
    bm.free()
    o = B._obj(c, name, mesh, colour)
    o.location = center
    o.scale = radii
    o.rotation_euler = (0, 0, rng.uniform(0, math.tau))
    return o


def terrain(root):
    c = col("Terrain", root)
    B.box(c, "sea", (0, 0, WATER - 0.05), (2 * SEA, 2 * SEA, 0.1), "water")
    B.box(c, "shallows round the island", (0, 30, WATER - 0.02), (120, 70, 0.1), "shallows")
    # The hill behind: a big faceted green mass north, rocky skirts, and the beach at its foot.
    lump(c, "hill", (0, 62, WATER), (60, 34, 38), "hill", sides=9, seed=1, top=0.25)
    lump(c, "hill east shoulder", (42, 46, WATER), (26, 20, 20), "hill_dark", sides=8, seed=2)
    lump(c, "hill west shoulder", (-44, 50, WATER), (28, 20, 24), "hill_dark", sides=8, seed=3)
    B.box(c, "beach east", (30, 22, WATER + 0.05), (26, 12, 0.3), "sand", rot_z=0.3)
    B.box(c, "beach west", (-30, 24, WATER + 0.05), (24, 12, 0.3), "sand", rot_z=-0.3)
    # THE ROCK SHELF: the court's platform, a faceted rock mass jutting south from the hill's
    # slope, its top flat at z = 0. Rocks break up its sides so it reads as a natural ledge.
    B.box(c, "shelf top", (0, 2, -0.15), (SHELF_W, SHELF_D, 0.3), "rock")
    B.box(c, "shelf body", (0, 2, (WATER + -0.3) / 2), (SHELF_W - 2, SHELF_D - 2, -0.3 - WATER), "rock_dark")
    B.box(c, "shelf joins hill", (0, 22, -2.0), (40, 14, 6), "rock_dark")
    rng = random.Random(7)
    for i in range(22):
        a = i / 22 * math.tau
        x, y = math.cos(a) * (SHELF_W / 2 + 0.5), 2 + math.sin(a) * (SHELF_D / 2 + 0.5)
        if y > 14:
            continue
        s = rng.uniform(2.2, 4.0)
        lump(c, f"shelf rock {i}", (x, y, WATER - 0.2), (s, s, 4.6 + rng.uniform(-0.8, 0.6)), "rock", seed=10 + i)
    B.box(c, "court surface", (0, 0, 0.02), (COURT_W, COURT_D, 0.06), "court")


def village(root):
    """The Filipino stilt village over the water south, east and west of the shelf: a walkway
    loop at WALK, stilt houses off it, two stairs up to the court, bangka boats under it."""
    c = col("Stilt village", root)
    rng = random.Random(21)
    # Walkway loop round the shelf's south half, 2.4 m wide, on piles.
    path = [(-26, 10), (-26, -24), (26, -24), (26, 10)]
    for (x0, y0), (x1, y1) in zip(path, path[1:]):
        cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
        L = math.hypot(x1 - x0, y1 - y0)
        rz = math.atan2(y1 - y0, x1 - x0)
        B.box(c, "walkway", (cx, cy, WALK), (L + 2.4, 2.4, 0.2), "deck", rot_z=rz)
        for k in range(int(L / 3) + 1):
            t = k / max(1, int(L / 3))
            for side in (-1, 1):
                px = x0 + (x1 - x0) * t - math.sin(rz) * side * 1.1
                py = y0 + (y1 - y0) * t + math.cos(rz) * side * 1.1
                B.cylinder(c, "pile", (px, py, (WALK + WATER - 1.5) / 2), 0.12, WALK - WATER + 1.5, "bamboo", sides=6)
    # Stairs from the walkway up to the court shelf, at its south-west and south-east corners.
    for sx in (-1, 1):
        B.box(c, "stairs to the court", (sx * 20.5, -15, (WALK + 0) / 2), (3.0, 7.0, 0.2), "deck", rot_z=0)
        B.label(c, "STAIRS UP", (sx * 20.5, -15, 1.5), 1.0)
    # Stilt houses: thatch roofs, sawali or painted walls, a veranda to the walkway.
    spots = [(-34, 4), (-34, -8), (-34, -20), (-20, -32), (-8, -32), (4, -32), (16, -32), (34, -20),
             (34, -8), (34, 4), (-44, -14), (44, -14), (-12, -44), (12, -44)]
    for i, (x, y) in enumerate(spots):
        w, d, h = rng.uniform(5, 7), rng.uniform(4.5, 6), rng.uniform(2.6, 3.2)
        floor = WALK + 0.1
        wall = rng.choice(("sawali", "sawali", "wall_paint", "deck"))
        B.box(c, f"house {i} floor", (x, y, floor), (w + 1.2, d + 1.2, 0.2), "deck")
        B.box(c, f"house {i} walls", (x, y, floor + h / 2), (w, d, h), wall)
        roof = "thatch" if rng.random() < 0.75 else rng.choice(("tin", "tin_red"))
        B.box(c, f"house {i} roof", (x, y, floor + h + 1.2), (w + 1.6, d + 1.6, 0.25), roof)
        m = bpy.data.meshes.new("roof")
        bm = bmesh.new()
        bmesh.ops.create_cone(bm, cap_ends=True, segments=4, radius1=1.0, radius2=0.02, depth=1.0,
                              matrix=Matrix.Rotation(math.pi / 4, 4, "Z"))
        bm.to_mesh(m)
        bm.free()
        o = B._obj(c, f"house {i} roof peak", m, roof)
        o.location, o.scale = (x, y, floor + h + 1.8), ((w + 2.0) * 0.72, (d + 2.0) * 0.72, 2.6)
        for cx in (-1, 1):
            for cy in (-1, 1):
                B.cylinder(c, "house pile", (x + cx * w / 2, y + cy * d / 2, (floor + WATER - 1.5) / 2), 0.14,
                           floor - WATER + 1.5, "bamboo", sides=6)
        if rng.random() < 0.5:   # a fishing net drying on poles
            B.box(c, "drying net", (x + w / 2 + 1.5, y, floor + 1.4), (0.05, 3.0, 1.6), "net")
    # Bangka outriggers moored in the water.
    for i in range(9):
        x, y = rng.uniform(-40, 40), rng.uniform(-40, -26)
        rz = rng.uniform(-0.4, 0.4)
        B.box(c, "bangka hull", (x, y, WATER + 0.25), (5.5, 0.9, 0.5), "boat", rot_z=rz)
        B.box(c, "bangka trim", (x, y, WATER + 0.52), (5.6, 0.95, 0.08), "boat_trim", rot_z=rz)
        for side in (-1, 1):
            B.box(c, "outrigger", (x - math.sin(rz) * side * 2.2, y + math.cos(rz) * side * 2.2, WATER + 0.12),
                  (4.2, 0.18, 0.18), "bamboo", rot_z=rz)


def ground_z(x, y):
    """The terrain's height under (x, y): a ray down onto the Terrain collection only, so
    planting sits ON the hill instead of floating at a guessed height (blockout v2)."""
    dg = bpy.context.evaluated_depsgraph_get()
    dg.update()
    hit, loc, _n, _i, obj, _m = bpy.context.scene.ray_cast(dg, Vector((x, y, 300)), Vector((0, 0, -1)))
    while hit and obj.name.startswith(("sea", "shallows")) is False and obj.users_collection[0].name != "Terrain":
        hit, loc, _n, _i, obj, _m = bpy.context.scene.ray_cast(dg, loc + Vector((0, 0, -0.01)), Vector((0, 0, -1)))
    return loc.z if hit else None


def landmark_and_planting(root):
    c = col("Landmark and planting", root)
    rng = random.Random(31)
    # THE LANDMARK: a small white capilla with a bell tower on the hill above the court, seen
    # from anywhere on the court (the reference's octopus rock, made Filipino).
    base = (6, 40, 14)
    B.box(c, "capilla", (base[0], base[1], base[2] + 2.5), (6, 9, 5), "chapel")
    B.box(c, "capilla roof", (base[0], base[1], base[2] + 5.6), (7, 10, 0.4), "tin_red")
    B.box(c, "bell tower", (base[0], base[1] - 5, base[2] + 5), (2.6, 2.6, 10), "chapel")
    B.label(c, "LANDMARK: CAPILLA", (base[0], base[1], base[2] + 13), 1.6)
    # Palms and bushes on the hill, the beaches and the shelf's back edge.
    for i in range(70):
        a = rng.uniform(-math.pi, 0.15) if i < 50 else rng.uniform(0, math.pi)
        r = rng.uniform(20, 70)
        x, y = math.cos(a) * r * 0.9, 34 + math.sin(a + math.pi / 2) * r * 0.35 + rng.uniform(-6, 12)
        if abs(x) < SHELF_W / 2 and y < 18:
            continue
        z = ground_z(x, y)
        if z is None or z < WATER + 0.2:
            continue
        h = rng.uniform(5, 8)
        B.cylinder(c, "palm trunk", (x, y, z + h / 2), 0.2, h, "bamboo", sides=5)
        B.blob(c, "palm crown", (x, y, z + h), 1.8, "palm")
    for x in (-15, -9, 9, 15):
        B.blob(c, "shelf bush", (x, 15.5, 0.6), 1.3, "palm")


def gameplay(root):
    c = col("Gameplay markers (not in game art)", root)
    z = 0.08
    for s in (-1, 1):
        B.box(c, "chalk", (s * BOX, 0, z), (0.1, 2 * BOX + 0.1, 0.02), "chalk")
        B.box(c, "chalk", (0, s * BOX, z), (2 * BOX + 0.1, 0.1, 0.02), "chalk")
        B.box(c, "throwing line", (0, s * THROW, z), (10, 0.12, 0.02), "throw")
    B.cylinder(c, "lata", (0, 0, z + 0.17), 0.14, 0.34, "chalk")
    for at in [(11, 9), (-12, 2), (4, -11.5), (-3, -THROW - 1)]:
        B.cylinder(c, "player 1.6 m", (at[0], at[1], z + 0.8), 0.4, 1.6, "person")
    B.label(c, "COURT ON ROCK SHELF 28 x 26", (0, -4, z + 0.1))


def horizon(root):
    c = col("Horizon", root)
    rng = random.Random(41)
    for i in range(12):
        a = rng.uniform(-math.pi * 0.95, -math.pi * 0.05) if i < 8 else rng.uniform(0, math.pi)
        d = rng.uniform(150, 210)
        s = rng.uniform(18, 40)
        lump(c, f"far island {i}", (math.cos(a) * d, math.sin(a) * d, WATER), (s, s * 0.8, rng.uniform(10, 30)),
             "hill_dark", sides=7, seed=50 + i)


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
    E = 1.25 + 0.05
    shots = [
        ("plan", "ORTHO", (0, 0, 200), (0, 0, 0), 150),
        ("aerial", "PERSP", (-55, -75, 45), (0, 8, -2), 28),
        ("aerial_south", "PERSP", (10, -95, 18), (0, 10, 2), 30),
        ("eye_north", "PERSP", (0, -9, E), (0, 45, 5), eye),
        ("eye_east", "PERSP", (-9, 0, E), (45, 0, 2), eye),
        ("eye_south", "PERSP", (0, 9, E), (0, -45, -1), eye),
        ("eye_west", "PERSP", (9, 0, E), (-45, 0, 2), eye),
    ]
    for name, kind, pos, tgt, lens in shots:
        cam.data.type = kind
        if kind == "ORTHO":
            cam.data.ortho_scale = lens
        else:
            cam.data.lens = lens
        cam.location = pos
        cam.rotation_euler = (Vector(tgt) - Vector(pos)).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = str(PREVIEWS / f"blockout_{name}_v{version}.png")
        bpy.ops.render.render(write_still=True)
        print("[lagoon] preview", scene.render.filepath)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
    bpy.ops.wm.read_factory_settings(use_empty=True)
    root = bpy.data.collections.new("lagoon_blockout")
    bpy.context.scene.collection.children.link(root)
    terrain(root)
    village(root)
    landmark_and_planting(root)
    gameplay(root)
    horizon(root)
    B.lighting()
    SOURCE.mkdir(parents=True, exist_ok=True)
    out = SOURCE / "lagoon_blockout.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(out), compress=True)
    backup = out.with_suffix(".blend1")
    if backup.exists():
        backup.unlink()
    print("[lagoon] blockout saved", out)
    if version:
        preview(version)


if __name__ == "__main__":
    main()
