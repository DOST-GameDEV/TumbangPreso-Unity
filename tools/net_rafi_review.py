"""Three native peers: Rafi owner input, water messages/snapshots and expiry."""
import argparse
import hashlib
import json
import math
from pathlib import Path
import shutil
import socket
import subprocess
import time

from presentation_peer_link import PresentationLink, arguments as link_arguments
from run_unity_guarded import profile_root, unity_environment
from run_ui_player_review import read_input_preferences

ROOT = Path(__file__).resolve().parents[1]


def evaluate(out):
    data = {name: [json.loads(line) for line in (out / (name + '.jsonl')).read_text().splitlines()]
            for name in ('host', 'owner', 'observer')}
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
        measured[name] = {'rows': len(rows), 'eventIds': sorted(observed), 'starts': rows[-1]['starts'] if rows else 0,
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


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('exe', type=Path); parser.add_argument('--out', type=Path, required=True)
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
            command = [str(exe), '-batchmode', '-screen-width', '800', '-screen-height', '450', '-screen-fullscreen', '0',
                       '-tp-framecap', '30', '-tp-autostart', '3', '-tp-map', 'Lagoon', '-tp-profile', profile_name,
                       '-tp-rafitrace', str(out / (name + '.jsonl')), '-logFile', str(out / (name + '.log'))]
            command += ['-tp-host', str(port)] if seat == 0 else ['-tp-join', '127.0.0.1', str(join_port)]
            commands.append(command)
            processes.append(subprocess.Popen(command, cwd=ROOT, env=unity_environment(), startupinfo=startup,
                                               stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL))
            (out / 'job.json').write_text(json.dumps({'commands': commands, 'pids': [p.pid for p in processes]}, indent=2))
            print(name, 'PID', processes[-1].pid, flush=True)
            deadline = time.monotonic() + 25
            while time.monotonic() < deadline:
                log = out / (name + '.log')
                if log.exists() and f'LocalSlot={seat}' in log.read_text(errors='replace'): break
                if processes[-1].poll() is not None: raise RuntimeError(name + ' exited before admission')
                time.sleep(.2)
            else: raise RuntimeError(name + ' did not enter its expected seat')
        deadline = time.monotonic() + 75
        for process in processes: process.wait(timeout=max(1, deadline-time.monotonic()))
        errors, measured = evaluate(out)
        if any(p.returncode != 0 for p in processes): errors.append('A native peer exited with failure.')
        if before != read_input_preferences(): errors.append('Shared input preferences changed.')
        dll = exe.parent / (exe.stem + '_Data') / 'Managed/TumbangPreso.Runtime.dll'
        result = {'passed': not errors, 'errors': sorted(set(errors)), 'measured': measured,
                  'runtimeSha256': hashlib.sha256(dll.read_bytes()).hexdigest(),
                  'link': {'delayMs': args.delay, 'jitterMs': args.jitter, 'loss': args.loss, 'seed': args.seed},
                  'scope': 'Three actual Windows peers on Lagoon; owner Q/E/shared ultimate, authoritative field IDs/paths/lifetime, repeated world snapshots and cleanup. Staged inputs; no WAN or balance claim.'}
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
