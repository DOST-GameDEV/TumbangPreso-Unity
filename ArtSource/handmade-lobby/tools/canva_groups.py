"""Flattens each HOME sticker (slab, face, inlays, glyphs, picture) into one PNG for the Canva page.

The owner gets one movable image per sticker on a new page of her board, each at its exact board
position. Lettering stays hers to set in Canva (the game sets it live). Writes out/canva/*.png and
out/canva/placements.json (left, top, width, height in page units).
"""
import json
import os

from PIL import Image

HERE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(HERE, 'out')
DEST = os.path.join(OUT, 'canva')
S = 2
M = json.load(open(os.path.join(OUT, 'manifest.json'), encoding='utf-8'))

GROUPS = {
    'avatar': ['avatar_frame_slab', 'avatar_frame_face', 'avatar_well'],
    'name_plate': ['name_plate_slab', 'name_plate_face', 'xp_track', 'xp_fill', 'glyph_level'],
    'currency': ['currency_bar_slab', 'currency_bar_face', 'currency_field', 'plus_button_slab', 'plus_button_face', 'glyph_plus', 'icon_coin'],
    'menu': ['menu_button_slab', 'menu_button_face', 'glyph_menu'],
    'queue': ['queue_plate_slab', 'queue_plate_face', 'cancel_button_slab', 'cancel_button_face', 'glyph_x'],
    'hero': ['tile_hero_slab', 'tile_hero_face', 'icon_hero'],
    'item': ['tile_item_slab', 'tile_item_face', 'icon_item'],
    'shop': ['tile_shop_slab', 'tile_shop_face', 'icon_shop'],
    'task': ['tile_task_slab', 'tile_task_face', 'icon_task'],
    'mode_card': ['mode_card_slab', 'mode_card_face', 'art_window', 'mode_art_classic', 'glyph_chevron'],
    'play': ['play_slab', 'play_face'],
    'mode_art_ranked': ['mode_art_ranked'], 'mode_art_classic': ['mode_art_classic'], 'mode_art_casual': ['mode_art_casual'],
}


def main():
    os.makedirs(DEST, exist_ok=True)
    places = {}
    for group, parts in GROUPS.items():
        parts = [p for p in parts if p in M]
        if not parts: continue
        x0 = min(M[p]['origin'][0] for p in parts); y0 = min(M[p]['origin'][1] for p in parts)
        x1 = max(M[p]['origin'][0] + M[p]['size'][0] for p in parts); y1 = max(M[p]['origin'][1] + M[p]['size'][1] for p in parts)
        cv = Image.new('RGBA', (round((x1 - x0) * S), round((y1 - y0) * S)), (0, 0, 0, 0))
        for p in parts:
            im = Image.open(os.path.join(OUT, p + '.png')).convert('RGBA')
            if p == 'xp_fill': im = im.crop((0, 0, int(im.width * 0.68), im.height))
            cv.alpha_composite(im, (round((M[p]['origin'][0] - x0) * S), round((M[p]['origin'][1] - y0) * S)))
        # the queue plate's top runs off the page; clip the canvas at the page edge
        if y0 < 0:
            cv = cv.crop((0, round(-y0 * S), cv.width, cv.height)); y0 = 0
        cv.save(os.path.join(DEST, group + '.png'), optimize=True)
        places[group] = [round(x0, 2), round(y0, 2), round(cv.width / S, 2), round(cv.height / S, 2)]
    json.dump(places, open(os.path.join(DEST, 'placements.json'), 'w'), indent=1)
    print(json.dumps(places))


if __name__ == '__main__':
    main()
