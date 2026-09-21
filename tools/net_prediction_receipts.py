"""Qualify canonical retained prediction and halftime delivery on three real local players."""
import argparse,csv,hashlib,json,os,shutil,socket,subprocess,time
from pathlib import Path
from run_unity_guarded import profile_root,unity_environment
from run_ui_player_review import read_input_preferences
ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('exe',type=Path);p.add_argument('--out',type=Path,required=True);p.add_argument('--mode',choices=['classic','hero'],default='hero');p.add_argument('--expect-baseline-defects',action='store_true');p.add_argument('--old-exe',type=Path);a=p.parse_args()
exe=a.exe.resolve();out=a.out.resolve()
assert exe.is_file() and exe.is_relative_to(ROOT/'Builds')
assert out.is_relative_to(ROOT/'Logs') and out!=ROOT/'Logs'
out.mkdir(parents=True,exist_ok=False)
with socket.socket(socket.AF_INET,socket.SOCK_DGRAM) as port_test:
 port_test.bind(('127.0.0.1',0));port=port_test.getsockname()[1]
processes=[];backups=[];before=read_input_preferences();commands=[]
startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
try:
 for index,name in enumerate(['host','scorer','observer']):
  profile_name=out.name+'-'+name;profile=profile_root(['-tp-profile',profile_name])
  for source in profile.rglob('*'):
   if source.is_file() and source.suffix!='.log':
    backup=out/'private-profiles'/name/source.relative_to(profile);backup.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,backup)
    backups.append((source,backup,hashlib.sha256(source.read_bytes()).hexdigest()))
  command=[str(exe),'-batchmode','-screen-width','960','-screen-height','540','-screen-fullscreen','0','-tp-framecap','30','-tp-autostart','3','-tp-map','Eskinita','-tp-profile',profile_name,'-tp-predictionseat',str(index),'-tp-predictiontrace',str(out/(name+'.csv')),'-logFile',str(out/(name+'.log'))]
  command+=['-tp-predictionmode',a.mode]
  command+=['-tp-host',str(port)] if index==0 else ['-tp-join','127.0.0.1',str(port)]
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
 for process in processes:
  if process.poll() is None:process.terminate();process.wait(timeout=15)
 for source,backup,digest in backups:
  source.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(backup,source)
  assert hashlib.sha256(source.read_bytes()).hexdigest()==digest
unchanged=before==read_input_preferences();errors=[];measured={}
for seat,name in enumerate(['host','scorer','observer']):
 path=out/(name+'.csv');rows=list(csv.DictReader(path.open())) if path.exists() else []
 if len(rows)<20 or any(int(r['local'])!=seat for r in rows):errors.append(name+': insufficient trace or wrong seat');continue
 newer=[r for r in rows if 5.4<=float(r['elapsed'])<=6.1]
 recall=[r for r in rows if 9.0<=float(r['elapsed'])<=10.3]
 if not newer or not recall:errors.append(name+': missing scenario windows')
 measured[name]={'rows':len(rows),'newerActive':any(int(r['active1'])==1 for r in newer),'recallCharges':max((int(r['charge2']) for r in recall),default=-1)}
if len(measured)==3:
 host=measured['host'];client=measured['scorer']
 defects=host['newerActive'] and not client['newerActive'] and host['recallCharges']==0 and client['recallCharges']==1
 if a.expect_baseline_defects:
  if not defects:errors.append('Both baseline refusal defects were not reproduced')
 elif not(host['newerActive'] and client['newerActive'] and host['recallCharges']==0 and client['recallCharges']==0):errors.append('A stale refusal cancelled a newer cast or a free recall refunded charge')
if a.old_exe:
 old_log=(out/'old.log').read_text(errors='replace') if (out/'old.log').exists() else ''
 if 'version mismatch' not in old_log.lower() and 'protocol 45' not in old_log.lower():errors.append('old protocol client refusal not witnessed')
if not unchanged:errors.append('shared input preferences changed')
result={'passed':not errors,'errors':errors,'measured':measured,'sharedInputUnchanged':unchanged,'scope':'Three real local Windows peers with explicit prediction and impossible-pose faults: stale refusal versus newer legal cast and refused free recall resource accounting. No freeform-play claim.'}
(out/'result.json').write_text(json.dumps(result,indent=2));print(json.dumps(result),flush=True)
raise SystemExit(0 if result['passed'] else 1)
