"""Lagoon Court: the painted sky, the golden late-afternoon sun and the horizon haze.

docs/LAGOON_REWORK_GUIDE.md § 7a item 9 and § 8 step 7. The reference (ArtStation GvJv5a) sits
under big soft PAINTED cumulus with warm sunlit tops and lilac-grey undersides over a clear blue
sky, lit by a warm golden low sun, with a little haze toward the horizon. The cove was a plain
gradient over flat grey haze.

Called by tools/author_lagoon_cove.py after its lighting:

    import author_lagoon_sky as SKY
    SKY.apply_sky_and_light(scene, sun)

It can also be tried on the saved cove without rebuilding it (READ-ONLY, never save):

    blender -b ArtSource/lagoon/lagoon_cove.blend --python tools/author_lagoon_sky.py -- --preview N

What it builds, and why each piece is shaped the way it is:

1. THE SKY IS ONE PAINTED EQUIRECTANGULAR IMAGE (ArtSource/lagoon/textures/sky_panorama.png,
   4096 x 2048, sRGB). It is painted here in numpy rather than taken from a photo or a noise
   texture: every cloud is a cluster of wobbly-edged discs whose union is shaded as a heap of
   spheres and then posterised into three soft painted tones (warm cream tops, a warm mid, a
   lilac-grey underside) with a flat base, which is how the reference's clouds are drawn.
   Photographic or noise clouds would be the airbrushed-smudge look the owner already rejected
   for textures ("garbage", LAGOON_REWORK_GUIDE § 2). A 2:1 equirect is also exactly what Unity's
   Skybox/Panoramic shader takes, so the same file is the Unity sky.
2. ONLY THE CAMERA SEES IT. The world shader mixes on Light Path "Is Camera Ray": the camera
   sees the painted panorama, everything else is lit by the cove's flat warm-grey fill
   (0.55, 0.6, 0.66). The fill was made warmer and greyer on purpose: a saturated blue sky light
   turned every shaded wall and rock side cold blue-grey, which the owner rejected twice. A blue
   sky must never reach the lighting.
3. THE SUN IS LOW AND GOLDEN: from the south-south-west (near the cove's old sun, so the faces
   the review cameras look at stay lit) but at 32 degrees instead of 48, so shadows run long,
   in a warm gold instead of near white.
4. THE HAZE IS A COMPOSITOR MIST THAT ONLY TOUCHES GEOMETRY. The Mist pass fades distant water
   and rock toward the warm horizon colour, capped low; the sky itself is excluded (its mist is
   exactly 1), because the painted panorama already carries its own horizon haze and a second
   tint would wash its blue out. This is Unity's linear fog, which also never touches the skybox.
"""
import math
import sys
from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
TEXTURES = ROOT / "ArtSource" / "lagoon" / "textures"
PREVIEWS = ROOT / "Logs" / "lagoon-blender"
SKY_FILE = TEXTURES / "sky_panorama.png"
SKY_W, SKY_H = 4096, 2048

# ---------------------------------------------------------------- the sun

# Where the light comes FROM: azimuth in degrees counter-clockwise from +X (atan2 convention,
# so 235 is south-west), elevation above the horizon. The old sun came from about 235 / 48. It
# swings 20 degrees south so ref_angle (looking north-north-west) gets side light that models the
# boulders, eye_north still faces lit rock, and the elevation drops for long late-afternoon shadow.
SUN_AZIMUTH, SUN_ELEVATION = 215.0, 32.0
# A warm gold, not orange: at (1, 0.8, 0.56) AgX Punchy pushed the sand toward offence orange
# (#f87020 is reserved, Art_Direction § 1).
SUN_COLOUR = (1.0, 0.86, 0.66)
SUN_ENERGY = 6.0           # the old 4.5 at 48 degrees; at 30 degrees 4.6 left the court and water dull
SUN_SPECULAR = 0.35        # see _sun: kills the hard glint column, keeps soft sheen
SUN_ANGLE = 2.5            # degrees: soft but still clearly shaped shadow ends

# The LIGHTING fill: the cove's own warm grey, unchanged (see the module docstring, point 2).
FILL_COLOUR, FILL_STRENGTH = (0.55, 0.6, 0.66), 0.6
SKY_STRENGTH = 1.0         # what the camera sees of the painted panorama

# ---------------------------------------------------------------- the haze

# Unity equivalent: RenderSettings.fog, FogMode.Linear, start 60, end 2000, with the density
# capped at MIST_CAP (URP's built-in fog has no cap: either keep the end far so the cap is never
# reached inside the arena, or add the cap in the water/lit shader's fog term).
# ⚠️ THE SKY IS TOLD APART BY ITS MIST BEING 1, AND EEVEE GIVES THE WORLD THE MIST OF THE FAR
# CLIP PLANE, NOT INFINITY. With depth 2400 and the review cameras' clip_end of 2000 the sky read
# about 0.8, was treated as geometry and hazed as a giant grey disc (review v2). So start + depth
# must equal the camera clip_end (every Lagoon preview camera uses 2000): the far plane is then
# exactly 1 and anything behind the sea edge is sky.
CAMERA_CLIP_END = 2000.0
MIST_START, MIST_CAP = 60.0, 0.38
MIST_DEPTH = CAMERA_CLIP_END - MIST_START

# ---------------------------------------------------------------- the painted palette (sRGB)

SKY_ZENITH = (0.20, 0.47, 0.86)
SKY_HIGH = (0.28, 0.60, 0.96)
SKY_LOW = (0.52, 0.78, 0.98)       # review v3: at 0.62 AgX greyed it to slate
SKY_HORIZON = (0.92, 0.90, 0.80)     # warm pale haze, matches the mist colour
SUN_GLOW = (1.0, 0.86, 0.62)
CLOUD_LIT = (1.0, 0.975, 0.90)       # sunlit tops: warm cream, never pure white
CLOUD_MID = (0.96, 0.87, 0.80)       # the warm peach half-tone the reference paints between
CLOUD_SHADE = (0.77, 0.74, 0.86)     # lilac-grey undersides (v3: darker read overcast)
CLOUD_BASE = (0.69, 0.67, 0.80)      # the flat base, one step darker


def srgb_to_linear(c):
    return tuple(v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4 for v in c)


# ---------------------------------------------------------------- painting the panorama

def _smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def _grid():
    """Per-pixel direction of Blender's equirect mapping: u = 0.5 - atan2(y, x) / 2pi,
    v = 0.5 + asin(z) / pi, with image row 0 at the TOP here (flipped when written)."""
    u = (np.arange(SKY_W) + 0.5) / SKY_W
    v = 1.0 - (np.arange(SKY_H) + 0.5) / SKY_H
    az = (0.5 - u) * 2 * math.pi                   # atan2(y, x)
    el = (v - 0.5) * math.pi
    return np.meshgrid(az, el)


def _sun_dir():
    a, e = math.radians(SUN_AZIMUTH), math.radians(SUN_ELEVATION)
    return np.array([math.cos(e) * math.cos(a), math.cos(e) * math.sin(a), math.sin(e)])


def _paint_gradient(az, el):
    """Clear blue overhead to a warm pale horizon, in bands placed by elevation, plus a broad
    warm glow on the sun's side so the whole sky agrees with the light."""
    d = np.degrees(np.maximum(el, 0.0))
    stops = [(0.0, SKY_HORIZON), (7.0, SKY_LOW), (26.0, SKY_HIGH), (70.0, SKY_ZENITH)]
    img = np.empty(el.shape + (3,))
    img[:] = SKY_HORIZON
    for (d0, c0), (d1, c1) in zip(stops, stops[1:]):
        t = _smooth(d0, d1, d)[..., None]
        img = np.where((d >= d0)[..., None], np.array(c0) * (1 - t) + np.array(c1) * t, img)
    s = _sun_dir()
    view = np.stack([np.cos(el) * np.cos(az), np.cos(el) * np.sin(az), np.sin(el)], -1)
    toward = np.clip(view @ s, 0.0, 1.0)
    low = 1.0 - _smooth(0.0, 35.0, d)
    # SCREENED, not mixed: mixing gold into blue made a grey-green smudge (panorama v1).
    glow = (0.45 * toward ** 6 * low + 0.35 * toward ** 40)[..., None]
    img = 1 - (1 - img) * (1 - glow * np.array(SUN_GLOW))
    return img


def _blur(a, sigma):
    """Separable Gaussian, numpy only (Blender's Python has no scipy)."""
    r = int(3 * sigma)
    k = np.exp(-0.5 * (np.arange(-r, r + 1) / sigma) ** 2)
    k /= k.sum()
    p = np.pad(a, r, mode="edge")
    p = np.apply_along_axis(lambda v: np.convolve(v, k, mode="valid"), 0, p)
    return np.apply_along_axis(lambda v: np.convolve(v, k, mode="valid"), 1, p)


def _cloud_blobs(rng, width, height):
    """One cumulus as discs in its own angular frame (x right, y up, radians, base at y = 0).
    A dome envelope sets the tower in the middle, a row of wide flat discs makes the base,
    and small discs on the rim give the cauliflower edge the reference paints."""
    # Disc sizes follow the cloud's HEIGHT, not its width: sized by width (panorama v1) a low
    # wide bank was a few giant discs whose sides lit as long diagonal stripes.
    blobs = []
    n = int(6 + 3.0 * width / height)
    for _ in range(n):
        x = rng.uniform(-0.5, 0.5) * width
        env = height * max(0.0, 1 - (2 * x / width) ** 2) ** 0.6
        env *= rng.uniform(0.8, 1.05)
        r = rng.uniform(0.30, 0.50) * height * (0.55 + 0.45 * env / height)
        y = max(env - r, r * 0.3)
        blobs.append((x, y, r, rng.uniform(0, 0.25) * r))
    for _ in range(int(n * 1.2)):                   # the rim: small puffs on the silhouette
        x = rng.uniform(-0.45, 0.45) * width
        env = height * max(0.0, 1 - (2 * x / width) ** 2) ** 0.6
        r = rng.uniform(0.09, 0.16) * height
        y = env * rng.uniform(0.82, 0.97)
        blobs.append((x, y, r, rng.uniform(0.25, 0.5) * r))
    nb = int(width / (0.45 * height)) + 2              # the base: low discs, cut flat below
    for i in range(nb):
        x = (i / (nb - 1) - 0.5) * width * 0.88
        r = height * rng.uniform(0.28, 0.36)
        blobs.append((x, r * 0.2, r, 0.0))
    return blobs


def _paint_cloud(img, az, el, rng, az0, el0, width, height, haze):
    """Shade one cloud into img in place. The union of the discs is treated as a height field
    of spheres (the highest sphere wins at each pixel), so every point has a real normal; the
    sun's direction in the cloud's own frame gives a Lambert term, which is then POSTERISED into
    three soft tones. That banding is what makes it read as painted rather than rendered."""
    cos0 = math.cos(el0)
    half_w = width * 0.62 / max(cos0, 0.2)
    top = el0 + height * 1.25
    cols = np.where(np.abs(((az[0] - az0 + math.pi) % (2 * math.pi)) - math.pi) < half_w)[0]
    rows = np.where((el[:, 0] > el0 - 0.02) & (el[:, 0] < top))[0]
    if len(cols) == 0 or len(rows) == 0:
        return
    r0, r1 = rows.min(), rows.max() + 1
    sub_az, sub_el = az[r0:r1][:, cols], el[r0:r1][:, cols]
    # Screen frame when facing the cloud: right is DEcreasing azimuth; x is scaled by cos(el)
    # so discs stay round on the sphere.
    x = -(((sub_az - az0 + math.pi) % (2 * math.pi)) - math.pi) * np.cos(sub_el)
    y = sub_el - el0
    px = 2 * math.pi / SKY_W                         # one pixel, in radians
    edge = 2.2 * px
    best = np.full(x.shape, -1e9)
    nx, ny, nz = np.zeros(x.shape), np.zeros(x.shape), np.ones(x.shape)
    alpha = np.zeros(x.shape)
    for bx, by, r, bz in _cloud_blobs(rng, width, height):
        dx, dy = x - bx, y - by
        d = np.hypot(dx, dy)
        ang = np.arctan2(dy, dx)
        # A hand-drawn edge: the radius wobbles gently with angle, two harmonics per disc.
        p1, p2 = rng.uniform(0, 6.3, 2)
        rr = r * (1 + 0.035 * np.sin(5 * ang + p1) + 0.02 * np.sin(9 * ang + p2))
        inside = d < rr
        h = np.sqrt(np.clip(rr * rr - d * d, 0.0, None)) + bz
        win = inside & (h > best)
        best = np.where(win, h, best)
        nx = np.where(win, dx / rr, nx)
        ny = np.where(win, dy / rr, ny)
        nz = np.where(win, np.sqrt(np.clip(1 - (dx * dx + dy * dy) / (rr * rr), 0, 1)), nz)
        alpha = np.maximum(alpha, _smooth(-edge, edge, rr - d))
    # The flat base every cumulus has, softened a touch so it is a painted edge not a cut.
    alpha *= _smooth(-0.3 * edge, 1.2 * edge, y)
    if alpha.max() <= 0:
        return
    # The sun in the cloud's frame: right, up, toward the viewer.
    s = _sun_dir()
    face = np.array([math.cos(az0), math.sin(az0), 0.0])
    right = np.array([math.sin(az0), -math.cos(az0), 0.0])
    lx, ly, lz = s @ right, s[2] + 0.6, -(s @ face)
    ln = math.sqrt(lx * lx + ly * ly + lz * lz)
    # Panorama v2 lit every disc on its own and read as a stack of coins: a crescent of light
    # on each. The reference paints a few BIG masses, so the light follows a blurred copy of the
    # height field (the discs melt into one mass) and the discs' own normals keep only a third.
    field = np.where(alpha > 0, np.maximum(best, 0.0), 0.0)
    sigma = max(2.0, 0.14 * height / px)
    soft = _blur(field, sigma)
    gy, gx = np.gradient(soft, px)
    # x runs toward DEcreasing column index here, so the column gradient flips sign.
    gx = -gx
    sn = np.stack([-gx, gy, np.ones_like(gx)], -1)
    sn /= np.linalg.norm(sn, axis=-1, keepdims=True)
    mnx = 0.35 * nx + 0.65 * sn[..., 0]
    mny = 0.35 * ny + 0.65 * sn[..., 1]
    mnz = 0.35 * nz + 0.65 * sn[..., 2]
    norm = np.sqrt(mnx * mnx + mny * mny + mnz * mnz)
    lam = (mnx * lx + mny * ly + mnz * lz) / (ln * norm)
    rel_h = np.clip(y / height, 0, 1)
    t = 0.62 * lam + 0.38 * rel_h + 0.1               # height brightens: tops catch the sun
    # A backlit cloud (sun behind it) gets a bright rim instead of a lit face.
    back = max(0.0, s @ face)
    rim = back * _smooth(0.5, 0.95, 1 - nz)
    t = t + 0.25 * rim
    lit = _smooth(0.50, 0.62, t)[..., None]
    mid = _smooth(0.24, 0.36, t)[..., None]
    base = _smooth(0.0, 0.07, rel_h)[..., None]
    col = np.array(CLOUD_SHADE) * (1 - mid) + np.array(CLOUD_MID) * mid
    col = col * (1 - lit) + np.array(CLOUD_LIT) * lit
    col = np.array(CLOUD_BASE) * (1 - base) + col * base
    # Far (low) clouds sink into the horizon haze: aerial perspective, painted.
    col = col * (1 - haze) + np.array(SKY_HORIZON) * haze
    a = (alpha * (1 - 0.35 * haze))[..., None]
    region = img[r0:r1][:, cols]
    img[r0:r1, cols] = region * (1 - a) + col * a


# The cloudscape: (azimuth deg, base elevation deg, width deg, height deg). Big cumulus sit low
# around the horizon because that is the only sky the review cameras see (ref_angle's frame tops
# out about 16 degrees up, eye_north's about 38), with a few smaller, higher ones for depth.
# The north arc (60 to 120) is what ref_angle and eye_north face; the south-west arc is what
# village faces, against the sun.
CLOUDS = [
    (95, 2.0, 40, 11), (62, 2.5, 28, 8), (128, 2.0, 32, 9), (160, 1.5, 26, 7.5),
    (35, 1.5, 24, 7), (8, 2.0, 30, 8), (-25, 1.5, 36, 9), (-60, 2.0, 28, 8),
    (-95, 1.8, 40, 11), (-130, 1.5, 34, 8), (-165, 2.2, 30, 8.5), (-160, 1.0, 20, 5),
    (80, 14.0, 14, 5), (112, 20.0, 11, 4), (45, 11.0, 13, 4.5), (-110, 15.0, 14, 5),
    (-40, 19.0, 11, 4), (150, 13.0, 11, 4), (20, 26.0, 9, 3.2), (-150, 24.0, 10, 3.5),
]


def paint_sky(path=SKY_FILE, seed=2026):
    """Paint the panorama and write it as an 8-bit sRGB PNG. Deterministic for a seed."""
    rng = np.random.default_rng(seed)
    az, el = _grid()
    img = _paint_gradient(az, el)
    # Paint from the highest cloud down so the low, near-horizon banks overlap the far ones.
    for a, e, w, h in sorted(CLOUDS, key=lambda c: -c[1]):
        haze = float(np.clip(1 - e / 14.0, 0, 1)) * 0.28
        _paint_cloud(img, az, el, rng, math.radians(a), math.radians(e), math.radians(w),
                     math.radians(h), haze)
    below = el < 0
    img[below] = SKY_HORIZON                         # the sea hides it; keep the edge seamless
    # One least-significant-bit of ordered dither: the long gradient bands in 8 bits otherwise.
    dither = (np.indices(img.shape[:2]).sum(0) % 2 - 0.5)[..., None] / 255.0
    img = np.clip(img + dither, 0, 1)
    path.parent.mkdir(parents=True, exist_ok=True)
    rgba = np.concatenate([img[::-1], np.ones(img.shape[:2] + (1,))], -1).astype(np.float32)
    im = bpy.data.images.new("sky_panorama_paint", SKY_W, SKY_H, alpha=False)
    im.pixels.foreach_set(rgba.ravel())
    im.filepath_raw = str(path)
    im.file_format = "PNG"
    im.save()
    bpy.data.images.remove(im)
    print("[lagoon-sky] painted", path)
    return path


# ---------------------------------------------------------------- the world, sun and haze

def _world(scene, path):
    world = scene.world or bpy.data.worlds.new("sky")
    scene.world = world
    world.use_nodes = True
    nt = world.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputWorld")
    fill = nt.nodes.new("ShaderNodeBackground")
    fill.name = "lighting fill"
    fill.inputs["Color"].default_value = (*FILL_COLOUR, 1)
    fill.inputs["Strength"].default_value = FILL_STRENGTH
    env = nt.nodes.new("ShaderNodeTexEnvironment")
    env.image = bpy.data.images.load(str(path), check_existing=True)
    env.image.reload()
    env.image.colorspace_settings.name = "sRGB"
    env.interpolation = "Cubic"
    seen = nt.nodes.new("ShaderNodeBackground")
    seen.name = "painted sky"
    seen.inputs["Strength"].default_value = SKY_STRENGTH
    path_node = nt.nodes.new("ShaderNodeLightPath")
    mix = nt.nodes.new("ShaderNodeMixShader")
    nt.links.new(env.outputs["Color"], seen.inputs["Color"])
    nt.links.new(path_node.outputs["Is Camera Ray"], mix.inputs["Fac"])
    nt.links.new(fill.outputs["Background"], mix.inputs[1])
    nt.links.new(seen.outputs["Background"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    # Eevee can pull a sun out of a bright world; the lighting branch is flat, so nothing should
    # be extracted, and the painted sun glow must not become a second light.
    if hasattr(world, "sun_threshold"):
        world.sun_threshold = 1000.0
    world.mist_settings.start = MIST_START
    world.mist_settings.depth = MIST_DEPTH
    world.mist_settings.falloff = "LINEAR"
    return world


def _sun(sun_obj):
    a, e = math.radians(SUN_AZIMUTH), math.radians(SUN_ELEVATION)
    travel = -Vector((math.cos(e) * math.cos(a), math.cos(e) * math.sin(a), math.sin(e)))
    sun_obj.rotation_mode = "XYZ"
    sun_obj.rotation_euler = travel.to_track_quat("-Z", "Y").to_euler()
    sun_obj.data.color = SUN_COLOUR
    sun_obj.data.energy = SUN_ENERGY
    sun_obj.data.angle = math.radians(SUN_ANGLE)
    # A low sun in front of the village camera laid a hard white glint column down the sea
    # (review v2). The reference has no glints; the Unity water shader draws its own sparkle.
    sun_obj.data.specular_factor = SUN_SPECULAR


def _haze(scene):
    """Distance haze in the compositor. The Mist pass is exactly 1 for the sky and under 1 for
    every piece of geometry nearer than the far clip (see CAMERA_CLIP_END), so a 'less than 1' mask
    keeps the painted sky untouched, which is what Unity's fog does to a skybox."""
    scene.view_layers[0].use_pass_mist = True
    colour = (*srgb_to_linear(SKY_HORIZON), 1.0)
    if hasattr(scene, "compositing_node_group"):          # Blender 5
        tree = bpy.data.node_groups.new("lagoon haze", "CompositorNodeTree")
        scene.compositing_node_group = tree
        tree.interface.new_socket("Image", in_out="OUTPUT", socket_type="NodeSocketColor")
        out = tree.nodes.new("NodeGroupOutput")
    else:
        scene.use_nodes = True
        tree = scene.node_tree
        tree.nodes.clear()
        out = tree.nodes.new("CompositorNodeComposite")
    L = tree.links.new
    rl = tree.nodes.new("CompositorNodeRLayers")
    geo = tree.nodes.new("ShaderNodeMath")
    geo.operation, geo.inputs[1].default_value = "LESS_THAN", 0.999
    L(rl.outputs["Mist"], geo.inputs[0])
    cap = tree.nodes.new("ShaderNodeMath")
    cap.operation, cap.inputs[1].default_value = "MINIMUM", MIST_CAP
    L(rl.outputs["Mist"], cap.inputs[0])
    fac = tree.nodes.new("ShaderNodeMath")
    fac.operation = "MULTIPLY"
    L(cap.outputs[0], fac.inputs[0])
    L(geo.outputs[0], fac.inputs[1])
    mix = tree.nodes.new("ShaderNodeMix")
    mix.data_type, mix.blend_type = "RGBA", "MIX"
    L(fac.outputs[0], mix.inputs[0])
    L(rl.outputs["Image"], mix.inputs[6])
    mix.inputs[7].default_value = colour
    L(mix.outputs[2], out.inputs[0])


def apply_sky_and_light(scene, sun_obj, repaint=False):
    """Painted sky for the camera, warm-grey fill for the lighting, a low golden sun and a
    capped horizon haze. Paints sky_panorama.png the first time (or with repaint=True)."""
    if repaint or not SKY_FILE.exists():
        paint_sky()
    _world(scene, SKY_FILE)
    _sun(sun_obj)
    _haze(scene)
    try:
        scene.view_settings.view_transform = "AgX"
        scene.view_settings.look = "AgX - Punchy"
    except TypeError:
        pass
    print("[lagoon-sky] sun from az %.0f el %.0f, colour %s energy %.1f" % (
        SUN_AZIMUTH, SUN_ELEVATION, SUN_COLOUR, SUN_ENERGY))


# ---------------------------------------------------------------- standalone review

SHOTS = [
    ("ref_angle", (40, -100, 20), (-6, 14, 8), 24),
    ("eye_north", (0, -9, 1.3), (0, 45, 8), 18 / math.tan(math.radians(95 / 2))),
    ("village", (95, -20, 14), (40, -75, 0), 28),
]


def review(version, shots=None):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 900
    scene.render.resolution_percentage = 100
    if hasattr(scene.eevee, "shadow_pool_size"):
        scene.eevee.shadow_pool_size = "1024"
    cam = bpy.data.objects.new("sky_cam", bpy.data.cameras.new("sky_cam"))
    cam.data.clip_end = 2000
    cam.data.sensor_fit = "HORIZONTAL"
    scene.collection.objects.link(cam)
    scene.camera = cam
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    for name, pos, tgt, lens in SHOTS:
        if shots and name not in shots:
            continue
        out = PREVIEWS / f"sky_{name}_v{version}.png"
        if out.exists():
            raise SystemExit(f"{out} exists; renders are never overwritten, pick a new version")
        cam.data.lens = lens
        cam.location = pos
        cam.rotation_euler = (Vector(tgt) - Vector(pos)).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = str(out)
        bpy.ops.render.render(write_still=True)
        print("[lagoon-sky] review", out)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
    shots = argv[argv.index("--shots") + 1].split(",") if "--shots" in argv else None
    if "--paint-only" in argv:
        paint_sky()
        return
    scene = bpy.context.scene
    sun = next(o for o in scene.objects if o.type == "LIGHT" and o.data.type == "SUN")
    apply_sky_and_light(scene, sun, repaint="--repaint" in argv)
    if version:
        review(version, shots)
    # Deliberately no save: this entry point is for reviewing the saved cove read-only.


if __name__ == "__main__":
    main()
