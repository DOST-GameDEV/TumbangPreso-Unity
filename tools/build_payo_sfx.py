"""Payo: the sounds of a highland temple among rice terraces, cut from REAL recordings. Nothing here is synthesised.

  py -3 tools/build_payo_sfx.py            fetch what is missing, then cut every cue and bed
  py -3 tools/build_payo_sfx.py --fetch    only download and check the sources

For `Map.PayoSound` (Assets/TumbangPreso/Runtime/Map/PayoSound.cs), wired by the scene builder
(Editor/MapKit/PayoSceneBuilder.Life.cs). This file is tools/build_paete_run_sfx.py with Payo's tables and names
(made by Logs/payo/unity/make_payo_sfx_script.js); the method is that script's, to the letter: Creative Commons 0
recordings from Freesound's preview CDN, each sound's OWN PAGE read first and used only if it states CC0 and no other
Creative Commons licence, one request at a time, and every file's id, page, URL, author, licence and SHA-256 kept in
tools/payo_sfx_sources.json.

WHAT IS WHERE
  tools/payo_sfx_sources.json                   the sources, what each is used for, what was measured and cut
  ArtSource/payo/sfx-sources/                   the downloads, as fetched (high-quality MP3 previews)
  Assets/TumbangPreso/Art/audio/ambience/sfx_payo_<cue>_<n>.wav   the one-shots: mono, 44.1 kHz, 16 bit, peak 0.85
  Assets/TumbangPreso/Art/audio/ambience/payo_bed_<name>.wav      the loops

WHAT THE GAME ALREADY HAD AND THIS DOES NOT FETCH AGAIN: the rooster, the hens and wings (sfx_alley_rooster_crow_N,
sfx_alley_chicken_*_N, sfx_alley_amb_wings_N), cut by tools/build_eskinita_life_sfx.py.

THE CUT and THE LOOPS are described in tools/build_paete_run_sfx.py. One loop is LOW-PASSED: the village, at 2 kHz,
because it is heard from across the pit (a filter on a real recording, not a made sound).

⚠️ NOBODY HAS HEARD THESE. They were chosen by title, description and measurement only. The json keeps each
source's title, description and every cut's time span, so a wrong one is found fast: change its row below and rerun.
numpy, scipy, soundfile (libsndfile 1.1 or later, which decodes MP3).
"""
import argparse
import datetime
import hashlib
import html
import json
import re
import time
import urllib.request
import wave
from pathlib import Path

import numpy as np
from scipy import signal

SR = 44100
PEAK = 0.85
ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "tools/payo_sfx_sources.json"
CACHE = ROOT / "ArtSource/payo/sfx-sources"
OUT = ROOT / "Assets/TumbangPreso/Art/audio/ambience"
AGENT = "TumbangPresoLifeSfx/1.0 (indie game, CC0 field recordings, one request at a time; thegrinchisthebest@gmail.com)"
CC0_MARK = "creativecommons.org/publicdomain/zero/1.0"
OTHER_LICENCES = ("creativecommons.org/licenses/by", "creativecommons.org/licenses/sampling")
TODAY = datetime.date.today().isoformat()

# cue -> rows of (freesound id, how many events to take, longest event in s, whole?, floor dB, join s, tail s)
CUES = {
    # a monolith locking home: deep impacts and a stone breaking (the heaviest CC0 thuds the project already used for Paete)
    "lock": [("513694", 1, 2.5, False, 24, 0.2, 0.7), ("584891", 1, 2.5, False, 24, 0.2, 0.7), ("369981", 1, 2.5, False, 24, 0.2, 0.7), ("843339", 1, 2.5, False, 24, 0.2, 0.6)],
    # the gong at the gate when a round ends: a temple gong, an 80 cm gong under a felt mallet
    "gong": [("803158", 1, 9.0, True, 34, 0.6, 3.0), ("853153", 1, 9.0, True, 34, 0.6, 3.0), ("717952", 1, 9.0, True, 34, 0.6, 3.0)],
    # a geyser throwing: a sea blowhole's spout, a geyser in Bolivia
    "geyser": [("222642", 1, 4.0, False, 18, 0.6, 0.8), ("441152", 1, 4.0, False, 14, 0.8, 0.8)],
    # the hawk: red-tailed and red-shouldered hawks (the nearest CC0 raptors to a lawin)
    "hawk": [("616926", 2, 2.5, False, 24, 0.2, 0.5), ("770032", 1, 3.0, False, 24, 0.25, 0.5), ("319619", 1, 3.0, False, 24, 0.25, 0.5)],
    # one frog close by at night
    "frog": [("866772", 2, 2.5, False, 22, 0.25, 0.4), ("700367", 2, 2.0, False, 22, 0.25, 0.4), ("251938", 1, 2.0, False, 22, 0.25, 0.4), ("536759", 1, 2.0, False, 22, 0.25, 0.4)],
    # an owl far off
    "owl": [("465697", 1, 4.0, False, 24, 0.5, 0.6), ("745208", 1, 5.0, False, 24, 0.6, 0.6), ("159726", 1, 5.0, False, 24, 0.6, 0.6)],
}

# The loops: name -> (freesound ids, loop seconds, RMS it is brought to, low-pass in Hz or 0). The first id is the one
# wanted; the rest are stand-ins used, in order, when it could not be fetched.
BEDS = {
    "wind": (("394964", "544844", "827359", "711441"), 59.0, 0.06, 0),     # wind in leaves and trees on a mountainside
    "day": (("731228", "583930", "591621", "673163"), 53.0, 0.05, 0),      # a hill forest by day: birds, insects, a river far off (Kaski, Nepal)
    "night": (("645706", "853607", "222688"), 47.0, 0.05, 0),              # paddy frogs and crickets at night
    "village": (("149935", "399025", "796622"), 43.0, 0.045, 2000),        # a village far off, the top taken off by distance
    "fall": (("727804", "698306", "660250"), 31.0, 0.06, 0),               # water falling on rock: at the foot of each fall
    "grind": (("430589", "345846", "578491"), 11.0, 0.07, 0),              # a millstone turning: heavy stone moving on stone, while the monoliths travel
}


def load_manifest():
    if MANIFEST.exists():
        return json.loads(MANIFEST.read_text(encoding="utf-8"))
    return {"about": "Provenance for the sounds of the Payo map (a highland temple among rice terraces). tools/build_payo_sfx.py "
                     "reads this, fills in what it fetches and measures, and cuts the cues and loops from it. Every source is a real "
                     "recording under Creative Commons 0, confirmed on the sound's own Freesound page. Nobody has heard the cuts.",
            "cache": "ArtSource/payo/sfx-sources",
            "output": "Assets/TumbangPreso/Art/audio/ambience/sfx_payo_<cue>_<n>.wav and payo_bed_<name>.wav",
            "reused": "sfx_alley_rooster_crow_N.wav, sfx_alley_chicken_*_N.wav and sfx_alley_amb_wings_N.wav (tools/eskinita_life_sfx_sources.json) for the hamlets' chickens",
            "sources": [], "cues": {}}


def save_manifest(m):
    MANIFEST.write_text(json.dumps(m, indent=1, ensure_ascii=False) + "\n", encoding="utf-8")


def get(url):
    req = urllib.request.Request(url, headers={"User-Agent": AGENT})
    with urllib.request.urlopen(req, timeout=75) as f:
        return f.read()


def fetch(m, pause=1.5):
    """Download what is missing. The sound's own page is read first and must state CC0 and no
    other Creative Commons licence; the file's URL, author and description come from that page."""
    import soundfile as sf
    CACHE.mkdir(parents=True, exist_ok=True)
    known = {s["id"]: s for s in m["sources"]}
    wanted = [(cue, row[0]) for cue, rows in CUES.items() for row in rows] + [("bed_" + name, ident) for name, row in BEDS.items() for ident in row[0]]
    for cue, ident in wanted:
        if ident not in known:
            s = {"id": ident, "origin": "freesound", "page": f"https://freesound.org/s/{ident}/", "use": cue, "file": f"{cue}_fs{ident}.mp3"}
            m["sources"].append(s)
            known[ident] = s
    used = {ident for _cue, ident in wanted}
    for s in m["sources"]:
        s["used"] = s["id"] in used
        if not s["used"] and not s.get("dropped"):
            s["dropped"] = "fetched, then not used: see the note in CUES"
    for s in m["sources"]:
        path = CACHE / s["file"]
        if not path.exists():
            if s.get("skipped", "").startswith("its page") or not s.get("used", True):
                continue
            try:
                page = get(s["page"]).decode("utf-8", "replace")
                time.sleep(pause)
                if CC0_MARK not in page or any(o in page for o in OTHER_LICENCES):
                    s["licence_confirmed"] = False
                    s["skipped"] = "its page does not state Creative Commons 0 (and only that)"
                    print("SKIPPED %s: page does not state CC0" % s["id"])
                    continue
                url = re.search(r"previews/\d+/%s_\d+-hq\.mp3" % s["id"], page)
                who = re.search(r"<title>Freesound - (.*?) by ([^<]+)</title>", page, re.S)
                said = re.search(r'<meta name="twitter:description" content="([^"]*)"', page)
                if not url or not who:
                    raise ValueError("no preview URL or author on the page")
                s["url"] = "https://cdn.freesound.org/" + url.group(0)
                s["name_on_page"] = html.unescape(who.group(1)).strip()
                s["author"] = html.unescape(who.group(2)).strip()
                s["description"] = re.sub(r"\s+", " ", html.unescape(said.group(1))).strip()[:400] if said else ""
                s["licence"] = "Creative Commons 0 1.0 (as stated on the sound's page)"
                s["licence_confirmed"] = True
                s["fetched"] = TODAY
                data = get(s["url"])
                time.sleep(pause)
                path.write_bytes(data)
                s.pop("skipped", None)
            except Exception as e:                          # a failed URL is skipped and reported, never substituted
                s["skipped"] = "download failed: %s" % e
                print("SKIPPED %s: %s" % (s["id"], e))
                save_manifest(m)
                continue
            save_manifest(m)
        data = path.read_bytes()
        s["sha256"], s["bytes"] = hashlib.sha256(data).hexdigest(), len(data)
        info = sf.info(str(path))
        s["seconds"] = round(info.frames / info.samplerate, 3)
        s["format"] = "%s, %d Hz, %d ch" % (info.format, info.samplerate, info.channels)
        print("%-8s %-40s %7d bytes %6.2f s  %s | %s" % (s["id"], s["file"], len(data), s["seconds"], s.get("name_on_page", ""), s.get("description", "")[:90]))
    save_manifest(m)


def decode(path):
    import soundfile as sf
    x, sr = sf.read(str(path), dtype="float64", always_2d=True)
    x = x.mean(axis=1)
    if sr != SR:
        x = signal.resample_poly(x, SR, sr)
    return x


def envelope(x, ms=10.0):
    n = max(1, int(SR * ms / 1000))
    return np.sqrt(np.convolve(x * x, np.ones(n) / n, mode="same"))


def events(x, floor_db, join):
    """(start, end, peak) in samples of every stretch within `floor_db` of the recording's peak envelope."""
    env = envelope(x)
    top = env.max()
    if top <= 0:
        return []
    on = env > top * 10 ** (-floor_db / 20)
    edges = np.flatnonzero(np.diff(np.concatenate(([0], on.astype(np.int8), [0]))))
    spans = [[int(a), int(b)] for a, b in zip(edges[::2], edges[1::2])]
    merged = []
    for a, b in spans:
        if merged and a - merged[-1][1] < join * SR:
            merged[-1][1] = b
        else:
            merged.append([a, b])
    return [(a, b, float(np.abs(x[a:b]).max())) for a, b in merged if b - a > 0.03 * SR]


def cut(x, a, b, tail):
    env = envelope(x)
    level = env[a:b].max() * 10 ** (-34 / 20)
    end = b
    stop = min(len(x), b + int(tail * SR))
    while end < stop and env[end] > level:
        end += 1
    a = max(0, a - int(0.03 * SR))
    y = x[a:end].copy()
    y = signal.sosfiltfilt(signal.butter(2, 80, "highpass", fs=SR, output="sos"), y)
    fi, fo = int(0.004 * SR), min(int(0.04 * SR), len(y) // 3)
    y[:fi] *= np.linspace(0, 1, fi)
    y[-fo:] *= np.linspace(1, 0, fo)
    peak = np.abs(y).max()
    return (y * (PEAK / peak) if peak > 0 else y), a, end


def write(path, x):
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes(np.round(np.clip(x, -1, 1) * 32767).astype(np.int16).tobytes())


def centroid(x):
    spec = np.abs(np.fft.rfft(x * np.hanning(len(x))))
    return float((spec * np.fft.rfftfreq(len(x), 1 / SR)).sum() / max(spec.sum(), 1e-12))


def build(m):
    OUT.mkdir(parents=True, exist_ok=True)
    known = {s["id"]: s for s in m["sources"]}
    m["cues"] = {}
    for old in OUT.glob("sfx_payo_*.wav"):
        old.unlink()
    for cue, rows in CUES.items():
        n = 0
        made = []
        for ident, take, longest, whole, floor_db, join, tail in rows:
            s = known.get(ident)
            path = CACHE / s["file"] if s else None
            if not s or not path.exists() or not s.get("licence_confirmed"):
                print("  %-16s source %s is not available: skipped" % (cue, ident))
                continue
            x = decode(path)
            ev = events(x, floor_db, join)
            if not ev:
                continue
            if whole:
                a, b = ev[0][0], ev[-1][1]
                if (b - a) / SR > longest:
                    b = a + int(longest * SR)
                chosen = [(a, b)]
            else:
                fit = [e for e in ev if (e[1] - e[0]) / SR <= longest]
                if not fit:                                  # nothing short enough: the loudest event, cut to length
                    a, b, _ = max(ev, key=lambda e: e[2])
                    fit = [(a, min(b, a + int(longest * SR)), 0.0)]
                chosen = sorted(sorted(fit, key=lambda e: -e[2])[:take])
                chosen = [(a, b) for a, b, _ in chosen]
            for a, b in chosen:
                y, a2, b2 = cut(x, a, b, tail)
                n += 1
                name = f"sfx_payo_{cue}_{n}.wav"
                write(OUT / name, y)
                made.append({"file": name, "source": ident, "author": s.get("author", ""), "from_s": round(a2 / SR, 3), "to_s": round(b2 / SR, 3),
                             "seconds": round(len(y) / SR, 3), "centroid_hz": round(centroid(y)), "events_in_source": len(ev)})
                print("  %-34s %5.2f s  from fs%s %.2f..%.2f (%d events in it)  centroid %d Hz" %
                      (name, len(y) / SR, ident, a2 / SR, b2 / SR, len(ev), centroid(y)))
        m["cues"][cue] = made
    m["beds"] = {}
    for old in OUT.glob("payo_bed_*.wav"):
        old.unlink()
    for name, (idents, seconds, rms, lowpass) in BEDS.items():
        ident = next((i for i in idents if known.get(i) and (CACHE / known[i]["file"]).exists() and known[i].get("licence_confirmed")), None)
        if ident is None:
            print("  bed %-14s none of its sources %s is available: skipped" % (name, ", ".join(idents)))
            continue
        s = known[ident]
        path = CACHE / s["file"]
        x = decode(path)
        x = signal.sosfiltfilt(signal.butter(2, 60, "highpass", fs=SR, output="sos"), x)
        if lowpass:
            x = signal.sosfiltfilt(signal.butter(2, lowpass, "lowpass", fs=SR, output="sos"), x)
        fade = 3.0
        need = int((seconds + fade) * SR)
        if len(x) < need:
            seconds = max(8.0, len(x) / SR - fade - 0.2)
            need = int((seconds + fade) * SR)
        total = np.concatenate(([0.0], np.cumsum(x * x)))            # a one-second RMS every half second
        starts = np.arange(0, len(x) - SR, SR // 2)
        sec = np.sqrt((total[starts + SR] - total[starts]) / SR + 1e-12)
        span = int((seconds + fade) * 2)
        best, start = 1e9, 0
        for k in range(0, max(1, len(sec) - span)):
            w = sec[k:k + span]
            score = w.max() / max(np.median(w), 1e-9)
            if score < best:
                best, start = score, k
        a = max(0, min(start * (SR // 2), len(x) - need))
        seg = x[a:a + need].copy()
        n, f = int(seconds * SR), int(fade * SR)
        if len(seg) < n + f:                                           # a recording shorter than the shortest loop: loop what there is
            n = max(f + SR, len(seg) - f)
            seg = np.concatenate((seg, np.zeros(max(0, n + f - len(seg)))))
        t = np.linspace(0, np.pi / 2, f)
        loop = seg[:n].copy()
        loop[:f] = seg[:f] * np.sin(t) + seg[n:n + f] * np.cos(t)
        level = np.sqrt((loop * loop).mean())
        loop *= rms / max(level, 1e-9)
        peak = np.abs(loop).max()
        if peak > PEAK:
            loop *= PEAK / peak
        out = f"payo_bed_{name}.wav"
        write(OUT / out, loop)
        seam = abs(loop[0] - loop[-1])
        m["beds"][name] = {"file": out, "source": ident, "author": s.get("author", ""), "from_s": round(a / SR, 2), "seconds": round(seconds, 2),
                           "low_pass_hz": lowpass, "rms": round(float(np.sqrt((loop * loop).mean())), 4), "peak": round(float(np.abs(loop).max()), 3),
                           "loudest_second_over_median": round(float(best), 2), "seam_step": round(float(seam), 5), "centroid_hz": round(centroid(loop[:SR * 8]))}
        print("  %-28s %5.1f s  from fs%s at %.1f s  rms %.3f peak %.2f  loudest second / median %.2f  seam %.5f" %
              (out, seconds, ident, a / SR, m["beds"][name]["rms"], m["beds"][name]["peak"], best, seam))
    save_manifest(m)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--fetch", action="store_true")
    args = ap.parse_args()
    m = load_manifest()
    fetch(m)
    if not args.fetch:
        build(m)


main()
