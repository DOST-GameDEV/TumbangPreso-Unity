"""Three current Rafi peers: retrieval, Baha and retirement. Water mode is a historical harness."""
import argparse
import hashlib
import json
import math
import os
import sys
import statistics
import xml.etree.ElementTree as ET
from pathlib import Path
import shutil
import socket
import subprocess
import time

from presentation_peer_link import PresentationLink, arguments as link_arguments
from run_unity_guarded import profile_root, unity_environment
if os.name == 'nt':
    from run_ui_player_review import read_input_preferences
else:
    def read_input_preferences():
        from run_unity_guarded import PROFILE
        import playerprefs_guard
        prefs = PROFILE / 'prefs'
        if not prefs.exists(): return {}
        root = ET.parse(prefs).getroot()
        return {node.attrib.get('name'): ET.tostring(node, encoding='unicode') for node in root
                if playerprefs_guard.allowed(node.attrib.get('name', ''))}

ROOT = Path(__file__).resolve().parents[1]


def evaluate(out, case="water"):
    data = {name: [json.loads(line) for line in (out / (name + '.jsonl')).read_text().splitlines()]
            for name in ('host', 'owner', 'observer')}
    if case == 'backwash': return evaluate_backwash(data)
    if case == 'integration': return evaluate_integration(data)
    errors, measured = [], {}
    reference = {}
    for row in data['host']:
        for field in row['fields']:
            reference.setdefault(field['id'], field)
    if len(reference) != 3 or {f['kind'] for f in reference.values()} != {8, 9, 10}:
        errors.append('Host did not create exactly the three accepted Rafi effects.')
    epoch = data['host'][0]['epoch'] if data['host'] else 0
    if epoch <= 0:
        errors.append('No live host epoch.')
    def vector_error(a, b):
        return math.dist([a[k] for k in ('x', 'y', 'z')], [b[k] for k in ('x', 'y', 'z')])
    for seat, (name, rows) in enumerate(data.items()):
        if len(rows) < 100 or any(r['local'] != seat or r['epoch'] != epoch for r in rows):
            errors.append(name + ': missing/wrong seat or epoch trace')
        observed = {}
        for row in rows:
            fields = row['fields']
            if len({f['id'] for f in fields}) != len(fields):
                errors.append(name + ': duplicate active field')
            for field in fields:
                observed.setdefault(field['id'], []).append((row, field))
                baseline = reference.get(field['id'])
                if baseline is None:
                    errors.append(name + ': field absent from host'); continue
                if any(field[k] != baseline[k] for k in ('kind', 'owner', 'duration')):
                    errors.append(name + ': changed field identity/lifetime')
                for key in ('position', 'forward'):
                    if vector_error(field[key], baseline[key]) > .001:
                        errors.append(name + ': changed field ' + key)
                if len(field['path']) != len(baseline['path']) or any(
                        vector_error(a, b) > .001 for a, b in zip(field['path'], baseline['path'])):
                    errors.append(name + ': changed bounded water path')
        if set(observed) != set(reference) or any(len(samples) < 3 for samples in observed.values()):
            errors.append(name + ': missing live field samples')
        if not rows or rows[-1]['fields'] or rows[-1]['q'] != 1 or rows[-1]['e'] != 1 or rows[-1]['starts'] != 1 or abs(rows[-1]['ultimate']) > .001:
            errors.append(name + ': leak, wrong fee or duplicated/missing ultimate')
        clips = {row.get('bodyClip', '') for row in rows}
        if not {'hero-rafi-cut', 'hero-rafi-feint', 'hero-rafi-breakwater'}.issubset(clips):
            errors.append(name + ': missing actual authored Rafi body playback')
        measured[name] = {'rows': len(rows), 'eventIds': sorted(observed), 'starts': rows[-1]['starts'] if rows else 0,
                          'bodyClips': sorted(clips),
                          'snapshots': rows[-1]['snapshots'] if rows else 0, 'largestExpiryOffset': 0}
        for event, samples in observed.items():
            host_samples = [(r, f) for r in data['host'] for f in r['fields'] if f['id'] == event]
            if not host_samples: continue
            host_end = host_samples[0][0]['wall'] + host_samples[0][1]['remaining']
            offset = max(abs(r['wall'] + f['remaining'] - host_end) for r, f in samples)
            measured[name]['largestExpiryOffset'] = max(measured[name]['largestExpiryOffset'], offset)
            if offset > .6: errors.append(name + ': field lifetime restarted or diverged')
    if measured['host']['snapshots'] != 6:
        errors.append('Missing repeated normal world snapshots during the three live effects.')
    return errors, measured


def evaluate_backwash(data):
    errors, measured = [], {}
    epoch = data['host'][0]['epoch'] if data['host'] else 0
    for seat, (name, rows) in enumerate(data.items()):
        if len(rows)<100 or epoch<=0 or any(r['local']!=seat or r['epoch']!=epoch or r['defending'] for r in rows):
            errors.append(name+': missing continuous attacker identity');continue
        active=[r for r in rows if r['passive']>0]
        regrab=[r for r in active if r['regrabbed']]
        measured[name]={'rows':len(rows),'activeSamples':len(active),'regrabSamples':len(regrab),'finalPassive':rows[-1]['passive']}
        if len(active)<10 or not regrab:errors.append(name+': retrieval and drop/regrab window not exercised')
        if any(r['passive']>1.51 or abs(r['movementScale']-1.2)>.001 for r in active):errors.append(name+': invalid duration or speed scale')
        if any(b['passive']>a['passive']+.1 for a,b in zip(active,active[1:])):errors.append(name+': drop/regrab refreshed the boost')
        if any(r['fields'] or r['starts'] for r in rows):errors.append(name+': unrelated skill activated')
        if rows[-1]['passive'] or rows[-1]['movementScale']!=1:errors.append(name+': passive did not retire')
        if name=='owner':
            ordinary=[r['travelSpeed'] for r in rows if 10.2<r['elapsed']<10.7 and r['travelSpeed']>1 and r['passive']==0]
            boosted=[r['travelSpeed'] for r in regrab if r['travelSpeed']>1]
            if len(ordinary)<3 or len(boosted)<3:errors.append('Owner lacks stable normal/boosted travel samples')
            else:
                ratio=statistics.median(boosted)/statistics.median(ordinary)
                measured[name].update(normalSpeed=statistics.median(ordinary),boostedSpeed=statistics.median(boosted),measuredRatio=ratio)
                if not 1.16<ratio<1.24:errors.append('Actual owner travel is not1.2x')
    if all(data.values()):
        endpoints=[r[-1]['casterPosition'] for r in data.values()]
        spread=max(math.dist([a[k] for k in ('x','y','z')],[b[k] for k in ('x','y','z')]) for a in endpoints for b in endpoints)
        measured['finalPositionSpread']=spread
        if spread>.15:errors.append('Host/observer did not adopt final movement')
    return errors, measured


def evaluate_integration(data):
    # New coherent Baha integration, including the retained retrieval passive.
    # The two old standalone false receipts remain false and are never rewritten.
    early={name:[row for row in rows if row['elapsed']<17] for name,rows in data.items()}
    errors, passive=evaluate_backwash(early)
    measured={'backwash':passive,'baha':{}};identities=set()
    for name,rows in data.items():
        active=[(r,f) for r in rows for f in r['fields'] if f['kind']==18]
        ids={f['id'] for _,f in active};identities|=ids
        warning=[(r,f) for r,f in active if f['duration']-f['remaining']<.8]
        before=next((r for r in rows if 17.5<r['elapsed']<18),None)
        result={'fieldIds':sorted(ids),'activeSamples':len(active),'warningSamples':len(warning),
                'maxTargetVelocity':max(r['targetVelocity'] for r in rows if r['elapsed']>=18),
                'finalUltimate':rows[-1]['ultimate'],'starts':rows[-1]['starts'],'snapshots':rows[-1]['snapshots']}
        measured['baha'][name]=result
        if len(ids)!=1 or len(active)<15 or len(warning)<2:errors.append(name+': missing one truthful Baha warning/front')
        if any(len(r['fields'])>1 or any(f['kind']!=18 for f in r['fields']) for r in rows):errors.append(name+': unrelated or duplicate field')
        if before is None:errors.append(name+': missing staged counterplay origin');continue
        carry=rows[-1]['loosePosition']['z']-before['loosePosition']['z']
        nudge=rows[-1]['targetPosition']['z']-before['targetPosition']['z']
        result.update(looseCarry=carry,targetTravel=nudge)
        if not 2.5<carry<=3.02:errors.append(name+': loose slipper did not retain its bounded3m carry')
        if not .1<nudge<2:errors.append(name+': rival did not receive one bounded nudge')
        if name=='observer' and result['maxTargetVelocity']<1:errors.append('Actual target owner missed resolved impulse')
        if any(not r['canUpright'] for r in rows if r['elapsed']>=17):errors.append(name+': Baha changed the can')
        if rows[-1]['fields'] or rows[-1]['ultimate']!=0 or rows[-1]['starts']!=1:errors.append(name+': field, fee or presentation did not retire once')
        if any(abs(f['duration']-(.8+f['range']/5+.6))>.01 or not 0<=f['range']<=64 for _,f in active):errors.append(name+': invalid bound or lifetime')
    if len(identities)!=1:errors.append('Peers disagreed on Baha identity')
    if data['host'] and data['host'][-1]['snapshots']!=2:errors.append('Repeated scoped Baha world snapshots were not exercised')
    return errors,measured


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('exe', type=Path); parser.add_argument('--out', type=Path, required=True)
    parser.add_argument('--case', choices=('water','backwash','integration'), default='integration')
    link_arguments(parser); args = parser.parse_args()
    exe, out = args.exe.resolve(), args.out.resolve()
    if not exe.is_file() or not exe.is_relative_to(ROOT / 'Builds'):
        raise SystemExit('Use an internal Builds player.')
    if not out.is_relative_to(ROOT / 'Logs') or out == ROOT / 'Logs':
        raise SystemExit('Use a fresh Logs output directory.')
    out.mkdir(parents=True, exist_ok=False)
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as test:
        test.bind(('127.0.0.1', 0)); port = test.getsockname()[1]
    processes, backups, commands = [], [], []
    before = read_input_preferences(); link = PresentationLink(ROOT, out, args)
    startup = None
    if os.name == 'nt':
        startup = subprocess.STARTUPINFO(); startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW; startup.wShowWindow = 0
    try:
        join_port = link.start(port)
        for seat, name in enumerate(('host', 'owner', 'observer')):
            profile_name = out.name + '-' + name; profile = profile_root(['-tp-profile', profile_name])
            for source in profile.rglob('*'):
                if source.is_file() and source.suffix != '.log':
                    backup = out / 'private-profiles' / name / source.relative_to(profile)
                    backup.parent.mkdir(parents=True, exist_ok=True); shutil.copy2(source, backup)
                    backups.append((source, backup, hashlib.sha256(source.read_bytes()).hexdigest()))
            command = [str(exe), '-batchmode', '-screen-width', ('640' if args.case!='water' else '800'), '-screen-height', ('360' if args.case!='water' else '450'), '-screen-fullscreen', '0',
                       '-tp-framecap', ('60' if args.case!='water' else '30'), '-tp-autostart', '3', '-tp-map', ('Eskinita' if args.case!='water' else 'Lagoon'), '-tp-profile', profile_name,
                       '-tp-raficase', args.case, '-tp-rafitrace', str(out / (name + '.jsonl')), '-logFile', str(out / (name + '.log'))]
            if sys.platform.startswith('linux'): command += ['-force-glcore']
            command += ['-tp-host', str(port)] if seat == 0 else ['-tp-join', '127.0.0.1', str(join_port)]
            commands.append(command)
            processes.append(subprocess.Popen(command, cwd=ROOT, env=unity_environment(), startupinfo=startup,
                                               stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL))
            (out / 'job.json').write_text(json.dumps({'commands': commands, 'pids': [p.pid for p in processes]}, indent=2))
            print(name, 'PID', processes[-1].pid, flush=True)
            deadline = time.monotonic() + 25
            while time.monotonic() < deadline:
                log = out / (name + '.log')
                text = log.read_text(errors='replace') if log.exists() else ''
                admitted = f'LocalSlot={seat}' in text
                # Arena installation precedes expensive scene prewarming. Do not
                # connect the next peer while this host is still loading.
                loaded = args.case=='water' or '[HubLoading] Eskinita ready' in text
                if admitted and loaded: break
                if processes[-1].poll() is not None: raise RuntimeError(name + ' exited before admission')
                time.sleep(.2)
            else: raise RuntimeError(name + ' did not enter its expected seat')
        deadline = time.monotonic() + 75
        for process in processes: process.wait(timeout=max(1, deadline-time.monotonic()))
        errors, measured = evaluate(out,args.case)
        if any(p.returncode != 0 for p in processes): errors.append('A native peer exited with failure.')
        if before != read_input_preferences(): errors.append('Shared input preferences changed.')
        dll = exe.parent / (exe.stem + '_Data') / 'Managed/TumbangPreso.Runtime.dll'
        result = {'passed': not errors, 'errors': sorted(set(errors)), 'measured': measured,
                  'runtimeSha256': hashlib.sha256(dll.read_bytes()).hexdigest(),
                  'link': {'delayMs': args.delay, 'jitterMs': args.jitter, 'loss': args.loss, 'seed': args.seed},
                  'scope': ('Three actual '+sys.platform+' peers on '+('Eskinita' if args.case!='water' else 'Lagoon')+'; '+('genuine retrieval, drop/regrab, measured movement, Baha warning/carry/nudge/retirement' if args.case=='integration' else 'genuine retrieval, drop/regrab, measured movement and expiry' if args.case=='backwash' else 'owner Q/E/shared ultimate, authoritative fields and repeated snapshots')+'. Staged input; no WAN/device/human approval.')}
        (out / 'result.json').write_text(json.dumps(result, indent=2)); print(json.dumps(result), flush=True)
        return 0 if result['passed'] else 1
    finally:
        link.close()
        for process in processes:
            if process.poll() is None: process.terminate(); process.wait(timeout=10)
        for source, backup, digest in backups:
            source.parent.mkdir(parents=True, exist_ok=True); shutil.copy2(backup, source)
            if hashlib.sha256(source.read_bytes()).hexdigest() != digest: raise RuntimeError('Profile restore failed.')


if __name__ == '__main__':
    raise SystemExit(main())
