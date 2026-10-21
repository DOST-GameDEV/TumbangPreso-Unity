"""Encode the Arena film (`Tests/PlayMode/ArenaMatchFilm.cs`) into one clean mp4 with the game's own sound.

    python tools/encode_arena_film.py Logs/arena-film Logs/arena-film/arena_entrance_bot_match.mp4

Owner, 2026-10-21: "don't put text, just give me the video". So, unlike `stitch_ability_film.py`, nothing is
drawn on the picture: the frames as the game camera rendered them, at 30 fps, over the film's cue log mixed by
`stitch_ability_film.mix` (each cue's wav from Resources/Sfx at its logged time, pitch and gain).
"""
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import stitch_ability_film as stitch  # noqa: E402


def main():
    film, out = Path(sys.argv[1]), Path(sys.argv[2])
    ffmpeg = shutil.which("ffmpeg")
    if ffmpeg is None:
        sys.exit("ffmpeg is not on PATH")
    frames = sorted((film / "view").glob("*.jpg"))
    if not frames:
        sys.exit("no frames in " + str(film / "view"))
    seconds = len(frames) / stitch.FPS
    work = Path(tempfile.mkdtemp(prefix="arena-film-"))
    try:
        audio = work / "mix.wav"
        stitch.write_wav(audio, stitch.mix(film / "cues.csv", seconds, 0.55))
        out.parent.mkdir(parents=True, exist_ok=True)
        subprocess.run([ffmpeg, "-v", "error", "-y", "-framerate", str(stitch.FPS), "-i", str(film / "view" / "%05d.jpg"),
                        "-i", str(audio), "-c:v", "libx264", "-preset", "slow", "-crf", "18", "-pix_fmt", "yuv420p",
                        "-c:a", "aac", "-b:a", "192k", "-shortest", "-movflags", "+faststart", str(out)], check=True)
        print(f"wrote {out} ({len(frames)} frames, {seconds:.1f} s, {out.stat().st_size / 1e6:.1f} MB)")
    finally:
        shutil.rmtree(work, ignore_errors=True)


if __name__ == "__main__":
    main()
