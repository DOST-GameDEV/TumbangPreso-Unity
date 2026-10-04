import sys,json,hashlib,subprocess,time,urllib.request,csv
from pathlib import Path
root=Path(__file__).resolve().parents[2];sys.path.insert(0,str(root/'tools'))
import run_lan_peer as lan
import run_unity_guarded as preservation
import run_completed_arrival as restore
from run_ui_player_review import read_input_preferences
manifestPath=root/'Builds/integration-f4890bdd4-1004-manifest.json';manifest=json.loads(manifestPath.read_text());exe=Path(manifest['artifact'])
lan.checked_artifact(exe,manifestPath,146,manifest['runtimeSha256'])
control='http://192.168.1.144:18059/'
def peer(path,post=False):
 request=urllib.request.Request(control+path,data=b'' if post else None,method='POST' if post else 'GET')
 with urllib.request.urlopen(request) as response:return json.loads(response.read())
status=peer('status');assert status['source']==manifest['sourceCommit'] and not status['started'] and not status['runnerTerminal'] and status['pid'] is None
out=root/'Logs/paired-replay146-pc-direct1004';out.mkdir(exist_ok=False)
name='pc-paired-replay146-f4891004';profile=preservation.player_profile()/'profiles'/hashlib.sha256(name.encode()).hexdigest();profile.mkdir(exist_ok=False,parents=True)
seed=json.dumps(dict(PlayerName='ReplayHost',HubQueueChoice=2,CustomRulesWire='',CharacterPick=0,MatchDefaultsRevision=1,GraphicsQuality=1)).encode();(profile/'settings.json').write_bytes(seed)
before=read_input_preferences();(out/'pre-input-prefs.json').write_text(json.dumps(before));child=None
result={'source':manifest['sourceCommit'],'role':'HOST','protocol':146,'artifactManifestSha256':lan.file_sha256(manifestPath),'passed':False,'scope':'Two physical Windows machines, synthetic actual catch contact and retained replay transport/view; diagnostic round advance, no natural full-match/operator claim'}
try:
 command=[str(exe),'-batchmode','-force-d3d11','-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-tp-framecap','60','-tp-profile',name,'-tp-autostart','2','-tp-host','49163','-tp-map','Eskinita','-tp-replayseat','0','-tp-replaytrace',str(out/'host.csv'),'-tp-replaymode','hero','-tp-replay-contact','catch','-tp-replay-ready-peers','1','-logFile',str(out/'player.log')]
 startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
 child=subprocess.Popen(command,cwd=root,env=preservation.unity_environment(),startupinfo=startup);result.update(pid=child.pid,command=command);(out/'launch.json').write_text(json.dumps(result,indent=2));print('OWNED REPLAY HOST PID '+str(child.pid),flush=True)
 started=False
 while child.poll() is None:
  log=(out/'player.log').read_text(encoding='utf-8-sig',errors='replace') if (out/'player.log').exists() else ''
  if not started and '[Net] hosting on 49163' in log:
   result['clientStart']=peer('start',True);started=True;print('ARMED REPLAY CLIENT STARTED',flush=True)
  time.sleep(.5)
 result['exitCode']=child.returncode
 log=(out/'player.log').read_text(encoding='utf-8-sig',errors='replace');rows=list(csv.DictReader((out/'host.csv').open()));errors=[]
 half=[r for r in rows if r['half']=='1'];views=[r for r in half if r['view']=='1'];held=[r for r in half if r['held']=='1']
 if child.returncode!=0:errors.append('Nonzero exit')
 if not rows or len(rows[0])!=19 or any(r['local']!='0' for r in rows):errors.append('Missing exact19column host trace')
 if len(views)<20:errors.append('Insufficient actual canonical playback')
 if len(held)<20:errors.append('Shared held phase absent')
 if held and max(float(r['sim']) for r in held)-min(float(r['sim']) for r in held)>.04:errors.append('Live simulation advanced while held')
 for slot in range(4):
  if len({r['score'+str(slot)] for r in half})!=1:errors.append('Score'+str(slot)+' changed during halftime')
 if not any(r['round']=='5' and r['held']=='0' for r in rows):errors.append('No round5 release')
 if not (out/'host.png').exists():errors.append('No replay render PNG')
 if 'InvalidOperationException' in log or 'NullReferenceException' in log:errors.append('Runtime exception; inspect original log')
 result.update(passed=not errors,errors=errors,measured={'rows':len(rows),'halfSamples':len(half),'viewSamples':len(views),'heldSamples':len(held),'epochs':sorted({r['epoch'] for r in half}),'clips':sorted({r['clip'] for r in half}),'scores':{str(s):sorted({r['score'+str(s)] for r in half}) for s in range(4)},'pngExists':(out/'host.png').exists()},clientStatus=peer('status'))
finally:
 restore.restore_input(before);(profile/'settings.json').write_bytes(seed)
 result.update(terminal=child is None or child.poll() is not None,inputRestored=before==read_input_preferences(),profileRestored=(profile/'settings.json').read_bytes()==seed)
 lan.checked_artifact(exe,manifestPath,146,manifest['runtimeSha256']);result['artifactUnchanged']=True
 (out/'result.json').write_text(json.dumps(result,indent=2));print(json.dumps(result),flush=True)
