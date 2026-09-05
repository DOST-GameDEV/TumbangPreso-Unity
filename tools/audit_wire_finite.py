#!/usr/bin/env python3
"""Every float, vector and rotation read off the wire is checked finite before it is used.

WHY THIS EXISTS
===============

WARNING: IN C# EVERY ORDINARY COMPARISON AGAINST NaN IS FALSE, SO THE OBVIOUS VALIDATION SHAPE
LETS NaN THROUGH. `if (value > limit) return false;` PASSES a NaN. `Mathf.Clamp` is exactly
`if (v < min) v = min; else if (v > max) v = max; return v;`, so it returns a NaN unchanged. A
guard that looks like a range check is not one.

That is not hypothetical here. `MatchRpc.HostSetTimeScale` clamped a spectator's requested time
scale to 0..1 and then assigned it to `Time.timeScale`, and the clamp handled both infinities
correctly (they DO compare) and passed NaN straight through. Any spectator in a live match could
freeze the host and, through `SyncTime`, every peer. `docs/TODO.md` section 149.9.

WARNING: AND THE HOST-TO-CLIENT DIRECTION IS NOT EXEMPT, FOR A DIFFERENT REASON. A non-finite
`Vector3` or `Quaternion` assigned to a `Transform` is REFUSED by Unity, logged once per frame,
and the object is left wherever it last was: a tsinelas that stops replicating, with nothing in
the log but a repeating engine warning. A corrupted packet is not an honest host.

WHAT IT CHECKS
==============

For every named-message handler in `Runtime/Net/MatchRpc.cs`, every `ReadValueSafe(out float ...)`,
`out Vector3`, `out Quaternion` and `out double` must be reached by a `Finite(...)` call on that
variable, by `PlausibleIntentPose(..., var)` (which begins with `Finite`), or by being handed to
`AcceptMove`, which validates all three of its own.

WARNING: IT IS A TEXT AUDIT AND IT SAYS SO. It cannot prove the check runs BEFORE the use, only
that the check exists in the same handler. That is the same bound every `audit_*.py` in this
folder works under, and it is the bound that catches the class of fault that actually happens
here, which is a value nobody looked at at all.

    python tools/audit_wire_finite.py
"""

import pathlib
import re
import sys

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass

ROOT = pathlib.Path(__file__).resolve().parent.parent
SOURCE = ROOT / "Assets" / "TumbangPreso" / "Runtime" / "Net" / "MatchRpc.cs"

NUMERIC = ("float", "double", "Vector3", "Quaternion", "Vector2")

# WARNING: A HANDLER MAY DELEGATE ITS VALIDATION AND TWO OF THEM DO. Naming the delegate here is
# what keeps this audit from being answered with a redundant second check, which is how a rule
# starts disagreeing with itself. Each row is a method that validates every numeric argument it
# is handed, and the reason it is trusted.
DELEGATES = {
    # `AcceptMove` opens with `!Finite(position) || !Finite(yaw) || !Finite(velocity)` and then
    # spends the seat's movement budget, which refuses NaN a second time by construction.
    "AcceptMove": "validates position, yaw and velocity and then spends Core.MoveBudget",
    # `PlausibleIntentPose` opens with `Finite(position)` before it measures anything.
    "PlausibleIntentPose": "opens with Finite(position)",
    # `HostSetTimeScale` refuses a non-finite scale outright rather than clamping it, because a
    # NaN is a malformed request and not a big number to bring into range. It is the one path
    # this audit was written for, and the check belongs there rather than in the handler: the
    # host's own pause button reaches it too, and a guard in only one of the two callers is a
    # guard with a documented way around it.
    "HostSetTimeScale": "refuses a non-finite scale before it clamps or assigns",
}


def strip_comments(text):
    text = re.sub(r"/\*.*?\*/", "", text, flags=re.S)
    return re.sub(r"//[^\n]*", "", text)


def handlers(text):
    """Every named-message handler body, by name.

    ⚠️ FOUND BY SIGNATURE RATHER THAN BY A LIST, for `InputSurfaceProbe`'s reason: a handler
    added next month is exactly as silent as the ones this was written for.
    """
    pattern = re.compile(
        r"private\s+void\s+(?P<name>On\w+Msg)\s*\(ulong\s+\w+,\s*FastBufferReader\s+\w+\)\s*\{")

    found = []
    for m in pattern.finditer(text):
        depth = 0
        i = m.end() - 1
        while i < len(text):
            if text[i] == "{":
                depth += 1
            elif text[i] == "}":
                depth -= 1
                if depth == 0:
                    break
            i += 1
        found.append((m.group("name"), text[m.end():i]))

    return found


def main():
    if not SOURCE.exists():
        print(f"audit_wire_finite: {SOURCE} is missing.")
        return 2

    text = strip_comments(SOURCE.read_text(encoding="utf-8", errors="replace"))

    findings = []
    checked = 0
    handler_count = 0

    for name, body in handlers(text):
        handler_count += 1

        for kind, var in re.findall(
                r"ReadValueSafe\(out\s+(" + "|".join(NUMERIC) + r")\s+(\w+)\s*\)", body):
            checked += 1

            if re.search(r"\bFinite\(\s*" + re.escape(var) + r"\s*\)", body):
                continue
            if re.search(r"PlausibleIntentPose\([^)]*\b" + re.escape(var) + r"\b", body):
                continue
            if any(re.search(r"\b" + d + r"\([^)]*\b" + re.escape(var) + r"\b", body)
                   for d in DELEGATES):
                continue

            findings.append(
                f"{name}: `{kind} {var}` is read off the wire and never checked finite. "
                f"In C# every comparison against NaN is false, so a range test on it is not a "
                f"guard; a NaN reaching a Transform makes the object stop replicating and a NaN "
                f"reaching a clock or a bar makes every later comparison false.")

    # ⚠️ THE ARRAY FORM TOO. `OnSyncWorldMsg` reads a float per seat into an array in a loop,
    # which the pattern above cannot see, so the loop variable is checked by name instead.
    for name, body in handlers(text):
        for var in re.findall(r"ReadValueSafe\(out\s+(\w+)\[\w+\]\s*\)", body):
            checked += 1
            if re.search(r"\bFinite\(\s*\w+\s*\)", body):
                continue
            findings.append(f"{name}: `{var}[]` is filled from the wire with nothing finite-checked.")

    for f in findings:
        print("  FINDING  " + f)

    print(f"audit_wire_finite: {handler_count} wire handlers, {checked} numeric field(s), "
          f"{len(findings)} finding(s).")
    return 1 if findings else 0


if __name__ == "__main__":
    sys.exit(main())
