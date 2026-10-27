"""One reproducible trailer capture with exact QualitySettings restoration and a job receipt."""
import argparse
import datetime as dt
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("kind", choices=("probe", "opening", "ending"))
    parser.add_argument("--tag", required=True)
    parser.add_argument("--width", type=int, default=960)
    parser.add_argument("--height", type=int, default=540)
    parser.add_argument("--fps", type=int, default=30)
    args = parser.parse_args()
    if not args.tag.replace("-", "").replace("_", "").isalnum():
        parser.error("Use an alphanumeric, hyphen/underscore tag")
    if not 240 <= args.width <= 3840 or not 135 <= args.height <= 2160 or args.fps not in (6, 12, 24, 30, 60):
        parser.error("Unsupported capture dimensions or frame rate")
    if sys.platform == "win32":
        running = subprocess.check_output(["powershell", "-NoProfile", "-Command",
            "@(Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'Unity.exe' } | Select-Object ProcessId,CommandLine) | ConvertTo-Json -Compress"], text=True).strip()
        if running and running != "[]":
            raise SystemExit("Another Unity Editor is active. Preserve it; no trailer job started: " + running)
    output = ROOT / "Logs/trailer/bring-it-home" / args.tag
    output.mkdir(parents=True, exist_ok=False)
    quality = ROOT / "ProjectSettings/QualitySettings.asset"
    quality_bytes = quality.read_bytes()
    (output / "QualitySettings.before.asset").write_bytes(quality_bytes)
    test = {"probe": "OpeningProbe", "opening": "Opening", "ending": "Ending"}[args.kind]
    command = [sys.executable, str(ROOT / "tools/run_unity_guarded.py"), "-batchmode", "-force-d3d11",
        "-tp-profile", "trailer-bring-it-home-1027", "-runTests", "-testPlatform", "PlayMode",
        "-testFilter", "TumbangPreso.PlayTests.Trailer.TrailerProductionRecording." + test,
        "-testResults", str(output / "results.xml"), "-logFile", str(output / "unity.log"),
        "-trailer-tag", args.tag, "-trailer-width", str(args.width), "-trailer-height", str(args.height),
        "-trailer-fps", str(args.fps)]
    inputs = list((ROOT / "Assets/TumbangPreso/Tests/PlayMode/Trailer").glob("*.cs"))
    receipt = {"started_utc": dt.datetime.now(dt.timezone.utc).isoformat(), "command": command,
        "source_commit": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(),
        "input_sha256": {str(p.relative_to(ROOT)): digest(p) for p in inputs},
        "quality_before_sha256": hashlib.sha256(quality_bytes).hexdigest(), "status": "starting"}
    receipt_path = output / "job.json"
    def save():
        receipt_path.write_text(json.dumps(receipt, indent=2), encoding="utf-8")
    save()
    process = None
    try:
        with (output / "guard.log").open("w", encoding="utf-8") as log:
            process = subprocess.Popen(command, cwd=ROOT, stdout=log, stderr=subprocess.STDOUT)
            receipt.update(status="running", guard_pid=process.pid)
            save()
            print(f"Trailer job {args.tag}: guard PID {process.pid}; receipt {receipt_path}", flush=True)
            result = process.wait()
        receipt["process_exit"] = result
    finally:
        if process is not None and process.poll() is None:
            process.wait()
        # The owned guard is terminal before restoring this exact file. No Git reset/restore/clean is used.
        quality.write_bytes(quality_bytes)
        receipt["quality_restored"] = quality.read_bytes() == quality_bytes
        receipt["finished_utc"] = dt.datetime.now(dt.timezone.utc).isoformat()
        receipt["status"] = "finished"
        save()
    results = output / "results.xml"
    if not results.exists():
        raise SystemExit("No fresh XML. Inspect unity.log and guard.log; compilation/capture is not validated.")
    root = ET.parse(results).getroot()
    receipt["tests"] = {key: root.attrib.get(key) for key in ("result", "total", "passed", "failed", "skipped")}
    save()
    print(json.dumps(receipt["tests"]), flush=True)
    if root.attrib.get("result") != "Passed" or int(root.attrib.get("total", "0")) < 1:
        raise SystemExit("Native trailer capture did not pass. Retain its evidence and repair the cause.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
