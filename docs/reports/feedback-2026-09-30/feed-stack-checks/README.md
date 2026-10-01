# Score feed stack refinement

Entries now last three seconds, with at most three visible. New entries arrive
at the top; existing entries move smoothly downward from their current visible
position, including when another insertion interrupts that movement. Insertion
does not reset older entries' expiry. Reduced motion snaps to the new positions.

One focused native PlayMode case passes: ordered entry retention, intermediate
movement, interrupted insertion continuity, unchanged expiry, capacity, final
positions, reduced-motion snap and three-second expiry. Small viewport inspected.
Frozen inputs unchanged, no tooling repair/new OOM. This is UI evidence, not new
scoring or actual-peer qualification. Status stacking and announcement rules
remain separate unfinished parts of the combined Feedback row.
