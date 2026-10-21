"""Joins the frames of tools/render_character_redesign_classic_lineup.py into one sheet.

    py -3 tools/sheet_character_redesign_classic_lineup.py v01

Writes Logs/character-redesign-classic/<version>_lineup.png: the six men and the six women, front and back,
each figure named.
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
V = sys.argv[1] if len(sys.argv) > 1 else "v01"
BASE = os.path.join(ROOT, "Logs", "character-redesign-classic")
ROWS = (("men", ("Bayan", "Kuya Boy", "Mang Kanor", "Totoy", "Tikboy", "Jun-Jun")),
        ("women", ("Bebang", "Maring", "Inday", "Ate Girlie", "Aling Nena", "Lola Pacing")))
try:
    FONT = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 26)
except OSError:
    FONT = ImageFont.load_default()

tiles = []
for row, names in ROWS:
    for view in ("front", "back"):
        im = Image.open(os.path.join(BASE, V, "lineup_%s_%s.png" % (row, view))).convert("RGB")
        d = ImageDraw.Draw(im)
        order = names if view == "front" else names
        w = im.width
        for i, name in enumerate(order):
            # the figures stand 0.74 apart about the middle; the back view turns each on the spot
            x = w * 0.5 + (i - 2.5) * w * 0.146
            d.text((x, im.height - 44), name, font=FONT, fill=(255, 255, 255), anchor="ms",
                   stroke_width=3, stroke_fill=(20, 20, 26))
        d.text((14, 10), "%s, %s" % (row, view), font=FONT, fill=(255, 255, 255), stroke_width=3, stroke_fill=(20, 20, 26))
        tiles.append(im)
sheet = Image.new("RGB", (tiles[0].width, sum(t.height for t in tiles)), (30, 32, 40))
y = 0
for t in tiles:
    sheet.paste(t, (0, y)); y += t.height
out = os.path.join(BASE, "%s_lineup.png" % V)
sheet.save(out)
print("wrote", out, sheet.size)
