from pathlib import Path
import sys,json,hashlib,subprocess
root=Path(__file__).resolve().parents[2];sys.path.insert(0,str(root/'tools'))
import run_unity_guarded as preservation
import playerprefs_guard
from run_ui_player_review import read_input_preferences
from run_completed_arrival import restore_input
kind='batched-release-menu-performance'
freshEffects=False
fixtureRoot=root/'Logs/replay-integration1007/fixtures-current-full-effects' if freshEffects else root/'Logs/replay-integration1007/fixtures'
out=root/'Logs/release-ui-perf1008g'/kind;out.mkdir(exist_ok=False)
exe=root/'Builds/release-ui-perf1008g/TumbangPreso.exe'
assert exe.is_file()
identity=json.loads((exe.parent/'TumbangPreso_Data/StreamingAssets/build-identity.json').read_text(encoding='utf-8-sig'))
source=json.loads((root/'Logs/release-ui-perf1008g/batched-release-run/source-classification.json').read_text())
assert identity['sha']==source['sourceCommit'] and not source['remainingSourceDeltas']
profile='pc-combined-replay-startup-'+identity['sha'][:9]+'-'+kind
folder=preservation.player_profile()/'profiles'/hashlib.sha256(profile.encode()).hexdigest()
assert not folder.exists();folder.mkdir(parents=True)
seed=b'{"PlayerName":"StartupPackageFixture","AccountHasPassword":false,"ReducedUiMotion":false,"MatchDefaultsRevision":1}'
(folder/'settings.json').write_bytes(seed)
before=read_input_preferences();editor=playerprefs_guard.read_editor()
(out/'input-before.json').write_text(json.dumps(before,indent=2));(out/'editor-input-before.json').write_text(json.dumps(editor,indent=2))
command=[str(exe),'-force-d3d11','-screen-fullscreen','0','-screen-width','1920','-screen-height','1080',
 '-tp-profile',profile,'-tp-uireview',str(out),'-tp-performance-only','-tp-performance-menus-only','-logFile',str(out/'player.log')]

receipt={'sourceCommit':identity['sha'],'protocol':identity['protocol'],'profile':profile,'exe':str(exe),'command':command,
 'execution':'hidden nonbatch graphics; synthetic UI raycast only, no OS mouse/keyboard/foreground interaction'}
child=None
try:
 startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
 child=subprocess.Popen(command,cwd=exe.parent,env=preservation.unity_environment(),startupinfo=startup)
 receipt['pid']=child.pid;(out/'launch.json').write_text(json.dumps(receipt,indent=2));print('OWNED HIDDEN STARTUP PLAYER PID '+str(child.pid),flush=True)
 child.wait();receipt['exitCode']=child.returncode
 receipt['scope']='Hidden ordinary player wall-clock menu timing without binary profiler; synthetic UI overhead remains included, no human or tournament approval';receipt['replayProbe']={'notApplicable':True}
 receipt['probe']=json.loads((out/'result.json').read_text()) if (out/'result.json').exists() else {'passed':False,'error':'No fresh package probe receipt'}
finally:
 assert child is None or child.poll() is not None
 restore_input(before);playerprefs_guard.restore_editor(editor)
 receipt['inputRestored']=before==read_input_preferences();receipt['editorInputRestored']=editor==playerprefs_guard.read_editor()
 pref=folder/'replay-folder.txt'
 assert pref.resolve().parent==folder.resolve()
 receipt['taskOwnedReplayPreferenceCreated']=pref.exists()
 if pref.exists():pref.unlink()
 (folder/'settings.json').write_bytes(seed);receipt['profileRestored']=(folder/'settings.json').read_bytes()==seed and not pref.exists()
 receipt['terminal']=child is None or child.poll() is not None
 (out/'runner-result.json').write_text(json.dumps(receipt,indent=2))
 print(json.dumps({k:receipt.get(k) for k in ['pid','exitCode','inputRestored','editorInputRestored','profileRestored','terminal']},ensure_ascii=True),flush=True);print(json.dumps({'passed':receipt.get('probe',{}).get('passed'),'error':receipt.get('probe',{}).get('error'),'windows':len(receipt.get('probe',{}).get('frameWindows',[]))}),flush=True)




