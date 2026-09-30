# Second Wind and Drift

Current Wiki requires25percent faster movement for2.5seconds on each ability cast,
and Drift at35seconds cooldown. Baseline2/2fails absent boost and stale QUICK DASH.

Amihan now owns one simulation-clock passive, refreshed without stacking. Offline
successful kit casts grant it. Network prediction alone does not: the kit opts
into matching accepted owner events through a generic default-off property.
The owner receives that event without replaying its paid payload. Older/duplicate
events do not refresh. Shared ultimate accepted reserved activation starts it too.
Round reset clears it, and the existing movement evaluator reads its1.25scale.

Protocol103 appends the bounded remaining clock to the existing scoped Amihan
recovery tail, aged by round-clock progress and gated by existing match, round,
epoch, generation, request and event watermarks. Transport reset clears it.
Drift keeps its stable ID, with current name and35s cooldown. Amihan-only retained
loadout text also uses Drift/Featherfall and current values; locked variants remain
locked. No authored animation, other hero behavior or skill SFX changed.

Six distinct native cases pass: five14.20s cover names/cooldowns, all four accepted
abilities, refresh/expiry/reset, recovery bounds/order, actual offline flight cast
and existing scoped Featherfall packet gates. The owner accept/deny/dedup path
passes separately6.53s, requiring no extra cooldown spend or body teleport.
Twenty-two focused Core status/Amihan/loadout contracts pass. No new OOM or guard
stop. These are native local authority-path checks, not actual remote peers.

Full Amihan feedback remains open for shared Whirled can-reset eligibility review
and any remaining current-spec mismatches. No new player build was produced here.
