"""Soraya's BARE first-person hands (hero id `phaister`): a stage magician's props that live in her sleeves and cuffs.

    py -3 tools/build_hands_phaister.py                 the model the game loads (run this one LAST)
    py -3 tools/build_hands_phaister.py --pose=rest     a review bake: her two arms with the props on them, as the game's
                                                        first-person camera frames them (rest, acts, fall, sprint, limp, carry)

Writes Assets/TumbangPreso/Resources/Models/HandCompanions/phaister_hands.glb. The C# side is
`Runtime/Camera/SorayaStageHands.cs`, which takes every part out of the model and places it itself, each frame, in the
arms' own space. So where a part sits in THIS file is only a tidy table for the review: what matters is each part's
shape and where its ORIGIN is.

No creature, no face. Hers, not pets:
    scarf-0 .. scarf-4   a silk scarf in her magenta, tucked in the LEFT cuff. Five flat tapered cloth plates, 0 the corner
                         (a gold bead on its point) and 4 the last to leave the cuff. Each one's origin is its joint on the
                         CUFF side and it runs along +y towards the corner, so it can be drawn out, trail and be whisked in.
    wand-a, wand-b       her two wands, in the LEFT purple band. Origin at the pommel, the crystal up +y: they slide along
                         their own length out of the band and back.
    strap                the little strap on the band they are tucked under. Origin on the band's face, +y off the face.
    card-0 .. card-3     four playing cards that spring from between the RIGHT knuckles. Origin at the middle of the bottom
                         edge (the hinge of the fan), +y up the card, the face on +z and HER BACK DESIGN on -z.
    front                a speck off to +x and +z: the C# side reads where it landed to learn how the importer mirrored
                         the model (only the cards care: every other part is the same on both sides).

Everything is typed in the ARM's own units (her first-person arm is 0.84 long, a fist is 0.3) and written at a quarter of
that, in metres, as the kit asks; `SorayaStageHands.Scale` is the 4 that brings it back.

Every part is the same left and right of its own x, on purpose: the importer mirrors one axis, and a part that is its own
mirror cannot come in backwards.
"""
import math
import re
import struct
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import hand_companion_kit as kit  # noqa: E402
from hand_companion_kit import ellipsoid, join, lathe, moved, slab  # noqa: E402

# The sixteen slots. `SorayaStageHands.Palette` carries the same colours in the same order: her witch accent, and
# `PhaisterProp.Palette`'s gold, wood, crimson, bone and violets. The last two are her arm's own band and her skin
# (the review bakes paint the arm with them; no part uses them).
ACCENT, ACCENT_LIGHT, VIOLET, DEEP, INK, GOLD, GOLD_DARK, PINK, BONE, LILAC, CRIMSON, WOOD, NIGHT, MAGENTA, BAND, SKIN = range(16)
PALETTE = ["E828C5", "F444D4", "9838D8", "3E1F6E", "14101C", "F8B824", "C88A10", "FF6AB8",
           "F2E6DA", "C9A2F0", "8C1424", "A8683C", "1A1020", "E0287E", "4A1E78", "E0A078"]

SCALE = 4.0                 # arm units to one of the model's metres: `SorayaStageHands.Scale`

# ------------------------------------------------------------------ the scarf

SEGMENTS = 5
LINK = .095                 # one plate's length: `SorayaStageHands.Link`
HALF_CLOTH = .0085
# The scarf's width at each joint, from the corner's point to the end that stays in the cuff.
WIDTHS = [.036, .105, .140, .156, .160, .150]


def cloth(w_root, w_tip, length, half_t, grow=0.0, seg=8, rings=6, over=.012):
    """A flat cloth plate from y 0 (width `w_root`) to y `length` (width `w_tip`), every edge rounded, running `over`
    past both ends so a bent chain shows no gap. `grow` widens it all round (the paler hem under the cloth)."""
    n, tris = kit._sphere(seg, rings)
    hl = (length + 2 * over) * .5
    rx, ry = .16, min(.030, hl)
    core = np.array([.5 - rx, hl - ry, 0], np.float32)
    scale = np.array([rx, ry, half_t], np.float32)
    pos = np.sign(n) * core + n * scale
    pos[:, 1] += length * .5
    u = np.clip(pos[:, 1] / length, 0, 1)
    width = w_root + (w_tip - w_root) * u + grow
    pos[:, 0] *= width
    nrm = n / np.array([rx * .13, ry, half_t], np.float32)
    nrm /= np.linalg.norm(nrm, axis=1, keepdims=True)
    return pos.astype(np.float32), nrm.astype(np.float32), tris


def diamond(dx, dy, depth, at=(0, 0, 0)):
    return slab([(0, -dy), (dx, 0), (0, dy), (-dx, 0)], depth, at)


def scarf_segment(s):
    """Plate `s`, counted from the corner. Magenta silk, a paler hem showing all round it, a violet diamond woven
    through it (seen on both faces), and on the corner a gold bead."""
    w_tip, w_root = WIDTHS[s], WIDTHS[s + 1]
    silk = cloth(w_root, w_tip, LINK, HALF_CLOTH)
    hem = cloth(w_root, w_tip, LINK, HALF_CLOTH * .55, grow=.026, seg=6, rings=4, over=.016)
    pieces = [(silk, ACCENT), (hem, ACCENT_LIGHT)]
    mid = (w_tip + w_root) * .5
    if s == 0:
        pieces.append((diamond(.016, .022, HALF_CLOTH * 2 + .007, (0, LINK * .34, 0)), VIOLET))
        pieces.append((ellipsoid((.017, .019, .015), (0, LINK + .016, 0), 6, 4), GOLD))
        pieces.append((ellipsoid((.009, .009, .009), (0, LINK + .038, 0), 5, 3), GOLD_DARK))
    else:
        pieces.append((diamond(mid * .25, LINK * .36, HALF_CLOTH * 2 + .007, (0, LINK * .5, 0)), VIOLET))
        pieces.append((diamond(mid * .09, LINK * .13, HALF_CLOTH * 2 + .013, (0, LINK * .5, 0)), GOLD if s % 2 == 0 else PINK))
    return pieces


# ------------------------------------------------------------------ the wands

WAND = .40                  # a wand's length: `SorayaStageHands.WandLength`


def wand(crystal, wrap):
    """A wand as her hat's are: a wood shaft, a wrapped grip between two gold rings, a gold ferrule, a crystal with a
    bulge and a point, and a gold pommel. Chunky: it has to read at the size of a finger."""
    shaft = lathe([(.016, .020), (.020, .034), (.018, .270)], 6)
    pommel = ellipsoid((.025, .022, .025), (0, .018, 0), 6, 4)
    grip = lathe([(.0235, .100), (.0255, .140), (.0235, .180)], 6)
    rings = join(lathe([(.027, .092), (.027, .104)], 6), lathe([(.027, .176), (.027, .188)], 6))
    ferrule = lathe([(.022, .262), (.028, .275), (.022, .288)], 6)
    stone = lathe([(.014, .285), (.039, .326), (.031, .356), (0.0, WAND)], 5)
    # One pale glint on each side of the stone, so it shows whichever way the wand has turned.
    glint = join(ellipsoid((.008, .013, .008), (0, .332, .033), 4, 3), ellipsoid((.008, .013, .008), (0, .332, -.033), 4, 3))
    return [(shaft, WOOD), (pommel, GOLD), (grip, wrap), (rings, GOLD_DARK), (ferrule, GOLD), (stone, crystal),
            (glint, BONE)]


def box(half, radius, at=(0, 0, 0), seg=8, rings=4):
    return kit.rounded(half, radius, at, seg, rings)


def strap():
    """The strap across her band that the wands are tucked under: dark, a magenta stitch along it, a gold stud at each
    end. +y is off the band's face; it is the same on both sides of x and of z."""
    band = box((.112, .009, .034), .009, (0, .004, 0))
    stitch = box((.058, .005, .007), .005, (0, .013, 0), 6, 4)
    studs = join(ellipsoid((.015, .010, .015), (.088, .012, 0), 6, 3), ellipsoid((.015, .010, .015), (-.088, .012, 0), 6, 3))
    return [(band, NIGHT), (stitch, ACCENT), (studs, GOLD)]


# ------------------------------------------------------------------ the cards

CARD_W, CARD_H = .150, .215     # `SorayaStageHands.CardHeight`


def card_outline(inset=0.0, corner=.024, steps=3):
    """A playing card's outline, its bottom edge's middle at the origin, counter-clockwise."""
    hw, h = CARD_W * .5 - inset, CARD_H - inset
    r = max(.004, corner - inset)
    pts = []
    for cx, cy, start in ((hw - r, inset + r, -90), (hw - r, h - r, 0), (-hw + r, h - r, 90), (-hw + r, inset + r, 180)):
        for k in range(steps):
            a = math.radians(start + 90 * k / (steps - 1))
            pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return pts


def star(radius, inner, points, at):
    out = []
    for k in range(points * 2):
        a = math.pi * .5 + math.pi * k / points
        r = radius if k % 2 == 0 else inner
        out.append((r * math.cos(a), r * math.sin(a)))
    return slab(out, .004, at)


def disc(radius, at, sides=10):
    return slab([(radius * math.cos(2 * math.pi * k / sides), radius * math.sin(2 * math.pi * k / sides)) for k in range(sides)], .004, at)


def card(index):
    """One card. A gold core (so the edge is gold all round), HER BACK on -z in two tones (deep violet, a magenta
    diamond with a gold eye, two pale pips), and a bone face on +z with the card's own pip."""
    mid = CARD_H * .5
    core = slab(card_outline(), .010)
    back = slab(card_outline(.011, steps=2), .006, (0, 0, -.005))
    lozenge = diamond(.046, .078, .004, (0, mid, -.0085))
    eye = diamond(.017, .029, .004, (0, mid, -.0105))
    pips = join(diamond(.011, .011, .004, (0, mid + .079, -.0085)), diamond(.011, .011, .004, (0, mid - .079, -.0085)))
    face = slab(card_outline(.011, steps=2), .006, (0, 0, .005))
    z = .0085
    tone = (MAGENTA, VIOLET, GOLD_DARK, INK)[index]
    if index == 0:
        big = diamond(.030, .044, .004, (0, mid, z))
    elif index == 1:
        big = star(.040, .017, 4, (0, mid, z))
    elif index == 2:
        big = disc(.031, (0, mid, z))
    else:
        big = slab([(-.034, -.026), (.034, -.026), (0, .040)], .004, (0, mid, z))
    # A pip in every corner, so the card is its own mirror (see the top of this file).
    small = join(*[diamond(.010, .014, .004, (sx * .043, y, z)) for sx in (-1, 1) for y in (.034, CARD_H - .034)])
    return [(core, GOLD), (back, DEEP), (lozenge, ACCENT), (eye, GOLD), (pips, ACCENT_LIGHT), (face, BONE), (big, tone), (small, tone)]


def parts():
    """Every part, in arm units, about its own origin."""
    out = {}
    for s in range(SEGMENTS):
        out["scarf-%d" % s] = scarf_segment(s)
    out["wand-a"] = wand(LILAC, CRIMSON)
    out["wand-b"] = wand(PINK, VIOLET)
    out["strap"] = strap()
    for i in range(4):
        out["card-%d" % i] = card(i)
    return out


def small(pieces):
    return [(moved(m, scale=(1 / SCALE,) * 3), slot) for m, slot in pieces]


def build():
    """The model the game loads: every part on a tidy table (the C# side places them)."""
    m = kit.Model("phaister_hands")
    made = parts()
    for s in range(SEGMENTS):
        m.add("scarf-%d" % s, small(made["scarf-%d" % s]), at=(-.075, (SEGMENTS - 1 - s) * LINK / SCALE, 0))
    m.add("wand-a", small(made["wand-a"]), at=(-.030, 0, 0))
    m.add("wand-b", small(made["wand-b"]), at=(-.012, 0, 0))
    m.add("strap", small(made["strap"]), at=(-.021, .112, 0))
    for i in range(4):
        m.add("card-%d" % i, small(made["card-%d" % i]), at=(.032 + .044 * (i % 2), .060 * (i // 2), 0))
    speck = slab([(-.0004, -.0004), (.0004, -.0004), (0, .0004)], .0004)
    m.add("front", speck, INK, at=(.004, .135, .008))
    m.write(PALETTE)


# =====================================================================================================================
# THE REVIEW BAKES. Nothing below is used by the game. It is `SorayaStageHands`'s own placement, typed again here, so
# the props can be LOOKED at on her arms as the first-person camera frames them (the game cannot be rendered while the
# owner's editor is open). The camera is the probe's (`FpvHandLifeProbe`): 95 degrees, the arms' root at
# (0, -0.18, 0.16) and 0.64 of its size. Checked against Logs/shots-fpv-hands/phaister_v07/f0048.png: her arm's own
# points land on her arm there. The picture's FRONT view is the game's view; the perspective is baked into the shape,
# so the other four views of a bake mean nothing.
# =====================================================================================================================

def unit(v):
    v = np.array(v, np.float64)
    return v / max(float(np.linalg.norm(v)), 1e-9)


def turn(v, axis, degrees):
    """`Quaternion.AngleAxis(degrees, axis) * v`."""
    k, a = unit(axis), math.radians(degrees)
    v = np.array(v, np.float64)
    return v * math.cos(a) + np.cross(k, v) * math.sin(a) + k * float(np.dot(k, v)) * (1 - math.cos(a))


def ribbon(y, side):
    """`SorayaStageHands.Ribbon`: +y exactly along `y`, +x (the cloth's width) as near `side` as that allows."""
    y = unit(y)
    x = unit(side - y * float(np.dot(side, y)))
    return np.stack([x, y, np.cross(x, y)], 1)


def frame(y, z_hint):
    """`SorayaStageHands.Facing`: +y exactly along `y`, +z as near `z_hint` as that allows. Columns x, y, z."""
    y = unit(y)
    x = unit(np.cross(y, z_hint))
    z = np.cross(x, y)
    return np.stack([x, y, z], 1)


class Arm:
    def __init__(self, left):
        fwd, up, at = np.array([-.64607, .68227, -.34220]), np.array([-.06305, .39908, .91474]), np.array([1.0820, -.8561, .3384])
        if left:
            flip = np.array([-1, 1, 1])
            fwd, up, at = fwd * flip, up * flip, at * flip
        self.z = unit(fwd)
        self.x = unit(np.cross(up, self.z))
        self.y = np.cross(self.z, self.x)
        self.o = at
        self.left = left

    def point(self, x, y, z):
        return self.o + self.x * x + self.y * y + self.z * z

    def mesh(self):
        """Her arm's own mesh, painted by piece: sleeve, band, rim, cuff (its upper face is crimson), skin."""
        text = (kit.ROOT / ("Assets/TumbangPreso/Resources/Models/RosterArms/phaister_%s.asset" % ("left" if self.left else "right"))).read_text()
        count = int(re.search(r"m_VertexCount: (\d+)", text).group(1))
        data = bytes.fromhex(re.search(r"_typelessdata: ([0-9a-f]+)", text).group(1))
        stride = len(data) // count
        v = np.array([struct.unpack_from("<8f", data, i * stride) for i in range(count)])
        raw = bytes.fromhex(re.search(r"m_IndexBuffer: ([0-9a-f]+)", text).group(1))
        tris = np.array(struct.unpack("<%dH" % (len(raw) // 2), raw), np.uint32).reshape(-1, 3)
        pieces = []
        for lo, hi, zmin, slot in ((0, .376, -9, NIGHT), (.376, .518, -9, BAND), (.518, .553, -9, GOLD), (.553, .652, .344, CRIMSON),
                                   (.553, .652, -9, BONE), (.53, .9, -9, SKIN)):
            pick = []
            for t in tris:
                p = v[t]
                cell = int(p[0][6] * 16) // 2
                mid_y, low_z = p[:, 1].mean(), p[:, 2].min()
                skin = cell == 6 and abs(p[0][7] - .573) < .01
                if slot == SKIN:
                    ok = skin or mid_y >= .652
                elif slot == CRIMSON:
                    ok = not skin and lo <= mid_y < hi and low_z >= zmin
                elif slot == BONE:
                    ok = not skin and lo <= mid_y < hi and low_z < .344
                else:
                    ok = not skin and lo <= mid_y < hi
                if ok:
                    pick.append(t)
            if pick:
                pick = np.array(pick, np.uint32)
                world = v[:, 0:1] * self.x + v[:, 1:2] * self.y + v[:, 2:3] * self.z + self.o
                normal = v[:, 3:4] * self.x + v[:, 4:5] * self.y + v[:, 5:6] * self.z
                pieces.append(((world.astype(np.float32), normal.astype(np.float32), pick), slot))
        return pieces


UP, DOWN = np.array([0., 1, 0]), np.array([0., -1, 0])


def smooth(t):
    t = min(1.0, max(0.0, t))
    return t * t * (3 - 2 * t)


def lay_scarf(arm, out, near, far, wave=0.0, phase=0.0):
    """`SorayaStageHands.LayScarf`: where each plate is with `out` of the scarf's length drawn from the cuff."""
    total = SEGMENTS * LINK
    out = min(out, total * .97)
    mouth = arm.point(.03, .640, .285)
    exit_way = unit(arm.y + arm.z * .10)
    placed, joint = {}, None
    for s in range(SEGMENTS - 1, -1, -1):
        t0, t1 = out - (s + 1) * LINK, out - s * LINK
        if t1 <= .004:
            continue
        middle = (max(t0, 0) + t1) * .5
        bend = smooth(middle / .20)
        flow = unit(near + (far - near) * min(1.0, middle / total))
        if wave:
            flow = unit(flow + arm.x * (wave * math.sin(phase - middle * 9)))
        way = exit_way + (flow - exit_way) * bend
        if np.linalg.norm(way) < .2:
            way = way + arm.z * .3
        way = unit(way)
        origin = mouth + way * t0 if joint is None else joint
        joint = origin + way * LINK
        # The cloth's width stays across her arm, and turns to lie across the SCREEN as it leaves the hand.
        across = np.array([1., 0, 0]) * (1 if arm.x[0] > 0 else -1)
        side = unit(arm.x + (across - arm.x) * (bend * .7))
        placed["scarf-%d" % s] = (ribbon(way, side), origin, 1.0)
    return placed


WAND_X, WAND_LEAN, PEEK = (.150, .250), (-.05, .08), .032


def lay_wand(arm, j, slide, tip=0.0):
    """`SorayaStageHands.LayWand`: `slide` of the wand stands out of the band; `tip` degrees swings it about the slot, flat to the band, as a baton tips."""
    slot = arm.point(WAND_X[j], .455, .329)
    axis = unit(arm.x * WAND_LEAN[j] + arm.y * .70 + arm.z * .714)
    axis = turn(axis, arm.z, tip)
    return frame(axis, arm.x), slot + axis * (min(slide, WAND - .09) - WAND), 1.0


def lay_strap(arm):
    return frame(arm.z, arm.y), arm.point(.200, .430, .331), 1.0


CARD_HINGE, CARD_LEAN = (-.085, .790, .232), 10.0
LOOSE_AT = ((-.24, .15, .02), (-.09, .27, -.03), (.10, .21, .04), (.25, .11, -.02))
LOOSE_WAY = ((-.45, .85, .1), (-.12, 1, -.2), (.2, .95, .2), (.5, .8, -.1))
LOOSE_FACE = ((.3, .2, .93), (-.25, -.1, .96), (.1, .35, .93), (-.3, .1, .95))


def lay_cards(arm, up, fan, loose=0.0):
    """`SorayaStageHands.LayCards`: `up` of each card stands out of the knuckles, `fan` opens them, `loose` lets them go."""
    hinge = arm.point(CARD_HINGE[0], CARD_HINGE[1], CARD_HINGE[2])
    toward = unit(arm.y * -.456 + arm.z * .891)
    rise = turn(unit(arm.y * .891 + arm.z * .456), toward, CARD_LEAN)
    placed = {}
    for i in range(4):
        if up[i] < .03:
            continue
        way = turn(rise, toward, (i - 1.5) * 27 * fan)
        at = hinge + way * ((min(up[i], 1.15) - 1) * CARD_H) + toward * (i * .007)
        face = -toward
        if loose > 0:
            way = unit(way + (unit(LOOSE_WAY[i]) - way) * loose)
            face = unit(face + (unit(LOOSE_FACE[i]) - face) * loose)
            at = at + np.array(LOOSE_AT[i]) * loose
        placed["card-%d" % i] = (frame(way, face), at, .6 + .4 * min(1.0, up[i]))
    return placed


def pose(name, left, right):
    """One moment, as `SorayaStageHands.Step` would leave it once its springs have settled."""
    straight = unit(left.y + left.z * .10)
    placed = {"strap": lay_strap(left)}
    out, near, far, wave, wands, tips = .15, straight, straight, 0.0, (PEEK, PEEK), (0, 0)
    up, fan, loose = (0, 0, 0, 0), 0.0, 0.0
    if name == "acts":                      # the three idle acts at once (in the game one at a time)
        out, near = .36, unit(UP * .8 + left.y * .6)
        far = near
        wands, tips = (.31, PEEK), (24, 0)
        up, fan = (1, 1, 1, 1), 1.0
    elif name == "fall":
        out, near, far, wave = .47, unit(UP + np.array([-.12, 0, 0])), unit(UP + np.array([-.2, 0, 0])), .30
        wands = (0, 0)
        up, fan, loose = (1, 1, 1, 1), 1.0, 1.0
    elif name == "sprint":
        out, wave = .46, .08
        near, far = unit(left.z + left.y * -.3), unit(left.y * -1 + left.z * .25)
    elif name == "limp":                    # tagged, and the moment after a landing
        out, near, far = .33, unit(left.y * .5 + left.x * -.8 + left.z * .05), unit(left.x * -.6 + DOWN * .8 + left.y * .2)
        up = (0, .9, 0, 0)
    elif name == "carry":
        out, near = .34, unit(np.array([.85, .35, 0]))
        far = near
        wands = (.31, PEEK)
    placed.update(lay_scarf(left, out, near, far, wave, 1.0))
    for j in range(2):
        if wands[j] > .01:
            placed["wand-" + "ab"[j]] = lay_wand(left, j, wands[j], tips[j])
    if name == "limp":
        hinge = right.point(CARD_HINGE[0], CARD_HINGE[1], CARD_HINGE[2])
        placed["card-1"] = (frame(unit(np.array([.5, .8, .1])), unit(np.array([.3, -.2, -1]))), hinge + np.array([.06, -.05, -.03]), 1.0)
    else:
        placed.update(lay_cards(right, up, fan, loose))
    return placed


def bake(name):
    """Her arms and the props of one moment, with the first-person camera's perspective baked into the shape."""
    left, right = Arm(True), Arm(False)
    placed, made = pose(name, left, right), parts()
    m = kit.Model("phaister_hands")
    seat, size, grain = np.array([0, -.18, .16]), .64, .1

    def seen(mesh, shift):
        p, n, t = mesh
        cam = p.astype(np.float64) * size + seat
        depth = np.maximum(cam[:, 2:3], .05)
        # Depth goes in as 1 / depth, as a depth buffer keeps it: only then does a flat face stay flat.
        flat = np.concatenate([cam[:, 0:1] / depth + shift, cam[:, 1:2] / depth, (1.0 / depth - 1.0) * .8], 1) * grain
        normal = n * np.array([1, 1, -1], np.float32)
        return flat.astype(np.float32), normal.astype(np.float32), t[:, ::-1].copy()

    for arm, tag, shift in ((left, "left", .32), (right, "right", -.32)):
        m.add("arm-" + tag, [(seen(mesh, shift), slot) for mesh, slot in arm.mesh()])
    for part, (basis, at, grow) in placed.items():
        shift = -.32 if part.startswith("card") else .32
        pieces = []
        for (p, n, t), slot in made[part]:
            world = (p.astype(np.float64) * grow) @ basis.T + at
            pieces.append((seen((world, (n.astype(np.float64) @ basis.T).astype(np.float32), t), shift), slot))
        m.add(part, pieces)
    m.write(PALETTE)


if __name__ == "__main__":
    which = next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--pose=")), None)
    if which:
        bake(which)
    else:
        build()
