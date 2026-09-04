#!/usr/bin/env python3
"""
Drive the real shipped player from a clean state and prove it can reach a finished match.

WHY THIS EXISTS
---------------
Every measurement in this repository runs inside the editor, against a warm `Library`, with
whatever the last session left in `persistentDataPath`. **None of that exists on the laptop
somebody carries into a hall.** `docs/TODO.md` § 143.15: the cold-start question is whether
the ARTIFACT works with no developer state behind it, and nothing here has ever asked it.

⚠️⚠️ IT DRIVES THE .exe, NOT THE EDITOR, AND THAT IS THE ENTIRE POINT. `SceneScriptCheck`'s
header records a shipped build that hard-crashed on a map select with Core, EditMode,
PlayMode and four editor checks all green, because **the editor resolves by class name what
the player cannot**. A cold-start test that ran in the editor would be blind to exactly the
class of fault it exists to find.

HOW IT DRIVES A PLAYER WITH NO INPUT
------------------------------------
Through the switches `NetStateReport` already ships, which were built to put two real
processes on a link and compare what each believed:

    -tp-host 8910 -tp-profile host -tp-allbots -tp-netreport <file> -tp-netseconds <n>

⚠️ `-tp-allbots` IS WHAT MAKES IT MEAN ANYTHING. Without it nobody presses a key, every seat
stands still, and a process that survived doing nothing is not evidence. With it all four
seats play, so the distances, the casts and the props in the report are real numbers.

⚠️⚠️ THE PROFILE IS NOT WIPED BY DEFAULT AND THAT IS DELIBERATE. Clearing
`persistentDataPath` destroys the settings, rebinds and career of whoever runs this, and on
this machine that is a person whose `Fullscreen` is false because he plays in a short wide
window (`CLAUDE.md` § 6.2b). `--clean-profile` moves the folder aside and puts it back, and
it is opt-in for that reason. **A truly clean MACHINE is a human test and stays in
`Attention.md`**; this is everything up to that.

USAGE
-----
  python tools/cold_start.py
  python tools/cold_start.py --clean-profile --seconds 60
"""

import argparse
import datetime
import json
import os
import pathlib
import re
import shutil
import subprocess
import sys
import time

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass

ROOT = pathlib.Path(__file__).resolve().parent.parent
LOGS = ROOT / "Logs"
REPORTS = ROOT / "docs" / "reports"

COMPANY = "BH Studios"
PRODUCT = "Tumbang Preso"


def head_sha():
    r = subprocess.run(["git", "rev-parse", "HEAD"], cwd=str(ROOT),
                       capture_output=True, text=True)
    return r.stdout.strip() if r.returncode == 0 else "UNKNOWN"


def player_path():
    desktop = pathlib.Path(os.path.expanduser("~")) / "Desktop"
    exe = desktop / "TumbangPreso-Unity" / "TumbangPreso.exe"
    return exe if exe.exists() else None


def profile_dir():
    """Where the player keeps settings, career and social. Unity's persistentDataPath."""
    local_low = pathlib.Path(os.path.expanduser("~")) / "AppData" / "LocalLow"
    return local_low / COMPANY / PRODUCT


def artifact_identity(exe):
    """
    What the artifact says it is, read from the StreamingAssets stamp.

    ⚠️ A COLD START OF THE WRONG BUILD PROVES NOTHING, so this refuses before spending
    minutes launching a player from a commit nobody asked about.
    """
    stamp = exe.parent / "TumbangPreso_Data" / "StreamingAssets" / "build-identity.json"
    if not stamp.exists():
        return None
    try:
        return json.loads(stamp.read_text(encoding="utf-8"))
    except Exception:
        return None


def run_player(exe, args, timeout, log):
    if log.exists():
        log.unlink()

    cmd = [str(exe), "-logFile", str(log)] + args
    started = time.time()
    try:
        proc = subprocess.run(cmd, cwd=str(exe.parent), timeout=timeout,
                              capture_output=True, text=True, errors="replace")
        code = proc.returncode
    except subprocess.TimeoutExpired:
        # ⚠️ A TIMEOUT IS A RESULT AND NOT AN ERROR HERE. A player that never exits is exactly
        # the failure a cold start is looking for, and it has to be reported rather than raised.
        code = None

    return {"seconds": round(time.time() - started, 1), "exit": code,
            "log": str(log), "log_bytes": log.stat().st_size if log.exists() else 0}


def read_log_faults(log):
    """Exceptions and errors the player wrote, which is the half a report file cannot carry."""
    if not log.exists():
        return ["the player wrote no log at all"]

    text = log.read_text(encoding="utf-8", errors="replace")
    faults = []
    for line in text.splitlines():
        if re.search(r"\b(Exception|NullReference|Assertion failed|Fatal)\b", line):
            faults.append(line.strip()[:200])
    return faults[:40]


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--clean-profile", action="store_true",
                    help="move persistentDataPath aside first and restore it afterwards")
    ap.add_argument("--seconds", type=int, default=45, help="how long the driven match runs")
    args = ap.parse_args()

    sha = head_sha()
    exe = player_path()

    steps = []
    LOGS.mkdir(exist_ok=True)

    if exe is None:
        print("REFUSED: there is no Windows player on the Desktop to cold start.\n"
              "         Build one first: GameBuilder.BuildWindows.", file=sys.stderr)
        return 2

    identity = artifact_identity(exe)
    if identity is None:
        print("REFUSED: the player carries no build-identity.json, so it predates the stamp "
              "and cannot be tied to a commit. Rebuild through GameBuilder.", file=sys.stderr)
        return 2

    if identity.get("sha") != sha:
        print(f"REFUSED: the player was built from {(identity.get('sha') or '?')[:12]} and HEAD "
              f"is {sha[:12]}. A cold start of a different commit proves nothing about this one.",
              file=sys.stderr)
        return 2

    moved = None
    profile = profile_dir()

    try:
        if args.clean_profile and profile.exists():
            moved = profile.with_name(profile.name + ".coldstart-backup")
            if moved.exists():
                shutil.rmtree(moved)
            shutil.move(str(profile), str(moved))
            print(f"  profile moved aside: {profile} -> {moved.name}")

        # ---- 1. does it launch at all, and can it say what it is ----------
        print("  1. identity ...", flush=True)
        step = run_player(exe, ["-tp-identity", "-batchmode", "-nographics"],
                          timeout=180, log=LOGS / "coldstart-identity.log")
        step["name"] = "launches and identifies itself"
        step["faults"] = read_log_faults(pathlib.Path(step["log"]))
        step["ok"] = step["log_bytes"] > 0 and not step["faults"]
        steps.append(step)

        # ---- 2. host a whole match with four bots, from nothing -----------
        print(f"  2. hosting a driven match for {args.seconds}s ...", flush=True)
        report = LOGS / "coldstart-netstate.txt"
        if report.exists():
            report.unlink()

        step = run_player(exe, ["-tp-host", "8910", "-tp-profile", "coldstart", "-tp-allbots",
                                "-tp-netreport", str(report),
                                "-tp-netseconds", str(args.seconds)],
                          timeout=args.seconds + 240,
                          log=LOGS / "coldstart-match.log")
        step["name"] = "hosts a match with four bots and finishes"
        step["faults"] = read_log_faults(pathlib.Path(step["log"]))
        step["report_written"] = report.exists()
        step["ok"] = report.exists() and not step["faults"]

        if report.exists():
            step["report_head"] = "\n".join(
                report.read_text(encoding="utf-8", errors="replace").splitlines()[:20])
        steps.append(step)

    finally:
        # ⚠️⚠️ RESTORED IN A `finally`, ALWAYS. A crash between the move and the restore would
        # otherwise leave somebody's settings, rebinds and career sitting under a `.backup` name
        # they have no reason to look for.
        if moved is not None:
            if profile.exists():
                shutil.rmtree(profile)
            shutil.move(str(moved), str(profile))
            print(f"  profile restored: {profile}")

    ok = all(s["ok"] for s in steps)

    lines = []
    lines.append("# Cold start of the shipped player")
    lines.append("")
    lines.append(f"- **Commit** `{sha}`")
    lines.append(f"- **Artifact** `{exe}`")
    lines.append(f"- **Built** {identity.get('builtAt')}, protocol {identity.get('protocol')}, "
                 f"{identity.get('target')}")
    lines.append(f"- **Profile cleared** {'yes' if args.clean_profile else 'NO (opt in with --clean-profile)'}")
    lines.append(f"- **Generated** {datetime.datetime.now().isoformat(timespec='seconds')}")
    lines.append("")
    lines.append(f"## Verdict: {'PASS' if ok else 'FAIL'}")
    lines.append("")
    lines.append("| Step | Verdict | Seconds | Detail |")
    lines.append("|---|---|---|---|")
    for s in steps:
        detail = "clean" if s["ok"] else "; ".join(s["faults"][:3]) or "no report written"
        lines.append(f"| {s['name']} | {'PASS' if s['ok'] else '**FAIL**'} | {s['seconds']} | {detail} |")
    lines.append("")

    for s in steps:
        if s.get("report_head"):
            lines.append("### What the player believed at exit")
            lines.append("")
            lines.append("```")
            lines.append(s["report_head"])
            lines.append("```")
            lines.append("")

    lines.append("⚠️ **A truly clean MACHINE is still a human test.** This clears the profile at "
                 "most; it cannot clear a driver, a firewall rule, a codec or a Visual C++ "
                 "runtime that this machine has and a borrowed one does not. `Attention.md`.")

    REPORTS.mkdir(parents=True, exist_ok=True)
    out = REPORTS / f"cold-start-{sha[:12]}.md"
    out.write_text("\n".join(lines) + "\n", encoding="utf-8")

    print("\n".join(lines))
    print(f"\nwritten: {out}")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
