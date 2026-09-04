#!/usr/bin/env python3
"""Runs two real players against each other over a link this script controls, and reports.

WHAT THIS ANSWERS
=================

`docs/TODO.md` section 135.7 lists two things as blocked on a harness that does not exist:
Part 2's disconnect matrix "as executed tests" and Part 4's bad-wifi table. Both are a table
where every row is "put two peers on a link with this property and write down what happened".
This is that runner. `tools/net_link.py` is the bad link; this drives the players over it and
compares what the two of them ended up believing.

WARNING: THE PREMISE IN SECTION 135.7 IS TOO PESSIMISTIC AND IT COST THAT SESSION THE TABLE.
It says "there is no harness in this repository that can put two peers on a simulated link",
which is true only of the word SIMULATED. Two peers on a REAL link have been drivable since
`NetBootstrap` landed, and section 68.18.10 did exactly that with two built players on one
machine. `NetStateReport` was then written specifically so the two processes could be compared
line by line, and its own header carries the command lines. What was missing was one piece,
the shaping, and it is 250 lines of Python rather than a PlayMode harness.

WARNING: THIS RUNS THE BUILT PLAYER, NOT THE EDITOR, AND THAT IS THE POINT. A PlayMode test
with two `NetworkManager`s in one process shares a clock, a frame loop and a memory space with
the thing it is measuring. Two .exe files do not, so a hang in one is visible from the other,
which is the class of fault the disconnect rows are about.

WARNING: A ROW WITH NO WRITTEN EXPECTED OUTCOME IS NOT A TEST. Every scenario below carries an
`expect` string that says what the run is supposed to show BEFORE it is run. A number with no
prediction beside it is a number nobody can be wrong about.
"""

import argparse
import os
import re
import shutil
import subprocess
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)

DEFAULT_EXE = os.path.join(
    os.path.expanduser("~"), "Desktop", "TumbangPreso-Unity", "TumbangPreso.exe")

HOST_PORT = 8910
LINK_PORT = 8911

# How long the host gets to boot and reach the arena before the client is launched at it. The
# player takes several seconds to load Eskinita, and a client that arrives first simply fails
# to connect and falls back to the lobby.
SETTLE = 7.0


class Scenario:
    def __init__(self, name, expect, seconds=45,
                 delay=0.0, jitter=0.0, loss=0.0,
                 outage_at=0.0, outage_for=0.0,
                 kill=None, kill_at=0.0, direct=False):
        self.name = name
        self.expect = expect
        self.seconds = seconds
        self.delay = delay
        self.jitter = jitter
        self.loss = loss
        self.outage_at = outage_at
        self.outage_for = outage_for
        # "client", "host" or None: which process is killed part way through.
        self.kill = kill
        self.kill_at = kill_at
        # A clean row does not need the proxy at all, and running without it separates
        # "the link did this" from "the proxy did this".
        self.direct = direct


# The one-way delay is HALF the round trip the row is named after. A "300 ms" row is a player
# on the far side of the country, and that is 150 ms each way.
WIFI = [
    Scenario("clean, direct",
             "Both peers agree on every discrete field. This row is the control: any "
             "disagreement here is a bug in the game or in this harness, not in the link.",
             direct=True),
    Scenario("clean, through the link",
             "Identical to the direct row. It exists to prove the proxy itself costs nothing, "
             "so every later row's difference is the shaping and not the extra hop.",
             delay=0.0),
    Scenario("150 ms round trip",
             "No visible effect. This is an ordinary domestic connection and the game is "
             "expected to be indistinguishable from the clean row.",
             delay=75.0, jitter=10.0),
    Scenario("300 ms round trip",
             "Playable. Expect the client's own body to stay responsive because it predicts, "
             "and remote bodies to lag. No divergence in discrete state.",
             delay=150.0, jitter=20.0),
    Scenario("600 ms round trip",
             "Degraded but connected. WARNING: this is the row where "
             "`PlausibleIntentPose` is expected to start refusing, per section 135.3, because "
             "the host's idea of where a body is and the client's diverge by the round trip. "
             "With the section 135.4 fix a refusal now costs the client a rolled-back cooldown "
             "instead of a silently eaten press.",
             delay=300.0, jitter=40.0),
    Scenario("2 per cent loss",
             "No divergence. NGO's reliable channel covers this; the unreliable snapshot "
             "stream loses frames nobody sees.",
             delay=25.0, jitter=5.0, loss=0.02),
    Scenario("10 per cent loss",
             "Connected, visibly stuttering. Discrete state must STILL agree: everything that "
             "decides a point travels reliably. A disagreement here is a real bug.",
             delay=25.0, jitter=5.0, loss=0.10),
    Scenario("five second outage",
             "The client must survive it. UTP's disconnect timeout is the number that decides "
             "this, and if it is under five seconds the client drops and the run shows a bot "
             "taking its seat on the host.",
             delay=25.0, jitter=5.0, outage_at=20.0, outage_for=5.0),
]

DISCONNECT = [
    Scenario("client quits mid-match",
             "Host keeps refereeing. The vacated seat becomes a bot on the host's report "
             "(`bot` true), the score is retained, and per section 135.5 no throw wind-up is "
             "left behind. The client writes no report because it is gone, so this row is read "
             "off the host's file alone.",
             kill="client", kill_at=22.0),
    Scenario("host quits mid-match",
             "The client must not hang. It has no referee, so the honest outcome is a clean "
             "disconnect rather than a frozen arena. WARNING: a client that keeps playing "
             "against a host that is gone is the worst result this row can produce, because it "
             "looks fine on screen.",
             kill="host", kill_at=22.0),
    Scenario("link dies permanently",
             "Both processes are alive and cannot reach each other. This separates 'the peer "
             "went away' from 'the network went away', which are the same event to the "
             "transport and very different to a player in a tournament room.",
             outage_at=20.0, outage_for=600.0),
]


def parse_report(path):
    """Pulls the fields two peers must agree on out of a `NetStateReport` file."""
    if not os.path.exists(path):
        return None

    with open(path, "r", encoding="utf-8", errors="replace") as f:
        text = f.read()

    out = {"path": path, "seats": []}

    for key, pattern in (
            ("role", r"role\s*:\s*(\S+)"),
            ("networked", r"networked\s*:\s*(\S+)"),
            ("slot", r"local slot\s*:\s*(-?\d+)"),
            ("protocol", r"protocol\s*:\s*(\d+)"),
            ("sampled", r"sampled\s*:\s*([\d.]+)"),
            ("round", r"round\s*:\s*(-?\d+)"),
            ("active", r"round active\s*:\s*(\S+)"),
            ("defender", r"defender\s*:\s*(-?\d+)"),
            ("hash", r"discrete state hash\s*:\s*(\S+)")):
        m = re.search(pattern, text)
        out[key] = m.group(1) if m else None

    # seat char bot taya score travelled skills ults
    for m in re.finditer(
            r"^(\d)\s+(-?\d+)\s+(True|False)\s+(True|False)\s+(-?\d+)\s+([\d.]+)\s+(\d+)\s+(\d+)\s*$",
            text, re.MULTILINE):
        out["seats"].append({
            "seat": int(m.group(1)),
            "char": int(m.group(2)),
            "bot": m.group(3) == "True",
            "taya": m.group(4) == "True",
            "score": int(m.group(5)),
            "travelled": float(m.group(6)),
        })

    return out


def compare(host, client):
    """What the two peers disagree about, split by whether disagreement is allowed.

    WARNING: NOT EVERY DIFFERENCE IS A FAULT AND TREATING THEM ALIKE MAKES THE TABLE USELESS.
    `NetStateReport`'s own header says the hash is for the eye. The two processes stop sampling
    at slightly different moments, so a score taken either side of an award, and a travelled
    distance which is a continuous quantity integrated on two different frame timelines, are
    expected to differ. Who is who, who is taya and which character sits where are not.
    """
    hard, soft = [], []

    if host is None or client is None:
        return hard, soft

    if host["defender"] != client["defender"]:
        hard.append(f"defender {host['defender']} vs {client['defender']}")

    if host["protocol"] != client["protocol"]:
        hard.append(f"protocol {host['protocol']} vs {client['protocol']}")

    by_seat = {s["seat"]: s for s in client["seats"]}

    for h in host["seats"]:
        c = by_seat.get(h["seat"])
        if c is None:
            hard.append(f"seat {h['seat']} missing on the client")
            continue

        if h["char"] != c["char"]:
            hard.append(f"seat {h['seat']} character {h['char']} vs {c['char']}")
        if h["taya"] != c["taya"]:
            hard.append(f"seat {h['seat']} taya {h['taya']} vs {c['taya']}")
        if h["bot"] != c["bot"]:
            # A seat the client believes is a bot and the host believes is a person is a real
            # fault, but it is also what a mid-run handover legitimately looks like for a frame,
            # so it is reported separately rather than as a divergence.
            soft.append(f"seat {h['seat']} bot {h['bot']} vs {c['bot']}")
        if h["score"] != c["score"]:
            soft.append(f"seat {h['seat']} score {h['score']} vs {c['score']}")

        if h["travelled"] > 1.0 and c["travelled"] <= 0.1:
            hard.append(
                f"seat {h['seat']} moved {h['travelled']:.1f} m on the host and not at all on "
                f"the client, which is section 36.1's three statues")

    return hard, soft


def slug(name):
    return re.sub(r"[^a-z0-9]+", "-", name.lower()).strip("-")


def link_summary(work):
    """The proxy's own count of what it forwarded and dropped, for the link column."""
    path = os.path.join(work, "link.log")
    if not os.path.exists(path):
        return None

    with open(path, "r", encoding="utf-8", errors="replace") as f:
        for line in f:
            if line.startswith("[link] forwarded="):
                return line.strip()[len("[link] "):]

    return None


def run(scenario, exe, outdir, python):
    work = os.path.join(outdir, slug(scenario.name))
    shutil.rmtree(work, ignore_errors=True)
    os.makedirs(work, exist_ok=True)

    host_report = os.path.join(work, "host.txt")
    client_report = os.path.join(work, "client.txt")

    join_port = HOST_PORT if scenario.direct else LINK_PORT

    procs = {}
    link = None

    def spawn(tag, args):
        log = os.path.join(work, f"{tag}.log")
        return subprocess.Popen([exe] + args + ["-logFile", log],
                                stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

    # WARNING: THE HOST MUST OUTLIVE THE CLIENT BY MORE THAN THE CLIENT'S HEAD START, AND THE
    # FIRST RUN OF THIS GOT IT WRONG IN A WAY THAT LOOKED LIKE A GAME BUG. `NetStateReport`
    # ends with `Application.Quit()`, so the host tearing its transport down is a
    # `ClosedByRemote` on the client, which then falls back to the lobby and AUTO-HOSTS. The
    # client's report then reads `role: HOST`, `map: MatchSetup` and character -1 on all four
    # seats, which is indistinguishable at a glance from a client that never joined at all.
    # The client starts SETTLE seconds late, so the host is given that back plus a margin.
    #
    # WARNING: AND THE OVERHANG IS KEPT SMALL ON PURPOSE, BECAUSE IT IS PAID FOR IN SCORE
    # DRIFT. The two peers stop sampling at different moments, so every second the host runs
    # past the client is a second of scoring the client never saw. The first working run gave
    # the host 21 extra seconds and the reports read 1230 against 810 on one seat, which is
    # honest but reads like a divergence. Six seconds is enough for the client to write and
    # quit first and small enough that the scores stay comparable.
    host_seconds = scenario.seconds + SETTLE + 6

    # WARNING: `-tp-autostart 2` IS NOT OPTIONAL AND ITS ABSENCE IS SILENT. `-tp-host` loads
    # the arena, but `MatchInstaller.BuildReadyGate` opens a ready gate on any NETWORKED
    # session, and nothing presses through it without this switch. The first run of this
    # harness sat at that gate for its whole 25 seconds: both reports read `round: 0`,
    # `round active: False` and zero skills and ultimates on every seat, while the bodies
    # still drifted enough to print plausible `travelled` numbers. Two peers agreeing that a
    # round never started is not evidence about the link.
    procs["host"] = spawn("host", [
        "-tp-host", str(HOST_PORT), "-tp-profile", "mtxhost", "-tp-allbots",
        "-tp-autostart", "2",
        "-tp-netreport", host_report, "-tp-netseconds", str(host_seconds),
        "-screen-width", "640", "-screen-height", "400", "-screen-fullscreen", "0"])

    time.sleep(SETTLE)

    if not scenario.direct:
        link_args = [
            python, os.path.join(HERE, "net_link.py"),
            "--listen", str(LINK_PORT), "--to", f"127.0.0.1:{HOST_PORT}",
            "--delay", str(scenario.delay), "--jitter", str(scenario.jitter),
            "--loss", str(scenario.loss),
            "--outage-at", str(scenario.outage_at), "--outage-for", str(scenario.outage_for),
            # WARNING: IT MUST OUTLIVE THE CLIENT AND THEN STOP ON ITS OWN, and the first
            # version got the second half wrong. At `seconds + 20` the orchestrator always
            # reached `terminate()` first, and `TerminateProcess` on Windows runs no handler, so
            # the proxy never printed the forwarded and dropped counts that say whether the
            # shaping actually happened. The client starts about a second after the link and
            # runs `seconds`, so four is enough to outlive it and still self-report.
            "--seconds", str(scenario.seconds + 4)]

        link = subprocess.Popen(link_args, stdout=open(os.path.join(work, "link.log"), "w"),
                                stderr=subprocess.STDOUT)
        time.sleep(1.0)

    procs["client"] = spawn("client", [
        "-tp-join", "127.0.0.1", str(join_port), "-tp-profile", "mtxclient", "-tp-allbots",
        "-tp-autostart", "2",
        "-tp-netreport", client_report, "-tp-netseconds", str(scenario.seconds),
        "-screen-width", "640", "-screen-height", "400", "-screen-fullscreen", "0"])

    started = time.monotonic()
    killed_at = None

    deadline = scenario.seconds + 75.0
    while time.monotonic() - started < deadline:
        elapsed = time.monotonic() - started

        if scenario.kill and killed_at is None and elapsed >= scenario.kill_at:
            victim = procs.get(scenario.kill)
            if victim and victim.poll() is None:
                victim.kill()
                killed_at = elapsed
                print(f"    killed {scenario.kill} at {elapsed:.0f} s", flush=True)

        if all(p.poll() is not None for p in procs.values()):
            break

        time.sleep(0.5)

    for name, p in procs.items():
        if p.poll() is None:
            p.kill()

    if link is not None:
        link.terminate()

    time.sleep(1.0)

    return {
        "scenario": scenario,
        "host": parse_report(host_report),
        "client": parse_report(client_report),
        "work": work,
        "killed_at": killed_at,
    }


def describe(result):
    s = result["scenario"]
    host, client = result["host"], result["client"]

    if host is None and client is None:
        return "NEITHER PEER WROTE A REPORT"

    if client is None:
        if s.kill == "client":
            if host is None:
                return "client gone AND THE HOST DIED WITH IT"

            # WARNING: "THE HOST IS STILL ALIVE" IS NOT THE CLAIM. A process that survives but
            # sits frozen on a dead round looks identical in a process list, and a host that
            # stops refereeing when one of four peers leaves is the whole reason this row
            # exists. `round active` and a moving score are what say it kept going.
            top = max((x["score"] for x in host["seats"]), default=0)
            return ("host kept refereeing after the peer left: round %s active=%s, "
                    "top score %d, sampled %s s"
                    % (host["round"], host["active"], top, host["sampled"]))

        return "CLIENT WROTE NO REPORT (it never got far enough, or it hung)"

    if host is None:
        if s.kill == "host":
            return "host gone as intended; client survived to write its own file"
        return "HOST WROTE NO REPORT"

    # WARNING: A CLIENT THAT REPORTS `role: HOST` NEVER STAYED JOINED, AND IT IS THE MOST
    # MISLEADING FILE THIS HARNESS CAN PRODUCE. On losing its connection the player returns to
    # the lobby and AUTO-HOSTS, so the report is well formed, says `networked: True`, and
    # describes a session of one. Naming it here stops it being read as a divergence, which is
    # what it looks like on the seat rows: every character reads -1.
    if client["role"] == "HOST":
        if s.kill == "host" or s.outage_for >= 60.0:
            return "client lost the host and fell back to its own lobby, which is the expected end"
        return ("CLIENT FELL BACK TO HOSTING: it was disconnected before it could report. "
                "Read client.log for the disconnect reason")

    hard, soft = compare(host, client)

    if hard:
        return "DIVERGED: " + "; ".join(hard[:3])

    note = f"agreed (hash {host['hash']} / {client['hash']})"
    if soft:
        note += "; drift: " + "; ".join(soft[:2])

    return note


def emit(results):
    """Prints the table in the shape `docs/TODO.md` wants to carry it."""
    print()
    print("| row | link | expected | observed |")
    print("|---|---|---|---|")

    for r in results:
        s = r["scenario"]
        link = []
        if s.direct:
            link.append("direct")
        if s.delay:
            link.append(f"{s.delay * 2:.0f} ms rtt")
        if s.jitter:
            link.append(f"jitter {s.jitter:.0f}")
        if s.loss:
            link.append(f"loss {s.loss * 100:.0f}%")
        if s.outage_for:
            link.append(f"outage {s.outage_for:.0f}s at {s.outage_at:.0f}s")
        if s.kill:
            link.append(f"kill {s.kill} at {s.kill_at:.0f}s")

        expected = s.expect.replace("\n", " ")
        print(f"| {s.name} | {', '.join(link) or 'clean'} | {expected} | {describe(r)} |")

    print()
    for r in results:
        line = link_summary(r["work"])
        if line:
            print(f"{r['scenario'].name}: {line}")


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--exe", default=DEFAULT_EXE)
    p.add_argument("--out", default=os.path.join(REPO, "Logs", "net-matrix"))
    p.add_argument("--python", default=sys.executable)
    p.add_argument("--only", default=None,
                   help="substring of a scenario name, to run one row")
    p.add_argument("--set", dest="which", choices=["wifi", "disconnect", "all"], default="all")
    p.add_argument("--seconds", type=int, default=45)
    p.add_argument("--summarise", action="store_true",
                   help="re-read the reports already in --out and reprint the table, "
                        "without launching anything")
    args = p.parse_args()

    if not os.path.exists(args.exe):
        print(f"no player at {args.exe}", file=sys.stderr)
        return 2

    chosen = []
    if args.which in ("wifi", "all"):
        chosen += [("bad wifi", s) for s in WIFI]
    if args.which in ("disconnect", "all"):
        chosen += [("disconnect", s) for s in DISCONNECT]

    if args.only:
        chosen = [c for c in chosen if args.only.lower() in c[1].name.lower()]

    for _, s in chosen:
        s.seconds = args.seconds

    os.makedirs(args.out, exist_ok=True)
    results = []

    # WARNING: RE-READ THE FILES RATHER THAN RE-RUN THE PLAYERS. A full set is about fifteen
    # minutes of two game processes, and the reports on disk are the whole measurement. When
    # the READING of a row improves (a field the parser did not take, a verdict written more
    # carefully), the honest move is to re-read what was already measured, not to measure
    # again and quietly get a different draw.
    if args.summarise:
        for group, s in chosen:
            work = os.path.join(args.out, slug(s.name))
            results.append({
                "scenario": s, "group": group, "work": work, "killed_at": None, "wall": 0.0,
                "host": parse_report(os.path.join(work, "host.txt")),
                "client": parse_report(os.path.join(work, "client.txt")),
            })
        emit(results)
        return 0

    for i, (group, s) in enumerate(chosen, 1):
        print(f"[{i}/{len(chosen)}] {group}: {s.name}", flush=True)
        started = time.monotonic()
        r = run(s, args.exe, args.out, args.python)
        r["group"] = group
        r["wall"] = time.monotonic() - started
        results.append(r)
        print(f"    {describe(r)}  ({r['wall']:.0f} s)", flush=True)

    emit(results)
    return 0


if __name__ == "__main__":
    sys.exit(main())
