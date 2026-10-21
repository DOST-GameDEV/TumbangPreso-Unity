# Refuse damaged chat packets before publication or rate limiting

Host Chat and client ChatLine callbacks previously decoded unchecked strings.
Original44592 reproduced two buffer-overflow exceptions and exposed one fixture
counter leak. One fixture repair resets event/name/line observations in SetUp,
without changing assertions or production. Corrected original58898: two causal
failures/seven controls, including four existing result-receiver cases.

Use the existing bounded UTF-16 string reader through ValidStringFrame(count1/2).
Validate complete payload and reject trailing data before string allocation,
host rate allowance or client event publication. Host line limits/newline removal,
Unicode fields, sender and loopback guards remain; no protocol/schema change.

First candidate52815 retained7/9: the root patch mistakenly placed the two-string
guard in the result callback, rejecting valid results and leaving ChatLine open.
This source was never published. Exact callback mapping was corrected; same
assertions and fixture. Corrected candidate49518 passes9/9 with both chat fields
and all four result cases. No further repair/retry. Profiles/input preserved and
leases released. Post65168: all18441 protected qualification assets
unchanged, exact three owned inputs match MAIN. Raw failures retained.

Native direct-handler evidence, not crafted live-peer fault injection or whole
competition readiness. Windows134 artifact1003b predates this chat unit.
