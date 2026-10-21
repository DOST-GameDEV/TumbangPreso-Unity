"""Dante's MOUNTAIN FORM across real players: a host, the casting owner, an observer and a late joiner.

    python3 tools/net_mountain_matrix.py Builds/mountain/TumbangPreso.app --out Logs/mf/net-v1
    python3 tools/net_mountain_matrix.py Builds/mountain/TumbangPreso.app --out Logs/mf/net-v1 --evaluate-only

Every player runs `Diagnostics.NetMountainProbe` (`-tp-mountaintrace`): seat 1's own client casts CONTINENTAL DRIFT through
the shared cutscene, the host tries a stun, a concussion and a knock on him inside the form, seat 1 throws a rock at seat 2 (a
remote human beside him), and a fourth player joins while the form runs. Each writes what it draws. The checks:

  every peer      the form lasts 30 s on the server clock and ends; the suit is on him through it and gone after its debris;
                  his mountain clip set is on through it and off after; he is immune through it and not after
  agreement       each peer's time left matches the host's at the same server time (within 0.3 s)
  immunity        after the host's test he was never stunned or concussed on any peer
  rock            the host staggered seat 2, and seat 2's own process saw itself launched (it owns that body)
  the joiner      it draws the suit within 3 s of receiving the form, on the clock where the form already is

Uses named profiles mountainhost/owner/observer/joiner (copied before and restored after). Needs a built player.
"""
import argparse
import csv
import hashlib
import json
import shutil
import subprocess
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools"))
from net_request_safety import executable, player_data_root, named_profile  # noqa: E402

PEERS = ("host", "owner", "observer", "joiner")
SEAT = {"host": 0, "owner": 1, "observer": 2, "joiner": 3}


def rows(path):
    if not path.exists():
        return []
    with path.open(newline="", encoding="utf-8") as handle:
        return [{k: float(v) for k, v in r.items()} for r in csv.DictReader(handle)]


def left_at(trace, t):
    best = None
    for r in trace:
        if r["time"] <= t:
            best = r
        else:
            break
    return best["mountainLeft"] if best else None


def evaluate(folder):
    data = {p: rows(folder / (p + ".csv")) for p in PEERS}
    errors, m = [], {}
    host = data["host"]
    if not host:
        return {"ok": False, "errors": ["no host trace"], "measurements": {}}
    form = [r for r in host if r["mountainLeft"] > 0]
    if not form:
        return {"ok": False, "errors": ["the host never entered MOUNTAIN FORM"], "measurements": {}}
    began, ended = form[0]["time"], form[-1]["time"]
    m["host_form_seconds"] = round(ended - began, 2)
    for peer in ("host", "owner", "observer"):
        tr = data[peer]
        mine = {}
        if not tr or any(r["local"] != SEAT[peer] for r in tr):
            errors.append(peer + " did not hold its seat"); continue
        if any(r["hero"] != 1 for r in tr):
            errors.append(peer + " did not see Dante's kit on seat 1")
        f = [r for r in tr if r["mountainLeft"] > 0]
        if not f:
            errors.append(peer + " never saw the form"); continue
        mine["form_seconds"] = round(f[-1]["time"] - f[0]["time"], 2)
        mine["peak_left"] = max(r["mountainLeft"] for r in f)
        if abs(mine["form_seconds"] - 30) > .6 or mine["peak_left"] < 29:
            errors.append(peer + f" saw a form of {mine['form_seconds']} s (peak {mine['peak_left']:.2f})")
        inside = [r for r in f if r["mountainLeft"] < 29.5]
        mine["armour_inside"] = sum(r["armour"] for r in inside) / max(1, len(inside))
        mine["variant_inside"] = sum(r["variant"] for r in inside) / max(1, len(inside))
        mine["immune_inside"] = sum(r["immune"] for r in inside) / max(1, len(inside))
        if mine["armour_inside"] < .98: errors.append(peer + " lost the suit inside the form")
        if mine["variant_inside"] < .98: errors.append(peer + " lost his mountain clip set inside the form")
        if mine["immune_inside"] < .98: errors.append(peer + " lost the immunity inside the form")
        after = [r for r in tr if r["time"] > f[-1]["time"] + 2.5]
        if not after:
            errors.append(peer + " has no trace after the form")
        else:
            if any(r["armour"] for r in after): errors.append(peer + " kept the suit after its debris")
            if any(r["variant"] or r["immune"] for r in after): errors.append(peer + " kept the clips or the immunity after the form")
        tested = [r for r in tr if r["time"] >= next((h["time"] for h in host if h["immunityTested"]), 1e9)]
        inside_tested = [r for r in tested if r["mountainLeft"] > 0]
        mine["stun_after_test"] = max((r["casterStun"] for r in inside_tested), default=-1)
        mine["concussed_after_test"] = max((r["casterConcussed"] for r in inside_tested), default=-1)
        if mine["stun_after_test"] > .02 or mine["concussed_after_test"] > 0:
            errors.append(peer + " showed him stunned or concussed through the immunity")
        if peer != "host":
            diffs = [abs(r["mountainLeft"] - left_at(host, r["time"])) for r in f[5:-5] if left_at(host, r["time"]) not in (None, 0)]
            mine["max_clock_disagreement"] = round(max(diffs, default=0), 3)
            if mine["max_clock_disagreement"] > .3: errors.append(peer + " disagreed with the host's form clock")
        m[peer] = mine
    # the rock
    thrown_at = next((r["time"] for r in data["owner"] if r["thrown"]), None)
    m["thrown_at"] = thrown_at
    if thrown_at is None:
        errors.append("seat 1 never released the throw")
    else:
        hit = [r for r in host if r["time"] > thrown_at and r["targetStun"] > 0]
        m["host_target_stun"] = max((r["targetStun"] for r in hit), default=0)
        if not hit: errors.append("the host never staggered seat 2")
        obs = data["observer"]
        base = next((r for r in reversed(obs) if r["time"] <= thrown_at), None)
        later = [r for r in obs if thrown_at < r["time"] < thrown_at + 2.5]
        if base and later:
            m["observer_target_rise"] = round(max(r["targetY"] for r in later) - base["targetY"], 3)
            m["observer_target_travel"] = round(max(r["targetX"] for r in later) - base["targetX"], 3)
            if m["observer_target_rise"] < .3: errors.append("seat 2's own process did not see itself launched")
        else:
            errors.append("the observer has no trace round the throw")
        held = [r for r in host if r["held"] and r["mountainLeft"] > 0 and r["time"] < thrown_at]
        m["held_stone_share_host"] = round(sum(r["heldStone"] for r in held) / max(1, len(held)), 3)
    # the joiner
    j = data["joiner"]
    if j:
        jf = [r for r in j if r["mountainLeft"] > 0]
        m["joiner_rows"] = len(j)
        if not jf:
            errors.append("the joiner never received the form")
        else:
            first = jf[0]
            suit = next((r for r in jf if r["armour"] and r["armourShown"] > 0), None)
            m["joiner_form_received_left"] = round(first["mountainLeft"], 2)
            m["joiner_suit_after"] = round(suit["time"] - first["time"], 2) if suit else None
            if suit is None or suit["time"] - first["time"] > 3:
                errors.append("the joiner did not draw the suit within 3 s")
            else:
                expected = suit["armourEnd"] - suit["armourAge"]
                m["joiner_clock_error"] = round(abs(expected - suit["mountainLeft"]), 3)
                if m["joiner_clock_error"] > .5: errors.append("the joiner's suit is on a different clock from its form")
                hl = left_at(host, suit["time"])
                m["joiner_vs_host"] = round(abs(suit["mountainLeft"] - hl), 3) if hl else None
                if hl and abs(suit["mountainLeft"] - hl) > .5: errors.append("the joiner's form disagrees with the host's")
    else:
        m["joiner_rows"] = 0
    return {"ok": not errors, "errors": errors, "measurements": m}


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("player", type=Path)
    ap.add_argument("--out", type=Path, required=True)
    ap.add_argument("--port", type=int, default=8990)
    ap.add_argument("--delay", type=float, default=0.0, help="one-way ms for the owner and observer through tools/net_link.py")
    ap.add_argument("--join-left", type=float, default=16.0, help="launch the joiner when the host's form has this many seconds left")
    ap.add_argument("--evaluate-only", action="store_true")
    a = ap.parse_args()
    folder = a.out.resolve()
    if a.evaluate_only:
        result = evaluate(folder); print(json.dumps(result, indent=2)); return 0 if result["ok"] else 1
    if not folder.is_relative_to(ROOT / "Logs"):
        ap.error("--out must be under Logs/")
    folder.mkdir(parents=True, exist_ok=False)
    data = player_data_root()
    backups = []
    for p in PEERS:
        scope = named_profile(data, "mountain" + p)
        if scope.exists():
            for source in scope.rglob("*"):
                if source.is_file():
                    target = folder / "profile-backup" / source.relative_to(data)
                    target.parent.mkdir(parents=True, exist_ok=True); shutil.copy2(source, target)
                    backups.append((source, target, hashlib.sha256(source.read_bytes()).hexdigest()))
    exe = executable(a.player)
    processes, handles = [], []
    common = [str(exe), "-batchmode", "-screen-width", "640", "-screen-height", "360", "-screen-fullscreen", "0",
              "-tp-framecap", "60", "-tp-autostart", "3"]

    def peer(name, route):
        p = subprocess.Popen(common + ["-tp-profile", "mountain" + name, "-tp-mountaintrace", str(folder / (name + ".csv")),
                                       "-logFile", str(folder / (name + ".log"))] + route,
                             cwd=ROOT, stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT)
        processes.append(p); return p

    try:
        host = peer("host", ["-tp-host", str(a.port)]); time.sleep(7)
        port = a.port
        if a.delay:
            port = a.port + 1
            log = (folder / "link.log").open("w"); handles.append(log)
            processes.append(subprocess.Popen([sys.executable, str(ROOT / "tools/net_link.py"), "--listen", str(port), "--to",
                                               f"127.0.0.1:{a.port}", "--delay", str(a.delay), "--seconds", "240"],
                                              cwd=ROOT, stdout=log, stderr=subprocess.STDOUT))
            time.sleep(1)
        owner = peer("owner", ["-tp-join", "127.0.0.1", str(port)]); time.sleep(6)
        observer = peer("observer", ["-tp-join", "127.0.0.1", str(port)])
        print("Running host, owner and observer: " + str(folder), flush=True)
        joiner = None
        deadline = time.monotonic() + 220
        while time.monotonic() < deadline and any(p.poll() is None for p in (host, owner, observer)):
            if joiner is None:
                trace = rows(folder / "host.csv")
                live = [r for r in trace if r["mountainLeft"] > 0]
                if live and live[-1]["mountainLeft"] <= a.join_left:
                    joiner = peer("joiner", ["-tp-join", "127.0.0.1", str(a.port)])
                    print(f"Joiner launched at {live[-1]['mountainLeft']:.1f} s of the form left", flush=True)
            time.sleep(.5)
        if joiner is not None:
            end = time.monotonic() + 60
            while time.monotonic() < end and joiner.poll() is None:
                time.sleep(.5)
        result = evaluate(folder)
        result["runtime_sha256"] = hashlib.sha256((a.player.resolve() / "Contents/Resources/Data/Managed/TumbangPreso.Runtime.dll").read_bytes()).hexdigest() \
            if a.player.suffix == ".app" else None
        result["delay_one_way_ms"] = a.delay
        (folder / "result.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
        print(json.dumps(result, indent=2), flush=True)
        return 0 if result["ok"] else 1
    finally:
        for p in processes:
            if p.poll() is None:
                p.terminate()
        for p in processes:
            try:
                p.wait(timeout=8)
            except subprocess.TimeoutExpired:
                p.kill(); p.wait()
        for h in handles:
            h.close()
        for source, backup, expected in backups:
            source.parent.mkdir(parents=True, exist_ok=True); shutil.copy2(backup, source)
            if hashlib.sha256(source.read_bytes()).hexdigest() != expected:
                raise RuntimeError("named profile restoration failed")
        print(f"Restored {len(backups)} named-profile files", flush=True)


if __name__ == "__main__":
    raise SystemExit(main())
