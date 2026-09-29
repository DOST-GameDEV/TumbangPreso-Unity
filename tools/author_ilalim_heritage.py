"""Model the heritage campus and government buildings west of Taft for Ilalim ng Tulay (ILALIM-1.3).

  py -3 tools/author_ilalim_textures_heritage.py        # paint the textures first
  blender -b --python tools/author_ilalim_heritage.py -- [--preview N] [--only NAME]

Writes ArtSource/ilalim/heritage.blend. With --preview it also writes versioned renders to
Logs/ilalim-blender/heritage_<shot>_vN.png (game's-eye shots from the court, close-ups, aerials).

WHAT IS BUILT. Every OpenStreetMap building WEST of the Taft corridor (footprint centre x < -11)
within 250 m of the court, from ArtSource/ilalim/osm_layout.json AFTER the blockout's
sightline_override() (so the Supreme Court is thinned at x = -50 and the Rizal Hall compound,
with the Gat Andres Bonifacio Building, is moved 26 m east). Not built here:
  * Rizal Hall itself, and the two small OSM blocks that sit inside its west wing (another kit);
  * anything inside the Taft corridor.
Each building stands at its real footprint (squared to its own axes and snapped to 1 m) and its
real storey count (OSM levels; unknown counts take 2 on the hospital side, 3 on the court side).

THE REFERENCES (Wikimedia Commons, studied only, never copied): the PGH front with its Oblation,
the PGH Nurses Home, the Supreme Court corner from above Taft, the campus aerial ("a sea of red
roofs among trees"), Padre Faura with the white court facades. The campus is William E.
Parsons's American-period neoclassicism: cream stucco, rusticated ground storeys, arcades,
arched top-floor loggias, deep-set dark steel windows, deep-eaved red-brown hipped roofs, and
window aircon units everywhere. The court buildings are white, with giant columns and pale
sage-green metal roofs behind an attic.

ONE GENERATOR FOR EVERY BUILDING, driven by the footprint polygon and a STYLE (per-building
parameters: colours, storey heights, bay rhythm, arcade or windows at ground, arched top storey,
pilasters or giant columns or none, quoins, rustication, roof kind, eave depth, pitch). Variety
is CONSTRUCTION, not repaint (KANTO_DESIGN_GUIDE section 4):
  * pgh:        cream, ground-floor ARCADE on the street and courtyard sides, square windows,
                red hipped roof on 1.4 m eaves, courtyard wings around big footprints;
  * pgh_tall:   the Central Block: many storeys, every top-floor window arched, pilasters;
  * nurses:     dusty pink, RUSTICATED arcaded ground floor, QUOINS, arched top loggia;
  * old_court:  grey-cream, rusticated ground, arched top loggia, bay pilasters, red roof;
  * court:      white, GIANT half-round columns over the upper storeys, rusticated base, an
                ATTIC with the roof set back behind it, sage metal roof, UNNAMED (no seal);
  * campus:     off-white college block, pilasters, jalousie windows, red roof;
  * modern:     the eye-centre: non-rectangular plan, flat roof behind a parapet;
  * pavilion:   one storey, arcaded all round (the small museum).

HOW A BUILDING IS MADE (all in tools, nothing hand-modelled):
  1. The footprint is turned into its own axes, squared, snapped to 1 m and rasterised. A
     footprint wider than 36 m becomes WINGS 13 m deep around a courtyard, as the real campus
     pavilions are. The mass is traced back into wall loops (outer loops and courtyard loops).
  2. The WALL SHELL is one welded mesh per building: every facade is a grid whose openings are
     pushed in as real recesses (0.3 m windows, 0.6 m doors, 2 m arcade loggias), welded at the
     corners.
  3. TRIM is swept along the loops with mitred corners: plinth (broken at openings), string
     courses at every floor, the cornice, the attic coping; plus sills, arched heads (a spandrel
     piece set into the recess), pilasters, giant columns, quoins, door steps and lintels.
  4. The ROOF is the true hipped roof of the squared plan: its height is the L-infinity
     distance to the walls, which for a squared plan is exactly the straight-skeleton hip roof
     (45 degree hips and valleys, ridges down the middle of every wing). It runs out past the
     walls to the eave, and a live Solidify gives it a fascia and a painted board soffit.
  5. Fittings: window aircon units (modelled: casing, louvred front), door panels, roof water
     tanks on the flat roofs.
LOD by distance from the court (nearest footprint corner): within 60 m, frames, sills, keystones,
imposts and aircons; within 120 m, sills, keystones and archivolts; beyond, recessed windows,
spandrels, swept trim and roofs only. Bevels are one segment (hardened normals keep them soft).

THE HOUSE STYLE: chunky, soft-bevelled (every object has a live Bevel with hardened normals),
nothing coplanar (attached parts penetrate 1 to 3 cm), world-scale UVs, flat hand-drawn
textures from tools/author_ilalim_textures_heritage.py. WEATHERING IS POSITIONAL: three drawn
multiplier overlays on their own UV maps, each its own drawing:
  * UVGrime: v = metres below the cornice (rain tongues and the eave shadow band);
  * UVSill:  u = the window bay (window in the middle), v = metres below that storey's sill, so
             the stains hang under the actual windows;
  * UVSplash: v = metres above the ground (the green-grey rising damp).

ROLE HUES (Art_Direction section 1): the roof red-brown is hue ~10 degrees and dark, the sage is
green-grey, the glass teal-grey; nothing sits near offence orange or defence blue.

COORDINATES: Blender X = game x (east), Blender Y = game z (north), origin = the court centre on
Taft. Buildings are placed at their world positions; each object's origin is its footprint centre
on the ground.
"""
import json
import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
import numpy as np
from mathutils import Matrix, Vector

TOOLS = Path(__file__).resolve().parent
sys.path.insert(0, str(TOOLS))
import author_ilalim_blockout as BO  # noqa: E402  (read-only: sightline_override, the ground heights)
import author_ilalim_lrt as LRT  # noqa: E402  (read-only: fillet, rounded_rect)
from ilalim_antitile import anti_tile  # noqa: E402

# Flat roofs read as wallpaper from above (owner: "not only ground but the flat roofs"): rotated,
# feathered extra samples (tools/ilalim_antitile.py).
ANTI_TILE = {"heritage_flat_roof"}

ROOT = TOOLS.parent
SOURCE = ROOT / "ArtSource" / "ilalim"
TEXTURES = SOURCE / "textures"
PREVIEWS = ROOT / "Logs" / "ilalim-blender"
TILE_M = 4.0
UP = Vector((0, 0, 1))
GROUND = BO.LOT_TOP          # 0.24, the lots and parking the campus stands on
SINK = 0.14                  # walls start this far below the ground
EXTENT = 250.0
fillet, rounded_rect = LRT.fillet, LRT.rounded_rect


def srgb_lin(h):
    c = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    return tuple(x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c)


# ------------------------------------------------------------------ styles

class Style:
    def __init__(self, name, **kw):
        self.name = name
        self.wall = "e9dcb4"          # stucco colour (sRGB hex)
        self.trim = "f4eedc"
        self.storey = 4.2
        self.ground = 4.4             # ground storey height
        self.bay = 3.8
        self.win_w = 1.5
        self.ground_mode = "windows"  # or "arcade" on street facades
        self.court_mode = "arcade"    # courtyard facades
        self.top_arch = False
        self.all_arch = False
        self.rustic = False
        self.pilasters = "none"       # "bays", "giant"
        self.quoins = False
        self.roof = "red"             # "sage", "flat"
        self.attic = 0.0              # attic height above the cornice (roof set back behind it)
        self.eave = 1.5
        self.pitch = 24.0
        self.window = ("sash",)
        self.ac = 0.18                # share of windows with an aircon (near buildings)
        self.string = True
        self.plinth = 0.85
        self.__dict__.update(kw)


STYLES = {
    "pgh": Style("pgh", ground_mode="arcade"),
    "pgh_small": Style("pgh_small", ground_mode="windows", bay=3.4, eave=1.2),
    "pgh_tall": Style("pgh_tall", wall="e6d3a3", storey=3.6, ground=4.2, bay=3.4, win_w=1.3, top_arch=True,
                      pilasters="bays", eave=1.2, pitch=22.0, court_mode="windows"),
    "nurses": Style("nurses", wall="dcc4b2", trim="efe4da", storey=4.0, ground=4.4, bay=3.6, win_w=1.3,
                    ground_mode="arcade", top_arch=True, rustic=True, quoins=True, eave=1.0, pitch=26.0),
    "old_court": Style("old_court", wall="d8d1bf", trim="efece2", storey=4.3, ground=4.6, bay=3.6, win_w=1.3,
                       top_arch=True, rustic=True, pilasters="bays", eave=1.1, pitch=24.0),
    "court": Style("court", wall="dcd8cd", trim="f4f2ec", storey=3.9, ground=4.6, bay=3.6, win_w=1.4,
                   rustic=True, pilasters="giant", roof="sage", attic=1.4, pitch=16.0, eave=0.0,
                   court_mode="windows", window=("sash",), ac=0.1),
    "court_annex": Style("court_annex", wall="e6e3da", trim="f5f3ed", storey=3.8, ground=4.2, bay=3.4, win_w=1.3,
                         roof="sage", eave=0.9, pitch=18.0, court_mode="windows", pilasters="bays"),
    "campus": Style("campus", wall="e7e0cf", trim="f6f2e8", storey=3.6, ground=4.0, bay=3.2, win_w=1.6,
                    pilasters="bays", window=("jal",), eave=1.1, pitch=22.0, court_mode="windows"),
    "modern": Style("modern", wall="e9e6dc", trim="f5f3ec", storey=3.5, ground=4.2, bay=3.0, win_w=1.9,
                    roof="flat", attic=1.1, window=("jal", "sash"), string=True),
    "pavilion": Style("pavilion", wall="e6dab8", ground=4.6, ground_mode="arcade", bay=3.4, eave=1.4),
}


def style_for(b, cx, cy):
    """(style, levels, front directions) for one OSM building."""
    name, levels = b["name"], b["levels"]
    south_of_faura = cy < 38
    if "Nurses" in name:
        return STYLES["nurses"], levels or 3
    if "Out-Patient" in name:
        return STYLES["pgh"], levels or 2
    if "Central Block" in name:
        return STYLES["pgh_tall"], levels or 7
    if "Centennial" in name:
        return STYLES["court"], levels or 5
    if "Old Supreme" in name:          # before "Supreme Court Main", which it also contains
        return STYLES["old_court"], levels or 3
    if "Supreme Court Main" in name:
        return STYLES["court"], levels or 3
    if "Bonifacio" in name:
        return STYLES["campus"], levels or 4
    if "Sentro" in name:
        return STYLES["modern"], levels or 5
    if "Museum" in name:
        return STYLES["pavilion"], 1
    if south_of_faura:
        big = b["use"] == "hospital" and polygon_area(b["poly"]) > 1500
        return (STYLES["pgh"] if big else STYLES["pgh_small"]), levels or 2
    if b["use"] == "office":
        return STYLES["campus"], levels or 3
    return STYLES["court_annex"], levels or 3


def polygon_area(poly):
    return abs(sum(poly[i - 1][0] * poly[i][1] - poly[i][0] * poly[i - 1][1] for i in range(len(poly)))) / 2


# ------------------------------------------------------------------ materials

OVERLAYS = {"eave": ("heritage_grime_eave", "UVGrime"), "sill": ("heritage_grime_sill", "UVSill"),
            "splash": ("heritage_grime_plinth", "UVSplash")}


def material(name, tex, tint=None, overlays=(), rough=0.85, normal=0.4, base="f4f1ea"):
    """A flat illustrated material: the albedo, multiplied by a tint (the colour relative to the
    neutral texture's base colour), then by each positional grime overlay on its own UV map."""
    m = bpy.data.materials.get(name)
    if m:
        return m
    m = bpy.data.materials.new(name)
    if m.node_tree is None:
        m.use_nodes = True
    nodes, links = m.node_tree.nodes, m.node_tree.links
    bsdf = nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = rough
    if tex is None:
        c = srgb_lin(tint)
        bsdf.inputs["Base Color"].default_value = (*c, 1)
        m.diffuse_color = (*c, 1)
        return m
    uv = nodes.new("ShaderNodeUVMap")
    uv.uv_map = "UVMap"
    albedo = nodes.new("ShaderNodeTexImage")
    albedo.image = bpy.data.images.load(str(TEXTURES / f"{tex}_albedo.png"), check_existing=True)
    links.new(uv.outputs["UV"], albedo.inputs["Vector"])
    colour = albedo.outputs["Color"]
    if name in ANTI_TILE:
        colour = anti_tile(nodes, links, uv.outputs["UV"], albedo.image, colour)
    avg = (0.7, 0.7, 0.7)
    if tint is not None:
        t, b = srgb_lin(tint), srgb_lin(base)
        mult = tuple(a / c for a, c in zip(t, b))
        mix = nodes.new("ShaderNodeMix")
        mix.data_type, mix.blend_type = "RGBA", "MULTIPLY"
        mix.inputs["Factor"].default_value = 1.0
        links.new(colour, mix.inputs[6])
        mix.inputs[7].default_value = (*mult, 1)
        colour = mix.outputs[2]
        avg = t
    for key in overlays:
        image, layer = OVERLAYS[key]
        guv = nodes.new("ShaderNodeUVMap")
        guv.uv_map = layer
        g = nodes.new("ShaderNodeTexImage")
        g.image = bpy.data.images.load(str(TEXTURES / f"{image}.png"), check_existing=True)
        g.image.colorspace_settings.name = "Non-Color"
        links.new(guv.outputs["UV"], g.inputs["Vector"])
        mul = nodes.new("ShaderNodeMix")
        mul.data_type, mul.blend_type = "RGBA", "MULTIPLY"
        mul.inputs["Factor"].default_value = 1.0
        links.new(colour, mul.inputs[6])
        links.new(g.outputs["Color"], mul.inputs[7])
        colour = mul.outputs[2]
    links.new(colour, bsdf.inputs["Base Color"])
    npath = TEXTURES / f"{tex}_normal.png"
    if normal and npath.exists():
        nt = nodes.new("ShaderNodeTexImage")
        nt.image = bpy.data.images.load(str(npath), check_existing=True)
        nt.image.colorspace_settings.name = "Non-Color"
        links.new(uv.outputs["UV"], nt.inputs["Vector"])
        nm = nodes.new("ShaderNodeNormalMap")
        nm.inputs["Strength"].default_value = normal
        links.new(nt.outputs["Color"], nm.inputs["Color"])
        links.new(nm.outputs["Normal"], bsdf.inputs["Normal"])
    m.diffuse_color = (*avg, 1)
    return m


def mats_for(style):
    """Material names for one style; created on first use."""
    s = style.name
    names = {
        "wall": f"heritage_wall_{s}", "rustic": f"heritage_rustic_{s}", "trim": f"heritage_trim_{s}",
        "loggia": f"heritage_loggia_{s}", "plinth": "heritage_plinth", "floor": "heritage_loggia_floor",
        "roof": "heritage_roof_sage" if style.roof == "sage" else "heritage_roof_red",
        "fascia": "heritage_fascia", "soffit": "heritage_soffit", "flatroof": "heritage_flat_roof",
        "sash": "heritage_window_sash", "jal": "heritage_window_jal", "door": "heritage_door",
        "frame": "heritage_frame", "ac": "heritage_ac_case", "ac_front": "heritage_ac_front",
        "tank": "heritage_water_tank", "step": "heritage_step",
    }
    material(names["wall"], "heritage_stucco", style.wall, ("eave", "sill", "splash"))
    material(names["rustic"], "heritage_rustic", style.wall, ("sill", "splash"), normal=0.6, base="f2eee6")
    material(names["trim"], "heritage_trim", style.trim, ("eave", "sill"), base="f7f5ef")
    # The loggia back walls: the same stucco in shade, a step darker, so the arcade reads deep.
    loggia = "".join(f"{int(int(style.wall[i:i + 2], 16) * 0.84):02x}" for i in (0, 2, 4))
    material(names["loggia"], "heritage_stucco", loggia, ("splash",))
    material(names["plinth"], "heritage_plinth", None, ("splash",))
    material(names["floor"], "heritage_plinth", "c9c2b4", (), base="a39b8c")
    material("heritage_roof_red", "heritage_roof_red", None, (), normal=0.5)
    material("heritage_roof_sage", "heritage_roof_sage", None, (), normal=0.5)
    material(names["fascia"], "heritage_trim", "f1ede2", (), base="f7f5ef")
    material(names["soffit"], "heritage_soffit", None, (), normal=0.3)
    material(names["flatroof"], "heritage_plinth", "b5afa3", (), base="a39b8c")
    material(names["sash"], "heritage_window_sash", None, (), rough=0.35, normal=0)
    material(names["jal"], "heritage_window_jal", None, (), rough=0.4, normal=0)
    material(names["door"], "heritage_door", None, (), rough=0.6, normal=0)
    material(names["frame"], "heritage_trim", "ece6d6", (), base="f7f5ef")
    material(names["ac"], None, "dcd9cf")
    material(names["ac_front"], "heritage_ac_front", None, (), normal=0)
    material(names["tank"], None, "3f5a4f")
    material(names["step"], "heritage_plinth", "b8b0a0", ("splash",), base="a39b8c")
    return names


# ------------------------------------------------------------------ mesh buffer

class Buf:
    """One object's geometry in a bmesh, with material slots and per-face UV tags:
    `mode` 0 = world UVs, 1 = roof slope UVs, 2 = one opening (0..1), `fid` = facade record."""

    def __init__(self, name):
        self.name, self.bm, self.mats = name, bmesh.new(), []
        self.mode = self.bm.faces.layers.int.new("mode")
        self.op = self.bm.faces.layers.int.new("op")
        self.fid = self.bm.faces.layers.int.new("fid")
        self.openings = []

    def mi(self, mat):
        if mat not in self.mats:
            self.mats.append(mat)
        return self.mats.index(mat)

    def tag(self, faces, mat=None, mode=None, op=None, fid=None):
        for f in faces:
            if mat is not None:
                f.material_index = self.mi(mat)
            if mode is not None:
                f[self.mode] = mode
            if op is not None:
                f[self.op] = op
            if fid is not None:
                f[self.fid] = fid

    def loft(self, rings, mat, cap=True, closed_path=False):
        vs = [[self.bm.verts.new(c) for c in ring] for ring in rings]
        if closed_path:
            vs.append(vs[0])
        k = len(rings[0])
        faces = []
        for r0, r1 in zip(vs, vs[1:]):
            for j in range(k):
                faces.append(self.bm.faces.new((r0[j], r0[(j + 1) % k], r1[(j + 1) % k], r1[j])))
        if cap and not closed_path:
            faces.append(self.bm.faces.new(list(reversed(vs[0]))))
            faces.append(self.bm.faces.new(vs[-1]))
        bmesh.ops.recalc_face_normals(self.bm, faces=faces)
        self.tag(faces, mat=mat)
        return faces

    def prism(self, pts, z0, z1, mat, top_scale=1.0):
        """A closed plan outline (world xy) swept up from z0 to z1."""
        cx = sum(p[0] for p in pts) / len(pts)
        cy = sum(p[1] for p in pts) / len(pts)
        bottom = [Vector((x, y, z0)) for x, y in pts]
        top = [Vector((cx + (x - cx) * top_scale, cy + (y - cy) * top_scale, z1)) for x, y in pts]
        return self.loft([bottom, top], mat)

    def obox(self, c, t, n, hw, hd, z0, z1, mat, r=0.04, top_scale=1.0):
        """A rounded box on a facade: `hw` along the facade t, `hd` along its normal n."""
        pts = [(c[0] + t[0] * x + n[0] * y, c[1] + t[1] * x + n[1] * y) for x, y in rounded_rect(hw, hd, r)]
        return self.prism(pts, z0, z1, mat, top_scale)

    def frame_prism(self, profile, origin, t, n, d0, d1, mat):
        """A closed (u, z) profile on a facade plane, extruded from depth d0 to d1 along n."""
        rings = []
        for d in (d0, d1):
            rings.append([Vector((origin[0] + t[0] * u + n[0] * d, origin[1] + t[1] * u + n[1] * d, z))
                          for u, z in profile])
        return self.loft(rings, mat)

    def tube(self, path, radius, mat, sides=10):
        rings = []
        for i, p in enumerate(path):
            d = (path[min(i + 1, len(path) - 1)] - path[max(i - 1, 0)]).normalized()
            side = d.cross(UP)
            if side.length < 1e-4:
                side = d.cross(Vector((1, 0, 0)))
            side.normalize()
            up = side.cross(d).normalized()
            rings.append([p + (side * math.cos(a) + up * math.sin(a)) * radius
                          for a in (k / sides * math.tau for k in range(sides))])
        return self.loft(rings, mat)

    # -------------------------------------------------------------- UVs

    def uvs(self, ctx):
        bm = self.bm
        uv = bm.loops.layers.uv.new("UVMap")
        eave = bm.loops.layers.uv.new("UVGrime")
        sill = bm.loops.layers.uv.new("UVSill")
        splash = bm.loops.layers.uv.new("UVSplash")
        clean = 0.999
        for f in bm.faces:
            n = f.normal
            mode, op, fid = f[self.mode], f[self.op], f[self.fid]
            side = abs(n.z) <= 0.7
            t = UP.cross(n)
            t = t.normalized() if t.length > 1e-6 else Vector((1, 0, 0))
            cz = f.calc_center_median().z
            rec = ctx.facades[fid - 1] if fid else None
            for l in f.loops:
                co = l.vert.co
                if mode == 2 and op:
                    o, tt, w, h = self.openings[op - 1]
                    l[uv].uv = ((co - o).dot(tt) / w, (co.z - o.z) / h)
                elif mode == 1:
                    d = Vector((n.x, n.y, 0))
                    d = d.normalized() if d.length > 1e-4 else Vector((0, 1, 0))
                    p = Vector((-d.y, d.x, 0))
                    slope = math.sqrt(max(1e-6, 1 - n.z * n.z)) / max(n.z, 0.2)
                    l[uv].uv = (co.dot(p) / TILE_M, co.dot(d) * math.sqrt(1 + slope * slope) / TILE_M)
                elif side:
                    l[uv].uv = (co.dot(t) / TILE_M, co.z / TILE_M)
                else:
                    l[uv].uv = (co.x / TILE_M, co.y / TILE_M)
                # Positional grime.
                if side and cz < ctx.eave_z + 0.05 and mode != 2:
                    l[eave].uv = (co.dot(t) / 8.0, min(clean, max(0.0, (ctx.eave_z - co.z) / 3.0)))
                else:
                    l[eave].uv = (co.x / 8.0, clean)
                v = clean
                u = co.x / 4.0
                if side and rec is not None and mode != 2:
                    for floor_z, sill_z in rec["sills"]:
                        if floor_z - 0.6 <= cz < sill_z:
                            v = min(clean, max(0.0, (sill_z - co.z) / 2.5))
                            break
                    a = (Vector((co.x, co.y)) - rec["p0"]).dot(rec["t"])
                    # The bay comes from the FACE centre: taken per corner, a corner on a bay
                    # edge jumped to the next variant and smeared it across the face (review v3).
                    fc = f.calc_center_median()
                    ac = (Vector((fc.x, fc.y)) - rec["p0"]).dot(rec["t"])
                    k = max(0, min(len(rec["bays"]) - 1, int(math.floor(rec["bay_of"](ac)))))
                    b0, b1 = rec["bays"][k]
                    frac = min(0.999, max(0.0, (a - b0) / max(0.1, b1 - b0)))
                    u = ((k * 3 + fid) % 4 + frac) / 4.0
                l[sill].uv = (u, v)
                if side:
                    l[splash].uv = (co.dot(t) / 8.0, min(clean, max(0.0, (co.z - GROUND) / 1.5)))
                else:
                    l[splash].uv = (co.x / 8.0, clean)

    def finish(self, collection, ctx, bevel=0.03, segments=1, weld=True, origin=None):
        if weld:
            bmesh.ops.remove_doubles(self.bm, verts=self.bm.verts, dist=0.002)
        # Normals come from construction: the wall shell is open (its roof and the ground close
        # it), so a global recalculation could turn a whole facade inside out.
        for f in self.bm.faces:
            f.normal_update()
        self.uvs(ctx)
        if origin is not None:
            bmesh.ops.translate(self.bm, verts=self.bm.verts, vec=-origin)
        mesh = bpy.data.meshes.new(self.name)
        self.bm.to_mesh(mesh)
        self.bm.free()
        for m in self.mats:
            mesh.materials.append(bpy.data.materials[m])
        for p in mesh.polygons:
            p.use_smooth = False
        obj = bpy.data.objects.new(self.name, mesh)
        if origin is not None:
            obj.location = origin
        collection.objects.link(obj)
        if bevel:
            mod = obj.modifiers.new("Bevel", "BEVEL")
            mod.width, mod.segments = bevel, segments
            mod.limit_method, mod.angle_limit = "ANGLE", math.radians(35)
            mod.harden_normals, mod.use_clamp_overlap = True, True
        return obj


def _opening(self, origin, t, w, h):
    """Register an opening frame for 0..1 UVs; returns its tag (index + 1)."""
    self.openings.append((Vector(origin), Vector((t[0], t[1], 0)), w, h))
    return len(self.openings)


Buf.opening = _opening


# ------------------------------------------------------------------ footprint to wall loops

def local_frame(poly):
    """The building's own axes: the length-weighted mean edge direction, modulo 90 degrees."""
    sx = sy = 0.0
    for (x0, y0), (x1, y1) in zip(poly, poly[1:] + poly[:1]):
        L = math.hypot(x1 - x0, y1 - y0)
        a = math.atan2(y1 - y0, x1 - x0) * 4
        sx += L * math.cos(a)
        sy += L * math.sin(a)
    return math.atan2(sy, sx) / 4


def orthogonality(poly, ang):
    good = total = 0.0
    for (x0, y0), (x1, y1) in zip(poly, poly[1:] + poly[:1]):
        L = math.hypot(x1 - x0, y1 - y0)
        a = (math.atan2(y1 - y0, x1 - x0) - ang) % (math.pi / 2)
        dev = min(a, math.pi / 2 - a)
        total += L
        good += L if dev < math.radians(9) else 0
    return good / max(total, 1e-6)


def squared(local):
    """Square a nearly orthogonal polygon: classify edges H or V, merge runs, rebuild corners,
    snap to 1 m."""
    edges = []
    n = len(local)
    for i in range(n):
        (x0, y0), (x1, y1) = local[i], local[(i + 1) % n]
        L = math.hypot(x1 - x0, y1 - y0)
        if L < 1e-6:
            continue
        horiz = abs(x1 - x0) >= abs(y1 - y0)
        edges.append([horiz, (y0 + y1) / 2 if horiz else (x0 + x1) / 2, L])
    merged = []
    for e in edges:
        if merged and merged[-1][0] == e[0]:
            m = merged[-1]
            m[1] = (m[1] * m[2] + e[1] * e[2]) / (m[2] + e[2])
            m[2] += e[2]
        else:
            merged.append(list(e))
    if len(merged) > 1 and merged[0][0] == merged[-1][0]:
        m, e = merged[0], merged.pop()
        m[1] = (m[1] * m[2] + e[1] * e[2]) / (m[2] + e[2])
        m[2] += e[2]
    if len(merged) < 4 or len(merged) % 2:
        return None
    pts = []
    for a, b in zip(merged, merged[1:] + merged[:1]):
        x = b[1] if a[0] else a[1]
        y = a[1] if a[0] else b[1]
        pts.append((round(x), round(y)))
    return pts


def point_in(x, y, poly):
    inside = False
    j = len(poly) - 1
    for i in range(len(poly)):
        xi, yi = poly[i]
        xj, yj = poly[j]
        if (yi > y) != (yj > y) and x < (xj - xi) * (y - yi) / (yj - yi + 1e-12) + xi:
            inside = not inside
        j = i
    return inside


def linf_segments(px, py, segs):
    """L-infinity distance from points to a set of axis-aligned segments (vectorised)."""
    d = np.full(px.shape, 1e9)
    for (x0, y0), (x1, y1) in segs:
        if abs(y1 - y0) < 1e-9:
            a, b = min(x0, x1), max(x0, x1)
            dx = np.maximum(np.maximum(a - px, px - b), 0)
            dy = np.abs(py - y0)
        else:
            a, b = min(y0, y1), max(y0, y1)
            dy = np.maximum(np.maximum(a - py, py - b), 0)
            dx = np.abs(px - x0)
        d = np.minimum(d, np.maximum(dx, dy))
    return d


def trace(cells):
    """Directed boundary loops of a set of unit cells (region on the LEFT of each loop)."""
    edges = {}
    for i, j in cells:
        for a, b in (((i, j), (i + 1, j)), ((i + 1, j), (i + 1, j + 1)),
                     ((i + 1, j + 1), (i, j + 1)), ((i, j + 1), (i, j))):
            if (b, a) in edges:
                del edges[(b, a)]
            else:
                edges[(a, b)] = True
    out = {}
    for a, b in edges:
        out.setdefault(a, []).append(b)
    loops = []
    while out:
        start = next(iter(out))
        loop, cur, prev_dir = [start], start, None
        while True:
            nxt = out[cur]
            if len(nxt) > 1 and prev_dir is not None:
                # A pinch: turn left first, which keeps each region's loop to itself.
                nxt.sort(key=lambda p: -((prev_dir[0] * (p[1] - cur[1]) - prev_dir[1] * (p[0] - cur[0]))))
            b = nxt.pop(0)
            if not nxt:
                del out[cur]
            prev_dir = (b[0] - cur[0], b[1] - cur[1])
            cur = b
            if cur == start:
                break
            loop.append(cur)
        # Drop collinear points.
        clean = []
        m = len(loop)
        for k in range(m):
            p0, p1, p2 = loop[k - 1], loop[k], loop[(k + 1) % m]
            if (p1[0] - p0[0]) * (p2[1] - p1[1]) - (p1[1] - p0[1]) * (p2[0] - p1[0]) != 0:
                clean.append(p1)
        if len(clean) >= 4:
            loops.append(clean)
    return loops


WING = 13        # wing depth around a courtyard, metres
RISE_MAX = 4.0   # the highest a hipped roof's ridge stands above its eave line
ROOF_VARIANTS = (("", None, "ffffff"), ("_bleached", "fff6f2", "e8e2de"), ("_weathered", "dcd7d3", "ffffff"))


def plan(b, shrink=1.0):
    """Footprint to plan: {'loops': world loops, 'segs': local segments, 'frame': (ang, cx, cy),
    'orth': bool, 'local_loops'}."""
    poly = [tuple(p) for p in b["poly"]]
    if poly[0] == poly[-1]:
        poly = poly[:-1]
    ang = local_frame(poly)
    cx = sum(p[0] for p in poly) / len(poly)
    cy = sum(p[1] for p in poly) / len(poly)
    ca, sa = math.cos(-ang), math.sin(-ang)
    local = [((x - cx) * ca - (y - cy) * sa, (x - cx) * sa + (y - cy) * ca) for x, y in poly]
    # `shrink` pulls the plan a few centimetres in, different per building, so two OSM buildings
    # that share a wall never put two walls in one plane.
    to_world = lambda p: (cx + shrink * (p[0] * math.cos(ang) - p[1] * math.sin(ang)),
                          cy + shrink * (p[0] * math.sin(ang) + p[1] * math.cos(ang)))
    sq = squared(local) if orthogonality(poly, ang) > 0.9 else None
    if sq is None:
        # A free plan (the curved eye-centre): walls along the simplified real outline.
        simple = [p for i, p in enumerate(local) if math.hypot(p[0] - local[i - 1][0], p[1] - local[i - 1][1]) > 0.8]
        if polygon_signed(simple) < 0:
            simple.reverse()
        return {"orth": False, "loops": [[to_world(p) for p in simple]], "local_loops": [simple],
                "segs": None, "frame": (ang, cx, cy), "to_world": to_world, "courtyard": False}
    xs, ys = [p[0] for p in sq], [p[1] for p in sq]
    cells = set()
    for i in range(min(xs), max(xs)):
        for j in range(min(ys), max(ys)):
            if point_in(i + 0.5, j + 0.5, sq):
                cells.add((i, j))
    # Courtyards for deep plans: keep only the wings within WING of the outline.
    loops0 = trace(cells)
    segs0 = [(l[k - 1], l[k]) for l in loops0 for k in range(len(l))]
    ci = np.array([c[0] + 0.5 for c in cells])
    cj = np.array([c[1] + 0.5 for c in cells])
    depth = linf_segments(ci, cj, segs0)
    courtyard = depth.max() > (WING + 5)
    if courtyard:
        keep = depth < WING
        cells = {c for c, k in zip(cells, keep) if k}
    loops = trace(cells)
    segs = [(l[k - 1], l[k]) for l in loops for k in range(len(l))]
    return {"orth": True, "loops": [[to_world(p) for p in l] for l in loops], "local_loops": loops, "segs": segs,
            "frame": (ang, cx, cy), "to_world": to_world, "cells": cells, "courtyard": courtyard, "shrink": shrink}


def polygon_signed(p):
    return sum(p[i - 1][0] * p[i][1] - p[i][0] * p[i - 1][1] for i in range(len(p))) / 2


# ------------------------------------------------------------------ the building

class Ctx:
    pass


def v2(p):
    return Vector((p[0], p[1]))


PAVILION_STYLES = {"pgh", "nurses", "old_court", "campus", "pgh_tall"}


def building(col, b, idx, detail, style=None, levels=None, name=None, pediment=False):
    xs = [p[0] for p in b["poly"]]
    ys = [p[1] for p in b["poly"]]
    if style is None:
        style, levels = style_for(b, sum(xs) / len(xs), sum(ys) / len(ys))
    levels = max(1, int(levels))
    pl = plan(b, 1.0 - 0.0015 * (1 + idx % 4))
    M = mats_for(style)
    rng = random.Random(idx * 7919 + 13)
    name = name or b["name"] or f"campus block {idx}"
    ctx = Ctx()
    ctx.facades = []
    g = GROUND
    z0 = g - SINK
    zp = g + style.plinth                      # plinth top
    floors = [g] + [g + style.ground + k * style.storey for k in range(levels - 1)]
    eave_z = g + style.ground + (levels - 1) * style.storey
    ctx.eave_z = eave_z
    top_z = eave_z + style.attic if style.attic else eave_z + 0.42
    heads, sills = [], []
    for k, f in enumerate(floors):
        h = style.ground if k == 0 else style.storey
        sill = max(zp + 0.2, f + 1.0) if k == 0 else f + 0.9
        head = f + h * (0.74 if k else 0.72)
        sills.append(sill)
        heads.append(head)
    arcade_top = floors[1] - 0.55 if levels > 1 else eave_z - 0.9
    door_top = heads[0]
    step_z = g + 0.12
    vbreaks = sorted({round(z, 3) for z in [z0, step_z, zp, top_z, arcade_top, *sills, *heads, *floors[1:], eave_z]})

    walls = Buf(f"{name} walls")
    trim = Buf(f"{name} trim")
    fit = Buf(f"{name} fittings")
    front = b.get("front") or pick_front(pl, b)
    street_edges = []

    loops = pl["loops"]
    outer_ids = set()
    for li, loop in enumerate(loops):
        if polygon_signed(loop) > 0:
            outer_ids.add(li)
    for li, loop in enumerate(loops):
        n = len(loop)
        for k in range(n):
            p0, p1 = v2(loop[k]), v2(loop[(k + 1) % n])
            t = (p1 - p0)
            L = t.length
            if L < 0.3:
                continue
            t.normalize()
            nrm = Vector((t.y, -t.x))
            if li in outer_ids:
                kind = "street" if any(nrm.dot(fv) > 0.7 for fv in front) else "side"
                if kind == "street":
                    street_edges.append((L, p0.copy(), t.copy(), nrm.copy()))
            else:
                kind = "court"
            facade(walls, trim, fit, ctx, style, M, p0, t, nrm, L, kind, vbreaks, floors, sills, heads,
                   arcade_top, door_top, step_z, z0, zp, top_z, eave_z, levels, detail, rng)
    # Swept trim along every loop.
    for loop in loops:
        sweeps(trim, style, M, loop, zp, floors, eave_z, top_z, z0, detail)
        if style.quoins and detail >= 1:
            quoins(trim, M, loop, zp, eave_z, polygon_signed(loop) > 0)
    origin = Vector((pl["frame"][1], pl["frame"][2], g))
    objs = [walls.finish(col, ctx, bevel=0.025, origin=origin), trim.finish(col, ctx, bevel=0.02, weld=False, origin=origin)]
    if fit.bm.faces:
        objs.append(fit.finish(col, ctx, bevel=0.015, weld=False, origin=origin))
    else:
        fit.bm.free()
    gable = b["front"][0] if pediment else None
    objs.append(roof(col, pl, style, M, eave_z, top_z, ctx, name, origin, rng, detail, gable=gable, idx=idx))
    if street_edges and not pediment:
        L, p0, t, nrm = max(street_edges, key=lambda e: e[0])
        if style.name in PAVILION_STYLES and L >= 30 and detail >= 1:
            objs += entrance_pavilion(col, b, idx, detail, style, levels, name, p0, t, nrm, L)
        elif style.attic and L >= 24:
            ped = Buf(f"{name} pediment")
            W = min(L * 0.4, 5 * style.bay)
            c = p0 + t * (L / 2)
            pediment_piece(ped, M, Vector((c.x, c.y, 0)), Vector((t.x, t.y, 0)), Vector((nrm.x, nrm.y, 0)), W,
                           top_z + 0.08, top_z + 0.08, 0.38, 0.2, -3.0)
            objs.append(ped.finish(col, ctx, bevel=0.02, weld=False, origin=origin))
    if pediment:
        # The pavilion's own front gable: the longest street edge is its front.
        L, p0, t, nrm = max(street_edges, key=lambda e: e[0])
        ped = Buf(f"{name} pediment")
        c = p0 + t * (L / 2)
        pediment_piece(ped, M, Vector((c.x, c.y, 0)), Vector((t.x, t.y, 0)), Vector((nrm.x, nrm.y, 0)), L + 0.3,
                       eave_z + 0.12, ctx.roof_z0 - 0.12, ctx.roof_tn, style.eave + 0.12, -1.2)
        objs.append(ped.finish(col, ctx, bevel=0.02, weld=False, origin=origin))
    return objs


def entrance_pavilion(col, b, idx, detail, style, levels, name, p0, t, nrm, L):
    """A projecting entrance block in the middle of the main street facade, three bays wide and a
    little taller, with its own hipped roof and a pediment: the PGH front's composition. It is
    a small building of the same style, overlapping 1.5 m into the main mass."""
    W = min(3 * style.bay + 0.6, L * 0.4)
    P = style.eave + 1.3
    c = p0 + t * (L / 2)
    pts = [c - t * (W / 2) - nrm * 1.5, c + t * (W / 2) - nrm * 1.5, c + t * (W / 2) + nrm * P, c - t * (W / 2) + nrm * P]
    fake = {"name": "", "levels": levels, "use": b["use"], "poly": [(p.x, p.y) for p in pts], "front": [nrm.copy()]}
    ps = Style(style.name + "_pavilion", **{k: v for k, v in style.__dict__.items() if k != "name"})
    ps.name = style.name
    ps.ground = style.ground + 0.9
    ps.plinth = style.plinth + 0.07        # its plinth top must not share the main one's plane
    ps.ground_mode = "windows"
    ps.top_arch = True
    ps.pilasters = "bays"
    ps.eave, ps.pitch = 0.45, 30.0
    ps.bay = (W - 2 * (1.1 if style.quoins else 0.9)) / 3 - 0.01
    return building(col, fake, idx + 5000, detail, ps, levels, f"{name} entrance", pediment=True)


def pediment_piece(buf, M, c, tt, n3, W, zb, zr, tn, front, back):
    """A pediment standing on the cornice: a wall-coloured tympanum whose rakes run PARALLEL to
    the gable roof behind it (rake height zr at the ends, rising at slope tn), a chunky base
    cornice, and two raking cornices that stand 16 cm proud of the roof as its verge."""
    rake = lambda u: zr + tn * (W / 2 - abs(u))
    tym = [(-W / 2, zb), (W / 2, zb), (W / 2, rake(W / 2)), (0.0, rake(0.0)), (-W / 2, rake(W / 2))]
    buf.frame_prism(tym, c, tt, n3, front, back, M["wall"])
    base = fillet([(-W / 2 - 0.3, zb - 0.1), (W / 2 + 0.3, zb - 0.1), (W / 2 + 0.3, zb + 0.16), (-W / 2 - 0.3, zb + 0.16)], 0.05, 2)
    buf.frame_prism(base, c, tt, n3, front + 0.14, back, M["trim"])
    for sgn in (-1, 1):
        x0 = sgn * (W / 2 + 0.3)
        band = [(x0, rake(W / 2) - tn * 0.3 - 0.06), (0.0, rake(0.0) - 0.06), (0.0, rake(0.0) + 0.3),
                (x0, rake(W / 2) - tn * 0.3 + 0.3)]
        if sgn > 0:
            band = list(reversed(band))
        buf.frame_prism(band, c, tt, n3, front + 0.14, back, M["trim"])


def pick_front(pl, b):
    """Street-facing directions: toward Taft (+x) always; toward Padre Faura for the buildings on
    it (south faces north of it, north faces south of it)."""
    cy = pl["frame"][2]
    dirs = [Vector((1, 0))]
    if 30 < cy < 80:
        dirs.append(Vector((0, -1)))
    if -10 < cy < 30:
        dirs.append(Vector((0, 1)))
    return dirs


# ------------------------------------------------------------------ one facade

def facade(walls, trim, fit, ctx, style, M, p0, t, nrm, L, kind, vbreaks, floors, sills, heads,
           arcade_top, door_top, step_z, z0, zp, top_z, eave_z, levels, detail, rng):
    margin = 1.1 if style.quoins else 0.9
    usable = L - 2 * margin
    nb = int(usable / style.bay) if usable > 2.2 else 0
    bays = []
    if nb >= 1:
        bw = usable / nb
        bays = [(margin + i * bw, margin + (i + 1) * bw) for i in range(nb)]
    openings = []          # (u0, u1, z0, z1, kind, depth, arch)
    arcade = (kind == "street" and style.ground_mode == "arcade") or (kind == "court" and style.court_mode == "arcade")
    door_bay = len(bays) // 2 if (kind == "street" and not arcade and bays) else -1
    win_tex = style.window
    for i, (a, b) in enumerate(bays):
        c = (a + b) / 2
        bw = b - a
        ww = min(style.win_w, bw * 0.55)
        for k in range(levels):
            top_storey = k == levels - 1 and levels > 1
            if k == 0 and arcade:
                aw = min(bw * 0.7, 3.0)
                openings.append((c - aw / 2, c + aw / 2, step_z, arcade_top, "arcade", 2.0, True))
                continue
            if k == 0 and i == door_bay:
                dw = min(bw * 0.6, 1.9)
                openings.append((c - dw / 2, c + dw / 2, step_z, door_top, "door", 0.6, False))
                continue
            arch = style.all_arch or (style.top_arch and top_storey) or style.name == "pavilion"
            tex = win_tex[(i + k) % len(win_tex)]
            openings.append((c - ww / 2, c + ww / 2, sills[k], heads[k], tex, 0.3, arch))
    fid = len(ctx.facades) + 1
    bay_edges = [a for a, _ in bays] or [0.0]
    bw0 = (bays[0][1] - bays[0][0]) if bays else max(L, 1)
    rec = {"p0": p0.copy(), "t": t.copy(), "bays": bays or [(0.0, L)],
           "bay_of": (lambda a, m=margin, w=bw0: (a - m) / w),
           "sills": [(f if i else f - 5, s) for i, (f, s) in enumerate(zip(floors, sills))]}
    ctx.facades.append(rec)
    # The grid.
    ubreaks = sorted({0.0, L, *(x for a, b in bays for x in (a, b)), *(o[0] for o in openings), *(o[1] for o in openings)})
    ubreaks = [u for i, u in enumerate(ubreaks) if i == 0 or u - ubreaks[i - 1] > 1e-3]
    zb = [z for z in vbreaks if z <= top_z + 1e-6]
    bm = walls.bm
    base = Vector((p0.x, p0.y, 0))
    tt = Vector((t.x, t.y, 0))
    grid = [[bm.verts.new(base + tt * u + Vector((0, 0, z))) for z in zb] for u in ubreaks]
    wall_mat = M["wall"]
    faces_of = {}
    for i in range(len(ubreaks) - 1):
        for j in range(len(zb) - 1):
            f = bm.faces.new((grid[i][j], grid[i + 1][j], grid[i + 1][j + 1], grid[i][j + 1]))
            uc, zc = (ubreaks[i] + ubreaks[i + 1]) / 2, (zb[j] + zb[j + 1]) / 2
            hit = None
            for oi, o in enumerate(openings):
                if o[0] < uc < o[1] and o[2] < zc < o[3]:
                    hit = oi
                    break
            if hit is not None:
                faces_of.setdefault(hit, []).append(f)
            m = wall_mat
            if zc < zp:
                m = M["plinth"] if hit is None else wall_mat
            elif style.rustic and zc < floors[1 if levels > 1 else 0] + (0 if levels > 1 else style.ground):
                m = M["rustic"]
            walls.tag([f], mat=m, fid=fid)
    # Faces are wound (u, u+1, u+1 z+1, z+1), so each normal is t x up = nrm: outward.
    n3 = Vector((nrm.x, nrm.y, 0))
    for oi, fs in faces_of.items():
        u0, u1, oz0, oz1, okind, depth, arch = openings[oi]
        ret = bmesh.ops.extrude_face_region(bm, geom=fs)
        new_v = [e for e in ret["geom"] if isinstance(e, bmesh.types.BMVert)]
        new_f = [e for e in ret["geom"] if isinstance(e, bmesh.types.BMFace)]
        bmesh.ops.translate(bm, verts=new_v, vec=-n3 * depth)
        bmesh.ops.delete(bm, geom=fs, context="FACES")
        # Reveal faces: every face touching the new verts that is not a back face.
        side = {f for v in new_v for f in v.link_faces} - set(new_f)
        walls.tag(side, mat=M["rustic"] if (style.rustic and okind == "arcade") else wall_mat, fid=0)
        if okind == "arcade":
            walls.tag(new_f, mat=M["loggia"], fid=0)
            for f in side:
                f.normal_update()
                if f.normal.z > 0.7:
                    walls.tag([f], mat=M["floor"])
                elif f.normal.z < -0.7:
                    walls.tag([f], mat=M["loggia"])
        else:
            origin = base + tt * u0 - n3 * depth + Vector((0, 0, oz0))
            op = walls.opening(origin, tt, u1 - u0, oz1 - oz0)
            walls.tag(new_f, mat=M[okind], mode=2, op=op, fid=0)
        dressing(walls, trim, fit, style, M, base, tt, n3, u0, u1, oz0, oz1, okind, depth, arch, zp, detail, rng)
    # Plinth and pilasters along this facade.
    plinth_gaps = [(o[0], o[1]) for o in openings if o[2] < zp]
    plinth(trim, M, base, tt, n3, L, plinth_gaps, z0, zp)
    if kind in ("street", "court") or style.pilasters == "giant":
        pilasters(trim, style, M, base, tt, n3, bays, floors, zp, eave_z, levels, detail, kind)


def dressing(walls, trim, fit, style, M, base, tt, n3, u0, u1, oz0, oz1, okind, depth, arch, zp, detail, rng):
    w = u1 - u0
    c = base + tt * ((u0 + u1) / 2)
    if arch:
        # The arched head: a spandrel set 3 cm into the recess, reaching 2 cm into the reveals.
        r = w / 2
        spring = oz1 - r
        ext = 0.02
        arc, outer = [], []
        N = 8 if detail >= 1 else 6
        angs = sorted({k / N * math.pi for k in range(N + 1)} | {math.atan2(r + ext, r + ext), math.pi - math.atan2(r + ext, r + ext)})
        for a in angs:
            ca, sa = math.cos(a), math.sin(a)
            arc.append((r * ca, spring + r * sa))
            tside = (r + ext) / abs(ca) if abs(ca) > 1e-6 else 1e9
            ttop = (r + ext) / sa if sa > 1e-6 else 1e9
            s = min(tside, ttop)
            outer.append((s * ca, spring + s * sa))
        prof = arc + list(reversed(outer))
        thick = min(0.45, depth - 0.05)
        # The spandrel is WALL, set 3 cm back: painted white it read as corner brackets, not an
        # arch (review v3). The arch itself is the archivolt band below.
        fill = M["rustic"] if (style.rustic and okind == "arcade") else M["wall"]
        walls.frame_prism(prof, c, tt, n3, -0.03, -0.03 - thick, fill)
        # The archivolt: a chunky band round the arch, 7 cm proud, springing from two imposts.
        bw = 0.2 if okind == "arcade" else 0.14
        band = [((r + ext) * math.cos(a), spring + (r + ext) * math.sin(a)) for a in [k / N * math.pi for k in range(N + 1)]]
        band += [((r + ext + bw) * math.cos(a), spring + (r + ext + bw) * math.sin(a)) for a in [k / N * math.pi for k in range(N, -1, -1)]]
        if detail >= 1:
            trim.frame_prism(band, c, tt, n3, 0.07, -0.05, M["trim"])
        for sgn in ((-1, 1) if detail >= 2 else ()):
            imp = c.xy + tt.xy * sgn * (r + ext + bw / 2)
            trim.obox(imp, tt, n3, bw / 2 + 0.08, 0.1, spring - 0.16, spring + 0.02, M["trim"], r=0.03)
        # A keystone: one chunky block at the crown, proud of the wall.
        if detail >= 1:
            trim.obox(c.xy, tt, n3, 0.16, 0.09, oz1 - 0.12, oz1 + 0.34, M["trim"], r=0.03, top_scale=1.12)
    if okind in ("sash", "jal"):
        if detail >= 1:
            # The sill: a chunky block standing 12 cm out, reaching 10 cm into the wall.
            sill = rounded_rect(w / 2 + 0.12, 0.12, 0.03)
            pts = [(c.x + tt.x * x + n3.x * (y + 0.02), c.y + tt.y * x + n3.y * (y + 0.02)) for x, y in sill]
            trim.prism(pts, oz0 - 0.13, oz0 + 0.03, M["trim"])
        if detail >= 2:
            # The frame: a cream ring in the recess, penetrating the reveals by 2 cm.
            fw = 0.09
            top = oz1 - (w / 2 if arch else 0)
            outer = [(-w / 2 - 0.02, oz0 - 0.02), (w / 2 + 0.02, oz0 - 0.02), (w / 2 + 0.02, top + 0.02), (-w / 2 - 0.02, top + 0.02)]
            inner = [(-w / 2 + fw, oz0 + fw), (w / 2 - fw, oz0 + fw), (w / 2 - fw, top - fw), (-w / 2 + fw, top - fw)]
            ring_prism(trim, outer, inner, c, tt, n3, -0.10, -0.17, M["frame"])
            if rng.random() < style.ac and oz1 - oz0 > 1.2:
                aircon(fit, M, c, tt, n3, w, oz0, rng)
    elif okind == "door":
        # A step out front, reaching into the recess, and a heavy lintel block over the door.
        step = rounded_rect(w / 2 + 0.35, 0.45, 0.05)
        pts = [(c.x + tt.x * x + n3.x * (y + 0.3), c.y + tt.y * x + n3.y * (y + 0.3)) for x, y in step]
        trim.prism(pts, GROUND - 0.06, oz0 + 0.02, M["step"])
        if detail >= 1:
            trim.obox(c, tt, n3, w / 2 + 0.3, 0.1, oz1 + 0.02, oz1 + 0.42, M["trim"], r=0.04)
    elif okind == "arcade" and detail >= 1:
        # A door on the loggia's back wall, 3 cm proud of it.
        back = c.xy - n3.xy * depth
        dw, dh = min(1.5, w * 0.55), min(2.5, oz1 - oz0 - 0.6)
        pts = [(back.x + tt.x * x + n3.x * y, back.y + tt.y * x + n3.y * y) for x, y in rounded_rect(dw / 2, 0.04, 0.01)]
        faces = fit.prism(pts, oz0 - 0.02, oz0 + dh, M["door"])
        o = Vector((back.x, back.y, oz0)) - tt * (dw / 2)
        op = fit.opening(o, tt, dw, dh)
        for f in faces:
            f.normal_update()
            if Vector((f.normal.x, f.normal.y, 0)).dot(n3) > 0.9:
                fit.tag([f], mode=2, op=op)


def ring_prism(buf, outer, inner, c, tt, n3, d0, d1, mat):
    """A flat ring (outer and inner (u, z) loops, 4 points each) extruded between depths."""
    bm = buf.bm
    def P(u, z, d):
        return Vector((c.x + tt.x * u + n3.x * d, c.y + tt.y * u + n3.y * d, z))
    vo0 = [bm.verts.new(P(u, z, d0)) for u, z in outer]
    vi0 = [bm.verts.new(P(u, z, d0)) for u, z in inner]
    vo1 = [bm.verts.new(P(u, z, d1)) for u, z in outer]
    vi1 = [bm.verts.new(P(u, z, d1)) for u, z in inner]
    faces = []
    for k in range(4):
        a, b = k, (k + 1) % 4
        faces.append(bm.faces.new((vo0[a], vo0[b], vi0[b], vi0[a])))
        faces.append(bm.faces.new((vo1[b], vo1[a], vi1[a], vi1[b])))
        faces.append(bm.faces.new((vo0[b], vo0[a], vo1[a], vo1[b])))
        faces.append(bm.faces.new((vi0[a], vi0[b], vi1[b], vi1[a])))
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    buf.tag(faces, mat=mat)


def aircon(fit, M, c, tt, n3, w, oz0, rng):
    """A window aircon: a rounded casing sitting in the bottom of the window, standing 0.3 m out,
    with a recessed louvred front panel."""
    aw = min(0.66, w - 0.2)
    h = 0.44
    off = rng.uniform(-0.1, 0.1) * (w - aw)
    cc = c.xy + tt.xy * off
    body = fit.obox(cc - n3.xy * 0.02, tt, n3, aw / 2, 0.3, oz0 + 0.03, oz0 + 0.03 + h, M["ac"], r=0.05)
    front = cc + n3.xy * 0.27
    pts = [(front.x + tt.x * x + n3.x * y, front.y + tt.y * x + n3.y * y) for x, y in rounded_rect(aw / 2 - 0.05, 0.035, 0.02)]
    faces = fit.prism(pts, oz0 + 0.08, oz0 + h - 0.02, M["ac_front"])
    o = Vector((front.x, front.y, oz0 + 0.08)) - tt * (aw / 2 - 0.05)
    op = fit.opening(o, tt, aw - 0.1, h - 0.1)
    for f in faces:
        f.normal_update()
        if Vector((f.normal.x, f.normal.y, 0)).dot(n3) > 0.9:
            fit.tag([f], mode=2, op=op)
        else:
            fit.tag([f], mat=M["ac"])


def plinth(trim, M, base, tt, n3, L, gaps, z0, zp):
    """The plinth along one facade, broken at arcade and door openings. At the corners it runs
    0.14 m past the facade end, so the neighbouring runs cross and penetrate (no shared plane)."""
    runs, a = [], -0.14
    for g0, g1 in sorted(gaps):
        if g0 - a > 0.2:
            runs.append((a, g0))
        a = g1
    if L + 0.14 - a > 0.2:
        runs.append((a, L + 0.14))
    # Short runs between arcade arches read as loose tombstones (review v2): piers go without.
    runs = [(a, b) for a, b in runs if b - a > 1.6]
    prof = [(-0.04, z0), (0.13, z0), (0.13, zp - 0.1), (0.07, zp), (-0.04, zp)]
    for u0, u1 in runs:
        rings = []
        for u in (u0, u1):
            p = base + tt * u
            rings.append([Vector((p.x + n3.x * o, p.y + n3.y * o, z)) for o, z in prof])
        trim.loft(rings, M["plinth"])


def pilasters(trim, style, M, base, tt, n3, bays, floors, zp, eave_z, levels, detail, kind):
    if not bays or detail < 1:
        return
    edges = sorted({round(a, 3) for a, _ in bays} | {round(bays[-1][1], 3)})
    if style.pilasters == "giant" and kind == "street" and levels > 1:
        # Giant half-round columns over the upper storeys, on a pedestal at the first floor.
        z_bot, z_top = floors[1] - 0.1, eave_z - 0.35
        for u in edges[1:-1]:
            p = base + tt * u + n3 * 0.32
            trim.obox(p.xy, tt, n3, 0.62, 0.5, z_bot - 0.6, z_bot + 0.05, M["trim"], r=0.05)
            path = [Vector((p.x, p.y, z_bot)), Vector((p.x, p.y, z_top))]
            trim.tube(path, 0.5, M["trim"], sides=16)
            trim.obox(p.xy, tt, n3, 0.66, 0.55, z_top - 0.05, z_top + 0.4, M["trim"], r=0.05)
        return
    if style.pilasters == "bays" and kind in ("street", "court"):
        z_bot = floors[1] if (style.rustic and levels > 1) else zp
        for u in edges:
            p = base + tt * u + n3 * 0.07
            trim.obox(p.xy, tt, n3, 0.3, 0.09, z_bot - 0.05, eave_z - 0.3, M["trim"], r=0.03)
            trim.obox(p.xy, tt, n3, 0.38, 0.14, eave_z - 0.55, eave_z - 0.25, M["trim"], r=0.03)
            trim.obox(p.xy, tt, n3, 0.37, 0.13, z_bot - 0.05, z_bot + 0.3, M["trim"], r=0.03)


def miters(loop):
    """Per-vertex miter vectors (outward offset of 1 m) for a loop with the region on its left."""
    n = len(loop)
    out = []
    for k in range(n):
        a, b, c = v2(loop[k - 1]), v2(loop[k]), v2(loop[(k + 1) % n])
        t0, t1 = (b - a).normalized(), (c - b).normalized()
        n0, n1 = Vector((t0.y, -t0.x)), Vector((t1.y, -t1.x))
        m = (n0 + n1) / max(0.2, 1 + n0.dot(n1))
        out.append(m)
    return out


def sweep(buf, loop, prof, mat):
    """A (offset, z) profile swept round a closed loop with mitred corners."""
    ms = miters(loop)
    rings = []
    for p, m in zip(loop, ms):
        rings.append([Vector((p[0] + m.x * o, p[1] + m.y * o, z)) for o, z in prof])
    faces = buf.loft(rings, mat, cap=False, closed_path=True)
    return faces


def sweeps(trim, style, M, loop, zp, floors, eave_z, top_z, z0, detail):
    # String courses at every upper floor: a chunky rounded band, 9 cm proud.
    if style.string:
        for k, f in enumerate(floors[1:]):
            h = 0.32 if k == 0 else 0.22
            prof = fillet([(-0.04, f - h / 2), (0.09, f - h / 2 + 0.03), (0.11, f + h / 2 - 0.05), (-0.04, f + h / 2)], 0.03, 2)
            sweep(trim, loop, prof, M["trim"])
    if style.attic:
        # The cornice at the eave line, then the attic wall and its coping.
        prof = fillet([(-0.05, eave_z - 0.45), (0.12, eave_z - 0.42), (0.34, eave_z - 0.12), (0.42, eave_z + 0.02),
                       (0.42, eave_z + 0.18), (-0.05, eave_z + 0.18)], 0.04, 2)
        sweep(trim, loop, prof, M["trim"])
        cop = fillet([(-0.42, top_z - 0.9), (-0.30, top_z - 0.9), (-0.30, top_z - 0.12), (0.16, top_z - 0.1),
                      (0.16, top_z + 0.1), (-0.42, top_z + 0.1)], 0.04, 2)
        sweep(trim, loop, cop, M["trim"])
    else:
        # Under deep eaves: a plainer cornice whose top follows the roof's underside, 3 cm into it.
        tn = math.tan(math.radians(style.pitch))
        under = lambda o: eave_z + 0.25 - tn * o + 0.03
        prof = fillet([(-0.05, eave_z - 0.4), (0.10, eave_z - 0.38), (0.22, eave_z - 0.16), (0.30, eave_z - 0.1),
                       (0.30, under(0.30)), (-0.05, under(-0.05))], 0.035, 2)
        sweep(trim, loop, prof, M["trim"])


def quoins(trim, M, loop, zp, eave_z, outer):
    """Rusticated corner blocks at every convex corner: alternating long and short, 6 cm proud."""
    n = len(loop)
    for k in range(n):
        a, b, c = v2(loop[k - 1]), v2(loop[k]), v2(loop[(k + 1) % n])
        t0, t1 = (b - a).normalized(), (c - b).normalized()
        if t0.x * t1.y - t0.y * t1.x <= 0:
            continue           # a reflex corner: no quoins
        n0, n1 = Vector((t0.y, -t0.x)), Vector((t1.y, -t1.x))
        z, i = zp + 0.02, 0
        # Chunky courses, 0.56 m tall: at 0.4 m the corners read as fine stitching (review v10).
        while z + 0.58 < eave_z - 0.45:
            la = 0.95 if i % 2 == 0 else 0.6
            lb = 0.6 if i % 2 == 0 else 0.95
            # Two blocks meeting at the corner, each running past it into the other.
            for tt, nn, ln in ((-t0, n0, la), (t1, n1, lb)):
                p0 = b + nn * 0.06 - tt * 0.06
                p1 = b + tt * ln + nn * 0.06
                q1 = b + tt * ln - nn * 0.03
                q0 = b - nn * 0.03 - tt * 0.06
                trim.prism([tuple(p0), tuple(p1), tuple(q1), tuple(q0)], z, z + 0.56, M["trim"])
            z += 0.63
            i += 1


# ------------------------------------------------------------------ roof

def roof(col, pl, style, M, eave_z, top_z, ctx, name, origin, rng, detail, gable=None, idx=0):
    buf = Buf(f"{name} roof")
    tn = math.tan(math.radians(style.pitch))
    ang, cx, cy = pl["frame"]
    if not pl["orth"] or style.roof == "flat":
        # A flat roof behind the parapet: one slab just below the coping, and a water tank.
        loop = pl["loops"][0]
        bm = buf.bm
        vs = [bm.verts.new((x, y, top_z - 0.5)) for x, y in loop]
        f = bm.faces.new(vs)
        f.normal_update()
        if f.normal.z < 0:
            f.normal_flip()
        buf.tag([f], mat=M["flatroof"])
        xs, ys = [p[0] for p in loop], [p[1] for p in loop]
        tx, ty = cx + 0.2 * (max(xs) - cx), cy + 0.2 * (max(ys) - cy)
        buf.prism(fillet([(tx - 1.4, ty - 1.4), (tx + 1.4, ty - 1.4), (tx + 1.4, ty + 1.4), (tx - 1.4, ty + 1.4)], 0.3),
                  top_z - 0.55, top_z + 1.6, M["tank"])
        buf.prism([(tx - 3 + x, ty - 5 + y) for x, y in rounded_rect(1.6, 1.3, 0.1)], top_z - 0.55, top_z + 2.2, M["wall"])
        return buf.finish(col, ctx, bevel=0.03, weld=False, origin=origin)
    e = style.eave if not style.attic else -0.2
    z_base = (eave_z + 0.25 + 0.24) if not style.attic else (top_z - 0.35 + tn * (-e))
    segs = pl["segs"]
    cells = pl["cells"]
    lx = [p[0] for l in pl["local_loops"] for p in l]
    ly = [p[1] for l in pl["local_loops"] for p in l]
    step = 0.5
    pad = max(e, 0) + step
    gx = np.arange(min(lx) - pad, max(lx) + pad + 1e-6, step)
    gy = np.arange(min(ly) - pad, max(ly) + pad + 1e-6, step)
    PX, PY = np.meshgrid(gx, gy, indexing="ij")
    d = linf_segments(PX, PY, segs)
    def inside_of(X, Y, D):
        out = np.zeros(X.shape, bool)
        fx, fy = np.floor(X + 1e-4).astype(int), np.floor(Y + 1e-4).astype(int)
        for idx in zip(*np.nonzero(D > 1e-6)):
            out[idx] = (fx[idx], fy[idx]) in cells
        return out

    s = np.where(inside_of(PX, PY, d), d, -d)
    CX, CY = (PX[:-1, :-1] + PX[1:, 1:]) / 2, (PY[:-1, :-1] + PY[1:, 1:]) / 2
    dc = linf_segments(CX, CY, segs)
    sc = np.where(inside_of(CX, CY, dc), dc, -dc)
    if gable is not None and len(pl["local_loops"]) == 1 and len(pl["local_loops"][0]) == 4:
        # A GABLE toward `gable` (the entrance pavilions): the height depends only on the
        # distance to the two side walls, and the roof runs out past the front and back walls.
        ca_, sa_ = math.cos(-ang), math.sin(-ang)
        lx = gable.x * ca_ - gable.y * sa_
        ly = gable.x * sa_ + gable.y * ca_
        x0, x1, y0, y1 = min(lx_ := [p[0] for p in pl["local_loops"][0]]), max(lx_), min(ly_ := [p[1] for p in pl["local_loops"][0]]), max(ly_)
        e_ = max(e, 0)

        def gable_s(X, Y):
            if abs(ly) > abs(lx):      # ridge runs along local y
                v = np.minimum(X - x0, x1 - X)
                out = (Y < y0 - e_ - 1e-6) | (Y > y1 + e_ + 1e-6)
            else:
                v = np.minimum(Y - y0, y1 - Y)
                out = (X < x0 - e_ - 1e-6) | (X > x1 + e_ + 1e-6)
            return np.where(out, -1e3, v)

        s, sc = gable_s(PX, PY), gable_s(CX, CY)
    smin = -e
    # Deep plans would carry towering roofs at the style's pitch; the real campus roofs stay low,
    # so the ridge rises at most RISE_MAX above the eave line (the pitch flattens instead).
    tn = min(tn, RISE_MAX / max(1.0, float(s.max())))
    z_base = (eave_z + 0.25 + 0.24) if not style.attic else (top_z - 0.35 + tn * (-e))
    ctx.roof_tn, ctx.roof_z0 = tn, z_base
    z = z_base + tn * s
    ca, sa = math.cos(ang), math.sin(ang)
    k_ = pl["shrink"]
    W = lambda x, y, zz: Vector((cx + k_ * (x * ca - y * sa), cy + k_ * (x * sa + y * ca), zz))
    bm = buf.bm
    vid = {}

    def vert(i, j):
        if (i, j) not in vid:
            vid[(i, j)] = bm.verts.new(W(gx[i], gy[j], z[i, j]))
        return vid[(i, j)]

    faces = []
    for i in range(len(gx) - 1):
        for j in range(len(gy) - 1):
            if min(s[i, j], s[i + 1, j], s[i, j + 1], s[i + 1, j + 1]) < smin - 1e-6:
                continue
            if sc[i, j] < smin - 1e-6:
                continue
            zc = z_base + tn * sc[i, j]
            a, b, c, dd = vert(i, j), vert(i + 1, j), vert(i + 1, j + 1), vert(i, j + 1)
            if abs(zc - (z[i, j] + z[i + 1, j + 1]) / 2) < 1e-5:
                faces += [bm.faces.new((a, b, c)), bm.faces.new((a, c, dd))]
            else:
                faces += [bm.faces.new((a, b, dd)), bm.faces.new((b, c, dd))]
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    for f in faces:
        if f.normal.z < 0:
            f.normal_flip()
    bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(0.5), verts=bm.verts, edges=bm.edges)
    # Three tones of the same roof, picked per building, so the campus from above is not one
    # flat sheet of red: as-painted, sun-bleached and weathered darker.
    variant = ROOF_VARIANTS[idx % len(ROOF_VARIANTS)]
    roof_mat = M["roof"] + variant[0]
    if not bpy.data.materials.get(roof_mat):
        material(roof_mat, M["roof"], variant[1], (), normal=0.5, base=variant[2])
    buf.tag(bm.faces, mat=roof_mat, mode=1)
    # Ridge and hip caps: a chunky rounded roll along every convex crease, a shade darker, so
    # the roofs keep their drawing from the air (review v6: big roofs read as flat red sheets).
    caps = Buf(f"{name} roof caps")
    cap_mat = M["roof"] + "_cap"
    if not bpy.data.materials.get(cap_mat):
        material(cap_mat, M["roof"], "cfc6c0", (), normal=0.3, base="ffffff")
    for ed in bm.edges:
        if len(ed.link_faces) != 2 or ed.calc_length() < 0.6:
            continue
        f1, f2 = ed.link_faces
        mid = (ed.verts[0].co + ed.verts[1].co) / 2
        if (f1.calc_center_median() - mid).dot(f2.normal) > -1e-3:
            continue          # a valley or a flat seam: no cap
        lift = (f1.normal + f2.normal).normalized() * 0.03
        a_, b_ = ed.verts[0].co + lift, ed.verts[1].co + lift
        caps.tube([a_, b_], 0.13, cap_mat, sides=8)
    if caps.bm.faces:
        caps.finish(col, ctx, bevel=0.02, weld=False, origin=origin)
    else:
        caps.bm.free()
    # Slot order for Solidify: roof 0, fascia 1 (the rim), soffit 2 (the shell underneath).
    buf.mi(M["fascia"])
    buf.mi(M["soffit"])
    obj = buf.finish(col, ctx, bevel=0, weld=False, origin=origin)
    sol = obj.modifiers.new("Solidify", "SOLIDIFY")
    sol.thickness, sol.offset = 0.24, -1.0
    sol.use_even_offset = True
    sol.material_offset, sol.material_offset_rim = 2, 1
    bev = obj.modifiers.new("Bevel", "BEVEL")
    bev.width, bev.segments = 0.03, 1
    bev.limit_method, bev.angle_limit = "ANGLE", math.radians(30)
    bev.harden_normals, bev.use_clamp_overlap = True, True
    return obj


# ------------------------------------------------------------------ layout and review

def load_layout():
    return BO.sightline_override(json.loads((SOURCE / "osm_layout.json").read_text(encoding="utf-8")))


def in_scope(layout):
    """Every building west of the Taft corridor within EXTENT of the court, except Rizal Hall
    and the OSM blocks inside it."""
    hall = next(b for b in layout["buildings"] if "Rizal Hall" in b["name"])
    out = []
    for k, b in enumerate(layout["buildings"]):
        xs = [p[0] for p in b["poly"]]
        ys = [p[1] for p in b["poly"]]
        cx, cy = sum(xs) / len(xs), sum(ys) / len(ys)
        if cx >= -BO.PAVE_OUT or max(abs(cx), abs(cy)) > EXTENT or b is hall:
            continue
        # Share of the footprint that lies inside Rizal Hall's outline, sampled on a 1 m grid.
        pts = [(x + 0.5, y + 0.5) for x in range(int(min(xs)), int(max(xs))) for y in range(int(min(ys)), int(max(ys)))
               if point_in(x + 0.5, y + 0.5, b["poly"])]
        overlap = sum(point_in(x, y, hall["poly"]) for x, y in pts) / max(1, len(pts))
        hx = [p[0] for p in hall["poly"]]
        hy = [p[1] for p in hall["poly"]]
        annex = min(xs) >= min(hx) - 2 and max(xs) <= min(hx) + 16 and min(ys) >= min(hy) - 2 and max(ys) <= max(hy) + 8
        if overlap > 0.25 or annex:
            print("[heritage] skipped (inside Rizal Hall):", k, b["name"])
            continue
        out.append((k, b, math.hypot(max(0, -max(xs) - 0), max(0, abs(cy) - 30))))
    return out


def assemble(only=None):
    layout = load_layout()
    root = bpy.data.collections.new("heritage west of Taft")
    bpy.context.scene.collection.children.link(root)
    made = []
    for k, b, dist in in_scope(layout):
        if only and only not in (b["name"] or f"campus block {k}"):
            continue
        near = min(math.hypot(x, y) for x, y in b["poly"])
        detail = 2 if near < 60 else (1 if near < 120 else 0)
        name = b["name"] or f"campus block {k}"
        col = bpy.data.collections.new(name)
        root.children.link(col)
        building(col, b, k, detail)
        made.append((name, detail))
        print(f"[heritage] {name}: detail {detail}")
    return layout, made


def review_set(layout):
    """Stand-ins so the renders read in place: ground, Taft, the west fence line, stand-in trees
    from the OSM tree list, grey blocks for the other kits' buildings, and the linked LRT
    guideway. None of it is part of this kit."""
    col = bpy.data.collections.new("review stand-ins (not part of the kit)")
    bpy.context.scene.collection.children.link(col)

    def flatmat(n, c):
        m = bpy.data.materials.get(n) or bpy.data.materials.new(n)
        if m.node_tree is None:
            m.use_nodes = True
        m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*srgb_lin(c), 1)
        m.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.9
        return m

    def slab(n, x0, x1, y0, y1, top, c, thick=0.4):
        me = bpy.data.meshes.new(n)
        bm = bmesh.new()
        bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation(((x0 + x1) / 2, (y0 + y1) / 2, top - thick / 2))
                              @ Matrix.Diagonal((x1 - x0, y1 - y0, thick, 1)))
        bm.to_mesh(me)
        bm.free()
        me.materials.append(flatmat(n, c))
        col.objects.link(bpy.data.objects.new(n, me))

    slab("stand-in road", -6.65, 6.65, -300, 300, 0.0, "4a4a4e")
    for s in (-1, 1):
        slab("stand-in kerb", *sorted((s * 6.65, s * 7.0)), -300, 300, 0.15, "e8e8e2")
        slab("stand-in pavement", *sorted((s * 7.0, s * 11.0)), -300, 300, 0.212, "b2a998")
    slab("stand-in campus ground", -300, -11.0, -300, 300, GROUND, "a8a58f")
    slab("stand-in east lots", 11.0, 300, -300, 300, GROUND, "b8b2a4")
    # Padre Faura, as a dark strip, so the corner reads.
    for r in layout["roads"]:
        if "Padre Faura" in r["name"]:
            for (x0, y0), (x1, y1) in zip(r["line"], r["line"][1:]):
                if max(x0, x1) < -11:
                    slab("stand-in Padre Faura", min(x0, x1), max(x0, x1), min(y0, y1) - 5, max(y0, y1) + 5, GROUND + 0.01, "55555a", 0.2)
    # The PGH fence on x = -11: black iron posts and rails (the street-props kit owns the real one).
    fm = flatmat("stand-in fence", "22282a")
    me = bpy.data.meshes.new("stand-in fence")
    bm = bmesh.new()
    y = -150.0
    while y < 32:
        bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation((-11.1, y, GROUND + 0.9)) @ Matrix.Diagonal((0.08, 0.08, 1.8, 1)))
        y += 2.4
    for zz in (0.35, 1.7):
        bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation((-11.1, -59, GROUND + zz)) @ Matrix.Diagonal((0.05, 182, 0.06, 1)))
    bm.to_mesh(me)
    bm.free()
    me.materials.append(fm)
    col.objects.link(bpy.data.objects.new("stand-in fence", me))
    # Trees: the mapped ones plus the blockout's scatter over lawns and parking.
    tm, bk = flatmat("stand-in canopy", "4f7a37"), flatmat("stand-in trunk", "5c4431")
    rng = random.Random(9)
    spots = [tuple(t) for t in layout["trees"]]
    for a in layout["areas"]:
        if a["kind"] not in ("village_green", "parking"):
            continue
        xs, ys = [p[0] for p in a["poly"]], [p[1] for p in a["poly"]]
        for _ in range(int((max(xs) - min(xs)) * (max(ys) - min(ys)) / 140)):
            x, y = rng.uniform(min(xs), max(xs)), rng.uniform(min(ys), max(ys))
            if point_in(x, y, a["poly"]):
                spots.append((x, y))
    polys = [b["poly"] for b in layout["buildings"]]
    me = bpy.data.meshes.new("stand-in trees")
    bm = bmesh.new()
    for x, y in spots:
        if x > -12.5 or max(abs(x), abs(y)) > 240 or any(point_in(x, y, p) for p in polys):
            continue
        h = 5 + rng.random() * 4
        bmesh.ops.create_cone(bm, cap_ends=True, segments=8, radius1=0.28, radius2=0.2, depth=h,
                              matrix=Matrix.Translation((x, y, GROUND + h / 2)))
        for c in range(3):
            r = 2.2 + rng.random() * 1.4
            ox, oy = rng.uniform(-1.5, 1.5), rng.uniform(-1.5, 1.5)
            res = bmesh.ops.create_icosphere(bm, subdivisions=2, radius=r,
                                             matrix=Matrix.Translation((x + ox, y + oy, GROUND + h + rng.uniform(0.3, 1.8))) @ Matrix.Diagonal((1, 1, 0.75, 1)))
            for v in res["verts"]:
                for f in v.link_faces:
                    f.material_index = 1
    bm.to_mesh(me)
    bm.free()
    me.materials.append(bk)
    me.materials.append(tm)
    for p in me.polygons:
        p.use_smooth = p.material_index == 1
    col.objects.link(bpy.data.objects.new("stand-in trees", me))
    # The other kits' buildings as plain grey blocks (Rizal Hall, the east side).
    gm = flatmat("stand-in building", "bdbab2")
    me = bpy.data.meshes.new("stand-in other buildings")
    bm = bmesh.new()
    scope = {id(b) for _, b, _ in in_scope(layout)}
    for b in layout["buildings"]:
        xs = [p[0] for p in b["poly"]]
        ys = [p[1] for p in b["poly"]]
        cx, cy = sum(xs) / len(xs), sum(ys) / len(ys)
        if id(b) in scope or max(abs(cx), abs(cy)) > 260:
            continue
        poly = [(max(x, 11.3) if cx > 0 else x, y) for x, y in b["poly"]]
        h = (b["levels"] or 3) * 3.3
        vs = [bm.verts.new((x, y, GROUND - 0.1)) for x, y in poly]
        try:
            f = bm.faces.new(vs)
        except ValueError:
            continue
        f.normal_update()
        if f.normal.z < 0:
            f.normal_flip()
        ext = bmesh.ops.extrude_face_region(bm, geom=[f])
        bmesh.ops.translate(bm, vec=(0, 0, h), verts=[v for v in ext["geom"] if isinstance(v, bmesh.types.BMVert)])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me)
    bm.free()
    me.materials.append(gm)
    col.objects.link(bpy.data.objects.new("stand-in other buildings", me))
    # The LRT-1 guideway kit, linked for context.
    lib = SOURCE / "lrt_kit.blend"
    if lib.exists():
        with bpy.data.libraries.load(str(lib), link=True) as (src, dst):
            dst.collections = [c for c in src.collections if c == "guideway over the court"]
        for c in dst.collections:
            if c is not None:
                inst = bpy.data.objects.new("LRT-1 guideway (linked)", None)
                inst.instance_type, inst.instance_collection = "COLLECTION", c
                col.objects.link(inst)
    return col


def lighting():
    scene = bpy.context.scene
    world = bpy.data.worlds.new("sky")
    scene.world = world
    if world.node_tree is None:
        world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (0.74, 0.80, 0.90, 1)
    bg.inputs["Strength"].default_value = 0.7
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy, sun.data.angle, sun.data.color = 3.8, math.radians(3), (1.0, 0.88, 0.72)
    sun.rotation_euler = Vector((0.80, -0.25, -0.55)).normalized().to_track_quat("-Z", "Y").to_euler()
    scene.collection.objects.link(sun)
    scene.view_settings.view_transform = "AgX"
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type == "VIEW_3D":
                for space in area.spaces:
                    if space.type == "VIEW_3D":
                        space.shading.type = "MATERIAL"
                        space.clip_end = 3000


EYE_LENS = 18 / math.tan(math.radians(95 / 2))
PAVE = BO.PAVE_TOP + 1.25
SHOTS = [
    # Game's eye, from inside the play area.
    ("court_west", Vector((8.5, -2.0, PAVE)), Vector((-60, -8, 7)), EYE_LENS),
    ("pavement_west", Vector((-9.5, 6.0, PAVE)), Vector((-60, -20, 6)), EYE_LENS),
    ("spawn_northwest", Vector((0.0, -9.0, 1.25)), Vector((-40, 60, 9)), EYE_LENS),
    ("north_corner", Vector((-8.5, 15.5, PAVE)), Vector((-45, 55, 10)), EYE_LENS),
    # Close-ups.
    ("nurses_close", Vector((-15.0, -38.0, 3.0)), Vector((-27, -24, 7)), 26),
    ("court_close", Vector((-12.0, 34.0, 2.0)), Vector((-24, 50, 9)), 24),
    ("opd_arcade", Vector((-44.0, 8.0, 2.2)), Vector((-56, 12, 3.5)), 24),
    ("eave_soffit", Vector((-13.0, -47.0, 1.9)), Vector((-26.0, -36.0, 13.0)), 24),
    ("east_pavement_nw", Vector((9.5, -14.0, PAVE)), Vector((-50, 40, 8)), EYE_LENS),
    # Aerials.
    ("aerial_campus", Vector((40.0, -120.0, 90.0)), Vector((-80, -20, 0)), 26),
    ("aerial_corner", Vector((8.0, -25.0, 42.0)), Vector((-45, 60, 5)), 26),
]


def building_shots():
    """Shots placed from the OPD's own plan: standing in its courtyard, and under its eave on
    the east front looking up at the soffit."""
    layout = load_layout()
    b = next(b for b in layout["buildings"] if "Out-Patient" in b["name"])
    pl = plan(b)
    out = []
    holes = [l for l in pl["loops"] if polygon_signed(l) < 0]
    if holes:
        h = max(holes, key=lambda l: -polygon_signed(l))
        cx = sum(p[0] for p in h) / len(h)
        cy = sum(p[1] for p in h) / len(h)
        xs = [p[0] for p in h]
        out.append(("opd_courtyard", Vector((max(xs) - 3, cy - 4, GROUND + 1.6)), Vector((min(xs), cy + 6, 5)), EYE_LENS))
    outer = max(pl["loops"], key=polygon_signed)
    best = None
    for k in range(len(outer)):
        p0, p1 = v2(outer[k]), v2(outer[(k + 1) % len(outer)])
        t = p1 - p0
        nrm = Vector((t.y, -t.x)).normalized()
        if nrm.x > 0.9 and (best is None or t.length > best[0]):
            best = (t.length, p0, p1, nrm)
    if best:
        _, p0, p1, nrm = best
        m = (p0 + p1) / 2
        out.append(("opd_front", Vector((m.x + nrm.x * 16, m.y + nrm.y * 16 - 10, GROUND + 1.7)), Vector((m.x, m.y, 4.5)), EYE_LENS))
    return out


def preview(version, shots=None):
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 1000
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    cam.data.clip_end = 3000
    scene.collection.objects.link(cam)
    scene.camera = cam
    shots_all = list(SHOTS) + building_shots()
    for name, pos, tgt, lens in shots_all:
        if shots and name not in shots:
            continue
        cam.location, cam.data.lens = pos, lens
        cam.rotation_euler = (tgt - pos).to_track_quat("-Z", "Y").to_euler()
        path = PREVIEWS / f"heritage_{name}_v{version}.png"
        if path.exists():
            print("[heritage] exists, not overwriting:", path)
            continue
        scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        print("[heritage] preview", path)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
    only = argv[argv.index("--only") + 1] if "--only" in argv else None
    shots = argv[argv.index("--shots") + 1].split(",") if "--shots" in argv else None
    bpy.ops.wm.read_factory_settings(use_empty=True)
    layout, made = assemble(only)
    out = SOURCE / ("heritage.blend" if not only else "heritage_partial.blend")
    SOURCE.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(out), compress=True)
    backup = out.with_suffix(".blend1")
    if backup.exists():
        backup.unlink()
    print("[heritage] saved", out, len(made), "buildings")
    if version:
        review_set(layout)
        lighting()
        preview(version, shots)


if __name__ == "__main__":
    main()
