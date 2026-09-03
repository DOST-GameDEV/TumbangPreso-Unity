"""Recolour the CC0 ability-effect sheets into this game's six hero families.

WHY THIS EXISTS
---------------
`docs/TODO.md` section 131 replaces the primitive-looking ability transients with sourced
art, and `docs/Asset_Sourcing.md` is the licensed list it draws from. Every sheet this
script reads is CC0, so it may be redistributed; what it may NOT do is arrive with an art
direction of its own. `Asset_Sourcing.md` rule 2: *"The game owns the final look. A
downloaded prefab is an ingredient, not an art direction."*

WARNING  THE SOURCE PACKS ARE THE WRONG COLOUR FOR EVERY HERO IN THIS GAME, AND THAT IS
NOT A MATTER OF TASTE. `UiTheme` names the six families and the sources disagree with all
six:

    family      the game's colour                the source sheet arrives as
    Dante       HeroMagmaCore  ff9a2e            slate grey-blue rock
    Cheska      HeroIce        5fe8d0  teal      cornflower and cobalt BLUE
    Sean        HeroFire       ff3355  red       orange and yellow
    Zack        HeroElectric   e8f53a  YELLOW    cobalt blue and white
    Nemu        HeroSpirit     b44dff  purple    purple with a CYAN ring
    Phaister    HeroWitch      e828c5  magenta   black line art on white

Dropping any of them in as delivered would put two Zacks in the game: a yellow one in the
HUD, the ability deck, the character select and the popup text, and a blue one on the
floor. The whole point of a colour that names a hero is that it names them everywhere.

WARNING  THE MAP IS EXACT-MATCH AND AN UNRECORDED PALETTE STOPS THE RUN.
This is `tools/build_input_glyphs.py`'s rule and it is here for the same reason: a ramp
applied blind would silently restyle a pack update and nobody would know it had happened.
Every source sheet's palette is recorded below in luminance order. If the pack ships a
different one the run stops and prints both, rather than guessing.

WARNING  THE RAMP IS ORDERED BY LUMINANCE AND NOT BY HUE, WHICH IS WHY IT SURVIVES A
RECOLOUR AT ALL. Pixel art reads by VALUE: the dark ink edge, the mid body and the one hot
core are the drawing, and the hue is a label on top of it. Mapping the nth darkest source
colour to the nth stop of a family ramp keeps every silhouette, every rim and every
interior shape exactly as the artist drew them, and changes only what the thing is made of.
Sorting by hue, or nearest-colour matching, loses the ordering and the art collapses.

WARNING  NO RAMP TOP MAY REACH THE BLOWOUT LEVEL. `AbilityShowcaseProbe.BlownLevel` is
Rec. 601 luminance 245 and `docs/VISION.md` section 2 rule 5 is the argument: a frame that
is white is a frame with no lata, no chalk and no players in it. Four of the source
palettes top out at 249 to 253, which is white. Every ramp here is capped, the cap is
asserted at the end of a run, and that assertion runs every time the sheets are built.

WARNING  THE CELL GRID IS PRESERVED BYTE FOR BYTE. `VfxSheets` slices these at runtime by
(column, row) at the cell size recorded in `Assets/TumbangPreso/Runtime/Visual/VfxSheets.cs`,
so a script that repacked the cells would be a second place to keep in step with the C#.
One sheet in, one sheet out, same size, same grid, same frame order.

USAGE
-----
    python tools/build_vfx_sheets.py [--src DIR] [--contact]

`--src` defaults to `scratchpad/asset-src/`, which is where the download cache lives and is
gitignored; `--contact` also writes a versioned contact sheet under `Logs/` for the review
render `CLAUDE.md` section 6.1 asks for.
"""

import argparse
import json
import os
import sys

try:
    from PIL import Image
except ImportError:  # pragma: no cover - the audit scripts all report rather than crash
    print("build_vfx_sheets: Pillow is required (pip install pillow)")
    sys.exit(2)

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC_DIR = os.path.join(REPO, "scratchpad", "asset-src")
OUT_DIR = os.path.join(REPO, "Assets", "TumbangPreso", "Resources", "Vfx")

# The blowout level `AbilityShowcaseProbe` fails a frame on, out of 255. Nothing this script
# writes may reach it. See the warning in the module docstring.
BLOWN_LEVEL = 245


def lum(rgb):
    """Rec. 601 luminance, the same arithmetic `AbilityShowcaseProbe` measures a frame with."""
    r, g, b = rgb
    return 0.299 * r + 0.587 * g + 0.114 * b


def hex_to_rgb(h):
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16))


def rgb_to_hex(c):
    return "%02x%02x%02x" % (c[0], c[1], c[2])


# ---------------------------------------------------------------------------
# THE SIX FAMILY RAMPS, plus one neutral.
#
# WARNING  EVERY MID AND TOP STOP IS EITHER A `UiTheme` CONSTANT OR DERIVED FROM ONE, AND
# THE COMMENT SAYS WHICH. A ramp invented here would be a seventh place the game decides
# what colour Cheska is, and the copy that drifts is always the one nobody greps for.
#
# WARNING  THE DARKEST STOP IS THE ONE THAT KEEPS BEING WRONG, AND THE REASON IS THE SAME EVERY
# TIME. Every source sheet spends its LARGEST single share of pixels on its own dark ink: 36 per
# cent for `frost-nova`, 33 for `electric-impact`, 37 for `void-implosion`. That is a shadow
# INSIDE a bright drawing in the source, and a ramp that takes it to a near-black turns it into a
# hole in whatever the effect is standing on. Three of the six first renders had it
# (`ability_ice_sheet_eye_v52.png`, `ability_blast_thunder_eye_v53.png`,
# `ability_kuro_unbound_nopet_eye_v53.png`). The rule that came out of it: **the darkest stop is
# a deep tone of the family, not black**, and only `earth` and `ash` may go properly dark because
# only those two are drawn against nothing but asphalt.
#
# WARNING  THE DARKEST STOP OF EVERY RAMP IS A WARM NEAR-BLACK RATHER THAN THE SOURCE'S
# NAVY. Four of the source palettes open on a blue-black (`1c224c`, `1e3070`, `181933`,
# `232258`), and at around 30 per cent of the pixels that is the single largest area in the
# sheet. `CLAUDE.md` section 6.4's test, stated wide, is that a hex with more blue in it
# than red does not belong; these are drawn beside `UiTheme.Ink` `1c0f06` and asphalt.
# ---------------------------------------------------------------------------
RAMPS = {
    # Dante. `UiTheme.HeroMagmaCore` ff9a2e is the mid-bright stop; the one above it is that
    # colour lifted toward the cream the rest of the front end uses.
    "earth": ["140a04", "3a2114", "70452a", "c06a22", "ff9a2e", "ffd489"],

    # Cheska. `UiTheme.HeroIce` 5fe8d0 and `HeroIceBright` b8fff2 are the top two, and the
    # three under them are that teal walked down in value rather than a separate blue.
    #
    # WARNING  HER DARKEST STOP IS NOT AS DARK AS THE OTHER FIVE, AND THAT IS MEASURED RATHER
    # THAN A PREFERENCE. Every one of her effects is placed on a PALE surface: the permafrost
    # sheet is near-white ice and the barricade is a lit wall, where Dante's rupture and Zack's
    # impact both sit on grey asphalt. `frost-nova`'s largest single colour is its shadow at
    # 36 per cent of the sheet, so a near-black there draws a hole in her own ice.
    # `Logs/shots-abilities/ability_ice_sheet_eye_v52.png` is that hole.
    "frost": ["17403a", "24625a", "3a9d8e", "5fe8d0", "b8fff2"],

    # Sean. `UiTheme.HeroFire` ff3355 and `HeroFireBright` ff8fa3, with one warm cream over
    # them so a flame still has a core that is lighter than its body.
    "fire": ["1e0a0c", "5c1219", "b02034", "ff3355", "ff8fa3", "ffd0b0"],

    # Zack. `UiTheme.HeroElectric` e8f53a and `HeroElectricBright` f6ffa0.
    #
    # WARNING  THE TWO MID STOPS ARE ORANGE-BROWN AND NOT OLIVE, AND THE FIRST VERSION WAS
    # OLIVE. `453c0e` and `9aa322` are yellow walked straight down in value, which is a GREEN,
    # and `electric-impact` spends half its pixels in the two mid bands: the recoloured strike
    # came out as a swamp-coloured cloud with a yellow rim rather than as lightning.
    # `scratchpad/recolour_compare_v2.png` row four is the pair. Walking the yellow down through
    # AMBER instead keeps every mid tone on the warm side of the wheel, which is also the only
    # direction `CLAUDE.md` section 6.4 leaves open.
    "spark": ["31220a", "7d5a10", "c9a616", "e8f53a", "f6ffa0"],

    # Nemu. `UiTheme.HeroSpirit` b44dff and `HeroSpiritBright` dfaaff.
    #
    # WARNING  HER DARKEST STOP IS LIFTED FOR THE SAME REASON CHESKA'S IS, AND THE SURFACE IT IS
    # DRAWN AGAINST IS THE OPPOSITE ONE. Nemu's ultimate is a large DARK PURPLE maw, so an
    # implosion whose own ink is near-black lands as a lump inside it rather than as an intake
    # over it. `Logs/shots-abilities/ability_kuro_unbound_nopet_eye_v53.png` is that lump.
    "spirit": ["2a1440", "45206b", "7c3ac0", "b44dff", "dfaaff"],

    # Smoke, dust and anything that belongs to the STREET rather than to a hero. Warm greys
    # only: `CLAUDE.md` section 6.4 bans a cold grey as firmly as it bans a blue, and the
    # source smoke sheet is `1b1f2b` through `d7e0e4`, which is five cold greys in a row.
    "ash": ["140d08", "33241a", "6b5240", "a8886a", "d8bd9a"],
}


# ---------------------------------------------------------------------------
# THE SOURCE PALETTE CONTRACT.
#
# Each list is that sheet's complete opaque palette, in ascending luminance, as shipped by
# the pack this script was written against. An exact set mismatch stops the run.
#
# WARNING  THE ORDER IS THE MAPPING. The nth entry becomes the ramp evaluated at
# n / (count - 1), so re-ordering a list silently redraws the effect.
# ---------------------------------------------------------------------------
PVFX_PALETTES = {
    "earth-rupture": ["1d1f28", "484445", "754030", "4e5869", "aa6b2d", "cd9d4e", "f5d176"],
    "frost-nova": ["1c224c", "344899", "3d89e0", "68d6ff", "ffec98", "c6f6ff", "faffff"],
    "ember-jet": ["251c27", "5b4548", "8e362d", "ce3d28", "f67727", "ffc844", "fff9c2"],
    "electric-impact": ["2b3046", "1e3070", "3e4862", "2667e0", "576581", "ff6721",
                        "45cdff", "ffde59", "baf7ff", "fffff5"],
    "void-implosion": ["181933", "393370", "5d359d", "a937d5", "dd60ff", "43d8de", "f8e6ff"],
    "warm-explosion": ["191824", "7e2b36", "5b434f", "c23a34", "946a60", "f47e30",
                       "cda482", "ffd356", "fffcdb"],
    "spectral-bloom": ["1b1934", "4c3577", "1f677a", "2ba69d", "dc68b9", "7ee7b4",
                       "e8ffdb", "fff8ee"],
    "magical-projectile": ["232258", "4642bc", "9163f5", "e056f6", "5ae0ff", "ffcc5b",
                           "caf9ff", "fffdee"],
    "smoke-puff": ["1b1f2b", "333a49", "5b6678", "97a4b3", "d7e0e4"],
    "landing-dust": ["393231", "554640", "9e7758", "cfa975", "eecd8f"],
    "solar-shrapnel": ["1f1930", "7a263d", "cd4833", "d44f8f", "ef8434", "ffcd4b",
                       "fff8c4", "fffff4"],
}


# ---------------------------------------------------------------------------
# WHAT SHIPS.
#
# WARNING  THE NAME ON THE LEFT IS THE ONE `VfxSheets.cs` LOADS AND IT CARRIES A VERSION.
# `CLAUDE.md` section 6.1: a render overwritten in place leaves the previous image on screen
# in every chat client, so the review is conducted against a picture that is not on disk.
# The same rule is worth having on a shipped sheet, because a sheet is reviewed exactly the
# same way.
#
# WARNING  ONE SOURCE MAY FEED TWO ABILITIES AND THAT IS DELIBERATE, but a source never
# feeds two HEROES. `warm-explosion` is Sean's ultimate and `solar-shrapnel` is the pieces it
# throws; both are on his ramp and neither is lent to Dante, whose ground work is the other
# warm kit and the one most at risk of reading as the same hero.
# ---------------------------------------------------------------------------
PVFX_SHEETS = [
    # (output stem, pvfx effect id, ramp)
    ("vfx_rupture_v1", "earth-rupture", "earth"),
    ("vfx_frostnova_v1", "frost-nova", "frost"),
    ("vfx_emberjet_v1", "ember-jet", "fire"),
    ("vfx_spark_v1", "electric-impact", "spark"),
    ("vfx_implosion_v1", "void-implosion", "spirit"),
    ("vfx_burst_v1", "warm-explosion", "fire"),
    ("vfx_bloom_v1", "spectral-bloom", "spirit"),
    ("vfx_bolthead_v1", "magical-projectile", "fire"),
    ("vfx_smoke_v1", "smoke-puff", "ash"),
    ("vfx_dust_v1", "landing-dust", "ash"),
    ("vfx_shrapnel_v1", "solar-shrapnel", "fire"),
]


def sample_ramp(stops, t):
    """The ramp at 0..1, interpolated in sRGB between adjacent stops.

    WARNING  IT INTERPOLATES RATHER THAN SNAPPING, AND THE OUTPUT IS STILL FLAT. A sheet with
    ten source colours snapped onto a five-stop ramp comes back with five, which merges the
    rim into the body and loses the drawing. Interpolating gives back exactly as many colours
    as went in, so every sheet stays the same flat, hard-edged pixel art it arrived as.
    """
    if t <= 0.0:
        return hex_to_rgb(stops[0])
    if t >= 1.0:
        return hex_to_rgb(stops[-1])

    span = (len(stops) - 1) * t
    i = int(span)
    f = span - i
    a = hex_to_rgb(stops[i])
    b = hex_to_rgb(stops[i + 1])
    return tuple(int(round(a[k] + (b[k] - a[k]) * f)) for k in range(3))


def build_map(source_palette, ramp_name):
    """The exact colour table for one sheet, source RGB to output RGB.

    WARNING  THE POSITION ON THE RAMP IS THE SOURCE COLOUR'S OWN LUMINANCE, NOT ITS RANK, AND
    THE FIRST VERSION OF THIS USED RANK AND VISIBLY BROKE ONE SHEET.
    `Logs/shots-abilities/ability_ice_sheet_eye_v52.png` is the receipt: Cheska's formation
    nova came out as a near-black dome sitting in a hole in her own ice.

    The cause is that a palette is not evenly spread. `frost-nova` is
    36, 75, 124, 185, 232, 232, 253, which is THREE colours at the bright end out of seven;
    rank spreads them 0, 1/6, 2/6 ... so the effect's main body at luminance 185 landed at the
    middle of the ramp and came back at about 86. The drawing keeps its shape and loses its
    VALUE, which is the one property the module docstring says pixel art reads by.

    WARNING  EQUAL LUMINANCES ARE NUDGED APART RATHER THAN MERGED. `frost-nova` carries `ffec98`
    and `c6f6ff` at exactly 232, and mapping both to one output colour would delete a distinction
    the artist drew. The nudge is a fiftieth of the ramp: enough to stay two colours, small enough
    that it cannot reorder anything.
    """
    stops = RAMPS[ramp_name]
    lums = [lum(hex_to_rgb(h)) for h in source_palette]
    lo, hi = min(lums), max(lums)
    span = max(1.0, hi - lo)

    out = {}
    last = -1.0
    for h, l in zip(source_palette, lums):
        t = (l - lo) / span
        if t <= last:
            t = min(1.0, last + 0.02)
        last = t
        out[hex_to_rgb(h)] = sample_ramp(stops, t)
    return out


def palette_of(image):
    seen = set()
    for px in image.convert("RGBA").getdata():
        if px[3]:
            seen.add(px[:3])
    return seen


def recolour(image, table, where):
    """Apply an exact-match table. An unrecorded colour is a hard failure."""
    image = image.convert("RGBA")
    out = Image.new("RGBA", image.size)
    dst = []
    unknown = set()
    for px in image.getdata():
        if px[3] == 0:
            dst.append((0, 0, 0, 0))
            continue
        hit = table.get(px[:3])
        if hit is None:
            unknown.add(px[:3])
            dst.append(px)
            continue
        dst.append((hit[0], hit[1], hit[2], px[3]))

    if unknown:
        print("build_vfx_sheets: %s has colours this script has never seen:" % where)
        for c in sorted(unknown, key=lum):
            print("    #%s" % rgb_to_hex(c))
        print("    Add them to the palette contract rather than letting a ramp guess.")
        sys.exit(1)

    out.putdata(dst)
    return out


def build_pvfx(src_dir, out_dir, report):
    pack_root = os.path.join(src_dir, "pvfx")
    pack_path = os.path.join(pack_root, "pack.json")
    if not os.path.isfile(pack_path):
        print("build_vfx_sheets: no PVFX Foundry pack at %s" % pack_root)
        print("    See docs/Asset_Sourcing.md section 2 for the source and the licence.")
        sys.exit(2)

    pack = json.load(open(pack_path, encoding="utf-8"))
    meta = {e["id"]: e for e in pack["effects"]}

    for stem, effect, ramp in PVFX_SHEETS:
        entry = meta.get(effect)
        if entry is None:
            print("build_vfx_sheets: the pack no longer carries '%s'" % effect)
            sys.exit(1)

        sheet_path = os.path.join(pack_root, "effects", effect, "grid", "sprite-sheet.png")
        image = Image.open(sheet_path)

        recorded = PVFX_PALETTES[effect]
        found = palette_of(image)
        expected = set(hex_to_rgb(h) for h in recorded)
        if found != expected:
            print("build_vfx_sheets: '%s' does not have the palette this script records." % effect)
            print("    recorded : %s" % " ".join(sorted(rgb_to_hex(c) for c in expected)))
            print("    on disk  : %s" % " ".join(sorted(rgb_to_hex(c) for c in found)))
            sys.exit(1)

        out = recolour(image, build_map(recorded, ramp), effect)

        # WARNING  THE CELL SIZE IS READ FROM THE MANIFEST, NOT ASSUMED. It is 96 today and
        # the C# table records 96; a pack that changed it would otherwise slice every frame
        # in half with nothing saying so.
        manifest = json.load(open(os.path.join(pack_root, "effects", effect, "grid",
                                               "manifest.json"), encoding="utf-8"))
        cell = cell_of(manifest)
        cols = out.size[0] // cell

        out.save(os.path.join(out_dir, stem + ".png"))
        report.append({
            "stem": stem,
            "source": "PVFX Foundry / " + effect,
            "ramp": ramp,
            "cell": cell,
            "cols": cols,
            "frames": entry["frames"],
            "fps": entry["fps"],
            "loop": entry["loop"] == "loop",
            # The pack's pivot is pixels DOWN from the top of the cell; `VfxSheets.Pivot` is a
            # fraction UP from the bottom, because that is where the ground line is and a quad
            # is anchored at the ground.
            "pivot": round((cell - entry["pivot"][1]) / float(cell), 3),
            "size": out.size,
            "peak": int(max(lum(c) for c in palette_of(out))),
        })


def cell_of(manifest):
    """The square cell size, dug out of whichever field the exporter used for it."""
    for key in ("source_size", "canvas", "canvas_size", "cell", "frame_size"):
        v = manifest.get(key)
        if isinstance(v, list) and len(v) == 2:
            return int(v[0])
        if isinstance(v, dict) and "width" in v:
            return int(v["width"])
        if isinstance(v, int):
            return v
    print("build_vfx_sheets: no canvas size in a PVFX manifest. Fields: %s"
          % ", ".join(sorted(manifest.keys())))
    sys.exit(1)


# ---------------------------------------------------------------------------
# PHAISTER IS DELIBERATELY ABSENT FROM THIS SCRIPT, AND THAT IS A DECISION RATHER THAN AN
# OMISSION.
#
# `docs/Asset_Sourcing.md` section 3 maps her Hex to one of the Four Summoning Circles and her
# Grand Coven to a second circle plus a bloom. `docs/TODO.md` section 131.2 allows a mapping to
# be dropped provided the reason is recorded after an in-engine comparison, and this is that
# record. Three reasons, in order of weight:
#
# 1. A FLAT DECAL CANNOT DRAPE TO THE KERB, AND THAT IS A FAULT SOMEBODY REPORTED BY NAME.
#    `HeroHazards.SpawnHexSigil` runs `VfxShapes.DrapeToGround` over both of the ward's layers
#    because 🧑 said, of the ultimate's circle, *"her magic circle doesnt draw over the sidewalk
#    and thats weird af"*. The ward is 4.8 m across and Ilalim ng Tulay has pavements inside
#    that. A 512 px quad has four vertices and nothing to conform with, so swapping the mesh for
#    the sourced art would trade a drawn circle for a floating one.
# 2. HER KIT HAS NO PRIMITIVE GEOMETRY IN IT AT ALL. `grep -n CreatePrimitive` over
#    `HeroHazards.cs` returns twenty-one sites and not one of them is Phaister's: the ward, the
#    tear, the arrival runes, the corona and the moon are all authored `VfxShapes` builders.
#    Section 131 replaces *"primitive-looking surfaces and transient layers"* and she has none.
# 3. THE THREE POWERS WERE ALREADY SEPARATED ONCE, ON REPORT. `docs/TODO.md` section 24 rebuilt
#    each on its own construction after *"her Q is just 2 stars on top of each other"*. Dropping
#    one downloaded circle onto two of them is how that gets undone.
#
# THE DOOR IS NOT CLOSED AND THE WAY THROUGH IT IS WRITTEN DOWN. A subdivided, UV-mapped ground
# plate carrying the sourced circle as a texture would drape exactly as the ward does and would
# put real authored script where `WardCircle` puts procedural glyph shapes. That is a mesh job
# rather than an import job, and `docs/TODO.md` section 131 carries it as open.
# ---------------------------------------------------------------------------


# ---------------------------------------------------------------------------
# ZACK'S BOLT RIBBON.
#
# WARNING  THE SOURCE IS A TILING FIELD OF WHITE BOLTS AND ONLY ONE COLUMN OF IT IS USED AT
# A TIME. hdst's texture is nine roughly vertical strokes across 512 px. A quad wearing the
# whole thing draws nine bolts at once, which is a curtain rather than a strike. One 64 px
# column per cell gives eight distinct bolts out of one file at no extra cost, and
# `VfxSheets` treats it as an eight-cell sheet whose cell is CHOSEN rather than stepped.
# ---------------------------------------------------------------------------
BOLT_SOURCE = os.path.join("oga", "lightnings", "lighting.png")
BOLT_CELL = 64
BOLT_CELLS = 8


def build_bolt(src_dir, out_dir, report):
    path = os.path.join(src_dir, BOLT_SOURCE)
    if not os.path.isfile(path):
        print("build_vfx_sheets: no lightning texture at %s" % path)
        sys.exit(2)

    src = Image.open(path).convert("RGBA")
    if src.size != (512, 512):
        print("build_vfx_sheets: the lightning texture is %s, not 512x512." % (src.size,))
        sys.exit(1)

    stops = RAMPS["spark"]
    out = Image.new("RGBA", (BOLT_CELL * BOLT_CELLS, 512))
    for i in range(BOLT_CELLS):
        col = src.crop((i * BOLT_CELL, 0, (i + 1) * BOLT_CELL, 512))
        px = []
        for r, g, b, a in col.getdata():
            if a == 0:
                px.append((0, 0, 0, 0))
                continue
            # The source is a white core with a soft grey falloff. Value drives the ramp the
            # same way it does for the flipbooks, so a bolt has an amber body and a bright
            # core rather than one flat yellow.
            c = sample_ramp(stops, min(1.0, lum((r, g, b)) / 255.0))
            px.append((c[0], c[1], c[2], a))
        cell = Image.new("RGBA", (BOLT_CELL, 512))
        cell.putdata(px)
        out.paste(cell, (i * BOLT_CELL, 0))

    stem = "vfx_bolt_v1"
    out.save(os.path.join(out_dir, stem + ".png"))
    opaque = [p[:3] for p in out.getdata() if p[3]]
    report.append({
        "stem": stem,
        "source": "hdst lightning texture",
        "ramp": "spark",
        "cell": BOLT_CELL,
        "cols": BOLT_CELLS,
        "frames": BOLT_CELLS,
        "fps": 0,
        "loop": False,
        # A bolt stands ON the ground rather than straddling it, so the anchor is the very
        # bottom of the cell.
        "pivot": 0.0,
        "size": out.size,
        "peak": int(max(lum(c) for c in opaque)),
    })


def contact_sheet(out_dir, report, path):
    """One picture of everything a run wrote. `CLAUDE.md` section 6.1: show, do not describe."""
    tiles = []
    for row in report:
        im = Image.open(os.path.join(out_dir, row["stem"] + ".png")).convert("RGBA")
        cell = row["cell"]
        n = row["frames"]
        picks = []
        for f in (0, n // 2, n - 1):
            cx = (f % row["cols"]) * cell
            cy = (f // row["cols"]) * cell
            crop = im.crop((cx, cy, cx + cell, cy + min(cell, im.size[1] - cy)))
            picks.append(crop.resize((128, 128), Image.NEAREST))
        tiles.append(picks)

    canvas = Image.new("RGB", (128 * 3, 128 * len(tiles)), (36, 32, 30))
    for r, picks in enumerate(tiles):
        for c, p in enumerate(picks):
            canvas.paste(p, (c * 128, r * 128), p)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    canvas.save(path)
    return path


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--src", default=SRC_DIR)
    ap.add_argument("--out", default=OUT_DIR)
    ap.add_argument("--contact", action="store_true")
    args = ap.parse_args()

    os.makedirs(args.out, exist_ok=True)
    report = []

    build_pvfx(args.src, args.out, report)
    build_bolt(args.src, args.out, report)

    # WARNING  THE BLOWOUT CAP IS ASSERTED HERE AND NOT TRUSTED TO THE RAMP TABLE. A stop
    # edited by hand is exactly how a white core gets back in, and `AbilityShowcaseProbe`
    # would then fail a whole capture run with the cause four files away.
    over = [r for r in report if r["peak"] >= BLOWN_LEVEL]
    if over:
        for r in over:
            print("build_vfx_sheets: %s peaks at luminance %d, at or over the %d blowout level."
                  % (r["stem"], r["peak"], BLOWN_LEVEL))
        print("    docs/VISION.md section 2 rule 5. Lower the ramp's top stop.")
        sys.exit(1)

    print("build_vfx_sheets: wrote %d sheets to %s" % (len(report), args.out))
    print()
    print("  %-20s %-34s %-7s %5s %5s %4s %4s %6s %6s %5s"
          % ("sheet", "source", "ramp", "cell", "cols", "fr", "fps", "loop", "pivot", "peak"))
    for r in report:
        print("  %-20s %-34s %-7s %5d %5d %4d %4d %6s %6.3f %5d"
              % (r["stem"], r["source"], r["ramp"], r["cell"], r["cols"],
                 r["frames"], r["fps"], str(r["loop"]).lower(), r["pivot"], r["peak"]))
    print()
    print("  Keep Assets/TumbangPreso/Runtime/Visual/VfxSheets.cs in step with this table.")
    print("  VfxSheetTests asserts every row of it against the PNGs on disk.")

    if args.contact:
        print("  contact sheet: %s"
              % contact_sheet(args.out, report,
                              os.path.join(REPO, "Logs", "shots-abilities", "vfx_sheets_v1.png")))


if __name__ == "__main__":
    main()
