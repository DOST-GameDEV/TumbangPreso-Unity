"""Paint the jump pad's one atlas, in the house illustrated style.

  py -3 tools/author_jump_pad_textures.py [--sheet PATH]

Writes ArtSource/ilalim/textures/jump_pad_paint.png and the copy the game loads at runtime,
Assets/TumbangPreso/Resources/Map/JumpPad/jump_pad_paint.png (Runtime/Map/JumpPad.cs reads it
with Resources.Load). The model is tools/author_jump_pad.py, which reads the first file.

WHY (owner, 2026-10-04, on the pad built from flat unlit quads): "it looks flat, doesnt have
shading, texture or any of that stylized character to it", and "can you fix up a better model for
the jump pads something like this with animations". So every part of the prop carries a DRAWING,
not a colour: brushed coats, an inked edge, drawn wear where feet land, grime in the corners.

THE STYLE is the prop kit's (tools/author_ilalim_textures_props.py, whose helpers this imports):
FLAT fills, a few LARGE patches with FEATHERED organic edges, broad brush bands, no grain, no
noise, no photo anything. Lines are warped by a smooth field so they read as a hand's.

THE ATLAS, 1024 x 1024, top-left origin. tools/author_jump_pad.py repeats REGIONS and EXTENT
(Blender's Python has no scipy to import this module). Each square region is a TOP-DOWN drawing
of one part: the part's centre is the region's centre and EXTENT metres reach the region's edge.
The parts' side walls are unfolded OUTWARD from the top's outline (the model writes those UVs), so
the drawing carries a margin beyond each outline for the wall's paint and its foot grime.

  cushion  (0, 0, 512, 512)       EXTENT 0.66: the amber pillow, half 0.575, with the painted
                                  double chevron, a worn rim and scuffs where shoes land.
  base     (512, 0, 1024, 512)    EXTENT 0.94: the charcoal rubber surround, half 0.85, with the
                                  sketch's WHITE OUTLINE as a painted band at 0.605..0.695, a
                                  worn chamfer and a bolt stain in each corner.
  chevron  (0, 512, 512, 832)     the floating chevron's FACE, 640 px per metre, centre at the
                                  region's centre: light yellow with a gold inked rim.
  ring     (512, 512, 1024, 1024) EXTENT 0.78: the rising outline frame, cream, 0.605..0.695.
  bolt     (0, 832, 192, 1024)    EXTENT 0.048: one bolt head, a pale steel disc with a slot.

ROLE HUES (Art_Direction.md section 1): the sketch's plate is orange, which sits on the attackers'
#f87020, so the cushion is a golden AMBER (0.97, 0.70, 0.07) and its shadows go toward brown-gold, never
toward orange; nothing here is near the taya's #0080e8 either.
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, str(Path(__file__).resolve().parent))
import author_ilalim_textures_props as P                 # noqa: E402  (read-only: the brushes)
from author_ilalim_textures_props import hexcol, smooth, patches, coat, fill, blobs, paint, rect_mask  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource" / "ilalim" / "textures"
RUNTIME = ROOT / "Assets" / "TumbangPreso" / "Resources" / "Map" / "JumpPad"
NAME = "jump_pad_paint"
SIZE = 1024
REGIONS = {
    "cushion": (0, 0, 512, 512),
    "base": (512, 0, 1024, 512),
    "chevron": (0, 512, 512, 832),
    "ring": (512, 512, 1024, 1024),
    "bolt": (0, 832, 192, 1024),
}
EXTENT = {"cushion": 0.66, "base": 0.94, "ring": 0.78, "bolt": 0.048}
CHEVRON_PX_PER_M = 640.0

AMBER = "f7b312"            # (0.97, 0.70, 0.07): golden, a clear step from orange
AMBER_LIGHT = "ffd04a"
AMBER_DEEP = "c48d0a"       # the worn rim and the wall: brown-gold, away from orange
INK = "5a3a0c"              # the drawn line on amber
CREAM = "f6f1e2"
RUBBER = "36343a"
RUBBER_LIGHT = "4a4850"
RUBBER_DARK = "232226"
CHEVRON = "ffdc3a"          # (1.0, 0.86, 0.22)
CHEVRON_LIGHT = "fff0a0"
CHEVRON_RIM = "c79a1c"


def px(part, metres, size=512):
    """Metres from a part's centre to pixels from its region's centre."""
    return metres / EXTENT[part] * size / 2


def square(part, half, radius, size=512, wobble=0.0, seed=0, feather=1.2):
    """A top-down rounded square of `half` metres, as a mask in the part's region."""
    c, h = size / 2, px(part, half, size)
    return rect_mask(size, size, (c - h, c - h, c + h, c + h), px(part, radius, size), wobble, seed, feather)


def band(part, inner, outer, r_inner, r_outer, size=512, wobble=0.0, seed=0):
    return np.clip(square(part, outer, r_outer, size, wobble, seed) - square(part, inner, r_inner, size, wobble, seed), 0, 1)


def strokes(h, w, length_px, width_px, coverage, seed, angle=0.0, feather=0.35):
    """Broad BRUSH BANDS: long soft-edged patches all lying one way, turned by `angle` degrees.
    Painted on a larger canvas and cropped, so a turned stroke never shows the canvas corner."""
    big = int(max(h, w) * 1.5)
    m = patches(big, big, width_px, coverage, seed, feather, stretch=(length_px / width_px, 1.0))
    if angle:
        m = ndimage.rotate(m, angle, reshape=False, order=1, mode="wrap")
    y0, x0 = (big - h) // 2, (big - w) // 2
    return np.clip(m[y0:y0 + h, x0:x0 + w], 0, 1)


def chevron_mask(h, w, cx, apex_y, half_w, drop, thick, seed, wobble=2.5):
    """A painted "^" (apex up): the band between two parallel V lines, hand-wobbled."""
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    xx = xx + smooth(h, w, 46, seed) * wobble
    yy = yy + smooth(h, w, 46, seed + 1) * wobble
    slope = drop / half_w
    d = (yy - apex_y) - slope * np.abs(xx - cx)          # 0 on the upper edge, growing downward
    inside = np.clip(d / 1.4 + 0.5, 0, 1) * np.clip((thick - d) / 1.4 + 0.5, 0, 1)
    ends = np.clip((half_w - np.abs(xx - cx)) / 1.4 + 0.5, 0, 1)
    return inside * ends


def lay(img, mask, colour):
    return img * (1 - mask[..., None]) + hexcol(colour) * mask[..., None]


# ------------------------------------------------------------------ the parts

def cushion():
    S = 512
    img = fill(S, S, AMBER_DEEP)                                    # the wall, beyond the outline
    top = square("cushion", 0.575, 0.10, S)
    body = fill(S, S, AMBER)
    # Two brushed coats lying across each other: a lighter one dragged up-right, a deeper one flat.
    body = lay(body, strokes(S, S, 300, 44, 0.34, 11, angle=28) * 0.55, AMBER_LIGHT)
    body = lay(body, strokes(S, S, 260, 36, 0.22, 12, angle=-8) * 0.40, AMBER_DEEP)
    # The pillow's own light: one big pale patch off-centre, where the dome catches the sky.
    yy, xx = np.mgrid[0:S, 0:S].astype(float)
    glow = np.clip(1 - np.hypot((xx - 215) / 190, (yy - 205) / 150), 0, 1) ** 1.5
    body = lay(body, glow * 0.30, AMBER_LIGHT)
    img = img * (1 - top[..., None]) + body * top[..., None]
    # THE WORN RIM: where the cover folds over the edge the paint has rubbed down to brown-gold,
    # in a ragged band, not an even border.
    rim = np.clip(top - square("cushion", 0.50, 0.06, S, wobble=9, seed=21, feather=5.0), 0, 1)
    img = lay(img, rim * 0.62, AMBER_DEEP)
    # THE PAINTED DOUBLE CHEVRON, cream road paint with an inked edge, the paint chipped by feet.
    c = S / 2
    half_w, drop, thick = px("cushion", 0.36), px("cushion", 0.21), px("cushion", 0.12)
    for k, apex in enumerate((c - px("cushion", 0.33), c - px("cushion", 0.03))):
        fat = chevron_mask(S, S, c, apex - 4, half_w + 4, drop * (half_w + 4) / half_w, thick + 8, 31 + k)
        thin = chevron_mask(S, S, c, apex, half_w, drop, thick, 31 + k)
        img = lay(img, np.clip(fat - thin, 0, 1) * top * 0.85, INK)
        chips = 1 - patches(S, S, 20, 0.05, 41 + k, feather=0.25) * 0.9     # worn through to the amber
        img = paint(img, thin * top * chips, CREAM, 51 + k, 0.04)
    # Shoe scuffs: a few dark soft smudges and pale rubbed spots where a body lands.
    img_scuffed = blobs(img, "8a5a08", 5, 15, seed=61, strength=0.30, wrap=False, region=(110, 300, 400, 420))
    img_scuffed = blobs(img_scuffed, "ffe08a", 4, 11, seed=62, strength=0.35, wrap=False, region=(90, 90, 420, 420))
    img = img * (1 - top[..., None]) + img_scuffed * top[..., None]
    # The inked line where the dome turns down into the wall, and grime at the wall's foot.
    img = lay(img, band("cushion", 0.553, 0.570, 0.09, 0.10, S, wobble=1.5, seed=71) * 0.75, INK)
    foot = np.clip(1 - square("cushion", 0.612, 0.11, S, wobble=4, seed=72, feather=4.0), 0, 1)
    img = lay(img, foot * 0.7, "6e4706")
    return img


def base():
    S = 512
    img = fill(S, S, RUBBER_DARK)                                   # the outer wall's foot, in the dirt
    body = fill(S, S, RUBBER)
    body = lay(body, strokes(S, S, 320, 50, 0.30, 111, angle=-20) * 0.45, RUBBER_LIGHT)
    body = lay(body, strokes(S, S, 280, 40, 0.22, 112, angle=65) * 0.50, RUBBER_DARK)
    plate = square("base", 0.85, 0.16, S)
    img = img * (1 - plate[..., None]) + body * plate[..., None]
    # The chamfer a foot climbs: rubbed pale along its top edge, dusty lower down.
    img = lay(img, band("base", 0.775, 0.800, 0.10, 0.11, S, wobble=2.5, seed=121) * 0.55, "8b8890")
    img = lay(img, band("base", 0.815, 0.852, 0.13, 0.16, S, wobble=5, seed=122) * 0.45, "5b554a")
    # THE WHITE OUTLINE (the sketch's frame): a painted cream band with an inked edge either side,
    # chipped through to the rubber where it is walked on.
    img = lay(img, band("base", 0.594, 0.706, 0.014, 0.080, S, wobble=1.6, seed=131) * 0.9, RUBBER_DARK)
    line = band("base", 0.605, 0.695, 0.02, 0.07, S, wobble=1.6, seed=131)
    chips = 1 - patches(S, S, 13, 0.09, 132, feather=0.25) * 0.95
    img = paint(img, line * chips, CREAM, 133, 0.05)
    img = lay(img, line * strokes(S, S, 200, 26, 0.25, 134, angle=40) * 0.22, "cfc7b0")
    # A bolt in each corner leaves a dark washer stain and a short rust run.
    c = S / 2
    for k, (sx, sy) in enumerate(((-1, -1), (1, -1), (1, 1), (-1, 1))):
        bx, by = c + sx * px("base", 0.715), c + sy * px("base", 0.715)
        img = lay(img, P.circle_mask(S, S, bx, by, px("base", 0.050), feather=3.0) * 0.75, RUBBER_DARK)
        img = blobs(img, "6b4a2a", 2, 5, seed=141 + k, strength=0.5, wrap=False,
                    region=(bx - 8, by + 4, bx + 8, by + 18))
    # The well the cushion sits in: darker, it is in the cushion's shade.
    well = square("base", 0.592, 0.10, S, feather=3.0)
    img = lay(img, well * 0.6, RUBBER_DARK)
    # Street dust gathered against the frame, a few broad pale patches.
    dust = patches(S, S, 60, 0.16, 151, feather=0.8) * (plate - well).clip(0, 1) * (1 - line)
    img = lay(img, dust * 0.22, "7a7468")
    return img


def chevron():
    W, H = 512, 320
    img = fill(H, W, CHEVRON_RIM)                                   # the sides take the rim's gold
    # The face's outline, as the model cuts it: half width 0.35, apex 0.21 above centre, arms
    # 0.15 thick measured vertically, their feet 0.21 below centre.
    yy, xx = np.mgrid[0:H, 0:W].astype(float)
    k = CHEVRON_PX_PER_M
    x, z = (xx - W / 2) / k, (H / 2 - yy) / k
    upper = 0.21 - 0.27 / 0.35 * np.abs(x)                         # the outer edge: 0.21 at the apex
    lower = upper - 0.15
    depth = np.minimum(np.minimum(upper - z, z - lower), 0.35 - np.abs(x))   # metres inside the outline
    depth = depth + smooth(H, W, 34, 213) * 0.0022                  # the rim's inner edge wanders
    face = np.clip((depth * k - 9) / 1.5 + 0.5, 0, 1)               # leave a rim about 9 px wide
    body = fill(H, W, CHEVRON)
    body = lay(body, strokes(H, W, 240, 30, 0.34, 211, angle=18) * 0.55, CHEVRON_LIGHT)
    body = lay(body, strokes(H, W, 200, 22, 0.20, 212, angle=-30) * 0.35, "f2c224")
    # A highlight dragged along the top of each arm, as if lit from above.
    shine = np.clip(1 - np.abs((upper - z) * k - 20) / 7, 0, 1) * (np.abs(x) < 0.30)
    body = lay(body, shine * 0.55, "fff8cf")
    return img * (1 - face[..., None]) + body * face[..., None]


def ring():
    S = 512
    img = fill(S, S, "d9d1ba")                                      # walls: a shade under the top
    top = band("ring", 0.605, 0.695, 0.02, 0.07, S)
    body = fill(S, S, CREAM)
    body = lay(body, strokes(S, S, 260, 30, 0.30, 311, angle=35) * 0.5, "ffffff")
    body = lay(body, strokes(S, S, 220, 26, 0.20, 312, angle=-50) * 0.4, "e3dbc4")
    img = img * (1 - top[..., None]) + body * top[..., None]
    # A soft inked edge either side of the top, so the frame reads as drawn.
    for inner, outer, ri, ro in ((0.597, 0.609, 0.02, 0.025), (0.691, 0.703, 0.066, 0.075)):
        img = lay(img, band("ring", inner, outer, ri, ro, S, wobble=1.2, seed=321) * 0.45, "a89f84")
    return img


def bolt():
    S = 192
    img = fill(S, S, "55535a")
    c = S / 2
    head = P.circle_mask(S, S, c, c, px("bolt", 0.032, S), feather=1.5)
    img = lay(img, head, "b9b6ae")
    img = lay(img, P.circle_mask(S, S, c - 8, c - 9, px("bolt", 0.019, S), feather=6.0) * 0.6, "e6e3da")
    ringm = np.clip(head - P.circle_mask(S, S, c, c, px("bolt", 0.027, S), feather=1.5), 0, 1)
    img = lay(img, ringm * 0.8, "3a383d")
    slot = rect_mask(S, S, (c - 34, c - 6, c + 34, c + 6), 4, wobble=1.0, seed=411)
    slot = ndimage.rotate(slot, 32, reshape=False, order=1)
    img = lay(img, np.clip(slot, 0, 1) * 0.85, "3a383d")
    return img


def main():
    atlas = np.zeros((SIZE, SIZE, 3))
    atlas[:] = hexcol(RUBBER_DARK)
    for name, fn in (("cushion", cushion), ("base", base), ("chevron", chevron), ("ring", ring), ("bolt", bolt)):
        x0, y0, x1, y1 = REGIONS[name]
        atlas[y0:y1, x0:x1] = fn()
    # The spare corner under the chevron bleeds its rim colour, so no mip pulls in a stranger.
    atlas[832:1024, 192:512] = hexcol(CHEVRON_RIM)
    out = Image.fromarray((np.clip(atlas, 0, 1) * 255).astype(np.uint8))
    for folder in (SOURCE, RUNTIME):
        folder.mkdir(parents=True, exist_ok=True)
        out.save(folder / f"{NAME}.png")
        print("[jump-pad-tex]", folder / f"{NAME}.png")
    if "--sheet" in sys.argv:
        path = Path(sys.argv[sys.argv.index("--sheet") + 1])
        path.parent.mkdir(parents=True, exist_ok=True)
        out.save(path)
        print("[jump-pad-tex] sheet", path)


if __name__ == "__main__":
    main()
