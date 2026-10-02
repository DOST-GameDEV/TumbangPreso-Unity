"""One bounded actual-peer custom completed-match cold arrival; no force finish or SDK."""
import argparse
import hashlib
import json
import os
import re
from pathlib import Path
import shutil
import socket
import subprocess
import sys
import time
import uuid
import winreg

import playerprefs_guard
import run_unity_guarded as guard
import run_unity_job as jobs
from run_ui_player_review import read_input_preferences, PLAYER_KEY

ROOT = Path(__file__).resolve().parents[1]
WIRE = "1|0|1|30|0|3|0|1|0|1"


def observed_renderer(log):
    match = re.search(r"Version:\s*Direct3D (11|12)\b", log)
    return "Direct3D" + match.group(1) if match else None


def read(path):
    try:
        return json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, ValueError):
        return None


def validate_rules(core):
    # The existing engine-free parser validates the profile wire before any player launch.
    shell = shutil.which("pwsh")
    if not shell:
        raise RuntimeError("PowerShell 7 is required for the installed engine-free rule parser")
    script = """$ErrorActionPreference='Stop'
$assembly=[Reflection.Assembly]::LoadFrom($env:TUMP_COMPLETED_CORE)
$type=$assembly.GetType('TumbangPreso.Core.CustomGameRules')
$mode=[Enum]::Parse($assembly.GetType('TumbangPreso.Core.GameMode'),'HeroStrike')
$rules=$type.GetMethod('Parse').Invoke($null,@($env:TUMP_COMPLETED_WIRE,$mode))
@{wire=$type.GetMethod('ToWire').Invoke($null,@($rules));rounds=$rules.Rounds;seconds=$rules.RoundSeconds;manual=$rules.ManualReady} | ConvertTo-Json -Compress
"""
    result = subprocess.run([shell, "-NoProfile", "-Command", script], capture_output=True, text=True,
                            env=dict(os.environ, TUMP_COMPLETED_CORE=str(core), TUMP_COMPLETED_WIRE=WIRE), check=True)
    rules = json.loads(result.stdout)
    if rules != {"wire": WIRE, "rounds": 1, "seconds": 30, "manual": True}:
        raise RuntimeError("Installed custom-rule parser rejected the scenario")
    return rules


def restore_input(before):
    def write(name, entry):
        import base64
        value = base64.b64decode(entry["bytes"]) if "bytes" in entry else entry["value"]
        with winreg.CreateKeyEx(winreg.HKEY_CURRENT_USER, PLAYER_KEY, 0, winreg.KEY_SET_VALUE) as key:
            winreg.SetValueEx(key, name, 0, entry["kind"], value)
    def delete(name):
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER, PLAYER_KEY, 0, winreg.KEY_SET_VALUE) as key:
            winreg.DeleteValue(key, name)
    playerprefs_guard.restore_values(before, read_input_preferences, write, delete)


def evaluate(host, client):
    errors = []
    if not host or not client:
        return ["Missing actual-peer receipts"]
    for role, receipt in (("host", host), ("client", client)):
        if not receipt.get("passed") or not receipt.get("sawLive") or not receipt.get("originHumanSeats") or not receipt.get("recordHumanOrigins"):
            errors.append(role + " did not prove a naturally completed two-human-origin match")
        if receipt.get("rounds") != 1 or receipt.get("roundSeconds") != 30:
            errors.append(role + " did not retain supported custom 1/30 rules")
    if not host.get("hostStayedEnded") or not client.get("admitted"):
        errors.append("Host restarted or client admission did not complete")
    if not host.get("initialRecordId") or host.get("recordId") != client.get("recordId") or host.get("scores") != client.get("scores"):
        errors.append("Actual terminal record identity/scores differ")
    if client.get("sceneBefore") == client.get("sceneAfter") or client.get("matchEndedEvents", 0) <= client.get("beforeEndEvents", 0) or client.get("recordReadyEvents", 0) <= client.get("beforeRecordEvents", 0):
        errors.append("Client reused old scene/event/record state rather than recovering after cold arrival")
    if not client.get("coldActorsFrozen") or client.get("actorRoundActive") != [False] * 4 or client.get("actorParked") != [True] * 4 or client.get("actorSprint") != [False] * 4 or client.get("actorMoveSquared") != [0] * 4:
        errors.append("Cold-arrival actors were not all present, frozen and released from gameplay input")
    return errors


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, default=ROOT)
    parser.add_argument("--exe", type=Path, required=True)
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("--port", type=int, default=9080)
    parser.add_argument("--source-commit", required=True)
    parser.add_argument("--runtime-sha", required=True)
    parser.add_argument("--build-receipt", type=Path, required=True)
    parser.add_argument("--chat", help="Optional local-peer UTF-16 chat marker to verify both receive callbacks")
    parser.add_argument("--graphics-api", choices=("d3d11", "default"), default="d3d11",
                        help="default leaves the packaged renderer order in charge")
    args = parser.parse_args()
    project, exe, folder = args.project.resolve(), args.exe.resolve(), args.out.resolve()
    if not exe.is_file() or not exe.is_relative_to(project / "Builds") or not folder.is_relative_to(project / "Logs"):
        parser.error("Use this project's internal Builds player and dedicated Logs output")
    if guard.project_identity(project) != ("BH Studios", "Tumbang Preso") or not 1024 <= args.port < 65535:
        parser.error("Use the frozen shipping identity and two nonprivileged ports")
    runtime = exe.parent / (exe.stem + "_Data/Managed/TumbangPreso.Runtime.dll")
    core = runtime.with_name("TumbangPreso.Core.dll")
    before_hash = hashlib.sha256(runtime.read_bytes()).hexdigest()
    build = read(args.build_receipt.resolve())
    if (not build or build.get("sourceCommit") != args.source_commit or build.get("runtimeSha256", "").lower() != args.runtime_sha.lower()
            or Path(build.get("artifact", "")).resolve() != exe or before_hash != args.runtime_sha.lower()):
        parser.error("Frozen source/build receipt, executable and Runtime hash do not match")
    if b"NetCompletedArrivalProbe" not in runtime.read_bytes():
        parser.error("This artifact lacks the opt-in probe; build the frozen qualified candidate first")
    rules = validate_rules(core)
    folder.mkdir(parents=True, exist_ok=False)
    for port in (args.port, args.port + 1):
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as sock:
            sock.bind(("127.0.0.1", port))
    key = uuid.uuid4().hex[:10]
    profile_base = guard.player_profile()
    profiles = {}
    seeds = {}
    claim = jobs.make_claim(project, "gpu", 2048, 2048, "completed-arrival-" + key,
                            [args.port, args.port + 1], ["-batchmode"])
    acquired = False
    before = None
    children = []
    result = {"passed": False, "errors": ["Scenario did not complete"], "profiles": profiles, "rules": rules,
              "runtimeSha256": before_hash, "sourceCommit": args.source_commit, "buildReceipt": str(args.build_receipt.resolve()),
              "graphicsApiRequested": args.graphics_api,
              "scope": "Short custom 1-round/30-second direct-peer cold completed arrival; MAIN MENU gives up the prior seat, so no retained-seat/full-default-match/physical-input claim."}
    if args.chat:
        result["chatRequested"] = args.chat

    def launch(role):
        route = ["-tp-lobby", "-tp-lobbyport", str(args.port)] if role == "host" else [
            "-tp-lobbyjoin", "127.0.0.1:" + str(args.port), "-tp-lobbyport", str(args.port + 1)]
        graphics = ["-force-d3d11"] if args.graphics_api == "d3d11" else []
        command = [str(exe), "-batchmode", *graphics, "-screen-fullscreen", "0", "-screen-width", "640", "-screen-height", "360",
                   "-tp-framecap", "60", "-tp-profile", profiles[role]["name"], "-tp-autostart", "2",
                   "-tp-completed-arrival", str(folder), "-tp-completed-role", role, "-tp-completed-port", str(args.port),
                   "-logFile", str(folder / (role + ".log")), *route]
        if args.chat:
            command += ["-tp-lobbychat", args.chat + " " + role]
        startup = subprocess.STARTUPINFO(); startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW; startup.wShowWindow = 0
        child = subprocess.Popen(command, cwd=project, env=guard.unity_environment(), startupinfo=startup)
        children.append(child)
        result[role + "Pid"] = child.pid
        result[role + "Command"] = command
        return child

    try:
        result["admission"] = jobs.acquire(jobs.POOL, claim, 300)
        acquired = True
        for role in ("host", "client"):
            name = "completed-arrival-" + role + "-" + key
            path = profile_base / "profiles" / hashlib.sha256(name.encode()).hexdigest()
            path.mkdir(parents=True, exist_ok=False)
            seed = json.dumps({"PlayerToken": uuid.uuid4().hex, "PlayerName": "Arrival" + role,
                "CustomRulesWire": WIRE, "GraphicsQuality": 0, "MatchDefaultsRevision": 1}).encode("utf-8")
            (path / "settings.json").write_bytes(seed)
            seeds[path / "settings.json"] = seed
            profiles[role] = {"name": name, "root": str(path), "seedSha256": hashlib.sha256(seed).hexdigest()}
        before = read_input_preferences()
        (folder / "player-input-before.json").write_text(json.dumps(before, indent=2), encoding="utf-8")
        (folder / "profile-seeds.json").write_text(json.dumps(profiles, indent=2), encoding="utf-8")
        deadline = time.monotonic() + 120
        host = launch("host")
        while time.monotonic() < deadline:
            receipt = read(folder / "host.json")
            if receipt and receipt.get("phase") == "network_ready":
                break
            if host.poll() is not None:
                raise RuntimeError("Host stopped before its ordinary hub transport was ready")
            time.sleep(.2)
        else:
            raise TimeoutError("Host did not reach network readiness within the scenario ceiling")
        client = launch("client")
        while time.monotonic() < deadline and any(child.poll() is None for child in children):
            time.sleep(.2)
        if any(child.poll() is None for child in children):
            raise TimeoutError("Actual-peer completed arrival exceeded 120 seconds")
        result["errors"] = evaluate(read(folder / "host.json"), read(folder / "client.json"))
        if args.chat:
            # The client's line must traverse host Chat, then client ChatLine. The
            # host's earlier line may precede client admission, so it is not a gate.
            received = {}
            for role in ("host", "client"):
                log = (folder / (role + ".log")).read_text(encoding="utf-8-sig", errors="replace")
                received[role] = bool(re.search(r"\[Chat\] received from '[^']*': " +
                    re.escape(args.chat + " client") + r"\s*$", log, re.MULTILINE))
                if not received[role]:
                    result["errors"].append(role + " did not receive the client's complete chat marker")
            result["clientChatReceived"] = received
        result["graphicsObserved"] = {role: observed_renderer((folder / (role + ".log")).read_text(
            encoding="utf-8-sig", errors="replace")) for role in ("host", "client")}
        if any(value != "Direct3D11" for value in result["graphicsObserved"].values()):
            result["errors"].append("Both peers did not demonstrate the compatible Direct3D11 backend")
        if any(child.returncode != 0 for child in children):
            result["errors"].append("A task-owned player exited unsuccessfully")
        result["passed"] = not result["errors"]
    except Exception as error:
        result["errors"] = [str(error)]
    finally:
        try:
            for child in children:
                if child.poll() is None:
                    child.terminate()
            for child in children:
                try:
                    child.wait(timeout=8)
                except subprocess.TimeoutExpired:
                    child.kill(); child.wait()
            if before is not None:
                restore_input(before)
            result["inputRestored"] = before is None or before == read_input_preferences()
            for path, seed in seeds.items():
                path.write_bytes(seed)
            result["profileSeedsRestored"] = all(path.read_bytes() == seed for path, seed in seeds.items())
            result["runtimeUnchanged"] = before_hash == hashlib.sha256(runtime.read_bytes()).hexdigest()
            result["retiredPids"] = [child.pid for child in children]
            result["passed"] &= result["inputRestored"] and result["profileSeedsRestored"] and result["runtimeUnchanged"]
        except Exception as error:
            result["passed"] = False
            result["errors"].append("Preservation/cleanup failed: " + str(error))
        finally:
            if acquired:
                jobs.update_lease(jobs.POOL, claim["id"])
            (folder / "result.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
            print(json.dumps(result, indent=2), flush=True)
    return 0 if result["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
