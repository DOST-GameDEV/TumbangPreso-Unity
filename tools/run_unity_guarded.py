"""Run this checkout's Unity while preserving the player's persistent files (Windows, macOS, Linux).

Editor tests use the real product/company persistentDataPath. UI tests can save
preferences even when their in-memory teardown succeeds. Copy bytes before a run,
restore them afterward and verify hashes. Never remove the user's profile folder.
Use -tp-profile <name> during a concurrent Desktop playtest: only that named
profile is snapshotted/restored, following Runtime/ProfilePaths.cs exactly.
"""
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import uuid
import playerprefs_guard

ROOT=Path(__file__).resolve().parents[1]


def unity_executable(environment=None,platform=None):
    """This checkout's editor on each machine the project is worked on.

    ⚠️ The Linux row is the cloud container (2026-09-26): the editor is unpacked to
    /opt/tump/unity by `tools/cloud_unity_setup.sh`, which also exports UNITY_EDITOR_PATH.
    An explicit UNITY_EDITOR_PATH wins everywhere, so a machine with the editor elsewhere
    needs no edit here.
    """
    environment=os.environ if environment is None else environment
    platform=sys.platform if platform is None else platform
    if environment.get("UNITY_EDITOR_PATH"):return Path(environment["UNITY_EDITOR_PATH"])
    if platform.startswith("win"):return Path(r"C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe")
    if platform=="darwin":return Path("/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/MacOS/Unity")
    return Path("/opt/tump/unity/Editor/Unity")


def player_profile(environment=None,platform=None):
    """Unity's persistentDataPath for company BH Studios, product Tumbang Preso, per OS.

    Windows `%USERPROFILE%/AppData/LocalLow`, macOS `~/Library/Application Support`,
    Linux `$XDG_CONFIG_HOME/unity3d` (default `~/.config/unity3d`), as Unity documents
    `Application.persistentDataPath`. `tools/cold_start.py` resolves the same folders.
    """
    environment=os.environ if environment is None else environment
    platform=sys.platform if platform is None else platform
    if platform.startswith("win"):return Path(environment["USERPROFILE"])/"AppData/LocalLow/BH Studios/Tumbang Preso"
    if platform=="darwin":return Path.home()/"Library/Application Support/BH Studios/Tumbang Preso"
    config=environment.get("XDG_CONFIG_HOME") or str(Path.home()/".config")
    return Path(config)/"unity3d/BH Studios/Tumbang Preso"


def launch_command(unity,args,environment=None,platform=None,has_xvfb=None):
    """The command line, wrapped for a headless Linux box.

    ⚠️⚠️ A cloud container has no display, and PlayMode, films and renders need a real
    graphics device (PlayMode never takes -nographics, CLAUDE.md section 7). Without a
    DISPLAY the editor runs under xvfb-run on Mesa's software OpenGL (llvmpipe); a run
    that passes -nographics needs no display and is left alone. The project's active
    target is Windows and a Linux editor has no Windows module, so a Linux launch that
    names no -buildTarget gets Linux64 (the same standalone code paths, bar the few
    `UNITY_STANDALONE_WIN` blocks).
    """
    environment=os.environ if environment is None else environment
    platform=sys.platform if platform is None else platform
    command=[str(unity),"-projectPath",str(ROOT),*args]
    if not platform.startswith("linux"):return command
    lowered=[a.lower() for a in args]
    if "-buildtarget" not in lowered:command+=["-buildTarget","Linux64"]
    if has_xvfb is None:has_xvfb=shutil.which("xvfb-run") is not None
    if not environment.get("DISPLAY") and "-nographics" not in lowered and has_xvfb:
        command=["xvfb-run","-a","-s","-screen 0 1920x1080x24",*command]
    return command


UNITY=unity_executable()
PROFILE=player_profile()


def digest(path): return hashlib.sha256(path.read_bytes()).hexdigest()


def unity_environment(source=None):
    """Restore the Windows common-profile alias omitted by some task shells.

    Unity 6000.5's package manager joins ALLUSERSPROFILE into a configuration
    path. A missing alias fails before it loads any package, even with a valid
    project and cache. Use this machine's existing ProgramData only in the
    child environment; never replace an explicit value or edit global settings.
    """
    environment=dict(os.environ if source is None else source)
    common=environment.get("PROGRAMDATA") or environment.get("ProgramData")
    if not environment.get("ALLUSERSPROFILE") and common and Path(common).is_dir():
        environment["ALLUSERSPROFILE"]=common
    return environment


def profile_root(args):
    """Mirror ProfilePaths.LaunchProfile/ForProfile; reject ambiguous input."""
    names=[]
    for index,arg in enumerate(args):
        if arg.lower() not in ("-tp-profile","-profile"):continue
        if index+1>=len(args) or not args[index+1].strip() or args[index+1].startswith("-"):
            raise ValueError("A profile flag requires a nonempty profile name")
        names.append(args[index+1].strip())
    if len(names)>1:
        raise ValueError("Use exactly one named profile flag")
    if not names:return PROFILE
    key=hashlib.sha256(names[0].encode("utf-8")).hexdigest()
    return PROFILE/"profiles"/key


def run(args):
    profile=profile_root(args)
    backup=ROOT/"Logs"/("profile-preservation-"+uuid.uuid4().hex[:12])
    backup.mkdir(parents=True)
    manifest={}
    if profile.exists():
        for source in profile.rglob("*"):
            if not source.is_file() or source.suffix==".log" or source.name=="TestResults.xml":continue
            relative=source.relative_to(profile)
            target=backup/relative;target.parent.mkdir(parents=True,exist_ok=True)
            shutil.copy2(source,target)
            manifest[str(relative)]=digest(source)
    (backup/"manifest.json").write_text(json.dumps(manifest,indent=2))
    (backup/"scope.json").write_text(json.dumps({"profileRoot":str(profile)},indent=2))
    editor_prefs=playerprefs_guard.read_editor()
    (backup/"editor-input-prefs.json").write_text(json.dumps(editor_prefs,indent=2))
    result=1
    try:
        result=subprocess.run(launch_command(UNITY,args),cwd=ROOT,
                              env=unity_environment()).returncode
    finally:
        restored=0
        for relative,expected in manifest.items():
            destination=profile/relative
            if not destination.resolve().is_relative_to(profile.resolve()):
                raise ValueError("Profile destination escaped its root")
            destination.parent.mkdir(parents=True,exist_ok=True)
            shutil.copy2(backup/relative,destination)
            if digest(destination)!=expected:raise RuntimeError("Profile restore did not verify")
            restored+=1
        playerprefs_guard.restore_editor(editor_prefs)
        print(f"Preserved {restored} existing profile files in {profile} and {len(editor_prefs)} shared Editor input preferences; snapshot {backup.name}",flush=True)
    return result


if __name__=="__main__":
    args=sys.argv[1:]
    if args and args[0]=="--":args=args[1:]
    raise SystemExit(run(args))
