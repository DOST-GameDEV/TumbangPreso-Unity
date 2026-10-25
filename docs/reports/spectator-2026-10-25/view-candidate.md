# Actual player view validation

The actual-view unit has local native and real-process validation. Broader
spectator framing/effect coverage and network reliability remain in progress.
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
- Windows candidate `6cdaef4261` built successfully in the canonical checkout.
  All 127 pre-existing dirty source/config files were restored and rehashed.
- Three real Windows processes, each with its own named profile, passed the
  owner-to-host-to-spectator test through the ordinary lobby, character selection,
  map opening and ready gate. The observer received 167 views; 163 distinct relay
  serials matched host samples for eye, full rotation, FOV, time and hand state.
  Applied eye smoothing differed by at most 0.0126 m during the controlled motion.
  It entered/left first person, followed 86-96 degree FOV changes and unsubscribed;
  owner publication stopped after unsubscribe. No exceptions in the three logs.
- Fresh canonical receipts: `Logs/view-peer-1025-02/summary.json`, per-role CSV/logs
  and `job.json`; runtime SHA256
  `a06998bda859e5b23dc49f4c867fba3ef2aa3060c7368de1a029825b4d4c3e81`.
  The first run remains as failed evidence: direct-arena admission assigned the
  observer a player seat, and the real midmatch seat-change refusal held. The
  fixture was corrected to choose Spectate before match start.

Transport replacement/unauthorized lifecycle cases, the full map/body/skill
matrix and private hallucination parity remain open. The opt-in
`NetObservedViewProbe` and `tools/net_observed_view.py` exercise actual processes
with camera-only owner motion, raw relay comparisons and unsubscribe checks.
No natural match, WAN, separate-machine or Android claim follows from this fixture.
