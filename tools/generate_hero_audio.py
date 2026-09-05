"""
The hero CAST and ULTIMATE voices, as opposed to the payload sounds
`generate_ability_audio.py` makes.

WHY THIS FILE IS BEING WRITTEN RATHER THAN EDITED:
It was never in the repository. `generate_ability_audio.py` line 35 does
`from generate_hero_audio import SAMPLE_RATE, write_wav`, and `git log --all -- tools/
generate_hero_audio.py` returns nothing on any branch, so the payload generator could not be
run from a fresh clone at all: the import failed before it reached a single synth. The
seventeen hero voice files in `Art/audio/sfx` were produced by a copy that only ever existed
on somebody's machine. `docs/TODO.md` section 21.4 records the consequence, that Phaister
"borrows `hero_nemu_grunt` for two casts" and that fixing it "needs its own pass", because the
missing script was believed to be present and unseeded.

WHAT THE OLD ONE DID WRONG, AND WHY THIS ONE CANNOT:
The note in `generate_ability_audio.py` records it: the original called `random.uniform` with
no seed, so every run rewrote all seventeen .wav files with different audio, and adding one cue
meant re-committing the whole set with the one intended change buried in it. Two rules here:

  1. EVERY CUE IS SEEDED FROM A WRITTEN-DOWN SLOT, never from its position in the table.
     `generate_ability_audio.py` carries the same rule and the same warning: renumbering a row
     silently rewrites a shipped sound. Add new cues with new slot numbers at the end. Never
     renumber one.
  2. AN EXISTING FILE IS NOT OVERWRITTEN UNLESS ASKED. A plain run writes only what is
     missing, so recovering this script cannot disturb audio that has already shipped and been
     listened to. `--force` rewrites everything, `--only <name>` rewrites one.

Run:  python tools/generate_hero_audio.py
      python tools/generate_hero_audio.py --only hero_phaister_ult
      python tools/generate_hero_audio.py --force
"""

import argparse
import math
import os
import random
import struct

SAMPLE_RATE = 44100

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)

# WARNING: THIS SAID "THE TWO PLACES THE GAME READS SFX FROM" AND ONE OF THEM WAS NEVER ONE.
# `Resources.Load` can only reach a folder called `Resources`, and `Art/audio/sfx` is not under
# one, so **the game has never loaded a single file from it**. `AudioCueCheck` and
# `audit_cue_audio.py` were both grading that folder until 2026-09-04 and were therefore grading
# files no player can hear; both were moved and this comment was not. `docs/TODO.md` section 144.3.
#
# WARNING: AND THE MIRROR HAS DRIFTED, WHICH IS THE MEASUREMENT THAT SETTLES WHAT IT IS.
# Compared 2026-09-05: 117 files each side, none missing from either, and **61 of the 117 differ
# byte for byte**. `tools/build_ability_audio.py` writes only `Resources/Sfx` (`SFX_DIR`), so the
# 2026-09-03 sourced pass replaced those 61 cues in the folder that ships and left this one
# holding the synthesised originals. So it is neither a master (nothing authors into it that does
# not also write `Resources/Sfx`) nor a copy (it disagrees on more than half its contents).
#
# WARNING: SO IT IS NOT FED ANY MORE, AND IT IS NOT DELETED EITHER. Writing to it produced a
# folder that looks authoritative and is not, which is the state that cost this entry its life;
# deleting 117 files the week of the nationals is a separate decision and `CLAUDE.md` section 6
# is why it is 🧑's: **sourced SFX are provisional until he hears them in play**, twenty-four are
# still awaiting exactly that judgement (`Attention.md` section 13), and this folder is the
# convenient A/B against them. The canonical restore point is git at `ee8bced^`, which
# `CLAUDE.md` section 6 already names.
OUT_DIRS = [
    os.path.join(ROOT, "Assets", "TumbangPreso", "Resources", "Sfx"),
]


def write_wav(path, samples, sample_rate=SAMPLE_RATE):
    """
    16-bit mono PCM, which is what every existing cue in this project already is.

    Imported by `generate_ability_audio.py`. Do not change the signature without changing that
    file: it is the only other caller and it passes positionally.
    """
    frames = bytearray()
    for s in samples:
        v = int(max(-1.0, min(1.0, s)) * 32767.0)
        frames += struct.pack("<h", v)

    with open(path, "wb") as f:
        f.write(b"RIFF")
        f.write(struct.pack("<I", 36 + len(frames)))
        f.write(b"WAVEfmt ")
        f.write(struct.pack("<IHHIIHH", 16, 1, 1, sample_rate, sample_rate * 2, 2, 16))
        f.write(b"data")
        f.write(struct.pack("<I", len(frames)))
        f.write(frames)


# ---------------------------------------------------------------------------- the voice model
#
# ⚠️⚠️ THESE ARE VOICES AND EVERY OTHER SOUND IN THIS PROJECT IS NOT, WHICH IS THE WHOLE REASON
# THEY NEED THEIR OWN SYNTH. 🧑 2026-08-26, asking for this pass: *"generate new vfx for the
# ultimates, maybe screams or laughter or something ominously sounding that is in chracter"*.
# A payload is noise shaped by an envelope, and `generate_ability_audio.py` builds all fifteen
# of its cues that way. Running a shout through the same machinery produces a whoosh with a
# pitch, which is what a filtered noise burst always sounds like.
#
# ⚠️ SO: A GLOTTAL PULSE TRAIN THROUGH THREE FORMANTS, which is the cheapest thing that reads as
# a human throat. The source is a periodic pulse at the speaker's pitch, and the formants are
# two-pole resonators at the frequencies that make a vowel a vowel. Sweeping the pitch gives the
# shout its shape; sweeping the FORMANTS is what turns "aaa" into "aaaugh" and is most of why
# these read as words rather than as tones.
#
# ⚠️ BREATH IS MIXED IN, NOT LAYERED OVER. A pure pulse train is a kazoo. Real shouting is part
# turbulence, and the ratio is the single biggest lever on whether a line reads as a roar (low
# pitch, heavy breath) or a scream (high pitch, light breath).


class Formant:
    """One two-pole resonator. Cheap, stable, and enough to place a vowel."""

    def __init__(self):
        self.y1 = 0.0
        self.y2 = 0.0

    def step(self, x, freq, bandwidth):
        r = math.exp(-math.pi * bandwidth / SAMPLE_RATE)
        theta = 2.0 * math.pi * freq / SAMPLE_RATE
        a1 = 2.0 * r * math.cos(theta)
        a2 = -r * r
        y = x + a1 * self.y1 + a2 * self.y2
        self.y2 = self.y1
        self.y1 = y
        return y * (1.0 - r)


def voice(duration, pitch_at, vowel_at, breath=0.25, drive=1.3, amp_at=None):
    """
    Render a vocalisation.

    `pitch_at(t01)`  -> fundamental in Hz at that point through the line.
    `vowel_at(t01)`  -> (f1, f2, f3) formant frequencies, which is the vowel being sung.
    `amp_at(t01)`    -> loudness envelope, defaulting to a shout's fast-in slow-out.
    """
    n = int(duration * SAMPLE_RATE)
    out = [0.0] * n

    f1, f2, f3 = Formant(), Formant(), Formant()
    phase = 0.0

    for i in range(n):
        t01 = i / n
        pitch = pitch_at(t01)

        # The glottal source. `phase` wraps once per period and each wrap emits a pulse; the
        # residual carries the fractional overshoot so the pulse train does not quantise to the
        # sample grid and buzz.
        phase += pitch / SAMPLE_RATE
        pulse = 0.0
        if phase >= 1.0:
            phase -= 1.0
            pulse = 1.0

        # A little jitter on every pulse. A perfectly periodic source is a synthesiser; a throat
        # is never exactly on pitch, and this is most of the difference.
        src = pulse * random.uniform(0.75, 1.0)
        src += random.uniform(-1.0, 1.0) * breath

        a, b, c = vowel_at(t01)
        shaped = (f1.step(src, a, 90.0) * 1.0
                  + f2.step(src, b, 110.0) * 0.7
                  + f3.step(src, c, 160.0) * 0.35)

        env = amp_at(t01) if amp_at else min(1.0, t01 / 0.06) * math.exp(-t01 * 2.4)
        out[i] = math.tanh(shaped * drive) * env

    return out


def lerp(a, b, t):
    return a + (b - a) * t


# ------------------------------------------------------------------------------- the ultimates
#
# ⚠️ ONE VOWEL SHAPE PER HERO, AND THEY ARE DELIBERATELY DIFFERENT VOWELS. Six ultimates that
# all shout "aah" at different pitches are the same repetition problem `docs/TODO.md` section 19
# spent a pass removing from the way the powers are DRAWN. The channel here is the vowel and its
# movement, not the pitch: Dante lands on a closed "oh", Zack is a clipped "ey", Nemu never
# closes at all.


def ult_dante():
    """
    Dante. A titan's roar: the lowest voice in the game, dropping further as it goes.

    Open "aa" collapsing to a closed "oh", which is a mouth closing round the end of a shout
    rather than a fade. Heavy breath, because a roar is more turbulence than tone.
    """
    return voice(
        1.45,
        pitch_at=lambda t: lerp(128.0, 74.0, t ** 0.7),
        vowel_at=lambda t: (lerp(720.0, 480.0, t), lerp(1180.0, 820.0, t), 2500.0),
        breath=0.40,
        drive=1.9,
        amp_at=lambda t: min(1.0, t / 0.05) * math.exp(-t * 1.7),
    )


def ult_cheska():
    """
    Cheska. A sharp indrawn shout, then a cold snap.

    ⚠️ IT RISES WHERE EVERY OTHER ULTIMATE HERE FALLS. Her kit freezes things in place, and a
    line that climbs and then stops dead is the audible version of that. The tail is cut short
    rather than decayed, which no other voice in the set does.
    """
    return voice(
        1.05,
        pitch_at=lambda t: lerp(280.0, 430.0, t ** 1.4),
        vowel_at=lambda t: (lerp(420.0, 320.0, t), lerp(2100.0, 2600.0, t), 3100.0),
        breath=0.18,
        drive=1.5,
        amp_at=lambda t: min(1.0, t / 0.04) * (1.0 if t < 0.72 else max(0.0, 1.0 - (t - 0.72) / 0.10)),
    )


def ult_sean():
    """
    Sean. A furious open shout, fire behind it. The most conventional of the six on purpose:
    something in a roster of six has to be the straight one, or the odd ones stop reading as odd.
    """
    return voice(
        1.25,
        pitch_at=lambda t: lerp(210.0, 165.0, t ** 0.8),
        vowel_at=lambda t: (lerp(780.0, 700.0, t), lerp(1300.0, 1150.0, t), 2650.0),
        breath=0.33,
        drive=1.8,
        amp_at=lambda t: min(1.0, t / 0.03) * math.exp(-t * 2.0),
    )


def ult_zack():
    """
    Zack. A clipped whoop, twice, fast. He is the speed hero and his ultimate is a sprint; a
    long held note would contradict the thing it is announcing.

    ⚠️ THE DOUBLE IS IN THE AMPLITUDE, NOT IN TWO RENDERS. Gating one continuous voice is what
    keeps the two halves obviously the same throat.
    """
    def amp(t):
        if t < 0.42:
            return min(1.0, t / 0.02) * math.exp(-t * 3.2)
        if t < 0.52:
            return 0.0
        return min(1.0, (t - 0.52) / 0.02) * math.exp(-(t - 0.52) * 4.0)

    return voice(
        0.95,
        pitch_at=lambda t: lerp(300.0, 380.0, (t % 0.5) * 2.0),
        vowel_at=lambda t: (520.0, lerp(1900.0, 2300.0, t), 2900.0),
        breath=0.20,
        drive=1.6,
        amp_at=amp,
    )


def ult_nemu():
    """
    Nemu. A wail that never lands on a vowel and never closes.

    ⚠️ THE PITCH WAVERS INSTEAD OF TRAVELLING. Every other line here goes somewhere; a ghost is
    the one that does not, and the slow vibrato with no destination is what makes it unsettling
    rather than merely high.
    """
    return voice(
        1.70,
        pitch_at=lambda t: 330.0 + math.sin(t * 26.0) * 38.0 - t * 40.0,
        vowel_at=lambda t: (lerp(500.0, 620.0, math.sin(t * 9.0) * 0.5 + 0.5),
                            lerp(1500.0, 1750.0, math.sin(t * 7.0) * 0.5 + 0.5),
                            2800.0),
        breath=0.30,
        drive=1.2,
        amp_at=lambda t: min(1.0, t / 0.18) * math.exp(-t * 1.1),
    )


def ult_phaister():
    """
    Phaister. A laugh, and she is the only one in the game who gets one.

    ⚠️⚠️ THIS IS THE CUE THE WHOLE PASS WAS ASKED FOR. 🧑: *"maybe screams or laughter or
    something ominously sounding that is in chracter"*. Five of the six ultimates are efforts,
    which is what a shout is: a body doing something hard. A witch calling an eclipse is not
    exerting herself, she is enjoying it, and a laugh is the one vocalisation that says the
    caster is in no danger. It is also the clearest way for the sixth hero to not sound like the
    five she was merged in beside.
    """
    def amp(t):
        # Five syllables, decreasing in length: "ha ha ha ha-ha". The gate is what makes it a
        # laugh rather than a held note, and the shortening is what makes it a real one.
        syllables = [(0.00, 0.13), (0.17, 0.13), (0.34, 0.11), (0.49, 0.09), (0.62, 0.16)]
        for start, length in syllables:
            if start <= t < start + length:
                k = (t - start) / length
                return min(1.0, k / 0.12) * math.exp(-k * 2.6)
        return 0.0

    return voice(
        1.55,
        # Descending across the laugh, which is what makes it read as mocking rather than merry.
        pitch_at=lambda t: lerp(340.0, 235.0, t) + math.sin(t * 60.0) * 12.0,
        vowel_at=lambda t: (lerp(700.0, 640.0, t), lerp(1250.0, 1100.0, t), 2600.0),
        breath=0.26,
        drive=1.5,
        amp_at=amp,
    )


def grunt_phaister():
    """
    Phaister's cast voice, which is the cue `docs/TODO.md` section 21.4 leaves open: she was
    playing `hero_nemu_grunt`, and Nemu is the one other spirit hero, so the borrow put the two
    characters most at risk of blurring together on the same throat.

    ⚠️ A HISSED INCANTATION, NOT AN EFFORT GRUNT. Every other cast voice in the set is a short
    exhale, because those five heroes are throwing their bodies at something. She is casting,
    so hers is breath-heavy and almost pitchless, and short enough to fire on a cooldown.
    """
    return voice(
        0.55,
        pitch_at=lambda t: lerp(215.0, 180.0, t),
        vowel_at=lambda t: (lerp(430.0, 380.0, t), lerp(1750.0, 1500.0, t), 2700.0),
        breath=0.62,
        drive=1.3,
        amp_at=lambda t: min(1.0, t / 0.07) * math.exp(-t * 3.4),
    )


GENERATORS = {
    # (seed slot, synth).
    #
    # ⚠️⚠️ NEVER RENUMBER A ROW. The slot is the seed, so changing one rewrites that cue's audio
    # even though nothing about its synth changed. `generate_ability_audio.py` carries the same
    # warning and the incident that produced it: seeding from a position in a sorted list meant
    # adding one cue rewrote all seven shipped sounds, and `git status` caught it rather than
    # anybody hearing it.
    #
    # Slots 0 to 5: the six ultimate voices. Rewritten 2026-08-26 on request, from a set that
    # predates this script.
    "hero_dante_ult.wav": (0, ult_dante),
    "hero_cheska_ult.wav": (1, ult_cheska),
    "hero_sean_ult.wav": (2, ult_sean),
    "hero_zack_ult.wav": (3, ult_zack),
    "hero_nemu_ult.wav": (4, ult_nemu),
    "hero_phaister_ult.wav": (5, ult_phaister),

    # Slot 6: the sixth hero's cast voice, which never existed.
    "hero_phaister_grunt.wav": (6, grunt_phaister),
}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--force", action="store_true",
                        help="rewrite cues that already exist on disk")
    parser.add_argument("--only", default=None,
                        help="rewrite exactly one cue, with or without the .wav suffix")
    args = parser.parse_args()

    only = args.only
    if only and not only.endswith(".wav"):
        only += ".wav"

    for filename, (slot, fn) in sorted(GENERATORS.items()):
        if only and filename != only:
            continue

        targets = [os.path.join(d, filename) for d in OUT_DIRS]

        # ⚠️ THE SKIP IS THE SAFETY RAIL, AND IT IS WHY RECOVERING THIS SCRIPT IS NOT A RISK.
        # A plain run cannot touch a sound that already shipped; see the header.
        if not args.force and not only and all(os.path.exists(p) for p in targets):
            print(f"skip  {filename} (exists; --force to rewrite)")
            continue

        # ⚠️ SEEDED FROM THE CUE'S OWN WRITTEN-DOWN SLOT, never from its position in this table,
        # so regenerating one cue cannot change another and a rerun with no source edit produces
        # no diff at all.
        random.seed(0x51C0 + slot * 977)
        samples = fn()

        for path in targets:
            os.makedirs(os.path.dirname(path), exist_ok=True)
            write_wav(path, samples)
            print(f"wrote {path} ({len(samples)} samples)")


if __name__ == "__main__":
    main()
