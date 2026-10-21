# Full-roster presentation and implementation plans

Planning artifact, October1. These are candidate directions, not completed art,
newly watched reference footage or a claim of superiority to finalized work.
Mechanics for complete human kits remain authoritative. New-kit numbers are
proposals in new-kit-design.md. External source principles live in sources.md.

## Shared production contract

Each action has six phases: anticipation, commitment, contact, follow-through,
recovery and interruption. A phase can be brief, but cannot be omitted from the
implementation contract. Body motion causes the effect; particles do not cover
up a missing pose. Test the animation with VFX disabled first, then without audio,
then with all channels. A recognisable screenshot is insufficient proof of motion.

Basic casts do not acquire a cinematic camera. Keep the central can/target clear
in first person, grounded feet in third person, and an identifiable action for
observers. Gameplay timestamps drive release/contact. A decorative windup cannot
silently add input latency or a free invulnerability window. Existing authored
sockets, simple hands, flat faces and skeleton paths remain. No eyebrow/nose or
realistic-detail redesign. Do not animate mesh scale as a substitute for weight.

Ultimate beat sheets below are proposed presentation times within the shared
cinematic system, never a second independent gameplay clock. Lock accepted origin,
direction/target and cast identity once. Freeze/resume all relevant clocks through
the existing contract. After handback, retain any explicit live warning required
for counterplay. Never resolve a victim outcome during a movie they cannot answer.
Round abort, disconnect, rejected cast, host recovery and camera takeover must
restore controls and retire effects without replaying the impact.

### Audio direction and limits

Author short original material tests rather than restoring old rejected cues.
No new human voice imitation/voice assets are assumed. Every cue identifies an
event: ready, release, accepted contact, rejection or expiry. Caster detail can be
richer than rival warning; spectator cues must locate the real world event.
Prioritise can hit/reset and genuine tag above decorative tails. No phantom sound
may impersonate an authoritative score or tag. Test headphones and laptop speakers,
low master volume, four simultaneous casts, and repeated use for fatigue.

Use a transient, body layer and short tail only when each has a role. Cap concurrent
voices by event type, avoid phasey duplicate host/client playback, fade sustained
loops on every interruption, and restore ducking/filters on pause/exit. Never use
unbounded ringing or a permanent lowpass as a cheap personality cue. Choose final
levels by listening against actual game mix; numeric gain guesses are not a mix.

### Visual and performance gates

Danger geometry and readiness survive low quality, grayscale and reduced-effects
settings. No full-screen white flash or rapid flicker. Cosmetic particles may scale
down, gameplay edges may not. Bound active objects and material instances; pool
repeated short-lived pieces when measured churn warrants it. No per-frame scene
search, physics spray or runtime asset loading for a cast. Compare CPU/GPU/frame
and allocation samples before/after on the same scene, not screenshots alone.

Record front/side/rear, caster FPP, victim and spectator views; inspect at small
window and wide aspect. Film failed/no-target casts, wall proximity, mid-jump,
last-frame expiry and round transitions. Show the real shoe/can location through
the strongest moment. No invented victim in a cinematic to suggest a hit missed
by authority. Actual-peer and player-build checks remain separate from Editor.

## Amihan: impatient, woven motion

Identity: fast changes in air direction, not a cyclone covering the court. Use
broken diagonal ribbons and small pressure edges, with empty space between them.
The existing Vigan/binakol inspiration informs rhythm, not copied sacred motifs.

- Passive Second Wind: brief cloth/heel continuation after an accepted cast;
  no separate celebration or persistent aura. HUD remaining state comes from truth.
- Drift: heel loads opposite direction, hip/shoulder lead the cut, rear foot catches
  the body. Air follows the displacement instead of moving before the actor.
  FPP hands briefly part from centre; contact applies existing Whirled/push once.
  Recovery plants a foot, not a hovering glide. Rejection cancels the gust.
- Featherfall: open posture and alternating small balance corrections communicate
  supported flight. Keep the held slipper readable. End with lowering knees and
  a grounded catch; interruption must not leave flying pose or a false lift ribbon.
- Whirlwind: a compact scooping arm/torso sweep sends the existing arc. Shape the
  edge as a travelling crescent with a readable gap behind it, not a solid ring.
  Contact cue is spatial and short; no new body knockdown or invisible widening.
- Airburst: a clear held breath, opposing arm draw and forward release. The air
  front catches eligible bodies/shoes and launches them airborne; being airborne
  beforehand is not a prerequisite. Do not fake ground hits outside the fan. Caster and
  victim cues identify release and actual arrival separately.

Ultimate sketch:0-.5 settle facing; .5-1.2 gather with chest/arms opposing;
1.2-1.6 compressed quiet;1.6-2.1 release pose/forward camera opening;2.1-2.5 return
to playable view. The current8680d9ad correction already implements the Wiki1.5second delay
and60-degree fan. Preserve those values; this proposed movie does not determine
the live timing or reopen the resolved discrepancy.

Audio: dry fabric pull plus shaped air impulse, a short low body layer on release,
no continuous vacuum roar. Failure cue is a soft cloth stop. Acceptance: grounded actors caught in the fan are actually launched, observer fronts are visual,
no stale flight lift on expiry, no old pitch/LOS fix regression. Review pose and
front readability before adding particles. Risk: ribbons resemble generic magic;
keep them tied to hand path and departure air, not orbiting the hero.

## Cheska: practical route control, crisp restraint

Identity: deliberate placement, small efficient gestures, cold expressed through
structure and brittle transition rather than blue fog. She remains cute and
practical, not a dramatic ice queen imported from another game.

- Chilling Touch: shove contact gets a small directional frost bite at real contact,
  not an aura declaring every nearby player chilled.
- Cold Feet: a planted foot sends frost across the actual fixed field footprint.
  The7.5s field is not a trail following her footsteps or a movement buff. Keep
  its edge and expiry legible, with no misleading frost outside the status area.
- Frostbite: turn the actual held slipper, pinch frost into its sole/edge, then
  return it to the normal throw-ready pose. Imbue follows that exact object's state.
  Hit/expiry/drop rules stay truthful; no phantom projectile added for spectacle.
- Glacial Wall: palm follows the intended arc, shoulder braces when segments lock.
  Three accepted slipper impacts have readable structural loss, not just a tiny
  counter. Breaks expose the lane immediately; solid geometry matches visible edge.
- Absolute Zero: nearly still setup contrasted with one decisive closure. Show the
  real affected players at freeze time; clear ice off bodies as the Frozen phase
  transitions to Chilled. Their allowed action state must match the body treatment.

Ultimate sketch:0-.6 small hand gathering; .6-1.3 angular ice structure frames the
hands;1.3-1.65 a quiet held pose;1.65-2.05 closure;2.05-2.5 handback. Keep the Wiki
1.5s live warning and actual Frozen/Chilled sequence in the authority timeline,
not secretly consumed while opponents are camera-locked.

Audio: light ceramic/glass tick for preparation, short layered ice crack on real
contact, subdued granular release at thaw. Avoid long piercing glass squeals.
Acceptance: held/loose/airborne Frostbite ownership, three wall hits, obstruction,
status immunity, round cleanup, low quality edges and camera comfort. Risk: every
cast becomes the same ice burst. Differentiate trace, imbue, structure and world
state by action shape and envelope, not escalating particle count.

## Dante: weight that earns the fracture

Identity: stubborn grounded strength. Shoulders, hips and feet move the stone;
horns stay character silhouette rather than a new magical projectile.

- Earthbound: passive resistance uses a brief planted recovery, never a false
  immovable state or new shield overlay.
- Unstoppable: brace stance, chest opens and weight settles. A few anchored stone
  accents indicate the15s state without continuously hiding the body. Release on
  expiry/cleanse and preserve Tagged/Haunted exceptions exactly.
- Boulder: the actual held slipper gains a compact weighted stone treatment, with
  the wrist visibly compensating. Impact Concussed occurs only where the shoe hits;
  keep its silhouette legible and avoid replacing it with an unrelated boulder.
- Bastion: shoulders guide the following barrier; clear centre/edge treatment lets
  Dante see and act. No new opaque wall in the caster lens. Fade/break uses the
  current7.5s state, not an independent loop timer.
- Continental Drift: a foot plants, pelvis rotates, shoulder follows and force
  travels from body into the ground. Five current bands remain authoritative.
  Give each fault a related but non-identical branching silhouette, with one
  dominant forward fracture and small secondary chips, not parallel stamps.

Ultimate sketch:0-.5 toe tests ground; .5-1.25 lower stance/shoulder draw;
1.25-1.6 tension held;1.6-2.05 committed ground strike;2.05-2.5 handback into live
travelling cascade. These are direction targets to fit existing shared timing,
not permission to duplicate the hit with a second animation event.

Audio: stone scrape before weight, compact low impact, sequential smaller cracks
at the actual band positions. Keep the tail short enough to hear a slipper landing.
Acceptance: six existing Drift checks retained, add film comparison for body cause
and irregular fractures, confirm low quality/readable can and no caster occlusion.
Do not repeat unchanged mechanics tests after a camera-only edit. The current
court film passed forward order but failed the bespoke-art ambition; this is the
explicit reason for refinement. Actual player112/peer evidence remains separate.

## Nemu and Kuro: commands, hesitation, obedient mischief

Identity: Nemu gives a small definite command; Kuro supplies the exaggerated
response. This is companionship, unlike Phaister's domination or Paete's guardian.
Preserve the current local contributor's runtime claim and adopted human kit.

- Passive Kuro: mobility benefit while basic cooldown runs should be understated;
  no permanent ghost cloud blocking the small character.
- Sit: hand points to an exact place; Kuro settles visibly. Reactivation has a
  distinct beckon/return action. The10s anchor, legal return and expiry must be clear.
- Fetch: indicate the actual loose slipper; Kuro travels to it and brings it next
  to Nemu, not magically into a hand. No eligible slipper gets a readable refusal.
- Catch: cue to the upright can, Kuro braces around it. Five-second protection
  should visibly end before the next legal knockdown. No fake catch when can is down.
- Haunt: Nemu sends Kuro, who visibly chooses one seen target at a time. A successful
  chase creates that victim's Haunted state; no fake instant all-player curse.
  Failed reach/lost target needs a legible continuation, not a teleporting hit.

Ultimate sketch:0-.5 Nemu notices something; .5-1.1 quiet command;
1.1-1.7 Kuro's attention shifts outward;1.7-2.2 release/first target indication;
2.2-2.7 camera returns. Actual chase runs in live play, with host-ordered targets.
No huge replacement monster or new biography without supporting design.

Audio: soft material movement for Nemu, small dry companion movement, a short
recognisable send cue. Haunted muffle affects only the intended listener and
restores on every exit. Essential real tag/can warnings retain intelligibility.
Acceptance: command/no-target/recast/expiry, target ordering and occlusion, joining
mid-chase without replaying hits, victim-only perception, round and camera exit.
Current112timer/wire/marker evidence is not full Haunt acceptance. Risk: cuteness
makes danger unreadable; separate Kuro's relaxed idle from its committed chase.

## Paete: protected reference and preservation plan

No new animation/VFX/SFX/kit/cutscene redesign. Current code wins over stale Wiki
names/cost. Preserve LIANA LEAP, BAKYA BLOOM, THORN HARVEST and MAKILING'S EMBRACE,
all lifecycle ownership and the one-time guardian introduction/handback.

For each slot: passive has no invented behaviour; leap preserves actual pull/body
cause; bloom preserves planting versus later command and readiness; harvest retains
hold-before-yank and actual slipper identity; embrace retains accepted real targets,
outward-facing rooted actors and usable throw/cast state. Do not add a generic
sentry intro or repeat its catch on gameplay restoration.

Quality-reference checks: limbs connect to body, camera never goes inside a player,
small gestures precede large effects, readiness refuses honestly, gameplay handback
matches cinematic end. Only rerun affected checks for a demonstrated regression.
Audio review may describe the existing result but does not author replacements.

## Phaister: protected kit, scoped Hex refinement

Preserve Voodoo, Teleport, Curse Drain, the finalized doll/puppeteer ultimate and
all non-Hex authored work. Separate handheld prop, portal/controller and autonomous
doll identities. Do not revive retired blackhole/showman concepts from old notes.

Teleport retains its actual destination and arrival tell. Drain retains reach,
delay, wring and real victim consequence. Doll retains accepted summon/AI/scoring
and one introduction. These are preservation plans, not new creative assignments.

The October1 owner exception permits hallucination refinement specifically.
[Hex plan](phaister-hex.md) records source behaviour, baseline questions, convincing
grounded-copy direction, observer isolation and comparison evidence. Inspect the
victim's real view before deciding on density, placement or the no-shadow tell.
Do not change2s reach,10s arm,35s cooldown or7.5s Hex duration merely for prettier
motion. Keep the caster's finalized reach/stab cause unless a scoped defect needs
repair. No fake gameplay UI or tag sounds in the hallucination.

## Hydro / Rafi: scoop, fold, drain

The original designs live in new-kit-design.md; gameplay implementation and
qualification now live in TODO and the dated Hydro evidence. This section is
the presentation plan, not a blanket completion claim. Preserve
the active boat-repairer/water-trickster direction, practical workcloth/cord/buoy
identity and no-gills rule from BADJAO_EXPANSION. Do not import a surfboard from
Mualani or turn fictional workwear into a claim about traditional dress. Water should visibly transport or redirect, not freeze.

- Backwash: two heel strokes begin at the real pickup, then decay over1.5s.
- Crosscurrent: one hand scoops across the other, hips follow; a narrow travelling
  fold shows direction. At the single contact, bend the real shoe and collapse the
  fold. Recovery retracts hands. A missed current dissipates without a hit sound.
- Skim: thumb/hand passes along the actual sole. A thin meniscus holds until throw;
  ground contact flattens into a short wake following the real2m skim. No floating
  sphere conceals the shoe. Expiry drains from its edge and restores ordinary pose.
- Water wall: both palms lift a narrow curtain, then let it stand. Near-clear centre
  with strong top/side edges; one interception pulls the sheet inward toward the
  real contact before collapse. People crossing do not produce a fake collision.
- Baha: low crouch gathers a shallow arc, torso unfolds and hands send it downcourt.
  Loose shoes ride visibly at the front, then settle at their actual destinations.

Ultimate sketch:0-.5 low scoop; .5-1.2 water bows inward;1.2-1.7 tension in the
curved front;1.7-2.2 forward release;2.2-2.6 camera handback; live0.8s warning and
front movement remain answerable. Keep shoulder/face readable rather than filming
only water. End on the changed court, not a splash filling the lens.

Audio: vessel slosh/cloth-like water shear, compact splash at actual interception,
low rolling body for Baha that thins as it drains. No ocean roar masking footsteps.
Acceptance: preserve shoe identity/score, wall one-use contact, no held-shoe pull,
cover/jump avoidance, exact endpoints, max travel, cleanup, join/replay and low
quality edges. Risk: safe-line delivery erases retrieval; inspect actual routes
before accepting balance. Proposed migration replaces old charges deliberately.

## Pyro / Sean: deliberate ignition, visible commitment

Use his existing precision/lantern-craft background as a restraint on spectacle,
not permission to add a new prop/costume. Ember geometry is compressed and angular;
large flame appears only at release and clears quickly.

- Steady Ember: actual retrieval lights one steady core on the held sole; it fades
  after use/4s. No idle farming animation.
- Stoke Step: heel and shoulders compress, then the body travels as one committed
  mass. Feet catch the finish, elbows absorb recovery. No invisible trail hits.
- Empowered throw: a deliberate press into the held slipper, brief check of the
  charge, normal throw windup retained. Contact bursts outward once from the actual
  shoe, with fast clearing centre and no lingering damaging-looking floor.
- Cinder Gate: one hand draws the line, foot braces; the line lights outward from
  that gesture. Warning differs from armed state. One body crossing snaps the line
  dark as the impulse resolves; jumping over gets no false contact cue.
- Supernova: deep compression, upward launch, brief apex silhouette, committed
  landing and recoil through knees/hips. The body is the event, not a fireball
  hiding a static rig. Real destination is shown early and remains fixed.

Revised ultimate sketch: preserve the existing five-stick parol construction,
not a generic fire orb.0-.6 cup hands/check one joint; .6-1.4 assemble the frame
with staggered clean contacts;1.4-1.9 hold the completed shape and look up;
1.9-2.5 compress it into the body/feet;2.5-3.0 launch with the frame breaking into
directional sparks;3.0-3.4 handback into the actual live leap/landing. The live landing warning must
remain adequate; do not freeze victims through their only chance to evade. Follow
actual airborne motor state, not a fake animation landing while physics still flies.

Audio: restrained ignition tick, paper/fibre-like crackle, short pressure thump,
then falling embers. Avoid generic explosion boom on every button or fake damage
screams. Acceptance: held-shoe charge consumption, exact impact/score, no duplicate
can score, role swap, confinement, gate one hit and no residual hazards. Risk:
Stoke Step feels interchangeable with another dash; review its tactical commitment
and recovery before producing expensive effects. No claim of final sound quality
until mixed listening and repeated play.

## Electro / Zack: angular timing, earned second chances

Use short snapped direction changes and clean contact forks, not continuous
lightning noodles. Overclock changes choice structure, not a permanent visual
storm. Keep the actual body's balance legible through rapid motion.

- Amped-Up: only a real awarded point advances cooldowns; a quick icon tick confirms
  the new remaining time. No extra score flourish or invented energy meter.
- Quick Circuit: heel plants, shoulder cuts laterally, trailing arm catches balance.
  Overclock's optional second cut gets an unmistakable ready cue and clean expiry.
  No phantom body left where a player is no longer hittable.
- Bank Shot: trace one angular contact motif over the actual slipper. A wall impact
  produces one short fork aligned to incoming/outgoing paths, then disappears.
  Second-bank readiness is a small distinct edge, not a full trajectory preview.
- Closed Circuit: hand/eyes track the real target, .4s acquisition visibly tightens,
  contact snaps once. Broken LOS unthreads immediately. Overclock's next target
  requires another real acquisition, never an automatic arc through cover.
- Overclock: Zack plants both feet and deliberately accepts a vertical strike.
  Brief stillness precedes the snap; after the impact he checks/settles the charged
  stance, then returns to gameplay. No permanent camera shake or rapid flicker.

Revised ultimate sketch: retain the existing casual fingertip/snap confidence.
0-.5 flick one spark and notice it; .5-1.2 raise a finger as the storm answers;
1.2-1.65 a held sideways glance, almost a dare;1.65-2.0 snap as lightning returns
to Zack himself;2.0-2.65 absorb it through the actual grounded body;2.65-3.4 small
shrug and handback. The persistent upgrade is the payoff, not a generic angry pose. Apply the separately
telegraphed live strike and persistent state once. Joining peers receive upgraded
state without the camera sequence or nearby Zapped being repeated.

Audio: dry electrical contact tick, rising restrained hum, single short strike and
quick decay. Distinguish acquisition from success and expiry; avoid shrill endless
buzz. Acceptance: once-per-objective cooldown reduction, bank corner contacts,
second-action windows, touch/pad aim tolerance, match persistence versus round
reset, new-match reset and late join. Risk: snowball and overly perfect bots;
measure opportunities rather than adding raw speed/status strength.
