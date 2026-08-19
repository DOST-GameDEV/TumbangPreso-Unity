# Port Plan — Tumbang Preso, Godot 4.7 → Unity 6

**Status:** Phases 0 to 2 done. Phase 3 code complete, feel unverified. Written 2026-08-15.
**Source of truth for the port:** the GDScript in `DOST-GameDev`, not `docs/Design.md`. See §7.1.

| Phase | State |
|---|---|
| 0 · Tooling | **done.** Unity 6000.5.8f1 + Linux Dedicated Server module, .NET SDK 9.0.317 |
| 1 · Rules core | **done.** 32 tests green via `dotnet test` |
| 2 · Skeleton + pipeline | **done.** URP, Input System 1.20, glTFast, asmdefs, 280 art files imported |
| 3 · Vertical slice | **code complete, NOT verified.** Needs a scene with prefabs wired, then the feel measurement below |
| 4 · Full offline match | code present (`MatchBootstrap`, `AIController`), unrun |
| 5 · Netcode | NGO 2.13.1 + UGS (Multiplay, Lobby, Relay, Auth). Seam and core net classes present. |
| 6 · Presentation | audio cue table done and validated both ways; `StatusStack` done. **21 UI screens, character visuals, camera and emotes outstanding** |
| 7 · Probes | structural checks only. The distributions are not measured yet |

**Verified green:** 32 Core tests via `dotnet test`, 8 Unity EditMode tests, the headless
compile check, and the audio cue check. 32 C# files, ~4,600 lines.

⚠️ **"Code complete" is not "done", and Phase 3 is the case that matters.** Its exit criterion
is a MEASUREMENT against the Godot build, not a compile. Nothing in this port has been played.

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

⚠️ **THE ART IS PLACEHOLDER AND IS BEING REPLACED. SEE §8.** Import work in this phase is
about proving the PIPELINE, not about finishing the look. Do not sink time into materials or
rig cleanup for a mesh that is scheduled to be replaced.


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

⚠️ **THE SEAM IS ALREADY IN, SO THIS DOES NOT BLOCK ANYTHING.** `NetAuthority` answers the one
question every verb has to ask before it acts ("do I decide this, or do I ask?"), and today it
answers "you are the host, there are no peers", which is exactly true of single player.
`AddScore`, `ResolveTag` and `HostKnockDown` are already guarded through it.

That matters because authority is not a layer you bolt on afterwards: it is a sentence in the
middle of every verb, and retrofitting it means touching every file again and getting one
wrong. The original shipped exactly that bug. The punch and the shove both sent a request when
not the host; the lunge, added later, guarded its sweep with "if not networked or host" and had
**no else branch**, so it never ran on a client, and never ran on the host either because the
host returns at its authority gate before stepping a body it does not own. **The taya's primary
tag verb was dead for three of the four players in every networked match.**

⚠️ **What is deliberately NOT abstracted is the RPCs themselves.** A generic message layer over
Mirror and NGO would be a worse version of both, and the original's netcode virtue is that its
RPCs are explicit and hand-written. Those get written natively, once, against the chosen stack.

**Chosen stack: Netcode for GameObjects (NGO) 2.13.1 + Unity Transport (UTP) 6.5.0.** Decided
2026-08-19. Earlier drafts recommended Mirror because Mirror's `[Command]` and `[ClientRpc]`
attributes and standalone server model mirrored the Godot VPS architecture closely. NGO was
selected instead because it ships native first-party Unity 6 integration, aligns with Unity
Gaming Services (UGS), and pairs directly with Unity Transport without third-party transport
shims. `MatchRpc` uses explicit `[ServerRpc(RequireOwnership = false)]` and `[ClientRpc]`
attributes across all gameplay verbs.

**Unity Gaming Services (UGS) integration:**
- **Dedicated hosting (Multiplay Hosting):** Replaces the Singapore VPS, `spawner.py`, systemd
  units, `deploy.sh`, `POOL_ADDRESS`, and the 8910-8917 port range. Fleet allocation spins up
  dedicated server instances on demand. Scaling ensures servers exist when players need them
  and prevents the memory exhaustion that crashed the 946 MB VPS twice during peak play.
- **Online discovery (UGS Lobby):** Replaces the raw UDP pool browse in `ServerQuery`. Live
  lobbies publish custom data using the game's 4-character confusable-free join codes.
  `ServerQuery` retains its LAN-first code resolution order (checking `LanBeacon` before UGS
  Lobbies).
- **NAT traversal (Relay):** Retained as a fallback for peer-hosted online matches without
  requiring port forwarding or VPNs.
- **Player identity (UGS Authentication):** Maps authenticated `PlayerId` to the existing stable
  player token, with automatic fallback to local minted tokens for offline LAN play.
- **Lobby rules invariant:** Seating, reconnection, seat reclamation, picks, and ready counts
  remain strictly inside the transport-agnostic `LobbySession` and NGO RPCs, never delegated
  to async cloud service calls.

Scope: the request/resolve/broadcast triplet, reconnection tokens, lobby leader election,
join codes, spectators, mid-match arrival rulings, late-joiner state sync, UGS Relay/Lobby, and
the Linux dedicated server build with Multiplay.

**Exit:** 4 real peers on an allocated Multiplay server (and via Relay peer host), and a
reconnect that restores the player's seat. (Updated from the original Singapore VPS exit
criterion, which was retired when hosting moved to Multiplay fleet allocation).

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

### 7.1 ✅ RESOLVED. `Design.md` drifted in four places, and all four are stale prose.

**Investigated 2026-08-15 against the Godot repo's git history. Full evidence in
[`Design_Drift_Report.md`](Design_Drift_Report.md).**

**Every constant in the shipping build is deliberate, human-instructed and correctly derived.
`Balance.cs` needs no change, and the Godot build needs no gameplay change.** Transcribing from
the GDScript rather than the doc was not merely cautious: three of these four numbers would have
been wrong had the doc been trusted.

The lunge, which looked worst, is the clearest case. `LUNGE_SPEED` went 12.247 → 7.746 in
`071061c` on explicit instruction (*"a short 1-meter forward dash"*), re-derived properly as
`sqrt(1.0 × 60)`, **and the reach loss was compensated in the same commit by giving the taya a
second tag verb**, the punch, which covers exactly the close-range case the shortened lunge
gives up. Not a regression. `Design.md`'s §2.6 measurement simply predates it.

⚠️ **One thing remains genuinely unmeasured:** the nerf was compensated *in design*, and no
`fair_probe` has been run since to confirm it was compensated *in practice*. That is a run on
the **Godot** build, not a port task, and it is worth doing before nationals.

The original statement of the problem is kept below, because the reasoning that found it is
what a future reader needs, not just the answer.

### 7.1a ⚠️ The original finding: `Design.md` disagrees with the code in four places.

`Design.md` opens with *"a number in the code must match a number here, or one of the two is
a bug."* Four are currently out of sync, and in **every** case the code is newer and the
prose is the stale half. Found while transcribing `Balance.cs`, which is exactly the job
that finds them.

| | Code says | `Design.md` says | Weight |
|---|---|---|---|
| `STAMINA_MAX` | **60.0**, so **1.5 s** of sprint at 40/s | §3's table agrees, but its ⚠️ note, the §2.5 "MEASURED" block and the §5.3 shove maths all still describe a **50**-point pool and **1.25 s** | high |
| **lunge reach** | `LUNGE_SPEED` **7.746** → a 1.0 m dash → **2.30 m** total reach | §6's table agrees ("a 1.0 m dash by v²/60"), but its §6 prose and the **§2.6 measurement** still describe `LUNGE_SPEED` **12.247**, a 2.5 m dash and a **3.20 m** reach | **highest** |
| reset channel | `1.5 / trait_scale(bilis, 0.05)` → **1.36 s** PASIP, **1.67 s** BOYBEN | §6 says **1.30** and **1.79**, which need ~8% per point | low |
| throw legality | `is_inside_box()` against `CONFINEMENT_RADIUS` **7.0** | §5.1 still writes the gate as `max(\|x\|,\|z\|) >= 5.0` | low |

**The lunge is the one that matters.** It is the taya's primary scoring verb, and the
current constant gives it **less than three quarters of the reach the balance doc reports as
measured**. §2.6's conclusion that *"the tag is a lead problem, not a reach problem"* was
drawn at the old value, and TAG's share of all points is one of the numbers `fair_probe`
gates on. Either the lunge was nerfed deliberately and every measurement downstream of it is
stale, or the constant was changed by accident and has been shipping wrong.

**The stamina one is second.** §3 argues that `STAMINA_MAX`, `STAMINA_DRAIN_RATE`,
`SPRINT_SCALE` and `CONFINEMENT_RADIUS` are **one interlocked set** ("move the box and you
change what a sprint buys"), and its headline finding — one full sprint covers 6.84 m and is
dimensioned to one crossing of the danger zone — is computed off a 50-point pool the code no
longer has. At 60 the sprint is 20% longer than the finding assumes.

**Action:** decide which side is intended for each, fix the losing side **in the Godot
repo**, re-run `mech_probe`, and let the answer arrive here as a constant change.
`Core.Tests` currently asserts the CODE and names the disagreement in a comment on each
affected test, so nothing is silently baked in either direction.

### 7.1a One formula was recovered rather than transcribed

The per-can hit windows (BOYBEN 0.493 m, PASIP 0.579 m) do not come from dividing the whole
window by STANCE, which yields 0.465 for BOYBEN. `lata.gd:188` divides **only `HIT_MARGIN`**,
and that reproduces both published figures to three decimals. Worth recording because the
wrong reading is the natural one, it is off by only ~6%, and it would have shifted every
knockdown in the game by an amount no playtest would name.

Likewise Design.md's body-block pair (4.238 against 5.618 m/s) reads as one slipper against
two blockers and is in fact **two slippers against one blocker**: their ratio is exactly the
IMPACT ratio 1.14 / 0.86. Both reproduce against Jun-Jun.

### 7.2 Settled

- **Unity, not Unreal.** Ruled out on RAM at the time of the decision, and the 3D-authoring
  work stays in Blender either way.
- **Rules layer in engine-free C#.** Non-negotiable; it is the whole verification strategy.
- **Phase 3 before any netcode.** Feel is the risk that compounds.
- **Netcode stack: Netcode for GameObjects 2.13.1 + UTP 6.5.0 + UGS.** Settled 2026-08-19.
  Multiplay replaces VPS hosting, UGS Lobby replaces pool query, Relay handles peer-to-peer NAT.

### 7.3 Open

- GLB via glTFast vs FBX re-export. Decide in Phase 2 by testing one rigged character.
- Whether the Python map builders (`build_eskinita.py`, `mapkit`) keep generating geometry
  or are replaced by authored Unity scenes. They currently derive the chalk from
  `CONFINEMENT_RADIUS` by regexing `character_base.gd`, which will not survive as written.

---

## 8 · Every asset is being replaced with the team's own

**Decided 2026-08-15.** The 280 files copied into `Assets/TumbangPreso/Art/` are the Godot
build's current art, brought over so the game can RUN during the port. **They are a working
set, not the shipping set.** All of it is to be replaced with the team's own work.

### 8.1 Why this is not only an originality preference

Two of the current assets are things a competition entry should not be carrying, and both are
recorded in the Godot repo's own comments rather than being a discovery:

- ⚠️ **The IKE slipper mesh carries the real Nike wordmark as geometry.** The display name
  was already shortened from "SIKE" to "IKE" because only "IKE" read legibly in play, and the
  roster comment states plainly that the N could not be swapped for an S *without editing the
  mesh, which was out of scope*. Replacing the model is what actually resolves it. **This one
  is not cosmetic and should go first.**
- **All twelve people are CC0 Kenney rigs**, recoloured through a palette. That is a
  legitimate and well-chosen placeholder, and the roster header is explicit that a character
  is "a rig plus a palette, not a new model". It is still someone else's rig in a piece of
  work being judged.

### 8.2 What the replacement must preserve

The art can change freely. These properties cannot, because gameplay is measured against them:

| Must hold | Why |
|---|---|
| **Index order in every roster list** | `character_index`, `can_index` and `slipper_index` cross the wire as bare ints. Append only; never reorder or delete, or two peers on different builds render different people |
| **The lata silhouettes stay distinguishable at arena distance** | The four cans are told apart by SHAPE, not colour, and each one's STANCE/RESET/REBOUND row was tuned *against the mesh that was drawn*. A new can with a different profile needs its row re-derived, not copied |
| **Can body radius stays roughly 0.108 to 0.143** | The collider is fitted from the mesh bounds at runtime, but the hit WINDOW is skin-independent except through STANCE. A wildly different radius changes how the two relate |
| **A slipper's rest height** | CROCS already misses the trajectory preview by 0.263 m purely because it rests 0.161 m off the ground against the others' 0.034 to 0.056. Taller props make that worse |
| **Character capsule proportions** | The 1.25 eye height, the 1.6 capsule and the viewmodel arms belong to the Person ROLE, not to any model, and the visual aligns by MEASURING the instanced mesh. A new rig must not assume a different scale |
| **No text rendered on any roster prop** | Currently true by rule. Keep it: it is what makes the props localisation-free and trademark-free |

### 8.3 Order to replace in

1. **IKE**, for the wordmark. It is the only item with a reason beyond preference.
2. **The four lata and four tsinelas.** Small, high-visibility, already drawn by the team once,
   and each is a single prop with no rig.
3. **The twelve people.** Largest job by far, because it is twelve rigs plus animation
   retargeting. Unity's Humanoid retargeting is the reason this got easier in the port: with a
   Humanoid avatar set up, the animations survive a rig swap.
4. **Environment kits and UI art** last, since neither is on the critical path for play.

⚠️ **Replace one at a time and keep the old mesh until the new one is measured.** Every prop
carries tuning that was derived from its shape, and a batch swap makes it impossible to tell
which of eight changes moved a number.

### 8.4 What the first person replacement learned

`zack` is the first row of §8.3 item 3 to land, as `team-zack.glb`, built by
`tools/build_person_voxel.py` from the CC0 `character-female-b.glb`. The old mesh is still in
the repo and still imports; nothing points at it. Four things came out of it that the other
eleven should not have to rediscover.

**The rig is a pile of boxes and so is the replacement.** Seven bones, both meshes rigidly
weighted (every vertex at weight 1.0 to exactly one bone), and one material. A voxel character
is therefore a table of boxes with a bone name and a palette slot each, which is why this is a
script rather than a modelling session. Adding the next character is a new table and a new
palette in the same file.

**The skeleton can be RETARGETED, which §8.2 implied it could not.** The constraint is not
"the bones cannot move", it is "the clips must not be contradicted", and those are different:

- `head`, `arm-left` and `arm-right` translations are never keyed by any of the 32 clips, so
  those bones are free.
- `root`, both legs and `torso` are keyed by four clips, and every track is an ABSOLUTE local
  position, so shifting the rest position and every keyframe by the same vector moves the bone
  and preserves the animation exactly.
- The inverse bind matrices must then be RECOMPUTED. Skipping that skins the mesh against the
  old bind pose and the character comes apart the first time it moves, with no error.

`tools/glb_anim_channels.py` reports which bones a rig's clips actually key, so this is a
measurement rather than a hope. It is what let this character be 33/37/30 legs/torso/head
against the Kenney rig's 24/23/53, which is most of the difference between the reference art
and a recolour of somebody else's proportions.

⚠️ **Limb LENGTHS are a different question from limb POSITIONS.** These limbs have no knee or
elbow, so a clip's hip rotation sweeps the whole leg and doubling the leg doubles the stride the
animator authored. Moving a bone costs nothing; lengthening one costs animation quality. The
legs here grow 36% and nothing else grows at all.

**What a replacement must still preserve**, on top of the §8.2 table, all of it asserted by
`Assets/TumbangPreso/Editor/PersonSwapProbe.cs`:

| Must hold | Why |
|---|---|
| The seven bone NAMES | `CharacterVisual.BuildHandAnchor` and `CharacterAnimator.ResolveChargeBone` both hunt `arm-right` then `arm-left` by string. A miss is one warning in a match log and a tsinelas hanging in the air |
| Authored height 0.7234 | `PersonScale` is one constant of 2.38 for the whole cast |
| The hand's top surface at palm + `HandTopLift` | A carried tsinelas is parked there. A chunkier hand buries it, which is the Godot side's *"js phasing a bit thru it"* |
| The face on the same side as the base rig | `PersonModelYaw` is one constant too, so a rig facing the other way walks, aims and throws backwards. Measured off the slot-8 vertices by `tools/glb_face_side.py`, which put the base rig's eyes at z +0.1596 |
| UVs in Unity atlas rows 0-7 | See below |

**The palette was dead in this port until this landed.** glTFast flips V on import, so a cell
authored in `.glb` row *r* arrives in Unity row *15 - r*, and `Toon.shader`'s `row >= 8` test
was written against the file's rows. It was never true for any character, so all twelve rendered
in Kenney's factory colours with sixteen correct values uploaded to the GPU and nothing logged.
Fixed on `main` separately from this work.
