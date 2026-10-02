from pathlib import Path
import csv,hashlib,json,os,subprocess,sys,uuid
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT/'tools'))
import run_unity_job as jobs
import run_unity_guarded as guard
from run_ui_player_review import read_input_preferences
from run_completed_arrival import restore_input
here=Path(__file__).resolve().parent;out=here/'map-availability';assert not out.exists();out.mkdir()
build=json.loads((here/'artifact-receipt.json').read_text(encoding='utf-8-sig'));exe=Path(build['artifact'])
runtime=exe.parent/(exe.stem+'_Data/Managed/TumbangPreso.Runtime.dll');sha=lambda:hashlib.sha256(runtime.read_bytes()).hexdigest()
assert build['passed'] and sha()==build['runtimeSha256']
name='map-availability1003e-'+uuid.uuid4().hex[:10]
profile=guard.player_profile()/'profiles'/hashlib.sha256(name.encode()).hexdigest();assert not profile.exists()
claim=jobs.make_claim(ROOT,'gpu',3072,2048,name,[],[])
acquired=False;before=None;child=None
result=dict(passed=False,sourceCommit=build['sourceCommit'],runtimeSha256=sha(),profile=name,scope='Existing native graphics probe: all registered maps and graphics profiles, Classic parked actors. Staged render/availability check, not combat or physical-device/WAN qualification.')
try:
 result['admission']=jobs.acquire(jobs.POOL,claim,300);acquired=True;before=read_input_preferences()
 start=subprocess.STARTUPINFO();start.dwFlags|=subprocess.STARTF_USESHOWWINDOW;start.wShowWindow=0
 command=[str(exe),'-screen-width','1920','-screen-height','1080','-screen-fullscreen','0','-tp-profile',name,'-tp-graphicsreport',str(out),'-logFile',str(out/'player.log')]
 child=subprocess.Popen(command,cwd=ROOT,env=guard.unity_environment(),startupinfo=start);result['pid']=child.pid
 (out/'launch.json').write_text(json.dumps(result,indent=2),encoding='utf-8');print('Started owned all-map player',child.pid,flush=True)
 result['exitCode']=child.wait(timeout=360)
 coverage=json.loads((out/'coverage.json').read_text(encoding='utf-8-sig'))
 rows=list(csv.DictReader((out/'world-render.csv').open(encoding='utf-8-sig')))
 expected={(m,q) for m in coverage['maps'] for q in coverage['qualities']};actual={(r['map'],r['quality']) for r in rows}
 result.update(coverage=coverage,rows=len(rows),exactCoverage=bool(expected) and actual==expected and len(rows)==len(expected),renderCountersPositive=all(float(r['mean_setpass'])>0 and float(r['mean_triangles'])>0 for r in rows))
 result['passed']=result['exitCode']==0 and result['exactCoverage'] and result['renderCountersPositive']
except Exception as e:result['error']=str(e)
finally:
 if child is not None and child.poll() is None:
  child.terminate()
  try:child.wait(timeout=8)
  except subprocess.TimeoutExpired:child.kill();child.wait()
 if before is not None:restore_input(before)
 result['inputRestored']=before is None or before==read_input_preferences();result['runtimeUnchanged']=sha()==build['runtimeSha256']
 result['passed'] &= result['inputRestored'] and result['runtimeUnchanged']
 if acquired:jobs.update_lease(jobs.POOL,claim['id'])
 (out/'result.json').write_text(json.dumps(result,indent=2),encoding='utf-8');print(json.dumps(result,indent=2))
sys.exit(0 if result['passed'] else 1)
