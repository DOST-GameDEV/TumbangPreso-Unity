"""Render candidate Lagoon textures ON THE MODELS, side by side, for the owner's review.

  blender -b ArtSource/lagoon/lagoon_cove.blend --python tools/render_lagoon_texture_preview.py -- \
      --material rock --textures rock_a,rock_b,rock_c --version N

OWNER, 2026-09-26: "when you texture i need a render of how it's gonna look on the models". A
swatch sheet shows a texture flat; this shows it where it will live. For each candidate texture
the named material is rebuilt around it and the same shots are rendered (a game's-eye view from
the court at 1.25 m and 95 degrees, and a close-up), then everything is laid out on one sheet:
Logs/lagoon-blender/<material>_on_models_vN.png (rows = textures, columns = shots), composed
and labelled by the same file under plain Python (Blender's Python has no PIL):

  py -3 tools/render_lagoon_texture_preview.py --sheet rock rock_a,rock_b,rock_c N

The rock material maps the texture by BOX projection in WORLD space at 4 m a tile, the same
mapping a triplanar shader does in Unity, so it needs no UVs and a stone of any size shows the
texture at its real scale. It also carries what the texture cannot know: lighter where a face
points UP, darker where it faces DOWN (the owner's reference rocks: light tops, dark bases).
"""
import math
import sys
from pathlib import Path

try:
    import bpy
    from mathutils import Vector
except ImportError:      # plain Python: the --sheet mode only
    bpy = None

ROOT = Path(__file__).resolve().parents[1]
TEX = ROOT / "ArtSource" / "lagoon" / "textures"
OUT = ROOT / "Logs" / "lagoon-blender"
TILE_M = 4.0
E = 1.3
EYE_LENS = 18 / math.tan(math.radians(95 / 2))
SHOTS = {
    "rock": [("game's eye, from the court", (0, -9, E), (0, 45, 8), EYE_LENS),
             ("close-up, the east cliff stones", (46, 6, 3.5), (62, 30, 4), 28),
             ("close-up, the stones behind the court", (-4, 4, 3.0), (-14, 20, 3.5), 30),
             ("from above, the massif (the owner's paint-over view)", (-26, 6, 24), (-34, 26, 8), 30)],
}


EDGE_ATLAS = TEX / "rock_edges_atlas.png"


def _math(nt, op, a, b, clamp=False):
    n = nt.nodes.new("ShaderNodeMath")
    n.operation = op
    n.use_clamp = clamp
    for i, v in enumerate((a, b)):
        if isinstance(v, (int, float)):
            n.inputs[i].default_value = v
        else:
            nt.links.new(v, n.inputs[i])
    return n.outputs["Value"]


def _range(nt, v, a, b, c=0.0, d=1.0, smooth=False):
    n = nt.nodes.new("ShaderNodeMapRange")
    if smooth:
        n.interpolation_type = "SMOOTHSTEP"
    n.inputs["From Min"].default_value, n.inputs["From Max"].default_value = a, b
    n.inputs["To Min"].default_value, n.inputs["To Max"].default_value = c, d
    nt.links.new(v, n.inputs["Value"])
    return n.outputs["Result"]


def _scale_colour(nt, colour, factor):
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    mix.blend_type = "MULTIPLY"
    mix.inputs["Factor"].default_value = 1.0
    nt.links.new(colour, mix.inputs["A"])
    grey = nt.nodes.new("ShaderNodeCombineColor")
    for ch in ("Red", "Green", "Blue"):
        nt.links.new(factor, grey.inputs[ch])
    nt.links.new(grey.outputs["Color"], mix.inputs["B"])
    return mix.outputs["Result"]


def _lift(nt, colour, target, fac):
    """Move `colour` toward `target` by `fac` (0..1)."""
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    mix.inputs["B"].default_value = target
    nt.links.new(colour, mix.inputs["A"])
    nt.links.new(fac, mix.inputs["Factor"])
    return mix.outputs["Result"]


EDGE_WHITE = (0.99, 0.88, 0.68, 1.0)   # warm near-white (v10 0.98/0.93/0.82 went blue-grey in shade)


def inner_glow(nt, colour, ch, up):
    """THE INNER-SHADOW EDGE (owner, 2026-09-26, on v9: "make them slightly more clear? think of
    like an inner shadow effect, the edges have the crispiest white and then it fades the closer
    it gets to the center"). Profile across a face, from its edge inward:
      * a CRISP near-white line on the break itself (the narrow mask, cut hard), then
      * a soft falloff toward the face's centre (the broad mask, eased), lifting the rock's own
        colour less and less.
    Full strength on top edges, weaker on the sides, faint underneath, so it stays natural."""
    weight = _range(nt, up.outputs["Z"], -0.2, 0.7, 0.15, 1.0)   # v10: side edges were as bright as tops
    crisp = _range(nt, ch.outputs["Red"], 0.45, 0.62, smooth=True)
    fade = _math(nt, "POWER", _range(nt, ch.outputs["Green"], 0.0, 1.0), 1.15)   # v10 1.6: too short
    fade = _math(nt, "MULTIPLY", fade, 0.7)
    # The FADE lightens toward a brighter version of the rock's OWN colour, weighted twice to
    # top faces; only the crisp CORE goes to warm near-white. (v11 lifted whole shaded side
    # faces toward white and the sky light turned them cold blue-grey.)
    fade = _math(nt, "MULTIPLY", _math(nt, "MULTIPLY", fade, weight), weight, clamp=True)
    colour = _scale_colour(nt, colour, _range(nt, fade, 0.0, 1.0, 1.0, 1.45))
    crisp = _math(nt, "MULTIPLY", _math(nt, "MULTIPLY", crisp, weight), weight, clamp=True)   # v12: grey lines on shaded sides
    return _lift(nt, colour, EDGE_WHITE, _math(nt, "MULTIPLY", crisp, 0.85))


def brushed_edge(nt, colour, ch, up, geo):
    """THE OWNER'S PAINT-OVER (2026-09-26, Logs/lagoon-blender/owner_rock_edges_paintover.webp,
    "see the center 2 rocks", "i just drew over it in photoshop"), after rejecting inner_glow
    ("nope. its worse"): inner_glow lifted WHOLE faces. The paint-over keeps each face the plain
    rock colour and paints only:
      * a thin crisp pale LINE exactly on the edge, and
      * a NARROW soft light BAND just inside it, its inner side feathered with a dry-brush
        breakup (speckled, not a smooth gradient),
    strongest round the perimeter of the TOP faces; the side faces stay darker. Both come from
    the narrow mask (R) only; the broad mask is not used."""
    weight = _range(nt, up.outputs["Z"], -0.1, 0.7, 0.2, 1.0)
    # Dry brush: a fine noise sets where the band's inner edge breaks up.
    fine = nt.nodes.new("ShaderNodeTexNoise")
    fine.inputs["Scale"].default_value = 7.0
    fine.inputs["Detail"].default_value = 8.0
    fine.inputs["Roughness"].default_value = 0.7
    nt.links.new(geo.outputs["Position"], fine.inputs["Vector"])
    threshold = _range(nt, fine.outputs["Fac"], 0.3, 0.7, 0.04, 0.30)   # v14 0.10..0.45: too narrow
    edge = ch.outputs["Red"]
    diff = _math(nt, "SUBTRACT", edge, threshold)
    band = _range(nt, diff, -0.02, 0.10, smooth=True)                 # speckled inner boundary
    band = _math(nt, "MULTIPLY", band, weight, clamp=True)
    colour = _scale_colour(nt, colour, _range(nt, band, 0.0, 1.0, 1.0, 1.40))   # v14 1.28: too faint
    line = _range(nt, edge, 0.70, 0.86, smooth=True)                   # the crisp line on the break
    line = _math(nt, "MULTIPLY", line, weight, clamp=True)
    return _lift(nt, colour, EDGE_WHITE, _math(nt, "MULTIPLY", line, 0.75))


def edge_wear(nt, colour, chips, inner=False, brush=False):
    """THE WEATHERED EDGES, from the three baked masks (tools/bake_lagoon_rock_edges.py).
    Owner on v7: "the weathered edges look really unnatural": one even light line along EVERY
    break (sides, bottoms, silhouette) reads as piping. On the reference the light is on the
    rounded SHOULDERS, broad and soft and mostly on top, and the crevices between planes are
    darker. So: shoulders lighten softly by the broad mask, weighted to up-facing surfaces;
    crevices darken by the stone's own occlusion (a value change only, never a new colour); with `chips`, the narrow mask adds a few
    crisper worn chips on TOP edges only, broken into pieces by noise."""
    uv = nt.nodes.new("ShaderNodeUVMap")
    uv.uv_map = "UVBake"
    atlas = nt.nodes.new("ShaderNodeTexImage")
    atlas.image = bpy.data.images.load(str(EDGE_ATLAS), check_existing=True)
    atlas.image.colorspace_settings.name = "Non-Color"
    nt.links.new(uv.outputs["UV"], atlas.inputs["Vector"])
    ch = nt.nodes.new("ShaderNodeSeparateColor")
    nt.links.new(atlas.outputs["Color"], ch.inputs["Color"])
    geo = nt.nodes.new("ShaderNodeNewGeometry")
    up = nt.nodes.new("ShaderNodeSeparateXYZ")
    nt.links.new(geo.outputs["Normal"], up.inputs[0])
    upward = _range(nt, up.outputs["Z"], -0.2, 0.8, 0.25, 1.0)
    if inner or brush:
        colour = _scale_colour(nt, colour, _range(nt, ch.outputs["Blue"], 0.35, 0.95, 0.72, 1.0, smooth=True))
        return brushed_edge(nt, colour, ch, up, geo) if brush else inner_glow(nt, colour, ch, up)
    # Shoulders: up to 20 % lighter, soft.
    shoulder = _math(nt, "MULTIPLY", _range(nt, ch.outputs["Green"], 0.0, 0.9, smooth=True), upward)
    # ⚠️ NEVER A SEPARATE COLOUR (owner on v7: "even how the color of the weathered edges are
    # unnatural": v7 mixed a flat tan over the rock). Wear only LIGHTENS or DARKENS the rock's
    # own texture, so an edge keeps its hue and its painted patches.
    colour = _scale_colour(nt, colour, _range(nt, shoulder, 0.0, 1.0, 1.0, 1.30))   # v8 at 1.20 read too faint
    # Crevices: down to 72 % where the stone occludes itself.
    colour = _scale_colour(nt, colour, _range(nt, ch.outputs["Blue"], 0.35, 0.95, 0.72, 1.0, smooth=True))
    if chips:
        noise = nt.nodes.new("ShaderNodeTexNoise")
        noise.inputs["Scale"].default_value = 0.9
        noise.inputs["Detail"].default_value = 3.0
        nt.links.new(geo.outputs["Position"], noise.inputs["Vector"])
        pieces = _range(nt, noise.outputs["Fac"], 0.5, 0.58, smooth=True)     # ~40 % of the length
        top_only = _range(nt, up.outputs["Z"], 0.25, 0.6, smooth=True)
        chip = _math(nt, "MULTIPLY", _range(nt, ch.outputs["Red"], 0.25, 0.6, smooth=True), pieces)
        chip = _math(nt, "MULTIPLY", chip, top_only)
        colour = _scale_colour(nt, colour, _range(nt, chip, 0.0, 1.0, 1.0, 1.20))
    return colour


def rock_material(m, texture):
    """Rebuild `m` in place (every stone links it) around one candidate texture. A name ending
    "+soft" adds the baked shoulders and crevices, "+chips" those plus top-edge chips."""
    wear = texture.split("+")[1] if "+" in texture else None
    texture = texture.split("+")[0]
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.inputs["Roughness"].default_value = 0.9
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    geo = nt.nodes.new("ShaderNodeNewGeometry")
    scale = nt.nodes.new("ShaderNodeVectorMath")
    scale.operation = "SCALE"
    scale.inputs["Scale"].default_value = 1.0 / TILE_M
    nt.links.new(geo.outputs["Position"], scale.inputs[0])
    img = nt.nodes.new("ShaderNodeTexImage")
    img.image = bpy.data.images.load(str(TEX / f"{texture}_albedo.png"), check_existing=True)
    img.projection = "BOX"
    img.projection_blend = 0.45   # 0.25 smeared streaks where the box sides meet on slanted faces
    nt.links.new(scale.outputs["Vector"], img.inputs["Vector"])
    # Light tops, dark undersides, by the face's world normal.
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    nt.links.new(geo.outputs["Normal"], sep.inputs[0])
    tone = nt.nodes.new("ShaderNodeMapRange")
    tone.inputs["From Min"].default_value, tone.inputs["From Max"].default_value = -0.6, 0.9
    tone.inputs["To Min"].default_value, tone.inputs["To Max"].default_value = 0.78, 1.14
    nt.links.new(sep.outputs["Z"], tone.inputs["Value"])
    mul = nt.nodes.new("ShaderNodeMix")
    mul.data_type = "RGBA"
    mul.blend_type = "MULTIPLY"
    mul.inputs["Factor"].default_value = 1.0
    nt.links.new(img.outputs["Color"], mul.inputs["A"])
    grey = nt.nodes.new("ShaderNodeCombineColor")
    for ch in ("Red", "Green", "Blue"):
        nt.links.new(tone.outputs["Result"], grey.inputs[ch])
    nt.links.new(grey.outputs["Color"], mul.inputs["B"])
    colour = mul.outputs["Result"]
    if wear:
        colour = edge_wear(nt, colour, chips=(wear == "chips"), inner=(wear == "inner"), brush=(wear == "brush"))
    nt.links.new(colour, bsdf.inputs["Base Color"])
    normal_path = TEX / f"{texture}_normal.png"
    if normal_path.exists():
        nimg = nt.nodes.new("ShaderNodeTexImage")
        nimg.image = bpy.data.images.load(str(normal_path), check_existing=True)
        nimg.image.colorspace_settings.name = "Non-Color"
        nimg.projection = "BOX"
        nimg.projection_blend = 0.45   # 0.25 smeared streaks where the box sides meet on slanted faces
        nt.links.new(scale.outputs["Vector"], nimg.inputs["Vector"])
        nmap = nt.nodes.new("ShaderNodeNormalMap")
        nmap.inputs["Strength"].default_value = 0.6
        nt.links.new(nimg.outputs["Color"], nmap.inputs["Color"])
        nt.links.new(nmap.outputs["Normal"], bsdf.inputs["Normal"])


BUILDERS = {"rock": rock_material}


def main():
    if bpy is None:
        _flag, material, textures, version = sys.argv[1:5]
        sheet(material, textures.split(","), int(version))
        return
    argv = sys.argv[sys.argv.index("--") + 1:]
    material = argv[argv.index("--material") + 1]
    textures = argv[argv.index("--textures") + 1].split(",")
    version = int(argv[argv.index("--version") + 1])
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1280, 720
    if hasattr(scene.eevee, "shadow_pool_size"):
        scene.eevee.shadow_pool_size = "1024"
    cam = bpy.data.objects.new("preview cam", bpy.data.cameras.new("preview cam"))
    cam.data.sensor_fit = "HORIZONTAL"
    cam.data.clip_end = 2000
    scene.collection.objects.link(cam)
    scene.camera = cam
    m = bpy.data.materials[material]
    for tex in textures:
        BUILDERS[material](m, tex)
        for k, (label, pos, tgt, lens) in enumerate(SHOTS[material]):
            cam.location = pos
            cam.data.lens = lens
            cam.rotation_euler = (Vector(tgt) - Vector(pos)).to_track_quat("-Z", "Y").to_euler()
            path = OUT / f"{material}_on_models_{tex}_{k}_v{version}.png"
            scene.render.filepath = str(path)
            bpy.ops.render.render(write_still=True)


def sheet(material, textures, version):
    """Compose the renders into one labelled sheet (plain Python, PIL)."""
    from PIL import Image, ImageDraw, ImageFont
    w, h, pad, head = 960, 540, 14, 36
    shots = SHOTS[material]
    W, H = pad + len(shots) * (w + pad), pad + len(textures) * (h + head + pad)
    page = Image.new("RGB", (W, H), (245, 241, 234))
    draw = ImageDraw.Draw(page)
    try:
        font = ImageFont.truetype("arial.ttf", 22)
    except OSError:
        font = ImageFont.load_default()
    for r, tex in enumerate(textures):
        for c, (label, *_rest) in enumerate(shots):
            x, y = pad + c * (w + pad), pad + r * (h + head + pad)
            draw.text((x, y + 6), f"{tex}: {label}", fill=(40, 36, 32), font=font)
            img = Image.open(OUT / f"{material}_on_models_{tex}_{c}_v{version}.png").convert("RGB")
            page.paste(img.resize((w, h), Image.LANCZOS), (x, y + head))
    out = OUT / f"{material}_on_models_v{version}.png"
    page.save(out)
    print("[texture-preview] sheet", out)


if __name__ == "__main__":
    main()
