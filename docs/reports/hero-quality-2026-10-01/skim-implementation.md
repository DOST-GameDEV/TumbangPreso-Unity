# Skim implementation plan

The published role replaces legacy Mirrorwake. It loads the currently held
slipper for eight seconds, costs a 35 second cooldown, and only the next real
throw can consume it. No slipper means refusal without cost. Body/can contact
consumes it normally. Its first real ground contact starts at most two metres
of low forward travel; ordinary loose retrieval follows. No second score event,
automatic pickup, teleport, cover penetration or off-court endpoint.

## Chosen integration

- Keep stable rafi_skill2 identity, existing role routing and input contexts.
- Bind the loaded state to the actual held slipper, not merely a hero boolean.
  Drop, replacement, expiry, role/round exit and kit reset retire the grant.
- Reuse timed-kit recovery for the eight-second load, with the owning ability ID.
  Do not restore by activating/spending or replaying presentation.
- Append a Skim affinity value to the existing slipper snapshot contract. The
  host carries it through the genuine throw. First ground contact enters a
  bounded host flight phase so the shoe cannot be grabbed halfway through the
  slide or gain another throw chain. Existing pose/state replication shows it.
- During slide, sweep the real geometry, keep the legal playable boundary and
  supporting floor, and stop before bodies/can/cover or an unsafe drop/step. End
  in the existing Land path. No extra score or fresh launch call.
- Generic contact/reset/throw transitions clear private slide state. Never let
  pooled or recovered equipment retain a previous throw's travel allowance.
- Keep inactive historical sidegrade IDs and historical Mirrorwake world replay
  decoding until their own migration. Do not let them spawn a live decoy from
  the new role. New role presentation needs its own native film pass afterward;
  an old feint clip is provisional and cannot establish creative completion.

## Questions the checks must answer

Held identity/refusal, eight-second expiry, exactly one consumed throw, genuine
ground start and <=2m travel, normal loose end, body/can consumption, cover and
court edge, round/reset interruption, Classic unaffected, and timed recovery
without recast. Native tests must use real Carrier/Slipper paths for the cast
and ground contact; narrow private helpers alone do not establish play.
Core owns tuning and bounded slide math. New semantics require a compatibility
bump and later actual matching-player qualification. No broad full-kit Done tick.
