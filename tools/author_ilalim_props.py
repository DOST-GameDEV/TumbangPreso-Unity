"""Model the gameplay street props on the Ilalim ng Tulay pavements (ILALIM-1.3, prop kit).

  py -3 tools/author_ilalim_textures_props.py [--sheet N]     # paint the prop_ textures first
  blender -b --python tools/author_ilalim_props.py -- [--preview N]

Writes ArtSource/ilalim/props.blend. With --preview it also writes versioned renders to
Logs/ilalim-blender/prop_<shot>_vN.png (an existing file is never overwritten).

THE BRIEF (Ilalim_Ng_Tulay.md section 4, 8.4, 10.2, 10.6; ILALIM_REWORK_GUIDE.md section 1): the
props that carry the map's gameplay, on the two 4 m pavements (x 7..11 each side, top 0.212),
always against the shopfront or the PGH fence, never mid-pavement, and NOTHING inside the chalk
box (|x|, |y| < 7). Blender X is the game's x (east), Blender Y is the game's z (north).

THE HOUSE STYLE (KANTO_DESIGN_GUIDE.md section 2, LAGOON_REWORK_GUIDE.md section 2), as in the
LRT kit (tools/author_ilalim_lrt.py, whose fillet() and rounded_rect() this script imports):
  * real, editable models, one object per prop part, each with a live Bevel modifier (hardened
    normals);
  * CHUNKY AND ORGANIC, NEVER FIDDLY: thick rounded members, small lean and size jitter. No nets,
    no thin spokes, no slats: that detail is painted (tools/author_ilalim_textures_props.py);
  * NO TWO SURFACES SHARE A PLANE: every foot sinks 1 cm into the pavement, attached parts
    penetrate 0.5 to 2 cm, sign panels are recessed or proud of their frames;
  * world-scale UVs (2 m per tile for these small things), plus two grime UV maps: UVGrime
    (v = metres below the prop's top edge, for prop_grime_top) and UVSplash (v = metres above
    the pavement, for prop_grime_foot). Signs carry their artwork on a 0..1 UV of one face.

ROLE HUES (Art_Direction.md section 1): nothing near offence orange #f87020 or defence blue
#0080e8. The barangay tarpaulin is DEEP NAVY; the pad's bars are plum, mint glow and gold; no
blue tarp, blue drum or orange kwek-kwek anywhere.

THE KIT. Each prop is its own collection under "props (placed)", and each object's ORIGIN is its
contract anchor on the pavement top, so the Unity builder can place and collide it by origin.
Footprints are x by y (metres, world axes) and height above the pavement top:

  prop_pisonet (EAST, against the shopfront at x 11)
    prop_pisonet_terminal_1..3   origins (9.70, 10.00 / 11.75 / 13.50). Plywood-and-laminate
                                 coin-op cabinets after the real ones: a hood over a recessed
                                 screen, a keyboard drawer, a locked coin box, aluminium edge
                                 trims. Footprint 0.72 x 0.84, height 1.86, front faces -x.
                                 The screens are material prop_screen (emissive in Unity).
    prop_pisonet_chair_1..3      monobloc chairs at (8.95, y), facing the cabinets. 0.56 sq, 0.87.
    prop_pisonet_awning          an olive tarpaulin on three steel rafters bolted to the wall,
                                 x 8.50..11.0, y 8.85..14.65, front edge 2.40 up, back 2.88.
                                 It has NO posts, so the pavement stays clear.
    prop_pisonet_rate_board      the hand-painted PISONET / P1 = 5 MIN board standing on the
                                 awning's front rail at (8.54, 11.75), 1.3 x 0.5, 2.48..2.98 up.
    prop_pisonet_cord            THE TRIP HAZARD: two taped leads and a power strip lying flat,
                                 origin (8.40, 11.30), extent x 8.20..9.40, y 10.30..13.20,
                                 UNDER 27 mm tall everywhere (the strip and its sockets top out
                                 at 26.5 mm; the leads 22 mm, the tape bands 25 mm).
  prop_pares_cart (EAST)
    prop_pares_cart              origin (8.80, -5.00): a jade-green steel cart with a stainless
                                 counter, two big pots, a glass case, bicycle-style wheels at
                                 y -4.45 and a maroon tarp roof on four posts. Body footprint
                                 x 7.95..9.55 (roof) / 8.10..9.49 (counter and wheels),
                                 y -6.27..-3.90, roof top 2.50. The painted side panel reads
                                 PARES NI MANG BOYET / MAMI · LUGAW · GOTO.
    prop_pares_cart_glass        the glass case panes (a transparent material, own object).
    prop_pares_lpg               the LPG tank on the pavement at (9.78, -5.55), r 0.16, 0.62 tall,
                                 its hose running into the cart.
    prop_pares_stool_1..2        round plastic stools at (7.90, -5.55) and (7.92, -4.70), 0.42 dia.
    prop_pares_aboard            the chalk A-board PARES / MAMI at (8.05, -3.75), faces -x and +x,
                                 0.52 x 0.66 footprint, 1.03 tall.
  prop_overclock_pad (EAST)      origin (9.00, 5.50): a 1.8 m square charcoal plate, 28 mm total
                                 with 10 mm sunk, so its top stands 18 mm proud and its three
                                 light bars 26 mm. Bars run along y, 0.90 long, at x -0.46, 0,
                                 +0.46: plum (prop_bar_plum), mint glow (prop_bar_glow), gold
                                 (prop_bar_gold), all emissive.
  prop_bridge_hoop (WEST)        origin (-8.90, -10.00): the RING CENTRE is at local (0, 0, 3.07)
                                 (BridgeHoop.RingCentre), tube radius 0.25 about it, facing the
                                 street (+x). Yellow steel post at x -9.62 set in a concrete-filled
                                 tyre (r 0.40), backboard 1.2 x 0.8 face at x -9.33 painted
                                 BRGY. 671, "Handog ni Kag. Ruben Dela Paz". No net: street rims
                                 on Taft lose theirs, and a net is exactly the fiddly strand the
                                 owner rejects. Footprint x -10.02..-8.63, y -10.60..-9.40.
  WEST vendor stalls against the PGH fence (x -11):
    prop_stall_fruit             (-10.35, 12.50): a plank table with two tilted crates of mangoes
                                 and bananas, pomelos and a market scale, under a red and cream
                                 umbrella (R 1.15). Table 1.0 x 1.5, 0.80 high.
    prop_stall_fishball          (-10.35, 14.60): a pale-yellow push cart with a wok of fishballs
                                 and kikiam, three sauce jars, a skewer cup, a small LPG tank, and
                                 a yellow and green umbrella (R 1.0). Body 0.95 x 1.30.
    prop_stall_sarisari          (-10.35, -14.20): a stepped plank stand of candy jars, hanging
                                 sachet strips, a styrofoam ice box and a cardboard MAY LOAD /
                                 YELO sign, under a four-colour umbrella (R 1.2).
  prop_clutter: prop_crate_stack_w (-10.55, 11.05), prop_crate_stack_e (10.45, -6.80),
    prop_water_drum (-10.55, 15.95, r 0.30, 0.92 tall, a cream drum with a tabo on the lid),
    prop_bench (-10.62, 6.20, 0.34 x 1.6, 0.46 high), prop_trash_bin (-10.50, -12.30, r 0.27,
    0.84 tall, green), prop_chair_w1 (-10.25, 13.62) and prop_chair_w2 (-10.35, -15.75).
  prop_column_signs, built at the ORIGIN (hidden from renders) for the Unity builder to place.
  Each has its BACK at local y = +0.008 and its face toward local -Y, so it sits on a column face
  with 8 mm sunk in. Rotate about Z so local -Y points away from the column:
    prop_bawal_umihi_a           red ENAMEL plate 0.80 x 0.52. Intended for the WEST live pier
                                 leg (-4.45, -10), inner face x = -3.74, centre 1.30 up, rot_z +90.
    prop_bawal_umihi_b           red brush-painted plank 0.90 x 0.45, hung 2 degrees crooked.
                                 Intended for the EAST live pier leg (4.45, 10), inner face
                                 x = +3.74, centre 1.30 up, rot_z -90.
    prop_barangay_tarp           deep-navy PAALALA tarpaulin 1.40 x 0.82 with rope ties round the
                                 column corners. Intended for the west leg of the y = +19 pier,
                                 inner face, centre 1.75 up, rot_z +90.
  The review scene places linked duplicates of the three there ("review placement"), computing
  each leg's true face from the pier's own lean (author_ilalim_lrt.pier, seed 1).

REVIEW CONTEXT (not part of the kit): the collection "review stand-ins" holds a plain road,
kerbs, pavements, a stand-in shopfront wall at x 11, a stand-in PGH iron fence at x -11 and a
1.7 m figure; "LRT (linked)" instances the guideway from ArtSource/ilalim/lrt_kit.blend.
"""
import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import author_ilalim_lrt as L                            # noqa: E402  (read-only: helpers, lighting)
from author_ilalim_lrt import fillet, rounded_rect      # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource" / "ilalim"
TEXTURES = SOURCE / "textures"
PREVIEWS = ROOT / "Logs" / "ilalim-blender"
TILE_M = 2.0
PT = 0.212                     # pavement top
UP = Vector((0, 0, 1))

# The pisonet atlas regions, in image pixels, top-left origin (same numbers as the texture script).
ATLAS = {
    "screen_1": (0, 0, 512, 384), "screen_2": (512, 0, 1024, 384), "screen_3": (0, 384, 512, 768),
    "keyboard": (512, 384, 1024, 576),
    "num_1": (512, 576, 682, 746), "num_2": (682, 576, 852, 746), "num_3": (852, 576, 1022, 746),
}


def atlas_uv(key, size=1024):
    x0, y0, x1, y1 = ATLAS[key]
    return (x0 / size, 1 - y1 / size, x1 / size, 1 - y0 / size)


# ------------------------------------------------------------------ materials
# name: (texture file or None, tint or colour, roughness, metallic, emission strength, grime)
M = {
    "prop_laminate":     ("prop_laminate", None, 0.55, 0.0, 0, True),
    "prop_lime":         ("prop_plastic", (0.56, 0.70, 0.30), 0.5, 0.0, 0, True),
    "prop_alu":          ("prop_stainless", (1.0, 1.0, 1.0), 0.4, 0.6, 0, False),
    "prop_stainless":    ("prop_stainless", None, 0.42, 0.5, 0, True),
    "prop_steel_dark":   ("prop_paint", (0.29, 0.29, 0.28), 0.6, 0.2, 0, True),
    "prop_steel_jade":   ("prop_paint", (0.36, 0.55, 0.46), 0.55, 0.1, 0, True),
    "prop_steel_jade_lt": ("prop_paint", (0.45, 0.63, 0.53), 0.55, 0.1, 0, True),
    "prop_steel_yellow": ("prop_paint", (0.84, 0.68, 0.25), 0.55, 0.1, 0, True),
    "prop_steel_red":    ("prop_paint", (0.64, 0.17, 0.15), 0.5, 0.1, 0, True),
    "prop_lpg_red":      ("prop_paint", (0.55, 0.15, 0.17), 0.5, 0.1, 0, True),
    "prop_cart_cream":   ("prop_paint", (0.88, 0.80, 0.52), 0.55, 0.1, 0, True),
    "prop_tarp_olive":   ("prop_tarp", (0.47, 0.50, 0.32), 0.8, 0.0, 0, True),
    "prop_tarp_maroon":  ("prop_tarp", (0.50, 0.17, 0.21), 0.8, 0.0, 0, True),
    "prop_canvas_red":   ("prop_canvas", (0.66, 0.18, 0.17), 0.85, 0.0, 0, False),
    "prop_canvas_cream": ("prop_canvas", (0.93, 0.90, 0.82), 0.85, 0.0, 0, False),
    "prop_canvas_green": ("prop_canvas", (0.25, 0.50, 0.32), 0.85, 0.0, 0, False),
    "prop_canvas_yellow": ("prop_canvas", (0.88, 0.74, 0.28), 0.85, 0.0, 0, False),
    "prop_canvas_maroon": ("prop_canvas", (0.45, 0.14, 0.20), 0.85, 0.0, 0, False),
    "prop_plastic_white": ("prop_plastic", (0.84, 0.83, 0.79), 0.5, 0.0, 0, True),
    "prop_plastic_red":  ("prop_plastic", (0.68, 0.17, 0.16), 0.45, 0.0, 0, True),
    "prop_plastic_green": ("prop_plastic", (0.28, 0.52, 0.34), 0.45, 0.0, 0, True),
    "prop_plastic_maroon": ("prop_plastic", (0.46, 0.14, 0.17), 0.45, 0.0, 0, True),
    "prop_plastic_yellow": ("prop_plastic", (0.85, 0.70, 0.22), 0.45, 0.0, 0, True),
    "prop_plastic_cream": ("prop_plastic", (0.88, 0.85, 0.75), 0.45, 0.0, 0, True),
    "prop_styro":        ("prop_plastic", (0.96, 0.95, 0.92), 0.9, 0.0, 0, True),
    "prop_wood":         ("prop_wood", None, 0.8, 0.0, 0, True),
    "prop_wood_dark":    ("prop_wood", (0.72, 0.66, 0.60), 0.8, 0.0, 0, True),
    "prop_rubber":       ("prop_rubber", None, 0.9, 0.0, 0, True),
    "prop_concrete":     ("prop_concrete", None, 0.9, 0.0, 0, True),
    "prop_glass":        (None, (0.78, 0.86, 0.84), 0.08, 0.0, 0, False),
    "prop_bezel":        (None, (0.11, 0.11, 0.12), 0.5, 0.0, 0, False),
    "prop_slot":         (None, (0.05, 0.05, 0.05), 0.6, 0.0, 0, False),
    "prop_cable":        ("prop_rubber", (0.55, 0.55, 0.55), 0.7, 0.0, 0, False),
    "prop_tape":         (None, (0.58, 0.58, 0.55), 0.7, 0.0, 0, False),
    "prop_cord_yellow":  ("prop_plastic", (0.84, 0.70, 0.22), 0.5, 0.0, 0, False),
    "prop_rope":         (None, (0.70, 0.62, 0.47), 0.9, 0.0, 0, False),
    "prop_lamp":         (None, (1.0, 0.90, 0.72), 0.3, 0.0, 3.0, False),
    "prop_bar_plum":     (None, (0.55, 0.28, 0.62), 0.3, 0.0, 0.55, False),
    "prop_bar_glow":     (None, (0.12, 0.68, 0.53), 0.3, 0.0, 0.55, False),
    "prop_bar_gold":     (None, (0.75, 0.58, 0.20), 0.3, 0.0, 0.55, False),
    "prop_pad_rim":      (None, (0.16, 0.16, 0.18), 0.7, 0.2, 0, False),
    "prop_food_broth":   (None, (0.30, 0.18, 0.11), 0.25, 0.0, 0, False),
    "prop_food_oil":     (None, (0.62, 0.45, 0.20), 0.2, 0.0, 0, False),
    "prop_food_fishball": (None, (0.86, 0.74, 0.55), 0.6, 0.0, 0, False),
    "prop_food_kikiam":  (None, (0.47, 0.29, 0.18), 0.6, 0.0, 0, False),
    "prop_food_mango":   (None, (0.93, 0.76, 0.24), 0.5, 0.0, 0, False),
    "prop_food_mango_g": (None, (0.66, 0.72, 0.28), 0.5, 0.0, 0, False),
    "prop_food_banana":  (None, (0.92, 0.82, 0.32), 0.5, 0.0, 0, False),
    "prop_food_pomelo":  (None, (0.56, 0.70, 0.34), 0.5, 0.0, 0, False),
    "prop_bowl":         (None, (0.93, 0.92, 0.88), 0.4, 0.0, 0, False),
    "prop_sauce_sweet":  (None, (0.46, 0.25, 0.12), 0.35, 0.0, 0, False),
    "prop_sauce_spicy":  (None, (0.60, 0.16, 0.12), 0.35, 0.0, 0, False),
    "prop_sauce_vinegar": (None, (0.80, 0.75, 0.56), 0.3, 0.0, 0, False),
    "prop_candy_a":      (None, (0.62, 0.30, 0.52), 0.4, 0.0, 0, False),
    "prop_candy_b":      (None, (0.90, 0.80, 0.42), 0.4, 0.0, 0, False),
    "prop_candy_c":      (None, (0.50, 0.70, 0.44), 0.4, 0.0, 0, False),
    "prop_candy_d":      (None, (0.86, 0.46, 0.46), 0.4, 0.0, 0, False),
    "prop_sticks":       (None, (0.80, 0.70, 0.52), 0.8, 0.0, 0, False),
    # One-off artwork on 0..1 UVs.
    "prop_sign_pisonet":   ("prop_sign_pisonet", None, 0.7, 0.0, 0, False),
    "prop_screen":         ("prop_pisonet_atlas", None, 0.3, 0.0, 1.4, False),
    "prop_keyboard":       ("prop_pisonet_atlas", None, 0.6, 0.0, 0, False),
    "prop_sticker":        ("prop_pisonet_atlas", None, 0.6, 0.0, 0, False),
    "prop_sign_cart":      ("prop_sign_cart", None, 0.6, 0.0, 0, False),
    "prop_sign_fishball":  ("prop_sign_fishball", None, 0.6, 0.0, 0, False),
    "prop_sign_aboard":    ("prop_sign_aboard", None, 0.9, 0.0, 0, False),
    "prop_sign_backboard": ("prop_sign_backboard", None, 0.7, 0.0, 0, False),
    "prop_sign_bawal_a":   ("prop_sign_bawal_a", None, 0.35, 0.0, 0, False),
    "prop_sign_bawal_b":   ("prop_sign_bawal_b", None, 0.8, 0.0, 0, False),
    "prop_sign_tarp":      ("prop_sign_tarp", None, 0.7, 0.0, 0, False),
    "prop_sign_sarisari":  ("prop_sign_sarisari", None, 0.9, 0.0, 0, False),
    "prop_sachet_strip":   ("prop_sachet_strip", None, 0.5, 0.0, 0, False),
    "prop_pad_plate":      ("prop_pad_plate", None, 0.7, 0.1, 0, False),
}


def image(name, colour=True):
    img = bpy.data.images.load(str(TEXTURES / f"{name}.png"), check_existing=True)
    if not colour:
        img.colorspace_settings.name = "Non-Color"
    return img


def material(name):
    m = bpy.data.materials.get(name)
    if m:
        return m
    tex, tint, rough, metal, emit, grime = M[name]
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
        if emit:
            bsdf.inputs["Emission Color"].default_value = (*tint, 1)
            bsdf.inputs["Emission Strength"].default_value = emit
        if name == "prop_glass":
            bsdf.inputs["Alpha"].default_value = 0.28
            if hasattr(m, "surface_render_method"):
                m.surface_render_method = "BLENDED"
        return m
    uv = nodes.new("ShaderNodeUVMap")
    uv.uv_map = "UVMap"
    albedo = nodes.new("ShaderNodeTexImage")
    albedo.image = image(tex)
    links.new(uv.outputs["UV"], albedo.inputs["Vector"])
    colour = albedo.outputs["Color"]
    if tint is not None:
        mix = nodes.new("ShaderNodeMix")
        mix.data_type, mix.blend_type = "RGBA", "MULTIPLY"
        mix.inputs["Factor"].default_value = 1.0
        links.new(colour, mix.inputs[6])
        mix.inputs[7].default_value = (*tint, 1)
        colour = mix.outputs[2]
    if grime:
        for img_name, layer in (("prop_grime_top", "UVGrime"), ("prop_grime_foot", "UVSplash")):
            guv = nodes.new("ShaderNodeUVMap")
            guv.uv_map = layer
            gtex = nodes.new("ShaderNodeTexImage")
            gtex.image = image(img_name, colour=False)
            gtex.extension = "EXTEND"
            links.new(guv.outputs["UV"], gtex.inputs["Vector"])
            mul = nodes.new("ShaderNodeMix")
            mul.data_type, mul.blend_type = "RGBA", "MULTIPLY"
            mul.inputs["Factor"].default_value = 1.0
            links.new(colour, mul.inputs[6])
            links.new(gtex.outputs["Color"], mul.inputs[7])
            colour = mul.outputs[2]
    links.new(colour, bsdf.inputs["Base Color"])
    if emit:
        links.new(colour, bsdf.inputs["Emission Color"])
        bsdf.inputs["Emission Strength"].default_value = emit
    avg = (0.6, 0.6, 0.6) if tint is None else tuple(0.85 * c for c in tint)
    m.diffuse_color = (*avg, 1)
    return m


# ------------------------------------------------------------------ geometry

def frame(right, up, normal, at):
    """A 4x4 matrix whose local x, y, z are `right`, `up`, `normal`, placed at `at`."""
    m = Matrix((right, up, normal)).transposed().to_4x4()
    m.translation = Vector(at)
    return m


def rz(deg):
    return Matrix.Rotation(math.radians(deg), 4, "Z")


def facing(direction, at, tilt_deg=0.0):
    """A panel frame whose local +z (its face) looks along horizontal `direction` ('+x', '-x',
    '+y', '-y' or 'up'), local +y is up, tilted back by `tilt_deg`."""
    d = {"+x": Vector((1, 0, 0)), "-x": Vector((-1, 0, 0)), "+y": Vector((0, 1, 0)), "-y": Vector((0, -1, 0))}
    if direction == "up":
        return frame(Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1)), at)
    n = d[direction]
    t = math.radians(tilt_deg)
    normal = (n * math.cos(t) + UP * math.sin(t)).normalized()
    up = (UP * math.cos(t) - n * math.sin(t)).normalized()
    right = up.cross(normal)
    return frame(right, up, normal, at)


class PBuf(L.Buf):
    """The LRT kit's Buf with this kit's needs: a current transform `M` applied to everything,
    decal faces with their own UVs, 2 m world UVs, prop grime maps and its own materials."""

    def __init__(self, name, top=None):
        super().__init__(name)
        self.top = top
        self.M = Matrix()
        # Decal faces get their UVs written at once and are flagged, so world_uvs() skips them.
        self.uv = self.bm.loops.layers.uv.new("UVMap")
        self.flag = self.bm.faces.layers.int.new("decal")

    def loft(self, rings, mat, cap=True, mat_of=None):
        rings = [[self.M @ Vector(v) for v in ring] for ring in rings]
        return super().loft(rings, mat, cap, mat_of)

    def blob(self, center, radii, mat, rot=None):
        mtx = self.M @ Matrix.Translation(center) @ (rot or Matrix()) @ Matrix.Diagonal((*radii, 1))
        r = bmesh.ops.create_icosphere(self.bm, subdivisions=2, radius=1.0, matrix=mtx)
        idx = self.mi(mat)
        for f in {f for v in r["verts"] for f in v.link_faces}:
            f.material_index = idx
            f.smooth = True

    # -- primitives, all in the current local frame -------------------------------------------

    def rbox(self, center, size, mat, r=0.02, rot=None, top_scale=1.0, mat_of=None):
        sx, sy, sz = size
        r = max(0.004, min(r, sx * 0.45, sy * 0.45))
        prof = rounded_rect(sx / 2, sy / 2, r)
        mtx = Matrix.Translation(center) @ (rot or Matrix())
        bottom = [mtx @ Vector((x, y, -sz / 2)) for x, y in prof]
        top = [mtx @ Vector((x * top_scale, y * top_scale, sz / 2)) for x, y in prof]
        return self.loft([bottom, top], mat, mat_of=mat_of)

    def prism(self, p0, p1, hw0, hw1, mat, r=0.02):
        """A rounded square member from p0 to p1 (a leg, a post), `hw` half widths at each end."""
        p0, p1 = Vector(p0), Vector(p1)
        d = (p1 - p0).normalized()
        side = d.cross(Vector((0, 1, 0)) if abs(d.y) < 0.9 else Vector((1, 0, 0))).normalized()
        up = side.cross(d).normalized()
        rings = []
        for p, hw in ((p0, hw0), (p1, hw1)):
            rings.append([p + side * x + up * y for x, y in rounded_rect(hw, hw, min(r, hw * 0.45))])
        return self.loft(rings, mat)

    def lathe(self, center, prof, mat, sides=16, mat_of=None, squash=(1.0, 1.0)):
        """Revolve a list of (radius, z) about the vertical through `center`."""
        c = Vector(center)
        rings = [[c + Vector((r * math.cos(a) * squash[0], r * math.sin(a) * squash[1], z))
                  for a in (k / sides * math.tau for k in range(sides))] for r, z in prof]
        return self.loft(rings, mat, mat_of=mat_of)

    def torus(self, center, R, r, mat, axis="z", seg=24, sides=10):
        c = Vector(center)
        basis = {"z": (Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))),
                 "x": (Vector((0, 1, 0)), Vector((0, 0, 1)), Vector((1, 0, 0))),
                 "y": (Vector((1, 0, 0)), Vector((0, 0, 1)), Vector((0, 1, 0)))}[axis]
        e1, e2, n = basis
        verts = []
        for i in range(seg):
            a = i / seg * math.tau
            radial = e1 * math.cos(a) + e2 * math.sin(a)
            ring = []
            for j in range(sides):
                b = j / sides * math.tau
                p = c + radial * (R + r * math.cos(b)) + n * (r * math.sin(b))
                ring.append(self.bm.verts.new(self.M @ p))
            verts.append(ring)
        faces = []
        for i in range(seg):
            for j in range(sides):
                a, b = verts[i], verts[(i + 1) % seg]
                faces.append(self.bm.faces.new((a[j], b[j], b[(j + 1) % sides], a[(j + 1) % sides])))
        bmesh.ops.recalc_face_normals(self.bm, faces=faces)
        idx = self.mi(mat)
        for f in faces:
            f.material_index = idx
            f.smooth = True
        return faces

    def panel(self, mtx, w, h, t, mat, decal, region=(0.0, 0.0, 1.0, 1.0), r=0.02, jitter=0.0, seed=0):
        """A board of w x h x t in the frame `mtx` (face = local +z), its front face carrying the
        artwork material `decal` on UV `region`. `jitter` wobbles the outline (a hand-cut plank)."""
        prof = rounded_rect(w / 2, h / 2, r)
        if jitter:
            rng = random.Random(seed)
            prof = [(x + rng.uniform(-jitter, jitter), y + rng.uniform(-jitter, jitter)) for x, y in prof]
        saved = self.M
        self.M = saved @ mtx
        faces = self.loft([[Vector((x, y, -t / 2)) for x, y in prof], [Vector((x, y, t / 2)) for x, y in prof]], mat)
        self.M = saved
        front = max(faces, key=lambda f: f.normal.dot((saved @ mtx).to_3x3() @ Vector((0, 0, 1))))
        front.material_index = self.mi(decal)
        inv = (saved @ mtx).inverted()
        u0, v0, u1, v1 = region
        uvs = {}
        for v in front.verts:
            lx, ly, _ = inv @ v.co
            uvs[v] = (u0 + (lx / w + 0.5) * (u1 - u0), v0 + (ly / h + 0.5) * (v1 - v0))
        self.set_uvs(front, uvs)
        return front

    def sheet(self, grid, thick, mat_fn, normal=UP, wrap=False, uv_fn=None, decal=None):
        """A cloth or tarp: a grid of points (rows of columns) given a thickness along -`normal`,
        closed round its border. `mat_fn(i, j)` names each cell's material; `uv_fn(i, j)` gives
        artwork UVs for the upper face, whose cells then take `decal`."""
        rows, cols = len(grid), len(grid[0])
        top = [[self.bm.verts.new(self.M @ Vector(p)) for p in row] for row in grid]
        n = self.M.to_3x3() @ Vector(normal)
        bot = [[self.bm.verts.new(v.co - n * thick) for v in row] for row in top]
        faces = []
        jmax = cols if wrap else cols - 1
        for i in range(rows - 1):
            for j in range(jmax):
                j2 = (j + 1) % cols
                ft = self.bm.faces.new((top[i][j], top[i][j2], top[i + 1][j2], top[i + 1][j]))
                fb = self.bm.faces.new((bot[i + 1][j], bot[i + 1][j2], bot[i][j2], bot[i][j]))
                name = mat_fn(i, j)
                ft.material_index = self.mi(decal if decal else name)
                fb.material_index = self.mi(name)
                faces += [ft, fb]
                if uv_fn:
                    self.set_uvs(ft, {top[a][b]: uv_fn(a, b) for a, b in ((i, j), (i, j2), (i + 1, j2), (i + 1, j))})
        edges = []
        for i in (0, rows - 1):
            edges += [(top[i][j], top[i][(j + 1) % cols], bot[i][(j + 1) % cols], bot[i][j]) for j in range(jmax)]
        if not wrap:
            for j in (0, cols - 1):
                edges += [(top[i][j], top[i + 1][j], bot[i + 1][j], bot[i][j]) for i in range(rows - 1)]
        for e in edges:
            f = self.bm.faces.new(e)
            f.material_index = self.mi(mat_fn(0, 0))
            faces.append(f)
        bmesh.ops.recalc_face_normals(self.bm, faces=faces)
        return faces

    def set_uvs(self, face, uvs):
        face[self.flag] = 1
        for l in face.loops:
            l[self.uv].uv = uvs[l.vert]

    # -- UVs and output ------------------------------------------------------------------------

    def world_uvs(self):
        uv = self.bm.loops.layers.uv.verify()
        for f in self.bm.faces:
            if f[self.flag]:
                continue
            n = f.normal
            if abs(n.z) > 0.7:
                for l in f.loops:
                    l[uv].uv = (l.vert.co.x / TILE_M, l.vert.co.y / TILE_M)
                continue
            t = UP.cross(n)
            t = t.normalized() if t.length > 1e-6 else Vector((1, 0, 0))
            for l in f.loops:
                l[uv].uv = (l.vert.co.dot(t) / TILE_M, l.vert.co.z / TILE_M)
        drip = self.bm.loops.layers.uv.new("UVGrime")
        splash = self.bm.loops.layers.uv.new("UVSplash")
        clean = 0.999
        for f in self.bm.faces:
            n = f.normal
            side = abs(n.z) <= 0.7
            t = UP.cross(n)
            t = t.normalized() if t.length > 1e-6 else Vector((1, 0, 0))
            for l in f.loops:
                co = l.vert.co
                if side and self.top is not None:
                    l[drip].uv = (co.dot(t) / 2.0, min(clean, max(0.0, (self.top - co.z) / 1.0)))
                else:
                    l[drip].uv = (co.x / 2.0, clean)
                if side:
                    l[splash].uv = (co.dot(t) / 2.0, min(clean, max(0.0, (co.z - PT) / 1.0)))
                else:
                    l[splash].uv = (co.x / 2.0, clean if n.z > 0 else min(clean, max(0.0, (co.z - PT))))

    def finish(self, collection, origin=(0, 0, 0), bevel=0.012, segments=2, smooth=True):
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


def collection(name, parent=None):
    c = bpy.data.collections.new(name)
    (parent or bpy.context.scene.collection).children.link(c)
    return c


def catmull(points, per=6):
    """A smooth path through `points` (Catmull-Rom), for cords and ropes."""
    pts = [Vector(p) for p in points]
    ext = [pts[0] * 2 - pts[1]] + pts + [pts[-1] * 2 - pts[-2]]
    out = []
    for i in range(1, len(ext) - 2):
        p0, p1, p2, p3 = ext[i - 1], ext[i], ext[i + 1], ext[i + 2]
        for k in range(per):
            t = k / per
            out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                              + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    out.append(pts[-1])
    return out


# ------------------------------------------------------------------ shared small props

def monobloc(b, pos, yaw, mat):
    """The Filipino monobloc: a dished seat, four splayed channel legs, a curved back and fat arms.
    Local +x is where the sitter faces."""
    saved = b.M
    b.M = saved @ Matrix.Translation(pos) @ rz(yaw)
    b.extrude_z(rounded_rect(0.235, 0.235, 0.09), 0.40, 0.447, mat, top_scale=1.02)
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.prism((sx * 0.25, sy * 0.25, -0.01), (sx * 0.19, sy * 0.19, 0.43), 0.042, 0.05, mat, r=0.02)
    arc = []
    for k in range(9):
        a = math.radians(-40 + 80 * k / 8)
        arc.append((0.13 - 0.37 * math.cos(a), 0.37 * math.sin(a)))
    inner = [(x + 0.034 * math.cos(math.radians(-40 + 80 * k / 8)), y - 0.034 * math.sin(math.radians(-40 + 80 * k / 8)))
             for k, (x, y) in enumerate(arc)]
    b.extrude_z(arc + inner[::-1], 0.42, 0.87, mat, lean=(-0.09, 0.0))
    for sy in (-1, 1):
        b.tube([Vector((-0.2, sy * 0.235, 0.66)), Vector((0.02, sy * 0.255, 0.645)), Vector((0.19, sy * 0.245, 0.59)),
                Vector((0.2, sy * 0.21, 0.43))], 0.026, mat, sides=8)
    b.M = saved


def plastic_stool(b, pos, mat):
    saved = b.M
    b.M = saved @ Matrix.Translation(pos)
    b.lathe((0, 0, 0), [(0.19, 0.41), (0.2, 0.425), (0.195, 0.445), (0.15, 0.452)], mat, sides=20)
    for k in range(4):
        a = math.radians(45 + 90 * k)
        c, s = math.cos(a), math.sin(a)
        b.prism((0.19 * c, 0.19 * s, -0.01), (0.13 * c, 0.13 * s, 0.42), 0.05, 0.045, mat, r=0.02)
    b.M = saved


def crate(b, center, size, mat, yaw=0.0):
    """An open-topped plastic crate: floor and four chunky walls, with a hand hole block on the
    long sides drawn darker by being recessed (a dark block sunk into the wall)."""
    sx, sy, sz = size
    saved = b.M
    b.M = saved @ Matrix.Translation(center) @ rz(yaw)
    w = 0.025
    b.rbox((0, 0, w / 2), (sx, sy, w), mat, r=0.03)
    for s in (-1, 1):
        b.rbox((s * (sx / 2 - w / 2), 0, sz / 2), (w, sy, sz), mat, r=0.01)
        b.rbox((0, s * (sy / 2 - w / 2), sz / 2), (sx - 0.01, w, sz), mat, r=0.01)
        b.rbox((0, s * (sy / 2 - w * 0.3), sz * 0.78), (sx * 0.35, w * 0.8, sz * 0.14), "prop_slot", r=0.02)
    b.M = saved


def umbrella(b, pole_base, top_z, radius, mats, tilt=(0.0, 0.0), panels=8, weight=True):
    """A market umbrella: a fat pole, a finial, and a striped canopy of `panels` panels, each sagging
    between its ribs, tilted by `tilt` (degrees about y, about x)."""
    base = Vector(pole_base)
    apex = Vector((base.x, base.y, top_z))
    rot = Matrix.Rotation(math.radians(tilt[0]), 4, "Y") @ Matrix.Rotation(math.radians(tilt[1]), 4, "X")
    tip = apex + (rot.to_3x3() @ Vector((0, 0, 0.0)))
    b.tube([base + Vector((0, 0, -0.01)), apex + Vector((0, 0, 0.05))], 0.024, "prop_steel_dark", sides=10)
    if weight:
        b.lathe(base, [(0.14, -0.01), (0.15, 0.1), (0.13, 0.2), (0.05, 0.21)], "prop_concrete", sides=16)
    rings, seg = 7, panels * 4
    grid = []
    for i in range(rings):
        rr = 0.05 + (radius - 0.05) * i / (rings - 1)
        row = []
        for j in range(seg):
            a = j / seg * math.tau
            frac = (j % 4) / 4
            sag = 0.05 * math.sin(math.pi * frac) * (rr / radius)
            z = 0.02 - 0.42 * (rr / radius) ** 1.4 - sag
            p = rot.to_3x3() @ Vector((rr * math.cos(a), rr * math.sin(a), z))
            row.append(apex + p)
        grid.append(row)
    b.sheet(grid, 0.014, lambda i, j: mats[(j // 4) % len(mats)], wrap=True)
    b.blob(apex + Vector((0, 0, 0.05)), (0.05, 0.05, 0.06), "prop_steel_dark")
    b.blob(apex + Vector((0, 0, -0.1)), (0.045, 0.045, 0.07), "prop_steel_dark")


# ------------------------------------------------------------------ the pisonet

def pisonet_terminal(col, x, y, k):
    b = PBuf(f"prop_pisonet_terminal_{k}", top=PT + 1.84)
    b.M = Matrix.Translation((x, y, PT))
    b.rbox((0, 0, 0.04), (0.66, 0.84, 0.10), "prop_laminate", r=0.03)
    body = [(-0.28, 0.07), (0.30, 0.07), (0.30, 1.78), (-0.02, 1.80), (-0.29, 1.73), (-0.29, 1.60), (0.10, 1.56),
            (0.05, 0.99), (-0.25, 0.97), (-0.25, 0.87), (-0.28, 0.85)]
    b.extrude_y(fillet(body, 0.025, 2), -0.37, 0.37, "prop_laminate")
    cheek = fillet([(-0.33, 0.05), (0.33, 0.05), (0.33, 1.82), (-0.02, 1.845), (-0.335, 1.75), (-0.31, 1.2),
                    (-0.335, 0.9)], 0.03, 2)
    for s in (-1, 1):
        y0, y1 = sorted((s * 0.35, s * 0.405))
        b.extrude_y(cheek, y0, y1, "prop_laminate")
        b.tube([Vector((-0.34, s * 0.378, 0.06)), Vector((-0.34, s * 0.378, 0.9)), Vector((-0.315, s * 0.378, 1.2)),
                Vector((-0.34, s * 0.378, 1.75)), Vector((-0.02, s * 0.378, 1.855)), Vector((0.33, s * 0.378, 1.83))],
               0.017, "prop_alu", sides=8)
    # The lime laminate band on the desk front and the keyboard drawer.
    b.rbox((-0.255, 0, 0.92), (0.03, 0.72, 0.09), "prop_lime", r=0.01)
    b.rbox((-0.33, 0, 0.815), (0.26, 0.64, 0.06), "prop_laminate", r=0.02)
    b.rbox((-0.46, 0, 0.815), (0.03, 0.66, 0.08), "prop_lime", r=0.012)
    b.panel(frame(Vector((0, -1, 0)), Vector((1, 0, 0)), Vector((0, 0, 1)), (-0.33, 0, 0.852)), 0.46, 0.17, 0.02,
            "prop_bezel", "prop_keyboard", atlas_uv("keyboard"), r=0.015)
    # The screen in its bezel, on the tilted face between (0.05, 0.99) and (0.10, 1.56).
    up = Vector((0.05, 0, 0.57)).normalized()
    n = Vector((-0.57, 0, 0.05)).normalized()
    mid = Vector((0.075, 0, 1.275))
    right = up.cross(n)
    b.rbox(Vector(mid + n * 0.005), (0.64, 0.52, 0.05), "prop_bezel", r=0.03,
           rot=frame(right, up, n, (0, 0, 0)))
    b.panel(frame(right, up, n, mid + n * 0.034), 0.54, 0.40, 0.02, "prop_bezel", "prop_screen",
            atlas_uv(f"screen_{k}"), r=0.02)
    # The terminal's number sticker on the hood front.
    b.panel(facing("-x", (-0.293, 0, 1.665)), 0.10, 0.10, 0.008, "prop_laminate", "prop_sticker", atlas_uv(f"num_{k}"),
            r=0.045)
    # Coin box with its slot and return cup; the lower door with a padlock.
    b.rbox((-0.285, 0.14, 0.60), (0.03, 0.20, 0.28), "prop_stainless", r=0.02)
    b.rbox((-0.302, 0.14, 0.665), (0.014, 0.035, 0.09), "prop_slot", r=0.006)
    b.rbox((-0.303, 0.14, 0.52), (0.02, 0.09, 0.045), "prop_slot", r=0.012)
    b.rbox((-0.29, -0.13, 0.43), (0.02, 0.34, 0.56), "prop_laminate", r=0.02)
    b.blob(Vector((-0.315, 0.0, 0.43)), (0.022, 0.035, 0.04), "prop_alu")
    b.torus((-0.315, 0.0, 0.475), 0.022, 0.008, "prop_alu", axis="x", seg=12, sides=6)
    b.finish(col, origin=(x, y, PT), bevel=0.01)


def pisonet_awning(col):
    b = PBuf("prop_pisonet_awning", top=PT + 2.95)
    xb, xf = 10.99, 8.56
    zb, zf = PT + 2.86, PT + 2.40
    ys = (9.05, 11.75, 14.45)

    def rz_at(x):
        return zb + (zf - zb) * (xb - x) / (xb - xf)

    for yr in ys:
        b.rbox((11.0, yr, zb), (0.07, 0.16, 0.24), "prop_steel_dark", r=0.02)
        b.rbox((11.0, yr, PT + 2.15), (0.06, 0.11, 0.14), "prop_steel_dark", r=0.02)
        b.tube([Vector((xb, yr, zb)), Vector((xf - 0.02, yr, zf))], 0.032, "prop_steel_dark", sides=10)
        b.tube([Vector((xb, yr, PT + 2.15)), Vector((9.9, yr, rz_at(9.9) + 0.005))], 0.024, "prop_steel_dark", sides=8)
    b.tube([Vector((xf, 8.9, zf)), Vector((xf, 14.6, zf))], 0.032, "prop_steel_dark", sides=10)
    b.tube([Vector((10.96, 8.9, zb)), Vector((10.96, 14.6, zb))], 0.03, "prop_steel_dark", sides=10)
    # The tarp: resting on the rafters, sagging between them, draping a little over the front.
    nx, ny = 10, 30
    x0, x1, y0, y1 = 11.0, 8.50, 8.85, 14.65
    grid = []
    for i in range(nx + 1):
        x = x0 + (x1 - x0) * i / nx
        row = []
        for j in range(ny + 1):
            y = y0 + (y1 - y0) * j / ny
            if ys[0] <= y <= ys[-1]:
                k = 0 if y < ys[1] else 1
                frac = (y - ys[k]) / (ys[k + 1] - ys[k])
                sag = 0.045 * math.sin(math.pi * frac) * (0.5 + 0.5 * i / nx)
            else:
                sag = 0.03 * min(1.0, abs(y - min(ys, key=lambda q: abs(q - y))) / 0.2)
            row.append(Vector((x, y, rz_at(min(x, xb)) + 0.037 - sag)))
        grid.append(row)
    b.sheet(grid, 0.012, lambda i, j: "prop_tarp_olive")
    # The front valance, a flap hanging from the edge with a slow wave.
    val = []
    for i in range(3):
        row = []
        for j in range(ny + 1):
            y = y0 + (y1 - y0) * j / ny
            edge = grid[-1][j]
            z = edge.z - 0.006 - 0.2 * i / 2
            row.append(Vector((8.515 + 0.012 * math.sin(y * 3.1) * i / 2, y, z)))
        val.append(row)
    b.sheet(val, 0.01, lambda i, j: "prop_tarp_olive", normal=Vector((-1, 0, 0)))
    b.finish(col, origin=(9.75, 11.75, PT), bevel=0.008)

    # The rate board, standing on the front rail, tilted back, on two flat straps.
    r = PBuf("prop_pisonet_rate_board", top=PT + 3.0)
    for yy in (11.3, 12.2):
        r.rbox((8.585, yy, PT + 2.56), (0.03, 0.06, 0.42), "prop_steel_dark", r=0.01)
    r.panel(facing("-x", (8.555, 11.75, PT + 2.73), tilt_deg=8), 1.3, 0.5, 0.035, "prop_wood_dark", "prop_sign_pisonet",
            r=0.03)
    r.finish(col, origin=(8.56, 11.75, PT + 2.40), bevel=0.008)


def pisonet_cord(col):
    """THE TRIP HAZARD. Two leads taped together into a flat bundle, a power strip, and a third lead,
    all lying on the pavement and none of it over 27 mm tall."""
    b = PBuf("prop_pisonet_cord", top=None)
    runs = [
        [(9.40, 10.32), (9.05, 10.46), (8.70, 10.56), (8.42, 10.78), (8.38, 11.02), (8.40, 11.12)],
        [(8.40, 11.48), (8.30, 11.76), (8.22, 12.10), (8.34, 12.55), (8.74, 12.80), (9.10, 12.95), (9.40, 13.20)],
        [(8.44, 11.30), (8.90, 11.33), (9.40, 11.48)],
    ]
    # Three leads per bundle, one of them the yellow heavy-duty extension cord, so the hazard
    # reads at game distance (review v1: a two-cable bundle read as a thin black line).
    for n, run in enumerate(runs):
        path = catmull([(x, y, 0) for x, y in run], per=6)
        leads = ((-0.022, "prop_cable"), (0.0, "prop_cord_yellow"), (0.022, "prop_cable")) if n < 2 else             ((0.0, "prop_cord_yellow"),)
        for side, mat in leads:
            pts = []
            for i, p in enumerate(path):
                d = (path[min(i + 1, len(path) - 1)] - path[max(i - 1, 0)]).normalized()
                lat = Vector((-d.y, d.x, 0))
                pts.append(Vector((p.x, p.y, PT + 0.0105)) + lat * side)
            b.tube(pts, 0.0115, mat, sides=8)
        if n < 2:                              # fat grey tape bands every ~40 cm
            acc = 0.2
            for i in range(1, len(path)):
                acc += (path[i] - path[i - 1]).length
                if acc > 0.4 and i < len(path) - 1:
                    acc = 0.0
                    d = (path[i + 1] - path[i - 1]).normalized()
                    yaw = math.degrees(math.atan2(d.y, d.x))
                    b.rbox((path[i].x, path[i].y, PT + 0.012), (0.07, 0.084, 0.026), "prop_tape", r=0.014,
                           rot=rz(yaw))
    b.rbox((8.40, 11.30, PT + 0.012), (0.09, 0.36, 0.027), "prop_plastic_white", r=0.025)
    for k in range(3):
        b.rbox((8.40, 11.19 + k * 0.08, PT + 0.024), (0.04, 0.05, 0.005), "prop_slot", r=0.012)
    b.rbox((8.40, 11.44, PT + 0.0235), (0.03, 0.03, 0.006), "prop_plastic_red", r=0.01)
    b.finish(col, origin=(8.40, 11.30, PT), bevel=0.004)


def pisonet(parent):
    col = collection("prop_pisonet", parent)
    for k, y in enumerate((10.0, 11.75, 13.5), start=1):
        pisonet_terminal(col, 9.70, y, k)
        ch = PBuf(f"prop_pisonet_chair_{k}", top=PT + 0.87)
        monobloc(ch, Vector((8.95, y + (0.04, -0.03, 0.05)[k - 1], PT)), (4, -6, 2)[k - 1],
                 ("prop_plastic_white", "prop_plastic_red", "prop_plastic_white")[k - 1])
        ch.finish(col, origin=(8.95, y, PT), bevel=0.008)
    pisonet_awning(col)
    pisonet_cord(col)


# ------------------------------------------------------------------ the pares cart

def pares(parent):
    col = collection("prop_pares_cart", parent)
    cx, cy = 8.80, -5.00
    b = PBuf("prop_pares_cart", top=PT + 1.10)
    # Wheels at y -4.45 outside the body: a fat tyre, a stainless rim, five fat spokes and a hub.
    wy, wz = -4.45, PT + 0.32
    for wx in (8.16, 9.44):
        b.torus((wx, wy, wz), 0.29, 0.038, "prop_rubber", axis="x", seg=28, sides=10)
        b.torus((wx, wy, wz), 0.245, 0.016, "prop_alu", axis="x", seg=24, sides=8)
        for k in range(5):
            a = k / 5 * math.tau + 0.3
            b.tube([Vector((wx, wy, wz)), Vector((wx, wy + 0.25 * math.cos(a), wz + 0.25 * math.sin(a)))], 0.018,
                   "prop_alu", sides=6)
        b.blob(Vector((wx, wy, wz)), (0.05, 0.045, 0.045), "prop_steel_dark")
    b.tube([Vector((8.10, wy, wz)), Vector((9.50, wy, wz))], 0.02, "prop_steel_dark", sides=8)
    # Legs with rubber feet at the south end.
    for lx in (8.32, 9.28):
        b.prism((lx, -5.75, PT + 0.03), (lx, -5.75, PT + 0.58), 0.025, 0.025, "prop_steel_dark", r=0.008)
        b.rbox((lx, -5.75, PT + 0.025), (0.08, 0.08, 0.07), "prop_rubber", r=0.02)
        b.prism((lx, -4.45, wz), (lx, -4.45, PT + 0.58), 0.022, 0.022, "prop_steel_dark", r=0.008)
    # Lower shelf with a tub of washed bowls.
    b.rbox((8.80, -5.2, PT + 0.28), (0.98, 1.2, 0.03), "prop_steel_dark", r=0.02)
    b.lathe((8.9, -5.35, PT + 0.295), [(0.2, 0.0), (0.24, 0.16), (0.25, 0.17), (0.21, 0.17), (0.2, 0.1)],
            "prop_plastic_green", sides=16)
    # The cabinet, its doors on the vendor side, and the painted panel on the street side.
    b.rbox((cx, cy, PT + 0.80), (1.10, 1.80, 0.50), "prop_steel_jade", r=0.03)
    for dy in (-0.45, 0.45):
        b.rbox((9.355, cy + dy, PT + 0.80), (0.02, 0.78, 0.40), "prop_steel_jade_lt", r=0.02)
        b.blob(Vector((9.37, cy + dy * 0.2, PT + 0.82)), (0.02, 0.03, 0.03), "prop_alu")
    b.panel(facing("-x", (8.243, cy, PT + 0.80)), 1.70, 0.44, 0.02, "prop_steel_jade", "prop_sign_cart", r=0.03)
    b.rbox((8.75, cy, PT + 1.07), (1.30, 1.95, 0.06), "prop_stainless", r=0.04)
    # Handle bar at the south end.
    b.tube([Vector((8.35, -5.85, PT + 0.95)), Vector((8.35, -6.24, PT + 0.99)), Vector((9.25, -6.24, PT + 0.99)),
            Vector((9.25, -5.85, PT + 0.95))], 0.024, "prop_alu", sides=8)
    for hx in (8.55, 9.05):
        b.tube([Vector((hx, -6.24, PT + 0.99)), Vector((hx + 0.18, -6.24, PT + 0.99))], 0.034, "prop_rubber", sides=8)
    # Roof posts and the frame, then the maroon tarp roof sloping to the street, with a valance.
    posts = [(8.15, -5.92), (8.15, -4.08), (9.38, -5.92), (9.38, -4.08)]

    def roof_z(x):
        return PT + 2.26 + (x - 7.95) / 1.6 * 0.18

    # Chunky members (review v2: 4 cm posts and 2 cm rails read as wire at game distance).
    for px, py in posts:
        b.prism((px, py, PT + 1.06), (px, py, roof_z(px) + 0.01), 0.032, 0.028, "prop_alu", r=0.01)
    for py in (-5.92, -4.08):
        b.tube([Vector((7.97, py, roof_z(7.97) - 0.02)), Vector((9.53, py, roof_z(9.53) - 0.02))], 0.028, "prop_alu",
               sides=10)
    for px in (8.15, 9.38):
        b.tube([Vector((px, -6.08, roof_z(px) - 0.02)), Vector((px, -3.92, roof_z(px) - 0.02))], 0.028, "prop_alu",
               sides=10)
    grid = []
    for i in range(9):
        x = 7.95 + 1.6 * i / 8
        row = []
        for j in range(13):
            y = -6.1 + 2.2 * j / 12
            sag = 0.03 * math.sin(math.pi * ((y + 6.1) / 2.2 * 2 % 1.0))
            row.append(Vector((x, y, roof_z(x) + 0.03 - sag + 0.02 * math.sin(math.pi * i / 8))))
        grid.append(row)
    b.sheet(grid, 0.02, lambda i, j: "prop_tarp_maroon")
    val = [[Vector((7.965 - 0.012 * math.sin(p.y * 5) * r / 2, p.y, p.z - 0.006 - 0.2 * r / 2)) for p in grid[0]]
           for r in range(3)]
    b.sheet(val, 0.016, lambda i, j: "prop_tarp_maroon", normal=Vector((-1, 0, 0)))
    # A tube lamp under the roof, over the counter.
    b.rbox((8.62, cy, roof_z(8.62) - 0.05), (0.07, 0.7, 0.05), "prop_steel_dark", r=0.015)
    b.tube([Vector((8.62, cy - 0.3, roof_z(8.62) - 0.095)), Vector((8.62, cy + 0.3, roof_z(8.62) - 0.095))], 0.024,
           "prop_lamp", sides=8)
    b.tube([Vector((8.62, cy, roof_z(8.62) - 0.03)), Vector((8.62, cy, roof_z(8.62) + 0.03))], 0.014, "prop_steel_dark")
    # The glass case sits at the vendor's north corner, so the big pot stands clear in the middle
    # of the counter where the street sees it (review v2: the case hid the pot).
    gx0, gx1, gy0, gy1 = 8.98, 9.36, -4.92, -4.20
    for gx in (gx0, gx1):
        for gy in (gy0, gy1):
            b.prism((gx, gy, PT + 1.09), (gx, gy, PT + 1.49), 0.017, 0.017, "prop_alu", r=0.006)
    b.rbox(((gx0 + gx1) / 2, (gy0 + gy1) / 2, PT + 1.495), (0.44, 0.78, 0.035), "prop_stainless", r=0.02)
    b.finish(col, origin=(cx, cy, PT), bevel=0.01)

    g = PBuf("prop_pares_cart_glass")
    for gx in (gx0, gx1):
        g.rbox((gx, (gy0 + gy1) / 2, PT + 1.29), (0.008, 0.70, 0.39), "prop_glass", r=0.004)
    for gy in (gy0, gy1):
        g.rbox(((gx0 + gx1) / 2, gy, PT + 1.29), (0.36, 0.008, 0.39), "prop_glass", r=0.004)
    g.finish(col, origin=(cx, cy, PT), bevel=0)

    f = PBuf("prop_pares_cart_food", top=PT + 1.5)

    def pot_mat(fc):
        return "prop_food_broth" if fc.normal.z > 0.9 and fc.calc_center_median().z < PT + 1.10 + 0.34 else "prop_alu"

    # The big pares pot, open, in the middle of the counter, with its ladle standing in it.
    f.lathe((8.66, -5.25, PT + 1.09), [(0.01, 0.0), (0.23, 0.0), (0.25, 0.03), (0.25, 0.35), (0.27, 0.36),
                                       (0.265, 0.385), (0.235, 0.375), (0.235, 0.30), (0.01, 0.30)],
            "prop_alu", sides=24, mat_of=pot_mat)
    f.tube([Vector((8.70, -5.20, PT + 1.38)), Vector((8.86, -5.08, PT + 1.66))], 0.016, "prop_alu", sides=6)
    f.blob(Vector((8.69, -5.21, PT + 1.39)), (0.06, 0.06, 0.022), "prop_alu")
    # The mami pot, lidded, behind it on the vendor side.
    f.lathe((9.12, -5.55, PT + 1.09), [(0.01, 0.0), (0.18, 0.0), (0.19, 0.03), (0.19, 0.30), (0.2, 0.31),
                                       (0.16, 0.36), (0.05, 0.385), (0.03, 0.41), (0.01, 0.41)], "prop_alu", sides=24)
    f.blob(Vector((9.12, -5.55, PT + 1.51)), (0.04, 0.04, 0.025), "prop_bezel")
    for k in range(4):                     # a stack of bowls in the glass case
        f.lathe((9.17, -4.74, PT + 1.10 + k * 0.035), [(0.035, 0.0), (0.075, 0.035), (0.085, 0.06), (0.07, 0.06),
                                                         (0.03, 0.015)], "prop_bowl", sides=16)
    f.lathe((9.17, -4.38, PT + 1.10), [(0.05, 0.0), (0.075, 0.08), (0.065, 0.085), (0.04, 0.02)], "prop_bowl", sides=14)
    # Condiments along the street-side ledge, where the customers stand.
    bottles = [("prop_sauce_spicy", -4.92), ("prop_sauce_sweet", -4.82), ("prop_sauce_vinegar", -4.72)]
    for mat, by in bottles:
        f.lathe((8.20, by, PT + 1.095), [(0.034, 0.0), (0.036, 0.12), (0.024, 0.15), (0.01, 0.19), (0.005, 0.2)],
                mat, sides=12)
    f.lathe((8.21, -4.56, PT + 1.095), [(0.045, 0.0), (0.047, 0.09), (0.05, 0.1), (0.05, 0.12), (0.01, 0.122)],
            "prop_sauce_spicy", sides=14)
    f.lathe((8.24, -4.36, PT + 1.095), [(0.04, 0.0), (0.045, 0.11), (0.035, 0.11), (0.03, 0.02)], "prop_alu", sides=12)
    for k in range(3):
        a = k * 2.1
        f.tube([Vector((8.24, -4.36, PT + 1.12)), Vector((8.24 + 0.03 * math.cos(a), -4.36 + 0.03 * math.sin(a), PT + 1.28))],
               0.009, "prop_alu", sides=5)
    f.finish(col, origin=(cx, cy, PT), bevel=0.005)

    lpg = PBuf("prop_pares_lpg", top=PT + 0.62)
    lpg.lathe((9.78, -5.55, PT - 0.01), [(0.12, 0.0), (0.155, 0.03), (0.16, 0.40), (0.14, 0.47), (0.08, 0.51),
                                          (0.075, 0.54)], "prop_lpg_red", sides=20)
    lpg.torus((9.78, -5.55, PT + 0.585), 0.075, 0.022, "prop_lpg_red", seg=16, sides=8)
    lpg.blob(Vector((9.78, -5.55, PT + 0.56)), (0.035, 0.035, 0.05), "prop_alu")
    lpg.tube(catmull([(9.78, -5.55, PT + 0.6), (9.7, -5.5, PT + 0.75), (9.52, -5.45, PT + 0.78),
                      (9.34, -5.42, PT + 0.72)], per=5), 0.012, "prop_rubber", sides=6)
    lpg.finish(col, origin=(9.78, -5.55, PT), bevel=0.008)

    for k, (sx, sy, mat) in enumerate(((7.90, -5.55, "prop_plastic_red"), (7.92, -4.70, "prop_plastic_green")), 1):
        s = PBuf(f"prop_pares_stool_{k}", top=PT + 0.45)
        plastic_stool(s, Vector((sx, sy, PT)), mat)
        s.finish(col, origin=(sx, sy, PT), bevel=0.008)

    a = PBuf("prop_pares_aboard", top=PT + 1.04)
    ax, ay = 8.05, -3.75
    th = math.atan2(0.24, 1.0)
    for s in (-1, 1):
        normal = Vector((s * math.cos(th), 0, math.sin(th)))
        up = Vector((-s * math.sin(th), 0, math.cos(th)))
        right = up.cross(normal)
        centre = Vector((ax + s * 0.12, ay, PT + 0.505))
        mtx = frame(right, up, normal, centre)
        a.M = mtx
        for sx in (-1, 1):
            a.rbox((sx * 0.30, 0, 0), (0.05, 1.03, 0.035), "prop_wood_dark", r=0.012)
        a.rbox((0, 0.485, 0), (0.62, 0.05, 0.035), "prop_wood_dark", r=0.012)
        a.rbox((0, -0.20, 0), (0.62, 0.04, 0.035), "prop_wood_dark", r=0.012)
        a.M = Matrix()
        a.panel(mtx @ Matrix.Translation((0, 0.14, 0.0)), 0.58, 0.66, 0.02, "prop_wood_dark", "prop_sign_aboard", r=0.01)
    a.tube([Vector((ax, ay - 0.33, PT + 1.005)), Vector((ax, ay + 0.33, PT + 1.005))], 0.02, "prop_steel_dark", sides=8)
    a.finish(col, origin=(ax, ay, PT), bevel=0.006)


# ------------------------------------------------------------------ the overclock pad

def overclock_pad(parent):
    col = collection("prop_overclock_pad", parent)
    px, py = 9.0, 5.5
    b = PBuf("prop_overclock_pad")
    b.panel(facing("up", (px, py, PT + 0.004)), 1.8, 1.8, 0.028, "prop_pad_rim", "prop_pad_plate", r=0.12)
    for dx, mat in ((-0.46, "prop_bar_plum"), (0.0, "prop_bar_glow"), (0.46, "prop_bar_gold")):
        b.rbox((px + dx, py, PT + 0.0165), (0.25, 0.96, 0.009), "prop_pad_rim", r=0.05)
        b.rbox((px + dx, py, PT + 0.0195), (0.17, 0.90, 0.013), mat, r=0.06)
    b.finish(col, origin=(px, py, PT), bevel=0.004)


# ------------------------------------------------------------------ the bridge hoop

def bridge_hoop(parent):
    col = collection("prop_bridge_hoop", parent)
    ox, oy = -8.90, -10.0
    b = PBuf("prop_bridge_hoop", top=PT + 3.65)
    bx = -9.62
    b.torus((bx, oy, PT + 0.09), 0.30, 0.10, "prop_rubber", seg=28, sides=10)
    b.lathe((bx, oy, PT + 0.02), [(0.30, 0.0), (0.30, 0.14), (0.2, 0.16), (0.05, 0.165)], "prop_concrete", sides=24)
    b.tube([Vector((bx, oy, PT + 0.12)), Vector((bx + 0.02, oy, PT + 3.60))], 0.065, "prop_steel_yellow", sides=12)
    b.blob(Vector((bx + 0.02, oy, PT + 3.61)), (0.075, 0.075, 0.05), "prop_steel_yellow")
    for z in (3.02, 3.46):
        b.tube([Vector((bx, oy, PT + z)), Vector((-9.35, oy, PT + z))], 0.036, "prop_steel_yellow", sides=10)
    b.tube([Vector((bx + 0.01, oy, PT + 2.62)), Vector((-9.36, oy, PT + 2.98))], 0.03, "prop_steel_yellow", sides=8)
    b.panel(facing("+x", (-9.345, oy, PT + 3.25)), 1.2, 0.8, 0.036, "prop_wood", "prop_sign_backboard", r=0.03)
    b.rbox((-9.235, oy, PT + 3.07), (0.21, 0.10, 0.06), "prop_steel_red", r=0.02)
    b.tube([Vector((-9.33, oy, PT + 2.91)), Vector((-9.17, oy, PT + 3.055))], 0.02, "prop_steel_red", sides=8)
    b.torus((ox, oy, PT + 3.07), 0.25, 0.02, "prop_steel_red", seg=32, sides=8)
    b.finish(col, origin=(ox, oy, PT), bevel=0.01)


# ------------------------------------------------------------------ the west stalls

def stall_fruit(parent):
    col = collection("prop_stall_fruit", parent)
    cx, cy = -10.35, 12.5
    b = PBuf("prop_stall_fruit", top=PT + 0.82)
    b.rbox((cx, cy, PT + 0.785), (1.0, 1.5, 0.05), "prop_wood", r=0.02)
    for lx in (cx - 0.44, cx + 0.44):
        for ly in (cy - 0.67, cy + 0.67):
            b.prism((lx, ly, PT - 0.01), (lx, ly, PT + 0.77), 0.035, 0.032, "prop_wood_dark", r=0.01)
    b.rbox((cx, cy, PT + 0.26), (0.92, 1.4, 0.035), "prop_wood_dark", r=0.02)
    crate(b, Vector((cx - 0.05, cy - 0.5, PT + 0.2)), (0.46, 0.4, 0.26), "prop_plastic_maroon", yaw=4)
    # Two display crates tilted to the street, mangoes in one and bananas in the other.
    rng = random.Random(21)
    for k, (dy, fruit) in enumerate(((-0.36, "mango"), (0.36, "banana"))):
        saved = b.M
        b.M = Matrix.Translation((cx - 0.05, cy + dy, PT + 0.81 + 0.08)) @ Matrix.Rotation(math.radians(14), 4, "Y")
        w = 0.022
        b.rbox((0, 0, 0.01), (0.62, 0.62, 0.025), "prop_wood", r=0.02)
        for s in (-1, 1):
            b.rbox((s * 0.3, 0, 0.07), (w, 0.62, 0.13), "prop_wood", r=0.008)
            b.rbox((0, s * 0.3, 0.07), (0.6, w, 0.13), "prop_wood", r=0.008)
        # A wedge block under the raised back edge, so the tilted crate rests on something.
        b.rbox((-0.25, 0, -0.07), (0.1, 0.56, 0.16), "prop_wood_dark", r=0.015,
               rot=Matrix.Rotation(math.radians(-14), 4, "Y"))
        if fruit == "mango":
            for i in range(4):
                for j in range(5):
                    for layer in (0, 1):
                        if layer and (i in (0, 3) or j in (0, 4)):
                            continue
                        x = -0.21 + i * 0.14 + rng.uniform(-0.015, 0.015)
                        y = -0.23 + j * 0.115 + rng.uniform(-0.015, 0.015)
                        mat = "prop_food_mango" if rng.random() > 0.25 else "prop_food_mango_g"
                        b.blob(Vector((x, y, 0.06 + layer * 0.06)), (0.07, 0.05, 0.045), mat,
                               rot=Matrix.Rotation(rng.uniform(-0.5, 0.5), 4, "Z"))
        else:
            for h in range(3):
                for f_ in range(5):
                    a = math.radians(-40 + f_ * 20)
                    base = Vector((-0.16 + h * 0.16, -0.1 + h * 0.07, 0.07))
                    b.blob(base + Vector((0.09 * math.sin(a), 0.09 * math.cos(a) * 0.4 + 0.08, 0.02)),
                           (0.028, 0.1, 0.028), "prop_food_banana",
                           rot=Matrix.Rotation(a * 0.6, 4, "Z") @ Matrix.Rotation(0.25, 4, "X"))
        b.M = saved
    for k in range(3):                     # pomelos on the front corner
        b.blob(Vector((cx + 0.33 - k * 0.02, cy + 0.02 + (k - 1) * 0.14, PT + 0.87)), (0.08, 0.08, 0.075),
               "prop_food_pomelo")
    # A market scale: a squat base, a round dial facing the street, a pan.
    b.rbox((cx + 0.3, cy - 0.2, PT + 0.86), (0.2, 0.22, 0.12), "prop_steel_red", r=0.04)
    b.panel(facing("+x", (cx + 0.41, cy - 0.2, PT + 0.97)), 0.16, 0.16, 0.03, "prop_steel_red", "prop_plastic_cream",
            r=0.075)
    b.lathe((cx + 0.3, cy - 0.2, PT + 1.07), [(0.03, 0.0), (0.14, 0.03), (0.15, 0.045), (0.13, 0.04), (0.02, 0.015)],
            "prop_alu", sides=18)
    b.tube([Vector((cx + 0.3, cy - 0.2, PT + 0.9)), Vector((cx + 0.3, cy - 0.2, PT + 1.08))], 0.02, "prop_alu")
    umbrella(b, Vector((cx - 0.48, cy - 0.05, PT)), PT + 2.5, 1.15, ["prop_canvas_red", "prop_canvas_cream"],
             tilt=(4, 0))
    b.finish(col, origin=(cx, cy, PT), bevel=0.008)


def stall_fishball(parent):
    col = collection("prop_stall_fishball", parent)
    cx, cy = -10.35, 14.6
    b = PBuf("prop_stall_fishball", top=PT + 0.92)
    b.rbox((cx, cy, PT + 0.62), (0.85, 1.20, 0.50), "prop_cart_cream", r=0.03)
    b.panel(facing("+x", (-9.922, cy, PT + 0.62)), 1.10, 0.31, 0.02, "prop_cart_cream", "prop_sign_fishball", r=0.02)
    b.rbox((cx, cy, PT + 0.895), (0.95, 1.30, 0.05), "prop_stainless", r=0.04)
    for wy in (13.97, 15.23):
        b.torus((-10.62, wy, PT + 0.17), 0.15, 0.035, "prop_rubber", axis="y", seg=20, sides=8)
        b.blob(Vector((-10.62, wy, PT + 0.17)), (0.05, 0.03, 0.05), "prop_alu")
    b.tube([Vector((-10.62, 13.95, PT + 0.17)), Vector((-10.62, 15.25, PT + 0.17))], 0.018, "prop_steel_dark")
    for ly in (14.1, 15.1):
        b.prism((-10.0, ly, PT - 0.01), (-10.0, ly, PT + 0.38), 0.022, 0.022, "prop_steel_dark", r=0.008)
        b.prism((-10.62, ly, PT + 0.17), (-10.62, ly, PT + 0.38), 0.02, 0.02, "prop_steel_dark", r=0.008)

    def wok_mat(fc):
        return "prop_food_oil" if fc.normal.z > 0.9 and fc.calc_center_median().z < PT + 1.05 else "prop_steel_dark"

    wx, wy, wz = -10.15, 14.40, PT + 0.905
    b.lathe((wx, wy, wz), [(0.02, 0.0), (0.15, 0.012), (0.27, 0.07), (0.32, 0.13), (0.335, 0.145), (0.322, 0.155),
                           (0.30, 0.13), (0.28, 0.095), (0.02, 0.095)], "prop_steel_dark", sides=24, mat_of=wok_mat)
    rng = random.Random(33)
    for k in range(11):
        a, r = rng.uniform(0, math.tau), rng.uniform(0.02, 0.2)
        b.blob(Vector((wx + r * math.cos(a), wy + r * math.sin(a), wz + 0.1)), (0.032, 0.032, 0.026),
               "prop_food_fishball")
    for k in range(4):
        a = rng.uniform(0, math.tau)
        b.blob(Vector((wx + 0.16 * math.cos(a), wy + 0.16 * math.sin(a), wz + 0.1)), (0.07, 0.022, 0.02),
               "prop_food_kikiam", rot=Matrix.Rotation(a + 1.3, 4, "Z"))
    for k, mat in enumerate(("prop_sauce_sweet", "prop_sauce_spicy", "prop_sauce_vinegar")):
        jy = 14.10 + k * 0.2 + 0.4
        b.lathe((-10.62, jy, PT + 0.915), [(0.07, 0.0), (0.075, 0.18), (0.07, 0.2), (0.02, 0.2)], mat, sides=16)
        b.tube([Vector((-10.62, jy, PT + 1.05)), Vector((-10.66, jy - 0.02, PT + 1.22))], 0.01, "prop_alu", sides=5)
    b.lathe((-10.25, 15.05, PT + 0.915), [(0.045, 0.0), (0.05, 0.12), (0.04, 0.12), (0.035, 0.02)], "prop_plastic_white",
            sides=12)
    b.blob(Vector((-10.25, 15.05, PT + 1.12)), (0.035, 0.035, 0.13), "prop_sticks")
    b.lathe((cx, cy, PT - 0.01), [(0.1, 0.0), (0.12, 0.02), (0.12, 0.25), (0.09, 0.31), (0.05, 0.33), (0.05, 0.36)],
            "prop_lpg_red", sides=16)
    b.tube(catmull([(cx, cy, PT + 0.35), (cx + 0.05, cy + 0.1, PT + 0.36), (cx + 0.1, cy + 0.2, PT + 0.39)], per=4),
           0.01, "prop_rubber", sides=6)
    umbrella(b, Vector((-10.82, cy + 0.05, PT)), PT + 2.3, 1.0,
             ["prop_canvas_yellow", "prop_canvas_green"], tilt=(5, 0), weight=False)
    b.rbox((-10.80, cy + 0.05, PT + 0.75), (0.06, 0.08, 0.1), "prop_steel_dark", r=0.02)
    b.finish(col, origin=(cx, cy, PT), bevel=0.008)


def stall_sarisari(parent):
    col = collection("prop_stall_sarisari", parent)
    cx, cy = -10.35, -14.2
    b = PBuf("prop_stall_sarisari", top=PT + 1.1)
    b.rbox((cx, cy, PT + 0.705), (0.9, 1.5, 0.05), "prop_wood", r=0.02)
    for lx in (cx - 0.4, cx + 0.4):
        for ly in (cy - 0.68, cy + 0.68):
            b.prism((lx, ly, PT - 0.01), (lx, ly, PT + 0.69), 0.035, 0.032, "prop_wood_dark", r=0.01)
    b.rbox((cx, cy, PT + 0.22), (0.82, 1.4, 0.035), "prop_wood_dark", r=0.02)
    b.rbox((cx - 0.26, cy, PT + 0.80), (0.34, 1.44, 0.15), "prop_wood_dark", r=0.02)
    b.rbox((cx - 0.34, cy, PT + 0.94), (0.18, 1.44, 0.15), "prop_wood_dark", r=0.02)
    jars = [("prop_candy_a", -0.26, -0.5, 0.875), ("prop_candy_b", -0.26, -0.17, 0.875), ("prop_candy_c", -0.26, 0.17, 0.875),
            ("prop_candy_d", -0.26, 0.5, 0.875), ("prop_candy_b", -0.34, -0.33, 1.015), ("prop_candy_a", -0.34, 0.0, 1.015),
            ("prop_candy_c", -0.34, 0.33, 1.015), ("prop_candy_d", 0.12, -0.55, 0.73)]
    for k, (mat, dx, dy, z) in enumerate(jars):
        b.lathe((cx + dx, cy + dy, PT + z - 0.005), [(0.065, 0.0), (0.075, 0.03), (0.075, 0.17), (0.055, 0.2),
                                                     (0.045, 0.2)], mat, sides=16)
        b.lathe((cx + dx, cy + dy, PT + z + 0.19), [(0.052, 0.0), (0.056, 0.045), (0.01, 0.05)],
                ("prop_plastic_red", "prop_plastic_green", "prop_plastic_yellow")[k % 3], sides=16)
    # Cigarettes and candy in a tray at the front edge.
    b.rbox((cx + 0.26, cy + 0.15, PT + 0.75), (0.28, 0.5, 0.05), "prop_plastic_cream", r=0.02)
    for k in range(5):
        b.rbox((cx + 0.26, cy - 0.05 + k * 0.1, PT + 0.775), (0.2, 0.07, 0.04),
               ("prop_candy_a", "prop_candy_b", "prop_candy_c", "prop_candy_d", "prop_plastic_red")[k], r=0.01)
    # The sachet rack: two posts and a bar at the back, four strips hanging.
    for py in (cy - 0.66, cy + 0.66):
        b.prism((cx - 0.42, py, PT + 0.7), (cx - 0.42, py, PT + 1.92), 0.02, 0.02, "prop_wood_dark", r=0.008)
    b.tube([Vector((cx - 0.42, cy - 0.7, PT + 1.88)), Vector((cx - 0.42, cy + 0.7, PT + 1.88))], 0.022, "prop_wood_dark")
    for k, py in enumerate((-0.48, -0.16, 0.16, 0.48)):
        b.panel(facing("+x", (cx - 0.40, cy + py, PT + 1.55), tilt_deg=-2), 0.12, 0.6, 0.006, "prop_plastic_white",
                "prop_sachet_strip", (0.0, 0.0, 1.0, 1.0), r=0.01)
    # The styrofoam ice box on the pavement, and the cardboard sign leaning on the stand.
    b.rbox((cx - 0.12, -13.04, PT + 0.16), (0.52, 0.36, 0.34), "prop_styro", r=0.03)
    b.rbox((cx - 0.12, -13.04, PT + 0.345), (0.55, 0.39, 0.05), "prop_styro", r=0.03)
    b.panel(facing("+x", (-9.87, cy + 0.2, PT + 0.33), tilt_deg=14), 0.6, 0.4, 0.008, "prop_sign_sarisari",
            "prop_sign_sarisari", r=0.01, jitter=0.012, seed=5)
    umbrella(b, Vector((cx - 0.52, cy, PT)), PT + 2.55, 1.2,
             ["prop_canvas_red", "prop_canvas_yellow", "prop_canvas_green", "prop_canvas_cream"], tilt=(4, 0))
    b.finish(col, origin=(cx, cy, PT), bevel=0.008)


# ------------------------------------------------------------------ clutter

def clutter(parent):
    col = collection("prop_clutter", parent)
    b = PBuf("prop_crate_stack_w", top=PT + 0.84)
    crate(b, Vector((-10.55, 11.05, PT - 0.01)), (0.45, 0.34, 0.28), "prop_plastic_yellow", yaw=3)
    crate(b, Vector((-10.54, 11.04, PT + 0.265)), (0.45, 0.34, 0.28), "prop_plastic_maroon", yaw=-5)
    crate(b, Vector((-10.57, 11.06, PT + 0.54)), (0.45, 0.34, 0.28), "prop_plastic_green", yaw=8)
    b.finish(col, origin=(-10.55, 11.05, PT))

    b = PBuf("prop_crate_stack_e", top=PT + 0.6)
    crate(b, Vector((10.45, -6.80, PT - 0.01)), (0.45, 0.34, 0.28), "prop_plastic_maroon", yaw=-4)
    crate(b, Vector((10.47, -6.78, PT + 0.265)), (0.45, 0.34, 0.28), "prop_plastic_yellow", yaw=6)
    b.lathe((10.46, -6.36, PT - 0.01), [(0.12, 0.0), (0.135, 0.03), (0.135, 0.3), (0.06, 0.4), (0.03, 0.42),
                                        (0.03, 0.47), (0.005, 0.47)], "prop_plastic_cream", sides=18)
    b.finish(col, origin=(10.45, -6.80, PT))

    b = PBuf("prop_water_drum", top=PT + 0.92)
    dx, dy = -10.55, 15.95
    b.lathe((dx, dy, PT - 0.01), [(0.26, 0.0), (0.285, 0.03), (0.29, 0.28), (0.3, 0.31), (0.29, 0.34), (0.29, 0.58),
                                  (0.3, 0.61), (0.29, 0.64), (0.285, 0.86), (0.26, 0.89), (0.05, 0.9)],
            "prop_plastic_cream", sides=24)
    b.lathe((dx, dy, PT + 0.87), [(0.3, 0.0), (0.305, 0.04), (0.28, 0.055), (0.02, 0.06)], "prop_plastic_green", sides=24)
    b.lathe((dx + 0.05, dy - 0.03, PT + 0.925), [(0.06, 0.0), (0.085, 0.08), (0.08, 0.085), (0.05, 0.01)],
            "prop_plastic_red", sides=16)
    b.tube([Vector((dx + 0.12, dy - 0.03, PT + 0.97)), Vector((dx + 0.24, dy - 0.05, PT + 0.985))], 0.014,
           "prop_plastic_red", sides=6)
    b.finish(col, origin=(dx, dy, PT))

    b = PBuf("prop_bench", top=PT + 0.46)
    bx, by = -10.62, 6.2
    b.rbox((bx, by, PT + 0.435), (0.34, 1.6, 0.05), "prop_wood", r=0.02)
    for ly in (by - 0.6, by + 0.6):
        for sx in (-1, 1):
            b.prism((bx + sx * 0.16, ly, PT - 0.01), (bx + sx * 0.08, ly, PT + 0.42), 0.03, 0.03, "prop_wood_dark", r=0.01)
        b.rbox((bx, ly, PT + 0.2), (0.3, 0.04, 0.05), "prop_wood_dark", r=0.01)
    b.rbox((bx, by, PT + 0.2), (0.04, 1.24, 0.05), "prop_wood_dark", r=0.01)
    b.finish(col, origin=(bx, by, PT))

    b = PBuf("prop_trash_bin", top=PT + 0.84)
    tx, ty = -10.50, -12.30
    b.lathe((tx, ty, PT - 0.01), [(0.21, 0.0), (0.225, 0.03), (0.26, 0.74), (0.27, 0.76), (0.24, 0.77)],
            "prop_plastic_green", sides=24)
    b.lathe((tx, ty, PT + 0.765), [(0.285, 0.0), (0.29, 0.03), (0.2, 0.08), (0.04, 0.09)], "prop_plastic_green", sides=24)
    b.rbox((tx, ty, PT + 0.86), (0.05, 0.14, 0.035), "prop_plastic_green", r=0.012)
    b.finish(col, origin=(tx, ty, PT))

    for name, (x, y, yaw, mat) in (("prop_chair_w1", (-10.25, 13.62, -8, "prop_plastic_red")),
                                   ("prop_chair_w2", (-10.35, -15.75, 5, "prop_plastic_white"))):
        c = PBuf(name, top=PT + 0.87)
        monobloc(c, Vector((x, y, PT)), yaw, mat)
        c.finish(col, origin=(x, y, PT), bevel=0.008)


# ------------------------------------------------------------------ column signs (at the origin)

def column_signs(parent):
    col = collection("prop_column_signs (place on piers)", parent)
    face = frame(Vector((1, 0, 0)), Vector((0, 0, 1)), Vector((0, -1, 0)), (0, 0, 0))   # face toward -Y
    a = PBuf("prop_bawal_umihi_a", top=0.26)
    a.panel(face @ Matrix.Translation((0, 0, -0.002)), 0.80, 0.52, 0.02, "prop_steel_red", "prop_sign_bawal_a", r=0.05)
    for sx in (-1, 1):
        for sz in (-1, 1):
            a.blob(Vector((sx * 0.355, -0.013, sz * 0.215)), (0.018, 0.008, 0.018), "prop_alu")
    a.finish(col, bevel=0.006)

    b = PBuf("prop_bawal_umihi_b", top=0.24)
    b.panel(face @ Matrix.Rotation(math.radians(2), 4, "Z") @ Matrix.Translation((0, 0, -0.0045)), 0.90, 0.45, 0.025,
            "prop_wood_dark", "prop_sign_bawal_b", r=0.012, jitter=0.008, seed=9)
    b.finish(col, bevel=0.006)

    t = PBuf("prop_barangay_tarp", top=0.42)
    w, h, nx, nz = 1.40, 0.82, 14, 8
    grid = []
    for i in range(nz + 1):
        row = []
        for j in range(nx + 1):
            u, v = j / nx, i / nz
            bulge = 0.025 * math.sin(math.pi * u) * math.sin(math.pi * v)
            row.append(Vector(((u - 0.5) * w, 0.002 - bulge, (v - 0.5) * h)))
        grid.append(row)
    t.sheet(grid, 0.006, lambda i, j: "prop_plastic_white", normal=Vector((0, -1, 0)),
            uv_fn=lambda i, j: (j / nx, i / nz), decal="prop_sign_tarp")
    for sx in (-1, 1):
        for sz in (-1, 1):
            c = Vector((sx * 0.67, 0.0, sz * 0.38))
            t.tube(catmull([c, c + Vector((sx * 0.05, 0.01, sz * 0.01)), Vector((sx * 0.735, 0.08, sz * 0.40)),
                            Vector((sx * 0.735, 0.35, sz * 0.40))], per=4), 0.011, "prop_rope", sides=6)
    t.finish(col, bevel=0.003)
    return col


# ------------------------------------------------------------------ review scene

def pier_face_x(side, z):
    """The true inner-face x of a live pier leg at height z, from the LRT kit's own pier: legs of
    half width 0.72 (0.70 + 0.02) tapering 5 per cent from z 0.05 to 7.25, each leaning by
    random.Random(1) as author_ilalim_lrt.pier() draws it."""
    rng = random.Random(1)
    leans = {}
    for s in (-1, 1):
        leans[s] = (rng.uniform(-0.03, 0.03), rng.uniform(-0.03, 0.03))
    t = (z - 0.05) / (L.CAP_TOP - 0.55 - 0.05)
    half = (L.PIER_HALF + 0.02) * (1 - 0.05 * t)
    return side * L.PIER_X + leans[side][0] * t - side * half


def review_placement(signs, parent):
    col = collection("review placement (column signs)", parent)
    spots = [("prop_bawal_umihi_a", -1, -10.0, 1.30, 90), ("prop_bawal_umihi_b", 1, 10.0, 1.30, -90),
             ("prop_barangay_tarp", -1, 19.0, 1.75, 90)]
    for name, side, y, z, yaw in spots:
        src = bpy.data.objects[name]
        o = src.copy()
        # The sign's back is at local y = +0.008; set it 6 mm inside the leg's face.
        o.location = (pier_face_x(side, z) - side * 0.002, y, z)
        o.rotation_euler = (0, 0, math.radians(yaw))
        col.objects.link(o)


def stand_ins(parent):
    col = collection("review stand-ins", parent)

    def slab(name, x0, x1, y0, y1, top, colour, depth=0.4):
        m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
        if m.node_tree is None:
            m.use_nodes = True
        m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*colour, 1)
        m.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.9
        me = bpy.data.meshes.new(name)
        bm = bmesh.new()
        bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation(((x0 + x1) / 2, (y0 + y1) / 2, top - depth / 2))
                              @ Matrix.Diagonal((x1 - x0, y1 - y0, depth, 1)))
        bm.to_mesh(me)
        bm.free()
        me.materials.append(m)
        col.objects.link(bpy.data.objects.new(name, me))

    slab("stand-in road", -6.65, 6.65, -120, 120, 0.0, (0.25, 0.25, 0.26))
    for s in (-1, 1):
        slab("stand-in kerb", *sorted((s * 6.65, s * 7.0)), -120, 120, 0.150, (0.9, 0.9, 0.88))
        slab("stand-in pavement", *sorted((s * 7.0, s * 11.0)), -120, 120, PT, (0.60, 0.57, 0.52))
        slab("stand-in chalk", -7, 7, s * 7 - 0.06, s * 7 + 0.06, 0.004, (0.92, 0.92, 0.9), depth=0.02)
    slab("stand-in shopfront", 11.0, 11.6, -40, 40, 4.6, (0.80, 0.74, 0.64), depth=4.6)
    slab("stand-in lawn", -40, -11.0, -40, 40, 0.24, (0.42, 0.52, 0.34))
    slab("stand-in fence base", -11.3, -11.02, -40, 40, 0.55, (0.78, 0.76, 0.70), depth=0.34)
    for k in range(33):
        y = -40 + k * 2.5
        slab("stand-in fence post", -11.22, -11.1, y - 0.06, y + 0.06, 2.0, (0.2, 0.26, 0.22), depth=1.5)
    for z in (1.0, 1.9):
        slab("stand-in fence rail", -11.2, -11.12, -40, 40, z, (0.2, 0.26, 0.22), depth=0.06)
    fig = bpy.data.meshes.new("figure")
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=12, radius1=0.25, radius2=0.25, depth=1.7,
                          matrix=Matrix.Translation((7.6, 2.5, PT + 0.85)))
    bm.to_mesh(fig)
    bm.free()
    fm = bpy.data.materials.new("figure")
    fm.use_nodes = True
    fm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.85, 0.35, 0.55, 1)
    fig.materials.append(fm)
    col.objects.link(bpy.data.objects.new("1.7 m figure", fig))
    lrt = SOURCE / "lrt_kit.blend"
    if lrt.exists():
        with bpy.data.libraries.load(str(lrt), link=True, relative=True) as (src, dst):
            dst.collections = [c for c in src.collections if c == "guideway over the court"]
        if dst.collections:
            e = bpy.data.objects.new("LRT (linked)", None)
            e.instance_type, e.instance_collection = "COLLECTION", dst.collections[0]
            col.objects.link(e)


def preview(version, only=None):
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 1000
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    cam.data.clip_end = 2000
    scene.collection.objects.link(cam)
    scene.camera = cam
    eye = 18 / math.tan(math.radians(95 / 2))
    shots = [
        ("eye_east_north", (-3.0, 1.0, 1.25), (10, 9.5, 1.3), eye),
        ("eye_east_south", (-2.0, 2.0, 1.25), (9.5, -6, 1.2), eye),
        ("eye_west_north", (3.0, 4.0, 1.25), (-10.5, 13.0, 1.4), eye),
        ("eye_west_south", (2.5, -1.0, 1.25), (-9.5, -12.5, 1.8), eye),
        ("pisonet_close", (6.0, 7.6, 1.75), (9.8, 11.8, 1.2), 22),
        ("cord_close", (7.3, 9.3, 1.3), (8.7, 11.6, 0.2), 26),
        ("pares_close", (6.1, -2.4, 1.7), (8.9, -5.0, 1.2), 22),
        ("aboard_close", (6.6, -3.2, 1.2), (8.05, -3.75, 0.7), 34),
        ("pad_close", (6.8, 3.4, 1.8), (9.0, 5.5, 0.2), 24),
        ("hoop_close", (-5.6, -8.0, 2.3), (-9.2, -10.0, 2.9), 24),
        ("stalls_north", (-7.2, 10.0, 1.7), (-10.4, 13.6, 1.1), 20),
        ("sarisari_close", (-7.6, -12.0, 1.6), (-10.4, -14.2, 1.1), 24),
        ("signs_west", (-1.2, -9.0, 1.4), (-3.74, -10.0, 1.3), 30),
        ("signs_east", (1.2, 9.0, 1.4), (3.74, 10.0, 1.3), 30),
        ("tarp_close", (-1.0, 17.6, 1.6), (-3.74, 19.0, 1.75), 30),
        ("aerial_east", (15.0, -14.0, 20.0), (9.5, 3.0, 0.3), 24),
        ("aerial_west", (-24.0, -10.0, 15.0), (-9.0, 2.0, 0.5), 22),
        ("aerial_plan", (0.0, -4.0, 46.0), (0, 0.0, 0), 22),
    ]
    for name, pos, tgt, lens in shots:
        if only and name not in only:
            continue
        pos, tgt = Vector(pos), Vector(tgt)
        cam.location, cam.data.lens = pos, lens
        cam.rotation_euler = (tgt - pos).to_track_quat("-Z", "Y").to_euler()
        path = PREVIEWS / f"prop_{name}_v{version}.png"
        if path.exists():
            print("[ilalim-props] exists, skipped", path)
            continue
        scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        print("[ilalim-props] preview", path)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
    only = argv[argv.index("--only") + 1].split(",") if "--only" in argv else None
    bpy.ops.wm.read_factory_settings(use_empty=True)
    placed = collection("props (placed)")
    pisonet(placed)
    pares(placed)
    overclock_pad(placed)
    bridge_hoop(placed)
    stall_fruit(placed)
    stall_fishball(placed)
    stall_sarisari(placed)
    clutter(placed)
    signs = column_signs(bpy.context.scene.collection)
    signs.hide_render = True
    review = collection("review")
    review_placement(signs, review)
    stand_ins(review)
    L.lighting()
    SOURCE.mkdir(parents=True, exist_ok=True)
    out = SOURCE / "props.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(out), compress=True, relative_remap=True)
    backup = SOURCE / "props.blend1"
    if backup.exists():
        backup.unlink()
    tris = sum(len(o.data.polygons) for o in placed.all_objects if o.type == "MESH")
    print(f"[ilalim-props] saved {out}; {len(list(placed.all_objects))} objects, {tris} faces before bevel")
    if version:
        preview(version, only)


if __name__ == "__main__":
    main()
