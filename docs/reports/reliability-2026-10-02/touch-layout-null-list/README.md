# Saved null touch-list suspicion did not reproduce

Status: finding only, no product patch.

Source inspection showed TouchLayoutStore.TweakFor and SetTweak dereference the
parsed Tweaks list, while TouchHud.Build/ApplyLayout uses those public methods.
A saved JSON preference containing Tweaks:null was suspected of stranding control
construction. The native check used the actual preference key, JsonUtility load
through TouchLayoutStore and both public lookup/edit operations.

Original run 72038 passed exactly 3 cases on unchanged source:

- Explicit saved Tweaks:null still permitted default lookup and a layout edit.
- No saved preference permitted defaults and an edit.
- An ordinary saved list retained its existing tweak and permitted updating it.

The supplied null-list representation therefore did not reproduce the suspected
failure. No candidate patch or repeated native run was justified. This acceptance
does not establish every malformed preference shape or non-finite numeric case.

The fixture snapshots/restores the exact touchlayout PlayerPrefs key, store cache
and Revision. The named-profile guard also restored the Editor preference snapshot.
Unity 6000.5.8f1, batch/nographics EditMode, profile touch-layout-null-list1002,
CPU 1536 MB plus 2048 MB reserve, 450-second ceiling. Preparation completed with
direct exit 0 before launch. All 12,543 protected qualification hashes remained
unchanged; the guard is terminal, preservation completed and no lease remains.
Zero tooling or fixture repairs. No browser or preview was opened.

The new temporary fixture and metadata are retired from both active Assets folders,
with their exact bytes retained here and in each workspace's local Logs. Cleanup
verifies absolute containment and frozen hashes. Runtime/Input/TouchLayoutStore.cs
is unchanged. Raw XML, receipt, input manifests and cleanup record accompany this
report. Complete logs and protected snapshots remain in local
Logs/touch-layout-null-list1002.

No gameplay, rendered HUD, hardware, Android, complete preference-format or player
build claim is made.
