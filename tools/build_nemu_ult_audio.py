"""Nemu's LET THEM IN sounds, DRAFTS for the owner's ear (`docs/reports/nemu-ultimate-2026-10-11/direction.md` 11.7).

Skill sounds stay switched off in the game until the owner picks a draft by ear (`AudioCues.ReworkedSkillSfx`), and an
unapproved skill sound has no file in the game (`AudioCues`, 2026-09-29). So these are written to the report's
`audio-drafts/` and mixed into the preview film; an approved draft is copied to `Resources/Sfx` and registered then.
numpy only, seeded: a rebuild writes identical bytes.

    python tools/build_nemu_ult_audio.py

Instruments (each typed below, never one recipe for two cues):
  drone    the room under everything, low and breathing; it ducks when her eyes ignite
  breath   shaped noise: the hush, the inhale, the breath out
  chime    a cold glassy partial pair (eyes igniting)
  sub      the ink spreading: a sub sine sliding up
  crackle  something wet breaking the surface (the first claw)
  wind     the ink racing over the court
  whisper  band-passed noise with a syllable-rate tremolo (the dead rising)
  hum      a low choir of detuned voices, swelling, cut dead on the held beat
  wail     the shriek as they launch
  suck     a reversed swell into a low thump (a soul going in)
  groan    a body fighting it
  snap     a claw yanked back in
  (toll, handbell, shuffle, snuff: v1's procession, kept as instruments, no longer in a cue)
Cues: sfx_ult_theme_nemu (the 5.6 s cutscene, every beat at the stage's time), sfx_nemu_possess_tell, sfx_nemu_possess_surge.
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


def drone(seconds, base=41.2, gain=1.0):
    """The room under everything: two low detuned sines and a filtered rumble, breathing slowly."""
    t = t_axis(seconds)
    x = np.sin(2 * np.pi * base * t) + .7 * np.sin(2 * np.pi * (base * 1.498 + .3) * t + 1.1)
    x *= .75 + .25 * np.sin(2 * np.pi * .45 * t)
    rumble = bandnoise(seconds, 30, 160)[:len(t)] * .35
    return (x / 1.7 + rumble) * gain


def sub_swell(seconds, gain=1.0):
    """The ink spreading: a sub sine sliding up, swelling."""
    t = t_axis(seconds)
    f = 28 + 22 * (t / seconds) ** 1.5
    ph = 2 * np.pi * np.cumsum(f) / RATE
    # Swells, then falls away into the flood's wind rather than stopping at its loudest.
    return np.sin(ph) * np.sin(np.pi * np.minimum(1, (t / seconds) * .62)) ** 1.5 * gain


def crackle(seconds=.45, gain=1.0):
    """Something wet breaking the surface: short clicks thickening."""
    n = int(seconds * RATE)
    x = np.zeros(n)
    for k in range(26):
        at = int((k / 26) ** .7 * (n - 800))
        g = bandnoise(.012, 1200, 7000) * env(int(.012 * RATE), .0005, .004)
        x[at:at + len(g)] += g[:n - at] * (.4 + .6 * ((k * 7) % 5) / 4)
    return lowpass(x, 5200) * 1.5 * gain


def wind(seconds, gain=1.0):
    """The ink racing over the court: a low rushing swell."""
    x = bandnoise(seconds, 120, 1400)
    u = np.linspace(0, 1, len(x))
    return x * np.sin(np.pi * np.minimum(1, u * 1.3)) ** 1.5 * gain


def wail(seconds=.6, gain=1.0):
    """The shriek as they launch: a pitched wail falling, under a torn whisper."""
    t = t_axis(seconds)
    f = 880 - 420 * (t / seconds)
    ph = 2 * np.pi * np.cumsum(f * (1 + .02 * np.sin(2 * np.pi * 6 * t))) / RATE
    tone = (np.sin(ph) + .4 * np.sin(2 * ph + .7)) * np.minimum(1, t / .04) * np.exp(-t / .45)
    tear = bandnoise(seconds, 2500, 9000)[:len(t)] * np.exp(-t / .25) * .6
    return (tone * .6 + tear) * gain


def groan(seconds=.5, base=73.0, gain=1.0):
    """A body fighting it: a low formant voice under the breath."""
    t = t_axis(seconds)
    ph = 2 * np.pi * (base + 6 * np.sin(2 * np.pi * 3.1 * t)) * t
    saw = 2 * ((ph / (2 * np.pi)) % 1) - 1
    x = lowpass(saw, 380) * np.sin(np.pi * t / seconds) ** 1.3
    return x * gain


def snap(gain=1.0):
    """A claw yanked back in: a short sucked whoosh into a dry crack."""
    w = bandnoise(.14, 500, 5000)
    w *= np.linspace(0, 1, len(w)) ** 3
    c = bandnoise(.05, 1500, 8000) * env(int(.05 * RATE), .0008, .012)
    out = np.zeros(int(.2 * RATE)); out[:len(w)] += w * .6; out[len(w) - 200:len(w) - 200 + len(c)] += c
    return out * gain


def place(track, sound, at):
    i = int(at * RATE)
    j = min(len(track), i + len(sound))
    if j > i:
        track[i:j] += sound[:j - i]


# The stage's times (`HeroIntroductionScene.Nemu.cs`): keep in step with NmHush, NmIgnite, NmSpread, NmTellClaw, NmFlood,
# NmSnap, NmLaunch, NmEnterFirst/NmEnterLast, NmFight, NmEscapes, NmEyesOn, NmDrain, NmRelease.
HUSH, IGNITE, SPREAD, CLAW, FLOOD, SNAP, LAUNCH = .30, .42, .55, .85, 1.10, 2.35, 2.55
ENTER_FIRST, ENTER_LAST, FIGHT, EYES, DRAIN, RELEASE, LENGTH = 3.10, 3.75, 3.80, 4.00, 4.20, 5.15, 5.6
ESCAPES = (.05, .30, .52, .70)


def theme():
    """LET THEM IN, beat for beat (`direction.md` section 11.4)."""
    track = np.zeros(int((LENGTH + 1.0) * RATE))
    room = drone(LENGTH + .5, gain=.35)
    # Everything ducks when her eyes ignite, then the drone comes back heavier under the flood.
    u = t_axis(len(room) / RATE)
    duck = 1 - .6 * np.exp(-((u - (IGNITE + .1)) / .18) ** 2)
    room *= np.minimum(1, u / .25) * duck * (1 + .5 * np.clip((u - FLOOD) / 1.0, 0, 1))
    place(track, room, 0)
    place(track, breath(.5, 2000, 7000, shape="hush", gain=.6), HUSH)          # shh
    place(track, chime(.18), IGNITE)                                           # her eyes
    place(track, sub_swell(FLOOD - SPREAD + .9, gain=.4), SPREAD)               # the shadow spreading
    place(track, crackle(gain=.45), CLAW)                                      # a claw breaks the surface
    place(track, wind(1.6, gain=.2), FLOOD)                                   # the ink floods the court
    # The dead rising: whispers thickening and a choir swelling, CUT DEAD on the snap.
    rise = SNAP - (FLOOD + .1)
    swell = np.linspace(.15, 1, int(rise * RATE)) ** 1.1
    place(track, whisper(rise, 8.0, 1.0) * swell, FLOOD + .1)
    place(track, hum(rise, notes=(98.0, 116.54, 146.83, 87.31), gain=1.6) * swell, FLOOD + .1)
    place(track, breath(.18, 800, 5000, shape="in", gain=.4), SNAP + .02)       # every head turns: one breath in
    place(track, wail(.7, 1.0), LAUNCH)                                         # they launch
    place(track, wind(.6, gain=.35), LAUNCH + .1)
    for k in range(7):                                                         # into the players, one after another
        at = ENTER_FIRST + (ENTER_LAST - ENTER_FIRST) * k / 6
        place(track, suck(1.15 - .1 * (k % 3)), at - .4)
    for v in range(3):                                                         # they fight it
        place(track, groan(.9, 66.0 + 9 * v, gain=.7), FIGHT + .11 * v)
        for e in ESCAPES:
            place(track, snap(.75 + .15 * ((v + int(e * 10)) % 2)), FIGHT + .11 * v + e + .26)
    place(track, chime(.3), EYES)                                              # their eyes snap on
    tail = drone(1.6, base=36.7, gain=.3)
    place(track, tail * np.exp(-np.linspace(0, 1.5, len(tail))), DRAIN)        # the ink drains into them
    place(track, whisper(.5, 6.0, .3) * np.linspace(1, 0, int(.5 * RATE)), 4.75)  # one last whisper
    track = track[:int(LENGTH * RATE)]
    fade = np.ones(len(track)); f0 = int(RELEASE * RATE); fade[f0:] = np.linspace(1, 0, len(track) - f0) ** 1.2
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
