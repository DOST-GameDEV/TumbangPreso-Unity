"""PAYO, the art's review SHEETS (2026-10-12): lays the pictures tools/author_payo_art.py --review N wrote into sheets.

  py -3 tools/author_payo_art_sheets.py --v=N [--refs=<folder with the owner's reference pictures>]

Writes Logs/payo/art/payo_art_sheet_<name>_vN.png, each tile captioned with what it shows:
  eye      1.7 m on the court, four directions          outer    1.7 m on three outer blocks
  close    a block's edge, the pit wall, the forecourt twice, a terrace, the shelf, the catch bird
  air      two from the air, the stage                  night    the night mock
  compare  Payo's player's-eye pictures BESIDE Lagoon Cove in the game and the owner's references 238 and 239
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent.parent
LOGS = ROOT / "Logs/payo/art"
VER = next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--v=")), "1")
REFS = next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--refs=")), "")
SHEETS = {
    "eye": [("eye_north", "1.7 m on the core, NORTH: the wall's notch, cloud, the ranges"), ("eye_east", "1.7 m on the court's east edge, EAST: the water shelf"),
            ("eye_south", "1.7 m from the attackers' side, SOUTH: the gate and its forecourt"), ("eye_west", "1.7 m on the core, WEST: terraces and hamlets")],
    "outer": [("outer_ne", "1.7 m on the north east tower, looking back at the centre"), ("outer_s", "1.7 m on the south block, looking at the gate"),
              ("outer_w", "1.7 m on the west block, looking at the west rim and the terraces")],
    "close": [("close_block", "A block's edge: cap, corbels, relief band, painted course, roots"), ("close_pit", "The pit wall: niche, carved spout, moss, roots"),
              ("close_forecourt", "The gate's forecourt from the rim"), ("close_forecourt_b", "The forecourt's other side: offerings, people on the stair"),
              ("close_terrace", "A terrace wall with paddies, stairs and field things"), ("close_shelf", "The water shelf: planters, carabao, egrets"),
              ("close_bird", "The catch: the hawk spirit over the north blocks")],
    "air": [("air_a", "From the air, north east"), ("air_b", "From the air, north west"), ("stage", "The stage and the pit")],
    "night": [("eye_north_night", "NIGHT MOCK, 1.7 m, north: Mayari over the far ridge"), ("eye_south_night", "NIGHT MOCK, 1.7 m, south: the gate lit"),
              ("close_forecourt_night", "NIGHT MOCK: the forecourt"), ("outer_s_night", "NIGHT MOCK: from the south block"), ("stage_night", "NIGHT MOCK: the stage")],
}


def font(n):
    try:
        return ImageFont.truetype("arialbd.ttf", n)
    except OSError:
        return ImageFont.load_default()


def sheet(name, tiles, W=1280, H=720):
    f = font(22)
    cols = 2
    rows = (len(tiles) + 1) // 2
    out = Image.new("RGB", (cols * W, rows * (H + 44)), (24, 26, 30))
    d = ImageDraw.Draw(out)
    for i, (im, cap) in enumerate(tiles):
        im = im.convert("RGB")
        k = min(W / im.size[0], H / im.size[1])
        im = im.resize((int(im.size[0] * k), int(im.size[1] * k)), Image.LANCZOS)
        x, y = (i % cols) * W, (i // cols) * (H + 44)
        out.paste(im, (x + (W - im.size[0]) // 2, y + 44 + (H - im.size[1]) // 2))
        d.text((x + 14, y + 9), cap, font=f, fill=(236, 236, 228))
    path = LOGS / ("payo_art_sheet_%s_v%s.png" % (name, VER))
    out.save(path)
    print("wrote", path.name)


def main():
    for name, tiles in SHEETS.items():
        have = [(Image.open(LOGS / ("payo_art_%s_v%s.png" % (n, VER))), c) for (n, c) in tiles if (LOGS / ("payo_art_%s_v%s.png" % (n, VER))).exists()]
        if have:
            sheet(name, have)
    comp = []
    for (n, c) in (("eye_south", "PAYO, this pass (a Blender mock), 1.7 m, south"), ("eye_west", "PAYO, this pass, 1.7 m, west")):
        p = LOGS / ("payo_art_%s_v%s.png" % (n, VER))
        if p.exists():
            comp.append((Image.open(p), c))
    lag = ROOT / "Logs/lagoon-cove-unity-v10/lagoon_eye_north.png"
    if lag.exists():
        comp.append((Image.open(lag), "LAGOON COVE in the game, eye height (the bar for finish)"))
    old = ROOT / "Logs/payo/unity/v5/payo_liwasan_day_eye_s.png"
    if old.exists():
        comp.append((Image.open(old), "PAYO's first pass in the game: what the owner rejected"))
    if REFS:
        for (n, c) in (("238.webp", "The owner's reference 238: the cool teal tropical tone"), ("239.webp", "The owner's reference 239: organic, unique building shapes")):
            p = Path(REFS) / n
            if p.exists():
                comp.append((Image.open(p), c))
    if comp:
        sheet("compare", comp)


if __name__ == "__main__":
    main()
