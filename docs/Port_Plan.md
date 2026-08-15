# Port Plan — Tumbang Preso, Godot 4.7 → Unity 6

**Status:** Phase 0. Written 2026-08-15.
**Source of truth for the port:** the GDScript in `DOST-GameDev`, not `docs/Design.md`. See §7.1.

---

## 1 · What is actually being ported

Measured on `main` @ `bc4d710`, 2026-08-15.

| | `main` | `online/dedicated-lobbies` |
|---|---|---|
| GDScript | 40,435 lines / 126 files | 48,672 lines / 145 files |
| Scenes `.tscn` | 100 | 114 |
| Resources `.tres` | 15 | 15 |
| GLB models | 81 | 81 |
| Textures / audio | 83 PNG · 49 WAV · 2 MP3 | same |
| Files touching netcode | 47 | 47 |
| Autoload singletons | 9 | 9 |
| Existing C# | none | none |

Export targets today: Windows Desktop, macOS, **Linux Server** (the Singapore VPS build).

**Assets carry over. The 100 scene trees and all 47 netcode files do not.**

---

## 2 · The principle: port the rules, rebuild the presentation

The 40k lines are not one kind of code, and treating them as one is how ports fail. Three
tiers, each with a different rule:

**Tier 1 — preserve exactly.** The rules layer: match rotation, scoring, stamina, throw
legality, the hit window, confinement, combat geometry. This is months of *measured*
balance work, documented number by number with the reasoning in `Design.md`. Any drift here
is a silent regression that no compiler catches and no playtest reliably surfaces. This tier
gets ported to **plain C# with zero Unity references** and locked down with unit tests
before a single MonoBehaviour exists.

**Tier 2 — re-architect, same shape.** The netcode. Good news: it is already
server-authoritative with hand-written explicit RPCs. The recurring triplet is

```
@rpc("any_peer","call_remote")  _rpc_request_X()   →  client asks
                                host_resolve_X()   →  host decides, host writes the score
@rpc("authority","call_local")  _rpc_X()           →  host tells everybody
```

That maps almost 1:1 onto Unity. They deliberately moved *away* from property-sync magic
(`lata.is_upright` uses an explicit RPC because a `MultiplayerSynchronizer` writes
properties directly and a setter's `signal` never fires on the receiving peer). That
decision, made to fix a Godot bug, is what makes this tier mechanical rather than a redesign.

**Tier 3 — rebuild native.** The 21 UI screens, `character_visual.gd` (2,086 lines), audio
wiring, VFX, emotes. Transliterating Godot `Control` nodes into Unity UI produces bad Unity
code that is harder to maintain than a rewrite. These get rebuilt to Unity idiom against the
same designs.

---

## 3 · Phases

Each phase has an exit criterion. Do not start the next one until it is met.

### Phase 0 · Tooling
*Human hands. Nothing else can start.*

- .NET SDK 9 (`winget install Microsoft.DotNet.SDK.9`)
- Unity Hub, then newest **Unity 6 LTS**
- Modules at install time: **Windows Build Support (IL2CPP)**, **Linux Dedicated Server
  Build Support**, Mac Build Support if macOS is still a target

⚠️ **Linux Dedicated Server Build Support is the one that gets skipped.** It is what the
Singapore VPS build needs, and adding it later means a second multi-GB module download.

**Exit:** `dotnet --version` responds, and Unity opens an empty URP project.

---

### Phase 1 · `TumbangPreso.Core` — the rules, in pure C#
*No Unity reference. Buildable and testable with `dotnet test` alone.*

This is the crown jewels and it goes first precisely because it does **not** need Unity.

| File | Carries |
|---|---|
| `Balance.cs` | every constant, transcribed from the `.gd` files |
| `MatchRules.cs` | `DefenderSlotFor(round)`, round state, the four scoring events |
| `Roster.cs` | 12 person rows, 4 cans, 4 slippers, `TraitScale(points, perPoint)` |
| `Stamina.cs` | pool, drain, regen, delay, sprint floor, fatigue, the speed-zone stack |
| `ThrowRules.cs` | `CanThrow`'s five conditions, the hit window, `SolveArc` |
| `Confinement.cs` | the square clamp, X and Z independent |
| `Combat.cs` | `v²/FRICTION` impulses, shove/lunge/punch range and arc, stagger `Max()` |

**Constants already extracted from source** (these are the real values, several of which
`Design.md` no longer agrees with — see §7.1):

```
SPEED 4.6 · ATTACKER_SPEED_SCALE 0.75 · SPRINT_SCALE 1.50 · FRICTION 30.0 · GRAVITY 20.0
STAMINA_MAX 60.0 · DRAIN 40.0 · REGEN 20.0 · REGEN_DELAY 1.0 · SPRINT_FLOOR 7.5
FATIGUE_TIME 2.0 · FATIGUE_SPEED_SCALE 0.75 · CONFINEMENT_RADIUS 7.0
SHOVE  speed 12.247 · stun 1.25 · cost 25.0 · cd 7.5 / miss 2.0 · range 1.6 · arc 70°
LUNGE  charge 0.5 · speed 7.746 · radius 1.3 · active 0.45 · cd 1.5
PUNCH  range 1.7 · arc 75° · cd 0.9
THROW  charge 2.5 · min power 0.35 · lock 1.25 · pickup 1.4 · launch 18.5 · hit radius 0.23
LATA   interaction 1.6 · reset channel 1.5 · tilt 88° · topple 0.22 · HIT_MARGIN 0.30
SCORE  knockdown 100 · tag 100 · sabotage 50 · defense 10/s · tag stun 5.0
MATCH  4 rounds · 4 players · 90 s · intermission 3.0 · throw restore cd 1.25
TRAITS speed ±5% · power ±7% · grit ±7%, on 1..5 with 3 neutral
```

**Exit:** `dotnet test` green, with every measured value in `Design.md` reproduced as an
assertion (§6).

---

### Phase 2 · Unity skeleton and the asset pipeline

- Project layout with **asmdefs**, so `TumbangPreso.Core` physically cannot acquire a
  `UnityEngine` reference by accident
- 81 GLB models: either the **glTFast** package, or re-export from Blender as FBX. Decide by
  testing one rigged character both ways, because rig import is where they differ
- 15 `.tres` roster resources → **ScriptableObjects**
- Godot `InputMap` → Input System asset. The full keymap is already recovered from
  `project.godot`: WASD, Shift sprint, Space jump, LMB `special_ability`, E `grab` **and**
  `lunge` (contextual, and RMB is the second lunge binding), B emote wheel, H clean feed,
  R ready up, Ctrl spectator down, F11 fullscreen

**Exit:** one character imports with its rig intact and renders correctly under URP.

---

### Phase 3 · Vertical slice — one player, offline, no netcode
**This is the feel gate and the highest-risk phase in the port.**

Godot's `CharacterBody3D.move_and_slide()` and Unity's `CharacterController` resolve
collisions differently. Movement feel will not survive the port for free, and if that is
discovered in Phase 6 the whole thing has to be re-tuned with the netcode already built on
top of it. Catch it here, on one player, with nothing else in the scene.

Scope: movement, sprint, stamina, fatigue, the square confinement, one lata, one slipper,
charge → throw → knockdown → reset.

**Exit, measured and not by vibes**, side by side against the Godot build:
- one full sprint covers the same distance (§7.1 must be settled first)
- the throw arc lands where the Godot build's lands, per slipper skin
- lunge still tags at 3.20 m against both a stationary and a crossing target

---

### Phase 4 · Full offline match

4 seats, the AI controller, role rotation, cumulative scoring, HUD.

The AI ports cleanly and this is worth stating: `character_base.gd` routes bot input through
`ai_set_intent()` / `input_pressed()` so **a bot presses the same buttons a human does**, and
one `_physics_process` serves both. That indirection is the reason Phase 4 is mostly a
transcription rather than a redesign. Keep it.

**Exit:** a complete 4-round match against 3 bots, with per-seat scores in the range the
existing `ai_probe` and `fair_probe` baselines report.

---

### Phase 5 · Netcode

**Recommended stack: Mirror.** The existing code hand-writes every RPC and resolves
authority explicitly, which maps directly onto `[Command]` → server method → `[ClientRpc]`.
Mirror's dedicated-server story is also the closest to what already runs on the VPS.
**Netcode for GameObjects** is the official alternative and maps nearly as well
(`[Rpc(SendTo.Server)]` → `[Rpc(SendTo.Everyone)]`); it is better documented and worse at
getting out of your way. **This decision does not block Phases 1 to 4 and should be revisited
when Phase 5 actually starts.**

Scope: the request/resolve/broadcast triplet, reconnection tokens, lobby leader election,
join codes, spectators, mid-match arrival rulings, late-joiner state sync, and the dedicated
Linux server build.

**Exit:** 4 real peers on the Singapore VPS, and a reconnect that restores the player's seat.

---

### Phase 6 · Presentation parity

21 UI screens, audio manager and its mix levels, VFX, emotes, spectator camera, offscreen
indicators, character visuals.

⚠️ **Bring `vo_import.py`'s magic-byte sniffing across.** Voice and music have arrived
mislabelled twice (AAC-in-3GP named `.wav`, MP3 named `.wav`), and a silently-failed load is
indistinguishable from "not recorded yet".

---

### Phase 7 · The probe harness

`mech_probe`, `fair_probe`, `trait_probe`, `ai_probe`, `lata_floor_probe`, `hit_probe`.

**Not optional and not last because it is least important.** These are how every number in
`Design.md` was established. Without them, "the balance survived the port" is an opinion.
Much of what they measure now lives in `TumbangPreso.Core` and becomes a unit test instead
(§6); what remains is the whole-match statistical runs, which need a headless Unity build.

---

## 4 · What ports better than expected

- **Contact is a distance check on the host, never an `Area3D`.** Tag, slipper contact and
  the reset ring are all plain distance maths, adopted after `hit_probe` measured 16 of 36
  overlaps failing. That means the most correctness-critical code in the game has **no
  physics-engine dependency at all** and moves to C# untouched.
- **Every point is awarded in one function**, `MatchManager.add_score()`, host-side. One
  place to port, and one place that can be wrong.
- **Role is a pure function**, `(round - 1) % 4`, not an accumulated counter. Nothing to
  desync and nothing to migrate.
- **The AI presses buttons**, it does not call gameplay methods directly.

## 5 · What will hurt

| Risk | Why | Mitigation |
|---|---|---|
| **Movement feel** | `move_and_slide` ≠ `CharacterController` | Phase 3 exists solely for this |
| **`Engine.time_scale` hitstop** | writes a **global** for 60 ms | Unity `Time.timeScale` is also global; keep it off the body block, as Godot already does |
| **Physics interpolation** | `physics_interpolation=true` project-wide | Unity interpolates per-Rigidbody; `CharacterController` needs it done by hand |
| **`SPAWN_SETTLE_FRAMES`** | an expensively-diagnosed physics fix (B-100), triggered by role rotation | Port it deliberately, do not assume Unity does not need it |
| **Perch shedding** | `_shed_character_perch()`, from live play with 3 attackers on one box | Unity `CharacterController` stands on capsules just as happily |
| **12 roster rows must stay distinct** | two pairs were byte-identical once and invisible on the select screen | `trait_probe` asserts distinctness; keep that assertion |
| **Entry 0 of each prop list must stay neutral** | it is what an unpicked prop wears, so a non-neutral row silently retunes every AI seat and every peer that never opened the CHARACTER screen | Unit test |

---

## 6 · The verification spine

`Design.md` records not just the numbers but the **measurements taken against them**. Each
becomes a unit test in Phase 1, which is what converts "we think the port preserved balance"
into something a CI run answers:

| Measured in Godot | Assertion |
|---|---|
| sprint to empty | exactly `STAMINA_MAX / DRAIN` s |
| fatigue lockout | exactly 2.00 s, regen locked for its whole duration |
| empty → full | 2.97 s |
| shove knockback | 2.40 m against the 2.50 predicted by `v²/60` |
| lunge reach | 3.20 m, **identical** against a stationary and a 3.45 m/s crossing target |
| trajectory preview vs flight | 0.000 m miss on TSINELAS / PANTULOG / IKE, 0.263 m on CROCS |
| hit window per can | BOYBEN 0.493 m, PASIP 0.579 m |
| defender rotation | every slot defends exactly once across 4 rounds |
| passive defence share | ≤ 50% of all points under a `turtle` taya |
| roster distinctness | all 12 person rows differ |

---

## 7 · Decisions and open questions

### 7.1 ⚠️ `Design.md` has drifted from the code. Settle this before Phase 1.

`Design.md` opens with *"a number in the code must match a number here, or one of the two is
a bug."* Two are currently out of sync, and in both cases **the code is newer and the prose
is stale**:

| | Code | `Design.md` |
|---|---|---|
| `STAMINA_MAX` | **60.0** (`character_base.gd`), so 1.5 s of sprint at 40/s | §3's table says 60 and 1.50 s, but its ⚠️ note, the §2.5 "MEASURED" block and the §5.3 shove maths all still say a **50**-point pool and **1.25 s** |
| throw legality | `is_inside_box()` against `CONFINEMENT_RADIUS` **7.0** | §5.1 still writes the gate as `max(\|x\|,\|z\|) >= 5.0` |

This matters more than a doc tidy. §3 argues that `STAMINA_MAX`, `STAMINA_DRAIN_RATE`,
`SPRINT_SCALE` and `CONFINEMENT_RADIUS` are **one interlocked set** — "move the box and you
change what a sprint buys" — and its headline finding, that one full sprint covers 6.84 m
and is dimensioned to one crossing of the danger zone, is computed off a 50-point pool the
code no longer has. At 60 the sprint is 20% longer than the finding assumes.

**Action:** confirm which is intended, fix the losing side in the Godot repo, re-run
`mech_probe`, and only then transcribe into `Balance.cs`. Porting from a stale doc bakes the
drift into the new engine permanently.

### 7.2 Settled

- **Unity, not Unreal.** Ruled out on RAM at the time of the decision, and the 3D-authoring
  work stays in Blender either way.
- **Rules layer in engine-free C#.** Non-negotiable; it is the whole verification strategy.
- **Phase 3 before any netcode.** Feel is the risk that compounds.

### 7.3 Open

- Netcode stack, Mirror vs NGO. Deferred to Phase 5 and blocks nothing before it.
- GLB via glTFast vs FBX re-export. Decide in Phase 2 by testing one rigged character.
- Whether the Python map builders (`build_eskinita.py`, `mapkit`) keep generating geometry
  or are replaced by authored Unity scenes. They currently derive the chalk from
  `CONFINEMENT_RADIUS` by regexing `character_base.gd`, which will not survive as written.
