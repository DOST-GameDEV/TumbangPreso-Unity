"""Sean's (Rago's) props, modelled part by part: the lava he throws, punches and wears, and the mountain behind him.

    python tools/build_sean_props.py [name ...] [--out=<folder>]
    python tools/build_sean_props.py --measure <name>

WHY A MODEL: the kit's first pass drew its fist, giant fist, bomb and backdrop from cubes, spheres and glow quads built at
runtime (`SeanMagmaFist.cs`, `SeanVolcanicBomb.cs`, `HeroIntroductionScene.Sean.cs` all say so). The owner's rule from Paete's
props stands for every hero: *"the current models of all his skills look ugly still its js blocks"*. So they are typed here like
Paete's and Dante's props (`tools/build_paete_props.py`'s helpers, `tools/build_dante_props.py`'s writer), never prisms.

HIS LOOK FAMILY (`docs/reports/sean-kit-2026-10-24/plan.md` section 0), which every part below obeys:
  * his lava is LIQUID and comes out of his own body: round shapes only (lumps, beads, drips, ropes and coils, splashes);
    never Dante's slabs, hexagonal columns, straight cracks, boulders or stone with light in its seams;
  * a gold-yellow core (`CORE`, about #FFC83A) running to cherry red (`CHERRY`, about #D8261C) at the edges; no flat orange
    (the steps between are `AMBER` and `EMBER`, small areas only); the hottest colour is gold, never white;
  * the BRIGHTEST value sits on the leading edge of what travels (the League VFX rule `feel-and-readability.md` § 0 cites):
    the knuckles of a flying fist, the top of a rising one;
  * everything ends cooling to glossy black glass (`GLASS`, `SHEEN`, `GLINT`): small flecks here, the runtime grows them;
  * no armour: the Molten fists are lava on his own fists and forearms, nothing else.

EVERY PART IS TYPED, NOT STAMPED (*"do it one by one dont try to mass generate it"*): each knuckle, drip, rope, bead and
splash lump is its own row with its own numbers. Nothing is one shape repeated round a circle at one size.

NAMED NODES FOR WHAT MOVES (the runtime poses them from the object's age):
  lava_fist    > fist > knuckles ; fist > drips                      the MAGMA FIST in flight, knuckles on +Z
  giant_fist   > forearm > fist ; forearm > ropes ; forearm > drips ; collar   the Puddle Punch, rising +Y from the court
  bomb_ball    > ball > bulges ; ball > ropes ; ball > skin ; ball > drips    the VOLCANIC BOMB, 1.2 m across
  mayon        > cone > summit ; cone > rivulets ; plume                the backdrop, authored 1.0 tall (the scene scales it)
  molten_fist  > sleeve > mitt > knuckles ; sleeve > drips              Molten, on his fist; origin at the wrist, fist on +Z

⚠️ glTFast NEGATES X ON IMPORT. Faces and fronts are on +Z, so only left and right flip; nothing here depends on a side except
the thumb, typed with `L`/`R` (this file's +X is the game's LEFT).

Palette: `SeanProp.Palette` in `Runtime/Visual/SeanProp.cs` carries the same sixteen hex values (`SEAN_PALETTE` below).
Output: Assets/TumbangPreso/Resources/Models/SeanProps/<name>.glb (or `--out`), metres, +Y up.
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import build_paete_props as bp  # noqa: E402
import build_dante_props as dp  # noqa: E402  (its writer: the donor atlas and material, the palette cells)
from build_paete_props import Node  # noqa: E402

ROOT = os.path.dirname(HERE)
OUT = os.path.join(ROOT, "Assets", "TumbangPreso", "Resources", "Models", "SeanProps")

L, R = 1.0, -1.0

SEAN_PALETTE = {0: "FFC83A", 1: "FFA92E", 2: "E8452A", 3: "D8261C", 4: "8E1712", 5: "17121A", 6: "3A2A35", 7: "B3261E",
                8: "C9C3C0", 9: "6D6668", 10: "2B1520", 11: "3B1E2A", 12: "FFB347", 13: "120C0E", 14: "2A1416", 15: "5A2A20"}
CORE, AMBER, EMBER, CHERRY, DEEP, GLASS, SHEEN, GLINT, STEAM, ASH, DUSK, FAR, SUMMIT, INK, CRUST, RUST = range(16)


def lump(node, centre, radii, slot, tilt=0.0, turn=0.0, sides=9):
    """One round mass of lava (Paete's crown clump profile: a low round lump, smoothed and inked)."""
    bp.clump(node, centre, radii, slot, tilt=tilt, turn=turn, sides=sides)


def drip(node, top, length, radius, slot, lean=(0.0, 0.0), bead=1.2):
    """A drip running down from `top`: a teardrop. Its neck is born as thick as the bead (sunk into the body it leaves), pinches
    a little, and swells into a heavy bead that overlaps the neck's end, so it reads as one hanging drop, never a pin on a ball.
    (v1 was a thin stick ending in a sphere; seen in Blender, every prop's drips read as pins.)"""
    x, y, z = top
    lx, lz = lean
    path = [(x, y + radius * .8, z), (x + lx * .3, y - length * .3, z + lz * .3), (x + lx * .7, y - length * .7, z + lz * .7),
            (x + lx, y - length, z + lz)]
    node.tube(path, [radius * 1.5, radius * 1.05, radius * .85, radius * .95], slot, sides=8)
    lump(node, (x + lx, y - length - radius * .45, z + lz), (radius * bead, radius * bead * 1.25, radius * bead), slot, sides=9)


def rope(node, centre_y0, centre_y1, around_r, start_deg, turns, radius, slot, wobble=0.0, steps=14, surface=None):
    """A coil of flowing lava wound round an upright form (the folded skin of pahoehoe), typed per call. `surface(y)` is the
    body's radius at height y, so the rope lies ON it (v1's ropes floated off the bomb where it narrows)."""
    path, radii = [], []
    for k in range(steps):
        u = k / (steps - 1)
        a = math.radians(start_deg + 360.0 * turns * u)
        y = centre_y0 + (centre_y1 - centre_y0) * u
        base = surface(y) if surface else around_r
        r = base * (1.0 + wobble * math.sin(u * math.pi * 3))
        path.append((r * math.sin(a), y, r * math.cos(a)))
        radii.append(radius * (0.55 + 0.45 * math.sin(math.pi * u)))
    node.tube(path, radii, slot, sides=6)


# ---------------------------------------------------------------------------------------------
# MAGMA FIST: the fist of lava he punches from his knuckles. About 0.46 m long, knuckles leading on +Z.
# ---------------------------------------------------------------------------------------------
def lava_fist():
    root = Node("lava_fist")
    fist = Node("fist", parent="lava_fist")
    # The mitt: one rounded block of liquid, typed as rows (y, cx, cz, rx, rz), the cast's simple hand made of lava.
    # v2: wider than deep and flat on top, so it reads as a fist and not a ball (v1, seen in Blender: "a bubbly ball").
    bp.loft(fist, [(-.16, 0, -.01, .15, .11), (-.12, 0, 0, .2, .15), (-.03, 0, .01, .23, .165), (.07, 0, .01, .225, .16),
                   (.13, 0, 0, .2, .145), (.165, 0, -.01, .14, .1)], AMBER, sides=16)
    # Its back cooler, where the lava left his hand: an ember band then a cherry heel, sunk into the mitt.
    lump(fist, (0, .0, -.1), (.2, .14, .08), EMBER, sides=12)
    lump(fist, (0, .01, -.13), (.15, .11, .06), CHERRY, sides=10)
    lump(fist, (.06, .1, -.16), (.04, .025, .02), GLASS, tilt=10, sides=7)
    lump(fist, (-.07, -.05, -.165), (.03, .02, .018), SHEEN, tilt=-15, sides=7)
    # The thumb, curled across the front of the fist on its left.
    lump(fist, (L * .13, -.08, .1), (.06, .055, .08), AMBER, tilt=18, turn=-20, sides=8)
    knuckles = Node("knuckles", origin=(0, 0, .16), parent="fist")
    # Four knuckles on the leading face, each its own size and height: the brightest value is here, at the front.
    # A ridge, not four bubbles: each knuckle wider than it is tall and sunk into the next.
    lump(knuckles, (L * .14, .04, -.005), (.07, .045, .045), CORE, sides=9)
    lump(knuckles, (L * .05, .06, .008), (.075, .05, .05), CORE, sides=9)
    lump(knuckles, (R * .045, .056, .006), (.074, .049, .049), CORE, sides=9)
    lump(knuckles, (R * .135, .034, -.008), (.066, .042, .043), CORE, sides=9)
    drips = Node("drips", origin=(0, -.06, -.12), parent="fist")
    # What it sheds behind it in flight: three drips trailing back and down, each its own length.
    drip(drips, (L * .08, 0, 0), .11, .026, CHERRY, lean=(L * .01, -.07))
    drip(drips, (R * .02, -.04, .02), .16, .03, EMBER, lean=(0, -.1))
    drip(drips, (R * .1, .01, -.01), .08, .022, DEEP, lean=(R * .015, -.05))
    return [root, fist, knuckles, drips]


# ---------------------------------------------------------------------------------------------
# The Puddle Punch's giant fist: a forearm of flowing lava rising out of the puddle, the fist on top, knuckles UP.
# About 1.75 m tall from the court; its base sits in the puddle at y 0.
# ---------------------------------------------------------------------------------------------
def giant_fist():
    root = Node("giant_fist")
    forearm = Node("forearm", parent="giant_fist")
    # The column: cherry where it leaves the puddle, amber as it climbs (the heat runs UP, as in his cutscene).
    bp.loft(forearm, [(0, 0, 0, .36, .34), (.12, 0, 0, .31, .3), (.34, 0, .01, .28, .27)], CHERRY, sides=14)
    bp.loft(forearm, [(.33, 0, .01, .282, .272), (.46, .005, 0, .272, .262), (.6, .01, 0, .272, .262)], EMBER, sides=14)
    bp.loft(forearm, [(.59, .01, 0, .274, .264), (.78, 0, -.01, .28, .27), (1.0, 0, 0, .3, .29), (1.14, 0, 0, .33, .31)],
            AMBER, sides=14)
    ropes = Node("ropes", parent="forearm")
    # Three coils of lava wound round it the way pahoehoe folds, each its own height, angle, girth and turn.
    column = lambda y: .29 if y > .3 else .34 - y * .17
    rope(ropes, .08, .62, .3, 20, .55, .07, CHERRY, wobble=.02, surface=column)
    rope(ropes, .38, .98, .29, 205, .48, .065, EMBER, wobble=.02, surface=column)
    rope(ropes, .7, 1.1, .32, 110, .32, .06, CORE, wobble=.01, surface=column)
    fist = Node("fist", origin=(0, 1.14, 0), parent="forearm")
    bp.loft(fist, [(0, 0, 0, .34, .32), (.12, 0, .01, .42, .38), (.3, 0, .02, .45, .4), (.46, 0, .01, .4, .36),
                   (.56, 0, 0, .26, .24)], AMBER, sides=14)
    # The knuckles on TOP, leading the punch upward: the brightest value of the prop.
    lump(fist, (L * .25, .55, .05), (.1, .085, .1), CORE, sides=10)
    lump(fist, (L * .085, .6, .07), (.11, .095, .11), CORE, sides=10)
    lump(fist, (R * .085, .59, .06), (.108, .092, .108), CORE, sides=10)
    lump(fist, (R * .245, .53, .04), (.095, .08, .095), CORE, sides=10)
    lump(fist, (L * .34, .28, .2), (.11, .14, .1), AMBER, tilt=-12, turn=25, sides=9)          # the thumb, folded over
    lump(fist, (R * .12, .2, -.36), (.12, .09, .05), CHERRY, sides=9)                          # cooler back of the hand
    lump(fist, (R * .16, .28, -.4), (.05, .03, .02), GLASS, tilt=12, sides=7)                  # a first fleck of glass
    drips = Node("drips", parent="forearm")
    # Lava running back down the arm, each drip its own start, length and lean.
    drip(drips, (L * .28, 1.42, .2), .32, .042, CORE, lean=(L * .03, .02))
    drip(drips, (R * .3, 1.36, .12), .44, .04, AMBER, lean=(R * .02, .04))
    drip(drips, (R * .05, 1.3, -.34), .26, .036, EMBER, lean=(0, -.03))
    drip(drips, (L * .2, 1.0, -.22), .38, .034, CHERRY, lean=(L * .02, -.04))
    collar = Node("collar", parent="giant_fist")
    # The splash where it bursts out of the puddle: seven lumps thrown up and out, each its own size, height and lean.
    # v2: a low ring of thrown lava hugging the column's foot and leaning OUT (v1 stood apart like petals round it).
    lump(collar, (.0, .05, .4), (.2, .12, .14), EMBER, tilt=-38, turn=0, sides=10)
    lump(collar, (.33, .04, .24), (.17, .1, .13), CHERRY, tilt=-34, turn=55, sides=10)
    lump(collar, (.4, .08, -.12), (.15, .15, .11), AMBER, tilt=-42, turn=108, sides=10)
    lump(collar, (.12, .03, -.4), (.19, .09, .14), CHERRY, tilt=-30, turn=165, sides=10)
    lump(collar, (-.27, .07, -.31), (.14, .13, .1), EMBER, tilt=-40, turn=220, sides=10)
    lump(collar, (-.41, .03, .02), (.2, .08, .15), DEEP, tilt=-28, turn=270, sides=10)
    lump(collar, (-.27, .06, .31), (.15, .12, .11), AMBER, tilt=-36, turn=320, sides=10)
    return [root, forearm, ropes, fist, drips, collar]


# ---------------------------------------------------------------------------------------------
# VOLCANIC BOMB: one heavy ball of lava pulled off both his arms, 1.2 m across. A ball of LAVA, never a rock.
# ---------------------------------------------------------------------------------------------
def bomb_ball():
    root = Node("bomb_ball")
    ball = Node("ball", parent="bomb_ball")
    # Slightly squashed, the way a heavy drop of liquid sits: rows (y, cx, cz, rx, rz).
    bp.loft(ball, [(-.55, 0, 0, .14, .14), (-.46, 0, 0, .38, .37), (-.3, 0, .01, .55, .53), (-.08, 0, 0, .62, .6),
                   (.14, 0, -.01, .61, .59), (.34, 0, 0, .5, .49), (.48, 0, 0, .3, .3), (.55, 0, 0, .1, .1)],
            AMBER, sides=16)
    bulges = Node("bulges", parent="ball")
    # Gold welling up through it, five bulges typed one by one (the bubbling the cutscene's ball shows).
    lump(bulges, (.18, .4, .32), (.2, .13, .2), CORE, tilt=-30, turn=30, sides=10)
    lump(bulges, (-.38, .22, .38), (.17, .14, .15), CORE, tilt=-40, turn=-45, sides=10)
    lump(bulges, (.45, .02, -.3), (.16, .17, .14), CORE, tilt=-62, turn=125, sides=10)
    lump(bulges, (-.28, -.18, -.47), (.14, .12, .12), CORE, tilt=-70, turn=210, sides=10)
    lump(bulges, (.05, .52, -.08), (.15, .07, .15), CORE, sides=10)
    ropes = Node("ropes", parent="ball")
    # Two folds of skin swirling round it in opposite turns.
    sphere = lambda y: math.sqrt(max(.0, .61 ** 2 - y * y)) * .98 + .01
    rope(ropes, -.36, .3, .61, 40, .6, .07, EMBER, wobble=.0, surface=sphere)
    rope(ropes, -.1, .42, .58, 250, -.45, .065, CHERRY, wobble=.0, surface=sphere)
    skin = Node("skin", parent="ball")
    # Where it has started to skin over: three flecks of black glass with a red glint, small and uneven.
    lump(skin, (-.5, .24, -.2), (.09, .05, .11), GLASS, tilt=-55, turn=-110, sides=8)
    lump(skin, (.3, -.38, .38), (.07, .04, .06), SHEEN, tilt=-60, turn=40, sides=8)
    lump(skin, (-.12, -.44, .33), (.05, .03, .05), GLINT, tilt=-68, turn=-20, sides=7)
    drips = Node("drips", parent="ball")
    # Hanging under it as it is held and as it flies.
    drip(drips, (.12, -.5, .1), .18, .045, CHERRY, lean=(.02, .01))
    drip(drips, (-.22, -.45, -.12), .12, .04, DEEP, lean=(-.02, -.02))
    drip(drips, (.3, -.38, -.2), .1, .035, EMBER, lean=(.03, -.01))
    return [root, ball, bulges, ropes, skin, drips]


# ---------------------------------------------------------------------------------------------
# Mayon, the cutscene's backdrop and the emblem: a near-perfect cone with concave sides and a lit summit. Authored 1.0 tall.
# ---------------------------------------------------------------------------------------------
def mayon():
    root = Node("mayon")
    cone = Node("cone", parent="mayon")
    # Concave sides: the radius falls fast near the foot and slowly near the top (rows typed off the silhouette, not a formula).
    # v2: the foot thickens instead of flaring (v1 had a witch-hat brim): a little rise at the very edge, then the long sweep.
    bp.loft(cone, [(0, 0, 0, .86, .72), (.035, 0, 0, .8, .67), (.09, 0, 0, .66, .56), (.17, 0, 0, .52, .44),
                   (.26, 0, 0, .4, .35)], FAR, sides=18, cap_last=False)
    bp.loft(cone, [(.26, 0, 0, .4, .35), (.4, 0, 0, .27, .24), (.56, 0, 0, .17, .155), (.72, 0, 0, .105, .095),
                   (.86, 0, 0, .06, .055), (.95, 0, 0, .035, .032)], DUSK, sides=18, cap_first=False)
    summit = Node("summit", origin=(0, .95, 0), parent="cone")
    # The lit summit: a gold cap and a softer ring of glow under it (it lights when the heat reaches his chest).
    lump(summit, (0, .025, 0), (.04, .03, .036), SUMMIT, sides=10)
    lump(summit, (0, -.01, 0), (.05, .018, .046), CHERRY, sides=10)
    rivulets = Node("rivulets", parent="cone")
    # Two threads of lava down the facing flank, each its own path (a gentle glow, not a disaster).
    rivulets.tube([(.012, .94, .03), (.03, .8, .08), (.05, .64, .15), (.09, .46, .25), (.13, .3, .36)],
                  [.008, .009, .009, .007, .005], CHERRY, sides=5)
    rivulets.tube([(-.01, .93, .032), (-.035, .76, .1), (-.07, .58, .19), (-.1, .42, .28)],
                  [.007, .008, .007, .005], EMBER, sides=5)
    plume = Node("plume", origin=(0, 1.0, 0), parent="mayon")
    # Steam and ash over the summit: three soft clumps, each its own size and drift.
    lump(plume, (.02, .05, 0), (.06, .045, .05), STEAM, sides=9)
    lump(plume, (.07, .12, -.01), (.08, .05, .06), ASH, tilt=8, sides=9)
    lump(plume, (.15, .18, -.02), (.09, .045, .06), ASH, tilt=12, turn=20, sides=9)
    return [root, cone, summit, rivulets, plume]


# ---------------------------------------------------------------------------------------------
# Molten: lava on his own fist and forearm, oversized and dripping. Origin at the wrist, the fist on +Z, the sleeve back to -Z.
# No armour (plan rule 3): this is liquid wrapping his arm, so its surface is round and running, never plated.
# ---------------------------------------------------------------------------------------------
def molten_fist():
    root = Node("molten_fist")
    sleeve = Node("sleeve", parent="molten_fist")
    # The forearm's lava, laid along -Z: rows here run in z, so a loft is built upright and turned onto its side.
    local = Node("_sleeve")
    bp.loft(local, [(-.36, 0, 0, .07, .066), (-.29, 0, 0, .095, .09), (-.2, .005, 0, .105, .1), (-.11, 0, 0, .11, .105)],
            CHERRY, sides=12)
    bp.loft(local, [(-.115, 0, 0, .111, .106), (-.04, 0, 0, .118, .11), (.04, 0, 0, .13, .115)], EMBER, sides=12)
    for slot, faces in local.parts:
        sleeve.parts.append((slot, [[(p[0], -p[2], p[1]) for p in f] for f in faces]))
    mitt = Node("mitt", origin=(0, 0, .1), parent="sleeve")
    # The oversized fist, 1.4 times his hand, amber running to gold at the knuckles.
    bp.loft(mitt, [(-.115, 0, 0, .12, .09), (-.07, 0, .005, .165, .115), (0, 0, .01, .18, .125), (.07, 0, .005, .17, .118),
                   (.115, 0, 0, .13, .09)], AMBER, sides=16)
    lump(mitt, (L * .12, -.05, .07), (.045, .045, .06), AMBER, tilt=15, turn=-18, sides=8)      # the thumb
    knuckles = Node("knuckles", origin=(0, 0, .13), parent="mitt")
    lump(knuckles, (L * .1, .03, 0), (.042, .038, .04), CORE, sides=8)
    lump(knuckles, (L * .034, .05, .01), (.047, .042, .045), CORE, sides=8)
    lump(knuckles, (R * .036, .047, .008), (.045, .04, .043), CORE, sides=8)
    lump(knuckles, (R * .098, .026, -.004), (.039, .035, .037), CORE, sides=8)
    drips = Node("drips", parent="sleeve")
    # Dripping off the underside of the arm and the fist, each its own place and length.
    drip(drips, (L * .04, -.11, -.08), .07, .018, CHERRY, lean=(0, .01))
    drip(drips, (R * .05, -.1, -.22), .1, .02, DEEP, lean=(R * .01, -.01))
    drip(drips, (L * .02, -.12, .14), .06, .016, EMBER, lean=(0, .015))
    return [root, sleeve, mitt, knuckles, drips]


BUILDS = {"lava_fist": lava_fist, "giant_fist": giant_fist, "bomb_ball": bomb_ball, "mayon": mayon, "molten_fist": molten_fist}


def measure(nodes):
    """World bounds of the whole prop (node origins summed down the parent chain), for sizing against the rules."""
    by = {n.name: n for n in nodes}

    def offset(n):
        o = (0.0, 0.0, 0.0)
        while n is not None:
            o = bp.add(o, n.origin); n = by.get(n.parent)
        return o
    lo, hi = [1e9] * 3, [-1e9] * 3
    for n in nodes:
        o = offset(n)
        for _, faces in n.parts:
            for f in faces:
                for p in f:
                    for k in range(3):
                        lo[k] = min(lo[k], p[k] + o[k]); hi[k] = max(hi[k], p[k] + o[k])
    return [round(v, 3) for v in lo], [round(v, 3) for v in hi]


def main(argv):
    out = next((a.split("=", 1)[1] for a in argv if a.startswith("--out=")), OUT)
    names = [a for a in argv if not a.startswith("--")] or list(BUILDS)
    if "--measure" in argv:
        for name in names:
            print(name, measure(BUILDS[name]()))
        return
    os.makedirs(out, exist_ok=True)
    for name in names:
        nodes = BUILDS[name]()
        dp.write(os.path.join(out, name + ".glb"), nodes)
        print("  bounds", measure(nodes))


if __name__ == "__main__":
    main(sys.argv[1:])
