"""Built-player checks of the final whistle's MATCH OVER beat (`MatchEndBeat`, `NetMatchEndProbe`).

Offline: every court in `SceneFlow.MapRegistry`, Classic and Hero Strike, with bodies in the air at the whistle
(Updraft, a real AIRBURST release, a long fall), one score-target finish, and the real REMATCH into the next match.

Networked: a real host, a client behind a delayed UDP link (`net_link.Link`) and a spectator, START GAME from the
room, the same air case at the whistle, and the real REMATCH. `--rejoin` cuts the client's link during the beat so it
goes through a real timeout, CONNECTION LOST and its REJOIN, and arrives back after the board is up.

Each peer writes `<role>.result.json`; this script judges them and writes `summary.json`.
"""
import argparse
import hashlib
import json
import shutil
import socket
import subprocess
import threading
import time
from pathlib import Path
from types import SimpleNamespace

from net_link import Link
from run_unity_guarded import profile_root, unity_environment
from run_ui_player_review import read_input_preferences

ROOT = Path(__file__).resolve().parents[1]
COURTS = [('Eskinita', 'Tondo'), ('IlalimNgTulay', 'Ermita'), ('LagoonCove', 'Sitangkai'),
          ('Kanto', 'BGC'), ('Arena', 'Philippine Arena'), ('Payo', 'Banaue')]


def read(path):
    try:
        return json.loads(Path(path).read_text(encoding='utf-8-sig'))
    except (FileNotFoundError, json.JSONDecodeError):
        return {}


def free_ports(count):
    socks = [socket.socket(socket.AF_INET, socket.SOCK_DGRAM) for _ in range(count)]
    try:
        for s in socks:
            s.bind(('127.0.0.1', 0))
        return [s.getsockname()[1] for s in socks]
    finally:
        for s in socks:
            s.close()


class Runner:
    def __init__(self, exe, out):
        self.exe, self.out = exe, out
        self.profiles, self.processes = [], {}
        startup = subprocess.STARTUPINFO()
        startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
        startup.wShowWindow = 0
        self.startup = startup

    def launch(self, tag, folder, role, extra):
        profile = 'matchend-' + hashlib.sha256((str(self.out) + tag).encode()).hexdigest()[:12]
        root = profile_root(['-tp-profile', profile])
        if root.exists():
            raise RuntimeError('Profile already exists, refusing to reuse it: ' + str(root))
        self.profiles.append(root)
        folder.mkdir(parents=True, exist_ok=True)
        command = [str(self.exe), '-screen-width', '1280', '-screen-height', '720', '-screen-fullscreen', '0',
                   '-force-d3d11', '-tp-framecap', '60', '-tp-profile', profile,
                   '-tp-matchend', str(folder), '-tp-matchend-role', role, '-logFile', str(folder / (role + '.log'))] + extra
        child = subprocess.Popen(command, cwd=ROOT, env=unity_environment(), startupinfo=self.startup,
                                 stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        self.processes[tag] = child
        (folder / (role + '.command.json')).write_text(json.dumps(command, indent=1))
        print(tag, 'PID', child.pid, flush=True)
        return child

    def wait(self, tags, seconds, tick=None):
        until = time.monotonic() + seconds
        while time.monotonic() < until and any(self.processes[t].poll() is None for t in tags):
            if tick:
                tick()
            time.sleep(0.2)
        for t in tags:
            if self.processes[t].poll() is None:
                self.processes[t].kill()
                self.processes[t].wait(timeout=10)
        return {t: self.processes[t].returncode for t in tags}

    def cleanup(self):
        for child in self.processes.values():
            if child.poll() is None:
                child.kill()
                child.wait(timeout=10)
        removed = []
        for root in self.profiles:
            if root.exists():
                size = sum(f.stat().st_size for f in root.rglob('*') if f.is_file())
                shutil.rmtree(root)
                removed.append(dict(path=str(root), bytes=size))
        return removed


def winner_of(scores):
    best = max(scores)
    return scores.index(best) if scores.count(best) == 1 else -1


def judge_match(m, role, problems, expect_board_at_once=False):
    name = f"{role} {m.get('map')} match {m.get('id')}"
    if not m.get('ended'):
        problems.append(name + ': never ended')
        return
    if m.get('reveals') != 1:
        problems.append(f"{name}: {m.get('reveals')} reveals")
    if m.get('boardSamplesDuringBeat', 0) > 0:
        problems.append(name + ': the board was up during the beat')
    if not expect_board_at_once:
        if m.get('bannerSamples', 0) <= 0:
            problems.append(name + ': MATCH OVER was never seen')
        if m.get('revealAge', 0) < 1.95:
            problems.append(f"{name}: board at age {m.get('revealAge')}")
    scores = m.get('scores') or []
    if len(scores) == 4 and winner_of(scores) != m.get('winner'):
        problems.append(f"{name}: winner {m.get('winner')} does not match scores {scores}")
    if role in ('offline', 'host'):
        for body in m.get('restLate', []):
            parts = body.split(':')
            if len(parts) >= 5 and (parts[1] != 'rest' or parts[2] != 'grounded' or abs(float(parts[4])) > 0.3):
                problems.append(f'{name}: body not at rest under the board: {body}')
        if not m.get('rematchOffered') and role != 'spectator':
            problems.append(name + ': REMATCH not offered')
    if role == 'offline' and not m.get('timePausedAfterReveal'):
        problems.append(name + ': single player time not stopped under the board')


def judge_run(results, problems, rejoin=False):
    for role, r in results.items():
        if not r.get('completed'):
            problems.append(f"{role}: did not complete ({r.get('notes')})")
        matches = r.get('matches', [])
        if len(matches) < 2:
            problems.append(f'{role}: {len(matches)} matches traced, expected the match and its rematch')
            continue
        judge_match(matches[0], role, problems, expect_board_at_once=rejoin and role == 'client')
        if matches[0].get('leftoverInNext'):
            problems.append(role + ': the first ending was still on screen in the rematch')
        if not matches[-1].get('liveRoundSeen'):
            problems.append(role + ': the rematch never went live')


def offline_suite(runner, out, problems, summary):
    runs = []
    for index, (scene, name) in enumerate(COURTS):
        mode = 'classic' if index % 2 == 0 else 'hero'
        runs.append((f'offline-{scene}', scene, mode, ['-tp-matchend-case', 'air'], name))
    runs.append(('offline-target-Kanto', 'Kanto', 'classic',
                 ['-tp-matchend-case', 'none', '-tp-matchend-rounds', '3', '-tp-matchend-target', '10'], 'BGC score target'))
    for start in range(0, len(runs), 2):
        batch = runs[start:start + 2]
        for tag, scene, mode, extra, _ in batch:
            runner.launch(tag, out / tag, 'offline', ['-tp-matchend-map', scene, '-tp-matchend-mode', mode,
                                                     '-tp-matchend-seconds', '30', '-tp-matchend-timeout', '420'] + extra)
        codes = runner.wait([b[0] for b in batch], 480)
        for tag, scene, mode, extra, name in batch:
            r = read(out / tag / 'offline.result.json')
            local = []
            judge_run({'offline': r}, local)
            first = (r.get('matches') or [{}])[0]
            if tag.startswith('offline-target') and first.get('endPath') != 'score-target':
                local.append(f"{tag}: ended by {first.get('endPath')}, not the score target")
            summary['offline'].append(dict(tag=tag, court=name, mode=mode, exit=codes[tag], problems=local,
                                           build=r.get('build'), notes=r.get('notes'),
                                           first={k: first.get(k) for k in ('map', 'endPath', 'finalRound', 'winner', 'scores',
                                                                            'reveals', 'revealAge', 'bannerSamples', 'corrections',
                                                                            'restLate', 'timePausedAfterReveal')},
                                           next={k: (r.get('matches') or [{}, {}])[-1].get(k) for k in ('map', 'liveRoundSeen')}))
            problems.extend(local)
            print(tag, 'problems:', local, flush=True)


def network_suite(runner, out, problems, summary, rejoin):
    tag = 'network-rejoin' if rejoin else 'network'
    folder = out / tag
    ports = free_ports(4)
    config = SimpleNamespace(to_host='127.0.0.1', to_port=ports[0], listen=ports[3], bind='127.0.0.1',
                             delay=75, jitter=10, loss=0, seed=20261126, outage_at=0, outage_for=0, seconds=0)
    link = Link(config)
    thread = threading.Thread(target=link.run, daemon=True)
    thread.start()
    scene = 'Kanto' if rejoin else 'LagoonCove'
    common = ['-tp-map', scene, '-tp-matchend-map', scene, '-tp-matchend-timeout', '420']
    try:
        runner.launch(tag + '-host', folder, 'host', common + [
            '-tp-lobby', '-tp-lobbyport', str(ports[0]), '-tp-matchend-mode', 'hero', '-tp-matchend-seconds', '30',
            '-tp-matchend-case', 'air', '-tp-matchend-rematch-after', '40' if rejoin else '4'])
        time.sleep(6)
        runner.launch(tag + '-client', folder, 'client', common + [
            '-tp-lobbyjoin', '127.0.0.1:' + str(ports[3]), '-tp-lobbyport', str(ports[1])] + (['-tp-matchend-rejoin'] if rejoin else []))
        time.sleep(3)
        runner.launch(tag + '-spectator', folder, 'spectator', common + [
            '-tp-lobbyjoin', '127.0.0.1:' + str(ports[0]), '-tp-lobbyport', str(ports[2])])
        cut = {'done': False}

        def tick():
            if not cut['done'] and (folder / 'client.drop').exists():
                cut['done'] = True
                link.args.outage_at = time.monotonic() - link.started
                link.args.outage_for = 13
                print('cut the client link for 13 s', flush=True)

        codes = runner.wait([tag + '-host', tag + '-client', tag + '-spectator'], 600, tick)
    finally:
        link.running = False
        thread.join(timeout=3)
    results = {role: read(folder / (role + '.result.json')) for role in ('host', 'client', 'spectator')}
    local = []
    judge_run(results, local, rejoin)
    firsts = {role: (r.get('matches') or [{}])[0] for role, r in results.items()}
    ids = {m.get('id') for m in firsts.values()}
    if len(ids) != 1:
        local.append(f'peers traced different first matches: {ids}')
    winners = {m.get('winner') for m in firsts.values()}
    scores = {tuple(m.get('scores') or []) for m in firsts.values()}
    if len(winners) != 1 or len(scores) != 1:
        local.append(f'peers disagree on the result: winners {winners} scores {scores}')
    if not rejoin and all(m.get('revealWall', -1) > 0 for m in firsts.values()):
        walls = [m['revealWall'] for m in firsts.values()]
        if max(walls) - min(walls) > 0.6:
            local.append(f'reveals spread over {max(walls) - min(walls):.2f} s across peers')
    for role in ('client', 'spectator'):
        if not firsts[role].get('hostTimed'):
            local.append(role + ': the beat was never timed by the host')
    if rejoin:
        notes = ' '.join(results['client'].get('notes') or [])
        if 'REJOIN admitted' not in notes:
            local.append('the client never came back through REJOIN: ' + notes)
        if not cut['done']:
            local.append('the client link was never cut')
    summary['network'].append(dict(tag=tag, court=scene, rejoin=rejoin, exit=codes, problems=local,
                                   link=dict(forwarded=link.sent, dropped=link.dropped, outageDrops=link.outage_drops),
                                   notes={role: r.get('notes') for role, r in results.items()},
                                   first={role: {k: m.get(k) for k in ('id', 'winner', 'scores', 'reveals', 'revealAge', 'revealWall',
                                                                       'hostTimed', 'bannerSamples', 'firstSeenAge', 'corrections',
                                                                       'restLate')} for role, m in firsts.items()},
                                   next={role: (r.get('matches') or [{}, {}])[-1].get('liveRoundSeen') for role, r in results.items()}))
    problems.extend(local)
    print(tag, 'problems:', local, flush=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('exe', type=Path)
    parser.add_argument('--out', required=True, type=Path)
    parser.add_argument('--suite', choices=['offline', 'network', 'all'], default='all')
    args = parser.parse_args()
    exe, out = args.exe.resolve(), args.out.resolve()
    if not exe.is_file() or not exe.is_relative_to(ROOT / 'Builds'):
        parser.error('Use the canonical Builds player.')
    if not out.is_relative_to(ROOT / 'Logs') or out == ROOT / 'Logs':
        parser.error('Use a fresh scoped Logs directory.')
    out.mkdir(parents=True, exist_ok=False)
    runtime = exe.parent / (exe.stem + '_Data/Managed/TumbangPreso.Runtime.dll')
    runtime_sha = hashlib.sha256(runtime.read_bytes()).hexdigest()
    before = read_input_preferences()
    runner = Runner(exe, out)
    problems, summary = [], dict(offline=[], network=[])
    try:
        if args.suite in ('offline', 'all'):
            offline_suite(runner, out, problems, summary)
        if args.suite in ('network', 'all'):
            network_suite(runner, out, problems, summary, rejoin=False)
            network_suite(runner, out, problems, summary, rejoin=True)
    except Exception as error:
        problems.append('harness: ' + repr(error))
    finally:
        removed = runner.cleanup()
    if read_input_preferences() != before:
        problems.append('Shared input preferences changed.')
    if hashlib.sha256(runtime.read_bytes()).hexdigest() != runtime_sha:
        problems.append('Player runtime changed during the run.')
    summary.update(passed=not problems, problems=problems, runtimeSha256=runtime_sha, removedProfiles=removed,
                   scope='Real Windows players on one PC: offline per court, and host + delayed client + spectator over '
                         'loopback with a real UDP delay and a real link cut. Not WAN, Relay, Android or human play.')
    (out / 'summary.json').write_text(json.dumps(summary, indent=2))
    print(json.dumps(dict(passed=not problems, problems=problems), indent=1), flush=True)
    return 1 if problems else 0


if __name__ == '__main__':
    raise SystemExit(main())
