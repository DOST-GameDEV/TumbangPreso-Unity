#!/usr/bin/env python3
"""
Every shader this project reaches only by name, and whether the player build will still have it.

WHY THIS EXISTS
---------------
`PostAntiAlias.cs` already states the rule it depends on:

    # A SHADER ONLY `Shader.Find` REACHES IS STRIPPED FROM THE PLAYER.

Unity ships a shader if some asset references it — a `.mat`, a prefab, a scene — or if it is
listed in `ProjectSettings/GraphicsSettings.asset` under `m_AlwaysIncludedShaders`. A shader
that is only ever pulled up at runtime by `Shader.Find("Some/Name")` has neither, so it
survives every Editor test and is GONE from the .exe.

That failure is silent and it is the worst shape a bug can have here: `Shader.Find` returns
null, the guard beside it logs a warning nobody reads in a shipped build, and the feature
just is not there. It costs a build, a playthrough and a bisect to find, and the Editor
cannot reproduce it at all.

WHAT IT FOUND THE DAY IT WAS WRITTEN
------------------------------------
Three, on the merge that became HARRYDAKS: `TumbangPreso/WorldOutline` (the entire ink
outline), `TumbangPreso/VolcanicRock` (Ilalim's rock) and `TumbangPreso/ButtonOutline`. The
first two are StarRayX's 2026-08-27 work, whose `.meta` files had been dropped and restored
during that merge; the always-included entry for `Fxaa` came back with them and the other
three had never been added at all.

⚠️ IT IS A REACHABILITY CHECK, NOT A CORRECTNESS ONE. A shader that is listed here is present
in the build; whether it compiles for the target is a different question and one the build
itself answers.
"""

import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ASSETS = os.path.join(ROOT, "Assets")
GRAPHICS = os.path.join(ROOT, "ProjectSettings", "GraphicsSettings.asset")

FIND = re.compile(r'Shader\.Find\(\s*"(?P<name>[^"]+)"')
DECL = re.compile(r'^\s*Shader\s+"(?P<name>[^"]+)"', re.MULTILINE)
GUID = re.compile(r"guid:\s*([a-f0-9]{32})")

# Only this project's own shaders can be stripped this way. Unity's built-ins are either in the
# always-included block by fileID already or are pulled in by the render pipeline asset.
OWNED_PREFIX = "TumbangPreso/"

# Asset kinds that count as a real reference and therefore keep a shader in the build.
REFERENCING = (".mat", ".prefab", ".unity", ".asset")


def walk(root, suffixes):
    for base, _dirs, files in os.walk(root):
        for f in files:
            if f.endswith(suffixes):
                yield os.path.join(base, f)


def main():
    # 1. Every "TumbangPreso/..." name the code looks up at runtime.
    wanted = set()
    for path in walk(ASSETS, (".cs",)):
        with open(path, "r", encoding="utf-8", errors="replace") as fh:
            for m in FIND.finditer(fh.read()):
                if m.group("name").startswith(OWNED_PREFIX):
                    wanted.add(m.group("name"))

    # 2. Map each declared shader name to the guid of its .shader.meta.
    name_to_guid = {}
    for path in walk(ASSETS, (".shader",)):
        with open(path, "r", encoding="utf-8", errors="replace") as fh:
            decl = DECL.search(fh.read())
        if not decl:
            continue
        meta = path + ".meta"
        if not os.path.exists(meta):
            continue
        with open(meta, "r", encoding="utf-8", errors="replace") as fh:
            g = GUID.search(fh.read())
        if g:
            name_to_guid[decl.group("name")] = (g.group(1), path)

    # 3. The always-included block.
    with open(GRAPHICS, "r", encoding="utf-8", errors="replace") as fh:
        graphics = fh.read()
    block = graphics.split("m_AlwaysIncludedShaders:", 1)
    always = set()
    if len(block) == 2:
        always = set(GUID.findall(block[1].split("m_PreloadedShaders:", 1)[0]))

    # 4. Any asset that references a shader guid directly.
    referenced = set()
    for path in walk(ASSETS, REFERENCING):
        with open(path, "r", encoding="utf-8", errors="replace") as fh:
            referenced.update(GUID.findall(fh.read()))

    bad = []
    print("%-36s %-9s %-9s %s" % ("SHADER", "LISTED", "REFERENCED", "VERDICT"))
    for name in sorted(wanted):
        if name not in name_to_guid:
            bad.append((name, "no .shader declares this name"))
            print("%-36s %-9s %-9s %s" % (name, "-", "-", "NOT FOUND"))
            continue
        guid, _path = name_to_guid[name]
        listed = guid in always
        refd = guid in referenced
        ok = listed or refd
        if not ok:
            bad.append((name, "reached only by Shader.Find and nothing keeps it in the build"))
        print("%-36s %-9s %-9s %s" % (name,
                                      "yes" if listed else "no",
                                      "yes" if refd else "no",
                                      "ok" if ok else "STRIPPED"))

    print()
    print("%d shaders looked up by name, %d would be missing from a player build."
          % (len(wanted), len(bad)))
    for name, why in bad:
        print("  %s: %s" % (name, why))

    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())
