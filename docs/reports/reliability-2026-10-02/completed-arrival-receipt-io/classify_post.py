import hashlib,json,re
from pathlib import Path
MAIN=Path(__file__).resolve().parents[2]
QUAL=Path(r'C:\Users\matth\Documents\Codex\work\tump-feedback-0930')
OUT=QUAL/'Logs/completed-arrival-receipt-io1002'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
before=json.loads((OUT/'protected-before.json').read_text())
strict=json.loads((OUT/'protected-post.json').read_text())
meta='Assets/TumbangPreso/Runtime/Net/NetCompletedArrivalProbe.cs.meta'
if strict['changed']!=[meta] or meta in before:raise RuntimeError('Unexpected metadata drift')
body=(QUAL/meta).read_text(encoding='utf-8-sig')
if not re.fullmatch(r'fileFormatVersion: 2\nguid: [0-9a-f]{32}\n?',body):raise RuntimeError('Unexpected generated metadata contents')
owned=json.loads((OUT/'post-inputs.json').read_text())
if any(sha(MAIN/rel)!=value or sha(QUAL/rel)!=value for rel,value in owned.items()):raise RuntimeError('Owned source mismatch')
shas={rel:sha(QUAL/rel) for rel in before}
if shas!=before:raise RuntimeError('Originally protected input changed')
result=dict(passed=True,originalProtectedCount=len(before),originalProtectedChanged=[],
    ownedSourceCount=len(owned),newOwnedImportMetadata=meta,generatedMetaSha256=sha(QUAL/meta),
    sourceMetaSha256=sha(MAIN/meta),sourceMetaChanged=False,
    originalStrictPostSha256=sha(OUT/'protected-post.json'),
    classification='One previously absent metadata output generated for our copied observer script. All originally protected inputs unchanged, exact3 MAIN/q source/fixture hashes match. Strict post result retained; no mutation, deletion or native retry.')
(OUT/'artifact-post.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result))
