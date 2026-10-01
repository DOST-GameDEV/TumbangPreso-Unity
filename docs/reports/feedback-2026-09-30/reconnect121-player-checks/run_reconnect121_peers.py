import argparse,hashlib,json,os,re,shutil,subprocess,sys,time,uuid
from pathlib import Path
ROOT=Path(r'C:\Users\matth\Documents\Codex\work\tump-feedback-0930')
sys.path.insert(0,str(ROOT/'tools'))
import net_matrix
from run_unity_guarded import profile_root,unity_environment
from run_ui_player_review import read_input_preferences

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--exe',required=True,type=Path)
    parser.add_argument('--out',required=True,type=Path);parser.add_argument('--profile-prefix',required=True)
    parser.add_argument('--protocol',required=True,type=int)
    parser.add_argument('--host-seconds',type=int,default=195)
    parser.add_argument('--return-seconds',type=int,default=120)
    args=parser.parse_args();os.chdir(ROOT);exe=args.exe.resolve();out=args.out.resolve()
    if not exe.is_file() or not exe.is_relative_to(ROOT/'Builds'):raise ValueError('Use an internal player.')
    if not out.is_relative_to(ROOT/'Logs') or out.exists():raise ValueError('Use a fresh scoped Logs directory.')
    out.mkdir(parents=True);profiles={};backups=[];processes=[];before=read_input_preferences();result={}
    def words(name):
        p=out/(name+'.log');return p.read_text(errors='replace') if p.exists() else ''
    def wait_until(test,seconds,description):
        deadline=time.monotonic()+seconds
        while time.monotonic()<deadline:
            if test():return
            time.sleep(.1)
        raise RuntimeError('Timed out: '+description)
    def spawn(side,seconds):
        suffix='host' if side=='host' else 'client';profile=args.profile_prefix+'-'+suffix
        network=['-tp-host','18917'] if side=='host' else ['-tp-join','127.0.0.1','18917']
        command=[str(exe),'-batchmode',*network,'-tp-profile',profile,'-tp-autostart','2',
            '-tp-framecap','60','-screen-width','640','-screen-height','400','-screen-fullscreen','0',
            '-tp-netreport',str(out/(side+'.txt')),'-tp-netseconds',str(seconds),'-logFile',str(out/(side+'.log'))]
        startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
        p=subprocess.Popen(command,cwd=ROOT,env=unity_environment(),startupinfo=startup,
            stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL);processes.append(p)
        (out/'processes.json').write_text(json.dumps([p.pid for p in processes]))
        return p
    try:
        template=profile_root(['-tp-profile','feedback-haunt-peer-1001-client'])/'settings.json'
        defaults=json.loads(template.read_text(encoding='utf-8-sig'))
        for side in ['host','client']:
            profile=profile_root(['-tp-profile',args.profile_prefix+'-'+side]);profiles[side]=profile
            backup=out/'private-profile-backup'/side;files={}
            if profile.exists():
                for p in profile.rglob('*'):
                    if not p.is_file():continue
                    relative=p.relative_to(profile);dest=backup/relative;dest.parent.mkdir(parents=True,exist_ok=True)
                    shutil.copy2(p,dest);files[str(relative)]=hashlib.sha256(p.read_bytes()).hexdigest()
            backups.append((profile,backup,files));profile.mkdir(parents=True,exist_ok=True)
            data=dict(defaults);data['PlayerToken']=uuid.uuid4().hex;data['PlayerName']='Reconnect '+side
            for field in ['AccountPlayerId','AccountUsername','AccountDiscriminator','AccountEmail','AccountToken']:
                if field in data:data[field]=''
            data['CharacterPick']=0 if side=='host' else 2;data['CanPick']=0 if side=='host' else 1
            data['SlipperPick']=0 if side=='host' else 3
            (profile/'settings.json').write_text(json.dumps(data),encoding='utf-8')
        host=spawn('host',args.host_seconds)
        wait_until(lambda:'[NetBoot] host requested on 18917' in words('host') and ': listening' in words('host'),60,'host listening')
        first=spawn('client-first',95)
        wait_until(lambda:'join admission: seat assigned' in words('client-first'),50,'first admission')
        wait_until(lambda:'[Slice] round 1 begins' in words('host') and 'LocalSlot=1 spectator=False' in words('client-first'),50,'first active arena')
        time.sleep(4)
        first.terminate();first.wait(timeout=15)
        if first.poll() is None:raise RuntimeError('First client remains live.')
        wait_until(lambda:'[NetDisconnect] peer=1 local=0 server=True' in words('host') and '[Handover] seat 1' in words('host'),25,'host departure/takeover')
        settings=profiles['client']/'settings.json'
        if not settings.resolve().is_relative_to(profiles['client'].resolve()):raise RuntimeError('Profile edit escaped scope.')
        data=json.loads(settings.read_text(encoding='utf-8-sig'));identity=data['PlayerToken'];data['CharacterPick']=0
        settings.write_text(json.dumps(data),encoding='utf-8')
        result['identityUnchangedBeforeRelaunch']=json.loads(settings.read_text())['PlayerToken']==identity
        second=spawn('client-rejoined',args.return_seconds)
        wait_until(lambda:'join admission: seat assigned' in words('client-rejoined'),50,'return admission')
        wait_until(lambda:re.search(r'\[NetArrival\] peer=2 seat=1',words('host')) is not None,25,'new peer reclaims seat')
        second.wait(timeout=args.return_seconds+90);host.wait(timeout=args.host_seconds+90)
        h=net_matrix.parse_report(str(out/'host.txt'));c=net_matrix.parse_report(str(out/'client-rejoined.txt'));faults=[]
        if not result.get('identityUnchangedBeforeRelaunch',False):faults.append('Identity changed before relaunch')
        if h is None or c is None:faults.append('Missing fresh state report')
        else:
            if h['role']!='HOST' or c['role']!='CLIENT' or int(c['slot'])!=1:faults.append('Incorrect restored network role/seat')
            for side,d in [('host',h),('client',c)]:
                if int(d['protocol'])!=args.protocol or str(d['networked']).lower()!='true' or str(d['active']).lower()!='true':faults.append(side+' inactive/mismatched session')
                seat=next((x for x in d['seats'] if x['seat']==1),None)
                if seat is None or seat['char']!=2:faults.append(side+' lost fixed character')
            if h['mode']!=c['mode'] or h['map']!=c['map']:faults.append('World mode/map mismatch')
            if int(h['round'])<2 or int(c['round'])<2:faults.append('Both peers must progress beyond the original round')
            if h['round']!=c['round'] or h['defender']!=c['defender'] or h['structural']!=c['structural']:
                faults.append('Round/defender/structural state mismatch')
            own=next((x for x in c['seats'] if x['seat']==1),None)
            if own is None or own['bot']:faults.append('Returning client is still a bot-controlled seat')
        for name in ['host','client-first','client-rejoined']:
            if re.search(r'\w+Exception:',words(name)):faults.append(name+' hard exception')
        result.update(passed=not faults,faults=faults,host=h,rejoined=c,originalClientExit=first.returncode,
            scope='Current integration: abrupt client death, same-identity cold relaunch, fixed character and active round2 agreement; samples differ in time', protocol=args.protocol, hostReportSeconds=args.host_seconds, returnReportSeconds=args.return_seconds)
    except Exception as e:
        result.update(passed=False,error=str(e))
    finally:
        for p in processes:
            if p.poll() is None:p.terminate()
            try:p.wait(timeout=15)
            except subprocess.TimeoutExpired:p.kill();p.wait(timeout=15)
        restored=0
        for profile,backup,files in backups:
            for relative,expected in files.items():
                dest=profile/relative
                if not dest.resolve().is_relative_to(profile.resolve()):raise RuntimeError('Restore escaped scope')
                dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(backup/relative,dest)
                if hashlib.sha256(dest.read_bytes()).hexdigest()!=expected:raise RuntimeError('Profile restore failed')
                restored+=1
        unchanged=read_input_preferences()==before
        result['sharedInputUnchanged']=unchanged;result['passed']=result.get('passed',False) and unchanged
        (out/'preservation.json').write_text(json.dumps(dict(existingFilesRestored=restored,sharedInputUnchanged=unchanged),indent=2))
    result['runtimeSha256']=hashlib.sha256((exe.parent/(exe.stem+'_Data/Managed/TumbangPreso.Runtime.dll')).read_bytes()).hexdigest()
    (out/'result.json').write_text(json.dumps(result,indent=2));print(json.dumps(result),flush=True)
    return 0 if result['passed'] else 1
if __name__=='__main__':raise SystemExit(main())
