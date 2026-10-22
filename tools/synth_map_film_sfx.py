"""The map intro films' scores and cues, by deterministic synthesis.

  py -3 tools/synth_map_film_sfx.py
  py -3 tools/synth_map_film_sfx.py --only tondo        (any substring of the file names)

Owner, 2026-10-22 (docs/TODO.md TOURNAMENT-FEEDBACK-1022, "Map intro films"): each map's opening with its own mood,
music and sound. The films are Runtime/Map/MapFilm.cs and one director per map. Each score is laid out on its
film's own beat table (the times below are the director's constants) and played from the film's clock, so the
music's hits land on the cuts. Cues are played by the film's own AudioSources straight from Resources (no row in
Runtime/Audio/AudioCues.cs), on every peer from the film's clock, so none is relayed. No human voice is made here.

Writes into Assets/TumbangPreso/Resources/Sfx/ (44100 Hz, mono, 16 bit, normalised to 0.85 peak like every other
cue there; the mix level is the film's own):
    sfx_film_tondo_score.wav   20.5 s  TONDO: a plucked lullaby under the laundry, the doors' bounce, the count's hush
    sfx_film_tondo_clap.wav     0.30 s  one child's clap, for each syllable of "Maiba taya"
    sfx_film_tondo_chalk.wav    2.40 s  chalk dragged fast along concrete, and the skid
    sfx_film_tondo_door.wav     0.90 s  a wooden door: the latch, the creak, the bump on its stop
    sfx_film_tondo_taya.wav     1.60 s  the count lands: a toy glockenspiel run up to a bright chord
    sfx_film_ermita_score.wav  17.0 s  ERMITA: a low pulse under the viaduct, a riser with the train, the drop as it passes
    sfx_film_ermita_horn.wav    2.20 s  the LRT's horn, far down the line, two tones in the concrete
    sfx_film_ermita_drop.wav    2.00 s  the drop: a sub boom, a crack and a metal ring as the train passes over
    sfx_film_ermita_buzz.wav    1.40 s  a sodium lamp striking: clicks, a stuttering hum, then on
    sfx_film_ermita_step.wav    1.00 s  the standoff: a low hit and a scrape of strings as the two sides walk in

Same house method as synth_arena_intro_sfx.py: numpy and scipy only, fixed seeds, nothing recorded or downloaded,
so a rerun is byte-identical."""
import argparse
import wave
from pathlib import Path

import numpy as np
from scipy import signal

SR = 44100
PEAK = 0.85
OUT = Path(__file__).resolve().parents[1] / "Assets/TumbangPreso/Resources/Sfx"


# ------------------------------------------------------------------ building blocks

def t_of(seconds):
    return np.arange(int(SR * seconds)) / SR


def lowpass(x, cut, order=2):
    b, a = signal.butter(order, min(cut, SR * 0.45) / (SR / 2), "low")
    return signal.lfilter(b, a, x)


def highpass(x, cut, order=2):
    b, a = signal.butter(order, cut / (SR / 2), "high")
    return signal.lfilter(b, a, x)


def band(x, low, high, order=2):
    b, a = signal.butter(order, [low / (SR / 2), min(high, SR * 0.45) / (SR / 2)], "band")
    return signal.lfilter(b, a, x)


def fade(x, fade_in=0.004, fade_out=0.03):
    n = len(x)
    a = min(n // 2, int(SR * fade_in))
    b = min(n // 2, int(SR * fade_out))
    y = x.copy()
    if a > 0:
        y[:a] *= 0.5 - 0.5 * np.cos(np.linspace(0, np.pi, a))
    if b > 0:
        y[-b:] *= 0.5 + 0.5 * np.cos(np.linspace(0, np.pi, b))
    return y


def note(name):
    """'C4' style name to Hz."""
    names = {"C": 0, "C#": 1, "D": 2, "D#": 3, "E": 4, "F": 5, "F#": 6, "G": 7, "G#": 8, "A": 9, "A#": 10, "B": 11}
    pitch, octave = name[:-1], int(name[-1])
    return 440.0 * 2 ** ((names[pitch] + 12 * (octave + 1) - 69) / 12)


def pluck(freq, seconds, rng, bright=0.5, decay=0.996):
    """Karplus-Strong: a nylon string or a ukulele, by how bright the burst is."""
    n = int(SR * seconds)
    period = max(2, int(SR / freq))
    buf = rng.uniform(-1, 1, period)
    buf = lowpass(buf, 1500 + 7000 * bright)
    out = np.zeros(n)
    idx = 0
    for i in range(n):
        out[i] = buf[idx]
        nxt = (idx + 1) % period
        buf[idx] = decay * 0.5 * (buf[idx] + buf[nxt])
        idx = nxt
    return fade(out, 0.001, 0.05)


def bell(freq, seconds, partials=((1, 1.0), (2.76, 0.42), (5.4, 0.18), (8.93, 0.08)), decay=3.0):
    """A music box or glockenspiel tine: inharmonic partials, each dying faster than the last."""
    t = t_of(seconds)
    y = np.zeros_like(t)
    for ratio, gain in partials:
        y += gain * np.sin(2 * np.pi * freq * ratio * t) * np.exp(-t * decay * (0.6 + ratio * 0.35))
    return fade(y, 0.0015, 0.05)


def pad(freqs, seconds, attack=1.2, release=1.5, bright=900):
    """A soft chord: detuned triangles, low-passed, swelling in and out."""
    t = t_of(seconds)
    y = np.zeros_like(t)
    for f in freqs:
        for detune in (-0.004, 0.0, 0.0045):
            y += signal.sawtooth(2 * np.pi * f * (1 + detune) * t, 0.5)
    y = lowpass(y / (3 * len(freqs)), bright)
    env = np.minimum(1, t / attack) * np.minimum(1, (seconds - t) / release)
    return y * np.clip(env, 0, 1)


def kick(seconds=0.35, low=48, high=140):
    t = t_of(seconds)
    f = low + (high - low) * np.exp(-t * 28)
    return fade(np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 9), 0.0005, 0.04)


def noise_hit(seconds, rng, low, high, decay):
    t = t_of(seconds)
    return fade(band(rng.uniform(-1, 1, len(t)), low, high) * np.exp(-t * decay), 0.0005, 0.02)


def place(track, clip, at, gain=1.0):
    i = int(at * SR)
    if i >= len(track):
        return
    j = min(len(track), i + len(clip))
    track[i:j] += clip[: j - i] * gain


def room(x, seconds=1.6, mix=0.22, seed=7, tone=3500):
    """A small reverb: the dry signal convolved with a decaying, darkened noise tail."""
    rng = np.random.default_rng(seed)
    t = t_of(seconds)
    ir = lowpass(rng.normal(0, 1, len(t)), tone) * np.exp(-t * 4.2 / seconds)
    ir /= np.sqrt(np.sum(ir ** 2))
    wet = signal.fftconvolve(x, ir)[: len(x)]
    return x * (1 - mix) + wet * mix * 3.0


def write(name, y, only):
    if only and only not in name:
        return
    y = np.asarray(y, dtype=np.float64)
    y = y - np.mean(y)
    peak = np.max(np.abs(y)) or 1.0
    y = y / peak * PEAK
    data = (np.clip(y, -1, 1) * 32767).astype("<i2").tobytes()
    OUT.mkdir(parents=True, exist_ok=True)
    with wave.open(str(OUT / name), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data)
    print(f"wrote {name}  {len(y) / SR:.2f} s")


# ------------------------------------------------------------------ TONDO: nostalgic and playful
#
# TondoFilm: SAMPAY 0.0, CHALK 3.6, DOORS 6.4 (one each 0.9 s), GATHER 10.2, MAIBA 11.3, syllables 11.75 12.15 12.55
# 12.95 (YA), the reaction to 14.3, TAYA 14.3, CLOSE 16.2, end 19.6. 100 bpm (0.6 s a beat); C major, pentatonic tunes.

def tondo_score():
    rng = np.random.default_rng(1022)
    total = 20.5
    track = np.zeros(int(SR * total))
    beat = 0.6

    # The lullaby on a music box from the first frame: under the laundry, looking up.
    tune = ["E5", "G5", "A5", "G5", "E5", "D5", "C5", "D5", "E5", "G5", "C6", "A5"]
    for i, n in enumerate(tune):
        at = 0.25 + i * beat * 0.5 if i < 6 else 0.25 + 3 * beat + (i - 6) * beat * 0.5
        place(track, bell(note(n), 2.2, decay=2.2), at, 0.30)

    # The chords underneath, warm and slow: C, Am, F, G, then home.
    chords = [(0.0, ["C3", "G3", "E4"]), (3.6, ["A2", "E3", "C4"]), (6.4, ["F2", "C3", "A3"]), (8.8, ["G2", "D3", "B3"]),
              (11.0, ["A2", "E3", "C4"]), (12.95, ["C3", "G3", "E4", "G4"])]
    for i, (at, names) in enumerate(chords):
        end = chords[i + 1][0] if i + 1 < len(chords) else total
        place(track, pad([note(n) for n in names], end - at + 0.8, attack=0.6 if i else 1.4, release=1.0), at, 0.22)

    # CHALK: plucked strings come in, a bouncing figure that runs with the kid.
    run = ["C5", "E5", "G5", "A5", "G5", "E5", "D5", "E5", "G5", "C6"]
    for i, n in enumerate(run):
        place(track, pluck(note(n), 0.9, rng, bright=0.65), 3.6 + i * beat * 0.5, 0.42)

    # DOORS: the groove, a bass pluck on the beat, strums on the off-beat, a shaker, one door each second.
    bass = ["F2", "F2", "C3", "F2", "G2", "G2", "D3", "G2"]
    for k in range(8):
        at = 6.4 + k * beat
        place(track, pluck(note(bass[k]), 0.8, rng, bright=0.25, decay=0.993), at, 0.55)
        for s, n in enumerate(["F4", "A4", "C5"] if k < 4 else ["G4", "B4", "D5"]):
            place(track, pluck(note(n), 0.5, rng, bright=0.7), at + beat * 0.5 + s * 0.012, 0.22)
        place(track, noise_hit(0.08, rng, 5000, 12000, 50), at + beat * 0.25, 0.12)
        place(track, noise_hit(0.08, rng, 5000, 12000, 50), at + beat * 0.75, 0.16)
    for k in range(4):
        # Each door gets a rising tine as it opens.
        place(track, bell(note(["C6", "D6", "E6", "G6"][k]), 1.2, decay=3.5), 6.4 + k * 1.0 + 0.1, 0.22)

    # MAIBA: the band drops away to the pad; a soft kick under each syllable leaves room for the claps.
    for at in (11.75, 12.15, 12.55):
        place(track, kick(0.3, 50, 120), at, 0.45)

    # YA (12.95): home. A bright C with a glockenspiel sparkle.
    place(track, kick(0.5, 45, 150), 12.95, 0.7)
    for i, n in enumerate(["C5", "E5", "G5", "C6"]):
        place(track, pluck(note(n), 1.6, rng, bright=0.6), 12.95 + i * 0.02, 0.35)
    # The laugh: a bouncing, giggling pluck figure while the three crack up at the taya.
    giggle = ["G5", "E5", "G5", "E5", "A5", "G5", "E5", "C5", "D5", "E5"]
    for i, n in enumerate(giggle):
        place(track, pluck(note(n), 0.35, rng, bright=0.85, decay=0.99), 13.25 + i * 0.105, 0.3)
    for k in range(4):
        place(track, pluck(note(["C3", "G2", "A2", "G2"][k]), 0.5, rng, bright=0.3), 13.2 + k * beat * 0.5, 0.45)
    # The taya squares up (14.3): two low, determined hits, then the lullaby comes back over the close.
    place(track, kick(0.5, 45, 140), 14.3, 0.55)
    place(track, kick(0.5, 45, 140), 15.05, 0.65)
    place(track, pad([note("A2"), note("E3"), note("C4")], 1.6, attack=0.1, release=0.8), 14.3, 0.2)
    for i, n in enumerate(["E5", "D5", "C5", "G4", "C5"]):
        place(track, bell(note(n), 2.4, decay=1.8), 15.6 + i * beat, 0.26)
    place(track, pad([note("C3"), note("G3"), note("E4")], 5.0, attack=0.8, release=2.0), 15.4, 0.2)

    track = room(track, 1.8, 0.25, seed=11)
    env = np.ones_like(track)
    tail = int(SR * 2.0)
    env[-tail:] = np.linspace(1, 0, tail) ** 2
    return fade(track * env, 0.01, 0.2)


def tondo_clap():
    rng = np.random.default_rng(5)
    t = t_of(0.3)
    y = np.zeros_like(t)
    # Two small hands: a few flams of band noise, a body, and the room.
    for k, (at, g) in enumerate([(0.0, 1.0), (0.006, 0.7), (0.013, 0.5)]):
        place(y, noise_hit(0.12, rng, 900, 6000, 38 + k * 6), at, g)
    place(y, noise_hit(0.05, rng, 300, 900, 60), 0.0, 0.5)
    return fade(room(y, 0.4, 0.3, seed=6, tone=5000), 0.0005, 0.05)


def tondo_chalk():
    rng = np.random.default_rng(9)
    t = t_of(2.4)
    # Scritching: grains of bright noise, faster with the run, then the skid.
    grains = np.zeros_like(t)
    at = 0.05
    while at < 1.9:
        place(grains, noise_hit(0.025, rng, 2500, 9000, 120), at, rng.uniform(0.4, 1.0))
        at += 0.045 - 0.015 * min(1, at / 1.2) + rng.uniform(0, 0.01)
    skid = band(rng.uniform(-1, 1, len(t)), 1200, 5000) * np.clip((t - 1.85) / 0.05, 0, 1) * np.exp(-np.clip(t - 1.85, 0, None) * 6)
    return fade(grains + 0.6 * skid, 0.002, 0.08)


def tondo_door():
    rng = np.random.default_rng(13)
    t = t_of(0.9)
    y = np.zeros_like(t)
    place(y, noise_hit(0.04, rng, 1500, 6000, 90), 0.0, 0.8)                    # the latch
    creak_t = t_of(0.4)
    f = 380 + 160 * np.sin(2 * np.pi * 3.5 * creak_t) + 90 * creak_t
    creak = signal.sawtooth(2 * np.pi * np.cumsum(f) / SR) * np.exp(-creak_t * 3)
    place(y, band(creak, 300, 2400) * 0.5, 0.1)                                 # the hinge
    place(y, kick(0.2, 70, 160), 0.5, 0.6)                                     # the bump on its stop
    place(y, noise_hit(0.1, rng, 200, 1200, 40), 0.5, 0.4)
    return fade(room(y, 0.6, 0.25, seed=14), 0.001, 0.08)


def tondo_taya():
    rng = np.random.default_rng(17)
    y = np.zeros(int(SR * 1.6))
    for i, n in enumerate(["C6", "E6", "G6", "C7"]):
        place(y, bell(note(n), 1.2, decay=2.8), i * 0.06, 0.5)
    for n in ["C5", "E5", "G5"]:
        place(y, pluck(note(n), 1.4, rng, bright=0.8), 0.24, 0.35)
    place(y, noise_hit(0.6, rng, 6000, 14000, 6), 0.24, 0.12)
    return fade(room(y, 1.0, 0.3, seed=18), 0.001, 0.15)


# ------------------------------------------------------------------ ERMITA: tense, an underground rivalry
#
# ErmitaFilm: UNDER 0.0, STANDOFF 3.4 (cuts 5.0, 6.1), TRAIN 7.2 (cuts 8.0, 8.55), DROP 9.0 to 9.8, TAYA 10.4,
# CLOSE 12.8, end 16.2. 120 bpm (0.5 s a beat); D minor.

def saw_bass(freq, seconds, cut=420, drive=1.6):
    t = t_of(seconds)
    y = signal.sawtooth(2 * np.pi * freq * t) + 0.5 * signal.sawtooth(2 * np.pi * freq * 1.006 * t)
    y = np.tanh(lowpass(y, cut) * drive)
    return fade(y * np.exp(-t * 1.5), 0.003, 0.04)


def ermita_score():
    rng = np.random.default_rng(2610)
    total = 17.0
    track = np.zeros(int(SR * total))
    beat = 0.5
    # UNDER: a dark drone and a slow pulse, like a heart under concrete.
    place(track, pad([note("D2"), note("A2"), note("F3")], 7.6, attack=1.5, release=1.2, bright=420), 0.0, 0.45)
    for k in range(7):
        place(track, kick(0.4, 40, 90), 0.2 + k * 1.0, 0.5)
        place(track, kick(0.3, 40, 80), 0.45 + k * 1.0, 0.25)
    for k, at in enumerate((0.8, 2.1, 3.0)):
        place(track, bell(note(["A5", "F5", "D6"][k]), 2.5, partials=((1, 1.0), (2.4, 0.5), (5.9, 0.3)), decay=1.4), at, 0.07)
    # STANDOFF: the bass line walks in with them; ticking hats; a snare from the second cut.
    line = ["D2", "D2", "F2", "D2", "C2", "D2", "A1", "C2"]
    for k in range(8):
        at = 3.4 + k * beat
        place(track, saw_bass(note(line[k]), 0.45), at, 0.55)
        place(track, saw_bass(note(line[k]), 0.2), at + beat * 0.5, 0.3)
    for k in range(int((9.0 - 3.4) / (beat / 2))):
        at = 3.4 + k * beat / 2
        place(track, noise_hit(0.05, rng, 6000, 14000, 70), at, 0.10 if k % 2 else 0.16)
    for k in range(5):
        place(track, noise_hit(0.22, rng, 900, 5000, 18), 5.0 + beat + k * beat * 2, 0.32)
        place(track, kick(0.35, 45, 120), 5.0 + k * beat * 2, 0.6)
    # TRAIN: everything gives way to a riser that tightens into the drop; a snare roll over the last bar.
    riser_t = t_of(1.8)
    sweep = band(rng.uniform(-1, 1, len(riser_t)), 400, 9000) * (riser_t / 1.8) ** 2
    tone = np.sin(2 * np.pi * np.cumsum(220 + 660 * (riser_t / 1.8) ** 2) / SR) * (riser_t / 1.8) ** 2
    place(track, sweep * 0.5 + tone * 0.25, 7.2, 0.7)
    for k in range(16):
        place(track, noise_hit(0.12, rng, 900, 5000, 26), 8.2 + k * 0.05, 0.12 + 0.02 * k)
    # DROP: a sub boom and the line again, doubled and driven, a crash.
    place(track, kick(0.9, 32, 160), 9.0, 1.0)
    place(track, noise_hit(1.6, rng, 3000, 14000, 2.2), 9.0, 0.35)
    for k in range(int((10.4 - 9.0) / (beat / 2))):
        at = 9.0 + k * beat / 2
        place(track, saw_bass(note(line[k % 8]), 0.24, cut=900, drive=2.4), at, 0.55)
        if k % 2 == 0:
            place(track, kick(0.3, 42, 140), at, 0.7)
        else:
            place(track, noise_hit(0.15, rng, 1200, 6000, 22), at, 0.3)
    # TAYA: almost nothing; a held high note, thin, and the pulse low under it.
    t = t_of(5.6)
    high = (np.sin(2 * np.pi * note("A5") * t) + 0.3 * np.sin(2 * np.pi * note("A5") * 1.003 * t)) * np.minimum(1, t / 1.2) * np.exp(-t * 0.25)
    place(track, high, 10.4, 0.08)
    place(track, pad([note("D2"), note("A2")], 6.6, attack=0.6, release=2.0, bright=300), 10.4, 0.35)
    for k in range(6):
        place(track, kick(0.4, 40, 85), 10.9 + k * 1.0, 0.35)
    track = room(track, 2.2, 0.28, seed=21, tone=2400)
    tail = int(SR * 2.0)
    track[-tail:] *= np.linspace(1, 0, tail) ** 2
    return fade(track, 0.01, 0.2)


def ermita_horn():
    t = t_of(2.2)
    y = np.zeros_like(t)
    for f, at, length in ((311.0, 0.0, 0.7), (277.0, 0.75, 1.1)):
        tt = t_of(length)
        tone = sum(np.sin(2 * np.pi * f * k * tt) / k for k in (1, 2, 3, 4)) * np.minimum(1, tt / 0.05) * np.minimum(1, (length - tt) / 0.15)
        place(y, lowpass(tone, 1800), at, 0.6)
    return fade(room(lowpass(y, 1400), 1.8, 0.55, seed=22, tone=1500), 0.01, 0.2)


def ermita_drop():
    rng = np.random.default_rng(23)
    y = np.zeros(int(SR * 2.0))
    place(y, kick(1.4, 28, 120), 0.0, 1.0)
    place(y, noise_hit(0.08, rng, 1500, 9000, 40), 0.0, 0.7)
    ring_t = t_of(1.8)
    ring = sum(np.sin(2 * np.pi * f * ring_t) for f in (523.0, 787.0, 1210.0)) * np.exp(-ring_t * 2.2)
    place(y, ring * 0.25, 0.02)
    return fade(room(y, 1.2, 0.3, seed=24), 0.001, 0.2)


def ermita_buzz():
    rng = np.random.default_rng(25)
    t = t_of(1.4)
    hum = (signal.square(2 * np.pi * 120 * t) * 0.3 + np.sin(2 * np.pi * 60 * t)) * 0.5
    gate = np.zeros_like(t)
    for a, b in ((0.0, 0.08), (0.2, 0.27), (0.42, 1.4)):
        gate[int(a * SR):int(b * SR)] = 1.0
    gate = lowpass(gate, 200)
    y = band(hum, 80, 3000) * gate * np.where(t > 0.42, np.exp(-(t - 0.42) * 1.2), 1.0)
    for at in (0.0, 0.2, 0.42):
        place(y, noise_hit(0.03, rng, 2000, 9000, 120), at, 0.8)
    return fade(y, 0.001, 0.15)


def ermita_step():
    rng = np.random.default_rng(27)
    y = np.zeros(int(SR * 1.0))
    place(y, kick(0.6, 38, 110), 0.0, 0.9)
    t = t_of(0.9)
    strings = sum(signal.sawtooth(2 * np.pi * f * t) for f in (note("D3"), note("D#3"))) * np.exp(-t * 2.5)
    place(y, band(strings, 200, 2500) * 0.3, 0.02)
    return fade(room(y, 1.0, 0.3, seed=28), 0.001, 0.12)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--only", default="")
    only = parser.parse_args().only
    write("sfx_film_tondo_score.wav", tondo_score(), only)
    write("sfx_film_tondo_clap.wav", tondo_clap(), only)
    write("sfx_film_tondo_chalk.wav", tondo_chalk(), only)
    write("sfx_film_tondo_door.wav", tondo_door(), only)
    write("sfx_film_tondo_taya.wav", tondo_taya(), only)
    write("sfx_film_ermita_score.wav", ermita_score(), only)
    write("sfx_film_ermita_horn.wav", ermita_horn(), only)
    write("sfx_film_ermita_drop.wav", ermita_drop(), only)
    write("sfx_film_ermita_buzz.wav", ermita_buzz(), only)
    write("sfx_film_ermita_step.wav", ermita_step(), only)


if __name__ == "__main__":
    main()
