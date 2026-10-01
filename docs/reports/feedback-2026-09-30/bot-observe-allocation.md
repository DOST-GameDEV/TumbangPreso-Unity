# Allocation-free warmed bot observation

Observe runs every active bot render update. Iterating cached RoundDirector.Bodies
through IReadOnlyList allocated a boxed list enumerator on every call. Indexed
access now reads the same cached list without that enumerator. Reaction/lapse
blending, actual body identity, self position and fresh body behavior are unchanged.

Native D3D11 baseline1/1fails:100allocation events across100warmed real Observe
calls. Final7/7passes with zero events, both tier lag and delayed-ranking controls,
replacement identity, and player/companion self-position checks. The recorder is
calibrated with a deliberate4096-byte allocation; a bound delegate avoids test
reflection allocations inside the measurement.

Sourcecaa2e78be plus two owned overlays;666frozen inputs have no drift and both
files match the tested candidate. Jobs21662/69409terminal; guarded input preferences
and profiles preserved. No fixture repair, extra suite or new player build.
[XML and manifests](bot-observe-checks).

The claim is warmed observation allocation, not a measured FPS improvement or
zero allocations during first observation/list rebuild. Haunted sensor coverage
remains separate. No kit, tier, loading, authored asset or protocol115 change.
