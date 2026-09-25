# Paete, the ninth hero: the plan

Owner, 2026-09-25: a PLANT hero on the new signature plus role ability system, from **Mount
Makiling**, named **Paete**, with the kit table in [research.md](research.md) section 0, and
*"thoroughly plan how to do it first and research"*. Brief, lore and model plan:
[ArtSource/paete/concept-20260925/design-brief.md](../../../ArtSource/paete/concept-20260925/design-brief.md).
Every effect, sound and animation follows [the baseline](../amihan-kit-2026-09-25/direction.md);
Amihan's kit (`AmihanHeroKit`, `AmihanRules`, `AmihanVfx`) is the worked example of the SHAPE,
never the look.

Status: numbers below marked **(proposed)** wait on the owner's answers (section 6). Nothing
is built until those land.

## 1. Names (the owner asked for better ones)

| Slot | Concept | Proposed name | Why |
|---|---|---|---|
| Signature | Vine Pull | **KAPIT-BAGING** | "hold on to the vine" |
| Attacking | Throwing Slipper Plant | **PUNLANG TSINELAS** | "slipper seedling" |
| Defending | Thorn Pull | **BAWI** | "take it back": the forest's rule, eat the fruit but never carry it home |
| Ultimate | Nature's Wrath | **YAKAP NG MAKILING** | "the mountain's embrace" (the mountain, not the diwata) |

## 2. The kit, mechanically (host authority, distances, Friction)

All contact is resolved by distance on the host behind `NetAuthority.ShouldResolve()`. Every
long move is a carry (`CharacterMotor.BeginCarry`, `Core.CarryRules`): distance
D = v t + v^2/(2 x 30), so t = (D - v^2/60)/v. Numbers live in `Core/PaeteRules.cs`, asserted in
`Core.Tests/PaeteRulesTests.cs`.

### 2.1 KAPIT-BAGING (signature, both roles)

- Press: both forearms unravel into vines that shoot along the aim to an anchor, then he is
  reeled to it. Anchor = the first wall or prop the aim ray meets within **8 m (proposed)**, else
  the floor point at 8 m along the aim. It always travels (Kinich).
- Reel: carry at **14 m/s (proposed)**, stopping 0.8 m short of the anchor: for 8 m,
  t = (7.2 - 3.27)/14 = 0.281 s, plus a small lift (1.5 m/s) for the arc.
- Taya-side and attacker-side identical (signature). **Refused while carrying a retrieved slipper
  inside the taya's box (proposed)**, the same guard as Updraft, so it is never an escape from the
  retrieval (`VISION.md` section 0).
- Cooldown **30 s (proposed)**; one charge.

### 2.2 PUNLANG TSINELAS (attacking role)

- Press: throw a seed up to **6 m (proposed)**; it pops up into a plant (0.3 s). One plant at a
  time; a new cast replaces the old.
- The plant grows one **wooden slipper** at a time. Press again while it has one: it fires at
  where Paete is aiming (the owner: *"he can control when they shoot"*). Reload **15 s
  (proposed)**, from the owner's *"10-20 seconds"*.
- A wooden slipper is NOT one of the four real slippers: it is never picked up and never retrieved,
  and it withers where it lands. What it does on contact is the open question 4.
- The plant lives **30 s (proposed)**; the taya can uproot it with a shove.

### 2.3 BAWI (defending role)

- Press: a thorn construct grows at his feet (0.4 s) and sends thorn vines to every **loose**
  slipper within **7 m (proposed)**. Each one is caught, held a beat (0.25 s, the Scorpion hold),
  then yanked to within 1 m of the construct over 0.5 s.
- Never the lata (scoring stays in `MatchDirector.AddScore`; the Kuro rule). The slipper keeps its
  owner; the owner can grab it once it lands, now beside the taya, which is the point: the
  retrieval gets more dangerous.
- Construct lasts 3 s then withers. Cooldown **30 s (proposed)**.

### 2.4 YAKAP NG MAKILING (ultimate)

- Introduction first (the shared ultimate phase), then: he throws a seed up to **8 m (proposed)**;
  it bursts into a sentry (Groot's Strangling Prison: a bright core, radial spiked vines).
- Every other player within **7 m of the sentry (proposed)** is carried to 1.1 m from it (carry,
  solved per body from its distance) and becomes **Rooted**.
- **Rooted** (new status, appended to `StatusRules` as `StatusKind.Rooted = 5`): no movement, the
  camera forced to third person, **can still throw and cast** (owner). Hold the break-free button
  for **7 s** of holding to escape (owner: *"like 7 seconds"*); progress is kept if they let go
  **(proposed)**, and the sentry wilts at **10 s (proposed)** releasing everyone left. A victim
  pressing the button plays the struggle loop (Dead by Daylight's wiggle: sway left, sway right,
  kick) and fills a ring on their own HUD.
- Cost **16 objective points (proposed)**, like Amihan and Rafi.

## 3. What gets written, part by part

| Part | Where | Notes |
|---|---|---|
| Rules | `Core/PaeteRules.cs`, `Core.Tests/PaeteRulesTests.cs` | every number above, carry times solved |
| Status | `Core/StatusRules.cs` (append Rooted), `CharacterMotor.Status.cs` (rooted timer, hold progress), `SyncUnit` field | icon in `tools/build_ability_icons.py`, body tell (root coils at the shins), start and end cues |
| Kit | `Runtime/Abilities/PaeteHeroKit.cs` | Skill1, AttackingSkill, DefendingSkill, Ultimate; `CanReactivate` for the plant's fire (Nemu's pattern) |
| World objects | `Runtime/Abilities/PaeteHazards.cs` | the plant, the wooden slipper, the thorn construct, the sentry; host-stepped, `WorldEffectSnapshot` kinds appended, `RecordedSpecialFields` for replay |
| Force TPP | `Camera/CameraRig` | the emote-view path (`BeginEmoteView` / `EndEmoteView`), entered and left by the Rooted status |
| Hold to break free | `InputLayer` | an existing button (proposed: Jump, since a rooted body cannot jump), pad and thumb answered per CLAUDE.md 4a; bots press it through `InputIntent` |
| Wire | `MatchRpc` | appended messages and enum values only; `NetSession.ProtocolVersion` to the next free number after 53, with its test |
| VFX | `Visual/PaeteVfx.cs`, `Visual/GrowthVfx.cs` | the growth family (vines, bark, leaves, narra pods, roots), its own builders, shared with nobody |
| Body casts | `Visual/HeroAbilityClips.Paete.cs`, baked by `HeroMotionAuthor` | one per ability plus the struggle loop for any rooted body |
| FPP hands | `Camera/ViewmodelArms` actions | both forearms unravel together for the signature |
| Glyphs | `AbilityGlyph.Paete*`, `tools/build_ability_icons.py` | four drawings plus the Rooted status icon |
| Audio | `tools/build_paete_audio.py`, `AudioCues` | one recipe per ability (wood creak, bark snap, leaf rush, seed pop, thorn rasp, root groan), contact and status cues, the ultimate theme |
| Introduction | `tools/author_ultimate_intros.py` (`paete`), `Visual/HeroIntroductionScene.Paete.cs` | who (still in the mist at the forest edge), intent (the eyes light in their sockets, FOCUSED to ANGRY), gather (roots coil up his arms, the seed glows in his palm), release (the throw; the live ultimate starts at that pose) |
| Sky | `SkyEvent.Look` (new) | Makiling mist: a soft white mist rolling in low, dappled canopy light |
| Lines | `HeroLines` paete rows, `docs/HUMAN.md` | text only; human recordings owed |
| Bots | `AIController` | signature to close or escape (never out of the box with a slipper), plant when a throw line to the lata exists, BAWI when loose slippers lie out of the box, ult when two or more are inside 7 m |
| Wiring | roster row (append), `CreateKitFor`, `HeroGlyphs`, `HeroLoadout` (two rows per slot), `UiTheme` (accent and the amended law), select blurb, portrait, avatar, FPP arms, `CharacterAnimator` action map, roster book motion bake, `MatchReplayArchive` hero list, `ugs/cloud-code/*.js` hero lists, the EditMode tests that enumerate heroes | follows how Amihan was added |

## 4. The six beats, per ability (direction.md section 1)

| | Tell | Release | Travel | Contact | Linger | Dissipate |
|---|---|---|---|---|---|---|
| **KAPIT-BAGING** | 0.12 s: both arms draw back, green glow in both palms, leaves shiver | both forearms snap forward and unravel; two dark vines shoot out with leaves along them | vines reach the anchor, a seed-pod burst marks it; a beat; he is reeled in, arms locked straight, legs trailing, a curved leaf trail behind | none (movement) | the vines hang slack for 0.3 s | vines curl back into the forearms, a few leaves fall |
| **PUNLANG TSINELAS** | a seed glows in the palm | underhand lob | the seed arcs; it lands and pops up (squash and stretch) into a stubby plant with a pod head | a wooden slipper grows in the pod; on fire the head pulls back then snaps forward (recoil) | the plant idles, bobbing, a growing slipper visible | at 30 s it droops, browns and sinks into the ground |
| **BAWI** | he stamps, roots crack out round his feet | a thorn construct bursts up | thorn vines race along the ground to each loose slipper | catch, hold a beat, yank; the slipper tumbles home | the construct, 3 s | it withers, thorns drop |
| **YAKAP NG MAKILING** | introduction; a large glow in the palm | overhand throw of the seed | it lands and bursts into the sentry, spiked vines radial | vines lash out to every body in range, catch, hold a beat, drag them in; roots coil up to their shins | the sentry pulses; the rooted struggle | on wilt the roots recoil into the ground and the sentry dries to a stump, then drops narra pods |

Palette (proposed, fixed on renders): core `eaffd0` (the leaf hue at low saturation), body
`6f9a2e` (moss to leaf), ink edge `2b3a12`; warm accent: narra blossom yellow `f2c14e` is
Amihan's brooch gold, so his warm accent is **bark** `8a5a32`. Motif particles: narra seed pods
and single leaves.

## 5. Order of work

1. Model, one part at a time (brief section 4), rendered beside the cast each step, the owner's
   three questions answered in writing each time.
2. Core rules and tests, the Rooted status, the kit with placeholder VFX, wiring, bots. Playable.
3. VFX, casts, FPP hands, audio, glyphs, per ability, each captured in-engine, criticised, redone.
4. The introduction, the sky, the lines.
5. Verification (CLAUDE.md section 7), TODO HERO-9, the build into `Builds/`.

## 6. Open questions (asked in ONE batch, 2026-09-25)

1. KAPIT-BAGING: anchor on walls, props and the floor only (never a player)? Range 8 m? Refused
   while holding a retrieved slipper inside the taya's box? Cooldown?
2. PUNLANG TSINELAS: is *"10-20 seconds"* the time between shots (one wooden slipper per 15 s) or
   the ability's cooldown?
3. How many shots per plant, and how long does it live?
4. What does a wooden slipper do: knock the lata for Paete's +100? Hit a player (a stun, Whirled,
   nothing)? Is it ever picked up?
5. Does it fire where Paete aims, or always at the lata?
6. Can the taya uproot the plant?
7. BAWI: loose slippers only, or also ones in flight and ones in hands? Range? Grown at his feet or
   thrown to a spot?
8. YAKAP NG MAKILING: pulls everyone within a radius of the sentry, or the whole court?
9. Which button is the break-free hold? Is the 7 s cumulative (letting go keeps progress)? Does the
   sentry expire if nobody breaks free?
10. Can a rooted player be tagged? (With Paete as taya this is the strongest play in the kit.)
11. The ultimate's price in objective points.
12. The four names above.

## 7. The owner's answers, 2026-09-25

| Question | Answer | What it becomes |
|---|---|---|
| 1. Vine Pull | *"yes on 1., i want it to be an escape"* | world anchors only (walls, props, floor), 8 m, 30 s. **No slipper guard**: it may carry a retrieved slipper out of the box. That is the one escape tool in his kit, and it has a 30 s cooldown and a readable 0.12 s tell. |
| 2. Shot timing | (defaults accepted) | one wooden slipper per 15 s, fired on the next press at the aim point |
| 3. Wooden slippers | *"maybe lessened plus"* | a wooden slipper that knocks the lata scores **less than a real knockdown** (+50, the sabotage tier, through `MatchDirector.AddScore`); it withers where it lands and is never picked up |
| 4. Uprooting | *"invincible for the first 15 seconds but after that make a visual indicator showing that it can be pulled out? maybe make the model gradually change too"*, *"i want the animation for pull out to be good and dont make the keybind shove maybe make like a general keybind for interact and remove and shit"* | 15 s rooted and untouchable (upright, bright, roots gripping); then a clear "can be pulled" read (the roots lift out of the soil, the pod head droops, a loose ring of soil) that keeps changing over its remaining life. Any opponent pulls it out with a **new general INTERACT verb** (hold), with its own pull-out animation (grip, lean back, heave, the roots tear free, stumble). |
| 5. Thorn Pull | *"i want it too be ALL, even the ones on the hands"* | every slipper within range: loose, in flight and in hands (a held slipper is yanked out of the hand). Never the lata. |
| 6. Ultimate reach | *"WITHIN 9 meters"* | 9 m from the sentry |
| 7. Break free | *"i suggest the general interact button"* | hold INTERACT, 7 s of holding, progress kept on release |
| 8. Tags | *"a rooted player can be tagged and theyre out of the root after getting tagged"* | a tag ends Rooted (the Tagged status replaces it) |
| 9. Price | *"yes"* | 16 objective points |

Also, on everything: *"thoroughly refine ALL details of EVERYTHINg ur making and do it carefully
dont auto make it all with a script"* and, on the model, *"it has to really look like a tree and
very detailed but blocky"*.

**The INTERACT verb.** One control for "do the thing in front of me" that is not a pickup: pull
out a plant, break out of roots, and whatever needs it later. Keyboard **G** (free in gameplay;
X is Grab, E and F are skills). Pad and thumb are answered when it is added (CLAUDE.md 4a); the
natural pad button, East, currently carries ReadyUp, which is read between rounds, so the two are
reconciled in the same change rather than doubled up.
