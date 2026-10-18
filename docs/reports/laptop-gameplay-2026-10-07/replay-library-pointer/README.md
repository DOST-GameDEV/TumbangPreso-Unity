# Mature replay-library pointer path

PC packaged47924 passed startup then failed the freshly created Replay0 row's
raycast. This focused native check leaves product code unchanged, opens the actual
library over an existing completed TUMP recording, observes asynchronous row
creation, settles its layout and sends an actual pointer event through EventSystem.

Native61168 passes: after one coroutine frame/ForceUpdate and after0.2s, Replay0
has one hit at(320,336), rectangle1740x84, enabled ScreenSpaceOverlay/nullcamera.
The top IPointerClickHandler is that row and the real pointer click opens an
actual replay frame without error. All21347 inputs/13 preferences/four original
profile files restore after terminal. Exact tested source SHA is verified.

Scope: native640x480 mature row, not its same creation frame or the PC packaged
window. This narrows the issue toward first-frame readiness; it does not prove a
cause or qualify the delayed actual packaged rerun. No production layout, graphic,
network, profile or traffic code was changed to make the test pass.
