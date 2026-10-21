# Real railing and bridge recovery, 2026-09-24

SaBubong now catches the actual nearby rail, holds the character outside it while
presses advance recovery, and pulls them over onto a supported landing immediately
inside that edge. Lagoon falls remain swimming at the water-entry position. Jump
beside a reachable public deck/bridge starts a reach-and-grip followed by the same
press-gated climb onto that surface. Ordinary recovery no longer calls Respawn.
Intentional stairs remain usable. Exceptional off-world rescue remains separate:
it uses the nearest roof edge or bounded water position, not the court centre.
Existing held/loose stock return delays remain10seconds roof and8seconds Lagoon.

Body animation uses the retained rigid rigs and actual measured palms, with a
suspended effort pose, planted grip, tucked pull-over and release onto the deck.
Hand spacing follows each rig's shoulders/arm reach. No character source model or
ordinary floor-get-up clip was edited. Cinematic recovery camera frames the lip
from outside; the old prone-floor camera was visibly wrong and was replaced only
for edge recovery. Reduced-camera mode retains first-person effort poses. Existing
native/legacy recovery prompts now say climb; there is no new HUD layout.

Host owns the constrained trajectory for every seat. Ordinary owner moves cannot
replace it; movement epochs separate takeover/release from stale input. Start/end
snapshots are reliable so packet loss cannot strand a remote owner. A monotonic
per-body pose serial orders those against ordinary pose packets. Climb requests
carry seat/epoch only; host current position and real geometry determine the anchor.
Mash episodes/rate caps and duplicate rejection remain. Paused edge requests are
refused, cancellation/teleport retire the grip, and stamina/fatigue clocks continue.
Protocol51adds this explicit state/request/order contract; mixed old builds must be
rebuilt before connecting. Recorded root/bone pose tracks need no new archive codec.

## Focused evidence

Initial compile attempts found two missing namespace imports and one local-name
collision; fixed directly, logs retained. V3passed2/2in53.380s but native inspection
rejected the prone camera and identified a witness timing issue: the existing
screenshot helper renders before LateUpdate and hid the owner after recovery.
The local case now uses its existing observedSubject option and applies the current
post-graph pose at pre-cull. No shared capture framework was rewritten.

V4passed2/2in50.205s with corrected camera/native prompt. Final grip spacing and
reliable ownership/order changes passed3/3in66.907s (v5). The actual rooftop/Lagoon
hang, pull, same-edge landing, owner and reduced-camera images were inspected,
plus25percent greyscale and all seven current hero grip images. The roster test
covers every current Classic/Hero registry entry's palm contact and cancellation,
paused requests, duplicate/stale episodes and pose-order rejection.

One final real side effect was corrected: the new early movement return had skipped
stamina idle/fatigue clocks. Only the all-roster/reset/contact case was rerun with
an explicit resource-clock assertion:1/1in18.465s (v6). No extra taste variants or
unchanged fall/stock reruns after that. Retain the result. Raw screenshots/logs/full
churn patches remain in QUAL Logs/edge-recovery-v1throughv6.

Known generated Inday/meta/ProjectAuditor churn was restored after each run. The
roster pass also generated Rafi FPP tangents; byte comparison confirmed positions,
normals andUVs unchanged, then those owned temporary changes were restored. No
unrelated character or source-asset edits were published.

## Limits and next gate

These are Unity-native local physics/input/rig/state checks and source-level
network ownership fixes. They are NOT a fresh two-machine/real transport result,
physical-device test or final native-player/performance qualification. Real peers,
packet loss/rejoin/seat handover, normal complete matches and replay playback remain
explicitly in REFINE-2.10/P7. No intermediate player build or Desktop overwrite.
The named implementation unit is complete; overall maps and goal remain OPEN.
Return to the unfinished bakery/signage and map work. Ultimate research/upgrade
stays queued after current map work, per the owner's explicit latest instruction.
