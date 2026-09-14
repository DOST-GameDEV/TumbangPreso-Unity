"""Exercise one Zack skill through its owning client and compare three real players."""
import argparse
import csv
import hashlib
import json
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


def evaluate(folder, case):
    data = {name: rows(folder / (name + ".csv")) for name in ("host", "owner", "observer")}
    errors, measurements = [], {}
    host = data["host"]
    cast = next((row["time"] for row in host if row["charged"] == 1), None) if case == "magnet" else next((row["time"] for row in host if row["ultcharge"] < 1), None)
    if cast is None:
        return {"ok": False, "errors": ["The authoritative real skill never activated"], "measurements": {}}
    for name, records in data.items():
        expected = {"host": 0, "owner": 1, "observer": 2}[name]
        if len(records) < 100 or any(row["local"] != expected or row["zack"] != 1 for row in records):
            errors.append(name + " lacks continuous evidence from the required Zack seat")
            continue
        late = [row for row in records if cast + 3 < row["time"] < cast + 7]
        if len(late) < 10:
            errors.append(name + " lacks the post-action comparison window")
            continue
        result = {"samples": len(records)}
        measurements[name] = result
        if case == "magnet":
            loaded = [row for row in records if cast + .4 < row["time"] < cast + 1]
            result.update(loaded_samples=sum(row["charged"] == 1 for row in loaded),
                          charged_flight_samples=sum(row["chargedFlight"] == 1 for row in records),
                          late_charged=max(row["charged"] for row in late),
                          late_held=max(row["held"] for row in late),
                          late_charge_visuals=max(row["chargeVisuals"] if "chargeVisuals" in row else row["embers"] for row in late))
            if result["loaded_samples"] < 2: errors.append(name + " never showed the loaded charge")
            if result["charged_flight_samples"] < 1: errors.append(name + " never observed the empowered slipper flight")
            if result["late_charged"] or result["late_held"] or result["late_charge_visuals"]:
                errors.append(name + " retained charge, possession or carried charge visuals after the real throw")
        else:
            active = [row for row in records if row["bolts"] > 0]
            result.update(bolt_samples=len(active), max_bolts=max(row["bolts"] for row in records),
                          front_stun=max(row["frontStun"] for row in records),
                          front_rise=max(row["frontY"] for row in records)-records[0]["frontY"],
                          final_bolts=records[-1]["bolts"], final_ult=records[-1]["ultRemaining"])
            if len(active) < 2 or result["max_bolts"] != 3: errors.append(name + " missed or duplicated the three strike channels")
            result["charged_flight_samples"] = sum(row["chargedFlight"] == 1 for row in records)
            if result["charged_flight_samples"] < 1: errors.append(name + " missed the ultimate's charged follow-up throw")
            if result["front_stun"] < .5: errors.append(name + " did not observe the target's resolved shock")
            if result["final_bolts"] or result["final_ult"] > 0: errors.append(name + " leaked the strike or charged-throw window")
    return {"ok": not errors, "errors": errors, "measurements": measurements}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("exe", type=Path)
    parser.add_argument("--case", choices=["magnet", "thunderstrike"], required=True)
    parser.add_argument("--delay", type=float, default=0)
    parser.add_argument("--out", type=Path, required=True)
    args = parser.parse_args()
    folder = args.out.resolve(); folder.mkdir(parents=True, exist_ok=False)
    backups = []
    for name in ("zackhost", "zackowner", "zackobserver"):
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
                           "-screen-fullscreen", "0", "-tp-framecap", "60", "-tp-autostart", "3", "-tp-profile", "zack" + name,
                           "-tp-zackcase", args.case, "-tp-zacktrace", str(folder / (name + ".csv")),
                           "-logFile", str(folder / (name + ".log"))] + route)
        host = peer("host", ["-tp-host", "8990"])
        deadline = time.monotonic() + 45
        while time.monotonic() < deadline:
            path = folder / "host.log"
            text = path.read_text(errors="replace") if path.exists() else ""
            if re.search(r"arena installed: LocalSlot=0[^\n]*host=True", text): break
            if host.poll() is not None: raise RuntimeError("Host exited before its arena was ready")
            time.sleep(.25)
        else: raise RuntimeError("Host arena was not ready before the timeout")
        port = "8990"
        if args.delay:
            port = "8991"
            log = (folder / "link.log").open("w"); handles.append(log)
            proxy = subprocess.Popen([sys.executable, str(ROOT / "tools/net_link.py"), "--listen", port,
                                      "--to", "127.0.0.1:8990", "--delay", str(args.delay), "--seconds", "110"],
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
        observer = peer("observer", ["-tp-join", "127.0.0.1", "8990"])
        print("Tracing " + args.case + " from three actual players: " + str(folder), flush=True)
        deadline = time.monotonic() + 90
        while time.monotonic() < deadline and host.poll() is None:
            time.sleep(.5)
        # The host exits only after its final trace. A disconnect may freeze a
        # client's round clock just before that threshold; let writes flush,
        # evaluate the complete window, then close the owned clients in finally.
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
