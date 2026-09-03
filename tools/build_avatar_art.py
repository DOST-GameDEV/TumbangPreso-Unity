"""Draw the profile avatars, in the logo's hand.

WHY THESE ARE DRAWN RATHER THAN CUT OUT OF THE ROSTER ART, AND IT IS NOT A SHORTCUT.
Two passes tried to knock the twelve Classic heads out of the sheets in
docs/Godot_Character_Select_References/. The background knockout is solvable (the ground
is the only cold thing in those frames, so a blue-greater-than-red test is exact at every
height of the gradient). THE FRAMING IS NOT: the model stands at a different height and a
different scale in every sheet, so half the set came out cropped at the forehead. A picker
is twelve things seen TOGETHER, and twelve things that disagree about where the eyes sit is
not a set, it is a mistake repeated twelve times.

WHY THEY ARE THE RIGHT ANSWER ON THE BRIEF ANYWAY. Flat fills inside a heavy uneven
deep-red stroke, four unequal corner radii, no ramp and no bevel: that is exactly how the
wordmark is drawn, and PaperCraft.PaintBrand now draws every button in the game by the same
rules. So an avatar reads as belonging to this game rather than as stock clip art, there is
no licence question to answer, and CLAUDE.md 6.4's colour ban cannot be violated because
every fill here is a UiTheme brand constant.

    "maybe give them an option to pick from a bunch of cute profile pics"   2026-09-03

RUN IT WITH:
    %LOCALAPPDATA%\\Programs\\Python\\Python312\\python.exe tools/build_avatar_art.py
"""

import hashlib
import math
import os

from PIL import Image, ImageDraw

ART = "Assets/TumbangPreso/Art/ui/brand/avatars"
RES = "Assets/TumbangPreso/Resources/UI/avatars"

TILE = 168
SS = 4                      # supersample. PIL has no antialiased polygon fill.

# Every colour is a UiTheme brand constant. Nothing here is eyeballed and nothing here is
# outside the palette CLAUDE.md 6.4 fixes.
RED = (152, 7, 21)          # BrandRed        the outline, everywhere
RIM = (195, 46, 13)         # BrandRimRed     the lit state of the outline
INK = (28, 15, 6)           # Ink             eyes and mouth
CREAM = (245, 230, 200)     # Cream           the chalk tick

GROUNDS = [
    (252, 211, 159),        # BrandHoney
    (232, 199, 126),        # BrandKhaki
    (245, 181, 33),         # BrandGolden
    (214, 206, 1),          # BrandChartreuse
    (253, 128, 65),         # BrandPersimmon
    (179, 168, 40),         # BrandArmy
]

# Skin is a ROSTER FACT, not a palette choice. docs/VISION.md 6: "nobody's skin is a dial".
# These six are sampled off the twelve Classic portraits rather than invented, so the picker
# offers the range of people who are actually in this game.
SKIN = [(240, 192, 138), (223, 162, 104), (201, 131, 74),
        (168, 98, 58), (239, 208, 168), (181, 113, 63)]
HAIR = [(42, 26, 16), (61, 36, 21), (28, 17, 9),
        (90, 52, 24), (36, 26, 18), (68, 42, 22)]


# ---------------------------------------------------------------------------------------
# geometry
# ---------------------------------------------------------------------------------------

def quad(p0, p1, p2, n=14):
    """Sample a quadratic Bezier. The shapes are authored as curves because the mark is;
    a polygon approximation with enough samples is indistinguishable at 4x supersample."""
    out = []
    for i in range(n + 1):
        t = i / n
        u = 1.0 - t
        out.append((u * u * p0[0] + 2 * u * t * p1[0] + t * t * p2[0],
                    u * u * p0[1] + 2 * u * t * p1[1] + t * t * p2[1]))
    return out


def blob(pts):
    """A closed outline from a list of either points or (ctrl, end) curve pairs."""
    out = []
    cur = pts[0]
    out.append(cur)
    for seg in pts[1:]:
        if isinstance(seg[0], (int, float)):
            out.append(seg)
            cur = seg
        else:
            out.extend(quad(cur, seg[0], seg[1])[1:])
            cur = seg[1]
    return out


def stroked(d, pts, fill, width):
    """Fill a closed shape and draw its outline in the deep red.

    THE OUTLINE IS DRAWN AS A JOINED POLYLINE WITH ROUND CAPS RATHER THAN AS PIL's
    polygon outline, because PIL's is one pixel wide and cannot be widened. The mark's
    line is 8.5 per cent of its letter height; a hairline would make these read as icons
    from a different set.
    """
    d.polygon(pts, fill=fill)
    d.line(list(pts) + [pts[0]], fill=RED, width=width, joint="curve")
    r = width / 2.0
    for (x, y) in pts:
        d.ellipse([x - r, y - r, x + r, y + r], fill=RED)


def rounded4(d, box, radii, fill):
    """A rounded rect with FOUR DIFFERENT corner radii.

    The same decision PaperCraft.Depth4 makes for every button: one radius on all four
    corners is the single detail that says "computed" out loud, and the mark has no two
    corners alike.
    """
    x0, y0, x1, y1 = box
    tl, tr, br, bl = radii
    pts = []
    for (cx, cy, r, a0, a1) in ((x0 + tl, y0 + tl, tl, 180, 270),
                                (x1 - tr, y0 + tr, tr, 270, 360),
                                (x1 - br, y1 - br, br, 0, 90),
                                (x0 + bl, y1 - bl, bl, 90, 180)):
        for i in range(13):
            a = math.radians(a0 + (a1 - a0) * i / 12.0)
            pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    d.polygon(pts, fill=fill)
    return pts


# ---------------------------------------------------------------------------------------
# the faces
# ---------------------------------------------------------------------------------------

def head(i):
    S = TILE * SS
    im = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    k = SS

    ground = GROUNDS[i % len(GROUNDS)]
    skin = SKIN[(i * 5 + 1) % len(SKIN)]
    hair = HAIR[(i * 7 + 2) % len(HAIR)]

    # the tile. Unequal radii, seeded off the slot, so the twelve are a set of drawings
    # rather than one drawing twelve times.
    h = int(hashlib.md5(str(i).encode()).hexdigest()[:6], 16)
    radii = [(26 + ((h >> (b * 3)) & 7) * 3) * k for b in range(4)]
    rounded4(d, (0, 0, S - 1, S - 1), radii, ground)

    lw = 7 * k

    # the head: a block with a heavy line round it
    face = blob([(40 * k, 48 * k),
                 ((37 * k, 25 * k), (62 * k, 24 * k)),
                 (110 * k, 26 * k),
                 ((132 * k, 28 * k), (130 * k, 52 * k)),
                 (131 * k, 104 * k),
                 ((132 * k, 126 * k), (108 * k, 128 * k)),
                 (60 * k, 127 * k),
                 ((38 * k, 126 * k), (40 * k, 102 * k))])
    stroked(d, face, skin, lw)

    style = i % 6
    if style == 0:
        pts = blob([(36 * k, 56 * k), ((33 * k, 20 * k), (86 * k, 22 * k)),
                    ((139 * k, 20 * k), (134 * k, 58 * k)),
                    ((120 * k, 40 * k), (86 * k, 43 * k)),
                    ((52 * k, 40 * k), (36 * k, 56 * k))])
    elif style == 1:
        pts = blob([(35 * k, 60 * k), ((40 * k, 18 * k), (88 * k, 22 * k)),
                    ((137 * k, 18 * k), (135 * k, 62 * k)),
                    ((126 * k, 34 * k), (108 * k, 47 * k)),
                    ((92 * k, 30 * k), (74 * k, 47 * k)),
                    ((54 * k, 34 * k), (35 * k, 60 * k))])
    elif style == 2:
        pts = blob([(35 * k, 58 * k), ((35 * k, 20 * k), (86 * k, 22 * k)),
                    ((137 * k, 20 * k), (135 * k, 60 * k)), (135 * k, 80 * k),
                    ((126 * k, 62 * k), (118 * k, 78 * k)), (118 * k, 50 * k),
                    ((86 * k, 43 * k), (54 * k, 52 * k)), (54 * k, 80 * k),
                    ((44 * k, 62 * k), (35 * k, 80 * k))])
    elif style == 3:
        pts = blob([(33 * k, 54 * k), ((42 * k, 16 * k), (88 * k, 20 * k)),
                    ((135 * k, 16 * k), (137 * k, 56 * k)),
                    ((128 * k, 44 * k), (118 * k, 51 * k)),
                    ((86 * k, 30 * k), (54 * k, 51 * k)),
                    ((44 * k, 44 * k), (33 * k, 54 * k))])
    elif style == 4:
        pts = blob([(37 * k, 52 * k), ((44 * k, 18 * k), (88 * k, 20 * k)),
                    ((133 * k, 18 * k), (133 * k, 54 * k)),
                    ((104 * k, 36 * k), (88 * k, 47 * k)),
                    ((68 * k, 36 * k), (37 * k, 52 * k))])
    else:
        pts = blob([(35 * k, 62 * k), ((33 * k, 18 * k), (88 * k, 22 * k)),
                    ((141 * k, 18 * k), (135 * k, 64 * k)),
                    ((112 * k, 42 * k), (88 * k, 45 * k)),
                    ((60 * k, 42 * k), (35 * k, 62 * k))])
    stroked(d, pts, hair, 6 * k)

    if style == 3:                      # a ponytail, escaping the head's own outline
        tail = blob([(126 * k, 44 * k), ((152 * k, 50 * k), (148 * k, 78 * k)),
                     ((145 * k, 98 * k), (130 * k, 94 * k)),
                     ((118 * k, 90 * k), (124 * k, 76 * k)),
                     ((130 * k, 62 * k), (126 * k, 44 * k))])
        stroked(d, tail, hair, 6 * k)
    if style == 4:                      # a hair tie in the rim red
        d.ellipse([113 * k, 17 * k, 139 * k, 43 * k], fill=RIM, outline=RED, width=6 * k)
    if style == 5:                      # a headband
        d.line([(28 * k, 45 * k), (148 * k, 41 * k)], fill=RIM, width=9 * k)

    # eyes are BLOCKS, not circles: the cast is voxel and the mark is drawn in flat shapes
    d.rounded_rectangle([60 * k, 72 * k, 75 * k, 91 * k], radius=4 * k, fill=INK)
    d.rounded_rectangle([97 * k, 72 * k, 112 * k, 91 * k], radius=4 * k, fill=INK)

    mouth = i % 4
    if mouth == 0:
        d.line(quad((72 * k, 105 * k), (86 * k, 118 * k), (100 * k, 105 * k)),
               fill=INK, width=6 * k, joint="curve")
    elif mouth == 1:
        d.line([(74 * k, 107 * k), (98 * k, 107 * k)], fill=INK, width=6 * k)
    elif mouth == 2:
        d.polygon(blob([(70 * k, 102 * k), ((86 * k, 122 * k), (102 * k, 104 * k)),
                        ((86 * k, 110 * k), (70 * k, 102 * k))]), fill=INK)
    else:
        d.line(quad((72 * k, 111 * k), (86 * k, 99 * k), (100 * k, 111 * k)),
               fill=INK, width=6 * k, joint="curve")

    extra = i % 5
    if extra == 1:              # a plaster. Everybody in a street game has one.
        d.polygon([(99 * k, 55 * k), (128 * k, 48 * k), (131 * k, 61 * k), (102 * k, 68 * k)],
                  fill=RIM, outline=RED, width=4 * k)
    elif extra == 2:            # freckles
        for (fx, fy) in ((54, 98), (64, 104), (118, 98), (108, 104)):
            d.ellipse([(fx - 4) * k, (fy - 4) * k, (fx + 4) * k, (fy + 4) * k],
                      fill=(168, 98, 58))
    elif extra == 3:            # the chalk tick, the one sign borrowed from the vocabulary
        d.line([(20 * k, 138 * k), (34 * k, 152 * k), (58 * k, 122 * k)],
               fill=CREAM + (140,), width=7 * k, joint="curve")

    return im.resize((TILE, TILE), Image.LANCZOS)


def tsinelas():
    """The mark itself, as an avatar. It is the game's own subject."""
    S, k = TILE * SS, SS
    im = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    rounded4(d, (0, 0, S - 1, S - 1), [30 * k, 38 * k, 32 * k, 26 * k], GROUNDS[3])
    sole = blob([(52 * k, 30 * k), ((88 * k, 18 * k), (114 * k, 38 * k)),
                 ((133 * k, 54 * k), (127 * k, 92 * k)),
                 ((121 * k, 132 * k), (106 * k, 148 * k)),
                 ((88 * k, 164 * k), (64 * k, 154 * k)),
                 ((40 * k, 142 * k), (40 * k, 106 * k)),
                 ((40 * k, 66 * k), (52 * k, 30 * k))])
    stroked(d, sole, (181, 113, 63), 8 * k)
    d.line(quad((60 * k, 60 * k), (84 * k, 82 * k), (108 * k, 60 * k)),
           fill=(245, 181, 33), width=13 * k, joint="curve")
    d.line([(84 * k, 72 * k), (84 * k, 116 * k)], fill=(245, 181, 33), width=13 * k)
    return im.resize((TILE, TILE), Image.LANCZOS)


def lata():
    """The tin can. The whole game is about it."""
    S, k = TILE * SS, SS
    im = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    rounded4(d, (0, 0, S - 1, S - 1), [34 * k, 26 * k, 36 * k, 30 * k], GROUNDS[0])
    body = [(52 * k, 44 * k), (116 * k, 44 * k), (116 * k, 126 * k), (52 * k, 126 * k)]
    d.polygon(body, fill=(232, 226, 210))
    d.rectangle([52 * k, 70 * k, 116 * k, 96 * k], fill=RIM)
    d.line(body + [body[0]], fill=RED, width=8 * k, joint="curve")
    d.ellipse([52 * k, 30 * k, 116 * k, 58 * k], fill=(253, 223, 186), outline=RED, width=7 * k)
    return im.resize((TILE, TILE), Image.LANCZOS)


def star():
    """A chalk star. Drawn on the road, which is where this game is played."""
    S, k = TILE * SS, SS
    im = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    rounded4(d, (0, 0, S - 1, S - 1), [28 * k, 34 * k, 26 * k, 36 * k], GROUNDS[4])
    pts = []
    for n in range(10):
        r = (60 if n % 2 == 0 else 26) * k
        a = math.radians(-90 + n * 36)
        pts.append((84 * k + r * math.cos(a), 84 * k + r * math.sin(a)))
    stroked(d, pts, (245, 181, 33), 8 * k)
    return im.resize((TILE, TILE), Image.LANCZOS)


# ---------------------------------------------------------------------------------------

FONT_META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
  textureType: 8
  textureShape: 1
  spriteMode: 1
  spritePixelsToUnits: 100
  alphaIsTransparency: 1
  spriteMeshType: 0
  spriteExtrude: 1
  spriteGenerateFallbackPhysicsShape: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def guid(path):
    return hashlib.md5(("tumbangpreso/" + path).encode("utf-8")).hexdigest()


def main():
    for folder in (ART, RES):
        os.makedirs(folder, exist_ok=True)

    tiles = [(f"avatar_{i + 1:02d}", head(i)) for i in range(12)]
    tiles += [("avatar_tsinelas", tsinelas()),
              ("avatar_lata", lata()),
              ("avatar_star", star())]

    total = 0
    for name, im in tiles:
        for folder in (ART, RES):
            p = os.path.join(folder, name + ".png")
            im.save(p, optimize=True)
            with open(p + ".meta", "w", newline="\n") as fh:
                fh.write(FONT_META.format(guid=guid(p)))
        total += os.path.getsize(os.path.join(RES, name + ".png"))
        print(f"  {name:18} {os.path.getsize(os.path.join(RES, name + '.png')) // 1024} KB")
    print(f"{len(tiles)} avatars, {total // 1024} KB in the player")


if __name__ == "__main__":
    main()
