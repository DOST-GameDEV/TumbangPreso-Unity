"""Encodes the Ilalim sidewalk films, mixes and muxes their soundtracks, and composes the taho carry comparison.

    py -3 tools/encode_ilalim_films.py            (videos: frames/<event>/f_*.jpg -> <event>.mp4, with sound)
    py -3 tools/encode_ilalim_films.py --compare  (stills/taho_carry_compare.png)
    py -3 tools/encode_ilalim_films.py --keep-frames
    py -3 tools/encode_ilalim_films.py --dir Logs/ilalim-unity/videos_v1   (an older set)
    py -3 tools/encode_ilalim_films.py --audio-only   (re-mix and re-mux the existing <event>.mp4 files)

The frames come from `IlalimSidewalkFilm.RunVideos` (Unity batch, 30 steps a second, 1280x720).
H.264, yuv420p, 30 fps, through imageio-ffmpeg's bundled ffmpeg when none is on PATH. The frame
folders are deleted after a successful encode unless --keep-frames.

THE SOUNDTRACK (2026-10-01). The film's second pass writes `sound_events.tsv` (every sound the
sidewalk life started: world time, clip, position, gain, pitch, near and far reach) and one
`camera_<event>.tsv` per film (each frame's world time, eye and aim). Each film's track is mixed
from the clips themselves (`Assets/TumbangPreso/Art/audio/ambience/`, or `Resources/Sfx/` for the
game's own footstep) the way `SidewalkLife.Sound` plays them in the game: the gain, Unity's
logarithmic rolloff from the camera (near / distance, held at `far`), resampled by the pitch, and
panned (equal power) by where the sound is across the camera. Kanto's city bed runs under it at
its in-game floor (`KantoStreetSound`: 0.6 x 0.7), so the mix is heard in its street, not in a
silent room. Engines, horns and sirens are not in the mix. The track is written beside the film
as `<event>.wav` and muxed in as AAC.
"""
import csv
import shutil
import subprocess
import sys
import wave
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Logs/ilalim-unity/videos_v2"
RATE = 44100
FPS = 30
AMBIENCE = ROOT / "Assets/TumbangPreso/Art/audio/ambience"
SFX = ROOT / "Assets/TumbangPreso/Resources/Sfx"
BED = AMBIENCE / "kanto_city_bed.wav"
BED_LEVEL = 0.6 * 0.7


def ffmpeg():
    exe = shutil.which("ffmpeg")
    if exe:
        return exe
    import imageio_ffmpeg
    return imageio_ffmpeg.get_ffmpeg_exe()


def read_wav(path):
    with wave.open(str(path)) as w:
        n, ch, width, rate = w.getnframes(), w.getnchannels(), w.getsampwidth(), w.getframerate()
        data = np.frombuffer(w.readframes(n), dtype=np.int16 if width == 2 else np.int32).astype(np.float64)
    data /= float(2 ** (8 * width - 1))
    data = data.reshape(-1, ch)
    if rate != RATE:
        t = np.arange(int(len(data) * RATE / rate)) * rate / RATE
        data = np.stack([np.interp(t, np.arange(len(data)), data[:, c]) for c in range(ch)], axis=1)
    return data


_clips = {}


def clip(name):
    if name not in _clips:
        for folder in (AMBIENCE, SFX):
            path = folder / f"{name}.wav"
            if path.exists():
                _clips[name] = read_wav(path).mean(axis=1)
                break
        else:
            print(f"  no clip {name}, skipped")
            _clips[name] = None
    return _clips[name]


def read_tsv(path):
    with open(path, newline="", encoding="utf-8") as f:
        return list(csv.DictReader(f, delimiter="\t"))


def soundtrack(folder, name, frames):
    """The film's stereo track (see the module note), or None without the logs."""
    events_path, camera_path = folder / "sound_events.tsv", folder / f"camera_{name}.tsv"
    if not events_path.exists() or not camera_path.exists():
        return None
    cam = read_tsv(camera_path)
    t = np.array([float(r["t"]) for r in cam])
    eye = np.array([[float(r["eye_x"]), float(r["eye_y"]), float(r["eye_z"])] for r in cam])
    aim = np.array([[float(r["at_x"]), float(r["at_y"]), float(r["at_z"])] for r in cam])
    length = int(frames / FPS * RATE)
    mix = np.zeros((length, 2))
    # The film's cuts: runs of frames one step apart.
    breaks = [0] + [i for i in range(1, len(t)) if t[i] - t[i - 1] > 1.5 / FPS] + [len(t)]
    placed = 0
    for row in read_tsv(events_path):
        te = float(row["t"])
        data = clip(row["clip"])
        if data is None:
            continue
        pitch = float(row["pitch"])
        seconds = len(data) / RATE / pitch
        for a, b in zip(breaks[:-1], breaks[1:]):
            t0, t1 = t[a], t[b - 1]
            if not (t0 - seconds < te <= t1 + 0.5 / FPS):
                continue
            k = int(np.clip(np.searchsorted(t, te), a, b - 1))
            at = np.array([float(row["x"]), float(row["y"]), float(row["z"])])
            to = at - eye[k]
            d = float(np.linalg.norm(to))
            near, far = float(row["near"]), float(row["far"])
            level = float(row["gain"]) * near / min(max(d, near), far)
            forward = aim[k] - eye[k]
            forward /= max(1e-6, np.linalg.norm(forward))
            right = np.cross([0.0, 1.0, 0.0], forward)
            right /= max(1e-6, np.linalg.norm(right))
            x = float(np.dot(to / max(d, 1e-6), right))
            angle = (np.clip(x, -1.0, 1.0) + 1.0) * np.pi / 4.0
            sound = np.interp(np.arange(0, len(data) - 1, pitch), np.arange(len(data)), data) * level
            start = int(round((a / FPS + (te - t0)) * RATE))
            lo, hi = max(0, start), min(length, start + len(sound))
            if hi <= lo:
                continue
            piece = sound[lo - start:hi - start]
            mix[lo:hi, 0] += piece * np.cos(angle)
            mix[lo:hi, 1] += piece * np.sin(angle)
            placed += 1
    if BED.exists():
        bed = read_wav(BED)
        if bed.shape[1] == 1:
            bed = np.repeat(bed, 2, axis=1)
        reps = int(np.ceil(length / len(bed))) + 1
        mix += np.tile(bed, (reps, 1))[:length] * BED_LEVEL
    peak = float(np.abs(mix).max()) if length else 0.0
    if peak > 0.95:
        mix = np.tanh(mix / 0.95) * 0.95
    fade = min(length, int(0.02 * RATE))
    if fade:
        ramp = np.linspace(0.0, 1.0, fade)[:, None]
        mix[:fade] *= ramp
        mix[-fade:] *= ramp[::-1]
    target = folder / f"{name}.wav"
    with wave.open(str(target), "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes((np.clip(mix, -1.0, 1.0) * 32767).astype(np.int16).tobytes())
    print(f"  {target.name}: {placed} sounds, peak {peak:.2f}")
    return target


def mux(folder, name, frames):
    video = folder / f"{name}.mp4"
    track = soundtrack(folder, name, frames)
    if track is None or not video.exists():
        return
    temp = folder / f"{name}.muxing.mp4"
    subprocess.run([ffmpeg(), "-y", "-loglevel", "error", "-i", str(video), "-i", str(track), "-map", "0:v", "-map", "1:a",
                    "-c:v", "copy", "-c:a", "aac", "-b:a", "192k", "-shortest", "-movflags", "+faststart", str(temp)], check=True)
    temp.replace(video)
    print(f"  muxed the soundtrack into {video.name}")


def encode(folder, keep):
    frames = folder / "frames"
    for sub in sorted(p for p in frames.iterdir() if p.is_dir()):
        count = len(list(sub.glob("f_*.jpg")))
        if count < 2:
            print(f"{sub.name}: {count} frames, skipped")
            continue
        target = folder / f"{sub.name}.mp4"
        subprocess.run([ffmpeg(), "-y", "-loglevel", "error", "-framerate", str(FPS), "-i", str(sub / "f_%05d.jpg"),
                        "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "18", "-preset", "slow",
                        "-movflags", "+faststart", str(target)], check=True)
        print(f"{target.relative_to(ROOT)}: {count} frames, {count / FPS:.1f} s, {target.stat().st_size // 1024} KB")
        mux(folder, sub.name, count)
        if not keep:
            shutil.rmtree(sub)


def audio_only(folder):
    for camera in sorted(folder.glob("camera_*.tsv")):
        name = camera.stem[len("camera_"):]
        frames = len(read_tsv(camera))
        print(f"{name}: {frames} frames")
        mux(folder, name, frames)


def compare(folder):
    from PIL import Image, ImageDraw, ImageFont
    stills = folder / "stills/taho"
    rows = [("waist", "WAIST (the old look)"), ("shoulder", "SHOULDER (the default now)")]
    cols = ["side", "front", "back", "walk_side"]
    tile_w, tile_h, label = 640, 360, 44
    sheet = Image.new("RGB", (tile_w * len(cols), (tile_h + label) * len(rows)), (24, 24, 28))
    draw = ImageDraw.Draw(sheet)
    try:
        font = ImageFont.truetype("arialbd.ttf", 26)
    except OSError:
        font = ImageFont.load_default()
    for r, (key, title) in enumerate(rows):
        y = r * (tile_h + label)
        draw.text((14, y + 8), f"MAGTATAHO CARRY: {title}", fill=(240, 236, 224), font=font)
        for c, view in enumerate(cols):
            path = stills / f"{key}_{view}.png"
            if not path.exists():
                continue
            tile = Image.open(path).convert("RGB").resize((tile_w, tile_h), Image.LANCZOS)
            sheet.paste(tile, (c * tile_w, y + label))
            draw.text((c * tile_w + 10, y + label + 8), view.replace("_", " "), fill=(255, 255, 255), font=font)
    target = folder / "stills/taho_carry_compare.png"
    sheet.save(target)
    print(f"wrote {target.relative_to(ROOT)}")


if __name__ == "__main__":
    args = sys.argv[1:]
    folder = OUT
    if "--dir" in args:
        folder = ROOT / args[args.index("--dir") + 1]
    if "--compare" in args:
        compare(folder)
    elif "--audio-only" in args:
        audio_only(folder)
    else:
        encode(folder, "--keep-frames" in args)
