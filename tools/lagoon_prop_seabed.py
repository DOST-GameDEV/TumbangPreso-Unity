"""Lagoon Court SEABED KIT: corals, sea grass and urchin rocks for the floor of the cove, models AND
the textures they wear.

  py -3 tools/lagoon_prop_seabed.py --paint                  # paint every psb_ texture
  py -3 tools/lagoon_prop_seabed.py --paint psb_brain        # paint the named ones
  blender -b --python tools/lagoon_prop_seabed.py -- --preview N

  blender -b --python tools/lagoon_prop_seabed.py -- --preview N --kinds reef_head,fan_coral --only close

With --preview N the Blender run renders into Logs/lagoon-blender/: psb_lineup_vN.png (every kind,
three seeds each, rows sized from the props' own boxes, on pale sand beside a 1.6 m pink scale
cylinder), psb_close_<kind>_vN.png (one close-up per kind, its three seeds side by side),
psb_swatch_vN.png (the four drawings flat), psb_underwater_d2_vN.png / psb_underwater_d5_vN.png
(the whole lineup seen from 10 m above a flat translucent turquoise stand-in water plane at 2 m and
5 m over the sand, low sunset sun: the readability test that matters, since nobody ever sees these
props dry; pieces over 2 m tall break the d2 surface) and psb_underwater_reef_vN.png (the reef
heads alone from 22 m away and 13 m up under 5 m of water, the court's view). --kinds limits the
build to the named kinds and --only to the named shots (lineup, close, underwater, or a kind). It
paints any missing psb_ texture first (through `py -3`, since Blender's Python has no PIL). An
existing render is never overwritten: bump N. Nothing is saved to a .blend; the cove script calls
build_prop and links it.

WHY THIS KIT EXISTS. OWNER, 2026-09-27, looking into the water of the cove: "it lacks corals and
plants". The water reference (ANGRY MESH, Stylized Water, docs/LAGOON_REWORK_GUIDE.md section 1)
shows a sandy, stony bottom through clear turquoise water, and a Sama-Bajau village stands over
exactly that kind of shallow reef flat: sea grass meadows, brain and staghorn coral heads, table
corals, sea fans and urchins on the stones.

  import lagoon_prop_seabed as PSB
  col = PSB.build_prop(kind, seed)      # a Collection, NOT linked; one root empty named by kind
  report = PSB.check_prop(col)          # tris, non-manifold, coplanar overlaps, floating shells

⚠️ REWORK, OWNER 2026-09-27, after the first placement: "corals need to be larger, more
organic/creative in shape and more dense. the seabed is empty rn". The first kit was 1 m domes, a
staghorn clump, one fan and one plate, authored small and up-scaled about 2x by the placement's
PLACE_SCALE. Every kind is now authored at its FINAL in-world size (PLACE_SCALE goes to 1), the
shapes are lobed, forked, wavy and grouped instead of single primitives, and five kinds are new,
the biggest being the reef_head, a whole crowded colony on a stone mound that fills the empty
floor on its own. The court camera sits 20 to 40 m away looking through water that goes deep by
about 3.6 m, so only big bold silhouettes and strong mid-light colours survive; that is the test
every kind below was iterated against (psb_underwater_*_v6 to v13).

KINDS, every one with its origin at the centre of its footprint on the seabed, z = 0 the sand, +Y
the front. Everything touching the sand sinks 3 to 7 cm into it, so a prop set on an uneven seabed
never shows a gap. Seeds 1, 2 and 3 are three different layouts (any other seed draws from the
same rules at random). Sizes are width x depth x height from prop_box, measured on v13.

  * "brain_coral"   colonies of MERGING LOBES (cluster: one shell pushed out to a main head, side
                    lobes that sag to the sand and smaller crown lobes, the creases between them
                    softened once): seed 1 one lopsided ochre boulder (2.5 x 2.4 x 1.2 m), seed 2
                    a wide seagreen cushion with a peach colony grown into it (3.1 x 1.9 x 0.9),
                    seed 3 three heads of falling size, orchid, peach and lime (2.6 x 2.3 x 1.4).
                    The meander grooves are PAINTED (psb_brain), projected straight down so on
                    the steep sides they run down the lobes.
  * "branch_coral"  a staghorn THICKET: a low rubble mound and 5 to 8 primary limbs (the outer ones
                    sprawling, the inner ones steep) that bend upward and FORK two or three times,
                    each child a fifth thinner, every last limb ending in a fat round knob. No limb
                    under 8 cm across. Seed 1 pink, three levels (1.6 x 1.5 x 1.2); seed 2 ochre,
                    wide, two levels (1.8 x 2.1 x 1.3); seed 3 a tall magenta colony grown into a
                    lower coral-red one (2.4 x 1.5 x 1.4).
  * "fan_coral"     sea fans in GROUPS, each a thick SECTOR blade (a hand fan growing out of a fat
                    stalk top, a domed crown, one slow S-wave across it and a gentle cup) with the
                    lattice PAINTED on (psb_lattice), never cut as holes. Seed 1 a 2.3 m magenta
                    fan with two smaller ones turned and rolled outward (3.4 x 0.8 x 2.3); seed 2
                    one ochre stalk forking into two big blades rolled apart, a young coral-red fan
                    in front (3.0 x 1.1 x 2.3); seed 3 a staggered row of four, pink and orchid
                    (4.0 x 0.8 x 2.0). The blades face +Y.
  * "table_coral"   Acropora tables: wide plates with a SCALLOPED, RUFFLED rim and a gentle upturn,
                    each tier on its own fat limb off one trunk that flares into the sand. Seed 1
                    one great seagreen plate 1 m up (3.4 x 3.5 x 1.2); seed 2 two ochre tiers (2.6 x
                    2.4 x 1.5); seed 3 three peach tiers (3.1 x 2.2 x 1.6).
  * "sea_grass"     a clump of 25 to 45 broad ribbon blades (two-sided alpha cards in the land
                    kit's method, lagoon_cove_planting._card, with their own drawing psb_seagrass),
                    all leaning with one baked current and curling over at the tip. Seed 1 32
                    blades (1.5 x 1.3 x 1.2), seed 2 44 blades (1.7 x 1.7 x 1.5), seed 3 26 blades,
                    the warmer olive variant (1.2 x 1.2 x 0.9).
  * "urchin_rock"   a rounded stone (the pillow-stone recipe, section 3 of the guide) with, seed 1,
                    seven chunky cartoon urchins on top (1.8 x 1.7 x 1.2); seed 2 one fat
                    five-armed starfish draped over it and two urchins (1.7 x 1.6 x 0.9); seed 3 a
                    starfish on top and four urchins at its foot (2.5 x 1.7 x 0.9).
  * "tube_sponge"   NEW. 3 to 7 fat OPEN tubes of mixed heights from one lumpy foot, leaning apart
                    so every mouth shows; each is a revolved profile up the outside, over a fat
                    round lip and down the inside to a floor, the inside in a darker partner
                    colour so the hollow reads from above. Seed 1 five orchid (2.0 x 2.0 x 1.6),
                    seed 2 three lime (0.9 x 1.5 x 1.3), seed 3 seven coral red (2.1 x 2.0 x 1.8).
  * "soft_coral"    NEW. A cartoon tree: a fat pale cream trunk splitting into limbs, each ending
                    in a coloured head of 8 to 10 long fat round-tipped fingers. Seed 1 pink, four
                    heads (1.5 x 1.4 x 1.5); seed 2 lime, three (1.0 x 0.8 x 1.1); seed 3 orchid,
                    five (1.5 x 1.5 x 1.7).
  * "giant_clam"    NEW. Two fluted cream shell halves, hinge down in the sand, slightly open,
                    their rims waving in round interlocking teeth, and a bright lumpy MANTLE
                    bulging out of the gape. Seed 1 lime mantle (1.3 x 1.0 x 0.6), seed 2 orchid
                    (1.0 x 0.8 x 0.5), seed 3 seagreen (1.4 x 1.1 x 0.7).
  * "anemone"       NEW. A fat squat column crowned by three rings of thick short round-tipped
                    tentacles, the outer ring lying out, the inner standing up (it can host a
                    clownfish later). Seed 1 pink on cream (1.0 x 0.9 x 0.6), seed 2 lime on plum
                    (0.7 x 0.7 x 0.4), seed 3 magenta on peach (1.2 x 1.1 x 0.7).
  * "reef_head"     NEW, THE HERO PIECE: a pile of merged boulders in the kit's stone, CROWDED with
                    the kinds above grown smaller and planted on it (tilted part-way to the slope,
                    sunk into it), then a filler pass of brain colonies, tube pairs, low plates
                    and small thickets on whatever stone a ray still reaches, then urchins. Seed 1
                    a three-tier peach table tower (5.2 x 4.7 x 2.7); seed 2 a ridge of four big
                    fans along the crest, thickets at both ends and a clam at the foot (6.7 x 3.8
                    x 2.6); seed 3 a magenta staghorn crown beside a stand of tall orchid tubes,
                    a clam at the foot (4.9 x 5.0 x 2.6).

THE ROOT EMPTY carries prop_kind, prop_seed, prop_mount ("ground"), prop_variant and prop_box (xmin,
ymin, zmin, xmax, ymax, zmax, in the root's frame), as the sibling kits do.

⚠️ CHUNKY AND ORGANIC, NEVER FIDDLY (LAGOON_REWORK_GUIDE.md section 2, owner 2026-09-27: "we're
going for a stylized semi-cartoony environment style"). Every member here is fat, tapered, bent
and ends round; the fine detail a real reef has (polyps, the fan's mesh, the grooves) lives in the
painted textures only. Triangles (v13): most props 0.6k to 5.5k, the busiest thicket and soft coral
about 6.4k, each reef_head held under about 19.8k by its filler budget (REEF_TRIS).

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
pink, magenta, orchid, warm yellow-ochre, a PINK peach, sea-green and (2026-09-27) lime, coral red
and cream, each a mid-light value so the water's cyan does not drown it; plum, moss and wine are
the dark accents (urchins, the insides of the sponge tubes). ROLE HUES (Art_Direction.md section 1): nothing near offence orange
#f87020 (hue 24) or defence blue #0080e8 (hue 207). audit_palette() measures every colour at import
and refuses one within 15 degrees of hue 24 or 25 degrees of hue 207 (a colour under 20 per cent
saturation is a grey and exempt). This is why the peach is a pink peach (hue about 7) and the
starfish is magenta or ochre, never the orange a real starfish often is.

UVs: one map "UVMap", WORLD SCALE, 1 UV unit = 2 m. Tubes (branches, stalks, trunks, fingers,
tentacles, sponge tubes) V along, U around; the lobed clusters, domes and urchin balls are
projected straight DOWN (x, y), so on a dome's flanks the drawing stretches downhill; the fan
blades and clam shells are projected on their own plane; the table plates straight down; the
stones and the reef mound are box projected. The sea grass cards are 0..1 across and base to tip.
Pieces planted on a reef head keep the UVs they were built with.

⚠️ NO TWO SURFACES SHARE A PLANE (KANTO_DESIGN_GUIDE.md section 2). The domes' flat undersides are
clamped at different depths under the sand, every member gets its own random ring phase and every
fork child starts inside its parent a fifth thinner; check_prop counts coplanar overlaps and proves
every shell is in a chain of intersections down to the sand. v13: no floating shell anywhere; a
few coplanar pairs remain where tapered limbs cross inside a fork or a head (branch 1 and 2: 1
each, soft coral 2 and 3: 1 each, sea grass 2: 3 card pairs, reef heads 2 to 3); the ones inspected
sit at fork joints, inside a finger head or between two leaf cards. Not yet driven to zero.
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

KINDS = ("brain_coral", "branch_coral", "fan_coral", "table_coral", "sea_grass", "urchin_rock",
         "tube_sponge", "soft_coral", "giant_clam", "anemone", "reef_head")
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
    # 2026-09-27 rework, for the new kinds. LIME (hue ~72) is the brightest thing on the reef:
    # a yellow-green the water's cyan cannot swallow (the sea-green had to be yellowed for the
    # same reason). CORAL RED sits at hue ~1, 23 degrees clear of offence orange, a warmer red
    # than the pink. CREAM (hue ~43, saturation 0.21) is the pale trunk and shell colour.
    "lime":     "b5d04a",
    "coralred": "e8605e",
    "cream":    "eee0bc",
    # darker partners for the inside of the sponge tubes: the hollow must read darker than the
    # rim or an open tube reads as a capped post from above
    "moss":     "6b8a30",    # hue ~81
    "wine":     "8a3048",    # hue ~344
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
        # ⚠️ a random ring phase per member: with one fixed phase, two limbs leaving the same
        # fork at the same radius had ring faces parallel within 4 mm (check_prop: coplanar)
        ph = p.rng.uniform(0, math.tau)
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

    # ------------------------------------------------------------ growth helpers
    # ⚠️ Every kind is built by a PARAMETERISED grower (a colony, a thicket, a fan group, a tube
    # cluster...) that stands on z = 0 at the origin. The single kinds call them at their full
    # size; the reef_head calls the same growers smaller and PLANTS them on its stone mound, so a
    # reef head is made of the same corals the player sees alone on the sand, never a second look.

    def up_frame(n):
        """The rotation that stands a piece built along +Z up along `n`."""
        return Z.rotation_difference(n.normalized()).to_matrix().to_4x4()

    def plant(p, grow, at, up, sink, spin):
        """Run `grow()` (which adds pieces standing on z = 0 at the origin), then move every piece
        it added onto `at`, stood along `up`, turned by `spin` about its own axis and sunk `sink`
        metres into whatever it stands on, so it is in contact all round (check_prop's floating
        test needs every shell to intersect its way down to the sand)."""
        i0 = len(p.pieces)
        grow()
        M = (Matrix.Translation(Vector(at) - up.normalized() * sink) @ up_frame(up)
             @ Matrix.Rotation(spin, 4, "Z"))
        for pc in p.pieces[i0:]:
            xform(pc, M)

    def cluster(p, slot, centre, lobes, segs=(30, 16), floor=None, uv="down", soften=2):
        """ONE closed shell that is a cluster of merging round LOBES: a UV sphere around `centre`
        whose every vertex is pushed out to the farthest lobe its ray from the centre meets
        (lobes are (offset from centre, radius)), then softened. Where two lobes meet the shell
        folds into a soft crease, which is what makes a brain coral read as a colony of heads
        and a stone mound as a pile of boulders, never a primitive dome.
        ⚠️ One shell instead of overlapping balls on purpose: overlapping balls leave hidden
        inner skins and hard intersection lines; this shell has neither. The first lobe must
        contain the centre, so every ray meets something."""
        pc = BK.Piece()
        bmesh.ops.create_uvsphere(pc.bm, u_segments=segs[0], v_segments=segs[1], radius=1.0)
        C = Vector(centre)
        L = [(Vector(c), R) for c, R in lobes]
        rad = {}
        for v in pc.bm.verts:
            d = v.co.normalized()
            best = 0.05
            for c, R in L:
                b = d.dot(c)
                disc = b * b - (c.length_squared - R * R)
                if disc >= 0.0:
                    best = max(best, b + math.sqrt(disc))
            rad[v] = best
        # softening: two passes of a half-weight neighbour average round the creases into soft
        # folds (unsoftened they are V-shaped and shade as a cut line)
        for _ in range(soften):
            new = {}
            for v in pc.bm.verts:
                nb = [e.other_vert(v) for e in v.link_edges]
                new[v] = 0.5 * rad[v] + 0.5 * sum(rad[u] for u in nb) / len(nb)
            rad = new
        for v in pc.bm.verts:
            q = v.co.normalized() * rad[v] + C
            if floor is not None and q.z < floor:
                q.z = floor
            v.co = q
        pc.bm.normal_update()
        if uv == "down":
            uv_down(pc, p.uv_off())
        else:
            BK._uv_frame(pc, Vector((1, 0, 0)), Vector((0, 1, 0)), Z, p.uv_off())
        return p.add(slot, pc)

    def facet_cuts(pc, rng, centre, count):
        """Two to four broad flat facets planed off the upper flanks: the pillow-stone recipe
        (guide section 3), so a stone reads as rock and not as a coral dome."""
        C = Vector(centre)
        for _ in range(count):
            n = rand_dir(rng, 0.3)
            n.z = max(n.z, 0.35)
            n.normalize()
            cut = max((v.co - C).dot(n) for v in pc.bm.verts) * rng.uniform(0.86, 0.92)
            for v in pc.bm.verts:
                over_ = (v.co - C).dot(n) - cut
                if over_ > 0:
                    v.co -= n * over_
        pc.bm.normal_update()

    # ------------------------------------------------------------ brain coral

    BRAIN_COLOURS = ("ochre", "peach", "seagreen", "orchid", "lime")

    def brain_colony(p, col, R, H, rng, n_side=5, n_crown=2, at=(0.0, 0.0), floor=-0.04, segs=(30, 16)):
        """One brain coral colony, R the footprint radius, H the height: a main head, `n_side`
        side lobes round it that sag to the sand at different heights, and `n_crown` smaller
        lobes on top, all merged into one lumpy shell (cluster). Research (Diploria and
        Platygyra colonies; stylized reef kits): a real brain coral is rarely a sphere, it is a
        boulder of fused heads, lopsided and lower on one side."""
        C = Vector((at[0], at[1], H * 0.3))
        # ⚠️ the main head is no wider than half the footprint, so the side lobes stand OUT of it
        # as their own heads: at 0.62 of the footprint (v6 to v9) they sank into it and the
        # colony read as one lump
        rm = max(min(H * 0.58, R * 0.5), H * 0.4)
        lobes = [(Vector((rng.uniform(-0.08, 0.08) * R, rng.uniform(-0.08, 0.08) * R, H - rm - C.z)), rm)]
        ph = rng.uniform(0, math.tau)
        for i in range(n_side):
            a = ph + math.tau * i / n_side + rng.uniform(-0.35, 0.35)
            rs = min(R * rng.uniform(0.36, 0.46), H * 0.78)
            d = R - rs
            top = H * rng.uniform(0.45, 0.82)
            zc = max(-0.12, min(top - rs, rs * 0.7))
            lobes.append((Vector((math.cos(a) * d, math.sin(a) * d, zc - C.z)), rs))
        for _ in range(n_crown):
            a = rng.uniform(0, math.tau)
            rc = rm * rng.uniform(0.42, 0.58)
            dd = rm * rng.uniform(0.35, 0.6)
            zc = H * rng.uniform(0.92, 1.02) - rc
            lobes.append((Vector((math.cos(a) * dd, math.sin(a) * dd, zc - C.z)), rc))
        # soften once only: twice (v6) melted the lobes into one lump
        return cluster(p, slot_of("brain", col), C, lobes, segs=segs, floor=floor, soften=1)

    def build_brain_coral(p):
        r = p.rng
        s = p.seed if p.seed in SEEDS else r.choice(SEEDS)
        if s == 1:
            # one big lopsided boulder of fused heads
            brain_colony(p, "ochre", r.uniform(1.2, 1.3), r.uniform(1.1, 1.25), r, n_side=6, n_crown=3)
            p.variant = "boulder, ochre"
        elif s == 2:
            # a wide low cushion with a second, smaller colony of another colour grown into it
            brain_colony(p, "seagreen", 1.0, 0.85, r, n_side=6, n_crown=2, at=(-0.5, 0.0))
            brain_colony(p, "peach", 0.72, 0.72, r, n_side=4, n_crown=2, at=(0.95, 0.3), floor=-0.052)
            p.variant = "cushion pair, seagreen and peach"
        else:
            # three colonies of falling size grown into one another round a triangle: a tall orchid
            # head, a peach one and a small lime one at their foot
            brain_colony(p, "orchid", 0.95, 1.4, r, n_side=5, n_crown=3)
            brain_colony(p, "peach", 0.7, 0.85, r, n_side=4, n_crown=2, at=(1.05, 0.35), floor=-0.05,
                         segs=(24, 13))
            brain_colony(p, "lime", 0.48, 0.5, r, n_side=3, n_crown=1, at=(0.35, 1.0), floor=-0.058,
                         segs=(20, 11))
            p.variant = "three heads, orchid, peach and lime"

    # ------------------------------------------------------------ branch coral

    BRANCH_COLOURS = ("pink", "ochre", "magenta", "coralred")

    def _branch(p, slot, start, d, L, r, depth, levels, rng, sides, knob):
        """One limb of a staghorn: a bent tapered member that bends upward as it grows, then
        FORKS into two (sometimes three) thinner limbs, down to `levels`; every last limb ends in
        a fat round KNOB a little wider than the limb (the growing tip a stylized staghorn wears)."""
        steps = 4
        pts = [Vector(start)]
        dd = Vector(d).normalized()
        for _ in range(steps):
            dd = (dd + Z * 0.16 + Vector((rng.uniform(-0.14, 0.14), rng.uniform(-0.14, 0.14), 0))).normalized()
            pts.append(pts[-1] + dd * (L / steps))
        pts = BK._catmull(pts, per=2)
        r1 = r * 0.84
        member(p, slot, pts, r, r1, sides=sides, step=0.12)
        last = depth >= levels - 1 or (depth >= 1 and rng.random() < 0.3)
        if last:
            ball(p, slot, pts[-1] - dd * r1 * 0.2, (r1 * knob,) * 3, (), segs=(8, 6))
            return 1
        tips = 0
        nk = 3 if rng.random() < 0.22 else 2
        e1, e2 = BK._perp(dd)
        spin = rng.uniform(0, math.tau)
        for c in range(nk):
            a = spin + math.tau * c / nk + rng.uniform(-0.4, 0.4)
            axis = e1 * math.cos(a) + e2 * math.sin(a)
            cd = Matrix.Rotation(math.radians(rng.uniform(24, 42)), 3, axis) @ dd
            cd = (cd + Z * 0.2).normalized()
            # each child starts a little over half a radius back INSIDE the parent, a fifth thinner than it: a
            # child as fat as its parent crossed the parent's skin almost parallel (check_prop: coplanar)
            tips += _branch(p, slot, pts[-1] - dd * r1 * (0.6 + 0.15 * c), cd, L * rng.uniform(0.62, 0.78),
                            max(0.042, r1 * 0.8), depth + 1, levels, rng, max(6, sides - 1), knob)
        return tips

    def thicket(p, col, R, H, rng, n=6, levels=3, r0=0.11, sides=6, knob=1.14):
        """A staghorn THICKET, R the footprint radius, H roughly the height: a low lumpy mound of
        rubble and `n` primary limbs leaving it at all angles (the outer ones low and sprawling,
        the inner ones steep), each forking up to `levels` deep. Returns the tip count."""
        slot = slot_of("polyp", col)
        lobes = [(Vector((0, 0, 0.0)), R * 0.3)]
        for _ in range(3):
            a = rng.uniform(0, math.tau)
            lobes.append((Vector((math.cos(a) * R * 0.2, math.sin(a) * R * 0.2, -0.04)), R * rng.uniform(0.2, 0.26)))
        cluster(p, slot, (0, 0, 0.0), lobes, segs=(14, 8), floor=-0.04)
        phase = rng.uniform(0, math.tau)
        tips = 0
        for k in range(n):
            az = phase + math.tau * k / n + rng.uniform(-0.25, 0.25)
            rad0 = R * rng.uniform(0.05, 0.22)
            start = Vector((math.cos(az) * rad0, math.sin(az) * rad0, 0.04))
            outer = k % 3 != 0
            pitch = math.radians(rng.uniform(24, 40) if outer else rng.uniform(58, 75))
            d = Vector((math.cos(az) * math.cos(pitch), math.sin(az) * math.cos(pitch), math.sin(pitch)))
            # the outer limbs' reach follows the footprint, the inner ones' the height
            L = max(H * 0.4, R * 0.62) * rng.uniform(0.9, 1.1) if outer else H * rng.uniform(0.44, 0.54)
            tips += _branch(p, slot, start, d, L, r0 * rng.uniform(0.92, 1.08), 0, levels, rng, sides, knob)
        return tips

    def build_branch_coral(p):
        r = p.rng
        s = p.seed if p.seed in SEEDS else r.choice(SEEDS)
        if s == 1:
            t = thicket(p, "pink", 1.0, 1.3, r, n=6, levels=3)
            p.variant = f"thicket, pink, {t} tips"
        elif s == 2:
            # wide and sprawling, two levels only, more primaries
            t = thicket(p, "ochre", 1.3, 1.05, r, n=8, levels=2, r0=0.12)
            p.variant = f"sprawl, ochre, {t} tips"
        else:
            # two colonies grown into one another, a tall magenta and a lower coral red
            i0 = len(p.pieces)
            t = thicket(p, "magenta", 0.8, 1.5, r, n=5, levels=3)
            for pc in p.pieces[i0:]:
                xform(pc, Matrix.Translation((-0.45, 0.0, 0.0)))
            i0 = len(p.pieces)
            t += thicket(p, "coralred", 0.7, 1.0, r, n=4, levels=2)
            for pc in p.pieces[i0:]:
                xform(pc, Matrix.Translation((0.75, 0.25, 0.0)))
            p.variant = f"two colonies, magenta and coralred, {t} tips"

    # ------------------------------------------------------------ fan coral

    FAN_COLOURS = ("magenta", "orchid", "ochre", "coralred", "pink")

    def fan_blade(p, slot, apex, H, half_angle, lean, turn, roll=0.0, thick=0.075):
        """One fan blade: a rounded SECTOR, its apex (the narrow foot) on `apex`, H tall from it,
        opening `half_angle` each side of vertical, in its own plane x-z facing +Y; then leant
        back by `lean`, rolled sideways in its own plane by `roll` and turned by `turn` about Z,
        all round the apex.
        ⚠️ WHY A SECTOR. v2 was an ellipse pinched at the bottom: its lower flanks swelled back
        down to the sand, so the fan sat on the ground like a cushion, the stalk a stub in front.
        A real sea fan (and every stylized one) is a hand fan: it grows OUT of the stalk top as a
        wedge that broadens into a round crown, clear of the sand all round.
        ⚠️ WAVY AND CUPPED, scaled with the blade (2026-09-27 rework): a flat slab read as a sign
        board from the court; the blade now ripples (a wave about 9 per cent of its height,
        growing toward the crown), cups toward the viewer and has a scalloped crown."""
        r = p.rng
        ph1, ph2, phw, phs = (r.uniform(0, math.tau) for _ in range(4))
        # one slow S-shaped wave across the crown (a wavelength about the fan's width): two waves
        # (v10, v11) bent the crown's middle back and the silhouette dipped into a heart
        lam = r.uniform(1.0, 1.3) * H
        # ⚠️ a gentle cup only: at 0.12 of the height (v10) the flanks curled so far forward that from
        # the court's high camera the crown's middle sat lower than its corners and read as a heart
        wave, cup = 0.09 * H, 0.06 * H
        W = 2 * H * math.sin(half_angle)
        nsc = r.choice((3, 4, 5))

        def inside(x, z):
            if z <= 0.0:
                return False
            ang = math.atan2(x, z)
            if abs(ang) > half_angle:
                return False
            u = ang / half_angle
            # ⚠️ A DOMED crown, fullest at the middle, with only slow waves along it: v7's notched
            # scallops and v8's three-cycle wave both put a dip at the middle whenever the phase
            # fell there, and the fan read as a Valentine heart
            rmax = H * (0.93 + 0.1 * math.cos(math.pi * u / 2) + 0.04 * math.sin(math.pi * u + ph1)
                        + 0.02 * math.sin(2 * math.pi * u + ph2))
            return math.hypot(x, z) <= rmax
        C = (0.0, H * 0.5)
        nt = 34
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
            y = (wave * math.sin(math.tau * x / lam + phw) * (z / H) ** 1.3
                 + cup * (x / (W / 2)) ** 2)
            return Vector((x, y, z))

        def T(th, rho):
            return thick * (1.0 - 0.35 * rho)
        pc = disc_slab(p, slot, M, T, nt, 5, Vector((1, 0, 0)), Vector((0, 0, 1)))
        foot = M(math.pi, 1.0).z
        xform(pc, Matrix.Translation(apex) @ Matrix.Rotation(turn, 4, "Z") @ Matrix.Rotation(roll, 4, "Y")
              @ Matrix.Rotation(-lean, 4, "X") @ Matrix.Translation((0, 0, -foot - 0.04)))
        return pc

    def fan(p, col, x, y, H, half_deg, turn, roll, rng, hs=0.25, stalk_col=None):
        """One fan on its own fat stalk from the sand at (x, y); the blade's foot sinks into the
        stalk's round top so stalk and blade read as one growth."""
        sslot = slot_of("polyp", stalk_col or col)
        stalk = [Vector((x, y, -0.05)), Vector((x + 0.01, y, hs * 0.5)), Vector((x, y + 0.01, hs))]
        member(p, sslot, stalk, 0.1, 0.085, sides=8, tip=True, base_flare=(0.17, 0.12))
        fan_blade(p, slot_of("lattice", col), Vector((x, y + 0.01, hs)), H - hs, math.radians(half_deg),
                  math.radians(rng.uniform(3, 7)), turn, roll=roll)

    def fan_group(p, specs, rng):
        """Fans in a GROUP (real gorgonians grow in stands on one ridge): each spec is
        (x, y, height, half-angle degrees, turn, roll, colour)."""
        for x, y, H, half, turn, roll, col in specs:
            fan(p, col, x, y, H, half, turn, roll, rng, hs=0.2 + 0.08 * H)

    def build_fan_coral(p):
        r = p.rng
        s = p.seed if p.seed in SEEDS else r.choice(SEEDS)
        if s == 1:
            # a tall magenta fan with two smaller ones either side, turned and rolled outward
            fan_group(p, [(0.0, 0.0, r.uniform(2.3, 2.5), 40, r.uniform(-0.1, 0.1), 0.0, "magenta"),
                          (-0.95, 0.35, r.uniform(1.45, 1.6), 36, 0.45, -0.32, "orchid"),
                          (0.95, 0.3, r.uniform(1.25, 1.4), 38, -0.5, 0.3, "magenta")], r)
            p.variant = "stand of three, magenta and orchid"
        elif s == 2:
            # the vee: one stalk forking into two limbs, a big fan on each rolled apart, and a
            # young fan in front
            sslot = slot_of("polyp", "ochre")
            hs = 0.32
            member(p, sslot, [Vector((0, 0, -0.05)), Vector((0.0, 0.01, hs * 0.5)), Vector((0.0, 0.0, hs * 0.8))],
                   0.1, 0.085, tip=True, base_flare=(0.16, 0.1))
            for sgn in (-1, 1):
                top = Vector((sgn * 0.3, 0.0, hs + 0.16))
                member(p, sslot, [Vector((0, 0, hs * 0.6)), Vector((sgn * 0.14, 0, hs + 0.04)), top], 0.075, 0.066)
                fan_blade(p, slot_of("lattice", "ochre"), top, r.uniform(1.75, 1.9), math.radians(r.uniform(30, 34)),
                          math.radians(r.uniform(4, 7)), sgn * r.uniform(0.1, 0.18),
                          roll=sgn * math.radians(r.uniform(22, 27)))
            fan(p, "coralred", r.uniform(-0.25, 0.25), 0.75, 1.0, 40, r.uniform(-0.3, 0.3), 0.0, r, hs=0.2)
            p.variant = "vee with a young fan, ochre and coralred"
        else:
            # a staggered row of four of mixed heights and two colours along one ridge
            specs = []
            for i, x in enumerate((-1.35, -0.45, 0.45, 1.35)):
                H = (1.4, 2.2, 1.85, 1.2)[i] * r.uniform(0.95, 1.05)
                specs.append((x, r.uniform(-0.25, 0.25) + (0.3 if i % 2 else 0.0), H, r.uniform(32, 40),
                              r.uniform(-0.35, 0.35), (i - 1.5) * 0.12, ("pink", "orchid")[i % 2]))
            fan_group(p, specs, r)
            p.variant = "row of four, pink and orchid"

    # ------------------------------------------------------------ table coral

    TABLE_COLOURS = ("seagreen", "ochre", "peach", "lime")

    def table_plate(p, slot, centre, R0, thick=None, nt=64, nr=5):
        """A wide plate: lobed outline with a SCALLOPED rim (round lobes with small notches, the
        growing edge of an Acropora table), a gentle upturn and a RUFFLE at the rim that follows
        the scallops, thick at the middle and thinner at the edge. One closed disc_slab shell."""
        r = p.rng
        ph = [r.uniform(0, math.tau) for _ in range(5)]
        nw = r.choice((3, 4))
        k = r.choice((5, 6, 7))
        # ⚠️ the wave scales with the plate: a fixed 3 to 5 cm crumpled the small second plate (v2)
        amp = R0 * r.uniform(0.04, 0.06)
        thick = thick or (0.07 + 0.035 * R0, 0.05 + 0.015 * R0)

        def Rof(th):
            return R0 * (1 + 0.07 * math.sin(3 * th + ph[0]) + 0.04 * math.sin(5 * th + ph[1])
                         + 0.06 * (abs(math.sin(k * th + ph[2])) ** 0.5 - 0.76))

        def M(th, rho):
            R = Rof(th) * rho
            z = (0.07 * R0 * rho * rho + amp * rho * rho * math.sin(nw * th + ph[3])
                 + 0.03 * R0 * rho ** 4 * math.sin(2 * k * th + 2 * ph[2]))
            return Vector((centre[0] + math.cos(th) * R, centre[1] + math.sin(th) * R, centre[2] + z))

        def T(th, rho):
            return thick[0] + (thick[1] - thick[0]) * rho
        return disc_slab(p, slot, M, T, nt, nr, Vector((1, 0, 0)), Vector((0, 1, 0)))

    def table_stack(p, col, tiers, rng, nt=64, trunk_r=0.17):
        """A table coral of one or more tiers on ONE fat trunk that flares into the sand: each
        tier (x, y, height, radius) gets its own fat limb curving off the trunk to its plate
        centre (a tier straight above the trunk just continues it)."""
        slot = slot_of("polyp", col)
        h0 = min(t[2] for t in tiers)
        member(p, slot, [Vector((0, 0, -0.05)), Vector((0.02, -0.01, h0 * 0.35)), Vector((0, 0, h0 * 0.6))],
               trunk_r, trunk_r * 0.86, tip=True, base_flare=(trunk_r * 1.8, 0.14))
        for i, (x, y, h, R0) in enumerate(tiers):
            top = Vector((x, y, h))
            mid = Vector((x * 0.35, y * 0.35, (h0 * 0.6 + h) * 0.5))
            # each limb leaves the trunk at its own height (from one point their first rings coincided)
            h_out = h0 * (0.3 + 0.12 * i)
            # ⚠️ the limb ends INSIDE the plate, at its mid surface: ending it 3 cm up put its flat
            # cap within 5 mm of the plate's top skin (check_prop: one coplanar overlap on seed 1)
            member(p, slot, [Vector((0, 0, h_out)), mid, top], trunk_r * 0.8, trunk_r * 0.62, tip=False)
            table_plate(p, slot, top, R0, nt=nt)

    def build_table_coral(p):
        r = p.rng
        s = p.seed if p.seed in SEEDS else r.choice(SEEDS)
        if s == 1:
            table_stack(p, "seagreen", [(r.uniform(-0.1, 0.1), r.uniform(-0.1, 0.1), 0.95, r.uniform(1.55, 1.65))], r,
                        trunk_r=0.2)
            p.variant = "one great plate, seagreen"
        elif s == 2:
            table_stack(p, "ochre", [(0.3, -0.1, 0.6, 1.15), (-0.45, 0.25, 1.35, 0.72)], r)
            p.variant = "two tiers, ochre"
        else:
            table_stack(p, "peach", [(0.6, 0.0, 0.5, 0.95), (-0.6, 0.25, 0.95, 0.8), (0.05, -0.3, 1.5, 0.62)], r)
            p.variant = "three tiers, peach"

    # ------------------------------------------------------------ urchins, stones, starfish

    def urchin(p, slot, centre, up, R, rng):
        """A cartoon urchin: a fat ball and 6 to 8 fat, blunt, tapered spikes spread over the
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

    def stone(p, rng, R):
        """A seabed stone R across the long half: the pillow-stone recipe (guide section 3), a
        lumpy sphere with two or three broad flat facets on its upper flanks, buried a third in
        the sand."""
        radii = (R * rng.uniform(1.0, 1.15), R * rng.uniform(0.85, 1.0), R * rng.uniform(0.72, 0.85))
        z0 = radii[2] * 0.35
        pc = ball(p, "stone:", (0, 0, z0), radii, [(rand_dir(rng, 0.0), 0.08) for _ in range(3)],
                  segs=(22, 12), floor=-0.04, uv="box")
        facet_cuts(pc, rng, (0, 0, z0), rng.randint(2, 3))
        BK._uv_frame(pc, Vector((1, 0, 0)), Vector((0, 1, 0)), Z, p.uv_off())
        return pc

    def surface_at(tree, x, y, ground=0.0):
        hit = tree.ray_cast(Vector((x, y, 12.0)), DOWN, 20.0)
        if hit[0] is None:
            return Vector((x, y, ground)), Z.copy(), False
        return hit[0], hit[1], True

    def starfish(p, slot, rock_tree, c, R, rng):
        """A fat five-armed star draped over the stone: a puffy slab (thick at the middle, rounded
        arms), its mid surface laid on the stone by ray casts and sunk into it, one or two arm
        tips curling up off it."""
        spin = rng.uniform(0, math.tau)
        curls = set(rng.sample(range(5), rng.randint(1, 2)))
        rc = R * 0.44           # a fat body: at 0.34 (v1) the arms read as thin petals
        th0 = 0.1 + 0.06 * R    # thickness grows with the star

        def Rof(th):
            a = 0.5 + 0.5 * math.cos(5 * (th - spin))
            return rc + (R - rc) * a ** 1.2

        def arm_index(th):
            return int(round(((th - spin) % math.tau) / (math.tau / 5))) % 5

        def T(th, rho):
            return th0 - 0.5 * th0 * rho

        def M(th, rho):
            x, y = c.x + math.cos(th) * Rof(th) * rho, c.y + math.sin(th) * Rof(th) * rho
            q, _n, _ok = surface_at(rock_tree, x, y)
            lift = 0.1 * R * rho ** 3 if arm_index(th) in curls else 0.0
            return Vector((x, y, q.z + T(th, rho) / 2 - 0.02 + lift))
        return disc_slab(p, slot, M, T, 50, 4, Vector((1, 0, 0)), Vector((0, 1, 0)), rim_push=0.45)

    STAR_COLOURS = ("magenta", "ochre", "pink", "coralred")

    def build_urchin_rock(p):
        r = p.rng
        s = p.seed if p.seed in SEEDS else r.choice(SEEDS)
        mode = {1: "urchins", 2: "starfish", 3: "both"}[s]
        R = {1: r.uniform(0.72, 0.8), 2: r.uniform(0.62, 0.7), 3: r.uniform(0.74, 0.8)}[s]
        st = stone(p, r, R)
        tree = BVHTree.FromBMesh(st.bm)
        uslot = slot_of("polyp", "plum")
        placed = []

        def put_urchin(x, y, Ru, on_rock):
            q, n, ok = surface_at(tree, x, y) if on_rock else (Vector((x, y, 0.0)), Z.copy(), True)
            if not ok:
                return False
            c = q + n * (Ru * 0.55)
            if any((c - o).length < (Ru + ro) * 1.6 for o, ro in placed):
                return False
            urchin(p, uslot, c, n, Ru, r)
            placed.append((c, Ru))
            return True
        star_col = None
        if mode in ("starfish", "both"):
            star_col = STAR_COLOURS[r.randrange(len(STAR_COLOURS))]
            c = Vector((r.uniform(-0.1, 0.1), r.uniform(-0.05, 0.1), 0))
            starfish(p, slot_of("polyp", star_col), tree, c, R * r.uniform(0.5, 0.56), r)
            placed.append((surface_at(tree, c.x, c.y)[0], R * 0.5))
        want = {"urchins": r.randint(6, 7), "starfish": 2, "both": 4}[mode]
        tries = 0
        n = 0
        rx = max(v.co.x for v in st.bm.verts)
        while n < want and tries < 300:
            tries += 1
            Ru = r.uniform(0.13, 0.17)
            if mode != "both" and tries < 200:
                a, d = r.uniform(0, math.tau), r.uniform(0.0, rx * 0.75)
                ok = put_urchin(math.cos(a) * d, math.sin(a) * d, Ru, True)
            else:
                # at the foot of the stone, on the sand, toward the front half
                a = r.uniform(-0.3, math.pi + 0.3)
                d = rx + r.uniform(0.12, 0.25)
                ok = put_urchin(math.cos(a) * d, math.sin(a) * d * 0.9, Ru, False)
            n += 1 if ok else 0
        p.variant = f"{mode}: {n} urchins" + (f", {star_col} starfish" if star_col else "")

    # ------------------------------------------------------------ tube sponge

    SPONGE_COLOURS = {"orchid": "plum", "magenta": "plum", "lime": "moss", "coralred": "wine", "ochre": "moss"}

    def tube(p, col, base, lean, H, ro, rng, sides=12):
        """One sponge TUBE, open at the top: a revolved profile along a gently bowed spine, up the
        outside, over a fat round LIP and down the inside to a floor well below the rim, all one
        closed shell. The inside wears a darker colour of the same hue, which is what makes the
        hollow read from above, through water, at 30 m (a hole the same colour as the wall reads
        as a flat cap). The wall is thick (28 per cent of the radius, at least 5 cm) and the
        cross-section a little irregular, a soft vase: a real Callyspongia tube is never a pipe."""
        wall = max(0.05, ro * 0.28)
        B = Vector(base)
        lv = Vector((lean.x, lean.y, 0.0))
        ctrl = [B + Vector((0, 0, -0.07)), B + Vector((0, 0, H * 0.33)) + lv * 0.12,
                B + Vector((0, 0, H * 0.67)) + lv * 0.5, B + Vector((0, 0, H)) + lv]
        P = BK._catmull(ctrl, per=4)
        cum = [0.0]
        for a, b in zip(P, P[1:]):
            cum.append(cum[-1] + (b - a).length)
        L = cum[-1]

        def at(s):
            if s >= L:
                T = (P[-1] - P[-2]).normalized()
                return P[-1] + T * (s - L)
            s = max(0.0, s)
            for i in range(len(P) - 1):
                if cum[i + 1] >= s:
                    seg = cum[i + 1] - cum[i]
                    return P[i].lerp(P[i + 1], 0.0 if seg < 1e-9 else (s - cum[i]) / seg)
            return P[-1]

        def frame(s):
            T = (at(s + 0.02) - at(s - 0.02)).normalized()
            e1 = Vector((1, 0, 0)) - T * T.x
            e1.normalize()
            return e1, T.cross(e1).normalized()
        ph1, ph2 = rng.uniform(0, math.tau), rng.uniform(0, math.tau)

        def rad(t):
            return ro * (1.14 - 0.3 * t + 0.28 * t ** 2.2)

        def ring(s, rr):
            c = at(s)
            e1, e2 = frame(s)
            return [c + (e1 * math.cos(a) + e2 * math.sin(a)) * rr * (1 + 0.06 * math.sin(2 * a + ph1)
                                                                      + 0.04 * math.sin(3 * a + ph2))
                    for a in (math.tau * k / sides for k in range(sides))]
        prof = []                                      # (s, radius, inside?)
        n_out = max(4, int(L / 0.12))
        for i in range(n_out + 1):
            s = L * i / n_out
            prof.append((s, rad(s / L), False))
        rc = rad(1.0) - wall / 2
        lr = wall * 0.62
        for deg in (30, 60, 90, 120, 150):
            a = math.radians(deg)
            prof.append((L + math.sin(a) * lr, rc + math.cos(a) * lr, deg > 90))
        depth = min(0.55 * L, 0.6)
        n_in = max(3, int(depth / 0.12))
        for i in range(n_in + 1):
            s = L - depth * i / n_in
            prof.append((s, max(0.03, rad(s / L) - wall), True))
        rings, uvs = [], []
        v = 0.0
        prev = None
        for s, rr, _ins in prof:
            rg = ring(s, rr)
            if prev is not None:
                v += (rg[0] - prev[0]).length
            prev = rg
            rings.append(rg)
            arc = math.tau * rr / sides
            uvs.append([(k * arc, v) for k in range(sides + 1)])
        pc = p.add(slot_of("polyp", col), BK.loft(rings, uvs, off=p.uv_off()))
        # the inside: every band whose lower ring is past the top of the lip, and the floor cap
        in_slot = slot_of("polyp", SPONGE_COLOURS[col])
        if in_slot not in p.slots:
            p.slots.append(in_slot)
        idx = p.slots.index(in_slot)
        first_in = next(j for j, q in enumerate(prof) if q[2])
        nq = (len(rings) - 1) * sides
        pc.bm.faces.ensure_lookup_table()
        for i, f in enumerate(pc.bm.faces):
            if (i < nq and i // sides >= first_in - 1) or (i >= nq and f.calc_center_median().z > B.z + 0.05):
                f.material_index = idx
        return pc

    def tube_cluster(p, col, n, Hmax, rng, spread=0.3, sides=12):
        """3 to 7 tubes of mixed heights rising from one low lumpy foot, leaning outward so their
        mouths open apart (a clump seen from above shows every hollow)."""
        cluster(p, slot_of("polyp", col), (0, 0, 0), [(Vector((0, 0, 0)), spread * 0.9 + 0.1)]
                + [(Vector((rng.uniform(-1, 1) * spread * 0.5, rng.uniform(-1, 1) * spread * 0.5, -0.05)),
                    spread * 0.7) for _ in range(2)], segs=(16, 8), floor=-0.04)
        placed = []
        ph = rng.uniform(0, math.tau)
        for i in range(n):
            H = Hmax * (1.0 if i == 0 else rng.uniform(0.5, 0.9))
            ro = rng.uniform(0.15, 0.21) * (0.75 + 0.25 * H / Hmax)
            if i == 0:
                x, y = rng.uniform(-0.05, 0.05), rng.uniform(-0.05, 0.05)
            else:
                for _ in range(60):
                    a = ph + math.tau * i / max(1, n - 1) + rng.uniform(-0.4, 0.4)
                    d = spread * rng.uniform(0.9, 1.4) + ro
                    x, y = math.cos(a) * d, math.sin(a) * d
                    if all(math.hypot(x - qx, y - qy) > (ro + qr) * 1.05 for qx, qy, qr in placed):
                        break
            out = Vector((x, y, 0.0))
            out = out.normalized() if out.length > 1e-6 else Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), 0)).normalized()
            lean = out * H * math.tan(math.radians(rng.uniform(6, 16) if i else rng.uniform(2, 6)))
            tube(p, col, (x, y, 0.0), lean, H, ro, rng, sides=sides)
            placed.append((x, y, ro))

    def build_tube_sponge(p):
        r = p.rng
        s = p.seed if p.seed in SEEDS else r.choice(SEEDS)
        col, n, H = {1: ("orchid", 5, 1.6), 2: ("lime", 3, 1.2), 3: ("coralred", 7, 1.75)}[s]
        tube_cluster(p, col, n, H * r.uniform(0.97, 1.03), r, spread=0.22 + 0.03 * n)
        p.variant = f"{n} tubes, {col}"

    # ------------------------------------------------------------ soft coral

    SOFT_COLOURS = ("pink", "lime", "orchid", "coralred")

    def soft_tree(p, col, H, rng, n_heads=4, trunk_col="cream", fingers=(8, 10), segs=(14, 8)):
        """A soft coral read as a cartoon TREE: a fat pale trunk (the translucent stalk of a
        Dendronephthya) that splits into fat limbs, each ending in a lobed HEAD crowded with fat
        round-tipped fingers. The heads carry the colour, the trunk stays pale, so the silhouette
        reads as a bush on a stem."""
        tslot, hslot = slot_of("polyp", trunk_col), slot_of("polyp", col)
        ht = H * 0.36
        lean = Vector((rng.uniform(-0.08, 0.08), rng.uniform(-0.08, 0.08), 0))
        top = Vector((0, 0, ht)) + lean
        member(p, tslot, [Vector((0, 0, -0.05)), Vector((0, 0, ht * 0.5)) + lean * 0.4, top], 0.17, 0.14,
               tip=True, base_flare=(0.26, 0.14))
        ph = rng.uniform(0, math.tau)
        for i in range(n_heads):
            a = ph + math.tau * i / n_heads + rng.uniform(-0.3, 0.3)
            up = i == 0
            reach = H * (0.12 if up else rng.uniform(0.28, 0.36))
            hz = H * (0.72 if up else rng.uniform(0.5, 0.64))
            hc = top + Vector((math.cos(a) * reach, math.sin(a) * reach, hz - ht))
            mid = top.lerp(hc, 0.5) + Vector((0, 0, 0.08))
            member(p, tslot, [top - Vector((0, 0, 0.06)), mid, hc], 0.11, 0.085, tip=True)
            Rh = H * (0.2 if up else rng.uniform(0.15, 0.19))
            # ⚠️ The head is a small core bristling with LONG fat fingers, spread over the upper
            # hemisphere by a golden spiral: v6 had a big ball with short stubs and read as a
            # ball with warts (a pompom, a mushroom), never a bush.
            core = Rh * 0.6
            cluster(p, hslot, hc, [(Vector((0, 0, 0)), core), (rand_dir(rng, 0.2) * core * 0.4, core * 0.8)],
                    segs=segs)
            outw = Vector((math.cos(a), math.sin(a), 0.0)) * (0.0 if up else 0.45)
            nf = rng.randint(*fingers)
            golden = math.pi * (3 - math.sqrt(5))
            spin = rng.uniform(0, math.tau)
            for k in range(nf):
                zz = 1 - 1.15 * (k + 0.5) / nf          # the top and the upper flanks only
                rr = math.sqrt(max(0.0, 1 - zz * zz))
                d = Vector((math.cos(golden * k + spin) * rr, math.sin(golden * k + spin) * rr, zz))
                d = (d + outw * 0.6).normalized()
                st = hc + d * core * rng.uniform(0.3, 0.5)
                Lf = Rh * rng.uniform(0.75, 1.0)
                rf = max(0.05, Rh * 0.27)
                pts = curve(st, math.atan2(d.y, d.x), math.asin(max(-1.0, min(1.0, d.z))),
                            max(math.asin(max(-1.0, min(1.0, d.z))), math.radians(55)), Lf, 3, bend=1.0)
                member(p, hslot, pts, rf, rf * 0.92, sides=6, step=0.09)

    def build_soft_coral(p):
        r = p.rng
        s = p.seed if p.seed in SEEDS else r.choice(SEEDS)
        col, H, heads = {1: ("pink", 1.6, 4), 2: ("lime", 1.15, 3), 3: ("orchid", 1.8, 5)}[s]
        soft_tree(p, col, H * r.uniform(0.97, 1.03), r, n_heads=heads)
        p.variant = f"{heads} heads, {col}"

    # ------------------------------------------------------------ giant clam

    CLAM_MANTLES = ("lime", "orchid", "seagreen")

    def giant_clam(p, mantle, Lc, rng, shell="cream", gape=0.1):
        """A giant clam (Tridacna) sitting hinge-down in the sand, slightly open: two fluted shell
        halves whose rims ZIG-ZAG (the flutes run from the hinge to the gape and each one pokes
        up as a round tooth, the two halves' teeth interlocking), and a bright lumpy MANTLE
        bulging out of the gap. Lc is the length. The mantle is the colour; the shell stays cream
        so the mantle reads as the thing inside."""
        Rx, Ry, Rz = Lc / 2, Lc * 0.3, Lc * 0.34
        zc = Rz * 0.3
        # ⚠️ three or four broad flutes and 72 rim samples: five flutes on 48 samples (v6) left
        # three samples a tooth and the rim read as torn paper, spiky, not a wavy shell
        nrib = rng.choice((3, 4))
        ph = rng.uniform(-0.2, 0.2)
        sslot = slot_of("polyp", shell)
        for sgn in (-1, 1):
            off = 0.0 if sgn > 0 else math.pi

            def M(th, rho, sgn=sgn, off=off):
                a, b = rho * math.cos(th), rho * math.sin(th)
                phi = b * math.radians(80)
                w = math.sqrt(max(0.0, 1 - 0.92 * a * a))
                rib = 1 + 0.07 * math.cos(nrib * math.pi * a + off + ph)
                top = max(0.0, (b - 0.4) / 0.6)
                zig = Lc * 0.05 * math.cos(nrib * math.pi * a + off + ph) * top * top
                # ⚠️ a fifth of the gape stays open at the hinge: closed there, the two halves' back
                # skins ran within 4 mm of each other (check_prop: coplanar)
                y = sgn * (Ry * w * math.cos(phi) * rib + gape * (0.6 + 0.4 * b))
                z = zc + Rz * w * math.sin(phi) * (1 + 0.04 * rib) + zig
                return Vector((a * Rx, y, z))

            def T(th, rho):
                return 0.085 - 0.02 * rho
            disc_slab(p, sslot, M, T, 72, 5, Vector((1, 0, 0)), Vector((0, 0, 1)), rim_push=0.55)
        # the mantle fills the gape and bulges well above the rim: it is the colour, the thing
        # a player sees from above
        lumps = [(rand_dir(rng, 0.3), 0.1) for _ in range(5)]
        ball(p, slot_of("polyp", mantle), (0, 0, zc + Rz * 0.62), (Rx * 0.86, gape + Ry * 0.5, Rz * 0.5),
             lumps, segs=(22, 10))

    def build_giant_clam(p):
        r = p.rng
        s = p.seed if p.seed in SEEDS else r.choice(SEEDS)
        Lc, mantle = {1: (1.2, "lime"), 2: (0.9, "orchid"), 3: (1.3, "seagreen")}[s]
        giant_clam(p, mantle, Lc * r.uniform(0.97, 1.03), r, gape=0.09 + 0.02 * s)
        p.variant = f"{mantle} mantle"

    # ------------------------------------------------------------ anemone

    def anemone(p, column, tent, D, rng, dense=1.0):
        """A cartoon anemone D across: a fat squat squishy COLUMN (waisted, flaring to its top) and
        an oral pad crowned by three rings of thick, short, round-tipped TENTACLES, the outer ring
        lying out and down, the inner ones standing up. Tentacles never thinner than 8 cm."""
        cslot, tslot = slot_of("polyp", column), slot_of("polyp", tent)
        Rc, Hc = D * 0.26, D * 0.4
        lean = Vector((rng.uniform(-0.04, 0.04), rng.uniform(-0.04, 0.04), 0))
        top = Vector((0, 0, Hc)) + lean
        member(p, cslot, [Vector((0, 0, -0.05)), Vector((0, 0, Hc * 0.5)) + lean * 0.5, top],
               Rc * 1.05, Rc * 1.2, sides=14, tip=False, base_flare=(Rc * 1.3, 0.12), step=0.08)
        ball(p, cslot, top, (Rc * 1.3, Rc * 1.3, Rc * 0.38), (), segs=(18, 8))
        rt = max(0.04, D * 0.05)
        for ring_i, (count, rr, p0, p1) in enumerate(((11, 1.18, -10, 25), (9, 0.82, 25, 60), (6, 0.42, 55, 82))):
            ph = rng.uniform(0, math.tau)
            count = max(4, int(round(count * dense)))
            for k in range(count):
                a = ph + math.tau * k / count + rng.uniform(-0.12, 0.12)
                st = top + Vector((math.cos(a) * Rc * rr, math.sin(a) * Rc * rr, Rc * 0.18))
                Lt = D * rng.uniform(0.2, 0.28) * (1.0 if ring_i < 2 else 0.8)
                pts = curve(st, a, math.radians(p0 + rng.uniform(-6, 6)), math.radians(p1 + rng.uniform(-6, 6)),
                            Lt, 3, wander=0.1, rng=rng)
                member(p, tslot, pts, rt, rt * 0.85, sides=6, step=0.07)

    def build_anemone(p):
        r = p.rng
        s = p.seed if p.seed in SEEDS else r.choice(SEEDS)
        column, tent, D = {1: ("cream", "pink", 0.9), 2: ("plum", "lime", 0.7), 3: ("peach", "magenta", 1.05)}[s]
        anemone(p, column, tent, D * r.uniform(0.97, 1.03), r)
        p.variant = f"{tent} on {column}"

    # ------------------------------------------------------------ reef head

    def reef_mound(p, rng, W, D, Hm, nl, segs=(44, 22)):
        """The reef head's stone: a pile of merged boulders (cluster) W x D and Hm tall, with a
        few flat facets planed off, in the kit's stone. Returns its piece."""
        C = Vector((0, 0, Hm * 0.3))
        rc = Hm * 1.0
        lobes = [(Vector((0, 0, Hm - rc - C.z)), rc)]
        ph = rng.uniform(0, math.tau)
        for i in range(nl):
            a = ph + math.tau * i / nl + rng.uniform(-0.25, 0.25)
            rl = min(W, D) * rng.uniform(0.2, 0.27)
            x, y = math.cos(a) * (W / 2 - rl), math.sin(a) * (D / 2 - rl)
            zc = Hm * rng.uniform(0.35, 0.8) - rl
            lobes.append((Vector((x, y, zc - C.z)), rl))
        pc = cluster(p, "stone:", C, lobes, segs=segs, floor=-0.05, uv="box", soften=2)
        facet_cuts(pc, rng, C, rng.randint(2, 4))
        BK._uv_frame(pc, Vector((1, 0, 0)), Vector((0, 1, 0)), Z, p.uv_off())
        return pc

    def _tris(p):
        return sum(len(f.verts) - 2 for pc in p.pieces for f in pc.bm.faces)

    def _tree_of(pieces):
        bm = bmesh.new()
        for pc in pieces:
            vm = {v: bm.verts.new(v.co) for v in pc.bm.verts}
            for f in pc.bm.faces:
                try:
                    bm.faces.new([vm[v] for v in f.verts])
                except ValueError:
                    pass
        bm.normal_update()           # new faces carry no normal until asked: every hit read as a wall
        tree = BVHTree.FromBMesh(bm)
        bm.free()
        return tree

    REEF_TRIS = 17600                # the filler stops here; its last piece and the urchins add about 2k

    def build_reef_head(p):
        """THE HERO PIECE: a whole colony on a stone mound, CROWDED with the kit's own corals grown
        smaller and planted on it (see plant). Each seed has one memorable top: seed 1 a
        three-tier table tower, seed 2 a ridge of big fans, seed 3 a staghorn crown beside a
        stand of tall tubes. A plan entry is (what, x, y, footprint radius, sink, tilt toward the
        slope, spin or None for random).
        ⚠️ DENSE (owner, 2026-09-27: "the seabed is empty rn"). The v6 heads were a bare stone
        with a few pieces on it and read as a rock. After the plan, a FILLER pass grows brain
        colonies (the cheapest big coral, about 500 triangles) on every patch of open stone a ray
        still reaches, until the stone is mostly covered or the triangle budget is spent, then
        the urchins go on what stone is left."""
        r = p.rng
        s = p.seed if p.seed in SEEDS else r.choice(SEEDS)
        W, D, Hm, nl = {1: (5.0, 4.6, 1.25, 6), 2: (6.6, 3.4, 1.0, 7), 3: (4.6, 4.3, 1.45, 6)}[s]
        mound = reef_mound(p, r, W, D, Hm, nl, segs=(34, 16))
        tree = BVHTree.FromBMesh(mound.bm)
        B = lambda col, R, H, **kw: (lambda: brain_colony(p, col, R, H, r, segs=(22, 12), **kw))
        TH = lambda col, R, H, **kw: (lambda: thicket(p, col, R, H, r, sides=6, **kw))
        FN = lambda specs: (lambda: fan_group(p, specs, r))
        TB = lambda col, tiers: (lambda: table_stack(p, col, tiers, r, nt=36, trunk_r=0.14))
        TS = lambda col, n, H: (lambda: tube_cluster(p, col, n, H, r, spread=0.16 + 0.03 * n, sides=10))
        SC = lambda col, H, n: (lambda: soft_tree(p, col, H, r, n_heads=n, fingers=(6, 8), segs=(10, 6)))
        CL = lambda m, Lc: (lambda: giant_clam(p, m, Lc, r))
        AN = lambda c, t_, D_: (lambda: anemone(p, c, t_, D_, r, dense=0.6))
        if s == 1:
            plan = [(TB("peach", [(0.3, 0.0, 0.5, 1.1), (-0.35, 0.25, 1.0, 0.85), (0.05, -0.2, 1.5, 0.58)]),
                     0.1, -0.1, 1.2, 0.15, 0.0, None),
                    (FN([(0.0, 0.0, 1.5, 36, 0.0, 0.0, "magenta"), (0.65, 0.2, 1.05, 36, -0.4, 0.25, "orchid")]),
                     -1.3, -1.25, 0.7, 0.1, 0.2, 0.0),
                    (TS("orchid", 4, 1.1), 1.75, -0.9, 0.55, 0.12, 0.35, None),
                    (SC("pink", 1.05, 3), -0.6, 1.65, 0.5, 0.1, 0.3, None),
                    (TH("coralred", 0.6, 0.8, n=4, levels=2), -2.0, 0.1, 0.55, 0.1, 0.4, None),
                    (B("ochre", 0.85, 0.75), -1.35, 0.95, 0.8, 0.14, 0.5, None),
                    (B("seagreen", 0.75, 0.65), 1.4, 1.0, 0.7, 0.14, 0.5, None),
                    (AN("cream", "lime", 0.75), 2.3, 0.3, 0.4, 0.06, 0.3, None)]
            urchins, top = 5, "table tower"
        elif s == 2:
            plan = [(FN([(0.0, 0.0, 2.0, 36, 0.05, 0.0, "magenta")]), -0.7, 0.05, 0.6, 0.1, 0.15, r.uniform(-0.3, 0.3)),
                    (FN([(0.0, 0.0, 1.7, 36, -0.1, 0.1, "ochre")]), 0.65, -0.1, 0.55, 0.1, 0.15, r.uniform(-0.3, 0.3)),
                    (FN([(0.0, 0.0, 1.45, 34, 0.2, -0.1, "orchid")]), -1.95, 0.1, 0.5, 0.1, 0.2, r.uniform(-0.3, 0.3)),
                    (FN([(0.0, 0.0, 1.3, 34, -0.25, 0.1, "coralred")]), 1.9, 0.0, 0.5, 0.1, 0.2, r.uniform(-0.3, 0.3)),
                    (TH("pink", 0.75, 0.9, n=5, levels=2), -2.75, 0.3, 0.7, 0.1, 0.4, None),
                    (TH("ochre", 0.65, 0.8, n=4, levels=2), 2.8, -0.2, 0.6, 0.1, 0.4, None),
                    (TS("lime", 3, 0.95), 2.2, -1.05, 0.45, 0.12, 0.35, None),
                    (AN("peach", "magenta", 0.8), -0.1, 1.35, 0.4, 0.06, 0.3, None),
                    (CL("orchid", 0.9), -2.9, -1.35, 0.45, 0.08, 0.0, 0.3)]
            urchins, top = 4, "fan ridge"
        else:
            plan = [(TH("magenta", 1.1, 1.35, n=4, levels=3, r0=0.115), 0.0, 0.1, 1.1, 0.15, 0.0, None),
                    (TS("orchid", 5, 1.5), 1.45, -1.05, 0.7, 0.14, 0.35, None),
                    (SC("coralred", 1.15, 3), -1.15, 1.25, 0.55, 0.1, 0.3, None),
                    (TB("seagreen", [(0.0, 0.0, 0.6, 0.85)]), 1.55, 0.95, 0.8, 0.12, 0.2, None),
                    (B("lime", 0.8, 0.7), -1.55, -0.6, 0.75, 0.14, 0.5, None),
                    (CL("seagreen", 1.0), -0.5, -2.4, 0.5, 0.08, 0.0, 0.0),
                    (AN("cream", "pink", 0.75), 2.2, 0.1, 0.4, 0.06, 0.3, None)]
            urchins, top = 6, "staghorn crown"
        placed = []
        for grow, x, y, rad, sink, tilt, spin in plan:
            q, n, ok = surface_at(tree, x, y)
            up = Z.lerp(n, tilt).normalized() if ok else Z.copy()
            plant(p, grow, q, up, sink, r.uniform(0, math.tau) if spin is None else spin)
            placed.append((x, y, rad))
        # the FILLER: wherever the stone is still open, big pieces first. Mostly brain colonies
        # (cheap and big), with the odd pair of tubes, a low plate or a small thicket between them
        # so the fill does not read as one repeated lump.
        fill_cols = ("ochre", "peach", "seagreen", "orchid", "lime", "pink")
        fill_kinds = ("brain", "brain", "tubes", "brain", "plate", "brain", "thicket")
        n_fill, tries = 0, 0
        all_tree = _tree_of(p.pieces)
        while tries < 400 and _tris(p) < REEF_TRIS:
            tries += 1
            a, d = r.uniform(0, math.tau), math.sqrt(r.uniform(0.02, 1.0)) * 0.95
            x, y = math.cos(a) * d * W / 2, math.sin(a) * d * D / 2
            R = r.uniform(0.7, 0.9) if tries < 150 else r.uniform(0.45, 0.65)
            if any(math.hypot(x - px, y - py) < (R + pr) * 0.62 for px, py, pr in placed):
                continue
            q, nrm, ok = surface_at(all_tree, x, y)
            q2, _n2, ok2 = surface_at(tree, x, y)
            if not ok or not ok2 or (q - q2).length > 0.02 or nrm.z < 0.25:
                continue
            col = fill_cols[n_fill % len(fill_cols)]
            what = fill_kinds[n_fill % len(fill_kinds)]
            if what == "tubes":
                grow = TS(("orchid", "magenta", "lime", "coralred")[n_fill % 4], 2, R * 1.4)
            elif what == "plate":
                grow = TB(("seagreen", "ochre", "lime", "peach")[n_fill % 4], [(0.0, 0.0, R * 0.45, R)])
            elif what == "thicket":
                grow = TH(("pink", "coralred", "ochre")[n_fill % 3], R * 0.8, R, n=3, levels=2)
            else:
                grow = (lambda col=col, R=R: brain_colony(p, col, R, R * r.uniform(0.75, 0.95), r, n_side=4,
                                                          n_crown=2, segs=(16, 9)))
            plant(p, grow, q, Z.lerp(nrm, 0.5).normalized(), 0.13, r.uniform(0, math.tau))
            placed.append((x, y, R))
            n_fill += 1
            all_tree = _tree_of(p.pieces)
        # urchins last, on the open stone between the colonies (a ray that meets coral first is
        # refused)
        uslot = slot_of("polyp", "plum")
        n_u, tries = 0, 0
        while n_u < urchins and tries < 400:
            tries += 1
            a, d = r.uniform(0, math.tau), r.uniform(0.3, 0.97)
            x, y = math.cos(a) * d * W / 2, math.sin(a) * d * D / 2
            q, nrm, ok = surface_at(all_tree, x, y)
            q2, _n2, ok2 = surface_at(tree, x, y)
            if not ok or not ok2 or (q - q2).length > 0.02 or nrm.z < 0.3:
                continue
            Ru = r.uniform(0.13, 0.16)
            urchin(p, uslot, q + nrm * Ru * 0.5, nrm, Ru, r)
            n_u += 1
        p.variant = f"{top}, {len(plan)} colonies, {n_fill} filler pieces, {n_u} urchins"

    BUILDERS = {"brain_coral": build_brain_coral, "branch_coral": build_branch_coral,
                "fan_coral": build_fan_coral, "table_coral": build_table_coral,
                "urchin_rock": build_urchin_rock, "tube_sponge": build_tube_sponge,
                "soft_coral": build_soft_coral, "giant_clam": build_giant_clam,
                "anemone": build_anemone, "reef_head": build_reef_head}
    # Bevel per kind (width m, angle limit degrees) or None. Only the stone has creases worth a
    # bevel (its flat facets); every coral is a smooth organic shell, shaded smooth throughout.
    FINISH = {"urchin_rock": (0.012, 38.0), "reef_head": (0.02, 38.0)}
    # ⚠️ The sharp-edge angle applies to the STONE's faces only: applied to the whole prop it also
    # flat-shaded every 6 and 7 sided coral limb (their facets meet at 51 to 60 degrees).
    SHARP = {"urchin_rock": 38.0, "reef_head": 38.0}

    # ------------------------------------------------------------ sea grass (cards)

    def build_sea_grass(seed):
        """12 to 25 broad ribbon cards from a small patch, all bent by one baked current: each
        blade leaves the sand steeply, leans downstream more and more along its length and curls
        over at the tip. Upright blades wear the light tint, the ones laid over the dark one (the
        planting kit's two-tint rule, per position, never at random)."""
        rng = random.Random(f"lagoon-prop-seabed:sea_grass:{seed}")
        variant = "olive" if seed == 3 else "green"
        n = {1: 32, 2: 44, 3: 26}.get(seed, rng.randint(25, 45))
        tall = {1: 1.2, 2: 1.5, 3: 0.95}.get(seed, rng.uniform(0.9, 1.6))
        cur = rng.uniform(0, math.tau)                   # the current's heading
        cdir = Vector((math.cos(cur), math.sin(cur), 0))
        m = PL._Mesh()
        centre = Vector((0, 0, 0.12))
        patch = 0.2 + 0.009 * n
        blades, bases = [], []
        for k in range(n):
            # ⚠️ bases at least 4 cm apart: two blades from one spot started as the same card
            # (check_prop: one coplanar overlap at the foot on seed 2)
            for _try in range(40):
                a = rng.uniform(0, math.tau)
                d = patch * math.sqrt(rng.random())
                base = Vector((math.cos(a) * d, math.sin(a) * d, -0.04))
                if all((base - b).length > 0.05 for b in bases):
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
            blades.append((pts, 1 if low else 0, rng.uniform(0.15, 0.19)))
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
        stone_idx = p.slots.index("stone:") if "stone:" in p.slots else -1
        for f in bm.faces:
            f.smooth = True
        for e in bm.edges:
            crease = len(e.link_faces) == 2 and e.link_faces[0].normal.angle(e.link_faces[1].normal, 0) > lim
            if kind in SHARP:
                crease = crease and all(f.material_index == stone_idx for f in e.link_faces)
            e.smooth = not crease
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

    # Kinds in rows along -Y, the three seeds side by side along X (seed 1 on the camera's LEFT,
    # which is +X since the camera looks down -Y); fronts face +Y, the camera stands on +Y. Rows
    # are sized from the props' own boxes, now that they range from a 0.7 m anemone to a 7 m
    # reef head.
    ROW_GAP, SEED_GAP = 1.2, 1.0

    def _shoot(cam, pos, tgt, lens, path):
        if path.exists():
            raise SystemExit(f"[prop-seabed] {path} exists: never overwrite a render, bump --preview")
        cam.location = pos
        cam.data.lens = lens
        cam.rotation_euler = (Vector(tgt) - Vector(pos)).to_track_quat("-Z", "Y").to_euler()
        bpy.context.scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        print("[prop-seabed] preview", path)

    def _layout(cols, kinds):
        rows, y = {}, 0.0
        for kind in kinds:
            boxes = {}
            for seed in SEEDS:
                root = next(o for o in cols[(kind, seed)].objects if o.parent is None)
                boxes[seed] = (root, list(root["prop_box"]))
            depth = max(b[4] - b[1] for _r, b in boxes.values())
            width = sum(b[3] - b[0] for _r, b in boxes.values()) + SEED_GAP * 2
            height = max(b[5] for _r, b in boxes.values())
            yc = y - depth / 2
            x = width / 2
            for seed in SEEDS:
                root, b = boxes[seed]
                w = b[3] - b[0]
                root.location = (x - w / 2 - (b[0] + b[3]) / 2, yc - (b[1] + b[4]) / 2, 0.0)
                x -= w + SEED_GAP
            rows[kind] = (yc, width, depth, height)
            y -= depth + ROW_GAP
        return rows, y + ROW_GAP

    def main():
        argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
        version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
        only = argv[argv.index("--only") + 1].split(",") if "--only" in argv else None
        kinds = argv[argv.index("--kinds") + 1].split(",") if "--kinds" in argv else KINDS
        bpy.ops.wm.read_factory_settings(use_empty=True)
        top = bpy.data.collections.new("lagoon_props_seabed")
        bpy.context.scene.collection.children.link(top)
        for n, h, s in audit_palette():
            print(f"[prop-seabed] colour {n}: hue {h}, sat {s}")
        cols = {}
        for kind in KINDS:
            if kind not in kinds:
                continue
            for seed in SEEDS:
                col = build_prop(kind, seed)
                top.children.link(col)
                c = check_prop(col)
                root = next(o for o in col.objects if o.parent is None)
                bx = root["prop_box"]
                c.pop("box", None)
                print(f"[prop-seabed] {kind} {seed} ({root['prop_variant']}): "
                      f"{bx[3] - bx[0]:.2f} x {bx[4] - bx[1]:.2f} x {bx[5]:.2f} m, {c}")
                cols[(kind, seed)] = col
        if not version:
            return
        LOGS.mkdir(parents=True, exist_ok=True)
        subprocess.run(["py", "-3", str(Path(__file__).resolve()), "--swatch", str(version)], check=False)
        cam, sun, world = _preview_scene()
        shown = [k for k in KINDS if k in kinds]
        rows, yend = _layout(cols, shown)
        ymid = yend / 2
        span = -yend
        wmax = max(rw for _y, rw, _d, _h in rows.values())
        bpy.data.objects["scale_ref_1m60"].location = (wmax / 2 + 1.2, rows[shown[0]][0], -0.02)
        shots = []
        if not only or "lineup" in only:
            shots.append(("lineup", (0.0, 0.5 * span + 4.0, 0.55 * span + 4.0), (0.0, ymid + 1.0, 0.0), 30, None))
        for kind in shown:
            if not only or kind in only or "close" in only:
                yc, w, d, h = rows[kind]
                dist = max(w * 0.95, 5.5)
                shots.append((f"close_{kind}", (0.0, yc + d / 2 + dist * 0.85, h * 0.6 + dist * 0.42),
                              (0.0, yc, h * 0.4), 28, kind))
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
        # (Pieces taller than 2 m break the d2 surface: in the cove they only stand in deeper
        # water, the placement keeps 0.6 m over every top.)
        sun.data.energy, sun.data.color = 3.8, (1.0, 0.82, 0.64)
        sun.rotation_euler = (math.radians(62), 0, math.radians(-120))
        world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.62, 0.5, 0.52, 1)
        wm = bpy.data.meshes.new("water")
        bm = bmesh.new()
        bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=span + 60)
        bm.to_mesh(wm)
        bm.free()
        wm.materials.append(_water_mat())
        water = bpy.data.objects.new("water_standin", wm)
        bpy.context.scene.collection.objects.link(water)
        for depth in (2.0, 5.0):
            water.location = (0, ymid, depth)
            pos = (0.0, 0.42 * span + 8.0, depth + 10.0)
            _shoot(cam, pos, (0.0, ymid + 0.5, 0.0), 26, LOGS / f"{PREFIX}_underwater_d{int(depth)}_v{version}.png")
        # the court's-eye view of the reef heads alone: 25 m away, 13 m up, under 5 m of water
        if "reef_head" in shown:
            yc, w, d, h = rows["reef_head"]
            water.location = (0, yc, 5.0)
            for (k, _s), col in cols.items():
                col.hide_render = k != "reef_head"
            _shoot(cam, (0.0, yc + 22.0, 13.0), (0.0, yc, 0.8), 35,
                   LOGS / f"{PREFIX}_underwater_reef_v{version}.png")



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
