"""Build Dante's (displayed: Basilio) FIRST-PERSON ARMS: his redesign's own pieces, cut for the close camera.

    py -3 tools/author_character_redesign_dante_fpv_arms_textures.py
    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/author_character_redesign_dante_fpv_arms.py

Writes
    Assets/TumbangPreso/Art/CharacterRedesign/dante/dante-redesign-fpv-arms.glb
    ArtSource/dante/redesign-20261005/dante_fpv_arms.blend
beside the atlas the textures script paints. NOTHING IN THE GAME LOADS IT YET: it becomes his
first-person arms when `RosterBookBuilder.FirstPersonArmModels` names it for "dante" and
`ViewmodelArmAuthor.Bake` is run. No C# is touched by this file.

WHY. The nine heroes were redesigned (docs/CHARACTER_REDESIGN_DANTE.md sections 13 and 15), but in
first person the player still sees the old look: the shared block arm
(Resources/Models/viewmodel_arm.obj) tinted skin colour, with hand-coded sleeve boxes
(`ViewmodelArms.BuildDanteAccessories`). Arms cut out of a redesigned body and stretched to
first-person length were turned down on another hero: "long thin tubes", "fragmented hands". The
owner likes the SOLID BLOCK HAND of the shared arm. So this is a model of its own: the same
pieces, colours and paint as his redesign, proportioned for the eye that is 1.4 arm-units away.

HOW IT REACHES THE GAME (read, not edited):
  * `ViewmodelArmAuthor.Extract(model, "arm-right" | "arm-left")` takes every triangle whose
    three vertices are weighted 0.99 or more to that bone, moves them into the bone's bind space
    and maps them into the first-person frame: outward along the arm becomes +Y, the model's UP
    becomes -Z, and the whole is scaled so its length is `ViewmodelArms.ArmLength` = 0.84.
  * `ViewmodelArms.ApplyRosterArm` then squeezes the width if the larger cross-section is over
    0.34. Nothing here is over 0.34, so nothing is squeezed.
  * So this file is WRITTEN IN THAT FIRST-PERSON FRAME (0 to 0.84 along Y) and turned back into
    the body rig's space at the end, about the body's own arm pivots, at the body arm's own
    length (0.192), so `Extract` gives back exactly the numbers below.

MEASURED ON Resources/Models/viewmodel_arm.obj (16 vertices, two boxes, length along +Y):
    forearm   y 0.000 to 0.620    x -0.130 to 0.130 (0.260)    z -0.122 to 0.122 (0.244)
    hand      y 0.620 to 0.840    x -0.158 to 0.158 (0.316)    z -0.150 to 0.150 (0.300)
    whole     0.840 long (`ArmLength`), widest section 0.316: 2.66 to 1
    the hand is the far 26.2 per cent of the length; its -Z face (the "upper surface" of the
    game's notes, where `HeldSlipperLocal` z -0.165 parks a carried tsinelas) is at z -0.150
THE FIST HERE: y 0.620 to 0.840 exactly; 0.308 across X plus the thumb (0.332 in all, from
    -0.164 to 0.168 on the right arm, mirrored on the left); z -0.150 to 0.150 at its fullest
    (y 0.775), so its -Z face is where the shared hand's is and every held prop, throw and cast
    posed on that hand frame lands on it. The forearm is 0.256 by 0.240 at the cuff, close to the
    shared arm's 0.260 by 0.244. The widest thing is the gold cuff, 0.336 by 0.324.

WHICH FACE THE PLAYER SEES, worked out from `ViewmodelArms` (RightBasis, RightOrigin,
`ToUnityRotation`), `CameraRig.ViewmodelSeat` (0, -0.10, 0.16) and the framing of
`ViewmodelArms.Framing.cs` at full weight (scale 0.64, 8 cm lower): in the right arm's own frame
the eye is at about (-0.05, 0.56, 1.44). It looks almost square onto the +Z face, from a point
level with the wrist. The arm leaves the bottom of the screen near y 0.47. So:
    +Z is the face to paint for the eye (it is the body model's UNDERSIDE, which is why arms cut
       from the body showed their shaded under half), and the back of the fist is drawn there;
    +X is screen left: the inner side of the right arm, the outer side of the left; the thumb is
       on the inner side of each;
    the far 44 per cent (cuff end or wrap, wrist, fist) is what is in view at rest, so the detail
    is there and the shoulder end is plain.

THE PIECES ARE HIS, from tools/author_character_redesign_dante.py `build_arm_right` and
`build_arm_left`, and nothing else:
    right   sleeve (forest green) / the fat rolled gold cuff / bare forearm / the jade cord
            bracelet with its gold knot (drawn on the body, small blocks here) / fist
    left    the torn sleeve stub at the shoulder / bare arm / the cloth wrap / fist
⚠️ THE FIST IS ONE SOLID BLOCK. Fingers are drawn, as on his body ("the cast's hands are plain
blocks, and this one's fingers and thumb are drawn"), and as the owner asked of every first-person
hand on 2026-08-29 ("i dont want finger geometry at all ... our characters dont have fingers").
The ONE addition is a low thumb block on the inner side, because this brief asks for a fist "with
a thumb side". Set THUMB_BLOCK to False to see the fist without it.

THE RIG is a small one with the body's names and pivots: root, torso, arm-left, arm-right, read
off dante-redesign.glb at build time (arm pivots at x 0.0999, y 0.0172, z 0.288 in Blender
space, rest pose straight out along x). No forearm bones: the first-person rig poses the arm as
one rigid piece. Every vertex is weighted 1.0 to its arm bone. One mesh, one material, its own atlas.
"""
import math
import os
import struct
import sys
import time

import bpy
import bmesh
from mathutils import Vector

TOOLS = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(TOOLS)
sys.path.insert(0, TOOLS)
import build_person_voxel as bpv  # noqa: E402  the cast's glb reader and writer, not edited
import author_character_redesign_dante as body  # noqa: E402  the block, the loft, the chamfer: not edited
import author_character_redesign_dante_fpv_arms_textures as ftex  # noqa: E402  the island layout and the measures

FOLDER = os.path.join(ROOT, "Assets/TumbangPreso/Art/CharacterRedesign/dante")
BODY_GLB = os.path.join(FOLDER, "dante-redesign.glb")
OUT = os.path.join(FOLDER, "dante-redesign-fpv-arms.glb")
BLEND = os.path.join(ROOT, "ArtSource/dante/redesign-20261005/dante_fpv_arms.blend")
NAME = "dante-redesign-fpv-arms"

BONES = ["root", "torso", "arm-left", "arm-right"]
ARM_LENGTH = ftex.ARM_LENGTH       # ViewmodelArms.ArmLength
SECTION_CAP = 0.34                 # ViewmodelArms.ApplyRosterArm squeezes anything wider
BODY_ARM = 0.192                   # the body's arm, pivot to fingertips: the size this is written back at
SCALE = BODY_ARM / ARM_LENGTH
TRIANGLE_BUDGET = 900              # per arm
SQUARE = 0.92                      # a face takes a drawing only when it faces a view this squarely
THUMB_BLOCK = True

X, Y, Z = Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))


def paint(group, tone, under=None, end=None, lines=False):
    """The arm's islands where a face squarely faces a view; else one flat tone: `under` for a
    face turned away from the eye, `end` for a block's end, `tone` for the rest (the chamfers).

    `lines` is for the fist: its far end takes the end-on island, and the bevel between the back
    of the fist and that end is drawn too, so the finger lines run over the corner unbroken. That
    bevel is tilted only ALONG the lines, so they stretch along their own length and do not smear.
    """
    return ("paint", group, tone, under or tone, end or tone, lines)


def flat(name):
    return ("flat", name)


def tones(eye, side, under):
    return ("tones", eye, side, under)


class Arms(body.Part):
    """The body's Part (loft, block, chamfer), with this file's paint rules and its four bones."""

    def resolve_uvs(self):
        self.bm.normal_update()
        for face, spec, _uvp, _verts in self.jobs:
            n = face.normal
            kind = spec[0]
            view = None
            if kind == "tones":
                name = spec[1] if n.z > 0.45 else (spec[3] if n.z < -0.45 else spec[2])
            elif kind == "flat":
                name = spec[1]
            else:
                _, group, tone, under, end, lines = spec
                for v, d in ftex.VIEW_DIR.items():
                    if n.dot(Vector(d)) > SQUARE and (v != "ypos" or lines):
                        view = v
                if view is None and lines and abs(n.x) < 0.25 and n.y > 0.2:
                    view = max(("zpos", "ypos", "zneg"), key=lambda v: n.dot(Vector(ftex.VIEW_DIR[v])))
                name = end if abs(n.y) > 0.9 else (under if n.z < -0.3 else tone)
            for loop in face.loops:
                co = loop.vert.co
                if view is not None:
                    a, b = ftex.VIEW_AXES[view]
                    loop[self.uv].uv = ftex.atlas_uv(group + "." + view, co[a], co[b])
                else:
                    loop[self.uv].uv = ftex.atlas_uv(name, 0.5, 0.5)

    def to_rig_space(self, pivot):
        """Out of the first-person frame, into the body rig's: the inverse of `Extract`.

            right arm   x = -(pivot.x + Y s)   y = pivot.y + X s   z = pivot.z - Z s
            left arm    x =  (pivot.x + Y s)   y = pivot.y - X s   z = pivot.z - Z s
        Both are mirrorings (the first-person frame is Unity's, left handed), so every face is
        turned over afterwards to face out again.
        """
        left = body.ALL_BONES.index("arm-left")
        for v in self.bm.verts:
            c = v.co.copy()
            if v[self.bone] == left:
                v.co = Vector((pivot.x + c.y * SCALE, pivot.y - c.x * SCALE, pivot.z - c.z * SCALE))
            else:
                v.co = Vector((-(pivot.x + c.y * SCALE), pivot.y + c.x * SCALE, pivot.z - c.z * SCALE))
        bmesh.ops.reverse_faces(self.bm, faces=self.bm.faces[:])
        self.bm.normal_update()

    def to_object(self, armature):
        mesh = bpy.data.meshes.new(self.name)
        self.bm.to_mesh(mesh)
        obj = bpy.data.objects.new(self.name, mesh)
        bpy.context.scene.collection.objects.link(obj)
        for b in BONES:
            obj.vertex_groups.new(name=b)
        layer = self.bm.verts.layers.int["bone"]
        self.bm.verts.ensure_lookup_table()
        self.bm.verts.index_update()
        for v in self.bm.verts:
            obj.vertex_groups[body.ALL_BONES[v[layer]]].add([v.index], 1.0, "REPLACE")
        for poly in mesh.polygons:
            poly.use_smooth = True
        mesh.set_sharp_from_angle(angle=math.radians(body.SHARP_ANGLE))
        mod = obj.modifiers.new("Armature", "ARMATURE")
        mod.object = armature
        obj.parent = armature
        return obj


def along(part, name, bone, stations, mapping, chamfer):
    """A chamfered block along the arm: stations are (y, half X, half Z) or (y, half X, half Z, centre x, centre z)."""
    rows = []
    for st in stations:
        y, hx, hz = st[:3]
        cx, cz = (st[3], st[4]) if len(st) > 3 else (0.0, 0.0)
        rows.append(((cx, y, cz), hx, hz))
    return part.block(name, bone, X, Z, rows, mapping, chamfer)


# ---------------------------------------------------------------------------
# THE FIST. One solid block where the shared arm's hand block is (0.62 to 0.84), a little fuller
# at the knuckles than at the wrist, set 10 mm away from the thumb so the thumb has room inside
# the 0.34 the game allows. `t` is +1 with the thumb at +X (right arm), -1 at -X (left arm).
# ---------------------------------------------------------------------------

def build_fist(part, bone, group, t):
    y0, y1 = ftex.FIST
    cx = -ftex.FIST_SHIFT * t
    along(part, "fist", bone, [(y0, 0.154, 0.143, cx, 0.0), (0.775, 0.154, 0.150, cx, 0.0), (y1, 0.151, 0.146, cx, 0.0)],
          paint(group, "skin", "skin_shade", "skin_shade", lines=True), 0.026)
    if THUMB_BLOCK:
        # folded along the inner side of the fist, on the half the eye sees, narrowing to its tip
        along(part, "thumb", bone, [(0.648, 0.027, 0.060, t * 0.141, 0.030), (0.802, 0.021, 0.047, t * 0.146, 0.036)],
              tones("skin", "skin", "skin_shade"), 0.016)


def build_right(part):
    bone, group = "arm-right", "R"
    # the coat sleeve; its shoulder end is never seen
    along(part, "sleeve", bone, [(ftex.R_SLEEVE[0], 0.152, 0.146), (ftex.R_SLEEVE[1], 0.148, 0.142)],
          paint(group, "green", "green_dark", "lining"), 0.030)
    # the roll: a fat gold block round the arm, the widest thing on it
    along(part, "cuff", bone, [(ftex.R_CUFF[0], 0.168, 0.162), (ftex.R_CUFF[1], 0.168, 0.162)],
          paint(group, "gold", "gold_under", "gold_dark"), 0.022)
    along(part, "forearm", bone, [(ftex.R_FOREARM[0], 0.128, 0.120), (ftex.R_FOREARM[1], 0.122, 0.114)],
          paint(group, "skin", "skin_shade", "skin_shade"), 0.022)
    # the cord bracelet, 6 mm proud, and its gold knot on the face the eye sees
    along(part, "cord", bone, [(ftex.R_CORD[0], 0.129, 0.121), (ftex.R_CORD[1], 0.129, 0.121)],
          paint(group, "jade", "jade_under", "jade_under"), 0.007)
    mid = 0.5 * (ftex.R_CORD[0] + ftex.R_CORD[1])
    part.block("cord-knot", bone, X, Y, [((0.034, mid, 0.112), 0.019, 0.019), ((0.034, mid, 0.138), 0.016, 0.016)],
               tones("gold", "gold_under", "gold_dark"), 0.006)
    build_fist(part, bone, group, 1.0)


def build_left(part):
    bone, group = "arm-left", "L"
    # what is left of the sleeve: a short block on the shoulder, its far edge torn in teeth
    y0, y1 = ftex.L_STUB
    ring = lambda y, hx, hz, c: body.rect_ring((0.0, y, 0.0), X, Z, hx, hz, c)
    jag = (y1, y1 - 0.050, y1 - 0.006, y1 - 0.046, y1 - 0.014, y1 - 0.054, y1 - 0.004, y1 - 0.044)
    edge = [Vector((p.x, jag[j], p.z)) for j, p in enumerate(ring(0.0, 0.158, 0.150, 0.030))]
    under = [Vector((p.x, jag[j] - 0.016, p.z)) for j, p in enumerate(ring(0.0, 0.138, 0.130, 0.026))]
    part.loft("sleeve-stub", bone, [ring(y0, 0.140, 0.132, 0.014), ring(y0 + 0.024, 0.158, 0.150, 0.030), edge, under],
              lambda i, j: tones("green", "green", "green_dark") if i < 2 else flat("lining"), caps=(True, False))
    along(part, "arm", bone, [(ftex.L_UPPER[0], 0.135, 0.127), (ftex.L_UPPER[1], 0.130, 0.122)],
          paint(group, "skin", "skin_shade", "skin_shade"), 0.024)
    # the wrap, 14 mm proud of the arm: the clear step before the fist
    along(part, "wrap", bone, [(ftex.L_WRAP[0], 0.147, 0.139), (ftex.L_WRAP[1], 0.141, 0.133)],
          paint(group, "bandage", "bandage_shade", "bandage_shade"), 0.016)
    build_fist(part, bone, group, -1.0)


# ---------------------------------------------------------------------------
# THE BUILD
# ---------------------------------------------------------------------------

def rig_pivots():
    """The body rig's joints, world space, glTF axes: {name: (x, y, z)} for the four bones kept."""
    gltf, _ = bpv.read_glb(BODY_GLB)
    parent = {}
    for i, node in enumerate(gltf["nodes"]):
        for c in node.get("children", []):
            parent[c] = i
    out = {}
    for i, node in enumerate(gltf["nodes"]):
        if node.get("name") in BONES:
            p, k = [0.0, 0.0, 0.0], i
            while k is not None:
                t = gltf["nodes"][k].get("translation", [0.0, 0.0, 0.0])
                p = [p[a] + t[a] for a in range(3)]
                k = parent.get(k)
            out[node["name"]] = tuple(p)
    if set(out) != set(BONES):
        raise SystemExit("the body rig has no %s" % sorted(set(BONES) - set(out)))
    return out


def measure(part):
    """Checked in the first-person frame, before the turn back into rig space."""
    for bone, label in (("arm-right", "right"), ("arm-left", "left")):
        index = body.ALL_BONES.index(bone)
        pts = [v.co for v in part.bm.verts if v[part.bone] == index]
        lo = [min(p[a] for p in pts) for a in range(3)]
        hi = [max(p[a] for p in pts) for a in range(3)]
        tris = sum(len(f.verts) - 2 for f in part.bm.faces if f.verts[0][part.bone] == index)
        print("%s arm  x %.3f..%.3f (%.3f)  y %.3f..%.3f  z %.3f..%.3f (%.3f)  %d triangles"
              % (label, lo[0], hi[0], hi[0] - lo[0], lo[1], hi[1], lo[2], hi[2], hi[2] - lo[2], tris))
        if abs(lo[1]) > 1e-4 or abs(hi[1] - ARM_LENGTH) > 1e-4:
            raise SystemExit("%s arm runs %.4f to %.4f, not 0 to %.2f" % (label, lo[1], hi[1], ARM_LENGTH))
        if max(hi[0] - lo[0], hi[2] - lo[2]) > SECTION_CAP + 1e-4:
            raise SystemExit("%s arm is wider than %.2f: the game would squeeze it" % (label, SECTION_CAP))
        if tris > TRIANGLE_BUDGET:
            raise SystemExit("%s arm is %d triangles, over %d" % (label, tris, TRIANGLE_BUDGET))
        hand = [p for p in pts if p.y > ftex.FIST[0] + 0.03]
        print("   fist  x %.3f..%.3f  z %.3f..%.3f  (shared hand block: x -0.158..0.158, z -0.150..0.150, y 0.620..0.840)"
              % (min(p.x for p in hand), max(p.x for p in hand), min(p.z for p in hand), max(p.z for p in hand)))


def mesh_arrays(obj):
    """The mesh as glTF arrays: +y up, -z the way Blender's -y faces, v flipped, one bone a vertex."""
    mesh = obj.data
    mesh.calc_loop_triangles()
    normals = mesh.corner_normals
    uvs = mesh.uv_layers[0].data
    bone_of = [BONES.index(obj.vertex_groups[v.groups[0].group].name) for v in mesh.vertices]
    seen, pos, nrm, uv, joints, weights, idx = {}, [], [], [], [], [], []
    for tri in mesh.loop_triangles:
        for loop in tri.loops:
            vi = mesh.loops[loop].vertex_index
            co = mesh.vertices[vi].co
            n = normals[loop].vector
            t = uvs[loop].uv
            key = (vi, round(n.x, 4), round(n.y, 4), round(n.z, 4), round(t.x, 6), round(t.y, 6))
            if key not in seen:
                seen[key] = len(pos)
                pos.append((co.x, co.z, -co.y))
                nrm.append((n.x, n.z, -n.y))
                uv.append((t.x, 1.0 - t.y))
                joints.append((bone_of[vi], 0, 0, 0))
                weights.append((1.0, 0.0, 0.0, 0.0))
            idx.append(seen[key])
    return pos, nrm, uv, joints, weights, idx


def write_glb(arrays, pivots, out):
    pos, nrm, uv, joints, weights, idx = arrays
    blob = bytearray()
    views, accessors = [], []

    def add(values, fmt, kind, component, minmax=False):
        while len(blob) % 4:
            blob.append(0)
        start = len(blob)
        for v in values:
            blob.extend(struct.pack("<" + fmt * len(v), *v))
        acc = {"bufferView": len(views), "componentType": component, "count": len(values), "type": kind}
        if minmax:
            acc["min"] = [min(v[a] for v in values) for a in range(len(values[0]))]
            acc["max"] = [max(v[a] for v in values) for a in range(len(values[0]))]
        views.append({"buffer": 0, "byteOffset": start, "byteLength": len(blob) - start})
        accessors.append(acc)
        return len(accessors) - 1

    # Every rest rotation in the body rig is identity, so a bind matrix is the inverse translation.
    binds = [(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, -pivots[b][0], -pivots[b][1], -pivots[b][2], 1) for b in BONES]
    local = lambda b, parent: [pivots[b][a] - (pivots[parent][a] if parent else 0.0) for a in range(3)]
    body_gltf, _ = bpv.read_glb(BODY_GLB)
    material = dict(body_gltf["materials"][0])   # the body's own material settings, on this atlas
    material["name"] = NAME
    gltf = {
        "asset": {"version": "2.0", "generator": "Tumbang Preso character redesign, first-person arms (dante)"},
        "extensionsUsed": list(body_gltf.get("extensionsUsed", [])),
        "extras": {"prototype": "character-redesign-20261005", "use": "RosterBookBuilder.FirstPersonArmModels"},
        "scene": 0,
        "scenes": [{"nodes": [0], "name": NAME}],
        "nodes": [
            {"name": NAME, "children": [1, 5]},
            {"name": "root", "translation": local("root", None), "children": [2]},
            {"name": "torso", "translation": local("torso", "root"), "children": [3, 4]},
            {"name": "arm-left", "translation": local("arm-left", "torso")},
            {"name": "arm-right", "translation": local("arm-right", "torso")},
            {"name": "arms-mesh", "mesh": 0, "skin": 0},
        ],
        "skins": [{"joints": [1, 2, 3, 4], "inverseBindMatrices": add([tuple(float(x) for x in m) for m in binds], "f", "MAT4", 5126)}],
        "meshes": [{"name": "arms-mesh", "primitives": [{
            "attributes": {
                "POSITION": add(pos, "f", "VEC3", 5126, minmax=True),
                "NORMAL": add(nrm, "f", "VEC3", 5126),
                "TEXCOORD_0": add(uv, "f", "VEC2", 5126),
                "JOINTS_0": add(joints, "H", "VEC4", 5123),
                "WEIGHTS_0": add(weights, "f", "VEC4", 5126),
            },
            "indices": add([(i,) for i in idx], "I", "SCALAR", 5125),
            "material": 0,
            "mode": 4,
        }]}],
        "materials": [material],
        "images": [{"uri": ftex.ATLAS_NAME, "name": NAME + "-atlas"}],
        "textures": [{"sampler": 0, "source": 0, "name": NAME + "-atlas"}],
        "samplers": [{"magFilter": 9729, "minFilter": 9987}],
    }
    if not gltf["extensionsUsed"]:
        del gltf["extensionsUsed"]
    gltf["accessors"] = accessors
    gltf["bufferViews"] = views
    gltf["buffers"] = [{"byteLength": len(blob)}]
    for attempt in range(5):
        try:
            bpv.write_glb(out, gltf, blob)
            return
        except OSError:     # Windows sometimes refuses the write (Errno 22); a second later it takes it
            time.sleep(1.0)
    raise SystemExit("could not write " + out)


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    pivots = rig_pivots()
    blender = lambda p: Vector((p[0], -p[2], p[1]))     # glTF axes to Blender's
    print("pivots (Blender space): " + "  ".join("%s %s" % (b, tuple(round(c, 4) for c in blender(pivots[b]))) for b in BONES))
    right = blender(pivots["arm-right"])
    left = blender(pivots["arm-left"])
    if abs(left.x + right.x) > 1e-6 or abs(left.y - right.y) > 1e-6 or abs(left.z - right.z) > 1e-6:
        raise SystemExit("the body's arm pivots are not mirrored")

    data = bpy.data.armatures.new(NAME)
    armature = bpy.data.objects.new(NAME, data)
    bpy.context.scene.collection.objects.link(armature)
    bpy.context.view_layer.objects.active = armature
    bpy.ops.object.mode_set(mode="EDIT")
    made = {}
    for b, parent in (("root", None), ("torso", "root"), ("arm-left", "torso"), ("arm-right", "torso")):
        eb = data.edit_bones.new(b)
        eb.head = blender(pivots[b])
        eb.tail = eb.head + (Vector((math.copysign(0.06, eb.head.x), 0, 0)) if b.startswith("arm") else Vector((0, 0, 0.06)))
        if parent:
            eb.parent = made[parent]
        made[b] = eb
    bpy.ops.object.mode_set(mode="OBJECT")

    image_path = os.path.join(FOLDER, ftex.ATLAS_NAME)
    if not os.path.exists(image_path):
        raise SystemExit("paint the atlas first: py -3 tools/author_character_redesign_dante_fpv_arms_textures.py")
    image = bpy.data.images.load(image_path)
    material = bpy.data.materials.new(NAME)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    node = nodes.new("ShaderNodeTexImage")
    node.image = image
    material.node_tree.links.new(node.outputs[0], nodes["Principled BSDF"].inputs["Base Color"])
    nodes["Principled BSDF"].inputs["Roughness"].default_value = 1.0

    part = Arms("arms-mesh")
    build_right(part)
    build_left(part)
    part.resolve_uvs()
    print(" ".join("%s:%d" % (n, t) for n, t in part.pieces))
    measure(part)
    part.to_rig_space(left)
    obj = part.to_object(armature)
    obj.data.materials.append(material)
    write_glb(mesh_arrays(obj), pivots, OUT)

    os.makedirs(os.path.dirname(BLEND), exist_ok=True)
    image.filepath = bpy.path.relpath(image_path, start=os.path.dirname(BLEND))
    bpy.context.preferences.filepaths.save_version = 0   # no .blend1 beside it
    bpy.ops.wm.save_as_mainfile(filepath=BLEND)
    print("wrote " + BLEND)


if __name__ == "__main__":
    main()
