# Actual player view candidate

This is a validation candidate, not completed spectator/network acceptance.
The published controls/pacing unit is described in [the main report](README.md).

An active player rig supplies its actual eye, full rotation, FOV, near-fade and
hand animation state. The spectator copies that presentation after camera/hand
updates without enabling the player's input. Remote views use a separate bounded,
capability-negotiated owner-to-host-to-spectator channel, only while subscribed.
Missing/stale views are labeled; a body-yaw/operator-pitch guess is no longer
presented as the player's actual view. Bots/reference views are identified as such.

Camera perspective identity also routes victim screen effects, Nemu's lens cover
and carried-can presentation. Free/third-person views must not inherit the
victim's screen. Borrowed hands advance once, preserving stowed/held prop state.

Evidence so far:

- Native codec6/6: serialization/bounds, all truncations and trailing data,
  nonfinite/out-of-range fields, names, time and serial order.
- Native actual camera/hand/emote plus body/lifetime/gamepad controls26/26.
- All six actual-view/perspective checks pass after the fixture matched the real
  MainCamera tagging. Full carry validation had one intermittent base-height
  failure; focused old/new drawing-predicate controls both passed. Failure retained.
- Existing Nemu dormant test expects a fixed point while current dormant ghosts
  follow their owner; no mechanic was retuned to satisfy that stale expectation.

Real host/owner/observer delivery, stale/unauthorized cases, the full map/body/skill
matrix and private hallucination parity remain open. The opt-in
`NetObservedViewProbe` and `tools/net_observed_view.py` exercise actual processes
with camera-only owner motion, raw relay comparisons and unsubscribe checks.
No natural match, WAN, separate-machine or Android claim follows from this fixture.
