# Ilalim ng Tulay: the map, and why it is shaped this way

The LRT Gilmore strip. A length of Aurora Boulevard under the elevated guideway, with a PC
Express showroom on one pavement and a pisonet and a pares cart on the other.

Read [`VISION.md`](VISION.md) first. This document is downstream of its § 1 (two modes, both
ship) and its § 2 (the readability budget), and several decisions below only make sense
against them.

Built by `Assets/TumbangPreso/Editor/MapKit/IlalimNgTulayBuilder.cs`. Refused by
`Assets/TumbangPreso/Editor/MapKit/MapGeometryCheck.cs` if the geometry stops holding.

---

## 0 · The one-paragraph version

**The chalk box IS the carriageway.** The road is 14 m across kerb to kerb because
`Balance.ConfinementRadius` is 7.0, so a player reads where the taya is confined off the kerb
line without ever looking at the paint. The taya is locked to the tarmac. The attackers work
from two pavements and two long lane ends. Overhead, a train crosses every 24 seconds and
gives the round a metronome that Hero Strike can plan against.

---

## 1 · Why the other two maps feel wrong for Hero Strike

🧑, 2026-08-24: *"the 2 maps feel weird to play abilities gamemode on"*. That is a real
measurement, not a mood, and it comes out of `MapGeometryCheck`'s own floor report:

| | box | playable area | **lateral room outside the box** |
|---|---|---|---|
| Eskinita | 14 x 14 | x +/-8.6, z +/-18.0 | **1.6 m per side on X** |
| Bayan Plaza | 14 x 14 | x +/-13.0, z +/-13.0 | 6.0 m per side, **with a monument inside the box** |
| Ilalim ng Tulay | 14 x 14 | x +/-11.2, z +/-16.7 | **4.2 m per side on X**, nothing inside the box |

**Eskinita is the one that feels worst and the reason is a single number.** An attacker must be
OUTSIDE the box to throw (`ThrowRules`, the negation of `Confinement.IsInsideBox`). On Eskinita
there is 1.6 m of legal standing room on each long side, which is less than one body plus one
step. Every attacker is therefore squeezed into the two Z ends, all four players fight over the
same two entrances, and:

- **Dashes have nowhere to go.** Sean's Flame Rush and Zack's Bolt Sprint both cross more
  ground than the map has to give sideways, so they are only ever used along Z, in the one
  direction the defender is already watching.
- **Zones cover a fraction of the map that reads as "most of it".** Cheska's Glacial Nova is
  4.6 m of radius, 9.2 m across, against a 17.2 m playable width. One ultimate paints more than
  half the width of the arena. `VISION.md` § 2's rule 5 (a mid-fight screenshot must still show
  the lata, the chalk and every player) cannot be met there by any ultimate at all.
- **`AiTuning.HazardAvoidMaxRadius` is pinned.** A bot cannot path around a disc that covers
  half the width, so it walks the perimeter, which is the behaviour `VISION.md` § 2 calls the
  canary for whether a human can read the floor.
- **`ArenaCheck` bound 3 passes by exactly zero.** The AI standoff ring is
  `7.0 + 1.2 + 0.4 = 8.6` against a wall face at 8.6. Bots are sent to points that are exactly
  on the wall.

**Bayan Plaza has the room and spends it badly.** It is 26 m square, which is enough, but
`Obstacles/MonumentBody` stands INSIDE the chalk: 0.70 m by 1.90 m of the box, 5 m tall. The
taya is clamped in there and cannot step out to walk around it, so one approach to the can is
permanently shielded. In Classic that is a quirk. In Hero Strike, where a wall or a zone placed
against the monument closes a lane outright, it is a coin flip on which seat draws the taya
round with the good geometry. Logged as `TODO.md` § 4.

**What this map does about it.** 4.2 m of legal ground on each long side instead of 1.6, which
is a body, a step, a dodge and a dash exit. `ArenaCheck` bound 3 clears its wall by 2.6 m
instead of 0.0. Nothing solid stands inside the chalk at all, and `MapGeometryCheck` now fails
the build if anything ever does.

---

## 2 · The cross section, and what every band is for

Every number below is either derived from `Balance` or measured out of a `.obj`, and the
builder holds no typed-in heights at all: everything asks `IlalimNgTulayBuilder.SurfaceTop(x)`.

```
        shopfront     pavement   kerb            CARRIAGEWAY            kerb   pavement    shopfront
  |--------------|------------|---|--------------------------------|---|------------|--------------|
 -20            -11          -7                  0                  7            11             20
                              ^                                      ^
                              +-- chalk, |x| = Balance.ConfinementRadius = 7.0 --+
  y = 0.150        y = 0.212   y = 0.150         y = 0.000        (wall face at |x| = 11.0)
```

- **The carriageway, |x| <= 7.0, y = 0.000.** Flat, uncluttered, mid-value asphalt. This is the
  box, and its emptiness is the single most important Hero Strike property of the map. See § 3.
- **The kerb, |x| 6.65 to 7.0, y = 0.150.** 0.35 m wide, continuous, and **painted white on
  top**: that paint is the east and west chalk. Drawn flat on the road at |x| = 7.0 like the
  other two maps it was invisible, because the kerb stands in front of it from every angle a
  player has. A 0.15 m lip is also below `CharacterController.stepOffset` (0.30), so it is
  crossed at a run without snagging and without being an obstacle.
- **The pavements, |x| 7.0 to 11.0, y = 0.212.** 4 m each. Every prop on the map that is not
  the guideway lives here, which is how the box stays empty. The 0.212 m step up is the visual
  and tactile edge of the danger zone.
- **The wall faces, |x| = 11.0.** Thin (0.4 m) and pushed OUT by their own half-thickness, so
  the reachable edge is exactly 11.0. `MatchInstaller` reads `AIController.PlayableHalfX` from
  the collider CENTRE, so a fat wall centred on the pavement edge would send bots at ground
  they cannot stand on.
- **The shopfront apron, |x| 11.0 to 20.0.** Solid plate. It exists so no building in the
  street stands on air, and PC Express's face lands exactly on the west wall.
- **The corridor, |z| to 24.0**, walls at |z| = 16.5, closed at both ends by a cross row of
  blocks at |z| = 21, with a far belt and a 240 m ground plate behind that.

---

## 3 · The Hero Strike plan

`VISION.md` § 2 sets the budget: 196 m² of box, ability footprints of 1.8 to 2.5 m of radius,
one big ultimate at a time, spend on detail not area, and a mid-fight screenshot must still show
the lata, the chalk and every player. A map cannot change those numbers. What it can do is give
them room to be true.

### 3.1 An empty canvas inside the box, and this is the load-bearing decision

Every zone in the game paints the FLOOR: Cheska's Permafrost Sheet (2.3 m), her Ice Barricade
(1.6 m telegraph, 3.2 s), Nemu's Seance Void (3.2 m), Sean's and Zack's trails, Dante's Seismic
Stomp (2.4 m) and Titan Fissure (5.5 m). Fourteen metres square of flat, unbroken, desaturated
asphalt is the best surface the game has to draw them on, and it is why nothing stands inside
the chalk.

The shipped version of this map broke exactly that: two 3.4 m viaduct columns at z = -5.0, both
inside the box. They are at |z| = 10.0 now, outside it, and `MapGeometryCheck.CheckBoxIsClear`
fails the build if a solid collider ever returns.

### 3.2 Contrast under the effects

Ability VFX are saturated: fire orange, ice cyan, magma, void purple, electric yellow. They read
loudest on a desaturated mid-value ground. The asphalt goes through `EnvColourPass`'s `RoadTint`
(0.66, 0.62, 0.55), the same warm-neutral correction the other two maps' roads get, and **the
guideway lays a hard diagonal band of shade across the carriageway** from the 55-degree sun.
That band is free contrast: cyan and purple pop hardest inside it, orange and yellow outside it.
No other map in the game has a roof.

### 3.3 Cover at the box edge, never inside it

Four columns at (±3.2, ±10.0), scaled to 0.6 on X and Z, plus a structural pair at |z| = 19.

- They are **outside the chalk** (|z| = 10 against a box that ends at 7.0), so the taya never has
  to walk around one.
- They sit **on the retrieval lanes**, which is the only moment of the game that is supposed to
  be dangerous (`VISION.md` § 0). An attacker running back in for their tsinelas can break line
  of sight behind one. That is a real choice and it costs time.
- They **break Sean's Ignition Cannon**, whose range is 10.0 m, at exactly the distance it wants
  to fire from.
- They **bank slippers** (`Slipper.BounceOffObstacles`), which the map's callouts name.
- At full size the pair left a **1.6 m gap** down the middle of a 14 m carriageway, which is a
  chokepoint on the only line between the south spawns and the can. At 0.6 the gap is **4.4 m**
  and they still read as columns.

### 3.4 Where each ultimate wants to be spent

This is the part that makes a map fun to bring a hero to: every kit should have a moment that is
better here than elsewhere.

| Hero | Ultimate | Its moment on this map |
|---|---|---|
| Sean | Supernova, 4.8 m | The corridor funnels three attackers onto the can from two ends. It is the only map where they converge on a line rather than a circle. |
| Dante | Titan Fissure, 5.5 m | A line weapon down a 14 m straight lane. Fired along Z it covers the whole approach; fired across X it is wasted. That is a real read. |
| Cheska | Glacial Nova, 4.6 m | At the column choke, where the 4.4 m gap and a 4.6 m radius are the same number. |
| Zack | Thunderstrike, 4.5 m | On the throwing line at \|z\| = 8.0, where every attacker has to stand to be legal. |
| Nemu | Seance Void, 3.2 m | On the can itself. Retrieval converges there and nothing else contests that square metre. |

**Cheska's Ice Barricade is strongest here and that is deliberate.** A 3.2 m wall across the
4.4 m column gap closes a lane outright for its duration; on an open plaza the same wall is
walked around. Every hero should have one map that is theirs.

### 3.5 The train, as a mechanic rather than a backdrop

It shipped as a model sliding along Z with one whoosh: a screensaver, since nothing on the
street changed for having seen it. A map's one recurring event is the cheapest depth there is,
because every player learns its period inside a single round.

```
WARNING  3.0 s   toast, rail hum, the shadow sweeping in from the south
OVERHEAD 2.6 s   the pass itself, keyed to the walls at |z| = 16.5 plus the 14 m consist
idle     rest    back to Interval = 24 s
```

**Hero Strike gets `OverheadPassWindow.CooldownRate = 2.0` while the consist is over the
street.** Named OVERCLOCK, which ties it to the PC Express theme. It is a power window every
24 s, and reading it is a skill: bank an ultimate for it, hold a barricade until it opens, save
a dash. That is exactly the counterplay `VISION.md` § 1.1 says Hero Strike exists to add.

Three constraints on it, none of them negotiable:

1. **It scales the cooldown drain and nothing else.** Not the ultimate charge:
   `VISION.md` § 4 says nothing may reward waiting, and a meter that fills faster on a timer is
   a reason to stand still for twenty seconds. Not effect durations either. The scale is applied
   inside `HeroAbility.Tick`, not at the call site, because the `dt` handed to `Tick` also drives
   `DurationRemaining` and, through `HeroKit`, the charge.
2. **Classic gets no part of it.** `VISION.md` § 1.1: Classic is not Hero Strike with the powers
   off, and a map may not hand it a power any more than a kit may. Classic gets the spectacle and
   a Street Hype callout, which `Hud.ReportStyle` makes cosmetic by construction.
3. **The window is cleared on the way out.** `OverheadPassWindow` is a static, and a 2x cooldown
   rate left behind would follow the player into the next match on a different map where nothing
   would ever put it back. `LrtTrainFlyby.OnDisable` and `OnDestroy` both clear it.

### 3.6 Two long lanes and two short flanks

The street runs north to south. That is four approaches, and they are not the same shape:

- **The two lane ends** are long and straight: good for dashes, projectiles and Dante's fissure,
  and they are where the spawn ring (`Confinement.AttackerSpawnRing()` = 9.0) puts everybody.
- **The two pavements** are short flanks 4 m wide: good for a shove, a lunge or a phase, and
  they are where every prop, hazard and pickup lives.

A taya can cover two of the four at once. That is the pressure Hero Strike wants and it is
geometry, not tuning.

---

## 4 · The things that only exist here

1. **The bridge hoop.** A ring beside the south west column. Putting a thrown tsinelas through
   it fires "TRES!" and, in Classic, Street Hype. **It awards no score**, and it must never:
   `MatchDirector.AddScore` is the only place a point is made in this game, host side, and that
   is what makes a point uncreatable on a client. `BridgeHoop` tests the crossing against the
   previous frame's position rather than with a trigger, because a slipper moves 0.4 m per frame
   at a full throw and the ring is 0.04 m thick.
2. **The PC Express overclock pad.** RGB floor pad on the pavement outside the shop: 1.5x speed
   for 2.2 s. It is a reason to leave the throwing lane. On the shipped map it was inside the
   shop's own collider and could not be reached.
3. **The pisonet.** Coin clinks and gamer callouts on a bump.
4. **The pares cart.** Broth sizzle and "MAINIT NA PARES!".
5. **Bank shots off the columns.** Named by the map, not just permitted by physics.
6. **BAWAL UMIHI DITO**, painted on two column faces, and a barangay tarpaulin on a third.
7. **Trip hazards**: a pisonet extension cord, a broth slick and dropped GPU boxes on the
   pavements, and two potholes in the road at |x| = 3.4, deliberately off the spawn-to-can line.
   The shipped map centred one on the world origin, which is where the can spawns and where
   every retrieval in the match converges.

---

## 5 · Making it look like the same game

🧑, 2026-08-24: *"im so worried the current map ur building doesnt have the same look as other
maps in terms of color or feel"*. That was correct, and the cause was one word.

**`EnvColourPass.DressingRoot()` looks for a child named exactly `Dressing`.** The shipped map
put everything under a node called `Geometry`, so the pass walked nothing, repainted nothing,
and logged "repainted 0 of 0" while Eskinita and Bayan Plaza were both getting the seeded Manila
facade palette, the six roof atlases, the warm-neutral road correction and the belt fade. The
map was not using a different palette. It was using no palette.

The node is called `Dressing` now and its children carry the group names that pass already
knows:

| Group | What is in it | What the pass does |
|---|---|---|
| `Kalsada` | asphalt, road sub-base | `RoadTint`, the warm-neutral correction |
| `Slab` | pavement tiles, kerbs, shopfront apron | `SlabTint` |
| `Belt` | the far horizon blocks | facade tint, then 68% toward `BeltFade` |
| `Malayo` | the 240 m ground plate | the same fade |
| `Gilid` | the near shophouses and both cross rows | **nothing, on purpose. See below** |
| `Tulay` | guideway, columns, the consist | nothing: concrete and livery are the point |
| `Tindahan` | PC Express, pisonet, pares cart | nothing: brand colours are the point |
| `Kalat`, `Kable`, `Hazards` | clutter, poles, pads and trips | nothing, matching Eskinita |

⚠️ **The near blocks are deliberately NOT in a facade group.** Eskinita and Bayan Plaza dress
themselves out of the Kenney City Kit, whose walls ship near-white, so MULTIPLYING a Manila
facade tint into them is what gives those maps their colour. This project's own
`env_building_block_*` meshes already carry the palette baked into their `.mtl`: block_a is
cream 0.886/0.824/0.675, block_b is terracotta 0.710/0.400/0.298. Multiplying terracotta by
terracotta gives 0.50/0.16/0.09, which is nearly black, and that is exactly how the whole
shopfront line came out of the first capture. **Same palette, reached by the model rather than
by the pass.** The far belt does stay in a facade group, because there the pass fades toward the
sky and a fade lightens.

⚠️ **And the showcase probe now runs `EnvColourPass.Apply()` before rendering.** The pass runs
from `Start()`, which never happens in an edit-mode capture. The first four renders of this map
were taken without it, so they showed raw `.mtl` colours. **Half of "it doesn't look like the
other maps" was the capture, not the map.**

---

## 6 · What refuses to ship

`MapGeometryCheck` opens the scene and measures. It gates Ilalim ng Tulay and reports on the
other two.

- **Resting.** Every renderer either rests on a surface within 0.030 m, bites into one by no
  more than 0.100 m, or carries an `AirborneByDesign` component **with a reason**, which is
  printed in the report every run. The support is sampled on a 5 by 5 grid across the prop's
  footprint, because the ground is tiled and no single 2 m tile covers a third of a shophouse.
- **The box.** No solid collider taller than `stepOffset` (0.30 m) stands inside |x|, |z| < 7.0.
- **The can.** Nothing within 1.4 m of the world origin, and no trip hazard centred on it.
- **The floor.** A 0.5 m grid across the whole walled area has ground under every sample.

It found four real bugs the day it was written that no render had caught, the best of which is
that all four utility poles were yawed the wrong way and hung their wire spans out over the back
lots instead of over the street. The support grid reported `0.212 x5, 0.150 x20`, meaning twenty
of the twenty-five squares under each pole were over the shopfront apron.

---

## 7 · Still open

- The train's wheel gauge does not match the rails. Measured: `lrt_steel_rail` puts the
  westbound pair at x -2.32 and -0.88, a 1.44 m gauge; `env_lrt_train_car`'s wheels are at
  ±1.10 from the car centre, a 2.20 m gauge. The car is centred correctly and rides the rail
  head at the right height, and the bogie skirt hides the wheels from every angle a player has,
  so this is cosmetic from the ground and wrong from the guideway. `TODO.md` § 5.
- The guideway's third rails float 0.030 m over the sleepers with no insulator brackets under
  them. Correct for a real third rail, wrong-looking without the brackets. `TODO.md` § 5.
- `env_cargo_tricycle_boxes` has two detached islands at y 1.02 and y 0.85, which are the
  handlebar and its grip with no stem joining them to the frame at y 0.93. `TODO.md` § 5.
- Bayan Plaza's monument stands inside the chalk. `TODO.md` § 4.
