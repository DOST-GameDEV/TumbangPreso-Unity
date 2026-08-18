"""Rasterises the donated face's ink triangles, so the expression can be tuned offline.

    python tools/face_mouth_sheet.py            the shipped settings
    python tools/face_mouth_sheet.py --sweep    a row of candidates to choose from

WHY THIS EXISTS. The mouth is the donor's own triangles moved in Y (see `_donor_head` in
`build_person_voxel.py`), and a Unity round trip to look at the result is several minutes.
This draws exactly what those triangles will be, in a couple of seconds.

⚠️⚠️ AND IT DRAWS THE INK OUTLINE, WHICH IS THE WHOLE REASON THE FIRST TWO ATTEMPTS
LOOKED NOTHING LIKE THE PLOT. `ToonSkin.PersonOutlineWidth` is `0.008 * 2.38`, and the
2.38 is `PersonScale`: in the model space this file authors in, the inverted hull stands
**8 mm** off every ink polygon. So a mouth stroke thinner than about 16 mm is swallowed by
its own halo, and a flattened smile does not read as a flat line, it reads as the original
smile with the gap filled in. 🧑 looking at the build that flattened it to 7 mm: the mouth
came back as a *":)"*. The expression has to be carried by the ink's SHAPE and never by a
thin line, because there is no such thing as a thin line on this character.

⚠️ THE BOTTOM ROW IS 90 PX, which is roughly what a head occupies in play. Anything that
only works in the top row does not exist in the game.
"""
import sys
import math

sys.path.insert(0, "tools")
from glb_mesh_dump import read_glb, read_accessor  # noqa: E402

try:
    from PIL import Image, ImageDraw
except ImportError:
    raise SystemExit("this needs Pillow: python -m pip install pillow")

DONOR = "Assets/TumbangPreso/Art/characters/persons/character-male-d.glb"
OUT = "Logs/face-mouth-sheet.png"

# See the module docstring. Model space, before PersonScale.
OUTLINE = 0.008

SKIN = (0xE8, 0xC9, 0x9A)
INK = (0x1A, 0x14, 0x22)
BG = (0x2A, 0x2C, 0x3C)


def slot_at(u, v):
    col = min(int(u * 16.0), 15)
    row = min(int(v * 16.0), 15)
    if row < 8:
        return None
    return (col // 2) + (8 if row >= 12 else 0)


def load():
    gltf, buffer = read_glb(DONOR)

    for node in gltf["nodes"]:
        if node.get("name") != "head-mesh":
            continue

        prim = gltf["meshes"][node["mesh"]]["primitives"][0]
        pos = [tuple(p) for p in read_accessor(gltf, buffer, prim["attributes"]["POSITION"])]
        uv = read_accessor(gltf, buffer, prim["attributes"]["TEXCOORD_0"])
        raw = read_accessor(gltf, buffer, prim["indices"])
        idx = [v[0] for v in raw] if isinstance(raw[0], tuple) else list(raw)

        ink = [(idx[t], idx[t + 1], idx[t + 2]) for t in range(0, len(idx), 3)
               if slot_at(*uv[idx[t]]) == 8]

        return pos, ink

    raise SystemExit("no head-mesh")


POS, INK_TRIS = load()
MOUTH = set()
for tri in INK_TRIS:
    if sum(POS[i][1] for i in tri) / 3.0 < 0.45:
        MOUTH.update(tri)


def smile_curve():
    """The donor smile's own centreline, `y = A + B x^2`, by least squares.

    ⚠️ THIS IS WHAT SEPARATES THE CURVE FROM THE STROKE, and it is why the bend can
    straighten the mouth without thinning it. `B` is the bow: 3.98 puts the corners 8.1 mm
    above the centre. The residuals are the stroke, 31 mm of it. Scaling the whole shape
    toward its pivot scales BOTH, which is what made a flat mouth a thin one.
    """
    n = len(MOUTH)
    sx2 = sum(POS[i][0] ** 2 for i in MOUTH)
    sx4 = sum(POS[i][0] ** 4 for i in MOUTH)
    sy = sum(POS[i][1] for i in MOUTH)
    sx2y = sum(POS[i][0] ** 2 * POS[i][1] for i in MOUTH)
    det = n * sx4 - sx2 * sx2
    return (sy * sx4 - sx2 * sx2y) / det, (n * sx2y - sx2 * sy) / det


CURVE_A, CURVE_B = smile_curve()


EYES = set()
for tri in INK_TRIS:
    if sum(POS[i][1] for i in tri) / 3.0 >= 0.45:
        EYES.update(tri)

EYE_CENTRE = {}
for side in (1.0, -1.0):
    lid = {i for i in EYES if POS[i][0] * side > 0.0}
    c = sum(POS[i][1] for i in lid) / len(lid)
    for i in lid:
        EYE_CENTRE[i] = c


def bend(p, curve, stroke, tilt):
    x, y, z = p
    line = CURVE_A + CURVE_B * x * x
    return (x, CURVE_A + (line - CURVE_A) * curve + (y - line) * stroke + x * tilt, z)


def lid(i, squash, drop):
    x, y, z = POS[i]
    c = EYE_CENTRE[i]
    return (x, c + (y - c) * squash - drop, z)


def render(curve, stroke, tilt, squash, drop, label, size=300):
    scale = 4
    W = H = size * scale
    img = Image.new("RGB", (W, H), SKIN)
    ink = Image.new("L", (W, H), 0)
    d = ImageDraw.Draw(ink)

    X0, X1 = 0.090, -0.090     # +x is the character's LEFT, drawn on the viewer's left
    Y0, Y1 = 0.372, 0.552

    def px(p):
        return ((p[0] - X0) / (X1 - X0) * W, H - (p[1] - Y0) / (Y1 - Y0) * H)

    def place(tri):
        low = sum(POS[i][1] for i in tri) / 3.0 < 0.45
        return [px(bend(POS[i], curve, stroke, tilt) if low else lid(i, squash, drop))
                for i in tri]

    for tri in INK_TRIS:
        d.polygon(place(tri), fill=255)

    # The inverted hull, as the width it actually is in this space. Drawn as a thick
    # stroke around each polygon rather than as a dilation of the mask: same result to
    # within a pixel and it does not cost a 100-tap filter per pixel.
    grow = OUTLINE / abs(X1 - X0) * W

    for tri in INK_TRIS:
        pts = place(tri)
        d.line(pts + [pts[0]], fill=255, width=int(grow) * 2, joint="curve")

    img.paste(INK, (0, 0), ink)
    img = img.resize((size, size), Image.LANCZOS)

    if label:
        ImageDraw.Draw(img).text((6, 6), label, fill=(0x60, 0x40, 0x20))

    return img


def sheet(variants, path):
    W = 300
    out = Image.new("RGB", (W * len(variants), W + 100), BG)

    for n, (curve, stroke, tilt, squash, drop, label) in enumerate(variants):
        out.paste(render(curve, stroke, tilt, squash, drop, label), (W * n, 0))
        out.paste(render(curve, stroke, tilt, squash, drop, "", 90), (W * n + 105, W + 5))

    out.save(path)
    print("wrote", path)
    print("donor smile: y = %.4f + %.4f x^2  (corners %.1f mm above centre)"
          % (CURVE_A, CURVE_B, CURVE_B * 0.045 ** 2 * 1000))


if __name__ == "__main__":
    if "--sweep" in sys.argv:
        sheet([
            (1.00, 1.00, 0.00, 1.00, 0.000, "donor, untouched"),
            (-0.35, 0.62, 0.34, 1.00, 0.000, "smirk, eyes untouched"),
            (-0.35, 0.62, 0.34, 0.70, 0.006, "+ eyes .70"),
            (-0.35, 0.62, 0.48, 0.55, 0.008, "+ eyes .55, tilt .48"),
            (-0.35, 0.62, 0.48, 0.42, 0.010, "+ eyes .42"),
            (-0.20, 0.55, 0.60, 0.42, 0.010, "tilt .60, eyes .42"),
            (-0.20, 0.55, 0.60, 0.30, 0.012, "tilt .60, eyes .30"),
        ], "Logs/face-mouth-sweep.png")
    else:
        sys.path.insert(0, ".")
        import importlib.util

        spec = importlib.util.spec_from_file_location("bpv", "tools/build_person_voxel.py")
        bpv = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(bpv)

        sheet([(1.00, 1.00, 0.00, 1.00, 0.0, "donor"),
               (bpv.MOUTH_CURVE, bpv.MOUTH_STROKE, bpv.MOUTH_TILT,
                bpv.EYE_SQUASH, bpv.EYE_DROP, "shipped")], OUT)
