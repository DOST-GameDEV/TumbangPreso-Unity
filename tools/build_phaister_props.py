"""Phaister's props (HERO-10, 2026-09-27), modelled part by part: the black BUTTERFLY of OMEN, the violet MOTH and the
BEETLE of VANISHING ACT's barang swarm, the MANIKA she throws, and the HAT PIN of SPOTLIGHT PIN.

    python tools/build_phaister_props.py [butterfly] [moth] [beetle] [manika] [hatpin]

Plan: `docs/reports/phaister-kit-2026-09-27/plan.md` sections 3 and 4. Research: `research.md` beside it (Castorice's wing:
a dark wing, darker veins, a BRIGHT EDGE and a glowing body; Seele's butterflies drifting slowly against fast strokes).

⚠️ EVERY PART IS TYPED, NOT STAMPED (owner, standing: *"manually do each part of that builder dont js auto generate
looping shit"*). Each wing lobe is its own outline, each vein its own points, each leg its own line. The two wings of one
insect are one outline mirrored across the body, which is what a wing is.

⚠️ WINGS ARE PLATES, NOT CARDS: a flat convex prism 4 to 6 mm thick, so the inverted-hull ink has back faces to draw
(the leaf lesson in `build_paete_voxel._leaf`). Each wing is its own NODE hinged on the body's long axis (+Z is the
head), so the runtime beats it by turning the node about Z; the manika's head and arms are nodes too.

Palette: `PhaisterProp.Palette` in `Runtime/Visual/PhaisterProp.cs`, sixteen slots, matched to the table below. The
manika's CLOTH slot is recoloured per victim at runtime (it becomes a doll OF them, owner: *"yes"* to plan question 4).

Output: Assets/TumbangPreso/Resources/Models/PhaisterProps/{butterfly,moth,beetle,manika,hatpin}.glb, metres, +Y up,
the front on +Z.
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import build_paete_props as bp  # noqa: E402
import build_paete_voxel as pv  # noqa: E402
from build_paete_props import Node, line  # noqa: E402

ROOT = os.path.dirname(HERE)
OUT = os.path.join(ROOT, "Assets", "TumbangPreso", "Resources", "Models", "PhaisterProps")

# Slots (PhaisterProp.Palette in C# carries the colours):
WING_BLACK = 0    # 1a1020  the omen's wing
WING_VEIN = 1     # 07040a  veins, darker than the wing
WING_EDGE = 2     # e0287e  her hair's magenta, the bright edge
BODY_GLOW = 3     # ff6ab8  the glowing thorax and antenna tips
MOTH_WING = 4     # 6a3aa8  the moth's violet
MOTH_VEIN = 5     # 3e1f6e
MOTH_EDGE = 6     # c9a2f0  lilac edge
SHELL = 7         # 4a2a6a  the beetle's shell, a violet sheen (v2: all-dark read as a blob)
INK = 8           # 14101c
CLOTH = 9         # e0a078  burlap (recoloured per victim)
CLOTH_DK = 10     # a8683c
PIN_GOLD = 11     # f8b824
PIN_LILAC = 12    # 9838d8
PIN_CRIMSON = 13  # 8c1424
BONE = 14         # f2e6da
FUZZ = 15         # 9c78c8  the moth's fuzzy thorax


def plate(node, outline, thickness, slot, y=0.0):
    """A flat convex prism in the node's XZ plane at height y: `outline` is (x, z) points, counter-clockwise from above."""
    top = [(x, y + thickness * 0.5, z) for x, z in outline]
    bottom = [(x, y - thickness * 0.5, z) for x, z in outline]
    middle = pv._rafi_mean(top + bottom)
    faces = [pv._rafi_orient(top, (0.0, 1.0, 0.0)), pv._rafi_orient(bottom, (0.0, -1.0, 0.0))]
    for i in range(len(outline)):
        k = (i + 1) % len(outline)
        side = [top[i], top[k], bottom[k], bottom[i]]
        faces.append(pv._rafi_orient(side, pv._rafi_sub(pv._rafi_mean(side), middle)))
    node.parts.append((slot, faces))


def mirror(points):
    return [(-p[0],) + tuple(p[1:]) for p in points]


def wing_pair(nodes, root, hinge, fore, hind, edges, veins, spots, wing_slot, vein_slot, edge_slot, spot_slot, thickness):
    """The left wing typed, the right its mirror. Each is a node hinged at +/-hinge on the body's axis."""
    for side, sign in (("l", 1.0), ("r", -1.0)):
        n = Node("wing-" + side, origin=(sign * hinge, 0.0, 0.0), parent=root); nodes.append(n)
        f = [(sign * x, z) for x, z in fore]
        h = [(sign * x, z) for x, z in hind]
        if sign < 0:
            f.reverse(); h.reverse()
        plate(n, f, thickness, wing_slot)
        plate(n, h, thickness, wing_slot, y=-0.0005)
        for pts, radius in edges:
            line(n, [(sign * x, 0.0, z) for x, z in pts], [radius] * len(pts), edge_slot, sides=4, per=2)
        for pts, radius in veins:
            line(n, [(sign * x, 0.0, z) for x, z in pts], [radius, radius * 0.6], vein_slot, sides=4, per=2)
        for (x, z), size in spots:
            n.obox((sign * x, 0.0, z), (size, thickness * 1.6, size * 0.8), spot_slot, yaw=sign * 20.0)


def butterfly():
    """OMEN's black butterfly. Wingspan 0.34 m (read at five metres; Paete's leaves were specks at their first typed size,
    film r16). Forewings long and pointed, hindwings rounder with a tail; the veins run from the root; the magenta edge
    runs along the OUTER margin only, as Castorice's wings are lit; one magenta eye-spot on each hindwing."""
    nodes = [Node("butterfly")]
    body = Node("body", parent="butterfly"); nodes.append(body)
    body.obox((0.0, 0.0, 0.012), (0.022, 0.020, 0.040), BODY_GLOW)                  # thorax, the glow
    line(body, [(0.0, 0.0, -0.008), (0.0, -0.002, -0.045), (0.0, -0.004, -0.080)], [0.011, 0.008, 0.004], WING_BLACK, sides=5, per=3)
    body.obox((0.0, 0.004, 0.040), (0.016, 0.015, 0.016), WING_BLACK)               # head
    line(body, [(0.004, 0.008, 0.046), (0.020, 0.020, 0.080), (0.032, 0.028, 0.104)], [0.0025, 0.002, 0.0018], WING_BLACK, sides=4, per=2)
    line(body, [(-0.004, 0.008, 0.046), (-0.018, 0.021, 0.081), (-0.028, 0.030, 0.106)], [0.0025, 0.002, 0.0018], WING_BLACK, sides=4, per=2)
    body.obox((0.032, 0.028, 0.104), (0.008, 0.008, 0.008), BODY_GLOW)
    body.obox((-0.028, 0.030, 0.106), (0.008, 0.008, 0.008), BODY_GLOW)
    fore = [(0.004, 0.028), (0.050, 0.074), (0.118, 0.104), (0.168, 0.098), (0.172, 0.070), (0.140, 0.030),
            (0.080, 0.002), (0.006, -0.004)]
    hind = [(0.006, -0.006), (0.078, -0.004), (0.118, -0.034), (0.122, -0.070), (0.096, -0.104), (0.060, -0.122),
            (0.030, -0.100), (0.008, -0.050)]
    edges = [([(0.118, 0.104), (0.168, 0.098), (0.172, 0.070), (0.140, 0.030)], 0.0055),
             ([(0.118, -0.034), (0.122, -0.070), (0.096, -0.104), (0.060, -0.122)], 0.0050)]
    veins = [([(0.008, 0.010), (0.150, 0.088)], 0.0030),
             ([(0.008, 0.006), (0.140, 0.052)], 0.0028),
             ([(0.008, 0.002), (0.098, 0.014)], 0.0026),
             ([(0.010, -0.010), (0.110, -0.060)], 0.0028),
             ([(0.010, -0.014), (0.068, -0.104)], 0.0026)]
    spots = [((0.092, -0.064), 0.022)]
    wing_pair(nodes, "butterfly", 0.010, fore, hind, edges, veins, spots, WING_BLACK, WING_VEIN, WING_EDGE, WING_EDGE, 0.005)
    bp.write(os.path.join(OUT, "butterfly.glb"), nodes)


def moth():
    """VANISHING ACT's violet moth. Wingspan 0.17 m. v2 (film of props v1: *it read as a lilac butterfly*): a moth is FAT
    and FURRY with SHORT FEATHERED antennae and a pointed DELTA forewing that hides most of the hindwing, so the thorax is
    now as wide as a forewing's root, the antennae are two short flat combs, the forewing is a swept triangle in a deeper
    violet, and the lilac edge is a thin line on its outer margin only."""
    nodes = [Node("moth")]
    body = Node("body", parent="moth"); nodes.append(body)
    body.obox((0.0, 0.004, 0.006), (0.034, 0.030, 0.036), FUZZ)                      # the fuzzy thorax, fat
    body.obox((0.0, 0.016, 0.004), (0.026, 0.010, 0.028), MOTH_EDGE)                 # a lighter ruff on top
    body.obox((0.0, 0.000, 0.028), (0.026, 0.022, 0.014), FUZZ)                      # the collar before the head
    line(body, [(0.0, 0.0, -0.010), (0.0, -0.003, -0.030), (0.0, -0.004, -0.048)], [0.014, 0.011, 0.005], MOTH_VEIN, sides=6, per=3)
    for z in (-0.020, -0.032):                                                        # two abdomen bands
        body.obox((0.0, -0.002, z), (0.024, 0.020, 0.004), FUZZ)
    body.obox((0.0, 0.002, 0.038), (0.016, 0.014, 0.010), MOTH_VEIN)                 # head
    body.obox((0.006, 0.004, 0.043), (0.004, 0.004, 0.002), INK)                     # eyes, ink
    body.obox((-0.006, 0.004, 0.043), (0.004, 0.004, 0.002), INK)
    # Feathered antennae: two short flat combs swept back over the head, each a typed plate.
    for sign in (1.0, -1.0):
        body.obox((sign * 0.012, 0.014, 0.048), (0.014, 0.003, 0.022), MOTH_EDGE, yaw=sign * 28.0, pitch=0.0, roll=0.0)
    fore = [(0.008, 0.026), (0.048, 0.022), (0.090, 0.004), (0.084, -0.010), (0.040, -0.030), (0.008, -0.012)]
    hind = [(0.008, -0.014), (0.036, -0.030), (0.046, -0.044), (0.022, -0.050), (0.008, -0.034)]
    edges = [([(0.048, 0.022), (0.090, 0.004), (0.084, -0.010)], 0.0032)]
    veins = [([(0.008, 0.012), (0.080, 0.004)], 0.0020),
             ([(0.008, 0.002), (0.060, -0.016)], 0.0018)]
    spots = [((0.050, 0.000), 0.010)]
    wing_pair(nodes, "moth", 0.016, fore, hind, edges, veins, spots, MOTH_WING, MOTH_VEIN, MOTH_EDGE, MOTH_EDGE, 0.004)
    bp.write(os.path.join(OUT, "moth.glb"), nodes)


def beetle():
    """The barang's beetle: 0.08 m long, a domed dark shell of two elytra (each a node that lifts to fly), a head with short
    mandibles, six legs each typed, and a thin violet line down the seam of the shell."""
    nodes = [Node("beetle")]
    body = Node("body", parent="beetle"); nodes.append(body)
    body.obox((0.0, 0.004, 0.022), (0.030, 0.018, 0.020), SHELL)                     # pronotum
    body.obox((0.0, 0.000, 0.040), (0.020, 0.014, 0.014), INK)                       # head
    line(body, [(0.006, 0.000, 0.046), (0.012, 0.000, 0.056), (0.006, 0.000, 0.062)], [0.0022, 0.002, 0.0015], INK, sides=4, per=2)
    line(body, [(-0.006, 0.000, 0.046), (-0.012, 0.000, 0.056), (-0.006, 0.000, 0.062)], [0.0022, 0.002, 0.0015], INK, sides=4, per=2)
    body.obox((0.0, -0.004, -0.004), (0.030, 0.014, 0.046), INK)                    # underside under the shell
    legs = [[(0.012, -0.006, 0.020), (0.026, -0.004, 0.030), (0.034, -0.014, 0.040)],
            [(0.013, -0.006, 0.004), (0.030, -0.004, 0.004), (0.038, -0.015, 0.000)],
            [(0.012, -0.006, -0.012), (0.026, -0.004, -0.022), (0.032, -0.015, -0.034)]]
    for leg in legs:
        line(body, leg, [0.0024, 0.0020, 0.0016], INK, sides=4, per=2)
        line(body, mirror(leg), [0.0024, 0.0020, 0.0016], INK, sides=4, per=2)
    for side, sign in (("l", 1.0), ("r", -1.0)):
        e = Node("elytron-" + side, origin=(sign * 0.001, 0.006, 0.012), parent="beetle"); nodes.append(e)
        e.obox((sign * 0.008, 0.004, -0.018), (0.016, 0.012, 0.040), SHELL, roll=sign * -8.0)
        e.obox((sign * 0.007, 0.010, -0.016), (0.012, 0.004, 0.032), SHELL, roll=sign * -6.0)   # the dome's crown
        e.obox((sign * 0.001, 0.010, -0.018), (0.003, 0.006, 0.038), MOTH_EDGE)                # the pale seam
        e.obox((sign * 0.010, 0.013, -0.012), (0.004, 0.003, 0.020), MOTH_EDGE, roll=sign * -6.0)  # the shine
    bp.write(os.path.join(OUT, "beetle.glb"), nodes)


def manika():
    """The rag doll of MANIKA MISCHIEF, 0.30 m, in burlap: a stitched head with X eyes and a stitched mouth, a body with a
    seam, stub arms that dangle (nodes), stub legs, a yarn tuft, and one of her hat pins through the chest. The CLOTH slot
    takes the victim's colour when it steals their look; the stitches, X eyes and pin stay hers."""
    nodes = [Node("manika")]
    body = Node("body", parent="manika"); nodes.append(body)
    body.obox((0.0, 0.100, 0.0), (0.130, 0.150, 0.080), CLOTH)
    body.obox((0.0, 0.020, 0.0), (0.120, 0.030, 0.076), CLOTH_DK)                   # the hem
    body.obox((-0.034, -0.030, 0.0), (0.044, 0.070, 0.050), CLOTH)                  # legs
    body.obox((0.036, -0.026, 0.0), (0.044, 0.064, 0.050), CLOTH)
    body.obox((0.0, 0.100, 0.042), (0.006, 0.130, 0.006), INK)                      # seam
    for y, w in [(0.150, 0.026), (0.118, 0.022), (0.084, 0.024), (0.052, 0.020)]:
        body.obox((0.0, y, 0.044), (w, 0.006, 0.004), INK)
    line(body, [(0.050, 0.140, -0.060), (0.020, 0.120, 0.010), (-0.010, 0.100, 0.090)], [0.005, 0.005, 0.004], PIN_GOLD, sides=4, per=2)
    body.obox((0.050, 0.140, -0.066), (0.026, 0.026, 0.026), PIN_LILAC)
    body.obox((-0.034, 0.070, 0.042), (0.040, 0.036, 0.006), CLOTH_DK)             # a patch, lower left, off square
    for (x, y) in [(-0.052, 0.086), (-0.016, 0.086), (-0.052, 0.054), (-0.016, 0.054)]:
        body.obox((x, y, 0.046), (0.006, 0.006, 0.004), INK)                        # its corner stitches
    head = Node("head", origin=(0.0, 0.176, 0.0), parent="manika"); nodes.append(head)
    head.obox((0.0, 0.052, 0.0), (0.120, 0.104, 0.096), CLOTH)
    head.obox((0.0, 0.004, 0.0), (0.050, 0.012, 0.050), CLOTH_DK)                   # the neck tie
    for cx in (-0.028, 0.030):                                                      # X eyes, two strokes each
        head.obox((cx, 0.064, 0.050), (0.026, 0.006, 0.004), INK, roll=0.0, yaw=0.0, pitch=45.0)
        head.obox((cx, 0.064, 0.050), (0.026, 0.006, 0.004), INK, roll=0.0, yaw=0.0, pitch=-45.0)
    head.obox((0.004, 0.030, 0.050), (0.052, 0.005, 0.004), INK)                    # the stitched mouth
    for x in (-0.016, 0.000, 0.016):
        head.obox((0.004 + x, 0.030, 0.052), (0.004, 0.014, 0.004), INK)
    for (x, z), h in [((-0.030, -0.010), 0.030), ((0.004, 0.012), 0.040), ((0.034, -0.018), 0.026)]:
        head.obox((x, 0.108 + h * 0.5, z), (0.018, h, 0.018), CLOTH_DK)             # yarn tuft, three uneven strands
    for side, sign in (("l", 1.0), ("r", -1.0)):
        a = Node("arm-" + side, origin=(sign * 0.066, 0.150, 0.0), parent="manika"); nodes.append(a)
        a.obox((sign * 0.020, -0.028, 0.0), (0.036, 0.070, 0.040), CLOTH, roll=sign * 18.0)
    bp.write(os.path.join(OUT, "manika.glb"), nodes)


def hatpin():
    """SPOTLIGHT PIN's hat pin, drawn from her hat band and stabbed down: 0.40 m of gold shaft, a faceted lilac head, a
    small crescent guard under the head (her moon), and a sharp stepped tip. The pin points DOWN its node's -Y."""
    nodes = [Node("hatpin")]
    pin = Node("pin", parent="hatpin"); nodes.append(pin)
    line(pin, [(0.0, 0.0, 0.0), (0.0, -0.20, 0.0), (0.0, -0.36, 0.0), (0.0, -0.40, 0.0)], [0.009, 0.008, 0.006, 0.0015], PIN_GOLD, sides=5, per=2)
    pin.obox((0.0, 0.030, 0.0), (0.050, 0.050, 0.050), PIN_LILAC, yaw=45.0)
    pin.obox((0.0, 0.030, 0.0), (0.034, 0.066, 0.034), PIN_LILAC, yaw=0.0)
    pin.obox((0.0, 0.060, 0.0), (0.020, 0.012, 0.020), BONE)
    for (x, y), size in [((-0.020, -0.012), (0.014, 0.010, 0.010)), ((0.000, -0.018), (0.024, 0.008, 0.010)),
                         ((0.020, -0.012), (0.014, 0.010, 0.010)), ((0.030, -0.002), (0.008, 0.012, 0.010))]:
        pin.obox((x, y, 0.0), size, PIN_GOLD)                                       # the crescent guard
    bp.write(os.path.join(OUT, "hatpin.glb"), nodes)


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    which = set(sys.argv[1:]) or {"butterfly", "moth", "beetle", "manika", "hatpin"}
    for name in ("butterfly", "moth", "beetle", "manika", "hatpin"):
        if name in which:
            globals()[name]()
