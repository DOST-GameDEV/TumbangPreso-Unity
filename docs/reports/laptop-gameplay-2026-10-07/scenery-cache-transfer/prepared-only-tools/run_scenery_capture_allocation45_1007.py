from pathlib import Path
import base64, hashlib, importlib.util, json, os, subprocess, sys, tarfile, tempfile, winreg
import xml.etree.ElementTree as ET
s=Path('C:/Users/Matthew/dev/tump-laptop-gameplay-next1003')
w=Path('C:/Users/Matthew/dev/tump-workers1003/qa-a')
p=Path('work/scenery-capture-allocation1007')
kind='candidate'; m=json.loads((p/'packet.json').read_text(encoding='utf-8-sig')); ref=m['base']
o=w/'Logs/scenery-capture-allocation45-direct1007'; o.mkdir()
marker=json.loads((w/'.tump-validation-worker.json').read_text())
spec=importlib.util.spec_from_file_location('prep',s/'tools/prepare_unity_test_workers.py')
prep=importlib.util.module_from_spec(spec); spec.loader.exec_module(prep)
files={}
if kind=='original':
    # Drain git completely to disk, avoiding the previous stream-padding deadlock.
    with tempfile.TemporaryFile() as spool:
        subprocess.run(['git','-C',str(s),'archive',ref,'Assets','Packages','ProjectSettings'],stdout=spool,check=True)
        spool.seek(0)
        with tarfile.open(fileobj=spool,mode='r|') as ar:
            for member in ar:
                if not member.isfile(): continue
                data=ar.extractfile(member).read(); target=w/member.name
                if member.name=='ProjectSettings/ProjectSettings.asset':
                    data=prep.replace_identity(data,marker['companyName'],marker['productName'])
                if target.exists() and target.read_bytes()!=data:
                    backup=o/'preserved-previous-inputs'/member.name
                    backup.parent.mkdir(parents=True,exist_ok=True); backup.write_bytes(target.read_bytes())
                target.parent.mkdir(parents=True,exist_ok=True)
                if not target.exists() or target.read_bytes()!=data: target.write_bytes(data)
                files[member.name]=hashlib.sha256(data).hexdigest()
                if len(files)%5000==0: print('Prepared',len(files),'immutable inputs',flush=True)
    for root in ['Runtime','Editor','Tests']:
        for old in (w/'Assets/TumbangPreso'/root).rglob('*.cs'):
            if old.relative_to(w).as_posix() in files: continue
            for item in [old,Path(str(old)+'.meta')]:
                if item.exists():
                    dest=o/'preserved-previous-overlays'/item.relative_to(w)
                    dest.parent.mkdir(parents=True,exist_ok=True); item.rename(dest)
else:
    prior=w/'Logs/scenery-capture-allocation44-direct1007'
    receipt=json.loads((prior/'classified-result.json').read_text())
    assert receipt['terminal'] and receipt['allInputsRestored']
    files=json.loads((prior/'qualified-source.json').read_text())['files']
    for path,h in files.items(): assert hashlib.sha256((w/path).read_bytes()).hexdigest()==h,path
overlays=json.loads((p/'overlays.json').read_text(encoding='utf-8-sig'))
for path,name in overlays.items():
    target=w/path;data=(p/name).read_bytes();target.parent.mkdir(parents=True,exist_ok=True)
    target.write_bytes(data);files[path]=hashlib.sha256(data).hexdigest()
(o/'qualified-source.json').write_text(json.dumps({'sourceCommit':ref,'kind':'candidate','files':files,'packet':m,'identity':marker},indent=2))
for path in files:
    if path.endswith(('.meta','.asset','.mat','.prefab','.unity','.asmdef')) or path.startswith('ProjectSettings/'):
        b=o/'prejob-restorable'/path; b.parent.mkdir(parents=True,exist_ok=True); b.write_bytes((w/path).read_bytes())
key_path='Software\\Unity\\UnityEditor\\'+marker['companyName']+'\\'+marker['productName']
prefs=[]
try:
    with winreg.OpenKey(winreg.HKEY_CURRENT_USER,key_path) as key:
        i=0
        while True:
            try: name,value,typ=winreg.EnumValue(key,i)
            except OSError: break
            prefs.append((name,base64.b64encode(value).decode() if isinstance(value,bytes) else value,typ,isinstance(value,bytes))); i+=1
except FileNotFoundError: pass
(o/'pre-editor-prefs-private.json').write_text(json.dumps(prefs))
args=[r'C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe','-projectPath',str(w),'-batchmode','-force-d3d11','-runTests','-testPlatform','PlayMode','-testFilter',m['filter'],'-testResults',str(o/'tests.xml'),'-logFile',str(o/'unity.log'),'-tp-profile',marker['requiredProfile'],'-job-worker-count','1','-gc-helper-count','1']
env=dict(os.environ);env.setdefault('ALLUSERSPROFILE',env.get('ProgramData',r'C:\ProgramData'))
profile_root=Path(os.environ['USERPROFILE'])/'AppData/LocalLow'/marker['companyName']/marker['productName']/'profiles'/hashlib.sha256(marker['requiredProfile'].strip().encode()).hexdigest()
profile_before={}
if profile_root.exists():
    for item in profile_root.rglob('*'):
        if not item.is_file(): continue
        relative=item.relative_to(profile_root); profile_before[relative]=item.read_bytes()
        backup=o/'private-profile-before'/relative;backup.parent.mkdir(parents=True,exist_ok=True);backup.write_bytes(profile_before[relative])
env['TUMP_REPLAY_EXISTING']=Path('work/local-replay1007/retained-entry.txt').read_text().strip()
env['TUMP_REPLAY_CAPTURES']=str(w/'Logs/replay-controls-frames1-1007')
env['TUMP_REPLAY_ROUNDS']=Path('work/replay-controls1007/retained-two-round.txt').read_text(encoding='utf-8').strip()
env['TUMP_SHADER_CAPTURES']=str(o/'shader-frames')
env['TUMP_REPLAY_AUTOSCENE']=Path('work/replay-scene1007/natural-scene-entry.txt').read_text(encoding='utf-8').strip()
env.pop('TUMP_REPLAY_STREAM',None)
env['TUMP_SCENERY_EXISTING']=r'C:\Users\Matthew\dev\tump-workers1003\qa-a\Logs\replay-scenery-natural1007\fd88c423944947568a33e29d5b003043\TUMP-20261007-121100-7c79fb4ea7554382bc971c2c14b1a4c0'
child=subprocess.Popen(args,cwd=w,env=env)
(o/'launch.json').write_text(json.dumps({'pid':child.pid,'source':ref,'kind':'candidate','out':str(o),'command':args},indent=2))
print('Unity launched',child.pid,'kind',kind,flush=True)
code=child.wait()
print('Unity terminal',code,'restoring source and isolated profile',flush=True)
profile_changes=0
if profile_root.exists():
    for item in profile_root.rglob('*'):
        if not item.is_file(): continue
        relative=item.relative_to(profile_root); current=item.read_bytes()
        if profile_before.get(relative)==current:continue
        after=o/'private-profile-generated-after'/relative;after.parent.mkdir(parents=True,exist_ok=True);after.write_bytes(current)
        profile_changes+=1
        if relative in profile_before:item.write_bytes(profile_before[relative])
        else:
            # Only remove new files created by this isolated run after preserving their bytes.
            item.resolve().relative_to(profile_root.resolve())
            item.unlink()
for relative,data in profile_before.items():
    item=profile_root/relative;item.parent.mkdir(parents=True,exist_ok=True);item.write_bytes(data)
assert all((profile_root/relative).read_bytes()==data for relative,data in profile_before.items())
(o/'profile-restoration.json').write_text(json.dumps({'profile':marker['requiredProfile'],'originalFileCount':len(profile_before),'generatedChangesPreserved':profile_changes,'originalFilesRestored':True},indent=2))
with winreg.CreateKey(winreg.HKEY_CURRENT_USER,key_path) as key:
    for name,value,typ,binary in prefs: winreg.SetValueEx(key,name,0,typ,base64.b64decode(value) if binary else value)
changes={}
unknown=[]
for path,h in files.items():
    target=w/path; data=target.read_bytes()
    if hashlib.sha256(data).hexdigest()==h: continue
    after=o/'preserved-generated-after'/path; after.parent.mkdir(parents=True,exist_ok=True);after.write_bytes(data)
    before=o/'prejob-restorable'/path
    changes[path]={'before':h,'after':hashlib.sha256(data).hexdigest()}
    if before.exists(): target.write_bytes(before.read_bytes())
    else: unknown.append(path)
all_restored=all(hashlib.sha256((w/path).read_bytes()).hexdigest()==h for path,h in files.items())
cases=ET.parse(o/'tests.xml').getroot().findall('.//test-case') if (o/'tests.xml').exists() else []
r={'pid':child.pid,'terminal':True,'exitCode':code,'passed':sum(c.attrib['result']=='Passed' for c in cases),'failed':sum(c.attrib['result']=='Failed' for c in cases),'expected':m['expected'],'cases':[{k:c.attrib.get(k) for k in ['name','result']} for c in cases], 'inputCount':len(files),'generatedDeltas':changes,'allInputsRestored':all_restored,'unknownChangesPreserved':unknown,'existingPrefsRestored':len(prefs)}
(o/'classified-result.json').write_text(json.dumps(r,indent=2))
print(json.dumps({k:r[k] for k in ['pid','terminal','exitCode','passed','failed','expected','inputCount','allInputsRestored','existingPrefsRestored']}|{'generatedDeltas':len(changes)}),flush=True)
assert not unknown,unknown

















