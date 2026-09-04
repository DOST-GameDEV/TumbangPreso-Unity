#!/usr/bin/env python3
"""
Event subscriptions with no matching unsubscribe: the reason match five differs from match one.

WHY THIS EXISTS
---------------
`docs/TODO.md` § 126.8's finding, in its own words, is "cross-test lifetime leakage:
objects, statics, scenes and one cloud session outliving the test that made them." That
is the same defect class the PLAYER meets, one level up and much harder to see: a
handler that outlives its match is still subscribed when the next one starts, so the
second match runs it twice and the fifth runs it five times.

Nothing in this repository has ever checked it. There are 89 `+=` subscriptions in
`Assets/TumbangPreso/Runtime/`.

⚠️ A DOUBLE-FIRED HANDLER IS NOT A CRASH, WHICH IS WHY IT SURVIVES TESTING. It is a
score toast played twice, an announcer line overlapping itself, a round advanced twice,
or an award counted once per stale listener. Every one of those reads as "the game got
weird after a few matches" and none of them reproduces in a fresh run.

WHAT IT CHECKS
--------------
For each `X.Event += Handler`, is there a `X.Event -= Handler` in the same file?

⚠️ THE PAIR IS KEYED ON THE EVENT NAME AND THE HANDLER, NOT ON THE EXPRESSION THAT
REACHED THEM. `AIController` subscribes via `match.Scored += OnScored` and releases via
`_hookedMatch.Scored -= OnScored`, and that asymmetry is the CORRECT pattern rather than
sloppiness: `GameServices.Match` is a property, so releasing through it lets go of
whichever director is current at teardown, which need not be the one you took. Keying on
the whole expression reported five correct unsubscribes as leaks.

WHAT IT DELIBERATELY DOES NOT FLAG
----------------------------------
A subscription whose PUBLISHER CANNOT OUTLIVE THE SUBSCRIBER. If the object raising the
event is created by the same component that listens to it and dies with it, there is
nothing to leak into: `MatchInstaller` adds `ReadyGate` to its own `gameObject`, so the
arena unload takes both. Those are in KNOWN_SAME_LIFETIME with the line that makes the
claim true, and the audit fails if a row stops matching anything.

⚠️ AN ALLOWLIST ENTRY THAT NO LONGER MATCHES ANYTHING IS AN ERROR, for
`audit_cue_relay.py`'s reason: "both allowlists assert the line that makes their claim
true still exists, so deleting one fails here rather than going quiet in a match."

Exits non-zero on any finding. `tools/qualify.py --stage audits` runs it.
"""

import pathlib
import re
import sys

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass

ROOT = pathlib.Path(__file__).resolve().parent.parent
RUNTIME = ROOT / "Assets" / "TumbangPreso" / "Runtime"

SUBSCRIBE = re.compile(r"(?<![+\-*/=!<>])\b([\w\.\[\]]+?)\s*\+=\s*([\w\.]+)\s*;")
UNSUBSCRIBE = re.compile(r"(?<![+\-*/=!<>])\b([\w\.\[\]]+?)\s*-=\s*([\w\.]+)\s*;")

# ⚠️⚠️ TELLING A SUBSCRIPTION FROM AN ACCUMULATOR IS THE WHOLE PROBLEM, AND `+=` LOOKS
# IDENTICAL IN BOTH. The first run of this audit reported 76 findings and about sixty of
# them were `_clock += dt`, `_elapsed += Time.deltaTime` and `line.DistanceTravelled +=
# step`. An audit that is mostly noise is an audit nobody reads, which is worse than not
# having one, so the discriminator has to be structural rather than a list of names.
#
# It is C# naming, and it is reliable here because this codebase follows it without
# exception: an EVENT is a PascalCase member, and a HANDLER is a PascalCase method group.
# An accumulator is a lowercase local or a `_camelCase` field on one side and a lowercase
# local or a property like `Time.deltaTime` on the other. Requiring BOTH sides to end in a
# PascalCase segment keeps every real subscription and drops every accumulator.
PASCAL_TAIL = re.compile(r"(?:^|\.)([A-Z]\w*)\s*$")


def event_name(expression):
    """The event's own name, without whatever expression reached the object holding it."""
    return expression.replace("[", " ").replace("]", " ").strip().split(".")[-1]


def is_member(expression):
    """Whether an expression ends in a PascalCase member, which an event and a handler both do."""
    return bool(PASCAL_TAIL.search(expression.replace("[", " ").replace("]", " ").strip()))

KNOWN_SAME_LIFETIME = [
    # (file, target fragment, why the publisher cannot outlive the subscriber)
    ("MatchInstaller.cs", "gate.",
     "BuildReadyGate does `gameObject.AddComponent<ReadyGate>()`, so the gate is a "
     "component on the installer's OWN object and the arena unload destroys both in the "
     "same frame. The HUD and the SliceRunner it wires the gate to are per-match objects "
     "for the same reason."),
    ("ConvertedMatchSetup.cs", "_queueCard.",
     "The card is built once behind an `if (_queueCard != null) return;` guard and is "
     "parented under this screen, so it cannot outlive it and cannot be subscribed twice."),
    # ⚠️ A `GameServices` ROW WAS TRIED AND DELETED. The argument was that a
    # process-lifetime service never needs to let go, which is true and described nothing:
    # it subscribes to no event. The stale-row check below reported the exemption as
    # covering nothing, which is exactly what it is for.
]


def strip_comments(text):
    out, i, n = [], 0, len(text)
    while i < n:
        if text[i] == '"':
            out.append(text[i]); i += 1
            while i < n:
                if text[i] == "\\":
                    out.append("  "); i += 2; continue
                out.append(text[i])
                if text[i] == '"':
                    i += 1; break
                i += 1
            continue
        if text.startswith("//", i):
            j = text.find("\n", i); j = n if j < 0 else j
            out.append(" " * (j - i)); i = j; continue
        if text.startswith("/*", i):
            j = text.find("*/", i); j = n if j < 0 else j + 2
            out.append(" " * (j - i)); i = j; continue
        out.append(text[i]); i += 1
    return "".join(out)


def allowed(path_name, target, handler):
    for f, fragment, _why in KNOWN_SAME_LIFETIME:
        if f == path_name and fragment and fragment in target:
            return True
    return False


def main():
    subs = 0
    findings = []
    allowlist_hits = {f: 0 for f, frag, _w in KNOWN_SAME_LIFETIME if frag}

    for path in sorted(RUNTIME.rglob("*.cs")):
        code = strip_comments(path.read_text(encoding="utf-8", errors="replace"))

        # ⚠️⚠️ THE PAIR IS KEYED ON THE EVENT AND THE HANDLER, NOT ON THE EXPRESSION THAT
        # REACHED THEM, AND THE FIRST VERSION GOT THIS BACKWARDS. `AIController` subscribes
        # through `match.Scored += OnScored` and unsubscribes through
        # `_hookedMatch.Scored -= OnScored`, and that difference is not sloppiness: caching
        # the exact publisher you subscribed to is the CORRECT pattern, because
        # `GameServices.Match` may be a different object by the time you let go. Keying on
        # the whole expression reported all five of that file's correct unsubscribes as
        # leaks, which would have taught the next reader to ignore this audit.
        released = set()
        for m in UNSUBSCRIBE.finditer(code):
            released.add((event_name(m.group(1)), m.group(2).split(".")[-1]))

        for m in SUBSCRIBE.finditer(code):
            target, handler = m.group(1), m.group(2)

            if not is_member(target) or not is_member(handler):
                continue

            subs += 1

            if allowed(path.name, target, handler):
                for f, frag, _w in KNOWN_SAME_LIFETIME:
                    if f == path.name and frag and frag in target:
                        allowlist_hits[f] = allowlist_hits.get(f, 0) + 1
                continue

            if (event_name(target), handler.split(".")[-1]) in released:
                continue

            line = code[:m.start()].count("\n") + 1
            findings.append(f"{path.relative_to(RUNTIME)}:{line}  "
                            f"{target} += {handler}  has no matching -= in this file")

    for f in findings:
        print(f)

    stale = [f for f, hits in allowlist_hits.items() if hits == 0]
    for f in stale:
        print(f"ALLOWLIST STALE: {f} is exempted and no longer subscribes anything. "
              f"Remove the entry, or the exemption is covering nothing and will one day "
              f"cover something new by accident.")

    print()
    print(f"{subs} subscriptions in Runtime/, {len(findings)} with no matching unsubscribe, "
          f"{len(stale)} stale allowlist entr{'y' if len(stale) == 1 else 'ies'}.")

    return 1 if (findings or stale) else 0


if __name__ == "__main__":
    sys.exit(main())
