"""Model the streets, the ground and the street furniture for the Ilalim ng Tulay rebuild (ILALIM-1.3).

  py -3 tools/author_ilalim_textures_street.py         # paint the street textures first
  blender -b --python tools/author_ilalim_street.py -- [--preview N] [--only shot,shot]

Writes ArtSource/ilalim/street.blend. With --preview it also writes versioned renders to
Logs/ilalim-blender/street_<shot>_vN.png, with the LRT-1 guideway (appended from lrt_kit.blend)
and the blockout's OSM buildings added AFTER the save, as context only.

THE PLACE: Taft Avenue at the Padre Faura corner, Ermita (docs/ILALIM_REWORK_GUIDE.md section 0),
laid out from ArtSource/ilalim/osm_layout.json through the blockout's sightline_override(), so
the streets meet the same moved Rizal Hall compound the other kits use. Blender X is the game's
x (east), Blender Y is the game's z (north), origin at the court centre on Taft.

THE GROUND IS FIELDS, NOT A GRID OF BOXES (owner, blockout v6: "fix the plane.."). Every street
is a signed distance field around its OSM centre line; the streets are joined with a SMOOTH
minimum, so every corner gets a real rounded kerb return, as at the Padre Faura corners. Each
ground class (road, pavement, lot, parking, drive, lawn) is then contoured out of the fields on
a 0.5 m lattice (marching squares with linear crossings), welded, and dissolved into a few big
flat faces, with a skirt down its whole edge. The classes never share a plane:
  * road 0.000, running under the kerbs;
  * the KERB is its own swept piece along the road's edge contour: 0.35 m wide, top 0.150,
    rounded on the road side, 1 m stones drawn in its texture. On Taft its top is painted WHITE:
    that paint is the east and west chalk. The side streets' kerbs are yellow and black;
  * pavements 0.212, starting 2 cm back over the kerb's top;
  * lots, parking and drives 0.24, starting 2 cm back over the pavement; lawns 0.30.
THE CONTRACT (guide section 1) still holds exactly on the OLD court under the bridge: for
|y| <= 17.5 the fields are Taft's alone, so the carriageway is |x| <= 6.65 at 0.000, the kerb
6.65..7.0 at 0.150, the pavements 7..11 at 0.212. It is plain road now: no chalk, the same calm,
desaturated asphalt as the rest of Taft, the two potholes at (+/-3.4, -/+3.0) still flat decals.

THE COURT IS ON THE CAMPUS LOT (owner, 2026-10-04: "can we move the play area to this open
space? then fix up the area where the old play area was. then fix up the fences so the area is
still open to the road."). Its centre is COURT = (-23.0, 14.2) on the lot surface (0.24), west of
the west pavement. The lot it stands on, x -35.0..-11.1 and y 3.4 up to the Padre Faura side
fence, is ONE plain lot surface: the parking bays that reached into it are cut back to the new
fence. The chalk is flat decals 6 mm proud of the lot: the box is the square 7 m about the
centre, FOUR objects, one per edge ("chalk box edge east" and so on; the game reads each edge
by its own bounds, so they are never one mesh), each exactly 14 m long and mitred into its
neighbours so no two share a face; the throwing line is the yellow square at 8 m, one object
("chalk throw line"). The PGH fence no longer runs along Taft in front of the lot: it turns
west at y 3.4, runs to x -35.0 and north to Padre Faura, where it meets the campus fence that
carries on west from there. The Padre Faura side fence along the lot is gone too (owner, looking
at the first result: "yes that fence needs to be removed"), so the lot is open to Taft on the
east AND to Padre Faura on the north, and on both the lot's own edge is a 28 mm step up from
the pavement.

THE ROAD MARKINGS are thin decal slabs (top 8 mm, sunk 1 cm): zebra crossings on Taft where OSM
has footways (y 22..25 and 40..43), stop lines, Manila's red intersection box with its
diagonals, zebras across both Padre Faura arms, dashed lane lines on every side street and
ONE WAY arrows on Padre Faura (one-way west, 3 lanes). None inside |y| < 17.

THE TAFT MEDIAN PLANTER (owner's street-view reference): a low wall painted dark GREEN with a
light rounded concrete coping, filled with soil, planted. Real Taft's single piers stand in it;
the game's twin legs get a planter COLLAR each, and a 1.2 m central median runs between them at
x = 0, so Taft still reads as a divided avenue. It exists only beyond the play area (|y| > 17.3)
and opens for the crossings and the Padre Faura junction. The plants are PLACEHOLDER clumps:
the planting kit replaces them (positions in the object "median plant placeholders").
Traffic: lanes are x +/-0.6..3.2 (between median and collars); the blockout's jeepneys at
x +/-3.5 would clip the collars at the pier rows, so move them to x +/-1.9.

THE FURNITURE (research.md section 4, and the Commons photographs in the report): all outside
the chalk box; on the play area's pavements only against the outer edge (|x| 10.3..11), never
mid-pavement, so the 4.2 m flanks stay walkable.
  * concrete power poles with a steel crossarm, a transformer on some, and a coil of spare
    cable, joined by TANGLED overhead cables: 4 to 6 chunky cables per span, sagging by
    different amounts, some twisted into bundles, a few drooping low, service drops into the
    east facades, and one bundle sagging across Taft under the viaduct at the Padre Faura corner;
  * traffic lights on galvanized mast arms at three Padre Faura corners (OSM signals), black
    heads with chunky visors, a countdown box, and the green street-name blade on the arm
    (PADRE FAURA ST. over Taft, TAFT AVE. over Padre Faura); a corner post with both blades;
  * black ONE WAY signs, the MMDA "BAWAL TUMAWID" sign in the median stub facing the court;
  * yellow-painted pedestrian railings along the kerb at the crossings, and freestanding yellow
    and black crowd barriers like the ones in the photographs;
  * the campus fences and walls from OSM: PGH's iron picket fence on a low plinth with
    concrete pillars (the Taft run lands exactly on the x = -11 wall line: see-through, chunky
    pickets; it goes round the back of the court lot, see open_court_lot()), the Supreme Court's white arched fence north of Padre Faura, cream concrete walls;
  * the OSM bus stops as simple SAKAYAN shelters; galvanized street lamps; and a hand-painted
    yellow and red barangay welcome board (BARANGAY 712, every name invented) on the east
    pavement north of the court, turned to face the court.
Every furniture type is a prototype at the origin in "street furniture (prototypes)", placed as
linked duplicates, like the LRT kit.
"""
import json
import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
import numpy as np
from mathutils import Euler, Matrix, Vector

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools"))
import author_ilalim_blockout as B   # noqa: E402  (read-only: layout, sightline override, buildings)
import author_ilalim_lrt as L        # noqa: E402  (read-only: fillet, rounded_rect, Buf, lighting)

SOURCE = ROOT / "ArtSource" / "ilalim"
TEXTURES = SOURCE / "textures"
PREVIEWS = ROOT / "Logs" / "ilalim-blender"
UP = Vector((0, 0, 1))

KERB_IN, BOX, THROW = 6.65, 7.0, 8.0
KERB_TOP, PAVE_TOP, PAVE_OUT, WALL_Y = 0.150, 0.212, 11.0, 16.5
LOT_TOP, LAWN_TOP = 0.24, 0.30
KERB_W = 0.35
GUTTER = 2.2               # the silt band's width, out from every kerb face
EXT, CELL = 220.0, 0.5
SIDEWALK = 2.0
LANE_W = 3.0
STREET_KINDS = B.STREET_KINDS
PIER_ROWS = (19.0, 44.0, 69.0, 94.0, 119.0, 144.0)
OVERRIDE_Y = 17.5          # rows where the fields are Taft's alone (the old court and a margin)
# The court on the campus lot (owner, 2026-10-04: "can we move the play area to this open space?").
# These numbers are shared with the game code: do not move them here alone.
COURT = (-23.0, 14.2)      # the chalk box's centre, on the lot surface
KEEP_CLEAR = 9.5           # nothing but chalk stands within this of the centre, in x or in y
FENCE_X = -(PAVE_OUT + 0.2)             # the Taft fence's centre line (prepare_barrier)
LOT_WEST, LOT_SOUTH = -35.0, 3.4        # the new fence runs: the lot's back and its south side

# ------------------------------------------------------------------ materials

# name: (texture, (tile w m, tile h m) or None for 0..1 sign UVs, tint or None, anti-tile, gutter overlay)
MATS = {
    "street_asphalt":       ("asphalt", (8, 8), None, True, True),
    "street_pavement":      ("pavement", (4, 4), None, False, False),
    "street_sidewalk":      ("sidewalk", (4, 4), None, False, False),
    "street_lot":           ("lot", (8, 8), None, True, False),
    "street_parking":       ("parking", (8, 8), None, True, False),
    "street_drive":         ("drive", (4, 4), None, False, False),
    "street_lawn":          ("lawn", (8, 8), None, True, False),
    "street_kerb_white":    ("kerb_white", "direct", None, False, False),
    "street_kerb_stripes":  ("kerb_stripes", "direct", None, False, False),
    "street_paint_white":   ("paint", (4, 4), None, False, False),
    "street_paint_yellow":  ("paint", (4, 4), (0.93, 0.77, 0.27), False, False),
    "street_paint_red":     ("paint", (4, 4), (0.45, 0.09, 0.08), False, False),
    "street_chalk":         ("chalk", (4, 4), None, False, False),
    "street_chalk_yellow":  ("chalk", (4, 4), (0.97, 0.82, 0.30), False, False),
    "street_pothole":       ("pothole", None, None, False, False),
    "street_median_wall":   ("median_wall", (4, 0.5), None, False, False),
    "street_coping":        ("coping", (4, 4), None, False, False),
    "street_soil":          ("soil", (4, 4), None, False, False),
    "street_plant_placeholder": (None, None, (0.30, 0.45, 0.20), False, False),
    "street_rail_yellow":   ("steel", (4, 4), (0.90, 0.72, 0.22), False, False),
    "street_rail_black":    ("steel", (4, 4), (0.17, 0.17, 0.16), False, False),
    "street_fence_iron":    ("steel", (4, 4), (0.15, 0.18, 0.16), False, False),
    "street_fence_white":   ("steel", (4, 4), (0.92, 0.91, 0.88), False, False),
    "street_galvanized":    ("steel", (4, 4), (0.62, 0.63, 0.61), False, False),
    "street_signal_black":  ("steel", (4, 4), (0.12, 0.13, 0.12), False, False),
    "street_shelter_green": ("steel", (4, 4), (0.26, 0.42, 0.31), False, False),
    "street_transformer":   ("steel", (4, 4), (0.50, 0.53, 0.50), False, False),
    "street_board_back":    ("steel", (4, 4), (0.55, 0.55, 0.53), False, False),
    "street_board_red":     ("steel", (4, 4), (0.62, 0.20, 0.17), False, False),
    "street_pole":          ("concrete_pole", (2, 4), None, False, False),
    "street_wall_cream":    ("wall", (4, 3), None, False, False),
    "street_wall_white":    ("wall", (4, 3), (1.07, 1.09, 1.13), False, False),
    "street_wire":          (None, None, (0.05, 0.05, 0.05), False, False),
    "street_lens_red":      (None, None, (0.80, 0.04, 0.03), False, False),
    "street_lens_amber":    (None, None, (0.55, 0.30, 0.02), False, False),
    "street_lens_green":    (None, None, (0.02, 0.26, 0.12), False, False),
    "street_lamp_glass":    (None, None, (0.92, 0.90, 0.82), False, False),
    "street_sign_blade_taft":  ("sign_blade_taft", None, None, False, False),
    "street_sign_blade_faura": ("sign_blade_faura", None, None, False, False),
    "street_sign_oneway":   ("sign_oneway", None, None, False, False),
    "street_sign_bawal":    ("sign_bawal", None, None, False, False),
    "street_sign_sakayan":  ("sign_sakayan", None, None, False, False),
    "street_sign_timer":    ("sign_timer", None, None, False, False),
    "street_sign_barangay": ("sign_barangay", None, None, False, False),
}
NORMALS = {"asphalt", "pavement", "sidewalk", "drive"}


def material(name):
    m = bpy.data.materials.get(name)
    if m:
        return m
    tex, tile, tint, anti, gutter = MATS[name]
    m = bpy.data.materials.new(name)
    if m.node_tree is None:
        m.use_nodes = True
    nodes, links = m.node_tree.nodes, m.node_tree.links
    bsdf = nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = 0.88
    if tex is None:
        bsdf.inputs["Base Color"].default_value = (*tint, 1)
        m.diffuse_color = (*tint, 1)
        if name == "street_lens_red":
            bsdf.inputs["Emission Color"].default_value = (1.0, 0.06, 0.03, 1)
            bsdf.inputs["Emission Strength"].default_value = 2.0
        return m
    uv = nodes.new("ShaderNodeUVMap")
    vec = uv.outputs["UV"]
    if isinstance(tile, tuple):
        # World UVs are metres / 4; the mapping scales them to this texture's own size.
        mp = nodes.new("ShaderNodeMapping")
        mp.inputs["Scale"].default_value = (4.0 / tile[0], 4.0 / tile[1], 1)
        links.new(vec, mp.inputs["Vector"])
        vec = mp.outputs["Vector"]
    img = bpy.data.images.load(str(TEXTURES / f"street_{tex}_albedo.png"), check_existing=True)
    albedo = nodes.new("ShaderNodeTexImage")
    albedo.image = img
    if tile is None:
        albedo.extension = "EXTEND"
    links.new(vec, albedo.inputs["Vector"])
    colour = albedo.outputs["Color"]
    if anti:
        # A second sample, rotated 37 degrees and scaled 0.61, blended in by a big soft mask,
        # so the repeat never lines up (the Kanto roof fix, as in the LRT kit).
        rot = nodes.new("ShaderNodeMapping")
        rot.inputs["Rotation"].default_value = (0, 0, math.radians(37))
        rot.inputs["Scale"].default_value = (0.61, 0.61, 1)
        rot.inputs["Location"].default_value = (0.37, 0.71, 0)
        links.new(vec, rot.inputs["Vector"])
        second = nodes.new("ShaderNodeTexImage")
        second.image = img
        links.new(rot.outputs["Vector"], second.inputs["Vector"])
        noise = nodes.new("ShaderNodeTexNoise")
        noise.inputs["Scale"].default_value = 0.35
        noise.inputs["Detail"].default_value = 0.0
        links.new(vec, noise.inputs["Vector"])
        ramp = nodes.new("ShaderNodeMapRange")
        ramp.inputs["From Min"].default_value, ramp.inputs["From Max"].default_value = 0.4, 0.6
        links.new(noise.outputs["Fac"], ramp.inputs["Value"])
        blend = nodes.new("ShaderNodeMix")
        blend.data_type = "RGBA"
        links.new(ramp.outputs["Result"], blend.inputs["Factor"])
        links.new(colour, blend.inputs[6])
        links.new(second.outputs["Color"], blend.inputs[7])
        colour = blend.outputs[2]
    if tint is not None:
        mix = nodes.new("ShaderNodeMix")
        mix.data_type, mix.blend_type = "RGBA", "MULTIPLY"
        mix.inputs["Factor"].default_value = 1.0
        links.new(colour, mix.inputs[6])
        mix.inputs[7].default_value = (*tint, 1)
        colour = mix.outputs[2]
    if gutter:
        # The gutter's silt band: a multiplier on the UV map UVGrime (u along the kerb, v metres
        # out from the kerb face / 2). Unity needs this second UV channel (ILALIM-1.4).
        guv = nodes.new("ShaderNodeUVMap")
        guv.uv_map = "UVGrime"
        gt = nodes.new("ShaderNodeTexImage")
        gt.image = bpy.data.images.load(str(TEXTURES / "street_gutter.png"), check_existing=True)
        gt.image.colorspace_settings.name = "Non-Color"
        gt.extension = "EXTEND"
        links.new(guv.outputs["UV"], gt.inputs["Vector"])
        mul = nodes.new("ShaderNodeMix")
        mul.data_type, mul.blend_type = "RGBA", "MULTIPLY"
        mul.inputs["Factor"].default_value = 1.0
        links.new(colour, mul.inputs[6])
        links.new(gt.outputs["Color"], mul.inputs[7])
        colour = mul.outputs[2]
    links.new(colour, bsdf.inputs["Base Color"])
    npath = TEXTURES / f"street_{tex}_normal.png"
    if tex in NORMALS and npath.exists():
        normal = nodes.new("ShaderNodeTexImage")
        normal.image = bpy.data.images.load(str(npath), check_existing=True)
        normal.image.colorspace_settings.name = "Non-Color"
        links.new(vec, normal.inputs["Vector"])
        nmap = nodes.new("ShaderNodeNormalMap")
        nmap.inputs["Strength"].default_value = 0.5
        links.new(normal.outputs["Color"], nmap.inputs["Color"])
        links.new(nmap.outputs["Normal"], bsdf.inputs["Normal"])
    avg = np.asarray(img.pixels[:], dtype=np.float32).reshape(-1, 4)[::97, :3].mean(axis=0)
    if tint is not None:
        avg = avg * np.asarray(tint)
    m.diffuse_color = (*[float(c) for c in avg], 1)
    return m


def collection(name, parent=None):
    c = bpy.data.collections.new(name)
    (parent or bpy.context.scene.collection).children.link(c)
    return c


class SBuf(L.Buf):
    """The LRT kit's Buf, finished with THIS kit's materials, plain world UVs, and optional
    sign faces mapped 0..1 across their panel."""

    def __init__(self, name):
        super().__init__(name)
        self.decals = []

    def decal(self, faces, origin, au, av, w, h):
        self.decals.append((faces, Vector(origin), Vector(au), Vector(av), w, h))

    def cylinder_uv(self, faces, cx, cy, tile_w):
        """Wrap faces round a vertical axis so one turn is exactly one texture width: no facet
        chops the drawing, no seam."""
        self.cyl = getattr(self, "cyl", []) + [(faces, cx, cy, tile_w)]

    def world_uvs(self):
        uv = self.bm.loops.layers.uv.verify()
        for f in self.bm.faces:
            n = f.normal
            if abs(n.z) > 0.7:
                for lp in f.loops:
                    lp[uv].uv = (lp.vert.co.x / 4.0, lp.vert.co.y / 4.0)
                continue
            t = UP.cross(n)
            t = t.normalized() if t.length > 1e-6 else Vector((1, 0, 0))
            for lp in f.loops:
                lp[uv].uv = (lp.vert.co.dot(t) / 4.0, lp.vert.co.z / 4.0)
        for faces, o, au, av, w, h in self.decals:
            for f in faces:
                for lp in f.loops:
                    d = lp.vert.co - o
                    lp[uv].uv = (d.dot(au) / w, d.dot(av) / h)
        for faces, cx, cy, tile_w in getattr(self, "cyl", []):
            for f in faces:
                if abs(f.normal.z) > 0.7:
                    continue
                c = f.calc_center_median()
                ac = math.atan2(c.y - cy, c.x - cx)
                for lp in f.loops:
                    a = math.atan2(lp.vert.co.y - cy, lp.vert.co.x - cx)
                    a = ac + (a - ac + math.pi) % math.tau - math.pi
                    lp[uv].uv = (a / math.tau * tile_w / 4.0, lp.vert.co.z / 4.0)

    def box(self, center, size, mat, r=0.03, rot=0.0):
        """A rounded box: `size` full extents, rotated `rot` about z."""
        cx, cy, cz = center
        c, s = math.cos(rot), math.sin(rot)
        prof = [(x * c - y * s, x * s + y * c) for x, y in L.rounded_rect(size[0] / 2, size[1] / 2, r)]
        return self.extrude_z(prof, cz - size[2] / 2, cz + size[2] / 2, mat, offset=(cx, cy))

    def seg_box(self, a, b, half_w, z0, z1, mat, ext=0.0, r=0.03):
        a, b = Vector((a[0], a[1], 0)), Vector((b[0], b[1], 0))
        d = b - a
        L_ = d.length
        if L_ < 1e-4:
            return []
        return self.box(((a.x + b.x) / 2, (a.y + b.y) / 2, (z0 + z1) / 2), (L_ + 2 * ext, 2 * half_w, z1 - z0), mat,
                        r=r, rot=math.atan2(d.y, d.x))

    def panel(self, x0, x1, z0, z1, y_front, y_back, face_mat, edge_mat, back_mat=None, r=0.03):
        """A sign panel facing -y, x0..x1 by z0..z1. The front face takes `face_mat` mapped 0..1
        (u along +x); the back takes `back_mat` mapped mirrored, so it reads from behind."""
        prof = L.fillet([(x0, z0), (x1, z0), (x1, z1), (x0, z1)], r, 2)

        def mat_of(f):
            if f.normal.y < -0.9:
                return face_mat
            if back_mat and f.normal.y > 0.9:
                return back_mat
            return edge_mat
        faces = self.extrude_y(prof, y_front, y_back, edge_mat, mat_of=mat_of)
        w, h = x1 - x0, z1 - z0
        self.decal([f for f in faces if f.normal.y < -0.9], (x0, 0, z0), (1, 0, 0), (0, 0, 1), w, h)
        if back_mat:
            self.decal([f for f in faces if f.normal.y > 0.9], (x1, 0, z0), (-1, 0, 0), (0, 0, 1), w, h)
        return faces

    def finish(self, col, bevel=0.03, segments=2, smooth=True):
        self.world_uvs()
        mesh = bpy.data.meshes.new(self.name)
        self.bm.to_mesh(mesh)
        self.bm.free()
        for m in self.mats:
            mesh.materials.append(material(m))
        for p in mesh.polygons:
            p.use_smooth = smooth and p.area < 0.35
        obj = bpy.data.objects.new(self.name, mesh)
        col.objects.link(obj)
        if bevel:
            mod = obj.modifiers.new("Bevel", "BEVEL")
            mod.width, mod.segments = bevel, segments
            mod.limit_method, mod.angle_limit = "ANGLE", math.radians(35)
            mod.harden_normals, mod.use_clamp_overlap = True, True
        return obj


def add_bevel(obj, width, segments=2):
    mod = obj.modifiers.new("Bevel", "BEVEL")
    mod.width, mod.segments = width, segments
    mod.limit_method, mod.angle_limit = "ANGLE", math.radians(35)
    mod.harden_normals, mod.use_clamp_overlap = True, True


# ------------------------------------------------------------------ the fields

XS = np.arange(-EXT, EXT + CELL / 2, CELL)
YS = np.arange(-EXT, EXT + CELL / 2, CELL)
GX, GY = np.meshgrid(XS, YS)          # rows are y


def window(xmin, xmax, ymin, ymax):
    i0 = max(0, int((xmin + EXT) / CELL))
    i1 = min(len(XS), int((xmax + EXT) / CELL) + 2)
    j0 = max(0, int((ymin + EXT) / CELL))
    j1 = min(len(YS), int((ymax + EXT) / CELL) + 2)
    return slice(j0, j1), slice(i0, i1)


def seg_dist(X, Y, a, b):
    ax, ay = a
    dx, dy = b[0] - ax, b[1] - ay
    l2 = dx * dx + dy * dy or 1e-9
    t = np.clip(((X - ax) * dx + (Y - ay) * dy) / l2, 0, 1)
    return np.hypot(X - ax - t * dx, Y - ay - t * dy)


def line_dist(X, Y, line):
    d = np.full(X.shape, 1e4)
    for a, b in zip(line, line[1:]):
        d = np.minimum(d, seg_dist(X, Y, a, b))
    return d


def poly_sdf(X, Y, poly):
    d = np.full(X.shape, 1e4)
    inside = np.zeros(X.shape, bool)
    n = len(poly)
    for k in range(n):
        (xi, yi), (xj, yj) = poly[k], poly[k - 1]
        d = np.minimum(d, seg_dist(X, Y, (xi, yi), (xj, yj)))
        cond = ((yi > Y) != (yj > Y)) & (X < (xj - xi) * (Y - yi) / (yj - yi + 1e-12) + xi)
        inside ^= cond
    return np.where(inside, -d, d)


def smin(a, b, k):
    h = np.clip(0.5 + 0.5 * (b - a) / k, 0, 1)
    return b * (1 - h) + a * h - k * h * (1 - h)


def street_half(r):
    return max(2.5, r["lanes"] * LANE_W / 2)


def streets(layout):
    return [r for r in layout["roads"] if r["kind"] in STREET_KINDS and "Taft" not in r["name"]]


def build_fields(layout):
    """R: signed distance to the road edge (negative on the road). S: to the back of the
    sidewalk. D: drives and footpaths inside lots. P: parking. W: lawns."""
    R = np.abs(GX) - KERB_IN
    S = np.abs(GX) - PAVE_OUT
    for r in streets(layout):
        half = street_half(r)
        xs, ys = [p[0] for p in r["line"]], [p[1] for p in r["line"]]
        m = half + KERB_W + SIDEWALK + 6
        w = window(min(xs) - m, max(xs) + m, min(ys) - m, max(ys) + m)
        d = line_dist(GX[w], GY[w], r["line"])
        R[w] = smin(R[w], d - half, 4.0)
        S[w] = smin(S[w], d - half - KERB_W - SIDEWALK, 1.5)
    S = np.minimum(S, R - KERB_W - 0.8)
    D = np.full(GX.shape, 1e4)
    for r in layout["roads"]:
        if r["kind"] == "service" or r["kind"] in ("footway", "pedestrian", "path"):
            half = 2.0 if r["kind"] == "service" else 0.9
            xs, ys = [p[0] for p in r["line"]], [p[1] for p in r["line"]]
            w = window(min(xs) - 4, max(xs) + 4, min(ys) - 4, max(ys) + 4)
            D[w] = np.minimum(D[w], line_dist(GX[w], GY[w], r["line"]) - half)
    P = np.full(GX.shape, 1e4)
    W = np.full(GX.shape, 1e4)
    for a in layout["areas"]:
        xs, ys = [p[0] for p in a["poly"]], [p[1] for p in a["poly"]]
        w = window(min(xs) - 2, max(xs) + 2, min(ys) - 2, max(ys) + 2)
        if a["kind"] in ("village_green", "grass", "park", "garden"):
            W[w] = np.minimum(W[w], poly_sdf(GX[w], GY[w], a["poly"]))
        elif a["kind"] == "parking":
            P[w] = np.minimum(P[w], poly_sdf(GX[w], GY[w], a["poly"]))
    # THE COURT LOT is one plain lot surface (owner: "can we move the play area to this open
    # space?"): no parking bay, drive or lawn crosses it. OSM's PGH parking reached about 1.5 m
    # into its south end; it now stops on the new fence line, under the plinth. The cut is a box
    # distance, so the contour lands on y = 3.4 exactly and not on the nearest lattice row.
    box = np.maximum.reduce([LOT_WEST - GX, GX + PAVE_OUT - 1.0, LOT_SOUTH - GY, GY - 30.0])
    P, D, W = np.maximum(P, -box), np.maximum(D, -box), np.maximum(W, -box)
    # THE CONTRACT: on the old court (and a margin) the corridor is Taft's alone.
    zone = (np.abs(GY) <= OVERRIDE_Y) & (np.abs(GX) < 14.0)
    R[zone] = (np.abs(GX) - KERB_IN)[zone]
    S[zone] = (np.abs(GX) - PAVE_OUT)[zone]
    j = int((OVERRIDE_Y + CELL + EXT) / CELL)
    near = np.abs(XS) < 14
    for jj in (j, len(YS) - 1 - j):
        print(f"[street] field seam at row y {YS[jj]:.1f}: max |dR| {np.abs(R[jj] - (np.abs(XS) - KERB_IN))[near].max():.4f} "
              f"max |dS| {np.abs(S[jj] - (np.abs(XS) - PAVE_OUT))[near].max():.4f}")
    return {"R": R, "S": S, "D": D, "P": P, "W": W}


def sample(F, x, y):
    fx, fy = (x + EXT) / CELL, (y + EXT) / CELL
    i, j = int(math.floor(fx)), int(math.floor(fy))
    i, j = max(0, min(len(XS) - 2, i)), max(0, min(len(YS) - 2, j))
    tx, ty = fx - i, fy - j
    return (F[j, i] * (1 - tx) * (1 - ty) + F[j, i + 1] * tx * (1 - ty) + F[j + 1, i] * (1 - tx) * ty
            + F[j + 1, i + 1] * tx * ty)


def grad(F, x, y, e=0.25):
    g = Vector(((sample(F, x + e, y) - sample(F, x - e, y)) / (2 * e),
                (sample(F, x, y + e) - sample(F, x, y - e)) / (2 * e), 0))
    return g.normalized() if g.length > 1e-6 else Vector((1, 0, 0))


def snap(F, x, y, target, iters=6):
    """Move (x, y) along the field's gradient until F = target (a signed distance)."""
    p = Vector((x, y, 0))
    for _ in range(iters):
        v = sample(F, p.x, p.y)
        p += grad(F, p.x, p.y) * (target - v)
    return p.x, p.y


# ------------------------------------------------------------------ marching squares

def _cells(F):
    inside = F < 0
    a, b, c, d = inside[:-1, :-1], inside[:-1, 1:], inside[1:, 1:], inside[1:, :-1]
    full = a & b & c & d
    boundary = (a | b | c | d) & ~full
    return inside, full, boundary


def _cross_pos(F, kind, j, i):
    if kind == "h":
        f0, f1 = F[j, i], F[j, i + 1]
        t = f0 / (f0 - f1)
        return XS[i] + t * CELL, YS[j]
    f0, f1 = F[j, i], F[j + 1, i]
    t = f0 / (f0 - f1)
    return XS[i], YS[j] + t * CELL


def _cell_polys(inside, F, j, i):
    """The inside part of one boundary cell as polygons of keys, CCW, plus its contour segments."""
    corners = [(j, i), (j, i + 1), (j + 1, i + 1), (j + 1, i)]
    edges = [("h", j, i), ("v", j, i + 1), ("h", j + 1, i), ("v", j, i)]
    ins = [inside[c] for c in corners]
    saddle = ins == [True, False, True, False] or ins == [False, True, False, True]
    if saddle and (F[j, i] + F[j, i + 1] + F[j + 1, i + 1] + F[j + 1, i]) >= 0:
        polys = []
        for k in range(4):
            if ins[k]:
                polys.append([edges[k - 1], ("n",) + corners[k], edges[k]])
        segs = [(p[2], p[0]) for p in polys]
        return polys, segs
    poly, segs = [], []
    for k in range(4):
        if ins[k]:
            poly.append(("n",) + corners[k])
        if ins[k] != ins[(k + 1) % 4]:
            poly.append(edges[k])
    for k in range(len(poly)):
        p, q = poly[k], poly[(k + 1) % len(poly)]
        if p[0] != "n" and q[0] != "n":
            segs.append((p, q))
    return [poly], segs


def region_mesh(name, F, z, skirt, mat_of, col, extra_uv=None, bevel=0.03):
    """A flat welded surface where F < 0 at height z, dissolved into large faces, with a skirt
    from its whole edge down to `skirt`. `mat_of(x, y)` names each face's material."""
    inside, full, boundary = _cells(F)
    node_id = np.full(F.shape, -1, dtype=np.int64)
    jj, ii = np.nonzero(inside)
    node_id[jj, ii] = np.arange(len(jj))
    verts = np.column_stack([XS[ii], YS[jj], np.full(len(jj), z)]).tolist()
    fj, fi = np.nonzero(full)
    faces = np.column_stack([node_id[fj, fi], node_id[fj, fi + 1], node_id[fj + 1, fi + 1], node_id[fj + 1, fi]]).tolist()
    cross = {}

    def vid(key):
        if key[0] == "n":
            return int(node_id[key[1], key[2]])
        if key not in cross:
            x, y = _cross_pos(F, *key)
            cross[key] = len(verts)
            verts.append([x, y, z])
        return cross[key]

    for j, i in zip(*np.nonzero(boundary)):
        polys, _ = _cell_polys(inside, F, j, i)
        for p in polys:
            ids = [vid(k) for k in p]
            if len(set(ids)) >= 3:
                faces.append(ids)
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-4)
    names = []
    for f in bm.faces:
        c = f.calc_center_median()
        mname = mat_of(c.x, c.y)
        if mname not in names:
            names.append(mname)
        f.material_index = names.index(mname)
    bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(0.5), verts=bm.verts, edges=bm.edges,
                             delimit={"MATERIAL"})
    bnd = [e for e in bm.edges if e.is_boundary]
    ret = bmesh.ops.extrude_edge_only(bm, edges=bnd)
    for v in ret["geom"]:
        if isinstance(v, bmesh.types.BMVert):
            v.co.z = skirt
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    for f in bm.faces:
        if f.normal.z < -0.5:
            f.normal_flip()
    uv = bm.loops.layers.uv.verify()
    for f in bm.faces:
        n = f.normal
        if abs(n.z) > 0.7:
            for lp in f.loops:
                lp[uv].uv = (lp.vert.co.x / 4.0, lp.vert.co.y / 4.0)
        else:
            t = UP.cross(n)
            t = t.normalized() if t.length > 1e-6 else Vector((1, 0, 0))
            for lp in f.loops:
                lp[uv].uv = (lp.vert.co.dot(t) / 4.0, lp.vert.co.z / 4.0)
    if extra_uv:
        extra_uv(bm)
    bm.to_mesh(mesh)
    bm.free()
    for mname in names:
        mesh.materials.append(material(mname))
    obj = bpy.data.objects.new(name, mesh)
    col.objects.link(obj)
    if bevel:
        add_bevel(obj, bevel)
    print(f"[street] {name}: {len(mesh.polygons)} faces")
    return obj


def contour_chains(F):
    """The F = 0 contour as polylines of (x, y) points (closed ones repeat their first point)."""
    inside, full, boundary = _cells(F)
    adj = {}
    for j, i in zip(*np.nonzero(boundary)):
        _, segs = _cell_polys(inside, F, j, i)
        for a, b in segs:
            adj.setdefault(a, []).append(b)
            adj.setdefault(b, []).append(a)
    seen, chains = set(), []
    starts = [k for k, v in adj.items() if len(v) == 1] + list(adj)
    for s in starts:
        if s in seen:
            continue
        chain, cur, prev = [s], s, None
        seen.add(s)
        while True:
            nxt = [n for n in adj[cur] if n != prev and n not in seen]
            if not nxt:
                if len(chain) > 2 and chain[0] in adj[cur]:
                    chain.append(chain[0])
                break
            prev, cur = cur, nxt[0]
            seen.add(cur)
            chain.append(cur)
        pts = [_cross_pos(F, *k) for k in chain]
        if len(pts) >= 2:
            chains.append(pts)
    return chains


def simplify(pts, tol):
    """Douglas-Peucker, iterative, keeping the ends."""
    if len(pts) < 3:
        return pts
    arr = np.asarray(pts)
    keep = np.zeros(len(pts), bool)
    keep[0] = keep[-1] = True
    stack = [(0, len(pts) - 1)]
    while stack:
        i, j = stack.pop()
        if j - i < 2:
            continue
        d = seg_dist(arr[i + 1:j, 0], arr[i + 1:j, 1], tuple(arr[i]), tuple(arr[j]))
        k = int(np.argmax(d))
        if d[k] > tol:
            keep[i + 1 + k] = True
            stack += [(i, i + 1 + k), (i + 1 + k, j)]
    return [tuple(p) for p in arr[keep]]


# ------------------------------------------------------------------ the ground

def ground(col, fl):
    R, S, D, P, W = fl["R"], fl["S"], fl["D"], fl["P"], fl["W"]

    def road_grime(bm):
        """UVGrime: u runs with the kerb, v = metres out from the kerb face / 2.

        u is (x + y) / 8, continuous everywhere. It used to be the position along the kerb from
        the field's gradient, which flips direction across a junction's middle, so neighbouring
        vertices of one big face got u values metres apart and the silt drew as jagged sawtooth
        streaks through the Taft and Padre Faura box (owner: "weird texture issue on the
        intersection")."""
        layer = bm.loops.layers.uv.new("UVGrime")
        for f in bm.faces:
            for lp in f.loops:
                x, y = lp.vert.co.x, lp.vert.co.y
                r = sample(R, x, y)
                v = 0.999 if r < -GUTTER + 0.05 else min(0.999, max(0.0, -r / 2.0))
                lp[layer].uv = ((x + y) / 8.0, v)

    def clean_grime(bm):
        """The core carries no silt: UVGrime v = 1 everywhere."""
        layer = bm.loops.layers.uv.new("UVGrime")
        for f in bm.faces:
            for lp in f.loops:
                lp[layer].uv = ((lp.vert.co.x + lp.vert.co.y) / 8.0, 0.999)

    def pave_mat(x, y):
        return "street_pavement" if abs(x) <= PAVE_OUT + 0.01 else "street_sidewalk"

    # The road is two welded-edge pieces: the GUTTER BAND (the 2.2 m next to every kerb, whose
    # inner edge is its own contour, so the silt overlay interpolates cleanly across big faces)
    # and the clean CORE. The core's UVGrime is v = 1 everywhere (no silt).
    region_mesh("ground road gutters", np.maximum(R - KERB_W / 2, -R - GUTTER), 0.0, -0.25,
                lambda x, y: "street_asphalt", col, extra_uv=road_grime, bevel=0)
    region_mesh("ground road", R + GUTTER, 0.0, -0.25, lambda x, y: "street_asphalt", col, extra_uv=clean_grime,
                bevel=0)
    region_mesh("ground pavement", np.maximum(KERB_W - 0.02 - R, S), PAVE_TOP, -0.05, pave_mat, col, bevel=0.03)
    lot = -S - 0.02
    region_mesh("ground lawn", np.maximum(W, lot), LAWN_TOP, 0.12, lambda x, y: "street_lawn", col, bevel=0.04)
    region_mesh("ground parking", np.maximum.reduce([P, -W, lot]), LOT_TOP, 0.12, lambda x, y: "street_parking", col,
                bevel=0.02)
    region_mesh("ground drive", np.maximum.reduce([D, -P, -W, lot]), LOT_TOP, 0.12, lambda x, y: "street_drive", col,
                bevel=0.02)
    region_mesh("ground lot", np.maximum.reduce([-D, -P, -W, lot]), LOT_TOP, 0.12, lambda x, y: "street_lot", col,
                bevel=0.02)
    # Beyond the lattice: one plain plate under the fog, just below the road.
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=900)
    for v in bm.verts:
        v.co.z = -0.03
    for f in bm.faces:
        f.material_index = 0
    me = bpy.data.meshes.new("ground outer plate")
    bm.to_mesh(me)
    bm.free()
    me.materials.append(material("street_lot"))
    o = bpy.data.objects.new("ground outer plate", me)
    col.objects.link(o)


def kerbs(col, fl):
    """The kerb swept along the road's edge contour: road face at the contour, top KERB_TOP,
    back tucked 4 cm under the pavement. UVs: u along (m / 4), v across the profile (m)."""
    R = fl["R"]
    prof = L.fillet([(0.0, -0.12), (0.0, KERB_TOP), (KERB_W + 0.04, KERB_TOP), (KERB_W + 0.04, -0.12)], 0.035, 3)
    # Reverse so the loop runs road-bottom, up the road face, across the top, down the back.
    cum = [0.0]
    for (n0, z0), (n1, z1) in zip(prof, prof[1:] + prof[:1]):
        cum.append(cum[-1] + math.hypot(n1 - n0, z1 - z0))
    bm = bmesh.new()
    uv = bm.loops.layers.uv.verify()
    mats = ["street_kerb_white", "street_kerb_stripes"]
    total = 0
    for chain in contour_chains(R):
        closed = len(chain) > 3 and math.dist(chain[0], chain[-1]) < 1e-6
        pts = simplify(chain[:-1] if closed else chain, 0.004)
        if closed:
            pts = pts + [pts[0]]
        if len(pts) < 2:
            continue
        rings, s_along = [], [0.0]
        for k, (x, y) in enumerate(pts):
            nrm = grad(R, x, y, 0.2)
            p = Vector((x, y, 0))
            rings.append([bm.verts.new(p + nrm * n + UP * z) for n, z in prof])
            if k:
                s_along.append(s_along[-1] + math.dist(pts[k - 1], pts[k]))
        m = len(prof)
        for k in range(len(rings) - 1):
            r0, r1 = rings[k], rings[k + 1]
            mx = (pts[k][0] + pts[k + 1][0]) / 2
            mi = 0 if abs(mx) < KERB_IN + 1.0 else 1
            for q in range(m):
                f = bm.faces.new((r0[q], r1[q], r1[(q + 1) % m], r0[(q + 1) % m]))
                f.material_index = mi
                u0, u1 = s_along[k] / 4.0, s_along[k + 1] / 4.0
                for lp, (u, v) in zip(f.loops, ((u0, cum[q]), (u1, cum[q]), (u1, cum[q + 1]), (u0, cum[q + 1]))):
                    lp[uv].uv = (u, v)
                total += 1
        if not closed:
            for ring in (rings[0], rings[-1]):
                f = bm.faces.new(ring)
                f.material_index = 0 if abs(ring[0].co.x) < KERB_IN + 1.0 else 1
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new("kerbs")
    bm.to_mesh(me)
    bm.free()
    for mname in mats:
        me.materials.append(material(mname))
    o = bpy.data.objects.new("kerbs", me)
    col.objects.link(o)
    add_bevel(o, 0.015)
    print(f"[street] kerbs: {total} faces")


# ------------------------------------------------------------------ markings and chalk

def decal_slab(buf, poly, mat, top=0.008, base=0.0):
    """A flat marking: a thin slab, its top `top` above the surface at `base` (the road unless
    given), sunk 1 cm into it."""
    buf.extrude_z(poly, base - 0.01, base + top, mat)


def rect(cx, cy, hw, hh, rot=0.0):
    c, s = math.cos(rot), math.sin(rot)
    return [(cx + x * c - y * s, cy + x * s + y * c) for x, y in ((-hw, -hh), (hw, -hh), (hw, hh), (-hw, hh))]


def markings(col, fl, layout):
    R = fl["R"]
    # THE CHALK, on the lot (owner: "can we move the play area to this open space?"). The old
    # lines across Taft at |y| = 7 and 8 are gone: that stretch is plain road.
    cx, cy = COURT

    def chalk_edge(buf, side, half, mat):
        """One side of the square `half` about the court centre: a 12 cm line, exactly 2 * half
        long, each end mitred along the corner's diagonal so two sides never overlap."""
        hw = 0.06
        pts = [(half + hw, -half), (half + hw, half), (half, half), (half - hw, half - hw),
               (half - hw, hw - half), (half, -half)]                         # the east side
        c, s = {"east": (1, 0), "north": (0, 1), "west": (-1, 0), "south": (0, -1)}[side]
        decal_slab(buf, [(cx + x * c - y * s, cy + x * s + y * c) for x, y in pts], mat, top=0.006, base=LOT_TOP)

    # The box is FOUR objects, one per edge: the game finds each edge by its own bounds, so they
    # must never be merged into one mesh.
    for side in ("east", "west", "north", "south"):
        edge = SBuf(f"chalk box edge {side}")
        chalk_edge(edge, side, BOX, "street_chalk")
        edge.finish(col, bevel=0.003, segments=1)
    throw = SBuf("chalk throw line")
    for side in ("east", "west", "north", "south"):
        chalk_edge(throw, side, THROW, "street_chalk_yellow")
    throw.finish(col, bevel=0.003, segments=1)

    pot = SBuf("potholes")
    rng = random.Random(7)
    for k, (x, y) in enumerate(((3.4, -3.0), (-3.4, 3.0))):
        pts = []
        for q in range(14):
            a = q / 14 * math.tau
            rr = 0.62 * (1 + 0.09 * math.sin(3 * a + k) + rng.uniform(-0.04, 0.04))
            pts.append((x + rr * math.cos(a) * 1.15, y + rr * math.sin(a)))
        faces = pot.extrude_z(pts, -0.01, 0.004, "street_pothole")
        pot.decal([f for f in faces if f.normal.z > 0.9], (x - 0.65 * 1.15, y - 0.65, 0), (1 / 1.15, 0, 0),
                  (0, 1, 0), 1.3, 1.3)
    pot.finish(col, bevel=0.003, segments=1)

    paint = SBuf("road markings")

    def zebra_across_x(y0, y1, x0, x1, bar=0.45, gap=0.45):
        """Bars running along y (Taft traffic runs along y), laid across x."""
        x = x0 + 0.3
        while x + bar <= x1 - 0.2:
            if sample(R, x + bar / 2, (y0 + y1) / 2) < -0.2:
                decal_slab(paint, L.fillet(rect(x + bar / 2, (y0 + y1) / 2, bar / 2, (y1 - y0) / 2), 0.04, 2),
                           "street_paint_white")
            x += bar + gap

    def zebra_across_y(x0, x1, yc, half_span, bar=0.45, gap=0.45):
        """Bars running along x (Padre Faura's traffic), laid across y wherever it is road."""
        y = yc - half_span
        while y < yc + half_span:
            ok = all(sample(R, xx, y + bar / 2) < -0.3 for xx in (x0, x1))
            if ok:
                decal_slab(paint, L.fillet(rect((x0 + x1) / 2, y + bar / 2, (x1 - x0) / 2, bar / 2), 0.04, 2),
                           "street_paint_white")
            y += bar + gap

    # Taft: zebras where OSM has footways, stop lines before them.
    zebra_across_x(21.9, 24.9, -KERB_IN, KERB_IN)
    zebra_across_x(40.2, 43.2, -KERB_IN, KERB_IN)
    decal_slab(paint, rect(3.6, 20.8, 3.0, 0.15), "street_paint_white")
    decal_slab(paint, rect(-3.6, 44.3, 3.0, 0.15), "street_paint_white")
    # Manila's red intersection box: an outline with two big diagonals, chunky and worn.
    x0, x1, y0, y1 = -6.2, 6.2, 25.6, 39.6
    for a, b in (((x0, y0), (x1, y0)), ((x1, y0), (x1, y1)), ((x1, y1), (x0, y1)), ((x0, y1), (x0, y0)),
                 ((x0, y0), (x1, y1)), ((x1, y0), (x0, y1))):
        d = (b[0] - a[0], b[1] - a[1])
        L_ = math.hypot(*d)
        decal_slab(paint, rect((a[0] + b[0]) / 2, (a[1] + b[1]) / 2, L_ / 2 + 0.1, 0.11, math.atan2(d[1], d[0])),
                   "street_paint_red")
    # Padre Faura: zebras across both arms just off Taft.
    for xc in (-15.2, 15.0):
        lo, hi = road_span_y(R, xc)
        zebra_across_y(xc - 1.5, xc + 1.5, (lo + hi) / 2, (hi - lo) / 2)
    # Dashed lane lines on every side street, and ONE WAY arrows on Padre Faura.
    for r in streets(layout):
        n = r["lanes"]
        if n < 2:
            continue
        half = street_half(r)
        for (ax, ay), (bx, by) in zip(r["line"], r["line"][1:]):
            d = Vector((bx - ax, by - ay, 0))
            L_ = d.length
            if L_ < 1:
                continue
            d.normalize()
            nrm = Vector((-d.y, d.x, 0))
            for lane in range(1, n):
                off = -half + lane * (2 * half / n)
                t = 2.0
                while t + 3.0 < L_:
                    c = Vector((ax, ay, 0)) + d * (t + 1.5) + nrm * off
                    ends = [c + d * 1.6, c - d * 1.6]
                    if abs(c.y) > 17 and all(sample(R, e.x, e.y) < -0.6 for e in ends) and \
                            not (abs(c.x) < 18 and 20 < c.y < 45):
                        decal_slab(paint, rect(c.x, c.y, 1.5, 0.07, math.atan2(d.y, d.x)), "street_paint_white")
                    t += 8.0
    for xc in (-26.0, -60.0):
        y = sample_center_y(R, xc)
        for off in (-3.0, 0.0, 3.0):
            arrow(paint, xc, y + off, math.pi)
    paint.finish(col, bevel=0.003, segments=1)


def road_span_y(R, x):
    """Padre Faura's two road edges at a given x (it crosses Taft near y 32)."""
    ys = [y for y in np.arange(15, 50, 0.1) if sample(R, x, y) < 0]
    return (min(ys), max(ys)) if ys else (27.5, 36.5)


def sample_center_y(R, x):
    lo, hi = road_span_y(R, x)
    return (lo + hi) / 2


def arrow(buf, x, y, heading):
    """A painted straight-ahead arrow 2.6 m long pointing along `heading`."""
    shape = [(-1.3, -0.1), (0.4, -0.1), (0.4, -0.38), (1.3, 0.0), (0.4, 0.38), (0.4, 0.1), (-1.3, 0.1)]
    c, s = math.cos(heading), math.sin(heading)
    decal_slab(buf, [(x + px * c - py * s, y + px * s + py * c) for px, py in shape], "street_paint_white")


# ------------------------------------------------------------------ the Taft median planter

MEDIAN_RUNS = [(-150.0, -17.3), (17.3, 21.0), (44.6, 150.0)]
MEDIAN_HALF = 0.6
COLLAR = (1.25, 1.4)


def planter(buf, soil, plants, cx, cy, hx, hy, rng, r=0.5, around=None):
    """A planter: a dark green wall ring, a rounded light coping ring over it, soil inside."""
    wall_t, top = 0.16, 0.44
    outer = L.rounded_rect(hx, hy, r)
    inner = L.rounded_rect(hx - wall_t, hy - wall_t, max(0.05, r - wall_t))
    ring(buf, outer, inner, cx, cy, -0.06, top, "street_median_wall")
    co_out = L.rounded_rect(hx + 0.04, hy + 0.04, r + 0.04)
    co_in = L.rounded_rect(hx - wall_t - 0.03, hy - wall_t - 0.03, max(0.04, r - wall_t - 0.03))
    ring(buf, co_out, co_in, cx, cy, top - 0.03, top + 0.07, "street_coping")
    soil.extrude_z(L.rounded_rect(hx - wall_t + 0.015, hy - wall_t + 0.015, max(0.05, r - wall_t)), -0.04, top - 0.07,
                   "street_soil", offset=(cx, cy))
    # placeholder clumps along the planter, for the planting kit to replace; in a collar they
    # ring the column (`around` is the column's half width) instead of standing in it
    if around:
        gx, gy = (around + hx - 0.16) / 2, (around + hy - 0.16) / 2
        spots = [(cx + sx * gx, cy + sy * gy) for sx in (-1, 1) for sy in (-1, 1)]
        spots += [(cx + sx * gx, cy) for sx in (-1, 1)] + [(cx, cy + sy * gy) for sy in (-1, 1)]
    else:
        n = max(1, int((2 * max(hx, hy)) / 0.95))
        spots = [(cx + (0 if hx < hy else ((k + 0.5) / n - 0.5) * 2 * (hx - 0.35)),
                  cy + (((k + 0.5) / n - 0.5) * 2 * (hy - 0.35) if hx < hy else 0)) for k in range(n)]
    for px, py in spots:
        rad = rng.uniform(0.26, 0.36)
        plants.blob(Vector((px + rng.uniform(-0.06, 0.06), py, top - 0.07 + rad * 0.55)),
                    (rad, rad * 1.1, rad * 0.8), "street_plant_placeholder")
        PLANT_SPOTS.append((round(px, 2), round(py, 2)))


PLANT_SPOTS = []


def ring(buf, outer, inner, cx, cy, z0, z1, mat):
    """A closed wall ring between two outlines with the same point count."""
    bm = buf.bm
    idx = buf.mi(mat)
    rings = []
    for pts, z in ((outer, z0), (outer, z1), (inner, z1), (inner, z0)):
        rings.append([bm.verts.new((cx + x, cy + y, z)) for x, y in pts])
    faces = []
    k = len(outer)
    for a, b in zip(rings, rings[1:] + rings[:1]):
        for j in range(k):
            faces.append(bm.faces.new((a[j], a[(j + 1) % k], b[(j + 1) % k], b[j])))
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    for f in faces:
        f.material_index = idx


def median(col):
    buf, soil, plants = SBuf("median planter walls"), SBuf("median soil"), SBuf("median plant placeholders")
    rng = random.Random(21)
    for y0, y1 in MEDIAN_RUNS:
        # long runs are split every ~25 m at the pier rows, so each piece stays a sane length
        cuts = [y0] + [s * r for r in PIER_ROWS for s in (-1, 1) if y0 + 3 < s * r < y1 - 3 and y1 - y0 > 30] + [y1]
        cuts = sorted(cuts)
        for a, b in zip(cuts, cuts[1:]):
            planter(buf, soil, plants, 0.0, (a + b) / 2, MEDIAN_HALF, (b - a) / 2 - 0.25, rng, r=MEDIAN_HALF - 0.02)
    for row in PIER_ROWS:
        for sy in (-1, 1):
            for sx in (-1, 1):
                planter(buf, soil, plants, sx * L.PIER_X, sy * row, COLLAR[0], COLLAR[1], rng, r=0.55,
                        around=L.PIER_HALF + 0.05)
    buf.finish(col, bevel=0.03)
    soil.finish(col, bevel=0.0)
    plants.finish(col, bevel=0.0)
    # the MMDA sign in the north stub, facing the court
    return


# ------------------------------------------------------------------ furniture prototypes

def proto_pole(col, transformer=False, coil=False, seed=0):
    """A tapered concrete power pole with a steel crossarm toward the street (+x), insulators,
    and optional transformer and spare-cable coil. Returns local cable attachment points."""
    rng = random.Random(seed)
    b = SBuf(col.name)
    circle = [(math.cos(a) * 1.0, math.sin(a) * 1.0) for a in (k / 12 * math.tau for k in range(12))]
    f1 = b.extrude_z([(0.18 * x, 0.18 * y) for x, y in circle], -0.35, 9.6, "street_pole", top_scale=0.62)
    f2 = b.extrude_z([(0.23 * x, 0.23 * y) for x, y in circle], -0.1, 0.35, "street_pole", top_scale=0.9)   # a cast collar
    b.cylinder_uv(f1 + f2, 0.0, 0.0, 2.0)
    arm = SBuf(col.name + " steel")
    arm.box((0.35, 0, 8.45), (1.55, 0.13, 0.13), "street_galvanized", r=0.03)
    arm.tube([Vector((0.0, 0.0, 7.75)), Vector((0.7, 0.0, 8.4))], 0.035, "street_galvanized", sides=6)
    for x in (-0.3, 0.45, 1.0):
        arm.blob(Vector((x, 0, 8.6)), (0.06, 0.06, 0.1), "street_board_back")
    if transformer:
        arm.extrude_z([(0.3 * x - 0.45, 0.3 * y) for x, y in circle], 6.2, 7.2, "street_transformer")
        arm.extrude_z([(0.33 * x - 0.45, 0.33 * y) for x, y in circle], 7.15, 7.28, "street_transformer")
        arm.box((-0.22, 0, 6.45), (0.28, 0.1, 0.12), "street_galvanized")
        arm.box((-0.22, 0, 7.0), (0.28, 0.1, 0.12), "street_galvanized")
    arm.finish(col, bevel=0.015)
    wires = SBuf(col.name + " cable coil")
    if coil:
        # a coil of spare cable strapped to the pole: three fat loops, not a thin mess
        # hung from a strap on the street side, in a vertical plane across the pole
        for k in range(3):
            path = [Vector((0.2 + 0.04 * k, 0.28 * math.sin(a) + 0.03 * k, 6.2 - 0.3 - 0.3 * math.cos(a) + 0.02 * k))
                    for a in (q / 16 * math.tau for q in range(17))]
            wires.tube(path, 0.03, "street_wire", sides=6)
        wires.box((0.2, 0, 5.92), (0.1, 0.16, 0.1), "street_rail_black", r=0.02)
    b.finish(col, bevel=0.03)
    if coil:
        wires.finish(col, bevel=0)
    else:
        wires.bm.free()
    return {"power": [Vector((-0.3, 0, 8.66)), Vector((0.45, 0, 8.66)), Vector((1.0, 0, 8.66))],
            "tel": [Vector((0.2, 0.0, 7.05)), Vector((0.2, 0.05, 6.6)), Vector((0.19, -0.05, 6.25))]}


def proto_lamp(col):
    b = SBuf(col.name)
    circle = [(math.cos(a), math.sin(a)) for a in (k / 10 * math.tau for k in range(10))]
    b.extrude_z([(0.25 * x, 0.25 * y) for x, y in circle], -0.05, 0.12, "street_galvanized")
    b.extrude_z([(0.1 * x, 0.1 * y) for x, y in circle], 0.0, 7.6, "street_galvanized", top_scale=0.6)
    path = [Vector((0, 0, 7.4)), Vector((0.2, 0, 7.95)), Vector((0.7, 0, 8.2)), Vector((1.9, 0, 8.25))]
    b.tube(path, 0.05, "street_galvanized", sides=8)
    b.box((2.05, 0, 8.22), (0.75, 0.32, 0.16), "street_galvanized", r=0.07)
    b.box((2.08, 0, 8.12), (0.55, 0.22, 0.06), "street_lamp_glass", r=0.04)
    b.finish(col, bevel=0.02)


def signal_head(b, x, y, z):
    """A black three-aspect head on a backplate, facing -y, with chunky visors."""
    b.box((x, y + 0.02, z), (0.62, 0.05, 1.28), "street_signal_black", r=0.08)
    b.box((x, y - 0.12, z), (0.38, 0.28, 1.05), "street_signal_black", r=0.07)
    for k, (dz, mat) in enumerate(((0.33, "street_lens_red"), (0.0, "street_lens_amber"), (-0.33, "street_lens_green"))):
        disc = [(x + 0.12 * math.cos(a), z + dz + 0.12 * math.sin(a)) for a in (q / 12 * math.tau for q in range(12))]
        b.extrude_y(disc, y - 0.29, y - 0.2, mat)
        b.box((x, y - 0.36, z + dz + 0.14), (0.32, 0.22, 0.04), "street_signal_black", r=0.015)


def proto_signal(col, blade_mat, arm_dir=-1):
    """A galvanized mast arm: pole at the origin, the arm reaching along x * arm_dir, two heads
    and a countdown box facing -y, and the street-name blade on the arm."""
    b = SBuf(col.name)
    circle = [(math.cos(a), math.sin(a)) for a in (k / 12 * math.tau for k in range(12))]
    b.extrude_z([(0.32 * x, 0.32 * y) for x, y in circle], -0.1, 0.1, "street_galvanized")
    b.extrude_z([(0.16 * x, 0.16 * y) for x, y in circle], 0.0, 7.2, "street_galvanized", top_scale=0.75)
    s = arm_dir
    b.tube([Vector((0, 0, 6.4)), Vector((s * 2.0, 0, 6.55)), Vector((s * 5.4, 0, 6.72))], 0.075, "street_galvanized",
           sides=10)
    b.tube([Vector((0, 0, 7.05)), Vector((s * 1.6, 0, 6.6))], 0.04, "street_galvanized", sides=6)   # the tie
    for xh in (3.1, 4.9):
        b.box((s * xh, 0, 6.6), (0.12, 0.12, 0.3), "street_galvanized", r=0.03)
        signal_head(b, s * xh, 0.0, 5.85)
    b.box((s * 3.95, -0.05, 6.1), (0.5, 0.14, 0.4), "street_signal_black", r=0.05)
    b.panel(s * 3.95 - 0.19, s * 3.95 + 0.19, 5.95, 6.25, -0.14, -0.1, "street_sign_timer", "street_signal_black")
    # the blade hangs below the arm on two clamps
    xb = s * 1.9
    for dx in (-0.55, 0.55):
        b.box((xb + dx, 0, 6.35), (0.08, 0.08, 0.35), "street_galvanized", r=0.02)
    b.panel(xb - 0.85, xb + 0.85, 5.8, 6.22, -0.025, 0.025, blade_mat, "street_board_back", back_mat=blade_mat, r=0.05)
    b.finish(col, bevel=0.015)


def proto_blade_post(col):
    b = SBuf(col.name)
    circle = [(math.cos(a), math.sin(a)) for a in (k / 10 * math.tau for k in range(10))]
    b.extrude_z([(0.07 * x, 0.07 * y) for x, y in circle], -0.2, 3.35, "street_galvanized")
    # A blade runs PARALLEL to the street it names. The post stands unrotated, so the lower blade
    # (along x, east-west) names Padre Faura and the upper one (turned to run along y, north-south)
    # names Taft. They were the other way round (owner: "wrong signage").
    b.panel(0.05, 1.55, 2.95, 3.3, -0.02, 0.02, "street_sign_blade_faura", "street_board_back",
            back_mat="street_sign_blade_faura", r=0.05)
    # the second blade crosses above, turned 90 degrees
    q = SBuf("tmp")
    q.panel(0.05, 1.55, 3.36, 3.71, -0.02, 0.02, "street_sign_blade_taft", "street_board_back",
            back_mat="street_sign_blade_taft", r=0.05)
    bmesh.ops.rotate(q.bm, verts=q.bm.verts, cent=(0, 0, 0), matrix=Matrix.Rotation(math.pi / 2, 3, "Z"))
    for faces, o, au, av, w, h in q.decals:
        q_rot = Matrix.Rotation(math.pi / 2, 3, "Z")
        b.decals.append((faces, q_rot @ o, q_rot @ au, q_rot @ av, w, h))
    _merge(b, q)
    b.finish(col, bevel=0.015)


def _merge(dst, src):
    """Move src's geometry into dst (materials remapped); src's decal faces are re-found by
    position, because faces cannot move between bmeshes."""
    remap = [dst.mi(m) for m in src.mats]
    vmap = {v: dst.bm.verts.new(v.co) for v in src.bm.verts}
    fmap = {}
    for f in src.bm.faces:
        nf = dst.bm.faces.new([vmap[v] for v in f.verts])
        nf.material_index = remap[f.material_index]
        fmap[f] = nf
    dst.decals = [(([fmap.get(f, f) for f in faces]),) + tuple(rest) for faces, *rest in dst.decals]
    src.bm.free()


def proto_oneway(col, flip=False):
    b = SBuf(col.name)
    circle = [(math.cos(a), math.sin(a)) for a in (k / 10 * math.tau for k in range(10))]
    b.extrude_z([(0.055 * x, 0.055 * y) for x, y in circle], -0.2, 2.75, "street_galvanized")
    faces = b.panel(-0.5, 0.5, 2.25, 2.62, -0.07, -0.035, "street_sign_oneway", "street_board_back", r=0.04)
    if flip:
        faces_front = [f for f in faces if f.normal.y < -0.9]
        b.decals[-1] = (faces_front, Vector((0.5, 0, 2.25)), Vector((-1, 0, 0)), Vector((0, 0, 1)), 1.0, 0.37)
    b.box((0, -0.045, 2.35), (0.1, 0.06, 0.08), "street_galvanized", r=0.02)
    b.box((0, -0.045, 2.52), (0.1, 0.06, 0.08), "street_galvanized", r=0.02)
    b.finish(col, bevel=0.012)


def proto_bawal(col):
    b = SBuf(col.name)
    for x in (-0.3, 0.3):
        b.box((x, 0.0, 0.95), (0.07, 0.07, 2.1), "street_galvanized", r=0.02)
    b.panel(-0.42, 0.42, 1.35, 1.92, -0.065, -0.035, "street_sign_bawal", "street_board_back", r=0.04)
    b.finish(col, bevel=0.012)


def proto_barrier(col, seed=0):
    """A freestanding crowd barrier as in the Padre Faura photographs: a yellow frame, pickets
    alternating yellow and black, splayed flat feet. Chunky tubes, a little out of true."""
    rng = random.Random(seed)
    b = SBuf(col.name)
    L_, H = 2.2, 1.1
    for z in (0.18, H):
        b.tube([Vector((-L_ / 2, 0, z)), Vector((0, 0, z + rng.uniform(-0.015, 0.015))), Vector((L_ / 2, 0, z))], 0.035,
               "street_rail_yellow", sides=8)
    for x in (-L_ / 2, L_ / 2):
        b.tube([Vector((x, 0, 0.02)), Vector((x, 0, H + 0.02))], 0.04, "street_rail_yellow", sides=8)
        b.box((x, 0, 0.03), (0.09, 0.62, 0.05), "street_rail_yellow", r=0.02)
    n = 10
    for k in range(1, n):
        x = -L_ / 2 + k * L_ / n
        mat = "street_rail_yellow" if k % 2 else "street_rail_black"
        b.tube([Vector((x, 0, 0.16)), Vector((x + rng.uniform(-0.02, 0.02), 0, H + 0.02))], 0.024, mat, sides=6)
    b.finish(col, bevel=0.0)


def proto_shelter(col):
    """A simple SAKAYAN shelter, 4 m long: back toward +y, open to the road at -y."""
    b = SBuf(col.name)
    for x in (-1.85, 1.85):
        for y in (-0.7, 0.75):
            b.box((x, y, 1.25), (0.1, 0.1, 2.6), "street_shelter_green", r=0.03)
    # a gently curved roof sheet, thick and rounded
    arc = [(-1.05 + 2.1 * t, 2.62 + 0.18 * math.sin(math.pi * t)) for t in (q / 8 for q in range(9))]
    prof = [(y, z) for y, z in arc] + [(y, z + 0.07) for y, z in reversed(arc)]
    q = SBuf("roof")
    q.extrude_y(prof, -2.15, 2.15, "street_shelter_green")
    bmesh.ops.rotate(q.bm, verts=q.bm.verts, cent=(0, 0, 0), matrix=Matrix.Rotation(-math.pi / 2, 3, "Z"))
    _merge(b, q)
    b.box((0, 0.45, 0.45), (3.4, 0.42, 0.08), "street_coping", r=0.03)          # the bench
    for x in (-1.3, 1.3):
        b.box((x, 0.45, 0.22), (0.12, 0.34, 0.46), "street_coping", r=0.03)
    b.box((0, 0.78, 1.0), (3.6, 0.06, 0.08), "street_shelter_green", r=0.02)    # back rails
    b.box((0, 0.78, 1.6), (3.6, 0.06, 0.08), "street_shelter_green", r=0.02)
    # the SAKAYAN panel at the east end, facing the road
    q = SBuf("panel")
    q.panel(-0.42, 0.42, 1.0, 2.26, -0.02, 0.02, "street_sign_sakayan", "street_shelter_green",
            back_mat="street_sign_sakayan", r=0.03)
    rot = Matrix.Rotation(math.pi / 2, 3, "Z")
    bmesh.ops.rotate(q.bm, verts=q.bm.verts, cent=(0, 0, 0), matrix=rot)
    bmesh.ops.translate(q.bm, verts=q.bm.verts, vec=(1.9, 0.02, 0))
    for faces, o, au, av, w, h in q.decals:
        b.decals.append((faces, rot @ o + Vector((1.9, 0.02, 0)), rot @ au, rot @ av, w, h))
    _merge(b, q)
    b.finish(col, bevel=0.02)


def proto_barangay(col):
    """The hand-painted barangay board, 3.2 x 1.8 m on two red posts, with a painted crest
    cut into its top edge. The face looks along -y."""
    b = SBuf(col.name)
    for x in (-1.25, 1.25):
        b.box((x, 0.06, 1.9), (0.14, 0.14, 4.0), "street_board_red", r=0.04)
    W, H, z0 = 3.2, 1.8, 2.1
    crest = [(-0.9, z0 + H), (-0.55, z0 + H + 0.22), (0.0, z0 + H + 0.34), (0.55, z0 + H + 0.22), (0.9, z0 + H)]
    body = [(-W / 2, z0), (W / 2, z0), (W / 2, z0 + H)] + list(reversed(crest)) + [(-W / 2, z0 + H)]
    b.extrude_y(L.fillet(body, 0.08, 2), -0.01, 0.05, "street_board_red")
    b.panel(-W / 2 + 0.06, W / 2 - 0.06, z0 + 0.06, z0 + H - 0.06, -0.025, 0.0, "street_sign_barangay",
            "street_board_red", r=0.05)
    b.finish(col, bevel=0.015)


# ------------------------------------------------------------------ placing

def place(proto, target, loc, heading=0.0, tilt=(0.0, 0.0)):
    for o in proto.objects:
        c = o.copy()
        c.location = Vector(loc)
        c.rotation_euler = (tilt[0], tilt[1], heading)
        target.objects.link(c)
    return Matrix.Translation(Vector(loc)) @ Euler((tilt[0], tilt[1], heading)).to_matrix().to_4x4()


def sag_path(a, b, sag, lateral=Vector((0, 0, 0)), n=14):
    return [a + (b - a) * (k / n) + Vector((0, 0, -4 * sag * (k / n) * (1 - k / n))) + lateral * math.sin(math.pi * k / n)
            for k in range(n + 1)]


def cables_between(buf, pa, pb, rng, heavy=True):
    """A tangle between two poles: power lines at the crossarm, telecom and TV cables lower,
    one of them a fat twisted bundle, and now and then a low droop."""
    L_ = (pb["power"][0] - pa["power"][0]).length
    for a, b in zip(pa["power"], pb["power"]):
        buf.tube(sag_path(a, b, 0.012 * L_ + rng.uniform(0, 0.15)), 0.02, "street_wire", sides=5)
    for k, (a, b) in enumerate(zip(pa["tel"], pb["tel"])):
        sag = 0.03 * L_ + rng.uniform(0.0, 0.35)
        lat = Vector((rng.uniform(-0.15, 0.15), rng.uniform(-0.15, 0.15), 0))
        if k == 1 and heavy:
            for q in range(3):     # a twisted bundle of three
                ang = q / 3 * math.tau
                path = [p + Vector((0.045 * math.cos(ang + 1.2 * i), 0.045 * math.sin(ang + 1.2 * i), 0.045 * math.sin(ang + 1.2 * i)))
                        for i, p in enumerate(sag_path(a, b, sag, lat))]
                buf.tube(path, 0.028, "street_wire", sides=6)
        else:
            buf.tube(sag_path(a, b, sag, lat), 0.03, "street_wire", sides=6)
    if heavy and rng.random() < 0.45:
        a, b = pa["tel"][2], pb["tel"][2]
        buf.tube(sag_path(a, b, 0.07 * L_ + 0.4, Vector((0, 0, 0))), 0.034, "street_wire", sides=6)


def pole_line(protos, target, cable_buf, poles, rng):
    """poles: list of (x, y, heading, variant). Places them and strings cables pole to pole."""
    points = []
    for k, (x, y, h, var) in enumerate(poles):
        proto, att = protos[var]
        tilt = (math.radians(rng.uniform(-1.2, 1.2)), math.radians(rng.uniform(-1.2, 1.2)))
        M = place(proto, target, (x, y, 0), h, tilt)
        points.append({key: [M @ p for p in pts] for key, pts in att.items()})
    for pa, pb in zip(points, points[1:]):
        cables_between(cable_buf, pa, pb, rng)
    return points


def railing(buf, a, b, z0):
    """A yellow pedestrian railing along a - b: chunky posts every ~2 m, top and mid rails."""
    a, b = Vector((a[0], a[1], 0)), Vector((b[0], b[1], 0))
    L_ = (b - a).length
    n = max(1, round(L_ / 2.0))
    for k in range(n + 1):
        p = a + (b - a) * (k / n)
        buf.tube([p + Vector((0, 0, z0 - 0.15)), p + Vector((0, 0, z0 + 1.05))], 0.045, "street_rail_yellow", sides=8)
    for z in (0.55, 1.0):
        buf.tube([a + Vector((0, 0, z0 + z)), b + Vector((0, 0, z0 + z))], 0.04, "street_rail_yellow", sides=8)


# ------------------------------------------------------------------ fences and walls

PICKETS_R = 130.0


def fence_style(line):
    if any(y > 36 and -70 < x < 0 for x, y in line):
        return "court"
    return "iron"


def prepare_barrier(fl, line):
    """Snap a barrier line off the pavements: any vertex near Taft lands on the |x| = 11 wall
    line (x = 11.2 at its centre: the plinth's inner face at 11.05, the pillars' at 10.99), and any vertex still inside
    a sidewalk is pushed out to 15 cm behind its back edge."""
    out = []
    for x, y in line:
        if abs(x) < 13.0 and abs(y) < EXT:
            x = math.copysign(PAVE_OUT + 0.2, x if abs(x) > 1e-6 else -1)
        if sample(fl["S"], x, y) < 0.15:
            x, y = snap(fl["S"], x, y, 0.15)
        out.append((x, y))
    return out


def open_court_lot(line):
    """Take the PGH fence off Taft in front of the court lot and send it round the back (owner,
    2026-10-04: "then fix up the fences so the area is still open to the road."; confirmed: the
    fence along the road in front of the lot is removed, and re-routed behind the lot).

    OSM's campus fence comes east along Padre Faura's south side, turns the corner at Taft (the
    corner itself stands 35 cm west of the wall line, pushed off Padre Faura's sidewalk) and runs
    south down the wall line, x = FENCE_X. The run from that corner down to y = LOT_SOUTH is
    dropped, and so is the Padre Faura side east of x = LOT_WEST (owner, on the first result,
    where it still stood: "yes that fence needs to be removed"). The Padre Faura fence west of
    there stays and now ends where it crosses x = LOT_WEST. The Taft run turns west at
    y = LOT_SOUTH, goes to x = LOT_WEST, and north up to that same point, so the two share ONE
    pillar there (fences() builds a pillar once per spot) and it reads as a finished fence corner.
    Returns the pieces (the line itself if it is not that fence)."""
    for pts in (line, line[::-1]):
        on_taft = [abs(x - FENCE_X) < 1.0 for x, _ in pts]
        for i in range(1, len(pts) - 1):
            (px, py), (qx, qy) = pts[i - 1], pts[i]
            if not (on_taft[i] and not on_taft[i - 1] and px < LOT_WEST < qx and qy > LOT_SOUTH):
                continue
            k = i                                # the first vertex on the wall line south of the lot
            while k < len(pts) and on_taft[k] and pts[k][1] >= LOT_SOUTH:
                k += 1
            if k == len(pts) or not on_taft[k]:
                continue
            t = (LOT_WEST - px) / (qx - px)
            join = (LOT_WEST, py + (qy - py) * t)
            print(f"[street] court lot: the Taft fence is off from ({qx:.2f}, {qy:.2f}) down to y {LOT_SOUTH}, the Padre "
                  f"Faura side from there west to x {LOT_WEST}; the new run and the Padre Faura fence share the corner "
                  f"pillar at ({join[0]:.2f}, {join[1]:.2f})")
            return [pts[:i] + [join],
                    [join, (LOT_WEST, LOT_SOUTH), (FENCE_X, LOT_SOUTH)] + pts[k:]]
    return [line]


def fences(col, fl, layout):
    iron, court, wall = SBuf("fence PGH iron"), SBuf("fence Supreme Court white"), SBuf("walls concrete")
    # The bars are their own objects with no bevel (round tubes need none, and a bevel on
    # thousands of them cost 0.5 M triangles); beyond PICKETS_R from the court the fences keep
    # plinth, pillars and rails only, which is all the fog leaves of them.
    bars_buf = {"iron": SBuf("fence PGH iron bars"), "court": SBuf("fence Supreme Court white bars")}
    rng = random.Random(31)
    base = LOT_TOP - 0.1
    lines, opened = [], 0
    for bar in layout["barriers"]:
        line = [tuple(p) for p in bar["line"] if max(abs(p[0]), abs(p[1])) < EXT - 2]
        if len(line) < 2:
            continue
        line = prepare_barrier(fl, line)
        style = "wall" if bar["kind"] == "wall" else fence_style(line)
        pieces = open_court_lot(line) if style == "iron" else [line]
        opened += len(pieces) > 1
        lines += [(style, piece) for piece in pieces]
    if opened != 1:
        print(f"[street] COURT LOT? the Taft frontage fence was found {opened} times, want 1")
    # One pillar per spot: where two segments meet (and where the court lot's new run joins the
    # Padre Faura side) the second pillar would stand in the first, turned a few degrees.
    pillars = set()

    def pillar_free(p):
        key = (round(p[0], 2), round(p[1], 2))
        if key in pillars:
            return False
        pillars.add(key)
        return True

    for style, line in lines:
        for (x0, y0), (x1, y1) in zip(line, line[1:]):
            mx, my = (x0 + x1) / 2, (y0 + y1) / 2
            if sample(fl["R"], mx, my) < 0.5:
                continue                     # a gate across a road or drive
            Ls = math.hypot(x1 - x0, y1 - y0)
            if Ls < 0.3:
                continue
            d = ((x1 - x0) / Ls, (y1 - y0) / Ls)
            if style == "wall":
                wall.seg_box((x0, y0), (x1, y1), 0.11, base, LOT_TOP + 2.6, "street_wall_cream", ext=0.1)
                wall.seg_box((x0, y0), (x1, y1), 0.16, LOT_TOP + 2.55, LOT_TOP + 2.68, "street_coping", ext=0.14, r=0.05)
                n = max(1, int(Ls / (4.0 if math.hypot(mx - COURT[0], my - COURT[1]) < PICKETS_R else 9.0)))
                for k in range(n + 1):
                    p = (x0 + d[0] * Ls * k / n, y0 + d[1] * Ls * k / n)
                    if not pillar_free(p):
                        continue
                    wall.box((p[0], p[1], base + 1.3), (0.4, 0.4, 2.6), "street_wall_cream", r=0.05,
                             rot=math.atan2(d[1], d[0]))
                continue
            buf = iron if style == "iron" else court
            bb = bars_buf[style] if math.hypot(mx - COURT[0], my - COURT[1]) < PICKETS_R else None
            wmat = "street_wall_cream" if style == "iron" else "street_wall_white"
            bars = "street_fence_iron" if style == "iron" else "street_fence_white"
            buf.seg_box((x0, y0), (x1, y1), 0.15, base, LOT_TOP + 0.42, wmat, ext=0.12)
            bay = (3.0 if style == "iron" else 2.6) * (1 if bb else 2.4)
            n = max(1, round(Ls / bay))
            ph = 2.05 if style == "iron" else 2.25
            for k in range(n + 1):
                p = (x0 + d[0] * Ls * k / n, y0 + d[1] * Ls * k / n)
                if not pillar_free(p):
                    continue
                buf.box((p[0], p[1], base + ph / 2), (0.42, 0.42, ph), wmat, r=0.05, rot=math.atan2(d[1], d[0]))
                buf.box((p[0], p[1], base + ph + 0.07), (0.54, 0.54, 0.16), wmat, r=0.06, rot=math.atan2(d[1], d[0]))
            for k in range(n):
                a = Vector((x0 + d[0] * Ls * k / n, y0 + d[1] * Ls * k / n, 0))
                b = Vector((x0 + d[0] * Ls * (k + 1) / n, y0 + d[1] * Ls * (k + 1) / n, 0))
                dv = (b - a).normalized()
                a2, b2 = a + dv * 0.19, b - dv * 0.19
                span = (b2 - a2).length
                npk = max(2, int(span / 0.2)) if bb else 0
                if style == "iron":
                    for z in (LOT_TOP + 0.6, LOT_TOP + 1.8):
                        buf.seg_box(a2, b2, 0.035, z - 0.035, z + 0.035, bars, ext=0.03, r=0.015)
                    for q in range(1, npk):
                        p = a2 + (b2 - a2) * (q / npk)
                        top = LOT_TOP + 1.95 + rng.uniform(-0.01, 0.01)
                        bb.tube([p + Vector((0, 0, LOT_TOP + 0.38)), p + Vector((0, 0, top))], 0.028, bars, sides=6)
                else:
                    arch = [a2 + (b2 - a2) * (q / 10) + Vector((0, 0, LOT_TOP + 1.55 + 0.3 * math.sin(math.pi * q / 10)))
                            for q in range(11)]
                    (bb or buf).tube(arch, 0.045, bars, sides=8)
                    buf.seg_box(a2, b2, 0.04, LOT_TOP + 0.62, LOT_TOP + 0.7, bars, ext=0.03, r=0.02)
                    for q in range(1, npk):
                        t = q / npk
                        p = a2 + (b2 - a2) * t
                        top = LOT_TOP + 1.55 + 0.3 * math.sin(math.pi * t)
                        bb.tube([p + Vector((0, 0, LOT_TOP + 0.38)), p + Vector((0, 0, top + 0.03))], 0.032, bars, sides=6)
    iron.finish(col, bevel=0.015, segments=1)
    court.finish(col, bevel=0.015, segments=1)
    for b in bars_buf.values():
        b.finish(col, bevel=0)
    wall.finish(col, bevel=0.03)


# ------------------------------------------------------------------ furniture assembly

def furniture(kit, placed, fl, layout):
    rng = random.Random(41)
    S = fl["S"]
    protos = {}
    for name, kw in (("pole", {}), ("pole_tx", {"transformer": True}), ("pole_coil", {"coil": True})):
        c = collection("street_" + name, kit)
        protos[name] = (c, proto_pole(c, seed=len(protos), **kw))
    lamp = collection("street_lamp", kit)
    proto_lamp(lamp)
    sig_faura = collection("street_signal_faura", kit)
    proto_signal(sig_faura, "street_sign_blade_faura")
    sig_taft = collection("street_signal_taft", kit)
    proto_signal(sig_taft, "street_sign_blade_taft")
    bpost = collection("street_blade_post", kit)
    proto_blade_post(bpost)
    ow = collection("street_oneway", kit)
    proto_oneway(ow)
    ow_f = collection("street_oneway_flipped", kit)
    proto_oneway(ow_f, flip=True)
    bawal = collection("street_bawal_tumawid", kit)
    proto_bawal(bawal)
    barriers = []
    for k in range(3):
        c = collection(f"street_crowd_barrier_{k}", kit)
        proto_barrier(c, seed=k)
        barriers.append(c)
    shelter = collection("street_bus_shelter", kit)
    proto_shelter(shelter)
    brgy = collection("street_barangay_board", kit)
    proto_barangay(brgy)

    poles_col = collection("power poles", placed)
    cable_col = collection("overhead cables", placed)
    cables = SBuf("overhead cables")
    # The east line stands 1.2 m off the shopfront line, not 0.42: at 0.42 the poles ran through
    # the shop row's awnings, its signs and the PC Express lightbox, and their crossarms into the
    # facades (owner: "fix these canopy + pole clipping issues"). The east kit cuts its awnings
    # round what still stands under them.
    W, E = -(PAVE_OUT - 0.42), PAVE_OUT - 1.2
    var = lambda k: ("pole_tx" if k % 4 == 1 else "pole_coil" if k % 4 == 3 else "pole")
    # Taft, west (the campus side): crossarms toward the street (+x, heading 0).
    west_s = [(W, y, 0.0, var(k)) for k, y in enumerate((-100, -86, -72, -58, -44, -30, -17.4, -5.0, 6.0, 17.4))]
    sw_corner = snap(S, -PAVE_OUT + 0.4, 25.0, -0.45)
    nw_corner = snap(S, -PAVE_OUT + 0.4, 39.5, -0.45)
    west_s.append((sw_corner[0], sw_corner[1], 0.0, "pole_tx"))
    west_n = [(nw_corner[0], nw_corner[1], 0.0, "pole")] + \
             [(W, y, 0.0, var(k)) for k, y in enumerate((54, 68, 82, 96, 110))]
    east_s = [(E, y, math.pi, var(k + 2)) for k, y in enumerate((-100, -86, -72, -58, -44, -31, -17.3, -6.2, 6.8, 17.3))]
    se_corner = snap(S, PAVE_OUT - 0.4, 24.5, -0.45)
    ne_corner = snap(S, PAVE_OUT - 0.4, 39.0, -0.45)
    east_s.append((se_corner[0], se_corner[1], math.pi, "pole_coil"))
    east_n = [(ne_corner[0], ne_corner[1], math.pi, "pole")] + \
             [(E, y, math.pi, var(k + 1)) for k, y in enumerate((53, 67, 81, 95, 109))]
    lines = {}
    for name, pl in (("west_s", west_s), ("west_n", west_n), ("east_s", east_s), ("east_n", east_n)):
        lines[name] = pole_line(protos, poles_col, cables, pl, rng)
    # Across the Padre Faura mouths (long, low spans) and one bundle across Taft under the
    # viaduct, sagging to about 6.3 m.
    for a, b in ((lines["west_s"][-1], lines["west_n"][0]), (lines["east_s"][-1], lines["east_n"][0])):
        cables_between(cables, a, b, rng)
    a, b = lines["west_s"][-1], lines["east_s"][-1]
    for k in range(3):
        cables.tube(sag_path(a["tel"][k], b["tel"][k], 0.55 + 0.12 * k, Vector((0, 0.3 * (k - 1), 0)), n=20),
                    0.03, "street_wire", sides=6)
    # Padre Faura's own pole rows, on the back edge of each sidewalk, crossarms over the street.
    for side, xs_ in (("south", (-26, -42, -58.7, -74, -88.2, -104, -120)), ("north", (-26, -44, -62, -76, -88.1, -106.1, -122))):
        pl = []
        for k, x in enumerate(xs_):
            y0 = sample_center_y(fl["R"], x)
            y = y0 - 6.0 if side == "south" else y0 + 6.0
            px, py = snap(S, x, y, -0.4)
            pl.append((px, py, math.pi / 2 if side == "south" else -math.pi / 2, var(k)))
        corner = lines["west_s"][-1] if side == "south" else lines["west_n"][0]
        pts = pole_line(protos, poles_col, cables, pl, rng)
        cables_between(cables, corner, pts[0], rng)
    polys = [bd["poly"] for bd in layout["buildings"]]

    def clearance(px, py):
        return min(0.0 if B.point_in_poly(px, py, poly) else
                   min(float(seg_dist(px, py, tuple(poly[k - 1]), tuple(poly[k]))) for k in range(len(poly)))
                   for poly in polys)

    def clear_spot(x, y):
        # OSM buildings come right up to this sidewalk: building 39's front stood 0.4 m from the
        # x = 54 pole, and the east kit grows footprints 8 cm and runs slab bands 0.38 m proud. Try
        # spots along the street, the nearest first, up to 8 m either way, until one is 0.75 m clear
        # of every footprint (bands, growth and the pole's own radius).
        for dx in [0.0] + [s * k * 0.5 for k in range(1, 17) for s in (1, -1)]:
            px, py = snap(S, x + dx, y, -0.4)
            if clearance(px, py) >= 0.75:
                return px, py
        return snap(S, x, y, -0.4)

    east_arm = []
    for k, x in enumerate((26, 40, 54, 68, 82)):
        y0 = sample_center_y(fl["R"], x)
        px, py = clear_spot(x, y0 - 6.0)
        east_arm.append((px, py, math.pi / 2, var(k + 1)))
    pts = pole_line(protos, poles_col, cables, east_arm, rng)
    cables_between(cables, lines["east_s"][-1], pts[0], rng)
    # Service drops from the east poles into the facades (x = 11 and beyond).
    for pole in lines["east_s"][6:10]:
        p = pole["tel"][0]
        q = Vector((PAVE_OUT + 0.25, p.y + rng.uniform(-2.5, 2.5), rng.uniform(4.8, 5.8)))
        cables.tube(sag_path(p, q, 0.35), 0.022, "street_wire", sides=5)
    cables.finish(cable_col, bevel=0)

    # Street lamps: arms over the road, never inside the chalk box's air.
    lamps = collection("street lamps", placed)
    for x, y, h in ((-(PAVE_OUT - 0.35), 0.5, 0.0), ((PAVE_OUT - 0.88), -10.0, math.pi),
                    (-(PAVE_OUT - 0.35), -51.0, 0.0), (-(PAVE_OUT - 0.35), -79.0, 0.0), (-(PAVE_OUT - 0.35), 61.0, 0.0),
                    (-(PAVE_OUT - 0.35), 89.0, 0.0), ((PAVE_OUT - 0.88), -47.0, math.pi), ((PAVE_OUT - 0.88), -75.0, math.pi),
                    ((PAVE_OUT - 0.88), 60.0, math.pi), ((PAVE_OUT - 0.88), 88.0, math.pi)):
        place(lamp, lamps, (x, y, PAVE_TOP - 0.05), h)
    for x in (-34, -60, -86, -112):
        for side in (-1, 1):
            y0 = sample_center_y(fl["R"], x + (13 if side > 0 else 0))
            xx = x + (13 if side > 0 else 0)
            px, py = snap(S, xx, y0 + side * 6.0, -0.35)
            place(lamp, lamps, (px, py, PAVE_TOP - 0.05), -side * math.pi / 2)

    # Traffic signals at the Padre Faura corners (OSM traffic_signals), and a blade post.
    sigs = collection("traffic signals", placed)
    place(sig_faura, sigs, (se_corner[0] - 0.3, se_corner[1] - 0.9, PAVE_TOP - 0.05), 0.0)
    place(sig_faura, sigs, (nw_corner[0] + 0.3, nw_corner[1] + 0.9, PAVE_TOP - 0.05), math.pi)
    place(sig_taft, sigs, (ne_corner[0] + 0.9, ne_corner[1] + 0.2, PAVE_TOP - 0.05), math.pi / 2)
    place(bpost, sigs, (sw_corner[0] + 0.1, sw_corner[1] + 1.0, PAVE_TOP - 0.05), 0.0)

    signs = collection("signs", placed)
    # ONE WAY: the texture's arrow points along the panel's +x. Padre Faura runs west.
    s1 = snap(S, -16.5, 26.0, -0.5)
    place(ow, signs, (s1[0], s1[1], PAVE_TOP - 0.05), math.pi)          # faces north, arrow west
    s2 = snap(S, -17.5, 38.5, -0.5)
    place(ow_f, signs, (s2[0], s2[1], PAVE_TOP - 0.05), 0.0)            # faces south, arrow west
    s3 = snap(S, 16.5, 25.5, -0.5)
    place(ow, signs, (s3[0], s3[1], PAVE_TOP - 0.05), math.pi)
    place(bawal, signs, (0.0, 20.2, 0.36), 0.0)                        # in the median stub, facing the court
    # y 21.6, not 20.6: at 20.6 the board's panel ran through the shop row's end awning (y ..20.0).
    place(brgy, signs, (PAVE_OUT - 0.75, 21.6, PAVE_TOP - 0.05), math.radians(-90 + 25))

    # Railings along the kerb at the crossings (outside the play area) and crowd barriers.
    rails = SBuf("railings yellow")
    for s in (-1, 1):
        x = s * (BOX + 0.35)
        railing(rails, (x, 17.3), (x, 21.4), PAVE_TOP)
        railing(rails, (x, 44.8), (x, 52.8), PAVE_TOP)
        railing(rails, (x, -17.3), (x, -23.3), PAVE_TOP)
    rails.finish(collection("railings", placed), bevel=0)
    bar = collection("crowd barriers", placed)
    place(barriers[0], bar, (-(PAVE_OUT - 0.35), 2.2, PAVE_TOP - 0.03), math.pi / 2, (0, math.radians(-4)))
    place(barriers[1], bar, (-8.2, 24.2, -0.02), math.radians(8))
    place(barriers[2], bar, (-5.6, 24.6, -0.02), math.radians(-5))
    place(barriers[1], bar, (8.9, 40.4, PAVE_TOP - 0.03), math.radians(80))

    # Bus stops from OSM, on the back edge of the sidewalk, open to the road.
    stops = collection("bus stops", placed)
    for p in layout["points"]:
        if p["kind"] != "bus_stop" or max(abs(p["at"][0]), abs(p["at"][1])) > EXT - 10:
            continue
        x, y = p["at"]
        if abs(x) < 13:
            x = math.copysign(PAVE_OUT - 0.95, x)
        else:
            x, y = snap(S, x, y, -0.95)
        g = grad(S, x, y)
        place(shelter, stops, (x, y, PAVE_TOP - 0.03), math.atan2(g.y, g.x) - math.pi / 2)


# ------------------------------------------------------------------ review

SHOTS = [
    ("spawn_north", Vector((0.0, -9.0, 1.25)), Vector((0, 30, 3)), "eye"),
    ("west_pavement_east", Vector((-9.5, -4.0, PAVE_TOP + 1.25)), Vector((12, 6, 3)), "eye"),
    ("faura_eye", Vector((0.0, 15.0, 1.25)), Vector((-4, 40, 3)), "eye"),
    ("taya_south", Vector((0.0, 4.0, 1.25)), Vector((0, -30, 2)), "eye"),
    ("east_pavement_north", Vector((9.3, -13.0, PAVE_TOP + 1.25)), Vector((3, 30, 3.5)), "eye"),
    ("median_close", Vector((3.4, -25.8, 1.7)), Vector((0.2, -19.5, 0.3)), 30),
    ("kerb_close", Vector((5.3, -8.8, 0.85)), Vector((7.0, -6.9, 0.12)), 30),
    ("signal_close", Vector((5.0, 14.0, 2.4)), Vector((9.5, 24.0, 5.2)), 26),
    ("fence_close", Vector((-8.4, -1.5, 1.5)), Vector((-11.1, 2.0, 1.0)), 28),
    ("busstop_close", Vector((-6.8, -40.0, 1.7)), Vector((-10.0, -33.8, 1.4)), 28),
    ("barangay_close", Vector((4.8, 14.0, 2.0)), Vector((10.4, 20.8, 3.1)), 30),
    ("cables_close", Vector((-5.5, -12.0, 1.5)), Vector((-10.6, 0.0, 7.2)), 30),
    ("aerial", Vector((30.0, -48.0, 46.0)), Vector((0, 18, 0)), 24),
    ("aerial_junction", Vector((-42.0, -12.0, 38.0)), Vector((-6, 32, 0)), 26),
    ("plan_junction", Vector((0.0, 12.0, 150.0)), Vector((0, 12.01, 0)), "ortho"),
]
BARE = {"aerial", "plan_junction"}         # shots taken without the context buildings


def context_for_review(layout):
    """Added AFTER the save: the LRT-1 guideway and the blockout's OSM buildings, so the renders
    read at the right scale. None of it is part of this kit."""
    lrt = SOURCE / "lrt_kit.blend"
    if lrt.exists():
        with bpy.data.libraries.load(str(lrt), link=False) as (src, dst):
            dst.collections = [c for c in src.collections if c in ("guideway over the court",)]
        for c in dst.collections:
            if c is not None:
                bpy.context.scene.collection.children.link(c)
    root = collection("context (blockout buildings)")
    B.buildings(root, layout)
    fig = bpy.data.meshes.new("figure")
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=12, radius1=0.25, radius2=0.25, depth=1.7,
                          matrix=Matrix.Translation((-3.0, -2.5, 0.85)))
    bm.to_mesh(fig)
    bm.free()
    fm = bpy.data.materials.new("figure")
    fm.use_nodes = True
    fm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.85, 0.35, 0.55, 1)
    fig.materials.append(fm)
    root.objects.link(bpy.data.objects.new("1.7 m figure", fig))


def preview(version, only=None):
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 1000
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    cam.data.clip_end, cam.data.clip_start = 2000, 0.05
    scene.collection.objects.link(cam)
    scene.camera = cam
    eye = 18 / math.tan(math.radians(95 / 2))
    for name, pos, tgt, lens in SHOTS:
        if only and name not in only:
            continue
        cam.data.type = "ORTHO" if lens == "ortho" else "PERSP"
        cam.data.ortho_scale = 70
        if lens != "ortho":
            cam.data.lens = eye if lens == "eye" else lens
        cam.location = pos
        ctx = bpy.data.collections.get("context (blockout buildings)")
        if ctx:
            ctx.hide_render = name in BARE
        lrt = bpy.data.collections.get("guideway over the court")
        if lrt:
            lrt.hide_render = name == "plan_junction"
        cam.rotation_euler = (tgt - pos).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = str(PREVIEWS / f"street_{name}_v{version}.png")
        bpy.ops.render.render(write_still=True)
        print("[street] preview", scene.render.filepath)


def verify():
    """Check the contract on the built geometry: ray-cast straight down across the play area and
    report the top surface at each station, and list anything of this kit standing taller than
    0.03 m inside the chalk box or mid-pavement (|x| 7..10.3) inside the walls."""
    dg = bpy.context.evaluated_depsgraph_get()
    scene = bpy.context.scene
    for x, want in ((0.0, 0.0), (3.0, 0.0), (6.5, 0.0), (6.8, KERB_TOP), (6.95, KERB_TOP), (7.2, PAVE_TOP),
                    (9.0, PAVE_TOP), (10.9, PAVE_TOP)):
        for sx in (-1, 1):
            for y in (-16.0, -6.0, 5.0, 16.0):
                hit = scene.ray_cast(dg, Vector((sx * x, y, 3.0)), Vector((0, 0, -1)))
                z = hit[1].z if hit[0] else None
                if z is None or abs(z - want) > 0.004:
                    print(f"[street] CONTRACT? top at ({sx * x}, {y}) is {z}, want {want}")
    print("[street] contract heights checked")
    bad = []
    for o in bpy.data.objects:
        if o.type != "MESH" or any(c.hide_render for c in o.users_collection) or o.name.startswith(("ground", "kerbs")) \
                or any(c.name.startswith("street_") for c in o.users_collection):
            continue
        for v in o.data.vertices:
            p = o.matrix_world @ v.co
            floor = 0.0 if abs(p.x) < KERB_IN else KERB_TOP if abs(p.x) < BOX else PAVE_TOP
            if abs(p.y) < WALL_Y and abs(p.x) < 10.3 and floor + 0.03 < p.z < 2.5:
                bad.append((o.name, tuple(round(c, 2) for c in p)))
                break
    print("[street] solids in the box or mid-pavement:", bad or "none")
    # The court on the lot: one flat lot surface under the whole keep-clear square (the chalk 6 mm
    # proud of it), every chalk edge on its own line, and nothing of this kit standing in it.
    cx, cy = COURT
    for dx in (-9.4, -7.0, -3.5, 0.0, 3.5, 7.0, 9.4):
        for dy in (-9.4, -7.0, -3.5, 0.0, 3.5, 7.0, 9.4):
            hit = scene.ray_cast(dg, Vector((cx + dx, cy + dy, 3.0)), Vector((0, 0, -1)))
            chalk = hit[0] and hit[4].name.startswith("chalk")
            want = LOT_TOP + (0.006 if chalk else 0.0)
            if not hit[0] or abs(hit[1].z - want) > 0.004 or not hit[4].name.startswith(("ground lot", "chalk")):
                print(f"[street] COURT? top at ({cx + dx}, {cy + dy}) is {hit[1].z if hit[0] else None} on "
                      f"{hit[4].name if hit[0] else None}, want {want} on the lot")
    for side, (ex, ey) in (("east", (BOX, 0)), ("west", (-BOX, 0)), ("north", (0, BOX)), ("south", (0, -BOX))):
        o = bpy.data.objects[f"chalk box edge {side}"]
        pts = [o.matrix_world @ v.co for v in o.data.vertices]
        lo = [min(q[i] for q in pts) for i in range(2)]
        hi = [max(q[i] for q in pts) for i in range(2)]
        mid, size = [(lo[i] + hi[i]) / 2 for i in range(2)], [hi[i] - lo[i] for i in range(2)]
        off = math.hypot(mid[0] - cx - ex, mid[1] - cy - ey)
        flag = "" if off < 0.2 and abs(max(size) - 2 * BOX) < 1e-4 else "  <-- COURT?"
        print(f"[street] chalk box edge {side}: centre ({mid[0]:.3f}, {mid[1]:.3f}), {max(size):.3f} m long, "
              f"{min(size):.3f} wide, {off:.3f} m off its edge{flag}")
    bad = []
    for o in bpy.data.objects:
        if o.type != "MESH" or any(c.hide_render for c in o.users_collection) or o.name.startswith(("ground", "chalk")):
            continue
        for v in o.data.vertices:
            q = o.matrix_world @ v.co
            if abs(q.x - cx) <= KEEP_CLEAR and abs(q.y - cy) <= KEEP_CLEAR and q.z < 4.0:
                bad.append((o.name, tuple(round(c, 2) for c in q)))
                break
    print("[street] solids in the court lot's keep-clear square:", bad or "none")


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
    only = argv[argv.index("--only") + 1].split(",") if "--only" in argv else None
    layout = B.sightline_override(json.loads(B.LAYOUT.read_text(encoding="utf-8")))
    bpy.ops.wm.read_factory_settings(use_empty=True)
    fl = build_fields(layout)
    ground_col = collection("street ground")
    ground(ground_col, fl)
    kerbs(ground_col, fl)
    markings(collection("street markings and chalk"), fl, layout)
    median(collection("street median planter"))
    fences(collection("street fences and walls"), fl, layout)
    kit = collection("street furniture (prototypes)")
    kit.hide_render = True
    furniture(kit, collection("street furniture (placed)"), fl, layout)
    L.lighting()
    verify()
    SOURCE.mkdir(parents=True, exist_ok=True)
    out = SOURCE / "street.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(out), compress=True)
    bak = SOURCE / "street.blend1"
    if bak.exists():
        bak.unlink()
    print("[street] saved", out)
    print("[street] median plant placeholder spots:", len(PLANT_SPOTS))
    if version:
        context_for_review(layout)
        preview(version, only)


if __name__ == "__main__":
    main()
