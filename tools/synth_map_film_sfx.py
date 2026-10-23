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
    sfx_film_sitangkai_score.wav 19.5 s SITANGKAI: a kulintang over a slow swell, the agung on the lola's point
    sfx_film_sitangkai_motor.wav 5.60 s the banca's pump engine putt-putting in and cutting out
    sfx_film_sitangkai_ground.wav 0.90 s the hull running up the sand: a hiss of sand and a thud
    sfx_film_sitangkai_hop.wav  0.40 s  a bare foot landing in soft sand
    sfx_film_sitangkai_clink.wav 0.70 s the tin can set down on the sand
    sfx_film_sitangkai_gong.wav 3.50 s  the agung: one deep, beating gong stroke
    sfx_film_bgc_score.wav     16.8 s  BGC: a 128 bpm drum line, doubled under the rush, dropped for the billboard
    sfx_film_bgc_brake.wav      1.20 s  tyres standing on the tarmac, a thump as it rocks to a stop
    sfx_film_bgc_horn.wav       0.90 s  a car horn, a long blast and a short one
    sfx_film_bgc_hop.wav        0.35 s  a shoe landing on asphalt
    sfx_film_bgc_skid.wav       1.30 s  a bike tyre skidding sideways, the frame clattering down
    sfx_film_bgc_slam.wav       0.60 s  a billboard card slamming in
    sfx_film_bgc_flip.wav       1.60 s  the taya's card flipping over: a whoosh and a big hit
    sfx_film_bgc_beep.wav       0.60 s  the crossing signal's beep
    sfx_film_bgc_go.wav         1.80 s  green: a boom and a bright stab

The kids' cheer and giggle under Sitangkai are recorded: copies of Art/audio/ambience/sfx_life_cheer_1 and
sfx_life_kid_giggle_1, as Resources/Sfx/sfx_film_sitangkai_kids and _giggle. Nothing here makes a voice.

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


# ------------------------------------------------------------------ SITANGKAI: calm wonder
#
# SitangkaiFilm: GLIDE 0.0, BANCA 3.6 (cuts 5.0, 5.9; aground 5.55; hops land 6.45 6.67 6.89 7.11), KIDS 6.9, WALK 8.1,
# LOLA 9.6 (the clink 10.75), LOOK 11.0, POINT 12.0, LAUGH 12.9, TAYA 13.7, CLOSE 15.2, end 18.6. The kulintang of
# Sulu and Tawi-Tawi: gong-chimes on a pentatonic row, the agung low under them, the dabakan drum. 96 bpm (0.625 s).

KULINTANG = ["D5", "E5", "F#5", "A5", "B5", "D6", "E6", "F#6"]


def gong_chime(freq, seconds=1.4, bright=1.0):
    """A kulintang kettle: a bossed gong, its hum slightly sharp of the octave, struck with a soft beater."""
    t = t_of(seconds)
    y = (np.sin(2 * np.pi * freq * t) * np.exp(-t * 2.4)
         + 0.35 * bright * np.sin(2 * np.pi * freq * 2.02 * t) * np.exp(-t * 4.5)
         + 0.12 * bright * np.sin(2 * np.pi * freq * 3.1 * t) * np.exp(-t * 8.0))
    y *= 1 + 0.15 * np.sin(2 * np.pi * 5.5 * t) * np.exp(-t * 2)
    return fade(y, 0.002, 0.08)


def agung(freq=58.0, seconds=3.5, gain=1.0, seed=31):
    """The agung: a big bossed gong, two close fundamentals beating, a hum an octave up, the mallet's thump."""
    rng = np.random.default_rng(seed)
    t = t_of(seconds)
    y = (np.sin(2 * np.pi * freq * t) + 0.8 * np.sin(2 * np.pi * freq * 1.012 * t)) * np.exp(-t * 0.9)
    y += 0.45 * np.sin(2 * np.pi * freq * 2.0 * t + 0.4) * np.exp(-t * 1.4)
    y += 0.2 * np.sin(2 * np.pi * freq * 2.9 * t) * np.exp(-t * 2.6)
    y += lowpass(rng.uniform(-1, 1, len(t)), 300) * np.exp(-t * 30) * 0.8
    return fade(y * gain, 0.003, 0.3)


def dabakan(seconds=0.3, seed=33, pitch=1.0):
    rng = np.random.default_rng(seed)
    t = t_of(seconds)
    body = np.sin(2 * np.pi * np.cumsum(120 * pitch + 90 * np.exp(-t * 30)) / SR) * np.exp(-t * 14)
    skin = band(rng.uniform(-1, 1, len(t)), 400, 3000) * np.exp(-t * 40)
    return fade(body + 0.35 * skin, 0.0005, 0.03)


def swell(seconds, seed=35):
    """The sea under it all: slow breaking swells of low-passed noise."""
    rng = np.random.default_rng(seed)
    t = t_of(seconds)
    n = lowpass(rng.normal(0, 1, len(t)), 700)
    env = 0.55 + 0.45 * np.sin(2 * np.pi * t / 4.2) ** 2
    return n * env


def sitangkai_score():
    rng = np.random.default_rng(1023)
    total = 19.5
    track = np.zeros(int(SR * total))
    beat = 0.625
    k = {n: note(n) for n in KULINTANG}
    place(track, swell(total) * np.minimum(1, t_of(total) / 1.5), 0.0, 0.10)
    # GLIDE: an airy D chord with a shimmer; the kulintang alone, slow, as the sun sits on the water.
    place(track, pad([note("D3"), note("A3"), note("F#4"), note("A4")], 9.8, attack=1.6, release=1.2, bright=1300), 0.0, 0.22)
    for n, at in [("A5", 0.3), ("F#5", 0.9), ("E5", 1.5), ("D5", 2.1), ("E5", 2.55), ("A5", 2.85), ("B5", 3.2)]:
        place(track, gong_chime(k[n], 2.0, 0.7), at, 0.34)
    place(track, agung(55.0, 3.5, 0.6), 0.0, 0.25)
    # BANCA: the ensemble comes in, a rolling eighth-note figure, the dabakan on the beats.
    figure = ["D5", "F#5", "A5", "F#5", "E5", "A5", "B5", "A5"]
    for i in range(int((9.6 - 3.6) / (beat / 2))):
        at = 3.6 + i * beat / 2
        if 5.45 < at < 5.95:
            continue   # the ground and the lurch get their own space
        place(track, gong_chime(k[figure[i % 8]], 0.9), at, 0.24 + (0.06 if i % 2 == 0 else 0.0))
        if i % 2 == 0:
            place(track, dabakan(0.3, 33 + i, 1.0 if i % 4 == 0 else 1.25), at, 0.32)
        if i % 4 == 3:
            place(track, noise_hit(0.07, rng, 3000, 9000, 60), at, 0.08)
    for at in (3.6, 6.1, 8.6):
        place(track, agung(58.0, 2.6, 0.7, seed=int(at * 10)), at, 0.3)
    # The grounding: a drum and a low gong together.
    place(track, dabakan(0.5, 40, 0.7), 5.55, 0.6)
    place(track, agung(52.0, 2.0, 0.8, seed=41), 5.55, 0.35)
    # The hops: a rising chime on each landing.
    for i, at in enumerate((6.45, 6.67, 6.89, 7.11)):
        place(track, gong_chime(k[["D6", "E6", "F#6", "A5"][i]] * (2 if i == 3 else 1), 1.0), at, 0.3)
    # KIDS: a bright run up the row, and the babendil's clank on every beat.
    for i, n in enumerate(KULINTANG):
        place(track, gong_chime(k[n], 0.9, 1.0), 6.9 + i * 0.06, 0.24)
    for i in range(4):
        place(track, bell(note("A6"), 0.25, partials=((1, 1.0), (2.3, 0.6), (3.7, 0.4)), decay=18), 6.9 + i * beat, 0.12)
    place(track, pad([note("G3"), note("D4"), note("B4")], 2.6, attack=0.3, release=0.8, bright=1500), 6.9, 0.14)
    place(track, pad([note("D3"), note("A3"), note("F#4")], 2.0, attack=0.3, release=0.8, bright=1500), 8.1, 0.14)
    # LOLA: a hush; single chimes as she comes; the pad holds.
    place(track, pad([note("B2"), note("F#3"), note("D4")], 5.2, attack=0.5, release=1.2, bright=800), 9.6, 0.22)
    for at, n in ((9.75, "F#5"), (10.25, "E5"), (10.75, "D5")):
        place(track, gong_chime(k[n], 1.6, 0.6), at, 0.28)
    # LOOK: two chimes trading, faster and faster, into her point.
    at, gap, i = 11.0, 0.34, 0
    while at < 11.95:
        place(track, gong_chime(k["A5" if i % 2 else "B5"], 0.6), at, 0.12 + 0.18 * (at - 11.0))
        at += gap
        gap = max(0.06, gap * 0.82)
        i += 1
    # POINT (12.0): the agung, full; a flourish down the row.
    place(track, agung(49.0, 4.5, 1.0, seed=51), 12.0, 0.65)
    place(track, dabakan(0.5, 52, 0.6), 12.0, 0.6)
    for i, n in enumerate(reversed(KULINTANG)):
        place(track, gong_chime(k[n], 0.9), 12.05 + i * 0.07, 0.24)
    # LAUGH (12.9): a bouncing, giggling figure.
    giggle = ["F#6", "D6", "F#6", "D6", "A5", "B5", "A5", "F#5"]
    for i, n in enumerate(giggle):
        place(track, gong_chime(k[n], 0.4, 1.0), 12.9 + i * 0.1, 0.24)
        if i % 2 == 0:
            place(track, dabakan(0.2, 60 + i, 1.4), 12.9 + i * 0.1, 0.2)
    # TAYA (13.7): two determined low strokes, then the slow tune back over the close.
    place(track, agung(46.0, 2.2, 0.9, seed=61), 13.7, 0.5)
    place(track, dabakan(0.5, 62, 0.6), 13.7, 0.5)
    place(track, agung(46.0, 2.2, 0.9, seed=63), 14.45, 0.55)
    place(track, dabakan(0.5, 64, 0.6), 14.45, 0.55)
    for n, at in [("A5", 15.2), ("F#5", 15.8), ("E5", 16.4), ("D5", 17.0), ("A5", 17.6)]:
        place(track, gong_chime(k[n], 2.2, 0.6), at, 0.3)
    place(track, pad([note("D3"), note("A3"), note("F#4")], 4.3, attack=0.8, release=2.0, bright=1100), 15.2, 0.22)
    track = room(track, 2.0, 0.26, seed=37, tone=4200)
    tail = int(SR * 1.8)
    track[-tail:] *= np.linspace(1, 0, tail) ** 2
    return fade(track, 0.01, 0.2)


def sitangkai_motor():
    """A pump boat's two-stroke: low pops about eleven a second, coming closer, then cut as it grounds."""
    rng = np.random.default_rng(71)
    seconds = 5.6
    y = np.zeros(int(SR * seconds))
    at = 0.0
    pop_t = t_of(0.06)
    while at < 5.35:
        pop = lowpass(rng.uniform(-1, 1, len(pop_t)), 500) * np.exp(-pop_t * 60)
        pop += np.sin(2 * np.pi * 70 * pop_t) * np.exp(-pop_t * 45) * 0.6
        place(y, pop, at, rng.uniform(0.8, 1.0))
        at += 1.0 / (10.5 + 1.2 * np.sin(at * 0.9)) + rng.uniform(-0.004, 0.004)
    t = t_of(seconds)
    near = np.clip(0.35 + 0.65 * t / 4.0, 0, 1) * np.where(t > 5.0, np.clip(1 - (t - 5.0) / 0.35, 0, 1), 1)
    return fade(lowpass(y, 1400) * near, 0.05, 0.2)


def sitangkai_ground():
    rng = np.random.default_rng(73)
    t = t_of(0.9)
    y = band(rng.uniform(-1, 1, len(t)), 500, 4500) * np.clip(t / 0.03, 0, 1) * np.exp(-t * 5) * 0.6
    place(y, kick(0.45, 40, 110), 0.12, 0.9)
    place(y, noise_hit(0.2, rng, 150, 900, 20), 0.12, 0.6)
    return fade(room(y, 0.5, 0.2, seed=74), 0.001, 0.1)


def sitangkai_hop():
    rng = np.random.default_rng(75)
    y = np.zeros(int(SR * 0.4))
    place(y, noise_hit(0.25, rng, 200, 2400, 22), 0.0, 0.8)
    place(y, kick(0.2, 60, 130), 0.0, 0.5)
    return fade(y, 0.0005, 0.06)


def sitangkai_clink():
    rng = np.random.default_rng(77)
    y = np.zeros(int(SR * 0.7))
    place(y, bell(1830.0, 0.6, partials=((1, 1.0), (1.47, 0.6), (2.31, 0.45), (3.9, 0.2)), decay=9), 0.0, 0.55)
    place(y, noise_hit(0.12, rng, 200, 1500, 35), 0.0, 0.6)
    return fade(room(y, 0.4, 0.15, seed=78), 0.0005, 0.08)


def sitangkai_gong():
    return agung(49.0, 3.5, 1.0, seed=79)


# ------------------------------------------------------------------ BGC: city adrenaline
#
# BgcFilm: RUSH 0.0 (cuts 0.8 1.6 2.4, the title 2.5), JEEP 3.2, BIKE 4.8, FOOT 5.9 (the brake 6.15, the horn 6.35),
# GATHER 7.0, BOARD 8.4 (cards 8.7 8.9 9.1, VS 9.35), the FLIP 9.8, LAUGH 10.2, AMBER 10.8, GREEN 11.3, CROUCH 11.6,
# CLOSE 12.4, end 15.8. 128 bpm (0.469 s a beat), E minor: four on the floor, a driving bass, bright stabs.

def bgc_bass(freq, seconds, cut=700, drive=2.0):
    t = t_of(seconds)
    y = signal.square(2 * np.pi * freq * t, 0.4) + 0.6 * signal.sawtooth(2 * np.pi * freq * 1.005 * t)
    y = np.tanh(lowpass(y, cut, 3) * drive)
    return fade(y * np.exp(-t * 3.0), 0.002, 0.03)


def bgc_stab(freqs, seconds=0.35, bright=3200):
    t = t_of(seconds)
    y = sum(signal.sawtooth(2 * np.pi * f * (1 + d) * t) for f in freqs for d in (-0.006, 0.0, 0.007))
    y = lowpass(y / (3 * len(freqs)), bright, 2)
    return fade(y * np.exp(-t * 7.0), 0.002, 0.05)


def snare(seconds, rng, gain=1.0):
    t = t_of(seconds)
    body = np.sin(2 * np.pi * 190 * t) * np.exp(-t * 30)
    return fade((band(rng.uniform(-1, 1, len(t)), 1200, 8000) * np.exp(-t * 20) + 0.5 * body) * gain, 0.0005, 0.03)


def bgc_score():
    rng = np.random.default_rng(1024)
    total = 16.8
    track = np.zeros(int(SR * total))
    beat = 60.0 / 128.0
    line = ["E2", "E2", "G2", "E2", "D2", "E2", "B1", "D2"]
    # RUSH: hats and kicks at double time over a rising filtered bass, a riser into the drop at 3.2.
    k = 0
    at = 0.0
    while at < 3.2:
        place(track, noise_hit(0.04, rng, 7000, 15000, 90), at, 0.12 + 0.05 * (k % 2 == 0))
        if k % 4 == 0:
            place(track, kick(0.3, 50, 160), at, 0.55)
        if k % 2 == 0:
            place(track, bgc_bass(note(line[(k // 2) % 8]), beat * 0.45, cut=300 + 200 * at), at, 0.4)
        at += beat / 4
        k += 1
    for cut in (0.8, 1.6, 2.4):
        place(track, snare(0.2, rng, 0.9), cut, 0.35)
    rt = t_of(1.0)
    place(track, band(rng.uniform(-1, 1, len(rt)), 500, 10000) * (rt / 1.0) ** 2, 2.2, 0.45)
    # 3.2 to 8.4: the groove. Kick on every beat, snare on 2 and 4, sixteenth hats, the bass, a stab each bar.
    at, n = 3.2, 0
    while at < 8.4:
        place(track, kick(0.32, 48, 150), at, 0.62)
        if n % 2 == 1:
            place(track, snare(0.22, rng), at, 0.4)
        for q in range(4):
            place(track, noise_hit(0.035, rng, 7000, 15000, 100), at + q * beat / 4, 0.1 if q % 2 else 0.14)
        place(track, bgc_bass(note(line[n % 8]), beat * 0.9), at, 0.45)
        place(track, bgc_bass(note(line[n % 8]), beat * 0.4), at + beat / 2, 0.3)
        if n % 4 == 0:
            place(track, bgc_stab([note("E4"), note("G4"), note("B4")]), at + beat * 0.5, 0.3)
        at += beat
        n += 1
    # Accents on the arrivals: the hops, the skid, the horn.
    for a in (4.11, 4.36):
        place(track, bgc_stab([note("B4"), note("E5")], 0.25), a, 0.22)
    place(track, bgc_stab([note("D4"), note("F#4"), note("A4")], 0.5), 4.8, 0.3)
    place(track, bgc_stab([note("C4"), note("E4"), note("G4")], 0.5), 6.35, 0.3)
    # BOARD (8.4): the beat drops out; a ticking hat, a low held note, a tension riser to the flip.
    pt = t_of(1.4)
    place(track, pad([note("E2"), note("B2")], 1.6, attack=0.2, release=0.5, bright=500), 8.4, 0.35)
    for q in range(int(1.4 / (beat / 2))):
        place(track, noise_hit(0.03, rng, 8000, 15000, 120), 8.4 + q * beat / 2, 0.12)
    place(track, np.sin(2 * np.pi * np.cumsum(300 + 900 * (pt / 1.4) ** 2) / SR) * (pt / 1.4) ** 2 * 0.3, 8.4, 0.5)
    # FLIP (9.8): the hit, then the laugh: bouncing blips over a half-time beat.
    place(track, kick(0.7, 38, 170), 9.8, 1.0)
    place(track, bgc_stab([note("E3"), note("B3"), note("E4"), note("G4")], 0.9, 2600), 9.8, 0.5)
    place(track, noise_hit(1.2, rng, 3000, 14000, 3), 9.8, 0.25)
    blips = ["E5", "G5", "E5", "B5", "A5", "G5", "E5", "D5"]
    for i, b in enumerate(blips):
        place(track, bell(note(b), 0.25, partials=((1, 1.0), (2.0, 0.3)), decay=14), 10.2 + i * beat / 2, 0.18)
    for q in range(2):
        place(track, kick(0.3, 48, 140), 10.2 + q * beat * 2, 0.45)
        place(track, snare(0.2, rng), 10.2 + beat + q * beat * 2, 0.3)
    # AMBER (10.8): everything holds its breath; only the signal's beep (a cue) and a low hum.
    place(track, pad([note("E2")], 0.6, attack=0.05, release=0.3, bright=300), 10.8, 0.3)
    # GO (11.3): the full drop, harder than before, into the close.
    at, n = 11.3, 0
    while at < 15.0:
        place(track, kick(0.32, 46, 160), at, 0.7 if at < 12.4 else 0.5)
        if n % 2 == 1:
            place(track, snare(0.22, rng), at, 0.45 if at < 12.4 else 0.3)
        for q in range(4):
            place(track, noise_hit(0.035, rng, 7000, 15000, 100), at + q * beat / 4, 0.12)
        place(track, bgc_bass(note(line[n % 8]), beat * 0.9, cut=1100, drive=2.6), at, 0.5)
        if n % 2 == 0:
            place(track, bgc_stab([note("E4"), note("G4"), note("B4"), note("E5")], 0.4), at + beat * 0.5, 0.32)
        at += beat
        n += 1
    place(track, kick(0.8, 36, 180), 11.3, 0.9)
    place(track, noise_hit(1.4, rng, 3000, 14000, 2.5), 11.3, 0.3)
    track = room(track, 0.9, 0.16, seed=81, tone=5000)
    tail = int(SR * 1.8)
    track[-tail:] *= np.linspace(1, 0, tail) ** 2
    return fade(track, 0.005, 0.2)


def bgc_brake():
    """Tyres standing on the tarmac: a squeal sliding down, rough under it, and a thump as it rocks to a stop."""
    rng = np.random.default_rng(83)
    t = t_of(1.2)
    f = 1900 - 700 * t / 1.2
    squeal = np.sin(2 * np.pi * np.cumsum(f) / SR) * (1 + 0.3 * np.sin(2 * np.pi * 31 * t))
    rough = band(rng.uniform(-1, 1, len(t)), 600, 4000)
    env = np.clip(t / 0.04, 0, 1) * np.clip((1.0 - t) / 0.25, 0, 1)
    y = (0.5 * squeal + 0.5 * rough) * env
    place(y, kick(0.3, 50, 120), 0.95, 0.5)
    return fade(y, 0.002, 0.08)


def bgc_horn():
    """A car horn: two square tones a third apart, one long blast and a short one."""
    y = np.zeros(int(SR * 0.9))
    for at, length in ((0.0, 0.42), (0.52, 0.28)):
        tt = t_of(length)
        tone = (signal.square(2 * np.pi * 415 * tt) + signal.square(2 * np.pi * 523 * tt)) * np.minimum(1, tt / 0.01) * np.minimum(1, (length - tt) / 0.02)
        place(y, band(tone, 300, 3200), at, 0.5)
    return fade(y, 0.002, 0.05)


def bgc_hop():
    rng = np.random.default_rng(85)
    y = np.zeros(int(SR * 0.35))
    place(y, kick(0.2, 70, 160), 0.0, 0.7)
    place(y, noise_hit(0.12, rng, 800, 5000, 40), 0.0, 0.6)
    return fade(y, 0.0005, 0.05)


def bgc_skid():
    """A bike tyre skidding sideways on asphalt, then the frame clattering down."""
    rng = np.random.default_rng(87)
    t = t_of(1.3)
    skid = band(rng.uniform(-1, 1, len(t)), 1500, 7000) * np.clip(t / 0.03, 0, 1) * np.clip((0.55 - t) / 0.2, 0, 1)
    y = skid * 0.7
    for i, at in enumerate((0.72, 0.8, 0.86, 0.95)):
        place(y, bell(900 + 300 * i, 0.3, partials=((1, 1.0), (1.6, 0.6), (2.7, 0.4)), decay=16), at, 0.3)
        place(y, noise_hit(0.08, rng, 1500, 8000, 50), at, 0.4)
    return fade(y, 0.001, 0.08)


def bgc_slam():
    rng = np.random.default_rng(89)
    y = np.zeros(int(SR * 0.6))
    wt = t_of(0.15)
    place(y, band(rng.uniform(-1, 1, len(wt)), 800, 7000) * (wt / 0.15) ** 2, 0.0, 0.5)
    place(y, kick(0.4, 45, 170), 0.15, 0.9)
    place(y, noise_hit(0.15, rng, 2000, 9000, 30), 0.15, 0.4)
    return fade(y, 0.001, 0.08)


def bgc_flip():
    rng = np.random.default_rng(91)
    y = np.zeros(int(SR * 1.6))
    wt = t_of(0.3)
    place(y, band(rng.uniform(-1, 1, len(wt)), 600, 9000) * (wt / 0.3) ** 1.5, 0.0, 0.55)
    place(y, kick(0.9, 34, 180), 0.0, 1.0)
    place(y, bgc_stab([note("E4"), note("G4"), note("B4"), note("E5")], 1.2, 3500), 0.0, 0.6)
    return fade(room(y, 1.0, 0.25, seed=92), 0.001, 0.2)


def bgc_beep():
    y = np.zeros(int(SR * 0.6))
    for at in (0.0, 0.25):
        tt = t_of(0.12)
        place(y, signal.square(2 * np.pi * 2300 * tt) * np.minimum(1, tt / 0.004) * np.minimum(1, (0.12 - tt) / 0.01), at, 0.35)
    return fade(lowpass(y, 6000), 0.001, 0.05)


def bgc_go():
    rng = np.random.default_rng(93)
    y = np.zeros(int(SR * 1.8))
    place(y, kick(1.0, 32, 200), 0.0, 1.0)
    place(y, bgc_stab([note("E3"), note("B3"), note("E4"), note("G#4"), note("B4")], 1.4, 4500), 0.0, 0.7)
    place(y, noise_hit(1.4, rng, 3000, 15000, 2.4), 0.0, 0.35)
    return fade(room(y, 1.2, 0.25, seed=94), 0.001, 0.2)


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
    write("sfx_film_sitangkai_score.wav", sitangkai_score(), only)
    write("sfx_film_sitangkai_motor.wav", sitangkai_motor(), only)
    write("sfx_film_sitangkai_ground.wav", sitangkai_ground(), only)
    write("sfx_film_sitangkai_hop.wav", sitangkai_hop(), only)
    write("sfx_film_sitangkai_clink.wav", sitangkai_clink(), only)
    write("sfx_film_sitangkai_gong.wav", sitangkai_gong(), only)
    write("sfx_film_bgc_score.wav", bgc_score(), only)
    write("sfx_film_bgc_brake.wav", bgc_brake(), only)
    write("sfx_film_bgc_horn.wav", bgc_horn(), only)
    write("sfx_film_bgc_hop.wav", bgc_hop(), only)
    write("sfx_film_bgc_skid.wav", bgc_skid(), only)
    write("sfx_film_bgc_slam.wav", bgc_slam(), only)
    write("sfx_film_bgc_flip.wav", bgc_flip(), only)
    write("sfx_film_bgc_beep.wav", bgc_beep(), only)
    write("sfx_film_bgc_go.wav", bgc_go(), only)


if __name__ == "__main__":
    main()
