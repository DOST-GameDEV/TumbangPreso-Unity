"""Put the owner's CHOSEN Paete skill sound drafts into the game.

  py -3 tools/install_paete_skill_sfx.py

The drafts are built by tools/build_paete_skill_sfx.py into Logs/paete-sfx-drafts/ (real CC0 recordings, listed in
tools/paete_sfx_sources.json) and nothing reaches the game until the owner has picked by ear. His picks so far:

  LIANA LEAP, 2026-10-07: "lets try option E" (the B and C blend leaning heavier).
    sfx_cast_paete_vine.wav    the cast: the coil, and the vines firing 0.14 s after it (the kit's own cast cue)
    sfx_paete_vine_catch.wav   the catch, played by each peer when its own vine arrives (Visual.PaeteVineReach)
    sfx_paete_vine_land.wav    the landing, played by each peer when its own reel ends

Each name has a row in Runtime/Audio/AudioCues.cs and is listed in `ReworkedSkillSfx`, which is what makes one skill's
sounds audible while every other skill's stay switched off. 44100 Hz, mono, 16 bit, peak 0.85.
"""
import wave
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
DRAFTS = ROOT / "Logs/paete-sfx-drafts"
OUT = ROOT / "Assets/TumbangPreso/Resources/Sfx"
SR, PEAK = 44100, 0.85


def read(name):
    with wave.open(str(DRAFTS / name), "rb") as w:
        assert w.getframerate() == SR and w.getnchannels() == 1 and w.getsampwidth() == 2, name
        return np.frombuffer(w.readframes(w.getnframes()), np.int16).astype(np.float32) / 32768.0


def write(name, x):
    x = x * min(1.0, PEAK / max(1e-6, float(np.max(np.abs(x)))))
    with wave.open(str(OUT / name), "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((x * 32767).astype(np.int16).tobytes())
    print("wrote", name, "%.2f s" % (len(x) / SR))


def mix(parts):
    end = max(int(at * SR) + len(x) for x, at in parts)
    out = np.zeros(end, np.float32)
    for x, at in parts:
        a = int(at * SR); out[a:a + len(x)] += x
    return out


LETTER = "e"
write("sfx_cast_paete_vine.wav", mix([(read("liana_coil_%s.wav" % LETTER), 0.0), (read("liana_fire_%s.wav" % LETTER), 0.14)]))
write("sfx_paete_vine_catch.wav", read("liana_catch_%s.wav" % LETTER))
write("sfx_paete_vine_land.wav", read("liana_land_%s.wav" % LETTER))
