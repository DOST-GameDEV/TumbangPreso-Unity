"""One sheet of every hero's first-person arms from `FpvNaturalArmProbe` (Logs/shots-fpv-natural).

    py -3 tools/sheet_fpv_natural_arms.py v1

Writes Logs/shots-fpv-natural/all_empty_<tag>.png and all_holding_<tag>.png: two columns, each cell labelled.
"""
import os
import sys

from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "Logs", "shots-fpv-natural")
TAG = sys.argv[1] if len(sys.argv) > 1 else "v1"
IDS = ["sean", "zack", "dante", "cheska", "nemu", "phaister", "rafi", "amihan", "paete", "bayan"]
W, H = 640, 360

for kind in ("empty", "holding"):
    have = [n for n in IDS if os.path.exists(os.path.join(SRC, "fpv_%s_%s.png" % (n, kind)))]
    sheet = Image.new("RGB", (W * 2, H * ((len(have) + 1) // 2)), (30, 32, 40))
    draw = ImageDraw.Draw(sheet)
    for i, n in enumerate(have):
        im = Image.open(os.path.join(SRC, "fpv_%s_%s.png" % (n, kind))).convert("RGB").resize((W, H), Image.LANCZOS)
        sheet.paste(im, ((i % 2) * W, (i // 2) * H))
        draw.text(((i % 2) * W + 8, (i // 2) * H + 6), n, fill=(255, 255, 255))
    out = os.path.join(SRC, "all_%s_%s.png" % (kind, TAG))
    sheet.save(out)
    print("wrote", out)
