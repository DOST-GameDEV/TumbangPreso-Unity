#!/usr/bin/env python3
"""
Every network request method, and whether anything in the game actually calls it.

WHY THIS EXISTS
---------------
`docs/TODO.md` sections 25.1, 32 and 36 are all the same class of fault: a networked verb
wired on one side and not the other. The most expensive version is silent, because a
message nobody sends and a handler nobody reaches both compile, both look finished in a
diff, and both pass every test that runs on one machine.

Three of them were live in one tree on 2026-08-27:

  * `LungeChargeServerRpc` and `ShoveChargeServerRpc`, host-only broadcasts of an
    animation flag, with no call site anywhere since the day they were written.
  * `RequestEmoteServerRpc`, and a whole `PlayEmote` broadcast behind it, unreachable
    because `EmotePlayer.Request` had the words "Phase 5: else send the request to the
    host here" where the call belonged. Emotes had never travelled between peers.
  * `RequestResetServerRpc`, unreachable because `Carrier.StepDefender` righted the can
    locally on whichever peer held the key.

None of the three is findable by playing: a verb that does nothing on somebody else's
screen looks exactly like a verb nobody used.

WHAT IT REPORTS
---------------
Two different questions, because the two halves of a verb fail differently.

  * A CLIENT half (`Request*`, `Submit*`, `Select*`, `Declare*`, `Vote*`) is the thing a
    gameplay script is supposed to call instead of resolving locally. It needs a caller
    OUTSIDE `Runtime/Net/`: a request only the network layer calls is a request the game
    never makes, which is exactly how the emote and the lata reset were unwired.
  * A HOST half (`Broadcast*`, `Host*`, `*ClientRpc`) is usually driven from inside the
    router, off a message handler or the world tick, so a call site in its own file counts.
    It needs at least one caller SOMEWHERE. Zero anywhere is dead code.

⚠️ TESTS DO NOT COUNT, AND THAT IS THE WHOLE POINT. A test calling a request method proves
the method works, not that the game ever reaches it. Both dead charge relays would have
been "covered" by a test that called them directly.

Exit code is 1 when anything is unreachable, so it can gate a verification pass.
"""

import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
NET_DIR = os.path.join(ROOT, "Assets", "TumbangPreso", "Runtime", "Net")
SCAN_DIRS = [
    os.path.join(ROOT, "Assets", "TumbangPreso", "Runtime"),
    os.path.join(ROOT, "Assets", "TumbangPreso", "Editor"),
]
TEST_MARKERS = (os.sep + "Tests" + os.sep, os.sep + "PlayMode" + os.sep)

# Public entry points that carry a verb across the wire. `Broadcast*` and `*ClientRpc` are
# the host's half; `Request*`/`Submit*`/`Select*`/`Declare*`/`Vote*` are the client's.
DECL = re.compile(
    r"^\s*public\s+(?:static\s+)?[\w<>\[\],\.\?]+\s+"
    r"(?P<name>(?:Request|Submit|Select|Declare|Vote|Broadcast|Host)\w*|\w*ClientRpc)\s*\("
)

# Methods that are deliberately reached only through a handler in their own file, or only
# from the network layer itself. Each one needs a reason, or this list becomes the place
# dead code goes to hide.
EXEMPT = {
    # Reached by NetSession's transport callbacks, which live in the same folder.
    "HostPeerLeft": "NetSession.OnClientDisconnected",
    "HostLateJoin": "NetSession.OnClientConnected",
    "HostSyncPeer": "same file, snapshot path",
}


def collect_declarations():
    found = []
    for name in sorted(os.listdir(NET_DIR)):
        if not name.endswith(".cs"):
            continue
        path = os.path.join(NET_DIR, name)
        with open(path, encoding="utf-8", errors="replace") as handle:
            for number, line in enumerate(handle, start=1):
                stripped = line.strip()
                if stripped.startswith("//") or stripped.startswith("///"):
                    continue
                match = DECL.match(line)
                if match:
                    found.append((match.group("name"), name, number))
    return found


CLIENT_HALF = re.compile(r"^(?:Request|Submit|Select|Declare|Vote)")


def collect_sources():
    sources = {}
    for base in SCAN_DIRS:
        for dirpath, _, filenames in os.walk(base):
            if any(marker in dirpath + os.sep for marker in TEST_MARKERS):
                continue
            for filename in filenames:
                if not filename.endswith(".cs"):
                    continue
                path = os.path.join(dirpath, filename)
                with open(path, encoding="utf-8", errors="replace") as handle:
                    sources[path] = handle.read()
    return sources


def call_sites(sources, method, own_file):
    """Call sites split into (outside the Net folder, inside it), declarations excluded."""
    pattern = re.compile(r"[\.\s\?]" + re.escape(method) + r"\s*\(")
    outside = []
    inside = []
    for path, text in sources.items():
        in_net = os.path.dirname(path) == NET_DIR
        for number, line in enumerate(text.split("\n"), start=1):
            stripped = line.strip()
            if stripped.startswith("//") or stripped.startswith("///"):
                continue
            if DECL.match(line):
                continue
            if not pattern.search(line):
                continue

            where = f"{os.path.relpath(path, ROOT)}:{number}"
            (inside if in_net else outside).append(where)

    return outside, inside


def main():
    sources = collect_sources()
    declarations = collect_declarations()

    print(f"{'out':>4} {'in':>4}  {'method':<34} {'declared':<26} note")
    print("-" * 104)

    unreachable = []
    seen = set()
    for method, own_file, line in sorted(declarations):
        if method in seen:
            continue
        seen.add(method)

        outside, inside = call_sites(sources, method, own_file)
        is_client_half = bool(CLIENT_HALF.match(method))
        note = ""

        if method in EXEMPT:
            note = "exempt: " + EXEMPT[method]
        elif is_client_half and not outside:
            note = "UNREACHABLE: no gameplay script asks for this verb"
            unreachable.append(f"{method} ({own_file}:{line})")
        elif not is_client_half and not outside and not inside:
            note = "UNREACHABLE: nothing calls this at all"
            unreachable.append(f"{method} ({own_file}:{line})")
        else:
            shown = outside or inside
            if len(shown) <= 2:
                note = ", ".join(shown)

        print(f"{len(outside):>4} {len(inside):>4}  {method:<34} "
              f"{own_file + ':' + str(line):<26} {note}")

    print()
    print(f"{len(seen)} wire entry points, {len(unreachable)} unreachable.")

    if unreachable:
        print()
        print("UNREACHABLE. Each of these is either a verb that never travels or dead code:")
        for item in unreachable:
            print("  " + item)
        return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
