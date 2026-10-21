# Pause menu binds the current local body before reopening

PauseWatcher assigned panel.Local after Panel.Open. Reusing an existing panel
invokes OnEnable synchronously, so its input-parking hook still saw the former
body. After the later assignment, Close released the new body instead of undoing
the old parking operation. This is a local input lifecycle bug after rebind.

Four added lines bind the existing panel before activation. Initial creation,
Escape toggle, online time and layout remain unchanged. No art/model/clip/SFX,
loading, network protocol or protected character change.

One native Unity6000.5.8f1/D3D11 baseline reproduces current-body Parked=False.
The exact keyboard-driven re-open case passes after the fix: new body parked,
former body released, online clock1, correct close cleanup.752 frozen inputs
unchanged; zero fixture/tooling repairs and no broad repeated suite.

Raw XML and scoped result are beside this file. This is not hardware controller,
actual-peer rebind, all-menu or tournament certification. The current126 player
observations in ../demo126 predate this fix. Native guards73328/29447 terminal;
named profiles/shared input preferences preserved.
