"""Paete's BAKYA BLOOM (the pitcher plant that spits a wooden clog): its sounds, cut from real recordings.
DRAFTS, for the owner's ear, each option laid over the effect film so he can choose by ear.

  py -3 tools/build_paete_bloom_sfx.py              (every cue of a, b, c, the three sequences and the three videos)
  py -3 tools/build_paete_bloom_sfx.py --no-video   (only the wav files)
  py -3 tools/build_paete_bloom_sfx.py --only b     (one option)

Owner, 2026-10-08: "the pitcher plant fdoesnt have sfx". It is silent because every synthesised skill sound left
the game on 2026-09-29 and only LIANA LEAP has been redone. This is the sibling of tools/build_paete_skill_sfx.py
and uses its tools: the same manifest and downloads, the same Freesound CC0 fetch, its second set's `gentle_layer`
(no recording moved more than 3 semitones, tails as recorded) and its `finish` (levelled, peak bent, never cut).

The plant is a CUTE CHARACTER, not a monster: small, wet, plosive, a little comic. Three readings:
  a  WET AND BOTANICAL   mud, a mouth's pop, a spit and a cork: the plant is the voice
  b  WOOD AND KNOCK      wood blocks, bamboo, a creak, a cork: more clog than plant
  c  A SMALL CREATURE    a squeaky toy's squeak, mouth pops, a gulp of mud: the most comic

THE CUES, and what the game calls them (read from the code, nothing added to it):
  cast      sfx_cast_paete_sprout     PaeteHeroKit, his first press: the seed leaves his hand
  grow      (NO CUE NAME YET)         the seed lands, the pitcher pops up and shakes itself off (PaeteBloomFx.SeedLanding, RiseShake)
  ready     sfx_paete_sprout_ready    PaeteHazards: a clog has ripened and risen to its mouth
  command   sfx_cast_paete_command    PaeteHeroKit, his second press
  spit      sfx_paete_sprout_fire     PaeteHazards: it spits the clog
  knock     (NO CUE NAME YET)         the clog meets the standing can (PaeteBloomFx.ClogKnock; the can has sounds of its own)
  clogland  sfx_paete_sprout_land     PaeteHazards: despite its name this plays where the CLOG lands, not the seed
  uproot    sfx_paete_sprout_uproot   PaeteHazards: an opponent pulls the plant out

THE FILM is Logs/paete-ability-film/bloomfx_pit1_frames (166 frames at 30 fps, 5.5 s, three views side by side), acted
by Assets/TumbangPreso/Editor/MapKit/PaeteAbilityFilm.Bloom.cs. Its times are in `T`, ONCE. The film has no hand and
no press in it (the command is laid 0.12 s before the spit) and it does not show the plant withering at 40 s.

NOTHING goes into Assets/. NOBODY HAS HEARD ANY OF THIS: it was cut and checked by a machine that cannot listen
(lengths, peaks, where each cue's loudest instant is). Deterministic. numpy, scipy, soundfile.
"""
import argparse
import subprocess
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import build_paete_skill_sfx as base  # noqa: E402
from build_paete_skill_sfx import SR, PEAK, ROOT, OUT, CACHE, envelope, fade, finish, gentle_layer, lay, load_manifest, save_manifest, soft, write  # noqa: E402

TODAY = "2026-10-08"
FPS = 30
FRAMES = ROOT / "Logs/paete-ability-film/bloomfx_pit1_frames"
VIDEO = OUT / "video"
BLENDER = Path("C:/Program Files/Blender Foundation/Blender 5.0/blender.exe")
MUX = ROOT / "tools/mux_paete_ult_video.py"
LEVEL = 0.24                  # the RMS of each cue's loudest 100 ms, as LIANA LEAP's cues

# ------------------------------------------------------------------ THE ONE TABLE OF TIMES
# Seconds into the film. Flight 0.35, GrowFrom 0.9 + GrowFor 1.2, SpitAt 2.9 and PullAt 4.3 are the film's own
# constants. The knock and the clog's landing are flown by physics there: 3.18 and 3.38 are READ OFF THE FRAMES
# (the knock's burst is on f096, the clog lies still by f105), good to a frame or two.
T = {"cast": 0.00, "grow": 0.35, "ready": 2.10, "command": 2.78, "spit": 2.90, "knock": 3.18, "clogland": 3.38, "uproot": 4.30, "end": 5.50}

# How long each cue may be (s), and its fade out. The grow has the landing at 0 and the shake-off at 0.33 s (RiseShakeAt).
CUES = {"cast": (0.35, 0.08), "grow": (1.00, 0.20), "ready": (0.45, 0.10), "command": (0.25, 0.06), "spit": (0.42, 0.10),
        "knock": (0.45, 0.12), "clogland": (0.40, 0.10), "uproot": (0.85, 0.20)}
NAMES = {"cast": "sfx_cast_paete_sprout", "grow": None, "ready": "sfx_paete_sprout_ready", "command": "sfx_cast_paete_command",
         "spit": "sfx_paete_sprout_fire", "knock": None, "clogland": "sfx_paete_sprout_land", "uproot": "sfx_paete_sprout_uproot"}

# A layer is tools/build_paete_skill_sfx.py's `gentle_layer`: src, a..b, at, gain, pitch, hp, lp, fin, fout, keep_lead.
OPTIONS = {
    "a": {"name": "WET AND BOTANICAL", "line": "mud, a mouth's pop, a real spit and a cork: the plant itself is the voice", "cues": {
        "cast": [{"src": 542752, "a": 0.0, "b": 0.25, "gain": 1.0},
                 {"src": 850168, "a": 0.0, "b": 0.30, "at": 0.02, "gain": 0.7, "keep_lead": True, "fin": 0.01}],
        "grow": [{"src": 536766, "a": 0.10, "b": 0.44, "gain": 0.6},
                 {"src": 516643, "a": 0.62, "b": 1.50, "at": 0.12, "gain": 1.0, "keep_lead": True, "fin": 0.03, "fout": 0.2},
                 {"src": 540278, "a": 0.05, "b": 0.70, "at": 0.33, "gain": 0.5, "hp": 500, "keep_lead": True, "fin": 0.02, "fout": 0.3}],
        "ready": [{"src": 258269, "a": 0.0, "b": 0.30, "gain": 1.0},
                  {"src": 178615, "a": 0.05, "b": 0.42, "at": 0.03, "gain": 0.3, "hp": 500, "keep_lead": True, "fin": 0.03, "fout": 0.2}],
        "command": [{"src": 792505, "a": 0.0, "b": 0.25, "gain": 1.0}],
        "spit": [{"src": 643871, "a": 0.0, "b": 0.42, "gain": 1.0},
                 {"src": 622150, "a": 0.08, "b": 0.36, "at": 0.004, "gain": 0.9},
                 {"src": 473583, "a": 0.02, "b": 0.42, "at": 0.03, "gain": 0.5, "keep_lead": True, "fin": 0.02}],
        "knock": [{"src": 595039, "a": 5.24, "b": 5.75, "gain": 1.0, "fout": 0.2},
                  {"src": 533093, "a": 0.0, "b": 0.30, "at": 0.004, "gain": 0.6}],
        "clogland": [{"src": 218460, "a": 0.0, "b": 0.28, "gain": 1.0},
                     {"src": 536766, "a": 0.10, "b": 0.44, "at": 0.008, "gain": 0.5},
                     {"src": 569745, "a": 0.30, "b": 0.70, "at": 0.05, "gain": 0.25, "keep_lead": True, "fin": 0.02, "fout": 0.2}],
        "uproot": [{"src": 516643, "a": 0.62, "b": 1.45, "gain": 1.0, "keep_lead": True, "fin": 0.01, "fout": 0.2},
                   {"src": 683793, "a": 0.90, "b": 1.45, "at": 0.10, "gain": 0.5},
                   {"src": 106113, "a": 0.02, "b": 0.70, "at": 0.15, "gain": 0.4, "keep_lead": True, "fin": 0.03, "fout": 0.3}]}},
    "b": {"name": "WOOD AND KNOCK", "line": "wood blocks, bamboo, a hinge's creak and a champagne cork: more clog than plant", "cues": {
        "cast": [{"src": 855844, "a": 0.03, "b": 0.38, "gain": 1.0, "keep_lead": True, "fin": 0.02},
                 {"src": 506383, "a": 0.0, "b": 0.30, "gain": 0.5}],
        "grow": [{"src": 547414, "a": 0.12, "b": 0.42, "gain": 1.0},
                 {"src": 390123, "a": 0.08, "b": 0.44, "at": 0.10, "gain": 0.8, "keep_lead": True, "fin": 0.02},
                 {"src": 349273, "a": 0.60, "b": 1.20, "at": 0.33, "gain": 0.5}],
        "ready": [{"src": 533093, "a": 0.0, "b": 0.30, "gain": 1.0},
                  {"src": 386888, "a": 0.10, "b": 0.40, "at": 0.09, "gain": 0.7}],
        "command": [{"src": 275679, "a": 0.03, "b": 0.28, "gain": 1.0}],
        "spit": [{"src": 392624, "a": 0.10, "b": 0.50, "gain": 1.0},
                 {"src": 278974, "a": 0.0, "b": 0.25, "at": 0.006, "gain": 0.5},
                 {"src": 352719, "a": 0.02, "b": 0.40, "at": 0.02, "gain": 0.6, "hp": 150, "keep_lead": True, "fin": 0.02}],
        "knock": [{"src": 595039, "a": 0.20, "b": 0.70, "gain": 1.0, "fout": 0.2},
                  {"src": 275679, "a": 0.03, "b": 0.29, "at": 0.004, "gain": 0.7}],
        "clogland": [{"src": 326299, "a": 0.08, "b": 0.50, "gain": 1.0, "fout": 0.15},
                     {"src": 547414, "a": 0.12, "b": 0.42, "at": 0.006, "gain": 0.7}],
        "uproot": [{"src": 183452, "a": 0.16, "b": 0.62, "gain": 1.0},
                   {"src": 676987, "a": 1.00, "b": 1.70, "at": 0.05, "gain": 0.6, "keep_lead": True, "fin": 0.02, "fout": 0.2},
                   {"src": 569745, "a": 0.30, "b": 0.90, "at": 0.20, "gain": 0.4, "keep_lead": True, "fin": 0.03, "fout": 0.3}]}},
    "c": {"name": "A SMALL CREATURE", "line": "a dog toy's squeak, mouth pops, a gulp of mud and a cork: the most comic", "cues": {
        "cast": [{"src": 704170, "a": 0.10, "b": 0.45, "gain": 1.0},
                 {"src": 850168, "a": 0.0, "b": 0.30, "at": 0.02, "gain": 0.6, "keep_lead": True, "fin": 0.01}],
        "grow": [{"src": 516643, "a": 0.62, "b": 1.00, "gain": 0.7, "keep_lead": True, "fin": 0.01, "fout": 0.1},
                 {"src": 485959, "a": 0.15, "b": 0.75, "at": 0.30, "gain": 0.8},
                 {"src": 178615, "a": 0.05, "b": 0.50, "at": 0.36, "gain": 0.3, "hp": 500, "keep_lead": True, "fin": 0.03, "fout": 0.2}],
        "ready": [{"src": 485959, "a": 3.90, "b": 4.34, "gain": 1.0, "fout": 0.1},
                  {"src": 691907, "a": 0.0, "b": 0.40, "gain": 0.6}],
        "command": [{"src": 258269, "a": 0.0, "b": 0.25, "gain": 1.0}],
        "spit": [{"src": 643871, "a": 0.0, "b": 0.42, "gain": 1.0},
                 {"src": 757257, "a": 0.0, "b": 0.34, "at": 0.004, "gain": 0.8},
                 {"src": 588509, "a": 0.06, "b": 0.30, "at": 0.08, "gain": 0.3, "keep_lead": True, "fin": 0.01}],
        "knock": [{"src": 595039, "a": 5.24, "b": 5.75, "gain": 1.0, "fout": 0.2},
                  {"src": 92624, "a": 0.05, "b": 0.40, "at": 0.08, "gain": 0.5, "keep_lead": True, "fin": 0.02, "fout": 0.15}],
        "clogland": [{"src": 218460, "a": 0.0, "b": 0.28, "gain": 1.0},
                     {"src": 533093, "a": 0.0, "b": 0.28, "at": 0.09, "gain": 0.5}],
        "uproot": [{"src": 516643, "a": 0.62, "b": 1.45, "gain": 1.0, "keep_lead": True, "fin": 0.01, "fout": 0.2},
                   {"src": 622150, "a": 0.08, "b": 0.36, "at": 0.02, "gain": 0.6},
                   {"src": 485959, "a": 8.15, "b": 8.75, "at": 0.12, "gain": 0.7}]}},
}

# d (owner, 2026-10-08, of a, b, c: "for the sound options, i like a and C the most but they're all too harsh on the
# ears"). ONE blend: a's wet botanical body with c's creature where it is charming (the squeak as it shakes off, on
# ready and on the uproot), made GENTLE (`soft`): every layer fades in over at least `fin` s (no hard edge), the whole
# cue is low-passed at `lp` Hz and dipped `dip_db` at 3 kHz, the squeaky toy is 3 semitones down and well under the
# rest (a coo, not a squeal), no slapstick and no champagne cork, the level is lower and the peak is held to `peak`
# by turning the cue DOWN, never by bending it, and the tails are longer. a, b and c are not touched by any of it.
OPTIONS["d"] = {"name": "WET AND GENTLE", "line": "a's mud, pop and spit with c's squeak as a quiet coo, softened: slower attacks, the top rolled off, quieter",
                "soft": {"fin": 0.006, "lp": 5500.0, "dip_db": -5.0, "level": 0.15, "peak": 0.55, "tail": 1.6}, "cues": {
    "cast": [{"src": 542752, "a": 0.0, "b": 0.25, "gain": 0.8},
             {"src": 850168, "a": 0.0, "b": 0.30, "at": 0.02, "gain": 0.8, "keep_lead": True, "fin": 0.02}],
    "grow": [{"src": 536766, "a": 0.10, "b": 0.44, "gain": 0.6},
             {"src": 516643, "a": 0.62, "b": 1.50, "at": 0.12, "gain": 1.0, "keep_lead": True, "fin": 0.04, "fout": 0.25},
             {"src": 485959, "a": 0.15, "b": 0.75, "at": 0.30, "pitch": 0.8409, "gain": 0.3},
             {"src": 540278, "a": 0.05, "b": 0.70, "at": 0.33, "gain": 0.4, "hp": 500, "keep_lead": True, "fin": 0.03, "fout": 0.3}],
    "ready": [{"src": 485959, "a": 3.90, "b": 4.34, "pitch": 0.8409, "gain": 0.5, "fout": 0.15},
              {"src": 258269, "a": 0.0, "b": 0.30, "gain": 1.0},
              {"src": 178615, "a": 0.05, "b": 0.42, "at": 0.03, "gain": 0.3, "hp": 500, "keep_lead": True, "fin": 0.03, "fout": 0.2}],
    "command": [{"src": 691907, "a": 0.0, "b": 0.40, "gain": 1.0}],
    "spit": [{"src": 643871, "a": 0.0, "b": 0.42, "gain": 0.7},
             {"src": 622150, "a": 0.08, "b": 0.36, "at": 0.004, "gain": 1.0},
             {"src": 473583, "a": 0.02, "b": 0.42, "at": 0.03, "gain": 0.5, "keep_lead": True, "fin": 0.02}],
    "knock": [{"src": 595039, "a": 5.24, "b": 5.75, "gain": 0.7, "fout": 0.2},
              {"src": 218460, "a": 0.0, "b": 0.28, "at": 0.004, "gain": 1.0}],
    "clogland": [{"src": 218460, "a": 0.0, "b": 0.28, "gain": 1.0},
                 {"src": 536766, "a": 0.10, "b": 0.44, "at": 0.008, "gain": 0.6},
                 {"src": 569745, "a": 0.30, "b": 0.70, "at": 0.05, "gain": 0.2, "keep_lead": True, "fin": 0.02, "fout": 0.2}],
    "uproot": [{"src": 516643, "a": 0.62, "b": 1.45, "gain": 1.0, "keep_lead": True, "fin": 0.02, "fout": 0.25},
               {"src": 485959, "a": 8.15, "b": 8.75, "at": 0.12, "pitch": 0.8409, "gain": 0.3},
               {"src": 106113, "a": 0.02, "b": 0.70, "at": 0.15, "gain": 0.35, "keep_lead": True, "fin": 0.03, "fout": 0.3}]}}


def build_cue(m, letter, cue):
    seconds, tail = CUES[cue]
    G = OPTIONS[letter].get("soft")
    out = np.zeros(1)
    for L in OPTIONS[letter]["cues"][cue]:
        L = dict(L)
        at = L.pop("at", 0.0)
        L["b"] = min(L["b"], len(base.source(m, L["src"])) / SR - 0.001)      # a span never runs past its recording
        if G:
            L["fin"] = max(L.get("fin", 0.0), G["fin"])
        out = lay(out, gentle_layer(m, L), at)
    if not G:
        return finish(out, seconds, LEVEL, tail)
    from scipy import signal
    out = base.sos(out, "low", G["lp"], 2)
    b3, a3 = signal.iirpeak(3000.0, 1.2, fs=SR)                              # what is round 3 kHz, taken part of the way out
    out = out - (1.0 - 10 ** (G["dip_db"] / 20.0)) * signal.lfilter(b3, a3, out)
    n = int(round(seconds * SR))
    out = fade(np.concatenate([out, np.zeros(max(0, n - len(out)))])[:n], 0.004, tail * G["tail"])
    out = out * G["level"] / (base.loud(out, 0.1) + 1e-12)
    tall = float(np.abs(out).max())
    if tall > G["peak"]:
        out = out * G["peak"] / tall
    return out, tall


def harshness(x):
    """(share of the energy above 3 kHz, crest factor in dB: the peak over the RMS of what is sounding)."""
    hi = base.sos(x, "high", 3000.0, 4)
    live = x[np.abs(x) > 1e-4]
    return float((hi ** 2).sum() / ((x ** 2).sum() + 1e-12)), 20 * np.log10(np.abs(x).max() / (np.sqrt((live ** 2).mean()) + 1e-12))


def measure(name, x, over, seconds):
    e = envelope(x, 2.0)
    r = {"name": name, "seconds": len(x) / SR, "peak": float(np.abs(x).max()), "bent_from": over,
         "clipped": int((np.abs(x) >= 0.999).sum()), "top_at": float(np.argmax(e)) / SR,
         "ends": (int(round(x[0] * 32767)), int(round(x[-1] * 32767)))}
    bad = []
    if r["clipped"] or r["peak"] > PEAK + 1e-6:
        bad.append("over the peak")
    if abs(r["seconds"] - seconds) > 0.002:
        bad.append("not its length")
    if max(abs(r["ends"][0]), abs(r["ends"][1])) > 40:
        bad.append("an end is not at zero")
    r["bad"] = bad
    return r


def make_video(letter):
    VIDEO.mkdir(parents=True, exist_ok=True)
    out = VIDEO / ("bloom_%s.mp4" % letter)
    if out.exists():
        out.unlink()
    cmd = [str(BLENDER), "-b", "--factory-startup", "-noaudio", "-P", str(MUX), "--", str(FRAMES),
           str(OUT / ("bloom_sequence_%s.wav" % letter)), str(out), str(FPS), "BLOOM %s  %s" % (letter.upper(), OPTIONS[letter]["name"])]
    done = subprocess.run(cmd, capture_output=True, text=True)
    if not out.exists():
        print(done.stdout[-2000:], done.stderr[-2000:])
        raise RuntimeError("Blender did not write %s" % out)
    import cv2
    cap = cv2.VideoCapture(str(out))
    frames, fps = int(cap.get(cv2.CAP_PROP_FRAME_COUNT)), float(cap.get(cv2.CAP_PROP_FPS))
    cap.release()
    data = out.read_bytes()
    return "%s  %d bytes  %d frames at %.0f fps = %.2f s  sound track in the file: %s" % (
        out.name, len(data), frames, fps, frames / max(fps, 1e-9), "yes" if b"mp4a" in data else "NO")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", default="")
    ap.add_argument("--no-video", action="store_true")
    args = ap.parse_args()
    m = load_manifest()
    base.TODAY = TODAY
    OUT.mkdir(parents=True, exist_ok=True)
    lines, used = [], {}
    for letter, O in OPTIONS.items():
        if args.only and letter not in args.only:
            continue
        track = np.zeros(int(round(T["end"] * SR)) + SR)
        for cue in CUES:
            x, over = build_cue(m, letter, cue)
            write(OUT / ("bloom_%s_%s.wav" % (cue, letter)), x)
            r = measure("bloom_%s_%s" % (cue, letter), x, over, CUES[cue][0])
            lines.append("%-20s %.3f s  peak %.3f (bent from %.2f)  clipped %d  loudest at %.3f s  game cue: %-24s %s" % (
                r["name"], r["seconds"], r["peak"], r["bent_from"], r["clipped"], r["top_at"], NAMES[cue] or "(none yet)",
                "OK" if not r["bad"] else "WRONG: " + "; ".join(r["bad"])))
            a = int(round(T[cue] * SR))
            track[a:a + len(x)] += x
            for L in O["cues"][cue]:
                used.setdefault(str(L["src"]), []).append("bloom_%s_%s" % (cue, letter))
        track = fade(soft(track[:int(round(T["end"] * SR))]), 0.002, 0.01)
        write(OUT / ("bloom_sequence_%s.wav" % letter), track)
        share, crest = harshness(track)
        lines.append("bloom_sequence_%s    %.3f s  peak %.3f  clipped %d  above 3 kHz %.1f%% of the energy  crest %.1f dB" % (
            letter, len(track) / SR, float(np.abs(track).max()), int((np.abs(track) >= 0.999).sum()), 100 * share, crest))
        if not args.no_video:
            lines.append(make_video(letter))
    print("\n".join(lines))
    if not args.only:
        (OUT / "report_bloom.txt").write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
        for s in m["sources"]:
            s["used_by_bloom"] = sorted(set(used.get(s["id"], [])))
        save_manifest(m)


if __name__ == "__main__":
    main()
