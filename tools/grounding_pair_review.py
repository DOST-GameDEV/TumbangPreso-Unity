"""Observe one visible-lobby host/client journey in matching native Windows players."""
import argparse,datetime as dt,hashlib,json,os,socket,subprocess,sys,time,uuid
from pathlib import Path
from run_unity_guarded import player_profile,unity_environment
from run_ui_player_review import read_input_preferences
import build_windows
import winreg
import playerprefs_guard
ROOT=Path(__file__).resolve().parents[1]

def window_preferences():
    values={}
    try:
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER,r'Software\BH Studios\Tumbang Preso') as key:
            for index in range(winreg.QueryInfoKey(key)[1]):
                name,value,kind=winreg.EnumValue(key,index)
                if name.lower().startswith(('screenmanager','unitygraphicsquality')):values[name]=(value,kind)
    except FileNotFoundError:pass
    return values

def restore_window_preferences(before):
    current=window_preferences()
    with winreg.CreateKey(winreg.HKEY_CURRENT_USER,r'Software\BH Studios\Tumbang Preso') as key:
        for name in current:
            if name not in before:winreg.DeleteValue(key,name)
        for name,(value,kind) in before.items():winreg.SetValueEx(key,name,0,kind,value)
    if window_preferences()!=before:raise RuntimeError('Window preference restore failed')

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--exe',required=True,type=Path)
    parser.add_argument('--out',required=True,type=Path)
    parser.add_argument('--map',required=True)
    parser.add_argument('--source-sha',required=True)
    parser.add_argument('--protocol',required=True,type=int)
    parser.add_argument('--seconds',type=int,default=145)
    parser.add_argument('--cast-nemu',action='store_true')
    args=parser.parse_args();exe=args.exe.resolve();out=args.out.resolve()
    assert exe.parent==ROOT/'Builds/Windows' and out.is_relative_to(ROOT/'Logs')
    if out.exists():raise RuntimeError('Use a new task-owned evidence folder')
    out.mkdir(parents=True)
    identity=json.loads((exe.parent/'TumbangPreso_Data/StreamingAssets/build-identity.json').read_text(encoding='utf-8-sig'))
    assert identity['sha']==args.source_sha and identity['protocol']==args.protocol
    before_hashes={str(p.relative_to(exe.parent)):build_windows.sha(p) for p in build_windows.player_artifacts(exe.parent)}
    with socket.socket(socket.AF_INET,socket.SOCK_DGRAM) as slot:slot.bind(('127.0.0.1',0));port=slot.getsockname()[1]
    profile_prefix='ground-'+dt.datetime.now().strftime('%Y%m%d-%H%M%S')+'-'+uuid.uuid4().hex[:5]
    prefs=read_input_preferences();windows=window_preferences();children=[];profiles=[];commands=[]
    def save(status):
        (out/'job.json').write_text(json.dumps({'status':status,'map':args.map,'identity':identity,'artifact_sha256':before_hashes,
            'port':port,'profiles':profiles,'commands':commands,'pids':[p.pid for p in children]},indent=2)+'\n',encoding='utf-8')
    try:
        for role,pick in [('host',4),('client',3)]:
            name=profile_prefix+'-'+role;profile=player_profile()/'profiles'/hashlib.sha256(name.encode()).hexdigest()
            if profile.exists():raise RuntimeError('Profile collision; preserve it')
            profile.mkdir(parents=True);profiles.append({'name':name,'path':str(profile)})
            settings={'PlayerToken':uuid.uuid4().hex,'PlayerName':'GROUND '+role.upper(),'CharacterPick':pick,'UseCustomCharacter':False,
                'CustomRulesWire':'1|0|8|90|0|3|3|1|0|0|0|0|12','MatchFormat':0,'MatchDefaultsRevision':1,
                'HubQueueChoice':2,'GraphicsQuality':2,'CinematicCameraMotion':True,'ReducedUiMotion':False,
                'MasterVolume':0.0,'SfxVolume':0.0,'MusicVolume':0.0,'AnnouncerVolume':0.0,'AmbienceVolume':0.0,'Fullscreen':False}
            (profile/'settings.json').write_text(json.dumps(settings),encoding='utf-8')
            evidence=out/role;evidence.mkdir()
            command=[str(exe),'-force-d3d11','-screen-fullscreen','0','-screen-width','960','-screen-height','540',
                '-tp-framecap','60','-tp-profile',name,'-tp-lobby','-tp-lobbyport',str(port if role=='host' else port+1),
                '-tp-map',args.map,'-tp-autostart','2','-tp-allbots','-tp-netreport',str(evidence/'state.txt'),
                '-tp-netseconds',str(args.seconds),'-tp-grounding-report',str(evidence),'-logFile',str(evidence/'player.log')]
            if role=='client':command+=['-tp-lobbyjoin','127.0.0.1:'+str(port)]
            elif args.cast_nemu:command+=['-tp-grounding-cast','nemu']
            startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
            process=subprocess.Popen(command,cwd=ROOT,env=unity_environment(),startupinfo=startup,creationflags=subprocess.CREATE_NO_WINDOW)
            children.append(process);commands.append(command);save('running');print(role,'PID',process.pid,flush=True)
            if role=='host':
                until=time.monotonic()+35
                while time.monotonic()<until:
                    text=(evidence/'player.log').read_text(errors='replace') if (evidence/'player.log').exists() else ''
                    if 'connected as seat 1' in text or 'hosting' in text.lower():break
                    if process.poll() is not None:raise RuntimeError('Host exited during startup')
                    time.sleep(.4)
        until=time.monotonic()+args.seconds+35
        while time.monotonic()<until and any(p.poll() is None for p in children):time.sleep(.5)
        codes=[p.poll() for p in children]
        if any(code is None for code in codes):raise RuntimeError('Native peer timed out')
        results={}
        for role in ['host','client']:
            path=out/role/'frames.jsonl'
            rows=[json.loads(line) for line in path.read_text().splitlines()] if path.exists() else []
            phases=sorted(set(r['phase'] for r in rows));seen_picks=sorted(set(a['pick'] for r in rows for a in r['actors']))
            suspects=[]
            for r in rows:
                if r['phase']!='play':continue
                for a in r['actors']:
                    offset=a['modelLocal']['y']-a['alignedLocal']['y']
                    if a['visible'] and a['grounded'] and not a['flying'] and not a['tripped'] and not a['possessed'] and offset>.25:
                        suspects.append({'time':r['time'],'slot':a['slot'],'offsetY':offset,'body':a['body'],'visual':a['visual'],'floor':a['floor']})
            results[role]={'frames':len(rows),'phases':phases,'picks':seen_picks,'suspects':suspects,'exitCode':codes[0 if role=='host' else 1],
                'error':(out/role/'error.txt').read_text() if (out/role/'error.txt').exists() else '',
                'ultimateThresholdObserved':any(r.get('ultimateThreshold') for r in rows)}
        after_hashes={str(p.relative_to(exe.parent)):build_windows.sha(p) for p in build_windows.player_artifacts(exe.parent)}
        assert before_hashes==after_hashes,'Artifact changed during peer review'
        unchanged=prefs==read_input_preferences()
        result={'scope':'Actual local Windows host/client through visible lobby; no physical-input, WAN or phone claim',
            'map':args.map,'sharedInputUnchanged':unchanged,'artifactUnchanged':True,'peers':results}
        (out/'result.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');save('complete')
        print(json.dumps({r:{k:v for k,v in x.items() if k!='suspects'}|{'suspectCount':len(x['suspects'])} for r,x in results.items()}),flush=True)
        return 0 if unchanged and all(x['frames'] and 'play' in x['phases'] and x['exitCode']==0 and not x['error'] for x in results.values()) else 1
    finally:
        for process in children:
            if process.poll() is None:process.terminate()
        for process in children:
            try:process.wait(timeout=15)
            except subprocess.TimeoutExpired:process.kill();process.wait()
        restore_window_preferences(windows)
        playerprefs_guard.restore_editor(prefs,key=r"Software\BH Studios\Tumbang Preso")

if __name__=='__main__':raise SystemExit(main())
