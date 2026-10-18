"""Basilio's BOULDER rocks, CARVED AND BAKED the way the Lagoon Cove stones and the ice crystals are.

  py -3 tools/build_dante_rocks.py            the stone of his own first-person hand (APPROVED 2026-10-12: do not rerun
                                              it idly, a run re-bakes the mask the approved films were drawn with)
  py -3 tools/build_dante_rocks.py world      the stone everybody else sees: the same rocks closed over the whole shoe

Writes, in Assets/TumbangPreso/Resources/Models/AbilityFx:
  dante_boulder.glb          the four rocks that grip the tsinelas (crown, jaw, knuckle-l, knuckle-r) and three pieces
                             of rubble (chunk0 to chunk2). Real metres, y up, +z the tsinelas's toe, the origin at the
                             middle of its sole; each rock a named node at its seat, its origin at its own middle.
  dante_boulder_mask.png     ONE atlas for all seven, LINEAR: R how near the nearest plane break is, G the broad
                             shoulder, B occlusion (the other rocks' as well as its own), A the gaps between pressed rocks
  dante_boulder_bench.glb    the bench's stand-ins only (a shoe and a 1.6 m body), never drawn in a match
and the bakes as pictures in Logs/dante-rocks/ for a board.

`world` writes dante_boulder_world.glb and dante_boulder_world_mask.png instead: six rocks and no rubble (`WORLD`).
The owner: "can you make it so that the slipper is visible only in the dante fpv?" In his own first-person hand the
stone stays the approved four, toe and strap out, so he knows what he holds. Everywhere else the stone covers the
shoe: the same four carved the same way, seated a little forward, with one more over the toe and one over the strap.
It is a model and a mask of its own because the gaps between pressed rocks are baked, and these press differently.

WHY IT IS MADE THIS WAY. Its first four builds typed facets and PAINTED a pale line along them by formula. The owner,
of the fourth: "need you to read up on how the rock edge highlights on the lagooncove map are done. also isnt the
process similar to how you made the ice crystals?" It is, and both were settled with him step by step
(docs/LAGOON_REWORK_GUIDE.md, the rock kit record; tools/build_ice_crystals.py). So nothing is painted here:

  * THE FORM IS CARVED. A rounded body (`author_lagoon_rocks._body`) cut by TYPED broad planes as true cuts
    (`_cut`: bisect and cap), unequal on purpose, one or two of them deep so the rock has a dominant face; then
    every break over 25 degrees gets a narrow real bevel (`_bevel_breaks`) and only the sharp breaks stay sharp
    (`_sharp_edges`). A chip is a small cut that takes a corner off; a dent is scooped out of a face with a boolean.
    Those four functions are the Lagoon kit's own, CALLED, not copied.
  * THE EDGE WEAR IS BAKED FROM THE FORM. R is the true distance from each texel to the nearest plane break
    (`bake_lagoon_rock_edges.plane_breaks` and `distance_to_breaks`, called), linear over `REACH` metres, so the
    shader can lay the brushed band and the thin line he approved on the Lagoon rocks at a width in metres. G and B
    are Cycles bakes of the Lagoon tool's bevel-normal shoulder and of occlusion.
  * THE GAPS ARE BAKED TOO. The rocks are baked SEATED, pressed together as they sit on the shoe, so B darkens where
    one leans on another, and A is occlusion over a finger's width only: the crevices between pressed rocks and the
    floor of a dent. That is the only place his molten light is laid (`FxStudyRock.shader`).

The look target is the owner's three pictures (chunky, convex, soft-cornered rocks; one or two big faces among smaller
ones; a cluster with the second and third rocks half to two thirds the big one's size).

Blender runs in the background for the carve and the bakes (never Unity). Seeded: two runs carve the same rocks.
"""
import math
import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent
import os

MODELS = ROOT / "Assets" / "TumbangPreso" / "Resources" / "Models"
# Where it writes. DANTE_ROCKS_OUT names another folder, to build aside while the owner's editor is in Play and copy in after.
OUT = Path(os.environ["DANTE_ROCKS_OUT"]) if os.environ.get("DANTE_ROCKS_OUT") else MODELS / "AbilityFx"
LOGS = ROOT / "Logs" / "dante-rocks"
BLENDER = r"C:\Program Files\Blender Foundation\Blender 5.0\blender.exe"
SIZE = 2048
REACH = 0.040          # metres: R falls linearly from 1 on a plane break to 0 this far from it
GAP = 0.030            # metres: how near another rock's skin counts as a gap (A)

# ---------------------------------------------------------------------------------------------------------------------
# THE ROCKS. Typed in Blender's axes while they are carved (z up); `seat` is in the game's (y up, +z the toe).
# A cut is (turn round the rock in degrees, how far its normal leans up, depth): depth 1 only touches the body, 0 would
# halve it, so 0.6 is a big face and 0.9 knocks a corner off. Cuts are made in order.
#   half     the body before it is cut (x, y, z)        p      2 a ball, 3 a pillow
#   across   its widest horizontal extent once done, in metres
#   dent     (turn, lean, radius in metres) a scoop out of the face that looks that way, or None
#   fracture (turn, lean) ONE short split cut into the face that looks that way, in two legs with a kink, or absent.
#            Only the big rock has one: carried in daylight the stone showed no molten light at all, because the gaps
#            between the rocks are hairlines from most sides. A cut has a floor the gap bake finds, so it lights.
# ---------------------------------------------------------------------------------------------------------------------
ROCKS = {
    "crown": dict(seat=(-0.010, 0.050, -0.150), across=0.345, yaw=15, seed=11, grid=4, half=(1.00, 0.95, 1.08), p=2.5, taper=0.10,
                  cuts=[(0, 9.0, 0.70),                                   # the top, tipped a little
                        (300, 0.12, 0.60),                                # THE big face
                        (25, 0.05, 0.80), (100, 0.28, 0.74), (168, 0.00, 0.82), (236, 0.20, 0.77),
                        (335, 0.95, 0.78), (140, 0.85, 0.82),             # two shoulders
                        (60, -0.60, 0.80), (200, -0.50, 0.83), (285, -0.75, 0.80),
                        (0, -9.0, 0.76),                                  # the foot
                        (262, 1.15, 0.90), (45, 1.30, 0.92)],             # two corners knocked off
                  dent=(318, 0.75, 0.034), fracture=(300, 0.12)),
    "jaw": dict(seat=(0.020, -0.090, -0.010), across=0.300, yaw=-20, seed=23, grid=4, half=(1.15, 1.00, 0.72), p=2.6, taper=-0.08,
                cuts=[(0, 9.0, 0.58), (40, 0.10, 0.72), (118, 0.05, 0.80), (190, 0.22, 0.70), (262, 0.00, 0.80), (320, 0.18, 0.78),
                      (80, -0.70, 0.78), (225, -0.60, 0.80), (0, -9.0, 0.72), (350, 0.9, 0.90)],
                dent=None),
    "knuckle-l": dict(seat=(-0.150, -0.030, -0.040), across=0.235, yaw=50, seed=37, grid=4, half=(0.95, 0.90, 1.05), p=2.4, taper=0.14,
                      cuts=[(0, 9.0, 0.72), (200, 0.15, 0.62), (285, 0.05, 0.80), (20, 0.25, 0.76), (105, 0.00, 0.80),
                            (150, 0.9, 0.80), (250, -0.6, 0.80), (60, -0.6, 0.82), (0, -9.0, 0.76), (320, 1.2, 0.90)],
                      dent=(215, 0.55, 0.020)),
    "knuckle-r": dict(seat=(0.150, -0.060, -0.070), across=0.185, yaw=140, seed=41, grid=4, half=(1.00, 0.92, 0.90), p=2.4, taper=0.06,
                      cuts=[(0, 9.0, 0.68), (70, 0.10, 0.66), (160, 0.20, 0.78), (245, 0.05, 0.80), (320, 0.25, 0.76),
                            (20, -0.6, 0.80), (190, -0.7, 0.80), (0, -9.0, 0.74)],
                      dent=None),
    "chunk0": dict(seat=None, across=0.150, yaw=0, seed=58, grid=3, half=(1.0, 0.9, 0.85), p=2.4, taper=0.08,
                   cuts=[(0, 9.0, 0.68), (30, 0.1, 0.64), (130, 0.2, 0.78), (215, 0.0, 0.76), (300, 0.3, 0.78), (80, -0.6, 0.80), (0, -9.0, 0.72), (250, 1.1, 0.90)],
                   dent=None),
    "chunk1": dict(seat=None, across=0.115, yaw=0, seed=61, grid=3, half=(1.05, 0.85, 0.80), p=2.3, taper=0.10,
                   cuts=[(0, 9.0, 0.66), (190, 0.15, 0.62), (290, 0.05, 0.78), (60, 0.25, 0.76), (130, -0.6, 0.80), (0, -9.0, 0.72)],
                   dent=None),
    "chunk2": dict(seat=None, across=0.085, yaw=0, seed=72, grid=3, half=(0.95, 0.95, 0.90), p=2.3, taper=0.05,
                   cuts=[(0, 9.0, 0.70), (100, 0.1, 0.64), (220, 0.2, 0.76), (330, 0.0, 0.78), (0, -9.0, 0.72)],
                   dent=None),
}


# THE WORLD'S STONE: the approved four, moved so that the whole of it lies inside the shoe's own 0.51 m hit capsule
# with the shoe covered (the big rock 5.5 cm forward, off the heel's overhang; the others a little, to stay pressed to
# it), and the two that close it. Its seats, by the carve's own sizes: 0.512 long (0.262 behind the sole's middle to
# 0.250 ahead, the toe's tip 3 cm inside the toe rock), 0.52 wide, 0.39 high.
WORLD_SEATS = {"crown": (-0.010, 0.050, -0.095), "jaw": (0.020, -0.090, 0.010), "knuckle-l": (-0.150, -0.030, -0.010), "knuckle-r": (0.150, -0.060, -0.040)}
WORLD_MORE = {
    "toe": dict(seat=(0.000, 0.004, 0.160), across=0.215, yaw=35, seed=83, grid=4, half=(1.00, 0.66, 0.62), p=2.5, taper=0.06,
                cuts=[(0, 9.0, 0.70), (90, 0.10, 0.70), (200, 0.15, 0.80), (270, 0.05, 0.78), (330, 0.25, 0.76), (20, 0.20, 0.80),
                      (140, -0.6, 0.82), (300, -0.6, 0.80), (0, -9.0, 0.76), (60, 1.2, 0.90)],
                dent=None),
    "bridge": dict(seat=(0.000, 0.074, 0.058), across=0.218, yaw=-25, seed=97, grid=4, half=(1.00, 0.80, 0.60), p=2.5, taper=0.10,
                   cuts=[(0, 9.0, 0.66), (60, 0.20, 0.74), (150, 0.10, 0.80), (235, 0.25, 0.72), (320, 0.05, 0.80),
                         (100, 0.9, 0.82), (280, -0.5, 0.82), (0, -9.0, 0.78)],
                   dent=None),
}


def stones(world):
    """The rocks of one build: the approved set, or the world's six."""
    if not world:
        return ROCKS
    out = {name: dict(ROCKS[name], seat=seat) for name, seat in WORLD_SEATS.items()}
    out.update(WORLD_MORE)
    return out


# =====================================================================================================================
def in_blender(work, world):
    ROCKS = stones(world)        # (shadows the table above: everything below carves and bakes this build's rocks)
    stem = "dante_boulder_world" if world else "dante_boulder"
    import random

    import bmesh
    import bpy
    import numpy as np
    from mathutils import Vector

    sys.path.insert(0, str(HERE))
    import author_lagoon_rocks as lagoon          # importable with no side effects: its cuts and bevels are called
    import bake_lagoon_rock_edges as edges        # its break finder and distance field are called

    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene

    def activate(ob):
        for other in bpy.context.view_layer.objects:
            other.select_set(False)
        ob.select_set(True)
        bpy.context.view_layer.objects.active = ob

    def normal_of(turn, lean):
        if abs(lean) > 5:                         # straight up or straight down
            return Vector((0.10, 0.06, 1.0 if lean > 0 else -1.0)).normalized()
        return lagoon._plane(math.radians(turn), lean)

    def carve(name, r):
        rng = random.Random(r["seed"])
        bm = bmesh.new()
        lagoon._body(bm, rng, r["grid"], r["p"], r["half"], taper=r["taper"], amp=0.03, lobes=2)
        verts = bm.verts[:]
        for turn, lean, depth in r["cuts"]:
            verts = lagoon._cut(bm, verts, normal_of(turn, lean), depth)
        lagoon._bevel_breaks(bm, verts)
        # To its size in metres, about its own middle, and turned as it sits.
        xs, ys, zs = ([v.co[i] for v in bm.verts] for i in range(3))
        f = r["across"] / max(max(xs) - min(xs), max(ys) - min(ys))
        mid = Vector(((max(xs) + min(xs)) / 2, (max(ys) + min(ys)) / 2, (max(zs) + min(zs)) / 2))
        yaw = math.radians(r["yaw"])
        for v in bm.verts:
            q = (v.co - mid) * f
            v.co = Vector((q.x * math.cos(yaw) - q.y * math.sin(yaw), q.x * math.sin(yaw) + q.y * math.cos(yaw), q.z))
        bm.normal_update()
        me = bpy.data.meshes.new(name)
        bm.to_mesh(me)
        bm.free()
        ob = bpy.data.objects.new(name, me)
        scene.collection.objects.link(ob)
        if r["dent"]:
            # A scoop out of the face that looks that way: a squashed ball sunk into the skin there.
            turn, lean, radius = r["dent"]
            way = normal_of(turn + r["yaw"], lean)
            at = max((v.co for v in me.vertices), key=lambda c: c.dot(way))
            bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=radius, location=at + way * radius * 0.25)
            ball = bpy.context.active_object
            ball.scale = (1.0, 0.72, 0.85)
            cut = ob.modifiers.new("dent", "BOOLEAN")
            cut.operation, cut.object, cut.solver = "DIFFERENCE", ball, "EXACT"
            activate(ob)
            bpy.ops.object.modifier_apply(modifier="dent")
            bpy.data.objects.remove(ball)
            # The ball brought a UV map of its own with it, and a model's FIRST map is the one the game reads.
            while me.uv_layers:
                me.uv_layers.remove(me.uv_layers[0])
        if r.get("fracture"):
            # Where a ray that way from its middle leaves the rock, and the face it leaves by: the split lies in that face.
            turn, lean = r["fracture"]
            way = normal_of(turn + r["yaw"], lean)
            hit, at, face, _ = ob.ray_cast(way * 2.0, -way)
            if hit:
                across = face.cross(Vector((0, 0, 1))).normalized()
                up = across.cross(face).normalized()
                cutter = bmesh.new()
                # Two legs, each a thin wedge sunk two centimetres in: the upper leans one way, the lower the other.
                for rise, lean_by, half in ((0.034, 22.0, 0.050), (-0.040, -28.0, 0.042)):
                    a = math.radians(lean_by)
                    along = up * math.cos(a) + across * math.sin(a)
                    side = face.cross(along).normalized()
                    made = bmesh.ops.create_cube(cutter, size=2.0)["verts"]
                    for v in made:
                        v.co = at + up * rise + across * (0.012 if rise > 0 else -0.006) + along * (v.co.x * half) + side * (v.co.y * 0.0065) + face * (v.co.z * 0.020)
                cut_me = bpy.data.meshes.new("fracture")
                cutter.to_mesh(cut_me)
                cutter.free()
                knife = bpy.data.objects.new("fracture", cut_me)
                scene.collection.objects.link(knife)
                cut = ob.modifiers.new("fracture", "BOOLEAN")
                cut.operation, cut.object, cut.solver = "DIFFERENCE", knife, "EXACT"
                activate(ob)
                bpy.ops.object.modifier_apply(modifier="fracture")
                bpy.data.objects.remove(knife)
            else:
                print("Error: no face found for the fracture of", name)
        bm = bmesh.new()
        bm.from_mesh(me)
        bmesh.ops.dissolve_degenerate(bm, dist=1e-5, edges=bm.edges[:])
        # What lies flat is one face: the cuts and the bevels leave rows of points along straight edges that draw nothing.
        bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(3.0), verts=bm.verts[:], edges=bm.edges[:])
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        bmesh.ops.triangulate(bm, faces=bm.faces[:])
        bm.normal_update()
        sharp = lagoon._sharp_edges(bm)
        bm.to_mesh(me)
        size = [max(v.co[i] for v in bm.verts) - min(v.co[i] for v in bm.verts) for i in range(3)]
        print("CARVED %s: %d triangles, %d sharp edges, %.3f x %.3f across, %.3f high" % (name, len(bm.faces), sharp, size[0], size[1], size[2]))
        bm.free()
        return ob

    rocks = {name: carve(name, r) for name, r in ROCKS.items()}
    names = list(rocks)
    grid = max(1, math.ceil(math.sqrt(len(names))))
    for i, name in enumerate(names):
        edges.unwrap_into_cell(rocks[name].data, i, grid)      # a unique "UVBake" unwrap, in its own cell of one atlas
        # Seated for the bakes: the game's (x, y up, z toe) is Blender's (x, -z, y). Rubble stands far off, alone.
        seat = ROCKS[name]["seat"]
        rocks[name].location = (seat[0], -seat[2], seat[1]) if seat else (3.0 + i, 0.0, 0.0)

    # ---- the bakes: G the broad shoulder and B occlusion in one pass, the gaps in a second.
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 48
    scene.cycles.device = "CPU"

    def bake(label, build):
        image = bpy.data.images.new(label, SIZE, SIZE, alpha=False)
        image.generated_color = (0, 0, 1, 1)
        image.colorspace_settings.name = "Non-Color"
        m = bpy.data.materials.new(label)
        m.use_nodes = True
        nt = m.node_tree
        nt.nodes.clear()
        out = nt.nodes.new("ShaderNodeOutputMaterial")
        emit = nt.nodes.new("ShaderNodeEmission")
        nt.links.new(emit.outputs["Emission"], out.inputs["Surface"])
        nt.links.new(build(nt), emit.inputs["Color"])
        tex = nt.nodes.new("ShaderNodeTexImage")
        tex.image = image
        nt.nodes.active = tex
        for i, name in enumerate(names):
            me = rocks[name].data
            me.materials.clear()
            me.materials.append(m)
            activate(rocks[name])
            bpy.ops.object.bake(type="EMIT", margin=8, use_clear=(i == 0), uv_layer="UVBake")
        px = np.empty(SIZE * SIZE * 4, np.float32)
        image.pixels.foreach_get(px)
        return image, px.reshape(SIZE, SIZE, 4)

    def shoulder_and_occlusion(nt):
        geo = nt.nodes.new("ShaderNodeNewGeometry")
        broad = edges._edge_mask(nt, geo, 0.045, 0.998, 0.80)    # the Lagoon tool's shoulder, at this stone's size
        ao = nt.nodes.new("ShaderNodeAmbientOcclusion")
        ao.samples = 16
        ao.inputs["Distance"].default_value = 0.14               # NOT only its own: a rock leaning on it darkens it
        rgb = nt.nodes.new("ShaderNodeCombineColor")
        nt.links.new(broad, rgb.inputs["Green"])
        nt.links.new(ao.outputs["AO"], rgb.inputs["Blue"])
        return rgb.outputs["Color"]

    def gaps(nt):
        ao = nt.nodes.new("ShaderNodeAmbientOcclusion")
        ao.samples = 24
        ao.inputs["Distance"].default_value = GAP
        return ao.outputs["Color"]

    image, first = bake("shoulder-occlusion", shoulder_and_occlusion)
    _, second = bake("gaps", gaps)

    # ---- R: the true distance to the nearest plane break, by the Lagoon tool, at this atlas and this reach.
    edges.ATLAS_PX, edges.DIST_MAX = SIZE, REACH
    edges.distance_to_breaks(image, [rocks[n].data for n in names])
    px = np.empty(SIZE * SIZE * 4, np.float32)
    image.pixels.foreach_get(px)
    mask = px.reshape(SIZE, SIZE, 4).copy()
    mask[..., 1], mask[..., 2] = first[..., 1], first[..., 2]
    mask[..., 3] = 1.0 - second[..., 0]
    np.save(str(Path(work) / (stem + "_mask.npy")), mask[::-1])            # Blender's rows run bottom up; a picture's run down

    # ---- the model: each rock a node at its seat (rubble at its own middle), smooth with its sharp breaks.
    for name in names:
        rocks[name].data.materials.clear()
        if ROCKS[name]["seat"] is None:
            rocks[name].location = (0, 0, 0)
        rocks[name].select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(OUT / (stem + ".glb")), export_format="GLB", use_selection=True, export_yup=True,
                              export_apply=False, export_texcoords=True, export_normals=True, export_materials="NONE",
                              export_animations=False)
    bpy.context.view_layer.update()
    lo = [min((rocks[n].matrix_world @ v.co)[k] for n in names if ROCKS[n]["seat"] for v in rocks[n].data.vertices) for k in range(3)]
    hi = [max((rocks[n].matrix_world @ v.co)[k] for n in names if ROCKS[n]["seat"] for v in rocks[n].data.vertices) for k in range(3)]
    print("MEASURED seated: %.3f wide, %.3f high, %.3f long (from %.3f behind the sole's middle to %.3f ahead of it)" % (hi[0] - lo[0], hi[2] - lo[2], hi[1] - lo[1], hi[1], -lo[1]))
    print("wrote", OUT / (stem + ".glb"))


# =====================================================================================================================
def main():
    import uuid

    import cv2
    import numpy as np

    LOGS.mkdir(parents=True, exist_ok=True)
    OUT.mkdir(parents=True, exist_ok=True)
    world = "world" in sys.argv[1:]
    stem = "dante_boulder_world" if world else "dante_boulder"
    done = subprocess.run([BLENDER, "-b", "--python", str(Path(__file__).resolve()), "--", str(LOGS)] + (["world"] if world else []), capture_output=True, text=True)
    print("\n".join(l for l in done.stdout.splitlines() if l.startswith(("CARVED", "MEASURED", "wrote", "[rock-edges]", "Error", "Traceback"))))
    if done.returncode != 0 or "Traceback" in done.stdout + done.stderr:
        print(done.stdout[-3000:], done.stderr[-3000:])
        raise SystemExit("Blender failed")
    mask = np.clip(np.load(str(LOGS / (stem + "_mask.npy"))), 0, 1)
    out = (mask * 255 + .5).astype(np.uint8)
    path = OUT / (stem + "_mask.png")
    cv2.imwrite(str(path), cv2.cvtColor(out, cv2.COLOR_RGBA2BGRA))
    meta = Path(str(path) + ".meta")
    if not meta.exists() and OUT == MODELS / "AbilityFx":
        # The import settings of the ice crystals' masks (linear data, since R is a distance), under a name of its own.
        lines = (MODELS / "IceCrystals" / "ice_spike_mask.png.meta").read_text().splitlines()
        lines[1] = "guid: " + uuid.uuid4().hex
        meta.write_text("\n".join(lines) + "\n")
    # The four bakes side by side, for a board.
    cv2.imwrite(str(LOGS / ("bakes_world.png" if world else "bakes.png")), cv2.hconcat([cv2.resize(out[..., k], (768, 768), interpolation=cv2.INTER_AREA) for k in range(4)]))
    print("wrote %s: R covers %.1f%%, gaps cover %.1f%%" % (path.name, 100 * (mask[..., 0] > .05).mean(), 100 * (mask[..., 3] > .3).mean()))

    if world:
        return
    # The bench's stand-ins, in a file of their own so the game's model holds nothing but rock.
    sys.path.insert(0, str(HERE))
    import hand_companion_kit as kit
    from build_hands_dante import CORD, SPARE, WRAP
    m = kit.Model("dante_boulder_bench")
    m.folder = OUT
    m.add("shoe", [(kit.rounded((.085, .024, .220), .022, seg=12, rings=6), WRAP),
                   (kit.tube([(-.075, .015, .050), (0, .085, .105), (.075, .015, .050)], .018, seg=6), CORD)])
    m.add("body", [(kit.rounded((.30, .52, .20), .16, at=(0, .52, 0), seg=14, rings=8), SPARE),
                   (kit.rounded((.27, .27, .25), .12, at=(0, 1.33, 0), seg=14, rings=8), SPARE)])
    m.write()


if __name__ == "__main__":
    if "--" in sys.argv:
        in_blender(sys.argv[sys.argv.index("--") + 1], "world" in sys.argv[sys.argv.index("--") + 1:])
    else:
        main()
