"""Kanto street sound: a city-block bed, three engine loops, horns and sirens, by deterministic synthesis.

Writes ONLY these files into Assets/TumbangPreso/Art/audio/ambience/:
    kanto_city_bed.wav
    kanto_engine_car.wav, kanto_engine_diesel.wav, kanto_engine_tricycle.wav
    kanto_horn_car_1.wav .. kanto_horn_car_8.wav, kanto_horn_jeepney_1.wav .. kanto_horn_jeepney_3.wav,
    kanto_horn_bus.wav, kanto_horn_truck.wav, kanto_horn_tricycle_1.wav, kanto_horn_tricycle_2.wav
    kanto_siren_1.wav .. kanto_siren_4.wav
and the measurement record docs/reports/kanto-street-audio-authoring.json.

Owner brief (2026-09-27, about Kanto's moving traffic): "needs sfx, bustling city ambience, car
engine and driving sounds, beep/horns". After hearing the first set in play, the same day: "the
sfx for the city is too calm. need to be more bustling, add some sirens here and louder and more
variety of beeps". ⚠️ The single-file `kanto_horn_jeepney.wav` and `kanto_horn_tricycle.wav` of
the first set were replaced by the numbered sets and removed; this tool never writes them again.
`Runtime/Map/KantoStreetSound.cs` plays these; this file only authors them.

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

# ⚠️ 50 s (it was 38). A busier bed has far more events to repeat, and at 38 s a listener starts
# recognising the same horn after the same pass-by within two laps. 50 s is inside the rework
# brief's 40 to 60, and not a multiple of any engine loop (3.0, 3.2, 2.5 s).
BED_SECONDS = 50.0

# The first bed's measured RMS (peak 0.50), kept for the before-and-after in the report.
FIRST_BED_RMS = 0.0846

AUTHORING_PASSES = [
    'Set 1, pass 1: bed loudest at 40 Hz and 40 dB over its 1 kHz; car engine swung 20 dB per firing '
    '(1.6 ms pulses, Q up to 3); diesel knock showed four narrow tonal peaks; one-shot horns carried a '
    'wrapped pre-ring tail (a 0.22 s beep measured 0.475 s).',
    'Set 1, pass 2: wash moved to 45 to 320 Hz; car pulses widened to 2.6 ms with lower-Q body; knock '
    'rebuilt as a noise burst with rings that wander 10 per cent; one-shot filter padded both sides. '
    'Distant horns and far engines still measured about 0 to 1 dB over the wash in their bands.',
    'Set 1, pass 3: far events raised and the mid wash lowered until every event measured 2.5 to 4.3 dB '
    'over the median in its own band.',
    'Owner heard set 1 in play (2026-09-27): too calm, add sirens, louder and more varied beeps.',
    'Set 2, pass 1: 50 s bed with 81 events; every kind only 0.9 to 1.8 dB over the bed median in its '
    'band because the pass-bys buried them (horns about 14 dB under the pass-by layer).',
    'Set 2, pass 2: pass-bys down a third, horns up about 3x near and far, vendor calls and revs up; '
    'horns now a median 5.2 dB over the bed, vendor calls 3.9, revs 2.8. Soft limiter (tanh, drive 2.2) '
    'takes the bed RMS from 0.0846 to about 0.16 at a 0.60 peak. Bus horn cut at 2.8 kHz after its '
    'first render ran bright to 8 kHz.',
]

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


def car_horn(f_lo, f_hi, seconds, seed, housing=2300, duty=(0.28, 0.24), lo=260, drive=1.8, pair=0.9):
    """One press of an electric car horn pair. `housing` moves the trumpet or disc resonance; a
    higher housing, a narrower duty and a higher low cut make a tinnier horn."""
    a = horn_tone(f_lo, seconds, duty[0], seed)
    b = horn_tone(f_hi, seconds, duty[1], seed + 10)
    x = (a + pair * b) * horn_env(seconds)
    x = lin_filter(x, lambda f: peaks_response(
        f, [(housing, 3.5, 1.0), (housing * 0.42, 2.5, 0.8), (housing * 1.6, 5.0, 0.35)],
        floor=0.15, lo=lo, hi=5200, order=2))
    x /= np.max(np.abs(x)) + 1e-12
    return np.tanh(drive * x)


def taps(presses, f_lo, f_hi, seed, **kw):
    """presses: [(start, seconds)], one horn pair pressed several times."""
    return sequence([(s, car_horn(f_lo, f_hi, d, seed + 3 * k, **kw)) for k, (s, d) in enumerate(presses)])


def air_trumpet(f0, seconds, seed, vibrato=0.0, bloom=0.035):
    """One air trumpet. ⚠️ BRASSY MEANS THE BRIGHTNESS FOLLOWS THE LEVEL: as the air pressure
    builds, more upper harmonics speak, so the attack blooms from dark to bright. A fixed spectrum
    at a fixed level reads as an organ stop."""
    rng = np.random.default_rng(seed)
    m = int(seconds * RATE)
    t = np.arange(m) / RATE
    env = np.clip(t / bloom, 0, 1) ** 1.5 * np.clip((seconds - t) / 0.06, 0, 1)
    env *= 0.94 + 0.06 * np.exp(-t / 0.2)
    f = f0 * (1.0 - 0.05 * np.exp(-t / 0.03)) * (1.0 + vibrato * np.sin(2 * np.pi * 5.2 * t) * np.clip((t - 0.15) / 0.2, 0, 1))
    phase = 2 * np.pi * np.cumsum(f) / RATE + rng.uniform(0, 6.28)
    sig = np.zeros(m)
    for h in range(1, 60):
        if h * f0 > 0.45 * RATE:
            break
        bright = np.exp(-h / (2.0 + 9.0 * env))
        sig += np.sin(h * phase) * bright / h ** 0.6
    return sig * env


def air_chord(roots, seconds, seed, vibrato=0.0, bloom=0.035, peaks=None, lo=200, drive=1.4, hiss=0.0, hi=7000):
    """Several air trumpets sounding together (a dual or triple air horn), through the bell."""
    x = np.zeros(int(seconds * RATE))
    for k, r in enumerate(roots):
        x += air_trumpet(r, seconds, seed + k, vibrato, bloom) * (1.0 if k == 0 else 0.85)
    if hiss:
        # The valve opening: a short burst of air before the reeds speak.
        m = min(len(x), int(0.12 * RATE))
        x[:m] += band_noise(m, seed + 50, 1800, 7000)[:m] * np.exp(-np.arange(m) / (0.03 * RATE)) * hiss
    peaks = peaks or [(1250, 2.2, 1.0), (2600, 3.0, 0.7), (650, 1.8, 0.6)]
    x = lin_filter(x, lambda f: peaks_response(f, peaks, floor=0.2, lo=lo, hi=hi))
    x /= np.max(np.abs(x)) + 1e-12
    return np.tanh(drive * x)


def jeep_note(root, seconds, seed, vibrato=0.0):
    # Two trumpets a major third apart, the upper a hair sharp of true: the jeepney's chord.
    return air_chord([root, root * 1.26 * 1.004], seconds, seed, vibrato)


def trike_note(seconds, seed, f0=510, drift=0.008, scoop=0.05):
    """One small disc horn on a motorcycle's weak 12 V.

    ⚠️ NASAL COMES FROM A NARROW HIGH HOUSING PEAK AND NO BODY BELOW 600 Hz. A single tone (no
    pair), a narrower pulse (more upper harmonics) and a buzzier overdrive."""
    x = horn_tone(f0, seconds, 0.14, seed, scoop=scoop, drift=drift) * horn_env(seconds, 0.006, 0.02)
    x = lin_filter(x, lambda f: peaks_response(
        f, [(2850, 6.0, 1.0), (1700, 4.0, 0.45), (4300, 6.0, 0.25)], floor=0.06, lo=650, hi=7500, order=3))
    x /= np.max(np.abs(x)) + 1e-12
    return np.tanh(2.4 * x)


def author_horns():
    """Every horn in the set, by file name.

    ⚠️⚠️ VARIETY IS THE BRIEF (owner, 2026-09-27: "louder and more variety of beeps"). Three car
    horns repeating every few seconds read as one car honking over and over. So each file is a
    different CAR (its own pair of pitches and housing) or a different DRIVER (tap, double, triple,
    lean-on, stutter), and the runtime picks at random and detunes each honk a few per cent."""
    horns = {}
    # 1. Short polite tap.
    horns['kanto_horn_car_1.wav'] = taps([(0.0, 0.16)], 415, 500, 7401)
    # 2. Double tap, a car tuned a major third apart.
    horns['kanto_horn_car_2.wav'] = taps([(0.0, 0.11), (0.18, 0.18)], 440, 554, 7411, housing=2500)
    # 3. Triple tap: "move, move, MOVE", the last one held.
    horns['kanto_horn_car_3.wav'] = taps([(0.0, 0.09), (0.15, 0.09), (0.30, 0.2)], 415, 523, 7421, housing=2400)
    # 4. The long lean-on-the-horn, 1.7 s, with a re-grip dip halfway and the battery sagging.
    lean = car_horn(392, 470, 1.7, 7431, housing=2100)
    t = np.arange(len(lean)) / RATE
    lean *= (1.0 - 0.55 * np.exp(-((t - 0.95) / 0.035) ** 2)) * (1.0 - 0.1 * t / 1.7)
    horns['kanto_horn_car_4.wav'] = sequence([(0.0, lean)])
    # 5. High tinny hatchback: a small single disc pair, narrow pulses, no body below 600 Hz.
    horns['kanto_horn_car_5.wav'] = taps([(0.0, 0.1), (0.16, 0.15)], 560, 680, 7441, housing=3300,
                                         duty=(0.16, 0.14), lo=600, drive=2.2)
    # 6. Low sedan horn: a big car's deep pair, darker housing, one firm press.
    horns['kanto_horn_car_6.wav'] = taps([(0.0, 0.55)], 330, 415, 7451, housing=1800, lo=180)
    # 7. Two-tone European style: trumpet horns rather than discs, so the two notes are TUNEFUL
    # (brass bloom through a trumpet bell) instead of a buzz.
    horns['kanto_horn_car_7.wav'] = sequence([(0.0, air_chord([370, 466], 0.5, 7461, bloom=0.015,
                                                               peaks=[(1500, 2.5, 1.0), (3000, 3.0, 0.6)],
                                                               lo=280, drive=1.9))])
    # 8. The nervous stutter: six uneven jabs, a driver who cannot decide.
    presses = []
    at = 0.0
    for d, gap in [(0.06, 0.07), (0.05, 0.05), (0.09, 0.10), (0.05, 0.06), (0.07, 0.08), (0.12, 0.0)]:
        presses.append((at, d))
        at += d + gap
    horns['kanto_horn_car_8.wav'] = taps(presses, 440, 523, 7471, housing=2600)

    # Jeepneys: the brassy air horns.
    # 1. The musical call: an original four-note rising arpeggio, the last note held.
    horns['kanto_horn_jeepney_1.wav'] = sequence([
        (0.0, jeep_note(349.2, 0.12, 7501)), (0.15, jeep_note(440.0, 0.12, 7503)),
        (0.30, jeep_note(523.3, 0.12, 7505)), (0.45, jeep_note(698.5, 0.5, 7507, vibrato=0.006)),
    ])
    # 2. The two-note "ta-DAAH".
    horns['kanto_horn_jeepney_2.wav'] = sequence([
        (0.0, jeep_note(349.2, 0.17, 7511)),
        (0.21, jeep_note(466.2, 0.62, 7513, vibrato=0.006)),
    ])
    # 3. The long one: one held chord, 1.3 s, the air valve's vibrato opening up.
    horns['kanto_horn_jeepney_3.wav'] = sequence([(0.0, jeep_note(330.0, 1.3, 7521, vibrato=0.008))])

    # The city bus: a deep triple air horn, a slower bloom (bigger reeds) and the valve's hiss.
    # ⚠️ CUT AT 2.8 kHz. The first render ran bright to 8 kHz (three low reeds through the overdrive
    # throw a lot of upper partials) and read as a brass section, not a big deep horn.
    horns['kanto_horn_bus.wav'] = sequence([(0.0, air_chord(
        [174.6, 220.0, 261.6], 1.05, 7601, vibrato=0.004, bloom=0.08,
        peaks=[(700, 1.8, 1.0), (1400, 2.5, 0.7), (2400, 3.0, 0.3)], lo=90, drive=1.6, hiss=0.5, hi=2800))])

    # Vans and pickups: a lower, rougher electric pair, a quick tap then a held press.
    horns['kanto_horn_truck.wav'] = taps([(0.0, 0.2), (0.3, 0.45)], 300, 378, 7611, housing=1700,
                                         duty=(0.32, 0.3), lo=160, drive=2.0)

    # Tricycles.
    # 1. "Meep-meep".
    horns['kanto_horn_tricycle_1.wav'] = sequence([(0.0, trike_note(0.11, 7701)), (0.17, trike_note(0.15, 7702))])
    # 2. The weak-battery "meeeep": one longer press whose pitch sags and wobbles.
    long_meep = trike_note(0.42, 7711, f0=540, drift=0.02, scoop=0.08)
    t = np.arange(len(long_meep)) / RATE
    horns['kanto_horn_tricycle_2.wav'] = sequence([(0.0, long_meep * (1.0 - 0.15 * t / 0.42))])

    for name in horns:
        horns[name] = trim_tail(horns[name])
    return horns


# ---------------------------------------------------------------------------------------------
# Sirens.
#
# Four emergency vehicles passing somewhere across the block. Each file is a WHOLE PASS, not a
# loop and not a clip that starts and stops: it swells in from the distance, peaks as the vehicle
# is nearest, and fades away, with a gentle Doppler bend (a few per cent sharp approaching, flat
# receding) baked in. The runtime also moves the source along a road, so the pass is heard
# travelling; the baked swell is kept gentle (about 10 dB) so the two do not double up.
# Distance: band-limited (a siren across a block keeps little above 4 kHz), and smeared by a short
# street reverb so it is heard off the buildings as much as directly.
# ---------------------------------------------------------------------------------------------

def smear(x, seconds=0.7, wet=0.4, seed=7901):
    """A short linear reverb: the siren off the facades."""
    rng = np.random.default_rng(seed)
    m = int(seconds * RATE)
    t = np.arange(m) / RATE
    ir = rng.normal(size=m) * np.exp(-t / (seconds / 6.9))
    ir[0] = 0.0
    ir /= np.sqrt(np.sum(ir ** 2))
    size = len(x) + m
    y = np.fft.irfft(np.fft.rfft(x, size) * np.fft.rfft(ir, size), size)[:len(x)]
    y = circ_filter(np.concatenate([y, np.zeros(int(0.1 * RATE))]),
                    lambda f: peaks_response(f, [], floor=1.0, lo=200, hi=2500))[:len(x)]
    return (1.0 - wet) * x + wet * y * (rms(x) / (rms(y) + 1e-12))


def siren_pass(f_curve, seconds, seed, mechanical=False, peak_at=0.52, bend=0.035):
    rng = np.random.default_rng(seed)
    m = int(seconds * RATE)
    t = np.arange(m) / RATE
    tp = peak_at * seconds
    # ⚠️ DOPPLER AS A SMOOTH STEP, NOT A SWITCH: the pitch leans sharp while approaching and flat
    # after, crossing over the 1.8 s either side of the nearest point, the way a vehicle 40 m away
    # sounds (a close one flips in a fraction of a second, which would sound like a fault).
    dop = 1.0 + bend * -np.tanh((t - tp) / 0.9)
    f = f_curve(t) * dop
    phase = 2 * np.pi * np.cumsum(f) / RATE + rng.uniform(0, 6.28)
    sig = np.zeros(m)
    if mechanical:
        # A rotor siren: a rounder wave (harmonics fall faster), a sub-octave growl from the rotor,
        # and air noise rushing through the ports.
        for h in range(1, 16):
            sig += np.sin(h * phase) / h ** 1.5 * (np.cos(h * 0.3) * 0.3 + 0.7)
        sig += 0.25 * np.sin(0.5 * phase)
        air = band_noise(m, seed + 3, 700, 3200)
        sig += 0.18 * air * (0.6 + 0.4 * np.sin(phase)) * np.sqrt(np.mean(sig ** 2))
    else:
        # An electronic siren driving a horn speaker: a square-ish wave (odd harmonics) plus a
        # little even content from the amplifier, overdriven.
        for h in range(1, 20):
            if h * 1600 > 0.45 * RATE:
                break
            sig += np.sin(h * phase) / h * (1.0 if h % 2 else 0.18)
        sig = np.tanh(1.5 * sig / np.max(np.abs(sig)))
    x = lin_filter(sig, lambda ff: peaks_response(ff, [(1100, 1.8, 1.0), (2300, 2.5, 0.6)],
                                                   floor=0.25, lo=250, hi=4200))[:m]
    # The pass: a gentle swell to the nearest point and away, about 10 dB across the whole file,
    # and a 1 s fade at each end so it neither starts nor stops.
    d0 = 40.0
    v = 17.0
    dist = np.sqrt(d0 ** 2 + (v * (t - tp)) ** 2)
    swell = (d0 / dist) ** 0.6
    edge = np.clip(t / 1.0, 0, 1) * np.clip((seconds - t) / 1.2, 0, 1)
    edge = edge * edge * (3 - 2 * edge)
    # The far half is darker: air takes the top end first.
    dark = lin_filter(x, lambda ff: peaks_response(ff, [], floor=1.0, lo=250, hi=1300))[:m]
    near = swell ** 1.5
    x = x * near + dark * (1.0 - near)
    x = x * swell * edge
    return smear(x, seed=seed + 7)


def author_sirens():
    sirens = {}

    # 1. Police wail: a slow sweep up (2.6 s) and down (1.4 s), 700 to 1350 Hz.
    def wail(t):
        u = (t % 4.0) / 4.0
        s = np.where(u < 0.65, 0.5 - 0.5 * np.cos(np.pi * u / 0.65), 0.5 + 0.5 * np.cos(np.pi * (u - 0.65) / 0.35))
        return 700 + 650 * s
    sirens['kanto_siren_1.wav'] = siren_pass(wail, 8.0, 7801)

    # 2. Yelp: the same sweep at 3.3 cycles a second.
    sirens['kanto_siren_2.wav'] = siren_pass(lambda t: 720 + 700 * (0.5 - 0.5 * np.cos(2 * np.pi * 3.3 * t)), 6.0, 7811)

    # 3. Ambulance hi-lo: 960 and 770 Hz alternating every 0.55 s, with 15 ms glides between.
    def hilo(t):
        hi = ((t % 1.1) < 0.55).astype(float)
        k = int(0.015 * RATE)
        return 770 + 190 * np.convolve(hi, np.ones(k) / k, mode='same')
    sirens['kanto_siren_3.wav'] = siren_pass(hilo, 7.0, 7821, bend=0.03)

    # 4. Fire truck mechanical wail: the rotor spins up over 3 s, holds with a wobble, winds down
    # partway and is wound up again.
    def rotor(t):
        up = 180 + 870 * (1 - np.exp(-t / 1.0))
        wobble = 1 + 0.02 * np.sin(2 * np.pi * 0.7 * t)
        down = np.where(t > 5.5, np.exp(-(t - 5.5) / 1.4), 1.0)
        rewind = np.where(t > 7.2, 1 - np.exp(-(t - 7.2) / 0.6), 0.0)
        lvl = down + (1 - down) * rewind * 0.9
        return np.maximum(180, up * wobble * (0.45 + 0.55 * lvl))
    sirens['kanto_siren_4.wav'] = siren_pass(rotor, 9.0, 7831, mechanical=True, peak_at=0.45, bend=0.03)
    return sirens


# ---------------------------------------------------------------------------------------------
# The city bed, stereo.
#
# A busy Manila block at rush hour heard from the small park in the middle of it. Layers:
#   1. a continuous far WASH of the city (30 to 320 Hz rumble, mids, tyre hiss) that breathes;
#   2. PASS-BYS at mid distance: the three engine loops driven past with real Doppler and 1/d level;
#   3. TRICYCLES puttering past, slower and nearer the kerb;
#   4. JEEPNEY AND BUS REVS at the stops;
#   5. HORNS, every kind in the set, some mid-distance and bright, some far and dark;
#   6. a CROWD MURMUR of 40 talkers too far away to have words, and
#   7. VENDOR CALLS over it, the shape of a call without words.
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


def loop_read(loop, rate):
    """Reads a periodic loop at a time-varying playback rate (1 = as authored), wrapping, with
    linear interpolation: a whole engine sped up and slowed down, Doppler and revs included."""
    pos = np.cumsum(rate)
    n = len(loop)
    i0 = np.floor(pos).astype(np.int64)
    frac = pos - i0
    i0 %= n
    return loop[i0] * (1.0 - frac) + loop[(i0 + 1) % n] * frac


def spread_times(count, seconds, rng):
    """`count` start times round the loop, one per equal slot at a random point inside it: busy
    everywhere, never two piled on one spot, and irregular across the seam like everywhere else."""
    return (np.arange(count) + rng.uniform(0.05, 0.95, count)) * seconds / count


def pass_by(loop, seconds, seed, speed, d0, rpm, tyre_gain, rng):
    """A vehicle driving past at `speed` m/s, `d0` m away at its nearest (the middle of the file).

    Returns (left, right). ⚠️ REAL GEOMETRY, NOT A FADE: the distance sets the level (1/d), its
    rate of change sets the Doppler (c / (c + v_radial)) on the whole engine loop, the side sets
    the pan, and the air takes the top end off the far half. That is what makes a pass WHOOSH by
    rather than swell up and down in place."""
    m = int(seconds * RATE)
    t = np.arange(m) / RATE
    x = speed * (t - seconds / 2)
    d = np.sqrt(d0 ** 2 + x ** 2)
    radial = speed * x / d
    doppler = 343.0 / (343.0 + radial)
    engine = loop_read(loop, rpm * doppler * (1.0 + 0.01 * slow_random(m, seed, 2.0)))
    tyre = band_noise(m, seed + 1, 350, 5000, tilt=-3)
    near = d0 / d
    bright = circ_filter(engine * 0.6 + tyre * tyre_gain, lambda f: peaks_response(f, [], floor=1.0, lo=60, hi=6000))
    dark = circ_filter(engine * 0.6 + tyre * tyre_gain, lambda f: peaks_response(f, [], floor=1.0, lo=60, hi=1200))
    air = near ** 1.5
    sig = (bright * air + dark * (1.0 - air)) * near
    edge = np.clip(t / 0.6, 0, 1) * np.clip((seconds - t) / 0.6, 0, 1)
    sig *= edge * edge * (3 - 2 * edge)
    pan = np.clip(x / d, -1, 1) * rng.choice([-0.85, 0.85])
    a = (pan + 1.0) * np.pi / 4
    return sig * np.cos(a), sig * np.sin(a)


def vendor_call(seed, rate):
    """A street vendor's call heard from down the block: two or three PROJECTED syllables, the
    last one held and falling, a voiced source through vowel formants. No words, only the shape
    of a call (the "ta-hooo" contour), which is what the ear picks out of a crowd."""
    rng = np.random.default_rng(seed)
    syllables = rng.integers(2, 4)
    parts = []
    at = 0.0
    base = rng.uniform(170, 300)
    vowels = [(730, 1090), (570, 840), (300, 870), (530, 1840), (660, 1720)]
    for k in range(syllables):
        last = k == syllables - 1
        d = rng.uniform(0.5, 0.9) if last else rng.uniform(0.14, 0.26)
        m = int(d * rate)
        t = np.arange(m) / rate
        top = base * (1.25 if k == syllables - 2 else 1.0)
        f0 = top * (1.0 - (0.22 if last else 0.04) * (t / d) ** 1.4) * (1.0 + 0.01 * np.sin(2 * np.pi * 5.5 * t))
        phase = 2 * np.pi * np.cumsum(f0) / rate
        f1, f2 = vowels[rng.integers(len(vowels))]
        sig = np.zeros(m)
        for h in range(1, 18):
            if base * h > 0.45 * rate:
                break
            fh = f0 * h
            amp = 1.0 / (1.0 + ((fh - f1) / 120) ** 2) + 0.6 / (1.0 + ((fh - f2) / 170) ** 2)
            sig += np.sin(h * phase) * amp
        env = np.clip(t / 0.03, 0, 1) * np.clip((d - t) / (0.25 if last else 0.04), 0, 1)
        breath = band_noise(max(m, 64), seed + 10 + k, 1000, 3500, rate=rate)[:m] * 0.08
        parts.append((at, (sig / (np.max(np.abs(sig)) + 1e-9) + breath) * env))
        at += d + rng.uniform(0.04, 0.12)
    out = np.zeros(int((at + 0.1) * rate))
    for s, p in parts:
        i = int(s * rate)
        out[i:i + len(p)] += p
    return out


def author_city_bed(engines):
    """⚠️⚠️ REWORKED 2026-09-27 AFTER THE OWNER HEARD THE FIRST BED IN PLAY: "the sfx for the city
    is too calm. need to be more bustling". The first bed was a far wash with 20 events in 38 s
    (31.6 a minute) sitting 2.5 to 4.3 dB over it. This one is a block at rush hour: a NEARER layer
    of vehicles whooshing past at mid distance (the real engine loops, driven past with Doppler),
    jeepney and bus revs, tricycles puttering by, horns of every kind near and far, vendors calling
    over a thicker crowd, and the old far wash underneath. The event count is in the report."""
    n = int(BED_SECONDS * RATE)
    rng = np.random.default_rng(7700)
    left = np.zeros(n)
    right = np.zeros(n)
    far = [np.zeros(n), np.zeros(n)]
    info = {}
    events = []   # (category, start seconds, duration, band lo, band hi) for band_events

    def add(dst_l, dst_r, sig_l, sig_r, start):
        add_wrapped(dst_l, int(start * RATE), sig_l)
        add_wrapped(dst_r, int(start * RATE), sig_r)

    def pan_add(dst, sig, start, pan):
        a = (pan + 1.0) * np.pi / 4
        add_wrapped(dst[0], int(start * RATE), sig * np.cos(a))
        add_wrapped(dst[1], int(start * RATE), sig * np.sin(a))

    loops = {k: v / rms(v) for k, v in engines.items()}

    # 1. The far wash, as before but a little fuller: the city behind the city.
    breath = 0.82 + 0.18 * np.tanh(slow_random(n, 7701, 0.07))
    for ch, dst in enumerate((left, right)):
        wash = 0.75 * band_noise(n, 7710, 45, 320, tilt=-3) + 0.66 * band_noise(n, 7711 + ch, 45, 320, tilt=-3)
        dst += wash * 0.20 * breath
        mid = 0.7 * band_noise(n, 7715, 180, 1100, tilt=-3) + 0.7 * band_noise(n, 7716 + ch, 180, 1100, tilt=-3)
        dst += mid * 0.07 * breath
        hiss = band_noise(n, 7720 + ch, 700, 5200, tilt=-4)
        dst += hiss * 0.055 * (0.8 + 0.2 * np.tanh(slow_random(n, 7725 + ch, 0.11)))

    # 2. Mid-distance pass-bys: cars, taxis, vans, the odd bus or jeepney, 20 to 45 m out.
    count = 30
    kinds = []
    for k, at in enumerate(spread_times(count, BED_SECONDS, rng)):
        kind = rng.choice(['car', 'car', 'car', 'diesel', 'diesel'])
        speed = rng.uniform(8.0, 14.0)
        d0 = rng.uniform(20.0, 45.0)
        dur = rng.uniform(4.0, 6.5)
        rpm = rng.uniform(1.15, 1.5) if kind == 'car' else rng.uniform(1.05, 1.35)
        l, r = pass_by(loops[kind], dur, 7900 + k, speed, d0, rpm, 0.9, rng)
        g = rng.uniform(0.5, 1.0) * 0.20 * (20.0 / d0) ** 0.5
        add(left, right, l * g, r * g, at)
        kinds.append(str(kind))
        events.append(('pass_by', at, dur, 250, 2500))
    info['pass_bys'] = {'count': count, 'kinds': {k: kinds.count(k) for k in sorted(set(kinds))}}

    # 3. Tricycles puttering past, slower and closer to the kerb.
    count = 8
    for k, at in enumerate(spread_times(count, BED_SECONDS, rng)):
        dur = rng.uniform(4.5, 7.0)
        l, r = pass_by(loops['tricycle'], dur, 7950 + k, rng.uniform(6.0, 9.0), rng.uniform(16.0, 35.0),
                       rng.uniform(1.2, 1.55), 0.35, rng)
        g = rng.uniform(0.6, 1.0) * 0.20
        add(left, right, l * g, r * g, at)
        events.append(('tricycle_pass', at, dur, 250, 2500))
    info['tricycle_passes'] = count

    # 4. Jeepney and bus revs at the stops: the diesel loop blipped and pulled away, standing still.
    count = 7
    for k, at in enumerate(spread_times(count, BED_SECONDS, rng)):
        dur = rng.uniform(2.6, 3.8)
        m = int(dur * RATE)
        u = np.linspace(0, 1, m)
        b1, b2 = rng.uniform(0.1, 0.2), rng.uniform(0.3, 0.45)
        rate = 0.9 + 0.7 * np.exp(-((u - b1) / 0.06) ** 2) + 0.8 * np.exp(-((u - b2) / 0.07) ** 2) \
            + 0.5 * np.clip((u - 0.6) / 0.4, 0, 1)
        if rng.uniform() < 0.4:
            rate *= 0.8   # a bus: bigger, slower engine
        tone = loop_read(loops['diesel'], rate)
        env = np.clip(u / 0.04, 0, 1) * np.clip((1 - u) / 0.3, 0, 1) * (0.55 + 0.45 * (rate - 0.9) / 0.8)
        pan_add(far, tone * env * rng.uniform(0.35, 0.5), at, rng.uniform(-0.8, 0.8))
        events.append(('diesel_rev', at, dur, 80, 700))
    info['diesel_revs'] = count

    # 5. Horns, every kind, near-ish and far. Near ones keep their brightness; far ones are dark
    # and wetter. ⚠️ NO TWO HORNS IN A ROW ARE THE SAME FILE, or the variety is thrown away.
    library = author_horns()
    names = sorted(library)
    count = 26
    last = None
    near_count = 0
    for k, at in enumerate(spread_times(count, BED_SECONDS, rng)):
        name = names[rng.integers(len(names))]
        while name == last:
            name = names[rng.integers(len(names))]
        last = name
        h = library[name]
        near = rng.uniform() < 0.55
        rate = rng.uniform(0.95, 1.05)
        h = np.interp(np.arange(0, len(h) - 1, rate), np.arange(len(h)), h)
        if near:
            near_count += 1
            h = lin_filter(h, lambda f: peaks_response(f, [], floor=1.0, lo=200, hi=4200))
            pan_add((left, right), h * rng.uniform(0.7, 1.0), at, rng.uniform(-0.9, 0.9))
            pan_add(far, h * 0.05, at, 0.0)
        else:
            h = lin_filter(h, lambda f: peaks_response(f, [], floor=1.0, lo=200, hi=1900))
            pan_add(far, h * rng.uniform(1.3, 1.9), at, rng.uniform(-0.9, 0.9))
        events.append(('horn', at, len(h) / RATE, 400, 2200))
    info['horns'] = {'count': count, 'mid_distance': near_count, 'far': count - near_count}

    # 6. Crowd murmur, thicker (40 talkers), at a quarter rate and upsampled exactly.
    q = 4
    nq = n // q
    rq = RATE // q
    crowd = [np.zeros(nq), np.zeros(nq)]
    phrases = 0
    for v in range(40):
        pan = rng.uniform(-0.9, 0.9)
        a = (pan + 1.0) * np.pi / 4
        t0 = rng.uniform(0, BED_SECONDS)
        span = 0.0
        while span < BED_SECONDS:
            d = rng.uniform(0.8, 2.6)
            ph = babble_voice(d, 8000 + v * 97 + phrases, rq) * rng.uniform(0.5, 1.0)
            add_wrapped(crowd[0], int((t0 + span) * rq), ph * np.cos(a))
            add_wrapped(crowd[1], int((t0 + span) * rq), ph * np.sin(a))
            span += d + rng.uniform(0.3, 2.2)
            phrases += 1
    info['crowd_phrases'] = phrases

    # 7. Vendor calls over the crowd, from the stalls round the block.
    count = 10
    for k, at in enumerate(spread_times(count, BED_SECONDS, rng)):
        call = vendor_call(8500 + k, rq)
        pan = rng.uniform(-0.9, 0.9)
        a = (pan + 1.0) * np.pi / 4
        g = rng.uniform(2.2, 3.0)
        add_wrapped(crowd[0], int(at * rq), call * g * np.cos(a))
        add_wrapped(crowd[1], int(at * rq), call * g * np.sin(a))
        events.append(('vendor_call', at, len(call) / rq, 250, 1500))
    info['vendor_calls'] = count

    for ch in range(2):
        spec = np.fft.rfft(crowd[ch])
        up = np.zeros(n // 2 + 1, dtype=complex)
        up[:len(spec)] = spec
        c = np.fft.irfft(up, n) * q
        c = circ_filter(c, lambda f: peaks_response(f, [], floor=1.0, lo=180, hi=2600))
        far[ch] += c / (rms(c) + 1e-12) * 0.045

    wet = [street_reverb(far[0], seed=7801, wet=0.45), street_reverb(far[1], seed=7802, wet=0.45)]
    left += wet[0]
    right += wet[1]

    stereo = np.stack([left, right], axis=1)
    for ch in range(2):
        stereo[:, ch] = circ_filter(stereo[:, ch], lambda f: peaks_response(f, [], floor=1.0, lo=35, hi=6500, order=1))

    # ⚠️⚠️ A SOFT LIMITER, SO THE BED IS LOUD WITHOUT CLIPPING. The brief asks for the bed louder
    # against the horns; normalising a dense mix to its single highest peak leaves the body quiet.
    # tanh on the peak-normalised mix rounds the few loudest moments (a near horn over a pass) and
    # lets the whole bed sit higher. It is applied sample by sample, so the loop stays periodic.
    stereo /= np.max(np.abs(stereo))
    drive = 2.2
    stereo = np.tanh(drive * stereo) / np.tanh(drive)

    info['event_density'] = density(events)
    info['band_events'] = band_events_summary(stereo.mean(axis=1), events)
    return seam_rotate(stereo), info


# The first bed's events (38 s): 12 passes, 4 engine pulls, 1 jeepney rev, 3 horns.
FIRST_BED = {'seconds': 38.0, 'events': 20}


def density(events):
    per_minute = 60.0 * len(events) / BED_SECONDS
    return {
        'before': {'seconds': FIRST_BED['seconds'], 'events': FIRST_BED['events'],
                   'per_minute': round(60.0 * FIRST_BED['events'] / FIRST_BED['seconds'], 1)},
        'after': {'seconds': BED_SECONDS, 'events': len(events), 'per_minute': round(per_minute, 1),
                  'by_kind_per_minute': {k: round(60.0 * sum(1 for e in events if e[0] == k) / BED_SECONDS, 1)
                                         for k in sorted({e[0] for e in events})}},
        'not_counted': 'the continuous wash and the crowd murmur, which are textures, not events',
    }


def band_events_summary(mono, events):
    """Per kind: how far its events rise over the bed's median in their own band (dB), the
    median and the quietest. The check that an event is there to be heard at all."""
    n = len(mono)
    f = np.fft.rfftfreq(n, 1.0 / RATE)
    spec = np.fft.rfft(mono)
    w = int(0.1 * RATE)
    frames_by_band = {}
    out = {}
    for kind in sorted({e[0] for e in events}):
        values = []
        for _, at, dur, lo, hi in (e for e in events if e[0] == kind):
            if (lo, hi) not in frames_by_band:
                y = np.fft.irfft(spec * ((f >= lo) & (f < hi)), n)
                fr = (y[:n // w * w].reshape(-1, w) ** 2).mean(axis=1)
                frames_by_band[(lo, hi)] = (fr, np.median(fr))
            fr, med = frames_by_band[(lo, hi)]
            a = int(at * RATE) // w
            b = max(a + 1, int((at + dur) * RATE) // w)
            idx = np.arange(a, b) % len(fr)
            values.append(10 * np.log10(fr[idx].max() / med))
        out[kind] = {'band_hz': [events[[e[0] for e in events].index(kind)][3], events[[e[0] for e in events].index(kind)][4]],
                     'median_db_over_bed': round(float(np.median(values)), 1),
                     'quietest_db_over_bed': round(float(np.min(values)), 1)}
    return out


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

    engines = {}
    engine_rows = []
    for key, name, fn in [('car', 'kanto_engine_car.wav', author_engine_car),
                          ('diesel', 'kanto_engine_diesel.wav', author_engine_diesel),
                          ('tricycle', 'kanto_engine_tricycle.wav', author_engine_tricycle)]:
        sig, info = fn()
        engines[key] = sig
        pcm = to_pcm(sig, 0.60)
        write_wav(name, pcm)
        row = measure(name, pcm, True)
        row.update(info)
        engine_rows.append(row)

    bed, bed_info = author_city_bed(engines)
    # ⚠️ PEAKS: the bed at 0.60 (it was 0.50, and soft-limited now, so its RMS is far higher),
    # engines at 0.60, horns and sirens at 0.70. Headroom for Unity's mixer summing these with the
    # whole match, never normalised to full scale (the lagoon's convention).
    pcm = to_pcm(bed, 0.60)
    write_wav('kanto_city_bed.wav', pcm)
    row = measure('kanto_city_bed.wav', pcm, True)
    row['rms_before_rework'] = FIRST_BED_RMS
    row.update(bed_info)
    rows.append(row)
    rows.extend(engine_rows)

    for name, sig in author_horns().items():
        pcm = to_pcm(fade_ends(sig), 0.70)
        write_wav(name, pcm)
        rows.append(measure(name, pcm, False))

    for name, sig in author_sirens().items():
        pcm = to_pcm(fade_ends(sig, 0.01, 0.05), 0.70)
        write_wav(name, pcm)
        rows.append(measure(name, pcm, False))

    for row in rows:
        seam = row.get('seam')
        if seam and (seam['rms_difference_percent'] >= 10 or seam['click']):
            raise SystemExit(f"{row['file']}: seam check failed {seam}")
    for row in rows:
        if 'horn' in row['file'] and not (0.15 <= row['seconds'] <= 2.2):
            raise SystemExit(f"{row['file']}: {row['seconds']} s is outside 0.15 to 2.2 s")
        if 'siren' in row['file'] and not (5.0 <= row['seconds'] <= 10.0):
            raise SystemExit(f"{row['file']}: {row['seconds']} s is outside the brief's 5 to 10 s")

    result = {
        'provenance': 'Original deterministic synthesis (numpy, fixed seeds). No external samples, recordings or paid API.',
        'listening': 'OPEN. The owner heard the first set in play on 2026-09-27 ("too calm ... add some sirens '
                     '... louder and more variety of beeps"); this rework answers that and has not been heard. '
                     'Authored SFX stay provisional until the owner hears them in play (CLAUDE.md section 6).',
        'tool': 'tools/build_kanto_street_audio.py',
        'runtime': 'Assets/TumbangPreso/Runtime/Map/KantoStreetSound.cs',
        'seam_rule': 'Loops are built periodic; first and last 50 ms RMS within 10 per cent and a seam '
                     'step no larger than the 99.9th percentile step inside the loop.',
        'engine_authoring_rate': 'Idle to low cruise; the runtime pitches up with speed (about 0.85 to 1.6).',
        'removed': ['Assets/TumbangPreso/Art/audio/ambience/kanto_horn_jeepney.wav (now kanto_horn_jeepney_1..3)',
                    'Assets/TumbangPreso/Art/audio/ambience/kanto_horn_tricycle.wav (now kanto_horn_tricycle_1..2)'],
        'authoring_passes': AUTHORING_PASSES,
        'files': rows,
    }
    REPORT.parent.mkdir(parents=True, exist_ok=True)
    REPORT.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    for row in rows:
        extra = f" seam {row['seam']['rms_difference_percent']}% click={row['seam']['click']}" if 'seam' in row else ''
        print(f"{row['file'].split('/')[-1]}: {row['seconds']} s, {row['channels']} ch, peak {row['peak']}, rms {row['rms']}{extra}")
    print(json.dumps(bed_info['event_density']['after']))
    print(json.dumps(bed_info['band_events']))


if __name__ == '__main__':
    main()
