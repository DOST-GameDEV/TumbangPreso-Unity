"""Replace the synthesised placeholder cues with real CC0 recordings.

WHY THIS EXISTS
---------------
`Attention.md` section 9: *"Every one of the 117 sound effects in the project is currently
SYNTHESISED by generate_hero_audio.py as a placeholder."* `docs/TODO.md` section 131 replaces
what can be replaced, and `docs/Asset_Sourcing.md` section 5.1 is the licensed source list.

WARNING  IT REPLACES THE PHYSICAL LAYER AND THE ELEMENTAL ONE, AND UNTIL 2026-09-04 IT COULD
ONLY DO THE FIRST. `Asset_Sourcing.md` section 5.2 names sixteen individual CC0 Freesound
recordings for fire, ice, thunder, rock and dark magic, and every one of those URLs answers 302
to `/home/login/`: a credential, not a choice. They are downloaded now and live in
`scratchpad/asset-src/freesound/`, which is gitignored, so the thirty `sfx_cast_*` and
`sfx_var_*` cues that section 5.2 exists for are sourced rather than synthesised. THE FOLDER IS
NOT IN THE REPOSITORY. A checkout without it regenerates nothing and says so per row rather than
writing silence, which is `MISSING SOURCE` below.

WARNING  THREE OF THE SIXTEEN ARE DELIBERATELY UNUSED AND THE REASON IS IN `KEPT`. The tin can
is the loudest case: section 5.2 names it for `lata_impact` and `lata_knockdown`, and section
5.4 records those exact cues being REJECTED BY EAR after the 2026-09-03 pass. A source table
written before a listening test does not overrule the listening test.

WARNING  A SLICE IS A MEASUREMENT, NOT AN OFFSET SOMEBODY TYPED. Section 5.3 says in as many
words *"Do not reuse one full cue for all three powers"* and *"Do not give all three abilities
the same witch sound"*, and six heroes with five cues each against thirteen usable recordings is
exactly that risk. `Slice(src, rank=n)` takes the **rank-th loudest non-overlapping window** of a
recording, so "the third loudest 1.1 s of the earthquake take" is reproducible, is different
material from the first, and cannot land on silence the way a hand-typed start time can.

WARNING  A RECORDING IS NOT AUTOMATICALLY BETTER THAN AN EXISTING CUE, AND THE TABLE BELOW ONLY
LISTS CUES WHERE IT IS. The first pass replaced `lata_impact`, `lata_knockdown` and `ui_hover`
because their source labels looked exact. The played comparison disagreed: 🧑 preferred the old
can hit, can down, and the whole ui_* press family. They are protected in `KEPT` now, so rerunning this script
cannot silently undo that decision. Cues with no honest improvement keep what they have rather
than getting a near-miss. The REASON is recorded beside them at the bottom of this file.

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
import json
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

HERE_TOOLS = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE_TOOLS)
SRC_DIR = os.path.join(REPO, "scratchpad", "asset-src")
SFX_DIR = os.path.join(REPO, "Assets", "TumbangPreso", "Resources", "Sfx")

RATE = 44100

# ---------------------------------------------------------------------------
# THE SIXTEEN CC0 FREESOUND RECORDINGS, `Asset_Sourcing.md` section 5.2.
#
# WARNING  NAMED HERE ONCE AND BY FULL FILENAME. The number in the filename IS the Freesound
# id, so a row below is traceable to the licence row in section 5.2 without a second table to
# keep in step. `Asset_Sourcing.md` section 5 is the licence record; this is the wiring.
# ---------------------------------------------------------------------------
FS = "freesound"

FIRE = f"{FS}/244926__hnhnh__fire-whoosh.wav"
ICE_LONG = f"{FS}/822369__frigeriose__ice-cracking.wav"
ICE_SHORT = f"{FS}/764657__getwecked__ice-cracking.wav"
FREEZE = f"{FS}/685253__antonsoederberg__freeze-sound-effect-fx.wav"
THUNDER = f"{FS}/475818__pluralz__thunder-impact.wav"
THUNDER_RAW = (f"{FS}/844420__tsp-talk__extreme-thunder-impact-munich-trudering-"
               f"raw-excerpt-from-field-recording-250702_001.wav")
QUAKE = f"{FS}/51123__rutgermuller__sounds-for-earthquakes-wood-and-deep-plastic.wav"
WOODMETAL = f"{FS}/163494__tompallant__wood_metal_impact_shake_earthquake_banging.wav"
CRACKLE = f"{FS}/582631__ironcross32__long-crackle-04.wav"
DARKLOOP = f"{FS}/442825__qubodup__dark-magic-loop.wav"
DARKSPELL = f"{FS}/683628__dneproman__dark-spell-2.wav"
MAGIC = f"{FS}/455205__lilmati__magic-spell-02.wav"
CANROLL = f"{FS}/593129__21100375__can-rolling-over-floor.wav"

IMPACT = "kenney_impact-sounds/Audio"
INTERFACE = "kenney_interface-sounds/Audio"
RPG = "kenney_rpg-audio/Audio"


class Slice:
    """One piece of a recording, chosen by measurement rather than by a typed offset.

    WARNING  `rank` IS LOUDNESS ORDER, NOT TIME ORDER, AND THAT IS WHY IT IS USABLE. A start
    time typed against a 68 second field recording is a guess that can land in a gap, and the
    only way to find out is to listen to all thirty outputs. `window` below ranks every
    non-overlapping candidate of the requested length by RMS and returns the rank-th, so rank 0
    is the loudest moment in the take, rank 3 is the fourth, and two abilities given different
    ranks are given different material by construction.

    WARNING  `pitch` RESAMPLES, SO IT CHANGES THE LENGTH TOO. It is here because section 5.3
    asks for *"a separate low coven toll"* and for a carapace *"quieter than the stomp"*: one
    recording has to become several distinct objects, and a pitch move is the cheapest honest
    way to make a low one. 0.65 is roughly a fifth down.

    WARNING  `gain` IS PER SOURCE AND THE ROW'S OWN GAIN IS STILL APPLIED ON TOP. A layer is
    summed and then the whole cue is renormalised to the peak of the file it replaces, so this
    is the BALANCE between layers and never the output level. `AudioCues.TrimDb` is the mix and
    is not this file's business.
    """

    def __init__(self, src, rank=0, seconds=0.0, pitch=1.0, reverse=False, gain=1.0):
        self.src = src
        self.rank = rank
        self.seconds = seconds
        self.pitch = pitch
        self.reverse = reverse
        self.gain = gain


def window(x, rank, seconds):
    """The rank-th loudest non-overlapping `seconds` window of a recording.

    WARNING  NON-OVERLAPPING IS THE HALF THAT MATTERS. Ranking every sample offset by RMS
    returns rank 0, rank 1 and rank 2 all sitting on the same transient one millisecond apart,
    which is a table of three identical cues that looks like three different ones.
    """
    n = int(RATE * seconds)
    if seconds <= 0.0 or len(x) <= n or n <= 0:
        return x

    count = len(x) // n
    if count <= 1:
        return x[:n]

    energies = []
    for i in range(count):
        chunk = x[i * n:(i + 1) * n]
        energies.append((float(np.sqrt(np.mean(chunk * chunk))), i))

    energies.sort(key=lambda e: -e[0])
    pick = energies[min(rank, len(energies) - 1)][1]
    return x[pick * n:(pick + 1) * n]


def repitch(x, factor):
    """Resample so the recording plays at `factor` of its original pitch.

    WARNING  LINEAR, AND THAT IS ADEQUATE HERE FOR ONE REASON: every output is a sub-second to
    two-second cue played once over a fight, not a sustained tone. `load` already carries the
    same interpolator for sample-rate conversion and says the same thing.
    """
    if abs(factor - 1.0) < 1e-6 or len(x) < 2:
        return x
    n = max(2, int(round(len(x) / factor)))
    return np.interp(np.linspace(0.0, len(x) - 1, n), np.arange(len(x)), x)


def material(spec, src_dir):
    """One source row as mono float, whether it is a whole file or a Slice of one."""
    if isinstance(spec, Slice):
        path = os.path.join(src_dir, *spec.src.split("/"))
        if not os.path.isfile(path):
            return None, spec.src
        x = trim(load(path))
        x = window(x, spec.rank, spec.seconds)
        x = repitch(x, spec.pitch)
        if spec.reverse:
            x = x[::-1].copy()
        return x * spec.gain, spec.src

    path = os.path.join(src_dir, *spec.split("/"))
    if not os.path.isfile(path):
        return None, spec
    return trim(load(path)), spec


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
    # ⚠️ THE KENNEY TRANSIENT STAYS AND THE EARTHQUAKE TAKE JOINS IT UNDERNEATH, which is
    # section 5.3's sentence in full: "earthquake/wood/metal recordings for the BODY, Kenney
    # impacts for the TRANSIENT". This row was the transient alone for as long as the body was
    # behind a login.
    ("sfx_quake_slam", [f"{IMPACT}/impactMining_000.ogg",
                        Slice(QUAKE, rank=0, seconds=0.80, gain=0.85)], 1.0, 0.85,
     "rock and debris, which is what Asset_Sourcing section 5.3 asks for on his body layer"),

    # --- Cheska's ice. Glass is the honest stand-in for ice breaking. ---
    # ⚠️ GLASS WAS THE STAND-IN AND THE REAL ICE IS HERE NOW. Section 5.2 names 764657 as
    # exactly this: "Short sfx_ice_shatter transient". The glass layer is kept underneath
    # because the recording is a crack rather than a break and the Kenney hit is the snap.
    ("sfx_ice_shatter", [Slice(ICE_SHORT, rank=0, seconds=0.50),
                         Slice(f"{IMPACT}/impactGlass_medium_000.ogg", gain=0.55)], 1.0, 0.55,
     "the short ice crack from section 5.2, over the glass transient that stood in for it"),

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
    # ⚠️⚠️ EVERY `ui_*` CUE HAS LEFT THIS LIST. See KEPT: the whole family was rejected by
    # ear on 2026-09-04. What remains below is MATCH audio that happens to be sourced from
    # the same interface pack, not front-end chrome, and none of it was judged.
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

    # -----------------------------------------------------------------------
    # THE ELEMENTAL LAYER. Every row below is `Asset_Sourcing.md` section 5.2's "use in this
    # game" column or section 5.3's per-hero paragraph, and nothing below is a cue somebody
    # invented a use for: the thirty `sfx_cast_*` and `sfx_var_*` files already exist and are
    # already played, and this replaces what is in them.
    #
    # ⚠️⚠️ THE CAP ON EVERY ROW IS THE LENGTH OF THE CUE IT REPLACES, MEASURED. A cast cue in
    # this project is 0.54 s to 1.70 s and the recordings behind them run to 68 seconds, so a
    # row with no cap would drop a field recording into a fight. The numbers are the placeholders'
    # own durations rather than a taste: `sfx_cast_dante_stomp` is 0.85 s, so its replacement is.
    # -----------------------------------------------------------------------

    # --- Dante: earth. Section 5.3, "earthquake/wood/metal recordings for the body". ---
    ("sfx_cast_dante_stomp", [Slice(QUAKE, rank=0, seconds=0.55),
                              Slice(f"{IMPACT}/impactMining_000.ogg", gain=0.8)], 1.0, 0.85,
     "the loudest moment of the earthquake take under the mining transient"),

    ("sfx_cast_dante_fissure", [Slice(WOODMETAL, rank=1, seconds=1.50)], 1.0, 1.60,
     "section 5.2 names 163494 for the fissure debris; rank 1 keeps it off the stomp's material"),

    ("sfx_cast_dante_carapace", [Slice(QUAKE, rank=2, seconds=1.00)], 0.75, 1.10,
     "section 5.3: carapace movement should be QUIETER than the stomp, hence the 0.75"),

    ("sfx_var_dante_tremor", [Slice(WOODMETAL, rank=3, seconds=1.00, pitch=0.85)], 0.9, 1.10,
     "a tremor is the same street one fifth lower and further off than the fissure"),

    ("sfx_var_dante_plating", [Slice(WOODMETAL, rank=2, seconds=1.10),
                               Slice(f"{IMPACT}/impactPlate_medium_000.ogg", gain=0.7)],
     0.85, 1.25,
     "rock closing over a body, so the metal take with the plate hit on top"),

    # --- Cheska: ice. Section 5.3, and its "do not reuse one full cue for all three". ---
    ("sfx_ice_form", [Slice(ICE_LONG, rank=0, seconds=1.00)], 1.0, 1.10,
     "section 5.2 names 822369 for sfx_ice_form"),

    ("sfx_ice_freeze", [Slice(FREEZE, rank=0, seconds=1.00)], 1.0, 1.10,
     "section 5.2 names 685253 for sfx_ice_freeze"),

    ("sfx_barricade_raise", [Slice(ICE_LONG, rank=1, seconds=0.95)], 1.0, 1.00,
     "section 5.2 names 822369 for the barricade raise; a different window from sfx_ice_form"),

    ("sfx_cast_cheska_barricade", [Slice(ICE_LONG, rank=2, seconds=1.00),
                                   Slice(FREEZE, rank=0, seconds=0.60, gain=0.5)], 1.0, 1.05,
     "the wall going up, with the freeze effect as section 5.3's magical high layer"),

    ("sfx_cast_cheska_sheet", [Slice(ICE_LONG, rank=3, seconds=1.15)], 0.9, 1.20,
     "the sheet is the long take's formation variation and the widest of her three"),

    ("sfx_cast_cheska_nova", [Slice(FREEZE, rank=0, seconds=1.40),
                              Slice(ICE_SHORT, rank=0, seconds=0.50, gain=0.6)], 1.0, 1.45,
     "section 5.2: 685253 IS the Glacial Nova cast layer, with the crack as its edge"),

    ("sfx_var_cheska_spires", [Slice(ICE_LONG, rank=4, seconds=0.95)], 1.0, 1.00,
     "spires are ice forming upward: a fifth window of the same take"),

    ("sfx_var_cheska_blackice", [Slice(ICE_LONG, rank=5, seconds=1.00, pitch=0.80)], 0.95, 1.05,
     "black ice is the same material heard through the road, so it is pitched down"),

    # --- Sean: fire. Section 5.3, fire whoosh as the recorded body plus Kenney layers. ---
    ("sfx_fire_whoosh", [Slice(FIRE, rank=0, seconds=0.85)], 1.0, 0.90,
     "section 5.2 names 244926 for sfx_fire_whoosh"),

    ("sfx_cast_sean_rush", [Slice(FIRE, rank=0, seconds=0.95),
                            Slice(f"{RPG}/cloth3.ogg", gain=0.45)], 1.0, 1.00,
     "section 5.3 wants a separate short whoosh layer for rush; the cloth is the body moving"),

    ("sfx_cast_sean_cannon", [Slice(FIRE, rank=1, seconds=0.75),
                              Slice(f"{IMPACT}/impactGeneric_light_000.ogg", gain=0.7)],
     1.0, 0.80,
     "a cannon is a shorter, harder whoosh than the rush, so a different window and a hit"),

    ("sfx_cast_sean_supernova", [Slice(FIRE, rank=0, seconds=1.50, pitch=0.80),
                                 Slice(f"{IMPACT}/impactMining_000.ogg", gain=0.55)], 1.0, 1.55,
     "the ultimate is the same fire an octave heavier, over debris"),

    ("sfx_var_sean_flare", [Slice(FIRE, rank=2, seconds=0.60)], 1.0, 0.62,
     "the shortest of the three fire windows, because a flare is the shortest of his cues"),

    ("sfx_var_sean_afterburn", [Slice(FIRE, rank=1, seconds=1.25, pitch=0.90)], 0.85, 1.30,
     "afterburn is what is left behind: quieter, lower and longer than the cast"),

    # --- Zack: electric. Section 5.3, crackle for movement, thunder for the hit. ---
    ("sfx_thunder_impact", [Slice(THUNDER, rank=0, seconds=1.45)], 1.0, 1.50,
     "section 5.2 names 475818 for sfx_thunder_impact"),

    ("sfx_lightning_strike", [Slice(THUNDER, rank=1, seconds=1.15),
                              Slice(CRACKLE, rank=0, seconds=0.50, gain=0.6)], 1.0, 1.20,
     "the strike is thunder with the crackle on its front edge"),

    ("sfx_cast_zack_sprint", [Slice(CRACKLE, rank=0, seconds=1.05)], 1.0, 1.10,
     "section 5.3: electric crackle for MOVEMENT, which is Bolt Sprint"),

    ("sfx_cast_zack_magnet", [Slice(CRACKLE, rank=0, seconds=0.90, pitch=0.85)], 0.95, 0.95,
     "section 5.3: the same crackle for CHARGE, pitched apart so the two are distinguishable"),

    ("sfx_cast_zack_summon", [Slice(THUNDER, rank=0, seconds=1.00),
                              Slice(THUNDER_RAW, rank=0, seconds=1.30, gain=0.7)], 1.0, 1.35,
     "section 5.3: Thunder Impact for the hit and ONLY a short slice of raw thunder for the tail"),

    ("sfx_var_zack_arcline", [Slice(CRACKLE, rank=0, seconds=1.00, pitch=1.15)], 1.0, 1.05,
     "an arc is a thinner, higher crackle than the sprint"),

    ("sfx_var_zack_discharge", [Slice(CRACKLE, rank=0, seconds=0.68),
                                Slice(THUNDER, rank=2, seconds=0.60, gain=0.5)], 1.0, 0.72,
     "a discharge is the crackle ending in a small thunder rather than starting one"),

    # --- Nemu: void. Section 5.3, dark loop as a QUIET bed. ---
    ("sfx_cast_nemu_veil", [Slice(DARKLOOP, rank=0, seconds=1.10, gain=0.8),
                            Slice(DARKSPELL, rank=0, seconds=0.70)], 1.0, 1.15,
     "section 5.3: the loop as a quiet bed with a short dark-spell transient over it"),

    ("sfx_cast_nemu_seance", [Slice(DARKLOOP, rank=0, seconds=1.55, pitch=0.80),
                              Slice(DARKSPELL, rank=1, seconds=0.90, gain=0.7)], 1.0, 1.60,
     "the seance is the longest and lowest of his three, which is what a bed is for"),

    ("sfx_cast_nemu_hijack", [Slice(DARKSPELL, rank=2, seconds=1.00),
                              Slice(f"{RPG}/knifeSlice.ogg", reverse=True, gain=0.7)], 1.0, 1.05,
     "section 5.3: hijack needs a DIRECTIONAL TRAVEL layer that is absent from veil, and the "
     "same paragraph asks for reversed whooshes"),

    ("sfx_var_nemu_fade", [Slice(DARKLOOP, rank=0, seconds=1.50, pitch=0.90, reverse=True)],
     0.85, 1.55,
     "a fade is the bed running backwards, which is arrival read as departure"),

    ("sfx_var_nemu_leash", [Slice(DARKSPELL, rank=3, seconds=0.68)], 1.0, 0.70,
     "the shortest dark-spell window, because a leash is a snap rather than a cast"),

    # --- Phaister: the witch. Section 5.3, and "do not give all three the same witch sound". ---
    ("sfx_hex_cast", [Slice(MAGIC, rank=0, seconds=0.95)], 1.0, 1.00,
     "section 5.2 names 455205 for sfx_hex_cast"),

    ("sfx_hex_afflict", [Slice(MAGIC, rank=0, seconds=0.75, pitch=0.90)], 0.9, 0.80,
     "section 5.2 names 455205 for sfx_hex_afflict; lower, because it lands on somebody"),

    ("sfx_blink_arrive", [Slice(MAGIC, rank=0, seconds=0.50, gain=0.9)], 1.0, 0.55,
     "section 5.2 names 455205 for the blink arrival; section 5.3 asks for it SHORT and DRY"),

    ("sfx_cast_phaister_hex", [Slice(MAGIC, rank=0, seconds=1.05)], 1.0, 1.10,
     "section 5.3: Magic Spell 02 for the written cast"),

    ("sfx_cast_phaister_blink", [Slice(MAGIC, rank=0, seconds=0.42)], 1.0, 0.90,
     "section 5.3: a SHORT DRY teleport arrival, so no bed under it and half the length"),

    ("sfx_cast_phaister_coven", [Slice(DARKLOOP, rank=0, seconds=1.65, pitch=0.65)], 1.0, 1.70,
     "section 5.3: a separate LOW coven toll, and section 5.2 names 442825 as the coven bed"),

    ("sfx_var_phaister_brand", [Slice(MAGIC, rank=0, seconds=0.90, pitch=1.10)], 1.0, 0.95,
     "a brand is the spell one step brighter, so the three of hers stay apart"),

    ("sfx_var_phaister_stride", [Slice(DARKLOOP, rank=0, seconds=1.20, pitch=0.90),
                                 Slice(f"{RPG}/cloth3.ogg", gain=0.45)], 0.9, 1.25,
     "a stride is the coven bed with a body moving through it"),

    # --- The lata. ---
    # ⚠️⚠️ THE SETTLE TAIL ONLY, AND NOT THE HIT. Section 5.2 names 593129 for "lata
    # settle/reset tail" and names the TIN CAN for `lata_impact` and `lata_knockdown`; section
    # 5.4 records those two being rejected by ear and restored. `lata_seal` is neither of them.
    ("lata_seal", [Slice(CANROLL, rank=0, seconds=1.05)], 1.0, 1.10,
     "section 5.2: the can settling, which is what sealing the lata is"),
]


# ---------------------------------------------------------------------------
# LEFT ALONE ON PURPOSE, WITH THE REASON. Anything not here and not above simply has no
# candidate in the free packs at all.
# ---------------------------------------------------------------------------
KEPT = {
    "lata_impact, lata_knockdown / can_knockdown":
        "The 2026-09-03 source pass replaced all three, and the played comparison rejected the "
        "new versions by name. The old can hit and can down are restored. These are protected "
        "choices, not missing work.",

    # ⚠️⚠️ THE WHOLE `ui_*` FAMILY, AND IT IS ONE DECISION RATHER THAN FOUR.
    # ui_hover went back on 2026-09-03. ui_click, ui_back and ui_error went back on
    # 2026-09-04: "i want to return old click sound bcz now it sounds weird", then
    # "replace all ui sfx changes with old", then "only ui sound effect changes".
    #
    # ⚠️ THE SCOPE IS THE `ui_*` PREFIX AND NOTHING ELSE. countdown_tick, countdown_go,
    # score_award, sfx_super_ready, stamina_empty and the two reset_channel cues are also
    # built from the Kenney interface pack and are MATCH audio, not front-end chrome. They
    # were deliberately left as they are, because the ask was "only ui sound effect changes"
    # and rolling them back too would have been the whole-batch rollback Attention.md
    # section 13 says not to do.
    #
    # ⚠️ THESE FOUR ARE THE MOST-HEARD SOUNDS IN THE GAME. ConvertedScreen, GodotButton and
    # MenuSfx route every press, every hover and every back through them, so they are heard
    # more often than any gameplay cue and a small wrongness in one costs more than a large
    # one anywhere else.
    "ui_click, ui_hover, ui_back, ui_error":
        "The entire front-end press family, rejected by ear and restored from ee8bced^ byte "
        "for byte: ui_click 8866 -> 4874, ui_back 5162 -> 10610, ui_error 9102 -> 17660, and "
        "ui_hover on 2026-09-03. Protected choices, not missing work. A future pass may not "
        "put a sourced press back without playing it to him first.",
    "hero_*_grunt / hero_*_ult":
        "Tagalog, recorded in-house. Attention.md section 9. A generic English grunt pack "
        "would take the one thing that makes this game sound like it is from here.",
    # ⚠️ THE FIVE NAMED ELEMENTAL CUES AND THE THIRTY CAST/VAR CUES CAME OFF THIS LIST ON
    # 2026-09-04, when the sixteen section 5.2 recordings were downloaded. They are rows in
    # REPLACEMENTS above. What is left here is what is still genuinely unanswerable.
    "sfx_sky_* ambience, and the two recordings that have no cue to go to":
        "708564 (basketball on concrete) is listed in section 5.2 as 'distant Bayan Plaza "
        "ambience detail' and 430046 (crowd cheer) as a 'tournament crowd bed'. THERE IS NO "
        "PLAZA AMBIENCE CUE AND NO CROWD BED CUE IN Resources/Sfx, so wiring either one would "
        "mean inventing a cue nobody asked for and then finding somewhere to play it. Both "
        "files are downloaded and sitting in scratchpad/asset-src/freesound/ for whoever adds "
        "the ambience layer; the sky cues are 7 to 10 s beds and still need the Manila street "
        "recording in section 7, which is CC BY and needs a credit line.",

    "134903 tin can, downloaded and deliberately unused":
        "Section 5.2 names it as the bright layer for lata_impact, lata_knockdown and "
        "can_knockdown. Section 5.4 records all three being replaced on 2026-09-03 and REJECTED "
        "BY EAR, and their originals restored. A source table written before a listening test "
        "does not overrule the listening test, and CLAUDE.md section 6 forbids putting a "
        "rejected sound back. It is downloaded so nobody has to fetch it again if he ever asks "
        "for a brighter can.",
    "round_win, round_lose, round_end, match_win, boot_sting":
        "Kenney Music Jingles is 8-bit, pizzicato, sax and steel. This game is a Filipino "
        "street in carved wood and warm cream. See the module docstring.",
    "sfx_sky_*, sfx_lrt_pass":
        "Ambience beds, 7 to 10 s. The one real recording already in the project is the train, "
        "and the rest need the Manila street bed in Asset_Sourcing section 7 (CC BY, credit).",

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


def remove_dc(x):
    """Take the constant offset out of a cue.

    ⚠️⚠️ A DC OFFSET IS A CLICK AT TRIGGER AND IT IS INAUDIBLE IN EVERY WAVEFORM VIEW. A cue
    whose samples sit around -0.12 instead of 0 starts by jumping the speaker cone from silence
    to -0.12 in one sample, every single time it fires, and `tools/audit_cue_audio.py` is what
    noticed: it flags `DC(...)` and it exits non-zero, so it gates a verification pass.

    ⚠️ IT IS APPLIED TO EVERY GENERATED CUE RATHER THAN THE NEW ONES ONLY. Slicing a recording
    at an arbitrary point is one way to acquire an offset and the Kenney packs arrived with
    theirs, so a rule that only covered the elemental rows would leave `countdown_tick` clicking
    at -0.139 for the same reason with the fix sitting one function away. **It changes no cue's
    character**: subtracting a constant moves nothing anybody can hear except the click.
    """
    if len(x) == 0:
        return x
    return x - float(np.mean(x))


def fade_tail(x, ms=8.0):
    """A short fade so a trimmed tail cannot click."""
    n = min(len(x), int(RATE * ms / 1000.0))
    if n <= 1:
        return x
    x = x.copy()
    x[-n:] *= np.linspace(1.0, 0.0, n)
    return x


# ---------------------------------------------------------------------------
# THE REFERENCE PEAKS.
#
# ⚠️⚠️ THIS LEDGER EXISTS BECAUSE THE LEVEL NORMALISATION WAS NOT IDEMPOTENT, AND A GENERATOR
# THAT IS NOT IDEMPOTENT IS A GENERATOR NOBODY CAN RUN TWICE. `peak_of` read the peak of the
# file it was about to OVERWRITE and the row's `gain` was multiplied onto it, so every re-run
# multiplied the gain again: `tag` at gain 0.9 goes 0.850, 0.765, 0.688, 0.620, and the sixth
# run is half the mix level the measurement in `AudioCues.TrimDb` was taken at. Nothing warns,
# every row still prints a plausible number, and the only symptom is that the game gets quieter
# in patches. It was found by running the tool a second time and diffing WAVs that no row in
# this file had changed.
#
# ⚠️ THE LEDGER IS THE PEAK OF THE CUE THE REPLACEMENT IS MEASURED AGAINST, WRITTEN DOWN ONCE.
# The docstring's rule is unchanged and is now actually true: "IT NORMALISES TO THE CUE IT
# REPLACES, NOT TO FULL SCALE". What changed is that "the cue it replaces" is a fixed number in
# a committed file rather than whatever happens to be on disk this minute.
#
# ⚠️ A CUE WITH NO ENTRY TAKES ITS CURRENT FILE'S PEAK AND THE ENTRY IS THEN WRITTEN. That is
# the same answer the old code gave on a first run, so adding a row needs no manual step; it is
# only the SECOND run that now behaves.
# ---------------------------------------------------------------------------
PEAKS_PATH = os.path.join(HERE_TOOLS, "assets", "cue_reference_peaks.json")

_peaks = None


def reference_peaks():
    global _peaks
    if _peaks is None:
        if os.path.isfile(PEAKS_PATH):
            with open(PEAKS_PATH, "r", encoding="utf-8") as f:
                _peaks = json.load(f)
        else:
            _peaks = {}
    return _peaks


def save_reference_peaks():
    os.makedirs(os.path.dirname(PEAKS_PATH), exist_ok=True)
    with open(PEAKS_PATH, "w", encoding="utf-8") as f:
        json.dump(reference_peaks(), f, indent=2, sort_keys=True)
        f.write("\n")


def target_peak(path, gain):
    """The level this cue is normalised to, decided once and then never recomputed.

    ⚠️⚠️ THE LEDGER HOLDS THE FINAL TARGET, GAIN ALREADY APPLIED, AND THAT IS THE WHOLE FIX.
    Storing the PRE-gain reference and multiplying on every run decays a row by `gain` per run
    exactly as the un-ledgered version did; the first attempt at this did that and `tag` went
    0.765 to 0.688 on a run whose row nobody had touched. What has to be stable across runs is
    the number the file ends up at, so that is the number that is written down.

    ⚠️ FIRST RUN REPRODUCES THE OLD BEHAVIOUR EXACTLY: the peak of the cue being replaced,
    times the row's gain. Every run after it reads that answer back.
    """
    cue = os.path.splitext(os.path.basename(path))[0]
    peaks = reference_peaks()

    if cue in peaks:
        return float(peaks[cue])

    if os.path.isfile(path):
        data, _ = sf.read(path, always_2d=True, dtype="float64")
        p = float(np.abs(data).max())
        p = p if p > 0.01 else 0.85
    else:
        p = 0.85

    peaks[cue] = p * gain
    return peaks[cue]


def build(cue, sources, gain, max_s, src_dir, out_dir, dry):
    parts = []
    for spec in sources:
        part, rel = material(spec, src_dir)
        if part is None:
            print(f"  MISSING SOURCE  {cue}: {rel}")
            return None
        parts.append(part)

    n = max(len(p) for p in parts)
    mixed = np.zeros(n)
    for p in parts:
        mixed[:len(p)] += p

    # ⚠️ DC LAST, AFTER trim AND cap. Removing it before they run leaves the SURVIVING
    # segment with its own offset, which is the version of this that measured as a
    # no-op: `countdown_tick` stayed at -0.139 with the call one line higher up.
    mixed = remove_dc(cap(fade_tail(trim(mixed)), max_s))

    peak = float(np.abs(mixed).max())
    if peak < 1e-6:
        print(f"  SILENT  {cue}")
        return None

    target = target_peak(os.path.join(out_dir, cue + ".wav"), gain)
    mixed = mixed * (target / peak)

    out = os.path.join(out_dir, cue + ".wav")
    if not dry:
        sf.write(out, mixed.astype(np.float32), RATE, subtype="PCM_16")

    return len(mixed) / float(RATE), target


def source_label(spec):
    """A source column a reader can act on: the file, and which slice of it."""
    if isinstance(spec, Slice):
        tail = os.path.basename(spec.src)
        bits = [f"{tail}#{spec.rank}"]
        if abs(spec.pitch - 1.0) > 1e-6:
            bits.append(f"x{spec.pitch:g}")
        if spec.reverse:
            bits.append("rev")
        return " ".join(bits)
    return os.path.basename(spec)


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
              f"{', '.join(source_label(s) for s in sources)}")

    # ⚠️ AFTER THE ROWS, SO A ROW THAT FAILED DOES NOT GET A REFERENCE PEAK RECORDED FOR AN
    # OUTPUT THAT WAS NEVER WRITTEN, AND NEVER ON A DRY RUN, WHICH IS SUPPOSED TO TOUCH NOTHING.
    if not args.dry_run:
        save_reference_peaks()

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
