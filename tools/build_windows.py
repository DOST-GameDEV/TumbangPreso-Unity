"""Build current source with the installed Editor, verify identity and optionally launch it."""
import argparse
import datetime as dt
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import time

ROOT=Path(__file__).resolve().parents[1]
BUILDS=ROOT/'Builds'
OUTPUT=BUILDS/'Windows'
EXE=OUTPUT/'TumbangPreso.exe'


def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def git(*args):return subprocess.check_output(['git',*args],cwd=ROOT,text=True).strip()
def powershell(source):
    return subprocess.check_output(['powershell','-NoProfile','-Command',source],text=True).strip()
def processes():
    raw=powershell("@(Get-CimInstance Win32_Process | Where-Object { $_.Name -ieq 'Unity.exe' -or $_.Name -ieq 'TumbangPreso.exe' } | Select-Object ProcessId,ExecutablePath,CommandLine) | ConvertTo-Json -Compress")
    if not raw:return []
    parsed=json.loads(raw)
    return [parsed] if isinstance(parsed,dict) else parsed
def checked_build_path(path):
    resolved=path.resolve()
    if resolved.parent!=BUILDS.resolve() or path.is_symlink() or path.is_junction():
        raise RuntimeError('Build target escaped the canonical Builds directory: '+str(path))
    return resolved


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--launch',action='store_true',help='Open the freshly verified player')
    args=parser.parse_args()
    if sys.platform!='win32':raise SystemExit('This shortcut builds Windows on Windows.')
    import msvcrt
    (ROOT/'Logs').mkdir(exist_ok=True)
    build_lock=(ROOT/'Logs/windows-build.lock').open('a+b')
    if os.fstat(build_lock.fileno()).st_size==0:build_lock.write(b'0');build_lock.flush()
    build_lock.seek(0)
    try:msvcrt.locking(build_lock.fileno(),msvcrt.LK_NBLCK,1)
    except OSError:raise SystemExit('A build shortcut is already running. Read Logs/windows-build-current.json; do not start another.')
    if (ROOT/'.git').is_file():raise SystemExit('Use the canonical checkout, not a worktree, for playable builds.')
    for process in processes():
        path=process.get('ExecutablePath') or ''
        if re.search(r'\\Editor\\Unity\.exe$',path,re.I):
            raise SystemExit('An Editor is active; preserve it and finish/save/close that Editor before this batch build: '+str(process['ProcessId']))
        if Path(path).name.lower()=='tumbangpreso.exe':
            raise SystemExit('Close the running game before rebuilding/launching its replacement: '+str(process['ProcessId']))
    subprocess.run(['git','fetch','origin'],cwd=ROOT,check=True)
    if subprocess.run(['git','merge-base','--is-ancestor','origin/main','HEAD'],cwd=ROOT).returncode:
        raise SystemExit('Current branch lacks latest origin/main. Integrate deliberately before building; never reset dirty work.')
    version=re.search(r'^m_EditorVersion: (.+)$',(ROOT/'ProjectSettings/ProjectVersion.txt').read_text(),re.M)[1].strip()
    editor=Path(os.environ.get('UNITY_EDITOR_PATH',f'C:/Program Files/Unity/Hub/Editor/{version}/Editor/Unity.exe'))
    if not editor.is_file():raise SystemExit('Matching Editor is missing: '+str(editor))
    started=dt.datetime.now();tag=started.strftime('windows-build-%Y%m%d-%H%M%S')
    job=ROOT/'Logs'/tag;job.mkdir(parents=True,exist_ok=False)
    checked_build_path(OUTPUT)
    backup=checked_build_path(BUILDS/'.Windows-previous')
    if backup.exists():raise SystemExit('A preserved previous-build backup needs review: '+str(backup))
    if OUTPUT.exists():
        if not (OUTPUT/'UnityPlayer.dll').is_file():raise SystemExit('Existing output is not an identified player; preserve it.')
        shutil.move(str(OUTPUT),str(backup))
    owner=[
        'Assets/TumbangPreso/Resources/SceneryTextureIdentities.asset',
        'Assets/TumbangPreso/Resources/UI/brand/tump_logo.png.meta',
        'Assets/TumbangPreso/Resources/UI/owner-menu-edits/cloud-bank-a.png.meta']
    generated=['Assets/TumbangPreso/Resources/BuildBranch.txt','Assets/TumbangPreso/Resources/BuildIdentity.json',
        'Assets/StreamingAssets/build-identity.json','Assets/TumbangPreso/Resources/ShaderWarmup.shadervariants',
        'Assets/TumbangPreso/Art/ui/brand/owner_app_icon.png.meta']
    names=set(owner+generated+[name+'.meta' for name in generated if not name.endswith('.meta')])
    names.update(path.relative_to(ROOT).as_posix() for path in (ROOT/'ProjectSettings').glob('*.asset'))
    before={name:(ROOT/name).read_bytes() if (ROOT/name).is_file() else None for name in names}
    for name,data in before.items():
        if data is not None:
            saved=job/'source-before'/name;saved.parent.mkdir(parents=True,exist_ok=True);saved.write_bytes(data)
    command=[sys.executable,str(ROOT/'tools/run_unity_guarded.py'),'-batchmode','-force-d3d11','-quit',
        '-tp-profile',tag,'-executeMethod','TumbangPreso.EditorTools.GameBuilder.BuildWindows',
        '-buildOutput',str(EXE),'-logFile',str(job/'unity.log')]
    receipt={'status':'starting','started':started.isoformat(),'source_sha':git('rev-parse','HEAD'),
        'branch':git('branch','--show-current'),'editor':str(editor),'output':str(EXE),'command':command,
        'working_tree_before':git('status','--short'),'previous_build':str(backup) if backup.exists() else None}
    def save():
        (job/'job.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
        (ROOT/'Logs/windows-build-current.json').write_text(json.dumps({'job':str(job),'status':receipt['status'],'guard_pid':receipt.get('guard_pid'),'output':str(EXE)},indent=2)+'\n',encoding='utf-8')
    save();environment=dict(os.environ);environment['UNITY_EDITOR_PATH']=str(editor)
    try:
        with (job/'guard.log').open('w',encoding='utf-8') as stream:
            process=subprocess.Popen(command,cwd=ROOT,env=environment,stdout=stream,stderr=subprocess.STDOUT,
                creationflags=subprocess.CREATE_NO_WINDOW)
            receipt.update(status='building',guard_pid=process.pid);save()
            print(f'BUILDING current source {receipt["source_sha"][:9]} to {EXE}\nProgress: {job / "unity.log"}',flush=True)
            receipt['exit_code']=process.wait()
    finally:
        # The actual Editor/guard is terminal before restoring the exact pre-build bytes.
        if 'process' in locals() and process.poll() is None:
            print('Waiting for the owned Editor/guard before restoring source settings.',flush=True)
            process.wait()
        for name,data in before.items():
            path=ROOT/name
            if data is None:
                if path.is_file():path.unlink()
            else:
                path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(data)
                if path.read_bytes()!=data:raise RuntimeError('Source preservation failed: '+name)
        receipt['source_settings_preserved']=True;save()
    if receipt['exit_code']!=0 or '[Build] SUCCEEDED.' not in (job/'unity.log').read_text(encoding='utf-8',errors='replace'):
        receipt['status']='failed';save();raise SystemExit('Build failed. No old player launched. Inspect '+str(job/'unity.log'))
    identity_path=OUTPUT/'TumbangPreso_Data/StreamingAssets/build-identity.json'
    identity=json.loads(identity_path.read_text(encoding='utf-8-sig'))
    runtime=OUTPUT/'TumbangPreso_Data/Managed/TumbangPreso.Runtime.dll'
    required=[EXE,OUTPUT/'UnityPlayer.dll',OUTPUT/'TumbangPreso_Data/globalgamemanagers',runtime]
    if identity['sha']!=receipt['source_sha'] or identity['target']!='StandaloneWindows64':raise RuntimeError('Wrong source/platform identity')
    if dt.datetime.fromisoformat(identity['builtAt'])<started-dt.timedelta(seconds=2):raise RuntimeError('Stale build stamp')
    if not all(path.is_file() and path.stat().st_size>0 for path in required):raise RuntimeError('Incomplete player output')
    receipt.update(status='built',identity=identity,artifact_sha256={p.relative_to(OUTPUT).as_posix():sha(p) for p in required});save()
    print('FRESH BUILD VERIFIED: '+json.dumps(identity),flush=True)
    if args.launch:
        player=subprocess.Popen([str(EXE),'-logFile',str(job/'player.log')],cwd=OUTPUT,
            creationflags=subprocess.DETACHED_PROCESS|subprocess.CREATE_NEW_PROCESS_GROUP)
        receipt.update(status='launching',player_pid=player.pid);save()
        deadline=time.monotonic()+60;window=None
        while time.monotonic()<deadline:
            if player.poll() is not None:raise RuntimeError('New player exited during startup; inspect '+str(job/'player.log'))
            raw=powershell(f"Get-Process -Id {player.pid} -ErrorAction SilentlyContinue | Select-Object Id,Responding,MainWindowTitle,@{{n='MainWindowHandle';e={{$_.MainWindowHandle.ToInt64()}}}} | ConvertTo-Json -Compress")
            window=json.loads(raw) if raw else None
            if window and window['MainWindowHandle'] and window['Responding']:break
            time.sleep(2)
        else:raise RuntimeError('No responsive game window within 60 seconds')
        receipt.update(status='launched',window=window);save()
        # Update familiar play links only after the fresh player is running.
        links="""$reader=New-Object -ComObject WScript.Shell
$desktop=[Environment]::GetFolderPath('Desktop')
foreach($name in @('TUMP - Latest.lnk','TUMP - BugFix.lnk')) {
 $link=$reader.CreateShortcut((Join-Path $desktop $name));$link.TargetPath=$env:TUMP_FRESH_EXE;$link.WorkingDirectory=$env:TUMP_FRESH_DIR;$link.Arguments='';$link.Save()
}"""
        env=dict(os.environ,TUMP_FRESH_EXE=str(EXE),TUMP_FRESH_DIR=str(OUTPUT))
        subprocess.run(['powershell','-NoProfile','-Command',links],env=env,check=True)
        if backup.exists():checked_build_path(backup);shutil.rmtree(backup);receipt['previous_build_retired']=True;save()
        print('OPENED fresh player PID '+str(player.pid)+'; receipt '+str(job/'job.json'),flush=True)


if __name__=='__main__':main()
