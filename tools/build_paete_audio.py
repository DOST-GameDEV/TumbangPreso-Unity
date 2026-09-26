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
    """The sentry bursts out of the ground: the biggest sound in his kit. A sub hit and a wall
    of cracking wood, a leaf explosion, then the radial vines lashing out (plucks, not whooshes,
    so it cannot be mistaken for Amihan's gale)."""
    s = 1.6
    t = times(s)
    sub = thump(t, 0.0, 42, 0.3) * 1.3
    crack = snap(t, 7901, 0.0, 70, 0.28, 1300, 2.6) + snap(t, 7902, 0.02, 40, 0.2, 3200, 1.4)
    leaves = rustle(t, 7903, 5200 * env_ar(t, 0.02, 0.35, 0.06), 3600) * 1.4
    lashes = sum(pluck(t, 0.18 + 0.05 * i, p, 0.6, 7910 + i, 0.993, 0.25) for i, p in enumerate([131, 165, 147, 175, 123])) * 0.55
    groan = creak(t, 7920, sweep(t, 55, 30, s), 0.4, [(240, 1.0), (560, 0.5)], 7.0, env_ar(t, 0.3, 0.5, 0.5)) * 2.2
    return finish("sfx_paete_sentry_burst", sub + crack + leaves + lashes + groan, s, 0.8)


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
    """HIS THEME, under the introduction (3.6 s): wooden bars over the forest.

    Paete is the carving town, so the lead is carved wood: an ORIGINAL descending-then-rising
    pentatonic figure on the bar model (`knock`), the register of a bamboo or narra xylophone,
    with no sample and no borrowed melody. Under it the mountain: a low bowed-wood drone that
    thickens and a leaf bed that swells as the roots gather, landing on the release beat."""
    s = 3.6
    t = times(s)
    notes = [(0.20, 392.0), (0.46, 349.2), (0.72, 293.7), (1.10, 261.6),
             (1.55, 293.7), (1.80, 349.2), (2.05, 392.0), (2.40, 523.3), (2.42, 392.0)]
    bars = sum(knock(t, at, p, 0.28, 0.9 if i < 7 else 1.3) for i, (at, p) in enumerate(notes))
    drone = creak(t, 8401, 110 + 0 * t, 0.05, [(130.8, 1.0), (196.0, 0.6)], 30.0, np.clip(t / 2.4, 0, 1) ** 1.4) * 1.4
    leaves = rustle(t, 8402, 300 + 2200 * np.clip((t - 0.8) / 1.6, 0, 1) ** 2 * (t < 2.9), 3600) * 0.5
    sub = thump(t, 2.40, 49, 0.5) * 0.9
    return finish("sfx_ult_theme_paete", bars + drone + leaves + sub, s, 0.66)


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
            thorn_burst(), sentry_burst(), sentry_catch(), rooted(), root_break(),
            sentry_wilt(), sentry_wake(), sprout_ready(), theme(), sky()]
    report = {
        "provenance": "Original deterministic synthesis (numpy only); no external samples, voices or paid API.",
        "listening": "Not yet heard by the owner in the game mix. Peak and RMS are measurements, not approval.",
        "cues": rows,
    }
    out = ROOT / "docs/reports/paete-kit-2026-09-25/paete-audio.json"
    out.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"Authored {len(rows)} Paete cues; nothing else touched.")
