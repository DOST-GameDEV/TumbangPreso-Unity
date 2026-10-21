"""Basilio's BASTION slabs, CARVED AND BAKED the way his BOULDER's rocks are (tools/build_dante_rocks.py), which is the
way the Lagoon Cove stones and the ice crystals are.

  py -3 tools/build_dante_bastion.py        the b3 wall (seven slabs; kept so the sample he saw can be filmed again)
  py -3 tools/build_dante_bastion.py b4     the b4 wall: seven slabs more varied in height and lean, and six stones held
                                            up along the field's sides and top (held-0 to held-5), in one atlas

Writes, in Assets/TumbangPreso/Resources/Models/AbilityFx:
  dante_bastion.glb          seven basalt slabs standing in a low wall (slab-0 at his left to slab-6 at his right). Real
                             metres, y up, +z the way he faces, the origin on the court at the middle of the wall; each slab
                             a named node at its seat, its origin at its own middle.
  dante_bastion_mask.png     ONE atlas for all seven, LINEAR, read exactly as the BOULDER's is: R how near the nearest
                             plane break is (linear over `REACH`), G the broad shoulder, B occlusion (its neighbours' as
                             well as its own), A the gaps between slabs pressed together, where his molten light is laid.
and the bakes as pictures in Logs/dante-rocks/ for a board.

HONEST TO THE RULE. The wall stands where the reflect is (`DanteHeroKit.Barrier`): `GeoRules.BarrierWidth` (3.4 m) wide,
centred `GeoRules.BarrierForward` (1.2 m) in front of him, inside the 0.4 m band either side of that line. The slabs
together span 3.4 m and are at most 0.34 m deep, so nothing of them stands where a slipper is not turned back, and
nothing where one is turned back is left bare at their height. They are waist-high on these bodies (0.58 to 0.92 m on
a body about 1.3 m tall): the field above them (`FxStudyBastionField.shader`) is the rest of the reflect, to 2.6 m.

The carve is the BOULDER's, at a slab's size: a rounded body (`author_lagoon_rocks._body`) cut by TYPED planes as true
cuts (`_cut`), two of them deep (the big face toward the thrower and the one toward him), then a narrow real bevel at
every break over 25 degrees (`_bevel_breaks`) and only the sharp breaks sharp (`_sharp_edges`). Those are the Lagoon
kit's own, called. The bakes are made with the seven SEATED, pressed shoulder to shoulder, so B darkens where one leans
on the next and A finds the gaps between them.

Blender runs in the background for the carve and the bakes (never Unity). Seeded: two runs carve the same slabs.
"""
import math
import os
import subprocess
import sys
import uuid
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = Path(os.environ["DANTE_ROOT"]) if os.environ.get("DANTE_ROOT") else HERE.parent
TOOLS = Path(os.environ["DANTE_TOOLS"]) if os.environ.get("DANTE_TOOLS") else HERE
MODELS = ROOT / "Assets" / "TumbangPreso" / "Resources" / "Models"
# Where it writes. DANTE_ROCKS_OUT names another folder, to build aside while the owner's editor is in Play and copy in after.
OUT = Path(os.environ["DANTE_ROCKS_OUT"]) if os.environ.get("DANTE_ROCKS_OUT") else MODELS / "AbilityFx"
LOGS = Path(os.environ["DANTE_LOGS"]) if os.environ.get("DANTE_LOGS") else ROOT / "Logs" / "dante-rocks"
BLENDER = r"C:\Program Files\Blender Foundation\Blender 5.0\blender.exe"
SIZE = 2048
REACH = 0.070          # metres: R falls linearly from 1 on a plane break to 0 this far from it (a slab is three times a rock)
GAP = 0.045            # metres: how near another slab's skin counts as a gap (A)

WIDTH = 3.4            # GeoRules.BarrierWidth
DEPTH_MAX = 0.34       # inside the 0.4 m band either side of the line

# ---------------------------------------------------------------------------------------------------------------------
# THE SLABS, his left to his right. Each: its width across the wall, its height, how far it stands off the line (+ toward
# the thrower), its yaw and its seed. The middle three tallest, so the wall reads as raised from his centre; no two alike.
# A cut is (turn round the slab in degrees, lean, depth), as the BOULDER's: in Blender's axes 270 looks toward the thrower.
# ---------------------------------------------------------------------------------------------------------------------
SLABS = [
    dict(width=0.47, height=0.58, off=0.03, yaw=-9, tilt=-6, seed=101),
    dict(width=0.58, height=0.80, off=-0.03, yaw=5, tilt=3, seed=113),
    dict(width=0.44, height=0.68, off=0.04, yaw=-4, tilt=-4, seed=127),
    dict(width=0.62, height=0.92, off=0.00, yaw=3, tilt=2, seed=131),
    dict(width=0.46, height=0.66, off=-0.04, yaw=-7, tilt=5, seed=149),
    dict(width=0.57, height=0.84, off=0.03, yaw=6, tilt=-3, seed=151),
    dict(width=0.49, height=0.60, off=-0.01, yaw=-5, tilt=7, seed=163),
]


# b4: the owner of b3, "polish it more". Heights from knee to chest (0.46 to 1.06 m), leans to 12 degrees, the line
# broken forward and back, so the row is a heap of earth stood up and not a row of teeth. Then SIX STONES HELD UP in
# the field (the earth in it, not a pattern drawn on it): each a small carved rock at its place along the reflect
# zone's sides and top, in the game's metres (x across, y up), its size and seed.
SLABS_B4 = [
    dict(width=0.50, height=0.52, off=0.05, yaw=-12, tilt=-11, seed=201),
    dict(width=0.60, height=0.88, off=-0.05, yaw=8, tilt=5, seed=213),
    dict(width=0.42, height=0.64, off=0.06, yaw=-6, tilt=-9, seed=227),
    dict(width=0.64, height=1.06, off=-0.02, yaw=4, tilt=3, seed=231),
    dict(width=0.44, height=0.58, off=0.06, yaw=-10, tilt=12, seed=249),
    dict(width=0.58, height=0.94, off=-0.04, yaw=9, tilt=-6, seed=251),
    dict(width=0.48, height=0.46, off=0.02, yaw=-7, tilt=10, seed=263),
]
HELD = [
    dict(at=(-1.62, 1.28), across=0.26, seed=301), dict(at=(-1.44, 2.12), across=0.20, seed=307),
    dict(at=(-0.62, 2.52), across=0.24, seed=311), dict(at=(0.48, 2.58), across=0.18, seed=313),
    dict(at=(1.38, 2.26), across=0.22, seed=317), dict(at=(1.66, 1.46), across=0.28, seed=331),
]
B4 = "b4" in sys.argv[1:]
STEM = "dante_bastion_b4" if B4 else "dante_bastion"


def cuts_for(seed):
    import random
    rng = random.Random(seed)
    j = lambda a: a + rng.uniform(-12, 12)
    # The top is a SLANTED plane, not a flat one: seven flat-topped slabs in a row read as a row of teeth.
    return [(rng.choice((0, 180)) + rng.uniform(-25, 25), rng.uniform(1.6, 2.6), 0.70),
            (j(270), 0.10, 0.60),                                # THE big face, toward the thrower
            (j(90), 0.06, 0.64),                                 # the face toward him
            (j(0), 0.08, 0.86), (j(180), 0.10, 0.86),            # the two ends, pressed to the neighbours
            (j(300), 0.95, 0.80), (j(120), 0.85, 0.82),          # two shoulders
            (j(230), 1.10, 0.86),                                # a third, smaller
            (0, -9.0, 0.82),                                     # the foot
            (j(rng.choice((20, 160, 200, 340))), 1.25, 0.92)]    # a corner knocked off


def in_blender():
    import random

    import bmesh
    import bpy
    import numpy as np
    from mathutils import Vector

    sys.path.insert(0, str(TOOLS))
    import author_lagoon_rocks as lagoon
    import bake_lagoon_rock_edges as edges

    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene

    def activate(ob):
        for other in bpy.context.view_layer.objects:
            other.select_set(False)
        ob.select_set(True)
        bpy.context.view_layer.objects.active = ob

    def normal_of(turn, lean):
        if abs(lean) > 5:
            return Vector((0.10, 0.06, 1.0 if lean > 0 else -1.0)).normalized()
        return lagoon._plane(math.radians(turn), lean)

    slabs = []
    for i, s in enumerate(SLABS_B4 if B4 else SLABS):
        rng = random.Random(s["seed"])
        bm = bmesh.new()
        lagoon._body(bm, rng, 5, 2.7, (1.0, 0.62, s["height"] / s["width"]), taper=0.10, amp=0.025, lobes=2)
        verts = bm.verts[:]
        for turn, lean, depth in cuts_for(s["seed"]):
            verts = lagoon._cut(bm, verts, normal_of(turn, lean), depth)
        lagoon._bevel_breaks(bm, verts)
        xs, ys, zs = ([v.co[k] for v in bm.verts] for k in range(3))
        f = s["width"] / (max(xs) - min(xs))
        # The height is said outright: the cuts take some of it, so it is scaled back to the slab's own.
        fz = s["height"] / ((max(zs) - min(zs)) * f)
        mid = Vector(((max(xs) + min(xs)) / 2, (max(ys) + min(ys)) / 2, (max(zs) + min(zs)) / 2))
        yaw = math.radians(s["yaw"])
        for v in bm.verts:
            q = (v.co - mid) * f
            q.z *= fz
            q = Vector((q.x * math.cos(yaw) - q.y * math.sin(yaw), q.x * math.sin(yaw) + q.y * math.cos(yaw), q.z))
            # Leant a few degrees along the wall, each its own way, so the row is not a fence.
            tilt = math.radians(s["tilt"])
            v.co = Vector((q.x * math.cos(tilt) + q.z * math.sin(tilt), q.y, -q.x * math.sin(tilt) + q.z * math.cos(tilt)))
        bmesh.ops.dissolve_degenerate(bm, dist=1e-5, edges=bm.edges[:])
        bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(3.0), verts=bm.verts[:], edges=bm.edges[:])
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        bmesh.ops.triangulate(bm, faces=bm.faces[:])
        bm.normal_update()
        sharp = lagoon._sharp_edges(bm)
        depth = max(v.co.y for v in bm.verts) - min(v.co.y for v in bm.verts)
        if depth > DEPTH_MAX:
            for v in bm.verts:
                v.co.y *= DEPTH_MAX / depth
            depth = DEPTH_MAX
        name = "slab-%d" % i
        me = bpy.data.meshes.new(name)
        bm.to_mesh(me)
        size = [max(v.co[k] for v in bm.verts) - min(v.co[k] for v in bm.verts) for k in range(3)]
        print("CARVED %s: %d triangles, %d sharp edges, %.3f wide, %.3f deep, %.3f high" % (name, len(bm.faces), sharp, size[0], size[1], size[2]))
        bm.free()
        ob = bpy.data.objects.new(name, me)
        scene.collection.objects.link(ob)
        ob["across"], ob["high"], ob["off"] = size[0], size[2], s["off"]
        slabs.append(ob)
    # Seated: shoulder to shoulder from his left, each pressed into the next by the same amount, so that the row spans
    # the rule's width exactly; each sunk 4 cm into the court. Game (x, y up, z toward the thrower) is Blender (x, -z, y).
    press = (sum(ob["across"] for ob in slabs) - WIDTH) / (len(slabs) - 1)
    x = -WIDTH / 2
    for ob in slabs:
        seat = (x + ob["across"] / 2, ob["high"] / 2 - 0.04, ob["off"])
        x += ob["across"] - press
        ob.location = (seat[0], -seat[2], seat[1])
    print("MEASURED the wall: %.3f wide (the rule's %.2f), each slab pressed %.3f m into the next" % (x + press + WIDTH / 2, WIDTH, press))

    if B4:
        for i, s in enumerate(HELD):
            rng = random.Random(s["seed"])
            bm = bmesh.new()
            lagoon._body(bm, rng, 4, 2.5, (1.0, 0.85, 0.80), taper=0.06, amp=0.03, lobes=2)
            verts = bm.verts[:]
            for turn, lean, depth in [(0, 9.0, 0.70), (rng.uniform(0, 360), 0.10, 0.62), (rng.uniform(0, 360), 0.20, 0.74),
                                      (rng.uniform(0, 360), 0.05, 0.78), (rng.uniform(0, 360), 0.9, 0.80), (0, -9.0, 0.72),
                                      (rng.uniform(0, 360), 1.2, 0.90)]:
                verts = lagoon._cut(bm, verts, normal_of(turn, lean), depth)
            lagoon._bevel_breaks(bm, verts)
            xs, ys, zs = ([v.co[k] for v in bm.verts] for k in range(3))
            f = s["across"] / max(max(xs) - min(xs), max(ys) - min(ys))
            mid = Vector(((max(xs) + min(xs)) / 2, (max(ys) + min(ys)) / 2, (max(zs) + min(zs)) / 2))
            for v in bm.verts:
                v.co = (v.co - mid) * f
            bmesh.ops.dissolve_degenerate(bm, dist=1e-5, edges=bm.edges[:])
            bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(3.0), verts=bm.verts[:], edges=bm.edges[:])
            bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
            bmesh.ops.triangulate(bm, faces=bm.faces[:])
            bm.normal_update()
            lagoon._sharp_edges(bm)
            name = "held-%d" % i
            me = bpy.data.meshes.new(name)
            bm.to_mesh(me)
            print("CARVED %s: %d triangles, %.3f across" % (name, len(bm.faces), s["across"]))
            bm.free()
            ob = bpy.data.objects.new(name, me)
            scene.collection.objects.link(ob)
            ob.location = (s["at"][0], 0.0, s["at"][1])
            slabs.append(ob)

    names = [ob.name for ob in slabs]
    grid = 4 if B4 else 3
    for i, ob in enumerate(slabs):
        edges.unwrap_into_cell(ob.data, i, grid)

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
        for i, ob in enumerate(slabs):
            ob.data.materials.clear()
            ob.data.materials.append(m)
            activate(ob)
            bpy.ops.object.bake(type="EMIT", margin=8, use_clear=(i == 0), uv_layer="UVBake")
        px = np.empty(SIZE * SIZE * 4, np.float32)
        image.pixels.foreach_get(px)
        return image, px.reshape(SIZE, SIZE, 4)

    def shoulder_and_occlusion(nt):
        geo = nt.nodes.new("ShaderNodeNewGeometry")
        broad = edges._edge_mask(nt, geo, 0.080, 0.998, 0.80)
        ao = nt.nodes.new("ShaderNodeAmbientOcclusion")
        ao.samples = 16
        ao.inputs["Distance"].default_value = 0.30
        rgb = nt.nodes.new("ShaderNodeCombineColor")
        nt.links.new(broad, rgb.inputs["Green"])
        nt.links.new(ao.outputs["AO"], rgb.inputs["Blue"])
        return rgb.outputs["Color"]

    def gaps(nt):
        ao = nt.nodes.new("ShaderNodeAmbientOcclusion")
        ao.samples = 24
        ao.inputs["Distance"].default_value = GAP
        return ao.outputs["Color"]

    # The court under them, for the occlusion at their feet: a plane at the ground, removed before export.
    bpy.ops.mesh.primitive_plane_add(size=8.0, location=(0, 0, 0))
    floor = bpy.context.active_object
    image, first = bake("shoulder-occlusion", shoulder_and_occlusion)
    _, second = bake("gaps", gaps)
    bpy.data.objects.remove(floor)

    edges.ATLAS_PX, edges.DIST_MAX = SIZE, REACH
    edges.distance_to_breaks(image, [ob.data for ob in slabs])
    px = np.empty(SIZE * SIZE * 4, np.float32)
    image.pixels.foreach_get(px)
    mask = px.reshape(SIZE, SIZE, 4).copy()
    mask[..., 1], mask[..., 2] = first[..., 1], first[..., 2]
    mask[..., 3] = 1.0 - second[..., 0]
    np.save(str(LOGS / (STEM + "_mask.npy")), mask[::-1])

    for ob in slabs:
        ob.data.materials.clear()
        ob.select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(OUT / (STEM + ".glb")), export_format="GLB", use_selection=True, export_yup=True,
                              export_apply=False, export_texcoords=True, export_normals=True, export_materials="NONE",
                              export_animations=False)
    print("wrote", OUT / (STEM + ".glb"))


def main():
    import cv2
    import numpy as np

    LOGS.mkdir(parents=True, exist_ok=True)
    OUT.mkdir(parents=True, exist_ok=True)
    done = subprocess.run([BLENDER, "-b", "--python", str(Path(__file__).resolve()), "--", "carve"] + (["b4"] if B4 else []), capture_output=True, text=True, env=dict(os.environ))
    print("\n".join(l for l in done.stdout.splitlines() if l.startswith(("CARVED", "MEASURED", "wrote", "[rock-edges]", "Error", "Traceback"))))
    if done.returncode != 0 or "Traceback" in done.stdout + done.stderr:
        print(done.stdout[-3000:], done.stderr[-3000:])
        raise SystemExit("Blender failed")
    mask = np.clip(np.load(str(LOGS / (STEM + "_mask.npy"))), 0, 1)
    out = (mask * 255 + .5).astype(np.uint8)
    path = OUT / (STEM + "_mask.png")
    cv2.imwrite(str(path), cv2.cvtColor(out, cv2.COLOR_RGBA2BGRA))
    meta = Path(str(path) + ".meta")
    source = MODELS / "AbilityFx" / "dante_boulder_mask.png.meta"
    if not meta.exists() and source.exists():
        # The BOULDER mask's import settings (linear data, since R is a distance), under a name of its own.
        lines = source.read_text().splitlines()
        lines[1] = "guid: " + uuid.uuid4().hex
        meta.write_text("\n".join(lines) + "\n")
    cv2.imwrite(str(LOGS / ("bakes_" + STEM + ".png")), cv2.hconcat([cv2.resize(out[..., k], (768, 768), interpolation=cv2.INTER_AREA) for k in range(4)]))
    print("wrote %s: R covers %.1f%%, gaps cover %.1f%%" % (path.name, 100 * (mask[..., 0] > .05).mean(), 100 * (mask[..., 3] > .3).mean()))


if __name__ == "__main__":
    if "--" in sys.argv:
        in_blender()
    else:
        main()
