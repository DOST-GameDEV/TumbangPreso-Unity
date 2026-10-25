#!/usr/bin/env python3
"""Inventory every tracked Markdown file; check concise live entry points and links.

Run --list to locate topic documents without loading their contents. Archives and
dated evidence retain original wording/links; vendor docs retain upstream content.
"""
import argparse
from collections import Counter
from pathlib import Path
import re
import subprocess
from urllib.parse import unquote

ROOT = Path(__file__).resolve().parents[1]
LIVE = {
    'AGENTS.md': 6500, 'CLAUDE.md': 1000, 'README.md': 4000,
    'docs/README.md': 4500, 'docs/TODO.md': 6000,
    'docs/ACTIVE_REWORK_LEDGER.md': 2400, 'docs/UnityProjectContext.md': 5000,
    'docs/VISION.md': 4000, 'docs/NETWORKING.md': 6500,
    'docs/SKILL_NETWORK_CONTRACT.md': 8000, 'docs/REFERENCE_INDEX.md': 5000,
    'docs/WORKING_RULES.md': 14000, 'docs/TESTING.md': 14000,
    'docs/WORKSTATION_SETUP.md': 7000, 'docs/RETIRED_WORK.md': 5000,
    'docs/SPECTATOR.md': 5000,
}


def role(path):
    if path.startswith('Packages/com.unity.'):
        return 'vendor'
    if path.startswith('docs/archive/'):
        return 'history'
    if path.startswith('docs/reports/'):
        return 'evidence'
    if path in LIVE:
        return 'live'
    return 'reference'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--list', action='store_true', help='List Markdown paths by role')
    args = parser.parse_args()
    names = subprocess.check_output(
        ['git', 'ls-files', '--cached', '--others', '--exclude-standard', '-z'],
        cwd=ROOT).decode('utf-8').split('\0')
    paths = sorted({p for p in names if p.lower().endswith('.md') and (ROOT/p).is_file()})
    counts = Counter(role(p) for p in paths)
    print(f'Markdown: {len(paths)} files; ' + ', '.join(f'{k}={v}' for k, v in sorted(counts.items())))
    if args.list:
        for path in paths:
            print(f'{role(path):9} {path}')
    failures = []
    total = 0
    for name, limit in LIVE.items():
        path = ROOT/name
        if not path.is_file():
            failures.append(f'{name}: missing live document')
            continue
        raw = path.read_bytes()
        total += len(raw)
        if len(raw) > limit:
            failures.append(f'{name}: {len(raw)} bytes exceeds {limit}; move history out of the entry point')
        body = raw.decode('utf-8-sig')
        for match in re.finditer(r'\]\(([^)]+)\)', body):
            target = match[1].strip().strip('<>').split('#')[0]
            if not target or re.match(r'^[a-z]+:', target, re.I):
                continue
            if not (path.parent/unquote(target)).resolve().exists():
                failures.append(f'{name}: missing local link {target}')
        if re.search(r'(?:pull origin/ASTRAReworks|push ASTRAReworks|Work/integrate on ASTRAReworks|Never edit/merge/push main)', body):
            failures.append(f'{name}: expired branch command')
    print(f'Live guides: {total:,} bytes. Startup documents are a subset; load topic guides on demand.')
    for failure in failures:
        print(f'ERROR: {failure}')
    print(f'Documentation checks: {len(failures)} failure(s)')
    return bool(failures)


if __name__ == '__main__':
    raise SystemExit(main())
