# Owner power readiness follows the actual second action

The live owner deck used the original cooldown to color/mute a power icon even
when the active ability offered a reactivation. Zack could see Again while its
icon remained unavailable; after spending the follow-up, an objective discount
could light the icon while recovery still rejected a third cut.

The owner deck now uses ReactivateReady for active reactivatable powers, matching
the existing HeroKit input route. Other ready checks, Zapped/practice gates,
ultimate rules, layout, text and all gameplay timing remain unchanged.

Actual public TumpPowerReadout.Build/Tick with the real Zack ability reproduced
two failures (available/spent-but-cooled) and passed two controls (expired/Zapped)
at06:06:20UTC,0.3393124s. Candidate four/four passed06:09:01UTC,0.3298807s,
zero skips, terminal exit0/no guard. Frozen maps differ only in the owner-deck
source. All candidate inputs and restored Editor/Quality settings matched.
Peak tree2,572,025,856bytes and cgroup7,355,588,608bytes.

Native reduced-project headless UI-state scope. This verifies the actual icon
Muted state, not a rendered screenshot, physical input, packaged or peer run.
Legacy unused deck rendering is not refactored by this focused correction.
