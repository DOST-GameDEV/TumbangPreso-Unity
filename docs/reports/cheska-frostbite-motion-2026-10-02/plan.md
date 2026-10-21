# Cheska Frostbite: prepare the carried shoe

Continue the approved cloud-only hero presentation assignment after the shipped
loaded-shoe cue dc6f5d71. The current attacking Frostbite and aimed Cold Feet
both request hero-cheska-frostwave / frost-sweep. The imported 0.7-second body
was authored for a ground-directed sheet, while Frostbite affects the held shoe.
Reference: the October 2 Cheska footage review and all-hero implementation plan.

Give Frostbite its own short, controlled preparation. The carrying hand presents
the actual slipper just outside the torso. The free hand makes a compact pass
along the shoe, holds, and returns. Upright body, planted feet, small head glance;
no crouch, copied fire ignition, new object, mist cloud or camera takeover.
Keep Cold Feet's existing sweep and every other imported clip byte-for-byte.
The new motion follows accepted activation; no delayed gameplay, new input,
load-clock change, cooldown change, transport change or SFX replacement.

Owned paths beneath Assets/TumbangPreso:
- Art/characters/persons/team-cheska.glb: append only hero-cheska-frostbite
- Resources/Roster/person_cheska.asset: reference only the added imported clip
- Runtime/Abilities/CheskaHeroKit.cs: Frostbite body/FPP action names only
- Runtime/Visual/CharacterAnimator.cs: one new explicit action alias
- Runtime/Camera/ViewmodelArms.cs and ViewmodelArms.CastGesture.cs: new frost-load keys only
- Tests/PlayMode/CheskaFrostbiteMotionTests.cs and metadata
- Editor/CheskaFrostbiteMotionAuthor.cs and metadata, if required for the targeted roster reference
- tools/author_cheska_frostbite.py, test partition and owning documents
Do not change Cryo rules, body geometry, materials, names/descriptions or others'
reserved reliability, Amihan, Sean, Rafi, Paete and Phaister work.

Acceptance: preserve original GLB binary prefix and all old animation entries;
inspect front/side pose previews, then real native body and owner-held-shoe
footage. Confirm the attacking action differs from Cold Feet, comes from the
serialized authored clip, keeps the same held shoe and retires normally. Run
one focused case per fresh process with the existing cloud memory guard.
A pose preview, compilation or invocation is not runtime or visual acceptance.
No broad rerun, full-player build or actual-peer claim is required by this
cosmetic-only unit. Preserve failed attempts and permit only one bounded repair
per execution question. Inspect each result before another product iteration.
