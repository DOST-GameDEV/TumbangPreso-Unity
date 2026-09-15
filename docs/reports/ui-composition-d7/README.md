# D7 match HUD and live menu

The HUD now has larger supporting text, lighter score strips and a simpler timer,
with a more compact held-power reference. The pause menu uses a side column and
lighter overall dimming so the live court remains visible. It truthfully says the
match keeps running. Existing role, recovery, aim/reticle, power state, spectator
clean feed and pause/settings input contracts stay intact. No camera, source model,
FPP mesh or gameplay-authority changes were made.

HUD-v2:3/3PASS, restorationf18fbbdc4e93. Classic HUD/role/recovery, Hero held power
info/spectator, and pause/settings/Escape cases passed. Classic, held info, spectator
and live pause views covered960x540,1280x720,1366x768,1920x1080,1920x1200,
1280x960,2560x1440,3440x1440,3840x1080 and3840x2160, with a small recovery capture.
UI geometry/readability is checked against real arena renders; profile/input data
was hash-restored. This does not certify physical hardware or full online gameplay.

## Camera-view investigation and fixture limits

Initial HUD-v1 passed3/3, but a nearby actor's face obscured the world in the UI
capture. A narrow probe confirmed the local body/head were already ShadowsOnly
and the camera followed the correct local actor. Temporarily hiding only neighbour
slot2 in a diagnostic comparison removed the obstruction. That hidden-model image
is diagnostic-only and is not used as final UI evidence. Normal ScreenCapture was
unavailable in Editor batch mode; no native-player comparison is claimed.

The final UI fixture parks bot input after the round starts and moves nearby bots
farther from the local player. Every model remains visible in the final captures.
This creates a clear repeatable UI review view; it is not a production camera fix
or a change to player collision. Probe source was removed from imported Assets and
kept in ignored Logs/hud-camera-diagnostics. Observation excerpts are included.
Diagnostic receipts0a939feb512a/cdd56c800843; initial HUD receiptc9f11f44b3f5.

The screenshots still show the existing game models/FPP art. Deferred Inday work
and broader model/animation review are not completed by this UI batch. Original
login/controller art remain unchanged. Results, round changes, chat, training and
other remaining UI, U8 native motion/routes/high-DPI preview review and the complete
gameplay bookmark still follow. No owner acceptance or full completion inferred.
