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
             ("close-up, the east cliff stones", (46, 6, 3.5), (62, 30, 4), 28)],
}


def rock_material(m, texture):
    """Rebuild `m` in place (every stone links it) around one candidate texture."""
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
    nt.links.new(mul.outputs["Result"], bsdf.inputs["Base Color"])
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
