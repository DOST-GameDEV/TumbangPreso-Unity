"""Measures the owner's labelled HOME board (Canva TUMP page 68, exported at 2x) into board.json.

Every rebuilt sticker takes its exact size, corner radius and position from here, and every
label its box, size and colour from the Canva text layers (canva/page68_text.json), so the
game matches the board rather than an eyeballed copy of it. Units are the 1920x1080 layout.
"""
import json
import os

import cv2
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(HERE, 'canva', 'labelled68@2x.png')
A = np.asarray(Image.open(SRC).convert('RGBA')).astype(np.int32)
RGB, AL = A[..., :3], A[..., 3]


def hexc(h):
    h = h.lstrip('#'); return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)])


def bbox_of(mask):
    ys, xs = np.nonzero(mask)
    return [int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1]


def colour_mask(region, colour, tol):
    x0, y0, x1, y1 = region
    sub = RGB[y0:y1, x0:x1]
    m = (np.linalg.norm(sub - hexc(colour), axis=-1) < tol) & (AL[y0:y1, x0:x1] > 200)
    # keep the largest connected piece (labels and icons cut holes, that is fine for a bbox)
    n, lab, st, _ = cv2.connectedComponentsWithStats(m.astype(np.uint8), 8)
    if n <= 1: raise RuntimeError(f'no {colour} in {region}')
    big = 1 + int(np.argmax(st[1:, 4]))
    full = np.zeros(AL.shape, bool); full[y0:y1, x0:x1] = lab == big
    return full


def corner_radius(mask, box):
    """Radius from the top-left corner: how far right the first filled pixel sits on the top row."""
    x0, y0, x1, y1 = box
    row = np.nonzero(mask[y0 + 1, x0:x1])[0]
    col = np.nonzero(mask[y0:y1, x0 + 1])[0]
    # for a circular corner of radius r, the first pixel at depth 1 is at r - sqrt(2r - 1)
    d = min(row[0] if len(row) else 0, col[0] if len(col) else 0)
    r = 0.0
    while r - np.sqrt(max(2 * r - 1, 0)) < d and r < 200: r += 0.25
    return r


def u(v): return round(v / 2.0, 1)


def rect_units(box): return [u(box[0]), u(box[1]), u(box[2] - box[0]), u(box[3] - box[1])]


def measure():
    out = {}
    # (name, search region in 2x px, face colour, slab colour or None, tolerance)
    pieces = [
        ('hero',  (40, 880, 460, 1240), 'F0B947', 'D36A20', 45),
        ('item',  (40, 1260, 460, 1620), 'F0B847', 'D36A20', 45),
        ('shop',  (40, 1640, 580, 2140), '469678', '2D7D55', 40),
        ('task',  (600, 1800, 950, 2140), 'F19910', 'D36A20', 40),
        ('avatar', (30, 30, 300, 300), '56071D', None, 30),
        ('avatar_well', (60, 60, 280, 280), 'FCD39F', None, 30),
        ('plate', (320, 30, 940, 300), '56071D', None, 30),
        ('xp_track', (370, 180, 890, 270), 'FCD39F', None, 30),
        ('xp_fill', (370, 180, 890, 270), '4A9C7E', None, 40),
        ('level', (340, 70, 460, 160), 'E8873A', None, 70),
        ('currency_bar', (2960, 40, 3620, 240), '56071D', None, 30),
        ('currency_field', (3000, 60, 3330, 220), 'FCD39F', None, 30),
        ('plus', (3400, 60, 3600, 220), 'A8B63E', None, 40),
        ('menu', (3600, 40, 3840, 240), 'A31A24', None, 35),
        ('queue', (1480, 0, 2340, 240), '4A0618', None, 40),
        ('cancel', (2080, 30, 2240, 180), 'B43024', None, 45),
        ('mode', (2930, 1150, 3820, 1780), '54071C', None, 35),
        ('mode_window', (2980, 1280, 3780, 1600), '808080', None, 20),
        ('play_face', (2930, 1780, 3820, 2140), 'A5B93D', None, 60),
        ('play_floor', (2930, 1780, 3820, 2140), '46842A', None, 30),
    ]
    for name, region, face, slab, tol in pieces:
        m = colour_mask(region, face, tol)
        box = bbox_of(m)
        entry = {'face': rect_units(box), 'radius': round(corner_radius(m, box) / 2, 1)}
        if slab:
            # the slab is everything opaque in the piece's own component that is not the face
            x0, y0, x1, y1 = region
            al = AL[y0:y1, x0:x1] > 128
            n, lab, st, _ = cv2.connectedComponentsWithStats(al.astype(np.uint8), 8)
            big = 1 + int(np.argmax(st[1:, 4]))
            full = np.zeros(AL.shape, bool); full[y0:y1, x0:x1] = lab == big
            sb = bbox_of(full)
            entry['outer'] = rect_units(sb)
            entry['depth'] = [round(entry['outer'][2] - entry['face'][2], 1), round(entry['outer'][3] - entry['face'][3], 1)]
        out[name] = entry
    return out


if __name__ == '__main__':
    board = measure()
    with open(os.path.join(HERE, 'board.json'), 'w', encoding='utf-8') as fh:
        json.dump(board, fh, indent=1)
    for k, v in board.items(): print(k, v)
