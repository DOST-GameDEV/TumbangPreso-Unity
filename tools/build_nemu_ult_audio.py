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
  buzz     the lights: a buzzing tube, cut dead in each blackout
  clunk    the lights failing
  heartbeat  quickening once the dead are right behind them
  gulp     the sky swallowed: a wet throat sweeping down into a sub hit (SWALLOWED SKY, section 13; kept as an instrument)
  peel     a shadow tearing up off the court: a slow stretching creak under a rising whisper (THEIR OWN SHADOWS, section 14)
  (toll, handbell, shuffle, snuff: v1's procession, kept as instruments, no longer in a cue)
  (buzz and clunk: BEHIND YOU's failing lights, retired with them in section 13, kept as instruments)
Cues: sfx_ult_theme_nemu (the 7.0 s THE PRAYER cutscene, v9, every beat at the stage's time), sfx_nemu_possess_tell, sfx_nemu_possess_surge.
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


def buzz(seconds, gain=1.0):
    """The lights: a mains hum (a buzzing tube) with a little flutter, under the court while the lights are on."""
    t = t_axis(seconds)
    ph = 2 * np.pi * 120.0 * t
    saw = 2 * ((ph / (2 * np.pi)) % 1) - 1
    x = lowpass(saw, 900) * .5 + .25 * np.sin(2 * ph)
    x *= .85 + .15 * np.sin(2 * np.pi * 7.3 * t)
    return x * gain


def clunk(gain=1.0):
    """The lights failing: a relay's dry click and a low thump."""
    c = bandnoise(.03, 1800, 9000) * env(int(.03 * RATE), .0005, .006)
    tt = t_axis(.25)
    th = np.sin(2 * np.pi * (60 - 25 * tt / .25) * tt) * np.exp(-tt / .07)
    out = np.zeros(len(tt)); out[:len(c)] += c * .8; out += th
    return out * gain


def heartbeat(start, end, track, gain=1.0):
    """A heartbeat that quickens from start to end (lub-dub)."""
    at = start
    period = .62
    while at < end:
        for k, g in ((0, 1.0), (.14, .7)):
            tt = t_axis(.18)
            hb = np.sin(2 * np.pi * 48 * tt) * np.exp(-tt / .05) * g
            place(track, hb * gain, at + k)
        at += period
        period = max(.32, period * .82)


def gulp(gain=1.0):
    """The sky swallowed: a wet throat sweeping down (a formant on a falling pitch) into a sub hit, with a hiss of ink."""
    t = t_axis(.6)
    f = 190 * np.exp(-t / .12) + 38
    ph = 2 * np.pi * np.cumsum(f) / RATE
    throat = lowpass(np.sin(ph) + .5 * np.sin(2 * ph + .4), 700) * np.minimum(1, t / .01) * np.exp(-t / .2)
    hit = np.sin(2 * np.pi * (46 - 14 * t) * t) * np.exp(-t / .22)
    hiss = bandnoise(.6, 1500, 7000)[:len(t)] * np.exp(-t / .08) * .25
    return (throat * .6 + hit + hiss) * gain


def peel(seconds, gain=1.0):
    """A shadow tearing up off the court: a slow creak (a low pitch wobbling upward through a narrow band) and a tearing hiss."""
    t = t_axis(seconds)
    f = 70 + 90 * (t / seconds) ** 1.4 + 6 * np.sin(2 * np.pi * 9 * t)
    ph = 2 * np.pi * np.cumsum(f) / RATE
    creak = lowpass(2 * ((ph / (2 * np.pi)) % 1) - 1, 900) * np.sin(np.pi * np.minimum(1, t / seconds)) ** 1.2
    tear = bandnoise(seconds, 900, 4000)[:len(t)] * (t / seconds) ** 2 * .35
    return (creak * .7 + tear) * gain


# v9 THE PRAYER (`direction.md` section 18), at the stage's own times (`HeroIntroductionScene.Nemu.cs` `Nm*`).
LENGTH = 7.0
BEAT, IGNITE, SPLIT, LEAD, LEAD_IN, FIRST_EYES, BURST = .21, .72, .85, 1.35, 2.2, 2.5, 2.55
RUN, TRIP, LANDED, ROLL, BACK_IN, TRIP_IN, RISE, TRIP_EYES, CRANE = 2.62, 3.95, 4.15, 4.2, 4.2, 4.6, 4.95, 5.3, 5.5


def theme():
    """LET THEM IN v9: THE PRAYER, beat for beat."""
    track = np.zeros(int((LENGTH + 1.0) * RATE))
    room = drone(LENGTH + .5, gain=.3)
    u = t_axis(len(room) / RATE)
    # The room ducks under her chant, then comes back heavier once the court splits.
    duck = 1 - .5 * np.clip(1 - u / SPLIT, 0, 1)
    room *= np.minimum(1, u / .25) * duck * (1 + .6 * np.clip((u - SPLIT) / 1.0, 0, 1))
    place(track, room, 0)
    # THE PRAYER: a low chant on each beat, a whisper under it, the light gathering in her palms.
    t = 0.0
    while t < SPLIT - .05:
        place(track, hum(BEAT * .9, notes=(98.0, 103.83), gain=.35) * np.hanning(int(BEAT * .9 * RATE)), t)
        t += BEAT
    place(track, whisper(SPLIT, 5.0, .3), 0)
    place(track, sub_swell(SPLIT, gain=.3), 0)
    place(track, chime(.2), IGNITE)                                               # her eyes snap open
    # THE SPLIT: the court breaking open, the boom under it, the wind of it racing out.
    place(track, crackle(.6, gain=.9), SPLIT)
    place(track, crackle(.45, gain=.6), SPLIT + .18)
    place(track, gulp(.7), SPLIT - .03)
    place(track, wind(.6, gain=.35), SPLIT + .05)
    # THE FIRST ONE: it claws out alone and wails; a slow heartbeat as everyone watches it circle.
    place(track, crackle(.35, gain=.4), LEAD - .05)
    place(track, wail(.7, gain=.55), LEAD + .15)
    place(track, whisper(LEAD_IN - LEAD, 4.0, .25), LEAD)
    heartbeat(LEAD + .1, LEAD_IN, track, gain=.45)
    place(track, breath(.25, 900, 6000, shape="in", gain=.7), LEAD_IN - .22)    # the gasp as it turns on him
    place(track, suck(1.1), LEAD_IN - .4)                                         # it pours into him
    place(track, groan(.45, 70.0, gain=.55), LEAD_IN + .05)
    place(track, chime(.26), FIRST_EYES)                                          # his eyes open violet
    # SUDDENLY MANY: every fissure erupts, a chorus of wails, the storm's wind.
    for k in range(6):
        place(track, wail(.55 + .05 * k, gain=.35), BURST + .04 * k)
    place(track, wind(2.0, gain=.45), BURST)
    place(track, crackle(.5, gain=.5), BURST)
    # THE FALL: running, the heart racing; the trip, the body hitting the court; the gasp as he sees it; it goes in; one
    # caught behind him first.
    heartbeat(RUN, TRIP_IN, track, gain=.7)
    place(track, snap(.6), TRIP)
    place(track, clunk(.7), LANDED)
    place(track, suck(.45), BACK_IN - .35)
    place(track, breath(.3, 900, 6000, shape="in", gain=.85), ROLL + .2)
    place(track, suck(1.2), TRIP_IN - .42)
    place(track, crackle(.3, gain=.45), TRIP_IN)
    place(track, groan(.5, 64.0, gain=.55), TRIP_IN + .06)
    place(track, peel(.5, gain=.7), RISE)                                         # hauled up like a puppet
    place(track, chime(.3), TRIP_EYES)
    # ALL OF THEM: a low choir swelling as they stare.
    rise = LENGTH - CRANE + .3
    swell = np.linspace(.2, 1, int(rise * RATE)) ** 1.2
    place(track, hum(rise, notes=(98.0, 116.54, 146.83, 87.31), gain=1.1) * swell, CRANE)
    place(track, whisper(rise, 6.0, .35) * swell, CRANE)
    track = track[:int(LENGTH * RATE)]
    fade = np.ones(len(track)); f0 = int((LENGTH - .6) * RATE); fade[f0:] = np.linspace(1, 0, len(track) - f0) ** 1.2
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
