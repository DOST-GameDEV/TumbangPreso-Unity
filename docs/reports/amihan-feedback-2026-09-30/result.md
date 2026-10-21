# Airburst status and airborne payload

Current Wiki explicitly requires Whirled and airborne players/slippers after the
2.5second delay. Native baseline reproduces both omissions: no Whirled and zero
slipper lift. The first cold launch stalled before tests; one warm retry produced
those two real failures. No OOM, and the stall is not a test result.

The host now applies Whirled before processing caught slippers. Loose and flying
slippers use existing environmental HostThrow with no thrower credit, preserving
existing collision, bounds and landing instead of a second horizontal-slide loop.
Body lift is7m/s, the existing safety cap; horizontal carry15m/s and16m budget stay.
The ultimate's displayed name is AIRBURST, retaining its stable ID and authored
cast/fan. No new skill sound, model, cutscene or finalized-hero change.

Initial corrected native3/3passed3.76seconds: inside/outside/caster status,
duplicate release,2.5s windup gate, held shoe dropped into flight, loose shoe lift,
actual body lift over0.6m and forward travel over5m within0.7s, and eventual loose
slipper landing. Final observer-inclusive suite4/4passes4.00seconds; an observing provider cannot
apply status, carry or slipper flight. Eight focused Core contracts pass.
Full-map near-edge composition and actual player validation remain pending;
the Feedback row stays unfinished until that follow-through.
Protocol102 prevents older clients joining changed gameplay. No actual-peer,
physical-device, current-player or human approval claim. The rest of Amihan's
Wiki reconciliation, including Second Wind and Drift, remains open.

Concurrent Frostbite, queue-advert and Fetch-query units through137bde05 were
integrated without discarding their behavior. Frostbite had already used101, so
the combined contract is protocol102. The merged candidate passes8/8native
Airburst/Frostbite cases11.33seconds. Previous unit evidence stays pre-merge.

Authored Eskinita follow-through1/1passes9.17seconds. The actual defender is
airborne with the Whirled mark, then travels9m to z7, its real confinement edge.
Both1920x1080witness frames inspected: body, can and chalk remain readable,
existing green fan retires. Five distinct Airburst cases total; no count inflation
from rerunning the four contracts during integration. This qualifies the scoped
Airburst feedback in native play; current-player and actual-peer checks remain
separate, as does broader Amihan reconciliation.
