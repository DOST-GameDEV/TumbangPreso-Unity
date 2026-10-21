"""Model the Ilalim ng Tulay landmark buildings: three silhouettes that give the district an identity
(ILALIM-1.3, landmarks kit).

  py -3 tools/author_ilalim_textures_landmarks.py [--sheet N]     # paint the land_ textures first
  blender -b --python tools/author_ilalim_landmarks.py -- [--preview N] [--only a,b]

Writes ArtSource/ilalim/landmarks.blend with ONE top-level placed collection, "landmarks (placed)",
holding a child collection per building. With --preview it writes versioned close-ups (against
plain stand-in ground) to Logs/ilalim-blender/landmark_<shot>_vN.png; the views from the court are
rendered from the assembled city (ilalim_city.blend with the landmarks linked in).

Owner, 2026-09-30: "can you think of a way to make the place look more lively, more unique building
shapes etc?", then "proceed". The east side's background blocks are generic mid-rises and LOD boxes.
Three of them, picked for what the court can see (ray casts from the spawn (0, -9, 1.25), the taya
spot (0, 4, 1.25) and the hoop (-9, 0, 1.46), each site probed with a 90 m tall stand-in), are
REPLACED by a building with its own construction (KANTO_DESIGN_GUIDE.md section 4):

  SITE                 REPLACES (hidden by the city assembly, listed in landmarks.json)
  land_condo           east_bldg_119 and east_bldg_119_kit (the 4-storey block behind the corner
                       sari-sari store, north of Padre Faura). The probe was seen from all three
                       points; from the hoop the WHOLE height to 90 m, so the crane's jib is
                       against the sky from the west pavement.
  land_deco            east_mirasol_building (an LOD block, the south-east corner of Taft and
                       G. Apacible Street). Its rounded corner looks up Taft toward the court.
  land_sixties         east_bldg_44 (an LOD block, the north-east corner of the same junction).

  Both southern sites read from the hoop and the west pavement down Taft, as a pair framing the
  G. Apacible junction; the spawn sees their upper floors past Cosmopolitan Church.

Frame: Blender X = game x (east), Y = game z (north, along Taft), heights absolute; the origin is
the court centre on Taft. The lots are at 0.24 (street kit, ray cast), the pavements at 0.212, the
roads at 0.0. Each object's ORIGIN is its building's anchor (the lot corner at ground level, in
ANCHORS below), so the Unity builder can place a whole building with one position.

1. TANAW RESIDENCES, a condo tower UNDER CONSTRUCTION (invented developer DALISAY LAND).
     tower    x 23.8..38.0, y 48.4..67.4 (corners rounded 1.2 m). A lobby storey (4.6 m) and
              18 storeys of 3.1 m. Floors 1 to 12 are finished: cream walls drawn with sliding doors
              and windows (land_condo_storey), a chunky slab band at every floor, sage accent fins
              up each long face. Floors 13 to 15 have hollow-block infill; floors 13 to 16 are
              wrapped in GREEN SAFETY NETTING hung half a metre out (cards painted to look
              see-through, land_netting), over a plywood catch fan at floor 13 and a DALISAY LAND
              / TANAW tarp. Floors 17 and 18 are bare slabs and columns with sky between them, and
              the top slab (60.6 up) carries column stubs with painted rebar, plywood edge forms,
              pallets of blocks and a concrete bucket. A construction hoist mast runs up the west
              face with its cage at floor 9.
     podium   x 26.2..38.0, y 67.2..82.6, two finished storeys (8 m) of shops and amenities, set in
              from x 23.2 so the narra canopy at x <= 25.5 stays clear.
     crane    a flat-top tower crane, YELLOW, mast 2 m square at (31.0, 43.3) on a concrete pad in the
              site yard, 66.5 m to the slewing unit, a cream cab, a 42 m jib toward the north-west
              (over the tower's west half) and a 13 m counter-jib with concrete counterweights. Its
              lattice is PAINTED on chunky members (land_crane, a cutout), never modelled struts.
              A tie frame braces the mast to the tower at floor 12.
     yard     the empty lot between Padre Faura's north pavement and the tower: a 2.4 m hoarding
              along y 38.75 (x 22.95..39.4) carrying the TANAW RESIDENCES artwork, green returns
              north to the tower with the site gate and a SAFETY FIRST sign on the east side, the
              green SITE OFFICE container, a formwork stack, cement pallets and a sand heap. The
              fig tree canopies at y <= 38.4 and the sari-sari gate wall (x <= 22.75) stay clear.
2. EDIFICIO AMIHAN, an art deco corner block (invented name, "1939").
     x 11.0..27.0, y -152.9..-134.9, four storeys (roof 14.84, parapet 15.8), the corner on Taft and
     G. Apacible rounded at 6 m. Ivory render; every upper floor is a deep spandrel band with a
     recessed steel ribbon window that wraps the round corner (land_deco_ribbon), and three sage
     "speed line" mouldings run round each spandrel. A deco eyebrow canopy wraps the shop floor at
     4.1 m, over chunky piers. On the corner apex a ROSE BLADE FIN rises to 24 m, square to Taft so
     its faces look up and down the avenue, with AMIHAN down both faces above the parapet, capped
     in three steps; the parapet steps up twice over the round corner.
     The entrance on Taft carries the EDIFICIO AMIHAN 1939 plaque. A rounded stair house and a
     tank on the roof.
3. MAKABAYAN BUILDING, a 1960s office block with a brise-soleil grid (invented name).
     The lot is only x 11.0..35.2, y -116.9..-109.8, so, as Manila's 1960s blocks do, the five
     upper floors (4.44 to 21.44) CANTILEVER over the G. Apacible pavement to y -119.2 on a row of
     tapered round pilotis at y -118.7: an arcade over the walk. The south and Taft faces carry
     a deep egg-crate grid: white shelves at every floor and vertical fins every 1.6 m turned 12
     degrees, 0.95 m deep, in front of a deep green wall with jalousie windows over mustard
     spandrels (land_sixties_cell). The north and east ends are plain pale mustard walls. A thin
     roof slab floats 0.6 m past the grid, the MAKABAYAN BUILDING letters stand on the Taft
     fascia, and a folded-plate (zigzag) canopy shades the roof terrace beside the tank room.

THE HOUSE STYLE (KANTO_DESIGN_GUIDE.md sections 2 to 4, LAGOON_REWORK_GUIDE.md section 2), through
the prop kit's PBuf (tools/author_ilalim_props.py) extended here as LBuf:
  * chunky, never fiddly: the crane's lattice and the netting's mesh are painted, members are
    thick and rounded; every piece has a live Bevel with hardened normals;
  * NO TWO SURFACES SHARE A PLANE: bands, fins and signs are proud of or sunk into what carries
    them by 2 to 25 cm, footings sink 5 cm into the lot, and each landmark sits inside its own
    lot line so no party wall is shared with a neighbour;
  * world UVs per material in metres (MATERIALS below), storey-aligned for facade drawings, with
    arc-length UVs round every footprint so windows run unbroken round the deco corner;
  * positional grime: UVGrime (v = metres below the band or slab above, over 3 m) and UVSplash
    (metres above the ground, over 1.5 m), multiplied by land_grime_drips and land_grime_splash;
  * the flat roofs use the shared anti-tiling chain (tools/ilalim_antitile.py).

ROLE HUES (Art_Direction.md section 1): nothing near #f87020 or #0080e8. The crane is yellow, the
netting green, the plywood tan, the deco accents rose and sage, the sixties wall deep green and
mustard, the glass teal-grey. Invented names only: TANAW RESIDENCES, DALISAY LAND, EDIFICIO
AMIHAN, MAKABAYAN BUILDING.

FOR ILALIM-1.4 (Unity): the crane material (land_crane) is a CUTOUT and double-sided; the netting
is opaque; the grime needs the two extra UV channels or a bake.
"""
import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import author_ilalim_props as P                          # noqa: E402  (PBuf, prop materials)
from author_ilalim_props import PBuf, facing, collection  # noqa: E402
from author_ilalim_lrt import fillet                     # noqa: E402
from ilalim_antitile import anti_tile                    # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource" / "ilalim"
TEXTURES = SOURCE / "textures"
PREVIEWS = ROOT / "Logs" / "ilalim-blender"
UP = Vector((0, 0, 1))
LOT = 0.24
PAVE = 0.212
GRIME_H, SPLASH_H = 3.0, 1.5

ANCHORS = {"condo": Vector((23.8, 48.4, LOT)), "deco": Vector((11.0, -134.9, LOT)),
           "sixties": Vector((11.0, -109.8, LOT))}

# ------------------------------------------------------------------ materials
# name: (texture, tint or None, roughness, metallic, tile (u m, v m) or None for decal UVs, flags)
# flags: g = positional grime, a = alpha cutout, t = anti-tiling
MATERIALS = {
    "land_concrete_raw":   ("land_concrete_raw", None, 0.9, 0.0, (4, 4), "g"),
    "land_slab_raw":       ("land_concrete_raw", (0.95, 0.94, 0.92), 0.9, 0.0, (4, 4), "g"),
    "land_blockwork":      ("land_blockwork", None, 0.9, 0.0, (2, 2), "g"),
    "land_formwork":       ("land_formwork", None, 0.85, 0.0, (2.4, 2.4), ""),
    "land_roof":           ("land_roof", None, 0.9, 0.0, (8, 8), "t"),
    "land_condo_wall":     ("land_condo_storey", None, 0.85, 0.0, (6, 3.1), "g"),
    "land_condo_band":     ("land_concrete_paint", (0.97, 0.94, 0.86), 0.85, 0.0, (4, 4), "g"),
    "land_condo_accent":   ("land_concrete_paint", (0.30, 0.42, 0.27), 0.85, 0.0, (4, 4), "g"),
    "land_netting":        ("land_netting", None, 0.9, 0.0, (4, 3.1), ""),
    "land_crane":          ("land_crane", None, 0.55, 0.1, None, "a"),
    "land_crane_solid":    ("prop_paint", (0.72, 0.48, 0.07), 0.55, 0.1, (2, 2), ""),
    "land_crane_weight":   ("land_concrete_raw", (0.85, 0.84, 0.82), 0.9, 0.0, (2, 2), ""),
    "land_hoard_green":    ("prop_paint", (0.035, 0.12, 0.065), 0.6, 0.1, (2, 2), "g"),
    "land_shop":           ("land_shop_bay", None, 0.6, 0.0, (4.6, 4.3), "g"),
    "land_deco_render":    ("land_concrete_paint", (0.98, 0.95, 0.87), 0.85, 0.0, (4, 4), "g"),
    "land_deco_sage":      ("land_concrete_paint", (0.36, 0.46, 0.30), 0.85, 0.0, (4, 4), "g"),
    "land_deco_rose":      ("land_concrete_paint", (0.55, 0.32, 0.28), 0.85, 0.0, (4, 4), "g"),
    "land_deco_ribbon":    ("land_deco_ribbon", None, 0.35, 0.0, (4, 1.6), ""),
    "land_sixties_fin":    ("land_concrete_paint", (0.96, 0.95, 0.91), 0.85, 0.0, (4, 4), "g"),
    "land_sixties_wall":   ("land_sixties_cell", None, 0.7, 0.0, (1.6, 3.4), "g"),
    "land_sixties_end":    ("land_concrete_paint", (0.78, 0.62, 0.30), 0.85, 0.0, (4, 4), "g"),
    "land_rebar":          (None, (0.16, 0.07, 0.04), 0.7, 0.3, None, ""),
    "land_glass_dark":     (None, (0.05, 0.08, 0.075), 0.3, 0.0, None, ""),
    "land_door_bronze":    (None, (0.08, 0.05, 0.03), 0.5, 0.3, None, ""),
    "land_sand":           (None, (0.45, 0.36, 0.22), 0.95, 0.0, None, ""),
    "land_cement_bag":     (None, (0.86, 0.84, 0.78), 0.9, 0.0, None, ""),
    # One-off artwork on 0..1 UVs.
    "land_sign_hoarding":  ("land_sign_hoarding", None, 0.7, 0.0, None, ""),
    "land_sign_net":       ("land_sign_net", None, 0.8, 0.0, None, ""),
    "land_sign_safety":    ("land_sign_safety", None, 0.6, 0.0, None, ""),
    "land_site_office":    ("land_site_office", None, 0.6, 0.1, None, ""),
    "land_sign_amihan":    ("land_sign_amihan", None, 0.7, 0.0, None, ""),
    "land_sign_deco":      ("land_sign_deco", None, 0.6, 0.0, None, ""),
    "land_sign_sixties":   ("land_sign_sixties", None, 0.5, 0.2, None, ""),
}


def image(name, colour=True):
    img = bpy.data.images.load(str(TEXTURES / f"{name}.png"), check_existing=True)
    if not colour:
        img.colorspace_settings.name = "Non-Color"
    return img


def material(name):
    if name not in MATERIALS:
        return P.material(name)
    m = bpy.data.materials.get(name)
    if m:
        return m
    tex, tint, rough, metal, _tile, flags = MATERIALS[name]
    m = bpy.data.materials.new(name)
    if m.node_tree is None:
        m.use_nodes = True
    nodes, links = m.node_tree.nodes, m.node_tree.links
    bsdf = nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = rough
    bsdf.inputs["Metallic"].default_value = metal
    if tex is None:
        bsdf.inputs["Base Color"].default_value = (*tint, 1)
        m.diffuse_color = (*tint, 1)
        return m
    uv = nodes.new("ShaderNodeUVMap")
    uv.uv_map = "UVMap"
    albedo = nodes.new("ShaderNodeTexImage")
    albedo.image = image(tex)
    links.new(uv.outputs["UV"], albedo.inputs["Vector"])
    colour = albedo.outputs["Color"]
    if "t" in flags:
        colour = anti_tile(nodes, links, uv.outputs["UV"], albedo.image, colour)
    if tint is not None:
        mix = nodes.new("ShaderNodeMix")
        mix.data_type, mix.blend_type = "RGBA", "MULTIPLY"
        mix.inputs["Factor"].default_value = 1.0
        links.new(colour, mix.inputs[6])
        mix.inputs[7].default_value = (*tint, 1)
        colour = mix.outputs[2]
    if "g" in flags:
        for img_name, layer, ext in (("land_grime_drips", "UVGrime", "REPEAT"),
                                     ("land_grime_splash", "UVSplash", "EXTEND")):
            guv = nodes.new("ShaderNodeUVMap")
            guv.uv_map = layer
            gtex = nodes.new("ShaderNodeTexImage")
            gtex.image = image(img_name, colour=False)
            gtex.extension = ext
            links.new(guv.outputs["UV"], gtex.inputs["Vector"])
            mul = nodes.new("ShaderNodeMix")
            mul.data_type, mul.blend_type = "RGBA", "MULTIPLY"
            mul.inputs["Factor"].default_value = 1.0
            links.new(colour, mul.inputs[6])
            links.new(gtex.outputs["Color"], mul.inputs[7])
            colour = mul.outputs[2]
    links.new(colour, bsdf.inputs["Base Color"])
    if "a" in flags:
        links.new(albedo.outputs["Alpha"], bsdf.inputs["Alpha"])
        m.surface_render_method = "DITHERED"
        m.use_backface_culling = False
    avg = (0.72, 0.70, 0.66) if tint is None else tuple(0.85 * c for c in tint)
    if name == "land_crane":
        avg = (0.85, 0.70, 0.24)
    m.diffuse_color = (*avg, 1)
    return m


def tile_of(mat):
    if mat in MATERIALS and MATERIALS[mat][4]:
        return MATERIALS[mat][4]
    return (2.0, 2.0)


# ------------------------------------------------------------------ 2D helpers

def ccw(poly):
    a = sum(p[0] * q[1] - q[0] * p[1] for p, q in zip(poly, poly[1:] + poly[:1]))
    return poly if a > 0 else list(reversed(poly))


def offset(poly, d):
    """Grow a CCW polygon by `d` metres (negative shrinks), mitred at each vertex."""
    out = []
    n = len(poly)
    for i in range(n):
        p0, p1, p2 = Vector(poly[i - 1]), Vector(poly[i]), Vector(poly[(i + 1) % n])
        e0, e1 = (p1 - p0), (p2 - p1)
        n0 = Vector((e0.y, -e0.x)).normalized() if e0.length > 1e-6 else None
        n1 = Vector((e1.y, -e1.x)).normalized() if e1.length > 1e-6 else None
        n0 = n0 or n1
        n1 = n1 or n0
        nm = (n0 + n1).normalized()
        c = max(0.3, nm.dot(n1))
        q = p1 + nm * (d / c)
        out.append((q.x, q.y))
    return out


def rect(x0, x1, y0, y1, r=0.3, seg=3, corners=None):
    """A CCW rectangle with filleted corners; `corners` maps 'sw','se','ne','nw' to radius/segments."""
    pts = {"sw": (x0, y0), "se": (x1, y0), "ne": (x1, y1), "nw": (x0, y1)}
    out = []
    for key in ("sw", "se", "ne", "nw"):
        rr, sg = (corners or {}).get(key, (r, seg))
        out.append((key, pts[key], rr, sg))
    poly = []
    n = len(out)
    for i, (key, p, rr, sg) in enumerate(out):
        prev_p, next_p = Vector(out[i - 1][1]), Vector(out[(i + 1) % n][1])
        pv = Vector(p)
        a, b = (prev_p - pv), (next_p - pv)
        t = min(rr, 0.49 * a.length, 0.49 * b.length)
        s, e = pv + a.normalized() * t, pv + b.normalized() * t
        # A true circular arc for a round corner (the deco corner reads as a drum, not a bulge).
        c = s + (b.normalized() * t)
        a0 = math.atan2(s.y - c.y, s.x - c.x)
        a1 = math.atan2(e.y - c.y, e.x - c.x)
        while a1 - a0 > math.pi:
            a1 -= math.tau
        while a0 - a1 > math.pi:
            a1 += math.tau
        for k in range(sg + 1):
            ang = a0 + (a1 - a0) * k / sg
            poly.append((c.x + t * math.cos(ang), c.y + t * math.sin(ang)))
    return ccw(poly)


def perimeter(poly):
    return sum((Vector(q) - Vector(p)).length for p, q in zip(poly, poly[1:] + poly[:1]))


def along(poly, s):
    """The point at arc length `s` round a CCW polygon, with the outward normal there."""
    s %= perimeter(poly)
    for p, q in zip(poly, poly[1:] + poly[:1]):
        p, q = Vector(p), Vector(q)
        L = (q - p).length
        if s <= L and L > 1e-6:
            e = (q - p) / L
            return p + e * s, Vector((e.y, -e.x)), e
        s -= L
    p, q = Vector(poly[0]), Vector(poly[1])
    e = (q - p).normalized()
    return p, Vector((e.y, -e.x)), e


# ------------------------------------------------------------------ the buffer

class LBuf(PBuf):
    """The prop kit's PBuf for buildings: per-material metre tiles, storey-aligned facades,
    arc-length rings, painted-lattice members, and this kit's grime mapping (drips from the
    nearest level above, splash from the ground)."""

    def __init__(self, name, levels=(), ground=LOT):
        super().__init__(name)
        self.levels = sorted(levels)
        self.ground = ground
        self.vbase = {}

    def base(self, mat, z):
        self.vbase[mat] = z

    # -- rings and prisms with arc-length UVs ---------------------------------------------------

    def ring(self, poly, z0, z1, mat, inward=False, vbase=None):
        tu, tv = tile_of(mat)
        vb = self.vbase.get(mat, 0.0) if vbase is None else vbase
        pts = [Vector((x, y, 0)) for x, y in poly]
        n = len(pts)
        bot = [self.bm.verts.new(self.M @ Vector((p.x, p.y, z0))) for p in pts]
        top = [self.bm.verts.new(self.M @ Vector((p.x, p.y, z1))) for p in pts]
        s = [0.0]
        for i in range(n):
            s.append(s[-1] + (pts[(i + 1) % n] - pts[i]).length)
        idx = self.mi(mat)
        faces = []
        for i in range(n):
            j = (i + 1) % n
            quad = (bot[i], bot[j], top[j], top[i])
            if inward:
                quad = tuple(reversed(quad))
            f = self.bm.faces.new(quad)
            f.material_index = idx
            uv = {bot[i]: (s[i] / tu, (z0 - vb) / tv), bot[j]: (s[i + 1] / tu, (z0 - vb) / tv),
                  top[j]: (s[i + 1] / tu, (z1 - vb) / tv), top[i]: (s[i] / tu, (z1 - vb) / tv)}
            self.set_uvs(f, uv)
            faces.append(f)
        return faces, bot, top

    def solid(self, poly, z0, z1, mat, cap_mat=None, top=True, bottom=True, vbase=None):
        faces, bot, tp = self.ring(poly, z0, z1, mat, vbase=vbase)
        cm = self.mi(cap_mat or mat)
        if top:
            f = self.bm.faces.new(tp)
            f.material_index = cm
        if bottom:
            f = self.bm.faces.new(list(reversed(bot)))
            f.material_index = cm
        return faces

    def annulus(self, outer, inner, z0, z1, mat, cap_mat=None, vbase=None):
        """A closed band between two polygons of the same vertex count (inner = offset(outer))."""
        _, ob, ot = self.ring(outer, z0, z1, mat, vbase=vbase)
        _, ib, it = self.ring(inner, z0, z1, mat, inward=True, vbase=vbase)
        n = len(outer)
        cm = self.mi(cap_mat or mat)
        for i in range(n):
            j = (i + 1) % n
            f = self.bm.faces.new((ot[i], ot[j], it[j], it[i]))
            f.material_index = cm
            f = self.bm.faces.new((ob[j], ob[i], ib[i], ib[j]))
            f.material_index = cm

    def plate(self, p0, p1, width, thick, mat, up_axis=None):
        """A flat rounded bar from p0 to p1 lying in the plane of `up_axis` (fins, shelves)."""
        p0, p1 = Vector(p0), Vector(p1)
        d = (p1 - p0)
        L = d.length
        c = (p0 + p1) / 2
        x = d.normalized()
        z = (up_axis or UP).normalized()
        y = z.cross(x).normalized()
        z = x.cross(y)
        rot = Matrix((x, y, z)).transposed().to_4x4()
        return self.rbox(c, (L, width, thick), mat, r=min(0.06, thick * 0.4), rot=rot)

    def lattice(self, p0, p1, prof, mat, cap_mat, panel_w=None):
        """A painted-lattice member (crane mast, jib): a closed profile swept p0 -> p1, each side
        face mapped so one 2 x 2 m texture panel spans the face's width (square panels along)."""
        p0, p1 = Vector(p0), Vector(p1)
        d = (p1 - p0)
        L = d.length
        d.normalize()
        side = d.cross(UP)
        if side.length < 1e-4:
            side = Vector((1, 0, 0))
        side.normalize()
        up = side.cross(d).normalized()
        rings = [[self.bm.verts.new(self.M @ (p + side * a + up * b)) for a, b in prof] for p in (p0, p1)]
        k = len(prof)
        idx = self.mi(mat)
        faces = []
        for j in range(k):
            j2 = (j + 1) % k
            w = (Vector(prof[j2]) - Vector(prof[j])).length
            f = self.bm.faces.new((rings[0][j], rings[0][j2], rings[1][j2], rings[1][j]))
            f.material_index = idx
            u = L / (panel_w or w)
            self.set_uvs(f, {rings[0][j]: (0, 0), rings[0][j2]: (0, 1), rings[1][j2]: (u, 1), rings[1][j]: (u, 0)})
            faces.append(f)
        cm = self.mi(cap_mat)
        for r in (rings[0], list(reversed(rings[1]))):
            f = self.bm.faces.new(r)
            f.material_index = cm
            faces.append(f)
        bmesh.ops.recalc_face_normals(self.bm, faces=faces)
        return faces

    # -- UVs and output ----------------------------------------------------------------------------

    def drip_level(self, z):
        for lv in self.levels:
            if lv > z + 0.02:
                return lv
        return None

    def world_uvs(self):
        uv = self.bm.loops.layers.uv.verify()
        mats = self.mats
        for f in self.bm.faces:
            if f[self.flag]:
                continue
            tu, tv = tile_of(mats[f.material_index])
            vb = self.vbase.get(mats[f.material_index], 0.0)
            n = f.normal
            if abs(n.z) > 0.7:
                for l in f.loops:
                    l[uv].uv = (l.vert.co.x / tu, l.vert.co.y / tu)
                continue
            t = UP.cross(n)
            t = t.normalized() if t.length > 1e-6 else Vector((1, 0, 0))
            for l in f.loops:
                l[uv].uv = (l.vert.co.dot(t) / tu, (l.vert.co.z - vb) / tv)
        drip = self.bm.loops.layers.uv.new("UVGrime")
        splash = self.bm.loops.layers.uv.new("UVSplash")
        clean = 0.999
        for f in self.bm.faces:
            n = f.normal
            side = abs(n.z) <= 0.7
            t = UP.cross(n)
            t = t.normalized() if t.length > 1e-6 else Vector((1, 0, 0))
            top = self.drip_level(f.calc_center_median().z)
            for l in f.loops:
                co = l.vert.co
                if side and top is not None:
                    l[drip].uv = (co.dot(t) / 2.0, min(clean, max(0.0, (top - co.z) / GRIME_H)))
                else:
                    l[drip].uv = (co.x / 2.0, clean)
                if side:
                    l[splash].uv = (co.dot(t) / 2.0, min(clean, max(0.0, (co.z - self.ground) / SPLASH_H)))
                else:
                    l[splash].uv = (co.x / 2.0, clean)

    def finish(self, collection, origin=(0, 0, 0), bevel=0.03, segments=2, smooth=False):
        self.world_uvs()
        o = Vector(origin)
        for v in self.bm.verts:
            v.co -= o
        mesh = bpy.data.meshes.new(self.name)
        self.bm.to_mesh(mesh)
        self.bm.free()
        for m in self.mats:
            mesh.materials.append(material(m))
        for p in mesh.polygons:
            p.use_smooth = smooth and p.area < 0.05
        obj = bpy.data.objects.new(self.name, mesh)
        obj.location = o
        collection.objects.link(obj)
        if bevel:
            mod = obj.modifiers.new("Bevel", "BEVEL")
            mod.width, mod.segments = bevel, segments
            mod.limit_method, mod.angle_limit = "ANGLE", math.radians(35)
            mod.harden_normals, mod.use_clamp_overlap = True, True
        return obj


def box(b, x0, x1, y0, y1, z0, z1, mat, r=0.04):
    b.rbox(((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2), (x1 - x0, y1 - y0, z1 - z0), mat, r=r)


# ================================================================== 1. TANAW RESIDENCES

C_TOWER = rect(23.8, 38.0, 48.4, 67.4, r=1.2, seg=3)
C_LOBBY = 4.6
C_STOREY = 3.1
C_FLOORS = 18
C_FINISHED = 12          # floors 1..12 finished
C_BLOCK = (13, 15)       # hollow-block infill on these floors
C_NET = (13, 16)         # netting wraps these floors
CRANE_AT = Vector((31.0, 43.3))
MAST_TOP = 66.5
JIB_DIR = Vector((-0.55, 0.835)).normalized()


def cz(k):
    """The top of the slab of floor k (k = 1 is the first floor over the lobby)."""
    return LOT + C_LOBBY + (k - 1) * C_STOREY


C_TOP = cz(C_FLOORS + 1)     # the top slab, 60.64


def condo_tower(col):
    o = ANCHORS["condo"]
    levels = [cz(k) - 0.12 for k in range(1, C_FLOORS + 2)]
    b = LBuf("land_condo_tower", levels=levels)
    inner = offset(C_TOWER, -0.35)
    # The lobby: a recessed shop and lobby wall behind corner piers, under the first slab.
    b.base("land_shop", LOT + 0.1)
    b.solid(offset(C_TOWER, -0.7), LOT - 0.05, cz(1) - 0.05, "land_shop", cap_mat="land_slab_raw")
    per = perimeter(C_TOWER)
    for k in range(12):
        p, nrm, e = along(C_TOWER, per * (k + 0.5) / 12)
        q = p - nrm * 0.45
        b.rbox((q.x, q.y, (LOT + cz(1)) / 2 - 0.05), (0.7, 0.7, cz(1) - LOT + 0.1), "land_condo_band", r=0.1,
               rot=Matrix.Rotation(math.atan2(e.y, e.x), 4, "Z"))
    # Finished floors: one wall prism drawn storey by storey, slab bands proud of it.
    b.base("land_condo_wall", cz(1) + 0.12)
    b.solid(offset(C_TOWER, -0.02), cz(1) - 0.1, cz(C_FINISHED + 1) - 0.08, "land_condo_wall",
            cap_mat="land_slab_raw", bottom=False)
    for k in range(1, C_FINISHED + 2):
        b.annulus(offset(C_TOWER, 0.25), inner, cz(k) - 0.12, cz(k) + 0.2, "land_condo_band")
    # Sage accent fins up the two long faces and the south face, full finished height.
    for x in (27.6, 34.2):
        for y, s in ((48.4, -1), (67.4, 1)):
            box(b, x - 0.4, x + 0.4, y - (0.7 if s < 0 else 0.1), y + (0.1 if s < 0 else 0.7),
                cz(1) - 0.1, cz(C_FINISHED + 1) + 0.45, "land_condo_accent", r=0.12)
    for y in (54.0, 61.8):
        for x, s in ((23.8, -1), (38.0, 1)):
            box(b, x - (0.7 if s < 0 else 0.1), x + (0.1 if s < 0 else 0.7), y - 0.4, y + 0.4,
                cz(1) - 0.1, cz(C_FINISHED + 1) + 0.45, "land_condo_accent", r=0.12)
    # Construction floors: bare slabs, then columns round the edge.
    for k in range(C_FINISHED + 2, C_FLOORS + 2):
        b.solid(offset(C_TOWER, 0.08 if k < C_FLOORS + 1 else 0.02), cz(k) - 0.12, cz(k) + 0.18, "land_slab_raw")
    cols = []
    ring = offset(C_TOWER, -0.45)
    per = perimeter(ring)
    for k in range(14):
        p, nrm, e = along(ring, per * (k + 0.25) / 14)
        cols.append(p)
    rng = random.Random(4)
    for k in range(C_FINISHED + 1, C_FLOORS + 2):
        z0 = cz(k) + 0.1
        z1 = cz(k + 1) - 0.08 if k <= C_FLOORS else cz(k) + 1.6
        for i, p in enumerate(cols):
            if k == C_FLOORS + 1 and i % 3 == 1:
                continue          # not every stub has been poured
            b.rbox((p.x, p.y, (z0 + z1) / 2), (0.62, 0.62, z1 - z0), "land_concrete_raw", r=0.08)
            if k == C_FLOORS + 1:
                # The rebar cage sticking out of each stub: one chunky rust block, bars painted.
                b.rbox((p.x, p.y, z1 + 0.35), (0.46, 0.46, 0.75), "land_rebar", r=0.06,
                       top_scale=0.8 + 0.1 * rng.random())
        # Core walls (lift and stairs) rise with the frame.
        if k <= C_FLOORS:
            box(b, 29.2, 32.8, 55.6, 60.4, cz(k) + 0.1, cz(k + 1) - 0.06, "land_concrete_raw", r=0.06)
    # Hollow-block infill on the lower construction floors (the netting hides most of it).
    for k in range(C_BLOCK[0], C_BLOCK[1] + 1):
        b.solid(offset(C_TOWER, -0.3), cz(k) + 0.15, cz(k + 1) - 0.1, "land_blockwork", top=False, bottom=False)
        b.solid(offset(C_TOWER, -0.5), cz(k) + 0.15, cz(k + 1) - 0.1, "land_blockwork", top=False, bottom=False)
    # Plywood edge forms round the top slab, ready for the next pour.
    b.annulus(offset(C_TOWER, 0.16), offset(C_TOWER, 0.04), C_TOP - 0.2, C_TOP + 0.42, "land_formwork")
    # Materials on the top slab: pallets of blocks, a stack of forms, a concrete bucket.
    for (x, y) in ((26.0, 51.0), (27.2, 51.0), (35.5, 64.2)):
        box(b, x - 0.55, x + 0.55, y - 0.55, y + 0.55, C_TOP + 0.15, C_TOP + 0.3, "prop_wood", r=0.03)
        box(b, x - 0.5, x + 0.5, y - 0.5, y + 0.5, C_TOP + 0.28, C_TOP + 1.05, "land_blockwork", r=0.05)
    box(b, 33.0, 35.4, 50.4, 51.6, C_TOP + 0.15, C_TOP + 0.75, "land_formwork", r=0.03)
    b.lathe(Vector((28.0, 64.0, C_TOP + 0.15)), [(0.25, 0.0), (0.7, 1.1), (0.72, 1.3), (0.0, 1.3)], "land_crane_solid",
            sides=12)
    t = b.finish(col, origin=o, bevel=0.04)
    # The roof of the frame is open sky: no roof material. The finished part's top is slab.
    return t


def condo_netting(col):
    o = ANCHORS["condo"]
    b = LBuf("land_condo_netting")
    b.base("land_netting", cz(C_NET[0]))
    net = offset(C_TOWER, 0.55)
    z0, z1 = cz(C_NET[0]) - 0.3, cz(C_NET[1] + 1) + 0.25
    b.ring(net, z0, z1, "land_netting")
    b.ring(offset(net, -0.03), z0, z1, "land_netting", inward=True)
    # The catch fan at floor 13: a sloped plywood deck on steel brackets.
    fan_in, fan_out = offset(C_TOWER, 0.2), offset(C_TOWER, 1.6)
    zi, zo = cz(C_NET[0]) - 0.35, cz(C_NET[0]) + 0.35
    _, ob, ot = b.ring(fan_out, zo - 0.06, zo, "land_formwork")
    _, ib, it = b.ring(fan_in, zi - 0.06, zi, "land_formwork", inward=True)
    n = len(fan_in)
    for i in range(n):
        j = (i + 1) % n
        b.bm.faces.new((ot[i], ot[j], it[j], it[i])).material_index = b.mi("land_formwork")
        b.bm.faces.new((ob[j], ob[i], ib[i], ib[j])).material_index = b.mi("land_formwork")
    # The DALISAY LAND / TANAW tarp on the west face of the netting (toward Taft).
    b.panel(facing("-x", Vector((23.8 - 0.62, 62.4, cz(14) + 1.6))), 8.0, 4.0, 0.04, "prop_plastic_white",
            "land_sign_net", r=0.05)
    b.finish(col, origin=o, bevel=0.02)


def condo_podium(col):
    o = ANCHORS["condo"]
    poly = rect(26.2, 38.0, 67.2, 82.6, r=0.5)
    b = LBuf("land_condo_podium", levels=[LOT + 4.2, LOT + 8.0, LOT + 9.0])
    b.base("land_shop", LOT + 0.1)
    b.solid(offset(poly, -0.5), LOT - 0.05, LOT + 4.3, "land_shop", top=False)
    b.base("land_condo_wall", LOT + 4.45)
    b.solid(offset(poly, -0.05), LOT + 4.1, LOT + 8.0, "land_condo_wall", top=False, bottom=False)
    b.annulus(offset(poly, 0.3), offset(poly, -0.5), LOT + 4.0, LOT + 4.45, "land_condo_band")
    b.annulus(offset(poly, 0.12), offset(poly, -0.3), LOT + 7.9, LOT + 9.0, "land_condo_band")
    b.solid(offset(poly, -0.28), LOT + 8.2, LOT + 8.5, "land_roof")
    b.finish(col, origin=o, bevel=0.04)


def condo_hoist(col):
    """The construction hoist on the west face: a painted-lattice mast tied back at every third
    floor, and its cage parked at floor 9."""
    o = ANCHORS["condo"]
    b = LBuf("land_condo_hoist")
    x, y = 23.8 - 1.35, 53.2
    sq = [(-0.45, -0.45), (0.45, -0.45), (0.45, 0.45), (-0.45, 0.45)]
    b.lattice(Vector((x, y, LOT - 0.05)), Vector((x, y, cz(C_FLOORS) + 1.2)), sq, "land_crane", "land_crane_solid")
    for k in range(2, C_FLOORS, 3):
        box(b, x + 0.3, 23.8 + 0.2, y - 0.12, y + 0.12, cz(k) + 0.8, cz(k) + 1.0, "land_crane_solid", r=0.04)
    zc = cz(9)
    box(b, x - 2.1, x - 0.44, y - 1.3, y + 1.3, zc + 0.05, zc + 2.55, "prop_steel_jade", r=0.08)
    box(b, x - 2.16, x - 0.4, y - 1.36, y + 1.36, zc + 2.5, zc + 2.72, "land_crane_solid", r=0.05)
    box(b, x - 1.2, x + 1.2, y - 1.2, y + 1.2, LOT - 0.05, LOT + 0.35, "land_crane_weight", r=0.05)
    b.finish(col, origin=o, bevel=0.02)


def crane(col):
    o = ANCHORS["condo"]
    b = LBuf("land_crane")
    x, y = CRANE_AT
    # The footing, sunk into the lot, and the mast.
    box(b, x - 2.2, x + 2.2, y - 2.2, y + 2.2, LOT - 0.1, LOT + 0.55, "land_crane_weight", r=0.08)
    sq = [(-1.0, -1.0), (1.0, -1.0), (1.0, 1.0), (-1.0, 1.0)]
    b.lattice(Vector((x, y, LOT + 0.5)), Vector((x, y, MAST_TOP)), sq, "land_crane", "land_crane_solid")
    # The tie frame to the tower at floor 12: two chunky yellow beams to the slab edge.
    zt = cz(12) + 0.6
    for dx in (-0.8, 0.8):
        b.prism((x + dx, y + 0.9, zt), (x + dx * 1.8, 48.4 + 0.1, zt + 0.1), 0.16, 0.16, "land_crane_solid", r=0.05)
    box(b, x - 1.15, x + 1.15, y - 1.15, y + 1.15, zt - 0.3, zt + 0.35, "land_crane_solid", r=0.05)
    # Slewing unit, turntable and the cab.
    z0 = MAST_TOP
    b.lathe(Vector((x, y, z0 - 0.05)), [(1.35, 0.0), (1.35, 0.5), (1.2, 0.62), (0.0, 0.62)], "land_crane_solid",
            sides=16)
    side = Vector((JIB_DIR.y, -JIB_DIR.x, 0))
    jd = Vector((JIB_DIR.x, JIB_DIR.y, 0))
    rot = Matrix.Rotation(math.atan2(jd.y, jd.x), 4, "Z")
    b.rbox(Vector((x, y, z0 + 1.1)), (3.2, 2.4, 1.0), "land_crane_solid", r=0.12, rot=rot)
    cab = Vector((x, y, z0)) + jd * 0.6 + side * 1.9 + Vector((0, 0, 1.3))
    b.rbox(cab, (2.0, 1.6, 2.1), "prop_plastic_cream", r=0.2, rot=rot)
    b.rbox(cab + jd * 0.95 + Vector((0, 0, 0.2)), (0.2, 1.3, 1.1), "land_glass_dark", r=0.06, rot=rot)
    # The jib (42 m) and counter-jib (13 m): triangular and box sections, lattice painted.
    zj = z0 + 1.5
    root = Vector((x, y, zj))
    tri = [(-0.8, -0.7), (0.8, -0.7), (0.0, 0.8)]
    b.lattice(root + jd * 1.2, root + jd * 42.0, tri, "land_crane", "land_crane_solid", panel_w=1.7)
    boxp = [(-0.9, -0.6), (0.9, -0.6), (0.9, 0.6), (-0.9, 0.6)]
    b.lattice(root - jd * 1.2, root - jd * 13.0, boxp, "land_crane", "land_crane_solid", panel_w=1.8)
    # Chunky solid ends, the trolley on the jib, the counterweights and a walkway rail block.
    b.rbox(root + jd * 42.2, (0.6, 1.8, 1.6), "land_crane_solid", r=0.08, rot=rot)
    tro = root + jd * 26.0 + Vector((0, 0, -0.95))
    b.rbox(tro, (1.4, 1.5, 0.7), "prop_steel_dark", r=0.08, rot=rot)
    for k in range(4):
        c = root - jd * (9.0 + k * 1.05) + Vector((0, 0, -0.5))
        b.rbox(c, (0.95, 2.3, 2.2), "land_crane_weight", r=0.1, rot=rot)
    b.finish(col, origin=o, bevel=0.03)


def condo_yard(col):
    o = ANCHORS["condo"]
    b = LBuf("land_condo_yard", levels=[LOT + 2.62])
    h = LOT + 2.6
    # The hoarding along Padre Faura: a timber frame, posts behind, and the artwork panel.
    y = 38.85
    box(b, 22.95, 39.4, y - 0.02, y + 0.12, LOT - 0.05, h, "land_hoard_green", r=0.03)
    b.panel(facing("-y", Vector((31.1, y - 0.04, LOT + 1.32))), 16.0, 2.4, 0.04, "land_hoard_green",
            "land_sign_hoarding", r=0.02)
    for k in range(8):
        px = 23.3 + k * 2.3
        box(b, px - 0.1, px + 0.1, y + 0.1, y + 0.3, LOT - 0.05, h - 0.05, "prop_wood_dark", r=0.03)
        b.prism((px, y + 0.25, LOT + 1.8), (px, y + 1.3, LOT - 0.02), 0.07, 0.07, "prop_wood_dark", r=0.02)
    # The returns north to the tower, with the gate and its sign on the east side.
    for x in (22.95, 39.4):
        y0, y1 = y + 0.05, 48.2
        if x > 30:
            box(b, x - 0.12, x + 0.02, y0, 41.4, LOT - 0.05, h, "land_hoard_green", r=0.03)
            box(b, x - 0.12, x + 0.02, 45.4, y1, LOT - 0.05, h, "land_hoard_green", r=0.03)
            box(b, x - 0.09, x + 0.01, 41.45, 45.35, LOT + 0.05, h - 0.1, "prop_steel_jade", r=0.03)
            for gy in (41.4, 45.4):
                box(b, x - 0.2, x + 0.1, gy - 0.12, gy + 0.12, LOT - 0.05, h + 0.2, "prop_steel_dark", r=0.04)
            b.panel(facing("+x", Vector((x + 0.05, 43.4, LOT + 1.55))), 1.2, 0.9, 0.02, "prop_plastic_white",
                    "land_sign_safety", r=0.02)
        else:
            box(b, x - 0.02, x + 0.12, y0, y1, LOT - 0.05, h, "land_hoard_green", r=0.03)
    # The site office container on its sleepers.
    ox0, ox1, oy0, oy1 = 33.3, 39.1, 45.2, 47.7
    for sx in (33.8, 38.6):
        box(b, sx - 0.2, sx + 0.2, oy0 + 0.1, oy1 - 0.1, LOT - 0.04, LOT + 0.2, "prop_wood_dark", r=0.03)
    box(b, ox0, ox1, oy0, oy1, LOT + 0.18, LOT + 2.78, "land_hoard_green", r=0.08)
    b.panel(facing("-y", Vector(((ox0 + ox1) / 2, oy0 - 0.02, LOT + 1.48))), 5.8, 2.56, 0.02, "land_hoard_green",
            "land_site_office", r=0.02)
    box(b, ox0 - 0.08, ox1 + 0.08, oy0 - 0.08, oy1 + 0.08, LOT + 2.74, LOT + 2.9, "prop_steel_dark", r=0.04)
    # Stacks: formwork sheets, cement pallets under a tarp, a sand heap, a pallet of blocks.
    box(b, 24.0, 26.6, 40.2, 41.5, LOT - 0.03, LOT + 0.12, "prop_wood", r=0.02)
    box(b, 24.1, 26.5, 40.3, 41.4, LOT + 0.1, LOT + 0.75, "land_formwork", r=0.03)
    for k, (cx, cy) in enumerate(((27.8, 40.3), (27.8, 41.6))):
        box(b, cx - 0.6, cx + 0.6, cy - 0.55, cy + 0.55, LOT - 0.03, LOT + 0.12, "prop_wood", r=0.02)
        b.rbox(Vector((cx, cy, LOT + 0.45)), (1.12, 1.02, 0.66), "land_cement_bag", r=0.15)
    b.rbox(Vector((27.8, 40.95, LOT + 0.82)), (1.3, 2.5, 0.12), "prop_tarp_olive", r=0.05)
    b.blob(Vector((35.4, 41.5, LOT - 0.3)), (1.9, 1.5, 1.35), "land_sand")
    box(b, 25.0, 26.2, 44.3, 45.5, LOT - 0.03, LOT + 0.12, "prop_wood", r=0.02)
    box(b, 25.05, 26.15, 44.35, 45.45, LOT + 0.1, LOT + 0.95, "land_blockwork", r=0.05)
    # The yard floor: a thin gravelled apron over the lot, sunk at its edges.
    box(b, 23.1, 39.25, 39.05, 48.3, LOT - 0.1, LOT + 0.02, "prop_concrete", r=0.05)
    b.finish(col, origin=o, bevel=0.02)


def condo(parent):
    col = collection("land_condo (TANAW RESIDENCES)", parent)
    condo_tower(col)
    condo_netting(col)
    condo_podium(col)
    condo_hoist(col)
    crane(col)
    condo_yard(col)
    return col


# ================================================================== 2. EDIFICIO AMIHAN

D_POLY = rect(11.0, 27.0, -152.9, -134.9, r=0.3, seg=2, corners={"nw": (6.0, 14)})
D_G = LOT + 4.4          # first floor
D_STOREY = 3.4
D_ROOF = D_G + 3 * D_STOREY   # 14.84
D_PARAPET = D_ROOF + 0.96


def d_floor(k):
    return D_G + (k - 1) * D_STOREY


def deco_body(col):
    o = ANCHORS["deco"]
    levels = [d_floor(k) + 2.6 for k in (1, 2, 3)] + [D_PARAPET + 0.2, D_G - 0.1]
    b = LBuf("land_deco_body", levels=levels)
    # Ground floor: shop bays set back 0.45 behind the piers and under the eyebrow.
    b.base("land_shop", LOT + 0.1)
    b.solid(offset(D_POLY, -0.45), LOT - 0.05, D_G + 0.1, "land_shop", cap_mat="land_deco_render")
    per = perimeter(D_POLY)
    n = int(per / 3.6)
    for k in range(n):
        p, nrm, e = along(D_POLY, per * k / n + 0.4)
        q = p - nrm * 0.3
        b.rbox((q.x, q.y, (LOT + D_G) / 2), (0.75, 0.7, D_G - LOT + 0.1), "land_deco_render", r=0.12,
               rot=Matrix.Rotation(math.atan2(e.y, e.x), 4, "Z"))
    # The eyebrow canopy wrapping the shop floor, and its sage lip.
    b.annulus(offset(D_POLY, 1.25), offset(D_POLY, -0.2), D_G - 0.32, D_G - 0.12, "land_deco_render")
    b.annulus(offset(D_POLY, 1.3), offset(D_POLY, 1.05), D_G - 0.36, D_G - 0.06, "land_deco_sage")
    # Upper floors: the recessed ribbon windows and the deep spandrel bands between them.
    for k in (1, 2, 3):
        z = d_floor(k)
        b.solid(offset(D_POLY, -0.28), z + 0.9, z + 2.7, "land_deco_ribbon", top=False, bottom=False, vbase=z + 1.0)
    bands = [(D_G - 0.15, d_floor(1) + 1.0)] + [(d_floor(k) + 2.6, d_floor(k + 1) + 1.0) for k in (1, 2)] + \
            [(d_floor(3) + 2.6, D_PARAPET)]
    for z0, z1 in bands:
        b.annulus(D_POLY, offset(D_POLY, -0.4), z0, z1, "land_deco_render")
        # Three sage speed lines round each band (none on the parapet's top edge).
        for i in range(3):
            zz = z0 + 0.28 + i * 0.22
            if zz + 0.1 < z1 - 0.1:
                b.annulus(offset(D_POLY, 0.09), offset(D_POLY, -0.05), zz, zz + 0.1, "land_deco_sage")
    # The roof inside the parapet, and a coping on the parapet.
    b.solid(offset(D_POLY, -0.35), D_ROOF - 0.3, D_ROOF, "land_roof")
    b.annulus(offset(D_POLY, 0.06), offset(D_POLY, -0.46), D_PARAPET - 0.02, D_PARAPET + 0.14, "land_deco_sage")
    b.finish(col, origin=o, bevel=0.035)


def deco_corner(col):
    """The corner's crown steps and the AMIHAN fin on the apex."""
    o = ANCHORS["deco"]
    b = LBuf("land_deco_corner", levels=[D_PARAPET + 2.2, 24.3])
    # The round corner's centre and apex (the rect() arc for 'nw' has radius 6).
    c = Vector((11.0 + 6.0, -134.9 - 6.0))
    apex_dir = Vector((-1, 1)).normalized()
    # Stepped crown over the arc: two tiers of the drum, each set back.
    for i, (dz0, dz1, inset) in enumerate(((D_PARAPET - 0.1, D_PARAPET + 1.0, 0.25),
                                          (D_PARAPET + 0.9, D_PARAPET + 1.9, 0.9))):
        r0 = 6.0 - inset
        arc = []
        for k in range(15):
            a = math.radians(90 + 90 * k / 14)
            arc.append((c.x + r0 * math.cos(a), c.y + r0 * math.sin(a)))
        arc_in = [(c.x + (r0 - 1.3) * math.cos(math.radians(90 + 90 * k / 14)),
                   c.y + (r0 - 1.3) * math.sin(math.radians(90 + 90 * k / 14))) for k in range(14, -1, -1)]
        poly = ccw(arc + arc_in)
        b.solid(poly, dz0, dz1, "land_deco_render", cap_mat="land_deco_sage")
    # The fin: a blade on the corner apex, square to Taft so its faces look up and down the
    # avenue (toward the court), 0.5 thick and 2.1 wide, from the eyebrow to 24 m.
    apex = c + apex_dir * 6.0
    fin_c = apex + apex_dir * 0.45
    z0, z1 = D_G + 0.3, 24.0
    b.rbox(Vector((fin_c.x, fin_c.y, (z0 + z1) / 2)), (2.1, 0.5, z1 - z0), "land_deco_rose", r=0.14)
    for i, (dz, w, d) in enumerate(((0.0, 1.7, 0.42), (0.55, 1.2, 0.34), (1.0, 0.7, 0.26))):
        b.rbox(Vector((fin_c.x, fin_c.y, z1 + dz + 0.25)), (w, d, 0.55),
               "land_deco_sage" if i % 2 == 0 else "land_deco_rose", r=0.1)
    # AMIHAN down both faces, on panels proud of the fin, above the parapet so it clears the
    # neighbours in the long view down Taft.
    for s in (1, -1):
        normal = Vector((0, s, 0))
        at = Vector((fin_c.x, fin_c.y + s * 0.27, 19.7))
        mtx = P.frame(UP.cross(normal), UP, normal, at)
        b.panel(mtx, 1.2, 7.0, 0.04, "land_deco_rose", "land_sign_amihan", r=0.06)
    b.finish(col, origin=o, bevel=0.04)


def deco_details(col):
    o = ANCHORS["deco"]
    b = LBuf("land_deco_details", levels=[D_G - 0.2])
    # The entrance on Taft: a recessed bronze door pair in a sage frame, the plaque over the eyebrow.
    y = -141.8
    box(b, 11.0 - 0.1, 11.0 + 0.3, y - 1.5, y + 1.5, LOT - 0.03, LOT + 3.3, "land_deco_sage", r=0.1)
    b.panel(facing("-x", Vector((11.0 - 0.13, y, LOT + 1.5))), 2.2, 2.8, 0.05, "land_deco_sage", "land_door_bronze",
            r=0.04)
    box(b, 10.3, 11.1, y - 1.7, y + 1.7, PAVE - 0.04, LOT + 0.08, "land_deco_render", r=0.03)
    b.panel(facing("-x", Vector((11.0 - 0.14, y, D_G + 0.55))), 3.6, 0.6, 0.06, "land_deco_render", "land_sign_deco",
            r=0.03)
    # The roof: a rounded stair house and a water tank on a stand.
    sh = rect(21.0, 25.2, -151.4, -147.6, r=0.3, corners={"nw": (1.9, 6), "sw": (1.9, 6)})
    b.solid(sh, D_ROOF - 0.1, D_ROOF + 2.8, "land_deco_render", cap_mat="land_deco_sage")
    b.annulus(offset(sh, 0.12), offset(sh, -0.1), D_ROOF + 2.7, D_ROOF + 3.0, "land_deco_sage")
    for dx in (-0.6, 0.6):
        for dy in (-0.6, 0.6):
            b.prism((23.2 + dx, -140.6 + dy, D_ROOF - 0.05), (23.2 + dx, -140.6 + dy, D_ROOF + 1.2), 0.1, 0.1,
                    "prop_steel_dark", r=0.03)
    b.lathe(Vector((23.2, -140.6, D_ROOF + 1.15)), [(0.95, 0.0), (1.0, 0.1), (1.0, 1.7), (0.7, 2.0), (0.0, 2.05)],
            "prop_plastic_cream", sides=16)
    b.finish(col, origin=o, bevel=0.03)


def deco(parent):
    col = collection("land_deco (EDIFICIO AMIHAN)", parent)
    deco_body(col)
    deco_corner(col)
    deco_details(col)
    return col


# ================================================================== 3. MAKABAYAN BUILDING

S_X0, S_X1 = 11.0, 35.2
S_YN, S_YLOT, S_YS = -109.8, -116.9, -119.2     # north face, lot line, cantilever face
S_G = LOT + 4.2
S_STOREY = 3.4
S_FLOORS = 5
S_ROOF = S_G + S_FLOORS * S_STOREY               # 21.44
S_DEPTH = 0.95


def s_floor(k):
    return S_G + (k - 1) * S_STOREY


def sixties_body(col):
    o = ANCHORS["sixties"]
    levels = [s_floor(k) + 0.1 for k in range(1, S_FLOORS + 2)]
    b = LBuf("land_sixties_body", levels=levels)
    # Ground floor: shops behind the lot line, glazed, under the cantilever.
    ground = rect(S_X0 + 0.3, S_X1 - 0.2, S_YLOT + 0.15, S_YN - 0.1, r=0.2)
    b.base("land_shop", LOT + 0.1)
    b.solid(ground, LOT - 0.05, S_G + 0.05, "land_shop", top=False)
    # The upper block: the deep green wall set back behind the grid on the south and Taft faces,
    # the plain ends flush on the north and east.
    # A plain box with world UVs, so the drawn cells sit on the 1.6 m grid the fins stand on.
    b.base("land_sixties_wall", S_G)
    box(b, S_X0 + S_DEPTH, S_X1 - 0.02, S_YS + S_DEPTH, S_YN - 0.02, S_G - 0.3, S_ROOF, "land_sixties_wall", r=0.15)
    # The slab under the block (the arcade's ceiling), whole footprint.
    b.solid(rect(S_X0, S_X1, S_YS, S_YN, r=0.2), S_G - 0.35, S_G, "land_sixties_fin")
    # The plain ends: north and east, thick painted walls wrapping round to meet the grid.
    box(b, S_X0 + 0.02, S_X1 + 0.08, S_YN - 0.45, S_YN + 0.06, S_G - 0.3, S_ROOF + 0.05, "land_sixties_end", r=0.08)
    box(b, S_X1 - 0.45, S_X1 + 0.08, S_YS - 0.05, S_YN + 0.02, S_G - 0.3, S_ROOF + 0.05, "land_sixties_end", r=0.08)
    # Pilotis: tapered round columns on the G. Apacible pavement, and at the lot line.
    for x in (13.0, 17.8, 22.6, 27.4, 32.2):
        for y, zb in ((S_YS + 0.5, PAVE - 0.05),):
            b.lathe(Vector((x, y, zb)), [(0.26, 0.0), (0.3, 0.3), (0.38, S_G - zb - 0.3), (0.42, S_G - zb)],
                    "land_sixties_fin", sides=14)
    # The floating roof slab and the tank room.
    b.solid(rect(S_X0 - 0.6, S_X1 + 0.4, S_YS - 0.6, S_YN + 0.4, r=0.3), S_ROOF + 0.05, S_ROOF + 0.45,
            "land_sixties_fin", cap_mat="land_roof")
    box(b, 28.5, 33.6, -114.8, -110.8, S_ROOF + 0.4, S_ROOF + 3.3, "land_sixties_end", r=0.08)
    box(b, 28.35, 33.75, -114.95, -110.65, S_ROOF + 3.25, S_ROOF + 3.5, "land_sixties_fin", r=0.06)
    b.finish(col, origin=o, bevel=0.035)


def sixties_grid(col):
    o = ANCHORS["sixties"]
    b = LBuf("land_sixties_grid", levels=[s_floor(k) + 0.12 for k in range(1, S_FLOORS + 2)])
    zb, zt = S_G - 0.3, S_ROOF + 0.1
    # Shelves at every floor, on the south face and the Taft face, overlapping at the corner.
    for k in range(1, S_FLOORS + 2):
        z = s_floor(k)
        box(b, S_X0 - 0.05, S_X1 - 0.4, S_YS - 0.05, S_YS + S_DEPTH + 0.1, z - 0.12, z + 0.12, "land_sixties_fin",
            r=0.07)
        box(b, S_X0 - 0.08, S_X0 + S_DEPTH + 0.1, S_YS - 0.02, S_YN - 0.5, z - 0.13, z + 0.11, "land_sixties_fin",
            r=0.07)
    # Vertical fins on the 1.6 m world grid (the cell drawing's edges), turned 12 degrees
    # (louvres), the full height of the grid.
    x = math.ceil((S_X0 + 1.0) / 1.6) * 1.6
    while x < S_X1 - 0.9:
        rot = Matrix.Rotation(math.radians(12), 4, "Z")
        b.rbox(Vector((x, S_YS + S_DEPTH / 2, (zb + zt) / 2)), (0.2, S_DEPTH - 0.02, zt - zb), "land_sixties_fin",
               r=0.06, rot=rot)
        x += 1.6
    y = math.ceil((S_YS + 1.0) / 1.6) * 1.6
    while y < S_YN - 1.0:
        rot = Matrix.Rotation(math.radians(90 + 12), 4, "Z")
        b.rbox(Vector((S_X0 + S_DEPTH / 2, y, (zb + zt) / 2)), (0.2, S_DEPTH - 0.02, zt - zb), "land_sixties_fin",
               r=0.06, rot=rot)
        y += 1.6
    # The corner pier, square and thick.
    box(b, S_X0 - 0.1, S_X0 + 0.55, S_YS - 0.1, S_YS + 0.55, zb, zt + 0.05, "land_sixties_fin", r=0.1)
    # The name on the Taft fascia (the roof slab's edge), letters on a band.
    b.panel(facing("-x", Vector((S_X0 - 0.63, (S_YS + S_YN) / 2, S_ROOF + 0.9))), 9.0, 0.9, 0.08, "land_sixties_fin",
            "land_sign_sixties", r=0.04)
    for yy in ((S_YS + S_YN) / 2 - 3.5, (S_YS + S_YN) / 2 + 3.5):
        box(b, S_X0 - 0.6, S_X0 - 0.3, yy - 0.1, yy + 0.1, S_ROOF + 0.4, S_ROOF + 0.7, "prop_steel_dark", r=0.03)
    b.finish(col, origin=o, bevel=0.03)


def sixties_canopy(col):
    """The folded-plate canopy over the roof terrace: four V folds on slim square columns."""
    o = ANCHORS["sixties"]
    b = LBuf("land_sixties_canopy")
    x0, x1, y0, y1 = 13.0, 25.0, -117.8, -111.4
    z = S_ROOF + 3.2
    folds = 4
    w = (x1 - x0) / folds
    rings = []
    for yy in (y0, y1):
        ring = []
        for k in range(folds * 2 + 1):
            xx = x0 + k * w / 2
            zz = z + (0.9 if k % 2 == 0 else 0.0)
            ring.append(Vector((xx, yy, zz)))
        rings.append(ring)
    t = 0.16
    # A thick sheet from the zigzag: two surfaces offset down, closed round the border.
    grid = [[rings[0][k] + (rings[1][k] - rings[0][k]) * f for k in range(len(rings[0]))] for f in (0.0, 0.5, 1.0)]
    b.sheet(grid, t, lambda i, j: "land_sixties_fin")
    for xx in (x0 + w, x1 - w):
        for yy in (y0 + 0.8, y1 - 0.8):
            b.prism((xx, yy, S_ROOF + 0.4), (xx, yy, z + 0.05), 0.14, 0.14, "land_sixties_fin", r=0.04)
    b.finish(col, origin=o, bevel=0.03)


def sixties(parent):
    col = collection("land_sixties (MAKABAYAN BUILDING)", parent)
    sixties_body(col)
    sixties_grid(col)
    sixties_canopy(col)
    return col


# ------------------------------------------------------------------ review

def stand_ins(parent):
    col = collection("review stand-ins", parent)

    def slab(name, x0, x1, y0, y1, top, colour):
        me = bpy.data.meshes.new(name)
        z0 = top - 0.4
        vs = [(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0), (x0, y0, top), (x1, y0, top), (x1, y1, top),
              (x0, y1, top)]
        fs = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
        me.from_pydata(vs, [], fs)
        mat = bpy.data.materials.new(name)
        if mat.node_tree is None:
            mat.use_nodes = True
        mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*colour, 1)
        me.materials.append(mat)
        col.objects.link(bpy.data.objects.new(name, me))

    slab("stand-in road", -10, 60, -170, 100, 0.0, (0.32, 0.32, 0.33))
    slab("stand-in lot N", 11.0, 60, 38.3, 100, LOT, (0.55, 0.52, 0.46))
    slab("stand-in lot S1", 11.0, 60, -116.9, -100, LOT, (0.55, 0.52, 0.46))
    slab("stand-in lot S2", 11.0, 60, -165, -134.7, LOT, (0.55, 0.52, 0.46))
    slab("stand-in walk", 7.0, 11.0, -170, 100, PAVE, (0.62, 0.60, 0.56))
    slab("stand-in walk G", 11.0, 60, -119.5, -116.9, PAVE, (0.62, 0.60, 0.56))
    return col


def lighting():
    scene = bpy.context.scene
    world = bpy.data.worlds.new("sky")
    scene.world = world
    if world.node_tree is None:
        world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (0.60, 0.74, 0.92, 1)
    bg.inputs["Strength"].default_value = 0.7
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy, sun.data.angle, sun.data.color = 4.6, math.radians(2.5), (1.0, 0.86, 0.68)
    sun.rotation_euler = Vector((0.86, 0.22, -0.46)).normalized().to_track_quat("-Z", "Y").to_euler()
    scene.collection.objects.link(sun)
    scene.view_settings.view_transform = "AgX"
    try:
        scene.view_settings.look = "AgX - Medium High Contrast"
    except TypeError:
        pass


def preview(version, only=None):
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 1000
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    cam.data.clip_end = 2000
    scene.collection.objects.link(cam)
    scene.camera = cam
    shots = [
        ("condo", (-6.0, 8.0, 12.0), (31.0, 58.0, 30.0), 24),
        ("condo_top", (8.0, 30.0, 58.0), (30.0, 57.0, 60.0), 24),
        ("condo_yard", (22.0, 30.0, 2.0), (30.0, 42.0, 2.0), 24),
        ("deco", (0.0, -118.0, 3.0), (17.0, -140.0, 9.0), 22),
        ("deco_corner", (4.0, -127.0, 1.7), (13.0, -137.0, 6.0), 20),
        ("sixties", (2.0, -132.0, 3.0), (22.0, -114.0, 11.0), 22),
        ("sixties_arcade", (6.0, -121.5, 1.6), (22.0, -118.0, 3.5), 20),
    ]
    for name, pos, tgt, lens in shots:
        if only and name not in only:
            continue
        path = PREVIEWS / f"landmark_{name}_v{version}.png"
        if path.exists():
            print("[ilalim-land] exists, skipped", path)
            continue
        pos, tgt = Vector(pos), Vector(tgt)
        cam.location, cam.data.lens = pos, lens
        cam.rotation_euler = (tgt - pos).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        print("[ilalim-land] preview", path)


def triangles(col):
    dg = bpy.context.evaluated_depsgraph_get()
    total = 0
    per = {}
    for o in col.all_objects:
        if o.type != "MESH":
            continue
        me = o.evaluated_get(dg).to_mesh()
        me.calc_loop_triangles()
        per[o.name] = len(me.loop_triangles)
        total += per[o.name]
        o.evaluated_get(dg).to_mesh_clear()
    return total, per


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
    only = argv[argv.index("--only") + 1].split(",") if "--only" in argv else None
    bpy.ops.wm.read_factory_settings(use_empty=True)
    placed = collection("landmarks (placed)")
    condo(placed)
    deco(placed)
    sixties(placed)
    stand_ins(collection("review"))
    lighting()
    SOURCE.mkdir(parents=True, exist_ok=True)
    out = SOURCE / "landmarks.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(out), compress=True, relative_remap=True)
    backup = SOURCE / "landmarks.blend1"
    if backup.exists():
        backup.unlink()
    total, per = triangles(placed)
    for k, v in sorted(per.items()):
        print(f"[ilalim-land]   {k:28} {v:7d} tris")
    print(f"[ilalim-land] saved {out}; {len(per)} objects, {total} triangles after bevel")
    if version:
        preview(version, only)


if __name__ == "__main__":
    main()
