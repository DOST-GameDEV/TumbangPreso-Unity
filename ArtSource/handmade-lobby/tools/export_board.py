"""Ships the hand-made HOME into the game: sprites and board.json under Resources/UI/handmade-lobby.

board.json carries every sprite's board origin and size (from out/manifest.json) and every
label's Canva text layer plus its measured BASELINE (the same model compose_preview.py checks
against the owner's export to within about a unit). HubBoard reads it; no coordinate is typed
twice. Usage: python -I tools/export_board.py   (after build_lobby_art.py and build_mode_art.py)
"""
import json
import os
import shutil
import sys

from PIL import ImageFont

HERE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(HERE, 'tools'))
import compose_preview as C  # noqa: E402

REPO = os.path.dirname(os.path.dirname(HERE))
DEST = os.path.join(REPO, 'Assets', 'TumbangPreso', 'Resources', 'UI', 'handmade-lobby')


def baseline(t):
    size = t['size'] * C.S
    f = ImageFont.truetype(C.FACE[t['font']], round(size))
    asc, desc = f.getmetrics()
    y = t['box'][1] * C.S
    b = y + (t['lineHeight'] * size - (asc + desc)) / 2 + asc
    b -= {'display': 0.115, 'digits': 0.12, 'heading': 0.32, 'tag': -0.38}[t['font']] * size
    return round(b / C.S, 2)


def main():
    manifest = json.load(open(os.path.join(C.OUT, 'manifest.json'), encoding='utf-8'))
    os.makedirs(DEST, exist_ok=True)
    keep = set()
    sprites = []
    for name, m in sorted(manifest.items()):
        src = os.path.join(C.OUT, name + '.png')
        shutil.copyfile(src, os.path.join(DEST, name + '.png'))
        keep.add(name + '.png')
        sprites.append({'name': name, 'x': m['origin'][0], 'y': m['origin'][1], 'w': m['size'][0], 'h': m['size'][1]})
    labels = []
    for t in C.TEXT:
        x, y, w, h = t['box']
        labels.append({'key': t['text'], 'text': t['text'], 'x': x, 'y': y, 'w': w, 'h': h, 'size': t['size'],
                       'colour': t['colour'], 'align': t['align'], 'font': t['font'],
                       'upper': t['text'] in C.UPPER, 'baseline': baseline(t)})
    with open(os.path.join(DEST, 'board.json'), 'w', encoding='utf-8') as fh:
        json.dump({'sprites': sprites, 'labels': labels}, fh, indent=1)
    keep.add('board.json')
    # prune superseded pieces (and their metas) so Resources never ships a stale sprite
    for f in os.listdir(DEST):
        base = f[:-5] if f.endswith('.meta') else f
        if base not in keep:
            os.remove(os.path.join(DEST, f))
    print('exported', len(sprites), 'sprites and', len(labels), 'labels to', DEST)


if __name__ == '__main__':
    main()
