"""Makes ArtSource/payo/payo_land_<tag>.blend and its pictures from Logs/payo/terrain/land_<tag>.npz (tools/payo_land.py).

  blender -b --python tools/payo_land_blend.py -- <tag> [--views=a,b]

Blender only; nothing for the game yet. The court and the gate are plain stand-ins.
"""
import math
import os
import sys

import bpy
import numpy as np
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
TAG = next((a for a in args if not a.startswith("--")), "v16")
ONLY = next((a.split("=", 1)[1].split(",") for a in args if a.startswith("--views=")), None)
OUT = os.path.join(ROOT, "Logs", "payo", "terrain")
data = np.load(os.path.join(OUT, "land_%s.npz" % TAG))
verts, loops, totals, lcol, water = data["verts"], data["loops"], data["totals"], data["lcol"], data["water"]

bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene


def material(name, colour=None, vertex=False, glow=0.0, rough=1.0):
    m = bpy.data.materials.new(name); m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = rough
    if vertex:
        n = m.node_tree.nodes.new("ShaderNodeVertexColor"); n.layer_name = "Col"
        m.node_tree.links.new(n.outputs["Color"], bsdf.inputs["Base Color"])
    else:
        bsdf.inputs["Base Color"].default_value = (*[v ** 2.2 for v in colour], 1)
        if glow:
            bsdf.inputs["Emission Color"].default_value = (*[v ** 2.2 for v in colour], 1)
            bsdf.inputs["Emission Strength"].default_value = glow
    return m


# ---- the land
# The land in two objects on the same points: the slopes (smooth all over, so the mountains' folds read as soft forms,
# not facets) and the terraces and other cut works (sharp at their walls and edges).
TEX = os.path.join(ROOT, "Logs", "payo", "pieces", "build", "terrace", "tex")


def image(name, folder=TEX):
    return bpy.data.images.get(name) or bpy.data.images.load(os.path.join(folder, name))


# The CALM land paint (tools/payo_land_tex.py, Logs/payo/pieces/CALM_STYLE.md): per surface a soft colour multiplier
# laid over the face's own colour and a height map for relief. Metres per tile (x, y, z) and relief depth, as
# payo_land_tex.py painted them. The terrace walls instead show the land kit's grey dry stone (bank_color.png).
CALM = os.path.join(ROOT, "Logs", "payo", "terrain", "tex_calm")
CALM_TEX = {"grass": (20.0, 0.12), "bank": ((12.0, 12.0, 5.0), 0.097), "paddy": (12.0, 0.02), "flooded": (14.0, 0.025),
            "rock": (16.0, 0.22), "paving": (10.0, 0.05), "water": (24.0, 0.10), "foam": (10.0, 0.10), "macro": (96.0, 0.0)}
WALL_C = (0.40, 0.55, 0.31)          # payo_land.py's WALL: the stones take its brightness, not its green
# the paddy rows (paddy_rows.png: each point's distance to the nearest terrace wall), its box x0 y0 x1 y1 and range
CALM_ROWS = [float(v) for v in open(os.path.join(CALM, "paddy_rows.txt")).read().split()]
ROW_PITCH, ROW_AMP = 1.2, 0.08       # metres between painted rice rows; how much lighter a row's crest is than its furrow


sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import ilalim_antitile  # noqa: E402  (the house anti-tiling: Kanto's roofs, Lagoon's rocks, Ilalim's grounds)

# which pictures may be ROTATED by the anti-tiling: rows, courses and slab joints may only be offset and feathered
# (ILALIM_REWORK_GUIDE: rotating a directional picture turns its lines diagonal)
CALM_ROTATE = {"grass": True, "rock": True, "flooded": True, "water": True, "foam": True,
               "bank": False, "paddy": False, "paving": False}
# the hue drift's two tints per surface (sRGB multipliers): warm and cool, a few degrees of hue apart
CALM_HUE = {"grass": ((1.10, 1.07, 0.84), (0.90, 1.0, 1.12)), "bank": ((1.06, 1.01, 0.92), (0.95, 0.99, 1.06)),
            "paddy": ((1.08, 1.02, 0.84), (0.94, 1.04, 0.98)), "rock": ((1.07, 1.01, 0.91), (0.94, 0.99, 1.07)),
            "paving": ((1.05, 1.01, 0.93), (0.96, 0.99, 1.05)), "flooded": ((1.02, 1.0, 0.96), (0.97, 1.0, 1.04))}
CALM_FAR = {"bank": (80.0, 240.0)}   # metres over which the painted shapes fade to the plain colour (walls sooner)


def calm_paint(m, surface, base):
    """Paints `surface` onto material m: base colour (a socket, or an sRGB colour) times the surface's colour map times
    one very large soft drift, and its height as a Bump. Box projection from object coordinates: walls take it from the
    side, floors from above, no UVs needed. (A tangent-space normal map has no tangents to work with here; the Bump reads
    the same height map the game's normal maps were baked from.)
    "bank" is the terrace wall: one picture a 5 m terrace rise, so its cap stones sit on every terrace's top edge.
    Every picture, colour and height alike, goes through the house anti-tiling (tools/ilalim_antitile.py: two more
    samples, rotated 37 and -71 degrees, scaled 0.61 and 1.43, offset, each laid over through a big feathered noise
    mask); directional pictures are only offset. The painted shapes fade out over CALM_FAR."""
    nt = m.node_tree; bsdf = nt.nodes["Principled BSDF"]
    co = nt.nodes.new("ShaderNodeTexCoord")
    feathers = {}

    def node(kind, **inputs):
        n = nt.nodes.new(kind)
        for k, v in inputs.items():
            n.inputs[k].default_value = v
        return n

    def sample(tex, metres, srgb=False, level=False, variant=None):
        """The picture box-projected; with `level` from the two sides only (along x or y, up z), so a wall's courses
        run level even where the wall leans far back, and a narrow cross-fade where the wall turns past 45 degrees.
        `variant` = (degrees, scale, (dx, dy)) moves the picture for the anti-tiling. Returns {"Color", "Alpha"}."""
        mp = node("ShaderNodeMapping", Scale=[1 / v for v in (metres if isinstance(metres, tuple) else (metres,) * 3)])
        nt.links.new(co.outputs["Object"], mp.inputs["Vector"])
        vec = mp.outputs["Vector"]
        if variant:
            deg, sc, off = variant
            mv = node("ShaderNodeMapping", Rotation=(0, 0, math.radians(deg)), Scale=(sc, sc, 1 if level else sc),
                      Location=(off[0], off[1], 0))                  # never along z: the caps stay on the terrace tops
            nt.links.new(vec, mv.inputs["Vector"]); vec = mv.outputs["Vector"]

        def picture(vector, projection):
            im = nt.nodes.new("ShaderNodeTexImage"); im.image = image(tex, CALM)
            im.image.colorspace_settings.name = "sRGB" if srgb else "Non-Color"
            im.image.alpha_mode = "CHANNEL_PACKED"
            im.projection = projection; im.projection_blend = 0.35; im.interpolation = "Cubic"
            nt.links.new(vector, im.inputs["Vector"])
            return im
        if not level:
            im = picture(vec, "BOX")
            return {"Color": im.outputs["Color"], "Alpha": im.outputs["Alpha"]}
        sep = nt.nodes.new("ShaderNodeSeparateXYZ"); nt.links.new(vec, sep.inputs[0])
        side = []
        for along in ("X", "Y"):
            cb = nt.nodes.new("ShaderNodeCombineXYZ")
            nt.links.new(sep.outputs[along], cb.inputs["X"]); nt.links.new(sep.outputs["Z"], cb.inputs["Y"])
            side.append(picture(cb.outputs[0], "FLAT"))
        if "side" not in feathers:
            geo = nt.nodes.new("ShaderNodeNewGeometry"); ns = nt.nodes.new("ShaderNodeSeparateXYZ")
            nt.links.new(geo.outputs["Normal"], ns.inputs[0])
            ax, ay = (nt.nodes.new("ShaderNodeMath") for _ in range(2))
            ax.operation = ay.operation = "ABSOLUTE"
            nt.links.new(ns.outputs["X"], ax.inputs[0]); nt.links.new(ns.outputs["Y"], ay.inputs[0])
            dif = nt.nodes.new("ShaderNodeMath"); dif.operation = "SUBTRACT"
            nt.links.new(ax.outputs[0], dif.inputs[0]); nt.links.new(ay.outputs[0], dif.inputs[1])
            fac = node("ShaderNodeMapRange", **{"From Min": -0.12, "From Max": 0.12}); fac.interpolation_type = "SMOOTHSTEP"
            nt.links.new(dif.outputs[0], fac.inputs["Value"])     # 1 where the wall faces along x: its picture runs along y
            feathers["side"] = fac.outputs["Result"]
        return blend(feathers["side"], {"Color": side[0].outputs["Color"], "Alpha": side[0].outputs["Alpha"]},
                     {"Color": side[1].outputs["Color"], "Alpha": side[1].outputs["Alpha"]})

    def blend(factor, a, b):
        mc = nt.nodes.new("ShaderNodeMix"); mc.data_type = "RGBA"
        ma = nt.nodes.new("ShaderNodeMix"); ma.data_type = "FLOAT"
        for mx, out, i0, i1 in ((mc, "Color", 6, 7), (ma, "Alpha", 2, 3)):
            nt.links.new(factor, mx.inputs[0])
            nt.links.new(a[out], mx.inputs[i0]); nt.links.new(b[out], mx.inputs[i1])
        return {"Color": mc.outputs[2], "Alpha": ma.outputs[0]}

    def feather(k, metres, mask_scale, seed):
        """The k-th anti-tiling mask: a big soft noise field (in tiles of this surface), feathered 0.42..0.58; for
        pictures with joints (walls, slabs) a narrower 0.47..0.53, so two offset stone layouts never ghost together."""
        if k not in feathers:
            mp = node("ShaderNodeMapping", Scale=[1 / v for v in (metres if isinstance(metres, tuple) else (metres,) * 3)],
                      Location=seed)
            nt.links.new(co.outputs["Object"], mp.inputs["Vector"])
            nz = node("ShaderNodeTexNoise", Scale=mask_scale, Detail=0.0)
            nt.links.new(mp.outputs["Vector"], nz.inputs["Vector"])
            w = 0.08 if rotate else 0.03
            fr = node("ShaderNodeMapRange", **{"From Min": 0.5 - w, "From Max": 0.5 + w}); fr.interpolation_type = "SMOOTHSTEP"
            nt.links.new(nz.outputs["Fac"], fr.inputs["Value"])
            feathers[k] = fr.outputs["Result"]
        return feathers[k]

    def antitiled(tex, metres, rotate, srgb=False, level=False):
        out = sample(tex, metres, srgb, level)
        for k, (deg, sc, off, mask_scale, seed) in enumerate(ilalim_antitile.SAMPLES):
            variant = (deg, sc, off) if rotate else (0.0, 1.0, off)
            out = blend(feather(k, metres, mask_scale, seed), out, sample(tex, metres, srgb, level, variant))
        return out

    def far_fade():
        if "far" not in feathers:
            cam = nt.nodes.new("ShaderNodeCameraData")
            near_m, far_m = CALM_FAR.get(surface, (120.0, 320.0))
            fade = node("ShaderNodeMapRange", **{"From Min": near_m, "From Max": far_m, "To Min": 1.0, "To Max": 0.0})
            nt.links.new(cam.outputs["View Distance"], fade.inputs["Value"])
            feathers["far"] = fade.outputs["Result"]
        return feathers["far"]

    def hue_drift(surface):
        """A slight shift of hue across the plain parts (owner: "some slight offset in hue across the plain parts"):
        two broad feathered world-space fields, tens of metres across, never tiling, lay the surface's warm tint and
        its cool tint over the vertex colour. A colour multiplier, about 1 on average."""
        warm, cool = CALM_HUE[surface]
        out = None
        for k, (metres, seed, tint, lo) in enumerate(((70.0, (3.1, 7.7, 0.0), warm, 0.30), (45.0, (17.3, 2.9, 5.0), cool, 0.35))):
            mp = node("ShaderNodeMapping", Scale=(1 / metres,) * 3, Location=seed)
            nt.links.new(co.outputs["Object"], mp.inputs["Vector"])
            nz = node("ShaderNodeTexNoise", Scale=1.0, Detail=1.0)
            nt.links.new(mp.outputs["Vector"], nz.inputs["Vector"])
            fr = node("ShaderNodeMapRange", **{"From Min": 0.5 - lo / 2, "From Max": 0.5 + lo / 2}); fr.interpolation_type = "SMOOTHSTEP"
            nt.links.new(nz.outputs["Fac"], fr.inputs["Value"])
            mx = nt.nodes.new("ShaderNodeMix"); mx.data_type = "RGBA"
            nt.links.new(fr.outputs["Result"], mx.inputs[0])
            mx.inputs[7].default_value = (*[c ** 2.2 for c in tint], 1)
            if out is None:
                inv = [1 / c for c in tint]                       # the other side of the field leans the other way
                mx.inputs[6].default_value = (*[(c ** 0.5) ** 2.2 for c in inv], 1)
            else:
                nt.links.new(out, mx.inputs[6])
            out = mx.outputs[2]
        return out

    def multiply(colour, factor):
        mul = nt.nodes.new("ShaderNodeMix"); mul.data_type = "RGBA"; mul.blend_type = "MULTIPLY"; mul.inputs[0].default_value = 1.0
        if isinstance(colour, tuple):
            mul.inputs[6].default_value = (*[v ** 2.2 for v in colour], 1)
        else:
            nt.links.new(colour, mul.inputs[6])
        nt.links.new(factor, mul.inputs[7])
        return mul.outputs[2]

    tile, depth = CALM_TEX[surface]
    rotate = CALM_ROTATE[surface]
    level = surface == "bank"
    if level:
        # the stone picture at the wall's own brightness (the vertex colour's luminance over WALL's); the grass
        # (alpha 0) keeps the vertex colour. Far off, the picture gives way to its own average: stone over grass.
        im = antitiled("bank_color.png", tile, rotate, srgb=True, level=True)
        bw = nt.nodes.new("ShaderNodeRGBToBW"); nt.links.new(base, bw.inputs["Color"])
        ratio = nt.nodes.new("ShaderNodeMath"); ratio.operation = "DIVIDE"
        ratio.inputs[1].default_value = 0.2126 * WALL_C[0] ** 2.2 + 0.7152 * WALL_C[1] ** 2.2 + 0.0722 * WALL_C[2] ** 2.2
        nt.links.new(bw.outputs["Val"], ratio.inputs[0])
        avg = [float(v) for v in open(os.path.join(CALM, "bank_mean.txt")).read().split()]   # sRGB stone r g b, cover
        rgb = node("ShaderNodeRGB"); rgb.outputs[0].default_value = (*[v ** 2.2 for v in avg[:3]], 1)
        cov = node("ShaderNodeValue"); cov.outputs[0].default_value = avg[3]
        near = blend(far_fade(), {"Color": rgb.outputs[0], "Alpha": cov.outputs[0]}, im)
        stone = nt.nodes.new("ShaderNodeVectorMath"); stone.operation = "SCALE"
        nt.links.new(near["Color"], stone.inputs[0]); nt.links.new(ratio.outputs[0], stone.inputs["Scale"])
        # the grass: the vertex colour, darker toward a hanging tongue's lower edge (bank_shade.png)
        sh = antitiled("bank_shade.png", tile, rotate, level=True)
        sg = nt.nodes.new("ShaderNodeGamma"); sg.inputs["Gamma"].default_value = 2.2
        nt.links.new(sh["Color"], sg.inputs["Color"])
        mix = nt.nodes.new("ShaderNodeMix"); mix.data_type = "RGBA"
        nt.links.new(near["Alpha"], mix.inputs[0]); nt.links.new(multiply(base, sg.outputs["Color"]), mix.inputs[6])
        nt.links.new(stone.outputs["Vector"], mix.inputs[7])
        colour = mix.outputs[2]
        layers = ["macro"]
    else:
        colour = base
        layers = [surface, "macro"]
    if surface in CALM_HUE:
        colour = multiply(colour, hue_drift(surface))
    rows = None
    if surface == "paddy":
        colour, rows = paddy_rows(nt, co, colour)
    if surface in ("paddy", "flooded"):
        colour, rows = mud_and_rice(nt, co, bsdf, surface, base, colour, rows)
    for s in layers:
        x2 = nt.nodes.new("ShaderNodeVectorMath"); x2.operation = "SCALE"; x2.inputs["Scale"].default_value = 2.0   # stored at half
        pic = sample("%s_value.png" % s, CALM_TEX[s][0]) if s == "macro" else antitiled("%s_value.png" % s, tile, rotate)
        nt.links.new(pic["Color"], x2.inputs[0])
        # the map is a multiplier on the colour as SEEN (sRGB), the shader works in linear: raise it to 2.2
        ga = nt.nodes.new("ShaderNodeGamma"); ga.inputs["Gamma"].default_value = 2.2
        nt.links.new(x2.outputs["Vector"], ga.inputs["Color"])
        paint = ga.outputs["Color"]
        if s != "macro":                   # the painted shapes fade out far off, so far slopes never turn to grain
            fm = nt.nodes.new("ShaderNodeMix"); fm.data_type = "RGBA"; fm.inputs[6].default_value = (1, 1, 1, 1)
            nt.links.new(far_fade(), fm.inputs[0]); nt.links.new(paint, fm.inputs[7])
            paint = fm.outputs[2]
        colour = multiply(colour, paint)
    nt.links.new(colour, bsdf.inputs["Base Color"])
    bump = nt.nodes.new("ShaderNodeBump"); bump.inputs["Distance"].default_value = depth
    nt.links.new(antitiled("%s_height.png" % surface, tile, rotate, level=level)["Color"], bump.inputs["Height"])
    if surface in ("paddy", "flooded"):    # the mud and water lie flat: the relief only where rice stands
        nt.links.new(RICE_OUT[0], bump.inputs["Strength"])
    if rows is not None:                   # the rows' own relief on top, inside the rice only
        b2 = nt.nodes.new("ShaderNodeBump"); b2.inputs["Distance"].default_value = 0.05
        nt.links.new(rows, b2.inputs["Height"]); nt.links.new(bump.outputs["Normal"], b2.inputs["Normal"])
        bump = b2
    nt.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    return m


# The paddies as real Ifugao paddies (owner: "muddy brown with green patches"): wet earth and shallow brown-olive water
# as the base, rice standing in soft irregular patches from rice_density.png (tools/payo_land_tex.py: each field its own
# planting, the same picture the Unity blade pass scatters from). The ripe gold fields keep their colour.
CALM_RICE = [float(v) for v in open(os.path.join(CALM, "rice_density.txt")).read().split("\n")[0].split()]
RICE_OUT = [None]                    # the last paddy material's rice socket (its height relief stays in the rice)
MUD, MUD_WATER, YOUNG_RICE = (0.42, 0.34, 0.24), (0.44, 0.42, 0.29), (0.46, 0.70, 0.27)
PADDY_LUM = 0.33                     # the mean PADDY green's luminance (linear): the mud follows each field's own value


def mud_and_rice(nt, co, bsdf, surface, base, green, rows):
    """Mixes mud (wet earth and brown-olive water in big soft patches, glossy so it holds the sky) with `green` (the
    painted rice) by the rice density; ripe gold fields (red over green in the vertex colour) stay rice. A flooded
    paddy is water: the vertex colour's sky tinted brown, with a little young rice. Returns colour and the rows inside
    the rice."""
    def node(kind, **inputs):
        n = nt.nodes.new(kind)
        for k, v in inputs.items():
            n.inputs[k].default_value = v
        return n

    def op(kind, a, b=None, clamp=False):
        n = nt.nodes.new("ShaderNodeMath"); n.operation = kind; n.use_clamp = clamp
        for i, v in enumerate((a, b)):
            if v is None:
                continue
            if isinstance(v, (int, float)):
                n.inputs[i].default_value = v
            else:
                nt.links.new(v, n.inputs[i])
        return n.outputs[0]

    def mix(fac, a, b):
        m = nt.nodes.new("ShaderNodeMix"); m.data_type = "RGBA"
        for i, v in ((0, fac), (6, a), (7, b)):
            if isinstance(v, (int, float)):
                m.inputs[i].default_value = v
            elif isinstance(v, tuple):
                m.inputs[i].default_value = (*[c ** 2.2 for c in v], 1)
            else:
                nt.links.new(v, m.inputs[i])
        return m.outputs[2]

    x0, y0, x1, y1 = CALM_RICE[:4]
    mp = node("ShaderNodeMapping", Scale=(1 / (x1 - x0), 1 / (y1 - y0), 1), Location=(-x0 / (x1 - x0), -y0 / (y1 - y0), 0))
    nt.links.new(co.outputs["Object"], mp.inputs["Vector"])
    im = nt.nodes.new("ShaderNodeTexImage"); im.image = image("rice_density.png", CALM)
    im.image.colorspace_settings.name = "Non-Color"; im.extension = "EXTEND"; im.interpolation = "Cubic"
    nt.links.new(mp.outputs["Vector"], im.inputs["Vector"])
    rgb = nt.nodes.new("ShaderNodeSeparateColor"); nt.links.new(base, rgb.inputs["Color"])
    gold = node("ShaderNodeMapRange", **{"From Min": 0.85, "From Max": 1.05}); gold.interpolation_type = "SMOOTHSTEP"
    nt.links.new(op("DIVIDE", rgb.outputs[0], op("MAXIMUM", rgb.outputs[1], 0.001)), gold.inputs["Value"])
    rice = op("MAXIMUM", im.outputs["Color"], gold.outputs["Result"]) if surface == "paddy" else im.outputs["Color"]
    # the mud: wet earth and brown-olive water in soft world-space patches (18 m), at the field's own brightness
    nz = node("ShaderNodeTexNoise", Scale=1 / 18.0, Detail=1.0)
    nt.links.new(co.outputs["Object"], nz.inputs["Vector"])
    wet = node("ShaderNodeMapRange", **{"From Min": 0.40, "From Max": 0.60}); wet.interpolation_type = "SMOOTHSTEP"
    nt.links.new(nz.outputs["Fac"], wet.inputs["Value"])
    mud = mix(wet.outputs["Result"], MUD, MUD_WATER)
    lum = nt.nodes.new("ShaderNodeRGBToBW"); nt.links.new(base, lum.inputs["Color"])
    k = op("MINIMUM", op("MAXIMUM", op("DIVIDE", lum.outputs["Val"], PADDY_LUM), 0.8), 1.3)
    sc = nt.nodes.new("ShaderNodeVectorMath"); sc.operation = "SCALE"
    nt.links.new(mud, sc.inputs[0]); nt.links.new(k, sc.inputs["Scale"])
    mud = sc.outputs["Vector"]
    if surface == "flooded":             # standing water: the sky (vertex colour) seen through brown water
        mud = mix(0.45, base, MUD_WATER)
        young = nt.nodes.new("ShaderNodeVectorMath"); young.operation = "SCALE"
        young.inputs[0].default_value = [c ** 2.2 for c in YOUNG_RICE]; nt.links.new(k, young.inputs["Scale"])
        green = young.outputs["Vector"]
    colour = mix(rice, mud, green)
    # glossy where it is water and mud, matte where the rice stands
    rough = node("ShaderNodeMapRange", **{"To Min": 0.12 if surface == "flooded" else 0.55, "To Max": 0.95})
    nt.links.new(rice, rough.inputs["Value"]); nt.links.new(rough.outputs["Result"], bsdf.inputs["Roughness"])
    if rows is not None:
        rows = op("MULTIPLY", rows, rice)
    RICE_OUT[0] = rice
    return colour, rows


def paddy_rows(nt, co, colour):
    """Rice rows painted along each field: stripes of the distance to the nearest terrace wall (so they run parallel to
    the field's own walls and bend with it), the crest a little lighter, the furrow a little darker, the edge crisp
    like a brush stroke, the line wandering slowly. Faded out from 35 to 110 m so they never turn to stripes or moire
    in the far views. Returns the new colour and the row height (0..1) for the Bump."""
    x0, y0, x1, y1, dmax = CALM_ROWS
    mp = nt.nodes.new("ShaderNodeMapping")
    mp.inputs["Scale"].default_value = (1 / (x1 - x0), 1 / (y1 - y0), 1)
    mp.inputs["Location"].default_value = (-x0 / (x1 - x0), -y0 / (y1 - y0), 0)
    nt.links.new(co.outputs["Object"], mp.inputs["Vector"])
    im = nt.nodes.new("ShaderNodeTexImage"); im.image = image("paddy_rows.png", CALM)
    im.image.colorspace_settings.name = "Non-Color"; im.extension = "EXTEND"; im.interpolation = "Linear"
    nt.links.new(mp.outputs["Vector"], im.inputs["Vector"])
    sep = nt.nodes.new("ShaderNodeSeparateColor"); nt.links.new(im.outputs["Color"], sep.inputs["Color"])
    wob = nt.nodes.new("ShaderNodeTexNoise"); wob.inputs["Scale"].default_value = 0.06; wob.inputs["Detail"].default_value = 0.0
    nt.links.new(co.outputs["Object"], wob.inputs["Vector"])

    def op(kind, a, b=None):
        n = nt.nodes.new("ShaderNodeMath"); n.operation = kind
        for i, v in enumerate((a, b)):
            if v is None:
                continue
            if isinstance(v, (int, float)):
                n.inputs[i].default_value = v
            else:
                nt.links.new(v, n.inputs[i])
        return n.outputs[0]

    phase = op("ADD", op("MULTIPLY", sep.outputs[0], dmax * 2 * math.pi / ROW_PITCH), op("MULTIPLY", wob.outputs["Fac"], 2.5))
    wave = op("COSINE", phase)
    crisp = nt.nodes.new("ShaderNodeMapRange"); crisp.interpolation_type = "SMOOTHSTEP"
    crisp.inputs["From Min"].default_value = 0.15; crisp.inputs["From Max"].default_value = 0.55   # a narrow lit crest
    nt.links.new(wave, crisp.inputs["Value"])
    cam = nt.nodes.new("ShaderNodeCameraData")
    fade = nt.nodes.new("ShaderNodeMapRange")
    fade.inputs["From Min"].default_value = 35.0; fade.inputs["From Max"].default_value = 110.0
    fade.inputs["To Min"].default_value = 1.0; fade.inputs["To Max"].default_value = 0.0
    nt.links.new(cam.outputs["View Distance"], fade.inputs["Value"])
    rows = op("MULTIPLY", crisp.outputs["Result"], fade.outputs["Result"])
    # value: 1 - amp .. 1 + amp, as seen; raised to 2.2 for the linear shader
    val = op("POWER", op("ADD", 1.0, op("MULTIPLY", op("SUBTRACT", rows, op("MULTIPLY", fade.outputs["Result"], 0.3)), 2 * ROW_AMP)), 2.2)
    mul = nt.nodes.new("ShaderNodeMix"); mul.data_type = "RGBA"; mul.blend_type = "MULTIPLY"; mul.inputs[0].default_value = 1.0
    nt.links.new(colour, mul.inputs[6]); nt.links.new(val, mul.inputs[7])
    return mul.outputs[2], rows


def land_material(name, tex, scale, mode="detail", strength=0.45, tint=(1, 1, 1), rough=1.0, calm=None):
    """A land material painted with one of the land kit's own textures, projected from the world so it needs no UVs:
    'detail' keeps the face's own colour and lets the texture's light and dark through; 'paint' shows the texture
    itself, tinted. Box projection, so walls take it from the side and floors from above. With `calm` (a CALM_TEX
    surface) the face's own colour gets that calm paint instead and `tex`, `scale` and `mode` are not used."""
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; bsdf = nt.nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = rough
    if calm:
        vc = nt.nodes.new("ShaderNodeVertexColor"); vc.layer_name = "Col"
        return calm_paint(m, calm, vc.outputs["Color"])
    co = nt.nodes.new("ShaderNodeTexCoord"); mp = nt.nodes.new("ShaderNodeMapping")
    mp.inputs["Scale"].default_value = scale
    nt.links.new(co.outputs["Object"], mp.inputs["Vector"])
    im = nt.nodes.new("ShaderNodeTexImage"); im.image = image(tex); im.projection = "BOX"; im.projection_blend = 0.25
    nt.links.new(mp.outputs["Vector"], im.inputs["Vector"])
    vc = nt.nodes.new("ShaderNodeVertexColor"); vc.layer_name = "Col"
    if mode == "detail":
        # the texture's brightness about its own middle, scaled: 1 - strength .. 1 + strength
        bw = nt.nodes.new("ShaderNodeRGBToBW"); nt.links.new(im.outputs["Color"], bw.inputs["Color"])
        mr = nt.nodes.new("ShaderNodeMapRange")
        mr.inputs["From Min"].default_value = 0.0; mr.inputs["From Max"].default_value = 0.6
        mr.inputs["To Min"].default_value = 1.0 - strength; mr.inputs["To Max"].default_value = 1.0 + strength
        nt.links.new(bw.outputs["Val"], mr.inputs["Value"])
        mul = nt.nodes.new("ShaderNodeMix"); mul.data_type = "RGBA"; mul.blend_type = "MULTIPLY"; mul.inputs[0].default_value = 1.0
        nt.links.new(vc.outputs["Color"], mul.inputs[6]); nt.links.new(mr.outputs["Result"], mul.inputs[7])
        nt.links.new(mul.outputs[2], bsdf.inputs["Base Color"])
    else:
        mul = nt.nodes.new("ShaderNodeMix"); mul.data_type = "RGBA"; mul.blend_type = "MULTIPLY"; mul.inputs[0].default_value = 1.0
        nt.links.new(im.outputs["Color"], mul.inputs[6]); mul.inputs[7].default_value = (*tint, 1)
        nt.links.new(mul.outputs[2], bsdf.inputs["Base Color"])
    return m


LAND_MATS = [land_material("land slope", None, None, calm="grass"),
             land_material("land terrace wall", None, None, calm="bank"),
             land_material("land paddy", None, None, calm="paddy"),
             land_material("land rock", None, None, calm="rock"),
             land_material("land paving", None, None, calm="paving"),
             land_material("land flooded paddy", None, None, rough=0.12, calm="flooded")]
mat_ids = data["mat"] if "mat" in data else np.zeros(len(totals), np.int8)
starts = np.concatenate([[0], np.cumsum(totals)[:-1]]).astype(np.int64)
n_plain = int(data["n_plain"]) if "n_plain" in data else len(totals)


def land_part(name, p0, p1, sharp):
    l0, l1 = int(starts[p0]), int(starts[p1]) if p1 < len(totals) else len(loops)
    me = bpy.data.meshes.new(name)
    me.vertices.add(len(verts)); me.vertices.foreach_set("co", verts.ravel())
    me.loops.add(l1 - l0); me.loops.foreach_set("vertex_index", loops[l0:l1])
    me.polygons.add(p1 - p0)
    me.polygons.foreach_set("loop_start", (starts[p0:p1] - l0).astype(np.int32))
    me.polygons.foreach_set("loop_total", totals[p0:p1])
    me.update(); me.validate()
    ca = me.color_attributes.new("Col", "FLOAT_COLOR", "CORNER")
    rgba = np.concatenate([np.clip(lcol[l0:l1], 0, 1) ** 2.2, np.ones((l1 - l0, 1), np.float32)], axis=1).astype(np.float32)
    ca.data.foreach_set("color", rgba.ravel())
    me.shade_smooth()
    if sharp:
        me.set_sharp_from_angle(angle=math.radians(sharp))
    for lm in LAND_MATS:
        me.materials.append(lm)
    me.polygons.foreach_set("material_index", mat_ids[p0:p1].astype(np.int32))
    ob = bpy.data.objects.new(name, me); sc.collection.objects.link(ob)
    return ob


land = land_part("Land", 0, n_plain, 0)
land_part("Land terraces", n_plain, len(totals), 30)

# ---- the river: a ribbon on its line
wv, wf = [], []
for k in range(len(water)):
    x, y, z = water[k]
    if k + 1 < len(water):
        dx, dy = water[k + 1][0] - x, water[k + 1][1] - y
    nlen = math.hypot(dx, dy) or 1.0
    nx, ny = -dy / nlen, dx / nlen
    wv += [(x - nx * 9, y - ny * 9, z), (x + nx * 9, y + ny * 9, z)]
    if k:
        a = 2 * (k - 1)
        wf.append((a, a + 1, a + 3, a + 2))
wm = bpy.data.meshes.new("river"); wm.from_pydata(wv, [], wf); wm.update()
wm.materials.append(calm_paint(material("river", (0.56, 0.74, 0.82), glow=0.25, rough=0.15), "water", (0.56, 0.74, 0.82)))
# where the river runs down a terrace wall it is white water: one pale foam colour on those stretches, read as rapids
wm.materials.append(calm_paint(material("river rapids", (0.86, 0.94, 0.96), glow=0.35, rough=0.4), "foam", (0.86, 0.94, 0.96)))
for k, poly in enumerate(wm.polygons):
    if water[k][2] - water[k + 1][2] > 0.4:
        poly.material_index = 1
sc.collection.objects.link(bpy.data.objects.new("River", wm))


# ---- the water stair from the head's basin down to the gate, and the basin's water
if "stair" in data:
    st = data["stair"]
    sv, sf = [], []
    for k in range(len(st)):
        x, y, z = st[k]
        sv += [(x - 3.2, y, z), (x + 3.2, y, z)]
        if k:
            a = 2 * (k - 1)
            sf.append((a, a + 1, a + 3, a + 2))
    sm = bpy.data.meshes.new("water stair"); sm.from_pydata(sv, [], sf); sm.update()
    sm.materials.append(bpy.data.materials["river"])
    sc.collection.objects.link(bpy.data.objects.new("Water stair", sm))
    bx0, bx1, by0, by1, bfz, bwz = (float(v) for v in data["basin"])
    # the plunge pool is round: its water a disc where the bowl (floor radius 14, rim 20, rising 3 m to the water) meets it
    bpy.ops.mesh.primitive_circle_add(vertices=72, radius=14.0 + 6.0 * (bwz - bfz) / 4.2 + 0.3, fill_type="NGON",
                                      location=((bx0 + bx1) / 2, (by0 + by1) / 2, bwz))
    bo = bpy.context.object; bo.name = "Basin water"
    bo.data.materials.append(bpy.data.materials["river"])

# ---- the sacred pool, and the fall that fills it from the open front of the pit
px, py, _, _, _, pz = (float(v) for v in data["pool"])
bpy.ops.mesh.primitive_circle_add(vertices=96, radius=23.0, fill_type="NGON", location=(px, py, pz))
pool = bpy.context.object; pool.name = "Sacred pool"
pool.data.materials.append(bpy.data.materials["river"])
fv = [(-7, 30.6, -30.2), (7, 30.6, -30.2), (8, 34.0, pz), (-8, 34.0, pz)]
fm = bpy.data.meshes.new("fall"); fm.from_pydata(fv, [], [(0, 1, 2, 3)]); fm.update()
fm.materials.append(material("fall", (0.88, 0.94, 0.98), glow=0.6, rough=0.3))
sc.collection.objects.link(bpy.data.objects.new("The pit's fall (stand-in)", fm))


def box(name, size, at, colour):
    bpy.ops.mesh.primitive_cube_add(size=1, location=at)
    o = bpy.context.object; o.name = name; o.scale = size
    o.data.materials.append(material(name, colour))
    return o


box("Court (44 m play square)", (44, 44, 60), (0, 0, -30), (0.66, 0.66, 0.63))
box("Gate (stand-in)", (18, 5, 15), (0, -54, -6 + 7.5), (0.78, 0.60, 0.36))
box("Person 1.75 m", (0.5, 0.3, 1.75), (7, 4, 0.875), (0.9, 0.3, 0.2))
bpy.ops.mesh.primitive_plane_add(size=16000, location=(0, 4000, float(verts[:, 2].min()) - 4.0))
plain = bpy.context.object; plain.name = "Low ground under everything"
plain.data.materials.append(material("plain", (0.32, 0.42, 0.32)))
bpy.ops.mesh.primitive_uv_sphere_add(radius=150, location=(0, 5200, 520))
sd = bpy.context.object; sd.name = "Setting sun"; sd.data.materials.append(material("sun", (1.0, 0.86, 0.58), glow=9))


def sky_material(name, colour, strength, falloff=None):
    """Unlit paint for the sky's pieces: an emission colour, faded out toward a disc's rim when `falloff` is given."""
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial"); em = nt.nodes.new("ShaderNodeEmission")
    em.inputs["Color"].default_value = (*[v ** 2.2 for v in colour], 1); em.inputs["Strength"].default_value = strength
    if falloff is None:
        nt.links.new(em.outputs[0], out.inputs["Surface"])
        return m
    tc = nt.nodes.new("ShaderNodeTexCoord"); gr = nt.nodes.new("ShaderNodeTexGradient"); gr.gradient_type = "SPHERICAL"
    mp = nt.nodes.new("ShaderNodeMapping"); mp.inputs["Location"].default_value = (0, 0, 0)
    nt.links.new(tc.outputs["Object"], mp.inputs["Vector"]); nt.links.new(mp.outputs["Vector"], gr.inputs["Vector"])
    pw = nt.nodes.new("ShaderNodeMath"); pw.operation = "POWER"; pw.inputs[1].default_value = falloff
    nt.links.new(gr.outputs["Fac"], pw.inputs[0])
    tr = nt.nodes.new("ShaderNodeBsdfTransparent"); mix = nt.nodes.new("ShaderNodeMixShader")
    nt.links.new(pw.outputs[0], mix.inputs[0]); nt.links.new(tr.outputs[0], mix.inputs[1]); nt.links.new(em.outputs[0], mix.inputs[2])
    nt.links.new(mix.outputs[0], out.inputs["Surface"])
    m.blend_method = "BLEND" if hasattr(m, "blend_method") else None
    if hasattr(m, "surface_render_method"):
        m.surface_render_method = "BLENDED"
    return m


# a soft glow round the setting sun (a flat white disc alone read as a cut-out)
bpy.ops.mesh.primitive_plane_add(size=2.0, location=(0, 5150, 520), rotation=(math.radians(90), 0, 0))
halo = bpy.context.object; halo.name = "Sun glow"; halo.scale = (1300, 1300, 1)
halo.data.materials.append(sky_material("sun glow", (1.0, 0.80, 0.55), 1.4, falloff=2.2))
# painted clouds: a few long, flat, soft-edged banks low over the valley's open end, lit from below by the sunset
rng_c = __import__("random").Random(5)
cloud_mat = sky_material("cloud", (1.0, 0.80, 0.70), 1.0)
cloud_shade = sky_material("cloud underside", (0.92, 0.62, 0.58), 1.0)
for k, (cx, cy, cz, length) in enumerate([(-1500, 5600, 760, 1500), (1300, 5900, 980, 1800), (-300, 6400, 1250, 1300),
                                          (2300, 5200, 560, 1000), (-2600, 5000, 620, 1200), (500, 5000, 360, 900)]):
    for j in range(5):
        u = (j - 2) / 2.0
        bpy.ops.mesh.primitive_uv_sphere_add(radius=1.0, segments=24, ring_count=12,
                                             location=(cx + u * length * 0.42, cy + rng_c.uniform(-60, 60), cz + (1 - u * u) * 60 + rng_c.uniform(-10, 25)))
        o = bpy.context.object; o.name = "Cloud %d.%d" % (k, j)
        r = length * (0.30 - 0.08 * abs(u)) * rng_c.uniform(0.85, 1.1)
        o.scale = (r, r * 0.35, r * 0.20)
        o.data.materials.append(cloud_mat if k % 3 else cloud_shade)
        bpy.ops.object.shade_smooth()

# ---- light and sky: a calm sunset
engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
sc.view_settings.view_transform = "Standard"
w = bpy.data.worlds.new("sky"); sc.world = w; w.use_nodes = True
nt = w.node_tree
tc = nt.nodes.new("ShaderNodeTexCoord"); sep = nt.nodes.new("ShaderNodeSeparateXYZ"); ramp = nt.nodes.new("ShaderNodeValToRGB")
nt.links.new(tc.outputs["Generated"], sep.inputs[0]); nt.links.new(sep.outputs["Z"], ramp.inputs["Fac"])
els = ramp.color_ramp.elements
els[0].position = 0.0; els[0].color = (1.0, 0.62, 0.40, 1)
els[1].position = 0.30; els[1].color = (0.42, 0.52, 0.72, 1)
e = els.new(0.08); e.color = (0.98, 0.70, 0.46, 1)
nt.links.new(ramp.outputs["Color"], nt.nodes["Background"].inputs[0])
nt.nodes["Background"].inputs[1].default_value = 0.9
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN")); sc.collection.objects.link(sun)
sun.data.energy = 4.2; sun.data.color = (1.0, 0.80, 0.58); sun.data.angle = math.radians(3.0)
sun.rotation_euler = Vector((0.55, -0.62, -0.42)).normalized().to_track_quat("-Z", "Y").to_euler()   # low, from the west-north-west
vol = nt.nodes.new("ShaderNodeVolumePrincipled")
vol.inputs["Density"].default_value = 0.00055; vol.inputs["Anisotropy"].default_value = 0.55
vol.inputs["Color"].default_value = (1.0, 0.86, 0.74, 1)
# (the volume is left unlinked: at this scale it swallowed the whole frame)
try:
    sc.eevee.volumetric_end = 6000.0; sc.eevee.volumetric_tile_size = "8"; sc.eevee.use_shadows = True
    sc.eevee.volumetric_shadow_samples = 32
except AttributeError:
    pass
sc.render.resolution_x, sc.render.resolution_y = 1280, 720

cams = {}


def camera(name, at, to, lens=28, ortho=None):
    cam = bpy.data.objects.new(name, bpy.data.cameras.new(name)); sc.collection.objects.link(cam)
    cam.location = at
    cam.rotation_euler = (Vector(to) - Vector(at)).to_track_quat("-Z", "Y").to_euler()
    cam.data.lens = lens; cam.data.clip_end = 14000
    if ortho:
        cam.data.type = "ORTHO"; cam.data.ortho_scale = ortho
    cams[name] = cam


camera("top", (0, 900, 2500), (0, 900.01, 0), ortho=3900)
camera("eye_court_front_edge", (0, 20, 1.7), (0, 300, -40), 16)
camera("eye_court_middle", (0, -10, 1.7), (0, 300, 22), 16)
camera("over_the_court", (0, -60, 34), (0, 400, -70), 22)
camera("terraces_close", (-60, 150, -12), (40, 330, -46), 24)
camera("pool_from_the_terraces", (-55, 125, 4), (0, 58, -44), 26)
camera("terraces_from_the_river", (25, 420, -62), (-120, 300, -30), 24)
camera("air_up_the_valley", (120, 980, 300), (0, 120, -20), 30)
camera("air_down_the_valley", (-30, -330, 420), (30, 700, -70), 22)
sc.camera = cams["eye_court_front_edge"]

path = os.path.join(ROOT, "ArtSource", "payo", "payo_land_%s.blend" % TAG)
bpy.ops.wm.save_as_mainfile(filepath=path, compress=True)
for name, cam in cams.items():
    if ONLY and name not in ONLY:
        continue
    sc.camera = cam
    sc.render.filepath = os.path.join(OUT, "land_%s_%s.png" % (TAG, name))
    bpy.ops.render.render(write_still=True)
print("SAVED", path)
