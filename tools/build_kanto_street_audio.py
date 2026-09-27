"""Kanto street sound: a city-block bed, three engine loops and five horns, by deterministic synthesis.

Writes ONLY these files into Assets/TumbangPreso/Art/audio/ambience/:
    kanto_city_bed.wav
    kanto_engine_car.wav, kanto_engine_diesel.wav, kanto_engine_tricycle.wav
    kanto_horn_car_1.wav .. kanto_horn_car_3.wav, kanto_horn_jeepney.wav, kanto_horn_tricycle.wav
and the measurement record docs/reports/kanto-street-audio-authoring.json.

Owner brief (2026-09-27, about Kanto's moving traffic): "needs sfx, bustling city ambience, car
engine and driving sounds, beep/horns". `Runtime/Map/KantoStreetSound.cs` plays these; this file
only authors them.

Same method as build_lagoon_ambience.py: numpy only, fixed seeds, no external samples, so a rerun
is byte-identical and the provenance is simple to state.

Run:  py -3 tools/build_kanto_street_audio.py
"""
import hashlib
import json
from pathlib import Path
import wave

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/TumbangPreso/Art/audio/ambience'
REPORT = ROOT / 'docs/reports/kanto-street-audio-authoring.json'
RATE = 44100

# ⚠️ 38 s: inside the brief's 30 to 45, and not a multiple of any engine loop (3.0, 3.2, 2.5 s), so
# the bed's one jeepney rev never lines up with the same engine cycle lap after lap.
BED_SECONDS = 38.0

# ⚠️⚠️ ENGINE LOOPS ARE AUTHORED AT IDLE-TO-LOW-CRUISE, NOT AT CRUISE. The runtime raises pitch with
# speed (about 0.85 waiting, up to about 1.6 at cruise), and pitching a loop DOWN below its authored
# rate makes it muddy and slow-motion, while pitching UP reads as revs. So each loop sits near the
# bottom of its range: the car fires at 36 Hz (a four-cylinder at 1,080 rpm), the diesel at 27.5 Hz
# (825 rpm, a jeepney's lumpy idle), the tricycle at 28 Hz (a single cylinder two-stroke at
# 1,680 rpm). At pitch 1.6 those become 1,730, 1,320 and 2,690 rpm: town speed, never a race.
CAR_SECONDS, CAR_FIRE_HZ = 3.0, 36.0
DIESEL_SECONDS, DIESEL_FIRE_HZ = 3.2, 27.5
TRIKE_SECONDS, TRIKE_FIRE_HZ = 2.5, 28.0

# ⚠️ THE SEAM CHECK WINDOW IS THE BRIEF'S OWN NUMBER: first and last 50 ms within 10 per cent RMS.
SEAM_WINDOW = int(0.050 * RATE)


# ---------------------------------------------------------------------------------------------
# Periodic building blocks (the lagoon tool's, restated so this file stands alone).
#
# ⚠️⚠️ EVERYTHING IN A LOOP IS BUILT CIRCULAR, SO THE LOOP HAS NO SEAM TO HIDE. Noise is shaped in
# the frequency domain over exactly the loop length (an inverse FFT of length N is periodic in N),
# every event is written modulo N, and every filter is a circular (FFT) filter over the whole loop,
# so a horn echo that starts at 37 s finishes at 1 s of the next lap. For the engines the firing
# COUNT is fixed per loop and the jittered intervals are rescaled to sum to exactly the loop length,
# so the last firing leads into the first exactly as any other pair does.
# ---------------------------------------------------------------------------------------------

def freqs(n, rate=RATE):
    f = np.fft.rfftfreq(n, 1.0 / rate)
    f[0] = 1e-3
    return f


def band_noise(n, seed, lo, hi, tilt=0.0, order=2, rate=RATE):
    """Unit-RMS noise, periodic in n, between lo and hi Hz; tilt in dB per octave above lo."""
    rng = np.random.default_rng(seed)
    spec = rng.normal(size=n // 2 + 1) + 1j * rng.normal(size=n // 2 + 1)
    f = freqs(n, rate)
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


def slow_random(n, seed, hi_hz, rate=RATE):
    """A smooth periodic random curve, unit RMS, with nothing faster than about hi_hz."""
    rng = np.random.default_rng(seed)
    spec = rng.normal(size=n // 2 + 1) + 1j * rng.normal(size=n // 2 + 1)
    spec *= np.exp(-(np.fft.rfftfreq(n, 1.0 / rate) / hi_hz) ** 2)
    spec[0] = 0.0
    out = np.fft.irfft(spec, n)
    return out / np.sqrt(np.mean(out ** 2))


def add_wrapped(dst, start, piece):
    """Adds piece into dst starting at sample start, wrapping past the end (the loop is a circle)."""
    n = len(dst)
    start = int(start) % n
    m = len(piece)
    first = min(m, n - start)
    dst[start:start + first] += piece[:first]
    rest = m - first
    while rest > 0:
        take = min(rest, n)
        dst[:take] += piece[first:first + take]
        first += take
        rest -= take


def peaks_response(f, peaks, floor=0.0, lo=0.0, hi=0.0, order=2):
    """A magnitude response: `floor` plus band-pass bumps (centre, Q, gain), then optional high
    and low cuts. ⚠️ BAND-PASS BUMPS, NOT RESONATOR LOW-PASSES: a resonator's response is 1 at DC,
    so summing five of them lifts the sub-bass by five and turns every engine into a woofer test."""
    h = np.full_like(f, floor)
    for centre, q, gain in peaks:
        h += gain / np.sqrt(1.0 + q * q * (f / centre - centre / f) ** 2)
    if lo > 0:
        h /= np.sqrt(1.0 + (lo / f) ** (2 * order))
    if hi > 0:
        h /= np.sqrt(1.0 + (f / hi) ** (2 * order))
    return h


def circ_filter(x, response, rate=RATE):
    """Circular FFT filter over the whole signal; `response(f)` gives the magnitude."""
    n = len(x)
    h = response(freqs(n, rate))
    h[0] = 0.0
    return np.fft.irfft(np.fft.rfft(x) * h, n)


def lin_filter(x, response, pad_seconds=0.25):
    """The same for a one-shot, zero-padded both sides so nothing wraps.

    ⚠️ THE FRONT PAD IS DROPPED AFTER FILTERING. An FFT filter is zero-phase, so it rings a little
    BEFORE a sharp attack as well as after; with no front pad that pre-ring wraps round onto the
    end of the buffer and the one-shot grows a faint tail of its own attack (the first pass measured
    a 0.22 s beep as a 0.475 s file for exactly this reason)."""
    pad = int(pad_seconds * RATE)
    front = int(0.05 * RATE)
    y = circ_filter(np.concatenate([np.zeros(front), x, np.zeros(pad)]), response)
    return y[front:]


def gamma_pulse(tau, length_taus=9.0):
    """A one-sided pressure pulse, t/tau * e^(1 - t/tau): peak 1 at t = tau, smooth tail."""
    m = max(8, int(length_taus * tau * RATE))
    t = np.arange(m) / RATE
    return (t / tau) * np.exp(1.0 - t / tau)


def damped(freq, decay, seconds, phase=0.0):
    m = int(seconds * RATE)
    t = np.arange(m) / RATE
    return np.sin(2 * np.pi * freq * t + phase) * np.exp(-t / decay)


def firing_times(n, count, rng, slow_jitter, white_jitter, seed):
    """Sample positions of `count` firings round a loop of n samples.

    ⚠️ TWO KINDS OF JITTER, BECAUSE AN ENGINE HAS TWO. A slow periodic wander (the idle governor
    hunting, about 1 Hz) and a per-firing scatter (combustion is never identical twice). Without the
    first the engine is a metronome; without the second it is a buzzer. The gaps are rescaled so
    they sum to exactly n: the loop's last gap is an ordinary gap."""
    wander = slow_random(count, seed, 1.0, rate=count / (n / RATE))
    gaps = 1.0 + slow_jitter * wander + white_jitter * rng.normal(size=count)
    gaps = np.maximum(gaps, 0.5)
    gaps *= n / gaps.sum()
    return np.concatenate([[0.0], np.cumsum(gaps)[:-1]])


# ---------------------------------------------------------------------------------------------
# Engines.
#
# Every engine is built the way the sound is made: a train of exhaust pressure pulses, one per
# firing, each a little different (per-cylinder imbalance and per-firing scatter), run through the
# resonances of the exhaust and body (a circular filter), plus a combustion and mechanical noise
# layer GATED by the firings so the noise pulses with the engine instead of hissing over it.
# Heard from outside a car at kerb distance: little above 4 kHz survives, and the fundamental
# (25 to 60 Hz) is felt more than heard, so the energy that reads as "engine" is its harmonics
# between about 80 and 800 Hz.
# ---------------------------------------------------------------------------------------------

def engine_core(seconds, fire_hz, pattern, seed, tau, slow_j, white_j, amp_j, weak_chance=0.0):
    n = int(round(seconds * RATE))
    rng = np.random.default_rng(seed)
    count = int(round(seconds * fire_hz / len(pattern))) * len(pattern)
    times = firing_times(n, count, rng, slow_j, white_j, seed + 1)
    amps = np.array([pattern[k % len(pattern)] for k in range(count)]) * (1.0 + amp_j * rng.normal(size=count))
    if weak_chance:
        # ⚠️ A light-load two-stroke skips and half-fires ("four-stroking"); that irregular lope is
        # most of what makes a tricycle sound like a tricycle and not a lawnmower.
        weak = rng.uniform(0, 1, count) < weak_chance
        amps[weak] *= rng.uniform(0.15, 0.45, weak.sum())
    amps = np.maximum(amps, 0.05)
    pulse = gamma_pulse(tau)
    train = np.zeros(n)
    gate = np.zeros(n)
    gate_shape = np.exp(-np.arange(int(0.012 * RATE)) / (0.004 * RATE))
    for at, a in zip(times, amps):
        add_wrapped(train, int(at), pulse * a)
        add_wrapped(gate, int(at), gate_shape * a)
    return n, rng, times, amps, train, gate


def author_engine_car():
    """A small petrol four-cylinder, smooth: tight scatter, gentle imbalance, soft pulses."""
    n, rng, times, amps, train, gate = engine_core(
        CAR_SECONDS, CAR_FIRE_HZ, [1.0, 0.94, 0.98, 0.91], 7101,
        tau=0.0026, slow_j=0.012, white_j=0.006, amp_j=0.05)
    # ⚠️ SMOOTH MEANS THE PULSES OVERLAP. The first pass (1.6 ms pulses, Q up to 3) swung 20 dB
    # between firings on the plot and read as a buzzer; wider pulses and broader, lower-Q
    # resonances close the gaps so the four cylinders blend into one hum, the way a small petrol
    # car sounds from the pavement.
    exhaust = circ_filter(train, lambda f: peaks_response(
        f, [(90, 1.1, 1.0), (185, 1.5, 0.65), (400, 1.8, 0.3), (850, 2.2, 0.1)],
        floor=0.05, lo=45, hi=2400, order=2))
    # Intake and valvetrain: a soft hiss with a faint tick, following the firings.
    hiss = band_noise(n, 7102, 900, 4200, tilt=-3) * (0.25 + gate / (gate.max() + 1e-9))
    tick = band_noise(n, 7103, 2500, 6500) * (gate / (gate.max() + 1e-9)) ** 2
    out = exhaust / rms(exhaust) + 0.10 * hiss + 0.035 * tick
    # A steady low hum under the pulses: the crank and body, never silent between firings.
    out += 0.22 * band_noise(n, 7104, 40, 160, tilt=-2)
    return seam_rotate(out), {'firings': len(times), 'firing_hz': round(len(times) / CAR_SECONDS, 3)}


def author_engine_diesel():
    """A jeepney or city bus diesel at idle: heavy low pulses, a lumpy imbalance, and the knock.

    ⚠️ THE CLATTER IS ITS OWN LAYER, A MILLISECOND AHEAD OF EACH PULSE. Diesel knock is the
    injection igniting all at once, a sharp metallic ring from the block, and it arrives before the
    exhaust pulse leaves the pipe. Filtering the pulse train brighter cannot make it: that gives a
    louder petrol engine, not a diesel."""
    n, rng, times, amps, train, gate = engine_core(
        DIESEL_SECONDS, DIESEL_FIRE_HZ, [1.0, 0.82, 0.95, 0.78], 7201,
        tau=0.0024, slow_j=0.02, white_j=0.014, amp_j=0.10)
    exhaust = circ_filter(train, lambda f: peaks_response(
        f, [(58, 1.4, 1.0), (118, 2.0, 0.85), (240, 2.4, 0.45), (520, 3.0, 0.18)],
        floor=0.04, lo=32, hi=1800, order=2))
    exhaust /= rms(exhaust)

    knock = np.zeros(n)
    # ⚠️ THE RINGS WANDER 10 PER CENT AND DIE IN 1 TO 2.5 ms, OVER A NOISE BURST. The first pass
    # rang four fixed frequencies for up to 5 ms and the spectrum showed four sharp peaks: tonal,
    # a bell being tapped, not a block clattering. Real knock is broadband with loose resonances.
    rings = [(1850, 0.0022), (2950, 0.0016), (4100, 0.0011), (1320, 0.0025)]
    burst_noise = band_noise(int(0.02 * RATE) * 64, 7204, 900, 5000)
    burst_env = np.exp(-np.arange(int(0.02 * RATE)) / (0.0018 * RATE))
    for k, (at, a) in enumerate(zip(times, amps)):
        m = int(0.02 * RATE)
        piece = burst_noise[(k % 64) * m:(k % 64 + 1) * m] * burst_env * 0.9
        for freq, decay in rings:
            f = freq * rng.uniform(0.9, 1.1)
            piece = piece + damped(f, decay, 0.02, rng.uniform(0, 2 * np.pi)) * rng.uniform(0.4, 1.0)
        # ⚠️ The knock's level scatters more than the pulse's (30 per cent): that unevenness is
        # the clatter; an even knock reads as a sewing machine.
        add_wrapped(knock, int(at) - int(0.0012 * RATE), piece * a * rng.uniform(0.7, 1.3))
    knock /= rms(knock)
    # Injector and valve ticks: a second, quieter clack half a cycle later on some firings.
    ticks = np.zeros(n)
    for at in times:
        if rng.uniform() < 0.55:
            add_wrapped(ticks, int(at + 0.5 * RATE / DIESEL_FIRE_HZ),
                        damped(rng.uniform(3200, 4800), 0.0012, 0.008) * rng.uniform(0.3, 1.0))
    ticks /= rms(ticks) + 1e-12
    rumble = band_noise(n, 7202, 30, 140, tilt=-2) * (0.6 + 0.4 * gate / gate.max())
    out = exhaust + 0.30 * knock + 0.07 * ticks + 0.22 * rumble
    out += 0.05 * band_noise(n, 7203, 700, 3000, tilt=-3) * (gate / gate.max())
    return seam_rotate(out), {'firings': len(times), 'firing_hz': round(len(times) / DIESEL_SECONDS, 3)}


def author_engine_tricycle():
    """A motorcycle-and-sidecar tricycle: a single-cylinder two-stroke putter with a rattling sidecar.

    ⚠️ ONE CYLINDER MEANS EVERY FIRING IS A SEPARATE EVENT. A four-cylinder's pulses overlap into
    a hum; a single's arrive as distinct pops with space between, which is the putter. So the pulse
    is SHARP (0.6 ms), the scatter is wide (four per cent per firing) and one firing in nine is
    weak. The sidecar's loose panels and the roof frame rattle off some firings: short metallic
    rings from 2 to 6 kHz, delayed a few milliseconds, never on every beat."""
    n, rng, times, amps, train, gate = engine_core(
        TRIKE_SECONDS, TRIKE_FIRE_HZ, [1.0], 7301,
        tau=0.0006, slow_j=0.03, white_j=0.04, amp_j=0.16, weak_chance=0.11)
    exhaust = circ_filter(train, lambda f: peaks_response(
        f, [(150, 1.5, 0.55), (310, 2.2, 1.0), (720, 2.8, 0.7), (1550, 3.5, 0.35), (2600, 4.0, 0.12)],
        floor=0.03, lo=70, hi=4200, order=2))
    exhaust /= rms(exhaust)

    rattle = np.zeros(n)
    panels = [2350, 3380, 4650, 5900, 2780]
    rattled = 0
    for at, a in zip(times, amps):
        if rng.uniform() < 0.45 * min(1.0, a):
            rattled += 1
            for _ in range(rng.integers(1, 4)):
                f = panels[rng.integers(len(panels))] * rng.uniform(0.98, 1.02)
                delay = rng.uniform(0.003, 0.016)
                add_wrapped(rattle, int(at + delay * RATE),
                            damped(f, rng.uniform(0.002, 0.006), 0.03, rng.uniform(0, 6.28)) * rng.uniform(0.3, 1.0))
    rattle /= rms(rattle)
    # The two-stroke's rasp: bright combustion noise riding the pulses.
    rasp = band_noise(n, 7302, 1200, 5500, tilt=-2) * (gate / gate.max()) ** 1.5
    rasp /= rms(rasp)
    out = exhaust + 0.20 * rattle + 0.22 * rasp + 0.06 * band_noise(n, 7303, 60, 200)
    return seam_rotate(out), {'firings': len(times), 'firing_hz': round(len(times) / TRIKE_SECONDS, 3),
                              'rattling_firings': rattled}


# ---------------------------------------------------------------------------------------------
# Horns.
#
# An electric car horn is a steel diaphragm slammed by an electromagnet that interrupts itself, so
# its wave is a narrow PULSE rich in odd and even harmonics (a sine never sounds like a horn). Most
# cars carry a PAIR tuned a minor or major third apart (about 400 and 500 Hz), and their beating is
# the "honk". The trumpet or disc housing then boosts a band around 2 to 3 kHz, and the diaphragm
# overdrives, which the tanh stands for. Each tone scoops up a few per cent as the diaphragm
# reaches full swing, and drifts a little as the voltage sags.
# ---------------------------------------------------------------------------------------------

def horn_tone(f0, seconds, duty, seed, scoop=0.03, drift=0.004):
    rng = np.random.default_rng(seed)
    m = int(seconds * RATE)
    t = np.arange(m) / RATE
    f = f0 * (1.0 - scoop * np.exp(-t / 0.018)) * (1.0 + drift * slow_random(max(m, 64), seed + 1, 6.0)[:m])
    phase = 2 * np.pi * np.cumsum(f) / RATE + rng.uniform(0, 2 * np.pi)
    sig = np.zeros(m)
    h = 1
    while h * f0 < 0.45 * RATE and h < 60:
        # Pulse-wave spectrum: sin(pi h d) / h, which is what a slammed diaphragm approximates.
        sig += np.sin(h * phase) * np.sin(np.pi * h * duty) / h
        h += 1
    return sig


def horn_env(seconds, attack=0.008, release=0.03):
    m = int(seconds * RATE)
    t = np.arange(m) / RATE
    a = np.clip(t / attack, 0, 1)
    r = np.clip((seconds - t) / release, 0, 1)
    # A horn is at full level almost at once and sags slightly as the battery voltage dips.
    return a * r * (0.92 + 0.08 * np.exp(-t / 0.15))


def car_horn_note(f_lo, f_hi, seconds, seed, housing=2300):
    lo = horn_tone(f_lo, seconds, 0.28, seed)
    hi = horn_tone(f_hi, seconds, 0.24, seed + 10)
    x = (lo + 0.9 * hi) * horn_env(seconds)
    x = lin_filter(x, lambda f: peaks_response(
        f, [(housing, 3.5, 1.0), (housing * 0.42, 2.5, 0.8), (housing * 1.6, 5.0, 0.35)],
        floor=0.15, lo=260, hi=5200, order=2))
    x /= np.max(np.abs(x)) + 1e-12
    return np.tanh(1.8 * x)


def sequence(parts, total_pad=0.12):
    """parts: [(start_seconds, signal)] placed into one buffer with a little tail room."""
    end = max(s + len(sig) / RATE for s, sig in parts)
    out = np.zeros(int((end + total_pad) * RATE))
    for s, sig in parts:
        i = int(s * RATE)
        out[i:i + len(sig)] += sig
    return out


def trim_tail(x, floor_db=-50):
    """Cuts trailing near-silence left by the filter padding, keeping a 5 ms fade."""
    thresh = np.max(np.abs(x)) * 10 ** (floor_db / 20)
    idx = np.nonzero(np.abs(x) > thresh)[0]
    end = min(len(x), (idx[-1] if len(idx) else len(x)) + int(0.005 * RATE))
    x = x[:end].copy()
    fade = min(len(x), int(0.005 * RATE))
    x[-fade:] *= np.linspace(1, 0, fade)
    return x


def author_horns():
    horns = {}
    # 1. One short polite beep: the "I'm here" tap.
    horns['kanto_horn_car_1.wav'] = sequence([(0.0, car_horn_note(415, 500, 0.22, 7401))])
    # 2. A double beep from a different car (its pair a major third apart, a touch higher).
    horns['kanto_horn_car_2.wav'] = sequence([
        (0.0, car_horn_note(440, 554, 0.12, 7411, housing=2500)),
        (0.20, car_horn_note(440, 554, 0.17, 7412, housing=2500)),
    ])
    # 3. The impatient lean-on: longer, lower, a hatchback's thinner pair, sagging at the end.
    long_note = car_horn_note(392, 470, 0.62, 7421, housing=2100)
    sag = np.linspace(1.0, 0.9, len(long_note)) ** 0.5
    horns['kanto_horn_car_3.wav'] = sequence([(0.0, long_note * sag)])

    # The jeepney: a dual-trumpet AIR horn playing its two-note call, "ta-DAAH".
    # ⚠️ BRASSY MEANS THE BRIGHTNESS FOLLOWS THE LEVEL: as the air pressure builds, more upper
    # harmonics speak, so the attack blooms from dark to bright. A fixed spectrum at a fixed level
    # reads as an organ stop. Each note is a chord of two trumpets a major third apart, slightly
    # detuned from true, with a slow vibrato on the held note from the air valve.
    def air_trumpet(f0, seconds, seed, vibrato=0.0):
        rng = np.random.default_rng(seed)
        m = int(seconds * RATE)
        t = np.arange(m) / RATE
        env = np.clip(t / 0.035, 0, 1) ** 1.5 * np.clip((seconds - t) / 0.06, 0, 1)
        env *= 0.94 + 0.06 * np.exp(-t / 0.2)
        f = f0 * (1.0 - 0.05 * np.exp(-t / 0.03)) * (1.0 + vibrato * np.sin(2 * np.pi * 5.2 * t) * np.clip((t - 0.15) / 0.2, 0, 1))
        phase = 2 * np.pi * np.cumsum(f) / RATE + rng.uniform(0, 6.28)
        sig = np.zeros(m)
        for h in range(1, 40):
            if h * f0 > 0.45 * RATE:
                break
            bright = np.exp(-h / (2.0 + 9.0 * env))
            sig += np.sin(h * phase) * bright / h ** 0.6
        return sig * env

    def jeep_note(root, seconds, seed, vibrato=0.0):
        a = air_trumpet(root, seconds, seed, vibrato)
        b = air_trumpet(root * 1.26 * 1.004, seconds, seed + 1, vibrato)
        x = lin_filter(a + 0.85 * b, lambda f: peaks_response(
            f, [(1250, 2.2, 1.0), (2600, 3.0, 0.7), (650, 1.8, 0.6)], floor=0.2, lo=200, hi=7000))
        x /= np.max(np.abs(x)) + 1e-12
        return np.tanh(1.4 * x)

    horns['kanto_horn_jeepney.wav'] = sequence([
        (0.0, jeep_note(349.2, 0.17, 7501)),
        (0.21, jeep_note(466.2, 0.62, 7503, vibrato=0.006)),
    ])

    # The tricycle: one small disc horn on a motorcycle's weak 12 V, thin and nasal, "meep-meep".
    # ⚠️ NASAL COMES FROM A NARROW HIGH HOUSING PEAK AND NO BODY BELOW 600 Hz. A single tone (no
    # pair), a narrower pulse (more upper harmonics) and a buzzier overdrive.
    def trike_note(seconds, seed):
        x = horn_tone(510, seconds, 0.14, seed, scoop=0.05, drift=0.008) * horn_env(seconds, 0.006, 0.02)
        x = lin_filter(x, lambda f: peaks_response(
            f, [(2850, 6.0, 1.0), (1700, 4.0, 0.45), (4300, 6.0, 0.25)], floor=0.06, lo=650, hi=7500, order=3))
        x /= np.max(np.abs(x)) + 1e-12
        return np.tanh(2.4 * x)

    horns['kanto_horn_tricycle.wav'] = sequence([
        (0.0, trike_note(0.11, 7601)),
        (0.17, trike_note(0.15, 7602)),
    ])

    for name in horns:
        horns[name] = trim_tail(horns[name])
    return horns


# ---------------------------------------------------------------------------------------------
# The city bed, stereo.
#
# A busy Manila block heard from a small park in the middle of it: the traffic is always there
# but never close (the close traffic is the runtime's own engines, which is why the bed stays in
# the background). Layers, quietest last:
#   1. a continuous low WASH of many far engines (30 to 250 Hz) with a slow breathing level;
#   2. TYRE HISS, the bright half of the wash, from the ring roads further out;
#   3. distant PASSES, a swell of hiss and engine that sweeps from one side to the other;
#   4. far ENGINES pulling away through the gears (a rise, a dip at the shift, a rise, a fade);
#   5. one far JEEPNEY REV, a diesel blipped twice;
#   6. three DISTANT HORNS, the horn synth darkened and echoed off the buildings;
#   7. a CROWD MURMUR, many talkers too far away to have words.
# Everything distant goes through one shared "street" reverb, a circular convolution with a
# decaying noise tail, so the far sounds sit in the same space rather than each in its own.
# ---------------------------------------------------------------------------------------------

def street_reverb(x, seconds=1.1, seed=7801, wet=0.55):
    """Circular convolution with a decaying stereo-decorrelated noise tail (periodic, so no seam)."""
    n = len(x)
    rng = np.random.default_rng(seed)
    m = int(seconds * RATE)
    t = np.arange(m) / RATE
    ir = rng.normal(size=m) * np.exp(-t / (seconds / 6.9))
    # Buildings eat the top end of every reflection.
    ir = lin_filter(ir, lambda f: peaks_response(f, [], floor=1.0, lo=120, hi=2400))[:m]
    # Early reflections: the facades across the road, 18 to 70 ms.
    for d, g in [(0.018, 0.5), (0.031, 0.35), (0.047, 0.3), (0.069, 0.22)]:
        ir[int(d * RATE)] += g * np.max(np.abs(ir))
    ir /= np.sqrt(np.sum(ir ** 2))
    big = np.zeros(n)
    big[:m] = ir
    y = np.fft.irfft(np.fft.rfft(x) * np.fft.rfft(big), n)
    return (1.0 - wet) * x + wet * y * (rms(x) / (rms(y) + 1e-12))


def engine_sweep(seconds, seed, f_curve, harmonics_lp, diesel=False):
    """A far engine: harmonic series of a moving firing rate, darkened for distance.

    f_curve(u) gives the firing rate for u in [0,1]. The level rises with the rate (load)."""
    rng = np.random.default_rng(seed)
    m = int(seconds * RATE)
    u = np.linspace(0, 1, m)
    f0 = f_curve(u) * (1.0 + 0.01 * slow_random(m, seed + 1, 8.0))
    phase = 2 * np.pi * np.cumsum(f0) / RATE
    sig = np.zeros(m)
    for h in range(1, 30):
        fh = f0 * h
        amp = 1.0 / np.sqrt(1.0 + (fh / harmonics_lp) ** 4) / h ** (0.5 if diesel else 0.8)
        sig += np.sin(h * phase + rng.uniform(0, 6.28)) * amp
    # Combustion roughness: amplitude flutter at the firing rate, stronger for a diesel.
    sig *= 1.0 + (0.35 if diesel else 0.15) * np.sin(phase * 0.5 + 1.0)
    if diesel:
        clatter = band_noise(m, seed + 2, 1200, 3500) * (0.5 + 0.5 * np.sin(phase)) ** 6
        sig += 0.8 * clatter * np.sqrt(np.mean(sig ** 2))
    return sig


def babble_voice(seconds, seed, rate):
    """One distant talker's phrase: a voiced harmonic source with a wandering pitch, shaped by two
    formants that jump per syllable, gated into 3 to 5 syllables a second."""
    rng = np.random.default_rng(seed)
    m = int(seconds * rate)
    t = np.arange(m) / rate
    base = rng.uniform(105, 235)
    f0 = base * (1.0 + 0.12 * slow_random(m, seed + 1, 2.5, rate=rate) * 0.5) * (1.0 - 0.1 * t / seconds)
    phase = 2 * np.pi * np.cumsum(f0) / rate
    syll_rate = rng.uniform(3.2, 5.0)
    count = max(2, int(seconds * syll_rate))
    edges = np.sort(rng.uniform(0, seconds, count))
    vowels = [(730, 1090), (270, 2290), (530, 1840), (570, 840), (300, 870), (660, 1720)]
    f1 = np.zeros(m)
    f2 = np.zeros(m)
    gate = np.zeros(m)
    for k, at in enumerate(edges):
        v1, v2 = vowels[rng.integers(len(vowels))]
        i0 = int(at * rate)
        d = rng.uniform(0.09, 0.22)
        i1 = min(m, i0 + int(d * rate))
        f1[i0:] = v1
        f2[i0:] = v2
        seg = np.arange(i1 - i0) / rate
        gate[i0:i1] = np.maximum(gate[i0:i1], np.sin(np.pi * seg / d) ** 1.5 * rng.uniform(0.5, 1.0))
    f1[f1 == 0] = 500
    f2[f2 == 0] = 1500
    # Glide the formants rather than step them.
    k = np.ones(int(0.04 * rate)) / int(0.04 * rate)
    f1 = np.convolve(f1, k, mode='same')
    f2 = np.convolve(f2, k, mode='same')
    sig = np.zeros(m)
    for h in range(1, 22):
        fh = f0 * h
        if base * h > 0.45 * rate:
            break
        amp = 1.0 / (1.0 + ((fh - f1) / 110) ** 2) + 0.5 / (1.0 + ((fh - f2) / 160) ** 2)
        sig += np.sin(h * phase) * amp / h ** 0.3
    env = np.clip(t / 0.05, 0, 1) * np.clip((seconds - t) / 0.08, 0, 1)
    return sig * gate * env


def band_events(mono, events):
    """How far each authored event rises over the bed's MEDIAN level in the band it lives in,
    in dB, over 100 ms windows. The check that an event is there to be heard at all: a steady
    noise bed hides a lot, and a far horn that measures 0 dB over the wash was never audible."""
    n = len(mono)
    f = np.fft.rfftfreq(n, 1.0 / RATE)
    spec = np.fft.rfft(mono)
    w = int(0.1 * RATE)
    out = {}
    for name, at, dur, lo, hi in events:
        y = np.fft.irfft(spec * ((f >= lo) & (f < hi)), n)
        frames = (y[:n // w * w].reshape(-1, w) ** 2).mean(axis=1)
        median = np.median(frames)
        a = int(at * RATE) // w
        b = max(a + 1, int((at + dur) * RATE) // w)
        idx = np.arange(a, b) % len(frames)
        out[name] = {'band_hz': [lo, hi], 'db_over_median': round(float(10 * np.log10(frames[idx].max() / median)), 1)}
    return out


def author_city_bed():
    n = int(BED_SECONDS * RATE)
    rng = np.random.default_rng(7700)
    left = np.zeros(n)
    right = np.zeros(n)
    info = {}

    def pan_add(sig, start, pan):
        # Equal-power pan, pan in [-1, 1].
        a = (pan + 1.0) * np.pi / 4
        add_wrapped(left, start, sig * np.cos(a))
        add_wrapped(right, start, sig * np.sin(a))

    # 1. The wash: a common part (the whole city, centred) and a per-ear part (width).
    breath = 0.82 + 0.18 * np.tanh(slow_random(n, 7701, 0.07))
    for ch, dst in enumerate((left, right)):
        # ⚠️ THE WASH STARTS AT 45 Hz, NOT 28. The first pass put the bed's loudest band at 40 Hz,
        # 40 dB above its 1 kHz: headroom spent on rumble a laptop speaker cannot play, and a
        # bed that read as a distant generator rather than a street. The traffic lives in the mids.
        wash = 0.75 * band_noise(n, 7710, 45, 320, tilt=-3) + 0.66 * band_noise(n, 7711 + ch, 45, 320, tilt=-3)
        dst += wash * 0.22 * breath
        mid = 0.7 * band_noise(n, 7715, 180, 1100, tilt=-3) + 0.7 * band_noise(n, 7716 + ch, 180, 1100, tilt=-3)
        dst += mid * 0.07 * breath
        # 2. Tyre hiss, the far ring roads, own per ear and breathing on its own clock.
        hiss = band_noise(n, 7720 + ch, 700, 5200, tilt=-4)
        dst += hiss * 0.050 * (0.8 + 0.2 * np.tanh(slow_random(n, 7725 + ch, 0.11)))

    # 3. Distant passes, each a swell that sweeps across. Intervals sum to the loop (circular).
    passes = []
    at = 0.0
    gaps = rng.uniform(2.4, 5.2, 12)
    gaps *= BED_SECONDS / gaps.sum()
    for k, gap in enumerate(gaps):
        d = rng.uniform(3.5, 6.5)
        m = int(d * RATE)
        u = np.linspace(0, 1, m)
        env = np.sin(np.pi * u) ** 2.4 * rng.uniform(0.45, 1.0)
        hiss = band_noise(m, 7730 + k, 500, 3800, tilt=-3)[:m]
        # The engine inside the pass drops in pitch across its centre, a gentle Doppler.
        fire = rng.uniform(32, 55)
        drop = rng.uniform(0.04, 0.07)
        tone = engine_sweep(d, 7740 + k, lambda uu: fire * (1 + drop / 2 - drop / (1 + np.exp(-(uu - 0.5) * 10))),
                            480, diesel=rng.uniform() < 0.35)
        tone /= rms(tone) + 1e-12
        sig = (0.10 * hiss + 0.05 * tone) * env
        start = int(at * RATE)
        direction = 1 if rng.uniform() < 0.5 else -1
        # Sweep: split into slices with a moving pan (crossfaded slices, so the sweep is smooth).
        pieces = 8
        edge = m // pieces
        fade = np.hanning(2 * edge)
        for p in range(pieces):
            i0 = max(0, p * edge - edge // 2)
            chunk = sig[i0:i0 + 2 * edge]
            w = fade[:len(chunk)]
            pan = direction * (-0.8 + 1.6 * (p + 0.5) / pieces)
            pan_add(chunk * w, start + i0, pan)
        passes.append(round(at, 2))
        at += gap
    info['passes_at'] = passes

    far = [np.zeros(n), np.zeros(n)]

    def far_add(sig, start, pan):
        a = (pan + 1.0) * np.pi / 4
        add_wrapped(far[0], start, sig * np.cos(a))
        add_wrapped(far[1], start, sig * np.sin(a))

    # 4. Far engines pulling away through the gears.
    pulls = []
    for k, (start_s, pan) in enumerate([(3.0, -0.6), (14.5, 0.5), (24.0, -0.2), (33.5, 0.7)]):
        d = rng.uniform(4.5, 6.0)
        shift = rng.uniform(0.42, 0.55)

        def gears(u, shift=shift):
            first = 26 + 40 * np.clip(u / shift, 0, 1) ** 0.8
            second = 38 + 22 * np.clip((u - shift) / (1 - shift), 0, 1) ** 0.9
            return np.where(u < shift, first, second)
        m = int(d * RATE)
        u = np.linspace(0, 1, m)
        tone = engine_sweep(d, 7750 + k, gears, 420)
        tone /= rms(tone)
        load = np.clip(0.55 + 0.45 * np.where(u < shift, u / shift, (u - shift) / (1 - shift)), 0, 1)
        # A dip at the gear change (the throttle lifts), then fading into the distance.
        dip = 1.0 - 0.6 * np.exp(-((u - shift) / 0.03) ** 2)
        env = np.clip(u / 0.12, 0, 1) * np.clip((1 - u) / 0.45, 0, 1) ** 1.3 * load * dip
        far_add(tone * env * 0.55, int(start_s * RATE), pan)
        pulls.append(start_s)
    info['engine_pulls_at'] = pulls

    # 5. One far jeepney rev: a diesel blipped twice at the stop, then pulling off.
    d = 3.4
    m = int(d * RATE)
    u = np.linspace(0, 1, m)
    blips = 24 + 26 * np.exp(-((u - 0.14) / 0.06) ** 2) + 30 * np.exp(-((u - 0.36) / 0.07) ** 2) \
        + 18 * np.clip((u - 0.6) / 0.4, 0, 1)
    tone = engine_sweep(d, 7760, lambda uu: np.interp(uu, u, blips), 520, diesel=True)
    tone /= rms(tone)
    env = np.clip(u / 0.05, 0, 1) * np.clip((1 - u) / 0.3, 0, 1) * (0.5 + 0.5 * (blips - 24) / 30)
    far_add(tone * env * 0.42, int(19.0 * RATE), 0.35)
    info['jeepney_rev_at'] = 19.0

    # 6. Three distant horns, darkened.
    horns = author_horns()
    # ⚠️ LEVELS SET BY MEASUREMENT, NOT BY EAR ALONE. At a third of these the second pass showed
    # no bump at all in the 500 to 2000 Hz band where a horn lives: the events were authored and
    # inaudible, and the bed read as one steady generator. These put each distant horn and the
    # jeepney rev a few dB over the wash in their own band (report: `band_events`), which is
    # "very occasional and far away", not "missing".
    for name, start_s, pan, g in [('kanto_horn_car_2.wav', 8.6, 0.55, 0.55),
                                  ('kanto_horn_car_1.wav', 21.7, -0.7, 0.45),
                                  ('kanto_horn_jeepney.wav', 29.8, -0.25, 0.40)]:
        h = lin_filter(horns[name], lambda f: peaks_response(f, [], floor=1.0, lo=200, hi=1900))
        far_add(h * g, int(start_s * RATE), pan)
    info['distant_horns_at'] = [8.6, 21.7, 29.8]

    # 7. Crowd murmur, synthesised at a quarter rate (it is all below 3 kHz) and upsampled
    # exactly by zero-padding the spectrum, which keeps it periodic in the loop length.
    q = 4
    nq = n // q
    rq = RATE // q
    crowd = [np.zeros(nq), np.zeros(nq)]
    talkers = 26
    phrases = 0
    for v in range(talkers):
        pan = rng.uniform(-0.9, 0.9)
        a = (pan + 1.0) * np.pi / 4
        t = rng.uniform(0, BED_SECONDS)
        span = 0.0
        while span < BED_SECONDS:
            d = rng.uniform(0.8, 2.6)
            ph = babble_voice(d, 8000 + v * 97 + phrases, rq) * rng.uniform(0.5, 1.0)
            add_wrapped(crowd[0], int((t + span) * rq), ph * np.cos(a))
            add_wrapped(crowd[1], int((t + span) * rq), ph * np.sin(a))
            span += d + rng.uniform(0.4, 2.8)
            phrases += 1
    info['crowd_phrases'] = phrases
    for ch in range(2):
        spec = np.fft.rfft(crowd[ch])
        up = np.zeros(n // 2 + 1, dtype=complex)
        up[:len(spec)] = spec
        c = np.fft.irfft(up, n) * q
        c = circ_filter(c, lambda f: peaks_response(f, [], floor=1.0, lo=180, hi=2200))
        far[ch] += c / (rms(c) + 1e-12) * 0.03

    wet = [street_reverb(far[0], seed=7801), street_reverb(far[1], seed=7802)]
    left += wet[0]
    right += wet[1]

    stereo = np.stack([left, right], axis=1)
    info['band_events'] = band_events(stereo.mean(axis=1), [
        ('distant_horn_car_2', 8.6, 0.4, 400, 2200), ('distant_horn_car_1', 21.7, 0.25, 400, 2200),
        ('distant_horn_jeepney', 29.8, 0.85, 400, 2200), ('jeepney_rev', 19.0, 2.0, 80, 700)]
        + [(f'engine_pull_{k + 1}', at, 3.0, 80, 700) for k, at in enumerate(pulls)])
    # ⚠️ A GENTLE SHELF ABOVE 4 kHz, THE BRIEF'S "NOT HARSH". Nothing in a far city reaches the
    # ear bright; the runtime's close engines and horns carry the detail.
    for ch in range(2):
        stereo[:, ch] = circ_filter(stereo[:, ch], lambda f: peaks_response(f, [], floor=1.0, lo=22, hi=5000, order=1))
    return seam_rotate(stereo), info


# ---------------------------------------------------------------------------------------------
# Seam, level, write, measure (the lagoon tool's, unchanged in method).
# ---------------------------------------------------------------------------------------------

def seam_rotate(x):
    """⚠️ Picks WHERE the file starts. The loop is already periodic, so the seam is continuous
    anywhere; this puts the file boundary where the 50 ms either side have the most similar
    loudness, so a tool that measures the ends sees nothing. Rotating a periodic signal changes
    nothing a listener hears."""
    mono = x if x.ndim == 1 else x.mean(axis=1)
    n = len(mono)
    w = SEAM_WINDOW
    energy = np.concatenate([mono, mono[:w]]) ** 2
    csum = np.concatenate([[0.0], np.cumsum(energy)])
    starts = np.arange(0, n, 147)
    after = np.sqrt((csum[starts + w] - csum[starts]) / w)
    before_idx = (starts - w) % n
    before = np.sqrt((csum[before_idx + w] - csum[before_idx]) / w)
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
            # ⚠️ A click is a seam step far outside what the signal does on its own.
            'click': bool(seam_step > p999),
        }
    return row


def fade_ends(x, attack=0.002, release=0.01):
    m = len(x)
    t = np.arange(m) / RATE
    return x * np.minimum(1, t / attack) * np.minimum(1, (t[-1] - t) / release)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    rows = []

    bed, bed_info = author_city_bed()
    # ⚠️ PEAKS: the bed at 0.50, engines at 0.60, horns at 0.70. Headroom for Unity's mixer summing
    # these with the whole match, never normalised to full scale (the lagoon's convention). The
    # bed is lowest because it plays all match long under everything.
    pcm = to_pcm(bed, 0.50)
    write_wav('kanto_city_bed.wav', pcm)
    row = measure('kanto_city_bed.wav', pcm, True)
    row.update(bed_info)
    rows.append(row)

    for name, fn in [('kanto_engine_car.wav', author_engine_car),
                     ('kanto_engine_diesel.wav', author_engine_diesel),
                     ('kanto_engine_tricycle.wav', author_engine_tricycle)]:
        sig, info = fn()
        pcm = to_pcm(sig, 0.60)
        write_wav(name, pcm)
        row = measure(name, pcm, True)
        row.update(info)
        rows.append(row)

    for name, sig in author_horns().items():
        pcm = to_pcm(fade_ends(sig), 0.70)
        write_wav(name, pcm)
        rows.append(measure(name, pcm, False))

    for row in rows:
        seam = row.get('seam')
        if seam and (seam['rms_difference_percent'] >= 10 or seam['click']):
            raise SystemExit(f"{row['file']}: seam check failed {seam}")
    for row in rows:
        if 'horn' in row['file'] and not (0.2 <= row['seconds'] <= 1.2):
            raise SystemExit(f"{row['file']}: {row['seconds']} s is outside the brief's 0.2 to 1.2 s")

    result = {
        'provenance': 'Original deterministic synthesis (numpy, fixed seeds). No external samples, recordings or paid API.',
        'listening': 'OPEN. Technical measurements only (peak, RMS, seam). Sourced and authored SFX stay '
                     'provisional until the owner hears them in play (CLAUDE.md section 6).',
        'tool': 'tools/build_kanto_street_audio.py',
        'runtime': 'Assets/TumbangPreso/Runtime/Map/KantoStreetSound.cs',
        'seam_rule': 'Loops are built periodic; first and last 50 ms RMS within 10 per cent and a seam '
                     'step no larger than the 99.9th percentile step inside the loop.',
        'engine_authoring_rate': 'Idle to low cruise; the runtime pitches up with speed (about 0.85 to 1.6).',
        'authoring_passes': [
            'Pass 1: bed loudest at 40 Hz and 40 dB over its 1 kHz; car engine swung 20 dB per firing '
            '(1.6 ms pulses, Q up to 3); diesel knock showed four narrow tonal peaks; one-shot horns '
            'carried a wrapped pre-ring tail (a 0.22 s beep measured 0.475 s).',
            'Pass 2: wash moved to 45 to 320 Hz; car pulses widened to 2.6 ms with lower-Q body; '
            'knock rebuilt as a noise burst with rings that wander 10 per cent; one-shot filter padded '
            'both sides. Distant horns and far engines still measured about 0 to 1 dB over the wash in '
            'their bands.',
            'Pass 3: far events raised and the mid wash lowered until every authored event measured '
            '2.5 to 4.3 dB over the median in its own band (band_events on the bed row).',
        ],
        'files': rows,
    }
    REPORT.parent.mkdir(parents=True, exist_ok=True)
    REPORT.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    for row in rows:
        extra = f" seam {row['seam']['rms_difference_percent']}% click={row['seam']['click']}" if 'seam' in row else ''
        print(f"{row['file'].split('/')[-1]}: {row['seconds']} s, {row['channels']} ch, peak {row['peak']}, rms {row['rms']}{extra}")


if __name__ == '__main__':
    main()
