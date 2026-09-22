"""Real player/observer/spectator notices for an orderly leave versus an abrupt process loss."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import socket
import subprocess
import time

from run_unity_guarded import profile_root, unity_environment
from run_ui_player_review import read_input_preferences

ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("exe", type=Path)
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("--case", choices=["leave", "drop"], required=True)
    parser.add_argument("--mode", choices=["classic", "hero"], default="classic")
    args = parser.parse_args()
    exe, out = args.exe.resolve(), args.out.resolve()
    if not exe.is_file() or not exe.is_relative_to(ROOT / "Builds"):
        raise SystemExit("Use an internal Builds player.")
    if not out.is_relative_to(ROOT / "Logs") or out == ROOT / "Logs":
        raise SystemExit("Use a fresh scoped Logs directory.")
    out.mkdir(parents=True, exist_ok=False)
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as sock:
        sock.bind(("127.0.0.1", 0)); port = sock.getsockname()[1]
    peers = [("host", 0), ("leaver", 1), ("other", 2), ("fourth", 3), ("spectator", -1)]
    processes, commands, backups = [], [], []
    measured, errors = {}, []
    before = read_input_preferences()
    startup = subprocess.STARTUPINFO()
    startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW; startup.wShowWindow = 0
    try:
        for index, (name, seat) in enumerate(peers):
            profile_name = out.name + "-" + name
            profile = profile_root(["-tp-profile", profile_name])
            for source in profile.rglob("*"):
                if source.is_file() and source.suffix != ".log":
                    backup = out / "private-profiles" / name / source.relative_to(profile)
                    backup.parent.mkdir(parents=True, exist_ok=True); shutil.copy2(source, backup)
                    backups.append((source, backup, hashlib.sha256(source.read_bytes()).hexdigest()))
            command = [str(exe), "-batchmode", "-screen-width", "960", "-screen-height", "540",
                       "-screen-fullscreen", "0", "-tp-framecap", "30", "-tp-autostart", "3",
                       "-tp-map", "Eskinita", "-tp-profile", profile_name,
                       "-tp-departure-seat", str(seat), "-tp-review-mode", args.mode,
                       "-tp-departure-review", str(out / (name + ".json")),
                       "-logFile", str(out / (name + ".log"))]
            command += ["-tp-host", str(port)] if index == 0 else ["-tp-join", "127.0.0.1", str(port)]
            commands.append(command)
            processes.append(subprocess.Popen(command, cwd=ROOT, env=unity_environment(), startupinfo=startup,
                                               stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL))
            print(name, "PID", processes[-1].pid, flush=True)
            (out / "job.json").write_text(json.dumps(dict(exe=str(exe), port=port, commands=commands,
                                                           pids=[p.pid for p in processes]), indent=2))
            if index == 0:
                time.sleep(7)
            elif index < len(peers) - 1:
                deadline = time.monotonic() + 18
                while time.monotonic() < deadline:
                    log = out / (name + ".log")
                    if log.exists() and f"LocalSlot={seat}" in log.read_text(errors="replace"):
                        break
                    time.sleep(.25)
        deadline = time.monotonic() + 70
        while not all((out / (name + ".json.ready")).exists() for name, _ in peers):
            if time.monotonic() > deadline or any(p.poll() is not None for p in processes):
                raise RuntimeError("All five actual peers did not become ready.")
            time.sleep(.25)
        epochs = {int((out / (name + ".json.ready")).read_text()) for name, _ in peers}
        if len(epochs) != 1 or min(epochs) <= 0:
            raise RuntimeError("Readiness was not for one actual match identity.")
        if args.case == "leave":
            (out / "leaver.json.leave").write_text("Leave through NetSession.Stop")
        else:
            processes[1].kill()  # Only the exact test-owned departing process.
            processes[1].wait(timeout=15)
        wanted = [name for name, _ in peers if name != "leaver"]
        deadline = time.monotonic() + 30
        while time.monotonic() < deadline and len(measured) < len(wanted):
            for name in wanted:
                path = out / (name + ".json")
                if path.exists() and name not in measured:
                    try: measured[name] = json.loads(path.read_text())
                    except json.JSONDecodeError: pass
            time.sleep(.2)
        expected_word = " LEFT" if args.case == "leave" else " DISCONNECTED"
        for name in wanted:
            row = measured.get(name, {})
            if not row.get("passed") or row.get("notices") != 1 or expected_word not in row.get("text", ""):
                errors.append(name + ": missing or wrong departure notice: " + str(row))
            if row.get("match") not in epochs: errors.append(name + ": wrong match identity")
            if row.get("mode") != ("HeroStrike" if args.mode == "hero" else "Classic"):
                errors.append(name + ": wrong game mode")
            picture = out / (name + ".json.png")
            if not picture.exists() or picture.stat().st_size < 1000:
                errors.append(name + ": actual HUD capture missing")
        if len({row.get("text") for row in measured.values()}) != 1:
            errors.append("Remaining peers disagree on the authoritative notice.")
        if args.case == "leave" and not (out / "leaver.json.left").exists():
            errors.append("The real orderly Stop path was not observed.")
    except Exception as error:
        errors.append(str(error))
    finally:
        for process in processes:
            if process.poll() is None: process.terminate()
        for process in processes:
            try: process.wait(timeout=15)
            except subprocess.TimeoutExpired:
                process.kill(); process.wait()
        for source, backup, digest in backups:
            source.parent.mkdir(parents=True, exist_ok=True); shutil.copy2(backup, source)
            if hashlib.sha256(source.read_bytes()).hexdigest() != digest:
                errors.append("Named profile restore failed: " + str(source))
    unchanged = before == read_input_preferences()
    if not unchanged: errors.append("Shared input preferences changed.")
    runtime = exe.parent / (exe.stem + "_Data") / "Managed/TumbangPreso.Runtime.dll"
    result = dict(passed=not errors, case=args.case, mode=args.mode, errors=errors, measured=measured,
                  sharedInputUnchanged=unchanged, existingFilesRestored=len(backups),
                  runtimeSha256=hashlib.sha256(runtime.read_bytes()).hexdigest(),
                  scope="Five actual local peers including a fifth-join spectator; one orderly leave or killed test process. Not WAN or human playtesting.")
    (out / "result.json").write_text(json.dumps(result, indent=2))
    print(json.dumps(dict(passed=result["passed"], errors=errors)), flush=True)
    return 0 if result["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
