# The arena map: brief (ARENA-1)

Status: BRIEF AND THE OWNER'S ANSWERS, written 2026-10-05. Nothing is built. This is the next map after the Ilalim
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
