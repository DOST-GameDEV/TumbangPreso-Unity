"""
The ability PAYLOAD sounds, as opposed to the cast sounds `generate_hero_audio.py` makes.

WHY THIS IS A SECOND SCRIPT AND NOT A FEW MORE ENTRIES IN THE FIRST ONE:
`generate_hero_audio.py` calls `random.uniform` with no seed, so every run rewrites all
seventeen of its .wav files with different audio. Adding a cue there would mean regenerating
and re-committing the entire existing sound set to add one sound, and the diff would hide the
one file that was meant to change. This script is seeded, so a given cue is byte-identical run
to run and a rebuild shows only what actually moved.

WHAT WAS WRONG THAT THIS FIXES:
Every hero CAST already plays its own element, wired in the kits: fire whoosh, lightning
strike, ice freeze, ghost teleport. The PAYLOADS did not. `HeroHazards.CreateExplosion` played
`ability_bagsak_bomb` for a 2.2 m stomp, a 4.5 m fissure, a 4.8 m supernova and a slipper, and
`CreateThunderstrike` played `ability_flick_dash`, which is a dash. Those five `ability_*`
files are documented in `AudioCues.DeletedAbilityCues` as belonging to a system that was
deleted, so the biggest moments in five different kits were sharing two leftovers.

The result is what 🧑 reported off the build: the skills feel repetitive and empty. They sound
it too, and for the same reason `Hero_Strike_Balance.md` section 8 gives for how they look,
which is that one asset was doing the work of five.

Run:  python tools/generate_ability_audio.py
"""

import math
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, ROOT)

from generate_hero_audio import SAMPLE_RATE, write_wav  # noqa: E402


# The two places the game reads sfx from. `AudioCueCheck` validates cues against files in both
# directions, so a file written to only one of these is a failing check rather than a new sound.
OUT_DIRS = [
    os.path.join(ROOT, "Assets", "TumbangPreso", "Art", "audio", "sfx"),
    os.path.join(ROOT, "Assets", "TumbangPreso", "Resources", "Sfx"),
]


def envelope(t, attack, decay):
    """Linear attack into an exponential decay, so nothing starts on a click."""
    if t < attack:
        return t / attack if attack > 0.0 else 1.0
    return math.exp(-(t - attack) * decay)


def synth_quake_slam(duration=1.6):
    """
    Dante. Seismic Stomp and Titan Fissure.

    Deliberately NOT the explosion: no crackle and no fizz, because nothing is burning. A
    quake is a sub-bass shove, the hard slap of the ground arriving, and then rubble. The
    rubble is discrete grains rather than continuous noise, which is what separates falling
    masonry from a hiss.
    """
    n = int(duration * SAMPLE_RATE)
    out = [0.0] * n
    grains = [(random.uniform(0.02, 0.75), random.uniform(0.25, 1.0)) for _ in range(90)]

    for i in range(n):
        t = i / SAMPLE_RATE

        # Sub shove, 46 Hz falling to 19. Lower and longer than the fire explosion's 65.
        sub = math.sin(2.0 * math.pi * (46.0 * math.exp(-t * 2.4) + 19.0) * t) * math.exp(-t * 1.9)

        # The slap: a short mid thump that says the ground was struck rather than that it blew up.
        slap = math.sin(2.0 * math.pi * (190.0 * math.exp(-t * 14.0)) * t) * math.exp(-t * 11.0)

        # Rubble. Each grain is a brief burst of band-limited noise at its own start time.
        rubble = 0.0
        for start, amp in grains:
            if start <= t < start + 0.05:
                rubble += random.uniform(-1.0, 1.0) * amp * (1.0 - (t - start) / 0.05)
        rubble *= 0.22 * math.exp(-t * 1.1)

        # A dust rumble under everything so the tail does not simply stop.
        rumble = random.uniform(-1.0, 1.0) * math.exp(-t * 2.2) * 0.18

        out[i] = math.tanh((sub * 0.95 + slap * 0.5 + rubble + rumble) * 1.25)
    return out


def synth_thunder_impact(duration=1.5):
    """
    Zack. The Thunderstrike LANDING, which is not the same event as the cast.

    `sfx_lightning_strike` is the charge and the arc, and it stays on the cast. This is the
    bolt reaching the street: a near-instant crack, a body of discharge, then thunder rolling
    away. The roll is what a dash sound could never supply and is why the ultimate had no
    weight.
    """
    n = int(duration * SAMPLE_RATE)
    out = [0.0] * n

    for i in range(n):
        t = i / SAMPLE_RATE

        # The crack. Very fast, very bright, gone in 40 ms.
        crack = random.uniform(-1.0, 1.0) * math.exp(-t * 42.0) * 1.7

        # Discharge body: detuned harmonics so it is electric rather than percussive.
        f = 300.0 + 180.0 * math.sin(2.0 * math.pi * 27.0 * t)
        p = 2.0 * math.pi * f * t
        body = (math.sin(p) + 0.45 * math.sin(p * 2.02) + 0.25 * math.sin(p * 3.05))
        body *= math.exp(-t * 5.5) * 0.55

        # Ground strike sub, so it lands on the floor and not in the air.
        sub = math.sin(2.0 * math.pi * (58.0 * math.exp(-t * 4.0) + 26.0) * t) * math.exp(-t * 3.0)

        # Thunder roll, arriving slightly late and fading slowly.
        roll_t = max(0.0, t - 0.18)
        roll = random.uniform(-1.0, 1.0) * math.exp(-roll_t * 1.6) * (1.0 - math.exp(-roll_t * 9.0)) * 0.4

        out[i] = math.tanh((crack + body + sub * 0.8 + roll) * 1.15)
    return out


def synth_frost_nova(duration=1.5):
    """
    Cheska. Glacial Nova.

    `sfx_ice_freeze` is ice FORMING, a rising chime, and it stays on the sheet and the
    barricade. A nova is ice BREAKING outward: the chimes descend rather than ring up, and
    the shards are the loudest thing in it.
    """
    n = int(duration * SAMPLE_RATE)
    out = [0.0] * n
    shards = [(random.uniform(0.0, 0.5), random.uniform(2600.0, 7200.0)) for _ in range(70)]

    for i in range(n):
        t = i / SAMPLE_RATE

        # The whump that pushes the shards out.
        whump = math.sin(2.0 * math.pi * (88.0 * math.exp(-t * 5.0) + 32.0) * t) * math.exp(-t * 4.0)

        # Descending crystal, the inverse of the freeze chime.
        val = 0.0
        for idx, base in enumerate([2400.0, 1810.0, 1290.0, 940.0]):
            f = base * (0.62 + 0.38 * math.exp(-t * 2.2))
            val += math.sin(2.0 * math.pi * f * t) * math.exp(-t * (2.6 + idx * 0.9)) / (idx + 1.4)

        # Shards. Short high sines struck at their own start times, so it glitters unevenly.
        glass = 0.0
        for start, f in shards:
            if start <= t < start + 0.10:
                a = 1.0 - (t - start) / 0.10
                glass += math.sin(2.0 * math.pi * f * (t - start)) * a * a
        glass *= 0.11

        out[i] = math.tanh((whump * 0.85 + val * 0.5 + glass) * 1.2)
    return out


def synth_possess_enter(duration=1.0):
    """
    Nemu. Leaving her body for Kuro.

    RISING, and that is the whole design. `Hero_Strike_Balance.md` section 8.6.1 says the
    possession failed to read because the swap had no duration; the sound now has the same
    shape as the camera move, so the two describe one event. It ends on arrival rather than
    fading, which is what tells the player the trip is finished.
    """
    n = int(duration * SAMPLE_RATE)
    out = [0.0] * n

    for i in range(n):
        t = i / SAMPLE_RATE
        u = t / duration

        # A rising formant pair. Voice-like without being a voice.
        f = 220.0 + 620.0 * u * u
        breath = 0.5 + 0.5 * math.sin(2.0 * math.pi * 6.5 * t)
        tone = (math.sin(2.0 * math.pi * f * t) + 0.4 * math.sin(2.0 * math.pi * f * 1.5 * t))
        tone *= envelope(t, 0.08, 2.2) * (0.55 + 0.45 * breath)

        # The suck: filtered noise pulled up with the tone.
        air = random.uniform(-1.0, 1.0) * (0.15 + 0.5 * u) * math.exp(-max(0.0, t - 0.75) * 12.0) * 0.35

        # A soft arrival tap at the top so it lands rather than stops.
        tap = 0.0
        if 0.80 <= t < 0.90:
            tap = math.sin(2.0 * math.pi * 150.0 * (t - 0.80)) * (1.0 - (t - 0.80) / 0.10) * 0.7

        out[i] = math.tanh((tone * 0.6 + air + tap) * 1.15)
    return out


def synth_possess_exit(duration=0.9):
    """Nemu, coming home. The enter sound's shape reversed: a falling formant onto a thump."""
    n = int(duration * SAMPLE_RATE)
    out = [0.0] * n

    for i in range(n):
        t = i / SAMPLE_RATE
        u = t / duration

        f = 840.0 - 560.0 * u * u
        tone = (math.sin(2.0 * math.pi * f * t) + 0.35 * math.sin(2.0 * math.pi * f * 1.5 * t))
        tone *= envelope(t, 0.02, 3.0)

        air = random.uniform(-1.0, 1.0) * (0.55 - 0.4 * u) * math.exp(-t * 3.2) * 0.3

        # Body reassembling: a low thump under the last third.
        thump = 0.0
        if t > 0.45:
            thump = math.sin(2.0 * math.pi * (120.0 * math.exp(-(t - 0.45) * 6.0) + 45.0) * (t - 0.45))
            thump *= math.exp(-(t - 0.45) * 5.0) * 0.9

        out[i] = math.tanh((tone * 0.55 + air + thump) * 1.2)
    return out


def synth_slipper_burst(duration=0.7):
    """
    The thrown tsinelas going off.

    ⚠️ SMALL AND RUBBERY ON PURPOSE. It shared `ability_bagsak_bomb` with the two ultimates,
    which told the player a slipper and a supernova were the same size of event. This game's
    joke is that the weapon is a flip-flop, and the sound should be funny rather than heavy.
    """
    n = int(duration * SAMPLE_RATE)
    out = [0.0] * n

    for i in range(n):
        t = i / SAMPLE_RATE

        # Rubber pop: a fast pitch drop with a comic squash to it.
        pop = math.sin(2.0 * math.pi * (330.0 * math.exp(-t * 22.0) + 90.0) * t) * math.exp(-t * 13.0)

        # A slap of air, brief.
        slap = random.uniform(-1.0, 1.0) * math.exp(-t * 26.0) * 0.55

        # Little bounce tail, two soft taps, so it reads as a sandal and not a grenade.
        tail = 0.0
        for start in (0.20, 0.34):
            if start <= t < start + 0.06:
                tail += math.sin(2.0 * math.pi * 260.0 * (t - start)) * (1.0 - (t - start) / 0.06) * 0.4

        out[i] = math.tanh((pop * 0.9 + slap + tail) * 1.3)
    return out


def synth_lrt_pass(duration=2.7):
    """The LRT consist crossing the guideway overhead.

    The map's signature event had NO SOUND AT ALL. `LrtTrainFlyby.Announce` called a cue named
    `ui_move`, and there has never been a `ui_move.wav`: the player log answers every pass with
    `[Audio] no cue registered for 'ui_move'`, once every 24 seconds, for the whole match. The
    one recurring event on Ilalim ng Tulay, the thing a player is supposed to learn the period
    of, was silent.

    Built to the measured window rather than to taste: `OverheadPassWindow.PassSeconds` is
    2.70 s, nose entering to tail leaving, so the sound is exactly as long as the thing it is
    describing.

    Three layers, because a train is not one noise:
      * a low rumble that swells and falls, which is the mass passing over,
      * rail clatter on the bogie period, which is what makes it a TRAIN and not wind,
      * filtered noise that peaks with the rumble, which is the air it drags.
    """
    n = int(duration * SAMPLE_RATE)
    out = []

    # The three-car consist runs 15.6 m at 18 m/s; its bogies pass at a steady beat.
    clatter_hz = 5.4
    last = 0.0

    for i in range(n):
        t = i / SAMPLE_RATE
        x = t / duration

        # A smooth swell to the middle of the pass and back. Never a click at either end.
        swell = math.sin(math.pi * x) ** 1.4

        rumble = (math.sin(2.0 * math.pi * 41.0 * t)
                  + 0.6 * math.sin(2.0 * math.pi * 27.0 * t + 0.7)
                  + 0.35 * math.sin(2.0 * math.pi * 63.0 * t + 1.9))

        # Rail joints: a short bright tick on each bogie beat.
        beat = (t * clatter_hz) % 1.0
        clatter = math.exp(-beat * 26.0) * math.sin(2.0 * math.pi * 1850.0 * t)
        clatter += 0.5 * math.exp(-beat * 40.0) * math.sin(2.0 * math.pi * 3100.0 * t + 0.4)

        # One-pole low pass on white noise, so the air reads as air rather than as hiss.
        noise = random.uniform(-1.0, 1.0)
        last += (noise - last) * 0.06
        air = last

        sample = swell * (0.52 * rumble * 0.33 + 0.22 * clatter + 0.30 * air)
        out.append(max(-1.0, min(1.0, sample)))

    return out


# =====================================================================
# THE ZONE LIFECYCLE CUES, ADDED 2026-08-26.
#
# WHAT WAS WRONG:
# Three of Cheska's four sounds were borrowed and one was borrowed BACKWARDS.
# `SpawnIceBarricade` and `SpawnIceSheet` both opened on `ability_shatter_trap`, so two
# different powers shared one cue and that cue is the sound of something BREAKING played at the
# moment something is BUILT. Worse, `IceBarricadeComponent.Shatter`, which is the one place in
# her kit where ice genuinely does break, played `slipper_land`: a rubber sandal hitting the
# road. This is the same fault the header of this file already describes against
# `ability_bagsak_bomb` and `ability_flick_dash`, in a place nobody checked when that was fixed.
#
# AND THE ZONES ALL DIED IN SILENCE. Every hazard here ticks down and calls `Destroy`, and not
# one of them made a sound doing it. `Hero_Strike_Balance.md` section 8.5 item 2 argues that a
# player being unable to tell a spent effect from a live one is a real gameplay read and that
# fixing it is free; that argument was applied to the visuals and never to the audio, which is
# the channel a player has even when they are looking somewhere else.
#
# ⚠️ THE TRAILS DELIBERATELY GET NOTHING. A dashing hero drops a mark every 0.10 to 0.30 s and
# each lives 3 s, so a single dash would fire up to thirty expiry cues inside three seconds.
# That is the same measurement `AbilityVfx` uses to keep emitters off trails, and it applies
# harder to sound: thirty overlapping tails is a wash, not a read. Singular zones only.
# =====================================================================


def synth_ice_form(duration=1.1):
    """
    Cheska. Permafrost Sheet going down.

    Ice GROWING: a chime that rises, over a bed of fine crackle that thickens rather than
    fades. The inverse of `synth_frost_nova`, deliberately, because that one is ice breaking
    outward and these two must not be confusable.
    """
    n = int(duration * SAMPLE_RATE)
    out = [0.0] * n
    ticks = [(random.uniform(0.0, 0.85), random.uniform(3200.0, 9000.0)) for _ in range(90)]

    for i in range(n):
        t = i / SAMPLE_RATE

        # The chime climbs. Partials sharpen as the crystal sets.
        val = 0.0
        for idx, base in enumerate([620.0, 930.0, 1480.0, 2210.0]):
            f = base * (0.74 + 0.34 * (1.0 - math.exp(-t * 3.4)))
            val += math.sin(2.0 * math.pi * f * t) * envelope(t, 0.05, 1.9 + idx * 0.7) / (idx + 1.3)

        # Crackle that BUILDS, which is what says the patch is spreading.
        frost = 0.0
        for start, f in ticks:
            if start <= t < start + 0.045:
                a = 1.0 - (t - start) / 0.045
                frost += math.sin(2.0 * math.pi * f * (t - start)) * a * a
        frost *= 0.055 * min(1.0, t / 0.55)

        out[i] = math.tanh((val * 0.62 + frost) * 1.1)
    return out


def synth_barricade_raise(duration=1.0):
    """
    Cheska. Ice Barricade, three pillars coming up out of the road.

    NOT the sheet: this is heavy and it ARRIVES. A low grinding rise, then a hard stop when the
    pillars lock, because the wall is a solid object and the player needs to hear that it is
    now in the way.
    """
    n = int(duration * SAMPLE_RATE)
    out = [0.0] * n
    lock = 0.52

    for i in range(n):
        t = i / SAMPLE_RATE

        # The grind up. Frequency climbs while the wall is still moving, then holds.
        rise = min(t, lock) / lock
        grind = math.sin(2.0 * math.pi * (70.0 + 150.0 * rise) * t)
        grind *= (0.35 + 0.65 * rise) * math.exp(-max(0.0, t - lock) * 7.0)

        # Grit riding on the grind, gone the instant it locks.
        grit = (random.uniform(-1.0, 1.0) * 0.16
                * (1.0 - abs(rise - 0.6)) * (1.0 if t < lock else 0.0))

        # The lock itself: a short hard knock with a crystalline tail.
        knock = 0.0
        if t >= lock:
            k = t - lock
            knock = (math.sin(2.0 * math.pi * 190.0 * k) * math.exp(-k * 26.0) * 0.9
                     + math.sin(2.0 * math.pi * 1750.0 * k) * math.exp(-k * 9.0) * 0.25)

        out[i] = math.tanh((grind * 0.6 + grit + knock) * 1.15)
    return out


def synth_ice_shatter(duration=1.2):
    """
    Cheska. The barricade breaking, which used to play `slipper_land`.

    A single hard CRACK, then the wall coming down as pieces. The crack is most of it: a wall
    failing is one event followed by debris, not a sustained noise.
    """
    n = int(duration * SAMPLE_RATE)
    out = [0.0] * n

    # Debris starts after the crack, never with it.
    pieces = [(random.uniform(0.06, 0.9), random.uniform(1400.0, 6400.0)) for _ in range(120)]

    for i in range(n):
        t = i / SAMPLE_RATE

        crack = (math.sin(2.0 * math.pi * 240.0 * t) * math.exp(-t * 30.0)
                 + random.uniform(-1.0, 1.0) * math.exp(-t * 55.0) * 0.7)

        glass = 0.0
        for start, f in pieces:
            if start <= t < start + 0.08:
                a = 1.0 - (t - start) / 0.08
                glass += math.sin(2.0 * math.pi * f * (t - start)) * a * a
        glass *= 0.085

        out[i] = math.tanh((crack * 0.9 + glass) * 1.2)
    return out


def synth_ice_thaw(duration=1.3):
    """
    Cheska. The sheet running out.

    ⚠️ QUIET ON PURPOSE, AND THAT IS TRUE OF ALL THREE EXPIRY CUES. A zone ending is
    information, not an event: it must be audible to somebody who has been watching the patch
    and must not compete with whatever is being cast at that moment. Descending, so it cannot be
    mistaken for the rising cue that opened it.
    """
    n = int(duration * SAMPLE_RATE)
    out = [0.0] * n
    drips = [(random.uniform(0.15, 1.0), random.uniform(700.0, 2100.0)) for _ in range(14)]

    for i in range(n):
        t = i / SAMPLE_RATE

        val = 0.0
        for idx, base in enumerate([1450.0, 1020.0, 720.0]):
            f = base * (1.0 - 0.34 * (1.0 - math.exp(-t * 2.0)))
            val += math.sin(2.0 * math.pi * f * t) * math.exp(-t * (1.8 + idx * 0.6)) / (idx + 1.6)

        drip = 0.0
        for start, f in drips:
            if start <= t < start + 0.09:
                k = t - start
                drip += math.sin(2.0 * math.pi * f * (1.0 + k * 2.4) * k) * math.exp(-k * 26.0)
        drip *= 0.18

        out[i] = math.tanh((val * 0.30 + drip * 0.30) * 1.0)
    return out


def synth_void_close(duration=1.1):
    """
    Nemu. Seance Void collapsing.

    ⚠️ IT IS `sfx_possess_enter` RUN BACKWARDS IN SHAPE RATHER THAN IN SAMPLES. The vortex
    opens on a rising suck; it should close on a falling one that CUTS rather than fades, so the
    end of the zone has an edge a player can act on. A tail that trails off says the danger is
    lessening; a hole in the world is either there or it is not.
    """
    n = int(duration * SAMPLE_RATE)
    out = [0.0] * n
    cut = 0.86

    for i in range(n):
        t = i / SAMPLE_RATE

        if t >= cut:
            # Silence, hard. This is the read.
            out[i] = 0.0
            continue

        k = t / cut

        # A falling swept tone: the throat closing.
        f = 420.0 * math.exp(-k * 2.4) + 46.0
        body = math.sin(2.0 * math.pi * f * t)

        # Air being pulled after it, thinning as the mouth narrows.
        air = random.uniform(-1.0, 1.0) * 0.22 * (1.0 - k) * (1.0 - k)

        # The very last of it tightens up rather than fading.
        squeeze = math.sin(2.0 * math.pi * 1180.0 * t) * 0.14 * k * k

        out[i] = math.tanh((body * 0.55 + air + squeeze) * (0.35 + 0.65 * (1.0 - k)) * 1.2)
    return out


def synth_magma_cool(duration=1.4):
    """
    Dante. The cracked ground going cold.

    A sizzle that thins out, with the irregular TICK of contracting rock over it. The ticks are
    what make it read as stone rather than as a fade-out on the sizzle.
    """
    n = int(duration * SAMPLE_RATE)
    out = [0.0] * n
    ticks = [(random.uniform(0.1, 1.25), random.uniform(180.0, 620.0)) for _ in range(11)]

    for i in range(n):
        t = i / SAMPLE_RATE

        # Sizzle, band-limited by a slow wobble so it is not flat hiss.
        sizzle = (random.uniform(-1.0, 1.0)
                  * 0.30 * math.exp(-t * 1.9)
                  * (0.7 + 0.3 * math.sin(2.0 * math.pi * 7.0 * t)))

        tick = 0.0
        for start, f in ticks:
            if start <= t < start + 0.05:
                k = t - start
                tick += math.sin(2.0 * math.pi * f * k) * math.exp(-k * 40.0)
        tick *= 0.34

        out[i] = math.tanh((sizzle + tick) * 1.0)
    return out


# ⚠️⚠️ THE SEED SLOT IS WRITTEN DOWN PER CUE, AND IT USED TO BE THE POSITION IN A SORTED LIST.
# The old `main` seeded with `0x7A4A + index * 977` where `index` came from enumerating
# `sorted(GENERATORS.items())`, and the comment beside it claimed *"regenerating one cue cannot
# change another"*. That was true only while nothing was ever added. Adding the six lifecycle
# cues below inserted names ahead of five of the original seven alphabetically, every following
# index shifted, and one run silently rewrote all seven shipped sounds with different audio.
# `git status` is what caught it, not listening.
#
# ⚠️ THE SLOTS BELOW ARE THE ORIGINAL SEVEN'S OLD INDICES, so those files regenerate BYTE
# IDENTICAL and the diff shows only what actually changed. New cues take the next free number.
# NEVER renumber an existing row: the number IS the sound.
def synth_eclipse_toll(duration=2.2):
    """
    Phaister. Grand Coven Eclipse.

    ⚠️ IT REPLACES `sfx_ghost_appear`, WHICH DOES NOT EXIST AND NEVER DID. The sixth hero's
    ULTIMATE asked for a cue with no file behind it, so `AudioDirector` warned and returned and
    the biggest moment in her kit was silent apart from a borrowed grunt. `AudioCues` records
    the same failure costing `LrtTrainFlyby` two months.

    A BELL, and deliberately the only one in the game. Every other payload here is an impact,
    a whump or a hiss; an eclipse is announced rather than delivered, so this is a struck tone
    with a long inharmonic tail over a slow rising shimmer. Bell partials are NOT harmonic
    multiples, which is the whole difference between a bell and an organ: the ratios below are
    the classic minor-third strike tone and they are why it reads as ominous without any
    processing.
    """
    n = int(duration * SAMPLE_RATE)
    out = [0.0] * n

    fundamental = 116.0
    partials = [
        (0.5, 1.00, 1.1),    # hum, an octave below the strike
        (1.0, 0.85, 1.5),    # the strike note
        (1.2, 0.55, 2.1),    # minor third: the sound of a bell rather than a tone
        (1.5, 0.40, 2.6),
        (2.0, 0.30, 3.2),
        (2.7, 0.18, 4.4),
        (3.4, 0.12, 5.5),
    ]

    for i in range(n):
        t = i / SAMPLE_RATE

        bell = 0.0
        for ratio, gain, decay in partials:
            bell += math.sin(2.0 * math.pi * fundamental * ratio * t) * gain * math.exp(-t * decay)

        # The strike itself: a very short noise transient, or the bell starts as a pure tone
        # with no hammer in it.
        strike = random.uniform(-1.0, 1.0) * math.exp(-t * 90.0) * 0.35

        # The eclipse arriving underneath. It RISES while the bell decays, so the cue hands over
        # from one to the other rather than simply fading.
        shimmer = (math.sin(2.0 * math.pi * (420.0 + 260.0 * (1.0 - math.exp(-t * 1.1))) * t)
                   * 0.16 * (1.0 - math.exp(-t * 1.4)) * math.exp(-t * 0.9))

        out[i] = math.tanh((bell * 0.42 + strike + shimmer) * 1.05)
    return out


def synth_hex_cast(duration=1.0):
    """
    Phaister. A hex being SPOKEN, then the sigil catching.

    ⚠️ IT REPLACES `ability_shatter_trap` ON HER HEX. That cue is a trap breaking, it belongs to
    the deleted ability set, and it was already carrying Cheska's two ground powers until
    `docs/TODO.md` § 20 gave those their own. A third hero reaching for it would have made it
    three kits sharing one leftover, which is the fault this whole file was written to end.

    Two halves, in order, because a spell is spoken and THEN it takes: a low rasping swell with
    no clear pitch, and a bright rune chime that only arrives at the end.
    """
    n = int(duration * SAMPLE_RATE)
    out = [0.0] * n
    catch = 0.58

    for i in range(n):
        t = i / SAMPLE_RATE

        # The incantation: filtered noise pushed through a slow wobble, so it reads as a voice
        # without being one. No fundamental, because a pitch here would sound like a synth pad.
        k = min(1.0, t / catch)
        rasp = (random.uniform(-1.0, 1.0) * 0.30
                * (0.25 + 0.75 * k)
                * (0.6 + 0.4 * math.sin(2.0 * math.pi * 11.0 * t))
                * (1.0 if t < catch else math.exp(-(t - catch) * 12.0)))

        # A low drone under it, rising a little as the words land.
        drone = math.sin(2.0 * math.pi * (58.0 + 26.0 * k) * t) * 0.22 * k

        # The catch: the sigil taking, as a struck chime with an odd partial in it.
        chime = 0.0
        if t >= catch:
            c = t - catch
            for idx, f in enumerate([1180.0, 1770.0, 2360.0]):
                chime += (math.sin(2.0 * math.pi * f * c)
                          * math.exp(-c * (5.0 + idx * 3.0)) / (idx + 1.5))
            chime *= 0.5

        out[i] = math.tanh((rasp + drone + chime) * 1.15)
    return out


def synth_hex_afflict(duration=0.8):
    """
    Phaister. The moment the hex takes hold of somebody.

    ⚠️ IT IS THE VICTIM'S CUE AND IT IS DELIBERATELY UNPLEASANT. Every other on-hit sound in this
    game is an impact: something struck something. A curse is not struck, it SETTLES, so this
    falls rather than rises and has a sour detuned pair at the bottom of it that none of the
    other cues use. A player who has been hexed should be able to tell without reading the HUD.
    """
    n = int(duration * SAMPLE_RATE)
    out = [0.0] * n

    for i in range(n):
        t = i / SAMPLE_RATE

        # Two tones a hair apart, which is what makes it sour rather than musical.
        f = 330.0 * math.exp(-t * 1.6) + 110.0
        pair = (math.sin(2.0 * math.pi * f * t)
                + math.sin(2.0 * math.pi * f * 1.032 * t)) * 0.5

        # A short scrape at the front so it has an onset.
        scrape = random.uniform(-1.0, 1.0) * math.exp(-t * 24.0) * 0.28

        out[i] = math.tanh((pair * 0.55 * math.exp(-t * 2.2) + scrape) * 1.2)
    return out



def synth_lrt_rumble(duration=2.0):
    """
    The LRT consist, as a SEAMLESS LOOP, for the source that rides the train.

    ⚠️⚠️ IT EXISTS BECAUSE `sfx_lrt_pass` CANNOT BE LOOPED. That cue is a one-shot: 2.70 s,
    beginning and ending on a sample value of ZERO because it was authored with a fade in and a
    fade out. A pass lasts 5.33 s, so looping it dropped the train to silence at 2.70 s and
    swelled it back from nothing while it was directly overhead.

    ⚠️⚠️ AND THE FIRST VERSION OF THIS BED WAS WIND, NOT A TRAIN, WHICH IS A REAL REPORT
    OFF THE PLAYED BUILD: *"theres a loud wind soudn that plays randomly, i think its the train,
    i think ur train sfx is broken"*. It was built mostly out of broadband filtered noise at a
    gain of 1.9, and broadband filtered noise IS wind: that is the standard way to synthesise it.
    Nothing in it said "mechanical".

    ⚠️ SO THE BALANCE IS INVERTED. What makes a train a train is PERIODICITY, not roar: a low
    tonal floor from the traction motors and the regular knock of bogies over rail joints. The
    noise is now a thin bed UNDER those rather than the substance of the cue, and the knocks
    carry the character. Peak is normalised well down as well, because the old one clipped in at
    0.88 of full scale and was then mixed at 0 dB (see `AudioCues.TrimDb`).

    ⚠️ THE NOISE IS MADE LOOP-SAFE BY FILTERING CIRCULARLY, not by crossfading the ends. Three
    copies are concatenated, the filter is run across the lot, and the MIDDLE third is kept, so
    the filter state at the last sample is exactly what it was at the first.

    ⚠️ AND EVERY TONAL COMPONENT COMPLETES A WHOLE NUMBER OF CYCLES IN THE LOOP, so each one
    arrives back at its starting phase at the seam. A 47 Hz hum in a 2.0 s loop clicks every 2 s
    forever.
    """
    n = int(duration * SAMPLE_RATE)
    base = 1.0 / duration

    # --- circularly filtered noise, kept deliberately thin and dark.
    raw = [random.uniform(-1.0, 1.0) for _ in range(n)]
    tripled = raw + raw + raw

    lp = 0.0
    out_lp = [0.0] * len(tripled)
    for i, x in enumerate(tripled):
        # ⚠️ 0.012, DOWN FROM 0.045. A higher coefficient passes more of the mid band, which is
        # exactly the region that reads as air moving. This is a dull roll under the tone.
        lp += (x - lp) * 0.012
        out_lp[i] = lp

    lo = out_lp[n:2 * n]

    def bin_at(hz):
        return round(hz / base) * base

    # The traction floor. Low, tonal, and the thing the ear locks onto as machinery.
    hums = [(bin_at(29.0), 1.00), (bin_at(58.0), 0.46), (bin_at(87.0), 0.22),
            (bin_at(116.0), 0.11)]

    # ⚠️ EIGHT KNOCKS, NOT FOUR, AND IN UNEVEN PAIRS. Real stock runs two bogies per car, so
    # joints arrive as a "da-dum" rather than as a metronome. Evenly spaced knocks read as a
    # machine ticking; paired ones read as something long going past.
    knocks = []
    for i in range(4):
        t0 = i * (duration / 4.0)
        knocks.append(t0)
        knocks.append(t0 + 0.085)

    out = [0.0] * n
    peak = 0.0

    for i in range(n):
        t = i / SAMPLE_RATE

        tone = 0.0
        for f, a in hums:
            tone += math.sin(2.0 * math.pi * f * t) * a

        knock = 0.0
        for start in knocks:
            d = t - start
            if 0.0 <= d < 0.11:
                # Two partials so a joint is a THUD with a rim to it, not a sine blip.
                knock += (math.sin(2.0 * math.pi * 68.0 * d)
                          + math.sin(2.0 * math.pi * 154.0 * d) * 0.34) * math.exp(-d * 27.0)

        v = tone * 0.30 + lo[i] * 0.55 + knock * 0.42
        out[i] = v
        peak = max(peak, abs(v))

    # ⚠️ NORMALISED TO 0.55, NOT LEFT AT WHATEVER THE SUM CAME TO. The previous bed peaked at
    # 0.88 and was then mixed at 0 dB because nothing had given it a `TrimDb` row, so it arrived
    # in the game louder than every ability payload in it. Normalising here means the mix trim
    # is the only volume decision and it is made in one place.
    if peak > 0.0:
        scale = 0.55 / peak
        out = [v * scale for v in out]

    return out


def synth_blink_arrive(duration=0.55):
    """
    Phaister. The moment she is suddenly standing there.

    ⚠️⚠️ THE ARRIVAL WAS SILENT AND THAT IS A REAL GAP RATHER THAN A MISSING FLOURISH. The blink
    plays `sfx_ghost_teleport` at the DEPARTURE, which is where she left; after the 2026-08-26
    rebuild the destination can be 5.5 m away, so the players standing next to where she lands
    heard nothing at all. The bar for adding a sound in this game is *"a player having to guess
    whether something happened"*, and a witch materialising beside you in silence is the clearest
    case of it in the kit.

    ⚠️ IT IS THE OPPOSITE SHAPE TO THE DEPARTURE CUE, not a variation on it. A teleport OUT is a
    swallow: broadband, decaying. This is an intake run backwards into a hard stop, so the two
    ends of one ability are told apart by ear as well as by eye. The tail is the only part with a
    pitch in it, because the last thing to arrive is her.
    """
    n = int(duration * SAMPLE_RATE)
    out = [0.0] * n
    land = 0.34

    for i in range(n):
        t = i / SAMPLE_RATE

        if t < land:
            # The suck: noise gated by a curve that TIGHTENS, plus a rising whistle. Both stop
            # dead at `land` rather than decaying, which is what makes the stop read as an
            # arrival instead of a fade.
            k = t / land
            tighten = k * k * k
            rush = random.uniform(-1.0, 1.0) * 0.34 * tighten
            whistle = math.sin(2.0 * math.pi * (240.0 + 900.0 * k * k) * t) * 0.16 * tighten
            out[i] = math.tanh((rush + whistle) * 1.2)
            continue

        # The body landing: a short struck pair a fifth apart, gold rather than violet, which is
        # the colour the arrival glyphs are drawn in.
        c = t - land
        body = (math.sin(2.0 * math.pi * 392.0 * c) * math.exp(-c * 11.0) * 0.42
                + math.sin(2.0 * math.pi * 588.0 * c) * math.exp(-c * 16.0) * 0.24
                + random.uniform(-1.0, 1.0) * 0.10 * math.exp(-c * 34.0))
        out[i] = math.tanh(body * 1.1)

    return out


def synth_stun_break(duration=0.7):
    """
    Anybody. The element coming OFF a body that fought its way out of a hold.

    ⚠️⚠️ BREAKING FREE MADE NO SOUND, WHICH IS THE ONE PLACE `docs/TODO.md` § 23 LEFT THE PLAYER
    READING THE HUD. That entry built the whole mash-out system on the argument that a held player
    should be FIGHTING rather than waiting, gave it a pip card and an element coat, and then ended
    the fight silently: the pips fill, the coat goes, and the only confirmation that the last press
    was the one that worked is a picture you are not looking at, because you are looking at the
    three people running at you.

    ⚠️ ONE CUE FOR ALL SIX ELEMENTS ON PURPOSE. § 23's own rule is that the coat says WHAT IS ON
    YOU and the mash says you are fighting it; the BREAK is the same event whoever caused it, and
    six variants would be six ways to say one thing. It is also mixed low for the reason
    `sfx_hex_afflict` is: in a 1-vs-3 game three of these can land inside a second.

    A shell failing: a short crack, then pieces, then nothing. No pitch anywhere in it, because a
    pitched break would belong to whichever element happened to own that note.
    """
    n = int(duration * SAMPLE_RATE)
    out = [0.0] * n

    # ⚠️ THE PIECES ARE DISCRETE GRAINS, NOT A NOISE TAIL, which is the same distinction
    # `synth_quake_slam` draws about rubble: continuous noise is a hiss, and separated grains are
    # things falling. Seven, at falling density, so the last two are clearly individual.
    grains = []
    at = 0.06
    gap = 0.035
    for _ in range(7):
        grains.append((at, random.uniform(0.35, 0.9)))
        gap *= 1.34
        at += gap

    for i in range(n):
        t = i / SAMPLE_RATE

        # The crack itself: one very short broadband burst with a hard edge.
        crack = random.uniform(-1.0, 1.0) * 0.85 * math.exp(-t * 90.0)

        pieces = 0.0
        for start, weight in grains:
            if t < start:
                continue
            c = t - start
            if c > 0.09:
                continue
            pieces += random.uniform(-1.0, 1.0) * 0.22 * weight * math.exp(-c * 46.0)

        out[i] = math.tanh((crack + pieces) * 1.05)

    return out


def _rumble_tail(out, start, duration, cutoff_hz, level, seed_drift=0.0, decay=0.9):
    """
    A low bed made by integrating noise, which is what makes it a RUMBLE and not a hiss.

    ⚠️ ONE-POLE INTEGRATION RATHER THAN A FILTER SWEEP. Summing white noise with a leak is a
    brown-noise generator: energy falls at 6 dB per octave, so what survives is the bottom.
    A band-passed hiss sounds like wind through a gap; this sounds like mass moving.

    ⚠️⚠️ `decay` IS THE AFTERMATH AND IT IS WHY THIS GREW A PARAMETER. It was a hard-coded
    `exp(-c * 0.9)`, which is about a three-second bed: correct when a weather event lasted
    2.65 s and wrong the moment `Visual.SkyEvent` started holding the sky for seven to ten.
    🧑 2026-08-27: *"make the changes in lighting and color and the sfx to continue playing for
    some time after too"*. The six weather beds pass a much slower value; every other caller
    keeps 0.9 by omitting it, because a hazard's rumble should still be over in three seconds.
    """
    n = len(out)
    state = 0.0
    leak = math.exp(-2.0 * math.pi * cutoff_hz / SAMPLE_RATE)

    for i in range(n):
        t = i / SAMPLE_RATE
        if t < start:
            continue

        c = t - start
        if c > duration:
            continue

        state = state * leak + random.uniform(-1.0, 1.0) * (1.0 - leak)

        # A slow swell in and a long decay out, so the bed has a shape rather than a switch.
        k = min(1.0, c / 0.35) * math.exp(-c * decay)
        wobble = 1.0 + seed_drift * math.sin(2.0 * math.pi * 0.7 * c)
        out[i] += state * level * k * wobble * 14.0


def synth_sky_eclipse(duration=7.6):
    """
    Phaister. The sky being pulled over.

    ⚠️ IT IS THE ONLY ONE THAT DESCENDS IN PITCH, and that is the whole read. An eclipse is
    something arriving from above and closing, so a falling tone under a struck bell says it
    without any of the other five needing to avoid the same idea. `sfx_eclipse_toll` is her
    PAYLOAD and is a single bell; this is the world answering it, so it is longer, lower and
    has no clear strike in it.
    """
    n = int(duration * SAMPLE_RATE)
    out = [0.0] * n

    for i in range(n):
        t = i / SAMPLE_RATE

        # The descent: two detuned partials sliding down a fifth over the first second.
        slide = math.exp(-t * 0.85)
        f = 96.0 * (0.55 + 0.45 * slide)
        voice = (math.sin(2.0 * math.pi * f * t) * 0.30
                 + math.sin(2.0 * math.pi * f * 1.497 * t) * 0.14) * math.exp(-t * 0.7)

        # A sour upper pair that beats slowly against itself: the magic in the sky.
        shimmer = (math.sin(2.0 * math.pi * 611.0 * t)
                   + math.sin(2.0 * math.pi * 617.0 * t)) * 0.05 * math.exp(-t * 1.4)

        out[i] = voice + shimmer

    _rumble_tail(out, 0.05, duration - 0.05, 55.0, 0.30, seed_drift=0.10, decay=0.30)
    return [math.tanh(v * 1.1) for v in out]


def synth_sky_storm(duration=8.2):
    """
    Zack. Thunder, and then the roll.

    ⚠️⚠️ THE CRACK IS SHAPED, NOT JUST LOUD. Real thunder is a near-instant edge followed by a
    tail whose top end disappears first, because air absorbs high frequencies over distance.
    Two strikes a beat apart, the second further away and duller, is what separates thunder from
    a snare hit, and it is the only cue in this file with a deliberate second event in it.
    """
    n = int(duration * SAMPLE_RATE)
    out = [0.0] * n

    # (when, level, how fast the top end goes)
    strikes = [(0.02, 1.0, 26.0), (0.34, 0.55, 44.0)]

    for when, level, damp in strikes:
        smooth = 0.0
        for i in range(n):
            t = i / SAMPLE_RATE
            if t < when:
                continue

            c = t - when

            # A one-pole low pass whose cutoff FALLS with time: the strike starts bright and
            # the distance eats the top of it.
            cutoff = 5200.0 * math.exp(-c * damp) + 90.0
            leak = math.exp(-2.0 * math.pi * cutoff / SAMPLE_RATE)
            smooth = smooth * leak + random.uniform(-1.0, 1.0) * (1.0 - leak)

            out[i] += smooth * level * math.exp(-c * 3.2) * 2.4

    _rumble_tail(out, 0.30, duration - 0.30, 70.0, 0.34, seed_drift=0.22, decay=0.26)
    return [math.tanh(v * 1.05) for v in out]


def synth_sky_whiteout(duration=7.4):
    """
    Cheska. A squall arriving.

    ⚠️ IT IS THE ONLY ONE WITH NO LOW END TO SPEAK OF. Every other weather here is mass; a
    whiteout is AIR, and giving it a rumble would put it in the same band as the storm and the
    dust. What carries it is a rising band of noise plus a thin whistle that arrives late, which
    is the sound of something being blown past an edge.
    """
    n = int(duration * SAMPLE_RATE)
    out = [0.0] * n
    smooth = 0.0

    for i in range(n):
        t = i / SAMPLE_RATE
        k = min(1.0, t / 0.9)

        # A band that OPENS as it arrives: cutoff climbs, so the squall gets brighter and
        # closer rather than simply louder.
        cutoff = 320.0 + 2600.0 * k
        leak = math.exp(-2.0 * math.pi * cutoff / SAMPLE_RATE)
        smooth = smooth * leak + random.uniform(-1.0, 1.0) * (1.0 - leak)

        # ⚠️ THE TWO DECAYS HERE ARE THIS CUE'S TAIL, BECAUSE IT HAS NO RUMBLE BED TO SLOW.
        # The other five weather cues get their aftermath from `_rumble_tail(..., decay=)`; a
        # whiteout is air rather than mass and deliberately has no low end to stretch (see the
        # docstring), so the squall body and the whistle carry it themselves. 1.5 and 1.1 were
        # right for a 2.6 s cue and left four seconds of silence at 7.4.
        body = smooth * 2.2 * k * math.exp(-max(0.0, t - 1.1) * 0.34)

        # The whistle, late and detuned, so it reads as a gap in something rather than a tone.
        whistle = 0.0
        if t > 0.55:
            c = t - 0.55
            whistle = (math.sin(2.0 * math.pi * (1180.0 + 90.0 * math.sin(2.0 * math.pi * 1.7 * c)) * c)
                       * 0.10 * math.exp(-c * 0.42))

        out[i] = math.tanh((body + whistle) * 1.0)

    return out


def synth_sky_emberfall(duration=7.6):
    """
    Sean. A firestorm drawing breath.

    ⚠️ A ROAR IS AMPLITUDE-MODULATED NOISE, NOT FILTERED NOISE. Fire is irregular at a few
    hertz: the modulation is what makes it read as combustion rather than as wind, and it is the
    one thing `synth_sky_whiteout` above must not have. Two modulators at unrelated rates so the
    pattern never repeats inside the cue.
    """
    n = int(duration * SAMPLE_RATE)
    out = [0.0] * n
    smooth = 0.0

    for i in range(n):
        t = i / SAMPLE_RATE

        cutoff = 1500.0 - 900.0 * min(1.0, t / 1.6)
        leak = math.exp(-2.0 * math.pi * cutoff / SAMPLE_RATE)
        smooth = smooth * leak + random.uniform(-1.0, 1.0) * (1.0 - leak)

        gust = (0.62
                + 0.26 * math.sin(2.0 * math.pi * 3.1 * t)
                + 0.12 * math.sin(2.0 * math.pi * 7.3 * t + 1.1))

        swell = min(1.0, t / 0.45) * math.exp(-max(0.0, t - 0.9) * 1.2)
        out[i] = smooth * gust * swell * 2.6

    _rumble_tail(out, 0.10, duration - 0.10, 62.0, 0.24, seed_drift=0.16, decay=0.30)
    return [math.tanh(v * 1.08) for v in out]


def synth_sky_dustveil(duration=8.4):
    """
    Dante. The street coming up off the ground.

    ⚠️ THE GRAINS ARE THE POINT AND THEY ARE DISCRETE. `synth_quake_slam` draws the same
    distinction about rubble: continuous noise is a hiss and separated grains are things
    falling. This is the lowest and longest of the six because it is the only one whose subject
    is MASS rather than air or light, and it is deliberately the dullest: nothing about a dust
    cloud sparkles.
    """
    n = int(duration * SAMPLE_RATE)
    out = [0.0] * n

    # A shove at the front, then debris at falling density for a second and a half.
    grains = []
    at = 0.05
    gap = 0.012
    while at < 1.7:
        grains.append((at, random.uniform(0.25, 1.0)))
        gap *= 1.06
        at += gap

    for i in range(n):
        t = i / SAMPLE_RATE
        v = 0.0

        for start, weight in grains:
            if t < start:
                continue
            c = t - start
            if c > 0.05:
                continue
            v += random.uniform(-1.0, 1.0) * 0.16 * weight * math.exp(-c * 70.0)

        out[i] = v

    _rumble_tail(out, 0.0, duration, 42.0, 0.46, seed_drift=0.28, decay=0.24)
    return [math.tanh(v * 1.05) for v in out]


def synth_sky_seance(duration=7.6):
    """
    Nemu. The one that is not weather.

    ⚠️⚠️ IT SWELLS BACKWARDS, WHICH IS THE ONLY WAY TO MAKE A SOUND THAT IS WRONG RATHER THAN
    LOUD. Her whole character is that things stop being right rather than that something
    arrives (`docs/TODO.md` section 27.5), so this is built and then its ENVELOPE is reversed:
    the tail comes first and the strike is at the end, which no physical event does. It is also
    the quietest of the six, because a seance that announced itself would be somebody else's
    ultimate.
    """
    n = int(duration * SAMPLE_RATE)
    body = [0.0] * n

    for i in range(n):
        t = i / SAMPLE_RATE

        # Two voices a semitone apart, which is the interval that refuses to resolve.
        pair = (math.sin(2.0 * math.pi * 174.0 * t)
                + math.sin(2.0 * math.pi * 184.4 * t)) * 0.22

        # A breath under them, filtered so it has no edge at all.
        breath = random.uniform(-1.0, 1.0) * 0.08

        body[i] = pair + breath

    # ⚠️ THE ENVELOPE IS APPLIED IN REVERSE, not the samples. Reversing the audio would reverse
    # the noise too, which sounds identical; reversing only the shape is what produces the
    # backwards-swell without touching the timbre.
    # ⚠️⚠️ THE RELEASE AT THE VERY END IS NOT A TASTE CHANGE, IT IS THE PRICE OF THE LENGTH.
    # This envelope peaks AT `duration` by construction, so the cue has always ended at full
    # amplitude and `tools/audit_cue_audio.py` has always reported a seam near 0.65 for it. At
    # 2.7 s that was a deliberate stab; at 7.6 s it is a seven-second crescendo terminated by a
    # click, which is the one artefact `envelope`'s own note says nothing in this file may have.
    # 0.22 s is long enough to remove the edge and far too short to soften the arrival.
    release = 0.22

    out = [0.0] * n
    for i in range(n):
        t = i / SAMPLE_RATE
        back = t / duration
        shape = (back * back) * (0.35 + 0.65 * min(1.0, t / 0.4))

        left = duration - t
        if left < release:
            shape *= left / release

        out[i] = math.tanh(body[i] * shape * 2.3)

    _rumble_tail(out, 0.0, duration, 48.0, 0.16, seed_drift=0.34, decay=0.28)
    return [math.tanh(v) for v in out]


def synth_kuro_unbound(duration=2.2):
    """
    Nemu. A pet becoming a mouth.

    ⚠️⚠️ IT IS AN INHALE, WHICH IS THE ONE ENVELOPE NOTHING ELSE IN THIS FILE USES. Every other
    payload here is a strike: an edge and a decay. A thing that SUCKS has the opposite shape, so
    this swells from nothing into its loudest moment and stops there, and the stop is what tells
    a player the maw is now open rather than still arriving.

    ⚠️ THE PITCH RISES AS IT SWELLS, which is the cheapest way to say "drawing in" without a
    doppler. Under it, a detuned pair a semitone apart: the same interval `synth_sky_seance` uses,
    because these two are the same character and should share an interval the way she and
    Phaister must not share a colour.
    """
    n = int(duration * SAMPLE_RATE)
    out = [0.0] * n
    smooth = 0.0

    for i in range(n):
        t = i / SAMPLE_RATE
        k = min(1.0, t / (duration * 0.8))

        # An intake: noise through a band that OPENS, gated by a curve that accelerates.
        cutoff = 240.0 + 1900.0 * k * k
        leak = math.exp(-2.0 * math.pi * cutoff / SAMPLE_RATE)
        smooth = smooth * leak + random.uniform(-1.0, 1.0) * (1.0 - leak)

        draw = smooth * 2.3 * (k * k)

        # The pair underneath, rising a minor third across the whole cue.
        f = 82.0 * (1.0 + 0.19 * k)
        pair = (math.sin(2.0 * math.pi * f * t)
                + math.sin(2.0 * math.pi * f * 1.059 * t)) * 0.20 * k

        out[i] = math.tanh((draw + pair) * 1.05)

    return out


def synth_kuro_return(duration=0.8):
    """
    Nemu. The pet coming home.

    ⚠️ IT IS SHORT, LIGHT AND THE ONLY CUE IN HER SET WITH A SMILE IN IT. Everything else she has
    is a hole, a possession or a maw; Kuro flying back to her shoulder is the one moment in her
    kit that is not sinister, and giving it another dark swell would make the whole character one
    note. A rising two-tone chirp with a soft landing, mixed low: it is punctuation on an
    animation the player is already watching.
    """
    n = int(duration * SAMPLE_RATE)
    out = [0.0] * n
    land = 0.52

    for i in range(n):
        t = i / SAMPLE_RATE

        if t < land:
            # The flight: a tone bending up, thin, with a little air behind it.
            k = t / land
            f = 430.0 + 320.0 * k * k
            body = math.sin(2.0 * math.pi * f * t) * 0.26 * (0.35 + 0.65 * k)
            air = random.uniform(-1.0, 1.0) * 0.05 * k
            out[i] = math.tanh((body + air) * 1.1)
            continue

        # The landing: a soft struck pair, gone quickly.
        c = t - land
        out[i] = math.tanh((math.sin(2.0 * math.pi * 720.0 * c) * math.exp(-c * 15.0) * 0.34
                            + math.sin(2.0 * math.pi * 1080.0 * c) * math.exp(-c * 22.0) * 0.16)
                           * 1.05)

    return out


GENERATORS = {
    # (seed slot, synth). Slots 0 to 6 are the original payload set, in the alphabetical order
    # that produced the audio currently in the repository.
    "sfx_frost_nova.wav": (0, synth_frost_nova),
    "sfx_lrt_pass.wav": (1, synth_lrt_pass),
    "sfx_possess_enter.wav": (2, synth_possess_enter),
    "sfx_possess_exit.wav": (3, synth_possess_exit),
    "sfx_quake_slam.wav": (4, synth_quake_slam),
    "sfx_slipper_burst.wav": (5, synth_slipper_burst),
    "sfx_thunder_impact.wav": (6, synth_thunder_impact),

    # The zone lifecycle set. See the block above these synths for what was wrong.
    "sfx_ice_form.wav": (7, synth_ice_form),
    "sfx_barricade_raise.wav": (8, synth_barricade_raise),
    "sfx_ice_shatter.wav": (9, synth_ice_shatter),
    "sfx_ice_thaw.wav": (10, synth_ice_thaw),
    "sfx_void_close.wav": (11, synth_void_close),
    "sfx_magma_cool.wav": (12, synth_magma_cool),

    # Phaister, the sixth hero, whose ultimate asked for a file that was never made.
    "sfx_eclipse_toll.wav": (13, synth_eclipse_toll),
    "sfx_hex_cast.wav": (14, synth_hex_cast),
    "sfx_hex_afflict.wav": (15, synth_hex_afflict),

    # The looping bed the LRT consist carries. See the synth for why `sfx_lrt_pass` could not
    # simply be looped: it is a one-shot that begins and ends on silence.
    "sfx_lrt_rumble.wav": (16, synth_lrt_rumble),

    # The 2026-08-26 sparse pass. Two sounds, both for events a player could not otherwise be
    # sure had happened: the far end of a 5.5 m teleport, and the last press of a mash landing.
    "sfx_blink_arrive.wav": (17, synth_blink_arrive),
    "sfx_stun_break.wav": (18, synth_stun_break),

    # ⚠️ ONE PER WEATHER, WHICH IS ONE PER ULTIMATE. Asked for directly: *"add thunder shit and
    # under sfx when they ult and the sky changes to their theme"*, then *"add personalized sfx
    # to all ULTs"*. `Visual.SkyEvent` plays these; the kits keep their own payload cues, and
    # these sit under them.
    "sfx_sky_eclipse.wav": (19, synth_sky_eclipse),
    "sfx_sky_storm.wav": (20, synth_sky_storm),
    "sfx_sky_whiteout.wav": (21, synth_sky_whiteout),
    "sfx_sky_emberfall.wav": (22, synth_sky_emberfall),
    "sfx_sky_dustveil.wav": (23, synth_sky_dustveil),
    "sfx_sky_seance.wav": (24, synth_sky_seance),

    # Nemu's kit moving onto her pet. `docs/TODO.md` § 28.
    "sfx_kuro_unbound.wav": (25, synth_kuro_unbound),
    "sfx_kuro_return.wav": (26, synth_kuro_return),
}


def main():
    for filename, (slot, fn) in sorted(GENERATORS.items()):
        # ⚠️ SEEDED FROM THE CUE'S OWN WRITTEN-DOWN SLOT, never from its position in this table,
        # so regenerating one cue cannot change another and a rerun with no source edit produces
        # no diff at all. The table's note has what the position-based version cost.
        random.seed(0x7A4A + slot * 977)
        samples = fn()

        for d in OUT_DIRS:
            os.makedirs(d, exist_ok=True)
            path = os.path.join(d, filename)
            write_wav(path, samples)
            print(f"wrote {path} ({len(samples)} samples)")


if __name__ == "__main__":
    main()
