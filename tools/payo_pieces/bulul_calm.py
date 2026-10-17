"""PAYO, THE GUARDIANS: the CALM painter (Logs/payo/pieces/CALM_STYLE.md). System Python with numpy and Pillow.

  py -3 tools/payo_pieces/bulul_paint.py <piece name> --calm [stone|wood ...] [--out <dir>]

Re-paints the guardian's OWN atlas from the same six baked maps tools/payo_pieces/bulul_kit.py wrote (where each
texel is, which way it faces, how buried it is, how near an edge, which material), so the UV layout and the model are
the approved ones and only the paint changes. The approved painter (bulul_paint.paint) is untouched.

What the calm paint is:
- ONE body colour (stone in the shared calm tone about 165, 156, 140; the hardwood a calm warm brown) and one soft
  gradient over the WHOLE figure: lighter at the top, a little darker and cooler at the foot, a soft dark in the deep
  folds, a subtle lighter edge (not a white stripe).
- Gone: chisel planes, tool strokes, cracks, wet streaks, lichen, chips, the per-stone course joints, the busy
  stepped fret course, moss speckle and drips.
- Kept as big bold shapes: the face (the painted eyes, the studs), the woven red, black and white band (bold
  diamonds, no stitches), the discs (a turquoise heart, an ochre ring, a red ring with eight bold ochre rays), the
  fret course as one ochre band with one bold red stripe, flat paint panels with no wear.
- Moss: a few big soft muted patches on upward faces only.
- A height map (metres, every relief in proportion to the figure's size) painted from the same maps: soft rounded
  edges, the band and the stripe, the disc rings, toe and finger grooves, studs, moss as soft raised shapes, a broad
  low swell on the stone, broad grain on the wood. Baked here to a tangent-space normal map (OpenGL, green up) with
  the gradient taken texel to texel only where the neighbouring texel is the SAME surface (its baked position is next
  to this one), so the many small UV islands of the atlas never bleed into each other.

Writes <out>/<name>-atlas.png (the primary material), <name>-atlas-<other>.png, <name>_height.png, <name>_normal.png.
"""
import json
from pathlib import Path

import numpy as np
from PIL import Image

import calm_glb
from bulul_paint import (WORK, STONE, CLOTH, MOSS, INLAY, ACCENT, RICE, JAR, BASKET, LEAF, WOOD, DARK, WATER,
                         C, vnoise, fbm, ss, mix, _h)

ROOT = Path(__file__).resolve().parents[2]
CALM_OUT = ROOT / "Logs/payo/pieces/calm/bulul"
NSTRENGTH = 3.0


def _soft(c, sat=0.80, warm=0.06):
    """As stone_kit's calm accents: a little less saturated, a little warmer."""
    lum = 0.30 * c[0] + 0.55 * c[1] + 0.15 * c[2]
    c = [lum + (v - lum) * sat for v in c]
    return C(*[v + (w - v) * warm for v, w in zip(c, (245, 230, 204))])


BODY = {"stone": dict(base=C(165, 156, 140), pale=C(224, 214, 194)),
        "wood": dict(base=C(128, 90, 62), pale=C(178, 136, 96))}
OCHRE, RED, CREAM = _soft((214, 160, 60)), _soft((186, 62, 44), 0.85), _soft((238, 230, 208), 1.0, 0.03)
TURQ = C(56, 196, 184)                                      # turquoise unchanged, as the pilot
BLACK = C(52, 46, 50)
CL_RED, CL_WHITE, CL_BLACK = _soft((180, 44, 40), 0.88), _soft((238, 230, 210), 1.0, 0.02), C(46, 40, 44)
MOSS_D, MOSS_M, MOSS_L = C(104, 124, 78), C(126, 144, 92), C(148, 164, 106)
LEAF_D, LEAF_L = C(70, 120, 68), C(126, 168, 90)
PAINT = {"ochre": OCHRE, "red": RED, "turq": TURQ, "cream": CREAM, "black": BLACK}


# ---------------------------------------------------------------- texel-space helpers
def masked_blur(a, valid, sigma):
    """A gaussian blur of a map counting only valid texels (normalised), so the empty atlas does not darken it."""
    w = valid.astype(np.float32)
    num = calm_glb.gblur((a * w).astype(np.float32), sigma)
    den = calm_glb.gblur(w, sigma)
    return np.where(den > 1e-4, num / np.maximum(den, 1e-4), a).astype(np.float32)


def texel_normals(Hm, Pm, valid, strength=NSTRENGTH):
    """Tangent-space normals from a height image (metres) and the baked position image (metres). Each axis of the
    image is differenced only to a neighbour that is the same surface (both valid and less than 3.5 texel-steps apart
    in space); texels with no such neighbour take a neighbour's normal (the bake margin), else stay flat."""
    step = []
    g = []
    ok = []
    for axis in (1, 0):                                     # 1: along a row (image right, +u); 0: down the image
        Hf, Hb = np.roll(Hm, -1, axis), np.roll(Hm, 1, axis)
        Pf, Pb = np.roll(Pm, -1, axis), np.roll(Pm, 1, axis)
        vf, vb = np.roll(valid, -1, axis) & valid, np.roll(valid, 1, axis) & valid
        df = np.linalg.norm(Pf - Pm, axis=-1)
        db = np.linalg.norm(Pm - Pb, axis=-1)
        step.append(np.median(np.concatenate([df[vf & (df > 1e-7)], db[vb & (db > 1e-7)]])))
        lim = step[-1] * 3.5
        of = vf & (df > step[-1] * 0.2) & (df < lim)
        ob = vb & (db > step[-1] * 0.2) & (db < lim)
        both = of & ob
        gr = np.zeros_like(Hm)
        gr = np.where(of & ~ob, (Hf - Hm) / np.maximum(df, 1e-9), gr)
        gr = np.where(ob & ~of, (Hm - Hb) / np.maximum(db, 1e-9), gr)
        gr = np.where(both, (Hf - Hb) / np.maximum(df + db, 1e-9), gr)
        g.append(gr)
        ok.append(of | ob)
    gx, gy = g                                              # gy: metres per metre DOWN the image
    n = np.stack([-gx * strength, gy * strength, np.ones_like(Hm)], -1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    got = ok[0] & ok[1]
    # the bake margin and lone texels: borrow the nearest got normal (a few rings), so filtering never meets flat
    for _ in range(6):
        if got.all():
            break
        acc = np.zeros_like(n); cnt = np.zeros(Hm.shape, np.float32)
        for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0)):
            sg = np.roll(np.roll(got, dy, 0), dx, 1)
            acc += np.roll(np.roll(n, dy, 0), dx, 1) * sg[..., None]
            cnt += sg
        take = ~got & (cnt > 0) & valid
        n[take] = acc[take] / cnt[take][:, None]
        n[take] /= np.linalg.norm(n[take], axis=-1, keepdims=True)
        got = got | take
    n[~valid] = (0, 0, 1)
    print("NORMALS texel step %.4f m (u) %.4f m (v); %.1f%% of texels with a true gradient" % (step[0], step[1], 100 * (ok[0] & ok[1])[valid].mean()))
    return (np.clip(n * 0.5 + 0.5, 0, 1) * 255 + 0.5).astype(np.uint8)


# ---------------------------------------------------------------- the calm painting
def paint_calm(name, variant, out, height=True):
    marks = json.load(open(WORK / (name + ".json")))
    z = np.load(WORK / (name + "_maps.npz"))
    lo, hi = z["lo"], z["hi"]
    idm = z["id"]; size = idm.shape[0]
    valid = idm > 0.5 / 32
    raw = (np.rint(idm[valid] * 32) - 1).astype(np.int32)
    chamfer = raw >= 16
    pid = raw % 16
    Pimg = lo + z["pos"] * (hi - lo)                         # metres, Blender axes (for the normal bake)
    pb = Pimg[valid]
    nb = z["nrm"][valid] * 2 - 1
    P = np.stack([pb[:, 0], pb[:, 2], -pb[:, 1]], 1).astype(np.float32)
    Nn = np.stack([nb[:, 0], nb[:, 2], -nb[:, 1]], 1).astype(np.float32)
    Nn /= np.linalg.norm(Nn, axis=1, keepdims=True) + 1e-6
    ao, ao2 = np.clip(z["ao"], 0, 1), np.clip(z["ao2"], 0, 1)
    ao, ao2 = masked_blur(ao, valid, 1.6)[valid], masked_blur(ao2, valid, 2.0)[valid]
    edge = np.clip(masked_blur(np.clip(z["edge"], 0, 1), valid, 1.8), 0, 1)[valid]
    X, Y, Zf = P[:, 0], P[:, 1], P[:, 2]
    S = float(marks.get("paint_scale", max(marks["size_xyz"])))
    Ht = marks["height"]
    N = len(P)
    pal = BODY[variant]
    Hh = np.zeros(N, np.float32)                             # the height, metres
    Ph = P.copy()
    for hd in marks.get("heads", []):
        pv, a = hd["pivot"], np.radians(hd["tilt"])
        near = np.linalg.norm(P - np.array(hd["mid"], np.float32), axis=1) < hd["r"]
        y, zz = P[near, 1] - pv[1], P[near, 2] - pv[2]
        Ph[near, 1] = pv[1] + y * np.cos(a) + zz * np.sin(a)
        Ph[near, 2] = pv[2] + zz * np.cos(a) - y * np.sin(a)

    # ---- the light painted in: one soft gradient over the whole figure, soft folds, a hint of facing
    top = np.clip(Y / Ht, 0, 1)
    L = (0.96 + 0.10 * top) * (0.95 + 0.05 * ao2) * (0.94 + 0.06 * ss(0.25, 0.75, ao)) + 0.03 * Nn[:, 1]
    cool = (1 - top)[:, None] * np.array([-0.025, -0.008, 0.02], np.float32)

    def lit(col):
        return np.clip(col * L[:, None] * (1 + cool), 0, 1)

    # ---- the body: one colour, a very gentle large shift so it is not dead flat
    big = fbm(P, S * 0.45, 11)
    body = np.ones((N, 3), np.float32) * pal["base"] * (0.975 + 0.05 * big)[:, None]
    if variant == "wood":
        Hh += 0.0006 * S * (fbm(P, (S * 0.05, S * 0.45, S * 0.05), 31) - 0.5)     # broad grain, in the relief only
    else:
        Hh += 0.0016 * S * (fbm(P, S * 0.20, 17) - 0.5)                            # a broad hand-worked swell
    Hh -= 0.0024 * S * ss(0.03, 0.30, edge)                                         # soft rounded edges

    def soft_line(d, w):
        return np.exp(-(d / max(w, 1e-6)) ** 2)

    # ---- the fret course: one ochre band, one bold red stripe, raised a little, the stripe sunk
    for fr in marks.get("fret", []):
        y0, y1, hw, hd = fr["y0"], fr["y1"], fr["hw"], fr["hd"]
        hgt = y1 - y0
        on = (Y > y0 - 0.02) & (Y < y1 + 0.02) & (np.abs(Nn[:, 1]) < 0.5) & (np.maximum(np.abs(X) - hw, np.abs(Zf) - hd) > -0.12 * hgt - 0.05)
        yy = (Y - y0) / hgt
        band = on & (yy > 0.06) & (yy < 0.94)
        stripe = on & (yy > 0.36) & (yy < 0.64)
        body[band] = OCHRE
        body[stripe] = RED
        soft_band = ss(0.0, 0.12, yy) * (1 - ss(0.88, 1.0, yy)) * on
        Hh += 0.0010 * S * soft_band
        Hh -= 0.0008 * S * ss(0.30, 0.40, yy) * (1 - ss(0.60, 0.70, yy)) * on

    # ---- the discs: turquoise heart, ochre ring, red ring with eight bold ochre rays
    for dc in marks.get("discs", []):
        c, R = dc["c"], dc["r"]
        if dc.get("axis", "z") == "z":
            rr = np.hypot(X - c[0], Y - c[1]); depth = np.abs(Zf - c[2]); th = np.arctan2(Y - c[1], X - c[0])
        else:
            rr = np.hypot(X - c[0], Zf - c[2]); depth = np.abs(Y - c[1]); th = np.arctan2(Zf - c[2], X - c[0])
        on = (rr < R * 1.02) & (depth < R * 0.55)
        q = rr / R
        ray = np.abs(((th / (2 * np.pi) * 8) % 1.0) - 0.5) * 2 < (0.98 - q) / 0.24 * 0.7
        body[on & (q > 0.36) & (q < 0.70)] = OCHRE
        body[on & (q > 0.74) & (q < 0.98)] = RED
        body[on & (q > 0.74) & (q < 0.98) & ray] = OCHRE
        body[on & (q < 0.27)] = TURQ
        Hh += on * S * (0.0010 * (1 - ss(0.22, 0.30, q)) - 0.0007 * soft_line(q - 0.32, 0.035) - 0.0007 * soft_line(q - 0.72, 0.03)
                        + 0.0004 * ((q > 0.74) & ray) * ss(0.74, 0.8, q) * (1 - ss(0.92, 0.98, q)))

    # ---- toe and finger grooves: relief, with only a soft hint of dark
    for g in marks.get("grooves", []):
        c, h, ax, n = np.array(g["c"]), np.array(g["h"]), "xyz".index(g["axis"]), g["n"]
        inside = np.all(np.abs(P - c) < h * 1.08, axis=1)
        tt = (P[:, ax] - (c[ax] - h[ax])) / (2 * h[ax]) * n
        line = soft_line(tt - np.round(tt), 0.11 * g.get("w", 1.0)) * ((tt > 0.5) & (tt < n - 0.5)) * inside
        Hh -= 0.010 * S * h[ax] * 2 / max(n, 1) * line
        body = body * (1 - 0.08 * line)[:, None]

    # ---- flat paint, no wear
    for pbx in marks.get("paintboxes", []):
        on = np.all(np.abs(P - np.array(pbx["c"], np.float32)) < np.array(pbx["half"], np.float32), axis=1)
        if pbx.get("up"):
            on &= Nn[:, 1] > 0.5
        # a box the approved left mostly worn back to stone becomes a soft wash of its colour, one flat area
        a = 1.0 if pbx.get("keep", 0.6) >= 0.6 else 0.35 + 0.6 * pbx["keep"]
        body[on] = body[on] * (1 - a) + PAINT[pbx["col"]] * a
    for b in marks.get("paintbands", []):
        on = (Y > b["y0"]) & (Y < b["y1"]) & (np.hypot(X - b.get("x", 0), Zf - b.get("z", 0)) < b.get("r", 99))
        body[on] = PAINT[b["c"]]

    # ---- the lighter edge, the light, then a few big soft moss patches on upward faces
    bev = np.maximum(ss(0.05, 0.22, edge), chamfer * 0.7) * ss(0.45, 0.8, ao)
    body = mix(body, pal["pale"] * np.ones((N, 1), np.float32), bev * 0.26)
    body = lit(body)
    mfac = marks.get("moss", 1.0) * (1.0 if variant == "stone" else 0.6)
    m = 0.9 * Nn[:, 1] + 2.1 * (fbm(P, S * 0.22, 33) - 0.5) + 0.35 * top - 0.92
    mossy = ss(0.0, 0.16, m) * ss(0.40, 0.75, Nn[:, 1]) * mfac
    mcol = lit(mix(MOSS_D * np.ones((N, 1), np.float32), MOSS_L * np.ones((N, 1), np.float32), ss(0.2, 0.9, Nn[:, 1]) * (0.6 + 0.4 * ao2)))
    body = mix(body, mcol, np.clip(mossy, 0, 1) * 0.92)
    Hh += 0.0007 * S * np.clip(mossy, 0, 1)

    col = np.zeros((N, 3), np.float32)
    is_body = (pid == STONE) | (pid == WOOD)
    col[is_body] = body[is_body]
    dk = pid == DARK
    col[dk] = body[dk] * 0.55

    # ---- the other materials
    k = pid == MOSS
    if k.any():
        col[k] = mcol[k]
        Hh[k] += 0.0004 * S * (fbm(P[k], S * 0.06, 37) - 0.5)
    k = pid == LEAF
    if k.any():
        t = 0.5 + 0.5 * Nn[k, 1]
        c = mix(LEAF_D * np.ones((k.sum(), 1), np.float32), LEAF_L * np.ones((k.sum(), 1), np.float32), ss(0.25, 0.95, t) * (0.6 + 0.4 * ao2[k]))
        col[k] = mix(c, LEAF_L * 1.05, ss(0.12, 0.3, edge[k]) * 0.25)
    k = pid == CLOTH
    if k.any():
        cc, ch = cloth_calm(P[k], Ph[k], marks, S)
        col[k] = cc * (0.84 + 0.16 * ss(0.3, 0.7, ao[k]))[:, None] * (0.95 + 0.07 * top[k])[:, None]
        Hh[k] += ch
    k = pid == INLAY
    if k.any():
        c = np.ones((k.sum(), 3), np.float32) * CREAM
        for e in marks.get("eyes", []):
            dx, dy = (Ph[k, 0] - e[0]) / e[3], (Ph[k, 1] - e[1]) / e[3]
            near = np.hypot(dx, dy) < 1.6
            c[near & (np.hypot(dx, dy * 1.6) > 0.90)] = OCHRE * 0.9
            c[near & (np.hypot(dx, dy + 0.10) < 0.46)] = C(70, 44, 30)
            c[near & (np.hypot(dx, dy + 0.10) < 0.30)] = C(24, 18, 20)
            c[near & (np.hypot(dx - 0.17, dy - 0.06) < 0.11)] = CREAM
        col[k] = c * (0.86 + 0.14 * ss(0.3, 0.7, ao[k]))[:, None]
    k = pid == ACCENT
    if k.any():
        th = np.arctan2(P[k, 0], P[k, 2])
        nb_ = max(4, int(marks.get("beads", 22)) // 2)
        bead = (th / (2 * np.pi) * nb_) % 1.0
        c = np.where((bead < 0.5)[:, None], OCHRE, RED)
        if marks.get("accent_plain"):
            c = np.ones_like(c) * OCHRE
        c = c * (0.86 + 0.18 * ss(0.4, 0.75, 0.5 + 0.5 * Nn[k, 1]))[:, None]
        col[k] = mix(c, np.minimum(c * 1.15 + 0.04, 1), ss(0.1, 0.25, edge[k]) * 0.3) * (0.82 + 0.18 * ss(0.3, 0.7, ao[k]))[:, None]
        Hh[k] += 0.0003 * S * np.cos(bead * 2 * np.pi * 2)
    k = pid == RICE
    if k.any():
        bundle = np.sin((P[k, 0] * 1.0 + P[k, 2] * 0.8 + vnoise(P[k], 0.4, 91) * 0.2) * (2 * np.pi / 0.16))
        c = mix(C(196, 164, 92) * np.ones((k.sum(), 1), np.float32), C(222, 194, 120) * np.ones((k.sum(), 1), np.float32), 0.5 + 0.5 * Nn[k, 1])
        col[k] = c * (1 + 0.03 * bundle)[:, None] * (0.80 + 0.20 * ss(0.3, 0.7, ao[k]))[:, None]
        Hh[k] += 0.004 * bundle
    k = pid == JAR
    if k.any():
        c = np.ones((k.sum(), 3), np.float32) * C(118, 72, 52)
        yy = P[k, 1]
        y0 = np.percentile(yy, 1); hh = max(np.percentile(yy, 99) - y0, 1e-3); q = (yy - y0) / hh
        band = (np.abs(q - 0.62) < 0.07)
        c[band] = OCHRE
        c = c * (0.86 + 0.2 * ss(0.5, 0.7, 0.5 + 0.5 * (Nn[k, 0] * -0.5 + Nn[k, 1] * 0.6 + Nn[k, 2] * 0.5)))[:, None]
        hl = ss(0.82, 0.92, (Nn[k, 0] * -0.45 + Nn[k, 1] * 0.55 + Nn[k, 2] * 0.65))
        c = mix(c, C(220, 190, 156) * np.ones_like(c), hl * 0.35)
        col[k] = c * (0.8 + 0.2 * ss(0.3, 0.7, ao[k]))[:, None]
        Hh[k] += 0.004 * hh * ss(0.0, 0.02, 0.07 - np.abs(q - 0.62))
    k = pid == BASKET
    if k.any():
        bands = ((P[k, 1] / 0.14) % 1.0)
        c = np.where((bands < 0.5)[:, None], C(198, 158, 96), C(178, 136, 80))
        col[k] = c * (0.80 + 0.20 * ss(0.3, 0.7, ao[k]))[:, None]
        Hh[k] += 0.004 * np.sin(bands * 2 * np.pi * 2)
    k = pid == WATER
    if k.any():
        fall = np.sin((P[k, 0] * 4 + vnoise(P[k], (0.2, 0.8, 0.2), 95) * 2) * 2 * np.pi)
        col[k] = np.where((fall > 0.45)[:, None], C(214, 242, 240), C(120, 214, 220))

    # flat paint panels (the worn paint pids of the approved): one colour, a soft lighter edge, the light
    for ppid, pc in ((12, C(60, 56, 62)), (13, CREAM), (14, OCHRE), (15, RED)):
        k = pid == ppid
        if k.any():
            c = pc[None, :] * np.ones((k.sum(), 1), np.float32)
            c = mix(c, np.minimum(c * 1.18 + 0.05, 1), ss(0.08, 0.25, edge[k]) * 0.35)
            col[k] = np.clip(c * L[k, None] * (1 + cool[k]), 0, 1)
    # studs and painted eyes: the colour with a soft darker rim, a little domed
    for dt in marks.get("dots", []):
        Q = Ph if dt.get("head") else P
        dd = np.linalg.norm(Q - np.array(dt["c"], np.float32), axis=1) / dt["r"]
        colr = PAINT[dt["col"]]
        rim = (dd < 1.7) & (dd >= 1.2)
        col = np.where(rim[:, None], col * 0.72, col)
        col = np.where((dd < 1.2)[:, None], colr[None, :] * np.where(dd < 0.6, 1.06, 0.96)[:, None], col)
        Hh += dt["r"] * (0.35 * np.clip(1 - (dd / 1.2) ** 2, 0, 1) - 0.12 * soft_line(dd - 1.45, 0.2))

    img = np.zeros((size, size, 3), np.float32)
    img[:] = pal["base"] * 0.85
    img[valid] = np.clip(col, 0, 1)
    out = Path(out)
    out.mkdir(parents=True, exist_ok=True)
    primary = marks.get("primary", "stone")
    path = out / (name + ("-atlas.png" if variant == primary else "-atlas-%s.png" % variant))
    Image.fromarray((img * 255 + 0.5).astype(np.uint8)).save(path)
    stone_px = col[is_body & (mossy < 0.05)]
    print("CALM PAINTED", path.name, "mean body colour", (stone_px.mean(0) * 255).round().astype(int).tolist() if len(stone_px) else None)
    if height:
        Himg = np.zeros((size, size), np.float32)
        Himg[valid] = Hh
        calm_glb.save_height(Himg, out / (name + "_height.png"))
        Image.fromarray(texel_normals(Himg, Pimg.astype(np.float32), valid)).save(out / (name + "_normal.png"))
        print("HEIGHT range %.4f .. %.4f m" % (float(Hh.min()), float(Hh.max())))


def cloth_calm(P, Ph, marks, S):
    """The woven band, bold: a red ground, a chain of big black diamonds with white hearts, a thick white stripe and a
    black border each side; no stitches, no thread lines. Returns (colour, height)."""
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
    c[dia < 0.80] = CL_BLACK
    c[dia < 0.42] = CL_WHITE
    c[(a > 0.58) & (a < 0.72)] = CL_WHITE
    c[a >= 0.72] = CL_BLACK
    hgt = (0.06 * ss(0.66, 0.78, a) + 0.04 * (1 - ss(0.72, 0.84, dia))) * ph * 0.25
    fr = fringe & (((cross * 2.0) % 1.0) < 0.5)
    c[fringe] = CL_BLACK
    c[fr] = CL_WHITE
    stripe = ((P[:, 1] / (S * 0.02)) % 1.0) < 0.4                                   # knots and ties: broad stripes
    c[~claimed] = np.where(stripe[~claimed][:, None], CL_BLACK, CL_RED)
    return c, hgt.astype(np.float32)


def main(argv, out=None):
    name = argv[0]
    marks = json.load(open(WORK / (name + ".json")))
    primary = marks.get("primary", "stone")
    other = "wood" if primary == "stone" else "stone"
    variants = argv[1:] or [primary, other]
    for v in variants:
        paint_calm(name, v, out or CALM_OUT, height=(v == primary))
