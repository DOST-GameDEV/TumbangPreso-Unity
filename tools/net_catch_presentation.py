"""Witness catch presentation on three real local peers without changing request validation."""
import argparse,csv,hashlib,json,os,shutil,socket,subprocess,time
from pathlib import Path
from run_unity_guarded import profile_root,unity_environment
from run_ui_player_review import read_input_preferences
ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('exe',type=Path);p.add_argument('--out',type=Path,required=True);a=p.parse_args()
exe=a.exe.resolve();out=a.out.resolve()
assert exe.is_file() and exe.is_relative_to(ROOT/'Builds')
assert out.is_relative_to(ROOT/'Logs') and out!=ROOT/'Logs'
out.mkdir(parents=True,exist_ok=False)
with socket.socket(socket.AF_INET,socket.SOCK_DGRAM) as port_test:
 port_test.bind(('127.0.0.1',0));port=port_test.getsockname()[1]
processes=[];backups=[];before=read_input_preferences();commands=[]
startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
try:
 for index,name in enumerate(['host','victim','observer']):
  profile_name=out.name+'-'+name;profile=profile_root(['-tp-profile',profile_name])
  for source in profile.rglob('*'):
   if source.is_file() and source.suffix!='.log':
    backup=out/'private-profiles'/name/source.relative_to(profile);backup.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,backup)
    backups.append((source,backup,hashlib.sha256(source.read_bytes()).hexdigest()))
  command=[str(exe),'-batchmode','-screen-width','960','-screen-height','540','-screen-fullscreen','0','-tp-framecap','30','-tp-autostart','3','-tp-map','Eskinita','-tp-profile',profile_name,'-tp-catchseat',str(index),'-tp-catchtrace',str(out/(name+'.csv')),'-logFile',str(out/(name+'.log'))]
  command+=['-tp-host',str(port)] if index==0 else ['-tp-join','127.0.0.1',str(port)]
  commands.append(command);processes.append(subprocess.Popen(command,cwd=ROOT,env=unity_environment(),startupinfo=startup,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL))
  print(name,'PID',processes[-1].pid,flush=True)
  (out/'job.json').write_text(json.dumps({'exe':str(exe),'port':port,'commands':commands,'pids':[x.pid for x in processes]},indent=2))
  if index==0:time.sleep(7)
  elif index==1:
   deadline=time.monotonic()+15
   while time.monotonic()<deadline:
    log=out/'victim.log'
    if log.exists() and 'LocalSlot=1' in log.read_text(errors='replace'):break
    time.sleep(.25)
 deadline=time.monotonic()+50
 for process in processes:process.wait(timeout=max(1,deadline-time.monotonic()))
finally:
 for process in processes:
  if process.poll() is None:process.terminate();process.wait(timeout=15)
 for source,backup,digest in backups:
  source.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(backup,source)
  assert hashlib.sha256(source.read_bytes()).hexdigest()==digest
unchanged=before==read_input_preferences();errors=[];measured={}
for seat,name in enumerate(['host','victim','observer']):
 path=out/(name+'.csv');rows=list(csv.DictReader(path.open())) if path.exists() else []
 if len(rows)<100 or any(int(r['local'])!=seat for r in rows):errors.append(name+': insufficient trace or wrong seat');continue
 seen=[r for r in rows if int(r['tags'])>0];playing=[r for r in rows if int(r['playing'])]
 if not seen:errors.append(name+': did not receive the accepted tag')
 if any(int(r['tags'])>1 for r in rows):errors.append(name+': duplicate tag event')
 if seat!=1 and playing:errors.append(name+': uninvolved/taya camera was taken over')
 if seat==1:
  if len(playing)<4:errors.append('victim: no sustained reconstructed contact')
  else:
   if any(float(r['stun'])<=.18 for r in playing):errors.append('victim: playback crossed the recovery deadline')
   after=[r for r in rows if float(r['real'])>float(playing[-1]['real'])+.1 and int(r['tags'])]
   if not after or int(after[0]['playing']) or float(after[0]['stun'])<=0:errors.append('victim: did not return inside real recovery')
 if seen and any(int(r['tayaCanAct'])==0 for r in seen):errors.append(name+': taya became unable to act')
 movement=[r for r in rows if 10<=float(r['elapsed'])<=11.1]
 travel=max((float(r['tayaZ']) for r in movement),default=0)-min((float(r['tayaZ']) for r in movement),default=0)
 if travel<.25:errors.append(name+': taya movement was not observed')
 measured[name]={'rows':len(rows),'tagSamples':len(seen),'playbackSamples':len(playing),'tayaTravel':travel}
if not unchanged:errors.append('shared input preferences changed')
result={'passed':not errors,'errors':errors,'measured':measured,'sharedInputUnchanged':unchanged,'scope':'Three real local Windows peers: taya, victim and uninvolved participant. No network spectator, link impairment or protocol change.'}
(out/'result.json').write_text(json.dumps(result,indent=2));print(json.dumps(result),flush=True)
raise SystemExit(0 if result['passed'] else 1)
