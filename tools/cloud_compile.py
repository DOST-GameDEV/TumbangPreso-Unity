"""COMPILE THE GAME WITHOUT THE UNITY EDITOR (cloud sessions, no licence, no Windows).

⚠️ WHY: a cloud box cannot run Unity, and C# written blind lands on the owner's PC with errors he
has to find. This compiles every assembly the project builds (the packages it depends on, then
TumbangPreso.Core, .Runtime and, with --editor, .Editor, .Tests and .PlayTests) with Roslyn
against Unity 6000.5.8f1's own reference DLLs. It proves the code COMPILES. It does not import
assets, run IL post-processing, or run a single test: EditMode, PlayMode and captures still need
Unity on the PC.

One-time setup (about 4.3 GB streamed, about 2 GB kept):
    curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 9.0 --install-dir /root/.dotnet
    mkdir -p /opt/unityref && cd /opt/unityref && curl -sSL \\
      https://download.unity3d.com/download_unity/5cb7df797b7d/LinuxEditorInstaller/Unity.tar.xz | tar -xJ \\
      --wildcards 'Editor/Data/Managed/*' 'Editor/Data/NetStandard/*' \\
      'Editor/Data/Resources/PackageManager/BuiltInPackages/*'
    then every registry package in Packages/packages-lock.json from
      https://packages.unity.com/<name>/-/<name>-<version>.tgz into /opt/unitypkgs/<name>
Run:
    python3 tools/cloud_compile.py                 # the player runtime
    python3 tools/cloud_compile.py TumbangPreso.Editor TumbangPreso.Tests TumbangPreso.PlayTests --editor
Package assemblies compile in player mode; the project's own assemblies get UNITY_EDITOR under
--editor, which is how the editor compiles them.
"""
import json, os, glob, subprocess, sys, re

ROOT = "/home/user/TumbangPreso-Unity"
UNITY = "/opt/unityref/Editor/Data"
PKGS = "/opt/unitypkgs"
BUILTIN = UNITY + "/Resources/PackageManager/BuiltInPackages"
OUT = "/opt/unitybuild-editor" if "--editor" in sys.argv else "/opt/unitybuild"
EDITOR = "--editor" in sys.argv
CSC = glob.glob("/root/.dotnet/sdk/*/Roslyn/bincore/csc.dll")[0]
os.makedirs(OUT, exist_ok=True)

DEFINES = ["UNITY_6000_5_OR_NEWER", "UNITY_6000_4_OR_NEWER", "UNITY_6000_3_OR_NEWER", "UNITY_6000_2_OR_NEWER",
           "UNITY_6000_1_OR_NEWER", "UNITY_6000_0_OR_NEWER", "UNITY_6000", "UNITY_6000_5", "UNITY_2023_1_OR_NEWER",
           "UNITY_2022_3_OR_NEWER", "UNITY_2022_2_OR_NEWER", "UNITY_2022_1_OR_NEWER", "UNITY_2021_3_OR_NEWER",
           "UNITY_2021_2_OR_NEWER", "UNITY_2021_1_OR_NEWER", "UNITY_2020_3_OR_NEWER", "UNITY_2020_2_OR_NEWER",
           "UNITY_2020_1_OR_NEWER", "UNITY_2019_4_OR_NEWER", "UNITY_2019_3_OR_NEWER", "UNITY_2019_1_OR_NEWER",
           "UNITY_2018_3_OR_NEWER", "UNITY_STANDALONE", "UNITY_STANDALONE_WIN", "PLATFORM_STANDALONE",
           "PLATFORM_STANDALONE_WIN", "UNITY_64", "ENABLE_MONO", "NET_STANDARD_2_1", "NET_STANDARD", "CSHARP_7_3_OR_NEWER",
           "ENABLE_INPUT_SYSTEM", "UNITY_POST_PROCESSING_STACK_V2", "ENABLE_UNITY_COLLECTIONS_CHECKS",
           "UNITY_ASSERTIONS", "DEVELOPMENT_BUILD", "UNITY_INCLUDE_TESTS", "UNITY_UGP_API", "ENABLE_UNET"]


# every asmdef we could compile
asm = {}
roots = [ROOT + "/Packages/com.tumbangpreso.core", ROOT + "/Packages/com.unity.transport", ROOT + "/Assets"]
INSTALLED = set(json.load(open(ROOT + "/Packages/packages-lock.json"))["dependencies"].keys())
roots += glob.glob(PKGS + "/*") + [b for b in glob.glob(BUILTIN + "/*") if os.path.basename(b) in INSTALLED]
versions = {}
for r in roots:
    pj = os.path.join(r, "package.json")
    if os.path.exists(pj):
        try:
            d = json.load(open(pj)); versions[d["name"]] = d.get("version", "0")
        except Exception:
            pass
    for f in glob.glob(r + "/**/*.asmdef", recursive=True):
        if "~" in f or "/Tests" in f and "TumbangPreso" not in f:
            continue
        try:
            d = json.load(open(f))
        except Exception:
            continue
        name = d["name"]
        if name in asm and not f.startswith(ROOT):
            continue
        asm[name] = (f, d)
for f in glob.glob(UNITY + "/Managed/UnityEngine/*.dll"):
    pass

refs_base = [UNITY + "/NetStandard/ref/2.1.0/netstandard.dll"] + glob.glob(UNITY + "/Managed/UnityEngine/UnityEngine*.dll")
refs_base = [r for r in refs_base if "UnityEditor" not in os.path.basename(r)]
refs_base.append(UNITY + "/Managed/UnityEngine/Unity.Scripting.dll")
SHIMS = glob.glob(UNITY + "/NetStandard/compat/2.1.0/shims/netstandard/*.dll") + glob.glob(UNITY + "/NetStandard/compat/2.1.0/shims/netfx/*.dll")
refs_base += SHIMS
PLUGINS = [d for d in glob.glob(PKGS + "/**/*.dll", recursive=True) + [x for b in INSTALLED for x in glob.glob(BUILTIN + "/" + b + "/**/*.dll", recursive=True)]
           if "/Editor/" not in d.replace(BUILTIN, "").replace(PKGS, "") and "/Tests" not in d and "~" not in d and "Unity.Burst" not in d and "Cecil" not in d]
if "--editor" in sys.argv:
    refs_base += glob.glob(UNITY + "/Managed/UnityEngine/UnityEditor*.dll") + [UNITY + "/Managed/UnityEngine/UnityEditor.Graphs.dll"]
    refs_base = [r for r in refs_base if os.path.exists(r)]

guid_to_name = {}
for name, (f, d) in asm.items():
    m = os.path.exists(f + ".meta") and re.search(r"guid:\s*(\w+)", open(f + ".meta").read())
    if m:
        guid_to_name[m.group(1)] = name

built = {}
INSTALLED = set(json.load(open(ROOT + "/Packages/packages-lock.json"))["dependencies"].keys())


def ref_name(r):
    return guid_to_name.get(r[5:], r) if r.startswith("GUID:") else r


def compile_asm(name, editor=False):
    if name in built:
        return built[name]
    if name not in asm:
        dll = next((d for d in PLUGINS if os.path.basename(d) == name + ".dll"), None)
        built[name] = dll
        return dll
    f, d = asm[name]
    plats = d.get("includePlatforms") or []
    if plats and "Editor" in plats and not (editor and f.startswith(ROOT + "/Assets") or editor and "TestRunner" in name):
        built[name] = None
        return None
    folder = os.path.dirname(f)
    refs = []
    for r in d.get("references", []):
        dll = compile_asm(ref_name(r), editor)
        if dll:
            refs.append(dll)
    # sources: this folder, minus sub folders owned by another asmdef/asmref
    others = [os.path.dirname(o) for o in glob.glob(folder + "/**/*.asmdef", recursive=True) if os.path.dirname(o) != folder]
    srcs = [s for s in glob.glob(folder + "/**/*.cs", recursive=True)
            if "~" not in s and not any(s.startswith(o + "/") for o in others)]
    precompiled = []
    for p in d.get("precompiledReferences", []) or []:
        precompiled += glob.glob(os.path.dirname(folder) + "/**/" + p, recursive=True)[:1] or glob.glob(PKGS + "/**/" + p, recursive=True)[:1] or glob.glob(BUILTIN + "/**/" + p, recursive=True)[:1]
    pkgdlls = glob.glob(folder + "/**/*.dll", recursive=True)
    if not d.get("overrideReferences"):
        pkgdlls += PLUGINS
    defs = [x for x in DEFINES if x != "UNITY_INCLUDE_TESTS" or "com.unity.inputsystem" not in f]
    project = f.startswith(ROOT + "/Assets")
    if EDITOR and project:
        defs += ["UNITY_EDITOR", "UNITY_EDITOR_WIN", "UNITY_EDITOR_64"]
    for vd in d.get("versionDefines", []) or []:
        if vd.get("name", "") in INSTALLED or vd.get("name", "") == "Unity":
            defs.append(vd["define"])
    if any(c not in defs and not c.startswith("!") for c in d.get("defineConstraints", []) or []):
        built[name] = None
        return None
    for vd in d.get("versionDefines", []) or []:
        n = vd.get("name", "")
        if n in INSTALLED or n == "Unity":
            defs.append(vd["define"])
    if not d.get("noEngineReferences"):
        eng = refs_base
    else:
        eng = [UNITY + "/NetStandard/ref/2.1.0/netstandard.dll"] + SHIMS
    out = os.path.join(OUT, name + ".dll")
    rsp = os.path.join(OUT, name + ".rsp")
    lines = ["-target:library", "-nologo", "-nostdlib+", "-langversion:9.0", "-deterministic", "-nowarn:1701,1702,0618,0649,0169,0414,0067,0105,0219,0162,0168,1998,0612,0414,8321",
             "-out:" + out, "-define:" + ";".join(defs)]
    if d.get("allowUnsafeCode"):
        lines.append("-unsafe")
    for r in eng + refs + precompiled + pkgdlls:
        lines.append('-r:"%s"' % r)
    # every compiled dependency's dependencies too (Unity passes the closure)
    for n2, v in list(built.items()):
        if v and v not in refs:
            lines.append('-r:"%s"' % v)
    rspf = os.path.join(folder, "csc.rsp")
    if os.path.exists(rspf):
        lines += [l.strip() for l in open(rspf) if l.strip()]
    lines += ['"%s"' % s for s in srcs]
    open(rsp, "w").write("\n".join(lines))
    r = subprocess.run(["/root/.dotnet/dotnet", CSC, "@" + rsp], capture_output=True, text=True)
    errs = [l for l in r.stdout.splitlines() if ": error " in l]
    if r.returncode != 0:
        print(f"[{name}] FAILED, {len(errs)} errors")
        for l in errs[:int(os.environ.get('SHOW', '40'))]:
            print("   ", l.replace(ROOT + "/", ""))
        built[name] = out if os.path.exists(out) else None
        return built[name]
    print(f"[{name}] ok ({len(srcs)} files)")
    built[name] = out
    return out


targets = [a for a in sys.argv[1:] if not a.startswith("--")] or ["TumbangPreso.Runtime"]
targets = ["UnityEngine.UI", "Unity.Burst", "Unity.Mathematics", "Unity.Collections"] + targets
for t in targets:
    compile_asm(t, editor="--editor" in sys.argv)
