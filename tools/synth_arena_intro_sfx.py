"""The Arena's opening sounds: the tunnel, the glare, the taya screen, by deterministic synthesis.

  py -3 tools/synth_arena_intro_sfx.py
  py -3 tools/synth_arena_intro_sfx.py --only ring        (any substring of the file names)

Owner, 2026-10-05: "they walk into the bright glare of the stadium lights alongside a drowning out
of the cheers before the glare disappears and the crowd cheers get louder. a screen billboard
flikers through all the characters and then lands on the person playing taya". The film is
Runtime/Map/ArenaIntro.cs. The crowd itself is ArenaCrowdAudio's, muffled from outside by a low
pass on the listener; these are the sounds the opening adds over it. They are played by
ArenaIntro's own AudioSources straight from Resources (no row in Runtime/Audio/AudioCues.cs), on
every peer from the opening's own clock, so none of them is relayed.

Writes these into Assets/TumbangPreso/Resources/Sfx/ (44100 Hz, mono, 16 bit, each normalised to
0.85 peak like every other cue there; the mix level is ArenaIntro's own table):
    sfx_arena_intro_rumble.wav   4.00 s  the tunnel's room: concrete hum and far feet, a seamless loop
    sfx_arena_intro_heart.wav    0.42 s  one heartbeat, "lub-dub", low and soft
    sfx_arena_intro_whoosh.wav   1.80 s  the glare coming: air and a tone rising to the last sample
    sfx_arena_intro_ring.wav     2.60 s  the ears ringing at the white: a thin high tone that fades
    sfx_arena_intro_tick.wav     0.07 s  the screen's shuffle: one relay tick
    sfx_arena_intro_stamp.wav    1.30 s  the screen lands: a slam, a steel ring, a two-note sting

Same house method as synth_arena_show_sfx.py: numpy and scipy only, fixed seeds, no recordings and
nothing downloaded, so a rerun is byte-identical."""
import argparse
import wave
from pathlib import Path

import numpy as np
from scipy import signal

SR = 44100
PEAK = 0.85
OUT = Path(__file__).resolve().parents[1] / "Assets/TumbangPreso/Resources/Sfx"


def t_of(seconds):
    return np.arange(int(SR * seconds)) / SR


def band(x, low, high, order=2):
    b, a = signal.butter(order, [low / (SR / 2), high / (SR / 2)], "band")
    return signal.lfilter(b, a, x)


def lowpass(x, cut, order=2):
    b, a = signal.butter(order, cut / (SR / 2), "low")
    return signal.lfilter(b, a, x)


def highpass(x, cut, order=2):
    b, a = signal.butter(order, cut / (SR / 2), "high")
    return signal.lfilter(b, a, x)


def fade(x, fade_in=0.004, fade_out=0.03):
    """Raised-cosine ends to exact zero, so no cue clicks at either end."""
    n = len(x)
    a = min(n // 2, int(SR * fade_in))
    b = min(n // 2, int(SR * fade_out))
    y = x.copy()
    if a > 0:
        y[:a] *= 0.5 - 0.5 * np.cos(np.pi * np.arange(a) / a)
    if b > 0:
        y[-b:] *= 0.5 + 0.5 * np.cos(np.pi * np.arange(b) / b)
    return y


def place(out, x, at):
    i = int(at * SR)
    n = min(len(x), len(out) - i)
    if n > 0:
        out[i:i + n] += x[:n]


def rumble(rng):
    """The tunnel: low filtered noise, two slow hums a fifth apart, a few dull far footfalls. A
    seamless loop: the noise is filtered round the circle (in the frequency domain) and every
    tone and every slow swell is a whole number of cycles in the 4 s."""
    seconds = 4.0
    t = t_of(seconds)
    n = len(t)
    spectrum = np.fft.rfft(rng.standard_normal(n))
    freq = np.fft.rfftfreq(n, 1.0 / SR)
    shape = (freq / 38.0) ** 2 / (1.0 + (freq / 38.0) ** 2) / (1.0 + (freq / 150.0) ** 4)
    air = np.fft.irfft(spectrum * shape, n)
    air /= np.abs(air).max()
    hum = 0.45 * np.sin(2 * np.pi * 55.0 * t) + 0.25 * np.sin(2 * np.pi * 82.5 * t + 0.7)
    hum *= 0.8 + 0.2 * np.sin(2 * np.pi * 0.5 * t)
    out = air * (0.75 + 0.25 * np.sin(2 * np.pi * 0.25 * t + 1.0)) + 0.35 * hum
    for at in (0.30, 0.86, 1.47, 2.02, 2.61, 3.20):
        tt = t_of(0.16)
        step = np.sin(2 * np.pi * 70.0 * tt) * np.exp(-tt * 34.0) * 0.30
        place(out, fade(step, 0.003, 0.02), at)
    return out


def heart(rng):
    """One beat: two soft low thumps 0.14 s apart, the second smaller and a little higher."""
    out = np.zeros(len(t_of(0.42)))
    for at, f, gain in ((0.0, 52.0, 1.0), (0.14, 62.0, 0.7)):
        tt = t_of(0.24)
        freq = f * (1.0 + 0.5 * np.exp(-tt * 40.0))
        thump = np.sin(2 * np.pi * np.cumsum(freq) / SR) * np.exp(-tt * 20.0) * gain
        place(out, fade(thump, 0.004, 0.04), at)
    return fade(lowpass(out, 220), 0.004, 0.05)


def whoosh(rng):
    """Air rising to the white: band noise whose band climbs, and a tone under it climbing a
    fifth. It is loudest on its last tenth and ends there: the white is where this stops."""
    t = t_of(1.80)
    share = t / t[-1]
    noise = rng.standard_normal(len(t))
    low = band(noise, 180, 700)
    high = band(noise, 900, 5200)
    air = low * (1.0 - share) + high * share ** 1.5
    freq = 110.0 * (1.5 ** share)
    tone = np.sin(2 * np.pi * np.cumsum(freq) / SR) * 0.35 + np.sin(2 * np.pi * np.cumsum(freq * 2.01) / SR) * 0.15
    out = (air / np.abs(air).max() * 0.8 + tone) * share ** 2.2
    return fade(out, 0.05, 0.05)


def ring(rng):
    """The ears ringing: three thin partials near 3.4 kHz beating slowly, up in a tenth of a
    second, then falling away for two. Soft: it sits over a muffled crowd."""
    t = t_of(2.60)
    env = np.clip(t / 0.12, 0.0, 1.0) * np.exp(-np.clip(t - 0.5, 0.0, None) * 1.7)
    out = np.sin(2 * np.pi * 3420.0 * t) + 0.6 * np.sin(2 * np.pi * 3433.0 * t + 1.1) + 0.35 * np.sin(2 * np.pi * 5130.0 * t + 0.4)
    out += 0.12 * highpass(rng.standard_normal(len(t)), 6000)
    return fade(out * env, 0.02, 0.3)


def tick(rng):
    """One shuffle tick: a short relay click with a pitched tail, so a run of them reads as a wheel."""
    t = t_of(0.07)
    click = highpass(rng.standard_normal(len(t)), 2500) * np.exp(-t * 320.0)
    tone = np.sin(2 * np.pi * 1320.0 * t) * np.exp(-t * 70.0) * 0.6
    return fade(click * 0.7 + tone, 0.0008, 0.015)


def stamp(rng):
    """The screen lands: a low slam with a noise crack, a steel ring, then a rising two-note
    sting (a fifth), bright, with a short tail."""
    t = t_of(1.30)
    out = np.zeros(len(t))
    tt = t_of(0.5)
    freq = 46.0 * (1.0 + 2.2 * np.exp(-tt * 30.0))
    slam = np.sin(2 * np.pi * np.cumsum(freq) / SR) * np.exp(-tt * 9.0)
    crack = band(rng.standard_normal(len(tt)), 500, 4200) * np.exp(-tt * 46.0) * 0.8
    place(out, slam + crack, 0.0)
    tr = t_of(0.9)
    steel = sum(np.sin(2 * np.pi * f * tr + p) * g for f, p, g in ((1174.0, 0.0, 0.5), (1760.0, 0.6, 0.32), (2637.0, 1.3, 0.2), (3136.0, 2.1, 0.12)))
    place(out, steel * np.exp(-tr * 5.5) * 0.45, 0.01)
    for at, f in ((0.10, 587.33), (0.24, 880.0)):
        tn = t_of(0.75)
        phase = 2 * np.pi * f * tn
        note = (np.sin(phase) + 0.45 * np.sin(phase * 2.0) + 0.22 * np.sin(phase * 3.0)) * np.exp(-tn * 4.2)
        note *= np.clip(tn / 0.012, 0.0, 1.0)
        place(out, lowpass(note, 5200) * 0.55, at)
    return fade(out, 0.002, 0.2)


SOUNDS = (
    ("sfx_arena_intro_rumble", rumble, 2601),
    ("sfx_arena_intro_heart", heart, 2602),
    ("sfx_arena_intro_whoosh", whoosh, 2603),
    ("sfx_arena_intro_ring", ring, 2604),
    ("sfx_arena_intro_tick", tick, 2605),
    ("sfx_arena_intro_stamp", stamp, 2606),
)

# A loop is not faded at its ends: its first sample follows its last.
LOOPS = ("sfx_arena_intro_rumble",)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", default="")
    args = ap.parse_args()
    OUT.mkdir(parents=True, exist_ok=True)
    for name, build, seed in SOUNDS:
        if args.only and args.only not in name:
            continue
        x = build(np.random.default_rng(seed))
        x = x / np.abs(x).max() * PEAK
        path = OUT / (name + ".wav")
        with wave.open(str(path), "wb") as w:
            w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
            w.writeframes((x * 32767).astype(np.int16).tobytes())
        jump = abs(int(x[0] * 32767) - int(x[-1] * 32767)) if name in LOOPS else 0
        print("%-26s %.2f s  rms %.3f  ends %d %d%s" % (name, len(x) / SR, float(np.sqrt((x ** 2).mean())), int(x[0] * 32767), int(x[-1] * 32767),
                                                       "  loop seam %d" % jump if name in LOOPS else ""))


if __name__ == "__main__":
    main()
