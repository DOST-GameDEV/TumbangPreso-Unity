"""Puts the owner's pieces into the game's sprite slots (out/ + out/manifest.json).

Every slot keeps its name (HubHome, HubChrome and HubQueue are unchanged); what fills it:

  finished, her pixels untouched (extract_originals.py):
    tile_*_face, icon_hero/item/shop/task, queue_plate_face, play_face + play_slab
    (PLAY is her one image, split along her own floor line so the face can press; no pixel changes)
  her Canva SHAPE layers, rasterised from their exact specs and checked against her board render:
    tile_*_slab (linear gradients, left to right), avatar_well, currency_field (flat peach)
  her vector glyphs, lifted off her board render by unmixing from the plate colour:
    glyph_plus, glyph_x, glyph_chevron, the XP stripes (xp_fill), art_window
  unfinished pieces refined (refine_unfinished.py): avatar_frame, name_plate, currency_bar,
    plus_button, menu_button, mode_card (face + floor slab each)
  edge-fixed sheet pieces: xp_track, glyph_level, icon_coin, glyph_menu, cancel_button_face

Icons move from the no-text page to the labelled board by the Canva crop offset between pages.
Kept as they are: spark_* particles. mode_art_* are rebuilt by build_mode_art.py afterwards.
Usage: python -I tools/assemble_board.py
"""
import json
import os
import shutil
import sys

import numpy as np
from PIL import Image
from scipy import ndimage

HERE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(HERE, 'tools'))
import audit_edges  # noqa: E402
from extract_originals import fix_edges, png  # noqa: E402

S, PAD = 2, 6
SRC = os.path.join(HERE, 'out_src')
OUT = os.path.join(HERE, 'out')
BOARD = os.path.join(HERE, 'canva', 'slide68@2x.png')
KEEP = ('spark_dot_peach', 'spark_dot_lime', 'spark_star_honey', 'spark_star_peach')

# icon: move from the no-text page to page 68 (Canva crop rect on 68 minus on 69, board units)
MOVES = {
    'icon_hero': (47.9 - 271.6, 387.0 - 342.3),
    'icon_item': (33.7 - 363.9, 621.8 - 600.2),
    'icon_shop': (14.2 - 486.3, 786.9 - 646.5),
    'icon_task': (316.6 - 756.3, 875.8 - 889.7),
}

# her slab shape layers (Canva element rects, gradient stops left -> right, cornerRounding 45)
SLABS = {
    'tile_hero': ((26.178023471928157, 450.146211000494, 193.34551865059981, 161.26592451605447), 'BA490D', 'EB8B33'),
    'tile_item': ((28.677982082213532, 638.2849419401969, 193.34551865059981, 161.26592451605447), 'B44109', 'EB8B33'),
    'tile_shop': ((25.34470393516638, 830.6980358897733, 253.39885262416064, 229.2698813383835), '325970', '2D8054'),
    'tile_task': ((312.1136151696152, 908.8151004645292, 152.77810222404236, 151.1528167636276), 'A64C13', 'F48D4B'),
}

MANIFEST = {}


def hexc(h):
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], np.float32) / 255


def read(path):
    a = np.asarray(Image.open(path).convert('RGBA')).astype(np.float32) / 255
    return a[..., :3], a[..., 3]


def put(name, rgb, a, origin, note=''):
    path = os.path.join(OUT, name + '.png')
    png(rgb, a, path)
    MANIFEST[name] = {'origin': [round(float(origin[0]), 3), round(float(origin[1]), 3)],
                      'size': [a.shape[1] / S, a.shape[0] / S], 'px': [a.shape[1], a.shape[0]], 'note': note}
    return path


def copy_src(name, slot=None, move=(0, 0), note=''):
    man = json.load(open(os.path.join(SRC, 'manifest.json')))
    rgb, a = read(os.path.join(SRC, name + '.png'))
    o = man[name]['origin']
    return put(slot or name, rgb, a, (o[0] + move[0], o[1] + move[1]), note)


def rrect_cover(h, w, x, y, rw, rh, r):
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32) + 0.5
    cx, cy = x + rw / 2, y + rh / 2
    qx = np.abs(xx - cx) - rw / 2 + r
    qy = np.abs(yy - cy) - rh / 2 + r
    sdf = np.hypot(np.maximum(qx, 0), np.maximum(qy, 0)) + np.minimum(np.maximum(qx, qy), 0) - r
    return np.clip(0.5 - sdf, 0, 1), xx


def canvas_for(rect):
    x, y, w, h = rect
    x0, y0 = np.floor(x * S) / S - PAD / S, np.floor(y * S) / S - PAD / S
    W = int(np.ceil((x + w - x0) * S)) + PAD
    H = int(np.ceil((y + h - y0) * S)) + PAD
    return x0, y0, W, H


def fit_radius(rect, board_a, guess):
    """Her shape's corner radius as Canva draws it: the radius whose coverage best matches her
    board render at the bottom-left and bottom-right corners (never under a face there)."""
    x0, y0, W, H = canvas_for(rect)
    X, Y = int(round(x0 * S)), int(round(y0 * S))
    ref = board_a[Y:Y + H, X:X + W]
    x, y, w, h = rect
    c = int(guess * S * 1.3)
    best = None
    for r in np.arange(guess * 0.75, guess * 1.15, 0.1):
        cov, _ = rrect_cover(H, W, (x - x0) * S, (y - y0) * S, w * S, h * S, r * S)
        err = np.abs(cov[-c - PAD:, :c + PAD] - ref[-c - PAD:, :c + PAD]).mean() + \
            np.abs(cov[-c - PAD:, -c - PAD:] - ref[-c - PAD:, -c - PAD:]).mean()
        if best is None or err < best[0]: best = (err, r)
    return best[1], best[0]


def shape(name, rect, left, right, r, note):
    x0, y0, W, H = canvas_for(rect)
    x, y, w, h = rect
    cov, xx = rrect_cover(H, W, (x - x0) * S, (y - y0) * S, w * S, h * S, r * S)
    t = np.clip((xx - (x - x0) * S) / (w * S), 0, 1)[..., None]
    rgb = left * (1 - t) + right * t
    rgb, _, _ = fix_edges(rgb, cov) if False else (rgb, cov, 0)
    solid = cov > 0.5
    _, (iy, ix) = ndimage.distance_transform_edt(~solid, return_indices=True)
    rgb = np.where((cov <= 0)[..., None], rgb[iy, ix], rgb)
    return put(name, rgb.astype(np.float32), cov.astype(np.float32), (x0, y0), note)


def unmix(name, rect, fg, bg, note, alpha_min=0.0):
    """One of her flat vector glyphs off her board render: alpha from how far each pixel has moved
    from the plate colour toward the glyph colour (exact for her flat plates)."""
    rgba = np.asarray(Image.open(BOARD).convert('RGBA')).astype(np.float32) / 255
    x0, y0, W, H = canvas_for(rect)
    X, Y = int(round(x0 * S)), int(round(y0 * S))
    sub = rgba[Y:Y + H, X:X + W, :3] * rgba[Y:Y + H, X:X + W, 3:] + bg * (1 - rgba[Y:Y + H, X:X + W, 3:])
    d = fg - bg
    a = np.clip(((sub - bg) @ d) / (d @ d), 0, 1)
    a = np.where(a < 0.06, 0, a)
    rgb = np.broadcast_to(fg, sub.shape).copy()
    lab, n = ndimage.label(a > 0.5)
    if n > 1:   # only the glyph (largest piece), no stray knit pixels
        sizes = ndimage.sum(np.ones_like(a), lab, range(1, n + 1))
        keep = ndimage.binary_dilation(lab == (np.argmax(sizes) + 1), iterations=2)
        a = a * keep
    solid = a > 0.5
    _, (iy, ix) = ndimage.distance_transform_edt(~solid, return_indices=True)
    return put(name, rgb.astype(np.float32), a.astype(np.float32), (x0, y0), note)


def xp_fill():
    """Her two XP stripe layers, lifted off the board and repeated by their own period so the bar
    can fill the whole track (Image.Type.Filled). Clipped to her track's shape."""
    rgba = np.asarray(Image.open(BOARD).convert('RGBA')).astype(np.float32) / 255
    man = json.load(open(os.path.join(SRC, 'manifest.json')))
    tr_rgb, tr_a = read(os.path.join(SRC, 'xp_track.png'))
    ox, oy = man['xp_track']['origin']
    X, Y = int(round(ox * S)), int(round(oy * S))
    H, W = tr_a.shape
    sub = rgba[Y:Y + H, X:X + W, :3]
    light, dark, peach = hexc('6FC5B8'), hexc('499C7D'), hexc('FCD39F')
    # her stripes span board x 189.4..356.4; take a clean band from their middle
    s0, s1 = int((215 - ox) * S), int((345 - ox) * S)
    band = sub[:, s0:s1]
    # the stripes are diagonal: the period is where a row best repeats itself
    row = band[H // 2]
    lum = (row - row.mean(0)).sum(-1)
    best = max(range(20, s1 - s0 - 20), key=lambda p: np.dot(lum[:-p], lum[p:]) / (len(lum) - p))
    period = best
    out = np.zeros_like(sub)
    for xcol in range(W):
        k = s0 + ((xcol - s0) % period)
        out[:, xcol] = sub[:, k]
    # pixels that were peach (outside the stripes vertically) take the nearest stripe colour
    isstripe = np.minimum(np.linalg.norm(out - light, axis=-1), np.linalg.norm(out - dark, axis=-1)) < np.linalg.norm(out - peach, axis=-1)
    _, (iy, ix) = ndimage.distance_transform_edt(~isstripe, return_indices=True)
    out = np.where(isstripe[..., None], out, out[iy, ix])
    path = put('xp_fill', out.astype(np.float32), tr_a, (ox, oy), f'her stripes repeated, period {period} px; Filled horizontal')
    return path, period


def art_window():
    rgba = np.asarray(Image.open(BOARD).convert('RGBA')).astype(np.float32) / 255
    rect = (1501, 651.5, 370, 143.5)
    x0, y0, W, H = canvas_for(rect)
    X, Y = int(round(x0 * S)), int(round(y0 * S))
    sub = rgba[Y:Y + H, X:X + W, :3]
    cov, _ = rrect_cover(H, W, (rect[0] - x0) * S, (rect[1] - y0) * S, rect[2] * S, rect[3] * S, 0.01)
    grey = np.median(sub[cov > 0.99], 0)
    return put('art_window', np.broadcast_to(grey, sub.shape).astype(np.float32), cov.astype(np.float32), (x0, y0),
               'her grey window; the mode art covers it')


def main():
    os.makedirs(OUT, exist_ok=True)
    old = json.load(open(os.path.join(OUT, 'manifest.json')))
    for f in os.listdir(OUT):
        if f.endswith('.png') and f[:-4] not in KEEP: os.remove(os.path.join(OUT, f))
    for k in KEEP: MANIFEST[k] = old[k]
    board_a = np.asarray(Image.open(BOARD).convert('RGBA')).astype(np.float32)[..., 3] / 255

    # finished, untouched
    for t in ('hero', 'item', 'shop', 'task'):
        copy_src(f'tile_{t}_face', note='hers; export fringe smoothed only')
        copy_src(f'icon_{t}', move=MOVES[f'icon_{t}'], note='hers, untouched')
    copy_src('queue_plate', 'queue_plate_face', note='hers; export fringe smoothed only; no slab')
    # PLAY: her image split on her floor line (y 1018, 45 degree bevel up from x 1873)
    man = json.load(open(os.path.join(SRC, 'manifest.json')))
    rgb, a = read(os.path.join(SRC, 'play.png'))
    ox, oy = man['play']['origin']
    yy, xx = np.mgrid[0:a.shape[0], 0:a.shape[1]].astype(np.float32) + 0.5
    bx, by = ox + xx / S, oy + yy / S
    sdf = np.maximum(by - 1018, ((bx - 1873) + (by - 1018)) / np.sqrt(2)) * S
    face = a * np.clip(0.5 - sdf, 0, 1)
    put('play_slab', rgb, a, (ox, oy), 'hers; export fringe smoothed only (whole image; the face sits on it)')
    put('play_face', rgb, face.astype(np.float32), (ox, oy), 'hers; export fringe smoothed only (stripes above her floor line)')

    # her shape layers
    radii = {}
    for name, (rect, l, r) in SLABS.items():
        rad, err = fit_radius(rect, board_a, 45)
        radii[name] = (round(float(rad), 2), round(float(err), 4))
        shape(name + '_slab', rect, hexc(l), hexc(r), rad, f'her shape layer, r {rad:.1f}')
    rect = (1521.654573949873, 41.721946271244526, 185.31357767784104, 57.516266898843426)
    shape('currency_field', rect, hexc('FCD39F'), hexc('FCD39F'), 13 * 0.936, 'her shape layer')
    rect = (33.08457237270102, 33.64960499290365, 100, 100)
    shape('avatar_well', rect, hexc('FCD39F'), hexc('FCD39F'), 20 * 0.936, 'her shape layer')

    # her vector glyphs
    unmix('glyph_plus', (1734.593, 57.197, 30.692, 30.692), hexc('0F5A13'), hexc('A8B63E'), 'her glyph')
    unmix('glyph_chevron', (1837.693, 812.656, 33.914, 49.51), hexc('E1D2B7'), hexc('56071D'), 'her glyph')
    unmix('glyph_x', (1062, 31, 40, 39),   # inside her button: the plate's maroon is outside it
          hexc('76151A'), hexc('B43024'), 'her glyph')
    _, period = xp_fill()
    art_window()

    # edge-fixed sheet pieces
    copy_src('xp_track'); copy_src('level_hex', 'glyph_level'); copy_src('icon_coin')
    copy_src('menu_lines', 'glyph_menu'); copy_src('x_button', 'cancel_button_face', note='no slab')

    # refined (face + floor slab)
    ref = json.load(open(os.path.join(SRC, 'refined_manifest.json')))
    for src, slot in (('avatar', 'avatar_frame'), ('name_plate', 'name_plate'), ('currency_bar', 'currency_bar'),
                      ('plus_button', 'plus_button'), ('menu_plate', 'menu_button'), ('mode_card', 'mode_card')):
        o = ref[src]['origin']
        for part in ('face', 'slab'):
            rgb, a = read(os.path.join(SRC, f'{src}_{part}_r.png'))
            put(f'{slot}_{part}', rgb, a, o, f'refined; floor {ref[src]["depth"]} units')
        MANIFEST[f'{slot}_face']['depth'] = ref[src]['depth']

    with open(os.path.join(OUT, 'manifest.json'), 'w') as fh:
        json.dump(MANIFEST, fh, indent=1)
    print('slab radii (fit, err):', radii, ' xp period px:', period)
    bad = 0
    for name in MANIFEST:
        n = audit_edges.audit(os.path.join(OUT, name + '.png'), os.path.join(HERE, 'work', 'audit_out'))
        if 'untouched' not in MANIFEST[name]['note']: bad += n
    print('FAIL' if bad else 'ALL CLEAN', len(MANIFEST), 'sprites')
    sys.exit(1 if bad else 0)


if __name__ == '__main__':
    main()
