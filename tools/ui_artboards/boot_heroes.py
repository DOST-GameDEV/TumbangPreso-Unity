"""Hero, story, skill tree, item, shop, task and picture screens in the boot kit.

Laid out from the captures in Logs/ui-artboards (hub-hero, hub-ability, hub-story, hub-skilltree,
hub-item, hub-item-inspect, hub-shop, hub-tasks, hub-avatar). A hero stands on a lit stage with
its name and text floating on the berry weave; small things are berry frames, large panels are
cream.

    py -3 tools/ui_artboards/boot_heroes.py [--only hub-hero hub-shop ...]
"""
import argparse
import json
import math
import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

from bootkit import (ROOT, Board, DARUMA, GRAND, MAROON, INK, TAN, TAN_D, WHITE, TITLE_EDGE, GREEN_INK, CREAM,
                     SOFT_RED, LIME, CARD, FACE_LIP, art, button, button_art, centred, field_art, frame, ground, header,
                     font, left, paint_mid, panel, para, poster, right, rule, scrim, subtitle, tabs, tag, text,
                     title, well, wrap, back, slice3, dark_panel, sunburst_art, framed)

CAPTURES = ROOT / "Logs" / "ui-artboards"
UI = ROOT / "Assets/TumbangPreso/Resources/UI"
OUT = ROOT / "ArtSource/ui/revamp-2026-10-30-boot/preview"
HOME = UI / "home/zack-home-poster.png"
STREET = UI / "map-previews/Eskinita-poster.png"
YELLOW_INK = (0xA8, 0x4E, 0x0C)
EDGE = dict(stroke=3, stroke_fill=TITLE_EDGE)


# Small helpers

def raster(board_id, name_or_file):
    """A raster of a capture, by file name (r007.png) or layer name."""
    m = json.loads((CAPTURES / board_id / "manifest.json").read_text(encoding="utf-8"))
    L = next(L for L in m["layers"] if L.get("file") == name_or_file or L.get("name") == name_or_file)
    return Image.open(CAPTURES / board_id / L["file"]).convert("RGBA")


def ink(im, floor=40):
    """Crop to the visible paint."""
    box = im.getchannel("A").point(lambda v: 255 if v > floor else 0).getbbox()
    return im.crop(box) if box else im


def fitted(im, w, h):
    im = im.copy()
    im.thumbnail((w, h), Image.LANCZOS)
    return im


def tint(im, rgb):
    out = Image.new("RGBA", im.size, tuple(rgb) + (255,))
    out.putalpha(im.getchannel("A"))
    return out


def showcase(b, x, y, w, h, layers, key, on=False, name="Card", burst_h=None, **kw):
    """A card whose whole opening is a sunburst with the art composited over it, cut to the rim
    by `framed`. layers: (image, cx, cy) in card coordinates. Below burst_h the face stays berry."""
    ow, oh = w - 22, h - 22
    base = Image.new("RGBA", (ow, oh), CARD + (255,))
    base.alpha_composite(sunburst_art(ow, burst_h - 11 if burst_h else oh, **kw), (0, 0))
    for im, cx, cy in layers:
        base.alpha_composite(im, (int(cx - im.width / 2 - 11), int(cy - im.height / 2 - 11)))
    framed(b, x, y, w, h, base, key, on=on, name=name)


def put(b, key, im, x, y, name=None):
    b.place(key, im, x, y, name or key)


def put_centred(b, key, im, cx, cy, name=None):
    b.place(key, im, cx - im.width / 2, cy - im.height / 2, name or key)


def fit_size(s, width, size, face=DARUMA, weight=None, lo=28):
    """The largest size up to `size` at which s fits in width (never below lo)."""
    while size > lo and font(face, size, weight).getlength(s) > width:
        size -= 1
    return size


def shout(b, xy, s, size, name=None, edge=5):
    """White Darumadrop with the berry edge, floating on the weave."""
    text(b, xy, s, size, fill=WHITE, stroke=edge, stroke_fill=TITLE_EDGE, name=name or "Label")


def sentence(b, xy, s, size=32, weight=650, fill=WHITE, name="Note"):
    text(b, xy, s, size, face=GRAND, fill=fill, weight=weight, name=name, **(EDGE if fill == WHITE else {}))


def glyph(kind, size, color):
    """Little icons drawn at 4x and shrunk: menu bars, a star, expand corners."""
    s = size * 4
    im = Image.new("RGBA", (s, s))
    d = ImageDraw.Draw(im)
    c = tuple(color) + (255,)
    if kind == "menu":
        for i in range(3):
            y = s * (0.22 + i * 0.26)
            d.rounded_rectangle((s * 0.12, y, s * 0.88, y + s * 0.14), radius=s * 0.06, fill=c)
    elif kind == "star":
        pts = []
        for i in range(10):
            r = s * (0.46 if i % 2 == 0 else 0.2)
            a = -math.pi / 2 + i * math.pi / 5
            pts.append((s / 2 + r * math.cos(a), s / 2 + r * math.sin(a) + s * 0.03))
        d.polygon(pts, fill=c)
    elif kind == "expand":
        t, L = s * 0.12, s * 0.30
        for sx in (0, 1):
            for sy in (0, 1):
                x0, y0 = (s * 0.14, s * 0.14) if not sx else (s * 0.86, s * 0.14)
                x0 = s * (0.86 if sx else 0.14)
                y0 = s * (0.86 if sy else 0.14)
                dx, dy = (-1 if sx else 1), (-1 if sy else 1)
                d.rectangle((min(x0, x0 + dx * L), min(y0, y0 + dy * t), max(x0, x0 + dx * L), max(y0, y0 + dy * t)), fill=c)
                d.rectangle((min(x0, x0 + dx * t), min(y0, y0 + dy * L), max(x0, x0 + dx * t), max(y0, y0 + dy * L)), fill=c)
    return im.resize((size, size), Image.LANCZOS)


def icon_button(b, kind, x, y, w, h, gname, gsize=None):
    """A kit button carrying a drawn glyph on its face."""
    piece = button_art(kind, w, h)
    b.place("button-%s-%dx%d" % (kind, w, h), piece, x, y, "Button %s" % kind)
    g = glyph(gname, gsize or int(h * 0.5), YELLOW_INK if kind == "yellow" else MAROON)
    put_centred(b, "glyph-%s-%s-%d" % (gname, kind, g.width), g, x + paint_mid(piece, FACE_LIP), y + h * FACE_LIP)


def chrome(b):
    """Top right: the TANSAN balance with its plus, and the menu."""
    b.group = "Chrome"
    coin = Image.open(UI / "handmade-lobby/icon_coin.png").convert("RGBA")
    coin = fitted(coin, 112, 112)
    put(b, "coin", coin, 1318, 18, "TANSAN coin")
    b.place("field-240x84", field_art(240, 84), 1432, 40, "Balance")
    left(b, 1462, 40 + 84 * 0.48, "000,000", 36, fill=TAN_D, name="Balance")
    button(b, "yellow", 1690, 40, 90, 84, "+", 44)
    icon_button(b, "yellow", 1796, 40, 90, 84, "menu")


def chalk(b, cx, cy, rx, ry, seed=5):
    rnd = random.Random(seed)
    im = Image.new("RGBA", (rx * 2 + 40, ry * 2 + 40))
    d = ImageDraw.Draw(im)
    pts = []
    for i in range(72):
        a = i / 72 * math.tau
        pts.append((rx + 20 + (rx + rnd.uniform(-4, 4)) * math.cos(a), ry + 20 + (ry + rnd.uniform(-3, 3)) * math.sin(a)))
    d.line(pts + pts[:2], fill=(0xF3, 0xEA, 0xE2, 235), width=7, joint="curve")
    put(b, "chalk-%d-%d" % (rx, ry), im, cx - rx - 20, cy - ry - 20, "Chalk circle")


def stage(b, cx, spot_y=(150, 1000)):
    """A lit patch on the weave, as on the pick screen."""
    b.group = "Stage"
    mask = Image.new("L", (1920, 1080))
    ImageDraw.Draw(mask).ellipse((cx - 520, spot_y[0], cx + 520, spot_y[1]), fill=150)
    light = Image.new("RGBA", (1920, 1080), (0xC2, 0x4A, 0x5A, 255))
    light.putalpha(mask.filter(ImageFilter.GaussianBlur(120)))
    put(b, "spot-%d" % cx, light, 0, 0, "Spotlight")


def strand(b, pts, key):
    """A chalk-coloured wobbly line between points (the skill tree's branches)."""
    xs, ys = [p[0] for p in pts], [p[1] for p in pts]
    x0, y0 = min(xs) - 12, min(ys) - 12
    im = Image.new("RGBA", (int(max(xs) - x0 + 12), int(max(ys) - y0 + 12)))
    d = ImageDraw.Draw(im)
    d.line([(x - x0, y - y0) for x, y in pts], fill=TAN + (255,), width=12, joint="curve")
    put(b, key, im, x0, y0, "Branch")


# Hero

ABILITIES = [("DanteShield", "E", False), ("DanteBoulder", "ATTACKING", False),
             ("DanteBarrier", "DEFENDING", False), ("DanteFissure", "X", True)]


def hero_screen(b):
    ground(b)
    header(b, "HERO")
    chrome(b)
    stage(b, 500)
    b.group = "Hero"
    chalk(b, 500, 930, 270, 56)
    model = ink(raster("hub-hero", "r007.png"), 8)
    k = 1.3
    model = model.resize((int(model.width * k), int(model.height * k)), Image.LANCZOS)
    put(b, "hero-dante", model, 500 - model.width / 2, 940 - model.height, "Hero (game render)")
    b.group = "Hero switch"
    button(b, "yellow", 60, 530, 100, 100, "<", 56)
    button(b, "yellow", 840, 530, 100, 100, ">", 56)
    b.group = "Details"
    text(b, (1000, 168), "EARTH JUGGERNAUT", 34, face=GRAND, fill=WHITE, weight=800, name="Role", **EDGE)
    text(b, (996, 190), "DANTE", 140, fill=WHITE, stroke=9, stroke_fill=TITLE_EDGE, name="Hero")
    for i, (icon, label, on) in enumerate(ABILITIES):
        x = 1000 + i * 220
        frame(b, x, 410, 200, 190, on=on, name=label.title())
        ic = Image.open(UI / "ability-icons" / (icon + ".png")).convert("RGBA")
        ic = fitted(ink(ic), 110, 100)
        put_centred(b, "ability-" + icon, ic, x + 100, 410 + 78, label.title())
        centred(b, x + 100, 410 + 152, label, 28, fill=LIME if on else CREAM, name="Key")
    shout(b, (1000, 650), "BIOGRAPHY", 44, name="Section", edge=5)
    para(b, 1000, 726, wrap("From Montalban, Rizal.  Holds the difficult space. Refuses to be rushed.", 32, 860, weight=650),
         32, 44, fill=WHITE, weight=650, stroke=3, stroke_fill=TITLE_EDGE)
    button(b, "plain", 1000, 810, 470, 96, "READ THE STORY", 38)
    book = ink(raster("hub-hero", "r031.png"))
    put(b, "icon-book", book, 1000 + 38, 810 + 96 * FACE_LIP - book.height / 2, "Book icon")
    b.group = "Footer"
    text(b, (1000, 962), "YOUR HERO", 40, fill=WHITE, stroke=5, stroke_fill=TITLE_EDGE, name="Label")
    button(b, "go", 1330, 928, 530, 124, "PLAYING AS DANTE", 44)


def hub_hero(board_id):
    b = Board()
    hero_screen(b)
    return b


def hub_ability(board_id):
    b = Board()
    hero_screen(b)
    scrim(b, (40, 8, 14, 150))
    px, py, pw, ph = 375, 270, 1170, 540
    dark_panel(b, px, py, pw, ph, seed=31, group="Popup: Ability")
    b.group = "Popup: Ability"
    button(b, "red", px + 40, py + 40, 120, 100, "<", 62)
    left(b, px + 200, py + 78, "UNSTOPPABLE", 76, fill=CREAM, name="Heading")
    left(b, px + 204, py + 148, "SIGNATURE · E", 34, face=GRAND, fill=LIME, weight=700, caps=False, name="Kind")
    ic = fitted(ink(Image.open(UI / "ability-icons/DanteShield.png").convert("RGBA")), 220, 220)
    showcase(b, px + 50, py + 210, 290, 290, [(ic, 145, 145)], "ability-dante-shield", name="Ability")
    para(b, px + 400, py + 218, wrap("Remove removable negative effects and gain Status Immunity for 15 s. "
                                      "Tagged cannot be removed.", 36, 700, weight=500), 36, 52, fill=CREAM, weight=500)
    return b


# Story

STORY = ("In Montalban, Dante was the player trusted to keep a crowded game fair: settle the argument, straighten "
         "the can, give the youngest player another try. He still arrives early to mark out the plaza court. "
         "That patience becomes stubbornness when a rival tries to move him. His stone armour and shaking faults "
         "make the mountain stories he grew up hearing feel close enough to touch. Yasmin admires his discipline. "
         "She also knows exactly how long he will defend a bad position.")


def hub_story(board_id):
    b = Board()
    ground(b)
    header(b, "MEET DANTE")
    panel(b, 60, 190, 1800, 830, seed=33)
    b.group = "Story"
    left(b, 130, 270, "MONTALBAN, RIZAL", 54, fill=MAROON, name="Hometown")
    left(b, 132, 340, "Home court · Bayan Plaza", 34, face=GRAND, fill=INK, weight=600, caps=False, name="Court")
    rule(b, 120, 1800, 386)
    frame(b, 130, 430, 500, 410, name="Portrait")
    p = fitted(Image.open(UI / "portraits/dante.png").convert("RGBA"), 400, 360)
    put_centred(b, "portrait-dante-story", p, 380, 635, "Dante")
    tl = wrap("HOLDS THE DIFFICULT SPACE. REFUSES TO BE RUSHED.", 34, 520, face=DARUMA, weight=None)
    for i, s in enumerate(tl):
        centred(b, 380, 898 + i * 44, s, 34, fill=MAROON, name="Tagline")
    para(b, 740, 432, wrap(STORY, 36, 1060, weight=550), 36, 58, fill=INK, weight=550)
    return b


# Skill tree

ROSTER = ["dante", "cheska", "sean", "zack", "nemu", "phaister", "rafi", "amihan", "paete"]


def node(b, x, y, w, h, name, status, icon, on, picked):
    if picked:  # the skill being viewed is a slightly darker card (owner, 2026-11-01)
        from bootkit import dark_card_art
        put(b, "card-viewed-%dx%d%s" % (w, h, "-on" if on else ""), dark_card_art(w, h, on, shade=.72), x, y, name.title())
    else:
        frame(b, x, y, w, h, on=on, name=name.title())
    ic = fitted(ink(Image.open(UI / "ability-icons" / (icon + ".png")).convert("RGBA")), 100, 100)
    put_centred(b, "skill-" + icon, ic, x + 82, y + h / 2 - 2, name.title())
    nm = fit_size(name, w - 160 - 20, 34)
    left(b, x + 150, y + h * 0.36, name, nm, fill=CREAM, name="Skill")
    left(b, x + 152, y + h * 0.70, status, 28, face=GRAND, fill=LIME if on else TAN, weight=700, caps=False, name="State")


def hub_skilltree(board_id):
    b = Board()
    ground(b)
    header(b, "SKILL TREE")
    chrome(b)
    b.group = "Heroes"
    for i, n in enumerate(ROSTER):
        x = 60 + i * 166
        frame(b, x, 172, 150, 150, on=n == "sean", name=n.title())
        p = fitted(Image.open(UI / "portraits" / (n + ".png")).convert("RGBA"), 124, 124)
        put_centred(b, "portrait-" + n + "-s", p, x + 75, 172 + 76, n.title())
    b.group = "Tree"
    strand(b, [(310, 540), (310, 640)], "br-a")
    strand(b, [(840, 540), (840, 640)], "br-b")
    strand(b, [(310, 790), (440, 850), (575, 880)], "br-c")
    strand(b, [(840, 790), (710, 850), (575, 880)], "br-d")
    sentence(b, (60, 354), "SKILL 1 · LAVA RAM", 34, weight=800, name="Skill line")
    sentence(b, (600, 354), "SKILL 2 · MOLTEN THROW", 34, weight=800, name="Skill line")
    node(b, 60, 400, 500, 150, "FLAME RUSH", "EQUIPPED", "SeanRush", True, True)
    node(b, 60, 640, 500, 150, "AFTERBURN", "UNLOCKED", "SeanRush", False, False)
    node(b, 590, 400, 500, 150, "IGNITION CANNON", "EQUIPPED", "SeanIgnite", True, False)
    node(b, 590, 640, 500, 150, "FLARE SHOT", "UNLOCKED", "SeanIgnite", False, False)
    frame(b, 480, 860, 190, 170, on=True, name="Rago")
    p = fitted(Image.open(UI / "portraits/sean.png").convert("RGBA"), 150, 140)
    put_centred(b, "portrait-sean-tree", p, 575, 945, "Rago")
    b.group = "Details"
    panel(b, 1150, 350, 710, 500, seed=34)
    left(b, 1200, 420, "RAGO", 56, fill=MAROON, name="Hero")
    left(b, 1202, 480, "MASTERY 1", 32, face=GRAND, fill=INK, weight=650, caps=False, name="Mastery")
    left(b, 1202, 548, "FLAME RUSH", 40, fill=MAROON, name="Skill")
    para(b, 1202, 585, wrap("A long launch that knocks down what you hit and burns the road.", 32, 620, weight=500),
         32, 44, fill=INK, weight=500)
    left(b, 1202, 700, "+ You cross the court", 32, face=GRAND, fill=GREEN_INK, weight=650, caps=False, name="Pro")
    left(b, 1202, 746, "- The road cools fast", 32, face=GRAND, fill=SOFT_RED, weight=650, caps=False, name="Con")
    left(b, 1202, 806, "Equipped on skill 1.", 32, face=GRAND, fill=MAROON, weight=700, caps=False, name="State")
    b.group = "Footer"
    button(b, "go", 1330, 908, 530, 124, "EQUIPPED", 56)
    return b


# Items

SHOES = ["tsinelas", "crocs", "pantulog", "sike", "spartan", "alpombra", "pambahay", "heels", "sandals", "loafers"]


ITEM_SIDE = "gradient"


def item_screen(b):
    ground(b)
    b.group = "Header"
    back(b, 60, 40)
    tabs(b, 220, 40, 560, 110, ["OWNED", "UNOWNED"], 0)
    # The right column needs a calmer field than the weave (owner, 2026-11-01): compared both ways.
    if ITEM_SIDE == "panel":
        dark_panel(b, 1380, 170, 520, 880, seed=37, group="Kinds")
    else:
        b.group = "Kinds"
        fade = Image.new("RGBA", (1020, 1080))
        px = fade.load()
        for fx in range(1020):
            a = int(160 * min(1, fx / 410))  # a long fade, full before the coin
            for fy in range(1080):
                px[fx, fy] = (0x2A, 0x06, 0x10, a)
        b.place("items-side-gradient", fade, 900, 0, "Gradient")
    b.group = "Items"
    for i, n in enumerate(SHOES):
        x, y = 60 + (i % 4) * 280, 190 + (i // 4) * 280
        frame(b, x, y, 260, 260, on=i == 0, name=n.title(), dark=True)
        p = fitted(ink(Image.open(UI / "portraits" / (n + ".png")).convert("RGBA")), 224, 170)
        put_centred(b, "item-" + n, p, x + 130, y + (110 if i == 0 else 130), n.title())
        if i == 0:
            tag(b, x + 30, y + 190, 200, 48, "EQUIPPED")
    chrome(b)
    b.group = "Kinds"
    for y, (label, kind, icon) in zip((200, 340), [("TSINELAS", "go", "r034.png"), ("LATA", "plain", "r037.png")]):
        button(b, kind, 1420, y, 440, 114, label, 48)
        ic = ink(raster("hub-item", icon))
        ic = tint(ic, GREEN_INK if kind == "go" else MAROON)
        ic = ic.resize((int(ic.width * 1.5), int(ic.height * 1.5)), Image.LANCZOS)
        put(b, "kind-" + label.lower(), ic, 1455, y + 114 * FACE_LIP - ic.height / 2, label.title())
    b.group = "Footer"
    text(b, (1420, 796), "TSINELAS", 52, fill=WHITE, stroke=5, stroke_fill=TITLE_EDGE, name="Item")
    button(b, "go", 1420, 900, 440, 130, "EQUIPPED", 58)


def hub_item(board_id):
    b = Board()
    item_screen(b)
    return b


def hub_item_inspect(board_id):
    b = Board()
    item_screen(b)
    scrim(b, (40, 8, 14, 150))
    px, py, pw, ph = 270, 100, 1380, 880
    dark_panel(b, px, py, pw, ph, seed=35, group="Popup: Item")
    b.group = "Popup: Item"
    button(b, "red", px + 40, py + 40, 120, 100, "<", 62)
    left(b, px + 200, py + 78, "TSINELAS", 76, fill=CREAM, name="Heading")
    left(b, px + 204, py + 160, "Tsinelas", 36, face=GRAND, fill=CREAM, weight=600, caps=False, name="Name")
    icon_button(b, "yellow", px + pw - 170, py + 40, 120, 100, "star")
    shoe = ink(Image.open(UI / "portraits/tsinelas.png").convert("RGBA"))
    shoe = shoe.resize((int(shoe.width * 2.2), int(shoe.height * 2.2)), Image.LANCZOS)
    showcase(b, px + 60, py + 230, pw - 120, 440, [(shoe, (pw - 120) / 2, 220)], "item-tsinelas", name="Preview", rays=24)
    icon_button(b, "plain", px + pw - 210, py + 540, 120, 100, "expand")
    button(b, "go", px + pw - 560, py + 730, 500, 118, "EQUIPPED", 56)
    return b


# Shop

BURSTS = [dict(c1=(0xE0, 0x4A, 0x4A), c2=(0xB2, 0x2A, 0x36)), dict(c1=(0xD8, 0xE8, 0x5A), c2=(0xA8, 0xC2, 0x30))]


def hub_shop(board_id):
    b = Board()
    poster(b, HOME, dim=(40, 8, 14, 150))
    px, py, pw, ph = 345, 130, 1230, 815
    dark_panel(b, px, py, pw, ph, seed=36, group="Popup: Shop")
    b.group = "Popup: Shop"
    button(b, "red", px + 45, py + 40, 120, 100, "<", 62)
    left(b, px + 200, py + 78, "SHOP", 76, fill=CREAM, name="Heading")
    left(b, px + 204, py + 160, "Spend TANSAN on heroes and items. Earn it by playing.", 34, face=GRAND, fill=CREAM,
         weight=600, caps=False, name="Hint")
    for i, name in enumerate(["HEROES", "ITEMS"]):
        x, y, w, h = px + 45 + i * 585, py + 230, 555, 540
        if i == 0:
            cast = fitted(ink(raster("hub-shop", "r020.png"), 90), 480, 340)
            layers = [(cast, w / 2, 250)]
        else:
            heels = fitted(Image.open(UI / "portraits/heels.png").convert("RGBA"), 170, 170)
            loafers = fitted(Image.open(UI / "portraits/loafers.png").convert("RGBA"), 170, 170)
            can = fitted(ink(raster("hub-shop", "r024.png"), 90), 210, 300)
            layers = [(heels, 105, 310), (can, w / 2, 240), (loafers, w - 105, 310)]
        showcase(b, x, y, w, h, layers, "shop-" + name.lower(), name=name.title(), burst_h=400, **BURSTS[i])
        centred(b, x + w / 2, y + h - 85, name, 76, fill=CREAM, name="Choice")
    return b


# Tasks

TASKS = {"TODAY": [("Get your tsinelas back 4 times", "+60", "0 / 4"), ("Play 2 matches", "+60", "0 / 2"),
                   ("Tag 2 attackers as the taya", "+70", "0 / 2")],
         "THIS WEEK": [("Knock the lata down 15 times", "+350", "0 / 15"), ("Play 10 matches", "+300", "0 / 10"),
                       ("Win 3 matches", "+400", "0 / 3")]}


def hub_tasks(board_id):
    b = Board()
    ground(b)
    header(b, "TASKS")
    subtitle(b, "EARN TANSAN", y=160)
    chrome(b)
    panel(b, 60, 230, 1800, 720, seed=37)
    b.group = "Tasks"
    coin = fitted(raster("hub-tasks", "r010.png"), 98, 98)
    coin = ink(coin)
    for c, (head_s, rows) in enumerate(TASKS.items()):
        x = 120 + c * 880
        left(b, x, 300, head_s, 52, fill=MAROON, name="Heading")
        for i, (s, reward, prog) in enumerate(rows):
            y = 360 + i * 190
            left(b, x, y + 34, s, fit_size(s, 560, 36, face=GRAND, weight=650, lo=30), face=GRAND, fill=INK, weight=650,
                 caps=False, name="Task")
            put_centred(b, "coin-task", coin, x + 600, y + 34, "TANSAN")
            left(b, x + 640, y + 34, reward, 50, fill=MAROON, name="Reward")
            b.place("track-440x56", slice3(art("login3-tabs-track"), 440, 56), x, y + 82, "Progress")
            left(b, x + 470, y + 110, prog, 32, face=GRAND, fill=INK, weight=600, caps=False, name="Progress")
            if i < 2:
                rule(b, x - 10, x + 770, y + 168)
    b.group = "Footer"
    text(b, (60, 984), "Offline: showing progress from matches on this machine. Sign in to claim.", 32, face=GRAND,
         fill=WHITE, weight=650, name="Note", **EDGE)
    return b


# Picture

AVATARS = ["dante", "sean", "cheska", "zack", "nemu", "phaister", "rafi", "amihan", "paete", "maring", "totoy", "inday",
           "kuya_boy", "ate_girlie", "tikboy", "bebang", "jun_jun", "lola_pacing", "mang_kanor", "aling_nena",
           "tsinelas", "boyben"]


def hub_avatar(board_id):
    b = Board()
    poster(b, HOME, dim=(40, 8, 14, 150))
    px, py, pw, ph = 250, 120, 1420, 840
    dark_panel(b, px, py, pw, ph, seed=38, group="Popup: Picture")
    b.group = "Popup: Picture"
    button(b, "red", px + 40, py + 40, 120, 100, "<", 62)
    left(b, px + 200, py + 78, "PICTURE", 76, fill=CREAM, name="Heading")
    left(b, px + 204, py + 160, "Pick the face your name plate wears.", 34, face=GRAND, fill=CREAM, weight=600,
         caps=False, name="Hint")
    big = Image.open(UI / "avatars/avatar_tikboy.png").convert("RGBA")
    framed(b, px + 60, py + 250, 460, 460, big, "avatar-large-tikboy", on=True, name="Your picture")
    for i, n in enumerate(AVATARS):
        x, y = px + 580 + (i % 6) * 132, py + 230 + (i // 6) * 132
        framed(b, x, y, 116, 116, Image.open(UI / "avatars" / ("avatar_" + n + ".png")).convert("RGBA"), "avatar-" + n,
               on=n == "tikboy", name=n.title())
    return b


SCREENS = {"hub-hero": hub_hero, "hub-ability": hub_ability, "hub-story": hub_story, "hub-skilltree": hub_skilltree,
           "hub-item": hub_item, "hub-item-inspect": hub_item_inspect, "hub-shop": hub_shop, "hub-tasks": hub_tasks,
           "hub-avatar": hub_avatar}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--only", nargs="*", help="capture ids to draw")
    args = parser.parse_args()
    OUT.mkdir(parents=True, exist_ok=True)
    for sid, draw in SCREENS.items():
        if args.only and sid not in args.only:
            continue
        draw(sid).im.convert("RGB").save(OUT / (sid + ".png"))
        print("drew", sid)


if __name__ == "__main__":
    main()
