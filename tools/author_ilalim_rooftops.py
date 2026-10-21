"""Model the rooftop life of the Ilalim ng Tulay flat roofs and place it (ILALIM-1.3, rooftops kit).

  py -3 tools/author_ilalim_textures_rooftops.py [--sheet N]     # paint the roof_ textures first
  blender -b --python tools/author_ilalim_rooftops.py -- [--preview N] [--only a,b]

Writes ArtSource/ilalim/rooftops.blend. With --preview it writes versioned close-ups of the
prototypes (on a stand-in roof slab) to Logs/ilalim-blender/roof_<shot>_vN.png; the views from the
court come from the city file, which links this kit's "rooftops (placed)".

Owner, 2026-09-30: "can you think of a way to make the place look more lively, more unique building
shapes etc?", then "proceed". From the court and from the air the east side's skyline was flat
boxes with a stair house and two tanks each. A Manila roof is where the building lives: water
tanks on steel stands, a plywood-and-GI shack or a hollow-block room someone added, laundry, a
billboard on lattice legs facing the avenue, a dish, a cell mast, pots of plants.

WHERE IT GOES. The script LOADS (links, read-only) ArtSource/ilalim/eastside.blend and
heritage.blend, and reads, in world space, every face with the material east_roof or
heritage_flat_roof of at least 12 m2: those are the flat roofs (about 110 on the east side, one on
the heritage side, the Sentro Oftalmologica, 190 m west). lrt_kit.blend and sarisari.blend are
linked too, as occluders only. Nothing is written back to any of those files.
  * CLEAR OF EVERYTHING: a footprint (the prototype's plan box plus 0.35 m) is sampled every 0.4 m;
    every sample must lie on the roof polygon, at least 0.35 m in from its edge, and a ray cast
    straight down from 300 m above must hit THIS roof face first. So nothing lands on a parapet, a
    kit's own stair house, tank or plinth, or under a taller part of the building (the Astral
    tower on its podium). Items keep clear of each other the same way.
  * NEVER on the sari-sari lot (x 11.8..22.8, y 38.3..47.3) or over the LRT guideway (|x| < 6).
  * LANDMARKS: if ArtSource/ilalim/landmarks.json exists ({"hide": [object names]}), the roofs of
    the listed buildings are skipped, and those buildings are left out of the occluders.
  * DENSITY FOLLOWS THE COURT. Each roof's distance from the court centre sets its budget: six
    items within 60 m (a big roof there takes one per 75 m2, up to ten), four within 100 m, two
    within 150 m, one on 60 per cent of the roofs within 220 m and on 30 per cent beyond; a small
    roof takes at most one per 55 m2. Within 150 m the FIRST item on a roof is a tall one (a
    billboard, a tank on its stand or the cell mast; a near roof over 400 m2 gets a second tall
    tank), put where the court sees it best: every candidate spot is ray-tested from seven eye
    points on the court (spawn, taya, centre, both pavements, north and south) at 85 per cent of
    the item's height, against every linked kit's geometry, and the spot seen by the most eyes
    (then the nearest the court) wins. A billboard seen by fewer than two eyes is wasted and
    becomes a tank. Far roofs get tanks, dishes and shacks only. The script prints every tall
    placement and how many eyes see it.
  * VARIETY: at most two of a family on one roof and three tanks of any sort (five tanks in a
    row on the Astral podium read as a tank yard); the penthouse, laundry and garden variants are
    taken in turn across the map; at most two cell masts.
  * Seeded per roof (the building's name), so a rebuild places the same things, unless the east
    kit's roofs change (re-run this script after any east or heritage rebuild).
  * As built on 2026-09-30 (east kit mid-rework, landmarks.json present): the soda billboard on
    the WEC roof faces the court over Taft (seen by five of seven eyes; from the taya spot its
    top reads over the WEC front), the Kape billboard on Manila Science High School's roof sits
    against the sky above Padre Faura in the tele view, the hardware board is on the Cathedral
    of Praise facing south down Taft, the cell masts are on the Astral tower top and on Vista GL,
    and a cream tank stands on the Astral podium's Taft edge.

THE PROTOTYPES (collection "rooftops prototypes", at the origin, hidden; every placement is an
EMPTY named "roof <kind> <building> <k>" holding LINKED DUPLICATES of the prototype's objects, so
editing one prototype edits every copy). Local frame: the roof surface is z = 0, the item's front
looks along local -Y; every foot, pad and post sinks 1 to 2 cm below z = 0 into the roof.
  roof_tank_stand_black / _green / _cream   the classic Manila rooftop tank: a 1.5 m polyethylene
        tank (moulded ribs, a domed shoulder, a screw lid) on a 2.2 m stand of four fat splayed
        legs, two ring frames and one fat diagonal a side, a steel deck, concrete pads, and a
        fat PVC down pipe. 2.0 x 2.0 m, 4.0 m tall. The stand is red oxide, grey or oxide.
  roof_tank_stand_galv    a galvanised steel tank with a coned lid on the same stand.
  roof_tank_pair          two short tanks (black and cream) side by side on hollow-block piers and
        two steel beams. 2.9 x 1.4 m, 2.0 m tall.
  roof_shack              a plywood-and-GI shack: timber corner posts, plywood walls, a painted
        door (3), a grilled window, a mono-pitch red-oxide GI roof held down by two old tyres and
        hollow blocks, a monobloc chair and a water drum outside. 3.9 x 3.6 m, 2.5 m tall.
  roof_penthouse_mint / _rose   a hollow-block room somebody added: painted block, a slab roof
        with a lip, a steel door under a GI awning on fat brackets, the grilled window, a window
        aircon. 4.5 x 3.7 m, 2.8 m tall.
  roof_bulkhead           a stair bulkhead: rendered block, a sloped slab roof, a maroon steel door,
        a gooseneck vent and a lamp box. 2.4 x 3.2 m, 2.6 m tall.
  roof_laundry_a / _b     laundry: two fat T posts standing in concrete-filled buckets, two thick
        sagging lines, and big garment CARDS hung on them (each a 14 mm outline slab: shirts, a
        sando, towels, a floral duster, a checked blanket, shorts, a pillowcase), a few degrees
        of sway each. 4.4 m / 3.4 m long.
  roof_billboard_soda / _kape / _hardware   billboards facing the court: a steel board with the ad
        on its face (roof_ad_*) and the drawn stiffener frame on its back, a fat rim, a catwalk
        on brackets with a lattice rail, three floodlights, and two LATTICE LEGS: each a square
        column of four cutout cards painted with angle-iron lattice (roof_lattice), never thin
        members, plus fat back kickers and concrete pads. 8 x 4 m on 3.5 m legs (soda),
        8 x 4 m on 2.5 m legs (kape, a faded print), 6 x 3 m on 2.2 m legs (hardware, painted).
  roof_dish               a satellite dish (0.95 m, cream) on a mast with a foot weighed down by
        hollow blocks: a thick bowl, a fat feed arm and the LNB.
  roof_cell_mast          a cell site: an 11 m lattice mast (cutout cards, tapering), a platform
        with three chunky panel antennas and two microwave drums, a fat feeder bundle down the
        mast, a red beacon, and a galvanised equipment cabinet at its foot.
  roof_garden_a / _b      potted gardens: drum halves, terracotta pots and paint buckets on a
        plank shelf or in a row, planted with shrubs and strap-leaf plants from the TREES KIT's own
        leaf cards (tools/author_ilalim_trees.py foliage() and lily_clump(): the owner-approved
        Lagoon round leaf and blade), two tints, custom clump normals.

THE HOUSE STYLE (KANTO_DESIGN_GUIDE.md sections 2 to 5, LAGOON_REWORK_GUIDE.md section 2), through
the prop kit's PBuf (tools/author_ilalim_props.py): chunky members (the thinnest real member is the
9 cm laundry post and the 4 cm laundry line), live bevels with hardened normals, no coplanar
faces, world UVs at 2 m, UVGrime and UVSplash grime maps. Lattice, grilles, veneer and stitching
are DRAWN (tools/author_ilalim_textures_rooftops.py). Leaves are the tree kit's cards.

ROLE HUES: nothing near #f87020 or #0080e8. NO BLUE TANKS: black, dark green, cream and galvanised.
The GI is red oxide and faded green, the blocks mint (yellow-green), rose and cream.

UNITY (ILALIM-1.4): the lattice and the leaves are cutout materials; the beacon is emissive. The
placements are empties with linked children, as in the trees kit.
"""
import json
import math
import random
import sys
import zlib
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

sys.path.insert(0, str(Path(__file__).resolve().parent))
import author_ilalim_props as P                          # noqa: E402  (PBuf, materials, small props)
import author_ilalim_trees as T                          # noqa: E402  (leaf cards, read-only)
from author_ilalim_props import PBuf, facing, catmull, collection, rz   # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource" / "ilalim"
TEXTURES = SOURCE / "textures"
PREVIEWS = ROOT / "Logs" / "ilalim-blender"
UP = Vector((0, 0, 1))

ROOF_MATERIALS = {"eastside": "east_roof", "heritage": "heritage_flat_roof"}
OCCLUDER_KITS = ["lrt_kit", "sarisari"]
SKIP = ("prototype", "review", "stand-in", "kit (", "(source", "(place on piers)")
EYES = [Vector(v) for v in ((0, -9, 1.25), (0, 4, 1.25), (0, 0, 1.25), (-9.5, -4, 1.46), (9.3, 2, 1.46),
                            (0, 14, 1.25), (0, -16, 1.25))]
SARI_LOT = (11.8, 22.8, 38.3, 47.3)
GUIDEWAY_X = 6.0
MIN_ROOF = 12.0
EDGE = 0.35                    # footprint margin and edge clearance

# This kit's materials, in the prop kit's format:
# name: (texture or None, tint, roughness, metallic, emission, grime)
P.M.update({
    "roof_plywood":      ("roof_plywood", None, 0.85, 0.0, 0, True),
    "roof_gi_red":       ("roof_gi", (0.62, 0.27, 0.22), 0.5, 0.25, 0, False),
    "roof_gi_green":     ("roof_gi", (0.45, 0.55, 0.36), 0.5, 0.25, 0, False),
    "roof_gi_plain":     ("roof_gi", (0.80, 0.81, 0.79), 0.45, 0.35, 0, False),
    "roof_block_mint":   ("roof_block", (0.76, 0.87, 0.66), 0.9, 0.0, 0, True),
    "roof_block_rose":   ("roof_block", (0.95, 0.69, 0.64), 0.9, 0.0, 0, True),
    "roof_block_cream":  ("roof_block", (0.93, 0.89, 0.76), 0.9, 0.0, 0, True),
    "roof_block_grey":   ("roof_block", (0.72, 0.71, 0.68), 0.9, 0.0, 0, True),
    "roof_render":       ("roof_render", None, 0.9, 0.0, 0, True),
    "roof_tank_black":   ("roof_tank", (0.12, 0.12, 0.12), 0.45, 0.0, 0, True),
    "roof_tank_green":   ("roof_tank", (0.18, 0.32, 0.20), 0.45, 0.0, 0, True),
    "roof_tank_cream":   ("roof_tank", (0.88, 0.83, 0.67), 0.45, 0.0, 0, True),
    "roof_galv":         ("roof_galv", None, 0.45, 0.5, 0, True),
    "roof_steel_oxide":  ("prop_paint", (0.47, 0.22, 0.18), 0.6, 0.15, 0, True),
    "roof_steel_grey":   ("prop_paint", (0.47, 0.48, 0.46), 0.6, 0.2, 0, True),
    "roof_steel_board":  ("prop_paint", (0.55, 0.56, 0.54), 0.6, 0.2, 0, True),
    "roof_steel_maroon": ("prop_paint", (0.42, 0.14, 0.16), 0.55, 0.15, 0, True),
    "roof_ad_soda":      ("roof_ad_soda", None, 0.75, 0.0, 0, False),
    "roof_ad_kape":      ("roof_ad_kape", None, 0.75, 0.0, 0, False),
    "roof_ad_hardware":  ("roof_ad_hardware", None, 0.75, 0.0, 0, False),
    "roof_board_back":   ("roof_board_back", None, 0.6, 0.2, 0, False),
    "roof_cloth":        ("roof_cloth", None, 0.9, 0.0, 0, False),
    "roof_cloth_edge":   (None, (0.80, 0.78, 0.73), 0.9, 0.0, 0, False),
    "roof_door":         ("roof_door", None, 0.8, 0.0, 0, False),
    "roof_window":       ("roof_window", None, 0.5, 0.0, 0, False),
    "roof_pot_clay":     ("prop_plastic", (0.60, 0.31, 0.25), 0.8, 0.0, 0, True),
    "roof_pot_black":    ("prop_plastic", (0.15, 0.15, 0.15), 0.6, 0.0, 0, True),
    "roof_soil":         (None, (0.22, 0.16, 0.11), 0.95, 0.0, 0, False),
    "roof_beacon":       (None, (0.72, 0.10, 0.12), 0.4, 0.0, 0.8, False),
    "roof_dish":         ("prop_plastic", (0.86, 0.84, 0.78), 0.5, 0.1, 0, True),
})


def lattice_material():
    """The drawn angle-iron lattice: the roof_lattice drawing cut out by its alpha, dithered, lit
    from both sides (the cards are single quads)."""
    m = bpy.data.materials.get("roof_lattice")
    if m:
        return m
    m = bpy.data.materials.new("roof_lattice")
    if m.node_tree is None:
        m.use_nodes = True
    nodes, links = m.node_tree.nodes, m.node_tree.links
    bsdf = nodes["Principled BSDF"]
    uv = nodes.new("ShaderNodeUVMap")
    uv.uv_map = "UVMap"
    tex = nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(str(TEXTURES / "roof_lattice.png"), check_existing=True)
    links.new(uv.outputs["UV"], tex.inputs["Vector"])
    links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    links.new(tex.outputs["Alpha"], bsdf.inputs["Alpha"])
    bsdf.inputs["Roughness"].default_value = 0.6
    bsdf.inputs["Metallic"].default_value = 0.2
    m.use_backface_culling = False
    if hasattr(m, "surface_render_method"):
        m.surface_render_method = "DITHERED"
    else:
        m.blend_method = "CLIP"
    m.diffuse_color = (0.4, 0.38, 0.36, 1)
    return m


# ------------------------------------------------------------------ small geometry helpers

class RBuf(PBuf):
    """The prop kit's PBuf, plus: turned parts (lathes, tori, tubes) are shaded smooth (PBuf shades
    only small faces smooth, which left the tanks and pots faceted), and cheap square members and
    blocks whose soft edges come from the Bevel modifier alone (a quarter of the triangles of a
    filleted prism)."""

    def __init__(self, name, top=None):
        super().__init__(name, top)
        self.round = self.bm.faces.layers.int.new("round")

    def _tag(self, faces):
        for f in faces:
            f[self.round] = 1
        return faces

    def lathe(self, center, prof, mat, sides=14, mat_of=None, squash=(1.0, 1.0)):
        return self._tag(super().lathe(center, prof, mat, sides, mat_of, squash))

    def torus(self, center, R, r, mat, axis="z", seg=16, sides=8):
        return self._tag(super().torus(center, R, r, mat, axis, seg, sides))

    def tube(self, path, radius, mat, sides=8):
        return self._tag(super().tube(path, radius, mat, sides))

    def bar(self, p0, p1, hw, mat):
        """A square member from p0 to p1, `hw` half wide."""
        p0, p1 = Vector(p0), Vector(p1)
        d = (p1 - p0).normalized()
        side = d.cross(Vector((0, 1, 0)) if abs(d.y) < 0.9 else Vector((1, 0, 0))).normalized()
        up = side.cross(d).normalized()
        sq = ((-hw, -hw), (hw, -hw), (hw, hw), (-hw, hw))
        return self.loft([[p + side * x + up * y for x, y in sq] for p in (p0, p1)], mat)

    def cube(self, x0, x1, y0, y1, z0, z1, mat, rot=None):
        m = (rot or Matrix()).to_3x3()
        c = Vector(((x0 + x1) / 2, (y0 + y1) / 2, 0))
        ring = [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]
        rings = [[c + m @ (Vector((x, y, 0)) - c) + Vector((0, 0, z)) for x, y in ring] for z in (z0, z1)]
        return self.loft(rings, mat)

    def finish(self, collection, origin=(0, 0, 0), bevel=0.012, segments=1, smooth=True):
        obj = super().finish(collection, origin, bevel, segments, smooth)
        me = obj.data
        attr = me.attributes.get("round")
        if attr:
            for poly, a in zip(me.polygons, attr.data):
                if a.value:
                    poly.use_smooth = True
            me.attributes.remove(attr)
        dec = me.attributes.get("decal")
        if dec:
            me.attributes.remove(dec)
        return obj

def box(b, x0, x1, y0, y1, z0, z1, mat, r=0.02):
    b.rbox(((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2), (x1 - x0, y1 - y0, z1 - z0), mat, r=r)


def card(b, corners, mat, uvs):
    """One single-sided quad with its own UVs (a lattice face, a rail): never bevelled, since its
    edges are boundary edges."""
    vs = [b.bm.verts.new(b.M @ Vector(c)) for c in corners]
    f = b.bm.faces.new(vs)
    f.material_index = b.mi(mat)
    b.set_uvs(f, dict(zip(vs, uvs)))
    return f


def lattice_column(b, cx, cy, z0, z1, w0, w1=None, bay=0.6):
    """A square lattice column of four cutout cards, `w0` wide at the foot tapering to `w1`: the
    drawing's chords land on the corners, so it reads as an angle-iron tower from every side."""
    w1 = w0 if w1 is None else w1
    h = z1 - z0
    reps = max(1.0, round(h / bay * 2) / 2)
    corners = [(-1, -1), (1, -1), (1, 1), (-1, 1)]
    for k in range(4):
        (ax, ay), (bx, by) = corners[k], corners[(k + 1) % 4]
        p = [Vector((cx + ax * w0 / 2, cy + ay * w0 / 2, z0)), Vector((cx + bx * w0 / 2, cy + by * w0 / 2, z0)),
             Vector((cx + bx * w1 / 2, cy + by * w1 / 2, z1)), Vector((cx + ax * w1 / 2, cy + ay * w1 / 2, z1))]
        card(b, p, "roof_lattice", [(0, 0), (1, 0), (1, reps), (0, reps)])


def lattice_rail(b, x0, x1, y, z0, z1):
    """A lattice railing card along x: the drawing turned on its side, so its chords run along the
    top and bottom as rails."""
    ln = (x1 - x0) / (z1 - z0)
    card(b, [(x0, y, z0), (x1, y, z0), (x1, y, z1), (x0, y, z1)], "roof_lattice",
         [(0, 0), (0, ln), (1, ln), (1, 0)])


def pad(b, x, y, w=0.36, h=0.2, mat="roof_render"):
    b.cube(x - w / 2, x + w / 2, y - w / 2, y + w / 2, -0.02, h, mat)


def hollow_block(b, x, y, z, yaw=0.0, mat="roof_block_grey"):
    b.cube(x - 0.2, x + 0.2, y - 0.1, y + 0.1, z - 0.005, z + 0.19, mat, rot=rz(yaw))


# ------------------------------------------------------------------ tanks

def tank_body(b, cx, cy, z0, r, h, mat, lid):
    """A moulded polyethylene tank: a rounded foot, three fat moulded ribs, a domed shoulder and a
    screw lid."""
    prof = [(r * 0.86, 0.0), (r * 0.97, 0.05), (r, 0.14)]
    for k in range(2):
        zc = 0.33 * h + k * 0.34 * h
        prof += [(r, zc - 0.06), (r * 1.035, zc - 0.02), (r * 1.035, zc + 0.02), (r, zc + 0.06)]
    prof += [(r, 0.9 * h), (r * 0.9, h + 0.1 * r), (r * 0.62, h + 0.22 * r), (r * 0.36, h + 0.27 * r)]
    b.lathe((cx, cy, z0), prof, mat, sides=14)
    top = z0 + h + 0.27 * r
    b.lathe((cx, cy, top - 0.02), [(r * 0.36, 0.0), (r * 0.36, 0.07), (r * 0.3, 0.1)], lid, sides=14)
    return top + 0.08


def tank_stand_frame(b, half, height, mat):
    """Four fat splayed legs on concrete pads, two ring frames, one fat diagonal a side, a deck."""
    for sx in (-1, 1):
        for sy in (-1, 1):
            pad(b, sx * (half + 0.06), sy * (half + 0.06))
            b.bar((sx * (half + 0.06), sy * (half + 0.06), 0.1), (sx * half, sy * half, height), 0.065, mat)
    for z in (0.95, height - 0.06):
        s = half + 0.06 * (1 - z / height)
        for a in range(4):
            c = [(-1, -1), (1, -1), (1, 1), (-1, 1)]
            (ax, ay), (bx, by) = c[a], c[(a + 1) % 4]
            b.bar((ax * s, ay * s, z), (bx * s, by * s, z), 0.05, mat)
    for a in range(4):
        c = [(-1, -1), (1, -1), (1, 1), (-1, 1)]
        (ax, ay), (bx, by) = c[a], c[(a + 1) % 4]
        s0, s1 = half + 0.05, half + 0.02
        b.bar((ax * s0, ay * s0, 0.28), (bx * s1, by * s1, 0.92), 0.04, mat)
    b.cube(-half - 0.12, half + 0.12, -half - 0.12, half + 0.12, height - 0.02, height + 0.08, mat)


def tank_stand(col, name, tank_mat, stand_mat, galv=False):
    b = RBuf(name, top=4.0)
    H, half = 2.2, 0.72
    tank_stand_frame(b, half, H, stand_mat)
    if galv:
        r, h = 0.72, 1.45
        b.lathe((0, 0, H + 0.06), [(r * 0.98, 0.0), (r, 0.05), (r, h), (r * 1.03, h + 0.03), (r * 0.3, h + 0.32),
                                    (r * 0.18, h + 0.34)], "roof_galv", sides=14)
        for z in (0.45, 0.95):
            b.lathe((0, 0, H + 0.06 + z), [(r + 0.012, -0.03), (r + 0.012, 0.03)], "roof_steel_grey", sides=14)
        top = H + h + 0.4
    else:
        top = tank_body(b, 0, 0, H + 0.06, 0.74, 1.4, tank_mat, "prop_plastic_white")
    # The fat PVC down pipe from the tank's foot, down beside a leg, into the roof.
    b.tube([Vector((0.3, -0.45, H + 0.1)), Vector((0.3, -0.45, H - 0.25)), Vector((0.46, -0.66, H - 0.4)),
            Vector((0.46, -0.66, 0.35)), Vector((0.46, -0.9, 0.18)), Vector((0.46, -1.0, -0.03))], 0.045,
           "prop_plastic_white", sides=8)
    b.top = top
    return [b.finish(col, bevel=0.012, segments=1)]


def tank_pair(col, name):
    b = RBuf(name, top=2.0)
    for x in (-1.05, 0.0, 1.05):
        for y in (-0.45, 0.45):
            b.cube(x - 0.2, x + 0.2, y - 0.1, y + 0.1, -0.02, 0.38, "roof_block_grey")
    for y in (-0.42, 0.42):
        b.cube(-1.4, 1.4, y - 0.07, y + 0.07, 0.37, 0.52, "roof_steel_oxide")
    tank_body(b, -0.72, 0, 0.51, 0.56, 1.05, "roof_tank_black", "prop_plastic_white")
    tank_body(b, 0.72, 0, 0.51, 0.56, 1.05, "roof_tank_cream", "prop_plastic_maroon")
    b.tube([Vector((-0.72, -0.35, 0.56)), Vector((-0.72, -0.62, 0.45)), Vector((0.72, -0.62, 0.45)),
            Vector((0.72, -0.35, 0.56))], 0.04, "prop_plastic_white", sides=8)
    b.tube([Vector((0.0, -0.62, 0.45)), Vector((0.0, -0.8, 0.3)), Vector((0.0, -0.85, -0.03))], 0.04,
           "prop_plastic_white", sides=8)
    return [b.finish(col, bevel=0.012, segments=1)]


# ------------------------------------------------------------------ rooms

def shack(col, name):
    """Plywood walls on timber posts, a mono-pitch GI roof falling to the back, tyres on the roof."""
    b = RBuf(name, top=2.4)
    x0, x1, y0, y1 = -1.6, 1.6, -1.25, 1.25
    zf, zb = 2.3, 1.98                       # wall tops, front and back

    def ztop(y):
        return zf + (zb - zf) * (y - y0) / (y1 - y0)

    t = 0.05
    # Walls: front around the door, back, and the two sloped sides, each a lofted slab.
    dx0, dx1 = -1.05, -0.2
    for xa, xb in ((x0, dx0), (dx1, x1)):
        box(b, xa, xb, y0, y0 + t, -0.015, zf + 0.01, "roof_plywood", r=0.01)
    box(b, dx0 - 0.01, dx1 + 0.01, y0 + 0.005, y0 + t, 2.0, zf + 0.01, "roof_plywood", r=0.01)
    box(b, x0, x1, y1 - t, y1, -0.015, zb + 0.01, "roof_plywood", r=0.01)
    for xa, xb in ((x0, x0 + t), (x1 - t, x1)):
        prof = [(y0 + 0.01, -0.015), (y1 - 0.01, -0.015), (y1 - 0.01, zb + 0.01), (y0 + 0.01, zf + 0.01)]
        b.loft([[Vector((xa, y, z)) for y, z in prof], [Vector((xb, y, z)) for y, z in prof]], "roof_plywood")
    # Timber corner posts and a head plate, proud of the plywood.
    for x in (x0 - 0.02, x1 + 0.02):
        for y in (y0 - 0.02, y1 + 0.02):
            box(b, x - 0.06, x + 0.06, y - 0.06, y + 0.06, -0.015, ztop(y) + 0.02, "prop_wood_dark", r=0.02)
    # The door, set 2 cm back in its opening, and its frame.
    b.panel(facing("-y", Vector(((dx0 + dx1) / 2, y0 + 0.035, 1.0))), dx1 - dx0 + 0.02, 1.99, 0.03,
            "prop_wood_dark", "roof_door", r=0.01)
    for x in (dx0 - 0.05, dx1 + 0.05):
        box(b, x - 0.045, x + 0.045, y0 - 0.03, y0 + 0.02, -0.015, 2.06, "prop_wood_dark", r=0.015)
    box(b, dx0 - 0.1, dx1 + 0.1, y0 - 0.03, y0 + 0.02, 1.98, 2.08, "prop_wood_dark", r=0.015)
    # The grilled window on the front, proud of the plywood with a timber frame.
    b.panel(facing("-y", Vector((0.75, y0 - 0.012, 1.45))), 1.0, 0.8, 0.03, "prop_wood_dark", "roof_window", r=0.01)
    box(b, 0.2, 1.3, y0 - 0.08, y0 + 0.01, 1.0, 1.06, "prop_wood_dark", r=0.015)
    # The GI roof: one sheet with an overhang, on the wall tops.
    rx0, rx1, ry0, ry1 = x0 - 0.3, x1 + 0.3, y0 - 0.4, y1 + 0.3
    bottom = [Vector((x, y, ztop(y) - 0.005)) for x, y in ((rx0, ry0), (rx1, ry0), (rx1, ry1), (rx0, ry1))]
    b.loft([bottom, [v + Vector((0, 0, 0.05)) for v in bottom]], "roof_gi_red")
    # Two tyres and two hollow blocks weighing the sheet down (sunk 1.5 cm into it).
    for x, y in ((-0.8, 0.1), (0.9, -0.5)):
        b.torus((x, y, ztop(y) + 0.045 + 0.1 - 0.015), 0.27, 0.1, "prop_rubber", seg=16, sides=8)
    hollow_block(b, 0.1, 0.6, ztop(0.6) + 0.03, 0.3)
    hollow_block(b, -1.4, -0.9, ztop(-0.9) + 0.03, -0.2)
    # Outside: a monobloc chair and a cream water drum with a basin lid.
    P.monobloc(b, Vector((1.1, -1.95, -0.012)), 110, "prop_plastic_white")
    b.lathe((-1.95, -0.5, -0.012), [(0.26, 0.0), (0.29, 0.04), (0.29, 0.8), (0.27, 0.84), (0.2, 0.84)],
            "prop_plastic_cream", sides=14)
    b.lathe((-1.95, -0.5, 0.82), [(0.27, 0.0), (0.3, 0.05), (0.26, 0.08)], "prop_plastic_maroon", sides=14)
    return [b.finish(col, bevel=0.01, segments=1)]


def penthouse(col, name, wall):
    b = RBuf(name, top=2.75)
    x0, x1, y0, y1 = -2.1, 2.1, -1.7, 1.7
    box(b, x0, x1, y0, y1, -0.02, 2.6, wall, r=0.04)
    box(b, x0 - 0.18, x1 + 0.18, y0 - 0.18, y1 + 0.18, 2.58, 2.74, "roof_render", r=0.04)
    box(b, x0 - 0.08, x1 + 0.08, y0 - 0.08, y1 + 0.08, -0.02, 0.18, "roof_render", r=0.03)   # a plinth band
    # The steel door with its frame, and the GI awning on two fat brackets.
    dx = -1.1
    b.panel(facing("-y", Vector((dx, y0 - 0.015, 1.06))), 0.9, 2.0, 0.04, "roof_steel_maroon", "roof_steel_maroon",
            r=0.015)
    for x in (dx - 0.5, dx + 0.5):
        box(b, x - 0.05, x + 0.05, y0 - 0.05, y0 + 0.02, 0.16, 2.12, "roof_block_cream", r=0.015)
    box(b, dx - 0.55, dx + 0.55, y0 - 0.05, y0 + 0.02, 2.06, 2.16, "roof_block_cream", r=0.015)
    ax0, ax1, zi, zo, yo = dx - 0.85, dx + 0.85, 2.42, 2.12, y0 - 0.95
    a = [Vector((ax0, y0 + 0.01, zi)), Vector((ax1, y0 + 0.01, zi)), Vector((ax1, yo, zo)), Vector((ax0, yo, zo))]
    b.loft([a, [v + Vector((0, 0, 0.04)) for v in a]], "roof_gi_green")
    for x in (ax0 + 0.12, ax1 - 0.12):
        b.prism((x, y0 + 0.01, 1.92), (x, yo + 0.12, zo + 0.02), 0.035, 0.03, "prop_steel_dark", r=0.012)
        b.prism((x, y0 + 0.01, zi + 0.01), (x, yo + 0.12, zo + 0.02), 0.03, 0.03, "prop_steel_dark", r=0.012)
    # The window, proud with a cream frame, and a window aircon through the side wall.
    b.panel(facing("-y", Vector((0.85, y0 - 0.012, 1.55))), 1.2, 1.0, 0.03, "roof_block_cream", "roof_window",
            r=0.01)
    box(b, 0.19, 1.51, y0 - 0.1, y0 + 0.01, 0.97, 1.05, "roof_block_cream", r=0.015)
    ac = Vector((x1 + 0.2, 0.4, 1.7))
    b.rbox(ac, (0.46, 0.66, 0.44), "prop_plastic_cream", r=0.04)
    for k in range(4):
        b.rbox(ac + Vector((0.235, 0, 0.13 - k * 0.07)), (0.03, 0.52, 0.03), "prop_slot", r=0.01)
    return [b.finish(col, bevel=0.015, segments=1)]


def bulkhead(col, name):
    b = RBuf(name, top=2.5)
    x0, x1, y0, y1 = -1.1, 1.1, -1.5, 1.5
    box(b, x0, x1, y0, y1, -0.02, 2.35, "roof_render", r=0.04)
    rx0, rx1, ry0, ry1 = x0 - 0.15, x1 + 0.15, y0 - 0.25, y1 + 0.15

    def z(y):
        return 2.52 + (2.28 - 2.52) * (y - ry0) / (ry1 - ry0)

    bottom = [Vector((x, y, z(y) - 0.2)) for x, y in ((rx0, ry0), (rx1, ry0), (rx1, ry1), (rx0, ry1))]
    b.loft([bottom, [v + Vector((0, 0, 0.18)) for v in bottom]], "roof_render")
    b.panel(facing("-y", Vector((0.0, y0 - 0.015, 1.02))), 0.9, 1.98, 0.04, "roof_steel_maroon", "roof_steel_maroon",
            r=0.015)
    box(b, -0.55, 0.55, y0 - 0.05, y0 + 0.02, 1.98, 2.08, "roof_block_grey", r=0.015)
    b.rbox((0.0, y0 - 0.1, 2.2), (0.28, 0.2, 0.16), "prop_steel_dark", r=0.03)
    b.rbox((0.0, y0 - 0.2, 2.17), (0.22, 0.02, 0.08), "prop_lamp", r=0.008)
    vent = [Vector((0.5, 0.6, z(0.6) - 0.1)), Vector((0.5, 0.6, z(0.6) + 0.55)), Vector((0.5, 0.45, z(0.6) + 0.72)),
            Vector((0.5, 0.25, z(0.6) + 0.6))]
    b.tube(vent, 0.07, "roof_steel_grey", sides=10)
    return [b.finish(col, bevel=0.015, segments=1)]


# ------------------------------------------------------------------ laundry

GARMENTS = {
    # outline in units of (width, height), from the top centre down; cell in the roof_cloth atlas
    "shirt":   ([(-0.5, -0.02), (-0.2, 0.0), (0.2, 0.0), (0.5, -0.02), (0.5, -0.32), (0.3, -0.27), (0.3, -1.0),
                 (-0.3, -1.0), (-0.3, -0.27), (-0.5, -0.32)], (0.62, 0.72)),
    "sando":   ([(-0.3, 0.0), (-0.14, 0.0), (-0.1, -0.14), (0.1, -0.14), (0.14, 0.0), (0.3, 0.0), (0.42, -0.3),
                 (0.42, -1.0), (-0.42, -1.0), (-0.42, -0.3)], (0.5, 0.66)),
    "towel":   ([(-0.5, 0.0), (0.5, 0.0), (0.5, -1.0), (-0.5, -1.0)], (0.55, 0.85)),
    "duster":  ([(-0.3, 0.0), (0.3, 0.0), (0.36, -0.25), (0.5, -1.0), (-0.5, -1.0), (-0.36, -0.25)], (0.62, 1.05)),
    "blanket": ([(-0.5, 0.0), (0.5, 0.0), (0.5, -1.0), (-0.5, -1.0)], (1.35, 1.1)),
    "shorts":  ([(-0.5, 0.0), (0.5, 0.0), (0.52, -1.0), (0.08, -1.0), (0.0, -0.42), (-0.08, -1.0), (-0.52, -1.0)],
                (0.48, 0.52)),
    "pillow":  ([(-0.5, 0.0), (0.5, 0.0), (0.5, -1.0), (-0.5, -1.0)], (0.55, 0.38)),
}
CELLS = {"shirt_maroon": 0, "shirt_mustard": 1, "sando": 2, "towel": 3, "duster": 4, "blanket": 5, "shorts": 6,
         "pillow": 7}


def garment(b, kind, cell, top, y, sway_deg, thick=0.014):
    """One hung garment: its outline as a 14 mm slab in the x-z plane at `y`, hanging from `top`
    (a point on the line), swayed about the line. Both faces carry the atlas cell."""
    outline, (w, h) = GARMENTS[kind]
    s = math.radians(sway_deg)
    ex = Vector((1, 0, 0))
    ez = Vector((0, math.sin(s), math.cos(s)))       # 'up' of the garment, swayed about x
    ey = ex.cross(ez)
    o = Vector((top.x, y, top.z + 0.012))
    rings = [[o + ex * (u * w) + ez * (v * h) + ey * off for u, v in outline] for off in (-thick / 2, thick / 2)]
    faces = b.loft(rings, "roof_cloth_edge")
    r, c = divmod(cell, 4)
    for f in faces[-2:]:
        f.material_index = b.mi("roof_cloth")
        uvs = {}
        for v in f.verts:
            d = b.M.inverted() @ v.co - o
            u = max(-0.49, min(0.49, d.dot(ex) / w))
            vv = max(-0.98, min(-0.01, d.dot(ez) / h))
            uvs[v] = ((c + u + 0.5) / 4, 1 - (r - vv) / 2)
        b.set_uvs(f, uvs)


def laundry(col, name, length, lines, seed):
    """`lines`: for each of the two lines, a list of garment keys hung along it in order."""
    rng = random.Random(seed)
    b = RBuf(name, top=1.95)
    xa, xb = -length / 2, length / 2
    for x in (xa, xb):
        b.lathe((x, 0, -0.015), [(0.19, 0.0), (0.21, 0.3), (0.19, 0.3)], "roof_pot_black" if x < 0 else "prop_plastic_red",
                sides=12)
        b.lathe((x, 0, 0.26), [(0.18, 0.0), (0.18, 0.02), (0.0, 0.03)], "prop_concrete", sides=12)
        b.prism((x, 0, 0.1), (x + rng.uniform(-0.03, 0.03), 0, 1.95), 0.045, 0.042, "roof_steel_grey", r=0.02)
        b.prism((x, -0.45, 1.9), (x, 0.45, 1.9), 0.04, 0.04, "roof_steel_grey", r=0.018)
    for li, y in enumerate((-0.38, 0.38)):
        sag = 0.16 + 0.04 * li
        pts = [Vector((xa + (xb - xa) * t, y, 1.9 - sag * 4 * t * (1 - t))) for t in (0, 0.25, 0.5, 0.75, 1)]
        path = catmull(pts, per=4)
        b.tube(path, 0.02, "prop_rope", sides=6)
        x = xa + 0.3
        for key in lines[li]:
            kind = key.split("_")[0]
            w = GARMENTS[kind][1][0]
            if x + w > xb - 0.25:
                break
            cx = x + w / 2
            t = (cx - xa) / (xb - xa)
            z = 1.9 - sag * 4 * t * (1 - t)
            garment(b, kind, CELLS[key], Vector((cx, y, z)), y, rng.uniform(-7, 7))
            x += w + rng.uniform(0.06, 0.16)
    return [b.finish(col, bevel=0.004, segments=1)]


# ------------------------------------------------------------------ billboards

def billboard(col, name, ad, w, h, leg):
    b = RBuf(name, top=leg + h)
    zc = leg + h / 2
    # The board: the ad board in front, the stiffened back board (4 cm smaller) behind it.
    b.panel(facing("-y", Vector((0, 0.0, zc))), w, h, 0.1, "roof_steel_board", ad, r=0.02)
    b.panel(facing("+y", Vector((0, 0.07, zc))), w - 0.06, h - 0.06, 0.1, "roof_steel_board", "roof_board_back",
            r=0.02)
    # A fat rim round the face, proud of it.
    t = 0.075
    y = -0.06
    b.prism((-w / 2 - 0.02, y, leg - 0.02), (w / 2 + 0.02, y, leg - 0.02), t, t, "roof_steel_board", r=0.02)
    b.prism((-w / 2 - 0.02, y, leg + h + 0.02), (w / 2 + 0.02, y, leg + h + 0.02), t, t, "roof_steel_board", r=0.02)
    for x in (-w / 2 - 0.02, w / 2 + 0.02):
        b.prism((x, y, leg - 0.04), (x, y, leg + h + 0.04), t, t, "roof_steel_board", r=0.02)
    # Two lattice legs behind, beams into the back board, fat kickers, concrete pads.
    lx = w * 0.3
    for x in (-lx, lx):
        pad(b, x, 0.55, 1.0, 0.25)
        lattice_column(b, x, 0.55, 0.2, leg + h * 0.75, 0.7)
        for z in (leg + 0.35, leg + h * 0.6):
            b.bar((x, 0.08, z), (x, 0.95, z), 0.07, "roof_steel_grey")
        pad(b, x, 2.3, 0.5, 0.18)
        b.bar((x, 0.92, leg * 0.8 + 0.3), (x, 2.3, 0.1), 0.065, "roof_steel_grey")
    # The catwalk on three brackets, its lattice rail, and three floodlights.
    box(b, -w / 2 - 0.1, w / 2 + 0.1, -0.78, -0.08, leg - 0.24, leg - 0.16, "roof_steel_grey", r=0.02)
    for x in (-w / 2 + 0.4, 0.0, w / 2 - 0.4):
        b.bar((x, -0.05, leg - 0.75), (x, -0.7, leg - 0.22), 0.04, "roof_steel_grey")
    lattice_rail(b, -w / 2 - 0.08, w / 2 + 0.08, -0.76, leg - 0.17, leg + 0.55)
    for x in (-w / 3, 0.0, w / 3):
        b.bar((x, -0.7, leg - 0.18), (x, -1.2, leg + 0.35), 0.035, "prop_steel_dark")
        rot = Matrix.Rotation(math.radians(-35), 4, "X")
        b.rbox((x, -1.25, leg + 0.4), (0.36, 0.22, 0.2), "prop_steel_dark", r=0.04, rot=rot)
    return [b.finish(col, bevel=0.012, segments=1)]


# ------------------------------------------------------------------ dish and mast

def dish(col, name):
    b = RBuf(name, top=1.7)
    # The foot: a steel cross weighed down by four hollow blocks, and the mast.
    for a in (0, 90):
        b.rbox((0, 0, 0.03), (1.1, 0.1, 0.08), "prop_steel_dark", r=0.02, rot=rz(a))
    for x, y in ((0.42, 0), (-0.42, 0), (0, 0.42), (0, -0.42)):
        hollow_block(b, x, y, 0.05, 90 if x else 0)
    b.prism((0, 0, 0.0), (0, 0, 1.25), 0.045, 0.045, "prop_steel_dark", r=0.015)
    saved = b.M
    b.M = saved @ Matrix.Translation((0, -0.12, 1.3)) @ Matrix.Rotation(math.radians(-58), 4, "X")
    b.lathe((0, 0, 0), [(0.06, 0.0), (0.3, 0.05), (0.47, 0.15), (0.49, 0.18), (0.45, 0.17), (0.28, 0.08),
                        (0.05, 0.03)], "roof_dish", sides=14)
    b.prism((0, -0.4, 0.14), (0, -0.05, 0.62), 0.028, 0.025, "roof_dish", r=0.01)
    b.rbox((0, -0.02, 0.66), (0.1, 0.12, 0.16), "prop_plastic_white", r=0.03)
    b.rbox((0, 0.02, -0.05), (0.2, 0.2, 0.12), "prop_steel_dark", r=0.03)
    b.M = saved
    return [b.finish(col, bevel=0.01, segments=1)]


def cell_mast(col, name):
    b = RBuf(name, top=11.6)
    H = 11.0
    box(b, -0.75, 0.75, -0.75, 0.75, -0.02, 0.25, "roof_render", r=0.04)
    lattice_column(b, 0, 0, 0.2, H, 0.95, 0.6)
    box(b, -0.85, 0.85, -0.85, 0.85, H - 0.9, H - 0.8, "roof_steel_grey", r=0.02)
    for k in range(3):
        a = math.radians(90 + 120 * k)
        d = Vector((math.cos(a), math.sin(a), 0))
        p = d * 0.82 + Vector((0, 0, H - 0.1))
        b.prism(d * 0.2 + Vector((0, 0, H - 0.5)), d * 0.72 + Vector((0, 0, H - 0.5)), 0.035, 0.035, "roof_steel_grey",
                r=0.012)
        b.prism(d * 0.72 + Vector((0, 0, H - 0.95)), d * 0.72 + Vector((0, 0, H + 0.6)), 0.04, 0.04, "roof_steel_grey",
                r=0.015)
        b.rbox(p + Vector((0, 0, 0)), (0.34, 0.16, 1.35), "prop_plastic_white", r=0.05, rot=rz(math.degrees(a) - 90))
    for k, a in enumerate((25, 205)):
        d = Vector((math.cos(math.radians(a)), math.sin(math.radians(a)), 0))
        c = d * 0.55 + Vector((0, 0, H - 2.4 - k * 0.9))
        rot = rz(a - 90) @ Matrix.Rotation(math.radians(90), 4, "X")
        b.rbox(c, (0.66, 0.66, 0.26), "prop_plastic_cream", r=0.2, rot=rot)
        b.prism(d * 0.3 + Vector((0, 0, c.z)), d * 0.5 + Vector((0, 0, c.z)), 0.05, 0.05, "roof_steel_grey", r=0.015)
    b.blob(Vector((0, 0, H + 0.12)), (0.11, 0.11, 0.14), "roof_beacon")
    b.prism((0, 0, H - 0.85), (0, 0, H + 0.05), 0.05, 0.05, "roof_steel_grey", r=0.015)
    b.tube([Vector((0.3, 0.3, H - 0.8)), Vector((0.36, 0.36, 3.0)), Vector((0.46, 0.46, 1.5)),
            Vector((0.95, 0.4, 1.2))], 0.06, "prop_rubber", sides=8)
    box(b, 0.8, 2.0, -0.2, 0.55, -0.02, 1.55, "roof_galv", r=0.04)
    box(b, 0.75, 2.05, -0.25, 0.6, 1.53, 1.61, "roof_steel_grey", r=0.02)
    return [b.finish(col, bevel=0.012, segments=1)]


# ------------------------------------------------------------------ gardens

def pot(b, x, y, z, kind, r):
    """A container and its soil 4 cm under the rim: a drum half, a clay pot or a paint bucket."""
    if kind == "drum":
        prof = [(r * 0.95, 0.0), (r, 0.04), (r, 0.42), (r * 1.04, 0.45), (r * 0.94, 0.45)]
        mat, h = "roof_pot_black", 0.45
    elif kind == "clay":
        prof = [(r * 0.7, 0.0), (r * 0.9, 0.3), (r * 1.08, 0.36), (r * 1.08, 0.42), (r * 0.96, 0.42)]
        mat, h = "roof_pot_clay", 0.42
    else:
        prof = [(r * 0.85, 0.0), (r, 0.34), (r * 1.03, 0.36), (r * 0.95, 0.36)]
        mat, h = "prop_plastic_white", 0.36
    b.lathe((x, y, z), prof, mat, sides=14)
    b.lathe((x, y, z + h - 0.06), [(prof[-1][0] * 0.98, 0.0), (prof[-1][0] * 0.98, 0.01), (0.0, 0.02)], "roof_soil",
            sides=14)
    return z + h - 0.04


def garden(col, name, layout, seed):
    """`layout`: (x, y, container, radius, plant) on a plank shelf (shelf=True) or on the roof."""
    rng = random.Random(seed)
    b = RBuf(name + "_pots", top=0.8)
    leaves = T.Mesh()
    shelf_z = 0.0
    if layout["shelf"]:
        x0, x1 = layout["shelf"]
        for x in (x0 + 0.2, (x0 + x1) / 2, x1 - 0.2):
            for y in (-0.22, 0.22):
                b.cube(x - 0.1, x + 0.1, y - 0.2, y + 0.2, -0.02, 0.2, "roof_block_grey")
        box(b, x0, x1, -0.35, 0.35, 0.19, 0.25, "prop_wood", r=0.015)
        shelf_z = 0.245
    for x, y, kind, r, plant in layout["pots"]:
        z0 = shelf_z if (layout["shelf"] and abs(y) < 0.4) else -0.015
        soil = pot(b, x, y, z0, kind, r)
        if plant == "shrub":
            s = r * rng.uniform(1.4, 1.8)
            T.foliage(leaves, "shrub", Vector((x, y, soil + s * 0.8)), (s, s, s * 0.85), 0.2, rng, floor_z=soil + 0.02,
                      cover=2.0)
        else:
            T.lily_clump(leaves, rng, (x, y, soil), scale=r * 2.2, flowers=0)
    objs = [b.finish(col, bevel=0.01, segments=1)]
    me = leaves.build(name + "_leaves")
    o = bpy.data.objects.new(name + "_leaves", me)
    col.objects.link(o)
    objs.append(o)
    return objs


# ------------------------------------------------------------------ the prototypes

def prototypes(col):
    """Every prototype: kind -> (list of objects, plan half extents (x, y) incl. overhangs, height)."""
    lattice_material()
    kit = {}
    kit["tank_black"] = (tank_stand(col, "roof_tank_stand_black", "roof_tank_black", "roof_steel_oxide"), (1.0, 1.1), 4.0)
    kit["tank_green"] = (tank_stand(col, "roof_tank_stand_green", "roof_tank_green", "roof_steel_grey"), (1.0, 1.1), 4.0)
    kit["tank_cream"] = (tank_stand(col, "roof_tank_stand_cream", "roof_tank_cream", "roof_steel_oxide"), (1.0, 1.1), 4.0)
    kit["tank_galv"] = (tank_stand(col, "roof_tank_stand_galv", None, "roof_steel_grey", galv=True), (1.0, 1.1), 4.0)
    kit["tank_pair"] = (tank_pair(col, "roof_tank_pair"), (1.45, 0.9), 2.0)
    kit["shack"] = (shack(col, "roof_shack"), (2.3, 2.3), 2.5)
    kit["penthouse_mint"] = (penthouse(col, "roof_penthouse_mint", "roof_block_mint"), (2.55, 2.7), 2.8)
    kit["penthouse_rose"] = (penthouse(col, "roof_penthouse_rose", "roof_block_rose"), (2.55, 2.7), 2.8)
    kit["bulkhead"] = (bulkhead(col, "roof_bulkhead"), (1.3, 1.8), 2.6)
    kit["laundry_a"] = (laundry(col, "roof_laundry_a", 4.4,
                                [["shirt_maroon", "towel", "sando", "shirt_mustard", "pillow", "shorts"],
                                 ["blanket", "duster", "towel", "shirt_maroon"]], 31), (2.45, 0.6), 2.0)
    kit["laundry_b"] = (laundry(col, "roof_laundry_b", 3.4,
                                [["duster", "shirt_mustard", "shorts", "sando"],
                                 ["towel", "blanket", "pillow"]], 32), (1.95, 0.6), 2.0)
    kit["billboard_soda"] = (billboard(col, "roof_billboard_soda", "roof_ad_soda", 8.0, 4.0, 3.5), (4.15, 1.9), 7.5)
    kit["billboard_kape"] = (billboard(col, "roof_billboard_kape", "roof_ad_kape", 8.0, 4.0, 2.5), (4.15, 1.9), 6.5)
    kit["billboard_hardware"] = (billboard(col, "roof_billboard_hardware", "roof_ad_hardware", 6.0, 3.0, 2.2),
                                 (3.15, 1.9), 5.2)
    kit["dish"] = (dish(col, "roof_dish"), (0.65, 0.75), 1.8)
    kit["cell_mast"] = (cell_mast(col, "roof_cell_mast"), (1.1, 1.0), 11.3)
    kit["garden_a"] = (garden(col, "roof_garden_a", {"shelf": (-1.2, 1.2), "pots": [
        (-0.9, 0.0, "clay", 0.2, "shrub"), (-0.35, 0.05, "bucket", 0.17, "strap"), (0.2, -0.05, "clay", 0.22, "shrub"),
        (0.8, 0.02, "bucket", 0.17, "strap"), (1.5, -0.1, "drum", 0.3, "shrub")]}, 41), (1.9, 0.75), 1.4)
    kit["garden_b"] = (garden(col, "roof_garden_b", {"shelf": None, "pots": [
        (-1.0, 0.0, "drum", 0.3, "shrub"), (-0.3, 0.1, "drum", 0.3, "strap"), (0.4, -0.05, "drum", 0.3, "shrub"),
        (1.0, 0.25, "clay", 0.2, "strap"), (1.1, -0.35, "bucket", 0.17, "shrub")]}, 42), (1.5, 0.8), 1.3)
    return kit


FAMILIES = {
    "tank": ["tank_black", "tank_green", "tank_cream", "tank_black", "tank_galv"],
    "billboard": ["billboard_soda", "billboard_kape", "billboard_hardware"],
    "penthouse": ["penthouse_mint", "penthouse_rose"],
    "laundry": ["laundry_a", "laundry_b"],
    "garden": ["garden_a", "garden_b"],
}


# ------------------------------------------------------------------ reading the roofs

def link_kit(name):
    path = SOURCE / f"{name}.blend"
    for attempt in range(3):
        try:
            with bpy.data.libraries.load(str(path), link=True) as (src, dst):
                dst.collections = list(src.collections)
            break
        except (OSError, RuntimeError) as e:          # the east kit may be mid-rewrite
            print("[ilalim-roof] retry loading", path, e)
    linked = [c for c in dst.collections if c is not None]
    children = {ch.name for c in linked for ch in c.children}
    kept = []
    for c in linked:
        if c.name not in children and not any(s in c.name.lower() for s in SKIP):
            bpy.context.scene.collection.children.link(c)
            kept.append(c)
    return kept


def hidden_names():
    path = SOURCE / "landmarks.json"
    if not path.exists():
        return set()
    try:
        return set(json.loads(path.read_text(encoding="utf-8")).get("hide", []))
    except (OSError, ValueError, AttributeError):
        print("[ilalim-roof] landmarks.json unreadable, ignored")
        return set()


def is_hidden(name, hide):
    base = name[:-5] if name.endswith(" roof") else name
    return name in hide or base in hide or any(h.startswith(base + " ") for h in hide)


def read_world():
    """Link the kits, and return the flat roofs and one BVH of every triangle (for the placement
    and the court's sightlines)."""
    hide = hidden_names()
    kits = {k: link_kit(k) for k in list(ROOF_MATERIALS) + OCCLUDER_KITS}
    bpy.context.view_layer.update()
    verts, tris, roof_of_tri = [], [], []
    roofs = []
    seen = set()
    for kit, cols in kits.items():
        roof_mat = ROOF_MATERIALS.get(kit)
        for c in cols:
            for o in c.all_objects:
                if o.type != "MESH" or o.name in seen or is_hidden(o.name, hide):
                    continue
                seen.add(o.name)
                me = o.data
                mw = o.matrix_world
                me.calc_loop_triangles()
                base = len(verts)
                verts += [mw @ v.co for v in me.vertices]
                rmats = {i for i, m in enumerate(me.materials) if m and roof_mat and m.name.startswith(roof_mat)}
                face_roof = {}
                if rmats:
                    for p in me.polygons:
                        if p.material_index not in rmats:
                            continue
                        pts = [mw @ me.vertices[v].co for v in p.vertices]
                        area = abs(sum(a.x * b.y - b.x * a.y for a, b in zip(pts, pts[1:] + pts[:1]))) / 2
                        if area < MIN_ROOF or abs((mw.to_3x3() @ p.normal).normalized().z) < 0.98:
                            continue
                        face_roof[p.index] = len(roofs)
                        roofs.append({"name": o.name, "kit": kit, "poly": [(v.x, v.y) for v in pts],
                                      "z": sum(v.z for v in pts) / len(pts), "area": area})
                for t in me.loop_triangles:
                    tris.append(tuple(base + i for i in t.vertices))
                    roof_of_tri.append(face_roof.get(t.polygon_index, -1))
    bvh = BVHTree.FromPolygons(verts, tris, all_triangles=True)
    print(f"[ilalim-roof] {len(roofs)} flat roofs, {len(tris)} occluder triangles, hidden: {sorted(hide)}")
    return roofs, bvh, roof_of_tri


# ------------------------------------------------------------------ placement

def inside(pt, poly):
    x, y = pt
    c = False
    n = len(poly)
    for i in range(n):
        x1, y1 = poly[i]
        x2, y2 = poly[(i + 1) % n]
        if (y1 > y) != (y2 > y) and x < (x2 - x1) * (y - y1) / (y2 - y1) + x1:
            c = not c
    return c


def edge_dist(pt, poly):
    px, py = pt
    best = 1e9
    n = len(poly)
    for i in range(n):
        ax, ay = poly[i]
        bx, by = poly[(i + 1) % n]
        dx, dy = bx - ax, by - ay
        L = dx * dx + dy * dy
        t = 0.0 if L == 0 else max(0.0, min(1.0, ((px - ax) * dx + (py - ay) * dy) / L))
        best = min(best, math.hypot(px - ax - t * dx, py - ay - t * dy))
    return best


def main_axis(poly):
    best, ang = -1, 0.0
    n = len(poly)
    for i in range(n):
        ax, ay = poly[i]
        bx, by = poly[(i + 1) % n]
        L = math.hypot(bx - ax, by - ay)
        if L > best:
            best, ang = L, math.atan2(by - ay, bx - ax)
    return ang


def footprint(cx, cy, yaw, hx, hy, step=0.4):
    c, s = math.cos(yaw), math.sin(yaw)
    nx, ny = max(1, math.ceil(2 * hx / step)), max(1, math.ceil(2 * hy / step))
    pts = []
    for i in range(nx + 1):
        for j in range(ny + 1):
            u, v = -hx + 2 * hx * i / nx, -hy + 2 * hy * j / ny
            pts.append((cx + u * c - v * s, cy + u * s + v * c))
    return pts


def in_obb(pt, ob):
    cx, cy, yaw, hx, hy = ob
    dx, dy = pt[0] - cx, pt[1] - cy
    c, s = math.cos(yaw), math.sin(yaw)
    u, v = dx * c + dy * s, -dx * s + dy * c
    return abs(u) <= hx and abs(v) <= hy


def fits(roof, ridx, bvh, roof_of_tri, cx, cy, yaw, hx, hy, placed):
    hx, hy = hx + EDGE, hy + EDGE
    for pt in footprint(cx, cy, yaw, hx, hy):
        x, y = pt
        if abs(x) < GUIDEWAY_X or (SARI_LOT[0] - 0.5 < x < SARI_LOT[1] + 0.5 and SARI_LOT[2] - 0.5 < y < SARI_LOT[3] + 0.5):
            return False
        if not inside(pt, roof["poly"]) or edge_dist(pt, roof["poly"]) < EDGE:
            return False
        if any(in_obb(pt, ob) for ob in placed):
            return False
        hit = bvh.ray_cast(Vector((x, y, roof["z"] + 300)), Vector((0, 0, -1)), 320)
        if hit[0] is None or roof_of_tri[hit[2]] != ridx or abs(hit[0].z - roof["z"]) > 0.05:
            return False
    for ob in placed:                                   # the old boxes' corners against the new one
        for pt in footprint(*ob, step=10.0):
            if in_obb(pt, (cx, cy, yaw, hx, hy)):
                return False
    return True


def seen_by(bvh, p):
    n = 0
    for e in EYES:
        d = p - e
        dist = d.length
        hit = bvh.ray_cast(e, d.normalized(), dist - 0.4)
        if hit[0] is None:
            n += 1
    return n


def plan(roofs, bvh, roof_of_tri, kit):
    """Decide every placement: [(kind, roof index, x, y, z, yaw, name)]."""
    out = []
    counts = {"cell_mast": 0, "billboard": 0}
    order = sorted(range(len(roofs)), key=lambda i: min(math.hypot(x, y) for x, y in roofs[i]["poly"]))
    ads = list(FAMILIES["billboard"])
    for ridx in order:
        roof = roofs[ridx]
        poly = roof["poly"]
        d = min(math.hypot(x, y) for x, y in poly)
        rng = random.Random(zlib.crc32(roof["name"].encode()))
        if d < 60:
            n, tier = 6, 0
        elif d < 100:
            n, tier = 4, 1
        elif d < 150:
            n, tier = 2, 2
        elif d < 220:
            n, tier = (1 if rng.random() < 0.6 else 0), 3
        else:
            n, tier = (1 if rng.random() < 0.3 else 0), 4
        if tier == 0:
            n = max(n, min(10, int(roof["area"] / 75)))      # the big roofs by the court fill up
        n = min(n, max(1, int(roof["area"] / 55)))
        if n == 0:
            continue
        axis = main_axis(poly)
        xs, ys = [p[0] for p in poly], [p[1] for p in poly]
        cands = []
        step = 1.0
        x = min(xs) + 0.5
        while x < max(xs):
            y = min(ys) + 0.5
            while y < max(ys):
                if inside((x, y), poly) and edge_dist((x, y), poly) > 1.0:
                    cands.append((x + rng.uniform(-0.3, 0.3), y + rng.uniform(-0.3, 0.3)))
                y += step
            x += step
        if not cands:
            continue
        rng.shuffle(cands)
        placed = []
        wants = []
        if tier <= 2:
            tall = ["tank"]
            if roof["area"] > 180 and counts["billboard"] < 6:
                tall = ["billboard", "billboard", "tank"]
            if roof["z"] > 15 and counts["cell_mast"] < 2 and roof["area"] > 250 and tier <= 1:
                tall = ["cell_mast"]
            wants.append((rng.choice(tall), True))
            if tier == 0 and roof["area"] > 400:
                wants.append(("tank", True))                  # a second tall item on a big near roof
        pool = {0: ["tank", "tank", "tank_pair", "shack", "penthouse", "laundry", "laundry", "garden", "garden", "dish",
                    "bulkhead"],
                1: ["tank", "tank", "tank_pair", "shack", "penthouse", "laundry", "garden", "dish", "bulkhead"],
                2: ["tank", "tank_pair", "shack", "laundry", "dish", "garden"],
                3: ["tank", "tank", "tank_pair", "shack", "dish"],
                4: ["tank", "tank_pair", "dish"]}[tier]
        # Variety: at most two of a family on one roof, and at most three tanks of any sort (a row
        # of five tanks on the Astral podium read as a tank yard, context v3).
        def group(f):
            return "tanks" if f in ("tank", "tank_pair") else f

        while len(wants) < n:
            used = [group(f) for f, _ in wants]
            ok = [f for f in pool if used.count(group(f)) < (3 if group(f) == "tanks" else 2)]
            if not ok:
                break
            wants.append((rng.choice(ok), False))
        k_on_roof = 0
        for family, hero in wants:
            if family == "billboard":
                kind = ads[counts["billboard"] % len(ads)]
            elif family in ("penthouse", "laundry", "garden"):
                # Taken in turn across the map, so every variant is used about equally.
                counts[family] = counts.get(family, 0) + 1
                kind = FAMILIES[family][counts[family] % len(FAMILIES[family])]
            elif family in FAMILIES:
                kind = rng.choice(FAMILIES[family])
            else:
                kind = family
            objs, (hx, hy), height = kit[kind]
            yaws = [axis + k * math.pi / 2 for k in range(4)]
            if family == "billboard":
                # The face (local -Y) turned toward the court as squarely as the building allows.
                def facing_court(yw, cx, cy):
                    f = Vector((math.sin(yw), -math.cos(yw)))
                    to = Vector((-cx, -cy)).normalized()
                    return f.dot(to)
            best = None
            tries = cands[:80] if hero else cands[:40]
            for cx, cy in tries:
                ys_order = list(yaws)
                if family == "billboard":
                    ys_order.sort(key=lambda yw: -facing_court(yw, cx, cy))
                    ys_order = ys_order[:1]
                else:
                    rng.shuffle(ys_order)
                for yw in ys_order:
                    if not fits(roof, ridx, bvh, roof_of_tri, cx, cy, yw, hx, hy, placed):
                        continue
                    if hero:
                        score = seen_by(bvh, Vector((cx, cy, roof["z"] + height * 0.85))) - 0.01 * math.hypot(cx, cy)
                        if best is None or score > best[0]:
                            best = (score, cx, cy, yw)
                    else:
                        best = (0, cx, cy, yw)
                    break
                if best is not None and not hero:
                    break
            if best is None:
                continue
            score, cx, cy, yw = best
            if family == "billboard" and round(score + 0.01 * math.hypot(cx, cy)) < 2:
                # A billboard the court cannot see is wasted: put up a tank instead.
                wants.append(("tank", True))
                continue
            if hero:
                print(f"[ilalim-roof] hero {kind} on {roof['name']} (d {d:.0f} m, z {roof['z']:.1f}) at "
                      f"({cx:.1f}, {cy:.1f}), seen by {round(score + 0.01 * math.hypot(cx, cy))} of {len(EYES)} eyes")
            placed.append((cx, cy, yw, hx + 0.3, hy + 0.3))
            if family == "billboard":
                counts["billboard"] += 1
            if family == "cell_mast":
                counts["cell_mast"] += 1
            k_on_roof += 1
            out.append((kind, ridx, cx, cy, roof["z"] - 0.012, yw, f"roof {kind} {roof['name']} {k_on_roof}"))
    return out


def instance(parent, proto_objs, name, loc, yaw):
    e = bpy.data.objects.new(name, None)
    e.empty_display_type, e.empty_display_size = "PLAIN_AXES", 0.6
    e.location = loc
    e.rotation_euler = (0, 0, yaw)
    parent.objects.link(e)
    for p in proto_objs:
        o = p.copy()                                   # a linked duplicate: shares the mesh
        o.name = f"{name} {p.name}"
        o.parent = e
        o.location = (0, 0, 0)
        o.rotation_euler = (0, 0, 0)
        parent.objects.link(o)
    return e


# ------------------------------------------------------------------ review

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


LINEUP = [("tank_black", -24, 0), ("tank_green", -21, 0), ("tank_cream", -18, 0), ("tank_galv", -15, 0),
          ("tank_pair", -11.5, 0), ("shack", -6, 0), ("penthouse_mint", 0, 0), ("penthouse_rose", 6, 0),
          ("bulkhead", 11, 0), ("dish", 14.5, 0), ("cell_mast", 18.5, 0),
          ("laundry_a", -20, 9), ("laundry_b", -13, 9), ("garden_a", -6, 9), ("garden_b", -1, 9),
          ("billboard_soda", 6, 10), ("billboard_kape", 16, 10), ("billboard_hardware", 25, 10)]


def review(kit):
    col = collection("review lineup")
    me = bpy.data.meshes.new("review roof slab")
    x0, x1, y0, y1, z0, z1 = -30, 32, -6, 16, -0.4, 0.0
    me.from_pydata([(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0), (x0, y0, z1), (x1, y0, z1), (x1, y1, z1),
                    (x0, y1, z1)], [], [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6),
                                        (3, 0, 4, 7)])
    mat = bpy.data.materials.new("review roof slab")
    if mat.node_tree is None:
        mat.use_nodes = True
    mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.55, 0.54, 0.51, 1)
    me.materials.append(mat)
    col.objects.link(bpy.data.objects.new("review roof slab", me))
    for kind, x, y in LINEUP:
        instance(col, kit[kind][0], f"review {kind}", (x, y, 0), 0.0)
    return col


def preview(version, only=None):
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 1000
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    cam.data.clip_end = 1000
    scene.collection.objects.link(cam)
    scene.camera = cam
    shots = [
        ("lineup", (0, -40, 18), (0, 4, 2), 24),
        ("tanks_close", (-18, -9, 3.2), (-17, 0, 2.2), 26),
        ("rooms_close", (1, -12, 3.0), (2, 0, 1.5), 24),
        ("laundry_close", (-14, 2.5, 1.9), (-15, 9, 1.4), 26),
        ("billboards", (15, -8, 3.0), (15, 10, 5.5), 22),
        ("mast_close", (12, -8, 6), (18, 0, 6), 22),
        ("gardens_close", (-3.5, 5.5, 1.6), (-3.5, 9, 0.6), 26),
    ]
    for name, pos, tgt, lens in shots:
        if only and name not in only:
            continue
        path = PREVIEWS / f"roof_{name}_v{version}.png"
        if path.exists():
            print("[ilalim-roof] exists, skipped", path)
            continue
        pos, tgt = Vector(pos), Vector(tgt)
        cam.location, cam.data.lens = pos, lens
        cam.rotation_euler = (tgt - pos).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        print("[ilalim-roof] preview", path)


def triangle_count(kit, placements):
    dg = bpy.context.evaluated_depsgraph_get()
    per = {}
    for kind, (objs, _, _) in kit.items():
        n = 0
        for o in objs:
            ev = o.evaluated_get(dg)
            m = ev.to_mesh()
            m.calc_loop_triangles()
            n += len(m.loop_triangles)
            ev.to_mesh_clear()
        per[kind] = n
    total = sum(per[k] for k, *_ in placements)
    return per, total


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
    only = argv[argv.index("--only") + 1].split(",") if "--only" in argv else None
    bpy.ops.wm.read_factory_settings(use_empty=True)
    roofs, bvh, roof_of_tri = read_world()
    # Build this kit in a clean file: nothing of the linked kits is saved into rooftops.blend.
    bpy.ops.wm.read_factory_settings(use_empty=True)
    protos = collection("rooftops prototypes")
    kit = prototypes(protos)
    placements = plan(roofs, bvh, roof_of_tri, kit)
    placed = collection("rooftops (placed)")
    subs = {}
    for kind, ridx, x, y, z, yaw, name in placements:
        fam = next((f for f, ks in FAMILIES.items() if kind in ks), kind)
        if fam not in subs:
            subs[fam] = collection(f"rooftops {fam}", placed)
        instance(subs[fam], kit[kind][0], name, (x, y, z), yaw)
    protos.hide_render = True
    lay = bpy.context.view_layer.layer_collection.children[protos.name]
    lay.hide_viewport = True
    review_col = review(kit)
    lighting()
    SOURCE.mkdir(parents=True, exist_ok=True)
    out = SOURCE / "rooftops.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(out), compress=True, relative_remap=True)
    backup = SOURCE / "rooftops.blend1"
    if backup.exists():
        backup.unlink()
    per, total = triangle_count(kit, placements)
    tally = {}
    for kind, *_ in placements:
        tally[kind] = tally.get(kind, 0) + 1
    print(f"[ilalim-roof] saved {out}; {len(placements)} placements on {len({p[1] for p in placements})} roofs")
    print("[ilalim-roof] placed:", dict(sorted(tally.items())))
    print("[ilalim-roof] triangles per prototype:", per)
    print(f"[ilalim-roof] placed triangles (after bevel): {total}")
    if version:
        # Close-ups only: hide the placed collection so the lineup renders alone.
        bpy.context.view_layer.layer_collection.children[placed.name].exclude = True
        preview(version, only)


if __name__ == "__main__":
    main()
