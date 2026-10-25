"""Build Zack's enabled electric family. Owner direction: clearly audible buzz and taser-like arcing on every skill."""
import argparse, hashlib, json, wave
from pathlib import Path
import numpy as np
from scipy import signal
from sfx_electric import times, band_noise, level, electric, arcs, attack_tail, finish, RATE, WORK
ROOT = Path(__file__).resolve().parents[1]

def make(name, seconds, kind):
    rng = np.random.default_rng(int.from_bytes(hashlib.sha256(name.encode()).digest()[:8], 'little'))
    t = times(seconds)
    e = electric(t, rng, pulse={'pull': 29, 'split': 37, 'zap': 19, 'status': 21, 'speed': 43, 'loop': 23, 'power': 31}.get(kind, 27), carrier=255 if kind in ['speed', 'power'] else 225)
    if name == 'sfx_zack_livewire_loop':
        e = electric(t, rng, pulse=60, carrier=175)
    elif name == 'sfx_zack_afterimage_loop':
        e = electric(t, rng, pulse=37, carrier=285)
    n = band_noise(t, rng)
    ticks = arcs(t, rng, max(5, int(seconds * 90)))
    if kind == 'pull':
        u = np.clip(t / 0.42, 0, 1)
        env = (0.45 + 0.55 * u) * np.clip((seconds - t) / 0.06, 0, 1)
        x = e * env + level(n, -24) * np.sin(np.pi * u) ** 2 + 0.12 * ticks
    elif kind == 'catch':
        x = e * attack_tail(t, decay=0.1) + 0.48 * n * np.exp(-t / 0.012) + 0.16 * ticks * np.exp(-t / 0.16)
    elif kind == 'split':
        x = e * attack_tail(t, decay=0.21) + 0.32 * n * (np.exp(-t / 0.012) + 0.65 * np.exp(-np.maximum(0, t - 0.045) / 0.016) * (t >= 0.045)) + 0.13 * ticks
    elif kind == 'pop':
        x = e * attack_tail(t, decay=0.11) + 0.5 * n * np.exp(-t / 0.014) + 0.12 * ticks * np.exp(-t / 0.12)
    elif kind == 'fizzle':
        x = (e * 0.8 + n * 0.18) * np.maximum(0, 1 - t / seconds) ** 1.4 + 0.11 * ticks * np.maximum(0, 1 - t / seconds)
    elif kind == 'string':
        phase = 2 * np.pi * 345 * t
        pluck = (np.sin(phase) + 0.3 * np.sin(phase * 2.02)) * np.exp(-np.maximum(0, t - 0.09) / 0.14) * (t >= 0.09)
        x = 0.8 * e * attack_tail(t, decay=0.2) + 0.16 * pluck + 0.4 * n * np.exp(-t / 0.009) + 0.13 * ticks
    elif kind == 'taut':
        x = e * attack_tail(t, attack=0.006, decay=0.38, held=0.35) + 0.2 * n * np.exp(-t / 0.01) + 0.1 * ticks
    elif kind == 'zap':
        x = 1.2 * e * attack_tail(t, decay=0.24, held=0.16) + 0.5 * n * np.exp(-t / 0.01) + 0.18 * ticks
    elif kind == 'snap':
        x = 0.8 * e * attack_tail(t, decay=0.12) + 0.4 * n * np.exp(-t / 0.007) + 0.16 * ticks * np.exp(-t / 0.2)
    elif kind == 'status':
        x = e * attack_tail(t, held=0.82) * np.minimum(1, (seconds - t) / 0.055) + 0.35 * n * np.exp(-t / 0.012) + 0.13 * ticks
    elif kind == 'speed':
        x = e * (0.45 + 0.55 * np.sin(np.pi * np.minimum(t / seconds, 1))) * attack_tail(t, attack=0.015, decay=0.4) + 0.12 * ticks
    elif kind in ['loop', 'power']:
        x = e * (0.85 + 0.15 * np.sin(2 * np.pi * 3 * t)) + 0.12 * ticks
    elif kind == 'end':
        x = e * np.maximum(0, 1 - t / seconds) ** 1.6 + 0.15 * ticks * np.maximum(0, 1 - t / seconds)
    else:
        raise ValueError(kind)
    return finish(x, loop=kind in ['loop', 'power'])

def theme():
    """Follow the bolt freeze at 1.70 s, finger poke at 2.20 s and fist impact at 3.43 s."""
    t = times(4.5)
    rng = np.random.default_rng(18325)
    e = electric(t, rng, 23, 210)
    x = np.zeros(len(t))
    x += e * 0.25 * np.clip(t / 0.7, 0, 1) * (t < 1.3)
    for at, gain in [(0.37, 0.12), (0.7, 0.3), (1.7, 0.45)]:
        x += band_noise(t, rng) * np.exp(-np.maximum(0, t - at) / 0.02) * (t >= at) * gain
    x += e * np.clip((t - 1.3) / 0.4, 0, 1) * 0.9 * ((t >= 1.3) & (t < 1.7))
    x += e * 0.13 * ((t >= 1.7) & (t < 2.7))
    x += e * 0.8 * np.exp(-np.maximum(0, t - 2.2) / 0.11) * (t >= 2.2) * (t < 2.55)
    ramp = np.clip((t - 2.7) / 0.73, 0, 1)
    x += e * (0.2 + 0.7 * ramp) * ((t >= 2.7) & (t < 3.43))
    hit = t - 3.43
    x += (e * 1.35 + band_noise(t, rng) * 0.6 * np.exp(-np.maximum(hit, 0) / 0.025)) * (hit >= 0) * (0.35 + 0.65 * np.exp(-np.maximum(hit, 0) / 0.35))
    return finish(x)
RECIPES = {'sfx_cast_zack_backtrack': (0.46, 'pull'),
 'sfx_zack_backtrack_land': (0.32, 'catch'),
 'sfx_cast_zack_afterimage': (0.48, 'split'),
 'sfx_zack_afterimage_pop': (0.4, 'pop'),
 'sfx_zack_afterimage_fizzle': (0.55, 'fizzle'),
 'sfx_zack_afterimage_loop': (2, 'loop'),
 'sfx_cast_zack_livewire': (0.5, 'string'),
 'sfx_zack_livewire_taut': (0.7, 'taut'),
 'sfx_zack_livewire_zap': (0.55, 'zap'),
 'sfx_zack_livewire_snap': (0.45, 'snap'),
 'sfx_zack_livewire_loop': (2, 'loop'),
 'sfx_zack_momentum_full': (0.6, 'speed'),
 'sfx_status_zapped': (1, 'status'),
 'sfx_zack_overclock_loop': (2, 'power'),
 'sfx_zack_overclock_end': (0.6, 'end')}
if __name__ == '__main__':
    p = argparse.ArgumentParser()
    p.add_argument('--out', default=str(ROOT / 'Logs/zack-lightning-audio'))
    a = p.parse_args()
    out = Path(a.out)
    out.mkdir(parents=True, exist_ok=True)
    manifest = []
    clips = {name: make(name, *recipe) for name, recipe in RECIPES.items()}
    clips['sfx_ult_theme_zack'] = theme()
    for name, x in clips.items():
        path = out / (name + '.wav')
        with wave.open(str(path), 'wb') as w:
            w.setnchannels(1)
            w.setsampwidth(2)
            w.setframerate(RATE)
            w.writeframes((np.clip(x, -1, 1) * 32767).astype('<i2').tobytes())
        manifest.append({'cue': name, 'seconds': len(x) / RATE, 'peak_dbfs': float(20 * np.log10(np.max(np.abs(x)) + 1e-12)), 'rms_dbfs': float(20 * np.log10(np.sqrt(np.mean(x * x)) + 1e-12)), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
    (out / 'manifest.json').write_text(json.dumps({'engine': 'Original oversampled synthesis, SciPy filtering and Pedalboard saturation/compression', 'external_generation_spend': 0, 'cues': manifest}, indent=2))
    print(json.dumps(manifest, indent=2))
