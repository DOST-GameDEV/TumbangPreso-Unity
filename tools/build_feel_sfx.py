"""Synthesize the throw, slipper-hit, can-hit and replay-wipe sounds (owner, 2026-10-31: "u can make sounds try mo nga").

Jobs: the throw is a rubber flick that leaves the hand, the body hit is a flat PAK with a little weight, the can is an
empty tin struck hard that then bounces, and the wipe is air crossing the screen. Original synthesis, seeded, no samples.
Run from the repository root; preserves existing Unity meta files and writes a meta for a new cue.
"""
from pathlib import Path
import re
import uuid
import wave

import numpy as np
from scipy import signal

SR = 44100
OUT = Path(__file__).resolve().parents[1] / 'Assets/TumbangPreso/Resources/Sfx'
rng = np.random.default_rng(20261031)


def t_of(seconds):
    return np.arange(int(seconds * SR)) / SR


def band(x, low, high, order=2):
    sos = signal.butter(order, [low, min(high, SR * .49)], 'bandpass', fs=SR, output='sos')
    return signal.sosfilt(sos, x)


def noise(seconds):
    return rng.standard_normal(int(seconds * SR))


def thump(seconds, start_hz, end_hz, decay):
    t = t_of(seconds)
    hz = end_hz + (start_hz - end_hz) * np.exp(-t * 45)
    return np.sin(2 * np.pi * np.cumsum(hz) / SR) * np.exp(-t * decay)


def sweep_air(seconds, low_hz, peak_hz, end_hz, peak_at, bandwidth=.9):
    """Noise through a moving band, block by block: air moving past, not a static hiss."""
    n = int(seconds * SR)
    source = rng.standard_normal(n)
    out = np.zeros(n)
    hop, size = 256, 512
    window = np.hanning(size)
    for start in range(0, n - size, hop):
        u = start / n
        hz = low_hz * (peak_hz / low_hz) ** (u / peak_at) if u < peak_at \
            else peak_hz * (end_hz / peak_hz) ** ((u - peak_at) / (1 - peak_at))
        sos = signal.butter(2, [hz / (1 + bandwidth), hz * (1 + bandwidth)], 'bandpass', fs=SR, output='sos')
        out[start:start + size] += signal.sosfilt(sos, source[start:start + size] * window)
    return out


def tin_strike(seconds, gain=1.0, shift=1.0):
    """Thin-walled tin: dense, inharmonic and fast-dying, so it reads as a can and never rings like a bell."""
    t = t_of(seconds)
    out = np.zeros(len(t))
    for hz, decay, level in ((610, 34, .6), (1040, 40, .75), (1490, 52, .55), (2310, 66, .5), (3170, 78, .36), (4630, 110, .26)):
        out += level * np.sin(2 * np.pi * hz * shift * t + rng.uniform(0, 6.28)) * np.exp(-t * decay)
    crack = band(noise(seconds), 1800, 9000) * np.exp(-t * 170) * 1.5
    return np.tanh((out * .55 + crack) * 1.6) * gain


def place(mix, part, at):
    start = int(at * SR)
    end = min(len(mix), start + len(part))
    mix[start:end] += part[:end - start]


def finish(x, peak):
    x = x - np.mean(x)
    fade = min(len(x), int(.012 * SR))
    x[-fade:] *= np.linspace(1, 0, fade)
    return x / max(1e-6, np.max(np.abs(x))) * peak


def slipper_throw():
    seconds = .32
    t = t_of(seconds)
    air = sweep_air(seconds, 420, 2900, 850, .34) * np.sin(np.pi * np.minimum(1, t / seconds)) ** 1.4
    flick = band(noise(seconds), 900, 3800) * np.exp(-t * 120)
    return finish(air * .85 + flick * .55, .72)


def slipper_hit():
    seconds = .24
    t = t_of(seconds)
    slap = band(noise(seconds), 1100, 6400) * np.exp(-t * 95)
    rubber = band(noise(seconds), 260, 950) * np.exp(-t * 48)
    weight = thump(seconds, 170, 78, 24)
    return finish(np.tanh(slap * 1.5 + rubber * .9 + weight * .85), .86)


def lata_knockdown():
    mix = np.zeros(int(.78 * SR))
    place(mix, tin_strike(.28), 0)
    # It falls and bounces: each landing quicker, quieter and brighter than the one before.
    for at, gain, shift in ((.15, .42, 1.06), (.29, .3, 1.12), (.39, .22, 1.17), (.46, .16, 1.21), (.51, .11, 1.25)):
        place(mix, tin_strike(.13, gain, shift), at)
    return finish(mix, .9)


def lata_impact():
    seconds = .3
    t = t_of(seconds)
    return finish(np.tanh(thump(seconds, 150, 58, 15) * 1.25 + band(noise(seconds), 300, 1500) * np.exp(-t * 70) * .5), .85)


def replay_wipe():
    seconds = .62
    t = t_of(seconds)
    air = sweep_air(seconds, 260, 3400, 420, .42, 1.1) * np.sin(np.pi * t / seconds) ** 1.6
    under = thump(seconds, 92, 48, 5) * np.minimum(1, t / .12) * .35
    return finish(air + under, .62)


def write(stem, x):
    path = OUT / (stem + '.wav')
    with wave.open(str(path), 'wb') as out:
        out.setnchannels(1)
        out.setsampwidth(2)
        out.setframerate(SR)
        out.writeframes((np.clip(x, -1, 1) * 32767).astype('<i2').tobytes())
    meta = path.with_suffix('.wav.meta')
    if not meta.exists():
        reference = (OUT / 'bump.wav.meta').read_text(encoding='utf-8')
        meta.write_text(re.sub(r'guid: \w+', 'guid: ' + uuid.uuid5(uuid.NAMESPACE_URL, 'tump/sfx/' + stem).hex, reference, 1),
                        encoding='utf-8', newline='\n')
    print(f'{stem}: {len(x) / SR:.3f}s peak={np.max(np.abs(x)):.3f} rms={np.sqrt(np.mean(x * x)):.3f}')


if __name__ == '__main__':
    write('slipper_throw', slipper_throw())
    write('slipper_hit', slipper_hit())
    write('lata_knockdown', lata_knockdown())
    write('lata_impact', lata_impact())
    write('replay_wipe', replay_wipe())
