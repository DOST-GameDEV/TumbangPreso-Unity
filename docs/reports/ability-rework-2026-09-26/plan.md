# ABILITY-2: the roster ability rework, planned before it is built

Owner, 2026-09-26, with four tables (Anemo, Cryo, Geo, Necro and Voodoo rows):
*"No idea for pyro and hydro rework yet But here are the ability reworks of effects etc"*,
*"U thoroughly plan them first"*, *"NECRO maps to nemu"*, *"Voodoo To phaister"*, *"geo to dante"*,
*"cheska to cryo"*, *"Anemo to amihhan"*, *"DOnt hhjave idea for sean and zack yet"*;
*"will create completley new VFX SFX AND SKILLS FOR ALL CHARACTERS AND WE MIGHT DUMP THE SKILL TREE
IDEA FOR NOW AND WILL COME BACK TO IT LATER (DEPENDS ON WHTEHTER WE STILL HAVE TIME)"*,
*"JS REMOVE ITS UI FOR NOW AND HARDCODE THE SKILLS AND SHIT"*, *"UR JOB IS TO CREATE DOCS PLANS AND
SHHIT FOR IMPLEMENTATION"*, *"u can continue coding after u make plans btw"*, *"u do implementation
after thorough planning and research"*, *"i think u cant test unity tho so ill js get a diff chat to
test uir shit"*, *"u try to figure out ways aroudn not having unity engine tho"*.

This file is the plan. The shape it builds on is ABILITY-1
([amihan-kit-2026-09-25/plan.md](../amihan-kit-2026-09-25/plan.md): signature, attacking, defending,
ultimate) and the direction baseline every effect, sound and animation follows is
[amihan-kit-2026-09-25/direction.md](../amihan-kit-2026-09-25/direction.md) (six beats, silhouette
first, edges over fills, three-layer sound). Paete (Dendro) is already in the new shape (HERO-9).

## 0. The owner's tables, verbatim

| Power (hero) | Signature | Attacking | Defending | Ultimate |
|---|---|---|---|---|
| Anemo (Amihan) | **Quick Dash:** Propel forward in the target direction that inflicts Whirled and slightly pushes back other players. 40 s | **Updraft:** Fly for 10 seconds | **Whirlwind:** Create an arc-shaped gale that inflicts Whirled to players hit as it swiftly moves forward. The gale lasts for 2.5 seconds. 35 s | **Storm Surge:** After a 2.5 second delay, unleash a map-wide fan-shaped wind in the target direction that greatly pushes back all players and slippers caught inside almost to the edge of the arena. 15 objective points |
| Cryo (Cheska) | **Cold Feet:** Create a chilling field on the floor that inflicts Chilled indefinitely to players caught inside of it. The chilling field lasts for 5 seconds. 35 s | **Frostbite:** Imbue the slipper with Frozen. Hitting another player with the slipper will inflict them with Frozen. 35 s | **Glacial Wall:** Create an arc-shaped icicle wall that blocks slippers and players. The icicle wall takes 3 slipper hits to shatter. 35 s | **Absolute Zero:** Inflict Frozen to every player, followed by Chilled after thawing. 12 objective points |
| Geo (Dante) | **Shield:** Status immunity for 20 seconds | **Boulder:** Throw rock -> Concussed | **Barrier:** Create a wide force field that reflects slippers in front of you. The force field lasts for 7.5 seconds and follows you around. 25 s | **Earthquake:** Everyone concussed |
| Necro (Nemu) | **Terrify:** Leave Kuro somewhere and everyone there gets feared or smth (add to status effects) | **Kuro:** Slipper retrieve | **Kuro bigger block** (kuro aids withh blocking and becomes a bit bigger) | **Clone/kuro can play:** Kuro can play for the whole round |
| Voodoo (Phaister) | **Teleport:** Js refine this | **Curse - Disoriented:** Fake screen/ fake slipper Aim. Make them hallucinate or smth | **Curse - Vulnerable:** (blank) | **Blackhole /higop:** Shhe casts a blackhole or smth and pulls everyone towards it. No button mashhing anymore or smth. Make blackhole really cool |
| Pyro (Sean), Electro (Zack), Hydro (Rafi) | no design yet | | | |

## 1. The owner's answers (2026-09-26, this session)

| Question | Answer, verbatim |
|---|---|
| FEARED | *"Flee from kuro and drop slipper do ur 1 and js add the drop slipper when fleeing, make an animation for feared too dont js make them walk back ahha"* |
| DISORIENTED | *"hallucinations but make it so that some of the shit they see are real"* |
| VULNERABLE | *"easier to tag and phaister can go out of box and tag them"* |
| CONCUSSED | Dizzy stumble: dizzy stars, 30 % slower, no sprint, throws wobble off aim |
| Kuro fetch | Taya can intercept (touching Kuro while he carries drops the slipper) |
| Kuro ultimate | *"HARD BOT and fulfills whatever role u have and ghets separate copy of ur skills"* |
| Kuro bigger block | *"give her like an AI to think abt where to stand but dont make it infallible ahha thas op"* |
| Blackhole | *"make this one really cool i want her to really slowly cast the black whole and it can be seen in her that power is surging in her and clothes are flying on her or smth, it pulls players ands slipeprs except for her shit and no escape for entire duration but they can try to, maybe make it so that they can try to move away but they js get sucked back in anyways"* |
| Teleport | *"refine everything about shadow blink"* |
| Sean, Zack, Rafi | Placeholder (the new four-slot shape, placeholder skills that do nothing) |
| Skill tree | Remove its UI for now; hardcode the skills; keep the code to come back to |

## 2. New statuses (append to `Core.StatusKind`; never renumber)

The ABILITY-1 table has Whirled 1, Chilled 2, Frozen 3, Tagged 4, Rooted 5. Appended:

| # | Status | Seconds | Rule | Tooltip |
|---|---|---|---|---|
| 6 | **CONCUSSED** | 3.0 | 30 % slower (x0.7), no sprint, throw aim wobbles up to 9 degrees (sampled from a seeded sine, so host and owner agree) | "Dizzy: Slower, No Sprint, Wobbly Aim" |
| 7 | **FEARED** | 1.5 | drops the slipper in hand; the body runs AWAY from the fear source at run speed on its own (intent overridden on the simulating peer); no throw, no pickup, no skills; its own animation (`feared-flee`: hunched, arms over the head, glancing back, a stumble step at the start) | "Feared: Fleeing" |
| 8 | **DISORIENTED** | 4.0 | the victim's OWN screen shows hallucinations: two to three phantom players and phantom slippers, mixed with the real ones and drawn identically, so they cannot tell which are real (owner: *"some of the shit they see are real"*); aim reticle sways. Local presentation only; nothing about it goes on the wire except the timer | "Disoriented: Not Everything Is Real" |
| 9 | **VULNERABLE** | 5.0 | the taya tags them from 1.5x the normal reach, and Phaister (as taya) may leave the box to tag a Vulnerable attacker (owner: *"phaister can go out of box and tag them"*); stuns on them last 1.5x | "Vulnerable: Easy to Tag" |

Plus **status immunity** (Geo's Shield): a flag on the ability system that blocks every status whose
`ImmunityApplies` is true. Tagged ignores it (owner's table: *"Cannot be removed or be immune to"*).
Overlap is `Max()` for every timer (`CLAUDE.md` § 4). Every timer rides `SyncUnit` (protocol bump),
the HUD draws each through the existing `StatusMarks`/`StatusOverhead` path, and each gets an icon in
`tools/build_ability_icons.py`.

## 3. The kits

Numbers not given by the owner are marked **(set here)** and listed again in section 7 for review.
Every distance is authored as a distance and solved against `Balance.Friction` (`CLAUDE.md` § 4);
every contact resolves by distance on the host behind `NetAuthority.ShouldResolve()`.

### 3.1 Anemo, Amihan: already built (ABILITY-1)

The code matches the table (`AmihanHeroKit`, `Core.AmihanRules`). Open work is ABILITY-1's: capture
and critique every beat, the screens, PlayMode and bot probes.

### 3.2 Cryo, Cheska (new role kit)

| Slot | Ability | Rules |
|---|---|---|
| Signature | **COLD FEET** | 35 s. Hold to aim (1.8 to 5.0 m, the old sheet's band), release: a chilling field 2.3 m radius for 5.0 s. Anyone inside is Chilled, refreshed every step while inside ("indefinitely"), so it runs its 5 s from the moment they step off. The slip of the old sheet is dropped: the field is the status. |
| Attacking | **FROSTBITE** | 35 s. Loads her held (or next picked-up) slipper with frost for 10 s (set here), like Ignition Cannon's load. A loaded throw that touches a player (the existing `HitsBody` band) Freezes them (2.5 s) and the slipper drops. A loaded throw that touches the can still knocks it; nothing extra. New `SlipperAffinity.Frost` (appended). |
| Defending | **GLACIAL WALL** | 35 s. Hold to aim (1.8 to 4.0 m), release: an ARC of icicles 4.2 m along the arc, radius 3.0 m (set here), facing her. Blocks bodies and slippers. 3 slipper hits shatter it (a hit counter on the host; each hit cracks a third of the icicles off; the third shatters the lot). 8 s life if not shattered (set here). |
| Ultimate | **ABSOLUTE ZERO** | 12 points. The shared 0.4 s wind-up, then EVERY other player on the map is Frozen 2.5 s; when each thaws, Chilled 5 s. No radius. |

### 3.3 Geo, Dante (new role kit)

| Slot | Ability | Rules |
|---|---|---|
| Signature | **SHIELD** | 45 s cooldown (set here). 20 s of status immunity: nothing on the status table except Tagged lands. A stone ward orbits him (his orbiting protectors are kept, `AGENTS.md`). Casting cleanses what he already has, except Tagged. |
| Attacking | **BOULDER** | 30 s (set here). An aimed throw (hold to aim, like the slipper throw, same arc solver `Slipper.SolveArc` at 14 m/s). The boulder rolls on landing 2 m; any body it touches in flight or rolling is Concussed (3 s) and shoved 1.5 m. Knocks the can if it hits it? NO (set here): a boulder is not a slipper and scoring stays the slipper's. |
| Defending | **BARRIER** | 25 s. A force field 3.4 m wide, 1.2 m in front of him, following his facing and position for 7.5 s. A slipper entering it is REFLECTED back along its incoming path (`Slipper.Deflect`, credit cleared as a block). Bodies pass through (set here). |
| Ultimate | **EARTHQUAKE** | 14 points (set here). The shared wind-up, a stamp; every other player on the map is Concussed (3 s, the status length) and the camera shakes on every screen. |

### 3.4 Necro, Nemu (new role kit, Kuro is the kit)

| Slot | Ability | Rules |
|---|---|---|
| Signature | **TERRIFY** | 40 s (set here). Hold to aim up to 7 m; Kuro flies there and HAUNTS the spot for 4 s (set here), swollen and glaring. Any other player who comes within 2.2 m is FEARED (1.5 s) once per haunt: drops the slipper, flees from Kuro in the feared animation. |
| Attacking | **KURO FETCH** | 30 s (set here). Kuro flies (9 m/s, set here) to Nemu's own slipper wherever it lies, picks it up and flies it back to her hand. The taya can intercept: a tag (or tag lunge) reaching Kuro while he carries drops the slipper where he is, and he returns empty. Only her own slipper, never mid-air. |
| Defending | **KURO GUARD** | 30 s (set here). Kuro grows 1.6x for 6 s (set here) and BLOCKS slippers (a body block by distance, the taya's own `HostBlockedBy` rule). He picks where to stand with a small AI: every 0.35 s he scores positions on the line between the can and each attacker holding a slipper, and moves toward the best at 6 m/s (set here). Fallible by design (owner: *"dont make it infallible"*): he reacts to the thrower's aim late (0.35 s think, plus a 0.25 s wind before moving) and never predicts a curve. |
| Ultimate | **KURO PLAYS** | 16 points (set here). For the rest of the ROUND Kuro is a full extra player: a HARD bot (`AIController` at the top difficulty) on Nemu's side, filling her current role (attacker: his own slipper, throws at the can, points to Nemu; taya: guards and tags, tags credited to Nemu), with his OWN copy of her kit (signature and role ability on their own cooldowns; no ultimate). Ends at the round boundary. |

⚠️ KURO PLAYS is the largest engineering item in the rework: a fifth unit in a four-seat match. It
is a host-owned `CharacterMotor` with no seat, a Kuro body, bot input, scoring routed to Nemu's seat
through `MatchDirector.AddScore`, tag rules for a second taya body, and a snapshot kind for a
rejoiner. It goes last in the order (section 6) and gets its own design pass before code.

### 3.5 Voodoo, Phaister (new role kit)

| Slot | Ability | Rules |
|---|---|---|
| Signature | **SHADOW BLINK**, refined | Keep the mechanic (hold to aim, release, she is there; whoever she left is shoved). Refine every channel: a new tell (her shadow detaches and runs ahead along the aim), a new release (she folds into her own shadow, a ring of voodoo pins, a snap), a travel (a dark ribbon of shadow along the ground), the arrival (she unfolds out of the shadow), a new cast clip and FPP gesture, new cues. Cooldown unchanged 52 s. |
| Attacking | **CURSE: DISORIENTED** | 32 s (set here). A thrown voodoo doll (aimed lob 12 m/s, 10 m, set here); the first player within 1.2 m of its landing, or struck in flight, is DISORIENTED 4 s. |
| Defending | **CURSE: VULNERABLE** | 32 s (set here). A pin through a doll: an aimed cone 60 degrees, 7 m; every attacker in it is VULNERABLE 5 s. While any attacker is Vulnerable, Phaister may leave the box to tag them. |
| Ultimate | **HIGOP** (the black hole) | 15 points (set here). A SLOW cast: a 2.2 s wind-up (set here) in which power surges through her (her dress and hair lift and whip upward, a crown of pins rises, the sky darkens) and the hole opens at an aimed spot up to 8 m. For 5 s (set here) it pulls every other player and every loose slipper that is not hers toward its centre. No escape (owner): bodies CAN push against it and gain ground for a moment, then are sucked back (the pull speed exceeds run speed by 1.5 m/s and grows toward the centre). No mashing. At the end it collapses with a thump and releases everyone standing at the rim. |

### 3.6 Pyro, Electro and Hydro: placeholders

Sean, Zack and Rafi move to the four-slot shape with PLACEHOLDER role abilities that cast and do
nothing, the way Amihan's first kit did (`HERO-8`). Their current Skill 1 and ultimate stay as they
are until the owner designs them (the placeholders are only the new role slots).

## 4. The skill tree: UI off, skills hardcoded

- One switch in Core: `HeroLoadoutRules.SidegradesOpen = false`. When false, every kit is built on
  the DEFAULT variant whatever the saved build or the wire says (`HeroAbilitySystem.ConfigureLoadout`
  and `UpdateLoadout`), so every peer plays the same fixed kit.
- Doors hidden while it is false: `HubHome` SKILL TREE button and its notice, `HubHero`'s
  ALTERNATIVES IN THE SKILL TREE, `ConvertedCharacterSelect`'s LOADOUT door,
  `ConvertedMatchSetup`'s lobby loadout button, `TumpSkillView`'s variant buttons.
- Kept, untouched: `HeroLoadout.cs`, `HeroBuildRules`, saved builds, challenges, the screens'
  code. Turning it back on is the one switch.

## 5. Presentation, per ability (the direction baseline's six beats)

Every new ability gets: its own cast body clip (`HeroAbilityClips.<Hero>.cs`, baked by that hero's
motion author), its own FPP gesture (`ViewmodelArms` action), its VFX class in its hero's own
`*Vfx.cs`, a cast cue plus contact and end cues in its hero's own audio builder
(`tools/build_<hero>_audio.py`, three layers each, no two recipes shared), a glyph drawing in
`tools/build_ability_icons.py`, the status icons for the four new statuses, the hold-to-read text,
bot use in `AIController`, and a world snapshot kind for anything that persists (the chilling field,
the wall, the barrier, the haunt, Kuro's guard, the black hole, Kuro playing).

Motifs per power: Cryo, faceted six-sided frost and a pale ice-blue core; Geo, stepped stone slabs
with his gold veins, dust; Necro, Kuro's ink smoke and teardrop wisps; Voodoo, pins, stitched thread
and doll cloth. The FEARED animation and the Concussed stumble are shared clips on all rigs (the
`RootedAnimationAuthor` precedent). Higop's cast gets its own long clip with cloth and hair driven
upward (bones added to her rig only if the cloth pieces are separate nodes; otherwise scale and lift
of the dress pieces).

## 6. Order of implementation

1. Core: new statuses, rules classes (`CryoRules`, `GeoRules`, `NecroRules`, `VoodooRules`),
   `SidegradesOpen`, Core.Tests for every number. (Runs here: `dotnet test`.)
2. Skill tree switch and hidden doors.
3. Motor statuses (timers, immunity, feared flee, concussed wobble, vulnerable reach), `SyncUnit`
   fields, protocol bump.
4. Cheska's kit, then Dante's, then Phaister's (blink refine, the two curses, Higop), then Nemu's
   signature, fetch and guard.
5. Placeholders for Sean, Zack, Rafi.
6. Presentation per ability (clips, VFX, audio, icons).
7. KURO PLAYS (its own design pass first).
8. Bots, snapshots, replays, screens, HUD; then the Unity verification a separate chat runs.

## 7. Numbers set here, for the owner's review

Frostbite load 10 s; Glacial Wall arc 4.2 m, radius 3.0 m, life 8 s; Shield cooldown 45 s, Boulder
30 s and 14 m/s, the boulder does not knock the can, Barrier lets bodies through, Earthquake 14
points; Terrify 40 s, 7 m, haunt 4 s, 2.2 m; Kuro Fetch 30 s, 9 m/s; Kuro Guard 30 s, 1.6x, 6 s,
6 m/s, 0.35 s think; Kuro Plays 16 points; Disoriented doll 32 s, 12 m/s, 10 m; Vulnerable cone
32 s, 60 degrees, 7 m; Higop 15 points, 2.2 s cast, 5 s pull, 8 m; statuses Concussed 3 s,
Feared 1.5 s, Disoriented 4 s, Vulnerable 5 s.

## 8. Working without Unity (owner: *"u try to figure out ways aroudn not having unity engine"*)

- Core rules and their tests run here (`dotnet test Core.Tests`, .NET 9 installed in the cloud box).
- Python builders (models, audio, icons) run here; models are checked with an out-of-engine toon
  renderer (labelled concept renders), sounds by their own analysis.
- Unity C# is written against the existing APIs read from source and checked by a syntax pass;
  compile, EditMode, PlayMode and captures are the separate testing chat's, and each batch's handoff
  names exactly what was not run.
