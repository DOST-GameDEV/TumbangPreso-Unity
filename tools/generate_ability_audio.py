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
