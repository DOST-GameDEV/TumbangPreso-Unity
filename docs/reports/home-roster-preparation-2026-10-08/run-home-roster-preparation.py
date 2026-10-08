from pathlib import Path
import sys
root=Path(__file__).resolve().parents[2]
mode=sys.argv[1]
assert mode in ('original','candidate','bake-missing','candidate2','candidate3')
source=(root/'Logs/map-direction1007/run-new3-cadence-geometry1008.py').read_text()
source=source.replace("kind='new3-cadence-geometry1008';out=ROOT/'Logs/map-direction1007'/kind", "kind='home-roster-preparation-'+"+repr(mode)+"+'1008';out=ROOT/'Logs/release-ui-perf1008g'/kind")
start=source.index("filters='TumbangPreso.PlayTests.DirectedMapArrivalTests.")
end=source.index('\nprofile=',start)
filters='TumbangPreso.PlayTests.HomeRosterPreparationTests.ActualHomePreloadPreparesRosterMeshesAndMotionBeforeReadyWithoutSpawningActors'
if mode in ('candidate','candidate2','candidate3'):
 filters+=';TumbangPreso.PlayTests.StartupOrder1007Tests.LogosLoginFiveSecondMainThenFirstHomeAnimation;TumbangPreso.PlayTests.OwnerMenuEditsTests.OutlineWarmupSurvivesSceneNotificationsWithoutSpawningOrDressingModels;TumbangPreso.PlayTests.GeneratedMotionAssetsTests.BakedMotionWarmupRetainsTheExactAssetsWithoutCreatingActors'
if mode in ('candidate2','candidate3'):filters+=';TumbangPreso.PlayTests.HomeRosterPreparationTests.EveryCurrentHeroRootedSetSamplesItsActualRig'
source=source[:start]+"filters="+repr(filters)+"\nexpected="+str(5 if mode in ('candidate2','candidate3') else 4 if mode=='candidate' else 1)+source[end:]
start=source.index('prior=json.loads(')
end=source.index("result={'sourceRef'",start)
source=source[:start]+"environment=preservation.unity_environment()\n"+source[end:]
if mode=='candidate3':source=source.replace("'-tp-profile',profile,'-runTests'", "'-tp-profile',profile,'-tp-performance-only','-runTests'")
if mode=='bake-missing':
 source=source.replace("'-runTests','-testPlatform','PlayMode','-testFilter',filters,'-testResults',str(out/'results.xml'),", "'-executeMethod','TumbangPreso.EditorTools.RootedAnimationAuthor.RunMissing',")
 # Snapshot current authored sets so targeted additions cannot rewrite them.
 source=source.replace('frozen=snapshot();', "rooted=ROOT/'Assets/TumbangPreso/Resources/RootedAnimations';rootedBefore={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest() for p in rooted.glob('*.asset')};(out/'rooted-before.json').write_text(json.dumps(rootedBefore,indent=2));frozen=snapshot();")
exec(compile(source,str(__file__)+':'+mode,'exec'))
