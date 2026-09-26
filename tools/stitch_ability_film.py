"""Stitch an in-match ability film into one mp4 with the game's own sound.

    python tools/stitch_ability_film.py Logs/paete-evidence-r12/paete-ult-film Logs/paete-share/paete_ultimate_v4.mp4 \
        --title "PAETE  MAKILING'S EMBRACE  v4" --view owner "HIS SCREEN" --view wide "THE COURT" --view victim "A CAUGHT PLAYER"

WHY THIS EXISTS: the owner judges an ability from a video of it IN A MATCH, never from stills
(HERO-9, 2026-09-26: Paete is *"THE BASELINE QUALITY OF EVERYTHING ELSE MOVING FORWARD"*), and every
hero after him is held to the same bar. A film probe (`PaeteKitPlayProbe.FilmTheUltimateOnHisScreen` is
the template) writes one numbered JPG sequence per view at a fixed 30 fps game clock, plus `cues.csv`: every
world cue the match played, with its film time, pitch and gain. This turns that folder into what he watches:

  * the SOUND is the game's own: each cue's wav from `Assets/TumbangPreso/Resources/Sfx`, pitched and gained
    as logged, mixed at its film time into one track (no music or effects added from outside the game);
  * each VIEW becomes one labelled clip over that same track, and the clips play one after another.

Needs ffmpeg on PATH. numpy only. Deterministic.
"""
import argparse
import csv
import os
import shutil
import subprocess
import sys
import tempfile
import wave
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
SFX = ROOT / "Assets" / "TumbangPreso" / "Resources" / "Sfx"
RATE = 44100
FPS = 30


def read_wav(path):
    with wave.open(str(path), "rb") as w:
        channels, width, rate, frames = w.getnchannels(), w.getsampwidth(), w.getframerate(), w.getnframes()
        data = w.readframes(frames)
    if width == 2:
        x = np.frombuffer(data, dtype="<i2").astype(np.float32) / 32768.0
    elif width == 4:
        x = np.frombuffer(data, dtype="<i4").astype(np.float32) / 2147483648.0
    else:
        x = (np.frombuffer(data, dtype=np.uint8).astype(np.float32) - 128.0) / 128.0
    if channels > 1:
        x = x.reshape(-1, channels).mean(axis=1)
    if rate != RATE:
        x = np.interp(np.arange(0, len(x) * RATE / rate) * rate / RATE, np.arange(len(x)), x).astype(np.float32)
    return x


def mix(cues_csv, seconds, theme_gain):
    """The film's cue log mixed into one mono track. A cue's pitch plays it faster and higher, as the game does."""
    track = np.zeros(int(seconds * RATE) + RATE, dtype=np.float32)
    missing = set()
    with open(cues_csv, newline="", encoding="utf-8") as fh:
        for row in csv.DictReader(fh):
            stem = row["cue"].strip()
            path = SFX / (stem + ".wav")
            if not path.exists():
                missing.add(stem)
                continue
            x = read_wav(path)
            pitch = float(row.get("pitch") or 1.0)
            if abs(pitch - 1.0) > 1e-3:
                x = np.interp(np.arange(0, len(x), pitch), np.arange(len(x)), x).astype(np.float32)
            gain = float(row.get("gain") or 1.0)
            # The theme is logged at a nominal gain by the film (it plays from the introduction's own source at
            # 0.6 of the effects volume); give it its in-game weight against the world cues.
            if stem.startswith("sfx_ult_theme_"):
                gain = theme_gain
            at = int(float(row["seconds"]) * RATE)
            end = min(len(track), at + len(x))
            if end > at:
                track[at:end] += x[:end - at] * gain
    if missing:
        print("no wav for: " + ", ".join(sorted(missing)), file=sys.stderr)
    peak = float(np.max(np.abs(track))) or 1.0
    # Normalised to -1 dBFS: the relative mix is the game's, the level is the video's.
    return (track / peak * 0.89)[: int(seconds * RATE)]


def write_wav(path, x):
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(RATE)
        w.writeframes((np.clip(x, -1, 1) * 32767).astype("<i2").tobytes())


def label_filter(text, sub):
    font = "C\\:/Windows/Fonts/arialbd.ttf"
    esc = lambda s: s.replace("\\", "\\\\").replace(":", "\\:").replace("'", "’")
    return (f"drawtext=fontfile='{font}':text='{esc(text)}':x=28:y=24:fontsize=34:fontcolor=white:borderw=3:bordercolor=black@0.85,"
            f"drawtext=fontfile='{font}':text='{esc(sub)}':x=28:y=66:fontsize=24:fontcolor=white@0.9:borderw=2:bordercolor=black@0.85")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("film")
    ap.add_argument("out")
    ap.add_argument("--title", default="")
    ap.add_argument("--view", nargs=2, action="append", metavar=("FOLDER", "LABEL"), required=True)
    ap.add_argument("--theme-gain", type=float, default=0.55)
    args = ap.parse_args()
    film = Path(args.film)
    ffmpeg = shutil.which("ffmpeg")
    if ffmpeg is None:
        sys.exit("ffmpeg is not on PATH")
    frames = min(len(list((film / v).glob("*.jpg"))) for v, _ in args.view)
    seconds = frames / FPS
    work = Path(tempfile.mkdtemp(prefix="stitch-"))
    try:
        audio = work / "mix.wav"
        write_wav(audio, mix(film / "cues.csv", seconds, args.theme_gain))
        parts = []
        for i, (folder, label) in enumerate(args.view):
            part = work / f"part{i}.mp4"
            vf = label_filter(args.title or film.name, label) + ",format=yuv420p"
            subprocess.run([ffmpeg, "-y", "-loglevel", "error", "-framerate", str(FPS), "-i", str(film / folder / "%05d.jpg"),
                            "-i", str(audio), "-frames:v", str(frames), "-vf", vf, "-c:v", "libx264", "-crf", "18", "-preset", "medium",
                            "-c:a", "aac", "-b:a", "192k", "-shortest", str(part)], check=True)
            parts.append(part)
        listing = work / "list.txt"
        listing.write_text("".join(f"file '{p.as_posix()}'\n" for p in parts), encoding="utf-8")
        Path(args.out).parent.mkdir(parents=True, exist_ok=True)
        subprocess.run([ffmpeg, "-y", "-loglevel", "error", "-f", "concat", "-safe", "0", "-i", str(listing), "-c", "copy", args.out], check=True)
        print(f"{args.out}: {len(parts)} views x {seconds:.1f} s")
    finally:
        shutil.rmtree(work, ignore_errors=True)


if __name__ == "__main__":
    main()
