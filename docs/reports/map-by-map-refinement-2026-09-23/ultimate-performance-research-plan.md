# Individual ultimate performance rework, owner2026-09-24

Latest owner assignment: a separate cloud animation lane now owns all animation
research and implementation, including independently authored ability casts and
freely directed ultimate performances. Its work may start now alongside ongoing
map/environment work. This supersedes the older wait-for-maps scheduling below
for that lane; it does not remove any older scope or mark the work completed.


Owner asks to add a new TODO to make ALL ultimates more expressive and cooler,
with thorough research/analysis of VFX and animation in Roblox and other games
praised for their ultimate effects. Example direction: Phaister starts laughing
and flies/lifts off when casting. Owner then specifies RESEARCH FIRST, THEN PLAN,
THEN IMPLEMENT, with a separate theme and stage that feels like each character's
own for a moment. Do not treat existing choreography or old checks as completion.

## Confirmed existing foundation

SharedUltimatePhase.Duration is2.8seconds. Host accepts a cohort, PresentationClock
holds simulation atTime.timeScale0, peers receive that same phase/end boundary,
UltimatePhaseView samples character-specific render copies/intro scenes, and
Complete releases the clock before ExecuteSharedUltimate starts the real ability.
Reduced-camera viewers retain the same shared timing. This is confirmed from
current source, not a new multiplayer test in this pass. Keep shared authority, simultaneous-cast handling, resource rules and live warnings.
The2.8second duration is only the CURRENT baseline, not a required duration or cap.
Inspect each current hero's body/FPP, voice and scene first; do not assume missing
laugh/lift/effects solely from a class name or duplicate good existing performance.

## Research gate before implementation

Study several relevant games, including selected Roblox experiences with strong
ultimate choreography and other readable action/fighting/hero games. Prefer actual
viewed gameplay, official trailers/developer breakdowns or original VFX artists'
work; distinguish viewed footage from text summaries. Record source links and exact
moments. Analyse anticipation, silhouette, pose/action arcs, spacing, hold/release,
impact, camera, temporary environment/stage transformation, shape language, colour/
value hierarchy, material motion, audio rhythm, recovery and return to live play.
Judge why it works and what fails at TUMP's small blocky3Dcast/court/view distances.
Roblox is a reference pool, not permission to copy another game's effects/characters.
Research claimed praise rather than asserting reputation without evidence.

Include multiple styles and compare restrained versus highly layered moments.
High density is welcome when layers have different jobs and complement one another;
reject identical rings/columns/bursts/noise on every hero. Preserve can, slipper,
player/role and real danger readability. Avoid strobing, whiteout, camera sickness
and cinematics that reveal private aim/unseen opponents. Keep effects affordable,
with reduced-effects/motion versions that preserve shared fairness/timing.

## Plan gate per current hero, before editing its effect

Read current roster/lore/abilities and authored/supplied source boundaries. Build a
separate retained-versus-refined assessment and timing sheet for Sean, Phaister,
Zack, Nemu/Kuro, Dante, Cheska and Rafi, plus any currently playable hero discovered
in the roster. For each: emotional intent; signature body silhouette/gesture;
preparation and exact live-action handoff; stage composition; owned shape/material/
colour/motion theme; foreground/midground/background layering; sound/voice rhythm;
caster/other-player/spectator/FPP readings; simultaneous-ult compatibility; reduced
settings; interruption/refusal/return; concrete files/assets; native acceptance shots.
The stage should briefly belong to THAT hero and still be recognizably TUMP.

Phaister direction to investigate explicitly: deliberate levitation and a readable
laughing performance, with expressive head/torso/hand timing and appropriate voice,
leading into her existing witch/eclipse ritual. Decide details from actual reference
and current art, not an unresearched generic levitation/explosion template.

## Implementation order and evidence

Preserve the active rooftop/bridge edge-recovery correction and unfinished world-sign
register. Owner explicitly says FINISH CURRENT WORK FIRST and do this later. Do not begin
ultimate research or implementation during the current recovery task. Preserve the
request now, then research and make per-hero plans before any ultimate edits.
Then build/refine one hero at a time, starting with the strongest justified design
unit; inspect actual native body/FPP/observer/stage, timed lead-in/live handoff and
outcome. Keep coherent themed components with distinct shapes, not repeated texture
motifs. Existing Dante shield baseline is preserved unless evidence warrants change.
Source sketches/generated references may help ideation but actual3Dapproval stays
Unity-native. Smallest meaningful behavior check plus reviewed motion/effect capture;
no verification-tool perfection loops. Final multiplayer/performance/native build
remains integrated qualification after implementation, with exact limits recorded.

## Newest sequencing and duration clarification

Owner: "its fine if its longer than2.8seconds part of the work is researching and
thinking about how long it should be". Then: "pls finish ur current work before
going to what i asked" and "just log whatever i requested ... do it later ... add
it to todo and make sure it survives compaction".

This ultimate work is DEFERRED IN ORDER, NOT DONE OR DROPPED. Finish the active
rooftop/bridge recovery correction first and preserve the earlier storefront/other
queue. Do not spend the current feature turn researching ultimates. When this pass
starts, research duration and pace as design questions: enough time for distinctive
character acting, anticipation and stage ownership, balanced against cast frequency,
shared paused time, repeated viewing, simultaneous casts, return to action and player
control. Longer than2.8seconds is explicitly allowed. Do not impose the old number,
or make every intro longer just because it is allowed. Choose and document durations
from the reviewed references and TUMP-specific analysis/per-hero plans, then implement.

Owner explicitly reiterates finish MAP REFINEMENTS too. They are not complete.
Finish remaining map/storefront/sign/edge-recovery work and map qualification before
starting this newly queued ultimate research/upgrade. Preserve every older TODO.
