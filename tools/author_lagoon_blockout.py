"""Block out the reworked Lagoon Court in Blender: layout and massing only, flat colours.

  blender -b --python tools/author_lagoon_blockout.py -- --preview N

Writes ArtSource/lagoon/lagoon_blockout.blend and Logs/lagoon-blender/blockout_<shot>_vN.png.
Direction and references: docs/LAGOON_REWORK_GUIDE.md. Owner decisions, 2026-09-26:
  * the square rock shelf was rejected ("i was thinking of a more natural platform"). The
    court is now the biggest CLEARING of a horseshoe cove, cradled by boulder clusters on three
    sides and open south to the beach and the lagoon tongue (docs/LAGOON_REWORK_GUIDE.md 4);
  * a Filipino stilt-house village (thatch roofs, bamboo piles, walkways, bangka boats) in
    the fishing-village art style.
Coordinates: Blender, +Z up, +Y north. The court floor is z = 0 so the Unity scene's gameplay
heights carry over; the beach is 1.2 m below it and the lagoon 1.8 m.
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


BEACH = -1.2          # beach sand level, one step (1.2 m) under the court terrace
WATER = -1.8          # lagoon surface: the beach meets shallow water
WALK = -0.4           # stilt village walkways over the lagoon


def prism(c, name, poly, z0, z1, colour):
    """A flat piece of ground from an outline (x, y) list, extruded z0..z1."""
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bot = [bm.verts.new((x, y, z0)) for x, y in poly]
    top = [bm.verts.new((x, y, z1)) for x, y in poly]
    bm.faces.new(top)
    bm.faces.new(list(reversed(bot)))
    n = len(poly)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((bot[i], bot[j], top[j], top[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    return B._obj(c, name, mesh, colour)


def blob_outline(cx, cy, rx, ry, n=18, wob=0.08, seed=0):
    rng = random.Random(seed)
    return [(cx + math.cos(a) * rx * (1 + rng.uniform(-wob, wob)), cy + math.sin(a) * ry * (1 + rng.uniform(-wob, wob)))
            for a in (k * math.tau / n for k in range(n))]


def boulder(c, name, at, size, seed, colour="rock"):
    """One big rounded-faceted boulder, the reference's building block: a low-subdivision
    icosphere squashed and jittered, flat shaded, sunk a little into whatever it sits on."""
    rng = random.Random(seed)
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=1, radius=1.0)
    for v in bm.verts:
        v.co *= 1 + rng.uniform(-0.12, 0.12)
    bm.to_mesh(mesh)
    bm.free()
    o = B._obj(c, name, mesh, colour)
    sx, sy, sz = size
    o.scale = (sx * rng.uniform(0.85, 1.15), sy * rng.uniform(0.85, 1.15), sz)
    o.location = (at[0], at[1], at[2] + sz * 0.55)
    o.rotation_euler = (rng.uniform(-0.15, 0.15), rng.uniform(-0.15, 0.15), rng.uniform(0, math.tau))
    return o


def cluster(c, name, center, spread, count, big, z, seed, stack=True):
    """A cluster of boulders: a few big ones, smaller ones round their feet, and (stack) one or
    two sitting on top, so the edge of a clearing reads as piled natural rock."""
    rng = random.Random(seed)
    for i in range(count):
        s = big * rng.uniform(0.55, 1.0)
        x = center[0] + rng.uniform(-spread[0], spread[0])
        y = center[1] + rng.uniform(-spread[1], spread[1])
        boulder(c, f"{name} {i}", (x, y, z - s * 0.4), (s, s * rng.uniform(0.8, 1.1), s * rng.uniform(0.7, 1.0)), seed * 31 + i)
        if stack and rng.random() < 0.35:
            t = s * 0.6
            boulder(c, f"{name} {i} top", (x + rng.uniform(-1, 1), y + rng.uniform(-1, 1), z + s * 0.9), (t, t, t * 0.8),
                    seed * 31 + i + 500)


# The land: a horseshoe of ground around the lagoon tongue, opening south to the sea.
LAND = [(-26, -95), (-30, -60), (-28, -36), (-20, -28), (-10, -25), (0, -24), (10, -25), (20, -28), (28, -36),
        (30, -60), (26, -95), (48, -92), (72, -50), (84, 0), (74, 50), (44, 86), (0, 98), (-44, 86), (-74, 50),
        (-84, 0), (-72, -50), (-48, -92)]


def terrain(root):
    c = col("Terrain", root)
    B.box(c, "sea", (0, 0, WATER - 0.05), (2 * SEA, 2 * SEA, 0.1), "water")
    B.box(c, "lagoon shallows", (0, -55, WATER - 0.02), (70, 90, 0.1), "shallows")
    prism(c, "land (beach level)", LAND, WATER - 3, BEACH, "sand")
    # THE COURT CLEARING: a packed-sand terrace one step over the beach, a natural rounded
    # outline a little bigger than the 28 x 26 court. Its north, east and west edges are rock.
    prism(c, "court clearing", blob_outline(0, 1, 19, 17, seed=3), BEACH - 0.5, 0.0, "court")
    # Upper terraces behind: clearings among the rock, each a step higher.
    prism(c, "terrace 2", blob_outline(-2, 30, 34, 9, seed=4), BEACH - 0.5, 4.0, "hill")
    prism(c, "terrace 3", blob_outline(4, 48, 28, 8, seed=5), 3.0, 9.0, "hill")
    prism(c, "terrace 4 (capilla)", blob_outline(-10, 62, 14, 7, seed=6), 8.0, 14.0, "hill")
    # The rock that cradles the court: clusters whose inner faces stand just outside the walls
    # at +/-13 (the walls themselves stay invisible colliders).
    cluster(c, "north rim", (0, 18.5), (15, 2.5), 11, 4.2, 0.0, 1)
    cluster(c, "east rim", (18.5, 2), (2.5, 13), 9, 4.0, 0.0, 2)
    cluster(c, "west rim", (-18.5, 2), (2.5, 13), 9, 4.0, 0.0, 3)
    cluster(c, "south-east corner", (15, -15), (3, 2), 4, 3.0, BEACH, 4)
    cluster(c, "south-west corner", (-15, -15), (3, 2), 4, 3.0, BEACH, 5)
    # The two arms of the horseshoe: piled rock rising toward the back.
    cluster(c, "east arm", (46, -40), (12, 40), 26, 9.0, BEACH, 6)
    cluster(c, "west arm", (-46, -40), (12, 40), 26, 9.0, BEACH, 7)
    cluster(c, "east shoulder", (44, 30), (14, 26), 20, 11.0, BEACH, 8)
    cluster(c, "west shoulder", (-44, 30), (14, 26), 20, 11.0, BEACH, 9)
    cluster(c, "between terraces", (0, 40), (30, 3), 14, 5.0, 3.5, 10)
    # The spire behind everything, green-topped.
    lump(c, "spire", (12, 82, BEACH + 30), (26, 22, 60), "rock", sides=8, seed=11, top=0.18)   # spans BEACH..BEACH+60
    B.blob(c, "spire cap", (12, 82, BEACH + 58), 6.0, "hill")
    # Wooden steps from the clearing's open south side down to the beach.
    for k in range(4):
        B.box(c, "steps down to the beach", (0, -16.5 - k * 0.7, -0.15 - k * 0.3), (10, 0.8, 0.3), "deck")
    for sx in (-1, 1):
        B.box(c, "stairs up to terrace 2", (sx * 22, 23, 2.0), (3, 8, 0.25), "deck", rot_z=0)


def village(root):
    """Filipino stilt houses along both sides of the lagoon tongue, a walkway out along each
    side from the beach, bangkas moored in the lagoon; more houses on the upper terraces."""
    c = col("Stilt village", root)
    rng = random.Random(21)
    for sx in (-1, 1):
        B.box(c, "lagoon walkway", (sx * 21, -58, WALK), (2.4, 60, 0.2), "deck")
        for k in range(20):
            for side in (-1, 1):
                B.cylinder(c, "pile", (sx * 21 + side * 1.1, -30 - k * 3, (WALK + WATER - 1.5) / 2), 0.12,
                           WALK - WATER + 1.5, "bamboo", sides=6)
    spots = [(-14, -40), (-14, -54), (-14, -68), (-14, -82), (14, -44), (14, -58), (14, -72), (14, -86)]
    upper = [(-26, 30, 4.0), (-10, 32, 4.0), (22, 30, 4.0), (-14, 48, 9.0), (14, 49, 9.0)]
    houses = [(x, y, WALK + 0.1, True) for x, y in spots] + [(x, y, z, False) for x, y, z in upper]
    for i, (x, y, floor, stilts) in enumerate(houses):
        w, d, h = rng.uniform(5, 7), rng.uniform(4.5, 6), rng.uniform(2.6, 3.2)
        wall = rng.choice(("sawali", "sawali", "wall_paint", "deck"))
        B.box(c, f"house {i} floor", (x, y, floor), (w + 1.2, d + 1.2, 0.2), "deck")
        B.box(c, f"house {i} walls", (x, y, floor + h / 2), (w, d, h), wall)
        roof = "thatch" if rng.random() < 0.75 else rng.choice(("tin", "tin_red"))
        m = bpy.data.meshes.new("roof")
        bm = bmesh.new()
        bmesh.ops.create_cone(bm, cap_ends=True, segments=4, radius1=1.0, radius2=0.02, depth=1.0,
                              matrix=Matrix.Rotation(math.pi / 4, 4, "Z"))
        bm.to_mesh(m)
        bm.free()
        o = B._obj(c, f"house {i} roof", m, roof)
        o.location, o.scale = (x, y, floor + h + 1.2), ((w + 2.0) * 0.72, (d + 2.0) * 0.72, 2.6)
        if stilts:
            for cx in (-1, 1):
                for cy in (-1, 1):
                    B.cylinder(c, "house pile", (x + cx * w / 2, y + cy * d / 2, (floor + WATER - 1.5) / 2), 0.14,
                               floor - WATER + 1.5, "bamboo", sides=6)
            if rng.random() < 0.5:
                B.box(c, "drying net", (x + (3.8 if x > 0 else -3.8), y, floor + 1.4), (0.05, 3.0, 1.6), "net")
    for i in range(8):
        x, y = rng.uniform(-8, 8), rng.uniform(-34, -88)
        rz = math.pi / 2 + rng.uniform(-0.3, 0.3)
        B.box(c, "bangka hull", (x, y, WATER + 0.25), (5.5, 0.9, 0.5), "boat", rot_z=rz)
        B.box(c, "bangka trim", (x, y, WATER + 0.52), (5.6, 0.95, 0.08), "boat_trim", rot_z=rz)
        for side in (-1, 1):
            B.box(c, "outrigger", (x - math.sin(rz) * side * 2.2, y + math.cos(rz) * side * 2.2, WATER + 0.12),
                  (4.2, 0.18, 0.18), "bamboo", rot_z=rz)
    # Piers out from the beach.
    for x in (-8, 9):
        B.box(c, "pier", (x, -32, WALK), (2.2, 10, 0.2), "deck")


def ground_z(x, y):
    """The terrain's height under (x, y): a ray down, keeping only Terrain hits, so planting
    sits ON the ground and rock."""
    dg = bpy.context.evaluated_depsgraph_get()
    dg.update()
    origin = Vector((x, y, 300))
    for _ in range(12):
        hit, loc, _n, _i, obj, _m = bpy.context.scene.ray_cast(dg, origin, Vector((0, 0, -1)))
        if not hit:
            return None
        if obj.users_collection and obj.users_collection[0].name == "Terrain" and not obj.name.startswith("sea"):
            return loc.z
        origin = loc + Vector((0, 0, -0.01))
    return None


def landmark_and_planting(root):
    c = col("Landmark and planting", root)
    rng = random.Random(31)
    # THE CAPILLA on the highest ledge under the spire, seen from the whole court.
    bx, by, bz = -10, 62, 14.0
    B.box(c, "capilla", (bx, by, bz + 2.5), (6, 8, 5), "chapel")
    B.box(c, "capilla roof", (bx, by, bz + 5.4), (7, 9, 0.4), "tin_red")
    B.box(c, "bell tower", (bx, by - 5, bz + 4.5), (2.6, 2.6, 9), "chapel")
    # THE WATERLINE LANDMARK: a big rock in the lagoon's middle, facing the court, to carry a
    # painted emblem or a banner (the reference's octopus rock).
    boulder(c, "landmark rock", (0, -44, WATER - 0.6), (4.5, 3.5, 6.0), 999, "chapel")
    B.label(c, "LANDMARK ROCK (painted emblem)", (0, -44, 10), 1.4)
    # Palms and bushes where rock meets flat ground, never spread evenly.
    for i in range(90):
        x, y = rng.uniform(-75, 75), rng.uniform(-80, 70)
        if -19 < x < 19 and -18 < y < 19:
            continue   # never on the court clearing
        z = ground_z(x, y)
        if z is None or z < BEACH - 0.1:
            continue
        h = rng.uniform(5, 8)
        B.cylinder(c, "palm trunk", (x, y, z + h / 2), 0.2, h, "bamboo", sides=5)
        B.blob(c, "palm crown", (x, y, z + h), 1.8, "palm")
    for i in range(30):
        a = rng.uniform(0, math.pi)
        x, y = math.cos(a) * rng.uniform(16, 20), 1 + math.sin(a) * rng.uniform(15, 19)
        B.blob(c, "rock-foot bush", (x, y, 0.4), rng.uniform(0.8, 1.4), "palm")


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
        ("aerial", "PERSP", (-60, -110, 70), (0, 5, 0), 28),
        ("aerial_south", "PERSP", (6, -130, 22), (0, 10, 6), 30),
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
