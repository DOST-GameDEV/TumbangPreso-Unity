"""Run this checkout's Unity while preserving the Windows player's persistent files.

Editor tests use the real product/company persistentDataPath. UI tests can save
preferences even when their in-memory teardown succeeds. Copy bytes before a run,
restore them afterward and verify hashes. Never remove the user's profile folder.
"""
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import uuid

ROOT=Path(__file__).resolve().parents[1]
UNITY=Path(r"C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe")
PROFILE=Path(os.environ["USERPROFILE"])/"AppData/LocalLow/BH Studios/Tumbang Preso"


def digest(path): return hashlib.sha256(path.read_bytes()).hexdigest()


def run(args):
    backup=ROOT/"Logs"/("profile-preservation-"+uuid.uuid4().hex[:12])
    backup.mkdir(parents=True)
    manifest={}
    if PROFILE.exists():
        for source in PROFILE.rglob("*"):
            if not source.is_file() or source.suffix==".log" or source.name=="TestResults.xml":continue
            relative=source.relative_to(PROFILE)
            target=backup/relative;target.parent.mkdir(parents=True,exist_ok=True)
            shutil.copy2(source,target)
            manifest[str(relative)]=digest(source)
    (backup/"manifest.json").write_text(json.dumps(manifest,indent=2))
    result=1
    try:
        result=subprocess.run([str(UNITY),"-projectPath",str(ROOT),*args],cwd=ROOT).returncode
    finally:
        restored=0
        for relative,expected in manifest.items():
            destination=PROFILE/relative
            if not destination.resolve().is_relative_to(PROFILE.resolve()):
                raise ValueError("Profile destination escaped its root")
            destination.parent.mkdir(parents=True,exist_ok=True)
            shutil.copy2(backup/relative,destination)
            if digest(destination)!=expected:raise RuntimeError("Profile restore did not verify")
            restored+=1
        print(f"Preserved {restored} existing profile files; snapshot {backup.name}",flush=True)
    return result


if __name__=="__main__":
    args=sys.argv[1:]
    if args and args[0]=="--":args=args[1:]
    raise SystemExit(run(args))
