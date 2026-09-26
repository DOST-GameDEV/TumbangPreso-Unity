# Amihan's kit, second pass: plan (2026-09-26, cloud)

The method is [HERO_KIT_METHOD.md](../../HERO_KIT_METHOD.md) (Paete's build). The first pass is
[../amihan-kit-2026-09-25/](../amihan-kit-2026-09-25/) (plan, research, direction); its effect family and value ladder stand.
Research for this pass: [research.md](research.md). Cutscene direction: [direction.md](direction.md).

## 0. The owner's updated tables, verbatim (2026-09-26, *"updated skill names and status effects"*)

| Passive | Signature Ability | Attacking Ability | Defending Ability | Ultimate |
|---|---|---|---|---|
| Anemo | **Drift:** Propel forward in the target direction that inflicts Whirled and slightly pushes back other players. *2 Charges, 15 Seconds Cooldown In-Between Use* | **Featherfall:** Propel upward and fly for 5 seconds. You may move or throw slippers while in the air. *40 Seconds Cooldown* | **Whirlwind:** Create an arc-shaped gale that inflicts Whirled to players hit as it swiftly moves forward. The gale lasts for 2.5 seconds. *35 Seconds Cooldown* | **Airburst:** After a 2.5 second delay, unleash a map-wide fan-shaped wind in the target direction that greatly pushes back all players and slippers caught inside almost to the edge of the arena. *15 Objective Points* |
| Cryo | **Cold Feet:** Create a chilling field on the floor that inflicts Chilled indefinitely to players caught inside of it. The chilling field lasts for 7.5 seconds. *2 Charges, 20 Seconds Cooldown In-Between Use* | **Frostbite:** Imbue the slipper with Frozen. Hitting another player with the slipper will inflict them with Frozen. *35 Seconds Cooldown* | **Glacial Wall:** Create an arc-shaped icicle wall that blocks slippers and players. The icicle wall takes 3 slipper hits to shatter. *35 Seconds Cooldown* | **Absolute Zero:** Inflict Frozen to every player, followed by Chilled after thawing. *12 Objective Points* |

| Status | Description | Tooltip |
|---|---|---|
| Whirled | Drops slipper if currently in hand. Prevents slipper retrieval for 2.5 seconds | Disabled Slipper Retrieval |
| Chilled | Decreases movement speed by 50% for 5 seconds. | Reduced Movement Speed |
| Frozen | Prevents movement or interaction for 2.5 seconds. *HUD Frozen Visual Effect.* | Disabled Movement and Interaction |
| Tagged | Prevents movement or interaction for 5 seconds. Cannot be removed or be immune to. | Disabled Movement and Interaction |
| Rooted | Prevents movement for 2.5 seconds. | Disabled Movement |
| Concussed | Prevents movement or interaction for 1.25 seconds. *HUD Concussed Visual Effect.* | Disabled Movement and Interaction |
| Drained | Depletes stamina to 0. Prevents stamina recovery for 2.5 seconds. | Disabled Stamina Recovery |
| Hexed | Removes | Disabled Protection |

The Cryo row and the status changes that are not Amihan's are logged under ABILITY-2 in `docs/TODO.md` for their heroes' passes.

## 1. The owner's notes for this pass, verbatim

1. *"thoroughly make sure amihan's animations look great a bug i found last time was her yellow circle looked weird as fuck
   when she was floating"*
2. *"she didnt have a flaot animation too and any VFX to indicate her shit as well"*
3. *"thoroughly think abt the hold indicators as well for the casting"*
4. *"fot cutscene thoroughly direct it and use genshin impact and other game ULT cutscene animations as reference for
   direction, what happens as welll as vfx sfx animation etc"*
5. *"visually show the win actually assisting her or working in her skills"*
6. *"thorouighly think and create each detail of the skills, all vfx, sfx, animation each part dont js mass prooduce witha
   script"*

## 2. How the table is read (numbers go into `Core.AmihanRules` with the quote on them)

| Point | Reading | Why |
|---|---|---|
| Names | DRIFT, FEATHERFALL, WHIRLWIND, AIRBURST | the table. Ability ids (`amihan_skill1`, `_skill2`, `_skill2d`, `_ultimate`), glyph enum names and cue stems are identities and are kept, never renumbered |
| Drift charges | **2 charges; a spent charge comes back 15 s later, one at a time** | *"2 Charges, 15 Seconds Cooldown In-Between Use"* read as the usual two-charge skill (Genshin's Xiao and Kazuha, Overwatch): both can be spent back to back, and each returns on its own 15 s timer. **Open question for the owner**: if he meant a forced 15 s gap between the two uses, it is one constant (`DriftChargeSeconds`) and one branch. This is a TIMED recharge, which `HeroAbility.Recharge` was written to forbid ("an event, never a timer"); the owner's number overrides that note, and the note is amended where it lives |
| Drift distance and push | 5.0 m carry, Whirled, pushed 1.2 m off her line | unchanged from Quick Dash; not in the table |
| Featherfall length | **5 s** of flight (was 10) | *"fly for 5 seconds"* |
| Featherfall cooldown | **40 s** (was 45, a number the owner had not given) | the table |
| Featherfall in the air | she can move and throw; she still cannot PICK UP until she lands; press again (or Grab) to come down early | *"You may move or throw slippers while in the air"*; the pick-up rule is the owner's first answer (*"cant pick up unless they choose to go down"*) and the new table does not reverse it |
| Featherfall height | 2.8 m, rise 0.45 s, glide down 3.5 m/s | unchanged |
| Whirlwind | unchanged: 35 s, 2.5 s, arc 3.2 m across, rolling 5.5 m/s | the table matches |
| Airburst | unchanged numbers from Storm Surge: 2.5 s gather (the dodge window, the fan on the court), then a map-wide fan (35 degree half-angle, 40 m), carry 16 m, 15 points | the table matches; the cutscene plays before the gather (the phase starts at the commit), see [direction.md](direction.md) section 4 |

## 3. The effect family (unchanged from the first pass, with two rules added)

The first pass's rules (thin bright edges round a darker middle, depth layering, her motif particles of cotton and abel
threads, the kasikus whirl as her one repeating pattern, one warm accent, fade by thinning the line) stand. Added by this
pass, from the footage (research section 4):

7. **Nothing flat under her feet in the air.** Height is the shadow on the court; the wind holding her is drawn round her
   limbs and as streaks rising past her; the court answers with a dust swirl ON it.
8. **The wind is visible wherever it acts on a body**: a gust at her back pushes the dash, a column carries the flight, a
   gale lifts and turns what it hits, and dust and loose slippers show the direction.

## 4. Each ability, six beats (typed by hand, one layer per column)

### 4.1 DRIFT (signature), 0.66 s body

| Beat | Time | Body (everyone) | First person | Effect | Sound |
|---|---|---|---|---|---|
| Tell | 0.00 to 0.09 | a coil: weight back, twisted away, arms swept back; her sash and hair pulled BACK toward the wind behind her | both hands pull in to the chest, the view dips 2 cm | the inhale: four short streaks converge on her back from behind (the gust arriving), dust drawn toward her heels | a short intake of air, rising |
| Release | 0.09 to 0.13 | flung forward off the back foot, the left arm leading low (the spiral), feet off the court | both hands thrown forward and apart, the view kicks forward | a CRESCENT of air bursts at her back (it pushes her), a kick of dust behind her | a cloth snap and a tight whoosh with a fast pitch drop |
| Travel | 0.13 to 0.34 | held, leaning into it | hands trail back, fingers spread | a tube of three streak ribbons wraps her body along the path (Xiao's smear); cotton pulled into her wake | the whoosh passes |
| Contact | on each body passed | (theirs) a stagger sideways and the Whirled spiral at the waist | a small crescent flicks past the screen edge | a crescent of air breaks on the body it passes, the slipper knocked out of the hand | a soft air thump per body, the Whirled whistle |
| Linger | 0.34 to 0.74 | the torso keeps turning past centre, then settles | hands return | two ribbons curl and hang along her line | a tail of air |
| Dissipate | 0.74 to 1.10 | rest | rest | the ribbons thin to threads and drop cotton | none |

Charges: two pips on the deck tile; each refills on its own 15 s sweep.

### 4.2 FEATHERFALL (attacking), 5 s of flight

| Beat | Time | Body (everyone) | First person | Effect | Sound |
|---|---|---|---|---|---|
| Tell | 0.00 to 0.16 | she sinks, palms pressed down at the court, knees bent | both palms push down out of view, the view sinks 6 cm | two ribbons curl round her shins at ankle height, one each way; dust drawn in | a low rumble opening |
| Push | 0.12 to 0.40 | (continues) | | a DISC of air spreads on the court under her (a thin bright rim, 0.4 to 1.8 m), dust puffs pushed outward at uneven angles | a soft thump of air on the ground |
| Release | 0.16 to 0.60 | springs up, arms swept DOWN and back (pushing the air down), head up, legs trailing, the torso turning with the column | the view rises; both hands sweep down and away | a column of vertical streaks shoots up round her (nine lines, each its own radius, height and delay); one ribbon spirals up it; cotton bolls and abel threads flutter up past her | the column's rush rising |
| Hang (aloft) | 0.60 to 5.0 | the float: knees tucked unevenly, one shin forward, arms out for balance; a breath, a dip and correction, a look round, cloth and hair lifted UP; leans into her travel when she moves | hands held out low at the sides with small swirls wrapping each wrist, a slow bob | small swirls wrap her hands and shins; short streaks rise past her from below (the column still holding her); a dust swirl turns ON the court under her, its size from her height; her shadow is the height cue. **No ring under her feet** | a soft fabric flutter loop and a breathing rush |
| Throw from the air | any time aloft | the throw, with her legs kicking back for balance | the normal throw | a puff of air from her hand as the slipper leaves | the throw's own cue |
| Descend and land | the last 0.6 s, or on a second press | arms rise, legs reach down, a soft knee bend on touchdown | the view sinks, hands come in | the streaks fold down into her feet; a ring of dust puffs on the court as she lands | a soft settle |

### 4.3 WHIRLWIND (defending), 2.5 s gale

| Beat | Time | Body | First person | Effect | Sound |
|---|---|---|---|---|---|
| Tell | 0.00 to 0.20 | a deep wind-up, torso twisted hard right, both arms swung back past the right hip | both hands swing out of view to the right | air gathers in a curl at her right hip | a swirl rising |
| Release | 0.32 | the torso whips through left, both arms sweep across at shoulder height | a two-hand sweep across the screen | the ARC is thrown from her arms: a curved front of five ribbons, bright leading edge | a rolling rush that swells |
| Travel | 0.32 to 2.5 | follow-through, recover | rest | the front rolls along the court carrying dust, cotton and any loose slipper in its path | the rush pans with the front |
| Contact | on each body | (theirs) lifted a hand's height and turned half round in it, slipper knocked out, the Whirled spiral | | the front breaks round the body in two small curls | a thump and the Whirled whistle |
| Linger | none | | | it is one front | |
| Dissipate | at 2.5 s | | | the arc frays into threads and drops what it carried | a fraying hiss |

### 4.4 AIRBURST (ultimate)

The cutscene is in [direction.md](direction.md). Live, after it: she plants (the pose the cutscene ends on), the fan lies on
the court from her feet (the dodge window, 2.5 s), the air inside the fan streams TOWARD her (dust and cloth pulled her way,
the pressure rising), then the wall of wind leaves and carries every player and loose slipper still inside it to the edge.

## 5. Hold to cast (CAST-1 applied to her four)

`docs/reports/ability-rework-2026-09-26/cast-preview.md` is the design; these are her shapes, from the same constants the
effects use (one number, never two):

| Ability | Preview while held | Release | Cancel |
|---|---|---|---|
| DRIFT | a PATH: a strip on the court from her feet 5.0 m along the crosshair's ground direction, with her footprint ring at the end and the two charge pips | dashes | free |
| FEATHERFALL | a COLUMN: a faint vertical ring at her feet and one at 2.8 m, with the dust swirl already turning, so she sees how high she will go | lifts off | free |
| WHIRLWIND | an ARC and its PATH: the arc at its birth 1.0 m ahead and a strip 13.75 m long showing where it rolls | throws the gale | free |
| AIRBURST | a FAN: the 35 degree fan's two edges out to range | casts (the cutscene) | free |

Cancel: right mouse, pad B (East), touch drag onto the cancel target. Nothing is spent on a cancel.

## 6. Files (what this pass touches)

| What | Where |
|---|---|
| Numbers | `Packages/com.tumbangpreso.core/Runtime/AmihanRules.cs`, `Core.Tests/AmihanRulesTests.cs` |
| Kit, names, charges | `Runtime/Abilities/AmihanHeroKit.cs`, `HeroAbility.cs` (timed charge recharge) |
| Body clips | `Runtime/Visual/HeroAbilityClips.Amihan.cs` (launch, hang, descend typed key by key), `Editor/AmihanMotionAuthor` |
| Effects | `Runtime/Visual/AmihanVfx.cs` (launch, aloft, landing, dash, gale contact), `WindVfx.cs` |
| Sound | `tools/build_amihan_audio.py` |
| Preview | the cast preview for her four |
| Cutscene | `tools/author_ultimate_intros.py` `amihan()`, `Runtime/Visual/HeroIntroductionScene.Amihan.cs` |
| Film | `Tests/PlayMode/AmihanKitPlayProbe.cs` (`TUMP_AMIHAN_FILM=1`) |
