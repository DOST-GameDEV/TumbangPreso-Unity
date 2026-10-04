import hashlib,json,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=Path(__file__).resolve().parent
EXE=ROOT/'Builds/competition-candidate1002k/TumbangPreso.exe'
def sha(path):
    h=hashlib.sha256()
    with path.open('rb') as f:
        for chunk in iter(lambda:f.read(1048576),b''): h.update(chunk)
    return h.hexdigest()
def git(*args): return subprocess.check_output(['git',*args],cwd=ROOT,text=True).strip()
def manifest():
    return {p.relative_to(ROOT).as_posix():sha(p) for base in ('Assets','Packages','ProjectSettings') for p in (ROOT/base).rglob('*') if p.is_file()}
if sys.argv[1]=='prepare':
    if EXE.exists(): raise RuntimeError('Candidate player already exists')
    dirty=git('status','--short').splitlines()
    if any(line.strip().endswith('.cs') for line in dirty): raise RuntimeError('Release C# is dirty')
    inputs=manifest()
    (OUT/'source-inputs.json').write_text(json.dumps(inputs,indent=2),encoding='utf8')
    plan={'sourceCommit':git('rev-parse','HEAD'),'inputCount':len(inputs),'dirtyBefore':dirty,'artifact':str(EXE),'profile':'competition-candidate1002k','scope':'Frozen committed source plus retained importer/generated deltas; not pristine post-import.'}
    (OUT/'build-plan.json').write_text(json.dumps(plan,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in plan.items() if k!='dirtyBefore'}))
else:
    plan=json.loads((OUT/'build-plan.json').read_text())
    original=json.loads((OUT/'source-inputs.json').read_text())
    current=manifest()
    changed=[p for p in original.keys()|current.keys() if original.get(p)!=current.get(p)]
    job=json.loads((OUT/'job-receipt.json').read_text())
    messages=[line for line in (OUT/'unity.log').read_text(encoding='utf8',errors='replace').splitlines() if '[Build] SUCCEEDED.' in line]
    dll=EXE.parent/(EXE.stem+'_Data')/'Managed/TumbangPreso.Runtime.dll'
    passed=not changed and job['exitCode']==0 and job['preservationCompleted'] and not job['leaseHeld'] and bool(messages) and dll.is_file() and git('rev-parse','HEAD')==plan['sourceCommit']
    receipt=dict(plan,passed=passed,changedFrozenInputs=changed,poolStatus=job['status'],exitCode=job['exitCode'],preservationCompleted=job['preservationCompleted'],buildMessages=messages,runtimeSha256=sha(dll) if dll.exists() else None)
    (OUT/'build-receipt.json').write_text(json.dumps(receipt,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in receipt.items() if k not in ('dirtyBefore',)}))
    sys.exit(0 if passed else 1)