"""Check ice effects with three players, or Cheska/Sean thaw with two players."""
import argparse
import csv
import hashlib
import json
import math
import os
from pathlib import Path
import re
import shutil
import socket
import statistics
import subprocess
import sys
import time

from run_unity_guarded import profile_root

ROOT = Path(__file__).resolve().parents[1]


def read(path):
    with path.open(newline="") as handle:
        return [{key: float(value) for key, value in row.items()} for row in csv.DictReader(handle)]


def evaluate(folder, case, reconnected=False):
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
        live_after_join = {}
        if reconnected and name == "observer":
            joined_at = rows[0]["wallTime"]
            for count in ("sheets", "walls"):
                live_after_join[count] = sum(r[count] > 0 and r["wallTime"] >= joined_at for r in data["host"])
            result["host_live_samples_after_join"] = live_after_join
            if max(live_after_join.values()) < 5:
                errors.append("Rejoined observer did not reach a live host ice window")
        if case == "denied":
            if result["max_sheets"] or result["max_walls"]:
                errors.append(name + " created a refused ice effect")
        else:
            expected_sheet = 1 if not live_after_join or live_after_join["sheets"] >= 5 else 0
            if result["max_sheets"] != expected_sheet:
                errors.append(name + " missed or duplicated the accepted sheet")
            expected_wall = 1 if case == "both" else 0
            if live_after_join and live_after_join["walls"] < 5: expected_wall = 0
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


def evaluate_sean(folder):
    required = ("time", "wallTime", "local", "host", "cheska", "sean", "ultStarted",
                "phaseSeen", "phase", "phaseAge", "phaseDuration", "held", "scale",
                "stun", "trip", "rooted", "chilled", "canMove", "simulated", "parked",
                "moveX", "moveY", "seanX", "seanZ")
    errors, measured = [], {}
    try:
        data = {name: read(folder / (name + ".csv")) for name in ("host", "owner")}
    except (OSError, ValueError, TypeError) as exc:
        return {"ok": False, "errors": ["Missing or malformed Sean trace: " + str(exc)], "measurements": {}}
    for name, rows in data.items():
        seat = 0 if name == "host" else 1
        invalid = len(rows) < 75 or any(set(required) - row.keys() or
                                        any(not math.isfinite(row[key]) for key in required) or
                                        row["local"] != seat or row["host"] != (1 if seat == 0 else 0) or
                                        row["cheska"] != 1 or row["sean"] != 1
                                        for row in rows)
        if invalid:
            errors.append(name + " lacks finite, continuous Cheska/Sean evidence from its required seat")
            continue
        if any(later["wallTime"] <= earlier["wallTime"] or
               later["time"] < earlier["time"] - .001
               for earlier, later in zip(rows, rows[1:])):
            errors.append(name + " has unordered wall/server timestamps")
    if errors:
        return {"ok": False, "errors": errors, "measurements": measured}
    host = data["host"]
    if (max(row["ultStarted"] for row in host) != 1 or host[-1]["ultStarted"] != 1 or
            not any(row["phase"] == 1 for row in host)):
        errors.append("Host did not accept and present exactly one Cheska ultimate")
    for name, rows in data.items():
        if not any(row["phaseSeen"] == 1 and row["phase"] == 1 for row in rows):
            errors.append(name + " never observed the shared ultimate phase")
    impact = next((row["wallTime"] for row in host if row["stun"] > .25), None)
    if impact is None:
        errors.append("Host never applied the ultimate's Frozen stun to Sean")
        return {"ok": False, "errors": errors, "measurements": measured}
    measured["impact_wall_time"] = impact
    for name, rows in data.items():
        critical = [row for row in rows if impact - .1 <= row["wallTime"] <= impact + 5.1]
        if (not critical or critical[0]["wallTime"] > impact + .1 or
                critical[-1]["wallTime"] < impact + 5.0 or
                any(later["wallTime"] - earlier["wallTime"] > .35
                    for earlier, later in zip(critical, critical[1:]))):
            errors.append(name + " has sparse evidence across the freeze/thaw window")
        observed = [row for row in rows if impact - .25 <= row["wallTime"] <= impact + 1.2
                    and row["stun"] > .25 and row["canMove"] == 0]
        frozen = [row for row in rows if impact + 1 <= row["wallTime"] <= impact + 1.8]
        thawed = [row for row in rows if impact + 3.2 <= row["wallTime"] <= impact + 4.8]
        baseline = [row for row in rows if impact + 2.8 <= row["wallTime"] <= impact + 3.1]
        late = [row for row in rows if impact + 4.2 <= row["wallTime"] <= impact + 5.1]
        if len(observed) < 3:
            errors.append(name + " did not observe Sean Frozen before thaw")
        if len(frozen) < 5 or sum(row["stun"] > .1 for row in frozen) < len(frozen) - 2:
            errors.append(name + " did not retain the intended early Frozen window")
        recovered = [row for row in thawed if row["stun"] <= .05 and row["trip"] <= .05 and
                     row["rooted"] <= .05 and row["canMove"] == 1 and row["held"] == 0 and
                     row["scale"] > .5 and row["phase"] == 0]
        if len(thawed) < 10 or len(recovered) < len(thawed) - 2:
            errors.append(name + " did not release Sean's movement gate after the 2.5 s freeze")
        displacement = math.dist((baseline[0]["seanX"], baseline[0]["seanZ"]),
                                 (late[-1]["seanX"], late[-1]["seanZ"])) if baseline and late else 0
        if displacement < .35:
            errors.append(name + " did not show Sean moving after thaw")
        measured[name] = {"samples": len(rows), "frozen_samples": len(observed),
                          "recovered_samples": len(recovered), "post_thaw_distance": displacement}
    owner = data["owner"]
    driven = [row for row in owner if impact + 3.9 <= row["wallTime"] <= impact + 4.8]
    if len(driven) < 10 or sum(row["simulated"] == 1 and row["parked"] == 0 and
                              row["moveY"] > .8 for row in driven) < len(driven) - 2:
        errors.append("Sean owner lacked a sustained, locally simulated forward intent after thaw")
    return {"ok": not errors, "errors": errors, "measurements": measured}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("exe", type=Path)
    parser.add_argument("--case", choices=["both", "denied", "sheet-then-denied", "cheska-sean"], required=True)
    parser.add_argument("--delay", type=float)
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("--reconnect", action="store_true")
    parser.add_argument("--lobby", action="store_true", help="Use normal lobby admission before starting the two-peer Sean case")
    args = parser.parse_args()
    sean_case = args.case == "cheska-sean"
    if args.lobby and not sean_case: parser.error("Lobby admission is supported for the two-peer Sean case")
    if args.delay is None: args.delay = 0 if sean_case else 150
    if args.delay < 0: parser.error("Delay cannot be negative")
    if args.reconnect and sean_case: parser.error("The Sean case uses exactly two peers")
    if args.reconnect and args.case != "both": parser.error("Reconnect qualification uses the both case")
    if not args.exe.is_file(): parser.error("Player executable does not exist")
    args.exe = args.exe.resolve()
    if sean_case and (ROOT / "Builds").resolve() not in args.exe.parents:
        parser.error("The Sean diagnostic requires a player executable inside this checkout's Builds")
    def free_port(exclude=()):
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as check:
            check.bind(("0.0.0.0", 0))
            port = check.getsockname()[1]
        return free_port(exclude) if port in exclude else port
    def check_port(port):
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as check:
            check.bind(("0.0.0.0", port))
    host_port = free_port() if sean_case else 9010
    proxy_port = free_port((host_port,)) if sean_case and args.delay else 9011
    try:
        check_port(host_port)
        if args.delay: check_port(proxy_port)
    except OSError as exc:
        parser.error("A required UDP port is already occupied: " + str(exc))
    folder = args.out.resolve(); folder.mkdir(parents=True, exist_ok=False)
    backups, processes, handles = [], [], []
    sean_prefix = "icesean-" + hashlib.sha256(str(folder).encode()).hexdigest()[:12]
    profiles = {name: (sean_prefix + "-" + name if sean_case else "ice" + name)
                for name in (("host", "owner") if sean_case else ("host", "owner", "observer"))}
    for name in profiles.values():
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
    overall_deadline = time.monotonic() + 100 if sean_case else None
    def wait_log(name, process, pattern, seconds):
        deadline = time.monotonic() + seconds
        if overall_deadline is not None: deadline = min(deadline, overall_deadline)
        while time.monotonic() < deadline:
            path = folder / (name + ".log")
            if path.exists() and re.search(pattern, path.read_text(errors="replace")): return
            if process.poll() is not None: raise RuntimeError(name + " exited during setup")
            time.sleep(.25)
        raise RuntimeError(name + " setup did not finish")
    def peer(name, route):
        return launch([str(args.exe.resolve()), "-batchmode", "-screen-width", "640", "-screen-height", "360",
                       "-screen-fullscreen", "0", "-tp-framecap", "60", "-tp-autostart", "2" if sean_case else "3",
                       "-tp-profile", profiles[name],
                       "-tp-icecase", args.case, "-tp-icetrace", str(folder / (name + ".csv")),
                       "-logFile", str(folder / (name + ".log"))] + route)
    try:
        host_route = ["-tp-lobby", "-tp-lobbyport", str(host_port)] if args.lobby else ["-tp-host", str(host_port)]
        host = peer("host", host_route)
        if args.lobby:
            wait_log("host", host, r"\[Net\] hosting on " + str(host_port), 45)
        else:
            wait_log("host", host, r"arena installed: LocalSlot=0[^\n]*host=True", 45)
        port = str(host_port)
        if args.delay:
            port = str(proxy_port); log = (folder / "link.log").open("w"); handles.append(log)
            proxy = subprocess.Popen([sys.executable, str(ROOT / "tools/net_link.py"), "--listen", port,
                                      "--to", "127.0.0.1:" + str(host_port), "--delay", str(args.delay), "--seconds", "110"],
                                     cwd=ROOT, stdout=log, stderr=subprocess.STDOUT, startupinfo=startup)
            processes.append(proxy); time.sleep(1)
        owner_route = ["-tp-lobbyjoin", "127.0.0.1:" + port,
                       "-tp-lobbyport", str(free_port((host_port, proxy_port)))] if args.lobby else ["-tp-join", "127.0.0.1", port]
        owner = peer("owner", owner_route)
        wait_log("owner", owner, r"(?:seat changed|arena installed): LocalSlot=1[^\n]*host=False", 35)
        if not sean_case:
            observer = peer("observer", ["-tp-join", "127.0.0.1", str(host_port)])
        print("Tracing " + args.case + " in " + str(folder), flush=True)
        deadline = overall_deadline if sean_case else time.monotonic() + 90
        rejoined = False
        while time.monotonic() < deadline and host.poll() is None:
            if args.reconnect and not rejoined:
                try: active = any(r["sheets"] > 0 for r in read(folder / "observer.csv"))
                except (OSError, ValueError, TypeError): active = False
                if active:
                    observer.terminate(); observer.wait(timeout=8)
                    (folder / "observer.csv").rename(folder / "observer-before.csv")
                    (folder / "observer.log").rename(folder / "observer-before.log")
                    observer = peer("observer", ["-tp-join", "127.0.0.1", str(host_port)])
                    rejoined = True
                    print("Reconnecting the same observer profile during active ice", flush=True)
            time.sleep(.25)
        time.sleep(.5)
        result = evaluate_sean(folder) if sean_case else evaluate(folder, args.case, rejoined)
        if sean_case and time.monotonic() >= overall_deadline and host.poll() is None:
            result["ok"] = False; result["errors"].append("Sean run exceeded its 100 s overall deadline")
        if args.reconnect and not rejoined:
            result["ok"] = False; result["errors"].append("The requested reconnect was never exercised")
        result["observer_reconnected"] = rejoined
        result["case"] = args.case; result["delay_one_way_ms"] = args.delay
        result["normal_lobby_admission"] = args.lobby
        if sean_case: result["host_udp_port"] = host_port
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
