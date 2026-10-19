"""Lays out the fire kit's review shots (fire_review.py) into one sheet. System Python.

  python -I tools/payo_pieces/fire_sheet.py <ABS Logs/payo/pieces/fire> <tag>   ->  fire/fire_<tag>.png
"""
import os
import sys

from PIL import Image, ImageDraw

OUT, TAG = sys.argv[1], sys.argv[2]
S = os.path.join(OUT, "shots")
rows = [["fire_pedestal_turn%d" % k for k in range(4)] + ["fire_pedestal_into"],
        ["fire_torch_turn%d" % k for k in range(4)] + ["fire_torch_into"],
        ["pair_12m", "pair_40m"]]
H = 360
pics = [[Image.open(os.path.join(S, "%s_%s.png" % (TAG, n))).convert("RGB") for n in r] for r in rows]
pics = [[p.resize((int(p.width * H / p.height), H)) for p in r] for r in pics]
W = max(sum(p.width for p in r) for r in pics)
sheet = Image.new("RGB", (W, (H + 22) * len(pics)), (24, 24, 30))
d = ImageDraw.Draw(sheet)
for i, (r, names) in enumerate(zip(pics, rows)):
    x = 0
    for p, n in zip(r, names):
        sheet.paste(p, (x, i * (H + 22) + 22))
        d.text((x + 6, i * (H + 22) + 5), n, fill=(230, 230, 230))
        x += p.width
sheet.save(os.path.join(OUT, "fire_%s.png" % TAG))
print("SHEET", os.path.join(OUT, "fire_%s.png" % TAG))
