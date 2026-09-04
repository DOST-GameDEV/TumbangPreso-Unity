#!/usr/bin/env python3
"""Puts a SEATLESS REFEREE on the wire and asks whether it can run a match.

WHAT THIS ANSWERS
=================

`Attention.md` section 16.2, in its own words: *"Nobody has ever run one."*

The architecture claims a refereeing process that holds no seat needs no new authority model.
`NetAuthority.IsSeatlessReferee` exists, `MatchInstaller` gives it slot -1, `LobbySession`
excludes it from every seat count, every authoritative path asks `ShouldResolve()` and every
point in the game is created in one function. **All of that is an argument.** The measurement
is whether a `-tp-dedicated` process actually starts, seats two clients, runs a round and
agrees with them about it.

WARNING: THIS IS THE MEASUREMENT SECTION 16.2 ASKS FOR BEFORE ANY CODE, AND THE ANSWER DECIDES
HOW MUCH CODE THERE IS. If a referee runs a match today, host loss at a venue is configuration
and a launch path rather than a rework: the operator's laptop referees, no player is the host,
and no player leaving can end a match. If it does not, the failure names what is missing.

WARNING: A REFEREE IS NOT A HOST WITH THE WINDOW MINIMISED, WHICH IS THE WHOLE POINT OF RUNNING
IT. `-tp-host` starts a server that also holds seat 0, so every host-authoritative path it
takes is being taken by a peer that is also a player. A `-tp-dedicated` server is
`_nm.IsServer && !_nm.IsClient`: it owns no body, submits no intent, and is the only
configuration in which "the host left" cannot mean "a player left".

WARNING: TWO CLIENTS, NOT ONE, AND THAT IS NOT A LUXURY. One client plus a referee cannot tell
"the referee is refereeing" from "the client is simulating on its own and nobody is
contradicting it". Two clients that agree with each other AND with the referee about the
defender, the roster and the taya is the claim worth making.

USAGE
-----
    python tools/referee_run.py [--exe PATH] [--seconds 45] [--out DIR]
"""

import argparse
import os
import subprocess
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)
sys.path.insert(0, HERE)

from net_matrix import parse_report, DEFAULT_EXE  # noqa: E402

PORT = 8930

# How long the referee gets to boot, open its transport and reach the arena before a client is
# launched at it. `net_matrix.SETTLE` is 7.0 for the same reason and against the same player.
SETTLE = 7.0


def spawn(exe, work, tag, args):
    log = os.path.join(work, tag + ".log")
    return subprocess.Popen([exe] + args + ["-logFile", log],
                            stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)


def log_says(work, tag, needle):
    """Whether a process's own log carries a line, for the claims no report can make."""
    path = os.path.join(work, tag + ".log")
    if not os.path.exists(path):
        return None
    with open(path, "r", encoding="utf-8", errors="replace") as f:
        for line in f:
            if needle in line:
                return line.strip()
    return None


def run(exe, seconds, work):
    os.makedirs(work, exist_ok=True)

    reports = {
        "referee": os.path.join(work, "referee.txt"),
        "client1": os.path.join(work, "client1.txt"),
        "client2": os.path.join(work, "client2.txt"),
    }

    for path in reports.values():
        if os.path.exists(path):
            os.remove(path)

    # WARNING: THE REFEREE MUST OUTLIVE BOTH CLIENTS. `NetStateReport` ends with
    # `Application.Quit()`, so a referee that writes first tears the transport down under the
    # clients, and a client that then falls back to its own lobby AUTO-HOSTS and writes
    # `role: HOST`. That reads as a client that never joined. `net_matrix` records the same
    # trap and gives the host the client's head start back plus a margin.
    referee_seconds = seconds + SETTLE + 8

    procs = {}

    # WARNING: `-tp-autostart 2` COUNTS SEATED PEERS AND THE REFEREE IS NOT ONE.
    # `LobbySession.PlayingPeerCount` skips `IsSeatlessReferee`, so "2" here means the two
    # clients and not one client plus the server. If that exclusion were wrong the run would
    # start a round with one client seated, which is itself a finding rather than a hang.
    procs["referee"] = spawn(exe, work, "referee", [
        "-tp-dedicated", str(PORT), "-tp-profile", "refsrv", "-tp-allbots",
        "-tp-autostart", "2",
        "-tp-netreport", reports["referee"], "-tp-netseconds", str(referee_seconds),
        "-screen-width", "640", "-screen-height", "400", "-screen-fullscreen", "0"])

    time.sleep(SETTLE)

    for tag, profile in (("client1", "refc1"), ("client2", "refc2")):
        procs[tag] = spawn(exe, work, tag, [
            "-tp-join", "127.0.0.1", str(PORT), "-tp-profile", profile, "-tp-allbots",
            "-tp-autostart", "2",
            "-tp-netreport", reports[tag], "-tp-netseconds", str(seconds),
            "-screen-width", "640", "-screen-height", "400", "-screen-fullscreen", "0"])
        # A second apart so two clients do not race the same seat assignment on arrival, which
        # would measure the seating code rather than the referee.
        time.sleep(1.0)

    started = time.monotonic()
    deadline = seconds + SETTLE + 90.0

    while time.monotonic() - started < deadline:
        if all(p.poll() is not None for p in procs.values()):
            break
        time.sleep(1.0)

    for p in procs.values():
        if p.poll() is None:
            p.kill()

    return dict((tag, parse_report(path)) for tag, path in reports.items())


def verdict(parsed, work):
    """What the three files say, and whether the referee actually refereed.

    WARNING: EVERY CLAIM HERE IS ONE A FAILING RUN COULD ALSO MAKE FALSE. A verdict that can
    only come out green is not a measurement, which is `net_matrix`'s `expect` rule applied one
    level up.
    """
    lines = []
    findings = []

    ref = parsed.get("referee")
    c1 = parsed.get("client1")
    c2 = parsed.get("client2")

    for tag in ("referee", "client1", "client2"):
        r = parsed.get(tag)
        if r is None:
            findings.append(tag + " wrote no report at all")
            lines.append("%-9s : NO REPORT" % tag)
            continue
        lines.append(
            "%-9s : role %s  networked %s  slot %s  round %s  active %s  defender %s  "
            "sampled %s s  hash %s"
            % (tag, r["role"], r["networked"], r["slot"], r["round"], r["active"],
               r["defender"], r["sampled"], r["hash"]))

    if ref is not None:
        # THE CLAIM: a referee holds no seat. `MatchInstaller` answers -1 for one.
        if ref["slot"] != "-1":
            findings.append(
                "the referee reported local slot %s, so it is holding a seat and is not seatless"
                % ref["slot"])

    # THE CLAIM: a round actually ran. Two peers agreeing nothing happened is not evidence.
    for tag, r in (("client1", c1), ("client2", c2)):
        if r is None:
            continue
        if r["active"] != "True":
            findings.append(
                "%s never reached a live round (round active %s)" % (tag, r["active"]))

        # WARNING: THIS CHECK WAS MISSING ON THE FIRST RUN AND THE FIRST RUN IS WHAT IT WOULD
        # HAVE CAUGHT. The verdict asserted that the REFEREE held no seat and never asked
        # whether the CLIENTS held one, so a client reporting `local slot: -1` printed in the
        # table and passed. A client with slot -1 is a player with no body: it owns nothing,
        # `HeroHazards`'s six `PlayerSlot == LocalSlot` tests are false for its own character,
        # and the arena looks completely normal. `docs/TODO.md` section 143.21.
        if r["slot"] == "-1":
            findings.append(
                "%s reported local slot -1, which is the REFEREE's value. A client holding no "
                "seat owns no body and its own abilities stop resolving on it." % tag)

        if r["role"] != "CLIENT":
            findings.append(
                "%s reported role %s: it fell back to its own lobby and auto-hosted rather than "
                "staying joined" % (tag, r["role"]))

    # THE CLAIM: the referee and both clients agree about the discrete state that decides points.
    if ref is not None and c1 is not None:
        if ref["defender"] != c1["defender"]:
            findings.append(
                "defender: referee %s vs client1 %s" % (ref["defender"], c1["defender"]))
    if c1 is not None and c2 is not None:
        if c1["defender"] != c2["defender"]:
            findings.append(
                "defender: client1 %s vs client2 %s" % (c1["defender"], c2["defender"]))
        by_seat = dict((s["seat"], s) for s in c2["seats"])
        for a in c1["seats"]:
            b = by_seat.get(a["seat"])
            if b is None:
                findings.append("seat %d missing on client2" % a["seat"])
                continue
            if a["char"] != b["char"]:
                findings.append(
                    "seat %d character %d vs %d between the clients"
                    % (a["seat"], a["char"], b["char"]))
            if a["taya"] != b["taya"]:
                findings.append(
                    "seat %d taya %s vs %s between the clients"
                    % (a["seat"], a["taya"], b["taya"]))

    listening = log_says(work, "referee", "[NetBoot] host requested")
    if listening:
        lines.append("referee   : " + listening)
        if "FAILED" in listening:
            findings.append("the referee never opened its transport")

    return lines, findings


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--exe", default=DEFAULT_EXE)
    ap.add_argument("--seconds", type=float, default=45.0)
    ap.add_argument("--out", default=os.path.join(REPO, "Logs", "referee"))
    args = ap.parse_args()

    if not os.path.isfile(args.exe):
        print("referee_run: no player at " + args.exe)
        print("             Build one with GameBuilder.BuildWindows first.")
        return 2

    print("referee_run: " + args.exe)
    print("             a seatless referee on port %d and two clients, %.0f s"
          % (PORT, args.seconds))
    print()

    parsed = run(args.exe, args.seconds, args.out)
    lines, findings = verdict(parsed, args.out)

    for line in lines:
        print("  " + line)

    print()
    if findings:
        print("referee_run: %d finding(s)" % len(findings))
        for f in findings:
            print("  FINDING  " + f)
        return 1

    print("referee_run: a seatless referee ran a match and both clients agreed with it.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
