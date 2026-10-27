"""Copy an owned completed-capture replay with manifest hashes checked, leaving its profile source intact."""
import argparse
import datetime as dt
import hashlib
import json
from pathlib import Path
import shutil

ROOT=Path(__file__).resolve().parents[1]


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("take")
    parser.add_argument("source",type=Path)
    args=parser.parse_args()
    if not args.take.replace("-","").replace("_","").isalnum():parser.error("Unsafe take name")
    take=ROOT/"Logs/trailer/bring-it-home"/args.take
    job=json.loads((take/"job.json").read_text(encoding="utf-8"))
    if job["status"]!="finished":raise SystemExit("Copy only after the owned capture is terminal")
    capture=json.loads((take/"capture.json").read_text(encoding="utf-8"))
    source=args.source.resolve(strict=True)
    manifest_path=source/"manifest.json"
    manifest=json.loads(manifest_path.read_text(encoding="utf-8-sig"))
    if manifest["Map"]!=capture["map"] or manifest["Mode"]!=capture["mode"]:
        raise SystemExit("Replay map/mode does not match this capture")
    created=dt.datetime.fromisoformat(manifest["CreatedUtc"].replace("Z","+00:00"))
    started=dt.datetime.fromisoformat(job["started_utc"])
    finished=dt.datetime.fromisoformat(job["finished_utc"])
    if not started<=created<=finished:raise SystemExit("Replay was not created during this owned take")
    files={"manifest.json":hashlib.sha256(manifest_path.read_bytes()).hexdigest()}
    for segment in manifest["Segments"]:
        for name_key,hash_key in (("File","Sha256"),("SceneFile","SceneSha256"),("EffectsFile","EffectsSha256"),("SceneryFile","ScenerySha256")):
            name=segment.get(name_key)
            if not name:continue
            path=(source/name).resolve(strict=True)
            if path.parent!=source:raise SystemExit("Replay filename escaped its source folder")
            actual=hashlib.sha256(path.read_bytes()).hexdigest()
            if actual.lower()!=segment[hash_key].lower():raise SystemExit("Replay hash mismatch: "+name)
            files[name]=actual
    destination=take/"replays"/source.name
    destination.mkdir(parents=True,exist_ok=True)
    for name,expected in files.items():
        target=destination/name
        if target.exists() and hashlib.sha256(target.read_bytes()).hexdigest()!=expected:
            raise SystemExit("Different file already exists: "+str(target))
        if not target.exists():shutil.copy2(source/name,target)
        if hashlib.sha256(target.read_bytes()).hexdigest()!=expected:raise SystemExit("Copied hash mismatch: "+name)
    receipt={"files":len(files),"bytes":sum((destination/name).stat().st_size for name in files),
        "destination":str(destination.relative_to(ROOT)),"manifest_sha256":files["manifest.json"],
        "completed_match":manifest["Completed"],"profile_source_preserved":True}
    (take/"replay-transfer.json").write_text(json.dumps(receipt,indent=2),encoding="utf-8")
    print(json.dumps(receipt,indent=2))


if __name__=="__main__":main()
