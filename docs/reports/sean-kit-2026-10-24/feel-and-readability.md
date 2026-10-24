# Sean: how every skill reads, feels and behaves for everyone (2026-10-24)

Owner, 2026-10-24: *"thoroughly think abt how all his skills should look like for everyone and interact with all shit in
the game (uneven map surface, etc). how should other ppl see it. if affected or hit how do u make them EXPERIENCE or feel
na its happening to them. spectator what do they see. how do u make it felt but not overwhelming and make it easy to
understand na that shit js happened."*

This sits beside `plan.md` (the kit and the six beats) and `research.md` (the footage study). It answers four questions
per ability: what Sean sees, what the player it lands on feels, what everyone else sees, and what a spectator is shown;
then how each one meets the real court (kerbs, steps, terraces, roofs, walls); then the budget that keeps all of it from
becoming noise. Section 7 is what the audit of the code as committed (`191b41cde`) found, and section 8 is the refined
build order.

## 0 · Research this adds (text sources, read 2026-10-24)

| Source | What it teaches | TUMP takes | TUMP does not take |
|---|---|---|---|
| Riot, "Clarity in League" (leagueoflegends.com/en-us/news/dev/clarity-in-league, 2021) | Three vows: gameplay clearly conveyed, hierarchy preserved (the most important thing draws the most attention), noise minimal. Attention scales with damage, crowd control, dodgeability and impact on play. Projectiles show their direction even when still. Hitboxes must match the effect | **The ring is the hitbox.** Every Sean area is drawn at exactly its resolved radius. Attention order: bomb landing ring, then Magma Fist mark, then RAM, then PRESSURE splash. The fist and the bomb are dodgeable, so they get the warning; the RAM is point-blank, so its warning is the brace | Champion-scale clutter; MOBA top-down rings drawn everywhere |
| League VFX style guide, as summarised by VFX Apprentice (vfxapprentice.com, 2026) | Primary element (the gameplay signal: high value, sharp, opaque) versus secondary (flavour: lower value, softer). The impact peaks inside the first quarter of an effect's life. The brightest value sits on a projectile's leading edge. "If your FX feel long, they're way too long." One dominant colour, the complement kept small | **Gold is primary, red is secondary.** Lava reads gold at the core and leading edge, cherry red only at the rim. Every contact peaks in the first 25% of its life; every linger except the pool is 0.6 s or less | Painted textures on screen space; complementary blue accents |
| Masahiro Sakurai, "Eight Hit Stop Techniques" (summary of his video tycbMSjDDLg) | The one who is hit shakes more than the attacker; the hitbox does not move with the shake; on the ground the shake is horizontal only; the amplitude decays; the pause length is tuned per attack; the attacker is never completely frozen | **The victim's body shakes horizontally on every screen** for the length of its freeze, decaying, while its collider stays put. Sean's own body gets a much smaller nudge. The victim's view gets the freeze (`HitFeel`), never anyone else's | Freezing the whole match (`Hitstop` is for the shared ultimate beat only) |
| Blizzard, "Overwatch: The Elusive Goal, Play by Sound" (GDC 2016, Lawlor and Neumann) | Sounds are ranked by threat to YOU: an enemy's footsteps are louder than a friend's, and a threat aimed at you outranks one aimed elsewhere | **The targeted player hears it first.** The Magma Fist's bubbling and the bomb's falling whistle play louder, and close to centre, for the player they are coming for | Voice lines (human-voice policy) |
| Unity URP setup in this repo | No decal renderer feature is configured; `VfxShapes.DrapeToGround` already bends a flat mesh onto the court (climbs up to 0.60 m, refuses walls) and `VfxShapes.TryGroundUnder` finds the court under a point | **Every ground mark is a draped mesh**, so it climbs kerbs and terraces on Low as well as High | A new decal render feature (a pipeline change for one hero) |

## 1 · The rules that come out of it

1. **One reaction per person, matched to what happened to them.** Victim: freeze, a camera punch from the direction of the
   hit, a flash in Sean's colour at the frame edge, a pad pulse. Sean: a small forward thud on his own screen, only for a
   hit he can see. Everyone else: the world event only (the splash, the shaking body, the stars). Nobody else's screen
   freezes or shakes for somebody else's hit.
2. **Warnings scale with how dodgeable a thing is.** RAM is point-blank: its warning is the 0.25 s brace (shoulder down,
   bubbling, his body glowing), seen by everyone. Magma Fist and Volcanic Bomb travel: each draws a ring on the ground where
   it will land, on every screen, and the player it is coming for gets it louder and brighter.
3. **The ring is the hitbox.** A ring's radius is the resolved radius from `SeanRules`, never a look that is bigger or
   smaller. A blast pushes everyone inside its ring the written distance (no hidden falloff), and nobody on another level
   (a roof above, a street below) is hit by a blast they cannot reach.
4. **Gold means Sean.** His UI accent (deck rim, stars, the hit flash, his rings) moves from `#ff3355`, which since the
   taya became red for everyone sits 2.2 degrees from the taya's colour, to molten gold `#C9A000` (25.6 degrees from the
   attackers' orange, and separated from every other hero by hue or lightness; both `HeroPresentationTests` colour rules
   pass). His lava keeps its gold core and cherry-red rim; red never dominates.
5. **Short, then clean.** Contacts peak in the first quarter, lingers are 0.6 s or less, and the court is left bare.
   The two things that stay longer are gameplay states, and both show a draining timer: the hot can (1.5 s) and the pool (4 s).
6. **Everything respects the player's settings.** `ReducedEffects` removes the lens splats, the body shake and the pad pulse;
   camera shake already scales by `EffectiveCameraShake`; `Rumble.Enabled` is the pad's own switch.
7. **What a player feels is what the host decided.** Every reaction is played from the host's replicated event
   (`MatchFlair`), so a client victim, a client observer and the host all see the same hit at the same place.

## 2 · Per ability, per viewer

### PRESSURE (passive)

| Who | What they get |
|---|---|
| Sean | Three pips on his passive tile, filling with each landed hit and draining after 6 s idle. At three, his fists glow gold and drip on his first-person arms; a short rising hiss marks "full" once |
| Everyone else | His fists on his body glow in three steps (none, warm, dripping). Full heat is the read a rival needs: his next shove or RAM erupts, so stay out of reach. The bars travel to every peer (section 7, item 9) so the glow is the same on every screen |
| A player erupted on | The hit's own reaction one weight up (a shove becomes a knockdown-weight hit), with a burst of round lava drops at the contact point |
| A player in the splash beside them | A jolt: a light freeze and a punch from the splash's centre, no stars (0 s stun) |
| Spectator | Nothing of its own: an eruption is part of the hit the director is already showing |

### LAVA RAM (Q)

| Beat | Sean (first person) | The player he hits | Everyone else | Spectator |
|---|---|---|---|---|
| Tell, 0.25 s brace | His arms drop low, the view dips 4 cm; bubbling from his own shoulder | Hears the bubbling from Sean's direction (3D) | Shoulder dropped, back foot dug in, lava beading on the leading shoulder; the brace is the dodge window | Shown if the director is already on him |
| Release | A thump, the view lunges | | Lava kicked off his back heel; ropes stream back off his shoulder | |
| Contact (a body) | He stops dead: a 0.12 m forward thud on his camera, a pip fills | **Knockdown weight**: 70 ms freeze on their own view, the camera punched FROM Sean, a gold edge flash, a pad pulse. Stumble 0.8 s with stars | The victim's body shakes side to side for 70 ms (decaying) and recoils away from him, a crown of lava drops at the contact point, they are thrown 3.0 m (4.2 m erupted), stars for 0.8 s, a wet splat and a boom | The hit is a Downed/LataHit-class moment only if the director is near; no special cut (a RAM is small) |
| Contact (a wall) | A 0.2 m backward punch on his own camera, a dull thud, his view wobbles for the 0.6 s daze | | He rebounds and sits dazed under a ring of steam, stars for 0.6 s: the punish window, readable at a glance | |
| Linger and dissipate | | | 0.3 s of glowing footprints that skin over into black glass and steam away | |

Uneven ground: the motor already carries the charge over kerbs and up ramps. The wall probe ignores any surface you can
walk on (its normal points up), so running up a slope or a step never dazes him; a real wall or prop does. Off a ledge
the charge continues in the air and the body check still applies.

### MOLTEN THROW (E, attacking)

| Beat | Sean | The taya | Everyone else | Spectator |
|---|---|---|---|---|
| Tell | His hand closes on the slipper, lava seeps between his knuckles; the slipper drips in first person | Sees the dripping slipper in his hand: a heavy throw is coming | Same | |
| Travel | A flat line, drips hissing on the court | | The slipper flies flat and fast with its brightest point at its nose (the leading-edge rule) and a short smoke line behind | |
| Contact (the can) | Normal can-hit confirmation | The can cracks, flies 3 m spinning and lands glowing; a gold ring on the court round it drains over 1.5 s. Walking up to it, the reset prompt reads **TOO HOT** with the same ring, and pressing it hisses. The prompt returns the moment the ring empties | The glowing can, the draining ring, the hiss and a puff of steam as it cools to its own colour | The existing LataHit beat already cuts to the can |
| Miss | | | The slipper lands with a hiss and a small steaming spot for 0.5 s | |

Uneven ground: the hot can's slide follows the court. It rides down a kerb or a terrace edge (it falls), stops at any wall
or any step up taller than 0.3 m, and never leaves the playable square (`AIController.ClampToPlayable`), so it can always
be reached and reset.

### MAGMA FIST (E, defending)

| Beat | Sean | The player it is coming for | Everyone else | Spectator |
|---|---|---|---|---|
| Tell | Arm pulled back, a glob swelling on his knuckles | | Same | |
| Travel (up to 0.75 s) | The fist skims low, curving after them | **A gold ring under their own feet on every screen, brighter on theirs, and a bubbling that rises as the fist closes, played louder for them.** A last-moment sidestep beats the 90 degree per second turn: the ring is the read that tells them to move | The fist skimming and dripping, the ring under the target: everyone knows who is being hunted | |
| Contact | The heat pip fills | The puddle opens under them and the giant fist rises; they are hit as the knuckles reach their waist (0.1 s), so what they see touch them is what hits them: **knockdown weight**, the camera punched in the direction they are thrown (away from the can), a pad pulse, 1.0 m up and 3.5 m away | The body shakes, lifts and flies away from the can; the fist holds 0.2 s at the top, glowing | |
| Dissipate | | | It skins over into black glass, cracks and slumps back into the puddle, which steams to a dark stain | |

Uneven ground: the fist follows the court smoothly (no pop when the ground changes height), climbs kerbs and steps up to
0.6 m like a ground mark does, falls off ledges, and stops at a wall, a prop or a terrace face taller than that. It cannot
reach a player on another level: if the target is more than 1.2 m above or below the fist's court, it passes under them
and fizzles at the end of its flight, and the punch only throws players standing on the puddle's level.

### MAYON AWAKENS (ultimate) and Molten

| Moment | Sean | Players inside the blast | Everyone else | Spectator |
|---|---|---|---|---|
| The cutscene (7.0 s) | The shared phase, on every screen | Same; staged where they stand | Same | Same (the phase plays for every seat) |
| The blast at the hand-back | | **Ultimate weight**: 110 ms freeze on their own view, the camera punched from Sean, a gold flash, a pad pulse, thrown 3.5 m whatever their distance inside the 4.0 m ring, stars for 0.4 s | A draped shockwave ring racing out to exactly 4.0 m; bodies shaking and flying | The director's ultimate shot is already on him |
| Molten, 8 s | Lava fists on his first-person arms; the E tile shows VOLCANIC BOMB with the ultimate's icon and an 8 s ring draining | | His fists and forearms oversized liquid lava, steam off his shoulders, a heat shimmer above him | |
| Somebody shoves him, or any knockback reaches him | | The shover gets a small backward recoil on their own camera and a dull wet thud: **you hit him and he did not move** | His fists splash and his feet stay planted | |
| The end | The lava on his arms skins over into black glass, cracks and sheds over 0.6 s | | Same | |

The blast does not knock the can over. The bomb does; the awakening is about bodies.

### VOLCANIC BOMB (E while Molten)

| Beat | Sean | Players where it will land | Everyone else | Spectator |
|---|---|---|---|---|
| Tell, 0.2 s | The lava runs off both arms into one ball between his fists, held overhead | | Same | |
| Release | His arms are bare again at once | | | **The director cuts to the landing spot** (a new interest, section 7, item 12) |
| Travel, 0.9 s | | **A gold ring at exactly 4.8 m on the ground where it will land, filling as it falls; brighter, with a falling whistle, for players standing inside it** | The ring and the ball's growing shadow; the ball wobbles and sheds drips | The ring and the arc |
| Contact | | **Ultimate weight** (knockdown weight for the smaller dropped bomb), thrown 4 m from the centre, stars 0.5 s | A crown of lava thrown up and out, the ring, the small Mayon cone of light; the can goes over if inside (never his own can) | The splash, held 1.5 s |
| Linger, 4 s | | Inside the pool: slowed 30%, a slow icon on their own screen, sizzling footsteps | The pool glowing, skinning over from its edge inward | |
| Dissipate | | | Glossy black skin, cracks, steam | |

Unthrown at the end of Molten, the ball forms at his feet for 0.4 s with its 3.0 m ring drawn, then splashes: the ring is
the warning for the players around him.

Uneven ground and walls: the bomb's arc is swept against the world each frame. A wall, an awning or a roof edge in the
way stops it, and it splashes at the foot of whatever it hit, never through it. Its landing point is the court under the
aim, roofs included, found from above. Its splash and pool affect only players within 1.8 m of the splash's height, and
the pool is a draped mesh, so it lies on a slope, climbs a kerb and does not float over a terrace edge.

## 3 · The budget (felt, not overwhelming)

| Weight (`HitFeel`) | Freeze on the victim's view | Used by |
|---|---|---|
| Jolt | 20 ms | PRESSURE's splash on bystanders |
| Solid | 50 ms | an ordinary shove landing |
| Knockdown | 70 ms | LAVA RAM, MAGMA FIST, an erupting shove, the dropped bomb |
| Ultimate | 110 ms | the awakening blast, the thrown bomb |

- At most one reaction per victim per hit, and none for anyone else. A player hit by the bomb and its splash together
  gets the one heaviest.
- Body shake: 0.03 m (Knockdown) and 0.05 m (Ultimate) side to side, horizontal only, decaying to zero over the freeze.
- Pad pulse on the victim only: low 0.35 and high 0.55 for 0.12 s (Knockdown), 0.6 and 0.8 for 0.2 s (Ultimate).
- Text popups: none. A BONK on every hit is the clutter `docs/VISION.md` § 2 warns about; stars already say "held".
- Lens splat (lava drops at the edge of the victim's view, from the side the hit came from): Ultimate weight only, three
  drops, 0.5 s, cooling to black and sliding off. Off under `ReducedEffects`.

## 4 · Interactions with the rest of the game

| Meets | What happens |
|---|---|
| Dante (cannot be moved) | RAM stops on him and he does not move: the impact still plays (his body shakes), PRESSURE still fills |
| Molten Sean versus any knockback | Nothing moves him; the absorb splash plays, and the shover feels the recoil |
| Paete's Rooted players | Pushes are refused by the root as today; the reaction still plays so the hit is seen |
| Walls of ice, Dante's walls, props | They block MAGMA FIST and VOLCANIC BOMB like any wall |
| Nemu's Keeper standing the can | A hot can is only ever a downed can; the keeper waits like the taya does |
| A tagged Sean | Molten ends at once and drops nothing (a caught player leaves no bomb behind) |
| A round ending mid-Molten | Molten is cancelled and drops nothing (as built) |
| A rejoining player | Molten, the heat bars, the hot can's timer and an in-flight RAM come back from the snapshots |
| `ReducedEffects` | Lens splats, body shake and pad pulse off; rings and gameplay timers stay (they are information) |

## 5 · What changed from `plan.md`

- Sean's accent is molten gold `#C9A000`, not red (rule 4).
- Blasts throw everyone in their ring the written distance, and reach only players on their own level.
- The awakening blast and PRESSURE's splash never knock the can over; only the bomb does.
- MAGMA FIST resolves as the giant fist reaches the target's waist, 0.1 s after it arrives.
- A tagged Sean leaves no bomb.
- The E tile shows VOLCANIC BOMB with the ultimate's icon while Molten.
- The spectator director cuts to a thrown bomb's landing spot.

## 6 · Not covered here

Models, the painted look of each effect, and the sound recipes are the art and sound passes (handoff steps 3 and 5); this
document fixes what they must communicate. The rings, shakes and reactions here use first-pass shapes in the family's
colours until those passes replace them.

## 7 · What the audit of `191b41cde` found

1. No Sean hit announces through `MatchFlair`, so on a client nobody sees stars and the victim gets no freeze, punch or
   flash. Only the host saw the BONK popups.
2. The awakening blast, PRESSURE's splash and the bomb all used `HeroHazards.CreateExplosion`, which falls off to 35% at its
   edge and adds a fixed 5.5 m/s lift: "everyone thrown 4 m" was only true at the centre.
3. The same function knocks the can over for an attacking caster, so the awakening blast and an erupting shove next to the
   can scored a knockdown the plan never gave them.
4. Blasts and the pool measured flat distance only: a player on a roof above was thrown and slowed by a blast in the street.
5. The pool was a flat disc at one height (floats or sinks on a slope or step).
6. The hot can slid flat: off a kerb it hung in the air, and it could slide outside the playable square.
7. The taya's reset prompt simply disappeared while the can was hot, with no reason given.
8. The bomb flew straight through walls and roofs.
9. Clients never learned how many heat bars he has (`RestoreTimedKit` set one bar whenever any heat was left), so his fist
   glow could not be the same on every screen.
10. The RAM's wall probe could fire on a steep ramp's own surface.
11. The fist popped up and down when the court changed height, and chased players on other levels.
12. The spectator director only watches the first 3.4 s of an ultimate, so a bomb thrown seconds later was never shown.
13. Tests still asserted the old kit (SUPERNOVA, Steady Ember, protocol 175, two Molten Throw charges), and PRESSURE's tray
    line was one character over the limit.

## 8 · Build order

1. The hit reactions (rules 1, 6, 7): new `MatchFlair` kinds for Sean's hits, the victim's body shake, the pad pulse; the
   protocol goes to 177.
2. Sean's own blast resolver (rules 3, section 7 items 2 to 4), used by the awakening, PRESSURE and both bombs.
3. The court: the pool and every ring draped; the can's slide, the fist's path and the bomb's arc meeting the real ground
   and walls (items 5, 6, 8, 10, 11).
4. The warnings: the fist's target ring, the bomb's landing ring, the hot can's ring and TOO HOT prompt (item 7).
5. The colour (rule 4), the heat bars on every peer (item 9), the E tile, the tagged-Sean rule.
6. The spectator's bomb shot (item 12).
7. The tests (item 13), then EditMode and the PlayMode probes, then the film.
