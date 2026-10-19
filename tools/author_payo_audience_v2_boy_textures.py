"""Paint the atlas of the Payo audience v2 villager `boy`, by hand, in the heroes' style.

    py -3 tools/author_payo_audience_v2_boy_textures.py [--size 1024] [--sheet file.png]

Writes ArtSource/payo/audience/v2/boy/audience_v2_boy-atlas.png. The model is built by
tools/author_payo_audience_v2_boy.py, which imports this file for the LAYOUT only (PIL is imported
inside the painting calls, Blender's Python has none). Paint first, then build.

WHY. docs/reports/character-redesign/payo-audience-brief.md: the Payo basin wall wants a crowd of
precolonial Ifugao and Cordillera villagers, each built EXACTLY like a Classic redesign. This is
the village boy, about eight, copied from the Totoy redesign's textures script (the nearest
approved Classic: a small boy on the same rig) and rewritten for him.

THE RULES HE INHERITS (classic-brief.md, section 15.3 of CHARACTER_REDESIGN_DANTE.md):
  * the face is drawn for the flat front of the head: flat skin, the fringe's shadow as one tone,
    two solid ink eye blocks with ONE cut, one thin stroke for a mouth. No nose, no blush (a boy),
    no wrinkles;
  * NO PAINTED HAIR SHINE. Hair is flat tones chosen by which way a face points;
  * a drawing only on a face that squarely faces its view; every chamfer takes a flat tone.

HIS COLOURS (the brief's "QUIET" rule). A crowd figure must never steal a hero's hue:
  * skin in the heroes' range, a warm mid brown;
  * Dante's black hair values (black is BLACK);
  * UNDYED COTTON for the bahag: a warm off-white ground with a shade and a deep;
  * a desaturated MADDER RED, only as the ONE thin band on each flap and two small beads;
  * dark seasoned wood for his spinning top; brass as the one metal (one bead).
ROLE HUES (offence orange #f87020, defence blue #0080e8): every colour but skin is checked below.
"""
import colorsys
import math
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "ArtSource" / "payo" / "audience" / "v2" / "boy"
ATLAS_NAME = "audience_v2_boy-atlas.png"
ATLAS = 2048          # the file is square; only the top half carries paint
GUTTER = 4
SS = 2

# ---------------------------------------------------------------------------
# THE COLOURS
# ---------------------------------------------------------------------------
SKIN = "8f5a38"; SKIN_LIT = "a46c47"; SKIN_SHADE = "76472b"; SKIN_DEEP = "5c3520"       # a warm mid brown
HAIR = "1a181e"; HAIR_TOP = "2b2933"; HAIR_DEEP = "0e0d12"                              # Dante's black
INK = "1a1420"                                                                            # Dante's ink
COTTON = "ddd0b4"; COTTON_LIT = "ebe2cc"; COTTON_SHADE = "c0b093"; COTTON_DEEP = "9c8c70"  # undyed cotton
RED = "983238"; RED_LIT = "aa4047"; RED_DARK = "74262c"                                   # madder, desaturated
WOOD = "6e5036"; WOOD_LIT = "7e6447"; WOOD_DARK = "4e3826"                                # his spinning top
BRASS = "b8953e"; BRASS_LIT = "cfae58"; BRASS_DARK = "8f722c"                             # the one metal
BEAD = "ece4d0"; BEAD_SHADE = "cbbfa6"                                                    # shell beads
SEED = "3a2e2a"                                                                           # dark seed beads
RATTAN = "a08a5c"; RATTAN_LIT = "b49e70"; RATTAN_DARK = "7a6a4a"                          # natural rattan, his armlet

CLOTH_HEXES = [HAIR, HAIR_TOP, HAIR_DEEP, COTTON, COTTON_LIT, COTTON_SHADE, COTTON_DEEP, RED, RED_LIT, RED_DARK,
               WOOD, WOOD_LIT, WOOD_DARK, BRASS, BRASS_LIT, BRASS_DARK, BEAD, BEAD_SHADE, SEED, RATTAN, RATTAN_LIT, RATTAN_DARK]

# ---------------------------------------------------------------------------
# MEASURES THE MODEL SCRIPT SHARES
# ---------------------------------------------------------------------------
WRIST = 0.240                   # where the fist block starts
HAND_END = 0.296
EYE_AT = (0.079, 0.482)         # the middle of his LEFT eye on the face (x, z), before the head scale

# ---------------------------------------------------------------------------
# THE ISLANDS. group -> view -> (window in metres, px per metre at 2048).
# ---------------------------------------------------------------------------
VIEW_AXES = {"front": (0, 2), "back": (0, 2), "xpos": (1, 2), "xneg": (1, 2), "top": (0, 1), "bottom": (0, 1)}
VIEW_DIR = {"front": (0, -1, 0), "back": (0, 1, 0), "xpos": (1, 0, 0), "xneg": (-1, 0, 0),
            "top": (0, 0, 1), "bottom": (0, 0, -1)}

GROUPS = {
    "head": {
        "front": ((-0.20, 0.20, 0.33, 0.67), 1500),
    },
    "torso": {
        "front": ((-0.17, 0.17, 0.16, 0.36), 1100),
        "back": ((-0.17, 0.17, 0.16, 0.36), 900),
    },
    "armL": {
        "front": ((0.09, 0.31, 0.19, 0.385), 1000),
        "back": ((0.09, 0.31, 0.19, 0.385), 800),
        "top": ((0.09, 0.31, -0.10, 0.12), 800),
    },
    "armR": {
        "front": ((-0.31, -0.09, 0.19, 0.385), 1000),
        "back": ((-0.31, -0.09, 0.19, 0.385), 800),
        "top": ((-0.31, -0.09, -0.10, 0.12), 800),
    },
}
VIEW_BIAS = {}

# PAINTED SWATCHES. name -> (px wide, px high at 2048, metres wide, metres high).
#   the bahag's two flaps, seen flat: the cotton's weave as a few drawn warp lines
SWATCHES = {
    "flapF": (150, 230, 0.14, 0.125),
    "flapB": (150, 260, 0.14, 0.14),
}

FLATS = {
    "hair_top": HAIR_TOP, "hair": HAIR, "hair_under": HAIR_DEEP,
    "skin_lit": SKIN_LIT, "skin": SKIN, "skin_shade": SKIN_SHADE, "skin_deep": SKIN_DEEP,
    "cotton_lit": COTTON_LIT, "cotton": COTTON, "cotton_shade": COTTON_SHADE, "cotton_deep": COTTON_DEEP,
    "red_lit": RED_LIT, "red": RED, "red_dark": RED_DARK,
    "wood_lit": WOOD_LIT, "wood": WOOD, "wood_dark": WOOD_DARK,
    "brass_lit": BRASS_LIT, "brass": BRASS, "brass_dark": BRASS_DARK,
    "bead": BEAD, "bead_shade": BEAD_SHADE, "seed": SEED, "ink": INK,
    "rattan_lit": RATTAN_LIT, "rattan": RATTAN, "rattan_dark": RATTAN_DARK,
}
FLAT_PX = 40

_LAYOUT = {}


def layout(size=ATLAS):
    """name -> (x, y, w, h) px in the atlas, top-left origin (a shelf pack into the top half)."""
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
        for name, (w, h, _wm, _hm) in SWATCHES.items():
            items.append((name, max(8, int(round(w * s))), max(8, int(round(h * s)))))
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

    def mark(self, colour, points, feather=0.0, strength=1.0, curved=True):
        from PIL import Image, ImageDraw
        pts = _spline(points, closed=True) if curved else list(points)
        mask = Image.new("L", (self.w, self.h), 0)
        ImageDraw.Draw(mask).polygon([self.px(p) for p in pts], fill=255)
        return self._lay(colour, mask, feather, strength)

    def blob(self, colour, centre, ra, rb, feather=0.0, strength=1.0):
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

    def column(self, colour, a0, a1, feather=0.0, strength=1.0):
        b0, b1 = self.win[2], self.win[3]
        pad = (b1 - b0)
        return self.mark(colour, [(a0, b0 - pad), (a1, b0 - pad), (a1, b1 + pad), (a0, b1 + pad)],
                         feather, strength, curved=False)

    def finished(self):
        from PIL import Image
        return self.img.resize((self.rect[2], self.rect[3]), Image.LANCZOS)


# ---------------------------------------------------------------------------
# THE HEAD. His face on the flat front of the block, IN THE HEROES' HAND.
#   EYES: the heroes' solid ink block, 52 by 64 mm, upright, at (0.079, 0.482) (Amihan's at
#   (0.076, 0.481), Cheska's at (0.072, 0.487)). HIS ONE CUT: the TOP edge is ARCHED, peaking a
#   little inside the middle, so the eye opens UP: wide, eager, eyebrows-up delight at the match.
#   Nobody in the row has it (Bebang, Bayan and Amihan slant the top, which reads as a brow
#   pulled down; Totoy's foot is slanted; Lola Pacing's are shut arcs).
#   MOUTH: the heroes' stroke as a big open grin, a U a touch higher at his left corner (cheeky),
#   72 mm across.
# ---------------------------------------------------------------------------
#   his LEFT eye about its middle, +x toward his left ear (the outside)
EYE = [(-0.026, 0.0150), (-0.019, 0.0270), (-0.008, 0.0345), (0.006, 0.0350), (0.018, 0.0285), (0.026, 0.0160),
       (0.026, -0.0270), (0.020, -0.0320), (-0.020, -0.0320), (-0.026, -0.0270)]
GRIN = [(-0.0420, 0.4225), (-0.0260, 0.4030), (0.0, 0.3960), (0.0260, 0.4035), (0.0430, 0.4255)]
MOUTH_WIDTH = 8.4
#   the fringe's shadow under each choppy lock: (from x, to x, the height it falls to)
FRINGE_SHADOW = [(-0.200, -0.150, 0.560), (-0.150, -0.094, 0.574), (-0.094, -0.030, 0.556), (-0.030, 0.034, 0.580),
                 (0.034, 0.096, 0.560), (0.096, 0.150, 0.572), (0.150, 0.200, 0.556)]


def paint_head_front(c):
    c.blob(SKIN_LIT, (0.0, 0.415), 0.120, 0.050, 22, 0.30)
    for x0, x1, z in FRINGE_SHADOW:
        c.mark(SKIN_SHADE, [(x0, 0.72), (x1, 0.72), (x1, z), (x0, z)], 1.2, 0.70, curved=False)
    for s in (1, -1):
        cx, cz = EYE_AT[0] * s, EYE_AT[1]
        c.mark(INK, [(cx + s * x, cz + z) for x, z in EYE], 0.25, 1.0, curved=False)
    c.stroke(INK, GRIN, MOUTH_WIDTH, 0.3, 1.0, (0.62, 0.62))
    c.blob(INK, GRIN[0], 0.0030, 0.0030, 0.25, 1.0)
    c.blob(INK, GRIN[-1], 0.0030, 0.0030, 0.25, 1.0)


# ---------------------------------------------------------------------------
# THE TORSO: bare skin. A child's chest is drawn as almost nothing: a soft lit patch on the
# chest, a soft shade down the spine. No muscle, no nipples, no tattoos (a child has none).
# ---------------------------------------------------------------------------

def paint_torso_front(c):
    c.mark(SKIN_LIT, [(-0.090, 0.315), (0.090, 0.315), (0.084, 0.262), (-0.084, 0.262)], 14, 0.45)
    c.blob(SKIN_LIT, (0.0, 0.218), 0.050, 0.020, 10, 0.25)


def paint_torso_back(c):
    c.mark(SKIN_LIT, [(-0.096, 0.318), (0.096, 0.318), (0.090, 0.286), (-0.090, 0.284)], 10, 0.45)
    c.stroke(SKIN_SHADE, [(0.0, 0.300), (0.0, 0.250), (0.0, 0.200)], 9.0, 4.0, 0.45, (0.3, 0.6))


# ---------------------------------------------------------------------------
# THE ARMS: bare skin, his fist drawn on its flat faces only.
# ---------------------------------------------------------------------------

def _hand(c, s, view):
    if view in ("front", "back"):
        for z, x1 in ((0.3070, 0.2810), (0.2880, 0.2825), (0.2690, 0.2800)):
            c.stroke(SKIN_DEEP, [(s * 0.2640, z), (s * x1, z)], 2.6, 0.3, 0.9, (1.0, 0.5), curved=False)
        c.stroke(SKIN_DEEP, [(s * 0.2540, 0.3230), (s * 0.2660, 0.3250), (s * 0.2730, 0.3310)], 2.6, 0.3, 0.9, (0.8, 0.4))
    elif view == "top":
        c.mark(SKIN_LIT, [(s * 0.256, -0.026), (s * 0.280, -0.026), (s * 0.280, 0.038), (s * 0.256, 0.038)], 4, 0.5)


def paint_arm(c, s, view):
    if view in ("front", "back"):
        c.mark(SKIN_LIT, [(s * 0.112, 0.322), (s * 0.160, 0.326), (s * 0.160, 0.304), (s * 0.112, 0.300)], 7, 0.40)
    elif view == "top":
        c.mark(SKIN_LIT, [(s * 0.110, -0.030), (s * 0.170, -0.034), (s * 0.170, 0.040), (s * 0.110, 0.036)], 8, 0.45)
    _hand(c, s, view)


# ---------------------------------------------------------------------------
# THE BAHAG'S FLAPS. Undyed cotton, a hand-woven strip: two pairs of thin warp lines down its
# length in the cotton's own shade (the weave, not a colour), each pair its own spacing, and the
# one red band is GEOMETRY on the model, not paint. Seen at the flap's own size (0..1 across, 0..1
# down = bottom to top).
# ---------------------------------------------------------------------------

def paint_flap_front(c):
    c.mark(COTTON_LIT, [(0.18, 0.95), (0.50, 0.97), (0.50, 0.40), (0.20, 0.38)], 6, 0.45)
    c.stroke(COTTON_SHADE, [(0.16, 0.98), (0.15, 0.02)], 3.2, 0.3, 0.9, (1.0, 1.0), curved=False)
    c.stroke(COTTON_SHADE, [(0.84, 0.98), (0.85, 0.02)], 3.2, 0.3, 0.9, (1.0, 1.0), curved=False)
    c.stroke(COTTON_DEEP, [(0.50, 0.30), (0.50, 0.04)], 2.0, 0.3, 0.6, (1.0, 1.0), curved=False)


def paint_flap_back(c):
    c.mark(COTTON_LIT, [(0.22, 0.96), (0.78, 0.96), (0.74, 0.70), (0.26, 0.70)], 8, 0.40)
    c.stroke(COTTON_SHADE, [(0.20, 0.98), (0.19, 0.02)], 3.0, 0.3, 0.9, (1.0, 1.0), curved=False)
    c.stroke(COTTON_SHADE, [(0.27, 0.98), (0.26, 0.02)], 2.0, 0.3, 0.8, (1.0, 1.0), curved=False)
    c.stroke(COTTON_SHADE, [(0.80, 0.98), (0.81, 0.02)], 3.0, 0.3, 0.9, (1.0, 1.0), curved=False)


def build(size=ATLAS):
    I = lambda name, base, metres=None: Island(name, base, size, metres)
    done = {}
    c = I("head.front", SKIN); paint_head_front(c); done[c.name] = c
    c = I("torso.front", SKIN); paint_torso_front(c); done[c.name] = c
    c = I("torso.back", SKIN); paint_torso_back(c); done[c.name] = c
    for group, s in (("armL", 1), ("armR", -1)):
        for view in GROUPS[group]:
            c = I(group + "." + view, SKIN)
            paint_arm(c, s, view)
            done[c.name] = c
    c = I("flapF", COTTON, (SWATCHES["flapF"][2], SWATCHES["flapF"][3])); paint_flap_front(c); done[c.name] = c
    c = I("flapB", COTTON, (SWATCHES["flapB"][2], SWATCHES["flapB"][3])); paint_flap_back(c); done[c.name] = c
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
    atlas = Image.new("RGB", (size, size), _rgb(SKIN_SHADE))
    bleed = max(1, int(round(GUTTER * size / float(ATLAS))))
    finished = {name: isl.finished() for name, isl in islands.items()}
    for name, img in finished.items():
        x, y, w, h = islands[name].rect
        atlas.paste(img.resize((w + 2 * bleed, h + 2 * bleed), Image.BILINEAR), (x - bleed, y - bleed))
    for name, img in finished.items():
        x, y, w, h = islands[name].rect
        atlas.paste(img, (x, y))
    os.makedirs(OUT_DIR, exist_ok=True)
    out = OUT_DIR / ATLAS_NAME
    for attempt in range(6):
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
        atlas.crop((0, 0, size, size // 2)).save(sys.argv[sys.argv.index("--sheet") + 1])


if __name__ == "__main__":
    main()
