# Changed Arena ownership survives an actual single-scene load

Existing MergedContentSmokeTests.MergedArenaSceneAndHeroCatalogResolveActualImportedArt
passes1/1 oncurrent8db source after the inactive-scene stage/recovery fixes.
Unity4512/parent83248 terminal0, graphicsPlayMode/Unity6000.5.8f1/isolatedqa-a.
All21126inputs/279generated deltas/QualitySettings/13 existing preferences restored.
The runner names this run original; it is a current-source regression check,
not another original defect baseline or a new candidate fixture.

MapRetrievalProbe.Load uses actual SceneManager.LoadSceneAsync(Arena) Single,
then begins the real SliceRunner and parks actors/disables input writers.
The existing case asserts stage/layouts, recovery singleton, fall/vine callbacks,
movement floor, can/four actors and actual imported hero renderer references.
This directly checks the changed active-scene claims during loading. Earlier
three-case evidence used an older stage and is not substituted for this check.
Only this affected case ran; unchanged replay cases were not repeated.

No source or new fixture was authored. Published e447 Home was adopted in the
isolated worker; the existing declared Home fixture remains a compile input.
This is native scene/ownership/import evidence with parked actors, not ordinary
GUI gameplay, full-map timing, human input, networking or performance acceptance.
Actual PC-host/laptop Arena match follows on the exact same-source Windows package.
