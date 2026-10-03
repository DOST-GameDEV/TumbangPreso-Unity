"""One guarded side of an identical-artifact, two-machine LAN match.

Run host and client separately on their own machines. Compare terminal result.json
files afterward; this runner does not control or message another agent or machine.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import uuid

import net_matrix
import run_completed_arrival as arrival
import run_unity_guarded as guard
import run_unity_job as jobs
from run_ui_player_review import read_input_preferences

ROOT = Path(__file__).resolve().parents[1]
WIRE = arrival.WIRE


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--role', choices=('host', 'client'), required=True)
    parser.add_argument('--exe', type=Path, required=True)
    parser.add_argument('--out', type=Path, required=True)
    parser.add_argument('--profile', required=True)
    parser.add_argument('--host', default='192.168.1.7')
    parser.add_argument('--port', type=int, default=49153)
    parser.add_argument('--seconds', type=int, required=True)
    args = parser.parse_args()
    if not 1 <= args.port < 65535 or not 90 <= args.seconds <= 240:
        parser.error('Require port1..65534 and90..240 seconds.')
    exe = args.exe.resolve(); out = args.out.resolve()
    runtime = exe.parent / (exe.stem + '_Data/Managed/TumbangPreso.Runtime.dll')
    sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
    expected = '501f0a02db575003c48910f08bdd0221810031c4d30aaf499f4727bbeca0c806'
    if sha(runtime) != expected:
        raise RuntimeError('This bounded case requires the verified1003e Runtime.')
    arrival.validate_rules(exe.parent / (exe.stem + '_Data/Managed/TumbangPreso.Core.dll'))
    out.mkdir(parents=True, exist_ok=False)
    profile = guard.player_profile() / 'profiles' / hashlib.sha256(args.profile.encode()).hexdigest()
    if profile.exists():
        raise RuntimeError('Require a fresh task-owned profile, never replace an existing one.')
    port = args.port if args.role == 'host' else args.port + 1
    claim = jobs.make_claim(ROOT, 'gpu', 1536, 1024, args.profile, [port], [])
    result = dict(passed=False, role=args.role, sourceCommit='14dfe9e416f5a98698844d9da34ab3c34b849a17',
                  protocol=134, runtimeSha256=expected, exeSha256=sha(exe),
                  coreSha256=sha(runtime.with_name('TumbangPreso.Core.dll')),
                  scope='Normal LAN lobby, ready, natural Hero1/30 completion and own saved career; no physical-input or current source-fix acceptance.')
    acquired = False; before = None; child = None; profile_created = False
    token = uuid.uuid4().hex
    seed = json.dumps(dict(PlayerToken=token, PlayerName='LAN' + args.role,
                           CustomRulesWire=WIRE, HubQueueChoice=2,
                           GraphicsQuality=0, MatchDefaultsRevision=1)).encode()
    try:
        result['admission'] = jobs.acquire(jobs.POOL, claim, 0); acquired = True
        before = read_input_preferences()
        profile.mkdir(parents=True, exist_ok=False); profile_created = True
        (profile / 'settings.json').write_bytes(seed)
        route = ['-tp-lobby', '-tp-lobbyport', str(port)] if args.role == 'host' else [
            '-tp-lobbyjoin', args.host + ':' + str(args.port), '-tp-lobbyport', str(port)]
        command = [str(exe), '-batchmode', '-screen-fullscreen', '0', '-screen-width', '640',
                   '-screen-height', '360', '-tp-framecap', '60', '-tp-profile', args.profile,
                   '-tp-autostart', '2', '-tp-netreport', str(out / 'state.txt'),
                   '-tp-netseconds', str(args.seconds), '-logFile', str(out / 'player.log'), *route]
        startup = subprocess.STARTUPINFO(); startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW; startup.wShowWindow = 0
        child = subprocess.Popen(command, cwd=ROOT, env=guard.unity_environment(), startupinfo=startup)
        result['pid'] = child.pid
        (out / 'launch.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
        result['exitCode'] = child.wait(timeout=args.seconds + 35)
        text = (out / 'state.txt').read_text(encoding='utf-8-sig')
        log = (out / 'player.log').read_text(encoding='utf-8-sig', errors='replace')
        report = net_matrix.parse_report(str(out / 'state.txt'))
        errors = []
        if result['exitCode'] != 0: errors.append('Player did not exit normally.')
        expected_role = 'HOST' if args.role == 'host' else 'CLIENT'
        expected_slot = '0' if args.role == 'host' else '1'
        if not report or any(report.get(k) != v for k, v in dict(role=expected_role, slot=expected_slot, protocol='134', networked='True', round='1', active='False', mode='HeroStrike').items()):
            errors.append('Terminal report lacks the required role, seat and completed Hero round1.')
        if '[NetAuto] READY submitted' not in log or '[Slice] match over' not in log:
            errors.append('No normal ready/natural end evidence.')
        if not re.search(r'^selected rules\s*:\s*' + re.escape(WIRE) + r'\s*$', text, re.MULTILINE):
            errors.append('Actual rules do not match custom Hero1/30.')
        cache = json.loads((profile / 'career.json').read_text(encoding='utf-8-sig'))
        history = cache.get('History', []); queue = cache.get('Queue', [])
        record = history[0] if len(history) == 1 else {}
        players = sorted(record.get('Players', []), key=lambda p: p['Slot'])
        own = [p for p in players if p.get('PlayerId') == token + '_' + args.profile]
        result['saved'] = dict(historyCount=len(history), queueCount=len(queue), witnessCount=len(cache.get('QueueWitness', [])),
            matchIdSha256=hashlib.sha256(record.get('MatchId', '').encode()).hexdigest() if record.get('MatchId') else '',
            scores=[p['Score'] for p in players], ownHumanLine=len(own) == 1 and not own[0].get('IsBot', True),
            twoHumanOrigins=sum(not p.get('IsBot', True) for p in players) == 2,
            markerCleared=not cache.get('InMatchSinceUtc'))
        seats = {p['seat']: p for p in report.get('seats', [])} if report else {}
        if (set(seats) != {0, 1, 2, 3} or any(net_matrix.person_sat_here(seats[s]) is not True for s in (0, 1))
                or [seats[s].get('score') for s in sorted(seats)] != result['saved']['scores']):
            errors.append('Terminal standings/human origins disagree with the saved record.')
        if not (len(history) == len(queue) == len(cache.get('QueueWitness', [])) == 1 and queue[0] == record
                and record.get('Online') is True and record.get('Rounds') == 1 and record.get('Mode') == 'HeroStrike'
                and len(players) == 4 and result['saved']['ownHumanLine'] and result['saved']['twoHumanOrigins']
                and result['saved']['markerCleared'] and result['saved']['matchIdSha256']):
            errors.append('Own saved career/queue/identity/terminal marker contract failed.')
        result['errors'] = errors; result['passed'] = not errors
    except Exception as error:
        result['errors'] = [str(error)]
    finally:
        cleanup_errors = []
        try:
            if child is not None and child.poll() is None:
                result['forcedTermination'] = True; result['passed'] = False
                child.terminate()
                try: child.wait(timeout=10)
                except subprocess.TimeoutExpired:
                    child.kill(); child.wait(timeout=10)
        except Exception as error: cleanup_errors.append('Owned player cleanup: ' + str(error))
        terminal = child is None or child.poll() is not None
        try:
            if not terminal: raise RuntimeError('Preservation waits for owned player termination.')
            if before is not None: arrival.restore_input(before)
            if profile_created: (profile / 'settings.json').write_bytes(seed)
            result['profileSeedsRestored'] = not profile_created or (profile / 'settings.json').read_bytes() == seed
            result['inputRestored'] = before is None or before == read_input_preferences()
            result['runtimeUnchanged'] = sha(runtime) == expected
            result['passed'] &= result['profileSeedsRestored'] and result['inputRestored'] and result['runtimeUnchanged']
        except Exception as error:
            cleanup_errors.append('Preservation: ' + str(error)); result['passed'] = False
        finally:
            result['terminal'] = terminal; result['leaseHeld'] = acquired and not terminal
            if acquired:
                try:
                    jobs.update_lease(jobs.POOL, claim['id'], None if terminal else {'guardPid': child.pid, 'awaitingRestoration': True})
                except Exception as error:
                    result['leaseHeld'] = True; cleanup_errors.append('Scheduler cleanup: ' + str(error))
            if cleanup_errors:
                result.setdefault('errors', []).extend(cleanup_errors); result['passed'] = False
            (out / 'result.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
            print(json.dumps(result, indent=2))
    return 0 if result['passed'] else 1


if __name__ == '__main__':
    raise SystemExit(main())
