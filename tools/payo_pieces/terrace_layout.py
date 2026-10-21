"""PAYO land kit: the numbers the painter and the modeller must agree on (pure Python, no bpy, no Pillow).

The terrace wall is ONE unfolded strip: u runs along the wall (12 m a tile), v runs up the leaning face and then
back over the cap stones. terrace_paint.py paints every stone where stones() puts it; terrace_build.py raises a
chunky block under each painted stone. Same list, same seed, so paint and form always agree.
"""
import math
import random

L = 12.0                      # metres of wall a module covers, and the width of one tile of the wall picture
H = 1.15                      # wall height (cheated low: Batad walls are 2 to 4 m)
BATTER = math.radians(7.0)    # lean back
T = H / math.cos(BATTER)      # length up the leaning face
CAP = 0.30                    # metres of cap stone unfolded past the top edge
TEX_V = 1.5                   # metres of wall picture height (2048 x 256 px: 170.7 px a metre)
JOINT = 0.036
COURSES = (0.43, 0.39, T - 0.82)

# Cross-section behind the wall top, as depth into the hill from the wall top edge.
CAP_SHOW = 0.13               # bare cap stone before the grass starts
BUND = 0.40                   # width of the grassy dike
WATER_Z = H - 0.035           # still water level
PADDY = 2.00                  # depth of paddy modelled behind the dike (the next wall up sits on it)

# Owner, 2026-10-12: "the stones were the old grey", the blue of reference 238 is LIGHT, not paint. Neutral to
# slightly warm greys with a real spread of value from stone to stone.
STONES = [(156, 154, 148), (170, 167, 160), (148, 146, 142), (184, 180, 170), (162, 158, 150), (150, 147, 142),
          (190, 186, 178), (144, 142, 140), (176, 170, 158), (158, 154, 146)]
WARM = [(182, 168, 140), (190, 174, 138)]


def stones(seed=7):
    """Every stone as a dict: s0, s1 (along, may pass 12: it overhangs into the next module, which has the matching
    gap), t0, t1 (up the face), top (it is a cap stone), out (how far it stands proud), tone, id."""
    r = random.Random(seed)
    out = []

    def add(s0, s1, t0, t1, top=False):
        tone = r.choice(WARM) if r.random() < 0.10 else r.choice(STONES)
        f = r.uniform(0.86, 1.0)
        out.append(dict(s0=s0, s1=s1, t0=t0, t1=t1, top=top, out=r.uniform(0.05, 0.11) + (0.02 if top else 0),
                        tone=tuple(min(255, int(c * f)) for c in tone), id=len(out), lift=r.uniform(-0.025, 0.03)))

    def course(t0, t1, a, b, lo, hi, top=False):
        s = a
        while s < b - 1e-6:
            w = r.uniform(lo, hi)
            if b - (s + w) < lo * 0.8:
                w = b - s
            add(s, s + w, t0, t1, top)
            s += w

    h0, h1, h2 = COURSES
    # the lower two courses as one band: now and then one tall stone fills both
    s = r.uniform(0.0, 0.3)
    start = s
    end = start + L
    while s < end - 1e-6:
        if r.random() < 0.30:
            w = min(r.uniform(0.44, 0.62), end - s)
            if end - (s + w) < 0.4:
                w = end - s
            add(s, s + w, 0.0, h0 + h1)
            s += w
        else:
            w = r.uniform(0.9, 2.1)
            if end - (s + w) < 0.6:
                w = end - s
            split = h0 + r.uniform(-0.10, 0.08)
            course(0.0, split, s, s + w, 0.42, 0.80)
            course(split, h0 + h1, s, s + w, 0.36, 0.70)
            s += w
    a = r.uniform(0.3, 0.6)
    course(h0 + h1, T, a, a + L, 0.40, 0.78, top=True)
    return out


def corners(st, seed=7):
    """The stone's four corners (s, t), anticlockwise from bottom left, with the joint taken off and a little skew."""
    r = random.Random(seed * 1000 + st["id"])
    g = JOINT / 2
    j = lambda: r.uniform(-0.028, 0.028)
    top = st["t1"] + (CAP if st["top"] else -g)
    return [(st["s0"] + g + j(), st["t0"] + g + j()), (st["s1"] - g + j(), st["t0"] + g + j()),
            (st["s1"] - g + j(), top + (0 if st["top"] else j())), (st["s0"] + g + j(), top + (0 if st["top"] else j()))]
