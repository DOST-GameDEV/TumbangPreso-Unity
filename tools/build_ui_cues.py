"""Synthesise the four UI cues the reworked login and title screen needed.

⚠️⚠️ THESE ARE GENERATED, NOT SOURCED, AND THAT IS DELIBERATE.
`docs/Asset_Sourcing.md` § 5.5 and `Attention.md` § 13 record that sourced
replacements for cues this game already had were rejected by name, and the
originals had to be restored. Nothing here replaces anything: `ui_click`,
`ui_hover`, `ui_back` and `ui_error` are untouched. These four are new states
that had no sound at all, which is the gap `MenuSfx`'s own header describes
("a converted menu that only plays a click on a successful action is a
regression against a game that already had the whole layer").

    ui_tick     a checkbox taking or losing its mark
    ui_toggle   the SIGN UP / SIGN IN pill sliding across its track
    ui_valid    a field that was wrong becoming right
    ui_start    the title screen letting go

⚠️ MIXED QUIET ON PURPOSE. `AudioCues.Live` carries a trim per cue and these sit
at or under `ui_click`'s, because three of the four fire while the player is
typing. A validation chime as loud as a knockdown is the thing that makes people
turn menu sound off.

16 bit mono 44100 PCM, matching every other file in Resources/Sfx.
"""
from pathlib import Path
import struct
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / 'Assets/TumbangPreso/Resources/Sfx'
RATE = 44100


def envelope(n, attack, decay, power=2.0):
    t = np.arange(n) / RATE
    rise = np.clip(t / max(attack, 1e-5), 0, 1)
    fall = np.exp(-t / max(decay, 1e-5)) ** (1 / power)
    return rise * fall


def tone(seconds, frequency, decay, attack=.002, drift=0.0, shape=np.sin):
    n = int(seconds * RATE)
    t = np.arange(n) / RATE
    phase = 2 * np.pi * (frequency * t + drift * t * t * .5)
    return shape(phase) * envelope(n, attack, decay)


def noise(seconds, decay, low, high, seed):
    n = int(seconds * RATE)
    rng = np.random.default_rng(seed)
    raw = rng.standard_normal(n)
    spectrum = np.fft.rfft(raw)
    frequencies = np.fft.rfftfreq(n, 1 / RATE)
    band = np.exp(-((frequencies - (low + high) / 2) / max((high - low) / 2, 1)) ** 2)
    return np.fft.irfft(spectrum * band, n) * envelope(n, .0005, decay)


def mix(*parts):
    length = max(len(p) for p in parts)
    out = np.zeros(length)
    for p in parts:
        out[:len(p)] += p
    return out


def write(name, samples, peak=.5):
    samples = samples / max(np.abs(samples).max(), 1e-9) * peak
    # ⚠️ A hard start or end is a click in its own right, and a UI cue is short
    # enough that a two millisecond ramp is inaudible as a fade.
    ramp = int(.002 * RATE)
    samples[:ramp] *= np.linspace(0, 1, ramp)
    samples[-ramp:] *= np.linspace(1, 0, ramp)
    data = (np.clip(samples, -1, 1) * 32767).astype('<i2').tobytes()
    header = b'RIFF' + struct.pack('<I', 36 + len(data)) + b'WAVEfmt '
    header += struct.pack('<IHHIIHH', 16, 1, 1, RATE, RATE * 2, 2, 16)
    header += b'data' + struct.pack('<I', len(data))
    path = TARGET / (name + '.wav')
    path.write_bytes(header + data)
    print(f'{path.name}  {len(samples) / RATE * 1000:.0f} ms')


def main():
    TARGET.mkdir(parents=True, exist_ok=True)

    # A fingernail on a checkbox: all transient, no note to speak of.
    write('ui_tick', mix(
        noise(.045, .010, 1800, 4200, 11) * 1.0,
        tone(.045, 1150, .012) * .35), peak=.30)

    # The pill landing in the other half of its track. Wooden, pitched down a
    # little across its own length so it reads as an object moving and settling.
    write('ui_toggle', mix(
        noise(.030, .008, 900, 2600, 23) * .55,
        tone(.110, 430, .045, drift=-900) * .9,
        tone(.110, 860, .025) * .25), peak=.34)

    # Two notes, a fifth apart, for a field that was wrong becoming right. Quiet:
    # it fires while somebody is still typing in the field below it.
    write('ui_valid', mix(
        tone(.20, 1318.5, .085, attack=.004) * .8,
        np.concatenate([np.zeros(int(.055 * RATE)), tone(.22, 1975.5, .095, attack=.004) * .7])),
        peak=.26)

    # The title screen letting go: a soft thump under a rising triad, so it lands
    # as a door opening rather than as a menu beep.
    write('ui_start', mix(
        tone(.24, 150, .075, attack=.001, drift=-60) * .9,
        noise(.30, .075, 700, 5200, 71) * .22,
        tone(.34, 523.25, .16, attack=.006) * .55,
        np.concatenate([np.zeros(int(.045 * RATE)), tone(.34, 659.25, .17, attack=.006) * .48]),
        np.concatenate([np.zeros(int(.090 * RATE)), tone(.36, 783.99, .19, attack=.006) * .42])),
        peak=.52)


if __name__ == '__main__':
    main()
