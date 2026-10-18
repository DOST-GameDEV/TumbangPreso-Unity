"""Basilio's own effect masks (`FxStudyDanteBoulder`), painted by construction. Nothing here touches another effect's
picture: `tools/build_ultimate_fx_textures.py` paints the bench's, this paints only the two below.

  py -3 tools/build_dante_fx_textures.py

Writes into Assets/TumbangPreso/Resources/Textures/UltimateFx (where `FxStudyKit.Mask` looks):

  dante_split.png   1024 x 512. A SPLIT in the ground, read by `FxStudyRibbon`'s ramp in three flat steps:
                    a thin lip at its edge (grey 0.22), the dark depth across its whole width (0.50), and one hot seam
                    down its middle (1.0). Built as: one jagged centre line with a width that swells to the middle and
                    comes to a point at both ends, three short branches off it, then the distance from that shape's
                    edge decides lip or depth, and the distance from the centre line decides the seam.
  dante_puff.png    512 x 512, four puffs of dust, two a side. Each is a few overlapping discs with a flat base: the
                    body in one grey (0.55), the lit crowns of the same discs moved up and shrunk in a paler one (0.95),
                    a crisp outline. A slow unevenness is multiplied through so a puff that is thinned frays in patches.
"""
import uuid
from pathlib import Path

import cv2
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets/TumbangPreso/Resources/Textures/UltimateFx"


def save(name, grey):
    path = OUT / (name + ".png")
    cv2.imwrite(str(path), np.clip(grey * 255.0 + .5, 0, 255).astype(np.uint8))
    meta = path.with_suffix(".png.meta")
    if not meta.exists():
        # The same import settings as every other mask in the folder, under a name of its own.
        lines = (OUT / "scorch_fissure.png.meta").read_text().splitlines()
        lines[1] = "guid: " + uuid.uuid4().hex
        meta.write_text("\n".join(lines) + "\n")
    print("wrote %s  %dx%d  covered %.1f%%  white %.1f%%" % (path.name, grey.shape[1], grey.shape[0], 100 * (grey > .1).mean(), 100 * (grey > .9).mean()))


def split():
    w, h = 1024, 512
    shape = np.zeros((h, w), np.uint8)
    line = np.zeros((h, w), np.uint8)

    def stroke(points, widths):
        for (a, b), (wa, wb) in zip(zip(points[:-1], points[1:]), zip(widths[:-1], widths[1:])):
            for k in range(24):
                t = k / 23.0
                x, y = a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t
                cv2.circle(shape, (int(x), int(y)), max(1, int(wa + (wb - wa) * t)), 255, -1, cv2.LINE_AA)
        cv2.polylines(line, [np.array(points, np.int32)], False, 255, 1, cv2.LINE_AA)

    # The main split: jagged, widest a little off its middle, a point at each end.
    main = [(70, 268), (190, 236), (300, 286), (420, 240), (520, 262), (640, 222), (760, 270), (870, 244), (958, 262)]
    stroke(main, [2, 15, 26, 36, 40, 34, 24, 13, 2])
    # Three branches, each from a corner of the main one, each to a point.
    stroke([(300, 286), (262, 348), (208, 372)], [22, 12, 2])
    stroke([(640, 222), (688, 160), (752, 138)], [26, 13, 2])
    stroke([(520, 262), (548, 330), (530, 384)], [18, 9, 2])
    inside = cv2.distanceTransform(shape, cv2.DIST_L2, 5)
    from_line = cv2.distanceTransform(np.where(line > 96, 0, 255).astype(np.uint8), cv2.DIST_L2, 5)
    grey = np.zeros((h, w), np.float32)
    grey = np.where(inside > 0, .22, grey)
    grey = np.where(inside > 5, .50, grey)
    # The seam: a fifth of the split's own half width, so it thins to nothing toward the points.
    grey = np.where((inside > 5) & (from_line < np.maximum(1.2, inside * .28)), 1.0, grey)
    save("dante_split", cv2.GaussianBlur(grey.astype(np.float32), (0, 0), .8))


def puffs():
    cell, n = 256, 2
    rng = np.random.default_rng(7)
    sheet = np.zeros((cell * n, cell * n), np.float32)
    plans = [
        [(.34, .40, .22), (.56, .36, .26), (.74, .44, .17), (.46, .58, .20), (.64, .62, .15)],
        [(.30, .46, .19), (.50, .42, .27), (.70, .40, .20), (.56, .64, .17)],
        [(.40, .38, .25), (.64, .42, .23), (.50, .62, .21), (.28, .52, .13), (.78, .56, .12)],
        [(.36, .44, .24), (.60, .40, .22), (.72, .58, .16), (.46, .66, .16)],
    ]
    yy, xx = np.mgrid[0:cell, 0:cell].astype(np.float32) / cell
    for k, plan in enumerate(plans):
        body = np.zeros((cell, cell), np.float32)
        crown = np.zeros((cell, cell), np.float32)
        for x, y, r in plan:
            # y is typed up from the puff's base; the picture's rows run down.
            d = np.hypot(xx - x, (1 - yy) - y)
            body = np.maximum(body, (d < r).astype(np.float32))
            crown = np.maximum(crown, (np.hypot(xx - (x - r * .16), (1 - yy) - (y + r * .22)) < r * .66).astype(np.float32))
        body[(1 - yy) < .20] = 0                                   # the flat base
        uneven = cv2.GaussianBlur(rng.random((cell, cell)).astype(np.float32), (0, 0), 14)
        uneven = (uneven - uneven.min()) / (uneven.max() - uneven.min())
        grey = body * (.55 + .40 * crown) * (.78 + .22 * uneven)
        r0, c0 = (n - 1 - k // n) * cell, (k % n) * cell          # cell 0 is the sheet's bottom left, as `FxFlecks` counts
        sheet[r0:r0 + cell, c0:c0 + cell] = cv2.GaussianBlur(grey, (0, 0), 1.0)
    save("dante_puff", sheet)


if __name__ == "__main__":
    split()
    puffs()
