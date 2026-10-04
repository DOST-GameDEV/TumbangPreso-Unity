"""Package exact terminal evidence; never launch players or overwrite an earlier report."""
import hashlib
import json
from pathlib import Path
import shutil

REPORT = Path(__file__).resolve().parent
MAIN = REPORT.parents[3]
SOURCE = Path('C:/Users/matth/Documents/Codex/work/tump-competition-release1002/Logs/host-loss-real-peer1002j-first').resolve()
DEST = (REPORT / 'first').resolve()
assert DEST.is_relative_to(REPORT.resolve()) and SOURCE.name == 'host-loss-real-peer1002j-first'
result = json.loads((SOURCE / 'result.json').read_text())
assert result['passed'] and not result['errors']
assert all(result[key] for key in ('inputRestored', 'profileSeedsRestored', 'runtimeUnchanged', 'allOwnedProcessesRetired', 'leaseReleased'))
DEST.mkdir(exist_ok=False)
names = ('client-before-kill.json', 'client.json', 'client.log', 'client.txt', 'host-before-kill.json',
         'host.json', 'host.log', 'player-input-before.json', 'profile-seeds.json', 'result.json')
hashes = {}
for name in names:
    source, target = (SOURCE / name).resolve(), (DEST / name).resolve()
    assert source.is_relative_to(SOURCE) and target.is_relative_to(DEST)
    shutil.copy2(source, target)
    expected = hashlib.sha256(source.read_bytes()).hexdigest()
    assert hashlib.sha256(target.read_bytes()).hexdigest() == expected
    hashes['first/' + name] = expected
for relative in ('tools/run_host_loss.py', 'tools/test_run_host_loss.py'):
    hashes[relative] = hashlib.sha256((MAIN / relative).read_bytes()).hexdigest()
(REPORT / 'evidence-hashes.json').write_text(json.dumps(hashes, indent=2), encoding='utf-8')
print(json.dumps(dict(copied=len(names), hashes=hashes), indent=2))
