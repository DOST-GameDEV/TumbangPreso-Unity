"""Short isolated native world-render baseline; does not rebuild the Desktop game."""
import argparse,csv,hashlib,json,os,shutil,subprocess,time
from pathlib import Path
from run_unity_guarded import profile_root

ROOT=Path(__file__).resolve().parents[1]

def main():
    parser=argparse.ArgumentParser();parser.add_argument('exe',type=Path);parser.add_argument('--out',type=Path,required=True)
    parser.add_argument('--graphics-api',choices=['d3d11','d3d12'],help='Explicit backend for the bounded native shutdown comparison.')
    args=parser.parse_args();exe=args.exe.resolve();folder=args.out.resolve()
    if not exe.is_file():raise FileNotFoundError(exe)
    folder.mkdir(parents=True,exist_ok=False)
    profile=profile_root(['-tp-profile','graphics-review-player']);backup=folder/'profile';manifest={}
    if profile.exists():
        for source in profile.rglob('*'):
            if not source.is_file() or source.suffix=='.log':continue
            relative=source.relative_to(profile);target=backup/relative;target.parent.mkdir(parents=True,exist_ok=True)
            shutil.copy2(source,target);manifest[str(relative)]=hashlib.sha256(source.read_bytes()).hexdigest()
    (folder/'profile-manifest.json').write_text(json.dumps(manifest,indent=2))
    startup=None
    if os.name=='nt':
        startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
    process=None
    try:
        command=[str(exe),'-screen-width','1920','-screen-height','1080','-screen-fullscreen','0',
            '-tp-profile','graphics-review-player','-tp-graphicsreport',str(folder),'-logFile',str(folder/'player.log')]
        if args.graphics_api:command.append('-force-'+args.graphics_api)
        process=subprocess.Popen(command,
            cwd=ROOT,stdout=subprocess.DEVNULL,stderr=subprocess.STDOUT,startupinfo=startup)
        print('Native graphics baseline PID'+str(process.pid)+' -> '+str(folder),flush=True)
        code=process.wait(timeout=180)
        path=folder/'world-render.csv'
        if code!=0 or not path.exists():raise RuntimeError('Native benchmark failed; inspect '+str(folder/'player.log'))
        rows=list(csv.DictReader(path.open()))
        # Draw Calls Count is unavailable in this release player. SetPass and
        # Triangles are supported; require both so a non-rendering batchmode
        # launch cannot be mistaken for a fast graphics result.
        coverage=json.loads((folder/'coverage.json').read_text())
        expected={(m,q) for m in coverage['maps'] for q in coverage['qualities']}
        actual={(row['map'],row['quality']) for row in rows}
        if not expected or actual!=expected or len(rows)!=len(expected) or any(float(row['mean_setpass'])<=0 or float(row['mean_triangles'])<=0 for row in rows):
            raise RuntimeError('Missing/unsupported rendering counter coverage')
        dll=exe.parent/(exe.stem+'_Data')/'Managed/TumbangPreso.Runtime.dll'
        identity={'exe':str(exe),'runtimeSha256':hashlib.sha256(dll.read_bytes()).hexdigest(),'rows':len(rows),'scope':'1920x1080 native world-camera rendering; not worst-case combat FPS'}
        (folder/'identity.json').write_text(json.dumps(identity,indent=2));print(json.dumps(identity,indent=2),flush=True)
        return 0
    finally:
        if process is not None and process.poll() is None:
            process.terminate()
            try:process.wait(timeout=8)
            except subprocess.TimeoutExpired:process.kill();process.wait()
        for relative,digest in manifest.items():
            target=profile/relative
            if not target.resolve().is_relative_to(profile.resolve()):raise ValueError('Profile target escaped named scope')
            target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(backup/relative,target)
            if hashlib.sha256(target.read_bytes()).hexdigest()!=digest:raise RuntimeError('Profile restoration mismatch')
        print('Restored '+str(len(manifest))+' named graphics-profile files; main profile untouched',flush=True)

if __name__=='__main__':raise SystemExit(main())
