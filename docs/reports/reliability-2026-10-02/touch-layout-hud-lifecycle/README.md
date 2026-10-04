# Touch editor previews retire before immediate reopen

The current settings touch-layout route reuses TumpTouchLayoutView. When its editor
created a preview HUD, Cancel hid the HUD canvas and scheduled the HUD's destruction
for the end of the frame. TouchHud.Instance still pointed at that retiring object.
An immediate Open in the same frame therefore borrowed controls already scheduled
for destruction. Old TouchHud OnDisable/OnDestroy also released the shared input
table even when a replacement HUD owned Instance.

The editor now deactivates only its created preview HUD before scheduling its
destruction. TouchHud releases its singleton ownership immediately on disable,
claims it on enable and clears shared touch input only while it owns that singleton.
Borrowed gameplay HUDs follow their existing restoration path. Layout, artwork,
bindings, GenericPadBridge and input-table semantics are unchanged.

## Native evidence

Three focused PlayMode cases use the current public touch-editor Open method,
actual CancelTouchLayout button callback, actual TouchHud lifecycle and existing
TouchInput table. They construct the native controls in a minimal owned UI scope.
The replacement-ownership case creates another actual TouchHud component and
observes the old component's deferred destruction; it does not inject a new input
framework. PlayerPrefs layout data and its cache are restored by the fixture and
the named-profile guard.

- Original run 68013: exactly 3 cases, 2 intended causal failures and 1 control
  passed. Immediate reopen returned the retiring preview, and old HUD teardown
  disabled replacement input. Cancelling the borrowed-HUD editor remained valid.
- Candidate run 9697: exactly 3 cases passed. Immediate reopen uses fresh visible
  controls, the borrowed gameplay HUD survives Cancel, and old deferred cleanup
  preserves replacement ownership, held Sprint and movement-table input.
- Identical native fixture and valid 32-hex metadata for both runs. Zero fixture,
  tooling or native repairs.
- Every dependent launch followed direct preparation exit 0. Exact 4 owned inputs
  match MAIN/qualification; all 12,534 protected qualification hashes remained
  unchanged, including the separately qualified SafeStore and frame-context units.
- Unity 6000.5.8f1, graphics PlayMode, profile touch-layout-hud-lifecycle1002,
  2048 MB GPU job plus 2048 MB reserve, 450-second ceiling. Both guards are terminal,
  preservation completed and no lease remains. No browser or preview was opened.

Raw baseline/candidate XML and job receipts, owned-input manifests, case counts and
protected-input results accompany this report. Full logs, original sources and
protected snapshots remain in local Logs/touch-layout-hud-lifecycle1002.

This accepts native editor UI lifetime and shared-input ownership. It does not
qualify a physical touchscreen, controller, rendered layout quality, Android,
shipping-map flow or player build. The frozen 1002h player predates this change.
