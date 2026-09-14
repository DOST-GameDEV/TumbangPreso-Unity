"""Verify an older local player receives the current host's protocol refusal."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import time

from net_dante_matrix import ROOT
from run_unity_guarded import profile_root


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("host", type=Path)
    parser.add_argument("legacy", type=Path)
    parser.add_argument("--out", type=Path, required=True)
    args = parser.parse_args()
    folder = args.out.resolve(); folder.mkdir(parents=True, exist_ok=False)
    processes, backups = [], []
    startup = None
    if os.name == "nt":
        startup = subprocess.STARTUPINFO(); startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
        startup.wShowWindow = 0
    for name in ("protocolhost", "protocollegacy"):
        profile = profile_root(["-tp-profile", name])
        for source in profile.rglob("*"):
            if source.is_file() and source.suffix != ".log":
                backup = folder / "profiles" / name / source.relative_to(profile)
                backup.parent.mkdir(parents=True, exist_ok=True); shutil.copy2(source, backup)
                backups.append((source, backup, hashlib.sha256(source.read_bytes()).hexdigest()))

    def launch(exe, name, route):
        process = subprocess.Popen([str(exe.resolve()), "-batchmode", "-screen-width", "640",
            "-screen-height", "360", "-screen-fullscreen", "0", "-tp-profile", "protocol" + name,
            "-logFile", str(folder / (name + ".log"))] + route,
            cwd=ROOT, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, startupinfo=startup)
        processes.append(process)
        return process

    def output(name):
        path = folder / (name + ".log")
        return path.read_text(encoding="utf-8", errors="replace") if path.exists() else ""

    try:
        host = launch(args.host, "host", ["-tp-host", "8980"])
        deadline = time.monotonic() + 25
        while "hosting on 8980" not in output("host"):
            if host.poll() is not None or time.monotonic() > deadline:
                raise RuntimeError("Current host did not listen")
            time.sleep(.2)
        legacy = launch(args.legacy, "legacy", ["-tp-join", "127.0.0.1", "8980"])
        deadline = time.monotonic() + 25
        while "Game version mismatch" not in output("legacy"):
            if legacy.poll() is not None or time.monotonic() > deadline:
                raise RuntimeError("Legacy player did not receive the expected refusal")
            time.sleep(.2)
        host_log, old_log = output("host"), output("legacy")
        current = int(re.search(r"\| protocol (\d+) \|", host_log).group(1))
        old = int(re.search(r"\| protocol (\d+) \|", old_log).group(1))
        refused = f"Game version mismatch (network protocol {current})" in old_log
        result = {"ok": current != old and refused and "approved=False" in host_log,
                  "host_protocol": current, "legacy_protocol": old,
                  "legacy_received_refusal": refused}
        (folder / "result.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
        print(json.dumps(result, indent=2), flush=True)
        return 0 if result["ok"] else 1
    finally:
        for process in processes:
            if process.poll() is None: process.terminate()
        for process in processes:
            try: process.wait(timeout=8)
            except subprocess.TimeoutExpired: process.kill(); process.wait()
        for source, backup, expected in backups:
            shutil.copy2(backup, source)
            if hashlib.sha256(source.read_bytes()).hexdigest() != expected:
                raise RuntimeError("Named protocol profile was not restored")
        print(f"Preserved {len(backups)} existing named-profile files", flush=True)


if __name__ == "__main__":
    raise SystemExit(main())
