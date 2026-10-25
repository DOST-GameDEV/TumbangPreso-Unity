# Zack rework: research (2026-10-25)

Owner brief, this session: Zack is the poster boy, so his kit is the simplest and most readable speedster in the roster.
The kit is approved on the Wiki ("Zack Kit Rework" tab, V3 SKILL SHEET, SPEEDSTER): Momentum, Backtrack, Afterimage,
Live Wire, Overclock, with Overclocked and Zapped as statuses. Owner requirements for this pass: research every current
hero first, do not repeat another hero, adapt the speed and lightning references without copying them, keep the Philippine
anchor, and consider everything the kit touches (first person of the player hit, the network, the maps, every other hero).

Sources: the current source at `9b15d2844` (every `*HeroKit.cs`, `HeroIntroductionScene.*`, `HeroAbilityClips.*`,
`ViewmodelArms.CastGesture.cs`, `UltimatePresentationDirector`, `UltimateLiveLook`, `WorldEffectSnapshot`,
`MatchRpc.RafiWater.cs`, `CharacterMotor.Status.cs`, `StatusRules.cs`, `AIController.cs`), the Wiki's design history, the
earlier ultimate research (`docs/reports/ultimate-performances-2026-09-24/research.md` section 4 stays the shared cutscene
vocabulary) and the reference pages read in the built-in browser (Valorant wiki: Neon, Cypher; Overwatch wiki: Tracer;
Apex wiki: Mirage). No footage was stepped through this pass: the references below are mechanics and staging conventions
that the pages and the current source state precisely, and the cutscene is a trim of an existing film-reviewed staging.

## 1 · The cast as it stands (what Zack must not repeat)

| Hero | Signature | Attacking | Defending | Ultimate and its staging |
|---|---|---|---|---|
| Amihan | DRIFT, a dash that Whirls | FEATHERFALL, flight | WHIRLWIND, a travelling gale | AIRBURST, 5.9 s: the monsoon forms a bird, play resumes on the hit (`IntroductionIsTheWindup`-style story clock) |
| Yasmin (`cheska`) | COLD FEET, aimed slow patch | FROSTBITE, frozen slipper | GLACIAL WALL, aimed icicle arc | ABSOLUTE ZERO, 11.9 s: skate, point at each real player through her eyes, heart, stomp; play starts with everyone frozen |
| Dante | UNSTOPPABLE, cleanse and immunity | BOULDER, loaded slipper | BASTION, following reflect wall | CONTINENTAL DRIFT, 3.61 s: the charge-up only, the cut lands as the first band rises; then MOUNTAIN FORM 30 s (`UltimateAftermath`, the timed-kit passive field) |
| Rago (`sean`) | LAVA RAM, charge | MOLTEN THROW, loaded slipper | MAGMA FIST, aimed fist from a puddle | MAYON AWAKENS, 5.0 s: fists, the lava runs, the mountain rises at the aimed spot; Molten 8 s |
| Ilyas (`rafi`) | CROSSCURRENT, bend a flying slipper | SKIM, coated slipper | WATER WALL, aimed single-use curtain | BAHA, 9.2 s: tide out, shark ride, the wave is the front |
| Nemu | PEEKER, hidden trap ghost | FETCHER, slipper fetch | KEEPER, can guard | LET THEM IN, 7.0 s: the gate, souls possess every real player |
| Paete (protected) | LIANA LEAP, vine haul | BAKYA BLOOM, planted pod | THORN HARVEST, slipper snatch | MAKILING'S EMBRACE, 9.0 s: call, root, rise; the take on the real targets |
| Phaister (protected) | TELEPORT, aimed moth blink | CURSE: DRAIN | CURSE: HEX | VOODOO DOLL, 6.0 s: the day dies, the eye, the puppeteer, the stare |

Taken verbs (the Wiki's own list, confirmed against source): dash, flight, loaded slipper, slipper fetching, walls, ground
slow zones, aim-hold curses, summons, pulls, hit-everyone ultimates, lane knockback, cleanse. Free verbs: rewind, decoy,
tripwire, a timed speed state. The new kit uses exactly those four.

**How each must not read like its nearest neighbour**

| Zack | Nearest existing | The difference that has to show on screen |
|---|---|---|
| BACKTRACK | Phaister TELEPORT (aimed, moths, instant) | No aim. It always goes to one place he has already been, along the path he really ran, played backwards. Gold electric, a body that rewinds, never a swarm or a blink |
| AFTERIMAGE | Nemu's ghosts (translucent icy children, host-driven helpers), Phaister's doll | It is HIM: his own body and run, solid enough to fool at a glance, crackling with his gold; it never helps, carries or fetches; it only runs straight and pops |
| LIVE WIRE | Glacial Wall, Water Wall, Bastion (solid or reflecting walls); Peeker (hidden ring) | A thin line, no collider, nothing stops. Fully visible, two little street posts, sagging wire; a zap, not a slow or a block |
| OVERCLOCK | MOUNTAIN FORM (30 s buff, armour), Molten (8 s) | No armour, nothing added to his silhouette but light: speed streaks, a gold crackle round him, and his skills visibly changed (two copies, a double wire, a fast tile). Timed and short |
| MOMENTUM | Second Wind (after casts), Backwash (after a retrieve), Voodoo (curse) | Earned by running uninterrupted; grows in three visible steps; lost the instant he stops, throws or is hit |

## 2 · External references (adapt, never copy)

| Reference | What it does (from the page) | What reads at gameplay distance | TUMP takes | TUMP does NOT take |
|---|---|---|---|---|
| Valorant Neon, HIGH GEAR and OVERDRIVE | High Gear: "+35% speed boost", channelled, with a slide charge; Overdrive: full speed plus a lightning beam, 20 s minimum, kills reset it | A runner lit from inside with a blue trail; the beam is the loud part | A timed speed state you can read from the body: light on him and a short trail | The beam, the slide, an energy bar, blue-white palette, the 20 s reset-on-kill loop. Ours is 25 s fixed and adds no attack |
| Overwatch Tracer, RECALL | "Travel back in time to your previous location": 3 s of rewound time, 0.9 s of travel, phased, removes most negative statuses | The blue streak running backwards along her real path, and where she reappears | 3 s, the real path played backwards, a readable landing ghost | Invulnerability, the cleanse, restoring health. Zack keeps his statuses and stays taggable |
| Ekko (League), CHRONOBREAK (Wiki v2's note) | A visible after-image marks where he will return | The return point is a figure on the ground | Zack alone sees a faint echo of his 3 s-ago spot while Backtrack is ready (owner, 2026-10-25) | Showing it to everyone at all times (owner chose cast-only for others) |
| Apex Mirage, PSYCHE OUT | "a hologram duplicate of Mirage is sent in a straight line", replicates walking or running, vanishes when shot or after 60 s | A full body doing the same run as the player; the reveal comes when it is hit | His real body and run, the slipper he is holding drawn in its hand, straight line, short life | Steering it, mirroring inputs, reveal-on-hit. Ours lives 3 s and pops on the taya's touch |
| Valorant Cypher, TRAPWIRE | "a line that spans between the placed location and the wall opposite"; crossing slows and reveals after a short delay unless destroyed | The anchor and a thin line; it is covert | Two anchored ends and a thin line; crossing is the trigger | Covertness, the delayed tether, destruction by shooting. Ours is fully visible, instant, a 1 s Zapped |
| Demon Slayer, Zenitsu (Thunderclap and Flash) | A held crouch, then the cut, then he is already past, with jagged gold lightning along the path | Stillness then a single gold streak; the after-frames along the line | The hold before the go (the cutscene's frozen bolt), gold jagged light along a path | Swords, kneeling iai, a white flash |
| The Flash, Sonic | Speed lines and ghost frames behind the runner | Ghost frames read as SPEED only when they trail the real body closely and fade fast | Three short ghost frames behind him while Overclocked, a ribbon trail at full Momentum | Long trails that fill the court, blue speed force colours, rings |

Notes on what reads at this game's camera distance (14 m box, first person for people):
1. Ground-anchored shapes read best in first person: a ring or a post on the court is seen even when the body is behind
   you. The wire's posts, the Backtrack landing ring and Afterimage's footfalls all touch the court.
2. A body-shaped decoy only fools if it moves like a body: it must use his real run clip at his real speed, not glide.
3. Gold lines on the bright plaza court wash out without a dark edge: every line gets an ink edge or a darker gold under
   the bright core (the same lesson as Amihan's strokes and Overclock's q2 bands).
4. White is never the hottest colour: gold core `#FFE27A`, body `#FFC21A`, edge `#C8740E`, ink `#2A2230`.
5. Speed lines must stay on the runner, not on the screen: the owner's "no constant yellow screen wash" and the camera
   rules mean Overclocked's first-person cue is on his hands and at the frame's lower corners only.

## 3 · Philippine anchor (`docs/PHILIPPINE_ABILITY_DIRECTION.md`)

Zack is from Pasig and grew up on a condo roofdeck (`docs/CHARACTER_ORIGINS.md`). His electricity is street electricity:
the tangled wires on Manila's concrete posts, the transformer can, the spark at a loose connection in the rain, the
afternoon storm over the skyline. Kept:
- LIVE WIRE's posts are small concrete street posts (square, chamfered, grey, with a crossarm and one porcelain insulator),
  the wire sags between them with one loose loop and a small tangle at each post. No real brand marks, no meter boxes.
- Electricity is narrow and branching, gold, never a flag colour, never a deity, never a constant yellow wash.
- The ultimate's storm city stays (`UltimateProp.Zack`, his skyline), no new weather mechanics.

## 4 · Ultimate cutscene conventions, measured across the eight current films

| Hero | Length | Shots | How play picks up | Held equipment |
|---|---|---|---|---|
| Dante | 3.61 | 5 | The cut lands as the first band rises; play continues the wave | `dante-held` table variant |
| Rago | 5.0 | 3 | Play's mountain throws at the hand-back | `sean-held` |
| Amihan | 5.9 | 6 | Story clock; fan released at the hit | none |
| Phaister | 6.0 | 7 | The doll's stare, play picks up its lurch | none |
| Zack (current) | 6.4 | 5 | The bolt finishes into him; ring Zaps 4.5 m | carrying hand keeps the real shoe |
| Nemu | 7.0 | 4 | Victims already possessed | `nemu-held` |
| Paete | 9.0 | 8 | The sentry hands back from the staged body | none |
| Rafi | 9.2 | 6 | The wave front | none |
| Yasmin | 11.9 | 8 | Everyone frozen | `cheska-held` |

Shared rules every one follows (HERO_KIT_METHOD section 6): nothing on `Update`, posed from the scene clock; flat
effects on `Slipper.GroundY`; one travelling thing; open on the element already in the air; the backdrop steps back; the
hero's colours, never white; end on the effect landing; never show it twice; at most a few beats per idea.

**What this means for Overclock.** It is a self-buff, like Mountain Form and Molten, so the "effect landing" is on HIM:
the cutscene must end on him charged, and play must pick up with him already Overclocked (never charge up twice). The
owner chose (2026-10-25) to trim the current film-reviewed staging ("the bolt waits for him") rather than restage: keep
THE CALL, THE STOP and THE POKE, cut THE LOOK, THE SHRUG and the 4.5 m Zap ring (the new Overclock Zaps nobody, and a
cutscene must not show a rule that is not in play), and end on him taking the bolt and setting off. Target 4.5 s.

## 5 · Lessons from the owner's verdicts on recent kits, applied here

- Rago v1, "I can't even tell what his ult does from his cutscene" and "doesn't earn its place": a self-buff must be
  VISIBLE for its whole length on the body and in his own first person, and the HUD tile must count it down.
- Paete, "the live cast must tell the same story as the cutscene": the bolt that enters him in the cutscene is the same
  gold crackle he wears in play.
- Phaister and Nemu, "every peer sees the same act": every visible part of the kit is driven from accepted casts and host
  state, never from one peer's guess.
- Sean's MAGMA FIST, "what is drawn on the ground is what hits": Live Wire's drawn line IS the crossing segment.
