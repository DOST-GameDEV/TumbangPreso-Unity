"""PAYO town, building 6: the STILTED LONG HOUSE. A stone podium, short stilts, a long body of woven wall panels
between painted posts, a veranda along the front, a long thatch that sags in the middle and sweeps up at both ends.
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from house_kit import *

k = Kit("house_long", 2048, seed=17)
ROSE, MINT, OCHRE, CORAL, TURQ, CREAM, RED = [0.84, 0.50, 0.52], [0.56, 0.78, 0.66], [0.90, 0.68, 0.28], [0.88, 0.46, 0.34], [0.20, 0.64, 0.68], [0.95, 0.90, 0.78], [0.66, 0.22, 0.18]
GREY = [[0.56, 0.54, 0.51], [0.47, 0.455, 0.44], [0.64, 0.62, 0.58], [0.52, 0.50, 0.46], [0.60, 0.57, 0.55]]
POST = [0.60, 0.24, 0.18]

# ---- the podium: two battered steps of stone, a rose coat mostly gone, a turquoise fret
pod = Wall(0, 0, -1.5, 0.75, 14.6, 8.2, 13.8, 7.5)
k.loft("stone", pod.rings(), bevel=0.13, base=GREY, bw=1.4, course=0.56, moss=0.6, ground=0.0,
       paint=dict(col=ROSE, cover=0.3, fade=0.6), bands=[dict(z0=0.12, z1=0.62, col=TURQ, col2=CREAM, pat="fret", period=0.9, wear=0.36)])
k.loft("block", [ring(0, 0.1, 0.72, 13.5, 7.1), ring(0, 0.1, 1.05, 13.2, 6.8)], bevel=0.08, base=PALESTONE, sub=1.4, moss=0.6, foot=False)
quoins(k, pod, corners=((1, -1), (-1, -1)), n=3, seed=1)
for i, (y, z, w) in enumerate(((-4.0, 0.72, 2.6), (-4.5, 0.42, 3.0), (-5.0, 0.12, 3.4))):
    k.box("block", (-2.6 + 0.05 * i, y, z - 0.5), (w, 0.9, 1.2), bevel=0.08, base=PALESTONE, sub=1.0, moss=0.6, rz=(2, -2, 1)[i], seed=i)

# ---- stilts and the floor
ZF = 2.3
for x in (-5.6, -2.8, 0.0, 2.8, 5.6):
    for y in (-2.9, 0.0, 2.9):
        k.box("block", (x, y, 1.15), (0.8, 0.8, 0.3), bevel=0.07, base=PALESTONE, foot=False, top=(0.8, 0.8), dens=0.5, seed=int(x + y))
        k.cyl("wood", (x, y, 1.2), (x * 0.99, y * 0.98, ZF - 0.2), 0.20, 0.17, n=8, col=DARKWOOD, grain="z", dens=0.7)
for y in (-2.95, 0.0, 2.95):
    k.beam("wood", (-6.5, y, ZF - 0.1), (6.5, y, ZF - 0.1), 0.26, 0.26, col=DARKWOOD, bevel=0.03)
k.box("wood", (0, -0.1, ZF + 0.1), (12.9, 6.5, 0.16), col=[0.56, 0.38, 0.25], plank=0.38, grain="h", bevel=0.03)

# ---- the body: woven panels between painted posts
ZW = 4.75
BY0, BY1 = -1.0, 3.0                                   # the body's front and back walls; the veranda is in front
body = Wall(0, (BY0 + BY1) / 2, ZF + 0.18, ZW, 11.4, BY1 - BY0, 11.6, BY1 - BY0 + 0.1)
k.loft("weave", body.rings(), col=[0.84, 0.68, 0.40], col2=[0.62, 0.46, 0.26], col3=[0.60, 0.25, 0.19], strip=0.15, diamond=0.62, dline=0.07, edge=0.0)
for i, x in enumerate((-5.75, -3.45, -1.15, 1.15, 3.45, 5.75)):
    for y in (BY0 - 0.04, BY1 + 0.04):
        k.box("wood", (x, y, (ZF + ZW) / 2 + 0.1), (0.30, 0.30, ZW - ZF + 0.1), col=POST, grain="z", bevel=0.04, rz=i * 3,
              bands=[dict(z0=ZF + 0.9, z1=ZF + 1.1, col=CREAM), dict(z0=ZW - 0.5, z1=ZW - 0.2, col=OCHRE, col2=RED, pat="dot", period=0.15)])
for y in (BY0 - 0.05, BY1 + 0.05):
    k.beam("wood", (-5.95, y, ZW + 0.05), (5.95, y, ZW + 0.05), 0.26, 0.24, col=DARKWOOD, bevel=0.03)
    k.beam("wood", (-5.9, y, ZF + 1.0), (5.9, y, ZF + 1.0), 0.14, 0.12, col=DARKWOOD)
# the door and three lit windows on the front, two at the back, one each end
dz = ZF + 0.18
obox(k, "door", (-2.3, BY0, dz + 0.95), "y-", 1.0, 0.14, 1.9, out=0.07, col=TURQ, face="y-", hide=others("y-"), dens=1.5, plank=0.26,
     decals=[dict(dir="y-", motif="weave", at=(-2.3, 0, dz + 1.2), size=0.7, col=OCHRE, wear=0.2)])
obox(k, "wood", (-2.3, BY0, dz + 2.02), "y-", 1.5, 0.3, 0.22, out=0.1, col=DARKWOOD, bevel=0.03)
for s in (-1, 1):
    obox(k, "wood", (-2.3, BY0, dz + 0.95), "y-", 0.18, 0.26, 1.94, out=0.08, side=s * 0.59, col=DARKWOOD, bevel=0.03)


def shutter_window(at, d, w=0.95, h=0.9, lat="dia"):
    at = Vector(at)
    obox(k, "glow", at, d, w, 0.16, h, out=0.05, glow=True, lattice=lat, hide=others(d))
    obox(k, "wood", at + Vector((0, 0, h / 2 + 0.08)), d, w + 0.4, 0.26, 0.14, out=0.08, col=DARKWOOD)
    obox(k, "wood", at - Vector((0, 0, h / 2 + 0.08)), d, w + 0.5, 0.36, 0.12, out=0.12, col=DARKWOOD)
    for s in (-1, 1):
        obox(k, "wood", at, d, 0.12, 0.2, h, out=0.07, side=s * (w / 2 + 0.06), col=DARKWOOD)
    # a propped shutter of woven cane
    o, r = DIRS[d]
    c = at + Vector((0, 0, h / 2 + 0.02)) + o * 0.42
    k.box("weave", c, (w + 0.1, 0.8, 0.06) if d[0] == "y" else (0.8, w + 0.1, 0.06), rx=(-32 if d == "y-" else 32 if d == "y+" else 0), ry=(32 if d == "x+" else -32 if d == "x-" else 0),
          col=[0.80, 0.62, 0.34], strip=0.09, edge=0.02, dens=0.8)


for x in (0.0, 2.3, 4.6):
    shutter_window((x, BY0, ZF + 1.75), "y-")
for x in (-3.5, 2.4):
    shutter_window((x, BY1, ZF + 1.75), "y+", lat="grid")
shutter_window((5.75, 1.0, ZF + 1.75), "x+", w=0.8)
shutter_window((-5.7, 1.0, ZF + 1.75), "x-", w=0.8, lat="grid")

# ---- the veranda: rail posts, a woven rail, washing over it, a bench
VY = -3.15
for i, x in enumerate((-6.2, -3.9, -1.25, 1.3, 3.8, 6.2)):
    k.box("wood", (x, VY, ZF + 0.72), (0.2, 0.2, 1.25), top=(0.8, 0.8), col=POST, grain="z", bevel=0.03, rz=i * 5, bands=[dict(z0=ZF + 1.0, z1=ZF + 1.15, col=CREAM)])
k.beam("wood", (-6.3, VY, ZF + 1.2), (-3.8, VY, ZF + 1.22), 0.14, 0.12, col=DARKWOOD)
k.beam("wood", (-1.3, VY, ZF + 1.22), (6.3, VY, ZF + 1.18), 0.14, 0.12, col=DARKWOOD)
for x0, x1 in ((-6.1, -4.0), (-1.15, 1.2), (1.4, 3.7), (3.9, 6.1)):
    k.box("weave", ((x0 + x1) / 2, VY, ZF + 0.72), (x1 - x0, 0.06, 0.66), col=[0.82, 0.66, 0.38], col3=TURQ, strip=0.11, diamond=0.33, edge=0.0)
for x in (-6.25, 6.25):
    k.beam("wood", (x, VY, ZF + 1.2), (x, BY0, ZF + 1.2), 0.12, 0.12, col=DARKWOOD)
for i, (x, c, w) in enumerate(((2.5, [0.80, 0.24, 0.20], 1.1), (4.9, [0.95, 0.90, 0.78], 0.8), (-5.0, [0.20, 0.60, 0.64], 0.9))):
    k.box("cloth", (x, VY - 0.09, ZF + 0.82), (w, 0.06, 0.9), col=c, seed=i, rz=(i - 1) * 2)
    k.box("cloth", (x, VY, ZF + 1.3), (w + 0.02, 0.24, 0.06), col=c, seed=i, stripes=[], rz=(i - 1) * 2)
for i in range(3):                                       # timber steps up from the podium
    k.box("wood", (-2.6, -3.55 - i * 0.32, ZF - 0.05 - i * 0.36), (1.6, 0.4, 0.14), col=[0.56, 0.38, 0.25], plank=0.2, grain="h", bevel=0.02)
for s in (-0.85, 0.85):
    k.beam("wood", (-2.6 + s, -3.3, ZF + 0.1), (-2.6 + s, -4.5, ZF - 1.2), 0.12, 0.2, col=DARKWOOD)
k.box("wood", (3.4, -1.55, ZF + 0.62), (1.9, 0.5, 0.1), col=[0.56, 0.38, 0.25], plank=0.25, grain="h", bevel=0.02)
for s in (-0.8, 0.8):
    k.box("wood", (3.4 + s, -1.55, ZF + 0.38), (0.12, 0.44, 0.44), col=DARKWOOD)
jar(k, (5.6, -2.0, ZF + 0.18), 0.34, 0.8)
jar(k, (5.0, -1.5, ZF + 0.18), 0.26, 0.55, col=(0.56, 0.36, 0.30))
lantern(k, (-0.9, -2.9, ZF + 2.15), 0.85)
k.rope((-0.9, -2.9, ZW + 0.2), (-0.9, -2.9, ZF + 2.45), sag=0, n=1)

# ---- the roof: sags in the middle, sweeps up to a prow at each end
TH = dict(col=[0.74, 0.62, 0.32], dark=[0.38, 0.30, 0.18], layer=0.5, moss=0.85, cx=0, cy=0.5)
HL = 6.9
YF, YB, RY = -4.25, 4.35, 0.4
rings = []
N = 9
for i in range(N):
    u = -1 + 2 * i / (N - 1)
    x = u * HL
    up = 1.25 * abs(u) ** 2.2                            # the ridge climbs toward the ends
    zr = 7.9 + up + 0.12 * math.sin(i * 2.1)
    ze = 4.15 + 0.35 * abs(u) ** 2 + (0.10 if i % 2 else -0.08)
    xr = x * (1.0 + 0.16 * abs(u) ** 2)                  # and leans out past the eaves
    xm = x * (1.0 + 0.06 * abs(u) ** 2)
    rings.append([(x, YF, ze), (xm, YF * 0.50 + RY * 0.5, ze + (zr - ze) * 0.40), (xr, RY, zr), (xm, YB * 0.50 + RY * 0.5, ze + (zr - ze) * 0.40), (x, YB, ze + 0.1),
                  (x, YB - 0.12, ze - 0.24), (x, RY, ze + 0.45), (x, YF + 0.12, ze - 0.34)])
k.loft("thatch", rings, vscale=1.1, bands=[dict(z0=7.3, z1=7.55, col=RED)], **TH)
# the ridge roll, bound in red, and crossed horns on each prow
for i in range(N - 1):
    a, b = rings[i][2], rings[i + 1][2]
    k.cyl("thatch", (a[0], a[1], a[2] + 0.05), (b[0], b[1], b[2] + 0.05), 0.30, 0.30, n=6, col=[0.70, 0.50, 0.26], layer=0.3, moss=0.7, dens=0.6)
for e in (-1, 1):
    px, pz = rings[0 if e < 0 else -1][2][0], rings[0 if e < 0 else -1][2][2]
    k.box("wood", (px - e * 0.1, RY, pz + 0.2), (0.5, 0.7, 0.5), col=RED, bevel=0.04, bands=[])
    for s in (-1, 1):
        k.beam("wood", (px - e * 0.1, RY - s * 0.2, pz + 0.2), (px + e * 0.5, RY + s * 1.0, pz + 1.7), 0.17, 0.17, w1=0.05, h1=0.05, col=DARKWOOD)
    # the gable: a rose board with a painted mark
    gx = e * 5.85
    k.loft("wood", [[(gx - 0.06, -1.9, ZW + 0.1), (gx - 0.06, 3.7, ZW + 0.1), (gx - 0.06 + e * 0.5, RY, 7.6)], [(gx + 0.06, -1.9, ZW + 0.1), (gx + 0.06, 3.7, ZW + 0.1), (gx + 0.06 + e * 0.5, RY, 7.6)]],
           col=[0.52, 0.35, 0.23], plank=0.4, grain="z", paint=dict(col=ROSE, cover=0.7, scale=0.8),
           decals=[dict(dir="x+" if e > 0 else "x-", motif="sun" if e > 0 else "face", at=(0, 0.75, 5.85), size=1.5, col=CREAM if e > 0 else OCHRE, wear=0.2)])

# ---- under the floor: firewood, a hammock, jars; plants
for i in range(8):
    r, c = divmod(i, 4)
    k.cyl("wood", (3.4 + c * 0.34 + 0.17 * (r % 2), 1.0, 1.25 + r * 0.3), (3.45 + c * 0.34 + 0.17 * (r % 2), 2.1, 1.25 + r * 0.3), 0.15, 0.14, n=6, col=[0.50 + 0.05 * (i % 3), 0.35, 0.22], grain="h", dens=0.5)
h0, h1 = Vector((-5.5, -2.8, ZF - 0.3)), Vector((-2.9, -2.8, ZF - 0.3))
pts = k.rope(h0, h1, sag=0.5, n=6)
for a, b in zip(pts[1:-1], pts[2:-1]):
    m = (a + b) / 2
    k.box("cloth", (m.x, m.y, m.z - 0.02), ((b - a).length + 0.05, 0.8, 0.06), ry=-math.degrees(math.atan2(b.z - a.z, b.x - a.x)), col=CREAM, stripes=[], col2=RED, dens=0.6)
for i, x in enumerate((-4.6, -4.1, 1.9, 5.2, 5.7)):
    k.rope((x, -3.55, 4.25), (x, -3.55, 3.85), sag=0, n=1)
    k.box("thatch", (x, -3.55, 3.5), (0.3, 0.3, 0.75), top=(0.25, 0.25), col=[0.86, 0.66, 0.30], layer=0.3, rz=i * 29, dens=0.7, moss=0)
jar(k, (0.9, -2.2, 1.05), 0.42, 1.0)
jar(k, (1.8, -2.3, 1.05), 0.3, 0.6, col=(0.74, 0.50, 0.30))
k.plant((7.0, -3.6, 0.0), n=6, size=1.8, seed=1)
k.plant((-7.2, 3.8, 0.0), n=6, size=2.0, seed=2)
k.plant((-6.9, -3.9, 0.0), n=5, size=1.3, seed=3)
k.vine([(6.9, 2.0, 4.6), (7.0, 2.1, 3.8), (6.85, 2.0, 3.0), (7.0, 2.1, 2.2)], leaves=6, seed=4)


def deform(v):
    z = max(v.z, 0.0)
    sag = -0.010 * (36 - min(v.x * v.x, 36)) * min(z / 4.0, 1.0) * 0.25
    return (v.x + 0.035 * z ** 1.3, v.y - 0.012 * z ** 1.3, v.z + sag + 0.012 * v.x * min(z / 3.0, 1.0))


k.deform = deform
k.meta.update(note="glow: material house_long_glow (seven windows, veranda lantern), same atlas",
              close=[(-0.5, -2.0, 3.4), (4.2, -9.5, 3.0)], close_label="veranda, woven panels, door", distance=17.0, look=0.42, figure=(-0.6, -5.4), fov=62)
k.build()
