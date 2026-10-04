# Largest live frame sample carries local observed context

The historical270.33ms host sample remains unattributed; its aggregate report has
no timestamp or round/context tied to that maximum. This change improves the
next local diagnostic without claiming a performance fix or rewriting old evidence.

MatchStatsCollector records observed frame/time/scene/round/time scale/focus/pause/
loading and totalGen0 collections only when an accepted sample sets a new maximum.
NetStateReport appends that local context. No per-frame string allocation, wire,
MatchRecord, career or telemetry payload change. Existing round-active and telemetry
optout counting gates remain intact; round1 clears context alongside histogram.
These values describe state when the sample was observed, not the cause of a stall;
gc0_total is a process-total count, not proof of a collection in that frame.

Native PlayMode2/2 FIRST run69846 validates real accepted unscaled-time sample with
observed scene/scale, optout preventing additional sample/context, and actual new-
match boundary clearing previous context. One SOURCE-ONLY reviewer caught foreign
previousStats sampling the temporary world; before first native run the fixture
saved/disabled it before service swaps and restored after references. No native/
fixture retry, weak assertions, map setup or fabricated performance sample.

Unity6000.5.8f1/D3D11/960x540, GPU2048+2048reserve/450s/profileframe-context1002;
preparation72638 terminal0 then reviewed4-input copy terminal0 before launch.
Exact4 owned inputs match MAIN/q; all12532 protected hashes unchanged. Guard
terminal/restored/no lease. No native benchmark, new270ms repro/causal attribution,
physical device or standalone inclusion claim. Current1002h predates this diagnostic.
