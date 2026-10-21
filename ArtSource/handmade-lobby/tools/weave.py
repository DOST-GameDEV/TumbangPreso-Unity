"""Builds the knit weave field every rebuilt sticker is printed with.

The owner's good tiles (HERO, ITEM, SHOP, TASK, PLAY, the mode card) share one
hand-painted knit texture. Only their INTERIORS are sampled, never an edge, so
no pale rim or stray speck can travel into the new art. The samples become a
zero-mean luminance detail map, quilted (Efros and Freeman, minimum-error seams)
into one large field so a wide panel never shows a repeating tile.

Output: work/weave.npy (float32, unit standard deviation) and a preview PNG.
"""
import os
import numpy as np
import cv2
from PIL import Image

HERE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(HERE, 'canva', 'slide68@2x.png')

# Interior rectangles (2x export pixels), each well inside a good tile's face.
INTERIORS = [
    (90, 940, 330, 1130),     # HERO tile face
    (90, 1320, 330, 1500),    # ITEM tile face
    (90, 1700, 440, 2000),    # SHOP tile face
    (650, 1840, 860, 2000),   # TASK tile face
    (2990, 1600, 3440, 1740), # mode card body under the window, left of the chevron
]

BLOCK, OVERLAP, SIZE = 112, 28, 2048


def detail_pool():
    a = np.asarray(Image.open(SRC).convert('RGB')).astype(np.float32)
    pool = []
    for x0, y0, x1, y1 in INTERIORS:
        L = a[y0:y1, x0:x1].mean(-1)
        d = L / cv2.GaussianBlur(L, (0, 0), 10) - 1
        d = (d - d.mean()) / d.std()
        d = np.clip(d, -4.0, 2.5)
        pool.append(d)
        pool.append(np.rot90(d).copy())
        pool.append(d[:, ::-1].copy())
    return pool


def min_cut(err):
    """Minimum-error vertical path through err (h x w); returns mask, 1 = new block."""
    h, w = err.shape
    cost = err.copy()
    for y in range(1, h):
        left = np.r_[np.inf, cost[y - 1, :-1]]
        right = np.r_[cost[y - 1, 1:], np.inf]
        cost[y] += np.minimum(np.minimum(left, cost[y - 1]), right)
    mask = np.zeros_like(err)
    x = int(np.argmin(cost[-1]))
    for y in range(h - 1, -1, -1):
        mask[y, x:] = 1
        if y:
            lo, hi = max(0, x - 1), min(w, x + 2)
            x = lo + int(np.argmin(cost[y - 1, lo:hi]))
    # soften the seam a pixel so it never reads as a line
    return cv2.GaussianBlur(mask, (0, 0), 0.8)


def quilt(pool, rng):
    step = BLOCK - OVERLAP
    n = SIZE // step + 2
    out = np.zeros((n * step + OVERLAP, n * step + OVERLAP), np.float32)
    for by in range(n):
        for bx in range(n):
            y, x = by * step, bx * step
            best, best_err = None, np.inf
            for _ in range(24):
                src = pool[rng.integers(len(pool))]
                sy = rng.integers(0, src.shape[0] - BLOCK)
                sx = rng.integers(0, src.shape[1] - BLOCK)
                blk = src[sy:sy + BLOCK, sx:sx + BLOCK]
                if blk.std() > 1.35 or np.abs(blk).max() > 6: continue  # a stray mark, not knit
                e = 0.0
                if bx: e += ((blk[:, :OVERLAP] - out[y:y + BLOCK, x:x + OVERLAP]) ** 2).sum()
                if by: e += ((blk[:OVERLAP, :] - out[y:y + OVERLAP, x:x + BLOCK]) ** 2).sum()
                if e < best_err: best, best_err = blk, e
            blk = best.copy()
            m = np.ones_like(blk)
            if bx:
                err = (blk[:, :OVERLAP] - out[y:y + BLOCK, x:x + OVERLAP]) ** 2
                m[:, :OVERLAP] *= min_cut(err)
            if by:
                err = (blk[:OVERLAP, :] - out[y:y + OVERLAP, x:x + BLOCK]) ** 2
                m[:OVERLAP, :] *= min_cut(err.T).T
            region = out[y:y + BLOCK, x:x + BLOCK]
            out[y:y + BLOCK, x:x + BLOCK] = region * (1 - m) + blk * m
    out = out[:SIZE, :SIZE]
    return (out - out.mean()) / out.std()


if __name__ == '__main__':
    rng = np.random.default_rng(20261021)
    field = quilt(detail_pool(), rng)
    os.makedirs(os.path.join(HERE, 'work'), exist_ok=True)
    np.save(os.path.join(HERE, 'work', 'weave.npy'), field.astype(np.float32))
    prev = np.clip(200 * (1 + 0.035 * field), 0, 255).astype(np.uint8)
    Image.fromarray(prev[:900, :900]).save(os.path.join(HERE, 'work', 'weave_preview.png'))
    print('weave', field.shape, float(field.std()))
