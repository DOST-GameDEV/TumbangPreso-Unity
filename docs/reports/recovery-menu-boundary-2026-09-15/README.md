# Menu Submit leaked into recovery

A real Input System gamepad Submit closed the actual match menu and also counted
as one get-up press. The failed v1 fixture observed MashPresses=1 immediately after
Resume, even though that press belonged to the menu. The menu deliberately leaves
match time running; changing time scale would not fix input ownership.

PausePanel now asks its local PlayerInputReader to discard menu button input on
close. The reader consumes the closing frame, clears pending intent, and retains
currently held button suppression until release. It reads the configured actions
and touch state, so there is no hard-coded key or arbitrary timed cooldown. The
latch is producer-owned, avoiding interference from Nemu's companion AI writer.
Movement axes and host recovery rules are unchanged. Net/C4 files are untouched.

Focused v2 reproduction passes: Resume spends no mash and a release/fresh press
still recovers. An extended check also holds Submit after closing. Related quick
keyboard/gamepad/touch recovery and timing regressions PASSED6/6, including the
extended menu hold check and63device/context/cadence combinations. Existing
63-case recovery evidence is preserved; this closes a distinct menu boundary.

This source fix is not yet included in nativev22, which remains the delivered UI
video/build. Fresh Windowsv23 native qualification PASSED: the actual menu consumes controller
Submit, continued hold remains consumed, and release/fresh input produces one mash.
Build1135MB/46s, guard23f33fc9973b. Shared standalone input preferences unchanged.
Runtime DLL SHA256: 71ca7072355efc1982e9e4a11cd0546d7d56caf904ad9b9896a68531ba1a0786. Native PID19336 exited. No
actual account/profile or usage reset was performed; test worlds are isolated.
