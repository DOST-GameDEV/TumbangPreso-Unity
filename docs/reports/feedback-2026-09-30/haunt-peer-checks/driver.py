"""One direct two-process demo check with isolated, preserved player profiles."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys

sys.path.insert(0,'C:\\Users\\matth\\Documents\\Codex\\work\\tump-feedback-0930\\tools')
import net_matrix
import csv, math
from run_unity_guarded import profile_root, unity_environment
from run_ui_player_review import read_input_preferences

ROOT=Path('C:\\Users\\matth\\Documents\\Codex\\work\\tump-feedback-0930')



HAUNT_METRICS={}
def assess_haunt(result,output):
    faults=[]; traces={}
    for side in ['host','client']:
        p=output/(side+'.csv')
        if not p.is_file():faults.append(side+' missing trace');continue
        with p.open() as f:rows=list(csv.DictReader(f))
        if len(rows)<100:faults.append(side+' insufficient live fixture samples');continue
        traces[side]=rows
        try:
            expected_host='1' if side=='host' else '0'
            expected_local='0' if side=='host' else '1'
            if any(r['host']!=expected_host or r['local']!=expected_local for r in rows):
                faults.append(side+' lost network role or expected seat')
            phases=[int(r['ultPhase']) for r in rows]
            if max(phases)<=0:faults.append(side+' no accepted ultimate phase')
            if not any(r['devouring']=='1' for r in rows):faults.append(side+' chase never active')
            if rows[-1]['devouring']!='0' or rows[-1]['ultactive']!='0':faults.append(side+' chase did not complete')
            if any(int(r['fieldCount'])>0 for r in rows):faults.append(side+' old pull field exists')
            peaks={}
            for seat in range(4):
                values=[float(r['haunted'+str(seat)]) for r in rows]
                peaks[str(seat)]=max(values)
                if any(not math.isfinite(v) or v<0 or v>7.501 for v in values):faults.append(side+' invalid Haunted clock '+str(seat))
                if max(values)<=0:faults.append(side+' seat never Haunted '+str(seat))
                if values[-1]>.05:faults.append(side+' status did not clear '+str(seat))
            chase=[r for r in rows if r['devouring']=='1']
            span=max(float(r['x']) for r in chase)-min(float(r['x']) for r in chase) if chase else 0
            span+=max(float(r['z']) for r in chase)-min(float(r['z']) for r in chase) if chase else 0
            if span<2:faults.append(side+' no meaningful familiar movement')
            HAUNT_METRICS[side]={'samples':len(rows),'peaks':peaks,'finalPhase':phases[-1],'movementSpan':span,
                'timeStart':float(rows[0]['time']),'timeEnd':float(rows[-1]['time'])}
        except (KeyError,ValueError) as e:faults.append(side+' malformed trace '+str(e))
    if len(HAUNT_METRICS)==2 and HAUNT_METRICS['host']['finalPhase']!=HAUNT_METRICS['client']['finalPhase']:
        faults.append('final accepted phases disagree')
    for side in ['host','client']:
        log=Path(result['work'])/(side+'.log')
        if not log.exists():faults.append(side+' no process log');continue
        words=log.read_text(errors='replace')
        if any(x in words for x in ['NullReferenceException','MissingReferenceException','IndexOutOfRangeException','Game version mismatch']):
            faults.append(side+' hard runtime fault')
        if side=='client' and 'join admission: seat assigned' not in words:faults.append('client has no successful admission')
    return not faults,faults

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--exe',required=True,type=Path)
    parser.add_argument('--client-exe',type=Path,help='Optional second internal binary for a real version-compatibility check.')
    parser.add_argument('--expect-protocol-refusal',action='store_true')
    parser.add_argument('--out',required=True,type=Path)
    parser.add_argument('--profile-prefix',required=True)
    parser.add_argument('--seconds',type=int,default=105)
    parser.add_argument('--rematch',action='store_true')
    parser.add_argument('--mode',choices=['classic','hero'],default='hero')
    args=parser.parse_args()
    if not args.rematch and args.mode!='hero':parser.error('Mode selection is supported for the explicit rematch review.')
    os.chdir(ROOT);exe=args.exe.resolve();output=args.out.resolve()
    if not exe.is_file() or not exe.is_relative_to(ROOT/'Builds'):raise ValueError('Use a verified internal build.')
    client_exe=args.client_exe.resolve() if args.client_exe else exe
    if not client_exe.is_file() or not client_exe.is_relative_to(ROOT/'Builds'):raise ValueError('Use an internal client build.')
    if args.expect_protocol_refusal and (client_exe==exe or args.rematch):
        raise ValueError('Version refusal requires two different binaries and no rematch.')
    if not output.is_relative_to(ROOT/'Logs') or output==ROOT/'Logs':raise ValueError('Use a dedicated Logs subfolder.')
    if output.exists():raise FileExistsError('Preserve prior evidence; select a fresh output folder.')
    output.mkdir(parents=True)
    scenario=net_matrix.Scenario('demo protocol refusal' if args.expect_protocol_refusal else 'demo rematch '+args.mode if args.rematch else 'demo clean direct',
        'Both peers load and start the voted rematch map.' if args.rematch else 'Both peers remain joined and progress to the next round.',
        seconds=args.seconds,direct=True)
    work=(output/net_matrix.slug(scenario.name)).resolve()
    # The older runner removes its scenario folder. Verify the exact resolved
    # target before calling it, and require it to be new for this operation.
    if work.parent!=output or not work.is_relative_to(ROOT/'Logs') or work.exists():raise ValueError('Unsafe or reused scenario output.')
    snapshots=[];processes=[];before=read_input_preferences();original_popen=subprocess.Popen
    try:
        for side in ['host','client']:
            name=args.profile_prefix+'-'+side;profile=profile_root(['-tp-profile',name]);backup=output/'private-profile-backup'/side
            files={}
            if profile.exists():
                for source in profile.rglob('*'):
                    if not source.is_file():continue
                    relative=source.relative_to(profile);dest=backup/relative;dest.parent.mkdir(parents=True,exist_ok=True)
                    shutil.copy2(source,dest);files[str(relative)]=hashlib.sha256(source.read_bytes()).hexdigest()
            snapshots.append((profile,backup,files))
        def launch(command,*positional,**kwargs):
            command=list(command)
            if Path(command[0]).resolve()==exe:
                key=command.index('-tp-profile')+1
                command[key]=args.profile_prefix+('-host' if command[key]=='mtxhost' else '-client')
                if '-tp-join' in command:command[0]=str(client_exe)
                # Explicit host/join routes remain normal host/client topology;
                # batch mode suppresses external UGS sign-in for this local check.
                command+=['-batchmode','-tp-framecap','60']
                side='client' if '-tp-join' in command else 'host'
                command+=['-tp-familiartrace',str(output/(side+'.csv')),'-tp-familiarcase','haunt']
                if args.rematch:command+=['-tp-autorematch','-tp-review-rounds','1','-tp-review-seconds','30','-tp-review-mode',args.mode]
                kwargs['env']=unity_environment()
                startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
                kwargs['startupinfo']=startup
            process=original_popen(command,*positional,**kwargs);processes.append(process);return process
        net_matrix.subprocess.Popen=launch
        print('Direct LAN check:',work,flush=True)
        result=net_matrix.run(scenario,str(exe),str(output),sys.executable)
        if args.expect_protocol_refusal:
            faults=[]
            client_log=(work/'client.log').read_text(encoding='utf-8',errors='replace')
            if 'Game version mismatch' not in client_log:faults.append('The old client did not receive the explicit version refusal.')
            if result['host'] is None or result['client'] is None:faults.append('Both binaries must leave fresh diagnostic reports.')
            else:
                if result['host']['protocol']==result['client']['protocol']:faults.append('The supposed old client uses the same protocol.')
                if result['client'].get('role')=='CLIENT' and result['client'].get('networked')=='True':
                    faults.append('The old client still reports an accepted network session.')
            ok=not faults
        else:ok,faults=assess_haunt(result,output)
        for side in ['host','client']:
            if args.expect_protocol_refusal:break
            data=result[side]
            if data is None:continue
            words=Path(data['path']).read_text(encoding='utf-8',errors='replace')
            mode=re.search(r'mode\s*:\s*(\S+)',words)
            expected='Classic' if args.mode=='classic' else 'HeroStrike'
            if mode is None or mode.group(1)!=expected:faults.append(side+' did not use the expected '+expected+' session.')
            if args.rematch:
                log=(Path(data['path']).parent/(side+'.log')).read_text(encoding='utf-8',errors='replace')
                if '[NetAuto] Explicit review rules:' not in log:faults.append(side+' lacks explicit short-match configuration.')
                if '[NetAuto] REMATCH began after the peer vote.' not in log:faults.append(side+' did not observe the new rematch actually start.')
                if data.get('map')!='BayanPlaza':faults.append(side+' did not load the next court.')
                if int(data.get('round',0))!=1:faults.append(side+' is not in the new match first round.')
            elif int(data.get('round',0))<1:faults.append(side+' did not reach the ability fixture round.')
        receipt={'passed':ok and not faults,'faults':faults,'host':result['host'],'client':result['client'],
                 'artifact':str(exe),'clientArtifact':str(client_exe),'expectedProtocolRefusal':args.expect_protocol_refusal,
                 'runtimeSha256':hashlib.sha256((exe.parent/(exe.stem+'_Data')/'Managed/TumbangPreso.Runtime.dll').read_bytes()).hexdigest(),
                 'clientRuntimeSha256':hashlib.sha256((client_exe.parent/(client_exe.stem+'_Data')/'Managed/TumbangPreso.Runtime.dll').read_bytes()).hexdigest()}
    finally:
        net_matrix.subprocess.Popen=original_popen
        for process in processes:
            if process.poll() is None:process.terminate()
            process.wait(timeout=15)
        restored=0
        for profile,backup,files in snapshots:
            for relative,expected in files.items():
                dest=profile/relative
                if not dest.resolve().is_relative_to(profile.resolve()):raise ValueError('Profile destination escaped its scope.')
                dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(backup/relative,dest)
                if hashlib.sha256(dest.read_bytes()).hexdigest()!=expected:raise RuntimeError('Profile restore failed.')
                restored+=1
        unchanged=read_input_preferences()==before
        (output/'preservation.json').write_text(json.dumps({'existingFilesRestored':restored,'sharedInputUnchanged':unchanged},indent=2),encoding='utf-8')
    receipt['haunt']=HAUNT_METRICS
    receipt['scope']='Dedicated actual owner-client Haunt fixture; not whole-match/round2/rejoin qualification'
    receipt['expectedProtocol']=116
    receipt['sharedInputUnchanged']=unchanged;receipt['passed'] &= unchanged
    (output/'result.json').write_text(json.dumps(receipt,indent=2),encoding='utf-8')
    print(json.dumps(receipt),flush=True)
    return 0 if receipt['passed'] else 1


if __name__=='__main__':raise SystemExit(main())
