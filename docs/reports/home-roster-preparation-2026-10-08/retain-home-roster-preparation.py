from pathlib import Path
import hashlib,json,re,shutil

root=Path(__file__).resolve().parents[2];base=root/'Logs/release-ui-perf1008g'
target=root/'docs/reports/home-roster-preparation-2026-10-08';target.mkdir(exist_ok=True)
(target/'.gitattributes').write_bytes(b'* -text\n')
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
summary={'runs':{},'buildsCreated':0,'desktopUpdated':False,'existingDesktopSource':'001d80b568fc0a13fb3603a8ccc23123a61f9a42'}
for mode in ['original','candidate','bake-missing','candidate2','candidate3']:
 source=base/('home-roster-preparation-'+mode+'1008')
 result=json.loads((source/'result.json').read_text())
 restored=json.loads((source/'source-restoration.json').read_text())
 assert result['terminal'] and all(result[key] for key in ['inputRestored','profileRestored','editorInputRestored','qualityRestored'])
 assert not restored['remainingSourceDeltas']
 if mode in ('candidate2','candidate3'): assert result['exitCode']==0 and len(result['cases'])==5 and all(case['result']=='Passed' for case in result['cases'])
 if mode=='bake-missing': assert result['exitCode']==0 and not result['cases']
 output=target/mode;output.mkdir(exist_ok=True);files={}
 for name in ['launch.json','result.json','results.xml','source-restoration.json','rooted-before.json','pre-input-prefs.json','pre-editor-prefs.json','editor.log']:
  path=source/name
  if path.exists():
   shutil.copy2(path,output/name);assert sha(path)==sha(output/name);files[name]=sha(path)
 summary['runs'][mode]={'pid':result['pid'],'exitCode':result['exitCode'],'cases':result['cases'],'protectedCount':result['protectedCount'],'exactRestoration':True,'retainedSha256':files}
before=json.loads((base/'home-roster-preparation-bake-missing1008/rooted-before.json').read_text())
for path,digest in before.items():assert sha(root/path)==digest,path
current={str(p.relative_to(root)):sha(p) for p in (root/'Assets/TumbangPreso/Resources/RootedAnimations').glob('*.asset')}
added={path:digest for path,digest in current.items() if path not in before}
assert len(added)==9
summary['bake']={'existingSetsPreserved':len(before),'existingSourceSha256':before,'newSetsSha256':added}
log=(base/'home-roster-preparation-candidate31008/editor.log').read_text(encoding='utf-8-sig')
matched=re.findall(r'\[RootedCurrentRig\] hero=(\S+) rig=(\S+) sampledClips=4 nonBindPoses=4',log)
assert len(matched)==9
summary['sampledCurrentRigs']=[{'hero':hero,'rig':rig,'clips':4} for hero,rig in matched]
summary['homePreparation']=re.search(r'\[HomeRosterPreparation\].*',log).group(0)
summary['startup']=re.search(r'\[StartupOrder1007\].*',log).group(0)
launchTime=(base/'home-roster-preparation-candidate31008/launch.json').stat().st_mtime
images=target/'startup';images.mkdir(exist_ok=True);summary['captureSha256']={}
for name in ['StartupLoginFirst1007','StartupMainLoading1007','StartupFirstHomePlaying1007']:
 for width in [1920,1280]:
  path=root/'Logs/shots-native-ui'/f'{name}-{width}.png'
  assert path.stat().st_mtime>=launchTime
  shutil.copy2(path,images/path.name);assert sha(path)==sha(images/path.name)
  summary['captureSha256'][path.name]=sha(path)
sourcePaths=['Assets/TumbangPreso/Runtime/UI/ConvertedMainMenu.cs','Assets/TumbangPreso/Editor/RootedAnimationAuthor.cs','Assets/TumbangPreso/Tests/PlayMode/HomeRosterPreparationTests.cs','Assets/TumbangPreso/Tests/PlayMode/HomeRosterPreparationTests.cs.meta']
sourcePaths+=list(added)+[path+'.meta' for path in added]
tested=json.loads((base/'home-roster-preparation-candidate31008/source-before.json').read_text())
summary['testedSourceSha256']={}
for rel in sourcePaths:
 path=root/rel; key=Path(rel).as_posix();assert tested[key]==sha(path),rel
 summary['testedSourceSha256'][key]=sha(path)
 if path.suffix in ['.cs','.meta'] and 'RootedAnimations' not in rel:
  output=target/'source'/key;output.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(path,output)
for helper in ['run-home-roster-preparation.py','restore-home-roster-native.py','retain-home-roster-preparation.py']:
 shutil.copy2(base/helper,target/helper)
summary['limits']='Native Editor data preparation, exact startup order controls and36 current-rig serialized clip samples. No new ordinary Windows latency, all-map natural/status gameplay, peer/human or tournament acceptance claim. Existing23rooted sets retain exact bytes.'
(target/'evidence.json').write_text(json.dumps(summary,indent=2))
print(json.dumps({'nativeCases':len(summary['runs']['candidate3']['cases']),'existingSetsPreserved':len(before),'newSets':len(added),'sampledClips':len(matched)*4,'preparation':summary['homePreparation'],'startup':summary['startup']}))
