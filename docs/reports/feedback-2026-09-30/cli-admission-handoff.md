# CLI admission handoff correction

The supported command-line client called SceneFlow.Go immediately after
StartClientAsync returned true. That result means transport startup, not an
approved connection or assigned seat. The first real114pair failed before
round1: clientClosedByRemote, offline fallback and no logged peer1approval.
The original failed result is preserved in current-player-checks.

The CLI client now awaits existing NetSession.WaitForConnectionAsync, which
requires connection plus applied seat and already owns cancellation/refusal/
timeout handling. Trusted seating/match-start messages retain arena ownership;
the boot route no longer starts a competing pre-admission arena transition.
Explicit admission success/failure is logged. No SceneFlow, loading/prewarm,
asset, approval policy, established8second silence budget, packet or protocol
change. New feature framework or alternate transport was not introduced.

## Checked candidate

Committed base80de6e7fa plus one explicit owned NetBootstrap overlay, including
the merged title-submit fix. Isolated native Git baseaecc0ee2/imported assets;
not a pristine release certification. Windows6000.5.8f1 builder succeeds,
2141MB/188s, to a new internal feedback-admission-player-1001 output.15422inputs
show no before/after drift, and the owned source matches the built candidate.
Runtime SHA256:d561c1f18851597d192f6e3a68f8bc35450c841b788e01a5a14966c59b67fd57.
Old Desktop/current-first/103/98outputs remain untouched.

## Actual peer result

The exact same binary runs D3D11 localhost UDP host/client processes with
separate guarded profiles and batchmode external-service suppression.
Host logs peer1approved and assigned seat1. Client logs connected, seat2display
(local slot1), then join admission: seat assigned. The existing direct evaluator
passes with no faults. Both reports reach active round2, defender1, protocol114,
HeroStrike/Eskinita and structural hash282AB88E. Movement/scoring progress.

Client samples150s; host163s. Client exits first and the host then hands that
seat to a bot. Their later scores/travel are different sampling instants, not
same-time equality measurements. The guard stops only its own players, preserves
existing profiles and confirms shared input settings unchanged. All jobs terminal.
Exact build/input/peer/preservation evidence is retained in admission-player-checks.

The failed-to-passing controlled scenario supports the admission handoff fix.
Asset warmup is also shorter on the second run; no claim that one global timeout
was conclusively responsible or that all cold-loading stalls are solved. Preserve
the original failure and established deadline policy. No repeated unchanged pair.
No cross-platform, lossy WAN/Relay, actual all-ability/rejoin, physical-device,
audio-taste or full release qualification is claimed. Those remain separate.
