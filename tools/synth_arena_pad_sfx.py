"""The Arena's stage furniture: jump pad, speed pad, the boost it gives, the stamina orb.

  py -3 tools/synth_arena_pad_sfx.py
  py -3 tools/synth_arena_pad_sfx.py --only stamina        (any substring of the file names)

Owner, 2026-10-05: "there needs to be better sfx for the pads, picking up the stamina orb, speed
boost sfx, and more unique jump sfx". Before this the jump pad played the ordinary jump pitched
down, the speed pad played nothing, and the orb played the slipper's pickup. Each now has a sound
of its own, in the map's voice (hover engines, light, glass):

    sfx_arena_pad_jump.wav   0.85 s  the pad fires: a pressure thump, a coil sprung upward (a tone
                                     that leaps two octaves and wobbles as it goes), air after it
    sfx_arena_pad_speed.wav  0.45 s  the pad catches a runner: three quick rising relay blips and
                                     an electric zip
    sfx_arena_boost.wav      1.50 s  the boost itself: a rush of air that climbs, holds and lets
                                     go, with a turbine whine inside it
    sfx_arena_stamina.wav    1.10 s  the orb is taken: a glass strike, three bell notes climbing a
                                     major chord, and a soft breath of sparkle filling up after

Same house method as synth_arena_show_sfx.py: numpy and scipy only, fixed seeds, no recordings and
nothing downloaded, so a rerun is byte-identical. Writes into Assets/TumbangPreso/Resources/Sfx/
(44100 Hz, mono, 16 bit, 0.85 peak); each has a row in Runtime/Audio/AudioCues.cs. NOBODY HAS
LISTENED TO THESE: they are built to a description, by something that cannot hear."""
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


def lowpass(x, cut, order=2):
    b, a = signal.butter(order, cut / (SR / 2), "low")
    return signal.lfilter(b, a, x)


def highpass(x, cut, order=2):
    b, a = signal.butter(order, cut / (SR / 2), "high")
    return signal.lfilter(b, a, x)


def fade(x, fade_in=0.003, fade_out=0.03):
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


def tone(freq, t):
    """A sine whose frequency is an array: the phase is its running sum."""
    return np.sin(2 * np.pi * np.cumsum(freq) / SR)


def swept_noise(rng, t, centre, width=0.35):
    """Noise through a band whose centre moves (an array, Hz): a state-variable filter, sample by sample."""
    x = rng.standard_normal(len(t))
    out = np.zeros(len(t))
    low = band = 0.0
    q = width
    for i in range(len(t)):
        f = 2.0 * np.sin(np.pi * min(centre[i], SR / 6.5) / SR)
        low += f * band
        high = x[i] - low - q * band
        band += f * high
        out[i] = band
    return out


def pad_jump(rng):
    t = t_of(0.85)
    out = np.zeros(len(t))
    # The pressure thump under the feet.
    tt = t_of(0.22)
    thump = tone(58.0 + 150.0 * np.exp(-tt * 55.0), tt) * np.exp(-tt * 17.0)
    place(out, fade(thump, 0.002, 0.04) * 1.0, 0.0)
    # The coil: two octaves up in a fifth of a second, wobbling as a spring does, with a fifth above it.
    tt = t_of(0.55)
    rise = 190.0 * 2.0 ** (2.1 * (1.0 - np.exp(-tt * 13.0)))
    wobble = 1.0 + 0.055 * np.sin(2 * np.pi * 31.0 * tt) * np.exp(-tt * 7.0)
    coil = tone(rise * wobble, tt) + 0.45 * tone(rise * wobble * 1.5, tt) + 0.2 * tone(rise * wobble * 2.0, tt)
    coil *= np.exp(-tt * 6.5) * (1.0 - np.exp(-tt * 260.0))
    place(out, fade(lowpass(coil, 6500), 0.002, 0.08) * 0.62, 0.012)
    # Air drawn up after the body.
    tt = t_of(0.7)
    air = swept_noise(rng, tt, 700.0 + 5200.0 * (tt / 0.7) ** 0.7)
    air *= np.sin(np.pi * np.clip(tt / 0.7, 0, 1)) ** 1.5 * np.exp(-tt * 2.2)
    place(out, fade(air, 0.01, 0.15) * 0.16, 0.05)
    # A small glass ping at the top of the spring: the pad's own signature.
    tt = t_of(0.3)
    ping = (np.sin(2 * np.pi * 1760.0 * tt) + 0.5 * np.sin(2 * np.pi * 2637.0 * tt)) * np.exp(-tt * 16.0)
    place(out, fade(ping, 0.002, 0.05) * 0.16, 0.19)
    return fade(out, 0.002, 0.12)


def pad_speed(rng):
    t = t_of(0.45)
    out = np.zeros(len(t))
    # Three relay blips, each a fourth above the last, closing up.
    for at, f in ((0.0, 660.0), (0.055, 880.0), (0.10, 1174.7)):
        tt = t_of(0.07)
        blip = (np.sign(np.sin(2 * np.pi * f * tt)) * 0.35 + np.sin(2 * np.pi * f * tt)) * np.exp(-tt * 42.0)
        place(out, fade(lowpass(blip, 5200), 0.001, 0.015) * 0.55, at)
    # The zip: a saw climbing fast, through a band that climbs with it.
    tt = t_of(0.32)
    f = 240.0 * 2.0 ** (3.6 * (tt / 0.32) ** 1.4)
    phase = np.cumsum(f) / SR
    saw = 2.0 * (phase - np.floor(phase + 0.5))
    zip_ = lowpass(saw, 7000) * (tt / 0.32) ** 0.6 * np.exp(-((tt - 0.26) / 0.06) ** 2 * (tt > 0.26))
    place(out, fade(zip_, 0.004, 0.03) * 0.42, 0.11)
    tt = t_of(0.3)
    hiss = swept_noise(rng, tt, 1500.0 + 7000.0 * (tt / 0.3)) * (tt / 0.3) * np.exp(-((tt - 0.24) / 0.05) ** 2 * (tt > 0.24))
    place(out, fade(hiss, 0.004, 0.03) * 0.14, 0.12)
    return fade(out, 0.001, 0.05)


def boost(rng):
    seconds = 1.5
    t = t_of(seconds)
    # The shape of the rush: up in 0.18 s, held, away by the end.
    shape = np.clip(t / 0.18, 0, 1) ** 0.7 * np.clip((seconds - t) / 0.85, 0, 1) ** 1.6
    centre = 500.0 + 2600.0 * shape + 300.0 * np.sin(2 * np.pi * 5.0 * t) * shape
    wind = swept_noise(rng, t, centre, 0.55) * shape
    low = lowpass(rng.standard_normal(len(t)), 220) * shape * 3.0
    # The turbine inside it: a thin whine that climbs with the rush and falls as it goes.
    f = 520.0 + 900.0 * shape
    whine = (tone(f, t) + 0.4 * tone(f * 2.01, t)) * shape ** 2
    out = 0.55 * wind / max(1e-9, np.abs(wind).max()) + 0.22 * low / max(1e-9, np.abs(low).max()) + 0.13 * whine
    # A flutter on the whole, as air past the ears.
    out *= 0.88 + 0.12 * np.sin(2 * np.pi * 17.0 * t + 0.4)
    return fade(highpass(out, 70), 0.004, 0.25)


def stamina(rng):
    t = t_of(1.1)
    out = np.zeros(len(t))
    # The strike: a tick of glass.
    tt = t_of(0.03)
    strike = highpass(rng.standard_normal(len(tt)), 3500) * np.exp(-tt * 190.0)
    place(out, strike * 0.30, 0.0)
    # Three bell notes up a major chord (E5, G#5, B5), then the octave held: each with a bell's inharmonic partials.
    for at, f, gain, decay in ((0.0, 659.3, 0.9, 7.0), (0.075, 830.6, 0.85, 7.0), (0.15, 987.8, 0.9, 6.0), (0.235, 1318.5, 1.0, 3.6)):
        tt = t_of(0.85)
        note = (np.sin(2 * np.pi * f * tt)
                + 0.42 * np.sin(2 * np.pi * f * 2.76 * tt) * np.exp(-tt * 9.0)
                + 0.22 * np.sin(2 * np.pi * f * 5.40 * tt) * np.exp(-tt * 16.0)
                + 0.30 * np.sin(2 * np.pi * f * 2.0 * tt + 0.3))
        note *= np.exp(-tt * decay) * (1.0 - np.exp(-tt * 900.0))
        place(out, fade(note, 0.001, 0.1) * 0.30 * gain, at)
    # The fill: a breath of sparkle rising, as the bar comes back.
    tt = t_of(0.75)
    sparkle = swept_noise(rng, tt, 2500.0 + 6500.0 * (tt / 0.75) ** 0.8, 0.25)
    sparkle *= np.sin(np.pi * np.clip(tt / 0.75, 0, 1)) ** 1.3 * (0.75 + 0.25 * np.sin(2 * np.pi * 23.0 * tt))
    place(out, fade(sparkle, 0.02, 0.2) * 0.07, 0.2)
    # A warm fifth under it, so it reads as something gained and not only a ping.
    tt = t_of(0.7)
    warm = (np.sin(2 * np.pi * 329.6 * tt) + 0.6 * np.sin(2 * np.pi * 493.9 * tt)) * np.sin(np.pi * np.clip(tt / 0.7, 0, 1)) ** 2
    place(out, warm * 0.10, 0.12)
    return fade(lowpass(out, 11000), 0.001, 0.2)


SOUNDS = (
    ("sfx_arena_pad_jump", pad_jump, 2701),
    ("sfx_arena_pad_speed", pad_speed, 2702),
    ("sfx_arena_boost", boost, 2703),
    ("sfx_arena_stamina", stamina, 2704),
)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", default="")
    args = ap.parse_args()
    OUT.mkdir(parents=True, exist_ok=True)
    for name, build, seed in SOUNDS:
        if args.only and args.only not in name:
            continue
        x = build(np.random.default_rng(seed))
        x = x - x.mean()
        # Driven into a soft clip (owner, 2026-10-05: "pads sfx arent too audible"): the body of each sound comes up
        # about 6 dB under the same peak.
        x = np.tanh(x / np.abs(x).max() * 2.6)
        x = x / np.abs(x).max() * PEAK
        with wave.open(str(OUT / (name + ".wav")), "wb") as w:
            w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
            w.writeframes((x * 32767).astype(np.int16).tobytes())
        loud = x[np.abs(x) > 0.02]
        print("%-22s %.2f s  rms %.3f  ends %d %d  finite %s" % (name, len(x) / SR, float(np.sqrt((x ** 2).mean())),
                                                                int(x[0] * 32767), int(x[-1] * 32767), bool(np.isfinite(x).all() and len(loud) > 0)))


if __name__ == "__main__":
    main()
