"""Amihan's sounds: original, deterministic, one recipe per ability. Writes only her files.

Owner, 2026-09-25: "thoroughly try to direct all vfx and sfx of the skills so that it will look
cohesive, good and satisfying", and the direction that came out of it
(docs/reports/amihan-kit-2026-09-25/direction.md section 4): every cue has a TRANSIENT, a BODY in
the ability's own texture and a TAIL that says it is over; the element is a family (air moving),
the ability is its own verb; no two abilities share a recipe.

WIND IS FILTERED NOISE, SO THE FILTER IS THE INSTRUMENT. A Chamberlin state-variable filter with a
centre frequency that MOVES per sample is what gives each cue its shape: a pass sweeps its band
down (a thing going by), a lift sweeps it up (a column rising), a swirl wobbles two bands against
each other (a phaser, the gale turning), a gather opens a low-pass slowly (pressure building).
Only numpy is used (no scipy on this machine), and every noise source is seeded, so a rebuild writes
identical bytes.

Run: python tools/build_amihan_audio.py
"""
import hashlib
import json
import math
from pathlib import Path
import wave

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
RATE = 44100
OUT = ROOT / "Assets/TumbangPreso/Resources/Sfx"


def times(seconds):
    return np.arange(int(round(seconds * RATE))) / RATE


def white(n, seed):
    """⚠️ NOT WHITE ANY MORE (v2). v1 filtered true white noise and its spectrogram was flat
    broadband hiss from 0 to 12 kHz under every cue: a 12 dB/octave band-pass on white noise keeps
    too much of everything, and flat hiss reads as static, not air. Real wind carries its energy
    low, so the source is now a darker "pink-ish" noise (white averaged with its running sum), and
    `band` below cascades two filter stages for 24 dB/octave."""
    raw = np.random.default_rng(seed).normal(0.0, 1.0, n)
    brown = np.cumsum(raw)
    brown -= one_pole_low(brown, 8.0)          # remove the drift, keep the low-heavy tilt
    brown /= max(1e-6, float(np.std(brown)))
    return 0.55 * raw + 0.45 * brown * 2.0


def svf(x, centre, q, mode="band"):
    """Chamberlin state-variable filter; `centre` may be a scalar or a per-sample array (Hz)."""
    n = len(x)
    fc = np.broadcast_to(np.asarray(centre, dtype=float), (n,))
    f = 2.0 * np.sin(np.pi * np.clip(fc, 20.0, RATE / 6.0) / RATE)
    damp = 1.0 / max(0.5, q)
    low = band = 0.0
    out = np.empty(n)
    for i in range(n):
        high = x[i] - low - damp * band
        band += f[i] * high
        low += f[i] * band
        out[i] = band if mode == "band" else low if mode == "low" else high
    return out


def band(x, centre, q):
    """Two band-pass stages in series: 24 dB/octave, so a moving centre is HEARD as a moving pitch."""
    return svf(svf(x, centre, q), centre, q)


def one_pole_low(x, cutoff):
    a = math.exp(-2.0 * math.pi * cutoff / RATE)
    y = np.empty_like(x)
    prev = 0.0
    for i, v in enumerate(x):
        prev = (1 - a) * v + a * prev
        y[i] = prev
    return y


def env_ar(t, attack, release, peak_at=None):
    """Rise over `attack`, then an exponential release."""
    peak_at = attack if peak_at is None else peak_at
    rise = np.clip(t / max(attack, 1e-4), 0, 1) ** 1.5
    fall = np.exp(-np.maximum(0, t - peak_at) / max(release, 1e-4))
    return np.where(t < peak_at, rise, fall)


def sweep(t, start, end, seconds, curve=1.0):
    u = np.clip(t / seconds, 0, 1) ** curve
    return start * (end / start) ** u


def thump(t, at, pitch, decay):
    local = np.maximum(0, t - at)
    return np.sin(2 * np.pi * pitch * local * (1 - 0.35 * local)) * np.exp(-local / decay) * (t >= at)


def edges(signal, seconds, fade_in=0.004, fade_out=0.04):
    t = times(seconds)
    return signal * np.minimum(1, t / fade_in) * np.minimum(1, (seconds - t) / fade_out)


def finish(name, signal, seconds, peak=0.7):
    signal = edges(signal, seconds)
    top = float(np.max(np.abs(signal)))
    signal = signal * (peak / max(1e-5, top))
    path = OUT / f"{name}.wav"
    with wave.open(str(path), "wb") as out:
        out.setnchannels(1)
        out.setsampwidth(2)
        out.setframerate(RATE)
        out.writeframes((np.clip(signal, -1, 1) * 32767).astype("<i2").tobytes())
    return {"cue": name, "seconds": seconds, "peak": float(np.max(np.abs(signal))),
            "rms": float(np.sqrt(np.mean(signal ** 2))), "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}


# ------------------------------------------------------------------ the recipes

def dash():
    """QUICK DASH: a cloth snap, then a band sweeping DOWN fast: something passing you."""
    s = 0.62
    t = times(s)
    n = len(t)
    snap = svf(white(n, 9101), 3200, 2.0, "high") * np.exp(-t / 0.012) * 1.4
    body = band(white(n, 9102), sweep(t, 2600, 420, 0.42, 0.8), 3.2) * env_ar(t, 0.05, 0.13, 0.08) * 5.0
    air = band(white(n, 9103), sweep(t, 900, 260, 0.5), 1.6) * env_ar(t, 0.08, 0.18, 0.12) * 1.6
    kick = thump(t, 0.0, 96, 0.05) * 0.45
    return finish("sfx_cast_amihan_dash", snap + body + air + kick, s)


def updraft():
    """UPDRAFT: a rumble of air under her, a band sweeping UP, and cloth fluttering in it."""
    s = 1.0
    t = times(s)
    n = len(t)
    column = band(white(n, 9201), sweep(t, 260, 1900, 0.7, 0.8), 2.6) * env_ar(t, 0.32, 0.25, 0.5) * 4.0
    low = one_pole_low(white(n, 9202), 140) * env_ar(t, 0.12, 0.35, 0.2) * 5.0
    flutter_rate = 18 - 9 * np.clip(t / s, 0, 1)
    flutter = 0.5 + 0.5 * np.sin(2 * np.pi * np.cumsum(flutter_rate) / RATE)
    cloth = svf(white(n, 9203), 3400, 1.8) * flutter ** 3 * env_ar(t, 0.15, 0.4, 0.35) * 1.1
    return finish("sfx_cast_amihan_updraft", column + low + cloth, s)


def settle():
    """UPDRAFT's landing: the column letting go, a soft sweep down and a sandal tap."""
    s = 0.5
    t = times(s)
    n = len(t)
    fall = band(white(n, 9301), sweep(t, 1300, 300, 0.4), 2.2) * env_ar(t, 0.06, 0.14, 0.1) * 1.6
    tap = svf(white(n, 9302), 1800, 3.0) * np.exp(-np.maximum(0, t - 0.3) / 0.02) * (t >= 0.3) * 1.2
    return finish("sfx_amihan_updraft_settle", fall + tap + thump(t, 0.3, 120, 0.04) * 0.3, s, 0.6)


def whirlwind():
    """WHIRLWIND: two bands wobbling against each other (the gale turning) under a rolling rush."""
    s = 1.35
    t = times(s)
    n = len(t)
    swirl = 900 + 520 * np.sin(2 * np.pi * 4.2 * t)
    counter = 1500 - 600 * np.sin(2 * np.pi * 4.2 * t + 0.6)
    shape = env_ar(t, 0.14, 0.5, 0.55)
    a = band(white(n, 9401), swirl, 4.5) * shape * 4.0
    b = band(white(n, 9402), counter, 4.0) * shape * 3.0
    rush = svf(white(n, 9403), sweep(t, 700, 380, s), 0.9) * shape * 1.3
    roll = one_pole_low(white(n, 9404), 110) * shape * 4.0
    fray = svf(white(n, 9405), 5200, 1.5) * np.clip((t - 0.85) / 0.35, 0, 1) * np.exp(-np.maximum(0, t - 1.1) / 0.15) * 0.7
    return finish("sfx_cast_amihan_whirlwind", a + b + rush + roll + fray, s)


def storm_press():
    """STORM SURGE's press: she draws a breath of air in and plants; the stamp ends it."""
    s = 0.7
    t = times(s)
    n = len(t)
    inhale = band(white(n, 9501), sweep(t, 500, 2200, 0.45, 1.4), 2.0) * np.clip(t / 0.45, 0, 1) ** 2.2 * (t < 0.46) * 1.8
    stamp = thump(t, 0.46, 62, 0.12) * 1.1 + svf(white(n, 9502), 900, 1.2) * np.exp(-np.maximum(0, t - 0.46) / 0.05) * (t >= 0.46) * 1.2
    return finish("sfx_cast_amihan_storm", inhale + stamp, s)


def storm_gather():
    """STORM SURGE's 2.5 s: pressure. A low-pass opening slowly, a beating drone, a whistle late."""
    s = 2.6
    t = times(s)
    n = len(t)
    # v3: audible from the first frame (a 2.5 s warning nobody hears for a second is not a
    # warning); v2's spectrogram showed the first 1.2 s near -70 dB.
    cutoff = sweep(t, 420, 4200, 2.5, 1.3)
    pressure = svf(white(n, 9601), cutoff, 0.8, "low") * (0.45 + 0.55 * np.clip(t / 2.5, 0, 1) ** 1.2) * 1.8
    rumble = one_pole_low(white(n, 9603), 90) * np.clip(t / 0.25, 0, 1) * 2.4
    drone = (np.sin(2 * np.pi * 110 * t) + np.sin(2 * np.pi * 113.4 * t)) * 0.10 * np.clip(t / 2.4, 0, 1) ** 1.5
    whistle = band(white(n, 9602), sweep(t, 900, 1750, 2.5, 2.2), 16.0) * np.clip((t - 1.2) / 1.3, 0, 1) ** 2 * 2.6
    return finish("sfx_amihan_storm_gather", pressure + rumble + drone + whistle, s, 0.62)


def storm_release():
    """STORM SURGE's release: a crack and a sub hit, then a WALL of air going away down the fan."""
    s = 1.9
    t = times(s)
    n = len(t)
    crack = svf(white(n, 9701), 2400, 0.9, "high") * np.exp(-t / 0.018) * 1.6
    sub = thump(t, 0.0, 44, 0.35) * 1.2
    wall = band(white(n, 9702), sweep(t, 520, 140, 1.6, 0.7), 1.1) * env_ar(t, 0.02, 0.42, 0.08) * 6.0
    hiss = band(white(n, 9703), sweep(t, 6000, 2500, 1.8), 1.4) * env_ar(t, 0.02, 0.5, 0.15) * 0.8
    return finish("sfx_amihan_storm_release", crack + sub + wall + hiss, s, 0.8)


def whirled():
    """WHIRLED (status): a short spinning whistle round the victim."""
    s = 0.55
    t = times(s)
    n = len(t)
    centre = 1350 + 420 * np.sin(2 * np.pi * 11 * t) * (1 - t / s)
    spin = band(white(n, 9801), centre, 14.0) * env_ar(t, 0.03, 0.18, 0.08) * 7.0
    puff = svf(white(n, 9802), 700, 1.2) * np.exp(-t / 0.05) * 0.9
    return finish("sfx_status_whirled", spin + puff, s, 0.6)


def chilled():
    """CHILLED (status): a crisp frost tick: three quick high pings and a crackle."""
    s = 0.4
    t = times(s)
    n = len(t)
    pings = sum(np.sin(2 * np.pi * f * np.maximum(0, t - at)) * np.exp(-np.maximum(0, t - at) / 0.05) * (t >= at) * g
                for at, f, g in [(0.0, 3900, 0.5), (0.05, 4700, 0.35), (0.11, 3400, 0.25)])
    crackle = svf(white(n, 9901), 6500, 2.0) * (np.random.default_rng(9902).random(n) > 0.985) * np.exp(-t / 0.12) * 2.0
    return finish("sfx_status_chilled", pings + crackle, s, 0.55)


def flute_note(t, start, length, pitch, breath_seed):
    """A breathy bamboo-flute tone: a vibrato'd fundamental, a soft octave, and breath noise."""
    local = t - start
    on = (local >= 0) & (local < length)
    shape = np.clip(local / 0.06, 0, 1) * np.clip((length - local) / 0.12, 0, 1) * on
    vibrato = 1 + 0.006 * np.sin(2 * np.pi * 5.2 * np.maximum(0, local)) * np.clip(local / 0.3, 0, 1)
    phase = 2 * np.pi * pitch * np.cumsum(vibrato) / RATE
    tone = np.sin(phase) + 0.22 * np.sin(2 * phase) + 0.08 * np.sin(3 * phase)
    breath = band(white(len(t), breath_seed), pitch * 2.0, 6.0) * 0.5
    return (tone + breath) * shape


def theme():
    """HER THEME, under the introduction (3.6 s): a bamboo flute over a rising wind.

    The Ilocos coast has its own bamboo and nose flutes; this is an ORIGINAL line in that
    register, a rising pentatonic call (D, E, G, A, D) that climbs as the storm gathers and holds
    its top note into the release. No sample, no borrowed melody."""
    s = 3.6
    t = times(s)
    n = len(t)
    notes = [(0.25, 0.45, 587.3), (0.72, 0.30, 659.3), (1.05, 0.55, 784.0),
             (1.70, 0.30, 880.0), (2.05, 0.30, 784.0), (2.40, 1.05, 1174.7)]
    # v3: the flute LEADS and the wind sits under it; v2's loudness curve was all wind.
    flute = sum(flute_note(t, a, l, p, 9950 + i) for i, (a, l, p) in enumerate(notes)) * 3.2
    wind = band(white(n, 9960), sweep(t, 300, 1600, 3.2, 1.4), 1.6) * (0.12 + 0.88 * np.clip(t / 3.0, 0, 1) ** 1.8) * 0.8
    low = one_pole_low(white(n, 9961), 90) * np.clip(t / 3.2, 0, 1) ** 2 * 3.0
    return finish("sfx_ult_theme_amihan", flute + wind + low, s, 0.66)


def sky():
    """The Monsoon sky: long gusts that swell and fall, clear rather than heavy."""
    s = 3.2
    t = times(s)
    n = len(t)
    gust = 0.55 + 0.45 * np.sin(2 * np.pi * 0.7 * t) ** 2
    bed = band(white(n, 9970), 600 + 350 * np.sin(2 * np.pi * 0.35 * t), 1.4) * gust * np.clip(t / 0.6, 0, 1) * 1.4
    high = svf(white(n, 9971), 3000, 1.2) * gust ** 2 * 0.4
    return finish("sfx_sky_monsoon", bed + high, s, 0.5)


if __name__ == "__main__":
    rows = [dash(), updraft(), settle(), whirlwind(), storm_press(), storm_gather(),
            storm_release(), whirled(), chilled(), theme(), sky()]
    report = {
        "provenance": "Original deterministic synthesis (numpy only); no external samples, voices or paid API.",
        "listening": "Not yet heard by the owner in the game mix. Peak and RMS are measurements, not approval.",
        "cues": rows,
    }
    out = ROOT / "docs/reports/amihan-kit-2026-09-25/amihan-audio.json"
    out.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"Authored {len(rows)} Amihan cues; nothing else touched.")
