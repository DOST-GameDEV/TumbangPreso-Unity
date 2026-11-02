"""Paints BAHA's pictures (Resources/Textures/UltimateFx/baha_body.png, baha_spray.png), by construction: curves and smooth
noise, no source image is read. The owner: "its too noisy. i need it looking exactly like the reference".

usage: build_baha_body_texture.py <.../baha_body.png> [preview.png]

baha_body is the body from above, u across, v tail (0) to front (1). Channels are data:
  R how deep the water is under the wave's own shape (0 trough, 1 the brightest azure). Travels with the wave.
  G the front's rolled mass and the feathered rim. Stays with the front, churning.
  B the smeared swoops and the caustic web. Laid on the road by the shader, so it tiles along v.
  A coverage: a rounded teardrop, its rear dissolving.
baha_spray: RG a 2x2 atlas of soft painted splashes (R coverage, G whiteness); B, over the whole picture, the roll's shading
of the front mass in the body's own uv (1 in shadow), so the crest reads as a form without another texture.
"""
import sys
import cv2
import numpy as np

N = 1024


def smooth(a, b, x):
    t = np.clip((x - a) / np.where(np.abs(np.asarray(b) - a) < 1e-6, 1e-6, np.asarray(b) - a), 0, 1)
    return t * t * (3 - 2 * t)


def noise(seed, cx, cy=None):
    """One octave of large smooth cells, 0 to 1."""
    cy = cy or cx
    g = np.random.RandomState(seed).rand(cy + 3, cx + 3).astype(np.float32)
    im = cv2.resize(g, (N + 2 * N // cx, N + 2 * N // cy), interpolation=cv2.INTER_CUBIC)[N // cy:N // cy + N, N // cx:N // cx + N]
    return (im - im.min()) / (im.max() - im.min())


def curve(points, count=240):
    p = np.array([points[0]] + list(points) + [points[-1]], dtype=np.float64)
    out = []
    for i in range(1, len(p) - 2):
        for t in np.linspace(0, 1, count // (len(p) - 3), endpoint=False):
            a, b, c, d = p[i - 1], p[i], p[i + 1], p[i + 2]
            out.append(0.5 * (2 * b + (c - a) * t + (2 * a - 5 * b + 4 * c - d) * t * t + (3 * b - a - 3 * c + d) * t ** 3))
    out.append(p[-2])
    return np.array(out)


def stroke(points, soft=0.0):
    img = np.zeros((N, N), np.float32)
    for x, v, w in curve(points):
        cv2.circle(img, (int((x * 0.5 + 0.5) * (N - 1)), int(v * (N - 1))), max(1, int(w * N * 0.5)), 1.0, -1, cv2.LINE_AA)
    return cv2.GaussianBlur(img, (0, 0), soft * N) if soft > 0 else img


def paint():
    ys, xs = np.mgrid[0:N, 0:N].astype(np.float32)
    x, v = xs / (N - 1) * 2 - 1, ys / (N - 1)

    # ---- the silhouette: a rounded teardrop. Narrow rear, widest at three quarters, a broad rolled front with slow lobes.
    vv = v + 0.045 * (noise(11, 6, 3) - 0.5) * smooth(0.6, 0.9, v)
    # The shader's `halfAt` is this same sum: the body's dome and its crest follow this outline.
    half = np.where(vv < 0.72, np.clip(vv / 0.72, 0, 1) ** 0.6, np.sqrt(np.clip(1 - ((vv - 0.72) / 0.28) ** 2, 0, 1))) * 0.95
    edge = half + (0.09 * (noise(12, 3, 6) - 0.5) + 0.05 * (noise(14, 2, 11) - 0.5) + 0.06 * (noise(15, 5, 34) - 0.5)) * smooth(0.1, 0.5, v) - np.abs(x - 0.05 * np.sin(v * 3.4 + 0.6))
    edge = cv2.GaussianBlur(edge.astype(np.float32), (0, 0), 3)
    # The rear dissolves: coverage climbs slowly over its first half, eaten by large soft cells.
    eaten = noise(13, 5, 4)
    rear = smooth(0.0, 0.34, v + 0.22 * (eaten - 0.5) * smooth(0.6, 0.2, v))
    cover = smooth(-0.008, 0.012, edge) * smooth(1.0, 0.99, vv) * (0.08 + 0.92 * rear)

    # ---- the water: saturated azure over the body, deep only in the troughs and at the rear.
    depth = 0.28 + 0.54 * smooth(0.14, 0.52, v) + 0.10 * smooth(0.70, 0.92, v)

    # ---- the front: one rolled crest, thick to thin, its inner edge feathered along the flow. No cuts.
    inner = 0.915 + 0.05 * (noise(31, 5, 2) - 0.5) + 0.03 * np.sin(x * 2.3 + 0.5) - 0.07 * np.exp(-((x - 0.45) / 0.35) ** 2) - 0.13 * x ** 2
    feather = noise(35, 30, 3)
    mass = smooth(inner - 0.075, inner + 0.03, vv + 0.10 * (feather - 0.5))
    pocket = stroke([(-0.40, 0.84, 0.09), (-0.22, 0.87, 0.09), (-0.04, 0.86, 0.05), (0.06, 0.83, 0.01)], soft=0.012)
    mass *= 1 - smooth(0.10, 0.60, pocket)
    t = np.clip((vv - inner) / np.maximum(0.02, 1.0 - inner), 0, 1)
    roll = np.clip(0.85 * smooth(0.62, 1.0, t) + 0.30 * smooth(0.30, 0.0, t), 0, 1)
    rim = smooth(0.075, 0.004, edge + 0.05 * (noise(37, 6, 40) - 0.5)) * smooth(0.26, 0.46, v) * smooth(0.18, 0.40, noise(33, 3, 9))
    rim = cv2.GaussianBlur(rim, (0, 0), 1.5)
    front = np.maximum(mass, rim * 0.85)
    front *= smooth(-0.008, 0.006, edge)

    # ---- the swoops: thick to thin, soft on one side, their ends feathered by streaks drawn along the flow.
    swoop = np.zeros((N, N), np.float32)
    for pts, soft in (
        ([(0.86, 0.87, 0.001), (0.66, 0.79, 0.085), (0.44, 0.71, 0.075), (0.22, 0.65, 0.026), (0.02, 0.60, 0.001)], 0.002),
        ([(0.72, 0.62, 0.001), (0.56, 0.55, 0.034), (0.40, 0.50, 0.020), (0.24, 0.47, 0.001)], 0.002),
        ([(-0.82, 0.76, 0.001), (-0.66, 0.68, 0.040), (-0.50, 0.60, 0.022), (-0.36, 0.51, 0.001)], 0.002),
        ([(-0.02, 0.84, 0.001), (-0.10, 0.75, 0.050), (-0.20, 0.67, 0.040), (-0.22, 0.60, 0.014), (-0.18, 0.54, 0.001)], 0.002),
        ([(-0.62, 0.85, 0.001), (-0.46, 0.77, 0.012), (-0.20, 0.755, 0.009), (0.04, 0.78, 0.001)], 0.0015),
        ([(0.34, 0.58, 0.001), (0.20, 0.50, 0.012), (0.04, 0.44, 0.007), (-0.10, 0.40, 0.001)], 0.0015),
        ([(-0.48, 0.46, 0.001), (-0.34, 0.40, 0.010), (-0.16, 0.34, 0.006), (0.02, 0.32, 0.001)], 0.0015),
        ([(0.58, 0.40, 0.001), (0.44, 0.34, 0.009), (0.28, 0.30, 0.005), (0.14, 0.27, 0.001)], 0.0015),
    ):
        swoop = np.maximum(swoop, stroke(pts, soft))
    # ⚠️ NO CLEAN OUTLINE, NO EVEN FILL. The owner: "i dont really like how stickering the white streaks on the water look".
    # Drawn out along the flow, torn across it by long thin cells, and capped well under white. The shader smears them further.
    ys2, xs2 = np.mgrid[0:N, 0:N].astype(np.float32)
    drag = 34 * (noise(38, 5, 3) - 0.5) + 10 * np.sin(ys2 / N * 44 + 6 * noise(39, 4, 4))
    swoop = cv2.remap(swoop.astype(np.float32), xs2 + drag, ys2 + 40 * (noise(49, 6, 3) - 0.5), cv2.INTER_LINEAR)
    swoop = cv2.GaussianBlur(swoop, (0, 0), sigmaX=4.0, sigmaY=20.0)
    swoop = smooth(0.04, 0.42, swoop) * (0.30 + 0.70 * smooth(0.22, 0.65, noise(36, 46, 5)))
    swoop = cv2.GaussianBlur(swoop, (0, 0), sigmaX=1.0, sigmaY=6.0) * 1.0

    # ---- the web: thin bright caustic lines, the contours of a slow field, varying in width, mostly mid body and rear.
    field = noise(41, 7, 6) * 0.72 + noise(42, 13, 11) * 0.28
    wide = 0.0035 + 0.0075 * noise(43, 5, 5)
    web = 1 - smooth(wide * 0.5, wide * 1.6, np.abs(field - 0.50))
    web2 = np.zeros_like(web)
    # A second, unrelated field: contours of one field run parallel and read as a map.
    other = noise(47, 9, 8) * 0.8 + noise(48, 17, 13) * 0.2
    web3 = 1 - smooth(wide * 0.3, wide * 1.1, np.abs(other - 0.52))
    place = smooth(0.12, 0.28, v) * smooth(0.92, 0.72, v) * smooth(0.30, 0.52, noise(44, 7, 7))
    web = np.maximum.reduce([web, web3 * 0.85]) * place
    # Its own soft glow, since nothing blooms.
    web = np.maximum(web, cv2.GaussianBlur(web, (0, 0), 3) * 0.28)

    wx, wy = 0.020 * (noise(45, 6, 6) - 0.5), 0.020 * (noise(46, 6, 6) - 0.5)
    mapx, mapy = xs + wx * N * 2, ys + wy * N * 2
    flow = cv2.remap(np.maximum(swoop, web).astype(np.float32), mapx, mapy, cv2.INTER_LINEAR)
    # It tiles along v: the road is longer than the picture. The two ends are faded through a copy shifted by half.
    fade = smooth(0.0, 0.22, v) * smooth(1.0, 0.78, v)
    flow = flow * fade + np.roll(flow, N // 2, axis=0) * (1 - fade)
    return np.clip(depth, 0, 1), np.clip(front, 0, 1), np.clip(flow, 0, 1), np.clip(cover, 0, 1), np.clip(roll, 0, 1)


def paint_spray(path, roll):
    """2x2 atlas of soft torn clusters: many small soft flecks scattered round a loose centre, broken by holes. No outline."""
    img = np.zeros((N, N), np.float32); r = np.random.RandomState(61); q = N // 2
    for cy in (0, 1):
        for cx in (0, 1):
            for _ in range(70):
                a, d = r.uniform(0, 6.283), r.uniform(0, 1) ** 0.7 * q * 0.42
                p = (int(cx * q + q / 2 + np.cos(a) * d), int(cy * q + q / 2 + np.sin(a) * d * 0.8))
                cv2.circle(img, p, max(1, int(r.uniform(0.008, 0.045) * q * (1.2 - d / q))), float(r.uniform(0.6, 1.0)), -1, cv2.LINE_AA)
    ys, xs = np.mgrid[0:N, 0:N].astype(np.float32)
    img = cv2.remap(img, xs + 26 * (noise(64, 34) - 0.5), ys + 26 * (noise(65, 34) - 0.5), cv2.INTER_LINEAR)
    img *= smooth(0.34, 0.50, noise(63, 40))
    soft = cv2.GaussianBlur(img, (0, 0), 2.6)
    white = smooth(0.40, 0.80, cv2.GaussianBlur(img, (0, 0), 10.0)) * (0.5 + 0.5 * noise(62, 12))
    # BGRA. The roll is in the body's own rows and the atlas is used as it lies, so both take the same flip.
    bgra = np.stack([roll, white, soft, np.ones_like(img)], -1)
    cv2.imwrite(path, (np.flipud(bgra)[::2, ::2] * 255 + 0.5).astype(np.uint8))


RAMP = [(0.00, (12, 30, 54)), (0.35, (14, 66, 128)), (0.68, (10, 128, 218)), (1.00, (70, 200, 248))]


def preview(depth, front, flow, cover, path):
    stops = [s for s, _ in RAMP]
    colour = np.stack([np.interp(depth, stops, [c[i] for _, c in RAMP]) for i in range(3)], -1)
    for m in (flow, front):
        colour = colour * (1 - m[..., None]) + np.array((236, 247, 252)) * m[..., None]
    a = cover[..., None]
    im = colour * a + np.array((24, 30, 42)) * (1 - a)
    cv2.imwrite(path, np.flipud(np.clip(im, 0, 255)).astype(np.uint8)[..., ::-1])


if __name__ == "__main__":
    d, f, l, c, roll = paint()
    bgra = np.stack([l, f, d, c], -1)  # cv2 writes BGRA: B flow, G front, R depth, A coverage
    cv2.imwrite(sys.argv[1], np.flipud((bgra * 255 + 0.5).astype(np.uint8)))
    paint_spray(sys.argv[1].replace('baha_body', 'baha_spray'), roll)
    if len(sys.argv) > 2:
        preview(d, f, l, c, sys.argv[2])
