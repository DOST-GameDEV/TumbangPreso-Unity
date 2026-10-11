"""Tiles the stills of tools/render_character_redesign_classic_clips.py: one row per clip, the front
three-quarter frames and then the side frames.

    python tools/sheet_character_redesign_classic_clips.py <id> [name] [clip ...]

Writes Logs/character-redesign-classic/clips/<id>_<name>.png (name defaults to "actions").
"""
import glob
import os
import sys

from PIL import Image, ImageDraw, ImageFont

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
who = sys.argv[1]
name = sys.argv[2] if len(sys.argv) > 2 else "actions"
only = sys.argv[3:]
src = REPO + "/Logs/character-redesign-classic/clips/" + who
rows = {}
for path in sorted(glob.glob(src + "/*.png")):
    clip, pct, view = os.path.basename(path)[:-4].rsplit("_", 2)
    if only and clip not in only:
        continue
    rows.setdefault(clip, []).append((("q", "side", "back").index(view) if view in ("q", "side", "back") else 3, int(pct), path))
order = [c for c in only if c in rows] or sorted(rows)
W, H = 200, 231
cols = max(len(v) for v in rows.values())
sheet = Image.new("RGB", (cols * W, len(order) * H), (28, 30, 36))
draw = ImageDraw.Draw(sheet)
try:
    font = ImageFont.truetype("arialbd.ttf", 14)
except OSError:
    font = ImageFont.load_default()
for r, clip in enumerate(order):
    for c, (side, pct, path) in enumerate(sorted(rows[clip])):
        sheet.paste(Image.open(path).convert("RGB").resize((W, H), Image.LANCZOS), (c * W, r * H))
        draw.text((c * W + 5, r * H + 3), "%s %d%%" % (clip if c == 0 or pct == 0 else "", pct), fill=(255, 255, 255),
                  font=font, stroke_width=2, stroke_fill=(0, 0, 0))
out = "%s/Logs/character-redesign-classic/clips/%s_%s.png" % (REPO, who, name)
sheet.save(out)
print("WROTE", out)
