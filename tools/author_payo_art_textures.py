"""PAYO, the art's textures (2026-10-12): approved drawings COPIED from Kanto and Lagoon Cove, and six painted here.

  py -3 tools/author_payo_art_textures.py

Writes Assets/TumbangPreso/Art/Payo/Textures/<name>_albedo.png (and _normal.png where the source has one).

COPIED, never repainted (the materials tint them GREY for this map, tools/author_payo_art_kit.py): Kanto's dressed
stone, plain stone and paving; Lagoon's rock, earth, grass, thatch, timber, cloth, clay, bamboo, sawali, capiz and its
palm and leaf drawings; Kanto's bark and leaf. That is how Paete's run was made and why its stone matches the game.

PAINTED HERE, in the game's flat chunky manner (fat shapes, a dark line, one shade, one highlight):
  carve   the carved band round every block: a stepped key pattern between two cut lines
  weave   Cordillera cloth: red, black, cream and ochre bands with diamonds (the gate, banners, accents)
  rice    young rice in rows, seen from above
  moss    moss on stone: lobed patches, a lighter bloom
  water   paddy water: pale sky with a few soft rings
  mayari  THE SKY PIECE'S ATLAS, 2048 x 2048, four 1024 tiles: top left her hair and its stars, top right the figure
          (far arm, head, moon, gold), bottom left the near arm and hand, bottom right empty. Variant A. Variants B and
          C are written whole as mayari_b_albedo.png and mayari_c_albedo.png for the owner to swap.
"""
import math
import random
import shutil
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parent.parent
ART = ROOT / "Assets/TumbangPreso/Art"
OUT = ART / "Payo/Textures"
L, K = ART / "LagoonCove/Textures", ART / "Kanto/Textures"

COPY = {
    "ashlar": (K, "stone_blocks"), "stone": (K, "stone"), "paving": (K, "paving"), "paint": (K, "paint"), "bark": (K, "bark"), "leaf_kanto": (K, "leaf"),
    "rock_a": (L, "rock_a"), "earth_a": (L, "earth_a"), "grass_a": (L, "grass_a"), "thatch": (L, "thatch_a"), "timber": (L, "timber_a"),
    "cloth": (L, "cloth"), "clay": (L, "pc_clay"), "bamboo": (L, "bamboo_a"), "sawali": (L, "sawali_a"), "capiz": (L, "capiz"),
    "palm_trunk": (L, "palm_trunk"), "frond_broad": (L, "frond_broad"), "frond": (L, "frond"), "coconut": (L, "coconut"),
    "leaf": (L, "leaf_round"), "blade": (L, "blade"), "banana_stem": (L, "banana_stem"), "paddle": (L, "paddle"),
    "gumamela": (L, "gumamela"), "bougainvillea": (L, "bougainvillea"),
}
S = 512
INK = (40, 46, 54)


def soft(img, r=0.8):
    return img.filter(ImageFilter.GaussianBlur(r))


def carve():
    """One period of the band, 512 square, tiles sideways. Stone grey: the material does not tint it."""
    im = Image.new("RGB", (S, S), (146, 154, 162))
    d = ImageDraw.Draw(im)
    rnd = random.Random(2)
    for _ in range(60):                                    # the stone's own mottle
        x, y, r = rnd.uniform(0, S), rnd.uniform(0, S), rnd.uniform(14, 46)
        c = rnd.choice(((152, 160, 168), (140, 148, 157), (150, 157, 163)))
        for ox in (-S, 0, S):
            d.ellipse([x + ox - r, y - r * 0.6, x + ox + r, y + r * 0.6], fill=c)
    for (y0, y1) in ((26, 58), (S - 58, S - 26)):          # two cut lines
        d.rectangle([0, y0, S, y1], fill=(92, 100, 110))
        d.rectangle([0, y0, S, y0 + 7], fill=INK)
        d.rectangle([0, y1 - 6, S, y1], fill=(176, 184, 190))
    # the key: a stepped hook, twice a period, raised (light top edge, ink under edge)
    def key(x0, flip):
        pts = [(0, 150), (150, 150), (150, 250), (100, 250), (100, 200), (50, 200), (50, 310), (200, 310), (200, 100), (0, 100)]
        pts = [((200 - px) if flip else px, py) for (px, py) in pts]
        for ox in (-S, 0, S):
            poly = [(x0 + px + ox, py + 44) for (px, py) in pts]
            d.polygon([(x + 5, y + 7) for (x, y) in poly], fill=INK)
            d.polygon(poly, fill=(186, 193, 199))
            d.line(poly + [poly[0]], fill=(104, 112, 122), width=5)
    key(28, False)
    key(284, True)
    for cx in (256, 0, S):                                 # a diamond between the keys
        for (r, c) in ((44, INK), (34, (118, 126, 136)), (16, (196, 202, 206))):
            d.polygon([(cx, 256 - r), (cx + r, 256), (cx, 256 + r), (cx - r, 256)], fill=c)
    return soft(im)


def weave():
    im = Image.new("RGB", (S, S), (156, 47, 38))
    d = ImageDraw.Draw(im)
    RED, BLK, CRM, OCH = (156, 47, 38), (36, 32, 36), (232, 222, 196), (204, 150, 46)
    y = 0
    for (h, c) in ((40, BLK), (14, CRM), (70, RED), (14, OCH), (26, BLK), (14, OCH), (156, RED), (14, OCH), (26, BLK), (14, OCH), (70, RED), (14, CRM), (40, BLK)):
        d.rectangle([0, y, S, y + h], fill=c)
        y += h
    for k in range(4):                                     # diamonds along the wide red band
        cx = 64 + k * 128
        for (r, c) in ((58, BLK), (46, CRM), (30, OCH), (14, BLK)):
            d.polygon([(cx, 256 - r), (cx + r * 0.8, 256), (cx, 256 + r), (cx - r * 0.8, 256)], fill=c)
    for k in range(16):                                    # small teeth on the cream lines
        x = k * 32
        d.polygon([(x, 54), (x + 16, 40), (x + 32, 54)], fill=CRM)
        d.polygon([(x, S - 54), (x + 16, S - 40), (x + 32, S - 54)], fill=CRM)
    rnd = random.Random(4)
    px = im.load()
    for yy in range(0, S, 4):                              # the thread: every other row a shade down
        for xx in range(S):
            r, g, b = px[xx, yy]
            px[xx, yy] = (int(r * 0.9), int(g * 0.9), int(b * 0.9))
    return soft(im, 0.6)


def rice():
    im = Image.new("RGB", (S, S), (112, 168, 62))
    d = ImageDraw.Draw(im)
    rnd = random.Random(6)
    for row in range(8):
        y = row * 64 + 32
        d.rectangle([0, y + 20, S, y + 30], fill=(84, 138, 70))          # water between the rows
        for k in range(9):
            x = k * 64 - 16 + rnd.uniform(-6, 6) + (32 if row % 2 else 0)
            for ox in (-S, 0, S):
                d.ellipse([x + ox - 30, y - 22, x + ox + 30, y + 22], fill=(74, 124, 46))
                d.ellipse([x + ox - 27, y - 24, x + ox + 25, y + 14], fill=(138, 196, 76))
                d.ellipse([x + ox - 16, y - 20, x + ox + 6, y - 4], fill=(176, 222, 104))
    return soft(im)


def moss():
    im = Image.new("RGB", (S, S), (88, 118, 56))
    d = ImageDraw.Draw(im)
    rnd = random.Random(9)
    for _ in range(110):
        x, y, r = rnd.uniform(0, S), rnd.uniform(0, S), rnd.uniform(18, 52)
        c = rnd.choice(((98, 132, 60), (78, 106, 52), (110, 146, 66), (86, 120, 58)))
        for ox in (-S, 0, S):
            for oy in (-S, 0, S):
                d.ellipse([x + ox - r, y + oy - r * 0.8, x + ox + r, y + oy + r * 0.8], fill=c)
    for _ in range(70):
        x, y, r = rnd.uniform(0, S), rnd.uniform(0, S), rnd.uniform(5, 12)
        d.ellipse([x - r, y - r, x + r, y + r], fill=(142, 178, 84))
    return soft(im)


def water():
    im = Image.new("RGB", (S, S), (176, 208, 214))
    d = ImageDraw.Draw(im)
    rnd = random.Random(12)
    for _ in range(26):
        x, y, r = rnd.uniform(0, S), rnd.uniform(0, S), rnd.uniform(40, 110)
        d.ellipse([x - r, y - r * 0.4, x + r, y + r * 0.4], fill=rnd.choice(((188, 218, 222), (166, 200, 208), (198, 224, 226))))
    for _ in range(12):
        x, y, r = rnd.uniform(40, S - 40), rnd.uniform(40, S - 40), rnd.uniform(12, 26)
        d.ellipse([x - r, y - r * 0.5, x + r, y + r * 0.5], outline=(232, 244, 244), width=4)
    return soft(im, 1.2)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    n = 0
    for name, (folder, src) in COPY.items():
        for kind in ("albedo", "normal"):
            a = folder / ("%s_%s.png" % (src, kind))
            if a.exists():
                shutil.copyfile(a, OUT / ("%s_%s.png" % (name, kind))); n += 1
            elif kind == "albedo":
                raise SystemExit("missing source texture " + str(a))
    # THE STONE IS GREY HERE (owner: "more grey than sandy"). Kanto's stone drawings and Lagoon's rock are cream and
    # tan, and a multiplying tint cannot take the yellow out of their joints, so each is written again with its own
    # light and dark kept and its colour replaced by a cool grey: same drawing, grey stone. Normals are the originals.
    for name in ("ashlar", "stone", "paving", "rock_a"):
        src = Image.open(OUT / (name + "_albedo.png")).convert("RGB")
        lum = src.convert("L")
        lo, hi = lum.getextrema()
        mean = sum(i * c for i, c in enumerate(lum.histogram())) / float(lum.size[0] * lum.size[1])
        k = 196.0 / max(1.0, mean)
        lut = [max(0, min(255, int(128 + (v * k - 128) * 1.25))) for v in range(256)]      # a little more contrast: the joints must read
        g = lum.point(lut)
        grey = Image.merge("RGB", (g.point(lambda v: int(v * 0.955)), g.point(lambda v: int(v * 0.985)), g))
        grey.save(OUT / (name + "_grey_albedo.png")); n += 1
        nrm = OUT / (name + "_normal.png")
        if nrm.exists():
            shutil.copyfile(nrm, OUT / (name + "_grey_normal.png")); n += 1
    for name, fn in (("carve", carve), ("weave", weave), ("rice", rice), ("moss", moss), ("water", water)):
        fn().save(OUT / (name + "_albedo.png")); n += 1
    sys.path.insert(0, str(Path(__file__).resolve().parent))
    import draw_payo_mayari as MY
    layers = MY.variant_a_layers()
    atlas = Image.new("RGBA", (2048, 2048), (0, 0, 0, 0))
    for layer, (ox, oy) in zip(layers, ((0, 0), (1024, 0), (0, 1024))):
        atlas.alpha_composite(layer.resize((1024, 1024), Image.LANCZOS), (ox, oy))
    atlas.save(OUT / "mayari_albedo.png")
    MY.variant_b().resize((1024, 1024), Image.LANCZOS).save(OUT / "mayari_b_albedo.png")
    MY.variant_c().resize((1024, 1024), Image.LANCZOS).save(OUT / "mayari_c_albedo.png")
    print("[payo-art] wrote", n + 3, "texture files to", OUT)


if __name__ == "__main__":
    main()
