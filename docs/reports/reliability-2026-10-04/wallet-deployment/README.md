# Wallet production alignment

Production v1 paid both supplied offline Practice records: 325 Tansan rather
than the expected 195 from the three online results. The exported old script
reproduces that failure using the existing endpoint fixture. Its catalog also
lacks Amihan/Paete and its declarations omit the Credits request parameter.

Published the existing canonical wallet script as production v2 on October 4
at 07:52:09 UTC. Fetched code matches ugs/cloud-code/wallet.js exactly after
line-ending normalization. Existing action/item/task parameters are preserved;
request is an optional String. Version1 remains available for rollback.
The actual exported v2 passes the wallet fixture, including current hero
purchases, offline reward exclusion, task settlement and Credits grant/retry/
receipt/overflow checks. Receipt and old-source reproduction are retained here.

The documented temporary PLAYTEST_TOPUP999999 is retained. Before a public
release it must be disabled and redeployed under the existing release directive;
existing balances must not be reset automatically. This is virtual game currency.
No live player-wallet calls, real-account reward, rendered shop or Victory-audio
acceptance are claimed. The local fixture runs the actual endpoint with a Cloud
Save stand-in. A fixture reload after the two added purchases respects the
existing top-up-on-load behavior before checking task payout deltas.
