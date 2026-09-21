"""Qualify canonical retained replay and halftime delivery on three real local players."""
import argparse,csv,hashlib,json,os,shutil,socket,subprocess,time
from pathlib import Path
from presentation_peer_link import PresentationLink,arguments as link_arguments
from run_unity_guarded import profile_root,unity_environment
from run_ui_player_review import read_input_preferences
ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('exe',type=Path);p.add_argument('--out',type=Path,required=True);p.add_argument('--mode',choices=['classic','hero'],default='classic');p.add_argument('--old-exe',type=Path);p.add_argument('--fault',choices=['none','missing','corrupt'],default='none');link_arguments(p);a=p.parse_args()
exe=a.exe.resolve();out=a.out.resolve()
assert exe.is_file() and exe.is_relative_to(ROOT/'Builds')
assert out.is_relative_to(ROOT/'Logs') and out!=ROOT/'Logs'
out.mkdir(parents=True,exist_ok=False)
with socket.socket(socket.AF_INET,socket.SOCK_DGRAM) as port_test:
 port_test.bind(('127.0.0.1',0));port=port_test.getsockname()[1]
processes=[];backups=[];before=read_input_preferences();commands=[]
startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
link=PresentationLink(ROOT,out,a)
try:
 join_port=link.start(port)
 for index,name in enumerate(['host','scorer','observer']):
  profile_name=out.name+'-'+name;profile=profile_root(['-tp-profile',profile_name])
  for source in profile.rglob('*'):
   if source.is_file() and source.suffix!='.log':
    backup=out/'private-profiles'/name/source.relative_to(profile);backup.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,backup)
    backups.append((source,backup,hashlib.sha256(source.read_bytes()).hexdigest()))
  command=[str(exe),'-batchmode','-screen-width','960','-screen-height','540','-screen-fullscreen','0','-tp-framecap','30','-tp-autostart','3','-tp-map','Eskinita','-tp-profile',profile_name,'-tp-replayseat',str(index),'-tp-replaytrace',str(out/(name+'.csv')),'-logFile',str(out/(name+'.log'))]
  command+=['-tp-replaymode',a.mode]
  if index==2 and a.fault!='none':command+=['-tp-replay-fault',a.fault]
  if index==0 and a.fault=='corrupt':command+=['-tp-replay-ready-peers','1']
  command+=['-tp-host',str(port)] if index==0 else ['-tp-join','127.0.0.1',str(join_port)]
  commands.append(command);processes.append(subprocess.Popen(command,cwd=ROOT,env=unity_environment(),startupinfo=startup,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL))
  print(name,'PID',processes[-1].pid,flush=True)
  (out/'job.json').write_text(json.dumps({'exe':str(exe),'port':port,'commands':commands,'pids':[x.pid for x in processes]},indent=2))
  if index==0:time.sleep(7)
  elif index==1:
   deadline=time.monotonic()+15
   while time.monotonic()<deadline:
    log=out/'scorer.log'
    if log.exists() and 'LocalSlot=1' in log.read_text(errors='replace'):break
    time.sleep(.25)
 if a.old_exe:
  old=a.old_exe.resolve();assert old.is_file() and old.is_relative_to(ROOT/'Builds')
  old_profile=profile_root(['-tp-profile',out.name+'-old'])
  for source in old_profile.rglob('*'):
   if source.is_file() and source.suffix!='.log':
    backup=out/'private-profiles'/'old'/source.relative_to(old_profile);backup.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,backup)
    backups.append((source,backup,hashlib.sha256(source.read_bytes()).hexdigest()))
  old_cmd=[str(old),'-batchmode','-tp-profile',out.name+'-old','-tp-join','127.0.0.1',str(port),'-tp-catchseat','3','-tp-catchtrace',str(out/'old.csv'),'-logFile',str(out/'old.log')]
  processes.append(subprocess.Popen(old_cmd,cwd=ROOT,env=unity_environment(),startupinfo=startup,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL));print('old protocol client PID',processes[-1].pid,flush=True)
 deadline=time.monotonic()+112
 for process in processes:process.wait(timeout=max(1,deadline-time.monotonic()))
finally:
 link.close()
 for process in processes:
  if process.poll() is None:process.terminate();process.wait(timeout=15)
 for source,backup,digest in backups:
  source.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(backup,source)
  assert hashlib.sha256(source.read_bytes()).hexdigest()==digest
unchanged=before==read_input_preferences();errors=[];measured={}
for seat,name in enumerate(['host','scorer','observer']):
 path=out/(name+'.csv');rows=list(csv.DictReader(path.open())) if path.exists() else []
 if len(rows)<20 or any(int(r['local'])!=seat for r in rows):errors.append(name+': insufficient trace or wrong seat');continue
 half=[r for r in rows if int(r['half'])==1]
 views=[r for r in half if int(r['view'])==1]
 if len(half)<60:errors.append(name+': shared halftime missing')
 if name=='observer' and a.fault!='none':
  if views or not any(int(r.get('fault','0')) and int(r['fallback']) for r in half):errors.append(name+': requested content fault did not produce truthful fallback')
 elif len(views)<20:errors.append(name+': canonical playback missing')
 if half and max(float(r['sim']) for r in half)-min(float(r['sim']) for r in half)>.04:errors.append(name+': live simulation advanced during halftime')
 if len({int(r['score1']) for r in half})!=1:errors.append(name+': score changed during replay')
 if not any(int(r['round'])==5 and int(r['held'])==0 for r in rows):errors.append(name+': halftime did not release into round5')
 epochs={int(r['epoch']) for r in rows if int(r['round'])>0}
 clip_ids={int(r['clip']) for r in half}
 if len(epochs)!=1 or min(epochs)<=0:errors.append(name+': invalid match identity')
 if len(clip_ids)!=1 or min(clip_ids)<=0:errors.append(name+': no canonical clip identity')
 measured[name]={'rows':len(rows),'epoch':list(epochs),'clip':list(clip_ids),'halfSamples':len(half),'viewSamples':len(views),'duration':float(half[-1]['real'])-float(half[0]['real']) if half else 0,'clockDrift':max((float(r['sim']) for r in half),default=0)-min((float(r['sim']) for r in half),default=0),'score1':int(rows[-1]['score1'])}
if len(measured)==3 and len({m['epoch'][0] for m in measured.values()})!=1:errors.append('peers disagree about match identity')
if len(measured)==3 and len({m['clip'][0] for m in measured.values() if m['clip']})!=1:errors.append('peers disagree about canonical clip')
if a.old_exe:
 old_log=(out/'old.log').read_text(errors='replace') if (out/'old.log').exists() else ''
 if 'version mismatch' not in old_log.lower():errors.append('old protocol client refusal not witnessed')
if not unchanged:errors.append('shared input preferences changed')
result={'passed':not errors,'errors':errors,'measured':measured,'fault':a.fault,'link':{'delayMs':a.delay,'jitterMs':a.jitter,'loss':a.loss,'seed':a.seed},'sharedInputUnchanged':unchanged,'scope':'Three real local Windows peers: physical contact retained in round1, bounded verified transport, canonical participant playback at round4, shared halftime clock and round5 return. Optional old44client refusal. Link settings and packet-count log specify simulated conditions; no WAN or freeform-play claim.'}
(out/'result.json').write_text(json.dumps(result,indent=2));print(json.dumps(result),flush=True)
raise SystemExit(0 if result['passed'] else 1)
