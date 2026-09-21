"""Run the opt-in native UI review in a named profile without desktop input automation."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import winreg
import playerprefs_guard
from run_unity_guarded import unity_environment

ROOT=Path(__file__).resolve().parents[1]
PLAYER_KEY=r'Software\BH Studios\Tumbang Preso'


def read_input_preferences():
    values={}
    try:
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER,PLAYER_KEY) as key:
            for index in range(winreg.QueryInfoKey(key)[1]):
                name,value,kind=winreg.EnumValue(key,index)
                if playerprefs_guard.allowed(name):values[name]=playerprefs_guard.encode(value,kind)
    except FileNotFoundError:
        pass
    return values


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--exe',required=True)
    parser.add_argument('--out',required=True)
    parser.add_argument('--profile',required=True)
    parser.add_argument('--ordinary-skills',action='store_true',help='Capture all twelve default ordinary skills through accepted input and actual game audio.')
    parser.add_argument('--result-end-only',action='store_true',help='Run supported one-round/30-second bot matches to check natural result cleanup in both modes.')
    parser.add_argument('--whole-matches',action='store_true',help='Observe complete default eight-round Classic/Hero matches and sampled native screen/audio windows.')
    parser.add_argument('--frame-poll',action='store_true',help='Diagnostic control: search for results every frame instead of at 10 Hz.')
    parser.add_argument('--menu-only',action='store_true',help='Only qualify the changed startup/login/main-menu surfaces.')
    parser.add_argument('--recovery-only',action='store_true',help='Only qualify the menu-to-recovery input boundary.')
    parser.add_argument('--halftime-only',action='store_true',help='Capture full native halftime replay, standings and return in both modes.')
    parser.add_argument('--hero-replays',action='store_true',help='Add retained real-exchange replay captures to the six live ultimate route.')
    parser.add_argument('--live-ultimates-only',action='store_true',help='Capture six actual shared ultimate phases and live execution in the native player.')
    parser.add_argument('--review-caster-roster',action='store_true',help='Busy Hero review with Sean, Phaister, Nemu and Cheska and explicit staged full meters for overlap stress.')
    parser.add_argument('--review-muted',action='store_true',help='Record busy native play with master audio muted for visual communication review.')
    parser.add_argument('--review-low-comfort',action='store_true',help='Use Low, reduced motion, no shake/flash/announcer and 1080p in the busy native route.')
    parser.add_argument('--busy-exchange-only',action='store_true',help='Record native owner and live spectator views with four active bots and game audio in both modes.')
    parser.add_argument('--gameplay-only',action='store_true',help='Exercise shipped keyboard/mouse verbs in both modes with staged legal targets.')
    parser.add_argument('--spectator-only',action='store_true',help='Exercise the real spectator entry, camera controls and manual replay.')
    parser.add_argument('--introduction-bodies-only',action='store_true',help='Render six authored introduction body studies in the native player; no shared phase claim.')
    parser.add_argument('--introduction-scenes',action='store_true',help='Include the private scene effects in the introduction art route.')
    parser.add_argument('--map-surfaces-only',action='store_true',help='Inspect all native map finishes, matched quality views and isolated detail-cost windows.')
    args=parser.parse_args()
    exe=Path(args.exe).resolve();out=Path(args.out).resolve()
    if not exe.is_file() or not exe.is_relative_to(ROOT/'Builds'):
        raise SystemExit('Use an existing internal Builds/... player, never the Desktop build.')
    if not out.is_relative_to(ROOT/'Logs'):
        raise SystemExit('Review output must be inside this workspace Logs directory.')
    out.mkdir(parents=True,exist_ok=True)
    profile=Path(os.environ['USERPROFILE'])/'AppData/LocalLow/BH Studios/Tumbang Preso/profiles'/hashlib.sha256(args.profile.encode()).hexdigest()
    backup=out/'private-profile-backup';manifest={}
    if profile.exists():
        for source in profile.rglob('*'):
            if not source.is_file():continue
            relative=source.relative_to(profile);destination=backup/relative;destination.parent.mkdir(parents=True,exist_ok=True)
            shutil.copy2(source,destination);manifest[str(relative)]=hashlib.sha256(source.read_bytes()).hexdigest()
    before=read_input_preferences()
    (out/'native-input-before.json').write_text(json.dumps(before,indent=2))
    startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
    command=[str(exe),'-screen-fullscreen','0','-screen-width','1280','-screen-height','720',
             '-tp-profile',args.profile,'-tp-uireview',str(out),'-logFile',str(out/'player.log')]
    if args.map_surfaces_only:command.append('-tp-map-surfaces-only')
    if args.ordinary_skills:command.append('-tp-ordinary-skills')
    if args.result_end_only:command+=['-tp-whole-matches','-tp-result-end']
    if args.whole_matches:command.append('-tp-whole-matches')
    if args.frame_poll:command.append('-tp-review-frame-poll')
    if args.menu_only:command.append('-tp-menu-review-only')
    if args.recovery_only:command.append('-tp-recovery-review-only')
    if args.halftime_only:command.append('-tp-halftime-only')
    if args.hero_replays:command+=['-tp-live-ultimates-only','-tp-hero-replays']
    elif args.live_ultimates_only:command.append('-tp-live-ultimates-only')
    if args.review_caster_roster:command.append('-tp-review-caster-roster')
    if args.review_muted:command.append('-tp-review-muted')
    if args.review_low_comfort:command.append('-tp-review-low-comfort')
    if args.busy_exchange_only:command.append('-tp-busy-exchange-only')
    if args.gameplay_only:command.append('-tp-gameplay-review-only')
    if args.introduction_bodies_only:command.append('-tp-introduction-bodies-only')
    if args.introduction_scenes:command.append('-tp-introduction-scenes')
    if args.spectator_only:command.append('-tp-spectator-review-only')
    process=subprocess.Popen(command,cwd=ROOT,env=unity_environment(),startupinfo=startup)
    print('Started internal UI review, process',process.pid,flush=True)
    try:
        code=process.wait(timeout=2300 if args.whole_matches else 420)
    except subprocess.TimeoutExpired:
        process.terminate();process.wait(timeout=15);code=1
    finally:
        for relative,expected in manifest.items():
            target=profile/relative;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(backup/relative,target)
            if hashlib.sha256(target.read_bytes()).hexdigest()!=expected:raise RuntimeError('Named profile restore failed.')
    after=read_input_preferences();unchanged=before==after
    result_path=out/'result.json';result=json.loads(result_path.read_text()) if result_path.exists() else {'passed':False,'error':'No review receipt.'}
    receipt={'exitCode':code,'sharedInputUnchanged':unchanged,'profile':args.profile,'existingFilesRestored':len(manifest),
             'reviewPassed':result.get('passed',False),'error':result.get('error','')}
    (out/'runner-result.json').write_text(json.dumps(receipt,indent=2));print(json.dumps(receipt),flush=True)
    if not unchanged:print('Shared standalone input preferences changed; retained for investigation, not overwritten.',flush=True)
    return 0 if code==0 and unchanged and result.get('passed') else 1


if __name__=='__main__':
    raise SystemExit(main())
