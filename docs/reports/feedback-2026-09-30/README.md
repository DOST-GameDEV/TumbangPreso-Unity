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

## Tutorial

The route now requires three separate takeoffs, fills and greens completed progress,
waits through the shared ultimate introduction plus 2.5 seconds, and excludes the
deprecated Mash lesson. Held ability information compacts the objective card;
release restores its description and key row. Quitting cancels pending lesson work.
No ability behavior, clips, models, effects, maps or loading paths were changed.

The real-jump/common-completion case passed in the initial run. The other case
required correcting its synthetic-input focus configuration and a measurement
that compared camera-world coordinates with overlay pixels. The final case composites
both real canvases and measures pixels, then exercises a real ultimate press/release.
It passed 1/1 in 12.6118027 seconds. Runtime inputs remained frozen; profiles and
shared Editor input preferences were restored. No broad suite was repeated.

- [Initial results, including the fixture failure](checks/tutorial.xml)
- [Final Tab/ultimate case](checks/tutorial-final.xml)
- [Initial inputs](checks/tutorial-inputs.json)
- [Final test input](checks/tutorial-final-inputs.json)
- [Three real jumps and green completion](Tutorial-three-jumps.png)
- [Tutorial and Tab at 960x540](Tutorial-tab-960x540.png)
- [Tutorial and Tab at 1600x680](Tutorial-tab-1600x680.png)

All three captures were inspected. This is local native state/input/layout evidence,
not physical keyboard/controller/touch certification or all-hero cutscene qualification.
The owner must still name the tutorial completion destination; existing manual exit
is retained. Loading is assigned to the owner's friend.
