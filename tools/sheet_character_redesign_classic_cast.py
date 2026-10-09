"""The whole Classic cast's action clips on two sheets: a row per person, a column per clip, each at its key beat.
For seeing where two people do the same thing.

    python tools/sheet_character_redesign_classic_cast.py [view]

Reads the stills and keys.json that tools/render_character_redesign_classic_clips.py and each character's pass left
under Logs/character-redesign-classic/clips/<id>/. Writes cast_keys_1.png and cast_keys_2.png there (six people each).
"""
import glob
import json
import os
import sys

from PIL import Image, ImageDraw, ImageFont

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
view = sys.argv[1] if len(sys.argv) > 1 else "q"
CAST = ["bayan", "maring", "totoy", "inday", "kuya_boy", "ate_girlie",
        "tikboy", "bebang", "jun_jun", "lola_pacing", "mang_kanor", "aling_nena"]
KEYS = [("holding-right", "carry", 25), ("holding-right-shoot", "throw", 33), ("pick-up", "pick up", 50),
        ("attack-melee-right", "tag", 60), ("attack-melee-left", "shove", 60), ("interact-right", "reach R", 35),
        ("interact-left", "reach L", 35), ("slide", "slide", 37), ("crouch", "breath", 50),
        ("sit", "sit", 100), ("die", "down", 100), ("emote-yes", "yes", 38), ("emote-no", "no", 30)]
BASE = REPO + "/Logs/character-redesign-classic/clips/"
W, H = 200, 231
try:
    font = ImageFont.truetype("arialbd.ttf", 15)
except OSError:
    font = ImageFont.load_default()
for part in range(2):
    people = CAST[part * 6:part * 6 + 6]
    sheet = Image.new("RGB", (len(KEYS) * W, len(people) * H), (28, 30, 36))
    draw = ImageDraw.Draw(sheet)
    for r, who in enumerate(people):
        own = {}
        if os.path.exists(BASE + who + "/keys.json"):
            own = json.load(open(BASE + who + "/keys.json", encoding="utf-8"))
        for c, (clip, label, key) in enumerate(KEYS):
            key = int(own.get(clip, key))
            have = []
            for path in glob.glob("%s%s/%s_*_%s.png" % (BASE, who, clip, view)):
                pct = os.path.basename(path)[len(clip) + 1:].split("_")[0]
                if pct.isdigit():
                    have.append((abs(int(pct) - key), path))
            if not have:
                continue
            sheet.paste(Image.open(min(have)[1]).convert("RGB").resize((W, H), Image.LANCZOS), (c * W, r * H))
            draw.text((c * W + 5, r * H + 3), (who + ": " if c == 0 else "") + label, fill=(255, 255, 255), font=font,
                      stroke_width=2, stroke_fill=(0, 0, 0))
    out = "%scast_keys_%d%s.png" % (BASE, part + 1, "" if view == "q" else "_" + view)
    sheet.save(out)
    print("WROTE", out)
