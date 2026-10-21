"""The review sheet of one set of props (step 4):  py -3 tools/payo_pieces/prop_sheet.py <set letter> <version>

Each prop in its own cell (three-quarter from above, then the opposite low corner), its real size and its triangles;
last, the whole set stood together with a 1.75 m figure. Writes Logs/payo/pieces/prop/set<letter>_<version>.png.
"""
import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Logs/payo/pieces/build/prop"
SHEETS = ROOT / "Logs/payo/pieces/prop"
letter, version = sys.argv[1], sys.argv[2]
meta = json.loads((OUT / "parts" / ("set%s.json" % letter)).read_text())
stats = json.loads((OUT / ("stats-%s.json" % letter)).read_text())
try:
    font = ImageFont.truetype("arialbd.ttf", 17)
except OSError:
    font = ImageFont.load_default()

VW, VH, PAD, HEAD = 500, 438, 6, 26
props = meta["props"]
cols = 2 if len(props) <= 6 else 3
rows = (len(props) + cols - 1) // cols
cw = VW * 2 + PAD
all_im = Image.open(SHEETS / "shots" / ("set%s_all.png" % letter)).convert("RGB")
aw = cols * cw + (cols - 1) * PAD
ah = int(all_im.height * aw / all_im.width)
sheet = Image.new("RGB", (aw, rows * (VH + HEAD + PAD) + HEAD + ah), (24, 26, 32))
d = ImageDraw.Draw(sheet)
for n, p in enumerate(props):
    x, y = (n % cols) * (cw + PAD), (n // cols) * (VH + HEAD + PAD)
    s = stats.get(p["name"], {})
    d.text((x + 6, y + 4), "%s   real %s (drawn x1.15: %s m)   %s triangles%s" % (p["name"].upper(), p["real"], " x ".join("%.2f" % v for v in s.get("size", [])),
                                                                             s.get("triangles", "?"), "   separate: " + ", ".join(p["groups"]) if p["groups"] else ""), fill=(255, 255, 255), font=font)
    for k, view in enumerate(("top", "low")):
        im = Image.open(SHEETS / "shots" / ("%s_%s.png" % (p["name"], view))).convert("RGBA").resize((VW, VH), Image.LANCZOS)
        cell = Image.new("RGBA", (VW, VH), (150, 150, 150, 255))
        cell.alpha_composite(im)
        sheet.paste(cell.convert("RGB"), (x + k * (VW + PAD), y + HEAD))
y = rows * (VH + HEAD + PAD)
d.text((6, y + 4), "SET %s as it stands in the village, a 1.75 m figure for scale" % letter.upper(), fill=(255, 255, 255), font=font)
sheet.paste(all_im.resize((aw, ah), Image.LANCZOS), (0, y + HEAD))
path = SHEETS / ("set%s_%s.png" % (letter, version))
sheet.save(path)
print("wrote", path, sheet.size)
