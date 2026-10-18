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
checks. The actual same-package client now passes the focused gate below.
Normal same-package admission/readiness/completion/save parity has already passed
and is recorded in the neighboring two-machine-lan-cb5f report.

This gate does not establish reconnect, every intermediate authority/input frame,
physical unplug, WAN/Relay, spectator/rematch, every map/ability or full-resolution
tournament performance.


Actual PC host40372 starts00:40:18.740082UTC, laptop client59804 starts
00:40:46.646170UTC (27.906s gap), using sourcecb5f4ad9f84fdde4eeb856dc1caca0eef2ab4944,
protocol153, Runtime SHAe9b69cf6d8c86a4a15cc2fbb16947312153209be5cd677e1383becf87c06cdd4
and the same261-file manifest43d83532e35eb85a6d0d0c98e80ba05272922cc6a07b1e167409a2d9a61723c8.
PC192.168.1.7 hosts49157; laptop192.168.1.144 joins from49158. Fresh isolated profiles,
640x360 Low graphics and ordinary automated room/ready actions are used.

The PC reports verifying both actual READY/liveRound1/noMatchOver/noAbandon at
00:41:50.1956542Z, rechecking the retained process creation/command, then stopping
only host40372 at00:41:50.3057485Z. Its deliberate nonzero exit and missing normal
state report are expected, not a normal-host PASS. PC retains its independent
before-stop host/client logs, paired client launch, live-stop and restoration
receipts. Combine those raw receipts with this laptop proof to qualify the pair;
the local coordination JSON records the message and does not substitute for them.

Client59804 actually receives ProtocolTimeout/HostLost: ABANDONED at round1/1
with authority revoked. It produces no match-over event. Its natural terminal
report is offlineHOST, slot0, MatchSetup, round0 and inactive. No career file,
History, Queue or QueueWitness entry is produced. Strict runner passes exit0,
errors[], all fresh-profile/input preferences restored and every packaged file,
Runtime and manifest unchanged. The client is not automatically hosting a new
LAN room. No runtime networking fix was needed for this current-package scenario.

The read-only client log endpoint served only this synthetic run for timely
PC observation; it had no chat, start or remote-command endpoint. The actual
functional client source is the pinned older cb5 package, not the newer preview
callback fix or subsequent AI source. Link samples4628ms/851ms occur during
loading/arrival before the live Good17ms sample, so this is not hitch-free
performance acceptance. The short live sample510frames/8.505seconds averages
59.96fps with18.51ms worst at Low/640; it is not a full-resolution tournament
benchmark. Broader remaining gates above stay open.


The independently owned [PC paired report](../two-machine-host-loss-cb5f-2026-10-07/README.md)
is now published in02944bd7e and its exact-byte receipt correctione764ee332.
The laptop independently verifies all11 committed raw receipt hashes, exact byte
equality of all four independently copied client files, both pre-stop live logs,
retained PIDs/time/artifact identities and both preservation outcomes. Git had
normalized CRLF in the PC's first publication; scoped receipt attributes and
re-adding retained originals restore every recorded hash without changing the
hash expectations or rerunning gameplay. `paired-verification.json` records
these checks. This closes the agreed functional active-host-loss gate within
the stated scope; broader acceptance remains open.
