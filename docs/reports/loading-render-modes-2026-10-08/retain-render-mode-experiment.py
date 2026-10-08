from pathlib import Path
import csv, hashlib, json, re, shutil

root=Path(__file__).resolve().parents[2]
base=root/'Logs/release-ui-perf1008g'
destination=root/'docs/reports/loading-render-modes-2026-10-08'
destination.mkdir(exist_ok=False)
(destination/'.gitattributes').write_text('* -text\n')
sha=lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
assert sha(root/'Builds/release-ui-perf1008g/TumbangPreso_Data/Managed/TumbangPreso.Runtime.dll')=='2005a5d0ac9bb5c4573e77a46eb0bc115f700f49e22d05afa06891702c242a20'
modes=['baseline','direct','baseline-repeat']
summary={'sourceCommit':'001d80b568fc0a13fb3603a8ccc23123a61f9a42',
 'runtimeSha256':'2005a5d0ac9bb5c4573e77a46eb0bc115f700f49e22d05afa06891702c242a20',
 'runs':[], 'productionChanges':False, 'buildsCreated':0, 'desktopModified':False}
for mode in modes:
 source=base/('render-mode-'+mode+'-1008')
 runner=json.loads((source/'runner-result.json').read_text())
 result=json.loads((source/'result.json').read_text())
 assert runner['terminal'] and runner['exitCode']==0 and runner['inputRestored'] and runner['editorInputRestored'] and runner['profileRestored']
 assert result['passed'] and len(result['frameWindows'])==14
 command=runner['command']
 assert ('-force-gfx-direct' in command)==(mode=='direct')
 log=(source/'player.log').read_text(encoding='utf-8-sig')
 threading=re.search(r'GfxDevice: creating device client; (\S+)',log).group(1)
 if mode=='direct': assert threading=='kGfxThreadingModeDirect'
 else: assert threading!='kGfxThreadingModeDirect'
 selected=[]
 for window in result['frameWindows']:
  assert window['graphicsApi']=='Direct3D11' and window['quality']=='Balanced' and window['unityQuality']=='Ultra'
  assert window['width']==1920 and window['height']==1080 and window['vSyncCount']==1 and window['targetFrameRate']==-1
  assert window['unfocusedFrames']==0
  if window['mode'] in ['00-boot-to-title','01-title-to-home','02-home-idle','menu-HeroButton','menu-HeroButton-repeat']:
   selected.append({key:window[key] for key in ['mode','samples','duration','maxMs','medianMs','p95Ms','p99Ms','framesOver33Ms','framesOver50Ms','framesOver100Ms']})
 output=destination/mode;output.mkdir()
 copied={}
 for path in source.iterdir():
  if path.is_file() and path.suffix in ['.json','.csv','.log']:
   shutil.copy2(path,output/path.name); assert sha(path)==sha(output/path.name)
   copied[path.name]=sha(path)
 with (source/'01-title-to-home-frame-times.csv').open(newline='',encoding='utf-8-sig') as stream:
  times=list(csv.DictReader(stream))
 with (source/'01-title-to-home-frame-context.csv').open(newline='',encoding='utf-8-sig') as stream:
  contexts={row['sample']:row for row in csv.DictReader(stream)}
 spikes=[{'sample':row['sample'],'frameMs':float(row['frame_ms']),'context':contexts.get(row['sample'])}
  for row in sorted(times,key=lambda row:float(row['frame_ms']),reverse=True)[:8]]
 stages=[{'stage':match[0],'wallMs':float(match[1]),'scope':match[2]}
  for match in re.findall(r'\[HomeAssetTiming\] stage=(\S+) wall_ms=([\d.]+) scope=(\S+)',log)]
 summary['runs'].append({'mode':mode,'pid':runner['pid'],'threadingMode':threading,'settingsVerified':True,
  'windows':selected,'loadingSpikes':spikes,'asyncStages':stages,'retainedFileSha256':copied})
for helper in ['run-render-mode-experiment.py','run-batched-release-performance.py','retain-render-mode-experiment.py']:
 shutil.copy2(base/helper,destination/helper)
summary['conclusion']='Direct graphics threading does not remove the loading stall. No adoption; resource/upload and CPU attribution remain unresolved. ABA ordering checks warm-cache/run-order confounding; timings are hidden-player wall-clock evidence, not actual GPU durations or human acceptance.'
(destination/'evidence.json').write_text(json.dumps(summary,indent=2))
print(json.dumps([{'mode':r['mode'],'threading':r['threadingMode'],'windows':r['windows'],'stages':r['asyncStages']} for r in summary['runs']],indent=2))
