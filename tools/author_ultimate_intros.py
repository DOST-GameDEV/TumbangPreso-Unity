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
  torso(x, y, z): +x leans forward, +y twists to the character's RIGHT, +z leans left.
  head(x, y, z):  +x tilts down, -x up, +y turns to the character's RIGHT, +z tilts left.
  (Measured on the skinned glb in Unity space, 2026-09-24: HeroAbilityClips.cs's header says
  "+Y turns left", which is the view from the front, i.e. the viewer's left.)
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
        self.holds = []

    def key(self, t, pose, punch=False):
        self.keys.append((round(t, 3), pose))
        if punch:
            self.punches.append(round(t, 3))
        return self

    def hold(self, t0, t1, pose):
        """A held shape. It is written as the same pose at both ends and turned into a MOVING hold
        when the table is written (`_moving_holds`): the body keeps drifting a little the way it
        was going, so no signature pose is ever dead still."""
        self.holds.append((round(t0, 3), round(t1, 3)))
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
        for t, pose in _moving_holds(self):
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


# ⚠️⚠️ REFINEMENT PASS 2 (2026-09-24): MOVING HOLDS. The first pass held every signature shape
# with the same pose at both ends (`Performance.hold`), which the curve then renders as a statue
# for up to 0.6 s: the "dead still pose" animators call a held drawing. A moving hold keeps the
# motion that arrived going, a little, and settles: the end of each hold is pushed a few per cent
# further along the direction the body came from, capped at 3 degrees per axis so the pose never
# changes what it says. The amount is the hero's character: Cheska is the one who stops (her
# holds barely drift), Phaister and Zack keep moving, Dante's holds already tremble as authored.
HOLD_DRIFT = {"sean": .06, "zack": .09, "dante": .05, "cheska": .02, "nemu": .08, "phaister": .09, "rafi": .08,
              "amihan": .10, "paete": .04}
HOLD_CAP = 3.0


def _drift(a, b, k):
    """b pushed further away from a by k of their difference, capped per axis."""
    if isinstance(b, (tuple, list)) and b and isinstance(b[0], (tuple, list)):
        return tuple(_drift(x, y, k) for x, y in zip(a, b))
    return tuple(y + max(-HOLD_CAP, min(HOLD_CAP, (y - x) * k)) for x, y in zip(a, b))


def _moving_holds(perf):
    keys = sorted(perf.keys, key=lambda k: k[0])
    k = HOLD_DRIFT.get(perf.hero.split("-")[0], .06)
    out = []
    for i, (t, pose) in enumerate(keys):
        hold = next((h for h in perf.holds if abs(h[1] - t) < 1e-6 and h[1] - h[0] >= .2), None)
        start = next((j for j in range(i) if abs(keys[j][0] - (hold[0] if hold else -1)) < 1e-6), None)
        if hold and start is not None and start > 0:
            prev = keys[start - 1][1]
            pose = pose.but(torso=_drift(prev.torso, pose.torso, k), head=_drift(prev.head, pose.head, k * 1.4),
                            left=_drift(prev.left, pose.left, k), right=_drift(prev.right, pose.right, k),
                            legs=_drift(prev.legs, pose.legs, k * .5))
        out.append((t, pose))
    return out


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
    keys = _moving_holds(perf)
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
    VOODOO DOLL, 6.35 s, v7 THE PUPPETEER (v12: THE TWIST gets a shot of its own, the rest quicker) (HERO-10 v3, 2026-09-29; `docs/reports/phaister-kit-2026-09-27/plan.md` 9.8c). The owner on
    v6: *"phaister's ult does not have a terrifying feeel at all eh"*, *"dont make her raise it up"*, *"i want her to look like she starts
    flying and is casting"*, *"show transition from day to dark too"*, *"when the eye opens the theres like a lot of vfx or smth of it
    being summoned"*, *"I want the portal (eye) to be diff from the thing that controls it too"* (a photo of gloved hands working a
    marionette control), and Flins' burst as the reference: darkness first, cool and terrifying.

    One sentence: "The day dies, she rises into the dark, an eye tears open in the sky and LOOKS AT YOU, and out of it two huge hands
    lower a puppet on wires."
      1 THE DAY DIES (0 to 1.10): daylight; she lowers her head and the dark spreads down the sky and out across the ground from her.
      2 SHE RISES (1.10 to 2.10): her feet leave the court, limp at first as if lifted; then her arms spread low, casting; pins orbit
        her; light is drawn up out of the ground into her palms; at 2.05 her head snaps up and her arms flare.
      3 THE SEAM (2.10 to 2.90): the sky splits along a stitched seam over her right, the stitches snapping; the circle sews round it;
        her pins fly up and stab into its rim.
      4 THE EYE OPENS (2.90 to 3.60): the lids peel apart, the pupil darts and LOCKS ON THE LENS (impact frame), then THE BURST.
      5 THE PUPPETEER (3.60 to 4.60): two huge mitten gloves push out of the pupil working a marionette control; they PULL (impact
        frame) and the doll is dragged out head-first, upside down, and swings through to hang under the control.
      6 THE DESCENT (4.60 to 5.40): lowered in three jerks; its head turns round too far to face the lens, then its body follows.
      7 THE DROP (5.40 to 5.80): the wires go slack, it lands (impact frame); the real opponents stagger; she floats down.
      8 THE PUPPET (5.80 to 6.40): the gloves jerk the control: its head snaps up at them (impact frame), it lurches; the gloves draw
        back into the eye, the control stays over its head for play.
    The eye, the gloves, the control, the doll and the staged opponents are `HeroIntroductionScene.Phaister.cs`; this table is her body
    and the shots (shots 3 to 8 are computed there; these rows are their fallback).

    ⚠️ THE CAST IS CHIBI: THE ARMS ARE ABOUT A HEAD LONG, and hands overhead vanish inside the brim, so her cast is LOW and WIDE.
    ⚠️ THE HEAD GOES BACK ONLY 10 DEGREES: further and the wide brim turns into a flat slab toward the camera.
    """
    p = Performance("phaister", 6.0)
    p.voice = (1.3, "hero_phaister_ult")

    rest = Pose(left=(0, 15, 0), right=(0, 15, 0))
    # THE DAY DIES: her head goes down, the arms hang a little open.
    bow = Pose(torso=(6, 0, 0), head=(18, 0, 0), left=(4, 24, 0), right=(4, 24, 0))
    # Lifted limp: hanging from nothing, head lolled, arms dangling.
    limp = Pose(torso=(10, 0, 4), head=(24, 0, 10), left=(0, 20, 0), right=(0, 20, 0), legs=((-6, 3), (4, 3)))
    # Casting: arms spread low and wide, palms down, chin level, the legs trailing.
    cast = Pose(torso=(-4, 0, 0), head=(-2, 0, 0), left=(38, 72, 0), right=(38, 72, 0), legs=((-10, 5), (6, 5)))
    cast_b = cast.but(torso=(-5, 4, -2), head=(-4, 6, 2), left=(44, 74, 0), right=(34, 70, 0))
    # The flare: head up, arms flung up and out, chest open.
    flare = Pose(torso=(-10, 0, 0), head=(-10, 0, 0), left=(80, 86, 0), right=(80, 86, 0), legs=((-12, 6), (8, 6)))
    watch_up = flare.but(torso=(-8, 0, 0), left=(62, 80, 0), right=(62, 80, 0))
    # The burst blows her back.
    blown = Pose(torso=(-14, 0, 0), head=(-10, 0, 0), left=(74, 96, 0), right=(74, 96, 0), legs=((-16, 8), (10, 8)))
    # Floating, watching it come down beside her: a hand out toward it.
    watch = Pose(torso=(-6, 14, 0), head=(-6, 16, 0), left=(30, 50, 0), right=(52, 56, 0), legs=((-8, 5), (6, 5)))
    flinch = watch.but(torso=(2, 12, 0), head=(0, 18, 0), left=(40, 34, 0), right=(46, 30, 0))
    # THE PUPPET: her smirk behind it, head tipped, a hand up to her hat brim, the other on her hip.
    smirk = Pose(torso=(-2, -14, 4), head=(2, 16, 12), left=(10, 50, -70), right=(150, 10, 10), legs=((-2, 6), (8, 6)))

    p.key(0, rest)
    p.key(.35, bow)
    p.hold(.35, .8, bow)
    p.key(1.1, limp)
    p.key(1.4, cast)
    p.hold(1.4, 1.55, cast)
    p.key(1.62, cast_b)
    p.key(1.72, flare, punch=True)
    p.key(2.05, watch_up)
    p.hold(2.05, 2.74, watch_up)
    p.key(2.84, blown, punch=True)
    p.key(3.2, watch_up)
    p.key(3.8, watch)
    p.hold(3.8, 6.0, watch)

    # She rises 0.8 m and floats there to the end, bobbing (v19 to v22 sent her 3 m up into the sky to clear THE STARE, the owner:
    # *"why tf is she flying away"*). THE STARE is its close-up alone: the scene leaves her out of that one shot.
    p.rise(0, 0).rise(.9, 0).rise(1.2, .25).rise(1.65, .8).rise(1.9, .86).rise(2.8, .8).rise(2.92, .95).rise(3.6, .85) \
        .rise(4.9, .8).rise(6.0, .86)

    # Shot distances are real metres (she is 2.38 m to the hat tip). Hero-local: +x her right, +z in front of her.
    # THE DAY DIES: wide and low in front, the whole sky in frame, nearly still.
    p.shot(0, .9, (1.4, .9, 6.2), (.3, 2.6, 0), 62, eye_to=(1.3, .9, 5.8), look_to=(.3, 2.5, 0), fov_to=60)
    # SHE RISES: a slow push up her body to her face from below, her pins round her and her sigil under her.
    p.shot(.9, 1.7, (.8, .55, 3.9), (0, 1.5, 0), 52, eye_to=(.6, .9, 3.3), look_to=(0, 2.35, 0), fov_to=48)
    # THE SEAM: from under her, tilting up past her to the seam over her right (computed from the circle).
    p.shot(1.7, 2.4, (.9, .6, 1.8), (0, 2.8, 0), 55, eye_to=(.2, .5, 2.2), look_to=(1.3, 7.0, 0), fov_to=66)
    # THE EYE OPENS: straight up at it, filling the frame, then thrown out by THE BURST (computed).
    p.shot(2.4, 3.0, (1.3, 2.8, .3), (1.3, 7.0, 0), 60, eye_to=(1.3, 2.4, .4), look_to=(1.3, 7.0, 0), fov_to=74)
    # THE PUPPETEER: under the eye, the gloves coming at the lens (computed).
    p.shot(3.0, 3.85, (3.1, 1.6, 3.8), (1.3, 5.0, 0), 58, eye_to=(3.3, 1.2, 4.2), look_to=(1.3, 4.2, 0), fov_to=62)
    # THE DESCENT: from the court looking up, it coming down (computed).
    p.shot(3.85, 4.45, (1.9, .7, 3.6), (1.3, 3.0, 0), 56, eye_to=(1.8, .6, 3.4), look_to=(1.3, 2.0, 0), fov_to=58)
    # THE TWIST and THE STARE: the gloves crank the control, then in behind its head as its face comes round into the lens, and it
    # holds on its stare to the end, the dark closing round its eyes (computed). The cutscene ENDS here (the owner: "just end it here").
    p.shot(4.45, 6.0, (2.4, 1.8, 4.4), (1.3, 3.2, 0), 54, eye_to=(1.6, 2.2, 2.0), look_to=(1.3, 2.3, 0), fov_to=40, close=True)
    p.locked((1.2, 1.8, 6.2), (.8, 2.4, .3), 60)
    return p


@performance
def sean():
    """
    SUPERNOVA, 3.4 s. Patience turning into commitment (plan.md section 1): "Waits for one
    opening. Makes it count." He is a lantern maker, so the fire is ASSEMBLED, not summoned:
    a parol frame of five sticks builds between his cupped hands one stick at a time while he
    watches it. Then the head snaps up (the opening), a held breath, the coil, and the rise
    that is the first frame of the live leap.
    Measured on his real mesh: twist 38 brings both hands together in front of the chest.
    """
    p = Performance("sean", 3.4)
    rest = Pose(left=(0, 15, 0), right=(0, 15, 0))
    plant = Pose(torso=(6, 8, 0), head=(16, -4, 0), left=(6, 18, 0), right=(6, 18, 0),
                 legs=((4, 7), (-4, 7)))
    roll = plant.but(torso=(6, -8, 0), head=(16, 4, 0))
    # Cupped hands at the chest, head bowed over them: the craftsman.
    cup = Pose(torso=(10, 0, 0), head=(24, 0, 0), left=(75, 5, 38), right=(75, 5, 38),
               legs=((4, 7), (-4, 7)))
    inspect_ = cup.but(head=(22, 0, -6), torso=(12, 0, 0))
    inspect2 = cup.but(head=(22, 0, 5), torso=(11, 0, 0))
    # The opening: the head snaps up to the target, hands still cupped. Held.
    look = cup.but(torso=(4, 0, 0), head=(-6, 0, 0))
    # The coil: chest down over the front leg, arms swept back, the lantern pulled in.
    coil = Pose(torso=(30, 0, 0), head=(-14, 0, 0), left=(-42, 22, 0), right=(-42, 22, 0),
                legs=((18, 5), (-22, 5)))
    # The rise: arms driving up, onto the toes, the first frame of the leap.
    rise = Pose(torso=(-8, 0, 0), head=(-14, 0, 0), left=(165, 22, 0), right=(165, 22, 0),
                legs=((-4, 3), (-10, 3)))

    p.key(0, rest)
    p.key(.22, plant).key(.38, roll).key(.5, plant)
    p.key(.72, cup)
    p.key(1.05, inspect_).key(1.35, inspect2).key(1.62, cup)
    p.key(1.80, look, punch=True)
    p.hold(1.80, 2.22, look)
    p.key(2.55, coil)
    p.hold(2.55, 2.78, coil)
    p.key(2.98, rise, punch=True)
    p.hold(2.98, 3.4, rise)
    p.rise(0, 0).rise(2.9, 0).rise(3.1, .12).rise(3.4, .14)

    # A: three-quarter front medium while he plants and builds the lantern.
    p.shot(0, 1.75, (1.9, 1.35, 4.1), (0, 1.0, 0), 44, eye_to=(1.6, 1.3, 3.6))
    # B: close on the lantern and his face as the head snaps up.
    p.shot(1.75, 2.3, (1.1, 1.45, 3.1), (0, 1.25, .2), 40, eye_to=(1.0, 1.45, 2.85), close=True)
    # C: low and to his side, so the arms swept back in the coil read, then the rise.
    p.shot(2.3, 3.4, (-2.9, .5, 3.3), (0, 1.0, 0), 50, eye_to=(-2.6, .55, 3.4), look_to=(0, 1.35, 0))
    p.locked((1.7, 1.15, 4.8), (0, 1.05, 0), 46)
    return p


@performance
def zack():
    """
    THUNDERSTRIKE, 2.8 s, the shortest on purpose (plan.md section 3): "Finds the angle before
    you see the opening", and makes difficult plays look casual. So nothing here strains: a hip
    cocked, a spark flicked off a fingertip, one lazy finger to the sky that the storm answers,
    then he SIGHTS the shot down his arm like a trick shot, snaps it down, and shrugs.
    The old intro raised his hand in front of his own face for a second; every pose here keeps
    the face clear.
    """
    p = Performance("zack", 2.8)
    rest = Pose(left=(0, 15, 0), right=(0, 15, 0))
    cocky = Pose(torso=(0, -6, 6), head=(0, -12, -10), left=(6, 34, -12), right=(58, 18, 24),
                 legs=((2, 3), (-2, 8)))
    flick = cocky.but(right=(74, 22, 24))
    # One finger to the sky. Straight up vanished behind his hair on the sheet, so the arm goes
    # up and a little forward and out, where it is a clear diagonal from every shot.
    point_up = Pose(torso=(-4, -4, 4), head=(-18, -6, -6), left=(6, 34, -12), right=(158, 44, 0),
                    legs=((2, 3), (-2, 8)))
    # Lining it up: the arm straight down the line, the other hand low as a guide, head along it.
    sight = Pose(torso=(4, -16, 0), head=(4, -10, 8), left=(70, 22, 22), right=(88, 6, 12),
                 legs=((10, 4), (-12, 4)))
    snap = Pose(torso=(10, -8, 0), head=(6, -6, 4), left=(40, 26, 10), right=(22, 34, 0),
                legs=((10, 4), (-12, 4)))
    shrug = Pose(torso=(0, -4, 4), head=(-2, -10, -12), left=(6, 34, -12), right=(12, 30, -8),
                 legs=((2, 3), (-2, 8)))

    p.key(0, rest)
    p.key(.26, cocky).key(.40, flick, punch=True).key(.50, cocky)
    p.key(.66, point_up)
    p.hold(.66, 1.12, point_up)
    p.key(1.30, sight)
    p.hold(1.30, 1.74, sight)
    p.key(1.88, snap, punch=True)
    p.hold(1.88, 2.02, snap)
    p.key(2.32, shrug)
    p.hold(2.32, 2.8, shrug)

    # A: low from his left side for the cocky stance and the finger to the sky.
    p.shot(0, .98, (-2.7, .75, 2.8), (0, 1.15, 0), 46, eye_to=(-2.5, .7, 2.9))
    # B: a profile from his right, level with the arm, so the arm and the line it sights run
    # across the frame: the angle he found. (Over the shoulder was tried and his head, which is
    # most of him, hid the arm completely.)
    p.shot(.98, 1.82, (3.6, 1.25, .5), (0, 1.1, 1.4), 46, eye_to=(3.35, 1.22, .6))
    # C: front and low for the snap and the shrug.
    p.shot(1.82, 2.8, (.95, .65, 3.7), (0, 1.2, 0), 46, eye_to=(.85, .7, 3.4))
    p.locked((-2.0, 1.1, 4.3), (0, 1.1, 0), 46)
    return p


@performance
def nemu(held=False):
    """
    DEVOURING SEANCE, 3.8 s (plan.md section 4). "Looks distracted. Already knows your next
    move." Two characters act: Nemu is gazing at nothing, hands behind her back; Kuro nudges
    her; she turns to him and offers a hand; then she opens it and looks straight at the viewer,
    the one beat she is fully present, while Kuro swells behind her. She stays calm, hands
    folded, and points him ahead.
    Kuro sits on her LEFT (hero-local -x), which the camera keeps in frame.
    """
    p = Performance("nemu-held" if held else "nemu", 3.8)
    rest = Pose(left=(0, 15, 0), right=(0, 15, 0))
    # Gazing away up to her right, hands clasped behind her back.
    away = Pose(torso=(-2, 8, 2), head=(-16, 30, 4), left=(-24, 8, -30), right=(-24, 8, -30))
    away_sway = away.but(torso=(-2, 8, -2), head=(-18, 32, 6))
    startle = away.but(torso=(-5, 2, 0), head=(-6, 10, 0))
    to_kuro = Pose(torso=(0, -8, 0), head=(4, -34, 6), left=(-10, 10, -20), right=(-20, 8, -30))
    offer = Pose(torso=(3, -12, 0), head=(6, -30, 10), left=(72, 30, -10), right=(-18, 8, -30))
    # The knowing look, straight down the lens, the open hand still out.
    knowing = Pose(torso=(0, 2, 0), head=(2, 0, 0), left=(48, 34, -6), right=(-10, 12, -20))
    # Calm while the giant grows: hands folded in front, a slow sway.
    calm = Pose(torso=(0, 0, 2), head=(0, 0, 6), left=(30, 2, 38), right=(30, 2, 38))
    calm_sway = calm.but(torso=(0, 0, -2), head=(0, 0, -4))
    send = Pose(torso=(4, -6, 0), head=(4, -4, 0), left=(28, 4, 36), right=(90, 6, 6),
                legs=((6, 2), (-4, 2)))
    if held:
        # Native review caught the fitted shoe crossing her large face during
        # the folded-hand/send beat. Keep it loosely by her outside hip and
        # send Kuro with the same free hand that just offered him a nuzzle.
        shoe = (8, 26, -5)
        away = away.but(right=shoe)
        away_sway = away_sway.but(right=shoe)
        startle = startle.but(right=shoe)
        to_kuro = to_kuro.but(right=shoe)
        offer = offer.but(right=shoe)
        knowing = knowing.but(right=shoe)
        calm = calm.but(right=shoe)
        calm_sway = calm_sway.but(right=shoe)
        send = send.but(left=(80, 30, 0), right=shoe)

    p.key(0, rest)
    p.key(.3, away).key(.55, away_sway)
    p.key(.72, startle, punch=True)
    p.key(.9, to_kuro)
    p.hold(.9, 1.12, to_kuro)
    p.key(1.3, offer)
    p.hold(1.3, 1.68, offer)
    p.key(1.86, knowing, punch=True)
    p.hold(1.86, 2.28, knowing)
    p.key(2.5, calm).key(2.85, calm_sway).key(3.15, calm)
    p.key(3.36, send, punch=True)
    p.hold(3.36, 3.8, send)

    # A: from her left front so Kuro, on her left, shares the frame with her.
    p.shot(0, 1.76, (-1.5, 1.2, 3.6), (-.4, .95, 0), 46, eye_to=(-1.3, 1.15, 3.25))
    # B: straight on, close, for the knowing look into the lens.
    p.shot(1.76, 2.26, (.05, 1.25, 3.1), (0, 1.05, 0), 40, eye_to=(.05, 1.22, 2.85), close=True)
    # C: the retained reveal that backs out to keep both bodies as Kuro grows.
    p.shot(2.26, 3.8, (2.8, 1.5, 5.6), (-.75, 1.4, 0), 50, eye_to=(2.63, 1.41, 5.26), fit=True)
    p.locked((3.4, 2.4, 8.6), (-.9, 2.1, 0), 52)
    return p


HELD["nemu"] = lambda: nemu(True)
HOLD_DRIFT["nemu-held"] = HOLD_DRIFT["nemu"]


@performance
def dante():
    """
    TITAN FISSURE, 3.8 s (plan.md section 5). "Holds the difficult space. Refuses to be rushed."
    Weight is shown by SLOWNESS: he plants, gets under something heavy, stands into it and
    pushes two stone slabs apart overhead (the divided-mountain image he grew up with, as his
    own), holds it, loads both fists over his right shoulder and stamps forward into the
    strike the live fissure continues. Every hold trembles slightly: effort, not stillness.
    Signs measured on the sheets: +twist brings an arm inward, +torso yaw turns to his right.
    """
    p = Performance("dante", 3.8)
    rest = Pose(left=(0, 15, 0), right=(0, 15, 0))
    plant = Pose(torso=(6, 0, 0), head=(18, 0, 0), left=(8, 24, 0), right=(8, 24, 0),
                 legs=((2, 14), (-2, 14)))
    brace = Pose(torso=(22, 0, 0), head=(2, 0, 0), left=(42, 12, 22), right=(42, 12, 22),
                 legs=((4, 16), (-4, 16)))
    brace_shake = brace.but(torso=(23, 0, 1.5))
    # The push: arms up and out, hands clear of his head, chest open, looking up at it.
    push = Pose(torso=(-6, 0, 0), head=(-12, 0, 0), left=(150, 58, 0), right=(150, 58, 0),
                legs=((0, 16), (0, 16)))
    push_shake = push.but(torso=(-6, 0, -1.5), left=(152, 60, 0))
    # The load: both fists together over his right shoulder, chest turned right.
    load = Pose(torso=(-4, 22, 0), head=(0, -14, 0), left=(150, 20, 50), right=(150, 20, -8),
                legs=((8, 12), (-10, 12)))
    # The stamp: driving forward and down over the front leg.
    stamp = Pose(torso=(28, -10, 0), head=(-8, 6, 0), left=(72, 6, 30), right=(72, 6, 30),
                 legs=((22, 10), (-18, 10)))

    p.key(0, rest)
    p.key(.45, plant).key(.8, plant.but(torso=(7, 0, 0)))
    p.key(1.12, brace).key(1.25, brace_shake).key(1.38, brace).key(1.48, brace_shake)
    p.key(1.82, push)
    for i, t in enumerate((1.95, 2.08, 2.2, 2.32)):
        p.key(t, push_shake if i % 2 == 0 else push)
    p.key(2.66, load)
    p.hold(2.66, 2.98, load)
    p.key(3.18, stamp, punch=True)
    p.hold(3.18, 3.8, stamp)

    # A: low and in front, slow, while he plants and gets under the weight.
    p.shot(0, 1.5, (1.4, .6, 3.8), (0, 1.0, 0), 48, eye_to=(1.15, .58, 3.35))
    # B: wide and very low, the slabs parting behind him.
    p.shot(1.5, 2.5, (-1.7, .35, 4.7), (0, 1.45, -1.0), 54, eye_to=(-1.9, .32, 5.0))
    # C: low front-side for the load and the stamp, the seams running toward the lens.
    p.shot(2.5, 3.8, (2.5, .6, 3.5), (0, .9, .6), 48, eye_to=(2.2, .55, 3.1))
    p.locked((1.3, .9, 5.4), (0, 1.2, -1.0), 52)
    return p


def _cheska(held):
    """
    GLACIAL NOVA, 3.2 s (plan.md section 6). "Reads the space. Leaves you the harder route."
    Quiet and precise: she stands still and READS the court with a slow head turn, draws one
    exact line of frost in the air with a fingertip, closes her hands round it into a crystal,
    lifts it to eye height, a beat, and snaps her hands apart: the frame before the live nova.
    Holding a slipper, the free (left) hand does the fine work and the shoe stays low and out
    of the way, and the camera takes her other side so the shoe is on the far side.
    """
    p = Performance("cheska-held" if held else "cheska", 3.2)
    rest = Pose(left=(0, 15, 0), right=(0, 15, 0))
    shoe = (18, 26, -6)  # the slipper hand, low and clear of the body
    read_l = Pose(torso=(0, -4, 0), head=(2, -24, 0), left=(4, 12, 0), right=shoe if held else (4, 12, 0))
    read_r = read_l.but(torso=(0, 4, 0), head=(2, 24, 0))
    if held:
        draw_a = Pose(torso=(2, -6, 0), head=(8, -10, 0), left=(82, 10, 6), right=shoe)
        draw_b = draw_a.but(left=(86, 22, -22), head=(8, -18, 0))
        close = Pose(torso=(4, -4, 0), head=(14, -8, 0), left=(72, 4, 30), right=shoe)
        lift = close.but(left=(104, 4, 30), head=(-2, -8, 6))
        snap = Pose(torso=(-4, 0, 0), head=(-4, 0, 0), left=(72, 74, 0), right=(30, 40, 0))
    else:
        draw_a = Pose(torso=(2, 6, 0), head=(8, 10, 0), left=(4, 12, 0), right=(82, 10, 6))
        draw_b = draw_a.but(right=(86, 22, -22), head=(8, 18, 0))
        close = Pose(torso=(4, 0, 0), head=(14, 0, 0), left=(70, 4, 36), right=(70, 4, 36))
        lift = close.but(left=(102, 4, 36), right=(102, 4, 36), head=(-2, 0, 6))
        snap = Pose(torso=(-4, 0, 0), head=(-4, 0, 0), left=(72, 74, 0), right=(72, 74, 0))

    p.key(0, rest)
    p.key(.18, read_l).key(.72, read_r)
    p.key(.9, draw_a).key(1.3, draw_b)
    p.key(1.5, close)
    p.hold(1.5, 1.98, close)
    p.key(2.18, lift)
    p.hold(2.18, 2.76, lift)
    p.key(2.9, snap, punch=True)
    p.hold(2.9, 3.2, snap)

    m = -1 if held else 1  # mirror the camera so a held shoe stays on the far side
    # A: a still, close medium: nothing moves but her eyes and one hand.
    p.shot(0, 1.42, (m * 1.8, 1.45, 4.1), (0, 1.1, 0), 42, eye_to=(m * 1.62, 1.42, 3.75), close=True)
    # B: close on the hands as the frost gathers into the crystal.
    p.shot(1.42, 2.12, (m * .95, 1.05, 3.1), (0, .9, .3), 40, eye_to=(m * .85, 1.05, 2.85), close=True)
    # C: wider and low for the lift, the frost spreading and the snap.
    p.shot(2.12, 3.2, (m * -1.6, .7, 3.7), (0, 1.1, 0), 48, eye_to=(m * -1.45, .68, 3.45))
    p.locked((m * 1.5, 1.1, 4.6), (0, 1.05, 0), 46)
    return p


@performance
def cheska():
    return _cheska(False)


HELD["cheska"] = lambda: _cheska(True)


@performance
def rafi():
    """
    BREAKWATER, 3.4 s (plan.md section 7). "Draws you into the wrong current. Leaves with his
    slipper." A teasing competitor who likes making a rival commit too early: he feints one
    way, then the other, shrugs, beckons twice ("come on"), drops into a boat-deck crouch that
    rocks like a hull, lifts the wave up behind him and sends it with one sweeping arm.
    """
    p = Performance("rafi", 3.4)
    rest = Pose(left=(0, 15, 0), right=(0, 15, 0))
    feint_l = Pose(torso=(4, -12, 8), head=(0, -16, 4), left=(10, 30, 0), right=(18, 18, 0),
                   legs=((4, 10), (-4, 10)))
    feint_r = Pose(torso=(4, 12, -8), head=(0, 16, -4), left=(18, 18, 0), right=(10, 30, 0),
                   legs=((-4, 10), (4, 10)))
    shrug = Pose(torso=(-3, 0, 0), head=(-4, 0, 10), left=(20, 34, 0), right=(20, 34, 0),
                 legs=((2, 6), (-2, 6)))
    beckon = Pose(torso=(0, -6, 0), head=(0, -6, -8), left=(80, 16, 22), right=(6, 36, -10),
                  legs=((2, 6), (-2, 6)))
    beckon_pull = beckon.but(left=(94, 12, -4))
    deck = Pose(torso=(20, 0, 3), head=(-8, 0, 0), left=(26, 56, 0), right=(26, 56, 0),
                legs=((10, 16), (-10, 16)))
    deck_rock = deck.but(torso=(20, 0, -3))
    raise_ = Pose(torso=(-6, 0, 0), head=(-10, 0, 0), left=(150, 46, 0), right=(150, 46, 0),
                  legs=((6, 12), (-6, 12)))
    send = Pose(torso=(22, -15, 0), head=(-4, 8, 0), left=(-30, 22, 0), right=(62, 10, 22),
                legs=((18, 8), (-16, 8)))

    p.key(0, rest)
    p.key(.16, feint_l, punch=True).key(.34, feint_l)
    p.key(.46, feint_r, punch=True).key(.6, feint_r)
    p.key(.8, shrug)
    p.key(1.0, beckon).key(1.12, beckon_pull).key(1.24, beckon).key(1.36, beckon_pull)
    p.key(1.58, deck).key(1.8, deck_rock).key(2.02, deck)
    p.key(2.28, raise_)
    p.hold(2.28, 2.72, raise_.but(torso=(-7, 0, 0)))
    p.key(2.9, send, punch=True)
    p.hold(2.9, 3.4, send)

    # A: front medium for the feints, the shrug and the beckon: he is talking to the viewer.
    p.shot(0, 1.46, (.9, 1.35, 3.9), (0, 1.05, 0), 44, eye_to=(.78, 1.3, 3.55))
    # B: low from his left side as he crouches on the deck and the wave climbs behind him.
    p.shot(1.46, 2.5, (-3.1, .5, 2.3), (0, 1.2, -.6), 50, eye_to=(-3.3, .45, 2.05))
    # C: front and low for the send, the wave coming over his shoulders toward the lens.
    p.shot(2.5, 3.4, (1.1, .55, 4.4), (0, 1.2, 0), 50, eye_to=(1.0, .6, 4.05))
    p.locked((1.0, 1.05, 4.9), (0, 1.1, -.5), 48)
    return p

@performance
def amihan():
    """
    AIRBURST v3, 5.6 s (docs/reports/amihan-presentation-2026-10-02/airburst-v3.md). The owner on the 3.6 s version:
    "throoughly revamp her cutscene what the fuck is that it sucsk", "it doesnt feel the same as paete and phaisters".

    "Amihan calls the monsoon down into her hands, weaves it into her family's kasikus, and strings the whole lane like
    a loom; everyone standing in it feels it coming." It plays BEFORE the live 1.5 s dodge window, so it never shows the
    release: it ends on the real players in her lane bracing (`HeroIntroductionScene.AmihanLane.cs`). Every shot is on her
    right side, so screen direction never flips.

    CALL (0 to 1.7): front right, low. She flings the right hand out and snaps it to her chest, and the monsoon answers.
    WEAVE (1.7 to 3.7): low on her right, hands and face. Palms meet at the right hip and the kasikus forms between them;
    two pack beats; the draw.
    WARP (3.7 to 5.6): over the right shoulder, rising and pulling back down the lane. A small flick of the cupped hands
    casts the warp threads, then the coil she hands back in (`HeroAbilityClips.Amihan.cs` BuildAmihanStorm's first key).
    """
    p = Performance("amihan", 5.6)
    rest = Pose(head=(0, 10, 0))
    # Arms stay under the shoulder: her head is wide and her arms short, and higher the held slipper passes through her face
    # (UltimateIntroductionProbe measured 86 shoe vertices inside the head before this rule).
    call_out = Pose(torso=(-3, -14, 5), head=(-6, 16, -4), left=(24, 58, 38), right=(18, 72, 0),
                    legs=((6, 8), (-6, 14)))
    call_in = Pose(torso=(2, 18, 0), head=(-8, -4, 0), left=(24, 58, 38), right=(48, 10, 55),
                   legs=((6, 8), (-6, 12)))
    # Arms open low and wide, chest and chin up: she takes in the whole street's air as it arrives.
    opened = Pose(torso=(-10, 0, 0), head=(-13, 0, 0), left=(22, 74, 10), right=(22, 74, 10),
                  legs=((4, 14), (-4, 14)))
    sweep = Pose(torso=(4, 22, -2), head=(-4, -10, 0), left=(48, 38, 60), right=(44, 8, 15),
                 legs=((10, 10), (-8, 12)))
    # Palms meet cupped at the right hip; the torso coils right and the head only half follows.
    cup = Pose(torso=(-2, 34, -5), head=(-4, -14, 0), left=(48, 8, 75), right=(0, 32, 60),
               legs=((18, 10), (-16, 14)))
    pack1 = cup.but(torso=(-3, 38, -6), head=(-4, -15, 0))
    pack2 = cup.but(torso=(-4, 42, -6), head=(-4, -16, 0))
    draw = cup.but(torso=(-6, 47, -7), head=(-5, -19, 0), legs=((20, 10), (-18, 14)))
    # The warp: a small flick of the cupped hands toward the lane, low, then straight back into the coil.
    flick = Pose(torso=(5, 18, -4), head=(-7, -6, 0), left=(52, 22, 48), right=(40, 26, 34),
                 legs=((22, 10), (-20, 14)))
    coil = Pose(torso=(-4, 40, -6), head=(-4, -16, 0), left=(48, 8, 75), right=(24, 20, 45),
                legs=((20, 10), (-18, 14)))
    set_end = coil.but(torso=(-5, 43, -7), head=(-4, -17, 0))

    p.key(0, rest)
    p.key(.25, call_out, punch=True).key(.40, call_out.but(torso=(-3, -16, 5)))
    p.key(.55, call_in, punch=True)
    p.key(.90, opened).key(1.55, opened.but(torso=(-12, 2, 0), head=(-14, 0, 0)))
    p.key(1.95, sweep)
    p.key(2.15, cup, punch=True).key(2.30, cup)
    p.key(2.50, pack1).key(2.65, cup).key(2.95, pack2).key(3.10, pack1)
    p.key(3.30, draw).key(3.55, draw.but(torso=(-6, 48, -7)))
    p.key(3.78, flick, punch=True)
    p.key(4.15, coil).key(5.6, set_end)
    # Grounded throughout: no vortex lift. Featherfall is her real flight.
    p.rise(0, 0)
    # CALL: front right, low, a slow push in that leaves room round her for the arriving wind.
    p.shot(0, 1.7, (2.1, .8, 3.4), (0, 1.05, 0), 46, eye_to=(1.75, .88, 2.9), look_to=(0, 1.15, .1), fov_to=42)
    # WEAVE: low on her right, cupped hands low in frame and her whole face above them.
    p.shot(1.7, 3.7, (2.6, .72, 1.3), (.22, .98, .15), 42, eye_to=(2.3, .8, 1.8), look_to=(.18, 1.02, .3), fov_to=38,
           close=True)
    # WARP: over the right shoulder, rising and pulling back so the lane and everyone standing in it fill the frame.
    p.shot(3.7, 5.6, (2.5, 1.9, -2.3), (0, .8, 4.0), 50, eye_to=(3.1, 2.9, -3.6), look_to=(0, .3, 8.5), fov_to=56)
    # Reduced motion: one side view with her, her hands and the lane together.
    p.locked((4.4, 1.6, 1.0), (0, .95, 2.5), 52)
    return p

@performance
def paete():
    """
    MAKILING'S EMBRACE, 5.0 s. ⚠️⚠️ v5, CALLED FROM THE GROUND (owner, 2026-09-26 night, on the v4 film: *"i also dont like
    that paete just throws seeds in his ult"*, *"REDIRECT IT I WANNT IT TO LOOK LIKE HE GOES TO THE GHHROUND AND HIS ROOTS CONNECT
    TO IT AND HE IS CHANNELLING HIS POWER AND HE GLOWS AND SHIT AND THEN HIS ROOTS TRAVEL TO THE GROUND AND THEN THE tree slowly show
    up"*, then *"dont go past 5 seconds for cutscene and u can make some parts of it faster thoroughly think abt which parts should be
    faster"*). direction.md section 5.14 is the design; this is its body and its three shots.

    His sentence is five beats and each is in the body here: he GOES TO THE GROUND (down on the right knee, both palms flat on the
    court, a slam at 1.22); his ROOTS CONNECT (1.25 to 1.6: pressed down while they dig in); he CHANNELS (1.6 to 2.3: bowed over his
    hands, three quickening pulses, each a punch that drives his shoulders down and lets them rise); his ROOTS TRAVEL (2.32: one
    push down into the court as they leave, his head lifting after them); the TREE SLOWLY SHOWS UP (2.75 to 4.5: he stays down,
    joined to the ground, and heaves with each of its three hauls, his head rising to it; the last beat is its eyes).

    WHAT GOT FASTER AND WHY. Her arrival and gift, 1.4 s to 1.1 s: the owner asked for her full form to be brief, and she is the
    setup, not the power. The drop to the ground, a 0.12 s fall: a tree deciding to act commits at once (direction.md section 0).
    The roots' race, 0.45 s: speed reads as force, and play uses the same 0.45 s. HELD: the channel, the part he asked to see.
    SLOWER: the tree, which he asked to come up slowly (0.5 s to its eyes became 1.75 s).

    ⚠️ HE ENDS DOWN ON HIS KNEE, HANDS IN THE COURT. The live clip (`hero-paete-sentry`) starts from exactly this pose, so the hand-back
    from the cutscene to play does not jump: in play the roots leave his hands, the tree crawls out, and only then does he stand.
    """
    p = Performance("paete", 5.0)
    rest = Pose(left=(0, 15, 0), right=(0, 15, 0))
    # CALL. He stands calm (the owner's "nonchalant calm"), head a little bowed, arms loose.
    stand = Pose(torso=(3, 0, 0), head=(12, 0, 0), left=(6, 20, 0), right=(6, 20, 0), legs=((0, 8), (0, 8)))
    breath = stand.but(torso=(1, 0, 0), head=(9, 0, 0))
    # ⚠️ HE RECEIVES IT IN ONE OPEN HAND, HIS LEFT (v4's finding: two cupped hands cannot be drawn on his long single-bone vine arms,
    # and the right may be holding a slipper, which the introduction keeps in his hand).
    offer = Pose(torso=(4, -6, 0), head=(20, -14, 0), left=(96, 18, 14), right=(4, 22, 0), legs=((0, 8), (0, 8)))
    receive = offer.but(torso=(7, -6, 0), head=(24, -14, 0), left=(88, 18, 14))
    # IGNITION (0.78): the light goes into him; the chest opens, the head comes up, the hand lifts with it.
    ignite = Pose(torso=(-8, -2, 0), head=(-6, -4, 0), left=(104, 20, 10), right=(10, 28, 0), legs=((0, 9), (0, 9)))
    # The breath before the drop: up onto the toes, both hands lifting a little, the head coming down to the court.
    gather = Pose(torso=(-6, 0, 0), head=(10, 0, 0), left=(64, 24, 6), right=(64, 24, 6), legs=((6, 10), (-4, 10)))
    # GROUND (1.22, the slam): down on the right knee, the trunk pitched over, BOTH palms flat on the court about a metre in
    # front of him and shoulder-width apart, so the roots can spread round them. ⚠️ SOLVED, NOT GUESSED: these rigs have no
    # knees and the engine grounds every frame on the lowest vertex before adding the lift (`GroundIntroduction`), so the kneel
    # is the legs swung apart and sunk 0.34 m into the court, and each arm angle below was solved against the real glb so the
    # palms land 0 to 5 cm above the court after that sink (a first guess put them 0.3 m UNDER it).
    legs = ((54, 7), (-30, 7))
    plant = Pose(torso=(28, 0, 0), head=(26, 0, 0), left=(69, 5, 14), right=(69, 5, 14), legs=legs)
    # CONNECT: pressing down into the court while the roots dig in, the shoulders dropped, the head down to the hands.
    press = plant.but(torso=(32, 0, 0), head=(32, 0, 0), left=(74, 5, 14), right=(74, 5, 14))
    # CHANNEL: bowed over his hands. Each pulse drives the shoulders down (the punch) and lets them rise again.
    bow = plant.but(torso=(30, 0, 0), head=(36, 0, 0), left=(72, 5, 14), right=(72, 5, 14))
    pulse = plant.but(torso=(35, 0, 0), head=(30, 0, 0), left=(78, 5, 14), right=(78, 5, 14))
    # SEND (2.32): one push down into the court as the roots leave; then the head lifts after them toward the spot.
    send = plant.but(torso=(38, 0, 0), head=(18, 0, 0), left=(82, 5, 14), right=(82, 5, 14))
    watch = plant.but(torso=(24, 0, 0), head=(-10, 0, 0), left=(64, 5, 14), right=(64, 5, 14))
    # THE HEAVES: with each haul of the tree his shoulders drive UP and back and his hands lift 12 cm, pulling on the roots
    # that are still in the court: he is lifting it up through the ground. Between hauls he sinks back onto his hands.
    heave = plant.but(torso=(15, 0, 0), head=(-22, 0, 0), left=(59, 5, 14), right=(59, 5, 14))
    settle = plant.but(torso=(22, 0, 0), head=(-16, 0, 0), left=(62, 5, 14), right=(62, 5, 14))
    # THE EYES: the head up to it, the chest lifted, still kneeling, hands still in the court.
    awe = plant.but(torso=(20, 0, 0), head=(-36, 0, 0), left=(60, 5, 14), right=(60, 5, 14))

    p.key(0, rest)
    p.key(.18, stand)
    p.hold(.18, .32, breath)
    p.key(.46, offer)
    p.hold(.46, .64, offer)
    p.key(.70, receive)
    p.key(.78, ignite, punch=True)
    p.hold(.78, .94, ignite)
    p.key(1.08, gather)
    p.key(1.22, plant, punch=True)
    # ⚠️⚠️ v7 (2026-09-27, direction.md 5.16): THE ENDING IS NEW AND IT TOOK ITS 1.2 S FROM THE SETUP. The owner on v6: *"this felt
    # liek a weak ending to his ult haha maybe change angle or smth and show everyone getting pulled? and animate too that theyre
    # all shocked or trying to get out"*, *"try to follwo vines going to ppl with camera"*, and still *"dont go past 5 seconds"*.
    # So the channel is 0.2 s shorter (its three heartbeats come 0.18 and 0.15 s apart, quickening harder), the roots race in
    # 0.40 s, and the tree hauls itself out at 1.45 times its play speed; the TAKE (3.8 to 5.0) is where the time went.
    # THE TAKE, in his body: as its limbs wrap the caught players he sinks onto his hands (gathering), and as they are yanked he
    # HAULS: shoulders driven up and back, hands lifting off the court, pulling them in through the ground with the tree. Then the
    # awe again, held into play (the live clip still starts from it).
    haul = plant.but(torso=(8, 0, 0), head=(-30, 0, 0), left=(52, 5, 14), right=(52, 5, 14))
    p.key(1.34, press)
    p.hold(1.34, 1.5, press)
    p.key(1.56, bow)
    p.key(1.62, pulse, punch=True)
    p.key(1.72, bow)
    p.key(1.80, pulse, punch=True)
    p.key(1.88, bow)
    p.key(1.95, pulse, punch=True)
    p.key(2.03, bow)
    p.key(2.12, send, punch=True)
    p.key(2.28, send)
    p.key(2.48, watch)
    p.key(2.66, settle)
    p.key(2.78, heave, punch=True)
    p.key(2.95, settle)
    p.key(3.05, heave, punch=True)
    p.key(3.22, settle)
    p.key(3.33, heave, punch=True)
    p.key(3.6, awe)
    p.key(3.9, awe.but(head=(-32, 0, 0)))
    p.key(4.12, settle)
    p.key(4.2, haul, punch=True)
    p.key(4.46, haul.but(torso=(10, 0, 0)))
    p.key(4.64, awe)
    p.hold(4.64, 5.0, awe)
    # The body goes down onto the knee with the slam and stays down to the end (and into play).
    p.rise(0, 0.0).rise(1.08, 0.03).rise(1.22, -0.34).rise(5.0, -0.34)
    # 1 CALL. Front, low three-quarter from his right, one slow push-in, her head held above his.
    p.shot(0, 1.1, (2.1, .72, 4.8), (.45, 2.05, -.5), 44, eye_to=(1.8, .84, 4.2), look_to=(.45, 2.15, -.5), fov_to=42)
    # 2 ROOT. Close and low on his front right at kneel height, a slow push-in toward his hands and face: the drop, the roots
    # digging in, the channel. Her reaching hands are the top of the frame; she is the light, he is the subject.
    # ⚠️ Wider than first cut (`PaeteSpiritReviewProbe` v4): she is directly behind him, bent over him, and her hands and face are
    # the top of this frame; he, his hands on the court and his roots are the bottom. It pushes in toward his hands as he channels.
    # v7: it ends at the send (2.1), 0.2 s earlier, so the push-in is a little shorter.
    p.shot(1.1, 2.1, (2.5, 1.3, 3.6), (.05, 1.35, .2), 50, eye_to=(2.0, 1.09, 2.93), look_to=(.01, 1.18, .37), fov_to=46.7)
    # 3 RISE, one continuous shot in three moves (no cut): held wide and low while the roots cross the frame from him (left) to
    # the spot (right) under the brush-stroke of light; craning up through the three heaves so the whole tree is in by the time
    # it tops out; then a push IN toward its face as its eyes light (4.5).
    # ⚠️ v6 (2026-09-27): re-framed for the v9 tree, 6.6 m with its eyes at 3.1 m (was 9 m with eyes over 4 m; owner: *"Make
    # the tre a bit smaller and a lot more sleek so that it isnt too distracting"*). A shorter crane, a tighter lens at the end,
    # and it closes in on the eyes rather than backing off to fit a crown that is no longer there.
    # v7: the same three moves on the faster clock (the race 2.1 to 2.5, the crane through the hauls to 3.45, the push in on the
    # eyes, which open at 3.71).
    p.shot(2.1, 2.5, (8.2, 1.25, 5.0), (.2, .9, 3.0), 54, eye_to=(8.3, 1.3, 5.1), look_to=(.2, .95, 3.1))
    p.shot(2.5, 3.45, (8.3, 1.3, 5.1), (.2, .95, 3.1), 54, eye_to=(8.9, 2.0, 6.2), look_to=(.1, 2.9, 3.9), fov_to=58)
    p.shot(3.45, 3.8, (8.9, 2.0, 6.2), (.1, 2.9, 3.9), 58, eye_to=(8.3, 2.4, 6.4), look_to=(.1, 3.2, 4.3), fov_to=52)
    # 4 THE TAKE (v7), ONE CONTINUOUS MOVE, 3.8 to 5.0: the camera rides the first limb out from the trunk to the player it
    # catches, holds on them as it wraps them, then rises and swings wide as everyone is yanked in, and settles on the tree with
    # all of them bound round it. ⚠️ In a match the move is COMPUTED from where the caught players really stand
    # (`HeroIntroductionScene.PaeteTake.cs`, `PaeteTakeFrame`); this row is the settle it ends on, used as written only when
    # nobody is in reach (the limbs then whip into the court) and by the storyboard.
    p.shot(3.8, 5.0, (6.4, 1.3, 9.2), (.3, 2.0, 5.6), 54, eye_to=(7.6, 2.3, 10.6), look_to=(.2, 2.4, 5.5), fov_to=58)
    # Reduced motion: one wide locked shot that holds him, her and the landing.
    p.locked((8.6, 1.9, 5.8), (.2, 2.5, 3.2), 60)
    # ⚠️⚠️ v8 (2026-09-27): 6.5 S, EVERY BEAT 1.3 TIMES AS LONG. The owner on v7: *"lowk slow down ult a bit i cant comprehend wtf is
    # happening"*; asked, he chose 6.5 s over his earlier 5.0 cap. The table above is still typed on the 5.0 s clock the direction
    # was written on (direction.md 5.14 to 5.16), and stretched here once, evenly, so no beat is squeezed to buy another; the scene
    # (`HeroIntroductionScene.Paete.cs`, `PaeteStretch`) and the theme (`tools/build_paete_audio.py`) run the same factor.
    return _stretch(p, 6.5)


def _stretch(p, seconds):
    k = seconds / p.seconds
    p.seconds = seconds
    p.keys = [(round(t * k, 3), pose) for t, pose in p.keys]
    p.punches = [round(t * k, 3) for t in p.punches]
    p.holds = [(round(a * k, 3), round(b * k, 3)) for a, b in p.holds]
    p.lift = [(round(t * k, 3), m) for t, m in p.lift]
    p.shots = [(round(s[0] * k, 3), round(s[1] * k, 3)) + tuple(s[2:]) for s in p.shots]
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



def preview(hero, times=None, out=None, legacy=False, witness=False, stage=True):
    import numpy as np
    from PIL import Image
    import intro_pose_preview as ipp
    import intro_stage_sketch as iss

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
        floor = tris[:, :, 1].min()
        tris[:, :, 1] -= floor
        tris[:, :, 1] += lift
        eye, look, fov = shot_at(perf, t)
        if stage and hero in iss.STAGES and not legacy:
            hands = {k: v * scale - np.array([0, floor - lift, 0]) for k, v in model.hands(pose).items()}
            sketch = iss.Sketch()
            iss.STAGES[hero](t, sketch, hands, perf.seconds, np.array(eye))
            st, sc = sketch.result()
            if len(st):
                tris = np.concatenate([tris, st]); cols = np.concatenate([cols, sc])
        if witness:
            # A fixed three-quarter front witness at mid distance: judges the POSE, not the shot.
            eye, look, fov = (2.2, 1.7, 5.4), (0, 1.15 + lift, 0), 44
        # Unity: the hero faces +z; a shot offset with +z is in front of them.
        img = ipp.render(tris, cols, np.array(eye), np.array(look), fov,
                         label=f"{hero} t={t:.2f}s lift={lift:.2f}m" + ("  + stage sketch" if stage and not legacy else ""))
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
    ap.add_argument("--no-stage", action="store_true", help="body only, no stage sketch")
    args = ap.parse_args()
    if args.preview:
        times = [float(x) for x in args.times.split(",")] if args.times else None
        print(preview(args.preview, times, args.out, args.legacy, args.witness, not args.no_stage))
    else:
        for name in LEGACY:
            print(build(name).write())
        # Heroes authored after the 2.8 s baseline have no legacy row (Amihan, 2026-09-25).
        for name in PERFORMANCES:
            if name not in LEGACY:
                print(build(name).write())
        for name in HELD:
            print(HELD[name]().write())
