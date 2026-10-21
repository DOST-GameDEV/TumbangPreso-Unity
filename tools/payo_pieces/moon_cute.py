"""PAYO NIGHT: BUWAN, THE CUTE MOON, AND MAYARI OF THE STARS (2026-10-19).

Owner, 2026-10-19: "the mayari could use a rework. take a look at the godess in the paete ult, i want something like
that but moon base. maybe instead of her showing at all times, do an easter egg similar to the balloon slipper in Arena
where we have a cartoony moon with a cute face, hit it enough times and mayari appears in the sky, looking like moving
stars, + a meteor/falling star shower".

WHAT THIS WRITES (one source for the game and the review; the game reads exactly what Blender shows):
  BUWAN     the moon: a plump round moon with craters, big glossy eyes, blush, a tiny mouth, stubby arms, Mayari's
            little gold crown tipped on its head (the signature prop: it is wearing the goddess's crown), resting on a
            puffy cloud. Seven faces (one shown at a time) and the bits that belong to a face (sleepy zzz, dizzy stars).
  MAYARI    the goddess as a constellation: faceted 3D star beads at her joints, thin star lines between them that
            draw themselves in, three streams of drifting star motes for her hair, a gold crescent halo with a ring of
            dots, star dust falling off her hem. She leans over the moon and cups it in both hands (in the old story the
            moon is Mayari's lost eye; the art's variant A had her peeking with the moon as that eye).
  METEORS   a shower of 28 falling stars, each a faceted star head with a tapered glowing tail and three sparks.
  COMET     the thrown slipper's drawn flight up to the moon (head and a crossed tail, one unit long).
  BURST     the hit: star beads and five chunky gold stars flying off the impact point.
  GLOW      the moon's soft glow disc and the reveal's ring pulse.
Every moving part is moved in the game's shaders (`PayoMoon.shader`, `PayoMoonSky.shader`) from numbers baked per
vertex; nothing on the CPU touches a vertex. Typed rows, one per star / line / meteor (the hero-kit rule: no stamped
shapes); only the hair motes and hem dust are spread along typed curves.

AUTHORING SPACE: units of the MOON'S RADIUS (the game scales the whole sky piece by `PayoMoon.Radius`, 200 m), y up,
+z toward the court (the moon's face). The moon's centre is the origin; Mayari stands in a plane 1.2 behind it.

  PY=/c/Users/StarX/AppData/Local/Python/pythoncore-3.14-64/python.exe   (always -I)
  B="/c/Program Files/Blender Foundation/Blender 5.0/blender.exe"
  $PY -I tools/payo_pieces/moon_cute.py unity      the atlas + six baked files into Resources/Map/PayoMoon/
  "$B" -b --python tools/payo_pieces/moon_cute.py -- render <ABS_ROOT>    review shots into Logs/payo/pieces/moon/shots
  $PY -I tools/payo_pieces/moon_cute.py sheet vN   Logs/payo/pieces/moon/moon_vN.png (faces) and mayari_vN.png

BAKED FILE ("KNL1", the Kanlit format): int vertices, int indices, float3 position, float3 normal, float2 uv, float4 A,
float4 B, int32 indices. Unity space (glTF x flipped, the winding with it). What A, B, the normal and the uv mean per part
is in the two shaders' headers and in `bake_*` below.
"""
import json
import math
import os
import struct
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
OUTD = os.path.join(ROOT, "Logs", "payo", "pieces", "moon")
SHOTS = os.path.join(OUTD, "shots")
# `stage` writes the baked files beside the review (Logs/payo/pieces/moon/bake) for a look before they are installed;
# `unity` writes them into the game
UNITY_OUT = os.path.join(ROOT, "Assets", "TumbangPreso", "Resources", "Map", "PayoMoon")
STAGE_OUT = os.path.join(ROOT, "Logs", "payo", "pieces", "moon", "bake")
ATLAS = "moon_atlas.png"
FACES = ["rest", "worried", "ouch", "dizzy", "wow", "happy", "sleepy"]   # part 10 + index

try:
    import bpy
    INBL = True
except ImportError:
    INBL = False


def read_knl(path):
    """The baked file back: P, N, UV, A, B (Unity space) and the triangles."""
    import numpy as np
    data = open(path, "rb").read()
    assert data[:4] == b"KNL1", path
    v, i = struct.unpack("<ii", data[4:12])
    at = 12

    def take(n):
        nonlocal at
        arr = np.frombuffer(data, np.float32, n, at)
        at += n * 4
        return arr
    P = take(v * 3).reshape(-1, 3); N = take(v * 3).reshape(-1, 3); UV = take(v * 2).reshape(-1, 2)
    A = take(v * 4).reshape(-1, 4); Bv = take(v * 4).reshape(-1, 4)
    T = np.frombuffer(data, np.int32, i, at).reshape(-1, 3)
    return P, N, UV, A, Bv, T


# =====================================================================================================================
# SYSTEM PYTHON: shapes, paint, bakes
# =====================================================================================================================
if not INBL:
    import numpy as np
    sys.path.insert(0, os.path.join(ROOT, "tools"))
    import hand_companion_kit as kit
    from hand_companion_kit import ellipsoid, lathe, moved, tube, rounded, slab

    # ---------------------------------------------------------------- palette (flat cells, lower half of the atlas)
    PALETTE = ["#2b1a3a", "#ffffff", "#ff8fb0", "#d94a6a", "#f5c242", "#ff6fa8", "#fff1a8", "#e8bd52",
               "#f4f0ff", "#c9c2f0", "#8fdcff", "#b9a3ff", "#ff7c9c", "#d99a2b", "#24183f", "#ffe36e"]
    INK, WHITE, BLUSH, MOUTH, GOLD, GEM, CREAM, CREAM_DK, CLOUD, CLOUD_SH, SWEAT, LAV, TONGUE, GOLD_DK, NAVY, YELLOW = range(16)

    A_PX = 2048

    def R(x0, y0, x1, y1):
        return (x0 / A_PX, y0 / A_PX, x1 / A_PX, y1 / A_PX)

    RG = dict(moon=R(0, 0, 1024, 512), eye=R(1024, 0, 1280, 256), eye_wow=R(1280, 0, 1536, 256),
              cloud=R(1536, 0, 2048, 256), crown=R(1024, 256, 1536, 512), cheek=R(1536, 256, 1792, 512),
              cap=R(1792, 256, 2048, 512))
    PAD = 3 / A_PX

    def inset(rect):
        return (rect[0] + PAD, rect[1] + PAD, rect[2] - PAD, rect[3] - PAD)

    def planar_uv(mesh, rect, axes=(0, 1)):
        p = mesh[0]
        x, y = p[:, axes[0]], p[:, axes[1]]
        u0, v0, u1, v1 = inset(rect)
        u = u0 + (x - x.min()) / max(1e-6, x.max() - x.min()) * (u1 - u0)
        v = v1 - (y - y.min()) / max(1e-6, y.max() - y.min()) * (v1 - v0)
        return np.stack([u, v], 1).astype(np.float32)

    def cell_uv(mesh, slot):
        return np.tile(np.array(kit.cell(slot), np.float32), (len(mesh[0]), 1))

    # ---------------------------------------------------------------- the face kit (laid onto the moon's ellipsoid)
    HEAD = ((0.0, 0.0, 0.0), (1.0, 0.96, 0.92))

    def surf(yaw, pitch):
        c, r = HEAD
        dx = math.sin(math.radians(yaw)) * math.cos(math.radians(pitch))
        dy = math.sin(math.radians(pitch))
        dz = math.cos(math.radians(yaw)) * math.cos(math.radians(pitch))
        p = np.array((c[0] + r[0] * dx, c[1] + r[1] * dy, c[2] + r[2] * dz), np.float32)
        n = np.array((dx / r[0], dy / r[1], dz / r[2]), np.float32)
        return p, n / np.linalg.norm(n)

    def wrap(mesh, yaw, pitch, roll=0.0, lift=0.0):
        """A feature built flat in xy (facing +z) laid onto the moon at (yaw, pitch), curved to its surface."""
        p, nrm, t = moved(mesh, turn=(0, 0, roll))
        p = p.copy()
        Rm = float(np.mean(HEAD[1]))
        p[:, 2] -= (p[:, 0] ** 2 + p[:, 1] ** 2) / (2 * Rm)
        at, n = surf(yaw, pitch)
        x = np.cross(np.array((0, 1, 0), np.float32), n)
        x /= np.linalg.norm(x)
        y = np.cross(n, x)
        M = np.stack([x, y, n], 1)
        return (p @ M.T + at + n * lift).astype(np.float32), (nrm @ M.T).astype(np.float32), t

    def arc(cx, cy, rx, ry, a0, a1, n=12):
        return [(cx + rx * math.cos(math.radians(a)), cy + ry * math.sin(math.radians(a)), 0.0) for a in np.linspace(a0, a1, n)]

    LW = .042                                   # the face's line weight
    EYE_YAW, EYE_PITCH, MOUTH_PITCH = 25.0, 2.0, -23.0
    EW, EH = .25, .30

    def stroke(pts, r, yaw, pitch, slot=INK, roll=0.0):
        m = wrap(tube(pts, r, seg=8), yaw, pitch, roll, lift=r * .55)
        return (m, slot)

    def eye_dome(side, region, scale=1.0, pitch_off=0.0):
        dome = ellipsoid((EW * scale, EH * scale, EW * scale * .42), seg=20, rings=12)
        uv = planar_uv(dome, region)
        return (wrap(dome, side * EYE_YAW, EYE_PITCH + pitch_off, lift=-EW * .08), uv), surf(side * EYE_YAW, EYE_PITCH + pitch_off)[0]

    def smile(width, depth, pitch, r=LW * .8):
        return stroke(arc(0, depth * .2, width, depth, 200, 340, 12), r, 0, pitch)

    def open_mouth(w, h, pitch, tongue=True):
        """A big open "D" smile: a dark half-oval with a pink tongue in its foot."""
        out = []
        # a flat top edge and a round foot, counter-clockwise (the angle runs backwards, so the list is reversed)
        cav = slab([(w * math.cos(a), h * .15 + h * min(0.0, math.sin(a))) for a in np.linspace(.12, -math.pi - .12, 18)][::-1], .02)
        out.append((wrap(cav, 0, pitch, lift=.01), INK))
        if tongue:
            tg = ellipsoid((w * .55, h * .32, .015), at=(0, -h * .62, .014), seg=12, rings=6)
            out.append((wrap(tg, 0, pitch, lift=.01), TONGUE))
        return out

    def faces():
        """Each face is (pieces, eye centres for the blink). Every piece is in the moon's own space."""
        out = {}
        # REST: big glossy eyes, a tiny content smile.
        p, centres = [], []
        for side in (-1, 1):
            piece, c = eye_dome(side, RG["eye"])
            p.append(piece); centres.append(c)
        p.append(smile(.075, .055, MOUTH_PITCH))
        out["rest"] = (p, centres)
        # WORRIED (three hits and more): the same eyes a touch smaller, brows up in the middle, a wobbly mouth, a sweat drop.
        p, centres = [], []
        for side in (-1, 1):
            piece, c = eye_dome(side, RG["eye"], .92)
            p.append(piece); centres.append(c)
            brow = [(-EW * .75 * side, EW * .5, 0), (0, EW * .3, 0), (EW * .7 * side, EW * .06, 0)]
            p.append(stroke(brow, LW * .75, side * (EYE_YAW + 1), EYE_PITCH + 22))
        p.append(stroke([(-.09, 0, 0), (-.045, .022, 0), (0, 0, 0), (.045, .022, 0), (.09, 0, 0)], LW * .7, 0, MOUTH_PITCH))
        drop = lathe([(0.0, 0.0), (.05, .035), (.055, .07), (.03, .11), (0.0, .16)], seg=10)
        p.append((wrap(moved(drop, scale=(1, 1, .5), at=(0, -.08, 0)), 50, 26, roll=-15, lift=.02), SWEAT))
        out["worried"] = (p, centres)
        # OUCH: squeezed > < eyes, an open "o" of a mouth, two tear sparks.
        p = []
        for side in (-1, 1):
            chev = [(-EW * .75 * side, EW * .6, 0), (EW * .5 * side, 0, 0), (-EW * .75 * side, -EW * .6, 0)]
            p.append(stroke(chev, LW * 1.2, side * EYE_YAW, EYE_PITCH))
            p.append((wrap(ellipsoid((.035, .05, .015)), side * (EYE_YAW + 17), EYE_PITCH - 8, lift=.02), SWEAT))
        o = ellipsoid((.085, .1, .03), seg=12, rings=8)
        p.append((wrap(o, 0, MOUTH_PITCH - 2, lift=.012), INK))
        p.append((wrap(ellipsoid((.05, .055, .02), at=(0, -.02, .022), seg=10, rings=6), 0, MOUTH_PITCH - 2, lift=.012), TONGUE))
        out["ouch"] = (p, [])
        # DIZZY (the fourth hit, and the fifth before she comes): spiral eyes, a wavy mouth. (Its orbiting stars are part 18.)
        p = []
        for side in (-1, 1):
            sp = [(EW * .75 * (k / 26) * math.cos(k * .62 * side), EW * .75 * (k / 26) * math.sin(k * .62 * side), 0) for k in range(1, 27)]
            p.append(stroke(sp, LW * .7, side * EYE_YAW, EYE_PITCH))
        p.append(stroke([(-.11, 0, 0), (-.055, .03, 0), (0, -.01, 0), (.055, .03, 0), (.11, 0, 0)], LW * .75, 0, MOUTH_PITCH))
        out["dizzy"] = (p, [])
        # WOW (as Mayari forms): star-shine eyes, wide open, a big round "o" smile.
        p, centres = [], []
        for side in (-1, 1):
            piece, c = eye_dome(side, RG["eye_wow"], 1.08, 1.5)
            p.append(piece); centres.append(c)
        p += open_mouth(.11, .12, MOUTH_PITCH - 1)
        out["wow"] = (p, centres)
        # HAPPY (with her): ^ ^ eyes, a big open smile.
        p = []
        for side in (-1, 1):
            p.append(stroke(arc(0, -EW * .3, EW * .78, EW * .85, 15, 165, 14), LW * 1.15, side * EYE_YAW, EYE_PITCH))
        p += open_mouth(.13, .12, MOUTH_PITCH)
        out["happy"] = (p, [])
        # SLEEPY (after she has gone, while it rests): closed arcs with lashes, a tiny "o". (Its zzz are part 17.)
        p = []
        for side in (-1, 1):
            p.append(stroke(arc(0, EW * .25, EW * .72, EW * .5, 195, 345, 14), LW * 1.0, side * EYE_YAW, EYE_PITCH - 4))
            for a in (215, 270, 325):
                x, y = EW * .72 * math.cos(math.radians(a)), EW * .25 + EW * .5 * math.sin(math.radians(a))
                p.append(stroke([(x, y, 0), (x * 1.1, y - EW * .2, 0)], LW * .5, side * EYE_YAW, EYE_PITCH - 4))
        p.append((wrap(ellipsoid((.04, .045, .02), seg=10, rings=6), 0, MOUTH_PITCH - 2, lift=.012), INK))
        out["sleepy"] = (p, [])
        return out

    def blush():
        b = ellipsoid((.17, .1, .035), seg=14, rings=6)
        return [(wrap(b, side * 45, -15, lift=-.006), BLUSH) for side in (-1, 1)]

    # ---------------------------------------------------------------- BUWAN, piece by piece
    def moon_parts():
        """A list of (name, part id, ink width, glow, pivot, [(mesh, slot-or-uv)])."""
        parts = []
        body = ellipsoid(HEAD[1], seg=56, rings=36)
        parts.append(("body", 0, 1.0, 1.0, (0, 0, 0), [(body, kit.grid_uv(body, inset(RG["moon"]), 56, "axis", 1))]))
        # Three soft crater pads that bump the outline (top left, back right, under the right cheek), a shade darker.
        craters = []
        for yaw, pitch, w in ((-58.0, 52.0, .2), (112.0, 18.0, .26), (70.0, -50.0, .15)):
            pad = ellipsoid((w, w * .82, w * .22), seg=16, rings=8)
            craters.append((wrap(pad, yaw, pitch, lift=-w * .1), CREAM_DK))
            rim = tube([(w * .82 * math.cos(a), w * .66 * math.sin(a), 0) for a in np.linspace(0, 2 * math.pi, 22)], w * .1, seg=6)
            craters.append((wrap(rim, yaw, pitch, lift=w * .02), CREAM))
        parts.append(("craters", 0, .55, 1.0, (0, 0, 0), craters))
        parts.append(("blush", 0, 0.0, .5, (0, 0, 0), blush()))
        # THE CROWN (Mayari's, tipped on its head): a gold band, three rounded points with pink gems, a pearl on top.
        crown = []
        cbase = np.array((-.22, .9, .02), np.float32)
        band = lathe([(.30, 0.0), (.31, .04), (.30, .1), (.285, .12)], seg=28)
        crown.append((band, kit.grid_uv(band, inset(RG["crown"]), 28, "axis", 1)))
        for k, (ang, tall) in enumerate(((-55.0, .2), (0.0, .28), (55.0, .2))):
            a = math.radians(ang + 90)
            tip = lathe([(.085, 0.0), (.09, .05), (.06, tall * .7), (.0, tall)], seg=12)
            tip = moved(tip, at=(math.cos(a) * .24, .09, math.sin(a) * .24))
            crown.append((tip, cell_uv(tip, GOLD)))
            gem = ellipsoid((.045, .05, .03), at=(math.cos(a) * .27, .17, math.sin(a) * .27))
            crown.append((gem, cell_uv(gem, GEM)))
            pearl = ellipsoid((.035, .035, .035), at=(math.cos(a) * .24, .09 + tall + .02, math.sin(a) * .24))
            crown.append((pearl, cell_uv(pearl, WHITE)))
        crown = [(moved(m, turn=(8, -10, -16), at=tuple(cbase)), uv) for m, uv in crown]
        parts.append(("crown", 1, .6, .35, tuple(cbase), crown))
        # STUBBY ARMS: a short round tube to a nub hand, out of the sides just under the middle.
        for k, side in enumerate((-1, 1)):
            sh = np.array((side * .86, -.26, .2), np.float32)
            arm = tube([tuple(sh), tuple(sh + (side * .2, -.08, .07)), tuple(sh + (side * .32, -.18, .12))], [.13, .125, .12], seg=12)
            hand = ellipsoid((.15, .14, .14), at=tuple(sh + (side * .36, -.22, .14)), seg=14, rings=8)
            parts.append(("arm_" + ("l" if side < 0 else "r"), 2 + k, 1.0, 1.0, tuple(sh), [(arm, CREAM), (hand, CREAM)]))
        # THE CLOUD it rests on: seven puffs along the bottom front, lit from above.
        cloud = []
        for x, y, z, r in ((-.95, -.86, .32, .28), (-.55, -.98, .5, .34), (-.1, -1.04, .56, .38), (.38, -1.0, .52, .35),
                           (.82, -.88, .36, .3), (-1.22, -.72, .1, .2), (1.15, -.74, .12, .22)):
            puff = ellipsoid((r * 1.15, r * .8, r * .9), at=(x, y, z), seg=16, rings=10)
            cloud.append((puff, planar_uv(puff, RG["cloud"], axes=(0, 1))))
        parts.append(("cloud", 4, 1.0, .8, (0, -.95, .4), cloud))
        # THE FACES.
        for k, kind in enumerate(FACES):
            pieces, centres = faces()[kind]
            pivot = tuple(((centres[0] + centres[1]) * .5).tolist()) if centres else (0, EYE_PITCH / 90.0, 1.0)
            parts.append(("face_" + kind, 10 + k, 0.0, .6, pivot, pieces))
        # SLEEPY'S zzz: three chunky Z, small to big, up and to the right; each rises and fades from its own centre.
        zs = []
        for k, (x, y, s) in enumerate(((1.0, .95, .13), (1.32, 1.36, .17), (1.7, 1.86, .22))):
            bar = (s * .5, s * .1, s * .08)
            top = rounded(bar, s * .07, at=(0, s * .42, 0), seg=10, rings=6)
            bot = rounded(bar, s * .07, at=(0, -s * .42, 0), seg=10, rings=6)
            dia = moved(rounded((s * .1, s * .55, s * .08), s * .07, seg=10, rings=6), turn=(0, 0, -50))
            for m in (top, bot, dia):
                mm = moved(m, at=(x, y, .35))
                zs.append(((mm, cell_uv(mm, LAV)), (x, y, .35), k))
        parts.append(("zzz", 17, .5, .9, None, zs))
        # DIZZY'S stars: three little gold five-point stars that orbit over its head.
        stars = []
        for k, ang in enumerate((0.0, 120.0, 240.0)):
            o = [((.13 if j % 2 == 0 else .055) * math.sin(math.radians(36 * j)), (.13 if j % 2 == 0 else .055) * math.cos(math.radians(36 * j))) for j in range(10)][::-1]
            st = slab(o, .05)
            a = math.radians(ang)
            at = (math.cos(a) * .7, 1.12, math.sin(a) * .5 + .1)
            mm = moved(st, at=at)
            stars.append(((mm, cell_uv(mm, YELLOW)), at, ang))
        parts.append(("dizzy_stars", 18, .5, 1.0, (0, 1.12, .1), stars))
        return parts

    # ---------------------------------------------------------------- v3: THE CRESCENT (owner, seen in game 2026-10-19)
    # The owner, of the round moon (moon_v2.png, Logs/payo/unity/moon/before_round_court.png): "does not look like a moon
    # at all". The round pale-yellow ball with arms on a cloud read as a chick, a character, not a moon. So: the
    # storybook "man in the moon" CRESCENT, the strongest read of a moon there is. A chunky rounded crescent, its back to
    # the left and its horns to the right, its face on the inner curve turned to look sideways (a nose on the inner edge,
    # two big glossy eyes in three-quarter, blush, a tiny mouth), silver-cream to cool blue-white with soft craters, a
    # sleepy nightcap with a little gold star for a pom-pom (Mayari's star). No arms, no cloud.
    CRES_RC, CRES_R, CRES_ZS = .72, .41, .82         # the arc's radius, the thickest half-width, the depth squash
    CRES_SHIFT = .38                                 # moved right so its bounding middle is the origin (what a throw aims at)
    # the face is laid on the thick middle, which is a TUBE running up and down: a tall ellipsoid (y radius twice the
    # width) follows its surface where a round one would bury a mouth inside it (v4)
    CHEAD = ((-CRES_RC + CRES_SHIFT, 0.0, 0.0), (CRES_R, CRES_R * 2.0, CRES_R * CRES_ZS))
    C_EYES = ((14.0, 8.0, 1.25),)   # v7: ONE big eye, the face in profile (two eyes crowded the inner edge: "looks weird")   # (yaw, pitch, size): the near eye, the far eye (turned away)
    C_MOUTH = (46.0, -15.0)
    C_EW, C_EH = .19, .235

    def crescent_body():
        pts, radii = [], []
        n = 40
        for k in range(n + 1):
            t = k / n
            a = math.radians(50 + 260 * t)
            pts.append((CRES_RC * math.cos(a) + CRES_SHIFT, CRES_RC * math.sin(a), 0.0))
            radii.append(.022 + (CRES_R - .022) * math.sin(math.pi * t) ** 1.05)
        m = tube(pts, radii, seg=28)
        p = m[0].copy()
        p[:, 2] *= CRES_ZS
        nrm = m[1].copy()
        nrm[:, 2] /= CRES_ZS
        nrm /= np.maximum(np.linalg.norm(nrm, axis=1, keepdims=True), 1e-6)
        return (p, nrm, m[2])

    def c_eye(k, region, scale=1.0):
        yaw, pitch, size = C_EYES[k]
        w, h = C_EW * size * scale, C_EH * size * scale
        dome = ellipsoid((w, h, w * .42), seg=20, rings=12)
        return (wrap(dome, yaw, pitch, lift=-w * .08), planar_uv(dome, region)), surf(yaw, pitch)[0]

    def c_stroke(pts, r, yaw, pitch, slot=None, roll=0.0):
        return (wrap(tube(pts, r, seg=8), yaw, pitch, roll, lift=r * .55), INK if slot is None else slot)

    def crescent_faces():
        lw = .026
        out = {}
        ew = C_EW

        def eyes(region, scale=1.0):
            pcs, cs = [], []
            for k in range(len(C_EYES)):
                pc, c = c_eye(k, region, scale)
                pcs.append(pc); cs.append(c)
            return pcs, cs
        my, mp = C_MOUTH
        cav = slab([(.09 * math.cos(a), .028 + .1 * min(0.0, math.sin(a))) for a in np.linspace(.12, -math.pi - .12, 18)][::-1], .015)
        tongue = ellipsoid((.05, .025, .012), at=(0, -.05, .011), seg=10, rings=6)
        # v10 (owner, of v9: "the moon eyes open looks so creepy"): NO big open eye anywhere. The face at rest is a
        # peaceful closed lid; a hit gets > <, swirls or at most a small startled dot.
        def lid(yaw, pitch, w):
            pcs = [c_stroke(arc(0, w * .25, w * .72, w * .5, 195, 345, 14), lw * 1.15, yaw, pitch - 2)]
            for a in (215, 270, 325):
                x, y = w * .72 * math.cos(math.radians(a)), w * .25 + w * .5 * math.sin(math.radians(a))
                pcs.append(c_stroke([(x, y, 0), (x * 1.1, y - w * .22, 0)], lw * .55, yaw, pitch - 2))
            return pcs

        def dot(yaw, pitch, r):
            return [(wrap(ellipsoid((r, r * 1.2, r * .45), seg=12, rings=8), yaw, pitch, lift=.004), INK),
                    (wrap(ellipsoid((r * .32, r * .32, r * .2), at=(-r * .35, r * .45, r * .4), seg=8, rings=6), yaw, pitch, lift=.004), WHITE)]
        eye_at = tuple(surf(C_EYES[0][0], C_EYES[0][1])[0].tolist())
        yaw0, pitch0, size0 = C_EYES[0]
        # REST: a closed, content lid with lashes, and a soft smile.
        p = lid(yaw0, pitch0, ew * size0)
        p.append(c_stroke(arc(0, .026, .105, .075, 200, 340, 12), lw * 1.05, my, mp))
        out["rest"] = (p, [np.array(eye_at, np.float32)])
        # WORRIED (three hits and more): a small startled dot eye, a worried brow, a wobbly mouth, a sweat drop.
        p = dot(yaw0, pitch0, .1)      # v11, owner: "larger eyes": a round bead eye, still not the glossy stare
        w = ew * size0
        p.append(c_stroke([(-w * .8, w * .55, 0), (0, w * .35, 0), (w * .75, w * .08, 0)], lw * .75, yaw0 + 2, pitch0 + 12))
        p.append(c_stroke([(-.055, 0, 0), (-.028, .014, 0), (0, 0, 0), (.028, .014, 0), (.055, 0, 0)], lw * .7, my, mp))
        drop = lathe([(0.0, 0.0), (.032, .022), (.035, .045), (.02, .07), (0.0, .1)], seg=10)
        p.append((wrap(moved(drop, scale=(1, 1, .5), at=(0, -.05, 0)), 64, 17, roll=-15, lift=.015), SWEAT))
        out["worried"] = (p, [np.array(eye_at, np.float32)])
        # OUCH: squeezed > < eyes, an open "o", tear sparks.
        p = []
        for k, (yaw, pitch, size) in enumerate(C_EYES):
            w = ew * size
            p.append(c_stroke([(-w * .75, w * .6, 0), (w * .5, 0, 0), (-w * .75, -w * .6, 0)], lw * 1.2, yaw, pitch))
            p.append((wrap(ellipsoid((.022, .032, .01)), yaw + 14, pitch - 6, lift=.015), SWEAT))
        p.append((wrap(ellipsoid((.05, .06, .02), seg=12, rings=8), my, mp - 2, lift=.008), INK))
        p.append((wrap(ellipsoid((.03, .033, .012), at=(0, -.012, .014), seg=10, rings=6), my, mp - 2, lift=.008), TONGUE))
        out["ouch"] = (p, [])
        # DIZZY: spiral eyes, a wavy mouth (its stars are part 18).
        p = []
        for k, (yaw, pitch, size) in enumerate(C_EYES):
            w = ew * size
            sp = [(w * .75 * (j / 26) * math.cos(j * .62), w * .75 * (j / 26) * math.sin(j * .62), 0) for j in range(1, 27)]
            p.append(c_stroke(sp, lw * .7, yaw, pitch))
        p.append(c_stroke([(-.065, 0, 0), (-.032, .018, 0), (0, -.006, 0), (.032, .018, 0), (.065, 0, 0)], lw * .75, my, mp))
        out["dizzy"] = (p, [])
        # WOW (as Mayari comes): a small bright dot eye with a star glint, a round open mouth, sparkles by the cheek.
        p = dot(yaw0, pitch0, .11)
        p.append((wrap(cav, my - 4, mp, lift=.03), INK))
        p.append((wrap(tongue, my - 4, mp, lift=.03), TONGUE))
        so = [((.045 if j % 2 == 0 else .016) * math.sin(math.radians(45 * j)), (.045 if j % 2 == 0 else .016) * math.cos(math.radians(45 * j))) for j in range(8)][::-1]
        for yaw, pitch, k in ((60, 22, 1.0), (72, 4, .7)):
            p.append((wrap(moved(slab(so, .012), scale=(k, k, 1)), yaw, pitch, lift=.03), YELLOW))
        out["wow"] = (p, [np.array(eye_at, np.float32)])
        # HAPPY: ^ ^ eyes, the open smile.
        p = []
        for k, (yaw, pitch, size) in enumerate(C_EYES):
            w = ew * size
            p.append(c_stroke(arc(0, -w * .3, w * .78, w * .85, 15, 165, 14), lw * 1.15, yaw, pitch))
        p.append((wrap(cav, my - 4, mp, lift=.03), INK))
        p.append((wrap(tongue, my - 4, mp, lift=.03), TONGUE))
        out["happy"] = (p, [])
        # SLEEPY: closed arcs with lashes, a tiny "o" (its zzz are part 17).
        p = []
        for k, (yaw, pitch, size) in enumerate(C_EYES):
            w = ew * size
            p.append(c_stroke(arc(0, w * .25, w * .72, w * .5, 195, 345, 14), lw, yaw, pitch - 2))
            for a in (215, 270, 325):
                x, y = w * .72 * math.cos(math.radians(a)), w * .25 + w * .5 * math.sin(math.radians(a))
                p.append(c_stroke([(x, y, 0), (x * 1.1, y - w * .2, 0)], lw * .5, yaw, pitch - 2))
        p.append((wrap(ellipsoid((.025, .028, .012), seg=10, rings=6), my, mp - 2, lift=.008), INK))
        out["sleepy"] = (p, [])
        return out

    def crescent_parts():
        global HEAD
        HEAD = CHEAD
        parts = []
        body = crescent_body()
        parts.append(("body", 0, 1.0, 1.0, (0, 0, 0), [(body, planar_uv(body, RG["moon"]))]))
        # the nose: a soft round bump on the inner edge, the profile's one feature that is all moon
        nose = ellipsoid((.085, .08, .07), seg=14, rings=8)
        np_at, nn = surf(86, -3)
        nose = moved(nose, at=tuple(np_at + nn * .025))
        # two shallow crater pads with a pale rim, on the back of the thick part, away from the face
        craters = []
        for yaw, pitch, w in ((-38.0, 19.0, .1), (-22.0, -24.0, .075)):
            pad = ellipsoid((w, w * .82, w * .22), seg=16, rings=8)
            craters.append((wrap(pad, yaw, pitch, lift=-w * .1), CREAM_DK))
            rim = tube([(w * .82 * math.cos(a), w * .66 * math.sin(a), 0) for a in np.linspace(0, 2 * math.pi, 22)], w * .1, seg=6)
            craters.append((wrap(rim, yaw, pitch, lift=w * .02), CREAM))
        parts.append(("nose_craters", 0, .6, 1.0, (0, 0, 0), [(nose, planar_uv(nose, RG["moon"])), *craters]))
        b = ellipsoid((.1, .06, .018), seg=14, rings=6)
        parts.append(("blush", 0, 0.0, .5, (0, 0, 0), [(wrap(moved(b, scale=(1.2, 1.2, 1)), 34, -8, lift=-.003), BLUSH)]))
        # THE NIGHTCAP: a soft drooping cone on the back of its head, indigo with pale stripes, a gold star for a pom-pom
        base_at, base_n = surf(-14, 33)
        prof = [(.15, 0.0), (.155, .03), (.13, .1), (.1, .18), (.07, .26), (.045, .33), (.025, .38), (.0, .41)]
        cone = lathe(prof, seg=20)
        cuv = kit.grid_uv(cone, inset(RG["cap"]), 20, "axis", 1)
        q = cone[0].copy()
        bend = q[:, 1] / .41
        q[:, 0] -= .22 * bend ** 2          # it droops back and down
        q[:, 1] -= .06 * bend ** 3
        cone = (q, cone[1], cone[2])
        brim = tube([(.16 * math.cos(a), .012, .16 * math.sin(a)) for a in np.linspace(0, 2 * math.pi, 26)], .03, seg=8)
        star_o = [((.07 if j % 2 == 0 else .03) * math.sin(math.radians(36 * j)), (.07 if j % 2 == 0 else .03) * math.cos(math.radians(36 * j))) for j in range(10)][::-1]
        star = moved(slab(star_o, .03), at=(-.22, .36, 0))
        tilt = math.degrees(math.atan2(-base_n[0], base_n[1]))
        cap_parts = [(cone, cuv), (brim, cell_uv(brim, CLOUD)), (star, cell_uv(star, YELLOW))]
        cap_parts = [(moved(m, scale=(1.55, 1.55, 1.55), turn=(8, 0, tilt + 6), at=tuple(base_at - base_n * .04)), uv) for m, uv in cap_parts]
        parts.append(("nightcap", 1, .6, .35, tuple(base_at), cap_parts))
        all_faces = crescent_faces()
        for k, kind in enumerate(FACES):
            pieces, centres = all_faces[kind]
            pivot = tuple((sum(centres) / len(centres)).tolist()) if centres else tuple(surf(14, 8)[0].tolist())
            parts.append(("face_" + kind, 10 + k, 0.0, .6, pivot, pieces))
        zs = []
        for k, (x, y, s) in enumerate(((.55, .55, .09), (.78, .82, .12), (1.04, 1.14, .15))):
            bar = (s * .5, s * .1, s * .08)
            top = rounded(bar, s * .07, at=(0, s * .42, 0), seg=10, rings=6)
            bot = rounded(bar, s * .07, at=(0, -s * .42, 0), seg=10, rings=6)
            dia = moved(rounded((s * .1, s * .55, s * .08), s * .07, seg=10, rings=6), turn=(0, 0, -50))
            for m in (top, bot, dia):
                mm = moved(m, at=(x, y, .3))
                zs.append(((mm, cell_uv(mm, LAV)), (x, y, .3), k))
        parts.append(("zzz", 17, .5, .9, None, zs))
        stars = []
        orbit = (CHEAD[0][0] + .1, .72, .05)
        for k, ang in enumerate((0.0, 120.0, 240.0)):
            o = [((.08 if j % 2 == 0 else .034) * math.sin(math.radians(36 * j)), (.08 if j % 2 == 0 else .034) * math.cos(math.radians(36 * j))) for j in range(10)][::-1]
            a = math.radians(ang)
            at = (orbit[0] + math.cos(a) * .36, orbit[1], orbit[2] + math.sin(a) * .26)
            mm = moved(slab(o, .03), at=at)
            stars.append(((mm, cell_uv(mm, YELLOW)), at, ang))
        parts.append(("dizzy_stars", 18, .5, 1.0, orbit, stars))
        return parts

    # ---------------------------------------------------------------- paint
    def hexc(h):
        h = h.lstrip("#")
        return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], np.float32)

    def sm(a, b, x):
        t = np.clip((x - a) / (b - a), 0, 1)
        return t * t * (3 - 2 * t)

    def paint(path):
        from PIL import Image
        img = np.zeros((A_PX, A_PX, 3), np.float32) + hexc("#888888")
        for k, hx in enumerate(PALETTE):
            x0 = (k % 8) * 256
            y0 = 1024 if k < 8 else 1536
            img[y0:y0 + 512, x0:x0 + 256] = hexc(hx)

        def fill(rect, base):
            x0, y0, x1, y1 = (int(round(v * A_PX)) for v in rect)
            u = (np.arange(x0, x1) + .5 - x0) / (x1 - x0)
            t = 1 - (np.arange(y0, y1) + .5 - y0) / (y1 - y0)
            U, T = np.meshgrid(u, t)
            return U, T, (x0, y0, x1, y1), np.zeros(U.shape + (3,), np.float32) + hexc(base)

        def mix(c, col, w):
            return c * (1 - w[..., None]) + hexc(col) * w[..., None]

        # ---- the moon (v5, the crescent, front-projected: U across, T up). The owner, 2026-10-19: "i really like a yellow
        # color for the moon but maybe a crescent design": warm butter yellow, paler at the inner curve where the face is,
        # a deeper gold round the back, soft amber craters on the back and the horns, none on the face
        U, T, (x0, y0, x1, y1), c = fill(RG["moon"], "#ffee96")
        c = mix(c, "#ffdf62", sm(.55, .05, U) * .7)
        c = mix(c, "#fff8cf", sm(.5, .9, U) * sm(.25, .45, T) * sm(.8, .6, T) * .6)
        c = mix(c, "#ffdc5a", (sm(.62, .95, T) + sm(.38, .05, T)) * .4)
        for cu, ct, cr, d in ((.12, .55, .07, .55), (.2, .78, .05, .5), (.18, .3, .06, .5), (.45, .92, .04, .45), (.42, .1, .045, .45),
                              (.07, .42, .035, .4), (.3, .66, .03, .35), (.62, .95, .035, .4), (.6, .06, .03, .4), (.28, .2, .03, .35)):
            dist = np.sqrt(((U - cu) / cr) ** 2 + ((T - ct) / (cr * 1.15)) ** 2)
            c = mix(c, "#f6cd5e", sm(1.0, .5, dist) * d)
            c = mix(c, "#e9b648", sm(.55, .15, dist) * d * .45)
            rim = np.sqrt(((U - cu + cr * .25) / cr) ** 2 + ((T - ct - cr * .3) / (cr * 1.15)) ** 2)
            c = mix(c, "#fff7d0", sm(1.1, .95, rim) * sm(.85, 1.0, rim) * .6)
        img[y0:y1, x0:x1] = c
        # ---- the nightcap: indigo, pale blue stripes round it, a soft shine
        U, T, (x0, y0, x1, y1), c = fill(RG["cap"], "#3b2f8f")
        c = mix(c, "#a9c4ff", (np.abs(((T * 5.5) % 1) - .5) < .17).astype(np.float32) * .9)
        c = mix(c, "#6d5fd0", np.exp(-((U - .3) / .1) ** 2) * .5)
        img[y0:y1, x0:x1] = c
        # ---- the eyes: deep indigo, a violet iris, a lavender glow at the foot, three highlights
        for key in ("eye", "eye_wow"):
            U, T, (x0, y0, x1, y1), c = fill(RG[key], "#1c1238")
            X, Y = (U - .5) * 2, (T - .5) * 2
            r = np.sqrt(X * X + Y * Y)
            c = mix(c, "#5b3fc4", sm(-.1, -.75, Y) * sm(.98, .75, r))
            c = mix(c, "#c8b0ff", sm(-.45, -.85, Y) * sm(.95, .6, r) * .85)
            c = mix(c, "#120a26", sm(.5, .38, np.sqrt((X / .9) ** 2 + ((Y + .05) / 1.0) ** 2)) * .85)
            c = mix(c, "#0e0820", sm(.86, .99, r))
            if key == "eye":
                for hx, hy, hr in ((-.32, .34, .27), (.36, -.42, .12), (.16, .52, .075)):
                    c = mix(c, "#ffffff", sm(hr, hr * .78, np.sqrt((X - hx) ** 2 + (Y - hy) ** 2)))
            else:
                # a four-point star shine, and two dots
                sx, sy = X + .22, Y - .22
                star = np.abs(sx) ** .55 + np.abs(sy) ** .55
                c = mix(c, "#ffffff", sm(.78, .62, star))
                for hx, hy, hr in ((.38, -.4, .11), (.5, .25, .06)):
                    c = mix(c, "#ffffff", sm(hr, hr * .78, np.sqrt((X - hx) ** 2 + (Y - hy) ** 2)))
            img[y0:y1, x0:x1] = c
        # ---- the cloud: lavender-white, lit from above, a cool foot
        U, T, (x0, y0, x1, y1), c = fill(RG["cloud"], "#f6f2ff")
        c = mix(c, "#cfc6f4", sm(.55, .0, T) * .8)
        c = mix(c, "#ffffff", sm(.7, 1.0, T) * .6)
        img[y0:y1, x0:x1] = c
        # ---- the crown band: gold, a bright stripe, a darker foot
        U, T, (x0, y0, x1, y1), c = fill(RG["crown"], "#f5c242")
        c = mix(c, "#d99a2b", sm(.35, .0, T) * .7)
        c = mix(c, "#fff1a8", np.exp(-((T - .62) / .08) ** 2) * .8)
        c = mix(c, "#ff6fa8", (np.abs(((U * 6) % 1) - .5) < .06) * sm(.3, .45, T) * sm(.85, .7, T) * .9)
        img[y0:y1, x0:x1] = c
        Image.fromarray(np.clip(img, 0, 255).astype(np.uint8)).save(path)
        print("wrote", path)

    # ---------------------------------------------------------------- writing
    def write_knl(path, P, N, UV, A, Bv, T):
        P = np.asarray(P, np.float32).copy(); N = np.asarray(N, np.float32).copy(); Bv = np.asarray(Bv, np.float32).copy()
        P[:, 0] *= -1; N[:, 0] *= -1; Bv[:, 0] *= -1
        UV = np.asarray(UV, np.float32).copy()
        T = np.asarray(T, np.int32).reshape(-1, 3)[:, ::-1].reshape(-1)
        with open(path, "wb") as f:
            f.write(b"KNL1" + struct.pack("<ii", len(P), len(T)))
            for arr in (P, N, UV, np.asarray(A, np.float32), Bv):
                f.write(np.ascontiguousarray(arr, np.float32).tobytes())
            f.write(np.ascontiguousarray(T, np.int32).tobytes())
        print("wrote %s: %d vertices, %d triangles" % (os.path.relpath(path, ROOT), len(P), len(T) // 3))
        return len(T) // 3

    class Bake:
        def __init__(self):
            self.P, self.N, self.UV, self.A, self.B, self.T, self.base = [], [], [], [], [], [], 0

        def add(self, mesh, uv, a, b, normals=None):
            p, n, t = mesh
            k = len(p)
            uv = np.asarray(uv, np.float32)
            if uv.ndim == 1:
                uv = np.tile(uv, (k, 1))
            self.P.append(p); self.N.append(n if normals is None else normals); self.UV.append(uv)
            a = np.asarray(a, np.float32); b = np.asarray(b, np.float32)
            self.A.append(a if a.ndim == 2 else np.tile(a, (k, 1)))
            self.B.append(b if b.ndim == 2 else np.tile(b, (k, 1)))
            self.T.append(t + self.base)
            self.base += k

        def write(self, name):
            return write_knl(os.path.join(UNITY_OUT, name + ".bytes"), np.concatenate(self.P), np.concatenate(self.N), np.concatenate(self.UV),
                             np.concatenate(self.A), np.concatenate(self.B), np.concatenate(self.T))

    MOON_DESIGN = "crescent"      # v3; "round" is the refused v1/v2 (moon_parts), kept for the record

    # BUWAN: UV = the atlas (v DOWN, as the kit paints; the shader flips); A = (part, ink width, glow, spare); B = (pivot, 0).
    # Parts: 0 still, 1 crown (wobbles about its base), 2 3 the arms (from the shoulders), 4 the cloud (bobs on its own),
    # 10..16 the seven faces (one shown), 17 the zzz (B: its own centre; A.w which Z), 18 the dizzy stars (B: the orbit's
    # centre; A.w its starting angle, degrees).
    def bake_moon():
        b = Bake()
        for name, part, ink, glow, pivot, pieces in (crescent_parts() if MOON_DESIGN == "crescent" else moon_parts()):
            for item in pieces:
                if part in (17, 18):
                    (mesh, uv), centre, extra = item
                    b.add(mesh, uv, (part, ink, glow, extra), (*(centre if part == 17 else pivot), 0))
                    continue
                mesh, slot = item
                uv = cell_uv(mesh, slot) if np.isscalar(slot) else slot
                b.add(mesh, uv, (part, ink, glow, 0), (*pivot, 0))
        return b.write("moon")

    # ---------------------------------------------------------------- MAYARI, typed
    # (name, x, y, size, appear s, seed, colour: 0 silver, 1 gold, 2 lavender, 3 ice)
    STARS = [
        ("hand_l", -0.95, -1.30, .17, .20, .13, 0), ("fing_l", -0.30, -1.42, .11, .30, .71, 0),
        ("hand_r", 1.05, -1.05, .17, .25, .37, 0), ("fing_r", 0.42, -1.40, .11, .35, .58, 0),
        ("wrist_l", -1.85, -0.80, .13, .45, .91, 2), ("wrist_r", 1.70, -0.05, .13, .50, .24, 2),
        ("elbow_l", -2.95, 0.85, .15, .70, .66, 0), ("elbow_r", 1.75, 1.55, .15, .75, .05, 0),
        ("shoulder_l", -2.75, 2.65, .18, 1.00, .48, 0), ("shoulder_r", -0.55, 2.85, .18, 1.05, .83, 0),
        ("collar", -1.60, 2.55, .12, 1.15, .19, 2), ("neck", -1.55, 3.10, .11, 1.30, .62, 0),
        ("chin", -1.10, 3.55, .13, 1.50, .33, 0), ("lips", -0.82, 3.95, .10, 1.58, .77, 2),
        ("nose", -0.66, 4.30, .12, 1.65, .09, 0), ("brow", -0.92, 4.72, .12, 1.72, .54, 0),
        ("eye", -1.22, 4.48, .09, 1.80, .29, 3), ("forehead", -1.55, 5.02, .13, 1.85, .88, 0),
        ("crown_l", -2.30, 5.20, .12, 2.10, .41, 1), ("crown_r", -0.85, 5.18, .12, 2.15, .68, 1),
        ("tip_l", -2.25, 5.65, .15, 2.30, .15, 1), ("tip_m", -1.58, 6.05, .21, 2.40, .97, 1),
        ("tip_r", -0.92, 5.62, .15, 2.35, .52, 1), ("crownback", -2.70, 4.70, .13, 1.90, .36, 0),
        ("jaw", -1.95, 3.62, .12, 1.55, .81, 0), ("ear", -2.05, 4.18, .10, 1.95, .27, 1),
        ("earring", -2.08, 3.80, .09, 2.05, .74, 1), ("chest", -1.75, 1.75, .13, 1.20, .45, 2),
        ("waist", -2.05, 0.35, .14, 1.30, .11, 0), ("hip_l", -3.00, -0.85, .14, 1.55, .63, 0),
        ("skirt_m", -0.55, -2.35, .13, 1.70, .87, 2), ("skirt_r", 1.45, -2.20, .13, 1.80, .31, 0),
        ("skirt_l", -2.95, -2.15, .13, 1.75, .57, 0), ("hem_1", -3.55, -3.60, .15, 2.30, .21, 0),
        ("hem_2", -2.10, -3.95, .12, 2.40, .69, 2), ("hem_3", -0.55, -4.05, .16, 2.50, .43, 0),
        ("hem_4", 1.05, -3.85, .12, 2.60, .95, 2), ("hem_5", 2.35, -3.40, .15, 2.70, .17, 0),
    ]
    # Lines between stars: the crown, her profile, neck and shoulders, both arms down to the hands round the moon, the gown.
    # Lines between stars, only where a line says something (v2: v1's lines round the head and down the gown read as a
    # cage and a walking stick figure): the crown, her profile, neck and shoulders, both arms down to the hands round the
    # moon, the line of her body. The gown and the back of her head are told by the veil, the hair and the dust instead.
    LINES = [
        ("crown_l", "tip_l"), ("tip_l", "tip_m"), ("tip_m", "tip_r"), ("tip_r", "crown_r"),
        ("forehead", "brow"), ("brow", "nose"), ("nose", "lips"), ("lips", "chin"),
        ("ear", "earring"), ("chin", "neck"), ("neck", "collar"), ("collar", "shoulder_l"), ("collar", "shoulder_r"),
        ("shoulder_l", "elbow_l"), ("elbow_l", "wrist_l"), ("wrist_l", "hand_l"), ("hand_l", "fing_l"),
        ("shoulder_r", "elbow_r"), ("elbow_r", "wrist_r"), ("wrist_r", "hand_r"), ("hand_r", "fing_r"),
        ("collar", "chest"), ("chest", "waist"),
    ]
    # THE VEIL (v2): soft glowing ribbons that give her body its shape behind the stars, the way Makiling is a see-through
    # light: (path, width at each point, appear s). Drawn very faint, brightest down the middle, drawn in along the path.
    VEIL = [
        ([(-1.70, 3.45), (-1.62, 4.25), (-1.70, 5.05)], [.55, .9, .62], 1.55),
        ([(-1.58, 3.20), (-1.70, 2.60), (-1.85, 1.60), (-2.05, 0.40)], [.22, .8, .62, .52], 1.10),
        ([(-2.75, 2.65), (-2.95, 0.85), (-1.85, -0.80), (-0.95, -1.30), (-0.30, -1.42)], [.3, .26, .2, .18, .1], .35),
        ([(-0.55, 2.85), (1.75, 1.55), (1.70, -0.05), (1.05, -1.05), (0.42, -1.40)], [.3, .26, .2, .18, .1], .40),
        ([(-2.05, 0.40), (-2.35, -0.85), (-2.05, -2.20), (-1.55, -3.20), (-1.10, -4.05)], [.6, 1.2, 1.9, 2.6, 3.2], 1.45),
        ([(-1.30, -1.60), (0.20, -2.40), (1.40, -3.30), (2.20, -3.95)], [.6, 1.1, 1.5, 1.8], 1.65),
    ]
    # Hair: typed curves (control points), how many motes ride each, their size range, when the stream appears.
    HAIR = [
        ([(-2.70, 4.70), (-2.45, 5.55), (-1.05, 6.45), (0.80, 6.05), (2.30, 5.00), (3.40, 3.50), (4.10, 1.80), (4.60, 0.00)], 34, (.04, .12), 1.70),
        ([(-2.80, 4.40), (-3.60, 3.60), (-4.00, 2.20), (-4.10, 0.60), (-4.45, -1.00)], 20, (.04, .10), 1.90),
        ([(-2.00, 5.90), (-0.20, 6.95), (1.80, 6.65), (3.40, 5.60), (4.70, 4.00), (5.60, 2.20)], 26, (.03, .09), 2.10),
    ]
    HALO = (-1.66, 4.45)
    HALO_R = 1.32
    HALO_DOTS = [(15, .07), (52, .1), (88, .06), (121, .11), (160, .07), (198, .09), (233, .06), (270, .12), (305, .07), (340, .09)]
    HEM_DUST = [(-3.4, -3.9, .07), (-2.7, -4.2, .05), (-1.9, -4.3, .08), (-1.2, -4.25, .05), (-0.4, -4.4, .07), (0.3, -4.3, .05),
                (1.0, -4.15, .06), (1.7, -3.95, .05), (2.3, -3.7, .07), (-3.0, -4.5, .05), (-0.9, -4.7, .06), (0.8, -4.6, .05),
                (1.9, -4.3, .05), (-2.3, -4.6, .06)]
    FIG_Z = -1.2
    # v3 (seen in Play from the court, 2026-10-19): her stars were too small to sparkle at 2.9 km; all typed sizes x1.35.
    STAR_GROW = 1.35

    def sparkle(size, long=1.0, short=.38, seg=5):
        """A faceted 3D star bead: four long spikes in its plane, two short ones toward and away from the eye, a core."""
        parts = []
        L, r = size * long, size * .2
        cone = lathe([(r, 0.0), (r * .55, L * .45), (0.0, L)], seg=seg)
        for turn in ((0, 0, -90), (0, 0, 90), (0, 0, 0), (0, 0, 180)):
            parts.append(moved(cone, turn=turn))
        sc = lathe([(r * .9, 0.0), (0.0, size * short)], seg=seg)
        parts.append(moved(sc, turn=(90, 0, 0)))
        parts.append(moved(sc, turn=(-90, 0, 0)))
        parts.append(ellipsoid((size * .3, size * .3, size * .22), seg=8, rings=6))
        return kit.join(*parts)

    def bezier(ctrl, t):
        """A Catmull-Rom spline through the typed points at t in 0..1, and its tangent."""
        pts = [np.array(p, np.float32) for p in ctrl]
        n = len(pts) - 1
        f = min(n - 1e-6, max(0.0, t * n))
        i = int(f)
        u = f - i
        p0, p1, p2, p3 = pts[max(0, i - 1)], pts[i], pts[min(n, i + 1)], pts[min(n, i + 2)]
        pos = .5 * ((2 * p1) + (-p0 + p2) * u + (2 * p0 - 5 * p1 + 4 * p2 - p3) * u * u + (-p0 + 3 * p1 - 3 * p2 + p3) * u ** 3)
        tan = .5 * ((-p0 + p2) + 2 * (2 * p0 - 5 * p1 + 4 * p2 - p3) * u + 3 * (-p0 + 3 * p1 - 3 * p2 + p3) * u * u)
        return pos, tan / max(1e-6, float(np.linalg.norm(tan)))

    def hash01(n):
        z = (n * 0x9E3779B97F4A7C15) & 0xFFFFFFFFFFFFFFFF
        z ^= z >> 31; z = (z * 0xBF58476D1CE4E5B9) & 0xFFFFFFFFFFFFFFFF; z ^= z >> 29
        return (z & 0xFFFFFF) / 16777216.0

    # MAYARI: A = (kind, appear s, seed, colour); B = (centre, size). Kinds: 0 star, 1 line, 2 hair mote, 3 halo crescent,
    # 4 halo dot, 5 hem dust, 13 veil (uv = across -1..1, along 0..1). A LINE vertex's B is the centre of the star at ITS end (B.w that star's seed), its normal the
    # offset from there to the line's START star, its uv (along 0..1, draw seconds). A mote's uv is its flow (xy, units).
    # The halo's B is the halo's centre; a crescent vertex's uv.x is along the arc.
    def bake_mayari():
        b = Bake()
        at = {s[0]: s for s in STARS}
        for name, x, y, size, appear, seed, colour in STARS:
            size *= STAR_GROW
            m = moved(sparkle(size), at=(x, y, FIG_Z))
            b.add(m, (0, 0), (0, appear, seed, colour), (x, y, FIG_Z, size))
        for k, (s0, s1) in enumerate(LINES):
            a, e = at[s0], at[s1]
            p0 = np.array((a[1], a[2], FIG_Z), np.float32); p1 = np.array((e[1], e[2], FIG_Z), np.float32)
            d = p1 - p0
            L = float(np.linalg.norm(d))
            # pulled in from both stars so a line never runs through a bead
            q0, q1 = p0 + d / L * a[3] * .8, p1 - d / L * e[3] * .8
            m = tube([tuple(q0), tuple((q0 + q1) * .5), tuple(q1)], [.022, .026, .022], seg=5)
            p = m[0]
            along = np.clip(((p - q0) @ (q1 - q0)) / max(1e-6, float((q1 - q0) @ (q1 - q0))), 0, 1)
            end = along > .5
            centre = np.where(end[:, None], p1, p0)
            seedv = np.where(end, e[5], a[5])
            # each vertex: B its own end star; the normal the way back to the line's start star (zero at the start)
            nrm = np.where(end[:, None], p0 - p1, 0 * p0).astype(np.float32)
            appear = max(a[4], e[4]) + .12
            A = np.stack([np.full(len(p), 1.0), np.full(len(p), appear), seedv, np.full(len(p), float(a[6] if a[6] == e[6] else 0))], 1)
            Bv = np.concatenate([centre, seedv[:, None]], 1)
            uv = np.stack([along, np.full(len(p), .45 + .1 * (k % 3))], 1)
            b.add(m, uv, A, Bv, normals=nrm)
        for h, (ctrl, count, (s_lo, s_hi), appear) in enumerate(HAIR):
            for k in range(count):
                t = (k + .5) / count
                pos, tan = bezier(ctrl, t)
                seed = hash01(1000 * h + k + 17)
                size = s_lo + (s_hi - s_lo) * hash01(1000 * h + k + 91)
                side = (hash01(1000 * h + k + 53) - .5) * .75
                nrm2 = np.array((-tan[1], tan[0]), np.float32)
                c = (float(pos[0] + nrm2[0] * side), float(pos[1] + nrm2[1] * side), FIG_Z - .1 - .2 * hash01(k + 7 * h))
                m = moved(sparkle(size, long=1.1), at=c)
                b.add(m, (float(tan[0] * .45), float(tan[1] * .45)), (2, appear + 1.2 * t, seed, 3 if k % 4 == 0 else (2 if k % 3 == 0 else 0)), (*c, size))
        # the veil: flat ribbons in a plane just behind the stars, uv (across -1..1, along 0..1), B = (start of path, 0)
        hair_veils = []
        for ctrl, _, _, appear in HAIR:
            path = [tuple(bezier(ctrl, t)[0].tolist()) for t in np.linspace(0, 1, 9)]
            hair_veils.append((path, [.3 - .24 * t for t in np.linspace(0, 1, 9)], appear))
        for k, (path, widths, appear) in enumerate(VEIL + hair_veils):
            pts = [np.array(p, np.float32) for p in path]
            fine, wide = [], []
            for j in range(len(pts) - 1):
                for s in range(6):
                    t = s / 6
                    fine.append(pts[j] * (1 - t) + pts[j + 1] * t); wide.append(widths[j] * (1 - t) + widths[j + 1] * t)
            fine.append(pts[-1]); wide.append(widths[-1])
            L = np.concatenate([[0], np.cumsum([np.linalg.norm(fine[j + 1] - fine[j]) for j in range(len(fine) - 1)])])
            vs, uv, tr = [], [], []
            for j, (c, wd) in enumerate(zip(fine, wide)):
                d = fine[min(j + 1, len(fine) - 1)] - fine[max(j - 1, 0)]
                d = d / max(1e-6, float(np.linalg.norm(d)))
                side = np.array((-d[1], d[0]), np.float32)
                for a in (-1.0, 0.0, 1.0):
                    q = c + side * wd * .5 * a
                    vs.append((float(q[0]), float(q[1]), FIG_Z - .08)); uv.append((a, float(L[j] / L[-1])))
            for j in range(len(fine) - 1):
                for a in range(2):
                    i0 = j * 3 + a
                    tr += [(i0, i0 + 3, i0 + 4), (i0, i0 + 4, i0 + 1)]
            vs = np.array(vs, np.float32)
            b.add((vs, np.tile(np.array((0, 0, 1), np.float32), (len(vs), 1)), np.array(tr, np.uint32)), np.array(uv, np.float32),
                  (13, appear, hash01(k + 1300), 2), (float(pts[0][0]), float(pts[0][1]), FIG_Z, 0))
        # the halo: a gold crescent round the back of her head, thick in the middle, pointed at both ends, and its dots
        hc = (HALO[0], HALO[1], FIG_Z - .4)
        pts, radii = [], []
        for k in range(29):
            a = math.radians(105 + 230 * k / 28)
            pts.append((hc[0] + HALO_R * math.cos(a), hc[1] + HALO_R * math.sin(a), hc[2]))
            radii.append(.008 + .075 * math.sin(math.pi * k / 28) ** 1.3)
        cres = tube(pts, radii, seg=8)
        p = cres[0]
        ang = (np.degrees(np.arctan2(p[:, 1] - hc[1], p[:, 0] - hc[0])) - 105) % 360
        b.add(cres, np.stack([np.clip(ang / 230, 0, 1), np.zeros(len(p))], 1), (3, 2.4, .5, 1), (*hc, HALO_R))
        for k, (deg, size) in enumerate(HALO_DOTS):
            a = math.radians(deg)
            c = (hc[0] + 1.8 * math.cos(a), hc[1] + 1.8 * math.sin(a), hc[2])
            m = moved(sparkle(size), at=c)
            b.add(m, (0, 0), (4, 2.6 + .05 * k, hash01(k + 300), 1 if k % 2 == 0 else 0), (*hc, size))
        for k, (x, y, size) in enumerate(HEM_DUST):
            c = (x, y, FIG_Z + .05)
            m = moved(sparkle(size), at=c)
            b.add(m, (0.0, -.55), (5, 2.6 + .04 * k, hash01(k + 500), 2 if k % 2 else 0), (*c, size))
        return b.write("mayari")

    # METEORS: (x, y, travel direction in degrees (0 right, -90 straight down), travel length, first launch s, life s,
    # head size, repeat period s). They fall out of the sky round her, away from her halo, in the plane 3 behind the moon.
    METEORS = [
        (-4.2, 8.6, -128, 5.0, 0.6, 1.3, .20, 7.3), (1.2, 9.2, -62, 5.5, 1.1, 1.5, .22, 8.1), (-7.4, 6.8, -140, 4.2, 1.6, 1.1, .16, 6.7),
        (4.8, 8.0, -55, 4.8, 2.0, 1.4, .18, 7.9), (-1.9, 9.6, -104, 5.8, 2.6, 1.6, .24, 8.6), (7.6, 6.9, -48, 4.0, 3.1, 1.2, .15, 6.4),
        (-6.0, 4.3, -152, 3.6, 3.5, 1.0, .14, 7.1), (3.0, 7.1, -70, 4.4, 3.9, 1.3, .17, 8.4), (-3.2, 7.6, -118, 4.6, 4.4, 1.3, .19, 6.9),
        (6.3, 4.6, -40, 3.8, 4.8, 1.1, .15, 7.6), (-8.4, 8.2, -135, 4.9, 5.3, 1.4, .18, 8.8), (0.1, 8.4, -88, 5.2, 5.7, 1.5, .21, 7.4),
        (8.8, 8.7, -60, 4.6, 6.2, 1.3, .17, 6.6), (-5.1, 9.9, -122, 5.6, 6.6, 1.6, .2, 8.2), (2.4, 10.1, -76, 6.0, 7.0, 1.7, .23, 7.0),
        (-9.3, 5.4, -146, 3.9, 0.2, 1.1, .14, 7.8), (5.6, 10.4, -66, 5.4, 1.4, 1.5, .19, 8.9), (-2.8, 10.6, -96, 6.2, 2.3, 1.8, .25, 7.7),
        (9.6, 5.9, -44, 3.5, 2.8, 1.0, .13, 6.8), (-6.8, 10.2, -130, 5.3, 3.3, 1.5, .18, 8.3), (3.9, 5.5, -58, 3.4, 4.1, 1.0, .13, 7.2),
        (-0.9, 11.0, -84, 6.4, 4.6, 1.8, .26, 8.7), (7.0, 10.8, -57, 5.0, 5.1, 1.4, .17, 6.5), (-4.6, 5.8, -138, 3.7, 5.5, 1.1, .14, 7.5),
        (1.7, 6.3, -80, 3.6, 6.0, 1.1, .14, 8.0), (-8.0, 11.2, -126, 5.8, 6.4, 1.6, .2, 6.3), (6.0, 7.8, -50, 4.1, 6.9, 1.2, .16, 7.1),
        (-3.8, 11.6, -108, 6.6, 0.9, 1.9, .27, 9.0),
    ]
    MET_Z = -3.0

    # METEORS: A = (kind, first launch, life, period); B = (the head's start, travel length); NORMAL = the unit direction of
    # travel. Kinds: 6 head (uv unused), 7 tail (uv = along 0 head .. 1 end, across -1..1), 8 spark (uv.x its place along).
    def bake_meteors():
        b = Bake()
        for k, (x, y, deg, length, launch, life, size, period) in enumerate(METEORS):
            d = np.array((math.cos(math.radians(deg)), math.sin(math.radians(deg)), 0), np.float32)
            start = np.array((x, y, MET_Z - .3 * hash01(k + 900)), np.float32)
            A = (0, launch, life, period)
            Bv = (*start, length)
            dirn = np.tile(d, (1, 1))
            head = moved(sparkle(size, long=1.15), at=tuple(start))
            b.add(head, (0, 0), (6, *A[1:]), Bv, normals=np.tile(d, (len(head[0]), 1)))
            tail_len = size * 13
            tpts = [tuple(start - d * tail_len * s) for s in np.linspace(0, 1, 9)]
            tl = tube(tpts, [size * .42 * (1 - s) ** 1.4 + .006 for s in np.linspace(0, 1, 9)], seg=6)
            p = tl[0]
            along = np.clip(((start - p) @ d) / tail_len, 0, 1)
            side = np.array((-d[1], d[0], 0), np.float32)
            across = ((p - start) @ side) / (size * .42)
            b.add(tl, np.stack([along, across], 1), (7, *A[1:]), Bv, normals=np.tile(d, (len(p), 1)))
            for j, (s, off) in enumerate(((.22, .9), (.45, -1.2), (.7, .6))):
                c = start - d * tail_len * s + side * size * off
                sp = moved(sparkle(size * .35), at=tuple(c))
                b.add(sp, (s, 0), (8, *A[1:]), Bv, normals=np.tile(d, (len(sp[0]), 1)))
        return b.write("meteors")

    # COMET (the slipper's flight): A = (9, 0, 0, 0); the head at the origin, the tail along -z to 1, two crossed puffy
    # tails so it reads from any side. uv = (along, across).
    def bake_comet():
        b = Bake()
        head = sparkle(.22, long=1.2)
        b.add(head, (0, 0), (9, 0, 0, 0), (0, 0, 0, 0))
        tpts = [(0, 0, -s) for s in np.linspace(0, 1, 9)]
        tl = tube(tpts, [.13 * (1 - s) ** 1.3 + .005 for s in np.linspace(0, 1, 9)], seg=8)
        p = tl[0]
        b.add(tl, np.stack([np.clip(-p[:, 2], 0, 1), np.zeros(len(p))], 1), (9, 1, 0, 0), (0, 0, 0, 0))
        return b.write("comet")

    # BURST (a hit): A = (10, size, seed, 0 bead / 1 gold star); B = (where it flies to, in moon radii, 0); the mesh round
    # the origin, the game puts the piece on the impact point. Typed directions, mostly in the plane facing the court.
    BURST = [(1.0, .25, .12, 0), (.72, .7, .1, 1), (.15, 1.0, .13, 0), (-.5, .86, .09, 0), (-.95, .35, .12, 1), (-.9, -.3, .1, 0),
             (-.45, -.9, .13, 0), (.2, -.98, .09, 1), (.75, -.62, .12, 0), (.98, -.15, .08, 0), (.4, .45, .07, 0), (-.3, .4, .07, 0),
             (-.35, -.45, .06, 0), (.5, -.3, .07, 1), (.05, .62, .06, 0), (-.62, .05, .06, 0)]

    def bake_burst():
        b = Bake()
        for k, (x, y, size, gold) in enumerate(BURST):
            if gold:
                o = [((size * 1.6 if j % 2 == 0 else size * .7) * math.sin(math.radians(36 * j)), (size * 1.6 if j % 2 == 0 else size * .7) * math.cos(math.radians(36 * j))) for j in range(10)][::-1]
                m = slab(o, size * .6)
            else:
                m = sparkle(size)
            d = np.array((x, y, .25 * (hash01(k + 77) - .5)), np.float32)
            b.add(m, (0, 0), (10, size, hash01(k + 700), gold), (*(d * 1.0), 0))
        return b.write("burst")

    # GLOW: 11 the moon's soft glow disc (uv = the disc's own xy, -1..1), 12 the reveal's ring (uv.x 0 inner .. 1 outer).
    def bake_glow():
        b = Bake()
        n = 48
        p = [(0.0, 0.0, -1.3)] + [(2.6 * math.cos(2 * math.pi * k / n), 2.6 * math.sin(2 * math.pi * k / n), -1.3) for k in range(n)]
        tris = [(0, 1 + (k + 1) % n, 1 + k) for k in range(n)]
        p = np.array(p, np.float32)
        b.add((p, np.tile(np.array((0, 0, 1), np.float32), (len(p), 1)), np.array(tris, np.uint32)), p[:, :2] / 2.6, (11, 0, 0, 0), (0, 0, 0, 0))
        q, uv, tr = [], [], []
        for k in range(n):
            a = 2 * math.pi * k / n
            for r, u in ((1.0, 0.0), (1.12, 1.0)):
                q.append((r * math.cos(a), r * math.sin(a), -1.25)); uv.append((u, 0))
        for k in range(n):
            i, j = 2 * k, 2 * ((k + 1) % n)
            tr += [(i, j, j + 1), (i, j + 1, i + 1)]
        q = np.array(q, np.float32)
        b.add((q, np.tile(np.array((0, 0, 1), np.float32), (len(q), 1)), np.array(tr, np.uint32)), np.array(uv, np.float32), (12, 0, 0, 0), (0, 0, 0, 0))
        return b.write("glow")

    def unity():
        import shutil
        os.makedirs(UNITY_OUT, exist_ok=True); os.makedirs(OUTD, exist_ok=True)
        paint(os.path.join(OUTD, ATLAS))
        # (the constellation Mayari, `bake_mayari`, was replaced by the modelled goddess, tools/payo_pieces/mayari_goddess.py)
        tris = {"moon": bake_moon(), "meteors": bake_meteors(), "comet": bake_comet(), "burst": bake_burst(), "glow": bake_glow()}
        shutil.copyfile(os.path.join(OUTD, ATLAS), os.path.join(UNITY_OUT, ATLAS))
        json.dump(tris, open(os.path.join(OUTD, "tris.json"), "w"), indent=1)
        print("TRIANGLES", json.dumps(tris))

    def sheet(version="v1"):
        from PIL import Image, ImageDraw
        names = ["face_" + f for f in FACES] + ["rest_quarter", "rest_side", "crown_top"]
        tiles = [(n, os.path.join(SHOTS, n + ".png")) for n in names if os.path.exists(os.path.join(SHOTS, n + ".png"))]
        if tiles:
            ims = [Image.open(p).convert("RGB") for _, p in tiles]
            w, h = ims[0].size
            cols = 5
            rows = (len(ims) + cols - 1) // cols
            out = Image.new("RGB", (w * cols, (h + 20) * rows), (14, 12, 28))
            d = ImageDraw.Draw(out)
            for k, (im, (n, _)) in enumerate(zip(ims, tiles)):
                x, y = (k % cols) * w, (k // cols) * (h + 20)
                out.paste(im, (x, y + 20)); d.text((x + 6, y + 4), "BUWAN %s  %s" % (version, n), fill=(255, 255, 255))
            p = os.path.join(OUTD, "moon_%s.png" % version); out.save(p); print("wrote", p)
        frames = [os.path.join(SHOTS, "mayari_%d.png" % k) for k in range(6)]
        frames = [f for f in frames if os.path.exists(f)]
        if frames:
            ims = [Image.open(p).convert("RGB") for p in frames]
            w, h = ims[0].size
            cols = 3
            rows = (len(ims) + cols - 1) // cols
            out = Image.new("RGB", (w * cols, (h + 20) * rows), (14, 12, 28))
            d = ImageDraw.Draw(out)
            labels = json.load(open(os.path.join(SHOTS, "mayari_labels.json"))) if os.path.exists(os.path.join(SHOTS, "mayari_labels.json")) else {}
            for k, im in enumerate(ims):
                x, y = (k % cols) * w, (k // cols) * (h + 20)
                out.paste(im, (x, y + 20)); d.text((x + 6, y + 4), "MAYARI %s  %s" % (version, labels.get(str(k), str(k))), fill=(255, 255, 255))
            p = os.path.join(OUTD, "mayari_%s.png" % version); out.save(p); print("wrote", p)

    if __name__ == "__main__":
        cmd = sys.argv[1] if len(sys.argv) > 1 else "unity"
        if cmd == "stage":
            UNITY_OUT = STAGE_OUT
            unity()
        elif cmd == "unity":
            unity()
        elif cmd == "sheet":
            sheet(sys.argv[2] if len(sys.argv) > 2 else "v1")


# =====================================================================================================================
# BLENDER: the review, from the baked files themselves
# =====================================================================================================================
else:
    import bmesh
    from mathutils import Vector

    def to_blender(p):
        # Unity (x right, y up, z forward; left-handed) to Blender (x right, y forward, z up): (x, z, y).
        return (float(p[0]), float(p[2]), float(p[1]))

    def mat_flat(name, col, emit=0.0, alpha=1.0):
        m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
        m.use_nodes = True
        b = m.node_tree.nodes["Principled BSDF"]
        b.inputs["Base Color"].default_value = (*col, 1)
        b.inputs["Roughness"].default_value = .55
        b.inputs["Emission Color"].default_value = (*col, 1)
        b.inputs["Emission Strength"].default_value = emit
        m.use_backface_culling = True
        return m

    def make_obj(name, P, T, keep_faces, uv=None, mats=None, face_mat=None):
        """A Blender object of the triangles `keep_faces` (a mask) of a baked mesh."""
        me = bpy.data.meshes.new(name)
        verts = [to_blender(p) for p in P]
        # the baked winding is for left-handed Unity; (x, z, y) mirrors once more, so each triangle is turned back
        faces = [(int(t[0]), int(t[2]), int(t[1])) for t, k in zip(T, keep_faces) if k]
        me.from_pydata(verts, [], faces)
        if uv is not None:
            layer = me.uv_layers.new(name="uv")
            for poly in me.polygons:
                for li in poly.loop_indices:
                    vi = me.loops[li].vertex_index
                    layer.data[li].uv = (float(uv[vi][0]), 1.0 - float(uv[vi][1]))
        for m in mats or []:
            me.materials.append(m)
        if face_mat is not None:
            fm = [f for f, k in zip(face_mat, keep_faces) if k]
            for poly, k in zip(me.polygons, fm):
                poly.material_index = k
        for poly in me.polygons:
            poly.use_smooth = True
        o = bpy.data.objects.new(name, me)
        bpy.context.scene.collection.objects.link(o)
        return o

    def ink(o, thick):
        line = bpy.data.materials.get("ink")
        if line is None:
            line = bpy.data.materials.new("ink"); line.use_nodes = True; line.use_backface_culling = True
            b = line.node_tree.nodes["Principled BSDF"]
            b.inputs["Base Color"].default_value = (0.07, 0.04, 0.12, 1); b.inputs["Roughness"].default_value = 1
        h = o.copy(); h.data = o.data.copy(); h.name = o.name + "_ink"
        bpy.context.scene.collection.objects.link(h)
        h.data.materials.clear(); h.data.materials.append(line)
        bm = bmesh.new(); bm.from_mesh(h.data)
        for f in bm.faces:
            f.material_index = 0
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0004)
        bmesh.ops.reverse_faces(bm, faces=bm.faces)
        bm.normal_update()
        for v in bm.verts:
            v.co = v.co - v.normal * thick
        bm.to_mesh(h.data); bm.free()
        return h

    def render():
        import numpy as np
        root = sys.argv[sys.argv.index("--") + 2] if len(sys.argv) > sys.argv.index("--") + 2 else ROOT
        res = os.path.join(root, "Assets", "TumbangPreso", "Resources", "Map", "PayoMoon")
        if "stage" in sys.argv:
            res = os.path.join(root, "Logs", "payo", "pieces", "moon", "bake")
        shots = os.path.join(root, "Logs", "payo", "pieces", "moon", "shots")
        os.makedirs(shots, exist_ok=True)
        bpy.ops.wm.read_factory_settings(use_empty=True)
        sc = bpy.context.scene
        engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
        sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
        sc.view_settings.view_transform = "Standard"
        try:
            sc.eevee.taa_render_samples = 32
        except Exception:
            pass
        w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
        bg = w.node_tree.nodes["Background"]
        bg.inputs[0].default_value = (0.035, 0.04, 0.12, 1); bg.inputs[1].default_value = 1.0

        def sun(direction, energy, col):
            s = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sc.collection.objects.link(s)
            s.data.energy = energy; s.data.color = col; s.data.angle = math.radians(6)
            s.rotation_euler = Vector(direction).normalized().to_track_quat("-Z", "Y").to_euler()
        # as the game shades it: a soft key from the upper left in front, a lilac fill
        sun((.5, .8, -.6), 3.2, (1.0, .97, .9))
        sun((-.6, -.3, .4), 1.0, (.7, .66, 1.0))
        cam = bpy.data.objects.new("c", bpy.data.cameras.new("c")); sc.collection.objects.link(cam); sc.camera = cam
        cam.data.clip_end = 1000

        def aim(eye, look, fov):
            cam.data.sensor_fit = "VERTICAL"
            cam.data.angle_y = math.radians(fov)
            cam.location = Vector(to_blender(eye))
            cam.rotation_euler = (Vector(to_blender(look)) - Vector(to_blender(eye))).to_track_quat("-Z", "Y").to_euler()

        def shoot(name, res_xy):
            sc.render.resolution_x, sc.render.resolution_y = res_xy
            sc.render.filepath = os.path.join(shots, name + ".png")
            bpy.ops.render.render(write_still=True)
            print("SHOT", name)

        # ---- BUWAN, one face at a time, inked as the game inks it
        P, N, UV, A, Bv, T = read_knl(os.path.join(res, "moon.bytes"))
        part = A[:, 0]
        face_part = part[T[:, 0]]
        atlas = bpy.data.images.load(os.path.join(res, ATLAS))
        am = bpy.data.materials.new("moon_atlas"); am.use_nodes = True
        bsdf = am.node_tree.nodes["Principled BSDF"]
        tex = am.node_tree.nodes.new("ShaderNodeTexImage"); tex.image = atlas; tex.interpolation = "Linear"
        am.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
        am.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Emission Color"])
        bsdf.inputs["Emission Strength"].default_value = .35
        bsdf.inputs["Roughness"].default_value = .45
        am.use_backface_culling = True
        objs = {}
        for kind in [0, 1, 2, 3, 4] + list(range(10, 19)):
            mask = face_part == kind
            if not mask.any():
                continue
            o = make_obj("moon_%d" % kind, P, T, mask, uv=UV, mats=[am])
            inkw = float(A[T[mask][0][0], 1])
            objs[kind] = (o, ink(o, .022 * inkw) if inkw > 0 else None)

        def face(k):
            for kind, (o, h) in objs.items():
                on = kind < 10 or kind == 10 + k or (kind == 17 and k == 6) or (kind == 18 and k == 3)
                o.hide_render = not on
                if h is not None:
                    h.hide_render = not on
        for k, f in enumerate(FACES):
            face(k)
            aim((0, .15, 5.2), (0, .1, 0), 40)
            shoot("face_" + f, (640, 640))
        face(0)
        aim((3.6, 1.2, 3.8), (0, .1, 0), 40); shoot("rest_quarter", (640, 640))
        aim((5.2, .3, 0), (0, .1, 0), 40); shoot("rest_side", (640, 640))
        aim((.6, 5.0, 1.6), (0, .3, 0), 40); shoot("crown_top", (640, 640))

        # ---- MAYARI, forming: stars with appear < t, lines drawn by then, motes, halo; meteors frozen in flight
        if not os.path.exists(os.path.join(res, "mayari.bytes")):
            print("RENDERED", shots, "(no constellation: Mayari is the modelled goddess now, mayari_goddess.py)")
            return
        face(5)
        cols = {0: (.82, .88, 1.0), 1: (1.0, .82, .4), 2: (.78, .66, 1.0), 3: (.62, .92, 1.0)}
        mats = [mat_flat("glow_%d" % k, cols[k], 2.2) for k in range(4)]
        veil = mat_flat("veil", (.55, .5, 1.0), 1.2)
        veil.node_tree.nodes["Principled BSDF"].inputs["Alpha"].default_value = .22
        try:
            veil.surface_render_method = "BLENDED"
        except Exception:
            veil.blend_method = "BLEND"
        veil.use_backface_culling = False
        mats.append(veil)
        Pm, Nm, UVm, Am, Bm, Tm = read_knl(os.path.join(res, "mayari.bytes"))
        Pt, Nt, UVt, At, Bt, Tt = read_knl(os.path.join(res, "meteors.bytes"))
        frames = [(0.6, "0.6 s: the hands"), (1.4, "1.4 s: arms and shoulders"), (2.2, "2.2 s: her face"), (3.4, "3.4 s: whole, the halo"),
                  (8.0, "8 s: with the shower"), (99, "the meteors alone")]
        labels = {}
        made = []
        for k, (t, label) in enumerate(frames):
            for o in made:
                bpy.data.objects.remove(o, do_unlink=True)
            made = []
            first = Tm[:, 0]
            kind, appear = Am[first, 0], Am[first, 1]
            draw = np.where(kind == 1, appear + .45, appear)
            keep = (draw <= t) if t < 50 else np.zeros(len(Tm), bool)
            if keep.any():
                made.append(make_obj("mayari", Pm, Tm, keep, mats=mats, face_mat=np.where(kind == 13, 4, Am[first, 3]).astype(int)))
            if t >= 8:
                ft = Tt[:, 0]
                launch, life, period = At[ft, 1], At[ft, 2], At[ft, 3]
                tt = 4.0 if t < 50 else 5.6
                age = (tt - launch) % period
                live = (tt >= launch) & (age < life)
                if live.any():
                    # each live meteor drawn where it is at this moment: its head moved along its direction
                    Pv = Pt.copy()
                    for vi in range(len(Pv)):
                        a = (tt - At[vi, 1]) % At[vi, 3]
                        Pv[vi] = Pv[vi] + Nt[vi] * Bt[vi, 3] * min(1.0, a / max(.01, At[vi, 2]))
                    made.append(make_obj("meteors", Pv, Tt, live, mats=mats, face_mat=np.where(At[ft, 0] == 7, 3, 0).astype(int)))
            # the view from the court: the moon 200 m radius at 2.9 km, a 60 degree tall frame, looking up at her
            aim((0, -3.0, 14.5), (-.6, 1.8, 0), 62)
            shoot("mayari_%d" % k, (960, 720))
            labels[str(k)] = label
        json.dump(labels, open(os.path.join(shots, "mayari_labels.json"), "w"))
        print("RENDERED", shots)

    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else ["render"]
    {"render": render}[argv[0]]()
