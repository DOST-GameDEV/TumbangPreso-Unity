# Remaining bot slipper planning scans

Nine remaining AIController scans now use the existing BotSlipperInventory.
Glance, cover, flying retrieval, thorn aim/count, denial, loose-shoe availability,
water interception consideration and void relevance retain their original
predicates and order. No selection is cached: state, position, ownership and
activity remain live. Birth/destruction invalidation and disabled-component
behavior remain owned by the existing inventory.

## Measured result

The native GC.Alloc recorder is calibrated with a deliberate4096-byte allocation.
One hundred warmed passes through five real planning helpers allocate1400events
before and zero afterward. Those helpers are flying retrieval, loose availability,
void relevance, thorn count and nested thorn aim. This does not measure total
glance/cover allocation: their separate actor enumerators remain unchanged.
Structural changes still allocate a new native inventory scan. No FPS claim.

Baseline2cases: allocation fails, live lifecycle control passes. Final7/7native
D3D11 cases pass: both new controls plus the five existing inventory controls,
including native duplicate selection, disabled/inactive objects, replacement and
flight/ownership. Source0b9475534plus two owned overlays;659frozen inputs have no
drift. Both source files match the tested candidate. Jobs67198/97546terminal;
guarded profiles/input preferences preserved. A manifest prior-name typo was
corrected before the first engine launch; no native fixture repair or retry.
Receipts: [bot-planning-checks](bot-planning-checks).

This shared query optimization changes no hero kit, tier, input, loading, asset or
packet contract. Protocol114 remains unchanged. Whole-match performance and actual
peer behavior are separate evidence.
