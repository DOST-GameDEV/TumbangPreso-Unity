"""Build the Payo audience: six very low poly precolonial Cordillera villagers, posed, one atlas.

    py -3 tools/author_payo_audience_textures.py
    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/author_payo_audience.py

Writes, all in ArtSource/payo/audience/:
    audience_<id>.glb       one static mesh per villager, one material, the shared atlas embedded
    audience.blend          the six side by side, for editing
    audience_lineup.png     the lineup render (Rafi at the end for scale)

WHY. docs/CHARACTER_REDESIGN_DANTE.md section 15.9 part K, item 5: the Payo map session asked, at
the owner's word, for "simple ancient filipino characters like rafi (doesnt have to be too
detailed) to serve as audience" on the basin wall. Dozens are on screen at once, so each one is a
handful of boxes (about 400 to 600 triangles), no skeleton, no clips, ONE material, ONE 512 atlas
(tools/author_payo_audience_textures.py). They keep the cast's build: the big box head with a
painted face, a short block body, short limbs, feet on zero, facing -y, Rafi's size (0.72 tall).

THE SIX (id: who, dress, pose):
    elder_man      lakay: red head cloth, indigo blanket over the shoulders, red bahag, STANDING with a spear
    woman          red tapis and chest wrap, bead collar, hair in a beaded bun, CHEERING arms up
    young_man      tattooed chest, indigo bahag, red band with a feather, betel bag on a strap, CLAPPING
    elder_woman    white blanket, white tapis, gray bun, SITTING on a ledge with a basket beside her
    boy            small red bahag, chopped fringe, CHEERING arms up
    girl           white tapis and wrap, bead collar, SITTING on a ledge, CLAPPING

PIVOTS. A standing villager's origin is between the feet on the ground. A sitting villager's origin
is the seat: the underside of the hips on the ledge top, with the thighs reaching 0.15 forward (-y)
and the shins hanging over the front, so place the origin about 0.12 back from the ledge's edge.
"""
import math
import os
import sys

import bpy
import bmesh
from mathutils import Matrix, Vector

TOOLS = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(TOOLS)
sys.path.insert(0, TOOLS)
import author_payo_audience_textures as tex  # noqa: E402  the cell names only

FOLDER = os.path.join(ROOT, "ArtSource/payo/audience")
ATLAS = os.path.join(FOLDER, "audience-atlas.png")
RAFI = os.path.join(ROOT, "Assets/TumbangPreso/Art/CharacterRedesign/rafi/rafi-redesign.glb")
GRID = tex.GRID
INSET = 1.5 / (tex.C * GRID)


# ---------------------------------------------------------------------------
# A PART: one box, mapped by the side each face looks at, then placed.
# ---------------------------------------------------------------------------

def cell_uv(cell, un, vn):
    i = tex.CELLS[cell]
    r, c = divmod(i, GRID)
    s = 1.0 / GRID
    un = min(max(un, 0.0), 1.0)
    vn = min(max(vn, 0.0), 1.0)
    return (c * s + INSET + un * (s - 2 * INSET), 1.0 - (r + 1) * s + INSET + vn * (s - 2 * INSET))


def part(parts, size, cells, matrix, bevel=0.0, open_ends=False, taper=1.0):
    """`size` is the full (x, y, z); `cells` one cell name or a dict by side ("+x" ... "-z").
    The box is mapped in its own space, so its paint rides it however it is posed."""
    hx, hy, hz = (s / 2 for s in size)
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        v.co = Vector((v.co.x * 2 * hx, v.co.y * 2 * hy, v.co.z * 2 * hz))
    if taper != 1.0:   # the top end narrowed, for a spear's blade
        for v in bm.verts:
            if v.co.z > 0:
                v.co.x *= taper
                v.co.y *= taper
    if open_ends:
        bmesh.ops.delete(bm, geom=[f for f in bm.faces if abs(f.normal.z) > .5], context="FACES_ONLY")
    if bevel > 0:
        bmesh.ops.bevel(bm, geom=list(bm.edges), offset=bevel, segments=1, affect="EDGES", clamp_overlap=True)
    uv = bm.loops.layers.uv.new("UVMap")
    if isinstance(cells, str):
        cells = {k: cells for k in ("+x", "-x", "+y", "-y", "+z", "-z")}
    for f in bm.faces:
        n = f.normal
        ax = max(range(3), key=lambda k: abs(n[k]))
        key = ("+" if n[ax] > 0 else "-") + "xyz"[ax]
        cell = cells[key]
        for loop in f.loops:
            x, y, z = loop.vert.co
            if ax == 1:     # front or back: across x, up z; the back is mirrored so it reads from behind
                un = (x + hx) / (2 * hx) if key == "-y" else (hx - x) / (2 * hx)
                vn = (z + hz) / (2 * hz)
            elif ax == 0:   # a side: the back of the head at u 0 seen from either side
                un = (hy - y) / (2 * hy) if key == "+x" else (y + hy) / (2 * hy)
                vn = (z + hz) / (2 * hz)
            else:
                un, vn = (x + hx) / (2 * hx), (y + hy) / (2 * hy)
            loop[uv].uv = cell_uv(cell, un, vn)
    bm.transform(matrix)
    parts.append(bm)


def T(x, y=0.0, z=0.0):
    return Matrix.Translation(Vector((x, y, z)) if not isinstance(x, (tuple, Vector)) else Vector(x))


def R(ex=0.0, ey=0.0, ez=0.0):
    """Degrees, applied x then y then z. Negative x swings a hanging limb forward (-y); negative y
    lifts a left (+x) arm out sideways, positive y a right one."""
    return (Matrix.Rotation(math.radians(ez), 4, "Z") @ Matrix.Rotation(math.radians(ey), 4, "Y")
            @ Matrix.Rotation(math.radians(ex), 4, "X"))


def limb(parts, pivot, rot, length, thick, cells, end_cell=None):
    """A box hanging `length` from `pivot`, turned by `rot`. Returns the far end and the frame."""
    frame = T(pivot) @ rot
    c = cells if isinstance(cells, dict) else {k: cells for k in ("+x", "-x", "+y", "-y", "+z", "-z")}
    if end_cell:
        c = dict(c, **{"-z": end_cell})
    part(parts, (thick[0], thick[1], length + 0.01), c, frame @ T(0, 0, -length / 2))
    return frame @ Vector((0, 0, -length)), frame


def aim(direction):
    """The turn that points a hanging limb (-z) along `direction`."""
    return Vector(direction).normalized().to_track_quat("-Z", "Y").to_matrix().to_4x4()


# ---------------------------------------------------------------------------
# A VILLAGER
# ---------------------------------------------------------------------------

ADULT = dict(leg=0.18, torso=0.17, tw=0.22, td=0.15, head=(0.28, 0.26, 0.27), arm=0.115, at=0.075)


def villager(v):
    p = []
    s = v.get("scale", 1.0)
    d = dict(ADULT, **v.get("dims", {}))
    leg, tw, td = d["leg"], d["tw"], d["td"]
    sit = v["pose"] == "sit"
    hip_z = 0.05 if sit else leg           # sitting: the thighs lie on the seat
    tz0 = hip_z
    tz1 = tz0 + d["torso"]
    skin = v["skin"]

    # Legs and feet.
    for side in (1, -1):
        hip = Vector((side * 0.055, 0.0, hip_z))
        if sit:
            knee, _ = limb(p, hip, R(-90), 0.15, (0.082, 0.10), skin)
            ankle, _ = limb(p, knee, R(-4), leg - 0.035, (0.078, 0.09), skin)
        else:
            ankle, _ = limb(p, hip, R(0, side * -2), leg - 0.035, (0.08, 0.10), skin)
        part(p, (0.085, 0.13, 0.035), dict(skin_cells(skin), **{"-z": "sole"}),
             T(ankle.x, ankle.y - 0.015, ankle.z - 0.0175))

    # Torso.
    tcell = v.get("torso_cell", skin)
    part(p, (tw, td, d["torso"]), dict(skin_cells(tcell), **{"+z": skin, "-z": skin}),
         T(0, 0, (tz0 + tz1) / 2), bevel=0.012)

    # Head, hair and what is on it. The face is the front of the box, like the cast.
    hw, hd, hh = d["head"]
    hz = tz1 + hh / 2 - 0.005
    hfwd = v.get("head_fwd", 0.0)
    head_m = T(0, -hfwd, hz)
    part(p, (hw, hd, hh), {"-y": v["face"], "+y": v["hair_back"], "+x": v["side"], "-x": v["side"],
                           "+z": v["hair"], "-z": skin}, head_m, bevel=0.018)
    if v.get("hair_cap"):
        part(p, (hw + 0.024, hd + 0.03, 0.075), {"-y": v["hair"], "+y": v["hair"], "+x": v["hair"], "-x": v["hair"],
                                                 "+z": v["hair"], "-z": v["hair"]},
             head_m @ T(0, 0.01, hh / 2 + 0.005), bevel=0.02)
        part(p, (hw + 0.012, 0.04, hh * 0.55), v["hair"], head_m @ T(0, hd / 2 + 0.008, hh * 0.12), bevel=0.01)
    if v.get("headcloth"):
        part(p, (hw + 0.03, hd + 0.03, 0.05), v["headcloth"], head_m @ T(0, 0, hh / 2 - 0.035), open_ends=True)
        part(p, (0.05, 0.04, 0.045), v["headcloth"], head_m @ T(hw / 2 + 0.01, hd / 2 - 0.02, hh / 2 - 0.04))
        part(p, (0.035, 0.012, 0.10), v["headcloth"],
             head_m @ T(hw / 2 + 0.015, hd / 2 - 0.005, hh / 2 - 0.10) @ R(8, 0, 0))
    if v.get("feather"):
        part(p, (0.008, 0.035, 0.14), "feather", head_m @ T(-hw / 2 - 0.01, 0.02, hh / 2 + 0.04) @ R(-12, 14, 0))
    if v.get("long_hair"):
        part(p, (hw - 0.03, 0.035, hh * 0.95), v["hair_back"], head_m @ T(0, hd / 2 + 0.012, -hh * 0.12), bevel=0.01)
    if v.get("bun"):
        part(p, (0.12, 0.08, 0.09), v["bun"], head_m @ T(0, hd / 2 + 0.03, hh / 2 - 0.05), bevel=0.012)

    # Arms with elbows, each segment aimed along a direction written for the LEFT arm.
    for side, key in ((1, "left"), (-1, "right")):
        up, fore = v["arms"][key]
        up = Vector((side * up[0], up[1], up[2]))
        fore = Vector((side * fore[0], fore[1], fore[2]))
        shoulder = Vector((side * (tw / 2 + d["at"] / 2 - 0.008), 0.0, tz1 - 0.035))
        elbow, _ = limb(p, shoulder, aim(up), d["arm"], (d["at"], d["at"]), v.get("arm_cell", skin))
        hand, _ = limb(p, elbow, aim(fore), d["arm"], (d["at"] - 0.006, d["at"] - 0.006), skin)
        v["_hand_" + key] = hand

    # Clothes.
    for item in v.get("wear", []):
        kind, cell = item[0], item[1]
        if kind == "bahag":
            part(p, (tw + 0.015, td + 0.015, 0.035), cell, T(0, 0, tz0 + 0.015))
            if sit:
                part(p, (0.085, 0.13, 0.012), cell, T(0, -0.08, hip_z + 0.058))
                part(p, (0.085, 0.012, 0.07), cell, T(0, -0.15, hip_z + 0.02))
            else:
                part(p, (0.085, 0.012, 0.15), cell, T(0, -td / 2 - 0.009, tz0 - 0.06))
                part(p, (0.075, 0.012, 0.12), cell, T(0, td / 2 + 0.009, tz0 - 0.045))
        elif kind == "tapis":
            if sit:
                part(p, (tw + 0.02, 0.19, 0.075), cell, T(0, -0.05, hip_z + 0.03), bevel=0.01)
                part(p, (tw + 0.02, 0.02, 0.10), cell, T(0, -0.145, hip_z - 0.035))
            else:
                part(p, (tw + 0.02, td + 0.03, 0.16), cell, T(0, 0, tz0 - 0.05), bevel=0.01)
        elif kind == "wrap":
            part(p, (tw + 0.012, td + 0.012, 0.07), cell, T(0, 0, tz1 - 0.06))
        elif kind == "collar":
            part(p, (tw * 0.75, 0.012, 0.035), cell, T(0, -td / 2 - 0.012, tz1 - 0.022))
        elif kind == "blanket":
            # Wrapped over both shoulders and down the back to the hip, open in front.
            wide = tw + 2 * d["at"] + 0.01
            part(p, (wide, td + 0.06, 0.06), cell, T(0, 0.008, tz1 - 0.02), bevel=0.014)
            part(p, (wide, 0.035, d["torso"] + 0.03), cell, T(0, td / 2 + 0.035, (tz0 + tz1) / 2 - 0.03), bevel=0.008)
            for side in (1, -1):
                part(p, (0.075, 0.035, d["torso"] + 0.0), cell,
                     T(side * (tw / 2 - 0.02), -td / 2 - 0.025, (tz0 + tz1) / 2 - 0.035), bevel=0.008)
        elif kind == "betel":
            part(p, (0.07, 0.04, 0.075), cell, T(tw / 2 + 0.02, -0.02, tz0 + 0.0), bevel=0.008)
            part(p, (0.025, 0.01, 0.27), cell,
                 T(0, -td / 2 - 0.008, (tz0 + tz1) / 2 + 0.01) @ R(0, -42, 0))
            part(p, (0.025, 0.01, 0.27), cell,
                 T(0, td / 2 + 0.008, (tz0 + tz1) / 2 + 0.01) @ R(0, -42, 0))

    for prop in v.get("props", []):
        prop(p, v, d, tz0, tz1)

    me = bpy.data.meshes.new(v["id"])
    bm = bmesh.new()
    for part_bm in p:
        tmp = bpy.data.meshes.new("tmp")
        part_bm.to_mesh(tmp)
        bm.from_mesh(tmp)
        bpy.data.meshes.remove(tmp)
        part_bm.free()
    if s != 1.0:
        bm.transform(Matrix.Scale(s, 4))
    bm.to_mesh(me)
    bm.free()
    for poly in me.polygons:
        poly.use_smooth = False
    return me


def skin_cells(cell):
    return {k: cell for k in ("+x", "-x", "+y", "-y", "+z", "-z")}


def spear(p, v, d, tz0, tz1):
    """Held in the right hand, upright beside him, butt on the ground."""
    hand = v["_hand_right"]
    x, y = hand.x, hand.y
    part(p, (0.024, 0.024, 0.88), "wood", T(x, y, 0.44))
    part(p, (0.034, 0.034, 0.03), "iron", T(x, y, 0.885))
    part(p, (0.055, 0.014, 0.14), "iron", T(x, y, 0.97), taper=0.12)


def basket(p, v, d, tz0, tz1):
    """A lidded storage basket on the ledge at her left."""
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=8, radius1=0.075, radius2=0.09, depth=0.14)
    uv = bm.loops.layers.uv.new("UVMap")
    for f in bm.faces:
        for loop in f.loops:
            co = loop.vert.co
            a = (math.atan2(co.y, co.x) / (2 * math.pi)) % 1.0 * 2 % 1.0
            loop[uv].uv = cell_uv("basket", a if abs(f.normal.z) < .7 else co.x * 5 + .5,
                                  co.z / 0.14 + .5 if abs(f.normal.z) < .7 else co.y * 5 + .5)
    bm.transform(T(d["tw"] / 2 + 0.17, 0.0, 0.07))
    p.append(bm)
    part(p, (0.19, 0.19, 0.03), "wood", T(d["tw"] / 2 + 0.17, 0.0, 0.155) @ R(0, 0, 22.5), bevel=0.006)


# Arms: (shoulder (x, y, z), elbow (x, y, z)) in degrees, written for the LEFT arm; the right
# mirrors y and z. Hanging is (0, 0, 0).
HANG = ((0.14, 0.0, -1.0), (0.06, -0.18, -1.0))
CHEER_L = ((0.75, -0.1, 0.65), (0.3, -0.15, 0.95))
CHEER_R = ((0.8, -0.05, 0.55), (0.45, -0.1, 0.9))
CLAP_L = ((0.12, -0.75, -0.65), (-0.6, -0.3, 0.75))
LAP_L = ((0.05, -0.4, -0.92), (-0.35, -0.95, -0.1))
HOLD_SPEAR = ((0.1, -0.25, -0.96), (0.0, -1.0, 0.25))
WAVE = ((0.35, -0.2, 0.92), (0.1, -0.1, 1.0))

VILLAGERS = [
    dict(id="elder_man", pose="stand", skin="skin_old", face="face_elder_man", side="side_gray",
         hair="hair_gray", hair_back="hair_back_gray", hair_cap=True, head_fwd=0.015, dims=dict(leg=0.17),
         headcloth="headcloth_red", wear=[("bahag", "bahag_red"), ("blanket", "blanket_indigo")],
         arms=dict(left=HANG, right=HOLD_SPEAR), props=[spear]),
    dict(id="woman", pose="stand", skin="skin", face="face_woman", side="side_woman", hair="hair_black",
         hair_back="hair_back_black", bun="hair_bun", long_hair=True, hair_cap=True, dims=dict(tw=0.205, td=0.14),
         wear=[("tapis", "tapis_red"), ("wrap", "tapis_red"), ("collar", "beads")],
         arms=dict(left=CHEER_L, right=CHEER_R)),
    dict(id="young_man", pose="stand", skin="skin_dark", face="face_man", side="side_black", hair="hair_black",
         hair_back="hair_back_black", hair_cap=True, headcloth="headcloth_red", feather=True,
         torso_cell="skin_tattoo", wear=[("bahag", "bahag_indigo"), ("betel", "betel_bag")],
         arms=dict(left=CLAP_L, right=CLAP_L)),
    dict(id="elder_woman", pose="sit", skin="skin_old", face="face_elder_woman", side="side_gray",
         hair="hair_gray", hair_back="hair_back_gray", bun="hair_gray", hair_cap=True, head_fwd=0.012,
         dims=dict(tw=0.205, td=0.14, leg=0.17),
         wear=[("tapis", "tapis_white"), ("blanket", "blanket_white")],
         arms=dict(left=LAP_L, right=LAP_L), props=[basket]),
    dict(id="boy", pose="stand", scale=0.78, skin="skin", face="face_boy", side="side_black", hair="hair_black",
         hair_back="hair_back_black", hair_cap=True, dims=dict(leg=0.15, torso=0.15, head=(0.30, 0.28, 0.29)),
         wear=[("bahag", "bahag_red")],
         arms=dict(left=CHEER_L, right=WAVE)),
    dict(id="girl", pose="sit", scale=0.78, skin="skin", face="face_girl", side="side_woman", hair="hair_black",
         hair_back="hair_back_black", long_hair=True, hair_cap=True, dims=dict(leg=0.15, torso=0.15, tw=0.2, td=0.14, head=(0.30, 0.28, 0.29)),
         wear=[("tapis", "tapis_white"), ("wrap", "tapis_white"), ("collar", "beads")],
         arms=dict(left=CLAP_L, right=CLAP_L)),
]


# ---------------------------------------------------------------------------

def material():
    mat = bpy.data.materials.new("payo_audience")
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = nt.nodes["Principled BSDF"]
    img = nt.nodes.new("ShaderNodeTexImage")
    img.image = bpy.data.images.load(ATLAS)
    img.interpolation = "Closest"
    nt.links.new(img.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.9
    return mat


def export(obj, path):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.gltf(filepath=path, use_selection=True, export_format="GLB",
                              export_apply=True, export_animations=False, export_skins=False)


def lineup(objs):
    sc = bpy.context.scene
    bpy.ops.import_scene.gltf(filepath=RAFI)
    rafi = [o for o in sc.objects if o.type in ("MESH", "ARMATURE") and o not in objs and o.parent is None]
    for o in rafi:
        o.location.x += len(objs) * 0.55
    # The ledge under the sitters.
    for o in objs:
        if o.get("sit"):
            o.location.z = 0.24
            bpy.ops.mesh.primitive_cube_add(size=1, location=(o.location.x, 0.105, 0.12))
            ledge = bpy.context.object
            ledge.scale = (0.5, 0.45, 0.24)
            ledge.data.materials.append(stone())
    bpy.ops.mesh.primitive_plane_add(size=12, location=(1.5, 0, 0))
    ground = bpy.context.object
    ground.data.materials.append(stone(0.36))
    sc.render.engine = "BLENDER_EEVEE"
    sc.render.resolution_x, sc.render.resolution_y = 1600, 700
    w = bpy.data.worlds.new("w")
    sc.world = w
    w.color = (0.62, 0.68, 0.74)
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    sc.collection.objects.link(cam)
    sc.camera = cam
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = 4.3
    ctr = Vector(((len(objs)) * 0.55 / 2, 0, 0.38))
    dirn = Vector((0.35, -1.0, 0.32)).normalized()
    cam.location = ctr + dirn * 8
    cam.rotation_euler = (ctr - cam.location).to_track_quat("-Z", "Y").to_euler()
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sc.collection.objects.link(sun)
    sun.data.energy = 3.2
    sun.rotation_euler = (math.radians(48), math.radians(8), math.radians(28))
    fill = bpy.data.objects.new("fill", bpy.data.lights.new("fill", "SUN"))
    sc.collection.objects.link(fill)
    fill.data.energy = 0.8
    fill.rotation_euler = (math.radians(70), 0, math.radians(-140))
    sc.render.filepath = os.path.join(FOLDER, "audience_lineup.png")
    bpy.ops.render.render(write_still=True)


def stone(v=0.48):
    m = bpy.data.materials.new("stone")
    m.use_nodes = True
    m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (v, v * 0.97, v * 0.9, 1)
    return m


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    mat = material()
    objs = []
    for i, v in enumerate(VILLAGERS):
        me = villager(v)
        me.materials.append(mat)
        obj = bpy.data.objects.new("audience_" + v["id"], me)
        bpy.context.scene.collection.objects.link(obj)
        export(obj, os.path.join(FOLDER, "audience_%s.glb" % v["id"]))
        tris = sum(len(p.vertices) - 2 for p in me.polygons)
        lo = min(c[2] for c in obj.bound_box)
        hi = max(c[2] for c in obj.bound_box)
        print("AUDIENCE %-12s %4d tris  z %.3f to %.3f" % (v["id"], tris, lo, hi))
        obj.location.x = i * 0.55
        obj["sit"] = v["pose"] == "sit"
        objs.append(obj)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(FOLDER, "audience.blend"))
    if "--no-render" not in sys.argv:
        lineup(objs)


main()
