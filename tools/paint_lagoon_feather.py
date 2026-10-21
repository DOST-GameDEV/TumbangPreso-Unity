"""Paints the Lagoon Cove burst FEATHER: Assets/TumbangPreso/Art/LagoonCove/Fauna/feather_albedo.png.

  py -3 tools/paint_lagoon_feather.py            # write the texture
  py -3 tools/paint_lagoon_feather.py --preview N  # also Logs/lagoon-blender/feather_vN.png (on dark and light)

WHY (owner, 2026-09-27, after the first slipper-burst pass threw thin grey boxes): "can you make an
actual feather texture, instead of just thin blocks?". One stylized contour feather, drawn in
code so it can be re-tuned: a curved quill, a vane that is narrow at the base, widest a third of
the way up and rounds off at the tip, the trailing side a little wider than the leading side (a
real flight feather's asymmetry, which is what makes it read as a feather rather than a leaf),
soft barb lines swept toward the tip, two splits in the vane where the barbs have parted, and a
fringe of down at the base.

⚠️ STYLIZED, NOT REALISTIC (docs/Art_Direction.md section 0). Barbs are a handful of soft value
bands, not hundreds of hairs; the shape carries it. The texture is near-white greyscale and the
material colour tints it, so one drawing serves the white, cream and grey feathers of the bird.

⚠️ ALPHA IS A CUT-OUT (the painted shader clips at _Cutoff), so the edge is drawn hard; the down
at the base is a ragged hard edge, not a soft fade, which a cut-out would turn to noise.
The quill runs along +V (texture bottom to top), base at the bottom.
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets/TumbangPreso/Art/LagoonCove/Fauna/feather_albedo.png"
W, H = 128, 384


def _smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def paint():
    rng = np.random.default_rng(27)
    ys, xs = np.mgrid[0:H, 0:W].astype(np.float32)
    v = 1.0 - ys / (H - 1)                    # 0 at the base (bottom), 1 at the tip (top)
    u = (xs - (W - 1) / 2) / ((W - 1) / 2)    # -1 .. 1 across

    # The quill bends gently to the leading side toward the tip.
    bend = 0.16 * v ** 2
    s = u - bend                              # across, measured from the quill

    # Vane half-widths along the feather: narrow base, widest near v = 0.35, round tip.
    grow = _smooth(0.10, 0.34, v)
    # The tip sweeps: the leading edge curves in early, the trailing edge carries on and rounds late.
    tip_lead = np.clip(1 - ((v - 0.34) / 0.60) ** 2, 0, 1) ** 0.5
    tip_trail = np.clip(1 - (np.abs(v - 0.34) / 0.64) ** 2.2, 0, 1) ** 0.55
    lead = 0.40 * np.where(v < 0.34, grow, tip_lead)     # leading side narrower (the flight-feather asymmetry)
    trail = 0.72 * np.where(v < 0.34, grow, tip_trail)
    half = np.where(s < 0, lead, trail)

    # Two splits where the barbs have parted: a thin wedge cut from each edge, swept to the tip.
    # A notch that OPENS toward the edge (a V between two parted barbs), following the barb sweep.
    def split(v0, side, depth):
        across = np.abs(s) / np.maximum(half, 1e-3)
        along = v - (v0 + 0.20 * np.abs(s))
        width = 0.004 + 0.022 * _smooth(1 - depth, 1.0, across)
        return (np.abs(along) < width) & (np.sign(s) == side) & (across > 1 - depth)
    cut = split(0.50, 1, 0.55) | split(0.70, -1, 0.45)

    inside = (np.abs(s) < half) & (v > 0.015) & (v < 0.985) & ~cut

    # Down at the base: a ragged fringe of tufts below v = 0.14, wider than the vane there.
    # Rounded tufts, not noise (v1's speckle turned to grit under the cut-out): a few soft lobes
    # either side of the quill whose outline wobbles along the feather.
    tuft = 0.5 + 0.5 * np.sin(v * 70 + np.sign(s) * 1.7)
    reach = (0.20 + 0.16 * _smooth(0.16, 0.05, v)) * (0.75 + 0.25 * tuft)
    down = (v < 0.17) & (v > 0.05) & (np.abs(s) < reach)
    shape = inside | down

    # The quill itself: a tapering stroke, and a bare calamus below the vane.
    quill_w = 0.05 * (1 - 0.8 * v) + 0.014
    quill = (np.abs(s) < quill_w) & (v < 0.97)
    shape |= quill & (v > 0.0)

    # Value: near-white vane, a soft darker band toward each edge, barb lines swept to the tip.
    edge = np.clip(np.abs(s) / np.maximum(half, 1e-3), 0, 1)
    val = 0.96 - 0.10 * _smooth(0.55, 1.0, edge)
    barbs = np.sin((v - 0.20 * np.abs(s)) * 95.0)
    val -= 0.06 * _smooth(0.2, 1.0, barbs) * _smooth(0.06, 0.25, np.abs(s))
    val = np.where(down & ~inside, 0.88 + 0.06 * tuft, val)
    val = np.where(quill, 0.74 + 0.10 * v, val)
    val = np.clip(val, 0, 1)

    rgb = (np.dstack([val, val * 0.985, val * 0.96]) * 255).astype(np.uint8)
    alpha = (shape * 255).astype(np.uint8)
    img = Image.fromarray(np.dstack([rgb, alpha]), "RGBA")
    OUT.parent.mkdir(parents=True, exist_ok=True)
    img.save(OUT)
    print("[feather] wrote", OUT, "coverage", round(float(shape.mean()), 3))
    return img


def preview(img, n):
    out = ROOT / "Logs/lagoon-blender" / f"feather_v{n}.png"
    if out.exists():
        sys.exit(f"{out} exists: bump the version")
    big = img.resize((W * 2, H * 2), Image.NEAREST)
    sheet = Image.new("RGB", (W * 4 + 30, H * 2 + 20), (60, 50, 60))
    light = Image.new("RGB", (W * 2, H * 2), (242, 196, 160))
    sheet.paste(big, (10, 10), big)
    light.paste(big, (0, 0), big)
    sheet.paste(light, (W * 2 + 20, 10))
    sheet.save(out)
    print("[feather] preview", out)


if __name__ == "__main__":
    image = paint()
    if "--preview" in sys.argv:
        preview(image, int(sys.argv[sys.argv.index("--preview") + 1]))
