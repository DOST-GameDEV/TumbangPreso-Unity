# Tumbang Preso: the whole game, in one file

**What this is.** A complete reference to what the game IS: every mode, every rule, every verb,
every number a player can feel, and all fifteen hero powers. Written 2026-08-26 from the code and
from `Design.md`, not from memory.

**What this is not.** It is not the balance source of truth. ⚠️ **`docs/Design.md` is**, and its
opening line is the rule: *a number in the code must match a number here, or one of the two is a
bug.* This file is a reader's map. Where it disagrees with `Design.md` or with `Balance.cs`, they
win and this file is the thing to fix.

Read [`VISION.md`](VISION.md) for WHY any of it is like this. Read
[`Hero_Strike_Balance.md`](Hero_Strike_Balance.md) for the per-ability derivations. Read
[`TODO.md`](TODO.md) for what is open.

---

## 1 · The street game it comes from

Tumbang preso is played in Philippine streets with an empty tin can (the *lata*), a slipper each
(*tsinelas*), and one kid who is *taya*: the guard. Everyone else throws their slipper at the can
from behind a line. Knock it over and the taya has to stand it back up; while it is down, you can
run in and get your slipper back. Get caught inside with your slipper in your hand and you are
the taya.

**The whole game is the run back in.** Throwing is free. Retrieving is the only moment you can be
caught. `VISION.md` § 0 states it as the sentence every rule protects.

---

## 2 · The shape of a match

| | Classic | Hero Strike |
|---|---|---|
| Players | 4, free for all | 4, free for all |
| Rounds | **4**, one full rotation | **8**, two full rotations |
| Round length | 90 s | 90 s |
| Roster | the twelve street characters + 3 custom player slots | the six heroes |
| Powers | **none, and that is the feature** | two skills and an ultimate each |
| Governed by | `Design.md` | `Design.md` § 13 points at the files |

### 2.1 Roster Integrity & The Custom Character Creator
- **Canonical Heroes & Classic Street Characters**: Berto, Sean, Dante, Cheska, Zack, Nemu, Phaister, etc. keep their canonical skin tones, facial features, and visual identity intact. No global hue-shifting or alien tint sliders are applied to named characters.
- **Dedicated "Create Your Own Character" Slot**: Features **3 save slots** (Custom 1, 2, 3) where players can fully customize their own street kid avatar (facial expressions, natural Filipino skin tone palette, height, body size, hair style/color, streetwear, accessories, custom tsinelas, and custom lata). One active custom character is chosen for play.

Four seats. One is the **taya** (defender); the other three are **attackers**. The taya rotates
clockwise every round, derived as `(round - 1) % 4`, so everyone defends the same number of times
by construction rather than by bookkeeping. Score is **cumulative across the match** and there is
no reset between rounds.

⚠️ **Both modes ship and neither is a variant of the other.** Classic is not Hero Strike with the
powers switched off; it is the whole street game, for players who want less happening on screen.

Between rounds: a 3 s intermission, then a 15 s warm-up buffer before the whistle.

---

## 3 · Scoring

| Event | Points | To whom |
|---|---|---|
| Knock the lata over | **+100** | the thrower |
| Tag an attacker holding their tsinelas inside the box | **+100** | the taya |
| Sabotage: knock the lata while the taya is mid-reset | **+50** | the thrower |
| Passive defence, while the lata stands | **+10 per second** | the taya |
| Taya camping on the can | **-5 per second** | the taya |
| Leaving your tsinelas unretrieved | **-5 per second** | the attacker |

**The two penalties are what stop the two ways of not playing.**

* **Camping.** A taya who never leaves the can collects +10/s for nothing. Standing within
  **2.2 m** of the lata for more than **3 s** starts a warning; after a **5 s** grace the penalty
  ticks every second until they step outside **2.8 m**.
* **Sitting on a thrown slipper.** An attacker who throws and never goes back in has taken no
  risk at all. A tsinelas left loose for **7 s** warns, and after a **10 s** grace it costs
  **-5/s** until it is picked up.

⚠️ **Passive defence pays 900 a round uncontested against 100 for a knockdown.** That is a known
balance tension, written into `Design.md` rather than quietly tuned away: the attackers' answer
is that a knocked-over lata stops the tick entirely.

---

## 4 · What a player can do

Eight verbs, shared by both modes, and one contextual key that does three jobs.

| Verb | Default | What it does |
|---|---|---|
| Move | WASD | 4.6 m/s as the taya; attackers move at **0.75x** of that |
| Sprint | Left Shift | **1.5x** speed while stamina lasts |
| Jump | Space | 5.8 m/s up. Also the **mash** that gets you off the floor |
| Throw / Punch | Left mouse | attacker: hold to charge, release to throw. taya: a stationary tag |
| Pick up / Shove / Reset | X | contextual, see below |
| Lunge tag | Right mouse | the taya's dash tag: hold to charge, release to sweep |
| Curve left / right | Z / C, or the mouse wheel | bends a charged throw (*pektus*) |
| Emote wheel | (bound in settings) | local flourish; ends only when interrupted |

**The contextual key is one control doing three jobs**, chosen by the world rather than by a
modifier: a tap with a tsinelas within **1.75 m** picks it up; a tap with nothing in reach is a
**shove**; a hold as the taya inside the lata's **1.6 m** ring runs the **1.5 s reset channel**
that stands the can back up.

⚠️ **One control, one action, in the input map.** The settings panel refuses a key another action
already holds, and a test asserts no two actions share a control.

### 4.1 Stamina

**60** units, drained at **40/s** while sprinting and regenerated at **20/s** after a **1 s**
delay. Sprinting is refused below **7.5**. Emptying the bar costs **2 s of fatigue** at **0.75x**
speed, which is the punishment for holding sprint rather than spending it.

A full bar is about 2.2 s of sprint: roughly one crossing of the danger box.

### 4.2 Melee

| | Range | Arc | Cost | Cooldown |
|---|---|---|---|---|
| Shove | 1.6 m | 70° | 25 stamina | 7.5 s (2.0 s on a miss) |
| Punch (taya only) | 1.7 m | 75° | none | 0.9 s |
| Lunge (taya only) | 1.3 m tag radius | dash | none | 1.5 s |

A shove throws a body at **12.2 m/s** with a **1.25 s** stagger. The lunge charges for **0.5 s**
and stays live for **0.45 s** after release.

⚠️ **Contact resolves by DISTANCE on the host, never by a trigger volume.** 16 of 36 overlaps were
measured failing to land, and that measurement is why the whole combat layer is engine-free.

### 4.3 Throwing, and the pektus curve

Charge for up to **2.5 s**; a release under **35 %** power is refused. A tsinelas leaves at
**18.5 m/s** on a 45° launch, and the skin you picked scales that. After a pickup there is a
**1.25 s** throw lock, so a retrieval cannot be converted into a throw on the same step.

**The pektus curve is the skill ceiling of the throw.** Hold the curve keys (or the wheel) while
charging to spin the tsinelas; spin bends the flight in the air at **14 m/s²** of lateral
acceleration, and a spin above **0.55** lets the shoe **bank once off a wall and still score**.
Exactly one bank counts.

### 4.4 Getting knocked down

Street hazards and several powers put you on the road. ⚠️ **As of 2026-08-26 the mash is the only
thing that ends a fall.**

* A trip is **2.5 s** long, of which **2.15 s** can be bought back by pressing Jump.
* Each accepted press buys **0.22 s**, rate-capped at **10 Hz** so a turbo mouse cannot beat a
  hand: about **10 presses**, roughly 1 s of hammering.
* The last **0.35 s** is the get-up animation and nothing can shorten it.
* Nothing else runs the fall down. A player who presses nothing lies there until the **5 s**
  stranding guard releases them, and the guard fills the meter on the way out so **you never
  stand up with the bar part-full**.
* After you are up, **1.2 s** of immunity stops the hazard you are standing on from re-tripping
  you.

---

## 5 · The arena

The danger box is a **SQUARE**, `CONFINEMENT_RADIUS` **7.0**, so **14 m by 14 m = 196 m²**. X and
Z clamp independently, which is why the corner is 2.9 m further from the middle than the edge is,
and that corner is exactly where a taya moves to cover a lane.

⚠️ **The taya is clamped inside the box and the attackers are not.** Everything the defender does
happens in 196 m²; the attackers own the street around it.

Three maps ship:

* **Eskinita**, the alley. The default, and the map every recorded probe number was measured on.
* **Bayan Plaza**, the town square, with a monument the defender has to play around.
* **Ilalim ng Tulay**, under the LRT bridge, and the only map with a mechanic of its own: a
  train passes overhead every **24 s** for **2.70 s**, and while it does, hero cooldowns advance
  at **3.5x**, worth a flat **6.75 s** of cooldown per pass. It also carries eight pillar hazards
  and the street trip hazards.

---

## 6 · Hero Strike

Six heroes, each with two skills and an ultimate. Everything in § 4 still applies: the kit is
added to the street game, not a replacement for it.

### 6.1 How a power is paid for

Two economies, deliberately different:

* **Skills** are either on a **cooldown** (30 to 45 s, which is 2 to 3 casts a round) or on
  **charges** (usually 2, some earned back by an act).
* **Ultimates** are bought with a meter that fills to **100** by PLAYING, never by waiting:

| Act | Charge |
|---|---|
| Knock the lata over | **+25** |
| Tag an attacker | **+20** |
| Retrieve your own tsinelas | **+12** |
| A legal throw | **+4** |

⚠️⚠️ **There is no passive charge and there must never be one again.** A passive 1.0/s handed a
player who did nothing 90 of the 100 in a 90 s round, which is `VISION.md` § 4's "nothing may
reward waiting" broken in one constant. Retrieval pays because retrieval is the game.

⚠️ **The overclock window does not touch the ultimate meter**, for the same reason: a window that
filled it would be a reason to stand still.

### 6.2 The five kits

⚠️ Cooldowns and costs below are the shipped values as of 2026-08-26 and several are **starting
positions with reasoning attached rather than measured results**. `TODO.md` § 0 and § 16 are the
open measurement.

#### SEAN, fire. Ultimate costs **130**.

| Slot | Name | Cost | What it does |
|---|---|---|---|
| Skill 1 | **FLAME RUSH** | 34 s | Rushes forward in a line of fire. Anyone you run through is knocked down; the trail burns whoever follows |
| Skill 2 | **IGNITION CANNON** | 2 charges, +1 per lata knocked | Loads your next throw with fire, so it explodes where it lands and a near miss still counts |
| Ultimate | **SUPERNOVA** | 130 | Launches you up and slams you down. The blast knocks the lata over and throws everyone near it away |

Supernova is the only ultimate that converts directly into a point, which is why it is priced
above Dante's and below Zack's.

#### ZACK, lightning. Ultimate costs **150**.

| Slot | Name | Cost | What it does |
|---|---|---|---|
| Skill 1 | **BOLT SPRINT** | 30 s | Faster movement, and the trail behind you shocks anyone chasing |
| Skill 2 | **STATIC CHARGE** | 2 charges, +1 per lata knocked | Your next throw flies much faster and flatter and jolts whoever is standing where it lands |
| Ultimate | **THUNDERSTRIKE** | 150 | Lightning on your position. Everyone underneath is stunned where they stand |

The most expensive ultimate in the game, because a stun on everyone near the can is the strongest
opening in it.

#### CHESKA, ice. Ultimate costs **140**.

| Slot | Name | Cost | What it does |
|---|---|---|---|
| Skill 1 | **PERMAFROST SHEET** | 2 charges | Freezes a patch of court. Anyone crossing it loses their footing and slides |
| Skill 2 | **ICE BARRICADE** | 1 charge, **recharged by retrieving your own tsinelas** | Three ice pillars. Bodies and thrown tsinelas both stop at them, so the lata gets time |
| Ultimate | **GLACIAL NOVA** | 140 | Freezes everyone near you and blows the loose tsinelas away |

⚠️ The barricade's recharge is the one place in the game where a POWER is paid for by doing the
thing the game is about.

#### DANTE, stone and magma. Ultimate costs **110**.

| Slot | Name | Cost | What it does |
|---|---|---|---|
| Skill 1 | **SEISMIC STOMP** | 2 charges | Slams the ground. Shoves nearby players off their feet and kicks loose tsinelas out of reach |
| Skill 2 | **DEMONIC CARAPACE** | 45 s, lasts 4 s | Nothing can stun, shove or slip you, so you can walk in and take what you need |
| Ultimate | **TITAN FISSURE** | 110 | Splits the court ahead. Everyone in the crack is thrown up and left dizzy |

⚠️ The longest cooldown in the game is on Carapace, deliberately: a power that ignores the game's
central risk waits longest.

⚠️ **Dante's accent colour is JADE, not orange.** His kit is magma and orange is the colour
`Art_Direction.md` spends on the attacker ROLE, so the hero cannot have it; the fissure light,
embers and magma core stay hot.

#### NEMU, spirit. Ultimate costs **90** and it is the cheapest in the game.

| Slot | Name | Cost | What it does |
|---|---|---|---|
| Skill 1 | **GHOST STEP** | 36 s | Part ghost: faster, and the taya cannot tag you. **Picking up a tsinelas ends it early** |
| Skill 2 | **ASTRAL PROJECTION** | 2 charges | Send Kuro, your spirit pet, ahead; possess him, then press again to teleport your body to him |
| Ultimate | **SEANCE VOID** | 90 | A vortex that drags players and loose tsinelas in and slows anyone inside |

⚠️ Ghost Step ending on pickup is the whole design of it: it is a power for getting IN, not for
walking out with the prize.

Nemu is the only hero with a companion. Kuro is his own object in the world, follows with spring
lag, and is the thing Astral Projection possesses.

### 6.3 The readability budget

⚠️⚠️ **"More stuff" is the point of Hero Strike and "unreadable" is its failure mode.** In a
14 by 14 box shared by four players, one lata, four tsinelas and up to twelve live effects:

1. A skill's floor footprint should be about **1.8 to 2.5 m of radius**, 3 to 8 % of the box.
2. An ultimate may be big. **One at a time.**
3. Spend the budget on **DETAIL, not AREA**.
4. Cap what can overlap.
5. **A screenshot taken mid-fight must still show the lata, the chalk and every player.**

Rule 5 is a gate, not an opinion: `AbilityShowcaseProbe` fails a run where one effect blows more
than **12 %** of the frame to white. Zack's Thunderstrike once read **62.8 %**.

---

## 6a · The cast

Every playable character carries three traits, each **1 to 5**, with **3 as exactly neutral**.
⚠️ **A point is not worth the same in every column**: BILIS is 5 % per point, LAKAS and TATAG are
7 %. Neutral is 1.0x by construction, which is what makes "no pick", an AI seat, a peer on an
older build and entry 0 all play the same game.

* **BILIS** (speed) scales movement.
* **LAKAS** (power) scales what you push: the shove, the throw's impact.
* **TATAG** (grit) scales how hard you are to move: stagger and knockback resist.

### The twelve street characters, for Classic

| Character | BILIS | LAKAS | TATAG | Plays like |
|---|---|---|---|---|
| **BERTO** | 2 | 4 | 5 | The immovable taya. Slow, hard to shift, punishing at the can |
| **MARING** | 5 | 2 | 2 | Pure runner. In and out before the tag lands |
| **TOTOY** | 5 | 2 | 3 | The same speed with a little more spine |
| **INDAY** | 3 | 4 | 4 | The all-rounder with no weak column |
| **KUYA BOY** | 3 | 5 | 3 | The heaviest hitter in the game |
| **ATE GIRLIE** | 4 | 3 | 3 | Quick and even |
| **TIKBOY** | 4 | 4 | 2 | Fast and strong, and goes down easily |
| **BEBANG** | 2 | 5 | 5 | The wall. Nothing moves her and everything she touches moves |
| **JUN-JUN** | 5 | 1 | 2 | The fastest legs and the weakest arms |
| **LOLA PACING** | 1 | 4 | 5 | Slowest in the game, and immovable with it |
| **MANG KANOR** | 5 | 3 | 2 | Fast and average, thin |
| **ALING NENA** | 2 | 3 | 5 | Slow and very hard to knock down |

⚠️ **No character has all three.** Every row spends what it takes somewhere else, and the two 5-5
rows (BEBANG, BERTO, LOLA PACING) all pay for it in BILIS.

### The six heroes, for Hero Strike

| Hero | BILIS | LAKAS | TATAG | Element | Ultimate cost |
|---|---|---|---|---|---|
| **DANTE** | 2 | 4 | 5 | stone and magma | 110 |
| **CHESKA** | 3 | 4 | 4 | ice | 140 |
| **SEAN** | 3 | 5 | 3 | fire | 130 |
| **ZACK** | 4 | 3 | 3 | lightning | 150 |
| **NEMU** | 4 | 3 | 4 | spirit | 90 |

Their kits are § 6.2. ⚠️ The trait rows are deliberately the same shape as street characters:
the kit is what makes a hero, not a better stat line. Dante is BERTO's row, Cheska is INDAY's,
Sean is KUYA BOY's.

### The four lata

The can you pick is the taya's equipment. ⚠️ **RESET / REBOUND / STANCE are three routes to one
goal**, which is keeping the can upright, and each owns exactly one 5 so that no can is the
correct answer.

| Lata | RESET | REBOUND | STANCE | What it does |
|---|---|---|---|---|
| **PASIP** | 5 | 1 | 1 | Tall and empty: topples easily and is back up instantly |
| **BOYBEN** | 1 | 3 | 5 | Squat and half full: barely moves, and a job to stand up |
| **DECADES TUNA** | 4 | 1 | 4 | A flat disc: hard to tip, quick to right, no mass to rebound |
| **KALAWANG** | 2 | 5 | 3 | Solid and ribbed: punishes the throw that hits it |

* **RESET** divides the 1.5 s reset channel.
* **REBOUND** is how far it throws back the tsinelas that hit it.
* **STANCE** divides the hit margin, so a high-stance can refuses glancing knockdowns.

### The ten tsinelas

| Tsinelas | FLIGHT | IMPACT | RECOVERY | What it does |
|---|---|---|---|---|
| **TSINELAS** | 3 | 3 | 3 | The plain flip-flop. Neutral, and the fallback for every unpicked seat |
| **CROCS** | 2 | 5 | 2 | Heavy: slowest flight, hardest body-block, longest throw lock |
| **PANTULOG** | 3 | 1 | 5 | The house slipper: no impact, armed again fastest after a pickup |
| **IKE** | 4 | 2 | 3 | The sports slide: fastest in the air |

* **FLIGHT** multiplies the 18.5 m/s launch. ⚠️ **It is the narrowest stat in the game, 2 to 4**,
  because the AI inverts the range equation against `LaunchSpeed` to decide how long to charge:
  a wider spread would make every bot holding a slow slipper fall short, and that reads as an AI
  regression rather than as a balance change.
* **IMPACT** multiplies the push a body-block deals.
* **RECOVERY** divides the 1.25 s throw lock after a pickup.

⚠️ **Entry 0 of every prop list stays neutral**, because it is what an unpicked prop wears.

---

## 7 · Classic's own depth

Classic gets no powers, ever. What it gets instead is **Street Hype**: a bottom-of-screen readout
that names skilled play as it happens (a curved throw, a bank, a close call, a block) without
changing a single point. That is the pattern for anything Classic is ever given.

---

## 8 · What the screen tells you

| Where | What |
|---|---|
| Top centre | round clock, round number, the taya's name |
| Left | the scoreboard, your own row in cream |
| Bottom centre | the ability deck (Hero Strike) or Street Hype (Classic) |
| Bottom left | your role card |
| Bottom right | the lata's state and the current objective |
| Centre | the crosshair, for **every** seat including the taya |
| Over a fallen player | the get-up meter, which is a mash meter and never a clock |

⚠️ **The in-match HUD carries no sentences.** Every explanation lives behind the hold-to-inspect
key, and every key label is read from the LIVE binding rather than typed, so a rebind cannot make
the screen teach the wrong key.

---

## 9 · Multiplayer

Four peers, host authoritative. **Every point is awarded in one function on the host**, contact
resolves by distance on the host, and a client never creates score. Peers join by code or over
LAN discovery; seats are what a peer joins, so bot-filled seats cannot vote in a rematch.

✅ **The transport HAS been run on a real LAN, with the internet unplugged**, confirmed by 🧑 on
2026-08-31. This line read "has never been run as two real processes on a LAN" and was stale; the
same claim was carried in `docs/TODO.md`'s header and `FUTURE.md` § 17 and is corrected in all of
them together. ⚠️ **The requirement it was guarding is permanent**: venue internet at the nationals
cannot be assumed, so a four-player match must stay startable and completable with UGS unreachable.
Treat it as a regression check after any change to the boot or network path, not as an open task.

---

## 10 · The tutorial

A dedicated guided route of **17 lessons**, not a match with a card over it: no clock, no round
counter, no scoreboard, and the other three seats are switched off until a lesson needs a body.
Each lesson unlocks only the verbs taught so far, and the hero deck stays off screen until the
lesson that teaches the kit.

---

## 11 · Where to look next

| Question | File |
|---|---|
| Why is it like this? | [`VISION.md`](VISION.md) |
| What is the exact number? | [`Design.md`](Design.md), then `Balance.cs` |
| Why is this ability priced like that? | [`Hero_Strike_Balance.md`](Hero_Strike_Balance.md) |
| What does the map do? | [`Ilalim_Ng_Tulay.md`](Ilalim_Ng_Tulay.md) |
| What is broken or open? | [`TODO.md`](TODO.md) |
| How do I test it? | [`TESTING.md`](TESTING.md) |
| How do I work in this repo? | `CLAUDE.md` |
