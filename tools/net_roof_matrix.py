"""Actual host/owner/observer roof recovery,using only named probe save profiles."""
import argparse,csv,hashlib,json,os,re,shutil,subprocess,sys,time
from pathlib import Path
from run_unity_guarded import profile_root

ROOT=Path(__file__).resolve().parents[1]
def rows(path):
    with path.open(newline='') as f:return [{k:float(v) for k,v in row.items()} for row in csv.DictReader(f)]

def evaluate(folder,rejoin,overlap_tag=False):
    data={name:rows(folder/(name+'.csv')) for name in ['host','owner','observer']}
    if rejoin:data['observer']+=rows(folder/'rejoined.csv')
    errors=[];measurements={}
    for name,records in data.items():
        records.sort(key=lambda r:r['time'])
        if len(records)<30 or any(r['local']!={'host':0,'owner':1,'observer':2}[name] for r in records):
            errors.append(name+' lacks a correctly seated nonzero trace');continue
        if any(r.get('map')!=3 for r in records):errors.append(name+' loaded a different map')
        lost=[r for r in records if not r['shoeActive']]
        trip=[r for r in records if r['trip']>0]
        if not lost or not trip:errors.append(name+' missed the actual loss/trip state');continue
        returned=[r for r in records if r['time']>lost[0]['time'] and r['shoeActive']]
        recovered=[r for r in records if r['time']>trip[0]['time'] and r['trip']==0 and r['stun']==0]
        if not returned or not recovered:errors.append(name+' never returned the shoe or cleared recovery');continue
        gap=returned[0]['time']-lost[0]['time'];down=recovered[0]['time']-trip[0]['time']
        maximum=max(r['mash'] for r in records)
        holding=[r for r in records if r['time']>returned[0]['time'] and r['holding']]
        if not 9.7<=gap<=10.6:errors.append(name+f' shoe delay was {gap:.3f}s')
        if not 0<down<5:errors.append(name+f' recovery took {down:.3f}s; auto-recovery is not mash evidence')
        if maximum<8:errors.append(name+' did not observe enough accepted mash presses')
        if len(holding)<4:errors.append(name+' never observed the returned shoe being picked up')
        if name in ['host','owner'] and min(r['y'] for r in records)>-.35:errors.append(name+' recorded no real descent')
        measurements[name]={'shoeDelay':gap,'downSeconds':down,'acceptedPresses':maximum,'postReturnHeldSamples':len(holding),'minimumY':min(r['y'] for r in records)}
        if overlap_tag:
            tagged=[r for r in records if r['stun']>0]
            free=[r for r in records if tagged and r['time']>tagged[0]['time'] and r['stun']==0]
            duration=free[0]['time']-tagged[0]['time'] if free else -1
            measurements[name]['independentTagSeconds']=duration
            if not 3.8<=duration<=4.4:errors.append(name+f' tag timer changed during trip recovery: {duration:.3f}s')
    if rejoin:
        joined=rows(folder/'rejoined.csv');hidden=[r for r in joined if not r['shoeActive']]
        if len(hidden)<3:errors.append('Rejoined observer missed the still-unavailable slipper')
        measurements['rejoin']={'unavailableSamples':len(hidden),'tripSamples':sum(r['trip']>0 for r in joined)}
    return {'ok':not errors,'errors':errors,'measurements':measurements}

def evaluate_swim(folder,rejoin):
    data={name:rows(folder/(name+'.csv')) for name in ['host','owner','observer']}
    if rejoin:data['observer']+=rows(folder/'rejoined.csv')
    errors=[];measurements={}
    for name,records in data.items():
        records.sort(key=lambda r:r['time'])
        if len(records)<30 or any(r['local']!={'host':0,'owner':1,'observer':2}[name] for r in records):
            errors.append(name+' lacks correctly seated swim samples');continue
        if any(r.get('map')!=3 for r in records):errors.append(name+' loaded a different map')
        wet=[r for r in records if r.get('swimming',0)]
        floating=[r for r in records if r.get('floating',0)]
        held_after=[r for r in records if floating and r['time']>floating[0]['time'] and r['holding']]
        exited=[r for r in held_after if not r['swimming'] and r['z']<-1.5 and r['y']>0]
        motion=[r for r in wet if r.get('swimAnimation',0)]
        if len(wet)<20:errors.append(name+' never spent a sustained interval swimming')
        if len(floating)<8:errors.append(name+' never saw the loose slipper float')
        if len(held_after)<4:errors.append(name+' missed the actual floating pickup')
        if len(exited)<4:errors.append(name+' never exited via the steps holding the retrieved slipper')
        if len(motion)<10:errors.append(name+' did not select its real serialized swimming animation')
        if any(not r['shoeActive'] or r['trip']>0 for r in records):errors.append(name+' treated accessible water as an off-roof loss/fall')
        if wet and min(r['y'] for r in wet)<-1.2:errors.append(name+' swimmer hit the basin floor')
        if name=='owner' and wet and min(r['eyeY'] for r in wet)<.10:errors.append('Owner eye submerged during the swim')
        measurements[name]={'swimSamples':len(wet),'floatingSamples':len(floating),'animatedSamples':len(motion),
            'postFloatHeldSamples':len(held_after),'dryExitHeldSamples':len(exited),'minimumY':min(r['y'] for r in records)}
    if rejoin:
        joined=rows(folder/'rejoined.csv');wet=sum(r.get('swimming',0)>0 for r in joined)
        if wet<3:errors.append('Rejoined observer did not restore the active swimmer')
        measurements['rejoin']={'swimmingSamples':wet}
    return {'ok':not errors,'errors':errors,'measurements':measurements}

def validate_capture(folder,rejoin,result):
    for role in ['owner','observer']:
        names=[role]+(['rejoined'] if rejoin and role=='observer' else [])
        total=0;missing=0;segments={}
        for name in names:
            timing=folder/(name+'-frames')/'frames.csv'
            with timing.open(newline='') as handle:frames=list(csv.DictReader(handle))
            absent=sum(not (timing.parent/(f"{int(row['frame']):05d}.jpg")).is_file() for row in frames)
            missing+=absent;total+=len(frames)-absent;segments[name]=len(frames)-absent
        if total<60 or missing:result['errors'].append(role+' has missing or insufficient actual-player frames')
        result['measurements'][role]['capturedFrames']=total
        result['measurements'][role]['captureSegments']=segments
    result['ok']=not result['errors']

def main():
    p=argparse.ArgumentParser();p.add_argument('exe',type=Path);p.add_argument('--mode',choices=['classic','hero'],default='classic')
    p.add_argument('--delay',type=float,default=0);p.add_argument('--rejoin',action='store_true');p.add_argument('--out',type=Path,required=True)
    p.add_argument('--capture',action='store_true');p.add_argument('--overlap-tag',action='store_true')
    p.add_argument('--case',choices=['fall','swim'],default='fall')
    a=p.parse_args();exe=a.exe.resolve();assert exe.is_file(),exe
    folder=a.out.resolve();folder.mkdir(parents=True,exist_ok=False)
    profiles={name:'roof-review-'+name for name in ['host','owner','observer']}
    manifest=[];processes=[];handles=[]
    for role,profile in profiles.items():
        root=profile_root(['-tp-profile',profile]);backup=folder/'profiles'/role
        if not root.exists():continue
        for source in root.rglob('*'):
            if not source.is_file() or source.suffix=='.log':continue
            rel=source.relative_to(root);dest=backup/rel;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,dest)
            manifest.append((root,rel,dest,hashlib.sha256(source.read_bytes()).hexdigest()))
    (folder/'profiles-manifest.json').write_text(json.dumps([{'root':str(root),'relative':str(rel),'backup':str(backup),'sha256':digest} for root,rel,backup,digest in manifest],indent=2))
    startup=None
    if os.name=='nt':
        startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
    def launch(args,stdout=subprocess.DEVNULL):
        proc=subprocess.Popen(args,cwd=ROOT,stdout=stdout,stderr=subprocess.STDOUT,startupinfo=startup);processes.append(proc);return proc
    try:
        common=[str(exe),'-batchmode','-screen-width','1280' if a.capture else '640','-screen-height','720' if a.capture else '360','-screen-fullscreen','0','-tp-map','SaBubong','-tp-autostart','3','-tp-roofcase',a.case]
        if a.overlap_tag:common+=['-tp-rooftag']
        def peer(name,route,role=None):
            capture=['-tp-roofshots',str(folder/(name+'-frames'))] if a.capture and name!='host' else []
            return launch(common+route+capture+['-tp-profile',profiles[role or name],'-tp-roofmode',a.mode,'-tp-rooftrace',str(folder/(name+'.csv')),'-logFile',str(folder/(name+'.log'))])
        host=peer('host',['-tp-host','8960']);time.sleep(7)
        port='8960'
        if a.delay:
            port='8961';handle=open(folder/'link.log','w');handles.append(handle)
            launch([sys.executable,str(ROOT/'tools/net_link.py'),'--listen',port,'--to','127.0.0.1:8960','--delay',str(a.delay),'--seconds','120'],handle);time.sleep(1)
        owner=peer('owner',['-tp-join','127.0.0.1',port]);deadline=time.monotonic()+35
        while time.monotonic()<deadline:
            log=folder/'owner.log';value=log.read_text(errors='replace') if log.exists() else ''
            if re.search(r'(?:seat changed|arena installed): LocalSlot=1[^\n]*host=False',value):break
            if owner.poll() is not None:raise RuntimeError('Owner exited before seating')
            time.sleep(.25)
        else:raise RuntimeError('Owner did not take seat1')
        observer=peer('observer',['-tp-join','127.0.0.1','8960']);print('Running actual roof host/owner/observer: '+str(folder),flush=True)
        rejoined=None;deadline=time.monotonic()+100
        while host.poll() is None and time.monotonic()<deadline:
            if a.rejoin and rejoined is None:
                try:lost=any(r.get('floating',0) if a.case=='swim' else not r['shoeActive'] for r in rows(folder/'owner.csv'))
                except (OSError,ValueError,TypeError):lost=False
                if lost:
                    observer.terminate();observer.wait(timeout=8)
                    rejoined=peer('rejoined',['-tp-join','127.0.0.1','8960'],'observer')
                    print('Rejoining observer during '+('active swimming' if a.case=='swim' else 'the ten-second slipper loss'),flush=True)
            time.sleep(.5)
        result=evaluate_swim(folder,a.rejoin) if a.case=='swim' else evaluate(folder,a.rejoin,a.overlap_tag)
        if a.capture:
            validate_capture(folder,a.rejoin,result)
        dll=exe.parent/(exe.stem+'_Data')/'Managed/TumbangPreso.Runtime.dll'
        result.update(mode=a.mode,case=a.case,delayOneWayMs=a.delay,runtimeSha256=hashlib.sha256(dll.read_bytes()).hexdigest(),exe=str(exe))
        (folder/'result.json').write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2),flush=True)
        return 0 if result['ok'] else 1
    finally:
        for proc in processes:
            if proc.poll() is None:proc.terminate()
        for proc in processes:
            try:proc.wait(timeout=8)
            except subprocess.TimeoutExpired:proc.kill();proc.wait()
        for handle in handles:handle.close()
        for root,rel,backup,digest in manifest:
            dest=root/rel
            if not dest.resolve().is_relative_to(root.resolve()):raise ValueError('Profile restore escaped named scope')
            dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(backup,dest)
            if hashlib.sha256(dest.read_bytes()).hexdigest()!=digest:raise RuntimeError('Profile restore mismatch')
        print(f'Preserved {len(manifest)} files in the three named probe profiles; main profile untouched',flush=True)
if __name__=='__main__':raise SystemExit(main())
