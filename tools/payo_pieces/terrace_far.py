"""PAYO land kit: the FAR and MID terrace modules (Blender). New files only; the approved full modules are not rebuilt.

  blender -b --python tools/payo_pieces/terrace_far.py

Into Logs/payo/pieces/build/terrace, for kind in straight, convex, concave and paddy in young, ripe, flooded:
  terrace_<kind>_<paddy>_far.glb      12 m   } FAR: a soft GREEN band over a darker grey band, and nothing else.
  terrace_<kind>_<paddy>_far24.glb    24 m   } No spout, no seep, no pale stone, no feature at a module's ends:
  terrace_<kind>_<paddy>_far48.glb    48 m   } thousands of them must never draw a grid on a hillside.
  terrace_<kind>_<paddy>_mid.glb      12 m, with the spout fall and the seep: use on about one module in five.
  terrace_<kind>_<paddy>_mid_plain.glb  12 m, without them: the other four.
Same pivot (foot of the wall, left end), footprint and curve as the full module; the 24 and 48 m ones simply run on
along the same line or the same 30 m radius.

FAR, by object: wall (py_terrace_far_wall: the approved wall picture flattened, darkened and mossed), dike
(py_terrace_ground), cards (py_terrace_far, alpha cut, both faces): the paddy as ONE canopy quad, the lip row of rice
standing 1.3 m (taller than life: green must win), weeds hanging 0.6 m over the cap. A flooded module has no rice:
its paddy is a strip of painted sky (object water, also py_terrace_far; give it py_water if you want the real shader).
MID: the full module's modelled wall (py_terrace_stone), a modelled lip row of rice (py_terrace_plant), cards behind.
"""
import json
import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import terrace_layout as TL   # noqa: E402
import terrace_lib as K       # noqa: E402
import terrace_build as TB    # noqa: E402
from mathutils import Vector  # noqa: E402

D0 = TB.D0
VB = dict(young_side=(0.875, 1.0), ripe_side=(0.75, 0.875), young_top=(0.5, 0.75), ripe_top=(0.25, 0.5),
          young_canopy=(0.158, 0.186), ripe_canopy=(0.127, 0.155), flooded_canopy=(0.066, 0.123))


class Way(TB.Way):
    """The full module's line, run on to any length."""

    def __init__(self, kind, length):
        TB.Way.__init__(self, kind)
        self.length = length
        pts = {0: (0.0, 0.0)}
        for sign in (-1, 1):
            x = y = s = 0.0
            for i in range(1, int((1.0 if sign < 0 else length + 1.0) / self.step) + 1):
                th = self.heading(s + sign * self.step * 0.5)
                x += sign * math.cos(th) * self.step
                y += sign * math.sin(th) * self.step
                s += sign * self.step
                pts[sign * i] = (x, y)
        self.pts = pts


def far_strips(way, variant, seed):
    """The far module: wall, dike, cards, water. Every strip's picture starts at its own random place along the
    picture, so no two kinds of module line their stones or their tufts up."""
    r = random.Random(seed)
    wall, ground, cards, water = K.MB(), K.MB(), K.MB(), K.MB()
    L = way.length
    seg = max(1, int(round(L / (12.0 if way.kind == "straight" else 3.0))))
    uo_wall, uo_dike, uo_card, uo_fr = r.uniform(0, 1), r.uniform(0, 1), r.uniform(0, 1), r.uniform(0, 1)
    out = 0.08
    for i in range(seg):
        a, b = L * i / seg, L * (i + 1) / seg
        wa, wb = a / TL.L + uo_wall, b / TL.L + uo_wall
        vt = (TL.T - 0.03) / TL.TEX_V
        wall.face([way.face(a, 0, out), way.face(b, 0, out), way.face(b, TL.T - 0.03, out), way.face(a, TL.T - 0.03, out)],
                  [(wa, 0.0), (wb, 0.0), (wb, vt), (wa, vt)])
        wall.face([way.face(a, TL.T - 0.03, out), way.face(b, TL.T - 0.03, out), way.at(b, D0 + 0.27, TL.H), way.at(a, D0 + 0.27, TL.H)],
                  [(wa, vt), (wb, vt), (wb, (TL.T + 0.27) / TL.TEX_V), (wa, (TL.T + 0.27) / TL.TEX_V)])
        prof = [(0.13, 0.0, 1.0), (0.30, 0.17, 0.80), (0.56, -0.06, 0.56)]
        for k in range(2):
            (d0, z0, v0), (d1, z1, v1) = prof[k], prof[k + 1]
            ground.face([way.at(a, D0 + d0, TL.H + z0), way.at(b, D0 + d0, TL.H + z0), way.at(b, D0 + d1, TL.H + z1), way.at(a, D0 + d1, TL.H + z1)],
                        [(a / 4 + uo_dike, v0), (b / 4 + uo_dike, v0), (b / 4 + uo_dike, v1), (a / 4 + uo_dike, v1)])
        ua, ub = a / 4 + uo_card, b / 4 + uo_card
        zw = TL.WATER_Z - 0.03
        v0, v1 = VB[variant + "_canopy"]
        zt = TL.WATER_Z + (0.0 if variant == "flooded" else 0.30 if variant == "young" else 0.42)
        tilt = 0.40 if variant == "flooded" else 0.0                  # cheat: the flooded paddy leans toward you a little, so its sky shows from low down
        q = [way.at(a, D0 + 0.52, zt), way.at(b, D0 + 0.52, zt), way.at(b, D0 + 0.55 + TL.PADDY, zt + tilt), way.at(a, D0 + 0.55 + TL.PADDY, zt + tilt)]
        (water if variant == "flooded" else cards).face(q, [(ua * 0.5, v0), (ub * 0.5, v0), (ub * 0.5, v1), (ua * 0.5, v1)])
        if variant == "flooded":                                       # and a brim of sky standing at the dike, where the rice band would be
            vm = (v0 + v1) / 2
            water.face([way.at(a, D0 + 0.50, TL.H + 0.10), way.at(b, D0 + 0.50, TL.H + 0.10), way.at(b, D0 + 0.50, TL.H + 0.50), way.at(a, D0 + 0.50, TL.H + 0.50)],
                       [(ua * 0.5, v0), (ub * 0.5, v0), (ub * 0.5, vm), (ua * 0.5, vm)])
        if variant != "flooded":
            s0, s1 = VB[variant + "_side"]
            cards.face([way.at(a, D0 + 0.66, zw), way.at(b, D0 + 0.66, zw), way.at(b, D0 + 0.66, zw + 1.05), way.at(a, D0 + 0.66, zw + 1.05)],
                       [(ua, s0), (ub, s0), (ub, s1), (ua, s1)])
        fa, fb = a / 4 + uo_fr, b / 4 + uo_fr
        cards.face([way.at(a, D0 - 0.24, TL.H - 0.36), way.at(b, D0 - 0.24, TL.H - 0.36), way.at(b, D0 - 0.05, TL.H + 0.08), way.at(a, D0 - 0.05, TL.H + 0.08)],
                   [(fa, 0.1885), (fb, 0.1885), (fb, 0.249), (fa, 0.249)])
    return wall, ground, cards, water


def mid_strips(way, variant, falls):
    ground, water, cards, fall = K.MB(), K.MB(), K.MB(), K.MB()
    L = way.length
    seg = 6
    for i in range(seg):
        a, b = L * i / seg, L * (i + 1) / seg
        prof = [(0.13, 0.0, 1.0), (0.30, 0.17, 0.80), (0.56, -0.06, 0.52)]
        for k in range(2):
            (d0, z0, v0), (d1, z1, v1) = prof[k], prof[k + 1]
            ground.face([way.at(a, D0 + d0, TL.H + z0), way.at(b, D0 + d0, TL.H + z0), way.at(b, D0 + d1, TL.H + z1), way.at(a, D0 + d1, TL.H + z1)],
                        [(a / 4, v0), (b / 4, v0), (b / 4, v1), (a / 4, v1)])
        q = [way.at(a, D0 + 0.50, TL.WATER_Z), way.at(b, D0 + 0.50, TL.WATER_Z), way.at(b, D0 + 0.55 + TL.PADDY, TL.WATER_Z), way.at(a, D0 + 0.55 + TL.PADDY, TL.WATER_Z)]
        water.face(q, [(p.x / 4, p.y / 4) for p in q])
        ua, ub = a / 4, b / 4
        zw = TL.WATER_Z - 0.03
        if variant in ("young", "ripe"):
            v0, v1 = VB[variant + "_side"]
            cards.face([way.at(a, D0 + 1.32, zw), way.at(b, D0 + 1.32, zw), way.at(b, D0 + 1.32, zw + 0.72), way.at(a, D0 + 1.32, zw + 0.72)],
                       [(ua + 0.37, v0), (ub + 0.37, v0), (ub + 0.37, v1), (ua + 0.37, v1)], (0.9, 0.9, 0.9, 1))
            v0, v1 = VB[variant + "_top"]
            zt = TL.WATER_Z + (0.20 if variant == "young" else 0.34)
            cards.face([way.at(a, D0 + 0.55, zt), way.at(b, D0 + 0.55, zt), way.at(b, D0 + 2.55, zt), way.at(a, D0 + 2.55, zt)],
                       [(ua, v0), (ub, v0), (ub, v1), (ua, v1)])
        cards.face([way.at(a, D0 - 0.24, TL.H - 0.40), way.at(b, D0 - 0.24, TL.H - 0.40), way.at(b, D0 - 0.05, TL.H + 0.08), way.at(a, D0 - 0.05, TL.H + 0.08)],
                   [(ua, 0.1885), (ub, 0.1885), (ub, 0.249), (ua, 0.249)])
        cards.face([way.at(a, D0 + 0.28, TL.H + 0.13), way.at(b, D0 + 0.28, TL.H + 0.13), way.at(b, D0 + 0.28, TL.H + 0.63), way.at(a, D0 + 0.28, TL.H + 0.63)],
                   [(ua, 0.001), (ub, 0.001), (ub, 0.0615), (ua, 0.0615)])
    if falls:
        for s, w, o0, o1, u0 in ((3.4, 0.09, 0.50, 0.75, 0.30), (8.3, 0.06, 0.16, 0.15, 0.70)):
            top = way.at(s, D0 - o0, TL.H - 0.05) if o0 > 0.3 else way.face(s, TL.T + 0.02, o0)
            bot = way.at(s, -o1, 0.0) if o0 > 0.3 else way.face(s, 0.0, o1)
            al = way.along(s)
            fall.face([bot - al * w * 1.3, bot + al * w * 1.3, top + al * w, top - al * w], [(u0, 0.3), (u0 + 0.1, 0.3), (u0 + 0.1, 0.9), (u0, 0.9)])
    return ground, water, cards, fall


def lip_row(way, variant, seed=3):
    """The first row of rice, modelled, three blades a tuft."""
    r = random.Random(seed)
    mb = K.MB()
    s = 0.15
    while s < way.length - 0.05:
        p = way.at(s + r.uniform(-0.04, 0.04), D0 + 0.72 + r.uniform(-0.03, 0.03), TL.WATER_Z - 0.03)
        if variant == "young":
            K.tuft(mb, p, r, n=3, h=0.74 * r.uniform(0.9, 1.1), w=0.11, lean=0.42, cell=0, shade=r.uniform(0.9, 1.0))
        else:
            K.tuft(mb, p, r, n=2, h=0.74 * r.uniform(0.9, 1.1), w=0.08, lean=0.36, cell=1, shade=r.uniform(0.9, 1.0))
            K.tuft(mb, p, r, n=2, h=0.78, w=0.10, lean=0.75, cell=2, droop=0.16, spread=0.02)
        s += 0.22 if variant == "young" else 0.26
    return mb


def make_far(kind, variant, length):
    K.reset()
    way = Way(kind, length)
    name = "terrace_%s_%s_far%s" % (kind, variant, "" if length == 12 else str(int(length)))
    wall, ground, cards, water = far_strips(way, variant, seed=hash((kind, variant, length)) % 100000 if False else sum(map(ord, name)))
    m_far = K.material("py_terrace_far", "py_terrace_far.png", alpha=True)
    obs = [wall.obj(name + "_wall", K.material("py_terrace_far_wall", "py_terrace_far_wall.png")),
           ground.obj(name + "_dike", K.material("py_terrace_ground", "py_terrace_ground.png")), cards.obj(name + "_cards", m_far)]
    if water.f:
        obs.append(water.obj(name + "_water", m_far))
    end = way.at(length)
    return name, obs, dict(level="far", length=length, end=[round(end.x, 3), round(end.y, 3)], end_heading_deg=round(math.degrees(way.heading(length)), 2))


def make_mid(kind, variant, falls):
    K.reset()
    way = TB.Way(kind)
    name = "terrace_%s_%s_mid%s" % (kind, variant, "" if falls else "_plain")
    ground, water, cards, fall = mid_strips(way, variant, falls)
    obs = [TB.build_wall(way).obj(name + "_wall", K.material("py_terrace_stone", "py_terrace_wall.png")),
           ground.obj(name + "_dike", K.material("py_terrace_ground", "py_terrace_ground.png")),
           water.obj(name + "_water", K.material("py_water", None, colour=(0.30, 0.62, 0.66, 0.85))),
           cards.obj(name + "_cards", K.material("py_terrace_far", "py_terrace_far.png", alpha=True))]
    if falls:
        obs.append(fall.obj(name + "_falls", K.material("py_fall", "py_fall.png", alpha=True)))
    if variant != "flooded":
        obs.append(lip_row(way, variant).obj(name + "_lip", K.material("py_terrace_plant", "py_terrace_plant.png")))
    return name, obs, dict(level="mid", length=12)


if __name__ == "__main__":
    stats = {}
    for kind in ("straight", "convex", "concave"):
        for variant in ("young", "ripe", "flooded"):
            for length in (12.0, 24.0, 48.0):
                name, obs, meta = make_far(kind, variant, length)
                stats[name] = K.export(name, obs, dict(pivot="foot of the wall, left end (same as the full module)", **meta))
            for falls in (True, False):
                name, obs, meta = make_mid(kind, variant, falls)
                stats[name] = K.export(name, obs, dict(pivot="foot of the wall, left end (same as the full module)", **meta))
    path = os.path.join(K.OUT, "terrace_stats.json")
    old = json.load(open(path)) if os.path.exists(path) else {}
    old.update(stats)
    json.dump(old, open(path, "w"), indent=1)
    print("done")
