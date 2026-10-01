# Existing Can-Down Indicator, 2026-09-30

The incoming fix was already present; no duplicate UI/effect implementation was
added. A focused native case on the current controls/charge candidate passes1/1
in9.7994559s: the local round-one taya sees no frame for an upright can, sees the
can-down frame after a knockdown behind them, never sees attacker danger, and the
frame clears after restoration. Offscreen can tracking records visible/DOWN.

The1280x720 capture was inspected for the frame, clock state and reset prompt.
The exact arrow bounds were not separately asserted. This is local state/render
evidence, not actual-peer or every-hardware acceptance.

- [Native result](checks/can-down.xml)
- [Inspected capture](CourtHud-taya-can-down-1280x720.png)
