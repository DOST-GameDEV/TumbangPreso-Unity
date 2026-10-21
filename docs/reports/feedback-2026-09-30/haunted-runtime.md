# Haunted runtime timer, delivery and marker gates

Haunted now has its own7.5-second runtime timer. It refreshes by maximum rather
than stacking, does not block ordinary actions/movement, and bypasses Status
Immunity as the current Wiki requires. Explicit cleanse and round/body reset end
it. Remote unapproved callers cannot create it; authoritative snapshots adopt
bounded remaining time and empty state.

SyncUnit appends the timer before the existing Voodoo/Aim blocks. Capacity/minimum
read size are adjusted by four bytes. The host/loopback/match/round/body/serial
gates remain; invalid Haunted values are rejected before state or serial mutation.
The original Voodoo fields/behavior and existing status IDs are unchanged.

The affected local player's actual SlipperRecall and OffscreenIndicators gates
hide the slipper/can HUD markers while Haunted is active. Clearing the timer
restores their existing tracking. No marker artwork/layout, world beam, authored
model, animation, VFX, SFX or loading path changed.

## Evidence

Unity6000.5.8f1 Windows D3D11, guarded feedback-haunted-runtime-1001 profile.
Source1a1c989f7 plus the explicit owned candidate,622 frozen inputs.

- First run6cases:5passed/1HUD setup failure. The new recall fixture omitted its
  required Build(owner) call, so the marker was absent before applying Haunted.
- One fixture repair calls Build and forces canvas layout. Only the HUD case is
  rerun; it passes1/1 for both real markers hiding and restoring. The four actor
  timer/authority/immunity/invalid-input cases are reused.
- The existing real SyncUnit receiver case passes, then is expanded to deliver
  Haunted3/2/0 and invalid NaN/negative/over-limit values. The expanded case passes
 1/1 with state/serial preservation and unchanged Voodoo reach/resource outcomes.
- Six distinct native cases pass across retained final receipts, not one final
 6/6 suite. Source/input hashes have no drift after the final wire case.

Exact receipts are in haunted-runtime-checks. Raw logs stay in the isolated
checkout Logs/feedback-0930/haunted-*.log. This proves native component/receiver/HUD
behavior, not real peer transport, target player/hardware or full Nemu Haunt.
The existing protocol103 player does not qualify the new status field.

Haunt chase/target lifecycle, nearsight and audio muffling remain unfinished.
The original F0930-12/Feedback row stays open; do not mark the complete feature
done from this timer/delivery step.
