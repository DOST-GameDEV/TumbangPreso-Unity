"""Does each cue file actually contain an audible, unclipped sound?

WHY THIS IS SEPARATE FROM `AudioCueCheck`:
That check answers four questions and none of them is about the AUDIO. It proves a cue has a
file, that a file has a cue, that a call site exists, and that the container matches its
extension. A cue that is registered, called, correctly named and two seconds of DIGITAL SILENCE
passes it cleanly, and so does one that is clipped into a buzz.

Raised on 2026-08-26: *"Check no cue is broken. AudioCueCheck passes but only proves a file
exists, is a real WAV, and has a call site."*

WHAT IT REPORTS PER FILE:
  peak      the largest absolute sample. Under 0.02 is inaudible in a mix.
  rms       loudness. A file with a high peak and a near-zero rms is one click.
  dc        mean sample value. A non-zero mean is a thump on every play and eats headroom.
  seam      |last - first|, which only matters for a cue something LOOPS. A one-shot authored
            with a fade begins and ends at zero, so it drops to silence at the loop point and
            swells back: that shipped once, on the LRT pass, and `sfx_lrt_rumble` exists
            because of it.

Run:  python tools/audit_cue_audio.py
"""
import os
import struct
import sys
import wave

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
SFX = os.path.join(ROOT, "Assets", "TumbangPreso", "Art", "audio", "sfx")

# A cue quieter than this is one nobody will hear over a match.
QUIET_PEAK = 0.02

# Above this for a meaningful share of the file, the waveform is squared off rather than loud.
CLIP_LEVEL = 0.999
CLIP_SHARE = 0.002


def read(path):
    with wave.open(path, "rb") as w:
        channels = w.getnchannels()
        width = w.getsampwidth()
        frames = w.readframes(w.getnframes())
        rate = w.getframerate()

    if width != 2:
        return None, rate

    count = len(frames) // 2
    values = struct.unpack("<%dh" % count, frames[: count * 2])

    if channels > 1:
        values = values[::channels]

    return [v / 32768.0 for v in values], rate


def main():
    problems = 0
    rows = []

    for name in sorted(os.listdir(SFX)):
        if not name.endswith(".wav"):
            continue

        path = os.path.join(SFX, name)
        try:
            samples, rate = read(path)
        except Exception as e:
            print("UNREADABLE %s: %s" % (name, e))
            problems += 1
            continue

        if not samples:
            print("EMPTY %s" % name)
            problems += 1
            continue

        peak = max(abs(v) for v in samples)
        rms = (sum(v * v for v in samples) / len(samples)) ** 0.5
        dc = sum(samples) / len(samples)
        clipped = sum(1 for v in samples if abs(v) >= CLIP_LEVEL)
        seam = abs(samples[-1] - samples[0])
        seconds = len(samples) / float(rate)

        flags = []
        if peak < QUIET_PEAK:
            flags.append("SILENT")
        if clipped > len(samples) * CLIP_SHARE:
            flags.append("CLIPPED(%d)" % clipped)
        if abs(dc) > 0.02:
            flags.append("DC(%.3f)" % dc)

        if flags:
            problems += 1

        rows.append((name, seconds, peak, rms, dc, seam, " ".join(flags)))

    print("%-26s %6s %6s %6s %7s %7s  %s"
          % ("cue", "sec", "peak", "rms", "dc", "seam", "flags"))
    for r in rows:
        print("%-26s %6.2f %6.3f %6.3f %7.4f %7.4f  %s"
              % (r[0], r[1], r[2], r[3], r[4], r[5], r[6]))

    print()
    print("%d files, %d flagged" % (len(rows), problems))
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
