"""Check accepted/refused ice effects in three real Windows player processes."""
import argparse
import csv
import hashlib
import json
import math
import os
from pathlib import Path
import re
import shutil
import statistics
import subprocess
import sys
import time

from run_unity_guarded import profile_root

ROOT = Path(__file__).resolve().parents[1]


def read(path):
    with path.open(newline="") as handle:
        return [{key: float(value) for key, value in row.items()} for row in csv.DictReader(handle)]


def evaluate(folder, case):
    data = {name: read(folder / (name + ".csv")) for name in ("host", "owner", "observer")}
    errors, details = [], {}
    # These peers run on the same PC. Unity's buffered ServerTime differs by peer;
    # use the shared machine clock for cross-process first/last sample ordering.
    if any(not rows or "wallTime" not in rows[0] for rows in data.values()):
        return {"ok": False, "errors": ["This timing check requires fresh traces with a shared wallTime column"], "measurements": {}}
    for name, rows in data.items():
        seat = {"host": 0, "owner": 1, "observer": 2}[name]
        if len(rows) < 100 or any(row["local"] != seat or row["cheska"] != 1 for row in rows):
            errors.append(name + " lacks continuous evidence from its required seat")
            continue
        result = {"samples": len(rows), "max_sheets": max(r["sheets"] for r in rows),
                  "max_walls": max(r["walls"] for r in rows),
                  "final_sheets": rows[-1]["sheets"], "final_walls": rows[-1]["walls"],
                  "final_qcharges": rows[-1]["qcharges"], "final_echarges": rows[-1]["echarges"]}
        details[name] = result
        if result["final_sheets"] or result["final_walls"]:
            errors.append(name + " retained ice after its expected lifetime")
        for field, count in (("sheet", "sheets"), ("wall", "walls")):
            active = [row for row in rows if row[count] > 0]
            if not active:
                continue
            if any(not math.isfinite(row[field + axis]) for row in active for axis in ("X", "Z")):
                errors.append(name + " has a nonfinite " + field + " position")
                continue
            result[field + "_position"] = [statistics.median(row[field + axis] for row in active) for axis in ("X", "Z")]
            result[field + "_first"] = active[0]["wallTime"]
            result[field + "_last"] = active[-1]["wallTime"]
        if case == "denied":
            if result["max_sheets"] or result["max_walls"]:
                errors.append(name + " created a refused ice effect")
        else:
            if result["max_sheets"] != 1:
                errors.append(name + " missed or duplicated the accepted sheet")
            expected_wall = 1 if case == "both" else 0
            if result["max_walls"] != expected_wall:
                errors.append(name + " has the wrong barricade count")
            if expected_wall and max(row["colliders"] for row in rows) < 3:
                errors.append(name + " never installed the accepted physical wall")
            if case == "sheet-then-denied":
                retained = [row for row in rows if 16 < row["elapsed"] < 17]
                if len(retained) < 5 or any(row["sheets"] != 1 for row in retained):
                    errors.append(name + " lost the earlier accepted sheet when a later request was refused")
    owner = data["owner"]
    if owner:
        for key in ("firstPredicted", "secondPredicted"):
            if not any(row[key] for row in owner): errors.append("Owner never predicted " + key)
        for key, expected in (("firstDenied", case == "denied"), ("secondDenied", case != "both")):
            actual = any(row[key] for row in owner)
            if actual != expected: errors.append("Owner has an unexpected " + key + " result")
    if len(details) == 3:
        for field in ("sheet", "wall"):
            key = field + "_position"
            if key not in details["host"]: continue
            for name in ("owner", "observer"):
                if key not in details[name]: continue
                error = math.dist(details["host"][key], details[name][key])
                details[name][field + "_position_error"] = error
                if error > .08: errors.append(name + " placed " + field + " away from the accepted host position")
            ends = [row[field + "_last"] for row in details.values() if field + "_last" in row]
            if len(ends) == 3:
                details["host"][field + "_expiry_spread"] = max(ends) - min(ends)
                if max(ends) - min(ends) > .45: errors.append(field + " expiry diverged by more than 450ms")
        if case != "denied" and "sheet_first" in details["host"]:
            host_rows = data["host"]
            host_first_index = next(i for i, row in enumerate(host_rows) if row["sheets"] > 0)
            # A sample only bounds creation between it and the previous sample.
            # Compare to the host's last observed absence, not a later first-presence
            # sample that could follow the owning client's first observed presence.
            host_absent = host_rows[max(0, host_first_index - 1)]["wallTime"]
            earlier = [row for row in owner if row["wallTime"] < host_absent]
            if any(row["sheets"] for row in earlier): errors.append("Owner created physical ice before host acceptance")
    return {"ok": not errors, "errors": errors, "measurements": details}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("exe", type=Path)
    parser.add_argument("--case", choices=["both", "denied", "sheet-then-denied"], required=True)
    parser.add_argument("--delay", type=float, default=150)
    parser.add_argument("--out", type=Path, required=True)
    args = parser.parse_args()
    folder = args.out.resolve(); folder.mkdir(parents=True, exist_ok=False)
    backups, processes, handles = [], [], []
    for name in ("icehost", "iceowner", "iceobserver"):
        profile = profile_root(["-tp-profile", name])
        for source in profile.rglob("*"):
            if not source.is_file() or source.suffix == ".log": continue
            target = folder / "profiles" / name / source.relative_to(profile)
            target.parent.mkdir(parents=True, exist_ok=True); shutil.copy2(source, target)
            backups.append((source, target, hashlib.sha256(source.read_bytes()).hexdigest()))
    startup = None
    if os.name == "nt":
        startup = subprocess.STARTUPINFO(); startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW; startup.wShowWindow = 0
    def launch(command):
        process = subprocess.Popen(command, cwd=ROOT, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, startupinfo=startup)
        processes.append(process); return process
    def wait_log(name, process, pattern, seconds):
        deadline = time.monotonic() + seconds
        while time.monotonic() < deadline:
            path = folder / (name + ".log")
            if path.exists() and re.search(pattern, path.read_text(errors="replace")): return
            if process.poll() is not None: raise RuntimeError(name + " exited during setup")
            time.sleep(.25)
        raise RuntimeError(name + " setup did not finish")
    def peer(name, route):
        return launch([str(args.exe.resolve()), "-batchmode", "-screen-width", "640", "-screen-height", "360",
                       "-screen-fullscreen", "0", "-tp-framecap", "60", "-tp-autostart", "3", "-tp-profile", "ice" + name,
                       "-tp-icecase", args.case, "-tp-icetrace", str(folder / (name + ".csv")),
                       "-logFile", str(folder / (name + ".log"))] + route)
    try:
        host = peer("host", ["-tp-host", "9010"])
        wait_log("host", host, r"arena installed: LocalSlot=0[^\n]*host=True", 45)
        port = "9010"
        if args.delay:
            port = "9011"; log = (folder / "link.log").open("w"); handles.append(log)
            proxy = subprocess.Popen([sys.executable, str(ROOT / "tools/net_link.py"), "--listen", port,
                                      "--to", "127.0.0.1:9010", "--delay", str(args.delay), "--seconds", "110"],
                                     cwd=ROOT, stdout=log, stderr=subprocess.STDOUT, startupinfo=startup)
            processes.append(proxy); time.sleep(1)
        owner = peer("owner", ["-tp-join", "127.0.0.1", port])
        wait_log("owner", owner, r"(?:seat changed|arena installed): LocalSlot=1[^\n]*host=False", 35)
        peer("observer", ["-tp-join", "127.0.0.1", "9010"])
        print("Tracing " + args.case + " in " + str(folder), flush=True)
        deadline = time.monotonic() + 90
        while time.monotonic() < deadline and host.poll() is None: time.sleep(.5)
        time.sleep(.5)
        result = evaluate(folder, args.case)
        result["case"] = args.case; result["delay_one_way_ms"] = args.delay
        result["exe_sha256"] = hashlib.sha256(args.exe.read_bytes()).hexdigest()
        runtime = args.exe.parent / (args.exe.stem + "_Data") / "Managed/TumbangPreso.Runtime.dll"
        result["runtime_sha256"] = hashlib.sha256(runtime.read_bytes()).hexdigest()
        (folder / "result.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
        print(json.dumps(result, indent=2), flush=True)
        return 0 if result["ok"] else 1
    finally:
        for process in processes:
            if process.poll() is None: process.terminate()
        for process in processes:
            try: process.wait(timeout=8)
            except subprocess.TimeoutExpired: process.kill(); process.wait()
        for handle in handles: handle.close()
        for source, backup, expected in backups:
            source.parent.mkdir(parents=True, exist_ok=True); shutil.copy2(backup, source)
            if hashlib.sha256(source.read_bytes()).hexdigest() != expected: raise RuntimeError("Named profile restoration failed")
        print("Preserved " + str(len(backups)) + " existing named-profile files", flush=True)


if __name__ == "__main__":
    raise SystemExit(main())
