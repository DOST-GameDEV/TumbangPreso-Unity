"""Encode timestamped runtime captures at their recorded wall-clock speed.

No interpolated frames and no arbitrary input FPS. Repeated output frames preserve
the time between captures, including stalls. The CSV remains the timing authority.
"""
import argparse
import csv
from pathlib import Path
import subprocess
import imageio_ffmpeg


def encode(folder):
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
    subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), "-hide_banner", "-loglevel", "error", "-y",
                    "-safe", "0", "-f", "concat", "-i", str(manifest), "-fps_mode", "vfr",
                    "-c:v", "libx264", "-crf", "19", "-pix_fmt", "yuv420p", "-movflags", "+faststart",
                    str(output)], check=True)
    print(output, "frames", len(rows), "seconds", rows[-1]["real_seconds"])


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("directory", type=Path)
    args = parser.parse_args()
    for csv_path in args.directory.rglob("frames.csv"):
        encode(csv_path.parent)
