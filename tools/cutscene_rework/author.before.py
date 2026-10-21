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


def _sean(held):
    """
    SUPERNOVA, 3.4 s. Patience turning into commitment (plan.md section 1): "Waits for one
    opening. Makes it count." He is a lantern maker, so the fire is ASSEMBLED, not summoned:
    a parol frame of five sticks builds between his cupped hands one stick at a time while he
    watches it. Then the head snaps up (the opening), a held breath, the coil, and the rise
    that is the first frame of the live leap.
    Measured on his real mesh: twist 38 brings both hands together in front of the chest.
    """
    p = Performance("sean-held" if held else "sean", 8.4)
    rest = Pose(left=(0, 15, 0), right=(0, 15, 0))
    plant = Pose(torso=(6, 8, 0), head=(16, -4, 0), left=(6, 18, 0), right=(6, 18, 0),
                 legs=((4, 7), (-4, 7)))
    roll = plant.but(torso=(6, -8, 0), head=(16, 4, 0))
    # Cupped hands at the chest, head bowed over them: the craftsman.
    cup = Pose(torso=(10, 0, 0), head=(24, 0, 0), left=(75, 5, 38), right=(75, 5, 38),
               legs=((4, 7), (-4, 7)))
    if held:
        # The free hand forges the parol; the carrying hand keeps its slipper low.
        cup = cup.but(left=(75, 24, 20), right=(18, 28, -6))
        plant = plant.but(right=(18, 28, -6))
        roll = roll.but(right=(18, 28, -6))
    inspect_ = cup.but(head=(22, 0, -6), torso=(12, 0, 0))
    inspect2 = cup.but(head=(22, 0, 5), torso=(11, 0, 0))
    # The opening: the head snaps up to the target, hands still cupped. Held.
    look = cup.but(torso=(4, 0, 0), head=(-6, 0, 0))
    # The coil: chest down over the front leg, hands compressing the lantern in front of the chest.
    coil = Pose(torso=(30, 0, 0), head=(-14, 0, 0), left=(55, 25, 24), right=(55, 25, 24),
                legs=((18, 5), (-22, 5)))
    if held:
        coil = coil.but(left=(60, 30, 4), right=(8, 28, -4))
    # The rise: arms driving up, onto the toes, the first frame of the leap.
    rise = Pose(torso=(-8, 0, 0), head=(-14, 0, 0), left=(165, 22, 0), right=(165, 22, 0),
                legs=((-4, 3), (-10, 3)))

    # ⚠️ 7.0 s, RESTAGED (owner 2026-10-08: of three staging ideas "sean i like 1 and 2", joined, "About 7 s"). He builds
    # the parol as before, lifts it over his head and lets it go; the lens follows it up as it grows into the giant
    # festival lantern and lies over above the court; from above, the ring where he will land and who is in it; then the
    # coil and the LEAP up into it, ending at the top (see the stage's header for why and how play picks it up).
    # The stage (`HeroIntroductionScene.Sean.cs`) reads: the flame 1.75, the lift 2.25, the release 2.70, the top 4.60,
    # the ring 4.75, the coil 5.20, the leap 6.20, the burst 6.72. The LAST pose is still `rise`.
    free = (165, 22, 0)
    # Lifting it over his head with both hands (the free one alone when he holds a shoe), his face following it up.
    lift = Pose(torso=(-6, 0, 0), head=(-22, 0, 0), left=free, right=(18, 28, -6) if held else free, legs=((4, 7), (-4, 7)))
    # Letting it go: the hands open out and stay up, as if it might come back.
    let_go = lift.but(torso=(-10, 0, 0), head=(-34, 0, 0), left=(150, 56, 0), right=(18, 28, -6) if held else (150, 56, 0))
    # Watching it climb: the arms come down slowly, his head stays back.
    watch = Pose(torso=(-8, 0, 0), head=(-38, 0, 0), left=(40, 30, 0), right=(18, 28, -6) if held else (40, 30, 0), legs=((4, 7), (-4, 7)))

    p.key(0, rest)
    p.key(.22, plant).key(.38, roll).key(.5, plant)
    p.key(.72, cup)
    p.key(1.05, inspect_).key(1.35, inspect2).key(1.62, cup)
    p.hold(1.62, 2.1, cup)
    p.key(2.5, lift)
    p.key(2.7, let_go, punch=True)
    p.hold(2.7, 3.3, let_go)
    p.key(4.2, watch)
    p.hold(4.2, 4.9, watch)
    # ⚠️ HE LEAPS (owner 2026-10-09: "he should leap but then at the apex of the leap ... their cam tween to their fpv"). Up
    # at 6.2, fast and then slowing, to 4.675 m at 7.0: `SeanHeroKit.SupernovaApex`, where play puts him to dive from.
    p.key(5.3, coil)
    p.hold(5.3, 6.02, coil)
    p.key(6.2, rise, punch=True)
    p.hold(6.2, 8.4, rise)
    # 8.4 s (owner 2026-10-09: "more airtime before going to the fpv"): the same leap, slower over its top, then he HANGS there
    # while the lens comes round behind him (`SeanFrame`), 7.15 to 7.85, and only after that goes into his eyes.
    p.rise(0, 0).rise(6.14, 0).rise(6.36, 1.9).rise(6.62, 3.3).rise(6.95, 4.25).rise(7.35, 4.6).rise(7.7, 4.675).rise(8.4, 4.675)

    side = -1 if held else 1
    # A: three-quarter front medium while he plants and builds the lantern.
    p.shot(0, 1.3, (side * 1.9, 1.35, 4.1), (0, 1.0, 0), 44, eye_to=(side * 1.6, 1.3, 3.6))
    # B: close on the lantern and his face as it fills with flame.
    p.shot(1.3, 2.25, (side * 1.1, 1.45, 3.1), (0, 1.25, .2), 40, eye_to=(side * .95, 1.45, 2.8), close=True)
    # C: beside and below him as he lets it go, then up after it (the stage turns the lens to the lantern as it climbs).
    p.shot(2.25, 4.9, (side * 2.4, 3.4, 5.4), (0, 1.5, .2), 50, eye_to=(side * 3.2, 4.4, 8.6), fov_to=60)
    # D: from over the lantern, down through its frame at the court, the ring and whoever stands in it.
    p.shot(4.9, 6.0, (.4, 13.4, -2.2), (0, 0, .4), 58, eye_to=(.2, 12.4, -1.8))
    # E: low in front of him: the coil with the flames at his feet, the lantern filling the sky over him, and the rise.
    p.shot(6.0, 8.4, (side * -2.0, .3, 6.6), (0, 3.2, 0), 66, eye_to=(side * -1.6, .3, 5.4), look_to=(0, 3.8, 0))
    p.locked((1.9, 1.6, 7.2), (0, 2.4, 0), 56)
    return p


@performance
def sean():
    return _sean(False)

HELD["sean"] = lambda: _sean(True)
HOLD_DRIFT["sean-held"] = HOLD_DRIFT["sean"]

@performance
def zack():
    """OVERCLOCK: a free-hand spark, draw the storm inward, receive it and settle.

    Keep the existing2.8s shared-phase boundary. The carrying arm stays low;
    neither the shoe nor a distant target is the source of this self-buff.
    """
    # THE BOLT WAITS FOR HIM (2026-10-09, 6.4 s): the world stops with the lightning an arm's length over his hand, and he
    # is the only thing in it that moves. The times are the stage's (`HeroIntroductionScene.Zack.cs`).
    p = Performance("zack", 6.4)
    rest = Pose(left=(0, 15, 0), right=(0, 15, 0))
    cocky = Pose(torso=(0, -6, 4), head=(0, 8, -6), left=(58, 32, -18), right=(8, 20, 0),
                 legs=((2, 3), (-2, 8)))
    flick = cocky.but(left=(74, 36, -18))
    point_up = Pose(torso=(-2, 6, -3), head=(-12, 10, 0), left=(148, 55, 0), right=(8, 20, 0),
                    legs=((2, 3), (-2, 8)))
    # Stopped: the arm comes down, he looks at the lens with the bolt hanging over him.
    aside = Pose(torso=(0, -8, 5), head=(2, 14, -8), left=(22, 26, -10), right=(8, 20, 0),
                 legs=((2, 3), (-2, 8)))
    # He looks up at it, lifts one hand, and pokes it.
    eyeing = Pose(torso=(-5, 2, -2), head=(-26, 4, 0), left=(120, 22, 0), right=(8, 20, 0),
                  legs=((2, 3), (-2, 8)))
    poke = eyeing.but(torso=(-7, 2, -3), left=(168, 12, 0))
    shrug = Pose(torso=(0, -4, 4), head=(-2, -10, -8), left=(26, 40, -12), right=(22, 36, 0),
                 legs=((2, 3), (-2, 8)))
    gather = Pose(torso=(8, 4, 0), head=(-4, -6, -4), left=(60, 30, -10), right=(12, 24, 0),
                  legs=((6, 4), (-6, 4)))
    strike = Pose(torso=(-12, 0, 0), head=(8, 0, 0), left=(42, 42, -10), right=(18, 30, 0),
                  legs=((8, 4), (-8, 4)))
    settle = Pose(torso=(0, -4, 4), head=(-2, -10, -8), left=(6, 34, -12), right=(8, 24, 0),
                  legs=((2, 3), (-2, 8)))
    p.key(0, rest)
    p.key(.26, cocky).key(.40, flick, punch=True).key(.50, cocky)
    p.key(.66, point_up).hold(.66, 1.98, point_up)
    p.key(2.22, aside).hold(2.22, 2.66, aside)
    p.key(2.95, eyeing)
    p.key(3.15, poke, punch=True).hold(3.15, 3.34, poke)
    p.key(3.5, eyeing)
    p.key(3.82, shrug).hold(3.82, 4.42, shrug)
    p.key(4.72, gather).hold(4.72, 5.06, gather)
    p.key(5.2, strike, punch=True).hold(5.2, 5.5, strike)
    p.key(5.86, settle).hold(5.86, 6.4, settle)
    # A: the call, on the face's readable side.
    p.shot(0, 1.2, (-2.2, 1.35, 3.1), (0, 1.15, 0), 42, eye_to=(-2.05, 1.35, 2.85))
    # B: from low, looking up past him at the sky the bolt comes down.
    p.shot(1.2, 1.98, (-1.4, .4, 3.5), (-.2, 2.7, 0), 62, eye_to=(-1.3, .42, 3.3))
    # C: close, his face and the hung tip over it. The look, then the poke.
    p.shot(1.98, 3.62, (-1.5, 1.5, 3.3), (-.2, 1.5, 0), 48, eye_to=(-1.25, 1.5, 3.0), close=True)
    # D: wide and high, travelling round the stopped court.
    p.shot(3.62, 4.62, (5.2, 4.0, 6.2), (0, 1.7, 0), 52, eye_to=(3.0, 4.3, 7.4))
    # E: front and low for the let go and what he is left as.
    p.shot(4.62, 6.4, (-.8, .95, 4.3), (0, 1.5, 0), 50, eye_to=(-.7, 1.0, 3.6))
    p.locked((-1.9, 1.15, 4.3), (0, 1.15, 0), 46)
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
    p = Performance("nemu-held" if held else "nemu", 7.2)
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
    p.hold(1.86, 2.5, knowing)
    # LIGHTS OUT (2026-10-09): she folds her hands and sways through the dive, the rise and his looking round, never
    # turning to him, and only then points. The times are the stage's (`HeroIntroductionScene.Nemu.cs`).
    p.key(2.8, calm).key(3.4, calm_sway).key(4.0, calm).key(4.6, calm_sway).key(5.2, calm).key(5.55, calm_sway)
    p.key(5.8, send, punch=True)
    p.hold(5.8, 7.2, send)

    # A: from her left front so Kuro, on her left, shares the frame with her.
    p.shot(0, 1.76, (-1.5, 1.2, 3.6), (-.4, .95, 0), 46, eye_to=(-1.3, 1.15, 3.4))
    # B: straight on, close, for the knowing look into the lens.
    p.shot(1.76, 2.45, (.05, 1.25, 3.1), (0, 1.05, 0), 40, eye_to=(.05, 1.22, 2.8), close=True)
    # C: high over the court from in front of her. The ink runs out, he dives, he rises behind her, he looks round.
    p.shot(2.45, 4.95, (1.6, 7.6, 12.5), (0, 2.4, -3.2), 56, eye_to=(1.0, 6.4, 11.2))
    # D: low and close, looking up past her at him.
    p.shot(4.95, 6.0, (.6, .45, 3.1), (0, 2.7, -2.6), 58, eye_to=(.5, .5, 2.8))
    # E: back along the court for the send. He comes at this lens; x is 0 on purpose, so a mirrored shot is the same shot.
    p.shot(6.0, 7.2, (0, 1.3, 6.5), (0, 3.0, -3.0), 52)
    p.locked((3.4, 2.4, 8.6), (-.9, 2.1, 0), 52)
    return p


HELD["nemu"] = lambda: nemu(True)
HOLD_DRIFT["nemu-held"] = HOLD_DRIFT["nemu"]


def _dante(held):
    """
    TITAN FISSURE, 3.8 s (plan.md section 5). "Holds the difficult space. Refuses to be rushed."
    Weight is shown by SLOWNESS: he plants, gets under something heavy, stands into it and
    pushes two stone slabs apart overhead (the divided-mountain image he grew up with, as his
    own), holds it, loads both fists over his right shoulder and stamps forward into the
    strike the live fissure continues. Every hold trembles slightly: effort, not stillness.
    Signs measured on the sheets: +twist brings an arm inward, +torso yaw turns to his right.
    """
    p = Performance("dante-held" if held else "dante", 7.0)
    return _dante_drift(p, held)


def _dante_drift(p, held):
    """
    CONTINENTAL DRIFT, 7.0 s (owner 2026-10-08: of three staging ideas "dante i like 2 and 3", joined, "About 7 s").
    He punches the court; it breaks into plates adrift on a molten sea (seen from above); the horned basalt back
    rises under his plate and lifts him; it raises a fist as he loads both of his, they come down as one, the
    blasts run up the court and he is set back down in the stamp the live strike starts from.
    ⚠️ The stage (`HeroIntroductionScene.Dante.cs`) reads these times: the punch 1.10, the lift, the stamp 6.88.
    It ends EXACTLY on the fist landing, him still up on its back (owner 2026-10-08: "the cutscene should end exactly
    when the fist lands ... by then dante should still be on the longhorn and only then will it disappeaer into the
    ground"). So the lift never comes back down in this table: the going under is live play's.
    The LAST pose is still `stamp`: play resumes from it.
    """
    wide = ((2, 14), (-2, 14))
    rest = Pose(left=(0, 15, 0), right=(0, 15, 0))
    plant = Pose(torso=(6, 0, 0), head=(18, 0, 0), left=(8, 24, 0), right=(8, 24, 0), legs=wide)
    # The wind-up: the fist goes up behind his head, chest opened to it. Holding a shoe, the free hand does it.
    wind = Pose(torso=(-10, -16 if held else 16, 0), head=(8, 0, 0), legs=((6, 16), (-8, 16)),
                left=(165, 22, 0) if held else (16, 30, 0), right=(20, 42, -8) if held else (165, 22, 0))
    # The punch: down on the front leg, the fist in the court just ahead of his feet.
    punch = Pose(torso=(40, 8 if held else -8, 0), head=(-4, 0, 0), legs=((34, 14), (-26, 14)),
                 left=(58, 8, 22) if held else (18, 38, 0), right=(20, 42, -8) if held else (58, 8, 22))
    # Standing on a plate that is adrift: low, wide, arms out for balance, watching the seams.
    adrift = Pose(torso=(14, 0, 0), head=(16, 0, 0), left=(26, 46, 0), right=(20, 42, -8) if held else (26, 46, 0),
                  legs=((6, 18), (-6, 18)))
    adrift_l = adrift.but(torso=(13, -6, 2), head=(18, -22, 0))
    adrift_r = adrift.but(torso=(13, 6, -2), head=(18, 22, 0))
    # Carried up: knees deeper, head coming up as the ground leaves.
    ride = Pose(torso=(20, 0, 0), head=(-6, 0, 0), left=(34, 54, 0), right=(20, 42, -8) if held else (34, 54, 0),
                legs=((12, 20), (-10, 20)))
    ride_shake = ride.but(torso=(21, 0, 2), left=(36, 56, 0))
    stand = Pose(torso=(-4, 0, 0), head=(-10, 0, 0), left=(18, 30, 0), right=(20, 42, -8) if held else (18, 30, 0),
                 legs=((0, 16), (0, 16)))
    load = Pose(torso=(-4, 22, 0), head=(0, -14, 0), left=(150, 20, 50), right=(150, 20, -8),
                legs=((8, 12), (-10, 12)))
    stamp = Pose(torso=(28, -10, 0), head=(-8, 6, 0), left=(72, 6, 30), right=(72, 6, 30),
                 legs=((22, 10), (-18, 10)))
    if held:
        load = load.but(right=(20, 42, -8))
        stamp = stamp.but(right=(30, 42, -8))
    landed = stamp.but(torso=(36, -10, 0), legs=((30, 12), (-24, 12)))

    p.key(0, rest)
    p.key(.42, plant)
    p.key(.82, wind)
    p.hold(.82, .98, wind)
    p.key(1.10, punch, punch=True)
    p.hold(1.10, 1.50, punch)
    p.key(1.95, adrift).key(2.4, adrift_l).key(2.9, adrift_r).key(3.4, adrift)
    p.key(3.85, ride).key(4.1, ride_shake).key(4.4, ride).key(4.7, ride_shake).key(5.0, ride)
    p.key(5.5, stand)
    p.key(6.05, load)
    p.hold(6.05, 6.66, load)
    p.key(6.88, stamp, punch=True)
    p.hold(6.88, 7.0, stamp)

    # His plate is carried: a slow first heave, the long rise, a settle at the top, then down with the thing as it goes under.
    p.rise(0, 0).rise(3.45, 0).rise(4.0, .45).rise(5.2, 3.4).rise(7.0, 3.4)

    # A: low and close in front while he plants, winds and punches.
    p.shot(0, 1.15, (1.3, .55, 3.4), (0, 1.0, 0), 46, eye_to=(1.0, .5, 2.9))
    # B: high over him, the court in plates on the sea, rising slowly as they drift.
    p.shot(1.15, 3.4, (.5, 7.4, -2.2), (0, 0, .5), 58, eye_to=(.9, 10.2, -3.0))
    # C: low, wide and to his front right, looking up as it comes out of the seam under him.
    p.shot(3.4, 5.3, (4.0, .7, 5.6), (0, 1.0, .6), 52, eye_to=(4.9, 1.1, 6.8), look_to=(0, 2.9, .6))
    # D: low in front of its face, him on top, the raised fist over both, creeping in until the fist lands.
    p.shot(5.3, 7.0, (-2.9, 1.3, 9.8), (.3, 3.3, .9), 46, eye_to=(-2.4, 1.1, 8.6), look_to=(.3, 3.0, 1.2))
    p.locked((2.6, 1.8, 8.0), (0, 1.6, .5), 54)
    return p


def _dante_fissure(held):
    """The first one, kept to read: TITAN FISSURE, 3.8 s (the two parted slabs). Nothing calls it."""
    p = Performance("dante-held" if held else "dante", 3.8)
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

    if held:
        # The free fist leads the stamp; the slipper stays below and outside the face.
        shoe = (20, 42, -8)
        brace = brace.but(right=shoe)
        brace_shake = brace_shake.but(right=shoe)
        load = load.but(right=shoe)
        stamp = stamp.but(right=(30, 42, -8))

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


@performance
def dante():
    return _dante(False)


HELD["dante"] = lambda: _dante(True)
HOLD_DRIFT["dante-held"] = HOLD_DRIFT["dante"]


def _cheska(held):
    """
    ABSOLUTE ZERO, 3.2 s (plan.md section 6). "Reads the space. Leaves you the harder route."
    Quiet and precise: she stands still and READS the court with a slow head turn, draws one
    exact line of frost in the air with a fingertip, closes her hands round it into a crystal,
    lifts it clear of her chest, a beat, and snaps her hands apart: the frame before the live nova.
    Holding a slipper, the free (left) hand does the fine work and the shoe stays low and out
    of the way, and the camera takes her other side so the shoe is on the far side.
    """
    p = Performance("cheska-held" if held else "cheska", 11.9)
    return _cheska_breath(p, held)


def _cheska_breath(p, held):
    """
    ABSOLUTE ZERO, 9.0 s, the seventh staging (owner 2026-10-08: "cheeky but surprisingly strong"; of the sixth, at 6.5 s:
    "its a bit too sped up and linear ... the first skating part is just not graceful enough ... just use the original fpv
    hand, instead make her whole elbow bent and then extend to point towards the person. after do a top view of her
    skating circling around, then do the glacier stuff").
    ⚠️ 9.0 s is to be shown to him, not yet agreed: asked for the length he said "show me the rendeer".
    She skates one long slow loop and turns to a stop; through her own eyes she turns to each other player, her arm
    comes up bent and drives out to point at them; from above she skates a full circle; one foot down and the court
    goes up in ice; she blows the frost off her fingertip; her hands snap apart and the glacier bursts.
    ⚠️ The stage (`HeroIntroductionScene.Cheska.cs`) MOVES HER BODY on the loop and the circle, and shots 1 to 3 are
    through her eyes with her own first-person arm, her body not drawn. It reads these times: the loop .22 to 1.94, the
    turn 1.86 to 2.26, the looks 2.30, 3.60, 4.50, the heart 5.30 to 7.92, the stomp 8.15, the last
    snap 8.72. The LAST pose is still `snap`: play resumes from it.
    """
    rest = Pose(left=(0, 15, 0), right=(0, 15, 0))
    shoe = (18, 26, -6)  # the slipper hand, low and clear of the body
    hip = (-8, 34, 30)   # a hand on her hip
    lean = ((4, 3), (-2, 8))  # her weight on one leg
    wing_l, wing_r = (8, 82, -10), (-14, 76, 10)  # her arms held out wide, one a little ahead, one a little behind
    # The push: low over the front knee, the other leg driving back.
    push = Pose(torso=(22, -8, 0), head=(-12, 6, 0), left=(36, 40, 0), right=shoe if held else (-26, 44, 0), legs=((30, 6), (-28, 8)))
    # The long glide: her chest forward, her head up, one leg held out straight behind her, her arms wide.
    glide_a = Pose(torso=(30, 6, -5), head=(-26, 0, 0), left=wing_l, right=shoe if held else wing_r, legs=((6, 3), (-62, 6)))
    # The change of edge: both skates under her for a moment, arms coming round.
    cross = Pose(torso=(12, 0, 0), head=(-8, 0, 0), left=(20, 60, 0), right=shoe if held else (20, 60, 0), legs=((10, 4), (-8, 4)))
    glide_b = Pose(torso=(30, -6, 5), head=(-26, 0, 0), left=wing_r, right=shoe if held else wing_l, legs=((-62, 6), (6, 3)))
    # The turn that stops her: drawn up tall, arms brought in and up, one foot lifted.
    turn = Pose(torso=(-6, 0, 0), head=(-10, 0, 0), left=(150, 18, 20), right=shoe if held else (150, 18, 20), legs=((30, 2), (0, 2)))
    if held:
        stand = Pose(torso=(-3, -5, 4), head=(4, 0, 0), left=hip, right=shoe, legs=lean)
        ready = Pose(torso=(-4, 0, 0), head=(2, 0, 0), left=(20, 30, 0), right=shoe, legs=((38, 6), (0, 6)))
        stomp = Pose(torso=(8, 0, 0), head=(6, 0, 0), left=(-10, 30, 0), right=shoe, legs=((6, 8), (0, 8)))
        blow = Pose(torso=(2, -4, 3), head=(6, 10, -6), left=(116, 8, 44), right=shoe, legs=lean)
        show = Pose(torso=(4, -4, 0), head=(8, -8, 4), left=(84, 35, 8), right=shoe)
        snap = Pose(torso=(-4, 0, 0), head=(-4, 0, 0), left=(72, 74, 0), right=(30, 40, 0))
    else:
        stand = Pose(torso=(-3, 5, -4), head=(4, 0, 0), left=hip, right=(4, 16, 0), legs=lean)
        ready = Pose(torso=(-4, 0, 0), head=(2, 0, 0), left=(20, 30, 0), right=(20, 30, 0), legs=((38, 6), (0, 6)))
        stomp = Pose(torso=(8, 0, 0), head=(6, 0, 0), left=(-10, 30, 0), right=(-10, 30, 0), legs=((6, 8), (0, 8)))
        blow = Pose(torso=(2, 4, -3), head=(6, -10, 6), left=hip, right=(116, 8, 44), legs=lean)
        show = Pose(torso=(4, 0, 0), head=(8, 0, 4), left=(84, 15, 20), right=(84, 15, 20))
        snap = Pose(torso=(-4, 0, 0), head=(-4, 0, 0), left=(72, 74, 0), right=(72, 74, 0))

    # ⚠️ 11.9 s; 11.5, then 12.6, then this (owner 2026-10-08: "ykw we can extend the ult cutscene until everything looks smooth"; it was 9.0 and the
    # heart's turns were still too quick). The stage reads: the loop .26 to 2.33, the turn 2.24 to 2.72, the looks 2.80,
    # 4.10, 5.00, the heart 5.80 to 9.16, the stomp 9.35, the blow 10.02, the last snap 11.58.
    p.key(0, rest)
    p.key(.26, push)
    p.key(.74, glide_a)
    p.hold(.74, 1.2, glide_a)
    p.key(1.42, cross)
    p.key(1.68, glide_b)
    p.hold(1.68, 2.04, glide_b)
    p.key(2.33, turn)
    p.hold(2.33, 2.64, turn)
    # Through her eyes she simply stands, a hand on her hip: nobody sees it.
    p.key(3.0, stand)
    p.hold(3.0, 5.7, stand)
    # The heart, seen from above, four seconds of it: one edge in and up the right side, the change of edge down in the
    # cleft, the other edge round the left lobe and out, and a last change in the curl of the tail.
    p.key(5.8, glide_a)
    p.hold(5.8, 7.0, glide_a)
    p.key(7.25, cross)
    p.key(7.5, glide_b)
    p.hold(7.5, 8.4, glide_b)
    p.key(8.85, cross)
    p.key(9.18, stand)
    p.key(9.27, ready)
    p.key(9.35, stomp, punch=True)
    p.hold(9.35, 9.75, stomp)
    # The blow is not hurried (he, of half a second of it: "its ending too fast"): her hand comes up, she blows, holds it
    # while the breath goes, and lets the hand down again before anything else happens.
    p.key(10.04, blow)
    p.hold(10.04, 10.72, blow)
    p.key(11.16, stand)
    p.key(11.44, show)
    p.key(11.58, snap, punch=True)
    p.hold(11.58, 11.9, snap)

    m = -1 if held else 1  # mirror the camera so a held shoe stays on the far side
    # The skate: in front of her and to one side, and HIGH: the others stand anywhere from 3 m out, and a lens at head
    # height out there looks at the back of one of them (films c11, c12). From up here it looks over them.
    p.shot(0, 2.8, (m * 2.6, 4.0, 6.2), (0, .5, 1.3), 44, eye_to=(m * 2.0, 3.2, 5.0), look_to=(0, .7, .5))
    # Looks 1 to 3: through her eyes, computed in the stage (these numbers are only stand-ins).
    p.shot(2.8, 4.1, (0, 1.4, .2), (-3.0, 1.0, 5.0), 60, close=True)
    p.shot(4.1, 5.0, (0, 1.4, .2), (0, 1.0, 6.0), 60, close=True)
    p.shot(5.0, 5.8, (0, 1.4, .2), (3.0, 1.0, 5.0), 60, close=True)
    # The heart: from straight above (computed in the stage from the line's own size; these numbers are only stand-ins).
    p.shot(5.8, 9.2, (-5.9, 10.0, -2.0), (-5.9, 0, -1.1), 46)
    # The stop: low and wide in front of her: the foot, and the glacier going up behind her.
    p.shot(9.2, 9.85, (m * 2.6, .55, 6.4), (0, 1.1, 0), 52, eye_to=(m * 3.0, .7, 7.4), look_to=(0, 2.8, -1.2), fov_to=58)
    # Close again for the frost blown off her fingertip, the ice standing behind her.
    p.shot(9.85, 11.2, (m * .9, 1.25, 3.0), (0, 1.1, 0), 40, eye_to=(m * .68, 1.2, 2.45), close=True)
    # High and wide for the last snap: the glacier bursts and everyone is left in their block.
    p.shot(11.2, 11.9, (m * 3.4, 6.4, 10.2), (0, .7, 2.6), 50, eye_to=(m * 3.0, 5.6, 9.2), look_to=(0, .8, 2.2))
    p.locked((m * 3.2, 6.0, 9.8), (0, .8, 2.4), 52)
    return p


def _cheska_nova(held):
    """The first one, kept to read: staged as GLACIAL NOVA, 3.2 s (the fingertip line of frost). Nothing calls it."""
    p = Performance("cheska-held" if held else "cheska", 3.2)
    rest = Pose(left=(0, 15, 0), right=(0, 15, 0))
    shoe = (18, 26, -6)  # the slipper hand, low and clear of the body
    read_l = Pose(torso=(0, -4, 0), head=(2, -24, 0), left=(4, 12, 0), right=shoe if held else (4, 12, 0))
    read_r = read_l.but(torso=(0, 4, 0), head=(2, 24, 0))
    if held:
        draw_a = Pose(torso=(2, -6, 0), head=(8, -10, 0), left=(82, 10, 6), right=shoe)
        draw_b = draw_a.but(left=(86, 22, -22), head=(8, -18, 0))
        close = Pose(torso=(4, -4, 0), head=(14, -8, 0), left=(72, 20, 15), right=shoe)
        lift = close.but(left=(82, 35, 8), head=(6, -8, 4))
        snap = Pose(torso=(-4, 0, 0), head=(-4, 0, 0), left=(72, 74, 0), right=(30, 40, 0))
    else:
        draw_a = Pose(torso=(2, 6, 0), head=(8, 10, 0), left=(4, 12, 0), right=(82, 10, 6))
        draw_b = draw_a.but(right=(86, 22, -22), head=(8, 18, 0))
        close = Pose(torso=(4, 0, 0), head=(14, 0, 0), left=(70, 4, 36), right=(70, 4, 36))
        lift = close.but(left=(82, 15, 20), right=(82, 15, 20), head=(6, 0, 4))
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
    # C: widen on the same side; keep the gathering palm visible through the snap.
    p.shot(2.12, 3.2, (m * 1.6, 1.65, 3.7), (0, 1.05, 0), 48, eye_to=(m * 1.45, 1.6, 3.45))
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
    p = Performance("rafi", 8.6)
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
    # THE TIDE GOES OUT, AND WHAT COMES BACK (2026-10-09, 8.6 s; the owner's shark joined to the tide going out). The times
    # are the stage's (`HeroIntroductionScene.Rafi.cs`), which also carries his body into the wall, onto the shark and back.
    haul = Pose(torso=(-10, -6, 0), head=(-6, -6, -4), left=(104, 16, -6), right=(96, 16, 6),
                legs=((10, 12), (-10, 12)))
    haul_hold = haul.but(torso=(-12, -4, 2))
    # Head first into the wall, arms out in front.
    dive = Pose(torso=(34, 0, 0), head=(-22, 0, 0), left=(165, 18, 0), right=(165, 18, 0),
                legs=((-12, 4), (-16, 4)))
    # On its back: low, knees wide, both hands forward on the fin, leaning into each bank.
    ride = Pose(torso=(30, 0, 0), head=(-18, 0, 0), left=(64, 26, 0), right=(64, 26, 0),
                legs=((34, 26), (34, 26)))
    ride_left = ride.but(torso=(30, -8, 12), head=(-18, -12, 6))
    ride_right = ride.but(torso=(30, 8, -12), head=(-18, 12, -6))
    # Let go: open, arms and legs wide, in the air.
    air = Pose(torso=(-12, 0, 0), head=(-4, 0, 0), left=(118, 56, 0), right=(118, 56, 0),
               legs=((22, 16), (-22, 16)))
    watch = Pose(torso=(-9, 0, 0), head=(-30, 0, 0), left=(12, 30, 0), right=(12, 30, 0),
                 legs=((2, 8), (-2, 8)))
    brace = Pose(torso=(8, 0, 0), head=(-14, 0, 0), left=(46, 44, 0), right=(46, 44, 0),
                 legs=((8, 14), (-8, 14)))
    found = Pose(torso=(6, 0, 0), head=(16, 0, 6), left=(14, 34, 0), right=(14, 34, 0),
                 legs=((8, 6), (-4, 6)))
    p.key(1.58, deck)
    p.key(1.78, haul, punch=True)
    p.hold(1.78, 2.6, haul_hold)
    p.key(2.9, dive)
    p.hold(2.9, 3.2, dive)
    p.key(3.3, ride).key(3.75, ride_left).key(4.3, ride_right).key(4.75, ride)
    p.hold(4.75, 5.3, ride)
    p.key(5.5, air)
    p.key(5.9, deck, punch=True)
    p.hold(5.9, 6.05, deck)
    p.key(6.3, watch)
    p.hold(6.3, 6.95, watch)
    p.key(7.2, brace, punch=True)
    p.hold(7.2, 7.6, brace)
    p.key(8.0, found)
    p.hold(8.0, 8.6, found)

    # A: front medium for the feints, the shrug and the beckon: he is talking to the viewer.
    p.shot(0, 1.46, (.9, 1.35, 3.9), (0, 1.05, 0), 44, eye_to=(.78, 1.3, 3.55))
    # B: high over the court from in front of him. The water runs off it toward him and past him.
    p.shot(1.46, 2.75, (2.0, 7.6, 11.6), (0, .4, -1.2), 56, eye_to=(1.4, 6.6, 10.6))
    # C: low from his side as he turns and goes into the wall.
    p.shot(2.75, 3.2, (4.6, 1.3, 2.2), (0, 2.2, -2.0), 58, eye_to=(4.2, 1.4, 1.6))
    # D: under. The stage puts this lens beside the shark (`RafiFrame`); these numbers are only its start.
    p.shot(3.2, 5.0, (3.6, 60.2, -2.2), (0, 59, -4), 58)
    # E: ONE shot from the ground, the lens not moving (owner 2026-10-09): out of the top of the wall, his landing, the shark
    # going up into the sky as a ghost, and its fall in front of him. The stage aims it (`RafiFrame`).
    p.shot(5.0, 7.6, (2.4, .35, 10.5), (0, 4.0, 0), 72)
    # G: high three-quarter for the wave the hit makes, and the slipper arriving.
    p.shot(7.6, 8.6, (6.6, 5.6, 7.4), (0, .7, 2.6), 56, eye_to=(5.6, 4.8, 8.4))
    p.locked((1.0, 1.05, 4.9), (0, 1.1, -.5), 48)
    return p

@performance
def amihan():
    """
    AIRBURST v7, THE BIRD AMIHAN, 5.6 s (docs/reports/amihan-presentation-2026-10-02/cutscene-v4-plan.md, "v7"). The owner on
    v6: "it doesnt look as good as paete's or phaisters"; "their ults look really good and have really cool beats and show off
    story of their character"; "i want amihan to have hher own ult that doesnt jsut copy someone elses but it has to have the
    same feel or impact"; "make it look like wind was slowly gathering around her and in the background"; and of the read,
    "make her fly higher here like gojo when he was enlightened".

    Paete's has Makiling and a tree with a face rising from the court; Phaister's opens an eye in the sky and lowers her doll.
    Hers is her name: in the Tagalog creation story Amihan is the first bird, the one that pecked open the bamboo. The
    monsoon answers her whistle across the whole plaza and gathers into that bird, made of wind; it swoops round her, rises
    behind her as she winds up, and on her drive it beats its wings down the lane: the fan.

    STILL (0 to 1.66): eyes closed, she floats calmly on a breeze of her own, a leaf circling her; she settles and shrugs.
    CALL (1.66 to 2.75): the whistle; the monsoon answers across the plaza, gathering round her into the sky; she looks up.
    THE BIRD (2.75 to 3.62): it forms above her, cries, swoops round her past the lens; she reaches up to it, grinning; then
    a glance into the lens.
    WIND-UP (3.62 to 4.55): the bird behind her, wings raised, as she coils. DRIVE (4.55): both palms, the wingbeat. HANG
    (4.55 to 5.18): the story clock at about a fifth (`HeroIntroductionScene.Amihan.cs` AmStory). FINISH: a close-up, her
    cute pose (leaning in, hands behind her back, a foot kicked up, a wink), one feather drifting down.
    The free (left) hand does the gestures: the right hand holds the slipper, which passes through her large head higher.
    """
    p = Performance("amihan", 5.9)
    # Easy, hip cocked, the slipper hand on her hip, head tipped: waiting for the wind.
    idle = Pose(torso=(-3, -6, 4), head=(-6, 10, 8), left=(8, 24, 0), right=(14, 62, -38), legs=((2, 10), (-6, 16)))
    idle_b = idle.but(torso=(-4, -4, 5), head=(-8, 12, 10))
    # The read: her free arm out straight to her side, palm up, feeling for the wind; leaning into it, head cocked, chin up.
    read = Pose(torso=(-6, -8, 10), head=(-14, -16, 14), left=(70, 88, -10), right=(14, 62, -38), legs=((4, 10), (-2, 14)))
    shrug = Pose(torso=(-8, 0, 0), head=(-10, 0, -10), left=(34, 62, 0), right=(30, 58, 0), legs=((4, 10), (-2, 14)))
    # The whistle: two fingers of the free hand at her mouth, chest up, feet planted wide.
    whistle = Pose(torso=(-10, 6, 0), head=(-12, 6, 0), left=(118, 6, 46), right=(12, 60, -38), legs=((6, 16), (-4, 16)))
    # The monsoon answers: she leans back and looks up at it gathering over the plaza, her free arm opening.
    sky = Pose(torso=(-12, -6, 0), head=(-32, -8, 4), left=(44, 66, 0), right=(14, 62, -38), legs=((4, 12), (-4, 14)))
    # Reaching up to the bird as it swoops round her, grinning.
    reach = Pose(torso=(-12, -10, 6), head=(-28, -18, 8), left=(150, 40, 0), right=(14, 62, -38), legs=((4, 10), (-4, 14)))
    reach_b = reach.but(left=(158, 46, 0), torso=(-13, -14, 6), head=(-26, -24, 8))
    # The glance into the lens at her left front, chin tipped.
    look = Pose(torso=(-4, -10, 4), head=(-10, -30, 10), left=(96, 48, -10), right=(14, 62, -38), legs=((4, 10), (-2, 14)))
    # The wind-up: coiled hard right, the free arm swung back over and behind her head.
    windup = Pose(torso=(-12, 48, -6), head=(-8, -22, 0), left=(-48, 44, 0), right=(30, 30, 30), legs=((24, 10), (-22, 14)))
    windup_deep = windup.but(torso=(-15, 58, -8), head=(-9, -26, 0), left=(-58, 46, 0))
    # The release keeps v3.2's drive (both palms forward, the lunge): the gameplay contract is pinned to it.
    drive = Pose(torso=(16, -6, -2), head=(-18, 8, 0), left=(104, 20, 30), right=(104, 20, 30), legs=((42, 12), (-44, 10)))
    follow = Pose(torso=(6, -8, 2), head=(-10, 6, 0), left=(70, 46, 10), right=(70, 46, 10), legs=((22, 10), (-18, 10)))
    # THE FINISH (owner: "end of ult cutscene should be her posing or looking cute", "TEEHEE pose", "close up of her"): leaning
    # in to the lens, hands clasped behind her back (her rigid arms cannot reach her head; behind her they read and hide the
    # slipper), head tipped, one foot kicked up behind her, a wink.
    # v10 ("make the pose cuter"): shoulders scrunched up as she leans in, her head tipped further, her hands tucked tighter
    # behind her, the foot kicked up higher, and the teehee face (`VoxelFace.Look.Teehee`).
    # v12 (owner: "doesnt look cute", "weird af expression"): with her rigid arms, hands behind her back read as two stiff
    # arms sticking out. A happy wave up beside her face instead, the slipper hand on her hip, her head tipped, a foot kicked
    # up behind her, and her happy closed-eyed grin (^v^), the face the owner liked.
    finish = Pose(torso=(6, -6, 8), head=(-8, -10, 12), left=(112, 20, 40), right=(16, 58, -38), legs=((4, 10), (-50, 8)))
    # v13: held out to her side her rigid arm only twisted, so the wave never showed. A GIGGLE instead (the classic teehee):
    # her hand up by her chin, eyes shut in her grin, shoulders and head bobbing with it.
    wave = finish.but(torso=(9, -6, 11), head=(-6, -12, 16), left=(116, 20, 40))

    p.key(0, idle).hold(.32, .55, idle_b)
    # (owner on v7: "the leg tapping dont make sense": she was tapping her feet in the air. The taps are gone: she floats,
    # settles, and shrugs.)
    p.hold(.78, 1.34, read)
    p.hold(1.52, 1.66, shrug)
    p.key(1.78, whistle, punch=True).key(1.98, whistle.but(head=(-13, 6, 0)))
    p.hold(2.20, 2.70, sky)
    p.key(2.92, reach, punch=True).key(3.12, reach_b).key(3.24, reach)
    p.hold(3.36, 3.62, look)
    p.key(3.85, windup).key(4.28, windup_deep).key(4.40, windup_deep.but(torso=(-16, 60, -8)))
    # THE DRIVE, then the hang: held while the clock slows (a moving hold, so even slowed she is never a statue).
    p.hold(4.55, 5.08, drive)
    p.punches.append(4.55)
    p.key(5.20, follow)
    # (v9, owner: "hold the end pose a bit more": held 0.6 s, swaying a little through it and ending exactly on it,
    # which the live clip starts from.)
    # The hold: the hand waving twice, ending exactly on the finish the live clip starts from.
    p.key(5.30, finish, punch=True).key(5.44, wave).key(5.58, finish).key(5.72, wave).key(5.9, finish)
    # THE FLOAT (owner: "make her fly higher here like gojo when he was enlightened", "js a bite higher tho"): as she closes
    # her eyes and feels for the wind she lifts, calm and weightless, about 0.37 m, and settles back onto her first tap.
    # v10 (owner: "she should stay floating too during her ult why does she even do this if she immediately falls back
    # donw"): she rises on her breeze and STAYS up, bobbing gently on it through the whistle, the bird, the wind-up and the
    # drive, and only touches down softly into her finish (play resumes with her on the court).
    p.rise(0, 0).rise(.60, 0).rise(.80, .2).rise(1.0, .36).rise(1.3, .42)
    for i, t in enumerate([1.7, 2.1, 2.5, 2.9, 3.3, 3.7, 4.1, 4.5, 4.9]):
        p.rise(t, .36 if i % 2 == 0 else .44)
    p.rise(5.2, .38).rise(5.32, .12).rise(5.42, 0)

    # A STILL: from her left front, all of her and the falling leaf, a slow push in.
    p.shot(0, 1.66, (-.9, 1.05, 4.3), (-.2, 1.0, 0), 42, eye_to=(-.75, 1.05, 3.7), look_to=(-.2, 1.05, 0), fov_to=40)
    # B CALL: close on the whistle, then pulled back low and wide, looking up past her at the monsoon gathering in the sky.
    p.shot(1.66, 2.75, (-.6, 1.4, 1.6), (0, 1.45, 0), 34, eye_to=(-2.4, .55, 3.4), look_to=(0, 2.3, -1.5), fov_to=62)
    # C THE BIRD: low and wide from her left front, her and the sky above her; the bird's swoop passes close to this lens.
    # (v7 r4: by her glance into this lens she was too small to read; it closes in to a medium shot as the bird settles.)
    p.shot(2.75, 3.62, (-3.0, .7, 4.6), (0, 2.4, -.5), 60, eye_to=(-1.7, 1.05, 3.0), look_to=(0, 1.45, 0), fov_to=44)
    # D WIND-UP: behind her right shoulder, high and wide: her coiling and the bird over her with its wings raised.
    # (v7 r2: the raised wings were cropped at the top; aimed higher.)
    p.shot(3.62, 4.55, (2.8, 2.2, -4.2), (0, 2.8, 3.0), 62, eye_to=(3.0, 2.5, -4.6), look_to=(0, 2.2, 5.0), fov_to=62)
    # E HANG: cut on the drive to her right side, wide enough for the wingbeat over the lane: her in the left third.
    p.shot(4.55, 5.18, (5.2, 1.6, .6), (0, 1.5, 1.2), 66, eye_to=(4.8, 1.6, .9), look_to=(0, 1.45, 1.6), fov_to=62)
    # F FINISH: a close-up of her at her left front (v14: the side of the hand at her chin; from her right her head hid
    # it), as she giggles, a slow push in.
    p.shot(5.18, 5.9, (-.7, 1.15, 3.3), (-.05, 1.2, 0), 42, eye_to=(-.6, 1.2, 2.85), look_to=(-.05, 1.22, 0), fov_to=38)
    p.locked((3.4, 1.5, 3.4), (0, 1.0, 1.5), 50)
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
    # ⚠️ v9 (2026-10-08): HIS ARMS GO STRAIGHT DOWN IN FRONT OF HIM (owner, drawing over him from behind: "his arms are bent
    # outwards. they arent bent straight to the front or inwards"). Every kneeling pose below held them 5 degrees out and
    # turned 14, to set his palms shoulder-width apart for roots to spread round; now that the arms themselves go into
    # the court that read as bowed elbows. 1 and 2. The live clip (`HeroAbilityClips.Paete.cs`, `BuildPaeteSentry`) matches.
    legs = ((54, 7), (-30, 7))
    plant = Pose(torso=(28, 0, 0), head=(26, 0, 0), left=(69, 1, 2), right=(69, 1, 2), legs=legs)
    # CONNECT: pressing down into the court while the roots dig in, the shoulders dropped, the head down to the hands.
    press = plant.but(torso=(32, 0, 0), head=(32, 0, 0), left=(74, 1, 2), right=(74, 1, 2))
    # CHANNEL: bowed over his hands. Each pulse drives the shoulders down (the punch) and lets them rise again.
    bow = plant.but(torso=(30, 0, 0), head=(36, 0, 0), left=(72, 1, 2), right=(72, 1, 2))
    pulse = plant.but(torso=(35, 0, 0), head=(30, 0, 0), left=(78, 1, 2), right=(78, 1, 2))
    # SEND (2.32): one push down into the court as the roots leave; then the head lifts after them toward the spot.
    send = plant.but(torso=(38, 0, 0), head=(18, 0, 0), left=(82, 1, 2), right=(82, 1, 2))
    watch = plant.but(torso=(24, 0, 0), head=(-10, 0, 0), left=(64, 1, 2), right=(64, 1, 2))
    # THE HEAVES: with each haul of the tree his shoulders drive UP and back and his hands lift 12 cm, pulling on the roots
    # that are still in the court: he is lifting it up through the ground. Between hauls he sinks back onto his hands.
    heave = plant.but(torso=(15, 0, 0), head=(-22, 0, 0), left=(59, 1, 2), right=(59, 1, 2))
    settle = plant.but(torso=(22, 0, 0), head=(-16, 0, 0), left=(62, 1, 2), right=(62, 1, 2))
    # THE EYES: the head up to it, the chest lifted, still kneeling, hands still in the court.
    awe = plant.but(torso=(20, 0, 0), head=(-36, 0, 0), left=(60, 1, 2), right=(60, 1, 2))

    p.key(0, rest)
    p.key(.18, stand)
    p.hold(.18, .32, breath)
    # ⚠️ v9 (2026-10-08): HE HANGS IN THE AIR, SO NOTHING OF HIM HANGS STILL (owner, of the first hover: "he goes up and down
    # too fast, i'd apprecieate if his non-moving arm and legs also swiveled around slowly"). These two stretches were
    # HOLDS: one pose frozen while he floated. They are slow drifts now: his free right arm and both legs turn a little
    # one way and back, each on its own count and never together, as a body does with nothing under it.
    p.key(.46, offer)
    p.key(.52, offer.but(right=(9, 29, 5), legs=((6, 10), (-4, 11))))
    p.key(.58, offer.but(right=(0, 25, 9), legs=((2, 13), (3, 9))))
    p.key(.64, offer.but(right=(7, 33, 4), legs=((-4, 11), (6, 12))))
    p.key(.70, receive.but(right=(1, 28, 8), legs=((-1, 13), (1, 10))))
    p.key(.78, ignite, punch=True)
    p.key(.86, ignite.but(right=(15, 33, 6), legs=((6, 12), (-5, 11))))
    p.key(.94, ignite.but(right=(7, 29, 10), legs=((-3, 13), (5, 10))))
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
    haul = plant.but(torso=(8, 0, 0), head=(-30, 0, 0), left=(52, 1, 2), right=(52, 1, 2))
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
    # ⚠️ v9 (2026-10-08): HER LIGHT LIFTS HIM, AND THE SLAM IS A FALL. He stood a hair off the court by accident (a 3 cm rise
    # before the drop, and whatever the map's court is against his feet), and the owner, circling the gap: "is he supposed
    # to be floating off the ground? if so can you make it more obvious? make him hover up and down or more in the air".
    # So it is meant now: as she comes close he is drawn up off the court (0.30 to 0.55), hangs there bobbing while the
    # light comes to him and runs through him, rises a little more as his eyes take it, and DROPS from there into the
    # slam, which gives the slam the height it never had.
    # One slow rise, one slow dip and a last lift (the first cut bobbed four times in a second and a half: "too fast").
    p.rise(0, 0.0).rise(.30, 0.0).rise(.50, 0.34).rise(.62, 0.42).rise(.80, 0.36).rise(1.00, 0.46).rise(1.10, 0.50).rise(1.22, -0.34).rise(5.0, -0.34)
    # 1 CALL. Front, low three-quarter from his right, one slow push-in, her head held above his.
    # ⚠️ v9 (2026-10-08): SHE IS IN THE SKY BEHIND HIM NOW (`HeroIntroductionScene.Paete.cs`, `MakilingSky`), so this shot is
    # lower and looks UP past him: he is the bottom third of the frame, the sky she rises into is the rest.
    p.shot(0, 1.1, (1.9, .42, 4.9), (.0, 2.2, -1.0), 48, eye_to=(1.5, .46, 4.2), look_to=(.0, 2.35, -1.0), fov_to=46)
    # 2 ROOT. Close and low on his front right at kneel height, a slow push-in toward his hands and face: the drop, the roots
    # digging in, the channel. Her reaching hands are the top of the frame; she is the light, he is the subject.
    # ⚠️ Wider than first cut (`PaeteSpiritReviewProbe` v4): she is directly behind him, bent over him, and her hands and face are
    # the top of this frame; he, his hands on the court and his roots are the bottom. It pushes in toward his hands as he channels.
    # v7: it ends at the send (2.1), 0.2 s earlier, so the push-in is a little shorter.
    # v9 (2026-10-08): it ends at 1.93, on his third heartbeat, where the camera goes under the court (below).
    # v9: and this one is on the court in front of his hands, looking up at him with her over him in the sky.
    # ⚠️ v9 (2026-10-08): AN INSERT ON HIS HANDS FOR THE SLAM, 1.17 to 1.52. His arms are PLANTED in the court now (owner: "make
    # it so it really looks like his arms are being planted into the ground"), and in the shot below his hands sit at the
    # bottom edge behind her meadow, so none of it was seen; asked whether to add a close insert, he said it looks good. Low
    # and close on his right, on the two mouths where his arms go in, pushing in a little as the court breaks round them.
    p.shot(1.1, 1.17, (1.1, .34, 3.6), (.0, 1.55, .1), 52, eye_to=(1.08, .34, 3.55), look_to=(.0, 1.54, .12), fov_to=52)
    p.shot(1.17, 1.52, (1.55, .62, 2.35), (-.02, .16, 1.0), 42, eye_to=(1.35, .52, 2.15), look_to=(-.02, .10, 1.0), fov_to=40)
    p.shot(1.52, 1.93, (1.0, .32, 3.3), (.0, 1.48, .2), 51, eye_to=(.9, .30, 3.0), look_to=(.0, 1.42, .3), fov_to=49)
    # 3 RISE, one continuous shot in three moves (no cut): held wide and low while the roots cross the frame from him (left) to
    # the spot (right) under the brush-stroke of light; craning up through the three heaves so the whole tree is in by the time
    # it tops out; then a push IN toward its face as its eyes light (4.5).
    # ⚠️ v6 (2026-09-27): re-framed for the v9 tree, 6.6 m with its eyes at 3.1 m (was 9 m with eyes over 4 m; owner: *"Make
    # the tre a bit smaller and a lot more sleek so that it isnt too distracting"*). A shorter crane, a tighter lens at the end,
    # and it closes in on the eyes rather than backing off to fit a crown that is no longer there.
    # v7: the same three moves on the faster clock (the race 2.1 to 2.5, the crane through the hauls to 3.45, the push in on the
    # eyes, which open at 3.71).
    # ⚠️⚠️ v9 (2026-10-08), THE DIVE, 1.93 to 2.7 (owner, choosing it from three ideas: "The camera goes underground. When he
    # slams the court, the camera drops through it and rides his root through the dark soil, then bursts back up with the
    # tree's first claw."). The move is COMPUTED (`HeroIntroductionScene.PaeteUnder.cs`, `PuCamera`), as THE TAKE's is; this
    # row is the old wide hold of the roots crossing the court, used as written only by the storyboard. It is ONE shot so
    # the phase camera judges it once (`UltimatePhaseView.ChooseShot` mirrors or pushes in per shot, at the shot's end, and
    # by then the camera is back above the court).
    p.shot(1.93, 2.7, (8.2, 1.25, 5.0), (.2, .9, 3.0), 54, eye_to=(8.3, 1.3, 5.1), look_to=(.2, .95, 3.1))
    # ⚠️ v9 (2026-10-08): THE RISE IS SHOT FROM IN FRONT, WITH HER BEHIND IT. It was shot from his right side, where she and
    # her dusk are out of frame, so the cut back from under the court went from a goddess in a dusk sky to plain daylight
    # (owner: "you can see a mismatch when it cuts back to both paete and the tree"). The same two moves (the crane
    # through the three heaves, the push in on its eyes) from ahead and to his right: the tree near, him beyond it, and
    # her over them both in the sky, watching it rise.
    p.shot(2.7, 3.45, (5.4, 1.3, 12.0), (.2, 1.1, 4.4), 54, eye_to=(5.9, 2.0, 12.8), look_to=(.1, 2.9, 4.8), fov_to=58)
    p.shot(3.45, 3.8, (5.9, 2.0, 12.8), (.1, 2.9, 4.8), 58, eye_to=(5.3, 2.4, 12.0), look_to=(.1, 3.2, 5.0), fov_to=52)
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
    # ⚠️⚠️ v9 (2026-10-08): 9.0 S, AND NOT EVENLY. The owner on the first cut with her in the sky and the dive in it, both
    # squeezed into 6.5 s: *"it looks like its sped up. theres not much weight to the timing of things"*; asked, he chose to
    # lengthen it to about 9 s. The 5.0 s clock is now mapped to real time by a TABLE (`PAETE_CLOCK`, `PAETE_REAL`, straight
    # lines between the points): her arrival and the dive get the time, the blows keep close to the pace they had. The same
    # table is in `HeroIntroductionScene.Paete.cs` (`PaeteClockAt`, `PaeteRealAt`) and `tools/build_paete_audio.py` (`theme`).
    return _warp(p, PAETE_CLOCK, PAETE_REAL)


PAETE_CLOCK = (0.0, 0.55, 0.85, 1.10, 1.93, 2.70, 3.80, 5.0)
PAETE_REAL = (0.0, 2.00, 2.70, 3.10, 4.34, 5.94, 7.44, 9.0)


def _warp(p, clock, real):
    def at(x):
        for i in range(len(clock) - 1):
            if x <= clock[i + 1] or i == len(clock) - 2:
                return round(real[i] + (x - clock[i]) * (real[i + 1] - real[i]) / (clock[i + 1] - clock[i]), 3)
    p.seconds = real[-1]
    p.keys = [(at(t), pose) for t, pose in p.keys]
    p.punches = [at(t) for t in p.punches]
    p.holds = [(at(a), at(b)) for a, b in p.holds]
    p.lift = [(at(t), m) for t, m in p.lift]
    p.shots = [(at(s[0]), at(s[1])) + tuple(s[2:]) for s in p.shots]
    return p


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
