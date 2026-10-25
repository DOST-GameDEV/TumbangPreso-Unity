# Current work

This is the only active queue. Updated 2026-10-25 from source and the owner's
current assignment. Older unchecked boxes are not automatically unfinished work.
[Retirement decisions](RETIRED_WORK.md) explain superseded requirements.
All original IDs and full bodies remain in the
[previous queue](archive/cleanup-2026-10-25/docs/TODO.md),
[backlog](TODO_Backlog.md) and [closed history](TODO_Archive.md).

## Current implementation queue

| ID | Status | Acceptance / next action |
|---|---|---|
| REPO-CLEANUP-1025 | IN PROGRESS | Shorten instructions and live docs, audit all Markdown, retire obsolete queues, remove proven unused assets and obsolete Kenney credit; validate before integrating BugFix into main, delete BugFix, create NETWORKFIX |
| SPECTATOR-1025 | OPEN, main | Clear controls-only Tab help; calm readable director; full skills/props/cutscene visibility; target follow with actual FPP/TPP; graphics and regression evidence; push to main |
| NETWORKFIX-1025 | OPEN, NETWORKFIX only | Reproduce disconnect/reconnect/lag/remote-state defects, fix causally, notify spectator on departure, exercise actual peers without regressing current behavior |

## Existing evidence limits worth retaining

These are observations to check when they overlap current work, not extra
assignments to redesign the game. Source and fresh reproduction decide priority.

| Previous ID | Current disposition |
|---|---|
| SEAN-LAVA-BRAWLER-1024 / ULT-READ-1024 | Rago's new kit, six-hero unlock and mountain/fist work are implemented; old Cinder Gate/E-bomb/parol tasks are retired. Keep 1 m Magma Fist reach. Owner appearance/audio review and live-peer acceptance remain unverified. [Report](reports/sean-kit-2026-10-24/feel-and-readability.md) |
| CAN-CARRY-1024 / CONTACT-1024 | Carry/contact implemented with local tests. Actual remote carry, shared hold and every-view behavior require current peer checks. Do not reimplement old reset channel. |
| TOURNAMENT-FEEDBACK-1022 / TAG-READABILITY-1023 | Delivered presentation is retained. Current spectator request supersedes old Tab-card and autopilot acceptance; inspect tag/replay failures only where still reproducible. |
| NEMU-SOULBOUND-1021 / NEMU-NETWORK-1015 | Current ghosts/state replace old KURO and intermediate ghost designs. Verify current observer/rejoin visibility; no old-kit restoration. |
| HALFTIME-HIGHLIGHT-1021 / OPENING-HUD-1022 | Timing/HUD fixes are implemented; old receipt limits and four-round exclusion are retired. Current peer/saved-art behavior remains bounded by source and reports. |
| EMOTES-SECOND-SET-1023 / SFX-TONDO-1023 | New emote set and audio authoring work are retained. Owner taste/listening and remote playback remain distinct from local evidence. No automatic new content pass. |
| ROOM-PLAYTEST-1023 / MAIN-MAP-PREVIEW-1020 | Delivered room/title and integration fixes are complete. Retain actual defects if reproduced, not old merge instructions. |

Known older test failures include retired-map setup, outdated HUD waits/art
expectations and LAN socket availability. Establish a baseline before blaming
new changes; do not weaken tests or call these product acceptance.

Do not reactivate numbered backlog proposals, old contributor lanes, rejected
art studies, completed branch merges, Desktop-build replacement tasks or expired
competition schedules without a current reason. Keep a short resume state in
[ACTIVE_REWORK_LEDGER](ACTIVE_REWORK_LEDGER.md), not another work queue.
