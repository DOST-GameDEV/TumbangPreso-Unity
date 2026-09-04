#!/usr/bin/env python3
"""
The bot probe across several seeds, so a number from it can mean something.

WHY THIS EXISTS
---------------
`CLAUDE.md` § 7.1 is unusually blunt about `BotBehaviourProbe`:

    "ITS NUMBERS ARE LIVENESS FLOORS, NEVER COMPARISONS AT n = 1. ... eight matches at
    the shipped settings spread from 58 to 100 throws, about 20 per cent, and two runs
    of one build with one seed are still not identical."

and `docs/TODO.md` § 16 carries the arithmetic for how many runs an arm of an A/B has to
buy before its answer means anything (three, for anything worth 20 per cent).

**Nothing automated that.** Every balance argument in this repository that cites the probe
cites ONE run, and the entry that says a fixed step solved the noise (§ 10) is contradicted
by the measurement that says it did not (§ 16). So this runs the probe across a list of
seeds and reports the SPREAD, which is the only honest way to quote it.

⚠️⚠️ THE DEFAULT SEED IS NOT TOUCHED AND MUST NOT BE. `BotBehaviourProbe` hard-codes
20260823 and its own note says why: *"a seed picked to make a red run green is a
measurement of nothing."* This passes `-tp-bot-seed` per run, which the probe reads only
when it is given, so the gate keeps measuring exactly what it measured yesterday. **Varying
the seed deliberately to measure noise and varying it to make a number look better are
opposite acts, and only the first one is done here.**

⚠️ THIS IS NOT PART OF THE QUALIFICATION GATE. It is minutes per seed, and a spread is
evidence for a decision rather than a pass or a fail. `tools/qualify.py` does not run it.

USAGE
-----
  python tools/bot_sweep.py --seeds 5
  python tools/bot_sweep.py --seeds 8 --mode Classic --map Eskinita
"""

import argparse
import datetime
import json
import pathlib
import re
import statistics
import subprocess
import sys

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass

ROOT = pathlib.Path(__file__).resolve().parent.parent
LOGS = ROOT / "Logs"
REPORTS = ROOT / "docs" / "reports"
UNITY = pathlib.Path(r"C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe")

# ⚠️ THE SEEDS ARE FIXED AND WRITTEN DOWN RATHER THAN RANDOM. A sweep whose inputs change
# every run cannot be compared against yesterday's sweep, which is the whole point of
# measuring a spread. Adding a seed is fine; changing one silently is not.
SEEDS = [20260823, 1, 7, 4242, 20260904, 99991, 31337, 5150]

METRICS = {
    "lata knocks": re.compile(r"lata knocks\s+(\d+)"),
    "tags": re.compile(r"tags\s+(\d+)"),
    "sabotages": re.compile(r"sabotages\s+(\d+)"),
    "throws": re.compile(r"throws\s+(\d+)"),
    "retrievals": re.compile(r"retrievals\s+(\d+)"),
    "camp penalties": re.compile(r"camp penalties\s+(\d+)"),
    "idle penalties": re.compile(r"idle penalties\s+(\d+)"),
    "skill uses": re.compile(r"skill uses\s+(\d+)"),
    "ultimate uses": re.compile(r"ultimate uses\s+(\d+)"),
}


def head_sha():
    r = subprocess.run(["git", "rev-parse", "HEAD"], cwd=str(ROOT),
                       capture_output=True, text=True)
    return r.stdout.strip() if r.returncode == 0 else "UNKNOWN"


def run_probe(seed, mode, map_name):
    """One Unity launch, one seed. Returns the parsed report or None."""
    report = LOGS / f"bot-behaviour-{mode}-{map_name}.txt"
    if report.exists():
        report.unlink()

    cmd = [str(UNITY), "-batchmode", "-runTests", "-projectPath", str(ROOT),
           "-buildTarget", "Win64", "-testPlatform", "PlayMode",
           "-testCategory", "!WallClock;!ThumbFloor",
           "-testFilter", "TumbangPreso.PlayTests.BotBehaviourProbe",
           "-testResults", str(LOGS / f"bot-sweep-{seed}.xml"),
           "-logFile", str(LOGS / f"bot-sweep-{seed}.log"),
           "-tp-bot-seed", str(seed)]

    subprocess.run(cmd, cwd=str(ROOT), capture_output=True, text=True, errors="replace")

    if not report.exists():
        return None

    text = report.read_text(encoding="utf-8", errors="replace")
    row = {"seed": seed}
    for name, pattern in METRICS.items():
        m = pattern.search(text)
        row[name] = int(m.group(1)) if m else None
    return row


def summarise(rows, name):
    values = [r[name] for r in rows if r.get(name) is not None]
    if not values:
        return None

    out = {
        "n": len(values),
        "min": min(values),
        "max": max(values),
        "mean": round(statistics.mean(values), 1),
        "median": statistics.median(values),
    }
    out["stdev"] = round(statistics.pstdev(values), 1) if len(values) > 1 else 0.0

    # ⚠️ THE SPREAD IS THE HEADLINE, NOT THE MEAN. `CLAUDE.md` § 7.1 quotes "58 to 100
    # throws, about 20 per cent" as the reason a single run cannot be compared, so the
    # number a reader needs first is how wide the band is relative to the middle of it.
    out["spread_pct"] = (round(100.0 * (out["max"] - out["min"]) / out["mean"], 1)
                         if out["mean"] else 0.0)
    return out


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--seeds", type=int, default=5, help="how many of the fixed seeds to run")
    ap.add_argument("--mode", default="HeroStrike", choices=["Classic", "HeroStrike"])
    ap.add_argument("--map", dest="map_name", default="Eskinita")
    args = ap.parse_args()

    seeds = SEEDS[:max(1, min(args.seeds, len(SEEDS)))]
    rows = []

    for seed in seeds:
        print(f"  seed {seed} ...", flush=True)
        row = run_probe(seed, args.mode, args.map_name)
        if row is None:
            print(f"    no report written; the run did not reach the probe")
            continue
        rows.append(row)
        print("    " + ", ".join(f"{k} {v}" for k, v in row.items() if k != "seed"))

    if not rows:
        print("no runs produced a report.", file=sys.stderr)
        return 1

    sha = head_sha()
    lines = []
    lines.append("# Bot behaviour across seeds")
    lines.append("")
    lines.append(f"- **Commit** `{sha}`")
    lines.append(f"- **Generated** {datetime.datetime.now().isoformat(timespec='seconds')}")
    lines.append(f"- **Mode** {args.mode} on {args.map_name}")
    lines.append(f"- **Seeds** {', '.join(str(r['seed']) for r in rows)}")
    lines.append("")
    lines.append("⚠️⚠️ **READ THE SPREAD BEFORE THE MEAN.** `CLAUDE.md` § 7.1: these numbers are "
                 "liveness floors, never comparisons at n = 1. A change worth less than the "
                 "spread below has not been measured by this probe, however different the two "
                 "runs look.")
    lines.append("")
    lines.append("| Metric | n | min | max | mean | median | stdev | spread |")
    lines.append("|---|---|---|---|---|---|---|---|")

    for name in METRICS:
        s = summarise(rows, name)
        if s is None:
            continue
        lines.append(f"| {name} | {s['n']} | {s['min']} | {s['max']} | {s['mean']} | "
                     f"{s['median']} | {s['stdev']} | **{s['spread_pct']}%** |")

    lines.append("")
    lines.append("## Every run")
    lines.append("")
    header = ["seed"] + list(METRICS)
    lines.append("| " + " | ".join(header) + " |")
    lines.append("|" + "---|" * len(header))
    for r in rows:
        lines.append("| " + " | ".join(str(r.get(h, "")) for h in header) + " |")

    REPORTS.mkdir(parents=True, exist_ok=True)
    out = REPORTS / f"bot-sweep-{sha[:12]}.md"
    out.write_text("\n".join(lines) + "\n", encoding="utf-8")

    (LOGS / "bot-sweep.json").write_text(json.dumps(
        {"sha": sha, "mode": args.mode, "map": args.map_name, "runs": rows,
         "summary": {n: summarise(rows, n) for n in METRICS}}, indent=2), encoding="utf-8")

    print("\n".join(lines))
    print(f"\nwritten: {out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
