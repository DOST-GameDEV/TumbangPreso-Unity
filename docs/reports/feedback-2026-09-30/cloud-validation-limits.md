# Cloud validation recovery and limits

## Memory recovery

The Linux environment's native cgroup has an 8 GiB hard memory limit, despite a
larger host memory figure. Its event counter recorded five OOM kills before
recovery. Forty-eight orphaned, exact-match Unity SDK shutdown helpers held about
2420 MiB summed RSS. Only those verified task-owned commands were terminated.
No unrelated application was stopped and no system memory limit was changed.

The guarded isolated run now records cgroup pressure and task process memory,
checks for overlapping Editors, and cleans only the known shutdown helpers before
and after execution. Its safety threshold stops the owned Editor before further
memory exhaustion. A diagnostic stopped at that threshold is not a game crash.

The full Eskinita retained-replay motion/isolation case passed 1/1 after cleanup,
with fresh XML and exit 0. Subsequent full-map comparison runs also completed with
no new OOM event. They approached the 8 GiB cap, so this is not evidence of unlimited
headroom or uninterrupted endurance operation. Blender was not running and was
not removed: removing it would free disk, not the occupied process memory.

## Unqualified rendering observation

The cloud uses Linux OpenGL with Mesa llvmpipe software rendering. Full-map
captures showed mostly black/orange character surfaces. Controlled comparisons
found the symptom in both reconstructed copies and live bodies, with the normal
main camera as well as the replay camera. It persisted without the camera colour
grade, screen-space outline, ink hull or shadows. An isolated character-pair
capture previously retained normal colours.

This does not establish that the Windows D3D11 player reproduces the appearance.
The owner specifically questioned the representativeness of the cloud captures.
No shader, authored material, lighting, model, animation or gameplay change was
made in response. The cause remains unresolved; do not mark a player visual bug
fixed from these comparisons or use these images as visual acceptance evidence.

A Vulkan launch detected no supported Vulkan device and fell back to OpenGL.
It was not an independent Vulkan qualification. The temporary comparison fixture
and raw captures are retained privately with the run records; the fixture was
removed from the source candidate after investigation. One fixture error was
corrected: disabling the replay component invokes its cleanup and destroys the
camera. That was a diagnostic mistake, not a gameplay defect.

Behavior assertions, automated bot matches, authored visual acceptance and
human play verification remain distinct. Fresh test counts alone do not qualify
layout, graphics fidelity, physical devices or real network peers.

## Classic full-match behavior

The existing Classic bot behavior assertions passed on an isolated Eskinita
candidate: eight 90-second rounds, four bots, 207 throws, 198 retrievals, 34 can
knocks, 49 resets and 167 tags. The match reached its normal finish. No camp or
unretrieved-slipper penalties were recorded, and no hero kit was exercised.

This uses the existing fixed 1/60-second simulation step, not real-time human
play. The private adapter only selects texture mip limit 2 and restores it;
gameplay assertions and rules are unchanged. Fresh XML reports 1/1 passed,
144.56 seconds test duration; the guarded process exits 0 with no new OOM event.
The candidate overlays include the shipped tag commitment and bot obstacle-query
changes. It is not a clean new player build or peer qualification.

Evidence: [XML](checks/classic-full-match-cloud.xml) and
[match counters and trace](checks/classic-full-match-cloud.txt). Long loose-shoe
samples with canAct=False include inactive presentation intervals and are not,
by themselves, proof of a stuck bot.

## Bridge follow-through and native Linux player

Classic Ilalim ng Tulay also completed all eight rounds:198 throws,193 retrievals,
41 can knocks,55 resets and165 tags. Existing behavior assertions passed1/1,
with no camp/idle penalties and no new OOM event. This is an independent map
coverage result, not a repeat of the unchanged Eskinita case. See the
[XML](checks/classic-bridge-cloud.xml) and [trace](checks/classic-bridge-cloud.txt).

An internal Linux x86_64 player was built through the existing GameBuilder
pipeline, retaining the authored-animation and scene-script gates. The cold build
was stopped by the memory safety threshold during compilation, without a kernel
OOM kill. One warmed-cache retry succeeded:2076MB,26 seconds build phase,61.93
seconds guarded process,exit0. All38 retained .anim file hashes remained unchanged.
The build identity honestly records detached83136899 plus dirty candidate overlays,
protocol97. It predates the later controls label/wheel-order integration and is
not a clean build of the latest remote HEAD or a replacement Windows release.

The actual player reached Guest, title,Home,Practice and the Eskinita training
ground. Desktop-native keyboard input moved the player; the training menu and
settings opened and closed. The training clock is deliberately fixed, so this
route alone cannot certify an ordinary round's pause timer. Relative mouse look
through the remote-control surface is not yet reliable enough for a claimed
manual aim/throw/contact qualification.

The earlier missing Vulkan driver was addressed with the official Debian Mesa
25.0.7 userspace package, isolated from system installation. Selecting its CPU
device explicitly with -force-vulkan -force-device-index0 starts the player;
normal automatic selection rejects that CPU device. Both OpenGL and Vulkan
software-rendered native players still show the character/prop surface anomaly.
The existing Standard/Nostalgic lighting setting changes the scene look but does
not remove it. The cause remains unresolved and Windows reproduction remains
unverified. No authored visual or runtime gameplay correction was made for this
platform discrepancy. No Blender removal, paid GPU or system security change
was needed. All opened native player processes exited normally after the checks.

## Rooftop behavior

Classic Sa Bubong completed eight rounds with195 throws,189 retrievals,43 can
knocks,55 resets and168 tags. The existing liveness assertions passed1/1 with
normal match completion and no new OOM event. There were no camp or idle penalties.
This sample does not separately force every off-roof slipper recovery or qualify
map visuals. [XML](checks/classic-rooftop-cloud.xml),
[counters](checks/classic-rooftop-cloud.txt).

## Linux peer admission remains unqualified

A real two-process Classic rematch check did not reach its first round. Both
processes wrote fresh state reports: the host remained at round0; the joining
client received ClosedByRemote and ended non-networked. No rematch began and no
OOM event occurred. This does not establish a Windows regression or its cause.
The host's logged arena preparation took13.96 seconds while the existing runner
started the client after7 seconds; NGO's configured approval-buffer timeout is
10 seconds. Startup timing is a hypothesis, not a confirmed diagnosis. A separate
30-second host-settle control completed; it does not qualify the original cold
entry. No production timeout or connection rule was changed.

Before this OpenGL run, the CPU Vulkan batch-mode attempt crashed natively inside
QualitySettings.set_vSyncCount during initialization. The same Vulkan binary
worked interactively. One bounded renderer repair switched the peer check to
OpenGL; those players then exited normally with the failed admission reports.
That native batch failure is a platform qualification limit, not a proved game
state regression.

- [Cold admission result](checks/linux-cold-rematch-result.json)
- [Host state](checks/linux-cold-rematch-host.txt)
- [Client state](checks/linux-cold-rematch-client.txt)

The warm-host control admitted the client and both peers submitted Ready. The
client then disconnected with ProtocolTimeout. The host completed its first round
and moved to BayanPlaza but was waiting at round0 when sampled; the client remained
non-networked in Eskinita. Thus additional host preparation time improved initial
admission without solving the session failure. The host recorded about30FPS during
its live round, but that excludes startup stalls and is not a general performance
qualification. No new OOM and no runtime edits occurred. Both processes exited.
[Control receipt](checks/linux-warm-host-control.json).
