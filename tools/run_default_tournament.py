"""One natural Classic tournament through the actual LAN lobby, with isolated profiles."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import socket
import subprocess
import time
import uuid

import net_matrix
import run_completed_arrival as arrival
import run_unity_guarded as guard
import run_unity_job as jobs
from run_ui_player_review import read_input_preferences


def validate(role, report, text, log, protocol):
    errors = []
    if not report:
        return [role + ' has no final report']
    expected_role = 'HOST' if role == 'host' else 'CLIENT'
    if report.get('role') != expected_role or report.get('networked') != 'True':
        errors.append(role + ' did not retain its expected network role')
    if report.get('protocol') != str(protocol) or report.get('slot') != ('0' if role == 'host' else '1'):
        errors.append(role + ' has an unexpected protocol or local seat')
    if report.get('round') != '8' or report.get('active') != 'False' or report.get('mode') != 'Classic':
        errors.append(role + ' lacks a completed Classic round8 report')
    if '[Slice] match over' not in log:
        errors.append(role + ' lacks natural match-end evidence')
    for field, value in (('tournament ruleset', 'OK'), ('tournament modifiers', 'none'), ('hub lobby seen', 'True')):
        if not re.search(r'^' + field + r'\s*:\s*' + value + r'\s*$', text, re.MULTILINE):
            errors.append(role + ' did not demonstrate ' + field)
    seats = {seat['seat']: seat for seat in report.get('seats', [])}
    if set(seats) != {0, 1, 2, 3} or any(net_matrix.person_sat_here(seats[slot]) is not True for slot in (0, 1)):
        errors.append(role + ' lacks four seats and two human origins')
    return errors


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', type=Path, required=True)
    parser.add_argument('--build-receipt', type=Path, required=True)
    parser.add_argument('--out', type=Path, required=True)
    parser.add_argument('--port', type=int, default=9140)
    args = parser.parse_args()
    project, out = args.project.resolve(), args.out.resolve()
    build = json.loads(args.build_receipt.read_text(encoding='utf-8-sig'))
    exe = Path(build['artifact']).resolve()
    runtime = exe.parent / (exe.stem + '_Data/Managed/TumbangPreso.Runtime.dll')
    digest = lambda: hashlib.sha256(runtime.read_bytes()).hexdigest()
    if not build.get('passed') or not exe.is_relative_to(project / 'Builds') or digest() != build['runtimeSha256']:
        parser.error('Require a qualified internal artifact with matching Runtime hash')
    if not out.is_relative_to(project / 'Logs') or not 1024 <= args.port < 65534:
        parser.error('Require dedicated project Logs output and two nonprivileged ports')
    if guard.project_identity(project) != ('BH Studios', 'Tumbang Preso'):
        parser.error('Unexpected player identity')
    out.mkdir(parents=True, exist_ok=False)
    for port in (args.port, args.port + 1):
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as sock:
            sock.bind(('127.0.0.1', port))
    key = uuid.uuid4().hex[:10]
    claim = jobs.make_claim(project, 'gpu', 3072, 2048, 'default-tournament-' + key,
                            [args.port, args.port + 1], ['-batchmode'])
    children, seeds, before, acquired = {}, {}, None, False
    result = dict(passed=False, sourceCommit=build['sourceCommit'], runtimeSha256=digest(),
                  scope='Natural Classic eight-round tournament through LAN lobby; two undriven human seats, two ordinary bots. No AllBots, forced end, SDK or physical-input claim.')

    def launch(role, seconds):
        name = 'default-tournament-' + role + '-' + key
        profile = guard.player_profile() / 'profiles' / hashlib.sha256(name.encode()).hexdigest()
        profile.mkdir(parents=True, exist_ok=False)
        path = profile / 'settings.json'
        seed = json.dumps(dict(PlayerToken=uuid.uuid4().hex, PlayerName='Tournament' + role,
                               GameMode=0, MatchDefaultsRevision=1, GraphicsQuality=1)).encode()
        path.write_bytes(seed); seeds[path] = seed
        route = ['-tp-lobby', '-tp-lobbyport', str(args.port)] if role == 'host' else [
            '-tp-lobbyjoin', '127.0.0.1:' + str(args.port), '-tp-lobbyport', str(args.port + 1)]
        command = [str(exe), '-batchmode', '-screen-fullscreen', '0', '-screen-width', '1920',
                   '-screen-height', '1080', '-tp-framecap', '60', '-tp-profile', name,
                   '-tp-tournament', '-tp-autostart', '2', '-tp-netreport', str(out / (role + '.txt')),
                   '-tp-netseconds', str(seconds), '-logFile', str(out / (role + '.log')), *route]
        startup = subprocess.STARTUPINFO(); startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW; startup.wShowWindow = 0
        children[role] = subprocess.Popen(command, cwd=project, env=guard.unity_environment(), startupinfo=startup)
        result[role + 'Pid'] = children[role].pid; result[role + 'Command'] = command
        result[role + 'Profile'] = str(profile)
        (out / 'launch.json').write_text(json.dumps(result, indent=2))

    try:
        result['admission'] = jobs.acquire(jobs.POOL, claim, 300); acquired = True
        before = read_input_preferences()
        (out / 'player-input-before.json').write_text(json.dumps(before, indent=2))
        deadline = time.monotonic() + 850
        launch('host', 803)
        time.sleep(7)
        launch('client', 790)
        while time.monotonic() < deadline and any(child.poll() is None for child in children.values()):
            time.sleep(.5)
        if any(child.poll() is None for child in children.values()):
            raise TimeoutError('Natural tournament exceeded its 850-second ceiling')
        errors, reports = [], {}
        for role, child in children.items():
            log = (out / (role + '.log')).read_text(encoding='utf-8-sig', errors='replace')
            report = net_matrix.parse_report(str(out / (role + '.txt'))); reports[role] = report
            if child.returncode != 0: errors.append(role + ' exited unsuccessfully')
            text = (out / (role + '.txt')).read_text(encoding='utf-8-sig') if report else ''
            errors.extend(validate(role, report, text, log, build['protocol']))
            if arrival.observed_renderer(log) != 'Direct3D11': errors.append(role + ' did not demonstrate Direct3D11')
        if all(reports.values()):
            scores = {role: [seat['score'] for seat in sorted(report['seats'], key=lambda seat: seat['seat'])]
                      for role, report in reports.items()}
            result['terminalScores'] = scores
            if scores['host'] != scores['client']: errors.append('Terminal peer scores disagree')
        result['reports'], result['errors'], result['passed'] = reports, errors, not errors
    except Exception as error:
        result['errors'] = [str(error)]
    finally:
        for child in children.values():
            if child.poll() is None: child.terminate()
        for child in children.values():
            try: child.wait(timeout=8)
            except subprocess.TimeoutExpired: child.kill(); child.wait()
        try:
            if before is not None: arrival.restore_input(before)
            for path, seed in seeds.items(): path.write_bytes(seed)
            result['inputRestored'] = before is None or read_input_preferences() == before
            result['profileSeedsRestored'] = all(path.read_bytes() == seed for path, seed in seeds.items())
            result['runtimeUnchanged'] = digest() == build['runtimeSha256']
            result['retiredPids'] = [child.pid for child in children.values()]
            result['passed'] &= result['inputRestored'] and result['profileSeedsRestored'] and result['runtimeUnchanged']
        except Exception as error:
            result['passed'] = False
            result.setdefault('errors', []).append('Preservation failed: ' + str(error))
        finally:
            if acquired: jobs.update_lease(jobs.POOL, claim['id'])
            (out / 'result.json').write_text(json.dumps(result, indent=2))
            print(json.dumps({key: value for key, value in result.items() if key not in ('reports',)}, indent=2), flush=True)
    return 0 if result['passed'] else 1


if __name__ == '__main__':
    raise SystemExit(main())
