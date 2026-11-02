"""Contact sheet of every exported artboard PNG, grouped by document, with titles.

    py -3 tools/ui_artboards/contact_sheet.py --out ArtSource/ui/revamp-2026-10-29
"""
import argparse
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
THUMB_W, THUMB_H, COLS, PAD, LABEL = 480, 270, 5, 24, 34
INK, PAPER, HEAD = (40, 22, 12), (252, 211, 159), (152, 7, 21)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", required=True)
    args = parser.parse_args()
    out = (ROOT / args.out).resolve()
    job = json.loads((out / "job.json").read_text(encoding="utf-8"))
    font_path = ROOT / "Assets/TumbangPreso/Resources/UI/fonts/Nunito-Bold.ttf"
    head_path = ROOT / "Assets/TumbangPreso/Resources/UI/fonts/DarumadropOne-Regular.ttf"
    font = ImageFont.truetype(str(font_path), 20)
    head = ImageFont.truetype(str(head_path), 44)

    rows = []
    for doc in job["documents"]:
        rows.append(("head", doc["name"] + "  (" + Path(doc["psd"]).name + ")"))
        boards = doc["boards"]
        for i in range(0, len(boards), COLS):
            rows.append(("boards", [(doc["name"], b) for b in boards[i:i + COLS]]))
    height = PAD
    for kind, _ in rows:
        height += 70 if kind == "head" else THUMB_H + LABEL + PAD
    sheet = Image.new("RGB", (PAD + COLS * (THUMB_W + PAD), height), PAPER)
    draw = ImageDraw.Draw(sheet)
    y = PAD
    for kind, content in rows:
        if kind == "head":
            draw.text((PAD, y), content, font=head, fill=HEAD)
            y += 70
            continue
        for c, (doc_name, board) in enumerate(content):
            x = PAD + c * (THUMB_W + PAD)
            png = out / "png" / ("%s__%s.png" % (doc_name, board["id"]))
            if png.exists():
                with Image.open(png) as image:
                    thumb = image.convert("RGBA").resize((THUMB_W, THUMB_H), Image.LANCZOS)
                    sheet.paste(thumb, (x, y), thumb)
            else:
                draw.rectangle((x, y, x + THUMB_W, y + THUMB_H), outline=HEAD, width=3)
                draw.text((x + 12, y + 12), "missing export", font=font, fill=HEAD)
            draw.text((x, y + THUMB_H + 6), board["title"], font=font, fill=INK)
        y += THUMB_H + LABEL + PAD
    target = out / "contact_sheet.png"
    sheet.save(target)
    print(target, sheet.size)


if __name__ == "__main__":
    main()
