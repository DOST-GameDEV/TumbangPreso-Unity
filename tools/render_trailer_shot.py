"""Mix a recorded trailer shot's cue stem and encode its reference MP4s.

  python tools/render_trailer_shot.py Logs/trailer/01_dusk-in-tondo

`TrailerShots.Shot<NN><Slug>` (Assets/TumbangPreso/Tests/PlayMode/TrailerShots.cs) writes the folder:
<pass>/frame_00000.png onward (1920x1080, 60 fps of shot time, 0.5 s handles either side), shot.json (fps, frames,
handle, passes) and cues.csv (shot_time, film_time, clip, gain, pitch, kind). This writes, beside them:

  * cues.wav: the stem. Every `cue` row is the game's own clip from Assets/TumbangPreso/Resources/Sfx (or anywhere
    under Assets/ by name), pitched and gained as logged (a pitch plays it faster and higher, as an AudioSource
    does), placed at its shot time; every `bed` row is an ambience bed looped under the whole shot from the first
    frame. Sample 0 is the first frame, the in-point handle included. No music and nothing synthesised here.
  * <pass>.mp4 for each pass: H.264, CRF 14, yuv420p, 60 fps, with the stem as AAC (a reference for the edit;
    the editor takes the PNGs and the WAV).

Needs ffmpeg on PATH and numpy. Deterministic: a bed starts at its own first sample.
"""
import argparse
import csv
import json
import shutil
import subprocess
import tempfile
import wave
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
SFX = ROOT / "Assets" / "TumbangPreso" / "Resources" / "Sfx"
RATE = 48000


def ffmpeg():
    exe = shutil.which("ffmpeg")
    if not exe:
        raise SystemExit("ffmpeg is not on PATH")
    return exe


def find_clip(name):
    for ext in (".wav", ".ogg", ".mp3", ".aif", ".aiff", ".flac"):
        if (SFX / (name + ext)).exists():
            return SFX / (name + ext)
    for ext in (".wav", ".ogg", ".mp3", ".aif", ".aiff", ".flac"):
        hits = sorted((ROOT / "Assets").rglob(name + ext))
        if hits:
            return hits[0]
    return None


def load(path, tmp):
    """Any audio file to mono float at the stem's rate, through ffmpeg."""
    out = Path(tmp) / (path.stem + ".wav")
    subprocess.run([ffmpeg(), "-y", "-loglevel", "error", "-i", str(path), "-ac", "1", "-ar", str(RATE), "-sample_fmt", "s16", str(out)], check=True)
    with wave.open(str(out)) as w:
        return np.frombuffer(w.readframes(w.getnframes()), dtype="<i2").astype(np.float64) / 32768.0


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("folder")
    args = parser.parse_args()
    folder = Path(args.folder).resolve()
    shot = json.loads((folder / "shot.json").read_text(encoding="utf-8"))
    fps, frames, handle = float(shot["fps"]), int(shot["frames"]), float(shot["handle"])
    seconds = frames / fps
    start = -handle   # shot time of the first frame

    with tempfile.TemporaryDirectory() as tmp:
        track = np.zeros(int(round(seconds * RATE)))
        cache, missing, placed = {}, set(), 0
        rows = list(csv.DictReader(open(folder / "cues.csv", newline="", encoding="utf-8"))) if (folder / "cues.csv").exists() else []
        for row in rows:
            name = row["clip"].strip()
            if name not in cache:
                path = find_clip(name)
                cache[name] = load(path, tmp) if path else None
            clip = cache[name]
            if clip is None:
                missing.add(name)
                continue
            pitch, gain = float(row["pitch"]), float(row["gain"])
            if abs(pitch - 1.0) > 1e-3:
                clip = np.interp(np.arange(0, len(clip) - 1, pitch), np.arange(len(clip)), clip)
            if row["kind"] == "bed":
                track += np.tile(clip, int(np.ceil(len(track) / len(clip))))[: len(track)] * gain
            else:
                i = int(round((float(row["shot_time"]) - start) * RATE))
                if i < 0:
                    clip, i = clip[-i:], 0
                j = min(len(track), i + len(clip))
                if j > i:
                    track[i:j] += clip[: j - i] * gain
            placed += 1
        peak = float(np.max(np.abs(track))) if len(track) else 0.0
        if peak > 0.98:
            track *= 0.98 / peak
        stem = folder / "cues.wav"
        with wave.open(str(stem), "wb") as w:
            w.setnchannels(1); w.setsampwidth(2); w.setframerate(RATE)
            w.writeframes((np.clip(track, -1, 1) * 32767).astype("<i2").tobytes())
        print(f"stem {stem}: {seconds:.3f} s, {placed} rows placed, peak {20 * np.log10(max(peak, 1e-9)):.1f} dBFS"
              + (f", scaled to -0.2 dBFS" if peak > 0.98 else "") + (f"; MISSING {sorted(missing)}" if missing else ""))

    for name in shot["passes"]:
        pngs = folder / name
        count = len(list(pngs.glob("frame_*.png")))
        if count != frames:
            raise SystemExit(f"{name}: {count} frames on disk, {frames} expected")
        out = folder / f"{name}.mp4"
        subprocess.run([ffmpeg(), "-y", "-loglevel", "error", "-framerate", f"{fps:g}", "-i", str(pngs / "frame_%05d.png"), "-i", str(folder / "cues.wav"),
                        "-c:v", "libx264", "-crf", "14", "-preset", "slow", "-pix_fmt", "yuv420p", "-r", f"{fps:g}",
                        "-c:a", "aac", "-b:a", "256k", "-shortest", "-movflags", "+faststart", str(out)], check=True)
        print(f"wrote {out}  ({count} frames, {count / fps:.3f} s)")


if __name__ == "__main__":
    main()
