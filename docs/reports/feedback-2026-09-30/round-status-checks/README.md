# Round status carryover

Harry reported Tagged and other effects surviving the next round. The native
baseline reproduces an actual EndRound/BeginIntermission/AdvanceRound transition:
Tagged remains at5seconds after the new round starts. Teleport already clears
elemental statuses, but the independent stun/trip stacks were not reset.

SliceRunner.ResetWorld now calls the existing ClearStun and ClearTrip methods
before resetting the rest of each seat. No ordinary teleport/tag penalty or kit
timing changes. This also retires the old recovery episode through existing code.

Final4/4native pass: Tagged/trip do not carry over, Chilled/Haunted clear, temporary
fields retire while authored map hazards and recorded geometry remain, and both
round-inactive and skipped-inactive snapshot field controls pass. Frozen input
hashes unchanged; OOM11/kill6unchanged. No tooling retries.

No new player or actual-peer qualification. This closes the status-reset portion
of the combined Feedback row. Can-down throw gating and centre-dot request are
separate pending work; do not mark the whole row done yet.
