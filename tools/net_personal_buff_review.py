"""Rebuild an observer's local kit and request authoritative remaining buff state.

Three actual players and ordinary durations; intentionally not a process-restart
claim. Short Veil windows need a live snapshot fixture instead of slower boot time.
"""
import argparse
import csv
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import time

from run_unity_guarded import profile_root

ROOT = Path(__file__).resolve().parents[1]


def rows(path):
    with path.open(newline='') as handle:
        return [{key: float(value) for key, value in row.items()} for row in csv.DictReader(handle)]


def evaluate(folder, case):
    data = {name: rows(folder / (name + '.csv')) for name in ('host', 'owner', 'observer')}
    errors, measurements = [], {}
    host = data['host']
    live_host = [row for row in host if row['active']]
    if not live_host:
        return {'ok': False, 'errors': ['Host never accepted the real cast'], 'measurements': {}}
    start = live_host[0]['wallTime']
    expiry = next((row['wallTime'] for row in host if row['wallTime'] > start and not row['active']), None)
    if expiry is None:
        return {'ok': False, 'errors': ['Host did not expire the buff'], 'measurements': {}}
    is_veil = case in ('veil', 'fade')
    slow = .65 if case == 'fade' else .7 if case == 'plating' else 1
    for name, trace in data.items():
        expected_seat = {'host': 0, 'owner': 1, 'observer': 2}[name]
        if len(trace) < 80 or any(row['local'] != expected_seat for row in trace):
            errors.append(name + ' lacks continuous seat evidence'); continue
        after = trace
        if name == 'observer':
            after = [row for row in trace if row['refreshed']]
            if not after:
                errors.append('Observer never replaced its local kit'); continue
            restored = next((row['wallTime'] for row in after if row['active']), None)
            ready = after[0]['wallTime']
            if restored is None or restored - ready > .3:
                errors.append('Observer did not receive live state within the direct reply budget')
            begin = ready + .3
        else:
            ready, restored, begin = None, None, start + .4
        live = [row for row in after if begin < row['wallTime'] < expiry - .3]
        result = {'samples': len(trace), 'live_samples': len(live),
                  'active_samples': sum(row['active'] > 0 for row in live),
                  'visual_samples': sum(row['visuals'] == 1 for row in live),
                  'restore_delay': restored - ready if restored is not None else None}
        measurements[name] = result
        if len(live) < 5 or any(not row['active'] or row['visuals'] != 1 for row in live):
            errors.append(name + ' lost or duplicated active presentation')
        # CharacterMotor intentionally refuses speed-stack mutation for a remote
        # body. The observer follows host transforms; host and controlling owner
        # must carry the grant. This also held before observer reconstruction.
        expected_slow = 1 if name == 'observer' else slow
        result['expected_speed_stack'] = expected_slow
        if any(abs(row['slow'] - expected_slow) > .001 for row in live):
            errors.append(name + ' changed or stacked the selected movement grant')
        if any(row['variant'] != int(case in ('fade', 'plating')) for row in live):
            errors.append(name + ' lost its selected sidegrade')
        if any(row['immune'] != (int(not row['held']) if is_veil else 1) for row in live):
            errors.append(name + ' disagrees with the real protection/carry rule')
        if not is_veil and any(row['orbit'] != 3 for row in live):
            errors.append(name + ' lost its three protectors')
        first = next((row['wallTime'] for row in after if row['active']), None)
        ended = next((row['wallTime'] for row in after if first is not None
                      and row['wallTime'] > first and not row['active']), None)
        result['expiry_offset'] = ended - expiry if ended is not None else None
        if ended is None or abs(ended - expiry) > .5:
            errors.append(name + ' replayed or shortened the remaining duration')
        final = trace[-1]
        if final['active'] or final['visuals'] or final['orbit'] or abs(final['slow']-1) > .001:
            errors.append(name + ' leaked protection, visuals or slowdown')
        if final['cooldown'] <= 30 or final['ultcharge'] != host[-1]['ultcharge']:
            errors.append(name + ' reset the spent cooldown or ultimate bank')
    return {'ok': not errors, 'errors': errors, 'measurements': measurements}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('exe', type=Path)
    parser.add_argument('--case', choices=['ward', 'plating', 'veil', 'fade'], required=True)
    parser.add_argument('--out', type=Path, required=True)
    args = parser.parse_args()
    folder = args.out.resolve(); folder.mkdir(parents=True, exist_ok=False)
    backups, processes, handles = [], [], []
    for name in ('personalhost', 'personalowner', 'personalobserver'):
        profile = profile_root(['-tp-profile', name])
        for source in profile.rglob('*'):
            if source.is_file() and source.suffix != '.log':
                target = folder / 'profiles' / name / source.relative_to(profile)
                target.parent.mkdir(parents=True, exist_ok=True); shutil.copy2(source, target)
                backups.append((source, target, hashlib.sha256(source.read_bytes()).hexdigest()))
    startup = None
    if os.name == 'nt':
        startup = subprocess.STARTUPINFO(); startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW; startup.wShowWindow = 0
    def launch(command, output=subprocess.DEVNULL):
        process = subprocess.Popen(command, cwd=ROOT, stdout=output, stderr=subprocess.STDOUT, startupinfo=startup)
        processes.append(process); return process
    def peer(name, route):
        return launch([str(args.exe.resolve()), '-batchmode', '-screen-width', '640', '-screen-height', '360',
                       '-screen-fullscreen', '0', '-tp-framecap', '60', '-tp-autostart', '3',
                       '-tp-profile', 'personal'+name, '-tp-personalcase', args.case,
                       '-tp-personaltrace', str(folder / (name+'.csv')), '-logFile', str(folder / (name+'.log'))]+route)
    def wait_seat(name, process, seat):
        deadline = time.monotonic()+45
        while time.monotonic() < deadline:
            path = folder / (name+'.log')
            log = path.read_text(errors='replace') if path.exists() else ''
            if re.search(r'(?:seat changed|arena installed): LocalSlot='+str(seat)+r'[^\n]*host=', log):
                return
            if process.poll() is not None: raise RuntimeError(name+' exited before arena readiness')
            time.sleep(.25)
        raise RuntimeError(name+' did not become ready')
    try:
        host = peer('host', ['-tp-host', '8970']); wait_seat('host', host, 0)
        log = (folder / 'link.log').open('w'); handles.append(log)
        launch([sys.executable, str(ROOT/'tools/net_link.py'), '--listen', '8971', '--to', '127.0.0.1:8970',
                '--delay', '150', '--seconds', '100'], log)
        time.sleep(.5)
        owner = peer('owner', ['-tp-join', '127.0.0.1', '8971']); wait_seat('owner', owner, 1)
        peer('observer', ['-tp-join', '127.0.0.1', '8970'])
        print('Tracing '+args.case+' with a refreshed observer kit: '+str(folder), flush=True)
        host.wait(timeout=80); time.sleep(.5)
        result = evaluate(folder, args.case)
        runtime = args.exe.parent / (args.exe.stem+'_Data') / 'Managed/TumbangPreso.Runtime.dll'
        result['runtime_sha256'] = hashlib.sha256(runtime.read_bytes()).hexdigest()
        result['owner_link_one_way_ms'] = 150
        result['scope'] = 'Three native peers; observer kit reconstruction and explicit world snapshot, not process restart'
        (folder/'result.json').write_text(json.dumps(result, indent=2)+'\n')
        print(json.dumps(result, indent=2), flush=True)
        return 0 if result['ok'] else 1
    finally:
        for process in processes:
            if process.poll() is None: process.terminate()
        for process in processes:
            try: process.wait(timeout=8)
            except subprocess.TimeoutExpired: process.kill(); process.wait()
        for handle in handles: handle.close()
        for source, backup, expected in backups:
            source.parent.mkdir(parents=True, exist_ok=True); shutil.copy2(backup, source)
            if hashlib.sha256(source.read_bytes()).hexdigest() != expected:
                raise RuntimeError('Named profile restoration failed')
        print('Preserved '+str(len(backups))+' named-profile files', flush=True)


if __name__ == '__main__':
    raise SystemExit(main())
