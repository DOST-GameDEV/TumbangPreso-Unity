#!/usr/bin/env python3
"""Two real player processes: a host and one joining client that sends duplicated,
stale-seat and wrong-role gameplay requests. docs/TODO.md 149.4, C4 in
docs/CLAUDE_REQUEST_SAFETY_LANE.md.

The client half is Diagnostics/NetRequestSafetyProbe.cs. It sends through the same public
request methods the game uses, so every host admission check runs unchanged; this script
only launches the players, preserves the two named profiles it uses, and reads the host's
authoritative trace against the client's send markers.

Player path: a macOS .app bundle or a Windows .exe. The platform decides the profile root,
never a hard-coded user directory.

  python3 tools/net_request_safety.py Builds/c4/TumbangPreso.app --out Logs/c4/run-v1
  python3 tools/net_request_safety.py Builds/c4/TumbangPreso.app --out Logs/c4/eval --evaluate-only
"""
import argparse, csv, gzip, hashlib, json, os, shutil, subprocess, sys, time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PROFILES = ("c4host", "c4client")
LEAD = 0.2


def player_data_root():
    if sys.platform == "darwin":
        return Path.home() / "Library/Application Support/BH Studios/Tumbang Preso"
    if os.name == "nt":
        return Path(os.environ["USERPROFILE"]) / "AppData/LocalLow/BH Studios/Tumbang Preso"
    return Path.home() / ".config/unity3d/BH Studios/Tumbang Preso"


def named_profile(root, name):
    # ProfilePaths.ForProfile: profiles/<sha256 of the trimmed name>.
    return root / "profiles" / hashlib.sha256(name.strip().encode("utf-8")).hexdigest()


def executable(player):
    player = player.resolve()
    if player.suffix == ".app":
        # The bundle's binary is named after the product name, which carries a space.
        binaries = sorted((player / "Contents/MacOS").iterdir())
        if len(binaries) != 1:
            raise FileNotFoundError(f"expected one binary in {player}/Contents/MacOS")
        return binaries[0]
    return player


def runtime_dll(player):
    player = player.resolve()
    if player.suffix == ".app":
        return player / "Contents/Resources/Data/Managed/TumbangPreso.Runtime.dll"
    return player.parent / (player.stem + "_Data") / "Managed/TumbangPreso.Runtime.dll"


def read(path):
    # Committed evidence keeps the traces gzipped; a live run writes them plain.
    if not path.exists() and path.with_name(path.name + ".gz").exists():
        with gzip.open(path.with_name(path.name + ".gz"), "rt", newline="") as f:
            return [{k: (float(v) if k != "name" else v) for k, v in r.items()} for r in csv.DictReader(f)]
    with path.open(newline="") as f:
        return [{k: (float(v) if k != "name" else v) for k, v in r.items()} for r in csv.DictReader(f)]


def rising(rows, key, eps=1e-3):
    count, prev = 0, None
    for r in rows:
        if prev is not None and prev <= eps < r[key]:
            count += 1
        prev = r[key]
    return count


def not_rearmed(rows, key, eps=1e-3):
    armed = [r[key] for r in rows]
    first = next((i for i, v in enumerate(armed) if v > eps), None)
    return first is not None and all(b <= a + eps for a, b in zip(armed[first:], armed[first + 1:]))


def drops(rows, key, amount, tolerance=0.75):
    return sum(1 for a, b in zip(rows, rows[1:]) if abs((a[key] - b[key]) - amount) <= tolerance)


def evaluate(folder):
    """Every match in the run is judged on its own. Traces from before the match column
    existed are one match."""
    markers = read(folder / "client.markers.csv")
    matches = sorted({int(m.get("match", 1)) for m in markers}) or [1]
    results = {m: evaluate_match(folder, m) for m in matches}
    return {"ok": all(r["ok"] for r in results.values()),
            "errors": [f"match {m}: {e}" for m, r in results.items() for e in r["errors"]],
            "matches": {str(m): r for m, r in results.items()}}


def evaluate_match(folder, match):
    def mine(rows):
        return [r for r in rows if int(r.get("match", 1)) == match]

    host = mine(r for r in read(folder / "host.csv") if r["host"] == 1)
    client = mine(r for r in read(folder / "client.csv") if r["host"] == 0)
    markers = mine(m for m in read(folder / "client.markers.csv") if m["name"] != "staged")
    handover = (folder / "client-rejoin.markers.csv").exists()
    rejoined = []
    if handover:
        markers += mine(m for m in read(folder / "client-rejoin.markers.csv") if m["name"] != "staged")
        rejoined = mine(r for r in read(folder / "client-rejoin.csv") if r["host"] == 0)
    errors, cases = [], []

    def at(name):
        found = [m for m in markers if m["name"] == name]
        return found[0] if found else None

    def window(marker, seconds):
        rnd, t0 = marker["round"], marker["elapsed"]
        # The client's replicated round clock trails the host's by about one clock step, so a
        # request sent at client elapsed t lands at host elapsed t - 0.02. Start the window
        # early by LEAD; no two markers are closer than 0.5 s.
        before = [r for r in host if r["round"] == rnd and r["elapsed"] < t0 - LEAD]
        span = [r for r in host if r["round"] == rnd and t0 - LEAD <= r["elapsed"] <= t0 + seconds]
        return (before[-1] if before else None), span

    # When each send is scheduled (round, client elapsed). The handover arm's first client
    # leaves before the later round-1 sends, so those are not expected from anybody.
    schedule = {"stale-seat grab claiming seat 2": (1, 7.0), "duplicate grab x2": (1, 8.0),
                "duplicate throw x2": (1, 10.0), "stale-seat shove claiming seat 2": (1, 11.5),
                "wrong-role punch as attacker": (1, 12.0), "duplicate shove x2": (1, 13.0),
                "duplicate slide x2": (1, 17.0), "duplicate carapace cast x2": (1, 19.5),
                "stomp cast x3 in one frame": (1, 21.0), "stomp cast 4 after windup": (1, 22.5),
                "stomp cast 5 with no charge": (1, 24.0)}
    leave_marker = at("client leaves for the handover arm") or at("client waits to be killed for the handover arm")
    skipped = []

    def case(name, seconds, check):
        marker = at(name)
        planned = schedule.get(name)
        if marker is None and handover and leave_marker is not None and planned and planned[1] >= leave_marker["elapsed"]:
            skipped.append(name)
            return
        if marker is None:
            errors.append(f"{name}: client never sent it")
            return
        before, span = window(marker, seconds)
        if before is None or len(span) < 5:
            errors.append(f"{name}: host trace does not cover the window")
            return
        after = span[-1]
        result = check(before, span, after)
        failures = [k for k, ok in result.items() if not ok[0]]
        cases.append({"case": name, "round": int(marker["round"]), "client_elapsed": marker["elapsed"],
                      "host_samples": len(span),
                      "checks": {k: {"ok": v[0], "measured": v[1]} for k, v in result.items()}})
        errors.extend(f"{name}: {k} measured {result[k][1]}" for k in failures)

    def delta(key):
        return lambda b, s, a: (a[key] - b[key])

    case("stale-seat grab claiming seat 2", 0.9, lambda b, s, a: {
        # Seat 2 is a parked attacker bot that already carries ITS OWN shoe from the whistle.
        "seat 2 carry unchanged": (all(r["seat2Holding"] == b["seat2Holding"] for r in s), b["seat2Holding"]),
        "shoe never held by seat 2": (all(r["shoeHolder"] != 2 for r in s), sorted({r["shoeHolder"] for r in s})),
        "shoe stays loose": (all(r["shoeState"] == 0 for r in s), sorted({r["shoeState"] for r in s})),
    })
    case("duplicate grab x2", 1.5, lambda b, s, a: {
        "one retrieval counted": (a["retrievals"] - b["retrievals"] == 1, a["retrievals"] - b["retrievals"]),
        "held by seat 1": (a["shoeHolder"] == 1 and a["holding"] == 1, (a["shoeHolder"], a["holding"])),
    })
    case("duplicate throw x2", 1.5, lambda b, s, a: {
        "one throw counted": (a["throws"] - b["throws"] == 1, a["throws"] - b["throws"]),
        "hand empty afterwards": (a["holding"] == 0, a["holding"]),
        "held before": (b["holding"] == 1, b["holding"]),
    })
    case("stale-seat shove claiming seat 2", 0.45, lambda b, s, a: {
        "no shove attempt": (a["shoveAttempts"] == b["shoveAttempts"], a["shoveAttempts"] - b["shoveAttempts"]),
        "no refusal sent (bare return)": (a["denShove"] == b["denShove"], a["denShove"] - b["denShove"]),
        "no stamina spent on seat 1": (drops(s, "stamina", 25) == 0, drops(s, "stamina", 25)),
    })
    case("wrong-role punch as attacker", 0.9, lambda b, s, a: {
        "one punch refusal": (a["denPunch"] - b["denPunch"] == 1, a["denPunch"] - b["denPunch"]),
        "no punch cooldown stamped": (max(r["punchCd"] for r in s) == 0, max(r["punchCd"] for r in s)),
    })
    case("duplicate shove x2", 1.5, lambda b, s, a: {
        "one shove attempt": (a["shoveAttempts"] - b["shoveAttempts"] == 1, a["shoveAttempts"] - b["shoveAttempts"]),
        "one shove refusal": (a["denShove"] - b["denShove"] == 1, a["denShove"] - b["denShove"]),
        "stamina spent once": (drops(s, "stamina", 25) == 1, drops(s, "stamina", 25)),
        "cooldown stamped once": (rising([b] + s, "shoveCd") == 1, rising([b] + s, "shoveCd")),
    })
    case("duplicate slide x2", 1.5, lambda b, s, a: {
        "not holding before": (b["holding"] == 0 and b["shoeState"] == 0, (b["holding"], b["shoeState"])),
        "one slide refusal": (a["denSlide"] - b["denSlide"] == 1, a["denSlide"] - b["denSlide"]),
        "stamina spent once": (drops(s, "stamina", 25) == 1, drops(s, "stamina", 25)),
        "cooldown stamped once": (rising([b] + s, "slideCd") == 1, rising([b] + s, "slideCd")),
        "one retrieval counted": (a["retrievals"] - b["retrievals"] == 1, a["retrievals"] - b["retrievals"]),
    })
    case("duplicate carapace cast x2", 1.5, lambda b, s, a: {
        "cooldown stamped once": (rising([b] + s, "carapaceCd") == 1, rising([b] + s, "carapaceCd")),
        "cooldown never re-armed after the stamp": (not_rearmed(s, "carapaceCd"), max(r["carapaceCd"] for r in s)),
        "active": (max(r["carapaceActive"] for r in s) == 1, max(r["carapaceActive"] for r in s)),
    })
    case("stomp cast x3 in one frame", 1.2, lambda b, s, a: {
        "two charges before": (b["stompCharges"] == 2, b["stompCharges"]),
        "exactly one charge spent": (min(r["stompCharges"] for r in s) == 1 and a["stompCharges"] == 1,
                                     (min(r["stompCharges"] for r in s), a["stompCharges"])),
        "one windup": (rising([b] + s, "stompWindup") == 1, rising([b] + s, "stompWindup")),
    })
    case("stomp cast 4 after windup", 1.2, lambda b, s, a: {
        "second charge spent once": (b["stompCharges"] == 1 and a["stompCharges"] == 0, (b["stompCharges"], a["stompCharges"])),
        "one windup": (rising([b] + s, "stompWindup") == 1, rising([b] + s, "stompWindup")),
    })
    case("stomp cast 5 with no charge", 1.2, lambda b, s, a: {
        "charges stay at zero, never negative": (all(r["stompCharges"] == 0 for r in s), sorted({r["stompCharges"] for r in s})),
        "no windup": (rising([b] + s, "stompWindup") == 0, rising([b] + s, "stompWindup")),
    })
    case("duplicate punch x2", 0.8, lambda b, s, a: {
        "seat 1 is the taya": (b["defender"] == 1, b["defender"]),
        "cooldown stamped once": (rising([b] + s, "punchCd") == 1, rising([b] + s, "punchCd")),
        "one punch refusal": (a["denPunch"] - b["denPunch"] == 1, a["denPunch"] - b["denPunch"]),
    })
    case("duplicate lunge x2", 1.3, lambda b, s, a: {
        "one lunge attempt": (a["lungeAttempts"] - b["lungeAttempts"] == 1, a["lungeAttempts"] - b["lungeAttempts"]),
        "one lunge refusal": (a["denLunge"] - b["denLunge"] == 1, a["denLunge"] - b["denLunge"]),
        "cooldown stamped once": (rising([b] + s, "lungeCd") == 1, rising([b] + s, "lungeCd")),
    })
    case("wrong-role shove as taya", 0.9, lambda b, s, a: {
        "one shove refusal": (a["denShove"] - b["denShove"] == 1, a["denShove"] - b["denShove"]),
        "no shove attempt": (a["shoveAttempts"] == b["shoveAttempts"], a["shoveAttempts"] - b["shoveAttempts"]),
        "no stamina spent": (drops(s, "stamina", 25) == 0, drops(s, "stamina", 25)),
    })
    case("stale-seat punch claiming seat 0", 0.9, lambda b, s, a: {
        "seat 0 cooldown untouched": (max(r["seat0PunchCd"] for r in s) == 0, max(r["seat0PunchCd"] for r in s)),
        "no refusal sent (bare return)": (a["denPunch"] == b["denPunch"], a["denPunch"] - b["denPunch"]),
    })
    case("legitimate punch input", 0.8, lambda b, s, a: {
        "accepted, cooldown stamped once": (rising([b] + s, "punchCd") == 1, rising([b] + s, "punchCd")),
        "not refused": (a["denPunch"] == b["denPunch"], a["denPunch"] - b["denPunch"]),
    })
    case("legitimate lunge input", 1.3, lambda b, s, a: {
        "accepted once": (a["lungeAttempts"] - b["lungeAttempts"] == 1, a["lungeAttempts"] - b["lungeAttempts"]),
        "not refused": (a["denLunge"] == b["denLunge"], a["denLunge"] - b["denLunge"]),
    })
    case("legitimate stomp input", 1.2, lambda b, s, a: {
        "one charge spent": (b["stompCharges"] == 2 and a["stompCharges"] == 1, (b["stompCharges"], a["stompCharges"])),
        "one windup": (rising([b] + s, "stompWindup") == 1, rising([b] + s, "stompWindup")),
    })

    boundary = at("duplicate throw at the round boundary")
    if handover:
        leave = leave_marker
        log = (folder / "client-rejoin.log").read_text(errors="replace") if (folder / "client-rejoin.log").exists() else ""
        reclaimed = "LocalSlot=1 spectator=False host=False" in log
        cases.append({"case": "handover", "left_at_round1_elapsed": leave["elapsed"] if leave else None,
                      "rejoined_reclaimed_seat_1": reclaimed,
                      "rejoined_rows": len(rejoined)})
        # The rejoined arena installs as LocalSlot 0 for the frames before its seat message
        # lands (the client log prints both lines), so the claim is about round 2, not every row.
        # And the last frame after the HOST leaves reads LocalSlot 0 again during teardown, so
        # only rows while the host was still sampling round 2 count.
        host_end = max((r["elapsed"] for r in host if r["round"] == 2), default=-1)
        round2 = [r for r in rejoined if r["round"] == 2 and r["elapsed"] < host_end]
        cases[-1]["rejoined_round2_rows"] = len(round2)
        cases[-1]["rejoined_round2_all_seat_1"] = bool(round2) and all(r["local"] == 1 for r in round2)
        if leave is None or not reclaimed or not round2 or any(r["local"] != 1 for r in round2):
            errors.append("handover: the client did not leave and reclaim seat 1")
        host_round2 = [r for r in host if r["round"] == 2]
        if host_round2 and "seat1Bot" in host_round2[0]:
            bot_rows = sum(1 for r in host_round2 if r["seat1Bot"] == 1)
            cases[-1]["host_round2_rows_with_seat1_as_bot"] = bot_rows
            if bot_rows:
                errors.append(f"handover: the host still drove seat 1 as a bot for {bot_rows} round-2 rows")
        if (folder / "host.log").exists():
            cases[-1]["host_log_arrivals"] = [line for line in (folder / "host.log").read_text(errors="replace").splitlines()
                                              if "[NetArrival]" in line or "[Handover]" in line or "[NetDisconnect]" in line]
    elif host and "seat1Bot" in host[0] and any(r["seat1Bot"] == 1 for r in host):
        errors.append("seat 1 was driven as a bot on the host while its player was connected")
    if handover:
        pass
    elif boundary is None:
        errors.append("boundary throw: client never sent it")
    else:
        r1 = [r for r in host if r["round"] == 1]
        r2 = [r for r in host if r["round"] == 2]
        before = [r for r in r1 if r["elapsed"] < boundary["elapsed"] - LEAD]
        if not (before and r1 and r2):
            errors.append("boundary throw: trace does not cover the boundary")
        else:
            landed = r1[-1]["throws"] - before[-1]["throws"]
            after_whistle = r2[0]["throws"] - r1[-1]["throws"]
            cases.append({"case": "duplicate throw at the round boundary", "held_before": before[-1]["holding"],
                          "throws_in_last_active_frames": landed, "throws_after_last_active_sample": after_whistle,
                          "last_round1_elapsed": r1[-1]["elapsed"]})
            if landed > 1 or landed < 0:
                errors.append(f"boundary throw: {landed} throws landed in the last active frames")
            if after_whistle != 0:
                errors.append(f"boundary throw: {after_whistle} throws counted after the last active round-1 sample")

    # The two refusal counters are the pair § 145.12 built: sent on the host, taken back on
    # the client. Equal totals mean every refusal the host sent reached the peer that asked.
    if host and client:
        last_host, last_client = host[-1], dict(client[-1])
        if handover and rejoined:
            # A reclaimed seat is a new process whose taken-back tally starts at zero.
            for key in ("denPunch", "denLunge", "denShove", "denSlide"):
                last_client[key] = client[-1][key] + rejoined[-1][key]
            client = rejoined
        for key in ("denPunch", "denLunge", "denShove", "denSlide"):
            if last_host[key] != last_client[key]:
                errors.append(f"refusal tally {key}: host sent {last_host[key]}, client took {last_client[key]}")
        cases.append({"case": "refusal tallies", "host": {k: last_host[k] for k in ("denPunch", "denLunge", "denShove", "denSlide")},
                      "client": {k: last_client[k] for k in ("denPunch", "denLunge", "denShove", "denSlide")}})
        legit = at("legitimate stomp input")
        if legit:
            settle = legit["elapsed"] + 2.0
            h = [r for r in host if r["round"] == 2 and r["elapsed"] >= settle]
            c = [r for r in client if r["round"] == 2 and r["elapsed"] >= settle]
            if h and c:
                ok = h[0]["stompCharges"] == c[0]["stompCharges"]
                cases.append({"case": "owner agrees after legitimate stomp", "host": h[0]["stompCharges"], "client": c[0]["stompCharges"]})
                if not ok:
                    errors.append("legitimate stomp: owner and host charges disagree")
    return {"ok": not errors, "errors": errors, "skipped_after_leave": skipped, "cases": cases}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("player", type=Path)
    ap.add_argument("--out", type=Path, required=True)
    ap.add_argument("--port", type=int, default=8970)
    ap.add_argument("--delay", type=float, default=0.0, help="one-way milliseconds through tools/net_link.py")
    ap.add_argument("--jitter", type=float, default=0.0, help="net_link.py jitter, milliseconds either side")
    ap.add_argument("--loss", type=float, default=0.0, help="net_link.py per-packet loss fraction, 0.03 is 3 per cent")
    ap.add_argument("--kill", action="store_true", help="with --handover, kill the client instead of letting it quit")
    ap.add_argument("--evaluate-only", action="store_true")
    ap.add_argument("--handover", action="store_true", help="client leaves in round 1 and reclaims seat 1 for round 2")
    ap.add_argument("--matches", type=int, default=1, help="2 plays the whole script again after a real rematch")
    ap.add_argument("--leave-at", type=float, default=25.0, help="round-1 elapsed seconds at which the handover client leaves")
    a = ap.parse_args()
    folder = a.out.resolve()

    if a.evaluate_only:
        result = evaluate(folder)
        print(json.dumps(result, indent=2))
        return 0 if result["ok"] else 1

    folder.mkdir(parents=True, exist_ok=False)
    data = player_data_root()
    backup = folder / "profile-backup"
    manifest, absent = {}, []
    for name in PROFILES:
        scope = named_profile(data, name)
        if not scope.exists():
            absent.append(scope)
            continue
        for source in scope.rglob("*"):
            if source.is_file():
                rel = source.relative_to(data)
                target = backup / rel
                target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(source, target)
                manifest[str(rel)] = hashlib.sha256(source.read_bytes()).hexdigest()

    exe = executable(a.player)
    processes, handles = [], []
    try:
        common = [str(exe), "-batchmode", "-screen-width", "640", "-screen-height", "360",
                  "-screen-fullscreen", "0", "-tp-autostart", "2"]
        if a.matches > 1:
            common += ["-tp-autorematch", "-tp-requestsafety-matches", str(a.matches)]
        host = subprocess.Popen(common + ["-tp-host", str(a.port), "-tp-profile", "c4host",
                                          "-tp-requestsafety", str(folder / "host.csv"),
                                          "-logFile", str(folder / "host.log")],
                                cwd=ROOT, stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT)
        processes.append(host)
        time.sleep(7)
        port = a.port
        if a.delay or a.jitter or a.loss:
            port = a.port + 1
            log = open(folder / "link.log", "w")
            handles.append(log)
            processes.append(subprocess.Popen([sys.executable, str(ROOT / "tools/net_link.py"), "--listen", str(port),
                                               "--to", f"127.0.0.1:{a.port}", "--delay", str(a.delay),
                                               "--jitter", str(a.jitter), "--loss", str(a.loss),
                                               "--seconds", "200"], cwd=ROOT, stdout=log, stderr=subprocess.STDOUT))
            time.sleep(1)
        def join(stem, extra):
            p = subprocess.Popen(common + ["-tp-join", "127.0.0.1", str(port), "-tp-profile", "c4client",
                                           "-tp-requestsafety", str(folder / (stem + ".csv")),
                                           "-logFile", str(folder / (stem + ".log"))] + extra,
                                 cwd=ROOT, stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT)
            processes.append(p)
            return p

        leave = ["-tp-requestsafety-leave", str(a.leave_at)] + (["-tp-requestsafety-kill"] if a.kill else [])
        client = join("client", leave if a.handover else [])
        print("Running real host and client: " + str(folder), flush=True)
        deadline = time.monotonic() + 300 * a.matches
        relaunched = False
        while time.monotonic() < deadline and (host.poll() is None or client.poll() is None):
            if a.handover and a.kill and client.poll() is None and not relaunched:
                marks = folder / "client.markers.csv"
                if marks.exists() and "client waits to be killed" in marks.read_text():
                    client.kill()
                    client.wait()
                    print("Client killed with its connection still open on the host", flush=True)
            if a.handover and not relaunched and client.poll() is not None and host.poll() is None:
                relaunched = True
                print("Client left; relaunching the same profile to reclaim seat 1", flush=True)
                client = join("client-rejoin", ["-tp-requestsafety-rejoin"])
            time.sleep(1)
        result = evaluate(folder)
        result.update(match_count=a.matches, delay_one_way_ms=a.delay, jitter_ms=a.jitter, loss=a.loss, killed=a.kill, handover=a.handover, leave_at=a.leave_at if a.handover else None, platform=sys.platform,
                      runtime_sha256=hashlib.sha256(runtime_dll(a.player).read_bytes()).hexdigest())
        (folder / "result.json").write_text(json.dumps(result, indent=2))
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
                p.kill()
                p.wait()
        for h in handles:
            h.close()
        # Put the two named profiles back exactly: restore every backed-up file, and remove a
        # profile folder this run created from nothing.
        for scope in absent:
            if scope.exists() and scope.resolve().is_relative_to(data.resolve()):
                shutil.rmtree(scope)
        for rel, digest in manifest.items():
            target = data / rel
            if not target.resolve().is_relative_to(data.resolve()):
                raise ValueError("Unsafe profile restore")
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(backup / rel, target)
            if hashlib.sha256(target.read_bytes()).hexdigest() != digest:
                raise RuntimeError("Profile restore failed")
        print(f"Preserved {len(manifest)} profile files; {len(absent)} named profiles did not exist before and were removed", flush=True)


if __name__ == "__main__":
    raise SystemExit(main())
