# September 30 Feedback Fixes

## Tasks And Round Standings

Candidate: committed aecc0ee2c plus eight task-owned source/test inputs.
Unrelated checkout edits and generated validation assets were excluded.

Tasks now retains unchanged rows through wallet refresh/busy notifications, keeping
their existing entrance state. Changed data updates without replaying the entrance.
Claim availability and status text still refresh.

The ordinary HUD hides while the actual round card is visible, including normal
breaks. The requested standings are centered, the duplicate warmup line is hidden,
and score formatting fits the actual chips without changing match totals.

One guarded Unity 6000.5.8f1 D3D11 PlayMode run passed both changed-flow cases:
2 total, 2 passed, 0 failed/skipped, 54.1619316 seconds. Input hashes stayed unchanged.
The named offline profile and shared Editor preference were guarded. No live wallet,
peer session or new player build was used.

- [Native results](checks/ux.xml)
- [Frozen inputs](checks/ux-inputs.json)
- [Tasks refresh](Tasks-refresh-960x540.png)
- [Round standings at 960x540](Round-break-960x540.png)
- [Round standings at 1600x680](Round-break-1600x680.png)

The three captures were visually inspected. Round-card dismissal and round advance
restore the HUD; the sampled score values include int.MaxValue, 999999, 10000 and 2670.
This evidence covers these local UI paths, not actual-peer or physical-device acceptance.
