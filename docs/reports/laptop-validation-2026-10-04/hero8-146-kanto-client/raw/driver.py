from pathlib import Path
import hashlib,json,os,subprocess,sys,uuid
base=Path(__file__).resolve().parent
repo=Path('C:/Users/Matthew/dev/tump-integration1003')
sys.path.insert(0,str(repo/'tools'))
import net_matrix,run_completed_arrival as arrival
from run_ui_player_review import read_input_preferences
cfg=json.loads((base/'true-hero8-config.json').read_text())
manifest=json.loads(Path(cfg['manifest']).read_bytes());artifact=Path(cfg['artifact'])
assert manifest['sourceCommit']==cfg['source'] and manifest['protocol']==cfg['protocol']
def verify():
    expected={r['path'] for r in manifest['files']}
    assert {p.relative_to(artifact).as_posix() for p in artifact.rglob('*') if p.is_file()}==expected
    for r in manifest['files']:
        p=artifact/r['path'];assert p.stat().st_size==r['bytes']
        h=hashlib.sha256()
        with p.open('rb') as f:
            while chunk:=f.read(1024*1024):h.update(chunk)
        assert h.hexdigest()==r['sha256'],r['path']
verify()
out=Path(cfg['out']);out.mkdir(parents=True,exist_ok=False)
name=cfg['profile'];token=uuid.uuid4().hex
profile=Path(os.environ['USERPROFILE'])/'AppData/LocalLow/BH Studios/Tumbang Preso/profiles'/hashlib.sha256(name.encode()).hexdigest()
profile.mkdir(parents=True,exist_ok=False)
seed=json.dumps(dict(PlayerToken=token,PlayerName='Hero8Laptop',CharacterPick=2,CustomRulesWire='',HubQueueChoice=2,GraphicsQuality=1,MatchDefaultsRevision=1)).encode()
(profile/'settings.json').write_bytes(seed)
before=read_input_preferences();(out/'pre-input-prefs.json').write_text(json.dumps(before))
command=[str(artifact/'TumbangPreso.exe'),'-batchmode','-force-d3d11','-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-tp-framecap','60','-tp-profile',name,'-tp-autostart','2','-tp-netreport',str(out/'state.txt'),'-tp-netseconds','1500','-logFile',str(out/'player.log'),'-tp-lobbyjoin','192.168.1.7:'+str(cfg['hostPort']),'-tp-lobbyport',str(cfg['hostPort']+1)]
env=dict(os.environ);env.setdefault('ALLUSERSPROFILE',env.get('ProgramData','C:/ProgramData'))
startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
child=None;result={'source':cfg['source'],'protocol':cfg['protocol'],'role':'client','profile':name,'profilePath':str(profile),'command':command,'expectedMap':cfg['map'],'expectedRules':'1|0|8|90|0|3|0|1|0|0|0','execution':'Direct packaged game; normal two-peer start; no RAM/admission/external timeout wrapper'}
try:
    child=subprocess.Popen(command,cwd=repo,env=env,startupinfo=startup)
    result['pid']=child.pid;(out/'launch.json').write_text(json.dumps(result,indent=2));print(json.dumps(result),flush=True)
    result['exitCode']=child.wait()
    result['report']=net_matrix.parse_report(str(out/'state.txt')) if (out/'state.txt').exists() else None
    result['career']=json.loads((profile/'career.json').read_text(encoding='utf-8-sig')) if (profile/'career.json').exists() else {}
finally:
    arrival.restore_input(before);(profile/'settings.json').write_bytes(seed)
    result['inputRestored']=before==read_input_preferences();result['seedRestored']=(profile/'settings.json').read_bytes()==seed
    result['terminal']=child is None or child.poll() is not None
    verify();result['artifactUnchanged']=True
    (out/'direct-result.json').write_text(json.dumps(result,indent=2));print(json.dumps(result),flush=True)
