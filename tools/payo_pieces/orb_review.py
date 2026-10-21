"""Review renders for Payo's stamina orb (tools/payo_pieces/orb_build.py). Blender 5.0, headless.

  blender -b --python tools/payo_pieces/orb_review.py -- <ABS Logs/payo/pieces/orb> <ABS repo root>

The orb (orb/pickup_orb.glb: cell and halo raised to the game's 0.95 m) over the carved plinth it floats over in the
game (Art/Payo/Models/pickup_plinth.glb, read only), beside the Arena's pickup for the old reference. The glow
material's picture is its emission (strength 1.4, as the game's day glow lifts it). The pad review's stage: warm low
sunset key, cool fill, warm stone floor, the reviews' ink hull, a 1.75 m figure. Writes orb/shots/orb_*.png.
"""
import bpy, math, os, sys
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
ORB, ROOT = sys.argv[sys.argv.index("--") + 1:][:2]
SHOTS = os.path.join(ORB, "shots")
os.makedirs(SHOTS, exist_ok=True)
for f in os.listdir(SHOTS):
    if f.startswith("orb_") and f.endswith(".png"):
        os.remove(os.path.join(SHOTS, f))
_argv = sys.argv; sys.argv = sys.argv[:sys.argv.index("--") + 3] + ["(none: only its helpers)"]
import pad_review as P                                    # noqa: E402  (no prop named: its own loop renders nothing)
sys.argv = _argv

PLINTH = os.path.join(ROOT, "Assets/TumbangPreso/Art/Payo/Models/pickup_plinth.glb")


def load_orb(path, yaw=0.0):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    for o in new:
        if o.parent is None:
            o.location.z += 0.95
        if o.name.startswith("pickup_cell"):
            o.rotation_euler[2] = math.radians(yaw)
    meshes = [o for o in new if o.type == "MESH"]
    for o in meshes:
        for m in o.data.materials:
            if not (m and m.use_nodes):
                continue
            b = m.node_tree.nodes.get("Principled BSDF")
            if b is None:
                continue
            b.inputs["Roughness"].default_value = 0.9
            if m.name.startswith("pickup_orb_glow"):
                b.inputs["Emission Strength"].default_value = 1.4
            else:
                tex = next((n for n in m.node_tree.nodes if n.type == "TEX_IMAGE" and n.image and "normal" not in n.image.name.lower()), None)
                if tex:
                    m.node_tree.links.new(tex.outputs["Color"], b.inputs["Emission Color"])
                    b.inputs["Emission Strength"].default_value = 0.22
    return meshes + [P.ink(o, 0.006) for o in meshes]


sc, cam = P.stage()
plinth = P.load(PLINTH)
orb = load_orb(os.path.join(ORB, "pickup_orb.glb"))
views = [("a_close", Vector((1.1, -1.5, 1.35)), Vector((0, 0, 0.82)), 34),
         ("b_eye5", Vector((2.2, -4.5, 1.7)), Vector((0, 0, 0.7)), 32),
         ("c_top34", Vector((1.4, -1.5, 2.3)), Vector((0, 0, 0.75)), 40),
         ("d_far12", Vector((4.0, -11.4, 1.7)), Vector((0, 0, 0.7)), 22),
         ("e_far30", Vector((9.0, -28.5, 2.0)), Vector((0, 0, 0.7)), 14)]
for name, eye, look, fov in views:
    P.aim(cam, eye, look, fov)
    P.shoot(sc, os.path.join(SHOTS, "orb_%s.png" % name))
# the cell turns in the game: a strip of its turn, close
for yaw in (22, 45):
    for o in orb:
        if o.name.startswith("pickup_cell"):
            o.rotation_euler[2] = math.radians(yaw)
    P.aim(cam, Vector((1.1, -1.5, 1.35)), Vector((0, 0, 0.82)), 34)
    P.shoot(sc, os.path.join(SHOTS, "orb_a_close_turn%02d.png" % yaw))
print("done")
