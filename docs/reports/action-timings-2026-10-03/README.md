# Owner action timing revision and retrieval-slide removal

October3 request: full throw charge1.5s; punch/tag0.25s after a real hit and0.5s
on a miss; lunge unchanged0.5s full charge with0.5–2.5s recovery; shove7.5s hit,
0.5s miss; remove retrieval slide.

## Implementation

Local and host punch paths use the authoritative tag outcome. A nearby target
without a valid tag does not earn the shorter recovery. Punch and shove results
reach the predicting owner through scoped ContactRecovery receipts, preserving
elapsed recovery and rejecting foreign, stale, duplicate or malformed results.
HUD cooldown totals follow the accepted result. Protocol144 requires matching
builds; actual live-peer latency acceptance remains separate.

The attacker right-click path only shoves. Bot retrieval uses normal movement
and pickup. Slide impulse/sweep/resource-spend code was removed, its host/query
compatibility entry points always refuse, and a retired denial cannot refund
stamina or release another commitment. The former slide-only test contracts
are retained as historical text outside test discovery. Existing shove input
ownership controls remain. Shared animation assets used by other actions and
legacy numeric constants are retained; they do not enable the removed verb.

## Native evidence

Final25/25 PlayMode checks passed in1.2400361s at17:22:04–05UTC, exit0 and no
resource guard. Peak process tree4553613312bytes, container7203930112bytes.
Twelve frozen changed inputs match source and isolated native checkout, and
both project settings restored. Coverage includes exact timing values, actual
local/host hit and miss, can-down refusal, no slide impulse/grab/spend/refund,
11 receipt/authority/framing cases, and8 existing input-ownership controls.

Initial21/25 exposed a fixture that forgot a carried slipper, which the real
retrieval/tag rules require. The fixture was corrected to equip a real slipper
and place the miss target inside the box but outside punch reach. Production
rules were not weakened. The first repaired compile process hit RAM before XML;
the successful runtime reused its compiled assemblies.

## Managed evidence

Initial707/708 exposed an older match-entry assertion that still expected a
manual-ready default and only one appended wire field. The current automatic
entry/map-vote contract predates this unit. The obsolete expectation was updated
without changing production entry behavior. A command-directory mistake caused
one unchanged repeat before the test edit; that failure is retained. The final
full-suite result is recorded alongside this report.

These checks are not a packaged-player, physical-device, full-match or actual
multiplayer-peer qualification. Bank Shot presentation edits are a separate
unpublished work unit and are excluded from this change.

Final managed suite:709/709 passed, no skipped tests. The original explicit
legacy non-zero ManualReady field decoding remains tested unchanged; only the
missing-field default and the appended-map-vote prefix expectation were stale.
One intermediate test edit incorrectly changed that explicit legacy expectation;
its failure is retained, then the original decoder assertion was restored.
