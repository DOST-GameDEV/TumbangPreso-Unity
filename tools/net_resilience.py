"""Three real Windows peers: host loss, or a shaped link followed by durable-seat rejoin."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import socket
import subprocess
import threading
import time
from types import SimpleNamespace

from net_link import Link
from run_unity_guarded import profile_root, unity_environment
from run_ui_player_review import read_input_preferences

ROOT = Path(__file__).resolve().parents[1]


def read(path):
    try:
        return json.loads(path.read_text(encoding='utf-8-sig'))
    except (FileNotFoundError, json.JSONDecodeError):
        return {}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('exe', type=Path)
    parser.add_argument('--out', required=True, type=Path)
    parser.add_argument('--case', required=True, choices=['host-leave', 'host-drop', 'host-shutdown', 'link-rejoin', 'outage-rejoin'])
    parser.add_argument('--dedicated', action='store_true', help='Use the existing seatless spectator server with two playing clients.')
    args = parser.parse_args()
    exe, out = args.exe.resolve(), args.out.resolve()
    if not exe.is_file() or not exe.is_relative_to(ROOT / 'Builds'):
        parser.error('Use the canonical Builds player.')
    if not out.is_relative_to(ROOT / 'Logs') or out == ROOT / 'Logs':
        parser.error('Use a fresh scoped Logs directory.')
    out.mkdir(parents=True, exist_ok=False)
    sockets = [socket.socket(socket.AF_INET, socket.SOCK_DGRAM) for _ in range(4)]
    try:
        for sock in sockets:
            sock.bind(('127.0.0.1', 0))
        ports = [sock.getsockname()[1] for sock in sockets]
    finally:
        for sock in sockets:
            sock.close()
    before = read_input_preferences()
    processes, commands, backups, stages = {}, [], [], []
    link, thread = None, None
    startup = subprocess.STARTUPINFO()
    startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
    startup.wShowWindow = 0
    errors = []
    runtime = exe.parent / (exe.stem + '_Data/Managed/TumbangPreso.Runtime.dll')
    runtime_sha = hashlib.sha256(runtime.read_bytes()).hexdigest()
    profiles = {role: 'net-' + hashlib.sha256(str(out).encode()).hexdigest()[:12] + '-' + role
                for role in ['host', 'client', 'observer']}

    def launch(role, tag=None):
        tag = tag or role
        profile = profile_root(['-tp-profile', profiles[role]])
        if tag == role:
            for source in profile.rglob('*'):
                if source.is_file() and source.suffix != '.log':
                    backup = out / 'profiles-before' / role / source.relative_to(profile)
                    backup.parent.mkdir(parents=True, exist_ok=True)
                    shutil.copy2(source, backup)
                    backups.append((source, backup, hashlib.sha256(source.read_bytes()).hexdigest()))
        command = [str(exe), '-screen-width', '960', '-screen-height', '540', '-screen-fullscreen', '0',
                   '-force-d3d11', '-tp-framecap', '30', '-tp-map', 'Eskinita', '-tp-profile', profiles[role],
                   '-tp-resilience', str(out / tag), '-tp-resilience-role', 'other' if args.dedicated and role == 'observer' else role,
                   '-tp-lobbyport', str(ports[['host', 'client', 'observer'].index(role)]),
                   '-logFile', str(out / (tag + '.log'))]
        join_port = ports[3] if link is not None else ports[0]
        if role == 'host':
            command += ['-tp-dedicated', str(ports[0])] if args.dedicated else ['-tp-lobby']
        else:
            command += ['-tp-lobbyjoin', '127.0.0.1:' + str(join_port)]
        processes[tag] = subprocess.Popen(command, cwd=ROOT, env=unity_environment(), startupinfo=startup,
                                          stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        commands.append(command)
        (out / 'job.json').write_text(json.dumps(dict(commands=commands, profiles=profiles,
            pids={name: child.pid for name, child in processes.items()}, runtimeSha256=runtime_sha), indent=2))
        print(tag, 'PID', processes[tag].pid, flush=True)

    def wait_for(predicate, seconds, reason, live=None):
        until = time.monotonic() + seconds
        while time.monotonic() < until:
            if predicate():
                return
            for name in live or []:
                if processes[name].poll() is not None:
                    raise RuntimeError(name + ' exited early: ' + str(read(out / (name + '.result'))))
            time.sleep(.2)
        raise TimeoutError(reason)

    def signal(tag, suffix):
        (out / (tag + suffix)).write_text('fixture command')

    def hold_stage(name, seconds):
        began = time.monotonic()
        samples = []
        while time.monotonic() - began < seconds:
            samples.append(read(out / 'client.state'))
            time.sleep(.25)
        stages.append(dict(name=name, samples=samples))
        print(name, sorted({(s.get('link', ''), s.get('tier', '')) for s in samples}), flush=True)
        return samples

    try:
        if args.case in ('link-rejoin', 'outage-rejoin'):
            config = SimpleNamespace(to_host='127.0.0.1', to_port=ports[0], listen=ports[3], bind='127.0.0.1',
                delay=0, jitter=0, loss=0, seed=20261025, outage_at=0, outage_for=0, seconds=240)
            link = Link(config)
            thread = threading.Thread(target=link.run, daemon=True)
            thread.start()
        launch('host')
        time.sleep(6)
        launch('client')
        time.sleep(4)
        launch('observer')
        names = ['host', 'client', 'observer']
        wait_for(lambda: all(read(out / (n + '.ready')).get('sawLive') for n in names), 150,
                 'Three actual lobby peers did not enter a live match.', names)
        initial = {n: read(out / (n + '.ready')) for n in names}
        (out / 'initial.json').write_text(json.dumps(initial, indent=2))
        match = initial['host']['match']
        assert match > 0 and all(s['match'] == match for s in initial.values()), initial
        assert initial['client']['local'] in (0, 1, 2, 3) and initial['client']['local'] != initial['host']['local'], initial
        watched = initial['host'] if args.dedicated else initial['observer']
        assert watched['local'] == -1 and watched['spectator'], initial
        if args.dedicated:
            assert initial['observer']['local'] in (0, 1, 2, 3) and initial['observer']['local'] != initial['client']['local'], initial
        if args.case.startswith('host-'):
            if args.case == 'host-leave':
                signal('host', '.stop')
            elif args.case == 'host-drop':
                processes['host'].kill()
                processes['host'].wait(timeout=10)
            else:
                signal('host', '.shutdown')
            survivors = names if args.case == 'host-shutdown' else ['client', 'observer']
            wait_for(lambda: all(read(out / (n + '.lost')) for n in survivors), 40,
                     'Host loss did not reach a persistent Home notice.', survivors)
            for name in survivors:
                state = read(out / (name + '.lost'))
                assert state['revokedAtLoss'] and state['oldRoundStopped'], state
                assert state['scene'] == 'MatchSetup' and not state['networked'] and not state['active'], state
                assert state['ended'] == 0 and state['notice'] and state['cause'] == 'HostLost', state
                wait_for(lambda: (out / (name + '.lost.png')).exists(), 8, 'Notice screenshot missing.')
        elif args.case == 'link-rejoin':
            for shaper in [link.to_host, link.to_client]:
                shaper.delay, shaper.jitter, shaper.loss = .135, .015, .01
            mild = hold_stage('mild', 11)
            signal('client', '.shot')
            wait_for(lambda: (out / 'client.png').exists(), 4, 'Mild icon screenshot missing.')
            shutil.copy2(out / 'client.png', out / 'mild.png')
            assert any(s.get('tier') == 'Mild' for s in mild), 'Sustained 270ms link never showed yellow.'
            for shaper in [link.to_host, link.to_client]:
                shaper.delay, shaper.jitter, shaper.loss = .30, .03, .02
            severe = hold_stage('severe', 12)
            (out / 'client.png').unlink()
            signal('client', '.shot')
            wait_for(lambda: (out / 'client.png').exists(), 4, 'Severe icon screenshot missing.')
            shutil.copy2(out / 'client.png', out / 'severe.png')
            assert any(s.get('tier') == 'Severe' for s in severe), 'Sustained 600ms link never showed red.'
            for shaper in [link.to_host, link.to_client]:
                shaper.delay = shaper.jitter = shaper.loss = 0
            recovered = hold_stage('recovered', 12)
            assert recovered[-1].get('tier') == 'Healthy', recovered[-1]
            link.args.outage_at = time.monotonic() - link.started
            link.args.outage_for = 5
            outage = hold_stage('five-second-outage', 6)
            # Sampling and the deliberate UI settle time are separate clocks. At
            # a five-second outage's boundary, traffic can resume before the red
            # tier settles; prove the measured fault and its displayed response.
            assert any(s.get('link') == 'Bad' and s.get('silence', 0) >= 4 for s in outage), outage[-1]
            assert any(s.get('tier') == 'Severe' for s in outage), outage[-1]
            recovered = hold_stage('outage-recovered', 9)
            assert recovered[-1]['networked'] and recovered[-1]['active'] and recovered[-1]['tier'] == 'Healthy', recovered[-1]
            processes['client'].kill()
            processes['client'].wait(timeout=10)
            wait_for(lambda: read(out / 'host.state').get('departures', 0) == 1,
                     12, 'Departure did not settle.', ['host', 'observer'])
            watcher = 'host' if args.dedicated else 'observer'
            wait_for(lambda: read(out / (watcher + '.state')).get('peerNoticeVisible'),
                     5, 'The actual spectator did not see the retained departure notice.', [watcher])
            signal(watcher, '.shot')
            wait_for(lambda: (out / (watcher + '.png')).exists(), 5, 'Departure notice screenshot missing.')
            shutil.copy2(out / (watcher + '.png'), out / 'departure-notice.png')
            launch('client', 'rejoined')
            wait_for(lambda: read(out / 'rejoined.ready').get('sawLive'), 60, 'Durable client could not rejoin.', ['host', 'observer', 'rejoined'])
            restored = read(out / 'rejoined.ready')
            assert restored['local'] == initial['client']['local'] and restored['match'] == match, restored
            assert restored['epoch'] >= initial['client']['epoch'] and restored['ended'] == 0, restored
            signal('rejoined', '.finish')
        else:
            link.args.outage_at = time.monotonic() - link.started
            link.args.outage_for = 11
            wait_for(lambda: all(read(out / (n + '.lost')) for n in ['client', 'observer']), 45,
                     'A real timeout did not retire both clients into the notice.', ['host', 'client', 'observer'])
            for name in ['client', 'observer']:
                lost = read(out / (name + '.lost'))
                assert lost['revokedAtLoss'] and lost['oldRoundStopped'] and not lost['networked'] and lost['ended'] == 0, lost
                wait_for(lambda: (out / (name + '.lost.png')).exists(), 8, 'Timeout notice screenshot missing.')
                (out / (name + '.ready')).unlink()
                (out / (name + '.rejoin')).write_text('127.0.0.1:' + str(ports[3]))
                wait_for(lambda: read(out / (name + '.ready')).get('sawLive'), 65,
                         name + ' could not rejoin in the same process through the ordinary hub.', ['host', name])
                restored = read(out / (name + '.ready'))
                assert restored['local'] == initial[name]['local'] and restored['match'] == match and restored['ended'] == 0, restored
                assert restored['spectator'] == initial[name]['spectator'], restored
                shutil.copy2(out / (name + '.ready'), out / (name + '.same-process-rejoined.json'))
                print(name, 'same-process rejoin admitted to original seat', restored['local'], flush=True)
        # Preserve client receipts before the host deliberately closes the room.
        for name in sorted(processes, key=lambda n: n == 'host'):
            child = processes[name]
            if child.poll() is None:
                signal(name, '.finish')
                child.wait(timeout=12)
                assert child.returncode == 0, (name, child.returncode)
    except Exception as error:
        errors.append(str(error))
    finally:
        for child in processes.values():
            if child.poll() is None:
                child.terminate()
        for child in processes.values():
            try:
                child.wait(timeout=8)
            except subprocess.TimeoutExpired:
                child.kill(); child.wait(timeout=8)
        if link:
            link.running = False
            thread.join(timeout=3)
        for source, backup, digest in backups:
            source.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(backup, source)
            if hashlib.sha256(source.read_bytes()).hexdigest() != digest:
                errors.append('Profile restore failed: ' + str(source))
        if read_input_preferences() != before:
            errors.append('Shared input preferences changed.')
        if hashlib.sha256(runtime.read_bytes()).hexdigest() != runtime_sha:
            errors.append('Player runtime changed during the run.')
    result = dict(passed=not errors, errors=errors, case=args.case, dedicated=args.dedicated, stages=stages, runtimeSha256=runtime_sha,
        link=None if link is None else dict(forwarded=link.sent, dropped=link.dropped, outageDrops=link.outage_drops),
        scope='Three controlled real Windows peers on one PC. Actual UDP shaping and durable-profile restart where selected; not WAN/Relay/Android or human playtesting.')
    (out / 'result.json').write_text(json.dumps(result, indent=2))
    print(json.dumps(dict(passed=not errors, errors=errors)), flush=True)
    return bool(errors)


if __name__ == '__main__':
    raise SystemExit(main())
