# Owner-painted settings and touch editor

The settings surface, rows, option popup and save/discard choice are new native
views. Former visual builders remain inactive. The existing settings transaction,
rebinding, graphics/audio services, preference IDs and controller/touch callbacks
retain ownership of behavior. Actual supplied field frames and checkbox pixels
are used without distortion. The new top categories, quiet reading area and
separate native toggles/sliders/choices avoid making every control an action pill.

The first render exposed top-left-pivot rotation lifting dropdown arrows above
their fields, and Slider stretching a39px handle over its58px area to97px. These
were corrected at their actual transform causes. Checkboxes keep the original
21x24art with a generous hit area. Settings-only bottom padding prevents
unnecessary scrolling on the short pages; shared credits layout was not changed.

The touch editor has a new source-action toolbar, compact by default. Its size
and opacity controls expand on demand, leaving upper gameplay controls reachable
for dragging. It retains the actual TouchHud geometry and layout-store save,
reset and cancel paths. Gameplay touch-button art itself migrates with U6.
The approved controller diagram,18button callouts and connecting lines were
not redesigned. The actual diagram still opens and returns through settings.

## Verification and honest limits

Fresh receipts and original Unity captures are in owner-ui-u5-evidence.

- Settingsv1:1/1 passed for five pages, VSync/frame-cap rules and unsaved discard.
- Settingsv2:3/4 passed, including controller/touch return and binding rollback.
  Pause settings' injected Escape did not open its decision.
- Isolated pausev3 failed the same way; v4 diagnostics showed dirtyTrue but
  keyboardFalse, MenuNavFalse and no competing takeover. The synthetic event was
  not delivered to the unattended GameView. v4's touch failure was a test querying
  the now-collapsed size slider before pressing its new expand control.
- Finalv5:2/2 passed after using the existing RecoveryDeviceProbe's test-only
  IgnoreFocus/AllDeviceInputAlwaysGoesToGameView settings and restoring them in
  finally. Explicit keyboard delivery assertion passed. Child settings owned
  Escape, discard returned to the parked pause menu and the next Escape resumed
  the player. Touch expanded/compact state and cancelled layout rollback passed.

No production input/controller algorithm was changed to satisfy the test.
Profiles and shared Editor input preferences were restored. These are four
related contracts across scoped runs, not a full game test suite or a claim of
physical controller/phone testing. Real device ergonomics and complete UI flows
remain part of U8. No Desktop build was replaced.

## Critique and next work

The source typography, palette and control hierarchy now agree with the new
entrance. The corrected arrows and compact slider handles read cleanly. Dense
binding lists still require scrolling; final controller focus and navigation
review must include the lowest rows and option popups. Large paper contours and
simple slider rails need the same U8 art refinement recorded in earlier stages.
The touch editing background is busy and some old gameplay-button art remains;
do not treat this toolbar pass as completion of those HUD surfaces.

Continue U6 HUD/pause/training/round/results/touch gameplay visuals, then U7
profile and U8 integrated art/input/motion review. Gameplay resumes afterward.
