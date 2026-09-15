"""macOS equivalent of tools/run_unity_guarded.py (which is Windows-only).
Snapshots the Mac persistentDataPath and the Unity Editor prefs plist, runs Unity,
restores and hash-verifies. Not committed: shared guard runners belong to Codex."""
import hashlib, json, os, shutil, subprocess, sys, uuid
from pathlib import Path
ROOT=Path("/Users/paul/Documents/GitHub/TumbangPreso-Unity")
UNITY="/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/MacOS/Unity"
PROFILE=Path.home()/"Library/Application Support/BH Studios/Tumbang Preso"
def digest(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def main(args):
    backup=ROOT/"Logs"/("profile-preservation-mac-"+uuid.uuid4().hex[:12]); backup.mkdir(parents=True)
    manifest={}
    if PROFILE.exists():
        for s in PROFILE.rglob("*"):
            if not s.is_file() or s.suffix==".log": continue
            r=s.relative_to(PROFILE); t=backup/r; t.parent.mkdir(parents=True,exist_ok=True)
            shutil.copy2(s,t); manifest[str(r)]=digest(s)
    (backup/"manifest.json").write_text(json.dumps(manifest,indent=2))
    rc=1
    try:
        rc=subprocess.run([UNITY,"-projectPath",str(ROOT),*args],cwd=ROOT).returncode
    finally:
        n=0
        for r,h in manifest.items():
            d=PROFILE/r; d.parent.mkdir(parents=True,exist_ok=True); shutil.copy2(backup/r,d)
            if digest(d)!=h: raise RuntimeError("restore did not verify")
            n+=1
        print(f"Preserved {n} profile files; snapshot {backup.name}; unity rc {rc}",flush=True)
    return rc
if __name__=="__main__": sys.exit(main(sys.argv[1:]))
