"""Composes HOME from the rebuilt sprites and the board's own text layers, and checks it against the board.

Sprites go where manifest.json says (each records its board origin). Text uses the Canva text
layers verbatim (canva/page68_text.json: box, size, colour, alignment, face) and Canva's line
box model: the line is 1.4 x size tall and the glyphs sit centred in it by the font's own ascent
and descent (CSS half-leading), so a label lands where the board has it.

Usage:
  python -I tools/compose_preview.py check            diff against canva/labelled68@2x.png
  python -I tools/compose_preview.py preview [mode]   over the home loop poster (ranked|casual|classic)
"""
import json
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REPO = os.path.dirname(os.path.dirname(HERE))
OUT = os.path.join(HERE, 'out')
S = 2
FONTS = os.path.join(REPO, 'Assets', 'TumbangPreso', 'Resources', 'UI', 'fonts')
FACE = {
    'display': os.path.join(FONTS, 'DarumadropOne-Regular.ttf'),
    'digits': os.path.join(FONTS, 'PaalalabasDisplayWide.otf'),
    'tag': os.path.join(FONTS, 'Lydian-Regular.ttf'),
    'heading': os.path.join(FONTS, 'DINNextLTArabic-Bold.ttf'),
}
MANIFEST = json.load(open(os.path.join(OUT, 'manifest.json'), encoding='utf-8'))
TEXT = json.load(open(os.path.join(HERE, 'canva', 'page68_text.json'), encoding='utf-8'))

# Board draw order, back to front (slabs before faces, inlays and glyphs after their plate).
ORDER = [
    'tile_hero_slab', 'tile_hero_face', 'tile_item_slab', 'tile_item_face', 'tile_shop_slab', 'tile_shop_face',
    'tile_task_slab', 'tile_task_face', 'icon_hero', 'icon_item', 'icon_shop', 'icon_task',
    'avatar_frame_slab', 'avatar_frame_face', 'avatar_well', 'name_plate_slab', 'name_plate_face',
    'xp_track', 'xp_fill', 'glyph_level',
    'currency_bar_slab', 'currency_bar_face', 'currency_field', 'plus_button_slab', 'plus_button_face', 'glyph_plus',
    'icon_coin', 'menu_button_slab', 'menu_button_face', 'glyph_menu',
    'queue_plate_slab', 'queue_plate_face', 'cancel_button_slab', 'cancel_button_face', 'glyph_x',
    'mode_card_slab', 'mode_card_face', 'art_window', 'MODE_ART', 'glyph_chevron', 'play_slab', 'play_face',
]

# Lettering the board sets in capitals (Canva's uppercase style is not in the text layer data).
UPPER = {'play', 'shop', 'item', 'hero', 'classic', 'task', 'xp'}


def put(cv, name, crop_fill=None):
    m = MANIFEST[name]
    im = Image.open(os.path.join(OUT, name + '.png')).convert('RGBA')
    if crop_fill is not None:
        # the xp fill is drawn to a fraction, as Image.Type Filled does in the game
        w = int(round(im.width * crop_fill)); im = im.crop((0, 0, w, im.height))
    cv.alpha_composite(im, (round(m['origin'][0] * S), round(m['origin'][1] * S)))


def text(cv, t, override=None):
    s = override if override is not None else (t['text'].upper() if t['text'] in UPPER else t['text'])
    size = t['size'] * S
    f = ImageFont.truetype(FACE[t['font']], round(size))
    asc, desc = f.getmetrics()
    x, y, w, h = [v * S for v in t['box']]
    line = t['lineHeight'] * size
    baseline = y + (line - (asc + desc)) / 2 + asc
    # Canva centres each face by its own vertical metrics, not the hhea ones PIL reads; measured
    # against the board export (tools/compose_preview.py check), as a fraction of the size.
    baseline -= {'display': 0.115, 'digits': 0.12, 'heading': 0.32, 'tag': -0.38}[t['font']] * size
    d = ImageDraw.Draw(cv)
    if t['align'] == 'center':
        d.text((x + w / 2, baseline), s, font=f, fill=t['colour'], anchor='ms')
    else:
        d.text((x, baseline), s, font=f, fill=t['colour'], anchor='ls')


def compose(bg, mode='classic', xp=0.68, labels=None):
    cv = bg.copy()
    for name in ORDER:
        if name == 'MODE_ART':
            art = 'mode_art_' + mode
            if art in MANIFEST: put(cv, art)
            continue
        put(cv, name, xp if name == 'xp_fill' else None)
    labels = labels or {}
    for t in TEXT:
        text(cv, t, labels.get(t['text']))
    return cv


if __name__ == '__main__':
    what = sys.argv[1] if len(sys.argv) > 1 else 'check'
    if what == 'check':
        board = Image.open(os.path.join(HERE, 'canva', 'labelled68@2x.png')).convert('RGBA')
        ground = Image.new('RGBA', board.size, (70, 90, 110, 255))
        ref = ground.copy(); ref.alpha_composite(board)
        ours = compose(ground, 'classic', xp=(356 - 190) / 245)
        ref.convert('RGB').save(os.path.join(HERE, 'work', 'check_board.png'))
        ours.convert('RGB').save(os.path.join(HERE, 'work', 'check_ours.png'))
        # flicker pair as one side-by-side plus an edge overlay: board edges red, ours cyan
        a = np.asarray(ref.convert('L')).astype(np.float32); b = np.asarray(ours.convert('L')).astype(np.float32)
        def edges(g): return np.hypot(*np.gradient(g)) > 18
        ov = np.zeros(a.shape + (3,), np.uint8); ov[..., :] = 30
        ov[edges(a)] = (255, 60, 60); ov[edges(b)] = (60, 220, 255); ov[edges(a) & edges(b)] = (255, 255, 255)
        Image.fromarray(ov).save(os.path.join(HERE, 'work', 'check_edges.png'))
        print('wrote work/check_board.png, check_ours.png, check_edges.png (white = both agree)')
    else:
        mode = sys.argv[2] if len(sys.argv) > 2 else 'classic'
        poster = os.path.join(REPO, 'Assets', 'TumbangPreso', 'Resources', 'UI', 'home', 'zack-home-poster.png')
        bg = Image.open(sys.argv[3] if len(sys.argv) > 3 else poster).convert('RGBA').resize((1920 * S, 1080 * S), Image.LANCZOS)
        out = os.path.join(HERE, 'work', f'lobby_{mode}.png')
        compose(bg, mode).convert('RGB').save(out)
        print(out)
