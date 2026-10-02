# Amihan's direction, and Airburst's

## Who she is when she moves

Lore: a weaver's daughter from Vigan, the fastest hero, bright, proud of her
town, hates a stalled game, commits before a play is ready. Her element is the
amihan, the cool northeast wind. Her motifs are cotton (the Binatbatan, beating
cotton free for the loom), abel threads and the kasikus, the binakol whirlwind
of graduated diamonds radiating from a centre, woven from straight lines.

Observable movement logic, for every action of hers:

- **Light feet, early weight.** She is on the balls of her feet and starts
  moving before the decision is finished. Recovery ends in a small settle, not
  a heavy plant.
- **Coil and unwind.** Power comes from a torso twist away from the target,
  then an unwind through it (the existing "spiral" language). Arms travel in
  arcs; nothing she does is a straight piston.
- **The head stays on the target.** While the body coils, the head counter-
  turns to keep looking where the wind will go. That is what makes her read as
  deliberate rather than flailing.
- **Holding still is effort for her.** The one action that roots her, the
  Airburst windup, should look like work: a held coil that tightens in beats.
- **Never floats for show.** Featherfall is her real flight; everything else
  stays grounded so flight keeps meaning.

## Airburst in one sentence

**She pulls the street's wind into her cupped hands, packs it tight, aims it
down one lane, and when she lets go the whole lane goes at once.**

The travelling thing is wind thread: thin abel-coloured lines that move
*inward* to her hands through the cutscene and the live windup, and reverse
*outward* at the release. Inward means gathering; outward means the strike.
That reversal is the readable moment.

## The three channels, beat by beat

| Beat | Body (everyone) | First person (hers) | Effect (world) |
|---|---|---|---|
| Call (cutscene) | Hip-weighted stance, a sharp flick of the right hand that pulls back to her chest, head swings down the lane | n/a (shared cutscene) | Threads begin bending toward her hand |
| Gather (cutscene) | Arms sweep out wide, then in, meeting cupped at her right hip; torso coils right, head stays on the lane | n/a | Threads stream into her palms; a cotton boll forms between them; kasikus diamonds bloom under her |
| Pack (cutscene, then live 0 to 1.3 s) | Two tightening beats of the coil; root sinks a little each beat | Both hands cupped low right of centre, pressing on the beats | Chevrons inside the fan step inward and brighten on the same beats |
| Aim (cutscene end) | The coil held; front foot set down the lane | n/a | The fan's two edges race out from her feet; chevrons appear |
| Draw back (live 1.3 to 1.5 s) | Extra pull back: torso to its deepest coil, hands drawn behind the hip | Hands drop back and down out of the centre | Chevrons pull in hardest; edges brighten |
| Release (live 1.5 s) | Unwind: both palms driven straight down the lane at chest height, front knee forward, rear leg long | Both palms drive forward low at centre, under the reticle | Every chevron fires outward at once; a standing chevron front crosses the court within about 0.1 s; sigil flash at her feet |
| Follow-through | Torso turns slightly past centre; hands part outward | Hands part | Chevrons thin to threads; cotton flies outward |
| Recovery | Back to a light stand within about 0.45 s | Hands return to rest | Edges fade; nothing remains within about 0.8 s |

Times are the plan; the beat sheet holds the exact numbers. The live release
key sits on the gameplay release (1.5 s after handback), not near it.

## Effect language

- **Kasikus chevrons.** Inside a 60 degree fan centred on her, each graduated
  kasikus diamond shows only its front corner: a forward-pointing chevron. Six
  of them, drawn as thin flat ribbons with gaps where they would touch the
  edges, are her own version of Miks's concentric rings: concentric, woven from
  straight lines, and directional. They point where victims will be pushed.
- **Edges are the boundary.** The two fan edges are the honest gameplay limit
  (exactly 30 degrees) and remain the brightest lines from the first frame to
  the release. Nothing decorative may extend wider than the contact fan.
- **The interior stays court.** No filled wedge. The can, chalk, slippers and
  players read between chevrons on Low and with reduced effects.
- **Beats, not a ramp.** Pulses at 0.5 s and 1.0 s, then the draw back and the
  release at 1.5 s. Three counts ("pack, pack, go") are easier to dodge by than
  a smooth fade.
- **Thinning ends.** Everything ends by narrowing to threads and dropping cotton.
- **Her palette only**: wind green body, cream core, deep green ink, cotton and
  a little brooch gold. No orange rings, no white flash.

## Camera

- **Cutscene:** three shots, all from her right side so the screen direction
  never flips: CALL (front-right, slow push in), GATHER (low right side, her
  hands near the lens), AIM (over the right shoulder, rising to show the lane).
  The last shot tells every player which way she is facing. The reduced-motion
  still is a side view showing her and the lane.
- **Live:** no added camera work. The existing release camera punch stays where
  the gameplay owns it (`AmihanStorm.Release`).

## UI tells

None added. The floor carries where and when; the body carries who and the
moment. A HUD countdown, a screen-edge warning or a victim screen tint was
considered and rejected: the match HUD stays minimal, and a screen effect is
Miks's language, not TUMP's.

## Alternatives rejected

- **Retime the current hold-and-shove.** Fixes the lag but still spends the
  release pose during the dodge window.
- **Overhead gather and slam.** Light from above and a downward slam are Paete,
  Phaister and Zack vocabulary, and a slam reads as down, not forward.
- **Blow the cotton off her palm as the release.** Lovely in close-up, invisible
  at match distance; the cotton survives as the charge object instead.
- **A full spin release.** Her spiral, but a spin hides her facing during the
  only window where facing is the gameplay information.
- **Circular sound rings, a vortex cage, a filled cone.** Miks, Venti and a
  puddle respectively.
- **Showing targets in the cutscene.** True positions, but any reaction staged
  before the live window is a promise the rules may not keep; and staging bodies
  would need shared phase plumbing.

## Interruption and cleanup

- Round end or reset during the windup: the storm already destroys itself when
  its ability stops winding up without releasing; the new fan pieces are its
  children and go with it. The body clip must not be left in the coil.
- Round end, disconnect or rejection during the cutscene: the phase disposes
  the scene; any scene-built fan goes with the scene root.
- After the release nothing persists past about 0.8 s; Whirled marks on victims
  are status presentation owned elsewhere and are unchanged.
