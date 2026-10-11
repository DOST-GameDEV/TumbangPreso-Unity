"""Try rest placements for Dante's first-person arms CUT FROM HIS BODY MODEL (natural, uniform scale).

    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/render_character_redesign_dante_fpv_placement.py -- fpvcut01
    py -3 tools/render_character_redesign_dante_fpv_placement.py fpvcut01        (labels and stacks)

A RENDER ONLY. Owner, 2026-10-06: "experiment on a more natural hands placement so it doesnt look
too compressed in the center of the screen".

THE SPACE THE NUMBERS ARE IN. `ViewmodelArms.BuildArm` sets each arm's PIVOT (`RightPivot`,
`LeftPivot`; `_rightArm` and `_leftArm` sit on them at identity) with
    pivot.localPosition = ToUnityPosition(RightOrigin)          (0.58, -1.02, 0.34)
    pivot.localRotation = ToUnityRotation(RightBasisX, RightBasisY, RightBasisZ)
in the space of the `~ViewmodelArms` object, Unity axes (x right, y up, z ahead). That object hangs
on the camera at `CameraRig.ViewmodelSeat` (0, -0.10, 0.16), and at full framing weight
(`ViewmodelArms.Framing.cs`) is drawn 8 cm lower at scale 0.64 through a 95 degree vertical lens.
Every variant below is given as that pivot localPosition and the three axes of that
localRotation (the arm's x, y = its length, z), absolute, and as a change from today's:
the pivot's move, and the turn about the FIST as yaw (about the rig's up, inward), pitch (about
the rig's right, the hand tipped away from the eye) and roll (about the arm's own length).
A variant is designed from where the fist should sit on screen, then solved back to the pivot.
The left arm is the right arm mirrored in x for every variant but A (today's two pivots are not quite mirrors).
"""
import json
import math
import os
import sys

TOOLS = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, TOOLS)
import render_character_redesign_dante_fpv_arms as R  # noqa: E402
import render_character_redesign_dante_fpv_cut as C  # noqa: E402

TAN = math.tan(math.radians(R.FOV * 0.5))
ASPECT = 16.0 / 9.0
FIST_Y = (0.534, 0.840)          # the body hand block along the cut arm, once it is 0.84 long
FIST_MID = 0.687
# name: (fist centre on screen as fractions of the half width and half height, or None for today's;
#        depth of the fist relative to today's; yaw, pitch, roll in degrees; what it is)
VARIANTS = [
    ("A", None, 1.00, 0, 0, 0, "current placement, unchanged (the one turned down: too big, too close)"),
    ("C", (0.44, -0.80), 1.60, 20, 10, 10, "out to the corners, angled in and up"),
    ("CD1", (0.46, -0.78), 1.90, 20, 14, 10, "a step further and wider"),
    ("CD2", (0.48, -0.76), 2.20, 22, 16, 10, "two steps"),
    ("CD3", (0.52, -0.76), 2.50, 24, 18, 10, "three steps"),
    ("D", (0.56, -0.76), 2.80, 25, 18, 10, "furthest of the set, gap near half the screen"),
    ("D9", (0.56, -0.74), 3.20, 25, 20, 10, "as D, smaller still (the 9 per cent try)"),
    ("E", (0.48, -0.75), 2.30, 16, 22, -90, "mine: as CD2, the arm rolled a quarter turn so the drawn side of the fist is to the eye"),
]
# Owner, 2026-10-06, on the first sheet: "B has potential but u cant see the arms at all". So the B
# family: fists flat as today, no roll, little or no yaw, wide apart in the bottom corners, and
# the ARM brought onto the screen. Chosen with a second argument: ... -- fpvcut02 B
B_VARIANTS = [
    ("B0", (0.46, -0.88), 1.00, 0, 0, 0, "B exactly as before (reference)"),
    ("B1", (0.43, -0.80), 1.90, 0, 0, 0, "B, further from the lens"),
    ("B2", (0.43, -0.80), 1.90, 0, 12, 0, "B1, the hand tipped 12 degrees away"),
    ("B3", (0.43, -0.80), 1.90, 0, 22, 0, "B1, the hand tipped 22 degrees away"),
    ("B4", (0.43, -0.80), 1.90, 9, 12, 0, "B2, the forearms converging 9 degrees"),
    ("B5", (0.44, -0.77), 2.10, 6, 17, 0, "mine: a little further, tipped 17, converging 6"),
]
# Owner on the B sheet: "i think theyre too small". B0 had the presence (17 per cent) and no arm;
# B5 had the arm and no presence. So B5's style at five sizes between them. Each was found by a
# search over depth, pitch and where the fist sits, for the LOWEST fist and the smallest tip that
# still keeps 70 per cent of the gold cuff and of the wrap on screen, a gap of 30 per cent or
# more, and the aim box clear. Chosen with: ... -- fpvcut03 size
S_VARIANTS = [
    B_VARIANTS[0],
    ("S1", (0.44, -0.77), 1.68, 6, 15, 0, "B5's style, fist about 14.5 per cent"),
    ("S2", (0.44, -0.74), 1.48, 6, 15, 0, "about 16 per cent"),
    ("S3", (0.48, -0.71), 1.36, 6, 12, 0, "about 17.5 per cent"),
    ("S4", (0.44, -0.68), 1.24, 6, 12, 0, "about 19 per cent"),
    ("S5", (0.44, -0.65), 1.12, 6, 15, 0, "about 21 per cent"),
    B_VARIANTS[5],
]
# Owner on the size sheet: "S5 looks good but can you make it less tilted". S5's size and place,
# the forearm swung outward about the fist in steps (yaw goes negative: out, not in), and one
# with the other tilt (the tip away from the eye) halved instead. Chosen with: ... -- fpvcut04 tilt
T_VARIANTS = [
    S_VARIANTS[5],
    ("T1", (0.44, -0.65), 1.12, -6, 15, 0, "S5, forearms swung 12 degrees outward"),
    ("T2", (0.44, -0.65), 1.12, -18, 15, 0, "24 degrees outward"),
    ("T3", (0.44, -0.65), 1.12, -30, 15, 0, "36 degrees outward"),
    ("T4", (0.44, -0.65), 1.12, -42, 15, 0, "48 degrees outward"),
    ("T2b", (0.44, -0.65), 1.12, 6, 7, 0, "S5 with the tip away from the eye halved instead"),
]
SETS = {"": (VARIANTS, ("CD2", "E")), "B": (B_VARIANTS, ("B4", "B5")), "size": (S_VARIANTS, ()), "tilt": (T_VARIANTS, ())}
# the pieces along each cut arm once it is 0.84 long, from author_character_redesign_dante.py
PIECES = {"right": (("fist", 0.534, 0.840), ("cord", 0.493, 0.519), ("gold cuff", 0.298, 0.455), ("green sleeve", 0.0, 0.298)),
          "left": (("fist", 0.534, 0.840), ("wrap", 0.333, 0.543), ("bare arm", 0.26, 0.333), ("sleeve stub", 0.0, 0.26))}
SLIPPER = (0.10, 0.26, 0.03)     # a tsinelas-sized block: across, along, thick
SLIPPER_ON = ("CD2", "E")


def add(a, b): return tuple(x + y for x, y in zip(a, b))
def sub(a, b): return tuple(x - y for x, y in zip(a, b))
def mul(a, k): return tuple(x * k for x in a)
def dot(a, b): return sum(x * y for x, y in zip(a, b))


def turn(v, axis, degrees):
    """Rodrigues: `v` turned about `axis` (plain right-handed arithmetic; the signs are chosen below by what they do)."""
    a = math.radians(degrees)
    k = R.norm(axis)
    c, s = math.cos(a), math.sin(a)
    return add(add(mul(v, c), mul(R.cross(k, v), s)), mul(k, dot(k, v) * (1 - c)))


def frames(variant):
    """{side: (x, y, z, origin)} of the pivot in the rig's own space, for one variant."""
    name, ndc, depth, yaw, pitch, roll, _ = variant
    today = {side: R.rest_frame(side) for side in ("right", "left")}
    if ndc is None:
        return today
    x0, y0, z0, o0 = today["right"]
    fist0 = add(R.SEAT, mul(add(o0, mul(y0, FIST_MID)), R.RIG_SCALE))        # camera space
    zc = fist0[2] * depth
    fist = (ndc[0] * TAN * ASPECT * zc, ndc[1] * TAN * zc, zc)
    d = turn(y0, (0, 1, 0), -yaw)            # inward: the right hand swings toward -x
    d = turn(d, (1, 0, 0), pitch)            # the hand tips away from the eye
    d = R.norm(d)
    eye = R.norm(mul(fist, -1.0))
    z = R.norm(sub(eye, mul(d, dot(eye, d))))  # the +Z face square to the eye, as today's nearly is
    z = R.norm(turn(z, d, roll))
    out = {}
    for side, m in (("right", 1.0), ("left", -1.0)):
        dy = (d[0] * m, d[1], d[2])
        dz = (z[0] * m, z[1], z[2])
        dx = R.norm(R.cross(dy, dz))
        f = (fist[0] * m, fist[1], fist[2])
        origin = sub(mul(sub(f, R.SEAT), 1.0 / R.RIG_SCALE), mul(dy, FIST_MID))
        out[side] = (dx, dy, dz, origin)
    return out


def to_view(p, frame):
    x, y, z, o = frame
    u = [R.SEAT[a] + R.RIG_SCALE * (o[a] + x[a] * p[0] + y[a] * p[1] + z[a] * p[2]) for a in range(3)]
    return (u[0], u[1], -u[2])


def screen(p, frame):
    b = to_view(p, frame)
    depth = -b[2]
    return (b[0] / (depth * TAN * ASPECT), b[1] / (depth * TAN), depth)


def quaternion(x, y, z):
    m = (x, y, z)   # columns
    r = lambda i, j: m[j][i]
    t = r(0, 0) + r(1, 1) + r(2, 2)
    if t > 0:
        s = math.sqrt(t + 1.0) * 2
        return ((r(2, 1) - r(1, 2)) / s, (r(0, 2) - r(2, 0)) / s, (r(1, 0) - r(0, 1)) / s, 0.25 * s)
    i = max(range(3), key=lambda k: r(k, k))
    j, k = (i + 1) % 3, (i + 2) % 3
    s = math.sqrt(1.0 + r(i, i) - r(j, j) - r(k, k)) * 2
    q = [0, 0, 0, 0]
    q[i] = 0.25 * s
    q[j] = (r(j, i) + r(i, j)) / s
    q[k] = (r(k, i) + r(i, k)) / s
    q[3] = (r(k, j) - r(j, k)) / s
    return tuple(q)


def measure(variant, cut):
    """The numbers the sheet prints: where the fists land, how big, how much arm is in the frame."""
    fr = frames(variant)
    today = {side: R.rest_frame(side) for side in ("right", "left")}
    facts = {"name": variant[0], "what": variant[6], "yaw": variant[3], "pitch": variant[4], "roll": variant[5]}
    inner, blocked = {}, 0
    for side in ("right", "left"):
        verts = cut[side][0]
        pts = [screen(v, fr[side]) for v in verts]
        fist = [screen(v, fr[side]) for v in verts if v[1] > FIST_Y[0]]
        top = min(1.0, max(p[1] for p in fist))
        low = max(-1.0, min(p[1] for p in fist))
        inner[side] = min(p[0] for p in fist) if side == "right" else max(p[0] for p in fist)
        line = [screen((0.0, 0.84 * i / 200.0, 0.0), fr[side]) + (0.84 * i / 200.0,) for i in range(201)]
        seen = sum(1 for p in line if abs(p[0]) <= 1 and abs(p[1]) <= 1 and p[2] > 0) / 201.0
        blocked += sum(1 for p in pts if abs(p[0]) < 1 / 3.0 and p[1] > -0.5 and p[2] > 0)
        x, y, z, o = fr[side]
        facts[side] = {
            "position": [round(c, 4) for c in o], "move": [round(c, 4) for c in sub(o, today[side][3])],
            "x": [round(c, 5) for c in x], "y": [round(c, 5) for c in y], "z": [round(c, 5) for c in z],
            "quaternion": [round(c, 5) for c in quaternion(x, y, z)],
            "fist_height": round(max(0.0, top - low) / 2.0, 3), "fist_top": round((1 - top) / 2.0, 3),
            "arm_on_screen": round(seen, 3),
            "pieces": {n: round(sum(1 for p in line if a <= p[3] <= b and abs(p[0]) <= 1 and abs(p[1]) <= 1)
                                / max(1.0, sum(1 for p in line if a <= p[3] <= b)), 2) for n, a, b in PIECES[side]},
        }
    facts["gap"] = round((inner["right"] - inner["left"]) / 2.0, 3)
    facts["covers_aim"] = blocked
    return facts


if R.bpy is not None:
    bpy = R.bpy

    def main():
        args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else ["fpvcut00"]
        out = R.BASE_OUT + "/" + args[0]
        which = args[1] if len(args) > 1 else ""
        variants, slipper_on = SETS[which]
        os.makedirs(out, exist_ok=True)
        for side in ("right", "left"):
            C.CUT[side] = C.cut(side)
        allfacts = []
        for variant in variants:
            facts = measure(variant, C.CUT)
            allfacts.append(facts)
            print(json.dumps(facts))
            fr = frames(variant)
            for slipper in ((False, True) if variant[0] in slipper_on else (False,)):
                R.reset()
                for side in ("right", "left"):
                    C.arm(side, lambda p, f=fr[side]: to_view(p, f), R.RIG_SCALE)
                if slipper:
                    top = min(v[2] for v in C.CUT["right"][0] if v[1] > FIST_Y[0])      # the -Z face: the body arm's top
                    centre = (0.0, FIST_MID + 0.02, top - 0.5 * SLIPPER[2])
                    corners = [(sx, sy, sz) for sx in (-0.5, 0.5) for sy in (-0.5, 0.5) for sz in (-0.5, 0.5)]
                    quads = [(0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)]
                    pts = [to_view(tuple(centre[a] + k[a] * SLIPPER[a] for a in range(3)), fr["right"]) for k in corners]
                    R.mesh_object("slipper", pts, quads, R.toon_material("slipper", colour=(0.93, 0.22, 0.30)),
                                  outline=R.OUTLINE_NEW * R.RIG_SCALE, recalc=True)
                R.aim((0, 0, 0), (0, 0, -1), (0, 1, 0), fov=R.FOV, res=(1920, 1080))
                R.render("%s/place_%s%s.png" % (out, variant[0], "_slipper" if slipper else ""))
        with open(out + "/placement%s.json" % which, "w") as handle:
            json.dump(allfacts, handle, indent=1)

    if __name__ == "__main__":
        main()
elif __name__ == "__main__":
    from PIL import Image, ImageDraw, ImageFont

    V = sys.argv[1]
    WHICH = sys.argv[2] if len(sys.argv) > 2 else ""
    VARIANTS, SLIPPER_ON = SETS[WHICH]
    TAG = "placement_" + WHICH if WHICH else "placement"
    F = "%s/%s/" % (R.BASE_OUT, V)
    BG = (34, 36, 44)
    facts = {f["name"]: f for f in json.load(open(F + "placement%s.json" % WHICH))}

    def font(size):
        try:
            return ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", size)
        except OSError:
            return ImageFont.load_default()

    def lines_for(f):
        r, l = f["right"], f["left"]
        return [
            "%s  %s" % (f["name"], f["what"]),
            "fist %.0f%% of screen height (its top %.0f%% down the screen), gap %.0f%% of width, arm on screen: right %.0f%%, left %.0f%%%s"
            % (100 * r["fist_height"], 100 * r["fist_top"], 100 * f["gap"], 100 * r["arm_on_screen"], 100 * l["arm_on_screen"],
               "" if not f["covers_aim"] else "   COVERS THE AIM AREA"),
            "about the fist: yaw %s in, pitch %s away, roll %s" % (f["yaw"], f["pitch"], f["roll"]),
            "on screen (share of each piece, along the arm's centre line): right " + ", ".join("%s %.0f%%" % (n, 100 * v) for n, v in r["pieces"].items())
            + "; left " + ", ".join("%s %.0f%%" % (n, 100 * v) for n, v in l["pieces"].items()),
            "RightPivot pos %s  (move %s)" % (tuple(r["position"]), tuple(r["move"])),
            "  x %s  y %s  z %s" % (tuple(r["x"]), tuple(r["y"]), tuple(r["z"])),
            "LeftPivot  pos %s  (move %s)" % (tuple(l["position"]), tuple(l["move"])),
            "  x %s  y %s  z %s" % (tuple(l["x"]), tuple(l["y"]), tuple(l["z"])),
        ]

    def tile(name, text, width=1280):
        img = Image.open(F + name).convert("RGB")
        w, h = img.size
        d = ImageDraw.Draw(img)
        # the crosshair, and the area that must stay clear: the middle third above the lower quarter
        cx, cy = w // 2, h // 2
        d.line([cx - 14, cy, cx + 14, cy], fill=(255, 255, 255), width=3)
        d.line([cx, cy - 14, cx, cy + 14], fill=(255, 255, 255), width=3)
        d.rectangle([w / 3, 0, 2 * w / 3, 0.75 * h], outline=(255, 255, 255, 90), width=1)
        img = img.resize((width, int(h * width / w)), Image.LANCZOS)
        d = ImageDraw.Draw(img)
        f = font(15)
        y = 6
        for i, line in enumerate(text):
            ff = font(19) if i == 0 else f
            box = d.textbbox((0, 0), line, font=ff)
            d.rectangle([0, y - 2, box[2] + 16, y + box[3] + 4], fill=BG)
            d.text((8, y), line, font=ff, fill=(236, 238, 244))
            y += box[3] + 7
        return img

    def stack(rows, path, gap=8):
        heights = [max(t.height for t in row) for row in rows]
        widths = [sum(t.width for t in row) + gap * (len(row) - 1) for row in rows]
        sheet = Image.new("RGB", (max(widths) + 2 * gap, sum(heights) + gap * (len(rows) + 1)), BG)
        y = gap
        for row, h in zip(rows, heights):
            x = gap
            for t in row:
                sheet.paste(t, (x, y)); x += t.width + gap
            y += h + gap
        sheet.save(path)
        print("WROTE", path)

    names = [v[0] for v in VARIANTS]
    tiles = [tile("place_%s.png" % n, lines_for(facts[n])) for n in names]
    stack([tiles[i:i + 2] for i in range(0, len(tiles), 2)], "%s/%s_%s_variants.png" % (R.BASE_OUT, V, TAG))
    if SLIPPER_ON:
      stack([[tile("place_%s_slipper.png" % n, ["%s with a tsinelas-sized block (0.26 x 0.10 x 0.03) on the right fist's top face" % n])
            for n in SLIPPER_ON]], "%s/%s_%s_slipper.png" % (R.BASE_OUT, V, TAG))
    with open("%s/%s_%s_numbers.txt" % (R.BASE_OUT, V, TAG), "w") as handle:
        for n in names:
            handle.write("\n".join(lines_for(facts[n])) + "\n")
            handle.write("  RightPivot quaternion (x, y, z, w) %s   LeftPivot %s\n\n" % (tuple(facts[n]["right"]["quaternion"]), tuple(facts[n]["left"]["quaternion"])))
    print(open("%s/%s_%s_numbers.txt" % (R.BASE_OUT, V, TAG)).read())
