"""Exercise one Sean skill through its owning client and compare three real players."""
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


def evaluate(folder, case, hold_charge=False, rejoined_seat=None, delay=0):
    data = {name: rows(folder / (name + ".csv")) for name in ("host", "owner", "observer")}
    errors, measurements = [], {}
    host = data["host"]
    cast = next((row["time"] for row in host if row["charged"] == 1), None) if case == "ignite" else next((row["time"] for row in host if row["ultcharge"] < 1), None)
    if cast is None:
        return {"ok": False, "errors": ["The authoritative real skill never activated"], "measurements": {}}
    if hold_charge:
        live_start = next(row["wallTime"] for row in host if row["charged"] == 1)
        for name, records in data.items():
            expected = {"host": 0, "owner": 1, "observer": 2}[name]
            if len(records) < 100 or any(row["local"] != expected for row in records):
                errors.append(name + " lacks continuous held-charge evidence"); continue
            ready_at = records[0]["wallTime"]
            sync_budget = (2 * delay / 1000 if name == "owner" else 0) + .25
            compare_from = live_start + 3
            first_charge = next((row["wallTime"] for row in records if row["charged"]), None)
            if name == rejoined_seat:
                # Arena presence precedes the requested world reply. Account for
                # its configured round trip, then require continuous restored state.
                compare_from = max(compare_from, ready_at + sync_budget)
                if first_charge is None or first_charge > ready_at + sync_budget:
                    errors.append(name + " did not hydrate within the bounded joining reply window")
            late = [row for row in records if compare_from < row["wallTime"] < live_start + 8]
            result = {"samples": len(records), "live_window_samples": len(late),
                      "charged_samples": sum(row["charged"] == 1 for row in late),
                      "ember_samples": sum(row["embers"] > 0 for row in late),
                      "final_charged": records[-1]["charged"], "final_embers": records[-1]["embers"],
                      "final_skill_charges": records[-1]["s2charges"]}
            if name == rejoined_seat:
                result["join_to_charge_seconds"] = first_charge - ready_at if first_charge is not None else None
                result["joining_reply_budget_seconds"] = sync_budget
            measurements[name] = result
            if len(late) < 10 or result["charged_samples"] < len(late) - 3 or result["ember_samples"] < len(late) - 3:
                errors.append(name + " failed to retain or reconstruct the active held charge")
            if result["final_charged"] or result["final_embers"]:
                errors.append(name + " restarted or leaked the charge after its original expiry")
            if records[-1]["s2charges"] != host[-1]["s2charges"]:
                errors.append(name + " changed the spent skill resource")
        return {"ok": not errors, "errors": errors, "measurements": measurements}
    for name, records in data.items():
        expected = {"host": 0, "owner": 1, "observer": 2}[name]
        if len(records) < 100 or any(row["local"] != expected or row["sean"] != 1 for row in records):
            errors.append(name + " lacks continuous evidence from the required Sean seat")
            continue
        late = [row for row in records if cast + 3 < row["time"] < cast + 7]
        if len(late) < 10:
            errors.append(name + " lacks the post-action comparison window")
            continue
        result = {"samples": len(records)}
        measurements[name] = result
        if case == "ignite":
            loaded = [row for row in records if cast + .4 < row["time"] < cast + 1]
            result.update(loaded_samples=sum(row["charged"] == 1 for row in loaded),
                          fire_flight_samples=sum(row["fireFlight"] == 1 for row in records),
                          late_charged=max(row["charged"] for row in late),
                          late_held=max(row["held"] for row in late),
                          late_embers=max(row["embers"] for row in late))
            if result["loaded_samples"] < 2: errors.append(name + " never showed the loaded charge")
            if result["fire_flight_samples"] < 1: errors.append(name + " never observed the empowered slipper flight")
            if result["late_charged"] or result["late_held"] or result["late_embers"]:
                errors.append(name + " retained charge, possession or carried embers after the real throw")
        else:
            active = [row for row in records if row["craters"] > 0]
            result.update(rise=max(row["casterY"] for row in records)-records[0]["casterY"],
                          crater_samples=len(active), max_craters=max(row["craters"] for row in records),
                          first_crater_grounded=active[0]["grounded"] if active else -1,
                          final_craters=records[-1]["craters"], final_pose=records[-1]["pose"])
            if result["rise"] < .8: errors.append(name + " missed the committed leap")
            if len(active) < 15 or result["max_craters"] != 1: errors.append(name + " missed or duplicated the crater")
            if active and active[0]["grounded"] != 1: errors.append(name + " created the crater before local ground contact")
            if result["final_craters"] or result["final_pose"] >= 0: errors.append(name + " leaked the crater or cast pose")
    return {"ok": not errors, "errors": errors, "measurements": measurements}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("exe", type=Path)
    parser.add_argument("--case", choices=["ignite", "supernova"], required=True)
    parser.add_argument("--delay", type=float, default=0)
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("--hold-charge", action="store_true")
    parser.add_argument("--reconnect", action="store_true")
    parser.add_argument("--rejoin-seat", choices=["owner", "observer"], default="observer")
    args = parser.parse_args()
    if args.hold_charge and args.case != "ignite": parser.error("Held charge uses the ignite case")
    if args.reconnect and not args.hold_charge: parser.error("Reconnect qualification requires a held charge")
    folder = args.out.resolve(); folder.mkdir(parents=True, exist_ok=False)
    backups = []
    for name in ("seanhost", "seanowner", "seanobserver"):
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
        def peer(name, route, observe_existing=False):
            return launch([str(args.exe.resolve()), "-batchmode", "-screen-width", "640", "-screen-height", "360",
                           "-screen-fullscreen", "0", "-tp-framecap", "60", "-tp-autostart", "3", "-tp-profile", "sean" + name,
                           "-tp-seancase", args.case, "-tp-seantrace", str(folder / (name + ".csv")),
                           "-logFile", str(folder / (name + ".log"))] + (["-tp-holdcharge"] if args.hold_charge else [])
                          + (["-tp-sean-observe-existing"] if observe_existing else []) + route)
        host = peer("host", ["-tp-host", "8980"]); time.sleep(7)
        port = "8980"
        if args.delay:
            port = "8981"
            log = (folder / "link.log").open("w"); handles.append(log)
            proxy = subprocess.Popen([sys.executable, str(ROOT / "tools/net_link.py"), "--listen", port,
                                      "--to", "127.0.0.1:8980", "--delay", str(args.delay), "--seconds", "110"],
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
        observer = peer("observer", ["-tp-join", "127.0.0.1", "8980"])
        print("Tracing " + args.case + " from three actual players: " + str(folder), flush=True)
        deadline = time.monotonic() + 90
        rejoined = False
        while time.monotonic() < deadline and host.poll() is None:
            if args.reconnect and not rejoined:
                name = args.rejoin_seat
                try: active = any(row["charged"] for row in rows(folder / (name + ".csv")))
                except (OSError, ValueError, TypeError): active = False
                if active:
                    previous = owner if name == "owner" else observer
                    previous.terminate(); previous.wait(timeout=8)
                    (folder / (name + ".csv")).rename(folder / (name + "-before.csv"))
                    (folder / (name + ".log")).rename(folder / (name + "-before.log"))
                    returned = peer(name, ["-tp-join", "127.0.0.1", port if name == "owner" else "8980"], observe_existing=True)
                    if name == "owner": owner = returned
                    else: observer = returned
                    rejoined = True
                    print("Reconnecting the " + name + " during the held fire charge", flush=True)
            time.sleep(.25)
        time.sleep(.5)
        result = evaluate(folder, args.case, args.hold_charge, args.rejoin_seat if rejoined else None, args.delay)
        if args.reconnect and not rejoined:
            result["ok"] = False; result["errors"].append("The requested observer reconnect was not exercised")
        result["observer_reconnected"] = rejoined and args.rejoin_seat == "observer"
        result["owner_reconnected"] = rejoined and args.rejoin_seat == "owner"
        result["rejoined_seat"] = args.rejoin_seat if rejoined else None
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
