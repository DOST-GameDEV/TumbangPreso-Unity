# Unverified graphics prototype, paused for substantial map revisions

Owner feedback on2026-09-12: current maps barely look improved and do not feel
more Filipino. Maps take priority again. The V9 technical/spatial batch at97e61f92
is not accepted as the requested visual transformation.

The integration patch and five source texts here preserve an UNCOMPILED,
UNTESTED experiment: independent3D resolution/supersampling presented through a
lowest-order native Canvas RawImage, native HUD, frame-cap settings and two probe
cases. No graphics prototype is active in compiled source. No Unity launch had
occurred after writing this experiment; no generated metadata needs restoration.

Before reviving it, inspect the patch against current source. The probe still
needs proper restoration of GameLaunch.AllBots/Spectator/SoloSeat and pinned
SceneFlow rules, matching MapRetrievalProbe's hooks. Verify actual delivered
linear/HDR color, overlay ordering, projection, outline thickness, external
capture ownership, replay, camera lifecycle and frame cost. Native mode should
avoid an offscreen render/presentation copy. One test uses actual screen captures;
its behavior in batch Editor is not established. Do not claim any pass or gain.
