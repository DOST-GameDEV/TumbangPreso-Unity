from pathlib import Path
import hashlib,json,os,subprocess,sys,uuid
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'tools'))
import run_unity_job as jobs
from run_ui_player_review import read_input_preferences
from run_completed_arrival import restore_input
here=Path(__file__).resolve().parent
out=here/'startup-menu'
assert not out.exists()
out.mkdir()
build=json.loads((here/'artifact-receipt.json').read_text(encoding='utf-8-sig'))
exe=Path(build['artifact']);runtime=exe.parent/(exe.stem+'_Data/Managed/TumbangPreso.Runtime.dll')
sha=lambda:hashlib.sha256(runtime.read_bytes()).hexdigest()
assert build['passed'] and sha()==build['runtimeSha256']
name='startup-menu1003b-'+uuid.uuid4().hex[:10]
profile=Path(os.environ['USERPROFILE'])/'AppData/LocalLow/BH Studios/Tumbang Preso/profiles'/hashlib.sha256(name.encode()).hexdigest()
assert not profile.exists()
claim=jobs.make_claim(ROOT,'gpu',3072,2048,name,[],[])
acquired=False;before=None;result={'passed':False,'profile':name,'sourceCommit':build['sourceCommit'],'runtimeSha256':sha(),'scope':'Existing native menu-only route on fresh task-owned profile; no dedicated-session/lobby boot bypass, signup submission, SDK login or physical-input claim.'}
try:
 result['admission']=jobs.acquire(jobs.POOL,claim,300);acquired=True
 before=read_input_preferences()
 command=[sys.executable,str(ROOT/'tools/run_ui_player_review.py'),'--exe',str(exe),'--out',str(out),'--profile',name,'--menu-only']
 (out/'launch.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
 completed=subprocess.run(command,cwd=ROOT,timeout=450)
 result['exitCode']=completed.returncode
 inner=json.loads((out/'runner-result.json').read_text(encoding='utf-8-sig'))
 result['reviewPassed']=inner['reviewPassed']
 result['passed']=completed.returncode==0 and inner['reviewPassed']
finally:
 if before is not None:restore_input(before)
 result['inputRestored']=before is None or before==read_input_preferences()
 result['runtimeUnchanged']=sha()==build['runtimeSha256']
 result['passed'] &= result['inputRestored'] and result['runtimeUnchanged']
 if acquired:jobs.update_lease(jobs.POOL,claim['id'])
 (out/'guard-result.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
 print(json.dumps(result,indent=2))
sys.exit(0 if result['passed'] else 1)
