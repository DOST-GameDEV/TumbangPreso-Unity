"""Ilalim ng Tulay sidewalk life: the taho call, kids, bystanders, the beggar, the taho gear and the
pigeons, by deterministic synthesis.

Writes ONLY these files into Assets/TumbangPreso/Art/audio/ambience/ (44100 Hz, mono, 16-bit PCM,
each peak normalized to 0.70 full scale like the kanto_horn_*.wav one-shots beside them):
    sfx_taho_call_1..3.wav         the magtataho's sung street call "Ta-hoooooo!" (a man's voice)
    sfx_life_kid_giggle_1..3.wav   a child's giggle while playing tag (the _3 is a short shriek "eee!")
    sfx_life_kid_taya_1..3.wav     a child shouting "Taya!" ("it!" in tag)
    sfx_life_cheer_1..3.wav        one bystander's "Ayyy!" (man, woman) and "Ayos!" (man)
    sfx_life_clap_1..3.wav         one person clapping 4, 6 and 7 times
    sfx_life_groan_1..3.wav        a disappointed "Awww" (man, woman) and "Ay!" (man)
    sfx_life_salamat_1..3.wav      the beggar's soft, tired "salamat po"
    sfx_life_coin_tin_1..3.wav     a coin dropped into his condensed-milk can
    sfx_life_carton_1..3.wav       his flattened carton sat on, picked up, dragged
    sfx_life_bucket_1..3.wav       the taho buckets on the pole: lid clink, bamboo creak, both
    sfx_life_pigeon_coo_1..3.wav   a rock pigeon's coo phrase
    sfx_life_pigeon_flap_1..3.wav  pigeons flushing (the _3 is three birds at once)
and the measurement record into Logs/ilalim-unity/:
    life_sfx/<name>.png            spectrogram 0 to 8 kHz with a waveform strip, one per file
    life_sfx/report.txt            per file: duration, peak, RMS, clipping, end samples, end jumps,
                                   leading and trailing silence, median f0; for the taho calls the
                                   pitch at checkpoints and LPC F1/F2 against the design
    taho_call/<name>_analysis.png  designed formant tracks over the spectrogram, measured pitch
                                   (YIN, 20 ms hops) against the designed contour, fades marked

Owner briefs (2026-10-01, verbatim):
    "can you add a \"Tahoooooo\" voice sfx for the taho guy?"
    "as much as possible all these liveliness-adding character need sounds"

Same house method as build_kanto_street_audio.py: numpy and scipy only, fixed seeds, no recordings
and nothing downloaded, so a rerun is byte-identical and the provenance is simple to state. The
runtime that plays these is not this file's business; this file only authors them.

Run:  py -3 tools/synth_ilalim_life_sfx.py
      py -3 tools/synth_ilalim_life_sfx.py --only taho        (any substring of the file names)
      py -3 tools/synth_ilalim_life_sfx.py --no-plots         (WAVs and report only, faster)

HOW THE VOICES ARE MADE (a small Klatt-style formant synthesizer, see voice()):
    source   a Rosenberg glottal pulse per period, built at 4x the rate and decimated so the closure
             does not alias. The flow falls about -12 dB/oct (measured and reported for the taho
             calls); the synthesizer uses its derivative, which is the same as adding lip radiation
             (+6 dB/oct) at the output. Small per-period jitter and shimmer, slow pitch drift.
    breath   aspiration noise mixed in and pulsed by the glottal cycle (louder while the folds are
             open), so voiced sounds are breathy rather than a clean buzz.
    tract    a cascade of five time-varying resonators (F1..F5) plus a nasal pole-zero pair for /m/.
             Formant targets come from a phone list (one dict per sound segment) and glide between
             phones over each phone's `w` seconds.
    extras   /s/ is high-passed noise added after the tract; /t/ and /p/ are timed noise bursts.
             An outdoor slap echo and a short street tail finish the taho call; the other voices get
             only the tail.

TUNING KNOBS (all in the variant tables right below; each row is one output file):
    TAHO_CALLS    base_hz (the "ta" note), ta_s, h_s (the breath), hold_s (the "hooo" before its
                  fall, including the rise), fall_s and fall_st (the drop at the end), leap_st (the
                  jump up into "ho"), rise_st (the further rise), vib_hz and vib_st (vibrato),
                  oq (open quotient, lower is more pressed and brighter), breath (aspiration level),
                  slaps ((delay s, level dB) reflections), tail_db.
    KID_GIGGLE    base_hz; pulses as (vowel, semitones over base, vowel s, gap after s); a shriek
                  row uses dur_s and peak_st instead.
    KID_TAYA      base_hz, ta_s, ya_s, peak_st (the stressed "YA"), end_st (where it falls to).
    CHEER, GROAN  words ('ayi' is "Ayyy", 'ayos' is "Ayos", 'aw' is "Awww", 'ay' is "Ay"), sex,
                  base_hz, dur_s, rise_st, fall_st.
    SALAMAT       base_hz, tempo (stretches every phone), stress_st (pitch lift on "LA").
    CLAP          count, per_s (claps per second), jitter, accel, body_hz (hand cavity range).
    COIN_TIN      pitch (scales every metal partial), bounces ((gap s, level) after the first hit).
    CARTON        kind ('sit', 'lift', 'drag'), length_s.
    BUCKET        kind ('clink', 'creak', 'both'), creak_hz (stick-slip rate range).
    PIGEON_COO    base_hz; syllables as (s, start st, peak st, end st, gurgle depth, gap s, level).
    PIGEON_FLAP   birds as (start s, flaps, first interval s, interval ratio per flap, level).
"""
import argparse
import hashlib
from pathlib import Path
import wave

import numpy as np
from scipy import signal
from scipy.interpolate import PchipInterpolator
from scipy.linalg import solve_toeplitz

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/TumbangPreso/Art/audio/ambience'
LOGS = ROOT / 'Logs/ilalim-unity'
SPEC_DIR = LOGS / 'life_sfx'
TAHO_DIR = LOGS / 'taho_call'
RATE = 44100
PEAK = 0.70            # the kanto_horn_*.wav one-shots peak at exactly this
SOURCE_OVERSAMPLE = 4  # the glottal pulse is built at 176.4 kHz, then decimated
TRIM_DB = -50.0        # leading and trailing sound quieter than this (re peak) is trimmed away

# ---------------------------------------------------------------------------------------------
# VARIANT TABLES. One row per output file. Retune here; the builders below only read these.
# ---------------------------------------------------------------------------------------------

TAHO_CALLS = [
    dict(name='sfx_taho_call_1', seed=1101, base_hz=175.0, ta_s=0.17, h_s=0.080, hold_s=1.02,
         fall_s=0.32, fall_st=5.0, leap_st=4.0, rise_st=1.5, vib_hz=5.3, vib_st=0.40, oq=0.62, o_bw=1.7,
         breath=0.10, slaps=((0.105, -12.0), (0.160, -15.0)), tail_db=-27.0),
    dict(name='sfx_taho_call_2', seed=1102, base_hz=188.0, ta_s=0.16, h_s=0.070, hold_s=1.24,
         fall_s=0.36, fall_st=4.0, leap_st=5.0, rise_st=1.0, vib_hz=5.8, vib_st=0.33, oq=0.60, o_bw=1.6,
         breath=0.08, slaps=((0.125, -11.0), (0.175, -16.0)), tail_db=-28.0),
    dict(name='sfx_taho_call_3', seed=1103, base_hz=198.0, ta_s=0.19, h_s=0.090, hold_s=1.05,
         fall_s=0.28, fall_st=6.0, leap_st=3.0, rise_st=2.0, vib_hz=5.0, vib_st=0.48, oq=0.64, o_bw=1.7,
         breath=0.12, slaps=((0.090, -13.0),), tail_db=-26.0),
]

KID_GIGGLE = [
    dict(name='sfx_life_kid_giggle_1', seed=2101, kind='giggle', base_hz=360.0,
         pulses=[('i', 3.0, 0.060, 0.050), ('i', 2.0, 0.055, 0.045), ('i', 1.2, 0.055, 0.045),
                 ('i', 0.2, 0.055, 0.050), ('e', -0.8, 0.085, 0.0)]),
    dict(name='sfx_life_kid_giggle_2', seed=2102, kind='giggle', base_hz=305.0,
         pulses=[('e', 2.0, 0.070, 0.060), ('e', 1.0, 0.065, 0.055), ('e', -0.2, 0.070, 0.060),
                 ('a', -1.5, 0.115, 0.0)]),
    dict(name='sfx_life_kid_giggle_3', seed=2103, kind='shriek', base_hz=385.0, dur_s=0.52, peak_st=9.0),
]

KID_TAYA = [
    dict(name='sfx_life_kid_taya_1', seed=2201, base_hz=335.0, ta_s=0.100, ya_s=0.30, peak_st=4.0, end_st=-3.0),
    dict(name='sfx_life_kid_taya_2', seed=2202, base_hz=380.0, ta_s=0.090, ya_s=0.36, peak_st=3.0, end_st=-4.5),
    dict(name='sfx_life_kid_taya_3', seed=2203, base_hz=415.0, ta_s=0.085, ya_s=0.24, peak_st=5.0, end_st=-2.0),
]

CHEER = [
    dict(name='sfx_life_cheer_1', seed=2301, words='ayi', sex='male', base_hz=150.0, dur_s=0.72, rise_st=4.0, fall_st=5.0),
    dict(name='sfx_life_cheer_2', seed=2302, words='ayi', sex='female', base_hz=248.0, dur_s=0.62, rise_st=5.0, fall_st=6.0),
    dict(name='sfx_life_cheer_3', seed=2303, words='ayos', sex='male', base_hz=165.0, dur_s=0.66, rise_st=3.0, fall_st=4.0),
]

GROAN = [
    dict(name='sfx_life_groan_1', seed=2401, words='aw', sex='male', base_hz=135.0, dur_s=0.95, rise_st=0.8, fall_st=6.0),
    dict(name='sfx_life_groan_2', seed=2402, words='aw', sex='female', base_hz=228.0, dur_s=0.85, rise_st=1.0, fall_st=5.0),
    dict(name='sfx_life_groan_3', seed=2403, words='ay', sex='male', base_hz=150.0, dur_s=0.62, rise_st=0.5, fall_st=7.0),
]

SALAMAT = [
    dict(name='sfx_life_salamat_1', seed=2501, base_hz=110.0, tempo=1.00, stress_st=2.5),
    dict(name='sfx_life_salamat_2', seed=2502, base_hz=102.0, tempo=1.14, stress_st=2.0),
    dict(name='sfx_life_salamat_3', seed=2503, base_hz=114.0, tempo=1.28, stress_st=3.0),
]

CLAP = [
    dict(name='sfx_life_clap_1', seed=2601, count=4, per_s=4.4, jitter=0.10, accel=1.00, body_hz=(1100, 1700)),
    dict(name='sfx_life_clap_2', seed=2602, count=6, per_s=5.6, jitter=0.12, accel=1.00, body_hz=(1400, 2300)),
    dict(name='sfx_life_clap_3', seed=2603, count=7, per_s=4.6, jitter=0.08, accel=0.96, body_hz=(1000, 1500)),
]

COIN_TIN = [
    dict(name='sfx_life_coin_tin_1', seed=2701, pitch=1.00, bounces=((0.120, 0.42), (0.082, 0.25), (0.055, 0.14))),
    dict(name='sfx_life_coin_tin_2', seed=2702, pitch=0.93, bounces=((0.150, 0.50), (0.100, 0.30), (0.068, 0.18), (0.045, 0.10))),
    dict(name='sfx_life_coin_tin_3', seed=2703, pitch=1.08, bounces=((0.095, 0.35), (0.060, 0.18), (0.040, 0.09))),
]

CARTON = [
    dict(name='sfx_life_carton_1', seed=2801, kind='sit', length_s=0.62),
    dict(name='sfx_life_carton_2', seed=2802, kind='lift', length_s=0.78),
    dict(name='sfx_life_carton_3', seed=2803, kind='drag', length_s=0.50),
]

BUCKET = [
    dict(name='sfx_life_bucket_1', seed=2901, kind='clink', creak_hz=(0, 0)),
    dict(name='sfx_life_bucket_2', seed=2902, kind='creak', creak_hz=(130, 280)),
    dict(name='sfx_life_bucket_3', seed=2903, kind='both', creak_hz=(150, 240)),
]

PIGEON_COO = [
    dict(name='sfx_life_pigeon_coo_1', seed=3001, base_hz=300.0,
         syllables=[(0.16, -1.5, 1.0, -1.0, 0.0, 0.060, 0.7), (0.46, -1.0, 2.0, -2.5, 0.75, 0.070, 1.0),
                    (0.26, -0.5, 0.8, -3.0, 0.0, 0.0, 0.75)]),
    dict(name='sfx_life_pigeon_coo_2', seed=3002, base_hz=268.0,
         syllables=[(0.52, -2.0, 1.5, -2.0, 0.65, 0.080, 1.0), (0.34, -1.0, 0.5, -3.5, 0.3, 0.0, 0.8)]),
    dict(name='sfx_life_pigeon_coo_3', seed=3003, base_hz=335.0,
         syllables=[(0.14, -1.0, 1.2, 0.0, 0.0, 0.045, 0.65), (0.40, -0.5, 2.2, -2.0, 0.8, 0.060, 1.0),
                    (0.18, 0.0, 1.0, -1.0, 0.0, 0.050, 0.7), (0.30, -0.5, 0.5, -3.5, 0.4, 0.0, 0.8)]),
]

PIGEON_FLAP = [
    dict(name='sfx_life_pigeon_flap_1', seed=3101, birds=[(0.0, 8, 0.112, 0.965, 1.0)]),
    dict(name='sfx_life_pigeon_flap_2', seed=3102, birds=[(0.0, 11, 0.105, 0.975, 1.0)]),
    dict(name='sfx_life_pigeon_flap_3', seed=3103, birds=[(0.0, 9, 0.100, 0.97, 1.0), (0.065, 9, 0.092, 0.97, 0.75),
                                                        (0.170, 8, 0.108, 0.965, 0.55)]),
]

# Adult male formant targets F1, F2, F3 (Hz). Women scale by 1.15, children by 1.2 (the brief).
VOWELS = {
    'a': (730, 1150, 2500),
    'aw': (610, 960, 2450),   # the open "aw" of a groan
    'o': (480, 880, 2400),
    'oc': (455, 850, 2380),   # the same /o/ a touch more closed, where a held "hooo" drifts
    'e': (480, 1850, 2550),
    'i': (300, 2250, 3000),
    'y': (320, 1920, 2700),   # the glide /j/
    'l': (350, 1100, 2600),
    'm': (250, 1100, 2200),
}
F45 = (3400.0, 4200.0)
BANDWIDTHS = (80.0, 90.0, 150.0, 250.0, 300.0)
NASAL_POLE_HZ, NASAL_BW = 270.0, 100.0
SPEAKER_SCALE = {'male': 1.0, 'female': 1.15, 'child': 1.2, 'old': 0.97}


# ---------------------------------------------------------------------------------------------
# Small numpy/scipy helpers.
# ---------------------------------------------------------------------------------------------

def secs(n):
    return int(round(n * RATE))


def white(n, seed):
    return np.random.default_rng(seed).standard_normal(n)


def sos_filter(x, kind, freq, order=2):
    return signal.sosfilt(signal.butter(order, freq, kind, fs=RATE, output='sos'), x)


def bandpass(x, lo, hi, order=2):
    return sos_filter(x, 'bandpass', [lo, hi], order)


def lowpass(x, hi, order=2):
    return sos_filter(x, 'lowpass', hi, order)


def highpass(x, lo, order=2):
    return sos_filter(x, 'highpass', lo, order)


def peak_filter(x, f, q):
    b, a = signal.iirpeak(f, q, fs=RATE)
    return signal.lfilter(b, a, x)


def smoothstep(u):
    u = np.clip(u, 0.0, 1.0)
    return u * u * (3.0 - 2.0 * u)


def rms(x):
    return float(np.sqrt(np.mean(np.square(x)))) if len(x) else 0.0


def slow_random(n, seed, hi_hz):
    """A smooth random curve, unit RMS, with nothing faster than about hi_hz."""
    rng = np.random.default_rng(seed)
    spec = rng.normal(size=n // 2 + 1) + 1j * rng.normal(size=n // 2 + 1)
    spec *= np.exp(-(np.fft.rfftfreq(n, 1.0 / RATE) / hi_hz) ** 2)
    spec[0] = 0.0
    out = np.fft.irfft(spec, n)
    return out / (rms(out) + 1e-12)


def place(dst, start_s, piece, gain=1.0):
    i = secs(start_s)
    m = min(len(piece), len(dst) - i)
    if m > 0:
        dst[i:i + m] += gain * piece[:m]


def decay_env(n, attack_s, tau_s):
    t = np.arange(n) / RATE
    a = max(attack_s, 1.0 / RATE)
    rise = 0.5 - 0.5 * np.cos(np.pi * np.clip(t / a, 0.0, 1.0))
    return rise * np.exp(-np.maximum(t - a, 0.0) / tau_s)


def modes(partials, seconds, seed, attack_s=0.0004):
    """A struck object: sum of (freq Hz, decay tau s, level) sinusoids with random phases."""
    rng = np.random.default_rng(seed)
    n = secs(seconds)
    t = np.arange(n) / RATE
    out = np.zeros(n)
    for f, tau, amp in partials:
        out += amp * np.sin(2 * np.pi * f * t + rng.uniform(0, 2 * np.pi)) * np.exp(-t / tau)
    return out * (0.5 - 0.5 * np.cos(np.pi * np.clip(t / attack_s, 0, 1)))


# ---------------------------------------------------------------------------------------------
# The voice: phones -> parameter tracks -> glottal source -> formant cascade.
# ---------------------------------------------------------------------------------------------

def phone_tracks(phones, scale=1.0, bw_scale=1.0):
    """Turns a phone list into per-sample tracks.

    Each phone is a dict: v (vowel key, or F=(F1,F2,F3) raw Hz), d (seconds), av (voicing 0..1),
    ah (aspiration), af (frication, relative to a full vowel's RMS), w (seconds of formant glide
    centred on the boundary INTO this phone, default 0.03), wa (the same for the amplitudes,
    default 0.01), nz (nasal zero Hz; leave out for no nasality), bw (bandwidth multiplier).
    Returns (t, tracks, bounds) with bounds the (start, end) seconds of every phone.
    """
    total = sum(p['d'] for p in phones)
    n = secs(total)
    t = np.arange(n) / RATE
    keys = ('F1', 'F2', 'F3', 'F4', 'F5', 'bwm', 'av', 'ah', 'af', 'nz')
    pts = {k: [] for k in keys}
    s = 0.0
    bounds = []
    for i, p in enumerate(phones):
        e = s + p['d']
        nxt = phones[i + 1] if i + 1 < len(phones) else {}
        half = p['d'] / 2.0
        a0 = s + min(p.get('w', 0.03) / 2.0, half) if i else s
        a1 = e - min(nxt.get('w', 0.03) / 2.0, half) if nxt else e
        b0 = s + min(p.get('wa', 0.01) / 2.0, half) if i else s
        b1 = e - min(nxt.get('wa', 0.01) / 2.0, half) if nxt else e
        raw = p.get('F') or VOWELS[p['v']]
        fs = [f * scale for f in raw] + [f * scale for f in F45]
        for k, f in zip(keys[:5], fs):
            pts[k] += [(a0, f), (a1, f)]
        pts['bwm'] += [(a0, p.get('bw', 1.0)), (a1, p.get('bw', 1.0))]
        pts['nz'] += [(a0, p.get('nz', NASAL_POLE_HZ)), (a1, p.get('nz', NASAL_POLE_HZ))]
        for k in ('av', 'ah', 'af'):
            pts[k] += [(b0, p.get(k, 0.0)), (b1, p.get(k, 0.0))]
        bounds.append((s, e))
        s = e
    tracks = {}
    smooth = np.hanning(secs(0.008))
    smooth /= smooth.sum()
    for k in keys:
        tt = np.array([q[0] for q in pts[k]])
        vv = np.array([q[1] for q in pts[k]])
        tt = tt + np.arange(len(tt)) * 1e-9  # strictly increasing for interp
        tr = np.interp(t, tt, vv)
        if k not in ('av', 'ah', 'af'):
            pad = len(smooth)
            tr = np.convolve(np.pad(tr, pad, mode='edge'), smooth, mode='same')[pad:-pad]
        tracks[k] = tr
    end = 1.0 - smoothstep((t - (total - 0.015)) / 0.015)  # nothing may stop dead at the last sample
    for k in ('av', 'ah', 'af'):
        tracks[k] = tracks[k] * end
    tracks['bw'] = [np.full(n, b * bw_scale) * tracks['bwm'] for b in BANDWIDTHS]
    return t, tracks, bounds


def pitch_contour(t, base_hz, keys, vib=None, drift_st=0.0, drift_hz=1.5, seed=0):
    """The designed f0 track (Hz). keys are (seconds, semitones over base_hz), joined by a monotone
    cubic. vib = (rate Hz, depth st, on s, full s, off s, gone s): vibrato fades in between on and
    full and out between off and gone, and its rate wanders 4 per cent. Drift is a slow random bend
    of drift_st semitones RMS. All of it is the design; jitter is added later, in the source."""
    kt = np.array([k[0] for k in keys])
    ks = np.array([k[1] for k in keys])
    st = PchipInterpolator(kt, ks)(np.clip(t, kt[0], kt[-1]))
    if vib:
        rate, depth, on, full, off, gone = vib
        env = smoothstep((t - on) / max(full - on, 1e-3)) * (1.0 - smoothstep((t - off) / max(gone - off, 1e-3)))
        wander = 1.0 + 0.04 * slow_random(len(t), seed + 7, 0.8)
        st = st + depth * env * np.sin(2 * np.pi * np.cumsum(rate * wander) / RATE)
    if drift_st:
        st = st + drift_st * slow_random(len(t), seed + 11, drift_hz)
    return base_hz * 2.0 ** (st / 12.0)


def glottal_source(f0, seed, oq, jitter, shimmer):
    """Rosenberg pulses, one per period, at SOURCE_OVERSAMPLE times the rate, then decimated.
    Returns (flow derivative, flow 0..1). Open phase = oq of the period: 62 per cent opening,
    38 per cent closing, then closed; the abrupt closure is the main excitation."""
    n = len(f0)
    rng = np.random.default_rng(seed)
    starts, periods, amps = [], [], []
    t, total = 0.0, n / RATE
    while t < total:
        f = f0[min(int(t * RATE), n - 1)] * (1.0 + jitter * rng.standard_normal())
        starts.append(t)
        periods.append(1.0 / f)
        amps.append(max(0.2, 1.0 + shimmer * rng.standard_normal()))
        t += 1.0 / f
    starts, periods, amps = np.array(starts), np.array(periods), np.array(amps)
    m = n * SOURCE_OVERSAMPLE
    ts = np.arange(m) / (RATE * SOURCE_OVERSAMPLE)
    k = np.searchsorted(starts, ts, side='right') - 1
    p = (ts - starts[k]) / periods[k]
    tp, tn = oq * 0.62, oq * 0.38
    opening, closing = p < tp, (p >= tp) & (p < tp + tn)
    flow = np.zeros(m)
    dflow = np.zeros(m)
    flow[opening] = 0.5 * (1 - np.cos(np.pi * p[opening] / tp))
    flow[closing] = np.cos(0.5 * np.pi * (p[closing] - tp) / tn)
    dflow[opening] = 0.5 * np.pi / tp * np.sin(np.pi * p[opening] / tp)
    dflow[closing] = -0.5 * np.pi / tn * np.sin(0.5 * np.pi * (p[closing] - tp) / tn)
    dflow *= amps[k]
    flow *= amps[k]
    dflow = signal.resample_poly(dflow, 1, SOURCE_OVERSAMPLE)[:n]
    flow = signal.resample_poly(flow, 1, SOURCE_OVERSAMPLE)[:n]
    return dflow, np.clip(flow, 0.0, 1.5)


def tv_filter(x, freq, bw, anti=False, block=32):
    """A Klatt resonator (unity gain at DC) whose frequency and bandwidth follow per-sample tracks,
    updated every block samples with the filter state carried across. anti=True is the matching
    antiresonator (a zero), used for the nasal: an FIR, so it is computed per sample with no state
    and a moving zero cannot ring (a stateful moving zero put clicks at the edges of /m/)."""
    if anti:
        r = np.exp(-np.pi * bw / RATE)
        c = -r * r
        b = 2 * r * np.cos(2 * np.pi * freq / RATE)
        x1 = np.concatenate([[0.0], x[:-1]])
        x2 = np.concatenate([[0.0, 0.0], x[:-2]])
        return (x - b * x1 - c * x2) / (1.0 - b - c)
    y = np.empty_like(x)
    zi = np.zeros(2)
    for s in range(0, len(x), block):
        e = min(s + block, len(x))
        m = (s + e) // 2
        r = np.exp(-np.pi * bw[m] / RATE)
        c = -r * r
        b = 2 * r * np.cos(2 * np.pi * freq[m] / RATE)
        a0 = 1.0 - b - c
        if anti:
            num, den = np.array([1.0, -b, -c]) / a0, np.array([1.0])
        else:
            num, den = np.array([a0, 0.0, 0.0]), np.array([1.0, -b, -c])
        y[s:e], zi = signal.lfilter(num, den, x[s:e], zi=zi)
    return y


def voice(tr, f0, seed, oq=0.6, jitter=0.005, shimmer=0.035, tilt_hz=0.0, breath_mod=0.6,
          asp_gain=1.0, fric_band=(4000.0, 8000.0), bursts=()):
    """Synthesizes one utterance from phone_tracks() output and a designed f0 track.
    bursts: (time s, length s, level re vowel RMS, lo Hz, hi Hz), the release of /t/ /p/ /k/.
    Returns (signal, flow) with flow the glottal flow, kept for the tilt measurement."""
    n = len(f0)
    dflow, flow = glottal_source(f0, seed, oq, jitter, shimmer)
    voiced = tr['av'] > 0.5
    src = dflow / (rms(dflow[voiced]) if voiced.any() else rms(dflow) + 1e-12)
    if tilt_hz:
        a = np.exp(-2 * np.pi * tilt_hz / RATE)
        src = signal.lfilter([1 - a], [1, -a], src)
        src /= rms(src[voiced]) if voiced.any() else 1.0
    noise = highpass(white(n, seed + 1), 400.0)
    noise /= rms(noise)
    vo = np.clip(tr['av'], 0.0, 1.0)
    pulse = 1.0 - breath_mod * vo * (1.0 - np.clip(flow, 0.0, 1.0))
    exc = src * tr['av'] + asp_gain * 0.35 * tr['ah'] * noise * pulse
    x = tv_filter(exc, np.full(n, NASAL_POLE_HZ), np.full(n, NASAL_BW))
    x = tv_filter(x, tr['nz'], np.full(n, NASAL_BW), anti=True)
    for k in range(5):
        x = tv_filter(x, tr['F%d' % (k + 1)], tr['bw'][k])
    ref = rms(x[voiced]) if voiced.any() else rms(x)
    if np.any(tr['af'] > 0):
        fr = bandpass(white(n, seed + 2), *fric_band, order=3)
        x = x + tr['af'] * ref * fr / (rms(fr) + 1e-12)
    for i, (at, dur, level, lo, hi) in enumerate(bursts):
        m = secs(dur * 3)
        b = bandpass(white(m, seed + 30 + i), lo, hi, order=4) * decay_env(m, 0.0008, dur / 2.5)
        place(x, at, b / (np.max(np.abs(b)) + 1e-12), level * ref * 2.5)
    return x, flow


def outdoor(x, slaps=(), slap_lp=2400.0, tail_db=-28.0, tail_s=0.32, seed=0):
    """A street under a bridge: discrete slap reflections (delay s, level dB), lowpassed, and a
    short diffuse tail (tail_s to -60 dB) at tail_db re the dry sound."""
    longest = max([d for d, _ in slaps] + [0.0]) + tail_s + 0.02
    out = np.concatenate([x, np.zeros(secs(longest))])
    dull = lowpass(x, slap_lp)
    for d, db in slaps:
        place(out, d, dull, 10 ** (db / 20.0))
    if tail_db is not None:
        m = secs(tail_s)
        ir = lowpass(white(m, seed + 99), 3500.0) * np.exp(-6.9 * np.arange(m) / m)
        ir = np.concatenate([np.zeros(secs(0.018)), ir / np.sqrt(np.sum(ir ** 2))])
        wet = signal.fftconvolve(x, ir)[:len(out)]
        out[:len(wet)] += wet * 10 ** (tail_db / 20.0)
    return out


def speaker_voice(sex):
    """oq, jitter, shimmer, breath_mod, bandwidth multiplier per speaker kind."""
    return {'male': (0.58, 0.005, 0.035, 0.6, 1.0), 'female': (0.64, 0.005, 0.035, 0.6, 1.15),
            'child': (0.62, 0.007, 0.05, 0.55, 1.3), 'old': (0.72, 0.014, 0.08, 0.7, 1.25)}[sex]


# ---------------------------------------------------------------------------------------------
# 1. The magtataho's "Ta-hoooooo!"
# ---------------------------------------------------------------------------------------------

def build_taho(c):
    ta, h, hold, fall = c['ta_s'], c['h_s'], c['hold_s'], c['fall_s']
    closure, asp = 0.012, 0.022
    phones = [
        dict(v='a', d=closure, wa=0.001),                               # /t/ closure, silent
        dict(v='a', d=asp, ah=0.55, wa=0.002),                          # aspiration after the burst
        dict(v='a', d=ta - closure - asp, av=1.0, ah=c['breath'], wa=0.012),
        dict(v='o', d=h, av=0.22, ah=0.9, w=h * 1.3, wa=0.03, bw=c['o_bw']),          # /h/, already shaped as /o/
        dict(v='o', d=hold * 0.5, av=1.0, ah=c['breath'], w=0.05, wa=0.035, bw=c['o_bw']),
        dict(v='oc', d=hold * 0.5 + fall, av=1.0, ah=c['breath'], w=hold * 0.5, bw=c['o_bw']),
    ]
    t, tr, bounds = phone_tracks(phones)
    t_a0, t_h0 = closure + asp, ta
    t_o0, t_f0 = ta + h, ta + h + hold
    t_end = t_f0 + fall
    top = c['leap_st'] + c['rise_st']
    keys = [(0.0, -0.8), (t_a0, -0.6), (ta * 0.7, 0.1), (t_h0, 0.25), (t_h0 + h * 0.85, c['leap_st']),
            (t_o0 + 0.38, top), (t_f0, top), (t_f0 + fall * 0.45, top - 0.35 * c['fall_st']),
            (t_end, top - c['fall_st'])]
    vib = (c['vib_hz'], c['vib_st'], t_o0 + 0.18, t_o0 + 0.55, t_f0 + fall * 0.2, t_end)
    f0 = pitch_contour(t, c['base_hz'], keys, vib=vib, drift_st=0.12, seed=c['seed'])
    # The fall: voicing decays through the last fall_s, breath lingers a little longer.
    u = np.clip((t - t_f0) / fall, 0.0, 1.0)
    tr['av'] = tr['av'] * (1.0 - smoothstep(u)) ** 0.8
    tr['ah'] = tr['ah'] * (1.0 - smoothstep(u)) ** 0.6
    bursts = [(closure, 0.014, 0.28, 2000.0, 5500.0)]
    x, flow = voice(tr, f0, c['seed'], oq=c['oq'], jitter=0.004, shimmer=0.03, breath_mod=0.6,
                    bursts=bursts)
    dry_len = len(x)
    x = outdoor(x, slaps=c['slaps'], tail_db=c['tail_db'], seed=c['seed'])
    design = dict(t=t, f0=f0, tracks=tr, flow=flow, dry_len=dry_len,
                  seg=dict(a=(t_a0, t_h0), h=(t_h0, t_o0), hold=(t_o0, t_f0), fall=(t_f0, t_end)))
    return x, dict(voiced=True, fade_in=0.005, fade_out=0.05, design=design)


# ---------------------------------------------------------------------------------------------
# 2 and 3. Kids at tag.
# ---------------------------------------------------------------------------------------------

def build_giggle(c):
    oq, jit, shim, bm, bws = speaker_voice('child')
    sc = SPEAKER_SCALE['child']
    if c['kind'] == 'shriek':
        d = c['dur_s']
        # A shriek at 500 to 650 Hz only rings if F1 sits near the fundamental, as it does in a
        # child's (and a singer's) high voice, so the "eee" is an open, bright /e/, F1 near 620 Hz.
        bright = (520, 2050, 2850)
        phones = [dict(F=bright, d=0.03, av=0.5, ah=0.6), dict(F=bright, d=d * 0.45, av=1.0, ah=0.08, wa=0.03),
                  dict(v='e', d=d * 0.55 - 0.03, av=1.0, ah=0.12, w=0.2)]
        t, tr, _ = phone_tracks(phones, sc, bws)
        pk = c['peak_st']
        keys = [(0, 0.0), (0.12 * d, pk * 0.75), (0.45 * d, pk), (0.8 * d, pk - 2.5), (d, pk - 7.0)]
        f0 = pitch_contour(t, c['base_hz'], keys, vib=(9.0, 0.35, 0.1, 0.25, d * 0.7, d), drift_st=0.2, seed=c['seed'])
        tr['av'] *= 1.0 - smoothstep((t - 0.72 * d) / (0.28 * d))
        tr['ah'] *= 1.0 - 0.6 * smoothstep((t - 0.72 * d) / (0.28 * d))
        x, _ = voice(tr, f0, c['seed'], oq=0.45, jitter=jit, shimmer=shim, breath_mod=0.4)
    else:
        phones, keys, at = [], [], 0.0
        for v, st, dv, gap in c['pulses']:
            phones.append(dict(v=v, d=0.024, av=0.12, ah=1.0, wa=0.012))
            phones.append(dict(v=v, d=dv, av=1.0, ah=0.35, wa=0.014))
            keys += [(at + 0.024, st - 0.8), (at + 0.024 + dv * 0.4, st + 1.2), (at + 0.024 + dv, st - 1.2)]
            at += 0.024 + dv
            if gap:
                phones.append(dict(v=v, d=gap, av=0.0, ah=0.05, wa=0.02))
                at += gap
        keys = [(0.0, keys[0][1])] + keys
        t, tr, _ = phone_tracks(phones, sc, bws)
        f0 = pitch_contour(t, c['base_hz'], keys, drift_st=0.15, seed=c['seed'])
        x, _ = voice(tr, f0, c['seed'], oq=oq, jitter=jit, shimmer=shim, breath_mod=bm, asp_gain=1.3)
    x = outdoor(x, tail_db=-30.0, tail_s=0.22, seed=c['seed'])
    return x, dict(voiced=True, fade_in=0.008, fade_out=0.035)


def build_taya(c):
    oq, jit, shim, bm, bws = speaker_voice('child')
    closure, asp = 0.008, 0.018
    ta, ya = c['ta_s'], c['ya_s']
    phones = [dict(v='a', d=closure, wa=0.001), dict(v='a', d=asp, ah=0.5, wa=0.002),
              dict(v='a', d=ta, av=0.85, ah=0.12, wa=0.01),
              dict(v='y', d=0.06, av=0.9, ah=0.1, w=0.05),
              dict(v='a', d=ya, av=1.0, ah=0.12, w=0.06)]
    t, tr, _ = phone_tracks(phones, SPEAKER_SCALE['child'], bws)
    t_y = closure + asp + ta
    t_end = t_y + 0.06 + ya
    keys = [(0.0, 0.0), (closure + asp, 0.0), (t_y, 0.6), (t_y + 0.06, c['peak_st'] - 1.0),
            (t_y + 0.06 + ya * 0.3, c['peak_st']), (t_end, c['end_st'])]
    f0 = pitch_contour(t, c['base_hz'], keys, drift_st=0.15, seed=c['seed'])
    u = np.clip((t - (t_y + 0.06 + ya * 0.45)) / (ya * 0.55), 0, 1)
    tr['av'] *= (1 - smoothstep(u)) ** 1.3
    tr['ah'] *= (1 - smoothstep(u)) ** 0.8
    x, _ = voice(tr, f0, c['seed'], oq=0.5, jitter=jit, shimmer=shim, breath_mod=bm,
                 bursts=[(closure, 0.012, 0.8, 2500.0, 6500.0)])
    x = outdoor(x, slaps=((0.095, -18.0),), tail_db=-29.0, tail_s=0.22, seed=c['seed'])
    return x, dict(voiced=True, fade_in=0.005, fade_out=0.03)


# ---------------------------------------------------------------------------------------------
# 4 and 6. Bystanders: cheers and groans.
# ---------------------------------------------------------------------------------------------

def build_exclaim(c, groan=False):
    oq, jit, shim, bm, bws = speaker_voice(c['sex'])
    sc = SPEAKER_SCALE[c['sex']]
    d = c['dur_s']
    breath = 0.35 if groan else 0.14
    if groan:
        oq = min(0.75, oq + 0.1)
    w = c['words']
    if w == 'ayi':
        phones = [dict(v='a', d=0.03, av=0.4, ah=0.5), dict(v='a', d=d * 0.36, av=1.0, ah=breath),
                  dict(v='i', d=d * 0.64 - 0.03, av=1.0, ah=breath, w=d * 0.3)]
    elif w == 'ayos':
        phones = [dict(v='a', d=0.03, av=0.4, ah=0.5), dict(v='a', d=d * 0.25, av=1.0, ah=breath),
                  dict(v='y', d=0.07, av=0.9, ah=breath, w=0.05), dict(v='o', d=d * 0.40, av=1.0, ah=breath, w=0.07),
                  dict(v='i', F=(400, 1500, 2600), d=d * 0.35 - 0.10, av=0.0, ah=0.12, af=0.2, wa=0.03, w=0.06)]
    elif w == 'aw':
        phones = [dict(v='a', d=0.04, av=0.3, ah=0.8), dict(v='a', d=d * 0.2, av=1.0, ah=breath),
                  dict(v='aw', d=d * 0.35, av=1.0, ah=breath, w=d * 0.2), dict(v='o', d=d * 0.45 - 0.04, av=1.0, ah=breath, w=d * 0.3)]
    else:  # 'ay'
        phones = [dict(v='a', d=0.03, av=0.4, ah=0.7), dict(v='a', d=d * 0.42, av=1.0, ah=breath),
                  dict(v='e', F=(360, 2100, 2800), d=d * 0.58 - 0.03, av=1.0, ah=breath, w=d * 0.35)]
    t, tr, bounds = phone_tracks(phones, sc, bws)
    T = t[-1]
    r, f = c['rise_st'], c['fall_st']
    if groan:
        keys = [(0, r * 0.4), (0.12 * T, r), (0.45 * T, r - f * 0.45), (T, r - f)]
        env_at, env_len = 0.5, 0.5
    elif w == 'ayos':
        t_o = bounds[3][0]
        keys = [(0, -1.0), (bounds[1][1], r * 0.4), (t_o + 0.05, r), (bounds[3][1], r - f)]
        env_at, env_len = 0.62, 0.3
    else:
        keys = [(0, -1.0), (0.28 * T, r), (0.55 * T, r - 0.6), (T, r - f)]
        env_at, env_len = 0.55, 0.45
    f0 = pitch_contour(t, c['base_hz'], keys, drift_st=0.15, seed=c['seed'])
    u = np.clip((t - env_at * T) / (env_len * T), 0, 1)
    tr['av'] *= (1 - smoothstep(u)) ** 1.1
    tr['ah'] *= (1 - smoothstep(u)) ** (0.6 if groan else 0.9)
    x, _ = voice(tr, f0, c['seed'], oq=oq, jitter=jit, shimmer=shim, breath_mod=bm,
                 tilt_hz=1800.0 if groan else 0.0, asp_gain=1.2 if groan else 1.0)
    x = outdoor(x, slaps=((0.11, -19.0),), tail_db=-29.0, tail_s=0.25, seed=c['seed'])
    return x, dict(voiced=True, fade_in=0.010, fade_out=0.045)


# ---------------------------------------------------------------------------------------------
# 7. The beggar's "salamat po".
# ---------------------------------------------------------------------------------------------

def build_salamat(c):
    oq, jit, shim, bm, bws = speaker_voice('old')
    k = c['tempo']
    ph = [  # (label, phone)
        ('s', dict(v='a', d=0.10 * k, af=0.13, ah=0.0, wa=0.02)),
        ('a1', dict(v='a', d=0.075 * k, av=0.6, ah=0.35, wa=0.02)),
        ('l', dict(v='l', d=0.05 * k, av=0.5, ah=0.25, w=0.03)),
        ('A', dict(v='a', d=0.16 * k, av=1.0, ah=0.3, w=0.035, wa=0.02)),
        ('m', dict(v='m', d=0.07 * k, av=0.5, ah=0.05, nz=520.0, bw=1.6, w=0.025, wa=0.02)),
        ('a2', dict(v='a', d=0.09 * k, av=0.75, ah=0.3, w=0.03, wa=0.02)),
        ('tcl', dict(v='a', d=0.05 * k, av=0.0, ah=0.0, wa=0.006)),
        ('trel', dict(v='a', d=0.012, ah=0.15, wa=0.003)),
        ('pcl', dict(v='o', d=0.045 * k, av=0.0, ah=0.0, wa=0.004)),
        ('o', dict(v='o', d=0.24 * k, av=0.7, ah=0.4, w=0.02, wa=0.01)),
    ]
    labels = [p[0] for p in ph]
    t, tr, bounds = phone_tracks([p[1] for p in ph], SPEAKER_SCALE['old'], bws)
    B = dict(zip(labels, bounds))
    sl = c['stress_st']
    keys = [(0.0, 0.0), (B['a1'][0], 0.0), (B['a1'][1], 0.4), (B['A'][0] + 0.03, sl), (B['A'][1], sl - 0.8),
            (B['a2'][1], -1.2), (B['o'][0], -1.8), (B['o'][1], -4.5)]
    f0 = pitch_contour(t, c['base_hz'], keys, drift_st=0.25, seed=c['seed'])
    u = np.clip((t - B['o'][0] - 0.04 * k) / (B['o'][1] - B['o'][0] - 0.04 * k), 0, 1)
    tr['av'] *= (1 - smoothstep(u)) ** 1.2
    tr['ah'] *= (1 - smoothstep(u)) ** 0.6
    bursts = [(B['trel'][0], 0.010, 0.16, 2000.0, 5000.0), (B['pcl'][1], 0.010, 0.08, 500.0, 2000.0)]
    x, _ = voice(tr, f0, c['seed'], oq=oq, jitter=jit, shimmer=shim, breath_mod=bm, tilt_hz=1400.0,
                 asp_gain=1.3, fric_band=(4000.0, 8000.0), bursts=bursts)
    x = outdoor(x, tail_db=-31.0, tail_s=0.2, seed=c['seed'])
    return x, dict(voiced=True, fade_in=0.012, fade_out=0.05)


# ---------------------------------------------------------------------------------------------
# 5. Clapping.
# ---------------------------------------------------------------------------------------------

def build_clap(c):
    rng = np.random.default_rng(c['seed'])
    times, at, gap = [], 0.006, 1.0 / c['per_s']
    for i in range(c['count']):
        times.append(at)
        at += gap * (1 + rng.uniform(-c['jitter'], c['jitter']))
        gap *= c['accel']
    x = np.zeros(secs(times[-1] + 0.2))
    for i, tc in enumerate(times):
        m = secs(0.09)
        n0 = white(m, c['seed'] * 10 + i)
        burst = bandpass(n0, 800.0, 3600.0, order=3) * (decay_env(m, 0.0012, 0.0085) + 0.12 * decay_env(m, 0.002, 0.03))
        body = peak_filter(burst, rng.uniform(*c['body_hz']), 2.2)
        thump = lowpass(n0, 350.0) * decay_env(m, 0.001, 0.012)
        clap = 0.6 * burst + body + 0.25 * thump / (np.max(np.abs(thump)) + 1e-9) * np.max(np.abs(body))
        place(x, tc, clap, rng.uniform(0.82, 1.0))
    x = outdoor(x, slaps=((0.048, -16.0), (0.11, -21.0)), slap_lp=3000.0, tail_db=-28.0, tail_s=0.2, seed=c['seed'])
    return x, dict(voiced=False, fade_in=0.004, fade_out=0.04)


# ---------------------------------------------------------------------------------------------
# 8. Coin into a condensed-milk can.
# ---------------------------------------------------------------------------------------------

def build_coin(c):
    """The coin's own partials ring short and high; the thin can wall adds a spread of brighter
    shell modes; the can body rings lower and longer. Each bounce lands the coin on another edge,
    so its partials shift a few per cent and the body is hit more softly."""
    rng = np.random.default_rng(c['seed'])
    k = c['pitch']
    coin = [(2900, 0.035, 1.0), (4300, 0.025, 0.8), (5600, 0.018, 0.6), (6900, 0.014, 0.45)]
    wall = [(f, rng.uniform(0.02, 0.05), rng.uniform(0.2, 0.45)) for f in np.sort(rng.uniform(1800, 5500, 7))]
    body = [(980, 0.10, 0.55), (1240, 0.085, 0.45), (1520, 0.07, 0.3)]
    hits = [(0.004, 1.0)]
    at = 0.004
    for gap, lvl in c['bounces']:
        at += gap
        hits.append((at, lvl))
    x = np.zeros(secs(at + 0.4))
    for i, (th, lvl) in enumerate(hits):
        sh = 1.0 + (rng.uniform(-0.05, 0.05) if i else 0.0)
        parts = [(f * k * sh, tau, a * rng.uniform(0.6, 1.0)) for f, tau, a in coin]
        parts += [(f * k * (1 + rng.uniform(-0.01, 0.01)), tau, a * rng.uniform(0.3, 1.0) * (1.0 if i == 0 else 0.6))
                  for f, tau, a in wall]
        parts += [(f * k, tau, a * (1.0 if i == 0 else 0.3) * rng.uniform(0.7, 1.0)) for f, tau, a in body]
        ring = modes(parts, 0.4, c['seed'] * 10 + i)
        m = secs(0.004)
        click = highpass(white(m, c['seed'] * 20 + i), 2500.0) * decay_env(m, 0.0002, 0.0008)
        place(x, th, ring + 1.2 * np.pad(click, (0, len(ring) - m)), lvl)
    x = outdoor(x, tail_db=-32.0, tail_s=0.15, seed=c['seed'])
    return x, dict(voiced=False, fade_in=0.004, fade_out=0.06, max_s=0.78)


# ---------------------------------------------------------------------------------------------
# 9. A flattened carton.
# ---------------------------------------------------------------------------------------------

def crackles(n, seed, rate_curve, lo=1300.0, hi=4000.0):
    """Sparse cardboard crackle: Poisson clicks whose rate follows rate_curve (per s), each a short
    resonant ping between lo and hi Hz."""
    rng = np.random.default_rng(seed)
    x = np.zeros(n)
    events = np.nonzero(rng.random(n) < rate_curve / RATE)[0]
    for i in events:
        m = secs(0.012)
        p = peak_filter(white(m, seed + int(i)), rng.uniform(lo, hi), 4.0) * decay_env(m, 0.0002, rng.uniform(0.0015, 0.004))
        place(x, i / RATE, p, rng.uniform(0.3, 1.0))
    return x


def thump(seed, f=110.0, tau=0.03, seconds=0.12):
    m = secs(seconds)
    t = np.arange(m) / RATE
    tone = np.sin(2 * np.pi * f * t * (1 - 0.15 * t / seconds)) * decay_env(m, 0.002, tau)
    puff = lowpass(white(m, seed), 300.0) * decay_env(m, 0.002, tau * 0.6)
    return tone + 0.8 * puff / (np.max(np.abs(puff)) + 1e-9)


def build_carton(c):
    L = c['length_s']
    n = secs(L)
    t = np.arange(n) / RATE
    s = c['seed']
    rough = 1.0 + 0.6 * slow_random(n, s + 1, 40.0)
    rustle = bandpass(white(n, s), 300.0, 4000.0, order=4)
    rustle /= rms(rustle)
    x = np.zeros(n)
    if c['kind'] == 'sit':
        env = decay_env(n, 0.03, 0.18)
        x += 0.35 * rustle * env * np.abs(rough)
        x += 0.5 * crackles(n, s + 2, 70 * np.exp(-t / 0.2))
        place(x, 0.01, thump(s + 3, 105.0, 0.035), 1.6)
    elif c['kind'] == 'lift':
        env = smoothstep(t / (0.55 * L)) * (1 - smoothstep((t - 0.6 * L) / (0.4 * L)))
        x += 0.35 * rustle * env * np.abs(rough)
        x += 0.5 * crackles(n, s + 2, 55 * env)
        place(x, 0.62 * L, thump(s + 3, 140.0, 0.02, 0.08), 0.45)  # the loose flap slapping back
    else:
        env = smoothstep(t / 0.05) * (1 - smoothstep((t - 0.55 * L) / (0.45 * L)))
        stick = 1.0 + 0.8 * np.sign(np.sin(2 * np.pi * np.cumsum(28 + 10 * slow_random(n, s + 5, 6.0)) / RATE))
        x += 0.35 * lowpass(rustle, 2200.0) * env * np.abs(rough) * stick
        x += 0.3 * crackles(n, s + 2, 35 * env, 900.0, 2800.0)
        place(x, 0.005, thump(s + 3, 120.0, 0.02, 0.08), 0.5)
    x = lowpass(x, 4500.0, order=4)  # cardboard is dull: nothing much above 4 kHz
    x = outdoor(x, tail_db=-32.0, tail_s=0.12, seed=s)
    return x, dict(voiced=False, fade_in=0.006, fade_out=0.05, max_s=0.9)


# ---------------------------------------------------------------------------------------------
# 10. The taho gear: aluminium lid and bamboo pole.
# ---------------------------------------------------------------------------------------------

def lid_clink(seed, seconds=0.24):
    rng = np.random.default_rng(seed)
    parts = [(1680, 0.06, 0.8), (2470, 0.05, 1.0), (3320, 0.035, 0.7), (4410, 0.025, 0.5), (4980, 0.02, 0.35)]
    x = np.zeros(secs(seconds))
    for j, (at, lvl) in enumerate([(0.002, 1.0), (0.026 + rng.uniform(-0.004, 0.004), 0.45)]):
        p = [(f * (1 + rng.uniform(-0.015, 0.015)), tau, a * rng.uniform(0.6, 1.0)) for f, tau, a in parts]
        place(x, at, modes(p, seconds, seed + j), lvl)
    return x


def pole_creak(seed, seconds, hz_range):
    """Stick-slip: an irregular pulse train whose rate wanders inside hz_range, through a woody
    resonance near 750 Hz and a weaker one near 1.8 kHz."""
    rng = np.random.default_rng(seed)
    n = secs(seconds)
    t = np.arange(n) / RATE
    lo, hi = hz_range
    rate = lo + (hi - lo) * (0.5 + 0.5 * np.sin(np.pi * t / seconds - 0.4) * (0.7 + 0.3 * slow_random(n, seed + 1, 5.0)))
    x = np.zeros(n)
    at = 0.0
    while at < seconds:
        i = int(at * RATE)
        x[i] = rng.uniform(0.4, 1.0)
        at += (1.0 / rate[min(i, n - 1)]) * (1 + rng.uniform(-0.12, 0.12))
    x = lowpass(x, 2500.0, order=2)
    wood = 1.0 * peak_filter(x, 740.0, 7.0) + 0.45 * peak_filter(x, 1820.0, 9.0) + 0.3 * peak_filter(x, 330.0, 4.0)
    env = np.sin(np.pi * np.clip(t / seconds, 0, 1)) ** 1.5
    return wood * env


def build_bucket(c):
    s = c['seed']
    if c['kind'] == 'clink':
        x = lid_clink(s)
    elif c['kind'] == 'creak':
        x = pole_creak(s, 0.34, c['creak_hz'])
    else:
        x = pole_creak(s, 0.30, c['creak_hz'])
        cl = lid_clink(s + 50, 0.2)
        cl *= 0.8 * np.max(np.abs(x)) / (np.max(np.abs(cl)) + 1e-9)
        x = np.concatenate([x, np.zeros(secs(0.1))])
        place(x, 0.11, cl)
    x = outdoor(x, tail_db=-32.0, tail_s=0.1, seed=s)
    return x, dict(voiced=False, fade_in=0.004, fade_out=0.03, max_s=0.42)


# ---------------------------------------------------------------------------------------------
# 11 and 12. Pigeons.
# ---------------------------------------------------------------------------------------------

def build_coo(c):
    s = c['seed']
    parts, at = [], 0.004
    for i, (d, st0, stp, st1, gurgle, gap, lvl) in enumerate(c['syllables']):
        n = secs(d)
        t = np.arange(n) / RATE
        st = PchipInterpolator([0, 0.35 * d, d], [st0, stp, st1])(t)
        g_rate = 27.0 + 3.0 * np.random.default_rng(s + i).uniform(-1, 1)
        g_phase = 2 * np.pi * g_rate * t
        st = st + 0.25 * gurgle * np.sin(g_phase)  # the throat roll bends the pitch a little too
        f = c['base_hz'] * 2 ** (st / 12.0)
        ph = 2 * np.pi * np.cumsum(f) / RATE
        tone = np.sin(ph) + 0.16 * np.sin(2 * ph + 0.4) + 0.05 * np.sin(3 * ph + 1.1)
        am = 1.0 - gurgle * 0.5 * (1 - np.cos(g_phase))
        env = smoothstep(t / 0.05) * (1 - smoothstep((t - (d - 0.08)) / 0.08))
        breath = bandpass(white(n, s * 10 + i), 300.0, 1800.0)
        y = (tone * am + 0.05 * breath / rms(breath)) * env * lvl
        parts.append((at, y))
        at += d + gap
    x = np.zeros(secs(at + 0.05))
    for a, y in parts:
        place(x, a, y)
    x = lowpass(x, 1900.0)
    x = outdoor(x, tail_db=-30.0, tail_s=0.18, seed=s)
    return x, dict(voiced=True, fade_in=0.010, fade_out=0.04, fmin=150.0)


def build_flap(c):
    s = c['seed']
    rng = np.random.default_rng(s)
    end = max(b[0] + sum(b[2] * b[3] ** k for k in range(b[1])) for b in c['birds'])
    x = np.zeros(secs(end + 0.15))
    for bi, (start, count, first, ratio, level) in enumerate(c['birds']):
        at, gap = start + 0.004, first
        for k in range(count):
            fade = 0.86 ** k                          # thinning out as it climbs away
            cut = 3000.0 * (0.9 ** k) + 700.0
            m = secs(0.075)
            whoosh = bandpass(white(m, s * 100 + bi * 20 + k), 200.0, cut, order=4) * decay_env(m, 0.014, 0.018)
            cm = secs(0.02)
            clap = bandpass(white(cm, s * 200 + bi * 20 + k), 900.0, min(cut + 400, 3200.0), order=4) * decay_env(cm, 0.0004, 0.0035)
            clap_lvl = (1.3 if k < 3 else 0.6) * rng.uniform(0.7, 1.0)
            place(x, at, whoosh / (np.max(np.abs(whoosh)) + 1e-9), 0.7 * level * fade * rng.uniform(0.8, 1.0))
            place(x, at + 0.002, clap / (np.max(np.abs(clap)) + 1e-9), level * fade * clap_lvl)
            at += gap * (1 + rng.uniform(-0.06, 0.06))
            gap *= ratio
    x = outdoor(x, slaps=((0.07, -18.0),), tail_db=-30.0, tail_s=0.18, seed=s)
    return x, dict(voiced=False, fade_in=0.004, fade_out=0.05, max_s=1.3)


SOUNDS = ([(c, build_taho) for c in TAHO_CALLS] + [(c, build_giggle) for c in KID_GIGGLE]
          + [(c, build_taya) for c in KID_TAYA] + [(c, build_exclaim) for c in CHEER]
          + [(c, build_clap) for c in CLAP] + [(c, lambda cc: build_exclaim(cc, groan=True)) for c in GROAN]
          + [(c, build_salamat) for c in SALAMAT] + [(c, build_coin) for c in COIN_TIN]
          + [(c, build_carton) for c in CARTON] + [(c, build_bucket) for c in BUCKET]
          + [(c, build_coo) for c in PIGEON_COO] + [(c, build_flap) for c in PIGEON_FLAP])


# ---------------------------------------------------------------------------------------------
# Finishing and writing.
# ---------------------------------------------------------------------------------------------

def finish(x, fade_in=0.008, fade_out=0.04, max_s=None):
    """DC removed (25 Hz high-pass, causal so nothing leaks before the onset), trimmed to the sound
    (TRIM_DB), raised-cosine fades that reach exactly zero at both ends. Returns (y, start sample)."""
    x = highpass(np.concatenate([np.zeros(64), np.asarray(x, float)]), 25.0)[64:]
    a = np.abs(x)
    idx = np.nonzero(a > a.max() * 10 ** (TRIM_DB / 20.0))[0]
    start = max(0, idx[0] - secs(0.004))
    end = min(len(x), idx[-1] + secs(0.010) + 1)
    if max_s:
        end = min(end, start + secs(max_s))
    y = x[start:end].copy()
    fi, fo = secs(fade_in), secs(fade_out)
    y[:fi] *= 0.5 - 0.5 * np.cos(np.pi * np.arange(fi) / fi)
    y[-fo:] *= 0.5 - 0.5 * np.cos(np.pi * np.arange(fo)[::-1] / fo)
    return y, start


def to_pcm(x):
    return np.round(x * (PEAK / np.max(np.abs(x))) * 32767).astype('<i2')


def write_wav(name, pcm):
    path = OUT / (name + '.wav')
    with wave.open(str(path), 'wb') as out:
        out.setnchannels(1)
        out.setsampwidth(2)
        out.setframerate(RATE)
        out.writeframes(pcm.tobytes())
    return path


# ---------------------------------------------------------------------------------------------
# Measurement: YIN pitch, LPC formants, source tilt.
# ---------------------------------------------------------------------------------------------

def yin(x, fmin=70.0, fmax=900.0, hop=0.02, win=0.04, thresh=0.1):
    """YIN pitch on 20 ms hops. Returns (frame centre s, f0 Hz or nan, aperiodicity, frame RMS)."""
    fs = 22050
    y = signal.resample_poly(x, 1, 2)
    W, L = int(win * fs), int(fs / fmin) + 2
    tmin = int(fs / fmax)
    y = np.concatenate([y, np.zeros(W + L)])
    out_t, out_f, out_ap, out_r = [], [], [], []
    for i in range(0, len(y) - W - L - (W + L), int(hop * fs)):
        seg = y[i:i + W + L]
        a = seg[:W]
        nfft = 1 << int(np.ceil(np.log2(W + L)))
        r = np.fft.irfft(np.fft.rfft(seg, nfft) * np.conj(np.fft.rfft(a, nfft)), nfft)[:L]
        c2 = np.concatenate([[0.0], np.cumsum(seg ** 2)])
        e0 = c2[W]
        et = c2[np.arange(L) + W] - c2[np.arange(L)]
        d = e0 + et - 2 * r
        d[0] = 0
        cm = np.ones(L)
        cs = np.cumsum(d[1:])
        cm[1:] = d[1:] * np.arange(1, L) / np.maximum(cs, 1e-12)
        cand = np.nonzero(cm[tmin:] < thresh)[0]
        if len(cand):
            tau = tmin + cand[0]
            while tau + 1 < L and cm[tau + 1] < cm[tau]:
                tau += 1
        else:
            tau = tmin + int(np.argmin(cm[tmin:]))
        ap = cm[tau]
        if 1 <= tau < L - 1:
            den = cm[tau - 1] - 2 * cm[tau] + cm[tau + 1]
            shift = 0.5 * (cm[tau - 1] - cm[tau + 1]) / den if den else 0.0
        else:
            shift = 0.0
        f = fs / (tau + shift)
        out_t.append((i + W / 2) / fs)
        out_f.append(f if ap < 0.3 else np.nan)
        out_ap.append(ap)
        out_r.append(rms(a))
    f = np.array(out_f)
    # Octave continuity: a frame an octave off the median of its voiced neighbours (+-5 frames)
    # is folded back. YIN's usual failure on a back vowel whose F1 sits on the 2nd harmonic.
    fixed = f.copy()
    for i in range(len(f)):
        nb = f[max(0, i - 5):i + 6]
        nb = nb[np.isfinite(nb)]
        if np.isfinite(f[i]) and len(nb) >= 3:
            r = f[i] / np.median(nb)
            if 1.7 < r < 2.3:
                fixed[i] = f[i] / 2
            elif 0.43 < r < 0.59:
                fixed[i] = f[i] * 2
    yin.octave_fixes = int(np.sum(fixed != f) - np.sum(~np.isfinite(f)))
    return np.array(out_t), fixed, np.array(out_ap), np.array(out_r)


def lpc_formants(x, t0, t1, order=12, win=0.030, step=0.010):
    """Median LPC F1, F2, F3 over frames in [t0, t1] s, at 11025 Hz with pre-emphasis."""
    fs = 11025
    seg = signal.resample_poly(x[secs(t0):secs(t1)], 1, 4)
    seg = np.append(seg[0], seg[1:] - 0.94 * seg[:-1])
    W = int(win * fs)
    found = []
    for i in range(0, len(seg) - W, int(step * fs)):
        fr = seg[i:i + W] * np.hamming(W)
        r = np.correlate(fr, fr, 'full')[W - 1:W + order]
        if r[0] <= 0:
            continue
        a = solve_toeplitz(r[:order], -r[1:order + 1])
        roots = np.roots(np.concatenate([[1.0], a]))
        roots = roots[np.imag(roots) > 0]
        f = np.angle(roots) * fs / (2 * np.pi)
        bw = -fs / np.pi * np.log(np.abs(roots))
        keep = sorted(ff for ff, bb in zip(f, bw) if 150 < ff < fs / 2 - 150 and bb < 500)
        if len(keep) >= 3:
            found.append(keep[:3])
    return np.median(np.array(found), axis=0) if found else np.array([np.nan] * 3)


def spectral_tilt(sig, f0_track, t0, t1, hop=0.1):
    """dB per octave of the harmonic amplitudes between 300 Hz and 4 kHz: 0.1 s frames across
    [t0, t1], each harmonic's level read at k times the designed f0, one line fitted per frame,
    median slope returned."""
    slopes = []
    for tc in np.arange(t0 + hop / 2, t1 - hop / 2, hop):
        seg = sig[secs(tc - hop / 2):secs(tc + hop / 2)]
        f0c = float(np.median(f0_track[secs(tc - hop / 2):secs(tc + hop / 2)]))
        nfft = 1 << 16
        spec = np.abs(np.fft.rfft((seg - seg.mean()) * np.hanning(len(seg)), nfft))
        f = np.fft.rfftfreq(nfft, 1 / RATE)
        hf, hl = [], []
        for k in range(1, int(4000 / f0c) + 1):
            if k * f0c < 300:
                continue
            band = np.abs(f - k * f0c) < 0.2 * f0c
            hf.append(k * f0c)
            hl.append(20 * np.log10(spec[band].max() + 1e-12))
        slopes.append(np.polyfit(np.log2(hf), hl, 1)[0])
    return float(np.median(slopes))


def st_diff(a, b):
    return 12 * np.log2(a / b)


# ---------------------------------------------------------------------------------------------
# Plots.
# ---------------------------------------------------------------------------------------------

def plot_spectrogram(name, y, fmax=8000.0):
    import matplotlib
    matplotlib.use('Agg')
    import matplotlib.pyplot as plt
    t = np.arange(len(y)) / RATE
    fig, (a1, a2) = plt.subplots(2, 1, figsize=(10, 6), gridspec_kw=dict(height_ratios=[3, 1]), sharex=True)
    nper = 1024 if len(y) > secs(0.5) else 512
    f, tt, S = signal.spectrogram(y, RATE, window='hann', nperseg=nper, noverlap=nper - nper // 8, mode='magnitude')
    Sd = 20 * np.log10(S + 1e-12)
    Sd -= Sd.max()
    keep = f <= fmax
    a1.pcolormesh(tt, f[keep], np.maximum(Sd[keep], -80), shading='auto', cmap='magma')
    a1.set_ylabel('Hz')
    a1.set_title('%s  (%.3f s, 0 to %d Hz, dB re max, floor -80)' % (name, len(y) / RATE, fmax))
    a2.plot(t, y, lw=0.5, color='k')
    a2.set_ylim(-0.75, 0.75)
    a2.axhline(0.70, color='r', lw=0.5, ls='--')
    a2.axhline(-0.70, color='r', lw=0.5, ls='--')
    a2.set_xlabel('s')
    fig.tight_layout()
    fig.savefig(SPEC_DIR / (name + '.png'), dpi=90)
    plt.close(fig)


def plot_taho(name, y, design, off_s, meta, pitch):
    import matplotlib
    matplotlib.use('Agg')
    import matplotlib.pyplot as plt
    fig, axs = plt.subplots(3, 1, figsize=(11, 10), gridspec_kw=dict(height_ratios=[3, 2, 1]), sharex=True)
    f, tt, S = signal.spectrogram(y, RATE, window='hann', nperseg=2048, noverlap=2048 - 256, mode='magnitude')
    Sd = 20 * np.log10(S + 1e-12)
    Sd -= Sd.max()
    keep = f <= 4000
    axs[0].pcolormesh(tt, f[keep], np.maximum(Sd[keep], -80), shading='auto', cmap='magma')
    dt = design['t'] - off_s
    tr = design['tracks']
    vo = (tr['av'] > 0.15) | (tr['ah'] > 0.3)
    for k, col in (('F1', 'cyan'), ('F2', 'lime'), ('F3', 'white')):
        axs[0].plot(dt, np.where(vo, tr[k], np.nan), color=col, lw=1.0, ls='--', label='designed ' + k)
    axs[0].set_ylim(0, 4000)
    axs[0].legend(loc='upper right', fontsize=8)
    axs[0].set_ylabel('Hz')
    axs[0].set_title('%s: spectrogram 0 to 4 kHz with designed formant tracks' % name)
    pt, pf = pitch
    voiced = tr['av'] > 0.3
    axs[1].plot(dt, np.where(voiced, design['f0'], np.nan), color='tab:blue', lw=1.5, label='designed f0')
    axs[1].plot(pt, pf, 'o', ms=3.5, color='tab:red', label='measured (YIN, 20 ms hops)')
    for lab, (a, b) in design['seg'].items():
        axs[1].axvspan(a - off_s, b - off_s, alpha=0.06, color='k')
        axs[1].text((a + b) / 2 - off_s, 140, lab, ha='center', fontsize=8)
    axs[1].set_yscale('log')
    axs[1].set_ylim(130, 330)
    axs[1].set_yticks([140, 160, 180, 200, 225, 250, 280, 320])
    axs[1].set_yticklabels(['140', '160', '180', '200', '225', '250', '280', '320'])
    axs[1].set_ylabel('f0 Hz')
    axs[1].legend(loc='upper right', fontsize=8)
    t = np.arange(len(y)) / RATE
    axs[2].plot(t, y, lw=0.4, color='k')
    axs[2].axvspan(0, meta['fade_in'], color='tab:green', alpha=0.4, label='fade-in')
    axs[2].axvspan(t[-1] - meta['fade_out'], t[-1], color='tab:orange', alpha=0.4, label='fade-out')
    axs[2].axvline(design['dry_len'] / RATE - off_s, color='tab:purple', lw=0.8, ls=':', label='dry voice ends')
    axs[2].set_ylim(-0.75, 0.75)
    axs[2].legend(loc='upper right', fontsize=8)
    axs[2].set_xlabel('s')
    fig.tight_layout()
    fig.savefig(TAHO_DIR / (name + '_analysis.png'), dpi=90)
    plt.close(fig)


# ---------------------------------------------------------------------------------------------
# Main.
# ---------------------------------------------------------------------------------------------

def measure_common(name, pcm, meta):
    y = pcm.astype(np.float64) / 32768.0
    n = len(y)
    edge = secs(0.020)
    thr = np.max(np.abs(y)) * 10 ** (TRIM_DB / 20.0)
    loud = np.nonzero(np.abs(y) > thr)[0]
    lines = ['%s.wav' % name,
             '  duration %.3f s, peak %.4f FS (%d), RMS %.4f FS (%.1f dBFS), clipped samples %d, mean %.2e'
             % (n / RATE, np.max(np.abs(y)), np.max(np.abs(pcm.astype(int))), rms(y), 20 * np.log10(rms(y)),
                int(np.sum(np.abs(pcm.astype(int)) >= 32767)), np.mean(y)),
             '  first |x| %.6f, last |x| %.6f; max jump first 20 ms %.5f, last 20 ms %.5f FS; '
             'max jump inside fade-in %.5f, inside fade-out %.5f FS; '
             'fade-in %.0f ms, fade-out %.0f ms; lead below %d dB %.1f ms, trail %.1f ms'
             % (abs(y[0]), abs(y[-1]), np.max(np.abs(np.diff(y[:edge]))), np.max(np.abs(np.diff(y[-edge:]))),
                np.max(np.abs(np.diff(y[:secs(meta['fade_in'])]))), np.max(np.abs(np.diff(y[-secs(meta['fade_out']):]))),
                meta['fade_in'] * 1000, meta['fade_out'] * 1000, TRIM_DB, loud[0] / RATE * 1000,
                (n - 1 - loud[-1]) / RATE * 1000)]
    pitch = None
    if meta.get('voiced'):
        pt, pf, ap, pr = yin(y, fmin=meta.get('fmin', 70.0))
        good = np.isfinite(pf) & (pr > 0.05 * pr.max())
        if good.any():
            lines.append('  median f0 %.1f Hz over %d voiced frames (range %.0f to %.0f Hz; %d frames octave-corrected)'
                         % (np.median(pf[good]), good.sum(), np.min(pf[good]), np.max(pf[good]), yin.octave_fixes))
        pitch = (pt, np.where(good, pf, np.nan))
    return y, lines, pitch


def taho_checks(y, design, off_s, pitch):
    pt, pf = pitch
    t = design['t']
    f0 = design['f0']
    tr = design['tracks']
    seg = {k: (a - off_s, b - off_s) for k, (a, b) in design['seg'].items()}
    des = np.interp(pt + off_s, t, f0)
    av = np.interp(pt + off_s, t, tr['av'])
    use = np.isfinite(pf) & (av > 0.5) & (pt + off_s < t[-1])
    dev = np.abs(st_diff(pf[use], des[use]))
    lines = ['  taho pitch: measured vs designed over %d frames with voicing > 0.5: median |dev| %.2f st, '
             '95th pct %.2f st, max %.2f st' % (use.sum(), np.median(dev), np.percentile(dev, 95), dev.max())]

    def at(a, b, fn=np.median):
        m = use & (pt >= a) & (pt <= b)
        return (fn(pf[m]), fn(des[m])) if m.any() else (np.nan, np.nan)

    a0, a1 = seg['a']
    h0, h1 = seg['hold']
    f0_, f1_ = seg['fall']
    tail = np.isfinite(pf) & (av > 0.35) & (pt >= f0_) & (pt + off_s < t[-1])
    last = pt[tail][-3:] if tail.any() else np.array([f1_])
    end = (np.median(pf[tail][-3:]), np.median(des[tail][-3:])) if tail.any() else (np.nan, np.nan)
    checks = [('"ta"', at(a0 + 0.02, a1 - 0.01)), ('hold start', at(h0, h0 + 0.08)),
              ('hold max', at(h0, h1, np.max)),
              ('end (last voiced, %.2f to %.2f s)' % (last[0], last[-1]), end)]
    for lab, (m, d) in checks:
        lines.append('    %-34s measured %6.1f Hz, designed %6.1f Hz, diff %+.2f st' % (lab, m, d, st_diff(m, d)))
    fa = lpc_formants(y, a0 + 0.035, a1 - 0.005)
    fo = lpc_formants(y, h0 + 0.25, h1 - 0.1, order=10)
    da = [np.interp((a0 + a1) / 2 + off_s, t, tr[k]) for k in ('F1', 'F2')]
    do = [np.interp((h0 + h1) / 2 + off_s, t, tr[k]) for k in ('F1', 'F2')]
    lines.append('  taho formants (LPC, median over frames): /a/ F1 %.0f F2 %.0f (designed %.0f, %.0f); '
                 '/o/ F1 %.0f F2 %.0f (designed %.0f, %.0f)' % (fa[0], fa[1], da[0], da[1], fo[0], fo[1], do[0], do[1]))
    ho = design['seg']['hold']
    lines.append('  taho source tilt: glottal flow %.1f dB/oct, flow derivative (flow + lip radiation) %.1f dB/oct, '
                 '300 Hz to 4 kHz over the hold' % (spectral_tilt(design['flow'], design['f0'], ho[0] + 0.1, ho[1]),
                                                   spectral_tilt(np.gradient(design['flow']), design['f0'], ho[0] + 0.1, ho[1])))
    return lines


def main():
    ap = argparse.ArgumentParser(description='Synthesize the Ilalim ng Tulay sidewalk-life one-shots.')
    ap.add_argument('--only', default='', help='only files whose name contains this text')
    ap.add_argument('--no-plots', action='store_true')
    args = ap.parse_args()
    OUT.mkdir(parents=True, exist_ok=True)
    SPEC_DIR.mkdir(parents=True, exist_ok=True)
    TAHO_DIR.mkdir(parents=True, exist_ok=True)
    report = ['Ilalim sidewalk-life SFX, written by tools/synth_ilalim_life_sfx.py',
              '44100 Hz mono 16-bit, peak %.2f FS, trim %d dB, fades raised cosine to exact zero' % (PEAK, TRIM_DB), '']
    for cfg, build in SOUNDS:
        name = cfg['name']
        if args.only and args.only not in name:
            continue
        x, meta = build(cfg)
        y, start = finish(x, meta['fade_in'], meta['fade_out'], meta.get('max_s'))
        pcm = to_pcm(y)
        path = write_wav(name, pcm)
        yy, lines, pitch = measure_common(name, pcm, meta)
        if 'design' in meta:
            lines += taho_checks(yy, meta['design'], start / RATE, pitch)
            if not args.no_plots:
                plot_taho(name, yy, meta['design'], start / RATE, meta, pitch)
        lines.append('  sha256 %s' % hashlib.sha256(path.read_bytes()).hexdigest()[:16])
        if not args.no_plots:
            plot_spectrogram(name, yy)
        report += lines + ['']
        print('\n'.join(lines))
    if not args.only:
        (SPEC_DIR / 'report.txt').write_text('\n'.join(report) + '\n', encoding='utf-8')


if __name__ == '__main__':
    main()
