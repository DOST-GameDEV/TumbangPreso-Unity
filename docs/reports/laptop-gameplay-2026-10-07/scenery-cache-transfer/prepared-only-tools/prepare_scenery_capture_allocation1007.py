from pathlib import Path
import json,subprocess,sys
task=Path(__file__).resolve().parent;s=Path('C:/Users/Matthew/dev/tump-laptop-gameplay-next1003');p=task/'scenery-capture-allocation1007';p.mkdir(exist_ok=True)
number=sys.argv[1];paths=set(json.loads((task/'recorded-scenery1007/overlays.json').read_text()))
paths.update(subprocess.check_output(['git','-C',str(s),'diff','--name-only','bcfc7cf8ad2f2b0f65fc0701b22b13cef4cd2ffb','HEAD','--','Assets','Packages','ProjectSettings'],text=True).splitlines())
overlays={};ref=subprocess.check_output(['git','-C',str(s),'rev-parse','HEAD'],text=True).strip()
for path in sorted(paths):
    target=s/path;data=target.read_bytes() if target.exists() else subprocess.check_output(['git','-C',str(s),'show','HEAD:'+path])
    name=path.replace('/','__');(p/name).write_bytes(data);overlays[path]=name
(p/'overlays.json').write_text(json.dumps(overlays,indent=2));m={'base':ref,'filter':';'.join('TumbangPreso.PlayTests.RecordedSceneryTests.'+method for method in ['ActualShowAndRecoveryPopulationHasMeasuredBoundedPayloadAndGenerationIdentity','CachedSceneryDescriptorKeepsDetachedFramesAndDetectsBornOrReparentedRenderers','SavedAnimalAndNewLineStayVisibleWhenTheirLiveRootIsInactive']),'expected':3,'purpose':'Final descriptor cache: measured actual12models, detached snapshots/hierarchy/render births andactualanimal-line pixels; countercalibrated unavailable=-1'}
(p/'packet.json').write_text(json.dumps(m,indent=2))
runner=(task/'run_recorded_scenery42_1007.py').read_text(encoding='utf-8').replace("p=Path('work/recorded-scenery1007')","p=Path('work/scenery-capture-allocation1007')")
runner=runner.replace("o=w/'Logs/recorded-scenery-native42-direct1007'","o=w/'Logs/scenery-capture-allocation"+number+"-direct1007'")
prior='recorded-scenery-native42' if number=='43' else 'scenery-capture-allocation'+str(int(number)-1)
runner=runner.replace("prior=w/'Logs/recorded-scenery-native41-direct1007'","prior=w/'Logs/"+prior+"-direct1007'")
(task/('run_scenery_capture_allocation'+number+'_1007.py')).write_text(runner,encoding='utf-8');print('Prepared',number,len(overlays),'immutable overlays',ref)
