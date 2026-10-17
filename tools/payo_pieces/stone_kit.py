"""PAYO, ONE PIECE AT A TIME: the stage stone family's kit (2026-10-12).

The owner on the second art pass: "the map currently looks like an eyesore. the old process was to build the different
buildings and props individually first to refine them." So nothing here builds a map. A piece is ONE object, typed as
closed round parts with smooth normals, wearing ITS OWN hand-painted atlas (its stones, its carving, its wear), welded
into one skin and looked at alone before it is allowed near the others. This is the project's house way of making a
model (tools/hand_companion_kit.py, tools/build_slippers.py), sized up from a slipper to a building.

  1. py -3 tools/payo_pieces/stone_build.py <piece>                  parts/<piece>.glb, <piece>-atlas.png, <piece>.json
  2. blender -b --python tools/payo_pieces/stone_weld.py -- <piece>   Logs/payo/pieces/build/stone/<piece>.glb, one closed skin
  3. blender -b --python tools/payo_pieces/stone_review.py -- <piece> four shots with an ink line and a 1.75 m figure
  4. py -3 tools/payo_pieces/stone_build.py sheet <piece> <version>   Logs/payo/pieces/stone/<piece>_vN.png

THIS BUILDER'S SCOPE IS THE STAGE STONE FAMILY (the monoliths, towers, core, stair, movers, pads). Other builders have
the gate, the statues, the houses, the terrace and waterfall kit and the props, each with their own files.

CONVENTIONS: metres, y up, +z is the piece's FRONT (glTF; in Blender the front is -Y). UVs: v down.

THE PALETTE: stone is painted GREY (neutral to slightly warm, varied stone to stone) with wide PALE painted bevels; warm
ochre, red and turquoise only as worn paint and inlay on carving; moss and vine greens. The cool blue of the owner's
reference 238 is LIGHT and air, which the game supplies: it is never painted into the stone.
"""
import json
import math
import zlib
import random
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import hand_companion_kit as hk                      # noqa: E402
import author_payo_art_paint as PP                   # noqa: E402  (the stone drawing: one stone at a time)

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Logs/payo/pieces/build/stone"
TEX = ROOT / "Assets/TumbangPreso/Art/Payo/Textures"

# STONE IS GREY (owner, 2026-10-12: "the texture all reads as too green and teal. the reference i gave only had cool blue
# LIGHTING. the stones were the old grey"). Neutral to slightly warm greys, value varying stone to stone, a few warmer
# and a few darker. The cool tone of reference 238 is the game's light and air, never the paint.
STONES = [(150, 148, 144), (166, 164, 158), (134, 132, 130), (178, 174, 166), (158, 152, 144), (142, 140, 138), (186, 184, 178), (122, 120, 120), (172, 164, 150), (150, 144, 136)]
PALE, INK, DARK = (222, 218, 208), (46, 44, 46), (78, 76, 78)
OCHRE, RED, CORAL, TURQ, ROSE, MINT, CREAM = (222, 164, 62), (184, 70, 54), (232, 128, 96), (64, 208, 190), (226, 150, 150), (150, 216, 178), (236, 226, 198)
MOSS, MOSS_L, LEAF, LEAF_L, BARK = (86, 126, 62), (144, 180, 92), (60, 134, 78), (132, 196, 96), (104, 76, 58)
THATCH, TIMBER, GLOW, WATER, FOAM, BLACK = (196, 160, 92), (118, 82, 58), (255, 206, 120), (120, 204, 208), (236, 248, 246), (40, 38, 46)
RICE, RICE_L, MUD, SKIN = (104, 170, 62), (170, 216, 96), (128, 110, 84), (176, 120, 84)

# ------------------------------------------------------------------ CALM mode (Logs/payo/pieces/CALM_STYLE.md)
# py -3 tools/payo_pieces/stone_build.py <piece> --calm   writes Logs/payo/pieces/calm/stone/ (never build/stone)
# Same parts and UV layout (painting draws on its own seeded randoms, never the piece's), new paint: one warm stone
# colour with one soft gradient per face, a few large stones with faint joints, no lichen, drips, chips or moss
# fringes, a few big soft muted moss shapes on top edges, carving flat and bold with ONE motif to a face of the band,
# the bevel a subtle lighter edge. A height map is painted beside the colour and baked to a normal map.
CALM = "--calm" in sys.argv
HEIGHT = {}                                          # id(painted image) -> its height image (L, 128 = 0 m)
HU = 0.002                                           # metres per height level
NSTRENGTH = 3.0
CMOSS = (126, 144, 92)
TINT = {}                                            # calm: a swatch key -> the colour it is painted (a block's own trim tint)
if "--out" in sys.argv:
    OUT = Path(sys.argv[sys.argv.index("--out") + 1])
elif CALM:
    OUT = ROOT / "Logs/payo/pieces/calm/stone"


def _soft(c, sat=0.80, warm=0.06):
    lum = 0.30 * c[0] + 0.55 * c[1] + 0.15 * c[2]
    c = [lum + (v - lum) * sat for v in c]
    return tuple(int(round(v + (w - v) * warm)) for v, w in zip(c, (245, 230, 204)))


if CALM:
    OCHRE, RED, CORAL, ROSE, MINT, CREAM = _soft(OCHRE), _soft(RED, 0.85), _soft(CORAL), _soft(ROSE), _soft(MINT), _soft(CREAM, 1.0, 0.03)
    PALE = (226, 216, 196)


def calm_tone(c):
    """Stone values drawn close together and warmed: soft warm grey to pale sand."""
    m = (152, 148, 142)
    c = [mv + (v - mv) * 0.45 for v, mv in zip(c, m)]
    return tuple(int(min(255, v * 0.80 + w)) for v, w in zip(c, (40, 36, 28)))


def hset(img, mask, metres):
    H = HEIGHT.get(id(img))
    if H is not None:
        H.paste(int(max(0, min(255, 128 + metres / HU))), (0, 0) + img.size, mask)


def light(img, top=1.05, bottom=0.90, cool=True):
    """The one soft gradient over the whole face, in place."""
    w, h = img.size
    a = np.asarray(img, np.float32) * np.linspace(top, bottom, h, dtype=np.float32)[:, None, None]
    if cool:
        t = np.linspace(0, 1, h, dtype=np.float32)[:, None, None]
        a = a * (1 - 0.5 * t) + a * np.array([0.95, 0.97, 1.03], np.float32) * 0.5 * t
    img.paste(Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)))


def shade(c, f):
    return tuple(max(0, min(255, int(v * f))) for v in c)


class Piece:
    """One object: its parts and its atlas."""

    def __init__(self, name, size=2048, ppm=48, seed=1):
        self.name, self.S, self.ppm = name, size, ppm
        self.img = Image.new("RGB", (size, size), (150, 148, 144))
        self.x, self.y, self.row = 6, 6, 0
        self.parts, self.sw, self.meta = [], {}, {}
        self.rnd = random.Random(seed)
        self.himg = Image.new("L", (size, size), 128) if CALM else None
        TINT.clear()                                      # a piece's trim tint is its own (set after this by its builder)
        self.rects = []

    # ---- the atlas
    def alloc(self, w, h):
        w, h = int(max(10, w)), int(max(10, h))
        if self.x + w + 6 > self.S:
            self.x, self.y, self.row = 6, self.y + self.row + 8, 0
        assert self.y + h + 6 <= self.S, "%s: the atlas is full (lower ppm or raise size)" % self.name
        r = (self.x, self.y, self.x + w, self.y + h)
        self.x += w + 8
        self.row = max(self.row, h)
        return r

    def uvrect(self, r, inset=1.5):
        return ((r[0] + inset) / self.S, (r[1] + inset) / self.S, (r[2] - inset) / self.S, (r[3] - inset) / self.S)

    def paint(self, r, fn, info=None):
        w, h = r[2] - r[0], r[3] - r[1]
        sub = Image.new("RGB", (w, h), (150, 148, 144))
        if CALM:
            hb = Image.new("L", (w, h), 128)
            HEIGHT[id(sub)] = hb
        out = fn(sub, info) if info is not None else fn(sub)
        if CALM:
            HEIGHT.pop(id(sub), None)
            if out is not None and out is not sub:
                hb = HEIGHT.pop(id(out), hb)
        sub = out or sub
        self.img.paste(sub.resize((w + 6, h + 6)), (r[0] - 3, r[1] - 3))          # bleed past the edge
        self.img.paste(sub, (r[0], r[1]))
        if CALM:
            self.himg.paste(hb, (r[0], r[1]))
            self.rects.append((r[0], r[1], r[2], r[3], (info or {}).get("ppm", self.ppm)))

    def swatch(self, colour, key=None):
        """A small painted patch shared by small parts: lighter at its top, darker at its foot, a couple of soft marks."""
        key = key or colour
        if key not in self.sw:
            r = self.alloc(72, 72)
            rnd = random.Random(zlib.crc32(str(key).encode()) & 0xffff)   # stable across runs (hash() is salted per process)

            col = TINT.get(key, colour) if CALM else colour

            def fn(img, colour=col):
                d = ImageDraw.Draw(img)
                for y in range(72):
                    t = y / 71.0
                    if CALM:
                        d.line([(0, y), (72, y)], fill=shade(colour, 1.06 - 0.20 * t if t > 0.14 else 1.10))
                    else:
                        d.line([(0, y), (72, y)], fill=shade(colour, 1.1 - 0.36 * t if t > 0.14 else 1.18))
                if CALM:
                    return img.filter(ImageFilter.GaussianBlur(1.5))
                for _ in range(3):
                    x, y, rr = rnd.uniform(8, 64), rnd.uniform(16, 60), rnd.uniform(6, 14)
                    d.ellipse([x - rr, y - rr * 0.6, x + rr, y + rr * 0.6], fill=shade(colour, rnd.choice((0.9, 1.1))))
                return img.filter(ImageFilter.GaussianBlur(1.5))
            self.paint(r, fn)
            self.sw[key] = r
        return self.sw[key]

    # ---- parts
    def add(self, mesh, uv, cut=False):
        """cut: False a part to union, True a part to take out, "join" a thin loose thing (a leaf) laid in as its own shell."""
        self.parts.append((mesh, np.asarray(uv, np.float32), cut))
        return mesh

    def write(self, out=None):
        out = out or OUT
        m = hk.Model(self.name)
        m.folder = Path(out) / "parts"
        m.texture = self.name + "-atlas.png"
        for i, (mesh, uv, cut) in enumerate(self.parts):
            m.add("%03d_%s" % (i, "join" if cut == "join" else ("cut" if cut else "add")), [(mesh, uv)])
        m.folder.mkdir(parents=True, exist_ok=True)
        self.img.save(m.folder / (self.name + "-atlas.png"))
        path = m.write()
        self.meta["parts"] = len(self.parts)
        (m.folder / (self.name + ".json")).write_text(json.dumps(self.meta, indent=1))
        if CALM:                                          # the atlas, its height map and the normal map beside the glb
            import calm_glb
            self.img.save(Path(out) / (self.name + "-atlas.png"))
            H = (np.asarray(self.himg, np.float32) - 128) * HU
            calm_glb.save_height(H, Path(out) / (self.name + "_height.png"))
            Image.fromarray(calm_glb.height_to_normal(H, self.rects, strength=NSTRENGTH, blur=1.4)).save(Path(out) / (self.name + "_normal.png"))
        return path


# ------------------------------------------------------------------ UVs
def wrap_uv(pos, cols, rect, axis=1, flip=True):
    """UVs for a mesh made of rings of `cols` + 1 points (rounded, ellipsoid, lathe, tube): round the piece by the true
    distance along each ring (so a box's flat sides are not squeezed), bottom to top by position along `axis`.
    `flip` lays it so that what is left on the piece's front is left in the picture."""
    count = len(pos)
    rows = count // (cols + 1)
    u0, v0, u1, v1 = rect
    uv = np.zeros((count, 2), np.float32)
    lo, hi = float(pos[:, axis].min()), float(pos[:, axis].max())
    others = [a for a in range(3) if a != axis]
    for r in range(rows):
        ring = pos[r * (cols + 1):(r + 1) * (cols + 1)]
        seg = np.linalg.norm(np.diff(ring[:, others], axis=0), axis=1)
        total = float(seg.sum())
        cum = np.concatenate([[0.0], np.cumsum(seg)]) / total if total > 1e-6 else np.linspace(0, 1, cols + 1)
        for c in range(cols + 1):
            t = (float(ring[c, axis]) - lo) / max(1e-6, hi - lo)
            u = 1.0 - cum[c] if flip else cum[c]
            uv[r * (cols + 1) + c] = (u0 + (u1 - u0) * u, v1 - (v1 - v0) * t)
    return uv


def planar_uv(pos, rect, ax=(0, 2), flip=(False, False)):
    u0, v0, u1, v1 = rect
    a, b = pos[:, ax[0]], pos[:, ax[1]]
    ta = (a - a.min()) / max(1e-6, float(a.max() - a.min()))
    tb = (b - b.min()) / max(1e-6, float(b.max() - b.min()))
    if flip[0]:
        ta = 1 - ta
    if flip[1]:
        tb = 1 - tb
    return np.stack([u0 + (u1 - u0) * ta, v0 + (v1 - v0) * tb], 1).astype(np.float32)


def shaped(mesh, hy, taper=(1.0, 1.0), lean=(0.0, 0.0), bulge=0.0):
    """Taper a part built about the origin (factor at its foot, at its top), lean its top over, swell its middle."""
    p, n, t = mesh
    p = p.copy()
    k = np.clip((p[:, 1] + hy) / (2 * hy), 0, 1)
    f = taper[0] + (taper[1] - taper[0]) * k + bulge * np.sin(np.pi * k)
    p[:, 0] = p[:, 0] * f + lean[0] * k
    p[:, 2] = p[:, 2] * f + lean[1] * k
    return p.astype(np.float32), n, t


# ------------------------------------------------------------------ the parts
def box(P, half, r, at=(0, 0, 0), paint=None, sw=None, turn=(0, 0, 0), taper=(1.0, 1.0), lean=(0.0, 0.0), bulge=0.0, seg=12, rings=8, ppm=None, cut=False):
    """A chunky rounded block. With `paint` it gets its own strip of the atlas, unrolled round its four sides, and
    paint(img, info) draws on it; info has ppm, W, D, H, r and `faces` (front, right, back, left, and the four corners:
    their pixel spans). With `sw` (a colour) it wears a shared swatch."""
    hx, hy, hz = half
    r = min(r, hx, hy, hz)
    mesh = hk.rounded(half, r, seg=seg, rings=rings)
    pos = mesh[0]
    if sw is not None:
        rect = P.swatch(sw)
    else:
        ppm = ppm or P.ppm
        eq = pos[(rings // 2) * (seg + 1):(rings // 2 + 1) * (seg + 1)]
        d = np.linalg.norm(np.diff(eq[:, [0, 2]], axis=0), axis=1)
        per = float(d.sum())
        rect = P.alloc(per * ppm, 2 * hy * ppm)
        w = rect[2] - rect[0]
        cum = np.concatenate([[0.0], np.cumsum(d)]) / per
        faces, corners = {}, []
        for s in range(seg):
            a, b = eq[s], eq[s + 1]
            x0, x1 = (1 - cum[s + 1]) * w, (1 - cum[s]) * w
            tol = r * (1 - math.cos(math.pi / seg)) + 2e-3
            if abs(a[2] - b[2]) < 1e-4 and abs(a[2]) > hz - tol and abs(a[0] - b[0]) > 2 * r * math.sin(math.pi / seg) + 1e-3:
                faces["front" if a[2] > 0 else "back"] = (x0, x1)
            elif abs(a[0] - b[0]) < 1e-4 and abs(a[0]) > hx - tol and abs(a[2] - b[2]) > 2 * r * math.sin(math.pi / seg) + 1e-3:
                faces["right" if a[0] > 0 else "left"] = (x0, x1)
        order = sorted(faces.items(), key=lambda kv: kv[1][0])
        for i, (nm, (x0, x1)) in enumerate(order):
            nx = order[(i + 1) % len(order)][1][0] if i + 1 < len(order) else w
            corners.append((x1, nx))
        info = {"ppm": ppm, "W": 2 * hx, "D": 2 * hz, "H": 2 * hy, "r": r, "faces": faces, "corners": corners, "w": w, "h": rect[3] - rect[1]}
        if paint:
            P.paint(rect, paint, info)
    uv = wrap_uv(pos, seg, P.uvrect(rect))
    mesh = shaped(mesh, hy, taper, lean, bulge)
    return P.add(hk.moved(mesh, at, turn), uv, cut)


def lid(P, hx, hz, y_top, thick, paint, ppm=None):
    """A flat top, exactly 2 hx by 2 hz, its upper face at y_top: sharp edged, painted from straight above."""
    ppm = ppm or P.ppm
    rect = P.alloc(2 * hx * ppm + 16, 2 * hz * ppm + 16)
    info = {"ppm": ppm, "W": 2 * hx, "D": 2 * hz, "w": rect[2] - rect[0], "h": rect[3] - rect[1], "margin": 8}
    P.paint(rect, paint, info)
    u0, v0, u1, v1 = P.uvrect(rect)
    mu, mv = 8.0 / P.S, 8.0 / P.S
    y0 = y_top - thick
    corners = [(-hx, -hz), (hx, -hz), (hx, hz), (-hx, hz)]

    def uvt(x, z, out=False):
        u = u0 + mu + (u1 - u0 - 2 * mu) * (x + hx) / (2 * hx)
        v = v0 + mv + (v1 - v0 - 2 * mv) * (z + hz) / (2 * hz)
        if out:
            u += mu * 0.8 * (1 if x > 0 else -1)
            v += mv * 0.8 * (1 if z > 0 else -1)
        return (u, v)

    pos, nrm, uv, tris = [], [], [], []

    def face(pts, n, uvs):
        b = len(pos)
        pos.extend(pts); nrm.extend([n] * 4); uv.extend(uvs)
        tris.extend([(b, b + 1, b + 2), (b, b + 2, b + 3)])

    face([(x, y_top, z) for (x, z) in reversed(corners)], (0, 1, 0), [uvt(x, z) for (x, z) in reversed(corners)])
    face([(x, y0, z) for (x, z) in corners], (0, -1, 0), [uvt(x, z, True) for (x, z) in corners])
    for i in range(4):
        (ax, az), (bx, bz) = corners[i], corners[(i + 1) % 4]
        n = (az - bz, 0, bx - ax)
        ln = math.hypot(n[0], n[2])
        face([(ax, y0, az), (bx, y0, bz), (bx, y_top, bz), (ax, y_top, az)][::-1], (-n[0] / ln, 0, -n[2] / ln),
             [uvt(ax, az, True), uvt(bx, bz, True), uvt(bx, bz), uvt(ax, az)][::-1])
    mesh = (np.array(pos, np.float32), np.array(nrm, np.float32), np.array(tris, np.uint32))
    return P.add(mesh, np.array(uv, np.float32))


def ball(P, half, at, sw, turn=(0, 0, 0), seg=12, rings=8, paint=None, ppm=None):
    mesh = hk.ellipsoid(half, seg=seg, rings=rings)
    if paint:
        ppm = ppm or P.ppm
        rect = P.alloc(math.pi * (half[0] + half[2]) * ppm, 2 * half[1] * ppm * 1.2)
        P.paint(rect, paint, {"ppm": ppm, "w": rect[2] - rect[0], "h": rect[3] - rect[1]})
    else:
        rect = P.swatch(sw)
    uv = wrap_uv(mesh[0], seg, P.uvrect(rect))
    return P.add(hk.moved(mesh, at, turn), uv)


def turned(P, profile, at, sw=None, seg=14, turn=(0, 0, 0), squash=1.0, paint=None, ppm=None):
    """A lathe: profile of (radius, y) bottom to top."""
    mesh = hk.lathe(profile, seg=seg, squash_z=squash)
    if paint:
        ppm = ppm or P.ppm
        rmax = max(p[0] for p in profile)
        ln = sum(math.hypot(b[0] - a[0], b[1] - a[1]) for a, b in zip(profile, profile[1:]))
        rect = P.alloc(2 * math.pi * rmax * ppm, ln * ppm)
        P.paint(rect, paint, {"ppm": ppm, "w": rect[2] - rect[0], "h": rect[3] - rect[1]})
        uv = hk.grid_uv(mesh, P.uvrect(rect), seg, along="rows")
    else:
        uv = hk.grid_uv(mesh, P.uvrect(P.swatch(sw)), seg, along="axis")
    return P.add(hk.moved(mesh, at, turn), uv)


def pipe(P, points, radius, sw, seg=8):
    """A root, a vine, a rope, a rail."""
    mesh = hk.tube(points, radius, seg=seg)
    uv = hk.grid_uv(mesh, P.uvrect(P.swatch(sw)), seg, along="rows")
    return P.add(mesh, uv)


def disc(P, radius, thick, at, paint, rim=0.3, seg=28, ppm=None, turn=(90, 0, 0)):
    """A great round carved disc standing up, facing +z: painted from straight in front."""
    ppm = ppm or P.ppm
    prof = [(radius - rim * 0.5, -thick / 2), (radius, -thick / 2 + rim * 0.4), (radius, thick / 2 - rim * 0.4), (radius - rim * 0.5, thick / 2)]
    mesh = hk.lathe(prof, seg=seg)
    rect = P.alloc(2 * radius * ppm, 2 * radius * ppm)
    P.paint(rect, paint, {"ppm": ppm, "w": rect[2] - rect[0], "h": rect[3] - rect[1], "R": radius})
    uv = planar_uv(mesh[0], P.uvrect(rect), ax=(0, 2), flip=(False, False))
    return P.add(hk.moved(mesh, at, turn), uv)


def strip(P, rows, sw=None, paint=None, ppm=None, two_sided=True):
    """A sheet through rows of points (each row left to right, rows top to bottom): cloth, a fall of water, a leaf.
    Given thickness so it is a closed skin."""
    rows = [np.array(r, np.float32) for r in rows]
    nr, nc = len(rows), len(rows[0])
    P0 = np.stack(rows)                                    # nr, nc, 3
    du = np.gradient(P0, axis=1); dv = np.gradient(P0, axis=0)
    nrm = np.cross(dv, du)
    nrm /= np.maximum(np.linalg.norm(nrm, axis=2, keepdims=True), 1e-6)
    th = 0.03
    front, back = P0 + nrm * th, P0 - nrm * th
    if paint:
        ppm = ppm or P.ppm
        wid = float(np.linalg.norm(P0[0, -1] - P0[0, 0])); ln = float(sum(np.linalg.norm(P0[i + 1, nc // 2] - P0[i, nc // 2]) for i in range(nr - 1)))
        rect = P.alloc(max(wid, 0.3) * ppm, ln * ppm)
        P.paint(rect, paint, {"ppm": ppm, "w": rect[2] - rect[0], "h": rect[3] - rect[1]})
    else:
        rect = P.swatch(sw)
    u0, v0, u1, v1 = P.uvrect(rect)
    pos, nn, uv, tris = [], [], [], []
    for (grid, sgn) in ((front, 1), (back, -1)):
        b = len(pos)
        for i in range(nr):
            for j in range(nc):
                pos.append(grid[i, j]); nn.append(nrm[i, j] * sgn)
                uv.append((u0 + (u1 - u0) * j / (nc - 1), v0 + (v1 - v0) * i / (nr - 1)))
        for i in range(nr - 1):
            for j in range(nc - 1):
                a, c, d, e = b + i * nc + j, b + i * nc + j + 1, b + (i + 1) * nc + j + 1, b + (i + 1) * nc + j
                tris += [(a, e, d), (a, d, c)] if sgn > 0 else [(a, d, e), (a, c, d)]
    nf = nr * nc                                           # close the rim
    def rim(i0, j0, i1, j1):
        a, c = i0 * nc + j0, i1 * nc + j1
        tris.extend([(a, c, nf + c), (a, nf + c, nf + a)])
    for j in range(nc - 1):
        rim(0, j + 1, 0, j); rim(nr - 1, j, nr - 1, j + 1)
    for i in range(nr - 1):
        rim(i, 0, i + 1, 0); rim(i + 1, nc - 1, i, nc - 1)
    mesh = (np.array(pos, np.float32), np.array(nn, np.float32), np.array(tris, np.uint32))
    return P.add(mesh, np.array(uv, np.float32), cut="join")


# ------------------------------------------------------------------ painting
def stones(img, ppm, rnd, course=0.55, length=(0.8, 1.5), palette=STONES, joint=INK, tone=1.0):
    """Fitted blocks, drawn one at a time: its own grey, a lighter worn patch, a darker foot, a pale chipped edge."""
    if CALM:
        return calm_stones(img, ppm, rnd, course, length, palette, tone)
    w, h = img.size
    ImageDraw.Draw(img).rectangle([0, 0, w, h], fill=joint)
    y = 0.0
    row = 0
    while y < h:
        ch = course * ppm * rnd.uniform(0.85, 1.15)
        x = -rnd.uniform(0.1, 0.9) * length[0] * ppm
        while x < w:
            cw = rnd.uniform(*length) * ppm
            g = max(1.5, ppm * 0.035)
            j = lambda: rnd.uniform(-0.05, 0.05) * ppm
            poly = [(x + g + j(), y + g + j()), (x + cw / 2 + j(), y + g + j()), (x + cw - g + j(), y + g + j()), (x + cw - g + j(), y + ch - g + j()), (x + cw / 2 + j(), y + ch - g + j()), (x + g + j(), y + ch - g + j())]
            base = shade(rnd.choice(palette), rnd.uniform(0.92, 1.08) * tone)
            PP.stone_shape(img, poly, base, rnd, size=(w, h), chip=True)
            x += cw
        y += ch
        row += 1
    return img


def soft(img, mask, colour, alpha=1.0):
    m = mask if alpha == 1.0 else mask.point(lambda v: int(v * alpha))
    img.paste(Image.new("RGB", img.size, colour), (0, 0), m)


def bevels(img, info, top=True, bottom=False, alpha=0.8):
    """The wide pale painted bevel: down every corner of a block, along its top edge."""
    if CALM:
        return calm_bevels(img, info, top, bottom, alpha)
    w, h = img.size
    m = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(m)
    for (x0, x1) in info.get("corners", []):
        d.rectangle([x0 - 2, 0, x1 + 2, h], fill=255)
    rp = info["r"] * info["ppm"]
    if top:
        d.rectangle([0, 0, w, max(3, rp * 0.9)], fill=255)
    if bottom:
        d.rectangle([0, h - max(2, rp * 0.6), w, h], fill=150)
    soft(img, m.filter(ImageFilter.GaussianBlur(max(1.2, rp * 0.12))), PALE, alpha)
    line = Image.new("L", (w, h), 0)                       # the cut line under the top bevel
    ImageDraw.Draw(line).rectangle([0, rp * 0.95, w, rp * 0.95 + max(1.5, info["ppm"] * 0.03)], fill=255)
    if top:
        soft(img, line.filter(ImageFilter.GaussianBlur(0.8)), INK, 0.55)


def weather(img, ppm, rnd, moss=0.6, drips=0.5, damp=0.35, lichen=0.5):
    """What time has done: damp toward the foot, moss hanging from the top edge, drip stains, lichen."""
    if CALM:
        return calm_weather(img, ppm, rnd, moss)
    w, h = img.size
    if damp:
        g = Image.linear_gradient("L").resize((w, h))      # black at the top
        soft(img, g.point(lambda v: int(v * damp)), (62, 62, 66))
    n = int(w / ppm * moss * 1.2)
    for _ in range(n):
        x = rnd.uniform(0, w)
        m = Image.new("L", (w, h), 0)
        d = ImageDraw.Draw(m)
        for k in range(rnd.randint(2, 5)):
            ln = rnd.uniform(0.25, 1.1) * ppm
            ww = rnd.uniform(0.12, 0.3) * ppm
            xx = x + rnd.uniform(-0.4, 0.4) * ppm
            d.ellipse([xx - ww, -ww, xx + ww, ln * 0.6], fill=255)
            d.ellipse([xx - ww * 0.7, ln * 0.3, xx + ww * 0.7, ln], fill=255)
        mm = m.filter(ImageFilter.GaussianBlur(max(1.0, ppm * 0.02)))
        soft(img, mm, rnd.choice((MOSS, shade(MOSS, 0.86), shade(MOSS, 1.15))))
        soft(img, ImageChops.multiply(ImageChops.subtract(mm, ImageChops.offset(mm, 2, 3)), mm), MOSS_L, 0.9)
    for _ in range(int(w / ppm * drips * 0.7)):
        x, ln, ww = rnd.uniform(0, w), rnd.uniform(0.25, 0.8) * h, rnd.uniform(0.08, 0.22) * ppm
        y0 = rnd.uniform(0, 0.2) * h
        m = Image.new("L", (w, h), 0)
        ImageDraw.Draw(m).polygon([(x - ww, y0), (x + ww, y0), (x + ww * 0.4, y0 + ln * 0.6), (x, y0 + ln), (x - ww * 0.4, y0 + ln * 0.6)], fill=255)
        soft(img, m.filter(ImageFilter.GaussianBlur(max(1.5, ppm * 0.04))), (52, 52, 56), 0.5)
    for _ in range(int(w * h / (ppm * ppm) * lichen * 0.35)):
        x, y, r = rnd.uniform(0, w), rnd.uniform(0.1, 1.0) * h, rnd.uniform(0.08, 0.2) * ppm
        m = Image.new("L", (w, h), 0)
        d = ImageDraw.Draw(m)
        for k in range(5):
            a = math.tau * k / 5 + rnd.uniform(-0.3, 0.3)
            rr = r * rnd.uniform(0.45, 0.7)
            d.ellipse([x + r * 0.5 * math.cos(a) - rr, y + r * 0.5 * math.sin(a) - rr, x + r * 0.5 * math.cos(a) + rr, y + r * 0.5 * math.sin(a) + rr], fill=255)
        soft(img, m.filter(ImageFilter.GaussianBlur(1.0)), rnd.choice(((200, 212, 184), (184, 204, 176))), 0.8)


def relief(img, mask, raised=None, paint=None, rnd=None, depth=3, worn=0.45):
    """A carved shape that reads as standing proud: an ink shadow down and right, a pale edge up and left, and old
    paint on it, worn off in patches."""
    raised = raised or (198, 196, 188)
    if CALM:                                              # one flat bold colour; the relief lives in the height map
        m = mask.filter(ImageFilter.GaussianBlur(1.0)).point(lambda v: 255 if v > 110 else 0).filter(ImageFilter.GaussianBlur(0.8))
        soft(img, m, mix3(paint, raised, 0.12) if paint else raised)
        hset(img, m.filter(ImageFilter.GaussianBlur(max(1.0, depth * 0.8))), 0.02)
        return
    m = mask.filter(ImageFilter.GaussianBlur(1.0)).point(lambda v: 255 if v > 110 else 0).filter(ImageFilter.GaussianBlur(0.8))
    soft(img, ImageChops.offset(m, depth, int(depth * 1.3)), INK)
    soft(img, m, raised)
    soft(img, ImageChops.multiply(ImageChops.subtract(m, ImageChops.offset(m, max(1, depth - 1), depth)), m), PALE)
    soft(img, ImageChops.multiply(ImageChops.subtract(m, ImageChops.offset(m, -depth + 1, -depth)), m), shade(raised, 0.74))
    if paint and rnd:
        w, h = img.size
        holes = Image.new("L", (w, h), 0)
        d = ImageDraw.Draw(holes)
        for _ in range(int(w * h / 900 * worn) + 3):
            x, y, r = rnd.uniform(0, w), rnd.uniform(0, h), rnd.uniform(5, 16)
            d.ellipse([x - r, y - r * 0.7, x + r, y + r * 0.7], fill=255)
        keep = ImageChops.multiply(m, ImageChops.invert(holes.filter(ImageFilter.GaussianBlur(2)).point(lambda v: 255 if v > 100 else 0)))
        soft(img, keep, paint, 0.9)


def mix3(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def calm_stones(img, ppm, rnd, course, length, palette, tone):
    w, h = img.size
    m = tuple(sum(c[i] for c in palette) / len(palette) for i in range(3))
    base = shade(calm_tone(m), tone ** 0.5)
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, w, h], fill=base)
    jm = Image.new("L", (w, h), 0)
    jd = ImageDraw.Draw(jm)
    ch = course * 1.7 * ppm
    rows = max(1, round(h / ch))
    ch = h / rows
    for r in range(rows):
        x = -rnd.uniform(0.2, 0.8) * length[0] * 1.6 * ppm
        while x < w:
            cw = rnd.uniform(*length) * 1.6 * ppm
            d.rectangle([x, r * ch, x + cw, (r + 1) * ch], fill=shade(base, rnd.uniform(0.975, 1.025)))
            jd.line([(x, r * ch), (x, (r + 1) * ch)], fill=255, width=max(2, int(ppm * 0.05)))
            x += cw
        jd.line([(0, r * ch), (w, r * ch)], fill=255, width=max(2, int(ppm * 0.05)))
    jb = jm.filter(ImageFilter.GaussianBlur(max(1.0, ppm * 0.02)))
    soft(img, jb, shade(base, 0.90), 0.8)
    hset(img, jb, -0.02)
    return img


def calm_weather(img, ppm, rnd, moss=0.6):
    light(img, 1.05, 0.90)
    if moss < 0.45:
        return
    w, h = img.size
    n = int(round(w / ppm * moss * 0.18 * rnd.uniform(0.7, 1.3)))
    m = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(m)
    for _ in range(n):
        x = rnd.uniform(0, w)
        span, ln = rnd.uniform(0.6, 1.3) * ppm, rnd.uniform(0.18, 0.42) * ppm
        d.ellipse([x - span, -ln, x + span, ln], fill=255)
    mb = m.filter(ImageFilter.GaussianBlur(max(1.5, ppm * 0.05)))
    soft(img, mb, CMOSS, 0.92)
    hset(img, mb, 0.015)


def calm_bevels(img, info, top=True, bottom=False, alpha=0.8):
    w, h = img.size
    m = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(m)
    for (x0, x1) in info.get("corners", []):
        d.rectangle([x0 - 2, 0, x1 + 2, h], fill=255)
    rp = info["r"] * info["ppm"]
    if top:
        d.rectangle([0, 0, w, max(3, rp * 0.9)], fill=255)
    mb = m.filter(ImageFilter.GaussianBlur(max(1.5, rp * 0.3)))
    soft(img, mb, PALE, alpha * 0.35)                     # a subtle lighter edge, not a white stripe
    H = HEIGHT.get(id(img))
    if H is not None:                                     # round corners: the height falls away toward each corner's middle
        a = np.zeros((h, w), np.float32)
        for (x0, x1) in info.get("corners", []):
            xs = np.arange(w, dtype=np.float32)
            c, half = (x0 + x1) / 2, max(1.0, (x1 - x0) / 2 + 2)
            a = np.maximum(a, np.clip(1 - np.abs(xs - c) / half, 0, 1)[None, :] ** 0.7 * np.ones((h, 1), np.float32))
        hv = np.asarray(H, np.float32) - 0.012 / HU * a
        H.paste(Image.fromarray(np.clip(hv, 0, 255).astype(np.uint8)))


def tile_from(name, img, ppm, metres, tint=(1.0, 1.0, 1.0)):
    """Lay one of the map's own drawings (thatch, timber) across a region at true scale: `metres` is the drawing's width."""
    src = Image.open(TEX / (name + "_albedo.png")).convert("RGB")
    s = max(8, int(metres * ppm))
    src = src.resize((s, int(s * src.size[1] / src.size[0])), Image.LANCZOS)
    if tint != (1.0, 1.0, 1.0):
        r, g, b = src.split()
        src = Image.merge("RGB", (r.point(lambda v: int(min(255, v * tint[0]))), g.point(lambda v: int(min(255, v * tint[1]))), b.point(lambda v: int(min(255, v * tint[2])))))
    for x in range(0, img.size[0], src.size[0]):
        for y in range(0, img.size[1], src.size[1]):
            img.paste(src, (x, y))
    return img
