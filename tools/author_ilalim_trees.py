"""Model the TREES AND PLANTING kit for the Ilalim ng Tulay rebuild, and plant it over the real layout.

  py -3 tools/author_ilalim_textures_trees.py              # paint the tree_ textures first
  blender -b --python tools/author_ilalim_trees.py -- [--preview N] [--shots a,b,...]

Writes ArtSource/ilalim/trees.blend. With --preview it also writes versioned renders to
Logs/ilalim-blender/tree_<shot>_vN.png, with the LRT-1 guideway linked from lrt_kit.blend and
the blockout's ground, buildings and Rizal Hall appended for context (renders only; neither is
saved into trees.blend).

THE PLACE. Taft Avenue at Padre Faura, Ermita: the UP Manila and PGH campus on the west, "a sea
of red roofs among trees" from the air (research.md section 4), huge umbrella rain trees over
the PGH frontage and the Supreme Court, small dense kerb trees along Padre Faura, palms in front
of Rizal Hall, and spider lilies in the Taft median planter under LRT-1.

THE SPECIES (each researched from photographs; the Commons links are at the end):
  * tree_raintree_0..2: RAIN TREE, "acacia" in Manila (Samanea saman). A short, massive trunk
    with chunky surface roots, forking low into four or five huge limbs that run out almost
    level and turn up at their ends, under a wide, layered umbrella of separate flattened
    clumps (the gaps between clumps show the limbs, as the photographs do). 16 to 21 m across,
    11 to 14 m tall. Bark: J, greyed.
  * tree_narra_0..1: NARRA (Pterocarpus indicus), the national tree. A taller, straighter trunk
    with a buttressed foot, three steep limbs, a rounded dome that droops at its rim, a lighter
    fresh green. About 12 m across, 13 to 15 m tall.
  * tree_mango_0..2: MANGO. A short trunk forking at under 2 m into a dense, low, dark green
    dome of mango whorls. 8 to 10 m across, 8 to 9 m tall.
  * tree_fig_0..1: the Padre Faura KERB TREE, a small-leaved fig (balete, Ficus benjamina): a
    slim, pale grey trunk and a dense round crown, 4 to 5 m across, about 6.5 m tall, standing in
    a concrete tree pit with its lower trunk LIME-WASHED white, as Manila's street trees are.
  * tree_manila_palm_0..1: MANILA PALM (Adonidia merrillii, literally the Manila palm), the
    palms in front of Rizal Hall: a slim ringed grey trunk, a smooth green crownshaft, and
    arching pinnate fronds. 5.5 to 7 m.
  * tree_royal_palm: ROYAL PALM: a tall grey trunk swelling a little at mid height, a long
    crownshaft, drooping fronds. 12 m.
  * tree_ixora_0..2: SANTAN (Ixora) shrubs, 0.9 to 1.2 m: glossy leaf cards with deep crimson
    flower-ball cards (never near offence orange #f87020).
  * tree_hedge: a 4 m run of clipped KAMUNING (Murraya) hedge, 1.0 m tall, 0.8 m deep.
  * tree_lily_0..2: SPIDER LILY clumps (Hymenocallis / Crinum), the Taft median planter's plant:
    arching strap leaves with a folded channel, and one or two white spider flowers on scapes.
    About 0.8 m tall and 1.1 m across. Origin at the SOIL TOP; the leaves start 5 cm under it.
  * tree_lily_bed: a 3 m x 0.8 m bed of seven lily clumps, one piece, for the median planter
    that the street kit builds (origin at the soil top, centred).
  * tree_pit: the concrete kerb tree pit of the Padre Faura pavement, 1.3 m square, hollow, the
    soil 3 cm above the pavement and 6 cm under the kerb top, so nothing shares a plane.

THE HOUSE STYLE, and how each rule is met:
  * FOLIAGE IS THE SETTLED KANTO METHOD (KANTO_DESIGN_GUIDE.md section 5): see-through leaf CARDS
    shingled on the surface of each clump, spread by a Fibonacci sphere, tips running down the
    surface, custom normals pointing out of the clump's centre so it shades as one soft ball, NO
    core, two tints per plant (light above, dark below), variation per PLANT (the shader's
    object random nudges value and hue) never per leaf. Trees are several clumps on limbs.
    Palm fronds and lily straps are the Lagoon's bent, V-folded cards, so they stay visible
    edge-on (tools/lagoon_cove_planting.py).
  * WOOD is Kanto's segmented, POLYGONAL limbs (7 sides on a trunk, 5 on a branch, flat shaded,
    a few per cent of hand-cut wobble), each limb its own tube with square-texel cylindrical
    UVs, a child limb starting buried inside its parent. Every wood, trunk, kerb and soil piece
    carries a live Bevel modifier with hardened normals. Leaf cards carry none: a bevel on a
    card only creases it and would overwrite its clump normals.
  * CHUNKY AND ORGANIC: thick limbs, few forks, lean and size jitter per tree, no twigs; the
    leaf drawings carry the detail.
  * NO TWO SURFACES SHARE A PLANE: trunks start 0.35 m under the ground, roots dip under it,
    limbs start inside their parents, lily leaves start under the soil, the pit's soil sits
    between the pavement and the kerb top.
  * GRIME AND PAINT ARE POSITIONAL: every trunk carries a second UV map, UVSplash (v = height
    above the ground / 2 m), through which tree_splash darkens the foot and, on the street figs
    and the PGH frontage rain trees, tree_limewash paints the lower 1.1 m white.

PLACEMENT (plant()), from the sightline-overridden layout (author_ilalim_blockout.sightline_override
on osm_layout.json), in Blender metres (X = game x east, Y = game z north):
  * every OSM tree node, then a jittered Poisson scatter over the campus lots, lawns and
    parking west of Taft (dense: the "sea of red roofs among trees") and a sparser one east;
  * a row of big rain trees along the PGH fence, whose canopies shade the west pavement;
  * figs in pits every ~10 m along both kerbs of Padre Faura;
  * Manila and royal palms and santan in front of Rizal Hall and round the Oblation;
  * kamuning hedges inside the PGH fence, with gaps.
  The rules every tree obeys:
  * no trunk inside a building footprint, on a street, its sidewalk band (except the kerb figs)
    or a driveway, and no canopy pushing into a building wall;
  * no trunk in the Taft corridor (|x| < 11.5), and no canopy over the LRT-1 deck (the canopy
    stays outside |x| 6.5);
  * THE RIZAL HALL SIGHTLINE STAYS OPEN: no canopy comes within 4 m (in plan) of the view lines
    from the court points (0, -9), (-8, 16), (8, 0) and (-8, -10) to the portico, the lines of
    the blockout's details();
  * the trees keep their distance from each other by canopy size.
  Every placement is an EMPTY named for its species, holding LINKED DUPLICATES of the
  prototype's objects: select any piece, Tab, edit, and every copy follows. The prototypes live
  in "kit (prototypes)" at the origin, hidden from renders.

OPEN (for the lead): the Unity export needs the UVSplash channel (or the overlays baked), and
the leaf materials need the per-object random nudge or a per-instance colour. The lily bed is
not placed: the median planter belongs to the street kit, so "median lilies" in the review
renders stand in a stand-in planter.

REFERENCES (Wikimedia Commons, studied only, never committed or copied):
  * Taft Avenue Landscape Vito Cruz LRT Station 03, and the Taft / Padre Faura photographs listed
    in docs/reports/ilalim-rework-2026-09-29/research.md sections 5 and 8.
  * File:Oldest Samanea saman Paco Park 03.jpg (CC BY-SA 4.0, P1898): the rain tree's form.
  * File:Hymenocallis littoralis, Negros Occ., Philippines 2.jpg (CC BY-SA 4.0, Paolobon140):
    the spider lily flower.
"""
import json
import math
import random
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector
from mathutils.geometry import normal as poly_normal

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools"))
import author_ilalim_blockout as B      # noqa: E402  read-only: layout, sightline override, contract numbers
import author_ilalim_lrt as L           # noqa: E402  read-only: fillet(), rounded_rect()

SOURCE = ROOT / "ArtSource" / "ilalim"
TEXTURES = SOURCE / "textures"
PREVIEWS = ROOT / "Logs" / "ilalim-blender"
UP = Vector((0, 0, 1))
BARK_TILE = 2.0          # tree_bark covers 2 m (Kanto's J tile)
CORRIDOR_X = 11.5        # no trunk inside this
DECK_CLEAR_X = 6.5       # no canopy inside this (the LRT-1 deck is 5.25 wide a side)
GROUND_Z = B.LOT_TOP     # campus lots and parking; trunks are sunk 0.35 m, so every ground class is met

# ------------------------------------------------------------------ materials

# Leaf pairs (light above, dark below), in linear RGB, multiplied into the greyscale drawing x 1.25.
LEAF_TINTS = {
    "raintree": ((0.30, 0.46, 0.12), (0.11, 0.25, 0.07)),
    "narra":    ((0.42, 0.58, 0.14), (0.17, 0.33, 0.07)),
    "mango":    ((0.20, 0.37, 0.10), (0.07, 0.18, 0.05)),
    "fig":      ((0.37, 0.55, 0.11), (0.14, 0.31, 0.06)),
    "palm":     ((0.50, 0.64, 0.19), (0.22, 0.36, 0.09)),
    "lily":     ((0.36, 0.56, 0.20), (0.15, 0.31, 0.10)),
    "shrub":    ((0.24, 0.46, 0.14), (0.08, 0.22, 0.06)),
    "hedge":    ((0.30, 0.52, 0.14), (0.10, 0.26, 0.06)),
}
LEAF_TEX = {"raintree": "tree_leaf_raintree", "narra": "tree_leaf_narra", "mango": "tree_leaf_mango",
            "fig": "tree_leaf_fig", "palm": "tree_frond", "lily": "tree_lily_leaf", "shrub": "tree_shrub_leaf",
            "hedge": "tree_hedge_leaf"}
# Card width over length, from each drawing's own aspect, so no drawing is stretched.
CARD_ASPECT = {"tree_leaf_raintree": 1.0, "tree_leaf_narra": 0.6, "tree_leaf_mango": 1.0, "tree_leaf_fig": 0.75,
               "tree_frond": 0.5, "tree_lily_leaf": 0.125, "tree_shrub_leaf": 0.75, "tree_hedge_leaf": 1.0,
               "tree_ixora": 1.0, "tree_lily_flower": 1.0}
# Wood: (texture, desaturation 0..1, multiply tint, limewashed). Bark is J for every species; the
# species differ only by a tint on it (the owner settled the bark drawing).
WOOD = {
    "tree_wood_raintree":      ("tree_bark", 0.45, (0.80, 0.76, 0.74), False),
    "tree_wood_raintree_lime": ("tree_bark", 0.45, (0.80, 0.76, 0.74), True),
    "tree_wood_narra":         ("tree_bark", 0.2, (1.00, 0.92, 0.84), False),
    "tree_wood_mango":         ("tree_bark", 0.3, (0.66, 0.60, 0.58), False),
    "tree_wood_fig":           ("tree_bark", 0.85, (1.25, 1.24, 1.22), True),
    "tree_palm_trunk":         ("tree_palm_trunk", 0.0, (1.0, 1.0, 1.0), False),
    "tree_crownshaft":         ("tree_crownshaft", 0.0, (1.0, 1.0, 1.0), False),
    # The lily's flower scape: a thin green stem, the crownshaft's smooth green lightened.
    "tree_lily_scape":         ("tree_crownshaft", 0.0, (1.15, 1.2, 1.05), False),
    "tree_pit_kerb":           ("tree_pit_kerb", 0.0, (1.0, 1.0, 1.0), False),
    "tree_pit_soil":           ("tree_pit_soil", 0.0, (1.0, 1.0, 1.0), False),
}


def _img(nodes, name, colour=True):
    n = nodes.new("ShaderNodeTexImage")
    n.image = bpy.data.images.load(str(TEXTURES / name), check_existing=True)
    if not colour:
        n.image.colorspace_settings.name = "Non-Color"
    return n


def card_material(name, tex, rgb, gain=1.25):
    """Kanto's leaf_material, as the Lagoon ported it: the drawing multiplied by the tint, cut out by
    its alpha, DITHERED, lit from both sides, roughness 0.7, and the object's random number nudging
    value and hue a few per cent so two trees sharing a mesh are not twins (per plant, never per leaf)."""
    m = bpy.data.materials.get(name)
    if m:
        return m
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*rgb, 1)
    if m.node_tree is None:
        m.use_nodes = True
    nodes, links = m.node_tree.nodes, m.node_tree.links
    bsdf = nodes["Principled BSDF"]
    img = _img(nodes, f"{tex}_albedo.png")
    mix = nodes.new("ShaderNodeMix")
    mix.data_type, mix.blend_type = "RGBA", "MULTIPLY"
    mix.inputs["Factor"].default_value = 1.0
    links.new(img.outputs["Color"], mix.inputs[6])
    mix.inputs[7].default_value = (*(min(1.0, c * gain) for c in rgb), 1)
    info = nodes.new("ShaderNodeObjectInfo")
    val = nodes.new("ShaderNodeMapRange")
    val.inputs["To Min"].default_value, val.inputs["To Max"].default_value = 0.9, 1.08
    hue = nodes.new("ShaderNodeMapRange")
    hue.inputs["To Min"].default_value, hue.inputs["To Max"].default_value = 0.488, 0.512
    hsv = nodes.new("ShaderNodeHueSaturation")
    links.new(info.outputs["Random"], val.inputs["Value"])
    links.new(info.outputs["Random"], hue.inputs["Value"])
    links.new(val.outputs["Result"], hsv.inputs["Value"])
    links.new(hue.outputs["Result"], hsv.inputs["Hue"])
    links.new(mix.outputs[2], hsv.inputs["Color"])
    links.new(hsv.outputs["Color"], bsdf.inputs["Base Color"])
    links.new(img.outputs["Alpha"], bsdf.inputs["Alpha"])
    bsdf.inputs["Roughness"].default_value = 0.7
    m.use_backface_culling = False
    if hasattr(m, "surface_render_method"):
        m.surface_render_method = "DITHERED"
    else:
        m.blend_method = "CLIP"
    return m


def leaf_material(kind, which):
    k = 0 if which == "light" else 1
    return card_material(f"tree_leaf_{kind}_{which}", LEAF_TEX[kind], LEAF_TINTS[kind][k])


def wood_material(name):
    """A painted surface on its UV map, desaturated and tinted per species; trunks multiply
    tree_splash through UVSplash, and the limewashed ones lay tree_limewash over it first."""
    m = bpy.data.materials.get(name)
    if m:
        return m
    tex, desat, tint, lime = WOOD[name]
    m = bpy.data.materials.new(name)
    if m.node_tree is None:
        m.use_nodes = True
    nodes, links = m.node_tree.nodes, m.node_tree.links
    bsdf = nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = 0.85
    uv = nodes.new("ShaderNodeUVMap")
    uv.uv_map = "UVMap"
    alb = _img(nodes, f"{tex}_albedo.png")
    links.new(uv.outputs["UV"], alb.inputs["Vector"])
    colour = alb.outputs["Color"]
    if desat:
        hs = nodes.new("ShaderNodeHueSaturation")
        hs.inputs["Saturation"].default_value = 1 - desat
        links.new(colour, hs.inputs["Color"])
        colour = hs.outputs["Color"]
    mul = nodes.new("ShaderNodeMix")
    mul.data_type, mul.blend_type = "RGBA", "MULTIPLY"
    mul.inputs["Factor"].default_value = 1.0
    links.new(colour, mul.inputs[6])
    mul.inputs[7].default_value = (*tint, 1)
    colour = mul.outputs[2]
    trunk = tex in ("tree_bark", "tree_palm_trunk")
    if trunk:
        suv = nodes.new("ShaderNodeUVMap")
        suv.uv_map = "UVSplash"
        if lime:
            lw = _img(nodes, "tree_limewash.png")
            links.new(suv.outputs["UV"], lw.inputs["Vector"])
            mx = nodes.new("ShaderNodeMix")
            mx.data_type = "RGBA"
            links.new(lw.outputs["Alpha"], mx.inputs["Factor"])
            links.new(colour, mx.inputs[6])
            links.new(lw.outputs["Color"], mx.inputs[7])
            colour = mx.outputs[2]
        sp = _img(nodes, "tree_splash.png", colour=False)
        links.new(suv.outputs["UV"], sp.inputs["Vector"])
        m2 = nodes.new("ShaderNodeMix")
        m2.data_type, m2.blend_type = "RGBA", "MULTIPLY"
        m2.inputs["Factor"].default_value = 1.0
        links.new(colour, m2.inputs[6])
        links.new(sp.outputs["Color"], m2.inputs[7])
        colour = m2.outputs[2]
    links.new(colour, bsdf.inputs["Base Color"])
    nrm = TEXTURES / f"{tex}_normal.png"
    if nrm.exists():
        n = _img(nodes, f"{tex}_normal.png", colour=False)
        links.new(uv.outputs["UV"], n.inputs["Vector"])
        nm = nodes.new("ShaderNodeNormalMap")
        nm.inputs["Strength"].default_value = 0.6
        links.new(n.outputs["Color"], nm.inputs["Color"])
        links.new(nm.outputs["Normal"], bsdf.inputs["Normal"])
    m.diffuse_color = (*(0.4 * t for t in tint), 1)
    return m


# ------------------------------------------------------------------ mesh building

class Mesh:
    """Verts and faces with a material slot per face, per-vertex UVs on two maps (UVMap, and
    UVSplash for the trunk overlays) and optional per-vertex custom normals (the leaf cards'
    clump normals). A zero normal keeps Blender's own."""

    def __init__(self):
        self.verts, self.faces, self.slots, self.smooth = [], [], [], []
        self.uvs, self.uv2, self.normals = [], [], []
        self.mats = []

    def slot(self, mat):
        if mat not in self.mats:
            self.mats.append(mat)
        return self.mats.index(mat)

    def add(self, verts, faces, mat, uvs=None, normals=None, smooth=False, uv2=None):
        base = len(self.verts)
        s = self.slot(mat)
        self.verts += [tuple(v) for v in verts]
        self.uvs += list(uvs) if uvs else [(0.0, 0.0)] * len(verts)
        self.uv2 += list(uv2) if uv2 else [(0.5, 0.995)] * len(verts)
        self.normals += list(normals) if normals else [None] * len(verts)
        for f in faces:
            self.faces.append(tuple(base + i for i in f))
            self.slots.append(s)
            self.smooth.append(smooth)

    def build(self, name):
        me = bpy.data.meshes.new(name)
        me.from_pydata(self.verts, [], self.faces)
        for m in self.mats:
            me.materials.append(m)
        for p, s, sm in zip(me.polygons, self.slots, self.smooth):
            p.material_index = s
            p.use_smooth = sm
        me.validate()
        for layer, data in (("UVMap", self.uvs), ("UVSplash", self.uv2)):
            uv = me.uv_layers.new(name=layer)
            for loop in me.loops:
                uv.data[loop.index].uv = data[loop.vertex_index]
        if any(n is not None for n in self.normals):
            me.normals_split_custom_set([self.normals[lp.vertex_index] or (0.0, 0.0, 0.0) for lp in me.loops])
        me.update()
        return me


def obj(col, name, mesh, bevel=0.0):
    o = bpy.data.objects.new(name, mesh)
    col.objects.link(o)
    if bevel:
        mod = o.modifiers.new("Bevel", "BEVEL")
        mod.width, mod.segments = bevel, 1
        mod.limit_method, mod.angle_limit = "ANGLE", math.radians(35)
        mod.harden_normals, mod.use_clamp_overlap = True, True
    return o


def tube(m, mat, pts, radii, sides=7, phase=0.0, wobble=0.0, rng=None, v_tile=BARK_TILE, smooth=False):
    """ONE limb as its own polygonal tube (Kanto's limb_tube): parallel-transported rings, a hand-cut
    wobble per vertex (never on the end rings), SQUARE-TEXEL cylindrical UVs (u wraps a whole number
    of tiles round the limb, v runs along it at the same tiles per metre), and UVSplash v = z / 2."""
    pts = [Vector(p) for p in pts]
    circ = math.tau * sum(radii) / len(radii)
    turns = max(1, round(circ / v_tile))
    per_m = turns / circ
    vs = [0.0]
    for i in range(1, len(pts)):
        vs.append(vs[-1] + (pts[i] - pts[i - 1]).length * per_m)
    verts, uvs, uv2, faces = [], [], [], []
    q, prev = None, None
    for i, p in enumerate(pts):
        d = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]).normalized()
        q = d.to_track_quat("Z", "Y").to_matrix() if q is None else prev.rotation_difference(d).to_matrix() @ q
        prev = d
        for k in range(sides + 1):
            kk = k % sides
            a = phase + kk * math.tau / sides
            j = 1.0 + (rng.uniform(-wobble, wobble) if rng and 0 < i < len(pts) - 1 else 0.0)
            co = p + q @ Vector((math.cos(a) * radii[i] * j, math.sin(a) * radii[i] * j, 0))
            verts.append(co)
            uvs.append((k / sides * turns, vs[i]))
            uv2.append((k / sides, min(0.99, max(0.005, co.z / 2.0))))
    row = sides + 1
    for i in range(len(pts) - 1):
        for k in range(sides):
            a, b = i * row + k, i * row + k + 1
            faces.append((a, b, b + row, a + row))
    for ring, flip in ((0, True), (len(pts) - 1, False)):
        c = len(verts)
        verts.append(pts[ring])
        uvs.append((0.5, vs[ring]))
        uv2.append((0.5, min(0.99, max(0.005, pts[ring].z / 2.0))))
        for k in range(sides):
            a, b = ring * row + k, ring * row + k + 1
            faces.append((b, a, c) if flip else (a, b, c))
    m.add(verts, faces, mat, uvs, None, smooth, uv2)


def curve(p0, p1, p2, n):
    p0, p1, p2 = Vector(p0), Vector(p1), Vector(p2)
    return [p0 * (1 - t) ** 2 + p1 * 2 * t * (1 - t) + p2 * t * t for t in (k / n for k in range(n + 1))]


def flat_card(m, mat, at, tip, normal, length, width, centre):
    """Kanto's leaf card: one quad from `at` along `tip`, facing `normal`, UV 0..1, normals out of
    the clump centre."""
    side = normal.cross(tip).normalized() * (width / 2)
    base = at - tip * (length * 0.08)
    end = at + tip * (length * 0.92)
    cos = [base - side, base + side, end + side, end - side]
    face = (0, 1, 2, 3)
    if poly_normal(cos).dot(normal) < 0:
        face = (3, 2, 1, 0)
    nrm = [(c - centre).normalized() for c in cos]
    m.add(cos, [face], mat, ((0, 0), (1, 0), (1, 1), (0, 1)), nrm, smooth=True)


def ellipsoid_area(a, b, c):
    p = 1.6075
    return 4 * math.pi * (((a * b) ** p + (a * c) ** p + (b * c) ** p) / 3) ** (1 / p)


def foliage(m, kind, centre, radii, leaf_len, rng, lobes=(), floor_z=None, lift=0.3, spread=0.45, cover=2.4,
            aspect=None):
    """THE HOUSE CLUMP (Kanto's foliage(), the Lagoon's _foliage()): cards ON the ellipsoid, facing out,
    tips running DOWN the surface within `spread`, lifted by `lift`, spread evenly on a Fibonacci
    sphere, shingled like roof tiles; NO core; cards inside a neighbouring lobe or under `floor_z`
    are skipped. Light tint above the clump's lower fifth, dark below."""
    centre = Vector(centre)
    tex = LEAF_TEX[kind]
    width = leaf_len * (aspect or CARD_ASPECT[tex])
    count = int(cover * ellipsoid_area(*radii) / (leaf_len * width * 0.8))
    light, dark = leaf_material(kind, "light"), leaf_material(kind, "dark")
    golden = math.pi * (3 - math.sqrt(5))
    for i in range(count):
        zf = 1 - 2 * (i + 0.5) / count
        r = math.sqrt(max(0.0, 1 - zf * zf))
        a = golden * i + rng.uniform(-0.3, 0.3)
        d = Vector((math.cos(a) * r, math.sin(a) * r, zf))
        p = centre + Vector((d.x * radii[0], d.y * radii[1], d.z * radii[2])) * rng.uniform(0.9, 1.02)
        if floor_z is not None and p.z < floor_z:
            continue
        if any(((p - c2).x / r2[0]) ** 2 + ((p - c2).y / r2[1]) ** 2 + ((p - c2).z / r2[2]) ** 2 < 0.72
               for c2, r2 in lobes if (Vector(c2) - centre).length > 1e-6):
            continue
        down = -UP - d * (-UP).dot(d)
        if down.length < 0.2:
            t = Vector((rng.gauss(0, 1), rng.gauss(0, 1), 0))
            down = t - d * t.dot(d)
        down.normalize()
        side = d.cross(down)
        ang = rng.uniform(-spread, spread)
        tip = (down * math.cos(ang) + side * math.sin(ang) + d * lift).normalized()
        nrm = (d - tip * d.dot(tip)).normalized()
        ln = leaf_len * rng.uniform(0.85, 1.15)
        flat_card(m, light if d.z > -0.2 else dark, p - tip * ln * 0.35, tip, nrm, ln, ln * width / leaf_len, centre)


def _frame(spine, i):
    n = len(spine)
    t = (spine[min(i + 1, n - 1)] - spine[max(i - 1, 0)]).normalized()
    side = t.cross(UP)
    if side.length < 1e-4:
        side = Vector((1, 0, 0))
    side.normalize()
    up = side.cross(t).normalized()
    if up.z < 0:
        up, side = -up, -side
    return t, side, up


def bent_card(m, mat, spine, width, fold, centre, up_bias=0.8):
    """The Lagoon's bent, V-folded card along a spine: three verts a section, UV U across and V by
    arc length, normals blended from the plant centre and the card's upper face."""
    centre = Vector(centre)
    lengths = [0.0]
    for a, b in zip(spine, spine[1:]):
        lengths.append(lengths[-1] + (b - a).length)
    total = lengths[-1] or 1.0
    verts, uvs, normals, ups = [], [], [], []
    for i, p in enumerate(spine):
        _t, side, up = _frame(spine, i)
        w = width / 2
        row = (p + side * w - up * fold * w, p.copy(), p - side * w - up * fold * w)
        for co, u in zip(row, (0.0, 0.5, 1.0)):
            verts.append(co)
            uvs.append((u, lengths[i] / total))
            r = co - centre
            r = r.normalized() if r.length > 1e-5 else up
            normals.append((r + up * up_bias).normalized())
        ups.append(up)
    faces = []
    for i in range(len(spine) - 1):
        a, b = i * 3, (i + 1) * 3
        faces += [(a, b, b + 1, a + 1), (a + 1, b + 1, b + 2, a + 2)]
    if poly_normal([verts[k] for k in faces[0]]).dot(ups[0]) < 0:
        faces = [tuple(reversed(f)) for f in faces]
    m.add(verts, faces, mat, uvs, normals, smooth=True)


def arc(start, azimuth, pitch0, pitch1, length, steps, bend=1.0):
    pts = [Vector(start)]
    ds = length / steps
    for k in range(steps):
        t = (k + 0.5) / steps
        pitch = pitch0 + (pitch1 - pitch0) * (t ** bend)
        pts.append(pts[-1] + Vector((math.cos(azimuth) * math.cos(pitch), math.sin(azimuth) * math.cos(pitch),
                                     math.sin(pitch))) * ds)
    return pts


# ------------------------------------------------------------------ broadleaf trees

class TreeSpec:
    def __init__(self, **kw):
        self.__dict__.update(kw)


def broadleaf(col, name, spec, seed):
    """A broadleaf tree from a spec: a trunk (flared, leaning a little), optional surface roots,
    primary limbs from the fork, one secondary fork on each, and a flattened clump at every limb
    end plus a crown clump or two over the fork. Returns the canopy's plan radius and height."""
    rng = random.Random(seed)
    s = spec
    wood = Mesh()
    leaves = Mesh()
    mat = wood_material(s.wood)
    lean = Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), 0)).normalized() * s.lean
    fork = Vector((0, 0, s.fork)) + lean
    R = s.radius
    # Trunk: sunk 0.35 m, a soft root flare in its own radius, one gentle bow to the fork.
    pts = curve((0, 0, -0.35), lean * 0.3 + Vector((rng.uniform(-0.15, 0.15), rng.uniform(-0.15, 0.15), s.fork * 0.5)),
                fork, 7)
    radii = []
    for p in pts:
        t = max(0.0, p.z) / s.fork
        radii.append(R * (1 - 0.28 * t) + R * s.flare * max(0.0, 1 - p.z / 0.9) ** 2)
    tube(wood, mat, pts, radii, 7, 0.0, 0.06, rng)
    # Surface roots: a few chunky spurs dipping under the ground.
    for k in range(s.roots):
        a = k * math.tau / s.roots + rng.uniform(-0.3, 0.3)
        d = Vector((math.cos(a), math.sin(a), 0))
        L = rng.uniform(1.0, 1.7) * R / 0.6
        rp = curve(Vector((0, 0, 0.45)) + d * R * 0.3, Vector((0, 0, 0.12)) + d * L * 0.5, Vector((0, 0, -0.18)) + d * L, 4)
        tube(wood, mat, rp, [R * 0.5, R * 0.42, R * 0.3, R * 0.2, R * 0.1], 5, math.pi / 5, 0.06, rng)
    clumps = []
    a0 = rng.uniform(0, math.tau)
    rt = R * 0.72
    reach_max = 0.0
    for i in range(s.limbs):
        a = a0 + i * math.tau / s.limbs + rng.uniform(-0.3, 0.3)
        d = Vector((math.cos(a), math.sin(a), 0))
        reach = s.reach * rng.uniform(0.85, 1.1)
        elev = math.radians(s.elev + rng.uniform(-6, 6))
        # A limb runs out at `elev` and turns UP at its end (the rain tree's great arms).
        tip = fork + d * reach + Vector((0, 0, reach * math.tan(elev) + s.upturn))
        mid = fork + d * reach * 0.55 + Vector((0, 0, reach * 0.55 * math.tan(elev) * 0.8))
        start = fork - Vector((0, 0, rt * 1.2))
        lp = curve(start, mid, tip, 6)
        r0 = rt * rng.uniform(0.7, 0.85) * getattr(s, "limb_r", 1.0)
        tube(wood, mat, lp, [r0 * (1 - 0.75 * (k / 6) ** 0.9) for k in range(7)], 5, math.pi / 5, 0.06, rng)
        k_ = rng.uniform(0.85, 1.12)
        cs = tuple(c * k_ for c in s.clump)
        clumps.append((tip + Vector((0, 0, cs[2] * s.lift)), cs))
        reach_max = max(reach_max, (tip - fork).xy.length + cs[0])
        # Secondary forks from the limb, each rising to its own clump beside it (the rain tree has
        # two, so its umbrella is a deep, continuous mound rather than a ring of separate puffs).
        for j in range(getattr(s, "seconds", 1)):
            sgn = (1 if rng.random() < 0.5 else -1) if j == 0 else -sgn
            side = Vector((-d.y, d.x, 0)) * sgn
            bi = 3 if j == 0 else 4
            bp = lp[bi]
            frac = 0.45 if j == 0 else 0.35
            stip = bp + (d * 0.4 + side * 0.9).normalized() * reach * frac + Vector((0, 0, reach * 0.3 + s.upturn * 0.5))
            sp = curve(bp - (lp[bi + 1] - lp[bi - 1]).normalized() * r0 * 0.5, bp.lerp(stip, 0.5) + Vector((0, 0, 0.3)), stip, 4)
            tube(wood, mat, sp, [r0 * 0.5 * (1 - 0.7 * k / 4) for k in range(5)], 5, 0.0, 0.05, rng)
            cs2 = tuple(c * 0.85 for c in cs)
            clumps.append((stip + Vector((0, 0, cs2[2] * s.lift)), cs2))
            reach_max = max(reach_max, (stip - fork).xy.length + cs2[0])
    # The crown over the fork, carried by a short leader so nothing floats.
    top = fork + Vector((0, 0, s.crown_h))
    tube(wood, mat, curve(fork - Vector((0, 0, rt)), fork + Vector((0, 0, s.crown_h * 0.5)), top - Vector((0, 0, 0.6)), 4),
         [rt * 0.6, rt * 0.5, rt * 0.36, rt * 0.22, rt * 0.1], 5, 0.0, 0.05, rng)
    for k in range(s.crowns):
        off = Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), 0)) * s.clump[0] * 0.5 * (k > 0)
        cc = tuple(c * s.crown_scale for c in s.clump)
        clumps.append((top + off + Vector((0, 0, cc[2] * 0.3)), cc))
    for c, r in clumps:
        foliage(leaves, s.leaf, c, r, s.leaf_len, rng, lobes=clumps, cover=s.cover)
    kit = obj(col, name + "_wood", wood.build(name + "_wood"), bevel=0.02)
    for p in kit.data.polygons:
        p.use_smooth = False
    obj(col, name + "_leaves", leaves.build(name + "_leaves"))
    height = max(c.z + r[2] for c, r in clumps)
    low = min(c.z - r[2] for c, r in clumps)
    return {"r": reach_max, "h": height, "low": low}


SPECIES = {
    # Rain tree: short massive trunk, low fork, near-level limbs turning up, a wide layered umbrella.
    "raintree": [TreeSpec(wood="tree_wood_raintree", leaf="raintree", radius=0.82, flare=0.5, lean=0.5, fork=2.8, roots=5,
                          limbs=5, reach=6.4, elev=26, upturn=1.6, clump=(3.3, 3.3, 2.0), lift=0.1, crown_h=4.2,
                          crowns=3, crown_scale=1.1, leaf_len=0.9, cover=2.3, limb_r=1.25, seconds=2),
                 TreeSpec(wood="tree_wood_raintree_lime", leaf="raintree", radius=0.74, flare=0.45, lean=0.8, fork=3.0,
                          roots=4, limbs=4, reach=6.0, elev=30, upturn=1.5, clump=(3.2, 3.2, 1.9), lift=0.1,
                          crown_h=4.0, crowns=3, crown_scale=1.1, leaf_len=0.9, cover=2.3, limb_r=1.25, seconds=2),
                 TreeSpec(wood="tree_wood_raintree", leaf="raintree", radius=0.9, flare=0.55, lean=0.4, fork=2.6, roots=6,
                          limbs=5, reach=7.2, elev=22, upturn=1.8, clump=(3.5, 3.5, 2.1), lift=0.1, crown_h=4.4,
                          crowns=3, crown_scale=1.1, leaf_len=0.95, cover=2.3, limb_r=1.25, seconds=2)],
    # Narra: taller straighter trunk, buttressed foot, steep limbs, a rounded dome drooping at its rim.
    "narra": [TreeSpec(wood="tree_wood_narra", leaf="narra", radius=0.42, flare=0.8, lean=0.25, fork=4.4, roots=3,
                       limbs=3, reach=3.4, elev=52, upturn=0.8, clump=(2.7, 2.7, 2.1), lift=0.1, crown_h=5.2, crowns=2,
                       crown_scale=1.05, leaf_len=0.7, cover=2.3),
              TreeSpec(wood="tree_wood_narra", leaf="narra", radius=0.46, flare=0.9, lean=0.4, fork=4.0, roots=4,
                       limbs=4, reach=3.8, elev=48, upturn=0.6, clump=(2.6, 2.6, 2.0), lift=0.1, crown_h=5.0, crowns=2,
                       crown_scale=1.1, leaf_len=0.7, cover=2.3)],
    # Mango: short trunk, low fork, a dense low dark dome.
    "mango": [TreeSpec(wood="tree_wood_mango", leaf="mango", radius=0.34, flare=0.4, lean=0.3, fork=1.7, roots=0,
                       limbs=4, reach=2.6, elev=40, upturn=0.8, clump=(2.3, 2.3, 1.9), lift=0.0, crown_h=3.6, crowns=2,
                       crown_scale=1.15, leaf_len=0.62, cover=2.6),
              TreeSpec(wood="tree_wood_mango", leaf="mango", radius=0.3, flare=0.4, lean=0.5, fork=1.9, roots=0,
                       limbs=3, reach=2.3, elev=45, upturn=0.6, clump=(2.2, 2.2, 1.8), lift=0.0, crown_h=3.3, crowns=2,
                       crown_scale=1.1, leaf_len=0.6, cover=2.6),
              TreeSpec(wood="tree_wood_mango", leaf="mango", radius=0.38, flare=0.45, lean=0.2, fork=1.6, roots=2,
                       limbs=5, reach=2.9, elev=36, upturn=0.9, clump=(2.4, 2.4, 1.9), lift=0.0, crown_h=3.8, crowns=2,
                       crown_scale=1.2, leaf_len=0.64, cover=2.6)],
    # The Padre Faura kerb fig: slim pale trunk, a dense round crown.
    "fig": [TreeSpec(wood="tree_wood_fig", leaf="fig", radius=0.15, flare=0.3, lean=0.15, fork=2.6, roots=0, limbs=3,
                     reach=0.9, elev=55, upturn=0.3, clump=(1.25, 1.25, 1.15), lift=0.1, crown_h=2.2, crowns=2,
                     crown_scale=1.3, leaf_len=0.42, cover=2.8),
            TreeSpec(wood="tree_wood_fig", leaf="fig", radius=0.17, flare=0.3, lean=0.3, fork=2.4, roots=0, limbs=4,
                     reach=1.1, elev=50, upturn=0.3, clump=(1.3, 1.3, 1.2), lift=0.1, crown_h=2.4, crowns=2,
                     crown_scale=1.35, leaf_len=0.42, cover=2.8)],
}


# ------------------------------------------------------------------ palms

def palm(col, name, seed, height, lean_deg, trunk_r, shaft_len, fronds, frond_len, royal=False):
    """A Manila palm (or, with royal=True, a royal palm): a smooth ringed trunk (12 sides) swept
    along a gentle curve with a slight foot swell (and the royal's mid-height swell), a green
    crownshaft penetrating it, and bent, V-folded frond cards arching out and down."""
    rng = random.Random(seed)
    wood, leaves = Mesh(), Mesh()
    az = rng.uniform(0, math.tau)
    off = height * math.tan(math.radians(lean_deg))
    path, radii = [], []
    steps = 16
    for k in range(steps + 1):
        t = k / steps
        f = 0.6 * (2 * t - t * t) + 0.4 * t
        path.append(Vector((math.cos(az) * off * f, math.sin(az) * off * f, -0.35 + (height + 0.35) * t)))
        swell = 0.18 * math.exp(-((t - 0.45) / 0.18) ** 2) if royal else 0.0
        foot = 0.35 * max(0.0, 1 - (path[-1].z + 0.35) / 0.8) ** 2
        radii.append(trunk_r * (1 - 0.18 * t + swell + foot))
    tube(wood, wood_material("tree_palm_trunk"), path, radii, 12, 0.0, 0.0, None, 2.0, smooth=True)
    top = path[-1]
    tan = (path[-1] - path[-2]).normalized()
    shaft = [top - tan * 0.25, top + tan * shaft_len * 0.5, top + tan * shaft_len]
    tube(wood, wood_material("tree_crownshaft"), shaft, [radii[-1] * 1.12, radii[-1] * 1.18, radii[-1] * 0.9], 12, 0.0,
         0.0, None, 1.0, smooth=True)
    crown = top + tan * (shaft_len + 0.05)
    centre = crown - Vector((0, 0, 0.6))
    light, dark = leaf_material("palm", "light"), leaf_material("palm", "dark")
    phase = rng.uniform(0, math.tau)
    for k in range(fronds):
        a = phase + math.tau * k / fronds + rng.uniform(-0.2, 0.2)
        lower = k % 2 == 1
        p0 = math.radians(rng.uniform(20, 34) if lower else rng.uniform(46, 62))
        p1 = math.radians(rng.uniform(-70, -50) if lower else rng.uniform(-40, -20))
        ln = frond_len * rng.uniform(0.9, 1.05)
        spine = arc(crown - tan * 0.1, a, p0, p1, ln, 9, bend=1.2)
        bent_card(leaves, dark if lower else light, spine, ln * 0.5 * 1.05, 0.45, centre)
    # Two young spears standing up out of the crown.
    for k in range(2):
        a = phase + 0.5 + math.pi * k
        spine = arc(crown - tan * 0.05, a, math.radians(78), math.radians(60), frond_len * 0.55, 5)
        bent_card(leaves, light, spine, frond_len * 0.55 * 0.28, 0.3, centre)
    obj(col, name + "_trunk", wood.build(name + "_trunk"), bevel=0.01)
    obj(col, name + "_fronds", leaves.build(name + "_fronds"))
    return {"r": frond_len * 0.85, "h": crown.z + frond_len * 0.5, "low": crown.z - frond_len * 0.6}


# ------------------------------------------------------------------ shrubs, hedge, lilies, pit

def ixora_shrub(col, name, seed, scale=1.0):
    """A santan shrub: a main dome and two or three side lobes of glossy leaf cards, with crimson
    flower-ball cards shingled on its upper surface facing out and up, a third tucked in."""
    rng = random.Random(seed)
    m = Mesh()
    lobes = [(Vector((0, 0, 0.5 * scale)), (0.62 * scale, 0.62 * scale, 0.55 * scale))]
    for k in range(rng.randint(2, 3)):
        a = rng.uniform(0, math.tau)
        d = rng.uniform(0.4, 0.55) * scale
        r = rng.uniform(0.38, 0.46) * scale
        lobes.append((Vector((math.cos(a) * d, math.sin(a) * d, r * 0.75)), (r, r, r * 0.85)))
    for c, r in lobes:
        foliage(m, "shrub", c, r, 0.22, rng, lobes=lobes, floor_z=0.02, cover=2.6)
    fm = card_material("tree_flower_ixora", "tree_ixora", (0.62, 0.05, 0.12), gain=1.25)
    golden = math.pi * (3 - math.sqrt(5))
    n = rng.randint(18, 24)
    for i in range(n):
        c, r = lobes[i % len(lobes)]
        zf = 1 - 1.3 * (i + 0.5) / n
        rr = math.sqrt(max(0.0, 1 - zf * zf))
        a = golden * i * 3.1 + rng.uniform(-0.4, 0.4)
        d = Vector((math.cos(a) * rr, math.sin(a) * rr, max(zf, 0.05))).normalized()
        tuck = 0.8 if i % 3 == 0 else 1.0
        p = c + Vector((d.x * r[0], d.y * r[1], d.z * r[2])) * tuck
        face = (d + Vector((0, 0, 0.5))).normalized()
        ref = Vector((0, 0, 1)) if abs(face.z) < 0.9 else Vector((1, 0, 0))
        tip = face.cross(ref).cross(face).normalized()
        sz = 0.3 * scale * rng.uniform(0.9, 1.1)
        flat_card(m, fm, p - tip * sz * 0.5, tip, face, sz, sz, c - Vector((0, 0, 0.3)))
    obj(col, name, m.build(name))
    return {"r": 1.0 * scale, "h": 1.1 * scale, "low": 0}


def hedge(col, name, length=4.0, seed=71):
    """A clipped kamuning hedge run: overlapping lobes along its length, flattened on top and at
    the sides (clipped), 1.0 m tall, 0.8 m deep, growing out of the ground."""
    rng = random.Random(seed)
    m = Mesh()
    lobes = []
    n = int(length / 0.5) + 1
    for k in range(n):
        x = -length / 2 + 0.45 + k * (length - 0.9) / (n - 1)
        lobes.append((Vector((x + rng.uniform(-0.04, 0.04), 0, 0.55)), (0.55, 0.44, 0.5)))
    for c, r in lobes:
        foliage(m, "hedge", c, r, 0.22, rng, lobes=lobes, floor_z=0.03, lift=0.2, spread=0.35, cover=2.8)
    obj(col, name, m.build(name))
    return {"r": length / 2, "h": 1.05, "low": 0}


def lily_clump(m, rng, at=(0, 0, 0), scale=1.0, flowers=None):
    """One spider lily clump into mesh `m`: 14 to 20 strap leaves arching out of the soil from a
    small ring (the outer ones flopping lower and wearing the dark tint), and one or two scapes
    carrying a white flower card. `at` is the soil top."""
    at = Vector(at)
    light, dark = leaf_material("lily", "light"), leaf_material("lily", "dark")
    centre = at + Vector((0, 0, -0.25))
    n = rng.randint(14, 20)
    phase = rng.uniform(0, math.tau)
    for k in range(n):
        a = phase + math.tau * k / n + rng.uniform(-0.25, 0.25)
        ln = rng.uniform(0.6, 0.95) * scale
        start = at + Vector((math.cos(a) * 0.05, math.sin(a) * 0.05, -0.05))
        p0 = rng.uniform(58, 80)
        p1 = rng.uniform(-35, 20)
        spine = arc(start, a, math.radians(p0), math.radians(p1), ln, 6, bend=1.3)
        bent_card(m, light if p1 > -5 else dark, spine, 0.085 * scale, -0.35, centre, up_bias=0.6)
    fm = card_material("tree_flower_lily", "tree_lily_flower", (1.0, 1.0, 1.0), gain=1.0)
    scape = wood_material("tree_lily_scape")
    for k in range(flowers if flowers is not None else rng.randint(1, 2)):
        a = rng.uniform(0, math.tau)
        top = at + Vector((math.cos(a) * 0.18, math.sin(a) * 0.18, rng.uniform(0.62, 0.8) * scale))
        tube(m, scape, [at + Vector((0, 0, -0.05)), at.lerp(top, 0.5) + Vector((0.02, 0, 0)), top],
             [0.018, 0.015, 0.012], 6, 0.0, 0.0, None, 0.5, smooth=True)
        face = (Vector((math.cos(a), math.sin(a), 0)) * 0.6 + UP).normalized()
        tip = face.cross(Vector((1, 0, 0)) if abs(face.x) < 0.9 else Vector((0, 1, 0))).cross(face).normalized()
        sz = 0.5 * scale
        flat_card(m, fm, top - tip * sz * 0.5 + face * 0.02, tip, face, sz, sz, top - Vector((0, 0, 0.4)))


def lily_proto(col, name, seed):
    rng = random.Random(seed)
    m = Mesh()
    lily_clump(m, rng)
    obj(col, name, m.build(name))
    return {"r": 0.6, "h": 0.9, "low": 0}


def lily_bed(col, name, length=3.0, width=0.8, seed=91):
    """Seven clumps in a 3 m x 0.8 m bed, staggered, one piece: the median planter's filling."""
    rng = random.Random(seed)
    m = Mesh()
    n = 7
    for k in range(n):
        x = -length / 2 + 0.3 + k * (length - 0.6) / (n - 1) + rng.uniform(-0.08, 0.08)
        y = (0.16 if k % 2 else -0.16) * width / 0.8 + rng.uniform(-0.05, 0.05)
        lily_clump(m, rng, (x, y, 0), rng.uniform(0.85, 1.05), flowers=rng.choice((0, 1, 1, 2)))
    obj(col, name, m.build(name))
    return {"r": length / 2, "h": 0.9, "low": 0}


def ring_profile(outer, inner, z0, z1):
    """Verts and faces of a hollow rounded-square kerb: outer and inner walls, a top, a bottom."""
    o = L.rounded_rect(outer, outer, 0.1)
    i = L.rounded_rect(inner, inner, 0.06)
    n = len(o)
    verts = [Vector((x, y, z0)) for x, y in o] + [Vector((x, y, z1)) for x, y in o] + \
            [Vector((x, y, z1)) for x, y in i] + [Vector((x, y, z0)) for x, y in i]
    faces = []
    for k in range(n):
        a, b = k, (k + 1) % n
        faces.append((a, b, n + b, n + a))                     # outer wall
        faces.append((n + a, n + b, 2 * n + b, 2 * n + a))     # top
        faces.append((2 * n + a, 2 * n + b, 3 * n + b, 3 * n + a))   # inner wall
        faces.append((3 * n + a, 3 * n + b, b, a))             # bottom
    return verts, faces


def tree_pit(col, name):
    """The Padre Faura pavement tree pit: a hollow rounded concrete kerb 1.3 m square (0.12 thick),
    its top 9 cm above the pavement and its foot sunk 10 cm into it, and the soil 3 cm above the
    pavement, 6 cm under the kerb top. Origin at the pavement top."""
    m = Mesh()
    verts, faces = ring_profile(0.65, 0.53, -0.10, 0.09)
    faces = [f if poly_normal([verts[k] for k in f]).dot((sum((verts[k] for k in f), Vector()) / 4).normalized() + UP * 0.01) >= 0
             else tuple(reversed(f)) for f in faces]
    m.add(verts, faces, wood_material("tree_pit_kerb"),
          [(v.x / 2 + (v.z if abs(v.x) > 0.6 else 0) / 2, v.y / 2 + v.z / 2) for v in verts])
    soil = L.rounded_rect(0.545, 0.545, 0.06)
    sv = [Vector((x, y, 0.03)) for x, y in soil]
    m.add(sv, [tuple(range(len(sv)))], wood_material("tree_pit_soil"), [(v.x / 2, v.y / 2) for v in sv])
    o = obj(col, name, m.build(name), bevel=0.02)
    return {"r": 0.65, "h": 0.1, "low": 0}


# ------------------------------------------------------------------ the kit

def collection(name, parent=None):
    c = bpy.data.collections.new(name)
    (parent or bpy.context.scene.collection).children.link(c)
    return c


def build_kit():
    kit = collection("kit (prototypes)")
    kit.hide_render = True
    info = {}

    def proto(name, fn, *a, **kw):
        c = collection(name, kit)
        info[name] = dict(fn(c, name, *a, **kw), col=c)

    for sp, specs in SPECIES.items():
        for i, spec in enumerate(specs):
            proto(f"tree_{sp}_{i}", broadleaf, spec, 1000 + 17 * i + 100 * list(SPECIES).index(sp))
    proto("tree_manila_palm_0", palm, 301, 6.0, 4, 0.19, 1.1, 11, 2.8)
    proto("tree_manila_palm_1", palm, 302, 7.0, 8, 0.2, 1.2, 12, 2.9)
    proto("tree_royal_palm", palm, 303, 11.5, 2, 0.36, 2.1, 14, 4.0, royal=True)
    for i in range(3):
        proto(f"tree_ixora_{i}", ixora_shrub, 400 + i, (0.9, 1.05, 1.2)[i])
    proto("tree_hedge", hedge)
    for i in range(3):
        proto(f"tree_lily_{i}", lily_proto, 500 + i)
    proto("tree_lily_bed", lily_bed)
    proto("tree_pit", tree_pit)
    for k, v in info.items():
        print(f"[ilalim-trees] {k}: canopy r {v['r']:.1f} m, underside {v['low']:.1f} m, top {v['h']:.1f} m")
    return kit, info


def place(target, proto_col, name, loc, yaw=0.0, scale=1.0):
    """An empty per placement holding linked duplicates of the prototype's objects."""
    e = bpy.data.objects.new(name, None)
    e.empty_display_type, e.empty_display_size = "PLAIN_AXES", 0.5
    e.location = Vector(loc)
    e.rotation_euler = (0, 0, yaw)
    e.scale = (scale, scale, scale)
    target.objects.link(e)
    for o in proto_col.objects:
        c = o.copy()                    # linked duplicate: shares the prototype's mesh
        c.parent = e
        c.location = (0, 0, 0)
        target.objects.link(c)
    return e


# ------------------------------------------------------------------ placement

def seg_dist(px, py, ax, ay, bx, by):
    dx, dy = bx - ax, by - ay
    L2 = dx * dx + dy * dy or 1e-9
    t = max(0.0, min(1.0, ((px - ax) * dx + (py - ay) * dy) / L2))
    return math.hypot(px - (ax + t * dx), py - (ay + t * dy))


class Site:
    """Everything a tree must keep clear of, from the sightline-overridden layout."""

    def __init__(self, layout):
        self.layout = layout
        self.buildings = []
        for b in layout["buildings"]:
            xs, ys = [p[0] for p in b["poly"]], [p[1] for p in b["poly"]]
            self.buildings.append((b["poly"], (min(xs), max(xs), min(ys), max(ys))))
        self.roads = []           # (segment, clearance for a trunk)
        self.streets = []
        for r in layout["roads"]:
            if "Taft" in r["name"]:
                continue
            if r["kind"] in B.STREET_KINDS:
                half = max(3.0, r["lanes"] * 3.2) / 2
                clear = half + B.SIDEWALK + 0.6
                self.streets.append((r, half))
            elif r["kind"] == "service":
                clear = 2.3
            elif r["kind"] in ("footway", "pedestrian", "path"):
                clear = 1.0
            else:
                continue
            for (x0, y0), (x1, y1) in zip(r["line"], r["line"][1:]):
                self.roads.append((x0, y0, x1, y1, clear))
        hall = next(b for b in layout["buildings"] if "Rizal Hall" in b["name"])
        self.oblation = next(p["at"] for p in layout["points"] if "Oblation" in p["name"])
        self.front_y = min(p[1] for p in hall["poly"])
        self.portico = (self.oblation[0], self.front_y - 2.6)
        self.eyes = [(0.0, -B.SPAWN), (-8.0, 16.0), (8.0, 0.0), (-8.0, -10.0)]
        self.placed = []          # (x, y, r)

    def view_clear(self, x, y, r):
        """No canopy within 4 m, in plan, of the court-to-portico view lines."""
        return all(seg_dist(x, y, ax, ay, *self.portico) >= r + 4.0 for ax, ay in self.eyes)

    def in_building(self, x, y, margin=0.0):
        for poly, (x0, x1, y0, y1) in self.buildings:
            if x0 - margin <= x <= x1 + margin and y0 - margin <= y <= y1 + margin:
                if B.point_in_poly(x, y, poly):
                    return True
                if margin and min(seg_dist(x, y, *poly[k - 1], *poly[k]) for k in range(len(poly))) < margin:
                    return True
        return False

    def on_road(self, x, y, extra=0.0):
        return any(seg_dist(x, y, x0, y0, x1, y1) < c + extra for x0, y0, x1, y1, c in self.roads
                   if min(x0, x1) - c - 3 <= x <= max(x0, x1) + c + 3 and min(y0, y1) - c - 3 <= y <= max(y0, y1) + c + 3)

    def corridor_ok(self, x, r):
        return abs(x) >= CORRIDOR_X and abs(x) - r >= DECK_CLEAR_X

    def overhang_ok(self, x, r, low):
        """A canopy reaching over the Taft pavement (inside |x| 11.2) keeps its underside above
        4.2 m: the bridge hoop's board tops out at 3.7 m and the vendors' umbrellas at 2.8 m."""
        return abs(x) - r >= B.PAVE_OUT + 0.2 or low >= 4.2

    def spacing_ok(self, x, y, r, k=0.72):
        return all(math.hypot(x - px, y - py) >= k * (r + pr) for px, py, pr in self.placed)

    def ok(self, x, y, r, trunk_margin=1.0, canopy_k=0.65, street=False):
        if max(abs(x), abs(y)) > B.EXTENT - 5 or not self.corridor_ok(x, r):
            return False
        if not self.view_clear(x, y, r):
            return False
        if self.in_building(x, y, max(trunk_margin, canopy_k * r)):
            return False
        if not street and self.on_road(x, y):
            return False
        return self.spacing_ok(x, y, r)


def plant(site, info, target):
    rng = random.Random(29)
    counts = {}

    def put(proto, x, y, z=GROUND_Z, scale=None, yaw=None):
        scale = scale if scale is not None else rng.uniform(0.88, 1.08)
        r = info[proto]["r"] * scale
        site.placed.append((x, y, r))
        counts[proto.rsplit("_", 1)[0] if proto[-1].isdigit() else proto] = counts.get(
            proto.rsplit("_", 1)[0] if proto[-1].isdigit() else proto, 0) + 1
        return place(target, info[proto]["col"], proto, (x, y, z), yaw if yaw is not None else rng.uniform(0, math.tau), scale)

    def variant(sp):
        return f"tree_{sp}_{rng.randrange(len(SPECIES[sp]))}"

    def try_species(x, y, order, **kw):
        for sp in order:
            proto = variant(sp)
            if site.ok(x, y, info[proto]["r"], **kw) and site.overhang_ok(x, info[proto]["r"], info[proto]["low"]):
                put(proto, x, y)
                return True
        return False

    # 1. Rizal Hall's front: Manila palms flanking the steps, royal palms at the lawn corners,
    #    santan along the front and round the Oblation.
    ox, oy = site.oblation
    fy = site.front_y
    palms = [(-7.5, -4.0, 0), (-11.0, -3.2, 1), (-14.5, -4.6, 0), (-18.0, -3.4, 1), (-9.5, -8.5, 1),
             (-16.0, -9.5, 0), (17.5, -3.2, 1), (21.0, -4.8, 0), (-23.0, -13.0, "royal"), (-23.0, -3.0, "royal"),
             (24.5, -3.0, "royal")]
    for dx, dy, kind in palms:
        proto = "tree_royal_palm" if kind == "royal" else f"tree_manila_palm_{kind}"
        x, y = ox + dx + rng.uniform(-0.3, 0.3), fy + dy + rng.uniform(-0.3, 0.3)
        r = info[proto]["r"]
        if (site.view_clear(x, y, r) and not site.in_building(x, y, 0.8) and not site.on_road(x, y)
                and all(math.hypot(x - px, y - py) > 3.0 for px, py, _ in site.placed)):
            put(proto, x, y, scale=rng.uniform(0.92, 1.06))
    for k in range(10):
        x = ox - 12 + k * 24 / 9 + rng.uniform(-0.3, 0.3)
        if abs(x - ox) < 4.0:
            continue
        y = fy - 1.6 + rng.uniform(-0.2, 0.2)
        if not site.in_building(x, y, 0.5):
            put(f"tree_ixora_{rng.randrange(3)}", x, y, scale=rng.uniform(0.9, 1.1))
    for k in range(6):
        a = k * math.tau / 6 + 0.3
        put(f"tree_ixora_{k % 3}", ox + math.cos(a) * 2.2, oy + math.sin(a) * 2.2, scale=0.85)
    # 2. The PGH frontage: big rain trees along the fence, canopies over the west pavement.
    y = -200.0
    while y < 200:
        x = -(CORRIDOR_X + 0.6) - rng.uniform(0, 3.5)
        for proto in (variant("raintree"), variant("narra"), variant("mango")):
            r = info[proto]["r"] * 1.0
            x2 = min(x, -(DECK_CLEAR_X + r))
            if site.ok(x2, y, r) and site.overhang_ok(x2, r, info[proto]["low"]):
                put(proto, x2, y, scale=1.0)
                break
        y += rng.uniform(13, 18)
    # 3. Padre Faura's kerbs: figs in pits every ~10 m, both sides.
    for road, half in site.streets:
        if "Padre Faura" not in road["name"]:
            continue
        line = road["line"]
        for (x0, y0), (x1, y1) in zip(line, line[1:]):
            Ls = math.hypot(x1 - x0, y1 - y0)
            nx, ny = -(y1 - y0) / Ls, (x1 - x0) / Ls
            t = rng.uniform(2, 8)
            while t < Ls:
                for side in (-1, 1):
                    px = x0 + (x1 - x0) * t / Ls + side * nx * (half + 1.0)
                    py = y0 + (y1 - y0) * t / Ls + side * ny * (half + 1.0)
                    proto = variant("fig")
                    r = info[proto]["r"]
                    other = any(seg_dist(px, py, a, b, c, d) < cl - 0.5 for a, b, c, d, cl in site.roads
                                if abs((a + c) / 2 - px) < 80 and seg_dist(px, py, a, b, c, d) < half + B.SIDEWALK - 0.2
                                and not (abs(a - x0) < 1e-6 and abs(b - y0) < 1e-6))
                    if not other and site.ok(px, py, r, trunk_margin=0.9, canopy_k=0.5, street=True):
                        put(proto, px, py, z=B.PAVE_TOP, scale=rng.uniform(0.92, 1.05))
                        place(target, info["tree_pit"]["col"], "tree_pit", (px, py, B.PAVE_TOP), rng.uniform(-0.05, 0.05))
                t += rng.uniform(9, 12)
    # 4. The mapped trees (OSM), species by where they stand.
    for x, y in site.layout["trees"]:
        near_street = site.on_road(x, y, 3.0)
        order = ["mango", "narra"] if near_street else ["raintree", "narra", "mango"]
        rng.shuffle(order)
        try_species(x, y, order + ["mango"])
    # 5. The scatter: a jittered grid, visited in random order. Dense on the campus (west), sparse
    #    on the commercial east, where the photographs show a tree only where a lot is open.
    cells = []
    step = 6.0
    g = -B.EXTENT + 6
    while g < B.EXTENT - 6:
        h = -B.EXTENT + 6
        while h < B.EXTENT - 6:
            cells.append((g + rng.uniform(0, step), h + rng.uniform(0, step)))
            h += step
        g += step
    rng.shuffle(cells)
    for x, y in cells:
        west = x < 0
        if not west and rng.random() > 0.3:
            continue
        if west:
            first = rng.choices(["raintree", "narra", "mango"], weights=(0.5, 0.25, 0.25))[0]
        else:
            first = rng.choices(["raintree", "narra", "mango"], weights=(0.2, 0.3, 0.5))[0]
        try_species(x, y, list(dict.fromkeys([first, "narra", "mango"])))
    # 6. Kamuning hedges just inside the PGH fence, in runs with gaps.
    y = -60.0
    while y < -3:
        if not site.in_building(-12.6, y, 0.6) and not site.on_road(-12.6, y):
            place(target, info["tree_hedge"]["col"], "tree_hedge", (-12.6, y, GROUND_Z), math.pi / 2, 1.0)
            counts["tree_hedge"] = counts.get("tree_hedge", 0) + 1
        y += 4.0 if rng.random() < 0.75 else 7.0
    print("[ilalim-trees] planted:", counts, "total", sum(counts.values()))
    return counts


# ------------------------------------------------------------------ lighting, context, review

def lighting():
    scene = bpy.context.scene
    world = bpy.data.worlds.new("sky")
    scene.world = world
    if world.node_tree is None:
        world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (0.74, 0.80, 0.90, 1)
    bg.inputs["Strength"].default_value = 0.6
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


def context():
    """Renders only: the LRT-1 guideway linked, and the blockout's ground, Taft corridor, buildings,
    Rizal Hall and street details appended, minus the blockout's own tree blobs and labels."""
    with bpy.data.libraries.load(str(SOURCE / "lrt_kit.blend"), link=True) as (src, dst):
        dst.collections = [c for c in src.collections if c == "guideway over the court"]
    ctx = collection("context (renders only)")
    for c in dst.collections:
        ctx.children.link(c)
    want = ["Ground", "Taft corridor", "Buildings (OSM)", "Rizal Hall portico", "Street details (OSM)", "Horizon",
            "East podium shops", "Gameplay"]
    with bpy.data.libraries.load(str(SOURCE / "ilalim_blockout.blend"), link=False) as (src, dst):
        dst.collections = [c for c in src.collections if c in want]
    for c in dst.collections:
        ctx.children.link(c)
        for o in list(c.objects):
            if o.name.startswith(("tree trunk", "tree canopy")) or o.type == "FONT" or o.name.startswith(("wall E/W", "wall N/S")):
                bpy.data.objects.remove(o)
    return ctx


def median_standin(y0, y1):
    """A stand-in median planter (renders only): a low green-painted wall along x = 0 with soil,
    filled with lily beds, outside the play area."""
    col = collection("median lilies (review stand-in)")
    m = Mesh()
    mat = bpy.data.materials.new("standin planter")
    mat.use_nodes = True
    mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.22, 0.36, 0.26, 1)
    soil = wood_material("tree_pit_soil")
    for s in (-1, 1):
        x0, x1 = sorted((s * 0.55, s * 0.7))
        v = [Vector(p) for p in ((x0, y0, -0.05), (x1, y0, -0.05), (x1, y1, -0.05), (x0, y1, -0.05),
                                 (x0, y0, 0.45), (x1, y0, 0.45), (x1, y1, 0.45), (x0, y1, 0.45))]
        m.add(v, [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)], mat)
    v = [Vector(p) for p in ((-0.56, y0, 0.38), (0.56, y0, 0.38), (0.56, y1, 0.38), (-0.56, y1, 0.38))]
    m.add(v, [(0, 1, 2, 3)], soil, [(p.x / 2, p.y / 2) for p in v])
    obj(col, "standin planter", m.build("standin planter"), bevel=0.02)
    return col


def preview(version, info, only=None):
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 1000
    if hasattr(scene, "eevee"):
        scene.eevee.taa_render_samples = 48
    ctx = context()
    # The lineup: one of each species on a stand-in lawn far from the map, for the close-ups.
    line = collection("lineup (renders only)")
    lawn = Mesh()
    lm = bpy.data.materials.new("standin lawn")
    lm.use_nodes = True
    lm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.30, 0.42, 0.18, 1)
    v = [Vector(p) for p in ((900, -60, 0), (1100, -60, 0), (1100, 60, 0), (900, 60, 0))]
    lawn.add(v, [(0, 1, 2, 3)], lm)
    obj(line, "standin lawn", lawn.build("standin lawn"))
    spots = {"tree_raintree_0": (960, 0), "tree_narra_0": (1000, 0), "tree_mango_0": (1030, 0), "tree_fig_0": (1050, 0),
             "tree_manila_palm_0": (1065, 0), "tree_royal_palm": (1075, 4), "tree_ixora_1": (1085, -2),
             "tree_hedge": (1085, 2), "tree_pit": (1050, 0)}
    for name, (x, y) in spots.items():
        place(line, info[name]["col"], name, (x, y, 0.1 if name == "tree_pit" else 0.0), 0.4, 1.0)
    median = median_standin(20.5, 36.0)
    for k in range(5):
        place(median, info["tree_lily_bed"]["col"], "tree_lily_bed", (0, 22.5 + k * 3.05, 0.38), math.pi / 2, 1.0)
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    cam.data.clip_end = 3000
    scene.collection.objects.link(cam)
    scene.camera = cam
    eye = 18 / math.tan(math.radians(95 / 2))
    e = 1.25
    pv = B.PAVE_TOP
    portico = Vector((info["_portico"][0], info["_portico"][1], 9.0))
    shots = [
        ("eye_west", Vector((0.0, -B.SPAWN, e)), Vector((-40, -4, 4)), eye),
        ("eye_northwest_rizal", Vector((3.0, -B.SPAWN, e)), portico, eye),
        ("eye_court_to_rizal", Vector((-8.0, 16.0, pv + e)), portico, eye),
        ("eye_west_pavement_south", Vector((-9.5, 12.0, pv + e)), Vector((-9.0, -30, 4)), eye),
        ("eye_east_pavement_west", Vector((9.5, -2.0, pv + e)), Vector((-40, 6, 5)), eye),
        ("aerial", Vector((70.0, -120.0, 95.0)), Vector((-60, 40, 0)), 24),
        ("aerial_campus", Vector((-10.0, -60.0, 60.0)), Vector((-90, 60, 0)), 22),
        ("rizal_front", Vector((portico.x + 8, info["_portico"][1] - 32, 1.9)), Vector((portico.x, info["_portico"][1], 6)), 24),
        ("close_raintree", Vector((960 + 4, -24, 3.0)), Vector((960, 0, 6.5)), 22),
        ("close_narra", Vector((1000 + 3, -19, 3.0)), Vector((1000, 0, 7.0)), 24),
        ("close_mango", Vector((1030 + 2, -15, 2.2)), Vector((1030, 0, 4.2)), 24),
        ("close_fig", Vector((1050 + 1.5, -8.5, 1.6)), Vector((1050, 0, 3.2)), 24),
        ("close_palms", Vector((1070, -16, 2.0)), Vector((1070, 2, 6.0)), 24),
        ("close_shrubs", Vector((1085.5, -6.0, 1.3)), Vector((1085, 0, 0.5)), 28),
        ("close_lilies", Vector((2.2, 17.3, 1.2)), Vector((0, 26, 0.3)), 24),
    ]
    for name, pos, tgt, lens in shots:
        if only and name not in only:
            continue
        cam.location, cam.data.lens = pos, lens
        cam.rotation_euler = (tgt - pos).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = str(PREVIEWS / f"tree_{name}_v{version}.png")
        bpy.ops.render.render(write_still=True)
        print("[ilalim-trees] preview", scene.render.filepath)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
    only = set(argv[argv.index("--shots") + 1].split(",")) if "--shots" in argv else None
    layout = B.sightline_override(json.loads(B.LAYOUT.read_text(encoding="utf-8")))
    bpy.ops.wm.read_factory_settings(use_empty=True)
    kit, info = build_kit()
    target = collection("trees over Ilalim")
    site = Site(layout)
    plant(site, info, target)
    lighting()
    SOURCE.mkdir(parents=True, exist_ok=True)
    out = SOURCE / "trees.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(out), compress=True)
    backup = SOURCE / "trees.blend1"
    if backup.exists():
        backup.unlink()
    print("[ilalim-trees] saved", out)
    if version:
        info["_portico"] = site.portico
        preview(version, info, only)


if __name__ == "__main__":
    main()
