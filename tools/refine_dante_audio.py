"""Fit Dante's existing recorded stone cues to pressure, contact and recovery.

Inputs are pinned before this pass, so reruns never process their own output.
Original recording attribution remains in docs/Asset_Sourcing.md. By default
write review WAVs to Logs; --write-assets explicitly updates only these cues.
Signal checks do not establish listening approval.
"""
import argparse
import io
import json
from pathlib import Path
import subprocess
import wave

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
BASE = "b106c6ac"
REL = "Assets/TumbangPreso/Resources/Sfx"
RATE = 44100


def load(name):
    data = subprocess.check_output(["git", "show", f"{BASE}:{REL}/{name}.wav"], cwd=ROOT)
    with wave.open(io.BytesIO(data)) as reader:
        assert (reader.getnchannels(), reader.getsampwidth(), reader.getframerate()) == (1, 2, RATE)
        return np.frombuffer(reader.readframes(reader.getnframes()), dtype="<i2").astype(float) / 32768


def band(samples, low, high):
    frequency = np.fft.rfftfreq(len(samples), 1 / RATE)
    gain = 1 / (1 + (low / np.maximum(frequency, .01)) ** 4) / (1 + (frequency / high) ** 6)
    return np.fft.irfft(np.fft.rfft(samples) * gain, n=len(samples))


def envelope(samples, attack, release):
    samples = samples.copy()
    start, end = min(len(samples), round(attack * RATE)), min(len(samples), round(release * RATE))
    if start:
        samples[:start] *= np.sin(np.linspace(0, np.pi / 2, start)) ** 2
    if end:
        samples[-end:] *= np.cos(np.linspace(0, np.pi / 2, end)) ** 2
    return samples


def stretch(samples, seconds):
    return np.interp(np.linspace(0, len(samples) - 1, round(seconds * RATE)), np.arange(len(samples)), samples)


def strongest(samples, seconds):
    length = min(len(samples), round(seconds * RATE))
    energy = np.convolve(samples[::441] ** 2, np.ones(max(1, length // 441)), mode="valid")
    start = min(int(np.argmax(energy)) * 441, len(samples) - length)
    return samples[start:start + length].copy()


def rms(samples):
    return float(np.sqrt(np.mean(samples * samples)))


def stats(samples):
    return {"seconds": round(len(samples) / RATE, 4), "peak": round(float(max(abs(samples))), 5),
            "rms": round(rms(samples), 5), "peak_seconds": round(float(np.argmax(abs(samples))) / RATE, 4)}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--write-assets", action="store_true")
    args = parser.parse_args()
    names = ["sfx_cast_dante_stomp", "sfx_cast_dante_fissure", "sfx_cast_dante_carapace",
             "sfx_var_dante_tremor", "sfx_var_dante_plating", "sfx_quake_slam", "sfx_ult_theme_dante"]
    source = {name: load(name) for name in names}
    recipes = {}
    # Quiet reverse grit leads into the real contact, rather than hitting twice.
    for name, length, low, high in [(names[0], .30, 160, 2100), (names[1], .40, 100, 2700),
                                   (names[3], .30, 85, 1250)]:
        pressure = stretch(strongest(source[name], .45)[::-1], length)
        recipes[name] = envelope(band(pressure, low, high), length * .78, .025)
    # Two assembly gestures: the light ward closes quickly; plating has a slower lock.
    recipes[names[2]] = envelope(band(strongest(source[names[2]], .52), 100, 3300), .08, .22)
    recipes[names[4]] = envelope(band(strongest(source[names[4]], .64), 70, 2700), .13, .26)
    # Preserve the onset of the existing mining impact and its lower stone body.
    recipes[names[5]] = envelope(band(source[names[5]][:round(.72 * RATE)], 42, 4100), .004, .34)
    recipes[names[6]] = envelope(band(source[names[6]][:round(1.8 * RATE)], 75, 2400), .22, .60)
    destination = ROOT / (REL if args.write_assets else "Logs/dante-audio-review-v1")
    destination.mkdir(parents=True, exist_ok=True)
    report = []
    for name, samples in recipes.items():
        old = source[name]
        samples -= samples.mean()
        samples = envelope(samples, .002, .005)
        scale = .60 if name.startswith("sfx_cast") or "tremor" in name else .74 if "theme" in name else .90
        gain = min(max(abs(old)) * scale / max(max(abs(samples)), 1e-9),
                   rms(old) * scale / max(rms(samples), 1e-9))
        samples *= gain
        assert np.isfinite(samples).all() and max(abs(samples)) < .90
        assert abs(samples[0]) < 1e-6 and abs(samples[-1]) < 1e-6
        with wave.open(str(destination / (name + ".wav")), "wb") as writer:
            writer.setnchannels(1)
            writer.setsampwidth(2)
            writer.setframerate(RATE)
            writer.writeframes(np.round(samples * 32767).astype("<i2").tobytes())
        report.append({"cue": name, "before": stats(old), "after": stats(samples)})
    target = ROOT / "Logs/dante-audio-timing-v1.json"
    target.write_text(json.dumps({"source_commit": BASE, "auditory_approval": False,
                                "assets_written": args.write_assets, "cues": report}, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
