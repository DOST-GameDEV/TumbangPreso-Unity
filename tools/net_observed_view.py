"""Check actual owner -> host -> spectator camera state on three local players."""
import argparse
import csv
import hashlib
import json
from pathlib import Path
import shutil
import socket
import subprocess
import time

from run_unity_guarded import profile_root, unity_environment
from run_ui_player_review import read_input_preferences

ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('exe', type=Path)
    parser.add_argument('--out', type=Path, required=True)
    args = parser.parse_args()
    exe, out = args.exe.resolve(), args.out.resolve()
    if not exe.is_file() or not exe.is_relative_to(ROOT/'Builds'):
        raise SystemExit('Use the canonical Builds player.')
    if not out.is_relative_to(ROOT/'Logs') or out == ROOT/'Logs':
        raise SystemExit('Use a fresh scoped Logs directory.')
    out.mkdir(parents=True, exist_ok=False)
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as sock:
        sock.bind(('127.0.0.1', 0)); port = sock.getsockname()[1]
    processes, commands, backups = [], [], []
    before = read_input_preferences()
    startup = subprocess.STARTUPINFO(); startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW; startup.wShowWindow = 0
    try:
        for index, role in enumerate(['host', 'owner', 'observer']):
            profile = profile_root(['-tp-profile', out.name+'-'+role])
            for source in profile.rglob('*'):
                if source.is_file() and source.suffix != '.log':
                    backup = out/'private-profiles'/role/source.relative_to(profile)
                    backup.parent.mkdir(parents=True, exist_ok=True); shutil.copy2(source, backup)
                    backups.append((source, backup, hashlib.sha256(source.read_bytes()).hexdigest()))
            command = [str(exe), '-screen-width', '960', '-screen-height', '540', '-screen-fullscreen', '0',
                '-tp-framecap', '30', '-tp-autostart', '2', '-tp-map', 'Eskinita',
                '-tp-profile', out.name+'-'+role, '-tp-viewrole', role, '-tp-viewtrace', str(out/(role+'.csv')),
                '-logFile', str(out/(role+'.log'))]
            command += ['-tp-host', str(port)] if index == 0 else ['-tp-join', '127.0.0.1', str(port)]
            commands.append(command)
            processes.append(subprocess.Popen(command, cwd=ROOT, env=unity_environment(), startupinfo=startup,
                stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL))
            print(role, 'PID', processes[-1].pid, flush=True)
            (out/'job.json').write_text(json.dumps({'commands': commands, 'pids': [p.pid for p in processes],
                'exe': str(exe), 'exe_sha256': hashlib.sha256(exe.read_bytes()).hexdigest(),
                'source': subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip()}, indent=2))
            if index == 0:
                time.sleep(6)
            elif index == 1:
                deadline = time.monotonic()+35
                while time.monotonic() < deadline:
                    log = out/'owner.log'
                    if log.exists() and ('LocalSlot=1' in log.read_text(errors='replace') or 'seat assigned' in log.read_text(errors='replace')):
                        break
                    if processes[-1].poll() is not None:
                        raise RuntimeError('Owner exited before admission.')
                    time.sleep(.5)
        deadline = time.monotonic()+175
        while any(p.poll() is None for p in processes) and time.monotonic() < deadline:
            time.sleep(.5)
        if any(p.poll() is None for p in processes):
            raise RuntimeError('View fixture did not finish in its bounded window.')
    finally:
        for process in processes:
            if process.poll() is None:
                process.terminate()
                try: process.wait(timeout=8)
                except subprocess.TimeoutExpired: process.kill(); process.wait(timeout=8)
        for source, backup, digest in backups:
            source.parent.mkdir(parents=True, exist_ok=True); shutil.copy2(backup, source)
            if hashlib.sha256(source.read_bytes()).hexdigest() != digest:
                raise RuntimeError('Profile restoration failed: '+str(source))
        if read_input_preferences() != before:
            raise RuntimeError('Shared Editor input preferences changed.')
    receipts, rows, errors = {}, {}, []
    for role in ['host', 'owner', 'observer']:
        result = out/(role+'.csv.result.json')
        if not result.exists():
            errors.append(role+': missing receipt'); continue
        receipts[role] = json.loads(result.read_text())
        if receipts[role].get('error'):
            errors.append(role+': '+receipts[role]['error'])
        with (out/(role+'.csv')).open(newline='') as file:
            rows[role] = list(csv.DictReader(file))
        if receipts[role]['samples'] < 30:
            errors.append(role+': fewer than30 view samples')
    matched = 0
    if 'host' in rows and 'observer' in rows:
        host = {(r['match'], r['round'], r['epoch'], r['serial']): r for r in rows['host']}
        for row in rows['observer']:
            reference = host.get((row['match'], row['round'], row['epoch'], row['serial']))
            if reference is None: continue
            matched += 1
            for field in ['eye_x', 'eye_y', 'eye_z', 'pitch', 'yaw', 'roll', 'fov', 'sent']:
                if abs(float(row[field])-float(reference[field])) > .0001:
                    errors.append('Relay changed '+field); break
            if row['action'] != reference['action'] or row['aim'] != reference['aim']:
                errors.append('Relay changed viewmodel state')
        if matched < 20: errors.append('Fewer than20 matching host/observer packets')
    if 'observer' in receipts:
        receipt = receipts['observer']
        if not receipt['watched'] or not receipt['unsubscribed'] or receipt['received'] < 20:
            errors.append('Observer did not enter/leave an actual received player view')
        if receipt['maxFov']-receipt['minFov'] < 4:
            errors.append('Observed lens did not follow owner FOV changes')
    if 'owner' in rows and rows['owner']:
        last = float(rows['owner'][-1]['time'])
        tail = [r for r in rows['owner'] if float(r['time']) >= last-2]
        if len({r['published'] for r in tail}) != 1:
            errors.append('Owner kept publishing after spectator unsubscribed')
    report = {'passed': not errors, 'matched_packets': matched, 'receipts': receipts, 'errors': sorted(set(errors)),
              'scope': 'Three real processes on one Windows PC; controlled camera fixture, not separate-machine/WAN/device acceptance.'}
    (out/'summary.json').write_text(json.dumps(report, indent=2))
    print(json.dumps(report, indent=2), flush=True)
    return bool(errors)


if __name__ == '__main__':
    raise SystemExit(main())
