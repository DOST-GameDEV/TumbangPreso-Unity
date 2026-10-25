"""Reusable deterministic electrical synthesis primitives. No samples, voices or external service calls."""
import numpy as np
from scipy import signal
from pedalboard import Pedalboard, HighpassFilter, LowpassFilter, Distortion, Compressor
RATE = 48000
WORK = 96000

def times(seconds):
    return np.arange(round(seconds * WORK)) / WORK

def band_noise(t, rng, lo=180, hi=6200):
    x = rng.normal(size=len(t))
    sos = signal.butter(3, [lo, hi], btype='bandpass', fs=WORK, output='sos')
    return signal.sosfilt(sos, x)

def level(x, db):
    return x / (np.sqrt(np.mean(x * x)) + 1e-12) * 10 ** (db / 20)

def electric(t, rng, pulse=21, carrier=235):
    """Midrange voltage with pulse gaps, a rough carrier and irregular arcing."""
    wobble = 0.7 * np.sin(2 * np.pi * 63 * t) + 0.3 * np.sin(2 * np.pi * 137 * t)
    phase = 2 * np.pi * np.cumsum(carrier * (1 + 0.045 * wobble)) / WORK
    rasp = signal.sawtooth(phase + 0.75 * np.sin(2 * np.pi * 91 * t), width=0.36)
    rasp += 0.45 * np.sin(phase * 3.03 + 1.2 * np.sin(2 * np.pi * 75 * t))
    gate = 0.22 + 0.78 * (0.5 + 0.5 * signal.square(2 * np.pi * pulse * t + 0.25 * np.sin(2 * np.pi * 6 * t), duty=0.58))
    grain = band_noise(t, rng, 450, 5800) * (0.45 + 0.55 * np.maximum(0, np.sin(2 * np.pi * 83 * t)))
    return level(rasp * gate * 0.65 + grain * 0.55 + 0.1 * np.sin(2 * np.pi * 60 * t), -12)

def arcs(t, rng, count):
    x = np.zeros(len(t))
    for at in rng.uniform(0, max(0.001, t[-1] - 0.015), count):
        i = int(at * WORK)
        n = min(int(rng.uniform(0.002, 0.012) * WORK), len(x) - i)
        click = rng.normal(size=n) * np.exp(-np.arange(n) / (n * 0.2))
        x[i:i + n] += click * rng.uniform(0.25, 0.65)
    return x

def attack_tail(t, attack=0.003, decay=0.16, held=0):
    return np.minimum(1, t / max(attack, 1e-06)) * (held + (1 - held) * np.exp(-t / decay))

def finish(x, loop=False):
    """Band-limit nonlinear color and overlap the loop seam before setting body level and peak headroom."""
    x = signal.resample_poly(x, 1, 2).astype(np.float32)
    board = Pedalboard([HighpassFilter(cutoff_frequency_hz=85), Distortion(drive_db=7), LowpassFilter(cutoff_frequency_hz=6800), Compressor(threshold_db=-17, ratio=2.3, attack_ms=2, release_ms=55)])
    x = np.asarray(board(x, RATE)).reshape(-1).astype(float)
    x -= x.mean()
    if loop:
        n = 480
        fade = np.linspace(0, 1, n)
        join = x[-n:] * (1 - fade) + x[:n] * fade
        x = np.concatenate((join, x[n:-n]))
    else:
        n = min(120, len(x) // 4)
        x[:n] *= np.linspace(0, 1, n)
        x[-n:] *= np.linspace(1, 0, n)
    target = 10 ** (-14 / 20)
    x *= min(target / (np.sqrt(np.mean(x * x)) + 1e-12), 10 ** (-3.5 / 20) / (np.max(np.abs(x)) + 1e-12))
    return x.astype(np.float32)
