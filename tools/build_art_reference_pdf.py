"""Assemble the UI/splash artist's reference pack as a single PDF.

WHY THIS EXISTS
---------------
2026-09-04: a designer is starting a UI and splash-art pass, and everything they need to see is
scattered across three places that nobody outside this repository can navigate:

  * every ability effect is a PNG in `Logs/shots-abilities`, named after the EFFECT
    (`ability_seance_void_v12.png`) rather than after the hero who casts it, 1,266 files deep,
    with up to sixty versions of each;
  * every icon in the game is baked in code at runtime and does not exist as a file at all
    (`AbilityIcons`, `VerbIcons`, `UltimateMotifs` all use `HideFlags.HideAndDontSave`), so
    `Assets/TumbangPreso/Editor/ArtReferenceSheet.cs` has to export them first;
  * the HUD only exists as whole-frame captures in `Logs/shots-play` and `Logs/shots-runtime`.

So this reads `Logs/art-reference/manifest.json`, picks the newest version of each ability shot,
groups everything BY CHARACTER, and writes one PDF.

WHAT IT DELIBERATELY DOES NOT DO
--------------------------------
It does not re-render anything. Every image in the output was produced by the in-engine probe
pipeline (`CLAUDE.md` section 6.1: never an external renderer, because the toon shader, the ink
outline and Unity's linear colour conversion are the look). This is a layout pass over frames the
engine already made.

DEPENDENCIES
------------
Pillow only, which is already on this machine. `reportlab` is NOT installed and is not needed:
Pillow writes multi-page PDFs directly with `save_all=True`.

USAGE
-----
    python tools/build_art_reference_pdf.py

Requires `Logs/art-reference/manifest.json`, which comes from the Unity side:

    Unity.exe -batchmode -quit -projectPath . \
              -executeMethod TumbangPreso.EditorTools.ArtReferenceSheet.Export
"""

import json
import os
import re
import sys
from collections import defaultdict

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = os.path.join(ROOT, "Logs", "art-reference")
MANIFEST = os.path.join(ART, "manifest.json")
ABILITY_SHOTS = os.path.join(ROOT, "Logs", "shots-abilities")
PLAY_SHOTS = os.path.join(ROOT, "Logs", "shots-play")
HERO_SHOTS = os.path.join(ROOT, "Logs", "shots-hero")
TOUCH_SHOTS = os.path.join(ROOT, "Logs", "shots-touch")
SHOWCASE_SHOTS = os.path.join(ROOT, "Logs", "shots-showcase")

# A4 landscape at 150 dpi. Landscape because every frame in this game is 16:9 and a portrait page
# wastes a third of itself on margins beside one.
PAGE = (1754, 1240)

INK = (28, 15, 6)
CREAM = (245, 230, 200)
PAPER = (254, 235, 212)
AMBER = (255, 186, 0)
MUTED = (120, 92, 62)


# ----------------------------------------------------------------- which effect belongs to whom
#
# The ability captures are named after the EFFECT, and the manifest is keyed by hero and slot.
# Nothing in the repository joins the two, so the join lives here and is stated rather than
# guessed: each entry is (hero, slot) -> the prefixes that shot that ability.
#
# A prefix that matches nothing is not an error. `AbilityShowcaseProbe` photographs what it can
# reach and the set has changed over time, so a missing effect shows as a gap on the page with the
# ability still named, which is more useful to an artist than a silently shorter document.
EFFECT_PREFIXES = {
    ("dante", "skill1"): ["ability_quake_debris", "ability_blast_quake"],
    ("dante", "skill2"): ["ability_stone_carapace", "ability_carapace"],
    ("dante", "ultimate"): ["ability_titan_fissure", "ability_lava_decal", "ability_corridors"],

    ("cheska", "skill1"): ["ability_ice_sheet"],
    ("cheska", "skill2"): ["ability_barricade"],
    ("cheska", "ultimate"): ["ability_blast_frost"],

    ("sean", "skill1"): ["ability_fire_trail"],
    ("sean", "skill2"): ["ability_blast_slipper", "ability_ignition"],
    ("sean", "ultimate"): ["ability_blast_fire", "ability_supernova"],

    ("zack", "skill1"): ["ability_shock_trail", "ability_circuit_arcs"],
    ("zack", "skill2"): ["ability_magnet", "ability_circuit"],
    ("zack", "ultimate"): ["ability_blast_thunder", "ability_thunder"],

    ("nemu", "skill1"): ["ability_ghost_step", "ability_blink_arrival"],
    ("nemu", "skill2"): ["ability_kuro_unbound"],
    ("nemu", "ultimate"): ["ability_seance_void"],

    ("phaister", "skill1"): ["ability_hex_ward"],
    ("phaister", "skill2"): ["ability_blink_rift", "ability_blink_aim_reticle"],
    ("phaister", "ultimate"): ["ability_coven_eclipse"],
}

# Every ultimate also changes the sky, keyed off the hero id in
# `HeroAbilitySystem.LookFor`. An artist redesigning the ultimate presentation needs to see the
# weather beside the effect, because on screen they arrive together.
SKY_LOOKS = {
    "dante": ("ability_sky_dustveil", "DUSTVEIL"),
    "cheska": ("ability_sky_whiteout", "WHITEOUT"),
    "sean": ("ability_sky_emberfall", "EMBERFALL"),
    "zack": ("ability_sky_stormfront", "STORMFRONT"),
    "nemu": ("ability_sky_seance", "SEANCE"),
    "phaister": ("ability_sky_eclipse", "ECLIPSE"),
}

# `AbilityShowcaseProbe`'s readability gate: the worst credible pile-up, and the two role-eye
# views of it. `docs/VISION.md` section 2 rule 5 is the rule these measure.
WORST_FRAMES = [
    ("ability_worstframe", "worst credible frame"),
    ("ability_worstframe_taya", "from the taya's eye"),
    ("ability_worstframe_thrower", "from a thrower's eye"),
]


def newest_shot(prefixes, folder=ABILITY_SHOTS):
    """The highest-numbered capture matching any of these prefixes.

    Version numbers are the repository's own convention (`CLAUDE.md` section 6.1: every render
    gets a new filename because chat clients cache by name), so the newest file is the one the
    last review was conducted against.
    """
    if not os.path.isdir(folder):
        return None

    best = None
    best_key = (-1, "")

    for name in os.listdir(folder):
        if not name.endswith(".png"):
            continue
        for prefix in prefixes:
            if not name.startswith(prefix):
                continue
            # Prefer the plain shot over the `_eye` variant: the eye-level one is a framing
            # check, and an artist wants the effect filling the frame.
            if "_eye" in name:
                continue
            m = re.search(r"_v(\d+)\.png$", name)
            version = int(m.group(1)) if m else 0
            if (version, name) > best_key:
                best_key = (version, name)
                best = os.path.join(folder, name)

    return best


def load_font(size, bold=False):
    """A real face if one is on this machine, else Pillow's bitmap default."""
    candidates = [
        r"C:\Windows\Fonts\segoeuib.ttf" if bold else r"C:\Windows\Fonts\segoeui.ttf",
        r"C:\Windows\Fonts\arialbd.ttf" if bold else r"C:\Windows\Fonts\arial.ttf",
    ]
    for path in candidates:
        if os.path.exists(path):
            try:
                return ImageFont.truetype(path, size)
            except OSError:
                pass
    return ImageFont.load_default()


F_TITLE = load_font(64, bold=True)
F_HEAD = load_font(40, bold=True)
F_SUB = load_font(26)
F_BODY = load_font(22)
F_SMALL = load_font(18)


def page():
    img = Image.new("RGB", PAGE, PAPER)
    return img, ImageDraw.Draw(img)


def header(d, title, subtitle=None):
    d.rectangle([0, 0, PAGE[0], 118], fill=INK)
    d.text((56, 30), title, font=F_HEAD, fill=CREAM)
    if subtitle:
        d.text((56, 78), subtitle, font=F_SMALL, fill=AMBER)


def paste_fit(img, path, box, label=None, d=None, tint_bg=None, tint=None):
    """Fit an image inside `box` = (x, y, w, h), centred, preserving aspect.

    `tint` recolours a white-on-transparent glyph.

    WHY THE TINT IS NOT OPTIONAL FOR THE ICONS
    ------------------------------------------
    Every icon in this game is baked WHITE and coloured at the use site through `Image.color`,
    which is right for the engine and invisible on paper: the first draft of this pack pasted
    them straight onto the cream page and produced a document of blank rectangles. The page is
    the use site here, so it has to do what the game does.
    """
    x, y, w, h = box

    if tint_bg is not None:
        ImageDraw.Draw(img).rectangle([x, y, x + w, y + h], fill=tint_bg)

    if path and os.path.exists(path):
        try:
            src = Image.open(path).convert("RGBA")
        except OSError:
            src = None
    else:
        src = None

    if src is not None and tint is not None:
        # Keep the alpha, replace the colour. A multiply would darken the feathered edge twice.
        flat = Image.new("RGBA", src.size, tuple(tint) + (255,))
        flat.putalpha(src.getchannel("A"))
        src = flat

    if src is None:
        if d is not None:
            d.rectangle([x, y, x + w, y + h], outline=MUTED, width=2)
            d.text((x + 14, y + h // 2 - 12), "not captured", font=F_SMALL, fill=MUTED)
    else:
        scale = min(w / src.width, h / src.height)
        nw, nh = max(1, int(src.width * scale)), max(1, int(src.height * scale))
        src = src.resize((nw, nh), Image.LANCZOS)
        img.paste(src, (x + (w - nw) // 2, y + (h - nh) // 2), src)

    if label and d is not None:
        d.text((x, y + h + 8), label, font=F_SMALL, fill=INK)


def hex_to_rgb(value):
    value = value.lstrip("#")
    return tuple(int(value[i:i + 2], 16) for i in (0, 2, 4))


def cover_page(data):
    img, d = page()
    d.rectangle([0, 0, PAGE[0], PAGE[1]], fill=INK)

    d.text((90, 300), "TUMBANG PRESO", font=F_TITLE, fill=CREAM)
    d.text((90, 390), "UI and splash art reference pack", font=F_HEAD, fill=AMBER)

    lines = [
        "Every ability effect, grouped by character.",
        "Every icon the game draws, exported from the code that bakes them.",
        "The in-game HUD and the touch layer, photographed in play.",
        "",
        f"Generated {data.get('generated', '')}",
        "",
        "Effects and HUD frames are in-engine captures: the toon shader, the ink outline",
        "and the linear colour conversion are the look, so nothing here was rendered outside",
        "the game. Icons are exported white on transparent and tinted at the use site.",
    ]

    y = 520
    for line in lines:
        d.text((90, y), line, font=F_BODY, fill=CREAM if line else INK)
        y += 40

    # The palette strip along the bottom.
    palette = data.get("palette", {})
    x = 90
    for name, value in palette.items():
        try:
            rgb = hex_to_rgb(value)
        except (ValueError, AttributeError):
            continue
        d.rectangle([x, 1040, x + 150, 1110], fill=rgb)
        d.text((x, 1118), name, font=F_SMALL, fill=CREAM)
        d.text((x, 1142), value, font=F_SMALL, fill=MUTED)
        x += 170

    return img


def hero_pages(data):
    """One spread per hero: the three abilities, their icons, and their effect captures."""
    out = []

    for hero in data.get("heroes", []):
        img, d = page()

        accent = hex_to_rgb(hero.get("accent", "#ffffff"))
        header(d, hero.get("name", "?").upper(),
               f"{hero.get('id')}  ·  accent {hero.get('accent')}  ·  "
               f"bright {hero.get('accentBright')}")

        # The accent bar, so the page is unmistakably this hero's.
        d.rectangle([0, 118, PAGE[0], 132], fill=accent)

        # The ultimate's motif strip, which is what the introduction card draws.
        # ⚠️ THE ACCENT IS DARKENED FOR PAPER. Several hero accents are chosen to glow on a
        # dark street (`UiTheme.BrightForHero`'s note), and the same value on cream is a pale
        # smear. Two thirds toward ink keeps the hue and buys the contrast.
        on_paper = tuple(int(c * 0.62) for c in accent)

        motif = os.path.join(ART, hero.get("motif", ""))
        paste_fit(img, motif, (56, 156, 520, 64), "ultimate card motif", d, tint=on_paper)

        y = 268
        for ability in hero.get("abilities", []):
            icon = os.path.join(ART, ability.get("icon", ""))
            paste_fit(img, icon, (56, y, 150, 150), None, d, tint=on_paper)

            d.text((230, y + 6), ability.get("name", "?"), font=F_HEAD, fill=INK)
            d.text((230, y + 56),
                   f"{ability.get('slot', '')}  ·  {ability.get('job', '')}  ·  "
                   f"glyph {ability.get('glyph', '')}",
                   font=F_SUB, fill=MUTED)
            d.text((230, y + 92),
                   f"cooldown {ability.get('cooldown', 0)}s  ·  "
                   f"telegraph radius {ability.get('telegraphRadius', 0)}m",
                   font=F_BODY, fill=MUTED)

            shot = newest_shot(EFFECT_PREFIXES.get((hero.get("id"), ability.get("slot")), []))
            paste_fit(img, shot, (900, y - 10, 780, 276),
                      os.path.basename(shot) if shot else None, d)

            y += 330

        out.append(img)

    return out


def icon_sheet(data):
    """Every touch control and every generic glyph, on two pages."""
    out = []

    img, d = page()
    header(d, "TOUCH CONTROLS",
           "the thumb layer, one icon per verb. Words were removed on 2026-09-04: a phone "
           "has no keys.")

    cols, x0, y0, cw, ch = 5, 70, 190, 320, 300
    for i, control in enumerate(data.get("touchControls", [])):
        cx = x0 + (i % cols) * cw
        cy = y0 + (i // cols) * ch

        paste_fit(img, os.path.join(ART, control.get("icon", "")), (cx, cy, 180, 180), None, d,
                  tint=INK)
        d.text((cx, cy + 194), control.get("label", ""), font=F_SUB, fill=INK)
        d.text((cx, cy + 226), control.get("describes", ""), font=F_SMALL, fill=MUTED)
        d.text((cx, cy + 250),
               f"{control.get('zone', '')} · {control.get('sizeUnits', '')}u",
               font=F_SMALL, fill=MUTED)

    out.append(img)

    img, d = page()
    header(d, "THE GLYPH VOCABULARY",
           "what a power does to the WORLD, not what element it is made of. "
           "Two heroes share a glyph when they share a job.")

    for i, glyph in enumerate(data.get("glyphVocabulary", [])):
        cx = x0 + (i % cols) * cw
        cy = y0 + (i // cols) * ch

        paste_fit(img, os.path.join(ART, glyph.get("icon", "")), (cx, cy, 180, 180), None, d,
                  tint=INK)
        d.text((cx, cy + 194), glyph.get("job", ""), font=F_SUB, fill=INK)
        d.text((cx, cy + 226), glyph.get("glyph", ""), font=F_SMALL, fill=MUTED)

    out.append(img)
    return out


def hud_pages():
    """The HUD and the touch layer as they actually appear on screen."""
    out = []

    groups = [
        ("IN-GAME HUD", PLAY_SHOTS,
         "the match HUD over the live street, which is the background every value in it was "
         "tuned against."),
        ("HERO STRIKE UI", HERO_SHOTS,
         "the ability deck, the hold-to-inspect tray and the character board."),
        ("TOUCH LAYER", TOUCH_SHOTS,
         "the thumb controls in place, at phone aspect ratios."),
        ("SPECTATOR AND REPLAY", SHOWCASE_SHOTS,
         "the autopilot's shot vocabulary, the ultimate introduction cards and the replay "
         "overlay."),
    ]

    for title, folder, note in groups:
        if not os.path.isdir(folder):
            continue

        names = sorted(n for n in os.listdir(folder) if n.endswith(".png"))
        if not names:
            continue

        # Keep only the newest version of each shot.
        #
        # WHY: this repository versions every render rather than overwriting it (`CLAUDE.md`
        # section 6.1: chat clients cache by filename, so overwriting leaves the previous image on
        # screen and the whole review is conducted against a file that no longer exists). The
        # folders therefore hold `touch-Classic-20-9-phone-v1.png` through `-v5.png` side by side,
        # and a plain alphabetical sample picks v1: **the reference pack would have shown a
        # designer the screen as it looked before the pass that this pack documents.**
        newest = {}
        for name in names:
            m = re.match(r"^(.*?)_?v(\d+)\.png$", name)
            if m:
                stem, version = m.group(1), int(m.group(2))
            else:
                stem, version = name[:-4], -1

            if stem not in newest or version > newest[stem][0]:
                newest[stem] = (version, name)

        names = sorted(entry[1] for entry in newest.values())

        # Spread the sample across the folder rather than taking the first six, which on a
        # dense capture would be six frames of the same second.
        step = max(1, len(names) // 6)
        picks = names[::step][:6]

        img, d = page()
        header(d, title, note)

        for i, name in enumerate(picks):
            cx = 60 + (i % 3) * 560
            cy = 200 + (i // 3) * 500
            paste_fit(img, os.path.join(folder, name), (cx, cy, 520, 300), name, d)

        out.append(img)

    return out


def sky_page(data):
    """The weather each ultimate brings with it."""
    img, d = page()
    header(d, "ULTIMATE WEATHER",
           "every ultimate changes the sky. One call in HeroAbilitySystem, keyed by hero, so a "
           "seventh hero gets weather by existing.")

    heroes = [h.get("id") for h in data.get("heroes", [])]

    for i, hero_id in enumerate(heroes):
        entry = SKY_LOOKS.get(hero_id)
        if entry is None:
            continue

        prefix, look = entry
        shot = newest_shot([prefix])

        cx = 60 + (i % 3) * 560
        cy = 200 + (i // 3) * 500

        paste_fit(img, shot, (cx, cy, 520, 300),
                  f"{hero_id.upper()}  ·  {look}", d)

    return [img]


def readability_page():
    """The readability budget, as the probe photographs it."""
    shots = [(newest_shot([prefix]), label) for prefix, label in WORST_FRAMES]

    if not any(path for path, _ in shots):
        return []

    img, d = page()
    header(d, "THE READABILITY BUDGET",
           "a screenshot taken mid-fight must still show the lata, the chalk and every player. "
           "A run that blows more than 12 per cent of a frame to white fails.")

    for i, (path, label) in enumerate(shots):
        cx = 60 + i * 560
        paste_fit(img, path, (cx, 240, 520, 300), label, d)

    notes = [
        "The arena is 14 m by 14 m and holds four players, one lata, four tsinelas and up to",
        "twelve live abilities. A skill's floor footprint should be about 1.8 to 2.5 m of radius.",
        "An ultimate may be big; one at a time. Spend the budget on DETAIL, not on AREA:",
        "a flat coloured plane at 40 per cent of the arena reads as a puddle.",
        "",
        "Measured: the empty street reads 3.0 per cent white, the ability corridors 3.0,",
        "the deliberate worst-frame pile-up 4.1, and the loudest legitimate effect 8.3.",
        "Zack's Thunderstrike once read 62.8 per cent, with the road markings themselves gone.",
    ]

    y = 640
    for line in notes:
        d.text((60, y), line, font=F_BODY, fill=INK if line else PAPER)
        y += 34

    return [img]


def main():
    if not os.path.exists(MANIFEST):
        print(f"missing {MANIFEST}", file=sys.stderr)
        print("run the Unity export first:", file=sys.stderr)
        print("  Unity.exe -batchmode -quit -projectPath . "
              "-executeMethod TumbangPreso.EditorTools.ArtReferenceSheet.Export",
              file=sys.stderr)
        return 1

    with open(MANIFEST, encoding="utf-8") as handle:
        data = json.load(handle)

    pages = [cover_page(data)]
    pages += hero_pages(data)
    pages += sky_page(data)
    pages += icon_sheet(data)
    pages += hud_pages()
    pages += readability_page()

    out = os.path.join(ROOT, "Logs", "TumbangPreso_Art_Reference_v1.pdf")
    pages[0].save(out, save_all=True, append_images=pages[1:], resolution=150.0)

    print(f"wrote {out} ({len(pages)} pages)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
