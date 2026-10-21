"""The props beside what they must belong with:  py -3 tools/payo_pieces/prop_compare.py <reference 238> [tag]

Top: the owner's reference 238 and this session's slippers (Logs/slippers/slippers_v6.png, cropped). Below: the four sets
as they stand. Writes Logs/payo/pieces/prop/compare_<tag>.png.
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
SHEETS = ROOT / "Logs/payo/pieces/prop"
ref = Image.open(sys.argv[1]).convert("RGB")
tag = sys.argv[2] if len(sys.argv) > 2 else "v1"
slip = Image.open(ROOT / "Logs/slippers/slippers_v6.png").convert("RGB").crop((0, 0, 2020, 1000))
try:
    font = ImageFont.truetype("arialbd.ttf", 20)
except OSError:
    font = ImageFont.load_default()
W, H = 1200, 675


def fit(im):
    k = max(W / im.width, H / im.height)
    im = im.resize((int(im.width * k + 1), int(im.height * k + 1)), Image.LANCZOS)
    x, y = (im.width - W) // 2, (im.height - H) // 2
    return im.crop((x, y, x + W, y + H))


cells = [("the owner's reference 238 (its cool tone is LIGHT; stone is grey)", fit(ref)), ("this session's slippers, v6", fit(slip))]
for letter in "abcd":
    p = SHEETS / "shots" / ("set%s_all.png" % letter)
    if p.exists():
        cells.append(("PAYO props, set %s" % letter.upper(), fit(Image.open(p).convert("RGB"))))
out = Image.new("RGB", (W * 2 + 8, ((len(cells) + 1) // 2) * (H + 34)), (24, 26, 32))
d = ImageDraw.Draw(out)
for n, (label, im) in enumerate(cells):
    x, y = (n % 2) * (W + 8), (n // 2) * (H + 34)
    d.text((x + 6, y + 5), label, fill=(255, 255, 255), font=font)
    out.paste(im, (x, y + 30))
path = SHEETS / ("compare_%s.png" % tag)
out.save(path)
print("wrote", path)
