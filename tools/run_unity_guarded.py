"""Run this checkout's Unity while preserving the Windows player's persistent files.

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
UNITY=Path(r"C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe")
PROFILE=Path(os.environ["USERPROFILE"])/"AppData/LocalLow/BH Studios/Tumbang Preso"


def digest(path): return hashlib.sha256(path.read_bytes()).hexdigest()


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
        result=subprocess.run([str(UNITY),"-projectPath",str(ROOT),*args],cwd=ROOT).returncode
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
