# Lagoon platform fall and button-mash recovery,2026-09-24

Ordinary airborne falls from Lagoon platforms now enter the same press-gated prone
recovery used by SaBubong after a real descent to water. The player returns to their
safe spawn, uses the existing mash meter/input/animation/camera sequence, and regains
control when accepted presses finish it. The shared anti-stranding guard remains.
Holding a button is one press. Both modes/all characters share this physical-fall
path; hero stun immunity cannot bypass it.

Grounded dry support and a0.75m drop distinguish a platform fall from supported water
stairs. Player identity, existing teleport serial and round changes clear tracking.
Intentional swimming/stair access and old under-deck/out-of-bounds failsafes remain.
A held slipper lost during a real fall uses Lagoon's existing eight-second return.
No new scoring, protocol field, recovery HUD, model or collider changes. Host-only
LagoonWater resolution reuses existing teleport/control snapshots/recovery episodes.

Focused checks passed2/2in47.921s: actual outer-rail jump, measured water descent,
no passive mash progress, held-button single press, repeated tap recovery, delayed
stock return and pickup in both modes; existing inner/outer stairs also passed in
both modes. These are controlled local physics/intent cases, not physical-device or
real-peer qualification. Existing bots use the same trip-input consumer.

Native prone/standing screenshots were inspected. Existing mash prompt is reached;
these debug-fixture views contain nearby staged actors and an old ready overlay,
so they are not clean motion/normal-match aesthetic approval. Preserve the images
and carry actual match/peer/device/presentation qualification into the existing final
gate, rather than spending this implementation pass repairing unrelated fixtures.

Flying birds are next, then the preserved gable/house/water/island art queue. Overall
TODO and goal remain active. No art task or earlier gameplay requirement was deleted.
