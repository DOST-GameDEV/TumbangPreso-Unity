"""AIRBURST's two sounds and FEATHERFALL's three: original, deterministic, numpy only. Writes ONLY these files.

Owner, 2026-10-03, on the feel-pass films: "throoughly revamp her cutscene ... as well as the vfx sfx an of her ult".
Every hero's skill sounds were deleted on 2026-09-29 (`AudioCues.IsSkillSfx`); these four are the first released from that
switch (`AudioCues.ReworkedSkillSfx`). Direction: docs/reports/amihan-presentation-2026-10-02/airburst-v3.md, "Sound".

Her family of instruments: AIR through moving filters (the 2026-09-25 recipes in `build_amihan_audio.py`, whose helpers this
reuses), a BAMBOO FLUTE for her theme, and the LOOM: a struck wooden beater (the batten knocking the weft home) and a plucked
warp thread (Karplus-Strong). The theme is timed beat for beat to the v6 5.6 s cutscene (a whistle, foot taps, the whirlwind, the hang); the gather to the live 1.5 s windup
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

def whistle(t, at, length, seed, weight=1.0):
    """A TWO-FINGER WHISTLE: a bright sine that scoops up into its note with a little vibrato, breath noise riding it."""
    local = t - at
    on = (local >= 0) & (local < length)
    u = np.clip(local / length, 0, 1)
    f = 2350 + 650 * (1 - np.exp(-np.maximum(0, local) / .035)) + 40 * np.sin(2 * np.pi * 7 * np.maximum(0, local))
    f = f - 500 * np.clip((u - .78) / .22, 0, 1) ** 2
    phase = 2 * np.pi * np.cumsum(np.where(on, f, 0)) / RATE
    level = np.clip(local / .012, 0, 1) * np.clip((length - local) / .05, 0, 1)
    tone = np.sin(phase) + .18 * np.sin(2 * phase)
    breath = band(white(len(t), seed), f, 6.0) * .35
    return (tone + breath) * level * on * weight


def tap(t, at, seed, weight=1.0):
    """A slipper tapping the cobbles: a short dry knock and a click."""
    local = np.maximum(0, t - at)
    on = t >= at
    knock = np.sin(2 * np.pi * 190 * local) * np.exp(-local / .018)
    click = svf(white(len(t), seed), 3000, 1.5) * np.exp(-local / .004) * .8
    return (knock + click) * on * weight


def theme():
    """HER THEME, 5.6 s, v6 beat for beat (`HeroIntroductionScene.Amihan.cs`). BORED 0: near silence, a lazy two-note
    flute, the lone leaf ticking down at 1.70, two taps 1.25 and 1.38, a shrug 1.52. THE WHISTLE 1.78; the wind answers
    1.92, racing in to her palm (2.34 to 2.63); the whirlwind spinning from 2.10; her phrase as she holds it up 2.42; a toss
    3.02; the look back 3.30 (the cheeky motif). THE WIND-UP 3.80, a breath held from 4.20; THE RELEASE 4.55; THE HANG to
    5.17, the world slowed: one low stretched whoom under a high held flute; the rush returns; the finish 5.34."""
    s = 5.6
    t = times(s)
    n = len(t)
    # BORED: a faint still courtyard, two slow lazy notes; nothing moves.
    still = one_pole_low(white(n, 13001), 500) * window(t, 0, 1.78, .3, .05) * .25
    lazy = flute_note(t, .12, .42, 587.3, 13100) * 1.2 + flute_note(t, .62, .5, 880.0, 13101) * 1.0
    leaf = pluck(t, 1.70, 1568.0, .3, 7, .3) * .5
    taps = tap(t, 1.25, 13040, 2.0) + tap(t, 1.38, 13041, 2.0)
    rustle = svf(white(n, 13042), 1800, 1.2) * env_ar(np.maximum(0, t - 1.52), .02, .1, .03) * (t >= 1.52) * .6
    # 1.78 THE WHISTLE; 1.92 the wind answers, a swell racing at her, each curl arriving as a small whoosh into her palm.
    call = whistle(t, 1.78, .26, 13050, 2.0)
    answer = band(white(n, 13060), sweep(np.maximum(0, t - 1.90), 280, 1500, .45, 1.4), 1.2) * np.clip((t - 1.90) / .25, 0, 1) ** 2 * window(t, 1.90, 2.75, .02, .3) * 5.0
    curls = sum(band(white(n, 13062 + i), sweep(np.maximum(0, t - a), 2200, 700, .12), 1.6) * env_ar(np.maximum(0, t - a), .01, .1, .03) * (t >= a) * 1.6
                for i, a in enumerate([2.34, 2.42, 2.50, 2.58]))
    # THE WHIRLWIND: air circling on her palm from 2.10, quiet while she shows it off, rising through the wind-up.
    whirl = band(white(n, 13071), 900 + 400 * np.sin(2 * np.pi * 2.2 * (t - 2.10)), 2.0) * window(t, 2.10, 4.55, .2, .02) * (1.0 + 1.2 * np.clip((t - 3.80) / .7, 0, 1))
    # 2.42 holding it up: the loom's soft knock and her phrase, unhurried (D E G A).
    catch = beater(t, 2.42, .8, 1) * 1.6
    phrase = sum(flute_note(t, a, l, p, 13200 + i) for i, (a, l, p) in enumerate([(2.46, .18, 587.3), (2.66, .18, 659.3), (2.86, .18, 784.0), (3.06, .24, 880.0)])) * 2.0
    toss = pluck(t, 3.02, 1174.7, .4, 3, .5) * .9
    # 3.30 THE LOOK BACK: the cheeky motif, plucked threads under it (D A G).
    look = sum(pluck(t, 3.30 + i * .03, p, .7, i, .6) for i, p in enumerate([587.3, 880.0, 1174.7])) * 1.3
    motif = sum(flute_note(t, a, l, p, 13300 + i) for i, (a, l, p) in enumerate([(3.32, .1, 1174.7), (3.44, .1, 1760.0), (3.56, .16, 1568.0)])) * 1.7
    # THE WIND-UP: pressure gathering behind her, the drone rising, a breath drawn in and held before the drive.
    pressure = svf(white(n, 13400), sweep(np.maximum(0, t - 3.80), 300, 1400, .7), .8, "low") * window(t, 3.80, 4.48, .1, .05) * 1.2
    phase = 2 * np.pi * np.cumsum(98.0 * 2 ** (np.clip((t - 3.80) / .7, 0, 1) * 5 / 12)) / RATE
    drone = (np.sin(phase) + .5 * np.sin(2 * phase * 1.003)) * window(t, 3.80, 4.54, .1, .02) * .35
    held = breath_in(t, 4.20, .33, 13410, 1.6)
    # 4.55 THE RELEASE: the batten's crack and the low thud.
    s2 = np.maximum(0, t - 4.55)
    beat = (t >= 4.55)
    crack = beater(t, 4.55, 3.4, 31) + svf(white(n, 13500), 2200, .9, "high") * np.exp(-s2 / .016) * beat * 3.6
    thud = thump(t, 4.55, 46, .3) * 3.0
    # THE HANG (4.65 to 5.17): the world slowed. The rush drops to one low stretched whoom and a held high flute note.
    whoom = band(white(n, 13501), sweep(s2, 420, 120, .6, .8), .9) * window(t, 4.58, 5.20, .05, .1) * 6.0
    hang_note = flute_note(t, 4.66, .48, 1760.0, 13550) * 1.2
    # 5.17 the rush returns at speed and goes away down the lane.
    s3 = np.maximum(0, t - 5.17)
    away = band(white(n, 13502), sweep(s3, 1400, 220, .4, .6), 1.1) * env_ar(s3, .015, .26, .04) * (t >= 5.17) * 8.0
    # 5.34 THE FINISH: one bright high flute note, cheeky, as she sets her hands on her hips.
    finish_note = flute_note(t, 5.34, .14, 1174.7, 13600) * 2.0
    mix = (still + lazy + leaf + taps + rustle + call + answer + curls + whirl + catch + phrase + toss + look + motif
           + pressure + drone + held + crack + thud + whoom + hang_note + away + finish_note)
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


# ------------------------------------------------------------------ FEATHERFALL (owner, 2026-10-03: "u can give it sfx already")

def updraft_cast():
    """THE TAKE-OFF: the court's air thumped down under her, then a rush sweeping UP past her as she rises (0.45 s),
    a light flutter at the top. Air pushing her up, not a jet."""
    s = 1.0
    t = times(s)
    n = len(t)
    push = thump(t, .0, 70, .12) * 1.6 + svf(white(n, 17001), 400, .9, "low") * env_ar(t, .005, .08, .02) * 2.4
    rise = band(white(n, 17002), sweep(t, 300, 2400, .5, 1.3), 1.1) * env_ar(t, .03, .38, .25) * 5.0
    sheen = band(white(n, 17003), sweep(t, 2600, 5200, .5), 1.6) * env_ar(np.maximum(0, t - .12), .05, .25, .2) * (t >= .12) * 1.2
    flutter = band(white(n, 17004), 1300 + 400 * np.sin(2 * np.pi * 14 * t), 2.0) * window(t, .35, .8, .05, .2) * .9
    return finish("sfx_cast_amihan_updraft", push + rise + sheen + flutter, s, .6)


def updraft_gust():
    """ONE GUST of the updraft pressing into her soles (`AmihanHoverRing.GustPeriod`): a soft low puff of air rising
    under her, with a breathy top. Quiet: it repeats about once a second while she flies."""
    s = .55
    t = times(s)
    n = len(t)
    puff = svf(white(n, 17101), sweep(t, 220, 900, .25), .9, "low") * env_ar(t, .06, .14, .25) * 2.4
    breath = band(white(n, 17102), sweep(t, 900, 1700, .3), 1.2) * env_ar(t, .08, .1, .2) * 1.1
    return finish("sfx_amihan_updraft_gust", puff + breath, s, .45)


def updraft_settle():
    """THE LANDING: the air under her letting go, a falling sigh, and her light step on the court."""
    s = .5
    t = times(s)
    n = len(t)
    sigh = band(white(n, 17201), sweep(t, 1600, 300, .35), 1.1) * env_ar(t, .02, .22, .15) * 3.0
    step = tap(t, .05, 17202, 1.0) + thump(t, .05, 80, .06) * .6
    return finish("sfx_amihan_updraft_settle", sigh + step, s, .5)

if __name__ == "__main__":
    # v3.2: the press and the gather have no moment any more (the cutscene shows the windup; play resumes on the hit).
    rows = [theme(), release(), updraft_cast(), updraft_gust(), updraft_settle()]
    report = {
        "provenance": "Original deterministic synthesis (numpy only); no external samples, voices or paid API.",
        "listening": "Not yet heard by the owner in the game mix. Peak and RMS are measurements, not approval.",
        "cues": rows,
    }
    out = ROOT / "docs/reports/amihan-presentation-2026-10-02/ult-audio.json"
    out.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"Authored {len(rows)} Airburst and Featherfall cues; nothing else touched.")
