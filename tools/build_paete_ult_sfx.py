"""Paete's ultimate, MAKILING'S EMBRACE: three whole soundtracks for its cutscene, cut from real
recordings. DRAFTS, for the owner's ear, each muxed onto the film so he can choose by ear.

  py -3 tools/build_paete_ult_sfx.py              (fetch what is missing, build a, b, c and the three videos)
  py -3 tools/build_paete_ult_sfx.py --no-video   (only the three wav files, README_ult.txt and report_ult.txt)
  py -3 tools/build_paete_ult_sfx.py --only b     (one of them; "b1,b2" for two; "abc" still means a, b and c)
  py -3 tools/build_paete_ult_sfx.py --video-only (mux the wav files that are there onto the frames again)

Owner, 2026-10-08, of the restaged cutscene (9.0 s, silent): "also needs sound effects".
His standing rules for sound: he picks by ear from a VIDEO of the move with each option, and the
sounds are REAL RECORDINGS, not synthesised (every synthesised skill sound left the game on
2026-09-29; the old `theme()` in tools/build_paete_audio.py is what he does not want back).
His complaint about the picture the same day: "it looks like its sped up. theres not much weight
to the timing of things". Sound is half of weight, so every big moment here has a lead-in, a breath
of near silence just before it, and a tail (LEAD-IN parts, DUCKS, and the tails in PARTS).

This is the sibling of tools/build_paete_skill_sfx.py (LIANA LEAP) and uses its tools: the same
manifest (tools/paete_sfx_sources.json), the same downloads (ArtSource/paete/sfx-sources/), the same
Freesound CC0 fetch, and its second set's `gentle_layer`: NO recording is sped up or slowed by more
than 3 semitones, tails run out as recorded, nothing is evened out. The 58 recordings of 2026-10-07
are all short foley, so 43 more were fetched on 2026-10-08 (n 59 to 101, ten new kinds: wind, struck
and bowed metal, voice, deep impacts, rumble, breaking stone, soil, tin can, tree cracks, heartbeat).

THE THREE (genuinely different choices, not three mixes of one):
  a  EARTH AND WOOD    foley only. Wood, soil, stone, rope, leaves, wind. The goddess is weather:
                       a gust, wind in trees, one long breath out. No pitched instrument anywhere.
  b  SHE SINGS         the same physical body, and the goddess has a VOICE of struck and sung metal:
                       a rubbed singing bowl, a soft bowl strike, a choir's held "oh", chime tubes for
                       the falling star, a temple gong when the tree breaks into daylight. In D sharp.
  c  THUNDER AND DRUM  bigger and darker. Her sky is rolling thunder and a bowed cymbal; a concert
                       bass drum and a low log drum sit under every big hit; the underground is far
                       more muffled and the gaps before the hits are deeper, so the burst opens wider.

AND TWO MORE, OF b (he chose b; then, 2026-10-08, of its instruments: "idk about the bells. it should be more chimey
and the overall instrument composition sounds too rigid and flat"). Both are b underneath, with the gong, the bowl
strikes and the chime tubes taken out and a chime PHRASE in their place; the whole of it is written above VOICES:
  b1 WIND CHIMES       koshi rods, a koshi in air, a bar chime's swell, a mark tree's run down, swished bar chimes
  b2 A LITTLE TUNE     a toy glockenspiel's melody, a music box and a kalimba under it, chime bars and a tingsha on top
30 more recordings were fetched for them (n 102 to 131: chimes, tines, small bells), CC0, the same way.

EVERY EVENT TIME IS IN `T`, ONCE. A retime of the film is one edit there.

WHAT IS WHERE.
  Logs/paete-ability-film/introfx_t1_frames/   the film (f000.png to f270.png, 30 fps, 640x360)
  Logs/paete-sfx-drafts/ult_a.wav, ult_b.wav, ult_c.wav, ult_b1.wav, ult_b2.wav   the drafts (44100 Hz, mono, 16 bit)
  Logs/paete-sfx-drafts/video/ult_a.mp4 ... ult_b2.mp4          the film with each (Blender muxes; no ffmpeg here)
  Logs/paete-sfx-drafts/README_ult.txt, report_ult.txt         what each is made of, and the numbers
  tools/mux_paete_ult_video.py                 the Blender script that joins frames and a wav into an mp4
  NOTHING goes into Assets/. Nothing here is in the game.

NOBODY HAS HEARD ANY OF THIS. It was cut and checked by a machine that cannot listen: it measures
length, peak, where the loudest 50 ms falls, each moment's level against the thud, how dark the
underground is against the open air, and the quietest stretch. It cannot say that any of it sounds
good, heavy, sacred or natural. Deterministic. numpy, scipy, soundfile.
"""
import argparse
import subprocess
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import build_paete_skill_sfx as base  # noqa: E402
from build_paete_skill_sfx import (SR, PEAK, ROOT, OUT, CACHE, by_id, centroid, fade, gentle_layer,  # noqa: E402
                                   lay, load_manifest, loud, save_manifest, soft, sos, write)

TODAY = "2026-10-08"
FPS = 30
FRAMES = ROOT / "Logs/paete-ability-film/introfx_v6_frames"
VIDEO = OUT / "video"
BLENDER = Path("C:/Program Files/Blender Foundation/Blender 5.0/blender.exe")
MUX = ROOT / "tools/mux_paete_ult_video.py"
LOUDEST = 0.30                # the RMS of the loudest 50 ms of a draft (the thud), before the peak is bent
TALLEST = 1.10                # a draft is never bent from higher than this (it is turned down instead)

# ------------------------------------------------------------------ THE ONE TABLE OF TIMES
#
# Real seconds into the film (frame / 30). Every part below is hung on one of these names, and a
# part that runs FROM one event TO another takes its length from here, so a retime is one edit.
T = {
    "gust": 0.00,        # a gust of leaves blows in
    "rise": 0.07,        # Mariang Makiling rises over the horizon, far off, the sky going to dusk
    "whole": 0.73,       # she is whole, in her halo and sunburst, and hangs there
    "approach": 1.09,    # she comes close
    "vast": 1.89,        # her face fills the sky, bending over him, holding a light
    "star": 1.93,        # the light falls from her hands like a falling star
    "palm": 2.35,        # it lands in his palm
    "eyes": 2.54,        # HIS EYES IGNITE
    "kneel": 3.07,       # he drops to a knee
    "slam": 3.28,        # THE SLAM: both palms hit the court
    "dig": 3.30,         # his roots dig into the ground (to 3.7)
    "beat1": 3.88,       # three heartbeats of light down his arms
    "beat2": 4.15,
    "beat3": 4.38,
    "plunge": 4.34,      # the camera plunges down past his arm
    "punch": 4.54,       # it punches THROUGH the court: everything is under the earth
    "launch": 4.69,      # his three glowing roots LAUNCH
    "pulse1": 4.84,      # pulses of light overtaking the camera in the burrow
    "pulse2": 5.00,
    "can": 5.07,         # a tin can in the soil goes by
    "pulse3": 5.17,
    "pulse4": 5.31,
    "pulse5": 5.44,
    "knock": 5.52,       # the roots KNOCK the court from below (cracks of light, clods, to 5.69)
    "burst": 5.73,       # the camera BURSTS out into daylight
    "claws": 5.82,       # the tree's claws slam the court
    "haul1": 6.05,       # the guardian tree HAULS itself out in three heaves, each harder
    "haul2": 6.42,
    "haul3": 6.80,
    "top": 7.07,         # it tops out, its crown shakes
    "tree_eyes": 7.32,   # ITS EYES OPEN
    "lash": 7.44,        # its limbs LASH out at the players
    "yank": 7.93,        # the YANK
    "thud": 8.35,        # THE THUD: everyone hits the trunk. The single heaviest hit of the whole thing
    "end": 9.00,
}

# ------------------------------------------------------------------ the parts
#
# A PART is one thing heard at one event: `on` (a name in T), `bus`, `level`, `layers`.
#   `level`  how loud the part is, as a share of THE THUD (1.0). A hit is levelled by its loudest
#            50 ms; a `bed` (wind, a held tone, soil running) by its loudest 400 ms. So the order of the big
#            moments is written here, not left to how hot each recording happened to be.
#   `bus`    "air" (the open world), "earth" (under the court: low-passed hard, close, dry, and cut
#            off at the burst) or "bright" (under the court but let through three times higher:
#            the tin can, which has to stay a tin can).
#   `to`     the event a bed runs until. A layer with `until: True` is cut to that length, and a
#            layer with `f` is laid that share of the way from `on` to `to` (the swishes of a rush).
# A LAYER is tools/build_paete_skill_sfx.py's second-set layer (`gentle_layer`): `src` (Freesound
# id), `a`..`b` (seconds of it), `at` (seconds after the part's event; may be negative), `gain`,
# `pitch` (tape speed, refused beyond 3 semitones), `hp`, `lp` (6 dB an octave), `fin`, `fout`,
# `decay`, `swell`, `keep_lead` (do not trim to the layer's own onset). Hits are trimmed to their
# own onset, so the recording's attack lands ON the event.
PARTS = {
    # ---------------- the sky: her arrival. Slow and large, never busy: one bed, one or two events.
    "gust": {"on": "gust", "bus": "air", "level": 0.20,
             "what": "a real gust of wind blowing leaves",
             "layers": [{"src": 711441, "a": 0.0, "b": 2.45, "keep_lead": True, "fin": 0.03, "fout": 0.6}]},
    "sky_wind": {"on": "rise", "to": "punch", "bus": "air", "bed": True, "level": 0.10,
                 "what": "wind in trees, rising as she rises and held under him until the court breaks",
                 "layers": [{"src": 827359, "a": 1.0, "until": True, "keep_lead": True, "fin": 1.0, "fout": 0.12}]},
    "sky_swell": {"on": "vast", "bus": "air", "bed": True, "level": 0.16,
                  "what": "wind rising in dead leaves, as recorded, its top as her face fills the sky",
                  "layers": [{"src": 544844, "a": 7.0, "b": 10.4, "at": -1.75, "keep_lead": True, "fin": 0.9, "fout": 1.3}]},
    "breath": {"on": "approach", "bus": "air", "level": 0.12,
               "what": "a woman's long breath out as she bends over him (the only voice in a)",
               "layers": [{"src": 663651, "a": 0.98, "b": 1.85, "at": 0.28, "keep_lead": True, "fin": 0.10, "fout": 0.25}]},
    "thunder": {"on": "gust", "to": "slam", "bus": "air", "bed": True, "level": 0.30,
                "what": "a low, muffled roll of real thunder: it swells as she comes close and is still falling at the slam",
                "layers": [{"src": 574385, "a": 1.25, "until": True, "hp": 30, "keep_lead": True, "fin": 0.5, "fout": 0.35}]},
    "shimmer": {"on": "eyes", "bus": "air", "bed": True, "level": 0.15,
                "what": "a cymbal bowed with a bass bow, swelling as recorded, its peak on his eyes igniting",
                "layers": [{"src": 215571, "a": 1.2, "b": 4.5, "at": -2.20, "keep_lead": True, "fin": 0.4, "fout": 1.0}]},
    # b's voice, tuned to D sharp (155.6 Hz, the soft bowl's own note) by under 1.5 semitones each:
    # the rubbed bowl 318 -> 311 Hz, the choir's A -> A sharp, the chime tubes to C, A sharp, G sharp.
    "tone": {"on": "rise", "to": "kneel", "bus": "air", "bed": True, "level": 0.17,
             "what": "a singing bowl made to sing with the stick round its rim: one held note, rising with her",
             "layers": [{"src": 202004, "a": 9.0, "until": True, "pitch": 0.978, "keep_lead": True, "fin": 0.65, "fout": 0.7}]},
    "whole_bowl": {"on": "whole", "bus": "air", "level": 0.24,
                   "what": "a large bowl struck once with a soft stick as she is whole (155 Hz, left to ring)",
                   "layers": [{"src": 616335, "a": 0.0, "b": 3.6, "hp": 40, "fout": 1.4}]},
    "choir": {"on": "approach", "to": "kneel", "bus": "air", "bed": True, "level": 0.16,
              "what": "a choir's held \"oh\" coming in as she comes close and becomes vast",
              "layers": [{"src": 702796, "a": 0.15, "until": True, "pitch": 1.0717, "keep_lead": True, "fin": 0.6, "fout": 0.7}]},
    # ---------------- the falling star, his palm, his eyes
    "star_a": {"on": "star", "to": "palm", "bus": "air", "level": 0.14,
               "what": "two bamboo swishes, the second lower, and a bush's rustle: something small falling through air",
               "layers": [{"src": 850168, "a": 0.0, "b": 0.50, "f": 0.0, "pitch": 1.1, "gain": 0.8, "keep_lead": True, "fin": 0.02},
                          {"src": 855844, "a": 0.03, "b": 0.55, "f": 0.42, "pitch": 0.9, "gain": 1.0, "keep_lead": True, "fin": 0.02},
                          {"src": 178615, "a": 0.05, "b": 0.60, "f": 0.2, "gain": 0.35, "hp": 600, "keep_lead": True, "fin": 0.08, "fout": 0.2}]},
    "palm_a": {"on": "palm", "bus": "air", "level": 0.13,
               "what": "a soft hit in a pile of leaves as it lands in his palm",
               "layers": [{"src": 106130, "a": 0.22, "b": 0.85, "gain": 1.0, "fout": 0.2}]},
    "star_b": {"on": "star", "to": "palm", "bus": "air", "level": 0.17,
               "what": "three single chime tubes stepping down (C, A sharp, G sharp) as the light falls",
               "layers": [{"src": 398496, "a": 0.0, "b": 2.0, "f": 0.0, "pitch": 1.06, "gain": 0.8, "fout": 0.8},
                          {"src": 398496, "a": 0.0, "b": 2.0, "f": 0.33, "pitch": 0.959, "gain": 0.9, "fout": 0.8},
                          {"src": 398496, "a": 0.0, "b": 2.0, "f": 0.66, "pitch": 0.854, "gain": 1.0, "fout": 0.8}]},
    "palm_b": {"on": "palm", "bus": "air", "level": 0.19,
               "what": "a small bowl struck as it lands, 2 semitones down so it lands on D sharp",
               "layers": [{"src": 271370, "a": 0.0, "b": 2.6, "pitch": 0.89, "fout": 1.0}]},
    "star_c": {"on": "star", "to": "palm", "bus": "air", "level": 0.13,
               "what": "one long bamboo swish falling",
               "layers": [{"src": 855844, "a": 0.03, "b": 0.55, "f": 0.35, "pitch": 0.86, "keep_lead": True, "fin": 0.03}]},
    "palm_c": {"on": "palm", "bus": "air", "level": 0.16,
               "what": "the bass drum, struck softly, as it lands",
               "layers": [{"src": 235453, "a": 0.36, "b": 1.5, "hp": 30, "fout": 0.5}]},
    "eyes_a": {"on": "eyes", "bus": "air", "level": 0.30,
               "what": "green wood cracking and a bush slashed with a stick: a flare with no metal in it",
               "layers": [{"src": 183452, "a": 1.36, "b": 1.78, "gain": 1.0},
                          {"src": 437355, "a": 1.05, "b": 1.70, "at": 0.008, "gain": 0.6, "fout": 0.2}]},
    "eyes_b": {"on": "eyes", "bus": "air", "level": 0.28,
               "what": "a bowl struck with a hard mallet for its high overtone (the brightest sound of b), a little wood under it",
               "layers": [{"src": 449952, "a": 0.0, "b": 2.2, "pitch": 0.925, "gain": 1.0, "fout": 0.9},
                          {"src": 183452, "a": 1.36, "b": 1.78, "at": 0.006, "gain": 0.3}]},
    "eyes_c": {"on": "eyes", "bus": "air", "level": 0.30,
               "what": "an old film library whip crack with its own ring, and the bass drum under it",
               "layers": [{"src": 675833, "a": 0.02, "b": 0.50, "gain": 1.0},
                          {"src": 235453, "a": 0.36, "b": 1.3, "at": 0.008, "hp": 30, "gain": 0.6, "fout": 0.4}]},
    # ---------------- THE SLAM: lead-in (the kneel), the hit, the tail (debris, his roots digging in)
    "kneel": {"on": "kneel", "to": "slam", "bus": "air", "level": 0.17,
              "what": "LEAD-IN: a ship's low timber creak as he drops to a knee, and a stick's swish whose peak is the slam",
              "layers": [{"src": 113362, "a": 0.05, "b": 0.80, "gain": 1.0, "keep_lead": True, "fin": 0.02, "fout": 0.2},
                         {"src": 352719, "a": 0.02, "b": 0.45, "f": 0.30, "gain": 0.7, "hp": 150, "keep_lead": True, "fin": 0.03}]},
    "slam": {"on": "slam", "bus": "air", "level": 0.62,
             "what": "a very big log thrown on dirt, a thud on earth, concrete breaking on asphalt and a heavy stone thrown down",
             "layers": [{"src": 584891, "a": 3.80, "b": 4.60, "hp": 35, "gain": 1.0},
                        {"src": 536766, "a": 0.10, "b": 0.44, "at": 0.008, "gain": 0.7},
                        {"src": 843339, "a": 3.15, "b": 3.85, "at": 0.012, "gain": 0.7},
                        {"src": 513694, "a": 0.12, "b": 0.80, "at": 0.006, "gain": 0.4}]},
    "slam_tail": {"on": "slam", "bus": "air", "level": 0.15,
                  "what": "TAIL: gravel, stones and dirt falling small, running out on its own",
                  "layers": [{"src": 569745, "a": 0.30, "b": 2.20, "at": 0.12, "gain": 1.0, "keep_lead": True, "fin": 0.03, "fout": 0.7},
                             {"src": 348955, "a": 0.15, "b": 1.20, "at": 0.10, "gain": 0.4}]},
    "dig": {"on": "dig", "bus": "air", "level": 0.15,
            "what": "his roots digging in: a rope's leathery creak, stressed leather, a trowel in soft soil",
            "layers": [{"src": 664929, "a": 0.95, "b": 1.75, "at": 0.05, "gain": 1.0, "keep_lead": True, "fin": 0.02, "fout": 0.2},
                       {"src": 559079, "a": 14.50, "b": 15.10, "at": 0.12, "gain": 0.5, "keep_lead": True, "fin": 0.01},
                       {"src": 696531, "a": 4.45, "b": 5.10, "at": 0.03, "gain": 0.6, "keep_lead": True, "fin": 0.03, "fout": 0.2}]},
    "slam_low": {"on": "slam", "bus": "air", "level": 0.48,
                 "what": "c only: a low log drum, the concert bass drum and a boulder of concrete under the slam",
                 "layers": [{"src": 149966, "a": 0.0, "b": 1.30, "hp": 28, "gain": 1.0, "fout": 0.3},
                            {"src": 235453, "a": 0.36, "b": 2.20, "at": 0.006, "hp": 30, "gain": 0.8, "fout": 0.8},
                            {"src": 522099, "a": 0.0, "b": 1.27, "at": 0.010, "hp": 30, "gain": 0.7}]},
    # ---------------- three heartbeats of light down his arms, each louder (one part per beat)
    "beat_a": {"bus": "air", "what": "a hollow wood block and a heavy thump on stone: a wooden heart",
               "layers": [{"src": 218460, "a": 0.0, "b": 0.28, "pitch": 0.89, "gain": 1.0},
                          {"src": 669719, "a": 0.60, "b": 1.10, "at": 0.008, "hp": 40, "gain": 0.8}]},
    "beat_b": {"bus": "air", "what": "the large bowl again, soft stick, each strike over the last one's ring",
               "layers": [{"src": 616335, "a": 0.0, "b": 1.5, "hp": 40, "gain": 1.0, "fout": 0.8},
                          {"src": 669719, "a": 0.60, "b": 1.10, "at": 0.008, "hp": 40, "gain": 0.4}]},
    "beat_c": {"bus": "air", "what": "a real heartbeat through a stethoscope, with the bass drum struck softly so it is heard on small speakers",
               "layers": [{"src": 831630, "a": 6.30, "b": 6.62, "hp": 30, "gain": 1.0, "fout": 0.1},
                          {"src": 235453, "a": 0.36, "b": 1.20, "at": 0.006, "hp": 30, "gain": 0.8, "fout": 0.4}]},
    # ---------------- THE PUNCH THROUGH THE COURT: lead-in (the plunge), the hit, then the world is muffled
    "plunge": {"on": "plunge", "to": "punch", "bus": "air", "level": 0.22,
               "what": "LEAD-IN: three swishes, lowest first, each louder, as the camera drops past his arm",
               "layers": [{"src": 352719, "a": 0.02, "b": 0.50, "f": 0.0, "gain": 0.4, "hp": 150, "keep_lead": True, "fin": 0.04},
                          {"src": 473583, "a": 0.02, "b": 0.42, "f": 0.30, "gain": 0.7, "keep_lead": True, "fin": 0.03},
                          {"src": 855844, "a": 0.03, "b": 0.50, "f": 0.45, "gain": 1.0, "keep_lead": True, "fin": 0.03}]},
    "punch": {"on": "punch", "bus": "air", "level": 0.42,
              "what": "concrete breaking and a stone hitting rubble: the crack is heard in the open for an instant, then swallowed",
              "layers": [{"src": 843339, "a": 6.12, "b": 6.85, "gain": 1.0},
                         {"src": 569510, "a": 0.0, "b": 0.60, "at": 0.006, "gain": 0.6},
                         {"src": 536766, "a": 0.10, "b": 0.44, "at": 0.010, "gain": 0.6}]},
    "punch_under": {"on": "punch", "bus": "earth", "level": 0.36,
                    "what": "the same hit from below: a heavy thump on stone and the big log on dirt, low-passed",
                    "layers": [{"src": 669719, "a": 4.50, "b": 5.10, "hp": 35, "gain": 1.0},
                               {"src": 584891, "a": 7.25, "b": 7.85, "at": 0.008, "hp": 35, "gain": 0.8}]},
    "punch_low": {"on": "punch", "bus": "earth", "level": 0.40,
                  "what": "c only: the boulder of concrete and the log drum, heard from under the court",
                  "layers": [{"src": 522099, "a": 0.0, "b": 1.27, "at": 0.004, "hp": 30, "gain": 1.0},
                             {"src": 149966, "a": 0.0, "b": 1.10, "hp": 28, "gain": 0.8, "fout": 0.3}]},
    # ---------------- under the earth (4.54 to 5.73): low-passed, close, dry. See UNDER.
    "burrow": {"on": "punch", "to": "burst", "bus": "earth", "bed": True, "level": 0.20,
               "what": "soil going by: a light rockslide, stones crunching and a trowel in soft soil, all low-passed",
               "layers": [{"src": 438682, "a": 16.0, "until": True, "gain": 1.0, "keep_lead": True, "fin": 0.10, "fout": 0.03},
                          {"src": 340621, "a": 0.2, "until": True, "gain": 0.7, "keep_lead": True, "fin": 0.10, "fout": 0.03},
                          {"src": 696531, "a": 0.3, "until": True, "gain": 0.6, "keep_lead": True, "fin": 0.10, "fout": 0.03}]},
    "rumble": {"on": "punch", "to": "burst", "bus": "earth", "bed": True, "level": 0.24,
               "what": "c only: the thunder's low tail as the weight of the ground overhead",
               "layers": [{"src": 574385, "a": 3.5, "until": True, "hp": 28, "gain": 1.0, "keep_lead": True, "fin": 0.08, "fout": 0.03}]},
    "launch": {"on": "launch", "bus": "earth", "level": 0.30,
               "what": "his roots launch: a rope snatched tight, a bamboo swish and a tree's creak",
               "layers": [{"src": 802697, "a": 2.56, "b": 2.92, "gain": 1.0},
                          {"src": 850168, "a": 0.02, "b": 0.42, "at": 0.008, "gain": 0.6, "keep_lead": True, "fin": 0.008},
                          {"src": 797994, "a": 0.72, "b": 1.04, "at": 0.06, "gain": 0.6, "fin": 0.01}]},
    "rootlets": {"on": "launch", "to": "knock", "bus": "earth", "level": 0.12,
                 "what": "rootlets snapping out: twigs breaking and a rope's creak, twice on the way",
                 "layers": [{"src": 683793, "a": 0.90, "b": 1.45, "f": 0.28, "gain": 1.0},
                            {"src": 664929, "a": 2.26, "b": 2.58, "f": 0.45, "gain": 0.8, "fin": 0.01},
                            {"src": 683793, "a": 0.90, "b": 1.45, "f": 0.70, "pitch": 0.9, "gain": 1.0}]},
    "pulse_a": {"bus": "earth", "what": "a rope's whoosh going by",
                "layers": [{"src": 473583, "a": 0.02, "b": 0.42, "at": -0.10, "gain": 1.0, "keep_lead": True, "fin": 0.03}]},
    "pulse_b": {"bus": "bright", "what": "one chime tube, heard through the soil",
                "layers": [{"src": 398496, "a": 0.0, "b": 0.60, "pitch": 0.959, "gain": 1.0, "fout": 0.3},
                           {"src": 473583, "a": 0.02, "b": 0.42, "at": -0.10, "gain": 0.5, "keep_lead": True, "fin": 0.03}]},
    "pulse_c": {"bus": "earth", "what": "a thump on stone and the rope's whoosh: a pulse that quickens toward the knock",
                "layers": [{"src": 669719, "a": 0.60, "b": 0.95, "hp": 35, "gain": 1.0, "fout": 0.1},
                           {"src": 473583, "a": 0.02, "b": 0.42, "at": -0.10, "gain": 0.5, "keep_lead": True, "fin": 0.03}]},
    "can": {"on": "can", "bus": "bright", "level": 0.16,
            "what": "a real tin can struck (let through brighter than the soil, so it is still a tin can)",
            "layers": [{"src": 595039, "a": 5.24, "b": 5.80, "gain": 1.0, "fout": 0.2}]},
    "knock": {"on": "knock", "to": "burst", "bus": "earth", "level": 0.34,
              "what": "LEAD-IN to the burst: the roots knock from below, two heavy thumps on stone, concrete cracking, clods of soil falling",
              "layers": [{"src": 669719, "a": 4.50, "b": 5.10, "hp": 35, "gain": 1.0},
                         {"src": 669719, "a": 5.80, "b": 6.40, "f": 0.48, "hp": 35, "gain": 0.8},
                         {"src": 843339, "a": 3.47, "b": 3.85, "f": 0.25, "gain": 0.5},
                         {"src": 384361, "a": 0.25, "b": 0.90, "f": 0.30, "gain": 0.5, "fout": 0.1}]},
    # ---------------- THE BURST into daylight, the claws, the tail
    "burst": {"on": "burst", "bus": "air", "level": 0.56,
              "what": "concrete breaking, green wood cracking, a real tree coming down (its crackle) and a bush struck, all in the open at once",
              "layers": [{"src": 843339, "a": 6.12, "b": 6.85, "gain": 1.0},
                         {"src": 183453, "a": 0.98, "b": 1.55, "at": 0.006, "gain": 0.7},
                         {"src": 139952, "a": 0.78, "b": 2.30, "at": 0.010, "gain": 0.6, "keep_lead": True, "fin": 0.005, "fout": 0.6},
                         {"src": 106113, "a": 0.02, "b": 0.80, "at": 0.03, "gain": 0.35, "keep_lead": True, "fin": 0.02, "fout": 0.3}]},
    "burst_low": {"on": "burst", "bus": "air", "level": 0.42,
                  "what": "c only: the bass drum and the boulder of concrete under the burst",
                  "layers": [{"src": 235453, "a": 0.36, "b": 2.00, "hp": 30, "gain": 1.0, "fout": 0.8},
                             {"src": 522099, "a": 0.0, "b": 1.27, "at": 0.008, "hp": 30, "gain": 0.7}]},
    "gong": {"on": "burst", "to": "tree_eyes", "bus": "air", "level": 0.22,
             "what": "b only: a temple gong struck by monks, as the tree breaks into daylight, ringing under the heaves",
             "layers": [{"src": 803158, "a": 10.50, "until": True, "pitch": 1.03, "hp": 40, "gain": 1.0, "fout": 0.9}]},
    "claws": {"on": "claws", "bus": "air", "level": 0.48,
              "what": "the claws slam the court: the big log's second throw, wooden rods struck, a heavy stone",
              "layers": [{"src": 584891, "a": 7.20, "b": 7.90, "hp": 35, "gain": 1.0},
                         {"src": 275679, "a": 0.03, "b": 0.29, "at": 0.006, "gain": 0.45},
                         {"src": 369981, "a": 24.40, "b": 25.20, "at": 0.010, "hp": 60, "gain": 0.5}]},
    "burst_tail": {"on": "burst", "bus": "air", "level": 0.12,
                   "what": "TAIL: gravel and dirt coming down after it",
                   "layers": [{"src": 569745, "a": 0.30, "b": 2.40, "at": 0.14, "gain": 1.0, "keep_lead": True, "fin": 0.03, "fout": 0.8}]},
    "open_wind": {"on": "burst", "to": "end", "bus": "air", "bed": True, "level": 0.085,
                  "what": "the open air again: wind in trees, in at once",
                  "layers": [{"src": 827359, "a": 5.0, "until": True, "keep_lead": True, "fin": 0.04, "fout": 0.1}]},
    # ---------------- THE THREE HEAVES, each harder: a creak that tightens (lead-in), a crack, soil, clods (tail)
    "haul1": {"on": "haul1", "bus": "air", "level": 0.34,
              "what": "a tree's creak, green wood cracking, a thud on earth, a spade of soil falling",
              "layers": [{"src": 797991, "a": 0.74, "b": 1.30, "at": -0.13, "gain": 0.7, "keep_lead": True, "fin": 0.03, "fout": 0.15},
                         {"src": 183452, "a": 0.16, "b": 0.62, "gain": 1.0},
                         {"src": 536766, "a": 0.10, "b": 0.44, "at": 0.010, "gain": 0.7},
                         {"src": 384361, "a": 8.30, "b": 8.95, "at": 0.08, "gain": 0.4, "fout": 0.15}]},
    "haul2": {"on": "haul2", "bus": "air", "level": 0.42,
              "what": "a bigger tree's creak, a second crack, a landing on the ground, soil",
              "layers": [{"src": 94359, "a": 5.90, "b": 6.60, "at": -0.14, "gain": 0.7, "keep_lead": True, "fin": 0.03, "fout": 0.15},
                         {"src": 183453, "a": 0.06, "b": 0.55, "gain": 1.0},
                         {"src": 364690, "a": 0.09, "b": 0.60, "at": 0.010, "gain": 0.8, "lp": 3000},
                         {"src": 384361, "a": 0.25, "b": 0.90, "at": 0.08, "gain": 0.4, "fout": 0.15}]},
    "haul3": {"on": "haul3", "bus": "air", "level": 0.50,
              "what": "an old sailing boat's timber stressing, an old trunk splitting in one crack, a very heavy fall on dirt, the dead tree's splintering",
              "layers": [{"src": 675785, "a": 2.66, "b": 3.50, "at": -0.15, "gain": 0.7, "keep_lead": True, "fin": 0.03, "fout": 0.2},
                         {"src": 354104, "a": 2.32, "b": 2.85, "gain": 0.4},
                         {"src": 504626, "a": 0.38, "b": 0.98, "at": 0.008, "gain": 1.0, "lp": 3000},
                         {"src": 869103, "a": 3.84, "b": 4.50, "at": 0.03, "gain": 0.5, "keep_lead": True, "fin": 0.01, "fout": 0.2}]},
    "haul_low": {"bus": "air", "what": "c only: the bass drum under each heave, harder each time",
                 "layers": [{"src": 235453, "a": 0.36, "b": 1.60, "hp": 30, "gain": 1.0, "fout": 0.6}]},
    # ⚠️ THE TRUNK HAS WEIGHT IN b (owner, 2026-10-08, choosing b by ear: "B but the trunk going up sound feels so light").
    # The three heaves were a creak, a crack and soil: all of it above the low end, so a seven metre tree came out of the
    # ground sounding like a branch. Three things were added, to b only: the ground GIVING the whole time it climbs
    # (`rise_rumble`, a rockslide kept under 260 Hz, from the claws to the top); under each heave the big log thrown on
    # dirt and the dead pine's crash, kept low (`haul_heavy`); and c's bass drum under each (`haul_low`). The heaves
    # themselves are a fifth louder. The thud is still the loudest moment (`measure`).
    "haul_heavy": {"bus": "air", "what": "b: under each heave, the big log thrown on dirt and the dead pine's crash, low",
                   "layers": [{"src": 584891, "a": 3.80, "b": 4.60, "hp": 30, "lp": 900, "gain": 1.0},
                              {"src": 869103, "a": 3.84, "b": 4.70, "at": 0.02, "lp": 700, "gain": 0.6, "keep_lead": True, "fin": 0.01, "fout": 0.3}]},
    "rise_rumble": {"on": "claws", "to": "top", "bus": "air", "bed": True, "level": 0.15,
                    "what": "b: the ground giving way the whole time the trunk climbs: a rockslide, kept low",
                    "layers": [{"src": 438682, "a": 16.0, "until": True, "lp": 260, "gain": 1.0, "keep_lead": True, "fin": 0.15, "fout": 0.5}]},
    "top": {"on": "top", "bus": "air", "level": 0.22,
            "what": "it tops out and its crown shakes: a tree shaken, pine needles rattling, a tree's creak settling",
            "layers": [{"src": 540278, "a": 0.05, "b": 1.20, "gain": 1.0, "keep_lead": True, "fin": 0.02, "fout": 0.4},
                       {"src": 846048, "a": 0.0, "b": 0.90, "at": 0.03, "gain": 0.45, "keep_lead": True, "fin": 0.03, "fout": 0.4},
                       {"src": 797994, "a": 0.72, "b": 1.04, "at": 0.04, "gain": 0.45, "fin": 0.01}]},
    "tree_eyes_a": {"on": "tree_eyes", "bus": "air", "level": 0.20,
                    "what": "its eyes open: a door hinge's creak that rises as recorded, and one wood hit",
                    "layers": [{"src": 390123, "a": 0.08, "b": 0.44, "gain": 1.0, "keep_lead": True, "fin": 0.01},
                               {"src": 547414, "a": 0.12, "b": 0.42, "gain": 0.6}]},
    "tree_eyes_b": {"on": "tree_eyes", "bus": "air", "level": 0.22,
                    "what": "its eyes open: the small bowl again (her light is in the tree now), and the hinge's creak",
                    "layers": [{"src": 271370, "a": 0.0, "b": 1.4, "pitch": 0.89, "gain": 1.0, "fout": 0.6},
                               {"src": 390123, "a": 0.08, "b": 0.44, "at": 0.006, "gain": 0.4, "keep_lead": True, "fin": 0.01}]},
    "tree_eyes_c": {"on": "tree_eyes", "bus": "air", "level": 0.24,
                    "what": "its eyes open: the bass drum struck softly, a heavy thump on stone, the hinge's creak",
                    "layers": [{"src": 235453, "a": 0.36, "b": 1.20, "hp": 30, "gain": 1.0, "fout": 0.4},
                               {"src": 669719, "a": 3.15, "b": 3.70, "at": 0.006, "hp": 35, "gain": 0.6},
                               {"src": 390123, "a": 0.08, "b": 0.44, "at": 0.010, "gain": 0.4, "keep_lead": True, "fin": 0.01}]},
    # ---------------- THE LASH, THE YANK, THE THUD: lead-in (the rush of bodies), the hit, the tail (the struggle)
    "lash": {"on": "lash", "bus": "air", "level": 0.30,
             "what": "its limbs lash out: three sticks swung through the air one after another, and a whip crack as they reach",
             "layers": [{"src": 855844, "a": 0.03, "b": 0.55, "at": -0.08, "gain": 0.8, "keep_lead": True, "fin": 0.02},
                        {"src": 352719, "a": 0.02, "b": 0.52, "at": 0.00, "gain": 0.8, "hp": 150, "keep_lead": True, "fin": 0.02},
                        {"src": 850168, "a": 0.0, "b": 0.45, "at": 0.10, "gain": 0.8, "keep_lead": True, "fin": 0.01},
                        {"src": 529925, "a": 0.055, "b": 0.20, "at": 0.27, "gain": 1.0, "fout": 0.05}]},
    "yank": {"on": "yank", "bus": "air", "level": 0.38,
             "what": "the yank: a rope snatched tight, cut in at its snap, a tree's creak and a rope's creak as the strain",
             "layers": [{"src": 802697, "a": 1.362, "b": 1.72, "gain": 1.0, "keep_lead": True, "fin": 0.004},
                        {"src": 797994, "a": 0.72, "b": 1.04, "at": 0.07, "gain": 0.5, "fin": 0.01},
                        {"src": 664929, "a": 2.80, "b": 3.08, "at": 0.10, "gain": 0.4, "fin": 0.01}]},
    "rush": {"on": "yank", "to": "thud", "bus": "air", "level": 0.24,
             "what": "LEAD-IN to the thud: four different swishes, lowest to highest and each louder, as everyone is dragged in",
             "layers": [{"src": 352719, "a": 0.02, "b": 0.50, "f": 0.05, "gain": 0.3, "hp": 150, "keep_lead": True, "fin": 0.04},
                        {"src": 473583, "a": 0.02, "b": 0.42, "f": 0.28, "gain": 0.5, "keep_lead": True, "fin": 0.03},
                        {"src": 855844, "a": 0.03, "b": 0.50, "f": 0.50, "gain": 0.8, "keep_lead": True, "fin": 0.03},
                        {"src": 850168, "a": 0.00, "b": 0.30, "f": 0.72, "gain": 1.0, "keep_lead": True, "fin": 0.02}]},
    # The pine's crash is cut in 30 ms before its top (its own crackle before that is left out), so
    # the breath before the thud stays a breath and the hit rises in a few ms.
    "thud": {"on": "thud", "bus": "air", "level": 1.00,
             "what": "THE THUD: a tall dead pine's crash onto the ground, the big log's hardest throw, a very heavy fall on dirt, a thud on earth, wood clattering (the players are wooden), a bush struck",
             "layers": [{"src": 869103, "a": 6.935, "b": 8.30, "at": -0.004, "hp": 35, "gain": 1.0, "keep_lead": True, "fin": 0.004, "fout": 0.3},
                        {"src": 584891, "a": 3.80, "b": 4.60, "hp": 35, "gain": 0.9},
                        {"src": 504626, "a": 0.38, "b": 0.98, "at": 0.006, "gain": 0.6, "lp": 3000},
                        {"src": 536766, "a": 0.10, "b": 0.44, "at": 0.010, "gain": 0.6},
                        {"src": 349273, "a": 0.60, "b": 1.30, "at": 0.03, "gain": 0.3},
                        {"src": 106113, "a": 0.02, "b": 0.80, "at": 0.04, "gain": 0.25, "hp": 500, "keep_lead": True, "fin": 0.03, "fout": 0.3}]},
    "thud_low": {"on": "thud", "bus": "air", "level": 0.80,
                 "what": "c only: the log drum, the bass drum at its hardest and the boulder of concrete under the thud",
                 "layers": [{"src": 149966, "a": 0.0, "b": 1.30, "hp": 28, "gain": 1.0, "fout": 0.3},
                            {"src": 235453, "a": 0.36, "b": 1.60, "at": 0.004, "hp": 30, "gain": 0.9, "fout": 0.3},
                            {"src": 522099, "a": 0.0, "b": 1.27, "at": 0.010, "hp": 30, "gain": 0.6}]},
    "bloom": {"on": "thud", "bus": "air", "level": 0.22,
              "what": "b only: the large bowl once more under the thud, so her note is the last thing in it",
              "layers": [{"src": 616335, "a": 0.0, "b": 1.2, "hp": 40, "gain": 1.0, "fout": 0.3}]},
    "struggle": {"on": "thud", "to": "end", "bus": "air", "level": 0.17,
                 "what": "TAIL: they struggle against the trunk: a rope's leathery creak, stressed leather, a tree shaken, a bush settling",
                 "layers": [{"src": 664929, "a": 0.95, "b": 1.60, "f": 0.28, "gain": 1.0, "keep_lead": True, "fin": 0.02, "fout": 0.15},
                            {"src": 559079, "a": 0.70, "b": 1.25, "f": 0.50, "gain": 0.6, "keep_lead": True, "fin": 0.02, "fout": 0.1},
                            {"src": 540278, "a": 0.05, "b": 0.75, "f": 0.20, "gain": 0.6, "hp": 500, "keep_lead": True, "fin": 0.04, "fout": 0.3},
                            {"src": 178615, "a": 0.12, "b": 0.72, "f": 0.45, "gain": 0.4, "hp": 500, "keep_lead": True, "fin": 0.05, "fout": 0.3}]},
}


# ⚠️ AND THE TRUNK'S OWN VOICE IS DEEPER IN b (owner, 2026-10-08, of b with the weight added under its heaves: "coiuld the
# trunk sounds itself be a bit deeper"). The rumble and the drum were UNDER the tree; the creaks and cracks that ARE the
# tree were still a sapling's. So b plays each heave's own recordings three semitones down (the most this file allows a
# recording to be moved: it is still the recording, a fifth slower) and takes the top off them at 3 kHz, and a ship's low
# timber creak leads each one in. a and c keep the heaves as they were.
DEEP = 2.0 ** (-3.0 / 12.0)
for _h in ("haul1", "haul2", "haul3"):
    PARTS[_h + "_b"] = dict(PARTS[_h], what="b: " + PARTS[_h]["what"] + ", all three semitones down and darker, led in by a ship's low timber creak",
                            layers=[dict(L, pitch=L.get("pitch", 1.0) * DEEP, lp=min(L.get("lp", 3000), 3000)) for L in PARTS[_h]["layers"]]
                                   + [{"src": 113362, "a": 0.05, "b": 0.80, "at": -0.22, "pitch": DEEP, "lp": 1800, "gain": 0.8, "keep_lead": True, "fin": 0.03, "fout": 0.2}])


# ------------------------------------------------------------------ b1 and b2: her voice as CHIMES, and as a phrase
#
# ⚠️ Owner, 2026-10-08, of b's instruments (he had chosen b, and had its trunk made heavier and deeper, above):
# "idk about the bells. it should be more chimey and the overall instrument composition sounds too rigid and flat".
#   THE BELLS were the heavy single struck tones: the temple gong at the burst, the bowl strikes (as she is whole, in
#   his palm, on his eyes, three heartbeats, the tree's eyes, under the thud) and the chime TUBES of the falling star.
#   b1 and b2 have none of them. Their voice is many small ones: koshi and wind chimes, a bar chime's sweep, a mark
#   tree, three little chime bars, a toy glockenspiel, a music box comb, a kalimba, a tingsha.
#   RIGID AND FLAT was single notes laid on the events at one level. Here the voice is one PHRASE that moves:
#     it RISES as she rises (three or four notes climbing, each louder), OPENS as she comes close (wider, two voices,
#     its top note as her face fills the sky), FALLS with the light (a run down into his palm), BLOOMS on his eyes (the
#     loudest it gets), and then comes back smaller: one note of the rise on each heartbeat, the same climb muffled in
#     the soil, a spark at the burst, the bloom again at two thirds as THE TREE'S EYES OPEN (her light arriving in the
#     tree), two last notes after the thud.
#   No note is on a grid: each is written with its own lead or lag (tens of ms, the `at` beside its `f`), its own level
#   (`gain`), and a different bar or rod speaks each time where the recordings allow it. Tails are left to ring over
#   each other (`fout`). A LITTLE SPACE: `ECHO`, three plain quieter, darker copies of the voice itself (no reverb is
#   synthesised; the mark tree's author had already put a reverb on his recording, and its `note` says so).
#   KEY: D major, not b's D sharp, because the recordings argue for it: the choir was sung on A and D, the toy
#   glockenspiel is a C major scale, the chime bars are C, E, G and the koshi is A, B, C, E, so two semitones or less
#   puts nearly every one of them in D, where D sharp wanted three. Each note is the recording NEAREST its pitch (`tine`).
#   b itself is untouched and still in D sharp.
#   KEPT from b: all of its physical body, the muffled underground, every hit, the trunk's weight and depth, the wind,
#   the choir's held "oh" (retuned a quarter of a semitone to D), and in b2 the rubbed bowl bed (quieter, down to D).
#   DROPPED: the gong, every bowl strike, the chime tubes; and in b1 the rubbed bowl bed, for a koshi hung in moving air.
#
#   b1  WIND CHIMES    free and airy: koshi rods struck singly, a koshi in air under them, a bar chime swelling as she
#                      comes close, a mark tree running down with the light, bar chimes swished on his eyes.
#   b2  A LITTLE TUNE  a clear small melody on tines: a toy glockenspiel leads, a music box comb and a kalimba answer
#                      under it, the three chime bars and a tingsha are the sparkle on top.
ECHO = ((0.13, -12.0), (0.27, -18.0), (0.43, -24.0))       # seconds after, dB: each copy also darker (ECHO_LP / 1, 2, 3)
ECHO_LP = 5000.0
SEMITONE = 2.0 ** (1.0 / 12.0)
_STEP = {"C": 0, "C#": 1, "D": 2, "D#": 3, "E": 4, "F": 5, "F#": 6, "G": 7, "G#": 8, "A": 9, "A#": 10, "B": 11}

# Every single note there is: (Freesound id, its start s, its end s, its own pitch in Hz as MEASURED from the file).
VOICES = {
    # a toy glockenspiel's C major scale, one note every 4 s. It is a toy: its bars are 4 to 48 cents sharp
    "glock": [(420501, 0.0, 1.8, 1062.0), (420501, 4.0, 5.8, 1199.0), (420501, 8.0, 9.8, 1315.0), (420501, 12.0, 13.8, 1408.0),
              (420501, 16.0, 17.8, 1585.0), (420501, 20.0, 21.8, 1810.0), (420501, 24.0, 25.8, 2014.0), (420501, 28.0, 29.8, 2130.0)],
    "box": [(218459, 0.0, 2.2, 1486.0), (732943, 0.10, 2.3, 797.0)],               # two music box combs: F sharp 6 and G5
    "kalimba": [(441143, 0.0, 2.6, 791.0), (691805, 0.0, 2.0, 785.0)],             # two kalimba tines, both G5 (one soft)
    "bar": [(517661, 0.10, 3.0, 2071.0), (517660, 0.10, 2.4, 2654.0), (517659, 0.10, 2.5, 3217.0)],   # three chime bars on a wood stand: C7, E7, G7
    "ting": [(435074, 0.0, 2.2, 2934.0)],                                           # a tingsha: one pure high ring
    # single strikes of a koshi's rods (A5, B5, C6, E6, B6), each cut from its strike to just before the next one
    "koshi": [(459402, 20.57, 21.29, 871.0), (459402, 15.04, 15.50, 871.0), (459402, 9.41, 9.92, 986.0), (459402, 6.99, 8.95, 986.0),
              (459402, 15.50, 17.50, 1049.0), (459402, 18.66, 20.55, 1049.0), (459402, 3.91, 6.40, 1320.0), (459402, 9.92, 11.30, 1320.0),
              (459402, 13.95, 15.04, 2009.0), (459402, 17.81, 18.28, 2009.0)],
}


def hz(note):
    """A note's pitch: "F#6" is 1479.98 Hz (A4 = 440)."""
    return 440.0 * 2.0 ** ((_STEP[note[:-1]] + 12 * (int(note[-1]) + 1) - 69) / 12.0)


def tine(voice, note, take=0, **more):
    """One note as a layer: the recording of that voice whose own pitch is NEAREST the note, moved the
    rest of the way (never more than 3 semitones: refused). `take` 1 is the next nearest, so the same
    note can be spoken by a different bar or rod the second time."""
    want = hz(note)
    near = sorted(VOICES[voice], key=lambda v: abs(np.log2(want / v[3])))
    src, a, b, own = near[take]
    if abs(12 * np.log2(want / own)) > 3.0 + 1e-6:
        raise ValueError("%s has no recording within 3 semitones of %s" % (voice, note))
    L = {"src": src, "a": a, "b": b, "pitch": want / own, "fout": 0.5, "note": note, "voice": voice}
    L.update(more)
    return L


_STONE = {"body": True, "src": 669719, "a": 0.60, "b": 1.10, "at": 0.008, "hp": 40, "gain": 0.45}                  # the heartbeat's own thump, as in b
_WHOOSH = {"body": True, "src": 473583, "a": 0.02, "b": 0.42, "at": -0.10, "gain": 0.5, "keep_lead": True, "fin": 0.03}   # a pulse going by, as in b
_CREAK = {"body": True, "src": 390123, "a": 0.08, "b": 0.44, "at": 0.006, "gain": 0.4, "keep_lead": True, "fin": 0.01}    # the tree's eyes, as in b

PARTS.update({
    # ---------------- the beds, in D
    "choir_d": {"on": "approach", "to": "kneel", "bus": "air", "bed": True, "level": 0.16,
                "what": "KEPT from b: the choir's held \"oh\", a quarter of a semitone up so its A and D are D major's",
                "layers": [{"src": 702796, "a": 0.15, "until": True, "pitch": 1.016, "keep_lead": True, "fin": 0.6, "fout": 0.7}]},
    "tone_d": {"on": "rise", "to": "kneel", "bus": "air", "bed": True, "level": 0.17,
               "what": "KEPT from b (b2 only, quieter): the singing bowl made to sing round its rim, 1.4 semitones down to D",
               "layers": [{"src": 202004, "a": 9.0, "until": True, "pitch": 0.9236, "keep_lead": True, "fin": 0.65, "fout": 0.7}]},
    "koshi_air": {"on": "rise", "to": "kneel", "bus": "air", "bed": True, "level": 0.10,
                  "what": "b1: a koshi chime hung in moving air, two semitones up (its B, D, F sharp and C sharp are D major's): the bed her notes sit in",
                  "layers": [{"src": 851363, "a": 17.0, "until": True, "pitch": SEMITONE ** 2, "keep_lead": True, "fin": 0.8, "fout": 0.6}]},
    "thud_wood": {"on": "thud", "bus": "air", "level": 0.12,
                  "what": "b1 and b2: where b had its bowl under the thud (155 Hz of weight), the big log thrown on dirt, kept low, so the thud keeps its margin over the heaves",
                  "layers": [{"src": 584891, "a": 3.80, "b": 4.60, "hp": 30, "lp": 900, "gain": 1.0}]},
    # ---------------- b1: WIND CHIMES
    "b1_rise": {"on": "rise", "to": "approach", "bus": "air", "level": 0.15, "echo": ECHO,
                "what": "b1 RISES: four koshi rods struck one after another, climbing B5, D6, F sharp 6, A6, each louder to the third (as she is whole), a chime bar over it",
                "layers": [tine("koshi", "B5", f=0.10, at=0.02, gain=0.40),
                           tine("koshi", "D6", take=1, f=0.36, at=-0.03, gain=0.60),
                           tine("koshi", "F#6", take=1, f=0.64, at=-0.02, gain=0.85),
                           tine("bar", "D7", take=1, f=0.64, at=0.05, gain=0.30, fout=0.8),
                           tine("koshi", "A6", f=0.88, at=0.03, gain=0.50)]},
    "b1_sweep": {"on": "vast", "bus": "air", "bed": True, "level": 0.19,
                 "what": "b1 OPENS: an orchestral bar chime swept slowly, swelling as recorded, its top as her face fills the sky",
                 "layers": [{"src": 399234, "a": 2.2, "b": 4.9, "at": -1.45, "keep_lead": True, "fin": 0.5, "fout": 1.2}]},
    "b1_open": {"on": "approach", "to": "vast", "bus": "air", "level": 0.23, "echo": ECHO,
                "what": "b1 OPENS: koshi rods and the three chime bars scattered wider and higher (D6, C sharp 6, D7, F sharp 7, A7), then the top: F sharp 6 with C sharp 7 over it, 45 ms ahead of her face",
                "layers": [tine("koshi", "D6", f=0.04, at=0.02, gain=0.50),
                           tine("koshi", "C#6", take=1, f=0.28, at=-0.03, gain=0.50, fout=0.2),
                           tine("bar", "D7", take=1, f=0.45, at=0.03, gain=0.40, fout=0.8),
                           tine("bar", "F#7", take=1, f=0.60, at=-0.02, gain=0.50, fout=0.8),
                           tine("bar", "A7", f=0.74, at=0.04, gain=0.55, fout=0.8),
                           tine("koshi", "F#6", f=1.0, at=-0.045, gain=1.00, fout=0.9),
                           tine("koshi", "C#7", f=1.0, at=-0.010, gain=0.50)]},
    "b1_fall": {"on": "star", "to": "palm", "bus": "air", "level": 0.20,
                "what": "b1 FALLS: a mark tree run from its highest bar to its lowest (its author's own reverb is on it), begun 30 ms before the light leaves her hands",
                "layers": [{"src": 796546, "a": 0.0, "b": 2.3, "at": -0.03, "keep_lead": True, "fin": 0.005, "fout": 1.0}]},
    "b1_palm": {"on": "palm", "bus": "air", "level": 0.14, "echo": ECHO,
                "what": "b1 lands: the koshi's D6 and B5, 15 and 40 ms after it reaches his palm",
                "layers": [tine("koshi", "D6", at=0.015, gain=1.0), tine("koshi", "B5", take=1, at=0.040, gain=0.45)]},
    "b1_bloom": {"on": "eyes", "bus": "air", "level": 0.34, "echo": ECHO,
                 "what": "b1 BLOOMS (the loudest the voice gets): twelve bar chimes swished by hand, the koshi's F sharp 6 and a cluster of its rods, the three chime bars rolled upward over 110 ms",
                 "layers": [{"src": 435075, "a": 5.45, "b": 7.0, "gain": 0.9, "fout": 0.6},
                            tine("koshi", "F#6", gain=1.0, fout=0.9),
                            {"src": 459402, "a": 3.16, "b": 3.90, "at": 0.020, "pitch": SEMITONE ** 2, "gain": 0.6, "fout": 0.3},
                            tine("bar", "D7", take=1, at=0.030, gain=0.5, fout=0.9),
                            tine("bar", "F#7", take=1, at=0.075, gain=0.5, fout=0.9),
                            tine("bar", "A7", at=0.140, gain=0.45, fout=0.9)]},
    "b1_spark": {"on": "burst", "to": "tree_eyes", "bus": "air", "bed": True, "level": 0.10,
                 "what": "b1 at the burst, in place of the gong: a church's bell tree swept, very high and quiet, ringing under the heaves",
                 "layers": [{"src": 271355, "a": 0.9, "until": True, "keep_lead": True, "fin": 0.03, "fout": 0.8}]},
    "b1_tree_eyes": {"on": "tree_eyes", "bus": "air", "level": 0.14, "echo": ECHO,
                     "what": "b1 ECHO as the tree's eyes open: the rise again in a sixth of the time (D6, F sharp 6, A6, from 40 ms before), a small swish of bar chimes, the hinge's creak",
                     "layers": [tine("koshi", "D6", take=1, at=-0.040, gain=0.6),
                                tine("koshi", "F#6", take=1, at=0.030, gain=0.9),
                                tine("koshi", "A6", at=0.100, gain=0.6),
                                {"src": 435075, "a": 2.62, "b": 3.40, "at": 0.02, "gain": 0.5, "fout": 0.3}, dict(_CREAK)]},
    "b1_last": {"on": "thud", "to": "end", "bus": "air", "level": 0.06,
                "what": "b1 after the thud: a real wind chime's high tinkle, far under the struggle",
                "layers": [{"src": 361427, "a": 0.55, "b": 1.60, "f": 0.38, "keep_lead": True, "fin": 0.01, "fout": 0.4}]},
    # ---------------- b2: A LITTLE TUNE
    "b2_rise": {"on": "rise", "to": "whole", "bus": "air", "level": 0.18, "echo": ECHO,
                "what": "b2 RISES: the toy glockenspiel climbs D6, F sharp 6 and reaches A6 25 ms before she is whole, a kalimba's A5 under that note, then a soft B6 above it as she hangs there",
                "layers": [tine("glock", "D6", f=0.12, at=0.02, gain=0.45),
                           tine("glock", "F#6", f=0.52, at=-0.02, gain=0.60),
                           tine("glock", "A6", f=1.0, at=-0.025, gain=0.90),
                           tine("kalimba", "A5", f=1.0, at=0.020, gain=0.50),
                           tine("glock", "B6", f=1.0, at=0.235, gain=0.40)]},
    "b2_open": {"on": "approach", "to": "vast", "bus": "air", "level": 0.28, "echo": ECHO,
                "what": "b2 OPENS: F sharp 6 (music box), A6, B6, C sharp 7, quickening and louder, to D7 40 ms before her face fills the sky, with D6, a kalimba's F sharp 5 and a music box's A5 rolled in under the top note",
                "layers": [tine("box", "F#6", f=0.03, at=0.02, gain=0.50),
                           tine("glock", "A6", f=0.30, at=-0.02, gain=0.60),
                           tine("glock", "B6", f=0.54, at=0.015, gain=0.70),
                           tine("glock", "C#7", f=0.76, at=-0.015, gain=0.82),
                           tine("glock", "D7", f=1.0, at=-0.040, gain=1.00, fout=0.8),
                           tine("glock", "D6", f=1.0, at=-0.015, gain=0.55),
                           tine("kalimba", "F#5", f=1.0, at=0.005, gain=0.50),
                           tine("box", "A5", f=1.0, at=0.025, gain=0.45)]},
    "b2_fall": {"on": "star", "to": "palm", "bus": "air", "level": 0.20, "echo": ECHO,
                "what": "b2 FALLS with the light: B6, A6, F sharp 6 (music box), E6, each softer, landing on D6 10 ms after it reaches his palm with a kalimba's F sharp 5 under it",
                "layers": [tine("glock", "B6", f=0.06, at=0.00, gain=0.65, fout=0.3),
                           tine("glock", "A6", f=0.26, at=0.012, gain=0.58, fout=0.3),
                           tine("box", "F#6", f=0.45, at=-0.010, gain=0.52, fout=0.3),
                           tine("glock", "E6", f=0.62, at=0.015, gain=0.48, fout=0.3),
                           tine("glock", "D6", f=1.0, at=0.010, gain=0.80),
                           tine("kalimba", "F#5", take=1, f=1.0, at=0.030, gain=0.50)]},
    "b2_bloom": {"on": "eyes", "bus": "air", "level": 0.36, "echo": ECHO,
                 "what": "b2 BLOOMS (the loudest the voice gets): D6, F sharp 6, A6, D7 rolled upward over 66 ms, the chime bars' F sharp 7 and A7 after them, a tingsha's F sharp 7 ringing through",
                 "layers": [tine("glock", "D6", at=0.000, gain=0.80),
                            tine("box", "F#6", at=0.022, gain=0.75),
                            tine("glock", "A6", at=0.041, gain=0.90),
                            tine("glock", "D7", at=0.066, gain=1.00, fout=0.8),
                            tine("bar", "F#7", take=1, at=0.090, gain=0.45, fout=0.9),
                            tine("bar", "A7", at=0.130, gain=0.38, fout=0.9),
                            tine("ting", "F#7", at=0.010, gain=0.50, fout=1.2)]},
    "b2_spark": {"on": "burst", "bus": "air", "level": 0.10, "echo": ECHO,
                 "what": "b2 at the burst, in place of the gong: the three chime bars scattered (D7, A7, F sharp 7), quiet",
                 "layers": [tine("bar", "D7", take=1, at=0.010, gain=1.0, fout=0.9), tine("bar", "A7", at=0.075, gain=0.7, fout=0.9),
                            tine("bar", "F#7", take=1, at=0.165, gain=0.8, fout=0.9)]},
    "b2_tree_eyes": {"on": "tree_eyes", "bus": "air", "level": 0.14, "echo": ECHO,
                     "what": "b2 ECHO as the tree's eyes open: A6 then D7 (50 ms before, 10 ms after), a music box's F sharp 6 and D6 falling away under them, the hinge's creak",
                     "layers": [tine("glock", "A6", at=-0.050, gain=0.60),
                                tine("glock", "D7", at=0.010, gain=0.85, fout=0.8),
                                tine("box", "F#6", at=0.090, gain=0.45),
                                tine("glock", "D6", at=0.150, gain=0.40), dict(_CREAK)]},
    "b2_last": {"on": "thud", "to": "end", "bus": "air", "level": 0.07, "echo": ECHO,
                "what": "b2 after the thud: A6, then a music box's F sharp 6, very quiet: the tune's last two notes",
                "layers": [tine("glock", "A6", f=0.46, gain=1.0), tine("box", "F#6", f=0.68, at=0.01, gain=0.7)]},
})
# One note of the rise on each heartbeat (with b's own stone thump), and the same climb, five notes, in the soil.
# The soil's notes go through the soil's own low-pass (the `earth` bus, not b's brighter one for its chime tube): a
# koshi rod's loud partial is near 2.7 kHz and would have let daylight into the burrow.
for _k, (_n1, _n2) in enumerate((("D6", "F#5"), ("F#6", "A5"), ("A6", "D6"))):
    PARTS["b1_beat%d" % (_k + 1)] = {"bus": "air", "echo": ECHO, "what": "b1 ECHO on a heartbeat: the koshi's %s and a heavy thump on stone" % _n1,
                                     "layers": [tine("koshi", _n1, take=_k % 2), dict(_STONE)]}
    PARTS["b2_beat%d" % (_k + 1)] = {"bus": "air", "echo": ECHO, "what": "b2 ECHO on a heartbeat: %s, an octave under the tune, and a heavy thump on stone" % _n2,
                                     "layers": [tine("glock" if _n2 == "D6" else "kalimba" if _k == 0 else "box", _n2), dict(_STONE)]}
for _k, (_n1, _n2) in enumerate((("B5", "D6"), ("D6", "E6"), ("F#6", "F#6"), ("A6", "A6"), ("C#7", "B6"))):
    PARTS["b1_pulse%d" % (_k + 1)] = {"bus": "earth", "what": "b1 in the soil: the koshi's %s, heard through the ground, and a rope's whoosh" % _n1,
                                      "layers": [tine("koshi", _n1, take=(_k + 1) % 2, fout=0.3), dict(_WHOOSH)]}
    PARTS["b2_pulse%d" % (_k + 1)] = {"bus": "earth", "what": "b2 in the soil: the toy glockenspiel's %s, heard through the ground, and a rope's whoosh" % _n2,
                                      "layers": [tine("glock", _n2, fout=0.3), dict(_WHOOSH)]}


def repeated(name, events, levels):
    """One part per event from one recipe (the heartbeats, the pulses, the bass drum under the heaves),
    or from one recipe EACH when `name` is a list (b1 and b2: a different note on every beat)."""
    names = [name] * len(events) if isinstance(name, str) else name
    return [(dict(PARTS[k], on=e, level=v), "%s@%s" % (k, e)) for k, e, v in zip(names, events, levels)]


BEATS = ("beat1", "beat2", "beat3")
PULSES = ("pulse1", "pulse2", "pulse3", "pulse4", "pulse5")
HAULS = ("haul1", "haul2", "haul3")
BODY_BEFORE = ["kneel", "slam", "slam_tail", "dig", "plunge", "punch", "punch_under", "burrow", "launch", "rootlets", "can", "knock"]
BODY_AFTER = ["burst", "claws", "burst_tail", "open_wind", "haul1", "haul2", "haul3", "top", "lash", "yank", "rush", "thud", "struggle"]

# The three. `parts` is the order they are laid in (it does not change the sound). `under` is how
# the underground is made: the earth bus is low-passed at `lp` Hz (fourth order), and whatever was
# still sounding in the open air (wind, a bowl's ring) is low-passed at `air_lp` and dropped by
# `air_db`, from the punch to the burst. `ducks` are the breaths before the hits: (event, seconds
# before it, dB): everything that began EARLIER than the event is dropped by that much for that
# long, to its hit, and let back up over DUCK_BACK seconds, so the hit stands in air.
SCORE = {
    "a": {"name": "EARTH AND WOOD",
          "line": "foley only: wood, soil, stone, rope, leaves and wind; the goddess is a gust, wind in trees and one breath out; no pitched instrument",
          "parts": ["gust", "sky_wind", "sky_swell", "breath", "star_a", "palm_a", "eyes_a"] + BODY_BEFORE + BODY_AFTER + ["tree_eyes_a"],
          "beats": ("beat_a", (0.16, 0.20, 0.25)), "pulses": ("pulse_a", (0.10, 0.11, 0.12, 0.13, 0.15)), "haul_low": None,
          "under": {"lp": 1100.0, "air_lp": 500.0, "air_db": -16.0},
          "ducks": [("slam", 0.09, -8.0), ("burst", 0.05, -10.0), ("thud", 0.10, -9.0)]},
    "b": {"name": "SHE SINGS",
          "line": "the same physical body, and the goddess has a voice in D sharp: a rubbed singing bowl, a soft bowl strike, a choir's held \"oh\", chime tubes for the star, a temple gong at the burst",
          "parts": ["gust", ("sky_wind", 0.6), "tone", "whole_bowl", "choir", "star_b", "palm_b", "eyes_b"] + BODY_BEFORE
                   + ["gong", "rise_rumble"] + [("open_wind", 0.7) if p == "open_wind" else (p + "_b", 1.25) if p in HAULS else p for p in BODY_AFTER]
                   + ["tree_eyes_b", "bloom"],
          "beats": ("beat_b", (0.13, 0.16, 0.20)), "pulses": ("pulse_b", (0.10, 0.11, 0.12, 0.13, 0.15)),
          "haul_low": (0.22, 0.29, 0.38), "haul_heavy": (0.28, 0.36, 0.46),
          "under": {"lp": 1000.0, "air_lp": 450.0, "air_db": -14.0},
          "ducks": [("slam", 0.09, -8.0), ("burst", 0.05, -10.0), ("thud", 0.10, -9.0)]},
    "c": {"name": "THUNDER AND DRUM",
          "line": "bigger and darker: rolling thunder and a bowed cymbal for her sky, a bass drum and a low log drum under every big hit, a far more muffled underground and deeper gaps before the hits",
          "parts": ["gust", ("sky_wind", 0.7), "thunder", "shimmer", "star_c", "palm_c", "eyes_c"] + BODY_BEFORE
                   + ["slam_low", "punch_low", "rumble", "burst_low"] + BODY_AFTER + ["tree_eyes_c", "thud_low"],
          "beats": ("beat_c", (0.20, 0.25, 0.31)), "pulses": ("pulse_c", (0.12, 0.14, 0.16, 0.19, 0.22)), "haul_low": (0.22, 0.28, 0.36),
          "under": {"lp": 550.0, "air_lp": 250.0, "air_db": -30.0},
          "ducks": [("slam", 0.13, -16.0), ("punch", 0.05, -8.0), ("burst", 0.08, -24.0), ("haul3", 0.05, -6.0), ("thud", 0.13, -18.0)]},
}


# b1 and b2 are b with its struck bells taken out and a chime phrase put in (the list is above VOICES).
# Everything else of b is read from b, so a change to b's body is a change to theirs.
BELLS = ("tone", "whole_bowl", "choir", "star_b", "palm_b", "eyes_b", "gong", "tree_eyes_b", "bloom")


def chimed(take, name, line, add):
    S = dict(SCORE["b"], name=name, line=line)
    S["parts"] = [q for q in SCORE["b"]["parts"] if (q if isinstance(q, str) else q[0]) not in BELLS] + ["thud_wood"] + add
    S["beats"] = (["%s_beat%d" % (take, k) for k in (1, 2, 3)], (0.11, 0.13, 0.16))
    S["pulses"] = (["%s_pulse%d" % (take, k) for k in (1, 2, 3, 4, 5)], SCORE["b"]["pulses"][1])
    return S


SCORE["b1"] = chimed("b1", "WIND CHIMES",
                     "b with its bells taken out and CHIMES in their place, free and airy, in D: koshi rods struck singly over a koshi hung in moving air, "
                     "a bar chime swelling as she comes close, a mark tree running down with the light, bar chimes swished on his eyes",
                     [("choir_d", 0.85), "koshi_air", "b1_rise", "b1_sweep", "b1_open", "b1_fall", "b1_palm", "b1_bloom", "b1_spark", "b1_tree_eyes", "b1_last"])
SCORE["b2"] = chimed("b2", "A LITTLE TUNE",
                     "b with its bells taken out and a small clear MELODY in their place, in D: a toy glockenspiel leads, a music box comb and a kalimba answer under it, "
                     "three chime bars and a tingsha sparkle on top",
                     ["choir_d", ("tone_d", 0.7), "b2_rise", "b2_open", "b2_fall", "b2_bloom", "b2_spark", "b2_tree_eyes", "b2_last"])

# b3 (owner, 2026-10-08, of b1: "i like the start sounds with the chimes, but it doesnt carry on well when the animation
# leans to be more dramatic near the end. i was thinking of a whistle before paete slams down, then some more dramatic
# musical sounds after. because the soft chimes dont sell the weight of the tree"). b1 to his eyes, then a slide whistle
# falling into the slam, and drums (bass drum, timpani, a timpani roll under the earth) with a low tuba. Built in five
# minutes on his word: spans and pitches of the new recordings are NOT measured, only trimmed to their own onsets.
_BD = {"src": 235453, "a": 0.36, "b": 1.60, "hp": 30, "gain": 1.0, "fout": 0.6}
_TIMP = {"src": 369394, "a": 0.0, "b": 2.5, "hp": 30, "pitch": SEMITONE ** 2, "gain": 0.9, "fout": 1.0}
_TUBA = {"src": 374271, "a": 0.0, "b": 3.0, "hp": 30, "pitch": SEMITONE ** -3, "gain": 0.7, "fout": 0.8}
PARTS.update({
    # Owner, 2026-10-08, of the slide whistle that fell into the slam: "i was thinking of a deeper whistle, without the
    # falling note before the slam." So one low bamboo flute note, HELD (measured steady at 197 Hz, G3), 1.9 semitones
    # up to A3, from 2.75 s, swelling a little, cut clean 40 ms before the slam.
    "b3_whistle": {"on": "slam", "bus": "air", "level": 0.20, "what": "b3: a low bamboo flute's held note (A3), swelling, cut 40 ms before the slam",
                   # Then: "low bamboo flute needs to go further back for longer". In softly from 1.95 s (the light begins to
                   # fall), one unbroken breath of the recording (1.44 s of its 3.9 s, no loop), swelling late so it stays
                   # under her chimes until they release.
                   "layers": [{"src": 659915, "a": 0.30, "b": 0.30 + 1.29 * 220.0 / 197.3, "at": -1.33, "pitch": 220.0 / 197.3, "swell": 2.0,
                               "keep_lead": True, "fin": 0.35, "fout": 0.012}]},
    "b3_beat": {"bus": "air", "what": "b3: a heartbeat as the bass drum and a timpani", "layers": [dict(_BD), dict(_TIMP, at=0.006, gain=0.6)]},
    "b3_roll": {"on": "punch", "to": "burst", "bus": "earth", "bed": True, "level": 0.20, "what": "b3: a timpani roll under the earth",
                "layers": [{"src": 373946, "a": 2.0, "until": True, "hp": 30, "pitch": SEMITONE ** -2, "keep_lead": True, "fin": 0.1, "fout": 0.03}]},
    "b3_burst": {"on": "burst", "bus": "air", "level": 0.40, "what": "b3: the burst as timpani, bass drum and a low tuba",
                 "layers": [dict(_TIMP), dict(_BD, at=0.004), dict(_TUBA, at=0.01)]},
    "b3_haul": {"bus": "air", "what": "b3: a timpani and the tuba under a heave", "layers": [dict(_TIMP), dict(_TUBA, at=0.01, gain=0.5)]},
    "b3_arrive": {"on": "tree_eyes", "bus": "air", "level": 0.24, "what": "b3: it has arrived: the tuba held, a timpani",
                  "layers": [dict(_TUBA, gain=1.0), dict(_TIMP, at=0.004, gain=0.6)]},
    "b3_drive1": {"on": "lash", "to": "yank", "bus": "air", "level": 0.24, "what": "b3: the drums drive the lash",
                  "layers": [dict(_BD, f=0.0), dict(_BD, f=0.5, gain=1.1)]},
    "b3_drive2": {"on": "yank", "to": "thud", "bus": "air", "level": 0.30, "what": "b3: the drums tighten into the thud",
                  "layers": [dict(_BD, f=0.0, gain=0.8), dict(_BD, f=0.33, gain=0.9), dict(_BD, f=0.57, gain=1.0), dict(_TIMP, f=0.0, gain=0.6)]},
    "b3_thud": {"on": "thud", "bus": "air", "level": 0.45, "what": "b3: timpani, bass drum and tuba under the thud",
                "layers": [dict(_TIMP), dict(_BD, at=0.004), dict(_TUBA, at=0.01, b=1.0, fout=0.4)]},
})
for _k, _h in enumerate(HAULS):
    PARTS["b3_" + _h] = dict(PARTS["b3_haul"], on=_h, level=(0.20, 0.27, 0.34)[_k])
SCORE["b3"] = dict(SCORE["b1"], name="WHISTLE AND DRUMS",
                   line="b1 to his eyes, then a low held bamboo flute note cut before the slam, and drums with a low tuba to the end",
                   parts=[q for q in SCORE["b1"]["parts"] if q not in ("b1_spark", "b1_last", "b1_tree_eyes")]
                   + [("b1_tree_eyes", 0.7), "b3_whistle", "b3_roll", "b3_burst", "b3_haul1", "b3_haul2", "b3_haul3", "b3_arrive", "b3_drive1", "b3_drive2", "b3_thud"],
                   beats=("b3_beat", (0.16, 0.20, 0.25)), pulses=SCORE["c"]["pulses"])

DUCK_IN = 0.03                # a breath comes down over this long
DUCK_BACK = 0.20              # and what was ducked comes back up over this long after the hit
UNDER_IN = (0.035, 0.10)      # the open air is swallowed from this long after the punch to this long after it
UNDER_OUT = 0.012             # and opens over this long, ending ON the burst

# What a recording's own page says that matters here. Written into the manifest as its `note`.
NOTES = {
    "394964": "its page says it is SYNTHESISED (brown noise in Audacity): fetched by mistake, not used by any draft",
    "419161": "its page says it is twelve layers of one voice with effects: not used by any draft",
    "462162": "its page says it is a box of Lego recorded on a phone: not used by any draft",
    "860298": "its page says it is an ECG's electrical signal, not a sound in air: not used (831630, a stethoscope, is)",
    "149966": "its page says it is a LOG DRUM hit, pitched low, with a delayed reverb added: a real drum, processed by its author. Used only in c",
    "215571": "its page says it is an acoustic cymbal bowed with a bass bow, 'sampled and processed'. Used only in c",
    "522099": "its page says it is edited and layered from recordings of concrete and rebar. Used only in c",
    "755522": "its page says it is a mix of four other CC0 recordings: not used by any draft",
    "663651": "the breath out is 0.98 to 1.85 s; before it is a quick breath in, not used",
    "702796": "a held chord on A and D for its first 3.3 s (measured), then it moves: only the held part is used",
    "831630": "all of it is under 200 Hz: on small speakers it is not heard at all, so c lays a bass drum with it",
    "869103": "cracks from 2.6 s, the crash onto the ground at 6.92 s: the crash is the thud of all three",
    "867563": "its page says it is SYNTHETICALLY GENERATED: fetched by mistake, not used by any draft",
    "521885": "its page says it was made in a VST synthesiser: fetched by mistake, not used by any draft",
    "796546": "a real mark tree, high bar to low; its page says its author put a reverb (Tal Reverb-2) on it. The falling run of b1",
    "547645": "its page says it was edited (EQ, dynamics, transients softened): not used by any draft",
    "420501": "a toy glockenspiel's C major scale, a note every 4 s; measured 1062, 1199, 1315, 1408, 1585, 1810, 2014, 2130 Hz (4 to 48 cents sharp). The tune of b2",
    "459402": "single strikes of a koshi's rods; measured A5 871, B5 986, C6 1049, E6 1320, B6 2009 Hz, each with a louder partial about 2.7 times higher. The notes of b1",
    "851363": "a koshi in air, the same tuning as 459402 (A, B, C, E): played 2 semitones up in b1 so its C is D major's D",
    "517662": "three chime bars on a wood stand, measured C7 2071, E7 2654, G7 3217 Hz (517661, 517660, 517659 are one strike each, and those are used)",
    "435074": "a tingsha: one partial at 2934 Hz (F sharp 7, 15 cents flat), ringing 8 s",
}


# ------------------------------------------------------------------ building one draft

def parts_of(letter):
    """Every part of a draft, with its level: (part, name)."""
    S = SCORE[letter]
    out = []
    for p in S["parts"]:
        name, scale = (p, 1.0) if isinstance(p, str) else p
        out.append((dict(PARTS[name], level=PARTS[name]["level"] * scale), name))
    out += repeated(S["beats"][0], BEATS, S["beats"][1])
    out += repeated(S["pulses"][0], PULSES, S["pulses"][1])
    if S["haul_low"]:
        out += repeated("haul_low", HAULS, S["haul_low"])
    if S.get("haul_heavy"):
        out += repeated("haul_heavy", HAULS, S["haul_heavy"])
    return out


def place(P, L):
    """Where a layer starts, in seconds after its part's event."""
    if "f" in L:
        return L["f"] * (T[P["to"]] - T[P["on"]]) + L.get("at", 0.0)
    return L.get("at", 0.0)


def build_part(m, P, bus_lp):
    """A part's layers summed, levelled, and where it starts in the film (s). A part of the earth
    is low-passed here, BEFORE it is levelled, so `level` is what is heard."""
    t0 = T[P["on"]]
    laid = []
    for L in P["layers"]:
        L = dict(L)
        at = place(P, L)
        if L.pop("until", False):
            L["b"] = L["a"] + (T[P["to"]] - (t0 + at)) * L.get("pitch", 1.0)
        L.pop("f", None)
        L.pop("at", None)
        laid.append((at, gentle_layer(m, L)))
    first = min(at for at, _ in laid)
    out = np.zeros(1)
    for at, x in laid:
        out = lay(out, x, at - first)
    if P.get("echo"):                                        # a little space: quieter, darker copies of the part itself
        dry = out.copy()
        for k, (after, db) in enumerate(P["echo"]):
            out = lay(out, sos(dry, "low", ECHO_LP / (k + 1), 1) * 10 ** (db / 20.0), after)
    if bus_lp:
        out = sos(out, "low", bus_lp, 4)
    level = loud(out, 0.4 if P.get("bed") else 0.05)
    return out * P["level"] / (level + 1e-12), t0 + first


def ramp(n, t_from, t_to, v_from, v_to):
    """A line from `v_from` at `t_from` to `v_to` at `t_to` (s), held outside, as n samples."""
    t = np.arange(n) / SR
    return np.interp(t, [t_from, max(t_to, t_from + 1e-6)], [v_from, v_to])


def duck_of(n, ducks, t_part):
    """The gain over time of a part that begins at `t_part`: down before every later hit, back after."""
    g = np.ones(n)
    at = np.arange(n) / SR
    for event, before, db in ducks:
        t = T[event]
        if t_part >= t - 1e-6:
            continue
        low = 10 ** (db / 20.0)
        g *= np.interp(at, [t - before - DUCK_IN, t - before, t, t + DUCK_BACK], [1.0, low, low, 1.0])
    return g


def build_draft(m, letter):
    S = SCORE[letter]
    U = S["under"]
    n = int(round(T["end"] * SR))
    buses = {"air": np.zeros(n + SR * 8), "earth": np.zeros(n + SR * 8), "bright": np.zeros(n + SR * 8)}
    used = {}
    for P, name in parts_of(letter):
        lp = {"air": None, "earth": U["lp"], "bright": U["lp"] * 3.0}[P["bus"]]
        x, start = build_part(m, P, lp)
        a = int(round(start * SR))
        if a < 0:                                            # a lead-in that would begin before the film
            x, a = x[-a:], 0
        x = x * duck_of(n + SR * 8, S["ducks"], T[P["on"]])[a:a + len(x)]
        buses[P["bus"]][a:a + len(x)] += x
        for L in P["layers"]:
            used.setdefault(str(L["src"]), []).append("ult_%s %s" % (letter, name))
    air = buses["air"][:n]
    earth = (buses["earth"] + buses["bright"])[:n]
    # Under the earth: what was sounding in the open is swallowed (low-passed and dropped), and the
    # earth is heard instead. It opens ON the burst, and the earth's own tails stop there.
    u = ramp(n, T["punch"] + UNDER_IN[0], T["punch"] + UNDER_IN[1], 0.0, 1.0) * ramp(n, T["burst"] - UNDER_OUT, T["burst"], 1.0, 0.0)
    swallowed = sos(air, "low", U["air_lp"], 4) * 10 ** (U["air_db"] / 20.0)
    gate = ramp(n, T["punch"] - 0.01, T["punch"], 0.0, 1.0) * ramp(n, T["burst"] - 0.004, T["burst"] + 0.03, 1.0, 0.0)
    x = air * (1.0 - u) + swallowed * u + earth * gate
    x = sos(x, "high", 30.0)
    x = fade(x, 0.002, 0.06)
    x = x * LOUDEST / (loud(x, 0.05) + 1e-12)
    tall = float(np.abs(x).max())
    if tall > TALLEST:                                       # too tall to bend cleanly: it stays quieter instead
        x, tall = x * TALLEST / tall, TALLEST
    x = fade(soft(x), 0.002, 0.004)
    return x, tall, used


# ------------------------------------------------------------------ listening without ears: a draft

def window(x, a, b):
    return x[max(0, int(round(a * SR))):max(0, int(round(b * SR)))]


def rms_db(s, ref=1.0):
    return 20 * np.log10(np.sqrt((s ** 2).mean()) / ref + 1e-9) if len(s) else -180.0


def loudest_window(x, seconds):
    """(start s, RMS) of the loudest `seconds` of it."""
    w = int(SR * seconds)
    c = np.concatenate([[0.0], np.cumsum(x ** 2)])
    e = (c[w:] - c[:-w]) / w
    k = int(np.argmax(e))
    return k / SR, float(np.sqrt(e[k]))


def quietest_window(x, seconds):
    w = int(SR * seconds)
    c = np.concatenate([[0.0], np.cumsum(x ** 2)])
    e = (c[w:] - c[:-w]) / w
    k = int(np.argmin(e))
    return k / SR, float(np.sqrt(e[k]))


def top_share(s, above=1500.0):
    """The share of a stretch's energy that is above `above` Hz."""
    if len(s) < 64:
        return 0.0
    hi = sos(s, "high", above, 4)
    return float((hi ** 2).sum() / ((s ** 2).sum() + 1e-12))


MOMENTS = ("eyes", "slam", "punch", "knock", "burst", "claws", "haul1", "haul2", "haul3", "tree_eyes", "lash", "yank", "thud")


def measure(letter, x, tall):
    """Everything that can be said of a draft without hearing it, and what is wrong with it."""
    r = {"name": "ult_" + letter, "seconds": len(x) / SR, "peak": float(np.abs(x).max()), "bent_from": tall,
         "clipped": int((np.abs(x) >= 0.999).sum()), "rms": float(np.sqrt((x ** 2).mean())),
         "ends": (int(round(x[0] * 32767)), int(round(x[-1] * 32767))), "dc": float(x.mean())}
    at, top = loudest_window(x, 0.05)
    r["loudest_at"], r["loudest_rms"] = at, top
    r["peak_at"] = float(np.argmax(np.abs(x))) / SR
    # Each moment: the loudest 50 ms from 20 ms before it to 150 ms after, in dB under the thud's.
    r["moments"] = {}
    for e in MOMENTS:
        s = window(x, T[e] - 0.02, T[e] + 0.15)
        r["moments"][e] = 20 * np.log10(loudest_window(s, 0.05)[1] / top + 1e-9)
    # The breath before a hit: the 60 ms before it against the 300 ms before that.
    r["breath"] = {e: rms_db(window(x, T[e] - 0.06, T[e])) - rms_db(window(x, T[e] - 0.36, T[e] - 0.06)) for e in ("slam", "burst", "thud")}
    # Under the earth against the open air: level, and how much of it is above 1.5 kHz.
    under = window(x, T["launch"], T["knock"])
    before = window(x, T["kneel"], T["punch"])
    after = window(x, T["burst"], T["tree_eyes"])
    r["under_db"], r["before_db"], r["after_db"] = rms_db(under, top), rms_db(before, top), rms_db(after, top)
    r["under_top"], r["before_top"], r["after_top"] = top_share(under), top_share(before), top_share(after)
    r["under_centroid"], r["after_centroid"] = centroid(under), centroid(after)
    r["open_step_db"] = rms_db(window(x, T["burst"], T["burst"] + 0.10)) - rms_db(window(x, T["burst"] - 0.10, T["burst"]))
    r["sky_db"] = rms_db(window(x, T["rise"], T["eyes"]), top)
    r["tail_db"] = rms_db(window(x, T["thud"] + 0.25, T["end"] - 0.06), top)
    q_at, q = quietest_window(x[:len(x) - int(SR * 0.08)], 0.10)
    r["quietest_at"], r["quietest_db"] = q_at, 20 * np.log10(q / top + 1e-9)
    e10 = base.envelope(x, 10.0)
    quiet = e10 < top * 10 ** (-50 / 20.0)
    # stretches of 60 ms or more that are 50 dB under the thud
    gaps, k = [], 0
    while k < len(quiet):
        if quiet[k]:
            j = k
            while j < len(quiet) and quiet[j]:
                j += 1
            if (j - k) / SR >= 0.06 and j < len(quiet) - int(SR * 0.07):
                gaps.append((k / SR, j / SR))
            k = j
        else:
            k += 1
    r["gaps"] = gaps
    bad = []
    if abs(r["seconds"] - T["end"]) > 0.001:
        bad.append("it is %.3f s, not %.2f" % (r["seconds"], T["end"]))
    if r["clipped"] or r["peak"] > PEAK + 1e-6:
        bad.append("over the peak")
    if max(abs(r["ends"][0]), abs(r["ends"][1])) > 40:
        bad.append("an end is not at zero (click)")
    if not T["thud"] - 0.03 <= r["loudest_at"] <= T["thud"] + 0.12:
        bad.append("the loudest 50 ms is at %.2f s, not on the thud" % r["loudest_at"])
    for e in MOMENTS[:-1]:
        if r["moments"][e] > -1.5:
            bad.append("%s is within 1.5 dB of the thud (%.1f dB)" % (e, r["moments"][e]))
    h = [r["moments"][e] for e in HAULS]
    if not h[0] < h[1] < h[2]:
        bad.append("the three heaves do not each get louder (%.1f, %.1f, %.1f dB)" % tuple(h))
    if r["under_top"] > 0.5 * r["after_top"] or r["under_top"] > r["before_top"]:
        bad.append("the underground is not darker than the open air (%.0f%% above 1.5 kHz under, %.0f%% before, %.0f%% after)" % (
            100 * r["under_top"], 100 * r["before_top"], 100 * r["after_top"]))
    if r["open_step_db"] < 6:
        bad.append("the burst does not open (only %+.1f dB over the 100 ms before it)" % r["open_step_db"])
    for e, v in r["breath"].items():
        if v > -3:
            bad.append("no breath before the %s (%+.1f dB)" % (e, v))
    if gaps:
        bad.append("silence at " + ", ".join("%.2f..%.2f s" % g for g in gaps))
    r["bad"] = bad
    return r


def voice_levels(m, letter):
    """b1 and b2: the chime voice alone (its parts at their levels, sweeps and runs included, b's own thumps and
    creaks left out) and how loud it is over the film, in dB under its own loudest 150 ms: the swell and the
    release, as numbers. Also every single note: (s, note, voice, level, semitones moved)."""
    n = int(round(T["end"] * SR))
    stem = np.zeros(n + SR * 8)
    notes = []
    for P, name in parts_of(letter):
        if not name.startswith(letter + "_"):
            continue
        P = dict(P, layers=[L for L in P["layers"] if not L.get("body")])
        x, start = build_part(m, P, None)
        a = max(0, int(round(start * SR)))
        stem[a:a + len(x)] += x
        for L in P["layers"]:
            if "note" in L:
                notes.append((T[P["on"]] + place(P, L), L["note"], L["voice"], P["level"] * L.get("gain", 1.0), 12 * np.log2(L["pitch"])))
    top = loudest_window(stem[:n], 0.15)[1]
    spans = (("she rises", "rise", "whole"), ("she is whole", "whole", "approach"), ("she comes close", "approach", "vast"),
             ("the light falls", "star", "palm"), ("in his palm", "palm", "eyes"), ("HIS EYES", "eyes", "kneel"),
             ("beat 1", "beat1", "beat2"), ("beat 2", "beat2", "beat3"), ("beat 3", "beat3", "punch"),
             ("the tree's eyes", "tree_eyes", "lash"), ("after the thud", "thud", "end"))
    levels = [(label, 20 * np.log10(loudest_window(window(stem, T[a], T[b]), min(0.15, T[b] - T[a] - 0.001))[1] / top + 1e-9)) for label, a, b in spans]
    return levels, sorted(notes)


def report_lines(r):
    L = ["%s  %.3f s  peak %.3f (bent from %.2f)  clipped %d  rms %.3f  ends %d %d" % (
        r["name"], r["seconds"], r["peak"], r["bent_from"], r["clipped"], r["rms"], r["ends"][0], r["ends"][1]),
        "   loudest 50 ms: at %.3f s (the thud is at %.2f), RMS %.3f; tallest sample at %.3f s" % (
            r["loudest_at"], T["thud"], r["loudest_rms"], r["peak_at"]),
        "   each moment's loudest 50 ms, dB under the thud's: " + ", ".join("%s %.1f" % (e, r["moments"][e]) for e in MOMENTS),
        "   the breath before a hit (last 60 ms against the 300 ms before): " + ", ".join("%s %+.1f dB" % (e, v) for e, v in r["breath"].items()),
        "   under the earth (%.2f to %.2f s): %.1f dB under the thud, %.1f%% of it above 1.5 kHz, centroid %d Hz" % (
            T["launch"], T["knock"], r["under_db"], 100 * r["under_top"], r["under_centroid"]),
        "   open air before (%.2f to %.2f): %.1f dB, %.1f%% above 1.5 kHz;  after (%.2f to %.2f): %.1f dB, %.1f%%, centroid %d Hz" % (
            T["kneel"], T["punch"], r["before_db"], 100 * r["before_top"], T["burst"], T["tree_eyes"], r["after_db"], 100 * r["after_top"], r["after_centroid"]),
        "   the burst opens by %+.1f dB (100 ms after against 100 ms before);  her sky (%.2f to %.2f) %.1f dB;  the struggle %.1f dB" % (
            r["open_step_db"], T["rise"], T["eyes"], r["sky_db"], r["tail_db"]),
        "   quietest 100 ms: at %.2f s, %.1f dB under the thud;  silences (60 ms at 50 dB under): %s" % (
            r["quietest_at"], r["quietest_db"], ", ".join("%.2f..%.2f" % g for g in r["gaps"]) or "none"),
        "   " + ("OK by the numbers" if not r["bad"] else "WRONG: " + "; ".join(r["bad"]))]
    if r.get("voice"):
        levels, notes = r["voice"]
        L.insert(-1, "   the chime phrase alone, loudest 150 ms of each stretch, dB under its own top: " + ", ".join("%s %.0f" % lv for lv in levels))
        L.insert(-1, "   its %d notes (s, note, level against the thud, semitones the recording was moved): " % len(notes)
                 + ", ".join("%.2f %s %.2f %+.1f" % (t, nt, lv, st) for t, nt, _, lv, st in notes))
        gaps = np.diff([t for t, _, _, _, _ in notes if t < T["kneel"]])
        L.insert(-1, "   gaps between its notes up to the kneel: %s ms (uneven on purpose: not a grid); furthest a recording is moved: %.2f semitones" % (
            " ".join("%d" % round(1000 * g) for g in gaps), max(abs(st) for _, _, _, _, st in notes)))
    return L


def src_names(m, layers):
    seen, out = set(), []
    for L in layers:
        if L["src"] in seen:
            continue
        seen.add(L["src"])
        s = by_id(m, L["src"])
        out.append("%s (%s, by %s)" % (L["src"], s["title"].lower(), s["author"]))
    return ", ".join(out)


def write_readme(m, rows, videos):
    L = ["PAETE, MAKILING'S EMBRACE (his ultimate's cutscene): soundtracks for the owner to choose from by ear", "",
         "Written by tools/build_paete_ult_sfx.py. Do not edit by hand: change the tables there and rerun.",
         "Nothing here is in the game. 44100 Hz, mono, 16 bit, peak at most %.2f, %.1f s each." % (PEAK, T["end"]), "",
         "Owner, 2026-10-08: \"also needs sound effects\". And of the picture: \"it looks like its sped up. theres not",
         "much weight to the timing of things\", so each big moment has a lead-in, a breath before it and a tail.", "",
         "NOBODY HAS HEARD THESE. They were cut and checked by a machine that cannot listen. It measured the length,",
         "the peak, where the loudest 50 ms falls, each moment against the thud, the underground against the open",
         "air and the quiet stretches. It cannot say that any of them sounds good, heavy, sacred or natural.",
         "report_ult.txt has the numbers.", "",
         "WATCH: " + ", ".join("video/ult_%s.mp4" % k for k in SCORE) + " (the film with each).",
         "LISTEN: " + ", ".join("ult_%s.wav" % k for k in SCORE) + ".", "",
         "He chose b. Then, of b's instruments: \"idk about the bells. it should be more chimey and the overall instrument",
         "composition sounds too rigid and flat\". b1 and b2 are two answers to that, both b underneath: the gong, the bowl",
         "strikes and the chime tubes are gone, and her voice is a PHRASE on chimes that rises, opens, falls, blooms and",
         "comes back smaller. b1 is wind chimes, free and airy. b2 is a small clear tune on tines. Both are in D. A little",
         "space is three plain quieter copies of the chimes themselves (130, 270, 430 ms after), nothing synthesised.",
         "" if videos else "THE VIDEOS WERE NOT MADE IN THIS RUN.", "",
         "Every source is a Creative Commons 0 recording from Freesound (the id is the number in freesound.org/s/<id>/).",
         "Each sound's page was read before the download and states CC0. No recording is sped up or slowed by more",
         "than 3 semitones. Provenance, authors, SHA-256: tools/paete_sfx_sources.json. Downloads: ArtSource/paete/sfx-sources/.",
         "Three sources are real recordings their authors PROCESSED (a log drum pitched low with reverb, a bowed cymbal",
         "'sampled and processed', concrete edited and layered): they are used only in c, and say so in the json.",
         "b1's mark tree is a real one whose author put a reverb on his recording; the json says so.", ""]
    for letter, S in SCORE.items():
        L += ["=" * 110, "%s  %s" % (letter, S["name"]), "   " + S["line"], ""]
        U = S["under"]
        L += ["   under the earth: the soil low-passed at %d Hz; what was sounding above it low-passed at %d Hz and dropped %d dB" % (
            U["lp"], U["air_lp"], -U["air_db"]),
            "   breaths before hits: " + ", ".join("%s %d ms at %d dB" % (e, round(1000 * b), db) for e, b, db in S["ducks"]), ""]
        for P, name in sorted(parts_of(letter), key=lambda pn: (T[pn[0]["on"]], pn[1])):
            L += ["   %5.2f s  %-18s %-6s level %.2f   %s" % (T[P["on"]], name, P["bus"], P["level"], P["what"]),
                  "            from: %s" % src_names(m, P["layers"])]
        L.append("")
    L += ["=" * 110, "MEASURED (report_ult.txt has all of it):"]
    for r in rows:
        L += ["  " + s for s in report_lines(r)]
    (OUT / "README_ult.txt").write_text("\n".join(L) + "\n", encoding="utf-8", newline="\n")


# ------------------------------------------------------------------ the videos

def make_video(letter):
    """Frames plus one draft, joined by Blender's sequencer (there is no ffmpeg on this machine)."""
    VIDEO.mkdir(parents=True, exist_ok=True)
    out = VIDEO / ("ult_%s.mp4" % letter)
    if out.exists():
        out.unlink()
    label = "%s  %s" % (letter.upper(), SCORE[letter]["name"])
    cmd = [str(BLENDER), "-b", "--factory-startup", "-noaudio", "-P", str(MUX), "--",
           str(FRAMES), str(OUT / ("ult_%s.wav" % letter)), str(out), str(FPS), label]
    done = subprocess.run(cmd, capture_output=True, text=True)
    if not out.exists():
        print(done.stdout[-3000:], done.stderr[-3000:])
        raise RuntimeError("Blender did not write %s" % out)
    return out


def check_video(path, wav):
    """Is it there, about as long as the film, and does it carry sound? Read with OpenCV (frames) and
    by decoding its sound with the ffmpeg that imageio-ffmpeg bundles (read only; nothing is installed)."""
    import cv2
    cap = cv2.VideoCapture(str(path))
    frames, fps = int(cap.get(cv2.CAP_PROP_FRAME_COUNT)), float(cap.get(cv2.CAP_PROP_FPS))
    size = (int(cap.get(cv2.CAP_PROP_FRAME_WIDTH)), int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT)))
    cap.release()
    line = "%s  %d bytes  %d frames at %.2f fps = %.2f s  %dx%d" % (path.name, path.stat().st_size, frames, fps, frames / max(fps, 1e-9), size[0], size[1])
    try:
        import imageio_ffmpeg
        raw = subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), "-v", "error", "-i", str(path), "-vn", "-ac", "1", "-ar", str(SR),
                              "-f", "s16le", "-"], capture_output=True).stdout
        a = np.frombuffer(raw, dtype="<i2").astype(np.float64) / 32768.0
        if len(a) == 0:
            return line + "  NO SOUND IN IT", False
        at, top = loudest_window(a, 0.05)
        line += "  sound: %.2f s, peak %.3f, loudest 50 ms at %.2f s" % (len(a) / SR, float(np.abs(a).max()), at)
        ok = abs(frames / max(fps, 1e-9) - T["end"]) < 0.15 and abs(at - T["thud"]) < 0.15
    except ImportError:
        data = path.read_bytes()
        has = b"soun" in data and b"mp4a" in data
        line += "  sound track in the file: %s (not decoded: imageio-ffmpeg is not installed)" % ("yes" if has else "NO")
        ok = has and abs(frames / max(fps, 1e-9) - T["end"]) < 0.15
    return line + ("" if ok else "  WRONG"), ok


# ------------------------------------------------------------------ main

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", default="")
    ap.add_argument("--no-video", action="store_true")
    ap.add_argument("--video-only", action="store_true")
    args = ap.parse_args()
    wanted = [w for w in args.only.split(",") if w]              # "b1,b2", or a run of letters as before: "abc"
    letters = [k for k in SCORE if not wanted or k in wanted or any(w not in SCORE and len(k) == 1 and k in w for w in wanted)]
    m = load_manifest()
    rows = []
    if not args.video_only:
        base.TODAY = TODAY
        if [s for s in m["sources"] if not (CACHE / s["file"]).exists() and not s.get("skipped")]:
            base.fetch(m)
        if any("measured" not in s for s in m["sources"] if not s.get("skipped")):
            base.analyse(m)
        OUT.mkdir(parents=True, exist_ok=True)
        used = {}
        for letter in letters:
            x, tall, u = build_draft(m, letter)
            write(OUT / ("ult_%s.wav" % letter), x)
            rows.append(measure(letter, x, tall))
            if any(name.startswith(letter + "_") and any("note" in L for L in P["layers"]) for P, name in parts_of(letter)):
                rows[-1]["voice"] = voice_levels(m, letter)
            for k, v in u.items():
                used.setdefault(k, []).extend(v)
        lines = [s for r in rows for s in report_lines(r)]
        print("\n".join(lines))
    videos = []
    if not args.no_video:
        for letter in letters:
            path = make_video(letter)
            line, ok = check_video(path, OUT / ("ult_%s.wav" % letter))
            videos.append(line)
            print(line)
    if rows and not args.only:
        (OUT / "report_ult.txt").write_text("\n".join(lines + [""] + videos) + "\n", encoding="utf-8", newline="\n")
        write_readme(m, rows, videos)
        for s in m["sources"]:
            s["used_by_ult"] = sorted(set(used.get(s["id"], [])))
            if s["id"] in NOTES:
                s["note"] = NOTES[s["id"]]
        m["about_ult"] = ("Sources n 59 and up were fetched on 2026-10-08 for the ultimate's cutscene (tools/build_paete_ult_sfx.py). "
                          "`used_by_ult` is what that script uses; `used_by` is still LIANA LEAP's.")
        save_manifest(m)


if __name__ == "__main__":
    main()
