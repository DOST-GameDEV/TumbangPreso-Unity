"""
Every hero line's voice, as a stylised babble in that hero's own voice (VOICE-1).

    python tools/generate_hero_voice.py              # write only what is missing
    python tools/generate_hero_voice.py --force      # rewrite every generated line
    python tools/generate_hero_voice.py --only sean.skill1.1
    python tools/generate_hero_voice.py --sheet out.png   # waveform/pitch sheet for review

⚠️⚠️ WHY BABBLE AND NOT SPEECH. `docs/HUMAN.md` is explicit that voice casting is scored at the
competition and that the team records its own lines ("it is original by construction, it needs no
licence"). A synthetic speaking voice standing in for that is a decision for the owner, not for a
build script. So this writes the approach cute games use when the words are on screen: a voice
with each hero's pitch, timbre and rhythm that follows the SHAPE of the line (its syllables, its
stresses, a question's rise, an exclamation's lift) without pronouncing it, the way Animal
Crossing and Banjo-Kazooie do. The words are an optional caption (`HeroVoice`; off by default,
because `docs/VISION.md` section 3 keeps sentences off the in-match HUD).

⚠️⚠️ A RECORDING REPLACES IT BY FILE NAME AND THIS SCRIPT NEVER OVERWRITES ONE. The clip for a line
is `Resources/HeroVo/hvo_<id>.wav` (`HeroLines.ClipName`). A plain run writes only missing files,
so a take the team drops in under that name is never touched; `--force` rewrites only files this
script wrote (it keeps a list in `tools/hero_voice_generated.txt`, outside `Resources` so it never ships) and still leaves recordings alone.

⚠️ THE LINES ARE READ FROM `HeroLines.cs` AS TEXT. One copy, two callers (`CLAUDE.md` section 4):
the script in the game is the script this voices. `HeroLinesTests` keeps the file's shape.

⚠️ SEEDED PER LINE ID (crc32), never per row position, for `generate_hero_audio.py`'s reason:
renumbering must not silently rewrite a shipped sound.

The voice model is `generate_hero_audio.py`'s: a glottal pulse train through three formant
resonators with breath mixed in. What is new is the TEXT-DRIVEN rhythm: one syllable per vowel
group, the onset consonant class drawn as a burst, a hiss, a hum or a glide, a stress contour per
sentence, and a hero's delivery on top.
"""
import argparse
import hashlib
import os
import re
import struct
import zlib

import numpy as np
from scipy.signal import lfilter

SR = 44100
HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
LINES_CS = os.path.join(ROOT, "Packages", "com.tumbangpreso.core", "Runtime", "HeroLines.cs")
OUT = os.path.join(ROOT, "Assets", "TumbangPreso", "Resources", "HeroVo")
LEDGER = os.path.join(HERE, "hero_voice_generated.txt")

# ------------------------------------------------------------------------------------ the heroes
#
# Each is one sentence of CHARACTER_ORIGINS.md turned into numbers. f0 is the resting pitch; tract
# scales every formant (above 1 is a smaller, younger throat, which the cute blocky cast wants);
# rate is syllables per second; swing is how far the pitch moves inside a sentence; breath is the
# share of turbulence; glide is how much each syllable's pitch slides (Zack's lazy slide, Nemu's
# drift); clip shortens vowels (Cheska's exactness); bounce alternates syllable pitch (Rafi's tease);
# lift is the extra rise on the last stressed syllable of an exclamation (Phaister's reveal).
HEROES = {
    #            f0   tract rate  swing breath glide clip bounce lift
    "sean":     (172, 1.10, 5.2,  0.10, 0.18, 0.02, 0.0, 0.00, 0.14),  # few words, even, sure
    "zack":     (205, 1.14, 7.0,  0.22, 0.14, 0.10, 0.0, 0.03, 0.22),  # quick, slides, showy
    "dante":    (122, 1.00, 4.2,  0.07, 0.22, 0.01, 0.0, 0.00, 0.10),  # low, slow, level
    "cheska":   (262, 1.22, 5.8,  0.09, 0.12, 0.00, 0.35, 0.00, 0.08),  # clear, clipped, exact
    "nemu":     (248, 1.24, 4.6,  0.16, 0.40, 0.12, 0.0, 0.00, 0.06),  # breathy, drifting, soft
    "phaister": (238, 1.18, 5.6,  0.30, 0.16, 0.05, 0.0, 0.00, 0.34),  # theatrical, wide, reveal
    "rafi":     (190, 1.12, 6.4,  0.18, 0.15, 0.04, 0.0, 0.09, 0.20),  # bouncy, warm, teasing
}

# Rough adult formants per vowel letter (F1, F2, F3), scaled by `tract`.
VOWELS = {
    "a": (750, 1220, 2600), "e": (530, 1840, 2480), "i": (300, 2250, 2950),
    "o": (500, 900, 2450), "u": (340, 870, 2250), "y": (320, 2100, 2800),
}
PLOSIVE, FRICATIVE, NASAL, LIQUID = set("pbtdkgcq"), set("sfhzvxj"), set("mn"), set("lrwy")


def read_lines():
    text = open(LINES_CS, encoding="utf-8").read()
    lines = {}
    for m in re.finditer(r'L\("([a-z]+\.[a-z0-9.]+)",\s*\w+,\s*"((?:[^"\\]|\\.)*)"\)', text):
        lines[m.group(1)] = m.group(2)
    for m in re.finditer(r'B\("([a-z]+)",\s*"([a-z]+)",\s*"((?:[^"\\]|\\.)*)",\s*"((?:[^"\\]|\\.)*)"\)', text):
        a, b, open_, reply = m.groups()
        lines[f"{a}.banter.{b}"] = open_
        lines[f"{b}.reply.{a}"] = reply
    return lines


def syllables(text):
    """[(onset class, vowel letter, stressed, sentence_end_mark or '')] plus pause markers."""
    out = []
    for sentence in re.findall(r"[^.!?]+[.!?]?", text):
        mark = sentence.strip()[-1] if sentence.strip()[-1:] in ".!?" else "."
        words = re.findall(r"[A-Za-z']+", sentence)
        sent = []
        for word in words:
            w = word.lower()
            groups = list(re.finditer(r"[aeiouy]+", w))
            if not groups:
                groups = [re.match(r".", w)]
            if len(groups) > 1 and w.endswith("e") and groups[-1].group() == "e":
                groups = groups[:-1]          # silent final e
            prev = 0
            for gi, g in enumerate(groups):
                onset = w[prev:g.start()]
                cls = ("p" if onset and onset[-1] in PLOSIVE else "f" if onset and onset[-1] in FRICATIVE
                       else "n" if onset and onset[-1] in NASAL else "l" if onset and onset[-1] in LIQUID else "")
                vowel = g.group()[0] if g.group()[0] in VOWELS else "a"
                stressed = gi == 0 and (len(w) > 3 or word[0].isupper())
                sent.append([cls, vowel, stressed, ""])
                prev = g.end()
        if sent:
            sent[-1][3] = mark
            out.extend(sent)
            out.append(None)                 # a breath between sentences
    return out


def resonator(x, f, bw):
    r = np.exp(-np.pi * bw / SR)
    theta = 2 * np.pi * f / SR
    return lfilter([1 - r], [1, -2 * r * np.cos(theta), r * r], x)


def render(line_id, text):
    hero = line_id.split(".")[0]
    f0, tract, rate, swing, breath, glide, clip, bounce, lift = HEROES[hero]
    rng = np.random.default_rng(zlib.crc32(line_id.encode()))
    sy = syllables(text)
    pieces = []
    count = sum(1 for s in sy if s)
    k = 0
    for s in sy:
        if s is None:
            pieces.append(np.zeros(int(SR * 0.11)))
            continue
        cls, vowel, stressed, mark = s
        k += 1
        pos = k / max(1, count)
        dur = (1.0 / rate) * (1.25 if stressed else 0.95) * (1 - clip * 0.5) * rng.uniform(0.9, 1.1)
        if mark == "!":
            dur *= 1.25
        n = int(dur * SR)
        t = np.arange(n) / SR
        # Pitch: declination through the line, stress, the mark, the hero's bounce and glide.
        p = f0 * (1 + swing * (0.35 - 0.5 * pos)) * (1.08 if stressed else 1.0)
        p *= 1 + bounce * (1 if k % 2 else -1)
        end = p
        if mark == "?":
            end = p * 1.35
        elif mark == "!":
            p *= 1 + lift
            end = p * (1 - 0.12)
        elif mark == ".":
            end = p * 0.88
        else:
            end = p * (1 - glide)
        pitch = np.linspace(p, end, n) * (1 + 0.012 * np.sin(2 * np.pi * 5.5 * t))
        phase = np.cumsum(pitch / SR)
        # Glottal source: a band-limited sawtooth with a soft tilt, plus breath.
        saw = 2 * (phase % 1.0) - 1
        source = lfilter([1], [1, -0.9], saw) * 0.12 + rng.normal(0, 1, n) * breath * 0.35
        f1, f2, f3 = (v * tract for v in VOWELS[vowel])
        voiced = resonator(source, f1, 90) * 1.0 + resonator(source, f2, 120) * 0.6 + resonator(source, f3, 170) * 0.3
        # Envelope: quick in, slower out; clipped heroes stop harder.
        a = int(SR * 0.012)
        r = int(n * (0.25 if clip else 0.4))
        env = np.ones(n)
        env[:a] = np.linspace(0, 1, a)
        env[-r:] = np.linspace(1, 0, r) ** (1.6 if clip else 1.0)
        syl = voiced * env * (1.2 if stressed else 1.0) * (1.25 if mark == "!" else 1.0)
        # The onset consonant, as a class rather than a phoneme.
        m = int(SR * 0.03)
        on = np.zeros(m)
        if cls == "p":
            # A soft click, not a crack: at full level the burst outranked the vowels and the
            # peak normalisation then turned the whole line down around it (first review sheet).
            b = int(SR * 0.006)
            on[:b] = resonator(rng.normal(0, 1, b), 2500 * tract, 1500) * np.linspace(1, 0, b) * 0.35
        elif cls == "f":
            on = resonator(rng.normal(0, 0.5, m), 4200 * tract, 1800) * np.linspace(0.6, 0, m)
        elif cls == "n":
            hum_t = np.arange(m) / SR
            on = np.sin(2 * np.pi * p * hum_t) * 0.18 * np.linspace(0.3, 1, m)
        elif cls == "l":
            on = resonator(saw[:m] if n >= m else np.zeros(m), 400 * tract, 150) * 0.4
        pieces.append(np.concatenate([on, syl, np.zeros(int(SR * 0.012))]))
    audio = np.concatenate(pieces + [np.zeros(int(SR * 0.05))])
    audio = lfilter([1, -0.3], [1], audio)            # a touch of presence
    peak = np.max(np.abs(audio)) or 1.0
    return audio / peak * (10 ** (-6 / 20))           # HUMAN.md: -6 dBFS peak


def write_wav(path, samples):
    data = (np.clip(samples, -1, 1) * 32767).astype("<i2").tobytes()
    with open(path, "wb") as f:
        f.write(b"RIFF" + struct.pack("<I", 36 + len(data)) + b"WAVEfmt ")
        f.write(struct.pack("<IHHIIHH", 16, 1, 1, SR, SR * 2, 2, 16))
        f.write(b"data" + struct.pack("<I", len(data)) + data)


META = """fileFormatVersion: 2
guid: {guid}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 8
  defaultSettings:
    serializedVersion: 2
    loadType: 0
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: 1
    quality: 1
    conversionMode: 0
    preloadAudioData: 0
  platformSettingOverrides: {{}}
  forceToMono: 0
  normalize: 1
  loadInBackground: 0
  ambisonic: 0
  3D: 1
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--force", action="store_true")
    ap.add_argument("--only")
    ap.add_argument("--sheet")
    args = ap.parse_args()
    os.makedirs(OUT, exist_ok=True)
    generated = set(open(LEDGER).read().split()) if os.path.exists(LEDGER) else set()
    lines = read_lines()
    wrote = 0
    for line_id, text in sorted(lines.items()):
        if args.only and line_id != args.only:
            continue
        name = "hvo_" + line_id.replace(".", "_")
        path = os.path.join(OUT, name + ".wav")
        exists = os.path.exists(path)
        if exists and not (args.force and name in generated) and not (args.only and name in generated):
            continue
        write_wav(path, render(line_id, text))
        generated.add(name)
        if not os.path.exists(path + ".meta"):
            # A stable guid from the name, so every machine that runs this agrees.
            guid = hashlib.md5(("hero-voice/" + name).encode()).hexdigest()
            open(path + ".meta", "w").write(META.format(guid=guid))
        wrote += 1
    with open(LEDGER, "w") as f:
        f.write("\n".join(sorted(generated)) + "\n")
    print(f"{len(lines)} lines, {wrote} written, {len(generated)} generated by this script")

    if args.sheet:
        sheet(lines, args.sheet)


def sheet(lines, out):
    """One row per hero: the waveform of two lines, so the voices can be compared by eye."""
    from PIL import Image, ImageDraw
    heroes = list(HEROES)
    w, h = 1400, 90
    img = Image.new("RGB", (w, h * len(heroes)), (250, 243, 230))
    d = ImageDraw.Draw(img)
    for row, hero in enumerate(heroes):
        ids = [i for i in sorted(lines) if i.startswith(hero + ".ultally") or i.startswith(hero + ".skill1")][:2]
        x = 150
        d.text((8, row * h + 36), hero, fill=(60, 30, 10))
        for line_id in ids:
            a = render(line_id, lines[line_id])
            n = len(a)
            width = int(n / SR * 260)
            for px in range(width):
                seg = a[int(px / width * n): int((px + 1) / width * n) + 1]
                v = float(np.max(np.abs(seg))) if len(seg) else 0
                y = row * h + 45
                d.line([(x + px, y - v * 40), (x + px, y + v * 40)], fill=(152, 7, 21))
            d.text((x, row * h + 4), lines[line_id][:40], fill=(85, 41, 15))
            x += width + 40
    img.save(out)
    print(out)


if __name__ == "__main__":
    main()
