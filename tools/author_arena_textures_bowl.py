"""Paint the arena's `bowl` kit textures, in the house illustrated style (ARENA-1.4, bowl kit).

  py -3 tools/author_arena_textures_bowl.py [--sheet N]

Writes Assets/TumbangPreso/Art/Arena/Textures/arena_bowl_<surface>.png (and `_emit.png` where the
surface glows) and a swatch sheet Logs/arena/bowl/bowl_swatches_vN.png. The model that wears them
is built by tools/author_arena_bowl.py, which IMPORTS THE CONSTANTS at the top of this file (the
tile sizes and the atlas regions), so the two cannot drift apart. Nothing heavy is imported at
module level for that reason: numpy, PIL and scipy load inside `main`.

THE STYLE (docs/ARENA_ART_BRIEF.md, docs/KANTO_DESIGN_GUIDE.md section 3): FLAT fills, a few LARGE
patches with FEATHERED organic edges, low contrast between a thing and its joints, no grain, no
streaks, no cracks, no noise layers. Seats, panels, windows and LED shapes are PAINTED shapes with
soft, slightly wobbly edges and a few chosen colours, never a procedural grid and never confetti.
Every surface is its own drawing, its own function. Deterministic: fixed seeds, no clock.

  TILING, world UVs:
    arena_bowl_turf_a     the turf, the darker mown tone, 16 m per tile: two broad coats and a few
                          big soft clumps. turf_b is the same grass mown the other way (lighter).
    arena_bowl_track      the dark rubber track round the field, 16 m: broad worn fields and a few
                          long soft scuffs.
    arena_bowl_concrete   the stands' structure, 8 m: navy-grey, faint formwork joints at 2 m, damp
                          fields low in contrast.
    arena_bowl_step       aisles, the cross walkway and the concourse, 8 m: paler paving slabs 2 m
                          square, a few slabs of another batch.
    arena_bowl_steel      rails, copings, fins, roofs, 8 m: painted steel, three broad sheen bands.
    arena_bowl_panel      the outside walls, 16 m: four courses of cladding panels of uneven
                          widths, and windows in CLUSTERS that suggest rooms (lit ones are in
                          arena_bowl_panel_emit).
  ATLASES (regions below):
    arena_bowl_seat       four seats wide (2.2 m), four bands up the seat's profile: front, pan,
                          back, the back's rear. The seat rows are real geometry; this paints the
                          single seats on them.
    arena_bowl_led        the LED rows (64 px per metre, 16 m per tile along u): the field
                          barrier, the front wall's ribbon, the upper stands' ribbon, the cyan,
                          magenta and dashed lines. DEEP blue (#0a1a9a) and white, never sky blue.
                          arena_bowl_led_emit is what glows.
    arena_bowl_door       what is seen at the back of an opening: a walkway doorway, a deck gate,
                          the players' tunnel, the two kiosk fronts and the kiosk sign.
                          arena_bowl_door_emit is what glows.
    arena_bowl_marking    RGBA: the TUMP logo as PALE FIELD PAINT (one tone family, no orange: the
                          team colours are reserved) and a band of the same white paint for the
                          field's rings and spokes. Alpha is the cutout.

ROLE HUES: nothing near offence orange #f87020 or defence blue #0080e8. Blues are deep navy or
indigo, or go to white or cyan.
"""
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "TumbangPreso", "Art", "Arena", "Textures")
SHEETS = os.path.join(ROOT, "Logs", "arena", "bowl")
LOGO = os.path.join(ROOT, "Assets", "TumbangPreso", "Art", "ui", "brand", "tump_logo.png")

# ------------------------------------------------------------------ constants the model imports
PX = 1024
TILE_M = {"turf_a": 16.0, "turf_b": 16.0, "track": 16.0, "panel": 16.0,
          "concrete": 8.0, "step": 8.0, "steel": 8.0}

# The seat atlas: 4 seats across the tile; bands in v (0 at the bottom of the image).
SEATS_PER_TILE = 4
SEAT_PITCH_M = 0.55
SEAT_BANDS = {"front": (0.0, 196 / 1024), "pan": (196 / 1024, 364 / 1024),
              "back": (364 / 1024, 584 / 1024), "rear": (584 / 1024, 816 / 1024)}

# The LED atlas, 1024 x 512: name -> (y0, y1) in pixels from the TOP. 16 m of ribbon per tile.
LED_SIZE = (1024, 512)
LED_TILE_M = 16.0
LED_ROWS = {"barrier": (6, 102), "front": (108, 172), "ribbon": (178, 370),
            "cyan": (376, 408), "magenta": (414, 446), "dash": (452, 484)}

# The door atlas, 1024 x 1024: name -> (x0, y0, x1, y1) in pixels from the top left.
DOOR_REGIONS = {"door": (8, 8, 248, 328), "gate": (264, 8, 504, 381), "tunnel": (520, 8, 1016, 315),
                "kiosk": (8, 400, 648, 720), "kiosk_b": (664, 400, 1016, 646), "kiosk_sign": (8, 736, 648, 800),
                "tower_gate": (664, 660, 1016, 934)}

# The marking atlas, 1024 x 1024: the logo fills the top, the paint band sits under it.
MARKING_LOGO = (0, 0, 1024, 674)
MARKING_PAINT = (0, 720, 1024, 1000)


def led_v(row):
    """A row's (v at its bottom edge, v at its top edge), v up."""
    y0, y1 = LED_ROWS[row]
    return 1.0 - y1 / LED_SIZE[1], 1.0 - y0 / LED_SIZE[1]


def region_uv(region, size=1024):
    """(u0, v0, u1, v1), v up, of a pixel box in a square atlas."""
    x0, y0, x1, y1 = region
    return x0 / size, 1.0 - y1 / size, x1 / size, 1.0 - y0 / size


# ------------------------------------------------------------------ the painting (system Python)
def main():
    import numpy as np
    from PIL import Image, ImageDraw
    from scipy import ndimage

    sheet = 1
    for a in sys.argv:
        if a.startswith("--sheet"):
            sheet = int(a.split("=", 1)[1]) if "=" in a else int(sys.argv[sys.argv.index(a) + 1])
    os.makedirs(OUT, exist_ok=True)
    os.makedirs(SHEETS, exist_ok=True)
    written = []

    def hexcol(h):
        return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)])

    def fill(h, w, colour):
        return np.ones((h, w, 3)) * hexcol(colour)

    def smooth(h, w, scale_px, seed, stretch=(1.0, 1.0)):
        """Periodic smooth field, features about `scale_px` across. Tiles exactly."""
        white = np.random.default_rng(seed).standard_normal((h, w))
        fy = np.fft.fftfreq(h)[:, None] * stretch[1]
        fx = np.fft.fftfreq(w)[None, :] * stretch[0]
        n = np.real(np.fft.ifft2(np.fft.fft2(white) * np.exp(-(fx ** 2 + fy ** 2) * scale_px * scale_px * 2)))
        return (n - n.mean()) / (n.std() + 1e-9)

    def patches(h, w, scale_px, coverage, seed, feather=1.0, stretch=(1.0, 1.0)):
        """A few large organic patches covering `coverage` of the image, feathered at the edge."""
        n = smooth(h, w, scale_px, seed, stretch)
        edge = np.quantile(n, 1 - coverage)
        m = np.clip((n - edge) / feather + 0.5, 0, 1)
        return m * m * (3 - 2 * m)

    def coat(img, shift, scale_px, coverage, seed, feather=1.0, stretch=(1.0, 1.0)):
        m = patches(img.shape[0], img.shape[1], scale_px, coverage, seed, feather, stretch)
        return img * (1 + (np.asarray(shift) - 1) * m[..., None])

    def lay(img, mask, colour):
        c = hexcol(colour) if isinstance(colour, str) else np.asarray(colour)
        return img * (1 - mask[..., None]) + c * mask[..., None]

    def grid(h, w, wobble=0.0, scale=60.0, seed=1):
        """Pixel coordinates, pushed about by a smooth field so a drawn edge is a hand's edge."""
        yy, xx = np.mgrid[0:h, 0:w].astype(float)
        if wobble:
            xx = xx + smooth(h, w, scale, seed) * wobble
            yy = yy + smooth(h, w, scale, seed + 1) * wobble
        return yy, xx

    def rrect(yy, xx, box, radius=0.0, feather=1.3):
        x0, y0, x1, y1 = box
        cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
        hx, hy = (x1 - x0) / 2 - radius, (y1 - y0) / 2 - radius
        qx, qy = np.abs(xx - cx) - hx, np.abs(yy - cy) - hy
        d = np.hypot(np.maximum(qx, 0), np.maximum(qy, 0)) + np.minimum(np.maximum(qx, qy), 0) - radius
        return np.clip(-d / feather + 0.5, 0, 1)

    def wrapped(fn, w):
        """A shape drawn three times, one tile apart, so it may cross the tile's edge."""
        return np.clip(fn(-w) + fn(0) + fn(w), 0, 1)

    def joints(h, w, spacing, width, wobble, seed, axis):
        """Soft hand-drawn joint lines every `spacing` px, drifting a little along their length."""
        yy, xx = np.mgrid[0:h, 0:w].astype(float)
        if axis == "h":
            drift = smooth(1, w, w / 5, seed)[0][None, :] * wobble
            d = (yy - drift) % spacing
        else:
            drift = smooth(h, 1, h / 5, seed)[:, 0][:, None] * wobble
            d = (xx - drift) % spacing
        d = np.minimum(d, spacing - d)
        return np.clip(1 - d / width, 0, 1) ** 1.5

    def save(name, img, alpha=None):
        rgb = (np.clip(img, 0, 1) * 255 + 0.5).astype(np.uint8)
        if alpha is not None:
            rgb = np.dstack([rgb, (np.clip(alpha, 0, 1) * 255 + 0.5).astype(np.uint8)])
        path = os.path.join(OUT, "arena_bowl_%s.png" % name)
        Image.fromarray(rgb, "RGBA" if alpha is not None else "RGB").save(path)
        written.append((name, path))
        print("[arena-bowl-tex]", name, rgb.shape)

    # ---------------------------------------------------------------- tiling surfaces
    def turf(name, base, deep, pale, seed):
        """Grass: one flat green, a broad deeper coat, a broad paler coat, five soft clumps."""
        img = fill(PX, PX, base)
        img = lay(img, patches(PX, PX, 300, 0.34, seed, feather=1.1) * 0.75, deep)
        img = lay(img, patches(PX, PX, 210, 0.22, seed + 1, feather=0.9) * 0.6, pale)
        rng = np.random.default_rng(seed + 2)
        yy, xx = np.mgrid[0:PX, 0:PX].astype(float)
        for _ in range(5):                                  # clumps: flat lozenges, feathered, wrapping
            cx, cy, rx, ry = rng.uniform(0, PX), rng.uniform(0, PX), rng.uniform(46, 80), rng.uniform(26, 44)
            dx = (xx - cx + PX / 2) % PX - PX / 2
            dy = (yy - cy + PX / 2) % PX - PX / 2
            m = np.clip((1 - np.hypot(dx / rx, dy / ry)) * 4.0 + 0.5, 0, 1)
            img = lay(img, m * 0.35, pale)
        save(name, img)

    def track():
        """Rubber: near-navy charcoal, two worn fields, four long soft scuffs lying one way."""
        img = fill(PX, PX, "2b3041")
        img = lay(img, patches(PX, PX, 320, 0.3, 4101, feather=1.2) * 0.6, "333a4e")
        img = lay(img, patches(PX, PX, 240, 0.2, 4102, feather=1.0) * 0.55, "252a39")
        scuff = patches(PX, PX, 420, 0.1, 4103, feather=0.8, stretch=(0.22, 1.0))
        img = lay(img, scuff * 0.5, "3a4258")
        save("track", img)

    def concrete():
        """Structure: navy-grey, formwork joints every 2 m (lifts) and 4 m (bays), damp fields."""
        img = fill(PX, PX, "475066")
        img = lay(img, patches(PX, PX, 280, 0.32, 4201, feather=1.2) * 0.6, "4e586f")
        img = lay(img, patches(PX, PX, 200, 0.2, 4202, feather=1.0) * 0.6, "414a5f")
        j = np.maximum(joints(PX, PX, 256, 3.0, 5.0, 4203, "h"), joints(PX, PX, 512, 3.0, 5.0, 4204, "v"))
        img = img * (1 - 0.10 * j[..., None])
        damp = patches(PX, PX, 360, 0.14, 4205, feather=1.2, stretch=(1.0, 0.45))
        img = lay(img, damp * 0.45, "3c4558")
        save("concrete", img)

    def step():
        """Paving: pale blue-grey slabs 2 m square, joints barely darker, six slabs of another batch."""
        img = fill(PX, PX, "6a7489")
        img = lay(img, patches(PX, PX, 300, 0.3, 4301, feather=1.3) * 0.5, "727c92")
        rng = np.random.default_rng(4302)
        yy, xx = grid(PX, PX, 2.0, 70, 4303)
        for k in range(6):
            i, j_ = rng.integers(0, 4, 2)
            m = rrect(yy, xx, (j_ * 256 + 5, i * 256 + 5, j_ * 256 + 251, i * 256 + 251), 8, 2.0)
            img = lay(img, m * 0.3, "636d82" if k % 2 else "76819a")
        j = np.maximum(joints(PX, PX, 256, 3.2, 4.0, 4304, "h"), joints(PX, PX, 256, 3.2, 4.0, 4305, "v"))
        img = img * (1 - 0.13 * j[..., None])
        save("step", img)

    def steel():
        """Painted steel: blue-grey, three broad soft sheen bands, one dull field."""
        img = fill(PX, PX, "525c76")
        yy = np.mgrid[0:PX, 0:PX][0].astype(float)
        drift = smooth(1, PX, 300, 4401)[0][None, :] * 26
        for k, (centre, half, tone) in enumerate(((170, 70, "5d6884"), (520, 110, "596480"), (850, 60, "606b88"))):
            d = np.abs(((yy - drift - centre + PX / 2) % PX) - PX / 2)
            img = lay(img, np.clip((half - d) / 18 + 0.5, 0, 1) * 0.7, tone)
        img = lay(img, patches(PX, PX, 320, 0.2, 4402, feather=1.2) * 0.5, "4a546c")
        save("steel", img)

    def panel():
        """The outside walls: cladding in four 4 m courses, each cut into panels of uneven width,
        a paler trim line under the second course, and windows in clusters (rooms, a stair, a
        plant room), most dark, some lit. The lit ones are the emission map."""
        img = fill(PX, PX, "1d2743")
        emit = np.zeros((PX, PX, 3))
        yy, xx = grid(PX, PX, 2.2, 80, 4501)
        widths = ((300, 212, 280, 232), (190, 330, 244, 260), (268, 236, 310, 210), (228, 290, 196, 310))
        tones = (("1f2946", "1c2642", "202a48", "1e2844"), ("1c2642", "1f2946", "1e2844", "1b2540"),
                 ("1e2845", "1d2743", "1c2541", "202a47"), ("1d2743", "1f2946", "1c2642", "1e2845"))
        for row in range(4):
            x = 0
            for wd, tone in zip(widths[row], tones[row]):
                m = rrect(yy, xx, (x + 5, row * 256 + 5, x + wd - 5, row * 256 + 251), 6, 2.0)
                img = lay(img, m, tone)
                x += wd
        img = lay(img, patches(PX, PX, 340, 0.25, 4502, feather=1.3) * 0.35, "253256")
        trim = rrect(yy, xx, (-40, 506, PX + 40, 520), 3, 1.6)
        img = lay(img, trim * 0.8, "33416a")
        # Windows: ONE course of them a tile (a concourse level every 16 m up the wall), in two
        # clusters that read as rooms; most are dark. (first x, count, pitch, width, height, lit, colour).
        clusters = ((150, 5, 64, 36, 92, (1, 2), "c4e8ff"), (700, 2, 64, 36, 92, (1,), "ffdcae"))
        for x0, count, pitch, wd, ht, lit, colour in clusters:
            y0 = 256 + (256 - ht) / 2 - 8
            for k in range(count):
                box = (x0 + k * pitch, y0, x0 + k * pitch + wd, y0 + ht)
                m = rrect(yy, xx, box, 6, 1.6)
                if k in lit:
                    img = lay(img, m, hexcol(colour) * 0.5)
                    emit = lay(emit, m, hexcol(colour) * (0.8 if k % 2 == 0 else 0.5))
                    sill = rrect(yy, xx, (box[0] + 5, box[3] - 18, box[2] - 5, box[3] - 5), 3, 1.4)
                    img = lay(img, sill * 0.5, "2a3558")
                    emit = lay(emit, sill * 0.6, "000000")
                else:
                    img = lay(img, m, "111829")
        save("panel", img)
        save("panel_emit", emit)

    # ---------------------------------------------------------------- the seat atlas
    def seat():
        """Four single seats, each drawn on four bands of the row's profile: the front under the
        pan's lip, the pan from above, the backrest, the back's rear shell. Navy plastic."""
        gap = "0f152b"
        img = fill(PX, PX, gap)
        yy, xx = grid(PX, PX, 2.4, 70, 4601)
        vy = PX - yy                                        # v in pixels, up from the bottom
        tone = (1.0, 0.955, 1.04, 0.98)

        def band(name):
            a, b = SEAT_BANDS[name]
            return a * PX, b * PX

        for s in range(SEATS_PER_TILE):
            x0 = s * 256
            sx = xx - x0
            k = tone[s]

            def shape(bx0, t0, bx1, t1, lo, hi, radius, feather=1.5):
                box = (bx0, lo + t0 * (hi - lo), bx1, lo + t1 * (hi - lo))
                inside = np.clip((vy - lo) / 1.5 + 0.5, 0, 1) * np.clip((hi - vy) / 1.5 + 0.5, 0, 1)
                return rrect(vy, sx, box, radius, feather) * inside

            lo, hi = band("front")                          # the floor at the bottom, the lip at the top
            img = lay(img, shape(58, -0.2, 76, 0.55, lo, hi, 5), hexcol("151d38") * k)      # two brackets
            img = lay(img, shape(180, -0.2, 198, 0.55, lo, hi, 5), hexcol("151d38") * k)
            img = lay(img, shape(14, 0.42, 242, 1.3, lo, hi, 20), hexcol("1b2648") * k)     # the pan's underside
            img = lay(img, shape(22, 0.84, 234, 0.97, lo, hi, 10, 2.0), hexcol("27345f") * k)   # the lip
            lo, hi = band("pan")                            # the front edge at the bottom, the hinge at the top
            img = lay(img, shape(12, 0.03, 244, 0.97, lo, hi, 28), hexcol("202c52") * k)
            img = lay(img, shape(40, 0.14, 216, 0.56, lo, hi, 30, 7.0) * 0.8, hexcol("283663") * k)
            lo, hi = band("back")                           # the hinge at the bottom, the top of the back at the top
            img = lay(img, shape(16, -0.3, 240, 0.95, lo, hi, 36), hexcol("1e2a50") * k)
            img = lay(img, shape(36, 0.40, 150, 0.82, lo, hi, 30, 7.0) * 0.75, hexcol("263460") * k)
            lo, hi = band("rear")                           # the top of the back at the bottom of the band
            img = lay(img, shape(16, 0.05, 240, 1.3, lo, hi, 36), hexcol("182242") * k)
            img = lay(img, shape(30, 0.36, 226, 0.43, lo, hi, 6, 2.5) * 0.8, hexcol("1f2a4e") * k)
        img = coat(img, (1.05, 1.05, 1.05), 300, 0.3, 4602, feather=1.3)
        save("seat", img)

    # ---------------------------------------------------------------- the LED atlas
    def led():
        """Blue and white ribbons. DEEP blue #0a1a9a; white shapes with a hand's edge: chevrons,
        long rounded bars, slanted blocks, dots. Seams between cabinets are unlit lines."""
        w, h = LED_SIZE
        blue, white, cyan, magenta = hexcol("0a1a9a"), hexcol("eaf2ff"), hexcol("58e0ff"), hexcol("dd2a90")
        emit = np.zeros((h, w, 3))
        yy, xx = grid(h, w, 1.6, 50, 4701)

        def row_box(name):
            return LED_ROWS[name]

        def bar(y0, y1, cx, cy_t, half_w, half_t, radius, slant=0.0):
            """A rounded bar inside a row, by its centre and half sizes; half_t and cy_t are
            fractions of the row's height. `slant` leans it."""
            ht = y1 - y0
            cy = y0 + cy_t * ht

            def one(shift):
                sx = xx - shift - slant * (yy - cy)
                return rrect(yy, sx, (cx - half_w, cy - half_t * ht, cx + half_w, cy + half_t * ht), radius, 1.3)
            return wrapped(one, w)

        def chevron(y0, y1, cx, half_w, thick):
            ht = y1 - y0
            cy = (y0 + y1) / 2

            def one(shift):
                sx = xx - shift + np.abs(yy - cy) * 0.9
                return rrect(yy, sx, (cx - thick, y0 + 0.14 * ht, cx + thick, y1 - 0.14 * ht), 3, 1.3)
            return wrapped(one, w)

        def rowmask(y0, y1):
            return ((yy >= y0 - 3) & (yy < y1 + 3)).astype(float)[..., None]

        # The barrier: three cabinets of 5.33 m per tile. Two blue with white drawing, one white with blue.
        y0, y1 = row_box("barrier")
        emit = emit * (1 - rowmask(y0, y1)) + blue * rowmask(y0, y1)
        third = w / 3
        for k in range(3):                                  # cabinet 0: three chevrons and a bar
            emit = lay(emit, chevron(y0, y1, 70 + k * 46, 0, 11), white)
        emit = lay(emit, bar(y0, y1, 258, 0.5, 52, 0.16, 12), white)
        emit = lay(emit, bar(y0, y1, third + third / 2, 0.5, third / 2 - 60, 0.34, 22), white)     # cabinet 1: a white plate
        for k in range(3):
            emit = lay(emit, bar(y0, y1, third + 118 + k * 52, 0.5, 11, 0.24, 4, slant=0.55), blue)
        emit = lay(emit, bar(y0, y1, 2 * third + 60, 0.5, 22, 0.23, 22), white)                    # cabinet 2: a dot and one long bar
        emit = lay(emit, bar(y0, y1, 2 * third + 200, 0.5, 90, 0.11, 8), white)
        # The front wall's ribbon: long dashes, a dot between them.
        y0, y1 = row_box("front")
        emit = emit * (1 - rowmask(y0, y1)) + hexcol("071168") * rowmask(y0, y1)
        for cx, half in ((190, 90), (700, 60)):
            emit = lay(emit, bar(y0, y1, cx, 0.5, half, 0.13, 7), white)
        emit = lay(emit, bar(y0, y1, 450, 0.5, 8, 0.13, 8), white)
        # The upper stands' ribbon, 3 m tall: a long band with blue cuts, slanted bars, a four-point star.
        y0, y1 = row_box("ribbon")
        emit = emit * (1 - rowmask(y0, y1)) + blue * rowmask(y0, y1)
        emit = lay(emit, bar(y0, y1, 250, 0.5, 190, 0.24, 30), white)
        for k in range(3):
            emit = lay(emit, bar(y0, y1, 130 + k * 70, 0.5, 14, 0.4, 3, slant=0.5), blue)
        for k in range(4):
            emit = lay(emit, bar(y0, y1, 560 + k * 52, 0.5, 13, 0.30, 5, slant=-0.5), white)
        ht = y1 - y0
        cx, cy = 880.0, y0 + ht / 2
        star = np.clip((1 - (np.abs(xx - cx) / 70) ** 0.6 - (np.abs(yy - cy) / (0.42 * ht)) ** 0.6) * 9 + 0.5, 0, 1)
        emit = lay(emit, star, white)
        emit = lay(emit, bar(y0, y1, 880, 0.12, 60, 0.035, 4), white)
        emit = lay(emit, bar(y0, y1, 880, 0.88, 60, 0.035, 4), white)
        # The thin lines.
        y0, y1 = row_box("cyan")
        emit = emit * (1 - rowmask(y0, y1)) + cyan * 0.8 * rowmask(y0, y1)
        y0, y1 = row_box("magenta")
        emit = emit * (1 - rowmask(y0, y1)) + magenta * rowmask(y0, y1)
        y0, y1 = row_box("dash")
        emit = emit * (1 - rowmask(y0, y1)) + blue * rowmask(y0, y1)
        for k in range(4):
            emit = lay(emit, bar(y0, y1, 64 + k * 256, 0.5, 48, 0.2, 5), white)
        # Cabinet seams on the wide rows, unlit, soft.
        for name, every in (("barrier", w / 3), ("ribbon", w / 4)):
            y0, y1 = row_box(name)
            d = np.abs(((xx + every / 2) % every) - every / 2)
            emit = emit * (1 - (np.clip(1 - d / 2.2, 0, 1) * ((yy >= y0) & (yy < y1)))[..., None] * 0.85)
        # Uneven brightness in broad fields: old cabinets, never noise.
        emit = emit * (1 - 0.10 * patches(h, w, 180, 0.3, 4702, feather=1.2)[..., None])
        albedo = hexcol("0a1024") * 0.8 + emit * 0.14        # unlit glass: the light is the emission map
        save("led", albedo)
        save("led_emit", emit)

    # ---------------------------------------------------------------- the door atlas
    def door():
        """What stands at the BACK of each modelled opening, 2 to 10 m inside it."""
        img = fill(PX, PX, "0a0e1a")
        emit = np.zeros((PX, PX, 3))
        yy, xx = grid(PX, PX, 1.8, 60, 4801)

        def inbox(region, fx0, fy0, fx1, fy1):
            x0, y0, x1, y1 = region
            return (x0 + fx0 * (x1 - x0), y0 + fy0 * (y1 - y0), x0 + fx1 * (x1 - x0), y0 + fy1 * (y1 - y0))

        def base(region, colour):
            x0, y0, x1, y1 = region
            m = ((xx >= x0 - 6) & (xx < x1 + 6) & (yy >= y0 - 6) & (yy < y1 + 6)).astype(float)
            return m

        # A walkway doorway: a dark passage, a lit landing far in, a stair's first steps.
        r = DOOR_REGIONS["door"]
        img = lay(img, base(r, 0), "1a2238")                          # the passage's far wall, dim
        fl = rrect(yy, xx, inbox(r, -0.1, 0.8, 1.1, 1.1), 2, 2.0)     # its floor running on
        img = lay(img, fl, "2b3552"); emit = lay(emit, fl, "10141f")
        m = rrect(yy, xx, inbox(r, 0.3, 0.3, 0.7, 0.84), 6, 2.0)      # the lit stair hall beyond, down to the floor
        img = lay(img, m, "4a5a86"); emit = lay(emit, m, "3a4a78")
        m = rrect(yy, xx, inbox(r, 0.3, 0.72, 0.7, 0.84), 3, 1.6)
        img = lay(img, m, "5d6d98"); emit = lay(emit, m, "44547f")
        m = rrect(yy, xx, inbox(r, 0.34, 0.09, 0.66, 0.13), 4, 1.5)   # a ceiling lamp
        img = lay(img, m, "cfe2ff"); emit = lay(emit, m, "cfe2ff")
        m = rrect(yy, xx, inbox(r, 0.08, 0.42, 0.2, 0.6), 4, 1.5)     # a notice on the wall
        img = lay(img, m, "2d3856")
        # A deck gate: two steel leaves with a small lit window each, push bars, a frame.
        r = DOOR_REGIONS["gate"]
        img = lay(img, base(r, 0), "222b47")
        for a, b in ((0.06, 0.49), (0.51, 0.94)):
            img = lay(img, rrect(yy, xx, inbox(r, a, 0.07, b, 1.05), 6, 1.6), "36446a")
            m = rrect(yy, xx, inbox(r, a + 0.1, 0.16, b - 0.1, 0.36), 6, 1.6)
            img = lay(img, m, "c9b78f"); emit = lay(emit, m, "8a7a58")
            img = lay(img, rrect(yy, xx, inbox(r, a + 0.05, 0.56, b - 0.05, 0.6), 4, 1.4), "5a6a94")
        # A stair tower's gate, 3.6 x 2.8 m: a wide shutter of broad slats, a lit transom over it.
        r = DOOR_REGIONS["tower_gate"]
        img = lay(img, base(r, 0), "20294a")
        img = lay(img, rrect(yy, xx, inbox(r, 0.06, 0.3, 0.94, 1.06), 6, 1.8), "3a4870")
        for j in range(5):
            img = lay(img, rrect(yy, xx, inbox(r, 0.08, 0.4 + j * 0.12, 0.92, 0.425 + j * 0.12), 2, 1.4), "2f3b60")
        m = rrect(yy, xx, inbox(r, 0.1, 0.1, 0.9, 0.22), 6, 1.6)
        img = lay(img, m, "bcd9ea"); emit = lay(emit, m, "6f97ad")
        for fx in (0.36, 0.64):
            b = rrect(yy, xx, inbox(r, fx - 0.012, 0.09, fx + 0.012, 0.23), 2, 1.3)
            img = lay(img, b, "20294a"); emit = lay(emit, b, "000000")
        # The players' tunnel: the passage narrowing away, two ceiling lights, a pale floor.
        r = DOOR_REGIONS["tunnel"]
        img = lay(img, base(r, 0), "0a0e1a")
        img = lay(img, rrect(yy, xx, inbox(r, 0.1, 0.08, 0.9, 1.1), 14, 2.0), "141b2e")
        img = lay(img, rrect(yy, xx, inbox(r, 0.24, 0.2, 0.76, 1.1), 12, 2.0), "1c2540")
        m = rrect(yy, xx, inbox(r, 0.37, 0.34, 0.63, 0.9), 10, 2.0)
        img = lay(img, m, "2c3a60"); emit = lay(emit, m, "1c2744")
        for fx in (0.3, 0.7):
            m = rrect(yy, xx, inbox(r, fx - 0.06, 0.13, fx + 0.06, 0.17), 4, 1.4)
            img = lay(img, m, "cfe2ff"); emit = lay(emit, m, "cfe2ff")
        m = rrect(yy, xx, inbox(r, 0.44, 0.24, 0.56, 0.27), 3, 1.4)
        img = lay(img, m, "b8cdf0"); emit = lay(emit, m, "a8bde0")
        img = lay(img, rrect(yy, xx, inbox(r, 0.1, 0.9, 0.9, 1.1), 4, 2.0), "232c46")
        # Kiosk A, 6 x 3 m: a counter, a wide lit opening, three menu boards, two posts.
        r = DOOR_REGIONS["kiosk"]
        img = lay(img, base(r, 0), "2a3558")
        m = rrect(yy, xx, inbox(r, 0.05, 0.12, 0.95, 0.62), 10, 2.0)
        img = lay(img, m, "e8cf9f"); emit = lay(emit, m, "c9a56a")
        for k in range(3):
            b = rrect(yy, xx, inbox(r, 0.09 + k * 0.29, 0.15, 0.33 + k * 0.29, 0.3), 5, 1.5)
            img = lay(img, b, "2d395c"); emit = lay(emit, b, "0b0e18")
            for j in range(2):
                ln = rrect(yy, xx, inbox(r, 0.115 + k * 0.29, 0.185 + j * 0.05, 0.25 + k * 0.29 + j * 0.04, 0.205 + j * 0.05), 2, 1.3)
                img = lay(img, ln, "9fb0d4"); emit = lay(emit, ln, "55648a")
        for fx in (0.345, 0.655):
            b = rrect(yy, xx, inbox(r, fx - 0.012, 0.1, fx + 0.012, 0.64), 2, 1.4)
            img = lay(img, b, "1c2440"); emit = lay(emit, b, "000000")
        for fx, wd in ((0.2, 0.05), (0.48, 0.07), (0.8, 0.045)):      # things on the counter, as silhouettes
            b = rrect(yy, xx, inbox(r, fx, 0.5, fx + wd, 0.63), 6, 1.5)
            img = lay(img, b, "3a2f3c"); emit = lay(emit, b, "1a1418")
        img = lay(img, rrect(yy, xx, inbox(r, 0.03, 0.62, 0.97, 0.68), 3, 1.5), "53618a")       # the counter's edge
        img = lay(img, rrect(yy, xx, inbox(r, 0.1, 0.74, 0.42, 0.93), 6, 2.0), "313d63")        # two panels under it
        img = lay(img, rrect(yy, xx, inbox(r, 0.58, 0.74, 0.9, 0.93), 6, 2.0), "313d63")
        # Kiosk B, 4 x 2.8 m: a hatch with a half-raised shutter and one lamp.
        r = DOOR_REGIONS["kiosk_b"]
        img = lay(img, base(r, 0), "2e3350")
        m = rrect(yy, xx, inbox(r, 0.12, 0.3, 0.88, 0.66), 8, 1.8)
        img = lay(img, m, "bfe3ee"); emit = lay(emit, m, "6fa9b8")
        sh = rrect(yy, xx, inbox(r, 0.1, 0.1, 0.9, 0.34), 5, 1.6)
        img = lay(img, sh, "465074"); emit = lay(emit, sh, "000000")
        for j in range(3):
            img = lay(img, rrect(yy, xx, inbox(r, 0.12, 0.15 + j * 0.06, 0.88, 0.17 + j * 0.06), 2, 1.3), "39425f")
        b = rrect(yy, xx, inbox(r, 0.42, 0.44, 0.6, 0.66), 8, 1.5)
        img = lay(img, b, "2b2f45"); emit = lay(emit, b, "101320")
        img = lay(img, rrect(yy, xx, inbox(r, 0.08, 0.66, 0.92, 0.72), 3, 1.5), "5a668c")
        # The kiosk's sign, 6 x 0.6 m: bars that read as a name, no letters, cyan-white on indigo.
        r = DOOR_REGIONS["kiosk_sign"]
        img = lay(img, base(r, 0), "1a1f58")
        emit = lay(emit, base(r, 0), "10144a")
        x = 0.07
        for wd in (0.05, 0.11, 0.03, 0.08, 0.14, 0.04, 0.09, 0.06, 0.12):
            m = rrect(yy, xx, inbox(r, x, 0.26, x + wd, 0.74), 7, 1.5)
            img = lay(img, m, "d9f2ff"); emit = lay(emit, m, "bfe6ff")
            x += wd + 0.022
        save("door", img)
        save("door_emit", emit)

    # ---------------------------------------------------------------- the field's paint
    def marking():
        """The logo as field paint: its own drawing kept, its colours taken to one pale family
        (white, a chalk green-grey, a deep outline green), because the team colours are reserved
        and a turf logo is paint, not a print. Under it, the white paint of the lines."""
        img = np.zeros((PX, PX, 3)); alpha = np.zeros((PX, PX))
        x0, y0, x1, y1 = MARKING_LOGO
        logo = Image.open(LOGO).convert("RGBA").resize((x1 - x0, y1 - y0), Image.LANCZOS)
        a = np.asarray(logo, dtype=float) / 255
        rgb, al = a[..., :3], a[..., 3]
        lum = rgb[..., 0] * 0.35 + rgb[..., 1] * 0.5 + rgb[..., 2] * 0.15
        lum = ndimage.gaussian_filter(lum, 0.8)
        dark, mid, pale = hexcol("1f5a34"), hexcol("b9cfb4"), hexcol("f1f5ec")
        t1 = np.clip((lum - 0.30) / 0.10, 0, 1)[..., None]           # the maroon outline goes deep green
        t2 = np.clip((lum - 0.62) / 0.10, 0, 1)[..., None]           # the cream letters go white
        col = dark * (1 - t1) + (mid * (1 - t2) + pale * t2) * t1
        img[y0:y1, x0:x1] = col
        alpha[y0:y1, x0:x1] = np.clip((al - 0.22) * 8 + 0.5, 0, 1)   # the cream letters are half transparent in the file
        img[alpha < 0.02] = dark                                      # no pale fringe at the cutout
        px0, py0, px1, py1 = MARKING_PAINT
        band = fill(py1 - py0 + 48, PX, "edf1ea")
        band = lay(band, patches(py1 - py0 + 48, PX, 200, 0.3, 4901, feather=1.2) * 0.5, "dfe6dc")
        img[py0 - 24:py1 + 24] = band
        alpha[py0 - 24:py1 + 24] = 1.0
        save("marking", img, alpha)

    turf("turf_a", "3b8a45", "34803f", "44964d", 4001)
    turf("turf_b", "4a9f53", "429549", "55aa5d", 4051)
    track(); concrete(); step(); steel(); panel(); seat(); led(); door(); marking()

    # ---------------------------------------------------------------- the swatch sheet
    cell, pad = 384, 14
    cols = 5
    rows_n = (len(written) + cols - 1) // cols
    sheet_img = Image.new("RGB", (cols * (cell + pad) + pad, rows_n * (cell + pad + 22) + pad), (24, 26, 34))
    dr = ImageDraw.Draw(sheet_img)
    for k, (name, path) in enumerate(written):
        im = Image.open(path).convert("RGBA")
        bg = Image.new("RGBA", im.size, (60, 140, 70, 255))
        im = Image.alpha_composite(bg, im).convert("RGB")
        im.thumbnail((cell, cell), Image.LANCZOS)
        x = pad + (k % cols) * (cell + pad)
        y = pad + (k // cols) * (cell + pad + 22)
        sheet_img.paste(im, (x, y + 20))
        dr.text((x, y + 2), "arena_bowl_" + name, fill=(220, 226, 240))
    p = os.path.join(SHEETS, "bowl_swatches_v%d.png" % sheet)
    sheet_img.save(p)
    print("[arena-bowl-tex] sheet", p)


if __name__ == "__main__":
    main()
