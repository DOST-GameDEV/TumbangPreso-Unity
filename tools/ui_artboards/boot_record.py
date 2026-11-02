"""The Record, Replays and Match flow screens in the boot kit: the player record (five tabs),
local replays, the result sheet (four views plus the next-court vote), loading, the round
break and halftime. Laid out from the captures in Logs/ui-artboards.

    py -3 tools/ui_artboards/boot_record.py [--only hub-replays,match-break]

Previews land in ArtSource/ui/revamp-2026-10-30-boot/preview/<id>.png.
"""
import math
import argparse
import json
from pathlib import Path

from PIL import Image, ImageDraw

from bootkit import (BUTTONS, button_art, framed, sunburst_art, dark_panel, sunburst, hazard_art, WELL_RIM,
                     ROOT, Board, DARUMA, GRAND, MAROON, INK, TAN, TAN_D, WHITE, TITLE_EDGE, CREAM, LIME, WELL,
                     SOFT_RED, FACE_LIP,
                     back, button, centred, field, frame, ground, header, left, panel, para, picture, poster, right,
                     rule, scrim, tabs, tag, text, title, well, wrap, font, ps_name, slice3, art)

CAPTURES = ROOT / "Logs" / "ui-artboards"
UI = ROOT / "Assets/TumbangPreso/Resources/UI"
OUT = ROOT / "ArtSource/ui/revamp-2026-10-30-boot/preview"


def capture(board_id):
    return json.loads((CAPTURES / board_id / "manifest.json").read_text(encoding="utf-8"))


def raster(board_id, name_or_file):
    """A raster layer of a capture, by file name."""
    return Image.open(CAPTURES / board_id / name_or_file).convert("RGBA")


# Shared small pieces

def edge(b, x, cy, s, size, face=DARUMA, weight=None, align="l", stroke=4, name="Label"):
    """White text with the berry edge, for labels on the weave or the world. x is the left edge,
    the centre or the right edge by `align`; the caps sit on cy."""
    f = font(face, size, weight)
    l, t, r, bt = f.getbbox(s, stroke_width=stroke)
    hh = f.getbbox("H")
    ox = {"l": x - l, "c": x - (l + r) / 2, "r": x - r}[align]
    b.write((ox, cy - (hh[1] + hh[3]) / 2), s, f, WHITE, ps_name(face, weight), size, stroke=stroke, edge=TITLE_EDGE,
            align={"l": "Left", "c": "Center", "r": "Right"}[align], name=name)


def chamfer_piece(w, h, color, c=14):
    im = Image.new("RGBA", (w, h))
    ImageDraw.Draw(im).polygon([(c, 0), (w, 0), (w, h - c), (w - c, h), (0, h), (0, c)], fill=tuple(color) + (255,))
    return im


def face(b, name, cx, cy, size, folder="portraits"):
    """A hero portrait from the game, cropped to its ink and centred on (cx, cy)."""
    p = Image.open(UI / folder / (name + ".png")).convert("RGBA")
    p = p.crop(p.getchannel("A").getbbox())
    p.thumbnail((size, size), Image.LANCZOS)
    b.place("portrait-%s-%d" % (name, size), p, cx - p.width / 2, cy - p.height / 2, name.title())


def chip(b, x, y, s, color, hero):
    """A seat colour square with the hero on it, as the game's swatches."""
    b.place("swatch-%02x%02x%02x-%d" % (tuple(color) + (s,)), chamfer_piece(s, s, color, 18), x, y, "Seat colour")
    face(b, hero, x + s / 2, y + s / 2 + 2, int(s * 0.86))


def scrollbar(b, x, y0, y1, frac):
    w, h = 26, y1 - y0
    im = Image.new("RGBA", (w, h))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((0, 0, w - 1, h - 1), radius=13, fill=WELL + (255,))
    d.rounded_rectangle((3, 3, w - 4, max(30, int(h * frac)) - 3), radius=10, fill=TAN + (255,))
    b.place("scroll-%d-%d" % (h, int(frac * 100)), im, x, y0, "Scrollbar")


def ring(b, cx, cy, s, color=LIME):
    im = Image.new("RGBA", (s, s))
    d = ImageDraw.Draw(im)
    d.ellipse((4, 4, s - 5, s - 5), outline=tuple(color) + (255,), width=10)
    b.place("ring-%d" % s, im, cx - s / 2, cy - s / 2, "Timer ring")


def sub(b, x, y, s):
    """A line under a title, white with the berry edge."""
    text(b, (x, y), s, 34, face=GRAND, fill=WHITE, weight=650, stroke=3, stroke_fill=TITLE_EDGE, name="Subtitle")


# The player record

TABS = ["PROFILE", "FRIENDS", "CAREER", "HISTORY", "ACCOUNT"]


def record_head(b):
    """Back, the avatar, the handle and level. Same on all five tabs."""
    back(b, 60, 40)
    b.group = "Header"
    frame(b, 210, 30, 156, 156, name="Avatar")
    face(b, "dante", 288, 114, 128)
    title(b, 400, 85, "Player#6261", 80)
    sub(b, 402, 140, "Saved on this device \u00b7 no username yet")
    b.group = "Level"
    edge(b, 1860, 70, "LEVEL 1", 56, align="r", stroke=5, name="Level")
    edge(b, 1860, 134, "0 / 1000 XP", 34, face=GRAND, weight=650, align="r", stroke=3, name="XP")
    b.place("track-xp", slice3(art("login3-tabs-track"), 460, 50), 1400, 164, "XP bar")


def record_frame(b, on, heading):
    ground(b)
    record_head(b)
    b.group = "Tabs"
    tabs(b, 60, 222, 1800, 112, TABS, on)
    panel(b, 60, 346, 1800, 550, seed=30 + on)
    left(b, 120, 400, heading, 54, fill=MAROON, name="Heading")


def disclosure(b, x, y, w, h, head, hint):
    """A '+' row that opens a group: a berry card with its title and a hint."""
    frame(b, x, y, w, h, name=head.title())
    left(b, x + 40, y + h * 0.34, "+ " + head, 38, fill=CREAM, name="Group")
    left(b, x + 42, y + h * 0.72, hint, 30, face=GRAND, fill=TAN, weight=600, caps=False, name="Hint")


def profile(board_id):
    b = Board()
    record_frame(b, 0, "PROFILE")
    left(b, 120, 520, "DISPLAY NAME", 40, fill=MAROON, name="Label")
    field(b, 1000, 470, 800, 100, "Player", size=40, weight=600)
    disclosure(b, 120, 600, 1680, 116, "OPTIONAL DETAILS", "Optional and public on your career page.")
    disclosure(b, 120, 736, 1680, 116, "BANNER", "Play to earn titles, badges, borders and palettes.")
    b.group = "Footer"
    button(b, "go", 1450, 924, 410, 112, "SAVE", 54)
    return b


def career(board_id):
    b = Board()
    record_frame(b, 2, "CAREER")
    left(b, 120, 480, "MODE", 40, fill=MAROON, name="Label")
    field(b, 1000, 432, 720, 96, "HERO STRIKE", chevron=True, face=DARUMA, size=40)
    left(b, 120, 570, "Your first completed match starts your career here.", 36, face=GRAND, weight=500, caps=False,
         name="Note")
    frame(b, 120, 612, 1600, 112, name="Ready")
    left(b, 160, 668, "READY WHEN YOU ARE", 40, fill=CREAM, name="Group")
    button(b, "go", 1420, 630, 260, 90, "PLAY", 46)
    disclosure(b, 120, 748, 1600, 116, "HERO MASTERY", "Each hero has their own progression.")
    scrollbar(b, 1762, 434, 860, 0.76)
    return b


def history(board_id):
    b = Board()
    record_frame(b, 3, "MATCH HISTORY")
    left(b, 120, 490, "Recent matches, newest first. Select a match for its scorecard.", 36, face=GRAND, weight=500,
         caps=False, name="Note")
    well(b, 120, 550, 1680, 290, seed=51)
    centred(b, 960, 700, "No matches recorded yet. Your first completed match appears here.", 36, face=GRAND,
            fill=CREAM, weight=600, name="Empty")
    b.group = "Footer"
    edge(b, 60, 980, "Local career", 34, face=GRAND, weight=650, stroke=3, name="Source")
    button(b, "go", 1450, 924, 410, 112, "REFRESH", 50)
    return b


def friends(board_id):
    b = Board()
    record_frame(b, 1, "FRIENDS")
    disclosure(b, 120, 440, 1680, 130, "FIND A FRIEND", "Enter their exact NAME#TAG.")
    well(b, 120, 600, 1680, 250, seed=52)
    centred(b, 960, 725, "No friends yet. Add someone by their tag or from the results after a match.", 36, face=GRAND,
            fill=CREAM, weight=600, name="Empty")
    b.group = "Footer"
    edge(b, 60, 980, "Presence updates about once a minute while the game is open.", 32, face=GRAND, weight=650,
         stroke=3, name="Note")
    button(b, "go", 1450, 924, 410, 112, "REFRESH", 50)
    return b


def account(board_id):
    b = Board()
    record_frame(b, 4, "ACCOUNT")
    frame(b, 120, 432, 1600, 124, name="Status")
    left(b, 160, 478, "STATUS", 40, fill=CREAM, name="Group")
    left(b, 162, 526, "Online sign-in is unavailable here. Local multiplayer still works.", 32, face=GRAND, fill=TAN,
         weight=600, caps=False, name="Hint")
    tag(b, 1440, 452, 240, 60, "LOCAL ONLY", "plain")
    frame(b, 120, 576, 1600, 124, name="Keep")
    left(b, 160, 622, "KEEP THIS PROGRESS", 40, fill=CREAM, name="Group")
    left(b, 162, 670, "Your current progress stays with you.", 32, face=GRAND, fill=TAN, weight=600, caps=False,
         name="Hint")
    button(b, "yellow", 1330, 596, 350, 90, "SET UP SIGN IN", 36)
    disclosure(b, 120, 720, 1600, 124, "TOURNAMENT GUEST", "Let somebody else play without changing your account.")
    scrollbar(b, 1762, 434, 850, 0.66)
    return b


# Replays

REPLAYS = [("Oct 31 \u00b7 04:47", "TONDO", "Classic", "0:03"), ("Oct 31 \u00b7 04:47", "TONDO", "Classic", "0:00"),
           ("Oct 31 \u00b7 04:46", "TONDO", "HeroStrike", "0:03"), ("Oct 31 \u00b7 04:46", "TONDO", "HeroStrike", "0:00"),
           ("Oct 31 \u00b7 04:46", "TONDO", "Classic", "0:03")]


def replays(board_id):
    b = Board()
    ground(b)
    header(b, "LOCAL REPLAYS")
    sub(b, 60, 146, "45 local replays \u00b7 page 1 / 9")
    b.group = "Folder"
    field(b, 60, 214, 1130, 100, "C:/Users/StarX/AppData/LocalLow/BH Studios/Tumbang Preso\\profile", face=DARUMA,
          size=30)
    button(b, "yellow", 1210, 222, 320, 92, "USE FOLDER", 38)
    button(b, "yellow", 1550, 222, 310, 92, "OPEN FOLDER", 38)
    b.group = "Replays"
    for i, (when, court, mode, length) in enumerate(REPLAYS):
        y, h = 346 + i * 116, 100
        cy = y + h / 2 - 2
        frame(b, 60, y, 1800, h, name="Replay")
        left(b, 108, cy, when, 38, fill=CREAM, name="When")
        left(b, 560, cy, court, 38, fill=CREAM, name="Court")
        left(b, 820, cy, mode, 38, fill=TAN, name="Mode")
        left(b, 1180, cy, length, 38, fill=CREAM, name="Length")
        tag(b, 1480, int(cy - 31), 260, 62, "PARTIAL", "plain")
    b.group = "Footer"
    button(b, "yellow", 60, 944, 320, 100, "PREVIOUS", 40)
    button(b, "yellow", 410, 944, 320, 100, "REFRESH", 40)
    button(b, "yellow", 1540, 944, 320, 100, "NEXT", 40)
    return b


# The result sheet

PLAYERS = [("P1", "RAGO", "sean", (0xA8, 0x70, 0xFF), "2450"), ("P2", "JUAN_DELA_CRUZ_99", "dante", (0xFC, 0xBD, 0x5B), "1980"),
           ("P4", "RAGO", "sean", (0x5E, 0xA1, 0xF5), "1120"), ("P3", "AMIHAN", "amihan", (0x9A, 0xE5, 0x79), "640")]
CROWN = None


def crown():
    global CROWN
    if CROWN is None:
        c = raster("match-result-nextcourt", "r036.png")
        c = c.crop(c.getchannel("A").getbbox())
        CROWN = c.resize((c.width * 2, c.height * 2), Image.LANCZOS)
    return CROWN


def result_frame(board_id, on):
    b = Board()
    poster(b, CAPTURES / "match-result-standings" / "background.png", dim=(40, 8, 14, 150))
    title(b, 60, 85, "RAGO \u00b7 P1 wins!", 90)
    sub(b, 62, 146, "Hero Strike \u00b7 8 rounds")
    b.group = "Tabs"
    tabs(b, 60, 206, 1080, 112, ["STANDINGS", "YOUR MATCH", "PLAYERS"], on)
    dark_panel(b, 60, 330, 1800, 550, seed=40 + on, group="Result")
    b.group = "Footer"
    button(b, "go", 60, 924, 480, 112, "REMATCH", 54)
    button(b, "yellow", 570, 924, 820, 112, "NEXT MAP \u00b7 ERMITA", 44)
    button(b, "red", 1420, 924, 440, 112, "MAIN MENU", 44)
    return b


def standings(board_id):
    b = result_frame(board_id, 0)
    b.group = "Standings"
    for i, (seat, who, hero, color, pts) in enumerate(PLAYERS):
        x, y, h = 110, 362 + i * 122, 106
        you = seat == "P2"
        frame(b, x, y, 1080, h, on=you, name=seat)
        centred(b, x + 50, y + h / 2 - 2, str(i + 1), 54, fill=LIME if you else TAN, name="Rank")
        chip(b, x + 104, y + 14, 78, color, hero)
        if i == 0:
            c = crown()
            b.place("crown-big", c, x + 104 + 39 - c.width / 2, y - 6, "Crown")
        left(b, x + 224, y + h / 2 - 2, "%s \u00b7 %s" % (seat, who), 40, fill=LIME if you else CREAM, name="Player")
        right(b, x + 1040, y + h / 2 - 2, pts + " PTS", 44, fill=LIME if you else CREAM)
    framed(b, 1230, 362, 580, 470, sunburst_art(580, 470, centre=(290, 262)), "winner-burst", name="Winner")
    tag(b, 1330, 394, 380, 66, "MATCH WINNER")
    r = raster("match-result-nextcourt", "r044.png")
    r = r.crop(r.getchannel("A").getbbox())
    r = r.resize((int(r.width * 1.12), int(r.height * 1.12)), Image.LANCZOS)
    b.place("winner-rago", r, 1520 - r.width / 2, 470, "Winner (game render)")
    frame(b, 1250, 752, 540, 92, name="Winner tag")
    centred(b, 1520, 798, "RAGO · P1", 60, fill=CREAM, name="Winner")
    return b


def your_match(board_id):
    """The moment as a reveal: the tagged player bursting out of the rays, the same language as
    the Next Round card, with cream words so nothing is dark on dark."""
    b = result_frame(board_id, 1)
    b.group = "Match moment"
    left(b, 120, 396, "MATCH MOMENT · RAGO · P1 · TAG", 44, fill=CREAM, name="Heading")
    rule(b, 110, 1800, 436)
    x, y, w, h = 300, 462, 1320, 340
    framed(b, x, y, w, h, sunburst_art(w, h, centre=(360, 200)), "taya-burst", name="Taya")
    # The portrait stands proud of the card's top edge.
    p = Image.open(ROOT / "Assets/TumbangPreso/Resources/UI/portraits/dante.png").convert("RGBA")
    p = p.crop(p.getchannel("A").getbbox())
    p.thumbnail((420, 370), Image.LANCZOS)
    b.place("taya-portrait", p, x + 360 - p.width / 2, y + h - 12 - p.height, "Taya portrait")
    title(b, x + 770, y + 100, "TAYA", 150)
    frame(b, x + 600, y + 190, 660, 104, on=True, name="Taya name")
    centred(b, x + 930, y + 240, "JUAN_DELA_CRUZ_99", 50, fill=LIME, name="Taya name")
    return b


def players(board_id):
    b = result_frame(board_id, 2)
    b.group = "Players"
    for i, (seat, who, hero, color, pts) in enumerate(PLAYERS):
        x, y, h = 110, 362 + i * 122, 106
        you = seat == "P2"
        frame(b, x, y, 1560, h, on=you, name=who)
        chip(b, x + 28, y + 14, 78, color, hero)
        name = who if who in ("JUAN_DELA_CRUZ_99", "AMIHAN") else "%s \u00b7 %s" % (who, seat)
        left(b, x + 140, y + h / 2 - 2, name, 46, fill=LIME if you else CREAM, name="Player")
    scrollbar(b, 1760, 362, 862, 0.97)
    return b


COURTS = [("TONDO", "Eskinita"), ("ERMITA", "IlalimNgTulay"), ("SITANGKAI", "LagoonCove"), ("BGC", "Kanto"),
          ("PHILIPPINE ARENA", "Arena"), ("BANAUE", "Payo")]


def next_court(board_id):
    b = Board()
    poster(b, UI / "map-previews/IlalimNgTulay-poster.png", dim=(40, 8, 14, 90))
    b.group = "Header"
    button(b, "red", 60, 40, 220, 100, "BACK", 44)
    title(b, 310, 90, "NEXT COURT")
    sub(b, 62, 166, "Browse a court, then lock your vote.")
    b.group = "Court"
    text(b, (56, 300), "ERMITA", 170, fill=WHITE, stroke=9, stroke_fill=TITLE_EDGE, name="Court")
    button(b, "go", 1400, 470, 460, 124, "LOCK VOTE", 54)
    b.group = "Courts"
    for i, (name, art_name) in enumerate(COURTS):
        x = 60 + i * 302
        frame(b, x, 650, 290, 250, on=i == 1, name=name.title())
        picture(b, x + 20, 668, 250, 140, UI / "map-previews" / (art_name + "-poster.png"), "court-" + art_name.lower())
        centred(b, x + 145, 848, name, 30 if len(name) < 12 else 26, fill=LIME if i == 1 else CREAM, name="Court")
    return b


# Loading

def clean_map(b):
    """The court's poster with no lettering of its own; the title is live text."""
    b.group = "Map"
    p = Image.open(UI / "map-previews/Eskinita-poster.png").convert("RGBA").resize((1920, 1080), Image.LANCZOS)
    b.place("map-eskinita-poster", p, 0, 0, "Map picture (game art)")
    scrim(b, (40, 8, 14, 60))
    b.group = "Title"
    text(b, (56, 70), "TONDO", 190, fill=WHITE, stroke=11, stroke_fill=TITLE_EDGE, name="Court")
    text(b, (66, 318), "Manila, NCR", 56, face=GRAND, fill=WHITE, weight=700, stroke=5, stroke_fill=TITLE_EDGE,
         name="Place")
    logo = Image.open(UI / "brand/tump_logo_white.png").convert("RGBA")
    logo = logo.crop(logo.getchannel("A").getbbox())
    logo.thumbnail((240, 200), Image.LANCZOS)
    b.place("tump-logo-white", logo, 1860 - logo.width, 1040 - logo.height, "Logo (game art)")


def loading(board_id):
    b = Board()
    clean_map(b)
    b.group = "Progress"
    b.place("track-420x58", slice3(art("login3-tabs-track"), 420, 58), 750, 990, "Progress track")
    return b


def loading_failed(board_id):
    b = Board()
    clean_map(b)
    b.group = "Error"
    edge(b, 960, 730, "The match could not be loaded.", 40, face=GRAND, weight=700, align="c", stroke=4, name="Error")
    button(b, "red", 640, 780, 640, 124, "RETURN TO MENU", 50)
    return b


# Round break and halftime

SEATS = {"P1": (0xA8, 0x70, 0xFF), "P2": (0xFC, 0xBD, 0x5B), "P3": (0x9A, 0xE5, 0x79), "P4": (0x5E, 0xA1, 0xF5)}
BREAKS = {
    "match-break": dict(banner="NEXT ROUND", clock="3", round="ROUND 2", taya="dante", taya_seat="P2",
                        rows=[("P1", "sean"), ("P2", "dante"), ("P4", "sean"), ("P3", "amihan")], crown="P1", note=None),
    "match-halftime": dict(banner="HALFTIME", clock="11", round="ROUND 5", taya="dante", taya_seat="P1",
                           rows=[("P1", "dante"), ("P2", "nemu"), ("P4", "sean"), ("P3", "amihan")], crown="P1",
                           note="HIGHLIGHT UNAVAILABLE"),
}
TOTALS = {"P1": 2450, "P2": 1980, "P4": 1120, "P3": 640}
_BADGES = {}


def badge(kind, k=1.0):
    """The small game icons from the round capture: the taya mark and the crown."""
    key = (kind, k)
    if key not in _BADGES:
        f = {"taya": "r004.png", "crown": "r009.png"}[kind]
        im = raster("match-break", f)
        im = im.crop(im.getchannel("A").getbbox())
        _BADGES[key] = im.resize((int(im.width * k), int(im.height * k)), Image.LANCZOS)
    return _BADGES[key]


def tilted(b, key, im, cx, cy, angle):
    r = im.rotate(angle, expand=True, resample=Image.BICUBIC)
    b.place("%s-%d" % (key, int(angle * 10)), r, cx - r.width / 2, cy - r.height / 2, "Tape")


def clock_art(s, left):
    """A stopwatch in the yellow button's paint, hand-cut rather than compass-drawn: a wobbled body
    with its slab joined below and a lit top edge, a deep berry dial so the body and the dial part
    clearly, and a lime arc that drains as time runs."""
    import random
    from PIL import ImageChops, ImageFilter
    S, top, drop, lip = 3, 44, 16, 9
    W, H = s * S, (s + top + drop + 4) * S
    rnd = random.Random(7)
    cx, cy, R = W / 2, (top + s / 2) * S, s / 2 * S - 6 * S

    def blob(radius, amp, n=26):
        pts = []
        for k in range(n):
            a = 2 * math.pi * k / n
            r = radius + rnd.uniform(-amp, amp)
            pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
        m = Image.new("L", (W, H))
        ImageDraw.Draw(m).polygon(pts, fill=255)
        return m.filter(ImageFilter.GaussianBlur(4 * S)).point(lambda v: 255 if v > 127 else 0)

    body = blob(R, 3.2 * S)
    d = ImageDraw.Draw(body)
    # Stem, crown and two pushers, leaning a little, all one piece with the body.
    d.polygon([(cx - 20 * S, cy - R + 8 * S), (cx - 17 * S, 22 * S), (cx + 19 * S, 21 * S), (cx + 21 * S, cy - R + 8 * S)], fill=255)
    d.polygon([(cx - 46 * S, 26 * S), (cx - 42 * S, 4 * S), (cx + 40 * S, 2 * S), (cx + 47 * S, 24 * S)], fill=255)
    for sign in (-1, 1):
        a = math.radians(-90 + sign * 38)
        ux, uy = math.cos(a), math.sin(a)
        px, py = cx + ux * (R + 8 * S), cy + uy * (R + 8 * S)
        tx, ty = -uy * 17 * S, ux * 17 * S
        d.polygon([(px - tx - ux * 24 * S, py - ty - uy * 24 * S), (px - tx, py - ty), (px + tx, py + ty), (px + tx - ux * 24 * S, py + ty - uy * 24 * S)], fill=255)
    body = body.filter(ImageFilter.GaussianBlur(3 * S)).point(lambda v: 255 if v > 127 else 0)
    slab = body
    for dy in range(S, drop * S + 1, S):
        slab = ImageChops.lighter(slab, ImageChops.offset(body, 0, dy))
    face = ImageChops.multiply(body, ImageChops.offset(body, 0, lip * S))
    im = Image.new("RGBA", (W, H))
    im.paste((0xC2, 0x6A, 0x12, 255), (0, 0), slab)
    im.paste((0xFF, 0xE4, 0x8A, 255), (0, 0), body)
    im.paste((0xF6, 0xBC, 0x3A, 255), (0, 0), face)
    dial = blob(R - 12 * S, 2.2 * S)
    im.paste(WELL + (255,), (0, 0), dial)
    d = ImageDraw.Draw(im)
    # The approved dial: a berry track, the lime arc draining over it, and tick marks inside.
    box = (cx - R + 20 * S, cy - R + 20 * S, cx + R - 20 * S, cy + R - 20 * S)
    d.arc(box, 0, 360, fill=(0x8A, 0x2A, 0x3C, 255), width=20 * S)
    d.arc(box, -90, -90 + 360 * left, fill=LIME + (255,), width=20 * S)
    r0, r1 = R - 48 * S, R - 36 * S
    for t in range(12):
        a = math.radians(t * 30)
        d.line((cx + r0 * math.sin(a), cy - r0 * math.cos(a), cx + r1 * math.sin(a), cy - r1 * math.cos(a)),
               fill=TAN + (255,), width=5 * S)
    im = im.resize((W // S, H // S), Image.LANCZOS)
    return im


PLATE = "plain"  # the name and round plates, under comparison (owner, 2026-11-01)


def plate(b, x, y, w, h, name):
    b.place("plate-%s-%dx%d" % (PLATE, w, h), button_art(PLATE, w, h), x, y, name)


def plate_ink():
    return BUTTONS[PLATE][1]


def round_break(board_id):
    spec = BREAKS[board_id]
    b = Board()
    poster(b, CAPTURES / board_id / "background.png", dim=(40, 8, 14, 110))
    taya_seat = spec["taya_seat"]
    # Top: the title and, behind it, a strip of tape.
    b.group = "Tape"
    title(b, 60, 85, spec["banner"], 110)
    if spec["note"]:
        edge(b, 1390, 76, spec["note"], 42, align="c", stroke=4, name="Note")
    # Focal point: the next taya on a sunburst, breaking out of the card.
    b.group = "Next taya"
    framed(b, 50, 190, 880, 450, sunburst_art(880, 450, centre=(312, 262)), "taya-burst", name="Next taya")
    p = Image.open(UI / "portraits" / (spec["taya"] + ".png")).convert("RGBA")
    p = p.crop(p.getchannel("A").getbbox())
    p.thumbnail((470, 470), Image.LANCZOS)
    px, py = 300 - p.width / 2, 170
    b.place("taya-portrait-%s" % spec["taya"], p, px, py, "Next taya (game art)")
    t = badge("taya", 2.6)
    b.place("taya-badge-big", t, px + p.width - t.width - 6, py + p.height - t.height - 60, "Taya mark")
    tag(b, 560, 226, 340, 74, "NEXT TAYA", "red")
    tilted(b, "tape-a", hazard_art(240, 30), 1846, 214, -40)
    tilted(b, "tape-b", hazard_art(240, 30), 62, 214, 40)
    plate(b, 90, 566, 800, 104, "Taya name")
    centred(b, 490, 566 + 104 * FACE_LIP, "JUAN_DELA_CRUZ_99", 58, fill=plate_ink(), name="Name")
    # The countdown.
    b.group = "Timer"
    framed(b, 980, 190, 880, 380, sunburst_art(880, 380, c1=(0xA8, 0x34, 0x48), c2=(0x6A, 0x1B, 0x2C),
                                               centre=(440, 190)), "clock-burst", name="Countdown")
    b.place("clock-%s" % spec["clock"], clock_art(300, int(spec["clock"]) / 15), 1270, 196, "Countdown")
    centred(b, 1420, 390, spec["clock"], 130, fill=CREAM, name="Seconds")
    plate(b, 1020, 566, 800, 104, "Round")
    centred(b, 1420, 566 + 104 * FACE_LIP, spec["round"], 70, fill=plate_ink(), name="Round")
    # The standings, as a podium: highest total tallest, rank numbers large.
    b.group = "Scores"
    heights = [330, 306, 282, 258]
    for i, (seat, hero) in enumerate(spec["rows"]):
        w, h = 400, heights[i]
        x, y = 115 + i * 430, 1030 - h
        on = seat == "P2"
        frame(b, x, y, w, h, on=on, name=seat)
        chip(b, x + 26, y + 26, 126, SEATS[seat], hero)
        if seat == spec["crown"]:
            c = badge("crown", 2.2)
            b.place("crown-small", c, x + 26 + 63 - c.width / 2, y - c.height / 2 - 4, "Crown")
        if seat == taya_seat:
            t = badge("taya", 1.4)
            b.place("taya-badge-small", t, x + 26 + 126 - t.width + 4, y + 26 + 126 - t.height + 4, "Taya mark")
        left(b, x + 176, y + 56, seat, 46, fill=LIME if on else TAN, name="Seat")
        left(b, x + 176, y + 112, "+%d" % TOTALS[seat], 40, fill=CREAM, name="Gain")
        right(b, x + w - 30, y + 62, str(i + 1), 84, fill=LIME if on else TAN)
        centred(b, x + w / 2, y + h - 56, str(TOTALS[seat]), 92, fill=LIME if on else CREAM, name="Total")
    return b


SCREENS = {
    "hub-record-profile": profile,
    "hub-record-career": career,
    "hub-record-matches": history,
    "hub-record-friends": friends,
    "hub-record-account": account,
    "hub-replays": replays,
    "match-result-yourmatch": your_match,
    "match-result-players": players,
    "match-result-nextcourt": next_court,
    "match-result-standings": standings,
    "match-loading": loading,
    "match-loading-failed": loading_failed,
    "match-break": round_break,
    "match-halftime": round_break,
}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--only", help="comma-separated capture ids to draw")
    args = parser.parse_args()
    ids = args.only.split(",") if args.only else list(SCREENS)
    OUT.mkdir(parents=True, exist_ok=True)
    for i in ids:
        SCREENS[i](i).im.convert("RGB").save(OUT / (i + ".png"))
        print("drew", i)


if __name__ == "__main__":
    main()
