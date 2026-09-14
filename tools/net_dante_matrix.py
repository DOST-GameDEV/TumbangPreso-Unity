"""Exercise one Dante skill through its owning client and compare three real players."""
import argparse
import csv
import hashlib
import json
import math
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import time

from run_unity_guarded import profile_root

ROOT = Path(__file__).resolve().parents[1]


def rows(path):
    with path.open(newline="") as handle:
        return [{key: float(value) for key, value in row.items()} for row in csv.DictReader(handle)]


def position(row, who):
    return [row[who + axis] for axis in "XYZ"]


def evaluate(folder, case):
    data = {name: rows(folder / (name + ".csv")) for name in ("host", "owner", "observer")}
    errors, measurements = [], {}
    guard = case in ("ward", "plating")
    # Round countdown estimates settle differently on joining peers. Anchor all
    # windows to the host's actual spend/activation in the shared server clock.
    def cast_started(row):
        return row["s2active"] == 1 if guard else row["ultcharge"] < 1 if case == "fissure" else row["s1charges"] == 1
    cast = next((row["time"] for row in data["host"] if cast_started(row)), None)
    if cast is None:
        return {"ok": False, "errors": ["The authoritative skill never activated"], "measurements": {}}
    for name, records in data.items():
        expected = {"host": 0, "owner": 1, "observer": 2}[name]
        if not records or any(row["local"] != expected for row in records):
            errors.append(name + " did not occupy its required seat")
            continue
        before = [row for row in records if cast - 2 < row["time"] < cast - .6]
        after = [row for row in records if cast + .8 < row["time"] < cast + 7]
        if len(before) < 5 or len(after) < 30:
            errors.append(name + " lacks pre-cast or recovery evidence")
            continue
        start = before[-1]
        if any(row["tremor"] != int(case == "tremor") or row["plating"] != int(case == "plating") for row in after):
            errors.append(name + " did not retain the case's actual selected variant")
        result = {"samples": len(records), "caster_rise": max(row["casterY"] - start["casterY"] for row in after),
                  "front_travel": max(math.dist(position(row, "front"), position(start, "front")) for row in after),
                  "rear_travel": max(math.dist(position(row, "rear"), position(start, "rear")) for row in after),
                  "shoe_travel": max(math.dist(position(row, "shoe"), position(start, "shoe")) for row in after),
                  "pillars": max(row["pillars"] for row in after)}
        measurements[name] = result
        if result["caster_rise"] > .12:
            errors.append(name + " launched the caster")
        if case == "fissure":
            result["ground_rumble"] = max(row.get("rumble", 0) for row in records)
            if result["ground_rumble"] < .0004 or result["ground_rumble"] > .011:
                errors.append(name + " missed the bounded local earthquake response")
            if max(row["frontY"] - start["frontY"] for row in after) < .6:
                errors.append(name + " did not launch the forward rival")
            if result["rear_travel"] > .18:
                errors.append(name + " struck behind the fissure")
            if result["pillars"] != 2 or records[-1]["pillars"] != 0:
                errors.append(name + " missed, duplicated or leaked mountain faces")
            if start["ultcharge"] < 12 or min(row["ultcharge"] for row in after) >= start["ultcharge"]:
                errors.append(name + " did not spend the actual ultimate charge")
        elif guard:
            active = [row for row in records if row["s2active"] and row["time"] > cast + .5]
            result["active_samples"] = len(active)
            result["active_stun"] = max((row["casterStun"] for row in active), default=-1)
            result["guard_travel"] = max((row["casterZ"] - start["casterZ"] for row in active), default=0)
            result["late_stun"] = max((row["casterStun"] for row in records if not row["s2active"] and row["time"] > cast + 3), default=0)
            if len(active) < 20 or not all(row["ward"] == 1 for row in active):
                errors.append(name + " did not sustain exactly one visible ward")
            if not all(row.get("orbitStones") == 3 for row in active) or records[-1].get("orbitStones") != 0:
                errors.append(name + " missed or leaked the three orbiting stone protectors")
            if result["active_stun"] > .02 or max(abs(row["casterX"] - start["casterX"]) for row in active) > .15:
                errors.append(name + " allowed a resolved hit through Carapace")
            if result["guard_travel"] < .7:
                errors.append(name + " froze the protected caster's own movement")
            if result["late_stun"] <= 0:
                errors.append(name + " retained immunity after Carapace ended")
            if records[-1]["ward"] or records[-1]["s2active"] or records[-1]["s2cooldown"] <= 0:
                errors.append(name + " leaked Carapace or lost its cooldown")
        else:
            if start["s1charges"] != 2 or any(row["s1charges"] != 1 for row in after):
                errors.append(name + " did not spend exactly one Stomp charge")
            if case == "tremor":
                if not any(row["frontTrip"] for row in after):
                    errors.append(name + " did not observe the trip")
                if result["front_travel"] > .18 or result["shoe_travel"] > .18:
                    errors.append(name + " launched a target or shoe during Long Tremor")
            else:
                if result["front_travel"] < .35:
                    errors.append(name + " did not shove the nearby rival")
                result["shoe_outward"] = start["shoeX"] - min(row["shoeX"] for row in after)
                if result["shoe_outward"] < .6 or max(abs(row["shoeZ"]-start["shoeZ"]) for row in after) > .35:
                    errors.append(name + " did not kick the loose slipper along its actual outward flight")
    if guard and measurements.get("host"):
        host = data["host"]
        if not any(row["guardTested"] for row in host) or not any(row["afterTested"] for row in host):
            errors.append("The host did not actually test both immunity windows")
    for name in ("owner", "observer"):
        if not data[name] or not data["host"]:
            continue
        # Compare settled authoritative positions in a shared clock window.
        host = next((row for row in reversed(data["host"]) if cast + 8 < row["time"] < cast + 10), None)
        peer = next((row for row in reversed(data[name]) if cast + 8 < row["time"] < cast + 10), None)
        if host is None or peer is None:
            errors.append(name + " lacks the settled shared comparison window")
            continue
        discrepancy = max(math.dist(position(host, who), position(peer, who)) for who in ("caster", "front", "rear", "shoe"))
        measurements.setdefault(name, {})["settled_error"] = discrepancy
        if discrepancy > .4:
            errors.append(name + " disagreed with the authoritative settled positions")
    return {"ok": not errors, "errors": errors, "measurements": measurements}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("exe", type=Path)
    parser.add_argument("--case", choices=["stomp", "tremor", "ward", "plating", "fissure"], required=True)
    parser.add_argument("--delay", type=float, default=0)
    parser.add_argument("--out", type=Path, required=True)
    args = parser.parse_args()
    folder = args.out.resolve(); folder.mkdir(parents=True, exist_ok=False)
    backups = []
    for name in ("dantehost", "danteowner", "danteobserver"):
        profile = profile_root(["-tp-profile", name])
        for source in profile.rglob("*"):
            if source.is_file() and source.suffix != ".log":
                target = folder / "profiles" / name / source.relative_to(profile)
                target.parent.mkdir(parents=True, exist_ok=True); shutil.copy2(source, target)
                backups.append((source, target, hashlib.sha256(source.read_bytes()).hexdigest()))
    processes, handles = [], []
    startup = None
    if os.name == "nt":
        startup = subprocess.STARTUPINFO(); startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW; startup.wShowWindow = 0
    def launch(command):
        process = subprocess.Popen(command, cwd=ROOT, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, startupinfo=startup)
        processes.append(process); return process
    try:
        def peer(name, route):
            return launch([str(args.exe.resolve()), "-batchmode", "-screen-width", "640", "-screen-height", "360",
                           "-screen-fullscreen", "0", "-tp-autostart", "3", "-tp-profile", "dante" + name,
                           "-tp-dantecase", args.case, "-tp-dantetrace", str(folder / (name + ".csv")),
                           "-logFile", str(folder / (name + ".log"))] + route)
        host = peer("host", ["-tp-host", "8960"]); time.sleep(7)
        port = "8960"
        if args.delay:
            port = "8961"
            log = (folder / "link.log").open("w"); handles.append(log)
            proxy = subprocess.Popen([sys.executable, str(ROOT / "tools/net_link.py"), "--listen", port,
                                      "--to", "127.0.0.1:8960", "--delay", str(args.delay), "--seconds", "110"],
                                     cwd=ROOT, stdout=log, stderr=subprocess.STDOUT, startupinfo=startup)
            processes.append(proxy); time.sleep(1)
        owner = peer("owner", ["-tp-join", "127.0.0.1", port])
        deadline = time.monotonic() + 35
        while time.monotonic() < deadline:
            path = folder / "owner.log"; text = path.read_text(errors="replace") if path.exists() else ""
            if re.search(r"(?:seat changed|arena installed): LocalSlot=1[^\n]*host=False", text):
                break
            if owner.poll() is not None:
                raise RuntimeError("Owner exited before taking seat one")
            time.sleep(.25)
        else:
            raise RuntimeError("Owner never took seat one")
        observer = peer("observer", ["-tp-join", "127.0.0.1", "8960"])
        print("Tracing " + args.case + " from three actual players: " + str(folder), flush=True)
        deadline = time.monotonic() + 90
        while time.monotonic() < deadline and any(process.poll() is None for process in (host, owner, observer)):
            time.sleep(.5)
        result = evaluate(folder, args.case)
        result["delay_one_way_ms"] = args.delay
        result["exe_sha256"] = hashlib.sha256(args.exe.read_bytes()).hexdigest()
        runtime = args.exe.parent / (args.exe.stem + "_Data") / "Managed/TumbangPreso.Runtime.dll"
        result["runtime_sha256"] = hashlib.sha256(runtime.read_bytes()).hexdigest()
        (folder / "result.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
        print(json.dumps(result, indent=2), flush=True)
        return 0 if result["ok"] else 1
    finally:
        for process in processes:
            if process.poll() is None:
                process.terminate()
        for process in processes:
            try:
                process.wait(timeout=8)
            except subprocess.TimeoutExpired:
                process.kill(); process.wait()
        for handle in handles:
            handle.close()
        for source, backup, expected in backups:
            source.parent.mkdir(parents=True, exist_ok=True); shutil.copy2(backup, source)
            if hashlib.sha256(source.read_bytes()).hexdigest() != expected:
                raise RuntimeError("Named test profile restoration failed")
        print("Preserved " + str(len(backups)) + " existing named-profile files", flush=True)


if __name__ == "__main__":
    raise SystemExit(main())
