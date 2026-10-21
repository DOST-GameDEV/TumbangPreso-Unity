"""The small modelled things of Ilyas's ultimate cutscene (BAHA), each typed part by part and PAINTED: the LEPA boat on the
crest of his wall, the reef FISH the tide leaves on the sand, the loose TSINELAS the flood brings him, and the STILT
HOUSES on his far water. (His shark has its own builder, `tools/build_ultimate_shark.py`.)

    py -3 tools/build_ultimate_props.py [lepa] [fish] [tsinelas] [stilthouse] [--out=<folder>]

⚠️ REBUILT 2026-10-10 IN THE PAINTED STYLE. These four were first built as faceted prisms in flat palette colours
(`build_paete_props.py`'s boxes), which is the style the owner refused for the shark that stood beside them: "doesn't fit
the hand painted illustrated stylistic art design of our characters and overall game style ... need you to read up on how
we modeled the fpv hand pets". So they are built the way the hand companions and Paete's plants are
(`tools/hand_companion_kit.py`): round forms, smooth normals, and a PAINTED ATLAS beside the models. They share one atlas
(`seaprops-atlas.png`): planked wood, dark wet wood, thatch, woven palm wall, striped sailcloth, the fish's skin, the
slipper's rubber and its footbed. Small trims (an eye, a rail, a lashing) stay flat palette cells.

⚠️ EVERY PART IS TYPED, NOT STAMPED (owner, standing: "manually do each part of that builder dont js auto generate looping
shit"). The left of a thing is typed and the right is its mirror.

The flat cells are `UltimateProp.SeaPalette` in `Runtime/Visual/UltimateProp.cs`, in the order below.
The fish is the one the owner approved as Ilyas's hand companion (`tools/build_hand_rafi.py`): fat, orange, a cream
belly, two pale bands, a coral crest.

Output: Assets/TumbangPreso/Resources/Models/UltimateProps/{lepa,fish,tsinelas,stilthouse}.glb, metres, +y up, front +z.
"""
import math
import sys
from pathlib import Path

import numpy as np

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import hand_companion_kit as kit  # noqa: E402
from hand_companion_kit import ellipsoid, grid_uv, lathe, mirror, moved, rounded, tube  # noqa: E402

# The sixteen flat colours: `UltimateProp.SeaPalette`.
PALETTE = ["#4F7391", "#36536C", "#F1ECE2", "#C8324A", "#FFFFFF", "#12161C", "#FFFFFF", "#2B4257",
           "#A56A3C", "#6F4526", "#F4E6C2", "#E0483A", "#2AA6A0", "#C9A36F", "#F2C230", "#20262E"]
EYE, SHINE, WOOD, WOOD_DK, SAIL, RED, TEAL, ROPE, YELLOW, INK = 5, 6, 8, 9, 10, 11, 12, 13, 14, 15

ATLAS_NAME = "seaprops-atlas.png"
ATLAS = 1024
# The paint, in the atlas's upper half (v under 0.5).
R_WOOD, R_DARK, R_THATCH, R_WOVEN = (0.0, 0.0, 0.25, 0.25), (0.25, 0.0, 0.5, 0.25), (0.5, 0.0, 0.75, 0.25), (0.75, 0.0, 1.0, 0.25)
R_SAIL, R_FISH, R_RUBBER, R_FOOTBED = (0.0, 0.25, 0.25, 0.5), (0.25, 0.25, 0.5, 0.5), (0.5, 0.25, 0.75, 0.5), (0.75, 0.25, 1.0, 0.5)


def arg(name, default):
    return next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--" + name + "=")), default)


def inset(rect, pad=0.008):
    return (rect[0] + pad, rect[1] + pad, rect[2] - pad, rect[3] - pad)


def round_uv(mesh, rect, cols, along="rows", axis=1):
    """The kit's own lay: round the piece left to right, along it bottom to top. Call it on the piece as BUILT."""
    return grid_uv(mesh, inset(rect), cols, along, axis)


def flat_uv(mesh, rect, a, b):
    """A piece seen flat-on: its extent along axis `a` is left to right and along `b` bottom to top. For a deck, a roof
    leaf, a sail: round-the-piece UVs pinch at the middle of a big flat face."""
    pos = mesh[0]
    r = inset(rect)
    lo_a, hi_a, lo_b, hi_b = pos[:, a].min(), pos[:, a].max(), pos[:, b].min(), pos[:, b].max()
    u = r[0] + (r[2] - r[0]) * (pos[:, a] - lo_a) / max(1e-6, hi_a - lo_a)
    v = r[3] - (r[3] - r[1]) * (pos[:, b] - lo_b) / max(1e-6, hi_b - lo_b)
    return np.stack([u, v], axis=1).astype(np.float32)


# ------------------------------------------------------------------ the paint

def paint_atlas(path):
    from PIL import Image, ImageDraw, ImageFilter
    picture = Image.new("RGB", (ATLAS, ATLAS), (165, 106, 60))
    d = ImageDraw.Draw(picture, "RGBA")

    def box(rect):
        return [int(rect[0] * ATLAS), int(rect[1] * ATLAS), int(rect[2] * ATLAS), int(rect[3] * ATLAS)]

    def at(rect, u, v):
        x0, y0, x1, y1 = box(rect)
        return x0 + u * (x1 - x0), y0 + (1 - v) * (y1 - y0)

    # PLANKED WOOD: warm, lighter up the middle of each plank, a dark seam between planks, a few drawn grain lines.
    d.rectangle(box(R_WOOD), fill=(168, 108, 62))
    for u in (0.10, 0.30, 0.50, 0.70, 0.90):
        d.line([at(R_WOOD, u, 0), at(R_WOOD, u, 1)], fill=(196, 134, 80, 150), width=22)
    for u in (0.0, 0.20, 0.40, 0.60, 0.80, 1.0):
        d.line([at(R_WOOD, u, 0), at(R_WOOD, u, 1)], fill=(96, 58, 30, 255), width=5)
    for u, v0, v1 in ((0.06, 0.1, 0.5), (0.14, 0.4, 0.95), (0.27, 0.05, 0.6), (0.34, 0.5, 0.9), (0.46, 0.2, 0.75), (0.55, 0.0, 0.4),
                      (0.66, 0.3, 0.85), (0.74, 0.1, 0.5), (0.86, 0.45, 1.0), (0.94, 0.05, 0.55)):
        d.line([at(R_WOOD, u, v0), at(R_WOOD, u + 0.012, (v0 + v1) / 2), at(R_WOOD, u, v1)], fill=(120, 74, 40, 170), width=3, joint="curve")
    # DARK WOOD (piles, posts, the keel): wet at the foot, grain drawn along it, a pale waterline ring.
    d.rectangle(box(R_DARK), fill=(112, 70, 38))
    for u in (0.08, 0.22, 0.37, 0.52, 0.66, 0.81, 0.93):
        d.line([at(R_DARK, u, 0), at(R_DARK, u + 0.02, 0.5), at(R_DARK, u, 1)], fill=(78, 46, 24, 200), width=4, joint="curve")
    for u in (0.15, 0.45, 0.74):
        d.line([at(R_DARK, u, 0.1), at(R_DARK, u, 0.9)], fill=(150, 98, 56, 140), width=6)
    d.rectangle([at(R_DARK, 0, 0.22)[0], at(R_DARK, 0, 0.22)[1], at(R_DARK, 1, 0)[0], at(R_DARK, 1, 0)[1]], fill=(58, 44, 36, 150))
    d.line([at(R_DARK, 0, 0.23), at(R_DARK, 1, 0.23)], fill=(214, 226, 214, 170), width=4)
    # THATCH: courses of nipa, each a shade lighter at its top and dark where it overhangs the next, with drawn straws.
    d.rectangle(box(R_THATCH), fill=(201, 163, 111))
    for k, v in enumerate((0.0, 0.2, 0.4, 0.6, 0.8)):
        x0, y1 = at(R_THATCH, 0, v)
        x1, y0 = at(R_THATCH, 1, v + 0.2)
        d.rectangle([x0, y0, x1, y0 + (y1 - y0) * 0.45], fill=(222, 188, 132, 150))
        d.rectangle([x0, y1 - 7, x1, y1], fill=(120, 84, 46, 235))
        for u in (0.04, 0.11, 0.19, 0.27, 0.34, 0.43, 0.5, 0.58, 0.66, 0.73, 0.82, 0.89, 0.96):
            lean = 0.012 if (k % 2) else -0.012
            d.line([at(R_THATCH, u, v + 0.19), at(R_THATCH, u + lean, v + 0.03)], fill=(150, 110, 62, 170), width=3)
    # WOVEN PALM WALL: cream, a drawn weave of two crossing sets of lines.
    d.rectangle(box(R_WOVEN), fill=(244, 230, 194))
    for k in (-0.8, -0.6, -0.4, -0.2, 0.0, 0.2, 0.4, 0.6, 0.8):
        d.line([at(R_WOVEN, k, 0), at(R_WOVEN, k + 1, 1)], fill=(206, 178, 126, 200), width=5)
        d.line([at(R_WOVEN, k + 1, 0), at(R_WOVEN, k, 1)], fill=(222, 198, 150, 200), width=5)
    for rect in (R_WOVEN,):
        x0, y0, x1, y1 = box(rect)
        picture.paste(picture.crop((x0, y0, x1, y1)), (x0, y0))
    # SAILCLOTH: cream, three stripes in the Sama colours, seams across it.
    d.rectangle(box(R_SAIL), fill=(244, 232, 200))
    for v0, v1, colour in ((0.70, 0.86, (224, 72, 58)), (0.46, 0.62, (42, 166, 160)), (0.22, 0.38, (242, 194, 48))):
        x0, y1 = at(R_SAIL, 0, v0)
        x1, y0 = at(R_SAIL, 1, v1)
        d.rectangle([x0, y0, x1, y1], fill=colour + (255,))
        d.line([(x0, y0 + 4), (x1, y0 + 4)], fill=(255, 255, 255, 90), width=4)
    for u in (0.25, 0.5, 0.75):
        d.line([at(R_SAIL, u, 0), at(R_SAIL, u, 1)], fill=(150, 130, 96, 130), width=3)
    # THE FISH'S SKIN. Left to right is round its body (its belly a quarter of the way across, its back three quarters),
    # bottom to top is tail to nose. Orange; a cream belly; two pale bands; a darker back; a paler face.
    x0, y0, x1, y1 = box(R_FISH)
    d.rectangle([x0, y0, x1, y1], fill=(255, 138, 60))
    d.rectangle([at(R_FISH, 0.60, 0)[0], y0, at(R_FISH, 0.90, 0)[0], y1], fill=(232, 104, 44, 170))                # the back
    d.ellipse([at(R_FISH, 0.07, 0)[0], y0 - 30, at(R_FISH, 0.43, 0)[0], y1 + 30], fill=(255, 226, 176, 255))       # the belly
    for v in (0.36, 0.62):
        # Drawn past both edges of its part of the atlas, so the band has no gap where the skin's two edges meet.
        d.line([at(R_FISH, -0.02, v), at(R_FISH, 1.02, v)], fill=(255, 246, 232, 255), width=20)
        d.line([at(R_FISH, -0.02, v - 0.05), at(R_FISH, 1.02, v - 0.05)], fill=(206, 84, 40, 200), width=4)
    d.rectangle([x0, y0, x1, at(R_FISH, 0, 0.86)[1]], fill=(255, 170, 96, 150))                                    # the face
    # THE SLIPPER'S RUBBER: red, worn pale along its edge, and its FOOTBED: cream, with the dark print of a heel and toes.
    d.rectangle(box(R_RUBBER), fill=(224, 72, 58))
    for v in (0.3, 0.5, 0.7):
        d.line([at(R_RUBBER, 0, v), at(R_RUBBER, 1, v)], fill=(190, 52, 44, 200), width=5)
    d.line([at(R_RUBBER, 0, 0.9), at(R_RUBBER, 1, 0.9)], fill=(246, 150, 130, 220), width=8)
    fx0, fy0, fx1, fy1 = box(R_FOOTBED)
    d.rectangle([fx0, fy0, fx1, fy1], fill=(244, 230, 194))
    d.ellipse([at(R_FOOTBED, 0.30, 0)[0], at(R_FOOTBED, 0, 0.30)[1], at(R_FOOTBED, 0.70, 0)[0], at(R_FOOTBED, 0, 0.06)[1]], fill=(214, 190, 150, 255))  # heel
    d.ellipse([at(R_FOOTBED, 0.24, 0)[0], at(R_FOOTBED, 0, 0.86)[1], at(R_FOOTBED, 0.78, 0)[0], at(R_FOOTBED, 0, 0.56)[1]], fill=(222, 200, 160, 255))  # ball
    for u, v in ((0.26, 0.90), (0.40, 0.94), (0.54, 0.94), (0.67, 0.91), (0.77, 0.86)):
        cx, cy = at(R_FOOTBED, u, v)
        d.ellipse([cx - 9, cy - 9, cx + 9, cy + 9], fill=(214, 190, 150, 255))
    for k in (0.15, 0.3, 0.45):
        d.line([at(R_FOOTBED, 0.1, k), at(R_FOOTBED, 0.9, k + 0.02)], fill=(230, 212, 172, 255), width=3)
    picture = picture.filter(ImageFilter.GaussianBlur(0.7))
    picture.save(path)
    return path


def model(name, folder):
    m = kit.Model(name)
    m.folder = folder
    m.texture = ATLAS_NAME
    return m


def painted(mesh, rect, cols, along="rows", axis=1, at=(0, 0, 0), turn=(0, 0, 0), scale=(1, 1, 1)):
    """One piece painted round itself, then put in its place."""
    uv = round_uv(mesh, rect, cols, along, axis)
    return (moved(mesh, at=at, turn=turn, scale=scale), uv)


def flat(mesh, rect, a, b, at=(0, 0, 0), turn=(0, 0, 0)):
    """One piece painted flat-on (see `flat_uv`), then put in its place."""
    uv = flat_uv(mesh, rect, a, b)
    return (moved(mesh, at=at, turn=turn), uv)


def post(a, b, radius, rect=R_DARK, seg=8):
    """A round timber from a to b, its paint running along it."""
    mesh = tube([a, ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2, (a[2] + b[2]) / 2), b], radius, seg=seg)
    return (mesh, round_uv(mesh, rect, seg))


# ------------------------------------------------------------------ the fish

def fish(folder):
    """A reef fish left on the sand, and the ones that swim past under water: the fish the owner approved for Ilyas's
    hand, at half a metre so it reads from the high shot. Fat, orange, a cream belly, two pale bands, a coral crest,
    big eyes. Its nose is on +z; laid on its side by the game when it is stranded. The tail is its own node."""
    m = model("fish", folder)
    # The body: turned about its long axis, fat ahead of its middle, built standing (nose up) and laid along +z.
    profile = [(0.0, -0.185), (0.030, -0.165), (0.058, -0.110), (0.090, -0.030), (0.104, 0.050), (0.098, 0.130), (0.072, 0.200), (0.036, 0.238),
               (0.0, 0.250)]
    body = lathe(profile, seg=18, squash_z=1.18)
    pieces = [painted(body, R_FISH, 18, along="axis", axis=1, turn=(90, 0, 0))]
    # The crest down its back and the small fin under it: coral leaves, thin.
    crest = lathe([(0.0, 0.0), (0.045, 0.012), (0.074, 0.04), (0.085, 0.075), (0.074, 0.108), (0.045, 0.134), (0.0, 0.145)], seg=20, squash_z=0.22)
    pieces.append((moved(crest, turn=(0, 90, 0), at=(0.0, 0.085, 0.01), scale=(1, 1, 1.5)), RED))
    under = lathe([(0.0, 0.0), (0.028, 0.010), (0.043, 0.035), (0.040, 0.062), (0.022, 0.082), (0.0, 0.09)], seg=18, squash_z=0.22)
    pieces.append((moved(under, turn=(180, 90, 0), at=(0.0, -0.085, -0.03)), RED))
    # Its side paddles, out and back with their flat sides showing: the right one typed, the left its mirror.
    paddle = moved(lathe([(0.0, 0.0), (0.024, 0.010), (0.036, 0.034), (0.034, 0.058), (0.018, 0.078), (0.0, 0.085)], seg=18, squash_z=0.25), turn=(-60, 0, -60), at=(0.084, -0.030, 0.090))
    pieces += [(paddle, RED), (mirror(paddle), RED)]
    # Big eyes, bulging off the sides and turned half forward: white, a dark pupil, one glint.
    white = ellipsoid((0.022, 0.036, 0.036), at=(0.078, 0.030, 0.158))
    pupil = ellipsoid((0.012, 0.022, 0.022), at=(0.092, 0.030, 0.166))
    glint = ellipsoid((0.006, 0.008, 0.008), at=(0.101, 0.040, 0.172), seg=8, rings=5)
    pieces += [(white, SHINE), (mirror(white), SHINE), (pupil, EYE), (mirror(pupil), EYE), (glint, SHINE), (mirror(glint), SHINE)]
    # Its mouth: a small round o.
    pieces.append((moved(lathe([(0.006, 0.0), (0.012, 0.004), (0.009, 0.009), (0.0, 0.008)], seg=12), turn=(90, 0, 0), at=(0.0, -0.014, 0.246)), RED))
    m.add("body", pieces)
    # The tail: two rounded lobes about its root.
    lobe = lathe([(0.0, 0.0), (0.030, 0.018), (0.050, 0.055), (0.058, 0.095), (0.050, 0.135), (0.030, 0.162), (0.0, 0.175)], seg=20, squash_z=0.2)
    upper = moved(lobe, turn=(0, 90, 0))
    upper = moved(upper, turn=(-118, 0, 0), at=(0.0, 0.0, 0.01))
    lower = moved(lobe, turn=(0, 90, 0))
    lower = moved(lower, turn=(-62, 0, 0), scale=(1, -1, 1), at=(0.0, 0.0, 0.01))
    lower = (lower[0], lower[1], lower[2][:, ::-1].copy())
    m.add("tail", [(upper, RED), (lower, RED)], at=(0.0, 0.0, -0.165))
    m.write(PALETTE)


# ------------------------------------------------------------------ the slipper

def tsinelas(folder):
    """The loose slipper the flood brings to his feet ("Leaves with his slipper"): a rubber tsinelas, 0.3 m, its toe on
    +z, lying on its sole. A red sole worn pale at its edge, a cream footbed with the print of a foot, a teal strap."""
    m = model("tsinelas", folder)
    sole = rounded((0.062, 0.017, 0.150), 0.017, seg=20, rings=10)
    # A foot's outline, not a bar: wider at the ball, narrower at the heel.
    p = sole[0].copy()
    z = p[:, 2] / 0.150
    p[:, 0] *= (1.0 + 0.14 * np.clip(z, -1, 1) - 0.10 * z * z) * np.sqrt(np.clip(1 - 0.80 * np.abs(z) ** 3.4, 0.05, 1))
    sole = (p, sole[1], sole[2])
    bed = rounded((0.052, 0.006, 0.138), 0.006, seg=20, rings=8)
    q = bed[0].copy()
    zb = q[:, 2] / 0.138
    q[:, 0] *= (1.0 + 0.14 * np.clip(zb, -1, 1) - 0.10 * zb * zb) * np.sqrt(np.clip(1 - 0.80 * np.abs(zb) ** 3.4, 0.05, 1))
    bed = (q, bed[1], bed[2])
    pieces = [painted(sole, R_RUBBER, 20, at=(0.0, 0.017, 0.0)), flat(bed, R_FOOTBED, 0, 2, at=(0.0, 0.036, 0.0))]
    # The strap: from each side of the arch up over the foot to the toe post. The right arm is typed, the left its mirror.
    arm = tube([(0.060, 0.034, -0.005), (0.050, 0.066, 0.030), (0.026, 0.078, 0.066), (0.0, 0.058, 0.094)], [0.011, 0.012, 0.011, 0.010], seg=8)
    pieces += [(arm, TEAL), (mirror(arm), TEAL)]
    pieces.append((tube([(0.0, 0.030, 0.094), (0.0, 0.060, 0.094)], 0.010, seg=8), TEAL))
    m.add("shoe", pieces)
    m.write(PALETTE)


# ------------------------------------------------------------------ the boat

def lepa(folder):
    """The little boat on the crest: a Sama Dilaut lepa in miniature, 2.2 m long. A round planked hull rising to a prow
    and a stern, bamboo outriggers on two booms, a low thatched house aft, one mast and a square sail in three stripes
    that fills with wind. Its middle is at the origin and its keel at y = 0, so it sits ON the water it is placed on."""
    m = model("lepa", folder)
    # The hull: one round timber shape along z, deepest amidships, lifting and narrowing to both ends, the prow higher.
    hull = tube([(0.0, 0.34, -1.08), (0.0, 0.20, -0.80), (0.0, 0.15, -0.35), (0.0, 0.14, 0.10), (0.0, 0.17, 0.55), (0.0, 0.27, 0.88), (0.0, 0.46, 1.12)],
                [0.07, 0.17, 0.25, 0.27, 0.23, 0.15, 0.06], seg=16)
    p = hull[0].copy()
    p[:, 1] = np.where(p[:, 1] > 0.27, 0.27 + (p[:, 1] - 0.27) * 0.25, p[:, 1])      # its top is cut level: a boat is open
    keep = hull[0][:, 2] > 0.80                                                      # ... but not at the prow, which stands up
    p[keep] = hull[0][keep]
    hull = (p, hull[1], hull[2])
    pieces = [(hull, round_uv(hull, R_WOOD, 16))]
    # The inside of it: a dark well, so it reads as hollow.
    pieces.append((ellipsoid((0.19, 0.03, 0.72), at=(0.0, 0.262, -0.05), seg=14, rings=6), INK))
    # The painted rails along each side: teal over yellow. The right pair typed, the left their mirror.
    rail = tube([(0.19, 0.275, -0.72), (0.268, 0.270, -0.20), (0.262, 0.270, 0.30), (0.17, 0.285, 0.78)], 0.022, seg=8)
    band = tube([(0.20, 0.235, -0.70), (0.280, 0.225, -0.20), (0.274, 0.225, 0.30), (0.18, 0.245, 0.76)], 0.014, seg=8)
    pieces += [(rail, TEAL), (mirror(rail), TEAL), (band, YELLOW), (mirror(band), YELLOW)]
    # Its carved head, painted red, and a yellow eye on each side of it.
    pieces.append((rounded((0.055, 0.075, 0.075), 0.045, at=(0.0, 0.52, 1.13)), RED))
    eye = ellipsoid((0.012, 0.022, 0.022), at=(0.052, 0.535, 1.14))
    pieces += [(eye, YELLOW), (mirror(eye), YELLOW)]
    # The outriggers: two booms across, and a bamboo float each side with its lashings.
    pieces.append(post((-0.95, 0.22, 0.44), (0.95, 0.22, 0.44), [0.022, 0.028, 0.022]))
    pieces.append(post((-0.95, 0.22, -0.42), (0.95, 0.22, -0.42), [0.022, 0.028, 0.022]))
    float_r = tube([(0.95, 0.12, -0.80), (0.95, 0.08, -0.25), (0.95, 0.08, 0.30), (0.95, 0.14, 0.88)], [0.040, 0.052, 0.052, 0.032], seg=10)
    pieces += [(float_r, ROPE), (mirror(float_r), ROPE)]
    lash_a = rounded((0.045, 0.060, 0.045), 0.03, at=(0.95, 0.17, 0.44))
    lash_b = rounded((0.045, 0.060, 0.045), 0.03, at=(0.95, 0.17, -0.42))
    pieces += [(lash_a, INK), (mirror(lash_a), INK), (lash_b, INK), (mirror(lash_b), INK)]
    # The house aft: four posts and a low round roof of thatch.
    pieces += [post((0.19, 0.24, -0.20), (0.19, 0.56, -0.20), 0.018), post((-0.19, 0.24, -0.20), (-0.19, 0.56, -0.20), 0.018),
               post((0.19, 0.24, -0.66), (0.19, 0.54, -0.66), 0.018), post((-0.19, 0.24, -0.66), (-0.19, 0.54, -0.66), 0.018)]
    roof = rounded((0.27, 0.085, 0.33), 0.085, seg=18, rings=10)
    r = roof[0].copy()
    r[:, 1] -= 0.55 * r[:, 0] ** 2                                                    # bowed down to its eaves
    roof = (r, roof[1], roof[2])
    pieces.append(flat(roof, R_THATCH, 2, 0, at=(0.0, 0.615, -0.43)))
    m.add("hull", pieces)

    # The mast and its sail are one node, so the sail can swing on the mast.
    rig = [post((0.0, 0.0, 0.0), (0.0, 1.55, 0.03), [0.032, 0.026, 0.018], R_WOOD)]
    rig.append((tube([(0.0, 1.42, -0.42), (0.0, 1.44, 0.03), (0.0, 1.42, 0.48)], [0.016, 0.020, 0.016], seg=8), ROPE))   # the yard
    rig.append((tube([(0.0, 0.52, -0.40), (0.0, 0.50, 0.03), (0.0, 0.52, 0.46)], [0.016, 0.020, 0.016], seg=8), ROPE))   # the boom
    sail = rounded((0.016, 0.44, 0.41), 0.014, seg=20, rings=12)
    s = sail[0].copy()
    s[:, 0] += 0.10 * (1 - (s[:, 1] / 0.44) ** 2) * (1 - (s[:, 2] / 0.41) ** 2)       # it holds the wind
    sail = (s, sail[1], sail[2])
    rig.append(flat(sail, R_SAIL, 2, 1, at=(0.02, 0.97, 0.03)))
    rig.append((rounded((0.012, 0.045, 0.085), 0.012, at=(0.0, 1.60, 0.10)), RED))                                      # the pennant
    m.add("rig", rig, at=(0.0, 0.24, 0.20))
    m.write(PALETTE)


# ------------------------------------------------------------------ the stilt house

def stilthouse(folder):
    """A Sama stilt house on the far water, 3.7 m to its ridge: six round piles, a planked deck with a rail, woven walls,
    a door and a window, a thatched roof of two leaves and a ladder to the water. Its door faces +z; y = 0 is the water.
    ⚠️ It stands. Fixed houses are on piles and do not bob; only boats float (the owner's lagoon correction, AGENTS.md)."""
    m = model("stilthouse", folder)
    pieces = []
    # Six piles, typed: front pair, middle pair, back pair, none of them quite plumb, each dark and wet at its foot.
    pieces += [post((0.66, -0.05, 0.54), (0.62, 2.04, 0.50), 0.062), post((-0.66, -0.05, 0.54), (-0.62, 2.04, 0.50), 0.062),
               post((0.66, -0.05, 0.0), (0.64, 2.04, 0.0), 0.056), post((-0.66, -0.05, 0.0), (-0.64, 2.04, 0.0), 0.056),
               post((0.60, -0.05, -0.54), (0.62, 2.04, -0.50), 0.062), post((-0.64, -0.05, -0.56), (-0.62, 2.04, -0.50), 0.062)]
    # Two cross braces under the deck.
    pieces += [post((-0.62, 0.60, 0.50), (0.62, 1.70, 0.50), 0.026), post((0.62, 0.60, -0.50), (-0.62, 1.70, -0.50), 0.026)]
    # The deck, wider than the house in front: a porch. Planked.
    deck = rounded((0.82, 0.055, 0.78), 0.04, seg=20, rings=8)
    pieces.append(flat(deck, R_WOOD, 0, 2, at=(0.0, 2.05, 0.10)))
    # The porch rail: two posts and a bar.
    pieces += [post((0.74, 2.08, 0.80), (0.74, 2.56, 0.80), 0.026), post((-0.74, 2.08, 0.80), (-0.74, 2.56, 0.80), 0.026),
               post((0.10, 2.52, 0.80), (0.78, 2.52, 0.80), 0.022)]
    # The walls: woven palm, one soft-cornered room, with a darker band at its foot and at its head.
    walls = rounded((0.67, 0.43, 0.50), 0.07, seg=24, rings=10)
    pieces.append(painted(walls, R_WOVEN, 24, at=(0.0, 2.52, -0.12)))
    pieces.append((rounded((0.69, 0.05, 0.52), 0.04, at=(0.0, 2.14, -0.12), seg=20, rings=6), ROPE))
    pieces.append((rounded((0.69, 0.04, 0.52), 0.035, at=(0.0, 2.92, -0.12), seg=20, rings=6), ROPE))
    # A door and a window on the front, and one window on each side (the right typed, the left its mirror).
    door = rounded((0.17, 0.33, 0.02), 0.03, seg=14, rings=8)
    pieces.append(flat(door, R_DARK, 0, 1, at=(-0.24, 2.47, 0.385)))
    pieces.append((rounded((0.21, 0.03, 0.025), 0.02, at=(-0.24, 2.82, 0.392)), RED))
    pieces.append((rounded((0.13, 0.13, 0.018), 0.03, at=(0.34, 2.60, 0.386)), INK))
    pieces.append((rounded((0.16, 0.022, 0.022), 0.018, at=(0.34, 2.60, 0.398)), TEAL))
    pieces.append((rounded((0.022, 0.16, 0.022), 0.018, at=(0.34, 2.60, 0.398)), TEAL))
    side = rounded((0.018, 0.12, 0.15), 0.03, at=(0.675, 2.58, -0.12))
    pieces += [(side, INK), (mirror(side), INK)]
    # The roof: two thatch leaves, each a soft slab, the eaves past the walls; and a ridge pole.
    leaf = rounded((0.56, 0.06, 0.80), 0.05, seg=20, rings=8)
    right = flat(leaf, R_THATCH, 2, 0, at=(0.42, 3.23, -0.12), turn=(0, 0, -33))
    left_mesh = mirror(leaf)
    left = (moved(left_mesh, at=(-0.42, 3.23, -0.12), turn=(0, 0, 33)), flat_uv(left_mesh, R_THATCH, 2, 0))
    # Both leaves' courses must run DOWN the slope, so the left one's paint is turned over.
    luv = left[1].copy()
    r = inset(R_THATCH)
    luv[:, 1] = r[1] + r[3] - luv[:, 1]
    pieces += [right, (left[0], luv)]
    pieces.append(post((0.0, 3.56, -0.94), (0.0, 3.56, 0.70), 0.05))
    pieces.append((rounded((0.035, 0.12, 0.035), 0.03, at=(0.0, 3.68, 0.64)), RED))                                    # a carved finial, front
    # The ladder down to the water: two rails and four rungs.
    pieces += [post((0.36, 0.05, 1.30), (0.36, 2.06, 0.86), 0.024), post((0.70, 0.05, 1.30), (0.70, 2.06, 0.86), 0.024)]
    pieces += [post((0.34, 0.48, 1.215), (0.72, 0.48, 1.215), 0.02, R_WOOD), post((0.34, 0.90, 1.120), (0.72, 0.90, 1.120), 0.02, R_WOOD),
               post((0.34, 1.32, 1.025), (0.72, 1.32, 1.025), 0.02, R_WOOD), post((0.34, 1.74, 0.930), (0.72, 1.74, 0.930), 0.02, R_WOOD)]
    m.add("house", pieces)
    m.write(PALETTE)


if __name__ == "__main__":
    out = Path(arg("out", kit.ROOT / "Assets/TumbangPreso/Resources/Models/UltimateProps"))
    out.mkdir(parents=True, exist_ok=True)
    paint_atlas(out / ATLAS_NAME)
    which = {a for a in sys.argv[1:] if not a.startswith("--")} or {"lepa", "fish", "tsinelas", "stilthouse"}
    for name, build in (("lepa", lepa), ("fish", fish), ("tsinelas", tsinelas), ("stilthouse", stilthouse)):
        if name in which:
            build(out)
