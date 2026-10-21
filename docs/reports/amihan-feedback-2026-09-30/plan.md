# Amihan current feedback reconciliation

Owner reopened Amihan and remaining ability work. Current Wiki, read directly:
Second Wind grants25percent movement speed for2.5seconds on a cast. Drift has35s
cooldown, Featherfall40s/5s, Whirlwind35s/2.5s. Airburst costs15points and, after
2.5seconds, applies Whirled and a strong airborne push to players and slippers in
its map-wide forward fan. Preserve stable ability IDs and current authored art.

Source65b9c8e2 still calls signature QUICK DASH at40s and ultimate STORM SURGE.
Airburst's release applies carry without Whirled. Loose slippers slide horizontally;
held slippers are never released by this effect. The body lift3.5m/s gives only
about0.31m of ideal ballistic height under20m/s² gravity. Existing host-only carry,
accepted fan pose, windup, immunity, playable bounds and scoring must survive.

First coherent unit: reproduce Airburst's missing status and airborne payload,
then correct through existing authority routes. Reuse authored fan/cast, no new
SFX, cutscene or character redesign. Preserve finalized Paete/Phaister.

Initial ownership: Tests/PlayMode/AmihanAirburstTests.cs/meta, its match suite
placement and this report. Runtime paths are not claimed until baseline narrows
what must change. Stop baseline on fresh expected failure. Final acceptance must
cover delay, inside/outside/caster, status/drop, real body lift and travel, slipper
lift/landing, duplicate release and client refusal. Record actual-peer limits
separately. One heavy native job, one bounded tooling repair if required.

Second Wind and other name/cooldown changes remain open in F0930-11; this first
unit must not mark the whole reconciliation Done.

## Second Wind and Drift follow-through

The next unit implements the complete stated passive rather than inferring it
from cooldowns, which are affected by overhead-map cooldown rates. One simulation
clock grants1.25movement scale for2.5seconds, refreshing rather than stacking.
Offline successful casts refresh it; networked casts refresh only from existing
accepted cast events, so a denied prediction cannot grant free speed. All four
Amihan abilities and reactivation share the kit-owned clock. Round reset clears it.
The existing scoped Featherfall recovery envelope can carry this Amihan timer as
an appended bounded float, aged only by round-clock progress, including defenders
whose flight phase is empty. Preserve generation/request/event/epoch guards.

Drift display/cooldown changes to the current35second Wiki value; stable IDs and
authored motion remain. Claim AmihanHeroKit.cs, AmihanRules.cs,
Net/MatchRpc.Featherfall.cs, NetSession.cs compatibility, new
Tests/PlayMode/AmihanWikiTests.cs/meta and relevant existing Featherfall packet
fixtures only if their wire shape needs updating. No other hero edits.
Baseline first: current name/cooldown and absent accepted-cast movement boost.
Final: refresh/expiry/reset, refused prediction, ordinary offline cast, recovery
age/rejection and existing flight compatibility. Actual peers remain separate.
