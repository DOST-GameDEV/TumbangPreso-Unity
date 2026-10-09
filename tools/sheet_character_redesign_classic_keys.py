"""One large still per action clip, at its key beat, for showing a Classic character's thirteen clips at a glance.

    python tools/sheet_character_redesign_classic_keys.py <id> [view]

Picks, from the stills tools/render_character_redesign_classic_clips.py already wrote, the one nearest each clip's
key beat. Writes Logs/character-redesign-classic/clips/<id>_keys.png (or <id>_keys_<view>.png).
"""
import glob
import os
import sys

from PIL import Image, ImageDraw, ImageFont

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
who = sys.argv[1]
view = sys.argv[2] if len(sys.argv) > 2 else "q"
# (clip, what it is, how far through its key beat is)
KEYS = [("holding-right", "carry", 25), ("holding-right-shoot", "throw", 33), ("pick-up", "pick up", 50),
        ("attack-melee-right", "tag", 60), ("attack-melee-left", "shove", 60), ("interact-right", "reach right", 35),
        ("interact-left", "reach left", 35), ("slide", "slide", 37), ("crouch", "out of breath", 50),
        ("sit", "sit", 100), ("die", "knocked down", 100), ("emote-yes", "yes", 38), ("emote-no", "no", 30)]
src = REPO + "/Logs/character-redesign-classic/clips/" + who
# A character's own key beats, where its clips peak somewhere else: {"emote-yes": 45, ...} in <id>/keys.json
if os.path.exists(src + "/keys.json"):
    import json
    own = json.load(open(src + "/keys.json", encoding="utf-8"))
    KEYS = [(clip, label, int(own.get(clip, key))) for clip, label, key in KEYS]
W, H, COLS = 380, 438, 7
rows = (len(KEYS) + COLS - 1) // COLS
sheet = Image.new("RGB", (COLS * W, rows * H), (28, 30, 36))
draw = ImageDraw.Draw(sheet)
try:
    font = ImageFont.truetype("arialbd.ttf", 22)
except OSError:
    font = ImageFont.load_default()
for i, (clip, label, key) in enumerate(KEYS):
    have = []
    for path in glob.glob("%s/%s_*_%s.png" % (src, clip, view)):
        pct = os.path.basename(path)[len(clip) + 1:].split("_")[0]
        if pct.isdigit():
            have.append((abs(int(pct) - key), path))
    if not have:
        continue
    x, y = (i % COLS) * W, (i // COLS) * H
    sheet.paste(Image.open(min(have)[1]).convert("RGB").resize((W, H), Image.LANCZOS), (x, y))
    draw.text((x + 10, y + 8), label, fill=(255, 255, 255), font=font, stroke_width=3, stroke_fill=(0, 0, 0))
out = "%s/Logs/character-redesign-classic/clips/%s_keys%s.png" % (REPO, who, "" if view == "q" else "_" + view)
sheet.save(out)
print("WROTE", out)
