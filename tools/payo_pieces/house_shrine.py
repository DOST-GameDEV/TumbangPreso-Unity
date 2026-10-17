"""PAYO town, building 2: the STEPPED SHRINE. Four battered tiers, a rose stair ramp up the front between ochre
balustrades, two stone heads at its foot, a small turquoise cella on top under a slab roof and a painted roof comb.
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from house_kit import *

k = Kit("house_shrine", 2048, seed=5)
ROSE, MINT, OCHRE, CORAL, TURQ, CREAM = [0.84, 0.50, 0.52], [0.56, 0.78, 0.66], [0.90, 0.68, 0.28], [0.88, 0.46, 0.34], [0.20, 0.64, 0.68], [0.95, 0.90, 0.78]
GREY = [[0.56, 0.54, 0.51], [0.47, 0.455, 0.44], [0.64, 0.62, 0.58], [0.52, 0.50, 0.46], [0.60, 0.57, 0.55]]

# ---- tiers
W = [11.0, 9.0, 7.1, 5.4]
D = [10.0, 8.3, 6.6, 5.0]
CY = [0.0, 0.25, 0.45, 0.6]
H = 1.5
tiers = []
paints = [dict(col=OCHRE, cover=0.20, fade=1.5), dict(col=MINT, cover=0.62, fade=0.5), dict(col=ROSE, cover=0.66, fade=0.5), dict(col=OCHRE, cover=0.72, fade=0.3)]
bands = [dict(col=CORAL, col2=CREAM, pat="step", period=1.1), dict(col=OCHRE, col2=CORAL, pat="tri", period=0.6), dict(col=CREAM, col2=TURQ, pat="fret", period=0.8), dict(col=CORAL, col2=CREAM, pat="dot", period=0.5)]
for i in range(4):
    z0 = i * H if i else -1.5
    z1 = (i + 1) * H
    w = Wall(0, CY[i], z0, z1, W[i] + (0.5 if i == 0 else 0), D[i] + (0.5 if i == 0 else 0), W[i] - 0.5, D[i] - 0.5)
    tiers.append(w)
    dec = []
    if i == 1:
        dec = [dict(dir="y-", motif="face", at=(s * 3.05, 0, 2.2), size=1.05, col=CORAL, wear=0.28) for s in (-1, 1)]
        dec += [dict(dir="x+", motif="lizard", at=(0, 0.3, 2.25), size=1.3, col=CREAM, rot=90, wear=0.3), dict(dir="x-", motif="spiral", at=(0, 0.3, 2.25), size=1.1, col=CREAM, wear=0.3)]
    if i == 2:
        dec = [dict(dir="x+", motif="sun", at=(0, 0.4, 3.7), size=1.0, col=OCHRE, wear=0.3), dict(dir="y+", motif="moon", at=(0, 0, 3.7), size=1.0, col=CREAM, wear=0.3)]
    kw = dict(ground=0.0) if i == 0 else {}
    k.loft("stone", w.rings(), bevel=0.11, base=GREY, bw=1.2, course=0.5, moss=0.55, foot=(i == 0), topmoss=0.3,
           paint=paints[i], bands=[dict(z0=z1 - 0.62, z1=z1 - 0.12, wear=0.32, **bands[i])], decals=dec, hide=["z-"], **kw)
    # the cap slab: a strong pale line round every step
    k.loft("block", [ring(0, CY[i], z1 - 0.02, W[i] - 0.3, D[i] - 0.3), ring(0, CY[i], z1 + 0.30, W[i] - 0.05, D[i] - 0.05)], bevel=0.07, base=PALESTONE, sub=1.3, moss=0.7, foot=False, hide=["z-"], dens=0.8)
for (sx, sy, n, sd) in ((1, -1, 4, 1), (-1, -1, 4, 2)):
    quoins(k, tiers[0], corners=((sx, sy),), n=n, seed=sd)

# ---- the stair ramp
y0, y1, ztop = -8.3, tiers[3].at("y-", 6.0).y + 0.3, 6.3


def wedge(x0, x1, lift, kind, **kw):
    prof = [(y0 - lift * 0.4, -1.5), (y0 - lift * 0.4, 0.0 + lift * 0.4), (y1, ztop + lift), (y1, -1.5)]
    return k.loft(kind, [[(x, y, z) for y, z in prof] for x in (x0, x1)], **kw)


wedge(-1.45, 1.45, -0.15, "stone", bevel=0.05, base=GREY, bw=1.0, course=0.5, paint=dict(col=ROSE, cover=0.8, fade=0), moss=0.3, ground=0.0)
for s in (-1, 1):
    wedge(s * 1.35, s * 2.1, 0.55, "stone", bevel=0.10, base=PALESTONE, bw=1.3, course=0.62, paint=dict(col=OCHRE, cover=0.62, fade=0), moss=0.45, ground=0.0, seed=s)
    # a stone head at the foot of each balustrade
    hx = s * 1.75
    k.box("block", (hx, y0 - 0.9, 0.0), (1.5, 1.5, 1.6), bevel=0.12, base=GREY, sub=0.9, moss=0.6, top=(0.9, 0.9), seed=3 + s)
    hy = y0 - 0.95
    HEAD = dict(base=PALESTONE, foot=False, moss=0.35, paint=dict(col=TURQ if s > 0 else MINT, cover=0.5, fade=0, scale=1.6))
    k.loft("block", [ring(hx, hy, 0.75, 1.35, 1.3), ring(hx, hy - 0.05, 1.6, 1.6, 1.4), ring(hx, hy, 2.45, 1.25, 1.2)], bevel=0.16, seed=7 + s, **HEAD)      # a swelling head
    k.box("block", (hx, hy - 0.72, 2.05), (1.5, 0.34, 0.30), bevel=0.08, seed=8, rx=-8, **HEAD)                           # the brow
    k.box("block", (hx, hy - 0.80, 1.62), (0.34, 0.42, 0.75), bevel=0.08, top=(0.6, 0.7), seed=9, **HEAD)                # the nose
    for e in (-1, 1):
        k.box("flat", (hx + e * 0.40, hy - 0.70, 1.82), (0.34, 0.12, 0.2), col=[0.16, 0.15, 0.15], bevel=0.03)           # sunk eyes
        k.box("glow", (hx + e * 0.40, hy - 0.74, 1.82), (0.14, 0.1, 0.12), glow=True, lattice="none")                    # that light at night
        k.box("block", (hx + e * 0.86, hy - 0.05, 1.65), (0.26, 0.5, 0.95), bevel=0.08, top=(0.8, 0.8), seed=10 + e, **HEAD)   # long ears
    k.box("flat", (hx, hy - 0.70, 1.08), (0.9, 0.12, 0.26), col=[0.16, 0.15, 0.15], bevel=0.03)                           # the mouth
    for i in range(4):
        k.box("block", (hx - 0.3 + i * 0.2, hy - 0.74, 1.08), (0.14, 0.1, 0.22), base=[[0.86, 0.84, 0.76]], foot=False, moss=0, bevel=0.02, dens=0.6)
    k.box("block", (hx, hy, 2.56), (1.55, 1.5, 0.32), bevel=0.08, base=PALESTONE, moss=0.7, foot=False, paint=dict(col=CORAL, cover=0.6, fade=0), rz=s * 5)
n = 13
for i in range(n):
    t = (i + 0.5) / n
    y = y0 + (y1 - y0) * t
    z = (ztop - 0.15) * (i + 1) / n
    k.box("block", (0.0 + 0.04 * math.sin(i * 2.1), y + 0.1, z - 0.3), (2.75, 0.72, 0.6), base=[[0.80, 0.52, 0.52], [0.74, 0.46, 0.46], [0.84, 0.58, 0.54], [0.62, 0.58, 0.54]], bevel=0.05,
          moss=0.35, foot=False, seed=i, sub=1.5, hide=["z-", "y+", "x+", "x-"], dens=0.8, rz=math.sin(i * 1.3) * 1.6)

# ---- the cella
c = Wall(0, 0.75, 6.28, 8.9, 3.5, 3.0, 3.1, 2.7)
k.loft("stone", c.rings(), bevel=0.09, base=GREY, bw=0.9, course=0.46, moss=0.4, foot=False, eave=8.9, eave_len=0.7,
       paint=dict(col=TURQ, cover=0.72, fade=0.5), bands=[dict(z0=6.35, z1=6.8, col=CREAM, col2=CORAL, pat="zig", period=0.6, wear=0.3)],
       decals=[dict(dir="x+", motif="rice", at=(0, 0.75, 7.75), size=1.3, col=CREAM, wear=0.28), dict(dir="x-", motif="hand", at=(0, 0.75, 7.7), size=0.9, col=CORAL, wear=0.28),
               dict(dir="y+", motif="weave", at=(0, 0, 7.7), size=1.2, col=OCHRE, wear=0.28)])
dw = c.at("y-", 6.3, 0.0)
obox(k, "glow", dw + Vector((0, 0, 0.95)), "y-", 1.05, 0.16, 1.9, out=0.03, glow=True, lattice="slit", hide=others("y-"))
obox(k, "block", dw + Vector((0, 0, 2.12)), "y-", 2.1, 0.6, 0.44, out=0.10, bevel=0.07, base=PALESTONE, foot=False, top=(0.86, 0.9), paint=dict(col=CORAL, cover=0.6, fade=0))
for s in (-1, 1):
    obox(k, "block", dw + Vector((0, 0, 0.95)), "y-", 0.36, 0.5, 1.94, out=0.08, side=s * 0.72, bevel=0.06, base=PALESTONE, foot=False, top=(0.85, 0.9), seed=s + 2, paint=dict(col=CORAL, cover=0.4, fade=0))
# its roof: a flared ochre slab, a smaller one, and a tall painted comb
k.loft("block", [ring(0, 0.75, 8.85, 3.6, 3.2), ring(0, 0.7, 9.4, 4.7, 4.3)], bevel=0.09, base=PALESTONE, sub=1.1, moss=0.7, foot=False, paint=dict(col=OCHRE, cover=0.7, fade=0),
       bands=[dict(z0=8.98, z1=9.3, col=OCHRE, col2=CORAL, pat="tri", period=0.5, wear=0.3)])
k.loft("block", [ring(0, 0.8, 9.38, 3.9, 3.4), ring(0, 0.85, 9.85, 3.0, 2.5)], bevel=0.09, base=GREY, sub=1.0, moss=0.8, foot=False, paint=dict(col=ROSE, cover=0.5, fade=0))
k.loft("block", [ring(0, 1.0, 9.8, 2.9, 0.8), ring(0.05, 1.0, 11.3, 2.5, 0.62), ring(0.05, 1.0, 12.2, 1.5, 0.5)], bevel=0.09, base=PALESTONE, sub=0.9, moss=0.6, foot=False,
       paint=dict(col=CORAL, cover=0.66, fade=0),
       decals=[dict(dir="y-", motif="sun", at=(0.03, 0, 10.75), size=1.5, col=OCHRE, wear=0.2), dict(dir="y+", motif="step", at=(0.03, 0, 10.7), size=1.4, col=CREAM, wear=0.25)])
for x in (-1.2, 1.25):
    k.box("block", (x, 1.0, 11.75), (0.4, 0.5, 0.7), bevel=0.06, base=PALESTONE, foot=False, top=(0.5, 0.7), paint=dict(col=TURQ, cover=0.6, fade=0))

# ---- two fire bowls at the stair head, lit
for s in (-1, 1):
    x, y = s * 2.05, y1 + 0.55
    k.box("block", (x, y, 6.55), (0.7, 0.7, 0.6), bevel=0.07, base=PALESTONE, foot=False, top=(0.7, 0.7), seed=s)
    jar(k, (x, y, 6.85), 0.40, 0.5, col=(0.44, 0.40, 0.38))
    k.box("glow", (x, y, 7.55), (0.46, 0.46, 0.7), top=(0.15, 0.15), glow=True, lattice="none", rz=20)
# two banners behind the cella, a lit offering niche in the back wall
banner(k, (-2.7, 2.6, 4.5), h=7.2, col=CORAL, lean=(-0.5, 0.2), drop=3.0, seed=1)
banner(k, (2.6, 2.9, 4.5), h=6.2, col=TURQ, lean=(0.45, 0.3), drop=2.4, seed=2)
window(k, tiers[1].at("y+", 2.25, 0.0), "y+", 0.9, 0.8, lattice="none")

# ---- fallen blocks at one corner, plants on the steps
for i, (x, y, z, rz, rx) in enumerate(((5.9, -4.6, 0.25, 20, 8), (6.6, -3.4, 0.2, -30, -12), (5.2, -5.6, 0.2, 55, 5), (4.6, -4.3, 1.75, 14, 6))):
    k.box("block", (x, y, z), (1.1, 0.8, 0.6), rz=rz, rx=rx, bevel=0.09, base=GREY, moss=0.7, seed=i + 20)
k.plant((-4.6, -3.9, 1.8), n=6, size=1.3, seed=1)
k.plant((3.5, 3.3, 3.3), n=6, size=1.2, seed=2)
k.plant((-5.9, 4.9, 0.0), n=7, size=2.0, seed=3)
k.plant((6.0, -5.4, 0.0), n=6, size=1.6, seed=4)
k.plant((2.2, -2.0, 6.3), n=5, size=0.8, seed=5)
k.vine([(-4.3, -3.6, 3.3), (-4.4, -3.75, 2.5), (-4.25, -4.4, 1.7), (-4.4, -4.5, 0.9)], leaves=7, seed=6)
k.vine([(3.4, -2.75, 4.8), (3.5, -2.85, 4.0), (3.35, -3.7, 3.2), (3.5, -3.75, 2.4)], leaves=6, seed=7)
k.vine([(1.3, -0.6, 9.4), (1.4, -1.3, 8.8), (1.3, -1.35, 8.0), (1.45, -1.3, 7.2)], leaves=6, seed=8)


def deform(v):
    z = max(v.z, 0.0)
    t = min(max((z - 6.0) / 1.0, 0), 1)
    a = math.radians(5.0) * t
    x, y = v.x * math.cos(a) - (v.y - 0.75) * math.sin(a), 0.75 + v.x * math.sin(a) + (v.y - 0.75) * math.cos(a)
    return (x - 0.012 * z ** 1.6, y + 0.004 * z ** 1.6, v.z - 0.02 * v.x * min(z, 6) / 6 + 0.012 * abs(v.x) * (z > 9.6))


k.deform = deform
k.meta.update(note="glow: material house_shrine_glow (cella door, two fire bowls), same atlas",
              close=[(0.3, -5.0, 3.6), (5.2, -14.5, 3.4)], close_label="stair ramp, stone heads, cella", distance=20.0, look=0.42, figure=(3.4, -9.6))
k.build()
