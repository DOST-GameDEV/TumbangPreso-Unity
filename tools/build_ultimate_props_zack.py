"""The modelled props of Zack's ultimate cutscene (OVERCLOCK, "the bolt waits for him"): the five BUILDINGS of the city
his storm comes in over, and the three STORM CLOUDS above it.

    py -3 tools/build_ultimate_props_zack.py [--out=<folder>] [condo] [office] [mast] [flats] [row] [cloud_a] [cloud_b] [cloud_top]
    blender -b --python tools/review_hand_companion.py -- --hero=zack_condo --file=<folder>/zack_condo.glb --atlas=<folder>/zack_storm-atlas.png --version=v1
    py -3 tools/hand_companion_kit.py sheet zack_condo v1

Why: the skyline was sixteen plain prisms with one strip of window wedges and the clouds were five flat splats, and the
owner, once the staging stood: "continue polishing all the ultimates. i dont want to see anything bare in textures,
anything looking unpolished, no vfx etc."

⚠️ BUILT AS THE HAND COMPANIONS AND PAETE'S REWORKED PLANTS ARE (`tools/hand_companion_kit.py`,
`docs/CHARACTER_REDESIGN_DANTE.md` 15.9 and 15.11): round chunky forms, smooth normals, and a PAINTED ATLAS beside the
models. A first set made of flat single-colour prisms was replaced the same day: the owner had just refused a prop
built that way ("doesn't fit the hand painted illustrated stylistic art design of our characters and overall game
style"). So a building here is a soft-cornered block, and its front is a PAINTING: a night wall that pales upward, and
every window drawn with its own state, its own glow on the wall round it, a curtain or a blind in some. A cloud is a
heap of round masses, each painted pale and warm underneath, where the city lights it, to dark on top.

⚠️ EVERY PART AND EVERY MARK IS TYPED (owner, standing: "manually do each part of that builder dont js auto generate
looping shit"). Every window is its own line, so no two fronts are the same grid, and each building is a different
kind of building: a condo slab, an office with a water tank, a slim block with a mast, a block of flats with balconies
and a blank lit hoarding, and a low row of shop houses under a tin roof.

⚠️ ONLY THE FRONT IS PAINTED WITH WINDOWS. Every lens in the cutscene is inside the ring looking out. A block takes its
front's painting straight on (`facing`), so its sides carry the painting's plain shaded edge and its top the roof tone:
a drawing is only put on a face that squarely faces the view (the redesign's rule 8).

One atlas for all eight: `zack_storm-atlas.png`, 2048 square, paint in its upper half; the lower half is left for the
flat palette cells (steel, trim, the lamp), whose colours are `UltimateProp.StormPalette`
(`Runtime/Visual/UltimateProp.Zack.cs`), same order as PALETTE here.

Output: Assets/TumbangPreso/Resources/Models/UltimateProps/zack_*.glb, metres, +Y up, the front on +Z, the foot at y = 0
for a building and the underside at about y = 0 for a cloud.
"""
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import hand_companion_kit as kit  # noqa: E402
from hand_companion_kit import Model, ellipsoid, lathe, moved, rounded, tube  # noqa: E402

PALETTE = ["#3A4054", "#2C3040", "#20232D", "#E2B85C", "#94683A", "#191B23", "#5B6276", "#4D6680",
           "#FF5A4A", "#3D4252", "#282B36", "#535A70", "#6C7389", "#4C5162", "#8A4B3A", "#14161C"]
WALL, WALL_DK, ROOF, WIN, WIN_DIM, WIN_OFF, STEEL, TANK, LAMP, CLOUD, CLOUD_DK, BELLY, TRIM, TIN, AWNING, INK = range(16)

OUT = Path(next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--out=")), kit.ROOT / "Assets/TumbangPreso/Resources/Models/UltimateProps"))
ATLAS_NAME = "zack_storm-atlas.png"
ATLAS = 2048
PAD = 8


def px(x0, y0, x1, y1):
    return (x0 / ATLAS, y0 / ATLAS, x1 / ATLAS, y1 / ATLAS)


# Where each painting is in the atlas (pixels, y down). All of it is in the upper half.
R_CONDO, R_MAST, R_FLATS, R_OFFICE, R_ROW = px(0, 0, 320, 880), px(320, 0, 560, 1024), px(560, 0, 920, 640), px(920, 0, 1384, 512), px(1384, 0, 1896, 400)
R_TIN, R_TANK = px(1384, 420, 1896, 620), px(1384, 640, 1640, 900)
R_BELLY, R_BODY = px(1896, 0, 1972, 1024), px(1972, 0, 2048, 1024)
# What each front's painting spans on its building, metres: half its width, and its height.
S_CONDO, S_MAST, S_FLATS, S_OFFICE, S_ROW = (0.93, 5.12), (0.70, 6.30), (1.05, 3.70), (1.36, 2.96), (1.49, 1.95)


def inset(rect):
    return (rect[0] + PAD / ATLAS, rect[1] + PAD / ATLAS, rect[2] - PAD / ATLAS, rect[3] - PAD / ATLAS)


def facing(mesh, rect, span):
    """`mesh` with its front's painting put on it straight from the front: across is x, up is y. `span` is the
    painting's (half width, height) in metres. Sides and top take the painting's edge, which is painted plain."""
    u0, v0, u1, v1 = inset(rect)
    pos = mesh[0]
    u = np.clip((pos[:, 0] + span[0]) / (2 * span[0]), 0, 1)
    t = np.clip(pos[:, 1] / span[1], 0, 1)
    return (mesh, np.stack([u0 + (u1 - u0) * u, v1 - (v1 - v0) * t], axis=1).astype(np.float32))


def lying(mesh, rect, half_x, half_z):
    """A flat piece painted from above: across is x, up the painting is -z (so its eave, at +z, is the bottom)."""
    u0, v0, u1, v1 = inset(rect)
    pos = mesh[0]
    u = np.clip((pos[:, 0] + half_x) / (2 * half_x), 0, 1)
    t = np.clip((half_z - pos[:, 2]) / (2 * half_z), 0, 1)
    return (mesh, np.stack([u0 + (u1 - u0) * u, v1 - (v1 - v0) * t], axis=1).astype(np.float32))


def round_painted(mesh, rect, cols):
    return (mesh, kit.grid_uv(mesh, inset(rect), cols, "axis", 1))


def turned(piece, **how):
    return (moved(piece[0], **how), piece[1])


# ---------------------------------------------------------------- the paint

def hexc(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], np.float32)


def smooth01(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


class Front:
    """One building's front being painted, in METRES: x across from -half to +half, y up from the street."""

    def __init__(self, img, rect, span):
        self.img = img
        self.box = [int(round(v * ATLAS)) for v in rect]
        x0, y0, x1, y1 = self.box
        self.half, self.height = span
        # The painting proper is the inset rect; the padding round it repeats its edge so the filter never bleeds.
        xs = (np.arange(x0, x1) + .5 - (x0 + PAD)) / (x1 - x0 - 2 * PAD)
        ys = 1.0 - (np.arange(y0, y1) + .5 - (y0 + PAD)) / (y1 - y0 - 2 * PAD)
        self.X, self.Y = np.meshgrid((np.clip(xs, 0, 1) * 2 - 1) * self.half, np.clip(ys, 0, 1) * self.height)
        self.c = np.zeros(self.X.shape + (3,), np.float32)
        self.glow = np.zeros(self.X.shape, np.float32)

    def mix(self, colour, weight):
        self.c = self.c * (1 - weight[..., None]) + hexc(colour) * weight[..., None]

    def wall(self, foot, top, light):
        """The wall: `foot` at the street paling to `top`, a soft wash of `light` falling across it from the upper
        left, and a shade down each edge, which is also what the block's sides wear."""
        self.c[:] = hexc(foot)
        self.mix(top, smooth01(0.0, self.height, self.Y))
        wash = np.exp(-(((self.X + self.half * .45) / (self.half * 1.1)) ** 2 + ((self.Y - self.height * .78) / (self.height * .5)) ** 2))
        self.mix(light, wash * .32)
        self.mix("#1E2130", smooth01(self.half * .86, self.half, np.abs(self.X)) * .5)
        self.mix("#1B1D29", (1 - smooth01(0.0, 0.34, self.Y)) * .45)          # the street's own dark at its foot

    def band(self, y, thick, colour, weight=.8):
        """A soft horizontal mark: a ledge's shadow, a floor line."""
        self.mix(colour, np.exp(-((self.Y - y) / thick) ** 2) * weight)

    def block(self, x, y, w, h, colour, weight=1.0, soft=.02):
        """A soft-edged patch (a door, a shop front's panel, a setback's own tone)."""
        m = (1 - smooth01(w / 2 - soft, w / 2 + soft, np.abs(self.X - x))) * (1 - smooth01(h / 2 - soft, h / 2 + soft, np.abs(self.Y - y)))
        self.mix(colour, m * weight)
        return m

    def streak(self, x, top, length, weight=.35):
        """Rain's stain running down from a ledge: widest where it starts, gone by its end."""
        down = np.clip((top - self.Y) / length, 0, 1)
        m = np.exp(-((self.X - x) / (.022 + .02 * down)) ** 2) * (self.Y < top) * (1 - down) ** 1.5
        self.mix("#1F2231", m * weight)

    def window(self, x, y, w, h, state, dress="", glow=1.0):
        """One window. `state`: "lit" (a warm room), "dim" (a lamp on somewhere inside) or "off" (dark glass with the
        sky in it). `dress`: "curtain" (drawn across one side), "blind" (half down), "bars" (panes across as well as
        up). A lit one puts its light on the wall round it."""
        dx, dy = self.X - x, self.Y - y
        pane = (1 - smooth01(w / 2 - .012, w / 2 + .012, np.abs(dx))) * (1 - smooth01(h / 2 - .012, h / 2 + .012, np.abs(dy)))
        frame = (1 - smooth01(w / 2 + .02, w / 2 + .045, np.abs(dx))) * (1 - smooth01(h / 2 + .02, h / 2 + .045, np.abs(dy)))
        self.mix("#232636", frame * .7)                                        # the reveal it sits in
        up = np.clip(dy / h + .5, 0, 1)
        if state == "off":
            self.mix("#181B27", pane)
            self.mix("#3B4462", pane * np.exp(-((dx * .9 + dy - .04) / .05) ** 2) * .55)   # the sky, across the glass
            self.mix("#2A3048", pane * smooth01(.5, 1.0, up) * .4)
        else:
            lo, hi = ("#F0A04A", "#FFE39A") if state == "lit" else ("#6E4E36", "#A9814E")
            self.mix(lo, pane)
            self.mix(hi, pane * smooth01(0.1, 1.0, up) * .85)
            if dress == "curtain":
                self.mix("#B86A3C" if state == "lit" else "#5A3E2C", pane * smooth01(-.02, .05, dx - w * .08 + dy * .25) * .8)
            if dress == "blind":
                self.mix("#D9B27A" if state == "lit" else "#7A6248", pane * smooth01(-.01, .02, dy - h * .05) * .85)
                self.mix("#8A5A2E", pane * smooth01(-.01, .02, dy - h * .05) * (np.cos(dy * 2 * np.pi / .045) > .6) * .35)
            self.mix("#7A4A26", pane * np.exp(-(dx / .011) ** 2) * .75)        # the mullion
            if dress == "bars" or h > .5:
                self.mix("#7A4A26", pane * np.exp(-((dy - h * .08) / .011) ** 2) * .7)
            strength = (.55 if state == "lit" else .16) * glow
            self.glow += np.exp(-((dx / (w * .5 + .16)) ** 2 + (dy / (h * .5 + .16)) ** 2)) * strength
        self.mix("#7C849C", (1 - smooth01(w / 2 + .03, w / 2 + .05, np.abs(dx))) * np.exp(-((dy + h / 2 + .035) / .014) ** 2) * .75)   # the sill

    def done(self):
        x0, y0, x1, y1 = self.box
        lit = self.c + self.glow[..., None] * np.array([150, 92, 30], np.float32)
        self.img[y0:y1, x0:x1] = np.clip(lit, 0, 255)


def paint_condo(img):
    f = Front(img, R_CONDO, S_CONDO)
    f.wall("#343A50", "#566080", "#6E7AA0")
    f.block(0.0, 0.18, 1.90, 0.36, "#262A3A", .9)                              # the plinth
    f.block(0.0, 4.92, 1.90, 0.50, "#3C435C", .85)                             # the crown's own tone
    f.band(0.90, .02, "#232636", .6); f.band(4.56, .03, "#232636", .7)
    f.block(0.0, 0.52, 0.46, 0.60, "#8A6A48", 1.0)                             # the lobby door, a lamp behind it
    f.glow += np.exp(-((f.X / .5) ** 2 + ((f.Y - .5) / .5) ** 2)) * .3
    # Six floors, three windows each, every one typed with its own state.
    f.window(-0.46, 1.22, 0.30, 0.36, "off")
    f.window(0.00, 1.22, 0.30, 0.36, "lit", "curtain")
    f.window(0.46, 1.22, 0.30, 0.36, "off")
    f.window(-0.46, 1.80, 0.30, 0.36, "lit")
    f.window(0.00, 1.80, 0.30, 0.36, "off")
    f.window(0.46, 1.80, 0.30, 0.36, "dim", "blind")
    f.window(-0.46, 2.38, 0.30, 0.36, "off")
    f.window(0.00, 2.38, 0.30, 0.36, "off")
    f.window(0.46, 2.38, 0.30, 0.36, "lit", "blind")
    f.window(-0.46, 2.96, 0.30, 0.36, "dim")
    f.window(0.00, 2.96, 0.30, 0.36, "lit")
    f.window(0.46, 2.96, 0.30, 0.36, "off")
    f.window(-0.46, 3.54, 0.30, 0.36, "lit", "curtain")
    f.window(0.00, 3.54, 0.30, 0.36, "off")
    f.window(0.46, 3.54, 0.30, 0.36, "off")
    f.window(-0.46, 4.12, 0.30, 0.36, "off")
    f.window(0.00, 4.12, 0.30, 0.36, "dim", "curtain")
    f.window(0.46, 4.12, 0.30, 0.36, "lit")
    f.window(0.0, 4.92, 0.34, 0.20, "dim")                                     # the lift room in the crown
    f.streak(-0.70, 4.50, 1.4); f.streak(0.23, 4.50, 0.9); f.streak(0.72, 4.50, 2.0); f.streak(-0.22, 0.88, 0.4)
    f.done()


def paint_mast(img):
    f = Front(img, R_MAST, S_MAST)
    f.wall("#30364C", "#55607F", "#6A77A0")
    f.block(0.0, 5.66, 1.50, 1.00, "#3A4058", .8)                              # the top stage is the darkest
    f.band(3.20, .03, "#222535", .7); f.band(5.08, .03, "#222535", .7)
    # The slits: two up the foot, three up the middle, each broken where a floor is dark.
    f.window(-0.32, 0.90, 0.20, 0.90, "lit", "bars")
    f.window(-0.32, 2.05, 0.20, 1.10, "off")
    f.window(0.32, 0.72, 0.20, 0.54, "off")
    f.window(0.32, 1.52, 0.20, 0.82, "dim", "bars")
    f.window(0.32, 2.52, 0.20, 0.86, "lit", "blind")
    f.window(0.0, 3.86, 0.20, 0.80, "off")
    f.window(0.0, 4.62, 0.20, 0.46, "lit")
    f.window(-0.30, 4.20, 0.12, 1.40, "off")
    f.window(0.30, 4.20, 0.12, 1.40, "dim")
    f.window(0.0, 5.70, 0.28, 0.28, "lit", "bars")                             # one square window at the very top
    f.streak(-0.50, 3.14, 1.6); f.streak(0.08, 3.14, 0.7); f.streak(0.40, 5.04, 0.8); f.streak(-0.18, 6.1, 0.5)
    f.done()


def paint_flats(img):
    f = Front(img, R_FLATS, S_FLATS)
    f.wall("#3A3A50", "#5E6282", "#7C7FA6")
    f.block(0.0, 1.80, 0.20, 3.70, "#474C68", .9, soft=.03)                    # the stair core up the middle
    f.window(0.0, 0.46, 0.12, 0.32, "dim")                                     # its landing lights, floor by floor
    f.window(0.0, 1.28, 0.12, 0.32, "lit")
    f.window(0.0, 2.10, 0.12, 0.32, "off")
    f.window(0.0, 2.92, 0.12, 0.32, "dim")
    # Four floors, a window each side of the core.
    f.window(-0.56, 0.52, 0.56, 0.44, "off")
    f.window(0.56, 0.52, 0.56, 0.44, "lit", "curtain")
    f.window(-0.56, 1.34, 0.56, 0.44, "lit", "blind")
    f.window(0.56, 1.34, 0.56, 0.44, "off")
    f.window(-0.56, 2.16, 0.56, 0.44, "dim", "curtain")
    f.window(0.56, 2.16, 0.56, 0.44, "lit")
    f.window(-0.56, 2.98, 0.56, 0.44, "off")
    f.window(0.56, 2.98, 0.56, 0.44, "off")
    f.band(3.56, .04, "#25283A", .7)
    f.streak(-0.86, 3.5, 1.2); f.streak(0.30, 3.5, 0.8); f.streak(0.90, 3.5, 1.7); f.streak(-0.25, 1.0, 0.5)
    f.done()


def paint_office(img):
    f = Front(img, R_OFFICE, S_OFFICE)
    f.wall("#333B52", "#52607E", "#6C7CA2")
    # The ground floor, shuttered, with one unit still open.
    f.block(0.0, 0.32, 2.30, 0.50, "#242838", .9)
    f.mix("#30364C", f.block(0.0, 0.32, 2.30, 0.50, "#242838", 0) * (np.cos(f.Y * 2 * np.pi / .07) > .3) * .5)   # the shutters' slats
    f.window(-0.62, 0.32, 0.50, 0.44, "lit", "bars")
    # Three glazed strips: dark glass end to end, with the lights left on in typed stretches of it.
    f.window(0.0, 1.00, 2.30, 0.30, "off")
    f.window(0.55, 1.00, 0.96, 0.26, "lit", "blind")
    f.window(0.0, 1.62, 2.30, 0.30, "off")
    f.window(-0.72, 1.62, 0.70, 0.26, "lit")
    f.window(0.86, 1.62, 0.38, 0.26, "dim")
    f.window(0.0, 2.24, 2.30, 0.30, "off")
    f.window(-0.10, 2.24, 1.20, 0.26, "dim", "blind")
    f.band(2.74, .04, "#232636", .7)
    f.streak(-1.0, 2.7, 0.5); f.streak(0.2, 2.7, 0.35); f.streak(1.1, 2.7, 0.6); f.streak(-0.3, 0.74, 0.3)
    f.done()


def paint_row(img):
    f = Front(img, R_ROW, S_ROW)
    f.wall("#3E3A4C", "#665F78", "#857C9A")
    f.band(1.02, .025, "#262838", .8)                                          # the line between the shop and the rooms
    # The shop: its wide lit front in two panes, the door beside it, a small window at the end.
    f.window(-0.88, 0.50, 0.62, 0.70, "lit", "bars", glow=1.2)
    f.window(-0.24, 0.50, 0.62, 0.70, "lit", "curtain", glow=1.2)
    f.window(0.60, 0.46, 0.36, 0.84, "off")
    f.window(1.08, 0.52, 0.34, 0.44, "dim", "bars")
    # The rooms above: three windows.
    f.window(-0.90, 1.44, 0.42, 0.40, "dim", "curtain")
    f.window(0.0, 1.44, 0.42, 0.40, "off")
    f.window(0.90, 1.44, 0.42, 0.40, "lit", "blind")
    f.band(1.86, .05, "#232636", .7)                                           # the eave's shadow
    f.streak(-1.2, 1.8, 0.4); f.streak(0.45, 1.8, 0.55); f.streak(1.3, 1.8, 0.35)
    f.done()


def paint_tin(img):
    """The tin roof from above: corrugations running down it, rust coming up from the eave, one patched sheet."""
    x0, y0, x1, y1 = [int(round(v * ATLAS)) for v in R_TIN]
    u, t = np.meshgrid((np.arange(x0, x1) + .5 - x0) / (x1 - x0), 1.0 - (np.arange(y0, y1) + .5 - y0) / (y1 - y0))
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#4A5064")
    mixed = lambda base, colour, w: base * (1 - w[..., None]) + hexc(colour) * w[..., None]
    c = mixed(c, "#6A728C", (0.5 + 0.5 * np.cos(u * 2 * np.pi * 26)) ** 2 * .6)             # the corrugations
    c = mixed(c, "#7A4A3A", (1 - smooth01(0.0, 0.45, t)) * (0.5 + 0.5 * np.cos(u * 2 * np.pi * 3.0 + 1.0)) * .6)   # rust at the eave
    c = mixed(c, "#39405A", ((u > .58) & (u < .74) & (t > .35)) * .55)                     # the patched sheet
    c = mixed(c, "#2A2E3E", smooth01(.9, 1.0, t) * .6)                                      # shade under the ridge
    img[y0:y1, x0:x1] = c


def paint_tank(img):
    """A water tank's drum, round it (u) and up it (t): a pale side toward the light, two hoops, rust under them."""
    x0, y0, x1, y1 = [int(round(v * ATLAS)) for v in R_TANK]
    u, t = np.meshgrid((np.arange(x0, x1) + .5 - x0) / (x1 - x0), 1.0 - (np.arange(y0, y1) + .5 - y0) / (y1 - y0))
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#3E546C")
    mixed = lambda base, colour, w: base * (1 - w[..., None]) + hexc(colour) * w[..., None]
    c = mixed(c, "#5F7C98", smooth01(0.0, 1.0, t) * .7)
    c = mixed(c, "#8FA9C0", (0.5 + 0.5 * np.cos((u - 0.30) * 2 * np.pi)) ** 4 * .5)
    c = mixed(c, "#2B3A4C", np.exp(-((t - .30) / .03) ** 2) * .85)
    c = mixed(c, "#2B3A4C", np.exp(-((t - .68) / .03) ** 2) * .85)
    c = mixed(c, "#8A5A44", np.exp(-((t - .22) / .05) ** 2) * (0.5 + 0.5 * np.cos(u * 2 * np.pi * 2 + .7)) ** 3 * .6)
    c = mixed(c, "#8A5A44", np.exp(-((t - .60) / .05) ** 2) * (0.5 + 0.5 * np.cos(u * 2 * np.pi * 3 + 2.1)) ** 3 * .5)
    img[y0:y1, x0:x1] = c


def paint_cloud(img, rect, under, low, middle, top):
    """One mass of cloud, round it (u) and up it (t). Its underside is pale and warm, where the city and the storm's own
    light reach it; it darkens up its flank and catches a little sky on its crown. Soft uneven patches, which join
    across the seam, keep it from being a plain gradient."""
    x0, y0, x1, y1 = [int(round(v * ATLAS)) for v in rect]
    u, t = np.meshgrid((np.arange(x0, x1) + .5 - x0) / (x1 - x0), 1.0 - (np.arange(y0, y1) + .5 - y0) / (y1 - y0))
    c = np.zeros(u.shape + (3,), np.float32) + hexc(under)
    mixed = lambda base, colour, w: base * (1 - w[..., None]) + hexc(colour) * w[..., None]
    c = mixed(c, low, smooth01(0.04, 0.30, t))
    c = mixed(c, middle, smooth01(0.26, 0.58, t))
    c = mixed(c, top, smooth01(0.66, 1.0, t) * .8)
    patch = 0.5 + 0.25 * np.cos(u * 2 * np.pi * 2 + t * 5.0) + 0.25 * np.cos(u * 2 * np.pi * 3 - t * 8.0 + 1.3)
    c = mixed(c, "#1E2130", smooth01(.55, .95, patch) * .22)
    c = mixed(c, "#8088A8", smooth01(.55, .95, 1 - patch) * .12 * (1 - smooth01(.5, .9, t)))
    img[y0:y1, x0:x1] = c


def paint_atlas(path):
    from PIL import Image, ImageFilter
    img = np.zeros((ATLAS, ATLAS, 3), np.float32)
    img[:] = hexc(PALETTE[WALL])
    paint_condo(img); paint_mast(img); paint_flats(img); paint_office(img); paint_row(img)
    paint_tin(img); paint_tank(img)
    paint_cloud(img, R_BELLY, "#B9A6A8", "#8B88A6", "#4C5068", "#565C78")
    paint_cloud(img, R_BODY, "#6C6E8C", "#454962", "#2C3042", "#4A506A")
    Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), "RGB").filter(ImageFilter.GaussianBlur(0.7)).save(path)
    return path


# ---------------------------------------------------------------- the models

def model(name):
    m = Model(name)
    m.folder = OUT
    m.texture = ATLAS_NAME
    return m


def tank(at, radius, height):
    """A water tank: a painted drum, and a low cone of a lid."""
    drum = lathe([(radius * .94, 0.0), (radius, height * .12), (radius, height * .88), (radius * .94, height)], seg=16)
    lid = lathe([(radius * 1.06, 0.0), (radius * .7, height * .2), (0.0, height * .34)], seg=16, at=(0, height, 0))
    return [turned(round_painted(drum, R_TANK, 16), at=at), (moved(lid, at=at), ROOF)]


def condo():
    """A condo slab, 1.7 m wide and 5.1 m to its cap: three windows across and six floors, a stepped crown."""
    m = model("zack_condo")
    m.add("block", [
        facing(rounded((0.93, 0.18, 0.62), 0.07, at=(0.0, 0.18, 0.0)), R_CONDO, S_CONDO),          # the plinth
        facing(rounded((0.85, 2.14, 0.55), 0.09, at=(0.0, 2.48, 0.0)), R_CONDO, S_CONDO),          # the slab
        (rounded((0.91, 0.06, 0.61), 0.05, at=(0.0, 4.66, 0.0)), TRIM),                             # the cornice
        facing(rounded((0.63, 0.20, 0.40), 0.08, at=(0.0, 4.92, -0.05)), R_CONDO, S_CONDO),        # the crown, set back
        (rounded((0.45, 0.05, 0.28), 0.04, at=(0.0, 5.13, -0.05)), ROOF),
        (rounded((0.15, 0.13, 0.15), 0.05, at=(0.36, 5.28, -0.10)), WALL_DK),                       # the lift head
        (rounded((0.36, 0.035, 0.13), 0.03, at=(0.0, 0.86, 0.62)), TRIM),                           # the canopy over the door
    ])
    m.write(PALETTE)


def office():
    """A wide office block, 2.6 m across and 2.9 m to its parapet, glazed in long strips with the lights left on in
    stretches of them, and a water tank on legs on its roof."""
    m = model("zack_office")
    m.add("block", [
        facing(rounded((1.30, 1.40, 0.60), 0.10, at=(0.0, 1.40, 0.0)), R_OFFICE, S_OFFICE),
        (rounded((1.36, 0.07, 0.66), 0.05, at=(0.0, 2.86, 0.0)), TRIM),                             # the parapet
        # The water tank: four legs, a deck, the drum.
        (tube([(-1.02, 2.90, 0.22), (-1.00, 3.50, 0.20)], 0.035, seg=8), STEEL),
        (tube([(-0.50, 2.90, 0.22), (-0.52, 3.50, 0.20)], 0.035, seg=8), STEEL),
        (tube([(-1.02, 2.90, -0.26), (-1.00, 3.50, -0.24)], 0.035, seg=8), STEEL),
        (tube([(-0.50, 2.90, -0.26), (-0.52, 3.50, -0.24)], 0.035, seg=8), STEEL),
        (rounded((0.36, 0.035, 0.34), 0.03, at=(-0.76, 3.52, -0.02)), STEEL),
        # Two air conditioners and a duct on the other half of the roof.
        (rounded((0.22, 0.14, 0.17), 0.05, at=(0.52, 3.06, 0.20)), WALL_DK),
        (rounded((0.15, 0.09, 0.02), 0.015, at=(0.52, 3.06, 0.375)), STEEL),
        (rounded((0.17, 0.10, 0.15), 0.04, at=(1.00, 3.02, -0.24)), WALL_DK),
        (tube([(0.74, 3.00, 0.20), (0.76, 3.02, -0.05), (0.84, 3.00, -0.24)], 0.05, seg=8), STEEL),
    ] + tank((-0.76, 3.55, -0.02), 0.31, 0.56))
    m.write(PALETTE)


def mast():
    """A slim block in three setbacks, 6.2 m to its roof, lit in tall slits, with a mast and its red light."""
    m = model("zack_mast")
    m.add("block", [
        facing(rounded((0.65, 1.60, 0.50), 0.09, at=(0.0, 1.60, 0.0)), R_MAST, S_MAST),             # the foot
        (rounded((0.70, 0.06, 0.55), 0.05, at=(0.0, 3.26, 0.0)), TRIM),
        facing(rounded((0.51, 0.90, 0.41), 0.08, at=(0.0, 4.20, -0.04)), R_MAST, S_MAST),           # the middle
        (rounded((0.56, 0.05, 0.46), 0.04, at=(0.0, 5.14, -0.04)), TRIM),
        facing(rounded((0.35, 0.48, 0.31), 0.07, at=(0.0, 5.66, -0.08)), R_MAST, S_MAST),           # the top
        (rounded((0.40, 0.045, 0.36), 0.04, at=(0.0, 6.18, -0.08)), ROOF),
        # The mast: a pole that thins, three cross arms that shorten as it climbs, a dish, the lamp.
        (tube([(0.0, 6.20, -0.08), (0.01, 6.90, -0.08), (0.0, 7.62, -0.08)], [0.045, 0.034, 0.02], seg=8), STEEL),
        (tube([(-0.23, 6.52, -0.08), (0.23, 6.52, -0.08)], 0.02, seg=6), STEEL),
        (tube([(-0.17, 6.86, -0.08), (0.17, 6.86, -0.08)], 0.018, seg=6), STEEL),
        (tube([(-0.11, 7.18, -0.08), (0.11, 7.18, -0.08)], 0.016, seg=6), STEEL),
        (moved(ellipsoid((0.11, 0.11, 0.035)), turn=(-18, 24, 0), at=(0.17, 6.68, 0.0)), TRIM),
        (ellipsoid((0.065, 0.065, 0.065), at=(0.0, 7.68, -0.08)), LAMP),
    ])
    m.write(PALETTE)


def flats():
    """A block of flats, 2.0 m wide and 3.6 m high: two windows a floor with a balcony under the lit ones, a stair head
    and a blank lit hoarding on the roof."""
    m = model("zack_flats")
    m.add("block", [
        facing(rounded((1.00, 1.80, 0.55), 0.09, at=(0.0, 1.80, 0.0)), R_FLATS, S_FLATS),
        (rounded((1.05, 0.05, 0.60), 0.04, at=(0.0, 3.64, 0.0)), ROOF),
        # A balcony under each lit window: a slab and a rail, each typed.
        (rounded((0.36, 0.035, 0.14), 0.03, at=(0.56, 0.25, 0.66)), TRIM),
        (rounded((0.36, 0.085, 0.018), 0.015, at=(0.56, 0.37, 0.79)), STEEL),
        (rounded((0.36, 0.035, 0.14), 0.03, at=(-0.56, 1.07, 0.66)), TRIM),
        (rounded((0.36, 0.085, 0.018), 0.015, at=(-0.56, 1.19, 0.79)), STEEL),
        (rounded((0.36, 0.035, 0.14), 0.03, at=(0.56, 1.89, 0.66)), TRIM),
        (rounded((0.36, 0.085, 0.018), 0.015, at=(0.56, 2.01, 0.79)), STEEL),
        (rounded((0.36, 0.035, 0.14), 0.03, at=(-0.56, 1.89, 0.66)), TRIM),
        (rounded((0.36, 0.085, 0.018), 0.015, at=(-0.56, 2.01, 0.79)), STEEL),
        # The roof: the stair head, and a hoarding on two posts with nothing on it but its own light.
        (rounded((0.25, 0.23, 0.25), 0.06, at=(-0.62, 3.92, -0.14)), WALL_DK),
        (rounded((0.29, 0.03, 0.29), 0.025, at=(-0.62, 4.17, -0.14)), ROOF),
        (tube([(0.26, 3.66, 0.10), (0.26, 4.24, 0.10)], 0.025, seg=6), STEEL),
        (tube([(0.82, 3.66, 0.10), (0.82, 4.24, 0.10)], 0.025, seg=6), STEEL),
        (rounded((0.43, 0.23, 0.03), 0.025, at=(0.54, 4.42, 0.12)), TRIM),
        (rounded((0.38, 0.18, 0.015), 0.012, at=(0.54, 4.42, 0.15)), WIN_DIM),
    ])
    m.write(PALETTE)


def row():
    """A low row of shop houses, 2.8 m wide under a tin roof: a lit shopfront under an awning, a door, the rooms above,
    and a drum of a water tank at one end of the ridge."""
    m = model("zack_row")
    leaf = rounded((1.48, 0.035, 0.42), 0.03)
    m.add("block", [
        facing(rounded((1.40, 0.95, 0.60), 0.09, at=(0.0, 0.95, 0.0)), R_ROW, S_ROW),
        # The tin roof: a leaf down to the front and one to the back, and the ridge.
        turned(lying(leaf, R_TIN, 1.48, 0.42), turn=(26, 0, 0), at=(0.0, 2.08, 0.36)),
        turned(lying(leaf, R_TIN, 1.48, 0.42), turn=(26, 180, 0), at=(0.0, 2.08, -0.36)),
        (tube([(-1.49, 2.27, 0.0), (1.49, 2.27, 0.0)], 0.05, seg=8), ROOF),
        (moved(rounded((0.73, 0.025, 0.22), 0.02), turn=(16, 0, 0), at=(-0.56, 0.98, 0.80)), AWNING),   # the shop's awning
        (rounded((0.25, 0.13, 0.23), 0.04, at=(1.00, 2.34, -0.10)), STEEL),                              # the tank's stand
    ] + tank((1.00, 2.47, -0.10), 0.23, 0.50))
    m.write(PALETTE)


def mass(half, at, rect, turn=(0, 0, 0)):
    """One round mass of a cloud, painted underside to crown."""
    return turned(round_painted(ellipsoid(half, seg=16, rings=10), rect, 16), turn=turn, at=at)


def cloud_a():
    """A storm cloud, 4.4 m long, built to be seen from underneath: a dark body, six bulges of different sizes hanging
    under it, and three heads on top. Its lowest bulge is at about y = 0."""
    m = model("zack_cloud_a")
    m.add("mass", [
        mass((1.95, 0.70, 1.15), (0.0, 0.95, 0.0), R_BODY),
        mass((0.90, 0.50, 0.80), (-1.20, 0.52, 0.20), R_BELLY, (0, 20, 0)),
        mass((1.00, 0.52, 0.85), (-0.20, 0.42, -0.30), R_BELLY, (0, -15, 0)),
        mass((0.95, 0.50, 0.80), (0.90, 0.50, 0.25), R_BELLY, (0, 10, 0)),
        mass((0.60, 0.38, 0.55), (1.62, 0.64, -0.20), R_BELLY),
        mass((0.55, 0.34, 0.50), (-1.82, 0.66, -0.10), R_BELLY),
        mass((0.60, 0.36, 0.50), (0.20, 0.40, 0.58), R_BELLY, (8, 0, 0)),
        mass((1.10, 0.60, 0.90), (-0.50, 1.46, 0.00), R_BODY),
        mass((0.90, 0.50, 0.75), (0.90, 1.40, 0.10), R_BODY, (0, 0, 8)),
        mass((0.70, 0.40, 0.60), (-1.50, 1.16, -0.10), R_BODY, (0, 0, -10)),
    ])
    m.write(PALETTE)


def cloud_b():
    """A longer, lower one, 5.6 m: it leans to one end, hangs seven bulges and trails a wisp from the other end."""
    m = model("zack_cloud_b")
    m.add("mass", [
        mass((2.30, 0.62, 1.05), (0.40, 0.92, 0.0), R_BODY, (0, 6, 0)),
        mass((1.00, 0.52, 0.82), (1.50, 0.50, 0.20), R_BELLY, (0, -18, 0)),
        mass((0.90, 0.48, 0.80), (0.50, 0.40, -0.25), R_BELLY),
        mass((0.85, 0.46, 0.72), (-0.50, 0.46, 0.20), R_BELLY, (0, 24, 0)),
        mass((0.75, 0.40, 0.62), (-1.40, 0.58, -0.10), R_BELLY),
        mass((0.55, 0.34, 0.50), (2.30, 0.66, -0.15), R_BELLY),
        mass((0.50, 0.30, 0.42), (0.95, 0.36, 0.55), R_BELLY, (-8, 0, 0)),
        mass((0.48, 0.30, 0.40), (-0.10, 0.44, -0.62), R_BELLY),
        mass((0.62, 0.26, 0.44), (-2.30, 0.72, 0.05), R_BODY, (0, 0, 14)),
        mass((0.30, 0.14, 0.22), (-2.90, 0.82, 0.08), R_BODY),
        mass((1.20, 0.62, 0.90), (1.30, 1.36, 0.00), R_BODY, (0, 0, -12)),
        mass((0.90, 0.46, 0.70), (0.00, 1.24, 0.15), R_BODY),
    ])
    m.write(PALETTE)


def cloud_top():
    """The cloud the bolt comes out of, 8 m across, seen from underneath: a ring of seven hanging bulges round a dark
    throat in its middle, under one heavy body with four heads."""
    m = model("zack_cloud_top")
    m.add("mass", [
        mass((3.90, 0.80, 3.30), (0.0, 1.10, 0.0), R_BODY),
        (ellipsoid((1.00, 0.34, 0.92), at=(0.0, 0.62, 0.0)), INK),                                  # the throat
        mass((1.15, 0.62, 1.00), (1.75, 0.62, 0.30), R_BELLY, (0, 20, 0)),
        mass((1.05, 0.56, 0.90), (0.85, 0.54, 1.62), R_BELLY, (0, -30, 0)),
        mass((1.10, 0.60, 0.95), (-0.95, 0.60, 1.55), R_BELLY, (0, 35, 0)),
        mass((1.05, 0.58, 0.95), (-1.95, 0.56, -0.05), R_BELLY),
        mass((1.00, 0.56, 0.90), (-0.85, 0.54, -1.62), R_BELLY, (0, -25, 0)),
        mass((1.15, 0.60, 0.95), (1.05, 0.60, -1.50), R_BELLY, (0, 15, 0)),
        mass((0.70, 0.42, 0.62), (2.70, 0.74, 1.50), R_BELLY),
        mass((0.90, 0.40, 0.80), (3.10, 0.90, -0.60), R_BODY, (0, 0, 10)),                          # ragged ends past the ring
        mass((0.85, 0.38, 0.75), (-3.20, 0.92, 0.70), R_BODY, (0, 0, -8)),
        mass((2.20, 0.74, 1.90), (-0.60, 1.86, 0.40), R_BODY),
        mass((1.50, 0.64, 1.30), (1.70, 1.94, -0.70), R_BODY),
        mass((1.30, 0.52, 1.10), (-2.00, 1.72, -1.00), R_BODY),
        mass((1.20, 0.52, 1.00), (0.60, 2.42, 0.20), R_BODY),
    ])
    m.write(PALETTE)


if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    builders = (("condo", condo), ("office", office), ("mast", mast), ("flats", flats), ("row", row),
                ("cloud_a", cloud_a), ("cloud_b", cloud_b), ("cloud_top", cloud_top))
    which = {a for a in sys.argv[1:] if not a.startswith("--")} or {name for name, _ in builders}
    paint_atlas(OUT / ATLAS_NAME)
    print("painted", OUT / ATLAS_NAME)
    for name, build in builders:
        if name in which:
            build()
