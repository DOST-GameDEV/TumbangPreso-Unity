from pathlib import Path
root=Path(__file__).resolve().parents[2]
source=(root/'Logs/map-direction1007/restore-cadence-native.py').read_text()
source=source.replace("out=root/'Logs/map-direction1007'/sys.argv[1]", "out=root/'Logs/release-ui-perf1008g'/sys.argv[1]")
exec(compile(source,str(__file__),'exec'))
