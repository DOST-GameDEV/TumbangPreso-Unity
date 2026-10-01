# Snapshot request framing before refresh budget

The connected-host ReqSnapshot handler ignored its payload. Empty, unknown-marker
and trailing-byte requests could trigger the full world refresh and consume the
peer's half-second throttle. It now requires exactly the existing one-byte zero
marker after the already-consumed NGO name hash, before reserving a reply ticket.
No format, rate, authority or protocol117 change.

Native D3D11 baseline3/3fails on an actual listening host at the throttle mutation.
Final4/4passes: all three malformed cases leave both timestamp and pending-reply
state untouched, then accept a valid enveloped request immediately. The existing
connected-host coalescing case still enforces the half-second floor, sends its
deferred reply and cancels pending work when disabled.

Source64d1c97b0 plus two owned overlays;671frozen inputs have no drift and both
files match the native candidate. Jobs20952/25940terminal; guarded settings and
profiles preserved. No fixture repair or unchanged suite rerun.
[XML and input receipts](snapshot-framing-checks).

This proves actual socket-host lifecycle and injected receiver framing locally.
It is not a new two-process cold rejoin or lossy-link qualification. Existing
admission/Haunt peer evidence retains its exact earlier source boundary. No
loading, external-service query, hero, sound, animation or asset changes.
