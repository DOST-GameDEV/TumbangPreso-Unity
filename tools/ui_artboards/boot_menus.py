"""The hub menus in the boot kit: game modes and its three popups, the hub menu (desktop and
touch), credits, connection lost, the toast, the queue strip and Let's Play. Laid out from the
captures in Logs/ui-artboards.

    py -3 tools/ui_artboards/boot_menus.py [--only hub-menu]
"""
import argparse
import json
from pathlib import Path

from PIL import Image

from bootkit import (framed, text, WHITE, TITLE_EDGE, paint_card, BUTTONS, button_art, ROOT, Board, DARUMA, GRAND, MAROON, INK, TAN, TAN_D, WHITE, TITLE_EDGE, CREAM, SOFT_RED, LIME,
                     FACE_LIP, button, centred, frame, ground, head, header, left, modal, panel, para, picture,
                     poster, rule, scrim, text, title, wrap, back, dark_panel, hazard_art)

CAPTURES = ROOT / "Logs" / "ui-artboards"
UI = ROOT / "Assets/TumbangPreso/Resources/UI"
OUT = ROOT / "ArtSource/ui/revamp-2026-10-30-boot/preview"
HOME = CAPTURES / "hub-home" / "reference.png"


def raster(board_id, name_or_file):
    return CAPTURES / board_id / name_or_file


def tinted(path, rgb, height=None):
    """A game icon repainted in one colour, cropped to its ink, optionally scaled to a height."""
    im = Image.open(path).convert("RGBA")
    box = im.getchannel("A").getbbox()
    im = im.crop(box)
    if height:
        im = im.resize((max(1, round(im.width * height / im.height)), height), Image.LANCZOS)
    out = Image.new("RGBA", im.size, tuple(rgb) + (255,))
    out.putalpha(im.getchannel("A"))
    return out


def icon(b, path, key, cx, cy, rgb, height=None):
    """A tinted icon, centred on a point."""
    im = tinted(path, rgb, height)
    b.place("%s-%02x%02x%02x-%d" % (key, rgb[0], rgb[1], rgb[2], im.height), im, cx - im.width / 2, cy - im.height / 2,
            "Icon " + key)


def edge_label(b, xy, s, size, face=GRAND, weight=650, name="Label"):
    text(b, xy, s, size, face=face, fill=WHITE, weight=weight if face == GRAND else None, stroke=max(3, size // 12),
         stroke_fill=TITLE_EDGE, name=name)


def focus_picture(b, x, y, w, h, path, key, focus, cut=22):
    """Like the kit's picture, but the crop window slides to `focus` (0 top, 1 bottom)."""
    from PIL import ImageDraw
    p = Image.open(path).convert("RGBA")
    s = max(w / p.width, h / p.height)
    p = p.resize((max(w, round(p.width * s)), max(h, round(p.height * s))), Image.LANCZOS)
    left_, top = (p.width - w) // 2, int((p.height - h) * focus)
    p = p.crop((left_, top, left_ + w, top + h))
    m = Image.new("L", (w, h))
    ImageDraw.Draw(m).polygon([(cut, 0), (w, 0), (w, h), (0, h), (0, cut)], fill=255)
    out = Image.new("RGBA", (w, h))
    out.paste(p, (0, 0), m)
    b.place(key, out, x, y, "Picture")


# The HOME top bar: wallet and menu, kept on every GAMEMODE screen.

def wallet(b):
    b.group = "Wallet"
    frame(b, 1290, 44, 330, 92, name="Wallet")
    coin = Image.open(UI / "handmade-lobby/icon_coin.png").convert("RGBA")
    coin.thumbnail((84, 84), Image.LANCZOS)
    b.place("coin", coin, 1308, 48 + (92 - coin.height) // 2, "Coin")
    left(b, 1408, 92, "000,000", 42, fill=CREAM, name="Balance")
    button(b, "yellow", 1640, 40, 100, 92, "+", 54)
    button(b, "red", 1760, 40, 100, 92, "", 40)
    glyph = Image.open(UI / "handmade-lobby/glyph_menu.png").convert("RGBA")
    glyph.thumbnail((58, 58), Image.LANCZOS)
    b.place("glyph-menu", glyph, 1810 - glyph.width / 2 - 1, 40 + 92 * FACE_LIP - glyph.height / 2, "Menu")


# Game modes

MODE_CARDS = [  # (art file, label, x, y, w, h, paint): the same colours as Let's Play's routes
    ("PracticeCard", "PRACTICE", 120, 190, 520, 400, "blue"),
    ("CustomCard", "CUSTOM", 120, 630, 520, 400, "go"),
    ("ClassicCard", "CLASSIC", 700, 190, 520, 840, "red"),
    ("RankedCard", "RANKED", 1280, 190, 520, 840, "yellow"),
]


# Deep enough to read on the cards' darker fill.
CARD_INK = {"blue": (0x05, 0x22, 0x38), "go": (0x16, 0x33, 0x06), "yellow": (0x52, 0x1E, 0x04), "red": CREAM}


def modes_screen(b):
    ground(b)
    header(b, "GAMEMODE")
    b.group = "Header"
    # The tutorial lives inside PRACTICE only (owner, 2026-11-02): no shortcut up here.
    wallet(b)
    for art, label, x, y, w, h, kind in MODE_CARDS:
        b.group = "Mode " + label.title()
        paint_card(b, x, y, w, h, kind, name=label.title(), pattern="checker")
        focus_picture(b, x + 24, y + 24, w - 48, h - 24 - 90, UI / "mode-cards" / (art + ".png"), "mode-" + art.lower() + "-%d" % h, .72 if label == "PRACTICE" else .5)
        centred(b, x + w / 2, y + h - 44, label, 60, fill=CARD_INK[kind], name="Mode")


def hub_modes(board_id):
    b = Board()
    modes_screen(b)
    return b


def sub_popup(b, heading, line, h):
    """The shared shell of the three mode popups: back arrow, centred heading, one sentence."""
    scrim(b, (40, 8, 14, 130))
    x, y = (1920 - 1230) // 2, (1080 - h) // 2 - 10
    dark_panel(b, x, y, 1230, h, seed=12, group="Popup: " + heading.title())
    centred(b, 960, y + 90, heading, 60, fill=CREAM, name="Heading")
    b.group = "Popup"
    button(b, "red", x + 40, y + 34, 110, 92, "<", 56)
    centred(b, 960, y + 160, line, 34, face=GRAND, fill=TAN, weight=600, name="Line")
    return x, y


def choice_card(b, x, y, w, h, art, label, kind=None):
    """A popup's option: painted when given a colour, else the regular berry card."""
    if kind:
        paint_card(b, x, y, w, h, kind, name=label.title())
    else:
        frame(b, x, y, w, h, name=label.title())
    picture(b, x + 24, y + 24, w - 48, int((w - 48) * 672 / 960), UI / "mode-cards" / (art + ".png"), "choice-" + art.lower())
    centred(b, x + w / 2, y + h - 48, label, 56, fill=CARD_INK[kind] if kind else CREAM, name="Choice")


def hub_modes_classic(board_id):
    b = Board()
    modes_screen(b)
    x, y = sub_popup(b, "CLASSIC", "Casual matchmaking. Which game?", 720)
    choice_card(b, x + 55, y + 200, 540, 450, "ClassicChoice", "CLASSIC", "red")
    choice_card(b, x + 635, y + 200, 540, 450, "HeroStrikeChoice", "HERO STRIKE", "yellow")
    return b


def hub_modes_practice(board_id):
    b = Board()
    modes_screen(b)
    x, y = sub_popup(b, "PRACTICE", "Offline, no match rewards.", 720)
    choice_card(b, x + 55, y + 200, 540, 450, "TutorialChoice", "TUTORIAL")
    choice_card(b, x + 635, y + 200, 540, 450, "TrainingChoice", "TRAINING")
    # PRACTICE's own RULES, re-homed from the hamburger's MATCH RULES (owner, 2026-11-02).
    menu_row(b, x + 1230 - 300, y + 44, 260, 92, "RULES", "r031.png", 38, 40)
    return b


def hub_modes_custom(board_id):
    b = Board()
    modes_screen(b)
    x, y = sub_popup(b, "CUSTOM", "Your own room.", 660)
    cards = [("HOST", "r031.png", "Open a room and set up the match.", "HOST ROOM"),
             ("JOIN", "r037.png", "Type a code, or pick a room near you or online.", "JOIN ROOM")]
    for i, (name, ic, desc, act) in enumerate(cards):
        cx, cy = x + 55 + i * 580, y + 200
        b.group = name.title()
        frame(b, cx, cy, 540, 410, name=name.title())
        # One left edge: icon and name on a line, the sentence under them, the action at the foot.
        icon(b, raster("hub-modes-custom", ic), "custom-" + name.lower(), cx + 92, cy + 76, TAN, 80)
        left(b, cx + 150, cy + 76, name, 66, fill=CREAM, name="Heading")
        para(b, cx + 50, cy + 142, wrap(desc, 32, 440, weight=600), 32, 44, fill=CREAM, weight=600)
        button(b, "yellow", cx + 36, cy + 278, 468, 104, act, 46)
    return b


# The hub menu

# Four doors only (owner, 2026-11-02): career and replays moved into the player profile, the tutorial is
# inside PRACTICE, PRACTICE has its own RULES, and BACK TO TITLE is gone. EXIT GAME is drawn apart, in red.
MENU_ROWS = [("SETTINGS", "r019.png"), ("FRIENDS", "r022.png"), ("CREDITS", "r037.png")]


MENU_LIGHT = True  # cream buttons on the dark panel, their slab kept quiet (owner, 2026-11-01)


def menu_row(b, x, y, w, h, label, file, size, ic):
    if MENU_LIGHT:
        ink = BUTTONS["plain-soft"][1]
        b.place("menu-light-%dx%d" % (w, h), button_art("plain-soft", w, h), x, y, label.title())
        icon(b, raster("hub-menu", file), "menu-light-" + file, x + 30 + ic // 2, y + h * FACE_LIP, ink, ic)
        left(b, x + 40 + ic + 10, y + h * FACE_LIP, label, size, fill=ink, name="Item")
        return
    frame(b, x, y, w, h, name=label.title())
    icon(b, raster("hub-menu", file), "menu-" + file, x + 30 + ic // 2, y + h / 2, CREAM, ic)
    left(b, x + 40 + ic + 10, y + h / 2, label, size, fill=CREAM, name="Item")


def hub_menu(board_id):
    b = Board()
    poster(b, HOME, dim=(40, 8, 14, 150))
    dark_panel(b, 1330, 30, 570, 590, seed=31, group="Menu")
    left(b, 1380, 112, "MENU", 70, fill=CREAM, name="Heading")
    button(b, "red", 1744, 58, 116, 100, "X", 50)
    for i, (label, file) in enumerate(MENU_ROWS):
        menu_row(b, 1380, 190 + i * 90, 480, 74, label, file, 38, 46)
    button(b, "red", 1380, 470, 480, 100, "EXIT GAME", 44)
    icon(b, raster("hub-menu", "r043.png"), "quit-x", 1430, 470 + 100 * FACE_LIP, (0x6A, 0x0C, 0x16), 40)
    return b


def hub_menu_touch(board_id):
    b = Board()
    poster(b, HOME, dim=(40, 8, 14, 150))
    dark_panel(b, 510, 110, 900, 845, seed=32, group="Menu")
    left(b, 565, 200, "MENU", 80, fill=CREAM, name="Heading")
    button(b, "red", 1235, 132, 130, 110, "X", 60)
    for i, (label, file) in enumerate(MENU_ROWS):
        menu_row(b, 565, 310 + i * 150, 790, 120, label, file, 52, 68)
    button(b, "red", 565, 780, 790, 120, "EXIT GAME", 52)
    icon(b, raster("hub-menu", "r043.png"), "quit-x", 625, 780 + 120 * FACE_LIP, (0x6A, 0x0C, 0x16), 48)
    return b


# Credits

TEAM = [("MATTHEW LABRADOR", "Lead Developer  ·  UI Designer  ·  3D Asset Editor  ·  Poster Design"),
        ("PAUL RECIO", "Developer  ·  Video Editor"),
        ("HARRY GOMEZ", "Composer, Original Soundtrack  ·  Logo & UI Design  ·  Game Voice Over"),
        ("CLARENCE PAGADUAN", "UI Designer  ·  Game Asset Artist  ·  Audio Composer"),
        ("HANS XAVIER LAO", "QA Tester & Validation  ·  Administration  ·  Cinematics Director")]


def hub_credits(board_id):
    b = Board()
    ground(b)
    b.group = "Header"
    button(b, "red", 60, 40, 120, 100, "<", 62)
    logo = Image.open(UI / "brand/tump_logo.png").convert("RGBA")
    logo.thumbnail((440, 330), Image.LANCZOS)
    b.place("credits-logo", logo, 60 + (480 - logo.width) // 2, 300, "Logo")
    text(b, (60 + (480 - 0) / 2 - 150, 690), "CREDITS", 96, fill=WHITE, stroke=8, stroke_fill=TITLE_EDGE, name="Title")
    panel(b, 640, 50, 1220, 970, seed=33, group="Credits")
    head(b, 710, 140, "THE TEAM", 40)
    for i, (name, role) in enumerate(TEAM):
        y = 230 + i * 150
        left(b, 710, y, name, 48, fill=MAROON, name="Name")
        left(b, 712, y + 62, role, 30, face=GRAND, weight=600, caps=False, name="Role")
        if i < len(TEAM) - 1:
            rule(b, 700, 1740, y + 112)
    b.group = "Scroll"
    from PIL import ImageDraw
    track = Image.new("RGBA", (16, 820))
    ImageDraw.Draw(track).rounded_rectangle((0, 0, 15, 819), 8, fill=TAN)
    thumb = Image.new("RGBA", (16, 230))
    ImageDraw.Draw(thumb).rounded_rectangle((0, 0, 15, 229), 8, fill=SOFT_RED)
    b.place("scroll-track", track, 1790, 125, "Scroll track")
    b.place("scroll-thumb", thumb, 1790, 125, "Scroll thumb")
    return b


# Connection lost, toast, queue

WARN, WARN_D, WARN_INK = (0xF6, 0xBC, 0x3A), (0xC2, 0x6A, 0x12), (0x2A, 0x10, 0x14)


def tape_art(key, w, h, angle):
    """A strip of warning tape in the yellow button's paint: hand-cut stripes, wandering edges, a
    slight sag along its length and zigzag ends."""
    import math
    import random
    from PIL import ImageDraw
    S, teeth = 3, 6
    pad = 12 * S
    W, H = w * S, h * S
    rnd = random.Random(key)
    im = Image.new("RGBA", (W, H + pad * 2), (0xF6, 0xBC, 0x3A, 255))
    d = ImageDraw.Draw(im)
    band, x = 44 * S, -H
    while x < W + H:
        lean = rnd.uniform(-5, 5) * S
        wide = band * rnd.uniform(.85, 1.15)
        d.polygon([(x, H + pad * 2), (x + H + pad * 2 + lean, 0), (x + H + pad * 2 + wide + lean, 0), (x + wide, H + pad * 2)],
                  fill=(0xA8, 0x4E, 0x0C, 255))
        x += band * 2
    edge = lambda: [rnd.uniform(-3.2, 3.2) * S for _ in range(W // (22 * S) + 2)]
    upper, lower = edge(), edge()
    at = lambda vals, px: vals[int(px / (22 * S))] + (vals[int(px / (22 * S)) + 1] - vals[int(px / (22 * S))]) * ((px / (22 * S)) % 1)
    lip, slab = 7 * S, 10 * S
    # Bands along each wandering edge: the lit lip on top, the slab below.
    for px in range(0, W, S):
        t = pad + at(upper, px)
        u = pad + H + at(lower, px)
        d.rectangle((px, t, px + S, t + lip), fill=(0xFF, 0xDE, 0x7A, 255))
        d.rectangle((px, u - slab, px + S, u), fill=(0xC2, 0x6A, 0x12, 255))
    # The outline: wandering long edges, pinked ends, each tooth a little different.
    depth, step = 15 * S, H / teeth
    outline = [(px, pad + at(upper, px)) for px in range(depth, W - depth, 6 * S)]
    for t in range(teeth):
        outline += [(W - rnd.uniform(0, 4 * S), pad + (t + .5) * step), (W - depth + rnd.uniform(-2, 2) * S, pad + (t + 1) * step)]
    outline += [(px, pad + H + at(lower, px)) for px in range(W - depth, depth, -6 * S)]
    for t in range(teeth):
        outline += [(rnd.uniform(0, 4 * S), pad + H - (t + .5) * step), (depth + rnd.uniform(-2, 2) * S, pad + H - (t + 1) * step)]
    mask = Image.new("L", im.size)
    ImageDraw.Draw(mask).polygon(outline, fill=255)
    im.putalpha(mask)
    # The sag: slide each column along a slow curve.
    sagged = Image.new("RGBA", im.size)
    phase = rnd.uniform(0, 6.28)
    for px in range(0, W, S):
        dy = int((math.sin(px / W * math.pi) - .5) * 9 * S + math.sin(px / W * 9 + phase) * 2 * S)
        sagged.paste(im.crop((px, 0, px + S, im.height)), (px, dy))
    r = sagged.rotate(angle, expand=True, resample=Image.BICUBIC)
    return r.resize((r.width // S, r.height // S), Image.LANCZOS)


def tape(b, key, cx, cy, w, h, angle):
    r = tape_art(key, w, h, angle)
    b.place("%s-%d" % (key, w), r, cx - r.width / 2, cy - r.height / 2, "Warning tape")


def warning_art(size):
    """A warning triangle in the yellow button's paint: an organic shape, its slab swept straight
    down from the body so the two stay joined, and a broad lit edge along the top."""
    from PIL import ImageChops, ImageDraw, ImageFilter
    from bootkit import wobble
    S = 3
    drop, lip = int(size * .1) * S, int(size * .06) * S
    tall = int(size * .9) * S
    w, h = size * S, tall + drop + 8 * S
    corner = int(size * .1) * S
    pts = [(w / 2, corner), (w - corner, tall - corner), (corner, tall - corner)]
    mask = Image.new("L", (w, h))
    ImageDraw.Draw(mask).polygon(wobble(pts, 2.6 * S, size), fill=255)
    # Grow and blur the wobbled triangle, then threshold it: soft, hand-cut corners.
    body = mask.filter(ImageFilter.GaussianBlur(corner * .62)).point(lambda v: 255 if v > 70 else 0)
    slab = body
    for dy in range(S, drop + 1, S):
        slab = ImageChops.lighter(slab, ImageChops.offset(body, 0, dy))
    face = ImageChops.multiply(body, ImageChops.offset(body, 0, lip))
    im = Image.new("RGBA", (w, h))
    im.paste((0xC2, 0x6A, 0x12, 255), (0, 0), slab)
    im.paste((0xFF, 0xE4, 0x8A, 255), (0, 0), body)
    im.paste((0xF6, 0xBC, 0x3A, 255), (0, 0), face)
    return im.resize((w // S, h // S), Image.LANCZOS)


def warning_mark(b, cx, top, size):
    im = warning_art(size)
    b.place("warn-mark-%d" % size, im, cx - im.width / 2, top, "Warning mark")
    centred(b, cx, top + size * 0.57, "!", int(size * 0.5), fill=(0xA8, 0x4E, 0x0C), name="Bang")


def hub_lost(board_id):
    b = Board()
    poster(b, HOME, dim=None)
    scrim(b, (20, 4, 8, 190))
    x, y, w, h = 490, 270, 940, 560
    dark_panel(b, x, y, w, h, seed=41, group="Popup: Connection Lost")
    tape(b, "tape-a", x + 30, y + 26, 300, 62, 36)
    tape(b, "tape-b", x + w - 30, y + h - 14, 300, 62, 36)
    b.group = "Popup: Connection Lost"
    warning_mark(b, 960, y - 120, 210)
    centred(b, 960, y + 150, "CONNECTION LOST", 68, fill=WARN, name="Heading")
    centred(b, 960, y + 245, "The room connection ended.", 40, face=GRAND, fill=CREAM, weight=700, name="Summary")
    centred(b, 960, y + 305, "The host left the room.", 34, face=GRAND, fill=TAN, weight=600, name="Reason")
    button(b, "go", 680, y + 370, 560, 128, "BACK TO HOME", 54)
    return b


def hub_toast(board_id):
    b = Board()
    poster(b, HOME, dim=None)
    b.group = "Toast"
    frame(b, 470, 836, 980, 100, name="Toast")
    centred(b, 960, 886, "Room code copied", 40, face=GRAND, fill=CREAM, weight=600, name="Words")
    return b


def ticket_art(w, h, stub):
    """A raffle ticket in the yellow button's paint: bitten ends, a torn line before the stub, a lit
    top edge and a slab."""
    from PIL import ImageChops, ImageDraw, ImageFilter
    from bootkit import wobble
    S, drop, lip = 3, 12, 6
    W, H = w * S, (h + drop) * S
    body = Image.new("L", (W, H))
    d = ImageDraw.Draw(body)
    d.polygon(wobble([(0, 0), (W, 0), (W, h * S), (0, h * S)], 1.4 * S, 7), fill=255)
    bite = h * S * .2
    for px in (0, W):
        d.ellipse((px - bite, h * S / 2 - bite, px + bite, h * S / 2 + bite), fill=0)
    x = W - stub * S
    for cy in (0, h * S):
        d.ellipse((x - 9 * S, cy - 9 * S, x + 9 * S, cy + 9 * S), fill=0)
    body = body.filter(ImageFilter.GaussianBlur(2 * S)).point(lambda v: 255 if v > 110 else 0)
    slab = body
    for dy in range(S, drop * S + 1, S):
        slab = ImageChops.lighter(slab, ImageChops.offset(body, 0, dy))
    face = ImageChops.multiply(body, ImageChops.offset(body, 0, lip * S))
    im = Image.new("RGBA", (W, H))
    im.paste((0xC2, 0x6A, 0x12, 255), (0, 0), slab)
    im.paste((0xFF, 0xDE, 0x7A, 255), (0, 0), body)
    im.paste((0xF6, 0xBC, 0x3A, 255), (0, 0), face)
    d = ImageDraw.Draw(im)
    for y in range(16 * S, (h - 12) * S, 15 * S):
        d.rectangle((x - 2 * S, y, x + 2 * S, y + 8 * S), fill=(0xC2, 0x6A, 0x12, 255))
    return im.resize((w, h + drop), Image.LANCZOS)


def hub_queue(board_id):
    """The search as a raffle ticket, its cancel torn off on the stub."""
    b = Board()
    poster(b, CAPTURES / "hub-queue" / "reference.png", dim=None)
    b.group = "Queue"
    w, h, stub, x, y = 600, 116, 150, 660, 20
    b.place("queue-ticket", ticket_art(w, h, stub), x, y, "Ticket")
    ink = (0x4A, 0x10, 0x18)
    main = x + 20 + (w - stub - 20) / 2
    centred(b, main, y + 44, "00:02", 70, fill=ink, name="Timer")
    centred(b, main, y + 92, "HERO STRIKE · SEARCHING", 28, face=GRAND, fill=(0x8A, 0x3A, 0x0C), weight=700, name="Queue line")
    # Centred on the stub between the tear line and the bite in its end, and on the painted face.
    button(b, "red", x + w - stub + (stub - 20) / 2 - 50, y + 61 - 72 * FACE_LIP, 100, 72, "X", 38)
    return b


# Let's play

ROUTES = [("WITH BOTS", "Offline match", "r006.png"), ("WITH FRIENDS", "Create or join", "r008.png"),
          ("RANKED", "Online. Sign in.", "r010.png")]


def hub_letsplay(board_id):
    b = Board()
    ground(b)
    b.group = "Header"
    back(b, 60, 40)
    title(b, 200, 40 + 100 * FACE_LIP, "LET'S PLAY")
    logo = Image.open(UI / "brand/tump_logo.png").convert("RGBA")
    logo.thumbnail((250, 190), Image.LANCZOS)
    b.place("play-logo", logo, 1860 - logo.width, 30, "Logo")
    for i, (name, detail, art, on, focus) in enumerate([("CLASSIC", "Eight rounds. No powers.", "ClassicChoice", False, 0.75),
                                                        ("HERO STRIKE", "Eight rounds. Six heroes.", "HeroStrikeChoice", True, 0.62)]):
        x, y = 60 + i * 930, 190
        b.group = "Mode " + name.title()
        dark_panel(b, x, y, 870, 460, seed=50 + i, group="Mode " + name.title(), on=on)
        left(b, x + 48, y + 66, name, 76, fill=LIME if on else CREAM, name="Mode")
        left(b, x + 50, y + 134, detail, 38, face=GRAND, fill=CREAM, weight=600, caps=False, name="Details")
        focus_picture(b, x + 24, y + 176, 822, 260, UI / "mode-cards" / (art + ".png"), "play-" + art.lower(), focus)
        if on:
            icon(b, raster("hub-letsplay", "r004.png"), "check", x + 800, y + 80, LIME, 64)
    b.group = "Routes"
    edge_label(b, (60, 690), "PLAY YOUR WAY", 40, face=DARUMA, name="Routes")
    for i, (name, detail, file) in enumerate(ROUTES):
        x, y = 60 + i * 610, 750
        kind = ("blue", "go", "yellow")[i]
        ink = BUTTONS[kind][1]
        b.place("route-button-%s" % kind, button_art(kind, 580, 170), x, y, name.title())
        icon(b, raster("hub-letsplay", file), "route-%s" % kind, x + 80, y + 62, ink, 56)
        left(b, x + 130, y + 50, name, 44, fill=ink, name="Route")
        left(b, x + 132, y + 92, detail, 30, face=GRAND, fill=ink, weight=700, caps=False, name="Details")
    b.group = "Footer"
    button(b, "yellow", 60, 950, 440, 100, "LEARN TO PLAY", 44)
    edge_label(b, (540, 975), "Choose a mode, then a way to play.", 34, name="Hint")
    return b


SCREENS = {"hub-modes": hub_modes, "hub-modes-classic": hub_modes_classic, "hub-modes-custom": hub_modes_custom,
           "hub-modes-practice": hub_modes_practice, "hub-menu": hub_menu, "hub-menu-touch": hub_menu_touch,
           "hub-credits": hub_credits, "hub-lost": hub_lost, "hub-toast": hub_toast, "hub-queue": hub_queue,
           "hub-letsplay": hub_letsplay}


GAME_KIT = ROOT / "Assets/TumbangPreso/Resources/UI/boot-kit"


def export_game():
    """Textures the menus need that no kit piece covers: warning tape, the warning mark and the
    queue ticket, written beside the kit (`HubKnit.Menus.cs` and its users load them)."""
    import uuid
    from bootkit import ART
    pieces = {"warning-tape": tape_art("tape-a", 300, 62, 0), "warning-mark": warning_art(210),
              "queue-ticket": ticket_art(600, 116, 150)}
    template = (ART / "login3-primary.png.meta").read_text(encoding="utf-8")
    for name, im in pieces.items():
        target = GAME_KIT / (name + ".png")
        im.save(target)
        meta = target.with_suffix(".png.meta")
        if not meta.exists():
            lines = template.splitlines()
            lines[1] = "guid: " + uuid.uuid4().hex
            meta.write_text(chr(10).join(lines) + chr(10), encoding="utf-8")
    print("game menus:", ", ".join(sorted(pieces)))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--game", action="store_true", help="write the menu textures into Resources/UI/boot-kit")
    parser.add_argument("--only", nargs="*", help="draw only these capture ids")
    args = parser.parse_args()
    if args.game:
        export_game()
        return
    OUT.mkdir(parents=True, exist_ok=True)
    for board_id, draw in SCREENS.items():
        if args.only and board_id not in args.only:
            continue
        draw(board_id).im.convert("RGB").save(OUT / (board_id + ".png"))
        print("drew", board_id)
    if not args.only or "hub-letsplay" in args.only:
        pass


if __name__ == "__main__":
    main()
