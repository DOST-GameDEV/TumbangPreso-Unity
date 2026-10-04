"""The Ilalim train's rail clank: wheels over the rail joints, a seamless 2.0 s loop.

  py -3 tools/synth_ilalim_train_clack.py

Owner, 2026-10-04: "theres no sfx for the rails clanking when the train goes over". The pass
recording (sfx_lrt_pass) is one long roar with no joint rhythm, so this is a second layer on
the consist's own moving source (LrtTrainFlyby): at 18 m/s over 12 m rails a joint goes by every
0.667 s, and each bogie's two axles strike it 0.09 s apart, "ta-tak ... ta-tak". Three periods
make the loop. Written to Resources/Ambience/lrt_clack.wav (mono, 44.1 kHz, 16 bit)."""
import wave
from pathlib import Path

import numpy as np
from scipy import signal

SR = 44100
PERIOD = 0.6667
LOOP = 3 * PERIOD
rng = np.random.default_rng(1904)
n = int(SR * LOOP)
out = np.zeros(n)


def strike(gain, tone):
    """One wheel on one joint: a steel tick, two ringing rail modes and a low thump from the deck."""
    m = int(SR * 0.32)
    t = np.arange(m) / SR
    tick = rng.standard_normal(m) * np.exp(-t * 240.0)
    b, a = signal.butter(2, [1400 / (SR / 2), 5200 / (SR / 2)], "band")
    tick = signal.lfilter(b, a, tick) * 1.6
    ring = (np.sin(2 * np.pi * 860 * tone * t) * np.exp(-t * 38.0) * 0.50
            + np.sin(2 * np.pi * 2140 * tone * t) * np.exp(-t * 62.0) * 0.28
            + np.sin(2 * np.pi * 3350 * tone * t) * np.exp(-t * 95.0) * 0.14)
    thump = np.sin(2 * np.pi * 74 * t + 0.6) * np.exp(-t * 22.0) * 0.85
    return (tick + ring + thump) * gain


for k in range(3):
    base = k * PERIOD
    # the leading bogie, then the trailing one a little softer
    for dt, gain in ((0.0, 1.0), (0.09, 0.86), (0.30, 0.62), (0.39, 0.54)):
        s = strike(gain * rng.uniform(0.92, 1.05), rng.uniform(0.97, 1.03))
        i = int((base + dt + rng.uniform(-0.004, 0.004)) * SR) % n
        idx = (i + np.arange(len(s))) % n          # wraps, so the loop is seamless
        np.add.at(out, idx, s)
# a faint steel hiss under the strikes, so the gaps are not dead silence
hiss = rng.standard_normal(n)
b, a = signal.butter(2, [2500 / (SR / 2), 7000 / (SR / 2)], "band")
hiss = signal.lfilter(b, a, np.concatenate([hiss, hiss]))[n:] * 0.035
out += hiss
out = out / np.abs(out).max() * 0.89
path = Path(__file__).resolve().parents[1] / "Assets/TumbangPreso/Resources/Ambience/lrt_clack.wav"
path.parent.mkdir(parents=True, exist_ok=True)
with wave.open(str(path), "wb") as w:
    w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
    w.writeframes((out * 32767).astype(np.int16).tobytes())
print(path, round(LOOP, 3), "s rms", round(float(np.sqrt((out ** 2).mean())), 3))
