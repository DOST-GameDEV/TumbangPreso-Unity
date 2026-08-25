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


GENERATORS = {
    "sfx_quake_slam.wav": synth_quake_slam,
    "sfx_thunder_impact.wav": synth_thunder_impact,
    "sfx_frost_nova.wav": synth_frost_nova,
    "sfx_possess_enter.wav": synth_possess_enter,
    "sfx_possess_exit.wav": synth_possess_exit,
    "sfx_slipper_burst.wav": synth_slipper_burst,
}


def main():
    for index, (filename, fn) in enumerate(sorted(GENERATORS.items())):
        # ⚠️ SEEDED PER FILE, so regenerating one cue cannot change another and a rerun with no
        # source edit produces no diff at all.
        random.seed(0x7A4A + index * 977)
        samples = fn()

        for d in OUT_DIRS:
            os.makedirs(d, exist_ok=True)
            path = os.path.join(d, filename)
            write_wav(path, samples)
            print(f"wrote {path} ({len(samples)} samples)")


if __name__ == "__main__":
    main()
