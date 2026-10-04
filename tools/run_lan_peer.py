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
WIRE = arrival.WIRE + '|0'  # Current canonical wire explicitly disables map voting.


def file_sha256(path):
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            digest.update(chunk)
    return digest.hexdigest()


def checked_artifact(exe, manifest_path, protocol, expected_runtime):
    manifest = json.loads(manifest_path.read_text(encoding='utf-8-sig'))
    if manifest.get('protocol') != protocol:
        raise RuntimeError('Artifact manifest does not match the agreed protocol.')
    if manifest.get('strictReceiptPassed') is not True and manifest.get('classifiedArtifactAccepted') is not True:
        raise RuntimeError('Require an accepted build or explicit accepted artifact classification.')
    source = manifest.get('sourceCommit', '')
    if re.fullmatch(r'[0-9a-fA-F]{40}', source) is None:
        raise RuntimeError('Require a complete artifact source identity.')
    files = manifest.get('files', [])
    if not files or manifest.get('fileCount') != len(files):
        raise RuntimeError('Require a complete checked artifact manifest.')
    root = exe.parent.resolve(); listed = set(); total = 0
    for entry in files:
        relative = entry['path']
        path = (root / relative).resolve()
        if Path(relative).is_absolute() or not path.is_relative_to(root) or relative in listed:
            raise RuntimeError('Artifact manifest contains an unsafe or duplicate path.')
        listed.add(relative)
        if path.stat().st_size != entry['bytes'] or file_sha256(path) != entry['sha256']:
            raise RuntimeError('Packaged file differs from the checked manifest: ' + relative)
        total += entry['bytes']
    actual = {path.relative_to(root).as_posix() for path in root.rglob('*') if path.is_file()}
    if actual != listed or manifest.get('totalBytes') != total:
        raise RuntimeError('Packaged file inventory differs from the checked manifest.')
    runtime = root / (exe.stem + '_Data/Managed/TumbangPreso.Runtime.dll')
    core = runtime.with_name('TumbangPreso.Core.dll')
    for path, key in [(exe, 'exeSha256'), (runtime, 'runtimeSha256'), (core, 'coreSha256')]:
        if file_sha256(path) != manifest.get(key):
            raise RuntimeError('Packaged executable/assembly identity does not match the manifest.')
    if manifest['runtimeSha256'] != expected_runtime:
        raise RuntimeError('Runtime does not match the pinned shared artifact.')
    identity = root / (exe.stem + '_Data/StreamingAssets/build-identity.json')
    if json.loads(identity.read_text(encoding='utf-8-sig')).get('sha', '').lower() != source.lower():
        raise RuntimeError('Packaged build identity does not match the artifact source.')
    return manifest


def profile_seed(token, role, character_pick):
    return json.dumps(dict(PlayerToken=token, PlayerName='LAN' + role,
                           CharacterPick=character_pick, CustomRulesWire=WIRE,
                           HubQueueChoice=2, GraphicsQuality=0, MatchDefaultsRevision=1)).encode()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--role', choices=('host', 'client'), required=True)
    parser.add_argument('--exe', type=Path, required=True)
    parser.add_argument('--out', type=Path, required=True)
    parser.add_argument('--profile', required=True)
    parser.add_argument('--host', default='192.168.1.7')
    parser.add_argument('--port', type=int, default=49153)
    parser.add_argument('--seconds', type=int, required=True)
    parser.add_argument('--wait-seconds', type=int, default=0)
    parser.add_argument('--protocol', type=int, required=True, help='Agreed protocol of the checked shared artifact')
    parser.add_argument('--artifact-manifest', type=Path, required=True)
    parser.add_argument('--character-pick', type=int, default=0, help='Valid Hero roster index for normal lobby selection')
    parser.add_argument('--runtime-sha256', required=True,
                        help='Pinned Runtime hash from the checked shared artifact receipt')
    args = parser.parse_args()
    if (not 1 <= args.port < 65535 or not 90 <= args.seconds <= 240 or not 0 <= args.wait_seconds <= 600
            or not 1 <= args.protocol <= 65535 or not 0 <= args.character_pick <= 2147483647):
        parser.error('Require valid port/protocol, nonnegative character pick,90..240 seconds and0..600 pool wait seconds.')
    exe = args.exe.resolve(); out = args.out.resolve()
    runtime = exe.parent / (exe.stem + '_Data/Managed/TumbangPreso.Runtime.dll')
    sha = file_sha256
    expected = args.runtime_sha256.lower()
    if re.fullmatch(r'[0-9a-f]{64}', expected) is None:
        parser.error('Require the checked artifact Runtime SHA256.')
    artifact = checked_artifact(exe, args.artifact_manifest.resolve(), args.protocol, expected)
    arrival.validate_rules(exe.parent / (exe.stem + '_Data/Managed/TumbangPreso.Core.dll'), wire=WIRE)
    source_commit = artifact['sourceCommit']
    out.mkdir(parents=True, exist_ok=False)
    profile = guard.player_profile() / 'profiles' / hashlib.sha256(args.profile.encode()).hexdigest()
    if profile.exists():
        raise RuntimeError('Require a fresh task-owned profile, never replace an existing one.')
    port = args.port if args.role == 'host' else args.port + 1
    claim = jobs.make_claim(ROOT, 'gpu', 1536, 1024, args.profile, [port], [])
    result = dict(passed=False, role=args.role, sourceCommit=source_commit,
                  protocol=args.protocol, runtimeSha256=expected, exeSha256=sha(exe),
                  coreSha256=sha(runtime.with_name('TumbangPreso.Core.dll')),
                  artifactManifestSha256=sha(args.artifact_manifest.resolve()), characterPick=args.character_pick,
                  scope='Normal LAN lobby, ready, natural Hero1/30 completion and own saved career; no physical-input or current source-fix acceptance.')
    acquired = False; before = None; child = None; profile_created = False
    token = uuid.uuid4().hex
    seed = profile_seed(token, args.role, args.character_pick)
    try:
        result['admission'] = jobs.acquire(jobs.POOL, claim, args.wait_seconds); acquired = True
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
        if not report or any(report.get(k) != v for k, v in dict(role=expected_role, slot=expected_slot, protocol=str(args.protocol), networked='True', round='1', active='False', mode='HeroStrike').items()):
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
            result['manifestUnchanged'] = sha(args.artifact_manifest.resolve()) == result['artifactManifestSha256']
            checked_artifact(exe, args.artifact_manifest.resolve(), args.protocol, expected)
            result['artifactUnchanged'] = True
            result['passed'] &= result['profileSeedsRestored'] and result['inputRestored'] and result['runtimeUnchanged'] and result['artifactUnchanged'] and result['manifestUnchanged']
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
