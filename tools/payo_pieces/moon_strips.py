"""Frame strips of the moon's Play film (`Editor/MapKit/PayoMoonShots.cs`, `film LABEL`).

  python -I tools/payo_pieces/moon_strips.py LABEL [crop]
Reads Logs/payo/unity/moon/frames/LABEL_NNN_TAG.png and writes one strip per beat into Logs/payo/unity/moon/:
LABEL_idle.png, LABEL_hits.png (each hit a row), LABEL_her.png (Mayari forming, game view and wide lens),
LABEL_strip.png (0.25 s apart), LABEL_leave.png. Frames are shown in order with their tag; `crop` (default 1)
scales each tile down.
"""
import os
import re
import sys

from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
FR = os.path.join(ROOT, "Logs", "payo", "unity", "moon", "frames")
OUT = os.path.join(ROOT, "Logs", "payo", "unity", "moon")


def strip(label, name, rows, scale, cols=None):
    tiles = [[Image.open(p).convert("RGB") for p, _ in row] for row in rows if row]
    if not tiles:
        return
    w, h = tiles[0][0].size
    w, h = int(w * scale), int(h * scale)
    cols = cols or max(len(r) for r in tiles)
    out = Image.new("RGB", (w * cols, (h + 18) * len(tiles)), (12, 10, 24))
    d = ImageDraw.Draw(out)
    for r, (row, names) in enumerate(zip(tiles, [x for x in rows if x])):
        for c, (im, (_, tag)) in enumerate(zip(row, names)):
            out.paste(im.resize((w, h)), (c * w, r * (h + 18) + 18))
            d.text((c * w + 4, r * (h + 18) + 3), tag, fill=(255, 255, 255))
    path = os.path.join(OUT, "%s_%s.png" % (label, name))
    out.save(path)
    print("wrote", path)


def main():
    label = sys.argv[1]
    scale = float(sys.argv[2]) if len(sys.argv) > 2 else 0.5
    frames = []
    for f in sorted(os.listdir(FR)):
        m = re.match(re.escape(label) + r"_(\d{3})_(.+)\.png$", f)
        if m:
            frames.append((int(m.group(1)), m.group(2), os.path.join(FR, f)))
    frames.sort()

    def pick(pred):
        return [(p, "%03d %s" % (n, t)) for n, t, p in frames if pred(t)]
    strip(label, "idle", [pick(lambda t: t.startswith("idle"))], scale)
    rows = [pick(lambda t, k=k: t == "hit%d" % k) for k in range(1, 5)]
    rows.append(pick(lambda t: t.startswith("hit1_comet") or t == "hit2_game" or t == "hit5_dizzy"))
    strip(label, "hits", rows, scale)
    strip(label, "her", [pick(lambda t: t == "her_game"), pick(lambda t: t == "her_wide"), pick(lambda t: t == "her_moon")], scale * 0.8)
    strip(label, "strip", [pick(lambda t: t.startswith("her_strip"))], scale)
    strip(label, "leave", [pick(lambda t: t == "leave_wide"), pick(lambda t: t == "leave_moon")], scale)


if __name__ == "__main__":
    main()
