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


def default_exe():
    """
    The shipped player on THIS machine.

    WARNING: `net_matrix.DEFAULT_EXE` IS A WINDOWS DESKTOP PATH AND THAT IS THE ONLY THING THAT
    STOPPED THIS RUNNING ON THE MAC. `CLAUDE.md` section 7 keeps a table of three machines and
    warns in its own words that "a note that is true on one machine and written as a fact about
    'here' sends whoever is on another one hunting"; a hard-coded path in a harness is that note
    as code. `tools/cold_start.py` learned the same lesson one file over, including that the
    binary inside a .app is named after `productName` rather than after the bundle.
    """
    import cold_start  # noqa: E402

    found = cold_start.player_path()
    return str(found) if found is not None else DEFAULT_EXE

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


def run(exe, seconds, work, allbots=True):
    """
    WARNING: `--no-allbots` IS A DIFFERENT MEASUREMENT AND NOT A TIDIER ONE.

    `-tp-allbots` makes `MatchInstaller.HumanSeat` answer -1, so a peer running it calls its OWN
    seat a bot as well as everybody else's. That is what a driven match needs and it is exactly
    the wrong switch for asking who the peers think the PEOPLE are: half of what the seat table
    prints under it is the harness. `docs/TODO.md` section 145.4b found three peers reporting
    three different human rosters and refused to fix it from that run for this reason.

    Without it each client holds its own seat as a human and simply stands there, and the seats
    nobody is sitting in are bots that drive. So the round still runs, the roster is the game's
    own, and the disagreement is either still there or it was the switch.
    """
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
    drive = ["-tp-allbots"] if allbots else []

    procs["referee"] = spawn(exe, work, "referee", [
        "-tp-dedicated", str(PORT), "-tp-profile", "refsrv"] + drive + [
        "-tp-autostart", "2",
        "-tp-netreport", reports["referee"], "-tp-netseconds", str(referee_seconds),
        "-screen-width", "640", "-screen-height", "400", "-screen-fullscreen", "0"])

    time.sleep(SETTLE)

    for tag, profile in (("client1", "refc1"), ("client2", "refc2")):
        procs[tag] = spawn(exe, work, tag, [
            "-tp-join", "127.0.0.1", str(PORT), "-tp-profile", profile] + drive + [
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

    WARNING: AND IT USED TO PRINT MORE THAN IT GATED, WHICH IS THE SAME FAULT ONE STEP SUBTLER.
    The table carried the protocol, the round, the seat roster, the characters and the scores;
    the verdict asked about the referee's own slot, each client's slot, each client's role, the
    defender and the per-seat character and taya flags **between the two clients only**. So the
    REFEREE could disagree with both of them about every seat in the game and the run came back
    green, and a reader looking at the printed table would have to notice by eye. `docs/TODO.md`
    section 145.4.

    WARNING: WHAT IS COMPARED AS AN EQUALITY AND WHAT IS NOT IS A DECISION PER FIELD, AND
    GETTING IT WRONG IN EITHER DIRECTION IS A USELESS GATE. The referee outlives both clients by
    design (see `run`), so the three reports are written seconds apart:

      * **Constant for the match** - the protocol, the mode, the map, the character in each seat,
        which seats are bots, the structural hash. These are hard equalities. A disagreement is
        real whenever you look.
      * **Monotonic** - the round number and the scores. A peer that stopped later may
        legitimately have more of both. What is refused is a peer that stopped LATER holding
        LESS, which cannot happen and needs no timing tolerance to say so.
      * **Derived** - the defender. `MatchRules.DefenderSlotFor(round)` on each peer's OWN round
        is the check, plus equality only where the rounds agree. Comparing the raw value between
        peers on different rounds would fail on a correct game.
      * **Sampled** - the discrete hash, the slipper states, the travelled distances. Printed,
        never gated. The discrete hash folds in the score, so it is a clock in disguise.
    """
    lines = []
    findings = []
    notes = []

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
            "%-9s : role %s  networked %s  slot %s  proto %s  round %s  active %s  defender %s  "
            "sampled %s s  struct %s  discrete %s"
            % (tag, r["role"], r["networked"], r["slot"], r["protocol"], r["round"], r["active"],
               r["defender"], r["sampled"], r["structural"], r["hash"]))

    present = [(tag, r) for tag, r in
               (("referee", ref), ("client1", c1), ("client2", c2)) if r is not None]

    if len(present) < 3:
        return lines, findings

    for tag, r in present:
        lines.append("%-9s : seats %s" % (tag, describe_seats(r)))

    # ---- the referee is seatless -------------------------------------------
    #
    # THE CLAIM: a referee holds no seat. `MatchInstaller` answers -1 for one.
    if ref["slot"] != "-1":
        findings.append(
            "the referee reported local slot %s, so it is holding a seat and is not seatless"
            % ref["slot"])

    # ---- the clients hold seats, and different ones ------------------------
    seats_held = {}
    for tag, r in (("client1", c1), ("client2", c2)):
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
            continue

        if r["slot"] in seats_held:
            findings.append(
                "%s and %s both report local slot %s. One gameplay seat with two owners is the "
                "reconnect fault: both peers believe they are driving it and the host is "
                "accepting movement and verbs from both."
                % (seats_held[r["slot"]], tag, r["slot"]))
        else:
            seats_held[r["slot"]] = tag

    for tag, r in (("client1", c1), ("client2", c2)):
        if r["role"] != "CLIENT":
            findings.append(
                "%s reported role %s: it fell back to its own lobby and auto-hosted rather than "
                "staying joined" % (tag, r["role"]))

    # ---- a round actually ran ----------------------------------------------
    #
    # THE CLAIM: two peers agreeing that nothing happened is not evidence, and neither is one.
    for tag, r in present:
        round_number = as_int(r["round"])
        if round_number is None or round_number < 1:
            findings.append("%s never reached round 1 (round %s), so no match played on it"
                            % (tag, r["round"]))

    for tag, r in (("client1", c1), ("client2", c2)):
        if r["active"] != "True":
            findings.append(
                "%s never reached a live round (round active %s)" % (tag, r["active"]))

    # ---- constant for the match: hard equalities ----------------------------
    for field, why in (
            ("protocol", "peers on different protocol numbers refuse each other by design, so "
                         "two that connected and disagree here means one of them is misreporting"),
            ("mode", "Classic and Hero Strike are four rounds and eight, with and without kits"),
            ("map", "two peers in different arenas are two matches"),
            ("structural", "the character in each seat, which seats are bots, and the protocol. "
                           "NetStateReport.StructuralHash covers only what cannot change while a "
                           "match runs, so a disagreement is real at any instant")):
        values = {tag: r[field] for tag, r in present}
        if len(set(values.values())) > 1:
            findings.append("%s: %s" % (field, ", ".join(
                "%s %s" % (tag, value) for tag, value in values.items())))

    # ---- the seat roster, all three ways round -----------------------------
    #
    # WARNING: AGAINST THE REFEREE AS WELL AS BETWEEN THE CLIENTS. Comparing only the two
    # clients is what let a referee disagree with both of them and still come back green: two
    # peers that were told the same wrong thing agree perfectly.
    by_seat = {}
    for tag, r in present:
        for seat in r["seats"]:
            by_seat.setdefault(seat["seat"], {})[tag] = seat

    for seat_number in sorted(by_seat):
        rows = by_seat[seat_number]

        if len(rows) < len(present):
            findings.append("seat %d is missing from %s"
                            % (seat_number,
                               ", ".join(t for t, _ in present if t not in rows)))
            continue

        # WARNING: `char` IS CONSTANT AND `bot` IS NOT, AND GATING THE SECOND ONE IS THE FAULT
        # section 145.4b WAS OPENED FOR. `MatchRpc.HostPeerLeft` flips the live flag the moment
        # somebody quits, and `run` gives the referee its clients' head start back plus a margin
        # ON PURPOSE, so the referee samples AFTER both of them have gone and their chairs have
        # correctly been handed to bots. The first run of the strengthened verifier reported that
        # as three findings and the game was right in all three.
        #
        # WARNING: SO THE PERSISTENT FACT IS WHAT IS GATED, AND IT IS A STRICTLY STRONGER CHECK.
        # `origin` is `Core.SeatOrigin`: `Bot` means nobody ever sat there, and `Human` and
        # `HandedToBot` both mean somebody did. A chair somebody sat in never becomes a chair
        # nobody sat in, whoever is driving it at the instant a report is written, so two peers
        # disagreeing about THAT are a real finding at any moment, which is exactly what the live
        # flag could never be.
        for field, why in (("char", "which character is in the chair"),
                           ("origin", "whether a PERSON ever sat in it, which is the half that "
                                      "cannot change while a match runs")):
            values = {tag: row.get(field) for tag, row in rows.items()}

            if field == "origin":
                if any(v is None for v in values.values()):
                    notes.append("seat %d: at least one peer predates the `origin` column, so "
                                 "the persistent roster could not be compared. Rebuild both."
                                 % seat_number)
                    continue

                # Human and HandedToBot are the same answer to "did a person sit here".
                values = {tag: (v != "Bot") for tag, v in values.items()}
                if len(set(values.values())) > 1:
                    findings.append("seat %d %s: %s" % (
                        seat_number, why,
                        ", ".join("%s %s" % (tag, rows[tag].get("origin"))
                                  for tag in values)))
                continue

            if len(set(values.values())) > 1:
                findings.append("seat %d %s (%s): %s" % (
                    seat_number, field, why,
                    ", ".join("%s %s" % (tag, value) for tag, value in values.items())))

        # WARNING: THE LIVE FLAG IS STILL PRINTED, AS A NOTE, BECAUSE IT IS INFORMATION AND NOT A
        # VERDICT. Reporting it as a finding was wrong; dropping it silently would be the other
        # half of the same mistake, and the `origin` beside it is what says whether a difference
        # is a departure or a disagreement.
        live = {tag: row.get("bot") for tag, row in rows.items()}
        if len(set(live.values())) > 1:
            notes.append("seat %d is driven by different things on different peers (%s), with "
                         "origins %s. That is a handover or a roster arriving late, not a "
                         "disagreement: only the origins above are gated."
                         % (seat_number,
                            ", ".join("%s %s" % (t, "bot" if v else "hum")
                                      for t, v in live.items()),
                            ", ".join("%s %s" % (t, rows[t].get("origin"))
                                      for t in rows)))

    # ---- derived: the taya --------------------------------------------------
    #
    # WARNING: THE DEFENDER IS NOT COMPARED RAW ACROSS PEERS AND MUST NOT BE. It is derived from
    # the round number (`docs/VISION.md` section 4), and the referee stops later than its
    # clients, so two peers on different rounds hold different defenders CORRECTLY. What is
    # checked is that each peer's own defender matches its own round, plus equality where the
    # rounds agree.
    for tag, r in present:
        round_number = as_int(r["round"])
        defender = as_int(r["defender"])
        if round_number is None or defender is None or round_number < 1:
            continue

        derived = (round_number - 1) % 4
        if defender != derived:
            findings.append(
                "%s is on round %d and calls seat %d the taya; the schedule derives seat %d. "
                "The role is a pure function of the round and is never accumulated."
                % (tag, round_number, defender, derived))

    rounds = {tag: as_int(r["round"]) for tag, r in present}
    if len({v for v in rounds.values() if v is not None}) > 1:
        notes.append("the peers stopped on different rounds (%s), which is expected: the "
                     "referee outlives its clients on purpose. Only the derived-taya check "
                     "applies across that gap." % ", ".join(
                         "%s r%s" % (t, v) for t, v in rounds.items()))
    else:
        defenders = {tag: r["defender"] for tag, r in present}
        if len(set(defenders.values())) > 1:
            findings.append("defender on one round: %s" % ", ".join(
                "%s %s" % (t, v) for t, v in defenders.items()))

    # ---- monotonic: the scores ---------------------------------------------
    #
    # WARNING: NOT AN EQUALITY. A peer sampled later can legitimately hold more points. What
    # cannot happen is a peer that stopped LATER holding FEWER, and that needs no tolerance.
    order = sorted(present, key=lambda item: as_float(item[1]["sampled"]) or 0.0)
    for i in range(len(order) - 1):
        (early_tag, early), (late_tag, late) = order[i], order[i + 1]
        early_by_seat = {s["seat"]: s for s in early["seats"]}

        for seat in late["seats"]:
            was = early_by_seat.get(seat["seat"])
            if was is None:
                continue

            if seat["score"] < was["score"]:
                findings.append(
                    "seat %d scored %d on %s (%s s) and %d on %s (%s s). A score cannot go "
                    "backwards, so the later peer lost an award the earlier one had."
                    % (seat["seat"], was["score"], early_tag, early["sampled"],
                       seat["score"], late_tag, late["sampled"]))

    # ---- printed, never gated ------------------------------------------------
    discretes = {tag: r["hash"] for tag, r in present}
    if len(set(discretes.values())) > 1:
        notes.append("the discrete hashes differ (%s), which is NOT a finding: it folds in the "
                     "score and the slipper states, and the three reports are written seconds "
                     "apart by design. The structural hash is the one that gates."
                     % ", ".join("%s %s" % (t, v) for t, v in discretes.items()))

    listening = log_says(work, "referee", "[NetBoot] host requested")
    if listening:
        lines.append("referee   : " + listening)
        if "FAILED" in listening:
            findings.append("the referee never opened its transport")

    return lines + ["note      : " + n for n in notes], findings


def describe_seats(report):
    """One short row per seat, so the table shows what the verdict is actually comparing."""
    return "  ".join(
        "%d[c%s %s %s %d]" % (s["seat"], s["char"], "bot" if s["bot"] else "hum",
                              "TAYA" if s["taya"] else "atk", s["score"])
        for s in sorted(report["seats"], key=lambda s: s["seat"]))


def as_int(value):
    try:
        return int(value)
    except (TypeError, ValueError):
        return None


def as_float(value):
    try:
        return float(value)
    except (TypeError, ValueError):
        return None


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--exe", default=None)
    ap.add_argument("--seconds", type=float, default=45.0)
    ap.add_argument("--out", default=os.path.join(REPO, "Logs", "referee"))
    ap.add_argument("--no-allbots", dest="allbots", action="store_false",
                    help="drop -tp-allbots, so each client holds its own seat as a HUMAN and "
                         "stands still. This is the topology that separates a roster the game "
                         "disagrees about from a roster the harness flattened. docs/TODO.md "
                         "section 145.4b")
    ap.set_defaults(allbots=True)
    args = ap.parse_args()

    exe = args.exe or default_exe()

    if not os.path.isfile(exe):
        print("referee_run: no player at " + str(exe))
        print("             Build one first: GameBuilder.BuildMac or GameBuilder.BuildWindows.")
        return 2

    args.exe = exe

    print("referee_run: " + args.exe)
    print("             a seatless referee on port %d and two clients, %.0f s%s"
          % (PORT, args.seconds, "" if args.allbots else ", clients idle (no -tp-allbots)"))
    print()

    parsed = run(args.exe, args.seconds, args.out, allbots=args.allbots)
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
