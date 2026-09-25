# Paete's kit: animation and effect research

Owner, 2026-09-25: *"for these thoroughly research games that have similar shit in animating
it"*, *"thoroughly research in ordert to implement his abilities ty"*. Read beside the baseline
every skill rework follows: [amihan-kit-2026-09-25/direction.md](../amihan-kit-2026-09-25/direction.md)
and its [research.md](../amihan-kit-2026-09-25/research.md).

The owner's kit (power: **Dendro**; *"all skill names are to be made better but here are the
concepts"*):

| Slot | Concept name | Owner's description |
|---|---|---|
| Signature | Vine Pull | *"Vine like whip wherein his arms in tpp/fpp view both extend in sync towards a direction and he flies towards it like a spiderman web swing"* |
| Attacking | Throwing Slipper Plant | *"He summons a plant/tree that creates wooden slippers taht shoot slippers on command with a cooldown of 10-20 seconds and he can control when they shoot"* |
| Defending | Thorn Pull | *"He creates a plant /tree construct that extends towards all players and PULL their slippers towards it"* |
| Ultimate | Nature's Wrath | *"He puts down a plant/tree sentry in a location and he PULLS all players here, they get stuck on it (switchhes to tpp view) and they have to hold a button, when button is hold an animation plays of them trying to get out and they have to hold for like 7 seconds while stuck here"*, *"THEY CAN STILL THROW AND USE SKILLS WHILE STUCK BTW theyre js rooted"* |

## 1. How it was researched

Like the Amihan pass, this one looked at footage. YouTube storyboard sheets (the frame grids the
player uses for scrubbing) were pulled at their largest size and cut into labelled frames,
one every 1 to 5 s, then the key moments were enlarged. Ability numbers and wording come from
the games' wikis (League of Legends Wiki, Marvel Rivals Wiki, Genshin Impact Wiki, Overwatch Wiki,
Apex Legends Wiki). Frames stay out of the repo; what they showed is written here as an
observation first and a rule second.

| Reference | Source | Looked at |
|---|---|---|
| Marvel Rivals, Groot (Vine Strike, Strangling Prison, walls) | [oVNakAZkfH0](https://www.youtube.com/watch?v=oVNakAZkfH0), 81 frames at 1 s | 0 to 80 s; 3 and 10 to 15 s enlarged |
| Genshin Impact, Kinich (Canopy Hunter grapple swing, Dendro) | [PqAMGdYdV-k](https://www.youtube.com/watch?v=PqAMGdYdV-k), 102 frames at 2 s | 40 to 78 s |
| League of Legends, Zyra (seeds that sprout into shooting plants) | [v9DLY0Qrcm4](https://www.youtube.com/watch?v=v9DLY0Qrcm4), 78 frames at 5 s | 45 to 95 s |
| Spider-Man (Insomniac), web swinging breakdown | [6q1jZQbV2Wk](https://www.youtube.com/watch?v=6q1jZQbV2Wk), 140 frames at 2 s | too small to read poses; used for the swing arc only |
| Mortal Kombat, Scorpion's spear ("Get over here") | [F4PBFKzzRAg](https://www.youtube.com/watch?v=F4PBFKzzRAg) | the throw, catch, yank |
| Dead by Daylight, the wiggle (a held body struggling free) | [YXllkktrTBE](https://www.youtube.com/watch?v=YXllkktrTBE), 60 frames at 1 s | 0 to 19 s |
| League of Legends, Maokai and Ivern (tree champions) | wiki: Twisted Advance, Sapling Toss, Nature's Grasp, Rootcaller, Daisy! | numbers and behaviour |
| Genshin Impact, Yaoyao (Yuegui, a summoned thrower of radishes) | wiki: Raphanus Sky Cluster | a summon that throws on the caster's behalf |
| Overwatch, Zarya's Graviton Surge; Apex, Pathfinder's Grappling Hook | wiki | the pull-to-a-point and the grapple reel |

## 2. What each taught

### Groot, Marvel Rivals (the owner's first reference image IS Groot's art)

- **Vine Strike** (3 s): the arm whips forward and a THIN DARK TENDRIL snakes out from the hand
  to the target, low along the ground, wavy rather than straight. The arm itself stays an arm;
  the vine grows out of it. No glow on the vine; its read is its dark line against a light floor.
- **Strangling Prison** (10 to 15 s): 10 s, the right arm draws back and **a green glow gathers
  in the palm**; 11 s, underhand wind-up with the glowing seed at the hip; 12 s, the seed lands
  between the targets and **bursts into a bright green core with radial spiked vines**; 13 s,
  the targets are dragged in and held **standing**, bound, around the core; 15 s, the core fades
  and they are still clustered. Wiki: enemies *"cannot move but are still able to attack and use
  non-mobility abilities"*, which is exactly the owner's ult.
- Walls grow out of the ground as a burst of vertical trunks in 1 s with a green flash at their
  base.

Rules: **the tell is a green glow gathering in the hand; the seed is thrown, not placed; the
pull ends with bodies held upright round a bright core.** The vine's read is a dark wavy line.

### Kinich, Genshin Impact (Dendro grapple)

- A thin bright line shoots to the anchor; a **Dendro glyph** marks the anchor point; the body
  swings through a wide arc and the path is drawn as **curved green ribbons** that fade behind.
- If nothing is in range he still fires forward and swings in the air (wiki: *"fires a grappling
  hook forward and swings in mid-air"*): the grapple never "fails" visibly.

Rules: **mark the anchor; draw the path as a curved ribbon; always travel.**

### Spider-Man and Pathfinder (the swing and the reel)

- The swing is a pendulum: the body drops slightly under the anchor, then rises; the arm stays
  locked straight toward the anchor; legs trail; release at the bottom carries momentum forward.
- Pathfinder's reel is straighter: the hook lands, a beat, then the body is yanked along the line
  with a slight upward arc.

Rule: in a 14 m box a real pendulum is too long; **a short reel with a lift arc** (Pathfinder)
reads as a swing when the arms stay locked to the anchor and the legs trail.

### Zyra and Maokai (plants as summons), Yaoyao (a summon that throws for you)

- Zyra's seeds are small glowing pods on the floor; when triggered they **pop up** into a plant in
  about 0.3 s and it spits thorns at a target. The birth is a squash and stretch pop, the shot a
  recoil.
- Maokai's Sapling is **thrown** to a spot, lands, and waits.
- Yaoyao's Yuegui is a small figure that **lobs** radishes; the caster decides where it stands.

Rules: **the plant is thrown as a seed and pops up; it shoots with a head-back, snap-forward
recoil** (the classic plant-shooter squash and stretch); one plant, clearly the caster's.

### Scorpion's spear (the pull)

- Throw, a taut line, the catch, a HOLD beat of tension, then the yank, the victim flying toward
  the thrower. The hold beat is what sells the pull.

Rule for Thorn Pull: **reach out, catch, hold a beat, yank.**

### Dead by Daylight (the struggle)

- Held survivor: body sways hard left then right, legs kicking, on a steady rhythm; a progress bar
  fills on the victim's screen; camera stays third person behind the pair.

Rule for the ult: **a side-to-side wiggle loop plus a filling bar, third-person camera.**

### Zarya's Graviton Surge (pull to one point)

- Pulls everyone in a radius to one point where they float, clustered, for 4 s; the orb is a dark
  core with bright rims. Our version keeps them standing (Groot), which reads better at kid scale
  and keeps feet on the court.

## 3. Rules this gives Paete (feeds plan.md)

1. **His element is growth**: dark wavy vines, bark, leaves, seed pods, roots. Not glowing lines.
   The green light is only at the hands, the eyes and a sentry's core. That keeps him apart from
   Amihan's bright ribbons and Dante's stone.
2. **The vine is a dark line with leaves along it** (Groot, the owner's arm-extension concept),
   growing FROM the forearm, which unravels.
3. **Tell = a green glow gathering in the palm**, for every ability.
4. **Seeds are thrown and pop up** (Maokai, Zyra, Groot's ult seed).
5. **Pulls have a hold beat** before the yank (Scorpion).
6. **The grapple always travels** and marks its anchor (Kinich); a short reel with a lift arc,
   arms locked straight, legs trailing (Pathfinder, Spider-Man).
7. **Rooted bodies stay upright**, bound at the legs, and struggle side to side (Groot, DBD).
8. **Motif particle: narra seed pods**, flat discs that spin as they fall, plus single leaves.
9. **Fade by withering**: vines curl, darken, and drop leaves, never alpha on a solid.
