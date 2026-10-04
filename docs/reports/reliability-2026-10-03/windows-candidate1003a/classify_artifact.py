import hashlib,json,re,sys,runpy
from pathlib import Path
HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[1]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
receipt=json.loads((HERE/'build-receipt.json').read_text(encoding='utf-8-sig'))
expected={'Assets/StreamingAssets/build-identity.json','Assets/TumbangPreso/Resources/BuildIdentity.json'}
changed=set(receipt['changedFrozenInputs'])
warmup='Assets/TumbangPreso/Resources/ShaderWarmup.shadervariants'
verifiedWarmup=None
if warmup in changed:
    runpy.run_path(str(HERE/'verify_warmup.py'))
    verifiedWarmup=json.loads((HERE/'warmup-proof.json').read_text())
    if verifiedWarmup['currentSha256']!=sha(ROOT/warmup):raise RuntimeError('Warmup changed after verification')
    changed.remove(warmup)
if changed-expected:raise RuntimeError('Unexpected source drift: '+str(changed-expected))
if receipt['exitCode']!=0 or not receipt['preservationCompleted'] or not receipt['buildMessages']:raise RuntimeError('Build/guard incomplete')
exe=Path(receipt['artifact'])
dat=exe.parent/(exe.stem+'_Data')
paths=[ROOT/p for p in expected]+[dat/'StreamingAssets/build-identity.json']
if len({sha(p) for p in paths})!=1:raise RuntimeError('Generated/packaged identities differ')
identity=json.loads(paths[0].read_text(encoding='utf-8-sig'))
protocol=int(re.search(r'public const int ProtocolVersion = (\d+);',(ROOT/'Assets/TumbangPreso/Runtime/Net/NetSession.cs').read_text(encoding='utf-8-sig')).group(1))
if identity['sha']!=receipt['sourceCommit'] or identity['protocol']!=protocol or identity['target']!='StandaloneWindows64':raise RuntimeError('Identity does not match frozen source/target')
if sha(dat/'Managed/TumbangPreso.Runtime.dll')!=receipt['runtimeSha256']:raise RuntimeError('Runtime changed')
result=dict(receipt,passed=True,unchangedFrozenInputs=receipt['inputCount']-len(changed)-bool(verifiedWarmup),expectedGeneratedChanges=sorted(changed),verifiedWarmupChange=verifiedWarmup,protocol=protocol,generatedIdentitySha256=sha(paths[0]),originalStrictReceiptSha256=sha(HERE/'build-receipt.json'),classification='Identity outputs match source/protocol/target. Only the exact new Boulder pass0 entry may change warmup; removing it must reproduce frozen pre-build bytes and hash. Original strict receipt preserved.')
(HERE/'artifact-receipt.json').write_text(json.dumps(result,indent=2),encoding='utf8')
print(json.dumps({k:v for k,v in result.items() if k!='dirtyBefore'}))
