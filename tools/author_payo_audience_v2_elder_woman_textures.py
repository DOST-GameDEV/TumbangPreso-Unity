"""Paint the atlas of the Payo audience v2 villager `elder_woman`, by hand, in the Classic finish.

    py -3 tools/author_payo_audience_v2_elder_woman_textures.py [--size 1024] [--sheet file.png]

Writes ArtSource/payo/audience/v2/elder_woman/audience_v2_elder_woman-atlas.png. The model is built by
tools/author_payo_audience_v2_elder_woman.py, which imports this file for the LAYOUT only (PIL is
imported inside the painting calls; Blender's Python has none). Paint first, then build.

A COPY OF tools/author_character_redesign_lola_pacing_textures.py rewritten for her
(docs/reports/character-redesign/payo-audience-brief.md: copy the nearest approved Classic).

WHO SHE IS. A precolonial Ifugao grandmother in the crowd on the Payo basin wall: gray hair in a low
bun wound with a bead string, an undyed white woven blanket with black and madder-red bands over her
shoulders (her focal piece, tied at the chest), a white-ground tapis with indigo and red bands, a few
strands of old beads, bare feet, and a lidded rattan basket beside her. Content and warm.

THE FINISH (owner, 2026-10-19, on the rejected v1 crowd: "compare the textures to the cast, they
look noisy"). The same finish as Lola Pacing: CLEAN FLAT TONES per form, a base, a shade and a light
step chosen by facing; no noise, no grain, no brush texture, no jitter. Every woven band, the belt,
the beads, the hems and the basket's rattan bands are GEOMETRY in flat tones, as the Classics build
their trim. The only drawings are her face, the fingers on her fists, one row of hand-set diamonds
on the back of the blanket, the diamonds on the tapis' indigo band, and the diamonds on the basket's
sides: each a clean hard-edged polygon with its own hand-set numbers.

HER COLOURS (QUIET, the brief's "Colour"): undyed cream for the blanket, a cooler white for the tapis
ground, a dark indigo-black, a desaturated madder red ONLY in thin bands and beads, natural rattan,
brass as the one metal, warm gray salt-and-pepper hair (Lola Pacing's is a cool lavender silver),
skin in the heroes' range. Every cloth, hair and prop colour is checked against the role hues
(#f87020, #0080e8) in `main`.
"""
import colorsys
import math
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "ArtSource" / "payo" / "audience" / "v2" / "elder_woman"
ATLAS_NAME = "audience_v2_elder_woman-atlas.png"
ATLAS = 2048          # the file is square; only the top half carries paint
GUTTER = 4
SS = 2

# ---------------------------------------------------------------------------
# THE COLOURS. Each family is a base with a light and a shade step (and a deep one where a piece
# turns under), spread as far apart as the Classics' steps.
# ---------------------------------------------------------------------------
SKIN = "d9a077"; SKIN_LIT = "e8b892"; SKIN_SHADE = "c08660"; SKIN_DEEP = "a2694a"
BLUSH = "dc7c70"
INK = "1a1620"
HAIR = "bdb8af"; HAIR_LIT = "e4e0d8"; HAIR_DARK = "908b83"; HAIR_DEEP = "68645e"
CREAM = "e6ddca"; CREAM_LIT = "f4eee2"; CREAM_SHADE = "cbc0aa"; CREAM_DEEP = "a89d88"     # the undyed blanket
WHITE = "eeeae2"; WHITE_LIT = "faf8f3"; WHITE_SHADE = "d2ccc0"                            # the tapis ground
INDIGO = "2e3550"; INDIGO_LIT = "434b6c"; INDIGO_DEEP = "1d2135"
RED = "9e3b40"; RED_LIT = "b85a5c"; RED_DARK = "7a2c32"                                  # madder, thin bands only
BLACK = "1e1c20"; BLACK_LIT = "38353b"; BLACK_DEEP = "121114"
BONE = "e8e1d0"; BONE_SHADE = "c8bfa9"                                                      # old bone beads
BRASS = "b39545"; BRASS_LIT = "d0b468"; BRASS_DARK = "8a7134"
RATTAN = "b8996a"; RATTAN_LIT = "d1b88c"; RATTAN_DARK = "8f7756"; RATTAN_DEEP = "6a5640"

CLOTH_HEXES = [HAIR, HAIR_LIT, HAIR_DARK, HAIR_DEEP, CREAM, CREAM_LIT, CREAM_SHADE, CREAM_DEEP, WHITE, WHITE_LIT,
               WHITE_SHADE, INDIGO, INDIGO_LIT, INDIGO_DEEP, RED, RED_LIT, RED_DARK, BLACK, BLACK_LIT, BLACK_DEEP,
               BONE, BONE_SHADE, BRASS, BRASS_LIT, BRASS_DARK, RATTAN, RATTAN_LIT, RATTAN_DARK, RATTAN_DEEP]

# ---------------------------------------------------------------------------
# MEASURES THE MODEL SCRIPT SHARES (Blender space: +x her left, -y the way she faces, z up), read off
# character-female-d.glb as Lola Pacing's are.
# ---------------------------------------------------------------------------
FRONT_Y = -0.0804               # the flat front of the torso block
WAIST = 0.2147                  # the tapis starts here
ELBOW = 0.2097                  # along the arm (x): the elbow joint
WRIST = 0.3440                  # where the fist block starts (the far 14 per cent of the arm)
HAND_END = 0.3834
ARM_Y_PAINT = 0.01725

#   the tapis, from the top down: (name, z top, z foot). The belt covers its top.
TAPIS_BANDS = [("white", WAIST, 0.176), ("indigo", 0.176, 0.150), ("red", 0.150, 0.137), ("white", 0.137, 0.112),
               ("indigo", 0.112, 0.094)]
TAPIS_HEM = 0.094
#   the blanket's hem bands, from the hem up
MANTLE_HEM = 0.234

#   THE BASKET, beside her left foot, on the root bone: its middle (x, y), its half sizes at its foot
#   and at its rim, and its heights
BASKET_C = (0.315, 0.020)
BASKET_FOOT = (0.060, 0.054)
BASKET_RIM = (0.071, 0.064)
BASKET_TOP = 0.122              # the rim; the lid sits on it
BASKET_LID = 0.152
BASKET_KNOB = 0.172

# ---------------------------------------------------------------------------
# THE ISLANDS. group -> view -> (window in metres, px per metre at 2048).
# ---------------------------------------------------------------------------
VIEW_AXES = {"front": (0, 2), "back": (0, 2), "xpos": (1, 2), "xneg": (1, 2), "top": (0, 1), "bottom": (0, 1)}
VIEW_DIR = {"front": (0, -1, 0), "back": (0, 1, 0), "xpos": (1, 0, 0), "xneg": (-1, 0, 0),
            "top": (0, 0, 1), "bottom": (0, 0, -1)}

_BX, _BY = BASKET_C
GROUPS = {
    "head": {
        "front": ((-0.19, 0.19, 0.33, 0.68), 1500),
    },
    # the blanket's back panel (its back view only, so its front takes the flat tone) and the tapis'
    # upper indigo band are the only drawn cloth
    "mantle": {
        "back": ((-0.19, 0.19, 0.23, 0.36), 1000),
    },
    "tapis": {
        "front": ((-0.19, 0.19, 0.146, 0.180), 1100),
        "back": ((-0.19, 0.19, 0.146, 0.180), 1000),
    },
    "armL": {
        "front": ((0.30, 0.40, 0.22, 0.36), 1000),
        "back": ((0.30, 0.40, 0.22, 0.36), 900),
    },
    "armR": {
        "front": ((-0.40, -0.30, 0.22, 0.36), 1000),
        "back": ((-0.40, -0.30, 0.22, 0.36), 900),
    },
    "basket": {
        "front": ((_BX - 0.09, _BX + 0.09, 0.0, 0.13), 900),
        "back": ((_BX - 0.09, _BX + 0.09, 0.0, 0.13), 700),
        "xpos": ((_BY - 0.08, _BY + 0.08, 0.0, 0.13), 800),
        "xneg": ((_BY - 0.08, _BY + 0.08, 0.0, 0.13), 700),
    },
}
VIEW_BIAS = {}
SWATCHES = {}

FLATS = {
    "hair_lit": HAIR_LIT, "hair": HAIR, "hair_dark": HAIR_DARK, "hair_under": HAIR_DEEP,
    "skin_lit": SKIN_LIT, "skin": SKIN, "skin_shade": SKIN_SHADE, "skin_deep": SKIN_DEEP,
    "cream_lit": CREAM_LIT, "cream": CREAM, "cream_shade": CREAM_SHADE, "cream_deep": CREAM_DEEP,
    "white_lit": WHITE_LIT, "white": WHITE, "white_shade": WHITE_SHADE,
    "indigo_lit": INDIGO_LIT, "indigo": INDIGO, "indigo_deep": INDIGO_DEEP,
    "red_lit": RED_LIT, "red": RED, "red_dark": RED_DARK,
    "black_lit": BLACK_LIT, "black": BLACK, "black_deep": BLACK_DEEP,
    "bone": BONE, "bone_shade": BONE_SHADE,
    "brass_lit": BRASS_LIT, "brass": BRASS, "brass_dark": BRASS_DARK,
    "rattan_lit": RATTAN_LIT, "rattan": RATTAN, "rattan_dark": RATTAN_DARK, "rattan_deep": RATTAN_DEEP,
}
FLAT_PX = 40

_LAYOUT = {}


def layout(size=ATLAS):
    """name -> (x, y, w, h) px in the atlas, top-left origin. A shelf pack into the top half."""
    if size in _LAYOUT:
        return _LAYOUT[size]
    k = size / float(ATLAS)
    gap = max(2, int(round((2 * GUTTER + 2) * k)))
    for step in range(40):
        s = k * (1.0 - 0.02 * step)
        items = []
        for group, views in GROUPS.items():
            for view, (win, density) in views.items():
                items.append((group + "." + view, int(round((win[1] - win[0]) * density * s)),
                              int(round((win[3] - win[2]) * density * s))))
        for name in FLATS:
            items.append((name, max(8, int(round(FLAT_PX * k))), max(8, int(round(FLAT_PX * k)))))
        items.sort(key=lambda it: (-it[2], it[0]))
        out, x, y, shelf = {}, gap, gap, 0
        for name, w, h in items:
            if x + w + gap > size:
                x, y, shelf = gap, y + shelf + gap, 0
            out[name] = (x, y, w, h)
            x += w + gap
            shelf = max(shelf, h)
        if y + shelf + gap <= size // 2:
            _LAYOUT[size] = out
            return out
    raise SystemExit("the islands do not fit the top half of the atlas")


def window(name):
    if "." in name:
        group, view = name.split(".")
        return GROUPS[group][view][0]
    return (0.0, 1.0, 0.0, 1.0)


def atlas_uv(name, a, b, size=ATLAS):
    x, y, w, h = layout(size)[name]
    a0, a1, b0, b1 = window(name)
    fa = min(max((a - a0) / (a1 - a0), 0.0), 1.0)
    fb = min(max((b - b0) / (b1 - b0), 0.0), 1.0)
    px = x + 0.5 + fa * (w - 1.0)
    py = y + 0.5 + (1.0 - fb) * (h - 1.0)
    return (px / size, 1.0 - py / size)


def _rgb(hex_str):
    h = hex_str.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def _near_role_hue(hex_str):
    r, g, b = (c / 255.0 for c in _rgb(hex_str))
    h, s, v = colorsys.rgb_to_hsv(r, g, b)
    for role in ("f87020", "0080e8"):
        hr = colorsys.rgb_to_hsv(*(c / 255.0 for c in _rgb(role)))[0]
        dh = abs(h - hr)
        dh = min(dh, 1 - dh)
        if dh < 0.05 and s > 0.45 and v > 0.45:
            return role
    return None


def _spline(points, closed=False, steps=10):
    pts = list(points)
    n = len(pts)
    if n < 3:
        return pts
    out = []
    last = n if closed else n - 1
    for i in range(last):
        p0 = pts[(i - 1) % n] if closed or i > 0 else pts[0]
        p1 = pts[i]
        p2 = pts[(i + 1) % n]
        p3 = pts[(i + 2) % n] if closed or i + 2 < n else pts[n - 1]
        for k in range(steps):
            t = k / float(steps)
            t2, t3 = t * t, t * t * t
            out.append(tuple(
                0.5 * ((2 * p1[c]) + (-p0[c] + p2[c]) * t + (2 * p0[c] - 5 * p1[c] + 4 * p2[c] - p3[c]) * t2
                       + (-p0[c] + 3 * p1[c] - 3 * p2[c] + p3[c]) * t3) for c in range(2)))
    if not closed:
        out.append(pts[-1])
    return out


class Island:
    """One island; every mark a clean hard-edged polygon (a 0.3 mm feather only to anti-alias)."""

    def __init__(self, name, base, size, metres=None):
        from PIL import Image
        self.name = name
        x, y, w, h = layout(size)[name]
        self.rect = (x, y, w, h)
        self.w, self.h = w * SS, h * SS
        self.win = window(name)
        a0, a1, b0, b1 = self.win
        span_a, span_b = (a1 - a0, b1 - b0) if metres is None else metres
        self.mm = 0.5 * (self.w / span_a + self.h / span_b) / 1000.0
        self.img = Image.new("RGB", (self.w, self.h), _rgb(base))

    def px(self, p):
        a0, a1, b0, b1 = self.win
        return ((p[0] - a0) / (a1 - a0) * self.w, (b1 - p[1]) / (b1 - b0) * self.h)

    def _lay(self, colour, mask, feather, strength):
        from PIL import Image, ImageFilter
        if feather > 0:
            mask = mask.filter(ImageFilter.GaussianBlur(feather * self.mm))
        if strength < 1.0:
            mask = mask.point(lambda v: int(v * strength))
        self.img.paste(Image.new("RGB", (self.w, self.h), _rgb(colour)), (0, 0), mask)
        return self

    def mark(self, colour, points, feather=0.3, strength=1.0, curved=True):
        from PIL import Image, ImageDraw
        pts = _spline(points, closed=True) if curved else list(points)
        mask = Image.new("L", (self.w, self.h), 0)
        ImageDraw.Draw(mask).polygon([self.px(p) for p in pts], fill=255)
        return self._lay(colour, mask, feather, strength)

    def blob(self, colour, centre, ra, rb, feather=0.3, strength=1.0):
        from PIL import Image, ImageDraw
        mask = Image.new("L", (self.w, self.h), 0)
        x0, y0 = self.px((centre[0] - ra, centre[1] + rb))
        x1, y1 = self.px((centre[0] + ra, centre[1] - rb))
        ImageDraw.Draw(mask).ellipse([x0, y0, x1, y1], fill=255)
        return self._lay(colour, mask, feather, strength)

    def stroke(self, colour, points, width, feather=0.3, strength=1.0, taper=(0.25, 0.25), curved=True):
        from PIL import Image, ImageDraw
        pts = [self.px(p) for p in (_spline(points) if curved else points)]
        n = len(pts)
        left, right = [], []
        for i, (x, y) in enumerate(pts):
            ax, ay = pts[max(i - 1, 0)]
            bx, by = pts[min(i + 1, n - 1)]
            dx, dy = bx - ax, by - ay
            d = math.hypot(dx, dy) or 1.0
            t = i / float(max(n - 1, 1))
            ease = min(1.0, t / 0.3) if t < 0.3 else min(1.0, (1.0 - t) / 0.3)
            end = taper[0] if t < 0.5 else taper[1]
            half = 0.5 * width * self.mm * (end + (1.0 - end) * math.sin(ease * math.pi / 2.0))
            left.append((x - dy / d * half, y + dx / d * half))
            right.append((x + dy / d * half, y - dx / d * half))
        mask = Image.new("L", (self.w, self.h), 0)
        ImageDraw.Draw(mask).polygon(left + right[::-1], fill=255)
        return self._lay(colour, mask, feather, strength)

    def finished(self):
        from PIL import Image
        return self.img.resize((self.rect[2], self.rect[3]), Image.LANCZOS)


# ---------------------------------------------------------------------------
# THE HEAD. Drawn as the heroes' faces are: one solid ink block for each eye at the heroes' size and
# height, one thin stroke for the mouth, a round blush (she is female). No nose, nothing of age.
# HER EYES ARE NOT LOLA PACING'S. Lola's are smile arcs standing on two feet (pleased, sly). Hers are
# soft upright blocks whose top edge SLOPES DOWN TO THE OUTER CORNER and whose foot is rounded: the
# kind, contented droop of a grandmother watching her grandchildren play. 47 by 37 mm, a little
# under Amihan's, at the heroes' eye line.
# ---------------------------------------------------------------------------
EYE_CENTRE = (0.076, 0.479)
#   her LEFT eye about its middle, (dx, dz), the outer corner at +dx; mirrored for the right
EYE = [(-0.0250, 0.0190), (-0.0070, 0.0195), (0.0110, 0.0160), (0.0235, 0.0080), (0.0270, -0.0030),
       (0.0210, -0.0170), (0.0080, -0.0225), (-0.0080, -0.0225), (-0.0210, -0.0170), (-0.0270, -0.0020)]
#   her mouth: a soft smile, both ends lifted, a little narrower than Lola's
MOUTH = [(-0.0255, 0.4265), (-0.0140, 0.4180), (0.0, 0.4155), (0.0140, 0.4180), (0.0255, 0.4265)]
MOUTH_WIDTH = 7.8
HAIRLINE = 0.590


def paint_head_front(c):
    # flat skin, the one soft lit patch the Classics carry, a round blush, the ink
    c.blob(SKIN_LIT, (0.0, 0.452), 0.105, 0.065, 26, 0.30)
    c.blob(BLUSH, (-0.104, 0.429), 0.029, 0.019, 6, 0.55)
    c.blob(BLUSH, (0.104, 0.429), 0.029, 0.019, 6, 0.55)
    for s in (1, -1):
        c.mark(INK, [(s * (EYE_CENTRE[0] + dx), EYE_CENTRE[1] + dz) for dx, dz in EYE], 0.3, 1.0, curved=True)
    c.stroke(INK, MOUTH, MOUTH_WIDTH, 0.3, 1.0, (0.62, 0.62))
    for end in (MOUTH[0], MOUTH[-1]):
        c.blob(INK, end, 0.0026, 0.0026, 0.25, 1.0)


# ---------------------------------------------------------------------------
# THE CLOTH. Only two drawings, both clean diamond rows with hand-set numbers:
#   the BACK OF THE BLANKET, between its shoulder bands and its hem bands: five black diamonds, the
#       middle one the largest, two small red ones between (an Ifugao weave's centre field)
#   the tapis' upper INDIGO BAND (0.150 to 0.176): cream diamonds, front and back, different counts
# Everything else on the cloth is geometry in flat tones.
# ---------------------------------------------------------------------------

def _diamond(c, colour, x, z, hw, hh):
    c.mark(colour, [(x, z + hh), (x + hw, z), (x, z - hh), (x - hw, z)], 0.3, 1.0, curved=False)


def paint_tapis(c, view):
    if view == "front":
        # four diamonds, the outer two smaller
        for x, hw in ((-0.088, 0.0085), (-0.030, 0.0105), (0.030, 0.0105), (0.088, 0.0085)):
            _diamond(c, CREAM, x, 0.163, hw, 0.0090)
    else:
        # five on the back
        for x, hw in ((-0.096, 0.0080), (-0.048, 0.0100), (0.0, 0.0110), (0.048, 0.0100), (0.096, 0.0080)):
            _diamond(c, CREAM, x, 0.163, hw, 0.0090)


def paint_mantle_back(c):
    # the blanket's centre field on her back
    _diamond(c, BLACK, 0.0, 0.286, 0.0200, 0.0240)
    _diamond(c, CREAM, 0.0, 0.286, 0.0080, 0.0100)
    for x, hw, hh in ((-0.052, 0.0145, 0.0180), (0.052, 0.0145, 0.0180)):
        _diamond(c, BLACK, x, 0.286, hw, hh)
    for x in (-0.027, 0.027):
        _diamond(c, RED, x, 0.286, 0.0070, 0.0090)
    for x, hw, hh in ((-0.094, 0.0100, 0.0130), (0.094, 0.0100, 0.0130)):
        _diamond(c, BLACK, x, 0.286, hw, hh)


def _hand(c, s):
    # the fist's flat faces: three finger lines and the folded thumb, as Lola Pacing's are
    for z in (0.3050, 0.2877, 0.2704):
        c.stroke(SKIN_DEEP, [(s * 0.3600, z), (s * 0.3700, z)], 2.2, 0.3, 0.85, (1.0, 0.5), curved=False)
    c.stroke(SKIN_DEEP, [(s * 0.3565, 0.3205), (s * 0.3640, 0.3225), (s * 0.3700, 0.3285)], 2.2, 0.3, 0.85, (0.8, 0.4))


# ---------------------------------------------------------------------------
# THE BASKET. Rattan in flat tones; its foot, its middle band and its rim are dark rattan geometry.
# On each side between the bands, one row of hand-set dark diamonds (a plaited twill's motif), a
# different count on the wide faces and the narrow ones.
# ---------------------------------------------------------------------------

def paint_basket(c, view):
    if view in ("front", "back"):
        xs = (-0.040, 0.0, 0.040) if view == "front" else (-0.042, -0.014, 0.014, 0.042)
        for dx in xs:
            _diamond(c, RATTAN_DEEP, _BX + dx, 0.093, 0.0100, 0.0130)
            _diamond(c, RATTAN_DEEP, _BX + dx, 0.040, 0.0100, 0.0130)
    else:
        ys = (-0.026, 0.026) if view == "xpos" else (-0.034, 0.0, 0.034)
        for dy in ys:
            _diamond(c, RATTAN_DEEP, _BY + dy, 0.093, 0.0095, 0.0125)
            _diamond(c, RATTAN_DEEP, _BY + dy, 0.040, 0.0095, 0.0125)


def build(size=ATLAS):
    I = lambda name, base, metres=None: Island(name, base, size, metres)
    done = {}
    c = I("head.front", SKIN); paint_head_front(c); done[c.name] = c
    c = I("mantle.back", CREAM); paint_mantle_back(c); done[c.name] = c
    for view in GROUPS["tapis"]:
        c = I("tapis." + view, INDIGO); paint_tapis(c, view); done[c.name] = c
    for group, s in (("armL", 1), ("armR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, SKIN)
            _hand(c, s)
            done[c.name] = c
    for view in GROUPS["basket"]:
        c = I("basket." + view, RATTAN); paint_basket(c, view); done[c.name] = c
    for name, colour in FLATS.items():
        done[name] = I(name, colour, (0.04, 0.04))
    missing = set(layout(size)) - set(done)
    if missing:
        raise SystemExit("islands not painted: %s" % sorted(missing))
    return done


def main():
    from PIL import Image
    size = ATLAS
    if "--size" in sys.argv:
        size = int(sys.argv[sys.argv.index("--size") + 1])
    for hex_str in CLOTH_HEXES:
        if _near_role_hue(hex_str):
            raise SystemExit("#%s sits near role hue #%s" % (hex_str, _near_role_hue(hex_str)))
    islands = build(size)
    atlas = Image.new("RGB", (size, size), _rgb(INDIGO_DEEP))
    bleed = max(1, int(round(GUTTER * size / float(ATLAS))))
    finished = {name: isl.finished() for name, isl in islands.items()}
    for name, img in finished.items():
        x, y, w, h = islands[name].rect
        atlas.paste(img.resize((w + 2 * bleed, h + 2 * bleed), Image.BILINEAR), (x - bleed, y - bleed))
    for name, img in finished.items():
        x, y, w, h = islands[name].rect
        atlas.paste(img, (x, y))
    out_dir = OUT_DIR
    if "--out-dir" in sys.argv:
        out_dir = Path(sys.argv[sys.argv.index("--out-dir") + 1])
    os.makedirs(out_dir, exist_ok=True)
    out = out_dir / ATLAS_NAME
    for attempt in range(5):
        try:
            atlas.save(out)
            break
        except OSError:
            import time
            time.sleep(1.0)
    else:
        raise SystemExit("could not write %s" % out)
    print("wrote %s  (%d x %d, %d islands)" % (out, size, size, len(islands)))
    if "--sheet" in sys.argv:
        sheet = sys.argv[sys.argv.index("--sheet") + 1]
        atlas.crop((0, 0, size, size // 2)).save(sheet)
        print("wrote %s" % sheet)


if __name__ == "__main__":
    main()
