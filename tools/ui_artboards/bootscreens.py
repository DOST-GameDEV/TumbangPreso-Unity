"""Every reworked screen laid out with the boot kit, and the Photoshop job that builds them.

Each screen is a function from a capture id (Logs/ui-artboards/<id>) to a `bootkit.Board`.
The job holds one artboard per screen: the kit pieces as linked smart objects from
<out>/kit/, live Darumadrop and Grandstander text, and the old game capture hidden on top
as REFERENCE. <out>/preview/ keeps the flat drawing of each screen for checking.

    py -3 tools/ui_artboards/bootscreens.py --out ArtSource/ui/revamp-2026-10-30-boot [--only settings-audio ...]

Then build with build_psd.jsx as in docs/UI_REVAMP.md, Pipeline.
"""
import argparse
import json
import re
from pathlib import Path

from PIL import Image, ImageDraw

from bootkit import (ROOT, Board, DARUMA, GRAND, MAROON, INK, TAN, TAN_D, WHITE, TITLE_EDGE, GREEN_INK, FIELD,
                     FACE_LIP, art, button, button_art, centred, field, field_art, ground, head, header, left, panel,
                     para, right, rule, scrim, slider, tag, text, toggle, world)
import prepare

CAPTURES = ROOT / "Logs" / "ui-artboards"
LIME = (0xBF, 0xD8, 0x3A)


def capture(board_id):
    return json.loads((CAPTURES / board_id / "manifest.json").read_text(encoding="utf-8"))


# Settings. The shell is the approved audio frame: sections down the left, one panel, SAVE.

SECTIONS = ["CONTROLS", "AUDIO", "GRAPHICS", "PLAYER", "ACCESSIBILITY"]
CTRL_X, VALUE_X = 1130, 1690  # where every control and every read-out starts


def shell(b, on, heading):
    ground(b)
    header(b, "SETTINGS")
    b.group = "Sections"
    for i, s in enumerate(SECTIONS):
        button(b, "go" if i == on else "plain", 60, 200 + i * 128, 420, 104, s, 40)
    panel(b, 530, 170, 1330, 730, seed=4)
    text(b, (590, 215), heading, 64, fill=MAROON, name="Heading")
    b.group = "Footer"
    button(b, "plain", 60, 940, 300, 96, "CREDITS", 38)
    button(b, "yellow", 1430, 930, 440, 118, "SAVE CHANGES", 46)
    b.group = "Panel"


def rows(b, items, y=318, pitch=100):
    """A panel's rows. ("head", text), ("row", label, draw[, height]) or ("note", lines);
    draw(cy) adds the control on the row's centre line."""
    for n, item in enumerate(items):
        if item[0] == "head":
            head(b, 590, y + 28, item[1])
            y += 56
        elif item[0] == "note":
            para(b, 590, y + 4, item[1], 28, 38)
            y += 38 * len(item[1]) + 18
        else:
            tall = item[3] if len(item) > 3 else pitch
            cy = y + tall / 2 - 6
            left(b, 590, cy, item[1], 38, face=GRAND, weight=600, caps=False, name="Setting")
            item[2](cy)
            following = items[n + 1] if n + 1 < len(items) else None
            if following and following[0] == "row":
                rule(b, 590, 1800, int(y + tall - 2))
            y += tall


def drop(b, value):
    return lambda cy: field(b, CTRL_X, int(cy - 40), 620, 80, value, chevron=True)


def switch(b, on):
    return lambda cy: toggle(b, CTRL_X, int(cy - 40), on)


def small(b, kind, label, w=200):
    return lambda cy: button(b, kind, CTRL_X, int(cy - 80 * FACE_LIP), w, 80, label, 34)


def level(b, v, readout):
    def draw(cy):
        slider(b, CTRL_X, int(cy - 26), 520, v)
        left(b, VALUE_X, cy, readout, 40, fill=MAROON, name="Value")
    return draw


def settings(board_id):
    b = Board()
    if board_id == "settings-audio":
        shell(b, 1, "AUDIO")
        names = ["Master volume", "Sound effects volume", "Music volume", "Voice over volume", "Ambience volume"]
        rows(b, [("row", s, level(b, 0.8, "80%")) for s in names], y=306, pitch=112)
    elif board_id == "settings-controls":
        shell(b, 0, "CONTROLS")
        rows(b, [("head", "DEVICE"), ("row", "Input device", drop(b, "Keyboard & mouse")),
                 ("row", "Controller map", small(b, "yellow", "OPEN")),
                 ("head", "BINDINGS"), ("row", "Control group", drop(b, "MOVEMENT")),
                 ("row", "Move forward", lambda cy: button(b, "plain", CTRL_X, int(cy - 84 * FACE_LIP), 120, 84, "W", 44))])
    elif board_id == "settings-graphics":
        shell(b, 2, "GRAPHICS")
        rows(b, [("head", "DISPLAY"), ("row", "Graphics quality", drop(b, "Balanced")),
                 ("row", "Lighting style", lambda cy: lighting(b, cy), 220),
                 ("row", "Visual style", drop(b, "Toon (Ink Outlines)"))], pitch=96)
    elif board_id == "settings-player":
        shell(b, 3, "PLAYER")
        rows(b, [("head", "PROFILE"), ("row", "Player name", lambda cy: field(b, CTRL_X, int(cy - 40), 620, 80, "Player")),
                 ("head", "PRIVACY"), ("row", "Share play statistics", switch(b, True)),
                 ("note", ["Counts only: matches, modes, maps, picks and frame rate.",
                           "No names, chat or anything you type."])])
    elif board_id in ("settings-accessibility", "settings-keep"):
        shell(b, 4, "ACCESSIBILITY")
        rows(b, [("head", "READING"), ("row", "Larger text", switch(b, False)),
                 ("row", "HUD size", level(b, 0.0, "100%")),
                 ("note", ["Larger text enlarges settings and the match HUD. HUD size keeps",
                           "each readout together; larger text uses at least 120%."]),
                 ("row", "High-contrast HUD", switch(b, False)), ("row", "Announcer captions", switch(b, False))],
             pitch=92)
        if board_id == "settings-keep":
            keep(b)
    elif board_id == "settings-controller":
        controller(b)
    elif board_id == "settings-touch":
        touch(b)
    elif board_id == "settings-touch-layout":
        touch_layout(b)
    return b


def lighting(b, cy):
    """Three lighting looks beside their row label: pictures in tan frames, the live one in lime."""
    looks = [("Standard", "standard"), ("Nostalgic", "nostalgic"), ("Coming soon", None)]
    w, h, gap = 270, 150, 18
    top = int(cy - (h + 44) / 2) + 40
    x0 = 1800 - 3 * w - 2 * gap
    for i, (name, pic) in enumerate(looks):
        x = x0 + i * (w + gap)
        frame = Image.new("RGBA", (w, h))
        d = ImageDraw.Draw(frame)
        d.rounded_rectangle((0, 0, w - 1, h - 1), 14, fill=LIME if i == 0 else TAN)
        if pic:
            p = Image.open(ROOT / "Assets/TumbangPreso/Resources/UI/lighting-styles" / (pic + ".png")).convert("RGBA")
            p = p.resize((w - 16, h - 16), Image.LANCZOS)
            m = Image.new("L", p.size)
            ImageDraw.Draw(m).rounded_rectangle((0, 0, p.width - 1, p.height - 1), 9, fill=255)
            frame.paste(p, (8, 8), m)
        else:
            d.rounded_rectangle((8, 8, w - 9, h - 9), 9, fill=FIELD)
        b.place("look-%s%s" % (pic or "soon", "-on" if i == 0 else ""), frame, x, top - 40, "Look " + name)
        if not pic:
            centred(b, x + w / 2, top - 40 + h / 2, "?", 64, fill=TAN_D)
        centred(b, x + w / 2, top - 40 + h + 26, name.upper(), 28, fill=GREEN_INK if i == 0 else MAROON)


def keep(b):
    b.group = "Footer"
    tag(b, 1220, 956, 190, 62, "UNSAVED")
    scrim(b, (40, 8, 14, 130))
    panel(b, 560, 250, 800, 560, seed=12, group="Keep your changes")
    centred(b, 960, 340, "KEEP YOUR CHANGES?", 60, fill=MAROON, name="Heading")
    button(b, "go", 640, 420, 640, 118, "SAVE & BACK", 50)
    button(b, "red", 640, 556, 640, 104, "DISCARD CHANGES", 42)
    button(b, "plain", 640, 678, 640, 104, "KEEP EDITING", 42)


def controller(b):
    m = capture("settings-controller")
    ground(b)
    header(b, "CONTROLLER MAP")
    button(b, "yellow", 1150, 40, 330, 100, "RESET ALL", 40)
    button(b, "go", 1520, 40, 340, 100, "DONE", 46)
    b.group = "Header"
    text(b, (60, 140), "Choose a control to change its binding.", 34, face=GRAND, fill=WHITE, weight=650,
         stroke=3, stroke_fill=TITLE_EDGE, name="Hint")
    b.group = "Diagram"
    leaders = next(L for L in m["layers"] if L["name"] == "Leaders shapes")
    lines = Image.open(CAPTURES / "settings-controller" / leaders["file"]).convert("RGBA")
    tinted = Image.new("RGBA", lines.size, TAN + (255,))
    tinted.putalpha(lines.getchannel("A"))
    b.place("controller-leaders", tinted, leaders["x"], leaders["y"], "Leaders")
    pad = next(L for L in m["layers"] if L["name"] == "Diagram")
    x0, y0 = pad["tl"]
    w, h = round(pad["tr"][0] - x0), round(pad["bl"][1] - y0)
    b.place("controller-diagram", Image.open(ROOT / pad["asset"]).convert("RGBA").resize((w, h), Image.LANCZOS),
            x0, y0, "Controller")
    # Callouts: glyph and job, in the order the capture lists them (left column, then right).
    calls, glyph = [], None
    for L in m["layers"]:
        if L["kind"] == "sprite" and L["name"] == "Glyph":
            glyph = L["asset"]
        elif L["kind"] == "text" and L["name"] == "Job":
            calls.append((glyph, L["text"]))
    b.group = "Callouts"
    cw, ch = 430, 64
    for i, (g, job) in enumerate(calls):
        col, row = divmod(i, 9)
        x = 24 if col == 0 else 1920 - 24 - cw
        cy = 232 + row * 76
        y = int(cy - ch / 2)
        if i == 0:
            piece = button_art("yellow", cw, ch + 8)
            b.place("chip-on-%dx%d" % (cw, ch + 8), piece, x, y - 2, "Callout (chosen)")
            cy = y - 2 + (ch + 8) * FACE_LIP
        else:
            b.place("chip-%dx%d" % (cw, ch), field_art(cw, ch), x, y, "Callout")
        icon = Image.open(ROOT / g).convert("RGBA")
        icon.thumbnail((40, 40), Image.LANCZOS)
        b.place("glyph-" + Path(g).stem, icon, x + 26, int(cy - icon.height / 2), "Glyph")
        left(b, x + 80, cy, job, 25, face=GRAND, weight=600, caps=False, name="Job")
    b.group = "Footer"
    for i, s in enumerate(["Connect a controller to change bindings.", "Spectator controls: Settings / Controls."]):
        centred(b, 960, 958 + i * 42, s, 30, face=GRAND, fill=WHITE, weight=600, name="Hint")


def touch(b):
    """The phone settings: sections as tabs, the input device beside the title."""
    ground(b)
    header(b, "SETTINGS")
    field(b, 1420, 44, 440, 88, "Touch", chevron=True)
    b.group = "Sections"
    from bootkit import tabs
    tabs(b, 60, 176, 1800, 108, SECTIONS, 0)
    panel(b, 60, 300, 1800, 600, seed=5)
    b.group = "Panel"
    entries = [("Swipe sensitivity", lambda cy: (slider(b, 1180, int(cy - 26), 520, 0.5),
                                                 left(b, 1740, cy, "1.0x", 40, fill=MAROON, name="Value"))),
               ("Invert vertical swipe", lambda cy: toggle(b, 1180, int(cy - 40), False)),
               ("Touch controls", lambda cy: button(b, "yellow", 1180, int(cy - 80 * FACE_LIP), 260, 80, "ARRANGE", 34))]
    for i, (label, draw) in enumerate(entries):
        y = 340 + i * 120
        cy = y + 50
        left(b, 120, cy, label, 40, face=GRAND, weight=600, caps=False, name="Setting")
        draw(cy)
        rule(b, 110, 1800, y + 112)
    para(b, 120, 720, ["Drag the clear right side to look. Hold and drag THROW or a targeted skill",
                       "to aim with the same thumb."], 30, 42)
    b.group = "Footer"
    button(b, "plain", 60, 940, 300, 96, "CREDITS", 38)
    button(b, "yellow", 1430, 930, 440, 118, "SAVE CHANGES", 46)


def touch_layout(b):
    """The layout editor: the game's touch controls stay; the toolbar becomes a kit panel that
    no longer covers the emote wheel."""
    m = capture("settings-touch-layout")
    folder = CAPTURES / "settings-touch-layout"
    world(b, folder / "background.png", dim=None)
    ref = Image.open(folder / "reference.png").convert("RGBA")
    controls = Image.new("RGBA", (1920, 1080))
    start = next(i for i, L in enumerate(m["layers"]) if L["name"] == "TouchLayoutGround")
    for L in m["layers"][start:]:
        if L["name"] == "LayoutToolbar":
            break
        if L["kind"] == "raster":
            controls.alpha_composite(Image.open(folder / L["file"]).convert("RGBA"), (L["x"], L["y"]))
        elif L["kind"] == "text":
            box = tuple(int(round(v)) for v in L["ink"])
            box = (box[0] - 2, box[1] - 2, box[2] + 2, box[3] + 2)
            controls.paste(ref.crop(box), box[:2])
    b.group = "Touch controls (game art)"
    b.place("touch-controls", controls, 0, 0, "Touch controls")
    panel(b, 40, 24, 1560, 214, seed=6, group="Toolbar")
    text(b, (88, 62), "TOUCH LAYOUT", 60, fill=MAROON, name="Heading")
    text(b, (90, 150), "Drag controls. Save to keep your layout.", 30, face=GRAND, weight=500, name="Hint")
    button(b, "plain", 880, 46, 330, 80, "SIZE & OPACITY", 32)
    button(b, "plain", 1230, 46, 330, 80, "TOOLS BELOW", 32)
    button(b, "red", 880, 136, 200, 80, "CANCEL", 34)
    button(b, "yellow", 1100, 136, 200, 80, "RESET", 34)
    button(b, "go", 1320, 136, 240, 80, "SAVE LAYOUT", 32)


SCREENS = {i: settings for i in ["settings-controls", "settings-audio", "settings-graphics", "settings-player",
                                 "settings-accessibility", "settings-keep", "settings-controller", "settings-touch",
                                 "settings-touch-layout"]}


import bootroom  # noqa: E402  (the Room area)

SCREENS.update(bootroom.SCREENS)
PER_DOCUMENT = 10


def safe(text):
    return re.sub(r"[^A-Za-z0-9._-]+", "_", text).strip("_")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", required=True)
    parser.add_argument("--only", nargs="*")
    args = parser.parse_args()
    out = (ROOT / args.out).resolve()
    kit, preview = out / "kit", out / "preview"
    kit.mkdir(parents=True, exist_ok=True)
    preview.mkdir(exist_ok=True)
    order = [i for i in (CAPTURES / "index.txt").read_text().split() if i in SCREENS]
    if args.only:
        order = [i for i in order if i in args.only]
    documents = {}
    for board_id in order:
        m = capture(board_id)
        b = SCREENS[board_id](board_id)
        b.im.convert("RGB").save(preview / (board_id + ".png"))
        for key, piece in b.pieces.items():
            piece.save(kit / (safe(key) + ".png"))
        layers = []
        for L in b.layers:
            if L["kind"] == "sprite":
                x, y, w, h = L["x"], L["y"], L["w"], L["h"]
                layers.append(dict(L, file=str(kit / (safe(L["key"]) + ".png")), tl=[x, y], tr=[x + w, y],
                                   bl=[x, y + h], opacity=1, tint="FFFFFF"))
            else:
                layers.append(L)
        board = {"id": board_id, "title": m["title"], "note": "", "reference": str(CAPTURES / board_id / "reference.png"),
                 "background": getattr(b, "background", None), "layers": layers}
        area = ("Settings" if board_id.startswith("settings-") else "Room" if board_id in bootroom.SCREENS
                else prepare.document_for(board_id, m["group"]))
        documents.setdefault(area, []).append(board)
    job = {"out": str(out), "documents": []}
    for area, boards in documents.items():
        # A PSD is at most 30,000 pixels wide; ten artboards per document.
        parts = [boards[i:i + PER_DOCUMENT] for i in range(0, len(boards), PER_DOCUMENT)]
        for n, part in enumerate(parts, 1):
            name = area if len(parts) == 1 else "%s %d" % (area, n)
            job["documents"].append({"name": name, "psd": str(out / ("TUMP_UI_%s.psd" % safe(name))), "boards": part})
    (out / "job.json").write_text(json.dumps(job, indent=1), encoding="utf-8")
    print("documents:", {d["name"]: len(d["boards"]) for d in job["documents"]})


if __name__ == "__main__":
    main()
