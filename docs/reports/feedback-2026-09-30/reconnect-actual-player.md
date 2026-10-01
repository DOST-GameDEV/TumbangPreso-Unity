# Actual crash and cold relaunch: evidence and limits

Two actual Windows117 attempts use separate guarded profiles. The first client
enters the live arena, is forcibly terminated, and the host confirms disconnect
and bot takeover. Its stopped profile keeps the same identity but changes local
character preference from2to0. A new process then rejoins as transport peer2,
reclaiming seat1. No live profile is edited and no external sign-in service runs.

## Original candidate: failed character expectation

Source72a1187b4, build2141MB/117s,15426inputs unchanged. Runtime SHA:
a27665cb0a52c8f9999429698fa048a645c9fb5f72e57064f4d679cca0d91721.
Both processes remain117/networked/active in Eskinita/HeroStrike, but both show
character3rather than selected2. The strict verifier fails; this result is kept.
Source investigation plus native baseline proves pre-round bot placeholder
handover failed to apply the arriving choice. [Fix](pre-round-pick-handover.md).

## Changed candidate: reconnect choices restored, aggregate gate still fails

Source5d2758f95, build2141MB/98s,15426inputs unchanged. Runtime SHA:
d3830cacd058bd683de12a82fa76eb2942c75d9c7d3035a4e74f2f4c77d1c414.
Same driver and character expectation, fresh profiles and output. Host logs
peer1departure/takeover, then peer2seat1arrival. The returner is CLIENT/local1,
not a spectator, with its live input body and character2. Both state reports show
character2and structuralE2DF775D; mode/map/protocol match. Identity is unchanged.

The client samples45s while round1is active. The host samples110s, after the
returner has naturally exited and the seat has again passed to a bot. Its report
is round1/inactive, with90.002s of measured live frame time, at the round boundary.
The aggregate strict active-host criterion therefore fails. No overall PASS is
claimed and no third unchanged launch is run. These asynchronous reports cannot
establish simultaneous score/defender/active equality or later round progression.

## What this establishes

Actual abrupt departure, host takeover, same-identity cold seat reclaim and
retained chosen character despite differing local preference are observed on
the changed binary. Both strict verdicts remain failed for their recorded reasons.
Complete round-transition/rejoin-under-loss and all ability recovery remain open.
This is not WAN/Relay, Android, hardware input, audio judgement or release proof.

Both attempts preserve shared input preferences and prior profile bytes. All
task players and helpers are terminal. Original Desktop/older players remain.
Build/hash/verdict/report/log-extract receipts and the unchanged driver are in
[reconnect-player-checks](reconnect-player-checks). Private profile backups and
full logs stay in the isolated checkout; no profile/token data is committed.
