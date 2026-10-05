# Paete refinement: existing shapes, more convincing motion

Owner scope: refine existing attacking/defending animations without replacing
Paete or the plants; add a player latch to the signature that pulls both bodies
unequally toward contact. ASTRAReworks only. These are active implementation and
acceptance tasks, not a completion report.

## Research applied

Riot's [VFX guidance](https://nexus.leagueoflegends.com/en-us/2017/10/dev-leagues-vfx-style-guide/)
prioritizes gameplay clarity, controlled noise, theme and timing. Apply that here
with visible endpoints/tension and separate beats, rather than more particles.
The linked historical PDF redirected to the homepage and was not reviewed.

The actual official [Thresh Q demonstration](https://www.leagueoflegends.com/en-us/champions/thresh/)
was downloaded through its video element and inspected as sequential frames:
a readable release tip, a connected taut line, target displacement and caster
approach make the cause of movement apparent. Paete retains his authored braided
branch design; no chain model, damage or League stun is being copied.

The official [Pyke Q clip](https://www.leagueoflegends.com/en-us/champions/pyke/)
shows a distinct preparation, linear release and bounded displacement. Riot's
[Pyke design account](https://www.leagueoflegends.com/en-gb/news/dev/origins-pyke/)
explains why a charged, limited-distance pull retained counterplay. Here the
victim should move only a smaller capped share, while Paete covers most distance.
The owner asks them to meet, not flip through each other.

Unity's [CharacterController.Move](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/CharacterController.Move.html)
constrains movement through collisions. The implementation must use the current
motor/collision path; do not teleport either participant through cover.

## Existing source and actual baseline

Source8e090ccd: Bakya Bloom grows a wooden slipper and launches on recast. The
current recent velocity correction preserves its authored13m/s flight. The
pitcher's old anticipation occurs after Fire has already spawned the projectile.
Thorn Harvest reaches visually in.18s, snatches immediately, and then has.25s
hold/.5s pull. Liana Leap currently ignores player colliders and reels only Paete.
Baseline native FilmHisSkillsInAMatch passed1/1 with405wide+405owner frames;
actual plant/defending frames were inspected. Preserve this baseline.

## Candidate sequence and safeguards

1. Attacking: coil as the bakya becomes ready, show the stored load clearly,
   open mouth/lid, throw forward at the actual release, then damped recovery.
   Preserve the existing model, palette, growth, roots, leaf details and cooldown.
2. Defending: show vine tip travel, contact before snatch, a short taut hold,
   longer pull and release. First candidate:.45s reach,.65s pull-start,1.1s
   travel; existing3s construct. These are candidates to judge in footage.
3. Signature: nearest valid visible player attachment, stable target identity,
   small bounded victim travel, larger caster approach, capsule-distance stop.
   Retain ground/wall fallback. Host decides target/movement; owner simulation,
   event/movement epochs and cancellation must prevent late or duplicate pulls.
   No added damage, stun, scoring, through-wall motion or teleport.

Keep preparation/contact/release observable at ordinary play distance. Compare
same-camera normal-speed and slow-motion captures. Reject a candidate that hides
existing silhouettes, loses attachment, snaps unexpectedly or makes cues noisier.
Gameplay timing and player-latch semantics need matching protocol qualification;
current148 must not be advertised as qualifying the proposed changes.

## Required evidence before completion

- Loaded plant winds up before release; correct projectile origin/flight,
  cooldown/reload and existing uproot/expiry remain.
- Held/loose slippers: no premature snatch, visible attachment, slower continuous
  travel to the same construct, cleanup and no collision/ownership regressions.
- Player latch: player in front/behind cover, terrain fallback, range/nearest hit,
  close contact, moving target, two-body stop without crossing, walls, immunity,
  interruptions, hero/role/round changes, despawn and stale/duplicate events.
- Native geometry/behaviour plus actual visual comparison; actual matching peers
  for authority/synchronization. Do not call a receiver unit test a peer pass.
