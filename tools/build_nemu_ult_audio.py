"""Nemu's PRUSISYON sounds, DRAFTS for the owner's ear (`docs/reports/nemu-ultimate-2026-10-11/direction.md` section 6).

Skill sounds stay switched off in the game until the owner picks a draft by ear (`AudioCues.ReworkedSkillSfx`), and an
unapproved skill sound has no file in the game (`AudioCues`, 2026-09-29). So these are written to the report's
`audio-drafts/` and mixed into the preview film; an approved draft is copied to `Resources/Sfx` and registered then. numpy only, seeded: a rebuild writes identical bytes.

    python tools/build_nemu_ult_audio.py

Instruments (each typed below, never one recipe for two cues):
  toll     the agunyas, the church bell for the dead: inharmonic partials over a low hum, a long tail
  handbell the Sakristan's small bell: bright partials, quick decay
  hum      a low choir of detuned 'oo' voices (formant-filtered saw), swelling, cut dead
  breath   shaped noise: the hush, the inhale, the breath out
  shuffle  bare feet on stone in step: short low noise grains on the beat
  whisper  band-passed noise with a syllable-rate tremolo
  snuff    a soft puff and a wick's hiss
  suck     a reversed swell into a low thump (a soul pulled in)
  chime    a cold glassy partial pair (the eyes)
Cues: sfx_ult_theme_nemu (the 5.8 s cutscene, every beat at the stage's time), sfx_nemu_possess_tell, sfx_nemu_possess_surge.
"""
import os
import wave

import numpy as np

RATE = 44100
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "docs", "reports", "nemu-ultimate-2026-10-11", "audio-drafts")
rng = np.random.default_rng(20261011)


def t_axis(seconds):
    return np.arange(int(seconds * RATE)) / RATE


def env(n, attack, release, hold=None):
    """A linear attack and an exponential-ish release over n samples."""
    x = np.ones(n)
    a = max(1, int(attack * RATE))
    x[:a] = np.linspace(0, 1, a)
    if release > 0:
        r = np.arange(n) / RATE
        x *= np.exp(-np.maximum(0, r - attack) / release)
    return x


def lowpass(x, cutoff):
    """One-pole low-pass, run twice."""
    k = np.exp(-2 * np.pi * cutoff / RATE)
    for _ in range(2):
        y = np.empty_like(x); acc = 0.0
        for i in range(len(x)):
            acc = (1 - k) * x[i] + k * acc
            y[i] = acc
        x = y
    return x


def bandnoise(seconds, lo, hi):
    """Noise between lo and hi Hz (FFT mask)."""
    n = int(seconds * RATE)
    spec = np.fft.rfft(rng.standard_normal(n))
    f = np.fft.rfftfreq(n, 1 / RATE)
    spec[(f < lo) | (f > hi)] = 0
    x = np.fft.irfft(spec, n)
    return x / (np.abs(x).max() + 1e-9)


def toll(seconds=3.0, base=98.0, gain=1.0):
    t = t_axis(seconds)
    # Bell partials (hum, prime, tierce, quint, nominal...), each with its own decay: typed.
    partials = [(.5, .55, 2.6), (1.0, 1.0, 2.2), (1.19, .45, 1.6), (1.5, .35, 1.3), (2.0, .55, 1.1), (2.51, .25, .8), (2.98, .2, .6), (4.07, .12, .4)]
    x = np.zeros_like(t)
    for ratio, amp, decay in partials:
        x += amp * np.sin(2 * np.pi * base * ratio * t + ratio) * np.exp(-t / decay)
    strike = bandnoise(seconds, 800, 5000) * np.exp(-t / .02) * .25
    return (x / 3.0 + strike) * gain


def handbell(seconds=.9, base=1180.0, gain=1.0):
    t = t_axis(seconds)
    x = np.zeros_like(t)
    for ratio, amp, decay in [(1.0, 1.0, .35), (2.32, .5, .22), (3.9, .3, .14), (5.4, .2, .09)]:
        x += amp * np.sin(2 * np.pi * base * ratio * t) * np.exp(-t / decay)
    return x / 2.0 * gain


def hum(seconds, notes=(110.0, 130.81, 164.81, 98.0), gain=1.0):
    """A low choir: four detuned voices on a minor chord through an 'oo' formant (two resonances)."""
    t = t_axis(seconds)
    x = np.zeros_like(t)
    for i, f in enumerate(notes):
        for detune in (-.35, .0, .4):
            ph = 2 * np.pi * (f + detune) * t + i
            saw = 2 * ((ph / (2 * np.pi)) % 1) - 1
            x += saw * (1 + .1 * np.sin(2 * np.pi * (.3 + .07 * i) * t))
    x = lowpass(x, 520)
    return x / (np.abs(x).max() + 1e-9) * gain


def breath(seconds, lo=300, hi=4500, attack=.15, shape="hush", gain=1.0):
    x = bandnoise(seconds, lo, hi)
    n = len(x)
    u = np.linspace(0, 1, n)
    if shape == "hush":       # 'shh': quick swell, a held hiss, a soft tail
        e = np.minimum(1, u / .12) * np.exp(-np.maximum(0, u - .45) * 5)
    elif shape == "in":       # an inhale: rising, cut at the top
        e = u ** 1.6 * (u < .97)
    else:                     # a breath out: falling
        e = np.exp(-u * 3.5) * np.minimum(1, u / .05)
    return x * e * gain


def shuffle(seconds, beat_hz, gain=1.0):
    n = int(seconds * RATE)
    x = np.zeros(n)
    step = 1 / beat_hz
    k = 0.0
    while k < seconds:
        i = int(k * RATE)
        g = bandnoise(.09, 90, 900) * env(int(.09 * RATE), .004, .03)
        x[i:i + len(g)] += g[:max(0, min(len(g), n - i))]
        k += step
    return x * gain


def whisper(seconds, rate_hz=7.0, gain=1.0):
    x = bandnoise(seconds, 1800, 6500)
    t = t_axis(seconds)[:len(x)]
    trem = .5 + .5 * np.sin(2 * np.pi * rate_hz * t + 1.3 * np.sin(2 * np.pi * 1.7 * t))
    return x * trem ** 2 * gain


def snuff(gain=1.0):
    p = bandnoise(.25, 200, 2200) * env(int(.25 * RATE), .005, .06)
    h = bandnoise(.6, 3000, 9000) * env(int(.6 * RATE), .02, .25) * .3
    out = np.zeros(int(.6 * RATE)); out[:len(p)] += p; out[:len(h)] += h
    return out * gain


def suck(gain=1.0):
    n = int(.5 * RATE)
    swell = bandnoise(.42, 300, 3000)
    u = np.linspace(0, 1, len(swell))
    swell *= u ** 3
    t = t_axis(.35)
    thump = np.sin(2 * np.pi * (70 - 30 * t) * t) * np.exp(-t / .09)
    out = np.zeros(n)
    out[:len(swell)] += swell * .7
    i = len(swell) - int(.02 * RATE)
    out[i:i + len(thump)] += thump[:n - i]
    return out * gain


def chime(gain=1.0):
    t = t_axis(1.2)
    x = (np.sin(2 * np.pi * 1660 * t) + .6 * np.sin(2 * np.pi * 2490 * t + 1)) * np.exp(-t / .5)
    return x * .5 * gain


def place(track, sound, at):
    i = int(at * RATE)
    j = min(len(track), i + len(sound))
    if j > i:
        track[i:j] += sound[:j - i]


def theme():
    """The cutscene, beat for beat (`HeroIntroductionScene.Nemu.cs`): NmHush .40, NmGrey .50, NmKindle .85,
    NmProcession 1.20, bells 1.55/2.05/2.55, NmTurn 2.95, NmSnuff 3.30, NmRelease 3.42, NmArrive 4.06, NmSuck 4.38,
    NmEyes 5.25, NmIris 5.55, end 5.8."""
    L = 5.8
    track = np.zeros(int((L + 1.0) * RATE))
    place(track, breath(.2, 500, 3000, shape="in", gain=.25), .2)          # a held breath before the hush
    place(track, breath(.55, 2000, 7000, shape="hush", gain=.55), .40)     # shh
    place(track, toll(3.4, 92.0, gain=.95), .55)                            # the death bell
    place(track, breath(.3, 1500, 6000, shape="out", gain=.2), .85)        # the flame kindles on a breath
    choir = hum(1.9, gain=.5) * np.linspace(0, 1, int(1.9 * RATE)) ** 1.5  # the dead's choir swells...
    place(track, choir, 1.05)                                               # ...and is cut dead at 2.95 (it ends there)
    place(track, shuffle(1.75, 1.6, gain=.32), 1.2)                         # in step
    for b in (1.55, 2.05, 2.55):
        place(track, handbell(.9, 1180.0, gain=.32), b)
    place(track, breath(.3, 800, 5000, shape="in", gain=.5), 2.65)          # they all draw breath and turn
    place(track, snuff(.8), 3.30)                                           # every flame dies
    place(track, whisper(.95, 7.5, .75) * np.linspace(.3, 1, int(.95 * RATE)), 3.42)  # the souls tear loose
    place(track, whisper(.35, 12.0, .85), 4.06)                             # fighting at the bodies
    for k, d in enumerate((0.0, .028, .056)):
        place(track, suck(.75 - .12 * k), 4.38 - .4 + d)                     # sucked in, one after another
    drone = hum(1.3, notes=(55.0, 65.41, 82.41), gain=.3)
    place(track, drone * np.exp(-np.linspace(0, 2, len(drone))), 4.45)
    place(track, toll(1.5, 92.0, gain=.35), 5.25)                            # the bell again, softer
    place(track, chime(.35), 5.25)                                          # their eyes
    track = track[:int(L * RATE)]
    fade = np.ones(len(track)); f0 = int(5.55 * RATE); fade[f0:] = np.linspace(1, 0, len(track) - f0) ** 1.5
    return track * fade


def tell():
    return breath(.5, 900, 6000, shape="in", gain=.6) + whisper(.5, 9.0, .25)


def surge():
    t = t_axis(.75)
    beat = np.zeros(len(t))
    for at in (.0, .22):
        tt = t_axis(.2)
        hb = np.sin(2 * np.pi * 52 * tt) * np.exp(-tt / .06)
        place(beat, hb, at)
    return breath(.75, 300, 2500, shape="out", gain=.55) + beat * .8


def write(name, x, peak=.89):
    x = x / (np.abs(x).max() + 1e-9) * peak
    data = (np.clip(x, -1, 1) * 32767).astype("<i2")
    path = os.path.join(OUT, name + ".wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(RATE); w.writeframes(data.tobytes())
    print("wrote", path, f"{len(x) / RATE:.2f}s")


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    write("sfx_ult_theme_nemu", theme())
    write("sfx_nemu_possess_tell", tell(), .6)
    write("sfx_nemu_possess_surge", surge(), .75)
