"""Which peers actually SEE and HEAR each piece of match feedback.

The successor to `audit_audio_reach.py`, and the reason it exists is that that
script came back CLEAN while eight sounds and a whole popup were host-only.

  audit_audio_reach.py asks: is a `ShouldResolve()` early return open at this
  call's own brace depth?

That finds a gate written in the same method and nothing else. Every fault
found on 2026-08-29 had the gate ONE OR TWO FRAMES UP THE STACK:

    Slipper.FixedUpdate      -> if (!NetAuthority.ShouldResolve()) return;
      Slipper.Land           -> plays "slipper_land"          <- invisible to it
    Lata.HostKnockDown       -> if (!NetAuthority.ShouldResolve()) return;
      Lata.SetUpright        -> plays "can_knockdown", spawns TUMBA!, punches
                                the camera, throws confetti   <- invisible to it

So this one propagates. A method is host-only when it is GATED itself, or when
every call site of it in the runtime is inside a method that is host-only. It
iterates to a fixed point, so a chain of any length is followed.

It also covers the VISUAL half. 🧑 2026-08-29: *"make sure that all host sided
shit is seen by everyone and not js host"* -- a popup nobody but the host can
see is the same defect as a sound nobody but the host can hear, and the can's
knockdown had both in the same method.

Usage:

    PYTHONIOENCODING=utf-8 python tools/audit_presentation_reach.py

Exit code is 1 when anything is host-only, so it can gate a build.
"""
import os
import re
import sys

ROOT = "Assets/TumbangPreso/Runtime"

# What counts as presentation: something a player is meant to see or hear.
# The key is the label printed; the value is the pattern that spots a call.
FEEDBACK = {
    "sound": re.compile(r"GameServices\.Audio\??\.\w+\("),
    "voice": re.compile(r"GameServices\.Voice\??\.\w+\("),
    "popup": re.compile(r"ComicPopup\.\w+\("),
    "burst": re.compile(r"ImpactBurst\.\w+\("),
    "hitfeel": re.compile(r"HitFeel\.\w+\("),
    "hitmarker": re.compile(r"Hud\.TriggerHitmarker\("),
    "style": re.compile(r"Hud\.ReportStyle\("),
    "stars": re.compile(r"DizzyStars\.\w+\("),
    "shake": re.compile(r"\.ImpactPunch\("),
}

# Calls that are ALREADY the fix. A relayed cue is correct by construction and
# must not be reported, or the audit cries wolf about its own remedy.
EXEMPT_LINE = re.compile(r"\bNetCue\.")

# Files that are the plumbing rather than the game. Being host-side is the
# whole point of these, and reporting them is noise that hides a real row.
EXEMPT_FILES = {
    "Assets/TumbangPreso/Runtime/Audio/NetCue.cs",
    "Assets/TumbangPreso/Runtime/Net/MatchRpc.cs",
}

GATE = re.compile(r"(ShouldResolve\(\)|NetAuthority\.IsHost)")
SIG = re.compile(
    r"^(?:public|private|protected|internal)\s+"
    r"(?:static\s+|sealed\s+|override\s+|virtual\s+|async\s+|unsafe\s+)*"
    r"[\w<>\[\],\.\?]+\s+(\w+)\s*\("
)


def strip_comment(line):
    s = line.strip()
    return "" if s.startswith("//") or s.startswith("*") or s.startswith("/*") else line


def scan():
    """Returns (methods, calls).

    methods[(path, name)] = {"gated": bool, "line": int}
    calls = [(path, line, kind, method_name, raw)]
    """
    methods = {}
    calls = []

    for dirpath, _, filenames in os.walk(ROOT):
        for fn in sorted(filenames):
            if not fn.endswith(".cs"):
                continue

            path = os.path.join(dirpath, fn).replace(os.sep, "/")
            text = open(path, encoding="utf-8", errors="replace").read()
            lines = text.split("\n")

            depth = 0
            current = None          # (name, depth_at_signature)
            opened = False          # has the body's opening brace been seen yet
            gates = []              # brace depths at which a gate return is open

            for i, raw in enumerate(lines):
                line = strip_comment(raw)

                m = SIG.match(line.strip())
                if m:
                    # ⚠️ A METHOD IS NOT CLOSED UNTIL ITS BODY HAS OPENED. The signature and
                    # the `{` are on separate lines in this codebase, so `depth` at the
                    # signature is still the CLASS depth. Closing on `depth <= sigDepth`
                    # without waiting for the body ends every method on the line after its
                    # own signature, which attributes nothing to anything -- the first
                    # version of this file did exactly that and cheerfully reported zero.
                    current = (m.group(1), depth)
                    opened = False
                    methods.setdefault((path, m.group(1)),
                                       {"gated": False, "line": i + 1, "gateLine": None})

                # ⚠️⚠️ A GATE ONLY COVERS WHAT COMES AFTER IT, AND IGNORING THAT COST A FALSE
                # POSITIVE ON THE FIRST CLEAN RUN. `HeroHazards.CreateExplosion` draws its
                # picture and THEN gates, deliberately — its own note says the order is the
                # point — so a whole-method flag reported the one call that is already correct.
                #
                # ⚠️ AND IT ONLY COUNTS AT THE BODY'S OWN DEPTH. A `ShouldResolve()` return
                # inside a loop or an `if` guards that block, not the rest of the method; taking
                # it as a method-wide gate is how the deliberate
                # `if (ShouldResolve() || p.PlayerSlot == LocalSlot)` split reads as a refusal.
                if current and GATE.search(line) and "return" in line:
                    gates.append(depth)
                    if depth == current[1] + 1:
                        m2 = methods[(path, current[0])]
                        if m2["gateLine"] is None:
                            m2["gateLine"] = i + 1
                        m2["gated"] = True

                if path not in EXEMPT_FILES and not EXEMPT_LINE.search(line):
                    for kind, pattern in FEEDBACK.items():
                        if pattern.search(line):
                            calls.append((path, i + 1, kind,
                                          current[0] if current else "?",
                                          line.strip()[:70]))

                opens = line.count("{")
                depth += opens - line.count("}")
                gates = [g for g in gates if g <= depth]

                if current:
                    if not opened and depth > current[1]:
                        opened = True
                    elif opened and depth <= current[1]:
                        current = None
                    elif not opened and ";" in line and "=>" in line:
                        # An expression-bodied member has no block to leave.
                        current = None

    return methods, calls


# ⚠️⚠️ THE ENGINE CALLS THESE, SO THEY ALWAYS HAVE A CALLER THIS TOOL CANNOT SEE. Without the
# list, one `x.Update(dt)` written inside a host-only method made EVERY `Update` in the project
# host-only, because edges are keyed by name. The first run of the fixed propagation reported 23
# hazards that are spawned on every peer and tick on every peer.
UNITY_MESSAGES = {
    "Awake", "OnEnable", "Start", "FixedUpdate", "Update", "LateUpdate",
    "OnDisable", "OnDestroy", "OnApplicationQuit", "OnApplicationFocus",
    "OnApplicationPause", "OnGUI", "OnValidate", "Reset",
    "OnTriggerEnter", "OnTriggerStay", "OnTriggerExit",
    "OnCollisionEnter", "OnCollisionStay", "OnCollisionExit",
    "OnDrawGizmos", "OnDrawGizmosSelected", "OnBecameVisible", "OnBecameInvisible",
}


def callers(methods):
    """Every method called from inside every method, WITHIN THE SAME FILE.

    ⚠️⚠️ SAME FILE, AND THAT IS A DELIBERATE UNDER-APPROXIMATION RATHER THAN LAZINESS. An edge
    keyed on a bare name collides across types the moment two classes both have a `Play`, a
    `Land` or a `Shatter` — and they do. Every fault this tool was written to catch was a
    private helper called from a gated method a few lines above it in the same file
    (`Slipper.FixedUpdate` -> `Land`, `Lata.HostKnockDown` -> `SetUpright`), so restricting to
    one file finds all of them and invents none.

    The cost is that a genuinely cross-file host-only chain is missed. That is the right way to
    be wrong: a missed row is one more report from him, a false row is a day spent "fixing"
    something that already works, and the second kind is what makes a tool get switched off.
    """
    edges = {}   # (path, callee name) -> set of (path, caller name)

    for dirpath, _, filenames in os.walk(ROOT):
        for fn in sorted(filenames):
            if not fn.endswith(".cs"):
                continue
            path = os.path.join(dirpath, fn).replace(os.sep, "/")
            lines = open(path, encoding="utf-8", errors="replace").read().split("\n")

            depth = 0
            current = None
            opened = False
            for i, raw in enumerate(lines):
                line = strip_comment(raw)
                m = SIG.match(line.strip())
                if m:
                    current = (m.group(1), depth)
                    opened = False
                elif current:
                    for call in re.findall(r"(?<![\w.])(\w+)\s*\(", line):
                        if call == current[0]:
                            continue
                        edges.setdefault((path, call), set()).add(
                            (path, current[0], i + 1))

                depth += line.count("{") - line.count("}")

                # ⚠️ SAME "WAIT FOR THE BODY" RULE AS `scan`. See the note there.
                if current:
                    if not opened and depth > current[1]:
                        opened = True
                    elif opened and depth <= current[1]:
                        current = None
                    elif not opened and ";" in line and "=>" in line:
                        current = None

    return edges


def propagate(methods, edges):
    """A method is host-only if it is gated, or every caller of it is host-only.

    ⚠️ A METHOD NOBODY CALLS IS NOT HOST-ONLY. It is an entry point (Update,
    Awake, an event handler, a public API), and "every caller is host-only" is
    vacuously true of an empty set. Treating that as host-only would mark the
    whole runtime.
    """
    reached = {k: v["gated"] for k, v in methods.items()}

    for _ in range(12):
        changed = False
        for (path, name), gated in list(reached.items()):
            if gated or name in UNITY_MESSAGES:
                continue
            incoming = edges.get((path, name), set())
            if not incoming:
                continue

            # ⚠️ A CALL MADE BEFORE ITS CALLER'S GATE IS NOT BEHIND IT. See the note in `scan`.
            def behind(edge):
                cp, cn, cline = edge
                if not reached.get((cp, cn), False):
                    return False
                gl = methods.get((cp, cn), {}).get("gateLine")
                return gl is None or cline > gl

            if all(behind(e) for e in incoming):
                reached[(path, name)] = True
                changed = True
        if not changed:
            break

    return reached


def main():
    methods, calls = scan()
    reached = propagate(methods, callers(methods))

    bad, ok = [], []
    for path, line, kind, method, raw in calls:
        host_only = reached.get((path, method), False)

        # ⚠️ AND THE SAME RULE FOR A DIRECT CALL: a line above its own method's gate runs on
        # every peer, which is what `CreateExplosion` does on purpose.
        gl = methods.get((path, method), {}).get("gateLine")
        if host_only and gl is not None and line < gl:
            host_only = False

        (bad if host_only else ok).append((path, line, kind, method, raw))

    for path, line, kind, method, raw in sorted(bad):
        print("HOST-ONLY  %-9s %s:%d  %s()\n           %s" % (kind, path, line, method, raw))

    print()
    print("%d presentation call sites, %d reachable by every peer, %d HOST-ONLY"
          % (len(calls), len(ok), len(bad)))

    if bad:
        print()
        print("Each of these is something one player out of four sees or hears.")
        print("The fix is NetCue for a world sound, or splitting the presentation")
        print("out so a client can run it from the state it is told about. See")
        print("Lata.AnnounceUprightChange and docs/TODO.md section 83.12.")

    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())
