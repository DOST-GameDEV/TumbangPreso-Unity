"""PAYO, THE GUARDIANS: several pieces' shots side by side, for a quick look between rounds.

  py -3 tools/payo_pieces/bulul_montage.py <out name> <piece name> [<piece name> ...] [--views=front,face,back] [--size=440]

Writes Logs/payo/pieces/bulul/work/<out name>.png: one row per piece.
"""
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SHOTS = ROOT / "Logs/payo/pieces/bulul/work/shots"
args = [a for a in sys.argv[1:] if not a.startswith("--")]
views = next((a.split("=")[1] for a in sys.argv if a.startswith("--views=")), "front,face,back").split(",")
T = int(next((a.split("=")[1] for a in sys.argv if a.startswith("--size=")), 440))
out, names = args[0], args[1:]
img = Image.new("RGB", (T * len(views), T * len(names)), (24, 26, 32))
for r, n in enumerate(names):
    for c, v in enumerate(views):
        p = SHOTS / ("%s_%s.png" % (n, v))
        if p.exists():
            img.paste(Image.open(p).convert("RGB").resize((T, T), Image.LANCZOS), (c * T, r * T))
img.save(ROOT / "Logs/payo/pieces/bulul/work" / (out + ".png"))
print("MONTAGE", out)
