"""
The seven ULTIMATE INTRODUCTIONS, authored one hero at a time.

REFINE-2.11 (owner 2026-09-24): every hero gets a moment in which the stage briefly feels like
theirs, and each is authored for that character, not copied: *"dont js spam copy paste stuff bcz
it will be boring"*. The research and the per-hero plan are in
`docs/reports/ultimate-performances-2026-09-24/`.

WHAT THIS WRITES: `Assets/TumbangPreso/Resources/UltimateIntros/<hero>.txt`, which
`HeroAbilityClips.BuildUltimateIntroduction` and `HeroIntroductionScene` read at runtime: the
duration, the body keys (RAW Unity local euler angles per bone, so C# does no conversion), the
authored lift off the floor, punch keys, the shots and the locked reduced-motion shot, and the
moment the hero's own voice plays.

WHY A TABLE HERE RATHER THAN KEYS IN C#: the same table drives `--preview`, which skins the real
glb from the real shot so every beat is LOOKED AT before the next hero starts. A pose that is only
typed is not reviewed. This follows `tools/author_hero_action.py`, which is where the shipping
cast clips already live.

CONVENTIONS for the helpers below (Unity space, from `HeroAbilityClips.cs`):
  torso(x, y, z): +x leans forward, +y twists to the character's left, +z leans left.
  head(x, y, z):  +x tilts down, -x up, +y turns left, +z tilts left.
  arm(raise, spread, twist): raise swings the arm forward and up from hanging (90 = pointing
                  forward, 180 = straight up); spread takes it away from the side (15 = rest,
                  90 = straight out); twist swings it round the vertical axis.
  leg(swing, spread): +swing forward, spread away from the other leg.
Run:  python tools/author_ultimate_intros.py            (write every hero)
      python tools/author_ultimate_intros.py --preview phaister
"""

import argparse
import math
import os

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
OUT = os.path.join(ROOT, "Assets", "TumbangPreso", "Resources", "UltimateIntros")
MODELS = os.path.join(ROOT, "Assets", "TumbangPreso", "Art", "characters", "persons")
PREVIEW = os.path.join(ROOT, "docs", "reports", "ultimate-performances-2026-09-24", "previews")

BONES = ("torso", "head", "arm-left", "arm-right", "leg-left", "leg-right")


def arm_raw(side, raise_, spread, twist=0.0):
    # PoseKey's transcription: drop from the T-pose by 80, then pitch. Left spreads with +z,
    # right with -z, in Unity's imported handedness.
    if side == "left":
        return (-raise_, twist, 80.0 - spread)
    return (-raise_, -twist, -(80.0 - spread))


def leg_raw(side, swing, spread=0.0):
    return (-swing, 0.0, -spread if side == "left" else spread)


class Pose:
    """One held shape. Everything defaults to the neutral stand."""

    def __init__(self, torso=(0, 0, 0), head=(0, 0, 0), left=(0, 15, 0), right=(0, 15, 0),
                 legs=((0, 0), (0, 0))):
        self.torso, self.head, self.left, self.right, self.legs = torso, head, left, right, legs

    def raw(self):
        return {
            "torso": tuple(self.torso),
            "head": tuple(self.head),
            "arm-left": arm_raw("left", *self.left),
            "arm-right": arm_raw("right", *self.right),
            "leg-left": leg_raw("left", *self.legs[0]),
            "leg-right": leg_raw("right", *self.legs[1]),
        }

    def but(self, **changes):
        p = Pose(self.torso, self.head, self.left, self.right, self.legs)
        for k, v in changes.items():
            setattr(p, k, v)
        return p


REST = Pose()


class Performance:
    def __init__(self, hero, seconds):
        self.hero, self.seconds = hero, seconds
        self.keys, self.punches, self.lift, self.shots = [], [], [], []
        self.voice = None
        self.still = None
        self.notes = []

    def key(self, t, pose, punch=False):
        self.keys.append((round(t, 3), pose))
        if punch:
            self.punches.append(round(t, 3))
        return self

    def hold(self, t0, t1, pose):
        """A held shape: the same pose at both ends, so the curve rests between them."""
        return self.key(t0, pose).key(t1, pose)

    def rise(self, t, metres):
        self.lift.append((round(t, 3), metres))
        return self

    def shot(self, start, end, eye, look, fov, eye_to=None, look_to=None, fov_to=None, close=False, fit=False):
        # Hero-local, Unity space: +x to the hero's right (on screen LEFT when the camera is
        # in front of them), y up, +z in front of the hero. `close` crops the body on purpose; `fit`
        # widens to keep every body (Nemu and Kuro) in frame.
        self.shots.append((start, end, eye, eye_to or eye, look, look_to or look, fov, fov_to or fov, close, fit))
        return self

    def locked(self, eye, look, fov):
        # The one shot reduced-motion players see for the whole performance: no cut, no move.
        self.still = (eye, look, fov)
        return self

    def write(self):
        os.makedirs(OUT, exist_ok=True)
        lines = [
            "# Written by tools/author_ultimate_intros.py. Edit the table there, not this file.",
            f"seconds {self.seconds:g}",
        ]
        if self.voice:
            lines.append(f"voice {self.voice[0]:g} {self.voice[1]}")
        for t, pose in sorted(self.keys, key=lambda k: k[0]):
            raw = pose.raw()
            values = " ".join(f"{v:g}" for b in BONES for v in raw[b])
            lines.append(f"key {t:g} {values}")
        for t in self.punches:
            lines.append(f"punch {t:g}")
        for t, m in self.lift:
            lines.append(f"lift {t:g} {m:g}")
        for s in self.shots:
            nums = [s[0], s[1], *s[2], *s[3], *s[4], *s[5], s[6], s[7]]
            flags = (" close" if s[8] else "") + (" fit" if s[9] else "")
            lines.append("shot " + " ".join(f"{v:g}" for v in nums) + flags)
        if self.still:
            e, l, f = self.still
            lines.append("still " + " ".join(f"{v:g}" for v in (*e, *l, f)))
        path = os.path.join(OUT, self.hero + ".txt")
        with open(path, "w", newline="\n") as fh:
            fh.write("\n".join(lines) + "\n")
        _ensure_meta(path, folder=False)
        _ensure_meta(OUT, folder=True)
        return path


def _ensure_meta(path, folder):
    # Unity needs a .meta beside every asset; write one once with a fresh GUID, never rewrite it
    # (a changed GUID would break every reference to the asset).
    meta = path + ".meta"
    if os.path.exists(meta):
        return
    import uuid
    body = (f"fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\n" +
            ("folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
             if folder else
             "TextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"))
    with open(meta, "w", newline="\n") as fh:
        fh.write(body)


# ----------------------------------------------------------------------------- sampling
# The preview samples the same keys with the same shape of curve Unity builds: Catmull-Rom
# slopes, with a punch key arriving fast and stopping dead. Close enough to judge a pose; the
# engine is still the judge of timing.

def _sample_track(keys, t, punches):
    if t <= keys[0][0]:
        return keys[0][1]
    if t >= keys[-1][0]:
        return keys[-1][1]
    for i in range(len(keys) - 1):
        (t0, v0), (t1, v1) = keys[i], keys[i + 1]
        if t0 <= t <= t1:
            u = (t - t0) / max(1e-6, t1 - t0)
            if t1 in punches:
                u = u ** 2.1
            elif t0 in punches:
                u = 1 - (1 - u) ** 1.6
            else:
                u = u * u * (3 - 2 * u)
            return v0 + (v1 - v0) * u
    return keys[-1][1]


def sample(perf, t):
    keys = sorted(perf.keys, key=lambda k: k[0])
    raws = [(k, p.raw()) for k, p in keys]
    out = {}
    for b in BONES:
        out[b] = tuple(_sample_track([(k, r[b][i]) for k, r in raws], t, perf.punches) for i in range(3))
    lift = _sample_track(sorted(perf.lift), t, []) if perf.lift else 0.0
    return out, lift


def shot_at(perf, t):
    for s in perf.shots:
        if s[0] <= t < s[1] or s is perf.shots[-1]:
            u = min(1, max(0, (t - s[0]) / max(1e-6, s[1] - s[0])))
            u = u * u * (3 - 2 * u)
            lerp = lambda a, b: tuple(x + (y - x) * u for x, y in zip(a, b))
            return lerp(s[2], s[3]), lerp(s[4], s[5]), s[6] + (s[7] - s[6]) * u
    return (2.2, 1.1, 4.5), (0, 1.05, 0), 46


# ----------------------------------------------------------------------------- heroes
# Each hero is its own function, written from its own plan section. Shared helpers above only
# carry the rig's conventions; no pose, beat, shot or timing is shared between heroes.

PERFORMANCES = {}
HELD = {}


def performance(fn):
    PERFORMANCES[fn.__name__] = fn
    return fn


def build(hero, legacy=False):
    if legacy or hero not in PERFORMANCES:
        return LEGACY[hero]()
    return PERFORMANCES[hero]()




@performance
def phaister():
    """
    GRAND COVEN, 4.2 s. Delighted mischief (plan.md section 2): she enjoys the setup more than
    winning, so the performance is a joke she is in on. A sly hat tip, a chuckle she tries to
    hold in, then the laugh breaks out and LIFTS her off the ground (owner's direction), she
    draws the eclipse overhead at the top, claims the ground with one pointed hand and lands.
    The five laugh syllables of `hero_phaister_ult` (0, .17, .34, .49, .62 of 1.55 s) each
    throw her head and chest back: the body laughs WITH the voice, not beside it.
    """
    p = Performance("phaister", 4.2)
    p.voice = (1.15, "hero_phaister_ult")

    rest = Pose(left=(0, 15, 0), right=(0, 15, 0))
    # The sly beat: hand to the hat brim, head dipped and turned to the camera side, the other
    # hand on the hip, weight on one leg.
    sly = Pose(torso=(3, -8, 4), head=(10, -16, 6), left=(12, 38, -10), right=(150, 40, 18),
               legs=((4, 4), (-2, 6)))
    # The chuckle she is holding in: hand brought in front of the chin, head tilted, shoulders up.
    hold_in = Pose(torso=(5, -4, 2), head=(6, -10, 10), left=(18, 34, -10), right=(112, 10, 40),
                   legs=((2, 3), (0, 4)))
    hold_hop = hold_in.but(torso=(-2, -4, 2), head=(0, -10, 12))
    # The laugh: chest open, arms flung up and out past the shoulders, legs trailing together.
    # ⚠️ THE HEAD GOES BACK ONLY 12 DEGREES. Further and the wide brim turns into a flat slab
    # toward the camera (the Hex complaint in the gameplay-animation report); the laugh reads
    # from the shaking chest and the open arms instead.
    laugh = Pose(torso=(-9, 0, 0), head=(-10, -6, 0), left=(34, 90, 0), right=(34, 90, 0),
                 legs=((-8, 1), (-12, 1)))
    ha = laugh.but(torso=(-15, 0, 0), head=(-14, -6, 0), left=(40, 98, 0), right=(40, 98, 0))
    # ⚠️ THE CAST IS CHIBI: THE ARMS ARE ABOUT A HEAD LONG. Measured on the real mesh, hands
    # raised overhead end inside the hat brim and hair and vanish from every front shot, so the
    # eclipse is drawn with the arms FORWARD and up, presenting the moon, hands in front of the
    # brim where they read.
    draw = Pose(torso=(-5, 10, 0), head=(-12, 6, 0), left=(135, 30, 0), right=(135, 30, 0),
                legs=((-10, 2), (-14, 2)))
    open_ = Pose(torso=(-4, -8, 0), head=(-10, -4, 0), left=(118, 62, 0), right=(118, 62, 0),
                 legs=((-10, 2), (-14, 2)))
    # The claim: one hand snaps down to point at the ground ahead, the other stays up and out.
    # Head only a little down: any more and the brim covers her face in every shot.
    claim = Pose(torso=(6, -6, 0), head=(5, -8, 0), left=(135, 45, 0), right=(42, 10, 0),
                 legs=((-6, 3), (-10, 3)))
    land = claim.but(torso=(12, -6, 0), head=(4, -8, 0), legs=((4, 5), (2, 5)))
    settle = claim.but(torso=(5, -6, 0), head=(3, -8, 0), legs=((2, 5), (0, 5)))

    p.key(0, rest)
    p.hold(.34, .66, sly)
    p.key(.80, hold_in).key(.88, hold_hop).key(.96, hold_in).key(1.04, hold_hop)
    p.key(1.15, laugh)
    for ts in (1.15, 1.41, 1.68, 1.91, 2.11):
        p.key(ts + .06, ha).key(ts + .17, laugh)
    p.key(2.30, laugh)
    p.key(2.55, draw)
    p.key(3.05, draw.but(torso=(-5, -10, 0)))
    p.key(3.30, open_)
    p.key(3.50, claim, punch=True)
    p.hold(3.50, 3.72, claim)
    p.key(3.95, land)
    p.key(4.20, settle)

    # The levitation: up with the laugh, hanging at the top for the eclipse, down for the claim.
    p.rise(0, 0).rise(1.12, 0).rise(1.70, .30).rise(2.30, .55).rise(3.45, .58).rise(3.92, 0).rise(4.2, 0)

    # Shot distances are real metres: a person glb is scaled by PersonScale 2.38, which makes
    # Phaister 2.38 m to the top of her hat.
    # A: waist-up on her and the hat for the sly beat and the held-in chuckle.
    p.shot(0, 1.08, (1.75, 1.85, 3.8), (0, 1.5, 0), 42, eye_to=(1.55, 1.8, 3.4), close=True)
    # B: low from her right front, tilting up as she rises and laughs.
    p.shot(1.08, 2.42, (-2.1, .8, 4.3), (0, 1.3, 0), 48, eye_to=(-2.3, .75, 4.6), look_to=(0, 1.85, 0))
    # C: wide and a little below her, the eclipse filling the sky behind; follows her down.
    p.shot(2.42, 4.2, (1.3, 1.05, 6.1), (0, 2.1, 0), 52, eye_to=(1.1, 1.05, 5.5), look_to=(0, 1.35, 0))
    p.locked((1.4, 1.15, 6.2), (0, 1.6, 0), 50)
    return p

# ----------------------------------------------------------------------------- the 2.8 s baseline
# The introductions as they shipped before REFINE-2.11, transcribed key for key from the old
# `HeroAbilityClips.Introductions.cs` (PoseKey arguments: raise = -x, twist = +/-y, spread = z),
# so each hero keeps working until its own new performance replaces it, and so every new sheet
# has a "before" beside it. A hero whose name is in NEW uses its new table instead.

def _legacy(hero, keys, eye, look, fov):
    p = Performance(hero, 2.8)
    for t, torso, head, left, right, *legs in keys:
        lp = legs[0] if legs else (0, 0, 0)
        rp = legs[1] if len(legs) > 1 else (0, 0, 0)
        pose = Pose(torso=torso, head=head,
                    left=(-left[0], left[2], left[1]),
                    right=(-right[0], -right[2], -right[1]),
                    legs=((-lp[0], lp[2]), (-rp[0], -rp[2])))
        p.key(t, pose)
    p.key(0, Pose(left=(0, 15, 0), right=(0, 15, 0)))
    p.shot(0, .35, eye, look, fov)
    p.shot(.35, 2.8, eye, look, fov, eye_to=tuple(v * .94 for v in eye), fit=hero == "nemu")
    p.locked(tuple(v * .97 for v in eye), look, fov)
    return p


LEGACY = {
    "sean": lambda: _legacy("sean", [
        (.30, (10, -8, 0), (8, 6, 0), (-25, 15, 22), (15, -20, -25), (-12, 0, 5), (14, 0, -5)),
        (.85, (18, -12, 0), (-8, 10, 0), (-55, 25, 18), (-35, -30, -20), (-18, 0, 8), (22, 0, -8)),
        (1.45, (14, 4, 0), (-14, -4, 0), (-60, 12, 32), (-60, -12, -32), (-18, 0, 8), (22, 0, -8)),
        (2.10, (26, 0, 0), (-18, 0, 0), (24, 0, 24), (24, 0, -24), (-24, 0, 10), (28, 0, -10)),
    ], (2.2, 1.1, 4.5), (0, 1.05, 0), 46),
    "phaister": lambda: _legacy("phaister", [
        (.45, (0, 15, -3), (-5, -12, 0), (-30, 25, 25), (-80, -25, -28)),
        (1.10, (-5, 8, 0), (-12, -5, 0), (-100, 12, 42), (-135, -12, -38)),
        (1.75, (-5, 0, 0), (-16, 0, 0), (-140, 0, 45), (-140, 0, -45)),
        (2.40, (5, 0, 0), (-5, 0, 0), (-85, 12, 40), (-85, -12, -40)),
    ], (1.8, 1.5, 5.2), (0, 1.45, 0), 48),
    "zack": lambda: _legacy("zack", [
        (.32, (3, -20, 0), (0, 18, 0), (-15, 0, 12), (-55, -20, -28), (-8, 0, 3), (10, 0, -3)),
        (.95, (-4, -20, 0), (-18, 20, 0), (8, 0, 15), (-145, -15, -12), (-8, 0, 3), (10, 0, -3)),
        (1.75, (-4, -20, 0), (-18, 20, 0), (8, 0, 15), (-145, -15, -12), (-8, 0, 3), (10, 0, -3)),
        (2.15, (8, 15, 0), (5, -10, 0), (12, 0, 15), (-80, 5, -8), (-8, 0, 3), (10, 0, -3)),
    ], (-2.8, 1.35, 4.6), (0, 1.05, 0), 44),
    "nemu": lambda: _legacy("nemu", [
        (.45, (0, -12, 0), (4, -28, -5), (-65, -15, 22), (-15, 10, -15)),
        (1.0, (6, -8, 0), (8, -20, 0), (-80, -10, 30), (-60, 15, -22)),
        (1.60, (8, 0, 0), (6, 0, 0), (-45, 28, 14), (-45, -28, -14)),
        (2.35, (-4, 0, 0), (-5, 0, 0), (-35, -18, 55), (-35, 18, -55)),
    ], (2.8, 1.5, 5.6), (-.75, 1.4, 0), 50),
    "dante": lambda: _legacy("dante", [
        (.45, (8, -14, 0), (-4, 12, 0), (-25, 0, 30), (20, -10, -30), (0, -6, 5), (0, 6, -5)),
        (1.05, (12, -26, -5), (-8, 24, 0), (-55, 15, 24), (45, -25, -35), (0, -8, 8), (0, 8, -8)),
        (1.80, (18, -30, -6), (-12, 28, 0), (-65, 8, 30), (65, -30, -32), (0, -10, 10), (0, 10, -10)),
    ], (-3, 1, 4.3), (0, .8, 0), 50),
    "cheska": lambda: _legacy("cheska", [
        (.55, (0, 8, 0), (8, -8, 0), (-65, 16, 18), (-30, -12, -16)),
        (1.25, (0, 4, 0), (7, -4, 0), (-70, 18, 24), (-70, -18, -24)),
        (1.90, (3, 0, 0), (4, 0, 0), (-55, 30, 15), (-55, -30, -15)),
        (2.35, (0, 0, 0), (0, 0, 0), (-85, -8, 42), (-85, 8, -42)),
    ], (1.8, 1.3, 4), (0, 1.1, 0), 43),
    "rafi": lambda: _legacy("rafi", [
        (.40, (4, 12, -3), (0, -14, 0), (-25, 10, 28), (-12, 0, -20)),
        (1.05, (8, -15, -4), (-4, 16, 0), (-38, -20, 35), (-20, -15, -25), (-6, 0, 3), (5, 0, -3)),
        (1.70, (8, -25, -6), (-4, 20, 0), (-40, -24, 32), (-26, -24, -25), (-10, 0, 5), (8, 0, -5)),
    ], (-2.6, 1.15, 4.7), (0, 1.0, 0), 47),
}

# Cheska holding a slipper gathers at her free palm; the old table widened that arm and mirrored
# the camera so the held shoe stays on the far side.
HELD["cheska"] = lambda: _legacy("cheska-held", [
    (.55, (0, 8, 0), (8, -8, 0), (-65, 16, 35), (-30, -12, -16)),
    (1.25, (0, 4, 0), (7, -4, 0), (-70, 18, 55), (-70, -18, -24)),
    (1.90, (3, 0, 0), (4, 0, 0), (-55, 30, 60), (-55, -30, -15)),
    (2.35, (0, 0, 0), (0, 0, 0), (-85, -8, 75), (-85, 8, -42)),
], (-1.8, 1.3, 4), (0, 1.1, 0), 43)


def preview(hero, times=None, out=None, legacy=False, witness=False):
    import numpy as np
    from PIL import Image
    import intro_pose_preview as ipp

    perf = build(hero, legacy)
    model = ipp.Model(os.path.join(MODELS, f"team-{hero}.glb"))
    # Unity scales every person glb by CharacterVisual.PersonScale, so shot distances here mean
    # what they mean in play.
    scale = 2.38
    times = times or [round(perf.seconds * i / 7, 2) for i in range(8)]
    frames = []
    for t in times:
        raw, lift = sample(perf, t)
        pose = {b: (ipp.quat_euler(*raw[b]), None) for b in BONES}
        tris, cols = model.skinned(pose)
        tris = tris * scale
        tris[:, :, 1] -= tris[:, :, 1].min()
        tris[:, :, 1] += lift
        eye, look, fov = shot_at(perf, t)
        if witness:
            # A fixed three-quarter front witness at mid distance: judges the POSE, not the shot.
            eye, look, fov = (2.2, 1.7, 5.4), (0, 1.15 + lift, 0), 44
        # Unity: the hero faces +z; a shot offset with +z is in front of them.
        img = ipp.render(tris, cols, np.array(eye), np.array(look), fov,
                         label=f"{hero} t={t:.2f}s lift={lift:.2f}m")
        frames.append(img)
    cols_n = 3
    rows = (len(frames) + cols_n - 1) // cols_n
    w, h = frames[0].size
    sheet = Image.new("RGB", (w * cols_n, h * rows), (255, 255, 255))
    for i, f in enumerate(frames):
        sheet.paste(f, ((i % cols_n) * w, (i // cols_n) * h))
    os.makedirs(PREVIEW, exist_ok=True)
    path = out or os.path.join(PREVIEW, f"{hero}_v1.png")
    sheet.save(path)
    return path


if __name__ == "__main__":
    import sys
    sys.path.insert(0, HERE)
    ap = argparse.ArgumentParser()
    ap.add_argument("--preview", default=None)
    ap.add_argument("--times", default=None)
    ap.add_argument("--out", default=None)
    ap.add_argument("--legacy", action="store_true", help="preview the pre-REFINE-2.11 baseline")
    ap.add_argument("--witness", action="store_true", help="fixed front witness instead of the authored shots")
    args = ap.parse_args()
    if args.preview:
        times = [float(x) for x in args.times.split(",")] if args.times else None
        print(preview(args.preview, times, args.out, args.legacy, args.witness))
    else:
        for name in LEGACY:
            print(build(name).write())
        for name in HELD:
            print(HELD[name]().write())
