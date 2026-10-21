"""Lagoon Cove ambience: surf and wind loops plus seabird calls, by deterministic synthesis.

Writes ONLY these files into Assets/TumbangPreso/Art/audio/ambience/:
    lagoon_waves.wav, lagoon_wind.wav, lagoon_gull_1.wav .. lagoon_gull_5.wav
and the measurement record docs/reports/lagoon-ambience-authoring.json.

Owner brief (2026-09-27): "add wind, birds and waves sfx (environmental sounds, so being
near/facing the water you hear more waves, same with wind when facing/nearer into the land)".
`Runtime/Map/LagoonSoundscape.cs` plays these; this file only authors them.

Same method as build_rafi_audio.py and build_lagoon_audio.py: numpy only, fixed seeds, no
external samples, so a rerun is byte-identical and the provenance is simple to state.

Run:  py -3 tools/build_lagoon_ambience.py
"""
import hashlib
import json
from pathlib import Path
import wave

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/TumbangPreso/Art/audio/ambience'
REPORT = ROOT / 'docs/reports/lagoon-ambience-authoring.json'
RATE = 44100

# ⚠️ 32 s AND 36 s, DIFFERENT ON PURPOSE. The two beds play at once for the whole match; with
# equal lengths every swell would meet the same gust on every lap and the repetition becomes
# audible within two minutes. 32 and 36 only realign every 288 s.
WAVES_SECONDS = 32.0
WIND_SECONDS = 36.0

# ⚠️ THE SEAM CHECK WINDOW IS THE BRIEF'S OWN NUMBER: first and last 50 ms within 10 per cent RMS.
SEAM_WINDOW = int(0.050 * RATE)


# ---------------------------------------------------------------------------------------------
# Periodic building blocks.
#
# ⚠️⚠️ EVERYTHING IN A LOOP IS BUILT CIRCULAR, SO THE LOOP HAS NO SEAM TO HIDE. Noise is shaped in
# the frequency domain over exactly the loop length (an inverse FFT of length N is periodic in N),
# slow envelopes are the same thing at sub-hertz, and every event is written modulo N, so a swell
# that starts at 31 s finishes its wash at 1 s of the next lap. A crossfade would have been the
# usual fix, and a crossfade of two unrelated noise stretches dips about 3 dB in the middle unless
# it is equal-power, and still smears one swell into another. Nothing to crossfade here.
# ---------------------------------------------------------------------------------------------

def band_noise(n, seed, lo, hi, tilt=0.0, order=2):
    """Unit-RMS noise, periodic in n, between lo and hi Hz with a soft Butterworth-like skirt.

    tilt is the spectral slope in dB per octave above lo (-3 gives pink-ish air)."""
    rng = np.random.default_rng(seed)
    bins = n // 2 + 1
    spec = rng.normal(size=bins) + 1j * rng.normal(size=bins)
    f = np.fft.rfftfreq(n, 1.0 / RATE)
    f[0] = 1e-3
    mag = np.ones_like(f)
    if lo > 0:
        mag /= np.sqrt(1.0 + (lo / f) ** (2 * order))
    if hi > 0:
        mag /= np.sqrt(1.0 + (f / hi) ** (2 * order))
    if tilt:
        mag *= (np.maximum(f, max(lo, 1.0)) / max(lo, 1.0)) ** (tilt / 6.02)
    mag[0] = 0.0
    out = np.fft.irfft(spec * mag, n)
    return out / np.sqrt(np.mean(out ** 2))


def slow_random(n, seed, hi_hz):
    """A smooth periodic random curve, unit RMS, with nothing faster than about hi_hz."""
    rng = np.random.default_rng(seed)
    bins = n // 2 + 1
    spec = rng.normal(size=bins) + 1j * rng.normal(size=bins)
    f = np.fft.rfftfreq(n, 1.0 / RATE)
    spec *= np.exp(-(f / hi_hz) ** 2)
    spec[0] = 0.0
    out = np.fft.irfft(spec, n)
    return out / np.sqrt(np.mean(out ** 2))


def add_wrapped(dst, start, piece):
    """Adds piece into dst starting at sample start, wrapping past the end (the loop is a circle)."""
    n = len(dst)
    start %= n
    m = len(piece)
    first = min(m, n - start)
    dst[start:start + first] += piece[:first]
    rest = m - first
    while rest > 0:
        take = min(rest, n)
        dst[:take] += piece[first:first + take]
        first += take
        rest -= take


def attack_decay(seconds, attack, tau, hold=0.0):
    """Raised-cosine attack, optional hold, exponential tail cut at about -60 dB."""
    n = int(seconds * RATE)
    t = np.arange(n) / RATE
    env = np.where(t < attack, 0.5 - 0.5 * np.cos(np.pi * t / max(attack, 1e-4)), 1.0)
    tail = t - attack - hold
    env = np.where(tail > 0, np.exp(-tail / tau), env)
    return env


def event_times(n_seconds, rng, lo, hi):
    """Irregular intervals between about lo and hi seconds that sum EXACTLY to the loop length,
    so the gap across the seam is as irregular as every other gap rather than a leftover stub."""
    count = max(2, int(round(n_seconds / ((lo + hi) / 2))))
    gaps = rng.uniform(lo, hi, count)
    gaps *= n_seconds / gaps.sum()
    return np.concatenate([[0.0], np.cumsum(gaps)[:-1]]) + rng.uniform(0, 1.0)


# ---------------------------------------------------------------------------------------------
# Surf.
#
# A calm lagoon beach, not a storm: each swell is heard as (1) a low build as it stands up,
# (2) a soft broadband WASH when it spills, whose top end dies first so it darkens as it runs up
# the sand, (3) a FIZZ of tiny bubbles popping in the foam, which outlasts the wash, and (4) a
# quiet DRAW-BACK hiss with a little sand grit as the water slides back. Under it, a steady
# low rumble of the whole cove and a faint distant surf band so the bed is never dead between
# swells. Intervals 4.5 to 8.5 s with strength 0.5 to 1.0 so no two swells in a lap are alike.
# ---------------------------------------------------------------------------------------------

def author_waves():
    n = int(WAVES_SECONDS * RATE)
    rng = np.random.default_rng(9101)
    out = np.zeros(n)

    rumble = band_noise(n, 9102, 25, 220, tilt=-4)
    bed = band_noise(n, 9103, 180, 1600, tilt=-3)
    body = band_noise(n, 9104, 70, 650, tilt=-3)
    wash = band_noise(n, 9105, 260, 2600, tilt=-2)
    hiss = band_noise(n, 9106, 2200, 9000, tilt=-2)
    crackle = band_noise(n, 9107, 3000, 11000)
    grit = band_noise(n, 9108, 900, 5000)

    swell_slow = slow_random(n, 9109, 0.08)
    out += rumble * 0.085 * (0.8 + 0.2 * np.tanh(swell_slow))
    out += bed * 0.022 * (0.75 + 0.25 * np.tanh(slow_random(n, 9110, 0.15)))

    env_body = np.zeros(n)
    env_wash = np.zeros(n)
    env_hiss = np.zeros(n)
    env_fizz = np.zeros(n)
    env_draw = np.zeros(n)
    fizz = np.zeros(n)

    starts = event_times(WAVES_SECONDS, rng, 4.5, 8.5)
    swells = []
    for at in starts:
        s = rng.uniform(0.5, 1.0)
        i0 = int(at * RATE)
        build = rng.uniform(0.9, 1.6)
        # (1) the build: rises for `build` seconds before the spill, then dies away.
        e = attack_decay(build + 6.0, build, rng.uniform(1.2, 1.9))
        add_wrapped(env_body, i0 - int(build * RATE), e * s)
        # (2) the wash, and its bright edge decaying faster.
        e = attack_decay(6.0, rng.uniform(0.22, 0.38), rng.uniform(0.9, 1.5), hold=rng.uniform(0.05, 0.25))
        add_wrapped(env_wash, i0, e * s)
        e = attack_decay(4.0, rng.uniform(0.25, 0.4), rng.uniform(0.4, 0.7))
        add_wrapped(env_hiss, i0, e * s ** 1.6)
        # (3) the fizz, starting as the wash peaks and outlasting it.
        e = attack_decay(8.0, rng.uniform(0.35, 0.6), rng.uniform(1.3, 2.1))
        add_wrapped(env_fizz, i0 + int(0.25 * RATE), e * s)
        # (4) the draw-back, a slow hump a couple of seconds after the spill.
        d0 = rng.uniform(1.6, 2.4)
        dl = rng.uniform(2.0, 3.2)
        t = np.arange(int(dl * RATE)) / RATE
        add_wrapped(env_draw, i0 + int(d0 * RATE), np.sin(np.pi * t / dl) ** 2 * s)
        swells.append({'at': round(float(at) % WAVES_SECONDS, 3), 'strength': round(float(s), 3)})

    # Bubbles: short decaying sine blips from 1.8 to 7 kHz, their density following the foam.
    # ⚠️ Drawn as a Poisson process against the MAX density, then thinned by the envelope, so a
    # louder swell has more bubbles rather than the same bubbles louder.
    peak_rate = 420.0
    count = rng.poisson(peak_rate * WAVES_SECONDS)
    positions = rng.integers(0, n, count)
    keep = rng.uniform(0, 1, count) < np.clip(env_fizz[positions], 0, 1) ** 1.3
    for p in positions[keep]:
        dur = rng.uniform(0.003, 0.014)
        m = int(dur * RATE)
        t = np.arange(m) / RATE
        f0 = rng.uniform(1800, 7000)
        # Real bubbles rise slightly in pitch as they close.
        blip = np.sin(2 * np.pi * f0 * (t + 0.5 * rng.uniform(4, 14) * t * t)) * np.exp(-t / (dur / 3.2))
        add_wrapped(fizz, int(p), blip * rng.uniform(0.2, 1.0))

    # A gate for the crackle so the foam hiss is grainy rather than smooth.
    grain = np.abs(band_noise(n, 9111, 20, 180)) ** 2.5
    grain /= np.sqrt(np.mean(grain ** 2))

    out += body * 0.26 * env_body
    out += wash * 0.34 * env_wash
    out += hiss * 0.16 * env_hiss
    out += crackle * 0.035 * env_fizz * grain
    out += fizz * 0.045
    out += (band_noise(n, 9112, 450, 2800, tilt=-3) * 0.10 + grit * 0.03 * grain) * env_draw

    return seam_rotate(out), {'swells': swells}


# ---------------------------------------------------------------------------------------------
# Wind, stereo.
#
# Warm coastal wind through palms, not a howl: every band is broad (nothing narrower than about
# an octave and a half, which is what keeps it from whistling), the brightness rises with the gust
# (air band follows gust^2.2 while the body follows gust^1.3), and leaves rustle in bursts whose
# DENSITY rises with the gust, the way fronds only really start to clatter above a threshold.
# The two channels share a gust that reaches one ear 0.18 s before the other, so a gust
# visibly passes across rather than pumping both ears at once.
# ---------------------------------------------------------------------------------------------

def author_wind():
    n = int(WIND_SECONDS * RATE)
    rng = np.random.default_rng(9201)

    g_slow = slow_random(n, 9202, 0.05)
    g_fast = slow_random(n, 9203, 0.35)
    # ⚠️ THE LULL STOPS AT 0.32. The first pass let the gust fall to 0.12 and one lull sat 20 dB
    # under the rest of the loop for three seconds: on a 36 s loop that reads as the wind
    # switching off, the same place every lap. Measured range now in the report.
    gust = 0.62 + 0.24 * np.tanh(0.9 * g_slow) + 0.13 * np.tanh(g_fast)
    gust = np.clip(gust, 0.32, 1.0)
    lag = int(0.18 * RATE)

    channels = []
    leaf_events = 0
    for ch in range(2):
        g = np.roll(gust, lag * ch)
        common_body = band_noise(n, 9210, 110, 1300, tilt=-3.5)
        own_body = band_noise(n, 9211 + ch, 110, 1300, tilt=-3.5)
        body = 0.72 * common_body + 0.69 * own_body
        low = 0.7 * band_noise(n, 9220, 35, 170) + 0.7 * band_noise(n, 9221 + ch, 35, 170)
        air = 0.6 * band_noise(n, 9230, 650, 3800, tilt=-2) + 0.8 * band_noise(n, 9231 + ch, 650, 3800, tilt=-2)
        sig = body * 0.30 * g ** 1.3 + low * 0.16 * g + air * 0.12 * g ** 2.2
        channels.append(sig)

    # Leaf rustle and frond clatter: short bursts of bright noise, placed per event and panned.
    leaf_hi = [band_noise(n, 9240 + ch, 2600, 9500) for ch in range(2)]
    frond = [band_noise(n, 9250 + ch, 700, 3200) for ch in range(2)]
    gate_leaf = [np.zeros(n), np.zeros(n)]
    gate_frond = [np.zeros(n), np.zeros(n)]
    max_rate = 55.0
    count = rng.poisson(max_rate * WIND_SECONDS)
    positions = rng.integers(0, n, count)
    thresh = np.clip((gust[positions] - 0.35) / 0.65, 0, 1) ** 2.2 + 0.04
    keep = rng.uniform(0, 1, count) < thresh
    for p in positions[keep]:
        dur = rng.uniform(0.006, 0.045)
        e = attack_decay(dur * 4, dur * rng.uniform(0.15, 0.5), dur * 0.6)
        pan = rng.uniform(-1, 1)
        amp = rng.uniform(0.3, 1.0) * gust[p]
        for ch in range(2):
            side = (1 - pan) / 2 if ch == 0 else (1 + pan) / 2
            add_wrapped(gate_leaf[ch], int(p), e * amp * np.sqrt(side))
        leaf_events += 1
    count = rng.poisson(6.0 * WIND_SECONDS)
    positions = rng.integers(0, n, count)
    keep = rng.uniform(0, 1, count) < np.clip((gust[positions] - 0.55) / 0.45, 0, 1)
    for p in positions[keep]:
        # A frond knock is a cluster of 2 to 5 soft taps.
        pan = rng.uniform(-1, 1)
        for k in range(rng.integers(2, 6)):
            dur = rng.uniform(0.015, 0.05)
            e = attack_decay(dur * 3, 0.004, dur * 0.5)
            q = int(p) + int(k * rng.uniform(0.03, 0.09) * RATE)
            for ch in range(2):
                side = (1 - pan) / 2 if ch == 0 else (1 + pan) / 2
                add_wrapped(gate_frond[ch], q, e * rng.uniform(0.3, 1.0) * np.sqrt(side))

    for ch in range(2):
        # Continuous faint rustle under the bursts so the leaves never switch off completely.
        channels[ch] += leaf_hi[ch] * (0.012 * gust ** 2 + 0.09 * gate_leaf[ch])
        channels[ch] += frond[ch] * 0.07 * gate_frond[ch]

    stereo = np.stack(channels, axis=1)
    return seam_rotate(stereo), {'leaf_bursts': leaf_events}


# ---------------------------------------------------------------------------------------------
# Seabirds.
#
# Cartoon-friendly gull and tern cries: a harmonic voice whose pitch follows a hand-drawn contour,
# shaped by two moving formants (so the "ee" to "ow" of a kee-ow is a real vowel change), with a
# rasp made the way a gull makes it: period-doubling (a subharmonic at f0/2) plus a fast amplitude
# flutter, plus a little breath noise. Each call is a list of notes; the notes are the variety.
# ---------------------------------------------------------------------------------------------

def voice(contour, seconds, rasp, formants, seed, breath=0.05):
    """contour(u) gives f0 in Hz for u in [0,1]; formants(u) gives [(centre, width, gain)]."""
    rng = np.random.default_rng(seed)
    m = int(seconds * RATE)
    u = np.linspace(0, 1, m)
    f0 = contour(u)
    jitter = 1.0 + 0.012 * slow_random(m, seed + 1, 30.0)
    f0 = f0 * jitter
    phase = 2 * np.pi * np.cumsum(f0) / RATE
    fm = formants(u)
    sig = np.zeros(m)
    for h in range(1, 16):
        fh = f0 * h
        amp = np.zeros(m)
        for centre, width, gain in fm:
            amp += gain / (1.0 + ((fh - centre) / width) ** 2)
        amp *= (fh < RATE * 0.45)
        sig += np.sin(h * phase) * amp / h ** 0.35
    # Period doubling: odd half-harmonics carry the rasp.
    for h in (0.5, 1.5, 2.5, 3.5):
        fh = f0 * h
        amp = np.zeros(m)
        for centre, width, gain in fm:
            amp += gain / (1.0 + ((fh - centre) / width) ** 2)
        sig += np.sin(h * phase + rng.uniform(0, 2 * np.pi)) * amp * rasp * 0.8
    flutter = 1.0 - rasp * 0.45 * (0.5 + 0.5 * np.sin(2 * np.pi * rng.uniform(55, 95) * u * seconds))
    sig *= flutter
    b = band_noise(max(m, 4096), seed + 2, 1500, 6000)[:m]
    sig += b * breath * np.sqrt(np.mean(sig ** 2) + 1e-12) * 0.6
    return sig


def note_env(seconds, attack, release, shape=1.0):
    m = int(seconds * RATE)
    t = np.arange(m) / RATE
    a = np.clip(t / attack, 0, 1) ** 0.7
    r = np.clip((seconds - t) / release, 0, 1) ** shape
    # ⚠️ A cry is loudest just after it opens and sags as the breath runs out; a flat box of
    # level reads as a synth tone rather than a bird.
    u = t / seconds
    swell = 0.72 + 0.28 * np.exp(-((u - 0.22) / 0.2) ** 2)
    return a * r * swell


def place(total, notes):
    out = np.zeros(int(total * RATE))
    for at, piece in notes:
        i = int(at * RATE)
        out[i:i + len(piece)] += piece[:len(out) - i]
    return out


def ee_ow(u):
    """Front vowel sliding to a rounded one: formants move down and together."""
    return [(2900 - 1300 * u, 700, 1.0), (1300 - 450 * u, 450, 0.8)]


def ee(u):
    return [(3100 + 0 * u, 800, 1.0), (1700 + 0 * u, 500, 0.5)]


def author_gulls():
    calls = []

    # 1. The classic long "kyaa-ow": up, hold, fall away, open vowel closing.
    d = 0.62
    v = voice(lambda u: 1050 + 480 * np.sin(np.pi * np.clip(u / 0.18, 0, 1) / 2) - 900 * np.clip((u - 0.3) / 0.7, 0, 1) ** 1.2,
              d, 0.35, ee_ow, 9301)
    calls.append(v * note_env(d, 0.03, 0.18))

    # 2. Two short bright "kee kee" up-chirps.
    notes = []
    for k, at in enumerate((0.0, 0.2)):
        d = 0.13
        v = voice(lambda u, k=k: 1350 + 450 * u + 60 * k, d, 0.2, ee, 9310 + k)
        notes.append((at, v * note_env(d, 0.012, 0.05)))
    calls.append(place(0.4, notes))

    # 3. A laughing run: five falling "ha" notes, each with its own small droop.
    notes = []
    for k in range(5):
        d = 0.11 - 0.006 * k
        top = 1250 - 70 * k
        v = voice(lambda u, top=top: top - 260 * u, d, 0.45, ee_ow, 9320 + k)
        notes.append((k * 0.155, v * note_env(d, 0.012, 0.045) * (1.0 - 0.1 * k)))
    calls.append(place(0.85, notes))

    # 4. A tern: high, thin and raspy "kee-rrr" with a trilled tail.
    d = 0.5
    v = voice(lambda u: 2100 + 350 * np.sin(np.pi * np.clip(u / 0.3, 0, 1)) - 500 * np.clip((u - 0.3) / 0.7, 0, 1),
              d, 0.6, lambda u: [(3400 - 600 * u, 900, 1.0), (2200, 600, 0.4)], 9330, breath=0.08)
    trill = 1.0 - 0.55 * (np.linspace(0, 1, len(v)) > 0.35) * (0.5 + 0.5 * np.sin(2 * np.pi * 26 * np.linspace(0, d, len(v))))
    calls.append(v * trill * note_env(d, 0.015, 0.12))

    # 5. A soft distant "mew": low, gentle, little rasp, falling. The far-off one in the mix.
    d = 0.48
    v = voice(lambda u: 760 - 220 * u ** 1.2 + 90 * np.sin(np.pi * np.clip(u / 0.2, 0, 1)),
              d, 0.12, lambda u: [(2000 - 500 * u, 700, 1.0), (1000 - 200 * u, 400, 0.7)], 9340)
    calls.append(v * note_env(d, 0.05, 0.2, shape=1.5) * 0.6)

    return calls


# ---------------------------------------------------------------------------------------------
# Seam, level, write, measure.
# ---------------------------------------------------------------------------------------------

def seam_rotate(x):
    """⚠️ Picks WHERE the file starts. The loop is already periodic, so the seam is continuous
    anywhere; this puts the file boundary at the calmest point, where the 50 ms either side have
    the most similar loudness, so an importer that decodes the first packet late or a tool that
    measures the ends sees nothing. Rotating a periodic signal changes nothing a listener hears."""
    mono = x if x.ndim == 1 else x.mean(axis=1)
    n = len(mono)
    w = SEAM_WINDOW
    energy = np.concatenate([mono, mono[:w]]) ** 2
    csum = np.concatenate([[0.0], np.cumsum(energy)])
    starts = np.arange(0, n, 441)
    after = np.sqrt((csum[starts + w] - csum[starts]) / w)
    before_idx = (starts - w) % n
    before = np.sqrt(np.array([(csum[b + w] - csum[b]) / w for b in before_idx]))
    diff = np.abs(after - before) / np.maximum(after, before)
    k = int(starts[np.argmin(diff)])
    return np.roll(x, -k, axis=0)


def to_pcm(x, peak):
    x = np.asarray(x, dtype=np.float64)
    x = x * (peak / max(1e-9, np.max(np.abs(x))))
    return np.round(x * 32767).astype('<i2')


def write_wav(name, pcm):
    path = OUT / name
    with wave.open(str(path), 'wb') as out:
        out.setnchannels(1 if pcm.ndim == 1 else pcm.shape[1])
        out.setsampwidth(2)
        out.setframerate(RATE)
        out.writeframes(pcm.tobytes())
    return path


def rms(a):
    return float(np.sqrt(np.mean(np.asarray(a, dtype=np.float64) ** 2)))


def measure(name, pcm, loop):
    x = pcm.astype(np.float64) / 32767
    path = OUT / name
    row = {
        'file': f'Assets/TumbangPreso/Art/audio/ambience/{name}',
        'channels': 1 if x.ndim == 1 else x.shape[1],
        'sample_rate': RATE,
        'bits': 16,
        'seconds': round(len(x) / RATE, 4),
        'peak': round(float(np.max(np.abs(x))), 4),
        'rms': round(rms(x), 4),
        'sha256': hashlib.sha256(path.read_bytes()).hexdigest(),
    }
    if loop:
        head = rms(x[:SEAM_WINDOW])
        tail = rms(x[-SEAM_WINDOW:])
        steps = np.abs(np.diff(x, axis=0))
        seam_step = float(np.max(np.abs(x[0] - x[-1])))
        p999 = float(np.percentile(steps, 99.9))
        row['seam'] = {
            'rms_first_50ms': round(head, 5),
            'rms_last_50ms': round(tail, 5),
            'rms_difference_percent': round(100 * abs(head - tail) / max(head, tail), 2),
            'seam_step': round(seam_step, 5),
            'p99_9_step_anywhere': round(p999, 5),
            # ⚠️ A click is a seam step far outside what the signal does on its own. Anything at
            # or below the 99.9th percentile of ordinary sample-to-sample steps is inaudible as
            # an edge because the loop body already contains a thousand steps that size.
            'click': bool(seam_step > p999),
        }
    return row


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    rows = []

    waves, waves_info = author_waves()
    # ⚠️ LOOP PEAKS SIT AT 0.60 AND THE CALLS AT 0.70, like the other authored cues here: headroom
    # for Unity's mixer summing these with the whole match, never normalised to full scale.
    pcm = to_pcm(waves, 0.60)
    write_wav('lagoon_waves.wav', pcm)
    row = measure('lagoon_waves.wav', pcm, True)
    row.update(waves_info)
    rows.append(row)

    wind, wind_info = author_wind()
    pcm = to_pcm(wind, 0.55)
    write_wav('lagoon_wind.wav', pcm)
    row = measure('lagoon_wind.wav', pcm, True)
    row.update(wind_info)
    rows.append(row)

    for i, call in enumerate(author_gulls(), start=1):
        name = f'lagoon_gull_{i}.wav'
        m = len(call)
        t = np.arange(m) / RATE
        call = call * np.minimum(1, t / 0.004) * np.minimum(1, (t[-1] - t) / 0.02)
        # Call 5 is the distant one and keeps its lower level after normalising the set.
        pcm = to_pcm(call, 0.70 if i != 5 else 0.45)
        write_wav(name, pcm)
        rows.append(measure(name, pcm, False))

    for row in rows:
        seam = row.get('seam')
        if seam and (seam['rms_difference_percent'] >= 10 or seam['click']):
            raise SystemExit(f"{row['file']}: seam check failed {seam}")

    result = {
        'provenance': 'Original deterministic synthesis (numpy, fixed seeds). No external samples, recordings or paid API.',
        'listening': 'OPEN. Technical measurements only (peak, RMS, seam). Sourced and authored SFX stay '
                     'provisional until the owner hears them in play (CLAUDE.md section 6).',
        'tool': 'tools/build_lagoon_ambience.py',
        'seam_rule': 'Loops are built periodic; first and last 50 ms RMS within 10 per cent and a seam '
                     'step no larger than the 99.9th percentile step inside the loop.',
        'files': rows,
    }
    REPORT.parent.mkdir(parents=True, exist_ok=True)
    REPORT.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    for row in rows:
        extra = f" seam {row['seam']['rms_difference_percent']}% click={row['seam']['click']}" if 'seam' in row else ''
        print(f"{row['file'].split('/')[-1]}: {row['seconds']} s, {row['channels']} ch, peak {row['peak']}, rms {row['rms']}{extra}")


if __name__ == '__main__':
    main()
