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
