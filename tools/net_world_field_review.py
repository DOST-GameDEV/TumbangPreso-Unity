"""Compare the full persistent world snapshot across three native players."""
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


def evaluate(folder):
    data={name:rows(folder/(name+'.csv')) for name in ('host','owner','observer')}
    errors=[];measurements={};host=data['host']
    expected={kind:next((row for row in host if row['kind']==kind),None) for kind in range(1,8)}
    if any(value is None for value in expected.values()):
        return {'ok':False,'errors':['Host did not create all seven field types'],'measurements':{}}
    for name,trace in data.items():
        seat={'host':0,'owner':1,'observer':2}[name]
        if len(trace)<100 or any(row['local']!=seat for row in trace):
            errors.append(name+' lacks continuous seat evidence');continue
        seen={int(row['kind']) for row in trace if row['kind']>0}
        result={'samples':len(trace),'kinds':sorted(seen),'max_fields':max(row['count'] for row in trace),
                'final_fields':trace[-1]['count'],'largest_expiry_offset':0}
        measurements[name]=result
        if seen!=set(range(1,8)) or result['max_fields']!=7 or result['final_fields']!=0:
            errors.append(name+' omitted, duplicated or leaked fields')
        if name!='host' and not any(row['repeated'] and row['count']==7 for row in trace):
            errors.append(name+' did not repeat a complete live snapshot')
        for kind,reference in expected.items():
            samples=[row for row in trace if row['kind']==kind]
            if len(samples)<5:errors.append(name+' lacks live samples for kind '+str(kind));continue
            expected_end=reference['wallTime']+reference['remaining']
            for row in samples:
                for key in ('x','y','z','fx','fy','fz','duration','radius','owner','first','second','split'):
                    if abs(row[key]-reference[key])>.025:
                        errors.append(name+' changed kind '+str(kind)+' '+key);break
                offset=abs(row['wallTime']+row['remaining']-expected_end)
                result['largest_expiry_offset']=max(result['largest_expiry_offset'],offset)
            if result['largest_expiry_offset']>.55:
                errors.append(name+' restarted or shortened the remaining field lifetime');break
        if not (folder/(name+'.png')).exists():errors.append(name+' missing original world overview')
    return {'ok':not errors,'errors':list(dict.fromkeys(errors)),'measurements':measurements}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('exe', type=Path)
    parser.add_argument('--out', type=Path, required=True)
    args = parser.parse_args()
    folder = args.out.resolve(); folder.mkdir(parents=True, exist_ok=False)
    backups, processes, handles = [], [], []
    for name in ('worldfieldhost', 'worldfieldowner', 'worldfieldobserver'):
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
                       '-tp-profile', 'worldfield'+name, '-tp-worldfieldtrace', str(folder / (name+'.csv')), '-logFile', str(folder / (name+'.log'))]+route)
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
        host = peer('host', ['-tp-host', '8960']); wait_seat('host', host, 0)
        log = (folder / 'link.log').open('w'); handles.append(log)
        launch([sys.executable, str(ROOT/'tools/net_link.py'), '--listen', '8961', '--to', '127.0.0.1:8960',
                '--delay', '150', '--seconds', '100'], log)
        time.sleep(.5)
        owner = peer('owner', ['-tp-join', '127.0.0.1', '8961']); wait_seat('owner', owner, 1)
        peer('observer', ['-tp-join', '127.0.0.1', '8960'])
        print('Tracing seven persistent field kinds: '+str(folder), flush=True)
        host.wait(timeout=80); time.sleep(.5)
        result = evaluate(folder)
        runtime = args.exe.parent / (args.exe.stem+'_Data') / 'Managed/TumbangPreso.Runtime.dll'
        result['runtime_sha256'] = hashlib.sha256(runtime.read_bytes()).hexdigest()
        result['owner_link_one_way_ms'] = 150
        result['scope'] = 'Host-created field fixture; two real clients request and repeat complete snapshots, not skill-cast or process-restart proof'
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
