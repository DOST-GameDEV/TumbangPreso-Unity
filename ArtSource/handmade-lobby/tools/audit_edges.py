"""Finds pale fringes and halo pixels on transparent UI art.

A pixel is a fringe when it is visible (alpha > 8), sits within 6 px of the
transparent outside, and is clearly lighter and greyer than the solid colour
just inside it. Also reports colour stored under fully transparent pixels,
which bilinear filtering bleeds into the edge in Unity.
Usage: python -I tools/audit_edges.py file.png [more.png] [--mark out_dir]
"""
import sys, os
import numpy as np
from PIL import Image, ImageFilter

def audit(path, mark_dir=None):
    im = Image.open(path).convert('RGBA')
    a = np.asarray(im).astype(np.float32)
    rgb, al = a[..., :3], a[..., 3]
    solid = al > 250
    outside = al < 8
    out_img = Image.fromarray((outside * 255).astype(np.uint8))
    near_out = np.asarray(out_img.filter(ImageFilter.MaxFilter(13))) > 0
    band = (~outside) & near_out
    # interior reference colour: median of solid pixels in a 25 px window
    inner = Image.fromarray(np.where(solid[..., None], rgb, 0).astype(np.uint8))
    blur = np.asarray(inner.filter(ImageFilter.BoxBlur(10))).astype(np.float32)
    cnt = np.asarray(Image.fromarray((solid * 255).astype(np.uint8)).filter(ImageFilter.BoxBlur(10))).astype(np.float32) / 255
    ref = blur / np.maximum(cnt[..., None], 1e-3)
    lum = rgb.mean(-1); rlum = ref.mean(-1)
    sat = rgb.max(-1) - rgb.min(-1); rsat = ref.max(-1) - ref.min(-1)
    lighter = (al > 8) & (lum - rlum > 28) & (rsat - sat > 25)
    # A fringe is THIN: a pale region that is still there after a 2 px erosion is painted
    # (a white singlet, a grey pole), not a halo, and is left to the eye check.
    from scipy import ndimage
    thick = ndimage.binary_dilation(ndimage.binary_erosion(lighter, iterations=2), iterations=5)
    near_white = (al > 8) & (lum > 225) & (rgb.max(-1) - rgb.min(-1) < 40)
    cream = np.linalg.norm(rgb - np.array([255, 236, 214], np.float32), axis=-1) < 40
    hard = band & (al > 8) & (near_white | cream)
    review = band & (cnt > 0.05) & lighter & ~thick & ~hard
    fringe = hard | review
    hidden = outside & (al == 0) & (rgb.max(-1) > 40)
    n = int(hard.sum())
    print(f"{os.path.basename(path)}: {im.size[0]}x{im.size[1]} white_or_cream_edge_px={n} "
          f"review_px={int(review.sum())} bled_rgb_px={int(hidden.sum())}")
    if mark_dir and fringe.any():
        os.makedirs(mark_dir, exist_ok=True)
        m = np.zeros(a.shape, np.uint8); m[..., 3] = 255
        m[..., :3] = (rgb * 0.35).astype(np.uint8)
        m[fringe] = (255, 0, 255, 255)
        Image.fromarray(m).save(os.path.join(mark_dir, os.path.basename(path)))
    return n

if __name__ == '__main__':
    args = sys.argv[1:]; mark = None
    if '--mark' in args:
        i = args.index('--mark'); mark = args[i + 1]; del args[i:i + 2]
    total = sum(audit(p, mark) for p in args)
    sys.exit(1 if total else 0)
