"""Phaister's sounds (HERO-10): original, deterministic, one recipe per cue. Writes only her files.

The baseline is `docs/HERO_KIT_METHOD.md` section 5: every cue has a TRANSIENT, a BODY in the ability's own texture and a
TAIL that says it is over; the hero's element is a family of instruments and no two abilities share a recipe. Plan:
`docs/reports/phaister-kit-2026-09-27/plan.md` section 3.

SHE IS A WITCH (owner: *"make her a frigging witchh not a showman"*), SO HER FAMILY IS WINGS, CLOTH, PIN METAL, CANDLE AND
MOONLIGHT. Paete's is wood and leaf (`build_paete_audio.py`, whose filters this borrows and whose instruments it does not).
Each of hers is named where it is defined:
- `flutter`: many wings beating, each an amplitude-modulated band of noise at its own beat rate (a moth beats about 30 times
  a second, far slower than a fly), summed; grains of air, never a hiss.
- `buzz`: a beetle in flight, a low sawtooth hum through a nasal band, its pitch wobbling.
- `stitch`: a needle through cloth, a tiny tear of noise that ends in a pop.
- `tick`: a pin's bright metal, three inharmonic partials that die fast.
- `moon`: moonlight, a glassy chord of pure partials with a slow bloom and a slightly sharp top that shimmers.
- `candle`: a flame catching, a low whoomph of noise and a few crackles.
- `musicbox`: the doll's voice, struck tines (fast decay, a metallic second partial), played slightly out of tune.
- `ash`: something crumbling to ash, dense quiet clicks through a low band, thinning out.
- `hex`: HER MOTIF, three falling glassy tones (a minor third then a tritone down), heard every time her sigil is drawn.

Only numpy (via Paete's builder's filters); every random source seeded, so a rebuild writes identical bytes.

Run: python tools/build_phaister_audio.py
"""
import json
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import build_paete_audio as pa  # noqa: E402  (filters and the writer only; none of his instruments)
from build_paete_audio import RATE, times, noise, svf, one_pole_low, env_ar, window, sweep, finish  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]


# ------------------------------------------------------------------ her instruments

def flutter(t, seed, rates, centre, density):
    """Many wings: each (beat Hz, gain, phase) in `rates` is a band of noise opened and shut at its own beat rate; `density`
    (an array over t) is how many are in the air."""
    n = len(t)
    out = np.zeros(n)
    base = svf(noise(n, seed), centre, 1.2) + 0.5 * svf(noise(n, seed + 1), centre * 2.1, 1.6)
    for hz, gain, phase in rates:
        gate = np.clip(np.sin(2 * np.pi * hz * t + phase), 0, 1) ** 2
        out += base * gate * gain
    return out * np.broadcast_to(np.asarray(density, dtype=float), (n,))


def buzz(t, pitch, wobble, centre, shape):
    """A beetle: a sawtooth at `pitch` (Hz, may be an array) wobbling by `wobble`, through a nasal band."""
    f = np.broadcast_to(np.asarray(pitch, dtype=float), t.shape) * (1 + wobble * np.sin(2 * np.pi * 7.3 * t))
    phase = np.cumsum(f) / RATE
    saw = 2 * (phase % 1.0) - 1
    return svf(saw, centre, 3.0) * shape


def stitch(t, seed, at, gain=1.0):
    """A needle through cloth: 30 ms of tearing noise rising in pitch, then a tiny pop as it comes through."""
    local = t - at
    on = (local >= 0) & (local < 0.03)
    tear = svf(noise(len(t), seed), 2200 + 60000 * np.clip(local, 0, 0.03), 2.5) * on * np.clip(local / 0.03, 0, 1)
    pop = np.sin(2 * np.pi * 1800 * np.maximum(0, local - 0.03)) * np.exp(-np.maximum(0, local - 0.03) / 0.006) * (local >= 0.03)
    return (tear * 0.8 + pop * 0.6) * gain


def tick(t, at, pitch, gain=1.0):
    """A pin's bright metal: inharmonic partials (1, 2.92, 5.37) dying in 20 to 60 ms."""
    local = np.maximum(0, t - at)
    on = t >= at
    return sum(np.sin(2 * np.pi * pitch * r * local) * g * np.exp(-local / d)
               for r, g, d in [(1.0, 1.0, 0.06), (2.92, 0.5, 0.03), (5.37, 0.3, 0.018)]) * on * gain


def moon(t, at, root, seconds, gain=1.0):
    """Moonlight: a glassy chord (root, fifth, octave, and a ninth a hair sharp that shimmers against the octave), blooming
    over 80 ms and fading over `seconds`."""
    local = np.maximum(0, t - at)
    on = t >= at
    env = np.clip(local / 0.08, 0, 1) * np.exp(-local / max(0.05, seconds * 0.45))
    tones = [(1.0, 0.6), (1.5, 0.45), (2.0, 0.4), (2.25 * 1.004, 0.25), (4.0, 0.08)]
    return sum(np.sin(2 * np.pi * root * r * local) * g for r, g in tones) * env * on * gain


def candle(t, seed, at, gain=1.0):
    """A flame catching: a low whoomph (noise through a falling low band, 0.2 s) and three crackles after it."""
    local = t - at
    on = local >= 0
    body = svf(noise(len(t), seed), 900 * np.exp(-np.maximum(0, local) / 0.12) + 140, 0.9) * env_ar(np.maximum(0, local), 0.02, 0.09) * on
    crackle = np.zeros(len(t))
    for k, dt in enumerate((0.05, 0.11, 0.19)):
        i = int((at + dt) * RATE)
        if 0 <= i < len(t):
            crackle[i] = 1.0 - 0.25 * k
    crackle = svf(crackle, 3200, 1.8) * 1.6
    return (body * 1.6 + crackle) * gain


def musicbox(t, at, notes, detune, gain=1.0):
    """Struck tines: each (offset s, Hz) a sine with a metallic 3.0x partial, 0.5 s decay; `detune` bends each note flat by
    that fraction as it rings (the doll's tune going wrong)."""
    out = np.zeros(len(t))
    for offset, hz in notes:
        local = np.maximum(0, t - at - offset)
        on = t >= at + offset
        f = hz * (1 - detune * np.clip(local / 0.5, 0, 1))
        phase = 2 * np.pi * np.cumsum(np.where(on, f, 0.0)) / RATE
        out += (np.sin(phase) + 0.25 * np.sin(3.0 * phase)) * np.exp(-local / 0.5) * on
    return out * gain


def ash(t, seed, at, seconds, gain=1.0):
    """Crumbling to ash: dense quiet clicks through a low-mid band, thinning over `seconds`."""
    local = t - at
    dens = 2400 * np.clip(1 - local / seconds, 0, 1) * (local >= 0)
    rng = np.random.default_rng(seed)
    hits = (rng.random(len(t)) < dens / RATE) * rng.normal(0, 1, len(t))
    return (svf(hits, 1400, 1.1) + 0.5 * svf(hits, 3600, 1.4)) * gain


def hex_motif(t, at, root, gain=1.0):
    """HER MOTIF: three falling glassy tones, 70 ms apart (root, a minor third down, a tritone below that)."""
    out = np.zeros(len(t))
    for k, ratio in enumerate((1.0, 0.8409, 0.5946)):
        local = np.maximum(0, t - at - 0.07 * k)
        on = t >= at + 0.07 * k
        f = root * ratio
        out += (np.sin(2 * np.pi * f * local) + 0.3 * np.sin(2 * np.pi * f * 2.005 * local)) * np.exp(-local / 0.28) * on * (1 - 0.15 * k)
    return out * gain


WINGS = [(28.0, 1.0, 0.0), (31.5, 0.8, 1.3), (25.0, 0.9, 2.1), (34.0, 0.6, 0.7), (29.3, 0.7, 2.9), (22.5, 0.5, 4.1)]


# ------------------------------------------------------------------ the recipes

def blink_cast():
    """VANISHING ACT, where she LEAVES (0.55 s): a candle-flame whoomph as she bursts, the swarm's wings rising dense and
    streaming away (their band sweeping up as they go), a beetle buzz under it, dying out along the lane."""
    s = 0.55
    t = times(s)
    burst = candle(t, 9101, 0.0, 1.0)
    dens = np.clip(t / 0.03, 0, 1) * np.exp(-np.maximum(0, t - 0.05) / 0.16)
    wings = svf(flutter(t, 9102, WINGS, 2400, dens), sweep(t, 1800, 4200, 0.35), 0.8) * 1.4
    beetle = buzz(t, sweep(t, 150, 190, 0.3), 0.05, 900, window(t, 0.01, 0.3, 0.05)) * 0.35
    return finish("sfx_cast_phaister_blink", burst + wings + beetle, s)


def swarm_knit():
    """VANISHING ACT, where she ARRIVES (0.9 s): the swarm arriving and spiralling in (wings swelling, the band falling as it
    packs tight), her motif as the sigil flares, and a last few wings settling on her hat."""
    s = 0.9
    t = times(s)
    dens = np.clip(t / 0.12, 0, 1) * np.exp(-np.maximum(0, t - 0.18) / 0.10) + 0.25 * window(t, 0.35, 0.8, 0.1)
    wings = svf(flutter(t, 9111, WINGS[:4], 2600, dens), sweep(t, 3600, 1600, 0.25), 0.8) * 1.3
    motif = hex_motif(t, 0.20, 1320, 0.55)
    return finish("sfx_phaister_swarm_knit", wings + motif, s)


def doll_cast():
    """MANIKA MISCHIEF's press (0.6 s): a needle pricking the doll, a breath of air (her whisper, synthesized as air only,
    never a voice), the overhand throw's swish and the pins rattling inside as it tumbles away."""
    s = 0.6
    t = times(s)
    prick = stitch(t, 9121, 0.02, 1.0)
    breath = svf(noise(len(t), 9122), 2800, 0.8) * env_ar(np.maximum(0, t - 0.08), 0.06, 0.08) * (t >= 0.08) * 0.25
    swish = svf(noise(len(t), 9123), sweep(t - 0.28, 700, 2600, 0.12), 1.4) * env_ar(np.maximum(0, t - 0.28), 0.03, 0.06) * (t >= 0.28) * 0.9
    rattle = sum(tick(t, 0.32 + 0.045 * k, 3100 + 170 * (k % 3), 0.18 - 0.02 * k) for k in range(6))
    return finish("sfx_cast_phaister_doll", prick + breath + swish + rattle, s)


def manika_land():
    """The doll missing (0.8 s): a soft cloth thump, then one sad tine going flat."""
    s = 0.8
    t = times(s)
    thump = one_pole_low(noise(len(t), 9131), 320) * env_ar(t, 0.004, 0.05) * 2.5
    tine = musicbox(t, 0.2, [(0.0, 659.3)], 0.05, 0.35)
    return finish("sfx_phaister_manika_land", thump + tine, s, 0.55)


def manika_steal():
    """The doll taking its victim (1.4 s): a slap, a sucking chime rising as their colour drains in, then the doll's tune on
    a music box, detuning as it rings (their head is hers to turn now)."""
    s = 1.4
    t = times(s)
    slap = svf(noise(len(t), 9141), 1500, 0.9) * env_ar(t, 0.002, 0.03) * 1.4
    suck = np.sin(2 * np.pi * np.cumsum(sweep(t, 300, 1200, 0.25)) / RATE) * window(t, 0.02, 0.27, 0.04) * 0.35
    tune = musicbox(t, 0.3, [(0.0, 880.0), (0.14, 739.99), (0.28, 659.26), (0.46, 587.33), (0.62, 523.25)], 0.035, 0.45)
    return finish("sfx_phaister_manika_steal", slap + suck + tune, s)


def manika_crumble():
    """The doll let go (0.6 s): a few stitches popping, then it crumbles to ash."""
    s = 0.6
    t = times(s)
    pops = sum(stitch(t, 9151 + k, 0.02 + 0.05 * k, 0.5) for k in range(3))
    dust = ash(t, 9155, 0.12, 0.45, 0.7)
    return finish("sfx_phaister_manika_crumble", pops + dust, s, 0.5)


def pin_cast():
    """SPOTLIGHT PIN's press (0.7 s): the pin drawn from the hat band (two ticks), the stab, and her motif as the sigils sweep
    the cone, with a thin glassy streak under the sweep moving up in pitch left to right."""
    s = 0.7
    t = times(s)
    draw = tick(t, 0.0, 2600, 0.6) + tick(t, 0.06, 3100, 0.45)
    stab = svf(noise(len(t), 9161), 1900, 1.2) * env_ar(np.maximum(0, t - 0.16), 0.004, 0.03) * (t >= 0.16) * 0.8
    motif = hex_motif(t, 0.18, 1480, 0.6)
    streak = np.sin(2 * np.pi * np.cumsum(sweep(np.maximum(0, t - 0.18), 1200, 2400, 0.2)) / RATE) * window(t, 0.18, 0.40, 0.04) * 0.15
    return finish("sfx_cast_phaister_pin", draw + stab + motif + streak, s)


def moonlight_on():
    """The moonlight dropping on a player (1.2 s): a pin of light striking the court (a bright tick), and the moon chord
    blooming over it."""
    s = 1.2
    t = times(s)
    strike = tick(t, 0.06, 2200, 0.8)
    chord = moon(t, 0.02, 523.25, 1.1, 0.6)
    return finish("sfx_phaister_moonlight_on", strike + chord, s, 0.6)


def moonlight_off():
    """The moonlight ending (0.5 s): a small snap as the beam goes out, then the pin cooling to ash."""
    s = 0.5
    t = times(s)
    snap_ = svf(noise(len(t), 9171), 4200, 1.6) * env_ar(t, 0.002, 0.012) * 1.2
    fall = moon(t, 0.0, 392.0, 0.25, 0.25)
    dust = ash(t, 9172, 0.08, 0.35, 0.4)
    return finish("sfx_phaister_moonlight_off", snap_ + fall + dust, s, 0.45)


def glitch(t, seed, start, end, rate, gain=1.0):
    """The eye forming (owner: *"glitchy and unstable as fuck ... pulsate"*): a crackle chopped by a stepped gate that
    stutters at `rate` steps a second, each step its own pitch of buzz or silence, like a signal breaking up."""
    n = len(t)
    rng = np.random.default_rng(seed)
    steps = int((end - start) * rate) + 2
    levels = rng.random(steps) > 0.35
    pitches = 180 + 900 * rng.random(steps)
    idx = np.clip(((t - start) * rate).astype(int), 0, steps - 1)
    inside = (t >= start) & (t < end)
    gate = levels[idx] * inside
    f = pitches[idx]
    phase = np.cumsum(f) / RATE
    square = np.sign(np.sin(2 * np.pi * phase))
    crackle = svf(noise(n, seed + 1), 3200, 1.5)
    return (square * 0.35 + crackle * 0.8) * gate * gain


def omen_cast():
    """OMEN's live cast, after the cutscene hands back (2.3 s; `sfx_cast_phaister_higop`, played as the eye she threw hangs
    unstable over the spot and the marks fly out; v8 moves it off the rework builder's generic whine and into her family):
      0.00       a candle whoomph as the cast takes (her flame, the same one VANISHING ACT bursts with)
      0.00-2.20  the eye GLITCHING: the stuttering crackle of the cutscene's forming, its step rate climbing as the 2.2 s wears on,
                 over a low throb that quickens (the eye's pulse)
      0.25-0.80  the marks: three bursts of wings leaving the eye, one for each butterfly going out to a player
      0.00-2.20  a wind drawn in under it all, rising, and cut dead at 2.2 where the eye opens (the open is its own cue)"""
    s = 2.3
    t = times(s)
    whoomph = candle(t, 9201, 0.0, 0.8)
    stutter = glitch(t, 9202, 0.05, 1.2, 12.0, 0.45) + glitch(t, 9203, 1.2, 2.2, 22.0, 0.55)
    rate = 2.4 + 3.0 * np.clip(t / 2.2, 0, 1)
    pulse = np.sin(2 * np.pi * 52 * t) * (0.5 + 0.5 * np.sin(2 * np.pi * np.cumsum(rate) / RATE)) ** 2 * window(t, 0.05, 2.2, 0.02) * 0.7
    marks = sum(svf(flutter(t, 9204 + k, WINGS[k:k + 3], 2600 + 300 * k, window(t, 0.25 + 0.2 * k, 0.55 + 0.2 * k, 0.08)), 2800, 0.9)
                for k in range(3)) * 0.8
    wind = svf(noise(len(t), 9208), sweep(t, 260, 1100, 2.2), 1.1) * np.clip(t / 2.2, 0, 1) ** 1.5 * window(t, 0.0, 2.2, 0.01) * 0.9
    return finish("sfx_cast_phaister_higop", whoomph + stutter + pulse + marks + wind, s)


def omen_open():
    """OMEN opening in play (2.8 s; `sfx_phaister_higop_open`): the eye snaps open and starts to drink (plan 4.4 rows 4, 6, 7):
      0.00       the snap: a hard two-frame silence already happened in the cast; here a deep whump in her low register and the
                 moon chord low and wide, a butterfly-shaped burst of wings outward (the landing flash)
      0.10-2.80  the maelstrom: a turning wind whose band swings round (clockwise, heard as a slow sweep), many wings circling
                 (the flutter band swinging with the wind), and a low drone under it that sinks a semitone (the drinking)"""
    s = 2.8
    t = times(s)
    local = np.maximum(0, t)
    whump = np.sin(2 * np.pi * 44 * local * (1 - 0.25 * local)) * np.exp(-local / 0.4) * 2.0
    chord = moon(t, 0.0, 196.0, 1.4, 0.9)
    burst = svf(flutter(t, 9211, WINGS, 2600, np.exp(-t / 0.25)), sweep(t, 3000, 1600, 0.3), 0.8) * 1.3
    turning = 0.6 + 0.4 * np.sin(2 * np.pi * 0.8 * t)
    wind = svf(noise(len(t), 9212), 600 + 350 * np.sin(2 * np.pi * 0.8 * t), 1.0) * window(t, 0.1, 2.8, 0.4) * 1.0
    wings = svf(flutter(t, 9213, WINGS[1:], 2200, window(t, 0.15, 2.8, 0.5) * turning), 2400, 0.8) * 0.9
    f = 55 * sweep(t, 1.0, 0.944, 2.8)
    drone = np.sin(2 * np.pi * np.cumsum(f) / RATE) * window(t, 0.1, 2.8, 0.6) * 0.6
    return finish("sfx_phaister_higop_open", whump + chord + burst + wind + wings + drone, s, 0.72)


def omen_close():
    """OMEN ending (1.2 s; `sfx_phaister_higop_close`): every butterfly bursts UP into the sky at once (a dense rush of wings
    sweeping up in pitch), the eye shuts (the moon chord falling and a soft pop), and three stragglers flutter off in the tail."""
    s = 1.2
    t = times(s)
    rush = svf(flutter(t, 9221, WINGS, 2400, np.clip(t / 0.03, 0, 1) * np.exp(-t / 0.35)), sweep(t, 1800, 5200, 0.5), 0.8) * 1.6
    shut = moon(t, 0.05, 311.1, 0.3, 0.5)
    pop = np.sin(2 * np.pi * 120 * np.maximum(0, t - 0.1)) * np.exp(-np.maximum(0, t - 0.1) / 0.03) * (t >= 0.1) * 0.9
    stragglers = sum(svf(flutter(t, 9222 + k, WINGS[k:k + 2], 2800, window(t, 0.45 + 0.18 * k, 0.75 + 0.18 * k, 0.08)), 3000, 0.9)
                     for k in range(3)) * 0.4
    return finish("sfx_phaister_higop_close", rush + shut + pop + stragglers, s)


def theme():
    """OMEN's cutscene, 5.0 s (v8), timed to its beats (`tools/author_ultimate_intros.py` phaister(); the method: one theme file,
    a cut-in accent on frame 0, a swell drawn in before the impact and cut dead on it, the impact in her own material, her motif
    where her sign is drawn, a tail):
      0.00       the cut-in: a candle-flame whoomph and her night falling (a low swell)
      0.15-1.30  SURGE: wings rising round her, dense and circling (the flutter band swinging), a rising wind, and a glassy moon
                 chord blooming low as the column of her light climbs
      1.38-1.46  her eyes light: one bright pin tick for the glint
      1.45-2.47  THE EYE: the wings sucked in and gone; the eye forming, GLITCHING and pulsing (a stuttering crackle whose rate
                 climbs), a low pulse on the beat of its throb; her motif at 2.20, on her wink
      2.34-2.95  the throw: the swell drawn in, then a whoosh that climbs as the eye crosses the frame
      2.95-3.00  two frames of silence
      3.00       the IMPACT, cut dead on it: a deep whump, the moon chord and a dry tearing crack (the picture turned inside out)
      3.20-5.00  THE MARK: a turning wind and many wings (the maelstrom starting); a small flutter and one falling glass tone as each
                 mark settles on a player (3.73, 3.83, 3.91: `HeroIntroductionScene.PhaisterMark.cs`); fading as play returns."""
    s = 5.0
    t = times(s)
    cut_in = candle(t, 9181, 0.0, 1.2) + one_pole_low(noise(len(t), 9182), 90) * env_ar(t, 0.3, 0.6) * 1.5
    circling = 0.6 + 0.4 * np.sin(2 * np.pi * 1.4 * t)
    wings = svf(flutter(t, 9183, WINGS, 2400, window(t, 0.15, 1.45, 0.3) * circling), sweep(t, 1600, 3000, 1.3), 0.8) * 1.2
    column = moon(t, 0.30, 261.6, 1.0, 0.35)
    wind = svf(noise(len(t), 9184), sweep(t, 300, 1400, 2.8), 1.1) * np.clip((t - 0.2) / 2.3, 0, 1) ** 2 * window(t, 0.2, 2.95, 0.02) * 1.1
    glint = tick(t, 1.42, 3300, 0.45)
    suck = svf(flutter(t, 9190, WINGS[:4], 2600, window(t, 1.45, 1.95, 0.15)), sweep(t - 1.45, 2000, 5200, 0.5), 0.9) * 0.9
    stutter = glitch(t, 9185, 1.5, 2.47, 14.0, 0.6) + glitch(t, 9186, 2.05, 2.47, 26.0, 0.5)
    throb = np.sin(2 * np.pi * 55 * t) * (0.5 + 0.5 * np.sin(2 * np.pi * 3.7 * t)) ** 2 * window(t, 1.5, 2.47, 0.05) * 0.9
    motif = hex_motif(t, 2.2, 1320, 0.55)
    throw = svf(noise(len(t), 9187), sweep(np.maximum(0, t - 2.47), 500, 3800, 0.48), 1.4) * window(t, 2.47, 2.94, 0.03) * 1.2
    silence = 1.0 - window(t, 2.945, 3.0, 0.005)
    hit = np.maximum(0, t - 3.0)
    whump = np.sin(2 * np.pi * 48 * hit * (1 - 0.3 * hit)) * np.exp(-hit / 0.35) * (t >= 3.0) * 2.2
    chord = moon(t, 3.0, 392.0, 1.2, 0.9)
    crack = svf(noise(len(t), 9191), 2400, 0.7) * env_ar(hit, 0.002, 0.05) * (t >= 3.0) * 1.3
    maelstrom = svf(noise(len(t), 9188), 700 + 300 * np.sin(2 * np.pi * 0.9 * t), 1.0) * window(t, 3.05, 5.0, 0.4) * 0.9
    tail = svf(flutter(t, 9189, WINGS[:4], 2000, window(t, 3.1, 5.0, 0.5)), 2200, 0.8) * 0.8
    marks = np.zeros(len(t))
    for k, at in enumerate((3.73, 3.83, 3.91)):
        marks += svf(flutter(t, 9192 + k, WINGS[k:k + 2], 2900, window(t, at - 0.08, at + 0.12, 0.04)), 3000, 0.9) * 0.5
        marks += hex_motif(t, at, 1480 - 90 * k, 0.12)
    mix = (cut_in + wings + column + wind + glint + suck + stutter + throb + motif + throw) * silence + whump + chord + crack + maelstrom + tail + marks
    return finish("sfx_ult_theme_phaister", mix, s, 0.75)


# ------------------------------------------------------------------ v3, THE VOODOO KIT (plan 9.5): thread, pins and cloth
# Two more instruments for the curses, named where they are defined:
# - `whip`: a thread snapped taut across the court, a narrow band of noise sweeping up fast and cut off.
# - `rope`: a rope or rag wrung, stick-slip clicks through a woody mid band, their rate climbing as it tightens.
# - `hum`: the soul thread holding, two slow-beating low sines under a breath of noise, rising in pitch as the reach fills.

def whip(t, seed, at, lo, hi, gain=1.0):
    """A thread snapped taut: 60 ms of narrow noise sweeping `lo` to `hi` Hz, cut off hard."""
    local = t - at
    on = (local >= 0) & (local < 0.06)
    band = svf(noise(len(t), seed), lo + (hi - lo) * np.clip(local / 0.06, 0, 1), 3.0)
    return band * on * np.clip(local / 0.01, 0, 1) * gain


def rope(t, seed, at, seconds, rate0, rate1, gain=1.0):
    """A rag wrung: stick-slip clicks through a woody band at a rate rising from `rate0` to `rate1` per second."""
    local = t - at
    on = (local >= 0) & (local < seconds)
    rate = rate0 + (rate1 - rate0) * np.clip(local / seconds, 0, 1)
    phase = np.cumsum(np.where(on, rate, 0.0)) / RATE
    clicks = (np.diff(np.floor(phase), prepend=0) > 0).astype(float)
    body = svf(clicks * (0.6 + 0.4 * noise(len(t), seed)), 700, 1.6) + 0.4 * svf(clicks, 1900, 2.2)
    return body * np.clip(local / 0.04, 0, 1) * on * gain


def hum(t, at, seconds, root, gain=1.0):
    """The thread holding: two low sines a few Hz apart (a slow beat) with a breath of noise, rising a fifth over `seconds`."""
    local = np.maximum(0, t - at)
    on = (t >= at) & (t < at + seconds)
    f = root * (1 + 0.5 * np.clip(local / seconds, 0, 1))
    phase = 2 * np.pi * np.cumsum(np.where(on, f, 0.0)) / RATE
    tone = np.sin(phase) + 0.8 * np.sin(phase * 1.012)
    return tone * np.clip(local / 0.2, 0, 1) * on * gain


def drain_cast():
    """CURSE: DRAIN's lock (0.9 s): the slipper tucked (a cloth rustle), the thread whipped out LOW and heavy, the needle's
    prick at their chest, one heavy heartbeat, and the crimson hum starting under it (the hold's first second)."""
    s = 0.9
    t = times(s)
    tuck = svf(noise(len(t), 9301), 1800, 0.9) * env_ar(t, 0.005, 0.04) * 0.35
    lash = whip(t, 9302, 0.08, 500, 2600, 1.2)
    prick = stitch(t, 9303, 0.14, 0.9)
    beat = np.sin(2 * np.pi * 52 * np.maximum(0, t - 0.2)) * np.exp(-np.maximum(0, t - 0.2) / 0.07) * (t >= 0.2) * 1.6
    hold = hum(t, 0.18, 0.72, 98.0, 0.35) * np.exp(-np.maximum(0, t - 0.6) / 0.12)
    return finish("sfx_cast_phaister_drain", tuck + lash + prick + beat + hold, s)


def hex_cast():
    """CURSE: HEX's lock (0.9 s): the tuck, the thread whipped out HIGH and thin, a finer prick at their eyes, one struck
    tine from the doll lifted to her cheek, and the violet hum starting higher than DRAIN's."""
    s = 0.9
    t = times(s)
    tuck = svf(noise(len(t), 9311), 1900, 0.9) * env_ar(t, 0.005, 0.04) * 0.35
    lash = whip(t, 9312, 0.08, 1400, 5200, 1.0)
    prick = stitch(t, 9313, 0.14, 0.7) + tick(t, 0.17, 3900, 0.25)
    tine = musicbox(t, 0.2, [(0.0, 1046.5)], 0.02, 0.35)
    hold = hum(t, 0.18, 0.72, 147.0, 0.3) * np.exp(-np.maximum(0, t - 0.6) / 0.12)
    return finish("sfx_cast_phaister_hexreach", tuck + lash + prick + tine + hold, s)


def reach_mark():
    """The reach landing (0.55 s): the stitch pulled tight (a tearing zip rising into a tick), a skipped heartbeat, her motif
    faint: the soul is in the doll."""
    s = 0.55
    t = times(s)
    zip_ = svf(noise(len(t), 9321), sweep(t, 900, 4200, 0.16), 2.2) * window(t, 0.0, 0.16, 0.01) * 0.9
    tight = tick(t, 0.16, 2200, 0.8)
    skip = np.sin(2 * np.pi * 60 * np.maximum(0, t - 0.2)) * np.exp(-np.maximum(0, t - 0.2) / 0.05) * (t >= 0.2) * 1.0
    motif = hex_motif(t, 0.22, 1175, 0.25)
    return finish("sfx_phaister_mark", zip_ + tight + skip + motif, s, 0.6)


def reach_snap():
    """The reach broken (0.45 s): the thread frays (a thin crackle) and snaps back (a dry tick and a falling whip)."""
    s = 0.45
    t = times(s)
    fray = ash(t, 9331, 0.0, 0.18, 0.5)
    snap = tick(t, 0.12, 2900, 0.7) + whip(t, 9332, 0.12, 3800, 900, 0.6)
    return finish("sfx_phaister_reach_snap", fray + snap, s, 0.5)


def wring():
    """DRAIN's wring, 1.7 s, the mark to the drain (plan 9.5: *"rope creak on each twist"*, *"a wet-cloth squeeze; an
    exhausted exhale"*): three twists at 0.25, 0.75 and 1.25, each longer and tighter, the wet squeeze on the last hard wring
    at 1.5 (the moment the body drains them), and an exhausted breath out after it (air only, never a voice)."""
    s = 1.7
    t = times(s)
    twists = (rope(t, 9341, 0.20, 0.22, 40, 90, 0.7) + rope(t, 9342, 0.70, 0.26, 50, 120, 0.85)
              + rope(t, 9343, 1.20, 0.30, 60, 160, 1.0))
    wet = svf(noise(len(t), 9344), 420 + 260 * np.sin(2 * np.pi * 23 * t), 1.3) * window(t, 1.48, 1.62, 0.02) * 1.3
    squeeze = rope(t, 9345, 1.48, 0.12, 180, 260, 0.8)
    exhale = svf(noise(len(t), 9346), 1100, 0.7) * window(t, 1.55, 1.7, 0.05) * np.exp(-np.maximum(0, t - 1.55) / 0.1) * 0.35
    return finish("sfx_phaister_wring", twists + wet + squeeze + exhale, s)


def hex_stab():
    """HEX's recast (0.95 s): the doll yanked up (a cloth whoosh), the pin STABBED into its eye (a dry thud and a stitch), a
    squelchy pop, then a music-box sting falling out of tune."""
    s = 0.95
    t = times(s)
    yank = svf(noise(len(t), 9351), sweep(t, 600, 2200, 0.1), 1.2) * window(t, 0.0, 0.12, 0.02) * 0.6
    stab = svf(noise(len(t), 9352), 1400, 1.1) * env_ar(np.maximum(0, t - 0.22), 0.002, 0.03) * (t >= 0.22) * 1.2
    thread = stitch(t, 9353, 0.21, 0.8)
    local = np.maximum(0, t - 0.25)
    pop = np.sin(2 * np.pi * np.cumsum(np.where(t >= 0.25, 320 * np.exp(-local / 0.04) + 90, 0.0)) / RATE) \
        * np.exp(-local / 0.05) * (t >= 0.25) * 0.9
    sting = musicbox(t, 0.32, [(0.0, 1318.5), (0.12, 1108.7), (0.24, 880.0), (0.40, 622.3)], 0.06, 0.5)
    return finish("sfx_cast_phaister_hexstab", yank + stab + thread + pop + sting, s)


def status_drained():
    """DRAINED landing on its victim (0.8 s): two pins stamped over them (two ticks), and the breath going out of them."""
    s = 0.8
    t = times(s)
    pins = tick(t, 0.0, 2500, 0.8) + tick(t, 0.07, 2150, 0.7)
    out_ = svf(noise(len(t), 9361), sweep(t, 1400, 500, 0.6), 0.8) * window(t, 0.08, 0.75, 0.08) * 0.4
    return finish("sfx_status_drained", pins + out_, s, 0.55)


def status_hexed():
    """HEXED landing on its victim (1.4 s): the stitch-blink across their eyes (a quick zip), then a detuned music box,
    muffled, with whispers under it (bands of noise breathing at speech-like rates; never a voice)."""
    s = 1.4
    t = times(s)
    blink = svf(noise(len(t), 9371), 3000, 1.8) * window(t, 0.0, 0.1, 0.01) * 0.6
    box = one_pole_low(musicbox(t, 0.1, [(0.0, 783.99), (0.18, 698.46), (0.36, 587.33), (0.6, 523.25)], 0.08, 1.0), 1400) * 0.6
    whisper = sum(svf(noise(len(t), 9372 + k), 2600 + 700 * k, 2.0) * (0.5 + 0.5 * np.sin(2 * np.pi * (4.1 + 1.3 * k) * t)) ** 2
                  for k in range(3)) * window(t, 0.15, 1.35, 0.2) * 0.22
    return finish("sfx_status_hexed", blink + box + whisper, s, 0.55)


if __name__ == "__main__":
    rows = [blink_cast(), swarm_knit(), doll_cast(), manika_land(), manika_steal(), manika_crumble(), pin_cast(),
            moonlight_on(), moonlight_off(), omen_cast(), omen_open(), omen_close(), theme(),
            drain_cast(), hex_cast(), reach_mark(), reach_snap(), wring(), hex_stab(), status_drained(), status_hexed()]
    report = {
        "provenance": "Original deterministic synthesis (numpy only); no external samples, voices or paid API.",
        "listening": "Not yet heard by the owner in the game mix. Peak and RMS are measurements, not approval.",
        "cues": rows,
    }
    out = ROOT / "docs/reports/phaister-kit-2026-09-27/phaister-audio.json"
    out.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"Authored {len(rows)} Phaister cues; nothing else touched.")
