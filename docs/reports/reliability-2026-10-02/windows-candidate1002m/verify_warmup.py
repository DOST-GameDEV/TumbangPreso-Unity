import hashlib,json,re
from pathlib import Path
here=Path(__file__).resolve().parent
root=here.parents[1]
rel='Assets/TumbangPreso/Resources/ShaderWarmup.shadervariants'
data=(root/rel).read_bytes()
guid=re.search(rb'guid: ([a-f0-9]{32})',(root/'Assets/TumbangPreso/Resources/Shaders/FrostbiteLoad.shader.meta').read_bytes()).group(1)
entry=b'  - first: {fileID: 4800000, guid: '+guid+b', type: 3}\n    second:\n      variants:\n      - keywords: \n        passType: 0\n'
assert data.count(entry)==1,'Expected exactly one keyword-free Frostbite shader variant'
prior=data.replace(entry,b'')
expected=json.loads((here/'source-inputs.json').read_text())[rel]
assert hashlib.sha256(prior).hexdigest()==expected,'Warmup changed beyond the one new Frostbite shader variant'
(here/'warmup-before-reconstructed.shadervariants.txt').write_bytes(prior)
(here/'warmup-after.shadervariants.txt').write_bytes(data)
result=dict(passed=True,path=rel,frozenBeforeSha256=expected,currentSha256=hashlib.sha256(data).hexdigest(),addedShaderGuid=guid.decode(),addedVariants=1,proof='Removing the exact new Frostbite keyword-free pass0 entry reproduces the frozen pre-build bytes and SHA256.')
(here/'warmup-proof.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result))
