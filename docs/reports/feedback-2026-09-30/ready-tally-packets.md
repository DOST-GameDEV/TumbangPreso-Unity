# Ready tally packet bounds

F0930-34, candidate62544c46d plus the receiver guards and focused fixture.

OnReadyTallyMsg previously read two integers without checking payload length or
count bounds. Short payloads threw; trailing payloads and impossible tallies were
published through OnLobbyReadyChanged. The receiver now requires exactly eight
remaining bytes and a readable buffer before reading. Expected count must be
between zero and four; ready count must be between zero and expected.

Four guests remain valid when the host is seatless. Empty0/0 remains valid.
The existing host-sender and host-loopback gates remain intact. Packet format and
protocol are unchanged; no loading, hero or presentation behavior changed.

## Evidence

Native Unity6000.5.8f1 Windows EditMode, isolated named profile
feedback-0930-ready-tally, actual receiver with FastBufferReader:

- Baseline25cases:16failed,9passed. All ten malformed-length cases and six
  impossible-count cases fail before the fix.
- Fixed25cases:25passed. Ten malformed lengths, six invalid counts, six valid
  tallies, two non-host cases and host loopback.
- Frozen539overlay inputs exclude unrelated main-checkout dirt. No non-metadata
  input drift after the final run.

Raw XML and manifests are in checks/ready-tally. Full logs remain in the isolated
checkout's Logs/feedback-0930. This qualifies the native receiver and event path;
actual-peer transport and live matchmaking are not tested by this fixture.
