"""Encode timestamped runtime captures at their recorded wall-clock speed.

No interpolated frames and no arbitrary input FPS. Repeated output frames preserve
the time between captures, including stalls. The CSV remains the timing authority.
"""
import argparse
import csv
from pathlib import Path
import subprocess
import imageio_ffmpeg


def encode(folder, with_audio=False):
    rows = list(csv.DictReader((folder / "frames.csv").open()))
    if len(rows) < 2:
        raise ValueError(f"No continuous capture: {folder}")
    lines = []
    for i, row in enumerate(rows):
        frame = folder / f"{int(row['frame']):05}.jpg"
        if not frame.is_file():
            raise FileNotFoundError(frame)
        duration = (float(rows[i+1]["real_seconds"]) - float(row["real_seconds"])) if i+1 < len(rows) else .05
        if duration <= 0:
            raise ValueError("Capture times are not increasing")
        lines += [f"file '{frame.name}'", f"duration {duration:.6f}"]
    lines.append(f"file '{int(rows[-1]['frame']):05}.jpg'")
    manifest = folder / "frames.ffconcat"
    manifest.write_text("\n".join(lines) + "\n")
    output = folder.parent / (folder.name + ".mp4")
    command = [imageio_ffmpeg.get_ffmpeg_exe(), "-hide_banner", "-loglevel", "error", "-y",
               "-safe", "0", "-f", "concat", "-i", str(manifest)]
    sound = folder / "game-audio.wav"
    if with_audio and sound.exists():
        # Same recorded-clock alignment as encode_motion_review.py. This is
        # engine-output review sync, not microphone capture or latency certification.
        info = dict(line.split("=", 1) for line in (folder / "game-audio.txt").read_text().splitlines() if "=" in line)
        origin = float((folder / "capture-start.txt").read_text())
        trim = origin + float(rows[0]["real_seconds"]) - float(info["first_callback_real"])
        command += ["-i", str(sound), "-map", "0:v:0", "-map", "1:a:0", "-af",
                    f"atrim=start={trim:.6f},asetpts=PTS-STARTPTS" if trim >= 0 else f"adelay={-trim * 1000:.3f}:all=1",
                    "-c:a", "aac", "-b:a", "192k", "-shortest"]
    command += ["-fps_mode", "vfr", "-c:v", "libx264", "-crf", "19", "-pix_fmt", "yuv420p",
                "-movflags", "+faststart", str(output)]
    subprocess.run(command, check=True)
    print(output, "frames", len(rows), "seconds", rows[-1]["real_seconds"])


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("directory", type=Path)
    parser.add_argument("--audio", action="store_true", help="include available engine audio aligned to its recorded callback clock")
    args = parser.parse_args()
    for csv_path in args.directory.rglob("frames.csv"):
        encode(csv_path.parent, with_audio=args.audio)
