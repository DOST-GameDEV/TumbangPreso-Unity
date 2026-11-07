"""The live match screens in the boot kit: the HUD in its four states, the countdown, the arrival
caption, the taya reveal, can down, the catch replay, the three earned moments, the emote wheel,
the held skills sheet, the ultimate card, guided training and the training menu. Laid out from
the captures in Logs/ui-artboards, over each capture's own world.

    py -3 tools/ui_artboards/boot_match.py [--only match-hud-thrower,match-candown]

Previews land in ArtSource/ui/revamp-2026-10-30-boot/preview/<id>.png.

A live match keeps its readouts small and at the screen edges. Cards over the world are dark
with a visible weave and a rose rim (lime for the local player), lettering is white with the
berry edge, and the moments (can down, the taya reveal, earned moments) get a sunburst or
hand-cut warning tape as their focal point.
"""
import argparse
import colorsys
import functools
import json
import math
import random
from pathlib import Path

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageOps

from bootkit import (INK, BUTTONS, CARD, CREAM, DARUMA, GRAND, LIME, MAROON, ROOT, TAN, TAN_D, TITLE_EDGE, WHITE, WEAVE,
                     Board, art, button, button_art, centred, chamfer, dark_panel, font, frame, framed, left,
                     panel, para, ps_name, rule, slice3, sunburst_art, tag, text, thin_button_art, toggle, weave, wobble,
                     head, hazard_art, FACE_LIP, FACE_PILL, GREEN_INK, SOFT_RED)
from boot_menus import tape, warning_mark
from functools import partial
from bootkit import paint_card as _paint_card
from bootkit import well
paint_card = partial(_paint_card, pattern="checker")
from boot_record import clock_art
from boot_record import badge, edge, face, SEATS

CAPTURES = ROOT / "Logs" / "ui-artboards"
UI = ROOT / "Assets/TumbangPreso/Resources/UI"
OUT = ROOT / "ArtSource/ui/revamp-2026-10-30-boot/preview"

ROSE, SLAB, DEEP = (0xD0, 0x62, 0x76), (0x2A, 0x06, 0x10), (0x36, 0x0A, 0x15)
LIME_SLAB = (0x8E, 0xA6, 0x22)
AMBER, AMBER_INK = (0xFF, 0xC4, 0x40), (0xA8, 0x4E, 0x0C)


# Reading a capture

def manifest(cid):
    return json.loads((CAPTURES / cid / "manifest.json").read_text(encoding="utf-8"))


def lay(m, name, under=None):
    return [l for l in m["layers"] if l["name"] == name and (under is None or l["path"][-1] == under)]


def raster(cid, layer):
    return Image.open(CAPTURES / cid / layer["file"]).convert("RGBA")


def tint(layer):
    h = layer["tint"]
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def hexcol(h):
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def sprite(layer, scale=1.0, size=None):
    p = ROOT / "Assets/TumbangPreso" / layer["asset"].split("TumbangPreso/", 1)[1]
    im = Image.open(p).convert("RGBA")
    w, h = size or (round((layer["tr"][0] - layer["tl"][0]) * scale), round((layer["bl"][1] - layer["tl"][1]) * scale))
    return im.resize((max(1, w), max(1, h)), Image.LANCZOS)


def stem(layer):
    return layer["asset"].split("/")[-1][:-4]


def bright(im, thr=190):
    """The light marks of a raster (an icon on its dark plate), cropped to their ink."""
    a = np.asarray(im.convert("RGBA"), dtype=np.float32)
    lum = a[..., :3] @ np.array([.299, .587, .114], dtype=np.float32)
    keep = (a[..., 3] > 120) & (lum > thr)
    out = im.copy()
    out.putalpha(Image.fromarray((keep * 255).astype(np.uint8)))
    return out.crop(out.getchannel("A").getbbox())


def trim(im):
    return im.crop(im.getchannel("A").getbbox())


# Pieces

@functools.lru_cache(maxsize=None)
def _weave(dark, lit):
    """The weave at its full-screen scale, three screens wide (mirrored copies), so a long plate's crop never runs
    off the end and leaves a flat band."""
    one = weave(dark, lit)
    out = Image.new(one.mode, (one.width * 3, one.height))
    out.paste(one, (0, 0)); out.paste(ImageOps.mirror(one), (one.width, 0)); out.paste(one, (one.width * 2, 0))
    return out


def organic_mask(w, h, S, corner, tail=None, depth=0, seed=3):
    """A hand-cut outline at scale S: edges that wander, soft uneven corners, and optionally a
    swallowtail cut into the left end, the right end or both."""
    rnd = random.Random(seed * 17 + w * 3 + h)
    j = lambda: rnd.uniform(-.6, .6) * S
    W, H = w * S, h * S
    m = 3 * S
    steps = max(2, w // 34)
    top = [(m + (W - 2 * m) * i / steps, m + j()) for i in range(steps + 1)]
    bottom = [(W - m - (W - 2 * m) * i / steps, H - m + j()) for i in range(steps + 1)]
    pts = list(top)
    if tail in ("r", "both"):
        pts.append((W - m - depth * S, H / 2 + j()))
    pts += bottom
    if tail in ("l", "both"):
        pts.append((m + depth * S, H / 2 + j()))
    im = Image.new("L", (W, H))
    ImageDraw.Draw(im).polygon(pts, fill=255)
    # Blur and threshold so the corners round off by cutting, not with a compass.
    return im.filter(ImageFilter.GaussianBlur(corner * S * .55)).point(lambda v: 255 if v > 118 else 0)


def banner_art(w, h, face_top, face_low, lip, slab, tail=None, depth=0, drop=8, seed=3, corner=None):
    """One banner: a hand-cut body, a lit edge that is thick on top and thin at the sides, the face
    falling from light to dark, and its slab swept straight down so the two stay joined."""
    S = 3
    corner = corner if corner is not None else max(4, min(10, h * .1))
    body = organic_mask(w, h, S, corner, tail, depth, seed)
    size = (w * S, (h + drop) * S)
    base_ = Image.new("L", size)
    base_.paste(body, (0, 0))
    swept = base_
    for dy in range(S, drop * S + 1, S):
        swept = ImageChops.lighter(swept, ImageChops.offset(base_, 0, dy))
    # The lit edge runs along the top only, thick like the buttons' lip.
    inner = ImageChops.multiply(base_, ImageChops.offset(base_, 0, max(2, round(h * .11)) * S))
    fall = Image.new("RGBA", size)
    fd = ImageDraw.Draw(fall)
    for y in range(size[1]):
        t = min(1, y / (h * S))
        fd.line((0, y, size[0], y), fill=tuple(int(a + (b_ - a) * t) for a, b_ in zip(face_top, face_low)) + (255,))
    im = Image.new("RGBA", size)
    im.paste(tuple(slab) + (255,), (0, 0), swept)
    im.paste(tuple(lip) + (255,), (0, 0), base_)
    im.paste(fall, (0, 0), inner)
    return im.resize((w, h + drop), Image.LANCZOS)


PAINT = {k: BUTTONS[k][0] for k in ("yellow", "red", "plain-soft", "blue")}
PAINT["go"] = (LIME_SLAB, LIME, (0xD8, 0xF0, 0x6A))
INKS = {k: BUTTONS[k][1] for k in BUTTONS}


def plate_art(w, h, kind="plain-soft", drop=5, tail=None):
    """A small plate in a button's paint: the HUD has no colours of its own."""
    slab, face_, lit = PAINT[kind]
    return banner_art(w, h, face_, face_, lit, slab, tail, 14 if tail else 0, drop, seed=3, corner=5)


def plate(b, x, y, w, h, kind="plain-soft", name="Plate", drop=5):
    b.place("plate-%s-%dx%d-%d" % (kind, w, h, drop), plate_art(w, h, kind, drop), x, y, name)


def hcard(b, x, y, w, h, on=False, name="Card", drop=5, tail=None):
    plate(b, x, y, w, h, "go" if on else "plain-soft", name, drop)


def inked(im, colour):
    """An icon's shape in one ink, for white marks that would vanish on a cream plate."""
    out = Image.new("RGBA", im.size, tuple(colour) + (255,))
    out.putalpha(im.getchannel("A"))
    return out


TICKET_INK = (0x4A, 0x10, 0x18)
LEAN = .2  # HudDraw.Lean, the shape the match bar settled on.


def lean(im, k=LEAN):
    """The match bar's lean: the top shifts right, the foot stays put."""
    pad = round(k * im.height)
    return im.transform((im.width + pad, im.height), Image.AFFINE, (1, k, -k * im.height, 0, 1, 0), Image.BICUBIC)


def slip_art(w, h, kind="yellow", left_stub=0, right_stub=0, drop=8):
    """A paper slip built like the queue's raffle ticket: bitten ends, a torn line and notches before
    each stub, a thick lit top edge and a slab joined below."""
    slab, face_, lit = PAINT[kind]
    S, lip = 3, max(3, round(h * .1))
    W, H = w * S, (h + drop) * S
    body = Image.new("L", (W, H))
    d = ImageDraw.Draw(body)
    d.polygon(wobble([(0, 0), (W, 0), (W, h * S), (0, h * S)], .8 * S, w + h), fill=255)
    bite = h * S * .17
    for px in (0, W):
        d.ellipse((px - bite, h * S / 2 - bite, px + bite, h * S / 2 + bite), fill=0)
    tears = [t * S for t in (left_stub, w - right_stub if right_stub else 0) if t]
    notch = max(4, h * .09) * S
    for x in tears:
        for cy in (0, h * S):
            d.ellipse((x - notch, cy - notch, x + notch, cy + notch), fill=0)
    body = body.filter(ImageFilter.GaussianBlur(1.5 * S)).point(lambda v: 255 if v > 110 else 0)
    swept = body
    for dy in range(S, drop * S + 1, S):
        swept = ImageChops.lighter(swept, ImageChops.offset(body, 0, dy))
    im = Image.new("RGBA", (W, H))
    im.paste(tuple(slab) + (255,), (0, 0), swept)
    im.paste(tuple(lit) + (255,), (0, 0), body)
    im.paste(tuple(face_) + (255,), (0, 0), ImageChops.multiply(body, ImageChops.offset(body, 0, lip * S)))
    d = ImageDraw.Draw(im)
    dash = max(5, h // 9) * S
    for x in tears:
        y = int(notch + lip * S)
        while y < h * S - notch - dash / 2:
            d.rectangle((x - S, y, x + S, y + dash), fill=tuple(slab) + (255,))
            y += int(dash * 1.8)
    return im.resize((w, h + drop), Image.LANCZOS)


def slip(b, x, y, w, h, kind="yellow", name="Slip", left_stub=0, right_stub=0, drop=8):
    art = lean(slip_art(w, h, kind, left_stub, right_stub, drop))
    b.place("slip-%s-%dx%d-%d-%d-%d" % (kind, w, h, left_stub, right_stub, drop), art, x - (art.width - w) / 2, y, name)


def chip_art(w, h, color, c=10, slab=5):
    """A seat colour tile: hand-cut, lit on top, its slab joined below."""
    deep = tuple(int(v * .62) for v in color)
    low = tuple(color)
    return banner_art(w, h, tuple(color), low, tuple(min(255, int(v * 1.2)) for v in color), deep, None, 0, slab, seed=w, corner=max(3, c * .5))


def chip(b, x, y, s, color, hero, name="Seat colour"):
    art = lean(chip_art(s, s, color))
    b.place("chip-%02x%02x%02x-%d" % (tuple(color) + (s,)), art, x - (art.width - s) / 2, y, name)
    if hero:
        face(b, hero, x + s / 2, y + s / 2 + 1, int(s * .9))


def rays_art(w, h, color=(0xFF, 0xE0, 0x78), rays=20, peak=170, seed=1, power=1.2):
    """Hand-cut rays from the middle that fade out toward the edge, so the burst has no box."""
    S = 2
    m = Image.new("L", (w * S, h * S))
    d = ImageDraw.Draw(m)
    rnd = random.Random(seed)
    cx, cy, R = w * S / 2, h * S / 2, (w + h) * S
    for i in range(rays):
        a = 2 * math.pi * i / rays + rnd.uniform(-.03, .03)
        half = math.pi / rays / 2 * rnd.uniform(.8, 1.0)
        d.polygon([(cx, cy), (cx + R * math.cos(a - half), cy + R * math.sin(a - half)),
                   (cx + R * math.cos(a + half), cy + R * math.sin(a + half))], fill=255)
    m = m.resize((w, h), Image.LANCZOS)
    yy, xx = np.indices((h, w))
    r = np.hypot((xx - w / 2) / (w / 2), (yy - h / 2) / (h / 2))
    fall = np.clip(1 - r, 0, 1) ** power
    a = np.asarray(m, dtype=np.float32) / 255 * fall * peak
    im = Image.new("RGBA", (w, h), tuple(color) + (255,))
    im.putalpha(Image.fromarray(a.clip(0, 255).astype(np.uint8)))
    return im


def rays(b, cx, cy, w, h, key, **kw):
    b.place("rays-%s-%dx%d" % (key, w, h), rays_art(w, h, **kw), cx - w / 2, cy - h / 2, "Sunburst")


def fit_size(s, size, maxw, face_=DARUMA, weight=None):
    while size > 14 and font(face_, size, weight).getlength(s) > maxw:
        size -= 2
    return size


def wrap_lines(s, size, width, weight=450):
    from bootkit import wrap
    out = []
    for para_ in s.split("\n"):
        out += wrap(para_, size, width, GRAND, weight) if para_.strip() else [""]
    return out


# The HUD, read from the capture

GLASS = (0x14, 0x12, 0x1C)


def word(b, x, cy, text_, size, fill=WHITE, align="l", face_=DARUMA, weight=None, name="Label"):
    """Lettering straight on the world: a thin dark edge, so it holds on bright sky."""
    f = font(face_, size, weight)
    edge_w = max(2, round(size / 15))
    l, t, r_, bt = f.getbbox(text_)
    pad = max(8, size // 3)
    im = Image.new("RGBA", (r_ - l + pad * 2, bt - t + pad * 2))
    ImageDraw.Draw(im).text((pad - l, pad - t), text_, font=f, fill=tuple(fill) + (255,), stroke_width=edge_w, stroke_fill=GLASS + (255,))
    w = r_ - l
    px = x - pad if align == "l" else x - w / 2 - pad if align == "c" else x - w - pad
    key = "".join(ch for ch in text_ if ch.isalnum())
    b.place("word-%s-%d-%02x%02x%02x-%s" % (key, size, fill[0], fill[1], fill[2], weight), im, px, cy - (bt - t) / 2 - pad, name)


def number(b, x, cy, text_, size, fill=WHITE, align="l", name="Number"):
    """Readouts in heavy Grandstander: Darumadrop goes soft and thin at score size."""
    word(b, x, cy, text_, size, fill, align, GRAND, 800, name)


def glass_art(w, h, alpha=120, lit=(0xFF, 0xFF, 0xFF), line=0):
    """A leaning pane of dark glass, the one quiet ground the HUD allows."""
    S = 3
    im = Image.new("RGBA", (w * S, h * S))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((0, 0, w * S - 1, h * S - 1), 4 * S, fill=GLASS + (alpha,))
    if line:
        d.rounded_rectangle((0, h * S - line * S, w * S - 1, h * S - 1), S, fill=tuple(lit) + (255,))
    return lean(im.resize((w, h), Image.LANCZOS))


def players(m, cid):
    """Every seat in the capture, highest score first."""
    out = []
    for i in range(4):
        under = "ScoreRow%d" % i
        if not lay(m, under, "MatchScores"):
            continue
        st = lay(m, "StateBadge", under)
        out.append(dict(colour=tint(lay(m, "SeatSwatch", under)[0]), hero=stem(lay(m, "PlayerPortrait", under)[0]),
                        seat=lay(m, "SeatTag", under)[0]["text"], score=lay(m, "Score", under)[0]["text"],
                        local=bool(lay(m, "LocalMark", under)), leader=bool(lay(m, "ScoreRow%d shapes" % i, under)),
                        state=trim(raster(cid, st[0])) if st else None))
    return sorted(out, key=lambda q: -int(q["score"]))


CARD_INK = (0x52, 0x1E, 0x04)
# "swatch" puts one hand-cut shape behind each readout, for bright maps; "bare" leaves them on the world.
STYLE = "swatch"


def seconds(text_):
    mm, ss = text_.split(":")
    return int(mm) * 60 + int(ss)


def swatch_art(w, h, kind, seed=1, drop=6):
    """One hand-cut slanted shape in a button's paint, uneven ends and a joined slab: the simplest
    ground that still holds a number on a bright map."""
    slab, face_, lit = PAINT[kind]
    S = 3
    rnd = random.Random(seed * 13 + w + h)
    j = lambda v: rnd.uniform(-v, v) * S
    W, H = w * S, h * S
    k = h * S * LEAN
    pts = [(k + j(3), H * .1 + j(2)), (W * .55, j(2) + S), (W - 2 * S, H * .06 + j(2)), (W - k + j(3), H * .86 + j(2)), (W * .42, H - S + j(1)), (2 * S, H * .9 + j(2))]
    body = Image.new("L", (W, (h + drop) * S))
    ImageDraw.Draw(body).polygon(pts, fill=255)
    body = body.filter(ImageFilter.GaussianBlur(S)).point(lambda v: 255 if v > 118 else 0)
    swept = body
    for dy in range(S, drop * S + 1, S):
        swept = ImageChops.lighter(swept, ImageChops.offset(body, 0, dy))
    im = Image.new("RGBA", body.size)
    im.paste(tuple(slab) + (255,), (0, 0), swept)
    im.paste(tuple(lit) + (255,), (0, 0), body)
    im.paste(tuple(face_) + (255,), (0, 0), ImageChops.multiply(body, ImageChops.offset(body, 0, max(2, round(h * .07)) * S)))
    return im.resize((w, h + drop), Image.LANCZOS)


def swatch(b, x, y, w, h, kind, name="Ground", seed=1, drop=6):
    if STYLE == "swatch":
        b.place("swatch-%s-%dx%d-%d" % (kind, w, h, seed), swatch_art(w, h, kind, seed, drop), x, y, name)


def btn(b, cx, y, w, h, kind, text_, size, ink=None, name="Banner"):
    """A banner as the kit's own button, its label on the painted face."""
    b.place("btn-%s-%dx%d" % (kind, w, h), button_art(kind, w, h), cx - w / 2, y, name)
    if text_:
        centred(b, cx, y + h * FACE_LIP, text_, size, fill=ink or BUTTONS[kind][1], name=name)


def thin_pill(w, h, keep=4):
    """The kit's own lime pill, keeping its outline and lit top, with its deep slab reduced to a thin
    band that follows the bottom edge round both ends."""
    pill = slice3(art("login3-tabs-pill"), w, h).convert("RGBA")
    a = np.array(pill)
    rgb = a[..., :3].astype(np.float32)
    lum = rgb @ np.array([.299, .587, .114], dtype=np.float32)
    solid = a[..., 3] > 128
    mid = h // 2
    face_ = a[mid - 4, w // 2, :3].copy()
    slab = a[np.where(solid[:, w // 2])[0][-3], w // 2, :3].copy()
    dark = lum < float(lum[mid - 4, w // 2]) * .985
    soft = np.asarray(Image.fromarray((solid * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.2))) > 128
    still = np.zeros_like(soft)
    still[:-keep] = soft[keep:]
    a[dark & still, :3] = face_
    a[dark & ~still, :3] = slab
    return Image.fromarray(a, "RGBA")


def grey_track(w, h):
    """The kit's slider track in dark grey: its shape and shading kept, its maroon gone."""
    t = slice3(art("login3-tabs-track"), w, h).convert("RGBA")
    # The tonal steps stretched to what the maroon had: a deep well inside, a clearly lighter rim.
    src = t.convert("L")
    a_ = np.asarray(src)[np.asarray(t.getchannel("A")) > 128]
    lo, hi = float(np.percentile(a_, 3)), float(np.percentile(a_, 97))
    lum = src.point(lambda v: int(28 + max(0, min(1, (v - lo) / max(1, hi - lo))) * 92))
    return Image.merge("RGBA", (lum, lum, lum.point(lambda v: min(255, v + 5)), t.getchannel("A")))


def timer(b, time_, left_, sub):
    """The kit's slider grown into a rectangle, the lime fill draining inside it. The time has
    no outline: its ink is dark over the fill and cream over the empty track, split at the fill's edge."""
    x, y, w, h = 770, 8, 380, 74
    inset = 11
    b.place("timer-track", grey_track(w, h), x, y, "Track")
    full = w - inset * 2
    fw = int(full * left_)
    if fw > 0:
        pill = thin_pill(max(70, fw), h - 22)
        if fw < 70:
            pill = pill.crop((0, 0, fw, pill.height))
        b.place("timer-fill-%d" % fw, pill, x + inset, y + 9, "Time left")
    mask = Image.new("L", (w, h))
    d = ImageDraw.Draw(mask)
    for text_, f, cy in ((time_, font(DARUMA, 44), 33),):
        l, t, r_, bt = f.getbbox(text_)
        d.text((w / 2 - (r_ - l) / 2 - l, cy - (bt - t) / 2 - t), text_, font=f, fill=255)
    dark, light = Image.new("RGBA", (w, h), BUTTONS["go"][1] + (255,)), Image.new("RGBA", (w, h), CREAM + (255,))
    ink = light.copy()
    ink.paste(dark.crop((0, 0, inset + fw, h)), (0, 0))
    ink.putalpha(mask)
    b.place("timer-ink-%s-%s" % ("".join(c for c in time_ if c.isalnum()), fw), ink, x, y, "Time")
    word(b, x + w / 2, y + h + 16, sub, 22, align="c", face_=GRAND, weight=800, name="Round")


def clock(b, m, cid):
    t = lay(m, "TimeLeft")
    if not t:
        return
    b.group = "Clock"
    timer(b, t[0]["text"], min(1, seconds(t[0]["text"]) / 90), "ROUND 1 OF 4")


def see_through(im, alpha=.82):
    """The dark panel's art, slightly transparent so it does not hide the game."""
    out = im.copy()
    out.putalpha(out.getchannel("A").point(lambda v: int(v * alpha)))
    return out


WEAVE_SHRINK = 3.8


def edge_plate_art(w, h, colour, alpha=.82):
    """A dark woven plate running off the screen's left edge, its right end skewed at the match bar's lean."""
    S, drop = 3, 5
    W, H = w * S, (h + drop) * S
    k = h * S * LEAN
    pts = [(-S, 0), (W, 0), (W - k, h * S), (-S, h * S)]
    body = Image.new("L", (W, H))
    ImageDraw.Draw(body).polygon(pts, fill=255)
    swept = body
    for dy in range(S, drop * S + 1, S):
        swept = ImageChops.lighter(swept, ImageChops.offset(body, 0, dy))
    im = Image.new("RGBA", (W, H))
    im.paste(GLASS + (235,), (0, 0), swept)
    im.paste((0x8C, 0x8A, 0x94, 255), (0, 0), body)
    inner = Image.new("L", (W, H))
    ImageDraw.Draw(inner).polygon([(-S, 3 * S), (W - 4 * S, 3 * S), (W - 4 * S - k, (h - 3) * S), (-S, (h - 3) * S)], fill=255)
    # The discs' grey made more solid, with the kit's weave at its own full-screen scale: cropped, never stretched to the plate.
    # A smaller weave, scaled evenly, its tones stretched across the two greys so the strands show on a small plate.
    k_ = WEAVE_SHRINK
    patch = _weave((0, 0, 0), (255, 255, 255)).crop((300, 200, 300 + int(w * k_), 200 + int((h + drop) * k_))).convert("L").resize((W, H), Image.LANCZOS)
    patch = ImageOps.autocontrast(patch, cutoff=2)
    tones = ImageOps.colorize(patch, (0x18, 0x17, 0x1E), (0x56, 0x54, 0x60)).convert("RGBA")
    tones.putalpha(215)
    im.paste(tones, (0, 0), inner)
    # A solid dark section for the hero, with the plate's own lean, so the face always has a clean ground.
    box = Image.new("L", (W, H))
    ImageDraw.Draw(box).polygon([(-S, 3 * S), (92 * S, 3 * S), (92 * S - k, (h - 3) * S), (-S, (h - 3) * S)], fill=255)
    im.paste((0x2C, 0x2B, 0x35, 255), (0, 0), box)
    # An inner shadow: dark falling in from the section's edges, heaviest from the top.
    shadow = ImageChops.invert(ImageChops.offset(box, 0, 4 * S)).filter(ImageFilter.GaussianBlur(5 * S))
    im.paste((0x08, 0x07, 0x0C, 255), (0, 0), ImageChops.multiply(shadow.point(lambda v: min(255, int(v * 1.5))), box))
    # The seat colour as a strip along the screen edge.
    strip = Image.new("L", (W, H))
    ImageDraw.Draw(strip).rectangle((0, 0, 9 * S, h * S), fill=255)
    im.paste(tuple(colour) + (255,), (0, 0), ImageChops.multiply(strip, body))
    plate = see_through(im.resize((w, h + drop), Image.LANCZOS), alpha)
    # The hero's section stays nearly solid while the rest of the plate is more see-through.
    solid = box.resize((w, h + drop), Image.LANCZOS).point(lambda v: int(v * .88))
    plate.putalpha(ImageChops.lighter(plate.getchannel("A"), solid))
    return plate


def _weave_for(W, H):
    return weave((0x36, 0x0A, 0x15), (0x6E, 0x1E, 0x30), (W, H))


def plate_hero(b, hero, cx, y, h):
    """A hero on a score plate: the same height for everyone and centred by its own ink on the plate's face."""
    im = trim(Image.open(UI / "portraits" / (hero + ".png")).convert("RGBA"))
    k = (h - 10) / im.height
    im = im.resize((max(1, round(im.width * k)), h - 10), Image.LANCZOS)
    b.place("plate-hero-%s-%d" % (hero, h), im, cx - im.width / 2, y + 5, "Hero")


def score_plate(b, y, w, h, q, name=True):
    b.place("score-plate-%d-%02x%02x%02x" % (w, q["colour"][0], q["colour"][1], q["colour"][2]), edge_plate_art(w, h, q["colour"]), 0, y, "Score plate")
    plate_hero(b, q["hero"], 48, y, h)
    left(b, 104, y + h / 2 - 2, q["score"], 34, fill=CREAM, name="Score")
    if q["leader"]:
        c = badge("crown", .9)
        b.place("plate-crown", c, 48 - c.width / 2, y - c.height / 2 + 4, "Crown")


def others_pop(b, m, cid):
    """The other players' scores, flush with the screen's left edge, shown only for a moment: when someone
    scores or the lead changes. Tab has the full table the rest of the time."""
    b.group = "Others"
    y, w, h = 150, 276, 58
    for k, q in enumerate(q for q in players(m, cid) if not q["local"]):
        score_plate(b, y, w, h, q)
        if k == 0:
            tag_w = font(GRAND, 24, 800).getlength("+100") + 56
            notice_tag(b, w + 14 + tag_w / 2, y + h / 2, "+100", "go", 24)
        y += h + 12


def tab_board(b, m, cid):
    """Tab keeps the view clear: the same plates on the left edge, now all four players ranked, with your own
    plate longer and rimmed in lime. Nothing crosses the middle of the screen."""
    b.group = "Scoreboard"
    y, w, h = 150, 276, 58
    for k, q in enumerate(players(m, cid)):
        score_plate(b, y, w + 36 if q["local"] else w, h, q)
        if q["local"]:
            b.place("tab-you", Image.new("RGBA", (w + 36 - 12, 4), LIME + (255,)), 0, y + h - 2, "You")
        centred(b, w - 34, y + h / 2 - 2, str(k + 1), 30, fill=AMBER if k == 0 else CREAM, name="Rank")
        y += h + 12


def stroked(im, colour=(0x14, 0x13, 0x1A), width=4, alpha=.35):
    """A crisp, see-through stroke round a piece's silhouette: grown from a hard edge at three times the size and
    brought down once, so it has only an anti-aliased edge and no feather."""
    S, pad = 3, width + 2
    out = Image.new("RGBA", (im.width + pad * 2, im.height + pad * 2))
    out.paste(im, (pad, pad))
    big = out.getchannel("A").resize((out.width * S, out.height * S), Image.BICUBIC).point(lambda v: 255 if v > 128 else 0)
    big = big.filter(ImageFilter.MaxFilter(width * S * 2 + 1))
    ring = big.resize(out.size, Image.LANCZOS)
    back = Image.new("RGBA", out.size, tuple(colour) + (255,))
    back.putalpha(ring.point(lambda v: int(v * alpha)))
    back.alpha_composite(out)
    return back


def vitals(b, m):
    v = lay(m, "VitalsPortrait")
    if not v:
        return
    b.group = "Vitals"
    b.place("vitals-band", glow_band_art(520, 70, colour=LIME), -40, 984, "Band")
    ob = lay(m, "VitalsObject")
    if ob:
        # The held slipper, flat on and tilted, behind the hero and the place. A bare hand has none.
        path = UI / "slippers-flat" / (stem(ob[0]) + ".png")
        flat = trim(Image.open(path if path.exists() else UI / "portraits" / (stem(ob[0]) + ".png")).convert("RGBA"))
        flat.thumbnail((170, 170), Image.LANCZOS)
        flat = flat.rotate(-20, Image.BICUBIC, expand=True)
        # A dark outline in the lettering's weight, so the pale sole holds on pale ground.
        flat = stroked(flat)
        b.place("held-flat-%s" % stem(ob[0]), flat, 150 - flat.width / 2, 960 - flat.height / 2, "Held slipper")
    hero = trim(Image.open(UI / "portraits" / (stem(v[0]) + ".png")).convert("RGBA"))
    hero.thumbnail((124, 124), Image.LANCZOS)
    hero = stroked(hero)
    b.place("vitals-hero-%s" % stem(v[0]), hero, 86 - hero.width / 2, 992 - hero.height / 2, "Hero")


def own_score(b, m, cid):
    table = players(m, cid)
    me = next((k for k, q in enumerate(table) if q["local"]), None)
    if me is None:
        return
    b.group = "Your score"
    b.place("own-score", lettering(table[me]["score"], 74, LIME, (0x34, 0x4C, 0x0C), (0x34, 0x4C, 0x0C), depth=7, edge_w=3, gap=8), 156, 966, "Your score")
    b.place("own-place", lettering(("1ST", "2ND", "3RD", "4TH")[me], 34, *NOTICE_YELLOW, depth=3, edge_w=2, gap=3), 156, 934, "Place")


def key_under(b, layer, cx, cy, scale=1.0):
    if not layer:
        return
    g = sprite(layer[0], scale)
    b.place("key-%s-%d" % (stem(layer[0]), g.width), g, cx - g.width / 2, cy - g.height / 2, "Key")


def charge_art(d, fill):
    """The ultimate's dial: dark glass with a thick lime arc, full yellow when ready."""
    S = 3
    D, w = d * S, 9 * S
    im = Image.new("RGBA", (D, D))
    dr = ImageDraw.Draw(im)
    dr.ellipse((0, 0, D - 1, D - 1), fill=GLASS + (185,))
    dr.arc((S, S, D - S, D - S), 0, 360, fill=(255, 255, 255, 70), width=w)
    if fill > 0:
        dr.arc((S, S, D - S, D - S), -90, -90 + 360 * fill, fill=(PAINT["yellow"][1] if fill >= 1 else LIME) + (255,), width=w)
    return im.resize((d, d), Image.LANCZOS)


def ring_glow_art(d, hole, colour=(0xFF, 0xD2, 0x4A), peak=225):
    """A soft round glow outside the ring of a ready ability; none inside, so the disc keeps its own colour."""
    yy, xx = np.indices((d, d))
    r = np.hypot(xx / (d - 1) * 2 - 1, yy / (d - 1) * 2 - 1)
    t = np.clip(np.clip(1 - r, 0, 1) / .55, 0, 1)
    a = np.zeros((d, d, 4), dtype=np.uint8)
    a[..., :3] = colour
    a[..., 3] = (t * t * (3 - 2 * t) * peak * (r * d / 2 > hole / 2)).astype(np.uint8)
    return Image.fromarray(a, "RGBA")


def ring_art(d, fill, colour, w):
    S = 3
    im = Image.new("RGBA", (d * S, d * S))
    dr = ImageDraw.Draw(im)
    dr.ellipse((S, S, d * S - S, d * S - S), fill=GLASS + (95,))
    dr.arc((S, S, d * S - S, d * S - S), 0, 360, fill=(255, 255, 255, 70), width=w * S)
    if fill > 0:
        dr.arc((S, S, d * S - S, d * S - S), -90, -90 + 360 * fill, fill=tuple(colour) + (255,), width=w * S)
    return im.resize((d, d), Image.LANCZOS)


def centred_art(b, im, cx, cy, key):
    b.place(key, im, cx - im.width / 2, cy - im.height / 2, key)


def powers(b, m, cid):
    if not lay(m, "Power0"):
        return
    if PLATE_POWERS is None:
        return
    if DISC_POWERS:
        disc_powers(b, m, cid)
        return
    if PLATE_POWERS:
        plate_powers(b, m, cid)
        return
    b.group = "Powers"
    gold = PAINT["yellow"][1]
    for i, cx in enumerate((1730, 1846)):
        icon = lay(m, "PowerIcon", "Power%d" % i)
        if not icon:
            continue
        st = lay(m, "PowerState", "Power%d" % i)
        if not st:
            centred_art(b, ring_glow_art(176, 96), cx, 966, "skill-glow")
        centred_art(b, ring_art(104, .4 if st else 1, gold, 9), cx, 966, "skill-ring-%s" % bool(st))
        ic = trim(raster(cid, icon[0]))
        ic.thumbnail((66, 66), Image.LANCZOS)
        if st:
            ic.putalpha(ic.getchannel("A").point(lambda v: int(v * .3)))
        centred_art(b, ic, cx, 966, "power-%d-%s-%d" % (i, bool(st), ic.width))
        if st:
            number(b, cx, 966, st[0]["text"], 30, align="c", name="State")
        key_under(b, lay(m, "BindingGlyph", "LiveBinding%d" % i), cx, 1044)
    ic = lay(m, "PowerIcon", "Power2")
    if ic:
        st = lay(m, "PowerState", "Power2")
        txt = st[0]["text"] if st else ""
        fill = int(txt[:-1]) / 100 if txt.endswith("%") and txt[:-1].isdigit() else 1
        ready = fill >= 1
        if ready:
            centred_art(b, ring_glow_art(236, 126), 960, 958, "ult-glow")
        centred_art(b, ring_art(136, fill, gold if ready else LIME, 12), 960, 958, "ult-ring-%d" % (fill * 100))
        im = trim(raster(cid, ic[0]))
        im.thumbnail((72, 72), Image.LANCZOS)
        centred_art(b, im, 960, 958 if ready else 946, "ult-icon-%d" % im.width)
        if not ready:
            number(b, 960, 988, txt, 26, align="c", name="Charge")
        key_under(b, lay(m, "BindingGlyph", "LiveBinding2"), 960, 1050)


def markers(b, m, cid):
    for name in ("Reticle", "CanArrow shapes", "RecallMark"):
        for l in lay(m, name):
            b.group = "Markers"
            b.place("%s-%d" % (name, l["w"]), raster(cid, l), l["x"], l["y"], name)


def prompt(b, m, cid):
    p = lay(m, "PromptPlate", "ContextualAction") or lay(m, "PromptPlate", "ReplayActionPrompt")
    if not p:
        return
    b.group = "Prompt"
    t = lay(m, "ActionPrompt")[0]
    g = lay(m, "ActionBindingGlyph")
    size = 34
    tw = font(DARUMA, size).getlength(t["text"])
    gw = sprite(g[0], 1.0).width + 14 if g else 0
    x, y = 960 - (tw + gw) / 2, p[0]["y"] + 34
    if g:
        im = sprite(g[0], 1.0)
        b.place("key-%s-%d" % (stem(g[0]), im.width), im, x, y - im.height / 2, "Key")
    word(b, x + gw, y, t["text"], size, name="Prompt")
    if lay(m, "RecoveryProgress"):
        b.place("recover-track", slice3(art("login3-tabs-track"), int(tw + gw), 20), x, y + 42, "Recovery")


STATUS_FOCUS = 0
STATUS_LANDING = False


def cut_rim_art(d, seed=7):
    """A hand-cut white rim: straight facets and sharp corners round the ring, with a pointed tip joined on top."""
    S = 4
    top = 16
    W, H = d * S, (d + top) * S
    cx, cy, R = W / 2, (d / 2 + top) * S, d / 2 * S
    rnd = random.Random(seed)
    n = 13
    pts = []
    for i in range(n):
        a = 2 * math.pi * (i + rnd.uniform(-.22, .22)) / n
        r_ = R * rnd.uniform(.955, 1.0)
        pts.append((cx + r_ * math.cos(a), cy + r_ * math.sin(a)))
    im = Image.new("RGBA", (W, H))
    d_ = ImageDraw.Draw(im)
    d_.polygon(pts, fill=(255, 255, 255, 255))
    d_.polygon([(cx - 9 * S, cy - R + 5 * S), (cx, S), (cx + 9 * S, cy - R + 5 * S)], fill=(255, 255, 255, 255))
    return im.resize((d, d + top), Image.LANCZOS)


def status_landed(b, m, cid):
    """An effect has just landed: its ring swells with a white rim and a small tick joins it to its name and description,
    which start over that ring. Effects landing together take turns, about 2 s each, the last holding 3 s."""
    chips = sorted([l for l in m["layers"] if l["name"].startswith("StatusChip") and l["name"].endswith(" shapes")],
                   key=lambda l: l["y"])
    if len(chips) <= STATUS_FOCUS:
        return
    chip_ = chips[STATUS_FOCUS]
    under = chip_["name"].replace(" shapes", "")
    icon = lay(m, "StatusIcon", under)[0]
    name = lay(m, "StatusName", under)[0]["text"].upper()
    tip = lay(m, "StatusTooltip", under)[0]["text"]
    cx, cy = 402 + STATUS_FOCUS * 86, 998
    b.group = "Status"
    # The focused ring, drawn again over the row, larger and white-rimmed.
    centred_art(b, cut_rim_art(99), cx, cy - 8, "status-rim")
    centred_art(b, ring_art(84, .72, (0xF0, 0x70, 0x6C), 7), cx, cy, "status-ring-focus")
    disc = Image.new("RGBA", (84 * 3, 84 * 3))
    ImageDraw.Draw(disc).ellipse((3, 3, 249, 249), fill=GLASS + (255,))
    centred_art(b, disc.resize((84, 84), Image.LANCZOS), cx, cy, "status-disc")
    centred_art(b, ring_art(84, .72, (0xF0, 0x70, 0x6C), 7), cx, cy, "status-ring-focus")
    centred_art(b, sprite(icon, size=(64, 64)), cx, cy, "status-focus-%s" % stem(icon))
    times = [t for t in lay(m, "StatusTime", "StatusTimeChip") if abs(t["ink"][1] - chip_["y"]) < chip_["h"] + 30]
    if times:
        sec = times[0]["text"].split(".")[0]
        centred_art(b, lettering(sec, 26, WHITE, GLASS, GLASS, depth=3, edge_w=3, gap=1), cx, cy + 58, "status-time-focus-%s" % sec)
    # Kept short of the ultimate: long descriptions wrap, and the name rises to make room.
    lines = wrap_lines(tip, 24, 440, 800)
    x, y = cx - 34, cy - 76 - (len(lines) - 1) * 30
    im = lettering(name, 36, WHITE, GLASS, GLASS, depth=4, edge_w=3, gap=3)
    b.place("status-name-%s" % ps_key(name), im, x, y - 60, "Status name")
    for k, line in enumerate(lines):
        word(b, x + 2, y + k * 30, line, 24, face_=GRAND, weight=800, name="Status description")


def statuses(b, m, cid):
    """Effects on you, in a row right of your score and clear of the slipper: the abilities' ring on a smaller scale,
    in coral, draining as the effect runs out, with its seconds painted under it."""
    chips = sorted([l for l in m["layers"] if l["name"].startswith("StatusChip") and l["name"].endswith(" shapes")],
                   key=lambda l: l["y"])
    cx, cy, d = 402, 998, 72
    for l in chips:
        under = l["name"].replace(" shapes", "")
        icon = lay(m, "StatusIcon", under)[0]
        times = [t for t in lay(m, "StatusTime", "StatusTimeChip") if abs(t["ink"][1] - l["y"]) < l["h"] + 30]
        b.group = "Status"
        centred_art(b, ring_art(d, .72, (0xF0, 0x70, 0x6C), 6), cx, cy, "status-ring")
        centred_art(b, sprite(icon, size=(54, 54)), cx, cy, "status-%s" % stem(icon))
        # The focused ring draws its own, lower, while it is swollen.
        if times and not (STATUS_LANDING and chips.index(l) == STATUS_FOCUS):
            sec = lettering(times[0]["text"].split(".")[0], 24, WHITE, GLASS, GLASS, depth=3, edge_w=3, gap=1)
            centred_art(b, sec, cx, cy + d / 2 + 8, "status-time-%s" % times[0]["text"].split(".")[0])
        cx += d + 14


def feed_plate_art(w, h, alpha=.82, section=168, glows=()):
    """The score plates' material on the screen's right edge: same stroke, slab, woven grey fill and lean, with a
    nearly solid dark section holding the two heroes and what happened between them."""
    S, drop = 3, 5
    W, H = w * S, (h + drop) * S
    k = h * S * LEAN
    body = Image.new("L", (W, H))
    ImageDraw.Draw(body).polygon([(k, 0), (W + S, 0), (W + S, h * S), (0, h * S)], fill=255)
    swept = body
    for dy in range(S, drop * S + 1, S):
        swept = ImageChops.lighter(swept, ImageChops.offset(body, 0, dy))
    im = Image.new("RGBA", (W, H))
    im.paste(GLASS + (235,), (0, 0), swept)
    im.paste((0x8C, 0x8A, 0x94, 255), (0, 0), body)
    inner = Image.new("L", (W, H))
    ImageDraw.Draw(inner).polygon([(k + 4 * S, 3 * S), (W + S, 3 * S), (W + S, (h - 3) * S), (4 * S, (h - 3) * S)], fill=255)
    patch = _weave((0, 0, 0), (255, 255, 255)).crop((300, 200, 300 + int(w * WEAVE_SHRINK), 200 + int((h + drop) * WEAVE_SHRINK))).convert("L").resize((W, H), Image.LANCZOS)
    tones = ImageOps.colorize(ImageOps.autocontrast(patch, cutoff=2), (0x18, 0x17, 0x1E), (0x56, 0x54, 0x60)).convert("RGBA")
    tones.putalpha(215)
    im.paste(tones, (0, 0), inner)
    box = Image.new("L", (W, H))
    ImageDraw.Draw(box).polygon([(k + 4 * S, 3 * S), (k + (section + 4) * S, 3 * S), ((section + 4) * S, (h - 3) * S), (4 * S, (h - 3) * S)], fill=255)
    im.paste((0x2C, 0x2B, 0x35, 255), (0, 0), box)
    # Each hero's seat colour, wider than the section and clipped by it.
    for cx, colour in glows:
        d = 130 * S
        glow = seat_glow_art(d, colour, peak=125)
        layer = Image.new("RGBA", (W, H))
        layer.paste(glow, (int(cx * S - d / 2), int(h * S / 2 - d / 2)))
        layer.putalpha(ImageChops.multiply(layer.getchannel("A"), box))
        im.alpha_composite(layer)
    shadow = ImageChops.invert(ImageChops.offset(box, 0, 4 * S)).filter(ImageFilter.GaussianBlur(5 * S))
    im.paste((0x08, 0x07, 0x0C, 255), (0, 0), ImageChops.multiply(shadow.point(lambda v: min(255, int(v * 1.5))), box))
    plate = see_through(im.resize((w, h + drop), Image.LANCZOS), alpha)
    plate.putalpha(ImageChops.lighter(plate.getchannel("A"), box.resize((w, h + drop), Image.LANCZOS).point(lambda v: int(v * .88))))
    return plate


def seat_glow_art(d, colour, peak=245):
    """A soft circle of a seat's colour behind a hero, with no edge."""
    yy, xx = np.indices((d, d))
    r = np.hypot(xx / (d - 1) * 2 - 1, yy / (d - 1) * 2 - 1)
    t = np.clip((1 - r) / .7, 0, 1)
    a = np.zeros((d, d, 4), dtype=np.uint8)
    a[..., :3] = colour
    a[..., 3] = (t * t * (3 - 2 * t) * peak).astype(np.uint8)
    return Image.fromarray(a, "RGBA")


def events(b, m, cid):
    # Room to breathe: 24 either side of the heroes, the points in their own part with 24 round them.
    y, h, section = 130, 58, 232
    hero_a_x, glyph_x, hero_o_x = 64, 124, 184
    for i in range(2):
        if not lay(m, "Event%d shapes" % i):
            continue
        under = "Event%d" % i
        pts = lay(m, "EventPoints", under)
        tag_w = int(font(GRAND, 28, 800).getlength(pts[0]["text"]) + 40) if pts else 0
        w = section + 24 + tag_w + 24 if pts else section + 28
        x = 1920 - w
        hero_a = stem(lay(m, "Portrait", "ActorChip")[i])
        hero_o = stem(lay(m, "Portrait", "OtherChip")[i])
        actor_tint = lay(m, "ActorChip", under)
        actor = tint(actor_tint[0]) if actor_tint and actor_tint[0].get("tint") else SEATS["P1"] if hero_a == "sean" else SEATS["P2"] if hero_a == "dante" else SEATS["P3"]
        other = tint(lay(m, "OtherChip", under)[0])
        b.group = "Events"
        art = feed_plate_art(w, h, section=section, glows=((hero_a_x, actor), (hero_o_x, other)))
        b.place("feed-plate-%d-%02x%02x%02x-%02x%02x%02x" % ((w,) + tuple(actor) + tuple(other)), art, x, y, "Event")
        plate_hero(b, hero_a, x + hero_a_x, y, h)
        plate_hero(b, hero_o, x + hero_o_x, y, h)
        g = inked(trim(raster(cid, lay(m, "EventGlyph", under)[0])), CREAM)
        g.thumbnail((28, 28), Image.LANCZOS)
        b.place("feed-glyph-%d-%d" % (i, g.width), g, x + glyph_x - g.width / 2, y + h / 2 - g.height / 2 - 1, "Event")
        if pts:
            tx = x + section + 24
            b.place("feed-tag-%d" % tag_w, thin_button_art("go", tag_w, 44), tx, y + 6, "Points")
            centred(b, tx + tag_w / 2, y + 26, pts[0]["text"], 28, face=GRAND, fill=BUTTONS["go"][1], weight=800, name="Points")
        y += h + 12


def lettering(s_, size, face_, edge_, slab, depth=9, gap=6, edge_w=5):
    """Painted lettering for notices: a flat fill, an outline and a slab joined below it."""
    S = 2
    f = font(DARUMA, size * S)
    pad = 16 * S
    adv = [f.getlength(c) + gap * S for c in s_]
    W, H = int(sum(adv)) + pad * 2, int(size * S * 1.25) + pad * 2 + depth * S
    fill_, ring = Image.new("L", (W, H)), Image.new("L", (W, H))
    x = pad
    for c, a in zip(s_, adv):
        ImageDraw.Draw(fill_).text((x, pad), c, font=f, fill=255)
        ImageDraw.Draw(ring).text((x, pad), c, font=f, fill=255, stroke_width=edge_w * S)
        x += a
    swept = ring
    for dy in range(S, depth * S + 1, S):
        swept = ImageChops.lighter(swept, ImageChops.offset(ring, 0, dy))
    im = Image.new("RGBA", (W, H))
    im.paste(tuple(slab) + (255,), (0, 0), swept)
    im.paste(tuple(edge_) + (255,), (0, 0), ring)
    im.paste(tuple(face_) + (255,), (0, 0), fill_)
    im = im.resize((W // S, H // S), Image.LANCZOS)
    return im.crop(im.getchannel("A").getbbox())


def glow_band_art(w, h, peak=140, colour=(0xF0, 0x70, 0x6C)):
    """A soft band of coral behind a warning, melting away at both ends."""
    yy, xx = np.indices((h, w))
    across = np.clip((1 - np.abs(xx / (w - 1) * 2 - 1)) / .55, 0, 1)
    down = np.clip((1 - np.abs(yy / (h - 1) * 2 - 1)) / .4, 0, 1)
    a = np.zeros((h, w, 4), dtype=np.uint8)
    a[..., :3] = colour
    a[..., 3] = (across * across * (3 - 2 * across) * down * down * (3 - 2 * down) * peak).astype(np.uint8)
    return Image.fromarray(a, "RGBA")


WARN_RED = ((0xF6, 0x4A, 0x48), (0x8E, 0x16, 0x24), (0x8E, 0x16, 0x24))
NOTICE_YELLOW = (PAINT["yellow"][1], (0xA8, 0x4E, 0x0C), (0xA8, 0x4E, 0x0C))


def ps_key(text_):
    return "".join(c for c in text_ if c.isalnum())


def notice_tag(b, cx, cy, text_, kind, size=24):
    """A short extra under a notice, on a thin button in the kit's paint."""
    w = int(font(GRAND, size, 800).getlength(text_) + 56)
    h = size + 28
    b.place("notice-tag-%s-%dx%d" % (kind, w, h), thin_button_art(kind, w, h), cx - w / 2, cy - h / 2, "Tag")
    centred(b, cx, cy - 2, text_, size, face=GRAND, fill=BUTTONS[kind][1], weight=800, name="Tag")


def warning(b, m, cid):
    if not lay(m, "WarningMessage shapes"):
        return
    b.group = "Warning"
    y = 130
    im = lettering("DO NOT IDLE", 74, *WARN_RED)
    x = 960 - im.width / 2
    b.place("warn-band", glow_band_art(900, 74), 510, y + 16, "Band")
    b.place("warn-title", im, x, y, "Warning")
    # One mark each side, leaning away from the words.
    mark = lettering("!", 74, *WARN_RED, edge_w=3)
    l_, r_ = mark.rotate(14, Image.BICUBIC, expand=True), mark.rotate(-14, Image.BICUBIC, expand=True)
    b.place("warn-bang-l", l_, x - l_.width - 14, y + (im.height - l_.height) / 2 + 7, "Bang")
    b.place("warn-bang-r", r_, x + im.width + 14, y + (im.height - r_.height) / 2 + 7, "Bang")
    reason = "RETRIEVE YOUR SLIPPER"
    w = int(font(GRAND, 22, 800).getlength(reason) + 44)
    cy = y + im.height + 40
    b.place("warn-tag-%d" % w, thin_button_art("plain-soft", w, 50), 960 - w / 2, cy - 25, "Reason")
    centred(b, 960, cy - 2, reason, 22, face=GRAND, fill=MAROON, weight=800, name="Reason")


def toast(b, m):
    t = lay(m, "MatchToast")
    if not t:
        return
    b.group = "Toast"
    im = lettering(t[0]["text"], 44, *NOTICE_YELLOW, depth=6, edge_w=4)
    y = 395 if lay(m, "WarningMessage shapes") else t[0]["ink"][1] - im.height / 2
    b.place("toast-%s" % ps_key(t[0]["text"]), im, 960 - im.width / 2, y, "Toast")


def hud(b, m, cid):
    clock(b, m, cid)
    vitals(b, m)
    own_score(b, m, cid)
    powers(b, m, cid)
    markers(b, m, cid)
    statuses(b, m, cid)
    events(b, m, cid)
    warning(b, m, cid)
    toast(b, m)
    prompt(b, m, cid)


# The moments

def crown_art(src, w, edge_w=3, depth=4):
    """The crown in the notices' paint: flat yellow, with the lettering's outline and joined slab."""
    S = 3
    h = round(src.height * w / src.width)
    pad = (edge_w + 2) * S
    a = src.getchannel("A").resize((w * S, h * S), Image.LANCZOS).filter(ImageFilter.GaussianBlur(1.5 * S)).point(lambda v: 255 if v > 110 else 0)
    body = Image.new("L", (w * S + pad * 2, h * S + pad * 2 + depth * S))
    body.paste(a, (pad, pad))
    ring = body.filter(ImageFilter.MaxFilter(edge_w * 2 * S + 1))
    swept = ring
    for dy in range(S, depth * S + 1, S):
        swept = ImageChops.lighter(swept, ImageChops.offset(ring, 0, dy))
    im = Image.new("RGBA", body.size)
    im.paste(NOTICE_YELLOW[1] + (255,), (0, 0), swept)
    im.paste(NOTICE_YELLOW[0] + (255,), (0, 0), body)
    im = im.resize((body.width // S, body.height // S), Image.LANCZOS)
    return im.rotate(12, Image.BICUBIC, expand=True)


def moment(b, m, cid):
    t = lay(m, "MomentTitle")
    if not t:
        return
    title = t[0]["text"].upper().replace("MULTI KNOCKDOWN", "MULTI DOWN")
    hero = stem(lay(m, "MomentPortrait")[0])
    bonus = lay(m, "MomentBonusPoints")
    crown = lay(m, "MomentCrown")
    colours = NOTICE_YELLOW if crown else WARN_RED
    b.group = "Moment"
    im = lettering(title, 74 if crown else 62, *colours)
    cs = 84
    # A warning keeps the top spot; a moment waits below it.
    y = 300 if lay(m, "WarningMessage shapes") else 130
    x = 960 - (im.width + cs + 10) / 2
    band_w = int(im.width + cs + 300)
    b.place("moment-band-%d-%s" % (band_w, bool(crown)), glow_band_art(band_w, int(im.height * .9), colour=PAINT["yellow"][1] if crown else (0xF0, 0x70, 0x6C)),
            960 - band_w / 2, y + im.height * .2, "Band")
    face(b, hero, x + cs / 2, y + im.height / 2, cs)
    b.place("moment-%s" % ps_key(title), im, x + cs + 10, y, "Moment")
    if crown:
        c = crown_art(trim(raster(cid, crown[0])), 42, edge_w=2, depth=3)
        # Tilted and sitting down into the hair, so it is worn rather than floating above the head.
        b.place("moment-crown", c, x + cs / 2 - c.width / 2 + 4, y + im.height / 2 - cs / 2 - c.height * .72, "Crown")
    if bonus:
        notice_tag(b, 960, y + im.height + 34, bonus[0]["text"], "go", 30)


def vignette(b, color=(0xF6, 0xBC, 0x3A), reach=80, peak=150):
    yy, xx = np.indices((1080, 1920))
    d = np.minimum(np.minimum(xx, 1919 - xx), np.minimum(yy, 1079 - yy))
    a = (np.clip(1 - d / reach, 0, 1) ** 1.6 * peak).astype(np.uint8)
    im = Image.new("RGBA", (1920, 1080), tuple(color) + (255,))
    im.putalpha(Image.fromarray(a))
    b.place("vignette-%02x%02x%02x" % color, im, 0, 0, "Edge glow")


def can_art(h):
    """A dented tin can drawn by hand: blue paint, a cream label, a lit rim, in the lettering's outline."""
    S = 3
    w = int(h * .62)
    W, H = (w + 16) * S, (h + 16) * S
    ox, oy = 8 * S, 8 * S
    bw, bh = w * S, h * S
    ry = bh * .11
    body = Image.new("L", (W, H))
    d = ImageDraw.Draw(body)
    d.polygon(wobble([(ox, oy + ry), (ox + bw, oy + ry), (ox + bw - 2 * S, oy + bh - ry), (ox + 2 * S, oy + bh - ry)], 1.2 * S, 5), fill=255)
    d.ellipse((ox, oy, ox + bw, oy + 2 * ry), fill=255)
    d.ellipse((ox + 2 * S, oy + bh - 2 * ry, ox + bw - 2 * S, oy + bh), fill=255)
    ring = body.filter(ImageFilter.MaxFilter(4 * S + 1))
    im = Image.new("RGBA", (W, H))
    im.paste(NOTICE_YELLOW[1] + (255,), (0, 0), ring)
    im.paste((0x24, 0x58, 0xB4, 255), (0, 0), body)
    paint = Image.new("RGBA", (W, H))
    pd = ImageDraw.Draw(paint)
    # Shade down one side, the label across the middle, and ribs.
    pd.rectangle((ox + bw * .72, 0, W, H), fill=(0x18, 0x3C, 0x84, 255))
    pd.rectangle((0, oy + bh * .42, W, oy + bh * .66), fill=CREAM + (255,))
    pd.rectangle((ox + bw * .72, oy + bh * .42, W, oy + bh * .66), fill=PAINT["plain-soft"][0] + (255,))
    paint.putalpha(ImageChops.multiply(paint.getchannel("A"), body))
    im.alpha_composite(paint)
    top = ImageDraw.Draw(im)
    top.ellipse((ox, oy, ox + bw, oy + 2 * ry), fill=(0xC8, 0xD0, 0xD8, 255), outline=NOTICE_YELLOW[1] + (255,), width=2 * S)
    top.ellipse((ox + bw * .14, oy + ry * .4, ox + bw * .86, oy + ry * 1.6), fill=(0x8C, 0x96, 0xA2, 255))
    im = im.resize((W // S, H // S), Image.LANCZOS)
    return im.crop(im.getchannel("A").getbbox())


def can_down(b, m, cid):
    b.group = "Can down"
    im = lettering(lay(m, "CanDownTitle")[0]["text"], 74, *NOTICE_YELLOW)
    y = 130
    x = 960 - im.width / 2
    b.place("can-band", glow_band_art(1000, 74, colour=PAINT["yellow"][1]), 460, y + 16, "Band")
    b.place("can-title", im, x, y, "Can down")
    can = can_art(84)
    l_, r_ = can.rotate(14, Image.BICUBIC, expand=True), can.rotate(-14, Image.BICUBIC, expand=True)
    b.place("can-l", l_, x - l_.width - 12, y + (im.height - l_.height) / 2 + 4, "Can")
    b.place("can-r", r_, x + im.width + 12, y + (im.height - r_.height) / 2 + 4, "Can")


TAYA_LOOK = "lime"
TAYA_LOOKS = {"cream": ((0xFF, 0xF8, 0xEE), PAINT["red"][1], PAINT["red"][1], (0xF0, 0x70, 0x6C), "go"),
              "lime": (LIME, (0x4E, 0x6E, 0x14), (0x4E, 0x6E, 0x14), LIME, "yellow")}


def taya_reveal(b, m, cid, dx=0):
    word_ = lay(m, "TayaRevealWord")
    if not word_:
        return
    b.group = "Taya reveal"
    fill_, edge_, slab, band, tag_kind = TAYA_LOOKS[TAYA_LOOK]
    im = lettering(word_[0]["text"], 104, fill_, edge_, slab, depth=12, edge_w=6)
    y = 130
    b.place("taya-band-%s" % TAYA_LOOK, glow_band_art(800, 92, colour=band), 560 + dx, y + 26, "Band")
    b.place("taya-title-%s" % TAYA_LOOK, im, 960 + dx - im.width / 2, y, "Taya")
    notice_tag(b, 960 + dx, y + im.height + 40, lay(m, "TayaRevealName")[0]["text"], tag_kind, 34)


def grey_weave(w, h, S=1):
    """The score plates' fill: the small, high-contrast weave in the dark greys, at its own scale. The weave is tiled
    with mirrored repeats, because a crop past the source's edge comes back black on a large panel."""
    k = WEAVE_SHRINK
    source = ImageOps.autocontrast(_weave((0, 0, 0), (255, 255, 255)).convert("L"), cutoff=2)
    tw, th = round(source.width / k), round(source.height / k)
    tile = source.resize((tw, th), Image.LANCZOS)
    sheet = Image.new("L", (w, h))
    for row in range(h // th + 1):
        for col in range(w // tw + 1):
            piece = tile
            if col % 2:
                piece = ImageOps.mirror(piece)
            if row % 2:
                piece = ImageOps.flip(piece)
            sheet.paste(piece, (col * tw, row * th))
    tones = ImageOps.colorize(sheet.resize((w * S, h * S), Image.BICUBIC), (0x1E, 0x1D, 0x25), (0x3A, 0x39, 0x44)).convert("RGBA")
    tones.putalpha(215)
    return tones


def hud_panel_art(w, h, flush=None, alpha=.82):
    """Any larger HUD surface in the score plates' material: grey stroke, dark slab, woven grey fill, and the lean.
    `flush` runs it off the left edge, or off the top left corner, so it never floats as a box."""
    S, drop = 3, 6
    W, H = w * S, (h + drop) * S
    # The lean of a short plate at most, so a tall panel does not become a parallelogram.
    k = min(h * S * LEAN, 14 * S)
    if flush == "left":
        outer = [(-S, 0), (W, 0), (W - k, h * S), (-S, h * S)]
        inner_ = [(-S, 4 * S), (W - 5 * S, 4 * S), (W - 5 * S - k, (h - 4) * S), (-S, (h - 4) * S)]
    else:
        outer = [(k, 0), (W, 0), (W - k, h * S), (0, h * S)]
        inner_ = [(k + 5 * S, 4 * S), (W - 5 * S, 4 * S), (W - 5 * S - k, (h - 4) * S), (5 * S, (h - 4) * S)]
    body = Image.new("L", (W, H))
    ImageDraw.Draw(body).polygon(outer, fill=255)
    swept = body
    for dy in range(S, drop * S + 1, S):
        swept = ImageChops.lighter(swept, ImageChops.offset(body, 0, dy))
    im = Image.new("RGBA", (W, H))
    im.paste(GLASS + (235,), (0, 0), swept)
    im.paste((0x8C, 0x8A, 0x94, 255), (0, 0), body)
    inner = Image.new("L", (W, H))
    ImageDraw.Draw(inner).polygon(inner_, fill=255)
    im.paste(grey_weave(w, h + drop, S), (0, 0), inner)
    return see_through(im.resize((w, h + drop), Image.LANCZOS), alpha)


def countdown(b, m, cid):
    t = lay(m, "Countdown")
    if not t:
        return
    b.group = "Countdown"
    im = lettering(t[0]["text"], 190, *NOTICE_YELLOW, depth=14, edge_w=8, gap=0)
    b.place("cdn-band", glow_band_art(900, 170, colour=PAINT["yellow"][1]), 510, 372, "Band")
    centred_art(b, im, 960, 456, "cdn-%s" % t[0]["text"])


# Emote wheel

def sector(cx, cy, a0, a1, r0, r1, n=14):
    pts = [(cx + r1 * math.sin(math.radians(a0 + (a1 - a0) * k / n)), cy - r1 * math.cos(math.radians(a0 + (a1 - a0) * k / n)))
           for k in range(n + 1)]
    pts += [(cx + r0 * math.sin(math.radians(a1 - (a1 - a0) * k / n)), cy - r0 * math.cos(math.radians(a1 - (a1 - a0) * k / n)))
            for k in range(n + 1)]
    return pts


def emote_screen(cid):
    # The wheel takes the middle of the screen, so a prompt waits until it closes.
    b = base(cid)
    m = manifest(cid)
    keep = globals()["prompt"]
    globals()["prompt"] = lambda *a: None
    try:
        hud(b, m, cid)
    finally:
        globals()["prompt"] = keep
    emote(b, m, cid)
    return b


def emote(b, m, cid):
    b.group = "Emote wheel"
    cx, cy = 960, 541
    ox, oy = 600, 200
    W, Hh, drop = 720, 700, 7
    im = Image.new("RGBA", (W, Hh))
    d = ImageDraw.Draw(im)
    gap = 2.2
    stroke_ = (0x8C, 0x8A, 0x94, 255)
    for i in range(7):
        a0, a1 = 51.43 * i + gap, 51.43 * (i + 1) - gap
        outer = sector(cx - ox, cy - oy, a0, a1, 138, 296)
        d.polygon([(px, py + drop) for px, py in outer], fill=GLASS + (235,))
    for i in range(7):
        a0, a1 = 51.43 * i + gap, 51.43 * (i + 1) - gap
        d.polygon(sector(cx - ox, cy - oy, a0, a1, 138, 296), fill=stroke_)
    mask = Image.new("L", im.size)
    md = ImageDraw.Draw(mask)
    for i in range(7):
        a0, a1 = 51.43 * i + gap + 1.2, 51.43 * (i + 1) - gap - 1.2
        md.polygon(sector(cx - ox, cy - oy, a0, a1, 143, 291), fill=255)
    im.paste(grey_weave(W, Hh), (0, 0), mask)
    # The centre: the nearly solid dark of the hero sections, with an inner shadow.
    r = 122
    d.ellipse((cx - ox - r, cy - oy - r + drop, cx - ox + r, cy - oy + r + drop), fill=GLASS + (235,))
    d.ellipse((cx - ox - r, cy - oy - r, cx - ox + r, cy - oy + r), fill=stroke_)
    disc = Image.new("L", im.size)
    ImageDraw.Draw(disc).ellipse((cx - ox - r + 5, cy - oy - r + 5, cx - ox + r - 5, cy - oy + r - 5), fill=255)
    im.paste((0x2C, 0x2B, 0x35, 255), (0, 0), disc)
    shade = ImageChops.invert(ImageChops.offset(disc, 0, 8)).filter(ImageFilter.GaussianBlur(14))
    im.paste((0x08, 0x07, 0x0C, 255), (0, 0), ImageChops.multiply(shade, disc))
    a_ = im.getchannel("A").point(lambda v: int(v * .86))
    im.putalpha(ImageChops.lighter(a_, disc.point(lambda v: int(v * .88))))
    b.place("emote-wheel", im, ox, oy, "Wheel")
    for i in range(7):
        pl = lay(m, "Petal%d shapes" % i)[0]
        ic = bright(raster(cid, pl), 200)
        ic = ic.resize((int(ic.width * 1.15), int(ic.height * 1.15)), Image.LANCZOS)
        ccx = pl["x"] + (pl["w"] * 0.0)
        lab = lay(m, "Label", "Petal%d" % i)[0]
        lx, ly = (lab["ink"][0] + lab["ink"][2]) / 2, (lab["ink"][1] + lab["ink"][3]) / 2
        # The icon keeps the capture's own position, centred on its original box.
        full = raster(cid, pl)
        bb = bright(full, 200).getchannel("A").getbbox()
        a = np.asarray(full.convert("RGBA"), dtype=np.float32)
        lum = a[..., :3] @ np.array([.299, .587, .114], dtype=np.float32)
        ys, xs = np.nonzero((a[..., 3] > 120) & (lum > 200))
        icx, icy = pl["x"] + (xs.min() + xs.max()) / 2, pl["y"] + (ys.min() + ys.max()) / 2
        b.place("emote-icon-%d" % i, ic, icx - ic.width / 2, icy - ic.height / 2 - 4, "Emote")
        centred(b, lx, ly + 8, lab["text"], 22, fill=CREAM, name="Emote")
    full = raster(cid, lay(m, "Wheel shapes")[0])
    cen = full.crop((full.width // 2 - 56, full.height // 2 - 56, full.width // 2 + 56, full.height // 2 + 56))
    a = np.asarray(cen, dtype=np.float32)
    lum = a[..., :3] @ np.array([.299, .587, .114], dtype=np.float32)
    keep = (a[..., 3] > 120) & (lum > 120)
    cen.putalpha(Image.fromarray((keep * 255).astype(np.uint8)))
    cen = trim(cen)
    cen = cen.resize((int(cen.width * 1.5), int(cen.height * 1.5)), Image.LANCZOS)
    b.place("emote-face", cen, cx - cen.width / 2, cy - cen.height / 2, "Face")


# Held skills

def held_sheet(b, m, cid):
    # No panel behind them: dark cards on a dark panel would blend.
    b.group = "Skills"
    icons = [trim(raster(cid, lay(m, "PowerIcon", "Power%d" % i)[0])) for i in range(3)]
    for i in range(3):
        x, y, w, h = 98 + i * 585, 326, 560, 510
        frame(b, x, y, w, h, dark=True, name="Skill %d" % i)
        ic = icons[i]
        ic = ic.resize((int(ic.width * 100 / max(ic.size)), int(ic.height * 100 / max(ic.size))), Image.LANCZOS)
        b.place("held-icon-%d" % i, ic, x + 34, y + 34, "Ability")
        name = lay(m, "PowerName%d" % i)[0]["text"]
        edge(b, x + 160, y + 72, name, fit_size(name, 40, w - 190), stroke=4, name="Name")
        timing = lay(m, "PowerTiming%d" % i)[0]["text"]
        left(b, x + 34, y + 168, timing, 30, face=GRAND, fill=AMBER, weight=700, caps=False, name="Timing")
        desc = lay(m, "PowerDescription%d" % i)[0]["text"]
        para(b, x + 34, y + 208, wrap_lines(desc, 28, w - 74), 28, 38, fill=CREAM, weight=500)


# Held powers reference, the game's layout (owner, 2026-11-07): one dark panel, the page title top left
# (ATTACKING or DEFENDING) with the key that flips it top right, then a column per power: the icon in a
# sunburst disc, its name, its number on a plain tag, and what it does in a sunk well.

REFERENCE = [  # (name, chip, key, description with {K} for the key cap)
    ("UNSTOPPABLE", "40s COOLDOWN", "E", "PRESS {K} to INSTANTLY shake off stuns and negative effects, then gain Status Immunity. "
                                         "Works while stunned, but not while Tagged."),
    ("BOULDER", "35s COOLDOWN", "Q", "PRESS {K} while holding your slipper to coat it in stone. Your next throw Concusses the "
                                     "player it hits, leaving them dizzy and sluggish."),
    ("CONTINENTAL DRIFT", "15 CHARGE", "X", "PRESS {K} to slam the court, tearing earth down the lane to knock players away "
                                            "and Concuss them. Then take MOUNTAIN FORM for heavier throws and Status Immunity."),
]


def keycap_art(letter, h=36):
    """The game's own key prompt (the Xelu pack in `Resources/UI/input/xelu`, the light key, as `InputGlyphs.For`
    draws on a dark ground), cropped to its ink and scaled to a height."""
    im = Image.open(UI / "input/xelu/dark" / (letter + "_Key_Dark.png")).convert("RGBA")
    im = im.crop(im.getchannel("A").getbbox())
    return im.resize((max(1, round(im.width * h / im.height)), h), Image.LANCZOS)


def inline_text(b, x, y, words, key, size, pitch, maxw, fill, key_h=34):
    """Wrapped words where {K} becomes a key cap; each line is one text layer, caps placed between."""
    f = font(GRAND, size, 600)
    cap = keycap_art(key, key_h)
    space = f.getlength(" ")
    lines, line, w = [], [], 0
    for tok in words.split():
        tw = cap.width if tok == "{K}" else f.getlength(tok)
        if line and w + space + tw > maxw:
            lines.append(line); line, w = [], 0
        w += (space if line else 0) + tw
        line.append(tok)
    if line:
        lines.append(line)
    for i, ln in enumerate(lines):
        cx, cy = x, y + i * pitch
        run = []
        def flush():
            nonlocal cx, run
            if run:
                s_ = " ".join(run)
                b.write((cx, cy), s_, f, fill, ps_name(GRAND, 600), size, name="Effect")
                cx += f.getlength(s_ + " ")
                run = []
        for tok in ln:
            if tok == "{K}":
                flush()
                b.place("key-%s-%d" % (key, key_h), cap, cx, cy + (size * 1.25 - key_h) / 2 - 7, "Key")
                cx += cap.width + space
            else:
                run.append(tok)
        flush()


def disc_art(d):
    sun = sunburst_art(d, d)
    mask = Image.new("L", (d, d))
    ImageDraw.Draw(mask).ellipse((6, 6, d - 7, d - 7), fill=255)
    out = Image.new("RGBA", (d, d))
    ImageDraw.Draw(out).ellipse((0, 0, d - 1, d - 1), fill=(0x5C, 0x14, 0x22, 255))
    out.paste(sun, (0, 0), mask)
    return out


HELD = [  # (name, one line, key, which ring): the ring already shows the icon and the cooldown
    ("UNSTOPPABLE", "Shake off stuns and debuffs, then gain Status Immunity.", "E", "skill"),
    ("BOULDER", "Stone your held slipper. Your next hit Concusses.", "Q", "skill"),
    ("CONTINENTAL DRIFT", "Slam the lane, knock players away, then Mountain Form.", "X", "ultimate"),
]


def held_plate_art(w, h, section, closed=False, alpha=.82):
    """The kill feed's plate: grey stroke and slab, the woven grey, the lean, a nearly solid dark section at its left
    end. `closed` leans the right end too, for a plate that stands in the middle of the screen."""
    S, drop = 3, 5
    W, H = w * S, (h + drop) * S
    k = h * S * LEAN
    right = [(W - k, 0), (W, 0)] if False else None
    outline = [(k, 0), (W, 0), (W - k, h * S), (0, h * S)] if closed else [(k, 0), (W + S, 0), (W + S, h * S), (0, h * S)]
    body = Image.new("L", (W, H))
    ImageDraw.Draw(body).polygon(outline, fill=255)
    swept = body
    for dy in range(S, drop * S + 1, S):
        swept = ImageChops.lighter(swept, ImageChops.offset(body, 0, dy))
    im = Image.new("RGBA", (W, H))
    im.paste(GLASS + (235,), (0, 0), swept)
    im.paste((0x8C, 0x8A, 0x94, 255), (0, 0), body)
    r = 4 * S
    inner_pts = ([(k + r, 3 * S), (W - r, 3 * S), (W - r - k, (h - 3) * S), (r, (h - 3) * S)] if closed else
                 [(k + r, 3 * S), (W + S, 3 * S), (W + S, (h - 3) * S), (r, (h - 3) * S)])
    inner = Image.new("L", (W, H))
    ImageDraw.Draw(inner).polygon(inner_pts, fill=255)
    patch = _weave((0, 0, 0), (255, 255, 255)).crop((300, 200, 300 + int(w * WEAVE_SHRINK), 200 + int((h + drop) * WEAVE_SHRINK))).convert("L").resize((W, H), Image.LANCZOS)
    tones = ImageOps.colorize(ImageOps.autocontrast(patch, cutoff=2), (0x18, 0x17, 0x1E), (0x56, 0x54, 0x60)).convert("RGBA")
    tones.putalpha(215)
    im.paste(tones, (0, 0), inner)
    box = Image.new("L", (W, H))
    ImageDraw.Draw(box).polygon([(k + r, 3 * S), (k + (section + 4) * S, 3 * S), ((section + 4) * S, (h - 3) * S), (r, (h - 3) * S)], fill=255)
    im.paste((0x2C, 0x2B, 0x35, 255), (0, 0), box)
    shadow = ImageChops.invert(ImageChops.offset(box, 0, 4 * S)).filter(ImageFilter.GaussianBlur(5 * S))
    im.paste((0x08, 0x07, 0x0C, 255), (0, 0), ImageChops.multiply(shadow.point(lambda v: min(255, int(v * 1.5))), box))
    plate = see_through(im.resize((w, h + drop), Image.LANCZOS), alpha)
    plate.putalpha(ImageChops.lighter(plate.getchannel("A"), box.resize((w, h + drop), Image.LANCZOS).point(lambda v: int(v * .88))))
    return plate


HELD_TEXT_W, HELD_H, PLATE_H, EDGE = 380, 118, 76, 24
PLATE_POWERS = False      # trial: the abilities as plates in place of the rings
TRIAL_STATES = {}         # trial: a state to show in place of the capture's, by power index


# Dante's abilities in the HUD's order (`DanteHeroKit`'s glyphs): the boards draw the authored icons at full brightness,
# not the captured ones, which the game had dimmed (the uncharged ultimate to about a quarter).
POWER_GLYPHS = ["DanteShield", "DanteBoulder", "DanteFissure"]


def icon_art(i):
    """An ability's authored icon (`Resources/UI/ability-icons`, the outlined cut the HUD loads), trimmed to its ink."""
    return trim(Image.open(UI / "ability-icons" / (POWER_GLYPHS[i] + ".png")).convert("RGBA"))


def power_icon(cid, m, i, size, dim=False, muted=False):
    """`muted` is the game's not-ready look (`TumpAbilitySymbol.Muted`, half brightness); `dim` is the trial's see-through."""
    ic = icon_art(i)
    if muted:
        r_, g_, b_, a_ = ic.split()
        ic = Image.merge("RGBA", [ch.point(lambda v: v // 2) for ch in (r_, g_, b_)] + [a_])
    k = size / max(ic.size)
    ic = ic.resize((max(1, int(ic.width * k)), max(1, int(ic.height * k))), Image.LANCZOS)
    if dim:
        ic.putalpha(ic.getchannel("A").point(lambda v: int(v * .6)))
    return stroked(ic, alpha=.35)


def power_state(m, i):
    if i in TRIAL_STATES:
        return TRIAL_STATES[i]
    st = lay(m, "PowerState", "Power%d" % i)
    return st[0]["text"] if st else ""


def ability_plate(b, m, cid, i, key, place, y, expanded=False, name=None, line=None, closed=False):
    """One ability as a kill-feed plate. Compact (the live HUD): its icon in the dark section, the key, and its state:
    a cooldown in amber, a charge in amber over a lime bar along the plate's foot, nothing when ready. Expanded (F1 held):
    the same plate, taller, with the key and name on one row and a short line under them."""
    h = HELD_H if expanded else PLATE_H
    section = 104 if expanded else 88
    lean = h * LEAN
    state = power_state(m, i)
    ult = state.endswith("%")
    cooling = bool(state) and not ult
    charge = int(state[:-1]) / 100 if ult and state[:-1].isdigit() else 1
    cap = keycap_art(key, 34 if expanded else 40)
    if expanded:
        body_w = HELD_TEXT_W
    else:
        # One width for every plate of a kind, wide enough for READY, so a skill's plate never changes size.
        body_w = cap.width + 16 + font(DARUMA, 30).getlength("READY")
    w = int(section + lean / 2 + EDGE + body_w + EDGE + (lean if closed else 0))
    x = 1920 - w if place == "right" else 960 - w / 2
    b.place("ability-plate-%dx%d%s" % (w, h, "-c" if closed else ""), held_plate_art(w, h, section, closed), x, y, "Ability")
    ic = power_icon(cid, m, i, 80 if expanded else 60, dim=cooling and not expanded)
    sx = x + lean / 2 + 4 + section / 2
    b.place("ability-icon-%d-%d-%s" % (i, ic.width, cooling), ic, sx - ic.width / 2, y + h / 2 - ic.height / 2, "Icon")
    tx = x + section + lean / 2 + EDGE
    if expanded:
        b.place("key-%s-%d" % (key, cap.height), cap, tx, y + 32 - cap.height / 2, "Key")
        left(b, tx + cap.width + 12, y + 32, name, fit_size(name, 30, HELD_TEXT_W - cap.width - 12), fill=WHITE, name="Name")
        para(b, tx, y + 54, wrap_lines(line, 24, HELD_TEXT_W, weight=600), 24, 30, fill=WHITE, weight=600)
    else:
        b.place("key-%s-%d" % (key, cap.height), cap, tx, y + h / 2 - cap.height / 2, "Key")
        ready = (ult and charge >= 1) or not state
        words = "READY" if ready else state
        left(b, tx + cap.width + 16, y + h / 2, words, 30, fill=PAINT["yellow"][1] if ready else LIME if ult else WHITE, name="State")
        if ult:
            bar_x, bar_w = x + lean * .2 + 10, w - lean * 1.4 - 20
            b.place("ult-track-%d" % bar_w, Image.new("RGBA", (int(bar_w), 6), (0x14, 0x13, 0x1A, 220)), bar_x, y + h - 10, "Charge track")
            if charge > 0:
                b.place("ult-fill-%d" % int(bar_w * charge), Image.new("RGBA", (max(1, int(bar_w * charge)), 6), LIME + (255,)), bar_x, y + h - 10, "Charge")
    return x, w


def plate_powers(b, m, cid, expanded=False, base=1080 - EDGE, ult_base=1080 - EDGE):
    """The abilities as plates: E and Q stacked flush right at the foot of the screen, Q at the bottom, the ultimate
    centred at the foot. Held open (F1), the same plates grow to carry the name and a line, and a small plate above the
    skills names the page with the other page dimmed."""
    if not lay(m, "Power0"):
        return
    h = HELD_H if expanded else PLATE_H
    y = base - h
    skills = [(0, "E"), (1, "Q")]
    for idx, key in reversed(skills):
        b.group = "Ability " + key
        name, line = HELD[idx][0], HELD[idx][1]
        ability_plate(b, m, cid, idx, key, "right", y, expanded, name, line)
        y -= h + 12
    if expanded:
        b.group = "Page"
        ph, psec = 60, 76
        af = font(DARUMA, 28)
        pw = int(psec + ph * LEAN / 2 + EDGE + af.getlength("ATTACKING") + 22 + af.getlength("DEFENDING") + EDGE)
        px, py = 1920 - pw, y + h - ph
        b.place("page-plate-%d" % pw, held_plate_art(pw, ph, psec), px, py, "Page")
        cap = keycap_art("F", 34)  # Interact turns the page while F1 is held
        b.place("key-F-34", cap, px + ph * LEAN / 2 + 4 + psec / 2 - cap.width / 2, py + ph / 2 - cap.height / 2, "Key")
        tx = px + psec + ph * LEAN / 2 + EDGE
        left(b, tx, py + ph / 2, "ATTACKING", 28, fill=WHITE, name="Page")
        left(b, tx + af.getlength("ATTACKING") + 22, py + ph / 2, "DEFENDING", 28, fill=(0x8C, 0x8A, 0x94), name="Other page")
    b.group = "Ability X"
    ability_plate(b, m, cid, 2, "X", "centre", ult_base - h, expanded, HELD[2][0], HELD[2][1], closed=True)


def disc_plate_art(d, fill, colour, alpha=.82, edge=5):
    """A round ability in the plates' material: the grey stroke and dark slab, the woven grey band, a nearly solid dark
    centre with an inner shadow for the icon, and the state as an arc drawn round the woven band."""
    S, drop = 3, 5
    D, H = d * S, (d + drop) * S
    body = Image.new("L", (D, H))
    ImageDraw.Draw(body).ellipse((0, 0, D - 1, D - 1), fill=255)
    swept = body
    for dy in range(S, drop * S + 1, S):
        swept = ImageChops.lighter(swept, ImageChops.offset(body, 0, dy))
    im = Image.new("RGBA", (D, H))
    im.paste(GLASS + (235,), (0, 0), swept)
    im.paste((0x8C, 0x8A, 0x94, 255), (0, 0), body)
    # The stroke itself is the state: the filled part of the edge turns gold (or lime), clockwise from the top.
    r = edge * S
    if fill > 0:
        wedge = Image.new("L", (D, H))
        if fill >= 1:
            ImageDraw.Draw(wedge).ellipse((0, 0, D - 1, D - 1), fill=255)
        else:
            ImageDraw.Draw(wedge).pieslice((0, 0, D - 1, D - 1), -90, -90 + 360 * fill, fill=255)
        im.paste(tuple(colour) + (255,), (0, 0), ImageChops.multiply(wedge, body))
    inner = Image.new("L", (D, H))
    ImageDraw.Draw(inner).ellipse((r, r, D - 1 - r, D - 1 - r), fill=255)
    patch = _weave((0, 0, 0), (255, 255, 255)).crop((300, 200, 300 + int(d * WEAVE_SHRINK), 200 + int((d + drop) * WEAVE_SHRINK))).convert("L").resize((D, H), Image.LANCZOS)
    tones = ImageOps.colorize(ImageOps.autocontrast(patch, cutoff=2), (0x18, 0x17, 0x1E), (0x56, 0x54, 0x60)).convert("RGBA")
    tones.putalpha(215)
    im.paste(tones, (0, 0), inner)
    band = int(d * .06) * S
    box = Image.new("L", (D, H))
    c = r + band
    ImageDraw.Draw(box).ellipse((c, c, D - 1 - c, D - 1 - c), fill=255)
    im.paste((0x2C, 0x2B, 0x35, 255), (0, 0), box)
    shadow = ImageChops.invert(ImageChops.offset(box, 0, 4 * S)).filter(ImageFilter.GaussianBlur(5 * S))
    im.paste((0x08, 0x07, 0x0C, 255), (0, 0), ImageChops.multiply(shadow.point(lambda v: min(255, int(v * 1.5))), box))
    out = see_through(im.resize((d, d + drop), Image.LANCZOS), alpha)
    out.putalpha(ImageChops.lighter(out.getchannel("A"), box.resize((d, d + drop), Image.LANCZOS).point(lambda v: int(v * .9))))
    return out


def cool_fill_art(d, left_):
    """A skill cooling down: grey over the disc's dark centre, as tall as the time still to go, so it drains from the
    top down to nothing as the skill comes back."""
    S = 3
    D = d * S
    r = 5 * S   # inside the edge: over the woven band and the centre alike
    disc = Image.new("L", (D, D))
    ImageDraw.Draw(disc).ellipse((r, r, D - 1 - r, D - 1 - r), fill=255)
    top = r + (D - 2 * r) * (1 - left_)
    level = Image.new("L", (D, D))
    ImageDraw.Draw(level).rectangle((0, top, D, D), fill=255)
    im = Image.new("RGBA", (D, D), (0xD8, 0xD6, 0xDE, 0))
    im.putalpha(ImageChops.multiply(disc, level).point(lambda v: int(v * .30)))
    return im.resize((d, d), Image.LANCZOS)


COOLDOWNS = {"E": 40, "Q": 35}   # the trial's totals, from Dante's sheet


DISC_POWERS = False   # trial: the abilities as round plates in place of the rings


def disc_powers(b, m, cid):
    """The abilities as round plates, where the rings sit: the two skills bottom right, the ultimate bottom centre.
    Gold arc and a full icon when ready; a skill cooling down dims its icon under its seconds; the ultimate fills a
    lime arc as it charges. The game's own dark key prompt sits under each."""
    if not lay(m, "Power0"):
        return
    gold = PAINT["yellow"][1]
    spots = [(0, "E", 1730, 962, 112), (1, "Q", 1846, 962, 112), (2, "X", 960, 952, 148)]
    for i, key, cx, cy, d in spots:
        b.group = "Ability " + key
        state = power_state(m, i)
        ult = state.endswith("%")
        if ult:
            fill = int(state[:-1]) / 100 if state[:-1].isdigit() else 1
            ready, colour = fill >= 1, gold if fill >= 1 else LIME
        else:
            ready = not state
            fill, colour = (1, gold) if ready else (0, gold)
        if ready:
            centred_art(b, ring_glow_art(int(d * 1.6), int(d * .85)), cx, cy, "disc-glow-%d" % d)
        art = disc_plate_art(d, fill, colour)
        b.place("disc-%d-%d-%02x" % (d, int(fill * 100), colour[0]), art, cx - d / 2, cy - d / 2, "Ability")
        ic = power_icon(cid, m, i, int(d * (.46 if ult and not ready else .58)), dim=not ready and not ult, muted=not ready)
        centred_art(b, ic, cx, cy - (12 if ult and not ready else 0), "disc-icon-%d-%d-%s" % (i, ic.width, ready))
        if not ready and not ult:
            left_ = min(1, float(state.rstrip("s")) / COOLDOWNS.get(key, 30))
            b.place("disc-cool-%d-%d" % (d, int(left_ * 100)), cool_fill_art(d, left_), cx - d / 2, cy - d / 2, "Cooldown")
        if not ready:
            if ult:
                number(b, cx, cy + d * .22, state, 22, align="c", name="Charge")
            else:
                number(b, cx, cy, state, 30, align="c", name="Cooldown")
        cap = keycap_art(key, 34)
        b.place("disc-key-%s" % key, cap, cx - cap.width / 2, cy + d / 2 + 14, "Key")


def discs_trial(cid):
    """Trial (owner, 2026-11-07): the live HUD with round plates for the abilities, Q shown cooling down."""
    global DISC_POWERS, TRIAL_STATES
    DISC_POWERS, TRIAL_STATES = True, {1: "12"}
    try:
        return live("match-hud-thrower")
    finally:
        DISC_POWERS, TRIAL_STATES = False, {}


def ref_plate_art(w, h, head, cut=None, alpha=.82, rad=14):
    """The F1 reference's plate (owner, 2026-11-07): the kill feed's grey stroke, dark slab and woven grey with soft
    corners, and its nearly solid dark box as a header strip `head` tall. `cut` (cx, cy, r) takes a circle out of the
    box so the weave shows clean round the ability's disc."""
    S, drop = 3, 5
    W, H = w * S, (h + drop) * S
    body = Image.new("L", (W, H))
    ImageDraw.Draw(body).rounded_rectangle((0, 0, W - 1, h * S - 1), rad * S, fill=255)
    swept = body
    for dy in range(S, drop * S + 1, S):
        swept = ImageChops.lighter(swept, ImageChops.offset(body, 0, dy))
    im = Image.new("RGBA", (W, H))
    im.paste(GLASS + (235,), (0, 0), swept)
    im.paste((0x8C, 0x8A, 0x94, 255), (0, 0), body)
    inner = Image.new("L", (W, H))
    ImageDraw.Draw(inner).rounded_rectangle((3 * S, 3 * S, W - 1 - 3 * S, h * S - 1 - 3 * S), (rad - 3) * S, fill=255)
    patch = _weave((0, 0, 0), (255, 255, 255)).crop((300, 200, 300 + int(w * WEAVE_SHRINK), 200 + int((h + drop) * WEAVE_SHRINK))).convert("L").resize((W, H), Image.LANCZOS)
    tones = ImageOps.colorize(ImageOps.autocontrast(patch, cutoff=2), (0x18, 0x17, 0x1E), (0x56, 0x54, 0x60)).convert("RGBA")
    tones.putalpha(215)
    im.paste(tones, (0, 0), inner)
    bx = Image.new("L", (W, H))
    ImageDraw.Draw(bx).rectangle((0, 0, W, head * S), fill=255)
    if cut:
        cx, cy, r = cut
        ImageDraw.Draw(bx).ellipse(((cx - r) * S, (cy - r) * S, (cx + r) * S, (cy + r) * S), fill=0)
    bx = ImageChops.multiply(bx, inner)
    im.paste((0x2C, 0x2B, 0x35, 255), (0, 0), bx.point(lambda v: int(v * .75)))
    shadow = ImageChops.invert(ImageChops.offset(bx, 0, 4 * S)).filter(ImageFilter.GaussianBlur(5 * S))
    im.paste((0x08, 0x07, 0x0C, 255), (0, 0), ImageChops.multiply(shadow.point(lambda v: min(255, int(v * 1.5))), bx))
    plate = see_through(im.resize((w, h + drop), Image.LANCZOS), alpha)
    plate.putalpha(ImageChops.lighter(plate.getchannel("A"), bx.resize((w, h + drop), Image.LANCZOS).point(lambda v: int(v * .75))))
    return plate


REF_DISC, REF_HEAD, REF_GAP, REF_TEXT_W = 88, 52, 12, 360


def ref_mini_disc(cid, m, i, d):
    """A small copy of the ability's disc. The icon goes on bare: the HUD's dark stroke is there to hold an icon over
    the world, and at this size on the disc's dark centre it only darkens the icon."""
    art = disc_plate_art(d, 0, (0, 0, 0))
    ic = icon_art(i)
    k = int(d * .62) / max(ic.size)
    ic = ic.resize((max(1, int(ic.width * k)), max(1, int(ic.height * k))), Image.LANCZOS)
    art.alpha_composite(ic, (int(d / 2 - ic.width / 2), int(d / 2 - ic.height / 2)))
    return art


def ref_size(i, width=REF_TEXT_W):
    rows = wrap_lines(HELD[i][1], 24, width, 600)
    tw = max(font(DARUMA, 28).getlength(HELD[i][0]), max(font(GRAND, 24, 600).getlength(r_) for r_ in rows))
    w = int(6 + REF_DISC / 2 + REF_GAP + 14 + tw + 26)
    h = REF_HEAD + 10 + len(rows) * 30 + 18
    return rows, w, h


def ref_card(b, cid, m, x, bottom, i, key, w=None, align="r", width=REF_TEXT_W):
    """One power: a mini copy of its disc on the plate's left edge with its key under it (as the HUD prints the key
    under the real disc), a clean weave circle cut from the header round the disc, the name in the header and the
    line on the weave. Placed by its bottom edge; `x` is the right edge, or the centre. Returns the top."""
    rows, w0, h = ref_size(i, width)
    w = w or w0
    top = bottom - h
    left_ = x - w if align == "r" else x - w / 2
    dcx, dcy, ring_r = 6, h / 2 - 2, REF_DISC / 2 + REF_GAP
    b.place("ref-plate-%dx%d" % (w, h), ref_plate_art(w, h, REF_HEAD, cut=(dcx, dcy, ring_r)), left_, top, "Power")
    b.place("ref-disc-%d" % i, ref_mini_disc(cid, m, i, REF_DISC), left_ + dcx - REF_DISC / 2, top + dcy - REF_DISC / 2, "Disc")
    cap = keycap_art(key, 32)
    b.place("ref-key-%s" % key, cap, left_ + dcx - cap.width / 2, top + dcy + REF_DISC / 2 - 6, "Key")
    tx = left_ + dcx + ring_r + 14
    # A few pixels under the box's middle: its inner shadow darkens the top and makes centred words read high.
    left(b, tx, top + REF_HEAD / 2 + 3, HELD[i][0], 28, fill=WHITE, name="Name")
    para(b, tx, top + REF_HEAD + 8, rows, 24, 30, fill=WHITE, weight=600)
    return top


def held_reference(b, m, cid):
    """F1 held (owner, 2026-11-07): each power on a soft-cornered kill-feed plate over its disc, a mini copy of the disc
    and its key on the plate's left edge. The skills stack right-aligned with Q nearest its disc and share one width;
    the ultimate's plate centres over its own disc; a header-only plate on top names the page, with the page key and
    the other page dimmed."""
    b.group = "Powers"
    right, gap = 1896, 14
    skill_w = max(ref_size(1)[1], ref_size(0)[1])
    y = 900
    for idx, key in ((1, "Q"), (0, "E")):
        b.group = "Power " + key
        y = ref_card(b, cid, m, right, y, idx, key, w=skill_w) - gap
    b.group = "Page"
    cap = keycap_art("F", 30)
    af, of = font(DARUMA, 28), font(GRAND, 24, 700)
    pad, ph = 22, REF_HEAD
    pw = skill_w - (6 + REF_DISC // 2 + REF_GAP)  # the skills' plates start where their discs' weave circles end
    pw = max(pw, int(pad + af.getlength("ATTACKING") + 20 + cap.width + 10 + of.getlength("DEFENDING") + pad))
    px, py = right - skill_w, y - ph
    pw = skill_w
    b.place("page-plate-%d" % pw, ref_plate_art(pw, ph, ph), px, py, "Page")
    pad = 6 + REF_DISC // 2 + REF_GAP + 14
    left(b, px + pad, py + ph / 2 + 3, "ATTACKING", 28, fill=WHITE, name="Page")
    kx = px + pad + af.getlength("ATTACKING") + 20
    b.place("page-key-F", cap, kx, py + ph / 2 + 3 - cap.height / 2, "Key")
    left(b, kx + cap.width + 10, py + ph / 2 + 3, "DEFENDING", 24, face=GRAND, weight=700, fill=(0xB8, 0xB6, 0xC0), name="Other page")
    b.group = "Power X"
    ref_card(b, cid, m, 960, 952 - 148 / 2 - 24, 2, "X", align="c", width=400)


# Ultimate card

def ultimate_card(b, m, cid):
    """Whose ultimate is live: no plate, as the score corner has none. The ability in its ready ring and glow, on a soft
    band of its seat's colour running off the screen's edge."""
    b.group = "Ultimate"
    cy = 800
    colour = tint(lay(m, "Motif")[0])
    b.place("ult-band", glow_band_art(560, 92, peak=170, colour=colour), -220, cy - 46, "Band")
    centred_art(b, ring_glow_art(176, 96), 96, cy, "ult-card-glow")
    centred_art(b, ring_art(104, 1, PAINT["yellow"][1], 9), 96, cy, "ult-card-ring")
    b.place("ult-icon", stroked(sprite(lay(m, "Icon", "Card")[0], size=(68, 68)), alpha=.35), 96 - 40, cy - 40, "Ability")


# Catch replay

def brackets(b):
    x0, y0, x1, y1, L, T = 83, 83, 1836, 997, 190, 16
    im = Image.new("L", (1920, 1080))
    d = ImageDraw.Draw(im)
    for (cx, cy, sx, sy) in ((x0, y0, 1, 1), (x1, y0, -1, 1), (x0, y1, 1, -1), (x1, y1, -1, -1)):
        d.polygon(wobble([(cx, cy), (cx + sx * L, cy), (cx + sx * L, cy + sy * T), (cx + sx * T, cy + sy * T),
                          (cx + sx * T, cy + sy * L), (cx, cy + sy * L)] if sx * sy > 0 else
                         [(cx, cy), (cx, cy + sy * L), (cx + sx * T, cy + sy * L), (cx + sx * T, cy + sy * T), (cx + sx * L, cy + sy * T), (cx + sx * L, cy)], 1.3, cx + cy), fill=255)
    rim = im.filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.GaussianBlur(1))
    lay_ = Image.new("RGBA", (1920, 1080), GLASS + (255,))
    lay_.putalpha(rim)
    white = Image.new("RGBA", (1920, 1080), WHITE + (255,))
    white.putalpha(im)
    lay_.alpha_composite(white)
    b.place("rec-brackets", lay_, 0, 0, "Frame")


def replay(b, m, cid):
    b.group = "Replay"
    brackets(b)
    x, y, w, h = 1500, 790, 240, 90
    b.place("rec-plate", hud_panel_art(w, h), x, y, "Recording")
    im = lettering(lay(m, "ReplayRecordLabel")[0]["text"], 52, WHITE, GLASS, GLASS, depth=5, edge_w=3, gap=3)
    b.place("rec-word", im, x + 44, y + h / 2 - im.height / 2, "REC")
    dot = Image.new("RGBA", (144, 144))
    ImageDraw.Draw(dot).ellipse((3, 3, 141, 141), fill=(0xF6, 0x4A, 0x48, 255))
    b.place("rec-dot", dot.resize((46, 46), Image.LANCZOS), x + w - 82, y + h / 2 - 23, "Record light")


# Guided training

def guided_bar(b, x, y, w, h, fill, key):
    """A thin sunk bar in the panel's slab colour, lime for what is done."""
    S = 3
    im = Image.new("RGBA", (w * S, h * S))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((0, 0, w * S - 1, h * S - 1), h * S // 2, fill=SLAB + (255,))
    if fill > 0:
        d.rounded_rectangle((0, 0, max(h * S, int(w * S * fill)) - 1, h * S - 1), h * S // 2, fill=LIME + (255,))
    b.place("guided-bar-%s" % key, im.resize((w, h), Image.LANCZOS), x, y, "Progress")


def clean_panel_art(w, h, fill=(0x6C, 0x1E, 0x32), drop=14, seed=61):
    """The dark panel's shape and rim with a brighter, flat berry face and no weave."""
    im = Image.new("RGBA", (w, h + drop))
    d = ImageDraw.Draw(im)
    d.polygon(wobble(chamfer(2, 2 + drop, w - 4, h - 4, 34), 1.6, seed), fill=SLAB + (255,))
    d.polygon(wobble(chamfer(2, 2, w - 4, h - 4, 34), 1.6, seed), fill=ROSE + (255,))
    d.polygon(wobble(chamfer(14, 14, w - 28, h - 28, 26), 1.2, seed + 1), fill=tuple(fill) + (255,))
    return im


def even_key(layer, height=46):
    """A key cap cut to its ink and sized to one height, so a row of keys lines up on its edges."""
    k = trim(sprite(layer, 2.0))
    return k.resize((max(1, round(k.width * height / k.height)), height), Image.LANCZOS)


def guided(b, m, cid):
    """Three groups only: where you are, what to do, how to leave. Every key is one height on the left margin; leaving
    takes its own row, with skip at the left margin and quit at the right."""
    b.group = "Training"
    e = lambda n, under=None: lay(m, n, under)[0]
    x, y, w = 20, 20, 780
    x0, x1 = x + 44, x + w - 44
    body = wrap_lines(e("LessonBody")["text"], 25, x1 - x0, 550)
    extra = (len(body) - 1) * 34
    h = 366 + extra
    b.place("guided-panel-%d" % h, see_through(clean_panel_art(w, h), .78), x, y, "Training")
    left(b, x0, y + 52, e("TrainingWord")["text"], 28, fill=CREAM, name="Training")
    tw = font(DARUMA, 28).getlength(e("LessonCounter")["text"])
    left(b, x1 - tw, y + 52, e("LessonCounter")["text"], 28, fill=CREAM, name="Lesson count")
    guided_bar(b, x0, y + 76, x1 - x0, 10, 1 / 20, "route")
    left(b, x0, y + 132, e("LessonTitle")["text"], 50, fill=WHITE, name="Lesson")
    para(b, x0, y + 168, body, 25, 34, fill=CREAM, weight=550)
    row1, row2 = y + 240 + extra, y + 310 + extra
    gap = 14
    kg = lay(m, "Key_F")
    if kg:
        g_ = even_key(kg[0], 50)
        b.place("key-F-g", g_, x0, row1 - 25, "Key")
        left(b, x0 + g_.width + gap, row1, e("Words", "KeyRow")["text"], 34, fill=LIME, name="Key action")
    sk, qt = lay(m, "Label", "SkipTrainingLesson")[0], lay(m, "Label", "QuitTraining")[0]
    enter = lay(m, "Key_ENTER")
    if enter:
        k_ = even_key(enter[0], 40)
        b.place("key-enter", k_, x0, row2 - 20, "Key")
        left(b, x0 + k_.width + gap, row2, sk["text"], 26, fill=CREAM, name="Skip")
    back = lay(m, "Key_BACKSPACE")
    label_w = font(DARUMA, 26).getlength(qt["text"])
    left(b, x1 - label_w, row2, qt["text"], 26, fill=CREAM, name="Quit")
    if back:
        k_ = even_key(back[0], 40)
        b.place("key-back", k_, x1 - label_w - gap - k_.width, row2 - 20, "Key")


# Training menu

def training_menu(b, m, cid):
    from bootkit import scrim
    scrim(b, (40, 8, 14, 130))
    panel(b, 50, 36, 1244, 990, seed=71, group="Training menu")
    t = lambda n, under=None: lay(m, n, under)[0]["text"]
    left(b, 112, 118, t("PauseTitle"), 84, fill=MAROON, name="Title")
    pf = lay(m, "TrainingPortrait")[0]
    pc = Image.new("RGBA", (200, 200), CARD + (255,))
    p = trim(Image.open(UI / "portraits" / (stem(pf) + ".png")).convert("RGBA"))
    p.thumbnail((180, 180), Image.LANCZOS)
    pc.alpha_composite(p, (100 - p.width // 2, 100 - p.height // 2))
    framed(b, 1096, 62, 140, 140, pc, "train-portrait", name="Portrait")
    head(b, 112, 238, t("Label", "Section_Player"), 32)
    rule(b, 112, 1230, 262)
    for cy, key, under, lab in ((332, "Character", "Row_Character", "CharacterChoice"), (432, "Defender", "Row_Defender", "DefenderChoice")):
        left(b, 112, cy, t("Label", under), 40, fill=MAROON, name=key)
        choice = t("Text", lab).replace("\u203a", ">").replace("\ufffd", ">").replace(">", "").strip()
        button(b, "yellow", 780, int(cy - 96 * FACE_LIP), 450, 96, choice + " >", 42)
    head(b, 112, 524, t("Label", "Section_Training rules"), 32)
    rule(b, 112, 1230, 548)
    rows = [("No cooldowns", "Row_No cooldowns"), ("Infinite skill charges", "Row_Infinite skill charges"), ("Full ultimate", None)]
    for k, (lab, under) in enumerate(rows):
        cy = 616 + k * 96
        left(b, 112, cy, t("Label", under) if under else lab, 40, fill=MAROON, name=lab)
        toggle(b, 990, int(cy - 40), False)
        if k < 2:
            rule(b, 112, 1230, cy + 48)
    by = 860
    button(b, "go", 110, by, 360, 118, "RESUME", 52)
    button(b, "yellow", 500, by, 360, 118, "SETTINGS", 46)
    button(b, "red", 890, by, 340, 118, "LEAVE", 52)


def fitted_hero(hero, size):
    im = trim(Image.open(UI / "portraits" / (hero + ".png")).convert("RGBA"))
    im.thumbnail((size, size), Image.LANCZOS)
    return im


# The arrival caption

def arrival(b, m, cid):
    """The film's caption: slimmer bars, the court in painted lettering on a soft band, and a quiet skip on the edge."""
    b.group = "Film"
    bar = Image.new("RGBA", (1920, 64), (0, 0, 0, 255))
    b.place("film-bar", bar, 0, 0, "Film bar top")
    b.place("film-bar", bar, 0, 1016, "Film bar bottom")
    t = lay(m, "Text")[0]
    im = lettering(t["text"], 120, WHITE, GLASS, GLASS, depth=10, edge_w=6, gap=4)
    b.place("arrival-word", im, 80, 780 - im.height / 2, "Court name")
    w, h = 170, 72
    b.place("skip-plate", hud_panel_art(w, h), 1920 - w - 40, 900, "Skip")
    d = Image.new("RGBA", (70, 44))
    dd = ImageDraw.Draw(d)
    for ox in (2, 34):
        dd.polygon([(ox, 2), (ox + 30, 22), (ox, 42)], fill=CREAM + (255,))
    b.place("skip-glyph", d, 1920 - w / 2 - 40 - 35, 900 + h / 2 - 22, "Fast forward")


# Screens

def base(cid, world_file="background.png"):
    b = Board()
    b.background = str(CAPTURES / cid / world_file)
    b.im.alpha_composite(Image.open(CAPTURES / cid / world_file).convert("RGBA").resize((1920, 1080)))
    return b


def live(cid, extras=()):
    b = base(cid)
    m = manifest(cid)
    hud(b, m, cid)
    for fn in extras:
        fn(b, m, cid)
    return b


def screen(extras=()):
    return lambda cid: live(cid, extras)


def replay_screen(cid):
    m = manifest(cid)
    layer = lay(m, "RecordedCatch")[0]
    b = Board()
    b.background = str(CAPTURES / cid / layer["file"])
    b.im.alpha_composite(raster(cid, layer).resize((1920, 1080)))
    replay(b, m, cid)
    prompt(b, m, cid)
    return b


def arrival_screen(cid):
    b = base(cid)
    arrival(b, manifest(cid), cid)
    return b


def powers_screen(cid):
    global DISC_POWERS
    DISC_POWERS = True    # the HUD's round abilities, with the open reference over them
    try:
        return live(cid, (held_reference,))
    finally:
        DISC_POWERS = False


def plates_trial(cid):
    """Trial (owner, 2026-11-07): the live HUD with its abilities as plates, Q shown cooling down."""
    global PLATE_POWERS, TRIAL_STATES
    PLATE_POWERS, TRIAL_STATES = True, {1: "12s"}
    try:
        return live("match-hud-thrower")
    finally:
        PLATE_POWERS, TRIAL_STATES = False, {}


def guided_screen(cid):
    b = base(cid)
    m = manifest(cid)
    # The reveal first, so its rays pass behind the training panel.
    taya_reveal(b, m, cid, dx=120)
    guided(b, m, cid)
    return b


def training_screen(cid):
    b = base(cid)
    training_menu(b, manifest(cid), cid)
    return b


def status_land_screen():
    global STATUS_LANDING
    STATUS_LANDING = True
    try:
        return live("match-hud-crowded", (status_landed,))
    finally:
        STATUS_LANDING = False


SCREENS = {
    "match-hud-thrower": screen(),
    "match-hud-scorepop": lambda _: live("match-hud-thrower", (others_pop,)),
    "match-status-land": lambda _: status_land_screen(),
    "match-tab": lambda _: live("match-hud-thrower", (tab_board,)),
    "match-hud-taya": screen(),
    "match-hud-classic": screen(),
    "match-hud-crowded": screen((moment,)),
    "match-countdown": screen((countdown,)),
    "match-arrival": arrival_screen,
    "match-tayareveal": screen((taya_reveal,)),
    "match-candown": screen((can_down,)),
    "match-catch": replay_screen,
    "match-moment-1": screen((moment,)),
    "match-moment-2": screen((moment,)),
    "match-moment-3": screen((moment,)),
    "match-emote": lambda cid: emote_screen(cid),
    "match-powers": powers_screen,
    "match-hud-plates": plates_trial,
    "match-hud-discs": discs_trial,
    "match-ultimate-card": screen((ultimate_card,)),
    "match-guided": guided_screen,
    "match-training-menu": training_screen,
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
