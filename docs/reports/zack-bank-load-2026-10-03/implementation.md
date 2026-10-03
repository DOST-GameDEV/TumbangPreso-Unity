# Bank Shot load gesture implementation

Appends hero-zack-bankshot (0.64s) and its dedicated owner bank-load path. Zack directs a compact free-hand gesture across the held shoe, then returns to his grip. The shoe never arrives from offscreen as in the old recall action. Lower-body walking remains enabled. No gameplay windup: charge accepts immediately and an immediate throw still consumes it. Cooldown35s, duration8s, bank retention85%, mechanics/SFX unchanged.

GLB preservation: original37animations, existing geometry/material/skin data and binary prefix unchanged. Appended38th animation, roster wired one clip, importer metadata unchanged.

## Evidence
- Edit3/3 (clip resolution, endpoints, existing clips, owner dispatcher, unchanged timings).
- First Play10/13: three fixtures cached Carrier too late; initialized real Carrier before HeroAbilitySystem, preserved failure. Repaired13/13.
- Canonical capsule/model-child placement then applied to visual fixture. Body-film cached1/1 with48 frames, actual CSV30fps cadence, inspected selected motion/recovery frames. First graphics compilation blocked by memory before capture.
- Current dependency overlays include protocol144 action timings. Fresh headless13/13,1.4961771s at17:57:03–04 UTC, guard-free, peak tree3,368,353,792/cgroup6,496,755,712. Both project settings restored and14 owned input hashes verified. This checks accepted casting, loaded-shoe ownership/attachment, immediate throw, expiration/role cleanup, owner hand movement and walking gait. Headless is not visible owner acceptance.

## Limits and failures retained
Current owner/walking graphics attempt hit the unchanged cgroup headroom guard before frames; after closing the task-started Hub and splitting to owner-only, graphics was still blocked. Effective container memory.max is8GiB (reserve1GiB); no guard weakened. Native shutdown produced SIGSEGV in those interrupted processes. No owner/walking film claimed. Alternate headless test qualified logic instead of repeatedly retrying graphics. Isolated native checkout uses explicit overlays, not whole-current-branch acceptance.

Body view reads as a short chest-level cross-hand load with stable feet and intact grip; it is not a large electrical spectacle. Owner-view composition, real map/multiplayer view and human taste remain open. No new audio or listening claim.
