#!/usr/bin/env python3
"""
Every named message, its writer's field order and its reader's field order, side by side.

WHY THIS EXISTS
---------------
`MatchRpc` speaks 55 custom messages and every one of them is a hand-written pair: a
run of `writer.WriteValueSafe(...)` on the sending side and a run of
`reader.ReadValueSafe(out ...)` on the receiving side. Netcode does not check that the two
agree. A field added to one and not the other, or two fields swapped, does not fail: the
reader consumes the same bytes in the wrong order and hands the game plausible garbage.

`docs/TODO.md` § 32 and § 36 are both faults of that family found by reading. The specific
one this was written after is `SyncWorld`, which grew a tournament-clock block on the
sending side during the 2026-08-27 network pass: had the reader not grown with it, every
client would have read the taya's camp timer out of the middle of the score array.

WHAT IT REPORTS
---------------
For each message name, the ordered list of types written and the ordered list read. A
mismatch in length or in type order is a defect and exits 1.

⚠️ IT IS A TYPE CHECK, NOT A NAME CHECK. Two `float`s swapped between writer and reader are
invisible here and are a real bug; nothing short of naming the fields on both sides can
catch that, and the fields are locals. What this closes is the whole class of "somebody
added a field to one half", which is the one that has actually happened.

⚠️ AND IT ONLY UNDERSTANDS STRAIGHT-LINE RUNS. A writer inside a loop (`SyncWorld` writes
one float per seat) is unrolled by hand below via LOOPS; anything else with a loop needs
adding there or it will report a false mismatch, which is the honest failure direction.
"""

import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SOURCE = os.path.join(ROOT, "Assets", "TumbangPreso", "Runtime", "Net", "MatchRpc.cs")

WRITE = re.compile(r"writer\.WriteValueSafe\(|ask\.WriteValueSafe\(")

# ⚠️ TWO READ FORMS. `out float x` declares; `out attackerIdle[slot]` writes into an existing
# array element and has no type at all. Only the first can be type-checked, and missing the
# second entirely is what made a correct `SyncWorld` look four fields short.
READ_TYPED = re.compile(r"reader\.ReadValueSafe\(out\s+(?P<type>[\w\[\]<>\.]+)\s+\w+\s*\)")
READ_ANY = re.compile(r"reader\.ReadValueSafe\(\s*out\s")

SEND = re.compile(r"SendNamedMessage(?:ToAll)?\(\s*\"(?P<name>[A-Za-z0-9_]+)\"")
HANDLER = re.compile(r"RegisterNamedMessageHandler\(\"(?P<name>[A-Za-z0-9_]+)\",\s*(?P<fn>\w+)\)")
METHOD = re.compile(r"^\s*(?:private|public|internal)\s+[\w<>\[\],\.\?]+\s+(?P<fn>\w+)\s*\(")

# ⚠️⚠️ A METHOD THAT RECEIVES A `FastBufferWriter` IS CONTINUING SOMEBODY ELSE'S RUN, AND THE
# METHOD-BOUNDARY RESET MUST NOT FIRE ON IT. Measured 2026-08-30: `Flair` is the one message whose
# writer and whose send live in two methods (`BroadcastFlair` writes the five fields, then hands
# the writer to `HostRelayFlair`, which loops the peers and sends). The reset cleared the run at
# the second declaration, so the audit reported "writer emits 0 fields, reader takes 5" against a
# writer and a reader that agree exactly, field for field and type for type, and exited 1.
#
# ⚠️ THAT MATTERS MORE THAN ONE WRONG ROW. This audit gates a verification pass (`CLAUDE.md`
# § 7.1), so a false red is a gate everybody learns to walk past, which is the only way § 38.6's
# real finding gets missed when it comes. `docs/TODO.md` § 38.6.
#
# ⚠️ IT DOES NOT SUPPRESS THE CHECK, it locates the run. `Flair` is still counted and still
# type-checked, now as 5 against 5.
RELAY = re.compile(
    r"^\s*(?:private|public|internal)\s+[\w<>\[\],\.\?]+\s+\w+\s*\([^)]*FastBufferWriter\s+\w+")

# Runs the parser cannot see, because the field is emitted inside a loop.
LOOPS = {
    # `BroadcastMatchState` writes one float per seat for the attacker idle clocks.
    "SyncWorld": 4,
}

# -------------------------------------------------------------------
# ⚠️⚠️ ASYMMETRIES THAT ARE CORRECT, EACH WITH ITS REASON. A list like this is only honest
# while every row says WHY, because the alternative is a place to hide a real mismatch.
# -------------------------------------------------------------------
# ⚠️⚠️ `DeclareReady` AND `VoteRematch` USED TO BE HERE AND NEITHER NEEDS TO BE. Both wrote a
# peer id the reader threw away, because the host resolves the sender at the door: a peer that
# could name itself could ready or vote for somebody else. The field is DELETED from both rather
# than read and discarded, so there is no longer a value on the wire the host has to remember to
# ignore, and remembering is exactly what failed the first time (`docs/TODO.md` section 52.1).
ACCEPTED = {
    "ReqSnapshot":
        "one placeholder byte, because the request carries no data and the sender id is the "
        "whole message",
}


def write_type(line):
    """Best-effort type of a written expression, from its literal or its cast."""
    body = line.split("WriteValueSafe(", 1)[1]
    body = body.rsplit(")", 1)[0].strip()

    if body.startswith("(int)") or body.startswith("(byte)"):
        return "int" if body.startswith("(int)") else "byte"
    if re.match(r"^-?\d+\.\d+f?$", body):
        return "float"
    if re.match(r"^-?\d+$", body):
        return "int"
    if body in ("true", "false"):
        return "bool"
    return None


def collect_writers(lines):
    """
    Message name to the ordered run of writes that precedes each send.

    ⚠️ THE RUN RESETS AT A METHOD BOUNDARY. Accumulating across one made a writer whose send
    is inside an `if` donate its fields to the next message in the file, which reported a
    correct `SyncWorld` as one field long.

    ⚠️ EXCEPT AT A METHOD THAT TAKES THE WRITER AS A PARAMETER, which is continuing the run
    rather than starting one. See `RELAY` above and `docs/TODO.md` § 38.6.
    """
    runs = {}
    pending = []

    for line in lines:
        stripped = line.strip()
        if stripped.startswith("//") or stripped.startswith("///"):
            continue

        if METHOD.match(line) and not RELAY.match(line):
            pending = []

        if WRITE.search(line):
            pending.append(write_type(line))
            continue

        m = SEND.search(line)
        if m:
            runs.setdefault(m.group("name"), []).append(list(pending))
            pending = []

    return runs


def collect_readers(lines):
    """Handler name to the ordered run of reads inside it. `None` where the type is implicit."""
    runs = {}
    current = None
    depth = 0
    started = False

    for line in lines:
        stripped = line.strip()

        # ⚠️ AN EXPRESSION-BODIED METHOD HAS NO BRACES, SO IT NEVER CLOSES. `SelectLobbyPick
        # ServerRpc(...) => ...` sits directly above `OnSelectLobbyPickMsg`, and entering it
        # meant the handler's four reads were counted against a method that has none. It
        # reported the one message whose reader is completely correct as reading zero fields.
        if "=>" in line or line.rstrip().endswith(";"):
            if current is not None and not started:
                current = None
            if METHOD.match(line):
                continue

        if current is None:
            m = METHOD.match(line)
            if m:
                current = m.group("fn")
                runs[current] = []
                depth = 0
                started = False
            continue

        if not (stripped.startswith("//") or stripped.startswith("///")):
            for m in READ_ANY.finditer(line):
                typed = READ_TYPED.search(line[m.start():])
                runs[current].append(typed.group("type") if typed else None)

        depth += line.count("{") - line.count("}")
        if line.count("{"):
            started = True
        if started and depth <= 0:
            current = None

    return runs


def main():
    with open(SOURCE, encoding="utf-8", errors="replace") as handle:
        lines = handle.read().split("\n")

    writers = collect_writers(lines)
    readers = collect_readers(lines)
    handlers = dict(HANDLER.findall("\n".join(lines)))

    print(f"{'message':<16} {'written':<8} {'read':<6} note")
    print("-" * 92)

    problems = []
    for name in sorted(handlers):
        fn = handlers[name]
        read = readers.get(fn, [])
        runs = writers.get(name)

        if not runs:
            print(f"{name:<16} {'-':<8} {len(read):<6} no writer in this file")
            continue

        extra = LOOPS.get(name, 0)
        note = ""

        if name in ACCEPTED:
            print(f"{name:<16} {len(runs[0]) + extra:<8} {len(read) + extra:<6} accepted: {ACCEPTED[name]}")
            continue

        # ⚠️ THE LOOP ALLOWANCE APPLIES TO BOTH SIDES. A `for` that writes one field per seat is
        # matched by a `for` that reads one per seat, and both are one LINE, so adding the extra
        # to the writer alone reported the symmetric pair as four fields apart.
        read_total = len(read) + extra

        for run in runs:
            written = len(run) + extra
            if written != read_total:
                note = (f"MISMATCH: writer emits {written} fields, reader takes {read_total}"
                        + (f" (+{extra} in a loop on each side)" if extra else ""))
                problems.append(f"{name}: {note}")
                break

        # Where the writer's type is knowable, it must line up with the reader's.
        if not note:
            run = runs[0]
            for i, written_type in enumerate(run):
                if written_type is None or i >= len(read) or read[i] is None:
                    continue
                if written_type != read[i]:
                    note = f"MISMATCH: field {i} written as {written_type}, read as {read[i]}"
                    problems.append(f"{name}: {note}")
                    break

        print(f"{name:<16} {len(runs[0]) + extra:<8} {read_total:<6} {note}")

    print()
    print(f"{len(handlers)} named messages, {len(problems)} mismatched.")

    if problems:
        print()
        for p in problems:
            print("  " + p)
        return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
