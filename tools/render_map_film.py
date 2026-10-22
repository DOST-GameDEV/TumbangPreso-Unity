"""Encode a recorded map intro film to video, with its score and every cue mixed at the film's own times.

  py -3 tools/render_map_film.py Eskinita
  py -3 tools/render_map_film.py IlalimNgTulay --out Logs/map-films/ermita.mp4

`MapFilmTests.<Map>Recording` writes Logs/map-films/<map>/video/: frame_0000.png onward at 24 a second, film.txt
(map, length, fps) and cues.txt (age, clip, volume, pitch: every `MapFilm.Cue` the film called). This mixes the
map's score (`MapFilm.Score`, from 0) and each cue (found by name under Assets/, any format ffmpeg reads; a pitch
plays it faster and higher, as an AudioSource does) into one track and encodes H.264 + AAC beside the frames.
The levels are the film's own volumes at full SFX volume; nothing here is a mix decision for the game."""
import argparse
import shutil
import subprocess
import tempfile
import wave
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
SR = 44100
SCORES = {
    "Eskinita": "sfx_film_tondo_score",
    "IlalimNgTulay": "sfx_film_ermita_score",
    "LagoonCove": "sfx_film_sitangkai_score",
    "Kanto": "sfx_film_bgc_score",
    "Payo": "sfx_film_banaue_score",
}
SCORE_LEVEL = 0.85


def ffmpeg():
    exe = shutil.which("ffmpeg")
    if exe:
        return exe
    import imageio_ffmpeg
    return imageio_ffmpeg.get_ffmpeg_exe()


def find_clip(name):
    for ext in (".wav", ".ogg", ".mp3", ".aif", ".aiff", ".flac"):
        hits = sorted((ROOT / "Assets").rglob(name + ext))
        if hits:
            return hits[0]
    return None


def load(path, tmp):
    """Any audio file to mono float at 44.1 kHz, through ffmpeg."""
    out = Path(tmp) / (path.stem + ".wav")
    subprocess.run([ffmpeg(), "-y", "-loglevel", "error", "-i", str(path), "-ac", "1", "-ar", str(SR), "-sample_fmt", "s16", str(out)], check=True)
    with wave.open(str(out)) as w:
        data = np.frombuffer(w.readframes(w.getnframes()), dtype="<i2").astype(np.float64) / 32768.0
    return data


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("map")
    parser.add_argument("--out", default="")
    args = parser.parse_args()
    folder = ROOT / "Logs" / "map-films" / args.map / "video"
    head = (folder / "film.txt").read_text(encoding="utf-8").split("\t")
    length, fps = float(head[1]), float(head[2])
    out = Path(args.out) if args.out else ROOT / "Logs" / "map-films" / f"{args.map}.mp4"
    out.parent.mkdir(parents=True, exist_ok=True)

    with tempfile.TemporaryDirectory() as tmp:
        track = np.zeros(int(SR * (length + 0.5)))
        cache = {}

        def place(name, at, gain, pitch):
            if name not in cache:
                path = find_clip(name)
                cache[name] = load(path, tmp) if path else None
                if path is None:
                    print(f"  (no clip named {name}: left out)")
            clip = cache[name]
            if clip is None:
                return
            if abs(pitch - 1.0) > 1e-3:
                idx = np.arange(0, len(clip) - 1, pitch)
                clip = np.interp(idx, np.arange(len(clip)), clip)
            i = int(at * SR)
            if i >= len(track):
                return
            j = min(len(track), i + len(clip))
            track[i:j] += clip[: j - i] * gain

        score = SCORES.get(args.map)
        if score:
            place(score, 0.0, SCORE_LEVEL, 1.0)
            # The film fades its score out across the return home (MapFilm.Sound): the last 1.6 s.
            fade_from = int(SR * max(0.0, length - 1.6))
            fade = np.ones_like(track)
            fade[fade_from:] = np.linspace(1, 0, len(track) - fade_from) ** 1.5
            track *= fade
        cues = (folder / "cues.txt").read_text(encoding="utf-8").strip().splitlines() if (folder / "cues.txt").exists() else []
        for line in cues:
            age, name, volume, pitch = line.split("\t")
            place(name, float(age), float(volume), float(pitch))
        peak = np.max(np.abs(track)) or 1.0
        if peak > 0.98:
            track *= 0.98 / peak
        mix = Path(tmp) / "mix.wav"
        with wave.open(str(mix), "wb") as w:
            w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
            w.writeframes((np.clip(track, -1, 1) * 32767).astype("<i2").tobytes())
        subprocess.run([ffmpeg(), "-y", "-loglevel", "error", "-framerate", str(fps), "-i", str(folder / "frame_%04d.png"), "-i", str(mix),
                        "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "18", "-preset", "slow",
                        "-c:a", "aac", "-b:a", "192k", "-shortest", "-movflags", "+faststart", str(out)], check=True)
    print(f"wrote {out}  ({length:.1f} s, {len(cues)} cues)")


if __name__ == "__main__":
    main()
