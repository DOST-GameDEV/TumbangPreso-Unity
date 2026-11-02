"""The boot kit: every reworked screen drawn in the boot screen's language (owner, 2026-10-30).

Boot and HOME stay as they are. Everything else uses these pieces:

- Ground: the boot weave repainted berry (`WEAVE`), not darkened.
- Panels: a cream card in the boot field's colours, chamfered corners, a lightly wobbled rim,
  a faint cream weave on its face and the slab 14 units straight below.
- Buttons: one shape, the boot CREATE button, repainted per role. Lime for the main action,
  yellow for secondary actions and arrow keys, red for leaving and back, cream for unselected
  tabs. Labels are solid Darumadrop in a dark shade of their own button, with no outline.
  The striped guest button is retired.
- Tabs, toggles and sliders: the SIGN UP / SIGN IN track with its lime pill.
- Fields and drop-downs: the boot field with its icon painted out.
- Type: Darumadrop for titles, buttons and labels; Grandstander for sentences.

Every piece is drawn onto a `Board`, which keeps a flat preview and a layer list: each kit
piece once as a PNG (a linked smart object in the PSD) and each label as live text placed by
its ink. `bootscreens.py` lays the screens out and writes the Photoshop job.

Run it to redraw the approved style frames:

    py -3 tools/ui_artboards/bootkit.py --out ArtSource/ui/revamp-2026-10-30-boot/style-frames
"""
import argparse
import math
import os
import random
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "Assets/TumbangPreso/Resources/UI/owner-menu-edits"
FONTS = Path(os.environ["LOCALAPPDATA"]) / "Microsoft/Windows/Fonts"
DARUMA = str(FONTS / "DarumadropOne-Regular.ttf")
GRAND = str(FONTS / "Grandstander-VariableFont_wght.ttf")

MAROON, CREAM, TAN, TAN_D, INK = (0x86, 0x14, 0x22), (0xF3, 0xEA, 0xE2), (0xE2, 0xB4, 0x8C), (0xB8, 0x86, 0x60), (0x4A, 0x10, 0x18)
GREEN_INK, WHITE, TITLE_EDGE = (0x0F, 0x59, 0x13), (255, 255, 255), (0x8F, 0x14, 0x1F)
SOFT_RED, FIELD = (0xB1, 0x2F, 0x36), (0xE2, 0xD7, 0xD8)
WEAVE = ((0x6A, 0x1B, 0x2C), (0x9A, 0x34, 0x45))  # berry, in place of boot's salmon

# Each role repaints the CREATE button's slab, face and highlight; the label is solid.
BUTTONS = {
    "go": (None, (0x2C, 0x4A, 0x0E)),
    "yellow": (((0xC2, 0x6A, 0x12), (0xF6, 0xBC, 0x3A), (0xFF, 0xDE, 0x7A)), (0xA8, 0x4E, 0x0C)),
    "red": (((0x7E, 0x10, 0x1C), (0xC9, 0x2C, 0x36), (0xEC, 0x6A, 0x66)), (0x6A, 0x0C, 0x16)),
    "plain": (((0xB8, 0x96, 0x78), (0xF1, 0xE7, 0xDE), (0xFF, 0xFA, 0xF4)), MAROON),
}
# The CREATE paint has a highlight lip on top, so its flat face centres lower than half.
FACE_LIP, FACE_PILL = 0.45, 0.47


# Photoshop faces for the two families. Grandstander is variable here and static there.
def ps_name(face, weight):
    if face == DARUMA:
        return "DarumadropOne-Regular"
    w = weight or 400
    return "GrandstanderRoman-" + ("Regular" if w < 520 else "SemiBold" if w < 680 else "Bold")


class Board:
    """One screen: a flat preview plus the layers the PSD builder places."""

    def __init__(self, base=None):
        self.im = base.copy() if base is not None else Image.new("RGBA", (1920, 1080))
        self.layers = []
        self.pieces = {}
        self.group = "UI"
        self.background = None

    def place(self, key, piece, x, y, name=None):
        x, y = int(round(x)), int(round(y))
        self.pieces.setdefault(key, piece)
        self.im.alpha_composite(piece, (x, y))
        self.layers.append({"kind": "sprite", "key": key, "name": name or key, "x": x, "y": y,
                            "w": piece.width, "h": piece.height, "path": [self.group]})

    def write(self, xy, lines, f, fill, ps, size, pitch=0, stroke=0, edge=None, align="Left", name=None):
        lines = [lines] if isinstance(lines, str) else lines
        d = ImageDraw.Draw(self.im)
        ink = None
        for i, s in enumerate(lines):
            at = (xy[0], xy[1] + i * pitch)
            d.text(at, s, font=f, fill=fill, stroke_width=stroke, stroke_fill=edge)
            l, t, r, b = d.textbbox(at, s, font=f, stroke_width=stroke)
            ink = (l, t, r, b) if ink is None else (min(ink[0], l), min(ink[1], t), max(ink[2], r), max(ink[3], b))
        effects = [{"type": "outline", "dx": stroke, "dy": stroke, "color": hexof(edge), "alpha": 1}] if stroke else []
        self.layers.append({
            "kind": "text", "name": name or "Label", "text": "\n".join(lines), "lines": lines, "ps": ps,
            "size": size, "style": "Normal", "align": "Middle" + align, "lineSpacing": 1, "pitch": pitch,
            "color": hexof(fill), "opacity": 1, "ink": list(ink), "rect": list(ink), "angle": 0,
            "effects": effects, "fit": "match" if len(lines) == 1 else "shrink", "path": [self.group]})


def hexof(rgb):
    return "%02X%02X%02X" % tuple(rgb[:3])


def art(name):
    return Image.open(ART / (name + ".png")).convert("RGBA")


def font(path, size, weight=None):
    f = ImageFont.truetype(path, size)
    if weight:
        f.set_variation_by_axes([weight])
    return f


def slice3(im, w, h, cap=0.7):
    """Scale to height h, keep both painted ends, stretch the middle."""
    s = h / im.height
    im = im.resize((max(1, round(im.width * s)), h), Image.LANCZOS)
    c = min(int(h * cap), im.width // 2 - 1, w // 2)
    out = Image.new("RGBA", (w, h))
    out.paste(im.crop((0, 0, c, h)), (0, 0))
    if w - 2 * c > 0:
        out.paste(im.crop((c, 0, im.width - c, h)).resize((w - 2 * c, h), Image.LANCZOS), (c, 0))
    out.paste(im.crop((im.width - c, 0, im.width, h)), (w - c, 0))
    return out


def weave(dark, light, size=(1920, 1080)):
    """The boot weave repainted between two colours; its strands keep their light and shade."""
    g = np.asarray(art("login-background").resize(size).convert("L"), dtype=np.float32)
    t = ((g - g.min()) / max(1.0, g.max() - g.min()))[..., None]
    out = np.array(dark, np.float32) * (1 - t) + np.array(light, np.float32) * t
    return Image.fromarray(out.astype(np.uint8)).convert("RGBA")


def ground(b):
    b.group = "Ground"
    b.place("ground", weave(*WEAVE), 0, 0, "Ground: berry weave")


def world(b, path, dim=(40, 8, 14, 110)):
    """A game render under the menu, dimmed. The render is the PSD's background layer."""
    b.background = str(path)
    b.im.alpha_composite(Image.open(path).convert("RGBA").resize((1920, 1080)))
    if dim:
        scrim(b, dim)


def scrim(b, rgba):
    b.group = "Dim"
    b.place("dim-%02x%02x%02x%02x" % rgba, Image.new("RGBA", (1920, 1080), rgba), 0, 0, "Dim")


def wobble(points, amp, seed):
    rnd = random.Random(seed)
    out = []
    for i, (x0, y0) in enumerate(points):
        x1, y1 = points[(i + 1) % len(points)]
        n = max(2, int(math.hypot(x1 - x0, y1 - y0) / 40))
        for k in range(n):
            t = k / n
            out.append((x0 + (x1 - x0) * t + rnd.uniform(-amp, amp), y0 + (y1 - y0) * t + rnd.uniform(-amp, amp)))
    return out


def chamfer(x, y, w, h, c):
    return [(x + c, y), (x + w, y), (x + w, y + h - c), (x + w - c, y + h), (x, y + h), (x, y + c)]


def panel_art(w, h, drop=14, seed=1):
    """A painted card: chamfered corners, a hand-wobbled rim, the slab straight below."""
    im = Image.new("RGBA", (w, h + drop))
    d = ImageDraw.Draw(im)
    d.polygon(wobble(chamfer(2, 2 + drop, w - 4, h - 4, 34), 1.6, seed), fill=TAN_D)
    d.polygon(wobble(chamfer(2, 2, w - 4, h - 4, 34), 1.6, seed), fill=TAN)
    mask = Image.new("L", im.size)
    ImageDraw.Draw(mask).polygon(wobble(chamfer(16, 16, w - 32, h - 32, 26), 1.2, seed + 1), fill=255)
    im.paste(weave((0xEC, 0xE0, 0xD5), (0xF6, 0xEE, 0xE7), im.size), (0, 0), mask)
    return im


def panel(b, x, y, w, h, seed=1, group="Panel"):
    b.group = group
    b.place("panel-%dx%d-s%d" % (w, h, seed), panel_art(w, h, seed=seed), x, y, "Panel")


def recolor(im, slab, face, light):
    """The lime button repainted: its slab, face and highlight each mapped to a new colour,
    with the brush variation in between kept."""
    rgb = np.asarray(im.convert("RGB"), dtype=np.float32)
    lum = rgb @ np.array([0.299, 0.587, 0.114], dtype=np.float32)
    stops = np.array([109.0, 191.0, 221.0])  # the lime slab, face and highlight
    cols = np.array([slab, face, light], dtype=np.float32)
    out = np.stack([np.interp(lum, stops, cols[:, c]) for c in range(3)], axis=-1)
    res = Image.fromarray(out.clip(0, 255).astype(np.uint8)).convert("RGBA")
    res.putalpha(im.getchannel("A"))
    return res


def button_art(kind, w, h):
    paint, _ = BUTTONS[kind]
    b = art("login3-primary")
    return slice3(recolor(b, *paint) if paint else b, w, h)


def text(b, xy, s, size, face=DARUMA, fill=INK, weight=None, stroke=0, stroke_fill=None, name=None):
    b.write(xy, s, font(face, size, weight), fill, ps_name(face, weight), size, stroke=stroke, edge=stroke_fill, name=name)


def para(b, x, y, lines, size, pitch, face=GRAND, fill=INK, weight=450, stroke=0, stroke_fill=None):
    """Several lines of a sentence, one text layer."""
    b.write((x, y), lines, font(face, size, weight), fill, ps_name(face, weight), size, pitch=pitch,
            stroke=stroke, edge=stroke_fill, name="Note")


def centred(b, cx, cy, s, size, face=DARUMA, fill=INK, weight=None, name=None):
    """Places text by its ink, so caps sit in the middle of what they label."""
    f = font(face, size, weight)
    l, t, r, bt = f.getbbox(s)
    b.write((cx - (l + r) / 2, cy - (t + bt) / 2), s, f, fill, ps_name(face, weight), size, align="Center", name=name)


def left(b, x, cy, s, size, face=DARUMA, fill=INK, weight=None, caps=True, name=None):
    """Left-aligned text centred on cy by its cap height (or x-height for sentences)."""
    f = font(face, size, weight)
    t, bt = f.getbbox("H")[1], f.getbbox("H")[3]
    if not caps:
        t = f.getbbox("x")[1]
    b.write((x, cy - (t + bt) / 2), s, f, fill, ps_name(face, weight), size, name=name)


def right(b, x, cy, s, size, face=DARUMA, fill=INK, weight=None):
    """Right-aligned at x, caps centred on cy."""
    f = font(face, size, weight)
    l, t, r, bt = f.getbbox(s)
    b.write((x - r, cy - (f.getbbox("H")[1] + f.getbbox("H")[3]) / 2), s, f, fill, ps_name(face, weight), size, align="Right")


def paint_mid(piece, frac):
    """The paint's own horizontal centre on one row: the PNGs carry uneven margins."""
    row = np.asarray(piece.getchannel("A"))[int(piece.height * frac)]
    cols = np.nonzero(row > 128)[0]
    return (cols[0] + cols[-1]) / 2 if len(cols) else piece.width / 2


def button(b, kind, x, y, w, h, label, size=None):
    piece = button_art(kind, w, h)
    b.place("button-%s-%dx%d" % (kind, w, h), piece, x, y, "Button %s" % kind)
    centred(b, x + paint_mid(piece, FACE_LIP), y + h * FACE_LIP, label, size or int(h * 0.42), fill=BUTTONS[kind][1],
            name="Label")


def tabs(b, x, y, w, h, labels, on):
    b.place("track-%dx%d" % (w, h), slice3(art("login3-tabs-track"), w, h), x, y, "Tab track")
    cw = (w - 40) // len(labels)
    for i, s in enumerate(labels):
        cx = x + 20 + i * cw
        if i == on:
            ph = int(h * 0.74)
            b.place("pill-%dx%d" % (cw, ph), slice3(art("login3-tabs-pill"), cw, ph), cx, y + int(h * 0.05), "Tab pill")
            centred(b, cx + cw / 2, y + h * 0.05 + ph * FACE_PILL, s, int(h * 0.32), fill=GREEN_INK, name="Tab")
        else:
            centred(b, cx + cw / 2, y + h * 0.47, s, int(h * 0.32), fill=SOFT_RED, name="Tab")


def toggle(b, x, y, on, w=240, h=80):
    """OFF / ON on the tab track. ON is the lime pill on the right; OFF a cream pill on the
    left, so lime always means on."""
    if on:
        tabs(b, x, y, w, h, ["OFF", "ON"], 1)
        return
    b.place("track-%dx%d" % (w, h), slice3(art("login3-tabs-track"), w, h), x, y, "Toggle track")
    cw, ph = (w - 40) // 2, int(h * 0.74)
    pill = slice3(recolor(art("login3-tabs-pill"), *BUTTONS["plain"][0]), cw, ph)
    b.place("pill-off-%dx%d" % (cw, ph), pill, x + 20, y + int(h * 0.05), "Toggle pill (off)")
    centred(b, x + 20 + cw / 2, y + h * 0.05 + ph * FACE_PILL, "OFF", int(h * 0.32), fill=MAROON, name="Tab")
    centred(b, x + 20 + cw * 1.5, y + h * 0.47, "ON", int(h * 0.32), fill=SOFT_RED, name="Tab")


def tag(b, x, y, w, h, label, kind="go"):
    """A small pill, as PAUSED on the match menu: lime, or repainted like the buttons."""
    paint, ink = BUTTONS[kind]
    pill = art("login3-tabs-pill")
    pill = recolor(pill, *paint) if paint else pill
    b.place("pill-%s%dx%d" % ("" if kind == "go" else kind + "-", w, h), slice3(pill, w, h), x, y, "Tag")
    centred(b, x + w / 2, y + h * FACE_PILL, label, int(h * 0.48), fill=GREEN_INK if kind == "go" else ink, name="Tag")


def back(b, x, y, s=100):
    b.group = "Header"
    button(b, "red", x, y, s + 20, s, "<", int(s * 0.62))


def title(b, x, cy, s, size=86):
    """Screen title, its caps centred on cy (the back button's face)."""
    b.group = "Header"
    f = font(DARUMA, size)
    t, bt = f.getbbox("H")[1], f.getbbox("H")[3]
    b.write((x, cy - (t + bt) / 2), s, f, WHITE, ps_name(DARUMA, None), size, stroke=7, edge=TITLE_EDGE, name="Title")


def header(b, s):
    back(b, 60, 40)
    title(b, 200, 40 + 100 * FACE_LIP, s)


def knob_art():
    im = Image.new("RGBA", (64, 74))
    d = ImageDraw.Draw(im)
    d.ellipse((2, 12, 62, 72), fill=TAN_D)
    d.ellipse((2, 2, 62, 62), fill=CREAM, outline=TAN, width=6)
    return im


def slider(b, x, y, w, v):
    b.place("track-%dx58" % w, slice3(art("login3-tabs-track"), w, 58), x, y, "Slider track")
    fw = int((w - 40) * v)
    b.place("pill-%dx36" % max(fw, 60), slice3(art("login3-tabs-pill"), max(fw, 60), 36), x + 18, y + 8, "Slider fill")
    b.place("knob", knob_art(), x + 18 + fw - 32, y - 10, "Slider knob")


def field_art(w, h):
    """The boot field with its person icon painted out."""
    im = art("login3-field-user")
    ImageDraw.Draw(im).rectangle((130, 70, 420, 240), fill=FIELD)
    return slice3(im, w, h, cap=0.45)


def field(b, x, y, w, h, value, chevron=False, face=GRAND, weight=600, size=None, fill=INK):
    """A text field, or a drop-down when it carries a chevron. A placeholder takes TAN_D."""
    b.place("field-%dx%d" % (w, h), field_art(w, h), x, y, "Drop-down" if chevron else "Field")
    cy = y + h * 0.48
    if value:
        left(b, x + int(h * 0.42), cy, value, size or int(h * 0.36), face=face, weight=weight if face == GRAND else None, fill=fill,
             caps=face == DARUMA, name="Value")
    if chevron:
        centred(b, x + w - h * 0.55, cy, "v", int(h * 0.44), fill=MAROON, name="Chevron")


def rule(b, x0, x1, y):
    piece = Image.new("RGBA", (x1 - x0, 4), TAN + (255,))
    b.place("rule-%d" % (x1 - x0), piece, x0, y - 1, "Rule")


def head(b, x, cy, s, size=30):
    """A section heading inside a panel."""
    left(b, x, cy, s, size, fill=TAN_D, name="Section")


LIME = (0xBF, 0xD8, 0x3A)
WELL, WELL_RIM = (0x5C, 0x14, 0x22), (0x47, 0x0C, 0x18)
CARD = (0x8A, 0x2A, 0x3C)  # small cards are berry; cream is kept for the large panels (owner)


def frame_art(w, h, on, fill=CARD):
    """A small card (a portrait, a seat, a choice): berry inside a tan rim, lime when chosen,
    the slab straight below. Cream belongs to the large panels only. Same chamfered, wobbled
    outline as the panels and the empty-slot wells, so a filled slot and an open one match."""
    c, seed = min(34, int(min(w, h) * 0.22)), 3
    im = Image.new("RGBA", (w, h + 14))
    d = ImageDraw.Draw(im)
    d.polygon(wobble(chamfer(2, 16, w - 4, h - 4, c), 1.6, seed), fill=(0x8E, 0xA6, 0x22) if on else TAN_D)
    d.polygon(wobble(chamfer(2, 2, w - 4, h - 4, c), 1.6, seed), fill=LIME if on else TAN)
    d.polygon(wobble(chamfer(12, 12, w - 24, h - 24, max(6, c - 8)), 1.2, seed + 1), fill=fill)
    return im


def frame(b, x, y, w, h, on=False, name="Card"):
    b.place("card-%dx%d%s" % (w, h, "-on" if on else ""), frame_art(w, h, on), x, y, name + (" (chosen)" if on else ""))


def picture(b, x, y, w, h, path, key, cut=22):
    """A picture cropped to fill w x h, its top-left corner cut like the card it sits in."""
    p = Image.open(path).convert("RGBA")
    s = max(w / p.width, h / p.height)
    p = p.resize((max(w, round(p.width * s)), max(h, round(p.height * s))), Image.LANCZOS)
    p = p.crop(((p.width - w) // 2, (p.height - h) // 2, (p.width - w) // 2 + w, (p.height - h) // 2 + h))
    m = Image.new("L", (w, h))
    ImageDraw.Draw(m).polygon([(cut, 0), (w, 0), (w, h), (0, h), (0, cut)], fill=255)
    out = Image.new("RGBA", (w, h))
    out.paste(p, (0, 0), m)
    b.place(key, out, x, y, "Picture")


def well_art(w, h, seed=1):
    """An empty slot: the panel's shape sunk into the berry, like the tab track."""
    im = Image.new("RGBA", (w, h + 14))
    d = ImageDraw.Draw(im)
    d.polygon(wobble(chamfer(2, 2, w - 4, h - 4, 34), 1.6, seed), fill=WELL_RIM)
    d.polygon(wobble(chamfer(10, 16, w - 20, h - 18, 28), 1.2, seed + 1), fill=WELL)
    return im


def well(b, x, y, w, h, seed=1):
    b.place("well-%dx%d-s%d" % (w, h, seed), well_art(w, h, seed), x, y, "Empty slot")


def poster(b, path, dim=(40, 8, 14, 90)):
    """A court poster as the world behind a menu, dimmed."""
    b.group = "World"
    p = Image.open(path).convert("RGBA").resize((1920, 1080), Image.LANCZOS)
    b.place("world-" + Path(path).stem, p, 0, 0, "World: " + Path(path).stem)
    if dim:
        scrim(b, dim)


def subtitle(b, s, y=140):
    """The line under a screen title."""
    b.group = "Header"
    text(b, (60, y), s, 34, face=GRAND, fill=WHITE, weight=650, stroke=3, stroke_fill=TITLE_EDGE, name="Subtitle")


def wrap(s, size, width, face=GRAND, weight=450):
    f = font(face, size, weight)
    lines, line = [], ""
    for word in s.split():
        trial = (line + " " + word).strip()
        if f.getlength(trial) > width and line:
            lines.append(line)
            line = word
        else:
            line = trial
    return lines + [line]


def modal(b, w, h, heading, cy_offset=90, seed=12):
    """A dimmed popup: cream panel centred on screen, heading centred in its top."""
    scrim(b, (40, 8, 14, 130))
    x, y = (1920 - w) // 2, (1080 - h) // 2 - 10
    panel(b, x, y, w, h, seed=seed, group="Popup: " + heading.title())
    centred(b, 960, y + cy_offset, heading, 60, fill=MAROON, name="Heading")
    return x, y


# The approved style frames.

def settings_audio():
    from bootscreens import settings  # the shell lives with the other screens
    return settings("settings-audio")


def custom_rules():
    b = Board()
    ground(b)
    header(b, "CUSTOM MATCH")
    text(b, (60, 140), "HERO STRIKE · STANDARD · 8 rounds · 90s", 34, face=GRAND, fill=WHITE, weight=650,
         stroke=3, stroke_fill=TITLE_EDGE, name="Summary")
    b.group = "Tabs"
    tabs(b, 60, 194, 760, 120, ["THE MATCH", "THE ROOM"], 0)
    panel(b, 60, 326, 1800, 560, seed=7)
    rows = [("FORMAT", "STANDARD", "Choose how the round is played."),
            ("MODE", "HERO STRIKE", "Classic street play or Hero Strike powers."),
            ("ROUNDS", "8", "How many rounds make up this match."),
            ("ROUND LENGTH", "90s", "Time allowed for each round."),
            ("SCORE TARGET", "OFF", "Reach this score to finish early.")]
    for i, (k, v, h) in enumerate(rows):
        y = 366 + i * 100
        cy = y + 42
        left(b, 120, cy, k, 40, fill=MAROON)
        button(b, "yellow", 520, int(cy - 84 * FACE_LIP), 104, 84, "<", 46)
        centred(b, 807, cy, v, 40)
        button(b, "yellow", 990, int(cy - 84 * FACE_LIP), 104, 84, ">", 46)
        left(b, 1140, cy, h, 32, face=GRAND, weight=450, caps=False)
        if i < 4:
            rule(b, 110, 1800, y + 94)
    b.group = "Footer"
    text(b, (60, 968), "These are the shipped rules, so this match counts for your rank.", 30, face=GRAND,
         fill=WHITE, weight=600, stroke=3, stroke_fill=TITLE_EDGE, name="Note")
    button(b, "yellow", 1020, 924, 400, 112, "DEFAULT RULES", 40)
    button(b, "go", 1450, 924, 410, 112, "DONE", 54)
    return b


def pause():
    b = Board()
    world(b, ROOT / "Logs/ui-artboards/match-pause/background.png")
    panel(b, 70, 160, 700, 760, seed=9, group="Menu")
    tag(b, 120, 205, 190, 62, "PAUSED")
    text(b, (124, 290), "MATCH MENU", 72, fill=MAROON, name="Heading")
    text(b, (126, 392), "Game paused while this menu is open.", 32, face=GRAND, weight=450, name="Hint")
    button(b, "go", 120, 480, 600, 128, "RESUME", 58)
    button(b, "yellow", 120, 630, 600, 118, "SETTINGS", 50)
    button(b, "red", 120, 780, 600, 108, "LEAVE MATCH", 44)
    return b


FRAMES = {"custom_rules": custom_rules, "settings_audio": settings_audio, "pause": pause}


GAME_KIT = ROOT / "Assets/TumbangPreso/Resources/UI/boot-kit"


def export_game_kit():
    """The kit as game textures, imported like the boot art (owner-menu-edits/login3-*):
    the buttons and pills repainted at full size, the ground, and the panel, card, well and
    knob drawn large enough to slice. Slice borders live with the code that draws them."""
    import uuid
    GAME_KIT.mkdir(parents=True, exist_ok=True)
    pieces = {}
    for kind, (paint, _) in BUTTONS.items():
        if paint:
            pieces["button-" + kind] = recolor(art("login3-primary"), *paint)
            pieces["pill-" + kind] = recolor(art("login3-tabs-pill"), *paint)
    pieces["ground-berry"] = weave(*WEAVE)
    pieces["panel"] = panel_art(480, 360, drop=14, seed=4)
    pieces["card"] = frame_art(240, 200, False)
    pieces["card-on"] = frame_art(240, 200, True)
    pieces["well"] = well_art(240, 200, seed=20)
    pieces["field"] = field_art(1094, 156)
    pieces["knob"] = knob_art()
    template = (ART / "login3-primary.png.meta").read_text(encoding="utf-8")
    for name, im in pieces.items():
        target = GAME_KIT / (name + ".png")
        im.save(target)
        meta = target.with_suffix(".png.meta")
        if not meta.exists():  # keep a texture's guid once Unity knows it
            lines = template.splitlines()
            lines[1] = "guid: " + uuid.uuid4().hex
            meta.write_text("\n".join(lines) + "\n", encoding="utf-8")
    folder_meta = GAME_KIT.with_suffix(".meta")
    if not folder_meta.exists():
        folder_meta.write_text("fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n"
                               "  userData: \n  assetBundleName: \n  assetBundleVariant: \n" % uuid.uuid4().hex,
                               encoding="utf-8")
    return sorted(pieces)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", help="redraw the approved style frames into this folder")
    parser.add_argument("--game", action="store_true", help="write the kit textures into Resources/UI/boot-kit")
    args = parser.parse_args()
    if args.game:
        print("game kit:", ", ".join(export_game_kit()))
    if args.out:
        out = ROOT / args.out
        out.mkdir(parents=True, exist_ok=True)
        for name, draw in FRAMES.items():
            draw().im.convert("RGB").save(out / (name + ".png"))
        print("frames:", ", ".join(FRAMES))


if __name__ == "__main__":
    main()
