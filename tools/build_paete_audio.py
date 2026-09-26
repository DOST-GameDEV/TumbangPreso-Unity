"""Paete's sounds: original, deterministic, one recipe per ability. Writes only his files.

The baseline is docs/reports/amihan-kit-2026-09-25/direction.md section 4: every cue has a
TRANSIENT, a BODY in the ability's own texture and a TAIL that says it is over; the element is a
family, the ability is its own verb, and no two abilities share a recipe. Plan: the Paete kit
plan (docs/reports/paete-kit-2026-09-25/plan.md) section 3, "wood creak, bark snap, leaf rush,
seed pop, thorn rasp, root groan".

AMIHAN'S FAMILY IS AIR, SO HERS IS FILTERED NOISE. HIS IS WOOD AND LEAF, SO THE INSTRUMENTS ARE
DIFFERENT ON PURPOSE, and each one is named where it is defined:
- `creak`: a stick-slip pulse train (irregular period, one impulse per slip) rung through two
  resonant bands. That is how a door hinge or a bending branch actually makes its sound, and it
  is the root groan, the reel and the strain.
- `snap`: a cluster of very short impulses through a bright band: bark and twigs breaking.
- `knock`: the first three modes of a free wooden bar (ratios 1, 2.76, 5.40), a hollow "tok".
  The carved-wood voice of Paete, the carving town.
- `rustle`: sparse random clicks (leaf-on-leaf contacts) through a high band, density shaped by
  an envelope. Grains, not hiss: the v1 lesson in Amihan's `white` is that flat hiss reads as
  static.
- `pluck`: Karplus-Strong, a taut fibre let go: the vine.
- `rasp`: noise chopped by a fast sawtooth gate, a thorn dragged over the ground.

Only numpy is used (no scipy on this machine), and every random source is seeded, so a rebuild
writes identical bytes.

Run: python tools/build_paete_audio.py
"""
import hashlib
import json
import math
import os
from pathlib import Path
import wave

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
RATE = 44100
# PAETE_AUDIO_OUT lets a trial run write somewhere Unity is not watching.
OUT = Path(os.environ.get("PAETE_AUDIO_OUT", ROOT / "Assets/TumbangPreso/Resources/Sfx"))


def times(seconds):
    return np.arange(int(round(seconds * RATE))) / RATE


def noise(n, seed):
    return np.random.default_rng(seed).normal(0.0, 1.0, n)


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


def one_pole_low(x, cutoff):
    a = math.exp(-2.0 * math.pi * cutoff / RATE)
    y = np.empty_like(x)
    prev = 0.0
    for i, v in enumerate(x):
        prev = (1 - a) * v + a * prev
        y[i] = prev
    return y


def env_ar(t, attack, release, peak_at=None):
    peak_at = attack if peak_at is None else peak_at
    rise = np.clip(t / max(attack, 1e-4), 0, 1) ** 1.5
    fall = np.exp(-np.maximum(0, t - peak_at) / max(release, 1e-4))
    return np.where(t < peak_at, rise, fall)


def window(t, start, end, fade=0.02):
    """1 inside [start, end], with short linear fades, 0 outside."""
    return np.clip((t - start) / fade, 0, 1) * np.clip((end - t) / fade, 0, 1)


def sweep(t, start, end, seconds, curve=1.0):
    u = np.clip(t / seconds, 0, 1) ** curve
    return start * (end / start) ** u


def thump(t, at, pitch, decay):
    local = np.maximum(0, t - at)
    return np.sin(2 * np.pi * pitch * local * (1 - 0.35 * local)) * np.exp(-local / decay) * (t >= at)


# ------------------------------------------------------------------ the instruments

def creak(t, seed, rate, spread, centres, q, shape):
    """A stick-slip pulse train: one impulse per slip, the gap between slips wandering by `spread`.

    `rate` (slips per second) may be an array over `t`, which is how a creak rises or sags in pitch.
    Two resonant bands give it the body of wood rather than of a buzzer."""
    n = len(t)
    rng = np.random.default_rng(seed)
    rates = np.broadcast_to(np.asarray(rate, dtype=float), (n,))
    pulses = np.zeros(n)
    i = 0.0
    while i < n:
        k = int(i)
        pulses[k] = 0.6 + 0.4 * rng.random()
        period = RATE / max(5.0, rates[k]) * (1 + spread * (rng.random() - 0.5))
        i += max(8.0, period)
    body = sum(svf(pulses, c, q) * g for c, g in centres)
    return body * shape


def snap(t, seed, at, count, spread, centre, gain=1.0):
    """Bark breaking: `count` very short impulses scattered over `spread` seconds after `at`."""
    n = len(t)
    rng = np.random.default_rng(seed)
    clicks = np.zeros(n)
    for _ in range(count):
        k = int((at + spread * rng.random() ** 1.6) * RATE)
        if 0 <= k < n:
            clicks[k] += (0.5 + rng.random()) * (1 if rng.random() > 0.5 else -1)
    burst = svf(clicks, centre, 1.4) + 0.6 * svf(clicks, centre * 2.3, 2.0)
    return burst * gain


def knock(t, at, pitch, decay, gain=1.0):
    """A hollow wooden tok: the first three modes of a free bar (1, 2.76, 5.40), higher dying faster."""
    local = np.maximum(0, t - at)
    on = t >= at
    modes = [(1.0, 1.0, 1.0), (2.76, 0.45, 0.55), (5.40, 0.22, 0.3)]
    return sum(np.sin(2 * np.pi * pitch * r * local) * g * np.exp(-local / (decay * d))
               for r, g, d in modes) * on * gain


def rustle(t, seed, density, centre, q=1.3):
    """Leaves: sparse contacts whose DENSITY (contacts per second, an array) is the envelope."""
    n = len(t)
    rng = np.random.default_rng(seed)
    dens = np.broadcast_to(np.asarray(density, dtype=float), (n,))
    hits = (rng.random(n) < dens / RATE) * rng.normal(0, 1, n)
    # each contact is a tiny decaying blip, not a single sample
    kernel = np.exp(-np.arange(90) / 18.0) * np.sin(np.arange(90) * 0.9)
    grains = np.convolve(hits, kernel)[:n]
    return svf(grains, centre, q) + 0.4 * svf(grains, centre * 1.9, q)


def pluck(t, at, pitch, seconds, seed, damping=0.996, bend=0.0):
    """Karplus-Strong: a taut fibre let go. `bend` raises the pitch over the note (a vine tightening)."""
    n = len(t)
    out = np.zeros(n)
    start = int(at * RATE)
    length = min(n - start, int(seconds * RATE))
    if length <= 0:
        return out
    rng = np.random.default_rng(seed)
    period = max(2, int(RATE / pitch))
    buf = rng.uniform(-1, 1, period)
    pos = 0
    for i in range(length):
        target = max(2, int(RATE / (pitch * (1 + bend * i / length))))
        if target < len(buf):
            buf = buf[:target]
            pos %= len(buf)
        nxt = (pos + 1) % len(buf)
        v = damping * 0.5 * (buf[pos] + buf[nxt])
        out[start + i] = buf[pos]
        buf[pos] = v
        pos = nxt
    return out


def rasp(t, seed, rate, centre, shape):
    """A thorn dragged over the ground: noise gated by a fast sawtooth, so it scratches in teeth."""
    n = len(t)
    rates = np.broadcast_to(np.asarray(rate, dtype=float), (n,))
    phase = np.cumsum(rates) / RATE
    teeth = (1 - (phase % 1.0)) ** 3
    return svf(noise(n, seed), centre, 2.2) * teeth * shape


def glide(t, at, f0, f1, seconds, gain):
    """A streak of light going up: a soft sine gliding from f0 to f1 over `seconds` from `at`, in and out on a sine window.
    Its own instrument (v6): the streaks, the shafts and the glints are LIGHT, and every wood and leaf voice here is noise or a
    struck bar, so the one pure tone in his world is always his light."""
    s = t - at
    inside = (s >= 0) & (s <= seconds)
    u = np.clip(s / seconds, 0, 1)
    f = f0 * (f1 / f0) ** u
    phase = 2 * np.pi * np.cumsum(np.where(inside, f, 0.0)) / RATE
    return np.sin(phase) * np.sin(np.pi * u) ** 2 * inside * gain


def whoosh(t, seed, at, seconds, f0, f1, gain, attack=0.3):
    """Air moved fast: noise through a band sweeping f0 to f1 over `seconds`, swelling to `attack` of the way and dying after.
    The gust that opens the cutscene, the burst as she forms and the brush stroke at the send."""
    s = t - at
    inside = (s >= 0) & (s <= seconds)
    u = np.clip(s / seconds, 0, 1)
    centre = f0 * (f1 / f0) ** u
    env = np.where(u < attack, (u / max(1e-4, attack)) ** 2, np.clip(1 - (u - attack) / max(1e-4, 1 - attack), 0, 1) ** 1.5) * inside
    return svf(noise(len(t), seed), centre, 1.6) * env * gain


def bell(t, at, pitch, gain):
    """THE MARK'S VOICE (v6): a carved-wood bell, a long low bar tone with its octave a hair sharp (so it beats slowly, like a
    struck temple block ringing) and one bright glint over it. Heard every time the mark is drawn: in the air as the light goes
    into him, on the court as he gives it to the ground, behind the guardian's head as its eyes open. A motif the ear learns."""
    glint = np.sin(2 * np.pi * pitch * 8.0 * t) * env_ar(np.maximum(0, t - at), 0.004, 0.35) * (t >= at) * 0.06
    return (knock(t, at, pitch, 1.4, 1.0) + knock(t, at, pitch * 2.006, 1.0, 0.45) + glint) * gain


def edges(signal, seconds, fade_in=0.003, fade_out=0.05):
    t = times(seconds)
    return signal * np.minimum(1, t / fade_in) * np.minimum(1, (seconds - t) / fade_out)


def finish(name, signal, seconds, peak=0.7):
    signal = edges(signal, seconds)
    signal = signal - np.mean(signal)
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

def vine():
    """KAPIT-BAGING: the 0.12 s tell (leaves shiver), both forearms unravel (two plucked fibres
    pulled taut, pitch rising), the catch (a knock where they bite), then the REEL: a creak whose
    slip rate climbs as he is dragged in, and a leaf rush trailing behind him."""
    s = 1.05
    t = times(s)
    shiver = rustle(t, 7101, 2600 * window(t, 0.0, 0.13, 0.03), 5200) * 0.9
    lash = (pluck(t, 0.12, 196, 0.5, 7102, 0.994, 0.35) + pluck(t, 0.125, 247, 0.5, 7103, 0.994, 0.3)) * 0.8
    whip = svf(noise(len(t), 7104), sweep(t - 0.12, 1800, 5200, 0.1), 2.0) * np.exp(-np.maximum(0, t - 0.12) / 0.03) * (t >= 0.12) * 1.3
    bite = knock(t, 0.30, 330, 0.05, 0.9)
    reel_shape = window(t, 0.32, 0.75, 0.05)
    reel = creak(t, 7105, sweep(t, 60, 210, s), 0.35, [(620, 1.0), (1450, 0.5)], 9.0, reel_shape) * 3.0
    trail = rustle(t, 7106, 1800 * window(t, 0.40, 1.0, 0.12), 3400) * 1.1
    return finish("sfx_cast_paete_vine", shiver + lash + whip + bite + reel + trail, s)


def sprout_cast():
    """PUNLANG TSINELAS's press: the seed's small glow (two soft wood-bar notes a fifth apart)
    and an underhand lob (a short low swish). Deliberately small: the payload is the landing."""
    s = 0.5
    t = times(s)
    glow = knock(t, 0.0, 880, 0.09, 0.35) + knock(t, 0.07, 1320, 0.08, 0.25)
    swish = svf(noise(len(t), 7201), sweep(t - 0.1, 500, 1400, 0.22), 1.6) * window(t, 0.1, 0.34, 0.08) * 0.9
    return finish("sfx_cast_paete_sprout", glow + swish, s, 0.55)


def sprout_land():
    """The seed hits the soil and POPS up into a seedling: a soft soil thud, a rising "bloop" (the
    squash and stretch, heard), and a few leaves settling. Also where a wooden slipper lands."""
    s = 0.55
    t = times(s)
    soil = one_pole_low(noise(len(t), 7301), 300) * np.exp(-t / 0.04) * 4.0 + thump(t, 0.0, 90, 0.05) * 0.7
    pop_t = np.maximum(0, t - 0.05)
    pop = np.sin(2 * np.pi * np.cumsum(sweep(pop_t, 260, 720, 0.09)) / RATE) * env_ar(pop_t, 0.01, 0.05, 0.02) * (t >= 0.05) * 0.8
    leaves = rustle(t, 7302, 1400 * window(t, 0.08, 0.5, 0.1), 4200) * 0.6
    return finish("sfx_paete_sprout_land", soil + pop + leaves, s, 0.6)


def sprout_fire():
    """The seedling fires: the pod head pulls back (a short creak, slips rising), then snaps
    forward: a hollow carved-wood THUNK, and the slipper's swish leaving."""
    s = 0.6
    t = times(s)
    pull = creak(t, 7401, sweep(t, 70, 160, 0.14), 0.3, [(900, 1.0), (2100, 0.4)], 8.0, window(t, 0.0, 0.14, 0.03)) * 2.2
    thunk = knock(t, 0.15, 210, 0.07, 1.2) + thump(t, 0.15, 75, 0.05) * 0.5
    flight = svf(noise(len(t), 7402), sweep(t - 0.16, 2600, 700, 0.3), 2.4) * env_ar(np.maximum(0, t - 0.16), 0.02, 0.1, 0.03) * (t >= 0.16) * 1.1
    return finish("sfx_paete_sprout_fire", pull + thunk + flight, s, 0.66)


def sprout_uproot():
    """Pulled out (owner: "i want the animation for pull out to be good"): a straining groan, the
    roots tearing in a run of snaps, the POP of the root ball leaving the soil, and dirt pattering
    down after it. The one recipe here with a real three-act shape, because the pull is a hold."""
    s = 1.1
    t = times(s)
    strain = creak(t, 7501, sweep(t, 40, 95, 0.35), 0.5, [(300, 1.0), (760, 0.6)], 7.0, window(t, 0.0, 0.36, 0.06)) * 3.0
    tear = snap(t, 7502, 0.28, 34, 0.16, 2600, 2.2)
    pop = thump(t, 0.42, 70, 0.08) * 1.0 + knock(t, 0.42, 150, 0.05, 0.6)
    soil = one_pole_low(noise(len(t), 7503), 700) * np.exp(-np.maximum(0, t - 0.42) / 0.07) * (t >= 0.42) * 3.0
    patter = rustle(t, 7504, 900 * window(t, 0.5, 1.05, 0.12), 1800) * 1.1
    return finish("sfx_paete_sprout_uproot", strain + tear + pop + soil + patter, s, 0.72)


def command():
    """PUNLANG TSINELAS's second press, the COMMAND: he points, the seedling throws. A dry snap
    of twig (a tiny `snap` cluster) and one small high knock, 0.25 s. It sits UNDER
    `sfx_paete_sprout_fire`, which is the real payload, so it is deliberately the smallest cue."""
    s = 0.25
    t = times(s)
    crack = snap(t, 7251, 0.0, 6, 0.012, 3600, 1.6)
    tok = knock(t, 0.01, 1180, 0.025, 0.5)
    return finish("sfx_cast_paete_command", crack + tok, s, 0.45)


def thorns_cast():
    """BAWI's press: he stamps and roots crack out round his feet. A low stamp and a ring of
    bark snaps; the thorns themselves are the next cue."""
    s = 0.5
    t = times(s)
    stamp = thump(t, 0.0, 58, 0.09) * 1.2 + one_pole_low(noise(len(t), 7601), 500) * np.exp(-t / 0.03) * 3.0
    crack = snap(t, 7602, 0.03, 22, 0.14, 1900, 1.8)
    return finish("sfx_cast_paete_thorns", stamp + crack, s, 0.64)


def thorn_burst():
    """The thorn construct bursts up and its vines RACE out to every slipper: a rasp whose teeth
    speed up and whose band sweeps up as the vines travel, three whips as they catch, and the
    dry scrape of the yank home."""
    s = 1.2
    t = times(s)
    burst = snap(t, 7701, 0.0, 26, 0.08, 1500, 2.0) + thump(t, 0.0, 80, 0.06) * 0.6
    race = rasp(t, 7702, sweep(t, 30, 90, 0.35), sweep(t, 900, 3000, 0.35), window(t, 0.02, 0.36, 0.04)) * 2.4
    catches = sum(svf(noise(len(t), 7703 + i), 3800, 2.5) * np.exp(-np.maximum(0, t - at) / 0.02) * (t >= at) * 1.1
                  for i, at in enumerate([0.34, 0.39, 0.45]))
    yank = rasp(t, 7710, 55, sweep(t - 0.6, 2200, 700, 0.5), window(t, 0.6, 1.1, 0.06)) * 1.6
    return finish("sfx_paete_thorn_burst", burst + race + catches + yank, s, 0.68)


def sentry_cast():
    """YAKAP NG MAKILING's throw: the seed's big glow as a swelling hollow-wood drone (two bar
    notes beating), cut off by a heavy overhand swish."""
    s = 0.75
    t = times(s)
    glow = (np.sin(2 * np.pi * 146.8 * t) + 0.6 * np.sin(2 * np.pi * 148.3 * t) + 0.3 * np.sin(2 * np.pi * 405 * t)) \
        * np.clip(t / 0.4, 0, 1) ** 1.5 * (t < 0.46) * 0.35
    swish = svf(noise(len(t), 7801), sweep(t - 0.42, 380, 1600, 0.2), 1.4) * window(t, 0.42, 0.72, 0.06) * 1.6
    return finish("sfx_cast_paete_sentry", glow + swish, s, 0.62)


def sentry_burst():
    """⚠️ v5 (2026-09-26 night, direction.md 5.14): THE COURT BULGES AND THE CLAWS BREAK OUT. The tree no longer erupts to full
    height in half a second (owner: *"i also dotn want the tree to jsut spawn in or teleport in"*, *"and then the tree slowly show
    up"*); this is the first beat of its crawl, when the roots arrive under the spot: a deep swelling crack as the court heaves up,
    six bark claws punching out one after another in their own order (`PaeteSentryBody` `ClawOut`), each gripping with a knock,
    and a groan starting under it all. The hauls that follow have their own cue (`sentry_heave`)."""
    s = 1.3
    t = times(s)
    swell = one_pole_low(one_pole_low(noise(len(t), 7931), 90), 90) * env_ar(t, 0.12, 0.35, 0.14) * 14.0
    crack = snap(t, 7901, 0.0, 55, 0.22, 1100, 2.2) + snap(t, 7902, 0.03, 30, 0.18, 2900, 1.1)
    claws = sum(snap(t, 7940 + k, at, 14, 0.04, 1500 + 180 * k, 1.1) + knock(t, at + 0.1, 110 + 9 * k, 0.07, 0.8)
                for k, at in enumerate([0.02, 0.07, 0.10, 0.14, 0.20, 0.26]))
    sub = thump(t, 0.0, 46, 0.22) * 0.9
    groan = creak(t, 7920, sweep(t, 32, 48, s), 0.4, [(200, 1.0), (470, 0.5)], 7.0, env_ar(t, 0.3, 0.5, 0.6)) * 2.0
    return finish("sfx_paete_sentry_burst", swell + crack + claws + sub + groan, s, 0.8)


def sentry_heave():
    """⚠️ NEW in v5: ONE HAUL of the tree crawling out of the court, played three times (`PaeteSentry`, each a step lower in pitch
    and harder in the shake). A strain first (a creak whose slips speed up, the trunk loading), then the pull: a deep wooden groan,
    the court breaking round it with a felt thump, and soil pouring off its shoulders (grains, not hiss)."""
    s = 1.0
    t = times(s)
    strain = creak(t, 7951, sweep(t, 40, 110, 0.28), 0.5, [(260, 1.0), (620, 0.4)], 8.0, window(t, 0.0, 0.32, 0.06)) * 1.6
    pull = creak(t, 7952, sweep(t - 0.25, 28, 60, 0.5), 0.35, [(95, 1.0), (230, 0.6)], 11.0, window(t, 0.25, 0.85, 0.12)) * 2.6
    court = snap(t, 7953, 0.27, 45, 0.25, 900, 1.6) + thump(t, 0.27, 42, 0.3) * 1.2
    soil = rustle(t, 7954, 2600 * window(t, 0.32, 0.95, 0.25), 1800, q=1.1) * 0.9
    return finish("sfx_paete_sentry_heave", strain + pull + court + soil, s, 0.74)


def sentry_catch():
    """The vines take them and drag them in: taut creaks straining (several bodies, several
    groans at different pitches) and a rope-like rasp as they are hauled across the ground."""
    s = 1.0
    t = times(s)
    strain = sum(creak(t, 8001 + i, sweep(t, a, b, 0.7), 0.4, [(c, 1.0), (c * 2.3, 0.4)], 8.0, window(t, 0.0, 0.75, 0.08)) * 1.8
                 for i, (a, b, c) in enumerate([(50, 120, 420), (65, 140, 560), (40, 100, 330)]))
    haul = rasp(t, 8010, 40, 1100, window(t, 0.05, 0.8, 0.1)) * 1.0
    settle = knock(t, 0.8, 120, 0.06, 0.8)
    return finish("sfx_paete_sentry_catch", strain + haul + settle, s, 0.7)


def rooted():
    """ROOTED (status, on the victim): the roots close round the shins. A short low root groan
    and a soil crunch. Mixed like the other statuses: three can land inside a second."""
    s = 0.6
    t = times(s)
    groan = creak(t, 8101, sweep(t, 85, 45, 0.5), 0.45, [(260, 1.0), (640, 0.5)], 7.5, env_ar(t, 0.06, 0.2, 0.12)) * 2.4
    crunch = one_pole_low(noise(len(t), 8102), 1200) * np.exp(-t / 0.05) * 2.0
    return finish("sfx_status_rooted", groan + crunch, s, 0.58)


def root_break():
    """Breaking free (7 s of Interact, or the sentry going back to sleep): the shin branches and the
    waist band CRACK apart (direction.md section 5.6). A bright bark snap, a second smaller one as the
    other leg rips out, the chunks pattering onto the road and a flurry of leaves. It has to read as
    RELIEF, so it stays bright and short and ends on the patter, not on a groan."""
    s = 0.7
    t = times(s)
    crack = snap(t, 8201, 0.0, 16, 0.04, 3000, 2.2) + knock(t, 0.0, 520, 0.03, 0.6)
    second = snap(t, 8203, 0.2, 9, 0.03, 3400, 1.3)
    chunks = knock(t, 0.30, 760, 0.02, 0.35) + knock(t, 0.37, 640, 0.02, 0.3) + knock(t, 0.45, 880, 0.02, 0.25) + knock(t, 0.52, 700, 0.018, 0.2)
    flurry = rustle(t, 8202, 2400 * env_ar(t, 0.02, 0.16, 0.05), 5000) * 0.8
    return finish("sfx_paete_root_break", crack + second + chunks + flurry, s, 0.6)


def sentry_wilt():
    """The sentry dries to a stump and drops narra pods: a dry crackle thinning out and three
    small pod knocks on the ground."""
    s = 1.2
    t = times(s)
    crackle = snap(t, 8301, 0.0, 40, 0.8, 2200, 1.2) * np.exp(-t / 0.5)
    sag = creak(t, 8302, sweep(t, 70, 25, 0.8), 0.5, [(200, 1.0)], 6.0, env_ar(t, 0.1, 0.4, 0.2)) * 1.6
    pods = knock(t, 0.55, 700, 0.03, 0.5) + knock(t, 0.72, 820, 0.03, 0.4) + knock(t, 0.86, 640, 0.03, 0.35)
    return finish("sfx_paete_sentry_wilt", crackle + sag + pods, s, 0.55)


def sentry_wake():
    """THE TREE WAKES (direction.md section 5.2): the light opens in the two hollows and it leans at
    its first prisoner. A deep wooden groan whose slips RISE (the tree drawing itself up), a low
    breath of air through the wood, a leaf shiver in the crown, and two soft carved-bar notes as the
    eyes open, a fourth apart and quiet: it is waking, not roaring."""
    s = 1.0
    t = times(s)
    groan = creak(t, 8601, sweep(t, 26, 64, 0.7), 0.35, [(170, 1.0), (410, 0.5)], 7.5, env_ar(t, 0.18, 0.35, 0.45)) * 2.6
    breath = one_pole_low(noise(len(t), 8602), 220) * env_ar(t, 0.3, 0.25, 0.4) * 1.4
    shiver = rustle(t, 8603, 1600 * window(t, 0.2, 0.75, 0.15), 3800) * 0.5
    eyes = knock(t, 0.42, 294, 0.16, 0.45) + knock(t, 0.58, 392, 0.18, 0.4)
    return finish("sfx_paete_sentry_wake", groan + breath + shiver + eyes, s, 0.6)


def sprout_ready():
    """The seedling's wooden slipper has grown (direction.md section 5.3): the petals part and the
    pod gives a proud little bob. A soft pod POP (a short rising bloop), a tiny leaf tick and one
    small high wood note, so the player who owns it hears a shot is ready without looking. Quiet:
    it plays every 15 s of a 40 s plant."""
    s = 0.35
    t = times(s)
    pop = np.sin(2 * np.pi * np.cumsum(sweep(t, 340, 620, 0.07)) / RATE) * env_ar(t, 0.006, 0.04, 0.012) * 0.8
    tick = rustle(t, 8701, 900 * window(t, 0.02, 0.18, 0.05), 5200) * 0.4
    note = knock(t, 0.05, 1046, 0.05, 0.35)
    return finish("sfx_paete_sprout_ready", pop + tick + note, s, 0.4)


def theme():
    """HIS THEME, under the introduction (6.5 s, beat for beat with it): wooden bars over the mountain.

    Paete is the carving town, so the lead is carved wood: an ORIGINAL pentatonic figure on the bar model
    (`knock`), the register of a bamboo or narra xylophone, with no sample and no borrowed melody. Under it
    the mountain: a low bowed-wood drone that thickens and a leaf bed that swells as the ground answers."""
    # ⚠️⚠️ v8 (2026-09-27): 6.5 S. The owner on v7: *"lowk slow down ult a bit i cant comprehend wtf is happening"*; he chose 6.5 s.
    # The cutscene is stretched evenly by 1.3 (`HeroIntroductionScene.Paete.cs` `PaeteStretch`, `tools/author_ultimate_intros.py`
    # `_stretch`), so every EVENT below is typed on the 5.0 s clock the direction was written on and placed through `T` (x 1.3).
    # WHEN a sound happens stretches; what it IS does not (a bar's ring, a pitch, a decay are its own), so the motif keeps its notes.
    # The beats, on the 5.0 s clock (x 1.3 for the film): her rise 0.03 to 0.45, her full form 0.30 to 0.50, the light falling 0.58
    # to 0.70, his eyes 0.78, the drop 1.08 to 1.22, the slam 1.22, the roots digging 1.25 to 1.50, the channel 1.45 to 2.10 with
    # heartbeats at 1.62, 1.80 and 1.95, the send 2.10, the race to 2.50, the court bulging 2.50, the hauls 2.78, 3.05 and 3.33, the
    # top-out 3.53, its eyes 3.71, THE TAKE: the lash 3.80 to 4.10, the cinch, the yank 4.18, the thud 4.50, the struggle to the end.
    K = 1.3
    s = 5.0 * K

    def T(x):
        return x * K

    t = times(s)
    pulses = tuple(T(x) for x in (1.62, 1.80, 1.95))
    hauls_at = tuple(T(x) for x in (2.78, 3.05, 3.33))
    send_at, arrive_at, topout_at, wake_at = T(2.10), T(2.50), T(3.53), T(3.71)
    yank_at, thud_at = T(4.18), T(4.50)
    slam = T(1.22)
    # Her rise, soft and climbing.
    bars = sum(knock(t, T(at), p, 0.28, 0.55) for at, p in [(0.08, 261.6), (0.20, 293.7), (0.32, 349.2), (0.44, 392.0)])
    # Her full form: a warm held chord swelling in and thinning away as she turns back to spirit.
    form = (np.sin(2 * np.pi * 261.6 * t) + 0.7 * np.sin(2 * np.pi * 329.6 * t) + 0.5 * np.sin(2 * np.pi * 392.0 * t)) \
        * window(t, T(0.30), T(1.05), 0.2) * np.clip((t - T(0.3)) / 0.26, 0, 1) * np.clip((T(1.08) - t) / 0.39, 0, 1) * 0.07
    # The light falls: a quick run down the bars, a tap where it lands in his hand.
    drop = sum(knock(t, T(at), p, 0.12, 0.5) for at, p in [(0.58, 784.0), (0.61, 659.3), (0.64, 523.3), (0.67, 440.0)]) \
        + knock(t, T(0.70), 392.0, 0.35, 0.8)
    # IGNITION: a low bloom under a bright struck chord.
    ignite = thump(t, T(0.78), 70, 0.45) * 0.9 + knock(t, T(0.78), 523.3, 0.9, 0.9) + knock(t, T(0.78), 784.0, 0.7, 0.55) \
        + np.sin(2 * np.pi * 1046.5 * t) * window(t, T(0.78), T(1.3), 0.3) * env_ar(t, 0.01, 0.5, T(0.80)) * 0.12
    # The drop: a rising draw of air and leaf into the slam.
    draw = rustle(t, 8410, 2200 * np.clip((t - T(1.06)) / (T(1.22) - T(1.06)), 0, 1) ** 2 * window(t, T(1.06), slam, 0.02), 4200) * 0.5
    # The slam: soil crunch, a felt thump, the court answering like a struck log.
    press = snap(t, 8403, slam, 40, 0.10, 900, 0.9) + thump(t, slam, 55, 0.35) * 1.3 + knock(t, slam, 98, 0.25, 0.9)
    # His roots dig in: wood creaking DOWN into soil (the slips slow as they go deeper).
    dig = creak(t, 8404, sweep(t - T(1.25), 120, 45, T(0.25)), 0.5, [(300, 1.0), (700, 0.4)], 8.0, window(t, T(1.25), T(1.5), 0.05)) * 1.6 \
        + rustle(t, 8412, 1600 * window(t, T(1.25), T(1.5), 0.08), 1500, q=1.1) * 0.6
    # The channel: a hum rising under three quickening heartbeats.
    hum = creak(t, 8405, 60 + 90 * np.clip((t - T(1.45)) / T(0.6), 0, 1), 0.2, [(130.8, 1.0), (196.0, 0.6), (261.6, 0.3)], 18.0,
                window(t, T(1.45), T(2.16), 0.08) * np.clip((t - T(1.45)) / T(0.6), 0.25, 1)) * 1.8
    beats = sum(thump(t, at, 58, 0.16) * 1.2 + knock(t, at, 196.0, 0.14, 0.45) for at in pulses)
    # The send: one big pulse, then the roots race off: a crackle running away and rising, a grind under it, a pop as the court bulges.
    send = thump(t, send_at, 50, 0.25) * 1.3 + knock(t, send_at, 261.6, 0.5, 0.8)
    race = snap(t, 8406, send_at + 0.02, 150, arrive_at - send_at - 0.02, 1400, 1.2) * window(t, send_at, arrive_at + 0.01, 0.04)
    grind = creak(t, 8413, sweep(t - send_at, 60, 170, arrive_at - send_at), 0.5, [(220, 1.0), (520, 0.4)], 8.0,
                  window(t, send_at, arrive_at + 0.03, 0.05)) * 1.8
    bulge = one_pole_low(one_pole_low(noise(len(t), 8407), 80), 80) * env_ar(np.maximum(0, t - arrive_at), 0.1, 0.35, 0.12) * (t >= arrive_at) * 14.0 \
        + snap(t, 8414, arrive_at, 50, 0.2, 1100, 1.0)
    # The three hauls, each bigger: a groan, the court breaking, a thump.
    hauls = sum((creak(t, 8420 + k, sweep(t - at, 26, 50, 0.45), 0.35, [(90 - 6 * k, 1.0), (215, 0.5)], 11.0, window(t, at - 0.08, at + 0.39, 0.07)) * 1.8
                 + snap(t, 8430 + k, at, 40 + 15 * k, 0.25, 800, 1.1) + thump(t, at, 44 - 3 * k, 0.33) * (1.0 + 0.25 * k))
                for k, at in enumerate(hauls_at))
    topout = snap(t, 8411, topout_at, 30, 0.25, 2600, 0.6)
    leaves = rustle(t, 8402, 1800 * window(t, T(2.8), T(4.9), 0.5) + 1600 * window(t, topout_at - 0.03, topout_at + 0.4, 0.1), 3600) * 0.6
    # Its eyes light: one deep carved note with a soft chime over it.
    wake = knock(t, wake_at, 130.8, 1.1, 1.1) + knock(t, wake_at, 392.0, 0.7, 0.35) \
        + np.sin(2 * np.pi * 784.0 * t) * window(t, wake_at, wake_at + 0.6, 0.15) * env_ar(np.maximum(0, t - wake_at), 0.02, 0.45) * (t >= wake_at) * 0.08
    drone = creak(t, 8401, 110 + 0 * t, 0.05, [(130.8, 1.0), (196.0, 0.6)], 30.0, np.clip(t / T(2.6), 0, 1) ** 1.4 * window(t, 0, s - 0.05, 0.3)) * 1.0

    # ---- v6 layers, each on the frame of its picture.
    gust = whoosh(t, 8440, 0.0, T(0.52), 480, 2400, 0.9, attack=0.6) \
        + rustle(t, 8441, 2600 * np.clip(t / T(0.4), 0, 1) * window(t, 0, T(0.5), 0.06), 3800) * 0.55 \
        + rustle(t, 8449, 3200 * np.exp(-t / 0.08) * window(t, 0, 0.3, 0.002), 4400) * 0.5  # the cut-in: leaves already flying on frame 0
    spin_rate = 4.0 + 5.0 * np.clip((t - T(0.12)) / T(0.34), 0, 1)
    whirl = rustle(t, 8442, 2400 * (0.5 + 0.5 * np.sin(2 * np.pi * np.cumsum(spin_rate) / RATE)) * window(t, T(0.12), T(0.47), 0.05), 3000) * 0.45
    chimes = sum(knock(t, T(at), p, 0.35, 0.28) for at, p in ((0.460, 1568.0), (0.474, 1975.5), (0.489, 1318.5), (0.508, 2349.3),
                                                              (0.529, 1760.0), (0.556, 2093.0), (0.590, 1568.0)))
    burst = whoosh(t, 8443, T(0.44), T(0.36), 2800, 900, 0.7, attack=0.12) + chimes
    rise0, rise1 = T(0.6), T(0.78)
    riser = svf(noise(len(t), 8444), 900 + 2100 * np.clip((t - rise0) / (rise1 - rise0), 0, 1), 1.4) \
        * np.clip((t - rise0) / (rise1 - rise0), 0, 1) ** 3 * window(t, rise0, rise1, 0.004) * 0.5
    # THE MARK, its seal on the court, and its fifth return as they all hit the trunk.
    marks = bell(t, T(0.78), 146.8, 0.55) + bell(t, slam, 73.4, 0.6) + bell(t, arrive_at, 110.0, 0.25) + bell(t, wake_at, 146.8, 0.5) \
        + bell(t, thud_at, 110.0, 0.45)
    seal = knock(t, T(1.24), 392.0, 0.9, 0.22) + knock(t, T(1.27), 587.3, 0.8, 0.18) + knock(t, T(1.30), 784.0, 0.7, 0.14)
    shock = rustle(t, 8445, 3000 * np.exp(-np.maximum(0, t - slam) / 0.14) * (t >= slam) * window(t, slam, T(1.7), 0.02), 3400) * 0.6
    orbit_rate = 2.0 + 0.85 * sum(np.clip((t - p) / 0.1, 0, 1) for p in pulses)
    orbit = rustle(t, 8446, 1500 * (0.5 + 0.5 * np.sin(2 * np.pi * np.cumsum(orbit_rate) / RATE)) * window(t, T(1.38), T(2.12), 0.15), 3200) * 0.35
    streaks = sum(glide(t, T(at), f0, f1, 0.3, 0.05) for at, f0, f1 in (
        (1.62, 700, 1500), (1.64, 760, 1620), (1.80, 680, 1480), (1.82, 820, 1700), (1.84, 740, 1560), (1.95, 720, 1600),
        (1.96, 800, 1760), (1.98, 660, 1440), (1.99, 780, 1680), (2.10, 600, 1500), (2.11, 700, 1650), (2.13, 640, 1560)))
    slash = whoosh(t, 8447, send_at - 0.02, 0.3, 3400, 800, 1.2, attack=0.2)
    shafts = glide(t, arrive_at - 0.04, 380, 1150, 0.55, 0.07) + glide(t, arrive_at + 0.01, 460, 1300, 0.5, 0.05) + glide(t, arrive_at + 0.06, 540, 1480, 0.48, 0.04)
    spirals = sum(rustle(t, 8448 + k, 2800 * np.exp(-np.maximum(0, t - at) / 0.35) * (t >= at) * window(t, at, at + 0.75, 0.03), 3600) * 0.45
                  for k, at in enumerate(hauls_at))
    shimmer = np.sin(2 * np.pi * 1568.0 * t) * window(t, wake_at, wake_at + 0.55, 0.15) * env_ar(np.maximum(0, t - wake_at), 0.03, 0.45) * (t >= wake_at) * 0.05

    # ---- v7 layers: the burst layer and THE TAKE.
    tinkle = sum(np.sin(2 * np.pi * p * t) * env_ar(np.maximum(0, t - T(at)), 0.002, 0.18) * (t >= T(at)) * 0.035 for at, p in (
        (0.44, 3136.0), (0.47, 3729.3), (0.50, 2793.8), (0.79, 3520.0), (0.82, 4186.0), (0.86, 3136.0),
        (3.72, 3520.0), (3.75, 4698.6), (3.78, 3951.1), (4.51, 3136.0), (4.54, 4186.0), (4.58, 3520.0), (4.64, 4698.6)))
    swirl_rate = 5.0 + 4.0 * sum(np.clip((t - p) / 0.08, 0, 1) for p in pulses)
    swirl = np.sin(2 * np.pi * np.cumsum(660.0 + 40.0 * np.sin(2 * np.pi * np.cumsum(swirl_rate) / RATE)) / RATE) \
        * window(t, T(1.45), T(2.14), 0.12) * np.clip((t - T(1.45)) / 0.6, 0, 1) * 0.035
    pillar = whoosh(t, 8460, arrive_at - 0.02, 0.6, 300, 1800, 0.55, attack=0.18)
    boom = thump(t, wake_at, 46, 0.45) * 0.9 + whoosh(t, 8461, wake_at, 0.55, 1400, 400, 0.4, attack=0.1)
    lash = whoosh(t, 8462, T(3.79), 0.34, 900, 3200, 0.8, attack=0.7) \
        + sum(snap(t, 8463 + k, T(at), 22, 0.03, 2400, 0.9) + knock(t, T(at), 220.0 + 30 * k, 0.12, 0.3)
              for k, at in enumerate((4.00, 4.03, 4.06, 4.10)))
    cinch = creak(t, 8467, sweep(t - T(4.02), 50, 140, 0.2), 0.4, [(320, 1.0), (760, 0.4)], 9.0, window(t, T(4.02), T(4.2), 0.03)) * 1.2
    yank = thump(t, yank_at, 52, 0.25) * 1.2 + knock(t, yank_at, 146.8, 0.3, 0.5) \
        + creak(t, 8468, sweep(t - yank_at, 30, 90, thud_at - yank_at), 0.35, [(85, 1.0), (190, 0.6)], 11.0, window(t, yank_at, thud_at + 0.02, 0.04)) * 2.0 \
        + whoosh(t, 8469, yank_at - 0.02, thud_at - yank_at + 0.04, 600, 2600, 0.9, attack=0.75)
    thud = thump(t, thud_at, 42, 0.45) * 1.6 + knock(t, thud_at, 98.0, 0.35, 1.0) + snap(t, 8470, thud_at, 60, 0.14, 900, 1.2) \
        + knock(t, thud_at + 0.01, 392.0, 0.6, 0.3) + knock(t, thud_at + 0.03, 587.3, 0.5, 0.22)
    strain = creak(t, 8471, 70 + 50 * (0.5 + 0.5 * np.sin(2 * np.pi * 3.2 * t)), 0.45, [(260, 1.0), (620, 0.35)], 9.0,
                   window(t, thud_at + 0.05, s, 0.08) * (0.55 + 0.45 * np.sin(2 * np.pi * 3.2 * t) ** 2)) * 1.1 \
        + rustle(t, 8472, 900 * window(t, thud_at + 0.05, s, 0.1), 3000) * 0.35

    return finish("sfx_ult_theme_paete", bars + form + drop + ignite + draw + press + dig + hum + beats + send + race + grind + bulge
                  + hauls + topout + leaves + wake + drone
                  + gust + whirl + burst + riser + marks + seal + shock + orbit + streaks + slash + shafts + spirals + shimmer
                  + tinkle + swirl + pillar + boom + lash + cinch + yank + thud + strain, s, 0.66)


def sky():
    """The Canopy sky (Makiling mist): a leaf bed that breathes in slow gusts, very quiet, with
    one far wood knock, like a branch settling somewhere up the mountain."""
    s = 3.2
    t = times(s)
    gust = 0.35 + 0.65 * np.sin(np.pi * np.clip(t / s, 0, 1)) ** 2
    bed = rustle(t, 8501, 900 * gust, 2800) * 0.9 + rustle(t, 8502, 500 * gust, 5600) * 0.4
    mist = one_pole_low(noise(len(t), 8503), 160) * gust * 1.2
    far = knock(t, 1.9, 240, 0.12, 0.2)
    return finish("sfx_sky_canopy", bed + mist + far, s, 0.5)


if __name__ == "__main__":
    # ⚠️ `sentry_cast` IS NO LONGER WRITTEN HERE: the ultimate is called up through the ground now, and
    # `tools/build_rework_audio.py` (`paete_ground_call`) owns `sfx_cast_paete_sentry`. Left defined for history.
    rows = [vine(), sprout_cast(), command(), sprout_land(), sprout_fire(), sprout_uproot(), thorns_cast(),
            thorn_burst(), sentry_burst(), sentry_heave(), sentry_catch(), rooted(), root_break(),
            sentry_wilt(), sentry_wake(), sprout_ready(), theme(), sky()]
    report = {
        "provenance": "Original deterministic synthesis (numpy only); no external samples, voices or paid API.",
        "listening": "Not yet heard by the owner in the game mix. Peak and RMS are measurements, not approval.",
        "cues": rows,
    }
    out = ROOT / "docs/reports/paete-kit-2026-09-25/paete-audio.json"
    out.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"Authored {len(rows)} Paete cues; nothing else touched.")
