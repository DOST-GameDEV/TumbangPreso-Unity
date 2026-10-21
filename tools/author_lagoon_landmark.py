"""Lagoon Court's LANDMARK EMBLEM (the painted mark on the landmark rock) and its BACKDROP (the
distant islands and rock spires on the horizon). docs/LAGOON_REWORK_GUIDE.md § 7a items 5 and 11.

  py -3 tools/author_lagoon_landmark.py --paint                 # the emblem textures (numpy, PIL, scipy)
  py -3 tools/author_lagoon_landmark.py --swatches N            # Logs/lagoon-blender/landmark_swatches_vN.png
  blender -b ArtSource/lagoon/lagoon_cove.blend --python tools/author_lagoon_landmark.py -- --test N

The test OPENS lagoon_cove.blend and never saves it: it applies each emblem in turn to the
"landmark rock", builds the backdrop, renders, and composes landmark_court_vN.png,
landmark_close_vN.png and landmark_backdrop_vN.png (the composing is done by `py -3` from a
subprocess, since Blender's own Python has no PIL).

Importable from Blender with no side effects. The two calls author_lagoon_cove.py makes:

  import author_lagoon_landmark as LM
  LM.apply_emblem(bpy.data.objects["landmark rock"], "pawikan", 4.6)
  LM.build_backdrop(L.col("Backdrop", root), KIT)

WHY AN EMBLEM AT ALL. The reference's signature is a big white octopus painted on a rock at the
waterline (§ 1): the one thing a player remembers the map by. Ours is our OWN mark, not the
octopus, and it is painted like the reference's: flat chunky shapes in one off-white lime wash,
worn and broken at the edge the way paint on stone wears, never a crisp sticker.

WHY A DECAL MESH, NOT A MATERIAL. Two ways were allowed: a planar projection mixed into a copy of
the rock material, or a thin mesh that follows the stone. The mesh wins on the Unity side: the
rock keeps its ONE shared material (the brushed-edge rock_a every stone links, § 8 step 2), and
the emblem is a plain lit mesh with an alpha texture and ordinary UVs, which any URP shader draws.
A projection inside the rock material would need a second rock shader in Unity that knows about
one object. The mesh is built by RAY CASTING a grid onto the stone from the court's side (the
same thing a shrinkwrap "project" does, but with control over what is kept), so it follows every
plane and bevel of the stone; it stands 2 cm off the surface along the surface normal, far more
than depth precision needs at 45 m and far less than the eye can see as a gap.
"""
import math
import os
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TEX = ROOT / "ArtSource" / "lagoon" / "textures"
PREVIEWS = ROOT / "Logs" / "lagoon-blender"
WATER = -1.8                 # author_lagoon_cove.WATER (not imported: that module builds on import of bpy)
COURT = (0.0, 1.0)           # the court's centre; the emblem faces it

try:
    import bpy               # noqa: F401
    IN_BLENDER = True
except ImportError:
    IN_BLENDER = False

# ------------------------------------------------------------------------------------------------
# THE EMBLEMS. Three candidates, each our own, each a Filipino sea subject:
#   pawikan  a sea turtle seen from above. Turtles nest on the Turtle Islands of Tawi-Tawi, in
#            Sama-Bajau waters; a creature, like the reference's octopus, and a silhouette that
#            reads at any size (a shell and four flippers).
#   pagi     a manta ray seen from above: one big wing shape, the most graphic of the three.
#   bangka   an outrigger boat under sail with a sun and two waves: the village's own boat,
#            but the busiest drawing, with the thinnest parts.
EMBLEMS = ("pawikan", "pagi", "bangka")

# LIME WASH, not white. Pure white under the cove's AgX Punchy grade blows out to a flat sticker;
# a warm off-white (sRGB f2e8cc). v1 (e8e1cc) read grey and v2 (f0ead6) read COLD blue-grey
# in the rock's shade, where only the sky fill reaches it (the same fault the owner rejected on
# shaded walls and rock sides), so the wash leans cream keeps a little of the stone's warmth and
# still reads as the lightest thing on the rock. A second, warmer tint comes in as a few big
# feathered patches (the same "few large feathered patches" rule as every Lagoon texture).
PAINT = (0xF2 / 255, 0xE8 / 255, 0xCC / 255)
PAINT_WARM = (0xE8 / 255, 0xDA / 255, 0xB8 / 255)
N = 2048                     # drawn at 2048 and saved at 1024, so every edge is antialiased
OUT_PX = 1024


def _paths(name):
    return TEX / f"lm_emblem_{name}_albedo.png"


# ---------------------------------------------------------------- painting (py -3 only)

def _ellipse(cx, cy, rx, ry, rot=0.0, egg=0.0, n=120):
    """An ellipse as a polygon in unit coordinates (y down). `egg` narrows the +y end, so a
    turtle shell tapers toward its tail."""
    pts = []
    c, s = math.cos(rot), math.sin(rot)
    for k in range(n):
        t = math.tau * k / n
        x, y = rx * math.cos(t), ry * math.sin(t)
        x *= 1 - egg * max(0.0, math.sin(t))
        pts.append((cx + x * c - y * s, cy + x * s + y * c))
    return pts


def _bezier(p0, p1, p2, t):
    a = (1 - t) ** 2
    b = 2 * (1 - t) * t
    d = t * t
    return (a * p0[0] + b * p1[0] + d * p2[0], a * p0[1] + b * p1[1] + d * p2[1])


def _blade(root, ctrl, tip, w0, n=48, tip_round=0.35):
    """A tapering curved blade (a flipper, a tail) along a quadratic curve, as a polygon. The
    width falls off as a rounded paddle, not a spike: chunky shapes read at 45 m, needles do not."""
    left, right = [], []
    for k in range(n + 1):
        t = k / n
        x, y = _bezier(root, ctrl, tip, t)
        x2, y2 = _bezier(root, ctrl, tip, min(1.0, t + 0.01))
        x1, y1 = _bezier(root, ctrl, tip, max(0.0, t - 0.01))
        dx, dy = x2 - x1, y2 - y1
        ln = math.hypot(dx, dy) or 1.0
        nx, ny = -dy / ln, dx / ln
        # a paddle: widest a third of the way out, rounded off at the tip
        w = w0 * (0.75 + 0.6 * math.sin(math.pi * min(1.0, t * 1.5)) * (1 - t) + tip_round * (1 - t))
        w *= math.sqrt(max(0.0, 1 - t ** 3))
        left.append((x + nx * w / 2, y + ny * w / 2))
        right.append((x - nx * w / 2, y - ny * w / 2))
    return left + right[::-1]


def _catmull(points, steps=10, closed=True):
    pts, n = [], len(points)
    rng = range(n) if closed else range(n - 1)
    for i in rng:
        p0 = points[(i - 1) % n] if closed or i > 0 else points[0]
        p1, p2 = points[i], points[(i + 1) % n]
        p3 = points[(i + 2) % n] if closed or i + 2 < n else points[-1]
        for k in range(steps):
            t = k / steps
            pts.append(tuple(0.5 * ((2 * p1[j]) + (-p0[j] + p2[j]) * t
                                    + (2 * p0[j] - 5 * p1[j] + 4 * p2[j] - p3[j]) * t * t
                                    + (-p0[j] + 3 * p1[j] - 3 * p2[j] + p3[j]) * t ** 3) for j in (0, 1)))
    if not closed:
        pts.append(points[-1])
    return pts


def _mirror(pts):
    return [(1 - x, y) for x, y in pts]


class _Pen:
    """Fills and carves in unit coordinates on an N x N mask. A carve is the GAP between two
    painted shapes: every emblem is drawn in the reference's way, solid shapes separated by
    negative-space lines, never outlines."""

    def __init__(self):
        from PIL import Image, ImageDraw
        self.img = Image.new("L", (N, N), 0)
        self.d = ImageDraw.Draw(self.img)

    def fill(self, pts, on=True):
        self.d.polygon([(x * N, y * N) for x, y in pts], fill=255 if on else 0)

    def line(self, pts, w, on=False):
        px = [(x * N, y * N) for x, y in pts]
        r = w * N / 2
        self.d.line(px, fill=255 if on else 0, width=max(1, int(round(w * N))), joint="curve")
        for x, y in (px[0], px[-1]):
            self.d.ellipse((x - r, y - r, x + r, y + r), fill=255 if on else 0)

    def dot(self, x, y, r, on=False):
        self.d.ellipse(((x - r) * N, (y - r) * N, (x + r) * N, (y + r) * N), fill=255 if on else 0)


GAP = 0.022                  # the negative-space line width, as a fraction of the emblem


def draw_pawikan(p):
    """A sea turtle from above, head up. The shell is split into a ring of marginal plates and a
    centre of three hexagonal plates with ribs out to the ring: the classic turtle mark, reduced
    to the gaps a brush would leave."""
    # flippers and head first, then the shell over them with a gap around it
    for side in (False, True):
        front = _blade((0.37, 0.43), (0.17, 0.25), (0.06, 0.43), 0.105)
        back = _blade((0.40, 0.71), (0.31, 0.77), (0.27, 0.88), 0.075, tip_round=0.5)
        p.fill(_mirror(front) if side else front)
        p.fill(_mirror(back) if side else back)
    p.fill(_ellipse(0.5, 0.195, 0.072, 0.088))                          # head
    p.fill([(0.47, 0.76), (0.53, 0.76), (0.5, 0.875)])                  # tail
    shell = dict(cx=0.5, cy=0.52, rx=0.215, ry=0.255, egg=0.14)
    p.fill(_ellipse(**{**shell, "rx": shell["rx"] + GAP * 1.1, "ry": shell["ry"] + GAP * 1.1}), on=False)
    p.fill(_ellipse(**shell))
    for x in (0.465, 0.535):                                            # eyes
        p.dot(x, 0.18, 0.014)
    # the marginal ring
    ring = _ellipse(0.5, 0.52, 0.155, 0.19, egg=0.12)
    p.line(ring + ring[:1], GAP)
    # three vertebral plates down the middle, ribs from their corners to the ring
    ys = (0.40, 0.52, 0.64)
    hw, hh = 0.055, 0.06
    for y in ys:
        hexa = [(0.5 - hw, y - hh * 0.5), (0.5, y - hh), (0.5 + hw, y - hh * 0.5),
                (0.5 + hw, y + hh * 0.5), (0.5, y + hh), (0.5 - hw, y + hh * 0.5)]
        p.line(hexa + hexa[:1], GAP)
    for y in (0.46, 0.58):
        p.line([(0.5 - hw, y), (0.345, y + 0.005)], GAP)
        p.line([(0.5 + hw, y), (0.655, y + 0.005)], GAP)


def draw_pagi(p):
    """A manta ray from above, head up: one broad wing with a curved leading edge, the two horn
    fins, a tail, and two chevron gaps across the back so it reads as a drawing, not a blot."""
    half = [(0.50, 0.345), (0.56, 0.335), (0.63, 0.315),
            (0.71, 0.32), (0.80, 0.355), (0.90, 0.43), (0.975, 0.515),
            (0.90, 0.535), (0.79, 0.565), (0.68, 0.61), (0.59, 0.67), (0.535, 0.72)]
    outline = half + [(1 - x, y) for x, y in half[::-1]][1:-1]
    p.fill(_catmull(outline, steps=8))
    p.fill(_blade((0.5, 0.69), (0.5, 0.83), (0.515, 0.965), 0.05, tip_round=0.05))
    # the horn fins CURL INWARD like a manta's (swatch v1's straight upright horns read as rabbit
    # ears): short paddles from the wing's front corners bending toward the mouth
    for side in (False, True):
        horn = _blade((0.605, 0.335), (0.625, 0.255), (0.555, 0.245), 0.052, tip_round=0.55)
        p.fill(_mirror(horn) if side else horn)
    for y0, span, dip in ((0.47, 0.30, 0.085), (0.57, 0.17, 0.06)):
        pts = [(0.5 - span, y0 - dip * 0.2), (0.5 - span * 0.5, y0 + dip * 0.35), (0.5, y0 + dip),
               (0.5 + span * 0.5, y0 + dip * 0.35), (0.5 + span, y0 - dip * 0.2)]
        p.line(_catmull(pts, closed=False), GAP)
    for x in (0.455, 0.545):
        p.dot(x, 0.365, 0.013)


def draw_bangka(p):
    """An outrigger boat under two sails, side view, bow right, with a sun behind and two waves
    under it. The boat the village lives by."""
    p.dot(0.25, 0.25, 0.085, on=True)                                   # the sun
    hull_top = [(0.13, 0.49), (0.30, 0.545), (0.50, 0.555), (0.70, 0.545), (0.88, 0.47)]
    hull_bot = [(0.84, 0.515), (0.68, 0.605), (0.50, 0.625), (0.32, 0.605), (0.17, 0.53)]
    p.fill(_catmull(hull_top + hull_bot, steps=8))
    # outrigger: two arched arms (katig) out to a float, thick enough to survive the distance
    p.line([(0.10, 0.665), (0.90, 0.665)], 0.032, on=True)
    for x in (0.33, 0.66):
        p.line(_catmull([(x - 0.12, 0.66), (x - 0.06, 0.575), (x, 0.555), (x + 0.06, 0.575),
                         (x + 0.12, 0.66)], closed=False), 0.026, on=True)
    p.line([(0.5, 0.56), (0.5, 0.12)], 0.028, on=True)                  # mast
    p.fill(_catmull([(0.53, 0.15), (0.64, 0.25), (0.76, 0.38), (0.82, 0.47), (0.53, 0.48)], steps=6))
    p.fill(_catmull([(0.47, 0.22), (0.40, 0.30), (0.33, 0.40), (0.30, 0.47), (0.47, 0.48)], steps=6))
    p.line([(0.515, 0.14), (0.515, 0.50)], GAP * 0.7)                   # gap between mast and sail
    p.line([(0.485, 0.20), (0.485, 0.50)], GAP * 0.7)
    for y in (0.78, 0.87):                                              # two waves
        pts = [(0.08 + k * 0.042, y - 0.028 * math.sin(k * math.pi / 3)) for k in range(21)]
        p.line(pts, 0.036, on=True)


DRAW = {"pawikan": draw_pawikan, "pagi": draw_pagi, "bangka": draw_bangka}


def _field(seed, sigma):
    """A smooth random field, unit variance, features about `sigma` px across. Smooth on purpose:
    the house style has no grain and no noise, only broad shapes (docs/KANTO_DESIGN_GUIDE.md § 3)."""
    import numpy as np
    from scipy.ndimage import gaussian_filter
    n = gaussian_filter(np.random.default_rng(seed).standard_normal((N, N)), sigma, mode="wrap")
    return (n - n.mean()) / (n.std() + 1e-9)


def _smooth(e0, e1, x):
    import numpy as np
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def paint(name, seed=0):
    """One emblem: a crisp drawing turned into worn lime paint on stone.

    1. The drawing is a crisp mask, turned into a SIGNED DISTANCE in pixels, so the edge can be
       moved in and out by a smooth field (the brush's wobble) instead of blurred.
    2. A slow warp bends every edge a little (a hand, not a ruler).
    3. The edge breaks: a medium field eats into it by up to ~1.5 cm, and in a narrow band just
       inside it a finer field frays it like a dry brush dragged over a rough stone.
    4. WEAR: a few broad patches where the paint has thinned (alpha down to ~0.55), and a few
       small chips where it has flaked off. Not a grain over everything: that read as a sticker
       printed with a texture, not paint that has been in the salt air for years.
    Returns an RGBA uint8 array at OUT_PX."""
    import numpy as np
    from PIL import Image
    from scipy.ndimage import distance_transform_edt, map_coordinates
    pen = _Pen()
    DRAW[name](pen)
    inside = np.asarray(pen.img) > 127
    sdf = distance_transform_edt(inside) - distance_transform_edt(~inside)
    s0 = seed * 101 + {"pawikan": 1, "pagi": 2, "bangka": 3}[name] * 7
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32)
    warp = 7.0
    sdf = map_coordinates(sdf, [yy + warp * _field(s0 + 1, 70), xx + warp * _field(s0 + 2, 70)],
                          order=1, mode="nearest")
    s = sdf + 5.0 * _field(s0 + 3, 22) + 2.2 * _field(s0 + 4, 7)
    a = _smooth(-1.8, 2.2, s)
    band = 1 - _smooth(3, 20, s)                                  # the dry-brush band, ~1 cm wide
    a *= 1 - _smooth(0.55, 1.15, _field(s0 + 5, 4.5)) * band
    # WORN-THIN PATCHES with a defined, feathered edge. Swatch v1 used a wide soft ramp and they
    # read as airbrushed smudges (the owner's word for that look was "garbage"); a narrow ramp on
    # a slow field makes a patch where the lime has weathered back, with a shape of its own.
    worn = _field(s0 + 6, 110) + 0.35 * _field(s0 + 9, 30)
    a *= 1 - 0.45 * _smooth(0.9, 1.07, worn)
    # FLAKES: a few, bigger than v1's pepper of dots (which read as a rubber stamp's speckle)
    a *= 1 - 0.92 * _smooth(2.25, 2.4, _field(s0 + 7, 20))
    warm = _smooth(0.2, 1.2, _field(s0 + 8, 220))[..., None]
    rgb = np.array(PAINT)[None, None] * (1 - warm) + np.array(PAINT_WARM)[None, None] * warm
    rgba = np.dstack([rgb, a[..., None]])
    img = Image.fromarray((np.clip(rgba, 0, 1) * 255).astype(np.uint8), "RGBA")
    return np.asarray(img.resize((OUT_PX, OUT_PX), Image.LANCZOS))


def paint_all():
    from PIL import Image
    TEX.mkdir(parents=True, exist_ok=True)
    for name in EMBLEMS:
        Image.fromarray(paint(name), "RGBA").save(_paths(name))
        print("[lagoon-landmark] painted", _paths(name))


def _font(size):
    from PIL import ImageFont
    for f in ("arialbd.ttf", "arial.ttf", "DejaVuSans-Bold.ttf"):
        try:
            return ImageFont.truetype(f, size)
        except OSError:
            pass
    return ImageFont.load_default()


def _new_version_path(stem, version):
    """Never overwrite a render (chat clients cache by filename, CLAUDE.md § 6.1)."""
    p = PREVIEWS / f"{stem}_v{version}.png"
    if p.exists():
        raise SystemExit(f"{p} exists: pick a new version number")
    return p


def swatches(version):
    """The three emblems on the approved rock_a, at a close size and at the size they cover from
    the court (about 70 px on a 1600 px frame), so the sheet answers "does it read from the
    court" before any render does."""
    import numpy as np
    from PIL import Image, ImageDraw
    rock = Image.open(TEX / "rock_a_albedo.png").convert("RGB")
    W, H = 620, 800
    sheet = Image.new("RGB", (W * len(EMBLEMS), H), (38, 30, 22))
    d = ImageDraw.Draw(sheet)
    for i, name in enumerate(EMBLEMS):
        em = Image.open(_paths(name)).convert("RGBA")
        bg = rock.resize((560, 560)).convert("RGBA")
        bg.alpha_composite(em.resize((560, 560), Image.LANCZOS))
        sheet.paste(bg.convert("RGB"), (i * W + 30, 70))
        small_bg = rock.crop((0, 0, 300, 300)).resize((110, 110)).convert("RGBA")
        small_bg.alpha_composite(em.resize((70, 70), Image.LANCZOS), (20, 20))
        sheet.paste(small_bg.convert("RGB"), (i * W + 30, 650))
        d.text((i * W + 30, 18), name, fill=(240, 230, 210), font=_font(34))
        d.text((i * W + 160, 685), "as seen from the court\n(~70 px)", fill=(220, 208, 186), font=_font(22))
        a = np.asarray(em)[..., 3] / 255.0
        d.text((i * W + 160, 745), f"paint cover {a.mean() * 100:.0f}%", fill=(180, 168, 146), font=_font(18))
    out = _new_version_path("landmark_swatches", version)
    sheet.save(out)
    print("[lagoon-landmark] sheet", out)


def compose(version, frames):
    """Lay the raw Blender frames out as the three review sheets, labelled."""
    from PIL import Image, ImageDraw
    frames = Path(frames)

    def labelled(path, w, text):
        im = Image.open(path).convert("RGB")
        im = im.resize((w, round(im.height * w / im.width)), Image.LANCZOS)
        ImageDraw.Draw(im).text((14, 10), text, fill=(255, 250, 235), font=_font(28),
                                stroke_width=3, stroke_fill=(30, 20, 12))
        return im

    for shot, w in (("court", 1600), ("close", 800)):
        ims = [labelled(frames / f"{shot}_{n}.png", w, n) for n in EMBLEMS]
        if shot == "court":
            sheet = Image.new("RGB", (w, sum(i.height for i in ims)))
            y = 0
            for im in ims:
                sheet.paste(im, (0, y))
                y += im.height
        else:
            sheet = Image.new("RGB", (w * len(ims), ims[0].height))
            for k, im in enumerate(ims):
                sheet.paste(im, (k * w, 0))
        out = _new_version_path(f"landmark_{shot}", version)
        sheet.save(out)
        print("[lagoon-landmark] sheet", out)
    big = labelled(frames / "backdrop_ref.png", 1600, "the cove, reference framing")
    row = [labelled(frames / f"backdrop_{k}.png", 533, t)
           for k, t in (("aerial", "aerial"), ("north", "game's eye, north"), ("east", "game's eye, east"))]
    sheet = Image.new("RGB", (1600, big.height + row[0].height))
    sheet.paste(big, (0, 0))
    for k, im in enumerate(row):
        sheet.paste(im, (k * 533, big.height))
    out = _new_version_path("landmark_backdrop", version)
    sheet.save(out)
    print("[lagoon-landmark] sheet", out)


# ---------------------------------------------------------------- the decal (Blender)

def emblem_material(name):
    """The paint: the emblem's albedo and alpha on the decal's UVs, times the "paint" attribute
    (fades on faces turning away and at the waterline), and modulated by the STONE underneath.
    ⚠️ Why the stone shows through: a flat off-white fill over a textured rock is exactly what
    reads as a sticker. Lime wash takes the tone of the surface it soaks into, so the paint is
    multiplied by the rock_a texture's own light and dark patches (sampled with the rock
    material's world box projection, so they line up with the rock around it), gently: 0.84 to
    1.06. Unity: the same multiply with the rock texture, triplanar in world space, or bake it."""
    import numpy as np
    m = bpy.data.materials.get(f"lm_emblem_{name}") or bpy.data.materials.new(f"lm_emblem_{name}")
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.inputs["Roughness"].default_value = 0.92       # chalky lime, no sheen
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    uv = nt.nodes.new("ShaderNodeUVMap")
    uv.uv_map = "UVMap"
    img = nt.nodes.new("ShaderNodeTexImage")
    img.image = bpy.data.images.load(str(_paths(name)), check_existing=True)
    img.extension = "CLIP"
    nt.links.new(uv.outputs["UV"], img.inputs["Vector"])
    fade = nt.nodes.new("ShaderNodeAttribute")
    fade.attribute_name = "paint"
    alpha = nt.nodes.new("ShaderNodeMath")
    alpha.operation = "MULTIPLY"
    nt.links.new(img.outputs["Alpha"], alpha.inputs[0])
    nt.links.new(fade.outputs["Fac"], alpha.inputs[1])
    nt.links.new(alpha.outputs["Value"], bsdf.inputs["Alpha"])
    # the stone under the paint
    rock = bpy.data.images.load(str(TEX / "rock_a_albedo.png"), check_existing=True)
    px = np.array(rock.pixels[:], dtype=np.float32).reshape(-1, 4)[:, :3]
    mean = float((px @ np.array([0.2126, 0.7152, 0.0722], dtype=np.float32)).mean())
    geo = nt.nodes.new("ShaderNodeNewGeometry")
    sc = nt.nodes.new("ShaderNodeVectorMath")
    sc.operation = "SCALE"
    sc.inputs["Scale"].default_value = 1.0 / 4.0        # render_lagoon_texture_preview.TILE_M
    nt.links.new(geo.outputs["Position"], sc.inputs[0])
    rimg = nt.nodes.new("ShaderNodeTexImage")
    rimg.image = rock
    rimg.projection, rimg.projection_blend = "BOX", 0.45
    nt.links.new(sc.outputs["Vector"], rimg.inputs["Vector"])
    lum = nt.nodes.new("ShaderNodeRGBToBW")
    nt.links.new(rimg.outputs["Color"], lum.inputs["Color"])
    rng = nt.nodes.new("ShaderNodeMapRange")
    rng.inputs["From Min"].default_value, rng.inputs["From Max"].default_value = mean * 0.7, mean * 1.3
    rng.inputs["To Min"].default_value, rng.inputs["To Max"].default_value = 0.84, 1.06
    nt.links.new(lum.outputs["Val"], rng.inputs["Value"])
    mul = nt.nodes.new("ShaderNodeMix")
    mul.data_type, mul.blend_type = "RGBA", "MULTIPLY"
    mul.inputs["Factor"].default_value = 1.0
    nt.links.new(img.outputs["Color"], mul.inputs["A"])
    grey = nt.nodes.new("ShaderNodeCombineColor")
    for ch in ("Red", "Green", "Blue"):
        nt.links.new(rng.outputs["Result"], grey.inputs[ch])
    nt.links.new(grey.outputs["Color"], mul.inputs["B"])
    nt.links.new(mul.outputs["Result"], bsdf.inputs["Base Color"])
    if hasattr(m, "surface_render_method"):
        m.surface_render_method = "DITHERED"
    return m


def apply_emblem(rock_obj, name, size_m=4.6, toward=COURT, lift=None, shift=0.0, offset=0.02,
                 water=WATER, cell=0.025):
    """Paint emblem `name` on `rock_obj`, on the face that looks toward `toward` (x, y), as a
    decal mesh parented to the rock. Replaces any emblem already on that rock. Returns the decal.

    size_m   the emblem's width and height, projected (a face turning away stretches it; the
             "paint" fade hides the worst of that).
    lift     the height of the emblem's centre in world z; by default as low as keeps its bottom
             0.35 m clear of the water, halfway up the room the stone leaves (paint at
             the waterline would be washed off; a mark on the crown would be seen against sky).
    shift    sideways offset in metres, positive to the viewer's right.
    offset   the stand-off along the surface normal (2 cm: no z-fighting, no visible gap).
    cell     the projection grid spacing. At 2.5 cm a chord across the stone's sharpest break dips
             under a centimetre, so the 2 cm stand-off always clears the surface.

    The grid is cast from a plane square to the court's direction, UPRIGHT (image up is world
    up), so the paint stands as a painter would put it on. A grid point is kept only if its ray
    hits the stone's front, and a quad only if its four hits are close in depth: a ray that slips
    past a bulge and lands on a face behind it would otherwise stretch the paint across the air."""
    from mathutils import Vector
    from mathutils.bvhtree import BVHTree
    bpy.context.view_layer.update()
    for o in list(bpy.data.objects):
        if o.get("emblem_of") == rock_obj.name:
            bpy.data.objects.remove(o)
    deps = bpy.context.evaluated_depsgraph_get()
    ev = rock_obj.evaluated_get(deps)
    me = ev.to_mesh()
    mw = rock_obj.matrix_world.copy()
    verts = [mw @ v.co for v in me.vertices]
    polys = [list(p.vertices) for p in me.polygons]
    ev.to_mesh_clear()
    tree = BVHTree.FromPolygons(verts, polys)
    top = max(v.z for v in verts)
    centre = sum(verts, Vector()) / len(verts)
    d = Vector((toward[0] - centre.x, toward[1] - centre.y, 0)).normalized()   # rock -> court
    view = -d
    right = view.cross(Vector((0, 0, 1))).normalized()
    up = Vector((0, 0, 1))
    if lift is None:
        lo, hi = water + 0.35 + size_m / 2, top - 0.3 - size_m / 2
        # halfway up the room (v1 sat a third of the way up and the turtle's back flippers
        # faded into the waterline fade)
        lift = lo + 0.5 * (hi - lo) if hi > lo else 0.5 * (lo + hi)
    reach = max((v - centre).length for v in verts) + 3.0
    base = Vector((centre.x, centre.y, lift)) + d * reach + right * shift
    n = max(8, int(round(size_m / cell)))
    grid = {}
    for j in range(n + 1):
        for i in range(n + 1):
            a, b = (i / n - 0.5) * size_m, (j / n - 0.5) * size_m
            o = base + right * a + up * b
            hit, nrm, _f, dist = tree.ray_cast(o, view, 2 * reach)
            if hit is None:
                continue
            facing = nrm.dot(d)
            if facing <= 0.02:
                continue
            # the paint fades as the stone turns away (a stretched stripe on a side face read as
            # a smear) and near the water, where waves wash it off
            f = _unit_smooth(0.12, 0.45, facing) * _unit_smooth(water + 0.1, water + 0.65, hit.z)
            grid[i, j] = (hit + nrm * offset, dist, (i / n, j / n), f)
    index, vco, uvs, fades, faces = {}, [], [], [], []
    jump = max(0.2, cell * 10)
    for j in range(n):
        for i in range(n):
            q = [(i, j), (i + 1, j), (i + 1, j + 1), (i, j + 1)]
            if not all(k in grid for k in q):
                continue
            ds = [grid[k][1] for k in q]
            if max(ds) - min(ds) > jump or max(grid[k][3] for k in q) <= 0.0:
                continue
            face = []
            for k in q:
                if k not in index:
                    index[k] = len(vco)
                    vco.append(grid[k][0])
                    uvs.append(grid[k][2])
                    fades.append(grid[k][3])
                face.append(index[k])
            faces.append(face)
    dm = bpy.data.meshes.new(f"lm_emblem_{name}")
    dm.from_pydata([tuple(v) for v in vco], [], faces)
    uvl = dm.uv_layers.new(name="UVMap")
    for poly in dm.polygons:
        for li in poly.loop_indices:
            uvl.data[li].uv = uvs[dm.loops[li].vertex_index]
    attr = dm.attributes.new("paint", "FLOAT", "POINT")
    attr.data.foreach_set("value", fades)
    for poly in dm.polygons:
        poly.use_smooth = True
    dm.materials.append(emblem_material(name))
    obj = bpy.data.objects.new(f"{rock_obj.name} emblem", dm)
    for c in rock_obj.users_collection:
        c.objects.link(obj)
    obj.parent = rock_obj
    obj.matrix_parent_inverse = mw.inverted()
    obj["emblem_of"] = rock_obj.name
    obj["emblem"] = name
    if hasattr(obj, "visible_shadow"):
        obj.visible_shadow = False       # paint casts no shadow; the stone already does
    print(f"[lagoon-landmark] emblem {name}: {len(faces)} quads, centre z {lift:.2f}, "
          f"facing ({d.x:.2f}, {d.y:.2f})")
    return obj


def _unit_smooth(e0, e1, x):
    t = min(1.0, max(0.0, (x - e0) / (e1 - e0)))
    return t * t * (3 - 2 * t)


# ---------------------------------------------------------------- the backdrop (Blender)

# The horizon, measured from the court's centre, 150 to 400 m out. Mostly behind the island's
# north and out to the open east and west; the SOUTH water view (the court's open side, the water
# village) stays open, so the one thing south of the court is the village and the landmark.
# ("island", x, y, width, height, seed, palms) | ("spires", x, y, height, count, seed)
BACKDROP = [
    # v1 review: at 24 to 30 m tall and 78 m spires, everything north hid behind the 50 m
    # massif from the reference framing, and the one island that showed was a long flat wall.
    # Heights now clear the massif from the cove's cameras; islands are humps, not slabs.
    ("island", -175, 240, 90, 38, 11, 6),     # north-west, behind the massif's west shoulder
    ("spires", -60, 330, 120, 3, 12),         # north, over the summit: needles that clear it
    ("island", 115, 320, 80, 48, 13, 4),      # north-north-east, the tallest island
    ("spires", 250, 160, 75, 2, 14),          # east-north-east
    ("island", 330, 10, 105, 30, 15, 7),      # the open east
    ("spires", 215, -175, 50, 2, 16),         # east-south-east: the one mark off the south-east
    ("island", -310, 60, 110, 36, 17, 5),     # the open west
    ("spires", -250, -150, 64, 3, 18),        # west-south-west, off the spit
]
HAZE = (0.56, 0.68, 0.64)      # the far colour. v2 used 0.70/0.79/0.80, near the sky's own
#                                horizon, and the islands read as pale grey-white cut-outs; v3's
#                                0.60/0.70/0.69 still read grey, so both lean green (v4)
FAR_ROCK = (0.26, 0.40, 0.32)  # grey-green stone before the haze
FAR_PALM = (0.24, 0.36, 0.28)


def haze_material(name, base):
    """AERIAL PERSPECTIVE as a material: the object's own shade (lighter tops by face normal)
    mixed toward a flat HAZE by its distance from the court (Object Info location), plus a little
    more at its foot where sea mist sits. Nearer masses keep more shape; the farthest are almost
    silhouettes. Emission for the haze so shadowed sides lift too: distance flattens shadow, which
    is what sells it. Unity: an unlit or simple-lit shader with the same lerp, or bake colours."""
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.inputs["Roughness"].default_value = 1.0
    geo = nt.nodes.new("ShaderNodeNewGeometry")
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    nt.links.new(geo.outputs["Normal"], sep.inputs[0])
    tone = nt.nodes.new("ShaderNodeMapRange")
    tone.inputs["From Min"].default_value, tone.inputs["From Max"].default_value = -0.3, 1.0
    tone.inputs["To Min"].default_value, tone.inputs["To Max"].default_value = 0.85, 1.2
    nt.links.new(sep.outputs["Z"], tone.inputs["Value"])
    col = nt.nodes.new("ShaderNodeMix")
    col.data_type, col.blend_type = "RGBA", "MULTIPLY"
    col.inputs["Factor"].default_value = 1.0
    col.inputs["A"].default_value = (*base, 1)
    grey = nt.nodes.new("ShaderNodeCombineColor")
    for ch in ("Red", "Green", "Blue"):
        nt.links.new(tone.outputs["Result"], grey.inputs[ch])
    nt.links.new(grey.outputs["Color"], col.inputs["B"])
    nt.links.new(col.outputs["Result"], bsdf.inputs["Base Color"])
    info = nt.nodes.new("ShaderNodeObjectInfo")
    dist = nt.nodes.new("ShaderNodeVectorMath")
    dist.operation = "LENGTH"
    nt.links.new(info.outputs["Location"], dist.inputs[0])
    far = nt.nodes.new("ShaderNodeMapRange")
    far.inputs["From Min"].default_value, far.inputs["From Max"].default_value = 150, 420
    # v1 ran 0.52 to 0.78 and the east island dissolved into a flat sky-coloured wall
    far.inputs["To Min"].default_value, far.inputs["To Max"].default_value = 0.30, 0.55
    nt.links.new(dist.outputs["Value"], far.inputs["Value"])
    pos = nt.nodes.new("ShaderNodeSeparateXYZ")
    nt.links.new(geo.outputs["Position"], pos.inputs[0])
    foot = nt.nodes.new("ShaderNodeMapRange")
    foot.inputs["From Min"].default_value, foot.inputs["From Max"].default_value = 0, 18
    foot.inputs["To Min"].default_value, foot.inputs["To Max"].default_value = 0.14, 0.0
    nt.links.new(pos.outputs["Z"], foot.inputs["Value"])
    k = nt.nodes.new("ShaderNodeMath")
    k.operation = "ADD"
    k.use_clamp = True
    nt.links.new(far.outputs["Result"], k.inputs[0])
    nt.links.new(foot.outputs["Result"], k.inputs[1])
    em = nt.nodes.new("ShaderNodeEmission")
    em.inputs["Color"].default_value = (*HAZE, 1)
    em.inputs["Strength"].default_value = 1.0
    mix = nt.nodes.new("ShaderNodeMixShader")
    nt.links.new(k.outputs["Value"], mix.inputs["Fac"])
    nt.links.new(bsdf.outputs["BSDF"], mix.inputs[1])
    nt.links.new(em.outputs["Emission"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    return m


def _kit_meshes(kit):
    """Private COPIES of the kit stones for the backdrop. ⚠️ Not the cove's own meshes: the cove
    treats every mesh carrying "rock_family" as a rock (the edge bake, rock_scale, cull_buried's
    ray tests), and a 100 m backdrop stone must not join any of that. The copies drop the tag."""
    import author_lagoon_rocks as R
    if not kit:
        kit = [m for m in bpy.data.meshes if m.get("rock_family") and m.name.startswith("rock_")
               and not m.name.startswith("lm_")]
        order = {f"rock_{f}_{R.ROCK_FAMILIES[f].index(i) + 1:02d}": i for i, (f, _s) in enumerate(R.STONES)}
        kit = sorted([m for m in kit if m.name in order], key=lambda m: order[m.name])
        if len(kit) != R.ROCK_KIT_SIZE:
            kit = R.build_rock_kit()
    copies = {}

    def get(family, k):
        idx = R.ROCK_FAMILIES[family][k % len(R.ROCK_FAMILIES[family])]
        if idx not in copies:
            src = kit[idx]
            me = src.copy()
            me.name = f"lm_far_{src.name}"
            top = src["rock_top_z"]
            for key in list(me.keys()):
                del me[key]
            me["far_top_z"] = top
            copies[idx] = me
        return copies[idx]
    return get


def _palm_mesh():
    """A far-off coconut palm as a SILHOUETTE: a curved tapering trunk and a crown of drooping
    flat fronds. At 200 to 400 m it is a few dozen pixels, so shape is all it needs."""
    import bmesh
    from mathutils import Vector
    me = bpy.data.meshes.get("lm_far_palm")
    if me is not None:
        return me
    bm = bmesh.new()
    rings, sides, height = 8, 6, 13.0
    prev = None
    for r in range(rings + 1):
        t = r / rings
        cx, cz = 2.2 * t * t, height * t                       # a lean that grows toward the top
        rad = 0.55 * (1 - 0.45 * t)
        ring = [bm.verts.new((cx + rad * math.cos(a), rad * math.sin(a), cz))
                for a in (math.tau * s / sides for s in range(sides))]
        if prev:
            for s in range(sides):
                bm.faces.new((prev[s], prev[(s + 1) % sides], ring[(s + 1) % sides], ring[s]))
        prev = ring
    crown = Vector((2.2, 0, height))
    for f in range(8):
        a = math.tau * f / 8 + 0.2
        dirv = Vector((math.cos(a), math.sin(a), 0))
        side = Vector((-dirv.y, dirv.x, 0))
        segs, length = 5, 6.0
        row = None
        for s in range(segs + 1):
            t = s / segs
            p = crown + dirv * (length * t) + Vector((0, 0, 1.6 * t - 4.2 * t * t))
            w = 1.1 * math.sin(math.pi * min(1.0, 0.15 + t)) + 0.05
            pair = (bm.verts.new(p + side * w), bm.verts.new(p - side * w))
            if row:
                bm.faces.new((row[0], row[1], pair[1], pair[0]))
            row = pair
    me = bpy.data.meshes.new("lm_far_palm")
    bm.to_mesh(me)
    bm.free()
    return me


def build_backdrop(c, kit=None):
    """Distant islands and rock spires on the horizon, in collection `c`. `kit` is the cove's
    rock kit (author_lagoon_cove.KIT, 16 meshes in author_lagoon_rocks.STONES order); None finds
    or builds it. Islands are one big stone (a boulder or slab stretched long and low) with three
    to five smaller stones against it for a lumpy skyline, and palms on the crown; spires are the
    kit's stacks stretched tall, in a tight group. Everything shares linked meshes.
    Returns the objects made."""
    import random
    from mathutils import Vector
    from mathutils.bvhtree import BVHTree
    get = _kit_meshes(kit)
    rock_m = haze_material("lm_far_rock", FAR_ROCK)
    palm_m = haze_material("lm_far_palm", FAR_PALM)
    palm = _palm_mesh()
    if not palm.materials:
        palm.materials.append(palm_m)
    made = []

    def stone(me, loc, scale, turn):
        o = bpy.data.objects.new("backdrop stone", me)
        o.location, o.scale, o.rotation_euler = loc, scale, (0, 0, turn)
        c.objects.link(o)
        if not o.material_slots:
            o.data.materials.append(rock_m)
        o.material_slots[0].link = "OBJECT"
        o.material_slots[0].material = rock_m
        made.append(o)
        return o

    for spec in BACKDROP:
        rng = random.Random(spec[-2] if spec[0] == "island" else spec[-1])
        kind, x, y = spec[:3]
        face = math.atan2(-y, -x)                     # the long side faces the court
        if kind == "island":
            _k, _x, _y, width, height, _seed, palms = spec
            # A HUMP, not a wall: v1 stretched one stone 4:1 along the horizon and it read as a
            # flat slab. The body is one boulder a bit over half the width, and the stones along
            # it fall off in height toward the ends (a dome profile), so the skyline is lumpy.
            me = get("boulder", rng.randrange(5))
            t = face + math.pi / 2
            sxy = width * 0.55 / 2.5                     # kit stones are 2.5 m across at scale 1
            body = stone(me, (x, y, WATER - 2.0), (sxy, sxy * 0.7, height / me["far_top_z"]),
                         t + rng.uniform(-0.2, 0.2))
            parts = [body]
            n_side = rng.randint(4, 6)
            for k in range(n_side):
                me2 = get(rng.choice(("boulder", "boulder", "cobble", "split")), rng.randrange(5))
                along = (-0.5 + (k + rng.uniform(0.2, 0.8)) / n_side) * width * 0.95
                px, py = x + math.cos(t) * along, y + math.sin(t) * along
                px += rng.uniform(-0.1, 0.1) * width
                py += rng.uniform(-0.1, 0.1) * width
                dome = math.sqrt(max(0.0, 1 - (2 * along / width) ** 2))
                h = height * rng.uniform(0.45, 0.85) * dome
                s = width * rng.uniform(0.2, 0.3) / 2.5
                parts.append(stone(me2, (px, py, WATER - 1.5), (s, s * rng.uniform(0.8, 1.1),
                                                                   max(5.0, h) / me2["far_top_z"]),
                                   rng.uniform(0, math.tau)))
            # palms on the crown, dropped onto the island's surface by ray
            bpy.context.view_layer.update()
            vs, ps = [], []
            for o in parts:
                b = len(vs)
                vs.extend(o.matrix_world @ v.co for v in o.data.vertices)
                ps.extend([b + i for i in p.vertices] for p in o.data.polygons)
            tree = BVHTree.FromPolygons(vs, ps)
            placed = tries = 0
            while placed < palms and tries < palms * 20:
                tries += 1
                along = rng.uniform(-0.38, 0.38) * width
                px, py = x + math.cos(t) * along, y + math.sin(t) * along
                px += rng.uniform(-0.1, 0.1) * width
                py += rng.uniform(-0.1, 0.1) * width
                hit, nrm, _i, _d = tree.ray_cast(Vector((px, py, 400)), Vector((0, 0, -1)))
                if hit is None or nrm.z < 0.55 or hit.z < WATER + 3:
                    continue
                p = bpy.data.objects.new("backdrop palm", palm)
                p.location = (hit.x, hit.y, hit.z - 0.8)
                sc = rng.uniform(0.85, 1.25)
                p.scale = (sc, sc, sc)
                p.rotation_euler = (0, 0, rng.uniform(0, math.tau))
                c.objects.link(p)
                made.append(p)
                placed += 1
        else:
            _k, _x, _y, height, count, _seed = spec
            # v2: at 0.32 to 0.42 of their height across, the lone north needle read as an obelisk
            for k in range(count):
                me = get("stack", rng.choice((1, 2, 1)))
                h = height * (1.0 if k == 0 else rng.uniform(0.45, 0.75))
                s = h / me["far_top_z"]
                off = 0 if k == 0 else rng.uniform(9, 18)
                a = rng.uniform(0, math.tau)
                stone(me, (x + math.cos(a) * off, y + math.sin(a) * off, WATER - 2.0),
                      (s * rng.uniform(0.5, 0.62), s * rng.uniform(0.5, 0.62), s), rng.uniform(0, math.tau))
    print(f"[lagoon-landmark] backdrop: {len(made)} objects in {len(BACKDROP)} groups")
    return made


# ---------------------------------------------------------------- the test (Blender)

def _look(cam, pos, tgt):
    from mathutils import Vector
    cam.location = pos
    cam.rotation_euler = (Vector(tgt) - Vector(pos)).to_track_quat("-Z", "Y").to_euler()


def test(version, recommended="pawikan"):
    """Open lagoon_cove.blend READ-ONLY (never saved), paint each emblem in turn, build the
    backdrop, render, compose. Frames go to the temp folder; only the sheets land in Logs."""
    from mathutils import Vector
    if not bpy.data.filepath.endswith("lagoon_cove.blend"):
        raise SystemExit("open ArtSource/lagoon/lagoon_cove.blend first (blender -b <it> --python ...)")
    for stem in ("court", "close", "backdrop"):
        if (PREVIEWS / f"landmark_{stem}_v{version}.png").exists():
            raise SystemExit(f"landmark_{stem}_v{version}.png exists: pick a new version")
    frames = Path(tempfile.gettempdir()) / f"lagoon_landmark_frames_v{version}"
    frames.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 900
    if hasattr(scene.eevee, "shadow_pool_size"):
        scene.eevee.shadow_pool_size = "1024"
    try:
        scene.view_settings.view_transform = "AgX"
        scene.view_settings.look = "AgX - Punchy"
    except TypeError:
        pass
    cam = bpy.data.objects.new("lm cam", bpy.data.cameras.new("lm cam"))
    cam.data.clip_end = 2000
    cam.data.sensor_fit = "HORIZONTAL"
    scene.collection.objects.link(cam)
    scene.camera = cam
    rock = bpy.data.objects["landmark rock"]
    eye = 18 / math.tan(math.radians(95 / 2))

    def shot(path, pos, tgt, lens):
        cam.data.lens = lens
        _look(cam, pos, tgt)
        scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        print("[lagoon-landmark] frame", path)

    root = bpy.data.collections.new("Backdrop")
    scene.collection.children.link(root)
    build_backdrop(root, None)
    for name in EMBLEMS:
        dec = apply_emblem(rock, name)
        bpy.context.view_layer.update()
        cs = [dec.matrix_world @ Vector(v) for v in dec.bound_box]
        mid = sum(cs, Vector()) / 8
        shot(frames / f"court_{name}.png", (0, -9, 1.3), mid, eye)
        d = Vector((COURT[0] - mid.x, COURT[1] - mid.y, 0)).normalized()
        shot(frames / f"close_{name}.png", mid + d * 13 + Vector((1.5, 0, 0.9)), mid, 35)
    apply_emblem(rock, recommended)
    shot(frames / "backdrop_ref.png", (40, -100, 20), (-6, 14, 8), 24)
    shot(frames / "backdrop_aerial.png", (60, -170, 110), (0, -10, 0), 26)
    shot(frames / "backdrop_north.png", (0, -9, 1.3), (0, 45, 8), eye)
    shot(frames / "backdrop_east.png", (-9, 0, 1.3), (45, 0, 4), eye)
    subprocess.run(["py", "-3", str(Path(__file__).resolve()), "--compose", str(version), str(frames)],
                   check=True)
    print("[lagoon-landmark] test done; lagoon_cove.blend was NOT saved")


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
    if IN_BLENDER:
        sys.path.insert(0, str(Path(__file__).resolve().parent))
        if "--test" in argv:
            test(int(argv[argv.index("--test") + 1]))
        return
    if "--paint" in argv:
        paint_all()
    if "--swatches" in argv:
        swatches(int(argv[argv.index("--swatches") + 1]))
    if "--compose" in argv:
        k = argv.index("--compose")
        compose(int(argv[k + 1]), argv[k + 2])


if __name__ == "__main__":
    main()
