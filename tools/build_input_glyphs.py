"""Recolour the bought control-icon sheets into this game's warm palette.

WHY THIS EXISTS
---------------
`docs/TODO.md` section 126.9 leaves an authored-glyph gap open: the tutorial and every key
prompt in the game draw a WORD in a box (`GuidedTrainingHud.KeyCap`), and on a pad that word
is `BUTTON WEST`, which is both wrong-looking and too wide for the 26-unit chip the hero
picker gives it. A picture of the key is the answer, and the pack bought for it is
vryell's "Controllers and Keyboard" (https://vryell.itch.io/controller-keyboard-icons).

WARNING  THE PACK IS BLUE AND `CLAUDE.md` SECTION 6.4 FORBIDS BLUE IN ANY UI LAYER.
Its whole palette is the author's `rosyandblue` ramp: `14182e`, `2b2b45`, `3a3f5e`,
`404973`, `4c6885`, `686f99`, `a3a7c2`, `dfe0e8`. Every one of those has more blue in it
than red, which is section 6.4's own test, stated wide: *"no blue, no navy, no cold grey,
in any UI colour, in any layer"*. Dropping the sheets in as bought would put navy keycaps on
the one screen a new player reads first, which is exactly the fault that section records
being reported five separate times.

So the sheets are RECOLOURED on the way in rather than at draw time, and this file is where
that happens. The pack ships a palette file precisely because it expects to be recoloured.

WARNING  THE MAP IS EXACT-MATCH AND UNKNOWN COLOURS ARE REPORTED RATHER THAN GUESSED.
A luminance ramp applied blind would silently warm a colour that carries MEANING (the pad's
face-button hues) into a neutral, and nobody would know it had happened. Every source colour
in all nine sheets is listed below; anything not in the table stops the run and prints the
hex, so a pack update cannot quietly ship a colour nobody chose.

WARNING  THE FACE BUTTONS LOSE THEIR HUE ON PURPOSE AND KEEP THEIR LETTER.
Xbox A is green, B is red, X is blue and Y is yellow, and two of those four are illegal here
(`2ce8f5` cyan is section 6.4's ban, `63ab3f` green is 🧑's own PLAY pennant colour and
would read as a menu affordance on a HUD prompt). They all become AMBER, which is the one
accent this front end has. Nothing is lost that a player reads: the letter on the cap is
what names the button, and `docs/FUTURE.md` section 16.1 is blunt that a distinction carried
by hue alone is a distinction some players do not have. A shape and a letter survive a
photograph and a colourblind player; a fill does not (`CLAUDE.md` section 6.5).

WARNING  THE SHEET LAYOUT IS PRESERVED BYTE FOR BYTE, ONLY THE COLOURS MOVE.
`InputGlyphs` slices these at runtime by (column, row) at 16 px, so a script that packed the
cells tighter would be a second place to keep in step with the C#. One sheet in, one sheet
out, same size, same grid.

USAGE
-----
    python tools/build_input_glyphs.py [--src DIR] [--contact]

`--src` defaults to the extracted pack beside this repository; `--contact` also writes a
versioned contact sheet under `Logs/` for the review render section 6.1 asks for.
"""

import argparse
import os
import sys

try:
    from PIL import Image
except ImportError:  # pragma: no cover - the audit scripts all report rather than crash
    print("build_input_glyphs: Pillow is required (pip install pillow)")
    sys.exit(2)


REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# The sheets that are worth shipping. `kb_dark_*` and the alphanumeric/symbol splits are the
# same pixels as `kb_light_all` in other arrangements, and the switch/ps pads are a brand
# swap this game does not offer yet, so importing them would be four unused textures in the
# player. They stay in the pack folder.
SHEETS = {
    "kb_light_all.png": "glyphs_key_v1.png",
    "controller_xbox.png": "glyphs_pad_v1.png",
    "mouse.png": "glyphs_mouse_v1.png",
    "sticks.png": "glyphs_stick_v1.png",
}

OUT_DIR = os.path.join(REPO, "Assets", "TumbangPreso", "Resources", "UI", "input")


def rgb(h):
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16))


# The nine-step blue ramp the pack is drawn in, mapped onto the wood-and-cream ramp
# `CLAUDE.md` section 6.4 names, darkest to lightest. The two ends are the palette's own
# constants (`UiTheme.WoodDark` and `UiTheme.Cream`); the seven between them are that ramp
# interpolated in the same hue family, so a keycap reads as a carved object rather than as a
# grey one that happens to be warm.
NEUTRALS = {
    "14182e": "1d0e06",   # outline            -> UiTheme.WoodDark
    "2b2b45": "31190b",   # deepest fill       -> UiTheme.WoodDeep
    "3a3f5e": "4a2610",
    "404973": "5a2f14",   #                    -> UiTheme.WoodMid
    "4c6885": "6e3d1b",
    "686f99": "8b5227",   #                    -> UiTheme.WoodEdge
    "a3a7c2": "c08a52",
    "dfe0e8": "e9d9b8",
    "f5ffe8": "f5e6c8",   # paper              -> UiTheme.Cream
}

# Every hue in the pack is an accent on a button, a cursor or a mouse wheel. They collapse to
# amber and its shade, for the reason in this file's header.
ACCENTS = {
    "63ab3f": "ffba00",   # Xbox A green
    "e64539": "ffba00",   # Xbox B red
    "2ce8f5": "ffba00",   # Xbox X cyan
    "f0b541": "ffba00",   # Xbox Y yellow
    "ff5277": "ffba00",   # PS pink
    "ad2f45": "c98d00",   # PS dark pink, the shade under it
    "ff8933": "ffba00",   # highlight orange
    "ffee83": "f5e6c8",   # pale highlight
    "8f4d57": "8b5227",   # cursor shade
    "4fa4b8": "c98d00",   # pad shade, the ring under a lit face button
    "3b7d4f": "c98d00",   # the shade under Xbox A's green, 24 px in the whole sheet
}

MAP = {}
for src, dst in list(NEUTRALS.items()) + list(ACCENTS.items()):
    MAP[rgb(src)] = rgb(dst)


def check_no_blue():
    """`CLAUDE.md` section 6.4's own test, applied to the destinations rather than to a render.

    WARNING  IT CHECKS THE MAP, NOT THE OUTPUT, AND THAT IS THE STRONGER PLACE.
    That section is blunt about how the rule was broken for the life of a file: *"CHECK IT BY
    GREPPING, NOT BY LOOKING. `UiTheme.Ink` was navy for the entire life of this file and
    nobody saw it, because a near-black navy looks black in a code review and blue on a 1440p
    screen at six pixels of outline."* Every pixel this script emits is a value from the
    right-hand column above, so a rule enforced there cannot be broken by a later edit to the
    table, by a pack update, or by anybody eyeballing a contact sheet.

    The test is section 6.4's, stated exactly: **if a hex has more blue in it than red, it does
    not belong in a menu.**
    """
    offenders = []
    for src, dst in list(NEUTRALS.items()) + list(ACCENTS.items()):
        r, g, b = rgb(dst)
        if b > r:
            offenders.append("#" + dst + "  (from #" + src + ")")

    return offenders


def recolour(path, out_path):
    im = Image.open(path).convert("RGBA")
    px = im.load()

    unknown = {}
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            hit = MAP.get((r, g, b))
            if hit is None:
                unknown["%02x%02x%02x" % (r, g, b)] = unknown.get("%02x%02x%02x" % (r, g, b), 0) + 1
                continue
            px[x, y] = (hit[0], hit[1], hit[2], a)

    if unknown:
        return None, unknown

    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    im.save(out_path)
    return im, {}


def contact_sheet(images, out_path, scale=3):
    """One picture of every sheet that shipped, for the review CLAUDE.md section 6.1 asks for."""
    pad = 8
    width = max(im.width for im in images) * scale + pad * 2
    height = sum(im.height for im in images) * scale + pad * (len(images) + 1)

    # The asphalt the HUD card actually sits on, so the keycaps are judged against the
    # background they are drawn over rather than against white. Section 6.2b: *"over the real
    # background, never an empty scene"*.
    sheet = Image.new("RGB", (width, height), rgb("2f2118"))

    y = pad
    for im in images:
        big = im.resize((im.width * scale, im.height * scale), Image.NEAREST)
        flat = Image.new("RGB", big.size, rgb("2f2118"))
        flat.paste(big, (0, 0), big)
        sheet.paste(flat, (pad, y))
        y += big.height + pad

    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    sheet.save(out_path)
    return out_path


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--src", default=os.path.join(REPO, "scratchpad", "input-icons"))
    # ⚠️ AN OUTPUT OVERRIDE, BECAUSE WRITING INTO `Assets/` REIMPORTS. `CLAUDE.md` § 2.1b
    # forbids editing sources while a Unity run is in flight, and a texture landing under
    # `Assets/` is exactly that: the editor picks it up mid-run and recompiles the import.
    # Dry-run somewhere else first, then write for real.
    ap.add_argument("--out", default=OUT_DIR)
    ap.add_argument("--contact", action="store_true")
    args = ap.parse_args()

    blue = check_no_blue()
    if blue:
        print("build_input_glyphs: these destination colours have more blue in them than red, "
              "which CLAUDE.md section 6.4 forbids in any UI layer:")
        for row in blue:
            print("    " + row)
        return 1

    if not os.path.isdir(args.src):
        print("build_input_glyphs: source pack not found at " + args.src)
        return 2

    made = []
    failed = False

    for src_name, out_name in SHEETS.items():
        src = os.path.join(args.src, src_name)
        if not os.path.isfile(src):
            print("build_input_glyphs: MISSING " + src)
            failed = True
            continue

        im, unknown = recolour(src, os.path.join(args.out, out_name))

        if unknown:
            failed = True
            print("build_input_glyphs: " + src_name + " has colours with no mapping:")
            for hexv, count in sorted(unknown.items(), key=lambda kv: -kv[1]):
                print("    #" + hexv + "  " + str(count) + " px")
            continue

        made.append(im)
        print("build_input_glyphs: wrote " + out_name + "  " + str(im.width) + "x" + str(im.height))

    if failed:
        return 1

    if args.contact and made:
        out = contact_sheet(made, os.path.join(REPO, "Logs", "input-glyphs-v1.png"))
        print("build_input_glyphs: contact sheet " + out)

    return 0


if __name__ == "__main__":
    sys.exit(main())
