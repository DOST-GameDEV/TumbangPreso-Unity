"""Cuts the owner's HOME pieces from her ORIGINAL images and fixes their edges only.

Owner, 2026-10-21: use her actual drawings; only fix their edges (smooth her silhouette,
drop specks, recolour cream rim pixels with the colour just inside). Never remake,
redraw, retexture or add shadows. Every pixel inside the edge stays hers.

Sources are her uncropped media, exported at native size from TUMP pages 70-78
(canva/src/*.png). The cut lines, specks and rim in the old page export came from
Canva cropping these images on the board, so nothing here is cut from that export.
canva/copy_elements.json holds each board element's frame and imageBox (the same
ids sit on TUMP page 69), which places every native pixel on the board exactly.

Each piece is resampled once to 2 px per board unit (the 4K layout), then:
  1. specks under 400 px go;
  2. cream rim pixels (#FFECD6 cutout rim, connected to the outside within 14 px)
     take the colour of the nearest pixel inside them; the silhouette keeps them;
  3. the silhouette is smoothed: 4x upsample, Gaussian sigma 3, threshold 0.5,
     area-downsample;
  4. the outer 3 px ring takes its colour from 3-4 px inside (no contaminated rim);
  5. colour is bled under alpha 0 so Unity's filtering never pulls in a halo.

Writes out_src/<piece>.png, out_src/manifest.json (board origin and size) and
work/before_after/<piece>.png (1:1, raw original vs fixed, on dark and mid grounds).
Usage: python -I tools/extract_originals.py
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

S = 2            # output pixels per board unit
PAD = 6          # transparent pixels around every sprite
SRC = os.path.join(HERE, 'canva', 'src')
OUT = os.path.join(HERE, 'out_src')
BA = os.path.join(HERE, 'work', 'before_after')
CREAM = np.array([255, 236, 214], np.float32) / 255

# piece name: (source file, board element id)
PIECES = {
    'tile_hero_face': ('hero_face', 'LBjljPGTDwj3GQT0'),
    'tile_item_face': ('item_face', 'LBbx3qc7z6vkTWQr'),
    'tile_shop_face': ('shop_face', 'LByKtQKTV7YXC0Wd'),
    'tile_task_face': ('task_face', 'LBC8NXSm6KHpVL4h'),
    'play': ('play', 'LBtNyfdt6DZwBLH3'),
    'mode_card': ('mode_card', 'LBMF6NlDLBqXl1XJ'),
    'queue_plate': ('queue_plate', 'LBKNj9q4DkDxdzDs'),
    'name_plate': ('sheet_panels', 'LBcTlTB1G6NSNbv8'),
    'currency_bar': ('sheet_panels', 'LBYxrZH25k6xp3sq'),
    'menu_plate': ('sheet_panels', 'LBzjh8sdY1TY7KvB'),
    'icon_hero': ('sheet_icons', 'LBD1jlXxBG332w0B'),
    'icon_item': ('sheet_icons', 'LBG2s3SVYg5yGmJW'),
    'icon_shop': ('sheet_icons', 'LBZ7HDmr3mNzkT4C'),
    'icon_task': ('sheet_icons', 'LB5d8WV7K0HSwSRl'),
    'icon_coin': ('sheet_icons', 'LBnKxjPZK0BkcskL'),
    'plus_button': ('sheet_icons', 'LBRyS7Mk1HBvDL4G'),
    'menu_lines': ('sheet_icons', 'LBjGZsvXV6kzq86P'),
    'x_button': ('sheet_icons', 'LBfwWC4sktMYKJFW'),
    'level_hex': ('sheet_icons', 'LBVYfxpz0Q4WY2Br'),
    'xp_track': ('sheet_icons', 'LBR4DK7hVsh3XdJt'),
}


# ⚠️ HER FINISHED PIECES ARE NOT EDITED (owner, 2026-10-21: "the finished ones ur NOT ALLOWED
# TO EDIT"). They are only placed on the 2x board grid; colour under fully transparent pixels is
# bled (invisible, it only stops Unity's filter pulling in a halo). No edge fix, no recolour.
FINISHED = {'tile_hero_face', 'tile_item_face', 'tile_shop_face', 'tile_task_face', 'play', 'queue_plate',
            'icon_hero', 'icon_item', 'icon_shop', 'icon_task'}
# Owner: on the finished buttons "u can only smooth edges if bobo export and NOTHING ELSE". These
# exports carry a near-white hairline of anti-aliasing all round (measured 9-468 px per piece), so
# they get the edge fix and nothing else; inside the edge every pixel stays hers. The icons' cream
# outline is her sticker border (it is on her final board), so the icons are left as they are.
BAD_EXPORT_EDGE = {'tile_hero_face', 'tile_item_face', 'tile_shop_face', 'tile_task_face', 'play', 'queue_plate'}


def bleed_only(rgb, a):
    solid = a > 0.5
    _, (iy, ix) = ndimage.distance_transform_edt(~solid, return_indices=True)
    return np.where((a <= 0)[..., None], rgb[iy, ix], rgb), a, 0


def load(name):
    a = np.asarray(Image.open(os.path.join(SRC, name + '.png')).convert('RGBA')).astype(np.float32) / 255
    return a


def select(alpha, frame):
    """Her whole pieces under the board frame: every component of 400+ px with at least half
    of itself inside the frame. Whole components, so the frame never cuts her art."""
    u0, v0, u1, v1 = frame
    n, lab, st, _ = cv2.connectedComponentsWithStats((alpha > 0).astype(np.uint8), 8)
    keep = np.zeros(alpha.shape, bool); cut = False
    for i in range(1, n):
        x, y, w, h, area = st[i]
        if area < 400: continue
        m = lab == i
        inside = m[max(0, int(v0)):int(np.ceil(v1)), max(0, int(u0)):int(np.ceil(u1))].sum()
        if inside >= 0.5 * area:
            keep |= m
            if inside < area: cut = True
    return keep, cut


def resample(rgba, mask, el, native_w):
    """Her pixels (only the selected pieces) placed on the 2x board grid in one resample."""
    top, bleft, bw, _ = el['box']
    s = bw / native_w                         # board units per native pixel
    ox, oy = el['left'] + bleft, el['top'] + top   # board position of native pixel (0, 0)
    ys, xs = np.nonzero(mask)
    m = 3
    x0, x1 = xs.min() - m, xs.max() + 1 + m
    y0, y1 = ys.min() - m, ys.max() + 1 + m
    bx0 = np.floor((ox + x0 * s) * S) / S - PAD / S
    by0 = np.floor((oy + y0 * s) * S) / S - PAD / S
    W = int(np.ceil((ox + x1 * s - bx0) * S)) + PAD
    H = int(np.ceil((oy + y1 * s - by0) * S)) + PAD
    pre = rgba.copy()
    pre[..., 3] *= mask
    pre[..., :3] *= pre[..., 3:4]
    P = 64   # zero margin so the sampling box may run past her canvas edge
    pre = np.pad(pre, ((P, P), (P, P), (0, 0)))
    # dst pixel X covers board [bx0 + X / S, bx0 + (X + 1) / S]; native pixel u covers
    # board [ox + u * s, ox + (u + 1) * s]. One Lanczos resample (PIL filters correctly
    # for both enlarging and reducing) of premultiplied channels.
    box = ((bx0 - ox) / s + P, (by0 - oy) / s + P, (bx0 + W / S - ox) / s + P, (by0 + H / S - oy) / s + P)
    out = np.dstack([np.asarray(Image.fromarray(pre[..., c], 'F').resize((W, H), Image.LANCZOS, box=box))
                     for c in range(4)])
    k = 1.0 / (S * s)
    a = np.clip(out[..., 3], 0, 1)
    rgb = np.clip(out[..., :3], 0, None)
    rgb = np.where(a[..., None] > 1e-4, rgb / np.maximum(a[..., None], 1e-4), 0)
    return np.clip(rgb, 0, 1), a, (round(float(bx0), 3), round(float(by0), 3)), 1.0 / k


def keep_big(solid, min_px=400):
    n, lab, st, _ = cv2.connectedComponentsWithStats(solid.astype(np.uint8), 8)
    keep = np.zeros_like(solid)
    for i in range(1, n):
        if st[i, 4] >= min_px: keep |= lab == i
    return keep


def fix_edges(rgb, a):
    """Edge work only. Inside the edge every pixel is hers, untouched."""
    solid = keep_big(a > 0.5)
    a = a * ndimage.binary_dilation(solid, iterations=3)       # specks go, her AA stays

    # cream cutout rim: recolour (never remove) rim pixels reachable from outside within 14 px
    pale = solid & (np.linalg.norm(rgb - CREAM, axis=-1) < 0.16)
    dist_in = ndimage.distance_transform_edt(solid)
    ring = pale & (dist_in <= 14)
    seed = ring & (dist_in <= 3.0)
    rim = ndimage.binary_propagation(seed, mask=ring) if seed.any() else np.zeros_like(solid)
    if rim.any():
        src = solid & ~rim
        _, (iy, ix) = ndimage.distance_transform_edt(~src, return_indices=True)
        rgb = np.where(rim[..., None], rgb[iy, ix], rgb)

    # smooth silhouette from her own alpha
    h, w = a.shape
    up = cv2.resize(solid.astype(np.float32), (w * 4, h * 4), interpolation=cv2.INTER_LINEAR)
    up = cv2.GaussianBlur(up, (0, 0), 3.0)
    alpha = cv2.resize((up > 0.5).astype(np.float32), (w, h), interpolation=cv2.INTER_AREA)

    # outer 3 px ring takes the colour from 3-4 px inside
    inner = ndimage.distance_transform_edt(alpha > 0.99)
    core = inner >= 3.5
    if core.any():
        _, (iy, ix) = ndimage.distance_transform_edt(~core, return_indices=True)
        ring3 = (alpha > 0) & ~core
        rgb = np.where(ring3[..., None], rgb[iy, ix], rgb)
    # bleed under alpha 0
    solid2 = alpha > 0.5
    _, (iy, ix) = ndimage.distance_transform_edt(~solid2, return_indices=True)
    rgb = np.where((alpha <= 0)[..., None], rgb[iy, ix], rgb)
    return rgb, alpha, int(rim.sum())


def png(rgb, a, path):
    Image.fromarray((np.dstack([rgb, a]) * 255 + 0.5).astype(np.uint8), 'RGBA').save(path, optimize=True)


def on(rgb, a, ground):
    return (rgb * a[..., None] + np.array(ground, np.float32) / 255 * (1 - a[..., None]))


def before_after(name, raw, fixed):
    """1:1. Rows: dark ground, mid ground. Columns: her original (raw), edge-fixed."""
    (r0, a0), (r1, a1) = raw, fixed
    h, w = a0.shape
    tiles = []
    for g in ((24, 22, 30), (128, 124, 120)):
        tiles.append(np.concatenate([on(r0, a0, g), np.ones((h, 8, 3)) * 0.5, on(r1, a1, g)], 1))
    sheet = np.concatenate([tiles[0], np.ones((8, tiles[0].shape[1], 3)) * 0.5, tiles[1]], 0)
    Image.fromarray((np.clip(sheet, 0, 1) * 255 + 0.5).astype(np.uint8)).save(os.path.join(BA, name + '.png'))


def main():
    els = {e['id']: e for e in json.load(open(os.path.join(HERE, 'canva', 'copy_elements.json')))['elements']}
    os.makedirs(OUT, exist_ok=True); os.makedirs(BA, exist_ok=True)
    for f in os.listdir(OUT):
        os.remove(os.path.join(OUT, f))
    cache, manifest, bad = {}, {}, 0
    for name, (src, eid) in PIECES.items():
        if src not in cache: cache[src] = load(src)
        rgba = cache[src]
        el = els[eid]
        top, bleft, bw, _ = el['box']
        s = bw / rgba.shape[1]
        frame = (-bleft / s, -top / s, (-bleft + el['width']) / s, (-top + el['height']) / s)
        mask, cut = select(rgba[..., 3], frame)
        rgb, a, origin, scale = resample(rgba, mask, el, rgba.shape[1])
        frgb, fa, rim = bleed_only(rgb, a) if name in FINISHED - BAD_EXPORT_EDGE else fix_edges(rgb, a)
        path = os.path.join(OUT, name + '.png')
        png(frgb, fa, path)
        before_after(name, (rgb, a), (frgb, fa))
        manifest[name] = {'origin': list(origin), 'size': [a.shape[1] / S, a.shape[0] / S], 'px': [a.shape[1], a.shape[0]],
                          'source': src, 'element': eid, 'native_px_per_unit': round(1 / s, 3),
                          'resample': round(scale, 3), 'frame_cut_her_art': cut, 'finished_untouched': name in FINISHED, 'rim_px_recoloured': rim}
        print(f'{name:16s} from {src:12s} native {1 / s:.2f} px/unit -> x{scale:.2f}  rim {rim:5d}  cut_on_board {cut}')
        n = audit_edges.audit(path, os.path.join(HERE, 'work', 'audit_src'))
        if name not in FINISHED - BAD_EXPORT_EDGE: bad += n
    with open(os.path.join(OUT, 'manifest.json'), 'w') as fh:
        json.dump(manifest, fh, indent=1)
    print('FAIL' if bad else 'ALL CLEAN', len(manifest), 'pieces')
    sys.exit(1 if bad else 0)


if __name__ == '__main__':
    main()
