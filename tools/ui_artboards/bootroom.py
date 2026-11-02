"""The Room area in the boot kit: hosting, joining, the room itself with its popups, the map
vote, character pick, match found and the custom rules. Laid out from the captures in
Logs/ui-artboards; `bootscreens.py` writes the job."""
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

from bootkit import (ROOT, Board, DARUMA, GRAND, MAROON, INK, TAN, TAN_D, WHITE, TITLE_EDGE, GREEN_INK, CREAM,
                     SOFT_RED, LIME,
                     FACE_LIP, button, centred, field, frame, ground, head, header, left, modal, panel, para,
                     picture, poster, right, rule, subtitle, tabs, tag, text, title, well, wrap)

CAPTURES = ROOT / "Logs" / "ui-artboards"
UI = ROOT / "Assets/TumbangPreso/Resources/UI"
STREET = UI / "map-previews/Eskinita-poster.png"


def capture(board_id):
    return json.loads((CAPTURES / board_id / "manifest.json").read_text(encoding="utf-8"))


def portraits(board_id):
    """The seat renders the room shows: the large 'Face' rasters, in seat order."""
    m = capture(board_id)
    return [Image.open(CAPTURES / board_id / L["file"]).convert("RGBA") for L in m["layers"]
            if L["kind"] == "raster" and L["name"] == "Face" and L["h"] > 300 and L["w"] < 420]


# The room

ROOM = {  # who sits where: (name, hero, status) or None for an open seat
    "host": [("YOU", "DANTE", "HOST"), None, None, None],
    "guest": [("MARIA", "RAGO", "HOST"), ("YOU", "DANTE", "NOT READY"), ("BOT", "PAETE", "READY"), None],
    "spectator": [("MARIA", "RAGO", "HOST"), ("JUAN", "DANTE", "NOT READY"), ("BOT", "PAETE", "READY"), None],
}
STATUS = {"HOST": "yellow", "READY": "go", "NOT READY": "plain"}


def seat(b, x, y, w, h, slot, who, face, host_view, n):
    s = w / 420
    b.group = "Seat P%d" % slot
    if who is None:
        well(b, x, y, w, h, seed=20 + n)
        left(b, x + 30 * s, y + 50 * s, "P%d" % slot, int(30 * s), fill=SOFT_RED, name="Slot")
        centred(b, x + w / 2, y + h * 0.36, "+", int(110 * s), fill=SOFT_RED, name="Plus")
        centred(b, x + w / 2, y + h * 0.6, "OPEN SEAT", int(44 * s), fill=CREAM, name="Open seat")
        if host_view:
            bw = int(w - 70 * s)
            button(b, "yellow", x + (w - bw) // 2, int(y + h - 130 * s), bw, int(84 * s), "+ ADD BOT", int(34 * s))
        return
    name, hero, status = who
    frame(b, x, y, w, h, on=name == "YOU", name="Seat")
    if face is not None:
        f = face.copy()
        f.thumbnail((int(w - 60 * s), int(h * 0.56)), Image.LANCZOS)
        b.place("seat-face-%s-%d" % (hero.lower(), f.width), f, x + (w - f.width) // 2, y + 30 * s, "Hero")
    left(b, x + 30 * s, y + 50 * s, "P%d" % slot, int(30 * s), fill=TAN, name="Slot")
    centred(b, x + w / 2, y + h - 150 * s, name, int(52 * s), fill=LIME if name == "YOU" else CREAM, name="Player")
    centred(b, x + w / 2, y + h - 104 * s, hero, int(28 * s), face=GRAND, fill=TAN, weight=650, name="Hero")
    tw = int((200 if status == "NOT READY" else 150) * s)
    tag(b, x + (w - tw) // 2, int(y + h - 76 * s), tw, int(52 * s), status, STATUS[status])


def code_panel(b, ip=True):
    b.group = "Room code"
    frame(b, 1400, 30, 460, 170, name="Room code")
    left(b, 1440, 76, "ROOM CODE", 28, fill=TAN, name="Section")
    left(b, 1440, 138, "2DEC", 72, fill=CREAM, name="Code")
    tag(b, 1720, 50, 110, 50, "LAN")
    if ip:
        right(b, 1830, 148, "25.17.144.85:8910", 24, face=GRAND, fill=TAN, weight=600)


def room(board_id):
    b = Board()
    host = board_id in ("room-host", "room-chat", "room-close")
    kind = "host" if host else ("spectator" if board_id == "room-spectator" else "guest")
    poster(b, STREET)
    header(b, "TEST ROOM" if host else "MARIA'S ROOM")
    subtitle(b, "YOUR ROOM" if host else "HOSTED BY MARIA")
    code_panel(b, ip=host)
    b.group = "Players"
    seated = sum(1 for w in ROOM[kind] if w)
    text(b, (60, 222), "PLAYERS %d / 4" % seated, 46, fill=WHITE, stroke=5, stroke_fill=TITLE_EDGE, name="Players")
    chat = board_id == "room-chat"
    right_edge = 1240 if chat else 1860
    if host:
        button(b, "plain", right_edge - 260, 212, 260, 80, "SPECTATE", 34)
    else:
        button(b, "go" if kind == "spectator" else "plain", right_edge - 280, 212, 280, 80,
               "SPECTATING" if kind == "spectator" else "SPECTATE", 32 if kind == "spectator" else 34)
        button(b, "plain", right_edge - 280 - 20 - 220, 212, 220, 80, "RULES", 34)
    faces = portraits("room-host" if host else "room-guest")
    w, gap = (280, 20) if chat else (420, 40)
    h = SEAT_H if host and not chat else (540 if chat else 520)
    fi = 0
    for i, who in enumerate(ROOM[kind]):
        face = None
        if who:
            face = faces[fi] if fi < len(faces) else None
            fi += 1
        seat(b, 60 + i * (w + gap), 310, w, h, i + 1, who, face, host, i)
    if host and not chat:
        setup(b)
    b.group = "Footer"
    mid = 650 if chat else 960
    if kind == "spectator":
        frame(b, 560, 930, 800, 120, name="Watching")
        centred(b, 960, 970, "WATCHING", 44, fill=CREAM, name="Status")
        centred(b, 960, 1018, "0 of 2 ready · waiting for the host to start", 28, face=GRAND, fill=TAN, weight=600,
                name="Hint")
    else:
        button(b, "go", mid - 270, 936, 540, 124, "START GAME" if host else "READY", 54)
    if chat:
        chat_panel(b)
    else:
        button(b, "yellow", 1560, 946, 300, 104, "CHAT", 44)
    if board_id == "room-close":
        x, y = modal(b, 860, 400, "CLOSE ROOM?")
        centred(b, 960, y + 170, "Leaving closes this room. Nobody else is in it yet.", 30, face=GRAND, weight=500,
                name="Message")
        button(b, "go", x + 70, y + 240, 340, 112, "KEEP ROOM", 46)
        button(b, "red", x + 450, y + 240, 340, 112, "CLOSE ROOM", 46)
    elif board_id == "room-swap":
        x, y = modal(b, 860, 440, "SWITCH SEATS?")
        centred(b, 960, y + 170, "Maria wants to switch seats with you.", 30, face=GRAND, weight=500, name="Message")
        centred(b, 960, y + 214, "You move from P2 to P1.", 30, face=GRAND, weight=500, name="Message")
        button(b, "go", x + 70, y + 286, 340, 112, "YES", 50)
        button(b, "red", x + 450, y + 286, 340, 112, "NO", 50)
    elif board_id == "room-settings":
        x, y = modal(b, 860, 760, "MATCH SETTINGS", cy_offset=84)
        centred(b, 960, y + 144, "Set by the host.", 30, face=GRAND, weight=500, name="Hint")
        items = [("MAP", "TONDO"), ("MODE", "HERO STRIKE"), ("ROUNDS", "8"), ("ROUND LENGTH", "90 S"), ("NETWORK", "LAN")]
        for i, (k, v) in enumerate(items):
            cy = y + 214 + i * 78
            left(b, x + 70, cy, k, 34, fill=TAN_D, name="Setting")
            right(b, x + 790, cy, v, 38, fill=MAROON)
            if i < len(items) - 1:
                rule(b, x + 60, x + 800, int(cy + 39))
        button(b, "yellow", x + 70, y + 610, 340, 108, "FULL RULES", 42)
        button(b, "go", x + 450, y + 610, 340, 108, "GOT IT", 48)
    return b


SEAT_H = 440


def setup(b):
    """The host's map, mode and rules under the seats: each control under its own white label,
    no strip (owner, 2026-10-30)."""
    b.group = "Match setup"
    for x, s in [(60, "MAP"), (720, "MODE"), (1380, "RULES")]:
        text(b, (x, 776), s, 34, fill=WHITE, stroke=4, stroke_fill=TITLE_EDGE, name="Label")
    button(b, "yellow", 60, 826, 100, 92, "<", 46)
    field(b, 170, 826, 360, 92, "TONDO", face=DARUMA, size=44)
    button(b, "yellow", 540, 826, 100, 92, ">", 46)
    tabs(b, 720, 818, 600, 104, ["CLASSIC", "HERO STRIKE"], 1)
    button(b, "plain", 1380, 826, 480, 92, "8 ROUNDS · 90 S", 36)


def chat_panel(b):
    panel(b, 1290, 230, 570, 830, seed=18, group="Room chat")
    left(b, 1340, 292, "ROOM CHAT", 48, fill=MAROON, name="Heading")
    button(b, "red", 1730, 252, 90, 80, "X", 40)
    centred(b, 1575, 420, "No messages yet.", 30, face=GRAND, fill=TAN_D, weight=600, name="Empty")
    field(b, 1330, 940, 380, 84, "Say something. Enter sends.", size=24, weight=500, fill=TAN_D)
    button(b, "yellow", 1724, 942, 100, 80, ">", 44)


# Getting into a room

def host_game(board_id):
    b = Board()
    poster(b, STREET)
    header(b, "HOST GAME")
    subtitle(b, "CUSTOM")
    panel(b, 360, 200, 1200, 660, seed=21)
    head(b, 420, 252, "ROOM NAME")
    field(b, 420, 280, 1080, 96, "PLAYER'S ROOM", face=DARUMA, size=40)
    head(b, 420, 422, "WHERE ARE YOUR FRIENDS?")
    for i, (name, hint) in enumerate([("LAN", "Same Wi-Fi, no internet"), ("ONLINE", "Anywhere, by code")]):
        x = 420 + i * 560
        frame(b, x, 450, 520, 190, on=i == 0, name=name)
        centred(b, x + 260, 520, name, 70, fill=LIME if i == 0 else CREAM, name="Choice")
        centred(b, x + 260, 590, hint, 30, face=GRAND, fill=TAN, weight=600, name="Hint")
    head(b, 420, 702, "WHO CAN JOIN")
    tabs(b, 420, 730, 1080, 104, ["PUBLIC", "FRIENDS ONLY", "PRIVATE"], 0)
    b.group = "Footer"
    button(b, "go", 690, 908, 540, 128, "CREATE ROOM", 56)
    return b


ROOMS = [("KAPITBAHAY", "ESKINITA", "2/4"), ("TONDO NIGHTS", "KANTO", "IN A MATCH")]


def join_game(board_id):
    b = Board()
    poster(b, STREET)
    header(b, "JOIN GAME")
    subtitle(b, "CUSTOM")
    panel(b, 360, 200, 1200, 760, seed=22)
    head(b, 420, 252, "GOT A CODE?")
    field(b, 420, 280, 790, 100, "ABCD or 192.168.1.20", size=34, weight=500, fill=TAN_D)
    button(b, "go", 1240, 280, 260, 100, "JOIN", 50)
    head(b, 420, 452, "OPEN ROOMS")
    button(b, "plain", 1280, 414, 220, 76, "REFRESH", 32)
    if board_id == "hub-join":
        for i, (name, court, seats) in enumerate(ROOMS):
            # A room is a small berry card, tall enough for its name and court on two lines.
            y, h = 506 + i * 150, 128
            cy = y + h / 2 - 2
            frame(b, 420, y, 1080, h, name="Room")
            left(b, 462, cy - 20, name, 40, fill=CREAM, name="Room")
            left(b, 464, cy + 24, court, 24, face=GRAND, fill=TAN, weight=700, name="Court")
            tag(b, 920, int(cy - 26), 110, 52, "LAN")
            if seats == "IN A MATCH":
                left(b, 1060, cy, seats, 26, face=GRAND, fill=CREAM, weight=700, name="Seats")
            else:
                left(b, 1060, cy, seats, 40, fill=CREAM, name="Seats")
            button(b, "yellow", 1290, int(cy - 80 * FACE_LIP), 180, 80, "JOIN", 38)
    else:
        centred(b, 960, 640, "No rooms yet. Host one, or join with a code.", 34, face=GRAND, weight=600, name="Empty")
        centred(b, 960, 690, "No public rooms yet.", 30, face=GRAND, fill=TAN_D, weight=600, name="Empty")
    return b


def match_found(board_id):
    b = Board()
    ground(b)
    panel(b, 260, 330, 1400, 380, seed=23, group="Match found")
    centred(b, 960, 470, "MATCH FOUND", 150, fill=MAROON, name="Heading")
    tag(b, 760, 580, 400, 92, "HERO STRIKE")
    return b


# Before the match

ROSTER_NAMES = ["dante", "cheska", "sean", "zack", "nemu", "phaister", "rafi", "amihan", "paete"]


def pick(board_id):
    """The hero on a lit stage; its name and ability float on the weave like a title, the
    roster and team in small berry cards (owner, 2026-10-30)."""
    b = Board()
    m = capture("hub-pick")
    ground(b)

    def raster(name):
        L = next(L for L in m["layers"] if L["name"] == name)
        return Image.open(CAPTURES / "hub-pick" / L["file"]).convert("RGBA"), L

    b.group = "Stage"
    mask = Image.new("L", (1920, 1080))
    ImageDraw.Draw(mask).ellipse((380, 140, 1420, 980), fill=150)
    light = Image.new("RGBA", (1920, 1080), (0xC2, 0x4A, 0x5A, 255))
    light.putalpha(mask.filter(ImageFilter.GaussianBlur(120)))
    b.place("pick-spotlight", light, 0, 0, "Spotlight")
    stage, S = raster("Stage shapes")
    surf, P = raster("PreviewSurface")
    k = 1.1
    dx, dy = 900 - (P["x"] + P["w"] / 2) * k, 860 - (P["y"] + P["h"]) * k
    stage = stage.resize((int(stage.width * k), int(stage.height * k)), Image.LANCZOS)
    surf = surf.resize((int(surf.width * k), int(surf.height * k)), Image.LANCZOS)
    b.place("pick-stage", stage, S["x"] * k + dx, S["y"] * k + dy, "Chalk circle")
    b.place("pick-hero-dante", surf, P["x"] * k + dx, P["y"] * k + dy, "Hero (game render)")
    b.group = "Hero"
    text(b, (60, 50), "// EARTH JUGGERNAUT", 30, face=GRAND, fill=WHITE, weight=700, stroke=3, stroke_fill=TITLE_EDGE,
         name="Role")
    text(b, (54, 90), "DANTE", 140, fill=WHITE, stroke=9, stroke_fill=TITLE_EDGE, name="Hero")
    icons = Image.new("RGBA", (520, 170))
    for L in m["layers"]:
        if L["kind"] == "raster" and L["x"] < 570 and 230 < L["y"] < 420:
            icons.alpha_composite(Image.open(CAPTURES / "hub-pick" / L["file"]).convert("RGBA"), (L["x"] - 30, L["y"] - 240))
    b.place("pick-abilities-dante", icons, 40, 250, "Ability icons (game art)")
    text(b, (60, 440), "UNSTOPPABLE", 54, fill=WHITE, stroke=5, stroke_fill=TITLE_EDGE, name="Ability")
    text(b, (62, 515), "SIGNATURE · PROTECTION", 30, face=GRAND, fill=LIME, weight=800, name="Kind")
    text(b, (62, 560), "Press E · 40 s cooldown", 30, face=GRAND, fill=WHITE, weight=600, stroke=3,
         stroke_fill=TITLE_EDGE, name="Use")
    para(b, 62, 612, wrap("Cleanse removable effects and gain 15 s of Status Immunity.", 32, 470, weight=600), 32, 44,
         fill=WHITE, weight=600, stroke=3, stroke_fill=TITLE_EDGE)
    b.group = "Timer"
    centred(b, 900, 70, "PICK", 34, face=GRAND, fill=WHITE, weight=800, name="Timer")
    centred(b, 900, 140, "29", 96, fill=WHITE, name="Seconds")
    b.group = "Roster"
    for i, name in enumerate(ROSTER_NAMES):
        x, y = 1330 + (i % 3) * 182, 160 + (i // 3) * 182
        frame(b, x, y, 166, 166, on=i == 0, name=name.title())
        p = Image.open(UI / "portraits" / (name + ".png")).convert("RGBA")
        p.thumbnail((140, 140), Image.LANCZOS)
        b.place("portrait-" + name, p, x + (166 - p.width) // 2, y + (166 - p.height) // 2, name.title())
    b.group = "Team"
    tag(b, 360, 846, 280, 50, "TAYA IN ROUND 2", "yellow")
    for i in range(4):
        x = 60 + i * 300
        if i == 1:
            frame(b, x, 904, 284, 130, on=True, name="You")
            p = Image.open(UI / "portraits/dante.png").convert("RGBA")
            p.thumbnail((100, 110), Image.LANCZOS)
            b.place("portrait-dante-small", p, x + 30, 904 + (130 - p.height) // 2, "Dante")
            left(b, x + 140, 950, "YOU", 40, fill=LIME, name="Player")
            left(b, x + 142, 994, "PICKING", 24, face=GRAND, fill=TAN, weight=700, name="State")
        else:
            well(b, x, 904, 284, 130, seed=40 + i)
            centred(b, x + 142, 964, "+ OPEN", 38, fill=CREAM, name="Open")
    b.group = "Footer"
    button(b, "go", 1330, 920, 530, 128, "SELECT", 60)
    return b


COURTS = [("TONDO", "Eskinita"), ("ERMITA", "IlalimNgTulay"), ("SITANGKAI", "LagoonCove"), ("BGC", "Kanto"),
          ("PHILIPPINE ARENA", "Arena"), ("BANAUE", "Payo")]


def map_vote(board_id):
    b = Board()
    poster(b, STREET, dim=(40, 8, 14, 70))
    title(b, 60, 85, "CHOOSE THE COURT")
    subtitle(b, "Browse a court, then lock your vote.", y=150)
    b.group = "Timer"
    frame(b, 1680, 30, 180, 130, name="Timer")
    centred(b, 1770, 95, "79", 84, fill=CREAM, name="Seconds")
    b.group = "Court"
    text(b, (56, 280), "TONDO", 170, fill=WHITE, stroke=9, stroke_fill=TITLE_EDGE, name="Court")
    text(b, (62, 530), "No vote locked yet.", 34, face=GRAND, fill=WHITE, weight=650, stroke=3, stroke_fill=TITLE_EDGE,
         name="Vote")
    button(b, "go", 1400, 470, 460, 124, "LOCK VOTE", 54)
    b.group = "Courts"
    for i, (name, art) in enumerate(COURTS):
        x = 60 + i * 302
        frame(b, x, 640, 290, 250, on=i == 0, name=name.title())
        picture(b, x + 20, 658, 250, 140, UI / "map-previews" / (art + "-poster.png"), "court-" + art.lower())
        centred(b, x + 145, 828, name, 30 if len(name) < 12 else 26, fill=LIME if i == 0 else CREAM, name="Court")
        centred(b, x + 145, 860, "0 VOTES", 24, face=GRAND, fill=TAN, weight=700, name="Votes")
    centred(b, 960, 970, "Ties favour a different court from the last one.", 32, face=GRAND, fill=WHITE, weight=650,
            name="Note")
    b.layers[-1]["effects"] = [{"type": "outline", "dx": 3, "dy": 3, "color": "8F141F", "alpha": 1}]
    return b


# Custom rules: the approved frame, both tabs, the long hints wrapped instead of cut.

RULES = {
    "hub-rules-match": (0, [("FORMAT", "STANDARD", "Choose how the round is played."),
                            ("MODE", "HERO STRIKE", "Classic street play or Hero Strike powers."),
                            ("ROUNDS", "8", "How many rounds make up this match."),
                            ("ROUND LENGTH", "90s", "Time allowed for each round."),
                            ("SCORE TARGET", "OFF", "Reach this score to finish early. OFF plays every round.")]),
    "hub-rules-room": (1, [("BOTS", "NONE", "Fill empty seats, or choose NONE to play with people only."),
                           ("PRIVATE ROOM", "OFF", "Hide this room from the online room list."),
                           ("MAP VOTE", "OFF", "ON lets players vote after character select. OFF uses the "
                                               "host-selected court.")]),
}


def rules(board_id):
    on, items = RULES[board_id]
    b = Board()
    ground(b)
    header(b, "CUSTOM MATCH")
    subtitle(b, "HERO STRIKE · STANDARD · 8 rounds · 90s")
    b.group = "Tabs"
    tabs(b, 60, 194, 760, 120, ["THE MATCH", "THE ROOM"], on)
    panel(b, 60, 326, 1800, 560, seed=7)
    for i, (k, v, h) in enumerate(items):
        y = 366 + i * 100
        cy = y + 42
        left(b, 120, cy, k, 40, fill=MAROON, name="Rule")
        button(b, "yellow", 520, int(cy - 84 * FACE_LIP), 104, 84, "<", 46)
        centred(b, 807, cy, v, 40, name="Value")
        button(b, "yellow", 990, int(cy - 84 * FACE_LIP), 104, 84, ">", 46)
        lines = wrap(h, 32, 660)
        if len(lines) == 1:
            left(b, 1140, cy, h, 32, face=GRAND, weight=450, caps=False, name="Hint")
        else:
            lines = wrap(h, 28, 660)
            para(b, 1140, cy - 36, lines, 28, 34)
        if i < len(items) - 1:
            rule(b, 110, 1800, y + 94)
    b.group = "Footer"
    text(b, (60, 968), "These are the shipped rules, so this match counts for your rank.", 30, face=GRAND,
         fill=WHITE, weight=600, stroke=3, stroke_fill=TITLE_EDGE, name="Note")
    button(b, "yellow", 1020, 924, 400, 112, "DEFAULT RULES", 40)
    button(b, "go", 1450, 924, 410, 112, "DONE", 54)
    return b


SCREENS = {"hub-host": host_game, "hub-join": join_game, "hub-join-empty": join_game, "hub-matchfound": match_found,
           "hub-pick": pick, "hub-rules-match": rules, "hub-rules-room": rules, "room-mapvote": map_vote}
SCREENS.update({i: room for i in ["room-host", "room-guest", "room-spectator", "room-chat", "room-settings",
                                  "room-swap", "room-close"]})
