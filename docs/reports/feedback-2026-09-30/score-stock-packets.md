# Score and Last Tsinelas packet bounds

## Reproduction

The receiving client read Score's three integers before checking whether they
existed. Its Last Tsinelas handler likewise read the header and stock table
without validating the complete payload. Truncated messages raised a native
FastBufferReader OverflowException; complete-looking messages with trailing bytes
still published a score notification or replaced the stock table.

Sixteen native PlayMode cases invoke the actual handlers with FastBufferWriter /
FastBufferReader and the real match/stock directors. Before the correction,
ten fail: five Score boundaries and five stock boundaries. Six valid-payload /
sender-control cases already pass. These are controlled packet injections, not
an observed production connection failure or an actual transport peer session.

## Correction

Score requires exactly12remaining bytes before reading. Last Tsinelas requires
its8-byte header, the existing0-4count bound and exactly4bytes per declared stock
before allocating or applying the table. Truncated and trailing payloads produce
no event or stock mutation. Sender authority and host-loopback guards remain.

The published payloads, stock-count meaning, zero-filled short-table behavior,
scoring totals, stock rules and protocol97 are unchanged. No new wire field or
message semantic is introduced; only malformed envelopes are refused.

## Validation

Unity6000.5.8f1 Linux64, graphics, named isolated cloud-score-stock-packets profile.
The same16cases pass after the targeted correction, with runtime/test hashes
unchanged throughout the run. No assertions or test inputs changed between runs.

- Score rejects0,4,8,11and13byte payloads without a reader exception or event.
- Stock rejects0,4and7byte headers, truncated tables and trailing bytes without
  changing the existing stock table or publishing StocksChanged.
- Valid score produces exactly one notification and leaves replicated total alone.
- Valid stock counts0,2and4 retain their previous interpretation.
- Non-host sender and host loopback cannot publish either kind of update.

[Before:6pass/10fail](checks/score-stock-before.xml).
[After:16pass](checks/score-stock-after.xml).
No authored map, player build, real-peer or cross-platform qualification is claimed.

Feedback records remain separate: hidden QA_TUMP_0040 for score notification and
QA_TUMP_0041 for Last Tsinelas stocks. Human verified is left for testers.
