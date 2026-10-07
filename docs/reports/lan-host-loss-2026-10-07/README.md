# Active host-loss acceptance

The existing two-machine LAN runner now has an explicit client-only
`--expect-host-loss` scenario. The normal completed-match gate remains strict.
Recovery requires client READY, a real live round, then HostLost at round1/1
with the authority-revocation diagnostic. Its terminal report must return to
offline MatchSetup/Home, slot0, round0 and inactive. A completed-match event,
History, Queue or QueueWitness entry fails this interrupted-match scenario.
A missing career file is valid when nothing was completed.

Both machines must use the same checked artifact and fresh isolated profiles.
The PC separately owns and records its deliberate host termination while both
peers are live. Client loss evidence alone is insufficient to qualify the pair.
Existing full-artifact verification, prepared/start coordination, natural client
exit and profile/input preservation remain in place. No game/runtime change,
RAM admission barrier, external timeout or remote process control is introduced.

Eighteen runner unit checks pass, including negative controls for pre-match
loss, reversed event order, completed or queued records, a replacement network
host, a lingering live arena, wrong protocol and client crash. These are runner
checks; actual current-package paired host-loss acceptance is still pending.
Normal same-package admission/readiness/completion/save parity has already passed
and is recorded in the neighboring two-machine-lan-cb5f report.

This gate does not establish reconnect, every intermediate authority/input frame,
physical unplug, WAN/Relay, spectator/rematch, every map/ability or full-resolution
tournament performance.
