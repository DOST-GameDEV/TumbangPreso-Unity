"""PAYO land kit, step 4: lay shots out as one labelled review sheet (system Python, Pillow).

  py -3 tools/payo_pieces/terrace_sheet.py <out name> <cols> <shot>=<label> [...]

Shots are read from Logs/payo/pieces/terrace/shots (or an absolute or repo-relative path); the sheet is written to
Logs/payo/pieces/terrace/<out name>.png.
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
DIR = ROOT / "Logs/payo/pieces/terrace"


def font(size):
    for f in ("arialbd.ttf", "arial.ttf"):
        try:
            return ImageFont.truetype(f, size)
        except OSError:
            pass
    return ImageFont.load_default()


def main():
    out, cols = sys.argv[1], int(sys.argv[2])
    cw, ch, bar = 1200, 750, 40
    items = []
    for a in sys.argv[3:]:
        shot, _, label = a.partition("=")
        p = Path(shot)
        if not p.suffix:
            p = DIR / "shots" / (shot + ".png")
        elif not p.is_absolute():
            p = ROOT / p
        items.append((p, label or shot))
    rows = (len(items) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * cw, rows * (ch + bar)), (24, 24, 30))
    dr = ImageDraw.Draw(sheet)
    fnt = font(24)
    for i, (p, label) in enumerate(items):
        x, y = (i % cols) * cw, (i // cols) * (ch + bar)
        dr.text((x + 12, y + 7), label, fill=(240, 240, 240), font=fnt)
        try:
            im = Image.open(p).convert("RGB")
        except OSError:
            dr.text((x + 12, y + bar + 12), "missing " + str(p), fill=(255, 120, 120), font=fnt)
            continue
        k = min(cw / im.width, ch / im.height)
        im = im.resize((max(1, int(im.width * k)), max(1, int(im.height * k))), Image.LANCZOS)
        sheet.paste(im, (x + (cw - im.width) // 2, y + bar + (ch - im.height) // 2))
    DIR.mkdir(parents=True, exist_ok=True)
    sheet.save(DIR / (out + ".png"))
    print("sheet", DIR / (out + ".png"), sheet.size)


main()
