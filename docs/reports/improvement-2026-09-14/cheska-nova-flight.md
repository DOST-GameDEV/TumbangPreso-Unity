# Cheska Nova: grounded slippers must enter flight

The earlier Cheska ice art, wall collision, traction, restrained body/FPP casts
and early-break freeze visuals are already implemented and locally verified.
See reports/improvement-2026-09-10/cheska-kit.md. This correction preserves them.
Its historical screenshots predate current maps/hands and do not justify another
wholesale redesign.

The current Nova calls Slipper.Deflect on nearby equipment. That method changes
an existing flight's velocity and thrower credit, but does not change a Loose
slipper's state. The new actual-input baseline confirms the consequence: Nova
is accepted, the nearby loose slipper never enters flight and moves zero metres.
Held equipment and an outside loose slipper remain in place.

Baseline: Logs/cheska-nova-slipper-baseline-v2.xml, one failed test; profile
c00f86bc88a9. CSV before: accepted=True, launched=False, outward=0,
held_same=True, outside_move=0. The earlier first attempt failed to compile due
to a missing System import in the new fixture and is not gameplay evidence.

The proposed correction uses the normal HostThrow transition for Loose objects,
retains Deflect for InFlight objects and leaves held/returning equipment alone.
No thrower receives score credit for the blast. Existing effective horizontal
speed19 and lift multiplier1.1 remain; an exactly centred loose slipper uses the
caster's planar forward direction instead of an undefined zero vector.

The corrected local contract and fresh three-skill owner/body recording pass2/2,
Logs/cheska-nova-flight-v1.xml, receipt63cbbe13f33c. Actual result: accepted=True,
launched=True, outward10.26metres, held_same=True, outside_move0. The corrected
flight uses the same state-transition path already qualified by the earlier Dante
network kick, and remains inside the existing host-resolution guard.

Fresh current owner sequences were inspected for all three actions. The sheet,
fractured wall and Nova remain distinct; the retained hand/prop poses preserve
the centre view. Old screenshots' bare oversized prop appearance is not the
current view. Recorded-time owner/body videos are encoded under
Logs/cheska-current-kit-v1. This is no new human play-feel or dedicated Cheska
network qualification claim. The separate internal build succeeded at1057MB/50s,
profile receipt ad0a4e72b9e3, Logs/cheska-flight-build-v1.log. Its path is
Builds/CheskaSkillReview/TumbangPreso.exe; the Desktop copy is unchanged.
