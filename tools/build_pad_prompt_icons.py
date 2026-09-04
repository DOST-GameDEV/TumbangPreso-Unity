"""Recolour Kenney's PS4 input prompts into this game's palette and pack them into one sheet.

WHY THIS EXISTS
---------------
🧑 2026-09-04, with a sheet of PlayStation button prompts: *"change the control icons to these.
look in the internet for the icons. do not replicate it yourself"*, then *"it should be the ps4
icons"*.

The pad glyphs in `UI.InputGlyphs` were the Xbox half of vryell's pack: a player looking at the
CONTROLLER MAP saw a DualShock drawing with `Y`, `B`, `A` and `X` beside it. That is two
vocabularies for one device on one screen, which is the fault `docs/VISION.md` § 3 names about
prompts generally: the screen should teach the control the player is holding.

    Source:  Kenney, "Input Prompts" 1.5, the PlayStation Series / Default set
    Licence: CC0 1.0 (see tools/assets/kenney_ps4/Kenney_License.txt, verbatim from the pack)
    Page:    https://kenney.nl/assets/input-prompts

WARNING  THE PS4 SET IS MOSTLY THE SHARED PLAYSTATION SET, AND ONLY TWO FILES ARE PS4-SPECIFIC.
The four shapes, the four triggers, the two stick clicks, the two sticks and the d-pad are drawn
once by Kenney and are correct for every PlayStation generation. **SHARE and OPTIONS are the pair
that changes**, and they are the PS4 files here rather than the PS5 ones, because the pad this map
draws is a DualShock 4. Swapping the diagram to a DualSense means swapping those two and nothing
else.

WARNING  THE SOURCE ART IS PURE WHITE ON TRANSPARENT AND IS TINTED HERE RATHER THAN AT DRAW TIME.
`UI.InputGlyphs.For` takes an `onDark` flag and returns a sprite that is already the right colour,
because its callers set `Image.sprite` and never touch `Image.color`. Two tinted rows keeps that
contract intact; tinting at runtime would mean auditing every call site for a colour it does not
currently set. Row 0 is ink for a paper screen, row 1 is cream for the in-match HUD, which is the
same split the pack this replaces used and the same one its note explains.

WARNING  THE D-PAD'S HIGHLIGHTED ARM KEEPS A COLOUR OF ITS OWN, AND THAT IS THE ONE THING THAT
MUST NOT BE FLATTENED. Kenney draws the four directions as the same cross with ONE arm in red;
tint the whole icon one colour and all four become the same picture, which is exactly the fault
`UI.InputGlyphs` already records about sharing one d-pad cell: *"one generic cross would teach a
player that all four are the same button."* The red arm becomes Persimmon, which `CLAUDE.md` § 6.4
gives the role of "the marker: the one value or selection that matters".

USAGE
-----
    python tools/build_pad_prompt_icons.py [--out DIR] [--preview]
"""

import argparse
import os
import sys

try:
    import numpy as np
    from PIL import Image
except ImportError:  # pragma: no cover - report rather than crash, like the audits
    print("build_pad_prompt_icons: Pillow and numpy are required")
    sys.exit(2)


SOURCE_DIR = os.path.join("tools", "assets", "kenney_ps4")

CELL = 64

# ⚠️ INK FOR PAPER, CREAM FOR THE STREET. See the third warning in the header for why both are
# baked rather than tinted at draw time.
ON_LIGHT = (0x55, 0x29, 0x0F)     # UiTheme.PaperInk
ON_DARK = (0xF5, 0xE6, 0xC8)      # UiTheme.Cream

# ⚠️ THE HIGHLIGHTED D-PAD ARM. `CLAUDE.md` § 6.4 gives Persimmon exactly one job, "the marker:
# the one value or selection that matters", and on these four icons the marked arm IS the value.
MARKER = (0xFD, 0x80, 0x41)

# Kenney draws the pressed direction in this red. Matched loosely, because the PNGs are indexed
# colour and the edges carry a couple of neighbouring values.
SOURCE_MARKER = (231, 50, 70)


# ---------------------------------------------------------------------------------------
# § THE SHEET
#
# ⚠️⚠️ THE KEYS ARE EXACTLY WHAT `Hud.KeyLabel` PRODUCES, UPPERCASE, because that is what
# `UI.InputGlyphs` looks a glyph up by and its own note is explicit about it: *"the keys are
# exactly what `Hud.KeyLabel` returns, uppercase"*. `InputControlPath.ToHumanReadableString` turns
# `<Gamepad>/buttonWest` into `Button West` and `<Gamepad>/dpad/up` into `D-Pad/Up`. Inventing a
# tidier name here would produce a sheet nothing can find.
#
# ⚠️ THE ORDER IS THE COLUMN ORDER AND IT IS EMITTED, NOT ASSUMED. `glyphs_pad_v2_index.txt` is
# written beside the sheet and `InputGlyphs` reads it, so a control added or moved here cannot
# leave the C# pointing at the wrong cell. This is `tools/build_controller_diagram.py`'s anchor
# manifest, one file over, for the same reason.
# ---------------------------------------------------------------------------------------
COLUMNS = [
    ("BUTTON NORTH", "playstation_button_triangle"),
    ("BUTTON EAST", "playstation_button_circle"),
    ("BUTTON SOUTH", "playstation_button_cross"),
    ("BUTTON WEST", "playstation_button_square"),

    ("LEFT SHOULDER", "playstation_trigger_l1"),
    ("RIGHT SHOULDER", "playstation_trigger_r1"),
    ("LEFT TRIGGER", "playstation_trigger_l2"),
    ("RIGHT TRIGGER", "playstation_trigger_r2"),

    ("LEFT STICK PRESS", "playstation_button_l3"),
    ("RIGHT STICK PRESS", "playstation_button_r3"),
    ("LEFT STICK", "playstation_stick_l"),
    ("RIGHT STICK", "playstation_stick_r"),

    ("D-PAD", "playstation_dpad"),
    ("D-PAD/UP", "playstation_dpad_up"),
    ("D-PAD/DOWN", "playstation_dpad_down"),
    ("D-PAD/LEFT", "playstation_dpad_left"),
    ("D-PAD/RIGHT", "playstation_dpad_right"),

    # ⚠️ THE TWO PS4-SPECIFIC FILES. The Input System calls these controls `select` and `start`;
    # a DualShock 4 calls them SHARE and OPTIONS and draws them as two different little shapes.
    # The NAME stays the Input System's, because that is what the label lookup is keyed on.
    ("SELECT", "playstation4_button_share"),
    ("START", "playstation4_button_options"),
]


def tint(image, base):
    """One icon, recoloured: the white body to `base` and the marked arm to Persimmon."""
    pixels = np.asarray(image.convert("RGBA")).astype(int)
    red, green, blue, alpha = (pixels[:, :, i] for i in range(4))

    out = np.zeros_like(pixels)
    out[:, :, 3] = alpha

    # Everything opaque starts as the base colour.
    out[:, :, 0] = base[0]
    out[:, :, 1] = base[1]
    out[:, :, 2] = base[2]

    # ⚠️ A TOLERANCE RATHER THAN AN EXACT MATCH. The PNGs are indexed colour and the marked arm's
    # antialiased edge carries values a step or two off Kenney's red; an exact test leaves a
    # one-pixel halo of the base colour around it, which at 34 units on screen reads as a smudge.
    near = ((np.abs(red - SOURCE_MARKER[0]) < 60)
            & (np.abs(green - SOURCE_MARKER[1]) < 60)
            & (np.abs(blue - SOURCE_MARKER[2]) < 60)
            & (alpha > 8))

    out[:, :, 0] = np.where(near, MARKER[0], out[:, :, 0])
    out[:, :, 1] = np.where(near, MARKER[1], out[:, :, 1])
    out[:, :, 2] = np.where(near, MARKER[2], out[:, :, 2])

    return Image.fromarray(out.astype(np.uint8), "RGBA")


def build():
    sheet = Image.new("RGBA", (CELL * len(COLUMNS), CELL * 2), (0, 0, 0, 0))
    missing = []

    for column, (_, filename) in enumerate(COLUMNS):
        path = os.path.join(SOURCE_DIR, filename + ".png")

        if not os.path.exists(path):
            missing.append(filename)
            continue

        icon = Image.open(path).convert("RGBA")

        if icon.size != (CELL, CELL):
            print(f"build_pad_prompt_icons: {filename} is {icon.size}, expected {CELL} square.")
            sys.exit(2)

        # ⚠️ ROW 0 IS THE LIGHT-GROUND VARIANT AND ROW 1 THE DARK ONE, WHICH IS THE ORDER
        # `InputGlyphs` READS AS `onDark ? 1 : 0`. Swapping them is invisible in the sheet and
        # puts cream glyphs on the cream settings screen.
        sheet.paste(tint(icon, ON_LIGHT), (column * CELL, 0))
        sheet.paste(tint(icon, ON_DARK), (column * CELL, CELL))

    if missing:
        print("build_pad_prompt_icons: missing source icons: " + ", ".join(missing))
        sys.exit(2)

    return sheet


def write(sheet, out_dir, preview):
    os.makedirs(out_dir, exist_ok=True)

    png = os.path.join(out_dir, "glyphs_pad_v2.png")
    sheet.save(png)

    index = os.path.join(out_dir, "glyphs_pad_v2_index.txt")

    with open(index, "w", encoding="utf-8") as handle:
        handle.write("# generated by tools/build_pad_prompt_icons.py - do not hand-edit\n")
        handle.write("# source: Kenney Input Prompts 1.5, PlayStation Series, CC0\n")
        handle.write(f"# cell {CELL}px; row 0 is the light-ground tint, row 1 the dark-ground one\n")
        handle.write("# label|column\n")

        for column, (label, _) in enumerate(COLUMNS):
            handle.write(f"{label}|{column}\n")

    print(f"wrote {png} ({sheet.width}x{sheet.height}, {len(COLUMNS)} controls)")
    print(f"wrote {index}")

    if not preview:
        return

    renders = os.path.join("Logs", "renders")
    os.makedirs(renders, exist_ok=True)

    # ⚠️ EACH ROW OVER THE GROUND IT IS FOR, because a tint is only correct against the thing it
    # is drawn on. `CLAUDE.md` § 6.2b: over the real background, never an empty scene.
    strip = Image.new("RGBA", (sheet.width, CELL * 2), (0, 0, 0, 255))
    strip.paste(Image.new("RGBA", (sheet.width, CELL), (0xFC, 0xD3, 0x9F, 255)), (0, 0))
    strip.paste(Image.new("RGBA", (sheet.width, CELL), (0x31, 0x19, 0x0B, 255)), (0, CELL))
    strip.alpha_composite(sheet)

    shot = os.path.join(renders, "pad_prompts_v1.png")
    strip.convert("RGB").resize((strip.width * 2, strip.height * 2), Image.NEAREST).save(shot)
    print(f"wrote {shot}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--out", default=os.path.join(
        "Assets", "TumbangPreso", "Resources", "UI", "input"))
    parser.add_argument("--preview", action="store_true")
    args = parser.parse_args()

    write(build(), args.out, args.preview)
    return 0


if __name__ == "__main__":
    sys.exit(main())
