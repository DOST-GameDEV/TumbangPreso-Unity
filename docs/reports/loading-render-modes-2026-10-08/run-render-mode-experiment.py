from pathlib import Path
import sys, hashlib, json

root = Path(__file__).resolve().parents[2]
mode = sys.argv[1]
assert mode in ('baseline', 'direct', 'baseline-repeat'), mode
run_name = 'render-mode-' + mode + '-1008'
expected_runtime = '2005a5d0ac9bb5c4573e77a46eb0bc115f700f49e22d05afa06891702c242a20'
runtime = root / 'Builds/release-ui-perf1008g/TumbangPreso_Data/Managed/TumbangPreso.Runtime.dll'
assert hashlib.sha256(runtime.read_bytes()).hexdigest() == expected_runtime
template = Path(__file__).with_name('run-batched-release-performance.py').read_text()
assert template.count("kind='batched-release-menu-performance'") == 1
template = template.replace("kind='batched-release-menu-performance'", 'kind=' + repr(run_name))
anchor = "receipt={'sourceCommit':identity['sha']"
assert template.count(anchor) == 1
variant = "\ncommand += ['-force-gfx-direct']\n" if mode == 'direct' else '\n'
template = template.replace(anchor, variant + anchor)
exec(compile(template, str(__file__) + ':' + mode, 'exec'))
