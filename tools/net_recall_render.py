"""Observe recall-beam ownership on three playing peers and a real spectator."""
import argparse
import hashlib
import json
import math
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
    parser.add_argument("--mode", choices=["classic", "hero"], default="classic")
    args = parser.parse_args()
    exe, out = args.exe.resolve(), args.out.resolve()
    if not exe.is_file() or not exe.is_relative_to(ROOT / "Builds"):
        raise SystemExit("Use an internal Builds player.")
    if not out.is_relative_to(ROOT / "Logs") or out == ROOT / "Logs":
        raise SystemExit("Use a fresh evidence directory inside Logs.")
    out.mkdir(parents=True, exist_ok=False)
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as test:
        test.bind(("127.0.0.1", 0))
        port = test.getsockname()[1]
    before = read_input_preferences()
    # Direct arena boot marks the lobby in-progress before READY; seat switching
    # is correctly forbidden even before the first round. The actual fifth join
    # is admitted as spectator after all four playing seats are occupied.
    peers = [("host", 0), ("owner", 1), ("other", 2), ("fourth", 3), ("spectator", -1)]
    processes, commands, backups = [], [], []
    errors, measured = [], {}
    startup = subprocess.STARTUPINFO()
    startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
    startup.wShowWindow = 0
    try:
        for index, (name, seat) in enumerate(peers):
            profile_name = out.name + "-" + name
            profile = profile_root(["-tp-profile", profile_name])
            for source in profile.rglob("*"):
                if source.is_file() and source.suffix != ".log":
                    backup = out / "private-profiles" / name / source.relative_to(profile)
                    backup.parent.mkdir(parents=True, exist_ok=True)
                    shutil.copy2(source, backup)
                    backups.append((source, backup, hashlib.sha256(source.read_bytes()).hexdigest()))
            command = [str(exe), "-batchmode", "-screen-width", "960", "-screen-height", "540",
                       "-screen-fullscreen", "0", "-tp-framecap", "30", "-tp-autostart", "3",
                       "-tp-map", "Eskinita", "-tp-profile", profile_name,
                       "-tp-recall-seat", str(seat), "-tp-recall-mode", args.mode,
                       "-tp-recall-render", str(out / (name + ".json")), "-logFile", str(out / (name + ".log"))]
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
        deadline = time.monotonic() + 85
        while time.monotonic() < deadline:
            for name, _ in peers:
                path = out / (name + ".json")
                if path.exists() and name not in measured:
                    try:
                        measured[name] = json.loads(path.read_text())
                    except json.JSONDecodeError:
                        pass  # Writer may still be completing the receipt.
            if len(measured) == len(peers):
                break
            if any(p.poll() is not None for p in processes):
                errors.append("A peer exited before every render receipt arrived.")
                break
            time.sleep(.25)
    finally:
        for process in processes:
            if process.poll() is None:
                process.terminate()
        for process in processes:
            try:
                process.wait(timeout=15)
            except subprocess.TimeoutExpired:
                process.kill()
                process.wait()
        for source, backup, digest in backups:
            source.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(backup, source)
            if hashlib.sha256(source.read_bytes()).hexdigest() != digest:
                raise RuntimeError("Named profile restore failed.")
    for name, seat in peers:
        row = measured.get(name)
        if row is None:
            errors.append(name + ": missing actual render receipt")
            continue
        expected = [seat == 1, seat == 2, seat == 3]
        if not row.get("passed") or row.get("local") != seat or row.get("drawing") != expected:
            errors.append(name + ": incorrect ownership/render: " + str(row.get("error")))
        if row.get("spectator") != (seat < 0):
            errors.append(name + ": wrong spectator assignment")
        if row.get("owners") != [1, 2, 3]:
            errors.append(name + ": different staged equipment")
        if row.get("mode") != ("HeroStrike" if args.mode == "hero" else "Classic"):
            errors.append(name + ": actor was installed in the wrong mode")
        for suffix in ["-beam-on.png", "-beam-off.png"]:
            image = out / (name + suffix)
            if not image.exists() or image.stat().st_size < 1000:
                errors.append(name + ": absent rendered image")
    epochs = {r.get("epoch") for r in measured.values()}
    if len(epochs) != 1 or any(not isinstance(epoch, int) or epoch <= 0 for epoch in epochs):
        errors.append("Peers did not report one actual host match identity.")
    host_positions = measured.get("host", {}).get("positions", [])
    for name, row in measured.items():
        positions = row.get("positions", [])
        if len(positions) != 9 or len(host_positions) != 9 or any(not math.isfinite(a) or not math.isfinite(b) or abs(a - b) > .15 for a, b in zip(positions, host_positions)):
            errors.append(name + ": loose shoe positions did not converge to the host")
    unchanged = before == read_input_preferences()
    if not unchanged:
        errors.append("Shared input preferences changed.")
    runtime = exe.parent / (exe.stem + "_Data") / "Managed/TumbangPreso.Runtime.dll"
    result = dict(passed=not errors, errors=errors, measured=measured, sharedInputUnchanged=unchanged,
                  existingFilesRestored=len(backups), runtimeSha256=hashlib.sha256(runtime.read_bytes()).hexdigest(),
                  scope="Four playing Windows peers plus an actual fifth-join spectator, transport/ownership and standardized world-camera A/B. Not physical/WAN or human visual approval.")
    (out / "result.json").write_text(json.dumps(result, indent=2))
    print(json.dumps(dict(passed=result["passed"], errors=errors, peers=list(measured))), flush=True)
    return 0 if result["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
