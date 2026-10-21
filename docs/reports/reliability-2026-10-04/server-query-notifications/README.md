# Notify room subscribers when map or visibility changes

ServerQuery refreshed Map/Visibility but omitted both from its change signature.
Subscribers therefore missed a map-only or visibility-only update. Include both
in the existing signature; query rate, service requests and gameplay are unchanged.

Actual native repaired baseline: two causal failures, two ordinary controls pass.
The same four-case fixture passes after the one-line fix. The earlier fixture
failed three SetUp checks because its counter persisted across NUnit cases;
that run is retained but not counted as product evidence. Only resetting the
per-test counter repaired setup; assertions remain unchanged.

Both accepted runs are terminal with input/profile restoration. Exact Runtime,
fixture/meta hashes were stable; frozen19230 maps had202 GUI importer metadata
rewrites. Those are restored to exact before bytes with raw generated copies
and full input maps retained locally. Strict all-input preservation is false.
This checks actual subscriber notifications, not live-service or rendered browser
acceptance. No UGS requests or account data were used.
