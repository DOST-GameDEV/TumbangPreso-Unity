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

# ⚠️⚠️ THE ONE SHAPE THAT CAN NEVER BE UNSUBSCRIBED WAS THE ONE SHAPE THIS AUDIT COULD NOT
# SEE, AND IT REPORTED "0 WITH NO MATCHING UNSUBSCRIBE" FOR ITS WHOLE LIFE WHILE THIRTEEN
# OF THEM EXISTED.
#
# `SUBSCRIBE` requires a NAMED handler (`([\w\.]+)\s*;`), because keying the pair on a name
# is what makes the `-=` lookup possible at all. An anonymous delegate has no name, matches
# nothing, and therefore was neither paired nor flagged: it fell out of the count entirely.
#
# ⚠️ AND IT IS EXACTLY BACKWARDS AS A RISK ORDERING. A named handler with no `-=` MIGHT
# leak; an anonymous one **provably cannot be released**, because there is no reference to
# hand to `-=`. So the shape with the strongest guarantee of leaking was the shape with no
# coverage, which is `audit_audio_reach.py`'s fault one file over (`CLAUDE.md` § 7.1: it
# "LIED for its whole life" because it alone did not strip comments) and
# `InputSurfaceProbe`'s argument for discovering rather than listing.
#
# ⚠️ SO THE ONLY LEGAL OUTCOME FOR AN ANONYMOUS SUBSCRIPTION IS A WRITTEN REASON.
# `ANONYMOUS_FOREVER` below carries one per site, and the stale-row check applies to it too:
# an exemption covering nothing is an exemption that will one day cover something new by
# accident.
ANONYMOUS = re.compile(
    r"(?<![+\-*/=!<>])\b([\w\.\[\]]+?)\s*\+=\s*"
    r"(\([^;{=]*?\)\s*=>|\w+\s*=>|delegate\s*\()")

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


# ⚠️⚠️ UNITY'S OWN EVENTS ARE camelCase, SO THE PascalCase RULE ABOVE EXCLUDED THE EXACT
# PUBLISHERS MOST LIKELY TO LEAK. `SceneManager.sceneLoaded`, `SceneManager.sceneUnloaded`
# and `InputSystem.onDeviceChange` are engine statics that live for the whole process, which
# is the worst thing a per-scene or per-match subscriber can attach itself to, and every one
# of them failed `is_member(target)` and was skipped without being counted. Three real
# subscriptions in `MatchAbandon.cs` and `OutlineNormals.cs` were never checked by this audit
# at all; both happen to release correctly, which is luck rather than coverage.
#
# ⚠️ THE WIDENING IS DELIBERATELY NARROW, BECAUSE THE PascalCase RULE IS EARNING ITS KEEP.
# Its own note records the first run reporting 76 findings of which about sixty were
# `_clock += dt`. So a camelCase TARGET is accepted only when it is a member access (it
# contains a dot) AND the handler side is still PascalCase: `SceneManager.sceneLoaded +=
# OnSceneLoaded` passes, and `total += Count`, a real accumulator with a PascalCase
# right-hand side, does not, because it has no dot.
CAMEL_EVENT_TAIL = re.compile(r"(?:^|\.)([a-z]\w*[A-Z]\w*)\s*$")


def is_event_target(expression):
    cleaned = expression.replace("[", " ").replace("]", " ").strip()

    if is_member(cleaned):
        return True

    # ⚠️ camelCase IS ACCEPTED ONLY WHEN IT LOOKS LIKE AN EVENT NAME, AND THE FIRST VERSION
    # OF THIS WIDENING WAS TOO LOOSE. Accepting any target containing a dot let in
    # `palm.y += HandTopLift` from `CharacterVisual`, which is a vector accumulator with a
    # PascalCase constant on the right: exactly the noise the PascalCase rule was written to
    # stop, readmitted through the back door. An engine event name is multi-word camelCase
    # (`sceneLoaded`, `onDeviceChange`, `onAfterUpdate`, `logMessageReceived`), so it always
    # carries an internal capital; a component or a field like `.y`, `.x` or `.magnitude`
    # never does.
    return bool(CAMEL_EVENT_TAIL.search(cleaned))

# ⚠️⚠️ ONE ROW PER ANONYMOUS SUBSCRIPTION, AND THE REASON IS THE POINT OF THE ROW. There is
# no `-=` available for any of these, so "it is released later" is never the answer. Only two
# answers are valid and each row says which it is using:
#
#   (a) THE SUBSCRIBER IS PROCESS-LIFETIME AND SO IS THE PUBLISHER, and the subscriber is
#       constructed exactly once. Nothing accumulates because nothing is ever built twice.
#   (b) THE PUBLISHER CANNOT OUTLIVE THE SUBSCRIBER, which is `KNOWN_SAME_LIFETIME`'s
#       argument: the object raising the event is owned by the object listening to it.
#
# ⚠️ MEASURED 2026-09-05: thirteen sites, all thirteen safe, none of them previously counted.
ANONYMOUS_FOREVER = [
    # -- (a) process-lifetime both ends -------------------------------------------------
    ("AudioDirector.cs", "sceneLoaded",
     "(a) `GameServices.Ensure` opens `if (_root != null) return;` over a DontDestroyOnLoad "
     "+ HideAndDontSave root, so exactly one AudioDirector is constructed per process and "
     "its handler is added once. `SecondMatchLifecycleProbe"
     ".TheAudioDirectorIsBuiltOnceForTheProcessSoItsSceneHandlerCannotAccumulate` asserts "
     "that singleton across a match boundary by IDENTITY, because the root is "
     "HideAndDontSave and FindObjectsByType cannot see it to count it."),
    ("ControllerWatch.cs", "onDeviceChange",
     "(a) A static class whose only subscriber is a "
     "[RuntimeInitializeOnLoadMethod(AfterSceneLoad)] hook, which Unity runs ONCE per "
     "process. The lambda captures no instance, and `InputSystem.onDeviceChange` is itself "
     "process-lifetime, so publisher and subscriber die together at process exit."),
    ("GenericPadBridge.cs", "onDeviceChange",
     "(a) Identical to ControllerWatch above and for the same reason: static class, one "
     "[RuntimeInitializeOnLoadMethod] hook, no captured instance. ⚠️ § 142 OWNS THIS FILE "
     "(`docs/TODO.md`'s queue: controller support has an owner). This row records that the "
     "site was audited and is safe; it is not a licence to edit it."),
    ("FrameCapProbe.cs", "Scored",
     "(a) The probe is a DontDestroyOnLoad object built once from a "
     "[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)] hook that RETURNS UNLESS a `-tp-` "
     "switch is on the command line, so it does not exist at all in a player. Its `Tally` is "
     "constructed once and `Subscribe()` opens `if (_subscribed) return;`."),

    # -- (b) the publisher is owned by the subscriber ------------------------------------
    ("MatchInstaller.cs", "gate.",
     "(b) Same claim KNOWN_SAME_LIFETIME already makes for this file's named handlers: "
     "BuildReadyGate does `gameObject.AddComponent<ReadyGate>()`, so the gate is a component "
     "on the installer's OWN object and the arena unload destroys both in the same frame."),
    ("MatchInstaller.cs", "lata.",
     "(b) The lata is built by this installer (`BuildLata`) and belongs to the arena, so the "
     "scene unload takes the publisher and the subscriber together."),
    ("MatchInstaller.cs", "wheel.",
     "(b) The emote wheel is built by this installer and parented under the match's own "
     "chrome; it cannot outlive the arena that made it."),
    ("ConvertedMatchSetup.cs", "_joinPanel.",
     "(b) The join panel is built by this screen and parented under it, so it dies with the "
     "screen. Same shape as the `_queueCard.` row above."),
    ("ConvertedSettingsPanel.cs", "gate.",
     "(b) The gate is created by this panel and lives under it."),
    ("PlayerHub.cs", "_signIn.",
     "(b) The sign-in screen is built by the hub and parented under it."),
    ("PlayerNameplate.cs", "_hub.",
     "(b) The nameplate is built by the hub it subscribes to and is destroyed with it; a "
     "nameplate that outlived its hub is the § 114 fault, which is a nameplate no screen "
     "installs rather than a stale handler."),
]

KNOWN_SAME_LIFETIME = [
    # (file, target fragment, why the publisher cannot outlive the subscriber)

    # ⚠️⚠️ THESE THREE WERE INVISIBLE UNTIL `is_event_target` LEARNED camelCase, 2026-09-05,
    # AND ALL THREE ARE SAFE. They are the reverse of this list's usual argument: the
    # publisher is an ENGINE STATIC that outlives everything, and what makes that fine is
    # that the subscriber is process-lifetime too. Every one is a static class whose only
    # subscribing method is a [RuntimeInitializeOnLoadMethod] hook, which Unity runs exactly
    # once per process, attaching a static handler. There is no instance to strand and no
    # second attach to accumulate.
    ("FailureBundle.cs", "logMessageReceived",
     "A static class. `Install` is [RuntimeInitializeOnLoadMethod(BeforeSceneLoad)] and "
     "opens `if (_hooked) return; _hooked = true;`, so the handler is added at most once "
     "per process even if the hook were ever run twice. `OnLog` is static."),
    ("LastInputDevice.cs", "onAfterUpdate",
     "A static class whose whole body is `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)] "
     "private static void Install() => InputSystem.onAfterUpdate += Sample;`. One hook, one "
     "process, one static handler."),
    ("GenericPadBridge.cs", "onAfterUpdate",
     "A static class, one [RuntimeInitializeOnLoadMethod(AfterSceneLoad)] hook, `Pump` "
     "static. Its own note explains why it is on `onAfterUpdate` rather than a MonoBehaviour "
     "at all: a pad has to work in the MENUS, and the only component that ticks input every "
     "frame exists on a seat inside a match. ⚠️ § 142 OWNS THIS FILE; this row records that "
     "the site was audited and is safe, and is not a licence to edit it."),
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


def anonymous_reason(path_name, target):
    """The written reason this un-releasable subscription is safe, or None."""
    for f, fragment, why in ANONYMOUS_FOREVER:
        if f == path_name and fragment in target:
            return why
    return None


def main():
    subs = 0
    anon = 0
    findings = []
    allowlist_hits = {f: 0 for f, frag, _w in KNOWN_SAME_LIFETIME if frag}
    anon_hits = {(f, frag): 0 for f, frag, _w in ANONYMOUS_FOREVER}

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

            if not is_event_target(target) or not is_member(handler):
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

        # ⚠️⚠️ THE ANONYMOUS PASS, WHICH `SUBSCRIBE` STRUCTURALLY CANNOT DO. See the note on
        # `ANONYMOUS`: there is no name to pair a `-=` against, so the question is never
        # "was it released" and always "why is it safe never to release it".
        for m in ANONYMOUS.finditer(code):
            target = m.group(1)

            # ⚠️ NO PascalCase FILTER HERE, AND NONE IS NEEDED. That heuristic exists to tell a
            # subscription from an accumulator, and a lambda on the right-hand side already
            # settles it: `_clock += (a, b) => ...` does not compile. Filtering here is what
            # hid `sceneLoaded` and `onDeviceChange`, which are the whole point of this pass.
            anon += 1

            why = anonymous_reason(path.name, target)
            if why is not None:
                for f, frag, _w in ANONYMOUS_FOREVER:
                    if f == path.name and frag in target:
                        anon_hits[(f, frag)] = anon_hits.get((f, frag), 0) + 1
                continue

            line = code[:m.start()].count("\n") + 1
            findings.append(
                f"{path.relative_to(RUNTIME)}:{line}  {target} += <anonymous>  is an "
                f"ANONYMOUS subscription and can never be unsubscribed: there is no "
                f"reference to hand to `-=`. Either give it a named handler and release it, "
                f"or add a row to ANONYMOUS_FOREVER saying why the subscriber and the "
                f"publisher share a lifetime.")

    for f in findings:
        print(f)

    stale = [f for f, hits in allowlist_hits.items() if hits == 0]
    for f in stale:
        print(f"ALLOWLIST STALE: {f} is exempted and no longer subscribes anything. "
              f"Remove the entry, or the exemption is covering nothing and will one day "
              f"cover something new by accident.")

    # ⚠️ THE SAME STALE RULE APPLIES TO THE ANONYMOUS ROWS, for `audit_cue_relay.py`'s
    # reason: an allowlist row asserts that the line making its claim true still exists, so
    # deleting that line fails here rather than going quiet in a match.
    anon_stale = [f"{f} ({frag})" for (f, frag), hits in anon_hits.items() if hits == 0]
    for f in anon_stale:
        print(f"ANONYMOUS ALLOWLIST STALE: {f} is exempted and no longer subscribes "
              f"anonymously. Remove the entry.")

    print()
    print(f"{subs} named subscriptions in Runtime/, {anon} anonymous, "
          f"{len(findings)} finding(s), "
          f"{len(stale) + len(anon_stale)} stale allowlist "
          f"entr{'y' if len(stale) + len(anon_stale) == 1 else 'ies'}.")

    return 1 if (findings or stale or anon_stale) else 0


if __name__ == "__main__":
    sys.exit(main())
