"""AIRBURST v3's four sounds: original, deterministic, numpy only. Writes ONLY these four files.

Owner, 2026-10-03, on the feel-pass films: "throoughly revamp her cutscene ... as well as the vfx sfx an of her ult".
Every hero's skill sounds were deleted on 2026-09-29 (`AudioCues.IsSkillSfx`); these four are the first released from that
switch (`AudioCues.ReworkedSkillSfx`). Direction: docs/reports/amihan-presentation-2026-10-02/airburst-v3.md, "Sound".

Her family of instruments: AIR through moving filters (the 2026-09-25 recipes in `build_amihan_audio.py`, whose helpers this
reuses), a BAMBOO FLUTE for her theme, and the LOOM: a struck wooden beater (the batten knocking the weft home) and a plucked
warp thread (Karplus-Strong). The theme is timed beat for beat to the v4 4.4 s cutscene (a whistle, foot taps, cloth snaps); the gather to the live 1.5 s windup
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
    """HER THEME, 4.4 s, v4 beat for beat (`HeroIntroductionScene.Amihan.cs`): OPEN 0, the skid .50, the read .74, two taps
    .94 and 1.06, the WHISTLE 1.30, the wind answering 1.42, the tear 1.60, the catch 1.85, the CARD 2.50 torn off 2.86, the
    wind-up 2.95, the RELEASE 3.85, the finish 4.24. Quick, bright and cheeky: her, not a ritual."""
    s = 4.4
    t = times(s)
    n = len(t)
    # OPEN: a breeze in the street, the abel snapping on the line, her running steps; the skid on the cobbles.
    breeze = band(white(n, 13001), 700 + 200 * np.sin(2 * np.pi * 1.3 * t), 1.3) * window(t, 0, .78, .02, .25) * 1.6
    flaps = sum(cloth_snap(t, a, 13010 + i, .9) for i, a in enumerate([.02, .19, .36]))
    steps = sum(tap(t, a, 13020 + i, .9) for i, a in enumerate([.0, .16, .32]))
    s0 = np.maximum(0, t - .50)
    skid = band(white(n, 13030), sweep(s0, 2600, 900, .18), 1.6) * env_ar(s0, .01, .16, .04) * (t >= .50) * 3.2 + tap(t, .50, 13031, 1.4)
    pickup = sum(flute_note(t, a, l, p, 13100 + i) for i, (a, l, p) in enumerate([(.04, .12, 587.3), (.18, .12, 659.3), (.32, .16, 784.0)])) * 1.6
    # READ: the street goes quiet; her two impatient taps; a shrug's rustle.
    taps = tap(t, .94, 13040, 2.2) + tap(t, 1.06, 13041, 2.2)
    rustle = svf(white(n, 13042), 1800, 1.2) * env_ar(np.maximum(0, t - 1.16), .02, .1, .03) * (t >= 1.16) * .6
    # 1.30 THE WHISTLE; 1.42 the street answers, a swell racing at her; 1.60 the abel tears off the line.
    call = whistle(t, 1.30, .26, 13050, 2.0)
    answer = band(white(n, 13060), sweep(np.maximum(0, t - 1.40), 280, 1500, .35, 1.4), 1.2) * np.clip((t - 1.40) / .22, 0, 1) ** 2 * window(t, 1.40, 2.45, .02, .3) * 5.0
    tear = cloth_snap(t, 1.60, 13061, 2.4) + thump(t, 1.60, 90, .05) * .8
    # 1.85 THE CATCH: a snap and the loom's knock; the whirl's air circling; her phrase over it (D E G A).
    catch = cloth_snap(t, 1.85, 13070, 2.0) + beater(t, 1.85, 1.0, 1) * 2.2
    whirl = band(white(n, 13071), 900 + 500 * np.sin(2 * np.pi * 1.6 * (t - 1.85)), 2.0) * window(t, 1.86, 2.48, .05, .1) * 1.8
    phrase = sum(flute_note(t, a, l, p, 13200 + i) for i, (a, l, p) in enumerate([(1.88, .14, 587.3), (2.03, .14, 659.3), (2.18, .14, 784.0), (2.32, .2, 880.0)])) * 2.2
    # 2.50 THE CARD: a knock and the motif struck bright (D A G) over plucked threads; 2.86 the wind rips it off.
    card = beater(t, 2.50, 1.2, 2) * 2.4 + sum(pluck(t, 2.50 + i * .03, p, .8, i, .6) for i, p in enumerate([587.3, 880.0, 1174.7])) * 1.6
    motif = sum(flute_note(t, a, l, p, 13300 + i) for i, (a, l, p) in enumerate([(2.52, .1, 1174.7), (2.63, .1, 1760.0), (2.74, .14, 1568.0)])) * 1.8
    s1 = np.maximum(0, t - 2.86)
    rip = band(white(n, 13310), sweep(s1, 3200, 700, .2), 1.4) * env_ar(s1, .01, .14, .04) * (t >= 2.86) * 5.0 + cloth_snap(t, 2.87, 13311, 1.6)
    # THE WIND-UP: pressure gathering behind her, the drone rising, a breath drawn in, held still before the drive.
    pressure = svf(white(n, 13400), sweep(np.maximum(0, t - 2.95), 300, 1400, .7), .8, "low") * window(t, 2.95, 3.68, .1, .05) * 1.2
    phase = 2 * np.pi * np.cumsum(98.0 * 2 ** (np.clip((t - 2.95) / .7, 0, 1) * 5 / 12)) / RATE
    drone = (np.sin(phase) + .5 * np.sin(2 * phase * 1.003)) * window(t, 2.95, 3.84, .1, .02) * .35
    held = breath_in(t, 3.50, .33, 13410, 1.6)
    # 3.85 THE RELEASE: the batten's crack, the low thud, the whoosh going away down the lane, a fabric snap.
    s2 = np.maximum(0, t - 3.85)
    beat = (t >= 3.85)
    crack = beater(t, 3.85, 3.4, 31) + svf(white(n, 13500), 2200, .9, "high") * np.exp(-s2 / .016) * beat * 3.6
    thud = thump(t, 3.85, 46, .3) * 3.0
    away = band(white(n, 13501), sweep(s2, 1400, 220, .55, .6), 1.1) * env_ar(s2, .015, .3, .04) * beat * 9.0
    snap = cloth_snap(t, 3.89, 13502, 2.0)
    # 4.24 THE FINISH: one bright high flute note, cheeky, as she sets her hands on her hips.
    finish_note = flute_note(t, 4.24, .14, 1174.7, 13600) * 2.0
    mix = (breeze + flaps + steps + skid + pickup + taps + rustle + call + answer + tear + catch + whirl + phrase + card + motif
           + rip + pressure + drone + held + crack + thud + away + snap + finish_note)
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
    # v3.2: the press and the gather have no moment any more (the cutscene shows the windup; play resumes on the hit).
    rows = [theme(), release()]
    report = {
        "provenance": "Original deterministic synthesis (numpy only); no external samples, voices or paid API.",
        "listening": "Not yet heard by the owner in the game mix. Peak and RMS are measurements, not approval.",
        "cues": rows,
    }
    out = ROOT / "docs/reports/amihan-presentation-2026-10-02/ult-audio.json"
    out.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"Authored {len(rows)} Airburst cues; nothing else touched.")
