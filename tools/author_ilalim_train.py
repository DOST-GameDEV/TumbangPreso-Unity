"""Model the LRT-1 train for the Ilalim ng Tulay rebuild (ILALIM-1.3, train kit).

  py -3 tools/author_ilalim_textures_train.py          # paint the textures first
  blender -b --python tools/author_ilalim_train.py -- [--preview N]

Writes ArtSource/ilalim/train.blend: the collection "lrt_train", every object parented to the
empty "lrt_train_root" at the origin. With --preview it appends the guideway from
ArtSource/ilalim/lrt_kit.blend (after saving, so train.blend holds only the train) and writes
versioned renders to Logs/ilalim-blender/train_<shot>_vN.png.

THE REFERENCE (research.md sections 4 and 8; Commons photos of the 1st generation cars on Taft
and the 3rd and 4th generation cars): short multi-car LRT-1 sets, yellow and blue, a boxy body
with a big dark front mask, a single-arm pantograph, roof air-con pods, outside-frame bogies.
The owner's brief: yellow body with a blue band, stylized and chunky.

THE CONTRACT (IlalimNgTulayBuilder.cs, guide section 1):
  * the consist is 15.6 m long (TrainConsistHalfLength 7.8): three 4.88 m cars with 0.3 m
    bellows gaps, car centres at y -5.18, 0, +5.18. The cab noses end at y +/-7.62 and the
    bumpers and couplers reach +/-7.74, inside the 7.8;
  * at most 2.6 m wide: the body is 2.5 m, and the proudest window frames reach x +/-1.294;
  * the ORIGIN is the consist centre ON THE RAIL HEAD: z = 0 is the rail head (world 9.19),
    x = 0 the track centre (the rails at x +/-0.72). The lowest point of the train is the
    wheel treads at z = -0.005, sunk 5 mm into the rail, so the builder's measured ride
    height (RailHead minus the bounds' minimum) seats it without lifting;
  * it runs along y, the game's z, so LrtTrainFlyby can move the root;
  * clearance: the pantograph's contact strip tops out at z 4.36 (world 13.55). The contact
    wire hangs at 13.74 at the gantries and sags to 13.56 mid-span, so the strip touches the
    wire mid-span and runs 0.19 m under it at the gantries. The body is 2.6 m wide against
    gantry posts at x +/-4.3 on a track at +/-2.35: 0.37 m clear at the widest post base.

ROLE HUES (Art_Direction.md section 1): the yellow is a lemon-to-mustard (#e2b83a, hue 46
degrees; offence orange #f87020 is 22 degrees); the blue is a deep navy band (#2a3854) and
teal-grey doors (#4f6a6e), never a mid blue near defence #0080e8. The destination blinds are
pale lemon, not amber.

THE HOUSE STYLE: a one-piece lofted body shell per car with a live Bevel; the cab nose is a
loft that narrows, drops and rakes back, so it reads rounded without thin parts. NO TWO
SURFACES SHARE A PLANE: the navy band stands 1.5 cm proud of the yellow; the glass 0.7 cm
proud of the band; each window's cream frame 4 cm proud, so the glass reads RECESSED inside
its frame (never coplanar with it). Doors sit in a dark surround with a gap between the leaves.
Detail lives in the painted textures (tools/author_ilalim_textures_train.py).

DRAWN WEATHERING: roof soot is painted into train_roof; two positional overlays go on through
the UV maps UVGrime (v = metres below the roof gutter, short brown rain tongues) and
UVSplash (v = metres above the body's lower edge, a brown dust band with wheel fans).

DESTINATIONS: the north cab (+y) reads FERNANDO POE JR., the south cab (-y) BACLARAN, LRT-1's
real termini of the Taft line's old span. The side route strip reads BACLARAN - FERNANDO POE JR.
"""
import math
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import author_ilalim_lrt as L            # noqa: E402  (read-only: Buf, fillet, rounded_rect, lighting)

ROOT = L.ROOT
SOURCE = L.SOURCE
TEXTURES = L.TEXTURES
PREVIEWS = L.PREVIEWS
UP = Vector((0, 0, 1))

CAR_L = 4.88
GAP = 0.3
CARS = (-5.18, 0.0, 5.18)
HW = 1.25                 # body half width
ZB = 0.62                 # body lower edge above the rail head
ZT = 3.37                 # roof crown
GUTTER = 2.80
GAUGE = 0.72
WHEEL_R = 0.30
NOSE = 0.62               # length of the cab nose loft
RAKE = 0.28               # how far the cab front leans back at the top
HEAD_TOP = 4.36           # pantograph contact strip top

# name: (texture or None, tint or flat colour, grimed)
MATERIALS = {
    "train_body":   ("train_body", None, True),
    "train_roof":   ("train_roof", None, False),
    "train_under":  ("train_under", None, False),
    "train_band":   ("train_band", None, True),
    "train_door":   ("train_door", None, True),
    "train_glass":  ("train_glass", None, False),
    "train_rubber": ("train_rubber", None, False),
    "train_bogie":  ("train_under", (0.85, 0.85, 0.85), False),
    "train_ac":     ("train_roof", (1.06, 1.06, 1.05), False),
    "train_frame":  (None, (0.78, 0.70, 0.52), False),     # cream window frames
    "train_mask":   (None, (0.028, 0.030, 0.034), False),  # the near-black front mask
    "train_gutter": (None, (0.42, 0.42, 0.40), False),
    "train_bumper": (None, (0.06, 0.06, 0.065), False),
    "train_lamp":   (None, (0.95, 0.92, 0.78), False),
    "train_lamp_red": (None, (0.50, 0.03, 0.03), False),
    "train_wheel":  (None, (0.13, 0.12, 0.11), False),
    "train_panto":  (None, (0.12, 0.12, 0.12), False),
    "train_insulator": (None, (0.36, 0.24, 0.18), False),
    "train_dest_baclaran": ("train_dest_baclaran", "decal", False),
    "train_dest_fpj": ("train_dest_fpj", "decal", False),
    "train_route":  ("train_route", "decal", False),
}


def material(name):
    m = bpy.data.materials.get(name)
    if m:
        return m
    tex, tint, grimed = MATERIALS[name]
    m = bpy.data.materials.new(name)
    if m.node_tree is None:
        m.use_nodes = True
    nodes, links = m.node_tree.nodes, m.node_tree.links
    bsdf = nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = 0.35 if "glass" in name else 0.8
    if tex is None:
        bsdf.inputs["Base Color"].default_value = (*tint, 1)
        m.diffuse_color = (*tint, 1)
        return m
    uv = nodes.new("ShaderNodeUVMap")
    albedo = nodes.new("ShaderNodeTexImage")
    albedo.image = bpy.data.images.load(str(TEXTURES / f"{tex}_albedo.png"), check_existing=True)
    links.new(uv.outputs["UV"], albedo.inputs["Vector"])
    colour = albedo.outputs["Color"]
    if tint == "decal":
        # The blinds and signs glow a little, as a lit sign does in the viaduct's shade.
        albedo.extension = "EXTEND"
        links.new(colour, bsdf.inputs["Base Color"])
        if name.startswith("train_dest"):
            links.new(colour, bsdf.inputs["Emission Color"])
            bsdf.inputs["Emission Strength"].default_value = 0.35
        m.diffuse_color = (0.3, 0.3, 0.3, 1)
        return m
    if tint is not None:
        mix = nodes.new("ShaderNodeMix")
        mix.data_type, mix.blend_type = "RGBA", "MULTIPLY"
        mix.inputs["Factor"].default_value = 1.0
        links.new(colour, mix.inputs[6])
        mix.inputs[7].default_value = (*tint, 1)
        colour = mix.outputs[2]
    if grimed:
        for image, layer in (("train_drips", "UVGrime"), ("train_skirt", "UVSplash")):
            guv = nodes.new("ShaderNodeUVMap")
            guv.uv_map = layer
            gtex = nodes.new("ShaderNodeTexImage")
            gtex.image = bpy.data.images.load(str(TEXTURES / f"{image}.png"), check_existing=True)
            gtex.image.colorspace_settings.name = "Non-Color"
            links.new(guv.outputs["UV"], gtex.inputs["Vector"])
            mul = nodes.new("ShaderNodeMix")
            mul.data_type, mul.blend_type = "RGBA", "MULTIPLY"
            mul.inputs["Factor"].default_value = 1.0
            links.new(colour, mul.inputs[6])
            links.new(gtex.outputs["Color"], mul.inputs[7])
            colour = mul.outputs[2]
    links.new(colour, bsdf.inputs["Base Color"])
    m.diffuse_color = (0.6, 0.55, 0.4, 1)
    return m


class TBuf(L.Buf):
    """The guideway's Buf with the train's own grime mapping and materials. `keep_uv` buffers
    (the painted signs) write their own 0..1 UVs and get no projection."""

    def __init__(self, name, keep_uv=False):
        super().__init__(name)
        self.keep_uv = keep_uv
        self.round_shell = False
        if keep_uv:
            self.uv = self.bm.loops.layers.uv.verify()

    def world_uvs(self):
        if self.keep_uv:
            return
        super().world_uvs()

    def grime_uvs(self):
        drip = self.bm.loops.layers.uv.new("UVGrime")
        splash = self.bm.loops.layers.uv.new("UVSplash")
        clean = 0.999
        for f in self.bm.faces:
            n = f.normal
            cz = f.calc_center_median().z
            if self.round_shell and 0.12 < abs(n.x) < 0.97 and abs(n.y) < 0.5:
                f.smooth = True
            side = abs(n.z) <= 0.7
            t = UP.cross(n)
            t = t.normalized() if t.length > 1e-6 else Vector((1, 0, 0))
            for l in f.loops:
                co = l.vert.co
                u = co.dot(t) / 8.0
                l[drip].uv = (u, min(clean, max(0.0, (GUTTER - co.z) / 2.0)) if side and cz < GUTTER else clean)
                l[splash].uv = (u, min(clean, max(0.0, (co.z - ZB) / 1.2)) if side and cz < ZB + 1.3 else clean)

    def finish(self, collection, bevel=0.03, segments=2, smooth=True, angle=35):
        self.world_uvs()
        mesh = bpy.data.meshes.new(self.name)
        self.bm.to_mesh(mesh)
        self.bm.free()
        for m in self.mats:
            mesh.materials.append(material(m))
        for p in mesh.polygons:
            p.use_smooth = p.use_smooth or (smooth and p.area < 0.2)
        obj = bpy.data.objects.new(self.name, mesh)
        collection.objects.link(obj)
        if bevel:
            mod = obj.modifiers.new("Bevel", "BEVEL")
            mod.width, mod.segments = bevel, segments
            mod.limit_method, mod.angle_limit = "ANGLE", math.radians(angle)
            mod.harden_normals, mod.use_clamp_overlap = True, True
        return obj


# ------------------------------------------------------------------ helpers

fillet, rounded_rect = L.fillet, L.rounded_rect


def rr_yz(yc, zc, hw, hh, r):
    return [(yc + a, zc + b) for a, b in rounded_rect(hw, hh, r)]


def slab_x(buf, yz, x0, x1, mat):
    """A closed y-z outline swept across x from x0 to x1 (a panel on a car side)."""
    return buf.loft([[Vector((x0, y, z)) for y, z in yz], [Vector((x1, y, z)) for y, z in yz]], mat)


def ring(buf, outer, inner, a0, a1, to3d, mat):
    """A frame: the band between two closed outlines of equal point count, given thickness
    along the third axis from a0 to a1. `to3d(p, a)` places an outline point at depth a."""
    k = len(outer)
    vo0 = [buf.bm.verts.new(to3d(p, a0)) for p in outer]
    vo1 = [buf.bm.verts.new(to3d(p, a1)) for p in outer]
    vi0 = [buf.bm.verts.new(to3d(p, a0)) for p in inner]
    vi1 = [buf.bm.verts.new(to3d(p, a1)) for p in inner]
    faces = []
    for j in range(k):
        n = (j + 1) % k
        faces.append(buf.bm.faces.new((vo0[j], vo0[n], vo1[n], vo1[j])))
        faces.append(buf.bm.faces.new((vi0[n], vi0[j], vi1[j], vi1[n])))
        faces.append(buf.bm.faces.new((vo1[j], vo1[n], vi1[n], vi1[j])))
        faces.append(buf.bm.faces.new((vi0[j], vi0[n], vo0[n], vo0[j])))
    bmesh.ops.recalc_face_normals(buf.bm, faces=faces)
    idx = buf.mi(mat)
    for f in faces:
        f.material_index = idx


def frame_x(buf, sgn, yc, zc, hw, hh, r, width, x0, x1, mat):
    """A rounded window frame ring on a car side (sgn = +1 east side, -1 west)."""
    outer = rounded_rect(hw + width, hh + width, r + width)
    inner = rounded_rect(hw, hh, r)
    ring(buf, outer, inner, x0, x1, lambda p, a: Vector((sgn * a, yc + p[0], zc + p[1])), mat)


def cyl(buf, c, axis, r, depth, mat, sides=16):
    q = Vector(axis).normalized().to_track_quat("Z", "Y").to_matrix().to_4x4()
    res = bmesh.ops.create_cone(buf.bm, cap_ends=True, segments=sides, radius1=r, radius2=r, depth=depth,
                                matrix=Matrix.Translation(c) @ q)
    idx = buf.mi(mat)
    for f in {f for v in res["verts"] for f in v.link_faces}:
        f.material_index = idx
        f.smooth = True


def rbox(buf, cx, cy, z0, z1, hx, hy, r, mat, top_scale=1.0):
    buf.extrude_z(rounded_rect(hx, hy, r), z0, z1, mat, top_scale=top_scale, offset=(cx, cy))


def sign_panel(buf, corners, mat):
    """A thin painted board from four front corners (bottom-left, bottom-right, top-right,
    top-left) and a depth vector: front face UV 0..1, the edges sample the board's border."""
    (bl, br, tr, tl), back = corners
    front = [buf.bm.verts.new(p) for p in (bl, br, tr, tl)]
    rear = [buf.bm.verts.new(p - back) for p in (bl, br, tr, tl)]
    faces = [buf.bm.faces.new(front), buf.bm.faces.new(list(reversed(rear)))]
    for j in range(4):
        n = (j + 1) % 4
        faces.append(buf.bm.faces.new((front[n], front[j], rear[j], rear[n])))
    bmesh.ops.recalc_face_normals(buf.bm, faces=faces)
    idx = buf.mi(mat)
    for f in faces:
        f.material_index = idx
        for l in f.loops:
            l[buf.uv].uv = (0.004, 0.5)
    fr = faces[0]
    for l, st in zip(fr.loops, ((0, 0), (1, 0), (1, 1), (0, 1))):
        l[buf.uv].uv = st


# ------------------------------------------------------------------ the body

def body_profile():
    pts = [(-1.10, ZB), (1.10, ZB), (HW, 0.95), (HW, 2.72), (1.12, 3.13), (0.72, 3.33), (0.0, ZT),
           (-0.72, 3.33), (-1.12, 3.13), (-HW, 2.72), (-HW, 0.95)]
    return fillet(pts, 0.14, 3)


def by_normal(f):
    if f.normal.z > 0.55:
        return "train_roof"
    if f.normal.z < -0.6:
        return "train_under"
    return "train_body"


def shell_ring(prof, y, sx=1.0, sz=1.0, rake=0.0, sgn=1):
    out = []
    for x, z in prof:
        z2 = ZB + (z - ZB) * sz
        out.append(Vector((x * sx, y - sgn * rake * (z2 - ZB) / (ZT - ZB), z2)))
    return out


def front_plane(y_end, sgn):
    """The cab front's plane: y = y_end - sgn * k * (z - ZB). Returns (point_on(u, z, off),
    normal)."""
    k = RAKE / (ZT - ZB)
    n = Vector((0, sgn, k)).normalized()

    def at(u, z, off=0.0):
        return Vector((u, y_end - sgn * k * (z - ZB), z)) + n * off
    return at, n


def face_slab(buf, pts_uz, at, off0, off1, mat):
    return buf.loft([[at(u, z, off0) for u, z in pts_uz], [at(u, z, off1) for u, z in pts_uz]], mat)


def car(bufs, yc, kind, sgn=1):
    """One car centred on y = yc. kind: 'cab' (nose toward sgn) or 'mid'."""
    body, trim, glass, run, roof, signs = (bufs[k] for k in ("body", "trim", "glass", "run", "roof", "signs"))
    prof = body_profile()
    y_a, y_b = yc - CAR_L / 2, yc + CAR_L / 2
    if kind == "mid":
        body.loft([shell_ring(prof, y_a), shell_ring(prof, y_b)], "train_body", mat_of=by_normal)
    else:
        y_back = yc - sgn * CAR_L / 2
        y_nose = yc + sgn * CAR_L / 2
        rings = [shell_ring(prof, y_back)]
        for d, sx, sz in ((NOSE, 1.0, 1.0), (0.30, 0.985, 0.994), (0.12, 0.94, 0.975), (0.0, 0.84, 0.955)):
            rings.append(shell_ring(prof, y_nose - sgn * d, sx, sz, RAKE * (1 - d / NOSE), sgn))
        body.loft(rings, "train_body", mat_of=by_normal)
        cab_front(bufs, y_nose, sgn)

    # Car-local layout along y (toward the nose for a cab car): doors and windows.
    def Y(ly):
        return yc + sgn * ly

    if kind == "mid":
        doors = (-1.3, 1.3)
        windows = ((-0.62, 0.62), (1.96, 2.32), (-2.32, -1.96))
        band = (-2.36, 2.36)
    else:
        doors = (-1.3, 0.72)
        windows = ((-0.68, 0.06), (1.36, 1.74), (-2.32, -1.96))
        band = (-2.36, CAR_L / 2 - NOSE - 0.04)
    for s in (-1, 1):
        # The navy band runs the car's whole length; the doors stand on it in their dark
        # surrounds (review v2: cut into pieces between the doors it read as navy blocks).
        # Tall enough to show well above and below the window frames (review v1).
        ya, yb = sorted((Y(band[0]), Y(band[1])))
        slab_x(trim, rr_yz((ya + yb) / 2, 2.03, (yb - ya) / 2, 0.64, 0.1), s * (HW - 0.015), s * (HW + 0.015),
               "train_band")
        # A navy pinstripe low on the body, as on the old cars.
        slab_x(trim, rr_yz((ya + yb) / 2, 1.02, (yb - ya) / 2, 0.055, 0.04), s * (HW - 0.015), s * (HW + 0.012),
               "train_band")
        for a, b in windows:
            ya, yb = sorted((Y(a), Y(b)))
            hw = (yb - ya) / 2
            slab_x(glass, rr_yz((ya + yb) / 2, 2.09, hw, 0.37, 0.1), s * (HW - 0.01), s * (HW + 0.022), "train_glass")
            frame_x(trim, s, (ya + yb) / 2, 2.09, hw - 0.03, 0.34, 0.08, 0.09, HW - 0.005, HW + 0.042, "train_frame")
        for d in doors:
            y = Y(d)
            # A dark surround, two teal-grey leaves with a gap, a tall window in each leaf.
            slab_x(trim, rr_yz(y, 1.80, 0.54, 0.82, 0.06), s * (HW - 0.01), s * (HW + 0.02), "train_mask")
            for e in (-1, 1):
                ly = y + e * 0.25
                slab_x(trim, rr_yz(ly, 1.80, 0.235, 0.78, 0.05), s * (HW - 0.005), s * (HW + 0.03), "train_door")
                slab_x(glass, rr_yz(ly, 2.02, 0.12, 0.34, 0.08), s * (HW + 0.02), s * (HW + 0.036), "train_glass")
                frame_x(trim, s, ly, 2.02, 0.10, 0.32, 0.07, 0.05, HW + 0.025, HW + 0.044, "train_mask")
        # The roof gutter, the line the rain tongues hang from.
        g = [(s * x, z) for x, z in rounded_rect(0.04, 0.035, 0.02)]
        # On a cab car it stops where the nose begins to narrow (review v1: it poked out of it).
        ga, gb = y_a + 0.12, y_b - 0.12
        if kind == "cab":
            ga, gb = sorted((Y(-CAR_L / 2 + 0.12), Y(CAR_L / 2 - NOSE)))
        trim.extrude_y([(s * 1.225 + x, GUTTER + z) for x, z in g], ga, gb, "train_gutter")
    if kind == "mid":
        for s in (-1, 1):
            # The route strip under the centre window.
            x = s * (HW + 0.012)
            back = Vector((s * 0.03, 0, 0))
            y0, y1 = (yc - 0.72, yc + 0.72) if s > 0 else (yc + 0.72, yc - 0.72)
            sign_panel(signs, ((Vector((x, y0, 1.12)), Vector((x, y1, 1.12)), Vector((x, y1, 1.34)),
                                Vector((x, y0, 1.34))), back), "train_route")

    # Running gear: two bogies, an equipment box between them.
    for lb in (-1.6, 1.6):
        bogie(run, yc + lb)
    rbox(run, 0, yc, 0.36, ZB + 0.04, 0.88, 0.62, 0.12, "train_bogie")
    # Roof: air-con pods on the cab cars, the pantograph and two small pods on the middle car.
    if kind == "cab":
        rbox(roof, 0, yc - sgn * 0.45, ZT - 0.08, ZT + 0.28, 0.74, 1.05, 0.26, "train_ac", top_scale=0.93)
    else:
        for ly in (-1.75, 1.75):
            rbox(roof, 0, yc + ly, ZT - 0.08, ZT + 0.22, 0.6, 0.45, 0.2, "train_ac", top_scale=0.92)
        pantograph(roof, yc)


def cab_front(bufs, y_nose, sgn):
    trim, glass, signs, run = bufs["trim"], bufs["glass"], bufs["signs"], bufs["run"]
    at, n = front_plane(y_nose, sgn)
    # The big near-black mask, the windscreen recessed inside a lip, the blind above it.
    face_slab(trim, rounded_rect(0.84, 0.74, 0.22), lambda u, z, o: at(u, 2.30 + z, o), -0.04, 0.012, "train_mask")
    face_slab(glass, rounded_rect(0.72, 0.43, 0.14), lambda u, z, o: at(u, 2.12 + z, o), -0.02, 0.024, "train_glass")
    outer = rounded_rect(0.79, 0.50, 0.2)
    inner = rounded_rect(0.70, 0.41, 0.12)
    ring(trim, outer, inner, 0.0, 0.04, lambda p, a: at(p[0], 2.12 + p[1], a), "train_mask")
    name = "train_dest_fpj" if sgn > 0 else "train_dest_baclaran"
    u0, u1 = (0.6, -0.6) if sgn > 0 else (-0.6, 0.6)
    sign_panel(signs, ((at(u0, 2.68, 0.03), at(u1, 2.68, 0.03), at(u1, 2.94, 0.03), at(u0, 2.94, 0.03)), n * 0.05),
               name)
    # The navy band carried across the front under the mask, so the livery wraps the cab.
    face_slab(trim, rounded_rect(0.96, 0.07, 0.05), lambda u, z, o: at(u, 1.43 + z, o), -0.04, 0.014, "train_band")
    # Headlights and tail lamps in round dark bezels, below the mask.
    for u in (-0.70, 0.70):
        cyl(trim, at(u, 1.20, 0.0), n, 0.14, 0.1, "train_mask", sides=18)
        cyl(trim, at(u, 1.20, 0.035), n, 0.10, 0.04, "train_lamp", sides=18)
        cyl(trim, at(u * 0.6, 1.20, 0.0), n, 0.075, 0.08, "train_mask", sides=14)
        cyl(trim, at(u * 0.6, 1.20, 0.03), n, 0.05, 0.035, "train_lamp_red", sides=14)
    # A chunky anti-climber bumper and the coupler under it.
    face_slab(trim, rounded_rect(0.98, 0.09, 0.07), lambda u, z, o: at(u, 0.80 + z, o), -0.12, 0.09, "train_bumper")
    yb = y_nose + sgn * 0.02
    run.extrude_y(fillet([(-0.14, 0.44), (0.14, 0.44), (0.14, 0.66), (-0.14, 0.66)], 0.05, 2),
                  *sorted((yb - sgn * 0.4, yb + sgn * 0.08)), "train_bumper")


def bogie(run, y):
    for s in (-1, 1):
        for a in (-0.42, 0.42):
            c = Vector((s * GAUGE, y + a, WHEEL_R - 0.005))
            cyl(run, c, (1, 0, 0), WHEEL_R, 0.13, "train_wheel", sides=22)
            cyl(run, c + Vector((s * 0.085, 0, 0)), (1, 0, 0), 0.14, 0.05, "train_bogie", sides=16)
            rbox(run, s * 0.93, y + a, 0.17, 0.43, 0.09, 0.13, 0.05, "train_bogie")
        # The side frame outside the wheels, with a lump for the spring.
        run.extrude_y([(s * 0.93 + x, 0.31 + z) for x, z in rounded_rect(0.075, 0.12, 0.06)], y - 0.8, y + 0.8,
                      "train_bogie")
        rbox(run, s * 0.93, y, 0.36, 0.56, 0.1, 0.18, 0.07, "train_bogie")
    cyl(run, Vector((0, y - 0.42, WHEEL_R - 0.005)), (1, 0, 0), 0.06, 1.3, "train_bogie", 10)
    cyl(run, Vector((0, y + 0.42, WHEEL_R - 0.005)), (1, 0, 0), 0.06, 1.3, "train_bogie", 10)
    run.extrude_y(fillet([(-0.95, 0.34), (0.95, 0.34), (0.95, 0.5), (-0.95, 0.5)], 0.06, 2), y - 0.14, y + 0.14,
                  "train_bogie")
    rbox(run, 0, y, 0.46, ZB + 0.05, 0.42, 0.26, 0.1, "train_bogie")


def pantograph(roof, yc):
    """A chunky single-arm pantograph on four insulators; the collector head's strip tops out
    at HEAD_TOP."""
    for x in (-0.42, 0.42):
        for y in (-0.36, 0.36):
            roof.blob(Vector((x, yc + y, ZT + 0.02)), (0.075, 0.075, 0.1), "train_insulator")
    rbox(roof, 0, yc, ZT + 0.06, ZT + 0.2, 0.56, 0.48, 0.12, "train_panto", top_scale=0.94)
    hinge = ZT + 0.2
    knee = Vector((0, yc + 0.58, 3.98))
    head_y = yc - 0.32
    for x in (-0.28, 0.28):
        roof.tube([Vector((x, yc - 0.46, hinge)), Vector((x * 0.35, knee.y, knee.z))], 0.065, "train_panto", sides=10)
    roof.blob(knee, (0.09, 0.09, 0.09), "train_panto")
    roof.blob(Vector((0, yc - 0.46, hinge)), (0.34, 0.1, 0.09), "train_panto")
    top = HEAD_TOP - 0.1
    roof.tube([knee, Vector((0, head_y, top - 0.04))], 0.058, "train_panto", sides=10)
    roof.blob(Vector((0, head_y, top - 0.03)), (0.07, 0.07, 0.07), "train_panto")
    for x in (-0.36, 0.36):
        roof.tube([Vector((0, head_y, top - 0.04)), Vector((x, head_y, top + 0.03))], 0.03, "train_panto", sides=8)
    # The collector: a bar with down-turned horns, and the carbon strip on top.
    roof.tube([Vector((-1.0, head_y, top - 0.1)), Vector((-0.8, head_y, top + 0.03)), Vector((0.8, head_y, top + 0.03)),
               Vector((1.0, head_y, top - 0.1))], 0.035, "train_panto", sides=8)
    roof.extrude_y([(x, z) for x, z in fillet([(-0.72, top + 0.03), (0.72, top + 0.03), (0.72, HEAD_TOP),
                                                 (-0.72, HEAD_TOP)], 0.02, 2)], head_y - 0.05, head_y + 0.05, "train_bumper")


def bellows(bufs, y0, y1):
    """Fat rubber bellows over the gap between two cars, three folds, and a coupler bar under."""
    b = bufs["bellows"]
    rings = []
    ys = [y0 - 0.06 + (y1 - y0 + 0.12) * k / 4 for k in range(5)]
    for k, y in enumerate(ys):
        s = 1.0 if k % 2 == 0 else 1.05
        rings.append([Vector((x * s, y, 1.95 + z * s)) for x, z in rounded_rect(1.0, 1.05, 0.3)])
    b.loft(rings, "train_rubber")
    bufs["run"].tube([Vector((0, y0 - 0.35, 0.52)), Vector((0, y1 + 0.35, 0.52))], 0.08, "train_bumper", sides=10)


# ------------------------------------------------------------------ assembly

def build():
    col = bpy.data.collections.new("lrt_train")
    bpy.context.scene.collection.children.link(col)
    shell = TBuf("train_body")
    # The shell's rounded shoulders and skirt are shaded smooth, so they read as one curve
    # rather than bands of facets (review v1); its flat sides stay flat.
    shell.round_shell = True
    bufs = {"body": shell, "trim": TBuf("train_trim"), "glass": TBuf("train_glass"),
            "run": TBuf("train_running"), "roof": TBuf("train_roof_kit"), "bellows": TBuf("train_bellows"),
            "signs": TBuf("train_signs", keep_uv=True)}
    car(bufs, CARS[0], "cab", sgn=-1)
    car(bufs, CARS[1], "mid")
    car(bufs, CARS[2], "cab", sgn=1)
    for a, b in zip(CARS, CARS[1:]):
        bellows(bufs, a + CAR_L / 2, b - CAR_L / 2)
    objs = [bufs["body"].finish(col, bevel=0.07, segments=3),
            bufs["trim"].finish(col, bevel=0.012, segments=2),
            bufs["glass"].finish(col, bevel=0.008, segments=1),
            bufs["run"].finish(col, bevel=0.025, segments=2),
            bufs["roof"].finish(col, bevel=0.03, segments=2),
            bufs["bellows"].finish(col, bevel=0.05, segments=2),
            bufs["signs"].finish(col, bevel=0.004, segments=1)]
    root = bpy.data.objects.new("lrt_train_root", None)
    root.empty_display_type, root.empty_display_size = "ARROWS", 1.0
    col.objects.link(root)
    for o in objs:
        o.parent = root
    return col, root


def report(col):
    dg = bpy.context.evaluated_depsgraph_get()
    lo, hi = Vector((1e9,) * 3), Vector((-1e9,) * 3)
    faces = 0
    for o in col.objects:
        if o.type != "MESH":
            continue
        ev = o.evaluated_get(dg)
        me = ev.to_mesh()
        faces += len(me.polygons)
        for v in me.vertices:
            p = o.matrix_world @ v.co
            lo = Vector((min(lo.x, p.x), min(lo.y, p.y), min(lo.z, p.z)))
            hi = Vector((max(hi.x, p.x), max(hi.y, p.y), max(hi.z, p.z)))
        ev.to_mesh_clear()
    print(f"[ilalim-train] bounds x {lo.x:.3f}..{hi.x:.3f}  y {lo.y:.3f}..{hi.y:.3f}  z {lo.z:.3f}..{hi.z:.3f}; "
          f"{faces} faces")


# ------------------------------------------------------------------ review renders

def append_guideway():
    path = str(SOURCE / "lrt_kit.blend")
    with bpy.data.libraries.load(path, link=False) as (src, dst):
        dst.collections = [c for c in src.collections if c in ("guideway over the court", "review stand-ins")]
    for c in dst.collections:
        bpy.context.scene.collection.children.link(c)
    return {c.name: c for c in dst.collections}


def preview(version, root):
    ctx = append_guideway()
    L.lighting()
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 1000
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    cam.data.clip_end = 2000
    cam.data.sensor_fit = "HORIZONTAL"
    scene.collection.objects.link(cam)
    scene.camera = cam
    eye = 18 / math.tan(math.radians(95 / 2))
    rh = L.RAIL_HEAD
    tx = L.TRACK_X
    # (name, train y, camera, target, lens, show guideway)
    shots = [
        ("court_eye", 40.0, Vector((0.0, -9.0, 1.25)), Vector((0, 30, 11)), eye, True),
        ("pavement_eye", 40.0, Vector((10.8, -14.0, 1.462)), Vector((3.0, 40.0, 10.5)), eye, True),
        ("pavement_west_eye", 0.0, Vector((-10.8, -14.0, 1.462)), Vector((-1.0, 2.0, 10.8)), eye, True),
        ("side_on_deck", 0.0, Vector((26.0, 0.0, 12.5)), Vector((tx, 0.0, rh + 1.6)), 34, True),
        ("side_elevation", 0.0, Vector((40.0, 0.0, rh + 1.9)), Vector((tx, 0.0, rh + 1.9)), 55, False),
        ("cab_close", 0.0, Vector((0.6, 12.6, rh + 2.4)), Vector((tx, 7.6, rh + 1.7)), 30, True),
        ("aerial", 0.0, Vector((28.0, -30.0, 30.0)), Vector((0.0, 0.0, 9.0)), 35, True),
        ("panto_close", 0.0, Vector((6.5, 4.0, rh + 5.2)), Vector((tx, 0.0, rh + 3.9)), 35, True),
        ("bogie_close", 0.0, Vector((6.2, 4.2, rh + 0.9)), Vector((tx, 1.8, rh + 0.4)), 32, False),
    ]
    for name, ty, pos, tgt, lens, show in shots:
        root.location = (-tx if "west" in name else tx, ty, rh)
        for c in ctx.values():
            c.hide_render = not show
        cam.location, cam.data.lens = pos, lens
        cam.rotation_euler = (tgt - pos).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = str(PREVIEWS / f"train_{name}_v{version}.png")
        bpy.ops.render.render(write_still=True)
        print("[ilalim-train] preview", scene.render.filepath)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
    bpy.ops.wm.read_factory_settings(use_empty=True)
    col, root = build()
    report(col)
    SOURCE.mkdir(parents=True, exist_ok=True)
    out = SOURCE / "train.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(out), compress=True)
    backup = SOURCE / "train.blend1"
    if backup.exists():
        backup.unlink()
    print("[ilalim-train] saved", out)
    if version:
        preview(version, root)


if __name__ == "__main__":
    main()
