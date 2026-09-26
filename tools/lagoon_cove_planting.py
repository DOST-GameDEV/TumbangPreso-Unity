"""Lagoon Court planting: coconut palms, grass tufts, banana and taro, flowering bushes, and the
reference's accents: croton, monstera and ground cover.

  import lagoon_cove_planting as P
  P.palm(c, rng, x, y, z, height=None, lean=None)
  P.tuft(c, rng, x, y, z, scale=1.0)
  P.broadleaf(c, rng, x, y, z, scale=1.0)
  P.flower_bush(c, rng, x, y, z, scale=1.0)
  P.croton(c, rng, x, y, z, scale=1.0)
  P.monstera(c, rng, x, y, z, scale=1.0)
  P.groundcover(c, rng, x, y, z, scale=1.0)
  n = P.plant_gaps(c, rng, spots, height_fn, avoid_fn=None, mix=P.GAP_MIX)

2026-09-27, THE PALMS AND THE VARIETY (docs/LAGOON_REWORK_GUIDE.md section 7a items 6 and 7). The
reference's palms have CURVED trunks (S bends, gentle arcs, a few nearly straight) in warm banded
bark, and BROAD, CHUNKY, heavily drooping fronds with a yellow-green gradient; ours were straight,
grey and thin-leafed. The palm kit now sweeps its trunk along a per-variant curve with a slight
root flare, wears the repainted warm banded palm_trunk, and crowns it with frond_broad cards that
droop hard, over two narrow splayed young spears (the old three wide ones met as a flat "tent").
Its rock feet add croton, monstera and ground cover, all Kanto leaf cards.

It has to read as the right plants from 20 to 150 m: the reference (Papaioanou, "Stylized Fishing
Village") has coconut palms leaning out of rock seams, broad leaves, grass tufts and red flowering
accents at the boulder feet.

EVERY LEAF IS A KANTO LEAF CARD. OWNER, 2026-09-27: "i really like what was used for leaves in
kanto. just need to ensure it matches this environment". So the METHOD is Kanto's, kept exactly
(docs/KANTO_DESIGN_GUIDE.md section 5, `foliage()` and `leaf_material()` in
tools/author_kanto_models.py): a see-through card carrying one soft-painted greyscale drawing,
tinted by its material, cut out by its own alpha, lit from both sides, with custom normals pointing
away from the plant's centre so a plant of flat cards shades as one soft form. What changes for
the tropics is the drawing and the card's shape: the drawings are painted by
tools/author_lagoon_leaves.py (a whole coconut frond, a banana paddle, a grass blade, Kanto's round
leaf), and a frond or paddle card is SUBDIVIDED along its length and bent, since a coconut frond's
whole character is that it arches up and then droops, which a flat quad cannot do.

WHY THE CARDS ARE FOLDED. A flat card is invisible edge-on, and from the court most fronds are
seen edge-on. Every frond, blade and paddle card has a V fold across its width (the midrib raised,
the edges dropped), so from the side it still shows a strip of leaf.

TWO TINTS A PLANT, LIGHT ON TOP AND DARK UNDERNEATH, CHOSEN PER CARD BY WHERE THE CARD SITS ON
THE PLANT (upper-tier fronds light, the hanging lower tier dark), never at random: per-leaf colour
noise was tried in Kanto and reverted by the owner. The tint pairs are imported from
author_lagoon_leaves.TINTS, the same numbers its review sheet was approved on.

WHY THE MESHES ARE SHARED. A cove carries several hundred plants. Each plant TYPE is built as a
small kit of whole-plant variant meshes, built once per file from its own fixed seed, and every
plant is ONE object linking one of those meshes with its own location, turn and scale. So 650
plants cost 650 objects and about 20 meshes, and the layout script's rng only decides WHERE and
WHICH, never the shape of a leaf. Per-PLANT colour variation (a few per cent of value and hue) comes
from the object's random number in the leaf shader, so a shared mesh still differs plant to plant.

THE REST OF THE PLANT IS PAINTED TOO. OWNER, 2026-09-27, reviewing the plants in Blender:
"stem part of this leafy plant is untextured", "coconut and trunks of palm tree are untextured",
"is the pink stuff supposed to look like this or did u forget to texture". The trunk (leaf-scar
rings, 2 m a tile), the coconuts (smooth UV spheres wearing a painted husk), the banana pseudo-stem
and every leaf stalk wear their own drawings from author_lagoon_leaves.py, UV'd round and along,
and the bushes flower in FLOWER CARDS (gumamela, bougainvillea) in place of the pink blobs.
Materials: lagoon_palm_trunk, lagoon_coconut, lagoon_banana_stem,
lagoon_stalk_<banana|taro|croton|monstera>, lagoon_flower_<gumamela|bougainvillea>_<red|yellow>,
and the lagoon_leaf_* cards (new: lagoon_leaf_frond_broad_palm_<light|dark>,
lagoon_leaf_croton_croton_<light|dark>, lagoon_leaf_monstera_monstera_<light|dark>,
lagoon_leaf_round_groundcover_<light|dark>).

Colours: nothing near offence orange #f87020 or defence blue #0080e8 (Art_Direction.md section 1).
"""
import ast
import math
import random
from pathlib import Path

import bpy
from mathutils import Vector
from mathutils.geometry import normal as poly_normal

ROOT = Path(__file__).resolve().parents[1]
TEXTURES = ROOT / "ArtSource" / "lagoon" / "textures"


def _from_leaves(name):
    """A constant from author_lagoon_leaves (TINTS, STALK_TINTS, FLOWER_TINTS, FLOWER_KEY,
    FLOWER_PALE), the one place the plant colours live. That module paints with PIL, which
    Blender's bundled Python does not ship, so when the import fails the value is read out of its
    source as a literal: still the same numbers from the same file, never a retyped copy."""
    try:
        import author_lagoon_leaves as leaves
        return getattr(leaves, name)
    except ImportError:
        src = (Path(__file__).resolve().parent / "author_lagoon_leaves.py").read_text(encoding="utf-8")
        for node in ast.parse(src).body:
            if isinstance(node, ast.Assign) and any(getattr(t, "id", None) == name for t in node.targets):
                return ast.literal_eval(node.value)
        raise


TINTS = _from_leaves("TINTS")
STALK_TINTS = _from_leaves("STALK_TINTS")
FLOWER_TINTS = _from_leaves("FLOWER_TINTS")
FLOWER_KEY = _from_leaves("FLOWER_KEY")
FLOWER_PALE = _from_leaves("FLOWER_PALE")
COLOUR_TINTS = _from_leaves("COLOUR_TINTS")

# ---------------------------------------------------------------- materials

# The flower colour a bush wears, by its old material name: kept as the public names so a layout
# script asking for flower="plant_flower_red" still gets a crimson bush.
FLOWERS = ("plant_flower_red", "plant_flower_yellow")

# Texture file per card type, and its width over its length. A card is built at the drawing's own
# aspect (times a small per-type squeeze) so the painted leaf is never stretched.
LEAF_TEX = {"frond": ("frond_albedo.png", 0.5), "paddle": ("paddle_albedo.png", 0.5),
            "blade": ("blade_albedo.png", 0.25), "round": ("leaf_round_albedo.png", 1.0),
            "frond_broad": ("frond_broad_albedo.png", 0.5), "croton": ("croton_leaf_albedo.png", 0.5),
            "monstera": ("monstera_leaf_albedo.png", 1.0)}

# The two drawings painted in COLOUR (author_lagoon_leaves: a frond's green-to-warm-tip gradient
# and a croton's crimson with yellow veins are several hues in one leaf, which one tint cannot
# give). Their card material multiplies by a COLOUR_TINTS pair at gain 1.0 instead of a TINTS
# pair at 1.25; everything else about the card is Kanto's.
COLOUR_LEAVES = {"frond_broad": "palm_broad", "croton": "croton"}

# One tile of the trunk drawing is this many metres of trunk, so its leaf-scar rings sit at their
# real 5 to 15 cm spacing on every palm whatever its height.
TRUNK_TILE_M = 2.0


def _srgb_to_linear(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def _image(nodes, filename, colour=True):
    img = nodes.new("ShaderNodeTexImage")
    img.image = bpy.data.images.load(str(TEXTURES / filename), check_existing=True)
    if not colour:
        img.image.colorspace_settings.name = "Non-Color"
    return img


def painted_material(name, tex, tint=None, normal=None, roughness=0.8):
    """A plant part that is not a card (trunk, coconut, banana stem, leaf stalk), wearing its own
    painted drawing from author_lagoon_leaves. OWNER, 2026-09-27: "coconut and trunks of palm tree
    are untextured", "stem part of this leafy plant is untextured": these were flat colours.
    `tint` multiplies a greyscale drawing by tint x 1.25, the leaf rule, so a taro stalk wears the
    taro green; `normal` adds the drawing's OpenGL normal map (the trunk's rings are ridges)."""
    m = bpy.data.materials.get(name)
    if m:
        return m
    m = bpy.data.materials.new(name)
    if m.node_tree is None:
        m.use_nodes = True
    nodes, links = m.node_tree.nodes, m.node_tree.links
    bsdf = nodes["Principled BSDF"]
    img = _image(nodes, tex)
    colour = img.outputs["Color"]
    if tint is not None:
        mix = nodes.new("ShaderNodeMix")
        mix.data_type, mix.blend_type = "RGBA", "MULTIPLY"
        mix.inputs["Factor"].default_value = 1.0
        links.new(colour, mix.inputs[6])
        mix.inputs[7].default_value = (*(min(1.0, c * 1.25) for c in tint), 1)
        colour = mix.outputs[2]
    links.new(colour, bsdf.inputs["Base Color"])
    if normal:
        nimg = _image(nodes, normal, colour=False)
        nmap = nodes.new("ShaderNodeNormalMap")
        nmap.inputs["Strength"].default_value = 1.0
        links.new(nimg.outputs["Color"], nmap.inputs["Color"])
        links.new(nmap.outputs["Normal"], bsdf.inputs["Normal"])
    bsdf.inputs["Roughness"].default_value = roughness
    m.diffuse_color = (*(tint or (0.5, 0.45, 0.35)), 1)
    return m


def trunk_material():
    return painted_material("lagoon_palm_trunk", "palm_trunk_albedo.png", normal="palm_trunk_normal.png",
                            roughness=0.85)


def coconut_material():
    return painted_material("lagoon_coconut", "coconut_albedo.png", roughness=0.55)


def banana_stem_material():
    return painted_material("lagoon_banana_stem", "banana_stem_albedo.png", roughness=0.6)


def stalk_material(plant):
    return painted_material(f"lagoon_stalk_{plant}", "stalk_albedo.png", tint=STALK_TINTS[plant], roughness=0.6)


def leaf_material(tex, plant, which):
    """lagoon_leaf_<tex>_<plant>_<light|dark>: Kanto's leaf_material with a tropical drawing.
    A colour drawing (COLOUR_LEAVES) takes its pair from COLOUR_TINTS at gain 1.0."""
    k = 0 if which == "light" else 1
    if tex in COLOUR_LEAVES:
        return _card_material(f"lagoon_leaf_{tex}_{plant}_{which}", LEAF_TEX[tex][0],
                              COLOUR_TINTS[COLOUR_LEAVES[tex]][k], gain=1.0)
    return _card_material(f"lagoon_leaf_{tex}_{plant}_{which}", LEAF_TEX[tex][0], TINTS[plant][k])


def flower_material(kind, colour):
    """lagoon_flower_<gumamela|bougainvillea>_<red|yellow>: a flower CARD, the leaf card's method
    with a flower drawing. OWNER, 2026-09-27: "is the pink stuff supposed to look like this or did u
    forget to texture" (the bushes carried faceted pink icosphere blobs). The drawing's pale parts
    (stamen, the tiny true flowers) are painted at full white and every petal under it, so above
    FLOWER_KEY the tint gives way to FLOWER_PALE: one tinted drawing, a cream stamen on a crimson
    flower. Unity: the same two steps in the leaf shader, or bake the two colours per material."""
    return _card_material(f"lagoon_flower_{kind}_{colour}", f"{kind}_albedo.png", FLOWER_TINTS[colour], key=True)


def _card_material(name, tex, rgb, key=False, gain=1.25):
    """Kanto's leaf_material. The greyscale drawing is MULTIPLIED by the tint x 1.25 (the painted
    value sits around 0.8, so this lands the leaf on the tint), cut out by the drawing's alpha,
    DITHERED rather than blended so hundreds of overlapping cards need no sorting, backface
    culling off since every card is seen from both sides, roughness 0.7.
    The one addition: the object's random number nudges value and hue by a few per cent, so two
    plants linking the same kit mesh are not identical twins. Per PLANT, never per leaf."""
    m = bpy.data.materials.get(name)
    if m:
        return m
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*rgb, 1)
    if m.node_tree is None:
        m.use_nodes = True
    nodes, links = m.node_tree.nodes, m.node_tree.links
    bsdf = nodes["Principled BSDF"]
    img = _image(nodes, tex)
    mix = nodes.new("ShaderNodeMix")
    mix.data_type, mix.blend_type = "RGBA", "MULTIPLY"
    mix.inputs["Factor"].default_value = 1.0
    links.new(img.outputs["Color"], mix.inputs[6])
    mix.inputs[7].default_value = (*(min(1.0, c * gain) for c in rgb), 1)
    if key:
        # The image node hands out LINEAR values, so the key thresholds (painted as sRGB) are
        # converted; a grey drawing's colour reads as its value when plugged into a float input.
        rng_node = nodes.new("ShaderNodeMapRange")
        rng_node.inputs["From Min"].default_value = _srgb_to_linear(FLOWER_KEY[0])
        rng_node.inputs["From Max"].default_value = _srgb_to_linear(FLOWER_KEY[1])
        links.new(img.outputs["Color"], rng_node.inputs["Value"])
        pale = nodes.new("ShaderNodeMix")
        pale.data_type = "RGBA"
        links.new(rng_node.outputs["Result"], pale.inputs["Factor"])
        links.new(mix.outputs[2], pale.inputs[6])
        pale.inputs[7].default_value = (*(_srgb_to_linear(c) for c in FLOWER_PALE), 1)
        mix = pale
    info = nodes.new("ShaderNodeObjectInfo")
    val = nodes.new("ShaderNodeMapRange")
    val.inputs["To Min"].default_value, val.inputs["To Max"].default_value = 0.9, 1.08
    hue = nodes.new("ShaderNodeMapRange")
    hue.inputs["To Min"].default_value, hue.inputs["To Max"].default_value = 0.485, 0.515
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


# ---------------------------------------------------------------- mesh building

class _Mesh:
    """Accumulates verts and faces with a material slot per face, and per-vertex UVs and custom
    normals (every part now gives both: the cards their clump-shading normals, the tubes and
    nuts their own round normals, which also hide the UV seam a split vertex would show)."""

    def __init__(self):
        self.verts, self.faces, self.mats, self.smooth = [], [], [], []
        self.uvs, self.normals = [], []

    def add(self, verts, faces, slot, uvs=None, normals=None, smooth=False):
        base = len(self.verts)
        self.verts += [tuple(v) for v in verts]
        self.uvs += list(uvs) if uvs else [None] * len(verts)
        self.normals += list(normals) if normals else [None] * len(verts)
        self.add_faces(base, faces, slot, smooth)
        return base

    def add_faces(self, base, faces, slot, smooth=False):
        for f in faces:
            self.faces.append(tuple(base + i for i in f))
            self.mats.append(slot)
            self.smooth.append(smooth)

    def build(self, name, slots):
        """slots: material objects, in slot order."""
        me = bpy.data.meshes.new(name)
        me.from_pydata(self.verts, [], self.faces)
        for s in slots:
            me.materials.append(s)
        for p, m, sm in zip(me.polygons, self.mats, self.smooth):
            p.material_index = m
            p.use_smooth = sm
        me.validate()
        uv = me.uv_layers.new(name="UVMap")
        for loop in me.loops:
            st = self.uvs[loop.vertex_index]
            uv.data[loop.index].uv = st if st is not None else (0.0, 0.0)
        # A zero vector keeps Blender's own normal, so only the cards are overridden.
        me.normals_split_custom_set([self.normals[lp.vertex_index] or (0.0, 0.0, 0.0) for lp in me.loops])
        me.update()
        return me


def _frame(spine, i):
    """Tangent, side (horizontal, across the leaf) and up (the leaf's upper face) at spine[i]."""
    n = len(spine)
    t = (spine[min(i + 1, n - 1)] - spine[max(i - 1, 0)]).normalized()
    side = t.cross(Vector((0, 0, 1)))
    if side.length < 1e-4:
        side = Vector((1, 0, 0))
    side.normalize()
    up = side.cross(t).normalized()
    if up.z < 0:
        up, side = -up, -side
    return t, side, up


def _card(mesh, slot, spine, width, fold, centre, up_bias=0.8):
    """A LEAF CARD bent along `spine`: three verts a section (edge, midrib, edge), U 0..1 across
    and V 0..1 base to tip by arc length, so the drawing maps stem to tip and bends with the card.
    `fold` drops both edges below the midrib by fold x half-width (negative raises them into a
    channel): the V that keeps a card visible edge-on.
    NORMALS. Kanto points every card's normal away from its clump centre so a clump shades as one
    ball. A frond or paddle RADIATES from the centre rather than lying on a ball around it, so the
    pure radial direction lies along the leaf; it is blended with the card's own upper-face normal
    (`up_bias`), which keeps the sunlit top of each leaf lit and its hanging tip darkening the way
    the radial term says. The faces are wound so their front is the upper face, which is what makes
    the underside read dark (Blender flips the normal on a back face)."""
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
        v = lengths[i] / total
        for co, u in zip(row, (0.0, 0.5, 1.0)):
            verts.append(co)
            uvs.append((u, v))
            r = co - centre
            r = r.normalized() if r.length > 1e-5 else up
            normals.append((r + up * up_bias).normalized())
        ups.append(up)
    faces = []
    for i in range(len(spine) - 1):
        a, b = i * 3, (i + 1) * 3
        faces += [(a, b, b + 1, a + 1), (a + 1, b + 1, b + 2, a + 2)]
    first = poly_normal([verts[k] for k in faces[0]])
    if first.dot(ups[0]) < 0:
        faces = [tuple(reversed(f)) for f in faces]
    mesh.add(verts, faces, slot, uvs, normals, smooth=True)


def _flat_card(mesh, slot, at, tip, normal, length, width, centre):
    """Kanto's `leaf()` card, one quad from `at` along `tip`, facing `normal`, with its normals
    pointing away from `centre` (the clump's), exactly as author_kanto_models.Buf.finish sets them."""
    side = normal.cross(tip).normalized() * (width / 2)
    base = at - tip * (length * 0.08)
    end = at + tip * (length * 0.92)
    cos = [base - side, base + side, end + side, end - side]
    face = (0, 1, 2, 3)
    if poly_normal(cos).dot(normal) < 0:
        face = (3, 2, 1, 0)
    nrm = [(c - centre).normalized() for c in cos]
    mesh.add(cos, [face], slot, ((0, 0), (1, 0), (1, 1), (0, 1)), nrm, smooth=True)


def _arc(start, azimuth, pitch0, pitch1, length, steps, bend=1.0):
    """A spine leaving start toward azimuth, its pitch sweeping from pitch0 to pitch1 (radians,
    up positive). bend > 1 keeps it straight longer and droops it late, like a frond's tip."""
    pts = [Vector(start)]
    ds = length / steps
    for k in range(steps):
        t = (k + 0.5) / steps
        pitch = pitch0 + (pitch1 - pitch0) * (t ** bend)
        d = Vector((math.cos(azimuth) * math.cos(pitch), math.sin(azimuth) * math.cos(pitch), math.sin(pitch)))
        pts.append(pts[-1] + d * ds)
    return pts


def _tube(mesh, slot, path, radii, sides=6, v_len=None):
    """A swept tube along path, UV'd for a painted drawing: U once round (the seam column is
    doubled so U runs 0..1 without a wrap), V along the length by arc length, divided by `v_len`
    metres a tile (the trunk: TRUNK_TILE_M, so the rings are real size) or by the tube's own length
    when v_len is None (a stem: V 0 at the foot, 1 at the top, so the drawing's dry base stays at
    the base). Normals point straight out from the axis, so the doubled seam shades as one.
    The old version banded a flat ring material every other segment; the rings are painted now."""
    lengths = [0.0]
    for a, b in zip(path, path[1:]):
        lengths.append(lengths[-1] + (b - a).length)
    scale = v_len if v_len else (lengths[-1] or 1.0)
    verts, uvs, normals, faces = [], [], [], []
    n = len(path)
    prev_side = None
    for i, p in enumerate(path):
        t = (path[min(i + 1, n - 1)] - path[max(i - 1, 0)]).normalized()
        side = t.cross(Vector((0, 1, 0))) if prev_side is None else prev_side - t * prev_side.dot(t)
        if side.length < 1e-4:
            side = t.cross(Vector((1, 0, 0)))
        side.normalize()
        prev_side = side
        other = t.cross(side).normalized()
        for k in range(sides + 1):
            a = math.tau * k / sides
            out = side * math.cos(a) + other * math.sin(a)
            verts.append(p + out * radii[i])
            uvs.append((k / sides, lengths[i] / scale))
            normals.append(out)
    row = sides + 1
    for i in range(n - 1):
        for k in range(sides):
            a, b = i * row + k, i * row + k + 1
            faces.append((a, b, b + row, a + row))
    top = len(verts)
    verts.append(path[-1])
    uvs.append((0.5, lengths[-1] / scale))
    normals.append((path[-1] - path[-2]).normalized())
    for k in range(sides):
        faces.append(((n - 1) * row + k, (n - 1) * row + k + 1, top))
    mesh.add(verts, faces, slot, uvs, normals, smooth=True)


def _oriented(verts, faces, outward):
    """Wind every face so its front looks along outward(face centre)."""
    out = []
    for f in faces:
        cos = [verts[k] for k in f]
        c = sum(cos, Vector()) / len(cos)
        out.append(tuple(reversed(f)) if poly_normal(cos).dot(outward(c)) < 0 else tuple(f))
    return out


def _nut(mesh, slot, centre, radius, facing, seg=14, rings=9):
    """A coconut: a smooth, slightly squashed UV sphere with the painted husk wrapped round it
    (U around, V bottom to top, so the drawing's darker cap sits where the nut hangs from the
    bunch). Turned so the drawing's highlight (U 0.25) faces `facing`, out of the crown.
    OWNER, 2026-09-27: "coconut and trunks of palm tree are untextured". The nuts were faceted
    one-level icospheres in a flat brown; these are rounder and softer, which is the cute blocky
    world's version of a nut (Art_Direction.md section 0), not a realistic one."""
    centre = Vector(centre)
    turn = math.atan2(facing.y, facing.x) - math.tau * 0.25
    squash = (1.0, 1.0, 0.88)
    verts, uvs, normals, faces = [], [], [], []
    for j in range(rings + 1):
        el = -math.pi / 2 + math.pi * j / rings
        for k in range(seg + 1):
            a = math.tau * k / seg + turn
            d = Vector((math.cos(el) * math.cos(a), math.cos(el) * math.sin(a), math.sin(el)))
            verts.append(centre + Vector((d.x * squash[0], d.y * squash[1], d.z * squash[2])) * radius)
            u = (k + 0.5) / seg if j in (0, rings) else k / seg
            uvs.append((u, j / rings))
            normals.append(Vector((d.x / squash[0], d.y / squash[1], d.z / squash[2])).normalized())
    row = seg + 1
    for j in range(rings):
        for k in range(seg):
            a, b = j * row + k, j * row + k + 1
            if j == 0:
                faces.append((b + row, a + row, a))           # the bottom pole: one triangle
            elif j == rings - 1:
                faces.append((a, b, a + row))                 # the top pole
            else:
                faces.append((a, b, b + row, a + row))
    faces = _oriented(verts, faces, lambda c: c - centre)
    mesh.add(verts, faces, slot, uvs, normals, smooth=True)


def _flower_card(mesh, slot, at, facing, size, spin, centre, cup=0.16):
    """A FLOWER CARD in the leaf card's method: one painted flower on a see-through card lying on
    the bush, facing out. A 3 x 3 grid rather than a quad so it can be a shallow cup (the rim lifted
    toward the viewer, the centre sunk), which keeps a strip of flower visible when it is seen
    obliquely, as a V fold does for a frond. Normals as the leaves': away from the clump's centre,
    blended with the card's own facing, so a flower shades with its bush."""
    n = facing.normalized()
    ref = Vector((0, 0, 1)) if abs(n.z) < 0.9 else Vector((1, 0, 0))
    a = n.cross(ref).normalized()
    b = n.cross(a)
    a, b = a * math.cos(spin) + b * math.sin(spin), b * math.cos(spin) - a * math.sin(spin)
    verts, uvs, normals = [], [], []
    for j in range(3):
        for i in range(3):
            x, y = i - 1, j - 1
            co = at + (a * x + b * y) * (size / 2) + n * (cup * size * ((x * x + y * y) / 2 - 0.35))
            verts.append(co)
            uvs.append((i / 2, j / 2))
            r = co - centre
            r = r.normalized() if r.length > 1e-5 else n
            normals.append((n + r * 0.8 + Vector((0, 0, 0.3))).normalized())
    faces = _oriented(verts, [(0, 1, 4, 3), (1, 2, 5, 4), (3, 4, 7, 6), (4, 5, 8, 7)], lambda c: n)
    mesh.add(verts, faces, slot, uvs, normals, smooth=True)


# ---------------------------------------------------------------- variant kits

def _kit(kind, count, builder):
    """The shared meshes for one plant type, built once per file. Cached by name in bpy.data so
    a factory reset (which empties bpy.data) simply rebuilds them."""
    names = [f"plant_{kind}_{i}" for i in range(count)]
    meshes = [bpy.data.meshes.get(n) for n in names]
    if any(m is None for m in meshes):
        meshes = [builder(i, names[i]) for i in range(count)]
    return meshes


# Palm variants: (height, lean in degrees, trunk shape). The whole plant is authored leaning toward
# +X and turned to its heading at placement; the height is matched by a uniform scale of at most
# about 12 per cent, so a frond never grows past 4.5 m or shrinks under 3.
# THE SHAPE IS NEW (docs/LAGOON_REWORK_GUIDE.md section 7a item 6: the reference's trunks "curve
# (S bends, leaning)", ours were "straight, grey"). Three kinds, as the reference's foliage sheet
# has them: "arc" leans out from the seam and curves back up toward the light, "s" swings out, back
# and out again (with a little sideways sway so it is an S from most angles, not only from one),
# "straight" is nearly straight with a slight bow. Still six variants, so `palm()` draws exactly
# the same random numbers as before and the cove's layout stream is unchanged.
PALM_VARIANTS = [(6.5, 10, "straight"), (7.5, 24, "arc"), (8.5, 16, "s"), (9.5, 26, "arc"),
                 (10.5, 18, "s"), (11.0, 7, "straight")]


def _trunk_path(height, lean_deg, shape, steps=22):
    """The trunk's centre line, base to crown, and its radii. The chord from base to crown makes
    lean_deg with the vertical; `shape` decides how the line wanders about that chord."""
    offset = height * math.tan(math.radians(lean_deg))
    path, radii = [], []
    for k in range(steps + 1):
        t = k / steps
        if shape == "arc":
            f, side = 0.8 * (2 * t - t * t) + 0.2 * t, 0.0
        elif shape == "s":
            # f' = 1 + 1.26 cos(2 pi t): out fast at the foot, BACK toward upright at mid height,
            # out again under the crown. A plain arc cannot give that swing.
            f, side = t + 0.2 * math.sin(math.tau * t), 0.12 * math.sin(math.pi * t)
        else:
            f, side = 0.3 * (2 * t - t * t) + 0.7 * t, 0.0
        z = height * t
        path.append(Vector((offset * f, offset * side, z)))
        # A slight ROOT FLARE over the lowest 0.9 m (a soft bell, not a cone), then a gentle taper.
        flare = 0.13 * max(0.0, 1 - z / 0.9) ** 2
        radii.append(0.29 - 0.12 * t + flare)
    path[0].z -= 0.35                                       # sunk into the ground
    return path, radii


def _palm_mesh(i, name):
    rng = random.Random(4100 + i)
    height, lean_deg, shape = PALM_VARIANTS[i]
    m = _Mesh()
    path, radii = _trunk_path(height, lean_deg, shape)
    # Twelve sides: the painted bands run round the trunk, and fewer sides show them as a polygon.
    # The bands are in the drawing, 2 m a tile along the trunk, so they stay real size on a curve.
    _tube(m, 0, path, radii, sides=12, v_len=TRUNK_TILE_M)
    top = path[-1]
    tangent = (path[-1] - path[-2]).normalized()
    # The crown's shading centre sits a little below the top, so the rising half of every frond
    # faces away from it (lit) and the hanging tips face down and out (shaded).
    centre = top - Vector((0, 0, 0.8))
    # CROWN. BROAD fronds (frond_broad: eleven wide leaflets a side with a yellow-green to warm-tip
    # gradient) that leave the top arching up and then DROOP HEAVILY, which is the reference's
    # palm: its fronds hang like a mop, where ours stood out like a star (section 7a item 6). Two
    # tiers: every other frond leaves lower and hangs almost straight down at its tip. The upper
    # tier wears the light tint and the hanging tier the dark one (Kanto: light on top, dark
    # underneath). The fold is deeper than the thin frond's (0.55 against 0.38): the leaflets hang
    # off both sides of the rib, which is also what keeps a frond visible edge-on.
    n = rng.randint(7, 9)
    phase = rng.uniform(0, math.tau)
    for k in range(n):
        az = phase + math.tau * k / n + rng.uniform(-0.2, 0.2)
        lower = k % 2 == 1
        length = rng.uniform(3.6, 4.2) if lower else rng.uniform(3.3, 3.9)
        up0 = math.radians(rng.uniform(6, 18) if lower else rng.uniform(26, 38))
        up1 = math.radians(rng.uniform(-92, -78) if lower else rng.uniform(-74, -56))
        # Fronds on the lean side droop a little more, so the crown hangs over the lean.
        if math.cos(az) > 0.3:
            up1 -= math.radians(8)
        spine = _arc(top + tangent * 0.1, az, up0, up1, length, 9, bend=1.05)
        _card(m, 2 if lower else 1, spine, length * LEAF_TEX["frond_broad"][1] * 1.1, 0.55, centre)
    # YOUNG FRONDS. Cove v48's three wide young fronds met in the middle as a flat pale "tent". Now
    # two narrow spears in the THIN frond drawing (a young frond is still folded shut), splayed
    # to opposite sides, one leaning more than the other, so the crown has a top and no tent.
    for k in range(2):
        az = phase + 0.5 + math.pi * k + rng.uniform(-0.3, 0.3)
        length = rng.uniform(1.6, 2.1)
        p0 = math.radians(rng.uniform(74, 80) if k == 0 else rng.uniform(64, 70))
        spine = _arc(top + tangent * 0.15, az, p0, p0 - math.radians(rng.uniform(16, 24)), length, 5)
        _card(m, 4, spine, length * LEAF_TEX["frond"][1] * 0.45, 0.25, centre)
    # COCONUTS: one or two BUNCHES hanging under the crown, three or four nuts a bunch packed
    # against each other and the trunk top, each nut's highlight turned out of the crown. A bunch
    # reads as coconuts from the court; nuts spread evenly round the trunk read as a collar.
    for b in range(rng.randint(1, 2)):
        az = phase + rng.uniform(0, math.tau) if b == 0 else az + math.pi + rng.uniform(-0.6, 0.6)
        out = Vector((math.cos(az), math.sin(az), 0))
        across = Vector((-out.y, out.x, 0))
        hub = top + out * 0.3 + Vector((0, 0, -0.42))
        for k, (s, h) in enumerate(((0, 0), (1, 0.05), (-1, 0.03), (0.2, -0.24))[:rng.randint(3, 4)]):
            r = rng.uniform(0.14, 0.17)
            c = hub + across * (s * 0.24) + out * (0.04 * abs(s)) + Vector((0, 0, h))
            _nut(m, 3, c, r, (c - top).normalized() + out)
    return m.build(name, [trunk_material(), leaf_material("frond_broad", "palm", "light"),
                          leaf_material("frond_broad", "palm", "dark"), coconut_material(),
                          leaf_material("frond", "palm", "light")])


def _tuft_mesh(i, name):
    rng = random.Random(4200 + i)
    m = _Mesh()
    n = rng.randint(7, 11)
    phase = rng.uniform(0, math.tau)
    centre = Vector((0, 0, -0.35))
    for k in range(n):
        az = phase + math.tau * k / n + rng.uniform(-0.3, 0.3)
        length = rng.uniform(0.75, 1.3)
        start = Vector((math.cos(az) * 0.05, math.sin(az) * 0.05, -0.12))
        p0 = rng.uniform(62, 84)
        p1 = rng.uniform(0, 38)
        spine = _arc(start, az, math.radians(p0), math.radians(p1), length, 5, bend=1.2)
        # The upright blades are the top of the tuft (light); the ones flopped over, dark.
        _card(m, 0 if p1 > 18 else 1, spine, length * LEAF_TEX["blade"][1] * 1.1, -0.35, centre, up_bias=0.6)
    return m.build(name, [leaf_material("blade", "grass", "light"), leaf_material("blade", "grass", "dark")])


def _broadleaf_mesh(i, name):
    """Even variants are BANANA (a fat pseudo-stem, long ribbed paddles rising and arching over),
    odd ones TARO (long stalks from the ground, broad blades hanging off their tips), each in its
    own tint pair from TINTS, so the kit's four variants are two plants and not one plant twice."""
    rng = random.Random(4300 + i)
    m = _Mesh()
    banana = i % 2 == 0
    plant = "banana" if banana else "taro"
    stem_base = Vector((0, 0, -0.1))
    # Stalks wear the painted stalk in the plant's own green: slot 3 on a banana (slot 0 is its
    # pseudo-stem), slot 0 on a taro, which has no stem.
    stalk_slot = 3 if banana else 0
    if banana:
        # A banana's pseudo-stem is tall and tapering, not a stump: at 1 m and 0.17 m thick it read
        # as a bottle (test render v1). The leaves leave from its top in a tight fan.
        # OWNER, 2026-09-27: "stem part of this leafy plant is untextured". It wears the painted
        # banana_stem now (sheath bands, dry brown sheaths at the foot), V foot to top, on ten
        # sides and five rings so the sheath bands are not drawn on a heptagon.
        h = rng.uniform(1.2, 1.6)
        path = [Vector((0, 0, -0.15 + (h + 0.15) * k / 4)) for k in range(5)]
        _tube(m, 0, path, [0.165, 0.145, 0.125, 0.105, 0.09], sides=10)
        stem_base = Vector((0, 0, h - 0.05))
    centre = stem_base + Vector((0, 0, 0.1 if banana else 0.2))
    n = rng.randint(4, 6)
    phase = rng.uniform(0, math.tau)
    for k in range(n):
        az = phase + math.tau * k / n + rng.uniform(-0.25, 0.25)
        low = k >= n - 2           # the two oldest leaves hang lowest and wear the dark tint
        if banana:
            stem = _arc(stem_base, az, math.radians(rng.uniform(66, 80)), math.radians(rng.uniform(58, 70)),
                        rng.uniform(0.25, 0.4), 3)
            # Banana leaves are huge and stand up before they arch over; short flat ones read
            # as a generic houseplant. Not steeper than about 60 degrees at the base: a paddle
            # standing straight up read as a tulip bud from the court (cove v40), which is also
            # why there is no rolled young leaf in the middle.
            length = rng.uniform(1.7, 2.2)
            p0 = rng.uniform(30, 42) if low else rng.uniform(48, 62)
            p1 = rng.uniform(-60, -40) if low else rng.uniform(-30, -5)
            spine = _arc(stem[-1], az, math.radians(p0), math.radians(p1), length, 8, bend=1.4)
            width = length * 0.38
        else:
            stem = _arc(stem_base, az, math.radians(rng.uniform(70, 82)), math.radians(rng.uniform(40, 55)),
                        rng.uniform(0.8, 1.2), 4)
            length = rng.uniform(0.8, 1.05)
            p0 = rng.uniform(0, 12) if low else rng.uniform(15, 30)
            p1 = rng.uniform(-70, -50) if low else rng.uniform(-50, -30)
            spine = _arc(stem[-1], az, math.radians(p0), math.radians(p1), length, 6, bend=1.1)
            width = length * 0.58
        # Six sides (was four): a painted stalk on a square section showed its corners.
        _tube(m, stalk_slot, stem, [0.045, 0.04, 0.035, 0.03, 0.028][:len(stem)], sides=6)
        # Pull the card's base a hand's width back along the stalk, so the stalk runs up the
        # midrib and the paddle never floats off the end of it.
        spine[0] = spine[0] - (spine[1] - spine[0]).normalized() * 0.08
        _card(m, 2 if low else 1, spine, width, 0.2, centre)
    slots = [banana_stem_material() if banana else stalk_material(plant),
             leaf_material("paddle", plant, "light"), leaf_material("paddle", plant, "dark")]
    if banana:
        slots.append(stalk_material(plant))
    return m.build(name, slots)


def _ellipsoid_area(a, b, c):
    p = 1.6075      # Knud Thomsen's approximation, within about 1 per cent
    return 4 * math.pi * (((a * b) ** p + (a * c) ** p + (b * c) ** p) / 3) ** (1 / p)


def _foliage(m, centre, radii, leaf_len, rng, lobes, floor_z=0.03, lift=0.25, spread=0.35, cover=2.6):
    """Kanto's `foliage()` (tools/author_kanto_models.py), ported onto a shared kit mesh: each card
    lies ON the ellipsoid, facing out, tip running DOWN the surface within `spread`, lifted by
    `lift`, spread evenly by a Fibonacci sphere, shingled like roof tiles so the clump is one ball.
    NO CORE: density closes the gaps (owner: "is there a need to keep the core visible?"). The
    card count comes from `cover`, how many times the leaves would tile the surface, so a bigger
    lobe gets more leaves rather than bigger ones. Cards whose base falls below `floor_z` or
    inside a neighbouring lobe are skipped: one grows out of the ground and the other is never
    seen. Light slot 0 above the clump's lower fifth, dark slot 1 under it, as in Kanto."""
    centre = Vector(centre)
    width = leaf_len * 0.8
    count = int(cover * _ellipsoid_area(*radii) / (leaf_len * width))
    golden = math.pi * (3 - math.sqrt(5))
    up = Vector((0, 0, 1))
    for i in range(count):
        zf = 1 - 2 * (i + 0.5) / count
        r = math.sqrt(max(0.0, 1 - zf * zf))
        a = golden * i + rng.uniform(-0.3, 0.3)
        d = Vector((math.cos(a) * r, math.sin(a) * r, zf))
        p = centre + Vector((d.x * radii[0], d.y * radii[1], d.z * radii[2])) * rng.uniform(0.9, 1.02)
        if p.z < floor_z:
            continue
        if any(((p - c2).x / r2[0]) ** 2 + ((p - c2).y / r2[1]) ** 2 + ((p - c2).z / r2[2]) ** 2 < 0.7
               for c2, r2 in lobes if (c2 - centre).length > 1e-6):
            continue
        down = -up - d * (-up).dot(d)
        if down.length < 0.2:
            t = Vector((rng.gauss(0, 1), rng.gauss(0, 1), 0))
            down = t - d * t.dot(d)
        down.normalize()
        side = d.cross(down)
        ang = rng.uniform(-spread, spread)
        tip = (down * math.cos(ang) + side * math.sin(ang) + d * lift).normalized()
        nrm = (d - tip * d.dot(tip)).normalized()
        length = leaf_len * rng.uniform(0.85, 1.15)
        _flat_card(m, 0 if d.z > -0.2 else 1, p - tip * length * 0.35, tip, nrm, length, length * 0.8, centre)


def _bush_mesh(i, name):
    rng = random.Random(4400 + i)
    m = _Mesh()
    # The body: a main dome and a few side lobes, each a shingled clump of round leaf cards.
    lobes = [(Vector((0, 0, 0.5)), (0.72, 0.72, 0.6))]
    for k in range(rng.randint(2, 3)):
        a = rng.uniform(0, math.tau)
        d = rng.uniform(0.45, 0.6)
        r = rng.uniform(0.42, 0.52)
        lobes.append((Vector((math.cos(a) * d, math.sin(a) * d, r * 0.7)), (r, r, r * 0.85)))
    for c, radii in lobes:
        _foliage(m, c, radii, 0.3, rng, lobes)
    # FLOWERS. OWNER, 2026-09-27: "is the pink stuff supposed to look like this or did u forget to
    # texture". They were faceted icosphere blobs in a flat pink, kept as solid dots because "a
    # card seen edge-on vanishes". They are FLOWER CARDS now, the leaves' own method: even variants
    # are GUMAMELA bushes (fewer, bigger hibiscus flowers), odd ones BOUGAINVILLEA (more, smaller
    # bract clusters, often two or three together), since one bush bearing both would be two
    # plants in one. Each card is shingled on the clump's surface facing out and a little up, so
    # from the court (looking across and down at a bush) most are seen face on, and each is a
    # shallow cup so the oblique ones keep a strip of colour. About a third sit half TUCKED
    # between the leaves, which is what makes them grow out of the bush rather than sit on it.
    kind = "gumamela" if i % 2 == 0 else "bougainvillea"
    count = rng.randint(11, 14) if kind == "gumamela" else 16
    placed, tries = 0, 0
    phase = rng.uniform(0, math.tau)
    while placed < count and tries < count * 6:
        tries += 1
        c, radii = lobes[rng.randrange(len(lobes))]
        # Round the bush by the golden angle, so every side flowers (random turns left the side
        # facing the camera bare in test v3).
        a = phase + tries * math.pi * (3 - math.sqrt(5)) + rng.uniform(-0.3, 0.3)
        el = math.asin(rng.uniform(0.0, 0.95))   # even by AREA over the upper half: v1 bunched them at the crown
        d = Vector((math.cos(a) * math.cos(el), math.sin(a) * math.cos(el), math.sin(el)))
        tucked = rng.random() < 0.35
        p = c + Vector((d.x * radii[0], d.y * radii[1], d.z * radii[2])) * (
            rng.uniform(1.1, 1.16) if tucked else rng.uniform(1.2, 1.3))
        # Those radii are measured against the LEAVES, not the lobe: a shingled leaf is lifted off
        # the ellipsoid and stands out to about 1.15 of its radius, so flowers at 1.03 to 1.08
        # (test v1) were all but hidden, and 1.15 to 1.24 (v2) only level with the tips. Tucked ones
        # now sit among the leaf tips and the rest just proud of them.
        if p.z < 0.2:
            continue   # at the foot, where no one sees a flower and the card would clip the ground
        if any(((p - c2).x / r2[0]) ** 2 + ((p - c2).y / r2[1]) ** 2 + ((p - c2).z / r2[2]) ** 2 < 0.8
               for c2, r2 in lobes if c2 is not c):
            continue   # buried inside a neighbouring lobe
        surf = Vector((d.x / radii[0], d.y / radii[1], d.z / radii[2])).normalized()
        facing = (surf + Vector((0, 0, 0.35))).normalized()
        if kind == "gumamela":
            _flower_card(m, 2, p, facing, rng.uniform(0.21, 0.25), rng.uniform(0, math.tau), c)
            placed += 1
        else:
            for _k in range(min(count - placed, rng.choice((1, 1, 2)))):
                q = p + Vector((rng.uniform(-0.08, 0.08), rng.uniform(-0.08, 0.08), rng.uniform(-0.06, 0.06)))
                _flower_card(m, 2, q, facing, rng.uniform(0.2, 0.25), rng.uniform(0, math.tau), c, cup=0.12)
                placed += 1
    me = m.build(name, [leaf_material("round", "bush", "light"), leaf_material("round", "bush", "dark"),
                        flower_material(kind, "red")])
    me["flower_kind"] = kind
    return me


# ---------------------------------------------------------------- the reference's accent plants
#
# docs/LAGOON_REWORK_GUIDE.md section 7a item 7: the reference's rock feet carry "red and orange
# ferny accents (croton), monstera-like round leaves, dense low ground cover"; ours had banana,
# taro, tufts and flower bushes only. Three more kinds, every leaf a Kanto card as the owner asked
# ("i really like what was used for leaves in kanto. just need to ensure it matches this
# environment"). The reference's ORANGE accent is crimson, coral-pink and yellow here: offence
# orange #f87020 is a gameplay colour and a plant may not wear it.

def _croton_mesh(i, name):
    """A croton: four to six short woody stems, each topped with a whorl of STIFF crimson-and-yellow
    leaves and more down its side, so the plant is a dense rounded clump about 0.8 m tall and a
    metre across. Stiff means little droop: a croton's leaves are leathery and hold their angle,
    which is what tells it from the soft banana and taro at a glance. Upper leaves light, lower
    ones dark. Test v1 had two to four tall stems with a few leaves each and read as a sparse
    seedling with its stems showing; the reference's red accents are full, leafy mounds."""
    rng = random.Random(4500 + i)
    m = _Mesh()
    centre = Vector((0, 0, 0.3))
    stems = rng.randint(4, 6)
    phase = rng.uniform(0, math.tau)
    for s_i in range(stems):
        az = phase + math.tau * s_i / stems + rng.uniform(-0.4, 0.4)
        base = Vector((math.cos(az) * 0.06, math.sin(az) * 0.06, -0.08))
        stem = _arc(base, az, math.radians(rng.uniform(62, 80)), math.radians(rng.uniform(55, 72)),
                    rng.uniform(0.12, 0.3), 3)
        _tube(m, 0, stem, [0.026, 0.023, 0.02, 0.017], sides=6)
        top = stem[-1]
        # The whorl at the top, rising and fanning out all round.
        w = rng.randint(8, 10)
        a0 = rng.uniform(0, math.tau)
        for k in range(w):
            a = a0 + math.tau * k / w + rng.uniform(-0.25, 0.25)
            length = rng.uniform(0.38, 0.52)
            p0 = math.radians(rng.uniform(25, 68))
            spine = _arc(top, a, p0, p0 - math.radians(rng.uniform(25, 40)), length, 4, bend=1.3)
            _card(m, 1, spine, length * LEAF_TEX["croton"][1] * 0.95, 0.22, centre)
        # Older leaves lower down the stem and at the foot, held flatter, in the dark tint: they
        # skirt the clump so no bare stem shows under it.
        for k in range(rng.randint(3, 4)):
            at = stem[rng.randint(0, 1)]
            a = rng.uniform(0, math.tau)
            length = rng.uniform(0.38, 0.5)
            p0 = math.radians(rng.uniform(0, 22))
            spine = _arc(at, a, p0, p0 - math.radians(rng.uniform(25, 40)), length, 4, bend=1.3)
            _card(m, 2, spine, length * LEAF_TEX["croton"][1] * 0.95, 0.22, centre)
    return m.build(name, [stalk_material("croton"), leaf_material("croton", "croton", "light"),
                          leaf_material("croton", "croton", "dark")])


def _monstera_mesh(i, name):
    """A monstera: three to six big split leaves, each held out on its own long stalk from the
    ground, the blade tilted to face up and out and drooping a little at its tip. The card is
    pulled back along the stalk so the stalk meets the blade in the drawing's notch, as a real
    monstera's does. The two oldest (lowest, flattest) leaves are dark."""
    rng = random.Random(4600 + i)
    m = _Mesh()
    # Test v1 held small leaves high on long thin stalks and read as lily pads on sticks. A
    # monstera is a MOUND of big leaves: shorter stalks, bigger blades, more of them, the blades
    # rising at the base and then arching over.
    n = rng.randint(4, 6)
    phase = rng.uniform(0, math.tau)
    centre = Vector((0, 0, 0.3))
    for k in range(n):
        az = phase + math.tau * k / n + rng.uniform(-0.3, 0.3)
        low = k >= n - 2
        base = Vector((math.cos(az) * 0.06, math.sin(az) * 0.06, -0.08))
        stalk_len = rng.uniform(0.3, 0.45) if low else rng.uniform(0.45, 0.7)
        stalk = _arc(base, az, math.radians(rng.uniform(68, 80)),
                     math.radians(rng.uniform(30, 45) if low else rng.uniform(45, 60)), stalk_len, 4)
        _tube(m, 0, stalk, [0.04, 0.036, 0.032, 0.028, 0.025], sides=6)
        length = rng.uniform(0.8, 1.05)
        # Tilted up and out rather than held flat (test v2's flat blades on stalks read as tables).
        p0 = math.radians(rng.uniform(20, 32) if low else rng.uniform(42, 56))
        spine = _arc(stalk[-1], az, p0, p0 - math.radians(rng.uniform(55, 70)), length, 6, bend=1.2)
        # The drawing's notch sits about 17 per cent up the card.
        back = (spine[1] - spine[0]).normalized() * (0.17 * length)
        spine = [p - back for p in spine]
        _card(m, 2 if low else 1, spine, length * LEAF_TEX["monstera"][1], 0.14, centre)
    return m.build(name, [stalk_material("monstera"), leaf_material("monstera", "monstera", "light"),
                          leaf_material("monstera", "monstera", "dark")])


def _groundcover_mesh(i, name):
    """Ground cover: a LOW, SPREADING mat of small round leaf cards (Kanto's own leaf drawing, in
    the groundcover green), a flattened main dome with two to four lobes crawling out from it,
    shingled by `_foliage` exactly as a Kanto clump is. A few small SPROUTS (grass blade cards)
    poke out of it, the reference's little upright shoots, so the mat is not a smooth pillow.
    Section 7a item 7 also calls our grass pockets "flat discs"; this is the plant that fills
    those spots with something that has a surface."""
    rng = random.Random(4700 + i)
    m = _Mesh()
    # Test v1's mat was 0.2 m high and read as moss; the reference's cover is a knee-high leafy
    # mound, so the dome is taller and the lobes rounder.
    lobes = [(Vector((0, 0, 0.08)), (0.5, 0.5, 0.3))]
    for k in range(rng.randint(2, 4)):
        a = rng.uniform(0, math.tau)
        d = rng.uniform(0.45, 0.7)
        r = rng.uniform(0.3, 0.4)
        lobes.append((Vector((math.cos(a) * d, math.sin(a) * d, 0.05)), (r, r, r * 0.65)))
    for c, radii in lobes:
        # spread 0.6 (Kanto's is 0.35): at this small leaf size the neater shingling read as
        # the scales of an artichoke; a looser turn per leaf reads as a leafy tangle.
        _foliage(m, c, radii, 0.14, rng, lobes, floor_z=0.0, lift=0.4, spread=0.6, cover=2.6)
    for k in range(rng.randint(3, 6)):
        c, radii = lobes[rng.randrange(len(lobes))]
        a = rng.uniform(0, math.tau)
        at = c + Vector((math.cos(a) * radii[0] * 0.6, math.sin(a) * radii[1] * 0.6, radii[2] * 0.5))
        length = rng.uniform(0.22, 0.36)
        spine = _arc(at, a, math.radians(rng.uniform(70, 84)), math.radians(rng.uniform(40, 60)), length, 4)
        _card(m, 2, spine, length * LEAF_TEX["blade"][1] * 1.1, -0.3, at - Vector((0, 0, 0.3)), up_bias=0.6)
    return m.build(name, [leaf_material("round", "groundcover", "light"),
                          leaf_material("round", "groundcover", "dark"),
                          leaf_material("blade", "grass", "light")])


# ---------------------------------------------------------------- placement

def _place(c, kind, mesh, x, y, z, scale, heading, slots):
    """One plant = one object linking a shared mesh. slots maps a material slot index to the
    material this plant wears there (the flower colour on a shared bush mesh)."""
    o = bpy.data.objects.new(kind, mesh)
    o.location = (x, y, z)
    o.rotation_euler = (0, 0, heading)
    o.scale = (scale, scale, scale)
    c.objects.link(o)
    for idx, mat in slots.items():
        slot = o.material_slots[idx]
        slot.link = "OBJECT"
        slot.material = mat
    return o


def _layout_draw(rng):
    """The previous kit drew a leaf green per plant here. The draw is kept, and discarded, so the
    layout rng stream (and so where every later plant in the cove stands) is unchanged by the
    move to leaf cards: the colour now varies per plant in the shader instead."""
    rng.choice((0, 1))


def palm(c, rng, x, y, z, height=None, lean=None):
    """A coconut palm, 6 to 11 m, trunk curving out toward heading `lean` (radians; None =
    random) in an arc, an S or nearly straight by variant, a heavily drooping crown of 7 to 9 broad
    frond cards over 2 young spears, and a nut cluster."""
    kit = _kit("palm", len(PALM_VARIANTS), _palm_mesh)
    if height is None:
        height = rng.uniform(6.0, 11.0)
    height = max(5.5, min(12.0, height))
    idx = min(range(len(PALM_VARIANTS)), key=lambda k: abs(PALM_VARIANTS[k][0] - height) + rng.uniform(0, 0.9))
    heading = rng.uniform(0, math.tau) if lean is None else lean
    o = _place(c, "palm", kit[idx], x, y, z, height / PALM_VARIANTS[idx][0], heading, {})
    _layout_draw(rng)
    return o


def tuft(c, rng, x, y, z, scale=1.0):
    """A grass tuft: 7 to 11 blade cards fanning out and arching, 0.6 to 1.2 m."""
    kit = _kit("tuft", 4, _tuft_mesh)
    o = _place(c, "tuft", kit[rng.randrange(len(kit))], x, y, z, scale * rng.uniform(0.85, 1.15),
               rng.uniform(0, math.tau), {})
    _layout_draw(rng)
    return o


def broadleaf(c, rng, x, y, z, scale=1.0):
    """A banana or taro plant: 4 to 6 bent paddle cards on stalks, 1.5 to 3 m."""
    kit = _kit("broadleaf", 4, _broadleaf_mesh)
    o = _place(c, "broadleaf", kit[rng.randrange(len(kit))], x, y, z, scale * rng.uniform(0.9, 1.3),
               rng.uniform(0, math.tau), {})
    _layout_draw(rng)
    return o


def flower_bush(c, rng, x, y, z, scale=1.0, flower=None):
    """A rounded low bush of shingled round-leaf cards, 1 to 1.8 m, flowering in gumamela or
    bougainvillea flower cards (by kit variant), crimson (mostly) or yellow.
    flower: 'plant_flower_red' or 'plant_flower_yellow'; None picks, red three times in four."""
    kit = _kit("bush", 4, _bush_mesh)
    if flower is None:
        flower = FLOWERS[0] if rng.random() < 0.75 else FLOWERS[1]
    mesh = kit[rng.randrange(len(kit))]
    s = scale * rng.uniform(0.9, 1.25)
    heading = rng.uniform(0, math.tau)
    _layout_draw(rng)
    colour = "yellow" if flower == FLOWERS[1] else "red"
    return _place(c, "flower bush", mesh, x, y, z, s, heading,
                  {2: flower_material(mesh.get("flower_kind", "gumamela"), colour)})


def croton(c, rng, x, y, z, scale=1.0):
    """A croton: a stiff clump of crimson-and-yellow leaves on short woody stems, 0.7 to 1 m."""
    kit = _kit("croton", 4, _croton_mesh)
    return _place(c, "croton", kit[rng.randrange(len(kit))], x, y, z, scale * rng.uniform(0.85, 1.2),
                  rng.uniform(0, math.tau), {})


def monstera(c, rng, x, y, z, scale=1.0):
    """A monstera: 3 to 6 big round split leaves on long stalks, about 1 to 1.4 m."""
    kit = _kit("monstera", 4, _monstera_mesh)
    return _place(c, "monstera", kit[rng.randrange(len(kit))], x, y, z, scale * rng.uniform(0.9, 1.25),
                  rng.uniform(0, math.tau), {})


def groundcover(c, rng, x, y, z, scale=1.0):
    """Ground cover: a low spreading mat of small round leaves with a few sprouts, about 2 m across
    and 0.3 m high."""
    kit = _kit("groundcover", 4, _groundcover_mesh)
    return _place(c, "groundcover", kit[rng.randrange(len(kit))], x, y, z, scale * rng.uniform(0.8, 1.3),
                  rng.uniform(0, math.tau), {})


# The gap mix, as weights (they need not sum to 1): tufts and ground cover the most, then broad
# leaves, some flowering bushes, the croton and monstera ACCENTS a few per cent each (the
# reference uses them as punctuation at boulder feet, not as a hedge), and the occasional palm
# leaning out of the seam.
# ⚠️ CHANGING THIS MIX CHANGES THE LAYOUT STREAM for any caller that uses the default: a different
# kind drawn at a spot draws a different number of random numbers, so every later plant moves.
# tools/author_lagoon_cove.py passes its OWN mix, so the cove's layout is unchanged until the lead
# adds the new kinds to it (which will, by the same argument, move the cove's later plants once).
GAP_MIX = (("tuft", 0.28), ("groundcover", 0.20), ("broadleaf", 0.22), ("flower_bush", 0.13),
           ("croton", 0.06), ("monstera", 0.05), ("palm", 0.06))

# How a gap plant of each kind is placed: its function, whether `scale` sizes it (palms are sized
# by height, not by the stones), and the footprint over which its base finds the lowest ground.
_GAP_KINDS = {
    "tuft": (lambda c, rng, x, y, z, s: tuft(c, rng, x, y, z, scale=s * rng.uniform(0.8, 1.2)), 0.5),
    "broadleaf": (lambda c, rng, x, y, z, s: broadleaf(c, rng, x, y, z, scale=s), 0.5),
    "flower_bush": (lambda c, rng, x, y, z, s: flower_bush(c, rng, x, y, z, scale=s), 0.5),
    "croton": (lambda c, rng, x, y, z, s: croton(c, rng, x, y, z, scale=s), 0.4),
    "monstera": (lambda c, rng, x, y, z, s: monstera(c, rng, x, y, z, scale=s), 0.5),
    "groundcover": (lambda c, rng, x, y, z, s: groundcover(c, rng, x, y, z, scale=s), 0.7),
}


def plant_gaps(c, rng, spots, height_fn, avoid_fn=None, density=0.55, per_spot=(1, 3), mix=GAP_MIX, ring=0.9,
               scale=1.0):
    """Plants in the gap at boulder feet. spots: (x, y, radius) footprints. A fraction `density`
    of spots gets per_spot[0] to per_spot[1] plants on a ring at about ring * radius; each point is
    rejected when avoid_fn(x, y) is True. Palms lean OUTWARD, away from the spot centre. Every
    base is set at the LOWEST ground within its footprint, so no plant floats on a slope.
    `scale` sizes the low plants (not palms) to the stones they sit among. `mix` may name any of
    tuft, groundcover, broadleaf, flower_bush, croton, monstera and palm. Returns how many plants
    were placed. The random draws for the old kinds are exactly as before, so a mix of only the
    old kinds lays out the same cove it always did."""
    total = sum(w for _k, w in mix)
    for k, _w in mix:
        if k != "palm" and k not in _GAP_KINDS:
            raise ValueError(f"plant_gaps: unknown plant kind {k!r}")
    placed = 0
    for sx, sy, r in spots:
        if rng.random() > density:
            continue
        for _ in range(rng.randint(*per_spot)):
            a = rng.uniform(0, math.tau)
            d = r * ring * rng.uniform(0.92, 1.08)
            x, y = sx + math.cos(a) * d, sy + math.sin(a) * d
            if avoid_fn is not None and avoid_fn(x, y):
                continue
            roll, kind = rng.uniform(0, total), mix[-1][0]
            for k, w in mix:
                if roll < w:
                    kind = k
                    break
                roll -= w
            foot = 0.9 if kind == "palm" else _GAP_KINDS[kind][1]
            z = min(height_fn(x + dx * foot, y + dy * foot) for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)))
            if kind == "palm":
                palm(c, rng, x, y, z, lean=a + rng.uniform(-0.35, 0.35))
            else:
                _GAP_KINDS[kind][0](c, rng, x, y, z, scale)
            placed += 1
    return placed
