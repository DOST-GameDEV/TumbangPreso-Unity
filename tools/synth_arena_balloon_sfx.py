"""The Arena's slipper balloon and the slipper's return to the stage, by deterministic synthesis.

  py -3 tools/synth_arena_balloon_sfx.py
  py -3 tools/synth_arena_balloon_sfx.py --only squeak        (any substring of the file names)

Owner, 2026-10-05: "make it an easter egg when you try to throw a slipper at full range directed
towards it you can actually hit it, and itll react like the balloon cow in overwatch. do it enough
times and itll pop", and "the slipper will just spawn/tp back to the nearest edge to be able to
retrieve it". The cues are fired by Runtime/Map/ArenaBalloon.cs and ArenaFallRecovery.cs on every
peer from state that peer already has (the balloon's one host message, a slipper's replicated
state), so none of them is relayed.

Writes these into Assets/TumbangPreso/Resources/Sfx/ (44100 Hz, mono, 16 bit, each normalised to
0.85 peak like every other cue there; the mix level is the cue's row in Runtime/Audio/AudioCues.cs):
    sfx_arena_balloon_fly.wav       1.10 s  the slipper leaving for the balloon: a rising whistle and air
    sfx_arena_balloon_squeak_a.wav  0.55 s  rubber squeaks, three of them: a pinched glide up and
    sfx_arena_balloon_squeak_b.wav  0.50 s    back with a rough edge, each on its own pitch and
    sfx_arena_balloon_squeak_c.wav  0.70 s    shape (the third is a double squeak)
    sfx_arena_balloon_boing.wav     1.30 s  the big wobble: a low rubber drum whose pitch wows down
    sfx_arena_balloon_creak.wav     1.10 s  strained skin: slow ratcheting rubber, a stitch ticking
    sfx_arena_balloon_pop.wav       1.80 s  the pop: a crack, a deep thump, air rushing out, flaps
    sfx_arena_balloon_hiss.wav      2.60 s  inflating again: a pump's hiss rising, two squeaks, a soft "pomp"
    sfx_arena_slipper_return.wav    0.60 s  a slipper set back on the stage: a soft chime and a tap

The house method (synth_arena_show_sfx.py, whose helpers these use): numpy and scipy only, fixed
seeds, no recordings and nothing downloaded, so a rerun is byte-identical."""
import argparse
import os
import sys
import wave

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from synth_arena_show_sfx import OUT, PEAK, SR, band, fade, highpass, lowpass, place, sweep, t_of   # noqa: E402


def glide(t, points):
    """A sine on a pitch that runs through (time, hertz) points, straight between them."""
    freq = np.interp(t, [p[0] for p in points], [p[1] for p in points])
    return 2 * np.pi * np.cumsum(freq) / SR


def rubber(t, points, rough, rng, bright=0.5):
    """One squeak: a pinched tone on a glide, its second and third partials, and stick-slip: the
    tone is broken up by a fast, slightly irregular flutter, which is what makes rubber squeak
    rather than whistle."""
    ph = glide(t, points)
    tone = np.sin(ph) + bright * np.sin(2 * ph + 0.3) + 0.5 * bright * np.sin(3 * ph)
    flutter = 0.5 + 0.5 * np.sin(2 * np.pi * rough * t + 1.5 * np.sin(2 * np.pi * 7.0 * t))
    grit = band(rng.standard_normal(len(t)), 2500, 9000) * 0.12
    return tone * (0.45 + 0.55 * flutter ** 2) + grit * flutter


def squeak_a(rng):
    t = t_of(0.55)
    env = np.clip(t / 0.02, 0, 1) * np.clip((0.55 - t) / 0.16, 0, 1) ** 1.2
    return fade(rubber(t, [(0, 620), (0.12, 1450), (0.30, 1180), (0.55, 720)], 46, rng) * env, 0.003, 0.05)


def squeak_b(rng):
    t = t_of(0.50)
    env = np.clip(t / 0.015, 0, 1) * np.clip((0.50 - t) / 0.2, 0, 1)
    return fade(rubber(t, [(0, 980), (0.10, 1820), (0.26, 1500), (0.50, 1040)], 58, rng, 0.42) * env, 0.003, 0.05)


def squeak_c(rng):
    """Two squeaks, the second higher: the toy being squeezed and let go."""
    t = t_of(0.70)
    out = np.zeros(len(t))
    for start, pts, rough in ((0.0, [(0, 760), (0.09, 1320), (0.26, 900)], 42), (0.30, [(0, 1050), (0.10, 2050), (0.36, 1250)], 54)):
        tt = t_of(0.36 if start else 0.26)
        env = np.clip(tt / 0.015, 0, 1) * np.clip((tt[-1] - tt) / 0.12, 0, 1)
        place(out, rubber(tt, pts, rough, rng) * env, start)
    return fade(out, 0.003, 0.05)


def boing(rng):
    """The whole balloon wobbling: a taut skin struck (a low tone whose pitch wows as the skin
    slackens and tightens), the air inside (a hollow band of noise), a rubbery overtone."""
    t = t_of(1.30)
    wow = 96 + 34 * np.exp(-t * 3.0) * np.cos(2 * np.pi * 5.2 * t) + 30 * np.exp(-t * 9)
    ph = 2 * np.pi * np.cumsum(wow) / SR
    skin = (np.sin(ph) + 0.45 * np.sin(2.02 * ph) + 0.2 * np.sin(3.1 * ph)) * np.exp(-t * 3.4)
    thump = sweep(t, 190, 70, 0.4) * np.exp(-t * 26) * 0.9
    air = band(rng.standard_normal(len(t)), 260, 900) * np.exp(-t * 7) * 0.35
    twang = np.sin(glide(t, [(0, 520), (0.2, 330), (1.3, 300)])) * np.exp(-t * 6.5) * (0.5 + 0.5 * np.cos(2 * np.pi * 5.2 * t)) * 0.3
    return fade(skin + thump + air + twang, 0.002, 0.2)


def creak(rng):
    """Skin under strain: a slow stick-slip (bursts of pinched tone that come quicker and rise), and
    a stitch ticking."""
    t = t_of(1.10)
    out = np.zeros(len(t))
    at, k = 0.03, 0
    while at < 0.98:
        n = t_of(0.05)
        f = 210 + 260 * (at / 1.0) + rng.uniform(-18, 18)
        grain = (np.sin(2 * np.pi * f * n) + 0.6 * np.sin(2 * np.pi * f * 2.03 * n)) * np.exp(-n * 55)
        place(out, grain * (0.5 + 0.5 * at), at)
        at += 0.085 * (1.0 - 0.55 * at) + rng.uniform(0, 0.012)
        k += 1
    rub = band(rng.standard_normal(len(t)), 700, 2600) * (0.15 + 0.25 * t / t[-1])
    tick = np.zeros(len(t))
    for start in (0.42, 0.71, 0.93):
        n = t_of(0.012)
        place(tick, band(rng.standard_normal(len(n)), 3000, 9000) * np.exp(-n * 420) * 0.7, start)
    return fade(lowpass(out, 5200) + rub * 0.5 + tick, 0.01, 0.1)


def pop(rng):
    """The pop: a crack with no warning, the thump of sixty metres of air let go, the rush of it
    leaving, and the skin's rags flapping as they fall."""
    t = t_of(1.80)
    crack = rng.standard_normal(len(t)) * np.exp(-t * 95) * 1.4
    crack += band(rng.standard_normal(len(t)), 900, 6000) * np.exp(-t * 28) * 0.9
    thump = sweep(t, 150, 30, 0.3) * np.exp(-t * 5.5) * 1.3
    sub = np.sin(2 * np.pi * 41 * t) * np.exp(-t * 4.0) * 0.6
    rush = band(rng.standard_normal(len(t)), 400, 5000) * np.clip(t / 0.03, 0, 1) * np.exp(-t * 3.6) * 0.5
    flaps = np.zeros(len(t))
    at = 0.22
    while at < 1.5:
        n = t_of(0.07)
        flap = band(rng.standard_normal(len(n)), 180, 1400) * np.exp(-n * 42)
        place(flaps, flap * rng.uniform(0.3, 0.7) * np.exp(-at * 1.3), at)
        at += rng.uniform(0.07, 0.16)
    whistle = np.sin(glide(t, [(0, 2600), (0.5, 900), (1.8, 500)])) * np.exp(-t * 5.0) * np.clip(t / 0.04, 0, 1) * 0.22
    return fade(crack + thump + sub + rush + flaps + whistle, 0.0005, 0.3)


def hiss(rng):
    """Inflating again: a pump's hiss that rises as the skin fills, a rubber squeak half way and
    another near the top, and a soft round "pomp" as it comes up to size."""
    t = t_of(2.60)
    share = t / t[-1]
    noise = rng.standard_normal(len(t))
    air = (band(noise, 1500, 5000) * (1 - share) + band(noise, 3000, 10000) * share) * np.clip(t / 0.08, 0, 1) * np.clip((2.3 - t) / 0.25, 0, 1) * 0.55
    air *= 0.8 + 0.2 * np.sin(2 * np.pi * 9 * t)                  # the pump's strokes
    body = np.sin(glide(t, [(0, 70), (2.2, 150), (2.6, 132)])) * share ** 1.5 * np.clip((2.45 - t) / 0.3, 0, 1) * 0.35
    out = air + body
    for start, lo, hi in ((0.95, 700, 1250), (1.75, 950, 1700)):
        tt = t_of(0.24)
        env = np.clip(tt / 0.02, 0, 1) * np.clip((0.24 - tt) / 0.1, 0, 1)
        place(out, rubber(tt, [(0, lo), (0.11, hi), (0.24, lo * 1.15)], 50, rng) * env * 0.45, start)
    tt = t_of(0.4)
    pomp = (sweep(tt, 210, 120, 0.5) * np.exp(-tt * 9) + band(rng.standard_normal(len(tt)), 300, 1200) * np.exp(-tt * 20) * 0.4) * 0.9
    place(out, pomp, 2.18)
    return fade(out, 0.03, 0.12)


def fly(rng):
    """The slipper leaving for the balloon: a slide whistle going up and away, and the air behind it."""
    t = t_of(1.10)
    share = t / t[-1]
    whistle = np.sin(glide(t, [(0, 520), (0.25, 1100), (1.1, 2300)])) * (1 - share) ** 1.4 * np.clip(t / 0.02, 0, 1) * 0.6
    whistle *= 0.8 + 0.2 * np.sin(2 * np.pi * 23 * t)             # the slipper turning over
    air = band(rng.standard_normal(len(t)), 900, 6000) * np.clip(t / 0.03, 0, 1) * (1 - share) ** 2.0 * 0.6
    return fade(whistle + air, 0.003, 0.2)


def slipper_return(rng):
    """Set back on the stage: two soft bell partials a fifth apart, and the rubber's tap as it lands."""
    t = t_of(0.60)
    bell = (np.sin(2 * np.pi * 1174.7 * t) * np.exp(-t * 9) + np.sin(2 * np.pi * 1760.0 * t) * np.exp(-t * 12) * 0.6
            + np.sin(2 * np.pi * 2349.3 * t) * np.exp(-t * 16) * 0.25) * 0.6
    tt = np.clip(t - 0.11, 0, None)
    tap = (sweep(t, 260, 120, 0.5) * np.exp(-tt * 34) + highpass(rng.standard_normal(len(t)), 1800) * np.exp(-tt * 110) * 0.5) * (t >= 0.11) * 0.8
    return fade(bell + tap, 0.002, 0.1)


SOUNDS = (
    ("sfx_arena_balloon_fly", fly, 2401),
    ("sfx_arena_balloon_squeak_a", squeak_a, 2402),
    ("sfx_arena_balloon_squeak_b", squeak_b, 2403),
    ("sfx_arena_balloon_squeak_c", squeak_c, 2404),
    ("sfx_arena_balloon_boing", boing, 2405),
    ("sfx_arena_balloon_creak", creak, 2406),
    ("sfx_arena_balloon_pop", pop, 2407),
    ("sfx_arena_balloon_hiss", hiss, 2408),
    ("sfx_arena_slipper_return", slipper_return, 2409),
)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", default="")
    args = ap.parse_args()
    OUT.mkdir(parents=True, exist_ok=True)
    for name, build, seed in SOUNDS:
        if args.only and args.only not in name:
            continue
        x = build(np.random.default_rng(seed))
        x = x / np.abs(x).max() * PEAK
        path = OUT / (name + ".wav")
        with wave.open(str(path), "wb") as w:
            w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
            w.writeframes((x * 32767).astype(np.int16).tobytes())
        print("%-28s %.2f s  rms %.3f  ends %d %d" % (name, len(x) / SR, float(np.sqrt((x ** 2).mean())), int(x[0] * 32767), int(x[-1] * 32767)))


if __name__ == "__main__":
    main()
