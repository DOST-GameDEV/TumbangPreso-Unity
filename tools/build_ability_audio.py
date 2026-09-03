"""Replace the synthesised placeholder cues with real CC0 recordings.

WHY THIS EXISTS
---------------
`Attention.md` section 9: *"Every one of the 117 sound effects in the project is currently
SYNTHESISED by generate_hero_audio.py as a placeholder."* `docs/TODO.md` section 131 replaces
what can be replaced, and `docs/Asset_Sourcing.md` section 5.1 is the licensed source list.

WARNING  THIS REPLACES THE PHYSICAL LAYER AND NOT THE ELEMENTAL ONE, AND THAT SPLIT IS A
CREDENTIAL RATHER THAN A CHOICE. `Asset_Sourcing.md` section 5.2 names sixteen individual
Freesound recordings for fire, ice, thunder, rock and dark magic. **Freesound requires an
account to download**: `curl` on any of those sound pages redirects to `/home/login/`. Creating
one is not something this toolchain may do, so every cue whose source is in 5.2 is untouched and
is listed in `Attention.md`. The Kenney packs in 5.1 need no account, so those cues are done.

WARNING  A RECORDING IS NOT AUTOMATICALLY BETTER THAN A SYNTH, AND THE TABLE BELOW ONLY LISTS
CUES WHERE IT IS. `lata_impact` is the clearest: the lata is a tin can and Kenney's
`impactTin_medium` is a recording of one being hit, where the placeholder is a filtered click.
Cues with no honest source in the free packs keep their placeholder rather than getting a
near-miss, and the REASON is recorded beside them at the bottom of this file. A worse sound that
came from a pack is still a worse sound.

WARNING  THE JINGLES ARE DELIBERATELY NOT USED. `Asset_Sourcing.md` section 5.1 lists Kenney
Music Jingles for round win, loss and match win. The pack is 8-bit NES, pizzicato, sax and steel
jingles; this game's identity is a Filipino street and its front end is carved wood and warm
cream. A chiptune win sting is a different game's voice, and music is a judgement rather than an
asset swap. `Attention.md` section 4 already puts the soundtrack with a person.

WARNING  EVERY OUTPUT IS MONO 44.1 kHz WAV, WHICH IS WHAT THE PROJECT ALREADY IS.
`Resources/Sfx/` is 117 mono 44.1 kHz files and `AudioDirector` spatialises them itself, so a
stereo cue would be silently downmixed by the engine at a position that has no stereo field.
`docs/TODO.md` section 131.2 asks for exactly this.

WARNING  IT NORMALISES TO THE CUE IT REPLACES, NOT TO FULL SCALE. `AudioCues.TrimDb` is a
measured mix: the SFX bus was found CLIPPING at +2.0 dBFS and those trims are what answered it.
A replacement that arrives 6 dB hotter than the file it replaces silently undoes that
measurement for one cue, and the only place it shows up is a busy round.

USAGE
-----
    python tools/build_ability_audio.py [--src DIR] [--dry-run]

`--src` defaults to `scratchpad/asset-src/`. Run `tools/fetch_asset_sources.py` first.
"""

import argparse
import os
import sys

try:
    import numpy as np
except ImportError:
    print("build_ability_audio: numpy is required (pip install numpy)")
    sys.exit(2)

try:
    import soundfile as sf
except ImportError:
    print("build_ability_audio: soundfile is required (pip install soundfile)")
    print("    It is the only thing on this machine that can decode Kenney's .ogg packs;")
    print("    there is no ffmpeg on PATH and Unity ships none.")
    sys.exit(2)

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC_DIR = os.path.join(REPO, "scratchpad", "asset-src")
SFX_DIR = os.path.join(REPO, "Assets", "TumbangPreso", "Resources", "Sfx")

RATE = 44100

IMPACT = "kenney_impact-sounds/Audio"
INTERFACE = "kenney_interface-sounds/Audio"
RPG = "kenney_rpg-audio/Audio"


# ---------------------------------------------------------------------------
# THE REPLACEMENTS.
#
# (cue, [source files, layered], gain, max seconds or 0 for "as recorded", why)
#
# WARNING  A LIST OF MORE THAN ONE SOURCE IS A LAYER, NOT A CHOICE. Two files are summed and
# then renormalised, which is how a lata knockdown gets both the tin and the roll. Where a cue
# only needs one recording it names one.
#
# WARNING  THE `why` IS NOT DECORATION. It is the thing a future session needs in order to
# disagree with a choice: "impactTin because the lata is a tin can" can be argued with, and
# "impactTin" cannot.
# ---------------------------------------------------------------------------
REPLACEMENTS = [
    # --- The lata. The single most important object in the game. ---
    ("lata_impact", [f"{IMPACT}/impactTin_medium_000.ogg"], 1.0, 0.00,
     "the lata is a tin can and this is a recording of one being struck"),

    ("lata_knockdown", [f"{IMPACT}/impactTin_medium_001.ogg",
                        f"{IMPACT}/impactMetal_light_003.ogg"], 1.0, 0.00,
     "the can taking the hit and then the can going over, layered"),

    # --- Bodies. ---
    ("bump", [f"{IMPACT}/impactSoft_medium_000.ogg"], 1.0, 0.00,
     "shoulder to shoulder, and `hit_body` aliases onto this one"),

    ("tag", [f"{IMPACT}/impactSoft_medium_003.ogg"], 0.9, 0.00,
     "the tag is the moment the round turns, so it is the same family as a bump and lighter"),

    ("downed", [f"{IMPACT}/impactSoft_heavy_000.ogg"], 1.0, 0.00,
     "a body reaching the road"),

    ("guard_block", [f"{IMPACT}/impactPlate_medium_000.ogg"], 1.0, 0.30,
     "a block is a hard flat stop, which is what a plate impact is"),

    ("sfx_hitmarker", [f"{IMPACT}/impactGeneric_light_000.ogg"], 0.8, 0.00,
     "the shortest confirmation in the game and it must not be a THUD"),

    # --- Feet and tsinelas on asphalt. ---
    ("land", [f"{IMPACT}/footstep_concrete_000.ogg"], 1.0, 0.00,
     "concrete, because every map surface in this game is a street"),

    ("slipper_land", [f"{IMPACT}/footstep_carpet_000.ogg"], 1.0, 0.00,
     "rubber on road is a soft slap, which is the carpet step and not the concrete one"),

    ("slipper_bounce", [f"{IMPACT}/footstep_carpet_003.ogg"], 0.85, 0.00,
     "the same slap one bounce quieter"),

    # --- Dante's ground. The one elemental cue the free packs genuinely cover. ---
    ("sfx_quake_slam", [f"{IMPACT}/impactMining_000.ogg"], 1.0, 0.00,
     "rock and debris, which is what Asset_Sourcing section 5.3 asks for on his body layer"),

    # --- Cheska's ice. Glass is the honest stand-in for ice breaking. ---
    ("sfx_ice_shatter", [f"{IMPACT}/impactGlass_medium_000.ogg"], 1.0, 0.00,
     "breaking glass IS the sound ice makes; section 5.2's own ice recordings need a login"),

    ("sfx_stun_break", [f"{IMPACT}/impactGlass_light_000.ogg"], 0.9, 0.00,
     "fighting out of a coat, which is the same material one size down"),

    # --- Hands. ---
    ("grab", [f"{RPG}/cloth1.ogg"], 1.0, 0.16,
     "picking a tsinelas up is cloth and rubber, not a click; `pickup` aliases onto this"),

    ("throw_whoosh", [f"{RPG}/knifeSlice.ogg"], 0.85, 0.00,
     "an arm through air; `throw_release` aliases onto this"),

    ("dash", [f"{RPG}/cloth3.ogg"], 1.0, 0.26,
     "a body moving fast is fabric, and `bump_swing` aliases onto this"),

    # --- The front end. ---
    ("ui_click", [f"{INTERFACE}/click_001.ogg"], 1.0, 0.00,
     "a press. `click_002` is 10 ms, which is a tick rather than a button"),
    ("ui_hover", [f"{INTERFACE}/tick_002.ogg"], 1.0, 0.00,
     "it fires on every mouse movement across a menu, so it is the quietest thing here"),
    ("ui_back", [f"{INTERFACE}/back_001.ogg"], 1.0, 0.00, "one step out"),
    ("ui_error", [f"{INTERFACE}/error_004.ogg"], 1.0, 0.00, "a refusal"),

    ("countdown_tick", [f"{INTERFACE}/tick_004.ogg"], 1.0, 0.00, "three of these then the go"),
    ("countdown_go", [f"{INTERFACE}/confirmation_001.ogg"], 1.0, 0.00, "the round starting"),
    ("score_award", [f"{INTERFACE}/pluck_001.ogg"], 1.0, 0.00, "a point landing"),
    ("sfx_super_ready", [f"{INTERFACE}/confirmation_002.ogg"], 1.0, 0.00,
     "the ultimate banking, which the player has to hear over a fight"),
    ("stamina_empty", [f"{INTERFACE}/error_008.ogg"], 0.8, 0.00,
     "out of breath is a refusal, quieter than a menu one because it fires mid-run"),

    ("reset_channel_start", [f"{INTERFACE}/open_001.ogg"], 1.0, 0.00, "the taya starting the reset"),
    ("reset_channel_complete", [f"{INTERFACE}/close_001.ogg"], 1.0, 0.00,
     "the reset landing; `reset_complete` aliases onto this"),
]


# ---------------------------------------------------------------------------
# LEFT ALONE ON PURPOSE, WITH THE REASON. Anything not here and not above simply has no
# candidate in the free packs at all.
# ---------------------------------------------------------------------------
KEPT = {
    "hero_*_grunt / hero_*_ult":
        "Tagalog, recorded in-house. Attention.md section 9. A generic English grunt pack "
        "would take the one thing that makes this game sound like it is from here.",
    "sfx_fire_whoosh, sfx_ice_form, sfx_ice_freeze, sfx_thunder_impact, sfx_lightning_strike":
        "Asset_Sourcing section 5.2 names a specific CC0 Freesound recording for each and "
        "Freesound requires an account to download. Attention.md.",
    "round_win, round_lose, round_end, match_win, boot_sting":
        "Kenney Music Jingles is 8-bit, pizzicato, sax and steel. This game is a Filipino "
        "street in carved wood and warm cream. See the module docstring.",
    "sfx_sky_*, sfx_lrt_pass":
        "Ambience beds, 7 to 10 s. The one real recording already in the project is the train, "
        "and the rest need the Manila street bed in Asset_Sourcing section 7 (CC BY, credit).",
    "sfx_cast_*, sfx_var_*":
        "Eighteen ability casts and twelve variant stings. Every one is an ELEMENT, and the "
        "elemental sources are the ones behind the login. Replacing them with impact foley "
        "would make six heroes sound like one.",
}


def load(path):
    """One source file as mono float at 44.1 kHz."""
    data, rate = sf.read(path, always_2d=True, dtype="float64")
    mono = data.mean(axis=1)

    if rate != RATE:
        # ⚠️ LINEAR RESAMPLE, AND EVERY KENNEY PACK IS ALREADY 44100 SO IT NEVER RUNS TODAY.
        # It is here so a pack that ships at 48 k does not silently play at the wrong pitch,
        # which is the failure mode that looks like bad performance rather than bad audio.
        n = int(round(len(mono) * RATE / float(rate)))
        mono = np.interp(np.linspace(0.0, len(mono) - 1, n),
                         np.arange(len(mono)), mono)

    return mono


def trim(x):
    """Cut leading and trailing near-silence.

    ⚠️ A CUE IS TRIGGERED, NOT SCHEDULED. Sixty milliseconds of silence at the head of an
    impact is sixty milliseconds of latency between the hit landing and the player hearing it,
    and it is invisible in every waveform view because it looks like nothing.

    ⚠️⚠️ THE FLOOR IS A FRACTION OF THE FILE'S OWN PEAK AND NOT AN ABSOLUTE, AND THE FIRST
    VERSION USED AN ABSOLUTE AND ATE THE UI CUES. At a fixed 0.002, `click_002` came out
    **0.01 s long** and `tick_002` 0.02 s: those files peak around 0.3, so almost all of their
    body sits under a floor written for an impact that peaks at 0.9. A tenth of a per cent of
    the peak keeps the decay on every cue whatever its level.
    """
    peak = float(np.abs(x).max()) if len(x) else 0.0
    floor = max(1e-5, peak * 0.001)
    above = np.nonzero(np.abs(x) > floor)[0]
    if len(above) == 0:
        return x
    return x[above[0]:above[-1] + 1]


def cap(x, seconds):
    """Hold a cue to a length, with a fade rather than a cut.

    ⚠️⚠️ A REPLACEMENT MAY NOT BE LONGER THAN THE THING IT REPLACES WITHOUT SOMEBODY SAYING SO.
    `cloth1` is a 0.59 s rustle and `grab` is a 0.06 s pickup that fires every time anybody
    touches a tsinelas: dropping the long one in makes four players sound like a laundry
    basket. The cap is per row so the decision is visible in the table rather than buried here.
    """
    n = int(RATE * seconds)
    if seconds <= 0.0 or len(x) <= n:
        return x
    return fade_tail(x[:n], ms=25.0)


def fade_tail(x, ms=8.0):
    """A short fade so a trimmed tail cannot click."""
    n = min(len(x), int(RATE * ms / 1000.0))
    if n <= 1:
        return x
    x = x.copy()
    x[-n:] *= np.linspace(1.0, 0.0, n)
    return x


def peak_of(path):
    """The peak of the file being replaced, so the new one lands where the mix expects it."""
    if not os.path.isfile(path):
        return 0.85
    data, _ = sf.read(path, always_2d=True, dtype="float64")
    p = float(np.abs(data).max())
    return p if p > 0.01 else 0.85


def build(cue, sources, gain, max_s, src_dir, out_dir, dry):
    parts = []
    for rel in sources:
        path = os.path.join(src_dir, *rel.split("/"))
        if not os.path.isfile(path):
            print(f"  MISSING SOURCE  {cue}: {rel}")
            return None
        parts.append(trim(load(path)))

    n = max(len(p) for p in parts)
    mixed = np.zeros(n)
    for p in parts:
        mixed[:len(p)] += p

    mixed = cap(fade_tail(trim(mixed)), max_s)

    peak = float(np.abs(mixed).max())
    if peak < 1e-6:
        print(f"  SILENT  {cue}")
        return None

    target = peak_of(os.path.join(out_dir, cue + ".wav")) * gain
    mixed = mixed * (target / peak)

    out = os.path.join(out_dir, cue + ".wav")
    if not dry:
        sf.write(out, mixed.astype(np.float32), RATE, subtype="PCM_16")

    return len(mixed) / float(RATE), target


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--src", default=SRC_DIR)
    ap.add_argument("--out", default=SFX_DIR)
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()

    if not os.path.isdir(args.out):
        print(f"build_ability_audio: no cue folder at {args.out}")
        sys.exit(2)

    print(f"{'cue':28s} {'was':>7s} {'now':>7s} {'peak':>5s}  source")
    done = 0
    for cue, sources, gain, max_s, why in REPLACEMENTS:
        before = os.path.join(args.out, cue + ".wav")
        was = "-"
        if os.path.isfile(before):
            d, r = sf.read(before, always_2d=True)
            was = f"{len(d) / r:.2f}s"

        result = build(cue, sources, gain, max_s, args.src, args.out, args.dry_run)
        if result is None:
            continue

        length, peak = result
        done += 1
        print(f"{cue:28s} {was:>7s} {length:6.2f}s {peak:5.2f}  "
              f"{', '.join(os.path.basename(s) for s in sources)}")

    print()
    print(f"build_ability_audio: {done} of {len(REPLACEMENTS)} cues replaced"
          + (" (dry run, nothing written)" if args.dry_run else ""))
    print()
    print("Left on their placeholders, on purpose:")
    for what, why in KEPT.items():
        print(f"  {what}")
        print(f"      {why}")
    print()
    print("Run Checks.RunAll after this. AudioCueCheck is the gate that says no cue became")
    print("fileless or unreachable, and it reads the container as well as the extension.")


if __name__ == "__main__":
    main()
