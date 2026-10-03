# Retired duplicate validation caches

The owner requested regular deletion of obsolete task outputs. Two unused
validation-only worker Libraries were removed with native PowerShell after
checking exact resolved absolute folders, worker ownership, no active project
process and no reparse points. Receipt.json records both measured byte counts
and successful removal:10491541961bytes,9.77GiB total.

Only qa-b and qa-c derived caches were retired. Their source checkouts, logs,
profiles and raw acceptance remain. Active e3 qa-a/qa-d caches and the shared
1003e artifact were preserved. Retired markers require fresh warmup before reuse.
No whole worktree, source asset, private artwork, save or unrelated process was
removed. The separate PC reported a policy blocker; this receipt covers only
successful laptop-local cleanup.
