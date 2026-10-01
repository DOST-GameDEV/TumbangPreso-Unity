# Ordinary bot slipper-query allocations

F0930-35, source80622a013 plus four ordinary query substitutions, lifecycle
invalidation, BotSlipperInventory and its focused native fixture.

## Change

RivalShotIsInbound, SlipperOwnedBy, MySlipper and TryInterceptPoint used a fresh
FindObjectsByType result on each call. They now share a retained native snapshot,
invalidated when a Slipper awakens or is destroyed. The snapshot includes inactive
objects; a value-type iterator filters their current activity without allocating.
Disabled Slipper components remain queryable, matching the original scan.

Ownership and flight state are read from each live component. No owner selection
or trajectory is cached. Keeping the native query avoids inventing an ordering
from EntityId. Four ordinary loops changed; hero-specific planning, mechanics,
loading, visuals and the gameplay protocol remain unchanged.

## Evidence

Unity6000.5.8f1 Windows D3D11 PlayMode, isolated profile
feedback-0930-bot-slipper. The Unity GC.Alloc recorder is calibrated with a
deliberate4096-byte allocation before measuring100warmed real MySlipper calls.

- Original lookup:200 allocation events across100 calls.
- Retained lookup:zero events across the same100 calls.
- Native selection across multiple owners/duplicates passes.
- Disabled-component/inactive-object and live owner-change behavior passes.
- Destruction, replacement and duplicate removal pass.
- All four ordinary queries see current flight/ownership. Own shots are excluded
  from rivalry; changing ownership immediately admits the same incoming flight.

Five distinct final checks pass: four in bot-slipper-final.xml and the corrected
flight case in bot-slipper-flight.xml. The latter fixes its test launch to.35m
above ground: its earlier ground-level shot genuinely missed the can. The other
qualified cases were not repeated. Runtime code is identical between those runs.
543 frozen overlay inputs; no non-metadata drift after the last run.

Earlier evidence is preserved: one compilation repair replaces the obsolete
GetInstanceID test call. A proposed ascending EntityId order assertion then fails,
so the implementation retains the engine's query order and tests actual native
selection instead. That false ordering assumption is not a gameplay defect.

Raw XML/input manifests are in checks/bot-slipper; full logs remain in the isolated
checkout's Logs/feedback-0930. This proves warmed query allocation and focused
native behavior. Player FPS, whole-match performance and real-peer bot behavior
are not measured by this fixture. Structural changes still allocate a fresh scan.
