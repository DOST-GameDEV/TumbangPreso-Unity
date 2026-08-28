"""Bring a downloaded footwear GLB into the game as a slipper prop, unretouched.

Run with Blender, not the system Python:
  blender -b --factory-startup --python tools/build_slipper_models.py -- [id ...]

⚠️⚠️ THIS IS THE "AS IS" PIPELINE AND IT IS DELIBERATELY NOT `build_slipper_roster.py`.
That script owns the old Poly-era sources and does four things this one must not: it
recolours every material to a flat two-colour palette, it bevels the silhouette, it
squashes Z independently of X and Y to fit MAX_HEIGHT, and it decimates nothing but
assumes a few hundred triangles. Those four are exactly what made the shipped roster
read as flat blobs, and 🧑 2026-08-28 asked for the new sources to go in untouched:
*"no need to compress them okay"*, *"js put them as is"*. The two scripts coexist
because PAMBAHAY still comes from the old flip-flop source and still wants the old
treatment. Do not merge them.

So this file changes exactly three things about a source, and every one of them is
forced by the runtime rather than by taste:

  1. ISOLATES ONE SHOE. Sources ship as pairs. `Balance.SlipperHitRadius` is a fixed
     0.23 m and every contact in this game is a host-side distance check, so a prop
     visually twice as wide as its hit radius reads as a slipper passing through the
     lata without knocking it down.
  2. TURNS THE SHOE TO POINT DOWN +X, toe first. See `align_to_x`.
  3. SCALES UNIFORMLY so the shoe is TARGET_LENGTH toe to heel. `MatchInstaller`
     `BuildSlipper` instantiates the model into the prop with no rescale at all, so
     the mesh's own units ARE the gameplay size. A Sketchfab export in centimetres
     spawns a five metre sandal.
  4. CENTRES ON XY AND SEATS ON Z=0, because the prop's origin is what the hand and
     the throw arc track.

⚠️ THE SCALE IS UNIFORM, UNLIKE THE OLD SCRIPT. `build_slipper_roster.py` multiplies Z
by a separate `height_scale` to cap every shoe at 0.160 m, which is what flattened the
stiletto into an unreadable wedge. Here an over-tall result is reported and left alone:
a squashed shoe is a worse outcome than a tall one, and none of the current sources
trips it.
"""

import math
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets/TumbangPreso/Art/models/kits/footwear"
OUTPUT = ROOT / "Assets/TumbangPreso/Art/models"

# Both constants are read from the shipping game, not chosen here.
# `build_slipper_roster.py` normalised the whole old roster to 0.432 m and the hand
# attachment, the throw arc and every screenshot of the character select assume it.
TARGET_LENGTH = 0.432

# Only a warning threshold. See the module note on why nothing is squashed to meet it.
TALL_WARNING = 0.160


class Recipe:
    """One roster id and the source it is cut from.

    The two stages run in this order and are independent:

    `keep`   name fragments. Only objects whose name contains one of them survive.
             Empty keeps everything.
    `split`  "pair"  cluster what is left in two and keep the heavier half
             "whole" what is left is already a single shoe

    ⚠️ PREFER A NAME FILTER OVER CLUSTERING WHENEVER THE SOURCE LABELS ITS PARTS.
    Clustering is a guess and it guessed wrong on the clog: 🧑 2026-08-28, looking at
    the render, *"yo what sup with crocs why is there 2"*. The heel strap of the right
    shoe sits closer to the left shoe's centroid than to its own, so 2-means claimed it
    and the prop shipped with a floating fragment of the other foot. That source names
    its objects `sabo_L_*` and `Sabo_R_*`, which is not a guess at all.

    ⚠️ AND THE TWO STAGES COMPOSE BECAUSE A SOURCE CAN NEED BOTH. The loafer source is
    a shop display: five objects, of which four are a pillar, a booth and a wooden
    shelf standing under the shoes rather than being them. The name filter drops the
    furniture and the pair split then picks one shoe out of what is left. Neither
    stage alone would have produced a shoe.
    """

    def __init__(self, entry_id, source, split="pair", keep=(), credit=""):
        self.id = entry_id
        self.source = source
        self.split = split
        self.keep = keep
        self.credit = credit


RECIPES = (
    Recipe("tsinelas", "src_tsinelas_flip_flops.glb", "pair",
           credit="Flip Flops by Remie07, CC-BY"),
    # ⚠️ TWO IDS CHANGED SHOE HERE AND NEITHER INDEX MOVED, WHICH IS THE WHOLE POINT.
    # 🧑 2026-08-28: *"the current pantulog should be renamed to another slipper, keep the
    # currentpantulog js give it a diff name"*, *"this one should be pantulog"*. PANTULOG
    # is slipper index 2 and that index crosses the wire, so the id cannot move: the
    # fuzzy house slipper takes over index 2's MODEL, and the rubber bathroom slide it
    # displaced took over PAMBAHAY at index 6, whose purple Poly flip-flop was retired.
    Recipe("pantulog", "src_pantulog_fuzzy.glb", "pair",
           credit="Fuzzy Slippers by Benz, CC-BY"),
    Recipe("pambahay", "src_pambahay_rubber_slide.glb", "pair",
           credit="Worn rubber slide, CC-BY"),
    Recipe("alpombra", "src_alpombra_heel_mule.glb", "pair",
           credit="Fashion heel sandals, CC-BY"),
    Recipe("crocs", "src_crocs_sabo_clog.glb", "whole", keep=("sabo_L_",),
           credit="[XYZ School] HW6-Detailing. Sabo by andrew.rudik, CC-BY"),

    # These three arrive as a single shoe already, so there is nothing to split.
    # ⚠️ THE BUILD PRINTS EACH FINISHED SIZE AND THAT IS THE CHECK ON "whole". A single
    # shoe comes out about 0.432 x 0.17; anything near 0.432 x 0.30 is two of them and
    # the recipe is wrong.
    Recipe("spartan", "src_spartan_worn_flip_flop.glb", "whole",
           credit="Worn Flip Flop by inciprocal, CC-BY"),
    Recipe("heels", "src_heels_plateau_sandal.glb", "whole",
           credit="Plateau Sandal Heels by hiirusama, CC-BY"),
    Recipe("sandals", "src_sandals_chappal.glb", "whole",
           credit="Chappal by Amad Junaid, CC-BY"),

    # ⚠️ THE LOAFER SOURCE IS A SHOP DISPLAY, NOT A PAIR OF SHOES. Five objects, and
    # four of them are a pillar, a booth panel and a wooden shelf that the shoes stand
    # ON: `PIllar 3.014_Display_Wood_0`, `PIllar 3.014_Booth_White_0`,
    # `PIllar 3.014_BlackMatte_0` and `Group_6_8.317_White.009_0`, all sitting between
    # z 27 and z 135 with the shoes at z 143 to 172. Importing it whole would have put
    # a shop fitting in somebody's hand.
    Recipe("loafers", "src_loafers_school.glb", "pair", keep=("Plane.039",),
           credit="Shoes Loafers (A6-2) by eeelabvisual, CC-BY"),
)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for blocks in (bpy.data.meshes, bpy.data.materials, bpy.data.images,
                   bpy.data.cameras, bpy.data.lights):
        for block in list(blocks):
            blocks.remove(block)


def flatten(obj):
    """Drop the importer's parent chain and bake the transform into the vertices."""
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    if obj.parent is not None:
        bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    obj.select_set(False)


def bounds(objects):
    points = [o.matrix_world @ Vector(c) for o in objects for c in o.bound_box]
    low = Vector((min(p[i] for p in points) for i in range(3)))
    high = Vector((max(p[i] for p in points) for i in range(3)))
    return low, high


def join_all(objects):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objects:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    if len(objects) > 1:
        bpy.ops.object.join()
    return bpy.context.object


def loose_parts(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.separate(type="LOOSE")
    bpy.ops.object.mode_set(mode="OBJECT")
    return [o for o in bpy.context.scene.objects if o.type == "MESH"]


def keep_one_shoe(parts):
    """2-means the loose parts in the ground plane and return the heavier cluster.

    ⚠️ THE SEEDS ARE THE TWO FARTHEST-APART PARTS, NOT THE EXTREMES OF X OR Y.
    A pair is not necessarily separated along a world axis: the flip-flop source
    lays its two shoes out on a diagonal, where both X and Y spread are dominated
    by the length of a single shoe rather than by the gap between them. Seeding on
    the widest axis put a seed at each END OF ONE SHOE and split it lengthwise.

    ⚠️ AND THE SEED SEARCH IGNORES SCRAPS UNDER 1% OF THE VERTICES. Sources carry
    stray nine-vertex fragments, buckles and loose stitching well outside the two
    shoes, and any one of them is farther from everything than the shoes are from
    each other.
    """
    weighted = []
    for o in parts:
        n = len(o.data.vertices)
        if n == 0:
            continue
        mid = sum((o.matrix_world @ v.co for v in o.data.vertices), Vector()) / n
        weighted.append((n, Vector((mid.x, mid.y)), o))

    total = sum(n for n, _, _ in weighted)
    major = [w for w in weighted if w[0] >= total * 0.01] or weighted

    seeds = None
    best = -1.0
    for i in range(len(major)):
        for j in range(i + 1, len(major)):
            d = (major[i][1] - major[j][1]).length
            if d > best:
                best, seeds = d, [major[i][1].copy(), major[j][1].copy()]

    if seeds is None:
        return parts

    for _ in range(32):
        groups = [[], []]
        for n, flat, o in weighted:
            near = 0 if (flat - seeds[0]).length <= (flat - seeds[1]).length else 1
            groups[near].append((n, flat, o))
        moved = False
        for i, g in enumerate(groups):
            w = sum(n for n, _, _ in g)
            if not w:
                continue
            centre = sum((flat * n for n, flat, _ in g), Vector((0.0, 0.0))) / w
            if (centre - seeds[i]).length > 1e-9:
                moved = True
            seeds[i] = centre
        if not moved:
            break

    counts = [sum(n for n, _, _ in g) for g in groups]
    ratio = min(counts) / max(max(counts), 1)
    print(f"    clusters {counts[0]} / {counts[1]} verts, "
          f"gap {(seeds[0] - seeds[1]).length:.4f}, balance {ratio:.2f}")
    if ratio < 0.25:
        print("    ⚠ lopsided split: the source may not be a symmetric pair. "
              "Check the render before trusting this one.")

    keep_index = 0 if counts[0] >= counts[1] else 1
    for i, g in enumerate(groups):
        if i == keep_index:
            continue
        for _, _, o in g:
            bpy.data.objects.remove(o, do_unlink=True)

    return [o for _, _, o in groups[keep_index]]


def level(obj):
    """Lay the shoe flat, sole down, before anything measures it.

    ⚠️⚠️ WITHOUT THIS, TWO SOURCES CAME OUT STANDING ON END. `align_to_x` only ever
    corrected YAW, on the assumption that every source already had its sole in the
    ground plane. That held for the first four and broke on the second four: the heel
    and the loafer are both photographed on a shop display, pitched back on a slanted
    shelf, so the shoe is rotated about a horizontal axis as well. The build's own
    height warning is what caught it, reporting a 0.432 m loafer as 0.361 m TALL when a
    loafer is about 0.10, and a render confirmed the shoe was tipped rather than tall.

    ⚠️ THE THREE AXES COME FROM THE VERTEX COVARIANCE, NOT THE BOUNDING BOX. A box drawn
    round a tilted shoe is bigger than the shoe on every axis and its proportions say
    nothing about which way is up. The eigenvectors of the covariance are the shoe's own
    length, width and thickness whatever angle it was exported at, and a shoe is always
    longest down its length and shortest through its sole, so the ordering is stable.

    ⚠️ AND WHICH END OF THE SHORT AXIS IS "UP" IS DECIDED BY FOOTPRINT, NOT BY GUESSING.
    An eigenvector has no sign. A shoe spreads out at the sole and narrows through the
    upper, so the half with the larger XY footprint is the bottom. Getting this backwards
    puts every slipper in the game sole-up in the hand, which is exactly the class of
    fault `Slipper.CarryRotation`'s note already records once.
    """
    verts = [obj.matrix_world @ v.co for v in obj.data.vertices]
    n = len(verts)
    mean = Vector((sum(v.x for v in verts) / n,
                   sum(v.y for v in verts) / n,
                   sum(v.z for v in verts) / n))

    cov = [[0.0] * 3 for _ in range(3)]
    for v in verts:
        d = (v.x - mean.x, v.y - mean.y, v.z - mean.z)
        for i in range(3):
            for j in range(3):
                cov[i][j] += d[i] * d[j]

    import numpy

    values, vectors = numpy.linalg.eigh(numpy.array(cov))
    order = list(numpy.argsort(values))[::-1]          # length, width, thickness
    axes = [Vector(vectors[:, k]) for k in order]

    # Right-handed, or the mesh comes through mirrored and every logo reads backwards.
    if axes[0].cross(axes[1]).dot(axes[2]) < 0.0:
        axes[2] = -axes[2]

    basis = Matrix((axes[0], axes[1], axes[2])).to_4x4()
    obj.matrix_world = basis @ obj.matrix_world
    flatten(obj)

    low, high = bounds((obj,))
    mid_z = (low.z + high.z) * 0.5
    halves = [[], []]
    for v in obj.data.vertices:
        w = obj.matrix_world @ v.co
        halves[0 if w.z < mid_z else 1].append(w)

    def footprint(group):
        if not group:
            return 0.0
        return ((max(p.x for p in group) - min(p.x for p in group))
                * (max(p.y for p in group) - min(p.y for p in group)))

    if footprint(halves[1]) > footprint(halves[0]):
        obj.matrix_world = Matrix.Rotation(math.pi, 4, "X") @ obj.matrix_world
        flatten(obj)
        return True
    return False


def align_to_x(obj):
    """Turn the shoe about Z until it lies along +X, toe first.

    ⚠️⚠️ +X IS THE GAME'S CONVENTION AND IT IS NOT ARBITRARY.
    `Slipper.CarryRotation` is a quarter turn about Y, converted from the Godot
    `CARRY_BASIS`, and its note records what happens without it: "the slipper lies
    across the palm sideways". That rotation takes +X to forward, so a mesh whose
    length runs down +X is the one it was written for. Measured on the shipped roster,
    PAMBAHAY, SPARTAN and HEELS are all 0.432 m along glTF X and only SANDALS is not,
    which is the odd one out rather than the rule.

    ⚠️ AND THE ANGLE IS MEASURED, NOT ASSUMED, BECAUSE A SOURCE CAN SIT ON A DIAGONAL.
    The flip-flop source lays its shoe at roughly 40 degrees to the world axes. Scaling
    off the axis-aligned bounding box then divides by the diagonal of the box rather
    than by the length of the shoe, so the first build came out 0.352 x 0.432 and the
    shoe itself was well under the 0.432 m every other prop is cut to. The major axis
    of the vertex covariance is the shoe's own length whatever angle it was exported at.
    """
    verts = [obj.matrix_world @ v.co for v in obj.data.vertices]
    n = len(verts)
    mean_x = sum(v.x for v in verts) / n
    mean_y = sum(v.y for v in verts) / n

    sxx = sum((v.x - mean_x) ** 2 for v in verts)
    syy = sum((v.y - mean_y) ** 2 for v in verts)
    sxy = sum((v.x - mean_x) * (v.y - mean_y) for v in verts)

    # Principal axis of a symmetric 2x2 covariance, in closed form.
    angle = 0.5 * math.atan2(2.0 * sxy, sxx - syy)
    obj.matrix_world = Matrix.Rotation(-angle, 4, "Z") @ obj.matrix_world
    flatten(obj)

    # ⚠️ PCA GIVES AN AXIS, NOT A DIRECTION, so half the shoes come out heel first.
    # A shoe carries most of its material forward of the middle: the sole widens through
    # the ball of the foot and narrows to the heel. So the vertex centroid sits toward
    # the toe of the bounding box, and a centroid behind the box centre means the shoe
    # is facing backwards.
    low, high = bounds((obj,))
    centroid_x = sum((obj.matrix_world @ v.co).x for v in obj.data.vertices) / n
    if centroid_x < (low.x + high.x) * 0.5:
        obj.matrix_world = Matrix.Rotation(math.pi, 4, "Z") @ obj.matrix_world
        flatten(obj)
        return math.degrees(-angle) + 180.0
    return math.degrees(-angle)


def build(recipe):
    path = SOURCE / recipe.source
    if not path.exists():
        print(f"[slipper] {recipe.id}: SKIP, no source at {path}")
        return False

    print(f"[slipper] {recipe.id} <- {recipe.source}")
    clear_scene()
    bpy.ops.import_scene.gltf(filepath=str(path))

    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    if not meshes:
        raise RuntimeError(f"{recipe.id}: source has no meshes")
    for o in meshes:
        flatten(o)

    if recipe.keep:
        wanted = [o for o in meshes
                  if any(fragment in o.name for fragment in recipe.keep)]
        if not wanted:
            raise RuntimeError(
                f"{recipe.id}: no object matched {recipe.keep}. Objects present: "
                f"{[o.name for o in meshes]}")
        for o in meshes:
            if o not in wanted:
                bpy.data.objects.remove(o, do_unlink=True)
        print(f"    kept by name: {[o.name for o in wanted]}")
        meshes = wanted

    if recipe.split == "pair":
        joined = join_all(meshes)
        parts = loose_parts(joined)
        kept = keep_one_shoe(parts)
        model = join_all(kept)
    else:
        model = join_all(meshes)

    flipped = level(model)
    turned = align_to_x(model)
    print(f"    levelled{' and turned sole-down' if flipped else ''}, "
          f"then {turned:+.1f} deg to lie along +X")

    low, high = bounds((model,))
    length = high.x - low.x
    if length <= 0.0:
        raise RuntimeError(f"{recipe.id}: zero horizontal extent")

    scale = TARGET_LENGTH / length
    centre = (low + high) * 0.5
    model.scale = Vector((scale, scale, scale))
    model.location = Vector((-centre.x * scale, -centre.y * scale, -low.z * scale))
    flatten(model)

    model.name = f"tsinelas_{recipe.id}"
    model.data.name = model.name

    final_low, final_high = bounds((model,))
    size = final_high - final_low
    if size.z > TALL_WARNING:
        print(f"    ⚠ {size.z:.3f} m tall, over the {TALL_WARNING} m the old roster "
              f"capped at. Left unsquashed on purpose; see the module note.")

    OUTPUT.mkdir(parents=True, exist_ok=True)
    out = OUTPUT / f"tsinelas_{recipe.id}.glb"
    bpy.ops.object.select_all(action="DESELECT")
    model.select_set(True)
    bpy.context.view_layer.objects.active = model
    bpy.ops.export_scene.gltf(
        filepath=str(out),
        export_format="GLB",
        use_selection=True,
        export_apply=True,
        export_yup=True,
        export_materials="EXPORT",
        export_cameras=False,
        export_lights=False,
    )

    tris = len(model.data.polygons)
    print(f"    {size.x:.3f} x {size.y:.3f} x {size.z:.3f} m, {tris} faces -> {out.name}")
    return True


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    wanted = set(argv)
    built = 0
    for recipe in RECIPES:
        if wanted and recipe.id not in wanted:
            continue
        built += 1 if build(recipe) else 0
    print(f"[slipper] built {built}")


if __name__ == "__main__":
    main()
