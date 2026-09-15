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

import net_matrix
from run_unity_guarded import profile_root, unity_environment
from run_ui_player_review import read_input_preferences

ROOT=Path(__file__).resolve().parents[1]


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--exe',required=True,type=Path)
    parser.add_argument('--out',required=True,type=Path)
    parser.add_argument('--profile-prefix',required=True)
    parser.add_argument('--seconds',type=int,default=105)
    args=parser.parse_args()
    os.chdir(ROOT);exe=args.exe.resolve();output=args.out.resolve()
    if not exe.is_file() or not exe.is_relative_to(ROOT/'Builds'):raise ValueError('Use a verified internal build.')
    if not output.is_relative_to(ROOT/'Logs') or output==ROOT/'Logs':raise ValueError('Use a dedicated Logs subfolder.')
    if output.exists():raise FileExistsError('Preserve prior evidence; select a fresh output folder.')
    output.mkdir(parents=True)
    scenario=net_matrix.Scenario('demo clean direct','Both peers remain joined and progress to the next round.',seconds=args.seconds,direct=True)
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
                # Explicit host/join routes remain normal host/client topology;
                # batch mode suppresses external UGS sign-in for this local check.
                command+=['-batchmode','-tp-framecap','60']
                kwargs['env']=unity_environment()
                startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
                kwargs['startupinfo']=startup
            process=original_popen(command,*positional,**kwargs);processes.append(process);return process
        net_matrix.subprocess.Popen=launch
        print('Direct LAN check:',work,flush=True)
        result=net_matrix.run(scenario,str(exe),str(output),sys.executable)
        ok,faults=net_matrix.evaluate(result)
        for side in ['host','client']:
            data=result[side]
            if data is None:continue
            words=Path(data['path']).read_text(encoding='utf-8',errors='replace')
            mode=re.search(r'mode\s*:\s*(\S+)',words)
            if mode is None or mode.group(1)!='HeroStrike':faults.append(side+' did not use the expected default Hero Strike session.')
            if int(data.get('round',0))<2:faults.append(side+' did not cross a real round boundary.')
        receipt={'passed':ok and not faults,'faults':faults,'host':result['host'],'client':result['client'],
                 'artifact':str(exe),'runtimeSha256':hashlib.sha256((exe.parent/(exe.stem+'_Data')/'Managed/TumbangPreso.Runtime.dll').read_bytes()).hexdigest()}
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
    receipt['sharedInputUnchanged']=unchanged;receipt['passed'] &= unchanged
    (output/'result.json').write_text(json.dumps(receipt,indent=2),encoding='utf-8')
    print(json.dumps(receipt),flush=True)
    return 0 if receipt['passed'] else 1


if __name__=='__main__':raise SystemExit(main())
