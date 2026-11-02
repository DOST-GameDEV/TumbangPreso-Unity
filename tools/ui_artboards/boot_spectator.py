"""Spectator and touch HUD review boards in the boot kit: the spectator views (live, controls,
instant replay), the phone HUD (Hero Strike and Classic), guided training, the held skills sheet
and the phone spectator with its tools drawer. Laid out from the captures in Logs/ui-artboards.

Rules these follow (owner, 2026-11-01): readouts stay at the edges, touch controls keep their
places and thumb-sized targets and stay semi-transparent, nothing shares its background's colour
(HUD pieces over the world are dark weave cards with a rose rim), cream panels are only for
large panels without a back or close button, dense buttons use the thin slab, lime means on.

    py -3 tools/ui_artboards/boot_spectator.py [--only mobile-hud ...]
"""
import argparse
import json
import re
from pathlib import Path

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFilter

from bootkit import (BUTTONS, CARD, CREAM, DARUMA, FACE_LIP, GRAND, LIME, MAROON, ROOT, TAN, TAN_D, TITLE_EDGE, WHITE,
                     Board, art, button_art, centred, chamfer, dark_panel, font, frame, framed, left, paint_card_art,
                     ps_name, right, text, thin_button_art, weave, wobble, world)

CAPTURES = ROOT / "Logs" / "ui-artboards"
OUT = ROOT / "ArtSource/ui/revamp-2026-10-30-boot/preview"

ROSE, SLAB = (0xD0, 0x62, 0x76), (0x2A, 0x06, 0x10)
DARK, LITD = (0x36, 0x0A, 0x15), (0x6E, 0x1E, 0x30)
WELL, WELL_RIM = (0x7A, 0x24, 0x36), (0x2A, 0x06, 0x10)
YELLOW, YELLOW_SLAB = (0xF6, 0xBC, 0x3A), (0xC2, 0x6A, 0x12)
SEATS = [(0xA8, 0x70, 0xFF), (0xFC, 0xBD, 0x5B), (0x9A, 0xE5, 0x79), (0x5E, 0xA1, 0xF5)]
YELLOW_INK, GREEN_DARK = BUTTONS["yellow"][1], (0x16, 0x33, 0x06)


# ---------------------------------------------------------------- small helpers

def manifest(bid):
    return json.loads((CAPTURES / bid / "manifest.json").read_text(encoding="utf-8"))


def raster(bid, name):
    return Image.open(CAPTURES / bid / name).convert("RGBA")


def asset(path):
    return Image.open(ROOT / path).convert("RGBA")


def mix(a, b, t):
    return tuple(int(a[i] * (1 - t) + b[i] * t) for i in range(3))


def fade(im, a):
    im = im.copy()
    im.putalpha(im.getchannel("A").point(lambda v: int(v * a)))
    return im


_WEAVE = {}


def wv(dark, light, size=(1920, 1080)):
    key = (dark, light, size)
    if key not in _WEAVE:
        _WEAVE[key] = weave(dark, light, size)
    return _WEAVE[key]


def tinted(im, rgb, height=None):
    """A game icon repainted in one colour at full strength, cropped to its ink."""
    im = im.convert("RGBA")
    a = np.asarray(im.getchannel("A"), dtype=np.float32)
    im = im.crop(im.getchannel("A").point(lambda v: 255 if v > 24 else 0).getbbox())
    a = np.asarray(im.getchannel("A"), dtype=np.float32)
    a = np.clip(a / max(1.0, a.max()) * 255, 0, 255).astype(np.uint8)
    if height:
        im = im.resize((max(1, round(im.width * height / im.height)), height), Image.LANCZOS)
        a = np.asarray(Image.fromarray(a).resize(im.size, Image.LANCZOS))
    out = Image.new("RGBA", im.size, tuple(rgb) + (255,))
    out.putalpha(Image.fromarray(a))
    return out


def icon(b, im, key, cx, cy, rgb, height):
    t = tinted(im, rgb, height)
    b.place("%s-%02x%02x%02x-%d" % (key, rgb[0], rgb[1], rgb[2], t.height), t, cx - t.width / 2, cy - t.height / 2, "Icon " + key)


def fitted(im, key, b, cx, cy, size, name="Art"):
    """Game art kept as it is, scaled to fit a box and centred on a point."""
    im = im.convert("RGBA")
    box = visible(im, 20)
    if box:
        im = im.crop(box)
    s = size / max(im.width, im.height)
    im = im.resize((max(1, round(im.width * s)), max(1, round(im.height * s))), Image.LANCZOS)
    b.place("%s-%d" % (key, size), im, cx - im.width / 2, cy - im.height / 2, name)


def visible(im, floor=40):
    return im.getchannel("A").point(lambda v: 255 if v > floor else 0).getbbox()


def fit_size(s, size, maxw, face=DARUMA, weight=None):
    while size > 12 and font(face, size, weight).getlength(s) > maxw:
        size -= 1
    return size


def edge_c(b, cx, cy, s, size, fill=WHITE, face=DARUMA, weight=None, name="Label"):
    """White lettering with the berry edge, centred by ink: readable over any world."""
    f = font(face, size, weight)
    l, t, r, bt = f.getbbox(s)
    b.write((cx - (l + r) / 2, cy - (t + bt) / 2), s, f, fill, ps_name(face, weight), size, stroke=max(3, size // 9),
            edge=TITLE_EDGE, align="Center", name=name)


def stack(b, cx, cy, lines, size, fill, pitch, face=DARUMA, name="Label"):
    for i, s in enumerate(lines):
        centred(b, cx, cy + (i - (len(lines) - 1) / 2) * pitch, s, size, face=face, fill=fill, name=name)


def aa(draw_fn, size, S=3):
    """Draw a mask at S times the size and bring it down smooth."""
    m = Image.new("L", (size[0] * S, size[1] * S))
    draw_fn(ImageDraw.Draw(m), S)
    return m.resize(size, Image.LANCZOS)


def poly_mask(size, pts):
    return aa(lambda d, S: d.polygon([(x * S, y * S) for x, y in pts], fill=255), size)


# ---------------------------------------------------------------- new primitives

def slim_art(w, h, rim=6, drop=8, rimc=ROSE, slab=SLAB, seed=3, c=None, pal=(DARK, LITD), alpha=1.0):
    """A thin card for HUD readouts over the world: a deep berry weave inside a rose rim,
    chamfered and hand-wobbled like the kit's cards, the slab straight below."""
    c = c or max(8, min(24, int(min(w, h) * .28)))
    size = (w, h + drop)
    body = lambda inset, dy, cc, sd, amp: poly_mask(size, wobble(chamfer(inset, inset + dy, w - 2 * inset, h - 2 * inset, cc), amp, sd))
    im = Image.new("RGBA", size)
    im.paste(Image.new("RGBA", size, tuple(slab) + (255,)), (0, 0), body(1, drop, c, seed, .7))
    im.paste(Image.new("RGBA", size, tuple(rimc) + (255,)), (0, 0), body(1, 0, c, seed, .7))
    face = body(1 + rim, 0, max(4, c - rim // 2), seed + 1, .5)
    im.paste(wv(*pal).crop((0, 0, w, h + drop)), (0, 0), face)
    return fade(im, alpha) if alpha < 1 else im


def slim(b, x, y, w, h, name="Card", key="slim", **kw):
    tag_ = "%s-%dx%d-%s" % (key, w, h, "-".join("%02x" % v for v in (kw.get("rimc") or ROSE)))
    b.place(tag_ + ("-a%d" % int(kw["alpha"] * 100) if "alpha" in kw else ""), slim_art(w, h, **kw), x, y, name)


def pill_art(w, h, face, slab, lit=None, drop=3):
    size = (w, h + drop)
    full = lambda dy: aa(lambda d, S: d.rounded_rectangle((0, dy * S, w * S - 1, (h + dy) * S - 1), radius=h * S // 2, fill=255), size)
    im = Image.new("RGBA", size)
    im.paste(Image.new("RGBA", size, tuple(slab) + (255,)), (0, 0), full(drop))
    top = full(0)
    im.paste(Image.new("RGBA", size, tuple(lit or mix(face, WHITE, .4)) + (255,)), (0, 0), top)
    inner = aa(lambda d, S: d.rounded_rectangle((0, 2 * S, w * S - 1, h * S - 1), radius=h * S // 2, fill=255), size)
    im.paste(Image.new("RGBA", size, tuple(face) + (255,)), (0, 0), ImageChops.multiply(top, inner))
    return im


def pill(b, x, y, w, h, face, slab, key):
    b.place("pill-%s-%dx%d-%02x%02x%02x" % (key, w, h, *face), pill_art(w, h, face, slab), x, y, "Pill")


def bar(b, x, y, w, h, frac, col, key="bar"):
    """A sunk track with a lit fill: the ultimate charge."""
    b.place("%s-track-%dx%d" % (key, w, h), pill_art(w, h, WELL, WELL_RIM, lit=(0xB8, 0x46, 0x5A), drop=0), x, y, "Track")
    fw = max(h, int(w * frac)) if frac > 0 else 0
    if fw:
        b.place("%s-fill-%dx%d-%02x%02x%02x" % (key, fw, h, *col), pill_art(fw, h, col, mix(col, (0, 0, 0), .45), drop=0), x, y, "Fill")


def seat_pic(b, x, y, s, seat, portrait, key, bg=None):
    """A portrait on its seat colour, in a small chamfered card: a lit rim, the seat colour as the
    ground and a slab below in a deep shade of it."""
    drop, c = max(4, s // 10), max(6, int(s * .22))
    size = (s, s + drop)
    lit, deep = mix(seat, WHITE, .5), mix(seat, (0, 0, 0), .6)
    body = lambda inset, dy: poly_mask(size, chamfer(inset, inset + dy, s - 2 * inset, s - 2 * inset, max(3, c - inset)))
    im = Image.new("RGBA", size)
    im.paste(Image.new("RGBA", size, deep + (255,)), (0, 0), body(1, drop))
    im.paste(Image.new("RGBA", size, lit + (255,)), (0, 0), body(1, 0))
    rim = max(3, s // 16)
    inner = body(1 + rim, 0)
    ground = Image.new("RGBA", size, tuple(bg or seat) + (255,))
    if portrait is not None:
        p = portrait.convert("RGBA")
        k = (s - 2 * rim) / min(p.width, p.height)
        p = p.resize((round(p.width * k), round(p.height * k)), Image.LANCZOS)
        ground.alpha_composite(p, (rim + 1 + ((s - 2 * rim) - p.width) // 2, rim + 1))
    im.paste(ground, (0, 0), inner)
    b.place("%s-%dx%d" % (key, s, s), im, x, y, "Portrait")


def keycap(b, x, y, w, h, label, size=None, kind="plain-soft"):
    b.place("cap-%s-%dx%d" % (kind, w, h), thin_button_art(kind, w, h), x, y, "Key")
    if label == "=":  # the display face has no real equals sign
        centred(b, x + w / 2, y + h * .44, label, 30, face=GRAND, fill=BUTTONS[kind][1], weight=700, name="Key")
        return
    centred(b, x + w / 2, y + h * .44, label, size or int(h * .5), fill=BUTTONS[kind][1], name="Key")


def thin(b, x, y, w, h, kind, lines, size, name="Button"):
    """One thin-slab button, its words centred on its face; yellow only for the chosen one."""
    b.place("thin-%s-%dx%d" % (kind, w, h), thin_button_art(kind, w, h), x, y, name)
    lines = [lines] if isinstance(lines, str) else lines
    stack(b, x + w / 2, y + h * .44, lines, size, BUTTONS[kind][1], int(size * 1.08), name="Label")


def keyhint(b, cx, cy, parts, size=28):
    """The bottom hint line: key caps and words, white with the berry edge, centred as a group."""
    f = font(GRAND, size, 650)
    items, total = [], 0
    for kind, s in parts:
        w = 18 + int(font(DARUMA, 22).getlength(s)) + 18 if kind == "key" else int(f.getlength(s))
        items.append((kind, s, w))
        total += w + 12
    x = cx - (total - 12) / 2
    for kind, s, w in items:
        if kind == "key":
            keycap(b, int(x), int(cy - 22), w, 44, s, 22)
        else:
            f2 = font(GRAND, size, 650)
            l, t, r, bt = f2.getbbox(s)
            b.write((x, cy - (t + bt) / 2 - 2), s, f2, WHITE, ps_name(GRAND, 650), size, stroke=3, edge=TITLE_EDGE, name="Hint")
        x += w + 12


def tag(b, x, y, w, h, label, kind):
    """A small pill in a button colour, its label a dark shade of it."""
    paint = BUTTONS[kind][0]
    from bootkit import recolor, slice3
    pill_ = slice3(recolor(art("login3-tabs-pill"), *paint) if paint else art("login3-tabs-pill"), w, h)
    b.place("tagpill-%s-%dx%d" % (kind, w, h), pill_, x, y, "Tag")
    centred(b, x + w / 2, y + h * .47, label, int(h * .5), fill=CREAM if kind == "red" else GREEN_DARK if kind == "go" else BUTTONS[kind][1], name="Tag")


# ---------------------------------------------------------------- the score strip

def parse_rows(m):
    rows = {}
    for L in m["layers"]:
        idx = None
        for p in L.get("path", []) + [L["name"]]:
            mt = re.match(r"ScoreRow(\d)", p)
            if mt:
                idx = int(mt.group(1))
        if idx is None:
            continue
        r = rows.setdefault(idx, {"idx": idx})
        n, k = L["name"], L["kind"]
        if k == "sprite" and n == "PlayerPortrait":
            r["portrait"] = L["asset"]
        elif k == "sprite":
            pass
        elif k == "text":
            t = L["text"]
            if re.fullmatch(r"P\d", t):
                r["tag"] = t
            elif re.fullmatch(r"\d+", t):
                r["score"], r["local"] = t, L["color"].upper() == "FFC440"
            else:
                r["name"] = t if t != "TAYA" else r.get("name")
                if t == "TAYA":
                    r["taya"] = True
        elif k == "raster":
            if n == "SeatSwatch":
                r["seat"] = tuple(int(L["tint"][i:i + 2], 16) for i in (0, 2, 4))
            elif n == "StateBadge":
                r["badge"], r["badge_tint"] = L["file"], L["tint"].upper()
            elif n == "UltFill":
                r["ult"] = L["w"]
            elif n == "UltTrack":
                r["track"] = L["w"]
            elif n == "TayaTab":
                r["taya"] = True
            elif n == "LocalMark":
                r["local"] = True
            elif n.endswith("shapes"):
                r["crown"], r["crown_file"] = True, L["file"]
    return [rows[i] for i in sorted(rows)]


def crown_img(bid, fname):
    im = raster(bid, fname)
    top = im.crop((0, 0, im.width, 24))
    return top.crop(visible(top, 60))


def score_row(b, bid, r, x, y, w, h, names):
    seat = r.get("seat") or SEATS[r["idx"]]
    slim(b, x, y, w, h, name="Score " + r.get("tag", ""), key="score")
    seat_pic(b, x + 9, y + 8, h - 18, seat, asset(r["portrait"]) if r.get("portrait") else None, "seatpic-%02x%02x%02x-%s" % (*seat, Path(r.get("portrait", "x")).stem))
    tx = x + h - 4
    left(b, tx, y + 20, r.get("tag", ""), 22, fill=seat, name="Seat")
    left(b, tx, y + 46, r.get("score", "0"), 38, fill=YELLOW if r.get("local") else CREAM, name="Score")
    if r.get("track"):
        bar(b, tx, y + h - 17, x + w - 14 - tx, 8, r.get("ult", 0) / r["track"], seat, "ult")
    if r.get("badge"):
        fitted(raster(bid, r["badge"]), "state-" + r["badge_tint"] + r["badge"], b, x + w - 30, y + 26, 36, "State")
    if r.get("taya"):
        tag(b, x + w - 104, y - 16, 104, 38, "TAYA", "red")
    if r.get("crown"):
        cr = crown_img(bid, r["crown_file"])
        cr = cr.resize((int(cr.width * 1.7), int(cr.height * 1.7)), Image.LANCZOS)
        b.place("crown", cr, x + 12, y - cr.height + 4, "Crown")
    if r.get("local"):
        pill(b, x + 24, y + h + 8, w - 48, 8, YELLOW, YELLOW_SLAB, "local")
    if names and r.get("name"):
        edge_c(b, x + w / 2, y + h + 34, r["name"], 26, name="Name")


def clock(b, bid, rows, x, y, w, h, text_):
    slim(b, x, y, w, h, name="Round clock", key="clock")
    badge = raster(bid, rows[0]["badge"]) if rows and rows[0].get("badge") else None
    if badge is not None:
        fitted(badge, "can-" + rows[0]["badge"], b, x + 36, y + h / 2 - 2, 44, "Can")
    centred(b, x + w / 2 + 18, y + h / 2 - 2, text_, 48, fill=CREAM, name="Clock")
    slim(b, x + 10, y + h + 14, w - 20, 30, name="Round pips", key="pips", rim=4, drop=5, c=10)
    for i in range(8):
        seat = SEATS[i % 4]
        px, py = x + 24 + i * ((w - 48 - 14) / 7), y + h + 22
        if i == 0:
            b.place("pip-on-%02x%02x%02x" % seat, pill_art(14, 14, seat, mix(seat, (0, 0, 0), .5), drop=2), px, py, "Pip")
        else:
            b.place("pip-%02x%02x%02x" % seat, pill_art(14, 14, SLAB, SLAB, lit=seat, drop=0), px, py, "Pip")


def strip(b, bid, rows, x0, y0, cw, ch, gap, clock_w, names, clock_text="01:30"):
    x = x0
    layout = [("row", 0), ("row", 1), ("clock", None), ("row", 2), ("row", 3)]
    byidx = {r["idx"]: r for r in rows}
    for kind, i in layout:
        if kind == "clock":
            clock(b, bid, rows, x, y0, clock_w, ch, clock_text)
            x += clock_w + gap
        elif i in byidx:
            score_row(b, bid, byidx[i], x, y0, cw, ch, names)
            x += cw + gap
    return x


# ---------------------------------------------------------------- desktop spectator

def spectator_base(bid, controls_hint, mode_label):
    b = Board()
    world(b, CAPTURES / bid / "background.png", dim=None)
    b.group = "Scores"
    m = manifest(bid)
    strip(b, bid, parse_rows(m), 408, 20, 214, 82, 8, 216, names=True)
    b.group = "Mode"
    tag(b, 960 - 125, 962, 250, 48, mode_label, "yellow")
    return b, m


def match_spectator(bid):
    b, _ = spectator_base(bid, None, "FREE CAMERA")
    b.group = "Hint"
    keyhint(b, 960, 1040, [("key", "TAB"), ("text", "show controls")])
    return b


def match_spectator_controls(bid):
    b, _ = spectator_base(bid, None, "FREE CAMERA")
    b.group = "Hint"
    keyhint(b, 960, 1040, [("key", "TAB"), ("text", "hide controls"), ("text", "|"), ("key", "H"), ("text", "hide HUD")])
    px, py, pw, ph = 270, 186, 1380, 676
    dark_panel(b, px, py, pw, ph, seed=61, group="Spectator controls")
    b.group = "Spectator controls"
    left(b, px + 56, py + 74, "SPECTATOR CONTROLS", 56, fill=CREAM, name="Heading")
    f = font(GRAND, 32, 600)
    cx_ = px + pw - 56 - int(f.getlength("close"))
    left(b, cx_, py + 74, "close", 32, face=GRAND, fill=CREAM, weight=600, caps=False, name="Close hint")
    keycap(b, cx_ - 112, py + 52, 100, 46, "TAB", 24)
    groups = [(0, "WATCH", [("G", "Next player"), ("V", "First / third person"), ("K", "Automatic camera"), ("F", "Free camera")]),
              (1, "FLY", [("SPACE", "Rise"), ("L CTRL", "Lower"), ("L SHIFT", "Move faster"), ("=", "Zoom / camera speed")]),
              (0, "MATCH TIME", [("P", "Pause / resume match"), ("T", "Cycle slow motion"), ("Y", "Instant replay")]),
              (1, "CAMERA TOOLS", [("B", "Save camera position"), ("N", "Return to saved position"), ("H", "Hide / show HUD")])]
    ytop = {0: py + 124, 1: py + 124}
    for col, head_, keys in groups:
        gx, gy = px + 56 + col * 650, ytop[col]
        tw = int(font(DARUMA, 26).getlength(head_)) + 52
        tag(b, gx, gy, tw, 42, head_, "yellow")
        for i, (k, d) in enumerate(keys):
            ry = gy + 54 + i * 51
            keycap(b, gx, ry, 188, 42, k, 24)
            left(b, gx + 214, ry + 22, d, 31, face=GRAND, fill=CREAM, weight=600, caps=False, name="Action")
        ytop[col] = gy + 54 + len(keys) * 51 + 12
    left(b, px + 56, py + ph - 46, "Movement keys: move. Mouse: look. F1-F4: player 1-4. Wheel: fine zoom / speed.", 31,
         face=GRAND, fill=TAN, weight=600, caps=False, name="Note")
    return b


def match_spectator_replay(bid):
    b = Board()
    world(b, CAPTURES / bid / "background.png", dim=None)
    b.group = "Replay"
    # Letterbox bands: dark weave with a rose edge, so the picture between them stays clear.
    for y, h, flip in ((0, 78, False), (1080 - 78, 78, True)):
        band = Image.new("RGBA", (1920, h))
        band.paste(wv(DARK, LITD).crop((0, 0, 1920, h)), (0, 0))
        d = ImageDraw.Draw(band)
        d.rectangle((0, h - 6 if not flip else 0, 1919, h - 1 if not flip else 5), fill=ROSE + (255,))
        b.place("band-%s" % ("bottom" if flip else "top"), band, 0, y, "Band")
    tag(b, 28, 20, 190, 40, "REPLAY", "yellow")
    left(b, 244, 40, "LAST PLAY", 44, fill=CREAM, name="Title")
    f = font(GRAND, 30, 600)
    ret = "LIVE PLAY CONTINUES"
    keys = [("text", ret), ("text", "·"), ("key", "Y"), ("text", "OR"), ("key", "ESC"), ("text", "TO RETURN")]
    # right aligned: measure first
    total = sum((int(f.getlength(s)) if k == "text" else int(font(DARUMA, 22).getlength(s)) + 36) + 14 for k, s in keys) - 14
    x = 1892 - total
    for kind, s in keys:
        if kind == "key":
            w = int(font(DARUMA, 22).getlength(s)) + 36
            keycap(b, x, 1001, w, 44, s, 22)
        else:
            w = int(f.getlength(s))
            l, t, r, bt = f.getbbox(s)
            b.write((x, 1023 - (t + bt) / 2), s, f, CREAM, ps_name(GRAND, 600), 30, name="Note")
        x += w + 14
    # progress: the filled share of the replay (350 of 1920), a lime fill on a sunk track
    bar(b, 28, 1015, 1160, 18, 350 / 1920, LIME, "replay")
    return b


# ---------------------------------------------------------------- the phone HUD

def touch_dark(b, x, y, w, h, key, alpha=.74, on=False):
    from bootkit import dark_card_art
    b.place("touch-%s-%dx%d" % (key, w, h), fade(dark_card_art(w, h, on), alpha), x, y, "Touch " + key)


def disc(b, cx, cy, d, rimc, face, slabc=None, key="disc", alpha=1.0, drop=0, weave_=False):
    """A round control: a lit rim, a face, an optional slab straight below."""
    size = (d, d + drop)
    circ = lambda inset, dy: aa(lambda dr, S: dr.ellipse((inset * S, (inset + dy) * S, (d - inset) * S - 1, (d - inset + dy) * S - 1), fill=255), size)
    im = Image.new("RGBA", size)
    if drop:
        im.paste(Image.new("RGBA", size, tuple(slabc) + (255,)), (0, 0), circ(1, drop))
    im.paste(Image.new("RGBA", size, tuple(rimc) + (255,)), (0, 0), circ(1, 0))
    rim = max(4, d // 22)
    fill = wv(*face).crop((0, 0, d, d + drop)) if weave_ else Image.new("RGBA", size, tuple(face) + (255,))
    im.paste(fill, (0, 0), circ(1 + rim, 0))
    im = fade(im, alpha) if alpha < 1 else im
    b.place("%s-%d-%02x%02x%02x" % (key, d, *rimc), im, cx - d / 2, cy - d / 2, "Disc")


def stick(b, m):
    base = [L for L in m["layers"] if L["name"] == "MoveStick"][0]
    knob = [L for L in m["layers"] if L["name"] == "Knob"][0]
    bb = visible(raster(base and m["id"], base["file"]))
    kb = visible(raster(m["id"], knob["file"]))
    d = bb[2] - bb[0]
    cx, cy = base["x"] + (bb[0] + bb[2]) / 2, base["y"] + (bb[1] + bb[3]) / 2
    disc(b, cx, cy, d, ROSE, (DARK, LITD), key="stick", alpha=.62, weave_=True)
    kd = kb[2] - kb[0]
    kcx, kcy = knob["x"] + (kb[0] + kb[2]) / 2, knob["y"] + (kb[1] + kb[3]) / 2
    disc(b, kcx, kcy - 6, kd, (0xDC, 0xEE, 0x72), (0xBB, 0xD6, 0x48), slabc=(0x6B, 0x8A, 0x1A), key="knob", alpha=.86, drop=12)


TOUCH_PLAIN = {"Touch_Sprint": 0, "Touch_Jump": 0, "Touch_Grab": 0, "Touch_Lunge": 0, "Touch_EmoteWheel": 0,
               "Touch_Interact": 0, "Touch_Crouch": 0}


def touch_buttons(b, m):
    bid = m["id"]
    layers = m["layers"]
    for L in layers:
        n = L["name"]
        if L["kind"] != "raster" or not n.startswith("Touch_") or "Spectator" in n:
            continue
        bb = visible(raster(bid, L["file"]))
        x, y, w, h = L["x"] + bb[0], L["y"] + bb[1], bb[2] - bb[0], bb[3] - bb[1]
        kids = [K for K in layers if K.get("path") and K["path"][-1] == n]
        ic = next((K for K in kids if K["kind"] == "raster" and K["name"] in ("VerbIcon", "AbilityIcon")), None)
        label = next((K["text"] for K in kids if K["kind"] == "text"), None)
        big = n == "Touch_SpecialAbility"
        b.group = "Touch " + n[6:]
        if big:
            b.place("touch-throw-%dx%d" % (w, h), fade(paint_card_art(w, h, "go"), .8), x, y, "Touch Throw")
            if ic:
                icon(b, raster(bid, ic["file"]), "throw", L["x"] + ic["x"] - L["x"] + ic["w"] / 2, ic["y"] + ic["h"] / 2, GREEN_DARK, int(ic["h"] * 1.05))
            continue
        touch_dark(b, x, y, w, h, "verb", alpha=.74)
        if ic is None:
            continue
        cx, cy = ic["x"] + ic["w"] / 2, ic["y"] + ic["h"] / 2
        if ic["name"] == "VerbIcon":
            icon(b, raster(bid, ic["file"]), "verb-" + n[6:].lower(), cx, cy, CREAM, int(ic["h"] * .98))
        else:
            fitted(raster(bid, ic["file"]), "ability-" + n[6:].lower(), b, cx, cy - 6, int(ic["w"] * 1.0), "Ability art")
        if label:
            ty = y + h - 20
            centred(b, x + w / 2, ty, label, 30, fill=CREAM, name="Label")


def tool_buttons(b, m, hold):
    """HOLD INFO and MENU: yellow buttons at their places, each a thumb-sized target."""
    b.group = "Player tools"
    y, h = 30, 140
    if hold:
        b.place("tool-hold", button_art("yellow", 154, h), 1330, y, "Button HOLD INFO")
        stack(b, 1330 + 77, y + h * FACE_LIP, ["HOLD", "INFO"], 36, YELLOW_INK, 40, name="Label")
    b.place("tool-menu", button_art("yellow", 154, h), 1492, y, "Button MENU")
    centred(b, 1492 + 77, y + h * FACE_LIP, "MENU", 40, fill=YELLOW_INK, name="Label")


def sandbox(b, m):
    L = next((L for L in m["layers"] if L["name"] == "SandboxPaper"), None)
    if not L:
        return
    t = next(K for K in m["layers"] if K["kind"] == "text" and "COOLDOWNS" in K["text"])
    b.group = "Sandbox"
    slim(b, 10, 262, 268, 138, name="No cooldowns", key="sandbox", alpha=.9)
    centred(b, 144, 306, "NO COOLDOWNS", fit_size("NO COOLDOWNS", 30, 236), fill=CREAM, name="Label")
    tag(b, 74, 330, 140, 48, t["text"].split("\n")[1], "plain")


def vitals(b, m):
    L = next((L for L in m["layers"] if L["name"].startswith("Vitals")), None)
    if not L:
        return
    por = next(K for K in m["layers"] if K["name"] == "VitalsPortrait")
    b.group = "Vitals"
    slim(b, 14, 886, 300, 164, name="Vitals", key="vitals", alpha=.92)
    seat_pic(b, 30, 900, 112, SEATS[1], asset(por["asset"]), "vitals-" + Path(por["asset"]).stem)
    fitted(asset("Assets/TumbangPreso/Resources/UI/portraits/tsinelas.png"), "vitals-slipper", b, 232, 954, 120, "Slipper")
    pill(b, 34, 1022, 260, 14, YELLOW, YELLOW_SLAB, "foot")


def power_seals(b, m, bid):
    if not any(L["name"] == "Power0" for L in m["layers"]):
        return
    b.group = "Powers"
    keys = [K for K in m["layers"] if K["name"] == "BindingGlyph"]
    pw = [K for K in m["layers"] if K["name"] == "PowerIcon"]
    for i, (x0, kg, ic) in enumerate(((1668, keys[0], pw[0]), (1796, keys[1], pw[1]))):
        slim(b, x0, 946, 112, 104, name="Power %d" % (i + 1), key="power", alpha=.92)
        fitted(raster(bid, ic["file"]), "power-icon-%d" % i, b, x0 + 56, 984, 62, "Power art")
        fitted(asset(kg["asset"]), "bind-%d" % i, b, x0 + 56, 1030, 28, "Binding")
    ult = next(K for K in m["layers"] if K["name"] == "PowerIcon" and K["x"] == 901)
    disc(b, 960, 950, 150, ROSE, (DARK, LITD), slabc=SLAB, key="ult", alpha=.92, drop=8, weave_=True)
    fitted(raster(bid, ult["file"]), "ult-icon", b, 960, 924, 64, "Ultimate art")
    pct = next(K for K in m["layers"] if K["kind"] == "text" and K["text"].endswith("%"))
    centred(b, 960, 968, pct["text"], 36, fill=CREAM, name="Charge")
    fitted(asset(keys[2]["asset"]), "bind-ult", b, 960, 1034, 34, "Binding")


def reticle(b, m, bid):
    b.group = "Reticle"
    for n, k in (("Reticle", "reticle"), ("CanArrow", "canarrow")):
        L = next((L for L in m["layers"] if L["name"] == n), None)
        if L:
            b.place(k, raster(bid, L["file"]), L["x"], L["y"], n)


def mobile_hud_board(bid, hold=True, rows=True):
    b = Board()
    world(b, CAPTURES / bid / "background.png", dim=None)
    m = manifest(bid)
    if rows:
        b.group = "Scores"
        # The tool buttons sit over the right end of the original strip, so the strip starts at the left edge.
        strip(b, bid, parse_rows(m), 56, 18, 220, 82, 8, 236, names=False)
    reticle(b, m, bid)
    vitals(b, m)
    power_seals(b, m, bid)
    stick(b, m)
    touch_buttons(b, m)
    sandbox(b, m)
    tool_buttons(b, m, hold and any(L["name"] == "TouchAbilityInfo" or L.get("path", [""])[-1] == "TouchAbilityInfo" for L in m["layers"]))
    return b, m


def mobile_hud(bid):
    return mobile_hud_board(bid)[0]


def mobile_hud_classic(bid):
    return mobile_hud_board(bid)[0]


# ---------------------------------------------------------------- guided training

def mobile_guided(bid):
    b, m = mobile_hud_board(bid, rows=False)
    t = {K["text"]: K for K in m["layers"] if K["kind"] == "text"}
    b.group = "Objective"
    px, py, pw, ph = 11, 11, 722, 420
    dark_panel(b, px, py, pw, ph, seed=62, group="Objective")
    b.group = "Objective"
    left(b, px + 50, py + 66, "01 / 20", 34, fill=TAN, name="Step")
    left(b, px + 50, py + 140, "READY UP", 64, fill=CREAM, name="Lesson")
    b.place("guided-skip", button_art("yellow", 150, 100), px + 382, py + 34, "Button SKIP")
    centred(b, px + 382 + 75, py + 34 + 100 * FACE_LIP, "SKIP", 36, fill=YELLOW_INK, name="Label")
    b.place("guided-quit", button_art("red", 150, 100), px + 548, py + 34, "Button QUIT")
    centred(b, px + 548 + 75, py + 34 + 100 * FACE_LIP, "QUIT", 36, fill=CREAM, name="Label")
    # twenty steps of the route, the first one reached
    sx, sw = px + 50, (pw - 100 - 19 * 4) / 20
    for i in range(20):
        x = sx + i * (sw + 4)
        if i == 0:
            b.place("route-on", pill_art(int(sw), 14, YELLOW, YELLOW_SLAB, drop=3), x, py + 196, "Step")
        else:
            b.place("route-off", pill_art(int(sw), 14, WELL, WELL_RIM, lit=(0xC0, 0x4C, 0x60), drop=0), x, py + 196, "Step")
    sent = next(s for s in t if s.startswith("Every match"))
    left(b, px + 50, py + 262, sent, fit_size(sent, 29, pw - 100, GRAND, 600), face=GRAND, fill=CREAM, weight=600, caps=False, name="Lesson text")
    keyf = next(L for L in m["layers"] if L["name"] == "Key_F")
    fitted(asset(keyf["asset"]), "guided-key-f", b, px + 78, py + 330, 62, "Key")
    left(b, px + 126, py + 330, "READY UP", 32, fill=CREAM, name="Key action")
    bar(b, px + 50, py + 376, pw - 100, 16, 0.0, LIME, "guided")
    # who is Taya: the reveal plate and the seat chip below it
    b.group = "Taya reveal"
    b.place("taya-plate", paint_card_art(447, 166, "red"), 736, 250, "Taya plate")
    centred(b, 736 + 447 / 2, 250 + 83, "TAYA", 98, fill=CREAM, name="Taya")
    slim(b, 856, 404, 208, 84, name="Who", key="taya-who")
    seat_pic(b, 868, 414, 62, SEATS[0], asset("Assets/TumbangPreso/Resources/UI/portraits/dante.png"), "taya-seat")
    left(b, 944, 446, "YOU", 40, fill=YELLOW, name="You")
    return b


# ---------------------------------------------------------------- held skills sheet

def mobile_powers(bid):
    b, m = mobile_hud_board(bid)
    t = [K for K in m["layers"] if K["kind"] == "text"]
    px, py, pw, ph = 47, 186, 1826, 748
    # No panel behind them: dark cards on a dark panel would blend.
    b.group = "Your powers"
    edge_c(b, px + 240, py + 78, "YOUR POWERS", 58, name="Heading")
    edge_c(b, px + pw - 170, py + 78, "SWIPE TO READ", 32, name="Hint")
    cards = []
    names = [K for K in t if K.get("size") == 34]
    for K in names:
        i = int(K["path"][-1][-1])
        cards.append(dict(name=K["text"], sub=next(S["text"] for S in t if S["path"][-1] == K["path"][-1] and "·" in S["text"]),
                          body=next(S["text"] for S in t if S["path"][-1] == K["path"][-1] and S["text"] != K["text"] and "·" not in S["text"]),
                          icon=next(L for L in m["layers"] if L["name"] == "PowerCard%d shapes" % i)))
    y = py + 118
    for c in cards:
        lines = []
        for para_ in c["body"].split("\n\n"):
            lines += wrap_lines(para_, 31, pw - 240) + ([""] if para_ is not c["body"].split("\n\n")[-1] else [])
        h = 140 + 42 * len(lines) + 40
        x = px + 40
        frame(b, x, y, pw - 80, h, name=c["name"].title(), dark=True)
        framed(b, x + 34, y + 30, 132, 132, raster(bid, c["icon"]["file"]), "power-" + c["name"].lower())
        left(b, x + 200, y + 70, c["name"], 48, fill=CREAM, name="Power")
        left(b, x + 200, y + 126, c["sub"], 32, face=GRAND, fill=YELLOW, weight=700, caps=False, name="Cooldown")
        ly = y + 190
        for s in lines:
            if s:
                left(b, x + 52, ly, s, 31, face=GRAND, fill=CREAM, weight=500, caps=False, name="Power text")
            ly += 42
        y += h + 20
    return b


def wrap_lines(s, size, width):
    f = font(GRAND, size, 500)
    lines, line = [], ""
    for word in s.split():
        trial = (line + " " + word).strip()
        if f.getlength(trial) > width and line:
            lines.append(line)
            line = word
        else:
            line = trial
    return lines + [line]


# ---------------------------------------------------------------- the phone spectator

def event_rows(b, m, bid, rows):
    seat_of = {}
    for r in rows:
        seat_of.setdefault(Path(r.get("portrait", "")).stem, r.get("seat") or SEATS[r["idx"]])
    ev = {}
    for L in m["layers"]:
        mt = next((re.match(r"Event(\d)$", p) for p in L.get("path", []) + [L["name"].split(" ")[0]] if re.match(r"Event(\d)$", p)), None)
        if mt:
            ev.setdefault(int(mt.group(1)), []).append(L)
    b.group = "Events"
    y = 150
    for i in sorted(ev):
        items = ev[i]
        sprites = [L for L in items if L["kind"] == "sprite"]
        glyph = next((L for L in items if L["kind"] == "raster" and L["name"] == "EventGlyph"), None)
        other = next((L for L in items if L["kind"] == "raster" and L["name"] == "OtherChip"), None)
        txt = next((L["text"] for L in items if L["kind"] == "text"), None)
        n = 1 + (1 if glyph or any(s["name"] == "EventAbility" for s in sprites) else 0) + (1 if other else 0)
        w = 24 + n * 56 + (96 if txt else 0)
        w = max(w, 150)
        x = 1900 - w
        slim(b, x, y, w, 60, name="Event %d" % (i + 1), key="event", alpha=.92)
        cx = x + 10
        actor = next(s for s in sprites if s["name"] in ("Portrait",) and s["path"][-1] == "ActorChip")
        stem = Path(actor["asset"]).stem
        seat_pic(b, cx, y + 6, 48, seat_of.get(stem, SEATS[0]), asset(actor["asset"]), "evpic-%s-%02x" % (stem, seat_of.get(stem, SEATS[0])[0]))
        cx += 54
        if glyph:
            fitted(raster(bid, glyph["file"]), "evglyph", b, cx + 22, y + 28, 44, "Event glyph")
            cx += 52
        ab = next((s for s in sprites if s["name"] == "EventAbility"), None)
        if ab:
            fitted(asset(ab["asset"]), "evability-" + Path(ab["asset"]).stem, b, cx + 22, y + 28, 48, "Event ability")
            cx += 52
        if other:
            op = next(s for s in sprites if s["path"][-1] == "OtherChip")
            seat_pic(b, cx, y + 6, 48, (0x5E, 0xA1, 0xF5), asset(op["asset"]), "evother")
            cx += 54
        if txt:
            right(b, x + w - 14, y + 28, txt, 36, fill=CREAM)
        y += 70


def phone_strip(b, m, bid, drawer):
    layers = {L["name"]: L for L in m["layers"]}
    texts = {L["path"][-1]: L["text"].split("\n") for L in m["layers"] if L["kind"] == "text" and L["path"][-1].startswith("SpectatorTouch") and not L["text"].startswith("Left stick")}
    order = ["SpectatorTouchAuto", "SpectatorTouchFollow", "SpectatorTouchPov", "SpectatorTouchPause", "SpectatorTouchTools"]
    b.group = "Spectator strip"
    for i, n in enumerate(order):
        x, y, w, h = 1128 + i * 155, 866, 148, 142
        touch_dark(b, x, y, w, h, "strip", alpha=.86)
        stack(b, x + w / 2, y + h / 2 - 4, texts[n], fit_size(max(texts[n], key=len), 28, w - 28), CREAM, 32, name="Label")


def spectator_touch_common(bid):
    b = Board()
    world(b, CAPTURES / bid / "background.png", dim=None)
    m = manifest(bid)
    rows = parse_rows(m)
    b.group = "Scores"
    strip(b, bid, rows, 56, 18, 220, 82, 8, 236, names=True)
    event_rows(b, m, bid, rows)
    stick(b, m)
    tool_buttons(b, m, True)
    b.group = "Mode"
    tag(b, 960 - 125, 962, 250, 48, "FREE CAMERA", "yellow")
    b.group = "Hint"
    edge_c(b, 620, 1044, "Left stick: fly  |  Drag right: look  |  More tools: camera and match", 28, face=GRAND, weight=650, name="Hint")
    phone_strip(b, m, bid, False)
    return b, m, rows


def mobile_spectator(bid):
    b, m, rows = spectator_touch_common(bid)
    b.group = "Can down"
    b.place("candown", paint_card_art(520, 100, "yellow"), 700, 236, "Can down")
    can = next(L for L in m["layers"] if L["name"] == "CanDownGlyph")
    fitted(raster(bid, can["file"]), "candown-glyph", b, 770, 286, 70, "Can")
    centred(b, 985, 286, "CAN DOWN", 64, fill=BUTTONS["yellow"][1], name="Banner")
    return b


def mobile_spectator_drawer(bid):
    b, m, rows = spectator_touch_common(bid)
    t = [L for L in m["layers"] if L["kind"] == "text"]
    # a score pop under the first player's name
    b.group = "Score pop"
    tag(b, 408 + 107 - 48, 158, 96, 42, "+100", "yellow")
    # the earned moment
    mp = next(L for L in m["layers"] if L["name"] == "MomentPortrait")
    b.group = "Earned moment"
    b.place("moment", paint_card_art(514, 100, "red"), 703, 172, "Earned moment")
    seat_pic(b, 726, 184, 76, SEATS[0], asset(mp["asset"]), "moment-pic")
    left(b, 826, 222, "Single Catch!", 56, fill=CREAM, name="Moment")
    # the cast callout over the stick
    cc = next(L for L in m["layers"] if L["name"] == "CastCalloutIcon")
    b.group = "Cast callout"
    disc(b, 249, 744, 140, mix(SEATS[2], WHITE, .3), (DARK, LITD), slabc=SLAB, key="callout", alpha=.95, drop=8, weave_=True)
    fitted(asset(cc["asset"]), "callout-icon", b, 249, 740, 86, "Ability art")
    edge_c(b, 249, 826, "DRIFT", 30, name="Callout")
    # the tools drawer
    px, py, pw, ph = 1108, 346, 812, 500
    dark_panel(b, px, py, pw, ph, seed=64, group="Spectator tools")
    b.group = "Spectator tools"
    tw = (pw - 72 - 14) // 2
    thin(b, px + 36, py + 34, tw, 76, "plain-soft", "CAMERA", 34, "Tab CAMERA")
    thin(b, px + 36 + tw + 14, py + 34, tw, 76, "yellow", "MATCH", 34, "Tab MATCH (chosen)")
    names = {L["path"][-1]: L["text"].split("\n") for L in t if L["path"][-1] in (
        "SpectatorTouchReplay", "SpectatorTouchSpeed", "SpectatorTouchFeed", "SpectatorTouchControls", "SpectatorTouchMenu", "SpectatorTouchDone")}
    grid = ["SpectatorTouchReplay", "SpectatorTouchSpeed", "SpectatorTouchFeed", "SpectatorTouchControls", "SpectatorTouchMenu", "SpectatorTouchDone"]
    cw = (pw - 72 - 28) // 3
    for i, n in enumerate(grid):
        x, y = px + 36 + (i % 3) * (cw + 14), py + 130 + (i // 3) * 128
        thin(b, x, y, cw, 112, "go" if n.endswith("Done") else "plain-soft", names[n], 31, "Tool")
    note = next(L["text"] for L in t if L["text"].startswith("Left stick: fly."))
    for i, s in enumerate(wrap_lines(note, 28, pw - 90)):
        centred(b, px + pw / 2, py + 402 + i * 40, s, 28, face=GRAND, fill=CREAM, weight=600, name="Help")
    return b


SCREENS = {"match-spectator": match_spectator, "match-spectator-controls": match_spectator_controls,
           "match-spectator-replay": match_spectator_replay, "mobile-hud": mobile_hud,
           "mobile-hud-classic": mobile_hud_classic, "mobile-guided": mobile_guided, "mobile-powers": mobile_powers,
           "mobile-spectator": mobile_spectator, "mobile-spectator-drawer": mobile_spectator_drawer}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--only", nargs="*", help="draw only these capture ids")
    args = parser.parse_args()
    OUT.mkdir(parents=True, exist_ok=True)
    for board_id, draw in SCREENS.items():
        if args.only and board_id not in args.only:
            continue
        draw(board_id).im.convert("RGB").save(OUT / (board_id + ".png"))
        print("drew", board_id)


if __name__ == "__main__":
    main()
