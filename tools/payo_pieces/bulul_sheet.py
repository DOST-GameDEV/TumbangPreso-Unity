"""PAYO, THE GUARDIANS: the review sheet and the comparison picture. System Python with Pillow.

  py -3 tools/payo_pieces/bulul_sheet.py <name> <version> [--variant=wood] [--ref=path]

Writes Logs/payo/pieces/bulul/<name>_<version>.png: front three-quarter, back three-quarter, the face, from above,
and a strip with the atlas itself on neutral grey. With --ref also <name>_<version>_compare.png: the reference
beside the front view and the face.
"""
import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
WORK = ROOT / "Logs/payo/pieces/bulul/work"
OUT = ROOT / "Logs/payo/pieces/build/bulul"
SHEETS = ROOT / "Logs/payo/pieces/bulul"
args = [a for a in sys.argv[1:] if not a.startswith("--")]
name, version = args[0], args[1]
variant = next((a.split("=")[1] for a in sys.argv if a.startswith("--variant=")), None)
ref = next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--ref=")), None)
marks = json.load(open(WORK / (name + ".json")))
primary = marks.get("primary", "stone")
variant = variant or primary
tag = "" if variant == primary else "_" + variant
alt = "wood" if primary == "stone" else "stone"
try:
    font = ImageFont.truetype("arial.ttf", 22)
except OSError:
    font = ImageFont.load_default()

T = 760
views = [("front", "front three-quarter, from a person's eye, 1.75 m figure"), ("back", "back three-quarter"),
         ("face", "the face"), ("top", "from above")]
sheet = Image.new("RGB", (T * 2 + T, T * 2 + 44), (24, 26, 32))
d = ImageDraw.Draw(sheet)
sx, sy, sz = marks["size_xyz"]
d.text((10, 10), "%s %s %s   %.2f x %.2f x %.2f m (w h d)   %d triangles   atlas %d   %s KB glb" %
       (name.upper(), version, variant, sx, sy, sz, marks["tris"], marks["atlas"], marks.get("glb_kb", "?")), fill=(235, 235, 235), font=font)
for i, (v, label) in enumerate(views):
    p = WORK / "shots" / ("%s%s_%s.png" % (name, tag, v))
    if p.exists():
        im = Image.open(p).convert("RGB").resize((T, T), Image.LANCZOS)
        sheet.paste(im, ((i % 2) * T, 44 + (i // 2) * T))
    d.text(((i % 2) * T + 10, 44 + (i // 2) * T + 8), label, fill=(20, 20, 20), font=font)
atlas = Image.open(OUT / (name + ("-atlas.png" if variant == primary else "-atlas-%s.png" % variant))).convert("RGB")
neutral = Image.new("RGB", (T, T * 2), (128, 128, 128))
neutral.paste(atlas.resize((T - 40, T - 40), Image.LANCZOS), (20, 60))
ImageDraw.Draw(neutral).text((20, 20), "the atlas on neutral grey", fill=(10, 10, 10), font=font)
other = OUT / (name + ("-atlas-%s.png" % alt if variant == primary else "-atlas.png"))
if other.exists():
    neutral.paste(Image.open(other).convert("RGB").resize((T - 40, T - 40), Image.LANCZOS), (20, T + 40))
    ImageDraw.Draw(neutral).text((20, T + 8), "the other atlas (%s)" % (alt if variant == primary else primary), fill=(10, 10, 10), font=font)
sheet.paste(neutral, (T * 2, 44))
out = SHEETS / ("%s_%s%s.png" % (name, version, tag))
sheet.save(out)
print("SHEET", out)
if ref:
    r = Image.open(ref).convert("RGB")
    h = 900
    r = r.resize((int(r.width * h / r.height), h), Image.LANCZOS)
    a = Image.open(WORK / "shots" / ("%s%s_front.png" % (name, tag))).convert("RGB").resize((h, h), Image.LANCZOS)
    b = Image.open(WORK / "shots" / ("%s%s_face.png" % (name, tag))).convert("RGB").resize((h, h), Image.LANCZOS) if (WORK / "shots" / ("%s%s_face.png" % (name, tag))).exists() else None
    cmp_ = Image.new("RGB", (r.width + h * (2 if b else 1), h), (24, 26, 32))
    cmp_.paste(r, (0, 0)); cmp_.paste(a, (r.width, 0))
    if b:
        cmp_.paste(b, (r.width + h, 0))
    cmp_.save(SHEETS / ("%s_%s%s_compare.png" % (name, version, tag)))
    print("COMPARE", SHEETS / ("%s_%s%s_compare.png" % (name, version, tag)))
