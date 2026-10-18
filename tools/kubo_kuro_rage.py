"""Kuro's RAGE form remodelled: the same cat-ghost gone huge and terrible, one skin, painted. In place, on pet-nemu-ghost.glb.

Owner, 2026-10-10, of Nemu's ultimate cutscene: "nemu, the ghost needs a remodel". The rage form was faceted blocks in
flat colours: a box hood, a box back, box shoulders and palms floating beside each other. It was the one thing in the
cutscene (and the one Kuro in the game) not in the rounded, painted style the calm Kuro got from `tools/kubo_kuro.py`.

  py -3 tools/kubo_kuro_rage.py             rebuild the rage form from the kept file
  py -3 tools/kubo_kuro_rage.py --restore   put the kept file back
  py -3 tools/kubo_kuro_rage.py --review    also render the result in Blender to Logs/kuro-rage/ (several views)

THE DESIGN: calm Kuro (a rounded block with ear tufts, paws with pale pads, big pale eyes, a tiered tail going from
lavender to white) is still all there, grown wrong. The block is his head and it is nearly all MOUTH now. The ear tufts
are long and split. The eyes are the same pale lilac, shut down to burning slits under heavy brows. The paws are on long
arms and have claws. The tail is the same tiers, long and ragged, and still goes to white.

WHAT IT CHANGES, AND ONLY THIS: the GEOMETRY and PAINT of the meshes under `RageForm`.
  * No node is added, removed, renamed or moved; the rig (`RageRig`, eighteen joints), its bind pose, `RageMaw` and the
    `KuroRageInhale` animation are untouched. `KuroRagePresentation` finds `RageForm`, `RageMaw`, the renderers named
    `RageEye*` and `*Fang*`; `KuroFormTests` reads `RageHood_part0`'s material colour and the form's size. All hold: the
    new form stands inside the old one's bounds (printed at the end of a run) and its mouth is where `RageMaw` is.
  * ONE SKIN where a creature has one. Head, brows, ears, back, shoulders, arms, palms and fingers are typed forms FUSED in
    Blender by a voxel remesh, with the mouth cut out of the result, then weighted to the old joints. That skin is the
    mesh of `RageHood_part0`. The tail is its own skin (`RageLowerSpirit_part0`) because it wears another shader.
  * Hard things stay separate, as they are on a creature: fangs (the five `Rage*Fang*_part0`), claws (the six
    `RageFinger*_part0`), eye slits (`RageEyeLeft/Right`), pads (`RagePalm*_part0`), the insides of the ears
    (`RageHoodWisp_part0`), and the ember in his throat (`RageMawCavity`).
  * Every other rage mesh had been a block that the skin now covers. Each keeps its node and renderer and holds one
    speck of a triangle inside the body, so nothing that walks the form finds a part missing.
  * PAINT: an atlas embedded in the file, and two new materials that carry it (`Kuro rage painted`, tinted violet so the
    hood's material still reads purple to the test; `Kuro rage tail`, untinted, because the tail goes to white). All paint
    is in the atlas's upper half, where `TumbangPreso/Toon` reads the texture even when a palette is on.
  * THE TAIL IS BUILT TO FADE. Its points carry an alpha down its length (1 under the body, 0 at the tip) and its UVs
    are flat cells, one a tier, exactly as calm Kuro's tail is made, so `Shaders/KuroTail` can thin it out in halftone
    dots. The cutscene dresses it so. ⚠️ The live game does not yet: `GhostPetCompanion.DressTail` only looks under
    `RestoredCalm`. Until it is taught `RageLowerSpirit_part0`, the live giant's tail is solid, painted, and whole.

⚠️ `tools/kubo_kuro.py` rebuilds the model from ITS kept original, which has the old rage form in it. Run this after it.

The kept file is ArtSource/kuro/pet-nemu-ghost.pre-rage.glb (made the first time this runs); every run rebuilds from it.
"""
import json
import math
import shutil
import struct
import subprocess
import sys
from pathlib import Path

import numpy as np

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent
sys.path.insert(0, str(HERE))
from hand_companion_kit import ellipsoid, join, lathe, mirror, moved, rounded, tube  # noqa: E402

GLB = ROOT / "Assets/TumbangPreso/Art/characters/pets/pet-nemu-ghost.glb"
KEPT = ROOT / "ArtSource/kuro/pet-nemu-ghost.pre-rage.glb"
LOGS = ROOT / "Logs/kuro-rage"
BLENDER = r"C:\Program Files\Blender Foundation\Blender 5.0\blender.exe"
try:
    import bpy
    INSIDE_BLENDER = True
except ImportError:
    INSIDE_BLENDER = False


def arg(name, default=None):
    return next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--" + name + "=")), default)


def smooth01(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


# ------------------------------------------------------------------ the rig, as it is bound (hundredths of the model's
# own unit, y up, +z his face, -x his left). Read off the kept file's inverse bind matrices; typed here so the forms
# below can be typed against them. `main` checks them against the file and refuses to build if the rig has moved.
J = {
    "RageChest": (0.0, 9.94, -7.16), "RageHead": (0.0, 22.01, -4.44), "RageJaw": (0.0, 17.57, -2.88),
    "RageArmLeft": (-4.28, 20.84, -7.01), "RageForearmLeft": (-8.25, 19.29, -5.45), "RageHandLeft": (-10.90, 17.50, -1.32),
    "RageFingerLeft0Joint": (-11.33, 18.83, -0.79), "RageFingerLeft1Joint": (-12.89, 17.71, -0.62), "RageFingerLeft2Joint": (-10.17, 16.94, -1.31),
    "RageArmRight": (5.61, 20.61, -7.32), "RageForearmRight": (8.72, 18.12, -5.61), "RageHandRight": (10.90, 15.16, -1.87),
    "RageFingerRight0Joint": (11.52, 16.18, -1.11), "RageFingerRight1Joint": (10.06, 15.06, -1.67), "RageFingerRight2Joint": (12.80, 14.29, -1.05),
    "RageWispA": (0.0, 9.94, -7.16), "RageWispB": (0.0, 6.05, -7.63), "RageWispC": (1.40, 3.32, -7.32),
}
V = {k: np.array(v, np.float32) for k, v in J.items()}
MAW = (0.0, 17.34, 0.0)                      # where `RageMaw` rests: the middle of the mouth's opening
OLD_LO, OLD_HI = np.array([-15.45, 0.45, -12.39]), np.array([15.44, 29.01, 3.24])   # the old form's bounds

# The block that is his head: where it is, how big, how round.
HEAD_AT, HEAD_HALF, HEAD_ROUND = (0.0, 19.6, -4.3), (5.5, 7.0, 5.2), 3.3
# The mouth that is cut out of it: nearly the whole face, its corners turned UP (he is enjoying this).
MOUTH_AT, MOUTH_HALF, MOUTH_ROUND, MOUTH_GRIN = (0.0, 17.2, -0.9), (4.25, 3.55, 4.1), 2.0, 1.0
EYE = (2.75, 23.35)                           # the right eye; the left is its mirror


def finger_way(side, i):
    hand = V["RageHand" + side]
    d = V["RageFinger%s%dJoint" % (side, i)] - hand
    return hand, d / np.linalg.norm(d)


# ------------------------------------------------------------------ the forms (typed), before they are fused

def ear(lean):
    """An ear tuft: calm Kuro's fat cone, drawn out long, with a second shorter point split off its outer side."""
    main = lathe([(2.05, -1.2), (2.0, 0.0), (1.55, 1.3), (1.0, 2.5), (0.42, 3.4), (0.0, 3.75)], seg=16, squash_z=0.78)
    split = moved(lathe([(1.0, -0.6), (0.9, 0.0), (0.6, 0.9), (0.24, 1.7), (0.0, 2.0)], seg=12, squash_z=0.8), turn=(0, 0, -34), at=(1.25, 0.5, 0.0))
    return moved(join(main, split), turn=(0, 0, lean))


def skin_forms():
    """Everything that becomes his one skin, as closed forms that overlap. The right side is typed where the rig is the
    same on both sides (the head); the arms are typed each to its own joints, because the rig's pose is not mirrored."""
    forms = [rounded(HEAD_HALF, HEAD_ROUND, HEAD_AT, seg=36, rings=20)]
    # The brows: heavy, low at their inner ends. Most of the anger is here.
    brow = moved(ellipsoid((2.5, 0.95, 1.25), seg=14, rings=8), turn=(0, 0, 17), at=(2.75, 24.75, 0.25))
    forms += [brow, mirror(brow)]
    # The ears, where calm Kuro's are, long and split.
    e = moved(ear(-17), at=(3.25, 25.3, -4.7))
    forms += [e, mirror(e)]
    # A ruff at each cheek: three points, down and out. A cat's, gone ragged.
    for at, turn, length in [((5.2, 18.6, -3.2), -112, 2.6), ((5.0, 16.4, -3.6), -134, 2.9), ((4.4, 14.4, -4.2), -152, 2.4)]:
        point = moved(lathe([(1.15, -0.8), (1.05, 0.0), (0.7, length * 0.45), (0.3, length * 0.82), (0.0, length)], seg=12, squash_z=0.8), turn=(0, 0, turn), at=at)
        forms += [point, mirror(point)]
    # His back: a hunch behind the head that the arms hang from.
    forms.append(ellipsoid((6.3, 5.4, 4.3), at=(0.0, 19.3, -8.0), seg=28, rings=16))
    # The hem of him: five ragged points hanging where the block ends and the tail begins.
    for x, z, length, lean in [(-4.3, -6.6, 2.7, 16), (-2.1, -8.6, 3.5, 7), (0.3, -9.3, 2.9, -3), (2.5, -8.4, 3.7, -9), (4.4, -6.4, 2.5, -18)]:
        forms.append(moved(lathe([(1.5, -1.6), (1.4, 0.0), (0.95, length * 0.45), (0.4, length * 0.82), (0.0, length)], seg=12, squash_z=0.75),
                           turn=(180, 0, lean), at=(x, 14.3, z)))
    for side in ("Left", "Right"):
        shoulder, elbow, hand = V["RageArm" + side], V["RageForearm" + side], V["RageHand" + side]
        sign = -1.0 if side == "Left" else 1.0
        forms.append(ellipsoid((3.1, 2.9, 2.9), at=shoulder + np.array([sign * 1.0, 0.2, -0.2]), seg=18, rings=12))
        # The arm: thick from the shoulder, thinner to the wrist, and a tuft at the elbow.
        forms.append(tube([shoulder, (shoulder + elbow) / 2 + np.array([0, -0.3, -0.5]), elbow, (elbow + hand) / 2 + np.array([sign * 0.3, 0, -0.2]), hand],
                          [2.5, 2.3, 2.05, 1.85, 1.75], seg=16))
        back = elbow - hand
        back /= np.linalg.norm(back)
        tuft = lathe([(1.0, -0.6), (0.9, 0.0), (0.55, 1.1), (0.2, 1.9), (0.0, 2.2)], seg=10)
        forms.append(moved(tuft, turn=(-70, 0, sign * 35), at=elbow + back * 0.8 + np.array([0, -0.6, -1.2])))
        # The paw: a round palm, and three fingers out along the rig's own finger joints.
        ways = [finger_way(side, i)[1] for i in range(3)]
        mid = sum(ways) / 3
        forms.append(ellipsoid((2.25, 2.0, 1.95), at=hand + mid * 0.7, seg=16, rings=10))
        for d in ways:
            forms.append(tube([hand + d * 0.5, hand + d * 1.8, hand + d * 2.9 + np.array([0, 0, 0.25])], [1.1, 0.98, 0.8], seg=12))
    return forms


def mouth_cutter():
    p, n, t = rounded(MOUTH_HALF, MOUTH_ROUND, (0, 0, 0), seg=32, rings=16)
    p = p.copy()
    p[:, 1] += MOUTH_GRIN * (p[:, 0] / MOUTH_HALF[0]) ** 2
    return p + np.array(MOUTH_AT, np.float32), n, t


# The tail: the line it hangs along, how thick it is there. It starts inside the block, BEHIND and BELOW the mouth's cut:
# the first build's tail began higher and its top stood up through the floor of his mouth like a tongue.
TAIL_LINE = [(0.0, 12.0, -8.6), (0.0, 10.6, -8.6), (-0.5, 9.0, -8.4), (-1.2, 7.2, -8.0), (-0.9, 5.3, -7.5), (0.4, 3.6, -6.9), (1.7, 2.3, -6.3), (1.9, 1.3, -5.5), (1.2, 0.85, -4.9)]
TAIL_THICK = [3.4, 3.7, 3.5, 3.0, 2.4, 1.8, 1.2, 0.7, 0.3]
# Its tiers, top to bottom: the height each begins at, and its colour. Calm Kuro's tail in the same order.
TIERS = [(99.0, "#3B2069"), (11.8, "#55309A"), (9.6, "#7A52C6"), (7.4, "#A483E6"), (5.2, "#CDB8F7"), (3.1, "#F4EEFF")]
EMBER_CELL = 7                                # the hot cell, for the ember in his throat
CELLS = [c for _, c in TIERS] + ["#F4EEFF", "#B98CFF"]


def tail_forms():
    forms = [tube(TAIL_LINE, TAIL_THICK, seg=20)]
    # Tatters off it: each a point torn loose, where a tier ends.
    for at, turn, length, thick in [((-3.0, 10.4, -8.4), (0, 0, 138), 2.6, 1.2), ((2.9, 8.6, -8.2), (0, 0, -142), 2.9, 1.1), ((-2.9, 6.4, -7.6), (10, 0, 150), 2.2, 0.9),
                                    ((1.9, 4.9, -7.9), (-20, 0, -150), 1.9, 0.75), ((0.0, 8.2, -10.4), (-140, 0, 0), 2.0, 1.0)]:
        forms.append(moved(lathe([(thick, -0.8), (thick * 0.92, 0.0), (thick * 0.6, length * 0.45), (thick * 0.25, length * 0.82), (0.0, length)], seg=10), turn=turn, at=at))
    return forms


# ------------------------------------------------------------------ Blender: fuse, lay UVs; or look at the result

if INSIDE_BLENDER:

    def to_object(name, mesh):
        p, _, t = mesh
        me = bpy.data.meshes.new(name)
        me.from_pydata([tuple(float(v) for v in row) for row in p], [], [tuple(int(v) for v in row) for row in t])
        me.update()
        ob = bpy.data.objects.new(name, me)
        bpy.context.scene.collection.objects.link(ob)
        return ob

    def apply(ob, kind, **settings):
        bpy.context.view_layer.objects.active = ob
        for o in bpy.context.scene.objects:
            o.select_set(o is ob)
        m = ob.modifiers.new(kind, kind)
        for key, value in settings.items():
            setattr(m, key, value)
        bpy.ops.object.modifier_apply(modifier=m.name)

    def fused(name, forms, voxel, cutter=None, keep=0.5, unwrap=False):
        obs = [to_object("%s-%d" % (name, i), f) for i, f in enumerate(forms)]
        for o in bpy.context.scene.objects:
            o.select_set(o in obs)
        bpy.context.view_layer.objects.active = obs[0]
        bpy.ops.object.join()
        ob = bpy.context.view_layer.objects.active
        apply(ob, "REMESH", mode="VOXEL", voxel_size=voxel, adaptivity=0.0)
        apply(ob, "SMOOTH", factor=0.5, iterations=6)
        if cutter is not None:
            cut = to_object(name + "-cut", cutter)
            apply(ob, "BOOLEAN", operation="DIFFERENCE", object=cut, solver="EXACT")
            bpy.data.objects.remove(cut)
            apply(ob, "REMESH", mode="VOXEL", voxel_size=voxel, adaptivity=0.0)
        apply(ob, "SMOOTH", factor=0.5, iterations=8)
        apply(ob, "DECIMATE", ratio=keep)
        apply(ob, "TRIANGULATE")
        me = ob.data
        for poly in me.polygons:
            poly.use_smooth = True
        me.update()
        uv = np.zeros((len(me.polygons) * 3, 2), np.float32)
        if unwrap:
            bpy.context.view_layer.objects.active = ob
            ob.select_set(True)
            bpy.ops.object.mode_set(mode="EDIT")
            bpy.ops.mesh.select_all(action="SELECT")
            bpy.ops.uv.smart_project(angle_limit=math.radians(72), island_margin=0.006)
            bpy.ops.object.mode_set(mode="OBJECT")
            layer = me.uv_layers.active.data
            for poly in me.polygons:
                for k, loop in enumerate(poly.loop_indices):
                    uv[poly.index * 3 + k] = layer[loop].uv[:]
        pos = np.array([v.co[:] for v in me.vertices], np.float32)
        nrm = np.array([v.normal[:] for v in me.vertices], np.float32)
        tri = np.array([p.vertices[:] for p in me.polygons], np.uint32)
        bpy.data.objects.remove(ob)
        return pos, nrm, tri, uv

    if arg("fuse"):
        bpy.ops.wm.read_factory_settings(use_empty=True)
        s = fused("skin", skin_forms(), 0.26, cutter=mouth_cutter(), keep=0.30, unwrap=True)
        t = fused("tail", tail_forms(), 0.22, keep=0.35)
        np.savez(arg("fuse"), sp=s[0], sn=s[1], st=s[2], suv=s[3], tp=t[0], tn=t[1], tt=t[2])
        print("fused: skin %d triangles, tail %d triangles" % (len(s[2]), len(t[2])))
        raise SystemExit

    if arg("look"):
        # The finished file, as a glTF reader sees it: the rage form alone, at rest, in its own paint.
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.gltf(filepath=arg("look"))
        seen = bpy.data.images.load(arg("seen"))
        for material in bpy.data.materials:
            if material.node_tree:
                for node in material.node_tree.nodes:
                    if node.type == "TEX_IMAGE":
                        node.image = seen
        for ob in list(bpy.context.scene.objects):
            names = []
            walk = ob
            while walk is not None:
                names.append(walk.name)
                walk = walk.parent
            if not any(n.startswith("Rage") for n in names):
                ob.hide_render = True
        scene = bpy.context.scene
        scene.render.engine = "BLENDER_WORKBENCH"
        scene.display.shading.light = "STUDIO"
        scene.display.shading.color_type = "TEXTURE"
        scene.display.shading.show_object_outline = True
        scene.display.shading.show_cavity = False
        scene.render.resolution_x = scene.render.resolution_y = 900
        scene.render.film_transparent = False
        scene.world = bpy.data.worlds.new("w")
        scene.world.color = (0.36, 0.38, 0.46)
        cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
        scene.collection.objects.link(cam)
        scene.camera = cam
        cam.data.lens = 70
        from mathutils import Vector
        # The importer stands the model up on Blender's z: glTF (x, y, z) is Blender (x, -z, y).
        target = Vector((0.0, 0.055, 0.155))
        for name, way in [("front", (0, -1, 0.10)), ("quarter", (0.75, -1, 0.30)), ("side", (1, -0.05, 0.10)), ("back", (-0.5, 1, 0.25)), ("under", (0.35, -1, -0.55)), ("mouth", (0.12, -1, -0.05))]:
            d = Vector(way).normalized()
            cam.location = target + d * (0.5 if name == "mouth" else 0.82)
            cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()
            scene.render.filepath = arg("shots") + "_" + name + ".png"
            bpy.ops.render.render(write_still=True)
        print("looked")
        raise SystemExit


# ------------------------------------------------------------------ weights

# A bone is the line from one joint toward the next thing it carries, and how far its hold reaches.
def bones():
    out = [("RageChest", V["RageChest"] + np.array([0, 6.0, 0]), np.array([0, 20.5, -8.4], np.float32), 5.2),
           ("RageHead", V["RageHead"] + np.array([0, -2.5, 0]), np.array([0, 26.5, -4.4], np.float32), 6.0)]
    for side in ("Left", "Right"):
        out.append(("RageArm" + side, V["RageArm" + side], V["RageForearm" + side], 2.5))
        out.append(("RageForearm" + side, V["RageForearm" + side], V["RageHand" + side], 2.0))
        hand, _ = finger_way(side, 0)
        mid = sum(finger_way(side, i)[1] for i in range(3)) / 3
        out.append(("RageHand" + side, hand, hand + mid * 1.2, 1.7))
        for i in range(3):
            _, d = finger_way(side, i)
            out.append(("RageFinger%s%dJoint" % (side, i), hand + d * 1.5, hand + d * 3.4, 0.85))
    return out


def distance_to_segment(p, a, b):
    ab = b - a
    t = np.clip(((p - a) @ ab) / float(ab @ ab), 0, 1)
    return np.linalg.norm(p - (a + t[:, None] * ab), axis=1)


def skin_weights(pos, order):
    """Each point is held by the bones whose lines it is near, the nearer the more, and the mouth's lower half by the jaw."""
    names = [b[0] for b in bones()]
    hold = np.stack([np.exp(-(distance_to_segment(pos, a, b) / reach) ** 2) for _, a, b, reach in bones()], axis=1)
    # An arm's bones do not reach into the block: a point of the head that happens to be near a shoulder stays the head's.
    arm = np.array([not n.startswith(("RageChest", "RageHead")) for n in names])
    inside = smooth01(6.6, 5.2, np.abs(pos[:, 0]))
    hold[:, arm] *= (1 - 0.85 * inside)[:, None]
    hold /= hold.sum(axis=1, keepdims=True)
    # The jaw: the lower lip, the chin and the floor of the mouth, fading out up the cheeks.
    jaw = smooth01(17.4, 15.2, pos[:, 1]) * smooth01(-6.6, -3.6, pos[:, 2]) * smooth01(6.2, 5.0, np.abs(pos[:, 0]))
    hold *= (1 - jaw)[:, None]
    names = names + ["RageJaw"]
    hold = np.concatenate([hold, jaw[:, None]], axis=1)
    top = np.argsort(-hold, axis=1)[:, :4]
    w = np.take_along_axis(hold, top, axis=1)
    w /= w.sum(axis=1, keepdims=True)
    joint = np.array([order[n] for n in names])[top]
    joint[w < 1e-4] = 0
    w[w < 1e-4] = 0
    w /= w.sum(axis=1, keepdims=True)
    return joint.astype(np.uint8), w.astype(np.float32)


def tail_weights(pos, order):
    """Down the tail: the chest holds its top, then each of the three wisp joints in turn."""
    y = pos[:, 1]
    stops = [("RageChest", 12.0), ("RageWispA", 9.0), ("RageWispB", 5.6), ("RageWispC", 2.6)]
    joint = np.zeros((len(pos), 4), np.uint8)
    w = np.zeros((len(pos), 4), np.float32)
    for k, (name, _) in enumerate(stops):
        joint[:, k] = order[name]
    # Plainly: between two stops, the upper joint's share falls from 1 to 0 on the way down.
    for k in range(len(stops) - 1):
        hi, lo = stops[k][1], stops[k + 1][1]
        share = smooth01(lo, hi, y)
        band = (y <= hi) & (y > lo)
        w[band, k] = share[band]
        w[band, k + 1] = 1 - share[band]
    w[y > stops[0][1], 0] = 1
    w[y <= stops[-1][1], len(stops) - 1] = 1
    return joint, w


def rigid(count, order, name):
    joint = np.zeros((count, 4), np.uint8)
    joint[:, 0] = order[name]
    w = np.zeros((count, 4), np.float32)
    w[:, 0] = 1
    return joint, w


# ------------------------------------------------------------------ the paint

ATLAS = 2048
R_SKIN = (0.004, 0.004, 0.996, 0.36)          # the skin's islands, in the atlas (u0, v0, u1, v1; v down, as glTF has it)
CELL_V = 0.40625                               # the row of flat cells: Unity row 9, which every toon shader here reads as slots 0 to 7
TINT = np.array([1.0, 0.58, 1.0])             # the painted material's colour; the paint is stored divided by it


def hexc(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4)])


def to_linear(c):
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def to_srgb(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - 0.055)


def cell_uv(slot):
    return np.array([(slot * 2 + 1) / 16.0, CELL_V], np.float32)


def mouth_depth(p):
    """How far inside the mouth's cut a point is: under 0 inside it. The cutter's own shape, read back."""
    q = p - np.array(MOUTH_AT)
    q = np.stack([q[..., 0], q[..., 1] - MOUTH_GRIN * (q[..., 0] / MOUTH_HALF[0]) ** 2, q[..., 2]], axis=-1)
    d = np.abs(q) - (np.array(MOUTH_HALF) - MOUTH_ROUND)
    return np.linalg.norm(np.maximum(d, 0), axis=-1) + np.minimum(d.max(axis=-1), 0) - MOUTH_ROUND


def paint_skin(p, n, hollow):
    """The colour of his skin at a point, from where the point is and which way it faces. Painted in FLAT STEPS with a
    wandering edge, as a brush leaves them, not in a gradient."""
    x, y, z = p[..., 0], p[..., 1], p[..., 2]
    wander = 0.045 * np.sin(x * 1.9 + y * 2.3) * np.sin(z * 2.1 - y * 1.3) + 0.03 * np.sin(x * 4.7 - z * 3.9 + y * 4.1)
    tone = 0.50 + 0.26 * n[..., 1] + 0.10 * n[..., 2] - 1.5 * hollow + wander
    # Lighter across the face, as calm Kuro's is the lightest thing on him.
    tone += 0.07 * smooth01(0.3, 0.8, n[..., 2]) * smooth01(6.5, 4.5, np.abs(x))
    # Soaked dark at the hem and down the points that hang from it, and at the tips of his ears.
    tone -= 0.26 * smooth01(15.0, 12.2, y)
    tone -= 0.16 * smooth01(27.0, 28.8, y)
    # A tabby's bars over the back of his head and his hunch, wandering, and two on each forearm.
    back = smooth01(-0.1, -0.5, n[..., 2]) + smooth01(0.45, 0.8, n[..., 1]) * smooth01(-3.0, -5.5, z)
    for at in (24.2, 21.4, 18.6, 15.9):
        tone -= 0.17 * np.clip(back, 0, 1) * np.exp(-((y - at - 0.5 * np.sin(x * 0.9 + at)) / 0.55) ** 2) * smooth01(7.0, 5.6, np.abs(x))
    for side in ("Left", "Right"):
        elbow, hand = V["RageForearm" + side], V["RageHand" + side]
        along = ((p - elbow) @ (hand - elbow)) / float((hand - elbow) @ (hand - elbow))
        near = np.exp(-(np.linalg.norm(p - (elbow + np.clip(along, 0, 1)[..., None] * (hand - elbow)), axis=-1) / 3.0) ** 4)
        for at in (0.28, 0.62):
            tone -= 0.18 * near * np.exp(-((along - at) / 0.075) ** 2)
        # Toward the claws the fingers go dark.
        tone -= 0.22 * smooth01(1.6, 3.3, np.linalg.norm(p - hand, axis=-1)) * smooth01(4.5, 3.0, np.linalg.norm(p - hand, axis=-1) - 1.0)
    # The steps.
    ramp = [hexc(h) for h in ("#190930", "#27104A", "#371B64", "#472680", "#5A359C", "#6E48B4")]
    edges = [0.20, 0.36, 0.52, 0.66, 0.80]
    c = np.zeros(p.shape[:-1] + (3,)) + ramp[0]
    for edge, colour in zip(edges, ramp[1:]):
        c += (colour - c) * smooth01(edge - 0.012, edge + 0.012, tone)[..., None]
    # The sockets: dark smudged round each slit, and the slit's own light on the skin beside it.
    for side in (1, -1):
        d = np.sqrt(((x - side * EYE[0]) / 1.55) ** 2 + ((y - EYE[1] - 0.25 * side * (x - side * EYE[0])) / 0.8) ** 2) + 3 * smooth01(0.2, -1.0, z)
        c += (hexc("#12061F") - c) * smooth01(1.75, 1.15, d)[..., None]
        c += (hexc("#B98CFF") - c) * (0.75 * smooth01(1.3, 0.95, d))[..., None]
    # The inside of his mouth: it GLOWS. Dark at the lips, hot down the throat, hottest at the back and the middle.
    depth = mouth_depth(p)
    inside = smooth01(0.12, -0.18, depth) * smooth01(1.1, 0.6, z)
    far = smooth01(0.4, -4.6, z)
    heat = np.clip(far ** 1.3 * (1.15 - 0.45 * (x / MOUTH_HALF[0]) ** 2) + 0.05 * np.sin(y * 3.1 + x * 2.3), 0, 1)
    # ⚠️ VIOLET, AND NEVER NEAR WHITE. The first throat went up to a pale pink: the lens goes down it at the end of Nemu's
    # cutscene, and the picture went pink-white for three frames. Pink is Nemu's own colour and is on nothing of his.
    throat = [hexc(h) for h in ("#1C0A33", "#34126A", "#4F1C9A", "#6C2CC4", "#8A47E0", "#A56BF2")]
    g = np.zeros_like(c) + throat[0]
    for edge, colour in zip([0.12, 0.30, 0.50, 0.70, 0.88], throat[1:]):
        g += (colour - g) * smooth01(edge - 0.015, edge + 0.015, heat)[..., None]
    # A lit lip: the glow catching the edge of the cut.
    c += (hexc("#8A55D8") - c) * (0.6 * smooth01(0.9, 0.15, np.abs(depth)) * smooth01(-0.6, 0.6, z) * (1 - inside))[..., None]
    return c + (g - c) * inside[..., None]


def hollows(pos, tri):
    """How far each point sits in a fold (above 0) or on a ridge (below): what a painter darkens and lightens."""
    count = len(pos)
    total = np.zeros((count, 3)); number = np.zeros(count)
    for a, b in ((0, 1), (1, 2), (2, 0)):
        np.add.at(total, tri[:, a], pos[tri[:, b]]); np.add.at(number, tri[:, a], 1)
        np.add.at(total, tri[:, b], pos[tri[:, a]]); np.add.at(number, tri[:, b], 1)
    mean = total / np.maximum(number, 1)[:, None]
    face = np.cross(pos[tri[:, 1]] - pos[tri[:, 0]], pos[tri[:, 2]] - pos[tri[:, 0]])
    nrm = np.zeros((count, 3))
    for k in range(3):
        np.add.at(nrm, tri[:, k], face)
    nrm /= np.maximum(np.linalg.norm(nrm, axis=1, keepdims=True), 1e-9)
    h = ((mean - pos) * nrm).sum(axis=1)
    for _ in range(3):
        t = np.zeros(count)
        for a, b in ((0, 1), (1, 2), (2, 0)):
            np.add.at(t, tri[:, a], h[tri[:, b]]); np.add.at(t, tri[:, b], h[tri[:, a]])
        h = 0.5 * h + 0.5 * t / np.maximum(number, 1)
    return np.clip(h, -0.12, 0.12)


def paint_atlas(pos, nrm, tri, uv, hollow):
    """Paint the skin's islands by laying every triangle into the atlas and asking `paint_skin` what colour each texel of
    it is, by where on him that texel lies. The paint is a function of the place, so no island's edge shows."""
    import cv2
    size = ATLAS
    where = np.zeros((size, size, 3), np.float32)
    facing = np.zeros((size, size, 3), np.float32)
    fold = np.zeros((size, size), np.float32)
    done = np.zeros((size, size), bool)
    px = uv * size
    for k in range(len(tri)):
        a, b, c = px[k * 3], px[k * 3 + 1], px[k * 3 + 2]
        x0, x1 = int(max(0, math.floor(min(a[0], b[0], c[0])) - 1)), int(min(size - 1, math.ceil(max(a[0], b[0], c[0])) + 1))
        y0, y1 = int(max(0, math.floor(min(a[1], b[1], c[1])) - 1)), int(min(size - 1, math.ceil(max(a[1], b[1], c[1])) + 1))
        area = (b[0] - a[0]) * (c[1] - a[1]) - (c[0] - a[0]) * (b[1] - a[1])
        if abs(area) < 1e-9 or x1 < x0 or y1 < y0:
            continue
        gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
        w1 = ((gx - a[0]) * (c[1] - a[1]) - (c[0] - a[0]) * (gy - a[1])) / area
        w2 = ((b[0] - a[0]) * (gy - a[1]) - (gx - a[0]) * (b[1] - a[1])) / area
        w0 = 1 - w1 - w2
        hit = (w0 >= -0.02) & (w1 >= -0.02) & (w2 >= -0.02)
        if not hit.any():
            continue
        i0, i1, i2 = tri[k]
        sy, sx = np.nonzero(hit)
        ww = np.stack([w0[hit], w1[hit], w2[hit]], axis=1)
        where[y0 + sy, x0 + sx] = ww @ np.stack([pos[i0], pos[i1], pos[i2]])
        facing[y0 + sy, x0 + sx] = ww @ np.stack([nrm[i0], nrm[i1], nrm[i2]])
        fold[y0 + sy, x0 + sx] = ww @ np.array([hollow[i0], hollow[i1], hollow[i2]])
        done[y0 + sy, x0 + sx] = True
    facing /= np.maximum(np.linalg.norm(facing, axis=2, keepdims=True), 1e-6)
    seen = paint_skin(where.astype(np.float64), facing.astype(np.float64), fold.astype(np.float64))
    stored = to_srgb(to_linear(seen) / to_linear(TINT))
    img = np.zeros((size, size, 3), np.float32)
    img[done] = stored[done]
    # Carry each island's edge outward, so a texel sampled just off it is still paint.
    mask = done.astype(np.uint8)
    kernel = np.ones((3, 3), np.uint8)
    for _ in range(10):
        grown = cv2.dilate(img, kernel)
        wider = cv2.dilate(mask, kernel)
        new = (wider > 0) & (mask == 0)
        img[new] = grown[new]
        mask = wider
    img[mask == 0] = to_srgb(to_linear(hexc("#371B64")) / to_linear(TINT))
    # The flat cells: the tail's tiers and the ember. Unity row 9; glTF rows 6 and 7 of sixteen, counted from the top.
    y0, y1 = int(size * 6 / 16), int(size * 7 / 16)
    for slot, colour in enumerate(CELLS):
        img[y0:y1, int(size * slot / 8):int(size * (slot + 1) / 8)] = hexc(colour)
    ok, data = cv2.imencode(".png", cv2.cvtColor((np.clip(img, 0, 1) * 255 + 0.5).astype(np.uint8), cv2.COLOR_RGB2BGR))
    assert ok
    # The same picture as it will be SEEN, the tint put back, for the review: a viewer that ignores a material's colour
    # showed the stored atlas, and he looked navy with a green mouth.
    shown = img.copy()
    shown[:y0] = to_srgb(to_linear(img[:y0]) * to_linear(TINT))
    cv2.imwrite(str(LOGS / "kuro-rage-atlas-seen.png"), cv2.cvtColor((np.clip(shown, 0, 1) * 255 + 0.5).astype(np.uint8), cv2.COLOR_RGB2BGR))
    return data.tobytes(), int(done.sum())


# ------------------------------------------------------------------ the hard parts (each typed; the left is the right's mirror
# where the rig is the same on both sides)

def fang(length, thick, lean_out, curl):
    """A cat's fang, point down: round, curved back a little, never a triangle."""
    p, n, t = lathe([(thick * 0.9, 0.5), (thick, 0.0), (thick * 0.86, -length * 0.3), (thick * 0.58, -length * 0.62), (thick * 0.26, -length * 0.88), (0.0, -length)][::-1], seg=12)
    p = p.copy()
    down = np.clip(-p[:, 1] / length, 0, 1)
    p[:, 2] -= curl * down ** 2
    p[:, 0] += lean_out * down ** 1.5
    return p, n, t


def hard_parts(front):
    """name -> (mesh, joint it rides, material: an old material's name, or TAIL for the atlas's flat cells)."""
    parts = {}
    # The eyes: calm Kuro's pale ovals, shut down to slits, their inner ends low.
    slit = moved(ellipsoid((1.55, 0.50, 0.38), seg=18, rings=8), turn=(0, 0, 14), at=(EYE[0], EYE[1], front + 0.12))
    parts["RageEyeRight"] = (slit, "RageHead", None)
    parts["RageEyeLeft"] = (mirror(slit), "RageHead", None)
    # Two great fangs above, three below (the middle one small), in the old ones' places.
    upper = moved(fang(3.5, 0.95, 0.25, 0.5), at=(2.55, 20.9, 0.15))
    parts["RageUpperFang1_part0"] = (upper, "RageHead", None)
    parts["RageUpperFang0_part0"] = (mirror(upper), "RageHead", None)
    lower = moved(fang(2.6, 0.85, -0.2, 0.35), turn=(180, 0, 0), at=(2.55, 13.75, 0.2))
    parts["RageLowerFang2_part0"] = (lower, "RageJaw", None)
    parts["RageLowerFang0_part0"] = (mirror(lower), "RageJaw", None)
    parts["RageLowerFang1_part0"] = (moved(fang(1.7, 0.7, 0.0, 0.25), turn=(180, 0, 0), at=(0.0, 13.65, 0.35)), "RageJaw", None)
    # A claw on every finger: curved toward what he is reaching for.
    for side in ("Left", "Right"):
        for i in range(3):
            hand, d = finger_way(side, i)
            tip = hand + d * 2.9 + np.array([0, 0, 0.25])
            hook = np.array([0, -0.15, 1.0])
            line = [tip - d * 0.35, tip + d * 0.55 + hook * 0.08, tip + d * 1.15 + hook * 0.34, tip + d * 1.5 + hook * 0.72]
            parts["RageFinger%s%d_part0" % (side, i)] = (tube(line, [0.66, 0.56, 0.36, 0.1], seg=10), "RageFinger%s%dJoint" % (side, i), "Block ivory")
        # The pad of the paw, pale, as calm Kuro's are.
        hand = V["RageHand" + side]
        mid = sum(finger_way(side, i)[1] for i in range(3)) / 3
        parts["RagePalm%s_part0" % side] = (ellipsoid((0.8, 0.68, 0.4), at=hand + mid * 0.7 + np.array([0, -0.1, 1.72]), seg=12, rings=8), "RageHand" + side, "Block ivory")
    # The insides of his ears, lavender, on their fronts.
    inner = moved(ellipsoid((0.95, 1.5, 0.32), seg=12, rings=8), turn=(0, 0, -17), at=(3.55, 26.5, -3.72))
    parts["RageHoodWisp_part0"] = (join(inner, mirror(inner)), "RageHead", None)
    # The ember at the back of his throat.
    parts["RageMawCavity"] = (ellipsoid((1.7, 1.45, 0.7), at=(0.0, 17.5, -4.75), seg=18, rings=10), "RageHead", "TAIL")
    return parts


# ------------------------------------------------------------------ the file

def read(path):
    data = path.read_bytes()
    n = struct.unpack_from("<I", data, 12)[0]
    doc = json.loads(data[20:20 + n])
    bn = struct.unpack_from("<I", data, 20 + n)[0]
    return doc, bytearray(data[28 + n:28 + n + bn])


def write(path, doc, blob_):
    while len(blob_) % 4:
        blob_.append(0)
    doc["buffers"][0]["byteLength"] = len(blob_)
    text = json.dumps(doc, separators=(",", ":")).encode()
    text += b" " * (-len(text) % 4)
    out = struct.pack("<4sII", b"glTF", 2, 28 + len(text) + len(blob_)) + struct.pack("<I4s", len(text), b"JSON") + text \
        + struct.pack("<I4s", len(blob_), b"BIN\0") + bytes(blob_)
    path.write_bytes(out)


def accessor_array(doc, blob_, index):
    a = doc["accessors"][index]
    view = doc["bufferViews"][a["bufferView"]]
    kind = {5126: "f4", 5123: "u2", 5125: "u4", 5121: "u1"}[a["componentType"]]
    width = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4, "MAT4": 16}[a["type"]]
    start = view.get("byteOffset", 0) + a.get("byteOffset", 0)
    return np.frombuffer(bytes(blob_), kind, a["count"] * width, start).reshape(a["count"], width)


def add(doc, blob_, array, kind, target=None, bounds=False):
    while len(blob_) % 4:
        blob_.append(0)
    view = {"buffer": 0, "byteOffset": len(blob_), "byteLength": array.nbytes}
    if target:
        view["target"] = target
    blob_ += array.tobytes()
    doc["bufferViews"].append(view)
    a = {"bufferView": len(doc["bufferViews"]) - 1, "componentType": {"float32": 5126, "uint32": 5125, "uint8": 5121}[str(array.dtype)],
         "count": int(array.shape[0]) if array.ndim > 1 else int(array.size), "type": kind}
    if bounds:
        a["min"] = [float(v) for v in array.min(axis=0)]
        a["max"] = [float(v) for v in array.max(axis=0)]
    doc["accessors"].append(a)
    return len(doc["accessors"]) - 1


def unit(mesh):
    """From the hundredths the forms are typed in to the model's own unit."""
    p, n, t = mesh[:3]
    return (np.asarray(p, np.float32) * 0.01).astype(np.float32), np.asarray(n, np.float32), np.asarray(t, np.uint32)


def main():
    if "--restore" in sys.argv:
        if not KEPT.exists():
            raise SystemExit("no kept file at " + str(KEPT))
        shutil.copyfile(KEPT, GLB)
        print("restored", GLB)
        return
    if not KEPT.exists():
        KEPT.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(GLB, KEPT)
        print("kept the file as it was at", KEPT)
    doc, blob_ = read(KEPT)
    if doc.get("extras", {}).get("kuboRage"):
        raise SystemExit("the kept file is already remodelled: refusing to build on it")
    nodes = {n.get("name"): n for n in doc["nodes"]}
    skin_index = nodes["RageHood_part0"]["skin"]
    skin = doc["skins"][skin_index]
    order = {doc["nodes"][j]["name"]: k for k, j in enumerate(skin["joints"])}
    bind = accessor_array(doc, blob_, skin["inverseBindMatrices"]).reshape(-1, 4, 4).transpose(0, 2, 1)
    for name, at in J.items():
        now = np.linalg.inv(bind[order[name]])[:3, 3] * 100
        if np.abs(now - np.array(at)).max() > 0.02:
            raise SystemExit("the rig has moved: %s is bound at %s, this tool was typed against %s" % (name, np.round(now, 2), at))

    # ---- Blender fuses the skins and lays the big one's UVs.
    LOGS.mkdir(parents=True, exist_ok=True)
    fuse = LOGS / "fused.npz"
    if fuse.exists():
        fuse.unlink()
    ran = subprocess.run([BLENDER, "-b", "--python", str(Path(__file__).resolve()), "--", "--fuse=" + str(fuse)], capture_output=True, text=True)
    print("\n".join(line for line in ran.stdout.splitlines() if line.startswith("fused") or "Error" in line or "Traceback" in line))
    if not fuse.exists():
        print(ran.stdout[-3000:]); print(ran.stderr[-3000:])
        raise SystemExit("Blender did not fuse him.")
    data = np.load(fuse)
    sp, sn, st, suv = data["sp"], data["sn"], data["st"], data["suv"]
    tp, tn, tt = data["tp"], data["tn"], data["tt"]
    data.close()
    fuse.unlink()

    # ---- the paint. Blender's v runs up; the atlas's (and glTF's) runs down.
    uv = np.stack([R_SKIN[0] + (R_SKIN[2] - R_SKIN[0]) * suv[:, 0], R_SKIN[1] + (R_SKIN[3] - R_SKIN[1]) * (1 - suv[:, 1])], axis=1).astype(np.float32)
    png, texels = paint_atlas(sp.astype(np.float64), sn.astype(np.float64), st, uv.astype(np.float64), hollows(sp.astype(np.float64), st))
    (LOGS / "kuro-rage-atlas.png").write_bytes(png)
    while len(blob_) % 4:
        blob_.append(0)
    doc["bufferViews"].append({"buffer": 0, "byteOffset": len(blob_), "byteLength": len(png)})
    blob_ += png
    doc["images"] = doc.get("images", []) + [{"bufferView": len(doc["bufferViews"]) - 1, "mimeType": "image/png", "name": "kuro-rage-atlas"}]
    doc["samplers"] = doc.get("samplers", []) + [{"magFilter": 9729, "minFilter": 9987, "wrapS": 33071, "wrapT": 33071}]
    doc["textures"] = doc.get("textures", []) + [{"source": len(doc["images"]) - 1, "sampler": len(doc["samplers"]) - 1}]
    texture = {"index": len(doc["textures"]) - 1}
    materials = {m.get("name"): i for i, m in enumerate(doc["materials"])}
    doc["materials"].append({"name": "Kuro rage painted", "pbrMetallicRoughness": {"baseColorTexture": dict(texture), "baseColorFactor": [float(v) for v in to_linear(TINT)] + [1.0],
                                                                                 "metallicFactor": 0.0, "roughnessFactor": 1.0}})
    doc["materials"].append({"name": "Kuro rage tail", "pbrMetallicRoughness": {"baseColorTexture": dict(texture), "baseColorFactor": [1.0, 1.0, 1.0, 1.0],
                                                                              "metallicFactor": 0.0, "roughnessFactor": 1.0}})
    painted, tail_material = len(doc["materials"]) - 2, len(doc["materials"]) - 1

    def put(name, mesh, joints, weights, material=None, uvs=None, colour=None):
        node = nodes[name]
        prim = doc["meshes"][node["mesh"]]["primitives"][0]
        pos, nrm, tris = mesh
        attributes = {"POSITION": add(doc, blob_, pos, "VEC3", 34962, True), "NORMAL": add(doc, blob_, nrm, "VEC3", 34962),
                      "JOINTS_0": add(doc, blob_, joints.astype(np.uint8), "VEC4", 34962), "WEIGHTS_0": add(doc, blob_, weights.astype(np.float32), "VEC4", 34962)}
        if uvs is not None:
            attributes["TEXCOORD_0"] = add(doc, blob_, uvs.astype(np.float32), "VEC2", 34962)
        if colour is not None:
            attributes["COLOR_0"] = add(doc, blob_, colour.astype(np.float32), "VEC4", 34962)
        prim["attributes"] = attributes
        prim["indices"] = add(doc, blob_, tris.astype(np.uint32).reshape(-1), "SCALAR", 34963)
        prim.pop("targets", None)
        if material is not None:
            prim["material"] = material
        print("  %-26s %5d points %5d triangles" % (name, len(pos), len(tris)))
        return pos

    lo, hi = np.full(3, 9e9), np.full(3, -9e9)
    used = set()

    # ---- the one skin. A point is split where two islands of the paint meet.
    corner = st.reshape(-1)
    key = np.concatenate([corner[:, None].astype(np.float64), np.round(uv * 8192)], axis=1)
    _, first, back = np.unique(key, axis=0, return_index=True, return_inverse=True)
    points = corner[first]
    joints, weights = skin_weights(sp[points].astype(np.float64), order)
    body = put("RageHood_part0", unit((sp[points], sn[points], back.reshape(-1, 3))), joints, weights, painted, uv[first])
    used.add("RageHood_part0")
    lo, hi = np.minimum(lo, body.min(0)), np.maximum(hi, body.max(0))

    # ---- the tail: a flat cell a tier, and how much of it is left down its length.
    tier = np.zeros(len(tp), int)
    for k, (start, _) in enumerate(TIERS):
        tier[tp[:, 1] + 0.35 * np.sin(tp[:, 0] * 1.7 + tp[:, 2] * 1.3) < start] = k
    tail_uv = np.stack([cell_uv(k) for k in tier])
    left = np.ones((len(tp), 4), np.float32)
    left[:, 3] = np.clip((tp[:, 1] - 0.7) / 5.2, 0, 1) ** 0.7        # whole to below its middle, then thinning to the tip
    joints, weights = tail_weights(tp.astype(np.float64), order)
    tail = put("RageLowerSpirit_part0", unit((tp, tn, tt)), joints, weights, tail_material, tail_uv, left)
    used.add("RageLowerSpirit_part0")
    lo, hi = np.minimum(lo, tail.min(0)), np.maximum(hi, tail.max(0))

    # ---- the hard parts, seated on the face wherever the fusing left it.
    near = (np.abs(sp[:, 0] - EYE[0]) < 0.8) & (np.abs(sp[:, 1] - EYE[1]) < 0.6) & (sp[:, 2] > -1)
    front = float(sp[near][:, 2].max()) if near.any() else 0.9
    for name, (mesh, joint, material) in hard_parts(front).items():
        joints, weights = rigid(len(mesh[0]), order, joint)
        uvs = np.tile(cell_uv(EMBER_CELL), (len(mesh[0]), 1)) if material == "TAIL" else None
        index = tail_material if material == "TAIL" else materials[material] if material else None
        got = put(name, unit(mesh), joints, weights, index, uvs)
        used.add(name)
        lo, hi = np.minimum(lo, got.min(0)), np.maximum(hi, got.max(0))

    # ---- every other rage mesh was a block the skin now covers: one speck, inside him, on the joint it always rode.
    speck = np.array([[0.0, 0.2000, -0.0600], [0.0002, 0.2000, -0.0600], [0.0, 0.2002, -0.0600]], np.float32)
    up = np.tile(np.array([[0, 0, 1]], np.float32), (3, 1))
    for name, node in nodes.items():
        if name in used or node.get("skin") != skin_index or "mesh" not in node:
            continue
        prim = doc["meshes"][node["mesh"]]["primitives"][0]
        rode = int(accessor_array(doc, blob_, prim["attributes"]["JOINTS_0"])[0, 0])
        joints = np.zeros((3, 4), np.uint8); joints[:, 0] = rode
        weights = np.zeros((3, 4), np.float32); weights[:, 0] = 1
        had_uv = "TEXCOORD_0" in prim["attributes"]
        put(name, (speck, up, np.array([[0, 1, 2]], np.uint32)), joints, weights, None, np.zeros((3, 2), np.float32) if had_uv else None)

    doc.setdefault("extras", {})["kuboRage"] = "2026-10-10 one skin, painted: the same cat-ghost gone huge"
    write(GLB, doc, blob_)
    print("wrote", GLB, GLB.stat().st_size, "bytes;", texels, "texels of skin painted")
    print("bounds now  %s to %s" % (np.round(lo * 100, 2), np.round(hi * 100, 2)))
    print("bounds were %s to %s" % (OLD_LO, OLD_HI))
    over = (lo * 100 < OLD_LO - 0.05) | (hi * 100 > OLD_HI + 0.05)
    if over.any():
        print("⚠️ OUTSIDE THE OLD FORM on axes", np.nonzero(over)[0].tolist(), ": `KuroFormTests` bounds its size; pull it in.")

    if "--review" in sys.argv:
        shots = LOGS / ("kuro_rage_" + arg("tag", "v1"))
        ran = subprocess.run([BLENDER, "-b", "--python", str(Path(__file__).resolve()), "--", "--look=" + str(GLB), "--shots=" + str(shots), "--seen=" + str(LOGS / "kuro-rage-atlas-seen.png")], capture_output=True, text=True)
        if "looked" not in ran.stdout:
            print(ran.stdout[-3000:]); print(ran.stderr[-2000:])
        else:
            print("looked:", shots, "_front/_quarter/_side/_back/_under/_mouth .png")


if __name__ == "__main__":
    main()
