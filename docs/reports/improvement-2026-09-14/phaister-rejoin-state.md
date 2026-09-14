# Phaister rejoin state and current ultimate weather

Implementation in progress after the measured construction checkpoint 06b54470.
Current validation state lives in ACTIVE_REWORK_LEDGER.md.

## Reproduced behavior

The corrected three-player test selects Phaister in the room and initializes that
accepted pick in its preloaded arena before the timed cast. It validates actual
character index, kit and room selection together. The returning observer performs
no kit or scene-state initialization of its own.

In net-phaister-rejoin-v3, host and owner kept the actual ritual. The returning
observer had the correct Phaister character but no ritual, active clock or sky
through 20 samples inside the host's still-active interval. This is distinct from
the earlier fixture's kit-only substitution and its resulting character change.

## State restoration

Follow the existing reliable FamiliarEffect pattern. HostSyncPeer sends character
picks/rebinding and the world snapshot before active effect state, only to the
synchronizing peer. Existing peers keep their own running casts and prediction.

CovenEffect carries slot, round, fixed world centre, server contact time and
server end time. A receiver validates the sender, round, hero and finite values,
then subtracts transport time. It restores remaining preparation or active time,
reconstructs the full ritual at its existing visual age, and retains normal expiry.
An already running ritual ignores a duplicate restoration. No resources are spent,
no historical activation/hit is replayed, and only the host resolves curses.
Restored preparation retains and releases the existing movement-root contract.

SkyEffect carries the round, current winning look, age, lifetime and server send
time. It restores that look at its present age without announcing a new ultimate.
This is the actual current weather, which can belong to a hero other than Phaister.
Original scene lighting is captured once and restored through the existing cleanup.

The two messages contain five fields each. The wire audit reports 68 named
messages and zero type/count mismatches. Their use of server times still requires
real delayed/rejoining-player validation; local reconstruction tests alone do not
prove network delivery or a complete warning on every peer.

## Compatibility and checks

Protocol 31 adds these required live-effect tells. Protocol 30 players must be
refused instead of joining a room where they cannot reconstruct them. This pass
qualifies internal Windows players; the existing Desktop copy and older Android
builds are not updated. All participants need matching builds when this is delivered.

Local PlayMode checks pass 2/2, receipt b2146266f552: active/preparing restoration
preserves charge, expiry and root release without client-resolved hits; sky
restoration resumes the latest of two looks at its existing age and then expires.
Protocol pin and queue separation pass 2/2, receipt 4441748062df. Actual old-player
refusal also passes: the new host advertises 31, the retained Dante review player
advertises 30 and receives the explicit version-mismatch reason. No older binary
was overwritten for this check.

The first new-protocol rejoin reconstructed one ritual with 22 active samples
and correct expiry, but the initial evaluator failed. Its first world-state sample
preceded the separate reliable effect/sky messages by 86.6 ms. Subsequent samples
contain the circle, active clock and sky continuously. The evaluator now measures
that initial reconstruction explicitly, requires completion within 250 ms and
forbids any later disappearance before authoritative expiry. The pre-fix trace,
which never reconstructs the field, still fails this contract.

The initial cast-duration check also used the first sampled warning as though it
were the instant of acceptance. A long first frame makes those different. The
diagnostic initially tried HeroAbilitySystem.SecondsSinceAnswer, but the host's
remote cast does not populate that owner-UI response field. It now timestamps the
existing UltimateStarted event on every peer and requires at least 1.50 seconds
from that cast start to active phase. First-sample warning and
curse times remain separate reported measurements; this does not establish a
full 1.55-second visible warning under every frame/transport condition. The updated
real rejoin run is pending. The v8 run already restored the returning observer
within 73.5 ms, maintained the active field and sky, and expired correctly; its
remaining failure was the host's absent UI-response age. Production restoration
is unchanged between v7, v8 and v9. The diagnostic event subscription is balanced
on enable/disable and restoration itself does not announce another cast.

## Verified current result

`net-phaister-rejoin-v6` PASSES with actual room/model/kit agreement and 150 ms
delay each direction on the owning client's link. Host has 511 samples, owner
503 and returning observer 139. The returning observer reconstructs in 135.94 ms,
has 25 active samples, remains consistent during 22 samples inside the host's
active window, and expires its circle, clock and sky without leaks.
Rechecking the recorded lifetime against nearest host samples passes the250ms
bound; maximum observed remaining-clock difference is126.17ms across25matched
active samples. No extra player run was needed for this additional recorded-data check.

The accepted cast-to-active measurements are 1.618656 seconds on host and
1.570118 seconds on owner. First sampled warning-to-active is shorter and remains
reported separately. The snapshot repair does not certify a full visible warning
duration, human network feel or unrelated skill restoration.

Current internal v9 build: 1057 MB/62 seconds, receipt d95ddb75293b.
Runtime SHA256:
`aa79cdc4fb45da2dbb70b15d23fc93a112c506f4c4b3bdeee6830944ca1dd231`.
All owned test players, proxies and Editor processes have exited. Existing named
profiles were restored; the Desktop build is unchanged. Remaining Hex/Slow Brand,
Blink/Long Stride and other persistent hazard restoration are separate work.
