"""AIRBURST's two sounds and FEATHERFALL's three: original, deterministic, numpy only. Writes ONLY these files.

Owner, 2026-10-03, on the feel-pass films: "throoughly revamp her cutscene ... as well as the vfx sfx an of her ult".
Every hero's skill sounds were deleted on 2026-09-29 (`AudioCues.IsSkillSfx`); these four are the first released from that
switch (`AudioCues.ReworkedSkillSfx`). Direction: docs/reports/amihan-presentation-2026-10-02/airburst-v3.md, "Sound".

Her family of instruments: AIR through moving filters (the 2026-09-25 recipes in `build_amihan_audio.py`, whose helpers this
reuses), a BAMBOO FLUTE for her theme, and the LOOM: a struck wooden beater (the batten knocking the weft home) and a plucked
warp thread (Karplus-Strong). The theme is timed beat for beat to the v7 5.6 s cutscene (the whistle, the monsoon, the bird's cry and wings, the hang); the gather to the live 1.5 s windup
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


def chirp(t, at, f0, f1, length, weight=1.0):
    """A small distant songbird: a sine gliding f0 to f1 with a quick flutter, very short."""
    local = np.maximum(0, t - at)
    on = (t >= at) & (local < length)
    f = f0 + (f1 - f0) * np.clip(local / length, 0, 1)
    ph = 2 * np.pi * np.cumsum(np.where(on, f, 0)) / RATE
    return np.sin(ph) * np.sin(np.pi * np.clip(local / length, 0, 1)) ** 2 * on * (1 + .3 * np.sin(2 * np.pi * 38 * local)) * weight


def gust(t, at, length, f0, f1, seed, weight=1.0):
    """One gust passing: band noise sweeping f0 to f1, swelling and dying."""
    local = np.maximum(0, t - at)
    shape = np.sin(np.pi * np.clip(local / length, 0, 1)) ** 1.5 * (t >= at)
    return band(white(len(t), seed), sweep(local, f0, f1, length), 1.4) * shape * weight


def rustle(t, at, length, seed, weight=1.0):
    """Leaves: a dry crackle, many tiny clicks in a band."""
    n = len(t)
    clicks = (white(n, seed) > 2.6).astype(float) * white(n, seed + 1)
    return svf(clicks, 3800, 1.1) * window(t, at, at + length, .08, .2) * weight


def cry(t, at, seed, weight=1.0):
    """THE BIRD'S CRY: a high raptor-like call, a bright glide down with a rasp and a quick vibrato, twice, then an echo."""
    out = np.zeros(len(t))
    for k, (a, l, g) in enumerate([(at, .34, 1.0), (at + .38, .26, .7), (at + .78, .3, .25)]):
        local = np.maximum(0, t - a)
        on = (t >= a) & (local < l)
        f = 2300 - 900 * np.clip(local / l, 0, 1) ** .7 + 60 * np.sin(2 * np.pi * 22 * local)
        ph = 2 * np.pi * np.cumsum(np.where(on, f, 0)) / RATE
        env = np.clip(local / .02, 0, 1) * np.exp(-local / (l * .55)) * on
        tone = np.sin(ph) + .35 * np.sin(2 * ph) + .15 * np.sin(3 * ph)
        rasp = band(white(len(t), seed + k), 2000, 3.0) * .5
        out += (tone + rasp) * env * g
    return out * weight


def wingbeat(t, at, seed, weight=1.0):
    """One great wingbeat: a low pushed thump and a broad whoosh of air."""
    local = np.maximum(0, t - at)
    whoosh = band(white(len(t), seed), sweep(local, 300, 900, .25), 1.0) * env_ar(local, .04, .2, .1) * (t >= at) * 3.0
    return (thump(t, at, 62, .14) * 1.6 + whoosh) * weight


def theme():
    """HER THEME, 5.6 s, v7 beat for beat (`HeroIntroductionScene.Amihan.cs`). STILL 0: the quiet plaza, distant songbirds,
    two lazy notes, a soft shimmer as she floats (0.6 to 1.24), the lone leaf's tick 1.70, taps 1.25 and 1.38, a shrug 1.52.
    THE WHISTLE 1.78. THE MONSOON 1.92 to 2.95: a low rumble rising, gusts passing faster and faster, leaves rustling, the wind
    howling up through the gaps. THE BIRD 2.75: it forms (a shimmering swell), CRIES at 2.90, its wings beat through the swoop,
    the close pass by the lens at 3.18 (a fly-by), her grin's motif. THE WIND-UP 3.80: one huge wingbeat as it raises its
    wings, a deep in-draw of air, a held breath, a hair of silence. THE RELEASE 4.55: the crack and the wingbeat boom. THE
    HANG to 5.17: the world muffled and slowed, a heartbeat, one high held note. The rush returns; the finish 5.34."""
    s = 5.6
    t = times(s)
    n = len(t)
    # STILL: a faint plaza, two songbirds far off, two slow lazy notes, the float's shimmer, the leaf, taps, a shrug.
    still = one_pole_low(white(n, 13001), 500) * window(t, 0, 1.8, .3, .05) * .25
    birds = (chirp(t, .18, 3200, 4100, .09, .25) + chirp(t, .31, 3400, 4300, .07, .2) + chirp(t, .96, 2900, 3800, .1, .18)
             + chirp(t, 1.08, 3100, 4000, .08, .15))
    lazy = flute_note(t, .12, .42, 587.3, 13100) * 1.1 + flute_note(t, .62, .5, 880.0, 13101) * .9
    shimmer = band(white(n, 13002), 5200 + 800 * np.sin(2 * np.pi * 3 * t), 4.0) * window(t, .6, 1.24, .2, .12) * .8
    leaf = pluck(t, 1.70, 1568.0, .3, 7, .3) * .5
    taps = tap(t, 1.25, 13040, 2.0) + tap(t, 1.38, 13041, 2.0)
    shrug = svf(white(n, 13042), 1800, 1.2) * env_ar(np.maximum(0, t - 1.52), .02, .1, .03) * (t >= 1.52) * .6
    # 1.78 THE WHISTLE.
    call = whistle(t, 1.78, .26, 13050, 2.2)
    # THE MONSOON: rumble swelling from the ground, gusts passing ever faster, leaves rustling, the howl rising.
    rumble = one_pole_low(white(n, 13060), 110) * np.clip((t - 1.92) / 1.0, 0, 1) ** 1.5 * window(t, 1.92, 3.7, .05, .5) * 4.0
    gusts = sum(gust(t, a, .5, 500 + 300 * (i % 3), 1600 + 200 * (i % 2), 13070 + i, .9 + .25 * i)
                for i, a in enumerate([1.95, 2.18, 2.36, 2.50, 2.61, 2.70, 2.78]))
    leaves = rustle(t, 2.0, 1.6, 13080, 1.4) + rustle(t, 3.0, .6, 13082, .8)
    howl_f = 320 + 700 * np.clip((t - 1.95) / 1.0, 0, 1) ** 1.3 + 40 * np.sin(2 * np.pi * 5 * t)
    howl = band(white(n, 13085), howl_f, 9.0) * window(t, 1.98, 2.95, .25, .2) * 3.2
    # THE BIRD: forms in a shimmering swell, cries, beats its wings through the swoop, roars past the lens.
    forms = band(white(n, 13090), sweep(np.maximum(0, t - 2.65), 1200, 4200, .3), 2.0) * window(t, 2.65, 2.98, .12, .08) * 2.0
    bird = cry(t, 2.90, 13100, 1.6)
    beats = sum(wingbeat(t, a, 13110 + i, .9) for i, a in enumerate([3.02, 3.42, 3.58]))
    s3 = np.maximum(0, t - 3.05)
    flyby = band(white(n, 13120), sweep(s3, 3400, 380, .35, .7), 1.2) * np.exp(-((t - 3.2) / .09) ** 2) * 9.0
    grin = sum(pluck(t, 3.26 + i * .03, p, .7, i, .6) for i, p in enumerate([587.3, 880.0, 1174.7])) * 1.1
    motif = sum(flute_note(t, a, l, p, 13300 + i) for i, (a, l, p) in enumerate([(3.36, .1, 1174.7), (3.48, .1, 1760.0), (3.60, .16, 1568.0)])) * 1.5
    # THE WIND-UP: the wings raised in one huge beat, air drawn in hard, a held breath, silence on the beat before.
    raise_beat = wingbeat(t, 3.82, 13130, 1.6)
    s4 = np.maximum(0, t - 3.90)
    draw = band(white(n, 13140), sweep(s4, 200, 2600, .6, 1.8), 1.0) * np.clip(s4 / .6, 0, 1) ** 2 * window(t, 3.90, 4.50, .05, .02) * 3.5
    phase = 2 * np.pi * np.cumsum(98.0 * 2 ** (np.clip((t - 3.80) / .7, 0, 1) * 7 / 12)) / RATE
    drone = (np.sin(phase) + .5 * np.sin(2 * phase * 1.003)) * window(t, 3.80, 4.51, .1, .02) * .4
    held = breath_in(t, 4.18, .32, 13410, 1.4)
    # 4.55 THE RELEASE: the crack, the wingbeat boom, the sub.
    s2 = np.maximum(0, t - 4.55)
    beat = (t >= 4.55)
    crack = beater(t, 4.55, 3.4, 31) + svf(white(n, 13500), 2200, .9, "high") * np.exp(-s2 / .016) * beat * 3.6
    boom = thump(t, 4.55, 42, .45) * 4.0 + wingbeat(t, 4.56, 13501, 2.2)
    # THE HANG (4.62 to 5.17): muffled and slowed. A low stretched whoom, a heartbeat, one high held flute note.
    whoom = one_pole_low(band(white(n, 13502), sweep(s2, 380, 110, .6, .8), .9), 600) * window(t, 4.6, 5.2, .05, .1) * 7.0
    heart = thump(t, 4.82, 55, .12) * 1.6 + thump(t, 5.02, 55, .12) * 1.1
    hang_note = flute_note(t, 4.66, .48, 1760.0, 13550) * 1.1
    # 5.17 the rush returns at speed and goes away down the lane; a far cry as the bird goes.
    s5 = np.maximum(0, t - 5.17)
    away = band(white(n, 13503), sweep(s5, 1400, 220, .4, .6), 1.1) * env_ar(s5, .015, .26, .04) * (t >= 5.17) * 8.0
    far_cry = one_pole_low(cry(t, 5.22, 13560, 1.0), 2500) * .35
    # 5.34 THE FINISH: one bright high flute note as she sets her hands on her hips.
    finish_note = flute_note(t, 5.34, .14, 1174.7, 13600) * 2.0
    mix = (still + birds + lazy + shimmer + leaf + taps + shrug + call + rumble + gusts + leaves + howl + forms + bird + beats
           + flyby + grin + motif + raise_beat + draw + drone + held + crack + boom + whoom + heart + hang_note + away + far_cry
           + finish_note)
    return finish("sfx_ult_theme_amihan", mix, s, .7)

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
