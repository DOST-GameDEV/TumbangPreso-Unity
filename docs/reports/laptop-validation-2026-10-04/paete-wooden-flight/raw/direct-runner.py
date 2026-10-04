from pathlib import Path
import base64,hashlib,importlib.util,io,json,os,subprocess,sys,tarfile,winreg
s=Path('C:/Users/Matthew/dev/tump-integration1003');w=Path('C:/Users/Matthew/dev/tump-workers1003/qa-a');packet=Path('work/paete-flight-packet1004')
kind=sys.argv[1];m=json.loads((packet/'packet.json').read_text());ref=m['base'];o=w/('Logs/paete-flight-'+kind+'1-direct1004');o.mkdir()
marker=json.loads((w/'.tump-validation-worker.json').read_text())
spec=importlib.util.spec_from_file_location('prep',s/'tools/prepare_unity_test_workers.py');prep=importlib.util.module_from_spec(spec);spec.loader.exec_module(prep)
paths=['Assets','Packages','ProjectSettings']
files={}
if kind=='original':
    print('Preparing immutable '+ref+' replay inputs',flush=True)
    archive_process=subprocess.Popen(['git','-C',str(s),'archive',ref,*paths],stdout=subprocess.PIPE)
    with tarfile.open(fileobj=archive_process.stdout,mode='r|') as archive:
        for member in archive:
            if not member.isfile():continue
            data=archive.extractfile(member).read();target=w/member.name
            if member.name=='ProjectSettings/ProjectSettings.asset':data=prep.replace_identity(data,marker['companyName'],marker['productName'])
            if target.exists() and target.read_bytes().replace(b'\r\n',b'\n')!=data.replace(b'\r\n',b'\n'):
                backup=o/'previous-source'/member.name;backup.parent.mkdir(parents=True,exist_ok=True);backup.write_bytes(target.read_bytes())
            target.parent.mkdir(parents=True,exist_ok=True)
            if not target.exists() or target.read_bytes()!=data: target.write_bytes(data)
            files[member.name]=hashlib.sha256(data).hexdigest()
            if len(files)%4000==0: print('Prepared '+str(len(files))+' source files',flush=True)
    for root in ['Runtime','Editor','Tests']:
        for old in (w/'Assets/TumbangPreso'/root).rglob('*.cs'):
            path=old.relative_to(w).as_posix()
            if path not in files:
                for item in [old,Path(str(old)+'.meta')]:
                    if item.exists():
                        dest=o/'preserved-extra-source'/item.relative_to(w);dest.parent.mkdir(parents=True,exist_ok=True);item.rename(dest)
else:
    baseline=w/'Logs/paete-flight-original1-direct1004'
    result=json.loads((baseline/'classified-result.json').read_text())
    assert result['terminal'] and result['allGeneratedBeforeAfterPreservedAndRestored']
    q=json.loads((baseline/'qualified-source.json').read_text());assert q['sourceCommit']==ref
    files=q['files']
    for path,expected_hash in files.items():
        assert hashlib.sha256((w/path).read_bytes()).hexdigest()==expected_hash,path
    print('Reused and verified '+str(len(files))+' immutable baseline inputs',flush=True)
overlays={'Assets/TumbangPreso/Runtime/Abilities/PaeteHazards.cs':'PaeteHazards.'+kind+'.cs','Assets/TumbangPreso/Tests/PlayMode/PaeteKitPlayProbe.cs':'PaeteKitPlayProbe.cs','Assets/TumbangPreso/Tests/PlayMode/PaeteKitPlayProbe.cs.meta':'PaeteKitPlayProbe.cs.meta'}
for path,name in overlays.items():
    data=(packet/name).read_bytes();target=w/path
    if target.exists():
        backup=o/'before-packet-overlay'/path;backup.parent.mkdir(parents=True,exist_ok=True);backup.write_bytes(target.read_bytes())
    target.write_bytes(data);files[path]=hashlib.sha256(data).hexdigest()
qualified={'sourceCommit':ref,'controlledRuntime':kind,'packet':m,'files':files,'filter':m['filter'],'expected':3,'identityIsolation':marker,'execution':'DirectUnity:no scheduler/memory/admission/externaltimeout'}
(o/'qualified-source.json').write_text(json.dumps(qualified,indent=2));quality=w/'ProjectSettings/QualitySettings.asset';before=quality.read_bytes();(o/'prejob-QualitySettings.asset').write_bytes(before)
key_path='Software\\Unity\\UnityEditor\\'+marker['companyName']+'\\'+marker['productName'];prefs=[]
try:
    with winreg.OpenKey(winreg.HKEY_CURRENT_USER,key_path) as key:
        i=0
        while True:
            try:name,value,typ=winreg.EnumValue(key,i)
            except OSError:break
            prefs.append((name,base64.b64encode(value).decode() if isinstance(value,bytes) else value,typ,isinstance(value,bytes)));i+=1
except FileNotFoundError:pass
(o/'pre-editor-prefs.json').write_text(json.dumps(prefs))
args=[r'C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe','-projectPath',str(w),'-batchmode','-force-d3d11','-runTests','-testPlatform','PlayMode','-testFilter',m['filter'],'-testResults',str(o/'tests.xml'),'-logFile',str(o/'unity.log'),'-tp-profile',marker['requiredProfile'],'-job-worker-count','1','-gc-helper-count','1']
env=dict(os.environ);env.setdefault('ALLUSERSPROFILE',env.get('ProgramData',r'C:\ProgramData'))
child=subprocess.Popen(args,cwd=w,env=env);launch={'pid':child.pid,'source':ref,'packetKind':kind,'expected':3,'out':str(o),'command':args};(o/'launch.json').write_text(json.dumps(launch,indent=2));print(json.dumps(launch),flush=True)
code=child.wait();(o/'post-QualitySettings.asset').write_bytes(quality.read_bytes());quality.write_bytes(before)
with winreg.CreateKey(winreg.HKEY_CURRENT_USER,key_path) as key:
    for name,value,typ,binary in prefs:winreg.SetValueEx(key,name,0,typ,base64.b64decode(value) if binary else value)
changed={p:{'before':h,'after':hashlib.sha256((w/p).read_bytes()).hexdigest()} for p,h in files.items() if hashlib.sha256((w/p).read_bytes()).hexdigest()!=h}
r={'pid':child.pid,'exitCode':code,'terminal':True,'inputCount':len(files),'inputChanges':changed,'qualityRestored':quality.read_bytes()==before,'existingPrefsRestored':len(prefs)}
(o/'direct-result.json').write_text(json.dumps(r,indent=2));print(json.dumps({k:r[k] for k in ['pid','exitCode','terminal','inputCount','qualityRestored','existingPrefsRestored']}|{'inputChanges':len(changed)}),flush=True)
