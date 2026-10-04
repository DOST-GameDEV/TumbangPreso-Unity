from pathlib import Path
import hashlib,json,re
here=Path('Logs/competition-candidate1003a')
rel='Assets/TumbangPreso/Resources/ShaderWarmup.shadervariants'
before=(here/'warmup-before.shadervariants.txt').read_bytes()
after=Path(rel).read_bytes()
guid=re.search(rb'guid: ([a-f0-9]{32})',Path('Assets/TumbangPreso/Resources/Shaders/BoulderLoad.shader.meta').read_bytes()).group(1)
entry=b'  - first: {fileID: 4800000, guid: '+guid+b', type: 3}\n    second:\n      variants:\n      - keywords: \n        passType: 0\n'
assert before.count(entry)==0 and after.count(entry)==1
assert after.replace(entry,b'')==before,'Unexpected warmup changes beyond exact Boulder pass0'
sha=lambda b:hashlib.sha256(b).hexdigest()
assert sha(before)==json.loads((here/'source-inputs.json').read_text())[rel]
(here/'warmup-after.shadervariants.txt').write_bytes(after)
result=dict(passed=True,path=rel,frozenBeforeSha256=sha(before),currentSha256=sha(after),addedShaderGuid=guid.decode(),addedVariants=1,proof='Removing the exact keyword-free Boulder pass0 entry reproduces retained frozen bytes.')
(here/'warmup-proof.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result))
