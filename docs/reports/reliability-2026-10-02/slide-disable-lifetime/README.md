# Disable retires an old retrieval-slide contact window

CombatVerbs stopped updating while its body was disabled, retaining an active
slide retrieval window and its starting position. Reactivating that body resumed
the old window. OnDisable now clears the slide window alongside the already
retired lunge window. The spent cooldown, commitment, impulse and ordinary
clock-hold behavior are unchanged.

The focused fixture calls actual HostResolveSlide at a legal owned loose slipper,
then actual GameObject.SetActive(false/true). It reads public SlideActive and
SlideCooldownLeft; it never assigns a timer. Motor and slipper simulation are
disabled to isolate this component lifetime boundary from travel and physics.
No map, raw input, actual pickup, practice operator or network claim is made.

Original64655 ran exactly3 cases: the old retrieval window remained active after
body retirement (intended causal failure); clock-hold preservation and idle-body
retirement controls passed. Candidate10107 passed exactly3/3. Both runs use identical
fixture/meta, GPU1536MB plus2048MB reserve,450-second ceiling, PlayMode/nographics
on Unity6000.5.8f1. Candidate launch followed direct preparation exit0. Original
and candidate guards are terminal, preferences restored and leases released.
Zero fixture, tooling or native repairs.

Post72297 terminal0: exact3 MAIN/qualification hashes match, and all18868 other
files under qualification Assets/Packages/ProjectSettings are unchanged. Original,
candidate and post owned manifests, raw XML and guard receipts accompany this
report. Full logs, protected manifest and original source remain in local
Logs/slide-disable-lifetime1002. Previous failed practice-fixture evidence remains
unchanged. Frozen Windows1002i predates this change.
