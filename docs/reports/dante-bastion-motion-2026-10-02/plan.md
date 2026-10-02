# Bastion: shoulder-led barrier brace

Bastion still requests Unstoppable's hero-dante-roar / carapace-guard. The
signature is a personal chest-opening flex; the defender skill places a following
field ahead. Give that second action its own planted forward brace and recovery.
Use the existing Dante footage review and presentation direction: shoulders lead,
weight stays grounded, and the caster keeps a usable centre view. No club, giant
floating rocks, lens-filling hands or new ground eruption.

Hand-author a short asymmetric draw and forward two-arm set, with a slight torso
turn followed by a controlled settle. One arm reaches a little earlier; avoid a
symmetric overhead roar. Rigid legs remain planted; no false knee crouch. Recover
to the normal defender stance while the existing barrier keeps following. The
body gesture communicates acceptance; it cannot delay the current activation or
create another live hit timestamp. The owner gesture opens toward the lower
sides of the lens and returns promptly.

Claim only these paths beneath Assets/TumbangPreso:
- Art/characters/persons/team-dante.glb: append hero-dante-bastion only
- Resources/Roster/person_dante.asset: append the serialized clip reference
- Runtime/Abilities/DanteHeroKit.cs: Bastion action names only
- Runtime/Visual/CharacterAnimator.cs: explicit new action alias
- Runtime/Camera/ViewmodelArms.cs and ViewmodelArms.CastGesture.cs: bastion-brace keys
- Editor/DanteBastionMotionAuthor.cs and metadata
- Tests/PlayMode/DanteBastionMotionTests.cs and metadata
Also tools/author_dante_bastion.py, the suite partition and owning documentation.
Preserve all37 prior animations, model binary prefix/tables, GUIDs, whole private
HeroHazards and other reservations, including paused Amihan and reliability work.

Before implementation capture real defender Skill2 input with the existing field.
Verify accepted cast,7.5second duration,35second cooldown, following transform and
round-reset cleanup, then expose the reused action in the baseline assertion.
After authoring and roster wiring run that same focused graphics case. Inspect
body and owner footage through formation, settle and recovery. Keep the field
visual/reflect geometry and all mechanics unchanged. One native case per fresh
process under the same memory guard; no personal PC. This is not player/peer,
full-map, low-quality performance, SFX or human approval evidence.
