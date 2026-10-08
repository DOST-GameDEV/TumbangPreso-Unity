from pathlib import Path
import csv,json
root=Path.cwd(); frameData={}
with (root/'Logs/tournament-review1007/cpu-trace-export1008/decoded/01-title-to-home-samples.csv').open(newline='',encoding='utf-8-sig') as stream:
 for r in csv.DictReader(stream):
  if r['thread_name'] not in ['Main Thread','Render Thread']:continue
  f=frameData.setdefault(r['frame'],{'frame':int(r['frame']),'mainMs':0,'renderMs':0,'uploadCalls':0,'uploadMs':0,'maxUploadMs':0,'presentationWaitMs':0})
  ms=float(r['inclusive_ms'])
  if r['depth']=='0':f['mainMs' if r['thread_name']=='Main Thread' else 'renderMs']=ms
  if r['thread_name']=='Render Thread' and r['name']=='Gfx.UploadTextureData':f['uploadCalls']+=1;f['uploadMs']+=ms;f['maxUploadMs']=max(f['maxUploadMs'],ms)
  if r['thread_name']=='Main Thread' and r['name']=='Gfx.WaitForPresentOnGfxThread':f['presentationWaitMs']+=ms
rows=sorted(frameData.values(),key=lambda r:r['uploadMs'],reverse=True)[:8]
(root/'Logs/release-ui-perf1008g/prior-loading-upload-frames.json').write_text(json.dumps(rows,indent=2))
print(json.dumps(rows,indent=2))
