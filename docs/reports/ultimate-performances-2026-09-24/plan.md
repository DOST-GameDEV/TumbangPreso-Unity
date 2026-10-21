# Ultimate performances: per-hero plan

Research and the duration decision: [research.md](research.md). Status and evidence per hero:
[progress.md](progress.md). Owner direction: one hero at a time, each authored for its own
character, *"dont js spam copy paste stuff bcz it will be boring"*.

## 0 · The retained foundation, and what changes in it

Kept exactly: the host accepts a cohort, `PresentationClock` holds simulation at time scale 0, peers
receive the same phase and boundary, the introduction plays on RENDER COPIES through the overlay
camera (the real camera and aim never move), and `Complete` releases the clock before
`ExecuteSharedUltimate` starts each real ability. Refusal, cancel, late join, snapshot clock hold,
simultaneous casts and reduced settings keep their paths.

What changes, shared by construction because it is plumbing, not performance:

| Change | Why |
|---|---|
| `SharedUltimatePhase.Duration` becomes per cohort: the longest member's `UltimatePerformance.Seconds(hero)`. Host and peers derive it from the same accepted commits. Protocol 51 to 52, because a peer on the old fixed 2.8 would release its clock at a different moment from the host. | 2.8 is not a cap (owner). |
| Introduction clips, scenes and the view read that duration instead of the literal 2.8 / 2.4 / 2.68 numbers. The body handoff starts 0.4 s before the end; the overlay returns over the last 0.12 s. | One timeline per hero. |
| `HeroIntroductionScene.Shot` becomes a short list of authored shots per hero (two or three, each with its own framing and move), every shot checked for walls and mirrored or replaced by a clear one. | Camera acts with the body. |
| A per-hero backdrop (the "stage") in the render copy only. It never exists on the live court. | "The stage briefly feels like theirs." |
| `GroundIntroduction` accepts an authored lift, so a performance can leave the floor on purpose. | Phaister levitates. |
| The intro may place the hero's own voice cue at a chosen moment; the live ability then does not repeat it. | Voice inside the gesture. |
| Reduced motion or reduced effects: the performance still plays, from ONE locked shot with no camera move, faded in rather than cut, with the backdrop at low contrast and no flashes. Same duration. The previous behaviour was no picture at all. | A good version for those players too. |

Everything below is authored separately per hero: poses, beats, shots, stage, colours and voice
timing. Rig: root, torso, head, two arms, two legs; no elbows, knees, fingers or face changes.

## 1 · Sean, SUPERNOVA, 3.4 s

- **Emotion:** patience turning into commitment. *"Waits for one opening. Makes it count."* A lantern
  maker's care, then a single committed run.
- **Signature silhouette:** both hands cupped at the chest around a small fire lantern, head bowed
  over it; then the low coil with the lantern pulled in.
- **Beats:** 0.0 plant and roll the shoulders, head down. 0.45 hands come together at the chest
  (hold). 0.9 to 1.7 a parol (five-point lantern star) assembles in fire between his palms, one
  point at a time, while he watches it (the craftsman). 1.8 head snaps up to the target and holds a
  breath (the opening). 2.35 drops into a deep coil, arms swept back, lantern flares. 2.95 rises onto
  the balls of the feet, arms driving up: the first frame of the live leap.
- **Stage:** a warm dusk falls behind him; small paper-lantern stars rise from the ground behind
  him and drift up (San Fernando's lanterns as HIS memory), ember ground-flames ring his feet only
  in the coil. Orange and gold; the world behind is darkened ember brown.
- **Camera:** A medium three-quarter front (0 to 1.75); B close on hands and face, slight push
  (1.75 to 2.3); C low wide from the front looking up for the coil and rise (2.3 to 3.4).
- **Sound:** the existing Sean theme bed; his shout stays on the live leap, which is where the
  effort is.
- **Handoff:** ends on the rise; the live ability launches him up.

## 2 · Phaister, GRAND COVEN, 4.2 s

- **Emotion:** delighted mischief. She enjoys the setup more than winning; the laugh says the caster
  is in no danger and the victims are.
- **Signature silhouette:** floating, head thrown back laughing, arms open wide, legs trailing
  together, the eclipse behind her.
- **Beats:** 0.0 a hand to the hat brim, head dipped, a sidelong look (the sly beat). 0.7 a
  chuckle: small shoulder hops, hand near the chin. 1.1 the laugh breaks out: head back, chest open,
  arms flung wide, and she LIFTS off the ground (to 0.55 m by 2.1) with the five laugh syllables
  shaking her torso, voice cue `hero_phaister_ult` placed here. 2.4 at the top she draws the eclipse
  overhead with both arms, a slow turn of the torso. 3.4 one arm points down to claim the ground,
  the other stays up. 3.55 to 3.95 she drifts back down and lands soft with a small dip. 4.2 end on
  the pointing pose, which the live ritual continues.
- **Stage:** night falls behind her in violet; the moon appears above and a serpent-like dark arc
  (the moon-swallowing serpent of the Capul story she grew up with, drawn as her own fictional
  image) closes round it into the eclipse; a ritual ring turns on the ground under her as she
  points. Violet and ember rim on near black.
- **Camera:** A medium on her upper body and hat (0 to 1.05); B low angle that tilts up as she
  rises (1.05 to 2.4); C wide from below with the eclipse filling the sky behind her (2.4 to 4.2).
- **Sound:** the Coven theme bed plus her laugh inside the laugh; the live cast then plays the toll
  without repeating the laugh.
- **Handoff:** lands and points; the live ritual's 1.55 s circle starts from there.

## 3 · Zack, THUNDERSTRIKE, 2.8 s

- **Emotion:** cocky ease. Makes a hard play look casual.
- **Signature silhouette:** one finger pointing lazily at the sky, other hand in a relaxed fist at
  the hip, weight on one leg; then lining up the shot along his arm.
- **Beats:** 0.0 weight shift, head tilt to camera (a grin with the head, face untouched), a spark
  flicked off the fingertip. 0.45 one finger up to the sky, held: the sky answers with a flicker
  high behind him. 1.2 sights down his extended arm toward the aim, the other hand framing, like
  lining up a trick shot (hold). 1.85 a snap-down of the arm (punch key) and a distant bolt drops in
  the backdrop. 2.2 relaxed follow-through, shoulders loose.
- **Stage:** a slate storm front rolls in above the roofline behind him, a few angular yellow bolts
  far away; the storm flickers at the snap only (no strobe, one bright frame at most, off in
  reduced effects).
- **Camera:** quick and sharp: A side low (0 to 0.95); B over the shoulder along his arm toward his
  aim point (0.95 to 1.8); C front low for the snap and grin (1.8 to 2.8).
- **Sound:** theme bed; the whoop remains with the live strike.
- **Handoff:** the live strike lands at the aimed ring, which his over-shoulder shot pointed toward.

## 4 · Nemu and Kuro, DEVOURING SEANCE, 3.8 s

- **Emotion:** dreamy calm that turns out to have known all along; Kuro is the alarming half.
- **Signature silhouette:** Nemu small and calm, hands folded, looking straight at the viewer, with
  the giant Kuro rising behind her.
- **Beats:** 0.0 she is looking away up at nothing, distracted; Kuro bobs at her shoulder. 0.6 Kuro
  nudges her; she turns her head to him (hold). 1.2 she offers a hand; Kuro nuzzles it. 1.8 she
  opens the hand and turns to look DIRECTLY at the viewer (the knowing look, held). 2.2 to 3.2 Kuro
  swells into the rage form behind her while she stays calm and sways. 3.35 she points ahead, sending
  him.
- **Stage:** ink seeps out from Kuro and darkens the world behind them from the ground up; a few
  eye-like wisps (Kuro's own) open in the dark.
- **Camera:** A medium on Nemu with Kuro (0 to 1.75); B close on her face for the look (1.75 to
  2.2); C the retained reveal that fits both bodies as Kuro grows (2.2 to 3.8).
- **Sound:** Nemu theme bed; the wail stays with the live cast.
- **Handoff:** points Kuro ahead; the live ultimate sends him ahead as the giant.

## 5 · Dante, TITAN FISSURE, 3.8 s

- **Emotion:** immovable pressure. *"Holds the difficult space. Refuses to be rushed."*
- **Signature silhouette:** both arms up and apart pushing two stone slabs away from each other, feet
  wide (his Bernardo Carpio image of divided mountains, as his own fictional expression).
- **Beats:** 0.0 plants feet wide, head down, fists low (hold). 0.8 slow squat and brace, torso
  forward, arms low as if under a weight. 1.6 stands into it: arms rise overhead and PUSH apart; two
  stone slabs behind him rise from the ground and part (hold 0.5 s, the pose of the whole piece).
  2.55 both fists come together over the right shoulder, loading. 3.2 stamps forward into the
  loaded strike pose; molten seams glow forward along the ground toward where the live fissure opens.
- **Stage:** dust-brown haze; the two slabs and a low ridge line behind; molten orange only in the
  seams.
- **Camera:** A low front, slow (0 to 1.5); B wide low from behind his shoulder to see the slabs
  part (1.5 to 2.5); C low front for the load and stamp (2.5 to 3.8).
- **Sound:** Dante theme bed; the roar stays with the live strike.
- **Handoff:** the stamp pose flows into the live fissure ahead of him.

## 6 · Cheska, GLACIAL NOVA, 3.2 s

- **Emotion:** calm certainty. She has read the space and is closing the easy route.
- **Signature silhouette:** very still shoulders, one hand raised with a single precise fingertip
  line of frost, then a small crystal held between both palms at the chest.
- **Beats:** 0.0 stillness, a slow head turn left to right (reading the court), breath visible.
  0.75 one hand lifts and draws a short line in the air; frost grows along it. 1.45 both hands close
  round it (hold). 2.1 frost spreads from her feet in angular plates; she lifts the crystal to eye
  height, head tilted, a beat. 2.85 hands snap apart: the frame before the live nova.
- **Stage:** a cold highland mist behind her, pale and low contrast, with a ring of ice spires rising
  far behind like a closed route. Pale cyan and white on a greyed ground.
- **Camera:** A still medium close (0 to 1.4); B close on the hands (1.4 to 2.1); C medium wide low
  for the frost spread and snap (2.1 to 3.2).
- **Sound:** Cheska theme bed; her rising shout stays with the live nova.
- **Handoff:** hands apart, the live nova bursts.
- **Holding a slipper:** the retained one-handed variant (gathering at the free palm) is kept.

## 7 · Rafi, BREAKWATER, 3.4 s

- **Emotion:** the tease. Makes a rival commit early, gets the last laugh.
- **Signature silhouette:** a beckoning "come on" hand with the wave curling up behind him.
- **Beats:** 0.0 a feint: shoulders and head dip left, (hold) then right. 0.75 he straightens with a
  shrug and beckons with the free hand (two beckons). 1.5 drops into a boat-deck crouch, arms low and
  wide, water gathering at his palms. 2.2 the wave rises behind him as he lifts both arms. 2.85 a
  sweeping send: the arm drives forward and down: the frame the live wave leaves on.
- **Stage:** a sea horizon behind him and a curling wall of water rising from the ground, cyan and
  white foam on a teal-dark ground.
- **Camera:** A front medium for the feint and beckon (0 to 1.45); B low side for the crouch and the
  rising wave (1.45 to 2.5); C front low for the send (2.5 to 3.4).
- **Sound:** Rafi theme bed.
- **Handoff:** the send pose; the live wave travels forward.

## 8 · What each viewer sees

- **The caster:** their own performance in the overlay, full screen; their camera and aim are
  untouched under it.
- **Other players and spectators:** the same performance, of the seat they are watching if it is in
  the cohort, otherwise the first caster. The header names every caster.
- **First person:** the overlay covers the view; the first-person hands resume with the live cast
  gesture (`ViewmodelArms.CastGesture`) after release.
- **Reduced motion / effects:** same duration and performance from one locked shot, faded in, low
  contrast stage, no flash.

## 9 · Acceptance per hero

A pose sheet of the actual model at the key beats from the intro camera's side, reviewed before
moving to the next hero (`tools/preview_intro_poses.py`, silhouette preview, not the in-engine look).
Native Unity frames of the full performance, an observer's view and the reduced version are owed to
the Windows machine if this cloud machine cannot run the editor (see progress.md).
