"""Review renders for the basin dressing kit (tools/payo_pieces/plaza_kit.py). Blender 5.0, headless.

  blender -b --python tools/payo_pieces/plaza_review.py -- <ABS Logs/payo/pieces/plaza> <ABS repo root> <tag>

Pieces ALONE first ("pieces first"), then each one where it will stand, against the court platform's own model:
  plinth_alone, planter_alone, vine_alone      the piece by itself on a grey floor with a 1.75 m figure
  plinth_seated                                the game's bulul_seated at 1.4 on the plinth at 1.2, the old size beside it
  plinth_standing                              bulul_standing at 1.5 on the plinth at 0.85
  vine_lip                                     vines over the pit's rim lip, seen from the stage's edge (court_platform.glb)
A warm low sunset key (the map's look), a cool fill, the reviews' ink hull (pad_review.py's).
"""
import bpy, math, os, sys
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pad_review as R  # noqa: E402  (its module body renders nothing when PROPS are filtered out below)

ARGS = sys.argv[sys.argv.index("--") + 1:]
OUT, ROOT, TAG = ARGS[0], ARGS[1], (ARGS[2] if len(ARGS) > 2 else "v1")
SHOTS = os.path.join(OUT, "shots")
os.makedirs(SHOTS, exist_ok=True)
MODELS = os.path.join(ROOT, "Assets/TumbangPreso/Art/Payo/Models")


def put(objs, at=(0, 0, 0), yaw=0.0, scale=1.0):
    roots = [o for o in objs if o.parent is None and not o.name.endswith("_ink")]
    inks = [o for o in objs if o.name.endswith("_ink")]
    for o in roots + inks:
        o.scale = (scale, scale, scale)
        o.rotation_euler = (0, 0, math.radians(yaw))
        o.location = Vector(at)
    return objs


def loadp(path, at=(0, 0, 0), yaw=0.0, scale=1.0):
    objs = R.load(path)
    # glTF children carry their parent: move only the importer's roots (and the ink copies, which have none)
    return put(objs, at, yaw, scale)


def cutout(objs):
    """Leaf cards: alpha clip in the review (the importer's materials blend)."""
    for o in objs:
        for m in o.data.materials if o.type == "MESH" else []:
            if m and "leaf" in m.name:
                try:
                    m.surface_render_method = "DITHERED"
                except Exception:
                    pass


def shoot(sc, cam, name, eye, look, fov):
    R.aim(cam, Vector(eye), Vector(look), fov)
    R.shoot(sc, os.path.join(SHOTS, "%s_%s.png" % (TAG, name)))


build = os.path.join(OUT)
# 1. alone
sc, cam = R.stage((900, 600))
p = loadp(os.path.join(build, "plaza_plinth.glb"))
shoot(sc, cam, "plinth_alone", (11, -13, 5.5), (0, 0, 1.8), 34)
shoot(sc, cam, "plinth_close", (4.2, -7.5, 2.8), (1.5, -3.9, 2.0), 40)

sc, cam = R.stage((900, 600))
loadp(os.path.join(build, "plaza_planter.glb"))
shoot(sc, cam, "planter_alone", (4.5, -5.5, 2.6), (0, 0, 0.4), 36)

sc, cam = R.stage((900, 600))
v = loadp(os.path.join(build, "plaza_vine.glb"), at=(0, 0, 4.5))
cutout(v)
bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 1.0, 2.25))          # a wall for it to hang on (front face at y 0)
w = bpy.context.object; w.scale = (5, 2, 4.5)
mw = bpy.data.materials.new("wall"); mw.use_nodes = True
mw.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.55, 0.53, 0.5, 1)
w.data.materials.append(mw)
shoot(sc, cam, "vine_alone", (2.5, -6.5, 3.0), (0, 0, 2.4), 42)
shoot(sc, cam, "vine_side", (5.5, -2.0, 4.0), (0, 0, 2.6), 42)

# 2. with the bulul (the game's own models, unchanged, only sized up)
sc, cam = R.stage((1000, 640))
loadp(os.path.join(build, "plaza_plinth.glb"), at=(0, 0, 0), scale=1.2)
loadp(os.path.join(MODELS, "bulul_seated.glb"), at=(0, 0, 3.8 * 1.2), yaw=0, scale=1.4)
loadp(os.path.join(MODELS, "bulul_seated.glb"), at=(-14, 0, 0), yaw=0, scale=1.0)
shoot(sc, cam, "plinth_seated", (8, -30, 6.0), (-4, 0, 6.5), 40)

sc, cam = R.stage((1000, 640))
loadp(os.path.join(build, "plaza_plinth.glb"), at=(0, 0, 0), scale=0.85)
loadp(os.path.join(MODELS, "bulul_standing.glb"), at=(0, 0, 3.8 * 0.85), yaw=0, scale=1.5)
loadp(os.path.join(MODELS, "bulul_standing.glb"), at=(-10, 0, 0), yaw=0, scale=1.0)
shoot(sc, cam, "plinth_standing", (6, -24, 5.0), (-3, 0, 6.0), 40)

# 3. the vines on the pit's rim lip, as from the stage's edge (Blender: x east, y north; the court's top z 0)
sc, cam = R.stage((1000, 640))
for o in list(bpy.data.objects):
    if o.type == "MESH" and o.name.startswith(("Plane", "Cylinder")):
        bpy.data.objects.remove(o, do_unlink=True)
court = loadp(os.path.join(MODELS, "court_platform.glb"))       # its authored frame (the game turns it 180 to undo glTFast's x flip)
for k, y in enumerate((-18, -6, 6, 18)):
    vv = loadp(os.path.join(build, "plaza_vine_long.glb" if k % 2 else "plaza_vine.glb"), at=(29.8, y + (k % 2) * 1.5, -5.65), yaw=-90)
    cutout(vv)
shoot(sc, cam, "vine_lip", (20, -8, 1.7), (30, 4, -9), 60)
print("done")
