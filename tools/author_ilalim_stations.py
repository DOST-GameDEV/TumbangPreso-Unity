"""Model the two LRT-1 stations that end the view along Taft, and the street-end building rows
behind them (ILALIM-1.3, stations kit).

  py -3 tools/author_ilalim_textures_stations.py [--sheet N]    # paint the stn_ textures first
  blender -b --python tools/author_ilalim_stations.py -- [--preview N] [--only a,b]

Writes ArtSource/ilalim/stations.blend. With --preview it writes versioned close-up renders
(against plain stand-ins and the linked guideway) to Logs/ilalim-blender/stn_<shot>_vN.png; an
existing file is never overwritten. The views from the court come from the city
(tools/author_ilalim_city.py links this kit through OPTIONAL_KITS).

THE PROBLEM. Owner, 2026-09-30, looking north along Taft from the court: "lrt way ending is
visible from the play area. need to figure out a way to end the view". The guideway
(tools/author_ilalim_lrt.py) stops at the pier rows y = +/-94 and the street runs on into an
empty horizon. He chose "Stations + haze": the real LRT-1 stations pulled in close, UNITED
NATIONS to the north and PEDRO GIL to the south, a deliberate departure from the true distances
in the spirit of the Rizal Hall sightline override; and behind each a row of buildings where
Taft "bends". The haze is the lead's, in the city file.

THE STATIONS. One station is modelled once, in its own frame, and placed twice as linked
duplicates: north with its anchor at (0, 94, 0), south rotated 180 degrees about the court
centre with its anchor at (0, -94, 0). Local +Y runs AWAY from the court. Only the name boards
differ (stn_name_un, stn_name_pgil); LRT-1's stations of this era were one standard design.

  the joint     The last guideway span ends at local y -0.03 (world 93.97) on the pier at 94. The
                station's trackway starts at local 0.03, bearing on the north half of the same
                pier cap exactly as a span would, so the deck runs straight in with the
                guideway's own 6 cm expansion joint and nothing overlaps it. The rails, sleepers
                and ballast keep the guideway's profile (rail head 9.19, tracks at x +/-2.35).
                A wider station end beam (x +/-8.3) sits behind the pier cap and carries the
                platforms' first metre. The guideway's parapets end at local -0.06; the platforms
                start at 0.30, clear of the foot of the guideway's gantry on that pier (it
                reaches 0.25).
  length        local 0.03..91.2 (north: y 94.03..185.2; south: y -94.03..-185.2), roof to 91.5.
  bents         twin columns in the lanes at x +/-4.45, the guideway's pier legs (1.44 m square,
                lrt_pier, so they belong on NearFade like every pier) at local 25, 50, 75 and 90,
                the guideway's 25 m rhythm. The street kit's median collars at y +/-119 and
                +/-144 already surround the 25 and 50 columns. 25 and 50 carry the concourse on a
                low hammerhead (underside 4.2 over the columns); 75 and 90 carry the platforms on
                a high one (7.15). The lanes at x +/-1.9 and both kerbs stay clear.
  platforms     side platforms x +/-3.80..8.35, top 10.10 (0.91 above the rail head), the
                guideway's grey fascia on their outer face and a mustard safety strip.
  platform walls a solid cream dado to 11.2, then big LOUVRED SCREENS (the slats are painted,
                stn_louvre) between stout posts every 6 m, a head band to the roof. The platform
                edge stays within x +/-8.35 because the street kit's pole crossarms and cables
                reach in to x 8.5 at z 7.7..8.7 near both ends.
  roof          a chunky BARREL ROOF, eaves 13.4 at x +/-9.9, crown 17.6, with a raised louvred
                monitor along the crown (top 18.5), arched end ribs and eave beams. Green painted
                standing seam (stn_roof); the underside is the guideway's dark soffit.
  ends          each end is closed above the platforms by louvred end walls and by a TYMPANUM
                filling the arch above 14.0, carrying the 9 m name board at 15.05..16.35 (it
                clears the guideway gantry at the pier, top 14.94). The track opening at the near
                end is x +/-3.9 up to 14.0 (the contact wires are at 13.74). The far end is
                closed across the track too: the flyby train never reaches the stations
                (LrtTrainFlyby runs it between z -48 and +48), and a closed end means the view
                through the station ends in wall, not sky.
  concourse     under the platforms at local 22..58, x +/-9.3, a closed box from 5.0 to 8.9 with
                the drawn window band (stn_windows), the base slab and a roof ledge.
  stairs        covered stairways down to BOTH pavements (x 7.0..9.2 each side), from the
                concourse's near end (local 22.3, floor 5.56) to the pavement at local 11.0,
                descending toward the court: 30 chunky steps, solid balustrade walls with a fat
                coping, a sloped green canopy on stout posts. They stop short of the street
                poles at y +/-100 and +/-109, the cables, and the cross street at y -118.8 south.
  name boards   UNITED NATIONS / UN AVENUE (north), PEDRO GIL / TAFT AVENUE (south): white on
                maroon, on both tympana and on the platform fascia each side. Place names only;
                no operator logos.

THE STREET-END ROWS. A chunky row of six buildings across the whole street width at each end, its
front at |y| 219.6..220.5 (right where the street kit's ground ends at |y| 220, so it also hides
the ground's edge), x -45..10.6, 14 m deep, on its own pavement strip (top 0.28, |y| 218..220).
Facades face the court: ground-floor shops (drawn shopfronts, stn_shops) under an upper-floor
overhang, hand-painted shop boards (stn_shopsigns, invented names), drawn windows (stn_window_a/b/c)
with chunky sills, string courses, cornices and parapets, balconies, fins, a tin gable, water
tanks. Heights 10 to 17 m. From the court eye points the station's underside (the concourse,
4.2..5.5) and the gap beside it frame these facades, so at eye height the view ends in buildings.
East of x 10.6 the east kit's buildings (the hotel at x 10.9.., y 213.., the Cathedral of Praise
to y 210, and east_bldg_24 south) already close the view; the rows stop just short of them.
The north row's west end starts past the Supreme Court fence (it ends at y 217.7).

WHAT THE LEAD MUST KNOW (mesh-overlap checked against every kit linked in the city, 2026-09-30):
  * STREET TREES. The kerb trees overhang Taft to x +/-3 at 5 to 13 m, so the station body
    runs through eleven canopies. They need culling (or moving) in the trees kit:
      north: tree_narra_1 .006 .007 .008 .010, tree_raintree_1 .003 .004;
      south: tree_narra_0 .001, tree_narra_1 .002, tree_raintree_1 .002, tree_raintree_2 .001 .002
    (the _leaves objects touch; drop each whole tree, its _wood with it).
  * MEDIAN LILIES. The 25 and 50 columns stand in the street kit's median collars at y +/-119
    and +/-144, as the piers do in theirs, but those collars are planted right through the
    column footprint: the city's lilies() should skip clumps within about 1 m of (+/-4.45,
    +/-119) and (+/-4.45, +/-144), as it skips the sign posts.
  * Meant to touch: the end beam behind each end pier's cap, the pier bearings under the
    station's girders and inner edge beam, column feet and stair feet sunk into road and
    pavement, the rows' strip over the ground's end.
  * Clear: the poles and crossarms (|x| >= 8.5), the cables, the gantries, the bus shelters
    (y 125..129 north under the concourse, -157..-162 south), the cross street at y -119..-132,
    the Edificio Amihan canopy (x >= 9.7), the Makabayan building (x >= 10.3), the hotel and
    east_bldg_24 (x >= 10.9), the Supreme Court fence (ends y 217.7), the campus blocks.

THE HOUSE STYLE (KANTO_DESIGN_GUIDE.md section 2, LAGOON_REWORK_GUIDE.md section 2): chunky
members with fillets and live Bevel modifiers (hardened normals); no two surfaces share a plane
(parts that meet penetrate by 2 to 10 cm, things resting on a surface sink into it); thin things
(louvre slats, grilles, window frames) are painted, never modelled; world UVs at 4 m (the
guideway's scale), band UVs for the louvre and window bands, 0..1 UVs for boards; and the
guideway's positional grime (grime_drips on UVGrime below each object's top, grime_splash on
UVSplash above the ground, the soot band under the platforms).

ROLE HUES: nothing near #f87020 or #0080e8. Maroon boards, bottle-green roofs, cream walls,
teal-grey glass, pastel renders kept dusty; no mid blue and no orange.
"""
import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import author_ilalim_lrt as L                                          # noqa: E402
import author_ilalim_props as P                                        # noqa: E402
from author_ilalim_lrt import fillet, rounded_rect                    # noqa: E402
from author_ilalim_props import facing                                # noqa: E402
from ilalim_antitile import anti_tile                                 # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource" / "ilalim"
TEXTURES = SOURCE / "textures"
PREVIEWS = ROOT / "Logs" / "ilalim-blender"
TILE_M = 4.0
UP = Vector((0, 0, 1))
PT = 0.212                                 # the pavement top

ANCHOR = 94.0                              # the pier row the station starts at
END = 91.2                                 # the deck's far end, local
PLAT_TOP = 10.10
PLAT_BOT = 8.55
PLAT_IN, PLAT_OUT = 3.80, 8.35
TROUGH_TOP = L.DECK_TOP - L.TROUGH         # 8.84, the trackway slab under the ballast
BENTS_LOW = (25.0, 50.0)                   # carry the concourse
BENTS_HIGH = (75.0, 90.0)                  # carry the platforms
CONC = (22.0, 58.0)                        # the concourse, local y
CONC_HALF = 9.3
CONC_FLOOR = 5.52
EAVE_X, EAVE_Z, CROWN = 9.9, 13.4, 17.6
ARCH_R = (EAVE_X ** 2 + (CROWN - EAVE_Z) ** 2) / (2 * (CROWN - EAVE_Z))
ARCH_C = CROWN - ARCH_R                    # the barrel's centre height
OPEN_TOP = 14.0                            # the track opening's head and the tympanum's foot
POSTS = [0.9 + 6.0 * k for k in range(16)]

# Stairs (east side, local; the west one is its mirror).
ST_FOOT, ST_N, ST_GO = 11.0, 30, 0.32
ST_TOP = ST_FOOT + ST_N * ST_GO            # 20.6
ST_LAND = 22.35
ST_Z = 5.56                                # the landing, 4 cm above the concourse base top
ST_RISE = (ST_Z - PT) / ST_N
ST_X0, ST_X1 = 7.25, 8.95                  # the walkable flight
ST_PITCH = (ST_Z - PT) / (ST_TOP - ST_FOOT)

ROW_FRONT = 219.6                          # the street-end rows' front line, |y|
ROW_DEPTH = 14.0
ROW_BASE = 0.28                            # their pavement strip's top

# name: (texture or None, tint or colour, roughness, metallic, grime, anti-tile)
M = {
    "stn_wall":       ("stn_wall", (0.94, 0.90, 0.80), 0.9, 0.0, True, False),
    "stn_wall_post":  ("stn_wall", (0.86, 0.83, 0.75), 0.9, 0.0, True, False),
    "stn_louvre":     ("stn_louvre", None, 0.8, 0.0, True, False),
    "stn_windows":    ("stn_windows", None, 0.6, 0.0, True, False),
    "stn_roof":       ("stn_roof", (0.36, 0.49, 0.30), 0.55, 0.2, True, False),
    "stn_floor":      ("stn_floor", None, 0.85, 0.0, False, False),
    "stn_tread":      ("stn_floor", (0.86, 0.85, 0.83), 0.85, 0.0, True, False),
    "stn_safety":     (None, (0.74, 0.62, 0.24), 0.7, 0.0, False, False),
    "stn_dark":       (None, (0.06, 0.06, 0.065), 0.8, 0.0, False, False),
    "stn_board_edge": (None, (0.26, 0.09, 0.12), 0.6, 0.1, False, False),
    "stn_name_un":    ("stn_name_un", None, 0.6, 0.0, False, False),
    "stn_name_pgil":  ("stn_name_pgil", None, 0.6, 0.0, False, False),
    # The street-end rows.
    "stn_trim":       ("stn_wall", (0.91, 0.89, 0.84), 0.9, 0.0, True, False),
    "stn_tin":        ("stn_roof", (0.62, 0.60, 0.56), 0.5, 0.3, True, False),
    "stn_tin_red":    ("stn_roof", (0.56, 0.27, 0.24), 0.55, 0.2, True, False),
    "stn_tank_black": (None, (0.12, 0.12, 0.12), 0.6, 0.0, False, False),
    "stn_tank_green": (None, (0.23, 0.36, 0.27), 0.6, 0.0, False, False),
    "stn_steel":      (None, (0.28, 0.28, 0.27), 0.6, 0.3, False, False),
    "stn_strip":      ("stn_floor", (0.80, 0.78, 0.74), 0.9, 0.0, True, False),
    "stn_window_a":   ("stn_window_a", None, 0.4, 0.0, False, False),
    "stn_window_b":   ("stn_window_b", None, 0.5, 0.0, False, False),
    "stn_window_c":   ("stn_window_c", None, 0.4, 0.0, False, False),
    "stn_shops":      ("stn_shops", None, 0.7, 0.0, False, False),
    "stn_shopsigns":  ("stn_shopsigns", None, 0.7, 0.0, False, False),
}
# The rows' paints: dusty, never saturated, never orange or blue. Each building its own.
PAINTS = {
    "rose":   (0.84, 0.58, 0.54), "sage":  (0.62, 0.74, 0.58), "ochre": (0.90, 0.76, 0.46),
    "grey":   (0.70, 0.74, 0.70), "cream": (0.93, 0.84, 0.62), "mint":  (0.60, 0.78, 0.68),
    "lilac":  (0.74, 0.64, 0.76), "sand":  (0.84, 0.72, 0.56), "pistachio": (0.74, 0.80, 0.52),
}
for _k, _c in PAINTS.items():
    M[f"stn_render_{_k}"] = ("stn_render", _c, 0.9, 0.0, True, True)


def image(name, colour=True):
    img = bpy.data.images.load(str(TEXTURES / f"{name}.png"), check_existing=True)
    if not colour:
        img.colorspace_settings.name = "Non-Color"
    return img


def material(name):
    """The guideway's own materials by name (lrt_*), and this kit's."""
    if name in L.MATERIALS:
        return L.material(name)
    m = bpy.data.materials.get(name)
    if m:
        return m
    tex, tint, rough, metal, grime, anti = M[name]
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
    if anti:
        colour = anti_tile(nodes, links, uv.outputs["UV"], albedo.image, colour)
    if tint is not None:
        mix = nodes.new("ShaderNodeMix")
        mix.data_type, mix.blend_type = "RGBA", "MULTIPLY"
        mix.inputs["Factor"].default_value = 1.0
        links.new(colour, mix.inputs[6])
        mix.inputs[7].default_value = (*tint, 1)
        colour = mix.outputs[2]
    if grime:
        # The guideway's drawn overlays, so station and line weather alike.
        for img_name, layer in (("grime_drips", "UVGrime"), ("grime_splash", "UVSplash")):
            guv = nodes.new("ShaderNodeUVMap")
            guv.uv_map = layer
            gtex = nodes.new("ShaderNodeTexImage")
            gtex.image = image(img_name, colour=False)
            links.new(guv.outputs["UV"], gtex.inputs["Vector"])
            mul = nodes.new("ShaderNodeMix")
            mul.data_type, mul.blend_type = "RGBA", "MULTIPLY"
            mul.inputs["Factor"].default_value = 1.0
            links.new(colour, mul.inputs[6])
            links.new(gtex.outputs["Color"], mul.inputs[7])
            colour = mul.outputs[2]
    links.new(colour, bsdf.inputs["Base Color"])
    avg = (0.6, 0.6, 0.6) if tint is None else tuple(0.85 * c for c in tint)
    m.diffuse_color = (*avg, 1)
    return m


class SBuf(P.PBuf):
    """The prop kit's PBuf (transform, panels with artwork UVs, rounded boxes, prisms, lathes) with
    the GUIDEWAY's scales: 4 m world UVs, rain tongues 4 m long below `top`, the road splash 2 m
    up from `base`, and the soot band under undersides within `soffit` of the centre line."""

    def __init__(self, name, top=None, base=0.0, splash=True, soffit=None):
        super().__init__(name, top=top)
        self.base, self.splash, self.soffit = base, splash, soffit

    def band(self, faces, outward, mat, z0, z1, u_len):
        """Give the big face of `faces` looking along `outward` the band material: u runs along
        the face every `u_len` metres, v across z0..z1."""
        out = Vector(outward)
        for f in faces:
            if f.normal.dot(out) < 0.95:
                continue
            f.material_index = self.mi(mat)
            t = UP.cross(f.normal).normalized()
            self.set_uvs(f, {v: (v.co.dot(t) / u_len, (v.co.z - z0) / (z1 - z0)) for v in f.verts})

    def top_uvs(self, faces, mat_of=None, seams_along_y=False):
        """Planar UVs seen from above. The drawn seams run along v: across the station on the barrel
        roof, down the slope on a stair canopy (`seams_along_y`)."""
        for f in faces:
            if seams_along_y:
                self.set_uvs(f, {v: (v.co.x / TILE_M, v.co.y / TILE_M) for v in f.verts})
            else:
                self.set_uvs(f, {v: (v.co.y / TILE_M, v.co.x / TILE_M) for v in f.verts})
            if mat_of:
                f.material_index = self.mi(mat_of(f))

    def world_uvs(self):
        uv = self.bm.loops.layers.uv["UVMap"]
        for f in self.bm.faces:
            if f[self.flag]:
                continue
            n = f.normal
            if abs(n.z) > 0.7:
                for lp in f.loops:
                    lp[uv].uv = (lp.vert.co.x / TILE_M, lp.vert.co.y / TILE_M)
                continue
            t = UP.cross(n)
            t = t.normalized() if t.length > 1e-6 else Vector((1, 0, 0))
            for lp in f.loops:
                lp[uv].uv = (lp.vert.co.dot(t) / TILE_M, lp.vert.co.z / TILE_M)
        drip = self.bm.loops.layers.uv.new("UVGrime")
        splash = self.bm.loops.layers.uv.new("UVSplash")
        clean = 0.999
        for f in self.bm.faces:
            n = f.normal
            side = abs(n.z) <= 0.7
            t = UP.cross(n)
            t = t.normalized() if t.length > 1e-6 else Vector((1, 0, 0))
            cz = f.calc_center_median().z
            top = self.top(cz) if callable(self.top) else self.top
            for lp in f.loops:
                co = lp.vert.co
                if side and top is not None:
                    lp[drip].uv = (co.dot(t) / 8.0, min(clean, max(0.0, (top - co.z) / 4.0)))
                else:
                    lp[drip].uv = (co.x / 8.0, clean)
                if side and self.splash:
                    lp[splash].uv = (co.dot(t) / 8.0, min(clean, max(0.0, (co.z - self.base) / 2.0)))
                elif n.z < -0.7 and self.soffit is not None:
                    lp[splash].uv = (co.y / 8.0, min(clean, max(0.0, (self.soffit - abs(co.x)) / 4.4)))
                else:
                    lp[splash].uv = (co.x / 8.0, clean)

    def finish(self, collection, origin=(0, 0, 0), bevel=0.03, segments=2, smooth=True):
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
            p.use_smooth = smooth and p.area < 0.3
        obj = bpy.data.objects.new(self.name, mesh)
        obj.location = o
        collection.objects.link(obj)
        if bevel:
            mod = obj.modifiers.new("Bevel", "BEVEL")
            mod.width, mod.segments = bevel, segments
            mod.limit_method, mod.angle_limit = "ANGLE", math.radians(35)
            mod.harden_normals, mod.use_clamp_overlap = True, True
        return obj


def collection(name, parent=None):
    c = bpy.data.collections.new(name)
    (parent or bpy.context.scene.collection).children.link(c)
    return c


def box(b, x0, x1, y0, y1, z0, z1, mat, r=0.04, mat_of=None):
    return b.rbox(((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2), (x1 - x0, y1 - y0, z1 - z0), mat, r=r,
                  mat_of=mat_of)


def by_normal(top, side, under):
    def pick(f):
        if f.normal.z > 0.6:
            return top
        if f.normal.z < -0.6:
            return under
        return side
    return pick


def arch_z(x):
    return ARCH_C + math.sqrt(max(0.0, ARCH_R ** 2 - x * x))


def under_arch(b, x0, x1, y0, y1, z0, lift, mat, r=0.05):
    """A rounded post or wall whose top follows the roof's curve, `lift` above its underside, so
    it meets the barrel without a gap on its inner side or poking through on its outer side."""
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    prof = rounded_rect((x1 - x0) / 2, (y1 - y0) / 2, min(r, (x1 - x0) * 0.45, (y1 - y0) * 0.45))
    bottom = [Vector((cx + x, cy + y, z0)) for x, y in prof]
    top = [Vector((cx + x, cy + y, arch_z(cx + x) + lift)) for x, y in prof]
    return b.loft([bottom, top], mat)


def arch_points(x0, x1, lift=0.0, n=20):
    return [(x0 + (x1 - x0) * k / n, arch_z(x0 + (x1 - x0) * k / n) + lift) for k in range(n + 1)]


# ------------------------------------------------------------------ the trackway

def trackway(col):
    """The guideway's deck carried on through the station: its four girders, a trackway slab
    between the platforms, and its ballast, sleepers and rails, all on the guideway's profiles."""
    rng = random.Random(41)
    y0, y1 = L.JOINT, END
    slab = SBuf("stn_trackway_slab", soffit=4.4, splash=False)
    slab.extrude_y(fillet([(-4.05, L.SLAB_BOTTOM), (4.05, L.SLAB_BOTTOM), (4.05, TROUGH_TOP), (-4.05, TROUGH_TOP)],
                          0.06, 2), y0, y1 - 0.04, "lrt_girder", mat_of=by_normal("lrt_deck_top", "lrt_girder", "lrt_soffit"))
    web = [(-0.36, L.SOFFIT), (0.36, L.SOFFIT), (0.36, L.SOFFIT + 0.2), (0.2, L.SOFFIT + 0.32),
           (0.2, L.SLAB_BOTTOM + 0.04), (-0.2, L.SLAB_BOTTOM + 0.04), (-0.2, L.SOFFIT + 0.32), (-0.36, L.SOFFIT + 0.2)]
    for gx in L.GIRDER_XS:
        slab.extrude_y([(gx + x, z) for x, z in fillet(web, 0.07, 2)], y0 + 0.02, y1 - 0.02, "lrt_girder")
    slab.finish(col, bevel=0.04)

    track = SBuf("stn_track", splash=False)
    bed_top = L.RAIL_HEAD - 0.13 - 0.07
    bed = [(-3.66, TROUGH_TOP - 0.03), (3.66, TROUGH_TOP - 0.03), (3.62, bed_top - 0.04), (3.2, bed_top),
           (1.1, bed_top + 0.02), (-1.1, bed_top + 0.02), (-3.2, bed_top), (-3.62, bed_top - 0.04)]
    track.extrude_y(fillet(bed, 0.2, 3), y0 + 0.02, y1 - 0.4, "lrt_ballast")
    rail = fillet([(-0.07, L.RAIL_HEAD - 0.13), (0.07, L.RAIL_HEAD - 0.13), (0.035, L.RAIL_HEAD - 0.1),
                   (0.035, L.RAIL_HEAD - 0.05), (0.055, L.RAIL_HEAD - 0.04), (0.055, L.RAIL_HEAD),
                   (-0.055, L.RAIL_HEAD), (-0.055, L.RAIL_HEAD - 0.04), (-0.035, L.RAIL_HEAD - 0.05),
                   (-0.035, L.RAIL_HEAD - 0.1)], 0.015, 2)
    for tx in (-L.TRACK_X, L.TRACK_X):
        for g in (-L.GAUGE_HALF, L.GAUGE_HALF):
            track.extrude_y([(tx + g + px, z - 0.005) for px, z in rail], y0, y1 - 0.6, "lrt_rail")
        # A chunky concrete buffer stop at the closed far end.
        box(track, tx - 1.0, tx + 1.0, y1 - 1.3, y1 - 0.35, bed_top - 0.1, L.RAIL_HEAD + 0.75, "lrt_sleeper", r=0.1)
    track.finish(col, bevel=0.012)

    sleeper = SBuf("stn_sleepers", splash=False)
    count = int((y1 - y0 - 1.8) / 0.75)
    for tx in (-L.TRACK_X, L.TRACK_X):
        for k in range(count):
            y = y0 + 0.4 + k * 0.75 + rng.uniform(-0.03, 0.03)
            # A chamfered block, no bevel: 240 of them in a station, seen only through the opening.
            sleeper.extrude_z(fillet([(-1.15, -0.14), (1.15, -0.14), (1.15, 0.14), (-1.15, 0.14)], 0.05, 1),
                              bed_top - 0.08, L.RAIL_HEAD - 0.13, "lrt_sleeper", top_scale=0.97,
                              offset=(tx + rng.uniform(-0.02, 0.02), y))
    sleeper.finish(col, bevel=0)


# ------------------------------------------------------------------ the platforms and their walls

def platforms(col):
    b = SBuf("stn_platforms", top=PLAT_TOP, splash=False, soffit=PLAT_OUT)
    y0, y1 = 0.30, END
    for s in (-1, 1):
        prof = fillet([(4.05, PLAT_BOT), (PLAT_OUT, PLAT_BOT), (PLAT_OUT, PLAT_TOP), (PLAT_IN, PLAT_TOP),
                       (PLAT_IN, 9.80), (4.05, 9.62)], 0.06, 2)
        b.extrude_y([(s * x, z) for x, z in prof], y0, y1, "lrt_girder",
                    mat_of=by_normal("stn_floor", "lrt_girder", "lrt_soffit"))
        # A deep edge beam under the outer fascia, 5 cm proud of it, and one under the inner edge.
        for xa, xb, zb in ((7.85, PLAT_OUT + 0.05, 7.90), (4.15, 4.70, 8.02)):
            b.extrude_y([(s * x, z) for x, z in fillet([(xa, zb), (xb, zb), (xb, PLAT_BOT + 0.05),
                                                        (xa, PLAT_BOT + 0.05)], 0.08, 2)],
                        y0 + 0.05, y1 - 0.05, "lrt_girder", mat_of=by_normal("lrt_girder", "lrt_girder", "lrt_soffit"))
        # The safety strip, 1.5 cm proud of the platform top.
        xa, xb = sorted((s * 3.95, s * 4.35))
        box(b, xa, xb, y0 + 0.3, y1 - 0.3, PLAT_TOP - 0.02, PLAT_TOP + 0.015, "stn_safety", r=0.01)
    b.finish(col, bevel=0.04)


def platform_walls(col):
    """The dado, posts, louvred screens and head band on each side. Water runs down from the
    roof's eaves over the head band and screens."""
    walls = SBuf("stn_platform_walls", top=lambda z: 14.95 if z > 11.3 else 11.25, base=PLAT_TOP, splash=True)
    screens = SBuf("stn_platform_screens", top=14.0, splash=False)
    z_dado, z_head = 11.2, 14.0
    for s in (-1, 1):
        out = Vector((s, 0, 0))
        xa, xb = sorted((s * 7.98, s * 8.26))
        box(walls, xa, xb, 0.9, END - 0.35, PLAT_TOP - 0.03, z_dado, "stn_wall", r=0.05)
        # The dado's coping: a fat rounded sill the screens stand on.
        xa2, xb2 = sorted((s * 7.9, s * 8.33))
        box(walls, xa2, xb2, 0.9, END - 0.35, z_dado - 0.06, z_dado + 0.12, "lrt_coping", r=0.05)
        xa3, xb3 = sorted((s * 7.98, s * 8.30))
        under_arch(walls, xa3, xb3, 0.9, END - 0.35, z_head - 0.05, 0.14, "stn_wall")
        for y in POSTS:
            xp0, xp1 = sorted((s * 7.88, s * 8.46))
            under_arch(walls, xp0, xp1, y - 0.26, y + 0.26, PLAT_TOP - 0.03, 0.14, "stn_wall_post", r=0.08)
        for ya, yb in zip(POSTS, POSTS[1:]):
            xs0, xs1 = sorted((s * 8.02, s * 8.20))
            fs = box(screens, xs0, xs1, ya + 0.2, yb - 0.2, z_dado + 0.08, z_head + 0.02, "stn_wall", r=0.03)
            screens.band(fs, out, "stn_louvre", z_dado + 0.08, z_head + 0.02, 2.0)
            screens.band(fs, -out, "stn_louvre", z_dado + 0.08, z_head + 0.02, 2.0)
    walls.finish(col, bevel=0.03, segments=1)
    screens.finish(col, bevel=0)


# ------------------------------------------------------------------ the ends and the roof

def ends(col):
    """End walls over the platforms and the tympanum filling the arch, at both ends. The near end
    leaves the track opening x +/-3.9 up to OPEN_TOP; the far end is closed across it."""
    b = SBuf("stn_ends", top=lambda z: arch_z(0) if z > OPEN_TOP else OPEN_TOP, splash=False)
    for ya, yb, closed in ((0.55, 0.85, False), (END - 0.30, END - 0.05, True)):
        outward = Vector((0, -1, 0)) if not closed else Vector((0, 1, 0))
        spans = [(-8.3, -4.15, PLAT_TOP - 0.03), (4.15, 8.3, PLAT_TOP - 0.03)]
        if closed:
            spans.append((-4.2, 4.2, TROUGH_TOP - 0.02))
        for xa, xb, z0 in spans:
            fs = box(b, xa, xb, ya, yb, z0, OPEN_TOP + 0.08, "stn_wall", r=0.05)
            b.band(fs, outward, "stn_louvre", z0, OPEN_TOP + 0.08, 2.0)
            b.band(fs, -outward, "stn_louvre", z0, OPEN_TOP + 0.08, 2.0)
        # The lintel over the track opening, a chunky beam into both end walls.
        box(b, -4.4, 4.4, ya - 0.08, yb + 0.08, OPEN_TOP - 0.45, OPEN_TOP + 0.12, "lrt_coping", r=0.06)
        # The tympanum: the arch's shape above OPEN_TOP, reaching 10 cm into the roof.
        xm = math.sqrt(ARCH_R ** 2 - (OPEN_TOP - ARCH_C) ** 2)
        pts = [(-xm, OPEN_TOP)] + arch_points(-xm, xm, lift=0.1, n=24) + [(xm, OPEN_TOP)]
        rings = [[Vector((x, y, z)) for x, z in pts] for y in (ya + 0.03, yb - 0.03)]
        b.loft(rings, "stn_wall")
    b.finish(col, bevel=0.03)


def roof(col):
    """The barrel roof: a thick arched sheet, eave beams, arched end ribs and the crown monitor."""
    b = SBuf("stn_roof", top=CROWN + 0.3, splash=False)
    y0, y1 = 0.3, END + 0.3
    t = 0.28
    outer = arch_points(-EAVE_X, EAVE_X, lift=t, n=28)
    inner = arch_points(-EAVE_X, EAVE_X, lift=0.0, n=28)
    prof = outer + inner[::-1]
    fs = b.extrude_y(prof, y0, y1, "stn_roof")
    b.top_uvs([f for f in fs if f.normal.z > 0.2 and abs(f.normal.y) < 0.5],
              mat_of=lambda f: "stn_roof")
    for f in fs:
        if f.normal.z < -0.2:
            f.material_index = b.mi("lrt_soffit")
        elif abs(f.normal.y) > 0.9:
            f.material_index = b.mi("lrt_coping")
    # Eave beams: a fat gutter along each side.
    for s in (-1, 1):
        g = fillet([(EAVE_X - 0.45, EAVE_Z - 0.35), (EAVE_X + 0.18, EAVE_Z - 0.35), (EAVE_X + 0.18, EAVE_Z + 0.34),
                    (EAVE_X - 0.45, EAVE_Z + 0.2)], 0.07, 2)
        b.extrude_y([(s * x, z) for x, z in g], y0 - 0.05, y1 + 0.05, "lrt_coping")
    # Arched end ribs over each end.
    for ya, yb in ((y0 - 0.06, y0 + 0.36), (y1 - 0.36, y1 + 0.06)):
        o = arch_points(-EAVE_X - 0.1, EAVE_X + 0.1, lift=t + 0.14, n=28)
        i = arch_points(-EAVE_X + 0.2, EAVE_X - 0.2, lift=-0.42, n=28)
        rib = o + i[::-1]
        b.loft([[Vector((x, y, z)) for x, z in rib] for y in (ya, yb)], "lrt_coping")
    # The crown monitor: louvred sides under a cap, the whole length but the ends.
    mz0 = arch_z(2.6) - 0.1
    mz1 = CROWN + 0.95
    for s in (-1, 1):
        xa, xb = sorted((s * 2.45, s * 2.62))
        fs = box(b, xa, xb, 2.0, END - 1.7, mz0, mz1, "stn_wall", r=0.03)
        b.band(fs, Vector((s, 0, 0)), "stn_louvre", mz0, mz1, 2.0)
    for ya in (2.0, END - 1.7):
        fs = box(b, -2.55, 2.55, ya - 0.1, ya + 0.1, mz0, mz1, "stn_wall", r=0.03)
    cap = fillet([(-2.95, mz1 - 0.05), (2.95, mz1 - 0.05), (2.95, mz1 + 0.12), (0, mz1 + 0.34),
                  (-2.95, mz1 + 0.12)], 0.06, 2)
    fs = b.extrude_y(cap, 1.8, END - 1.5, "stn_roof")
    b.top_uvs([f for f in fs if f.normal.z > 0.2], mat_of=lambda f: "stn_roof")
    for f in fs:
        if f.normal.z < -0.2:
            f.material_index = b.mi("lrt_soffit")
    b.finish(col, bevel=0.03)


# ------------------------------------------------------------------ structure: bents and concourse

def bent(col, name, low):
    """Twin columns (the guideway's pier legs) under a wide hammerhead. `low` carries the
    concourse at 5.0; otherwise the platforms at 8.55."""
    rng = random.Random(7 if low else 8)
    top = 4.95 if low else 7.45
    body = SBuf(name, top=lambda z: top if z < top else top + 1.0, splash=True)
    for s in (-1, 1):
        body.extrude_z(rounded_rect(0.80, 0.80, 0.24), -0.12, 0.06, "lrt_pier", top_scale=0.97, offset=(s * L.PIER_X, 0))
        lean = (rng.uniform(-0.03, 0.03), rng.uniform(-0.03, 0.03))
        body.extrude_z(rounded_rect(L.PIER_HALF + 0.02, L.PIER_HALF + 0.02, 0.2), 0.05, top, "lrt_pier",
                       top_scale=0.95, offset=(s * L.PIER_X, 0), lean=lean)
    if low:
        prof = [(-9.2, 4.75), (-8.0, 4.42), (-4.9, 4.2), (0.0, 4.32), (4.9, 4.2), (8.0, 4.42), (9.2, 4.75),
                (9.2, 5.12), (-9.2, 5.12)]
    else:
        prof = [(-8.3, 7.75), (-7.2, 7.35), (-4.9, 7.15), (0.0, 7.3), (4.9, 7.15), (7.2, 7.35), (8.3, 7.75),
                (8.3, 8.58), (-8.3, 8.58)]
    body.extrude_y(fillet(prof, 0.22, 3), -0.85, 0.85, "lrt_pier_cap")
    return body.finish(col, bevel=0.05)


def end_beam(col):
    """The station's end beam on the guideway's last pier: behind the pier cap (it overlaps the cap's
    north half) and wider, carrying the platforms' first metre."""
    b = SBuf("stn_end_beam", top=8.58, splash=False)
    prof = [(-8.3, 7.75), (-7.2, 7.4), (-5.8, 7.22), (5.8, 7.22), (7.2, 7.4), (8.3, 7.75), (8.3, 8.58), (-8.3, 8.58)]
    b.extrude_y(fillet(prof, 0.22, 3), 0.08, 0.98, "lrt_pier_cap")
    b.finish(col, bevel=0.05)


def concourse(col):
    b = SBuf("stn_concourse", top=8.9, base=5.0, splash=False, soffit=CONC_HALF + 0.2)
    ya, yb = CONC
    box(b, -CONC_HALF - 0.2, CONC_HALF + 0.2, ya - 0.2, yb + 0.2, 5.0, CONC_FLOOR, "lrt_girder", r=0.1,
        mat_of=by_normal("lrt_coping", "lrt_girder", "lrt_soffit"))
    fs = box(b, -CONC_HALF, CONC_HALF, ya, yb, CONC_FLOOR - 0.07, 8.62, "stn_wall", r=0.12)
    for out, u in ((Vector((1, 0, 0)), 3.0), (Vector((-1, 0, 0)), 3.0), (Vector((0, 1, 0)), 3.0), (Vector((0, -1, 0)), 3.0)):
        b.band(fs, out, "stn_windows", CONC_FLOOR - 0.07, 8.62, u)
    box(b, -CONC_HALF - 0.25, CONC_HALF + 0.25, ya - 0.25, yb + 0.25, 8.52, 8.9, "lrt_coping", r=0.08,
        mat_of=by_normal("lrt_coping", "lrt_coping", "lrt_soffit"))
    # The doors where the stairs land: dark openings with a chunky frame, proud of the wall.
    for s in (-1, 1):
        xa, xb = sorted((s * ST_X0, s * ST_X1))
        box(b, xa, xb, ya - 0.05, ya + 0.02, ST_Z - 0.02, ST_Z + 2.35, "stn_dark", r=0.02)
        for x in (xa - 0.12, xb + 0.12):
            box(b, x - 0.12, x + 0.12, ya - 0.1, ya + 0.03, ST_Z - 0.02, ST_Z + 2.5, "lrt_coping", r=0.04)
        box(b, xa - 0.24, xb + 0.24, ya - 0.1, ya + 0.03, ST_Z + 2.35, ST_Z + 2.6, "lrt_coping", r=0.04)
    b.finish(col, bevel=0.04)


def stair(col):
    """The east stairway (the west one is its mirror): the flight as one stepped solid, solid
    balustrade walls with a fat coping, and a sloped canopy on stout posts. Everything that meets
    the pavement sinks 2 cm into it."""
    b = SBuf("stn_stair", top=lambda z: z + 0.6, base=PT, splash=True)
    zb = PT - 0.022

    def nose(y):
        return PT + (y - ST_FOOT) * ST_PITCH

    pts = [(ST_FOOT, zb)]
    for k in range(ST_N):
        z = PT + (k + 1) * ST_RISE
        pts += [(ST_FOOT + k * ST_GO, z), (ST_FOOT + (k + 1) * ST_GO, z)]
    pts[-1] = (ST_LAND, ST_Z)
    soffit_at = lambda y: nose(y) - 0.55                              # noqa: E731
    y_meet = ST_FOOT + (zb - PT + 0.55) / ST_PITCH
    pts += [(ST_LAND, ST_Z - 0.45), (ST_TOP, soffit_at(ST_TOP)), (y_meet, zb)]
    rings = [[Vector((x, y, z)) for y, z in pts] for x in (ST_X0 - 0.03, ST_X1 + 0.03)]
    b.loft(rings, "lrt_concrete", mat_of=by_normal("stn_tread", "lrt_concrete", "lrt_soffit"))
    # Balustrades: overlapping the flight's sides, so its end faces are buried in them.
    h = 1.05
    for xa, xb in ((ST_X0 - 0.25, ST_X0 + 0.02), (ST_X1 - 0.02, ST_X1 + 0.25)):
        bal = [(ST_FOOT - 0.02, zb), (ST_FOOT - 0.02, PT + h), (ST_TOP, ST_Z + h), (ST_LAND - 0.1, ST_Z + h),
               (ST_LAND - 0.1, ST_Z - 0.5), (ST_TOP, soffit_at(ST_TOP) - 0.05), (y_meet - 0.05, zb)]
        b.loft([[Vector((x, y, z)) for y, z in bal] for x in (xa, xb)], "stn_wall",
               mat_of=by_normal("stn_wall", "stn_wall", "lrt_soffit"))
        xm = (xa + xb) / 2
        path = [Vector((xm, ST_FOOT - 0.08, PT + h + 0.02)), Vector((xm, ST_TOP, ST_Z + h + 0.02)),
                Vector((xm, ST_LAND - 0.05, ST_Z + h + 0.02))]
        b.prism(path[0], path[1], 0.2, 0.2, "lrt_coping", r=0.07)
        b.prism(path[1] - Vector((0, 0.15, 0)), path[2], 0.2, 0.2, "lrt_coping", r=0.07)
    # The canopy, parallel to the flight, stopping where it would meet the platform's edge beam.
    c0, c1 = ST_FOOT + 0.2, 20.0
    lo, th = 2.35, 0.2
    xa, xb = ST_X0 - 0.35, ST_X1 + 0.35
    rings = []
    for y in (c0, c1):
        z = nose(y) + lo
        rings.append([Vector((x, y, zz)) for x, zz in fillet([(xa, z), (xb, z), (xb, z + th), (xa, z + th)], 0.05, 2)])
    fs = b.loft(rings, "stn_roof")
    b.top_uvs([f for f in fs if f.normal.z > 0.2], mat_of=lambda f: "stn_roof", seams_along_y=True)
    for f in fs:
        if f.normal.z < -0.2:
            f.material_index = b.mi("lrt_soffit")
        elif abs(f.normal.z) <= 0.2:
            f.material_index = b.mi("lrt_coping")
    for y in (12.3, 14.9, 17.5, 19.7):
        for x in (ST_X0 - 0.115, ST_X1 + 0.115):
            b.prism((x, y, nose(y) + h - 0.05), (x, y, nose(y) + lo + 0.05), 0.15, 0.13, "stn_wall_post", r=0.05)
    return b.finish(col, bevel=0.025, segments=1)


# ------------------------------------------------------------------ name boards

def boards(col, name, mat):
    """The station's name on both tympana and on the platform fascia each side."""
    b = SBuf(name, top=None, splash=False)

    def board(mtx, w, h):
        b.panel(mtx, w, h, 0.1, "stn_board_edge", mat, r=0.05)

    board(facing("-y", Vector((0, 0.54, 15.70))), 9.0, 1.35)
    board(facing("+y", Vector((0, END - 0.05, 15.70))), 9.0, 1.35)
    for s, d in ((1, "+x"), (-1, "-x")):
        board(facing(d, Vector((s * (PLAT_OUT + 0.04), 70.0, 9.30))), 7.6, 1.14)
    return b.finish(col, bevel=0.01)


# ------------------------------------------------------------------ the street-end rows

# Each building: x0, x1, storeys above the ground floor, paint, window, front offset, extras.
ROWS = {
    1: [(-45.0, -33.8, 2, "rose", "stn_window_a", 0.55, {"gable"}),
        (-34.0, -21.8, 3, "sage", "stn_window_b", 0.15, {"balconies"}),
        (-22.0, -12.4, 2, "ochre", "stn_window_c", 0.85, {"tank"}),
        (-12.6, -2.2, 4, "grey", "stn_window_a", 0.0, {"fins", "penthouse"}),
        (-2.4, 4.6, 3, "cream", "stn_window_c", 0.45, {"tank"}),
        (4.4, 10.6, 4, "mint", "stn_window_b", 0.25, {"balconies"})],
    -1: [(-45.0, -35.2, 3, "pistachio", "stn_window_b", 0.35, {"tank"}),
         (-35.4, -25.0, 2, "lilac", "stn_window_a", 0.8, {"gable"}),
         (-25.2, -14.6, 4, "sand", "stn_window_c", 0.1, {"fins"}),
         (-14.8, -5.6, 3, "rose", "stn_window_b", 0.6, {"balconies", "tank"}),
         (-5.8, 3.0, 4, "sage", "stn_window_a", 0.0, {"penthouse"}),
         (2.8, 10.6, 3, "ochre", "stn_window_c", 0.4, {"tank"})],
}
GF = 4.0
STOREY = 3.1
SHOP_ATLAS = [(0.0, 1 - 420 / 1024, 0.5, 1.0), (0.5, 1 - 420 / 1024, 1.0, 1.0),
              (0.0, 0.5 - 420 / 1024, 0.5, 0.5), (0.5, 0.5 - 420 / 1024, 1.0, 0.5)]


def row(col, sgn):
    """The chunky row across the street end. sgn +1 is north (fronts face -y), -1 south."""
    rng = random.Random(900 + sgn)
    face = "-y" if sgn > 0 else "+y"
    towards = Vector((0, -sgn, 0))

    def Y(d):
        return sgn * (ROW_FRONT + d)

    def bx(b, x0, x1, d0, d1, z0, z1, mat, r=0.05, mat_of=None):
        ya, yb = sorted((Y(d0), Y(d1)))
        return box(b, x0, x1, ya, yb, z0, z1, mat, r=r, mat_of=mat_of)

    strip = SBuf(f"stn_row_{'north' if sgn > 0 else 'south'}_strip", top=ROW_BASE, base=0.0)
    bx(strip, -45.4, 10.6, -1.6, 0.9, -0.2, ROW_BASE, "stn_strip", r=0.05)
    strip.finish(col, bevel=0.02)
    sign_k = 0 if sgn > 0 else 4
    for n, (x0, x1, storeys, paint, win, d0, extras) in enumerate(ROWS[sgn]):
        H = GF + storeys * STOREY
        wall = f"stn_render_{paint}"
        b = SBuf(f"stn_row_{'north' if sgn > 0 else 'south'}_{n}", top=H + 1.0, base=ROW_BASE)
        dg = d0 + 0.85                                       # the ground floor sits back under the overhang
        back = ROW_DEPTH - 0.13 * (n % 3)                  # no two back walls in one plane
        bx(b, x0 + 0.03, x1 - 0.03, dg, back, -0.3, GF + 0.06, wall, r=0.06)
        bx(b, x0, x1, d0, back - 0.1, GF - 0.05, H, wall, r=0.06)
        # The floor band where the upper floors overhang, and a string course at every floor.
        bx(b, x0 - 0.04, x1 + 0.04, d0 - 0.12, dg + 0.1, GF - 0.25, GF + 0.08, "stn_trim", r=0.05)
        for k in range(1, storeys):
            z = GF + k * STOREY
            bx(b, x0 + 0.02, x1 - 0.02, d0 - 0.09, d0 + 0.3, z - 0.07, z + 0.09, "stn_trim", r=0.04)
        # Cornice and a parapet round the roof.
        bx(b, x0 - 0.1, x1 + 0.1, d0 - 0.2, d0 + 0.5, H - 0.12, H + 0.22, "stn_trim", r=0.06)
        for pa in ((x0 + 0.05, x1 - 0.05, d0 + 0.02, d0 + 0.3), (x0 + 0.05, x1 - 0.05, back - 0.55, back - 0.25),
                   (x0 + 0.05, x0 + 0.33, d0 + 0.2, back - 0.4), (x1 - 0.33, x1 - 0.05, d0 + 0.2, back - 0.4)):
            bx(b, pa[0], pa[1], pa[2], pa[3], H - 0.05, H + 0.85, wall, r=0.05)
        # Ground-floor shop bays: piers between them, a drawn shopfront in each.
        bays = max(1, round((x1 - x0) / 3.8))
        bw = (x1 - x0) / bays
        for k in range(bays + 1):
            x = x0 + k * bw
            bx(b, max(x0 - 0.02, x - 0.26), min(x1 + 0.02, x + 0.26), dg - 0.14, dg + 0.1, -0.1, GF - 0.1, "stn_trim", r=0.05)
        for k in range(bays):
            cx = x0 + (k + 0.5) * bw
            # The FOR RENT shutter is the rare one.
            region = SHOP_ATLAS[rng.choice((0, 0, 1, 1, 3, 3, 2))]
            b.panel(facing(face, Vector((cx, Y(dg - 0.02), 0.28 + 1.45))), bw - 0.62, 2.9, 0.06, "stn_trim",
                    "stn_shops", region=region, r=0.02)
        # One or two painted shop boards on the band over the shops.
        for k in range(min(2, bays)):
            cx = x0 + (k + 0.5) * (x1 - x0) / min(2, bays)
            w = min(4.2, (x1 - x0) / min(2, bays) - 0.6)
            row_k = sign_k % 8
            sign_k += 1
            v0, v1 = 1 - (row_k + 1) / 8, 1 - row_k / 8
            b.panel(facing(face, Vector((cx, Y(d0 - 0.02), GF + 0.52))), w, w / 8, 0.05, "stn_trim", "stn_shopsigns",
                    region=(0.0, v0, 1.0, v1), r=0.02)
        # Upper-floor windows: a drawn window panel proud of the wall, a chunky sill under it.
        wbays = max(1, round((x1 - x0) / 2.9))
        ww = min(1.6, (x1 - x0) / wbays - 0.9)
        wh = {"stn_window_a": ww * 1.14, "stn_window_b": ww * 0.7, "stn_window_c": ww * 0.82}[win]
        for f in range(storeys):
            zf = GF + f * STOREY
            for k in range(wbays):
                cx = x0 + (k + 0.5) * (x1 - x0) / wbays
                zc = zf + 1.05 + wh / 2
                b.panel(facing(face, Vector((cx, Y(d0 - 0.02), zc))), ww, wh, 0.06, "stn_trim", win, r=0.02)
                bx(b, cx - ww / 2 - 0.12, cx + ww / 2 + 0.12, d0 - 0.16, d0 + 0.05, zc - wh / 2 - 0.14, zc - wh / 2 - 0.01,
                   "stn_trim", r=0.03)
                if "balconies" in extras and f >= 1:
                    bd = 0.95
                    bx(b, cx - ww / 2 - 0.45, cx + ww / 2 + 0.45, d0 - bd, d0 + 0.1, zf - 0.04, zf + 0.16, "stn_trim", r=0.04)
                    bx(b, cx - ww / 2 - 0.42, cx + ww / 2 + 0.42, d0 - bd + 0.02, d0 - bd + 0.2, zf + 0.12, zf + 1.02,
                       wall, r=0.05)
                    for sx in (-1, 1):
                        xs = cx + sx * (ww / 2 + 0.33)
                        bx(b, xs - 0.09, xs + 0.09, d0 - bd + 0.05, d0 + 0.05, zf + 0.12, zf + 1.0, wall, r=0.04)
        if "fins" in extras:
            for k in range(wbays + 1):
                x = x0 + 0.2 + k * (x1 - x0 - 0.4) / wbays
                bx(b, x - 0.13, x + 0.13, d0 - 0.55, d0 + 0.1, GF + 0.95, H - 0.1, "stn_trim", r=0.05)
        if "gable" in extras:
            # A tin gable facing the street over the parapet: the roof ridge runs back from it.
            ridge = H + 0.8 + (x1 - x0) * 0.22
            xm = (x0 + x1) / 2
            prof = [(x0 - 0.3, H + 0.55), (x1 + 0.3, H + 0.55), (xm, ridge)]
            ya, yb = sorted((Y(d0 - 0.35), Y(back - 0.3)))
            rings = [[Vector((x, y, z)) for x, z in fillet(prof, 0.08, 2)] for y in (ya, yb)]
            fs = b.loft(rings, "stn_tin_red")
            for f in fs:
                if abs(f.normal.y) > 0.9:
                    f.material_index = b.mi(wall)
        if "tank" in extras:
            tx = x0 + (x1 - x0) * rng.uniform(0.3, 0.7)
            td = rng.uniform(4.5, 9.0)
            bx(b, tx - 1.0, tx + 1.0, td - 1.0, td + 1.0, H - 0.05, H + 1.2, "stn_steel", r=0.05)
            b.lathe(Vector((tx, Y(td), H + 1.15)), [(0.0, 0.0), (0.92, 0.0), (0.95, 0.08), (0.95, 1.7), (0.8, 1.9),
                                                     (0.3, 2.05), (0.0, 2.06)],
                    rng.choice(["stn_tank_black", "stn_tank_green"]), sides=16)
        if "penthouse" in extras:
            px = x0 + (x1 - x0) * 0.35
            bx(b, px - 1.8, px + 1.8, 3.5, 7.5, H - 0.05, H + 2.8, wall, r=0.06)
            bx(b, px - 2.0, px + 2.0, 3.3, 7.7, H + 2.75, H + 3.0, "stn_trim", r=0.05)
        # No bevel modifier: every box is already filleted in plan, and at 220 m a bevel is
        # thousands of triangles nobody sees.
        b.finish(col, bevel=0)


# ------------------------------------------------------------------ assembly

def place(proto_objs, target, loc, rot_z=0.0, mirror_x=False):
    out = []
    for o in proto_objs:
        c = o.copy()                         # linked duplicate: shares the prototype's mesh
        c.location = Vector(loc)
        c.rotation_euler = (0, 0, rot_z)
        if mirror_x:
            c.scale = (-1, 1, 1)
        target.objects.link(c)
        out.append(c)
    return out


def assemble():
    kit = collection("stations kit (prototypes)")
    kit.hide_render = True
    station = collection("stn_station (prototype)", kit)
    trackway(station)
    platforms(station)
    platform_walls(station)
    ends(station)
    roof(station)
    concourse(station)
    end_beam(station)
    parts = collection("stn_parts (prototype)", kit)
    low = bent(parts, "stn_bent_low", True)
    high = bent(parts, "stn_bent_high", False)
    st = stair(parts)
    names = collection("stn_names (prototype)", kit)
    un = boards(names, "stn_boards_un", "stn_name_un")
    pg = boards(names, "stn_boards_pgil", "stn_name_pgil")

    placed = collection("stations (placed)")
    for label, sgn, board in (("UN Avenue station (north)", 1, un), ("Pedro Gil station (south)", -1, pg)):
        c = collection(label, placed)
        rot = 0.0 if sgn > 0 else math.pi
        at = (0.0, sgn * ANCHOR, 0.0)
        # Rotating a local point (x, y) by 180 degrees gives (-x, -y); the anchor moves with it.
        place(list(station.objects), c, at, rot)
        place([board], c, at, rot)
        for ly in BENTS_LOW:
            place([low], c, (0.0, sgn * (ANCHOR + ly), 0.0), rot)
        for ly in BENTS_HIGH:
            place([high], c, (0.0, sgn * (ANCHOR + ly), 0.0), rot)
        place([st], c, at, rot)
        place([st], c, at, rot, mirror_x=True)
    for sgn, label in ((1, "street end north (row)"), (-1, "street end south (row)")):
        row(collection(label, placed), sgn)
    return placed


# ------------------------------------------------------------------ review

def review_set():
    """Plain stand-ins for the court side of Taft and the linked guideway, for the close-ups only."""
    col = collection("review stand-ins")

    def slab(name, x0, x1, y0, y1, top, colour):
        me = bpy.data.meshes.new(name)
        z0 = top - 0.4
        vs = [(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0), (x0, y0, top), (x1, y0, top), (x1, y1, top),
              (x0, y1, top)]
        me.from_pydata(vs, [], [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)])
        mat = bpy.data.materials.new(name)
        if mat.node_tree is None:
            mat.use_nodes = True
        mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*colour, 1)
        me.materials.append(mat)
        col.objects.link(bpy.data.objects.new(name, me))

    slab("stand-in road", -6.75, 6.75, -240, 240, 0.0, (0.30, 0.30, 0.31))
    for s in (-1, 1):
        slab("stand-in pavement", *sorted((s * 6.75, s * 10.75)), -240, 240, PT, (0.62, 0.60, 0.56))
        slab("stand-in lot", *sorted((s * 10.75, s * 60.0)), -240, 240, 0.24, (0.55, 0.53, 0.47))
    try:
        with bpy.data.libraries.load(str(SOURCE / "lrt_kit.blend"), link=True, relative=True) as (src, dst):
            dst.collections = ["guideway over the court"]
        g = collection("review (guideway linked)")
        g.children.link(dst.collections[0])
    except (OSError, KeyError, IndexError) as e:
        print("[ilalim-stn] guideway not linked:", e)


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
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type == "VIEW_3D":
                for space in area.spaces:
                    if space.type == "VIEW_3D":
                        space.shading.type = "MATERIAL"
                        space.clip_start, space.clip_end = 0.1, 3000


def preview(version, only=None):
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 1000
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    cam.data.clip_end = 3000
    scene.collection.objects.link(cam)
    scene.camera = cam
    shots = [
        ("north_end", (0.0, 55.0, 6.0), (0.0, 100.0, 11.0), 28),
        ("joint", (7.5, 84.0, 12.5), (0.0, 95.0, 9.5), 24),
        ("stairs_east", (13.0, 96.0, 2.2), (8.0, 112.0, 4.0), 24),
        ("stair_close", (12.5, 101.0, 3.2), (8.2, 108.5, 3.0), 26),
        ("joint_under", (10.5, 84.0, 2.5), (1.5, 95.5, 8.0), 24),
        ("side_elevation", (48.0, 140.0, 9.0), (0.0, 140.0, 9.0), 22),
        ("aerial_north", (40.0, 60.0, 45.0), (0.0, 150.0, 8.0), 22),
        ("row_north", (0.0, 190.0, 3.0), (-15.0, 220.0, 7.0), 20),
        ("south_end", (0.0, -55.0, 6.0), (0.0, -100.0, 11.0), 28),
        ("row_south", (-5.0, -190.0, 3.0), (-18.0, -220.0, 7.0), 20),
    ]
    for name, pos, tgt, lens in shots:
        if only and name not in only:
            continue
        path = PREVIEWS / f"stn_{name}_v{version}.png"
        if path.exists():
            print("[ilalim-stn] exists, skipped", path)
            continue
        pos, tgt = Vector(pos), Vector(tgt)
        cam.location, cam.data.lens = pos, lens
        cam.rotation_euler = (tgt - pos).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        print("[ilalim-stn] preview", path)


def triangles(col):
    dg = bpy.context.evaluated_depsgraph_get()
    total = 0
    for o in col.all_objects:
        if o.type != "MESH":
            continue
        ev = o.evaluated_get(dg)
        me = ev.to_mesh()
        me.calc_loop_triangles()
        total += len(me.loop_triangles)
        ev.to_mesh_clear()
    return total


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
    only = argv[argv.index("--only") + 1].split(",") if "--only" in argv else None
    bpy.ops.wm.read_factory_settings(use_empty=True)
    SOURCE.mkdir(parents=True, exist_ok=True)
    out = SOURCE / "stations.blend"
    # Save first, so the review link to the guideway is written relative to this file.
    bpy.ops.wm.save_as_mainfile(filepath=str(out))
    placed = assemble()
    review_set()
    lighting()
    bpy.ops.wm.save_as_mainfile(filepath=str(out), compress=True, relative_remap=True)
    backup = SOURCE / "stations.blend1"
    if backup.exists():
        backup.unlink()
    print(f"[ilalim-stn] saved {out}; {len(list(placed.all_objects))} placed objects, "
          f"{triangles(placed)} triangles placed (after bevels)")
    if version:
        preview(version, only)


if __name__ == "__main__":
    main()
