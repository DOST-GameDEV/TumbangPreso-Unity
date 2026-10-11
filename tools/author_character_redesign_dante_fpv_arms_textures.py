"""Paint the atlas of Dante's (displayed: Basilio) FIRST-PERSON ARMS, by hand, in his redesign's style.

    py -3 tools/author_character_redesign_dante_fpv_arms_textures.py [--sheet out.png]

Writes Assets/TumbangPreso/Art/CharacterRedesign/dante/dante-redesign-fpv-arms-atlas.png (1024).
The model is built by tools/author_character_redesign_dante_fpv_arms.py, which imports this file
for the LAYOUT and the MEASURES only (PIL is imported inside functions; Blender's Python has
none). Paint first, then build.

WHY. In first person the player still saw the old look: one shared block arm tinted skin colour
with hand-coded sleeve boxes. Arms cut out of the redesigned body and stretched to first-person
length were turned down on another hero as "long thin tubes" with "fragmented hands". So these
are his own pieces, colours and paint style, PROPORTIONED FOR THE CLOSE CAMERA.

EVERYTHING HERE IS HIS, from tools/author_character_redesign_dante.py and its textures script:
  * his RIGHT arm: the forest green coat sleeve, the fat rolled GOLD cuff, the bare forearm with
    the jade cord bracelet and its gold knot, the bronze fist;
  * his LEFT arm: the sleeve torn off at the shoulder, the bare arm, the off-white cloth WRAP
    bound round the forearm, the bronze fist.
The colours are that script's own constants, imported, not retyped. The clothes are at the QUIET
level (`QUIET_CLOTH` there): no stitches, no dust, no patches; soft cloth marks and folds are laid
through the same brush and so at the same reduced strength.

THE FRAME. Every length below is in the FIRST-PERSON ARM'S OWN FRAME, the one
`ViewmodelArmAuthor.Extract` bakes into and every held prop is posed in:
    +Y  along the arm, 0 at the shoulder end, 0.84 (`ViewmodelArms.ArmLength`) at the knuckles
    +Z  the face turned TO THE PLAYER'S EYE at rest (worked out in the model script's header)
    -Z  the "upper surface" of the game's notes: where a carried tsinelas rides, away from the eye
    +X  screen left: the INNER side of the right arm, the OUTER side of the left arm
So the back of each fist is drawn on +Z, its knuckle line near the far end, and the thumb sits on
the inner side: +X on the right arm, -X on the left.

THE ONE MECHANISM is the body's: `Toon.shader` reads the palette, not the texture, in the bottom
half of UV space, so every island lives in the TOP half of the file.

AN ISLAND is one flat view of one arm: `zpos` and `zneg` are (Y, X), `xpos` and `xneg` are (Y, Z),
`ypos` (the far end of the fist) is (X, Z). A face takes an island only when it squarely faces
that view; chamfers and block ends take one flat tone (CHARACTER_REDESIGN_DANTE.md 15.3 rule 8:
paint smears on angled faces).
"""
import os
import sys
from pathlib import Path

TOOLS = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, TOOLS)
import author_character_redesign_dante_textures as body  # noqa: E402  his colours and the brush, not edited

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "Assets" / "TumbangPreso" / "Art" / "CharacterRedesign" / "dante"
ATLAS_NAME = "dante-redesign-fpv-arms-atlas.png"
ATLAS = 1024
GUTTER = 3
SS = 2

SKIN, SKIN_LIT, SKIN_SHADE, SKIN_DEEP = body.SKIN, body.SKIN_LIT, body.SKIN_SHADE, body.SKIN_DEEP
GREEN, GREEN_LIT, GREEN_DARK, GREEN_DEEP = body.GREEN, body.GREEN_LIT, body.GREEN_DARK, body.GREEN_DEEP
GOLD, GOLD_LIT, GOLD_DARK = body.GOLD, body.GOLD_LIT, body.GOLD_DARK
BANDAGE, BANDAGE_SHADE, BANDAGE_DIRT = body.BANDAGE, body.BANDAGE_SHADE, body.BANDAGE_DIRT
JADE, JADE_DARK = body.JADE, body.JADE_DARK
mix = body.mix

# ---------------------------------------------------------------------------
# THE MEASURES, shared with the model script. Along the arm (Y) unless said otherwise.
# The hand block of the game's shared arm (Resources/Models/viewmodel_arm.obj) runs 0.62 to 0.84
# and is 0.316 by 0.300 across; the fist here keeps that place and that size.
# ---------------------------------------------------------------------------
ARM_LENGTH = 0.84
FIST = (0.62, 0.84)
FIST_SHIFT = 0.010            # the fist block sits this far AWAY from the thumb, to give it room
THUMB_SIDE = {"R": 1.0, "L": -1.0}   # the sign of X the thumb is on
R_SLEEVE = (0.0, 0.430)
R_CUFF = (0.405, 0.560)
R_FOREARM = (0.36, 0.65)
R_CORD = (0.578, 0.602)
L_STUB = (0.0, 0.215)
L_UPPER = (0.02, 0.42)
L_WRAP = (0.385, 0.624)
KNUCKLE = 0.764               # where the knuckle line is drawn on the back of the fist
FINGERS = (-0.087, -0.010, 0.067)   # the three lines between four fingers, X on a thumb-at-plus-X fist

VIEW_AXES = {"zpos": (1, 0), "zneg": (1, 0), "xpos": (1, 2), "xneg": (1, 2), "ypos": (0, 2)}
VIEW_DIR = {"zpos": (0, 0, 1), "zneg": (0, 0, -1), "xpos": (1, 0, 0), "xneg": (-1, 0, 0), "ypos": (0, 1, 0)}
_LONG = (0.0, 0.86, -0.19, 0.19)
_END = (-0.19, 0.19, -0.19, 0.19)
# group -> view -> (window in metres, px per metre at 1024). The eye's face gets the most paint.
GROUPS = {arm: {"zpos": (_LONG, 520), "zneg": (_LONG, 330), "xpos": (_LONG, 330), "xneg": (_LONG, 330),
                "ypos": (_END, 330)} for arm in ("R", "L")}

FLATS = {
    "skin": SKIN, "skin_shade": mix(SKIN, SKIN_SHADE, 0.75), "skin_deep": SKIN_DEEP,
    "green": GREEN, "green_dark": mix(GREEN, GREEN_DARK, 0.7), "lining": GREEN_DEEP,
    "gold": GOLD, "gold_dark": GOLD_DARK, "gold_under": mix(GOLD, GOLD_DARK, 0.55),
    "bandage": BANDAGE, "bandage_shade": mix(BANDAGE, BANDAGE_SHADE, 0.6), "bandage_dirt": mix(BANDAGE_SHADE, BANDAGE_DIRT, 0.6),
    "jade": JADE_DARK, "jade_under": mix(JADE_DARK, GREEN_DEEP, 0.4),
}
FLAT_PX = 14

_LAYOUT = {}


def layout(size=ATLAS):
    """name -> (x, y, w, h) px, top-left origin. A shelf pack into the TOP half, tallest first;
    densities shrink together two per cent at a time until everything fits (the body's packer)."""
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
    """(a, b) on an island to a Blender UV (u right, v up), clamped a hair inside the island."""
    x, y, w, h = layout(size)[name]
    a0, a1, b0, b1 = window(name)
    fa = min(max((a - a0) / (a1 - a0), 0.0), 1.0)
    fb = min(max((b - b0) / (b1 - b0), 0.0), 1.0)
    px = x + 0.5 + fa * (w - 1.0)
    py = y + 0.5 + (1.0 - fb) * (h - 1.0)
    return (px / size, 1.0 - py / size)


class Island(body.Island):
    """The body's brush (mark, blob, stroke, band, column), on this file's layout. Widths and
    feathers are millimetres of the first-person frame, which is about 4.4 times the body's."""

    def __init__(self, name, base, size):
        from PIL import Image
        self.name = name
        x, y, w, h = layout(size)[name]
        self.rect = (x, y, w, h)
        self.w, self.h = w * SS, h * SS
        self.win = window(name)
        a0, a1, b0, b1 = self.win
        self.mm = 0.5 * (self.w / (a1 - a0) + self.h / (b1 - b0)) / 1000.0
        self.img = Image.new("RGB", (self.w, self.h), body._rgb(base))

    def below(self, colour, b, a0, a1, feather=0.0, strength=1.0):
        """Everything under height `b` between two places along the arm: the half turned away."""
        lo = self.win[2] - 1.0
        return self.mark(colour, [(a0, lo), (a1, lo), (a1, b), (a0, b)], feather, strength, curved=False)


# ---------------------------------------------------------------------------
# THE FIST, the same on both arms but for the side the thumb is on. `t` is +1 when the thumb is
# at +X (the right arm), -1 when it is at -X (the left).
# The body's mitten is three drawn finger lines and a shaded under half (`_hand` there); this is
# that hand seen from the back: a knuckle line across, the three lines running from it over the
# end of the block and back under it, the wrist end in a little shade.
# ---------------------------------------------------------------------------

def _fist(c, t, view):
    y0, y1 = FIST
    c.column(SKIN, y0 - 0.006, 0.90)
    cx = -FIST_SHIFT * t
    if view == "zpos":
        c.blob(SKIN_LIT, (0.705, cx), 0.050, 0.095, 16, 0.55)
        c.column(SKIN_SHADE, y0 - 0.006, y0 + 0.036, 5, 0.5)
        # the knuckle line: one stroke, bowed a little toward the wrist at both ends
        c.stroke(SKIN_DEEP, [(KNUCKLE - 0.010, t * -0.152), (KNUCKLE + 0.003, t * -0.087), (KNUCKLE + 0.004, t * 0.000),
                             (KNUCKLE + 0.001, t * 0.074), (KNUCKLE - 0.012, t * 0.132)], 9.5, 0.6, 0.95, (0.45, 0.45))
        for x, w in zip(FINGERS, (8.0, 8.8, 7.6)):
            c.stroke(SKIN_DEEP, [(KNUCKLE + 0.002, t * x), (0.862, t * x)], w, 0.5, 0.95, (1.0, 1.0), curved=False)
        # each knuckle catches a little light, just wristward of the line
        for x, r in ((-0.126, 0.022), (-0.049, 0.026), (0.028, 0.026), (0.104, 0.022)):
            c.blob(SKIN_LIT, (KNUCKLE - 0.026, t * x), 0.014, r, 4, 0.55)
    elif view == "zneg":
        c.column(SKIN_SHADE, y0 - 0.006, 0.90, 0, 0.75)
        for x, w in zip(FINGERS, (7.0, 7.6, 6.8)):
            c.stroke(SKIN_DEEP, [(0.862, t * x), (0.702, t * x)], w, 0.5, 0.9, (1.0, 0.4), curved=False)
        # where the curled fingertips end against the palm
        c.stroke(SKIN_DEEP, [(0.698, t * -0.140), (0.692, t * -0.020), (0.700, t * 0.100)], 8.0, 0.6, 0.85, (0.5, 0.5))
    elif view in ("xpos", "xneg"):
        c.below(SKIN_SHADE, -0.036, y0 - 0.006, 0.90, 0, 0.7)
        c.column(SKIN_SHADE, y0 - 0.006, y0 + 0.036, 5, 0.5)
        # the little finger, curled: one drawn hook, on the side away from the thumb only
        if (view == "xneg") == (t > 0):
            c.stroke(SKIN_DEEP, [(0.862, 0.030), (0.740, 0.026), (0.704, -0.012), (0.716, -0.052)], 7.6, 0.5, 0.95, (0.9, 0.4))
    else:
        c.below(SKIN_SHADE, -0.040, -0.2, 0.2, 0, 0.7)
        for x, w in zip(FINGERS, (8.0, 8.8, 7.6)):
            c.stroke(SKIN_DEEP, [(t * x, 0.170), (t * x, -0.170)], w, 0.5, 0.95, (1.0, 1.0), curved=False)
        # the fold where the fingers turn under
        c.stroke(SKIN_DEEP, [(t * -0.150, -0.040), (t * 0.000, -0.046), (t * 0.130, -0.040)], 7.0, 0.6, 0.8, (0.6, 0.6))


def _bare(c, view, a0, a1, lit):
    """A length of bare arm between two places along it."""
    c.column(SKIN, a0, a1)
    if view == "zpos":
        c.mark(SKIN_LIT, lit, 10, 0.5)
    elif view == "zneg":
        c.column(SKIN_SHADE, a0, a1, 0, 0.75)
    else:
        c.below(SKIN_SHADE, -0.030, a0, a1, 5, 0.7)


# ---------------------------------------------------------------------------
# HIS RIGHT ARM: sleeve, rolled gold cuff, bare forearm with the jade cord, fist.
# The colours change where one block's flat face ends and the next one's begins (the blocks
# overlap, so the join is hidden inside the bigger one): sleeve to cuff at 0.416, cuff to skin
# at 0.549.
# ---------------------------------------------------------------------------

def paint_right(c, view):
    if view == "ypos":
        return _fist(c, 1.0, view)
    _bare(c, view, 0.53, 0.66, [(0.556, -0.075), (0.614, -0.070), (0.614, 0.062), (0.556, 0.068)])
    # the sleeve, with the two folds a rolled cuff pushes up the arm (quiet: the brush thins them)
    c.column(GREEN, -0.01, 0.416)
    if view == "zpos":
        c.mark(GREEN_LIT, [(0.110, -0.092), (0.318, -0.086), (0.322, 0.066), (0.124, 0.082)], 14, 0.65)
        c.stroke(GREEN_DARK, [(0.214, 0.118), (0.262, 0.030), (0.286, -0.100)], 15, 1.6, 0.9)
        c.stroke(GREEN_DARK, [(0.300, 0.110), (0.326, 0.020), (0.340, -0.074)], 12, 1.6, 0.8)
        c.column(GREEN_DARK, 0.376, 0.416, 6, 0.8)
    elif view == "zneg":
        c.column(GREEN_DARK, -0.01, 0.416, 0, 0.7)
    else:
        c.below(GREEN_DARK, -0.040, -0.01, 0.416, 0, 0.7)
        c.stroke(GREEN_DARK, [(0.230, 0.120), (0.262, 0.020), (0.250, -0.110)], 13, 1.6, 0.85)
        c.column(GREEN_DARK, 0.376, 0.416, 6, 0.8)
    # the roll: gold, a pale band along it, darker at both edges
    c.column(GOLD, 0.416, 0.549)
    c.column(GOLD_DARK, 0.416, 0.438, 2.0, 0.6)
    c.column(GOLD_LIT, 0.462, 0.490, 2.6, 0.8)
    c.column(GOLD_DARK, 0.522, 0.549, 2.0, 0.85)
    if view == "zneg":
        c.column(GOLD_DARK, 0.416, 0.549, 0, 0.5)
    elif view != "zpos":
        c.below(GOLD_DARK, -0.046, 0.416, 0.549, 0, 0.55)
    # the cord bracelet at the wrist, jade green (its gold knot is a block of its own)
    c.column(JADE_DARK, R_CORD[0], R_CORD[1])
    c.column(JADE, R_CORD[0] + 0.007, R_CORD[1] - 0.008, 1.4, 0.45)
    if view == "zneg":
        c.column(GREEN_DEEP, R_CORD[0], R_CORD[1], 0, 0.4)
    _fist(c, 1.0, view)


# ---------------------------------------------------------------------------
# HIS LEFT ARM: bare, the forearm bound in the cloth wrap, then the fist. (The torn sleeve stub
# at the shoulder is flat tones and is never in view.)
# The wrap is wound on a slant, as the body's is. Three strands, so a line that leaves one face
# at an edge comes onto the next face where another line starts: each face's strokes are set
# from the same winding, then given their own widths.
# ---------------------------------------------------------------------------
WRAP_HALF = (0.144, 0.136)      # half width (X), half depth (Z) of the wrap block, mid length
WRAP_STEP = 0.056               # between one winding and the next, along the arm
WRAP_STRANDS = 3
# The windings are the wrap's whole drawing and the eye is close, so they are laid a little
# firmer than the body's (whose brush thins BANDAGE_SHADE strokes to a third as fold marks).
WRAP_LINE = mix(BANDAGE, BANDAGE_SHADE, 0.62)


def _windings(view):
    """[(a0, b0), (a1, b1)] for each winding line on one face, in that face's (Y, across) axes."""
    hx, hz = WRAP_HALF
    turn = 4.0 * (hx + hz)
    rise = WRAP_STRANDS * WRAP_STEP / turn          # along the arm, per metre round it
    # the distance round the block at which each face starts, and the way `across` runs on it
    start = {"zpos": 0.0, "xpos": 2 * hx, "zneg": 2 * hx + 2 * hz, "xneg": 4 * hx + 2 * hz}[view]
    half = hx if view in ("zpos", "zneg") else hz
    sign = 1.0 if view in ("zpos", "xneg") else -1.0     # across grows with the winding, or against it
    lines = []
    for k in range(-4, 9):
        y = L_WRAP[0] + 0.020 + k * WRAP_STEP + rise * start
        lines.append([(y, -sign * half), (y + rise * 2 * half, sign * half)])
    return lines


def paint_left(c, view):
    if view == "ypos":
        return _fist(c, -1.0, view)
    lo, hi = L_WRAP
    c.column(BANDAGE, lo, hi + 0.01)
    if view == "zneg":
        c.column(BANDAGE_SHADE, lo, hi + 0.01, 0, 0.6)
    elif view != "zpos":
        c.below(BANDAGE_SHADE, -0.040, lo, hi + 0.01, 0, 0.6)
    widths = (12.0, 13.5, 11.0, 13.0, 12.5, 11.5, 13.0)
    for i, line in enumerate(_windings(view)):
        c.stroke(WRAP_LINE, line, widths[i % len(widths)], 0.5, 0.8, (1, 1), curved=False)
    # grubby at both ends, more at the hand
    c.column(BANDAGE_DIRT, lo, lo + 0.034, 2.5, 0.45)
    c.column(BANDAGE_DIRT, hi - 0.026, hi + 0.004, 2.5, 0.32)
    _bare(c, view, -0.01, lo + 0.008, [(0.150, -0.080), (0.330, -0.076), (0.334, 0.064), (0.160, 0.074)])
    _fist(c, -1.0, view)


def build(size=ATLAS):
    done = {}
    for arm, paint in (("R", paint_right), ("L", paint_left)):
        for view in GROUPS[arm]:
            c = Island(arm + "." + view, SKIN, size)
            paint(c, view)
            done[c.name] = c
    for name, colour in FLATS.items():
        done[name] = Island(name, colour, size)
    missing = set(layout(size)) - set(done)
    if missing:
        raise SystemExit("islands not painted: %s" % sorted(missing))
    return done


def main():
    from PIL import Image

    size = ATLAS
    for hex_str in (GREEN, GREEN_LIT, GREEN_DARK, GOLD, GOLD_LIT, GOLD_DARK, BANDAGE, BANDAGE_SHADE, BANDAGE_DIRT, JADE, JADE_DARK):
        if body._near_role_hue(hex_str):
            raise SystemExit("#%s sits near role hue #%s" % (hex_str, body._near_role_hue(hex_str)))
    islands = build(size)
    atlas = Image.new("RGB", (size, size), body._rgb(GREEN_DEEP))
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
    for attempt in range(5):
        try:
            atlas.save(out)
            break
        except OSError:      # Windows sometimes refuses the write (Errno 22); a second later it takes it
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
