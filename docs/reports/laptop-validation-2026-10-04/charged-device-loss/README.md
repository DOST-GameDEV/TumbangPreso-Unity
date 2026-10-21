# Charged input survives device changes safely

Removing the driving controller made the reader publish a release: Carrier
launched the held slipper, and CombatVerbs committed a lunge with0.5s recovery.
Adding/removing an unused pad also lost the primary held action during binding
re-resolution. The original five cases produced three failures and two passing
controls. The unused-pad case includes both addition and removal; it does not
isolate removal as the sole cause of that failure.

PlayerInputReader remembers each charged action's actual driving device, marks
its loss and checks aggregate input after InputSystem updates. A lost throw or
lunge is retired only if no input still holds that action. Carrier has a narrow
throw retirement that leaves reset-channel state intact; the existing lunge
retirement preserves committed contact/recovery. There is no blanket input,
touch, hero or companion cancellation. Supported initial-state checks recover
alternate controls. If re-resolution retains a processed float but loses
IsPressed, only throw/lunge recover their held state with the actual button
press point and normal release-threshold hysteresis.

Original source dd3c4563698b98b3aa00eb023f5aa0ec14523b69 plus declared fixture:
3fail/2controls. First candidate40740f5f38f0d41fc1a2dae22ca09b74e5d40987:
4/5, fixing the two accidental actions but still failing the unused-pad hold.
Candidate daa00beeec2240d2b5247c0bf346a0755517597d:5/5. The exact five-case
fixture/meta remain unchanged across all three runs. Only PlayerInputReader.cs
and Carrier.cs differ between original and final candidate3432-input maps.
The failed partial candidate is retained; its remaining product failure led
to the processed-value correction, not a diagnostic or fixture retry.

Four additional meaningful controls and seven existing reader regressions pass
11/11 on coherent source2999163e89d8b88d097796a08dcadb9d7c776380, integrating
the checked PC replay queue fix. Added controls preserve a still-held mouse,
a0.45 trigger above the release threshold then its0.3 release, unrelated touch
movement/hero key and committed lunge contact/recovery. Existing cases preserve
received tells, clear obsolete local tells and retain committed state on reader
withdrawal. Prior unchanged practice/font/packet/replay cohorts were reused.

All four serial D3D11 PlayMode jobs ran on gamergmae Windows11/Unity6000.5.8f1,
GPU4096MiB/reserve1024MiB/300s with isolated validation identity, physical Library
and profilevalidation-qa-a-be075dcdfa07. Each guard is terminal, preservation
complete and lease free.3432/3432/3432/3434 declared hashes match after byte-exact
QualitySettings restoration. Pre/post quality and exact tested raw source/fixtures
remain alongside XML, receipts and full maps; no data or importer work discarded.

This is injected-device state followed by shipping reader and consumer callbacks.
It does not qualify physical unplug, OS focus/background behavior, every custom
binding/interaction, every hero, packaged-player or live-peer acceptance. Reset,
disable and disconnect branches share the retirement handler but were not separately
injected. PRACTICE-BOT-RESUME-1002 operator acceptance remains open.
