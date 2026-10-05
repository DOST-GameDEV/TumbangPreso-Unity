# The arena map: brief (ARENA-1)

Status: THE MAP IS BUILT AND IN PLAY-TEST (2026-10-05). **Read [ARENA_HANDOFF](ARENA_HANDOFF.md) first for the current
state and the open list.** This file is the design record: the brief, the owner's answers, the agreed design (ARENA-1.1),
then one section per system as it was built (ARENA-1.6 to 1.10). Where an early section and a later one disagree (the box
grey-box, the 14 m floor, a catch at y -3, a 10 s halftime), the LATER section is the game. It began as the map after the Ilalim
rebuild; read [ILALIM_REWORK_GUIDE](ILALIM_REWORK_GUIDE.md)'s HANDOFF block for the pipeline and the
lessons this brief leans on, and TODO ARENA-1 for the work items.

## The owner's brief (2026-10-05, verbatim)

"we'll be resuming with a new arena style map. ideally this arena will feature a cheering crowd,
rocket leaegue goal style effects and holograms, the main play area has features like a sprint charge
pad (like how in rocket league theres orbs u can pick up to charge ur nitro/sprint), jump pads,
platforms that you can fall off, and it rotates around different layouts for the duration of the
game, limited to a certain amount of layouts though"

## What that asks for, item by item

| Asked for | What it means for the build | What already exists to start from |
|---|---|---|
| An arena style map | A purpose-built stadium, not a street: a clean bowl around one play floor. No city to draw, so the triangle and material budget can be small from the start. | The Blender kit pipeline and `IlalimSceneBuilder`'s pattern (export a layout JSON, a builder writes the scene, nothing hand-edited). |
| A cheering crowd | Stands full of spectators that react to the match: a throw, a hit on the can, a tag, a round's end. Must be cheap: hundreds of people cannot be hundreds of animated rigs. | `Runtime/Map/SidewalkLife.cs` has spectators with cheer, clap and groan sounds (`tools/synth_ilalim_life_sfx.py`); its people are too heavy to fill stands, so the crowd needs its own instanced or card-based technique. |
| Rocket League goal style effects | A big, bright, celebratory burst when the can is knocked down (and likely on a round win): shockwave, light, confetti, a stadium flash, sound. | `ImpactBurst`, `ComicPopup`, `CameraRig.Shake`, the bloom in `ColourGrade`. The game's VFX rules are in [HERO_KIT_METHOD](HERO_KIT_METHOD.md). |
| Holograms | Floating translucent displays: the score, the round, player portraits, replays, arena branding. | `Shaders/ToonTransparent.shader` (lit, blended), emission in `IlalimPainted`. No hologram shader yet. |
| A sprint charge pad, like Rocket League's boost orbs | A pickup on the floor that refills or boosts sprint, then respawns after a delay. This is GAMEPLAY: it changes movement, so it is host-authoritative and falls under the network contract. | `OverclockBoostPad` on Ilalim (a pad that gives a boost) and the motor's fatigue state (`CharacterMotor`, "fatigued"): check both before designing a new one. |
| Jump pads | The Ilalim pad, reused. | `Runtime/Map/JumpPad.cs`, `CharacterMotor.LaunchUp`, the kit `tools/author_jump_pad.py`. It launches on the simulating peer only; an arena with pads in the middle of play needs that rule checked against the network contract. |
| Platforms that you can fall off | Raised play surfaces with open edges and a fall beneath: a drop to a lower floor, or out of the arena and a respawn. | `KillPlane`, the edge-recovery state (`CharacterMotor.IsEdgeRecovering`, `CameraRig.ApplyEdgeRecoveryView`), and the rooftop map (`SaBubong`), which already has falls. Read how that map handles a fall before inventing one. |
| It rotates around different layouts during the game, a limited number of them | The play area CHANGES during a match: platforms, pads and obstacles rearrange between a fixed set of layouts, on a schedule or between rounds. | Nothing. This is the new system, and the largest risk. |

## The hard parts, named now

1. **Layout rotation is a gameplay system, not dressing.** Every peer must hold the same layout at
   the same time; a body standing where a platform leaves must be handled; bots must path on the
   current layout; the can, the chalk box and the spawns either stay fixed while the rest moves or
   move with a rule. It needs a design before any art: when it rotates (between rounds is far
   simpler than mid-round), how it is announced, what a layout may and may not change. Read
   [SKILL_NETWORK_CONTRACT](SKILL_NETWORK_CONTRACT.md) and [NETWORKING](NETWORKING.md) first.
2. **The rules assume a flat court with the can at the origin.** `Confinement` is a 14 m square
   about (0, 0), the attackers spawn on a ring at 9 m, the throwing line is at 8 m, and
   `MatchInstaller.MeasureWalls` reads the walls from the centre spot. Platforms and falls must fit
   round that, or the rules change (an owner decision).
3. **Falling.** What a fall costs (time, position, nothing) is a game-design decision the owner
   has to make; `CharacterMotor`'s rule today is that falling off the map "costs position and nothing
   else".
4. **The crowd's cost.** It must be designed against a budget from the first day. Ilalim reached
   11 M triangles and 5 k set-pass calls a frame before its performance pass, and the players who
   join on weaker PCs paid for it.

## Lessons carried from Ilalim (do these from the start)

- The can stays at the world origin. Build the arena around it; do not plan to move it later
  (Ilalim's court move needed the whole world shifted, `IlalimFrame`).
- Real LODs, occlusion and a performance probe from the first build, not after
  (`Tests/PlayMode/IlalimPerfProbe.cs` is the pattern; count triangles, batches, set-pass calls and
  shadow casters at fixed viewpoints).
- Share materials across the kit: Ilalim's 694 materials are its remaining cost.
- No coplanar or overlapping faces (every z-fight on Ilalim was one), and nothing near the role
  hues #f87020 and #0080e8.
- Anything that moves a player (pads, launches, hits) is resolved by the peer that simulates the
  body or by the host through the existing impact path; nothing new on the wire without a protocol
  decision.
- Ground colliders are the drawn ground's own meshes; props get merged box colliders.
- Every art step: render versioned PNGs, look at them, critique, fix, then show the owner.
- The owner's Unity editor is usually open: batch Unity needs it closed; compile-check C# outside
  Unity (the HANDOFF block says how).

## The owner's answers (2026-10-05)

Asked the seven questions this brief first carried, the owner answered:

| Question | The owner's answer (verbatim) | What it fixes |
|---|---|---|
| 1. When layouts rotate, and how many | "maybe between rounds? but it visually needs to show the arena transforming. aiming for 4-5? if that works" | The layout changes BETWEEN ROUNDS, never mid-round, and the change is a SHOW: the arena is seen rebuilding itself. Four or five layouts. |
| 2. What a fall costs | "u just fall of and get ufo-style float carried by a drone to back to the platform, then ur temporarilyfrozen in place like u just got tagged" | No kill plane respawn. A drone catches the fallen player, floats them back up in a beam and sets them down, and they are then frozen for a moment, as after a tag. |
| 3. Where the can is | "can can be in different heights, just needs to be in the center" | The can is always at the centre in plan (x = 0, z = 0); its HEIGHT may differ between layouts. |
| 4. The sprint charge | "refill normal stamina, a speed pad will be for the speedboost" | Two different things: a CHARGE PICKUP that refills the existing stamina, and a SPEED PAD that gives a temporary speed boost. |
| 5. The setting | "you decide" | Decided below. |
| 6. The size | "try a bigger floor" | A floor larger than the 14 m court; how much larger is found in the grey-box. |
| 7. The modes | "same map pool that lagoon, kanto, and ilalim ng tulay is at" | It joins the same pool as Lagoon Cove, Kanto and Ilalim ng Tulay, with whatever modes those run. |

### The setting (decided for the owner, who said "you decide"; open to change)

A NOONTIME GAME-SHOW ARENA. Philippine television's variety and game shows are loud, bright studio
spectacles with a live audience on its feet, and that is the one setting where a cheering crowd,
holograms, a goal-style celebration and a floor that rebuilds itself between rounds all belong
without leaving Manila: the street game has been put on television. Working picture:
- a round studio bowl of stands under a lighting rig, fiesta banderitas and jeepney-style chrome
  and paint on the trim, LED ribbon boards, a host's booth, camera drones (the same drones that
  carry a fallen player back);
- the floor is a stage of mechanical platforms over a lit pit, which is why it can transform;
- holograms are the show's graphics: the score, the round, the players' portraits, "TUMBA!" when
  the can goes down;
- the crowd is the studio audience, with placards and light sticks.
Nothing copied from a real show, channel or brand. Still to respect: nothing near the role hues
#f87020 and #0080e8 on large surfaces, since the teams wear them.

### What the answers mean for the build

1. **Layouts.** A layout is a named arrangement of the stage's platforms, pads and pickups, with
   the can's height. The host picks the next layout at a round's end and every peer plays the same
   transformation during the break between rounds; a round never starts until the stage is still.
   Because nobody is playing while it moves, there is no body on a moving platform to resolve.
   What must be designed: where players stand during the transformation (lifted on their spawn
   plates, or held by the drones), and how the layout id reaches every peer (it must ride an
   existing round-start message or it is a protocol change: read the network contract first).
2. **The can's height.** `Confinement` is a square in PLAN, so a can on a raised centre platform
   keeps the rule's shape; what changes is every throw's arc and the taya's reach. The throw
   prediction, the bots' aim and the court's chalk overlay (`CourtBoundaryPresentation`, which
   looks for edges near the floor it measures under the can) must be checked on a raised can.
3. **The fall.** A new recovery, built on the existing ones: the edge-recovery state
   (`CharacterMotor.IsEdgeRecovering`), the carry (`IsCarried`, used when a wind or a dash moves a
   body another peer simulates) and the tag's freeze. The drone is presentation; the carry back
   and the freeze are gameplay and are resolved like a tag.
4. **Pickups and pads.** Three floor features: the stamina charge (a pickup that respawns), the
   speed pad (a boost while crossing it; `OverclockBoostPad` on Ilalim is the nearest thing), and
   the jump pad (`JumpPad`, reused). Each layout places its own.
5. **A bigger floor.** `MatchInstaller.MeasureWalls` already takes each side's wall separately, and
   the attackers' spawn ring and the throwing line come from `Confinement`, so a larger floor is
   free in the rules; what it changes is how far a slipper must be fetched and how long a round
   takes. Start the grey-box at about twice the court's width and play it.

## The design for rotation, the fall and the pads (ARENA-1.1, agreed 2026-10-05)

Checked against the code, [SKILL_NETWORK_CONTRACT](SKILL_NETWORK_CONTRACT.md) and
[NETWORKING](NETWORKING.md). The owner's three decisions are at the end. The wire here is named
messages on `MatchRpc` (no NetworkVariables); `NetSession.ProtocolVersion` is 145.

### 1. The layout reaches every peer with nothing sent
- `SyncWorld` already carries `RoundNumber`, `RoundActive`, `MatchInProgress` and
  `PresentationMatchId` at 5 Hz and in the late-join snapshot (`HostSyncPeer`).
- The layout is a pure function of those: the match id seeds a shuffle of the layouts, the round
  number indexes it. The stage shows the layout of the round being played, or of the NEXT round
  while `MatchInProgress && !RoundActive`; before the match, round 1's. It polls that every frame
  and snaps if it ever disagrees, so a dropped packet or a late join cannot leave a peer on the
  wrong stage.
- FIVE layouts, not four: with four seats taking the taya in turn (`MatchRules.DefenderSlotFor`),
  four layouts would hand each player the same layout every time they are taya.

### 2. The transformation and where players are
- It plays in the existing break (`HalftimePresentation`): simulation held (`PresentationClock.Hold`,
  `Time.timeScale` 0), input refused, every peer timing from the host's `Began` stamp in
  `MatchBreak` (`SharedUltimatePhase.Now`). The stage animates on THAT clock, never `deltaTime`.
- The break today draws a frozen frame. On this map it draws the live stage through cinematic
  cameras (owner: "ther'll be camera cinematics showing the transforming play arrea").
- THE BREAK IS LONGER ON THIS MAP: 8 s in place of 3.5 s (start value; halftime is its own 10 s and THEN these 8 s, see ARENA-1.9). The
  duration is read from the map on every peer, so it stays shared. This is a timing change like
  protocol133's and ships under the same protocol bump as the map's entry in the pool.
- Colliders switch to the next layout at the break's start (nothing simulates while held); the
  visuals travel. Players are shown lifted by the drones and set down on their marks; the real
  reset is the existing `SliceRunner.ResetWorld` teleport at round start.
- Fixed in every layout: floor under the computed marks (taya at (0, 0, -2.5), attackers on z +9
  at x -1.8, 0, +1.8; `MatchHost.SeatOnFloor` casts from +2 m down 6 m) and the walls (`Bounds`,
  measured once in `MatchInstaller.Start`).
- The can: centre in plan always; height 0 to 1.5 m in the grey-box, the taya's mark on the same
  platform. `Lata._mark` is snapped once in `Start` and has no setter: it needs a host re-snap
  before `HostRestore` each round. Clients take the can's position from `SyncLata` already.

### 3. The fall, the drone, the freeze
- The host decides, in a host-only `FixedUpdate` like `RooftopRecovery`: a body below the stage by
  about 3 m is caught. It must be above y -5, where `MatchRpc.AcceptMove` refuses a pose.
- The carry is a third `EdgeRecoveryKind` (after Rooftop and Lagoon): the host owns the body's
  trajectory (`BeginEdgeMovementOwnership`), the owner's moves are refused until it ends, and the
  kind, grip and phase already ride `SyncUnit`. No new message; a new enum value, so it ships
  under the same protocol bump.
- Set down on the last safe spot the body stood on in this layout, inset from the edge; else the
  nearest platform. Not the round spawn.
- A held slipper is lost and returns to its mark after a delay (`Slipper.HostBeginMapRecovery`,
  as on Sa Bubong).
- Then `ApplyTagged()`: the tag's own 5 s, not removable, no immunity (owner: "same 5 second tag
  freeze"). No score, no teleport to the spawn.

### 4. Pads and the pickup
- Jump pad: `JumpPad` as it is (the simulating peer launches, nothing on the wire), launch lowered
  so the apex is about 6 m (`AcceptMove` refuses y above 20; the ceiling is 12).
- Speed pad: the simulating peer, nothing on the wire; the host's move budget is 30 m/s. The motor
  needs a real timed speed-up: `SpeedZoneStack` takes the LOWEST value from 1.0, so it can only
  slow, and Ilalim's `OverclockBoostPad` (1.5 through that stack) does nothing today.
- Stamina pickup: the host grants `Stamina.RefillAndClearFatigue()`, which reaches the owner
  through `SyncUnit`. Whether an orb is taken or back is new shared state: ONE new host message
  with the pickups' availability, also in the late-join snapshot (owner: agreed). The solo
  grey-box does not need it.

### 5. What else the grey-box must add
- BOTS HAVE NO PATHFINDING AND NO EDGE SENSE (`AIController.Goto` steers straight on a flat plane).
  They need a ground probe ahead that turns them along an edge, and layouts joined by ramps and
  bridges rather than jump-only islands.
- One protocol bump covers everything above: the map's entry in `SceneFlow.Maps` (maps travel as
  an index), the break's length here, the drone recovery kind, the pickup message.

### The owner's decisions (2026-10-05)
1. The freeze after a fall: "same 5 second tag freeze".
2. The transformation: "lengthen the break on this map, ther'll be camera cinematics showing the
   transforming play arrea".
3. The pickup message, shipped with the map list change: "yea".

## Order of work

1. A one-page design for the layout rotation, the fall and the drone carry, against the network
   contract (the owner's answers are above).
2. A grey-box blockout in Blender at exact gameplay dimensions, with two layouts, playable in
   Unity with bots: prove the rotation, the fall, the pads and the charge pickup before any art.
3. The performance budget and the probe.
4. The arena kit (floor, platforms, stands, lights), then the crowd technique, then holograms and
   the knockdown celebration.
5. Sound: the crowd bed and its reactions, the celebration, the pads.
6. Checks, a played match online with a joining player, then the map pool.

## After the first play: the show, the deeper fall, the effects (ARENA-1.6, 2026-10-05)

The owner played the map and said (verbatim): "map transformation is so dull, theres no emphasis on
it. could also use some screenshake"; "falling off threshold is too high, you need to fall further";
"there should be more vfx overall in the map, including the drone stuff". What changed, and where
each number now lives. Nothing here added anything to the wire.

### The transformation is three beats on the break's shared clock
`ArenaStage.BreakBeats` owns the times; `ArenaShow` puts light, sound and shake on them;
`ArenaBreakCamera` cuts to them. On the 8 s break:

| Seconds | Beat | The stage | Light, sound, shake | The camera |
|---|---|---|---|---|
| 0.0 to 0.8 | ALARM | still | the picture dims; the alarm and its riser; beacons round the rim | low push in from the turf |
| 0.3 to 1.7 | ALARM | the next layout's hologram sweeps out from the can | the scanning ring; from 0.65 the layout's NAME as a hologram title | from 0.8: straight down on the whole stage |
| 1.65 to 2.3 | ALARM | still | a drone comes down over every player | |
| 2.4 | THE MOVE | every moving platform jolts (the undock) | clunk, thruster bursts under each, arcs along each, a camera punch | cut: a low push across the stage |
| 2.6 to 6.2 | THE MOVE | the platforms go one after another; each LOCKS at the end of its own window | per platform: a thruster as it goes, light trailing off it, and on its lock a thud (a step higher each time), flash, shock ring, column of light, camera punch; a pulse chases round the LED barrier; the players are carried over the stage | from 4.95: a long lens on the last platform to lock |
| 6.35 | THE REVEAL | still | a stadium-wide flash and two shock rings from the can, the boom, the lights back, the crowd up and roaring, pyro and confetti from the field's edge, fireworks over the stands, the screens' stinger; the biggest punch | cut: a rising wide shot |
| 6.5 to 7.4 | THE REVEAL | still | the players are set down on their marks | |

- The "Next Round" card sits in the lower third at 0.7 of its size on this map's ordinary break
  only (`TumpRoundSwapView.CourtBreak.cs`).
- The players' lift moves only what is drawn (`CharacterVisual.ModelRoot`); the real reset is still
  `SliceRunner.ResetWorld`.
- The crowd has its own clock (`ArenaCrowd`, `_ArenaCrowdClock`), which keeps running in a break.
- The sounds are `tools/synth_arena_show_sfx.py` (nine cues, `sfx_arena_*`).

### The fall is a real fall
This REPLACES point 3's "it must be above y -5" above.

| Height | What | Where it is set |
|---|---|---|
| y -4.5 | a SLIPPER is taken (well above `Balance.VoidY` -12) | `ArenaStage.SlipperCatchY` |
| y -22 | the drone takes a body | `catchY` in `tools/arena_layouts.json` (written by `tools/author_arena_layouts.py`) |
| y -40 | `MatchRpc.AcceptMove` believes an owner's pose down to here, on this map only | `ArenaStage.MoveFloor`, written to `MatchRpc.MoveFloorY` while the stage is loaded (-5 on every other map) |
| y -60 | the kill plane (last resort) and, 2 m under it, the walls' feet | `ArenaStage.KillPlaneY`, `KillPlane.Height` |

The catch can never be set closer than 12 m to the move floor (`ArenaStage.CatchMargin`). Terminal
speed is 25 m/s (`Balance.MaxFallSpeed`), under the 30 m/s move budget, so a 22 m fall takes about
1.5 s; the carry back is 3.5 s (0.4 caught, 2.5 hauled and floated, 0.6 set down). While a body
falls the camera swings out above it looking down the shaft, the body tumbles and wind streaks rush
past it (`ArenaStage.IsShaftFall`).

### The fall is a clutch window (ARENA-1.10, 2026-10-05)
The owner, playing: "the fall effect is too fast, paete's utility is like a grappling hook but it
doesnt even have much of a time window to let me clutch back up". This REPLACES the 1.5 s and the
"while a body falls" sentence above. The catch line, the move floor and the kill plane have not moved.

- THE UPDRAFT (`ArenaFallRecovery.Updraft`, set as `CharacterMotor.MapFall` while the map is loaded,
  null on every other map). A body in the air with nothing of the stage under it, from 0.3 m under
  the deck: gravity 8 m/s2 (20 elsewhere), terminal speed 3 m/s at the deck rising to 10 m/s from
  14 m down (25 elsewhere), and a body arriving faster is braked at 30 m/s2. Walking off an edge:
  3.7 m down after 1 s (it was 12.2), 6.4 m after 1.5 s, 9.8 m after 2 s, caught at y -22 after 3.3 s
  (it was 1.4 s). Air control is the motor's own, unchanged (full walking speed).
- ONLY THE PEER THAT SIMULATES THE BODY RUNS IT. Nothing is sent. The host and the replicas never
  predicted a fall (a remote body is where its owner's poses put it), and `AcceptMove` limits how
  fast a body goes, never how slow. The catch is still the host's, at the line.
- PAETE'S LIANA LEAP IS A FLAT REEL (14 m/s along the ground, a 3 m/s lift): time alone could not
  make it a way back. In the shaft it hauls instead (`PaeteVine.MapCatch` = `ArenaFallRecovery.VineCatch`,
  `CharacterMotor.BeginHaul`): the vines take the deck point nearest what they caught, if that was
  the stage, else the one nearest him within 9 m of his chest (his 8 m range to the rim, and the
  metre in from it), and he is pulled up his own column and over onto it at 14 m/s. Cast within
  about 1.8 s of leaving the edge. No deck in reach, or a floor over his head: the ordinary reel.
- THE VIEW AND THE BODY ARE THE PLAYER'S OWN down to 10.5 m under the deck (`ArenaFallRecovery.LostDepth`,
  about 2.1 s in). The fall view is the emote orbit (the mouse turns the lens, not the body; the
  pitch is held 6 to 48 degrees down; the aim is cast along it), so it used to take the aim away
  from the first metre. It and the tumble now open only under that depth, where no skill reaches.
- OTHER KITS: Nemu's return to Kuro and Amihan's Updraft (2.8 m of climb, so from the first 0.7 s)
  work as they always did, with the longer window. Dashes and blinks are flat and do not climb.
- SEEN: a teal ring lifting off under the body and cyan streaks rising past it (`ArenaFallRecovery.Gust`
  through `ArenaFx`), on every peer, in the window only.

### Effects
Every effect on this map is drawn by `ArenaFx` (one mesh, one material, one draw call) and placed
by `ArenaShow`, `ArenaDrone` or `ArenaAmbience`. All of it is presentation, local to each peer, and
halved under reduced effects.

### The sky outside: haze, spotlights, clouds (owner, 2026-10-05)
"we should also add a distance haze effect for outside the arena, moving spotlights, use the clouds we have in the other maps too."
- HAZE: two shader globals read by `TumbangPreso/ArenaPainted` and set in Play by `ArenaAmbience` (`_ArenaHazeColor`, `_ArenaHaze`): lit air low among the towers, by height and by level distance from the eye, only on what stands more than 250 m from the can. Plus one faint ring of light 300 m out. Tune the numbers at the top of `ArenaAmbience`.
- SPOTLIGHTS: six on the rim, four on the landing pads, five on rooftops (the shared clock), and eight show spots under the canopies (idle sweep, chase in the alarm and at the match end, on the stage when it moves and when the can falls). All quads through `ArenaFx`; no Light.
- CLOUDS: the other maps' voxel clouds, placed by this map's look row (`MapLook.CloudBands`, in `WorldLookProfile.cs` and `Resources/WorldLookProfile.asset`): a deck of 16 under the stadium (y -345 to -228) and 5 high ones (y 340 and up, 250 to 272 m out), lit from below in the city kit's night colours.

## The slipper balloon and a slipper that leaves the stage (ARENA-1.7, 2026-10-05)

The owner (verbatim): "the balloon should be animated btw, and make it an easter egg when you try to throw a slipper at full range directed towards it you can actually hit it, and itll react like the balloon cow in overwatch. do it enough times and itll pop"; "the slipper will just spawn/tp back to the nearest edge to be able to retrieve it"; and, after playing: "issue with the bots is that if their slipper goes off the platform they cant retrieve it".

### The balloon is a rig of rigid parts (`Runtime/Map/ArenaBalloon.cs`)
The holo kit delivers it as a body (its own axes), two arms, two legs, two scarf tails, five ropes (pivot at the anchor, y along the rope), a heap of skin, the mooring; `tools/arena_holo_motion.json` names them in its `balloon` block and `ArenaHoloAuthor` bakes them onto one `ArenaBalloon` on the Holo group. It leans and rides about the winch, squashes and stretches along its length, its lobes turn about pivots inside the body, and every rope is aimed at its ring each frame. Idle is closed-form on the shared clock (the same sky on every peer); a hit, a knocked can and the crowd kick damped springs. No shader was added.

### The hit is a rule, decided by the host at the throw
| Test | Number | Where |
|---|---|---|
| a human seat, a plain slipper (no ability payload) | | `ArenaBalloon.HostTake` |
| charge | 0.9 of full or more | `MinCharge` |
| the sight line (throw origin to aim point) from the middle of the balloon | within 10 degrees | `ConeDegrees` |
| the sight line's climb | 20 degrees or more (the cone already needs 22) | `MinElevationDegrees` |
| the aim point | at or past where the line leaves the walls or the 12 m ceiling, less 1 m | `PlayExit`, `AimSlack` |
| hits to pop | 5 | `PopHits` |

From the stage the balloon's middle stands 32 to 41 degrees up, south-south-west. A throw at the can, a body or a platform aims at a point in play and fails the last test; bots are refused by seat. A hit slipper leaves play at the throw, is drawn flying up for 1.1 s, and is set back loose 1.4 s after the strike at the nearest standable point to where its line left the play area. No score, can, tag or rule is touched. Popped, it re-inflates at the next round's start or after 75 s.

### One message
`ArenaBalloon`, host to all and in `HostSyncPeer` (`Net/MatchRpc.ArenaBalloon.cs`): hits, popped, and the last event (a hit's seat, origin, line and launch time on the server clock; a re-inflation). It could not ride the slipper (an out-of-play slipper travels as Loose with no affinity) and nothing existing carries a count for a late joiner. Under protocol 146.

### A slipper off the stage comes back to the nearest edge (`ArenaFallRecovery`)
What was wrong: `Slipper.FindGroundY` answers 0 when its cast finds nothing, and 0 is this stage's deck height, so a slipper past an edge or in a gap "landed" in the air at deck height, out of reach, and stayed. A bot walked to the lip and stood there. Now the host takes any slipper with nothing of the stage under it and sets it down 1.6 s later at `ArenaStage.TryNearestStandable` of where it left (inset 1 m, never a ramp or a bonus piece). It is switched off meanwhile, so a bot sees none, waits on its throwing ring and fetches it when it is back. A slipper held by a body that fell keeps the fall's 8 s return to its owner. A slipper resting more than 1.2 m above its owner already goes to the owner (`Slipper`'s own rule), which covers a loft.

Probe: `Tests/PlayMode/ArenaBalloonProbe.cs` (report `Logs/arena/unity/balloon_probe.txt`). Sounds: `tools/synth_arena_balloon_sfx.py`.

## Lamps that look like lamps, and the sound of the stadium (ARENA-1.8, 2026-10-05)

The owner, after playing: "the spotlight being pointed at you when the can is down just looks like a bunch of feathered circles, and doesnt look like actual glare", and "there should be reverbey crowd cheers, chants, and an announcer". NOBODY HAS SEEN OR HEARD ANY OF THIS YET: every number below is a first guess to be tuned in play.

### Glare and shafts (`Runtime/Map/ArenaGlare.cs`, drawn by `ArenaAmbience` through `ArenaFx`)
- What it replaced: eight soft dots 3.2 m across hung in the air round the can, at the ends of eight beams whose streak fades at both ends.
- GLARE is drawn at a lamp when it points at this camera: the angle between its aim and the line to the eye (a show spot: whole inside 2.5 degrees, gone by 9), times how far inside the frame it is, times whether a body or a deck stands in the way. A core laid three deep (4.5 against this map's bloom threshold of 1.7), a tight centre, a long thin anamorphic streak, and for the show spots a small star, a halo and three faint ghosts through the middle of the screen. It is drawn out at the lamp, so the play is always in front of it.
- A SHAFT is three quads from the lamp (a soft cone, a narrower one, a thin white core), brightest at the lamp (`ArenaFx.DrawShaft`), with dust drifting down it.
- On a knocked can four spots land round the can and four follow the players. A pool is drawn only on the deck under the aim, 5 cm over that deck's own top, shrunk to fit inside its edges, never over a gap. The bodies take a rim (`_WorldLookShape.y`, raised for the gameplay camera only) and the picture a brief veil of at most 9 per cent, scaled by Flash intensity.
- The 24 floodlight banks and the 15 searchlights use the same lamp, quietly.

### The crowd and the PA (`Runtime/Map/ArenaCrowdAudio.cs`, clips from `tools/synth_arena_crowd_sfx.py`)
- Five seamless beds (calm, lively, roar, tension, applause) follow `ArenaCrowd.Level` and the match; six reactions; seven chants that come and go on their own with long gaps; four PA stings. Crowd on the Ambience slider, PA on the Announcer slider. Levels are the rows in `AudioCues.TrimDb`.
- Everything has the bowl's reverb baked in (a synthesised impulse response, loops convolved circularly so they have no seam). `py -3 tools/synth_arena_crowd_sfx.py --report` writes pictures and numbers to `Logs/arena/audio/`.
- On this map the announcer's takes play through the stadium (`Resources/ArenaPa/pa_<take>.wav`, baked by the same tool; rerun it with `--only pa` when a new take lands in `Resources/Vo`).
- The map's own lines are NOT RECORDED (there is no text to speech here): `ArenaCrowdAudio.Lines` lists the nine, with wording. Each is wired and captioned and plays the day `Resources/Vo/vo_<id>_1.wav` exists; until then a sting marks the moment.

## Halftime is two acts: the package, then the show (ARENA-1.9, 2026-10-05)

The owner, playing: "halftime replay is interfereing with the transformation animation". NOT YET RUN: compiled outside Unity only.

What collided. The colliders went to the next layout at the break's start, so the replay's camera and ground marks cast against the wrong floor. The replay is the LIVE scene drawn with recorded bodies, and the stage in it was already scanning (2.7 s) and moving (5.0 s) under them, with the pads switched off. A clip from an earlier round was drawn on the current round's layout. The rest of the travel ran hidden behind the frozen frame, so the round started on a stage nobody saw change.

Now, on this map only, halftime is 18 s on the one shared clock (`HalftimePresentation.DurationFor`, from the host's `Began` stamp and the map, nothing sent):

| Seconds | Act | What |
|---|---|---|
| 0 to 10 | THE PACKAGE, as on every map | frozen frame; replay from 0.35 s to 5.8 s; standings to 10 s (the centred card). The stage stands on the layout of the round just played, colliders and visuals; no show light, sound, camera, drone or PA call. |
| 10 to 18 | THE SHOW | the ARENA-1.6 table from its 0.0 to its 8.0, unchanged (alarm, move, reveal, break camera, lift, crowd). The colliders go to the next layout at 10.0. The frozen frame is dropped and the card comes in again in the lower third. |

- ONE CLOCK FOR THE SHOW: `HalftimePresentation.StageShowBegan`, `StageShowAge`, `StageShowPlaying` (the break's start on an ordinary break, the package's end at halftime). `ArenaStage.TryBreak` reads it and gives `BreakBeats.Age` and `Showing`; `ArenaShow`, `ArenaBreakCamera`, `ArenaAmbience` and `ArenaCrowdAudio` read only those. A peer joining in either act computes the same act.
- To trim the standings, lower `ArenaStage.HalftimeLeadSeconds` (10; the replay ends at 5.8 s). It is the only number.
- A REPLAY ON THIS MAP stands the stage in the layout of the clip's own round (`ArenaStage.BeginReplay`, from the clip's match id and round; the clip format is unchanged), and leaves out this map's effects mesh (it is built facing the live camera) and its drones (live, not recorded).
- NOT DONE: the drone is not recorded, so a clip that contains a fall shows the body carried by nothing; the crowd, the screens and the balloon in a replay are the present ones, not the recorded moment's.
- `ArenaMatchProbe` now expects 18 s at halftime, the stage travelling in every break, and no travel during the package.
