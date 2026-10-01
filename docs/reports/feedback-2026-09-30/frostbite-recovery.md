# Frostbite timed recovery

Cheska lacked the timed-kit binding, so the host's snapshot producer skipped
her live Frostbite load and the receiver rejected an otherwise valid scoped
state. Resource snapshots recover cooldowns/charges, not this accepted effect.
The native receiver baseline reproduces rejection of a seven-second load aged
to five seconds on a returning Cheska.

Cheska now implements the existing timed interface for cheska_skill2. Capture
includes its current load clock or authoritative empty state. Recovery restores
the aged load without Activate, equipment writes or resource spending/refunds.
Equipment may hydrate later. A settle-once guard prevents expired/consumed loads
being revived; invalid values/role do not poison valid recovery, and round/match
reset clears the gate. Original activation, ten-second expiry and throw rules
stay unchanged. Protocol122 requires matching builds; wire layout stays bounded
and unchanged, with existing match/round/epoch/sequence/identity gates.

Windows Unity6000.5.8f1 baseline1/1fails at actual receiver binding. Final4/4passes:
actual receiver aging/resources/duplicate/no-op behavior, invalid/role bounds,
expiry/empty/new-round lifecycle, and real restored Carrier release/Slipper flight
freezing the defender. All716frozen inputs remain unchanged.26621/16853terminal,
profiles/shared input preferences preserved. No native tooling repair; an initial
patch-context typo was corrected before applying or running the candidate.
[Raw XML and input manifests](frostbite-recovery-checks).

This qualifies local scoped recovery and the restored real-hit path. Actual122
players/peers remain unqualified; the passing121crash/rejoin predates this binding.
No authored VFX/SFX/animation/model/map, loading or protected-hero changes.
