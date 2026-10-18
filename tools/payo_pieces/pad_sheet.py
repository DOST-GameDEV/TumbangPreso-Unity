"""Lays the meso pads' review shots (tools/payo_pieces/pad_review.py) into one sheet.

  py -I tools/payo_pieces/pad_sheet.py <shots dir> <out.png> [note] [prop ...]

Rows: per prop, the player's eye at 5 m, the three-quarter top view, 10 m at eye height. Columns: NEW, CURRENT Payo,
ARENA original.
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


def font(n, bold=True):
    try:
        return ImageFont.truetype("arialbd.ttf" if bold else "arial.ttf", n)
    except OSError:
        return ImageFont.load_default()


shots, out = Path(sys.argv[1]), Path(sys.argv[2])
note = sys.argv[3] if len(sys.argv) > 3 else ""
ONLY = sys.argv[4:]
PROPS = [("pad_geyser", "GEYSER BOWL (jump pad)"), ("pad_speed", "SPEED PAD (travel away from the eye camera)"), ("pickup_plinth", "PICKUP PLINTH")]
COLS = [("new", "NEW meso stone"), ("payo", "CURRENT Payo"), ("arena", "ARENA original")]
PROPS = [p for p in PROPS if not ONLY or p[0] in ONLY]
VIEWS = [("eye5", "eye 1.7 m, 5 m away"), ("top34", "three-quarter top"), ("far10", "10 m, eye height")]
tw, th = 600, 392
lab, head, gap = 24, 56, 6
H = head + len(PROPS) * (34 + len(VIEWS) * (th + lab + gap))
W = len(COLS) * (tw + gap) + gap
sheet = Image.new("RGB", (W, H), (20, 22, 30))
d = ImageDraw.Draw(sheet)
d.text((12, 10), "PAYO PADS remodelled as carved temple stone", fill=(240, 236, 226), font=font(26))
d.text((12, 38), "warm low sunset key, cool fill, stone floor, 1.75 m figure. Geyser: blue dome = stand-in for PayoGeyser's resting pool (r 0.62, y -0.02). " + note,
       fill=(190, 190, 200), font=font(13, False))
y = head
for name, title in PROPS:
    d.text((12, y + 6), title, fill=(255, 214, 140), font=font(20))
    y += 34
    for v, vt in VIEWS:
        for i, (c, ct) in enumerate(COLS):
            x = gap + i * (tw + gap)
            d.text((x + 4, y + 4), "%s  |  %s" % (ct, vt), fill=(230, 230, 236), font=font(14))
            p = shots / ("%s_%s_%s.png" % (name, c, v))
            if p.exists():
                sheet.paste(Image.open(p).convert("RGB").resize((tw, th), Image.LANCZOS), (x, y + lab))
        y += th + lab + gap
sheet.save(out)
print("sheet", out, sheet.size)
