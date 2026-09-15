# Touch-layout tools can move clear of controls

The expanded toolbar partially covered upper touch controls. Added TOOLS BELOW /
TOOLS ABOVE to move the same compact or expanded toolbar between the top and
bottom of the editing canvas. Saved touch-control positions remain unchanged.
Moving the toolbar exposes the other part of the canvas; it is not a new layout
or a change to gameplay input. Save, Cancel, Reset, size and opacity stay intact.

The focused PlayMode case
TumpNativeSettingsTests.ControllerAndTouchViewsKeepTheirRealReturnAndCancelPaths
passed1/1 in touch-toolbar-position-v3,2026-09-15 06:41:19Z to06:41:28Z.
At1920x1080 it finds three partly covered controls among nine, moves the toolbar,
requires all three to be completely clear, and checks every control coordinate.
Ten-size upper and three-size lower captures check action bounds. The rendered
lower toolbar was inspected for text, spacing and access to the upper controls.
The lower position naturally covers bottom controls until moved above again.

Two earlier test attempts failed because the fixture checked control centres,
then the restored batch GameView instead of the actual capture viewport. The
production change did not need alteration. Their XML is preserved beside the
passing result to keep this distinction explicit.

Profile restoration receipt259d0ed80457 preserves two existing files and shared
Editor input. Nativev4 predates this fix. Physical touch comfort and the owner's
visual approval remain pending; this is scoped runtime/viewport evidence.
