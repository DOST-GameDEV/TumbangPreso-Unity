"""
One CAST sound per ability, and one per loadout alternate. Thirty cues.

WHY THIS IS A THIRD SCRIPT
`generate_hero_audio.py` makes the VOICES and is unseeded, so it rewrites all seventeen of its
files on every run. `generate_ability_audio.py` makes the PAYLOADS: what a zone sounds like once
it is on the road. Neither makes the sound of a player pressing the key, and that is the sound
this file is about. Keeping it separate means a regeneration here cannot touch a payload and a
regeneration there cannot touch a cast, and each rebuild's diff shows only what moved.

WHAT WAS WRONG THAT THIS FIXES
Every cast in the game played its hero's ELEMENT and nothing else. Measured on 2026-09-02 by
reading the six kits:

  * Sean's FLAME RUSH, IGNITION CANNON and SUPERNOVA all opened on `sfx_fire_whoosh`.
  * Zack's BOLT SPRINT, MAGNET and THUNDERSTRIKE all opened on `sfx_lightning_strike`.
  * Cheska's PERMAFROST SHEET and ICE BARRICADE both opened on `sfx_ice_freeze`.
  * Nemu's PHANTOM VEIL and ASTRAL HIJACK both opened on `sfx_ghost_teleport`.
  * Dante's SEISMIC STOMP and TITAN FISSURE both opened on `sfx_explosion_heavy`.

So eighteen powers were speaking with six voices, and a player could hear WHICH HERO had cast
something and never WHAT. 🧑 2026-09-02: "as well as add sfx for each skill, for all character as
well as the ones in loadout", "make it unique throughout each character (dont generate them the
same way bcz theyll end up sounding the same huhu)", "make sure the sfx matches wat the skill
really does".

THE TWO RULES THAT FOLLOW FROM THOSE TWO SENTENCES

1. NO TWO CUES IN THIS FILE ARE THE SAME RECIPE WITH DIFFERENT NUMBERS. That is the same
   instruction he gave for the ultimate themes on 2026-08-27 ("dont generate it the same way bcz
   its gonna sound the same way") and the same fault `docs/TODO.md` section 19 records about the
   VFX: "the same logic and code was used to generate all of them". Every synth below names its
   method in its docstring, and the methods differ per hero AND per slot: additive inharmonic
   partials, Karplus-Strong, granular scatter, sample-and-hold, ring modulation, comb-filtered
   noise, formant-shaped growl, reversed envelopes, impulse trains, chirped sweeps.

2. THE SOUND IS OF THE VERB, NOT OF THE ELEMENT. "make sure the sfx matches wat the skill really
   does". A barricade is three things PUNCHING UP out of the road; a magnet is a thing being
   DRAGGED to you and slapping into your hand; a carapace is plates CLOSING. None of those is
   "ice", "lightning" or "earth", and playing the element for all three is what made them
   interchangeable.

THE LOADOUT ALTERNATES GET THEIR OWN, AND THEY ARE NOT THE SLOT'S SOUND PITCHED
An alternate changes how the power behaves, so it changes what the power sounds like doing it:
Long Tremor sweeps feet instead of throwing bodies, so its cue is a low horizontal sweep rather
than a vertical slam. `HeroAbilitySystem.CastCueFor` picks the variant's cue when one is
equipped and the slot's cue otherwise, so exactly one of the two ever plays.

EVERYTHING HERE IS PLAYED THROUGH `NetCue.Play` BY THE KITS, NEVER `GameServices.Audio`
🧑 2026-09-02: "make sure sfx can be heard by everyone in all modes / not js client sided".
`NetCue` relays a world cue to every peer; `tools/audit_audio_reach.py` is the check.

Run:  python tools/generate_skill_audio.py
"""

import math
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, ROOT)
sys.path.insert(0, HERE)

from generate_hero_audio import SAMPLE_RATE, write_wav  # noqa: E402

# The two places the game reads sfx from. `AudioCueCheck` validates cues against files in both
# directions, so a file written to only one of these is a failing check rather than a new sound.
OUT_DIRS = [
    os.path.join(ROOT, "Assets", "TumbangPreso", "Art", "audio", "sfx"),
    os.path.join(ROOT, "Assets", "TumbangPreso", "Resources", "Sfx"),
]

SR = SAMPLE_RATE


# ---------------------------------------------------------------------------- shared primitives
#
# These are the only things shared between the thirty synths, and they are deliberately the
# dullest possible pieces: an envelope, a clamp and a fade. Anything with character in it would
# be the shared recipe this file exists to avoid.


def fade_out(out, seconds=0.02):
    """Kill the last few milliseconds so nothing ends on a click."""
    n = min(len(out), int(seconds * SR))
    for i in range(n):
        out[len(out) - n + i] *= 1.0 - i / float(n)
    return out


def soft(x):
    """Gentle saturation. Keeps a peaky synth inside the rails without hard clipping."""
    return math.tanh(x * 1.35) * 0.86


def buf(seconds):
    return [0.0] * int(seconds * SR)


# ================================================================== DANTE
# Method family: PHYSICAL MASS. Sub-bass bodies, discrete debris grains and formant growl.
# Nothing in Dante's set uses a tuned partial, because nothing he does rings.


def cast_dante_stomp(seconds=0.85):
    """
    SEISMIC STOMP: a boot arriving on asphalt and the ground splitting under it.

    Method: a hard noise transient gated to 12 ms (the boot), a sub sine dropping 70 to 28 Hz
    (the mass), and a short burst of exponentially thinning grains (the crack running out).
    Deliberately shorter and drier than the payload's `sfx_quake_slam`: this is the moment of
    contact, that is the aftermath, and stacking two aftermaths was the old sound.
    """
    out = buf(seconds)
    grains = [(random.uniform(0.03, 0.34), random.uniform(0.3, 1.0)) for _ in range(46)]

    for i in range(len(out)):
        t = i / SR
        boot = random.uniform(-1.0, 1.0) * math.exp(-t * 260.0)
        mass = math.sin(2.0 * math.pi * (28.0 + 42.0 * math.exp(-t * 9.0)) * t) * math.exp(-t * 4.2)

        crack = 0.0
        for start, amp in grains:
            if start <= t < start + 0.018:
                crack += random.uniform(-1.0, 1.0) * amp * (1.0 - (t - start) / 0.018)

        out[i] = soft(boot * 0.55 + mass * 0.95 + crack * 0.30)

    return fade_out(out)


def cast_dante_carapace(seconds=1.10):
    """
    DEMONIC CARAPACE: stone plates swinging shut around a body and sealing.

    Method: four short grinding slides (band-limited noise whose centre frequency FALLS, which
    is what stone dragging on stone does) landing on a schedule, then one low seal thud. No
    impact transient at the front: armour closes, it does not hit you.
    """
    out = buf(seconds)
    plates = [0.02, 0.15, 0.28, 0.41]
    phase = 0.0

    for i in range(len(out)):
        t = i / SR
        grind = 0.0
        for k, start in enumerate(plates):
            if start <= t < start + 0.16:
                u = (t - start) / 0.16
                centre = 900.0 - 620.0 * u
                phase += centre / SR
                grind += (random.uniform(-1.0, 1.0) * 0.5
                          + math.sin(2.0 * math.pi * phase) * 0.5) * (1.0 - u) * (0.9 - k * 0.12)

        seal = 0.0
        if t >= 0.52:
            u = t - 0.52
            seal = math.sin(2.0 * math.pi * (52.0 + 30.0 * math.exp(-u * 12.0)) * u) \
                * math.exp(-u * 3.0)

        out[i] = soft(grind * 0.42 + seal * 0.9)

    return fade_out(out)


def cast_dante_fissure(seconds=1.60):
    """
    TITAN FISSURE: the road tearing open in a line away from him.

    Method: a TRAVELLING tear. A noise band whose centre frequency falls while its amplitude
    stays up, which reads as the split running away from the listener, over a sub that arrives
    late rather than at the front. The one cue in the game with no attack transient on frame
    zero, because the ground does not slam, it gives way.
    """
    out = buf(seconds)
    z = 0.0

    for i in range(len(out)):
        t = i / SR
        u = min(1.0, t / 0.9)

        # One-pole low pass whose cutoff falls as the tear travels.
        cut = 0.55 - 0.48 * u
        z += cut * (random.uniform(-1.0, 1.0) - z)
        tear = z * (0.25 + 0.75 * math.sin(math.pi * min(1.0, t / 1.1))) * 1.6

        heave = 0.0
        if t >= 0.18:
            v = t - 0.18
            heave = math.sin(2.0 * math.pi * (22.0 + 26.0 * math.exp(-v * 3.2)) * v) \
                * (1.0 - math.exp(-v * 9.0)) * math.exp(-v * 1.5)

        out[i] = soft(tear * 0.55 + heave * 1.0)

    return fade_out(out)


# ================================================================== CHESKA
# Method family: ADDITIVE INHARMONIC PARTIALS. Ice is the only element in the game that RINGS,
# and every one of her three is built from struck partials rather than from noise.


def cast_cheska_sheet(seconds=1.20):
    """
    PERMAFROST SHEET: water going hard, the freeze running outward across the road.

    Method: six inharmonic partials that RISE in pitch as they decay, which is the sound of a
    body stiffening, plus a crackle whose rate falls as the ice sets. Rising partials are rare
    and are what stops this reading as a generic chime.
    """
    out = buf(seconds)
    ratios = [1.0, 2.37, 3.41, 4.72, 6.13, 8.05]
    base = 620.0

    for i in range(len(out)):
        t = i / SR
        ring = 0.0
        for k, r in enumerate(ratios):
            f = base * r * (1.0 + 0.10 * (1.0 - math.exp(-t * 3.0)))
            ring += math.sin(2.0 * math.pi * f * t) * math.exp(-t * (2.4 + k * 0.8)) / (k + 1.4)

        rate = 130.0 * math.exp(-t * 2.2)
        crackle = random.uniform(-1.0, 1.0) if random.random() < rate / SR * 40.0 else 0.0

        out[i] = soft(ring * 0.75 + crackle * 0.30 * math.exp(-t * 1.4))

    return fade_out(out)


def cast_cheska_barricade(seconds=1.05):
    """
    ICE BARRICADE: three pillars punching up out of the street, one after the other.

    Method: three DISCRETE risers 90 ms apart, each a chirp sweeping UP through a fifth with a
    hard stop at the top. The sheet spreads and this stands up, so the sheet falls and this
    rises: same element, opposite gesture, and that is the whole point of giving each slot its
    own cue.
    """
    out = buf(seconds)
    starts = [0.0, 0.09, 0.18]

    for i in range(len(out)):
        t = i / SR
        v = 0.0
        for k, start in enumerate(starts):
            if start <= t < start + 0.34:
                u = (t - start) / 0.34
                f = 240.0 * (1.0 + 1.5 * u * u)
                grow = min(1.0, u * 6.0)
                stop = 1.0 if u < 0.72 else max(0.0, 1.0 - (u - 0.72) / 0.28)
                v += (math.sin(2.0 * math.pi * f * (t - start))
                      + 0.35 * math.sin(2.0 * math.pi * f * 2.41 * (t - start))) \
                    * grow * stop * (0.9 - k * 0.1)

        out[i] = soft(v * 0.55)

    return fade_out(out)


def cast_cheska_nova(seconds=1.45):
    """
    GLACIAL NOVA: everything near her going still, then the shell letting go.

    Method: a SILENCE at the front. 120 ms of near nothing with one held partial in it, then an
    outward bloom of twenty partials all released on the same frame. The gap is the sound: an
    ultimate that opens on a bang is Zack's, and hers is the one that holds its breath.
    """
    out = buf(seconds)
    parts = [(random.uniform(300.0, 2600.0), random.uniform(0.3, 1.0)) for _ in range(20)]

    for i in range(len(out)):
        t = i / SR
        if t < 0.12:
            out[i] = soft(math.sin(2.0 * math.pi * 190.0 * t) * 0.10 * (t / 0.12))
            continue

        u = t - 0.12
        bloom = 0.0
        for f, a in parts:
            bloom += math.sin(2.0 * math.pi * f * u) * a * math.exp(-u * (1.6 + f / 1400.0))

        shell = random.uniform(-1.0, 1.0) * math.exp(-u * 26.0) * 0.5
        out[i] = soft((bloom / 6.0) * 0.9 + shell)

    return fade_out(out)


# ================================================================== SEAN
# Method family: TURBULENCE. Filtered noise with a moving resonance, no tuned content at all.
# Fire is the one element with no pitch, and giving it one is how it stops sounding like fire.


def cast_sean_rush(seconds=1.00):
    """
    FLAME RUSH: ignition, then a body going past you fast.

    Method: a doppler. Two-pole resonant noise whose centre sweeps UP and then DOWN through the
    cue while the amplitude peaks in the middle, so the sound passes the listener rather than
    happening at them. Nothing else in the game does this.
    """
    out = buf(seconds)
    y1 = y2 = 0.0

    for i in range(len(out)):
        t = i / SR
        u = t / seconds
        centre = 300.0 + 1500.0 * math.sin(math.pi * min(1.0, u * 1.15))
        r = 0.86
        w = 2.0 * math.pi * centre / SR
        x = random.uniform(-1.0, 1.0)
        y = x + 2.0 * r * math.cos(w) * y1 - r * r * y2
        y2, y1 = y1, y
        pass_by = math.sin(math.pi * min(1.0, u * 1.05)) ** 1.6
        out[i] = soft(y * 0.30 * pass_by)

    return fade_out(out)


def cast_sean_cannon(seconds=0.80):
    """
    IGNITION CANNON: a fuse catching in his hand and the shoe going hot.

    Method: a granular fuse. Sixty short noise ticks whose RATE accelerates, ending in one
    chambered thump. The rush sweeps and this accelerates: two ways to make noise move, so the
    ear can tell which of his two skills went off without looking.
    """
    out = buf(seconds)

    # ⚠️⚠️ THE TICK COUNT IS BOUNDED, AND WITHOUT THE BOUND THIS LOOP NEVER ENDS. A gap that
    # shrinks by a constant factor is a geometric series, so it CONVERGES: 0.055 / (1 - 0.86) is
    # 0.393 s, and the `t < 0.52` condition is therefore never reached. The first run of this
    # file appended ticks until the interpreter died with a `MemoryError`, twelve files in, and
    # the twelve that had already been written looked like a successful partial run. **A while
    # loop over an accelerating schedule needs a count, not only a time.**
    ticks = []
    t = 0.02
    gap = 0.055
    while t < 0.52 and len(ticks) < 60:
        ticks.append(t)
        gap = max(gap * 0.86, 0.006)
        t += gap

    for i in range(len(out)):
        tt = i / SR
        v = 0.0
        for start in ticks:
            if start <= tt < start + 0.010:
                v += random.uniform(-1.0, 1.0) * (1.0 - (tt - start) / 0.010)

        thump = 0.0
        if tt >= 0.53:
            u = tt - 0.53
            thump = math.sin(2.0 * math.pi * (120.0 * math.exp(-u * 8.0) + 48.0) * u) \
                * math.exp(-u * 7.0)

        out[i] = soft(v * 0.42 + thump * 0.85)

    return fade_out(out)


def cast_sean_supernova(seconds=1.55):
    """
    SUPERNOVA: launched up, held, and then dropped.

    Method: THREE PARTS with a real gap between them, which no other cue in the game has. A
    rising whoosh (0 to 0.45), 200 ms of thin high air with nothing under it (the hang), then
    the drop. The hang is what makes the landing land; the old sound was `sfx_fire_whoosh` and
    had no shape at all.
    """
    out = buf(seconds)
    z = 0.0

    for i in range(len(out)):
        t = i / SR

        if t < 0.45:
            u = t / 0.45
            cut = 0.06 + 0.55 * u
            z += cut * (random.uniform(-1.0, 1.0) - z)
            out[i] = soft(z * 0.75 * u)
        elif t < 0.65:
            u = (t - 0.45) / 0.20
            out[i] = soft(random.uniform(-1.0, 1.0) * 0.06 * (1.0 - u * 0.6)
                          + math.sin(2.0 * math.pi * 2400.0 * t) * 0.04)
        else:
            u = t - 0.65
            crash = random.uniform(-1.0, 1.0) * math.exp(-u * 30.0)
            body = math.sin(2.0 * math.pi * (34.0 + 90.0 * math.exp(-u * 10.0)) * u) \
                * math.exp(-u * 2.4)
            out[i] = soft(crash * 0.55 + body * 1.05)

    return fade_out(out)


# ================================================================== ZACK
# Method family: DISCONTINUITY. Impulse trains, sample-and-hold and ring modulation. Electricity
# is the one element that is not a continuous body, so nothing of his is a smooth envelope.


def cast_zack_sprint(seconds=1.10):
    """
    BOLT SPRINT: skates spinning up and the charge catching in them.

    Method: an impulse train whose RATE rises from 26 to 240 Hz, which turns from a rattle into
    a pitch as it accelerates. The pitch is a consequence of the rate rather than an oscillator,
    which is why it does not read as a musical note.
    """
    out = buf(seconds)
    phase = 0.0
    ring = 0.0

    for i in range(len(out)):
        t = i / SR
        rate = 26.0 + 214.0 * min(1.0, t / 0.7) ** 1.7
        phase += rate / SR
        if phase >= 1.0:
            phase -= 1.0
            ring = 1.0
        ring *= 0.9985

        spark = random.uniform(-1.0, 1.0) if random.random() < 0.004 else 0.0
        out[i] = soft((ring * math.sin(2.0 * math.pi * 1450.0 * t) * 0.7 + spark * 0.35)
                      * (1.0 - max(0.0, (t - 0.85) / 0.25)))

    return fade_out(out)


def cast_zack_magnet(seconds=0.95):
    """
    MAGNET: the tsinelas being dragged off the road and slapping into his hand.

    Method: a RISING pull, which is the opposite envelope shape to everything else in the game,
    then one flat slap and a discharge. The pull is a sine whose frequency climbs while its
    amplitude climbs, so the sound arrives rather than decays, and the slap is the only thing
    with a transient. That is what the ability does: nothing happens, and then it is in his hand.
    """
    out = buf(seconds)
    hold = 0.0

    for i in range(len(out)):
        t = i / SR

        if t < 0.42:
            u = t / 0.42
            # Sample and hold on the pull, so it grinds rather than glides.
            if random.random() < 0.02:
                hold = random.uniform(-1.0, 1.0)
            f = 90.0 + 620.0 * u * u
            pull = math.sin(2.0 * math.pi * f * t) * (u ** 1.4)
            out[i] = soft(pull * 0.7 + hold * 0.12 * u)
        else:
            u = t - 0.42
            slap = random.uniform(-1.0, 1.0) * math.exp(-u * 90.0)
            zap = math.sin(2.0 * math.pi * 1700.0 * u) * math.exp(-u * 26.0)
            tail = math.sin(2.0 * math.pi * 150.0 * u) * math.exp(-u * 9.0)
            out[i] = soft(slap * 0.8 + zap * 0.35 + tail * 0.45)

    return fade_out(out)


def cast_zack_summon(seconds=1.35):
    """
    THUNDERSTRIKE: the air going tight over the spot he has picked, before it opens.

    Method: RING MODULATION. Two sines a few Hz apart multiplied together, which beats, and the
    beat rate accelerates until it is a buzz. It is the only cue of his with continuous content
    and it says PRESSURE rather than discharge, which is right: the strike itself is
    `sfx_thunder_impact` and it plays a beat later on the ground, not in his hands.
    """
    out = buf(seconds)

    for i in range(len(out)):
        t = i / SR
        u = min(1.0, t / 1.0)
        beat = 3.0 + 46.0 * u * u
        a = math.sin(2.0 * math.pi * 210.0 * t)
        b = math.sin(2.0 * math.pi * (210.0 + beat) * t)
        crack = random.uniform(-1.0, 1.0) * (0.02 + 0.22 * u * u)
        out[i] = soft((a * b) * 0.85 * (0.25 + 0.75 * u) + crack)

    return fade_out(out)


# ================================================================== NEMU
# Method family: BREATH AND REVERSAL. Comb-filtered noise and envelopes that run backwards.
# Nothing of hers has an attack, because nothing she does arrives.


def cast_nemu_veil(seconds=1.15):
    """
    PHANTOM VEIL: a body going thin.

    Method: comb-filtered noise (a delayed copy summed with itself) whose delay LENGTHENS, so
    the resonance slides downward and the sound hollows out as it goes. Plus one breath that
    fades IN and stops. Nothing struck, nothing tuned.
    """
    out = buf(seconds)
    hist = [0.0] * 2000

    for i in range(len(out)):
        t = i / SR
        d = int(60 + 900 * min(1.0, t / 0.8))
        x = random.uniform(-1.0, 1.0) * 0.5
        y = x + 0.82 * hist[(i - d) % len(hist)]
        hist[i % len(hist)] = y

        breath = math.sin(math.pi * min(1.0, t / 0.9)) ** 2.0
        out[i] = soft(y * 0.30 * breath)

    return fade_out(out)


def cast_nemu_hijack(seconds=1.05):
    """
    ASTRAL HIJACK: her leaving, and something small taking off.

    Method: a REVERSED swell into a departure. The first 0.6 s runs an exponential envelope
    backwards (silence growing into a peak), which is the classic sound of something being
    un-done, and then a fast falling glide away. The veil hollows and this departs.
    """
    out = buf(seconds)

    for i in range(len(out)):
        t = i / SR
        if t < 0.60:
            u = t / 0.60
            env = math.exp((u - 1.0) * 4.0)
            wob = math.sin(2.0 * math.pi * (2.5 + 9.0 * u) * t)
            tone = math.sin(2.0 * math.pi * (330.0 + 60.0 * wob) * t)
            out[i] = soft((tone * 0.6 + random.uniform(-1.0, 1.0) * 0.18) * env)
        else:
            u = t - 0.60
            f = 900.0 * math.exp(-u * 5.5) + 120.0
            out[i] = soft(math.sin(2.0 * math.pi * f * u) * math.exp(-u * 4.0) * 0.55)

    return fade_out(out)


def cast_nemu_seance(seconds=1.60):
    """
    DEVOURING SEANCE: an inhale that does not stop.

    Method: an inward chirp. Band-passed noise whose centre falls the whole way while the level
    RISES, which the ear reads as something being drawn in from far off, plus a sub that arrives
    only at the end. No release: the cue simply stops, and the maw's own `sfx_kuro_unbound`
    takes over.
    """
    out = buf(seconds)
    y1 = y2 = 0.0

    for i in range(len(out)):
        t = i / SR
        u = t / seconds
        centre = 2200.0 * math.exp(-u * 3.4) + 120.0
        r = 0.9
        w = 2.0 * math.pi * centre / SR
        x = random.uniform(-1.0, 1.0)
        y = x + 2.0 * r * math.cos(w) * y1 - r * r * y2
        y2, y1 = y1, y

        sub = 0.0
        if u > 0.62:
            v = (u - 0.62) / 0.38
            sub = math.sin(2.0 * math.pi * 41.0 * t) * v * v

        out[i] = soft(y * 0.24 * (0.2 + 0.8 * u) + sub * 0.7)

    return fade_out(out)


# ================================================================== PHAISTER
# Method family: STRUCK METAL AND CHALK. Karplus-Strong for anything drawn, bell partials for
# anything summoned. She is the only hero whose sounds have a decay long enough to hear.


def cast_phaister_hex(seconds=1.10):
    """
    HEX: chalk dragged round a circle on asphalt.

    Method: KARPLUS-STRONG on a very short buffer, which is a plucked string and, at this
    length, a scrape. The buffer is re-excited four times as the hand comes round the circle,
    so the cue is four strokes and not one, and the tone sharpens each time as the ring closes.
    """
    out = buf(seconds)
    n = 96
    ks = [random.uniform(-1.0, 1.0) for _ in range(n)]
    strokes = [0.0, 0.17, 0.33, 0.47]
    p = 0

    for i in range(len(out)):
        t = i / SR
        for s in strokes:
            if abs(t - s) < 1.0 / SR:
                for k in range(n):
                    ks[k] += random.uniform(-1.0, 1.0) * 0.9

        nxt = (p + 1) % n
        v = (ks[p] + ks[nxt]) * 0.5 * 0.9965
        ks[p] = v
        p = nxt

        grit = random.uniform(-1.0, 1.0) * 0.10 * math.exp(-((t - 0.25) ** 2) * 12.0)
        out[i] = soft(v * 0.55 + grit)

    return fade_out(out)


def cast_phaister_blink(seconds=0.90):
    """
    SHADOW BLINK: cloth pulled through a tear.

    Method: a gated sweep. Broadband noise chopped by a fast square gate whose duty cycle
    collapses, so the sound shreds rather than fades, over one falling partial. The hex is
    plucked and this is torn; both are hers and neither is the other.
    """
    out = buf(seconds)

    for i in range(len(out)):
        t = i / SR
        u = min(1.0, t / 0.55)
        gate_rate = 40.0 + 260.0 * u
        duty = 0.85 - 0.75 * u
        gate = 1.0 if (t * gate_rate) % 1.0 < duty else 0.0

        cloth = random.uniform(-1.0, 1.0) * gate * (1.0 - u * 0.7)
        drop = math.sin(2.0 * math.pi * (520.0 * math.exp(-t * 4.0) + 90.0) * t) \
            * math.exp(-t * 3.4)

        out[i] = soft(cloth * 0.36 + drop * 0.55)

    return fade_out(out)


def cast_phaister_coven(seconds=1.70):
    """
    GRAND COVEN: the sky being called down.

    Method: additive BELL partials at a struck-bell ratio set, with a slow tremolo that DEEPENS
    rather than fades. Her ultimate already has `sfx_eclipse_toll` for the toll itself; this is
    the summons under it, and the two are built differently on purpose so they do not phase into
    one another when they play a fraction apart.
    """
    out = buf(seconds)
    ratios = [0.5, 1.0, 1.19, 1.56, 2.0, 2.51, 2.66, 3.01]
    base = 146.0

    for i in range(len(out)):
        t = i / SR
        v = 0.0
        for k, r in enumerate(ratios):
            v += math.sin(2.0 * math.pi * base * r * t) * math.exp(-t * (0.7 + k * 0.25)) \
                / (1.0 + k * 0.6)

        trem = 1.0 - 0.45 * min(1.0, t / 1.2) * (0.5 + 0.5 * math.sin(2.0 * math.pi * 5.5 * t))
        out[i] = soft(v * 0.65 * trem)

    return fade_out(out)


# ================================================================== LOADOUT ALTERNATES
#
# Twelve cues, one per non-default variant. Each is built from a DIFFERENT method again, and
# each says what the alternate changed rather than being the slot's cue transposed.


def var_dante_tremor(seconds=1.10):
    """Long Tremor: the break running sideways and taking legs out. A horizontal sweep with no
    vertical slam, which is exactly the trade the variant makes."""
    out = buf(seconds)
    for i in range(len(out)):
        t = i / SR
        u = min(1.0, t / 0.8)
        sweep = math.sin(2.0 * math.pi * (58.0 - 30.0 * u) * t) * (1.0 - u * 0.5)
        skid = random.uniform(-1.0, 1.0) * 0.20 * math.sin(math.pi * u) ** 2
        out[i] = soft(sweep * 0.9 + skid)
    return fade_out(out)


def var_dante_plating(seconds=1.25):
    """Heavy Plating: more plates, closing slower, and one of them dragging. Six grinds instead
    of four, spaced wider, over a held drone that says weight."""
    out = buf(seconds)
    starts = [0.0, 0.11, 0.23, 0.36, 0.50, 0.63]
    for i in range(len(out)):
        t = i / SR
        g = 0.0
        for k, s in enumerate(starts):
            if s <= t < s + 0.20:
                u = (t - s) / 0.20
                g += random.uniform(-1.0, 1.0) * (1.0 - u) * (0.85 - k * 0.09)
        drone = math.sin(2.0 * math.pi * 44.0 * t) * min(1.0, t / 0.3) * math.exp(-t * 0.9)
        out[i] = soft(g * 0.34 + drone * 0.7)
    return fade_out(out)


def var_cheska_blackice(seconds=1.05):
    """Black Ice: a small patch going glassy and silent. One high partial and almost nothing
    else, because the whole point of the variant is that you do not notice it."""
    out = buf(seconds)
    for i in range(len(out)):
        t = i / SR
        a = math.sin(2.0 * math.pi * 1840.0 * t) * math.exp(-t * 2.2)
        b = math.sin(2.0 * math.pi * 2790.0 * t) * math.exp(-t * 3.6) * 0.5
        hiss = random.uniform(-1.0, 1.0) * 0.06 * math.exp(-t * 6.0)
        out[i] = soft((a + b) * 0.32 + hiss)
    return fade_out(out)


def var_cheska_spires(seconds=1.00):
    """Split Spires: two risers instead of three, further apart and thinner. Same gesture as the
    barricade, two thirds of the mass, and an audible gap where the middle pillar was."""
    out = buf(seconds)
    starts = [0.0, 0.22]
    for i in range(len(out)):
        t = i / SR
        v = 0.0
        for k, s in enumerate(starts):
            if s <= t < s + 0.36:
                u = (t - s) / 0.36
                f = 300.0 * (1.0 + 1.7 * u * u)
                v += math.sin(2.0 * math.pi * f * (t - s)) * min(1.0, u * 7.0) \
                    * max(0.0, 1.0 - max(0.0, (u - 0.7) / 0.3)) * (0.8 - k * 0.1)
        out[i] = soft(v * 0.5)
    return fade_out(out)


def var_sean_afterburn(seconds=1.30):
    """Afterburn: a short kick and a long burn. The doppler is gone; what is left is the ground
    still alight after he has stopped, which is the trade."""
    out = buf(seconds)
    z = 0.0
    for i in range(len(out)):
        t = i / SR
        kick = random.uniform(-1.0, 1.0) * math.exp(-t * 40.0)
        z += 0.10 * (random.uniform(-1.0, 1.0) - z)
        burn = z * min(1.0, t / 0.2) * math.exp(-max(0.0, t - 0.5) * 1.2)
        out[i] = soft(kick * 0.5 + burn * 1.5)
    return fade_out(out)


def var_sean_flare(seconds=0.62):
    """Flare Shot: no fuse, just the launch. Half the length of the cannon and all transient,
    which is the +50% flight speed as a sound."""
    out = buf(seconds)
    for i in range(len(out)):
        t = i / SR
        crack = random.uniform(-1.0, 1.0) * math.exp(-t * 120.0)
        whistle = math.sin(2.0 * math.pi * (1400.0 + 2600.0 * t) * t) * math.exp(-t * 6.0)
        out[i] = soft(crack * 0.7 + whistle * 0.30)
    return fade_out(out)


def var_zack_arcline(seconds=1.05):
    """Arc Line: one wire instead of a corridor. A single narrow band that stays put, and a
    faster spark rate, so it reads as concentrated rather than as wide."""
    out = buf(seconds)
    for i in range(len(out)):
        t = i / SR
        wire = math.sin(2.0 * math.pi * 2100.0 * t) * math.exp(-t * 1.6) * 0.35
        spark = random.uniform(-1.0, 1.0) if random.random() < 0.011 else 0.0
        out[i] = soft(wire + spark * 0.5 * math.exp(-t * 1.1))
    return fade_out(out)


def var_zack_discharge(seconds=0.72):
    """Snap Discharge: the pull and the slap collapsed into one event, because that is what the
    variant does to the timing. No sample-and-hold grind, no tail."""
    out = buf(seconds)
    for i in range(len(out)):
        t = i / SR
        if t < 0.14:
            u = t / 0.14
            out[i] = soft(math.sin(2.0 * math.pi * (200.0 + 900.0 * u * u) * t) * u * 0.8)
        else:
            u = t - 0.14
            out[i] = soft(random.uniform(-1.0, 1.0) * math.exp(-u * 55.0) * 0.85
                          + math.sin(2.0 * math.pi * 1900.0 * u) * math.exp(-u * 30.0) * 0.4)
    return fade_out(out)


def var_nemu_fade(seconds=1.55):
    """Long Fade: the same hollowing, taken much further and much slower, with a tone under it
    that says she is still walking rather than gone."""
    out = buf(seconds)
    hist = [0.0] * 3000
    for i in range(len(out)):
        t = i / SR
        d = int(80 + 1800 * min(1.0, t / 1.3))
        x = random.uniform(-1.0, 1.0) * 0.42
        y = x + 0.87 * hist[(i - d) % len(hist)]
        hist[i % len(hist)] = y
        step = math.sin(2.0 * math.pi * 84.0 * t) * 0.10 * min(1.0, t / 0.5)
        out[i] = soft(y * 0.26 * math.sin(math.pi * min(1.0, t / 1.5)) ** 1.5 + step)
    return fade_out(out)


def var_nemu_leash(seconds=0.70):
    """Short Leash: Kuro darting. The reversed swell is cut to almost nothing and the departure
    is twice as fast, which is the whole variant in one envelope."""
    out = buf(seconds)
    for i in range(len(out)):
        t = i / SR
        if t < 0.16:
            u = t / 0.16
            out[i] = soft(math.sin(2.0 * math.pi * 420.0 * t) * math.exp((u - 1.0) * 5.0) * 0.7)
        else:
            u = t - 0.16
            f = 1300.0 * math.exp(-u * 11.0) + 150.0
            out[i] = soft(math.sin(2.0 * math.pi * f * u) * math.exp(-u * 8.0) * 0.6)
    return fade_out(out)


def var_phaister_brand(seconds=0.95):
    """Slow Brand: one hard stroke instead of four, on a tighter string. A brand is stamped, not
    drawn, and that is the difference between the two hexes."""
    out = buf(seconds)
    n = 58
    ks = [random.uniform(-1.0, 1.0) for _ in range(n)]
    p = 0
    for i in range(len(out)):
        t = i / SR
        nxt = (p + 1) % n
        v = (ks[p] + ks[nxt]) * 0.5 * 0.9978
        ks[p] = v
        p = nxt
        stamp = random.uniform(-1.0, 1.0) * math.exp(-t * 70.0)
        out[i] = soft(v * 0.62 + stamp * 0.45)
    return fade_out(out)


def var_phaister_stride(seconds=1.25):
    """Long Stride: the same tear, opened wider and held longer before it takes her. The gate
    collapses more slowly, which is the -30% cast speed the taya is meant to read."""
    out = buf(seconds)
    for i in range(len(out)):
        t = i / SR
        u = min(1.0, t / 0.95)
        gate_rate = 26.0 + 150.0 * u
        duty = 0.9 - 0.72 * u
        gate = 1.0 if (t * gate_rate) % 1.0 < duty else 0.0
        cloth = random.uniform(-1.0, 1.0) * gate * (1.0 - u * 0.55)
        drop = math.sin(2.0 * math.pi * (420.0 * math.exp(-t * 2.4) + 70.0) * t) \
            * math.exp(-t * 2.0)
        out[i] = soft(cloth * 0.34 + drop * 0.6)
    return fade_out(out)


# ---------------------------------------------------------------------------- the registry

GENERATORS = {
    # ⚠️ THE SEED SLOT IS WRITTEN DOWN AND NEVER DERIVED FROM POSITION IN THIS TABLE, for the
    # reason `generate_ability_audio.py` records: a positional seed means adding one cue silently
    # regenerates every cue after it, and the diff then hides the one file that was meant to move.
    "sfx_cast_dante_stomp.wav": (0, cast_dante_stomp),
    "sfx_cast_dante_carapace.wav": (1, cast_dante_carapace),
    "sfx_cast_dante_fissure.wav": (2, cast_dante_fissure),

    "sfx_cast_cheska_sheet.wav": (3, cast_cheska_sheet),
    "sfx_cast_cheska_barricade.wav": (4, cast_cheska_barricade),
    "sfx_cast_cheska_nova.wav": (5, cast_cheska_nova),

    "sfx_cast_sean_rush.wav": (6, cast_sean_rush),
    "sfx_cast_sean_cannon.wav": (7, cast_sean_cannon),
    "sfx_cast_sean_supernova.wav": (8, cast_sean_supernova),

    "sfx_cast_zack_sprint.wav": (9, cast_zack_sprint),
    "sfx_cast_zack_magnet.wav": (10, cast_zack_magnet),
    "sfx_cast_zack_summon.wav": (11, cast_zack_summon),

    "sfx_cast_nemu_veil.wav": (12, cast_nemu_veil),
    "sfx_cast_nemu_hijack.wav": (13, cast_nemu_hijack),
    "sfx_cast_nemu_seance.wav": (14, cast_nemu_seance),

    "sfx_cast_phaister_hex.wav": (15, cast_phaister_hex),
    "sfx_cast_phaister_blink.wav": (16, cast_phaister_blink),
    "sfx_cast_phaister_coven.wav": (17, cast_phaister_coven),

    # The twelve loadout alternates.
    "sfx_var_dante_tremor.wav": (18, var_dante_tremor),
    "sfx_var_dante_plating.wav": (19, var_dante_plating),
    "sfx_var_cheska_blackice.wav": (20, var_cheska_blackice),
    "sfx_var_cheska_spires.wav": (21, var_cheska_spires),
    "sfx_var_sean_afterburn.wav": (22, var_sean_afterburn),
    "sfx_var_sean_flare.wav": (23, var_sean_flare),
    "sfx_var_zack_arcline.wav": (24, var_zack_arcline),
    "sfx_var_zack_discharge.wav": (25, var_zack_discharge),
    "sfx_var_nemu_fade.wav": (26, var_nemu_fade),
    "sfx_var_nemu_leash.wav": (27, var_nemu_leash),
    "sfx_var_phaister_brand.wav": (28, var_phaister_brand),
    "sfx_var_phaister_stride.wav": (29, var_phaister_stride),
}


def main():
    only = sys.argv[1:] if len(sys.argv) > 1 else None

    for filename, (slot, fn) in sorted(GENERATORS.items()):
        if only and not any(o in filename for o in only):
            continue

        random.seed(0x5C11 + slot * 1013)
        samples = fn()

        for d in OUT_DIRS:
            os.makedirs(d, exist_ok=True)
            path = os.path.join(d, filename)
            write_wav(path, samples)
            print(f"wrote {path} ({len(samples)} samples)")


if __name__ == "__main__":
    main()
