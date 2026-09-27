"""Lagoon Court SEABED KIT: corals, sea grass and urchin rocks for the floor of the cove, models AND
the textures they wear.

  py -3 tools/lagoon_prop_seabed.py --paint                  # paint every psb_ texture
  py -3 tools/lagoon_prop_seabed.py --paint psb_brain        # paint the named ones
  blender -b --python tools/lagoon_prop_seabed.py -- --preview N

With --preview N the Blender run renders into Logs/lagoon-blender/: psb_lineup_vN.png (every kind,
three seeds each, on pale sand beside a 1.6 m pink scale cylinder), psb_close_<kind>_vN.png (one
close-up per kind, its three seeds side by side), psb_swatch_vN.png (the four drawings flat) and
psb_underwater_d2_vN.png / psb_underwater_d5_vN.png (the whole lineup seen from 10 m above a flat
translucent turquoise stand-in water plane at 2 m and 5 m over the sand, low sunset sun: the
readability test that matters, since nobody ever sees these props dry). It paints any missing psb_
texture first (through `py -3`, since Blender's Python has no PIL). An existing render is never
overwritten: bump N. Nothing is saved to a .blend; the cove script calls build_prop and links it.

WHY THIS KIT EXISTS. OWNER, 2026-09-27, looking into the water of the cove: "it lacks corals and
plants". The water reference (ANGRY MESH, Stylized Water, docs/LAGOON_REWORK_GUIDE.md section 1)
shows a sandy, stony bottom through clear turquoise water, and a Sama-Bajau village stands over
exactly that kind of shallow reef flat: sea grass meadows, brain and staghorn coral heads, table
corals, sea fans and urchins on the stones.

  import lagoon_prop_seabed as PSB
  col = PSB.build_prop(kind, seed)      # a Collection, NOT linked; one root empty named by kind
  report = PSB.check_prop(col)          # tris, non-manifold, coplanar overlaps, floating shells

KINDS, every one with its origin at the centre of its footprint on the seabed, z = 0 the sand, +Y
the front. Everything touching the sand sinks 2 to 5 cm into it, so a prop set on an uneven seabed
never shows a gap. Seeds 1, 2 and 3 are three different layouts (any other seed draws from the
same rules at random).

  * "brain_coral"   fat, soft, lumpy domes: seed 1 one big dome, seed 2 two, seed 3 three of
                    falling size in different colours (a small colony). The meander grooves are
                    PAINTED (psb_brain), projected straight down so on the steep sides they run
                    down the dome the way a real brain coral's valleys do. 1.0 to 1.55 m across.
  * "branch_coral"  a chunky staghorn clump: a low mound and 4 to 8 fat branches that leave it
                    sideways, taper, bend and curl upward, some with one fat fork, every end a
                    round knob. No branch is thinner than 4 cm across. 0.7 to 1.0 m across, 0.7 to
                    0.76 m tall.
  * "fan_coral"     a sea fan: one thick blade (seeds 1 and 3, seed 3 with a small second fan on
                    its own stalk) or two blades on a forked stalk, rolled apart into a V (seed
                    2). Each blade is a rounded SECTOR growing out of the stalk top like a hand
                    fan, a 3 to 5 cm slab with a rounded rim, a slight wave and cup, the lattice
                    PAINTED on (psb_lattice), never cut as holes. The blade faces +Y. 1.1 to 1.3
                    m across, 0.8 to 0.95 m tall, only 0.2 to 0.5 m deep.
  * "table_coral"   a flat, wavy, lobed plate on a short thick trunk that flares into the sand;
                    seed 3 adds a smaller second plate lower on a side limb. 1.1 to 1.5 m across,
                    0.4 to 0.46 m tall.
  * "sea_grass"     a clump of 12 to 25 broad ribbon blades (two-sided alpha cards in the land
                    kit's method, lagoon_cove_planting._card, with their own drawing psb_seagrass),
                    all leaning with one baked current and curling over at the tip. 0.5 to 1.0 m
                    tall. Seed 3 is the warmer olive variant.
  * "urchin_rock"   a small rounded stone (the pillow-stone recipe, section 3 of the guide) with,
                    seed 1, four or five chunky cartoon urchins (a ball with a few fat blunt
                    spikes) on top; seed 2, one fat five-armed starfish draped over it; seed 3,
                    a starfish on top and three urchins at its foot. 0.9 to 1.2 m across.

THE ROOT EMPTY carries prop_kind, prop_seed, prop_mount ("ground"), prop_variant and prop_box (xmin,
ymin, zmin, xmax, ymax, zmax, in the root's frame), as the sibling kits do.

⚠️ CHUNKY AND ORGANIC, NEVER FIDDLY (LAGOON_REWORK_GUIDE.md section 2, owner 2026-09-27: "we're
going for a stylized semi-cartoony environment style"). Every member here is fat, tapered, bent
and ends round; the fine detail a real reef has (polyps, the fan's mesh, the grooves) lives in the
painted textures only. Every prop stays under about 4k triangles.

ONE MESH PER PROP, SEVERAL MATERIALS, as in lagoon_prop_shore: one mesh object parented to the
root, each face carrying its material index. The sea grass is the exception by nature: it is
leaf cards (open, two-sided, custom normals pointing out of the clump) built with the planting
kit's own card code, so it is ONE card mesh with a light and a dark tint.

MATERIALS. A drawing is painted once in soft greys and MULTIPLIED by a reef colour on the material
(Unity: the material colour), the leaf-card rule, so one drawing serves every colour of its coral
and the colours stay one list (PALETTE). Names are psb_<drawing>_<colour>:

  psb_brain     NEW: brain coral meanders, wide soft ridges and narrower soft valleys 11 cm
                apart, a Turing labyrinth (band-passed noise, thresholded and re-filtered).
  psb_polyp     NEW: a coral skin. Big soft mottles and sparse, soft, 3 cm polyp cups at low
                contrast (the branch, table and trunk surfaces, the urchins and the starfish).
  psb_lattice   NEW: the sea fan's net, a soft pale vein network over darker cells about 12 cm
                across (the holes a real fan has, painted as shade).
  psb_seagrass  NEW, a CARD (greyscale with alpha): a broad ribbon with a round tip, a soft
                fold, two pale vein bands and a darker foot.
  psb_stone     rock_a (the approved rock) multiplied toward a cool green-grey, the stone of a
                seabed with a skin of algae, so the urchin rocks read apart from the pale sand.

⚠️ COLOURS (PALETTE) are a stylized reef tuned to read THROUGH turquoise water at sunset: coral
pink, magenta, orchid, warm yellow-ochre, a PINK peach and sea-green, each a mid-light value so the
water's cyan does not drown it. ROLE HUES (Art_Direction.md section 1): nothing near offence orange
#f87020 (hue 24) or defence blue #0080e8 (hue 207). audit_palette() measures every colour at import
and refuses one within 15 degrees of hue 24 or 25 degrees of hue 207 (a colour under 20 per cent
saturation is a grey and exempt). This is why the peach is a pink peach (hue about 7) and the
starfish is magenta or ochre, never the orange a real starfish often is.

UVs: one map "UVMap", WORLD SCALE, 1 UV unit = 2 m. Tubes (branches, stalks, trunks) V along, U
around; the domes and urchin balls are projected straight DOWN (x, y), so on a dome's flanks the
drawing stretches downhill; the fan blades are projected on their own plane; the table plates
straight down; the stone is box projected. The sea grass cards are 0..1 across and base to tip.

⚠️ NO TWO SURFACES SHARE A PLANE (KANTO_DESIGN_GUIDE.md section 2). The domes' flat undersides are
clamped at different depths under the sand; check_prop proves no coplanar overlaps and that every
shell is in a chain of intersections down to the sand.
"""
import colorsys
import math
import random
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TOOLS = ROOT / "tools"
if str(TOOLS) not in sys.path:
    sys.path.insert(0, str(TOOLS))

try:
    import bmesh
    import bpy
    from mathutils import Matrix, Vector
    from mathutils.bvhtree import BVHTree
except ImportError:          # plain Python: the --paint mode only
    bpy = None

import lagoon_prop_shore as PS    # field, mask, smooth, over, hexcol, normal_from_height

TEX = ROOT / "ArtSource" / "lagoon" / "textures"
LOGS = ROOT / "Logs" / "lagoon-blender"
PREFIX = "psb"

KINDS = ("brain_coral", "branch_coral", "fan_coral", "table_coral", "sea_grass", "urchin_rock")
SEEDS = (1, 2, 3)
UV_METRES = 2.0

# ================================================================ palette
# sRGB hex, the colour the painted drawing is multiplied TO (the drawings' mean sits near white).
# ⚠️ Mid-light values on purpose: at 2 to 8 m the water multiplies everything toward cyan and
# darkens it, and a dark coral reads as a hole in the seabed (seen in the d5 test renders).
PALETTE = {
    "pink":     "e8708f",    # coral pink, hue ~345
    "magenta":  "c64a9b",    # hue ~322
    # ⚠️ ORCHID, not lilac: under the turquoise stand-in water (psb_underwater_d2_v1) a cool
    # lilac (a88bd4, hue 264) lost its red and read BLUE, which is the defence hue's side of the
    # wheel; c48fd2 (hue 288) still read periwinkle (v3). A violet with this much red keeps
    # reading violet through the water.
    "orchid":   "cc8ad0",    # hue ~297
    "ochre":    "dcbc4c",    # warm yellow-ochre, hue ~47
    "peach":    "f0a096",    # a PINK peach, hue ~7: a yellow peach sits on offence orange
    # a yellower, lighter sea-green than 5fb38c (hue 152): that one sank into the water's own
    # cyan-green in the underwater test (v3), a table coral in it read as a patch of water
    "seagreen": "6cbf86",    # hue ~139
    "plum":     "7a3f78",    # the urchins, hue ~302: dark on purpose, the one dark accent
}
STONE_TINT = (0.56, 0.64, 0.58)            # linear multiply on rock_a: cool green-grey stone
# Sea grass cards: (light, dark) linear tints in the planting kit's convention (TINTS), per variant.
SEAGRASS_TINTS = {
    "green": ((0.40, 0.62, 0.20), (0.14, 0.36, 0.12)),
    "olive": ((0.62, 0.60, 0.20), (0.30, 0.34, 0.10)),
}
ROLE_HUES = ((24.0, 15.0, "offence orange #f87020"), (207.0, 25.0, "defence blue #0080e8"))


def _hsv_of(rgb):
    return colorsys.rgb_to_hsv(*rgb)


def audit_palette():
    """Every colour this kit can put on screen, measured against the role hues. Raises on a
    violation, returns the table otherwise. (Linear tints are audited as given: their hue is what
    a viewer sees after the drawing's grey multiplies them.)"""
    rows = []
    items = [(n, tuple(int(h[i:i + 2], 16) / 255 for i in (0, 2, 4))) for n, h in PALETTE.items()]
    items.append(("stone", STONE_TINT))
    for v, (lt, dk) in SEAGRASS_TINTS.items():
        items += [(f"seagrass_{v}_light", lt), (f"seagrass_{v}_dark", dk)]
    for name, rgb in items:
        h, s, _v = _hsv_of(rgb)
        hue = h * 360
        for role, gap, label in ROLE_HUES:
            d = abs((hue - role + 180) % 360 - 180)
            if s >= 0.2 and d < gap:
                raise ValueError(f"[prop-seabed] {name} hue {hue:.0f} is {d:.0f} deg from {label}")
        rows.append((name, round(hue), round(s, 2)))
    return rows


audit_palette()

# ================================================================ painting (numpy, PIL; py -3)
# The house style (KANTO_DESIGN_GUIDE.md section 3): flat fills, a few LARGE feathered patches,
# soft hand-drawn shapes, low contrast, no grain, no noise. Each drawing is its own, painted in
# greys around 0.9 so the material's reef colour lands on the surface (multiply).

SIZE = 1024
TILE_M = 2.0
NEW_TEXTURES = ("psb_brain", "psb_polyp", "psb_lattice", "psb_seagrass")


def _np():
    import numpy as np
    return np


def _blur(a, sigma_m):
    """Periodic gaussian blur, sigma in metres of the 2 m tile."""
    np = _np()
    h, w = a.shape
    fy = np.fft.fftfreq(h, d=TILE_M / h)[:, None]
    fx = np.fft.fftfreq(w, d=TILE_M / w)[None, :]
    k = np.exp(-2 * (math.pi ** 2) * (sigma_m ** 2) * (fx ** 2 + fy ** 2))
    return np.real(np.fft.ifft2(np.fft.fft2(a) * k)).astype(np.float32)


def _cells(n, seed, jitter=0.75):
    """A periodic jittered-grid Voronoi across the tile, n cells a side: (F1, F2) in metres."""
    np = _np()
    rng = np.random.default_rng(seed)
    off = rng.uniform(0.5 - jitter / 2, 0.5 + jitter / 2, (n, n, 2)).astype(np.float32)
    y, x = np.mgrid[0:SIZE, 0:SIZE].astype(np.float32)
    u, v = (x + 0.5) / SIZE * n, (y + 0.5) / SIZE * n
    ci, cj = np.floor(v).astype(int), np.floor(u).astype(int)
    f1 = np.full(u.shape, 1e9, np.float32)
    f2 = np.full(u.shape, 1e9, np.float32)
    for di in (-1, 0, 1):
        for dj in (-1, 0, 1):
            ni, nj = (ci + di) % n, (cj + dj) % n
            px = cj + dj + off[ni, nj, 0]
            py = ci + di + off[ni, nj, 1]
            d = np.sqrt((u - px) ** 2 + (v - py) ** 2)
            f2 = np.where(d < f1, f1, np.minimum(f2, d))
            f1 = np.minimum(f1, d)
    cell = TILE_M / n
    return f1 * cell, f2 * cell


def _grey(tone):
    np = _np()
    t = np.clip(tone, 0, 1)
    return np.dstack([t, t, t])


def psb_brain():
    """Brain coral. Research (painted brain corals in stylized underwater kits and real
    Diploria/Platygyra): long winding ridges with narrower valleys between them, the ridges running
    in loose PARALLEL meanders at one even spacing, a labyrinth. v1 drew the contour lines of a
    smooth random field and read as a contour map (uneven spacing, bald blobs). Now it is the
    Turing labyrinth: white noise band-passed to ONE wavelength (11 cm), pushed through a soft
    threshold and band-passed again three times, which settles into even, winding stripes. Wide
    soft ridges (a lit crown) over narrower soft valleys: at 1 to 8 m under the water a finer
    groove is noise, a coarser one reads as stripes painted on a ball."""
    np = _np()
    h, w = SIZE, SIZE
    fy = np.fft.fftfreq(h, d=TILE_M / h)[:, None]
    fx = np.fft.fftfreq(w, d=TILE_M / w)[None, :]
    f = np.sqrt(fx ** 2 + fy ** 2)
    f0 = 1 / 0.11
    ring = np.exp(-((f - f0) / (0.12 * f0)) ** 2)
    n = np.random.default_rng(201).standard_normal((h, w))
    for _ in range(8):
        n = np.real(np.fft.ifft2(np.fft.fft2(n) * ring))
        n = np.tanh(2.2 * n / (n.std() + 1e-9))
    # ⚠️ Keep the THRESHOLDED state (swatch v3 re-filtered it and cut deeper: the labyrinth broke
    # into leopard spots) and only soften its edges, then take the valley a little narrower than
    # the ridge, so the ridges read as the body and the valleys as grooves (swatch v2's dark bands
    # were as wide as the ridges, with hard edges).
    t = _blur(n.astype(np.float32), 0.012)
    valley = PS.smooth(np.clip((-t - 0.15) / 0.75, 0, 1))
    crown = PS.smooth(np.clip((t - 0.3) / 0.7, 0, 1))
    mott = PS.mask(PS.field(0.25, 203), 0.35, 0.6)
    tone = 0.93 - 0.26 * valley + 0.03 * crown - 0.06 * mott
    return _grey(tone.astype(np.float32)), (1.0 - valley + 0.2 * crown).astype(np.float32)


def psb_polyp():
    """A coral skin for branches, plates, trunks, urchins and the starfish. Research (stylized
    hand-painted coral in fantasy underwater kits): a flat body colour, one or two big soft patches
    a step darker, and a scatter of soft round polyp cups, a pale rim round a darker centre, big
    enough to see as dots from a few metres and soft enough never to read as grain."""
    np = _np()
    # ⚠️ one cup per 11 cm cell, 3 cm across: at 7.7 cm cells and 2 cm cups (v2) they read as
    # crumbs on a table plate and vanished at 3 m
    f1, _f2 = _cells(18, 211, jitter=0.6)
    cup = np.exp(-(f1 / 0.016) ** 2)
    rim = np.exp(-((f1 - 0.028) / 0.009) ** 2)
    mott = PS.mask(PS.field(0.16, 212), 0.35, 0.6)
    light = PS.mask(PS.field(0.22, 213), 0.25, 0.6)
    tone = 0.91 - 0.1 * cup + 0.035 * rim - 0.07 * mott + 0.04 * light
    return _grey(tone), rim * 0.5 - cup + 0.1 * light


def psb_lattice():
    """A sea fan's net. Research (gorgonian sea fans; stylized fans in painted reef kits): the fan
    is a mesh of branchlets, drawn in stylized art as a pale vein network over darker cells, with
    the cells reading as the holes. Cells about 12 cm across and veins about 3 cm wide: CHUNKY,
    readable at a few metres, never a fine net. Painted, not modelled (the owner's no-fiddly rule;
    holes cut in a 4 cm slab would be hundreds of triangles and a lattice of thin members)."""
    np = _np()
    f1, f2 = _cells(16, 221, jitter=0.8)
    edge = f2 - f1
    vein = 1.0 - PS.smooth(np.clip(edge / 0.032, 0, 1))
    hole = PS.smooth(np.clip((f1 - 0.02) / 0.05, 0, 1))
    mott = PS.mask(PS.field(0.2, 222), 0.3, 0.6)
    tone = 0.93 - 0.22 * hole * (1 - vein) - 0.05 * mott
    return _grey(tone), vein - 0.3 * hole


def psb_seagrass(w=128, h=1024):
    """A sea grass blade CARD (greyscale with alpha, tinted by its material, the planting kit's
    card rule). Research (Enhalus and Thalassia meadows; stylized sea grass in painted kits): a
    strap of even width with a ROUND tip, not the pointed land grass blade, so this is its own
    drawing and not `blade` recoloured. In the same hand as the land blade: a dark foot to a light
    tip, a lit half, a soft crease with a pale ridge, two soft vein bands, a darker margin."""
    np = _np()
    v, u = np.mgrid[0:h, 0:w].astype(np.float32)
    u, v = u / (w - 1), 1 - v / (h - 1)
    x = (u - 0.5) * 2
    tip = 0.84
    half = 0.86 * np.clip(v * 12, 0, 1) ** 0.35
    over_tip = np.clip((v - tip) / (1 - tip), 0, 1)
    half = np.where(v > tip, half * np.sqrt(np.clip(1 - over_tip ** 2, 0, 1)), half)
    d = half - np.abs(x)
    alpha = np.clip(d * w / 2 / 1.5 + 0.5, 0, 1)
    xn = x / np.maximum(half, 1e-3)

    def feather(a, lo, hi):
        t = np.clip((a - lo) / (hi - lo), 0, 1)
        return t * t * (3 - 2 * t)
    tone = 0.56 + 0.32 * np.clip(v, 0, 1) ** 0.8
    tone = tone + 0.14 * feather(-xn, -0.1, 0.3)
    tone = tone - 0.16 * np.exp(-(xn / 0.1) ** 2)
    tone = tone + 0.10 * np.exp(-((xn + 0.22) / 0.1) ** 2)
    for c in (0.55, -0.6):
        tone = tone + 0.07 * np.exp(-((xn - c) / 0.12) ** 2)
    tone = tone - 0.12 * feather(np.abs(xn), 0.74, 1.0)
    tone = tone - 0.12 * (1 - feather(v, 0.04, 0.25))
    tone = tone + 0.10 * np.exp(-(((xn + 0.4) / 0.45) ** 2 + ((v - 0.6) / 0.22) ** 2))
    g = np.clip(tone * 0.92, 0, 1)
    return np.dstack([g, g, g, alpha])


PAINTERS = {"psb_brain": psb_brain, "psb_polyp": psb_polyp, "psb_lattice": psb_lattice}
CARDS = {"psb_seagrass": psb_seagrass}
# ⚠️ Low on purpose (close-up v1): at 1.2 / 0.8 the grooves and polyp cups rendered as CARVED
# holes (a cheese, a pepperoni); the painted value alone carries them, the relief only hints.
STRENGTH = {"psb_brain": 0.7, "psb_polyp": 0.35, "psb_lattice": 0.5}


def save(name, albedo, height):
    np = _np()
    from PIL import Image
    TEX.mkdir(parents=True, exist_ok=True)
    Image.fromarray((np.clip(albedo, 0, 1) * 255 + 0.5).astype(np.uint8)).save(TEX / f"{name}_albedo.png")
    rng = np.ptp(height)
    hh = (height - height.min()) / rng if rng > 1e-9 else np.full_like(height, 0.5)
    Image.fromarray((hh * 255 + 0.5).astype(np.uint8)).save(TEX / f"{name}_height.png")
    nrm = PS.normal_from_height(hh, STRENGTH[name])
    Image.fromarray((nrm * 255 + 0.5).astype(np.uint8)).save(TEX / f"{name}_normal.png")
    print("[prop-seabed] painted", name)


def save_card(name, rgba):
    np = _np()
    from PIL import Image
    TEX.mkdir(parents=True, exist_ok=True)
    Image.fromarray((np.clip(rgba, 0, 1) * 255 + 0.5).astype(np.uint8), "RGBA").save(TEX / f"{name}_albedo.png")
    print("[prop-seabed] painted", name)


def swatch_sheet(version):
    """The four drawings flat, each at its tile and multiplied by its reef colours, one labelled
    sheet: Logs/lagoon-blender/psb_swatch_vN.png."""
    np = _np()
    from PIL import Image, ImageDraw
    path = LOGS / f"{PREFIX}_swatch_v{version}.png"
    if path.exists():
        return
    LOGS.mkdir(parents=True, exist_ok=True)
    rows = [("psb_brain", ["ochre", "peach", "seagreen", "orchid"]),
            ("psb_polyp", ["pink", "orchid", "ochre", "plum"]),
            ("psb_lattice", ["magenta", "orchid", "ochre", "pink"])]
    T = 300
    sheet = Image.new("RGB", (T * 5 + 60, T * 3 + 80), (40, 40, 40))
    dr = ImageDraw.Draw(sheet)
    for r, (name, cols) in enumerate(rows):
        a = np.asarray(Image.open(TEX / f"{name}_albedo.png").convert("RGB"), np.float32) / 255
        crop = a[:512, :512]                     # 1 m x 1 m
        for c, col in enumerate(cols):
            tint = np.array([int(PALETTE[col][i:i + 2], 16) / 255 for i in (0, 2, 4)], np.float32)
            img = Image.fromarray((np.clip(crop * tint / 0.93, 0, 1) * 255).astype(np.uint8)).resize((T, T))
            sheet.paste(img, (20 + c * (T + 5), 40 + r * (T + 5)))
            dr.text((24 + c * (T + 5), 44 + r * (T + 5)), f"{name} {col} (1 m)", fill=(255, 255, 255))
    card = Image.open(TEX / "psb_seagrass_albedo.png").convert("RGBA").resize((40, 320))
    sheet.paste(card, (20 + 4 * (T + 5) + 60, 40), card)
    dr.text((20 + 4 * (T + 5) + 20, 370), "psb_seagrass card", fill=(255, 255, 255))
    sheet.save(path)
    print("[prop-seabed] swatches", path)


def paint(names=None):
    for n in names or list(PAINTERS) + list(CARDS):
        if n in PAINTERS:
            save(n, *PAINTERS[n]())
        else:
            save_card(n, CARDS[n]())


def _ensure_textures():
    """Blender's Python has no PIL: paint any missing psb_ texture through the system Python."""
    missing = [n for n in NEW_TEXTURES if not (TEX / f"{n}_albedo.png").exists()]
    if missing:
        subprocess.run(["py", "-3", str(Path(__file__).resolve()), "--paint", *missing], check=True)


# ================================================================ geometry (Blender)

if bpy is not None:
    import author_lagoon_boats as BK          # Piece, sweep, loft, _uv_frame
    import lagoon_cove_planting as PL         # _Mesh, _card, _card_material (the leaf-card method)
    from render_lagoon_texture_preview import uv_material

    Z = Vector((0.0, 0.0, 1.0))
    DOWN = Vector((0.0, 0.0, -1.0))

    class Prop:
        """Collects the pieces of one prop. Each face carries its material SLOT (a "drawing:colour"
        key) as an index into this prop's own slot list. Interface-compatible with the boat kit's
        builder, so its sweep and loft work unchanged."""

        def __init__(self, kind, seed):
            self.kind, self.seed = kind, seed
            self.rng = random.Random(f"lagoon-prop-seabed:{kind}:{seed}")
            self.pieces, self.slots = [], []
            self.variant = ""

        def add(self, slot, pc):
            if pc is None or not pc.bm.faces:
                return None
            if slot not in self.slots:
                self.slots.append(slot)
            idx = self.slots.index(slot)
            for f in pc.bm.faces:
                f.material_index = idx
            self.pieces.append(pc)
            return pc

        def uv_off(self):
            return (self.rng.random(), self.rng.random())

    def xform(pc, M):
        bmesh.ops.transform(pc.bm, matrix=M, verts=pc.bm.verts)
        pc.bm.normal_update()
        return pc

    def uv_down(pc, off):
        """World-scale planar UVs straight DOWN (x, y). On a dome's steep flank the drawing
        stretches downhill, which is how a brain coral's valleys run down its sides, and there is
        no seam anywhere on the dome."""
        s = 1.0 / UV_METRES
        for f in pc.bm.faces:
            for loop in f.loops:
                q = loop.vert.co
                loop[pc.uv].uv = (q.x * s + off[0], q.y * s + off[1])

    def lumpy(d, lumps):
        """Soft lumps on a unit direction: a few broad bumps, never ridges or spikes."""
        k = 1.0
        for c, amp in lumps:
            k += amp * max(0.0, d.dot(c)) ** 3
        return k

    def rand_dir(rng, zmin=-0.2):
        while True:
            v = Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), rng.uniform(zmin, 1)))
            if 0.2 < v.length <= 1.0:
                return v.normalized()

    def ball(p, slot, centre, radii, lumps=(), segs=(20, 12), floor=None, uv="down"):
        """A UV sphere scaled to `radii`, pushed by `lumps`, around `centre`. `floor` clamps every
        vertex below that z up onto it: the flat, buried underside of a dome (each dome gets its
        own floor depth, so two domes' undersides never share a plane)."""
        pc = BK.Piece()
        bmesh.ops.create_uvsphere(pc.bm, u_segments=segs[0], v_segments=segs[1], radius=1.0)
        C = Vector(centre)
        for v in pc.bm.verts:
            d = v.co.normalized()
            k = lumpy(d, lumps)
            q = Vector((d.x * radii[0], d.y * radii[1], d.z * radii[2])) * k + C
            if floor is not None and q.z < floor:
                q.z = floor
            v.co = q
        pc.bm.normal_update()
        if uv == "down":
            uv_down(pc, p.uv_off())
        else:
            BK._uv_frame(pc, Vector((1, 0, 0)), Vector((0, 1, 0)), Z, p.uv_off())
        return p.add(slot, pc)

    def member(p, slot, pts, r0, r1, sides=8, step=0.045, tip=True, base_flare=None):
        """A fat round member along `pts`, tapering from r0 to r1, its far end a round KNOB (the
        radius falls on a quarter circle over the last r1 of length, stopping at 30 per cent so
        the cap stays a small disc, not a point). `base_flare` (radius, length) swells the foot,
        a trunk's root flare into the sand."""
        P = [Vector(q) for q in pts]
        cum = [0.0]
        for a, b in zip(P, P[1:]):
            cum.append(cum[-1] + (b - a).length)
        L = cum[-1]

        def at(s):
            s = max(0.0, min(L, s))
            for i in range(len(P) - 1):
                if cum[i + 1] >= s:
                    seg = cum[i + 1] - cum[i]
                    return P[i].lerp(P[i + 1], 0.0 if seg < 1e-9 else (s - cum[i]) / seg)
            return P[-1]

        def body(s):
            r = r0 + (r1 - r0) * (s / L)
            if base_flare and s < base_flare[1]:
                t = 1 - s / base_flare[1]
                r += (base_flare[0] - r0) * t * t
            return r
        # ⚠️ The body is sampled every `step`, the KNOB at four fixed angles of a quarter circle:
        # uniform sampling put one ring in the last 5 cm and the "round" end came out a cone.
        end = L - r1 if tip else L
        n = max(2, int(math.ceil(end / step)))
        samples = [(end * i / n, body(end * i / n)) for i in range(n + 1)]
        if base_flare:
            fl = [(s, body(s)) for s in (base_flare[1] * 0.3, base_flare[1] * 0.65) if s < end]
            samples = sorted(samples + fl)
        if tip:
            for deg in (30, 55, 75):
                a = math.radians(deg)
                s = end + r1 * math.sin(a)
                samples.append((s, body(end) * math.cos(a)))
        rings, uvs = [], []
        prev = None
        for s, rr in samples:
            T = at(s + 0.01) - at(s - 0.01)
            T = T.normalized() if T.length > 1e-9 else Z.copy()
            if prev is None:
                e1, e2 = BK._perp(T)
            else:
                e2 = prev - T * prev.dot(T)
                e2 = e2.normalized() if e2.length > 1e-6 else BK._perp(T)[1]
                e1 = T.cross(e2).normalized()
                e2 = e1.cross(T).normalized()
            prev = e2
            c = at(s)
            ph = 0.3
            rings.append([c + (e1 * math.cos(a) + e2 * math.sin(a)) * rr
                          for a in (ph + math.tau * k / sides for k in range(sides))])
            arc = math.tau * rr / sides
            uvs.append([(k * arc, s) for k in range(sides + 1)])
        return p.add(slot, BK.loft(rings, uvs, off=p.uv_off()))

    def curve(start, az, pitch0, pitch1, length, steps, wander=0.0, rng=None, bend=1.0):
        """A path leaving `start` toward azimuth `az`, its pitch sweeping pitch0 -> pitch1
        (radians), the azimuth wandering a little per step: a bent, organic limb."""
        pts = [Vector(start)]
        ds = length / steps
        a = az
        for k in range(steps):
            t = (k + 0.5) / steps
            pitch = pitch0 + (pitch1 - pitch0) * (t ** bend)
            if rng is not None and wander:
                a += rng.uniform(-wander, wander)
            d = Vector((math.cos(a) * math.cos(pitch), math.sin(a) * math.cos(pitch), math.sin(pitch)))
            pts.append(pts[-1] + d * ds)
        return BK._catmull(pts, per=3)

    def disc_slab(p, slot, M, T, nt, nr, U, V, rim_push=0.5):
        """A THICK rounded slab over a disc-shaped mid surface M(theta, rho) (rho 0 the centre, 1 the
        rim), thickness T(theta, rho): the fan blades, the table plates and the starfish. Front and
        back skins offset along the surface normal, joined by a rim of one extra ring pushed
        outward by rim_push x T, so the edge is ROUND in section, never a sawn board edge. One
        closed shell. UVs: world-scale planar on the axes U, V."""
        e = 1e-3

        def nrm(th, r):
            r = max(r, 0.04)
            a = M(th, min(1.0, r + e)) - M(th, max(0.0, r - e))
            b = M(th + e, r) - M(th - e, r)
            n = a.cross(b)
            return n.normalized() if n.length > 1e-12 else Z.copy()
        ths = [math.tau * i / nt for i in range(nt)]
        rhos = [k / nr for k in range(1, nr + 1)]
        pc = BK.Piece()
        bm, uvl = pc.bm, pc.uv
        c0 = M(0.0, 0.0)
        nc = sum((nrm(t, 0.05) for t in ths), Vector()).normalized()
        t0 = T(0.0, 0.0)
        cf, cb = bm.verts.new(c0 + nc * t0 / 2), bm.verts.new(c0 - nc * t0 / 2)
        F, B = [], []
        for r in rhos:
            fr, br = [], []
            for th in ths:
                q, n, t = M(th, r), nrm(th, r), T(th, r)
                fr.append(bm.verts.new(q + n * t / 2))
                br.append(bm.verts.new(q - n * t / 2))
            F.append(fr)
            B.append(br)
        rim = []
        for i, th in enumerate(ths):
            q, n, t = M(th, 1.0), nrm(th, 1.0), T(th, 1.0)
            out = M(th, 1.0) - M(th, 1.0 - 0.02)
            out = (out - n * out.dot(n)).normalized()
            rim.append(bm.verts.new(q + out * t * rim_push))
        faces = []
        for i in range(nt):
            j = (i + 1) % nt
            faces.append(bm.faces.new((cf, F[0][i], F[0][j])))
            faces.append(bm.faces.new((cb, B[0][j], B[0][i])))
            for k in range(nr - 1):
                faces.append(bm.faces.new((F[k][i], F[k + 1][i], F[k + 1][j], F[k][j])))
                faces.append(bm.faces.new((B[k][j], B[k + 1][j], B[k + 1][i], B[k][i])))
            faces.append(bm.faces.new((F[-1][i], rim[i], rim[j], F[-1][j])))
            faces.append(bm.faces.new((rim[i], B[-1][i], B[-1][j], rim[j])))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        ou, ov = p.uv_off()
        s = 1.0 / UV_METRES
        for f in bm.faces:
            for loop in f.loops:
                q = loop.vert.co
                loop[uvl].uv = (q.dot(U) * s + ou, q.dot(V) * s + ov)
        bm.normal_update()
        return p.add(slot, pc)

    def slot_of(drawing, colour):
        return f"{drawing}:{colour}"

    # ------------------------------------------------------------ kinds

    BRAIN_COLOURS = ("ochre", "peach", "seagreen", "orchid")

    def build_brain_coral(p):
        r = p.rng
        count = {1: 1, 2: 2, 3: 3}.get(p.seed, r.randint(1, 3))
        sizes = {1: [r.uniform(0.52, 0.66)], 2: [r.uniform(0.42, 0.5), r.uniform(0.26, 0.33)],
                 3: [r.uniform(0.33, 0.38), r.uniform(0.25, 0.3), r.uniform(0.18, 0.22)]}[count]
        cols = r.sample(BRAIN_COLOURS, count)
        placed = []
        phase = r.uniform(0, math.tau)
        for i, R in enumerate(sizes):
            if not placed:
                cx, cy = 0.0, 0.0
            else:
                # snug against the first dome, overlapping a fifth of the smaller radius
                a = phase + i * r.uniform(1.9, 2.5)
                dist = sizes[0] + R * 0.7
                cx, cy = placed[0][0] + math.cos(a) * dist, placed[0][1] + math.sin(a) * dist
            h = R * r.uniform(0.68, 0.82)
            rx, ry = R * r.uniform(0.94, 1.06), R * r.uniform(0.94, 1.06)
            lumps = [(rand_dir(r, 0.1), r.uniform(0.05, 0.1)) for _ in range(4)]
            # ⚠️ the sphere's centre sits 15 % of the height above the sand, so the dome meets the
            # sand just below its fattest girth: it SITS, like a real coral head, not a ball
            # dropped on the floor and not a flat blister.
            rz = h / 1.15
            ball(p, slot_of("brain", cols[i]), (cx, cy, h - rz), (rx, ry, rz), lumps,
                 segs=(22, 12), floor=-(0.03 + 0.009 * i))
            placed.append((cx, cy, R))
        p.variant = f"{count} dome{'s' if count > 1 else ''}: " + ", ".join(cols)

    BRANCH_COLOURS = ("pink", "ochre", "magenta")

    def build_branch_coral(p):
        r = p.rng
        col = BRANCH_COLOURS[(p.seed - 1) % 3] if p.seed in SEEDS else r.choice(BRANCH_COLOURS)
        slot = slot_of("polyp", col)
        nb = {1: 5, 2: 7, 3: 6}.get(p.seed, r.randint(4, 8))
        # the mound the branches grow from: low, wide, buried half in the sand
        ball(p, slot, (0, 0, 0.0), (r.uniform(0.17, 0.2), r.uniform(0.15, 0.18), 0.15),
             [(rand_dir(r, 0.2), 0.12) for _ in range(3)], segs=(14, 8), floor=-0.035)
        phase = r.uniform(0, math.tau)
        forks = 0
        for k in range(nb):
            az = phase + math.tau * k / nb + r.uniform(-0.3, 0.3)
            # ⚠️ Branches leave from all over the mound, not one point: from one point (v1)
            # they fanned out like the fingers of a hand.
            rad0 = r.uniform(0.02, 0.11)
            start = Vector((math.cos(az) * rad0, math.sin(az) * rad0, 0.07))
            L = r.uniform(0.46, 0.78)
            # low branches spread wide, the later ones stand more upright: a clump, not a star
            upright = k % 2 == 1
            p0 = math.radians(r.uniform(50, 65) if upright else r.uniform(25, 38))
            p1 = math.radians(r.uniform(76, 88) if upright else r.uniform(62, 78))
            pts = curve(start, az, p0, p1, L, 6, wander=0.12, rng=r, bend=1.3)
            rb = r.uniform(0.075, 0.09)
            rt = rb * r.uniform(0.6, 0.7)
            member(p, slot, pts, rb, rt)
            # ⚠️ one FAT fork on about half the branches, no twigs: a fork is at least 4 cm thick
            # (owner: no thin details) and leaves in the upper half so the clump stays readable.
            if r.random() < 0.6 and forks < 4:
                forks += 1
                t = r.uniform(0.4, 0.55)
                i = int(t * (len(pts) - 1))
                fa = az + r.choice((-1, 1)) * r.uniform(0.5, 0.8)
                L2 = L * r.uniform(0.4, 0.55)
                sub = curve(pts[i], fa, math.radians(r.uniform(35, 50)), math.radians(r.uniform(72, 86)),
                            L2, 4, wander=0.1, rng=r, bend=1.2)
                rs = max(0.05, rb * 0.8 - 0.01)
                member(p, slot, sub, rs, max(0.04, rs * 0.72))
        p.variant = f"{nb} branches, {forks} forks, {col}"

    FAN_COLOURS = ("magenta", "orchid", "ochre")

    def fan_blade(p, slot, apex, H, half_angle, lean, turn, wave=0.025, cup=0.04, roll=0.0):
        """One fan blade: a rounded SECTOR, its apex (the narrow foot) on `apex`, H tall from it,
        opening `half_angle` each side of vertical, in its own plane x-z facing +Y; then leant
        back by `lean`, rolled sideways in its own plane by `roll` and turned by `turn` about Z,
        all round the apex.
        ⚠️ WHY A SECTOR. v2 was an ellipse pinched at the bottom: its lower flanks swelled back
        down to the sand, so the fan sat on the ground like a cushion, the stalk a stub in front.
        A real sea fan (and every stylized one) is a hand fan: it grows OUT of the stalk top as a
        wedge that broadens into a round crown, clear of the sand all round."""
        r = p.rng
        ph1, ph2, phw = r.uniform(0, math.tau), r.uniform(0, math.tau), r.uniform(0, math.tau)
        lam = r.uniform(0.5, 0.7)
        W = 2 * H * math.sin(half_angle)

        def inside(x, z):
            if z <= 0.0:
                return False
            ang = math.atan2(x, z)
            if abs(ang) > half_angle:
                return False
            u = ang / half_angle
            rmax = H * (1 + 0.06 * math.sin(3 * math.pi * u + ph1) + 0.04 * math.sin(5 * math.pi * u + ph2))
            return math.hypot(x, z) <= rmax
        C = (0.0, H * 0.5)
        nt = 30
        Rs = []
        for i in range(nt):
            th = math.tau * i / nt
            dx, dz = math.sin(th), math.cos(th)
            lo, hi = 0.0, 2 * H
            for _ in range(24):
                mid = (lo + hi) / 2
                if inside(C[0] + dx * mid, C[1] + dz * mid):
                    lo = mid
                else:
                    hi = mid
            Rs.append(lo)
        # round the sector's corners and its apex: two passes of a circular 1-2-1 average
        for _ in range(2):
            Rs = [(Rs[i - 1] + 2 * Rs[i] + Rs[(i + 1) % nt]) / 4 for i in range(nt)]

        def Rof(th):
            f = (th % math.tau) / math.tau * nt
            i = int(f) % nt
            t = f - int(f)
            return Rs[i] * (1 - t) + Rs[(i + 1) % nt] * t

        def M(th, rho):
            R = Rof(th) * rho
            x, z = C[0] + math.sin(th) * R, C[1] + math.cos(th) * R
            y = wave * math.sin(math.tau * x / lam + phw) * rho ** 1.5 + cup * (x / (W / 2)) ** 2
            return Vector((x, y, z))

        def T(th, rho):
            return 0.05 - 0.02 * rho
        pc = disc_slab(p, slot, M, T, nt, 4, Vector((1, 0, 0)), Vector((0, 0, 1)))
        foot = M(math.pi, 1.0).z
        xform(pc, Matrix.Translation(apex) @ Matrix.Rotation(turn, 4, "Z") @ Matrix.Rotation(roll, 4, "Y")
              @ Matrix.Rotation(-lean, 4, "X") @ Matrix.Translation((0, 0, -foot - 0.03)))
        return pc

    def build_fan_coral(p):
        r = p.rng
        col = FAN_COLOURS[(p.seed - 1) % 3] if p.seed in SEEDS else r.choice(FAN_COLOURS)
        slot = slot_of("lattice", col)
        stalk_slot = slot_of("polyp", col)
        mode = {1: "single", 2: "vee", 3: "pair"}.get(p.seed, r.choice(("single", "vee", "pair")))
        hs = r.uniform(0.14, 0.2)
        # ⚠️ Fans STAND on a visible stalk, leant back 3 to 6 degrees; the blade's foot sinks 3 cm
        # into the stalk's round top, so stalk and blade read as one growth.
        if mode == "vee":
            stalk = [Vector((0, 0, -0.04)), Vector((0.0, 0.01, hs * 0.5)), Vector((0.0, 0.0, hs * 0.75))]
            member(p, stalk_slot, stalk, 0.065, 0.055, tip=True, base_flare=(0.1, 0.06))
            # the stalk forks into two short fat limbs, one fan on each, splayed into a V
            for sgn in (-1, 1):
                top = Vector((sgn * 0.17, 0.0, hs + 0.1))
                limb = [Vector((0, 0, hs * 0.55)), Vector((sgn * 0.08, 0, hs + 0.02)), top]
                member(p, stalk_slot, limb, 0.048, 0.042, tip=True)
                # ⚠️ splayed apart by ROLLING each fan outward in its own plane: turning them about
                # Z (v3, v4) only showed them edge-on, and the two crowns still overlapped into
                # one heart shape from the front
                fan_blade(p, slot, top, r.uniform(0.56, 0.62), math.radians(r.uniform(30, 36)),
                          math.radians(r.uniform(4, 8)), sgn * r.uniform(0.1, 0.18),
                          roll=sgn * math.radians(r.uniform(22, 28)))
        else:
            stalk = [Vector((0, 0, -0.04)), Vector((0.01, 0.0, hs * 0.5)), Vector((0.0, 0.01, hs))]
            member(p, stalk_slot, stalk, 0.065, 0.05, tip=True, base_flare=(0.1, 0.06))
            big = r.uniform(0.74, 0.84) if mode == "single" else r.uniform(0.66, 0.74)
            fan_blade(p, slot, Vector((0.0, 0.01, hs)), big, math.radians(r.uniform(44, 54)),
                      math.radians(r.uniform(3, 6)), r.uniform(-0.2, 0.2))
            if mode == "pair":
                # a young fan on its own short stalk, in front and to one side, turned away
                sx = r.choice((-1, 1)) * r.uniform(0.5, 0.58)
                hs2 = hs * 0.65
                s2 = [Vector((sx, 0.22, -0.04)), Vector((sx, 0.22, hs2 * 0.5)), Vector((sx + 0.01, 0.22, hs2))]
                member(p, stalk_slot, s2, 0.05, 0.042, tip=True, base_flare=(0.075, 0.05))
                fan_blade(p, slot, Vector((sx + 0.01, 0.22, hs2)), r.uniform(0.4, 0.46),
                          math.radians(r.uniform(38, 46)), math.radians(r.uniform(6, 10)), -sx * 0.9)
        p.variant = f"{mode}, {col}"

    TABLE_COLOURS = ("seagreen", "ochre", "peach")

    def table_plate(p, slot, centre, R0, thick=(0.09, 0.055)):
        r = p.rng
        ph = [r.uniform(0, math.tau) for _ in range(4)]
        nw = r.choice((3, 4, 5))
        # ⚠️ the wave scales with the plate: a fixed 3 to 5 cm crumpled the small second plate (v2)
        amp = R0 * r.uniform(0.05, 0.08)

        def Rof(th):
            return R0 * (1 + 0.08 * math.sin(3 * th + ph[0]) + 0.05 * math.sin(5 * th + ph[1])
                         + 0.025 * math.sin(9 * th + ph[2]))

        def M(th, rho):
            R = Rof(th) * rho
            z = 0.1 * R0 * rho * rho + amp * rho * rho * math.sin(nw * th + ph[3])
            return Vector((centre[0] + math.cos(th) * R, centre[1] + math.sin(th) * R, centre[2] + z))

        def T(th, rho):
            return thick[0] + (thick[1] - thick[0]) * rho
        return disc_slab(p, slot, M, T, 36, 5, Vector((1, 0, 0)), Vector((0, 1, 0)))

    def build_table_coral(p):
        r = p.rng
        col = TABLE_COLOURS[(p.seed - 1) % 3] if p.seed in SEEDS else r.choice(TABLE_COLOURS)
        slot = slot_of("polyp", col)
        R0 = {1: r.uniform(0.62, 0.7), 2: r.uniform(0.5, 0.56), 3: r.uniform(0.5, 0.55)}.get(p.seed, r.uniform(0.45, 0.7))
        h = r.uniform(0.2, 0.32)
        lean = Vector((r.uniform(-0.06, 0.06), r.uniform(-0.06, 0.06), 0))
        top = Vector((0, 0, h)) + lean
        # ⚠️ the trunk ends INSIDE the plate, at its mid surface: ending it 3 cm up put its flat cap
        # within 5 mm of the plate's top skin (check_prop: one coplanar overlap on seed 1)
        trunk = [Vector((0, 0, -0.04)), Vector((0, 0, h * 0.4)) + lean * 0.3, top]
        member(p, slot, trunk, 0.11, 0.08, tip=False, base_flare=(0.17, 0.09))
        table_plate(p, slot, top, R0)
        tiers = 1
        if p.seed == 3:
            tiers = 2
            a = r.uniform(0, math.tau)
            d = R0 * 0.95
            end = Vector((math.cos(a) * d, math.sin(a) * d, h * 0.45))
            limb = [Vector((0, 0, h * 0.25)), Vector((math.cos(a) * d * 0.5, math.sin(a) * d * 0.5, h * 0.35)),
                    end]
            member(p, slot, limb, 0.075, 0.06, tip=False)
            table_plate(p, slot, end, R0 * 0.42, thick=(0.07, 0.045))
        p.variant = f"{tiers} plate{'s' if tiers > 1 else ''}, {col}"

    def urchin(p, slot, centre, up, R, rng):
        """A cartoon urchin: a fat ball and 9 to 12 fat, blunt, tapered spikes spread over the
        upper part (none pointing into what it sits on). Each spike is a three-ring loft, its end
        a small flat disc that smooth shading turns into a round blunt tip."""
        ball(p, slot, centre, (R, R, R * 0.85), (), segs=(12, 7), uv="box")
        # ⚠️ FEW, FAT spikes (v1 had 12 to 18 thin ones, which read as a spiky burr): 8 to 10
        # directions round the whole ball, the ones into the support dropped, 6 to 8 left.
        n = rng.randint(8, 10)
        golden = math.pi * (3 - math.sqrt(5))
        spin = rng.uniform(0, math.tau)
        C = Vector(centre)
        for i in range(n):
            zz = 1 - 2 * (i + 0.5) / n
            rr = math.sqrt(max(0.0, 1 - zz * zz))
            d = Vector((math.cos(golden * i + spin) * rr, math.sin(golden * i + spin) * rr, zz))
            if d.dot(up) < -0.1:
                continue
            L = R * rng.uniform(0.8, 1.0)
            rb = R * 0.42
            e1, e2 = BK._perp(d)
            rings, uvs = [], []
            for s, f in ((R * 0.6, 1.0), (R + L * 0.55, 0.75), (R + L, 0.5)):
                c = C + d * s
                ring = [c + (e1 * math.cos(a) + e2 * math.sin(a)) * rb * f
                        for a in (math.tau * k / 6 for k in range(6))]
                rings.append(ring)
                uvs.append([(k * rb, s) for k in range(7)])
            p.add(slot, BK.loft(rings, uvs, off=p.uv_off()))

    def stone(p, rng):
        """A small seabed stone: the pillow-stone recipe (guide section 3), a lumpy sphere with two
        or three broad flat facets on its upper flanks, buried a third in the sand."""
        R = rng.uniform(0.3, 0.4)
        radii = (R * rng.uniform(1.0, 1.2), R * rng.uniform(0.85, 1.0), R * rng.uniform(0.75, 0.88))
        z0 = radii[2] * 0.35
        pc = ball(p, "stone:", (0, 0, z0), radii, [(rand_dir(rng, 0.0), 0.08) for _ in range(3)],
                  segs=(18, 10), floor=-0.04, uv="box")
        for _ in range(rng.randint(2, 3)):
            n = rand_dir(rng, 0.3)
            n.z = max(n.z, 0.35)
            n.normalize()
            cut = max((v.co - Vector((0, 0, z0))).dot(n) for v in pc.bm.verts) * rng.uniform(0.86, 0.92)
            for v in pc.bm.verts:
                q = v.co - Vector((0, 0, z0))
                over_ = q.dot(n) - cut
                if over_ > 0:
                    v.co -= n * over_
        pc.bm.normal_update()
        BK._uv_frame(pc, Vector((1, 0, 0)), Vector((0, 1, 0)), Z, p.uv_off())
        return pc

    def surface_at(tree, x, y, ground=0.0):
        hit = tree.ray_cast(Vector((x, y, 5.0)), DOWN, 10.0)
        if hit[0] is None:
            return Vector((x, y, ground)), Z.copy(), False
        return hit[0], hit[1], True

    def starfish(p, slot, rock_tree, c, R, rng):
        """A fat five-armed star draped over the stone: a puffy slab (thick at the middle, rounded
        arms), its mid surface laid on the stone by ray casts and sunk 1.2 cm in, one or two arm
        tips curling up off it."""
        spin = rng.uniform(0, math.tau)
        curls = set(rng.sample(range(5), rng.randint(1, 2)))
        rc = R * 0.44           # a fat body: at 0.34 (v1) the arms read as thin petals

        def Rof(th):
            a = 0.5 + 0.5 * math.cos(5 * (th - spin))
            return rc + (R - rc) * a ** 1.2

        def arm_index(th):
            return int(round(((th - spin) % math.tau) / (math.tau / 5))) % 5

        def T(th, rho):
            return 0.085 - 0.045 * rho

        def M(th, rho):
            x, y = c.x + math.cos(th) * Rof(th) * rho, c.y + math.sin(th) * Rof(th) * rho
            q, _n, _ok = surface_at(rock_tree, x, y)
            lift = 0.04 * rho ** 3 if arm_index(th) in curls else 0.0
            return Vector((x, y, q.z + T(th, rho) / 2 - 0.012 + lift))
        return disc_slab(p, slot, M, T, 50, 4, Vector((1, 0, 0)), Vector((0, 1, 0)), rim_push=0.45)

    STAR_COLOURS = ("magenta", "ochre", "pink")

    def build_urchin_rock(p):
        r = p.rng
        mode = {1: "urchins", 2: "starfish", 3: "both"}.get(p.seed, r.choice(("urchins", "starfish", "both")))
        st = stone(p, r)
        tree = BVHTree.FromBMesh(st.bm)
        uslot = slot_of("polyp", "plum")
        placed = []

        def put_urchin(x, y, R, on_rock):
            q, n, ok = surface_at(tree, x, y) if on_rock else (Vector((x, y, 0.0)), Z.copy(), True)
            if not ok:
                return False
            c = q + n * (R * 0.55)
            if any((c - o).length < (R + ro) * 2.0 for o, ro in placed):
                return False
            urchin(p, uslot, c, n, R, r)
            placed.append((c, R))
            return True
        star_col = None
        if mode in ("starfish", "both"):
            star_col = STAR_COLOURS[r.randrange(3)]
            c = Vector((r.uniform(-0.05, 0.05), r.uniform(-0.03, 0.05), 0))
            starfish(p, slot_of("polyp", star_col), tree, c, r.uniform(0.18, 0.22), r)
            placed.append((surface_at(tree, c.x, c.y)[0], 0.2))
        want = {"urchins": r.randint(4, 5), "starfish": 0, "both": 3}[mode]
        tries = 0
        n = 0
        rx = max(v.co.x for v in st.bm.verts)
        while n < want and tries < 200:
            tries += 1
            R = r.uniform(0.08, 0.095)
            if mode == "urchins" and tries < 120:
                a, d = r.uniform(0, math.tau), r.uniform(0.0, rx * 0.7)
                ok = put_urchin(math.cos(a) * d, math.sin(a) * d, R, True)
            else:
                # at the foot of the stone, on the sand, toward the front half
                a = r.uniform(-0.3, math.pi + 0.3)
                d = rx + r.uniform(0.08, 0.16)
                ok = put_urchin(math.cos(a) * d, math.sin(a) * d * 0.9, R, False)
            n += 1 if ok else 0
        p.variant = f"{mode}: {n} urchins" + (f", {star_col} starfish" if star_col else "")

    BUILDERS = {"brain_coral": build_brain_coral, "branch_coral": build_branch_coral,
                "fan_coral": build_fan_coral, "table_coral": build_table_coral,
                "urchin_rock": build_urchin_rock}
    # Bevel per kind (width m, angle limit degrees) or None. Only the stone has creases worth a
    # bevel (its flat facets); every coral is a smooth organic shell, shaded smooth throughout.
    FINISH = {"urchin_rock": (0.012, 38.0)}
    SHARP = {"urchin_rock": 38.0}

    # ------------------------------------------------------------ sea grass (cards)

    def build_sea_grass(seed):
        """12 to 25 broad ribbon cards from a small patch, all bent by one baked current: each
        blade leaves the sand steeply, leans downstream more and more along its length and curls
        over at the tip. Upright blades wear the light tint, the ones laid over the dark one (the
        planting kit's two-tint rule, per position, never at random)."""
        rng = random.Random(f"lagoon-prop-seabed:sea_grass:{seed}")
        variant = "olive" if seed == 3 else "green"
        n = {1: 16, 2: 22, 3: 13}.get(seed, rng.randint(12, 25))
        tall = {1: 0.8, 2: 0.95, 3: 0.65}.get(seed, rng.uniform(0.5, 1.0))
        cur = rng.uniform(0, math.tau)                   # the current's heading
        cdir = Vector((math.cos(cur), math.sin(cur), 0))
        m = PL._Mesh()
        centre = Vector((0, 0, 0.12))
        patch = 0.12 + 0.01 * n
        blades, bases = [], []
        for k in range(n):
            # ⚠️ bases at least 4 cm apart: two blades from one spot started as the same card
            # (check_prop: one coplanar overlap at the foot on seed 2)
            for _try in range(40):
                a = rng.uniform(0, math.tau)
                d = patch * math.sqrt(rng.random())
                base = Vector((math.cos(a) * d, math.sin(a) * d, -0.04))
                if all((base - b).length > 0.04 for b in bases):
                    break
            bases.append(base)
            L = tall * rng.uniform(0.7, 1.12) / 0.92
            out = Vector((math.cos(a), math.sin(a), 0))
            lean = rng.uniform(0.5, 1.0)             # how far this blade gives to the current
            sway = rng.uniform(-0.35, 0.35)
            steps = 6
            pts = [base]
            for i in range(steps):
                t = (i + 0.5) / steps
                horiz = (out * 0.35 + cdir * (0.5 + lean * t * 1.6)).normalized()
                pitch = math.radians(86 - (40 + 30 * lean) * t ** 1.5)
                side = Vector((-horiz.y, horiz.x, 0)) * (0.25 * sway * math.sin(math.pi * t))
                dv = horiz * math.cos(pitch) + Z * math.sin(pitch) + side
                pts.append(pts[-1] + dv.normalized() * (L / steps))
            low = lean > 0.8
            # ⚠️ 11 to 14 cm cards (the drawing's strap fills 86 %): at 7.5 to 9.5 cm (v1) the
            # blades read as land grass, and from above, through water, as thin lines.
            blades.append((pts, 1 if low else 0, rng.uniform(0.11, 0.14)))
        allv = [q for b in blades for q in b[0]]
        cx = (min(q.x for q in allv) + max(q.x for q in allv)) / 2
        cy = (min(q.y for q in allv) + max(q.y for q in allv)) / 2
        shift = Vector((cx, cy, 0))
        for pts, slot, w in blades:
            PL._card(m, slot, [q - shift for q in pts], w, 0.22, centre, up_bias=0.6)
        lt, dk = SEAGRASS_TINTS[variant]
        mats = [PL._card_material(f"psb_seagrass_{variant}_light", "psb_seagrass_albedo.png", lt),
                PL._card_material(f"psb_seagrass_{variant}_dark", "psb_seagrass_albedo.png", dk)]
        me = m.build(f"sea_grass_{seed}", mats)
        return me, f"{n} blades, {variant}, {tall:.2f} m, current {math.degrees(cur):.0f} deg"

    # ------------------------------------------------------------ assembly

    def _tint(m, rgb):
        PS._tint(m, rgb)

    def _srgb_lin(c):
        return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4

    # ⚠️ The drawings are painted with their body tone at 0.91 to 0.93 (linear ~0.82), so the
    # multiply that lands the body ON the palette colour is colour / 0.82, capped at 1.
    BODY_LINEAR = 0.82
    BUMP = {"brain": 0.005, "polyp": 0.002, "lattice": 0.003}

    def material(slot):
        drawing, colour = slot.split(":")
        if drawing == "stone":
            name, texture, tint = "psb_stone", "rock_a", STONE_TINT
        else:
            name, texture = f"psb_{drawing}_{colour}", f"psb_{drawing}"
            hx = PALETTE[colour]
            tint = tuple(min(1.0, _srgb_lin(int(hx[i:i + 2], 16) / 255) / BODY_LINEAR) for i in (0, 2, 4))
        m = bpy.data.materials.get(name)
        if m is not None:
            return m
        m = bpy.data.materials.new(name)
        uv_material(m, texture)
        for nd in m.node_tree.nodes:
            if nd.type == "DISPLACEMENT" and drawing in BUMP:
                nd.inputs["Scale"].default_value = BUMP[drawing]
            if nd.type == "BSDF_PRINCIPLED":
                nd.inputs["Roughness"].default_value = 0.75
        _tint(m, tint)
        return m

    def _finish(kind, seed, me, variant, box, modifiers=True):
        col = bpy.data.collections.new(f"prop_{kind}_{seed}")
        root = bpy.data.objects.new(kind, None)
        root.empty_display_type, root.empty_display_size = "PLAIN_AXES", 0.5
        col.objects.link(root)
        root["prop_kind"], root["prop_seed"], root["prop_mount"] = kind, seed, "ground"
        root["prop_variant"] = variant
        root["prop_box"] = [round(v, 3) for v in box]
        ob = bpy.data.objects.new(me.name, me)
        ob.parent = root
        col.objects.link(ob)
        fin = FINISH.get(kind)
        if fin and modifiers:
            bev = ob.modifiers.new("Bevel", "BEVEL")
            bev.width, bev.segments, bev.limit_method = fin[0], 2, "WEIGHT"
            bev.harden_normals = True
            bev.use_clamp_overlap = True
        return col

    def build_prop(kind, seed=1):
        """Build one prop of `kind` (see KINDS) from `seed` and return its Collection, NOT linked to
        any scene: one mesh parented to one root empty named `kind` at the centre of its footprint
        on the sand, +Y the front."""
        if kind not in KINDS:
            raise ValueError(f"unknown prop kind {kind!r}; one of {KINDS}")
        _ensure_textures()
        if kind == "sea_grass":
            me, variant = build_sea_grass(seed)
            vs = [v.co for v in me.vertices]
            box = [min(v.x for v in vs), min(v.y for v in vs), min(v.z for v in vs),
                   max(v.x for v in vs), max(v.y for v in vs), max(v.z for v in vs)]
            return _finish(kind, seed, me, variant, box)
        p = Prop(kind, seed)
        BUILDERS[kind](p)
        bm = bmesh.new()
        uv = bm.loops.layers.uv.new("UVMap")
        for pc in p.pieces:
            vmap = {v: bm.verts.new(v.co) for v in pc.bm.verts}
            for f in pc.bm.faces:
                try:
                    nf = bm.faces.new([vmap[v] for v in f.verts])
                except ValueError:
                    continue
                nf.material_index = f.material_index
                for a, c in zip(f.loops, nf.loops):
                    c[uv].uv = a[pc.uv].uv
            pc.bm.free()
        xs = [v.co.x for v in bm.verts]
        ys = [v.co.y for v in bm.verts]
        bmesh.ops.translate(bm, vec=(-(min(xs) + max(xs)) / 2, -(min(ys) + max(ys)) / 2, 0), verts=bm.verts)
        bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4])
        bm.normal_update()
        lim = math.radians(SHARP.get(kind, 70.0))
        for f in bm.faces:
            f.smooth = True
        for e in bm.edges:
            e.smooth = not (len(e.link_faces) == 2 and e.link_faces[0].normal.angle(e.link_faces[1].normal, 0) > lim)
        if kind in FINISH:
            # ⚠️ Only the STONE's creases get the bevel (a weight, the modifier limited to it): an
            # angle-limited bevel over the whole prop also rounded every urchin spike's end and
            # took urchin_rock from 2.2k to 9.2k triangles (first check run).
            stone_idx = p.slots.index("stone:") if "stone:" in p.slots else -1
            bw = bm.edges.layers.float.get("bevel_weight_edge") or bm.edges.layers.float.new("bevel_weight_edge")
            for e in bm.edges:
                if (not e.smooth and len(e.link_faces) == 2
                        and all(f.material_index == stone_idx for f in e.link_faces)):
                    e[bw] = 1.0
        me = bpy.data.meshes.new(f"{kind}_{seed}")
        bm.to_mesh(me)
        box = [min(v.co.x for v in bm.verts), min(v.co.y for v in bm.verts), min(v.co.z for v in bm.verts),
               max(v.co.x for v in bm.verts), max(v.co.y for v in bm.verts), max(v.co.z for v in bm.verts)]
        bm.free()
        for slot in p.slots:
            me.materials.append(material(slot))
        return _finish(kind, seed, me, p.variant, box)

    # ------------------------------------------------------------ checks

    def check_prop(col):
        """Tri counts (base and bevelled), non-manifold edges (leaf cards excluded: they are open
        by design), COPLANAR overlaps and FLOATING shells, as lagoon_prop_shore.check_prop."""
        out = PS.check_prop(col)
        cards = 0
        for ob in col.objects:
            if ob.type == "MESH" and any(m and m.name.startswith("psb_seagrass") for m in ob.data.materials):
                cards += len(ob.data.polygons)
                bm = bmesh.new()
                bm.from_mesh(ob.data)
                out["nonmanifold"] -= sum(1 for e in bm.edges if not e.is_manifold)
                bm.free()
        if cards:
            out["card_faces"] = cards
        return out

    # ------------------------------------------------------------ preview

    def _plain_mat(name, colour, rough=0.85):
        m = bpy.data.materials.new(name)
        m.use_nodes = True
        bsdf = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
        bsdf.inputs["Base Color"].default_value = colour + (1,)
        bsdf.inputs["Roughness"].default_value = rough
        return m

    def _water_mat():
        """The brief's stand-in: a principled BSDF, colour (0.2, 0.8, 0.8), alpha 0.5, blended."""
        m = bpy.data.materials.new("psb_water_standin")
        m.use_nodes = True
        bsdf = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
        bsdf.inputs["Base Color"].default_value = (0.2, 0.8, 0.8, 1)
        bsdf.inputs["Roughness"].default_value = 0.08
        bsdf.inputs["Alpha"].default_value = 0.5
        if hasattr(m, "surface_render_method"):
            m.surface_render_method = "BLENDED"
        else:
            m.blend_method = "BLEND"
        return m

    def _preview_scene():
        scene = bpy.context.scene
        g = bpy.data.meshes.new("sand")
        bm = bmesh.new()
        bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=200)
        bm.to_mesh(g)
        bm.free()
        g.materials.append(_plain_mat("sand_pale", (0.78, 0.66, 0.46)))
        scene.collection.objects.link(bpy.data.objects.new("sand", g))
        ref = bpy.data.meshes.new("scale_ref_1m60")
        bm = bmesh.new()
        bmesh.ops.create_cone(bm, cap_ends=True, segments=24, radius1=0.25, radius2=0.25, depth=1.6)
        bmesh.ops.translate(bm, vec=(0, 0, 0.8), verts=bm.verts)
        bm.to_mesh(ref)
        bm.free()
        ref.materials.append(_plain_mat("scale_pink", (0.95, 0.30, 0.55), 0.6))
        o = bpy.data.objects.new("scale_ref_1m60", ref)
        o.location = (4.6, 0.8, -0.02)
        scene.collection.objects.link(o)
        sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
        sun.data.energy, sun.data.color = 4.2, (1.0, 0.9, 0.76)
        sun.data.angle = math.radians(3)
        sun.rotation_euler = (math.radians(45), 0, math.radians(-150))
        scene.collection.objects.link(sun)
        world = bpy.data.worlds.new("world")
        world.use_nodes = True
        world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.55, 0.6, 0.62, 1)
        world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.8
        scene.world = world
        cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
        cam.data.clip_end = 500
        scene.collection.objects.link(cam)
        scene.camera = cam
        scene.render.engine = "BLENDER_EEVEE"
        scene.render.resolution_x, scene.render.resolution_y = 1600, 900
        scene.view_settings.view_transform = "AgX"
        for look in ("AgX - Punchy", "Punchy"):
            try:
                scene.view_settings.look = look
                break
            except TypeError:
                continue
        return cam, sun, world

    # Kinds in rows along -Y, the three seeds side by side along X; fronts face +Y, the camera
    # stands on +Y.
    ROW_Y = {k: -2.7 * i for i, k in enumerate(KINDS)}
    SEED_X = {1: 2.3, 2: 0.0, 3: -2.3}          # the camera looks down -Y: +X is on the LEFT

    def _shoot(cam, pos, tgt, lens, path):
        if path.exists():
            raise SystemExit(f"[prop-seabed] {path} exists: never overwrite a render, bump --preview")
        cam.location = pos
        cam.data.lens = lens
        cam.rotation_euler = (Vector(tgt) - Vector(pos)).to_track_quat("-Z", "Y").to_euler()
        bpy.context.scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        print("[prop-seabed] preview", path)

    def main():
        argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
        version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
        only = argv[argv.index("--only") + 1].split(",") if "--only" in argv else None
        bpy.ops.wm.read_factory_settings(use_empty=True)
        top = bpy.data.collections.new("lagoon_props_seabed")
        bpy.context.scene.collection.children.link(top)
        for n, h, s in audit_palette():
            print(f"[prop-seabed] colour {n}: hue {h}, sat {s}")
        cols = {}
        for kind in KINDS:
            for seed in SEEDS:
                col = build_prop(kind, seed)
                top.children.link(col)
                c = check_prop(col)
                root = next(o for o in col.objects if o.parent is None)
                bx = root["prop_box"]
                print(f"[prop-seabed] {kind} {seed} ({root['prop_variant']}): "
                      f"{bx[3] - bx[0]:.2f} x {bx[4] - bx[1]:.2f} x {bx[5]:.2f} m, {c}")
                root.location = (SEED_X[seed], ROW_Y[kind], 0.0)
                cols[(kind, seed)] = col
        if not version:
            return
        LOGS.mkdir(parents=True, exist_ok=True)
        subprocess.run(["py", "-3", str(Path(__file__).resolve()), "--swatch", str(version)], check=False)
        cam, sun, world = _preview_scene()
        yc = sum(ROW_Y.values()) / len(ROW_Y)
        shots = []
        if not only or "lineup" in only:
            shots.append(("lineup", (0.0, 7.5, 9.5), (0.0, yc + 0.4, 0.0), 24, None))
        for kind in KINDS:
            if not only or kind in only or "close" in only:
                y = ROW_Y[kind]
                shots.append((f"close_{kind}", (0.0, y + 5.4, 2.4), (0.0, y, 0.3), 28, kind))
        for tag, pos, tgt, lens, keep in shots:
            for (k, _s), col in cols.items():
                col.hide_render = keep is not None and k != keep
            _shoot(cam, pos, tgt, lens, LOGS / f"{PREFIX}_{tag}_v{version}.png")
        for col in cols.values():
            col.hide_render = False
        if only and "underwater" not in only:
            return
        # ⚠️ The readability test: a sunset sun low in the west, and the brief's flat translucent
        # turquoise plane at 2 m and at 5 m over the sand, seen from 10 m above it at a shallow
        # angle. This is how the player meets these props; a coral that only reads dry fails.
        sun.data.energy, sun.data.color = 3.8, (1.0, 0.82, 0.64)
        sun.rotation_euler = (math.radians(62), 0, math.radians(-120))
        world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.62, 0.5, 0.52, 1)
        wm = bpy.data.meshes.new("water")
        bm = bmesh.new()
        bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=60)
        bm.to_mesh(wm)
        bm.free()
        wm.materials.append(_water_mat())
        water = bpy.data.objects.new("water_standin", wm)
        bpy.context.scene.collection.objects.link(water)
        for depth in (2.0, 5.0):
            water.location = (0, yc, depth)
            pos = (0.0, yc + 19.0, depth + 10.0)
            _shoot(cam, pos, (0.0, yc - 0.5, 0.0), 30, LOGS / f"{PREFIX}_underwater_d{int(depth)}_v{version}.png")


if __name__ == "__main__":
    if bpy is None:
        args = sys.argv[1:]
        if args and args[0] == "--paint":
            paint(args[1:] or None)
        elif args and args[0] == "--swatch":
            swatch_sheet(int(args[1]))
        else:
            # only the usage paragraph: the rest carries a warning sign a cp1252 console cannot print
            print(__doc__.split("\n\n")[0])
    else:
        main()
