# Roster and specification coverage

Snapshot: October1, source84f07fcb, protocol112. This is design coverage, not a
claim that all entries are implemented, tested or approved by a human. Wiki read
00:32UTC. Re-read before publishing descriptions because collaborators edit it.

| Hero | Passive | Signature | Attacker | Defender | Ultimate | Required treatment |
|---|---|---|---|---|---|---|
| Amihan | Second Wind | Drift | Featherfall | Whirlwind | Airburst | Preserve human kit; reconcile latest1.5s Wiki delay against qualified2.5s code |
| Cheska | Chilling Touch | Cold Feet | Frostbite | Glacial Wall | Absolute Zero | Preserve human kit; clarify charged slipper expiry rather than silently invent a Wiki duration |
| Dante | Earthbound | Unstoppable | Boulder | Bastion | Continental Drift | Mechanics shipped in scoped checks; bespoke body/presentation and retained descriptions still need reconciliation |
| Nemu | Kuro | Kuro: Sit! | Kuro: Fetch! | Kuro: Catch! | Haunt | Human kit; preserve concurrent implementation claim; legacy seance is not Haunt |
| Paete | No separate implemented passive | LIANA LEAP | BAKYA BLOOM | THORN HARVEST | MAKILING'S EMBRACE | Protected finalized source. Reference/bug checks only, no polish redesign |
| Phaister | Voodoo | Teleport | Curse Drain | Curse Hex | Voodoo Doll | Protected finalized source except owner-authorized Hex hallucination refinement; see phaister-hex.md |
| Rafi / Hydro | Blank | Blank | Blank | Water wall anchor | Baha anchor | New proposals permitted. Rafi is current code identity, not permission to invent biography |
| Sean / Pyro | Blank | Blank | Empowered throw anchor | Blank | Blank | New proposals permitted. Existing fire implementation is migration input, not proof of adopted Wiki design |
| Zack / Electro | Amped-Up | ??? | Normal/Overclocked blank | Normal/Overclocked blank | Overclock | Preserve human passive/ultimate premise; author missing slots and explicit permanent-state semantics |

## Existing code that must not be mistaken for new specification

- Rafi has Crosscurrent (two uses), Mirrorwake (two uses), an empty defender slot,
  and Breakwater16points. Water-field ownership/replay helpers are valuable;
  old abilities are not automatically approved answers to blank cells.
- Sean has Flame Rush50s/0.6s, Ignition Cannon two uses with10s charge lifetime,
  an empty defender slot and Supernova15points. Old charge replenishment and
  ignition behaviour must be explicitly retained or migrated, never half mixed
  with a cooldown design. Existing IDs, profiles and authored assets survive.
- Zack has Bolt Sprint46s/2.5s, Magnet one objective-replenished use, an empty
  defender slot and Thunderstrike20points with a7s empowered window. This is not
  the human's Amped-Up five-seconds-per-objective or permanent Overclock15points.
- Nemu has shipped Sit/Fetch/Catch and the new Haunted timer/wire/HUD gates.
  The old Devouring Seance remains10points; chase/nearsight/muffle are still open.
  Another contributor owns that implementation; these plans do not claim paths.

## Rules for proposals

All numbers introduced in the new-kit proposal are initial tuning targets, not
measurements or pre-existing owner decisions. Keep the owner's named anchors.
New Wiki prose must say Proposed design until implemented, with the requested
attribution on each authored cell. Do not overwrite human text to conceal a
conflict. Finalization requires functional tests, recorded play and taste review.

All nine heroes receive a plan. Paete receives a preservation/verification plan only; Phaister adds the specific
owner-authorized Hex hallucination refinement. Classic's cosmetic cast remains powerless. Roster counts
in older vision prose are historical, not a reason to delete current hero classes.
