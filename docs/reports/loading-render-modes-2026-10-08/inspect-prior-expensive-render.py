from pathlib import Path
import csv,json
root=Path.cwd();data=[]
with (root/'Logs/tournament-review1007/cpu-trace-export1008/decoded/01-title-to-home-samples.csv').open(newline='',encoding='utf-8-sig') as stream:
 for r in csv.DictReader(stream):
  if r['thread_name']=='Render Thread' and float(r['inclusive_ms'])>100 and int(r['depth'])>0:data.append(r)
rows=sorted(data,key=lambda r:float(r['inclusive_ms']),reverse=True)[:25]
(root/'Logs/release-ui-perf1008g/prior-loading-expensive-render-samples.json').write_text(json.dumps(rows,indent=2))
print(json.dumps([{'frame':r['frame'],'depth':r['depth'],'ms':r['inclusive_ms'],'name':r['name']} for r in rows],indent=2))
