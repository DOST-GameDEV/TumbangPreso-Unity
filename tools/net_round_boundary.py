"""Qualify a rejoining client across a real host round boundary in two native players."""
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

from run_unity_guarded import profile_root, unity_environment
from run_ui_player_review import read_input_preferences

ROOT = Path(__file__).resolve().parents[1]


def evaluate(folder):
    traces = {}
    errors = []
    measures = {}
    for name, seat in (("host", 0), ("client", 1)):
        with (folder / (name + ".csv")).open(newline="") as source:
            traces[name] = [{k: float(v) for k, v in row.items()} for row in csv.DictReader(source)]
        rows = traces[name]
        live = [r for r in rows if r["networked"] and r["inProgress"] and r["round"] > 0]
        buffer = [r for r in live if r["round"] == 1 and not r["active"] and r["warmup"]]
        next_round = [r for r in live if r["round"] == 2 and r["active"] and not r["warmup"]]
        invalid = [r for r in live if r["active"] and r["warmup"]]
        if len(buffer) < 5 or len(next_round) < 15:
            errors.append(name + " lacks the real intermission or sustained second live round")
        if invalid:
            errors.append(name + " reports warm-up during live gameplay")
        if not live or any(r["slot"] != seat for r in live):
            errors.append(name + " did not retain its expected seat")
        events = max((r["intermissions"] for r in rows), default=-1)
        if events != (1 if name == "host" else 0):
            errors.append(name + " fired the wrong number of authoritative intermission events")
        if name == "client":
            if not any(r["cycle"] == 0 and r["active"] for r in live) or not any(r["cycle"] == 2 for r in next_round):
                errors.append("Client did not rejoin and continue")
            if len({r["pid"] for r in rows}) != 1:
                errors.append("Client did not stay in the same native process")
        log = (folder / (name + ".log")).read_text(errors="replace")
        if re.search(r"(?:Exception:|\[RoundBoundary\] timed out|error CS\d)", log):
            errors.append(name + " has a runtime error")
        measures[name] = dict(samples=len(rows), buffer_samples=len(buffer), round2_samples=len(next_round),
                              invalid_warmup_samples=len(invalid), intermission_events=events)
    return dict(passed=not errors, errors=errors, measurements=measures)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--exe", type=Path, required=True)
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("--mode", choices=("classic", "hero"), required=True)
    args = parser.parse_args()
    exe, folder = args.exe.resolve(), args.out.resolve()
    if not exe.is_file() or not exe.is_relative_to(ROOT / "Builds"):
        raise SystemExit("Use an internal Builds player")
    if not folder.is_relative_to(ROOT / "Logs"):
        raise SystemExit("Output must be inside Logs")
    folder.mkdir(parents=True, exist_ok=False)
    processes, backups, handles = [], [], []
    before = read_input_preferences()
    startup = subprocess.STARTUPINFO()
    startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
    startup.wShowWindow = 0

    def launch(command, stdout=subprocess.DEVNULL):
        process = subprocess.Popen(command, cwd=ROOT, env=unity_environment(), startupinfo=startup,
                                   stdout=stdout, stderr=subprocess.STDOUT)
        processes.append(process)
        return process

    def peer(name, route):
        profile_name = "round-boundary-" + args.mode + "-" + name
        profile = profile_root(["-tp-profile", profile_name])
        for source in profile.rglob("*"):
            if source.is_file():
                target = folder / "private-profile-backup" / name / source.relative_to(profile)
                target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(source, target)
                backups.append((source, target, hashlib.sha256(source.read_bytes()).hexdigest()))
        return launch([str(exe), "-batchmode", "-screen-fullscreen", "0", "-screen-width", "640",
                       "-screen-height", "360", "-tp-framecap", "60", "-tp-autostart", "2", "-tp-autorematch",
                       "-tp-review-mode", args.mode, "-tp-review-rounds", "2", "-tp-review-seconds", "30",
                       "-tp-profile", profile_name, "-tp-roundtrace", str(folder / (name + ".csv")),
                       "-tp-round-rejoin-port", "8965", "-logFile", str(folder / (name + ".log"))] + route)

    result = dict(passed=False, errors=["Run did not complete"])
    try:
        host = peer("host", ["-tp-host", "8964"])
        deadline = time.monotonic() + 45
        while time.monotonic() < deadline:
            log_path = folder / "host.log"
            if log_path.exists() and re.search(r"(?:seat changed|arena installed): LocalSlot=0", log_path.read_text(errors="replace")):
                break
            if host.poll() is not None:
                raise RuntimeError("Host exited before readiness")
            time.sleep(.25)
        else:
            raise RuntimeError("Host never reached the arena")
        link_log = (folder / "link.log").open("w")
        handles.append(link_log)
        launch([sys.executable, str(ROOT / "tools/net_link.py"), "--listen", "8965", "--to", "127.0.0.1:8964",
                "--delay", "150", "--seconds", "170"], link_log)
        time.sleep(.5)
        client = peer("client", ["-tp-join", "127.0.0.1", "8965"])
        print("Tracing " + args.mode + " host and rejoining client", flush=True)
        host_code = host.wait(timeout=160)
        client_code = client.wait(timeout=20)
        result = evaluate(folder)
        if host_code or client_code:
            result["errors"].append("A player exited unsuccessfully")
            result["passed"] = False
    except Exception as error:
        result = dict(passed=False, errors=[str(error)])
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
                raise RuntimeError("Named profile restore failed")
        result["shared_input_unchanged"] = before == read_input_preferences()
        if not result["shared_input_unchanged"]:
            result["passed"] = False
            result["errors"].append("Shared input preferences changed")
        runtime = exe.parent / (exe.stem + "_Data") / "Managed/TumbangPreso.Runtime.dll"
        result["runtime_sha256"] = hashlib.sha256(runtime.read_bytes()).hexdigest()
        result["mode"] = args.mode
        result["one_way_delay_ms"] = 150
        result["retired_pids"] = [p.pid for p in processes]
        (folder / "result.json").write_text(json.dumps(result, indent=2) + "\n")
        print(json.dumps(result, indent=2), flush=True)
    return 0 if result["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
