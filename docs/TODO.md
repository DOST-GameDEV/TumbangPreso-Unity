# Current work

This is the only active queue. Updated 2026-10-26 from source and the owner's
current assignment. Older unchecked boxes are not automatically unfinished work.
[Retirement decisions](RETIRED_WORK.md) explain superseded requirements.
All original IDs and full bodies remain in the
[previous queue](archive/cleanup-2026-10-25/docs/TODO.md),
[backlog](TODO_Backlog.md) and [closed history](TODO_Archive.md).

## Current implementation queue

| ID | Status | Acceptance / next action |
|---|---|---|
| LOBBY-UI-1028 | DESKTOP FIXES integrated | Knit rules sheet, MATCH SETTINGS footer, joined-room spacing and immediate input on rules close are integrated. Evidence: Logs/lobby-ui-20261028/fixes/. Phone work excluded. |
| ONLINE-CAST-1028 | ACTIVE: final build/peer check |7bc9cf91a pushed. Six map pairs, impaired LAN and Relay: distinct heroes, no exceptions or flagged offsets. Nemu hand-back preserves4.2m floor; Ermita lighting stable. Old MIRROR preference migrates to standard. First Dante stone load caused9s recording gap: hidden shape preparation/playback regression1/1. Unlock gate1/1; Core97/97. Logs/floating-20261029/. |
| REPO-CLEANUP-1025 | DONE | Cleanup aaf96e52e1 integrated into main; BugFix deleted after ancestry verification; NETWORKFIX created at the merged commit. 107.2 MiB assets removed; entry docs 97.62% smaller. [Evidence](reports/repository-cleanup-2026-10-25.md) |
| ZACK-SPEEDSTER-1025 | Visuals approved; SFX review open | Owner approved electrical-outline rewind `308fb345f` for publication. The skin-mounted hand zigzag overlay and its retired test are removed, with a final native capture passing 1/1. Rewind integration 23/23 plus final stroke capture 1/1 passed; both exports were reviewed at 1x. The procedural SFX challenger was rejected; source-material auditions remain separate from the installed mix. Listening acceptance and actual peers remain open. [Evidence](reports/zack-kit-2026-10-25/refinement-validation.md). |
| SPECTATOR-1025 | DONE desktop, main | Controls/pacing, actual player FPP, body/effect framing, all six maps and private hallucination/held-marker delivery implemented and checked. Current skill/prop/transition checks are recorded with NETWORKFIX; mobile remains its explicit separate assignment. [Evidence](reports/spectator-2026-10-25/README.md) |
| NETWORKFIX-1025 | DONE within recorded Windows/Android scope, mobile+network | Host/referee loss stops old simulation; departure card supports manual spectator pause, real yellow/red quality badge, bounded loading buffers and stable LAN identity. Android same-process and background reconnect reclaimed the original seat/match; physical/WAN/Relay limits remain. [Evidence](reports/mobile-2026-10-26/README.md); [earlier Windows checks](reports/networkfix-2026-10-25/README.md). |
| MOBILE-1025 | DONE within recorded emulator scope, mobile+network | Touch aiming/lifecycle, spectator tools/replay, safe areas and actual source lens implemented. Windows/Android player/spectator/host view transfer, seat-preserving reconnect, actual phone UI taps, six map scenes and Classic HUD checked. Lossless asset payload is about71% smaller. [Evidence and limits](reports/mobile-2026-10-26/README.md). Unrelated future sessions do not automatically owe mobile updates. |
| MOBILE-UI-1026 | HANDED OFF at owner request, mobile+network | ff9 APK uploaded as candidate; native41/41 and recorded actual touch journeys passed. Newer source6/7 with picker clipping failing. Final fetch found newmain43c158b86; integrate/recheck compatibility before rebuilding and finishing target match/replay/crossplay checks. [Exact handoff](reports/mobile-ui-2026-10-26/HANDOFF.md). No future every-feature mobile mandate. |

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
