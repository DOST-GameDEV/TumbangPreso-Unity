"""The modelled ice of Cheska's ABSOLUTE ZERO (2026-10-10): the BLOCK that comes down on each player and the squat one that
takes the can, the GLACIER behind her, the three SPIKES that race out round the court, the CHUNKS the glacier breaks into,
and the snowed-on PINE of her ridge.

    py -3 tools/build_ultimate_props_cheska.py [--out=<folder>] [iceblock] [icecube] [glacier] [icespike_a] [icespike_b] [icespike_c] [icechunks] [frostpine]

Why: the cutscene was staged with stock prisms, and the owner, once the staging stood: "continue polishing all the
ultimates. i dont want to see anything bare in textures, anything looking unpolished, no vfx etc." A first set of these,
flat palette prisms out of the plant builder, was dropped the same day when he refused Rafi's shark, built that way
("doesn't fit the hand painted illustrated stylistic art design of our characters and overall game style").

So these are built the way the hand companions and Paete's reworked plants are (`tools/hand_companion_kit.py`,
`docs/CHARACTER_REDESIGN_DANTE.md` 15.9 and 15.11): the kit's writer, a PAINTED ATLAS beside the models, round forms
with smooth normals for everything soft (snow, drifts, the pine, the block's worn edges). The ice itself keeps clean
planes, as her approved crystals do (`tools/build_hand_cheska.py`: a narrow root, a fat shoulder, a point, a white tip),
but no plane is one flat colour: every face is painted (its own tone, a lit edge and a shaded one, light inside it,
frost run down from the top, cracks, glints).

Two atlases, their UPPER half paint as the cast's are:
  ice-atlas.png        the glacier, the spikes, the chunks and the pine (opaque, dressed as the cast is: `UltimateProp.Spawn`)
  iceblock-atlas.png   the two blocks, WITH ALPHA: a player stands inside one and must be read through it, so its panes
                       are painted clear in the middle and frosted toward their edges (`UltimateProp.SpawnIceBlock`)

⚠️ EVERY MARK AND EVERY PART IS TYPED: each crack, glint, drip, crystal and lump of snow has its own row of numbers.
⚠️ THE NODE NAMES ARE THE STAGE'S CONTRACT (`HeroIntroductionScene.Cheska.cs`): `rim` on a block; `foot`, `peak`, `left-a`,
`right-a`, `left-b`, `right-b`, `front-l`, `front-r` on the glacier, each with its pivot at its own foot; `chunk-0` to
`chunk-9`, each about its own middle.

Look at one in Blender before the game is asked:
    blender -b --python tools/review_hand_companion.py -- --hero=glacier --version=v1 --file=<out>/glacier.glb --atlas=<out>/ice-atlas.png
    py -3 tools/hand_companion_kit.py sheet glacier v1
"""
import math
import sys
from pathlib import Path

import numpy as np

import hand_companion_kit as kit
from hand_companion_kit import Model, ellipsoid, lathe, moved, rounded

OUT = next((Path(a.split("=", 1)[1]) for a in sys.argv[1:] if a.startswith("--out=")),
           kit.ROOT / "Assets/TumbangPreso/Resources/Models/UltimateProps")

# The sixteen flat colours (`UltimateProp.IcePalette`). Nothing here is painted with a flat cell; the list is what the
# review and the game dress the models with, and what a later flat piece would use.
PALETTE = ["#7fd3ea", "#3e9cc4", "#c4f0fb", "#ffffff", "#eafcff", "#ff9ec4", "#2f7391", "#1f5270",
           "#20262e", "#6a5a72", "#ffffff", "#cfe6f2", "#3b9eba", "#1f6f86", "#a3ecf7", "#11485a"]

PAD = 0.004
# ice-atlas.png, the upper half (u0, v0, u1, v1; v down):
R_PEAK_A, R_PEAK_B, R_SHARD = (0.0, 0.0, 0.375, 0.25), (0.375, 0.0, 0.75, 0.25), (0.75, 0.0, 1.0, 0.25)
R_SNOW, R_CHUNK, R_PINE, R_TRUNK = (0.0, 0.25, 0.25, 0.5), (0.25, 0.25, 0.5, 0.5), (0.5, 0.25, 0.875, 0.5), (0.875, 0.25, 1.0, 0.5)
# iceblock-atlas.png, the upper half:
B_SIDES, B_SHARD, B_SNOW = (0.0, 0.0, 1.0, 0.3125), (0.0, 0.3125, 0.5, 0.5), (0.5, 0.3125, 1.0, 0.5)
# The kit's rounded box puts its first column half a column round from a pane's middle: twenty columns, so a fortieth.
TURN = 0.025


def inset(rect):
    return (rect[0] + PAD, rect[1] + PAD, rect[2] - PAD, rect[3] - PAD)


# ------------------------------------------------------------------ shapes

def gem(rings, sides, rect, twist=0.0, squash_z=1.0, lean=(0.0, 0.0)):
    """A cut crystal with painted faces: `rings` is (radius, y) from bottom to top, each a regular polygon of `sides`
    corners; radius 0 closes an end to a point. Every face is flat. Round it is left to right of `rect` (with six sides
    and no twist the face that looks along +z is the second, so the front is at a quarter across, as the plants' is) and
    up it is bottom to top. `lean` moves each ring across by (x, z) for every metre it stands above the first, so a peak
    leans without its node being turned."""
    y0, y1 = rings[0][1], rings[-1][1]
    u0, v0, u1, v1 = inset(rect)

    def corner(r, k):
        radius, y = rings[r]
        a = twist + 2 * math.pi * k / sides
        return np.array((radius * math.cos(a) + lean[0] * (y - y0), y, radius * math.sin(a) * squash_z + lean[1] * (y - y0)), np.float32)

    def tex(r, k):
        return (u0 + (u1 - u0) * k / sides, v1 - (v1 - v0) * (rings[r][1] - y0) / (y1 - y0))

    pos, nrm, uv, tris = [], [], [], []
    for r in range(len(rings) - 1):
        axis = np.array((lean[0] * ((rings[r][1] + rings[r + 1][1]) * .5 - y0), (rings[r][1] + rings[r + 1][1]) * .5,
                         lean[1] * ((rings[r][1] + rings[r + 1][1]) * .5 - y0)), np.float32)
        for k in range(sides):
            if rings[r][0] < 1e-7:
                pts, tx = [corner(r, k), corner(r + 1, k), corner(r + 1, k + 1)], [tex(r, k + .5), tex(r + 1, k), tex(r + 1, k + 1)]
            elif rings[r + 1][0] < 1e-7:
                pts, tx = [corner(r, k), corner(r, k + 1), corner(r + 1, k)], [tex(r, k), tex(r, k + 1), tex(r + 1, k + .5)]
            else:
                pts = [corner(r, k), corner(r, k + 1), corner(r + 1, k + 1), corner(r + 1, k)]
                tx = [tex(r, k), tex(r, k + 1), tex(r + 1, k + 1), tex(r + 1, k)]
            n = np.cross(pts[1] - pts[0], pts[-1] - pts[0])
            n /= max(float(np.linalg.norm(n)), 1e-9)
            if float(np.dot(n, np.mean(pts, axis=0) - axis)) < 0:
                pts, tx, n = pts[::-1], tx[::-1], -n
            base = len(pos)
            pos.extend(pts); nrm.extend([n] * len(pts)); uv.extend(tx)
            for i in range(1, len(pts) - 1):
                tris.append((base, base + i, base + i + 1))
    # An end left open above the ground is closed flat (a chunk has a top and a bottom to tumble).
    middle = np.array((lean[0] * (y1 - y0) * .5, (y0 + y1) * .5, lean[1] * (y1 - y0) * .5), np.float32)
    for end in (0, len(rings) - 1):
        if rings[end][0] < 1e-7 or (end == 0 and abs(y0) < 1e-6):
            continue
        pts, tx = [corner(end, k) for k in range(sides)], [tex(end, k + .5) for k in range(sides)]
        n = np.array((0, -1 if end == 0 else 1, 0), np.float32)
        if float(np.dot(np.cross(pts[1] - pts[0], pts[2] - pts[0]), n)) < 0:
            pts, tx = pts[::-1], tx[::-1]
        base = len(pos)
        pos.extend(pts); nrm.extend([n] * sides); uv.extend(tx)
        for i in range(1, sides - 1):
            tris.append((base, base + i, base + i + 1))
    return (np.array(pos, np.float32), np.array(nrm, np.float32), np.array(tris, np.uint32)), np.array(uv, np.float32)


def put(piece, at=(0, 0, 0), turn=(0, 0, 0), scale=(1, 1, 1)):
    """A painted piece moved: its UVs were laid as it was built and stay."""
    return (moved(piece[0], at, turn, scale), piece[1])


def lump(half, at, rect, turn=(0, 0, 0)):
    """Something soft and round (snow, a drift), painted from `rect`: its top is the top of the rect."""
    mesh = ellipsoid(half, seg=12, rings=7)
    return (moved(mesh, at, turn), kit.grid_uv(mesh, inset(rect), 12, "axis", 1))


# ------------------------------------------------------------------ the paint

def hexc(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], np.float32)


def smooth01(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


class Canvas:
    """An atlas being painted: colour and (for the blocks) alpha, a rect at a time."""

    def __init__(self, size, base):
        self.size = size
        self.rgb = np.zeros((size, size, 3), np.float32) + hexc(base)
        self.alpha = np.ones((size, size), np.float32)

    def box(self, rect):
        return int(rect[0] * self.size), int(rect[1] * self.size), int(rect[2] * self.size), int(rect[3] * self.size)

    def grid(self, rect):
        """u across (0 to 1) and t up (0 at the foot of the piece, 1 at its top) for every pixel of `rect`."""
        x0, y0, x1, y1 = self.box(rect)
        u = (np.arange(x0, x1) + .5 - x0) / (x1 - x0)
        t = 1.0 - (np.arange(y0, y1) + .5 - y0) / (y1 - y0)
        return np.meshgrid(u, t)

    def put(self, rect, colour, alpha=None):
        x0, y0, x1, y1 = self.box(rect)
        self.rgb[y0:y1, x0:x1] = colour
        if alpha is not None:
            self.alpha[y0:y1, x0:x1] = alpha

    def picture(self, with_alpha):
        from PIL import Image
        rgb = np.clip(self.rgb, 0, 255).astype(np.uint8)
        if not with_alpha:
            return Image.fromarray(rgb, "RGB")
        a = np.clip(self.alpha * 255, 0, 255).astype(np.uint8)
        return Image.fromarray(np.dstack([rgb, a]), "RGBA")


def mix(base, colour, weight):
    return base * (1 - weight[..., None]) + hexc(colour) * weight[..., None]


def crystal_paint(c, rect, sides, tone, frost_at, scallop):
    """The painted skin of a cut crystal, face by face. `tone` is how light each face is (-1 deep to 1 light; the second
    is the one that looks at the lens). Up it: a deep foot with snow lying against it, the body, paler to the shoulder,
    and frost from `frost_at` up with a scalloped lower edge (`scallop` waves round the crystal)."""
    u, t = c.grid(rect)
    face = np.floor(u * sides).astype(int).clip(0, sides - 1)
    x = u * sides - face                                         # 0 to 1 across each face
    col = np.zeros(u.shape + (3,), np.float32) + hexc("#3b9eba")  # the ice teal of the live ice (`CheskaIceVisuals`)
    col = mix(col, "#1f6f86", (1 - smooth01(0.0, 0.22, t)) * 0.85)
    col = mix(col, "#7fd3ea", smooth01(0.18, 0.62, t) * 0.75)
    col = mix(col, "#a3ecf7", smooth01(0.55, 0.9, t) * 0.6)
    lightness = np.array(tone, np.float32)[face]
    col = mix(col, "#d4f8ff", np.clip(lightness, 0, 1) * 0.55)
    col = mix(col, "#155a78", np.clip(-lightness, 0, 1) * 0.6)
    # Across a face: a soft turn from its lit side to its shaded side, so a plane is never one flat tone.
    col = mix(col, "#e2fbff", (1 - smooth01(0.0, 0.55, x)) * 0.22)
    col = mix(col, "#1f6f86", smooth01(0.6, 1.0, x) * 0.2)
    # The light inside it: a pale core up the middle of the faces that look at the lens, with a breath of her pink low in it.
    front = np.isin(face, (0, 1, 2)).astype(np.float32)
    core = np.exp(-((x - 0.5) / 0.26) ** 2) * np.exp(-((t - 0.46) / 0.30) ** 2) * front
    col = mix(col, "#e9fdff", core * 0.62)
    col = mix(col, "#ffc4dc", np.exp(-((x - 0.42) / 0.2) ** 2) * np.exp(-((t - 0.2) / 0.1) ** 2) * (face == 1) * 0.3)
    # The cut edges, drawn: a bright line down the lit side of each face and a deep one down the other.
    col = mix(col, "#f4feff", (1 - smooth01(0.0, 0.045, x)) * 0.85)
    col = mix(col, "#11485a", smooth01(0.955, 1.0, x) * 0.7)
    # Snow lying against the foot, and the frost on top with its scalloped edge.
    drift = 0.035 + 0.02 * np.cos((u * sides * 1.5 + 0.2) * 2 * np.pi)
    col = mix(col, "#eaf8fd", 1 - smooth01(drift, drift + 0.012, t))
    edge = frost_at + 0.035 * np.cos((u * scallop + 0.13) * 2 * np.pi) + 0.02 * np.cos((u * scallop * 2.3 + 0.4) * 2 * np.pi)
    col = mix(col, "#cfeef8", smooth01(edge - 0.05, edge, t) * 0.7)
    col = mix(col, "#ffffff", smooth01(edge - 0.008, edge + 0.004, t))
    col = mix(col, "#cfe6f2", smooth01(edge, edge + 0.02, t) * (1 - smooth01(edge + 0.02, edge + 0.07, t)) * 0.35)
    c.put(rect, col)


def marks(c, rect, sides, cracks, glints, flecks, with_alpha=False, shift=0.0):
    """The drawn marks of one crystal, every one typed. A crack is (face, [(x, t), ...], width) in that face's own
    0 to 1; a glint is a short slanted stroke of light (face, x, t, length, width); a fleck is a frost speck (face, x, t, r)."""
    from PIL import Image, ImageDraw
    x0, y0, x1, y1 = c.box(rect)
    w, h = x1 - x0, y1 - y0
    layer = Image.new("RGBA", (c.size, c.size), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)

    def at(face, x, t):
        return x0 + ((face + x) / sides - shift) * w, y0 + (1 - t) * h

    for face, line, width in cracks:
        pts = [at(face, x, t) for x, t in line]
        d.line(pts, fill=(17, 72, 90, 150), width=width + 4, joint="curve")      # the shade either side of it
        d.line(pts, fill=(240, 254, 255, 255), width=width, joint="curve")       # the crack, white as frost
    for face, x, t, length, width in glints:
        ax, ay = at(face, x, t)
        bx, by = at(face, x + length * 0.35, t + length)
        d.line([(ax, ay), (bx, by)], fill=(255, 255, 255, 215), width=width)
    for face, x, t, r in flecks:
        cx, cy = at(face, x, t)
        d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(255, 255, 255, 235))
    over = np.asarray(layer, np.float32)
    a = over[..., 3:4] / 255.0
    c.rgb = c.rgb * (1 - a) + over[..., :3] * a
    if with_alpha:
        c.alpha = np.maximum(c.alpha, a[..., 0] * 0.95)


def paint_ice_atlas(path):
    from PIL import ImageFilter
    c = Canvas(2048, "#7fd3ea")
    # ---- the tall peaks (A: the middle one and the outer two; B: the two beside it). Six faces; the front is the second.
    crystal_paint(c, R_PEAK_A, 6, (0.15, 0.85, 0.25, -0.45, -0.85, -0.3), 0.80, 6)
    marks(c, R_PEAK_A, 6,
          cracks=[(1, [(0.62, 0.60), (0.48, 0.50), (0.58, 0.41), (0.40, 0.30)], 5), (1, [(0.48, 0.50), (0.30, 0.47)], 4),
                  (0, [(0.30, 0.44), (0.46, 0.36), (0.34, 0.26), (0.50, 0.17)], 5), (2, [(0.55, 0.66), (0.42, 0.56), (0.60, 0.47)], 4),
                  (3, [(0.40, 0.52), (0.56, 0.42), (0.44, 0.33)], 4), (5, [(0.52, 0.40), (0.38, 0.30), (0.56, 0.22)], 4)],
          glints=[(1, 0.20, 0.52, 0.16, 9), (1, 0.30, 0.56, 0.09, 5), (0, 0.62, 0.50, 0.12, 7), (2, 0.24, 0.30, 0.13, 7),
                  (2, 0.34, 0.33, 0.07, 4), (4, 0.30, 0.42, 0.10, 5), (5, 0.66, 0.56, 0.11, 6)],
          flecks=[(1, 0.72, 0.70, 6), (1, 0.80, 0.66, 4), (0, 0.24, 0.68, 5), (2, 0.70, 0.72, 5), (2, 0.78, 0.62, 3),
                  (3, 0.30, 0.70, 4), (4, 0.66, 0.66, 4), (5, 0.26, 0.72, 5), (0, 0.76, 0.24, 4), (1, 0.18, 0.20, 5)])
    crystal_paint(c, R_PEAK_B, 6, (0.35, 0.70, -0.05, -0.6, -0.75, 0.0), 0.76, 5)
    marks(c, R_PEAK_B, 6,
          cracks=[(1, [(0.36, 0.58), (0.52, 0.47), (0.40, 0.38), (0.58, 0.27)], 5), (1, [(0.52, 0.47), (0.70, 0.45)], 4),
                  (2, [(0.44, 0.46), (0.30, 0.36), (0.48, 0.26)], 4), (0, [(0.60, 0.62), (0.46, 0.52), (0.62, 0.44)], 4),
                  (4, [(0.50, 0.50), (0.36, 0.40), (0.52, 0.30)], 4)],
          glints=[(1, 0.66, 0.54, 0.14, 8), (0, 0.26, 0.34, 0.14, 7), (0, 0.36, 0.38, 0.07, 4), (2, 0.64, 0.56, 0.10, 6),
                  (5, 0.30, 0.48, 0.12, 6), (3, 0.62, 0.36, 0.09, 5)],
          flecks=[(1, 0.24, 0.68, 6), (0, 0.70, 0.66, 4), (2, 0.28, 0.64, 5), (3, 0.70, 0.64, 4), (5, 0.68, 0.66, 5),
                  (1, 0.78, 0.22, 4), (2, 0.74, 0.20, 4), (4, 0.30, 0.62, 3)])
    # ---- the small crystals: the ones grown out of a peak's side, the teeth, the spikes. Five faces.
    crystal_paint(c, R_SHARD, 5, (0.3, 0.9, 0.1, -0.7, -0.4), 0.74, 5)
    marks(c, R_SHARD, 5,
          cracks=[(1, [(0.56, 0.56), (0.42, 0.44), (0.56, 0.34)], 4), (0, [(0.44, 0.46), (0.58, 0.36)], 4), (3, [(0.46, 0.50), (0.56, 0.38)], 3)],
          glints=[(1, 0.22, 0.44, 0.18, 7), (0, 0.60, 0.50, 0.12, 5), (2, 0.30, 0.36, 0.14, 6), (4, 0.60, 0.44, 0.10, 4)],
          flecks=[(1, 0.74, 0.62, 5), (0, 0.26, 0.62, 4), (2, 0.70, 0.60, 4), (4, 0.30, 0.62, 3)])
    # ---- what it breaks into: the raw inside of the ice, paler, with the grain of the break across it. Five faces.
    u, t = c.grid(R_CHUNK)
    face = np.floor(u * 5).astype(int).clip(0, 4)
    x = u * 5 - face
    col = np.zeros(u.shape + (3,), np.float32) + hexc("#7fd3ea")
    col = mix(col, "#3b9eba", (1 - smooth01(0.0, 0.5, t)) * 0.6)
    col = mix(col, "#c4f0fb", smooth01(0.45, 1.0, t) * 0.7)
    col = mix(col, "#d4f8ff", np.clip(np.array((0.2, 0.9, 0.0, -0.6, -0.3), np.float32)[face], 0, 1) * 0.5)
    col = mix(col, "#155a78", np.clip(-np.array((0.2, 0.9, 0.0, -0.6, -0.3), np.float32)[face], 0, 1) * 0.55)
    col = mix(col, "#eafcff", (0.5 + 0.5 * np.cos((t * 5.5 + x * 1.3) * 2 * np.pi)) ** 5 * 0.4)      # the grain of the break
    col = mix(col, "#f4feff", (1 - smooth01(0.0, 0.06, x)) * 0.85)
    col = mix(col, "#11485a", smooth01(0.94, 1.0, x) * 0.65)
    col = mix(col, "#ffffff", smooth01(0.84, 0.9, t))                                                # its frost still on
    c.put(R_CHUNK, col)
    marks(c, R_CHUNK, 5, cracks=[(1, [(0.3, 0.6), (0.5, 0.44), (0.4, 0.3)], 4), (3, [(0.6, 0.6), (0.44, 0.44)], 3)],
          glints=[(1, 0.62, 0.3, 0.2, 7), (0, 0.3, 0.36, 0.16, 5), (2, 0.6, 0.4, 0.14, 5)], flecks=[(1, 0.2, 0.2, 5), (4, 0.6, 0.5, 4)])
    # ---- snow: white on top, a cool shade under, and soft blue hollows so a lump is not a white ball.
    u, t = c.grid(R_SNOW)
    col = np.zeros(u.shape + (3,), np.float32) + hexc("#ffffff")
    col = mix(col, "#cfe6f2", (1 - smooth01(0.1, 0.55, t)) * 0.9)
    col = mix(col, "#9fcfe6", (1 - smooth01(0.0, 0.2, t)) * 0.7)
    for at_u, at_t, wide, tall in [(0.14, 0.62, 0.07, 0.10), (0.40, 0.70, 0.09, 0.08), (0.63, 0.58, 0.06, 0.11), (0.86, 0.66, 0.08, 0.09)]:
        col = mix(col, "#d6eef8", np.exp(-((u - at_u) / wide) ** 2 - ((t - at_t) / tall) ** 2) * 0.6)
    col = mix(col, "#ffe3ee", np.exp(-((u - 0.25) / 0.12) ** 2 - ((t - 0.86) / 0.08) ** 2) * 0.35)    # one warm glance of her pink
    c.put(R_SNOW, col)
    # ---- the pine: cold teal needles, darker under each tier, snow lying on top of it with a scalloped edge.
    u, t = c.grid(R_PINE)
    col = np.zeros(u.shape + (3,), np.float32) + hexc("#2f7391")
    col = mix(col, "#1f5270", (1 - smooth01(0.0, 0.35, t)) * 0.9)
    col = mix(col, "#3f8fa8", smooth01(0.3, 0.75, t) * 0.5)
    col = mix(col, "#17405a", (0.5 + 0.5 * np.cos(u * 2 * np.pi * 11)) ** 4 * (1 - smooth01(0.3, 0.7, t)) * 0.5)    # the hang of the boughs
    col = mix(col, "#58a9bd", np.exp(-((u - 0.25) / 0.10) ** 2) * 0.35)
    edge = 0.60 + 0.07 * np.cos((u * 7 + 0.2) * 2 * np.pi) + 0.03 * np.cos((u * 17 + 0.5) * 2 * np.pi)
    col = mix(col, "#cfe6f2", smooth01(edge - 0.05, edge, t) * 0.6)
    col = mix(col, "#ffffff", smooth01(edge - 0.01, edge + 0.01, t))
    c.put(R_PINE, col)
    u, t = c.grid(R_TRUNK)
    col = np.zeros(u.shape + (3,), np.float32) + hexc("#6a5a72")
    col = mix(col, "#4a3e55", (0.5 + 0.5 * np.cos(u * 2 * np.pi * 7)) ** 3 * 0.6)
    col = mix(col, "#eaf8fd", 1 - smooth01(0.10, 0.16, t))
    c.put(R_TRUNK, col)
    picture = c.picture(False).filter(ImageFilter.GaussianBlur(0.7))
    picture.save(path)
    return path


def paint_block_atlas(path):
    """The blocks' skin, WITH ALPHA. Round a block the four panes are a quarter each, their middles at 0, a quarter, a half
    and three quarters across (the kit's rounded box starts on a pane's middle); the front is the second."""
    from PIL import ImageFilter
    c = Canvas(1024, "#7fd3ea")
    u, t = c.grid(B_SIDES)
    u = u + TURN                                                 # round the block as the box's own columns go
    pane = np.floor(u * 4 + 0.5).astype(int) % 4
    x = np.abs(((u * 4 + 0.5) % 1.0) - 0.5) * 2                  # 0 at a pane's middle, 1 at its edge (the block's corner)
    col = np.zeros(u.shape + (3,), np.float32) + hexc("#3b9eba")
    col = mix(col, "#7fd3ea", smooth01(0.1, 0.7, t) * 0.7)
    col = mix(col, "#1f6f86", (1 - smooth01(0.0, 0.16, t)) * 0.5)
    tone = np.array((0.0, 0.5, -0.2, -0.45), np.float32)[pane]
    col = mix(col, "#c4f0fb", np.clip(tone, 0, 1) * 0.5)
    col = mix(col, "#1f6f86", np.clip(-tone, 0, 1) * 0.45)
    # Frost grown in from the corners, the light in the middle of each pane, and the corner itself drawn as a pale line.
    col = mix(col, "#d4f8ff", smooth01(0.58, 0.95, x) * 0.75)
    col = mix(col, "#e9fdff", np.exp(-(x / 0.42) ** 2) * np.exp(-((t - 0.42) / 0.3) ** 2) * 0.35)
    col = mix(col, "#f4feff", smooth01(0.90, 0.985, x) * 0.9)
    alpha = 0.26 + 0.5 * smooth01(0.5, 0.95, x) + 0.2 * smooth01(0.9, 1.0, x)
    # Snow piled against the foot of each pane.
    drift = 0.045 + 0.02 * np.cos((u * 6 + 0.1) * 2 * np.pi) + 0.012 * np.cos((u * 13 + 0.4) * 2 * np.pi)
    low = 1 - smooth01(drift, drift + 0.014, t)
    col = mix(col, "#eaf8fd", low)
    alpha = np.maximum(alpha, low * 0.95)
    # The frost on top, run down each pane as icing: every drip typed (where round the block, how far down, how wide).
    edge = np.zeros_like(u) + 0.905
    for at_u, deep, wide in [(0.035, 0.07, 0.022), (0.095, 0.035, 0.016), (0.150, 0.095, 0.020), (0.215, 0.045, 0.018), (0.262, 0.11, 0.024),
                             (0.330, 0.04, 0.016), (0.385, 0.08, 0.020), (0.455, 0.05, 0.018), (0.520, 0.10, 0.022), (0.585, 0.035, 0.016),
                             (0.640, 0.075, 0.020), (0.715, 0.045, 0.018), (0.775, 0.105, 0.024), (0.845, 0.04, 0.016), (0.905, 0.085, 0.020),
                             (0.965, 0.05, 0.018)]:
        edge = edge - deep * np.exp(-((((u - at_u + 0.5) % 1.0) - 0.5) / wide) ** 2)
    top = smooth01(edge - 0.006, edge + 0.006, t)
    col = mix(col, "#cfeef8", smooth01(edge - 0.035, edge, t) * 0.6)
    col = mix(col, "#ffffff", top)
    alpha = np.maximum(alpha, np.maximum(top * 0.97, smooth01(edge - 0.035, edge, t) * 0.6))
    c.put(B_SIDES, col, np.clip(alpha, 0, 1))
    # Cracks and glints, pane by pane (pane 1 is the front, the one turned to her). A mark's x is measured from its
    # pane's middle, half a pane either way.
    marks(c, B_SIDES, 4, with_alpha=True, shift=TURN,
          cracks=[(1, [(0.22, 0.86), (0.13, 0.72), (0.24, 0.62), (0.11, 0.50)], 5), (1, [(0.13, 0.72), (0.02, 0.70)], 4),
                  (1, [(-0.26, 0.10), (-0.17, 0.24), (-0.27, 0.34)], 5), (2, [(0.12, 0.80), (0.22, 0.66), (0.13, 0.54)], 5),
                  (2, [(-0.22, 0.16), (-0.12, 0.30)], 4), (3, [(0.15, 0.84), (0.07, 0.70), (0.19, 0.58), (0.09, 0.46)], 5),
                  (4, [(-0.24, 0.78), (-0.14, 0.64), (-0.22, 0.52)], 5), (0, [(0.12, 0.14), (0.22, 0.28), (0.14, 0.40)], 4)],
          glints=[(1, -0.16, 0.50, 0.2, 8), (1, -0.07, 0.56, 0.1, 5), (2, 0.02, 0.30, 0.16, 7), (3, -0.14, 0.48, 0.18, 7), (0, 0.06, 0.52, 0.16, 7),
                  (4, -0.12, 0.30, 0.14, 6)],
          flecks=[(1, 0.26, 0.30, 5), (1, -0.30, 0.74, 4), (2, 0.28, 0.60, 4), (3, -0.28, 0.72, 5), (0, 0.30, 0.20, 4), (2, -0.28, 0.70, 4)])
    # ---- the shards thrown up round its foot: small crystals, paler than the block, nearly solid.
    u, t = c.grid(B_SHARD)
    face = np.floor(u * 5).astype(int).clip(0, 4)
    x = u * 5 - face
    col = np.zeros(u.shape + (3,), np.float32) + hexc("#5fc0da")
    col = mix(col, "#2a86a2", (1 - smooth01(0.0, 0.3, t)) * 0.7)
    col = mix(col, "#c4f0fb", smooth01(0.4, 0.9, t) * 0.75)
    tone = np.array((0.2, 0.9, 0.0, -0.6, -0.3), np.float32)[face]
    col = mix(col, "#e2fbff", np.clip(tone, 0, 1) * 0.5)
    col = mix(col, "#155a78", np.clip(-tone, 0, 1) * 0.5)
    col = mix(col, "#f4feff", (1 - smooth01(0.0, 0.06, x)) * 0.85)
    col = mix(col, "#ffffff", smooth01(0.80, 0.86, t))
    c.put(B_SHARD, col, np.zeros_like(u) + 0.9)
    # ---- snow on top of it and frost piled between the shards.
    u, t = c.grid(B_SNOW)
    col = np.zeros(u.shape + (3,), np.float32) + hexc("#ffffff")
    col = mix(col, "#cfe6f2", (1 - smooth01(0.1, 0.6, t)) * 0.9)
    col = mix(col, "#9fcfe6", (1 - smooth01(0.0, 0.2, t)) * 0.6)
    c.put(B_SNOW, col, np.zeros_like(u) + 0.96)
    picture = c.picture(True)
    r, g, b, a = picture.split()
    from PIL import Image
    picture = Image.merge("RGBA", [ch.filter(ImageFilter.GaussianBlur(0.6)) for ch in (r, g, b, a)])
    picture.save(path)
    return path


# ------------------------------------------------------------------ the models

def model(name, texture):
    m = Model(name)
    m.folder = OUT
    m.texture = texture
    return m


def block_body(m, name, half_height):
    """The block itself: a box worn round at every edge, its four panes and its frosted top one painted skin."""
    box = rounded((0.525, half_height, 0.525), 0.085, seg=20, rings=12)
    parts = [(moved(box, at=(0, half_height, 0)), kit.grid_uv(box, inset(B_SIDES), 20, "axis", 1))]
    return parts


def iceblock():
    """The block that comes down over a player: 1.05 m square, 2.0 m tall, standing on the origin. Snow lies on its top;
    `rim` is the ring of shards and frost thrown up round its foot, its own node (it arrives a beat after the block)."""
    m = model("iceblock", "iceblock-atlas.png")
    shell = block_body(m, "shell", 1.0)
    shell += [lump((0.34, 0.085, 0.30), (-0.13, 2.02, 0.08), B_SNOW, turn=(0, 18, 0)),
              lump((0.20, 0.07, 0.19), (0.24, 2.03, -0.19), B_SNOW, turn=(0, -12, 0)),
              lump((0.13, 0.05, 0.12), (0.26, 2.02, 0.26), B_SNOW)]
    m.add("shell", shell)
    rim = [
        put(gem([(0.13, 0.0), (0.16, 0.14), (0.11, 0.38), (0.0, 0.58)], 5, B_SHARD, lean=(0.36, 0.34)), at=(0.58, 0, 0.52)),
        put(gem([(0.11, 0.0), (0.14, 0.10), (0.09, 0.26), (0.0, 0.40)], 5, B_SHARD, lean=(0.70, 0.08)), at=(0.74, 0, 0.06)),
        put(gem([(0.14, 0.0), (0.17, 0.16), (0.11, 0.44), (0.0, 0.68)], 5, B_SHARD, twist=0.6, lean=(0.30, -0.36)), at=(0.60, 0, -0.50)),
        put(gem([(0.10, 0.0), (0.13, 0.09), (0.08, 0.22), (0.0, 0.34)], 5, B_SHARD, twist=1.1, lean=(0.08, -0.76)), at=(0.02, 0, -0.74)),
        put(gem([(0.13, 0.0), (0.16, 0.14), (0.10, 0.40), (0.0, 0.60)], 5, B_SHARD, twist=2.0, lean=(-0.44, -0.34)), at=(-0.56, 0, -0.56)),
        put(gem([(0.12, 0.0), (0.15, 0.11), (0.09, 0.28), (0.0, 0.42)], 5, B_SHARD, twist=2.6, lean=(-0.70, 0.12)), at=(-0.74, 0, -0.04)),
        put(gem([(0.14, 0.0), (0.17, 0.17), (0.11, 0.48), (0.0, 0.74)], 5, B_SHARD, twist=3.3, lean=(-0.24, 0.38)), at=(-0.54, 0, 0.56)),
        put(gem([(0.09, 0.0), (0.12, 0.08), (0.07, 0.19), (0.0, 0.28)], 5, B_SHARD, twist=0.3, lean=(0.34, 0.84)), at=(0.04, 0, 0.74)),
        lump((0.24, 0.07, 0.15), (0.32, 0.03, 0.68), B_SNOW, turn=(0, 14, 0)), lump((0.14, 0.06, 0.22), (0.70, 0.03, -0.24), B_SNOW, turn=(0, -10, 0)),
        lump((0.23, 0.07, 0.14), (-0.30, 0.03, -0.70), B_SNOW, turn=(0, 9, 0)), lump((0.14, 0.06, 0.23), (-0.72, 0.03, 0.28), B_SNOW, turn=(0, -16, 0)),
    ]
    m.add("rim", rim)
    return m.write(PALETTE)


def icecube():
    """The squat block that takes the can: 1.05 m square, 0.72 m tall. The same ice, typed for its size."""
    m = model("icecube", "iceblock-atlas.png")
    shell = block_body(m, "shell", 0.36)
    shell += [lump((0.32, 0.075, 0.30), (0.10, 0.74, -0.06), B_SNOW, turn=(0, -20, 0)), lump((0.16, 0.055, 0.15), (-0.24, 0.74, 0.22), B_SNOW)]
    m.add("shell", shell)
    rim = [
        put(gem([(0.11, 0.0), (0.14, 0.10), (0.09, 0.26), (0.0, 0.40)], 5, B_SHARD, lean=(0.52, 0.48)), at=(0.60, 0, 0.54)),
        put(gem([(0.09, 0.0), (0.12, 0.07), (0.07, 0.18), (0.0, 0.27)], 5, B_SHARD, twist=0.5, lean=(0.90, -0.14)), at=(0.74, 0, -0.10)),
        put(gem([(0.11, 0.0), (0.14, 0.11), (0.09, 0.28), (0.0, 0.43)], 5, B_SHARD, twist=1.3, lean=(-0.10, -0.54)), at=(-0.02, 0, -0.74)),
        put(gem([(0.09, 0.0), (0.12, 0.07), (0.07, 0.18), (0.0, 0.27)], 5, B_SHARD, twist=2.1, lean=(-0.84, -0.62)), at=(-0.62, 0, -0.50)),
        put(gem([(0.11, 0.0), (0.14, 0.11), (0.09, 0.29), (0.0, 0.45)], 5, B_SHARD, twist=2.9, lean=(-0.56, 0.26)), at=(-0.72, 0, 0.24)),
        put(gem([(0.08, 0.0), (0.11, 0.06), (0.06, 0.15), (0.0, 0.23)], 5, B_SHARD, twist=0.2, lean=(-0.34, 0.92)), at=(-0.10, 0, 0.74)),
        lump((0.22, 0.06, 0.14), (0.30, 0.03, 0.70), B_SNOW, turn=(0, -12, 0)), lump((0.14, 0.055, 0.21), (-0.68, 0.03, -0.16), B_SNOW, turn=(0, 12, 0)),
        lump((0.20, 0.06, 0.13), (0.36, 0.03, -0.70), B_SNOW, turn=(0, 16, 0)),
    ]
    m.add("rim", rim)
    return m.write(PALETTE)


def glacier():
    """The glacier that goes up behind her: five peaks in a crown, the middle one the tallest (9 m), two short teeth in
    front, and broken ice and drifts round the foot. Every peak is its own node with its pivot at its foot, so the stage
    brings each up on its own beat. A peak is her crystal grown huge: a narrow root, a fat shoulder a little way up, a
    long body that narrows to a second shoulder, and a point; a smaller crystal grown out of its side; snow heaped on that
    second shoulder (never balanced on a point: the frost on a tip is painted). The origin is the middle of the crown on the ground; the lens is on +Z."""
    m = model("glacier", "ice-atlas.png")
    m.add("peak", [
        gem([(1.20, 0.0), (1.50, 1.1), (1.30, 5.6), (0.86, 7.4), (0.0, 9.0)], 6, R_PEAK_A, lean=(0.03, 0.0)),
        put(gem([(0.56, 0.0), (0.72, 0.6), (0.56, 3.2), (0.0, 4.9)], 5, R_SHARD, twist=0.4, lean=(-0.07, 0.05)), at=(-1.25, 0, 0.75)),
        put(gem([(0.44, 0.0), (0.56, 0.45), (0.42, 2.1), (0.0, 3.3)], 5, R_SHARD, twist=1.2, lean=(0.06, 0.04)), at=(1.32, 0, 0.62)),
        # Snow heaped on its second shoulder, where the point begins, and banked against its foot.
        lump((0.66, 0.40, 0.46), (0.20, 7.30, 0.44), R_SNOW, turn=(14, 0, 0)), lump((0.50, 0.30, 0.36), (-0.52, 7.22, 0.20), R_SNOW, turn=(10, 0, 12)),
        lump((0.90, 0.34, 0.60), (0.10, 0.10, 1.30), R_SNOW),
    ], at=(0.2, 0.0, -0.5))
    m.add("left-a", [
        gem([(0.96, 0.0), (1.20, 0.9), (1.02, 4.4), (0.66, 5.9), (0.0, 7.2)], 6, R_PEAK_B, lean=(-0.085, 0.0)),
        put(gem([(0.42, 0.0), (0.54, 0.45), (0.40, 2.3), (0.0, 3.6)], 5, R_SHARD, twist=0.9, lean=(0.07, 0.03)), at=(0.92, 0, 0.60)),
        lump((0.52, 0.32, 0.38), (-0.48, 5.82, 0.34), R_SNOW, turn=(14, 0, 8)),
    ], at=(-1.95, 0.0, -0.1))
    m.add("right-a", [
        gem([(0.90, 0.0), (1.12, 0.8), (0.96, 4.0), (0.62, 5.4), (0.0, 6.6)], 6, R_PEAK_B, twist=0.2, lean=(0.12, 0.0)),
        put(gem([(0.38, 0.0), (0.48, 0.4), (0.36, 1.8), (0.0, 2.9)], 5, R_SHARD, twist=2.2, lean=(-0.08, 0.03)), at=(-0.86, 0, 0.56)),
        lump((0.48, 0.30, 0.36), (0.62, 5.32, 0.32), R_SNOW, turn=(14, 0, -8)),
    ], at=(2.35, 0.0, 0.0))
    m.add("left-b", [
        gem([(0.74, 0.0), (0.94, 0.7), (0.80, 2.7), (0.52, 3.7), (0.0, 4.6)], 6, R_PEAK_A, twist=0.3, lean=(-0.21, 0.0)),
        put(gem([(0.32, 0.0), (0.42, 0.3), (0.30, 1.3), (0.0, 2.1)], 5, R_SHARD, twist=1.6, lean=(0.04, 0.03)), at=(0.72, 0, 0.48)),
        lump((0.40, 0.25, 0.30), (-0.76, 3.64, 0.26), R_SNOW, turn=(12, 0, 12)),
    ], at=(-3.95, 0.0, 0.8))
    m.add("right-b", [
        gem([(0.70, 0.0), (0.90, 0.65), (0.76, 2.4), (0.50, 3.3), (0.0, 4.1)], 6, R_PEAK_A, twist=0.5, lean=(0.23, 0.0)),
        put(gem([(0.30, 0.0), (0.40, 0.28), (0.28, 1.1), (0.0, 1.8)], 5, R_SHARD, twist=2.8, lean=(-0.03, 0.03)), at=(-0.68, 0, 0.46)),
        lump((0.38, 0.24, 0.28), (0.74, 3.24, 0.24), R_SNOW, turn=(12, 0, -12)),
    ], at=(4.25, 0.0, 1.0))
    m.add("front-l", [
        gem([(0.40, 0.0), (0.52, 0.34), (0.38, 1.1), (0.0, 1.7)], 5, R_SHARD, twist=0.2, lean=(-0.15, 0.15)),
        put(gem([(0.24, 0.0), (0.31, 0.2), (0.22, 0.62), (0.0, 0.98)], 5, R_SHARD, twist=1.4, lean=(0.18, 0.14)), at=(0.44, 0, 0.10)),
        lump((0.44, 0.16, 0.30), (0.10, 0.05, 0.40), R_SNOW),
    ], at=(-1.2, 0.0, 1.7))
    m.add("front-r", [
        gem([(0.36, 0.0), (0.47, 0.3), (0.34, 0.94), (0.0, 1.45)], 5, R_SHARD, twist=1.0, lean=(0.21, 0.14)),
        put(gem([(0.22, 0.0), (0.28, 0.18), (0.20, 0.54), (0.0, 0.84)], 5, R_SHARD, twist=2.4, lean=(-0.17, 0.16)), at=(-0.42, 0, 0.12)),
        lump((0.40, 0.15, 0.28), (-0.08, 0.05, 0.40), R_SNOW),
    ], at=(1.5, 0.0, 1.8))
    # Broken ice and drifts round the foot, so the peaks stand in something.
    slab_a = rounded((0.50, 0.24, 0.38), 0.10, seg=12, rings=8)
    slab_b = rounded((0.60, 0.28, 0.42), 0.11, seg=12, rings=8)
    slab_c = rounded((0.42, 0.20, 0.36), 0.09, seg=12, rings=8)
    m.add("foot", [
        (moved(slab_a, at=(-2.9, 0.20, 1.1), turn=(0, 24, 9)), kit.grid_uv(slab_a, inset(R_CHUNK), 12, "axis", 1)),
        (moved(slab_b, at=(-0.2, 0.24, 1.0), turn=(0, -14, -7)), kit.grid_uv(slab_b, inset(R_CHUNK), 12, "axis", 1)),
        (moved(slab_c, at=(3.1, 0.18, 1.3), turn=(8, -22, 10)), kit.grid_uv(slab_c, inset(R_CHUNK), 12, "axis", 1)),
        lump((1.10, 0.22, 0.70), (-2.4, 0.05, 1.5), R_SNOW, turn=(0, 20, 0)), lump((1.30, 0.26, 0.80), (0.4, 0.05, 1.5), R_SNOW, turn=(0, -8, 0)),
        lump((1.00, 0.20, 0.66), (3.3, 0.05, 1.7), R_SNOW, turn=(0, -24, 0)), lump((0.70, 0.16, 0.50), (-4.8, 0.04, 1.9), R_SNOW, turn=(0, 30, 0)),
        lump((0.66, 0.15, 0.48), (5.2, 0.04, 2.1), R_SNOW, turn=(0, -28, 0)),
    ])
    return m.write(PALETTE)


def icespike_a():
    """A spike of the rings: one fat crystal leaning with a short one at its foot. Unit height, about a unit across at
    the foot, standing on the origin: the stage gives each its own height and width."""
    m = model("icespike_a", "ice-atlas.png")
    m.add("spike", [
        gem([(0.50, 0.0), (0.64, 0.14), (0.52, 0.62), (0.0, 1.0)], 6, R_PEAK_B, lean=(0.16, 0.0)),
        put(gem([(0.28, 0.0), (0.36, 0.08), (0.26, 0.30), (0.0, 0.48)], 5, R_SHARD, twist=0.7, lean=(-0.4, 0.2)), at=(-0.56, 0, 0.34)),
        lump((0.34, 0.06, 0.26), (0.40, 0.02, -0.36), R_SNOW, turn=(0, 20, 0)),
    ])
    return m.write(PALETTE)


def icespike_b():
    """A spike of the rings: twins, leaning apart, one a head taller."""
    m = model("icespike_b", "ice-atlas.png")
    m.add("spike", [
        put(gem([(0.40, 0.0), (0.52, 0.13), (0.42, 0.62), (0.0, 1.0)], 6, R_PEAK_A, twist=0.3, lean=(-0.26, 0.04)), at=(-0.30, 0, 0.0)),
        put(gem([(0.34, 0.0), (0.44, 0.10), (0.34, 0.46), (0.0, 0.74)], 5, R_SHARD, twist=1.5, lean=(0.42, 0.08)), at=(0.36, 0, 0.10)),
        lump((0.36, 0.06, 0.22), (0.0, 0.02, 0.52), R_SNOW, turn=(0, -8, 0)),
    ])
    return m.write(PALETTE)


def icespike_c():
    """A spike of the rings: a low cluster of three round a stubby middle one."""
    m = model("icespike_c", "ice-atlas.png")
    m.add("spike", [
        gem([(0.42, 0.0), (0.54, 0.15), (0.44, 0.64), (0.0, 1.0)], 6, R_PEAK_A, twist=0.1, lean=(-0.10, 0.0)),
        put(gem([(0.28, 0.0), (0.36, 0.08), (0.27, 0.32), (0.0, 0.52)], 5, R_SHARD, twist=0.9, lean=(0.52, 0.28)), at=(0.52, 0, 0.24)),
        put(gem([(0.25, 0.0), (0.32, 0.07), (0.24, 0.25), (0.0, 0.40)], 5, R_SHARD, twist=2.0, lean=(-0.62, 0.42)), at=(-0.50, 0, 0.30)),
        put(gem([(0.27, 0.0), (0.35, 0.09), (0.26, 0.38), (0.0, 0.62)], 5, R_SHARD, twist=3.1, lean=(0.08, -0.44)), at=(0.06, 0, -0.52)),
    ])
    return m.write(PALETTE)


def icechunks():
    """What the glacier breaks into: ten chunks, each its own node about its own middle so the stage throws and tumbles
    each one. Every chunk is a different broken shape: a stub, a wedge, a splinter, a block."""
    m = model("icechunks", "ice-atlas.png")
    m.add("chunk-0", [gem([(0.0, -0.46), (0.40, -0.10), (0.34, 0.22), (0.0, 0.50)], 5, R_CHUNK)])
    m.add("chunk-1", [gem([(0.26, -0.26), (0.44, -0.06), (0.40, 0.14), (0.20, 0.28)], 5, R_CHUNK, twist=0.5, squash_z=0.8)])
    m.add("chunk-2", [put(gem([(0.0, -0.52), (0.26, -0.12), (0.20, 0.20), (0.0, 0.56)], 4, R_SHARD, twist=0.8), turn=(0, 0, 70))])
    m.add("chunk-3", [gem([(0.20, -0.40), (0.34, -0.20), (0.30, 0.20), (0.12, 0.40)], 5, R_CHUNK, twist=1.1, lean=(0.12, 0.0))])
    m.add("chunk-4", [gem([(0.0, -0.36), (0.36, -0.04), (0.26, 0.30), (0.0, 0.64)], 6, R_PEAK_B)])
    m.add("chunk-5", [gem([(0.30, -0.18), (0.46, -0.04), (0.42, 0.10), (0.24, 0.20)], 5, R_CHUNK, twist=2.0, squash_z=1.2)])
    m.add("chunk-6", [gem([(0.0, -0.30), (0.34, 0.06), (0.22, 0.30), (0.0, 0.44)], 4, R_SHARD, twist=0.3)])
    m.add("chunk-7", [put(gem([(0.16, -0.44), (0.26, -0.20), (0.22, 0.22), (0.0, 0.50)], 5, R_CHUNK, twist=1.7), turn=(64, 0, 0))])
    m.add("chunk-8", [gem([(0.0, -0.42), (0.28, 0.0), (0.0, 0.46)], 4, R_SHARD, twist=1.0)])
    m.add("chunk-9", [gem([(0.22, -0.26), (0.38, -0.08), (0.36, 0.12), (0.16, 0.28)], 5, R_CHUNK, twist=2.6, lean=(-0.16, 0.1))])
    return m.write(PALETTE)


def frostpine():
    """A pine of the ridge behind her world, 3 m tall, snowed on: a stub of trunk and three round tiers each smaller than
    the one under it, the snow painted over the upper half of each, a lump of it on the tip and a drift at the foot. Round
    and cute, not a real tree."""
    m = model("frostpine", "ice-atlas.png")
    trunk = lathe([(0.15, 0.0), (0.13, 0.35), (0.11, 0.70)], seg=10)
    low = lathe([(0.0, 0.50), (0.86, 0.56), (1.02, 0.72), (0.74, 1.06), (0.30, 1.52)], seg=12)
    mid = lathe([(0.0, 1.20), (0.66, 1.26), (0.80, 1.40), (0.56, 1.74), (0.22, 2.20)], seg=12)
    top = lathe([(0.0, 1.92), (0.46, 1.98), (0.58, 2.10), (0.36, 2.48), (0.0, 3.00)], seg=12)
    m.add("tree", [
        (trunk, kit.grid_uv(trunk, inset(R_TRUNK), 10, "axis", 1)),
        (low, kit.grid_uv(low, inset(R_PINE), 12, "axis", 1)), (mid, kit.grid_uv(mid, inset(R_PINE), 12, "axis", 1)),
        (top, kit.grid_uv(top, inset(R_PINE), 12, "axis", 1)),
        lump((0.13, 0.12, 0.13), (0.0, 2.98, 0.0), R_SNOW), lump((0.52, 0.09, 0.42), (0.10, 0.03, 0.16), R_SNOW, turn=(0, 16, 0)),
    ])
    return m.write(PALETTE)


if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    builds = (("iceblock", iceblock), ("icecube", icecube), ("glacier", glacier), ("icespike_a", icespike_a),
              ("icespike_b", icespike_b), ("icespike_c", icespike_c), ("icechunks", icechunks), ("frostpine", frostpine))
    which = {a for a in sys.argv[1:] if not a.startswith("--")} or {name for name, _ in builds}
    paint_ice_atlas(OUT / "ice-atlas.png")
    paint_block_atlas(OUT / "iceblock-atlas.png")
    for name, build in builds:
        if name in which:
            build()
