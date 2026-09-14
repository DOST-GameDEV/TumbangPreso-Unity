"""Check Phaister's real client cast, warning, victims and cleanup on three players."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import time

from net_dante_matrix import ROOT, rows
from run_unity_guarded import profile_root


def evaluate(folder, scenario):
    data = {peer: rows(folder / f"{peer}.csv") for peer in ("host", "owner", "observer")}
    errors, measured = [], {}
    host_active = [row for row in data["host"] if row["active"]]
    for peer, seat in (("host", 0), ("owner", 1), ("observer", 2)):
        records = data[peer]
        if len(records) < 100 or any(row["local"] != seat for row in records):
            errors.append(f"{peer}: wrong seat or insufficient trace")
            continue
        warnings = [row for row in records if row["windup"] > 0]
        active = [row for row in records if row["active"]]
        measured[peer] = {"samples": len(records), "warning_samples": len(warnings),
                          "active_samples": len(active), "maximum_circles": max(row["circles"] for row in records)}
        established = [row for row in records if row["elapsed"] > 10]
        if any(row.get("heroMode", 0) != 1 for row in established):
            errors.append(f"{peer}: fixture did not retain Hero Strike")
        if any(row.get("phaister", 0) != 1 or row.get("charIndex", -2) != row.get("roomPick", -1)
               for row in established[5:]):
            errors.append(f"{peer}: actual character and authoritative pick do not match Phaister's kit")
        if scenario == "rejoin" and peer == "observer":
            live = [row for row in records if host_active and
                    host_active[0]["time"] + .2 < row["time"] < host_active[-1]["time"] - .2]
            measured[peer]["rejoined_during_active_samples"] = len(live)
            if len(live) < 10:
                errors.append("observer: rejoin did not provide a usable live-ritual observation window")
            elif any(row["phaister"] != 1 for row in live):
                errors.append("observer: rejoin did not recover the caster's actual hero")
            else:
                # World, effect and sky are ordered reliable messages. Measure
                # bounded reconstruction after the first world sample, then
                # require uninterrupted state throughout the remaining window.
                ready = next((row for row in live if row["circles"] == 1 and row["active"] and row["sky"] == 1), None)
                delay = ready["time"] - live[0]["time"] if ready else None
                measured[peer]["reconstruction_seconds"] = delay
                if ready is None or delay > .25:
                    errors.append("observer: live ritual reconstruction exceeded 250 ms after world state")
                elif any(row["circles"] != 1 or not row["active"] or row["sky"] != 1
                         for row in live if row["time"] >= ready["time"]):
                    errors.append("observer: reconstructed ritual disappeared before its authoritative expiry")
                if ready is not None:
                    clock_errors = []
                    for row in live:
                        if row["time"] < ready["time"]: continue
                        authoritative = min(host_active, key=lambda value: abs(value["time"] - row["time"]))
                        if abs(authoritative["time"] - row["time"]) < .08:
                            clock_errors.append(abs(authoritative["remaining"] - row["remaining"]))
                    measured[peer]["remaining_clock_error"] = max(clock_errors) if clock_errors else None
                    if len(clock_errors) < 10 or max(clock_errors, default=1) > .25:
                        errors.append("observer: restored lifetime disagrees with the authoritative clock")
            if records[-1]["circles"] or records[-1]["active"] or records[-1]["sky"]:
                errors.append("observer: restored presentation leaked after expiry")
            continue
        if scenario == "rejected":
            if peer != "owner" and (warnings or active or any(row["circles"] for row in records)):
                errors.append(f"{peer}: the host's empty meter allowed a real ritual")
            if peer == "owner":
                if not warnings:
                    errors.append("owner: no predicted cast to reject")
                if any(row["active"] or row["circles"] or row["sky"] for row in records if 15 < row["elapsed"] < 20):
                    errors.append("owner: rejected cast left ritual, active clock or sky alive")
                measured[peer]["late_sky_samples"] = sum(row["sky"] > 0 for row in records if 15 < row["elapsed"] < 20)
            if any(row["frontStun"] > .02 for row in records):
                errors.append(f"{peer}: a rejected ritual cursed a rival")
            continue
        if len(warnings) < 12 or not active:
            errors.append(f"{peer}: missing preparation or active phase")
            continue
        # NGO's adjusted server time is useful across peers, but its correction
        # must not be mistaken for the duration of a local Unity animation clock.
        warning_start = warnings[0]["gameTime"]
        first_active = active[0]["gameTime"]
        delay = first_active - warning_start
        measured[peer]["warning_to_active"] = delay
        # The first CSV sample can be a long frame after acceptance. Use the
        # system's actual accepted-cast age for the mechanical 1.55-second delay;
        # keep first-sample duration as a separate presentation measurement.
        accepted_delay = active[0].get("castAge", -1)
        measured[peer]["accepted_cast_to_active"] = accepted_delay
        if not 1.50 <= accepted_delay < 1.85:
            errors.append(f"{peer}: active phase does not follow the accepted cast's 1.55-second preparation")
        sustained = [row for row in records if warning_start + .25 < row["gameTime"] < first_active + 6.6]
        if any(row["circles"] != 1 for row in sustained):
            errors.append(f"{peer}: duplicated or missing owned ritual")
        if any(abs(row["centreX"]) > .2 or abs(row["centreZ"] + 8) > .2 for row in sustained):
            errors.append(f"{peer}: ritual centre disagrees with accepted cast")
        if any((row["rearX"] ** 2 + (row["rearZ"] + 8) ** 2) ** .5 < 11 for row in sustained):
            errors.append(f"{peer}: fixture's outside target entered the curse footprint")
        sky_phase = [row for row in active if first_active + .6 < row["gameTime"] < first_active + 6]
        measured[peer]["accepted_sky_samples"] = sum(row["sky"] == 1 for row in sky_phase)
        if len(sky_phase) < 40 or any(row["sky"] != 1 for row in sky_phase):
            errors.append(f"{peer}: accepted ritual failed to sustain its eclipse sky")
        hits = [row for row in records if row["frontStun"] > .02]
        if not hits:
            errors.append(f"{peer}: no real curse reached the nearby opponent")
        else:
            measured[peer]["first_curse_after_warning"] = hits[0]["gameTime"] - warning_start
        if any(row["rearStun"] > .02 or row["casterStun"] > .02 for row in records):
            errors.append(f"{peer}: curse hit its caster or a player outside the circle")
        if records[-1]["circles"] or records[-1]["active"] or records[-1]["sky"]:
            errors.append(f"{peer}: presentation or ability leaked past expiry")
    if scenario != "rejected" and data["host"]:
        host = data["host"]
        warning = next((row for row in host if row["windup"] > 0), None)
        hit = next((row for row in host if row["frontStun"] > .02), None)
        if warning is None or hit is None or hit.get("castAge", -1) < 1.50:
            errors.append("host: curse contact preceded the accepted preparation")
    return {"ok": not errors, "errors": errors, "measurements": measured}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("exe", type=Path)
    parser.add_argument("--case", choices=("coven", "rejected", "rejoin"), required=True)
    parser.add_argument("--delay", type=float, default=150)
    parser.add_argument("--out", type=Path, required=True)
    args = parser.parse_args()
    folder = args.out.resolve()
    folder.mkdir(parents=True, exist_ok=False)
    backups, processes, handles = [], [], []
    startup = None
    if os.name == "nt":
        startup = subprocess.STARTUPINFO()
        startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
        startup.wShowWindow = 0
    for peer in ("host", "owner", "observer"):
        profile = profile_root(["-tp-profile", "phaister" + peer])
        for source in profile.rglob("*"):
            if not source.is_file() or source.suffix == ".log":
                continue
            backup = folder / "profiles" / peer / source.relative_to(profile)
            backup.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source, backup)
            backups.append((source, backup, hashlib.sha256(source.read_bytes()).hexdigest()))

    def launch(command, **kwargs):
        process = subprocess.Popen(command, cwd=ROOT, startupinfo=startup,
                                   stderr=subprocess.STDOUT, **kwargs)
        processes.append(process)
        return process

    def peer(name, route, label=None):
        label = label or name
        return launch([str(args.exe.resolve()), "-batchmode", "-screen-width", "640",
                       "-screen-height", "360", "-screen-fullscreen", "0", "-tp-autostart", "3",
                       "-tp-profile", "phaister" + name, "-tp-phaistercase", args.case,
                       "-tp-phaistertrace", str(folder / f"{label}.csv"),
                       "-logFile", str(folder / f"{label}.log")] + route, stdout=subprocess.DEVNULL)

    try:
        host = peer("host", ["-tp-host", "8970"])
        time.sleep(7)
        link = (folder / "link.log").open("w", encoding="utf-8")
        handles.append(link)
        launch([sys.executable, str(ROOT / "tools/net_link.py"), "--listen", "8971", "--to",
                "127.0.0.1:8970", "--delay", str(args.delay), "--seconds", "120"], stdout=link)
        time.sleep(1)
        owner = peer("owner", ["-tp-join", "127.0.0.1", "8971"])
        deadline = time.monotonic() + 35
        while time.monotonic() < deadline:
            path = folder / "owner.log"
            output = path.read_text(encoding="utf-8", errors="replace") if path.exists() else ""
            if re.search(r"(?:seat changed|arena installed): LocalSlot=1[^\n]*host=False", output):
                break
            if owner.poll() is not None:
                raise RuntimeError("Owner exited before taking seat one")
            time.sleep(.25)
        else:
            raise RuntimeError("Owner never took seat one")
        observer = peer("observer", ["-tp-join", "127.0.0.1", "8970"],
                        "observer-before" if args.case == "rejoin" else None)
        print(f"Tracing {args.case} from three actual players: {folder}", flush=True)
        deadline = time.monotonic() + 90
        rejoined = False
        while time.monotonic() < deadline and any(p.poll() is None for p in (host, owner, observer)):
            if args.case == "rejoin" and not rejoined:
                try:
                    ritual_live = any(row["active"] for row in rows(folder / "host.csv"))
                except (OSError, ValueError, TypeError):
                    ritual_live = False
                if ritual_live:
                    observer.terminate()
                    observer.wait(timeout=8)
                    observer = peer("observer", ["-tp-join", "127.0.0.1", "8970",
                                                 "-tp-phaister-observe-existing"])
                    rejoined = True
                    print("Rejoining the observer's same profile during the actual ritual", flush=True)
            time.sleep(.5)
        if any(p.poll() is None for p in (host, owner, observer)):
            raise RuntimeError("Players did not finish their bounded trace")
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
                process.kill()
                process.wait()
        for handle in handles:
            handle.close()
        for source, backup, expected in backups:
            shutil.copy2(backup, source)
            if hashlib.sha256(source.read_bytes()).hexdigest() != expected:
                raise RuntimeError("Named test profile restoration failed")
        print(f"Preserved {len(backups)} existing named-profile files", flush=True)


if __name__ == "__main__":
    raise SystemExit(main())
