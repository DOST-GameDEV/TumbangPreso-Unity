# Haunted rulebook: implementation in progress

The current Status Effects tab defines Haunted as7.5seconds of reduced perception:
can/slipper HUD markers hidden, nearsight and muffled audio. Status Immunity excludes
Haunted. The runtime and rulebook previously had no Haunted kind or rule.

The rulebook now appends Haunted12 after Hexed11, preserves all existing IDs,
records the7.5-second contract and immunity exception, and keeps normal movement,
interaction, retrieval and equipment unchanged. Cleanse eligibility is retained;
the immunity exception does not mean an explicit cleanse cannot remove it.

Focused engine-free Core contract1/1passes with exact policy/ID checks. This does
not establish runtime behavior. CharacterMotor timer/lifecycle, shared replication,
HUD-marker gating, nearsight/audio integration and Kuro's current Haunt chase still
need implementation and native/peer evidence. No Feedback row is marked Done.
No authored model, animation, VFX, SFX, map or loading path changed.

Next source locations: CharacterMotor.Status.cs StatusLeft/ClearStatuses/
StepStatuses; MatchRpc.cs WriteUnit/OnSyncUnitMsg; existing HUD marker owners;
NemuHeroKit's legacy NightmareSeanceVoidAbility. Preserve finalized Phaister/Paete
behavior and contributor reservations. Do not relabel the old seance as completed
Haunt or treat this data-only contract as the requested whole feature.
