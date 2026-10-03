"""AIRBURST v3's four sounds: original, deterministic, numpy only. Writes ONLY these four files.

Owner, 2026-10-03, on the feel-pass films: "throoughly revamp her cutscene ... as well as the vfx sfx an of her ult".
Every hero's skill sounds were deleted on 2026-09-29 (`AudioCues.IsSkillSfx`); these four are the first released from that
switch (`AudioCues.ReworkedSkillSfx`). Direction: docs/reports/amihan-presentation-2026-10-02/airburst-v3.md, "Sound".

Her family of instruments: AIR through moving filters (the 2026-09-25 recipes in `build_amihan_audio.py`, whose helpers this
reuses), a BAMBOO FLUTE for her theme, and the LOOM: a struck wooden beater (the batten knocking the weft home) and a plucked
warp thread (Karplus-Strong). The theme is timed beat for beat to the 5.6 s cutscene; the gather to the live 1.5 s windup
(`AmihanRules.StormSurgeGatherSeconds`, its pack beats at a third and two thirds, the draw at 0.88), cut dead at the release.

Every noise source is seeded, so a rebuild writes identical bytes. Not heard by the owner yet: provisional.

Run: python tools/build_amihan_ult_audio.py
"""
import json

import numpy as np

from build_amihan_audio import (RATE, ROOT, band, env_ar, finish, flute_note, one_pole_low, svf, sweep, thump, times,
                                white)

GATHER = 1.5  # AmihanRules.StormSurgeGatherSeconds


# ------------------------------------------------------------------ her loom

def beater(t, at, weight=1.0, seed=0):
    """THE BATTEN: a struck hardwood beam. Three decaying wood modes, a dry click and a low knock of the frame."""
    local = np.maximum(0, t - at)
    on = t >= at
    modes = sum(np.sin(2 * np.pi * f * local) * np.exp(-local / d) * g
                for f, d, g in [(212, .060, .9), (517, .034, .55), (1093, .018, .35), (1871, .010, .2)])
    click = svf(white(len(t), 12100 + seed), 2600, 1.4) * np.exp(-local / .006) * 1.1
    frame = thump(t, at, 78, .07) * .8
    return (modes + click) * on * weight + frame * weight


def pluck(t, at, pitch, length=.9, seed=0, bright=.5):
    """A PLUCKED WARP THREAD: Karplus-Strong on a short seeded noise burst, damped toward a cotton thread's dullness."""
    out = np.zeros(len(t))
    start = int(round(at * RATE))
    if start >= len(t):
        return out
    n = min(len(t) - start, int(length * RATE))
    period = max(2, int(round(RATE / pitch)))
    buf = np.random.default_rng(12200 + seed).uniform(-1, 1, period)
    y = np.empty(n)
    keep = .5 + .5 * bright
    for i in range(n):
        v = buf[i % period]
        y[i] = v
        buf[i % period] = .996 * (keep * v + (1 - keep) * buf[(i + 1) % period])
    out[start:start + n] = y * np.clip(np.arange(n) / (.002 * RATE), 0, 1)
    return out


def cloth_snap(t, at, seed, weight=1.0):
    """A fabric snap: a sharp high crack of cloth pulled taut, with a tiny flutter after it."""
    local = np.maximum(0, t - at)
    on = t >= at
    crack = svf(white(len(t), seed), 3600, 1.6, "high") * np.exp(-local / .010) * 1.4
    flutter = svf(white(len(t), seed + 1), 2400, 2.2) * (.5 + .5 * np.sin(2 * np.pi * 34 * local)) ** 3 * np.exp(-local / .06) * .6
    return (crack + flutter) * on * weight


def breath_in(t, start, length, seed, weight=1.0):
    """An inhale: a band rising under a swelling level, stopped dead at its end."""
    local = t - start
    on = (local >= 0) & (local < length)
    u = np.clip(local / length, 0, 1)
    return band(white(len(t), seed), 500 + 1700 * u ** 1.3, 2.0) * u ** 2.0 * on * weight


def window(t, start, end, rise=.02, fall=.02):
    return np.clip((t - start) / rise, 0, 1) * np.clip((end - t) / fall, 0, 1)


# ------------------------------------------------------------------ the four cues

def theme():
    """HER THEME, 5.6 s, the beat sheet beat for beat (CALL 0 to 1.7, WEAVE 1.7 to 3.7, WARP 3.7 to 5.6)."""
    s = 5.6
    t = times(s)
    n = len(t)
    # 0.00 the cut-in: a cloth snap and a breath of wind already moving.
    cut = cloth_snap(t, .0, 13001, 2.6) + thump(t, .0, 84, .06) * 1.2 + band(white(n, 13002), sweep(t, 700, 380, .6), 1.4) * env_ar(t, .03, .35, .06) * 3.0
    # 0.25 the flute's first note, on the call out. 0.90 to 1.60 its phrase over the swell (D E G, then A held).
    notes = [(.25, .32, 587.3), (.92, .22, 659.3), (1.16, .24, 784.0), (1.42, .30, 880.0)]
    flute = sum(flute_note(t, a, l, p, 13100 + i) for i, (a, l, p) in enumerate(notes)) * 2.4
    # 0.55 THE MONSOON ANSWERS: a wind swell rising INTO her from 0.30, cut on the snap of her hand to her chest; then the
    # swell it left carries under the phrase until the weave.
    rise = band(white(n, 13201), sweep(t, 300, 1500, .55, 1.6), 1.3) * np.clip((t - .30) / .25, 0, 1) ** 2 * (t < .55) * 6.5
    swell = band(white(n, 13202), 600 + 260 * np.sin(2 * np.pi * .9 * t), 1.2) * window(t, .55, 1.75, .02, .15) * (1.2 + 2.6 * np.exp(-np.maximum(0, t - .55) / .35)) * 1.6
    answer = thump(t, .55, 70, .14) * 2.0 + cloth_snap(t, .55, 13203, 1.8)
    # 1.70 the breath drawn in, into the cup.
    inhale = breath_in(t, 1.70, .45, 13301, 1.6)
    # 2.15 THE LOOM KNOCK and the motif's three notes (D, A, G); 2.50 and 2.95 knock, knock; the drone steps up each time.
    knocks = (beater(t, 2.15, 1.2, 1) + beater(t, 2.50, .9, 2) + beater(t, 2.95, 1.0, 3)) * 2.6
    motif = sum(flute_note(t, a, l, p, 13400 + i) for i, (a, l, p) in enumerate([(2.17, .16, 587.3), (2.33, .14, 880.0), (2.47, .22, 784.0)])) * 1.8
    step = np.where(t < 2.50, 0, np.where(t < 2.95, 1, 2))
    root = 98.0 * 2 ** (step * 2 / 12)
    drone_on = window(t, 2.15, 3.72, .08, .03)
    phase = 2 * np.pi * np.cumsum(root) / RATE
    drone = (np.sin(phase) + .5 * np.sin(2 * phase * 1.003) + .25 * np.sin(3 * phase)) * drone_on * .32
    weave_air = band(white(n, 13501), 900, 1.1) * window(t, 2.15, 3.30, .05, .1) * .7
    # 3.30 a held breath: everything quiet but the drone (the drone swells a little into the flick).
    hold = drone * np.clip((t - 3.30) / .4, 0, 1) * .5
    # 3.70 THE WARP: a plucked run outward, nine notes up her pentatonic, a speed whoosh down the lane under it.
    run = [587.3, 659.3, 784.0, 880.0, 987.8, 1174.7, 1318.5, 1568.0, 1760.0]
    strings = sum(pluck(t, 3.72 + i * .045, p, .9, i, .55 - .03 * i) * (1 - .05 * i) for i, p in enumerate(run)) * 2.4
    whoosh = band(white(n, 13601), sweep(np.maximum(0, t - 3.72), 2400, 380, .6, .7), 1.2) * env_ar(np.maximum(0, t - 3.72), .03, .25, .05) * (t >= 3.72) * 4.0
    # 4.10 to 5.20 low wind pressure over the lane, and the motif once more, low; 5.20 to 5.60 cut dead into the live gather.
    pressure = svf(white(n, 13701), sweep(np.maximum(0, t - 4.0), 300, 900, 1.2), .8, "low") * window(t, 4.00, 5.58, .25, .02) * .75
    rumble = one_pole_low(white(n, 13702), 80) * window(t, 4.05, 5.58, .3, .02) * .7
    last = sum(flute_note(t, a, l, p, 13800 + i) for i, (a, l, p) in enumerate([(4.40, .22, 293.7), (4.66, .22, 440.0), (4.92, .32, 392.0)])) * 1.4
    mix = cut + flute + rise + swell + answer + inhale + knocks + motif + drone + weave_air + hold + strings + whoosh + pressure + rumble + last
    return finish("sfx_ult_theme_amihan", mix, s, .66)


def cast():
    """THE PRESS: a cloth snap as she plants, and a quick inhale."""
    s = .55
    t = times(s)
    n = len(t)
    snap = cloth_snap(t, .0, 14001, 1.0) + thump(t, .0, 90, .05) * .5
    inhale = breath_in(t, .06, .40, 14002, 1.8)
    return finish("sfx_cast_amihan_storm", snap + inhale, s, .7)


def gather():
    """THE LIVE 1.5 s: pressure rising from the first frame, loom knocks on the pack beats (0.5, 1.0), the held breath at
    1.32 with everything but the drone dropped, and cut dead at the release (1.5)."""
    s = GATHER
    t = times(s)
    n = len(t)
    beat1, beat2, draw = GATHER / 3, GATHER * 2 / 3, GATHER * .88
    pressure = svf(white(n, 15001), sweep(t, 420, 3200, GATHER, 1.2), .8, "low") * (.5 + .5 * np.clip(t / GATHER, 0, 1)) * 1.8
    pressure *= np.where(t < draw, 1.0, .25)
    rumble = one_pole_low(white(n, 15002), 90) * np.clip(t / .2, 0, 1) * 2.2 * np.where(t < draw, 1.0, .5)
    step = np.where(t < beat1, 0, np.where(t < beat2, 1, 2))
    phase = 2 * np.pi * np.cumsum(98.0 * 2 ** (step * 2 / 12)) / RATE
    drone = (np.sin(phase) + .4 * np.sin(2 * phase * 1.003)) * .22 * (.6 + .4 * np.clip(t / GATHER, 0, 1))
    knocks = beater(t, beat1, .9, 11) + beater(t, beat2, 1.0, 12)
    whistle = band(white(n, 15003), sweep(t, 900, 1500, GATHER, 2.0), 14.0) * np.clip((t - .6) / .7, 0, 1) ** 2 * (t < draw) * 2.0
    held = breath_in(t, draw - .02, GATHER - draw + .02, 15004, .9)
    return finish("sfx_amihan_storm_gather", pressure + rumble + drone + knocks + whistle + held, s, .62)


def release():
    """THE BEATER: the batten's crack and a low thud, the whoosh going away down the lane, a fabric snap, a cotton tail."""
    s = 2.0
    t = times(s)
    n = len(t)
    crack = beater(t, .0, 1.3, 21) + svf(white(n, 16001), 2200, .9, "high") * np.exp(-t / .016) * 1.4
    sub = thump(t, .0, 46, .32) * 1.3
    away = band(white(n, 16002), sweep(t, 1400, 160, 1.4, .6), 1.1) * env_ar(t, .015, .45, .05) * 6.0
    lane = band(white(n, 16003), sweep(t, 5200, 1800, 1.2), 1.3) * env_ar(t, .02, .35, .08) * 1.0
    snap = cloth_snap(t, .05, 16004, .9)
    cotton = svf(white(n, 16005), 6400, 1.2) * np.clip((t - .3) / .2, 0, 1) * np.exp(-np.maximum(0, t - .5) / .45) * .35
    return finish("sfx_amihan_storm_release", crack + sub + away + lane + snap + cotton, s, .8)


if __name__ == "__main__":
    rows = [theme(), cast(), gather(), release()]
    report = {
        "provenance": "Original deterministic synthesis (numpy only); no external samples, voices or paid API.",
        "listening": "Not yet heard by the owner in the game mix. Peak and RMS are measurements, not approval.",
        "cues": rows,
    }
    out = ROOT / "docs/reports/amihan-presentation-2026-10-02/ult-audio.json"
    out.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"Authored {len(rows)} Airburst cues; nothing else touched.")
