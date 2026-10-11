"""Render the review stills of Dante's (displayed: Basilio) first-person arms, in Blender, no Unity.

    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/render_character_redesign_dante_fpv_arms.py -- fpv01
    py -3 tools/render_character_redesign_dante_fpv_arms.py fpv01        (stacks and labels the frames)

The first call writes single frames into Logs/character-redesign-dante/<version>/; the second
(the same file, run by plain Python, which has PIL) writes beside that folder
    <version>_first_person.png   both arms where the game holds them, from the player's eye
    <version>_old_vs_new.png     the same view over the OLD arm (approximate), for comparison
    <version>_turnaround.png     one arm from five sides, a three-quarter view, left and right together
A NEW VERSION NAME EVERY ITERATION, and LOOK at what comes out before believing it.

WHAT IS REPRODUCED, from the game's own numbers (read, not edited):
  * THE BAKE. The .glb is read raw and put through the arithmetic of
    `ViewmodelArmAuthor.Extract`, as glTFast would hand it over (x mirrored): triangles whose
    vertices are 0.99 or more on `arm-right` / `arm-left`, moved into the bone's bind space,
    mapped to (-side z, side x - first, -y) and scaled to 0.84 long. So what is drawn is what the
    bake would make of the file, and its bounds are printed.
  * THE WIDTH RULE of `ViewmodelArms.ApplyRosterArm`: x and z scaled by min(1, 0.34 / section).
  * THE REST PLACEMENT of `ViewmodelArms` (RightBasis, RightOrigin, LeftBasis, LeftOrigin, turned
    for Unity by `ToUnityPosition` and `ToUnityRotation`), under `CameraRig.ViewmodelSeat`
    (0, -0.10, 0.16), with the framing of `ViewmodelArms.Framing.cs` at full weight: the rig 8 cm
    lower and at scale 0.64, seen through a 95 degree vertical lens (`FixedViewmodelFov`), 16:9.
  * THE OLD ARM: Resources/Models/viewmodel_arm.obj in `SkinDante`, with boxes where
    `BuildDanteAccessories` puts them (sizes and places copied; the dark backing plates under his
    stripes are left out). Labelled "old, approximate".
⚠️ WHAT IS NOT. The idle breathe, a carried tsinelas, any action pose; the real `Toon.shader`
(this is the two-band imitation of tools/render_character_redesign_dante.py, and the game now
runs its smooth shading), its ink colour and width rule, tonemap, fog and the world's sun
direction; the squash the lens applies when the player's field of view is not 95. Nothing here
has been through Unity. It is for judging shape, paint and placement.
"""
import math
import os
import sys

TOOLS = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(TOOLS).replace("\\", "/")
BASE_OUT = REPO + "/Logs/character-redesign-dante"
D = REPO + "/Assets/TumbangPreso/Art/CharacterRedesign/dante/"
GLB = D + "dante-redesign-fpv-arms.glb"
ATLAS = D + "dante-redesign-fpv-arms-atlas.png"
OLD_OBJ = REPO + "/Assets/TumbangPreso/Resources/Models/viewmodel_arm.obj"

try:
    import bpy
except ImportError:
    bpy = None

# ---------------------------------------------------------------------------
# THE GAME'S NUMBERS
# ---------------------------------------------------------------------------
ARM_LENGTH = 0.84                 # ViewmodelArms.ArmLength
SECTION_CAP = 0.34                # ViewmodelArms.ApplyRosterArm
FOV = 95.0                        # ViewmodelArms.FixedViewmodelFov, vertical
SEAT = (0.0, -0.10 - 0.08, 0.16)  # CameraRig.ViewmodelSeat, lowered 8 cm by the framing
RIG_SCALE = 0.64                  # the framing's scale at full weight (CameraRig.ViewmodelScale is 0.72 without it)
# (basis y, basis z, origin) as the .tscn has them; ToUnityRotation and ToUnityPosition are applied below
PIVOT = {
    "right": ((-0.30109, 0.62224, -0.72261), (0.31474, -0.65046, -0.69126), (0.5800, -1.0200, -0.3400)),
    "left": ((0.30109, 0.62224, -0.72261), (-0.31474, -0.65046, -0.69126), (-0.6000, -1.0400, -0.3200)),
}
OUTLINE_OLD = 0.0045 * 2.38           # ToonSkin.PersonOutlineWidth, the shared arm
OUTLINE_NEW = OUTLINE_OLD * 0.45      # ApplyRosterArm's finer contour
SKIN_DANTE = (0.659, 0.376, 0.173)
ROBE_GREEN, ROBE_DARK = (0.239, 0.388, 0.208), (0.141, 0.243, 0.122)
LEATHER, LEATHER_DARK = (0.282, 0.184, 0.114), (0.180, 0.110, 0.060)
OLD_GOLD, OLD_GOLD_DARK = (0.875, 0.698, 0.282), (0.680, 0.520, 0.180)
BACKDROP = (0.135, 0.185, 0.270)      # linear; a mid grey-blue on screen

# name, size, position, turn about z in degrees, colour: BuildDanteAccessories, arm-local
OLD_BOXES = {
    "right": [
        ("LeatherSleeve", (0.315, 0.30, 0.305), (0.0, 0.16, 0.0), 0, LEATHER),
        ("LeatherSleeveCrease", (0.325, 0.04, 0.315), (0.0, 0.06, 0.0), 0, LEATHER_DARK),
        ("HarnessStrap", (0.075, 0.28, 0.025), (0.02, 0.16, 0.155), -28, OLD_GOLD),
        ("HarnessBuckle", (0.09, 0.06, 0.035), (-0.02, 0.19, 0.162), 0, OLD_GOLD_DARK),
        ("GoldCuffLining", (0.325, 0.09, 0.315), (0.0, 0.355, 0.0), 0, ROBE_DARK),
        ("GoldCuffBody", (0.345, 0.080, 0.335), (0.0, 0.355, 0.0), 0, OLD_GOLD),
        ("GoldCuffRimTop", (0.352, 0.020, 0.342), (0.0, 0.385, 0.0), 0, OLD_GOLD),
        ("GoldCuffRimBot", (0.352, 0.020, 0.342), (0.0, 0.325, 0.0), 0, OLD_GOLD_DARK),
        ("MarkOuterWrap", (0.018, 0.320, 0.110), (-0.132, 0.482, 0.0), 0, ROBE_GREEN),
    ] + [item for z in (0.128, -0.128) for item in (
        ("MarkWide1", (0.092, 0.120, 0.018), (-0.045, 0.360, z), 0, ROBE_GREEN),
        ("MarkNarrow", (0.062, 0.120, 0.018), (-0.060, 0.482, z), 0, ROBE_GREEN),
        ("MarkWide2", (0.092, 0.120, 0.018), (-0.045, 0.604, z), 0, ROBE_GREEN),
        ("MarkGoldStrip", (0.024, 0.330, 0.018), (-0.108, 0.482, z), 0, OLD_GOLD),
    )],
    "left": [
        ("ShoulderCap", (0.295, 0.12, 0.275), (0.0, 0.07, 0.0), 0, ROBE_DARK),
        ("ShoulderGoldTrim", (0.305, 0.035, 0.285), (0.0, 0.13, 0.0), 0, OLD_GOLD),
        ("ShoulderGreenLining", (0.300, 0.03, 0.280), (0.0, 0.155, 0.0), 0, ROBE_GREEN),
        ("StripeOuterWrap", (0.018, 0.300, 0.055), (-0.132, 0.470, 0.030), 0, ROBE_GREEN),
    ] + [item for x in (-0.070, 0.030) for z in (0.129, -0.129) for item in (
        ("StripeRun", (0.046, 0.312, 0.018), (x, 0.430, z), 0, ROBE_GREEN),
        ("StripeCap", (0.062 + 0.046, 0.046, 0.018), (x + 0.031, 0.603, z), 0, ROBE_GREEN),
    )],
}


# ---------------------------------------------------------------------------
# VECTORS (plain tuples, so the arithmetic reads like the C# it copies)
# ---------------------------------------------------------------------------

def cross(a, b):
    return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])


def norm(a):
    d = math.sqrt(sum(c * c for c in a)) or 1.0
    return tuple(c / d for c in a)


def rest_frame(side):
    """The arm transform's axes and origin in the CAMERA's space, Unity axes (x right, y up, z ahead)."""
    by, bz, origin = PIVOT[side]
    y = norm((by[0], by[1], -by[2]))             # ToUnityRotation
    z = norm((-bz[0], -bz[1], bz[2]))
    x = norm(cross(y, z))                        # Quaternion.LookRotation(z, y): right is up cross ahead
    o = (origin[0], origin[1], -origin[2])       # ToUnityPosition
    return x, y, z, o


def to_view(p, side, width=1.0):
    """An arm-local point to Blender space with the camera at the origin looking down -z, +y up."""
    x, y, z, o = rest_frame(side)
    u = [SEAT[a] + RIG_SCALE * (o[a] + x[a] * p[0] * width + y[a] * p[1] + z[a] * p[2] * width) for a in range(3)]
    return (u[0], u[1], -u[2])


def eye_in_arm(side):
    x, y, z, o = rest_frame(side)
    rel = [(0.0 - SEAT[a]) / RIG_SCALE - o[a] for a in range(3)]
    return tuple(sum(rel[a] * axis[a] for a in range(3)) for axis in (x, y, z))


def to_bench(p, shift=(0.0, 0.0, 0.0)):
    """An arm-local point to Blender space for the turnaround. The arm's frame is Unity's (left
    handed), so one axis is turned over; drawn straight into Blender it would come out mirrored."""
    return (p[0] + shift[0], p[1] + shift[1], -(p[2] + shift[2]))


# ---------------------------------------------------------------------------
# THE BAKE: ViewmodelArmAuthor.Extract, on the raw .glb
# ---------------------------------------------------------------------------

def extract(path, bone):
    sys.path.insert(0, TOOLS)
    import build_person_voxel as bpv
    g, buf = bpv.read_glb(path)
    skin = g["skins"][0]
    names = [g["nodes"][j]["name"] for j in skin["joints"]]
    b = names.index(bone)
    bind = bpv.read_accessor(g, buf, skin["inverseBindMatrices"])[b]
    positions, uv, triangles, remap = [], [], [], {}
    for mesh in g["meshes"]:
        for prim in mesh["primitives"]:
            att = prim["attributes"]
            P = bpv.read_accessor(g, buf, att["POSITION"])
            T = bpv.read_accessor(g, buf, att["TEXCOORD_0"])
            J = bpv.read_accessor(g, buf, att["JOINTS_0"])
            W = bpv.read_accessor(g, buf, att["WEIGHTS_0"])
            I = [i[0] for i in bpv.read_accessor(g, buf, prim["indices"])]
            owned = lambda i: sum(w for j, w in zip(J[i], W[i]) if j == b) > 0.99
            for t in range(0, len(I), 3):
                tri = I[t:t + 3]
                if not all(owned(i) for i in tri):
                    continue
                for i in tri:
                    key = (id(P), i)
                    if key not in remap:
                        remap[key] = len(positions)
                        # glTFast mirrors x; the bind pose is a translation (the rig has no rest rotation)
                        positions.append((-P[i][0] - bind[12], P[i][1] + bind[13], P[i][2] + bind[14]))
                        uv.append((T[i][0], 1.0 - T[i][1]))
                    triangles.append(remap[key])
    if not positions:
        raise SystemExit("no geometry is weighted to " + bone)
    side = math.copysign(1.0, sum(p[0] for p in positions) / len(positions))
    first = min(p[0] * side for p in positions)
    last = max(p[0] * side for p in positions)
    scale = ARM_LENGTH / (last - first)
    out = [((-side * p[2]) * scale, (side * p[0] - first) * scale, (-p[1]) * scale) for p in positions]
    lo = [min(p[a] for p in out) for a in range(3)]
    hi = [max(p[a] for p in out) for a in range(3)]
    section = max(hi[0] - lo[0], hi[2] - lo[2])
    width = min(1.0, SECTION_CAP / max(0.001, section))
    print("%s: %d triangles, x %.3f..%.3f  y %.3f..%.3f  z %.3f..%.3f, section %.3f, width scale %.3f"
          % (bone, len(triangles) // 3, lo[0], hi[0], lo[1], hi[1], lo[2], hi[2], section, width))
    return out, uv, [tuple(triangles[i:i + 3]) for i in range(0, len(triangles), 3)], width


def read_obj(path):
    verts, faces = [], []
    for line in open(path):
        part = line.split()
        if not part:
            continue
        if part[0] == "v":
            verts.append(tuple(float(c) for c in part[1:4]))
        elif part[0] == "f":
            faces.append(tuple(int(c.split("/")[0]) - 1 for c in part[1:]))
    return verts, faces


# ---------------------------------------------------------------------------
# BLENDER
# ---------------------------------------------------------------------------
if bpy is not None:
    import bmesh
    from mathutils import Matrix, Vector

    def reset():
        bpy.ops.wm.read_factory_settings(use_empty=True)
        sc = bpy.context.scene
        sc.render.engine = "BLENDER_EEVEE"
        sc.view_settings.view_transform = "Standard"
        sc.render.film_transparent = False
        w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
        w.node_tree.nodes["Background"].inputs[0].default_value = (*BACKDROP, 1)
        w.node_tree.nodes["Background"].inputs[1].default_value = 1.0
        return sc

    def toon_material(name, image=None, colour=None):
        """The two-band imitation of tools/render_character_redesign_dante.py, on a texture or one colour."""
        m = bpy.data.materials.get(name)
        if m:
            return m
        m = bpy.data.materials.new(name); m.use_nodes = True
        nt = m.node_tree; nt.nodes.clear()
        dif = nt.nodes.new("ShaderNodeBsdfDiffuse"); dif.inputs[0].default_value = (1, 1, 1, 1)
        s2r = nt.nodes.new("ShaderNodeShaderToRGB")
        ramp = nt.nodes.new("ShaderNodeValToRGB"); ramp.color_ramp.interpolation = "LINEAR"
        ramp.color_ramp.elements[0].position = 0.10; ramp.color_ramp.elements[0].color = (0.45, 0.47, 0.56, 1)
        ramp.color_ramp.elements[1].position = 0.14; ramp.color_ramp.elements[1].color = (1.0, 0.97, 0.90, 1)
        mul = nt.nodes.new("ShaderNodeMixRGB"); mul.blend_type = "MULTIPLY"; mul.inputs[0].default_value = 1
        em = nt.nodes.new("ShaderNodeEmission")
        out = nt.nodes.new("ShaderNodeOutputMaterial")
        nt.links.new(dif.outputs[0], s2r.inputs[0]); nt.links.new(s2r.outputs[0], ramp.inputs[0])
        if image is not None:
            tex = nt.nodes.new("ShaderNodeTexImage"); tex.image = image; tex.interpolation = "Linear"
            nt.links.new(tex.outputs[0], mul.inputs[1])
        else:
            lin = [c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4 for c in colour]
            mul.inputs[1].default_value = (*lin, 1)
        nt.links.new(ramp.outputs[0], mul.inputs[2])
        nt.links.new(mul.outputs[0], em.inputs[0]); nt.links.new(em.outputs[0], out.inputs[0])
        return m

    def ink_material():
        m = bpy.data.materials.get("ink")
        if m:
            return m
        m = bpy.data.materials.new("ink"); m.use_nodes = True
        nt = m.node_tree; nt.nodes.clear()
        em = nt.nodes.new("ShaderNodeEmission"); em.inputs[0].default_value = (0.002, 0.002, 0.003, 1)
        out = nt.nodes.new("ShaderNodeOutputMaterial"); nt.links.new(em.outputs[0], out.inputs[0])
        m.use_backface_culling = True
        return m

    def add_outline(obj, width):
        hull = obj.copy(); hull.data = obj.data.copy(); hull.name = obj.name + "-ink"
        for c in obj.users_collection:
            c.objects.link(hull)
        bm = bmesh.new(); bm.from_mesh(hull.data)
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
        for f in bm.faces:
            f.normal_flip(); f.smooth = True
        bm.to_mesh(hull.data); bm.free()
        hull.data.materials.clear(); hull.data.materials.append(ink_material())
        d = hull.modifiers.new("push", "DISPLACE"); d.strength = -width; d.mid_level = 0.0
        hull.visible_shadow = False
        return hull

    def mesh_object(name, verts, faces, material, uvs=None, outline=0.0, recalc=False):
        mesh = bpy.data.meshes.new(name)
        mesh.from_pydata([tuple(v) for v in verts], [], [tuple(f) for f in faces])
        if recalc:
            bm = bmesh.new(); bm.from_mesh(mesh)
            bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
            bm.to_mesh(mesh); bm.free()
        if uvs is not None:
            layer = mesh.uv_layers.new(name="UVMap")
            for loop in mesh.loops:
                layer.data[loop.index].uv = uvs[loop.vertex_index]
            for poly in mesh.polygons:
                poly.use_smooth = True     # the .glb's vertices are already split at its hard edges
        mesh.materials.append(material)
        obj = bpy.data.objects.new(name, mesh)
        bpy.context.scene.collection.objects.link(obj)
        if outline > 0:
            add_outline(obj, outline)
        return obj

    _BAKED = {}

    def new_arm(side, put, outline_scale):
        if side not in _BAKED:
            _BAKED[side] = extract(GLB, "arm-" + side)
        verts, uvs, tris, width = _BAKED[side]
        image = bpy.data.images.get("fpv-atlas") or bpy.data.images.load(ATLAS)
        image.name = "fpv-atlas"
        return mesh_object("new-" + side, [put((v[0] * width, v[1], v[2] * width)) for v in verts], tris,
                           toon_material("toon-new", image=image), uvs, OUTLINE_NEW * outline_scale)

    def old_arm(side, put, outline_scale):
        verts, faces = read_obj(OLD_OBJ)
        mesh_object("old-" + side, [put(v) for v in verts], faces, toon_material("old-skin", colour=SKIN_DANTE),
                    outline=OUTLINE_OLD * outline_scale, recalc=True)
        corners = [(sx, sy, sz) for sx in (-0.5, 0.5) for sy in (-0.5, 0.5) for sz in (-0.5, 0.5)]
        quads = [(0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)]
        for name, size, pos, turn, colour in OLD_BOXES[side]:
            c, s = math.cos(math.radians(turn)), math.sin(math.radians(turn))
            pts = []
            for k in corners:
                lx, ly, lz = k[0] * size[0], k[1] * size[1], k[2] * size[2]
                pts.append(put((pos[0] + lx * c - ly * s, pos[1] + lx * s + ly * c, pos[2] + lz)))
            thin = min(size)
            mesh_object("old-%s-%s" % (side, name), pts, quads, toon_material("old-%.3f-%.3f-%.3f" % colour, colour=colour),
                        outline=min(OUTLINE_OLD, thin * 0.25) * outline_scale, recalc=True)   # AccessoryOutlineWidth

    def aim(eye, ahead, up, lens=None, ortho=None, res=(1280, 720), fov=None):
        """A camera at `eye` looking along `ahead` with `up` up, and the one low sun from over its left shoulder."""
        sc = bpy.context.scene
        for o in [o for o in bpy.data.objects if o.type in ("CAMERA", "LIGHT")]:
            bpy.data.objects.remove(o)
        f = Vector(ahead).normalized()
        r = f.cross(Vector(up)).normalized()
        u = r.cross(f).normalized()
        cam = bpy.data.cameras.new("cam"); co = bpy.data.objects.new("cam", cam); sc.collection.objects.link(co)
        co.matrix_world = Matrix.Translation(Vector(eye)) @ Matrix((r, u, -f)).transposed().to_4x4()
        cam.clip_start = 0.01; cam.clip_end = 100
        if ortho:
            cam.type = "ORTHO"; cam.ortho_scale = ortho
        elif fov:
            cam.sensor_fit = "VERTICAL"; cam.angle_y = math.radians(fov)
        else:
            cam.lens = lens
        sc.camera = co; sc.render.resolution_x, sc.render.resolution_y = res
        sun = bpy.data.lights.new("sun", "SUN"); sun.energy = 3.14; sun.angle = math.radians(1.0)
        so = bpy.data.objects.new("sun", sun); sc.collection.objects.link(so)
        travel = (f * 0.72 - u * 0.58 + r * 0.38).normalized()
        so.rotation_euler = travel.to_track_quat("-Z", "Y").to_euler()

    def render(path):
        sc = bpy.context.scene
        sc.render.filepath = path; sc.render.image_settings.file_format = "PNG"
        bpy.ops.render.render(write_still=True)
        print("WROTE", path)

    def main():
        args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else ["fpv00"]
        out = BASE_OUT + "/" + args[0]
        os.makedirs(out, exist_ok=True)
        for side in ("right", "left"):
            print("the eye, in the %s arm's own frame: (%.3f, %.3f, %.3f)" % ((side,) + eye_in_arm(side)))

        # 1 and 2: from the player's eye, the new arms and the old
        for tag, maker in (("new", new_arm), ("old", old_arm)):
            reset()
            for side in ("right", "left"):
                if maker is new_arm:
                    new_arm(side, lambda p, s=side: to_view(p, s), RIG_SCALE)   # the width is applied inside
                else:
                    old_arm(side, lambda p, s=side: to_view(p, s), RIG_SCALE)
            aim((0, 0, 0), (0, 0, -1), (0, 1, 0), fov=FOV, res=(1920, 1080))
            render("%s/fp_%s.png" % (out, tag))

        # 3: the right arm alone, from five sides. Directions are written in the arm's own frame.
        bench = lambda v: Vector(to_bench(v))
        mid = (0.0, 0.42, 0.0)
        reset(); new_arm("right", to_bench, 1.0)
        for tag, toward, up in (("eye", (0, 0, 1), (1, 0, 0)), ("inner", (1, 0, 0), (0, 0, 1)), ("under", (0, 0, -1), (-1, 0, 0)),
                                ("outer", (-1, 0, 0), (0, 0, 1))):
            aim(bench(mid) + bench(toward) * 3.0, -bench(toward), bench(up), ortho=0.92, res=(1380, 600))
            render("%s/turn_%s.png" % (out, tag))
        aim(bench((0, 0.84, 0)) + bench((0, 1, 0)) * 3.0, -bench((0, 1, 0)), bench((0, 0, 1)), ortho=0.46, res=(690, 600))
        render("%s/turn_end.png" % out)
        # a three-quarter look at each, for the solidity
        for side, lean in (("right", 0.75), ("left", -0.75)):
            reset(); new_arm(side, to_bench, 1.0)
            toward = Vector(to_bench((lean, 0.55, 1.0))).normalized()
            aim(bench((0, 0.50, 0)) + toward * 2.4, -toward, bench((0, 1, 0)), lens=85, res=(900, 900))
            render("%s/threeq_%s.png" % (out, side))
        # left and right together, hands up, as the eye has them (right arm on the right)
        reset()
        new_arm("right", lambda p: to_bench(p, (-0.24, 0, 0)), 1.0)
        new_arm("left", lambda p: to_bench(p, (0.24, 0, 0)), 1.0)
        aim(bench(mid) + bench((0, 0, 1)) * 3.0, -bench((0, 0, 1)), bench((0, 1, 0)), ortho=1.0, res=(1000, 1000))
        render("%s/pair.png" % out)

    if __name__ == "__main__":
        main()

# ---------------------------------------------------------------------------
# THE SHEETS (plain Python)
# ---------------------------------------------------------------------------
elif __name__ == "__main__":
    from PIL import Image, ImageDraw, ImageFont

    V = sys.argv[1]
    R = "%s/%s/" % (BASE_OUT, V)
    BG = (34, 36, 44)

    def font(size):
        try:
            return ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", size)
        except OSError:
            return ImageFont.load_default()

    def tile(name, label, width=None, crop=None, zoom=1.0):
        img = Image.open(R + name).convert("RGB")
        if crop:
            w, h = img.size
            img = img.crop((int(crop[0] * w), int(crop[1] * h), int(crop[2] * w), int(crop[3] * h)))
        if zoom != 1.0:
            img = img.resize((int(img.width * zoom), int(img.height * zoom)), Image.LANCZOS)
        if width:
            img = img.resize((width, int(img.height * width / img.width)), Image.LANCZOS)
        d = ImageDraw.Draw(img)
        f = font(22)
        box = d.textbbox((0, 0), label, font=f)
        d.rectangle([0, 0, box[2] + 20, box[3] + 16], fill=BG)
        d.text((10, 6), label, font=f, fill=(236, 238, 244))
        return img

    def stack(rows, path, gap=8):
        heights = [max(t.height for t in row) for row in rows]
        widths = [sum(t.width for t in row) + gap * (len(row) - 1) for row in rows]
        sheet = Image.new("RGB", (max(widths) + 2 * gap, sum(heights) + gap * (len(rows) + 1)), BG)
        y = gap
        for row, h in zip(rows, heights):
            x = gap
            for t in row:
                sheet.paste(t, (x, y)); x += t.width + gap
            y += h + gap
        sheet.save(path)
        print("WROTE", path)

    hands = (0.22, 0.52, 0.78, 1.0)      # the part of the frame the arms are in
    stack([[tile("fp_new.png", "%s  new arms, from the player's eye (95 degree lens, rest pose)" % V, 1600)],
           [tile("fp_new.png", "the same frame, the lower middle enlarged", 1600, crop=hands)]],
          "%s/%s_first_person.png" % (BASE_OUT, V))
    stack([[tile("fp_new.png", "%s  new" % V, 1600, crop=hands)],
           [tile("fp_old.png", "old, approximate (shared block arm, his sleeve boxes)", 1600, crop=hands)]],
          "%s/%s_old_vs_new.png" % (BASE_OUT, V))
    stack([[tile("turn_eye.png", "%s  right arm: the face the eye sees (back of the fist)" % V, 1100), tile("turn_end.png", "end-on at the fist", 550)],
           [tile("turn_inner.png", "inner side (thumb)", 825), tile("turn_outer.png", "outer side", 825)],
           [tile("turn_under.png", "the far face (where a carried tsinelas rides)", 825), tile("pair.png", "left and right, as the eye has them", 478)],
           [tile("threeq_left.png", "left, three-quarter", 600), tile("threeq_right.png", "right, three-quarter", 600)]],
          "%s/%s_turnaround.png" % (BASE_OUT, V))
