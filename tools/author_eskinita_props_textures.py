"""Painted artwork for the Eskinita Alley prop kit, and the contact-sheet composer.

  py -3 tools/author_eskinita_props_textures.py              paint the prop_* textures
  py -3 tools/author_eskinita_props_textures.py --compose <tiles.json> <out.png>

Blender's python has no PIL, so tools/author_eskinita_props.py calls this script for both jobs.

Writes into Assets/TumbangPreso/Art/EskinitaAlley/Textures/:
  prop_sign_sarisari_albedo.png   the store's hand-named sign board (4 : 1)
  prop_sign_arch_albedo.png       the barangay arch board (8 : 1)
  prop_decals_albedo.png          one atlas of small paintings; regions in DECALS below

THE STYLE: flat fills, wobbly hand-cut outlines, hand-lettered words with every letter nudged,
one or two LARGE feathered wear patches at low contrast. No noise, grain or streaks. Nothing
near the role hues (offence orange f87020, defence blue 0080e8). Shop and place names are
original: a person plus the trade.
"""
import json
import math
import random
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[1]
TEX = ROOT / "Assets" / "TumbangPreso" / "Art" / "EskinitaAlley" / "Textures"
FONTS = Path("C:/Windows/Fonts")

CREAM = (244, 232, 200)
RED = (196, 52, 44)
DEEP_RED = (150, 36, 36)
GREEN = (38, 120, 92)
DEEP_GREEN = (26, 84, 70)
YELLOW = (242, 196, 54)
PINK = (214, 76, 128)
INK = (52, 44, 44)
WHITE = (250, 247, 238)
TAN = (214, 180, 128)
LEAF = (92, 160, 66)

# Atlas regions in pixels (x0, y0, x1, y1) of the 1024 x 1024 decal sheet, y down.
# tools/author_eskinita_props.py carries the same table as UV rectangles.
DECALS = {
    "backboard": (0, 0, 512, 384),
    "brgy": (512, 0, 1024, 384),
    "dama": (0, 384, 384, 768),
    "sachets": (384, 384, 512, 768),
    "poster": (512, 384, 768, 768),
    "meter": (768, 384, 1024, 640),
    "plate": (768, 640, 1024, 768),
    "crate": (0, 768, 384, 896),
    "tag": (0, 896, 384, 1024),
    "sack": (384, 768, 768, 1024),
    "wash": (768, 768, 1024, 1024),
}


def font(name, size):
    for n in (name, "arialbd.ttf"):
        try:
            return ImageFont.truetype(str(FONTS / n), size)
        except OSError:
            continue
    return ImageFont.load_default()


def wobble_box(rng, x0, y0, x1, y1, amp, step=60):
    """The outline of a rectangle cut by hand: points along each edge pushed in and out."""
    pts = []
    def run(ax, ay, bx, by):
        n = max(2, int(math.hypot(bx - ax, by - ay) / step))
        for i in range(n):
            t = i / n
            pts.append((ax + (bx - ax) * t + rng.uniform(-amp, amp), ay + (by - ay) * t + rng.uniform(-amp, amp)))
    run(x0, y0, x1, y0)
    run(x1, y0, x1, y1)
    run(x1, y1, x0, y1)
    run(x0, y1, x0, y0)
    return pts


def patches(img, rng, colour, count, size, alpha=38, inside=None):
    """A few LARGE soft patches (sun fade, old repaint): the only wear a flat texture gets.
    `inside` is a polygon the patches are clipped to, so a painted border stays clean."""
    w, h = img.size
    layer = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(layer)
    for _ in range(count):
        cx, cy = rng.uniform(0, w), rng.uniform(0, h)
        rx, ry = size * rng.uniform(0.7, 1.4), size * rng.uniform(0.4, 0.8)
        d.ellipse((cx - rx, cy - ry, cx + rx, cy + ry), fill=alpha)
    layer = layer.filter(ImageFilter.GaussianBlur(size * 0.22))
    if inside is not None:
        clip = Image.new("L", (w, h), 0)
        ImageDraw.Draw(clip).polygon(inside, fill=255)
        layer = Image.composite(layer, Image.new("L", (w, h), 0), clip)
    img.paste(Image.new("RGB", (w, h), colour), (0, 0), layer)


def letters(img, rng, text, fnt, cx, cy, fill, spacing=0.0, jitter=2.5, tilt=2.5, shadow=None):
    """Hand lettering: each letter drawn on its own, nudged and tilted a little."""
    d = ImageDraw.Draw(img)
    widths = [d.textlength(ch, font=fnt) for ch in text]
    total = sum(widths) + spacing * (len(text) - 1)
    x = cx - total / 2
    asc, desc = fnt.getmetrics()
    for ch, wd in zip(text, widths):
        if ch != " ":
            pad = 12
            tile = Image.new("RGBA", (int(wd) + pad * 2, asc + desc + pad * 2), (0, 0, 0, 0))
            td = ImageDraw.Draw(tile)
            if shadow:
                td.text((pad + 4, pad + 5), ch, font=fnt, fill=shadow)
            td.text((pad, pad), ch, font=fnt, fill=fill)
            tile = tile.rotate(rng.uniform(-tilt, tilt), resample=Image.BICUBIC, expand=False)
            img.paste(tile, (int(x - pad + rng.uniform(-jitter, jitter)),
                             int(cy - (asc + desc) / 2 - pad + rng.uniform(-jitter, jitter))), tile)
        x += wd + spacing


def sampaguita(d, cx, cy, r, rng):
    """A small painted sampaguita: two leaves, five or six round white petals, a yellow eye."""
    for a in (rng.uniform(2.4, 3.0), rng.uniform(0.2, 0.8)):
        lx, ly = cx + math.cos(a) * r * 1.5, cy + math.sin(a) * r * 1.2
        d.ellipse((lx - r * 0.95, ly - r * 0.5, lx + r * 0.95, ly + r * 0.5), fill=LEAF)
    n = rng.choice((5, 6))
    off = rng.uniform(0, 1)
    for i in range(n):
        a = off + i * math.tau / n
        px, py = cx + math.cos(a) * r * 0.62, cy + math.sin(a) * r * 0.62
        d.ellipse((px - r * 0.52, py - r * 0.52, px + r * 0.52, py + r * 0.52), fill=WHITE)
    d.ellipse((cx - r * 0.3, cy - r * 0.3, cx + r * 0.3, cy + r * 0.3), fill=YELLOW)


def sign_sarisari():
    rng = random.Random(11)
    S = 2
    w, h = 1024 * S, 256 * S
    img = Image.new("RGB", (w, h), DEEP_GREEN)
    d = ImageDraw.Draw(img)
    board = wobble_box(rng, 22 * S, 20 * S, w - 22 * S, h - 20 * S, 5 * S)
    d.polygon(board, fill=CREAM)
    patches(img, rng, (232, 214, 172), 3, 300 * S, 60, inside=board)
    d = ImageDraw.Draw(img)
    # A yellow sun burst behind the name's start, and a red ribbon under the words.
    d.ellipse((60 * S, 40 * S, 236 * S, 216 * S), fill=YELLOW)
    d.polygon(wobble_box(rng, 250 * S, 196 * S, w - 60 * S, 228 * S, 3 * S), fill=GREEN)
    letters(img, rng, "Aling Nena's", font("segoeprb.ttf", 44 * S), 330 * S, 50 * S, DEEP_GREEN, jitter=2, tilt=2)
    letters(img, rng, "SARI-SARI STORE", font("impact.ttf", 96 * S), 604 * S, 138 * S, RED, spacing=5 * S,
            jitter=3 * S, tilt=2.2, shadow=(120, 30, 30))
    letters(img, rng, "YELO  \u2022  LOAD  \u2022  BIGAS  \u2022  ULING", font("arialbd.ttf", 23 * S), 630 * S, 212 * S, CREAM,
            spacing=3 * S, jitter=1.5, tilt=1.5)
    # A painted bottle and a sampaguita in the sun disc.
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((128 * S, 96 * S, 168 * S, 196 * S), 12 * S, fill=GREEN)
    d.rectangle((140 * S, 62 * S, 156 * S, 104 * S), fill=GREEN)
    d.rectangle((128 * S, 132 * S, 168 * S, 164 * S), fill=CREAM)
    sampaguita(d, 950 * S, 70 * S, 26 * S, rng)
    img.resize((1024, 256), Image.LANCZOS).save(TEX / "prop_sign_sarisari_albedo.png")


def sign_arch():
    rng = random.Random(23)
    S = 2
    w, h = 2048 * S, 256 * S
    img = Image.new("RGB", (w, h), YELLOW)
    d = ImageDraw.Draw(img)
    board = wobble_box(rng, 16 * S, 14 * S, w - 16 * S, h - 14 * S, 4 * S, step=90)
    d.polygon(board, fill=DEEP_GREEN)
    patches(img, rng, (34, 104, 84), 4, 420 * S, 70, inside=board)
    d = ImageDraw.Draw(img)
    # The red welcome ribbon across the top middle.
    rib = wobble_box(rng, 800 * S, 26 * S, 1248 * S, 92 * S, 3 * S)
    d.polygon(rib, fill=RED)
    letters(img, rng, "MABUHAY!", font("impact.ttf", 54 * S), 1024 * S, 58 * S, CREAM, spacing=8 * S, jitter=2 * S, tilt=2)
    letters(img, rng, "ESKINITA SAMPAGUITA", font("impact.ttf", 118 * S), 1024 * S, 160 * S, CREAM, spacing=9 * S,
            jitter=3 * S, tilt=2, shadow=(18, 58, 50))
    letters(img, rng, "BRGY. 143", font("arialbd.ttf", 34 * S), 300 * S, 62 * S, YELLOW, spacing=4 * S, jitter=2, tilt=2)
    letters(img, rng, "ZONE 7", font("arialbd.ttf", 34 * S), 1748 * S, 62 * S, YELLOW, spacing=4 * S, jitter=2, tilt=2)
    d = ImageDraw.Draw(img)
    for side in (-1, 1):
        for k, (dx, dy, r) in enumerate(((150, 150, 44), (250, 190, 30), (86, 80, 28), (330, 130, 24))):
            sampaguita(d, (1024 + side * (1024 - dx)) * S, dy * S, r * S, rng)
    img.resize((2048, 256), Image.LANCZOS).save(TEX / "prop_sign_arch_albedo.png")


def decal_backboard(rng, S):
    w, h = 512 * S, 384 * S
    img = Image.new("RGB", (w, h), (236, 226, 204))
    patches(img, rng, (216, 198, 164), 3, 200 * S, 70)
    d = ImageDraw.Draw(img)
    def ring(x0, y0, x1, y1, t, col):
        outer = wobble_box(rng, x0, y0, x1, y1, 3 * S)
        inner = wobble_box(rng, x0 + t, y0 + t, x1 - t, y1 - t, 3 * S)
        m = Image.new("L", (w, h), 0)
        md = ImageDraw.Draw(m)
        md.polygon(outer, fill=255)
        md.polygon(inner, fill=0)
        img.paste(Image.new("RGB", (w, h), col), (0, 0), m)
    ring(12 * S, 12 * S, w - 12 * S, h - 12 * S, 20 * S, RED)
    ring(166 * S, 150 * S, 346 * S, 300 * S, 18 * S, RED)
    letters(img, rng, "LIGA NG ESKINITA", font("segoeprb.ttf", 30 * S), 256 * S, 90 * S, DEEP_GREEN, jitter=2, tilt=3)
    return img


def decal_brgy(rng, S):
    w, h = 512 * S, 384 * S
    img = Image.new("RGB", (w, h), DEEP_RED)
    d = ImageDraw.Draw(img)
    board = wobble_box(rng, 14 * S, 14 * S, w - 14 * S, h - 14 * S, 4 * S)
    d.polygon(board, fill=WHITE)
    patches(img, rng, (232, 222, 198), 2, 220 * S, 80, inside=board)
    letters(img, rng, "PAALALA", font("impact.ttf", 62 * S), 256 * S, 66 * S, RED, spacing=6 * S, jitter=2 * S, tilt=2.5)
    letters(img, rng, "BAWAL MAGKALAT", font("impact.ttf", 58 * S), 256 * S, 150 * S, INK, spacing=3 * S, jitter=2 * S, tilt=2.5)
    letters(img, rng, "SA ESKINITA", font("impact.ttf", 58 * S), 256 * S, 222 * S, INK, spacing=3 * S, jitter=2 * S, tilt=2.5)
    letters(img, rng, "Mahiya naman, kapitbahay!", font("segoeprb.ttf", 26 * S), 256 * S, 296 * S, DEEP_GREEN, jitter=1.5, tilt=2)
    letters(img, rng, "- Kap. Dolor", font("segoeprb.ttf", 22 * S), 370 * S, 340 * S, RED, jitter=1.5, tilt=2)
    return img


def decal_dama(rng, S):
    w = h = 384 * S
    img = Image.new("RGB", (w, h), (120, 84, 56))
    d = ImageDraw.Draw(img)
    m = 28 * S
    cell = (w - 2 * m) / 8
    d.polygon(wobble_box(rng, m - 8 * S, m - 8 * S, w - m + 8 * S, h - m + 8 * S, 3 * S), fill=CREAM)
    for i in range(8):
        for j in range(8):
            if (i + j) % 2:
                x, y = m + i * cell, m + j * cell
                d.polygon(wobble_box(rng, x + 2, y + 2, x + cell - 2, y + cell - 2, 1.6 * S, step=30), fill=GREEN)
    return img


def decal_sachets(rng, S):
    """One hanging strip of six sachets, top to bottom."""
    w, h = 128 * S, 384 * S
    img = Image.new("RGB", (w, h), WHITE)
    d = ImageDraw.Draw(img)
    cols = [RED, YELLOW, GREEN, PINK, (124, 84, 160), DEEP_GREEN]
    ch = h / 6
    for i, c in enumerate(cols):
        y = i * ch
        d.rectangle((0, y + 3 * S, w, y + ch - 3 * S), fill=c)
        d.ellipse((w * 0.24, y + ch * 0.26, w * 0.76, y + ch * 0.74), fill=WHITE)
        d.ellipse((w * 0.38, y + ch * 0.38, w * 0.62, y + ch * 0.62), fill=cols[(i + 2) % 6])
        d.rectangle((0, y + ch - 12 * S, w, y + ch - 3 * S), fill=(232, 228, 216))
    return img


def decal_poster(rng, S):
    w, h = 256 * S, 384 * S
    img = Image.new("RGB", (w, h), YELLOW)
    d = ImageDraw.Draw(img)
    d.polygon(wobble_box(rng, 12 * S, 12 * S, w - 12 * S, 150 * S, 3 * S), fill=PINK)
    letters(img, rng, "PISTA", font("impact.ttf", 76 * S), 128 * S, 62 * S, WHITE, spacing=4 * S, jitter=2 * S, tilt=3)
    letters(img, rng, "sa Eskinita", font("segoeprb.ttf", 26 * S), 128 * S, 122 * S, YELLOW, jitter=1.5, tilt=2)
    for k, word in enumerate(("LIGA", "SAYAWAN", "PALARO")):
        letters(img, rng, word, font("impact.ttf", 44 * S), 128 * S, (196 + k * 52) * S, (DEEP_GREEN, RED, DEEP_GREEN)[k],
                spacing=3 * S, jitter=2 * S, tilt=2.5)
    d = ImageDraw.Draw(img)
    for x in (40, 216):
        sampaguita(d, x * S, 350 * S, 16 * S, rng)
    return img


def decal_meter(rng, S):
    w = h = 256 * S
    img = Image.new("RGB", (w, h), (176, 184, 176))
    d = ImageDraw.Draw(img)
    d.ellipse((34 * S, 34 * S, 222 * S, 222 * S), fill=(232, 232, 222))
    d.ellipse((34 * S, 34 * S, 222 * S, 222 * S), outline=(92, 100, 98), width=10 * S)
    for k in range(5):
        x = (72 + k * 28) * S
        d.rectangle((x - 10 * S, 92 * S, x + 10 * S, 122 * S), fill=INK)
    d.rectangle((70 * S, 150 * S, 186 * S, 162 * S), fill=RED)
    d.ellipse((116 * S, 168 * S, 140 * S, 192 * S), fill=(92, 100, 98))
    return img


def decal_plate(rng, S):
    w, h = 256 * S, 128 * S
    img = Image.new("RGB", (w, h), DEEP_GREEN)
    d = ImageDraw.Draw(img)
    d.polygon(wobble_box(rng, 10 * S, 10 * S, w - 10 * S, h - 10 * S, 3 * S), fill=CREAM)
    letters(img, rng, "143-B", font("impact.ttf", 78 * S), 128 * S, 62 * S, DEEP_GREEN, spacing=4 * S, jitter=2 * S, tilt=3)
    return img


def decal_crate(rng, S):
    """The long side of a softdrink crate: a band and four hand holes."""
    w, h = 384 * S, 128 * S
    img = Image.new("RGB", (w, h), RED)
    d = ImageDraw.Draw(img)
    d.polygon(wobble_box(rng, 0, 40 * S, w, 92 * S, 2 * S), fill=CREAM)
    letters(img, rng, "SOFTDRINKS NI MANG KANOR", font("impact.ttf", 30 * S), 192 * S, 64 * S, DEEP_RED, spacing=2 * S, jitter=1.5 * S, tilt=2)
    for k in range(4):
        x = (48 + k * 96) * S
        d.rounded_rectangle((x - 30 * S, 10 * S, x + 30 * S, 30 * S), 8 * S, fill=(112, 26, 28))
        d.rounded_rectangle((x - 30 * S, 102 * S, x + 30 * S, 120 * S), 8 * S, fill=(112, 26, 28))
    return img


def decal_tag(rng, S):
    """A small price card: the store's hand-written "bawal utang" note."""
    w, h = 384 * S, 128 * S
    img = Image.new("RGB", (w, h), WHITE)
    letters(img, rng, "BAWAL UTANG", font("segoeprb.ttf", 40 * S), 192 * S, 44 * S, RED, jitter=2, tilt=3)
    letters(img, rng, "bukas na lang :)", font("segoepr.ttf", 26 * S), 192 * S, 96 * S, INK, jitter=1.5, tilt=2)
    return img


def decal_sack(rng, S):
    w, h = 384 * S, 256 * S
    img = Image.new("RGB", (w, h), (236, 226, 200))
    patches(img, rng, (218, 204, 170), 2, 160 * S, 80)
    d = ImageDraw.Draw(img)
    d.polygon(wobble_box(rng, 0, 24 * S, w, 52 * S, 2 * S), fill=GREEN)
    d.polygon(wobble_box(rng, 0, 208 * S, w, 236 * S, 2 * S), fill=RED)
    letters(img, rng, "BIGAS", font("impact.ttf", 84 * S), 192 * S, 112 * S, RED, spacing=5 * S, jitter=2 * S, tilt=3)
    letters(img, rng, "DINORADO  25 KG", font("arialbd.ttf", 26 * S), 192 * S, 178 * S, DEEP_GREEN, jitter=1.5, tilt=2)
    return img


def decal_wash(rng, S):
    """A flowered duster cloth for the blanket on the line: big soft blooms on pink."""
    w = h = 256 * S
    img = Image.new("RGB", (w, h), (226, 132, 160))
    d = ImageDraw.Draw(img)
    for i in range(3):
        for j in range(3):
            cx, cy = (44 + i * 84 + (j % 2) * 30) * S, (44 + j * 84) * S
            for k in range(5):
                a = k * math.tau / 5 + i
                d.ellipse((cx + math.cos(a) * 20 * S - 15 * S, cy + math.sin(a) * 20 * S - 15 * S,
                           cx + math.cos(a) * 20 * S + 15 * S, cy + math.sin(a) * 20 * S + 15 * S), fill=WHITE)
            d.ellipse((cx - 10 * S, cy - 10 * S, cx + 10 * S, cy + 10 * S), fill=YELLOW)
    return img


def decals():
    S = 2
    sheet = Image.new("RGB", (1024, 1024), CREAM)
    painters = {"backboard": decal_backboard, "brgy": decal_brgy, "dama": decal_dama, "sachets": decal_sachets,
                "poster": decal_poster, "meter": decal_meter, "plate": decal_plate, "crate": decal_crate,
                "tag": decal_tag, "sack": decal_sack, "wash": decal_wash}
    for k, (name, (x0, y0, x1, y1)) in enumerate(DECALS.items()):
        img = painters[name](random.Random(100 + k), S)
        sheet.paste(img.resize((x1 - x0, y1 - y0), Image.LANCZOS), (x0, y0))
    sheet.save(TEX / "prop_decals_albedo.png")


def compose(spec_path, out_path):
    """Lay rendered tiles out as a labelled contact sheet. spec = {"title", "cols", "tiles":
    [{"file", "label", "sub"}]}."""
    spec = json.loads(Path(spec_path).read_text())
    tiles = spec["tiles"]
    cols = spec.get("cols", 6)
    first = Image.open(tiles[0]["file"])
    tw, th = first.size
    bar, head = 44, 56
    rows = math.ceil(len(tiles) / cols)
    sheet = Image.new("RGB", (cols * tw, head + rows * (th + bar)), (40, 36, 34))
    d = ImageDraw.Draw(sheet)
    d.text((14, 12), spec.get("title", ""), font=font("arialbd.ttf", 28), fill=(250, 240, 214))
    f1, f2 = font("arialbd.ttf", 17), font("arial.ttf", 13)
    for i, t in enumerate(tiles):
        x, y = (i % cols) * tw, head + (i // cols) * (th + bar)
        sheet.paste(Image.open(t["file"]).convert("RGB"), (x, y))
        d.text((x + 8, y + th + 3), t["label"], font=f1, fill=(250, 240, 214))
        d.text((x + 8, y + th + 24), t.get("sub", ""), font=f2, fill=(190, 184, 168))
    Path(out_path).parent.mkdir(parents=True, exist_ok=True)
    sheet.save(out_path)
    print("[eskinita props] sheet", out_path)


def main():
    if "--compose" in sys.argv:
        i = sys.argv.index("--compose")
        compose(sys.argv[i + 1], sys.argv[i + 2])
        return
    TEX.mkdir(parents=True, exist_ok=True)
    sign_sarisari()
    sign_arch()
    decals()
    print("[eskinita props] textures written to", TEX)


if __name__ == "__main__":
    main()
