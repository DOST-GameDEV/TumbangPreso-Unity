"""PAYO, THE GUARDIANS: the painter. System Python with numpy and Pillow.

  py -3 tools/payo_pieces/bulul_paint.py <piece name> [stone|wood ...]

Reads the six maps tools/payo_pieces/bulul_kit.py baked from the welded skin (where each texel is, which way it
faces, how buried it is at two sizes, how near an edge it is, which material it is) and paints the piece's OWN atlas:
chisel planes in several greys, a wide pale bevel on every real convex edge, dark grooves in every real fold, cracks,
wet streaks down the walls, moss creeping from the tops and the folds, lichen, and the worn paint and inlay.

COLOUR (owner, 2026-10-12: "the texture all reads as too green and teal. the reference i gave only had cool blue
LIGHTING. the stones were the old grey"): STONE IS A NEUTRAL TO SLIGHTLY WARM GREY. No teal or green in any stone
texel. Green is only moss, lichen and leaves. The cool tone is the game's light.

Writes Logs/payo/pieces/build/bulul/<name>-atlas.png (stone) and <name>-atlas-wood.png (dark hardwood).
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
WORK = ROOT / "Logs/payo/pieces/bulul/work"
OUT = ROOT / "Logs/payo/pieces/build/bulul"
STONE, CLOTH, MOSS, INLAY, ACCENT, RICE, JAR, BASKET, LEAF, WOOD, DARK, WATER = range(12)


def C(*rgb):
    return np.array(rgb, np.float32) / 255.0


PAL = {
    "stone": dict(tones=[C(152, 149, 142), C(136, 134, 129), C(166, 162, 153), C(124, 123, 120), C(176, 171, 160), C(144, 141, 133), C(158, 152, 140)],
                  pale=C(224, 219, 205), shade=C(86, 84, 86), ink=C(44, 42, 46), wet=C(98, 96, 97)),
    "wood": dict(tones=[C(104, 66, 42), C(92, 57, 37), C(116, 75, 47), C(82, 51, 34), C(124, 82, 52), C(98, 62, 40), C(110, 70, 46)],
                 pale=C(186, 142, 96), shade=C(52, 32, 24), ink=C(30, 18, 14), wet=C(64, 40, 28)),
}
MOSS_C = [C(62, 98, 42), C(96, 138, 52), C(140, 176, 70), C(186, 208, 104)]
LEAF_C = [C(44, 104, 52), C(74, 150, 62), C(134, 196, 84)]
OCHRE, RED, TURQ, CREAM, BLACK = C(214, 160, 60), C(186, 62, 44), C(56, 196, 184), C(238, 230, 208), C(34, 30, 34)
CL_RED, CL_WHITE = C(180, 44, 40), C(238, 230, 210)
LICHEN = [C(206, 198, 150), C(190, 196, 168)]


# ---------------------------------------------------------------- noise
def _h(ix, iy, iz, seed):
    n = (ix.astype(np.int64) * 374761393 + iy.astype(np.int64) * 668265263 + iz.astype(np.int64) * 2147483647 + seed * 1013904223) & 0xFFFFFFFF
    n = ((n ^ (n >> 13)) * 1274126177) & 0xFFFFFFFF
    n = n ^ (n >> 16)
    return (n & 0xFFFFFF).astype(np.float32) / float(0xFFFFFF)


def vnoise(p, s, seed=0):
    q = p / np.asarray(s, np.float32)
    i = np.floor(q); f = q - i; f = f * f * (3 - 2 * f)
    ix, iy, iz = (i[:, k].astype(np.int64) for k in range(3))
    out = 0
    for dx in (0, 1):
        for dy in (0, 1):
            for dz in (0, 1):
                w = (f[:, 0] if dx else 1 - f[:, 0]) * (f[:, 1] if dy else 1 - f[:, 1]) * (f[:, 2] if dz else 1 - f[:, 2])
                out = out + w * _h(ix + dx, iy + dy, iz + dz, seed)
    return out


def fbm(p, s, seed=0, octaves=3):
    out, amp, tot = 0, 1.0, 0
    for o in range(octaves):
        out = out + amp * vnoise(p, np.asarray(s, np.float32) / (2 ** o), seed + o * 17); tot += amp; amp *= 0.5
    return out / tot


def voronoi(p, s, seed=0):
    """F1, F2, a random number per cell, and the cell's feature point."""
    q = p / np.asarray(s, np.float32)
    i = np.floor(q).astype(np.int64)
    f1 = np.full(len(p), 9.0, np.float32); f2 = f1.copy(); cid = np.zeros(len(p), np.float32)
    for dx in (-1, 0, 1):
        for dy in (-1, 0, 1):
            for dz in (-1, 0, 1):
                cx, cy, cz = i[:, 0] + dx, i[:, 1] + dy, i[:, 2] + dz
                fx = cx + _h(cx, cy, cz, seed + 1); fy = cy + _h(cx, cy, cz, seed + 2); fz = cz + _h(cx, cy, cz, seed + 3)
                d = np.sqrt((q[:, 0] - fx) ** 2 + (q[:, 1] - fy) ** 2 + (q[:, 2] - fz) ** 2)
                near = d < f1
                f2 = np.where(near, f1, np.minimum(f2, d))
                cid = np.where(near, _h(cx, cy, cz, seed + 4), cid)
                f1 = np.where(near, d, f1)
    return f1, f2, cid


def ss(a, b, x):
    t = np.clip((x - a) / (b - a + 1e-9), 0, 1)
    return t * t * (3 - 2 * t)


def mix(a, b, t):
    return a + (b - a) * t[:, None]


# ---------------------------------------------------------------- the painting
def paint(name, variant="stone"):
    marks = json.load(open(WORK / (name + ".json")))
    z = np.load(WORK / (name + "_maps.npz"))
    lo, hi = z["lo"], z["hi"]
    idm = z["id"]; size = idm.shape[0]
    valid = idm > 0.5 / 32
    raw = (np.rint(idm[valid] * 32) - 1).astype(np.int32)
    chamfer = raw >= 16                                                         # the real bevel faces (kit: pid + 16)
    pid = raw % 16
    pb = lo + z["pos"][valid] * (hi - lo)
    nb = z["nrm"][valid] * 2 - 1
    P = np.stack([pb[:, 0], pb[:, 2], -pb[:, 1]], 1).astype(np.float32)        # authoring: x, y up, z front
    Nn = np.stack([nb[:, 0], nb[:, 2], -nb[:, 1]], 1).astype(np.float32)
    Nn /= np.linalg.norm(Nn, axis=1, keepdims=True) + 1e-6
    ao, ao2, edge = np.clip(z["ao"][valid], 0, 1), np.clip(z["ao2"][valid], 0, 1), np.clip(z["edge"][valid], 0, 1)
    X, Y, Zf = P[:, 0], P[:, 1], P[:, 2]
    S = float(marks.get("paint_scale", max(marks["size_xyz"])))                 # the piece's size: every feature scales with it
    H = marks["height"]
    pal = PAL[variant]
    N = len(P)
    col = np.zeros((N, 3), np.float32)
    # the head's own space: texels near a nodded head, un-nodded, so its band, eyes and studs are painted level
    Ph = P.copy()
    for hd in marks.get("heads", []):
        pv, a = hd["pivot"], np.radians(hd["tilt"])
        near = np.linalg.norm(P - np.array(hd["mid"], np.float32), axis=1) < hd["r"]
        y, zz = P[near, 1] - pv[1], P[near, 2] - pv[2]
        Ph[near, 1] = pv[1] + y * np.cos(a) + zz * np.sin(a)
        Ph[near, 2] = pv[2] + zz * np.cos(a) - y * np.sin(a)

    # ---- the body material (stone or wood)
    f1, f2, cid = voronoi(P, S * 0.11, 3)
    tones = np.stack(pal["tones"])
    base = tones[np.minimum((cid * len(tones)).astype(int), len(tones) - 1)]
    big = fbm(P, S * 0.30, 11)
    base = np.mean(tones, 0)[None, :] * 0.45 + base * 0.55
    base = base * (0.92 + 0.18 * big)[:, None]
    if variant == "wood":
        warp = fbm(P, (S * 0.06, S * 0.5, S * 0.06), 31)
        grain = np.sin((X * 1.3 + Zf * 0.9 + warp * S * 0.35) * (2 * np.pi / (S * 0.016)))
        base = base * (1.0 + 0.085 * np.sign(grain) * ss(0.2, 0.7, np.abs(grain)))[:, None]
        ringk = np.sin((warp * 9 + Y / (S * 0.12)) * 2 * np.pi)
        base = base * (1.0 - 0.10 * ss(0.86, 0.98, ringk))[:, None]
    else:
        # chisel planes: a faint dark seam between neighbouring planes and short tool strokes inside each
        base = base * (1.0 - 0.08 * (1 - ss(0.0, 0.04, f2 - f1)))[:, None]
        ang = cid * 6.283
        d = X * np.cos(ang) + Y * np.sin(ang) * 0.8 + Zf * np.sin(ang * 1.7)
        stroke = np.sin(d * (2 * np.pi / (S * 0.011)))
        smask = ss(0.52, 0.66, vnoise(P, S * 0.035, 5))
        base = base * (1.0 + 0.04 * ss(0.72, 0.95, stroke) * smask - 0.05 * ss(0.72, 0.95, -stroke) * smask)[:, None]
    # painted light: tops paler, undersides and buried places darker, in three flat steps
    v = 0.42 * (0.5 + 0.5 * Nn[:, 1]) + 0.58 * ao2 + 0.10 * (0.5 + 0.5 * Nn[:, 2]) - 0.05
    lit = ss(0.60, 0.66, v); shd = 1 - ss(0.36, 0.43, v)
    body = base * (1.0 + 0.11 * lit - 0.0)[:, None]
    body = mix(body, body * 0.60 + pal["shade"] * 0.22, shd * 0.85)
    # wet streaks down the walls
    wall = 1 - ss(0.35, 0.6, np.abs(Nn[:, 1]))
    streak = vnoise(P, (S * 0.02, S * 0.42, S * 0.02), 7) * 0.7 + vnoise(P, (S * 0.045, S * 0.6, S * 0.045), 8) * 0.3
    wet = ss(0.60, 0.68, streak) * wall * ss(0.15, 0.45, fbm(P, S * 0.2, 9))
    body = mix(body, body * 0.72, wet * 0.75)
    # cracks: a few long dark lines with a pale lip
    c1, c2, _ = voronoi(P + (fbm(P, S * 0.05, 21)[:, None] - 0.5) * S * 0.035, S * 0.24, 13)
    cmask = ss(0.56, 0.64, vnoise(P, S * 0.33, 15))
    crack = (1 - ss(0.010, 0.022, c2 - c1)) * cmask
    lip = (1 - ss(0.022, 0.05, c2 - c1)) * cmask * (1 - crack)
    body = mix(body, pal["pale"] * 0.92, lip * 0.5)
    body = mix(body, pal["ink"], crack * 0.9)
    # dark grooves in the folds, then the wide pale bevel on the convex edges
    fold = (1 - ss(0.30, 0.62, ao)) * 0.85
    body = mix(body, pal["ink"] * 0.6 + body * 0.3, fold)
    chip = ss(0.55, 0.75, vnoise(P, S * 0.018, 23)) * 0.55
    bev = np.maximum(ss(0.05, 0.12, edge) * 0.55, chamfer * 1.0) * ss(0.45, 0.8, ao) * ss(0.2, 0.5, ao2)
    body = mix(body, pal["pale"] * (0.93 + 0.10 * big)[:, None], np.clip(bev * (0.95 - chip), 0, 1))
    hot = ss(0.22, 0.34, edge) * ss(0.75, 0.9, ao) * ss(0.2, 0.6, Nn[:, 1] + 0.5)
    body = mix(body, np.minimum(pal["pale"] * 1.08, 1.0), hot * 0.8)

    # ---- carving that is painted on: the fret course, the sun disc, toe and finger grooves
    def groove(mask_dark, mask_lip):
        nonlocal body
        body = mix(body, pal["pale"], np.clip(mask_lip, 0, 1) * 0.75)
        body = mix(body, pal["ink"], np.clip(mask_dark, 0, 1) * 0.92)

    def worn(colour, mask, seed, keep=0.62):
        nonlocal body
        w = ss(1 - keep - 0.06, 1 - keep + 0.06, fbm(P, S * 0.03, seed)) * mask
        body = mix(body, colour[None, :] * (0.86 + 0.2 * big)[:, None], np.clip(w, 0, 1) * 0.9)

    for fr in marks.get("fret", []):
        y0, y1, hw, hd = fr["y0"], fr["y1"], fr["hw"], fr["hd"]
        hgt = y1 - y0
        on = (Y > y0 - 0.02) & (Y < y1 + 0.02) & (np.abs(Nn[:, 1]) < 0.5) & (np.maximum(np.abs(X) - hw, np.abs(Zf) - hd) > -0.12 * hgt - 0.05)
        t = np.where(np.abs(Nn[:, 0]) > np.abs(Nn[:, 2]), Zf, X)
        yy = (Y - y0) / hgt
        per = hgt * 1.5
        u = (t / per) % 1.0
        tri = 0.5 + 0.26 * (np.abs(u * 2 - 1) * 2 - 1)                                 # the zigzag
        steps = np.round(tri * 6) / 6                                                   # stepped, as cut stone
        w = 0.055
        zig = (np.abs(yy - steps) < w) | ((np.abs(((u * 12) % 1.0) - 0.5) > 0.5 - w * 0.9) & (np.abs(yy - tri) < 0.09))
        rules = (np.abs(yy - 0.1) < 0.035) | (np.abs(yy - 0.9) < 0.035)
        worn(OCHRE, on & (yy > steps) & (yy < 0.9), 41, 0.66)
        worn(RED, on & (yy < steps) & (yy > 0.1), 43, 0.6)
        dia = (np.abs(u - 0.5) * per / hgt * 2.2 + np.abs(yy - 0.78) * 4.2) < 0.34
        dia2 = (np.minimum(u, 1 - u) * per / hgt * 2.2 + np.abs(yy - 0.22) * 4.2) < 0.34
        body[on & (dia | dia2)] = TURQ * 0.95
        groove(on & (zig | rules), on & ((np.abs(yy - steps + 0.07) < w * 0.8) | (np.abs(yy - 0.04) < 0.03)))
    for dc in marks.get("discs", []):
        c, R = dc["c"], dc["r"]
        if dc.get("axis", "z") == "z":
            rr = np.hypot(X - c[0], Y - c[1]); depth = np.abs(Zf - c[2]); th = np.arctan2(Y - c[1], X - c[0])
        else:
            rr = np.hypot(X - c[0], Zf - c[2]); depth = np.abs(Y - c[1]); th = np.arctan2(Zf - c[2], X - c[0])
        on = (rr < R * 1.02) & (depth < R * 0.55)
        q = rr / R
        worn(OCHRE, on & (q > 0.36) & (q < 0.70), 51, 0.7)
        worn(RED, on & (q > 0.74) & (q < 0.98), 53, 0.5)
        rays = (np.abs(((th / (2 * np.pi) * 12) % 1.0) - 0.5) < 0.1) & (q > 0.40) & (q < 0.68)
        tri2 = (np.abs(((th / (2 * np.pi) * 12 + 0.5) % 1.0) - 0.5) * 2 < (q - 0.74) / 0.24) & (q > 0.74) & (q < 0.98)
        body[on & (q < 0.26)] = TURQ
        groove(on & (rays | (np.abs(q - 0.30) < 0.03)), on & tri2 & False)
        body = np.where((on & tri2)[:, None], body * 0.62, body)
    for g in marks.get("grooves", []):
        c, h, ax, n = np.array(g["c"]), np.array(g["h"]), "xyz".index(g["axis"]), g["n"]
        inside = np.all(np.abs(P - c) < h * 1.08, axis=1)
        tt = (P[:, ax] - (c[ax] - h[ax])) / (2 * h[ax]) * n
        line = (np.abs(tt - np.round(tt)) < 0.07 * g.get("w", 1.0)) & (tt > 0.5) & (tt < n - 0.5)
        groove(inside & line, inside & (np.abs(tt - np.round(tt) - 0.13) < 0.05) & (tt > 0.5) & (tt < n - 0.5))
    cs = marks.get("courses")
    if cs and variant == "stone":
        # the giant is built of several stones: level joints, and upright joints staggered course by course
        hC = cs["h"]
        wy = (Y + (vnoise(P, S * 0.25, 61) - 0.5) * hC * 0.22) / hC
        row = np.floor(wy); fy = wy - row
        t = np.where(np.abs(Nn[:, 0]) > np.abs(Nn[:, 2]), Zf, X)
        ft = (t / (hC * cs.get("len", 2.2)) + _h(row, row * 0 + 3, row * 0 + 5, 7)) % 1.0
        onw = (wall > 0.5) & (Y < cs["ymax"]) & (Y > cs.get("ymin", -1))
        wj = 0.022
        groove(onw & ((fy < wj) | (ft < wj * 0.45)), (onw & ((np.abs(fy - wj * 2.2) < wj) | (np.abs(ft - wj * 1.0) < wj * 0.45))) * 0.8)
    for pb in marks.get("paintboxes", []):                                            # worn paint inside a box
        on = np.all(np.abs(P - np.array(pb["c"], np.float32)) < np.array(pb["half"], np.float32), axis=1)
        if pb.get("up"):
            on &= Nn[:, 1] > 0.5
        worn({"ochre": OCHRE, "red": RED, "turq": TURQ, "cream": CREAM, "black": BLACK}[pb["col"]], on, 45 + len(pb["col"]), pb.get("keep", 0.6))
    for b in marks.get("paintbands", []):                                            # worn paint rings round a limb or post
        on = (Y > b["y0"]) & (Y < b["y1"]) & (np.hypot(X - b.get("x", 0), Zf - b.get("z", 0)) < b.get("r", 99))
        worn({"ochre": OCHRE, "red": RED, "turq": TURQ, "cream": CREAM, "black": BLACK}[b["c"]], on, 61 + int(b["y0"] * 10), b.get("keep", 0.62))

    # ---- lichen and moss on the body
    l1, _, lc = voronoi(P, S * 0.035, 29)
    lich = (marks.get("lichen", 1.0) > 0) & (l1 + 0.35 * (vnoise(P, S * 0.012, 31) - 0.5) < 0.22 + 0.16 * lc) & (lc > 0.86) & (vnoise(P, S * 0.25, 30) > 0.55)
    body = np.where(lich[:, None], np.stack(LICHEN)[(lc * 97).astype(int) % 2] * (0.9 + 0.1 * big)[:, None], body)
    top = np.clip(Y / H, 0, 1)
    m = 0.62 * Nn[:, 1] + 2.3 * (fbm(P, S * 0.09, 33) - 0.5) + 0.9 * (1 - ao2) + 0.25 * top - 1.0
    drip = ss(0.66, 0.76, vnoise(P, (S * 0.025, S * 0.16, S * 0.025), 35)) * ss(0.45, 0.85, 1 - ao2) * wall
    mossy = np.clip(ss(0.0, 0.07, m) + drip * 0.9, 0, 1) * marks.get("moss", 1.0)
    mcol = moss_colour(P, Nn, ao2, S)
    stone_final = mix(body, mcol, mossy)
    rim = ss(-0.07, 0.0, m) * (1 - ss(0.0, 0.07, m))
    stone_final = mix(stone_final, MOSS_C[0][None, :] * np.ones((N, 1), np.float32), rim * 0.5 * marks.get("moss", 1.0))

    is_body = (pid == STONE) | (pid == WOOD)
    col[is_body] = stone_final[is_body]
    dk = pid == DARK
    col[dk] = (body[dk] * 0.42 + pal["ink"] * 0.35)

    # ---- the other materials
    k = pid == MOSS
    if k.any():
        col[k] = mcol[k]
    k = pid == LEAF
    if k.any():
        t = fbm(P[k], S * 0.02, 71) * 0.5 + 0.35 * (0.5 + 0.5 * Nn[k, 1]) + 0.25 * ao2[k]
        c = np.where((t > 0.55)[:, None], LEAF_C[1], LEAF_C[0])
        c = np.where((t > 0.70)[:, None], LEAF_C[2], c)
        col[k] = mix(c, LEAF_C[2] * 1.08, ss(0.12, 0.3, edge[k]) * 0.6)
    k = pid == CLOTH
    if k.any():
        col[k] = cloth(P[k], Ph[k], marks, S)
        col[k] *= (0.78 + 0.22 * ss(0.3, 0.7, ao[k]))[:, None]
    k = pid == INLAY
    if k.any():
        c = np.ones((k.sum(), 3), np.float32) * CREAM
        for e in marks.get("eyes", []):
            dx, dy = (Ph[k, 0] - e[0]) / e[3], (Ph[k, 1] - e[1]) / e[3]
            near = np.hypot(dx, dy) < 1.6
            c[near & (np.hypot(dx, dy * 1.6) > 0.90)] = OCHRE * 0.9
            c[near & (np.hypot(dx, dy + 0.10) < 0.46)] = C(70, 44, 30)                          # a warm brown iris, low in the eye
            c[near & (np.hypot(dx, dy + 0.10) < 0.30)] = C(24, 18, 20)                          # the pupil
            c[near & (np.hypot(dx - 0.17, dy - 0.06) < 0.11)] = CREAM                           # one catch light
        col[k] = c * (0.8 + 0.2 * ss(0.3, 0.7, ao[k]))[:, None]
    k = pid == ACCENT
    if k.any():
        th = np.arctan2(P[k, 0], P[k, 2])
        bead = ((th / (2 * np.pi) * marks.get("beads", 22)) % 1.0)
        c = np.where((bead < 0.5)[:, None], OCHRE, RED)
        c = np.where(((bead > 0.82))[:, None], TURQ, c)
        c = np.where((np.abs(bead - 0.5) < 0.05)[:, None] | (bead < 0.05)[:, None], pal["ink"] * 1.3, c)
        if marks.get("accent_plain"):
            c = np.ones_like(c) * OCHRE
        c = c * (0.82 + 0.3 * ss(0.4, 0.75, 0.5 + 0.5 * Nn[k, 1]))[:, None]
        wornm = ss(0.62, 0.72, fbm(P[k], S * 0.02, 81))
        c = mix(c, body[k], wornm * 0.8)
        col[k] = mix(c, np.minimum(c * 1.25 + 0.08, 1), ss(0.1, 0.25, edge[k]) * 0.5) * (0.7 + 0.3 * ss(0.3, 0.7, ao[k]))[:, None]
    k = pid == RICE
    if k.any():
        strand = np.sin((P[k, 0] * 1.0 + P[k, 2] * 0.8 + vnoise(P[k], 0.3, 91) * 0.3) * (2 * np.pi / 0.045))
        c = np.where((strand > 0.3)[:, None], C(232, 200, 110), C(204, 164, 74))
        c = np.where((strand < -0.75)[:, None], C(150, 112, 50), c)
        c = np.where((vnoise(P[k], 0.05, 93) > 0.72)[:, None], C(246, 228, 150), c)
        col[k] = c * (0.72 + 0.28 * ss(0.3, 0.7, ao[k]))[:, None]
    k = pid == JAR
    if k.any():
        c = np.ones((k.sum(), 3), np.float32) * C(112, 62, 44)
        yy = P[k, 1]
        y0 = np.percentile(yy, 1); hh = max(np.percentile(yy, 99) - y0, 1e-3); q = (yy - y0) / hh
        c[(np.abs(q - 0.62) < 0.05)] = OCHRE
        c[(np.abs(q - 0.72) < 0.02) | (np.abs(q - 0.52) < 0.02)] = C(60, 34, 28)
        c = c * (0.85 + 0.25 * ss(0.5, 0.7, 0.5 + 0.5 * (Nn[k, 0] * -0.5 + Nn[k, 1] * 0.6 + Nn[k, 2] * 0.5)))[:, None]
        hl = ss(0.86, 0.9, (Nn[k, 0] * -0.45 + Nn[k, 1] * 0.55 + Nn[k, 2] * 0.65))
        c = mix(c, C(236, 206, 170) * np.ones_like(c), hl * 0.8)
        col[k] = c * (0.7 + 0.3 * ss(0.3, 0.7, ao[k]))[:, None]
    k = pid == BASKET
    if k.any():
        th = np.arctan2(P[k, 0] - np.median(P[k, 0]), P[k, 2] - np.median(P[k, 2]))
        a = ((th / (2 * np.pi) * 26) % 1.0) < 0.5
        b = ((P[k, 1] / 0.07) % 1.0) < 0.5
        c = np.where((a ^ b)[:, None], C(206, 164, 96), C(160, 116, 62))
        c = np.where((((P[k, 1] / 0.07) % 1.0) < 0.12)[:, None], C(104, 72, 40), c)
        col[k] = c * (0.72 + 0.28 * ss(0.3, 0.7, ao[k]))[:, None]
    k = pid == WATER
    if k.any():
        fall = np.sin((P[k, 0] * 9 + vnoise(P[k], (0.1, 0.6, 0.1), 95) * 4) * 2 * np.pi)
        c = np.where((fall > 0.35)[:, None], C(232, 250, 250), C(120, 214, 220))
        col[k] = np.where((fall < -0.6)[:, None], C(70, 176, 196), c)

    # worn paint over the body: the colour, chipped back to the stone or wood on its edges and in patches
    for ppid, pc in ((12, C(52, 50, 56)), (13, C(228, 220, 198)), (14, OCHRE), (15, RED)):
        k = pid == ppid
        if k.any():
            v2 = 0.5 * (0.5 + 0.5 * Nn[k, 1]) + 0.5 * ao2[k]
            c = pc[None, :] * (0.80 + 0.28 * ss(0.45, 0.6, v2))[:, None]
            pw = marks.get("paintwear", 0.0)
            chipm = np.clip(ss(0.62 - pw, 0.70 - pw, fbm(P[k], S * 0.025, 83)) + chamfer[k] * 0.6 + ss(0.2, 0.4, edge[k]) * 0.3, 0, 1)
            c = mix(c, stone_final[k], chipm * 0.85)
            col[k] = c * (0.6 + 0.4 * ss(0.3, 0.7, ao[k]))[:, None]
    # inlay studs and painted eyes: a dark ring, the colour, a paler heart
    for dt in marks.get("dots", []):
        Q = Ph if dt.get("head") else P
        dd = np.linalg.norm(Q - np.array(dt["c"], np.float32), axis=1) / dt["r"]
        colr = {"ochre": OCHRE, "red": RED, "turq": TURQ, "cream": CREAM, "black": BLACK}[dt["col"]]
        col = mix(col, pal["ink"], ((dd < 1.9) & (dd >= 1.25)) * 0.9)
        col = np.where((dd < 1.25)[:, None], colr[None, :] * np.where(dd < 0.6, 1.12, 0.92)[:, None], col)

    img = np.zeros((size, size, 3), np.float32)
    img[:] = np.mean(np.stack(pal["tones"]), 0) * 0.8
    img[valid] = np.clip(col, 0, 1)
    OUT.mkdir(parents=True, exist_ok=True)
    out = OUT / (name + ("-atlas.png" if variant == marks.get("primary", "stone") else "-atlas-%s.png" % variant))
    Image.fromarray((img * 255 + 0.5).astype(np.uint8)).save(out)
    stone_px = col[(pid == STONE) & (mossy < 0.05)]
    print("PAINTED", out.name, "mean body colour", (stone_px.mean(0) * 255).round().astype(int).tolist() if len(stone_px) else None)


def moss_colour(P, Nn, ao2, S):
    t = fbm(P, S * 0.028, 37) * 0.6 + 0.28 * (0.5 + 0.5 * Nn[:, 1]) + 0.22 * ao2
    c = np.where((t > 0.50)[:, None], MOSS_C[1], MOSS_C[0])
    c = np.where((t > 0.66)[:, None], MOSS_C[2], c)
    sp, _, sc = voronoi(P, S * 0.012, 39)
    return np.where(((sp < 0.24) & (sc > 0.6) & (t > 0.45))[:, None], MOSS_C[3], c)


def cloth(P, Ph, marks, S):
    """The woven cloth: red ground, black borders with white stitches, a chain of black and white diamonds. Each
    texel belongs to the ring (a band round something) or strip (a hanging tail or flap) whose mark holds it."""
    n = len(P)
    cross = np.zeros(n, np.float32); along = P[:, 1].copy(); ph = np.full(n, S * 0.02, np.float32)
    fringe = np.zeros(n, bool); claimed = np.zeros(n, bool)
    for m in marks.get("cloths", []):
        Q = Ph if m.get("head") else P
        c = np.array(m["c"], np.float32)
        if m["type"] == "ring":
            k = (np.abs(Q[:, 1] - c[1]) < m["h"] * 1.3) & (np.hypot(Q[:, 0] - c[0], Q[:, 2] - c[2]) < m["r"] * 1.7) & ~claimed
            cross[k] = (Q[k, 1] - c[1]) / m["h"]
            along[k] = np.arctan2(Q[k, 0] - c[0], Q[k, 2] - c[2]) * m["r"]
            ph[k] = m["h"]
        else:
            h = np.array(m["half"], np.float32)
            k = np.all(np.abs(Q - c) < h * 1.2 + 0.01, axis=1) & ~claimed
            wi, ai = "xyz".index(m["wide"]), "xyz".index(m["along"])
            cross[k] = (Q[k, wi] - c[wi]) / h[wi]
            along[k] = Q[k, ai]
            ph[k] = h[wi]
            if m.get("fringe"):
                fringe |= k & (Q[:, ai] < c[ai] - h[ai] + h[wi] * 0.5)
        claimed |= k
    per = ph * 2.2
    u = (along / per) % 1.0
    a = np.abs(cross)
    c = np.ones((n, 3), np.float32) * CL_RED
    dia = np.abs(u - 0.5) * 2 * 0.9 + a / 0.55
    c[dia < 0.82] = BLACK
    c[dia < 0.5] = CL_WHITE
    c[dia < 0.2] = CL_RED
    c[(a > 0.58) & (a < 0.70)] = CL_WHITE
    c[a >= 0.70] = BLACK
    c[(a >= 0.78) & (a < 0.92) & (((along / (per * 0.25)) % 1.0) < 0.45)] = CL_WHITE
    weave = np.sin(along * (2 * np.pi / (ph * 0.12)))
    c = c * (1.0 - 0.07 * (weave > 0.3))[:, None]
    fr = fringe & (((cross * 4.0) % 1.0) < 0.5)
    c[fringe] = BLACK
    c[fr] = CL_WHITE
    stripe = ((P[:, 1] / (S * 0.006)) % 1.0) < 0.4                                     # knots and ties: plain stripes
    c[~claimed] = np.where(stripe[~claimed][:, None], BLACK, CL_RED)
    return c


if __name__ == "__main__":
    # --out <dir> writes the atlas there instead of build/bulul (a throwaway check of the approved paint);
    # --calm hands over to tools/payo_pieces/bulul_calm.py (the CALM pass, Logs/payo/pieces/CALM_STYLE.md)
    argv = sys.argv[1:]
    if "--out" in argv:
        i = argv.index("--out")
        OUT = Path(argv[i + 1])
        del argv[i:i + 2]
    if "--calm" in argv:
        import bulul_calm
        argv.remove("--calm")
        bulul_calm.main(argv, OUT if "--out" in sys.argv else None)
        sys.exit(0)
    nm = argv[0]
    for var in (argv[1:] or [json.load(open(WORK / (nm + ".json"))).get("primary", "stone")]):
        paint(nm, var)
