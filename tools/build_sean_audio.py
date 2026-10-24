"""Sean's (Rago's) sounds, DRAFTS for the owner's ear (`docs/reports/sean-kit-2026-10-24/plan.md` section 0, his sound family,
and `feel-and-readability.md`, who hears what).

Skill sounds stay switched off in the game until the owner picks a draft by ear (`AudioCues.ReworkedSkillSfx`), and an
unapproved skill sound has no file in the game. So these are written to the report's `audio-drafts/`; an approved draft is
copied to `Resources/Sfx` and registered then. numpy only, seeded: a rebuild writes identical bytes. Synthesised from scratch
(the 2026-10-01 sound authorisation). No voice anywhere: the motif is an instrument, and his breath is steam, not a person.

    python tools/build_sean_audio.py

HIS INSTRUMENTS (his lava is LIQUID, out of his own body; Dante's is stone, so nothing here grinds, chunks or cracks rock):
  bubble    one lava bubble: a thick, low sine whose pitch rises as it grows, ending in a soft plop (Minnaert's rising bubble)
  bubbling  many bubbles at random, the texture of a pot of lava
  splat     a wet impact: a falling sine body under a squelch of band noise with a fast flutter
  sizzle    a drop hitting a hot court: a bright noise flash with a fast decay and a few micro pops (not Dante's seam hiss)
  steam     a soft airy hiss that swells and flutters (his breath, the cooling)
  knuckle   a knuckle crack: two dry clicks with a small bony resonance
  boom      the heaviest blow: a sub falling an octave under a long low noise
  glass     cooling: tiny glassy pings (inharmonic, high) and hair-thin crackles as the skin goes to black glass
  ting      hot metal: three inharmonic partials struck once, for his eyes igniting and a hot can
  whum      a heavy object leaving the hand: a band of low noise under a sine envelope
  mountain  THE MOTIF (the cone's sound, heard when the summit lights, when his eyes ignite, and when he stands Molten): a low
            drone on A1 and its fifth with a slow swell, a steam whistle riding on top. An instrument, never a voice.
Every cue is TRANSIENT / BODY / TAIL, one recipe per cue, never one recipe for two cues.

THREAT RANKING (`feel-and-readability.md` § 1 rule 2, after Overwatch's "play by sound"): a cue that warns somebody it is
coming for them (the fist's skim, the bomb's fall) is written loud and close here; the game plays it louder for that player.

The cutscene's theme (`sfx_ult_theme_sean`) is timed beat for beat to the BUILT stage, `HeroIntroductionScene.Sean.cs`
(`SnCracksIn` .45, `SnSummit` 1.4, `SnClench` 2.25, the run 2.4 to 3.2, `SnIgnite` 3.45, `SnDraw` 4.1, `SnSlam` 5.6,
`SnHome` 6.5) and the table in `tools/author_ultimate_intros.py` (`_sean`: shots at 2.1, 4.3, 5.6; the coil held 4.8 to 5.45;
he stands from 6.25 to 6.65). Its loudness, 0.1 s RMS, is written beside it so the loudest moment is the slam, not the setup.
"""
import json
import os
import wave

import numpy as np

RATE = 44100
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "docs", "reports", "sean-kit-2026-10-24", "audio-drafts")
rng = np.random.default_rng(20261024)


# ---------------------------------------------------------------------------------------------- helpers
def n_of(seconds): return int(round(seconds * RATE))
def t_axis(seconds): return np.arange(n_of(seconds)) / RATE


def band(x, lo, hi, order=4):
    """A soft band-pass by FFT mask (Butterworth-like magnitude), so the edges never ring."""
    if len(x) == 0: return x
    f = np.fft.rfftfreq(len(x), 1 / RATE)
    g = np.ones_like(f)
    if lo > 0: g *= 1 / np.sqrt(1 + (lo / np.maximum(f, 1e-3)) ** (2 * order))
    if hi > 0: g *= 1 / np.sqrt(1 + (f / hi) ** (2 * order))
    return np.fft.irfft(np.fft.rfft(x) * g, len(x))


def noise(seconds): return rng.standard_normal(n_of(seconds))


def decay(seconds, attack, release):
    t = t_axis(seconds)
    a = np.clip(t / max(attack, 1e-4), 0, 1)
    return a * np.exp(-np.maximum(0, t - attack) / max(release, 1e-4))


def place(track, clip, at, gain=1.0):
    i = n_of(at)
    if i >= len(track) or i < 0: return track
    j = min(len(track), i + len(clip))
    track[i:j] += clip[:j - i] * gain
    return track


def fade_tail(x, seconds=0.03):
    k = min(len(x), n_of(seconds))
    if k: x[-k:] *= np.cos(np.linspace(0, np.pi / 2, k)) ** 2
    return x


def norm(x, peak=0.85):
    m = np.max(np.abs(x)) or 1.0
    return x * (peak / m)


def loop_ends(x, seconds=0.2):
    """Crossfade the tail into the head so a loop has no seam."""
    k = n_of(seconds)
    x = x.copy()
    x[:k] = x[:k] * np.linspace(0, 1, k) + x[-k:] * np.linspace(1, 0, k)
    return x[:-k]


# ---------------------------------------------------------------------------------------------- instruments
def bubble(f0=180.0, seconds=0.12, rise=1.8, thick=1.0):
    """One lava bubble: the pitch rises as it swells (thick lava: low and slow), then a soft lowpassed plop."""
    t = t_axis(seconds)
    f = f0 * (1 + rise * t / seconds)
    body = np.sin(2 * np.pi * np.cumsum(f) / RATE) * np.exp(-t / (seconds * 0.45 * thick)) * np.clip(t / 0.004, 0, 1)
    plop = band(noise(seconds), 60, 600) * np.exp(-np.maximum(0, t - seconds * 0.7) / 0.01) * (t > seconds * 0.7) * 0.35
    return body + plop


def bubbling(seconds, per_second=14, lo=110, hi=340, size=1.0):
    """A pot of lava: bubbles at random times, sizes and pitches."""
    x = np.zeros(n_of(seconds))
    for _ in range(int(per_second * seconds)):
        f = rng.uniform(lo, hi)
        d = rng.uniform(.07, .16) * size
        place(x, bubble(f, d, rng.uniform(1.2, 2.4)), rng.uniform(0, max(0.0, seconds - d)), rng.uniform(.35, 1.0))
    return x


def splat(size=1.0, f0=170.0, f1=55.0):
    """A wet impact: a falling sine body under a squelch that flutters."""
    seconds = 0.28 * size + 0.08
    t = t_axis(seconds)
    f = f1 + (f0 - f1) * np.exp(-t / (0.05 * size))
    body = np.sin(2 * np.pi * np.cumsum(f) / RATE) * np.exp(-t / (0.09 * size))
    squelch = band(noise(seconds), 280, 1600) * np.exp(-t / (0.05 * size)) * (0.6 + 0.4 * np.sin(2 * np.pi * 38 * t))
    return body * 0.9 + squelch * 0.75


def sizzle(seconds=0.22, bright=1.0):
    """A drop on a hot court: a bright flash of noise, a quick decay, three micro pops."""
    t = t_axis(seconds)
    flash = band(noise(seconds), 2800 * bright, 10000) * np.exp(-t / (seconds * 0.35)) * np.clip(t / 0.002, 0, 1)
    x = flash * 0.5
    for _ in range(3):
        place(x, band(noise(0.004), 2000, 8000) * np.exp(-t_axis(0.004) / 0.001), rng.uniform(0, seconds * 0.6), rng.uniform(.2, .5))
    return x


def steam(seconds, swell_at=0.5, lo=1400, hi=7000):
    """A soft airy hiss that swells to `swell_at` (a share of its length) and flutters."""
    t = t_axis(seconds)
    shape = np.where(t < seconds * swell_at, (t / (seconds * swell_at)) ** 1.5,
                     np.clip((seconds - t) / (seconds * (1 - swell_at) + 1e-4), 0, 1) ** 1.2)
    flutter = 0.8 + 0.2 * np.sin(2 * np.pi * 7.3 * t) * np.sin(2 * np.pi * 1.1 * t)
    return band(noise(seconds), lo, hi) * shape * flutter * 0.6


def knuckle():
    """A knuckle crack: two dry clicks 18 ms apart with a small bony resonance."""
    x = np.zeros(n_of(0.09))
    for at, g in ((0.0, 1.0), (0.018, 0.7)):
        click = band(noise(0.005), 1500, 6000) * np.exp(-t_axis(0.005) / 0.0012)
        t = t_axis(0.03)
        bone = np.sin(2 * np.pi * 1150 * t) * np.exp(-t / 0.006) * 0.4
        place(x, click, at, g); place(x, bone, at, g)
    return x


def boom(seconds=1.4, f0=64.0, f1=31.0, weight=1.0):
    """The heaviest blow: a sub falling an octave under a long low noise."""
    t = t_axis(seconds)
    f = f1 + (f0 - f1) * np.exp(-t / 0.12)
    sub = np.sin(2 * np.pi * np.cumsum(f) / RATE) * np.exp(-t / (0.35 * weight + 0.15))
    air = band(noise(seconds), 30, 260, 3) * np.exp(-t / (0.5 * weight)) * 1.6
    return sub + air * 0.6


def glass(seconds=0.6, count=9):
    """Cooling: glassy pings, inharmonic and high, and hair-thin crackles, thinning out."""
    x = np.zeros(n_of(seconds))
    for k in range(count):
        at = seconds * (k / count) ** 1.4
        f = rng.uniform(3000, 6200)
        t = t_axis(0.05)
        ping = (np.sin(2 * np.pi * f * t) + 0.5 * np.sin(2 * np.pi * f * 1.61 * t)) * np.exp(-t / 0.012)
        place(x, ping, at, rng.uniform(.15, .4))
        place(x, band(noise(0.003), 4000, 11000), at + 0.004, rng.uniform(.1, .3))
    return x


def ting(f=2100.0, seconds=0.7):
    """Hot metal struck once: three inharmonic partials."""
    t = t_axis(seconds)
    return (np.sin(2 * np.pi * f * t) * np.exp(-t / 0.22) + 0.6 * np.sin(2 * np.pi * f * 1.63 * t) * np.exp(-t / 0.14)
            + 0.35 * np.sin(2 * np.pi * f * 2.67 * t) * np.exp(-t / 0.08)) * np.clip(t / 0.001, 0, 1)


def whum(seconds=0.45, lo=70, hi=320):
    t = t_axis(seconds)
    return band(noise(seconds), lo, hi) * np.sin(np.pi * np.clip(t / seconds, 0, 1)) * 1.8


def mountain(seconds=1.4, base=55.0, whistle=1760.0, whistle_gain=0.22):
    """THE MOTIF: a drone on A1 and its fifth swelling slowly, a steam whistle gliding up a little on top."""
    t = t_axis(seconds)
    shape = np.clip(t / 0.25, 0, 1) * np.clip((seconds - t) / 0.45, 0, 1)
    drone = (np.sin(2 * np.pi * base * t) + 0.55 * np.sin(2 * np.pi * base * 1.5 * t)
             + 0.25 * np.sin(2 * np.pi * base * 2 * t)) * (0.85 + 0.15 * np.sin(2 * np.pi * 0.7 * t))
    pitch = whistle * (1 + 0.015 * np.clip(t / seconds, 0, 1)) * (1 + 0.003 * np.sin(2 * np.pi * 5.1 * t))
    tone = np.sin(2 * np.pi * np.cumsum(pitch) / RATE)
    breath = band(noise(seconds), whistle * 0.8, whistle * 1.4) * 0.6
    wshape = np.clip((t - 0.15) / 0.3, 0, 1) * np.clip((seconds - t) / 0.35, 0, 1)
    return drone * shape * 0.8 + (tone * 0.6 + breath) * wshape * whistle_gain


def rumble(seconds, lo=24, hi=70):
    t = t_axis(seconds)
    return band(noise(seconds), lo, hi, 3) * 5.0 * (0.7 + 0.3 * np.sin(2 * np.pi * 0.8 * t + 0.4))


def patter(seconds=0.8, count=10):
    """Lava drops falling back after a splash: small splats and sizzles, thinning."""
    x = np.zeros(n_of(seconds))
    for k in range(count):
        at = seconds * (k / count) ** 1.3 + rng.uniform(0, 0.03)
        place(x, splat(0.22, rng.uniform(380, 620), 180), at, rng.uniform(.2, .45))
        if k % 3 == 0: place(x, sizzle(0.12), at + 0.02, 0.25)
    return x


# ---------------------------------------------------------------------------------------------- the cues
# PRESSURE
def cue_pressure_full():
    """Full heat, once: a short rising hiss with a bubble on top (his fists start dripping)."""
    x = np.zeros(n_of(0.6))
    place(x, steam(0.5, 0.85, 1800, 8000), 0.0, 0.9)
    place(x, bubble(220, 0.14, 2.6), 0.36, 0.6)
    return x


def cue_erupt():
    """An eruption on a hit: a big splat, a boom under it and drops falling back."""
    x = np.zeros(n_of(1.2))
    place(x, splat(1.3, 150, 48), 0.0, 1.0)
    place(x, boom(0.9, 70, 38, 0.6), 0.005, 0.6)
    place(x, patter(0.7, 8), 0.12, 0.6)
    return x


# LAVA RAM
def cue_ram_brace():
    """The tell (0.25 s and held): low bubbling on his shoulder over a rumble (loops while he braces)."""
    x = bubbling(1.4, 18, 90, 220, 1.2) * 0.8 + rumble(1.4, 28, 80) * 0.25
    return loop_ends(x)


def cue_ram_release():
    """The commit: lava kicked off his heel (a splat) and a thump."""
    x = np.zeros(n_of(0.6))
    place(x, splat(0.6, 210, 80), 0.0, 0.8)
    place(x, boom(0.35, 95, 60, 0.2), 0.0, 0.6)
    return x


def cue_ram_hit():
    """On a body: a wet splat and a boom, spray of drops."""
    x = np.zeros(n_of(1.1))
    place(x, splat(1.0, 180, 55), 0.0, 1.0)
    place(x, boom(0.8, 72, 40, 0.5), 0.0, 0.8)
    place(x, patter(0.5, 5), 0.08, 0.4)
    return x


def cue_ram_wall():
    """Into a wall: a dull thud and the ring of steam he sits under."""
    x = np.zeros(n_of(1.0))
    place(x, boom(0.5, 110, 70, 0.25), 0.0, 0.9)
    place(x, splat(0.5, 140, 60), 0.0, 0.5)
    place(x, steam(0.8, 0.25, 1500, 6500), 0.05, 0.7)
    return x


# MOLTEN THROW
def cue_molten_coat():
    """Lava seeps between his knuckles over the slipper: thick bubbling and a sizzle."""
    x = np.zeros(n_of(0.8))
    place(x, bubbling(0.6, 20, 140, 300, 1.1), 0.0, 0.9)
    place(x, sizzle(0.3), 0.35, 0.6)
    return x


def cue_molten_throw():
    """The heavier throw: a low whum and a hiss of drips leaving."""
    x = np.zeros(n_of(0.6))
    place(x, whum(0.45, 80, 360), 0.0, 1.0)
    place(x, sizzle(0.25, 0.8), 0.1, 0.4)
    return x


def cue_hot_can_hit():
    """The can is hit hot: a crack of hot metal (a ting with a splat under it) and a sizzle."""
    x = np.zeros(n_of(0.9))
    place(x, ting(1850, 0.6), 0.0, 0.8)
    place(x, splat(0.4, 260, 120), 0.0, 0.5)
    place(x, sizzle(0.35), 0.03, 0.6)
    return x


def cue_hot_can_hiss():
    """The can lying hot (1.5 s): a hiss that thins as it darkens."""
    s = 1.5; t = t_axis(s)
    return steam(s, 0.05, 2200, 9000) * np.linspace(1.0, 0.35, len(t))


def cue_hot_can_cool():
    """The last of it: one hiss and a puff of steam as it returns to its own colour."""
    x = np.zeros(n_of(0.7))
    place(x, sizzle(0.18), 0.0, 0.6)
    place(x, steam(0.55, 0.3, 900, 4500), 0.05, 0.8)
    return x


def cue_hot_can_refuse():
    """The taya reaches for a hot can: a short snap of sizzle (refused, not hurt)."""
    x = sizzle(0.14, 1.2)
    return place(x, band(noise(0.01), 1500, 5000) * np.exp(-t_axis(0.01) / 0.002), 0.0, 0.6)


# MAGMA FIST
def cue_fist_cast():
    """The glob swells on his knuckles (rising bubbles) and the punch leaves (a whum)."""
    x = np.zeros(n_of(0.7))
    for k, f in enumerate((150, 175, 205, 240)):
        place(x, bubble(f, 0.1, 2.0), k * 0.06, 0.7)
    place(x, whum(0.35, 90, 420), 0.28, 0.9)
    return x


def cue_fist_skim():
    """The fist skimming after them (loops): quick bubbling with a drip hiss. Played louder for the player it is coming for."""
    x = bubbling(1.4, 26, 160, 360, 0.8) * 0.8
    for at in np.arange(0.1, 1.3, 0.23):
        place(x, sizzle(0.1), at, 0.3)
    return loop_ends(x)


def cue_fist_punch():
    """The puddle opens (a gloop), the giant fist bursts up (a splat and a boom), it holds glowing."""
    x = np.zeros(n_of(1.2))
    place(x, bubble(95, 0.18, 1.0, 1.4), 0.0, 0.8)
    place(x, splat(1.1, 200, 60), 0.1, 1.0)
    place(x, boom(0.9, 78, 42, 0.6), 0.1, 0.8)
    place(x, patter(0.5, 5), 0.2, 0.35)
    return x


def cue_fist_cool():
    """It skins to black glass, cracks and slumps back into the puddle, which steams."""
    x = np.zeros(n_of(1.1))
    place(x, glass(0.5, 10), 0.0, 0.8)
    place(x, splat(0.7, 120, 50), 0.4, 0.5)
    place(x, steam(0.6, 0.3, 1200, 6000), 0.45, 0.6)
    return x


# MAYON AWAKENS, Molten and VOLCANIC BOMB
def cue_molten_absorb():
    """Something hit Molten Sean and he did not move: a dull wet thud, short (the pusher's recoil sells the rest)."""
    x = np.zeros(n_of(0.45))
    place(x, splat(0.45, 120, 70), 0.0, 0.9)
    place(x, boom(0.3, 85, 60, 0.15), 0.0, 0.4)
    return x


def cue_bomb_gather():
    """The lava runs off both arms into one ball between his fists (0.5 s), swelling and bubbling."""
    s = 0.6; x = np.zeros(n_of(s))
    b = bubbling(s, 30, 120, 300, 1.0)
    place(x, b * np.linspace(0.3, 1.0, len(b)), 0.0, 0.9)
    place(x, rumble(s, 30, 90) * np.linspace(0, 1, n_of(s)), 0.0, 0.3)
    return x


def cue_bomb_fall():
    """The ball falling (0.9 s): a steam whistle sliding down and a wobble of bubbles. Louder for whoever it is falling on."""
    s = 0.9; t = t_axis(s)
    f = 2400 * (1 - 0.45 * t / s)
    tone = np.sin(2 * np.pi * np.cumsum(f) / RATE) * 0.35 + band(noise(s), 1200, 3200) * 0.25
    x = tone * np.clip(t / 0.1, 0, 1) * (0.5 + 0.5 * t / s)
    place(x, bubbling(s, 12, 100, 200, 0.9), 0.0, 0.5)
    return x


def cue_bomb_splash():
    """The splash: the biggest boom of his kit, a crown of lava thrown up (a huge splat) and the patter falling back."""
    x = np.zeros(n_of(2.0))
    place(x, boom(1.8, 60, 28, 1.4), 0.0, 1.0)
    place(x, splat(1.8, 140, 40), 0.0, 1.0)
    place(x, patter(1.2, 14), 0.25, 0.7)
    return x


def cue_bomb_dropped():
    """The unthrown bomb at his feet: smaller and closer, no fall."""
    x = np.zeros(n_of(1.3))
    place(x, boom(1.1, 70, 34, 0.9), 0.0, 1.0)
    place(x, splat(1.2, 160, 50), 0.0, 0.9)
    place(x, patter(0.7, 8), 0.15, 0.5)
    return x


def cue_pool():
    """The pool (4 s, loops): a slow bubbling that skins over (it thins), with a little glass."""
    s = 2.2
    x = bubbling(s, 9, 90, 200, 1.4) * 0.7
    place(x, glass(1.6, 5), 0.3, 0.25)
    return loop_ends(x)


def cue_molten_end():
    """The lava on his arms skins to black glass, cracks and sheds (0.6 s)."""
    x = np.zeros(n_of(0.9))
    place(x, glass(0.6, 12), 0.0, 1.0)
    place(x, steam(0.7, 0.2, 1500, 6500), 0.05, 0.5)
    return x


def cue_theme():
    """The cutscene, 7.0 s, beat for beat on the BUILT stage's clock (see the header)."""
    s = 7.0; x = np.zeros(n_of(s))
    # 0.00 cut-in accent: a deep thump and a crackle swelling in (embers already in the air, cracks running to his feet).
    place(x, boom(0.9, 58, 34, 0.5), 0.0, 0.8)
    c = glass(0.45, 7); place(x, c * np.linspace(0.2, 1.0, len(c)), 0.0, 0.35)
    # 0.45 the cracks reach his feet: a second thump, lower.
    place(x, boom(0.9, 50, 30, 0.6), 0.45, 0.85)
    # 0.45 to 1.40 the crane up his body: the rumble rises with the heat.
    r = rumble(1.6); place(x, r * np.linspace(0.15, 1.0, len(r)), 0.45, 0.5)
    # 1.40 the summit lights: the motif, first time.
    place(x, mountain(1.3, 55, 1760, 0.22), 1.38, 0.5)
    # 1.60 to 2.10 held, the long draw in: steam swelling (his breath is steam, not a voice).
    place(x, steam(0.5, 0.9, 900, 5000), 1.60, 0.45)
    # 2.10 the cut to the fist: the rumble ducks (everything before is faded by 2.1).
    i0, i1 = n_of(2.02), n_of(2.12)
    x[i0:i1] *= np.linspace(1, 0.25, i1 - i0); x[i1:] *= 0.25
    # 2.25 the clench: two knuckle cracks.
    place(x, knuckle(), 2.25, 0.9); place(x, knuckle(), 2.33, 0.7)
    # 2.40 to 3.20 the lava runs UP his forearm: a thick bubbling per rivulet, a sizzle per drip on the court.
    for at in (2.40, 2.56, 2.74, 2.93):
        place(x, bubbling(0.32, 30, 120, 260, 1.1), at, 0.55)
    for at in (2.62, 2.81, 2.98, 3.10, 3.18):
        place(x, sizzle(0.2), at, 0.35)
    # 3.20 to 3.45 the swell rises as the lens slides round to his face.
    t = t_axis(0.25); place(x, band(noise(0.25), 150, 3200) * (t / 0.25) ** 2 * 0.45, 3.20)
    # 3.45 his eyes ignite: a hot-metal ting and the motif, short.
    place(x, ting(2100, 0.7), 3.45, 0.35)
    place(x, mountain(0.6, 55, 1760, 0.3), 3.45, 0.35)
    # 3.60 to 4.10 held, the hero frame: steam leaving his mouth.
    place(x, steam(0.55, 0.2, 1100, 5500), 3.60, 0.5)
    # 4.10 to 4.30 the swell drawn in hard as he pulls the fist back.
    t = t_axis(0.2); place(x, band(noise(0.2), 120, 2800) * (t / 0.2) ** 3 * 0.55, 4.10)
    # 4.30 to 5.60 the coil: the swell keeps climbing and holds at the top (4.8 to 5.45), the rumble under it.
    t = t_axis(1.3); place(x, band(noise(1.3), 100, 3000) * np.clip(t / 0.5, 0, 1) ** 2 * 0.38, 4.30)
    r = rumble(1.3); place(x, r * np.linspace(0.4, 1.0, len(r)), 4.30, 0.5)
    # 5.60 THE SLAM: the swell cut dead into the impact: a deep wet thud and a low boom, then the patter.
    i0 = n_of(5.56); i1 = n_of(5.60)
    x[i0:i1] *= np.linspace(1, 0, i1 - i0); x[i1:] = 0
    place(x, boom(1.4, 60, 28, 1.3), 5.60, 1.6)
    place(x, splat(1.5, 150, 42), 5.60, 1.2)
    place(x, patter(0.9, 12), 5.75, 0.55)
    # 5.60 to 6.25 the shockwave out over the court: a crackle under it (the ring), the column of the fountain.
    place(x, glass(0.6, 10), 5.80, 0.3)
    place(x, steam(0.6, 0.3, 900, 4000), 5.85, 0.3)
    # 6.25 to 6.65 he stands out of it, Molten: the motif, long, a crackle under it.
    place(x, mountain(0.75, 55, 1760, 0.26), 6.25, 0.5)
    place(x, bubbling(0.7, 10, 100, 220, 1.0), 6.25, 0.25)
    # 6.65 to 7.00 the tail: a settling rumble carried into play.
    r = rumble(0.35); place(x, r * np.linspace(1, 0.2, len(r)), 6.65, 0.35)
    return fade_tail(x, 0.04)


CUES = {
    "sfx_sean_pressure_full": cue_pressure_full, "sfx_sean_erupt": cue_erupt,
    "sfx_sean_ram_brace": cue_ram_brace, "sfx_sean_ram_release": cue_ram_release, "sfx_sean_ram_hit": cue_ram_hit,
    "sfx_sean_ram_wall": cue_ram_wall,
    "sfx_sean_molten_coat": cue_molten_coat, "sfx_sean_molten_throw": cue_molten_throw, "sfx_sean_hot_can_hit": cue_hot_can_hit,
    "sfx_sean_hot_can_hiss": cue_hot_can_hiss, "sfx_sean_hot_can_cool": cue_hot_can_cool, "sfx_sean_hot_can_refuse": cue_hot_can_refuse,
    "sfx_sean_fist_cast": cue_fist_cast, "sfx_sean_fist_skim": cue_fist_skim, "sfx_sean_fist_punch": cue_fist_punch,
    "sfx_sean_fist_cool": cue_fist_cool,
    "sfx_sean_molten_absorb": cue_molten_absorb, "sfx_sean_bomb_gather": cue_bomb_gather, "sfx_sean_bomb_fall": cue_bomb_fall,
    "sfx_sean_bomb_splash": cue_bomb_splash, "sfx_sean_bomb_dropped": cue_bomb_dropped, "sfx_sean_pool": cue_pool,
    "sfx_sean_molten_end": cue_molten_end,
    "sfx_ult_theme_sean": cue_theme,
}

# Loops and quiet beds are normalised lower so a loop under play never outweighs a hit.
QUIET = {"sfx_sean_ram_brace": .45, "sfx_sean_hot_can_hiss": .4, "sfx_sean_fist_skim": .55, "sfx_sean_pool": .35}


def write(path, x):
    pcm = (np.clip(x, -1, 1) * 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(RATE); w.writeframes(pcm.tobytes())


def main():
    os.makedirs(OUT, exist_ok=True)
    table = {}
    for name, make in CUES.items():
        x = norm(make(), QUIET.get(name, 0.85))
        write(os.path.join(OUT, name + ".wav"), x)
        rms = float(np.sqrt(np.mean(x * x)))
        table[name] = {"seconds": round(len(x) / RATE, 3), "peak": round(float(np.max(np.abs(x))), 3), "rms": round(rms, 4)}
        if name == "sfx_ult_theme_sean":
            w = n_of(0.1)
            table[name]["rms_per_0.1s"] = [round(float(np.sqrt(np.mean(x[i:i + w] ** 2))), 3) for i in range(0, len(x), w)]
    with open(os.path.join(OUT, "loudness.json"), "w", encoding="utf-8") as f:
        json.dump(table, f, indent=1)
    loud = table["sfx_ult_theme_sean"]["rms_per_0.1s"]
    print("theme loudest 0.1 s at %.1f s (the slam is at 5.6)" % (loud.index(max(loud)) / 10))
    print("wrote %d cues to %s" % (len(table), OUT))


if __name__ == "__main__":
    main()
