# Focus loss retires input toggles and buffered recovery

PlayerInputReader cleared stale input on menus and disable but had no focus-loss
hook. A latched sprint and an unconsumed recovery tap survived that boundary.
OnApplicationFocus(false) now uses existing DiscardMenuButtonsUntilRelease,
resetting toggles/intent/commit and holding sampled buttons until release. It
leaves other owners' Parked state and ordinary toggle release behavior intact.

The isolated native fixture uses actual TouchInput press/release into the enabled
reader. The motor is disabled so physics cannot consume the queued recovery tap.
Unity SendMessage dispatches the focus callback; it is not physical OS focus
automation or an InputSystem device-reset measurement. Touch flags and the
in-memory ToggleSprint preference are restored; named Editor guards restore input
preferences. No saved settings/profile write is part of the fixture.

Initial72476 returned exit0 but ran zero tests: the temporary metadata GUID had33
characters and Unity ignored the fixture. It is not acceptance. ONE metadata-only
repair supplies32 valid hex characters, retaining original metadata and zero-test
receipt; fixture and production source stayed unchanged before corrected baseline.
Corrected16510 ran exactly3: two intended stale sprint/recovery failures, one
ordinary toggle-release control passed. Candidate52999 passed exactly3 first run,
same corrected fixture/meta. No further repair, assertion weakening or native
retry. Each launch followed direct prep exit0/exact3;2048MB GPU plus2048MB reserve,
450-second ceiling, PlayMode/nographics on Unity6000.5.8f1. Guards terminal/restored/
no lease. Post74047 terminal0: exact3 MAIN/q hashes match and all18431 protected
Assets/TumbangPreso files unchanged. Raw0test/corrected-original/candidate XML,
guard receipts and owned/preservation manifests accompany this report; logs and
original metadata remain in
local Logs/input-focus-lifetime1002.

Acceptance is the native reader's focus-message boundary. Background gamepad or
keyboard sampling, actual alt-tab, Android app suspension, touch hardware, every
buffered verb and a new Windows build are not qualified. Frozen1002k predates
this correction; prior toggle/accessibility evidence remains unchanged.
