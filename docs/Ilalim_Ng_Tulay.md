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
- **The corridor, |z| to 24.0**, walls at |z| = 16.5. The visible road continues past them to
  intersections at |z| = 31, with car-kit traffic, corner shops, traffic lights, a lower
  mid-rise belt and a 240 m ground plate behind that. Nothing solid visually terminates the road.

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

Four live columns at (±4.45, ±10.0), plus a structural pair at each side of |z| = 19. Each is
1.4 m square and reaches the 8.0 m soffit.

- They are **outside the chalk** (|z| = 10 against a box that ends at 7.0), so the taya never has
  to walk around one.
- They sit **on the retrieval lanes**, which is the only moment of the game that is supposed to
  be dangerous (`VISION.md` § 0). An attacker running back in for their tsinelas can break line
  of sight behind one. That is a real choice and it costs time.
- They **break Sean's Ignition Cannon**, whose range is 10.0 m, at exactly the distance it wants
  to fire from.
- They **bank slippers** (`Slipper.BounceOffObstacles`), which the map's callouts name.
- The pair leaves a **7.5 m centre gap** and **1.85 m side gaps** to the kerbs. The middle is
  the fast retrieval lane. The gutters are slower bank-shot routes with real cover.

### 3.4 Where each ultimate wants to be spent

This is the part that makes a map fun to bring a hero to: every kit should have a moment that is
better here than elsewhere.

| Hero | Ultimate | Its moment on this map |
|---|---|---|
| Sean | Supernova, 4.8 m | The corridor funnels three attackers onto the can from two ends. It is the only map where they converge on a line rather than a circle. |
| Dante | Titan Fissure, 5.5 m | A line weapon down a 14 m straight lane. Fired along Z it covers the whole approach; fired across X it is wasted. That is a real read. |
| Cheska | Glacial Nova, 4.6 m | At a support row, where it catches the 7.5 m centre and one gutter but cannot own both pavement exits. |
| Zack | Thunderstrike, 4.5 m | On the throwing line at \|z\| = 8.0, where every attacker has to stand to be legal. |
| Nemu | Seance Void, 3.2 m | On the can itself. Retrieval converges there and nothing else contests that square metre. |

**Cheska's Ice Barricade cuts rather than closes.** A 3.2 m wall covers 43 per cent of the
7.5 m middle, so it forces a direction without switching the whole north or south approach
off. Zack and Sean keep clean dash exits; Nemu can take a 1.85 m gutter; Dante still has the
long Z line. The geometry gives each kit a use and no kit ownership of the map.

### 3.5 The train, as a mechanic rather than a backdrop

It shipped as a model sliding along Z with one whoosh: a screensaver, since nothing on the
street changed for having seen it. A map's one recurring event is the cheapest depth there is,
because every player learns its period inside a single round.

```
WARNING  3.0 s   toast, rail hum, the shadow sweeping in from the south
OVERHEAD 2.70 s  nose entering the south wall through tail leaving the north wall
idle     rest    back to Interval = 24 s
```

The three-car city consist is 15.6 m long and moves at 18 m/s. Its trigger watches the train
centre across |z| = `16.5 + 7.8 = 24.3`, so the measured window is
`(33.0 + 15.6) / 18 = 2.70 s`. The earlier trigger watched only the origin and did not match
the train still visible above the street.

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
| `Malayo` | the 240 m ground plate | the same fade |
| `Gilid`, `BackgroundStreet` | commercial kit shophouses and the two far intersections | nothing: each instance has a complete warm atlas variant |
| `SkylineKit`, `BacklotKit` | low-detail commercial skyline and industrial rail lots | nothing: warm atlas plus distance fog |
| `Tulay` | guideway, columns, the consist | nothing: concrete and livery are the point |
| `Tindahan` | PC Express, pisonet, pares cart | nothing: brand colours are the point |
| `Kalat`, `Kable`, `Hazards` | clutter, poles, pads and trips | nothing, matching Eskinita |

⚠️ **The kit buildings are deliberately NOT in a facade group.** The commercial and industrial
meshes already sample a coloured atlas. `tools/make_ilalim_kit_palettes.py` emits three complete
commercial variants and two industrial variants from those source atlases, replacing saturated
blue and orange swatches with cream, concrete, mint, ochre, faded rose, rust and galvanized
metal. The builder swaps the whole atlas per instance. Multiplying another facade tint into it
would crush walls and trim together and would reintroduce the exact palette failure recorded in
`EnvColourPass`.

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

- Bayan Plaza's monument stands inside the chalk. `TODO.md` § 4.

---

## 8 · The v2 rebuild plan, written before construction

🧑, 2026-08-24: the map needs to read as Filipino Skyways with PC Express among the stalls,
feel good for the hero mode, and have quirks that make it pleasant to return to. The v1 rules
geometry is worth keeping. The v1 architecture is not: four box buildings repeated through the
whole frame make the one authored storefront look as if it was pasted into a blockout, and the
6.88 m deck covers only 49 per cent of the 14 m carriageway. This pass keeps the cross section
and rebuilds everything above it.

### 8.1 The guideway is the map's silhouette

The target deck is **10.5 m wide**, or **75 per cent of the carriageway**, built from twelve
4 m `roads/road-bridge` bays for a 48 m visible run. Its soffit stays at y = 8.0, so gameplay
clearance and the hard shadow band do not change. Two detailed tracks sit at x = +/-2.35. A
2x train scale makes the widest city carriage 2.6 m wide, leaving 1.6 m from its outer side to
the deck edge and making the consist look carried by the guideway instead of balanced on it.

Each live support row stays at z = +/-10.0, outside the box by 3.0 m. The wide kit pillars are
scaled to **1.4 m square** and centred at x = +/-4.45, which gives:

- a **7.5 m central gap**, up from 4.4 m;
- **1.85 m side gaps** to the kerb;
- 43 per cent central-lane coverage from Cheska's 3.2 m wall, instead of a full closure;
- a pillar inner edge at |x| = 3.75, far enough from the spawn-to-can axis for a straight
  retrieval while still giving both sides bank-shot cover.

This deliberately removes the v1 map's best-hero bias. Cheska still cuts a lane, but she does
not get to switch the entire north or south approach off with one skill. Zack and Sean get
clean dash exits through the middle, Nemu can phase through a side gap, and Dante still has a
long Z lane for Titan Fissure.

### 8.2 The train window must match the train players see

The consist becomes three `train-electric-city` pieces at 2x scale, **15.6 m long**. At
18 m/s, a window that starts when its nose reaches the south wall and ends when its tail leaves
the north wall is `(33.0 + 15.6) / 18 = 2.70 s`. The trigger half-distance is therefore
`16.5 + 7.8 = 24.3 m`. This replaces the v1 prose claim of 2.6 s over a trigger that actually
watched only the train origin and lasted 1.38 s at 24 m/s.

Hero Strike keeps cooldown overclock and Classic keeps spectacle plus Street Hype. The train
still never charges an ultimate, awards a point, or rewards waiting.

### 8.3 Build a Gilmore strip, not a row of boxes

The six near `env_building_block_*` instances are replaced by the committed commercial kit at
5x scale. The cross rows are removed completely: they made Aurora Boulevard look built against
a wall. Asphalt and supported pavements now continue to intersections at |z| = 31, car-kit
traffic sits wholly beyond the |z| = 16.5 gameplay wall, and corner shops plus traffic lights
frame the road without closing it. The far belt uses smaller low-detail mid-rises, with a small
industrial back-lot cluster beyond the north wall.

PC Express remains the authored hero storefront at the west wall and is now based on the
supplied real exterior. `tools/build_pc_express_logo_mesh.py` traces the official horizontal
artwork into one clean raised white silhouette with a blue channel-letter return; the noisy
doubled red-blue face outline was rejected in the v13 review. The registered-mark badge is
omitted because it is not mounted on the real storefront. A deep red-blue lightbox,
metal returns, a slim overhang, glass mullions, centre doors, kick plate, repair signs and
delivery boxes make it one shop in a continuous computer strip rather than the only detailed
object in the scene. The official red and blue are a recorded brand exception to the role-hue
law and appear only on this fixed environmental mark, never on a gameplay signal.

The palette is an **atlas replacement**, never a renderer tint. The commercial, industrial,
roads and train kit colormaps contain saturated orange and blue swatches close to the role
hues. A repeatable palette tool emits map-specific warm atlases from the source maps, replacing
those swatches with cream, concrete, mint, ochre, faded rose, rust and dark galvanized metal.
Each kit instance receives a complete atlas variant. `EnvColourPass` does not multiply a second
colour into those already-coloured atlases.

That inverts § 5's v1 exception: the near row remains outside the facade tint groups because
its variation now comes from whole warm atlas swaps, not because a hand-built `.mtl` carries a
baked facade colour.

### 8.4 Make the pisonet read or remove it

It stays, but not as the lone cabinet that failed. Three terminals under one tarpaulin, three
monobloc chairs, bundled extension leads and a hand-painted **PISONET / P1 / 5 MIN** board make
it a recognisable sidewalk business. The PC Express showroom and the pisonet then read in the
same frame as two ends of Gilmore computer culture instead of as unrelated props. If the
cluster still reads as random in the v7 street-life capture, it is cut rather than defended by
prose.

### 8.5 Quirks with a gameplay job

The empty 14 m box remains non-negotiable. Character comes from the ring around it:

1. **Overclock train pass.** A learned 24 s rhythm and one 2.70 s cooldown window.
2. **Bridge hoop.** A hard optional slipper shot with callout and no score.
3. **Split support rows.** Two wide middle lanes plus risky 1.85 m bank-shot gutters.
4. **PC Express pad.** A short pavement speed route that costs the direct throwing line.
5. **Readable pisonet and pares corners.** Interactive street pockets on the safe flanks,
   never new objectives and never score sources.
6. **Vertical Manila layer.** Awnings, signs, cable spans, lamps, rust and damp marks put detail
   above eye level while the asphalt remains quiet enough for ability telegraphs.
7. **Continuous shopfront-edge wiring.** Thirteen 12.09 m wire-only spans per side run parallel
   to the Z street from the south district into the north district and disappear into fog before
   their end can be seen. Fourteen single posts per side carry the shared joins, so no seam gets
   a doubled pole pair. Their centres sit at |x| = 10.65 beside the shopfront, leaving the 4 m
   pavement clear. The gate rejects a seam, an X-oriented span, a mid-pavement part or a floating
   pole foot.

### 8.6 Updated Hero Strike spots against the target geometry

| Hero | Best local use after the rebuild | Counterplay left by the map |
|---|---|---|
| Sean | Supernova at a throwing line catches a committed retrieval and both exits. | Pavement retreat remains outside its 4.8 m radius. |
| Dante | Titan Fissure along Z owns the straight road; across X it spends only 14 m. | A player can cut behind either support or onto a pavement. |
| Cheska | Glacial Nova at a support row catches the 7.5 m centre and one side gap. | It cannot cover both pavement exits, and the 3.2 m wall closes only 43 per cent of centre. |
| Zack | Thunderstrike on the can punishes a pile-up, while the centre gap gives Bolt Sprint a clean exit. | Both 4 m pavements remain lateral escape routes. |
| Nemu | Seance Void on the can controls retrieval; Phase Walk can take the 1.85 m gutter others hesitate to enter. | The opposite gutter and the wide centre stay open. |

Acceptance is not a beauty shot alone: `MapGeometryCheck` stays at zero, the box stays empty,
the taya and thrower views keep the lata and all four chalk edges readable, and the overview
must show PC Express as part of the strip beneath a guideway that unmistakably carries a train.

---

## 9 · Final composition plan after the v13 review

The v13 review caught three faults that measurements alone cannot: complete cable-span prefabs
duplicated a pole at every seam, six block-letter rectangles made every business look owned by
the same sign painter, and the sparse far belt left beige ground visible behind the near row.
The correction is a hierarchy, not more random clutter.

### 9.1 Three visual depths

1. **Gameplay depth, |x| <= 11.** Quiet asphalt, chalk, lata, two support rows and readable
   ability floor. Nothing new spends this layer.
2. **Street depth, |x| = 11..30.** Continuous commercial facades, the five actual businesses,
   delivery traffic, kerb-edge utility posts, awnings, chairs and repair clutter. Every object
   answers a shop or street use.
3. **District depth, 30..120 m.** A dense second mid-rise row, industrial back lots, far
   intersection corners, car traffic, lamps, fences and a lower horizon belt. Roads and
   pavements continue to the 120 m plate and disappear into fog at 110 m, so no player ray ends
   on an exposed world edge.

### 9.2 One sign language per job

- **PC Express:** the supplied official mark as smooth raised channel letters. No registered
  badge, no block-font substitute.
- **Pisonet:** one framed cream rate fascia tied to its awning, with maroon hand-painted type.
- **PC repair:** a projecting vertical blade sign read while moving along the pavement.
- **Pares:** a small ground A-board beside the cart, not another wall fascia.
- **Regulatory:** the two small red BAWAL placards remain on concrete, where repetition is real.
- **Civic:** the barangay notice remains one blue cloth-scale tarp at the far support.

No two adjacent businesses get the same sign silhouette, size, mounting or colour blocking.

### 9.3 Utility line construction

The full-span prefab is replaced by one `electricity-pole-single` at each shared join and one
`electricity-wires-wide` mesh between joins. Posts sit at the shopfront edge |x| = 10.65, not
mid-pavement, and the cable runs stay parallel to Z. The elevated gate measures all 28 post
feet, all 26 wire spans, every seam, orientation and street clearance.
