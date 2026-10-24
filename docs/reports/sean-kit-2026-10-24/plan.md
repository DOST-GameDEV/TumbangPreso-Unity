# Sean rework: plan and direction (2026-10-24)

Status: PLAN, owner questions all answered 2026-10-24 (section 6). Nothing here is built; numbers the owner did not give stay PROPOSED. Research is in
`research.md` beside this file. How every skill reads and feels for Sean, the player hit, everyone else and a spectator, and how it meets the real court, is `feel-and-readability.md`; its section 5 lists what it changed here, and it wins where the two differ. Method: `docs/HERO_KIT_METHOD.md`. Network: `docs/SKILL_NETWORK_CONTRACT.md`.

## 0 · Who he is now

**Sean, born at the foot of Mayon in Albay.** He played his first games of tumbang preso on the slopes in the mountain's
shadow, then moved to San Fernando as a kid, where the Eskinita is still his home court (owner, 2026-10-24: keep the
Eskinita). He plays the way the mountain behaves: quiet for a long time, then all at once. Strength is his talent; the heat is his
mountain in him. A fictional sporting life: no eruptions that hurt anyone, no disaster lore (LORE.md, warm sporting world).

His old thread holds and gets sharper: *"waits for the line he wants, then commits completely"* is now pressure building and
being let go. Every skill has a held wind-up the others can read, then a heavy commit.

**Against the cast:** Dante can't be moved; Sean moves you (and only while Molten, for 8 s, can nothing move him). Dante is stone and standing still; Sean is molten and moving
forward. Amihan's Drift passes through a crowd; Sean picks one body and hits it. Paete takes slippers; Sean leaves them alone.

**Cut completely:** the parol and everything of it (cutscene, `parol`, `parol_small` and `duskline` props, `ParolGround`
shader), Stoke Step, Empowered Throw, Cinder Gate, Steady Ember, Supernova. Saved ids `sean_skill1`, `sean_skill2`,
`sean_skill2d`, `sean_ultimate` are kept so loadouts still load; only what they do and their names change.

**The look family, kept apart from Dante's** (written before any effect, Paete's rule). The owner, 2026-10-24: Dante keeps
his magma. So both heroes have lava, and they must never read alike. Dante's lava belongs to the GROUND: stone with lava
trapped inside it (`DanteMountainArmour`, `DanteDriftLive`, `HeroIntroductionScene.Dante`). Sean's lava belongs to his
BODY: it is still liquid, and it never stops moving.

| | Dante (in the game now) | Sean |
|---|---|---|
| What it is | stone with lava trapped in it | lava that is still liquid |
| Shapes | square slabs, hexagonal basalt columns, straight cracks, boulder fists | round: beads, drips, ropes and coils (the folded skin of flowing lava), splashes, bubbles |
| Motion | heaves up, then holds still | never still: it runs, drips, splashes and bubbles |
| Where it comes from | up out of the court | out of his own fists and arms, then onto the court |
| Colour | orange (`1, .42, .08`) glowing in grey-brown stone | hotter: a gold-yellow core (about `#FFC83A`) running to cherry red (about `#D8261C`) at the edges, with no flat orange |
| When it cools | matte grey-brown basalt; brown dust | glossy black volcanic glass with a red glint, which cracks and sheds; white steam and grey ash, never dust |
| Big shapes | the basalt titan, slab bands, a bulk suit of columns | the Mayon cone; a lava fountain; a ball of lava, never a rock |
| Sound | stone grinding and cracking | bubbling, wet splats, sizzle, steam hiss |

Rules that follow from it:
1. **Nothing of Sean's is rock until it has cooled.** No slabs, no columns, no hexagons, no glowing seams cut in grey stone.
2. **The glow lives in the liquid itself**, never in a crack. Never white: the hottest colour is the gold-yellow core.
3. **No armour.** Dante's power form adds a suit of stone; Sean's adds nothing to his body except lava on his fists and
   forearms. His silhouette stays his own, and his fists get bigger.
4. **Things FLOW along a path** (they run, drip, pour); they are never scaled up out of nothing. Nothing appears from empty
   air: lava comes out of him first.
5. **Everything ends by cooling** to glossy black glass, which cracks, sheds and steams away.

## 1 · The kit (all numbers proposed; distances written, speeds solved from `Friction` 30)

| Slot (kept id) | Name | What it does |
|---|---|---|
| Passive | **PRESSURE** | Every body hit he lands (shove, punch, LAVA RAM, MAGMA FIST) adds one bar of heat; three bars is full and his fists glow. When full, his next shove or LAVA RAM **erupts**: the target goes 40% farther (never past the 4.27 m knockback cap) and a 1.6 m splash pushes anyone beside them 1.5 m. Heat cools away after 6 s without contact. Replaces Steady Ember on the existing timed-kit passive field |
| Q, both roles (`sean_skill1`) | **LAVA RAM** | Hold to brace (0.25 s minimum, the tell), release to charge 4.0 m straight along his aim (15.5 m/s). He stops dead on the **first** body: that one rival is launched 3.0 m (13.4 m/s; 4.2 m erupted, under the 16 m/s knockback cap) and stumbles 0.8 s. Hitting a wall or prop stuns HIM 0.6 s. No trail. Cooldown 14 s |
| E, attacking (`sean_skill2`) | **MOLTEN THROW** | Coat the held slipper in lava for one throw within 8 s. It flies flat and heavy: no curve, no dip. If it knocks the lata over, the can is **blasted 3 m along the throw** and lands **hot**: the taya cannot start the reset for 1.5 s while it cools (it hisses and darkens). Cooldown 30 s |
| E, defending (`sean_skill2d`) | **MAGMA FIST** | Hold to lock on the rival nearest his aim within 9 m (a 30 degree cone; their feet show the mark), release to punch: a fist of lava flies from his knuckles low over the court at 12 m/s, curving toward them at up to 90 degrees per second. Where it reaches them it sinks into a puddle under their feet and a giant fist **punches up out of it** (the Puddle Punch): knocked up 1.0 m and pushed 3.5 m directly away from the lata. Walls stop it; a sidestep at the last moment beats the curve. Cooldown 22 s |
| Ultimate (`sean_ultimate`) | **MAYON AWAKENS** | The cutscene, then the awakening blast (everyone within 4.0 m pushed 3.5 m), then he is **Molten** for 8 s: no knockback at all (owner's pick), PRESSURE stays full so every shove and RAM erupts, LAVA RAM cooldown 4 s. While Molten his E button becomes **VOLCANIC BOMB** (the Lapu-Lapu lesson): he pulls the lava off both arms into one heavy ball between his fists and hurls it up to 14 m; it splashes 4.8 m wide, throws everyone 4 m, knocks the lata over (never his own when he is taya), and leaves a pool that skins over into black glass for 4 s and slows by 30%. Throwing it ends Molten. If he never throws it, it drops at his feet as the 8 s run out and splashes there, smaller: 3.0 m wide, everyone thrown 3 m (owner's pick). Cost 15 charges, as now |

Why each earns its slot:
- **PRESSURE** makes contact the resource: a brawler who stays out of reach is half a brawler.
- **LAVA RAM** bets on one target, and the wall stun is the price of missing.
- **MOLTEN THROW** speaks the game's own sentence, "the whole game is the run back in": a can that flies farther and stays
  hot buys every attacker time. No other hero touches the reset.
- **MAGMA FIST** is the taya's version of a punch: control instead of a free tag. Pushing them AWAY FROM THE CAN makes them
  run the danger box again; their slipper stays in their hand, so it never copies Paete or Amihan.
- **MAYON AWAKENS** is both things the owner asked for in one: the state, then the bomb he chooses when to throw.

## 2 · The six beats per ability (Tell, Release, Travel, Contact, Linger, Dissipate)

| Ability | Tell | Release | Travel | Contact | Linger | Dissipate |
|---|---|---|---|---|---|---|
| LAVA RAM | shoulder dropped, back foot dug in; lava beads and bubbles on his leading shoulder; a low bubbling rumble | lava kicked off his back heel in a splash; a thump | the lava on his shoulder streams back off him in ropes; heat haze behind | a wet splat and a boom; the target flung with a spray of lava drops at the contact point; on a wall, he rebounds and sits dazed under a ring of steam | 0.3 s of glowing liquid footprints | the prints skin over into black glass, crack and steam away |
| MOLTEN THROW | his hand closes on the slipper and lava seeps between his knuckles over it; sizzle | the throw with a heavier shoulder; the slipper trails drips and smoke | a flat line, drips falling and hissing on the court | the can is hit with a crack of hot metal and flies, spinning, trailing smoke | the can lies glowing orange, hissing; a small ring of heat on the court round it | the can darkens through dull red to its own colour, with a last hiss and a puff of steam |
| MAGMA FIST | arm pulled back, a fist-sized glob swelling on the knuckles; the target's feet show the mark | the punch at the air; the glob leaves the knuckles as a fist | it skims low, a glowing fist dripping as it curves | it sinks into a puddle under them; a giant blocky fist bursts up (0.25 s from puddle to full height) and they go up and away | the fist holds at the top 0.2 s, knuckles glowing | it skins over into glossy black glass, cracks, and slumps back into the puddle, which steams and dries to a dark stain |
| VOLCANIC BOMB | he claps his fists together over his head and the lava runs off both arms into one ball between them, swelling and bubbling; the aim ring shows where it will land | the ball (1.2 m), held overhead 0.2 s, then hurled; his arms are bare again at once | a high arc for 0.9 s, wobbling like liquid, shedding drips and a ribbon of steam; its shadow grows on the spot | a splash: a crown of lava thrown up and out, a ring, the small Mayon cone of light (the emblem) | the pool glowing and bubbling for 4 s, skinning over from its edge inward | the skin goes glossy black, cracks into plates and steams away |

## 3 · MAYON AWAKENS: the cutscene, directed

**One sentence:** the mountain's heat comes up out of the ground into him until he cannot hold it, and he lets it out.

**One travelling thing: the heat.** It always travels UP the screen: through the court into his feet, up his legs and arms
into his fist and eyes, then out of the court as the eruption. Every cut continues that upward line.

**The emblem: Mayon's cone.** A near-perfect cone with concave sides and a lit summit. It appears three times, each where the
power is: on the horizon behind him (the mountain wakes), in his eyes as the glint (small, the "now"), and as the shape of the
eruption's column (the payoff).

**Length 7.0 s**, inside `UltimatePerformance.MaxSeconds`. The setup is fast, the fist and the slam are held, the ending is
the take. The match clock is frozen throughout (shared plumbing).

**Grade:** the court dims to 0.70 and warms from 0.00 to 0.45 s and holds; daylight returns from 6.60 to 7.00. The mountain
backdrop exists only in the cutscene's render copy.

### Shot A, THE MOUNTAIN WAKES (0.00 to 2.10)

Lens: ankle height, 1.2 m in front of him and to his left, looking at his feet, then a slow crane up his body.

| Time | Picture | Sound | Answers |
|---|---|---|---|
| 0.00 | Opens on the element already in the air: ash flakes and embers drifting past the lens, two of them at the lens itself. Thin orange cracks already running across the court toward his tsinelas | a cut-in accent: a deep thump and a crackle swelling in | Genshin open-on-element, Mavuika |
| 0.45 | The cracks reach his feet; his toes press into the court; the glow climbs his shins as veins | a second thump, lower | the heat travels up |
| 0.45 to 1.60 | The lens cranes up from 0.2 m to 1.6 m following the glow up his body. Over his shoulder the horizon reveals Mayon's cone in silhouette; its summit lights as the glow reaches his chest (1.40) | the rumble rises; the mountain's tone (a low sustained note with a steam whistle on top) when the summit lights | the backdrop steps back; the emblem, first time |
| 1.60 to 2.10 | Held: he breathes in, shoulders rising, lava starting to bead and run on his collarbones. 0.5 s still | the rumble holds, a long inhale | held pose, Xrd |

### Shot B, THE FIST (2.10 to 4.30)

Lens: macro, low and tilted 12 degrees (dutch), his right fist big in the right third of the frame, his face soft behind it.

| Time | Picture | Sound | Answers |
|---|---|---|---|
| 2.10 | Cut. The fist, close | the rumble ducks under | Wriothesley, Lighter |
| 2.25 | He clenches harder: a knuckle crack | two sharp knuckle cracks | the muscle beat |
| 2.40 to 3.20 | Lava wells up out of his knuckles and runs UP his forearm in rivulets that braid round it (against gravity: the mountain is pulling it up through him); his fist swells into a dripping lava fist; each drip hisses on the court | a thick bubbling per rivulet; sizzles per drip | things flow along a path, never scale up; liquid, so never Dante's stone |
| 3.20 to 3.45 | The lens slides 20 degrees round the fist and focus moves from the fist to his face | a rising swell | fist in front, face behind |
| 3.45 | His eyes ignite: a four-pointed glint on each eye, orange | a bright hot-metal ting; the mountain's tone, short | Koleda's eye glint; the emblem as the glint |
| 3.60 to 4.10 | Held, the hero frame: fist forward, face behind, steam leaving his mouth | a steam exhale | the pose to remember |
| 4.10 to 4.30 | He pulls the fist back out of frame; for 0.2 s the frame holds only embers | the swell drawn in hard | anticipation |

### Shot C, ERUPTION (4.30 to 7.00)

Lens: wide and low, 3.5 m away at knee height, looking UP at him 15 degrees. Mayon's cone huge behind him. The real other
players are staged where they stand (`StageOthers`).

| Time | Picture | Sound | Answers |
|---|---|---|---|
| 4.30 to 4.80 | He drops into a squat, both fists drawn back above his head | the swell keeps climbing | Zangief, strength from below |
| 4.80 to 5.05 | Held at the top of the coil, 0.25 s | the swell at its peak | anticipation held |
| 5.05 | **SLAM.** Both fists into the court. Two frames of a slanted orange halftone flash panel. The court ring bursts; a fountain of liquid lava shoots up behind him, its spray filling exactly the mountain's cone silhouette (the emblem, third time); a shockwave ring races out over the court | the swell cut dead into the impact: a deep wet thud and a low boom, then the patter of falling drops | Varesa's panel, Lighter's pillar, Dehya's crest |
| 5.05 to 6.20 | **THE TAKE.** The lens rides the shockwave out to the nearest real player inside the 4.0 m blast and ends beside them (2.4 m off, 50 degrees round from the shockwave's line), so they are flung past the lens, never through it; they play their shock clip. Then it swings wide to frame every player inside the blast being thrown | each thrown player: a thud and a short scrape | Paete's ending rule |
| 6.20 to 6.60 | Settle on Sean rising out of the squat, Molten: both fists and forearms are liquid lava, oversized and dripping, with steam rolling off his shoulders and ash falling. He rolls one shoulder and holds | the mountain's tone, long; crackle under it | the state, shown |
| 6.60 to 7.00 | Daylight returns; the mountain sinks out of the sky. Hand-back: his screen goes into his own eyes (`HandBackView`), everyone else cuts to their own first person | the tail: a settling rumble | truthful hand-back |

**Nobody in reach:** the ride goes out to the edge of the ring and holds on the glowing cracks, then settles. Never invent a
target.

**Never shown twice:** the cutscene shows the awakening blast landing, so play starts from that end state: the blast resolves
as the cutscene ends and is not replayed (`IntroductionIsTheWindup`). The bomb is NOT in the cutscene; it is his choice later.

**Density pass kinds** (typed tables, Paete's `PaeteBurst.cs` as the pattern, never its rows): embers and ash on every beat;
rising motes up his body in shot A; four-pointed glints on the eyes and the summit; shockwave rings at the slam (one big,
two thin); rays behind the cone column; a curtain of rising streaks round him as he stands Molten; a near layer of ash at the
lens in every shot; a veil sinking the frame edges into his dark orange.

## 4 · Molten in play (after the hand-back)

- Nothing moves him: shoves, blasts and other heroes' knockback leave him where he stands. His feet stay planted, and the
  lava on his fists splashes with every hit he takes, so the immunity can be seen.

- His body: both fists and forearms are liquid lava, oversized, bubbling and dripping; steam rolls off his shoulders. No
  armour and nothing else on him changes (rule 3 of the look family). His first-person arms are the same two lava fists.
- His HUD: the ultimate card becomes VOLCANIC BOMB with an 8 s ring draining.
- Others read it at a glance: a heat shimmer above him and the glow in his seams; no change to the court's light.
- The end: when the bomb leaves his hands, or when the 8 s end, the lava on his arms skins over into black glass, cracks and sheds over 0.6 s.

## 5 · What gets touched (from `HERO_KIT_METHOD.md` section 4)

- Core: `SeanRules.cs` (new, all numbers above with tests), retire `SeanGateRules`, `EmpoweredThrowRules`; `StatusRules` only if
  "stumble" or "hot can" needs a new status; `HeroLoadout`, `HeroLines` text.
- Kit: `SeanHeroKit.cs` rewritten (four abilities and the passive); `SeanCinderGate.cs` removed; new host-resolved world objects
  for MAGMA FIST's fist, the hot can and the glass-skinned pool, each with a `WorldEffectSnapshot` kind and a rejoin probe.
- Network: the Molten state rides `ITimedKitReplication` (the field Empowered Throw used); MAGMA FIST's flight is
  host-confirmed; a protocol bump for the changed rules, with its test.
- Presentation: one body clip and one first-person action per ability; the lava fists on body and FPP arms; icons and glyphs.
- Cutscene: `HeroIntroductionScene.Sean.cs` rewritten (the parol file goes to `tools/cutscene_rework/old/`); `sean()` in
  `tools/author_ultimate_intros.py`; the mountain backdrop and the lava ball modelled in the painted kit
  (`tools/hand_companion_kit.py`, never prisms).
- Sound: one recipe per cue (instruments: sub rumble, crackle grains, sizzle, bubbling, wet splats, knuckle crack, steam hiss, boom;
  the mountain's tone as the motif).
- Bots: when to RAM, when to MAGMA FIST a retriever, where to throw the bomb.
- Docs: `LORE.md` and `docs/CHARACTER_ORIGINS.md` (Albay, the foot of Mayon), `HERO_KIT_REWORK_DECISIONS.md`, `TODO.md`.

## 6 · Questions for the owner (one batch)

1. ~~Dante's magma.~~ **Answered 2026-10-24: "Keep Dante's magma. Find another way to keep Sean and Dante visually
   distinct."** Dante is unchanged. Sean is kept apart by the look family in section 0: liquid instead of stone, round
   instead of square, gold-to-red instead of orange, black glass instead of basalt, no armour, and the bomb is a ball of
   lava rather than a rock.
2. ~~Molten's weight.~~ **Answered: no knockback** for the 8 s of Molten. Dante stays distinct because his is permanent
   (Earthbound) and comes with stone armour; Sean's lasts 8 s and is bare lava fists.
3. ~~The unused bomb.~~ **Answered: it drops at his feet** as Molten ends and splashes there, smaller (3.0 m, 3 m throw).
4. ~~Length.~~ **Answered: 7.0 s**, as directed in section 3.
5. ~~PRESSURE.~~ **Answered: keep PRESSURE** as written.
6. ~~Home court.~~ **Answered: keep the Eskinita.** Born near Mayon, moved to San Fernando as a kid; `LORE.md` and
   `docs/CHARACTER_ORIGINS.md` get his birthplace, and the Eskinita stays his court.

All six questions are answered. Step 4 is done (2026-10-24): `Packages/com.tumbangpreso.core/Runtime/SeanRules.cs` holds every number above and `Core.Tests/SeanRulesTests.cs` asserts them (837 core tests pass). Step 5 is done too (2026-10-24): `SeanHeroKit`, `SeanMagmaFist`, `SeanVolcanicBomb`, the hot can in `Lata`, PRESSURE in `CombatVerbs`, protocol 176. Two rules changed on the way: VOLCANIC BOMB is the E press while Molten (the ultimate's button belongs to the shared cutscene path, which has no second press), and the hot can slides 3 m (an ordinary knockdown never moved the can, so "twice as far" had nothing to double). Compiled outside Unity; not yet played or filmed. The MAYON AWAKENS cutscene replaced the parol the same day as blocking: the table in `tools/author_ultimate_intros.py` (`_sean`, 7.0 s, four camera moves) and the stage in `HeroIntroductionScene.Sean.cs` (the old one is in `tools/cutscene_rework/old/`). One change to section 3: it ENDS as the shockwave reaches the real players, and play's awakening blast throws them as it hands back, so the throw is seen once; the staged players only brace and lean. Not filmed yet. Next: film it, then the model and effects.
