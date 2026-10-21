"""One bounded Windows loopback host-loss check against a frozen internal player."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import socket
import subprocess
import time
import uuid

import net_matrix
import run_unity_guarded as guard
import run_unity_job as jobs
import run_lan_peer as lan
from run_completed_arrival import read, restore_input, validate_rules
from run_ui_player_review import read_input_preferences

ROOT = Path(__file__).resolve().parents[1]
WIRE = lan.WIRE
SCOPE = ("One actual Windows loopback host process loss during custom 1-round/30-second "
         "two-human-origin match. Final round retirement and absence of completed events only; "
         "no direct MatchInProgress/clock, human input, WAN, AllBots or full-tournament claim.")


def live_faults(receipt, role, pid):
    if not receipt:
        return [role + " has no live receipt"]
    errors = []
    if (receipt.get("role") != role or receipt.get("pid") != pid or
            receipt.get("phase") != "live" or not receipt.get("sawLive") or
            not receipt.get("originHumanSeats") or receipt.get("error")):
        errors.append(role + " has not proved its owned two-human-origin live peer")
    if receipt.get("rounds") != 1 or receipt.get("roundSeconds") != 30:
        errors.append(role + " did not use the supported custom 1/30 rules")
    if receipt.get("matchEndedEvents") != 0 or receipt.get("recordReadyEvents") != 0:
        errors.append(role + " already completed before the host loss")
    if not receipt.get("scene") or receipt.get("scene") == "MatchSetup":
        errors.append(role + " was not in the arena")
    return errors


def evaluate(report, receipt, log, source_commit, client_pid, killed_at, exit_code, protocol):
    errors = []
    if killed_at is None:
        errors.append("The owned host was not deliberately killed after both live receipts")
    if exit_code != 0:
        errors.append("Client did not exit normally after its final report")
    if not report:
        return errors + ["Client wrote no final NetStateReport"]
    # A new lobby may auto-host; that does not permit the old round to remain active.
    if report.get("active") != "False" or report.get("round") != "0" or report.get("map") != "MatchSetup":
        errors.append("Client did not return to MatchSetup with its old round inactive/reset")
    if report.get("protocol") != str(protocol):
        errors.append("Client report did not use the agreed artifact protocol")
    if not receipt or receipt.get("role") != "client" or receipt.get("pid") != client_pid:
        errors.append("Missing or foreign post-loss client observation")
    else:
        if not receipt.get("sawLive") or not receipt.get("originHumanSeats") or receipt.get("scene") != "MatchSetup":
            errors.append("Client observation did not retain live admission and return to MatchSetup")
        if receipt.get("matchEndedEvents") != 0 or receipt.get("recordReadyEvents") != 0 or receipt.get("recordId"):
            errors.append("Host loss fabricated a completed match/record event")
        if receipt.get("error"):
            errors.append("Client observation reported an error: " + str(receipt["error"]))
    if not re.search(r"\[Abandon\] HostLost: ABANDONED at round 1 of 1;", log):
        errors.append("Client did not record the actual transport loss at round1/1")
    # BuildIdentity.OneLine prints 12 characters; the full SHA/hash were checked before launch.
    if not re.search(r"build identity\s*:\s*[^\r\n]*" + re.escape(source_commit[:12]) + r"\b", report.get("text", "")):
        errors.append("Client report lacks the frozen source identity prefix")
    return errors


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, default=ROOT)
    parser.add_argument("--exe", type=Path, required=True)
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("--port", type=int, default=9090)
    parser.add_argument("--source-commit", required=True)
    parser.add_argument("--runtime-sha", required=True)
    parser.add_argument("--build-receipt", type=Path, required=True)
    parser.add_argument("--artifact-manifest", type=Path, required=True)
    parser.add_argument("--protocol", type=int, required=True)
    args = parser.parse_args()
    project, exe, folder = args.project.resolve(), args.exe.resolve(), args.out.resolve()
    if os.name != "nt" or not exe.is_file() or not exe.is_relative_to(project / "Builds") or not folder.is_relative_to(project / "Logs"):
        parser.error("Use an existing Windows internal Builds player and dedicated Logs output")
    if (guard.project_identity(project) != ("BH Studios", "Tumbang Preso")
            or not 1024 <= args.port < 65535 or not 1 <= args.protocol <= 65535):
        parser.error("Use the frozen shipping identity and two nonprivileged ports")
    runtime = exe.parent / (exe.stem + "_Data/Managed/TumbangPreso.Runtime.dll")
    runtime_bytes = runtime.read_bytes()
    before_hash = hashlib.sha256(runtime_bytes).hexdigest()
    manifest_path = args.artifact_manifest.resolve()
    artifact = lan.checked_artifact(exe, manifest_path, args.protocol, args.runtime_sha.lower())
    manifest_hash = lan.file_sha256(manifest_path)
    build = read(args.build_receipt.resolve())
    identity = read(exe.parent / (exe.stem + "_Data/StreamingAssets/build-identity.json"))
    if (not build or not build.get("preservationCompleted") or
            (not build.get("passed") and artifact.get("classifiedArtifactAccepted") is not True) or
            artifact.get("sourceCommit") != args.source_commit or
            build.get("sourceCommit") != args.source_commit or
            build.get("runtimeSha256", "").lower() != args.runtime_sha.lower() or
            Path(build.get("artifact", "")).resolve() != exe or before_hash != args.runtime_sha.lower() or
            not identity or identity.get("sha") != args.source_commit or identity.get("protocol") != args.protocol or
            identity.get("target") != "StandaloneWindows64"):
        parser.error("Classified source/build receipt, player identity and Runtime hash must match")
    if b"NetCompletedArrivalProbe" not in runtime_bytes or b"NetStateReport" not in runtime_bytes:
        parser.error("Frozen player lacks the existing live observer/final reporter")
    rules = validate_rules(runtime.with_name("TumbangPreso.Core.dll"), wire=WIRE)
    folder.mkdir(parents=True, exist_ok=False)
    for port in (args.port, args.port + 1):
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as sock:
            sock.bind(("127.0.0.1", port))
    key = uuid.uuid4().hex[:10]
    claim = jobs.make_claim(project, "gpu", 2048, 2048, "host-loss-" + key,
                            [args.port, args.port + 1], ["-batchmode"])
    acquired, before = False, None
    profiles, seeds, children = {}, {}, []
    result = dict(passed=False, errors=["Scenario did not complete"], profiles=profiles, rules=rules,
                  sourceCommit=args.source_commit, runtimeSha256=before_hash,
                  protocol=args.protocol, artifactManifestSha256=manifest_hash,
                  buildReceipt=str(args.build_receipt.resolve()), scope=SCOPE, killedAt=None)

    def launch(role):
        route = ["-tp-lobby", "-tp-lobbyport", str(args.port)] if role == "host" else [
            "-tp-lobbyjoin", "127.0.0.1:" + str(args.port), "-tp-lobbyport", str(args.port + 1)]
        command = [str(exe), "-batchmode", "-force-d3d11", "-screen-fullscreen", "0", "-screen-width", "640", "-screen-height", "360",
                   "-tp-framecap", "60", "-tp-profile", profiles[role]["name"], "-tp-autostart", "2",
                   "-tp-completed-arrival", str(folder), "-tp-completed-role", role, "-tp-completed-port", str(args.port),
                   "-tp-netreport", str(folder / (role + ".txt")), "-tp-netseconds", "90" if role == "host" else "60",
                   "-logFile", str(folder / (role + ".log")), *route]
        startup = subprocess.STARTUPINFO(); startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW; startup.wShowWindow = 0
        child = subprocess.Popen(command, cwd=project, env=guard.unity_environment(), startupinfo=startup)
        children.append(child)
        result[role + "Pid"] = child.pid
        result[role + "Command"] = command
        return child

    try:
        result["admission"] = jobs.acquire(jobs.POOL, claim, 30)
        acquired = True
        for role in ("host", "client"):
            name = "host-loss-" + role + "-" + key
            path = guard.player_profile() / "profiles" / hashlib.sha256(name.encode()).hexdigest()
            path.mkdir(parents=True, exist_ok=False)
            seed = json.dumps({"PlayerToken": uuid.uuid4().hex, "PlayerName": "HostLoss" + role,
                               "CharacterPick": 0 if role == "host" else 2,
                               "CustomRulesWire": WIRE, "GraphicsQuality": 0, "MatchDefaultsRevision": 1}).encode("utf-8")
            (path / "settings.json").write_bytes(seed)
            seeds[path / "settings.json"] = seed
            profiles[role] = {"name": name, "root": str(path), "seedSha256": hashlib.sha256(seed).hexdigest()}
        before = read_input_preferences()
        (folder / "player-input-before.json").write_text(json.dumps(before, indent=2), encoding="utf-8")
        (folder / "profile-seeds.json").write_text(json.dumps(profiles, indent=2), encoding="utf-8")
        began = time.monotonic(); deadline = began + 90
        host = launch("host")
        while time.monotonic() < deadline:
            receipt = read(folder / "host.json")
            if receipt and receipt.get("phase") == "network_ready" and receipt.get("pid") == host.pid:
                break
            if host.poll() is not None:
                raise RuntimeError("Owned host stopped before ordinary hub transport readiness")
            time.sleep(.2)
        else:
            raise TimeoutError("Host did not reach network readiness within90s")
        client_began = time.monotonic(); client = launch("client")
        while time.monotonic() < min(deadline, client_began + 45):
            host_live, client_live = read(folder / "host.json"), read(folder / "client.json")
            if not live_faults(host_live, "host", host.pid) and not live_faults(client_live, "client", client.pid):
                if host_live["scene"] != client_live["scene"]:
                    raise RuntimeError("Live peers reported different arenas")
                for role, receipt in (("host", host_live), ("client", client_live)):
                    (folder / (role + "-before-kill.json")).write_text(json.dumps(receipt, indent=2), encoding="utf-8")
                break
            if host.poll() is not None or client.poll() is not None:
                raise RuntimeError("A peer exited before both live observations")
            time.sleep(.2)
        else:
            raise TimeoutError("No verified live pair with at least15s remaining before client report")
        if host.poll() is not None:
            raise RuntimeError("Host already stopped before the intended transport failure")
        # Popen retains the exact owned process handle; never look up a process by name/PID to kill.
        host.kill(); host.wait(timeout=8)
        result["killedAt"] = time.monotonic() - began
        result["clientSecondsAtKill"] = time.monotonic() - client_began
        result["hostKillExitCode"] = host.returncode
        print("Verified live pair; owned host stopped at %.1fs" % result["killedAt"], flush=True)
        while time.monotonic() < deadline and client.poll() is None:
            time.sleep(.2)
        if client.poll() is None:
            raise TimeoutError("Client exceeded the90s scenario ceiling")
        report = net_matrix.parse_report(folder / "client.txt")
        if report:
            report["text"] = (folder / "client.txt").read_text(encoding="utf-8-sig", errors="replace")
        receipt = read(folder / "client.json")
        log = (folder / "client.log").read_text(encoding="utf-8-sig", errors="replace")
        result["errors"] = evaluate(report, receipt, log, args.source_commit, client.pid, result["killedAt"], client.returncode, args.protocol)
        if (folder / "host.txt").exists():
            result["errors"].append("Host wrote its scheduled terminal report before intended loss")
        result["clientExitCode"] = client.returncode
        result["elapsedSeconds"] = time.monotonic() - began
        result["passed"] = not result["errors"]
    except Exception as error:
        result["errors"] = [str(error)]
    finally:
        try:
            for child in children:
                try:
                    if child.poll() is None:
                        child.terminate()
                    try:
                        child.wait(timeout=8)
                    except subprocess.TimeoutExpired:
                        child.kill(); child.wait(timeout=8)
                except Exception as error:
                    result["errors"].append("Owned process cleanup failed: " + str(error))
            try:
                if before is not None:
                    restore_input(before)
                result["inputRestored"] = before is not None and before == read_input_preferences()
            except Exception as error:
                result["errors"].append("Input restore failed: " + str(error))
            for path, seed in seeds.items():
                try:
                    path.write_bytes(seed)
                except Exception as error:
                    result["errors"].append("Owned profile seed restore failed: " + str(error))
            result["profileSeedsRestored"] = len(seeds) == 2 and all(path.read_bytes() == seed for path, seed in seeds.items())
            result["runtimeUnchanged"] = before_hash == hashlib.sha256(runtime.read_bytes()).hexdigest()
            result["manifestUnchanged"] = manifest_hash == lan.file_sha256(manifest_path)
            lan.checked_artifact(exe, manifest_path, args.protocol, args.runtime_sha.lower())
            result["artifactUnchanged"] = True
            result["retiredPids"] = [child.pid for child in children if child.poll() is not None]
            result["allOwnedProcessesRetired"] = len(children) == 2 and len(result["retiredPids"]) == 2
            for field in ("inputRestored", "profileSeedsRestored", "runtimeUnchanged", "manifestUnchanged", "artifactUnchanged", "allOwnedProcessesRetired"):
                if not result.get(field):
                    result["errors"].append("Preservation/cleanup gate failed: " + field)
            result["passed"] &= not result["errors"]
        except Exception as error:
            result["passed"] = False
            result["errors"].append("Preservation/cleanup failed: " + str(error))
        finally:
            try:
                if acquired and all(child.poll() is not None for child in children):
                    jobs.update_lease(jobs.POOL, claim["id"])
                result["leaseReleased"] = acquired and not any(job.get("id") == claim["id"] for job in jobs.read_pool(jobs.POOL))
            except Exception as error:
                result["leaseReleased"] = False
                result["errors"].append("Lease cleanup failed: " + str(error))
            result["passed"] &= result["leaseReleased"]
            (folder / "result.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
            print(json.dumps(result, indent=2), flush=True)
    return 0 if result["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
