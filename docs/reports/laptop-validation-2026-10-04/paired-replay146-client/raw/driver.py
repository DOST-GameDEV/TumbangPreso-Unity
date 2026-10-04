from pathlib import Path
import hashlib,json,os,subprocess,sys,uuid
base=Path(__file__).resolve().parent
repo=Path('C:/Users/Matthew/dev/tump-integration1003')
sys.path.insert(0,str(repo/'tools'))
import net_matrix,run_completed_arrival as arrival
from run_ui_player_review import read_input_preferences
cfg=json.loads((base/'replay-pair-config.json').read_text())
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
command=[str(artifact/'TumbangPreso.exe'),'-batchmode','-force-d3d11','-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-tp-framecap','60','-tp-profile',name,'-tp-autostart','2','-tp-replaytrace',str(out/'replay.csv'),'-tp-replayseat','1','-tp-replaymode','hero','-tp-replay-contact','catch','-tp-replay-ready-peers','1','-logFile',str(out/'player.log'),'-tp-join','192.168.1.7',str(cfg['hostPort']),'-tp-map',cfg['map']]
env=dict(os.environ);env.setdefault('ALLUSERSPROFILE',env.get('ProgramData','C:/ProgramData'))
startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
child=None;result={'source':cfg['source'],'protocol':cfg['protocol'],'role':'client','profile':name,'profilePath':str(profile),'command':command,'expectedMap':cfg['map'],'scope':'Controlled capture/transfer/midpoint playback; forced intermediate rounds by existing probe, not normal full-match acceptance','execution':'Direct packaged game; normal two-peer start; no RAM/admission/external timeout wrapper'}
try:
    child=subprocess.Popen(command,cwd=repo,env=env,startupinfo=startup)
    result['pid']=child.pid;(out/'launch.json').write_text(json.dumps(result,indent=2));print(json.dumps(result),flush=True)
    result['exitCode']=child.wait()
    result['report']=net_matrix.parse_report(str(out/'state.txt')) if (out/'state.txt').exists() else None
    import csv
    rows=list(csv.DictReader((out/'replay.csv').open())) if (out/'replay.csv').exists() else []
    views=[x for x in rows if x.get('view')=='1']
    result['replayTrace']={'rows':len(rows),'viewRows':len(views),'schemaAllScores':bool(rows) and all(k in rows[0] for k in ['score0','score1','score2','score3']),'viewScores':[list(v) for v in sorted({tuple(int(x[k]) for k in ['score0','score1','score2','score3']) for x in views})] if views and all(k in views[0] for k in ['score0','score1','score2','score3']) else [],'viewSimTimes':sorted({x.get('sim') for x in views}),'clipIds':sorted({x.get('clip') for x in views}),'pngExists':(out/'replay.png').exists(),'lastRow':rows[-1] if rows else None}
    trace=result['replayTrace']
    result['localReplayChecks']={'normalExit':result.get('exitCode')==0,'actualViewObserved':bool(views),'allFourScoresObserved':trace['schemaAllScores'],'allFourViewScoresStable':len(trace['viewScores'])==1,'viewSimulationFrozen':bool(views) and len(trace['viewSimTimes'])==1 and all(x.get('held')=='1' for x in views),'oneVerifiedPlaybackIdentity':len(trace['clipIds'])==1 and trace['clipIds'][0]!='0','actualReplayPNG':trace['pngExists'],'probeAdvancedAfterSharedBreak':bool(rows) and any(x.get('round')=='5' for x in rows)}
    result['localReplayChecksPassed']=all(result['localReplayChecks'].values())
    result['career']=json.loads((profile/'career.json').read_text(encoding='utf-8-sig')) if (profile/'career.json').exists() else {}
finally:
    arrival.restore_input(before);(profile/'settings.json').write_bytes(seed)
    result['inputRestored']=before==read_input_preferences();result['seedRestored']=(profile/'settings.json').read_bytes()==seed
    result['terminal']=child is None or child.poll() is not None
    verify();result['artifactUnchanged']=True
    (out/'direct-result.json').write_text(json.dumps(result,indent=2));print(json.dumps(result),flush=True)
