"""PAYO land kit: review pictures for the second set of pieces (Blender, NOT the game's shader).

  blender -b --python tools/payo_pieces/terrace_review2.py -- far        full | mid | far terrace stacks side by side
  blender -b --python tools/payo_pieces/terrace_review2.py -- pit        the pit wall, 4 high and 4 wide with a corner
  blender -b --python tools/payo_pieces/terrace_review2.py -- cliff      the rock cliff, with a waterfall set in it
  blender -b --python tools/payo_pieces/terrace_review2.py -- ground     slope tiles and the footpath
  blender -b --python tools/payo_pieces/terrace_review2.py -- one <name> [...]   a piece alone, as terrace_review.py does

Same light as terrace_review.py (warm-white sun, cool blue sky), same ink line. Pictures go to
Logs/payo/pieces/terrace/shots.
"""
import math
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import terrace_review as R    # noqa: E402  (its own modes do not run: the first word here is none of them)

args = R.args
H, RUN = R.H, R.RUN
GRASS = (0.05, 0.13, 0.05)


def fix():
    """Materials the first review script does not know."""
    for m in bpy.data.materials:
        n = m.name.split(".")[0]
        if n == "py_terrace_far" and m.use_nodes:
            m.surface_render_method = "DITHERED"
            m.use_backface_culling = False
            b = m.node_tree.nodes.get("Principled BSDF")
            tex = next((x for x in m.node_tree.nodes if x.type == "TEX_IMAGE"), None)
            if b and tex:
                if not b.inputs["Alpha"].links:
                    m.node_tree.links.new(tex.outputs["Alpha"], b.inputs["Alpha"])
                b.inputs["Emission Strength"].default_value = 0.5


def load(name, at=(0, 0, 0), turn=0.0, ink=0.012):
    root = R.load(name, at, turn, ink)
    for o in root.children_recursive:
        if o.type == "MESH" and any(m and m.name.split(".")[0] == "py_terrace_far" for m in o.data.materials):
            for mod in list(o.modifiers):
                o.modifiers.remove(mod)
    fix()
    return root


def far():
    sc, cam = R.stage()
    rows = ["young", "young", "ripe", "young", "flooded", "young"]
    made = {}
    for col, level in enumerate(("", "_mid", "_far")):
        for i, v in enumerate(rows):
            key = "terrace_straight_" + v + level
            at = (col * 13.0 - 19.5, i * RUN, i * H)
            if key not in made:
                made[key] = load(key, at)
            else:
                R.clone(made[key], at)
    grass = R.flat("hill", GRASS, 0.0)
    t35 = math.tan(math.radians(35))
    R.plane("slope", [(-80, 0, 0), (80, 0, 0), (80, 60, 60 * t35), (-80, 60, 60 * t35)], grass)
    R.plane("floor", [(-80, -200, -0.05), (80, -200, -0.05), (80, 0.4, -0.05), (-80, 0.4, -0.05)], R.flat("floor", (0.07, 0.15, 0.06), 0.0))
    R.figure((-7.0, -0.8, 0), 0.4); R.figure((6.0, -0.8, 0), 0.4); R.figure((19.0, -0.8, 0), 0.4)
    R.look(cam, (0, -60, 1.7), (0, 3, 3.6), 56); R.shoot("far_60")
    R.look(cam, (0, -25, 1.7), (0, 3, 3.6), 24); R.shoot("far_25")
    R.look(cam, (0, -100, 1.7), (0, 3, 3.6), 90); R.shoot("far_100")
    R.look(cam, (0, -52, 34), (0, 4, 3.2), 56); R.shoot("far_above")
    R.look(cam, (19.0, -7.0, 1.7), (19.0, 3.5, 4.3), 24); R.shoot("far_itself_7m")
    R.look(cam, (6.0, -7.0, 1.7), (6.0, 3.5, 4.3), 24); R.shoot("mid_itself_7m")


def hill(suffix="", per=12.0, cols=20, tag="hill"):
    """The grid test: 30 rows of far modules, 240 m wide, on a 37 degree slope, no stagger between rows."""
    import random
    sc, cam = R.stage()
    r = random.Random(5)
    run = H / math.tan(math.radians(37))
    made = {}
    v = "young"
    for row in range(30):
        c = 0
        while c < cols:
            if r.random() < 0.30:
                v = r.choice(["young", "young", "young", "ripe", "ripe", "flooded"])
            key = "terrace_straight_%s_far%s" % (v, suffix)
            at = (c * per - cols * per / 2, row * run, row * H)
            if key not in made:
                made[key] = load(key, at, 0.0, ink=0)
            else:
                R.clone(made[key], at)
            c += 1
    t37 = math.tan(math.radians(37))
    R.plane("slope", [(-400, 0, 0), (400, 0, 0), (400, 120, 120 * t37), (-400, 120, 120 * t37)], R.flat("hill", GRASS, 0.0))
    R.plane("floor", [(-400, -400, -0.05), (400, -400, -0.05), (400, 0.4, -0.05), (-400, 0.4, -0.05)], R.flat("floor", (0.07, 0.15, 0.06), 0.0))
    R.look(cam, (0, -120, 1.7), (0, 20, 17.0), 22); R.shoot(tag + "_120")
    R.look(cam, (40, -120, 1.7), (40, 20, 17.0), 50); R.shoot(tag + "_120_tele")
    R.look(cam, (0, -150, 45), (0, 20, 15.0), 28); R.shoot(tag + "_high")
    R.look(cam, (-150, -90, 12), (0, 20, 16.0), 30); R.shoot(tag + "_oblique")


def one(names):
    for n in names:
        R.piece(n)


if __name__ == "__main__":
    if args[0] == "far":
        far()
    elif args[0] == "hill":
        hill()
        hill("48", 48.0, 5, "hill48")
    elif args[0] == "one":
        one(args[1:])
    else:
        import terrace_review3 as R3
        getattr(R3, args[0])()
    print("done2")
