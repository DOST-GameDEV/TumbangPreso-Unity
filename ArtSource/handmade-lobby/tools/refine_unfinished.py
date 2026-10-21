"""Refines the owner's UNFINISHED HOME pieces to match her finished ones.

Owner, 2026-10-21: the avatar and name plate, the currency bar with its + and menu, and the
mode card are unfinished; refine them to look like the finished tiles and PLAY. The finished
pieces themselves are never edited (tools/extract_originals.py places them untouched).

What "finished" means on her board, read off PLAY and the tiles:
  - her knit on the face. Here it is HER knit, lifted from her mode card (maroon) and PLAY
    (green) as a brightness pattern and laid on each flat face in that face's own colour;
  - a darker floor under the face (PLAY's deep green floor), drawn below her silhouette so
    no text, inlay or glyph moves. Peach inlays stay flat (owner: peach knit read as stains).
The mode card's bottom was a crop line on the board (her original runs on into PLAY); it is
finished at the board's crop with rounded corners, then floored like the rest.

Reads out_src/ (run extract_originals.py first), writes out_src/<piece>_refined.png and
work/before_after/<piece>_refined.png, plus work/look/board_refined.png (her no-text board
with the refined pieces and her text, for the owner's yes before anything ships).
Usage: python -I tools/refine_unfinished.py
"""
import json
import os
import sys

import cv2
import numpy as np
from PIL import Image
from scipy import ndimage

HERE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(HERE, 'tools'))
import audit_edges  # noqa: E402
from extract_originals import fix_edges, png, on  # noqa: E402

S = 2
PAD = 6
OUT = os.path.join(HERE, 'out_src')
BA = os.path.join(HERE, 'work', 'before_after')
FLOOR_SHADE = 0.6          # PLAY's floor against its face, measured
MODE_CROP_Y = 560.1979179791065 + 317.43997243823526   # her mode card frame's bottom on the board


def hexc(h):
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], np.float32) / 255


def read(name):
    a = np.asarray(Image.open(os.path.join(OUT, name + '.png')).convert('RGBA')).astype(np.float32) / 255
    return a[..., :3], a[..., 3]


def knit_pattern(name):
    """Her knit as a brightness ratio: the interior of her knit piece over its local mean."""
    rgb, a = read(name)
    core = ndimage.binary_erosion(a > 0.99, iterations=24)
    ys, xs = np.nonzero(core)
    y0, y1, x0, x1 = ys.min(), ys.max(), xs.min(), xs.max()
    lum = rgb[y0:y1, x0:x1].mean(-1)
    mean = cv2.GaussianBlur(lum, (0, 0), 18)
    return np.clip(lum / np.maximum(mean, 1e-3), 0.6, 1.4)


def tile_pattern(pat, h, w, seed):
    """A crop of her knit big enough for the piece (mirrored repeats if it is not)."""
    ph, pw = pat.shape
    if ph < h or pw < w:
        reps = (int(np.ceil(h / ph)) + 1, int(np.ceil(w / pw)) + 1)
        pat = np.block([[pat if (i + j) % 2 == 0 else pat[::-1, ::-1] for j in range(reps[1])] for i in range(reps[0])])
        ph, pw = pat.shape
    rng = np.random.default_rng(seed)
    oy, ox = rng.integers(0, ph - h + 1), rng.integers(0, pw - w + 1)
    return pat[oy:oy + h, ox:ox + w]


def flat_mask(rgb, a, colour, tol=0.09):
    """Her flat face colour only (inlays, glyphs and painted parts keep their pixels)."""
    return (a > 0) & (np.linalg.norm(rgb - colour, axis=-1) < tol)


def refine(rgb, a, pat, depth, seed, face_col, knit=True):
    """Knit on her flat face colour, then a floor of `depth` units under her silhouette."""
    d = int(round(depth * S))
    h, w = a.shape
    H = h + d
    rgb = np.concatenate([rgb, np.repeat(rgb[-1:], d, 0)], 0)
    a = np.concatenate([a, np.zeros((d, w), np.float32)], 0)
    solid = a > 0.5
    m = tile_pattern(pat, H, w, seed)[..., None]
    out = rgb.copy()
    if knit:
        fm = flat_mask(rgb, a, face_col)
        out = np.where(fm[..., None], np.clip(rgb * m, 0, 1), rgb)
    # floor: her silhouette swept straight down by d, darker, in the same knit
    floor_a = np.zeros_like(a)
    for k in range(1, d + 1):
        floor_a[k:] = np.maximum(floor_a[k:], a[:-k])
    floor_rgb = np.clip(face_col * FLOOR_SHADE * m, 0, 1) * np.ones_like(out)
    # face over floor (premultiplied over)
    A = a + floor_a * (1 - a)
    C = (out * a[..., None] + floor_rgb * (floor_a * (1 - a))[..., None]) / np.maximum(A[..., None], 1e-4)
    # for the press: the face alone, and the floor as a whole slab (it runs under the face too)
    slab_a = np.maximum(floor_a, a)
    return C, A, (out, a), (floor_rgb, slab_a)


def rrect(h, w, x, y, rw, rh, r):
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32) + 0.5
    cx, cy = x + rw / 2, y + rh / 2
    qx = np.abs(xx - cx) - rw / 2 + r
    qy = np.abs(yy - cy) - rh / 2 + r
    sdf = np.hypot(np.maximum(qx, 0), np.maximum(qy, 0)) + np.minimum(np.maximum(qx, qy), 0) - r
    return np.clip(0.5 - sdf, 0, 1)


def avatar():
    """Her avatar frame and peach well are Canva shapes (#56071D r25, #FCD39F r20); drawn from
    those exact shape layers at 2x, then refined like the rest."""
    fx, fy, fs = 19.999999999999986, 19.687944943430807, 126.40903439126225
    wx, wy, ws = 33.08457237270102, 33.64960499290365, 100.0
    x0, y0 = np.floor(fx * S) / S - PAD / S, np.floor(fy * S) / S - PAD / S
    W = H = int(np.ceil(fs * S)) + 2 * PAD + 1
    fa = rrect(H, W, (fx - x0) * S, (fy - y0) * S, fs * S, fs * S, 25 * 0.936 * S)
    wa = rrect(H, W, (wx - x0) * S, (wy - y0) * S, ws * S, ws * S, 20 * 0.936 * S)
    maroon = np.array([0x56, 0x07, 0x1D], np.float32) / 255
    peach = np.array([0xFC, 0xD3, 0x9F], np.float32) / 255
    rgb = maroon * (1 - wa[..., None]) + peach * wa[..., None]
    return rgb.astype(np.float32), fa.astype(np.float32), (round(x0, 3), round(y0, 3))


def main():
    man = json.load(open(os.path.join(OUT, 'manifest.json')))
    maroon_knit = knit_pattern('mode_card')
    green_knit = knit_pattern('play')
    maroon, red, lime = hexc('56071D'), hexc('A31A24'), hexc('A8B63E')
    jobs = {
        # name: (depth in board units, knit pattern, her face colour on the board)
        'name_plate': (9, maroon_knit, maroon),
        'currency_bar': (9, maroon_knit, maroon),
        'menu_plate': (9, maroon_knit, red),
        'plus_button': (5, green_knit, lime),
        'mode_card': (12, maroon_knit, maroon),
        'avatar': (9, maroon_knit, maroon),
    }
    refined = {}
    for i, (name, (depth, pat, col)) in enumerate(jobs.items()):
        if name == 'avatar':
            rgb, a, origin = avatar()
            raw = (rgb, a)
        else:
            rgb, a = read(name)
            origin = tuple(man[name]['origin'])
            raw = (rgb.copy(), a.copy())
        knit = name != 'mode_card'          # her mode card already wears her knit
        if name == 'mode_card':
            # finish the board's crop line: cut at the frame bottom, round those two corners
            cut = int(round((MODE_CROP_Y - origin[1]) * S))
            a = a.copy(); a[cut:] = 0
            r = 23 * S
            h, w = a.shape
            ys, xs = np.nonzero(a > 0.5)
            left, right = xs.min(), xs.max() + 1
            box = rrect(h, w, left, ys.min(), right - left, cut - ys.min(), r)
            top_half = np.zeros_like(a); top_half[:ys.min() + int((cut - ys.min()) / 2)] = 1
            a = np.where(top_half > 0, a, np.minimum(a, box))
            a, rgb = a[:cut + PAD], rgb[:cut + PAD]
        col = np.median(rgb[flat_mask(rgb, a, col, 0.12)], 0)   # her exact pixel colour near the board's
        rgb, a, face, slab = refine(rgb, a, pat, depth, i, col, knit=knit)
        rgb, a, _ = fix_edges(rgb, a)
        for part, (pr, pa) in (('face', face), ('slab', slab)):
            pr, pa, _ = fix_edges(pr, pa)
            png(pr, pa, os.path.join(OUT, f'{name}_{part}_r.png'))
        path = os.path.join(OUT, name + '_refined.png')
        png(rgb, a, path)
        refined[name] = (rgb, a, origin)
        # before/after: pad the raw piece to the refined height so both sit side by side
        r0, a0 = raw
        if name == 'mode_card':
            r0, a0 = r0[:a.shape[0]], a0[:a.shape[0]]
        if a0.shape[0] < a.shape[0]:
            pad = a.shape[0] - a0.shape[0]
            r0 = np.concatenate([r0, np.zeros((pad, r0.shape[1], 3), np.float32)])
            a0 = np.concatenate([a0, np.zeros((pad, a0.shape[1]), np.float32)])
        from extract_originals import before_after
        before_after(name + '_refined', (r0, a0), (rgb, a))
        audit_edges.audit(path)
    json.dump({k: {'origin': list(v[2]), 'size': [v[1].shape[1] / S, v[1].shape[0] / S], 'depth': jobs[k][0]} for k, v in refined.items()},
              open(os.path.join(OUT, 'refined_manifest.json'), 'w'), indent=1)

    # preview: her no-text board, refined pieces over the unfinished ones, her text on top
    base = np.asarray(Image.open(os.path.join(HERE, 'canva', 'slide68@2x.png')).convert('RGBA')).astype(np.float32) / 255
    lab = np.asarray(Image.open(os.path.join(HERE, 'canva', 'labelled68@2x.png')).convert('RGBA')).astype(np.float32) / 255
    ground = np.array([0x3a, 0x3a, 0x44], np.float32) / 255
    img = base[..., :3] * base[..., 3:] + ground * (1 - base[..., 3:])
    def paste(rgb, a, origin):
        X, Y = int(round(origin[0] * S)), int(round(origin[1] * S))
        h, w = a.shape
        ys, xs = slice(max(Y, 0), min(Y + h, img.shape[0])), slice(max(X, 0), min(X + w, img.shape[1]))
        sa = a[ys.start - Y:ys.stop - Y, xs.start - X:xs.stop - X, None]
        img[ys, xs] = rgb[ys.start - Y:ys.stop - Y, xs.start - X:xs.stop - X] * sa + img[ys, xs] * (1 - sa)

    board = img.copy()

    def from_board(rect, plate):
        """A shape layer that sits on a plate (inlay, glyph, window): her board pixels in its rect
        that are not the plate's colour."""
        x, y, w, h = [int(round(v * S)) for v in rect]
        sub = board[y:y + h, x:x + w]
        m = (np.linalg.norm(sub - plate, axis=-1) > 0.12).astype(np.float32)[..., None]
        img[y:y + h, x:x + w] = sub * m + img[y:y + h, x:x + w] * (1 - m)

    def piece(name):
        rgb, a = read(name); paste(rgb, a, man[name]['origin'])

    # Canva's stacking order, refined plates in place of the unfinished ones
    paste(*refined['avatar'])
    from_board((33.08, 33.65, 100, 100), maroon)                 # peach well (flat)
    paste(*refined['name_plate'])
    piece('level_hex'); piece('xp_track')
    from_board((189.4, 96.7, 167.1, 35.2), hexc('FCD39F'))       # her XP stripes
    paste(*refined['currency_bar'])
    from_board((1521.65, 41.72, 185.32, 57.52), maroon)          # peach currency field
    piece('icon_coin')
    paste(*refined['plus_button'])
    from_board((1734.6, 57.2, 30.7, 30.7), lime)                 # plus glyph
    paste(*refined['menu_plate'])
    piece('menu_lines')
    paste(*refined['mode_card'])
    from_board((1501, 651.5, 370.5, 144), maroon)                # grey window
    from_board((1837.7, 812.7, 33.9, 49.5), maroon)              # chevron
    text = np.abs(lab - base).sum(-1) > 0.08
    text = ndimage.binary_erosion(text, iterations=0) if False else text
    text = ndimage.binary_dilation(text, iterations=1)
    lab_rgb = lab[..., :3] * lab[..., 3:] + ground * (1 - lab[..., 3:])
    img = np.where(text[..., None], lab_rgb, img)
    Image.fromarray((np.clip(img, 0, 1) * 255 + 0.5).astype(np.uint8)).save(os.path.join(HERE, 'work', 'look', 'board_refined.png'))
    print('preview -> work/look/board_refined.png')


if __name__ == '__main__':
    main()
