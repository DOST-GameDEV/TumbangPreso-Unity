# Device change followed by HUD scaling

While reviewing the requested25percent shared power controls, a native transition
check found a placement defect: switch keyboard to touch, then increase HUD scale.
The centred touch offset(0,34) reverted to the cached keyboard offset(-40,30).

Power placement queried HudReadingLayout on the owner component, but the actual
layout is installed on its root canvas. Resolve that ancestor and rebase only
the PowerSeals group. Rebasing all groups would risk capturing already-scaled
positions and accumulating offsets in unrelated notices. Existing no-argument
callers retain their prior behavior, and authored margins/base size stay unchanged.

## Evidence

Unity6000.5.8f1 Linux OpenGL, named isolated profiles. Baseline1/1fails with the
exact stale position above. Final1/1passes in3.16seconds: repeated device directions,
HUD scales1and1.2, base1.25multiplier and unchanged score-group position after
round trips. No new OOM or guard stop in either run. [Raw checks](power-anchor-checks/). This is native layout-state qualification;
physical touch hardware and human preference remain separate.
