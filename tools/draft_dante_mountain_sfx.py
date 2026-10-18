"""Draft sounds for Dante's MOUNTAIN FORM, for the owner to pick by ear. Nothing here is wired into the game.

    python3 tools/draft_dante_mountain_sfx.py Logs/mf/sfx_drafts

The skill sounds are off on the owner's instruction (`AudioCues.SkillSfxOn`, 2026-09-29) and come back one ability at a
time, each chosen from drafts first (`AudioCues.ReworkedSkillSfx`). So this writes two takes of every cue the form needs
into a review folder, plus one reel that plays them in order, and the chosen take is registered only after he hears it:

  form    the armour closing over him: grinding stone, three locks on the charge-up's beats, a molten hiss
  stomp   one armoured footfall: a low body thump under a gravel crunch (it replaces the pitched-down `land`)
  hit     his rock throw landing on a player: a hard crack with a short boom
  crack   one stone of the suit cracking off as it sheds (it plays many times, so it is short and quiet)
  end     the last of the suit falling away: a crumble that settles

Take A is dry and tight, take B wetter and lower. numpy only, deterministic (fixed seeds), 44.1 kHz mono 16-bit.
"""
import os
import sys
import wave

import numpy as np

RATE = 44100


def t_axis(seconds):
    return np.arange(int(seconds * RATE)) / RATE


def env(t, attack, decay):
    return np.minimum(1.0, t / max(attack, 1e-4)) * np.exp(-np.maximum(0.0, t - attack) / decay)


def lowpass(x, cutoff):
    # one-pole, run twice for a gentler 12 dB slope
    a = np.exp(-2.0 * np.pi * cutoff / RATE)
    for _ in range(2):
        y = np.empty_like(x)
        acc = 0.0
        for i, v in enumerate(x):
            acc = (1 - a) * v + a * acc
            y[i] = acc
        x = y
    return x


def highpass(x, cutoff):
    return x - lowpass(x, cutoff)


def noise(rng, n):
    return rng.standard_normal(n)


def thump(t, freq, drop, decay):
    # a pitched body hit: a sine falling from freq*(1+drop) to freq
    f = freq * (1.0 + drop * np.exp(-t / 0.03))
    phase = 2.0 * np.pi * np.cumsum(f) / RATE
    return np.sin(phase) * env(t, 0.002, decay)


def grains(rng, seconds, count, size, cutoff, spread=1.0):
    """Gravel: many short noise grains scattered over the first `spread` of the sound."""
    n = int(seconds * RATE)
    out = np.zeros(n)
    for _ in range(count):
        start = int(rng.uniform(0, spread) * n)
        length = int(rng.uniform(.004, size) * RATE)
        if start + length >= n:
            continue
        g = noise(rng, length) * np.exp(-np.arange(length) / (length / 4.0)) * rng.uniform(.3, 1.0)
        out[start:start + length] += g
    return highpass(lowpass(out, cutoff), 180.0)


def normal(x, peak=0.89):
    m = np.max(np.abs(x))
    return x * (peak / m) if m > 0 else x


def stomp(take):
    rng = np.random.default_rng(11 + take)
    t = t_axis(0.62)
    body = thump(t, 48.0 if take == 0 else 38.0, 1.6, 0.11 if take == 0 else 0.16)
    gravel = grains(rng, 0.62, 70, 0.03, 3200.0, spread=0.35) * env(t, 0.001, 0.12)
    dust = lowpass(noise(rng, len(t)), 900.0) * env(t, 0.01, 0.2) * 0.25
    return normal(1.0 * body + 0.35 * gravel + dust)


def hit(take):
    rng = np.random.default_rng(23 + take)
    t = t_axis(0.7)
    crack = highpass(noise(rng, len(t)), 1200.0) * env(t, 0.0005, 0.018)
    boom = thump(t, 62.0 if take == 0 else 50.0, 2.2, 0.14 if take == 0 else 0.22)
    chips = grains(rng, 0.7, 45, 0.02, 5000.0, spread=0.3) * env(t, 0.001, 0.16)
    return normal(0.8 * crack + 1.0 * boom + 0.4 * chips)


def crack(take):
    rng = np.random.default_rng(37 + take)
    t = t_axis(0.42)
    snap = highpass(noise(rng, len(t)), 900.0) * env(t, 0.0005, 0.012)
    tumble = grains(rng, 0.42, 14, 0.025, 2600.0, spread=0.9) * env(t, 0.03, 0.18)
    knock = thump(t, 120.0 if take == 0 else 90.0, 0.8, 0.05)
    return normal(0.9 * snap + 0.5 * tumble + 0.45 * knock, 0.7)


def form(take):
    rng = np.random.default_rng(41 + take)
    seconds = 2.3
    t = t_axis(seconds)
    # grinding stone: band-limited noise with a slow rough amplitude, swelling to the end
    grind = lowpass(highpass(noise(rng, len(t)), 140.0), 1400.0 if take == 0 else 900.0)
    rough = 0.55 + 0.45 * np.abs(np.sin(2 * np.pi * 7.3 * t + 3 * np.sin(2 * np.pi * .7 * t)))
    grind *= rough * np.minimum(1.0, t / 0.6) * np.exp(-np.maximum(0.0, t - 1.9) / 0.15)
    out = 0.45 * grind
    # three locks on the beats the stone lands on him (.84, 1.14, 1.44) and the last at FormedAt 2.0
    for at, gain in ((0.84, .8), (1.14, .85), (1.44, .9), (2.0, 1.0)):
        i = int(at * RATE)
        seg = np.arange(len(out) - i) / RATE
        out[i:] += gain * (thump(seg, 70.0 if take == 0 else 55.0, 1.4, 0.07) + 0.4 * highpass(noise(rng, len(seg)), 1500.0) * env(seg, .0005, .01))
    # a molten hiss under it
    hiss = highpass(noise(rng, len(t)), 3500.0) * 0.06 * np.minimum(1.0, t / 1.2) * np.exp(-np.maximum(0.0, t - 2.0) / 0.2)
    return normal(out + hiss)


def end(take):
    rng = np.random.default_rng(53 + take)
    t = t_axis(1.6)
    crumble = grains(rng, 1.6, 140 if take == 0 else 200, 0.04, 2400.0, spread=0.8) * env(t, 0.05, 0.6)
    settle = thump(t, 44.0, 0.9, 0.3) * 0.6
    sigh = lowpass(noise(rng, len(t)), 500.0) * env(t, 0.3, 0.5) * 0.3
    return normal(0.7 * crumble + settle + sigh)


CUES = {"form": form, "stomp": stomp, "hit": hit, "crack": crack, "end": end}


def write(path, x):
    data = (np.clip(x, -1, 1) * 32767).astype(np.int16)
    with wave.open(path, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(RATE)
        w.writeframes(data.tobytes())


def main():
    out = sys.argv[1] if len(sys.argv) > 1 else "Logs/mf/sfx_drafts"
    os.makedirs(out, exist_ok=True)
    reel = []
    gap = np.zeros(int(0.5 * RATE))
    for name, build in CUES.items():
        for take in (0, 1):
            x = build(take)
            write(os.path.join(out, f"mountain_{name}_{'AB'[take]}.wav"), x)
            reel += [x, gap]
            print(f"  mountain_{name}_{'AB'[take]}.wav  {len(x) / RATE:.2f} s")
    write(os.path.join(out, "mountain_reel.wav"), np.concatenate(reel))
    print("  mountain_reel.wav (every take in order: form, stomp, hit, crack, end; A then B)")


if __name__ == "__main__":
    main()
