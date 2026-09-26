# TODO: Tumbang Preso Unity

**How this file is organised (docs cleanup, 2026-09-23).** This file is the ONE status
queue: the current queue and priority order, then one index row for every numbered entry.
Nothing was deleted or renumbered.

- Open numbered entries (§ 155 down to § 69) keep their whole bodies, unchanged, in
  [TODO_Backlog.md](TODO_Backlog.md). A `docs/TODO.md § N` pointer anywhere in the repo
  lands on its index row below, which links to the body.
- Finished entries and batch reports stay whole in [TODO_Archive.md](TODO_Archive.md),
  indexed at the bottom of this file. The queue text as it stood at `a1110a9e`
  (2026-09-22) is preserved verbatim in
  [archive/TODO_queue_2026-09-22.md](archive/TODO_queue_2026-09-22.md).
- Detailed design lives in [NATIONALS_POLISH.md](NATIONALS_POLISH.md); the exact resume
  state lives in [ACTIVE_REWORK_LEDGER.md](ACTIVE_REWORK_LEDGER.md).
- Section numbers are not unique (§ 53, 63, 64, 65 repeat). Search by title too.

## CURRENT IMPLEMENTATION QUEUE

**Standing mandate (owner, 2026-09-21):** "finish everything note yet done", then "i want
every single thing in todo to be done pls mark that in todo and shit". Every unfinished
item in this file, including the backlog index, is in scope. Each item ends with
implementation plus evidence, a specific completed replacement, or a named external
dependency. An old OPEN heading is not proof code is missing, and a task may not be
skipped because it is old. No parent is checked while an actionable child is open.

**Supersession check (owner2026-09-23):** newer instructions and current adopted
designs win. FUTURE.md and archived plans are historical, not independent task
sources. Resolve each old entry as current work, already implemented, superseded
or retired before adding it to execution. An old OPEN label does not reactivate a
proposal. Preserve the original IDs/body and link its replacement or reason; never
restore obsolete behavior or completed-UI layouts merely to clear a checkbox.

**Owner direction, 2026-09-23:** make the game more visually appealing and satisfying to
play without making it more realistic; make the in-game HUD minimalist, professional and
easy to look at; explore on-screen effects and indicators that work together without
overwhelming the player (Sepak U named as the example); rethink priority. This is
VISUAL-1 below. It supersedes the 2026-09-01 "do not touch the in-match HUD" scope note
for this pass and changes no rule, timing, authority or network contract.
Owner, same day: "u can edit all UI and hud in the actual game btw including the match
end and mid round report and icon", "u figure out which to edit and thoroughly revamp
it too and make it more visually pleasing + good to look at". Every in-match surface is
in scope: HUD, prompts, feed, banners, the halftime and round reports, the match-end
board and the in-match icon set (VISUAL-1.4, 1.16, 1.17, 1.18). Owner and girlfriend
supplied artwork is still preserved, and the front-end menus stay as they are.

**Standing sequencing rule (owner, 2026-09-22):** avoid verification and test-repair
loops. Use the smallest check that resolves the risk a change introduced; comprehensive
regression happens once, at final integration. Quality stays the priority.

**Latest quality rule (owner2026-09-23):** inspect before changing. Improve only a
concrete visual, usability or functional weakness; keep successful parts. Apply this
to UX-1 and each REFINE-2 map/character/aspect, without dropping unfinished tasks.

### Priority order, rethought 2026-09-23

**Owner override, 2026-09-26: ASKS-0926 below comes FIRST, newest ask first, and inside it the
Amihan and Rafi remodel (matching Dante and Phaister) is PRIORITY 1** (*"in the todo section
prioritize most recent assks and ask handoff ltr to prioritize it as well"*). Every handoff from here
on points the next session at ASKS-0926 before anything else in this file. The order underneath
stands for everything else.

**Owner override,2026-09-24:** the cloud work pushed in4f62fcc5c is the immediate
priority: native review, actual visual critique and bug fixes before resuming the
older queue. Follow [the local intake plan](reports/cloud-integration-2026-09-24/plan.md).
Preserve every older item. Cloud compile/pose sheets do not establish Unity quality;
VOICE-1 uses human recordings only, and the20remaining world-skill VFX/SFX remain open.

1. **P0, finish what is in flight. ✅ DONE 2026-09-23.** The 134.10 reduced-effects link
   (Hitstop, SkyEvent, CanContactAccent, TumpHudEffects, UltimatePresentationDirector and
   others, plus camera shake) and the native `--accessibility-only` route are published;
   native v57 passed all 15 stages, EditMode 19/19, Core 615/615. Receipts in
   `reports/full-backlog-2026-09-21/accessibility-completion.md`. One 127.3 follow-up and
   the shutdown crash below remain open.
2. **P1, VISUAL-1 batch A: communication and the in-match UI.** Order: 1.4 and 1.18 (the
   HUD and its icon family), 1.6, 1.1, 1.2, 1.3, 1.5, 1.16, 1.17, 1.7. The HUD leads because
   the owner asked for the UI revamp twice and every later capture sits under it; the
   danger work follows because the game's thesis (retrieval risk) has no picture yet.
3. **P2, VISUAL-1 batch B: the world.** 1.8, 1.9, 1.10. Lighting, court and hero objects
   change every frame of every map.
4. **P3, remaining expansion work.** The lagoon deck sampling refinement and any open
   Rafi or lagoon integration items below.
5. **P4, VISUAL-1 batch C with PRESENTATION-1.5.** 1.11, 1.12, 1.13.
6. **P5, VISUAL-1 batch D with the rest of the existing scope.** 1.14 and 1.15 alongside
   152.4 / MAP_FINAL_PASS, 151.9 / 151.19 and 153 / U8 / 149.4 / 145 / 143.
7. **P5.5, UX-1 front-end flow and progression addition (owner2026-09-23).**
   After the existing visible work, implement the new Home and flows below. Preserve
   login and main menu; Home opens from the existing TAP TO START. This is separate
   from the completed in-match UI/HUD pass.
8. **P6, backlog disposition. Source/design review COMPLETE2026-09-23.** All366
   original heading entries are preserved and individually accounted for in
   `reports/full-backlog-2026-09-21/todo-disposition.json`; none remain unreviewed.
   This is not full implementation/qualification completion. Current gaps remain
   explicitly open below; retired FUTURE/layout recipes are not reactivated.
9. **P7, final coherent qualification and build.** PRESENTATION-5.2, the final
   integration item and 152 final delivery on one frozen, identified candidate.
10. **P8, REFINE-2: map-by-map asset and actual-gameplay refinement (owner2026-09-23).**
    After the older actionable work above, thorough reference research and planning,
    then each map individually, natural ambient life, all-bot inactivity diagnosis
    and gameplay/animation refinement. Existing tasks remain; this is additional work.

**Why this order.** P0 protects dirty work. P1 comes before P2 because a clearer game
reads better even with today's lighting, and because it removes HUD and text that P2's
captures would otherwise have to be retaken around. Paperwork (P6) moved behind visible
work because the owner's standing complaint is loops that do not change the game.

### ASKS-0926 · Every owner ask of the 2026-09-26 cloud session, in one list ⚠️ OPEN, 2026-09-26

Owner, 2026-09-26: *"pls log all todo i asked u for when i go to diff session ty"*. This is the whole
list from the cloud session (Unity 6000.5.8f1 on Linux, `tools/cloud_unity_setup.sh`), each with where
the work lives and what done looks like. ⚠️⚠️ **NEWEST ASK FIRST, and this list comes before everything
else in the queue** (owner, same day: *"in the todo section prioritize most recent assks"*): the open rows
are ordered most recent first, the finished ones follow. A row that belongs to a bigger entry
points at it rather than copying it. Nothing here is ticked without evidence.

- [ ] ⚠️⚠️ **NEWEST (2026-09-27): every character's own walk and run, from their personality, arms attached** (owner, on
  the walk clip `walk_v3_HeroStrike.mp4`: *"walk is fucking ugly hahaha do it one by oen dont generate the same one for
  all"*, *"really lock in with animating the walk and run of each character man / i want u to tink of their personalities
  and shti and how it will show up in walking"*, with a still of the cast: *"look dude everyones arms are floating and not
  even attached right"*, *"manually do it for each character ty"*, *"make it a rule in repo to never copy paste game wide
  changes"*, *"thoroughly think abt how to mane walk and run look natural for ALL characters"*). CAUSE of the floating:
  the second walk pass slid every SHOULDER outward (24 to 54 world cm, up to 0.8 of an arm's length) so the fist cleared
  the hips; the arm's top left the torso. RULE written: CLAUDE.md section 0 and AGENTS.md, never stamp one change across
  the cast. BUILT: `FitArm`, the shoulder shift and every cast-wide gait constant deleted; the shoulder never leaves its
  pivot. `Runtime/Visual/GaitStyles.cs` holds one hand-written walk and run per body (9 heroes, the custom hero, 12
  neighbourhood players and Iggy), each with its reason from the lore, and some with a quirk only they do (Cheska scans
  the court, Nemu's head wanders on its own clock and her run sweeps the sleeves back, Rafi glances over his shoulder,
  Paete's crown sways late, Sean's shoulders drive). Legs are posed from the style too, with the cadence solved from each
  stride so no foot slides (`GaitStyle.CycleMetres`). `WalkArmsProbe` films each body walking and then running from the
  front, the side and three-quarter (`TUMP_WALK_BODIES=<ids>` for one body), and `tools/stitch_walk_video.py --per-body`
  makes one clip per character. MEASURED on the first per-character film (`Logs/gait-v5`): a stride that never slides
  cost 3.9 to 7.8 steps a second on these sub-metre bodies at 2.3 to 4.2 m/s (Nemu 7.8, a blur of legs), so each gait
  trades a capped slide (`Gait.Glide`, 1.1 Sean to 1.63 Jun-jun and Nemu 1.6, capped at 1.7 by
  `MotionContinuityTests.GaitCadenceMatchesTheActualFootTravelOnEveryRosterRig`, which caught two runs at 1.72 and 1.75):
  walking cadence now 2.9 (Paete) to 4.5 (Amihan) steps a second, running 3.4 to 5.2. Also found and fixed: the shared
  clips' own chest and root lean (`tools/author_grounded_gaits.py`) leaked under the new pose and tipped every chest back,
  so the gait is now drawn from the bind pose; arm lag is in cycles (seconds put Nemu's arms with the same-side leg).
  Tests: EditMode `MotionContinuityTests` 11/11, `AnimationReviewTests` passing; PlayMode `LocomotionArmsProbe` (swing
  floor now each style's own) and `ExchangePresenceTests` 3/3; Core 658/658. The full EditMode run is 590/609: none of the
  18 other failures touches the gait (hero-kit names, glyphs, cooldowns and charges, Nemu reactivation, toon light falloff
  under `-nographics`, and `ThrowEquipmentClearanceTests`, which poses a standing body where the gait layer does nothing).
  OPEN: the owner's eye on each character's clip (`Logs/walk-share/gait_v7_<body>.mp4`), refined one at a time; the
  idle pose (still the shared clip, arms pressed into the torso) per character; the fists still pass inside the hips
  front-on at the moment they pass them (`gaps_v7.csv`: -1 to -19 cm on the heroes, -27 on Phaister's robe), accepted rather than sliding the shoulder
  off the torso. Supersedes the walk row below.
  SECOND ROUND, same day, the owner watching each clip: *"walk of these 2 characters suck"* (Rafi, Amihan), *"nemu walks so
  awkward"*, then the real cause, *"the arms are too long for cheska sean and majority of cast / tahts why tey look so fucked
  up walking"*, *"can u lowk js edit thheir models"*, *"no need to edit some of the characters that SHOULD have long arms like
  paete"*, *"make sean look more muscular by moving his arms out"*, *"do it one at a time"*. MEASURED: the shared hero body hangs
  a 0.284 arm from a shoulder 0.288 above the floor (the hand alone 0.15), so a hanging arm reaches the ground. BUILT:
  `tools/reshape_hero_arms.py`, one hand-picked row per hero, surgery inside the shipped glb (only arm vertices, their bounds,
  and for Sean the shoulder nodes, binds and the one arm translation key; refuses a second run): Sean 0.293 to 0.225 with the
  shoulders 5.5 cm out onto his chest, Cheska 0.18, Dante 0.19, Zack, Phaister and Rafi 0.185, Amihan 0.175, the custom hero
  0.182; Paete and Nemu untouched. Re-authored one at a time from film: Amihan (arms out of her same-coloured coat, a skip),
  Rafi (arms clear of his tattooed chest; the in-across-the-belly swing removed), Nemu (the 7 degree head tilt read as a
  broken neck: now level, hands behind her back), Zack (24 degrees, yellow sleeves on a yellow jacket). Clips sent per
  character (`Logs/walk-share/gait_v10` to `v13`). OPEN: the owner's word per character; the neighbourhood players' arms
  are sourced CC0 art (CLAUDE.md 6.0), not reshaped without his say; first-person arms of Amihan, Rafi and Paete are baked
  (`ViewmodelArmAuthor`) and still the old length.
- [ ] ⚠️⚠️ **PRIORITY 1 FOR THE NEXT SESSION: remodel Amihan and Rafi so they belong in the same art style as the
  rest of the cast, Dante and Phaister in particular** (owner, 2026-09-26: *"add to todo priority for ltr, this will be
  no.1 priority"*, *"remodel amihan and rafi a bit so that they look more like other characters in terms of art style
  coz they look so diff"*, *"specifically dante or phaister"*). "A bit": a restyle of the two existing builders
  (`tools/build_amihan_voxel.py`, Rafi's builder), not new characters. Method: `docs/CHARACTER_MODEL_METHOD.md`
  (research first: put both beside Dante and Phaister in the cast lineup and write down, measured, what differs:
  head-to-body ratio, voxel size, how many colours and how flat, face construction, outline weight, costume detail
  density; then change only those). Keep each one's identity and quiet colour. Done looks like: versioned turnarounds
  and a cast lineup (`_v1`, `_v2`...) where neither reads as from another game, the owner's yes, FPP arms re-derived
  from the new models, and the in-match walk and casts re-checked on the new bodies.
- [ ] **The walk looks wrong: the hands stay close to the body** (*"the walkingh animation looks so
  weird, hands close to body"*). Same complaint as REFINE-2.9b (Sean's arms stick to his body).
  MEASURED (`WalkArmsProbe`, new: every roster body walked at the lens and past it at a fixed 30 steps a
  game second, the gap between each hand and the hip at the same height measured every frame): with the
  old fixed spread (14 degrees walking, every body) every hanging fist sat INSIDE the body's front-on
  outline, -9 to -17 cm at the hip on the heroes (`Logs/cloud6/walk-arms/gaps_v1.csv`). Cause, read off
  the glbs: every shoulder pivot sits inside the torso (0.100 against a 0.158 half-width on the shared
  body, 0.125 against 0.188 on Sean's) and the arm is a thick block whose lower face turns inward when
  it hangs. BUILT: `CharacterAnimator.LocomotionArms.cs` `FitArm` solves each arm per body from its own
  bind-pose vertices: the spread opens from 16 degrees (walk) and 20 (run) up to 30, and the rest is a
  sideways shoulder shift of at most 22 per cent of the arm's length, so the hanging fist clears the hip
  by 5 per cent of it; walk swing 30 to 32 degrees. After (`Logs/cloud7/walk-arms/gaps_v2.csv`): the
  hand beside the hip clears it by +0.5 to +12.8 cm on every body but Phaister (-0.7 cm against her
  robe, spread and shift both at their caps), spreads 19 to 30 degrees, shifts 5 to 15 cm; the front-on
  frames show the fists outside the hips. `LocomotionArmsProbe` now runs at a fixed 60 steps a game
  second (it sampled 5 frames on the cloud's software renderer and failed on the count, not the arms).
  SECOND PASS, same day (owner on those frames: *"thihs walk still sucks pls imrpove still on all"*): the
  first fit bought its clearance with spread, so every body walked with its arms held out in an A like
  a penguin, and nothing else moved. Now: the arms hang nearly straight (5 degrees walking, 8 running,
  opening to at most 14 only where a fist would land inside the hips) and FLUSH beside the torso, the
  shoulder moved out until the arm's inner face touches the torso's side (measured on the glbs: 9 to 12
  mesh cm on the shared bodies, 17 on Sean and Iggy, 22 on Phaister's robe); swing 36 degrees walking,
  58 running; `ApplyFootPlant` drops the root so the lower foot stays on the court (knee-less legs at 38
  degrees lifted both soles 21 per cent of a leg, about 9 cm, off the ground at every contact, so the
  body hovered; the drop is also the bob, twice a cycle); `LocomotionWeight` adds a gait lean (4 degrees
  walking, 9 running) and a weight roll over the stance leg (3 and 1.5 degrees), the head giving back
  half. `WalkArmsProbe` writes a two-second clip of every body (`TUMP_WALK_VIDEO=1`,
  `tools/stitch_walk_video.py`). Cloud film `Logs/cloud9` (stopped after 12 bodies to hand off): hand
  beside the hip now clears it by +2.2 (Rafi) to +27 cm (Phaister), spreads 5 to 8 degrees, shifts 24 to
  54 world cm; `LocomotionArmsProbe` walked at 5 degrees and would fail its old 6 degree floor, now 3
  with the reason (NOT re-run after that edit: run `LocomotionArmsProbe` and `WalkArmsProbe` first).
  Open: idle still uses the clip's arms pressed into the torso (a hand-over
  pop when a walk starts or stops is possible; decide whether idle hangs the same way), and the owner's
  eye on the clip.
- [ ] **Every recast of Paete's attacking E throws a wooden slipper** (*"paete attacking e is supposed to
  throw a wooden slipper whenever u recast it"*). With the fix above every ACCEPTED recast throws a clog;
  a press before the next clog has grown is refused with its countdown. Open: the owner's eye on it in
  play; if he wants a throw on literally every press, the 15 s growth becomes the throw cooldown and
  that number is his to give.
- [ ] **Paete looks small and scuffed on the hero screen and on character select** (*"why is paete so
  small here"*, *"can u make the size paete bigger he looks so small and scuffed"*, then *"have u made paete bigger?
  supposed to be larger than sean"*). Measured on the glbs: Paete stands 0.792 rig units against Sean's 0.848, so his
  BODY must grow (in the match too, not only on the screens), and the previews must stop fitting every hero to the
  same frame height or a taller hero never reads as taller. Cause of the small preview found:
  `ModelPreview.Frame()` sets the camera distance from the REST-pose bounds, and Paete's long T-posed
  arms make his width, not his height, decide the distance, so the body is drawn at a fraction of
  Cheska's height. Done looks like: frame on the standing height (or the idle pose's bounds), Paete's
  body height on screen within 10 per cent of Cheska's on both screens, photographed at the owner's
  window shape and a phone shape, and no other hero's framing moved by more than a few per cent.
  BUILT 2026-09-27: `CharacterVisual.BodyScaleFor` (Paete 1.3, visual only; capsule, reach and rules unchanged; 1.84 m to
  Sean's 1.23), applied in the match and in `ModelPreview`; `ModelPreview.Frame` fits a character's standing height, never
  closer than the shared body's 0.79 (not its T-pose). `HeroPreviewSizeProbe` (960x1015 and 620x700): standard heroes 0.64 to
  0.69 of the panel, Paete 0.65, Nemu 0.50, nobody cropped. Film beside Sean: `Logs/walk-share/paete_big_vs_sean_v2.mp4`.
  OPEN: the owner's eye; the lobby and hub podium previews were not photographed.
- [ ] **A prompt showing which button to press whenever something can be interacted with** (*"make it so
  that theres ui showing what button to click when theres smth to interact with"*). Inventory first:
  every Interact use (Paete's plant uproot and the 7 s rooted break-out, the can raise, pickups, doors
  in the hub if any), then one prompt component near the reticle reading the live binding through
  `Rebinding.DisplayNameFor(asset, action, device)` for mouse and keyboard, pad and touch (on touch it
  points at the thumb target). Photograph each prompt on all three devices (CLAUDE.md 4a).
- [ ] **Everyone gets 999999 tansan so the testers can unlock everything** (*"unlock all"* was asked and
  withdrawn in the next message). Built: `ugs/cloud-code/wallet.js` `PLAYTEST_TOPUP = 999999` tops every
  loaded wallet up (`tools/test_wallet_script.js` passes). OPEN: the OWNER must deploy `wallet.js` to the
  UGS project (`dcf0831e-...`), nothing changes in game until then. ⚠️⚠️ TEMPORARY: before any public
  build set `PLAYTEST_TOPUP = 0` and redeploy (balances already topped up stay topped up; resetting them
  is a separate decision for the owner).
- [ ] **LIANA LEAP from his own eyes looks like his arms extending**, then *"it doesnt bend with arms
  tho"*. HERO-9 row "LIANA LEAP from his own eyes". The bend (`PaeteVineReach.BendAlongArm`: the vine
  leaves along the drawn forearm and curves to the anchor) is built and looked at in the owner-view
  frames; open until the owner has seen it.
- [ ] **Online rooms fail for QA:** *"Could not open an online room. (relay allocation failed: There is
  no NetworkManager assigned to this instance!)"*. Found: Netcode 2.13.1's `StartHost` sets the role and
  then `Initialize` returns early (a nested NetworkManager, or a lost transport) before
  `ConnectionManager.Initialize`; the failed start's shutdown then throws exactly that message from
  `GetServerTransportId`. Built: `NetSession.PrepareManagerForStart` (unparents a nested session, restores
  a lost transport) and `StartNetcode`, which rebuilds the manager once and retries, around all four
  starts (LAN host, LAN join, relay host, relay join); the status line names the real problem. Evidence
  so far: `SessionRestartTests` 3/3 in the cloud (`Logs/cloud5/playmode.xml`), including the new
  `HostingWorksEvenWhenTheSessionWasNestedUnderSomething`.
  OPEN until a QA tester opens a relay room on the new build (UGS sign-in is refused in cloud
  batchmode, so the relay path cannot be run here). The two `OwnerPreparationTests` 90 s timeouts in
  the cloud look environmental; re-run them on Windows.
- [ ] **THORN HARVEST placed where he looks, not on his body.** HERO-9 row of that name.
- [ ] **Amihan, thoroughly** (*"thoroughly make sure amihan's animations look great"*, *"thorouighly think
  and create each detail of the skills, all vfx, sfx, animation each part / dont js mass prooduce witha
  script"*). Each piece hand-made and looked at:
  - [ ] The owner's updated table (ABILITY-2 row "Owner's UPDATED tables"): DRIFT two charges, each back
    15 s after it is spent (a timed refill, the one owner-written exception to event-only charges);
    FEATHERFALL 5 s of flight on a 40 s cooldown, moving and throwing in the air; WHIRLWIND 35 s;
    AIRBURST. Names in the kit, descriptions within the card limits.
  - [ ] *"her yellow circle looked weird as fuck when she was floating"*: no ground ring under a flying
    body (SKILLUI-1's first row covers the rest of the rings).
  - [ ] *"she didnt have a flaot animation too and any VFX"*: a hand-keyed hover loop with launch and
    landing, a flight lean into the direction of travel, and VFX that show the wind holding her up
    (curls round her shins, streaks rising past her, the court's dust turning under her).
  - [ ] *"visually show the win actually assisting her or working in her skills"*: DRIFT a gust at her
    back and visible shoves on the bodies she passes; WHIRLWIND lifting and turning what it catches;
    AIRBURST per its direction. Drift is barely visible today.
  - [ ] *"thoroughly think abt the hold indicators as well for the casting"*: her four on CAST-1.
  - [ ] The ultimate cutscene (*"use genshin impact and other game ULT cutscene animations as
    reference"*): direction written in `docs/reports/amihan-kit-2026-09-26/direction.md` (BREATH, WHIRL,
    THE TAKE, 6.2 s); build it, film it, iterate.
  - [ ] The Whirled overhead badge reads as a big disc; redraw it.
  - [ ] `AmihanKitPlayProbe.FilmHerSkillsInAMatch`: start her OUTSIDE the box (FEATHERFALL is refused
    inside it, which the first film showed), then film all four and send.

- [x] **Paete's E is placeable only outside the taya's box** (*"make paete's E only placeable outside
  box"*). `PaeteRules.PlantSpotOutsideBox` pushes an aimed spot inside the box (half-size 7 m) across its
  nearest edge by 0.6 m plus the plant's radius (ties go to X); `PaeteVine.PlantTarget` applies it, so
  bots and every peer agree. Core `APlantAimedIntoTheBoxLandsJustOutsideIt`. Protocol 60.
- [x] **Paete's E (BAKYA BLOOM) could be recast without limit** (*"unli cast for e / supposed to have
  cooldown"*). The recast only throws once a clog has grown (3 s after planting, then every 15 s); the
  presses in between played the gesture and the sound and threw nothing. Now `HeroAbility.ReactivateReady`
  and `ReactivateReadyIn` let a kit refuse a recast: `HeroKit.CheckFire` answers NotYet, and the tile
  shows the countdown instead of "Again" (`TumpPowerReadout`, both decks). Cloud PlayMode:
  `PaeteKitPlayProbe.TheSeedlingGrowsFiresAndComesOutOnlyWhenLoose` passes (`Logs/cloud5/playmode.xml`).
- [x] **"CODE: 3CHS"**: the colon added in both places the room code is drawn
  (`ConvertedMatchSetup.OwnerPainted.cs`, `Hub/HubCustom.cs`).
- [x] **The Paete skills film showed a human casting his plant skills.** Fixed in the film rig (the
  HERO-9 row "The skills film showed a HUMAN"). Film v2 sent; v3 (placed thorns and first-person arms)
  stitched locally, v4 comes from the run that checks the bend below.
- [x] **Unity in the cloud.** `tools/cloud_unity_setup.sh` (install plus Personal activation from
  `UNITY_EMAIL` and `UNITY_PASSWORD`), `tools/run_unity_guarded.py` resolves Unity, the player profile
  and `xvfb-run` per OS (`tools/test_run_unity_guarded.py` 10/10). PlayMode films render at about
  1.3 frames per wall second on llvmpipe. The account used was the owner's throwaway; never commit it.

### HERO-8 · Amihan, the eighth hero (Vigan, wind) ⚠️ OPEN, 2026-09-25

Owner, 2026-09-25: "start working on a new character u figure out her lore and everything
else needed but dont make skills yet js put placeholders", "she comes from vigan city and is
a wind character", with a concept sheet ("use this as reference u can change facial
expression"), then of v1: "that dont look like reference at all haha pls make it look very
similar or better". Brief, research and lore: `ArtSource/amihan/concept-20260925/design-brief.md`;
method `docs/CHARACTER_MODEL_METHOD.md`.

- [x] Research (Vigan, Abel Iloko, the binakol "kasikus" whirlwind, the amihan wind) and lore
  (`docs/CHARACTER_ORIGINS.md`, `LORE.md`), stats 5/2/3 (fastest hero).
- [x] Model: her own builder `tools/build_amihan_voxel.py` (female-a base, native skull), v4
  matched to the concept (swept fringe, three pixel flowers and a tassel, teal mantle and gold
  brooch, open cream coat over a teal inner, banded sleeves, rust belt, abel sash, teal shorts,
  cream sandals, the kasikus on her back). Slide re-solved on her mesh at every build.
- [x] Playable with PLACEHOLDER skills: roster row (appended), `AmihanHeroKit` (three slots that
  cast and do nothing), two placeholder options per slot, UI accent (hue 100, the one legal
  window), select blurb, FPP arms from her model, portrait, avatar, lines (text) with the
  HUMAN.md recording rows, cloud-code hero lists in the repo.
- [ ] **Owner pick: her colour.** Concept teal (default) or abel indigo (`AMIHAN_CLOTH=abel`),
  compared beside the cast in `Logs/rafi-share/amihan-v4-colour-compare.png`; the teal sits in
  Rafi's family.
- [ ] **Her skills and ultimate.** Owner designed them 2026-09-25 (QUICK DASH signature,
  UPDRAFT attacking, WHIRLWIND defending, STORM SURGE ultimate) and answered the open questions;
  see ABILITY-1 below and `docs/reports/amihan-kit-2026-09-25/`. Done so far: gameplay
  (`AmihanHeroKit`, `AmihanHazards`, `Core.AmihanRules`), wind VFX toolkit and every ability's
  effects (`WindVfx`, `AmihanVfx`, `Shaders/WindRibbon`), audio (`tools/build_amihan_audio.py`),
  lines, glyph ids, sky look, the introduction's body table (`tools/author_ultimate_intros.py`).
  Also written: cast clips (`HeroAbilityClips.Amihan.cs`, baked by `Editor/AmihanMotionAuthor`),
  first-person actions (`gust-dash`, `updraft-lift`, `gale-sweep`, `storm-call`), glyph and status
  icon drawings (`tools/build_ability_icons.py`, reviewed v1 to v3), the introduction's stage
  (`HeroIntroductionScene.Amihan.cs`), bots (`AIController`). Open: screens, in-engine review of
  every beat (captures), PlayMode and bot probes.
- [ ] Deploy the two cloud-code scripts that now list her (`ugs/cloud-code/match-record.js`,
  `wallet.js`) to the live UGS project; until then the server does not know her id.
- [ ] Record her voice lines (human voices only; the rows are in `docs/HUMAN.md`).
- [ ] A home court (brief section 3 names one) when maps are next opened.

### ABILITY-1 · Signature + role abilities, the status table, and the ability direction ⚠️ IN PROGRESS, 2026-09-25

Owner, 2026-09-25: "we are overhauling how abilities work ... there will be 2 abilities, one
signature ability that doesnt change and stays no matter what role and one that changes", then
the status table (Whirled, Chilled, Frozen, Tagged), "thoroughly try to direct all vfx and sfx of
the skills so that it will look cohesive, good and satisfying", "it will be the baseline for all
rework of skills". Design, research and direction: `docs/reports/amihan-kit-2026-09-25/`
(`plan.md`, `research.md`, `direction.md`). Amihan (HERO-8) is the first kit in the new shape.

- [x] Role kit data shape: `HeroKit.AttackingSkill`/`DefendingSkill`, `Skill2` is the live one,
  `SetRole` from the derived taya each frame; legacy kits unchanged.
- [x] Status system: `Core.StatusRules` (the owner's four rows), `CharacterMotor.Status.cs`
  (Whirled, Chilled timers; Tagged ignores stun immunity; flight; carry), pickup gate,
  Cheska's ice sheet applies Chilled, `SyncUnit` carries both new timers, `Carry` message,
  protocol 53.
- [ ] HUD: role badge and swap on the slot-2 tile (`TumpPowerReadout.OwnerDeck`, a flip on
  change, the role in the hold-to-read tray), status icons with the owner's tooltips over other
  bodies (`Visual.StatusOverhead`) and as chips under the reticle (`TumpMatchReadout.Statuses`),
  status body tells (`WhirledMark`, `ChilledMark`). Written; open until photographed on mouse and
  keyboard, pad and touch.
- [ ] Screens: character select, skill tree and loadout show signature, attacking, defending and
  ultimate for a role kit.
- [x] Screens: hub character select and hero screen/popup show SIGNATURE, ATTACKING, DEFENDING,
  ULTIMATE for a role kit (`HeroKit.ScreenSlots`). Not yet photographed; `ConvertedCharacterSelect`,
  `TumpSkillView`, `AbilityInspectPanel`, `BrandAbilitySelection` and `HubSkillTree` labels still
  read the three-slot shape (they show the attacking ability in slot 2).
- [ ] **NEXT SESSION, in order** (state at the 2026-09-25 hand-off):
  1. `AmihanReviewProbe` (`Editor/MapKit`) wrote dash, updraft, whirlwind, storm and hit strips
     to `Logs/amihan-review/*_v1.png` and then threw a NullReferenceException in
     `Introduction` (the cutscene strip): fix it, bump `Version`, render, LOOK at every strip,
     critique hard (owner: "watch it all and berate it then improve it even more"), iterate.
  2. Re-run EditMode (last run: 605/609 with only the three known pre-existing failures plus
     the description-length one, which is now fixed but not re-run).
  3. Capture HUD states (status chips, overhead badges, role badge flip) and the screens on
     keyboard, pad and touch; fix what the pictures show.
  4. PlayMode gate, `Checks.RunAll`, audits (last run: only the pre-existing cue_audio,
     event_subscriptions and tournament_defaults findings), `BotBehaviourProbe` with Amihan,
     `AbilityShowcaseProbe`, then a Windows build into `Builds/`.
  5. Remaining screens above; replay recording of dash/updraft/hover; rejoin during the storm
     gather (plan.md § 9).
- [ ] Owner approval of the proposed mapping for the other seven heroes (`plan.md` § 6); until
  then they are unchanged. Owner review of the gap decisions in `plan.md` § 8.
- [ ] Full verification (Core, EditMode, PlayMode gate, Checks.RunAll, audits, BotBehaviourProbe,
  AbilityShowcaseProbe, Windows build).

### ABILITY-2 · The roster ability rework (Cryo, Geo, Necro, Voodoo; skill tree UI off) ⚠️ OPEN, 2026-09-26

Owner, 2026-09-26, with the power tables: *"will create completley new VFX SFX AND SKILLS FOR ALL
CHARACTERS AND WE MIGHT DUMP THE SKILL TREE IDEA FOR NOW"*, *"JS REMOVE ITS UI FOR NOW AND HARDCODE
THE SKILLS AND SHIT"*, *"U thoroughly plan them first"*, then *"also i want u to think abt all ult
animations and direction and cutscene as well"*. Mapping: Anemo Amihan, Cryo Cheska, Geo Dante,
Necro Nemu, Voodoo Phaister; Pyro (Sean), Electro (Zack) and Hydro (Rafi) have no design yet
(placeholders). Plan with the tables verbatim, the owner's answers and every number:
`docs/reports/ability-rework-2026-09-26/plan.md`; ultimates and cutscenes: `ultimates.md` beside it.

- [x] Plan and ultimate direction written; owner questions answered (Feared, Disoriented, Vulnerable,
  Concussed, Kuro fetch, Kuro guard, Kuro plays, Higop, Teleport, placeholders).
- [x] Core: statuses Concussed 6, Feared 7, Disoriented 8, Vulnerable 9 appended with the owner's
  answers; `RosterReworkRules.cs` (Cryo, Geo, Necro, Voodoo numbers); `HeroLoadoutRules.SidegradesOpen`
  (false); Core.Tests 649/649 (2026-09-26, cloud). Status IMMUNITY is still to do (motor row).
- [x] Skill tree UI hidden and kits built on the default variant: `HeroAbilitySystem.ConfigureLoadout`
  and `UpdateLoadout`, the HubHome SKILL TREE door, HubHero's ALTERNATIVES button, the picker's
  LOADOUT door, the lobby loadout button. Syntax-checked only; owed to the testing chat: compile, and
  the PlayMode tests that look for those doors (`HubFlowTests`, `HomeFlowTests`, `FrontEndControlWalk`,
  `LoadoutSurfaceProbe`, `BrandPickerTests`, `TumpNativePickerTests`, `UiRuntimeShots`,
  `BrandPreparationTests`): a test that opens a hidden door is updated to expect it hidden, never
  the switch flipped to make it pass.
- [x] Motor statuses (`CharacterMotor.Rework.cs`): Concussed x0.7 speed, no sprint, throw wobble;
  Feared drops the slipper (host), flees from the source on its own, no act; Disoriented aim sway;
  Vulnerable taggable anywhere, tag reach and stuns x1.5, out-of-box tag for Phaister's kit; status
  immunity (`IsImmuneToStatuses`); `SyncUnit` fields and protocol 56. Compiled (tools/cloud_compile.py),
  not run. OWED: the Feared flee clip, the Disoriented hallucinations (presentation), status icons.
- [x] Cheska (COLD FEET, FROSTBITE with `SlipperAffinity.Frost`, GLACIAL WALL with 3 hits and a
  shatter on every peer, ABSOLUTE ZERO) and Dante (SHIELD, BOULDER `DanteBoulder`, BARRIER reflecting
  slippers, EARTHQUAKE) rewritten as role kits. Compiled, not run. The Glacial Wall is still the straight
  barricade and the boulder and barrier are placeholder slabs (`GeoVfx.cs`) until the presentation pass.
- [x] Phaister (SHADOW BLINK kept as the signature, CURSE: DISORIENTED doll `VoodooDoll`, CURSE: VULNERABLE
  cone, HIGOP black hole `VoodooBlackHole` on the seance pull with her slippers spared and no drowse) and
  Nemu (TERRIFY haunt, KURO FETCH with the taya's intercept, KURO GUARD with the fallible 0.35 s + 0.25 s
  AI; Kuro errands `GhostPetCompanion.BeginErrand`). Compiled, not run. Nemu's ULTIMATE is still DEVOURING
  SEANCE: KURO PLAYS (a fifth, seatless bot unit) needs its own design pass and a Unity session.
- [x] Sean, Zack, Rafi on the four-slot shape: their old second skill is the ATTACKING slot, DEFENDING is
  `PlaceholderRoleAbility` (COMING SOON, does nothing). With the tree off, no variant name or cue is
  written over a kit (the old defaults would have renamed Dante's SHIELD "SEISMIC STOMP").
- [x] First presentation pieces (cloud, compiled, not run or seen): the shared `feared-flee` clip (a
  panicked looping run, arms over the head, glancing back; `HeroAbilityClips.Status.cs`, baked by
  `RootedAnimationAuthor`, played by `CharacterAnimator.StepRootedPose`); Disoriented hallucinations on the
  victim's own screen (`DisorientedHallucinations`: baked copies of the other players and the slippers,
  wandering among the real ones); status icons for Concussed, Feared, Disoriented, Vulnerable
  (`tools/build_ability_icons.py`) and the HUD's live-status list.
- [x] Second presentation pass (cloud, compiled, not run or seen): 28 new sounds
  (`tools/build_rework_audio.py`, registered in `AudioCues`, wired in the kits; Paete's ultimate called from the
  ground); four typed models (`tools/build_rework_props.py`: Dante's boulder and barrier, Phaister's doll and the
  Higop ring, in `Resources/Models/ReworkProps/`, loaded by `ReworkProp.Spawn` with the old blocks as the
  fallback; the barrier rises out of the court through `BarrierRise`); the Glacial Wall is now an ARC of five
  ice pieces (`CheskaIceVisuals.BuildArc`, `SpawnIceBarricade` arc length and radius). Owed in Unity: the glb
  import, a look at every one of them in play, and the owner hearing every sound (`CLAUDE.md` 6).
- [ ] Presentation per ability, one character at a time: own cast clips (the new kits still borrow their
  old cast actions), FPP gestures, VFX and SFX made far better than the first
  passes above (any of it may be overhauled),
  the ability glyphs (several reuse an old one), the Concussed stumble, bots for every new ability.
- [ ] KURO PLAYS (its own design pass first), bots, snapshots, replays, screens, HUD.
- [x] ⚠️ FOUND 2026-09-26 night, FIXED 2026-09-26 (cloud): all four `Resources/Models/ReworkProps/*.glb.meta` (barrier, boulder, doll,
  higop) could not be parsed ("Expected closing '}'" on every launch): line 10 had lost the importer reference (a guid rewrite had put
  each file's OWN guid there and cut the line). Restored to the glTFast importer (`guid: 715df9372183c47e389bb6e19fbc3b52, type: 3`),
  each file's own `guid:` kept, and the texture dependency pointed at `ReworkProps/Textures` (it named `PaeteProps`). Evidence: all
  6,744 `.meta` files in Assets and Packages parse as YAML; the first cloud Unity import logged no YAML error and kept the fix.
- [ ] **Owner's UPDATED tables, 2026-09-26 (cloud), *"updated skill names and status effects"*.** They replace the Anemo and Cryo rows
  and the status table in `plan.md` section 0 (both recorded verbatim in `docs/reports/amihan-kit-2026-09-26/plan.md` section 0):
  Anemo **Drift** (2 charges, 15 s in between), **Featherfall** (*"Propel upward and fly for 5 seconds. You may move or throw slippers
  while in the air."*, 40 s), **Whirlwind** (35 s), **Airburst** (15 points), built in HERO-8. Cryo **Cold Feet** now *"2 Charges, 20
  Seconds Cooldown In-Between Use"* and the field lasts **7.5 s** (was 35 s, 5 s): open for Cheska's pass. Statuses: **Rooted** *"Prevents
  movement for 2.5 seconds"* (Paete's Rooted is 7 s with a break-out: ask the owner which wins before changing it), **Concussed**
  *"Prevents movement or interaction for 1.25 seconds"* with a *"HUD Concussed Visual Effect"* (the built Concussed is the older answer,
  a 30 % slow and throw wobble), **Frozen** gains a *"HUD Frozen Visual Effect"*, and two new rows, **Drained** (*"Depletes stamina to 0.
  Prevents stamina recovery for 2.5 seconds"*, tooltip *"Disabled Stamina Recovery"*) and **Hexed** (*"Removes"*, tooltip *"Disabled
  Protection"*). Open: each built into `StatusRules` (appended, never renumbered), motor, icons and HUD when its hero's pass uses it.
- [ ] Ultimate cutscenes rebuilt per `ultimates.md`.
- [ ] Owner review of the numbers set in plan section 7.
- [ ] Unity verification (the separate testing chat): compile, EditMode, PlayMode gate, captures.
  First Unity run of the cloud kits, 2026-09-26 (`Logs/paete-r1-editmode.xml`): it COMPILES; EditMode 587/609. Reds that
  belong to this rework, each to be fixed in its hero's pass (update the test to the new design, never flip the design):
  `BroadcastPassTests.AllSixHeroesHaveTheirOwnNamedUltimate` (Dante's card still expects TITAN FISSURE),
  `HeroLoadoutRefreshTests.SameHeroVariantRefresh...` x6 (variants are off with `SidegradesOpen = false`),
  `HeroPresentationTests.EveryAbilityAcrossAllHeroesHasAUniqueBespokeGlyph` (BARRIER reuses DanteShield),
  `...EveryHeroAbilityHasBespokeCastAndViewModelActions` and `ViewmodelArms_PreservesHeldSlipper...` and
  `RosterArmGeometryTests.EveryHeroUsesBothHandsAndReturnsCleanly` (the COMING SOON placeholder has no cast or FPP action),
  `...EverySummaryFitsTheCardItIsDrawnIn` (CURSE: DISORIENTED 134 > 125 characters), `...TelegraphsMatchWhatTheAbilityPlaces`
  (FROSTBITE draws 0 m, places 1.6 m), `InputMapAndAbilityTests` x3 (charges, Dante skill2 30 s under the 45 s floor, Glacial
  Nova vs Supernova cost), `RuntimeLayerTests.Nemu_AstralProjection_SupportsReactivation`. Known, not the rework:
  `ThrowEquipmentClearanceTests` and the two `ToonLightFalloffTests` (need graphics). PlayMode `SharedUltimatePhaseTests`:
  `TwoAcceptedCastsShareOnePhase...` and `FourCastersShareOneDeadline...` fail because Phaister's HIGOP is not accepted into a
  shared phase (commits 1 of 2, `Logs/paete-r8-play.xml`).

### CAST-1 · Hold to preview, release to cast, cancel on every device ⚠️ OPEN, 2026-09-26

Owner, 2026-09-26: *"Make it easier for ppl to understand and visualize HOW and where their skills
will be cast if they HOLD"*, *"check out marvel rivals for skill cast indicator"*, *"all characters
have shityt preview and cancel cast rn thoroughly think abt implementation of it later too"*, *"and
ui for the cancel shit idk"*, *"U figure it ALL out"*. Design and research:
`docs/reports/ability-rework-2026-09-26/cast-preview.md` (crosshair aim instead of the time ramp,
seven preview shapes with a real-size ghost for constructs, red invalid that refuses, cancel and
rotate on mouse, pad and touch, the prompts, the touch cancel target). Supersedes the ring half of
SKILLUI-1.

- [x] Research and design.
- [ ] `HeroAbility` aim shape, anchor from the camera ray, `CanPlaceAt`, rotation; system Aiming
  state with cast, cancel (free) and rotate; aimed pose on the wire.
- [ ] Input verbs `AbilityCancel`, `AbilityRotate` (pad, thumb), `InputAssetSync.Regenerate`,
  touch drag-to-aim and the cancel target, the prompts.
- [ ] `CastPreview` shapes; every ability on every hero moved onto it and photographed.

### HERO-9 · Paete, the ninth hero (Mount Makiling, plant) ⚠️ OPEN, 2026-09-25

Owner, 2026-09-25: a PLANT hero on the signature plus role ability system; from Mount Makiling,
Laguna; named Paete; concepts (GUARDY sheet, a blocky treant with three face states and an
arm-extension panel, a carved-plank face close-up) with *"engraved sunked green eyes and a
nonchalant calm expresison"*; the kit table (Vine Pull, Throwing Slipper Plant, Thorn Pull,
Nature's Wrath); *"thoroughly plan how to do it first and research"*; *"do it one by one dont
try to mass generate it"*. Brief and lore: `ArtSource/paete/concept-20260925/design-brief.md`.
Research and plan: `docs/reports/paete-kit-2026-09-25/`.

- [x] Research: Makiling, the Mariang Makiling stories, Paete's carvers, narra; animation
  references from footage (Groot, Kinich, Zyra, Scorpion, Dead by Daylight) and wikis.
- [x] Lore, colour decision (dark moss `4f6b1f` under an amended accent law), stats 3/3/5, names
  proposed, the six beats per ability, the part-by-part plan.
- [x] Owner answers (plan.md section 7).
- [x] Model v17 (`tools/build_paete_voxel.py`, evidence in `ArtSource/paete/concept-20260925/evidence/`),
  rebuilt from the owner's idea board; vine arms, no fingers. Owner's verdict on v17 still owed.
- [x] Core: `PaeteRules`, Rooted status, `ScoreEvent.SproutKnock`, roster row, lines, loadout rows;
  Core.Tests 646/646.
- [x] Roster book entry (`RosterBookBuilder.RefreshPersonFromCommandLine -person paete`, which now
  inserts a hero new to the book without re-baking the others), FPP arms, baked dance. EditMode
  606/609 on 2026-09-26: the three left are `ThrowEquipmentClearanceTests` (every person but Paete,
  who clears all ten slippers at 0; the known synthetic-head red) and the two `ToonLightFalloffTests`,
  which pass 2/2 with graphics on (`-nographics` renders no light, so they cannot pass there).
- [x] His own animations (owner, 2026-09-26: *"give paete his own animations taht make sense wiht his
  shit"*, *"i want each of his skill to have their own animation"*): five body clips, each acting out
  its skill (`HeroAbilityClips.Paete.cs`, baked by `PaeteMotionAuthor`): the vine drag, the planting
  lob, the COMMAND point on the second press (its own action, FPP gesture and click), the stamp then
  rope-yank, and the thrown-to-embrace that picks up from the introduction's last pose. Matching FPP
  clips. Shared `rooted-struggle` and `plant-heave` on every rig (`RootedAnimationAuthor`), shown on
  every peer through the struggle flag and pull progress on `SubmitMove`/`SyncUnit` (protocol 55).
  Filmstrips `PaeteReviewProbe`.
- [x] `UltimateIntros/paete.txt` (who, intent, gather, release; ends on the thrown pose).
- [x] Audio, `tools/build_paete_audio.py`: 16 cues, every cue the kit plays plus the sentry burst,
  catch and wilt, Rooted and the root break, the command click, the theme and the Canopy sky.
- [x] Icons: the four glyphs and `StatusRooted`, critiqued to v2/v3 on the real grounds.
- [x] Played (`PaeteKitPlayProbe`, Bayan Plaza, Hero Strike, real input, 4/4): the vine reels him
  6.9 m; the seedling plants, fires on the second press, survives a pull inside its 15 s and comes out
  to a full Interact hold once loose; BAWI takes a slipper out of a hand and hauls it to 1 m; the
  sentry drags both bodies in and roots them (0 m walked), a tag frees one, 7 s of Interact frees the
  other with the struggle showing. The first play found the sentry rooting bodies where they stood
  (Rooted killed the pull); roots now land on arrival and the pull speed is solved per distance
  (`PaeteRules.SentryPullSpeedFor`). Interact card on the HUD (`Hud.Paete.cs`).
- [x] Bots (`AIController`): the vine as an escape out of the box with a slipper, away from the taya,
  else travel; the seedling planted 4 to 10 m from an upright can and commanded at it; BAWI for two
  slippers or one being carried; the sentry for two bodies in 9 m; Interact held while rooted and at an
  opponent's loose seedling. Not yet measured in `BotBehaviourProbe`.
- [x] First-person vines leave the viewmodel hands (`CameraRig.TryViewmodelHand`); not yet captured in FPP.
- [x] Owner's Groot references (2026-09-26): vines are entangled bark limbs with dark vines, lit strands
  and forked twigs (`GrowthTwigs`), easing out of the forearm; the sentry is a 4 m braided-trunk tree
  (research.md, "Groot's whole arsenal"; direction.md; frames in the report's review/).
- [x] Everything rises out of the ground (owner, 2026-09-26: *"coming out of the ground each time and
  forming on the spot not just spawning in"*): `PaeteGroundBreak` cracks the road and throws soil
  chunks; the seedling pushes up through it, the thorns punch up one by one, the sentry screws up out
  of the road twisting (review v12). The ultimate now throws a vine cluster, not a small seed.
- [x] Seedling body v2: stem, base leaves, a five-petal pod that opens round a real wooden slipper.
  Fixed a real fault: the growing slipper's pose overwrote its block size and a grown shot rendered as
  a 1 m tan cube.
- [x] World snapshot and replay: `WorldEffectSnapshot.Kind.Plant/Thorns/Sentry` (12 to 14, appended
  inside protocol 55), captured, validated, restored for a rejoiner, retired with the round, and drawn
  in replay (`RecordedFieldView`). Compiled in the v13 probe run; NOT yet exercised by a rejoin test.
- [x] The trees, modelled and wired (owner, 2026-09-26: *"the current models of all his skills look ugly
  still its js blocks"*, *"thoroughly work on the detail of each part manually"*, *"really caerfullly and
  delicately work on the animations + model of his trees"*, *"think of vfx that should accompany it as
  well as sfx"*, *"thoroughly direct it"*). Direction written first: `docs/reports/paete-kit-2026-09-25/direction.md`
  section 5 (what was wrong, the idea, beat tables per tree with body, VFX and SFX, how prisoners are held,
  the break-out). `tools/build_paete_props.py` v7 types every part by hand (no looped shapes, the owner's
  rule): the sentry is a woven trunk of eleven hand-keyed cords crossing over and under (his Groot crop),
  on the silhouette of the cartoon tree he sent (flared claw roots with curled toes, pinched waist,
  swollen top, seven gnarled woven branches with hooked tips), woven vines threading the cords, and two
  engraved hollows with a slanted light (he rejected a carved face, brows and knots on the way: *"Js make 2
  fucking holles"*). The seedling has a three-cord stem, arm leaves and a petal-bud head; the thorn
  construct is a fist of roots and seven barbed square thorns. Wired in `PaeteTreeBodies.cs` (loaded and
  dressed with his palette and ink by `PaeteProp`): the sentry screws up, squashes, slams its roots down one
  by one, unfurls, WAKES (the light opens in the hollows, `sfx_paete_sentry_wake`), clenches its crown on
  the catch, looks from prisoner to prisoner, blinks, breathes, drops leaves, then sleeps and unscrews into
  the road; the seedling sways with follow-through, parts its petals as the slipper ripens (`sfx_paete_sprout_ready`),
  coils and snaps on the shot and dries to straw in four palette steps; the thorns ripple up, quiver in the
  hold, whip on the yank and clench. Prisoners are HELD: a woven limb reaches out of the trunk, drags them
  in and wraps their waist, woven root-branches climb their shins (arms and head free, they can still
  throw), no vines in the binding. Break-out (owner: *"make sure to create the animation for getting out
  of his ult too"*): a new shared clip `root-breakout` on all 23 rigs (`RootedAnimationAuthor`), the shins
  crack into bark chunks (`PaeteBarkShatter`), the limb whips back into the trunk, `sfx_paete_root_break`
  rebuilt with a second snap and the chunks pattering. Films: `PaeteReviewProbe.RunTrees` v14 (found every
  runtime branch drawn as solid ink: `GrowthVfx.Tube` and `GrowthVfx.Leaf` wind inside out, fixed in `PaeteInk`), v15, and
  v16 at 1.75 times the authored size, about 9 m (owner: *"make tree bigger"*, *"REALLY big and imposing and really feel
  like an ult"*; 1.75 is the most the 1.9 m hold allows before the flared foot swallows a prisoner).
- [x] Skills renamed and rewritten (owner, 2026-09-26: *"u can change name and description of all his skills to
  make it all sound better"*): LIANA LEAP, BAKYA BLOOM (bakya, the carved wooden clogs Paete's carvers make),
  THORN HARVEST, MAKILING'S EMBRACE; kit, loadout rows (Core 646/646), HUMAN.md notes. Ids unchanged.
- [x] Lore: the guardian of Mount Makiling (owner: *"i want the lore for this character to be that its the guardian
  of mount makiling"*). Mariang Makiling woke the carved tree and made it the keeper of her mountain; when one
  tree is not enough she puts a seed in his hand. `CHARACTER_ORIGINS.md`, the design brief and
  `character-stories.json`; the old "never shown" boundary is recorded as reversed.
- [x] Mariang Makiling in the ultimate's cutscene (owner: *"make it seem like the spirit of maria makiling or smth
  is watching over him"*, Aphelios and Alune; *"make her look see thru so that it seems like a ghost"*):
  `makiling.glb` (typed: long hair, flower crown, closed eyes, gown, shawl, cupped hands, her deer),
  `Resources/Shaders/SpiritGhost.shader` (one jade hue, value-only forms, fresnel edge, depth pre-pass so only
  her front shows, hem dissolving into mist), `MakilingSpirit`. She rises behind his left shoulder, bows to him,
  and the seed arcs from her cupped hands into his palm (the gift shot is the Alune composition). direction.md 5.10.
- [x] The cutscene pays off (owner: *"i also dont see the tree sprouting to its full size"*): 3.6 s to 4.6 s; the
  seed arcs down the court and the full 9 m tree screws up in a last shot tilting up past his shoulder, its eyes
  lighting. Nothing climbs Paete in the gather any more (owner: *"it shouldnt root the caster"*: the v2 leg
  branches read as the caster rooted; in play the caster was never caught). Sentry fits under a roof
  (`PaeteSentryBody.FitUnder`, Ilalim ng Tulay's deck).
- [x] Rooted swings the camera to third person (the owner's table: *"they get stuck on it (switches to tpp
  view)"*, planned and never wired): `CameraRig.StepFallView`, standing pitch.
- [x] Films (owner: *"record as well people getting pulled and rooted"*): `PaeteKitPlayProbe.FilmTheSentryPullingAndRootingThem`
  (TUMP_PAETE_FILM=1), three players dragged in and rooted, wide view plus a caught player's view; the
  cutscene by `UltimateIntroductionProbe.PaeteThrowsHisSeedFromTheForest` (TUMP_INTRO_SCENE=1). Sent to the owner
  as mp4 v1. Open on it: the film's own-screen view is a victim's, not Paete's (the local seat), so the
  cutscene on his screen is not yet filmed in a match.
- [x] The ultimate is CALLED UP THROUGH THE GROUND, not thrown (owner, 2026-09-26: *"i also dont want him to be
  throwing an orb I want him to be CALLING IT FROM THE GROUND"*, *"he connects with the ground for a bit (his roots all
  move and shit and it connects to the ground and then he summons his powers ... and then the tree sprouts"*). Cutscene
  (`tools/author_ultimate_intros.py` `paete`, `HeroIntroductionScene.Paete.cs`): Makiling's light comes down into his
  cupped hands; he kneels and presses both palms into the court (1.5 s), roots burst round him and writhe, three root
  veins with a light at their front race under the court to the spot; he rises arms high (2.62 s) and the tree erupts.
  Live: `PaeteRootVein` replaces the seed arc on the same 0.45 s (timing and warning unchanged); the live clip
  `hero-paete-sentry` starts from the raised pose. Verified by the verification run named in the commit.
- [x] First-person arms BULKIER (owner: *"make his fpp arms loook BULKIER bcz he is QUITE bulky"*): 1.45 across in
  `ViewmodelArms.ApplyCharacterStyle` (the roster-arm early return had skipped the thickness entirely); every hero now
  starts from scale one there. NOT yet seen in a first-person capture after the change.
- [x] The deer beside Makiling is cut (owner: *"why is there a deer even did i ask for that"*).
- [x] Portrait and avatar (`UI/portraits/paete.png` via `TumpPortraitAuthor.CaptureOnly -tp-portrait-id paete` with a
  closer framing for him, `UI/avatars/avatar_paete.png` via `tools/build_avatars.py`). NOT yet checked after the
  closer framing re-bake (the (height, zoom) argument order was wrong once; fixed to `LookAt(.84f, .42f)`).
- [x] (compiled, filmed and refined in Unity 2026-09-26: `PaeteReviewProbe` v19 to v22, pitcher deepened `93B540`/`5B7F2C`, fatter jug, thick two-tone rolled lip, five typed belly stripes, a rounded lid bigger than the mouth that opens to 84 degrees, the bakya rising clear of the lip at READY, a round heaved soil mound instead of the square tile; `PaeteKitPlayProbe` 8/8) DESIGN APPROVED 2026-09-26 (*"thats pretty fucking good"*; THORN HARVEST re-armed after *"it looks like  a flimsy plant and not a dangerous cool plant"*): BAKYA BLOOM a Makiling pitcher plant, THORN HARVEST an armed rattan (direction.md 5.11, concept sheets in the report's review/). BUILT 2026-09-26 (cloud, no Unity): `tools/build_paete_props.py` `seedling()` (pitcher) and `thorns()` (armed rattan), glbs rebuilt (2673 and 6534 triangles), `PaetePlantBody`/`PaeteThornBody` re-posed with their own palettes (lid, rising bakya, spit; stem ripple, talon fronds, rattan whips from the facing frond). Syntax-checked only: compile, `PaeteReviewProbe.RunTrees` film and `PaeteKitPlayProbe` owed to the testing chat. Earlier text: THE ATTACKER AND DEFENDER PLANTS MUST BE THEIR OWN SPECIES (owner, 2026-09-26: *"do all his sentries look
  the same? i wanted all his sentries (ult and attacker skill and defender skill TO ALL look diff and distinct and have
  their own style)"*, *"attacker and defender sentry should look like distinct plants or trees"*). Today the seedling and
  the thorn fist share the woven brown bark of the ultimate's tree. Proposed to the owner (not yet built): BAKYA BLOOM a
  squat bulbous green plant, smooth bottle stem, big broad paddle leaves (young banana plant), a gold-hearted bud that
  opens like a mouth to spit the wooden slippers, no bark; THORN HARVEST a low dark spiky rosette like rattan palm (uway),
  stiff barbed fronds that fan out and whip, no bark trunk. Typed by hand in `tools/build_paete_props.py` (seedling(),
  thorns()), node names kept for `PaeteTreeBodies.cs`, filmed with `PaeteReviewProbe.RunTrees`.
- [x] (2026-09-26: `sfx_cast_paete_sentry` cut to 0.8 s, the heave that launches the 0.45 s live vein race, because it
  plays AFTER the cutscene and the 2.2 s cloud cut swelled 1.5 s past the tree; the ground call itself moved into his theme,
  `sfx_ult_theme_paete`, re-timed from 3.6 s to the 4.6 s cutscene beat for beat: her figure, the light, the press 1.5 s,
  the veins 1.6 to 2.62 s, the rising bars, the eruption 2.95 s. Not yet heard by the owner in the game mix; the video
  `paete_ultimate_v3.mp4` carries it.) The ultimate's cast sound still describes a seed and an overhand swish (`tools/build_paete_audio.py`
  `sentry_cast`): rewrite it as the ground call (a root groan swelling, soil crunch at the press, a rising rumble), and
  add a sound for the veins racing under the court. Not heard by the owner.
- [x] (2026-09-26: `PaeteKitPlayProbe.FilmTheUltimateOnHisScreen`, TUMP_PAETE_FILM=1: his screen with the cutscene
  RawImage copied, the court, a caught player's side view; `Time.captureFramerate` 30 plus `SharedUltimatePhase.FilmClock`
  so the cutscene lasts its real 4.6 s; every world cue logged with its film time and mixed into the mp4. Sent as
  `Logs/paete-share/paete_ultimate_v3.mp4`.) Film the cutscene ON HIS SCREEN in a match: `UltimatePhaseView` draws it through its own camera onto an overlay,
  which `ImprovementEvidenceProbe.Record` (Camera.main) cannot see. Capture the phase's RenderTexture in
  `PaeteKitPlayProbe.FilmTheSentryPullingAndRootingThem`. The victim's-eye film (dragged in, held, camera swung to third
  person) was made once and overwritten; add it as its own capture.
- [x] (2026-09-26: the red was the owner's own 1.45 bulk against the 0.36 m cap. `ViewmodelArms.PaeteArmBulk` names
  the factor and `RosterArmAuditTests` gives Paete exactly that allowance over his measured 0.364 m bake; see the EditMode
  row in the ledger for the run.) ⚠️ NEW RED, 2026-09-26: EditMode 605/609 (`Logs/paete-editmode12.xml`): the three known reds plus
  `RosterArmAuditTests.EveryArmHasARealForearmAxisAndCurrentCharacterGeometry`, first seen after
  `RosterBookBuilder.RefreshPersonFromCommandLine -person paete` re-baked his clips and after the 1.45 first-person
  arm bulk in `ViewmodelArms`. Read its message and fix the cause (not the assertion). It passed at 606/609 before.
- [ ] Look notes from the films, not yet acted on: Paete reads orange rather than brown under the cutscene's lighting;
  the cutscene's stage walls read as flat boards; the editor capture runs at 7 to 17 fps (a real-time player capture
  would read better for the owner).
- [x] Rerun `PaeteKitPlayProbe` and EditMode after the rise and snapshot changes: 4/4 (`Logs/paete-play4.xml`) and
  606/609 with the three known reds (`Logs/paete-editmode5.xml`), 2026-09-26; rerun after every batch since.
- [ ] Still to do: PlayMode gate, Checks.RunAll, audits, a build. Done 2026-09-27: the rejoin test (`PaeteWorldSnapshotProbe`); the
  portrait checked against all 37 (every other hero's top at y 71 to 76 of 320, his at 118: re-aimed to `LookAt(.65, ZoomMin)` (the old zoom was clamped by
  `ModelPreview.ZoomMin` and never did anything), re-baked with
  the avatar). Done 2026-09-26: bots measured by `PaeteKitPlayProbe.PaeteBotsUseEveryAbility`
  (four Paete bots, two rounds: vine 3, bloom 4, thorns 3, ultimate 12 with the bar topped up every 20 s); the first-person
  vine film ran and is reviewed (row below).
- [x] ⚠️⚠️ THE ULTIMATE, REDIRECTED (owner, 2026-09-26, after `paete_ultimate_v3.mp4`: *"ur direction of the entire cutscene
  sucks"*, the three small trees make no sense, refine Makiling, *"make his eyes glow"*). BUILT 2026-09-26 night as three shots
  (CALL, CONNECT, RISE; direction.md 5.13, one light travelling left to right): forest trees, stage walls and landing spikes cut,
  `PaeteForestTree` deleted, the payoff is the live `PaeteSentryBody` in `Staged` mode, his eyes ignite at 0.95 s (glows found on
  his own face, `SpiritGlow.shader`), the light falls from her hands into his LEFT hand, the palm slam cracks and heaves the court,
  roots dive back in, three veins race to a pool at the landing, camera shake, theme re-timed. Makiling v3 (baro't saya, rounded
  face, parted hair, sampaguita wreath; `SpiritGhost.shader` v3 premultiplied with an inner light). Films r12 and r13
  (`Logs/paete-evidence-r12`, `-r13`); video `Logs/paete-share/paete_ultimate_v4.mp4`.
- [x] ⚠️⚠️ v5, CALLED FROM THE GROUND, NOTHING THROWN (owner, 2026-09-26 night: *"i also dont like that paete just throws seeds in his
  ult"*, *"REDIRECT IT I WANNT IT TO LOOK LIKE HE GOES TO THE GHHROUND AND HIS ROOTS CONNECT TO IT AND HE IS CHANNELLING HIS POWER AND HE
  GLOWS AND SHIT AND THEN HIS ROOTS TRAVEL TO THE GROUND AND THEN THE tree slowly show up"*, *"dont go past 5 seconds for cutscene"*,
  *"match time should pause during cutscenes"*). Design: direction.md 5.14. The seed was the LIVE cast after the cutscene (first-person
  `sentry-throw`, the arms-high body clip, `PaeteRootVein`'s lit head), all replaced: cutscene 5.0 s (was 4.6; protocol 57) in three shots,
  CALL (her full form, the light, his eyes), ROOT (kneel, both palms on the court, `PaeteGroundRoots` out of his forearms and knee, the
  channel glow `SpiritVeins.shader` on his own vines plus three quickening pulses), RISE (`PaeteRootRidge`: the court heaves and splits
  with the light inside, no lit head; the tree CRAWLS); in play `PaeteGroundCall` keeps him kneeling and joined to the ground (his view
  lowered like the taya's squat, first-person `GroundCallClip`, walking cancels it), the live clip `hero-paete-sentry` is a kneel with
  its own lift (solved so the palms sit 0 to 5 cm on the court). Catch timing unchanged (0.45 s roots, 0.75 s catch). Film r14
  (`Logs/paete-evidence-r14`, `PaeteKitPlayProbe.FilmTheUltimateOnHisScreen` 1/1), video `Logs/paete-share/paete_ultimate_v5.mp4`.
  MATCH CLOCK MEASURED in that film: 89.860 s as the cutscene came up, 89.860 on its last frame, 88.860 one second after (the probe now
  asserts it). Owner's verdict on v5 owed.
- [x] HER FULL FORM, BRIEFLY (owner: *"she sstarts translucent to full forma nd translucent again"*): `SpiritGhost.shader` v4
  `_SolidFrom`/`_SolidTo` (opaque, her palette, the cast's two bands, a glowing seam) plus an INK pass clipped to the solid band;
  `MakilingSpirit.Look.Form`/`Unform`; forms 0.30 to 0.50 s, back to spirit 0.82 to 1.08 s. Reviewed in `PaeteSpiritReviewProbe` v4/v5
  (`Logs/paete-review/paete_makiling_v5.png`: ghost, forming, full form, turning back, and the ROOT shot).
- [x] HER PLACE IN THE SHOT: the overhead crane is cut (v5 shots); for the ROOT shot she stands directly behind him and bends over him
  (`MakilingClose`), framing him from above rather than filling the left.
- [x] (2026-09-27: moss v3, each cushion a typed group of two to four taller lumps, ry 0.07 to 0.11, dark and light mixed.) ⚠️ HER MEADOW, built and wired, one fault left (owner: *"when maria makiling starts coming into the pic flowers start sprouting and
  lushh greenery"*, *"and they disappear slowly as she disappears"*). `tools/build_paete_props.py meadow` v2 (18 moss cushions, 14 grass
  tufts, 6 ferns whose fronds unroll, 6 sampaguita, 3 gumamela, 6 makahiya), `PaeteMeadow` (grows as a wave from her, flinches from the
  palm slam with the makahiya folding shut and reopening, leans from each haul, wilts from the outer edge in 3.6 to 4.5 s). ⚠️ glTFast
  negates X: the layout is written mirrored (`mx`) so it lands round her; v1 came in on the wrong side. OPEN: from above the moss cushions
  still read as outlined green discs ("lily pads"): make each cushion a group of 2 to 4 taller overlapping lumps (ry 0.07 to 0.11), typed.
- [x] THE GUARDIAN CRAWLS OUT (owner: *"i also dotn want the tree to jsut spawn in or teleport in"*): `PaeteSentryBody` v6, the same in
  play and in the cutscene: bulge, claws out and gripping (`ClawOut`), three hauls with a strain and a pause (`Heaves`, `Risen`), crown
  opens 1.35 to 1.72, eyes at `WakeAt` 1.75; limbs leave the ground while the trunk is under it; `sfx_paete_sentry_heave` (new) per haul.
  Filmstrip `PaeteReviewProbe.RunTrees` v23.
- [x] The guardian's crown masses (v8): `sentry.glb` rebuilt (`thorns.glb` untouched), seen in RunTrees v23 and at 9 m in film r14.
- [x] (2026-09-27, all six: (1) `PaeteSentry.Spawn` keeps a restored age, (2) `RecordedFieldView`'s body is `Staged`, (3) `PaeteWorldSnapshotProbe`
  written and green (it measured the 0.45 s fault before the fix), placed in the gate's match group, (4) the first-person rings at 0.45 size and
  0.35 strength on his own screen (`PaeteGroundRoots.RingSize`), (5) moss v3, (6) `RosterArms/paete_*` restored from HEAD.) ⚠️ NEXT (found this
  session, not fixed): (1) a rejoiner's sentry runs 0.45 s behind: `WorldEffectSnapshot.Apply` restores with
  `PaeteSentry.Spawn(..., age)` and `Spawn` subtracts `Flight` again; make it `_age = age > 0 ? age : -Flight`. (2) `RecordedFieldView`'s
  sentry body is not `Staged`, so a replay can spawn ground breaks into the live world; set `Staged = true` there. (3) Write the rejoin
  test (`PaeteWorldSnapshotProbe`, template `IceWorldSnapshotProbe`): plant, thorns and a mid-crawl sentry captured, applied, same
  owner/place/age, no second `PaeteRootRidge`. (4) In his first-person view after the cutscene the pulse ring round his hands is large
  and bright; smaller and fainter. (5) The meadow moss above. (6) The roster refresh re-baked `RosterArms/paete_*.asset` WITHOUT the tangent
  channel the ink reads; this session restored them from HEAD: do the same after any `RefreshPersonFromCommandLine -person paete`.
- [x] ⚠️⚠️ THE OWNER'S VERDICT ON v5, ACTED ON (2026-09-27; direction.md 5.15; the method for every hero is now `docs/HERO_KIT_METHOD.md`).
  *"Make the tre a bit smaller and a lot more sleek so that it isnt too distracting"*: v9 then v10 (`tools/build_paete_props.py` `sentry`), a
  rope of five cords twisting one way round a dark core, one vine, slimmer claw roots (2.14 m reach), at 1.3 (was 1.75), four ground branches
  (was eight), fewer falling leaves, muted crown greens (measured: v8/v9's crown rendered (158, 228, 44) against the plaza trees' (97, 124, 71)).
  Then, of v9's leaf clouds, *"it makes it look goofy"*, *"js pointy on the top with a glow coming from within"*, *"a few leaves at the edge of the
  top but dont put like a green blob"* (an Ent as the picture): v10's crown is a pointed spire of forked branches, a few leaves at the tips only,
  and a light INSIDE it that shows between the branches (`PaeteSentryBody` crown light); 7.0 m. *"dont let it be placed in a place it STANDS on
  can"*: `PaeteRules.SentrySpotClearOfCan` (core, 2.4 m, tested), `PaeteVine.SentryTarget` on every peer, the ground branches turned to part
  round the can, the cutscene's staged tree pushed by the same rule; `PaeteKitPlayProbe.AimedAtTheCanTheGuardianComesUpBesideIt` aims straight
  at the can. Prisoners held at 1.4 m (was 1.9) so their backs are against the roots, bindings in dark bark. *"add more special effects and
  vfx on his ult cutscene ... open it with leaves"*, then five Genshin burst frames and *"focus on direction and vfx and sfx"*: research from
  footage (`docs/reports/ultimate-performances-2026-09-24/research.md` section 4, for every hero) and the v6 effects pass
  (`HeroIntroductionScene.PaeteVfx.cs`): the opening gust and the wind round her, the petal ribbon, the mark of the mountain four times, the
  streaks, the brush stroke (a translucent lime body with its light down the middle), the spears of light, the haul spirals, a near layer at the lens, the phase camera's grade
  (`HeroIntroductionScene.GradeAt`), and the theme's matching layers (`glide`, `whoosh`, the mark's `bell` motif). The RISE re-framed for
  7.0 m; the ROOT shot's drifted table restored (the source was right, the committed `paete.txt` was stale). It holds still in play (owner:
  *"tree doesnt need to look left and right"*; `PaeteSentryBody.WatchPrisoners` off). Protocol 58 (the spot and the hold are computed on
  every peer). Its vines crawl (owner: *"make its vines like move or crawl"*): three living vines wind up the trunk, a ripple running up
  each (`PaeteSentryBody.TrunkVineRows`); the baked vine is gone from the model. Films r16 to r19.
- [x] ⚠️⚠️ THE OWNER'S VERDICT ON v6, ACTED ON (2026-09-27; direction.md 5.16). *"its almost perfect"*, *"A LOT MORE VFX AND SHIT LIKE THE
  GENSHIN REFERENCES"*: the burst layer (`HeroIntroductionScene.PaeteBurst.cs`, every row typed): 59 glints, 15 court shockwave rings and 4
  flashes, her rays and the crown's, the channel vortex, 32 rising motes, the arrival pillar, 26 curtain streaks, the trunk tornado, and the
  veil (`SpiritVeil.shader`, clip space). *"this felt liek a weak ending"*, *"show everyone getting pulled"*, *"shocked or trying to get out"*,
  *"follwo vines going to ppl with camera"*: THE TAKE, 3.8 to 5.0 (`HeroIntroductionScene.PaeteTake.cs`): render copies of exactly the
  players `PaeteSentry` will catch (same rule, same accepted cast; `HeroIntroductionScene` now takes the commit's aim), shocked (their rig's
  break-out frame), wrapped, yanked on his haul, spun, bound and struggling (their own `RootedMotion` clips); the camera rides the longest
  limb out, holds beside them, swings wide and settles on the guardian's face with every prisoner fanned round it. The time came from the
  setup: channel 0.2 s shorter, roots race 0.40 s, the staged tree at 1.45 times its play speed; still 5.0 s, clock frozen (film r20:
  89.860 / 89.860 / 88.860). Theme v7 re-timed with new quiet layers; the thud at 4.5 is its loudest moment. *"eye itself is ugly its weird
  that it floats and isnt embedded anywhere"*: measured, the v10 face was a flat plane 25 to 30 cm in front of the bark at the eyes; v11 grows
  a burl out of the rope and seats the sockets on it, framed above and below, the light inside (`tools/build_paete_props.py` `sentry`); the
  live vines dive under the burl (`PaeteSentryBody.FaceBurl`) and the cutscene's eye streak is gone. *"not all branches have a leaf only
  some"*: six leaves on three of five branches (was fourteen on every tip). Reach and shin radius re-measured unchanged (2.14 m, 1.17 m).
  Films r20 to r23; video `Logs/paete-share/paete_ultimate_v7.mp4` sent, verdict owed. Also: `RosterArmGeometryTests.EveryHeroUsesBothHandsAndReturnsCleanly` now names the hero and action it fails on;
  Bayan Plaza's 16 house finishes are marked fitted trim (`BayanHouseFinishAuthor.MarkFitted`, `AirborneByDesign`), which is what failed
  `Checks.RunAll`'s map geometry on the v6 handoff.
- [x] ⚠️⚠️ v8 (2026-09-27; direction.md 5.17). *"slow down ult a bit"*: 6.5 s (his choice), every beat 1.3 times as long
  (`PaeteStretch`, `_stretch`, the theme's `T`; `UltimatePerformance.MaxSeconds` 6.5). *"he is supposed to be watching cutscene too"*: the
  film's first-person part is play resuming; it no longer grows and catches a second time (his choice, *"Yes, no repeat"*): the live
  guardian is handed back grown (`PaeteSentry.BodyLead`) and catches at once. *"make it so that paete can choose ... where his ult will be
  cast"*: hold-to-aim placed where he looks (`HeroAbility.AimsWhereLooking`, `CameraRig.TryLookGround`, 3 to 8 m), the Groot-wall answer.
  *"they should face against the tree"*: turned to face out when held (`PaeteRootCoil.Attach(body, tree)`, `CameraRig.FaceHeldView`). The
  escape filmed (18 s film, the filmed player holds Interact until free). Protocol 59.
- [x] (FIXED 2026-09-27: the shell was the seedling's own INK. `ToonSkin.Apply` sizes an outline as width / the part's current scale,
  and `PaeteProp.Redress` re-dressed the pitcher on its first pose while it was still popping up at nearly zero scale, so its
  inverted hulls came out up to ten thousand times too wide and grew with the plant. `PaeteProp.Spawn` now records each part's spawn
  scale and width (`PaeteOutlineRest`) and `Redress` keeps them; the seedling, thorn and sentry play tests pass.) Found by the skills film (2026-09-27, `PaeteKitPlayProbe.FilmHisSkillsInAMatch`, frames `Logs/paete-evidence-s2`, video
  `Logs/paete-share/paete_skills_v1.mp4`): from about 0.1 s after BAKYA BLOOM is planted (film frame 78 on), a building-sized dark jagged
  shell covers the background in every camera that sees the court near the taya (brown when lit, black from the shade side), and stays
  for the rest of the film; the taya bot's name tag floats inside it. All four skill checks still pass. Find the renderer (log every
  renderer whose bounds exceed 6 m after the plant lands) and fix its scale or source; suspects: the plant's `PaeteGroundBreak`, the
  bot Paete's model or first-person arms rendered in a world camera.
- [x] ABILITY-2 lane, found here, FIXED 2026-09-26 (cloud): `RosterArmGeometryTests.EveryHeroUsesBothHandsAndReturnsCleanly` failed on
  `sean/sean_skill2d`, the COMING SOON defending slot (`PlaceholderRoleAbility`), which casts and does nothing and so has no hands on
  purpose. The gesture tests (`RosterArmGeometryTests`, `HeroPresentationTests.EveryHeroAbilityHasBespokeCastAndViewModelActions` and
  `ViewmodelArms_PreservesHeldSlipperAndActions_AcrossCharacterSwaps`) now skip a placeholder, which stops matching the day its hero's real
  skill replaces it. Cloud EditMode: `RosterArmGeometryTests` 3/3 (`Logs/paete-cloud2/editmode.xml`).
- [ ] **THORN HARVEST placed where he looks, not on his body (owner, 2026-09-26: *"I WANT IT to be castable and not cast on body make
  it possible for him to place it somewhere else like his ult and other skill (do they do that already?)"*; answer: BAKYA BLOOM and
  MAKILING'S EMBRACE already were, LIANA LEAP aims at a spot, THORN HARVEST alone was on his feet).** Built: the same hold-to-aim as his
  ultimate (`AimByHolding(..., whereLooking: true)`, from his feet out to `PaeteRules.ThornAimRange`, 6 m, PROPOSED: BAKYA BLOOM's reach);
  he still stamps, and a line of the rattan's own thorn shoots races through the court to the spot (`Visual.PaeteThornTrail`, ten shoots
  typed by hand, 24 m/s, at most 0.25 s, one hold beat) where the rattan bursts and catches every slipper within 7 m of THAT spot, which
  land 1 m from it. The catch set is decided at the burst. Bots place it where it takes most (`AIController.PaeteThornAim`). Protocol
  60 (every peer computes the spot and the trail). Core 653/653 (`TheThornsArePlacedWithinHisPlantsReachAndArriveInOneHoldBeat`).
  Cloud PlayMode 2026-09-26 (`Logs/cloud5/playmode.xml`, 6/6): `ThornsTakeASlipperOutOfAHand` (aims 5 m away, asserts the burst
  spot and that the slipper went to the thorns, not to him) passes; filmed in `FilmHisSkillsInAMatch`. Open: the owner's verdict on
  the 6 m reach in play.
- [ ] **LIANA LEAP from his own eyes: his arms extending (owner, 2026-09-26, on a first-person frame: *"refine this too for his point of
  view make it look like its actually his arms extending bcz it doesnt look like taht"*).** Found: the braid started at the RESTING hand
  while the drawn arm is lensed per render (pulled toward a 95 degree look, lowered 8 cm), so the two thick bark limbs sat beside the
  hands as planks. Built: `ViewmodelArms.TryDrawnArm` reads where the arm is DRAWN; his first-person forearms lengthen a third with the
  reach (`SetReachStretch`); each braid starts inside the drawn forearm, 30 % back from the hand, no wider than the arm. Then *"it
  doesnt bend with arms tho"*: `PaeteVineReach.BendAlongArm` re-lays the centreline as a curve that leaves along the drawn forearm's
  own direction (a control point a third of the way out, at least 35 cm) and bends to the anchor, the sag and wave riding on top.
  Looked at in the skills film's owner view (`Logs/cloud5/paete-skills-film/owner/`, frames 24 to 56): both vines now run on from
  the forearms and curve out to the anchor. Open: the owner's eye on it.
- [x] **The skills film showed a HUMAN casting Paete's skills (owner, 2026-09-26: *"idk why a fkn CHARACTER was the one doing the shit
  instead of the plants"*). FIXED in the film rig, not the game:** `PaeteKitPlayProbe.Paete()` re-bound the kit on a seat the match had
  already dressed as someone else, so the taya casting THORN HARVEST was a curly-haired human, and the one seat that did wear Paete was the
  local one, hidden from the court camera by the first-person self-hide. Every converted seat now wears his model (and the local seat's
  first-person arms are re-matched), and the film cameras show every body the way a spectator sees them (`RenderFilmView`). Real matches
  dress each seat from its pick at install, so players never saw this. Film `Logs/paete-cloud2/paete-skills-film` (first Unity film shot
  in a cloud session, software OpenGL), stitched as `Logs/paete-share/paete_skills_v2.mp4` and sent. Owner's verdict on v2 owed.
- [x] Prisoners "actually TIED" (owner) reviewed close up in film r16's `victim/`: backs pressed to the trunk, straining, bands at the shins;
  the lit bark in the limb and bands blended into warm skin, so both are dark and mid bark now.
- [x] First-person vine film reviewed (film r11 `owner/`): both hands punch forward, the braids leave the viewmodel hands and converge on
  the anchor, the landing rosette flashes at the hands. Reads.
- [ ] Surface texture: the owner asked to *"really refine and texture and make it all detailed"*. The
  modelled props give detail in geometry and his palette only; decide with him whether bark wants a
  painted texture (grain, rings) on the props and vines, and do it if so.
- [ ] Deploy the cloud-code hero lists once they name him (needs the owner's UGS deploy); record his
  lines (human voices only, `docs/HUMAN.md` PAETE rows).

### PRACTICE-1 · A practice picker and a Valorant-style training range ⚠️ OPEN, 2026-09-26

Owner, 2026-09-26: *"can u also make it so that when u click practice theres a screen that pops up
that lets u pick between Tutorial and Training mode"*, and *"allow character change + cheats +
summon/remove bots in practice just like valorant practice"*.

- [ ] PRACTICE opens a two-card picker (TUTORIAL, the guided `GuidedTraining` walk-through; TRAINING,
  the free range), built through `MenuKit`/`ConvertedScreen` so pad focus, thumb targets and one-press
  back come by construction (CLAUDE.md 4a, 6.2).
- [ ] Training range panel (pause-style, opened by one bound key, pad and touch answered): change hero
  or person in place; cheats (infinite skills, no cooldowns, ultimate full, freeze the can, infinite
  stamina); summon a bot (seat, role, idle or active) and remove bots. Offline only, never on the wire,
  never reachable from a networked or ranked match.
- [ ] Render every state over the real background at his window shape; record the journey (presses to
  each action).

### SKILLUI-1 · The yellow skill ring, and cooldowns against charges ⚠️ OPEN, 2026-09-26

Owner, 2026-09-26: *"the yellow circle looks bad af with a lot of skills, one of which is updraft"*,
and *"make the distinction between a skill that has a cooldown and a skill with charges clearer"*.

- [ ] The yellow ground ring (`GroundReticle` and each kit's `telegraphRadius`): inventory every skill
  that draws it (Updraft first), photograph each over a real court, and replace the flat yellow ring
  with a telegraph in that ability's own element and shape, or none where the effect already shows
  its footprint. Keep the readability rule (the can, slippers and players stay readable on Low).
- [ ] HUD ability tiles: a cooldown skill and a charges skill must read differently at a glance
  (e.g. a smooth sweep for a cooldown against notched pips with a count for charges, the meter
  language VISUAL-1 already uses: `HudRing.Notches`). Render both states of both kinds on the owner's
  window shape and on a pad and a phone.

### GAMEANIM-1 · The can raise crouches first ⚠️ IN PROGRESS, 2026-09-26

Owner, 2026-09-26: *"i want u to improve animation of raising can too when its down"*, *"they should
crouch first and put it up"*. `Visual.CanRaiseShape` is one crouch, grip, lift, set curve over the
channel, shared by the body (`CharacterAnimator.ResetRaise`: legs splay with the root dropped by the
height the splay costs, so the feet stay planted) and the first-person view (`CameraRig.ApplyFpp`:
the eye drops 0.34 m and tips 16 degrees down; `ViewmodelArms.RaiseCan` reaches lower in the squat).
The v1 capture found the body replaying the 0.33 s `pick-up` one-shot on every 0.4 s relayed `grab`
(torso 87, 42, 83 degrees and back); a raise no longer replays it.

- [x] Shared shape, body squat, FPP eye drop, one-shot suppression.
- [x] Native before/after frames from `GameplayActionShots.RaisingTheCanInBothViews` (window now
  4.0 s so it reaches the lift), inspected: `docs/reports/can-raise-crouch-2026-09-26/`.
- [ ] The owner's eye on it in play.

### BUGS-0926 · Owner bug list on the lighting branch ⚠️ IN PROGRESS, 2026-09-26 (all six fixed; .1 and .6 await a look in play)

Owner, 2026-09-26, six non-gameplay bugs on `merge/astra-lighting-2026-09-25`. Each fix is one
commit.

Evidence (`484562c9`, Mac, one PlayMode launch, total 5 failed 0):
`HubFlowTests.HomeAndEveryDoorOpensItsScreenAndBackReturns` (.2, .4),
`HubFlowTests.QueuePlateMatchFoundCharacterSelectLobbyAndLoadingAreDrawn` (.3),
`WorldCourtCueTests.LightingStyleThumbnails` and
`TumpNativeSettingsTests.LightingStyleCardsSwitchTheLookAndJoinSaveAndDiscard` (.5, photograph in
[reports/bugs-0926/](reports/bugs-0926/lighting-style-cards-1920x1080.png)), and
`HomeFlowTests.TitleIsOnePressAndKeepsHerStreetMoving` (the title still builds with one press
target). Not covered by a test: an actual keyboard key on the title (.1) and the stamina arc in a
live match (.6); both want a look in the owner's editor.

- [x] BUGS-0926.1 The title screen ("Click anywhere to continue.") also continues on any keyboard key.
  `MenuNav.KeyboardAnyPressed` (any key except Escape, which still quits from the title, and
  except Alt chords, so Alt+Enter still toggles fullscreen); `OwnerMenuPrompt` invokes the same
  full-screen press with it, after a 0.25 s arrival guard so the key that finished the previous
  screen cannot skip this one. Her wording is unchanged.
- [x] BUGS-0926.2 Escape on HOME no longer returns to the title screen. `HubHome.Back` (Escape,
  pad B, Android BACK) left the room and loaded `MainMenu`; it now opens the hamburger MENU, and a
  second BACK closes it. BACK TO TITLE in that menu is the deliberate way out. Queued, BACK still
  cancels the queue first. `HubFlowTests.HomeAndEveryDoorOpensItsScreenAndBackReturns` asserts it.
- [x] BUGS-0926.3 Pressing the IN QUEUE button cancels the queue. `HubHome.Tick` used to make
  the button non-interactable while queued; it now stays pressable and `HubHome.Play` calls
  `CancelQueue` (the same call as the plate's X and BACK) unless a match was already found.
  `HubFlowTests.QueuePlateMatchFoundCharacterSelectLobbyAndLoadingAreDrawn` queues again and
  leaves through the button.
- [x] BUGS-0926.4 The hamburger MENU popup no longer swaps HOME's background for the live court.
  Cause: `HubSceneVideo` showed the HOME loop only while `TumpHub.AtHome` (HOME on top of the
  stack), and the MENU is a popup pushed on top, so the loop hid and the live court showed
  through the popup's scrim. It now reads `TumpHub.ShowingHome`, the top non-popup screen.
  `HubFlowTests.HomeAndEveryDoorOpensItsScreenAndBackReturns` asserts the loop stays up under
  the MENU.
- [x] BUGS-0926.5 Lighting styles: Bright is renamed Standard, stays the default and moves to slot 1;
  Classic is renamed Nostalgic and moves to slot 2 (the owner chose this reading of "make the
  classic lighting style the default, rename it to Standard" when asked). The swap moves both
  stored indices, so the stored field is now `GameSettings.LightingLook`; the old
  `LightingStyle` field is read once by `Validate` through `LightingStyles.FromLegacy` (old 0
  Classic to Nostalgic, old 1 Bright to Standard) and cleared to -1. Thumbnails renamed to
  `standard.png` / `nostalgic.png` with their GUIDs kept. `WorldCourtCueTests.LightingStyleThumbnails`
  asserts the migration both ways.
- [x] BUGS-0926.6 The stamina arc beside the reticle drains from the top. `HudRing.FillFromEnd`
  anchors the fill at the arc's lower end; only the stamina arc sets it, so cooldown sweeps and
  the notched ultimate are unchanged. Not yet seen in a native match.

### LOAD-1 · Loading screens end when the work ends, and warm everything first ⚠️ IN PROGRESS, 2026-09-27

Owner, 2026-09-27: "make optimized loading so every shader and every shit will render and load
in the loading screen, the loading screen is hardcoded to be 5 seconds. fix that, make it also
it downloads or renders in the background in the loading screen".

- [x] LOAD-1.1 The boot screen's random 5 to 15 s reading window is gone
  (`LoadingPresentation.CanLeave` takes no clock). It leaves when the preload, the held menu load
  and sign-in are done; an opened story card still holds it, because that is the player's press.
- [x] LOAD-1.2 Boot warms every map's assets (`SceneFlow.Maps`), not only Eskinita and Bayan
  Plaza, so Ilalim ng Tulay, Sa Bubong and the Lagoon no longer load cold on PLAY.
- [x] LOAD-1.3 `Visual.ArenaPrewarm`: behind the arena curtain the match camera draws the loaded
  arena offscreen from 20 viewpoints, one per frame, into a target of the screen's HDR/MSAA
  format, so pipeline states, meshes and textures are on the GPU before the first visible frame.
  `HubLoading` lifts when that finishes; its 2 s hold is gone.
- Evidence (Mac player built from this work): boot loading finished after 2.53 s (it waited at
  least 5 s before); a bot match's Eskinita curtain lifted after 1.60 s with the prewarm taking
  0.84 s; Ilalim through HOME lifted after 1.47 s (prewarm 0.87 s). Not measured: the Windows
  tournament machine, a phone (five warmed maps are held in memory by `WarmAssetCache`, which is
  a memory question on Android), and whether a first-turn hitch is actually gone in play.
  Tests (Mac PlayMode, total 2 failed 0): `HomeFlowTests.LoadingTipsStayInlineAndReadinessStillGatesTheTitle`
  and `HubFlowTests.QueuePlateMatchFoundCharacterSelectLobbyAndLoadingAreDrawn`.

### LIGHT-4 · Ilalim ng Tulay lighting changes with view angle and distance ⚠️ OPEN, 2026-09-27

Owner report with four frames: the street loses its sun shadows and goes flat and bluish from
some positions, and gets them back closer to the shops. Investigation and numbers are in
[reports/ilalim-lighting-2026-09-27/](reports/ilalim-lighting-2026-09-27/README.md): across
offscreen PlayMode renders and two real Mac player sweeps of the back buffer (392 poses each,
one entering through HOME and the loading curtain), the sun's shadows were drawn at every pose.
Eliminated: the match-end portrait's preview key light, fog, occlusion culling, graphics tier,
lighting style and MSAA. Not reproduced. Next: the owner's frames are from editor Play mode, so
check whether it happens in a built player; if editor-only, suspect the Scene view camera
interleaving with `WorldLookPresentation`'s per-camera globals. The in-player probe is
`-tp-shadowsweep DIR [-tp-shadowsweep-hub] [-tp-map ID]` (`Diagnostics.WorldShadowSweepProbe`).

### LIGHT-5 · Map select washed out and brighter than the match ✅ FIXED, 2026-09-27

Owner report with a HOST GAME frame: the map preview is far too bright and should match the
game's actual lighting. Cause, measured on the real HOST GAME screen: the hub runs two
`MapPreviewSurface`s and both load the selected map on their first frame. Each claimed its load
with `GetSceneByName`, which returns the FIRST scene of that name, and one surface is also
deactivated mid-load, which stops its coroutine. Either way one copy of the map was nobody's:
never confined to the preview layer, never parked, its sun left on with every layer in its mask.
That orphaned Eskinita sun lit every map previewed afterwards on top of the map's own sun, and a
new orphan could appear each time the hub was rebuilt. Fix: the surface claims the exact scene its
own load created, in the load's `completed` callback (so a stopped coroutine cannot orphan it),
confines and parks it at once, and unloads it if the surface was destroyed.

Evidence in [reports/map-preview-2026-09-27/](reports/map-preview-2026-09-27/): before/after for
all five maps (preview mean luminance Eskinita 0.559 to 0.494, Bayan 0.644 to 0.594, Ilalim 0.693
to 0.612, Sa Bubong 0.671 to 0.599, Lagoon 0.751 to 0.686), and the fixed preview beside the
match camera rendered from the same pose (0.494/0.492, 0.594/0.579, 0.611/0.627, 0.599/0.600,
0.686/0.692). The haze that remains in the distance is each map's own fog at that height.
Tests (Mac PlayMode, total 3 failed 0): new `HubFlowTests.HostGameMapPreviewIsLitByTheShownMapsSunAlone`,
`WorldCourtCueTests.MapPreviewShowsTheBrightLookAndHandsEachMapItsLightingBack`,
`HubFlowTests.HomeAndEveryDoorOpensItsScreenAndBackReturns`.

### LIGHT-1 · Bright PEAK-style lighting and edges ⚠️ IN PROGRESS, 2026-09-23

Integrated into ASTRAReworks on2026-09-24at owner request, through lighting branch
50f1fc255(merge241e13bb5). Existing Windows Stage/ramp checks2/2passed; five-map
after frames inspected. This is now the map-art baseline. Remaining preview,
tuning and performance rows below stay open; keep tracking the source branch.
[Integration evidence](reports/lighting-integration-2026-09-24/report.md).

Owner request: overhaul the "gloomy and dark" lighting to be bright and pleasing like PEAK
(Aggro Crab and Landfall, 2025), not realistic, and make the edges similar to PEAK. Work is on
branch `lighting/peak-bright-overhaul` (off `ASTRAReworks` at `2a3c7e16`), one commit per
change. This supersedes VISUAL-1.8's darker ambient and near fog on that branch; the off value
(`WorldCueProfile.WorldLighting` 0) still restores each scene's own lighting.

**Research (PEAK Steam store frames, Game Informer review):** high key with no true black;
shadows carry hue (warm sun, sky-coloured shade); bright horizon-matched haze layers the
distance; soft plush shading on the cast with no ink; edges read from light and colour (lit
convex bevels, coloured inside corners), not black lines; soft bloom on sky and highlights.

- [x] LIGHT-1.1 Per-map rig in `WorldLookProfile` (+ asset): sun colour, intensity, lifted
  elevation (azimuth kept), shadow strength, saturated sky-tinted ambient, haze colour and
  range, sky/cloud colours, coloured black lift. Applied and restored by `WorldLookPresentation`.
- [x] LIGHT-1.2 Cast soft wrapped terminator with warm band (`Toon`, `ToonTransparent`).
- [x] LIGHT-1.3 Cast hull drawn in a deeper shade of its own colour at 72% width; coloured
  gameplay outlines (landed slipper) untouched; previews keep black ink.
- [x] LIGHT-1.4 World edges: near-side silhouette deepening, sun-side convex highlight,
  coloured concave crease, replacing black ink under the look (`WorldOutline`).
- [x] LIGHT-1.5 Grade: HDR bloom (off on Low tier), coloured lift, vibrance (`ColourGrade`).
- [ ] LIGHT-1.6 Tune from the first render. The v2 render fixed four
  of the five v1 problems, each its own commit: bloom threshold 1.7 and intensity 0.12 so the lit
  cast no longer haloes (`7f7dc42e`); Eskinita and SaBubong skies blue instead of lavender, top of
  frame (195,196,239) to (154,191,241), by pairing a cream or gold horizon with a cyan-leaning
  zenith (`d30fcb16`); Bayan and Lagoon haze deepened to a light sky blue (`eb27679c`); per-map
  `GroundLift` 1.6 on the Eskinita and Ilalim court asphalt through a property block, sunlit road
  (90,85,71) to (158,150,119) on Eskinita and (124,126,106) to (181,185,156) on Ilalim, found by
  shape and logged (`68d297ba`). Edge close-up checked: no black ink, silhouettes darken their own
  colour, eaves carry a lighter bevel. SaBubong read milky in v2, so its haze now runs 60 to 300 m
  (`848e21d4`); the v3 render confirms it: the ring of towers keeps more colour and edge than in
  v2, and the warm horizon still lightens the farthest ones. Remaining visual review stays
  open: whether Ilalim's sunlit road at 181 is too pale;
  overall contrast, which is lower than PEAK's; SaBubong's high preview shot, where the street
  40 m below the roof fogs into flat peach; Ilalim's preview, which reads very pastel; and the
  cast hull on dark colours. The self-shade rule (`pow(albedo, 1.6) * 0.42`, Toon's OUTLINE
  pass) turns light skin into a deeper peach but dark brown skin into near ink: the native
  Ilalim first-person arms sample (32,23,22) at the edge against (151,90,51) skin. A PlayMode
  A/B confirmed the viewmodel does get the look (weight 1, CastInkSelf 0.88, black ink with the
  look off), so this is the formula, not a missing hookup. The hull call is now a number rather
  than a description: `WorldLookProfile.CastInkFloor` keeps the hull at no less than that share of
  its colour's luminance and ships at 0, today's hull exactly. Rendered at 0, 0.25 and 0.35 on the
  native probe's brown-skinned seat 1 ([report](reports/light-1-2026-09-24/report.md), frames in
  `hull-choice/`): the darkest tenth of the Ilalim arm edge goes (25,19,18), (47,32,31),
  (55,38,35), a near-black line becoming a dark brown one. It is not only dark skin: 0.25 also
  lifts the hulls around tan skin, greys and greens in the cast shot, 0.35 adds orange, and the
  black loafer's edge goes from near black to dark grey. A fade sparing
  near-black was tried and removed, because `ViewmodelArms.SkinMangKanor` sits at luminance
  0.020, level with dark hair. The owner picks 0, 0.25 or 0.35, or asks for more.
  Source a28037622 integrated with our explicit-sun/preview restoration preserved.
  Windows focused floor-choice case1/1 passed; actual current-map comparisons and
  grey inspected. Default0 retained, alternatives available without blocking maps.
  [Local integration evidence](reports/lighting-integration-2026-09-24/hull-floor/report.md).
  SaBubong's preview court marks now use separate dark warm paint on only six
  lines, following a fixed-camera three-colour study. Study/final native1/1 each,
  actual colour/grey inspected. [Court-paint evidence](reports/map-by-map-refinement-2026-09-23/rooftop-court-contrast/report.md).
  High-preview haze then compared60-300/90-380/120-480m;90-380selected for middle
  facade identity and a softer far skyline. Saved roof profile and static card
  updated, importer/GUID preserved. Actual final v2 colour/grey inspected,1/1.
  [Haze/card evidence and corrected persistence mistake](reports/map-by-map-refinement-2026-09-23/rooftop-haze/report.md).
  Lower-ground material/context assessment and integrated map gates remain open.
  Lower roof streets now have restrained markings and two supported parked native
  tricycles. Original asphalt/buildings/physics retained; native v2 1/1, actual
  matched preview/street witness/grey inspected, card refreshed. One camera repair
  used; visibility limits retained. [Street-context evidence](reports/map-by-map-refinement-2026-09-23/rooftop-street-context/report.md).
- [x] LIGHT-1.7 The two Stage tests assert the bright look's own claims (`20c977e5`): applied rig,
  bright shade colour, haze past the court, court ground found; toon ramp measured 1.70:1 under the
  look against 1.95:1 authored, asserted inside 1.35 to 2. WorldCourtCueTests 3/3 on the Mac.
- [x] LIGHT-1.8 Map-select/lobby previews now install the adopted selected-map look
  with an explicit sun and measured floor. Tagged world cameras only; same-map
  refresh reuses the live rig/sky. Map switches and destruction release the old
  state; cached-map court brightening fixed by clearing property-block ownership.
  Initial preview/transitions4/4passed, focused revisit/preview2/2passed; actual
  overview/small/grey25inspected. [Evidence](reports/lighting-integration-2026-09-24/preview/report.md).
  Tracked source advanced to 8d73471f3 on2026-09-24: HDR preview target and
  active-scene handback guard merged with our existing cache/preview fixes.
  Focused native 2/2 passed; actual five-map LDR/HDR pairs and grey inspected.
  [Follow-up evidence](reports/lighting-integration-2026-09-24/preview-followup/report.md).
  Source branch also records 4/4 on macOS for its original preview/look cases;
  the Windows integration evidence above remains the current local receipt.

  Source 429643416 also integrated: edges use the selected preview sun, and the
  native probe records a same-binary look-off comparison. Local five-map edge-key
  case v2 1/1 passed; matched colour/grey inspected after one orbit-capture repair.
  [Edge-key integration](reports/lighting-integration-2026-09-24/preview-edge-key/report.md).
- [ ] LIGHT-1.9 Performance check of bloom plus edges on the Balanced tier, and a native build
  look at the owner's window shape. Mac half done: the native graphics probe now measures every
  map and tier a second time with `WorldLighting` 0 into `world-render-look-off.csv`
  (`f2685884`). Native macOS player built by `GameBuilder.BuildMac` from `8d73471f` plus that
  probe edit, Apple M5, Metal, 1920x1080 HDR, 120 uncapped frames per cell. Balanced costs 0.30
  to 0.46 ms per frame with the look on (Lagoon the most); the worst Balanced frame is Ilalim at
  5.63 ms mean and 5.91 ms p95 (off: 5.33 and 5.53). Low, which skips bloom, costs 0.06 to 0.18
  ms, so most of the cost is the bloom chain. The native frames show the look.
  The owner's window shape is now photographed natively on the Mac
  ([report](reports/light-1-2026-09-24/report.md)). The probe takes one extra frame per map on
  the player's real window at Balanced, measured with the look on and off in the same binary,
  and re-applies the requested size because `GameSettings.ApplyDisplay` overrides
  `-screen-width` at boot (the first attempt asked for 1600x680 and photographed 2940x1912).
  At 1600x680 the look costs 0.11 to 0.22 ms per frame, worst frame Ilalim 5.12 ms mean and
  5.49 ms p95; all five frames show the look with no black world ink. At the Mac's own
  fullscreen, 2940x1912, it costs 0.73 to 1.31 ms. Still open, and only this needs hardware the
  Mac lacks: `tools/graphics_review.py` on the Windows tournament machine. It now defaults
  `--window` to 1600x680, so that one run also photographs the owner's shape on Direct3D.
- [ ] LIGHT-1.10 Mac fullscreen hitching, found by LIGHT-1.9 and not caused by the look. At
  2940x1912 fullscreen on the Apple M5 the Balanced window path runs 8.2 to 13.3 ms mean with
  the look OFF but 27.5 to 43.4 ms p95, three to four times the mean on every map; with the look
  on it is 8.9 to 14.2 and 33.4 to 47.7. The same maps at 1600x680 windowed have a p95 within
  0.1 to 1.7 ms of the mean. Numbers in `reports/light-1-2026-09-24/mac-fullscreen-2940x1912/`.
  Not yet known: whether it is the present path, the macOS scale from 2940x1912 down to the
  2560x1664 panel, or fill rate. Done means the cause is named from a measurement and either
  fixed or recorded as a platform limit with the Mac player's numbers.

Capture: `WorldCourtCueTests.BrightLookSameCameraCapturesOnAllFiveMaps` writes stage, eye and
cast frames per map to `TUMP_WORLD_CUE_OUT`. Baseline 1/1 and branch v1 1/1 passed on the Mac.

### LIGHT-2 · Lighting style picker in the Graphics tab ⚠️ IN PROGRESS, 2026-09-25 (only slot 3 open)

**Renamed 2026-09-26 (BUGS-0926.5):** Bright is now **Standard** (slot 1, the default) and Classic is
now **Nostalgic** (slot 2). The entries below keep the names they were written with.

Owner request (2026-09-25), with a PUBG Mobile Style row as the reference: a style setting in
the graphics settings with three slots. Slot 1 is the lighting on `main`, slot 2 is this
branch's bright look, slot 3 is a placeholder. Picking a style changes the game's lighting.

**What slot 1 is, measured.** `main` has no world look at all. Every map's authored
RenderSettings (fog, ambient trilight, skybox) and directional sun (colour, intensity) are
identical on `main` (`85504a52`) and this branch, checked scene by scene for Bayan, Eskinita,
Ilalim and SaBubong (the Lagoon is not on `main`). So slot 1 is the look at weight 0, which
every consumer already treats as the scene's own lighting. It keeps this branch's newer map,
material and court work; only the lighting is `main`'s.

- [x] LIGHT-2.1 `Settings.LightingStyles` (Classic weight 0, Bright weight 1, a placeholder that
  is not selectable), `GameSettings.LightingStyle` (default Bright, the look this branch already
  draws for everybody, so an upgraded `settings.json` changes nothing; a stored placeholder
  normalises to the default), and `WorldCueProfile.LightingWeight`, the product of the profile's
  `WorldLighting` and the style. Every runtime read of `WorldLighting` goes through it (look,
  contact shadows, world outline, recorded and ultimate views). Local only, never on the wire.
- [x] LIGHT-2.2 The card row (`SettingsStyleCards`), second on the Graphics tab, three 376x211.5
  cards with a caption and an accent ring on the pick. It applies live and joins save and
  discard. Thumbnails in `Resources/UI/lighting-styles/`, rendered by
  `WorldCourtCueTests.LightingStyleThumbnails` from one Eskinita camera, which also asserts that
  Classic hands the authored ambient and fog back exactly.
  Evidence (`daf427e2`, Mac, one PlayMode launch, total 3 failed 0):
  `TumpNativeSettingsTests.LightingStyleCardsSwitchTheLookAndJoinSaveAndDiscard` presses a card
  through a real raycast (live weight, ring, dirty session, discard restores);
  `WorldCourtCueTests.LightingStyleThumbnails`; and the older
  `FiveMapStageCapturesPreserveGeometryAndRestoreOriginalLighting`, rerun because every look
  consumer now reads the weight through the style. Frames in
  [reports/light-2-2026-09-25/](reports/light-2-2026-09-25/): the row at 1920x1080 and the owner's
  1600x680, a Classic pick, and both thumbnails. The first photograph put the third card 3 units
  past the row rule; cards went from 384 to 376 (`daf427e2`). Not done: a native player build,
  a pad walk of the row, and a look inside a live match's pause menu. That menu opens the same
  `TumpSettingsView` through `ConvertedSettingsPanel`, so the row is there by construction.
- [ ] LIGHT-2.3 Slot 3 content. The owner's call: the card shows an empty slot until then.

### LIGHT-3 · Tone down the Bright style: colour-theory light, depth, blocky clouds ⚠️ IN PROGRESS, 2026-09-25

Owner request (2026-09-25), with a PEAK frame of three climbers on sand as the reference: "tone
down the brightness on the bright lighting style. it currently is too bright and the character
glows", "overhaul the lighting if needed, remove the bright finish on all characters", "do not
make the cloud realistic. do not go towards the route of realism". Classic is not touched.

**Reference, measured from the owner's frame:**
- The lit green body is (48,160,77), luma 130, and the khaki shirt is (239,194,97). Nothing on a
  character is near white.
- The darkest 1 per cent sits at luma 64.
- Shadow on sand is a deeper, more saturated sand, (165,92,51).
- The sky is a pale mint, (210,230,222), with low-contrast brushed clouds, and the far mountain
  dissolves into teal air.

Public write-ups of PEAK's lighting internals were not found (searched 2026-09-25), so the frame
is the evidence.

**Before, measured at `d27c9712`:**
- The Eskinita cast shot had the yellow jacket at (255,225,5) and orange skin at (244,121,5), so
  red was clipped, blue had collapsed and the colours read neon.
- The Bright sky was a saturated poster blue, (131,184,241) on Bayan.
- The current renders of both styles on all five maps (sky, wide, eye, stage, cast) are in
  [reports/light-3-2026-09-25/before/](reports/light-3-2026-09-25/before/).

**Causes found:**
- The cast was lit at albedo x (1.34 sun + ~0.65 ambient), about 1.9 before the curve.
- Vibrance 0.24 drove already saturated colours to their floor.
- The bloom soft knee was a hard-coded 0.6 of the 1.7 threshold, so the chain collected every
  value above 0.68, which is every sunlit body.
- Three finishes sat on the cast: the cream upper rim, the metal glint, and the
  distance-readability lift with its cream rim past 5 m.
- The warm terminator band pushed red by 1.25.

**Second owner direction, same day, after the first tone-down render:** the lighting looked flat,
for three reasons in the owner's words. (1) "the textures are flat with no depth/normal map
added". (2) "the lighting itself leans towards adjusting the shadows and brightness instead of
adjusting the ambient hues", while PEAK uses "artistic color theory for shadow and light colors
(leaning more towards a slight purple instead of a plain dark shadow for cooler areas, and a more
fuzzy orange for warmer settings)"; look at illustrated environment concept art and stylised 3D
environments. (3) "skybox should be either 2d hand painted designs, or maybe try a more blocky
style of clouds where they are real 3d assets". On the first blocky clouds: "too small", "less
volume-y", "too sharp", look at blocky cloud references. Then: "aren't they being rendered inside
out?" They were (winding, below). The owner put character shading (cel against normal) off for
now; the study is in the scratchpad and not part of this entry.

**Research used:** the colour-theory sources agree that lit areas shift toward the light's hue
and shadows away from it, and that warm light against cool shade makes depth. So the shade leans
violet under a peach key. The blocky-cloud references (Minecraft Better Clouds, Photon's blocky
mode, voxel cloud renders) read as volume through four things: a domed mass of many blocks,
light that rolls over the blocks, darkened crevices, and fewer, bigger clouds.

- [x] LIGHT-3.1 The tone-down (`8e7b9707`, black floor `a234bc4b`):
  - the sun goes to about 1.08 and the ambient to about 0.8 of its old strength;
  - vibrance drops to 0.14, and bloom to 0.05 at threshold 2.2 with a 0.2 knee (`BloomKnee`);
  - `UpperRim` and `MetalHighlight` go to 0;
  - the distance-readability lift fades out under the look (`Toon.shader`), while Classic keeps it.
  Measured on the Eskinita cast shot, the share of neon-clipped cast pixels went from 7.2 to 2.8
  per cent; on Bayan and the Lagoon it went from 4.8 to 0.
- [x] LIGHT-3.2 Colour-theory light, per map:
  - a warm key: peach, orange on the alley and rooftop, cream under the bridge and on the lagoon;
  - the ambient sky term, which is every shadow's colour here, goes violet, the equator mauve and
    the ground term a warm orange bounce;
  - the cast's `ShadowTint` leans violet;
  - the terminator is a fuzzy orange at constant luminance (0.22, adds no light);
  - a split tone in the grade (`SplitTone` 0.18, `ShadowHue` violet, `HighlightHue` warm).
  - `Lift` was being sent through `SetColor`, which converts it from sRGB, so the linear black
    floor reached the shader at about a thirteenth of its authored value. It is a vector now, set
    at 0.03 to 0.06 linear, a violet-tinted floor near PEAK's luma 64.
- [x] LIGHT-3.3 Depth without a texture sweep (`WorldOutline`): inside corners take a violet
  cavity (`CavityHue`, `CreaseShade` 0.45), and walls take ground occlusion toward it within 2.8 m
  of the court (`GroundOcclusion` 0.28), from the depth the pass already reads. The cast is excluded
  through the mask, and the Low tier skips it.
  Not done, and why: real normal maps are per-asset art, and AGENTS.md forbids one texture or
  noise sweep across every building (REFINE-2 does maps one at a time). If the owner still wants
  normal detail after this, it belongs in each map's REFINE-2 pass.
- [x] LIGHT-3.4 Blocky clouds (`BlockyClouds`, `BlockyCloud.shader`): 10 voxel cumulus per map,
  50 to 85 m across in 5.5 m blocks, 100 to 150 m out and 40 to 64 m up, inside the 240 m far
  plane. Each is filled from two or three dome lobes with a flat belly, and only outer faces are
  drawn. The normals roll over the blocks, the crevices darken, the colour runs from a cream crown
  to a lavender belly, and the edges melt into the sky's own colour at their elevation. The ring
  drifts on the shared sky clock, and the build is seeded from the map name. The photo panorama
  stays behind as a faint far layer (`PaintedCloudOpacity` 0.18, painted mip, `CloudPaint`).
  Classic's sky is untouched. The shader is on `GameBuilder`'s always-included list.
  ⚠️ The first two cuts had their triangle winding reversed (left-handed Unity read as
  right-handed), so every cloud drew inside out. The owner caught it; the winding is fixed.
- [ ] LIGHT-3.6 Ambient occlusion (owner 2026-09-25: "can we try adding ambient occlusion").
  Screen-space, in `WorldOutline` passes 2 and 3 because the built-in pipeline has none and no
  post-processing package is installed. The pass first ran at half resolution with twelve
  cosine-weighted hemisphere samples round the depth-normals normal, 0.9 m radius, per-pixel
  noise rotation, a range check and a fade out by 60 m. A 3x3 depth-aware blur follows, and the
  composite leans the occluded part toward the violet `CavityHue`. `AmbientOcclusion` 0.8 on a steepened curve (the first cut at 0.6 moved the deepest corner 14 levels in 255). On the
  Bright style's own gate (not Classic, not the Low tier, perspective cameras only). Rendered
  with it off and on from the same cameras on all five maps, Mac, 1/1 each run, with the owner's
  profile untouched (`-tp-profile`). The first cut darkened the right places, per the heat map
  `Eskinita-stage-aomap.png`: feet, fences, house joins and props. It was too faint, so v2
  steepened it. Sheets are in
  [reports/light-3-2026-09-25/ambient-occlusion/](reports/light-3-2026-09-25/ambient-occlusion/).
  v3, after the owner's playtest ("im not noticing any ao in the concave intersections of faces
  like what minecraft does"): the kernel skims the surface at 8 to 40 degrees in four rings to 1 m,
  nearer hits weigh more, and a 90 degree inside corner maps to full occlusion (about a third
  darker on screen, toward violet). The cosine hemisphere had sent most samples straight out,
  where they never reached the neighbouring face. v4 and v5, after the owner saw a noise
  pattern in play. The rotation is now a 4x4 tile of sixteen angles, the blur averages exactly a 4x4
  window with a Gaussian depth weight (the old 1/(0.001 + difference) weight hardly blurred sloped
  surfaces), and the pass runs at full resolution.
  v6, after the owner's playtest: "the lighting suddenly changes when i look in different
  directions" (it went darker).
  - Measured on Ilalim: the lighting state never changed with view direction, but from 90 to 225
    degrees the AO darkened the whole frame (mean 107 against 131 with AO off at 180 degrees).
  - Cause: a bridge pillar beside the camera. It is dissolved by NearFade in the colour pass, but
    Unity's depth-normals prepass draws it solid with its internal shader, so it filled the texture
    as a wall at the lens (depth about 0, one flat normal, about 80 per cent of the frame).
    Isolated by switching renderer groups off one at a time; the LRT pillars alone.
  - Fix: the AO and the ground occlusion ignore any depth-normals pixel nearer than
    `NearFade.FadeStartMetres` (1.8 m).
  - The ink edges and ground contact read the same texture and may still show this near a
    dissolved prop. That is older than LIGHT-3 and not fixed here.
  Open for the owner's playtest.
- [ ] LIGHT-3.5 The owner's look at the final comparison. The rendering is done: `e26eeb04`,
  Mac, one PlayMode launch, total 3 failed 0 (`FiveMapStageCapturesPreserveGeometryAndRestore
  OriginalLighting`, `LightingStyleThumbnails` and a scratch same-camera review that was not
  committed). Sheets in [reports/light-3-2026-09-25/after/](reports/light-3-2026-09-25/after/):
  per map Classic, Bright before and Bright after for sky, wide, eye, stage and cast, plus
  all-map sky and cast sheets. The Bright card thumbnail is re-rendered from the same launch.
  Taste calls left to the owner: SaBubong's clouds run pink under its golden-hour palette, and
  character shading (cel against normal) is deferred at the owner's word.

### REFINE-2 · Map-by-map assets, natural life and actual play (queued after older work)

LATEST owner WIP rule: finish the currently active implementation and its actual
in-game review before starting another pass; log new requests without dropping old work.

LATEST owner priority,2026-09-24: MAP REFINEMENTS ARE NOT FINISHED. Complete the
remaining per-map storefront/sign/asset work, actual SaBubong/Lagoon edge recovery
and integrated map camera/gameplay qualification. Keep new REFINE-2.11ultimate
research/upgrade queued AFTER this map work. Do not let a new request erase or
prematurely close the map parents, or interpret earlier art batches as full acceptance.

Owner reference: [3D Asset](https://3d-asset.com/). Requirements, order and research
questions are preserved in [the intake](reports/map-by-map-refinement-2026-09-23/intake.md).
[Exact owner feedback](reports/map-by-map-refinement-2026-09-23/owner-request.md) and
[the research/execution plan](reports/map-by-map-refinement-2026-09-23/research-and-execution-plan.md)
are saved. Aesthetic appeal matters more than realism; research must use multiple
references. Work one map, character/movement or aspect at a time.
Owner reiterated: finish assigned UI first. During this later map pass, generate
additional visual references, critique them and adopt only useful ideas; iterate
references to solve concrete gaps and verify the improvement in the actual3D game.
Latest owner additions: research PEAK and other relevant games first; make every
map more distinctly Filipino, each showcasing different cultural/place aspects.
The real map is visible behind LOBBY: fill sparse surrounding context with a
coherent spatial plan, not random decoration. Detailed later research/camera scope:
[cultural and camera brief](reports/map-by-map-refinement-2026-09-23/cultural-and-camera-brief.md).
The owner says assets still feel bland, animals repeat obvious fixed routes, and
bots were sometimes AFK/standing. Treat these as new experience reports; earlier
passing checks do not prove these complaints resolved. Do not start another global
map shader sweep as a substitute for individually improving all five places.

Owner reiterated: extend meaningful surroundings beyond the arena on ALL maps,
and improve plain/bland houses themselves with appropriate texture/construction
detail and stronger Filipino place identity. Research first and use built-in image
generation for critically reviewed inspiration. Extra background copies are not
house-quality completion. Each map gets its own distance plan and individual
building/material decisions, preserving the one-map-at-a-time order.

Owner also explicitly requests recognizable Filipino street life, including
TRICYCLES, researched thoroughly first. Inspect the existing passenger/cargo
tricycle and jeepney assets, choose map-appropriate forms and purposeful pickup,
parking, service/household settings. Ground/support them and preserve routes.
Include other researched place-specific objects; no identical cultural-prop pack
scattered across all maps. Execute inside each map's existing refinement row.

- [ ] **REFINE-2.0 Older-work gate.** Finish/account for older actionable TODOs with
  actual implementation/evidence; preserve shared/concurrent and external dependencies.
- [ ] **REFINE-2.1 Thorough reference research and plan.** First research good-game
  qualities and visual/motion satisfaction, then translate them into TUMP criteria.
  [Initial foundation notes](reports/map-by-map-refinement-2026-09-23/foundation-research.md)
  and [comparative reference notes](reports/map-by-map-refinement-2026-09-23/comparative-reference-notes.md)
  have begun; this research is not complete. Brown City, official PEAK media and
  A Short Hike's gallery were visually inspected; initial Philippine place sources
  and current Eskinita authoring routes are recorded. Eskinita context is implemented;
  the fresh [house material study](reports/map-by-map-refinement-2026-09-23/house-material-study.md)
  adds primary construction research, generated inspiration and explicit critique.
  Inspect actual rendered
  assets, cross-reference place/material construction, inventory per-map assets,
  write concrete keep/refine/replace decisions and ordered implementation plans.
  Include comparative game research, a sourced Filipino cultural/place brief per
  map, and a context-density plan at actual lobby/introduction/play/spectator angles.
  Owner PEAK and sparse-lobby reference images are preserved in ArtSource.
- [ ] **REFINE-2.2 Eskinita.** Individual asset/material/shape/detail refinement.
  Owner2026-09-24 building request reviewed per [the all-map requirement](reports/map-by-map-refinement-2026-09-23/building-texture-brief-20260924.md).
  [Per-house surface disposition](reports/map-by-map-refinement-2026-09-23/eskinita-building-disposition.md) retains
  the individually refined plaster/timber/metal/glass families after actual bright
  views/source review. No new blanket texture pass or unchanged tests justified.
  First context group implemented:26retained-family outer houses and4connected
  streets, existing collision preserved. Matched actual-preview/small/grey25
  inspected; focused1/1passed after correcting new-group roof palette routing.
  [Evidence and remaining work](reports/map-by-map-refinement-2026-09-23/eskinita-context/report.md).
  Owner correctly rejected the remaining empty distance beyond that first row.
  A continuing96-block district now fills the exposed preview background using
  retained middle-distance geometry and simpler distant houses, one palette material
  and no new collision. Matched preview/small/grey25inspected; focused1/1passed.
  [Distance evidence](reports/map-by-map-refinement-2026-09-23/eskinita-distance/report.md).
  Primary home4_W now has individually fitted plaster/roof finishes, a measured
  supported door shade and a local correction for roof-tinted shrubs. Variant1
  rejected, variant2inspected at matched near cameras/small/grey; focused1/1pass.
  [House evidence](reports/map-by-map-refinement-2026-09-23/eskinita-house-b/report.md).
  Home5_W's horizontal timber no longer gets an incorrect vertical procedural grid;
  original boards/colors/windows preserved. Matched near/street/small/grey inspected,
  focused1/1passed. [Timber evidence](reports/map-by-map-refinement-2026-09-23/eskinita-timber5w/report.md).
  The same demonstrated cladding/material mismatch is now corrected individually
  at2_E/5_E/6_E, each inspected from a lit side and its shadowed street frontage;
  focused1/1passed. [East timber evidence](reports/map-by-map-refinement-2026-09-23/eskinita-east-timber/report.md).
  Later masonry/garden/final-art work below resolves the remaining primary-home
  roof/composition decisions; integrated camera/performance acceptance stays open.
  Home3_Wnow differs from0_W's shop: raised bamboo shade, basket and folded cloth
  on the retained sill. Neighbor shop/windows/steps preserved, no new collision.
  Matched near/street/small/grey inspected; focused1/1passed including the corrected
  persistence of reauthored timber materials.[Domestic evidence](reports/map-by-map-refinement-2026-09-23/eskinita-domestic3w/report.md).
  Bay2_Wnow has a researched, map-specific motorcycle/sidecar tricycle with improved
  glass, panels, mirrors, lamp and supported driver shade. Original vehicle sources/
  colliders preserved;3grounded tyres and old footprint checked. Placement variant2
  inspected in matched street/small/grey and four model views; focused1/1passed.
  [Vehicle evidence and limits](reports/map-by-map-refinement-2026-09-23/eskinita-tricycle/report.md).
  Five remaining primary masonry homes retain original plaster with fitted lower
  courses, roof detail and foliage fixes. Whole-wall draft rejected; clipped revision
  inspected under adopted bright lighting, focused1/1passed.[Masonry evidence](reports/map-by-map-refinement-2026-09-23/eskinita-masonry/report.md).
  Five overlapping tree crowns now open selected facade views; four timber-home
  planters have corrected green foliage. Lower trunks/roots/collision and other
  trees retained. Matched bright-look pairs/grey25 inspected, existing route1/1pass.
  [Garden evidence](reports/map-by-map-refinement-2026-09-23/eskinita-garden/report.md).
  Oversized mountain paintings now sit lower behind the district; original art
  retained. Matched game/grey25 and actual preview inspected, existing2/2passed.
  [Background evidence](reports/map-by-map-refinement-2026-09-23/eskinita-backdrop/report.md).
  Remaining shop/seating/court views inspected at High/Low; retained useful props,
  lettered two blank shop boards and replaced the outdated map-vote card with the
  actual bright preview. Sign pass1/1passed and paired/grey25inspected.
  [Final art-batch decisions and remaining integrated acceptance](reports/map-by-map-refinement-2026-09-23/eskinita-prop-review.md).
  Art implementation is ready for the integrated intro/spectator/native gate;
  this parent remains open until that acceptance. Continue Bayan art next.
  West Lita's Store now uses generated3:1art on a modest supported front-fascia
  plate, with original booth/body/east retained. Corrected native1/1passed3.959s;
  actual front/neighborhood/grey inspected. [West sign evidence](reports/map-by-map-refinement-2026-09-23/eskinita-west-sign/report.md).
  East Aling Luz now has its own generated3:1cream/charcoal/red fascia; same
  supported fitting, existing booth/west retained. Native1/1passed3.997s, actual
  front/neighborhood/grey inspected. [East sign evidence](reports/map-by-map-refinement-2026-09-23/eskinita-east-sign/report.md).
  Both reopened sign units implemented/reviewed; integrated map gates stay OPEN.
  The original correction for both boards is preserved here: per-sign reference, built-in generated artwork, critique and physical
  fitting are now implemented in the two named reports above. Prior stock-font
  signs are preserved baselines, not current final art.
- [ ] **REFINE-2.3 Bayan Plaza.** Individual asset/material/shape/detail refinement.
  Owner2026-09-24 building review found the hall body still uniform. Eight large
  wall triangles now have fitted plaster wash, preserving arches/slats/roof/physics.
  [Hall and other building disposition](reports/map-by-map-refinement-2026-09-23/bayan-hall/report.md):
  native1/1passed, actual paired front/rear/grey inspected; church stone and existing
  fitted timber/paint families retained. Integrated camera/native gate stays open.
  Next active map. [Primary place references and generated-study critique](reports/map-by-map-refinement-2026-09-23/bayan-reference-notes.md)
  saved. Eight mint hedge bars now have locally authored low foliage in the retained
  planter shells; variant3selected after paired/overview/grey25inspection,1/1passed.
  [Planting evidence](reports/map-by-map-refinement-2026-09-23/bayan-garden/report.md).
  Abrupt town/ground edge replaced by32connected blocks,38retained-family homes,
  68far homes and22planted plots. Original collision/landmarks retained; actual
  preview/overview/grey25inspected, focused1/1passed.
  [Context evidence](reports/map-by-map-refinement-2026-09-23/bayan-context/report.md).
  Four monument-edge pots now have supported shrubs/contained soil, original concrete
  retained. Paired/grey25/native preview inspected; focused2/2passed. Civic fronts/rear
  and south High/Low reviewed; sky20/180motion samples inspected and retained.
  [Pot and near-art findings](reports/map-by-map-refinement-2026-09-23/bayan-potted-shrubs/report.md).
  Sixteen e/o houses now have their existing fitted timber/jalousie/terrace finishes;
  original bodies/collision retained. Paired front/back/grey25and final preview
  inspected, focused2/2passed. Native map-vote card refreshed.
  [House finish evidence and art-batch disposition](reports/map-by-map-refinement-2026-09-23/bayan-house-finish/report.md).
  Art batch ready for integrated intro/spectator/native acceptance; keep parent open
  for that gate and continue Ilalim art next.
- [ ] **REFINE-2.4 Ilalim ng Tulay.** Individual asset/material/shape/detail refinement.
  Vulcanizing now uses a real painted3Dtire sign and supported tire-service bay.
  Native1/1passed5.024s, actual front/street/grey inspected. [Vulcanizing evidence](reports/map-by-map-refinement-2026-09-23/ilalim-vulcanizing/report.md).
  Current map-vote card now shows the actual street/court beneath the bridge; two
  native framing variants inspected, axial retained. Eskinita card refreshed too,
  original importers/GUIDs verified unchanged. [Card evidence](reports/map-by-map-refinement-2026-09-23/map-cards-after-storefronts/report.md).
  All11Ilalim storefront units now have individual implementation/retention evidence;
  this parent stays OPEN for integrated map/camera/native-performance acceptance.
  Pisonet retains its source cabinets/chairs with fitted controls, quiet screen
  panes and lower glazing; original3:1commercial sign. Native1/1passed4.549s,
  actual paired/grey inspected. [Pisonet evidence](reports/map-by-map-refinement-2026-09-23/ilalim-pisonet/report.md).
  Pares counter implemented with actual pot/bowl/condiment forms. Owner-rejected
  painterlyv1replaced after LugawKing study: wood fascia/simple graphic bowl,3:1.
  Native1/1passed4.571s, actual paired/grey inspected. [Pares evidence and rejection](reports/map-by-map-refinement-2026-09-23/ilalim-pares/report.md).
  Tony's barber unit implemented:3:1original fascia, retained chairs face mirrors,
  fitted supports/worktops and lower glazing. Native1/1passed4.634s; actual paired/
  grey inspected. [Barber evidence](reports/map-by-map-refinement-2026-09-23/ilalim-barber/report.md).
  Owner-selected ALING PASING replaces rejected generic TINDAHAN: original3:1
  fascia, separate load placard, green grille/sachets and supported navy/white
  awning. Native1/1passed4.565s, front/street/grey inspected.
  [Aling Pasing evidence](reports/map-by-map-refinement-2026-09-23/ilalim-aling-pasing/report.md).
  Owner-referenced CIAO GRAZEY BOUTIQUE implemented: red/white3:1fascia, paired
  shoe shelves and hanging garments. Existing street rack moved beside the shop
  to reveal its window; original model/colliders retained. Native+both-mode routes
  2/2passed, actual paired/grey inspected. [Ciao Grazey evidence](reports/map-by-map-refinement-2026-09-23/ilalim-ciao-grazey/report.md).
  Other signs and integrated map gates remain OPEN.
  Supplied PCExpress source retained byte-for-byte; local import and physical board
  corrected from4:1to original3.783:1. Native1/1passed5.207s, paired/grey inspected.
  [Protected-art fitting](reports/map-by-map-refinement-2026-09-23/ilalim-pcexpress/report.md).
  Computer repair now has a supported open-laptop/case workbench and original
 3:1service sign. Native1/1passed4.587s, actual front/street/grey inspected.
  [Repair evidence](reports/map-by-map-refinement-2026-09-23/ilalim-repair/report.md).
  Laundry unit implemented: original LABADA art at3:1, retained machines revealed
  through lower glazing, layered folded stock. Native1/1passed4.545s and matched
  front/street/grey inspected. [Laundry evidence](reports/map-by-map-refinement-2026-09-23/ilalim-laundry/report.md).
  Bakery unit now implemented: owner-liked vintagev2fascia fitted3:1, stepped glazed
  bread showcase and grouped loaves replacing generic cubes. Native1/1passed5.754s;
  paired front/street/grey inspected. [Bakery evidence](reports/map-by-map-refinement-2026-09-23/ilalim-bakery/report.md).
  Other signs/storefronts and integrated map qualification remain OPEN.
  Owner explicitly REJECTED print/copy storefront and generic world-sign lettering.
  [Storefront rework](reports/map-by-map-refinement-2026-09-23/ilalim-storefront-rework-plan.md)
  is OPEN and current priority: research actual Philippine shops, distinct functional
  interiors/fronts, cartoon construction and authored sign artwork. Earlier shop
  retain judgment is superseded; structure and skyline decisions remain valid.
  PRINT prototype now has generatedv3art at verified3:1, readable copier/laminator/
  paper supplies, side grille, supported awning and painted panel. [Native evidence](reports/map-by-map-refinement-2026-09-23/ilalim-print-shop/report.md):
  complete-frontage1/1passed, front/street/grey inspected. V1owner rejection and
  v2artifact output preserved as rejected only. [Each remaining sign](reports/map-by-map-refinement-2026-09-23/ilalim-sign-register.md)
  stays OPEN, bakery next; this is not whole-map or owner aesthetic approval.
  Next active map. [Primary reference intake and Gilmore/LRT2identity](reports/map-by-map-refinement-2026-09-23/ilalim-reference-notes.md)
  saved. First local finish replaces mint concrete/pink-yellow track lookup colors
  with neutral structural materials on24pillars/28bays/56tracks. Original geometry,
  collision, palettes and train livery retained. Paired preview/eye/under-deck/grey25
  inspected, focused1/1passed. [Evidence](reports/map-by-map-refinement-2026-09-23/ilalim-structure/report.md).
  The34existing low-detail skyline bodies now have distinct fitted facade treatments:
  514window groups/144glass divisions in3overlay renderers. Shapes/positions/collision
  preserved. Paired preview/street/district/grey25inspected, focused1/1passed.
  [Skyline evidence](reports/map-by-map-refinement-2026-09-23/ilalim-skyline/report.md).
  Outer visual ground edge corrected without new collision; near shop/service
  pockets, Low street and sky20/180reviewed and retained. Native card refreshed.
  [Final art-batch disposition](reports/map-by-map-refinement-2026-09-23/ilalim-final-art/report.md),
  focused1/1passed. Parent remains open for integrated intro/spectator/native gate;
  continue SaBubong art next.
- [ ] **REFINE-2.5 Sa Bubong.** Individual asset/material/shape/detail refinement.
  Owner2026-09-24: building texture/detail acceptance remains OPEN; inspect and refine
  each family under bright lighting per [the all-map requirement](reports/map-by-map-refinement-2026-09-23/building-texture-brief-20260924.md).
  Active map. [Primary reference and concept critique](reports/map-by-map-refinement-2026-09-23/sabubong-reference-notes.md)
  saved. Existing water tank now has fitted lid/bands/service outlet, original body/
  collision retained. Paired/grey25inspected, focused1/1passed.
  [Tank evidence](reports/map-by-map-refinement-2026-09-23/sabubong-tank/report.md).
  Stairhead now has fitted door/vent/entry housing/flashing detail; original body/
  noticeboard/mural/collision retained. Paired/grey25inspected, focused1/1passed.
  [Stairhead evidence](reports/map-by-map-refinement-2026-09-23/sabubong-stairhead/report.md).
  Shade now has supported beams/rafters and restrained panel seams; original
  furniture/posts/plants/collision retained. Paired/grey25inspected, focused1/1passed.
  [Shade evidence](reports/map-by-map-refinement-2026-09-23/sabubong-shade/report.md).
  Six individually used neighboring roofs now occupy verified vacant plots, with
  grouped windows/access and per-part material roles. Paired/grey/Lowinspected,1/1pass.
  [Neighbor roof evidence](reports/map-by-map-refinement-2026-09-23/sabubong-neighbors/report.md).
  Outer72-block district/284homes/four planted courts and connected streets now
  continue the city; visual ground edge removed. Existing native case1/1passed and
  paired/grey/roof views inspected. [District evidence](reports/map-by-map-refinement-2026-09-23/sabubong-district/report.md).
  Six neighboring apartments now have local metre-scaled plaster textures, concrete
  corners, sills/jambs and floor reveals. Representative native pairs/grey inspected;
  [surface evidence](reports/map-by-map-refinement-2026-09-23/sabubong-apartment-surfaces/report.md),1/1passed after one bounded provenance-fixture fix.
  Retained straight B towers now have fitted secondary glazing divisions while
  source glass/frame materials stay intact. [Family evidence](reports/map-by-map-refinement-2026-09-23/sabubong-tower-b/report.md), native1/1passed/pairs/grey inspected.
  Main condo now has a local painted-mineral wall finish, fitted end windows and
  supported exterior rainwater runs; original pool/roof/collision retained. Native
  pairs/grey/four directions/Low/card inspected,1/1then1/1after a local sky-sampling
  correction. Other-family keep/refine decisions and timed-sky evidence limits are
  [recorded here](reports/map-by-map-refinement-2026-09-23/sabubong-final-art/report.md).
  Native map card refreshed. Current art batch ready for integrated intro/spectator/
  gameplay/performance acceptance; parent remains open. Continue Lagoon next.
- [ ] **REFINE-2.6 Lagoon.** Individual homes/piles/boats/water/context refinement.
  Owner2026-09-24: building texture/detail acceptance remains OPEN; inspect and refine
  each family under bright lighting per [the all-map requirement](reports/map-by-map-refinement-2026-09-23/building-texture-brief-20260924.md).
  Active map: current18-home/six-watercraft layout retained. [Fresh primary source
  intake](reports/map-by-map-refinement-2026-09-23/lagoon-reference-notes.md) includes
  actually viewed Embassy stilt-house and NCCA boat photos. Native family/material
  assessment precedes local edits; preserve court/deck/water recovery and collision.
  Four gabled-thatch roofs now have slope-aligned fibre/bundle texture, modest
  eave fringe and ridge ties; source geometry/other material slots/collision retained.
  [Family evidence](reports/map-by-map-refinement-2026-09-23/lagoon-gable/report.md): native
  paired preview/front/roof/detached views and grey inspected, focused1/1passed.
  Three hip roofs now have slope-specific palm fibres and restrained four-eave/
  short-ridge detail, with original body/other materials/collision retained.
  [Hip evidence](reports/map-by-map-refinement-2026-09-23/lagoon-hip/report.md), native1/1passed
  and representative paired/grey views inspected. Five low palm roofs and four
  woven screens now have fitted materials/fibre axes; source weave/geometry/collision
  retained. [Veranda evidence](reports/map-by-map-refinement-2026-09-23/lagoon-veranda/report.md),
  native1/1passed and paired/grey inspected. Six metal roofs now have fitted rib
  direction/pitch and home2alone has a painted lower-board accent; geometry/other
  palettes/collision retained. [Metal-family evidence](reports/map-by-map-refinement-2026-09-23/lagoon-metal/report.md),
  native1/1passed/paired/grey inspected. Water now reveals submerged supports/hulls,
  with restrained moving surface/bed detail and a visual-only bed extension. Fixed
  runtime replacement of LagoonBed via its preserve tag; physical surfaces unchanged.
  [Water evidence](reports/map-by-map-refinement-2026-09-23/lagoon-water/report.md): final1/1passed,
  actual paired/grey/Low/held-time views inspected. Six boats now have restrained
  curved-hull grain and the two houseboats have fitted shelter fibres, with original
  shape, non-target parts, mooring and collision retained. [Boat evidence](reports/map-by-map-refinement-2026-09-23/lagoon-boats/report.md):
  native1/1passed, three paired views and25percent grey inspected. Eleven near islands
  now have fitted land/shore surfaces and distinct grounded woodland/scrub/mangrove/
  stone groups; nine far silhouettes retained. [Coast and building disposition](reports/map-by-map-refinement-2026-09-23/lagoon-coast/report.md):
  final native1/1passed, actual pairs/grey/card/Low/heldsky inspected, updated native
  map card. Current art batch ready for integrated intro/spectator/gameplay/native
  performance gate; parent remains open for that final acceptance.
- [x] **REFINE-2.6b Lagoon actual swim-to-bridge recovery, owner2026-09-24.**
  Latest correction implemented: falling enters swimming; Jump beside a reachable
  public bridge/deck starts press-gated physical climb onto that same edge. Normal
  centre respawn/prone get-up removed; stairs and8second stock delay preserved.
  [Native/state/rig evidence and remaining peer gate](reports/map-by-map-refinement-2026-09-23/edge-recovery/report.md):
  final3/3plus focused clock/reset1/1passed, actual world/owner/reduced/grey inspected.
  Protocol51has explicit host-owned edge state and reliable ownership handover.
  Prior3e0d0c69a/2passed respawn-get-up proof is historical and superseded, preserved
  in lagoon-fall-recovery/report.md. Fresh real-peer/native gate stays in2.10/P7.
- [x] **REFINE-2.6c Lagoon flying birds, owner2026-09-24.** Check existing ambient
  birds first; add coastal flying life if absent, with varied glide/flap/transit
  choices around village/islands. Preserve readable action and avoid repetitive
  fixed circles. Coordinate with REFINE-2.7natural ambient movement.
  Added three existing native birds with private curved goals, banked turns and
  wingbeat/glide variation. Local bird check1/1passed; revised world scale/poses/grey
  inspected. [Evidence](reports/map-by-map-refinement-2026-09-23/lagoon-birds/report.md).
  Full-map visibility/performance remains in final Lagoon integration; broader
  all-map animal behavior stays open.
- [x] **REFINE-2.6d SaBubong actual railing recovery, owner2026-09-24.**
  Real rail catch/effort/pull-over and supported same-edge landing implemented;
  normal centre respawn removed. Current cast palm fitting, owner camera, reduced
  first-person view, no passive climb, held-vs-tapped input, reset and stock checks
  are covered in [edge evidence](reports/map-by-map-refinement-2026-09-23/edge-recovery/report.md).
  Fresh transport/rejoin/full-match/native qualification remains in2.10/P7.
- [ ] **REFINE-2.6a Skies, islands and backgrounds.** After UI, inspect and improve
  animated skies, island/mountain layers and other distant/background scenery on
  every map. Evaluate composition, silhouettes, depth, materials, motion and harmony
  with that map. Existing animation is not automatic completion or a reason to skip
  refinement. Carry out changes within each individual map pass, keeping good parts.
- [ ] **REFINE-2.7 Natural ambient movement.** Diagnose repeated paths and author
  plausible habitat/goal/activity choices, motion and reactions per map.
  Eskinita tan aspin now chooses connected investigate/watch/rare marking sites,
  eases movement and retreats from intrusion without panicking at a stationary
  observer. Focused native 1/1 passed; actual frames/grey inspected.
  [Research/plan](reports/map-by-map-refinement-2026-09-23/ambient-life-plan.md),
  [implementation and evidence limits](reports/map-by-map-refinement-2026-09-23/eskinita-dog/report.md).
  Eskinita tabby also implemented independently: east-side sites, quieter tail,
  shorter investigations/longer watch and its own pace. Native v2 1/1 passed after
  one inward-side fixture correction; actual frames/grey inspected.
  [Cat evidence](reports/map-by-map-refinement-2026-09-23/eskinita-cat/report.md).
  Bayan cream aspin implemented independently: three ordinary plaza activities
  plus rare marking, its own walk pace/watch holds. Native 1/1 passed, actual
  frames/grey inspected. [Evidence](reports/map-by-map-refinement-2026-09-23/bayan-dog/report.md).
  Bayan ginger cat implemented independently: paving investigation/two watch
  sites, own relaxed pace/watch durations. Native 1/1 passed without retry; actual
  frames/grey inspected. [Evidence](reports/map-by-map-refinement-2026-09-23/bayan-cat/report.md).
  Ilalim patched aspin implemented: three actual storefront/pavement activities
  plus its original marking point. Native 1/1 passed without retry, actual frames
  and grey inspected. [Evidence](reports/map-by-map-refinement-2026-09-23/ilalim-dog/report.md).
  Ilalim tuxedo cat implemented: three storefront/pavement sites, original curled
  tail and its own pace/holds. Native 1/1 passed without retry, actual frames/grey
  inspected. [Evidence](reports/map-by-map-refinement-2026-09-23/ilalim-cat/report.md).
  All six ground animals individually authored/reviewed. Eskinita bird visits now
  use supported alternatives, shaped landing/long departure and distinct forage/
  fantail lookout. Native 1/1 passed; actual frames/grey inspected, staged-close
  dither/wing-blend limitation retained for final camera review.
  [Bird evidence](reports/map-by-map-refinement-2026-09-23/eskinita-bird-visits/report.md).
  Bayan visits fitted separately to broader civic paving/longer pauses; northern
  fantail spots moved clear of church steps. Native v2 1/1 passed after one clear-
  arrival staging correction; actual sequences/grey inspected.
  [Bayan bird evidence](reports/map-by-map-refinement-2026-09-23/bayan-bird-visits/report.md).
  Ilalim visits fitted beneath the real guideway with street-axis flight/air
  corridor and supported sites. Native 1/1 passed, sampled ceiling/column-gap
  clearance and actual sequences/grey reviewed.
  [Ilalim bird evidence](reports/map-by-map-refinement-2026-09-23/ilalim-bird-visits/report.md).
  SaBubong visits now use roof-deck pads and actual raised canopy seams; native
  1/1 passed and before/new sequences/grey inspected.
  [Roof bird evidence](reports/map-by-map-refinement-2026-09-23/sabubong-bird-visits/report.md).
  All21 current ambient actors have map/species placement coverage. Ordinary-camera,
  replay and combined qualification remain open; Lagoon flight retained.
  Replay integration follow-up: present-time animals no longer leak into retained
  or victim-catch cameras; existing render flags restore after each draw. Native
  baseline reproduced both leaks, final2/2 passed with actual paired/grey frames.
  A can-model replacement stale-reference error exposed by the retained test is
  also fixed, without changing its authored motion or authority.
  [Replay isolation evidence](reports/map-by-map-refinement-2026-09-23/ambient-replay/report.md).
- [ ] **REFINE-2.8 All-bot behaviour.** Observe both modes/roles/maps/roster/choices,
  trace idle decisions and fix actual stalls; distinguish deliberate tactical waits.
  Initial Eskinita samples cover four bots in both modes. A reaction-clock defect
  was reproduced and corrected: intermittent planner calls now use elapsed game
  time instead of accumulating single-frame deltas. Focused native3/3 passed,
  including repeat-query/reset/pause and the two ordinary-speed samples.
  [Research/plan](reports/map-by-map-refinement-2026-09-23/bot-refinement-plan.md),
  [causal evidence and remaining coverage](reports/map-by-map-refinement-2026-09-23/bot-reaction-fix/report.md).
  Other maps/roles/transitions/tier/roster coverage remains open. Existing tag-stun
  waits are intentional, not removed to manufacture activity.
  The chase-patience clock had the same planner/frame mismatch and now measures
  time since real closing progress. Focused chase plus Ilalim samples3/3 passed;
  Lagoon samples2/2 passed. [Map findings and limits](reports/map-by-map-refinement-2026-09-23/bot-map-coverage/report.md).
  No Lagoon water falls occurred in the sample; explicit bot recovery remains open.
  Bayan/SaBubong samples also passed: initial four-bot, two-mode traces now cover
  all five maps. Per-seat pektus/lunge decisions now use the assigned room tier,
  consistently with the existing personality lookup; rule checks/compile3/3.
  These samples do not close full role/roster/tier qualification. The added bot
  recovery setup failed twice before control handoff; its draft/failures/next
  input-basis diagnostic are preserved in the map report, with no more fixture
  retries during implementation. Bot recovery remains an actionable final gate.
Owner assignment update2026-09-24: the owner prepared a separate cloud assignment
for ALL animation research/direction/implementation, including individual ability
casts and ultimates. That animation lane can begin now while local map/environment
and bot work continues. The prompt/setup were delivered for the owner to paste;
no other conversation was created or contacted, and no task is marked done by
this handoff. Preserve the newest merged animation work and remaining per-body checks.

- [ ] **REFINE-2.9 Gameplay and animation.** Review actual complete exchanges,
  movement/collision, interaction and interruption, body/FPP/contact/observer timing.
  Owner specifically reopens walking (Sean arms stick to body; similar across cast),
  throwing and left/right pektus. Earlier tests are not final visual acceptance.
  - [ ] REFINE-2.9a motion reference research and every-character/state coverage list.
  - [ ] REFINE-2.9b Sean walking/arm clearance, weight and carrying blends first.
  - [x] REFINE-2.9c each other character walking, inspected and fitted individually. Done 2026-09-24:
    all 19 roster bodies filmed sprinting and walking side-on and front-on (`CastAndMotionReel`,
    `Logs/reel/all2`); every one swings clear of its torso against its legs, including the wide and
    costumed bodies (Dante, Cheska's pack, Nemu's sleeves); no body needed its own fitting.
  - [ ] REFINE-2.9d run/strafe/start/stop/turn and locomotion transitions separately.
  - [ ] REFINE-2.9e ordinary throw anticipation/release/follow-through/recovery.
  - [ ] REFINE-2.9f left pektus and right pektus, distinct real release/flight intent.
  - [ ] REFINE-2.9g cancel/interruption/pickup and remaining verbs/hero performances.
  - **Progress 2026-09-24** (owner: *"my biggest issue is where the hands are and what they do when u
    run"*, *"refine ... the raising of can or throwing of slippers or pektus ... both FPP and TPP"*).
    Research and per-action analysis: [gameplay-animation-2026-09-24](reports/gameplay-animation-2026-09-24/research-and-analysis.md).
    - 2.9b/d, arms while moving: the shared rig's `walk`/`sprint` held the arms 10 degrees off vertical,
      pressed to the torso, swinging +/-12 and +/-24; carrying froze the whole upper body in
      `holding-right` (slipper held straight out, off hand fixed). `CharacterAnimator.LocomotionArms.cs`
      now poses the free arms clear of the body and against the opposite leg (sprint -28 to +65, walk
      -21 to +30), and carries the slipper low at the side with a small swing. `ViewmodelArms.RunSway.cs`
      gives the first-person hands a footfall bob and the empty hand a pump. `LocomotionArmsProbe` 1/1,
      pictures in `Logs/locomotion-arms/`. Found on the way: glTF import mirrors X and hero bodies carry
      a hidden second rig, so limbs are resolved from the visible skin and their axes from the bind pose.
      Sean seen side-on; 2.9c (every other body individually) is NOT yet inspected.
    - 2.9e/f, throw and pektus: filmed before (`GameplayActionShots`), all three were the same few degrees
      of shoulder. `CharacterAnimator.ThrowBody.cs`: chest turned away, arm back and out, free arm pointing
      at the target; release across the body; pektus is sidearm, wrist-rolled, and finishes open (right)
      or closed (left). `ViewmodelArms.ThrowReach.cs`: the slipper hand draws back and close, the off hand
      rises into view, pektus drops and rolls, and the release finish follows the spin. Taya's front view
      and first person inspected; head-volume clearance of the new overhand pose is NOT re-measured yet.
    - Can raise: was the `pick-up` one-shot re-fired every 0.4 s beside a flat can. Now a bow to the can,
      hands rising with `ChannelRatio` (estimated from the relayed reaches on other screens, no wire
      change), the can's mesh tilting up under them (presentation only), and both first-person hands on it.
    - The tag (owner: *"make tagging better too"*): the dash tag played a 104-degree KICK and the jab an
      overhead CHOP. `CharacterAnimator.TagBody.cs` makes both a reach to touch (a jab snapped out and home;
      a dive with the arm held long while the sweep can land); `ViewmodelArms.TagReach.cs` moves the
      first-person hand toward the crosshair.
    - Every hero cast (owner: *"and the animation of all skill casting"*): 11 of 21 filmed as a few degrees
      and back at rest in half a second. The SHIPPING casts are the glb tables in `tools/author_hero_action.py`
      (`HeroAbilityClips` is only an editor fallback), so a readability pass there scales those eleven about
      rest and holds their contact pose; baked by `polish_hero_actions.py`, all 18 verified by
      `verify_hero_action.py`. Rafi's three are re-keyed in `HeroAbilityClips.Rafi.cs` and rebaked by
      `RosterBookBuilder`. Phaister's Hex no longer folds her brim into a slab. First person:
      `ViewmodelArms.CastGesture.cs` gives each cast a hand gesture through the screen (thrust, raise, slam,
      sweep, pull, spread, glide), keyed by the first-person action each ability sends.
- [ ] **REFINE-2.11 Individual ultimate performances, owner2026-09-24.**
  Thoroughly research/compare VFX and ultimate animations across several relevant
  Roblox and other games FIRST, then plan each hero separately, THEN implement.
  Each hero gets their own expressive animation/effect/sound theme and temporary
  stage treatment, with readable body/FPP/other-player/spectator views and truthful
  live-ability handoff. Phaister laughing and becoming airborne is an explicit
  direction to investigate. [Full requirements and research/plan gates](reports/map-by-map-refinement-2026-09-23/ultimate-performance-research-plan.md).
  Existing shared2.8second pause/introduction is implemented; the deeper new pass
  is OPEN. Owner allows longer durations: research and choose pacing,2.8is NOTa cap.
  Explicit sequencing: finish current recovery work FIRST; log this for LATER.
  No ultimate research/implementation detour during the current feature. Preserve accepted costs/warnings/cohorts and reduced-setting fairness.
  No older task deleted; this pass must precede final integrated qualification.
  - **Progress 2026-09-24 (cloud session):** research, per-hero plan and durations saved in
    [ultimate-performances-2026-09-24](reports/ultimate-performances-2026-09-24/plan.md). Per-hero
    lengths (2.8 to 4.2 s, cohort takes the longest, protocol 52), authored shots, stage walls,
    lift and voice timing built for all seven heroes. Owner-pushed4f62fcc5c is under
    local review. Runtime grounding assertions, black stage-wall normals and Nemu's
    held-slipper/head intersection are fixed. Native seven-hero study1/1 and shared
    phase6/6 passed; actual sequence sheets and grey comparisons inspected.
    [Local findings and remaining critique](reports/cloud-integration-2026-09-24/findings.md).
    Per-hero artistic refinement, real-peer timing and final acceptance remain open.
    Sean's existing parol frame was weak against his orange chest in the actual
    close shot. Its local material value, size and depth were refined without
    changing his gesture or stage timing; same-camera color/grey inspected,
    native introduction study1/1. [Sean evidence](reports/cloud-integration-2026-09-24/sean-parol/report.md).
- [ ] **VOICE-1 Hero voice lines, owner 2026-09-24. OPEN: HUMAN RECORDINGS ONLY, NONE RECORDED YET.**
  **Owner decision, 2026-09-24 (after hearing a Kokoro/Chatterbox synthetic audition):** *"remove
  voices u made with ai lets js do humans"*. Every generated clip (the stylised babble and the
  synthetic speech) and both generators were deleted; `HeroVoice.Play` now skips a line that has no
  recording (no sound, no caption, no held room). Done looks like: the team records `docs/HUMAN.md`
  Table E into `Resources/HeroVo/hvo_<id>.wav`, then the listening pass below. The history below is
  kept as written.
  Owner: *"can u give them all their own voice lines too or with actual audio and connect it to
  their story and personality"*, *"voicelines wherein they interact with each other and voicelines
  related to skills ... use valorant as reference"*. Research and decisions:
  `docs/reports/voice-lines-2026-09-24/research.md`. Built: the script (`Core/HeroLines.cs`, 171
  lines: two skills, the ultimate's ally and opponent readings, round start, tag, tagged, knockdown,
  lead, win, and twelve biography-grounded exchanges), `HeroLinesTests` (7 cases, 637/637 green), a
  per-hero stylised babble voice (`tools/generate_hero_voice.py`; a recording replaces a clip by file
  name, and the generator never overwrites one), `Audio/HeroVoice` (per peer, no wire change,
  announcer first, one line at a time, skill voice rests 14 s, customs do not speak as heroes) and an
  optional `HeroLineCaptions` setting (off by default: VISION section 3). `docs/HUMAN.md` Table E is
  the recording list. **Owner decision owed:** whether to record the lines (Table E) or accept a
  synthetic speaking voice; a Piper TTS voice was available and deliberately not used, because
  HUMAN.md treats voice casting as scored team work. **Owed on Windows:** a four-hero Hero Strike
  match listened to from a thrower and from the taya (the two ultimate readings), the round-one
  exchange, the announcer hand-off, and the caption row at 4:3 and on a phone.
- [ ] **NATIVE-CHECK-1 Animation lane native pass, 2026-09-24. IN PROGRESS.** First Windows run of the
  cloud animation branch. Found and fixed: `VfxShapes.TwoSided` and the Grand Coven curtain lit BLACK
  (zero normals; every upright effect and most ultimate introductions); the introduction grounding
  asserted "Key index out of range" on every match warm-up and left roots NaN (25 PlayMode fixtures);
  Sean's and Zack's skill-1 wakes were invisible from eye height; Nemu's introduction filmed 0.35 s of
  solid black (camera outside an 8 m ink wall, now 16 m) and cropped at 4:3 by 0.003. Gate before
  the fixes: 520 cases, 451 passed, 60 failed (`tools/playmode_suite.py --gate`). **Still open:**
  (1) The original Nemu held-slipper/head intersection stopped the seven-hero study before
  Dante and Rafi. A dedicated held table now keeps the shoe at her hip; the local native
  seven-hero study passed1/1 with zero intersections, and all seven sequences were inspected.
  The later Nemu reveal and widened opening shot passed the focused 4:3 framing case.
  [Local repair evidence](reports/cloud-integration-2026-09-24/findings.md).
  (2) Re-run the gate on the final candidate, then run the remaining red
  fixtures on clean `origin/ASTRAReworks` to split pre-existing from new (CarryTests, MatchRunTests,
  TumpNativeResultTests rematch, StunFrostTests, ThrowAimIntegrationProbe, TutorialDefenderProbe,
  HubSceneVideoTests, QueueCardLayoutProbe, TumpNativeFrontEndTests, NemuKitContractProbe possession).
  (3) EditMode, pre-existing on ASTRAReworks (baseline run 2026-09-24):
  `ThrowMotionTests` first-person grip at0.08m from lens is corrected at the actual
  fingertip; three focused EditMode cases3/3 and native owner/body throw views1/1
  passed. `ThrowEquipmentClearanceTests` still fails for190/190 synthetic pairs,
  mostly35% right pektus. Real-input Bayan runs at75% and100% right spin, including
  the largest alpombra, passed with0vertices in the head across charged samples.
  Preserve the healthy body motion; reconcile the fixture representation at P7.
  [Evidence](reports/cloud-integration-2026-09-24/throw-clearance/report.md).
  (4) The `CastAndMotionReel` review of all casts and first-person
  paths, `Checks.RunAll`, SKILL-TREE-1's pad and touch look, and the Windows and Android builds
  (protocol 52) were not reached.
- [ ] **SKILL-FX-1 Every skill's VFX, SFX and cast animation, owner 2026-09-24. IN PROGRESS.** Done 2026-09-24 (evidence in `docs/reports/skill-performances-2026-09-24/progress.md`): research and the per-skill plan; Grand Coven's cast, first-person path and circle; the cast-sheet audit and eleven body clips re-authored under an enforced head-pitch bound; one first-person hand path per cast (21); code-driven verbs checked. Still open: the other twenty skills' world VFX and cast SFX, which need `AbilityShowcaseProbe` stills on a Unity machine, one skill at a time against plan section 3.
  Local2026-09-25: Flame Rush's pushed trail was retained after actual eye/corridor/
  grey review. Ignition Cannon's generic slipper impact now has its own small
  five-point fire read; v61 selected from staged native comparisons with ordinary
  Slipper and Supernova. [Impact evidence](reports/cloud-integration-2026-09-24/skill-fx/ignition-impact.md).
  Both skills still need live cast/audio/peer qualification; the other skills
  remain open. Do not infer completion from staged geometry captures.
  Owner: *"refine all their skills VFX SFX and animation and everything too. THOROUGHLY research how
  other games that are good in roblox and or actual games like valorant or overwatch make skill
  effects and try to author one that works in our world"*, and *"REFINE ANY OTHER ANIMATION THAT CAN
  BE REFINED/STILL SUCKS"*. Research first (extend `reports/visual-research-2026-09-23/findings.md`
  with Valorant, Overwatch and Roblox skill-effect practice), then audit all 21 skills one at a time:
  cast body (`tools/author_hero_action.py` glb tables, Rafi's `HeroAbilityClips.Rafi.cs`), first
  person (`ViewmodelArms.CastGesture.cs`), world VFX (`HeroHazards`, `AbilityVfx`) and cast/payload
  SFX (`tools/generate_ability_audio.py`, `build_ability_audio.py`; sourced SFX rules in CLAUDE.md
  section 6). Fix weak ones individually; no shared template.
- [ ] **SKILL-TREE-1 Per-hero skill tree, owner 2026-09-24. IMPLEMENTED, LOCAL NATIVE ROUTE PASSED.** *"create
  the ui and code for skill tree, ur supposed to unlock the other skills as u play the character more
  but for now keep it all unlocked and make it easy to lock again (keeping it all unlocked for
  testing)"*. The tree screen already existed on ASTRAReworks (`UI/Hub/HubSkillTree.cs`, from HOME
  and the hero screen: branches per slot, cast-challenge progress, EQUIP), so it was not rebuilt.
  Added: **`HeroLoadoutRules.LockSkillTree = false`, the one switch** (set `true` to lock again);
  `ChallengesEnforced` follows it; `IsUnlocked(counters, variant, enforced)` overload so the locked
  path stays asserted (HeroLoadoutTests 14/14); the tree shows UNLOCKED on available alternates
  and still shows each challenge and count. Its mastery header now omits the internal "open for
  testing" explanation; the locked path retains its earning instruction. The actual HOME-to-tree
  and BACK PlayMode route passed 1/1 both before and after the copy fix at five viewport shapes,
  with inspected screenshots and 25 percent greyscale thumbnails.
  [Native report](reports/cloud-integration-2026-09-24/skill-tree/report.md).
  Still owed: `LoadoutSurfaceProbe`, `TumpNativePickerTests`, physical pad and touch review,
  real progression/account integration. These gates do not reopen the passed local screen route.
- [ ] **REFINE-2.10 Integrated qualification.** One coherent candidate with specific
  evidence/limits; no blanket completion from screenshots or object-spawn tests.
  Include protocol51edge-climb real peers, dropped/late pose packets, reconnect/seat
  handover, physical input and actual replay playback; local focused tests are not
  transport proof. All map introduction/spectator/native performance gates remain.

### VISUAL-1 · Visual communication and appeal pass ⚠️ OPEN, 2026-09-23

Design, reasoning and file-level routes:
[NATIONALS_POLISH.md, Visual communication and appeal pass](NATIONALS_POLISH.md#visual-communication-and-appeal-pass-visual-1-2026-09-23).
Each child ends with a same-camera before/after pair plus a 25 percent greyscale
thumbnail (V4). Every new look lever has an "off" value that reproduces today's look.
Human taste approval is separate and never blocks the next child.
**Research and rules (owner, 2026-09-23: "i really dont wanna have to communicate by using
text"):** every finding, source, plan and idea for this pass is recorded in
[reports/visual-research-2026-09-23/findings.md](reports/visual-research-2026-09-23/findings.md)
(Sepak U, Knockout City, Rocket League, Valorant, Overwatch, TF2, the VALORANT shading article,
Riot's VFX principles, Peacocke et al. 2018, Fagerholt and Lorentzon 2009, Hodent's heuristics,
the Apex ping system). The step-by-step build plan with files, traps and acceptance checks is
[reports/visual-research-2026-09-23/implementation-plan.md](reports/visual-research-2026-09-23/implementation-plan.md). Standing rules from it: no sentences in ordinary play, a
state is a shape, colour and place; world first, then screen edge, then HUD; one card and
badge family for every in-match surface; timers as rings; the HUD recedes when effects peak.
- [ ] **VISUAL-1.0 Research record kept current.** Append each new reference and decision to
  the findings file in the same commit as the work it informs.

Batch A, communication:
- [x] **VISUAL-1.1 Danger made visible.** Implementation and focused qualification DONE.
  Existing HUD danger frame unchanged. World square follows `Balance.ConfinementRadius`
  and all five map surfaces, with rest/armed/restore sweep; local nearest exit;
  physical escape dust/swish; quiet filtered air. Per-camera restoration prevents
  preview/replay leakage. Shared boundary and escape replay use recorded state.
  Authorable `WorldCueProfile` supplies independent off values. Same-camera normal,
  comfort and25percent-grey evidence personally inspected in
  [look-1.1-world](reports/visual-research-2026-09-23/look-1.1-world/report.md).
  Final changed checks2/2, five-map geometry1/1; earlier replay/audio check retained.
  Muted stranger-readability and human listening acceptance remain P7 external review,
  not a claimed result. No gameplay rule, collision or completed HUD changes.
- [x] **VISUAL-1.2 The taya's view and shared restore clock.** Implementation and
  focused qualification DONE. Camera-only Defense-blue upper rim and open catchable
  brackets use actual tag predicates and restore after nested views. Existing reach
  tick unchanged. Close can clock fills with actual reset progress, drains protection,
  shares presentation snapshots with peers and records both in existing clip11 state.
  Bounded distance readability for cast/can preserves their authored colours. Normal,
  comfort and25percent-grey frames personally inspected in
  [look-1.2-world](reports/visual-research-2026-09-23/look-1.2-world/report.md).
  Target checks passed v4; wire compatibility v3; strengthened actual clock v7 (1/1).
  Real peer/latency and native/human interpretation acceptance remain P7.
- [x] **VISUAL-1.3 Timers on objects.** DONE (look-batchA-v4 icon sheet): the recall ring
  drains in gold through the fetch warning, turns solid Offense orange while the penalty
  runs and drains in the owner's seat colour during a roof or lagoon return
  (`SlipperRecallMark.Timer`); the two prompt sentences are gone and the recall mark's own
  edge chevron was already the off-screen cue. Original scope: own-slipper marker ring for fetch grace, penalty
  and roof or lagoon return; an edge chevron when your slipper is off-screen. No timer
  text in ordinary play.
- [x] **VISUAL-1.6 Reticle as the personal-state hub.** DONE (look-batchA-v4
  reticle-states): `HudReticle` draws a dot and ticks, a charge ring from the 0.35 floor, a
  pektus arc on the curve's side, a thin cooldown sweep for throw, shove, lunge and tag, a
  grey refused state while the can is protected and (1.2's HUD part) a blue reach tick for
  the taya from `Combat.InCone`. Charge text and timed status lines are gone. Original scope: a drawn reticle with charge ring,
  pektus tick, cooldown sweep and refusal state, replacing the "+" glyph and the charge
  and pektus sentences.
- [x] **VISUAL-1.4 Minimalist in-game HUD.** DONE. HUD 120 plus High contrast captured at
  1920x1080, 1600x680 and 960x540 (look-batchA-v4); chip badges now turn white on the
  contrast plate. Area with the drawn reticle: Classic 4.15, Hero Strike 5.71 percent. First slice DONE at `4d85395c` (match bar, can
  glyph, taya-coloured round pips, stamina arc, prompt pill, powers in the lower right, 28-unit
  floor; before and after in `reports/visual-research-2026-09-23/look-1.4-v2`). Second slice
  DONE (look-1.4-v3): pictogram feed (portrait, Knock/Restore/Tag/Block glyph, portrait on dark
  plates, words kept only as `Entry()` records), "+N" pops rising into the scorer's chip instead
  of centre score toasts, a drawn hit mark, the toast on the halftime brush under the bar,
  "TAGGED" instead of a sentence, the owner's 1600x680 window in every HUD capture. Measured
  permanent HUD: Classic 3.72 percent and Hero Strike 5.30 percent of 1920x1080 (budget 8,
  asserted by `TumpNativeHudTests`). Version label confirmed hidden; sandbox line only offline
  in warm-up or practice. Remaining: HUD 120 and High contrast captures. Original scope:
  centred top bar of four player chips around
  the clock with state and role badges, pips for rounds and one can glyph; a compact
  bottom-centre kit with corner keycaps and a contextual stamina arc; a pictogram feed;
  no sentences in ordinary play; one plate style; permanent HUD under about 8 percent of
  the frame at 1920x1080. Replaces the four score slabs and duplicate can text. Checked at
  1920x1080, 1280x720 and the owner's short wide window with every accessibility setting.
- [x] **VISUAL-1.5 One signal language.** Implementation/comparisons DONE.
  Completed HUD glyphs, recede and tint priority retained. The supposed remaining
  world LATA DOWN sentence was already absent; actual event test confirms it.
  V2 owner comments added. Existing grade supports separate off controls for quiet
  peripheral sprint/dash strokes, audible-footstep bearing pips and world-only
  ultimate desaturation preserving cast/can/prop colour and scene occlusion.
  All experiments default OFF until the owner's later default choice.
  [look-1.5-experiments](reports/visual-research-2026-09-23/look-1.5-experiments/report.md):
  v2 behavior3/3, v3 corrected personal captures2/2; normal/25percent-grey reviewed.
  External taste/default selection and integrated performance/native review remain P7.
- [x] **VISUAL-1.7 FPP viewmodel framing.** Implementation/focused qualification DONE.
  Fixed95degree apparent lens independent of world75..110, lower/smaller rest,
  lit Toon arms and separate held-slipper rim; camera scopes cover attached effects
  and spectator copies. Existing charge/release timing unchanged, with a short
  preparation pulse and lower-screen follow-through past centre/settle. Off and
  reduced-motion routes retained; physics/rig/model assets untouched.
  [look-1.7-viewmodel](reports/visual-research-2026-09-23/look-1.7-viewmodel/report.md):
  v1 behavior2/2, v5 actual release1/1; normal/comfort/grey25 and before/after
  release sequences personally inspected. Integrated native/all-cast acceptance P7.

Batch B, the world:
- [x] **VISUAL-1.8 Toon lighting.** Implementation/focused qualification DONE.
  Authorable per-map two-band ramp, lower tinted ambient, upper rim/body foot gradient,
  cap-only can metal accent, contact blobs/lower-wall grounding and nearer horizon fog.
  Runtime off restores original scene settings; previews keep independent shader scope.
  Raw HDR diagnostic1.95:1 before,2.39:1 selected look; Built-in/geometry unchanged.
- [x] **VISUAL-1.9 Court and ground as the stage.** DONE. Reversible contrasting
  chalk/charcoal, static grain, scuffed home area and5medium-specific floor-wear modes.
  Multiplicative overlays preserve real lighting/shadows and omit unsupported cells.
  No collision or route changes; original meshes/material assets retained.
- [x] **VISUAL-1.10 Hero objects.** DONE. Can contrast/caps/ground support replace the
  permanent red rim under the new look. A dotted footprint follows real toppling or
  elevated authoritative poses; normal knocks have no fabricated ballistic flight.
  Short ordinary ink trails use actual ThrowerSlot and retain their look in replay.
  [look-batchB-world](reports/visual-research-2026-09-23/look-batchB-world/report.md)
  contains all5normal/comfort/grey25 comparisons, inspected can/stroke frames and
  focused receipts. Integrated native/performance/peer/human acceptance remains P7.

Batch C, effects and motion:
- [x] **VISUAL-1.11 Effects in the ink language.** Two-tone ink shapes, erosion
  dissipation, chalk dust and puffs, confetti rebuilt as fluttering paper outside the
  camera's central cone, `CanContactAccent` and slipper trails off `Sprites/Default`.
  Ink/paper/contact-dust slice implemented and focused1/1 passed; first look retained.
  [Evidence](reports/visual-research-2026-09-23/look-batchC-ink/report.md).
  Chalk-line skid sampling is integrated through real motion and saved cue timing;
  native qualification remains P7.
- [x] **VISUAL-1.12 The exchange as one performance.** Check PRESENTATION-1 against the
  V3 beat sheet for knockdown, tag, block, escape and failure cases; confirm remote charge
  and lunge windups read at 8 to 12 m and strengthen poses before adding markers.
  Implemented: remote lunge preparation was missing and now uses a compatible
  presentation suffix; body counterbalance, expiry/stop, no gameplay mutation.
  [Focused1/1 and8/12m views](reports/visual-research-2026-09-23/look-batchC-exchange/report.md).
  Live peer/readability acceptance remains P7; stills are not that acceptance.
- [x] **VISUAL-1.13 Round rhythm.** Round-start role swap beat, round-end settle,
  chalk accents on the compact halftime popup. Existing role ring now gives one
  bounded settle on a real role change, removed by reduced motion. Round arm/rest
  and settle verified in the exchange case; completed HUD/popup/results preserved.

Batch D, with existing scope:
- [x] **VISUAL-1.14 Environment appeal and life.** Building value structure, window
  glass, roof edges, landmarks, banderitas where they fit, event reactions. Tracks with
  PRESENTATION-1.5 and 152.4. Implemented sky-gradient glazing, wall/roof value
  treatment and supported peripheral cloth in Bayan/Eskinita; retained existing
  distinct map motion/reactions. Five-map focused1/1 passed; normal/comfort/grey25
  inspected. [Evidence and limits](reports/visual-research-2026-09-23/look-batchD-world/report.md).
- [x] **VISUAL-1.15 Spectator, ultimates and audio.** Framing hysteresis, complete
  seven-hero ultimates through the shared phase, one audio peak at a time. Tracks with
  PRESENTATION-3, PRESENTATION-4 and PRESENTATION-5.2. Existing committed shot
  and seven-hero shared-phase paths retained; overlapping bass bodies now leave
  headroom while every attack remains audible. Rubber-step DC corrected without
  changing contact timing. Focused audio1/1 passed; live listening/device/normal-speed
  qualification stays P7. This is not human approval or a final build claim.

In-match UI revamp (owner addition, 2026-09-23), in batch A alongside 1.4:
- [x] **VISUAL-1.16 Match-end board.** DONE (look-1.16-v1): the court stays visible under a
  62 percent warm plate (still the click blocker), a brush winner line, the standings as the
  bar's chips grown (seat-colour portrait, crown and gold border for the winner, gold
  underline for you) with two record-backed accolades each as glyph and number
  (`MatchResult.Accolades`: knockdowns, catches, close retrievals, sabotages, retrievals;
  zero is never drawn), a gold REMATCH and quiet NEXT MAP and MAIN MENU. Every route, tab
  and name kept; `TumpNativeResultTests` 5 of 5. Owner-podium with models on the court is
  left to the world agent. Original scope: rework the result screen in the same visual
  language as the new top bar and the halftime popup: a clear winner moment, the four
  chips ranked with scores that count only after the final value is shown, per-player
  highlights from the existing recognition facts, and one obvious next action. Keep the
  existing flow, rematch and exit routes, ranked readouts and all three input devices.
- [x] **VISUAL-1.17 Mid-round and halftime reports.** DONE (look-batchA-v4 CourtBreak): the
  popup moved to the upper third in the match bar's family (brush headline, next taya
  ticket with the taya border and can badge, a draining return ring, standings as cream
  chips with crown and your underline); every label is 28 units or more, so the NextRole
  floor fault is fixed and `TumpNativeHudTests` is 8 of 8 with the exchange and icon tests.
  Original scope: the compact halftime popup and any
  round-end summary share one card style with the match-end board: standings as chips,
  next taya as the one highlighted fact, the court visible behind. No full-screen board.
- [x] **VISUAL-1.18 In-match icon set.** DONE (look-batchA-v4 HudIconSheet): power icons in
  the deck draw at one stroke weight with round ends and a black keel
  (`TumpAbilitySymbol.HudStyle`), `HudBadge` holds every state and event glyph, and the can
  glyph replaces `TumpSymbol` in the bar and the off-screen marker. The drawings (each
  ability's job) are unchanged; the front end keeps its thin line. Original scope: one consistent family for ability tiles, state
  badges, the can glyph, role badges, feed pictograms and prompts: one stroke weight,
  one corner treatment, black outlines, readable at the smallest HUD size. Replace
  placeholder project-generated icons (AGENTS: swappable, not approved art); never
  repaint supplied artwork. Every ability keeps its `AbilityGlyph` job (VISION § 3).

### UX-1 · Owner front-end flow and real progression systems, OPEN2026-09-23

**Latest correction overrides the attached brief:** DO NOT TOUCH LOGIN AND MAIN
MENU. Both existing surfaces/art stay intact. New HOME opens when the existing
main-menu TAP TO START action is pressed; rewire only the destination needed for
that flow. Do not replace main menu or automatically bypass it after login.
This adds to the queue without deleting or restarting VISUAL-1. The completed
match HUD, halftime popup and match-end board are outside this front-end lane.

Full supplied requirements: [verbatim brief](reports/front-end-flow-2026-09-23/owner-brief.txt),
[correction and intake](reports/front-end-flow-2026-09-23/intake.md). Original flow,
custom-game, profile-door and final Home sketches plus all seven zip images are
preserved under `ArtSource/front-end-flow-20260923/` with provenance hashes.
The UX is prescribed; design the visuals in TUMP's playful street-game identity,
logo palette, Darumadrop/supporting face and purposeful motion. No generic white
or pale styling or blue/navy UI chrome; Defense blue remains a rule cue. Preserve
supplied final artwork. Use actual engine renders for cast/map placeholders.

**Lane, 2026-09-23:** UX-1 is being implemented on a separate machine in parallel with the
VISUAL-1 batch D lane. The design, routes, per-screen four answers, economy rules and
verification plan are in [ux1-plan.md](reports/front-end-flow-2026-09-23/ux1-plan.md).
Key decision: HOME is the view of the `MatchSetup` scene over the live court, so every
existing networking path stays in `ConvertedMatchSetup`; TAP TO START and match exits land there.

**Status 2026-09-23 (UX-1 lane):** every screen and system below is implemented in
`Runtime/UI/Hub/` (map in ux1-plan.md § 7b) and walked by `HubFlowTests` through its
real buttons with captures at 960x540, 1280x720, 1920x1080, 1280x960 and 1600x680. An
item is ticked only when its screen passed that fixture and its captures were looked at.
`wallet.js` is now published as version1 in the existing production project; live
source and all3parameters verified against local. Rules pass the existing node
contract. Runtime service actions remain unchecked; deployment receipt is in
[completion/wallet-deployment.json](reports/front-end-flow-2026-09-23/completion/wallet-deployment.json).
Restore path (the § 68.3 keep-the-old-chrome rule): launching with `-tp-preparation-board`
sets `ConvertedMatchSetup.HubEnabled = false` and the retired preparation board is the view
again. It is also how the old fixtures are run against the view they were written for.

Implementation order within UX-1:
- [x] **UX-1.0 Plan and route/data audit.** Done in ux1-plan.md (routes, four answers per
  screen, data ownership, saves/IDs, three devices). Read CLAUDE4a,6.2-6.5 and
  Front_End_Design. Answer the four screen-design questions per surface. Map every
  old feature to one visible destination and preserve saves/IDs and three devices.
- [x] **UX-1.1 HOME hub after TAP TO START.** DONE: `HubHome`, walked by `HubFlowTests` and
  `HomeFlowTests.TapToStartOpensHomeWhoseDoorsReachProfileAndLoadout`, captured at five shapes. Top-left square avatar opens picture
  view/change; adjacent level/name/#tag/XP plate opens Profile Settings. Skill Tree
  card and notification dot below. Left HERO, LOADOUT, larger SHOP with TASK beside
  it. Top-centre elapsed queue/cancel-X only while queued. Top-right currency/+ and
  hamburger. Bottom-right selected map/mode card above PLAY. Reserve a clean
  full-bleed animated-scene layer; use a still/current court now, not a new animated
  background project. Login and main menu remain intact.
- [ ] **UX-1.2 Mode and match-entry flow.** Built and walked offline (GAMEMODE SELECT, both
  popups, PRACTICE, the queue plate and X, MATCH FOUND, timed CHARACTER SELECT). Open: a
  real two-to-four-peer queue pop through MATCH FOUND into a match, which needs UGS Relay. Mode card opens four-card GAMEMODE SELECT:
  small stacked Practice/Custom, tall Classic/Ranked, descriptions on hover/focus,
  top-right currency/menu. Practice enters practice directly. Custom opens HOST/JOIN
  popup. Ranked is Hero Strike and returns Home. Classic asks Classic/Hero Strike
  casual in a popup and returns Home. PLAY queues, then MATCH FOUND, then character
  select for everyone, then loading. Keep real queue cancellation and pick rules.
- [x] **UX-1.3 HERO screen.** DONE: `HubHero` with the real kit, STORY door, UNLOCK/PLAY AS. Large illustration/model left, previous/next/back;
  role/name, actual-kit clickable ability details, biography and real UNLOCK right.
- [x] **UX-1.4 LOADOUT and ITEM POPUP.** DONE: `HubLoadout`, `HubItemPopup` (inspect, star, EQUIP/BUY). Owned/unowned tabs, currency/menu, item grid,
  equipped tag, favourite star, selected corner brackets, dim unowned silhouettes.
  Right categories TSINELAS/LATA only; skill alternatives move to Skill Tree. EQUIP
  bottom-right. Item opens popup over dimmed grid with back/name/favourite, inspectable
  3D model, fullscreen/inspect brackets and EQUIP. Remove old STATS button.
- [ ] **UX-1.5 Character select.** Built (`HubCharacterSelect`, today's pick RPC, lock-in via
  the ready tally, host start on all-locked or 30 s). Open: a multi-peer lock-in check. After match found, big name/model,3x4 portrait grid
  and SELECT using existing legal pick rules; preserve the complete roster.
  P6 review found the timed selector lost VISION3's Learn layer. Add a compact
  selected-ability readout using actual kit/variant data: icon, name, kind, one
  sentence and cooldown/ultimate charge. Keep the selection clock running while
  inspecting it; no modal that suspends host Tick, no redundant navigation tutorial.
  Implemented: all7heroes/3slots checked with normal and large/high-contrast text,
  five shapes and running timer; both960x540frames inspected. Peer lock-in remains open.
- [ ] **UX-1.6 Custom host/join.** Built; a real LAN host is opened by `HubFlowTests`. Open: a
  second process joining by code and from the LAN and online lists (`tools/net_matrix.py`).
  Immediate native code entry exposed cold LAN lookup plus concurrent online-query429;
  bounded discovery/query-spacing fix passed its actual datagram case and native
  immediate code/rejoin run. Both clients were seated and saw LOBBY; no429. Host: lobby name, defaulted map, game mode,
  public/private/friends-only visibility, LAN/Online, CREATE LOBBY; subtle selected-map
  art updates. Join sources: Dedicated Internet, Dedicated LAN, Code. Shared server
  list with name/map/player count/join and correct Online/LAN heading; code field/JOIN.
- [ ] **UX-1.7 Custom lobby and loading.** Built (`HubLobby`, `HubLoading`). Actual two-native-peer lobby entry, automatic
  intro/countdown and rematch passed; both exited0 and shared input prefs stayed intact.
  Native code reconnect into LOBBY also passed using the same isolated profile.
  Online list/Relay coverage stays separate under UX-1.6. Lobby name/back, selected-map background,
  n/4 portrait list/host mark, START GAME and character/loadout/settings doors.
  Loading uses map art/name/percentage, bottom tips, optional BH Studios mark. Preserve
  UGS Lobby/Relay,4-character codes,LAN discovery,quick/ranked,reconnect and rematch.
- [ ] **UX-1.8 Real soft currency and hero/item shop.** Built: `EconomyRules`, `wallet.js`,
  `WalletStore`, SHOP popup, UNLOCK/BUY. Deployment DONE: version1 and exact source/params verified in existing project
  production. Live wallet action checks remain open; no runtime pass inferred. SHOP is a popup exposing Hero
  and Loadout shops; Home HERO/LOADOUT open directly. Server-authoritative Cloud Code
  balances/unlocks, profile migration and graceful offline behavior. No client grants,
  real-money purchases or paid services. Keep the existing UGS project/IDs.
- [ ] **UX-1.9 Tasks, skill tree and unlocks.** Built: `HubTasks`, `HubSkillTree`, server
  claims. Deployment complete as UX-1.8; live claim/action verification remains open. Currency+ opens earning/tasks. Skill
  Tree owns actual hero alternatives and integrates XP/levels/mastery,
  HeroBuildRules/AbilityChallenges,Cloud Save/Cloud Code. Real unlock transactions;
  cosmetics never alter gameplay and Classic stays neutral. Verify declared Cloud
  Code params and UGS refused:0 when live service checks are available/authorized.
- [x] **UX-1.10 Profile and hamburger doors.** DONE: `HubAvatar`, name plate → `PlayerHub`,
  `HubMenu` (settings, party, career, match rules, learn to play, credits, title, quit). Separate avatar-picture view/change
  from name-plate Profile Settings (name,achievements,friends,etc.). Hamburger retains
  settings,party,career hub and every former feature, with one visible route each.
- [ ] **UX-1.11 Per-screen acceptance.** Five shapes, 28 floor, bounds and one-press BACK are
  asserted per screen. Retired preparation-board fixture routes migrated; complete
  High contrast + Larger text route passed at five shapes. Core629/629, requested
  EditMode36/36 and final editor checks8/8 pass. Open: full/native regression and
  physical pad/touch passes. Actual mouse/keyboard,controller focus/B and
  thumb-sized touch; live bindings,one-press Back.960x540,1280x720,1920x1080,4:3 and
  1600x680; every label28canvas units or more; High contrast/larger text. Use shared
  approved canvas/input construction. Inspect captures of default,hover,focus,locked,
  empty and error states. Update old-screen probes to reachable new routes with a
  design reason. Focused changed-behavior checks only; full/native gate remains P7.

- [ ] **UX-1.12 Complete queued match arrival (owner addition2026-09-23).** Selected
  Ranked on HOME -> PLAY -> queue elapsed timer -> MATCH FOUND -> character select ->
  map vote -> deliberate loading presentation -> short map/player introduction with
  camera panning for5..10seconds ->3,2,1,START. Remove the in-game R-to-start step for
  queued matches. Custom rooms may retain manual ready, behind a room option. Reuse
  existing ballot/load/intro/countdown authority; inspect and fill actual missing
  connections, do not replace working stages or remove prior TODO requirements.
  Exact request and implementation decisions in the UX-1 completion plan.
  Local arrival/manual/reduced-motion cases pass. Two native peers entered the lobby,
  saw arrival/automatic countdown and rematched without any diagnostic READY press.
  Both exited0; correct authentication profiles and no duplicate-sign-in errors.
  Native evidence is custom automatic rooms, not a live UGS queued-map-ballot claim.
  Build missing UI to finished quality now; keep every built/changed surface in
  [the UI inventory](reports/front-end-flow-2026-09-23/ui-authorship-inventory.md)
  for a possible later refinement review, with paths/evidence/remaining issues.

- [ ] **UX-1.13 Mode names, awaiting owner selection.** Owner requests simpler,
  meaningful replacements for Classic and Hero Strike throughout the game. Options
  revised after the owner clarified the distinction: Chill/Powers (recommended),
  Chill/Chaos, Relaxed/Powered. The first should sound relaxed, the second ability-based. Do not pick on the owner's behalf or rename before their choice.
  After selection, audit all player-facing menus/HUD/help/settings/announcements and
  current design copy; preserve existing save, analytics and wire identifiers through
  a display-name mapping. Historical quoted feedback and evidence remain intact.

- [x] **UX-1.14 Loading tips and rotating artwork (owner2026-09-23).** Tips belong
  directly on the loading screen, not behind STORIES & TIPS opening a separate
  overlay/HUD. Replace the rejected loading-street illustration with a small set of
  newly generated, cohesive game-world images; rotate about every5seconds during
  loading with reduced-motion support. Applies to boot loading and match loading;
  preserve the title/login artwork. Save prompts/provenance and add surfaces to the
  UI inventory. Do not slow fast loads solely to cycle through every image.
  Implemented in both loading owners; actual boot readiness and three-image rotation
  passed. Revised contrast/type captured and inspected. See completion evidence.
- [x] **UX-1.15 Terms reading and consent (owner2026-09-23).** Explicit narrow
  exception to login protection: improve the Terms and Conditions presentation and
  behavior, expand meaningful content, and make consent unmistakable. Unaccepted is
  an empty square; accepted is a solid-filled square, NEVER a tick/checkmark. Preserve
  actual acceptance state, keyboard/controller/touch operation and account gating.
  Draft terms against actual implemented features, not invented services or promises.
  Implemented and passed: normal/large text at five shapes, scroll, unchanged consent
  on open/Back, solid fill and actual validation. Latest arrow-only popup inspected.
  Product draft remains subject to the owner's public-release legal/contact review.

- [x] **UX-1.16 BH Studios proportions (owner2026-09-23).** Fix the stretched studio
  mark without repainting the supplied logo. The runtime copy's power-of-two import
  changed its445x370 proportions despite preserveAspect on the Image. Preserve
  original texture dimensions and alpha; check the actual loading mark and dismissal.
  Existing loading/room-flow case passed; actual five-shape loading captures checked.
  Corrected screenshot:completion/Loading-studio-proportions-960x540.png.
  Owner screenshot retained under the UX-1 completion references. Map emptiness in
  the same screenshot belongs to the later per-map context/cultural brief.

- [x] **UX-1.17 Per-hero HOME loops, picked at random (owner 2026-09-24).** "i decided to
  make diff character lobby screens and the current one will js be random", "make it
  random which one shows up". Phaister's loop (Ilalim ng Tulay at night, a three-act trick
  in one unbroken shot) is built: research, design and timetable in
  [phaister.md](reports/home-scene/phaister.md), source `ArtSource/home-scene/src/phaister/`.
  `HubSceneVideo.Pick` rolls over `HubSceneVideo.Heroes` whose clip ships and falls back
  to Zack's; `HubSceneVideoTests` covers every listed hero's files, the pick and the
  fallback, and Phaister's loop playing in the real hub. Evidence and remaining limits
  are recorded in phaister.md § 5. Next heroes (Sean, Dante, Cheska, Nemu) each need their
  own place and hero moment by the method, never a copy; at 25 to 38 MB per clip and no
  LFS, raise repo size with the owner before shipping all six.

- [x] **UX-1.18 Every hub screen is its own place (owner 2026-09-24).** "check out and refine
  the look of all other ui", "give them all more personality", "each screen shoudl feel like
  their own screen". Every pushed screen was the same call (`HubPattern.Ground` in maroon plus
  the same chalk marks) under the same header, which is `CLAUDE.md` § 6.5's "the same code to
  generate them all" again. `HubScenery` makes the central theme ONE STREET and each screen a
  different place on it, all mesh, all logo palette, all still under reduced motion: SKILL
  TREE is chalk on asphalt (branches chalk themselves in), GAMEMODE is posters taped to a
  yero wall, JOIN a hollow-block wall with a lone lata for an empty list, TASKS the listahan
  (cardboard taped to a chipped wall, marker lines, highlighter on a finished task, a red
  tick once claimed), LOADOUT and SHOP the sari-sari store (planks, awning, a shelf under
  every row, the item popup on the counter under the bulb), HERO a collector's poster (rays,
  halftone, slanted role tag), HOST the organiser's clipboard, CHARACTER SELECT and MAP VOTE
  the court at night (one warm light, a chalk circle under the hero), LOBBY and MATCH FOUND
  the fiesta (bunting on the title street's breeze; MATCH FOUND bursts, slams and jolts once).
  Walls get sprayed titles that drip like the TUMP graffiti; roads get chalked ones.
  Research, before and after captures and every decision:
  [ui-personality-2026-09-24](reports/ui-personality-2026-09-24/README.md). HOME, the login,
  the title, LOADING and the in-match HUD are untouched on purpose. Final run: HubFlowTests 4/4,
  MatchArrivalFlowTests 4/4 (its larger-text ability-name clip, red since 2026-09-23 23:53, is
  fixed by re-stacking in `HubCharacterSelect.Tick`), title and home suites 8/8.

- [x] **UX-1.19 The match-end board speaks the game's button language (owner 2026-09-24).**
  "keep trying to improve, include ingame ui in improvements". The in-match HUD, the NEXT
  ROUND ribbon and the pause card were inspected and kept (VISUAL-1's minimal rules hold).
  The finish sheet was the outlier: REMATCH a flat gold box, NEXT MAP and MAIN MENU flat dark
  plates, tabs as bare words. All six are now the hub's pressable sticker (`HubShape`):
  REMATCH the chartreuse primary, the rest honey, the open tab persimmon, all in the display
  face; the XP track is a chunky inked bar; fiesta bunting drops in over the winner's banner
  (`HubScenery.Bunting`, still under reduced motion). Routes, names and focus paths unchanged.
  `TumpNativeResultTests`, `MatchFinishPresentationTests`, `PhaseSurfaceLayoutProbe`: 9 of 10,
  the one red being `RematchActuallyLoadsTheChosenArena`'s one-frame scene check.
  Reconciled2026-09-24: offline loading is asynchronous; a bounded wait reached
  BayanPlaza. Final fixture source retains the actual-map/ready checks; its final
  green rerun stays P7 after the one repair exposed an invalid extra service-scene
  assumption. [Exact evidence](reports/map-by-map-refinement-2026-09-23/rematch-reconciliation/report.md).
  Open: YOUR MATCH is still one line of text in a large card, and PLAYERS a sparse list.

### UI-REVIEW · Research-first UI and HUD refinement ⚠️ IN PROGRESS, 2026-09-23

Owner brief: research first, then refine the whole game's UI and HUD, with Settings as a
priority; later the same day "improve ui LOOK of all current screens", "genuinly improve all
buttons theyre so bland and ugly", "their colors are ugly", "make sure the colors all work
well tgthr with the background", "improve all skill icons" then "do a drawing for all",
"generate profile pics too", and Rafi "DOESNT LOOK GOOD ... compared to reference" and "doesnt
look like it belongs in hero cast". Research, critique and plan:
`docs/reports/ui-hud-review-2026-09-23/` (`research.md`, `critique-and-plan.md`).
This machine's checkout: `C:/Users/Matthew/dev/TumbangPreso-Unity-ASTRAReworks`.

- [x] **UX-1.20 DONE 2026-09-24 · Phaister's HOME loop remade as "Gulatin si Nemu" (owner 2026-09-24).**
  "make sure it actually loooks like the map too ... like the pc express", "give her more action
  sequences like zack, he actually moves somewhere", "make it actually reflect our game", "tell a
  story about tump", "make her use her ult in the end ... big ass magic circle ... eyes are glowing
  pink and she's floating", "make ur own animation", "the signs we have in the game are not final so
  js imagine it on ur own, research PH store signs". The direction, research and beat sheet are
  [phaister.md](reports/home-scene/phaister.md) §§ 1 to 3: a whole round of tumbang preso on the
  real Ilalim ng Tulay layout (map.ts, read from `IlalimNgTulayBuilder`), a wrong-way throw through
  the bridge hoop, the overclock pad, the blink under the train, and Grand Coven transcribed from
  `CovenCircleBuild.BuildRings`; every pose hand-keyed, no game clips; street signs hand-lettered
  (lettering.tsx), never a typeset font. Done when the master renders, passes `smooth.py` and the
  seam check, ships through `npm run ship:phaister`, and `HubSceneVideoTests` passes.
  **Done:** master 1620 frames; `smooth.py` flags only the intended impact and punchline cuts (a 19.57 s
  camera jump through the blink was found and fixed); seam 0.78 against a 1.14 frame step; shipped at crf
  23, 35.1 MB, SSIM 0.980; `HubSceneVideoTests` 6/6 in the real hub. After review the throw was remade so
  every link is seen (`phaister.md` § 3, Act I); the owner approved the result.
- [x] **UX-1.21 DONE 2026-09-24 · Kuro is cuter and more expressive, in the game and the loop (owner
  2026-09-24).** "can u make kuro's expressions cuter both ingame and in the animation", "make it
  more expressive hehe". Board (ArtSource/home-scene `PhaisterBoard` frame 2): his blush shared the
  eyes' violet slot, so he read as four eyes; his rest mouth was a pupil-sized square; three of eleven
  idle gestures had a face. `tools/cute_kuro.py` (idempotent, rage form and motion digested
  unchanged) moves the blush to Nemu's peach slot 13 and shrinks it and the mouth, and authors seven
  new parts under `KuroExpressions`: happy, sleepy, heart and sparkle eyes, a grin, an "o", a tongue.
  `GhostPetCompanion.ExpressionFor` maps eight gestures to faces; `KuroIdleClipAuthor` rebakes the
  trailer clips; `KuroFormTests` covers every face and that every named part exists.
  **Done:** `KuroFormTests` 14/14 (`Logs/kuro-form3.xml`); the loop wears the same faces.

- [x] Settings (both routes): warm grey palette, grouped sections, loud row focus (band,
  bar, accent label), keycap and pill chips, filled SAVE with UNSAVED marker, patronising
  notes cut. `ui-batch1`/`ui-batch2` 11/11 and 12/12 (HubFlow, NativeSettings, NativeHud,
  HudIconSheet); five tabs inspected at 1920x1080.
- [x] Pause card as the sticker family (RESUME primary, SETTINGS, LEAVE MATCH destructive).
- [x] Button finish for every pressable sticker (`HubShape`): cream die-cut rim, hue-shifted
  gradient, varnish, same-hue lip, hover light, breathing focus ring, primary shimmer; paper
  lettering with an ink outline on saturated and dark stickers (`HubKit.Letterpress`).
- [x] Palette: olive grounds and fills replaced; full screens on `HubStyle.Maroon`, secondary
  stickers one warm-dark family; HOME doors with coloured wells. Mock of olive/Night/maroon
  on the real HERO capture chose maroon.
- [x] Character select: contact shadow, clock plate with PICK, stacked ability text.
- [x] HOME hero door and XP rim, LOADOUT selected-item name, JOIN one-line empty state,
  keyboard ESC cap removed from BACK (pad glyph kept).
- [x] Skill icons: 31 coloured cel-shaded illustrations (`tools/build_ability_icons.py`,
  `Resources/UI/ability-icons`), drawn untinted by `TumpAbilitySymbol` and `AbilityIcons.Tint`.
  Checked in HERO, character select, skill tree and the in-match tray.
- [x] Profile pictures: 20 composed from the real roster portraits (`tools/build_avatars.py`),
  `Avatars.Ids` and a self-sizing picker grid. `ui-batch3` HubFlowTests 4/4; picker and HOME
  door inspected. `avatar_rafi` rebuilt from the v7 portrait. Saved old ids still load.
- [ ] Rafi model, ISLANDER REWORK 2026-09-25 (his own builder only). Owner: "looks so bad",
  "research first on islanders", references a painted Visayan datu and Maui, then a concept
  sheet, then Harbor. v10 to v40 (`Logs/rafi-v*`, shared picks in `Logs/rafi-share/`): bare
  "a bit" muscular body, curly mane with a tied tail, muted sea-teal putong and bahag, silver
  cuffs and a shark-tooth necklace on a chain, ink-only nonchalant face (relaxed gaze, chill mouth),
  and a continuous batok tattoo from collar to bahag (chaklag, labid, dakag, inagdan, tud-tud;
  brief section 7). His slide re-solved on his own mesh on every build (hair went 0.112 m through
  the street; now 0.000). Portrait, avatar, FPP arms, authored clips and the HERO STRIKE poster
  refreshed. Brief: `ArtSource/rafi/islander-rework-20260925/`;
  method: `docs/CHARACTER_MODEL_METHOD.md`. Earlier v7 to v9 history is in that brief's
  table. **Owner approval of v40 still required.**
- [x] GAMEMODE posters: `Editor/ModeCardPoseAuthor.cs` renders the real models in their clips
  (1061 poses); `tools/build_mode_cards.py` composes PRACTICE, CUSTOM, CLASSIC, RANKED and the
  two choice cards; `HubCards.Art` shows a poster when one exists. Inspected at 1920x1080,
  1280x960 and 1600x680 Larger text (PRACTICE sits close to its card edge there, fitted).
  The CLASSIC/HERO STRIKE choice popup is not in the capture set yet.
- [x] Follow-up round (`ui-batch4` 11/11, `ui-batch5`/`ui-batch6` 4/4): in-match charges moved to a
  gold pip so the icon stays whole, drawn icons larger in the dials; Settings "KEEP YOUR CHANGES?"
  as three ranked slabs; JOIN source labels on two lines; HOME's mode card wears the mode poster
  anchored right (words keep the left); item popup frames the item closer; Rafi v9 chest pocket.
- [ ] Remaining critique rows once the above land: five-shape and Larger-text captures of the
  changed screens, 4:3 and 1600x680 checks, and the owner's look approval.
- Known pre-existing failures, not caused here: `OwnerAccountUsesExactArtworkTypeColoursAndWorkingTerms`
  (retired "PLAY FAIR" copy), `TitlePlayCreditsAndSettingsReturnThroughNativeViews` (retired
  title route), `RematchActuallyLoadsTheChosenArena` (stale synchronous wait;
  actual Bayan load observed, final corrected-fixture rerun remains P7).

### P6 supersession decisions

- **140.4:** the historical textual connection/countdown layout is not reactivated.
  Current VISUAL-1's adopted signal language and the completed HUD scope take
  precedence. Existing RTT telemetry and peer-departure notices remain. The new
  [research draft](reports/full-backlog-2026-09-21/connection-readout-plan.md) is
  explicitly not adopted; no HUD/network changes were made from it.
- **136.1:** the old shared debug-key catalogue was a proposed implementation
  approach. Current context guards already resolve the documented F1-F4 collision.
  Do not add a catalogue solely because the old proposal has no corresponding class;
  retain the one-action-per-context rule and investigate actual new clashes if found.

### Active items carried forward (status unchanged by this cleanup)

- [x] **127.3 accessibility software and targeted native completion.** FPP FOV, hold or toggle sprint and can restore,
  HUD size and larger text, high contrast, reduced particles and flashes, and
  delivered-announcer captions are implemented; the wider angular taya ring reads in
  greyscale; controls, layout, captions, native spectator v56 and the bounded native
  accessibility route v57 (15/15 stages) passed; the 134.10 reduced-effects link is
  published. The final specific follow-up was v57's wide large-HUD frame showing the injected
  match-chat line clipped to "LOCA...". The current focused check reproduced a second
  cause: `Ellipsise` ran before the row received its width. Fixed by laying out the
  visible column before fitting text; before0/1, after1/1, wide/small enlarged-HUD
  frames inspected in [chat-first-line](reports/front-end-flow-2026-09-23/completion/chat-first-line/README.md).
  The right anchor is also asserted. Native7a0ae3363plus the opt-in diagnostic passed
  both1680x720 and960x540: full line visible over the actual map, personally inspected;
  player exit0 and shared input unchanged. The focused route bypasses retired menu
  traversal and does not claim a new whole-settings run. Physical device and final
  integrated acceptance remain separately in P7.
- [ ] **Native player shutdown crash.** v57 (and one earlier recorded runner result)
  exited with 0xC0000005 after the review had passed and `CodeReloadManager destroyed`
  was logged. On2026-09-24 the exact v57 binary passed15stages on both backends:
  D3D12 then crashed in D3D12Core1.618.1.0 at0xa1f5; D3D11 exited0 on RX6600.
  Windows now explicitly prefers D3D11, retaining D3D12 second. Current-source
  five-map D3D11 rendering passed1/1 and actual views/grey were inspected. This is
  a machine-supported compatibility mitigation; internal engine/driver cause is
  unproven. P7 still owes current-player default-backend, exit and performance.
  [Native comparison and evidence](reports/map-by-map-refinement-2026-09-23/native-shutdown/report.md).
- [ ] **Rafi B / lagoon C expansion, final integration.** Model, kit, map, v47 to v52
  evidence and the three-peer water checks are done (see the done list). Final coherent
  qualification remains in P7. Local deck sampling refinement is DONE:47deck
  renderers use filtered single-plank grain and local thin-plane outline suppression;
  original mesh/collision preserved, off restored, focused1/1 and personally inspected
  before/after/comfort/grey25 in [look-p3-deck](reports/visual-research-2026-09-23/look-p3-deck/report.md). Owner addition
  2026-09-22 (varied background islands and mountain layers with stable seeded
  construction and clear routes) is implemented and awaits final qualification.
- [ ] **Final integration:** remaining overlap, real-peer, whole-backlog disposition and
  coherent candidate qualification (P7; P6 disposition complete). Preserve all existing task IDs.
- [ ] **PRESENTATION-1 /154/155/152.4: Complete ordinary exchange.** Unchecked while 1.5's
  full listening review is open.
  - [x] PRESENTATION-1.1 throw, contact, flight, landing, retrieval, restore and chase as
    one body/FPP/VFX/SFX/UI sequence in both modes.
  - [x] PRESENTATION-1.2 starts, stops, turns, carrying, grip/cancel, slide, shove,
    punch/lunge whiff, stagger, get-up and grounded sound/contact timing.
  - [x] PRESENTATION-1.3 contextual state: own slipper, can state/protection, legal
    danger and next action, with stable identity and glyphs. (VISUAL-1.1 to 1.3 extend
    the presentation of this; the state logic stays.)
  - [x] PRESENTATION-1.4 material sound, personal confirmation, world reactions,
    score-row accents and earned graphics shared with PRESENTATION-2.
  - [ ] PRESENTATION-1.5 character/crowd/prop/atmosphere reactions on existing maps and
    the full mix review. Reactions and audio signal checks are implemented; the full
    listening review remains. VISUAL-1.14 extends it.
  - [x] PRESENTATION-1.6 normal/busy play and Low/reduced/small-view clarity.
- [x] **PRESENTATION-2 /152.4: catch and capped chains** (2.1 host bookkeeping, 2.2 capped
  rewards and milestones published through ce0e83cf, 2.3 victim catch and return).
- [x] **PRESENTATION-3 /152.4: six complete hero performances** (3.1 shared phase,
  3.2 authored performances, 3.3 live warnings and ordinary skills, 3.4 role/view and
  cleanup). Human taste and broader device review stay in 5.2.
- [x] **PRESENTATION-4 /134.20: watchability and halftime** (4.1 recorded clips, 4.2 live
  spectator shots, 4.3 halftime package and punctuation).
- [ ] **PRESENTATION-5: integrated qualification.**
  - [x] PRESENTATION-5.1 four active players, overlapping events, both modes, all views.
  - [ ] PRESENTATION-5.2 audio and muted review, ordinary-speed and short replay,
    compression, 720p/1080p/aspect, Low and comfort controls; human listening and taste,
    full human play and physical input/device/separate-machine validation.
  - [x] PRESENTATION-5.3 native/LAN late, duplicate and interrupted cases, invariants,
    costs and cleanup.

Remaining existing scope, unchanged, now scheduled in P3 and P5:
1. **152.4 / MAP_FINAL_PASS / owner map revisions:** finish remaining architecture,
   vegetation, street life, material/lighting, readable routes and Sa Bubong
   pool/edge/resident gaps from current source and evidence; keep swimming, recovery,
   laundry and map palettes; do not restart the completed foundation.
2. **151.9 / 151.19:** audit remaining alternative skills, body/FPP/contact/cancel
   continuity and graphics controls; implement unmet contracts.
3. **153 / U8 / 149.4 / 145 / 143:** remaining primary and secondary UI routes, prompt
   rebind and device-state behaviour, native preview/layout and feature-relevant
   authority/rejoin gaps. Hardware, external services and human judgment are named checks.
4. **152 final delivery:** qualify changed contracts, all maps and roster routes, native
   controls, representative full matches and real peers on one Windows candidate.

### Done in the 2026-09-21 and 2026-09-22 passes

Kept here so their pointers still land. Full wording is in
[archive/TODO_queue_2026-09-22.md](archive/TODO_queue_2026-09-22.md) and the named reports.
- [x] Black in-game UI outlines (`reports/full-backlog-2026-09-21/black-ui-outlines.md`).
- [x] 152.4 complete environment surface pass: 2686 renderers, 19 construction families,
  native v39.
- [x] 152.4 animated sky, and Inday plain arms (environment v37; the preview-to-match
  lifecycle defect found during it was repaired with a failed-before/passed-after check).
- [x] Rafi blocky hair with no eyebrows (`rafi-block-hair.md`); Rafi rebuilt through the
  copied builder (`rafi-parts-and-motion.md`); lagoon refinement with 8 connected and
  10 detached stilt homes, boats, residents and islands (`BADJAO_EXPANSION.md`, v47 to v49).
- [x] The quality and verification rule preserved in AGENTS.md and the ledger.
- [x] Final-integration recall renderer checks in both modes (v49c, five peers).
- [x] Compact animated halftime popup over the visible court replacing the green board.
- [x] Sepak U and Blue Lock research merged into NATIONALS_POLISH (2026-09-21).

**Still owed from those passes and not hidden:** 1.5's listening review and 5.2's human
listening, taste, play and physical device checks; the owner's full-size valid-mark
export and the Google OAuth client id (`TUMP_GOOGLE_CLIENT_ID` absent, checked
2026-09-22); live UGS deployment of the Rafi mastery source. Retired from automatic
pick-next and kept with their fixes: unchanged C2 lunge-ratio and C3 registry work and old
spectator, manual-control and menu rechecks, reopened only for a relevant regression.
C1's exact idle attribution stays historical and unresolved.

**Ownership:** owner handback 2026-09-21, "get everything done u are the only agent
working on this". Necessary C4 network integration belongs to this queue; its report,
tests and unfinished 149.4 checks are preserved. No delegation or cross-chat work.
**Map feedback still binding:** blank or poorly textured buildings and empty skies need
construction-specific material and sky refinement on every map as part of 152.4, never
one texture or noise stamped everywhere.

## Open backlog index

One row per numbered entry whose body lives in [TODO_Backlog.md](TODO_Backlog.md), in the
order they appear there. The disposition column follows the cumulative review recorded in
`reports/full-backlog-2026-09-21/todo-disposition.json`; all 366 headings now have
source/design dispositions. Remaining implementation and qualification are explicit
in those dispositions and the current queue. The heading's own status word is
history, not proof of what is missing.

| § | Entry, heading as written | Reviewed disposition |
|---|---|---|
| [§ 155](TODO_Backlog.md#s155) | THE RECALL BEAM, DRAWN BY A SHADER RATHER THAN BUILT OUT OF TUBES ⚠️ IN PROGRESS, 2026-09-20, branch `feature/slipper-beam-shader` | Shader recall beam implemented and qualified in actual five-peer Classic and Hero v49c runs; human art acceptance remains separate. |
| [§ 154](TODO_Backlog.md#s154) | THE RECALL MARK, AND OWNERSHIP BECOMES A LOCK ⚠️ IN PROGRESS, 2026-09-19, branch `ASTRAReworks` | Own-slipper lock and recall marker implemented; binding/device and actual peer rendering evidence retained. Human match-feel judgment is not inferred. |
| <a id="153--the-title-street-and-the-login-redrawn-by-her-and-put-in-motion"></a>[§ 153](TODO_Backlog.md#s153) | THE TITLE STREET AND THE LOGIN, REDRAWN BY HER AND PUT IN MOTION ⚠️ IN PROGRESS, 2026-09-18, branch `ASTRAReworks` | Painted title/login and later menu routes are implemented. Whole-source final qualification remains active; original full-size valid-mark export and OAuth configuration are external dependencies. |
| [§ 152](TODO_Backlog.md#s152) | Coherent game improvement and release verification: IN PROGRESS, 2026-09-09 | Current expanded implementation is integrated across game feel, maps, characters and painted UI. Whole-file disposition and coherent final qualification are still active; historical UI exclusion and checkpoint stop rules are superseded. |
| [§ 142](TODO_Backlog.md#s142) | CONTROLLER SUPPORT: A PICTURE OF THE PAD, A PAD THAT CAN LEAVE A SCREEN, AND AN UNRECOGNISED PAD THAT WORKS ⚠️⚠️ OPEN, 2026-09-04, merged to `main` | Controller software paths are implemented and retain current native/synthetic-device evidence. Physical unknown-pad certification and owner art acceptance remain external; no backend replacement is required. |
| [§ 151](TODO_Backlog.md#s151) | THE NATIONALS FUN PASS: EARS AT THE PLAYER, AN IMPACT FRAME ONE PEER GOT, AND A SLIDE NOBODY COULD SEE OR HEAR ⚠️ IN PROGRESS, 2026-09-06, branch `main` | All specified listener/feedback/slide/cue implementation and later authored hero presentation are retained. Final coherent candidate qualification remains active; human audio/feel acceptance is unverified. |
| [§ 149](TODO_Backlog.md#s149) | THE FRESH-AUDIT FOLLOW-UP: MOVEMENT BUDGET, RE-ADMISSION, AND THE ONE-SHOT REQUESTS ⚠️⚠️ IN PROGRESS, 2026-09-05, branch `main` | Confirmed movement/admission/request/lifecycle defects are fixed with retained failed-before/passed-after evidence. Current native request/rehost and receipt-window results supersede the old unfinished audit; final coherent qualification remains pending. |
| [§ 148](TODO_Backlog.md#s148) | THE 2026-09-05 NATIONALS BATCH ✅ CLOSED, and archived in the commit that wrote it | Closed historical batch index. Its cited child requirements remain individually tracked; this index does not create a second unfinished implementation. |
| [§ 147](TODO_Backlog.md#s147) | THE GAME NOTICES ITS OWN GOOD MOMENTS AND WRITES THEM DOWN ⚠️ IN PROGRESS, 2026-09-05, branch `main` | Structured highlights, deduplication and replay-window markers are implemented; current shared highlights/replay/halftime extend this system without adding score side effects. |
| [§ 146](TODO_Backlog.md#s146) | CLASSIC'S DEPTH COMES FROM MOVEMENT: THE COMMITTED RETRIEVAL SLIDE ⚠️ IN PROGRESS, 2026-09-05, branch `main` | Dedicated slide/input/host/relay/bot implementation retained; final candidate motion and human feel remain P7/external. |
| [§ 145](TODO_Backlog.md#s145) | THE HARDENING THAT COULD STILL PRODUCE FALSE CONFIDENCE ⚠️ IN PROGRESS, 2026-09-05, branch `main` | Harness and identity implementation retained; exact Windows candidate qualification remains P7. |
| [§ 144](TODO_Backlog.md#s144) | THE TWO ACCOUNT-GATED DOWNLOADS LANDED, AND THE AUDIO GATE WAS GRADING A COPY THE GAME CANNOT LOAD ⚠️ IN PROGRESS, 2026-09-04, branch `main` | Sourcing and actual Resources audio gate implemented. Current listening/art assessment and final candidate remain separate. |
| [§ 143](TODO_Backlog.md#s143) | THE NATIONALS HARDENING PASS: A QUALIFICATION THAT CANNOT LIE ⚠️⚠️ IN PROGRESS, 2026-09-04, branch `main` | Qualification machinery implemented; coherent current-candidate regression/native evidence remains P7. |
| [§ 141](TODO_Backlog.md#s141) | SPECTATOR AND A DRIVEN SEAT WERE ON SCREEN AT THE SAME TIME, AND F1-F4 HAVE TWO READERS ⚠️⚠️ OPEN, 2026-09-04, branch `abilities-rework` | Spectator handover, input-context guard and distinguishable labels implemented; no speculative revival of the old catalogue proposal. |
| [§ 140](TODO_Backlog.md#s140) | THE PLAYER CANNOT SEE THE NETWORK, AND THE TIMEOUT GIVES THEM EIGHT BLIND SECONDS ⚠️⚠️ OPEN, 2026-09-04, branch `abilities-rework` | RTT/departure implementation retained; historical warning/countdown layout not reactivated under the newer completed-HUD scope. |
| [§ 139](TODO_Backlog.md#s139) | SETTINGS IS FOUR PAGES NOW, AND THE RENDERS FOUND THREE FAULTS THAT HAD SHIPPED FOR THE WHOLE PORT ⚠️ OPEN, 2026-09-04, branch `abilities-rework` | Implemented current five-section settings workspace and controller-map/rebinding routes supersede the former four-page converted panel. Owner visual acceptance remains separate. |
| [§ 138](TODO_Backlog.md#s138) | A CONTROLLER UNITY DOES NOT RECOGNISE IS INVISIBLE TO THIS WHOLE GAME ⚠️ OPEN, 2026-09-04, branch `abilities-rework` | All software discovery/fallback/settings paths are implemented and current hotplug/native evidence is retained. Actual unrecognised-controller vendor/product certification requires hardware. |
| [§ 134](TODO_Backlog.md#s134) | THE BROADCAST PASS: AUTOPILOT, REPLAY, ULTIMATE INTRODUCTIONS, THE SHOVE THAT MEANT NOTHING, AND THE KEYBOARD ON THE PHONE ⚠️⚠️ OPEN, 2026-09-04, branch `abilities-rework` | Current broadcast/touch systems and later evidence retained. Old spectator-letter layout superseded; device/final limits remain explicit. |
| [§ 133](TODO_Backlog.md#s133) | ONE FONT IS DOING EVERY JOB, AND IT IS A DISPLAY FACE ⚠️⚠️ OPEN, 2026-09-03, NEXT SESSION'S BRIEF | Old font/composition plan superseded by current owner-painted, VISUAL-1 and UX-1 designs. Surviving usability checks stay in UX-1. |
| [§ 132](TODO_Backlog.md#s132) | The loadout said nothing about the hero, and a build vanished the moment the match started ⚠️ IN PROGRESS, 2026-09-03, branch `abilities-rework` | Current HERO/SKILL TREE/LOADOUT/selector routes replace the combined board. Later variant evidence retained; no old layout or blanket font sweep. |
| [§ 131](TODO_Backlog.md#s131) | Replace Hero Strike VFX and synthesised SFX from the licensed source list ⚠️⚠️ IN PROGRESS, 2026-09-03, branch `abilities-rework` | Licensed VFX/SFX sourcing and later authored presentation implemented; current listening/overlap acceptance remains mapped. |
| [§ 130](TODO_Backlog.md#s130) | Crossplay, the boot ANR, and the lobby that was drawn in a different language ⚠️⚠️ OPEN, 2026-09-03, branch `ui-redesign` | Preview parity and boot fixes implemented; old layouts superseded by UX-1. Physical crossplay/handset acceptance remains P7. |
| [§ 128](TODO_Backlog.md#s128) | Phases 11 and 12 are almost entirely built, and this entry was wrong about it once ⚠️ OPEN, 2026-09-03, branch `ui-redesign` | Former missing format implementation is superseded by implemented LastTsinelasDirector and network map ballot; final format integration remains in the retained qualification gate. |
| [§ 127](TODO_Backlog.md#s127) | Phase 16.1: the taya is a RING and an attacker is a DISC ⚠️⚠️ OPEN, 2026-09-03, branch `ui-redesign` | Accessibility and role readability implemented; current chat-clip/device/final checks remain in127.3. |
| [§ 88](TODO_Backlog.md#s88) | Accounts and identity ⚠️ IN PROGRESS 2026-08-31 | Account/project/service setup implemented; current isolated startup fix checked. Historical relink blockers superseded. |
| [§ 89](TODO_Backlog.md#s89) | The profile, the stats and the match history ⚠️ IN PROGRESS 2026-08-30 | Career/history implemented; old FUTURE phase allocations retired. Current PlayerHub and P7 govern acceptance. |
| [§ 126](TODO_Backlog.md#s126) | The full PlayMode suite had never been run on this commit, and it was 42 red ⚠️⚠️ 2026-09-03, branch `ui-redesign` | Historical failures resolved or superseded; current full regression and physical-device limits remain P7. |
| [§ 121](TODO_Backlog.md#s121) | The v61 report: one material for the primaries, a hub with a tab column, and the stuck hover ⚠️⚠️ OPEN, 2026-09-02, branch `ui-redesign` | Historical layout/palette recipes superseded by current UX-1/VISUAL-1. Surviving functions and current acceptance remain mapped in UX-1. |
| [§ 119](TODO_Backlog.md#s119) | The whole front end is repainted in PAPER, and the lobby is rebuilt around the room ⚠️⚠️ OPEN, 2026-09-01, branch `ui-redesign` | Historical layout/palette recipes superseded by current UX-1/VISUAL-1. Surviving functions and current acceptance remain mapped in UX-1. |
| [§ 118](TODO_Backlog.md#s118) | The lobby is coherent now and it is not finished ⚠️⚠️ OPEN, 2026-09-01, branch `ui-redesign` | Historical layout/palette recipes superseded by current UX-1/VISUAL-1. Surviving functions and current acceptance remain mapped in UX-1. |
| [§ 96](TODO_Backlog.md#s96) | OPEN: he has never found the way into the hub ⚠️⚠️ | superseded by the implemented lobby player-card/profile door; native UI route qualified, user discoverability judgment remains |
| [§ 95b](TODO_Backlog.md#s95b) | OPEN: nothing asserts that a menu label fits, only that it is legible ⚠️ | Current capture gates assert label fit/rendered characters and bounds. Whole current surface acceptance remains UX-1.11/P7. |
| [§ 72](TODO_Backlog.md#s72) | Two lobby controls reported dead that every headless check says are alive ⚠️ OPEN | Old controls replaced by current profile/JOIN routes; migrated caret/code checks pass. Native physical typing remains P7. |
| [§ 68](TODO_Backlog.md#s68) | The lobby is a form, and it should be a room ⚠️ OPEN, PLANNED 2026-08-28 | Old PUBG lobby design superseded by owner UX-1. Native code/reconnect and arrival/rematch pass; remaining current peer checks stay P7. |
| [§ 69](TODO_Backlog.md#s69) | The game has no chat, in the lobby or in a match ⚠️ OPEN, PLANNED 2026-08-28 | Chat implemented with host limits and input isolation. Current both-direction native chat and 127.3 clip follow-up remain P7. |

<a id="earlier-execution-index-and-supporting-reasoning"></a>
Earlier startup notes (September 15 demo queue, reserved lanes, the old execution index and
"how this file stays short") were already pointer stubs to
`reports/presentation-pass-2026-09-21/planning-intake/TODO-startup-history.md`; the stubs
now sit at the top of [TODO_Backlog.md](TODO_Backlog.md).

## The archive index

- **152.7, CLOSED 2026-09-12:** owner-view spatial composition and retrieval-route batch, with original Bayan passenger tricycle and repeatable authoring. Full entry in [TODO_Archive.md](TODO_Archive.md); complete maps/graphics/new-map/game scope remains under152.4.
- **152.6, CLOSED 2026-09-12:** isolated under-guideway flight/direct-carrier release correction and saved-map semantic repeatability. Full entry in [TODO_Archive.md](TODO_Archive.md); broader map/game qualification remains under152.4.

- **147.3, CLOSED 2026-09-10:** one observed result-board moment, without score changes; reader and clearing verified. Whole entry in [TODO_Archive.md](TODO_Archive.md).

- **149.7, CLOSED 2026-09-10:** duplicate protocol assertion removed; the exact compiled owner remains in ChatAndLobbyChromeTests. Whole review in [TODO_Archive.md](TODO_Archive.md).

- **151.6, CLOSED 2026-09-10:** Trip hazards now have their own Balance.ConfinementRadius clearance, separate from the unchanged 1.4 m generic-prop clearance. Current Checks.RunAll reports all eight checks passed. The first patch accidentally changed the generic-prop loop; the geometry gate caught it and that loop was corrected before this checkpoint. Whole entry in [TODO_Archive.md](TODO_Archive.md).

- **151.21, CLOSED 2026-09-10:** PersonSwapProbe now tests face ink, facing and head-weighted dye on the named Zack reference while the naked base retains rig, clip, height, palette and hand-anchor checks. The current probe reports RESULT: PASS in Logs/person-swap-finish-v2.log. Face/hair assertions were retained. Whole entry in [TODO_Archive.md](TODO_Archive.md).

- **151.15, CLOSED 2026-09-10:** Added audit_positional_audio.py: all 21 direct positional producers carry explicit ownership/reach classifications, with new or stale sites rejected. Private motor confirmations are local 2D cues; jump/land use NetCue. Fourteen gating source audits pass. Live network verification remains part of section 152.4. Whole entry in [TODO_Archive.md](TODO_Archive.md).

- **151.18, CLOSED 2026-09-10:** Removed the two unregistered person assets and made RosterBookBuilder reject new unregistered person filenames. All twenty intended roster entries remain, including the inaccessible maker rigs. Historical root names were preserved. Roster rebuild and exact arm-geometry tests pass. Whole entry in [TODO_Archive.md](TODO_Archive.md).

- **151.23, dedicated first-person retrieval slide, CLOSED 2026-09-09:** own low
  reach and recovery, authoritative transition into carry, versioned action evidence
  and focused successful/failed tests in [TODO_Archive.md](TODO_Archive.md).
- **151.22, Sean retrieval-slide strip review, CLOSED 2026-09-09:** kept unchanged;
  five judgments, corrected hand-height record and evidence limits in
  [TODO_Archive.md](TODO_Archive.md). Full task 3 remains open for match review.

One row per section that now lives in [`TODO_Archive.md`](TODO_Archive.md). Same numbers,
whole bodies, nothing deleted. **This table exists so that a pointer written anywhere in the
repository still lands on something**: follow it here, find the number, read it there.

| § | What it was |
|---|---|
| 150 | The camera/feel/lifecycle pass: a hitstop that drifted 11.9 m, a bearing nobody passed, and an audit blind to its own worst case ✅ CLOSED 2026-09-06 by § 151. ⚠️ Its four open halves each have a successor: § 150.7's listener is § 151.1 and § 151.2, the jeepney is § 151.5, the sweep is § 151.9 and the maximum-effects measurement is § 151.8 |
| 93 | A held tsinelas "drifted" 0.084 m from the hand ✅ CLOSED 2026-09-05. ⚠️ It was never a carry regression: `CarryTests` subtracted `RestHeight` and not the `DrawnCentreOffset` that `RideAnchor` also applies, so it measured half a shoe and called it slack. The bound is unchanged at 0.05 m |
| 137 | The two-process harness § 135.7 said did not exist, and the tables it was blocking ✅ CLOSED 2026-09-04. ⚠️ Read § 137.2 before reaching for `UnityTransport`'s simulator: it is `[Obsolete]` with no effect here. Closes § 135.6, § 135.7's buildable half, § 136.4 and § 134.9 |
| 136 | F1 did three things at once in practice, and the whole `ui_*` sound family went back ✅ CLOSED 2026-09-04. § 136.4's touch control is built in § 137 |
| 135 | The tournament network pass: the baseline, and the three verbs that refuse in silence ✅ CLOSED 2026-09-04. ⚠️ § 135.7's premise about the harness is corrected in § 137.1; its two HUMAN-blocked parts are in `Attention.md`, not here |
| 131 | The suite became a gate, the tutorial got its glyphs, and a red that was never about steering ✅ CLOSED 2026-09-03 |
| 129 | Three faults off the first phone render, and the one that was invisible on a monitor ✅ CLOSED 2026-09-03. § 129.3's mechanism is § 130.9 |
| 90 | The impersonation guard, and telemetry ⚠️ 2026-08-30 |
| 91 | Phase 4: XP, levels and hero mastery ⚠️⚠️ 2026-08-30 |
| 92 | The account and career screens, rebuilt ⚠️⚠️ 2026-08-30 |
| 94 | Phase 4.5: quality control across phases 1 to 4 ⚠️⚠️ 2026-08-30 |
| 125 | Controller, touch and crossplay, built so that forgetting is impossible ⚠️⚠️ 2026-09-02, branch `ui-redesign` |
| 124 | The skills are aimed and drawn in their own hand, the tutorial stopped lying, and Zack stopped being Sean ⚠️⚠️ 2026-09-02, branch `ui-redesign` |
| 123 | The match settings go back to steppers, the shadow was retuned on the wrong axis, and a tab pair sat at half its neighbour's contrast ⚠️⚠️ 2026-09-02, branch `ui-redesign` |
| 122 | The black line everywhere, the picker goes back to wood, and the loadout moves to the hero ⚠️⚠️ 2026-09-02, branch `ui-redesign` |
| 120 | The buttons get a thickness, and the four screens § 119.11 left get finished ⚠️⚠️ 2026-09-02, branch `ui-redesign` |
| 117 | The front end was two design systems stacked, and the code-drawn one was the wrong one ⚠️⚠️ 2026-09-01, branch `ui-redesign` |
| 116 | The front end had one material and no focus state ⚠️⚠️ 2026-09-01, branch `ui-redesign` |
| 115 | Eight faults in one build, phases 11 and 12, and the door he could not find ⚠️⚠️ 2026-09-01 |
| 114 | The boot is four screens, the lobby is the home, and the colour dial is deleted ⚠️⚠️ 2026-09-01 |
| 113 | The clothes were not clothes, the screen was see-through, and the door was a chip ⚠️⚠️ 2026-09-01 |
| 112 | The base rig is naked now, and the custom character walks into a match ⚠️⚠️ 2026-08-31 |
| 111 | The build he opened: no studio mark, and the boot screen in the wrong unit space ⚠️⚠️ 2026-08-31 |
| 110 | The character maker gets a wardrobe, and the custom hero borrows a kit ⚠️⚠️ 2026-08-31 |
| 109 | Phase 6's last mile: the three-hour hang, and the presence state nothing had ever lit ⚠️⚠️ 2026-08-31 |
| 108 | The custom character had no screen, and two screens were drawn under the screen that opened them ⚠️⚠️ 2026-08-31 |
| 107 | Roster Integrity and the 3-Slot Custom Character Creator ⚠️⚠️ 2026-08-31 |
| 106 | Phases 5 and 6 finished: the free colour dial, and parties as queue tickets ⚠️⚠️ 2026-08-31 |
| 105 | Phase 9: one ladder, five tiers, Glicko-2 ⚠️⚠️ 2026-08-31 |
| 104 | Phase 8: the witnessed result, and the finding that the plan's design would have been theatre ⚠️⚠️ 2026-08-31 |
| 103 | Phase 7: QUICK MATCH as a rating-banded queue ⚠️⚠️ 2026-08-31 |
| 102 | Phase 6: friends, presence and blocking ⚠️⚠️ 2026-08-31 |
| 101 | Phase 5 continued: the banner on the wire, palettes on remote seats, and the colour picker ⚠️⚠️ 2026-08-31 |
| 100 | ⚠️⚠️ THE BOOT SCREEN'S ART WAS FITTED TO A FRAME NOBODY CAN SEE, AND THE COLUMN WAS SIZED AGAINST THE WINDOW INSTEAD OF AGAINST THE FORM |
| 99 | ⚠️⚠️ EVERY `sortingOrder` A CODE-BUILT SCREEN SET WAS SILENTLY IGNORED, AND § 92.7'S FIX NEVER WORKED |
| 98 | Phase 5 begins: the banner, and wiring the rewards nothing wore ⚠️⚠️ 2026-08-31 |
| 97 | The boot account screen, PUBG-shaped, with the guest escape ⚠️⚠️ 2026-08-31 |
| 95 | ✅ CLOSED: the four title-screen buttons overflowed their own artwork at 720p |
| 95c | CLOSED: the loading screen was a black rectangle for most of the boot |
| 71 | The 2026-08-29 report, and the two faults only a non-host could see |
| 73 | The rest of the 2026-08-29 batch: feel, audio, and the casts nobody could tell apart |
| 74 | Zack's shock trail has the hazard bug that was fixed everywhere else ✅ CLOSED 2026-08-29 |
| 75 | The slipper throw wind-up, and what was actually checked ✅ CLOSED 2026-08-29 |
| 76 | Holding the pickup key does not right the can in the tutorial ✅ CLOSED 2026-08-29 |
| 77 | The network deep-dive: the half of § 71.3 that was never applied, and a refusal that was never sent ✅ CLOSED 2026-08-29 |
| 78 | The two-machine acceptance test, run at last, and the batch it paid for |
| 79 | The 2026-08-29 evening batch: what he reported, what landed, and what is still open |
| 81 | ⚠️⚠️ THE PLAYMODE ARENA SUITE IS NOT A GATE ANY MORE, AND HERE IS THE EVIDENCE |
| 80 | The 2026-08-29 late batch, reported while § 79 was being fixed |
| 82 | The 2026-08-29 night batch: the match that was over before it started |
| 83 | The 2026-08-29 balance-and-controls batch, reported while § 82 was being pushed |
| 84 | The 2026-08-30 batch: twelve reports off the shipped build, and a lighting number read off a dead field |
| 85 | The 2026-08-30 AUDIO and VISUAL list, sent as one block |
| 86 | The spectator pause, and the 35 ms every non-host was standing behind |
| 87 | Every tsinelas rendered flat brown in first person, and the fix for it flattened the shading on all of them ✅ FIXED 2026-08-31 |
| 0 | Hero Strike is being reworked, and the plan is its own file |
| 8 | The abilities still look repetitive, and half the fix is not done |
| 9 | Ilalim ng Tulay dressing defects, reported off the 2026-08-25 player |
| 12 | Everything 🧑 found playing the 2026-08-26 build ✅ ALL CLOSED SAME DAY |
| 13 | Everything the 2026-08-26 evening build showed, and the pattern in it |
| 14 | The 4.69 player's second batch, shipped in `349b0171` |
| 15 | The 4.70 tutorial batch, and why four screenshots were one probe apart |
| 16 | The probe was never deterministic, and § 10 was closed on an argument |
| 17 | The bots are steeply sensitive to the frame step, and a 50 fps machine is in the bad band |
| 18 | HUD strings overflow their boxes, in more than one place |
| 19 | The powers were fifteen poses sharing one construction, at every layer |
| 20 | Cheska's kit played the wrong sounds, and every zone died in silence |
| 21 | Phaister merged in, and everything she arrived without |
| 22 | Everything the 4.71 player showed, and the two entries that were ticked but not wired |
| 23 | Ability stuns are now fought out of, not waited out |
| 24 | Phaister's three powers were one builder at three radii |
| 25 | Which peers actually hear a sound, measured rather than assumed |
| 26 | Every ultimate changes the weather, and each hero changes it differently |
| 27 | The other five heroes need a motif, and it is not more symbols |
| 28 | Nemu's ultimate is her pet now, and her kit is named after him |
| 29 | The other four heroes got their motif, and none of them shares a builder |
| 30 | Two findings from measuring the cue files, and one stale line in `CLAUDE.md` |
| 31 | Everything the 4.72 playtest reported, and the two faults it exposed |
| 32 | The networking was broken by one unreplicated static, and four other faults on top of it |
| 33 | The bots picked a target by seat number, aimed powers at rings they do not cast, and had no keyboard between decisions |
| 34 | Seat 0 was steered by a different movement model in every all-bots run, and it is § 11's second layer |
| 35 | The spectator flies itself, every key is in the panel, and a reconnect stops refunding cooldowns |
| 36 | The host never transmitted its own bodies, so a joiner saw three statues |
| 37 | Two Phaister presentation faults from the 4.72 player ✅ CLOSED, SEE § 43 |
| 38 | The network pass: eleven faults the host cannot see, and the loopback behind four of them |
| 39 | The settings wheel, for the fourth time, and the cause the first three missed |
| 40 | The train is one field recording now, and it plays rarely |
| 41 | The ultimate meter counts events now |
| 42 | Nemu's ride home was being erased by her own body's bot |
| 43 | Two Phaister presentation faults, and a class of fault behind one of them |
| 44 | § 32.3's slider fix was muted by the sweep on the next line ✅ CLOSED 2026-08-27 |
| 45 | The in-match HUD had five ambient sines, three copies of "LATA DOWN" and twelve coloured cells |
| 46 | Both intermission banners were drawn on top of something ✅ CLOSED 2026-08-27 |
| 47 | `Checks.RunAll` has been red since the Phaister merge, in two places |
| 48 | Kuro's projected body deleted itself mid-ability, and took Nemu's way home with it |
| 49 | Seat 0 travels about half what seats 1 to 3 do, in Classic, every run |
| 50 | Fourteen reports off the 4.73 player ✅ CLOSED 2026-08-27 |
| 51 | The four follow-ups off § 50 ✅ CLOSED 2026-08-27 |
| 52 | The ready and rematch gates counted a seat as a peer, and five guards allocated before they guarded |
| 53 | A joining client could not move, and the cause is that its keyboard was left on seat 0 |
| 54 | Which of the two lobby fixes was kept, and why |
| 55 | The lobby was a picture of a lobby ✅ CLOSED 2026-08-27 |
| 56 | What the merged network pass still leaves open |
| 57 | The match ends on one machine, and three other events never reach a client at all |
| 59 | Two machines could discover each other and could not join, and it is one missing string split |
| 60 | The host announces a seat twice, by two protocols, and only one of them does the job |
| 53 | The corner stamp is the branch name ✅ CLOSED 2026-08-27 |
| 62 | Losing the host left a client playing on alone, and § 60.1 did not fix the movement |
| 63 | A game could be joined exactly once per launch, and remote bodies never animated |
| 64 | The bots had no face, no feet, a perfect memory and one opinion |
| 65 | Hosting or joining a SECOND time in one launch was refused, silently |
| 66 | Joining bounced the view about, and rejoining a running match was impossible |
| 67 | What the HARRYDAKS merge was hiding, found by building it |
| 70 | The prop art was replaced wholesale, and IKE was never a bad model ✅ MOSTLY DONE 2026-08-28 |
| 1 | Peer rematch voting across the wire |
| 2 | Cheska's Ice Barricade duration was set by accident ✅ CLOSED 2026-08-25 |
| 3 | The five hero accents have not been seen in a real match |
| 4 | Bayan Plaza's monument stands inside the defender's box |
| 5 | The overclock window has not been measured against a match |
| 11 | Every probe number ever printed was an average over a seat that could not play ✅ CLOSED 2026-08-26 |
| 10 | `BotBehaviourProbe` cannot answer a comparison, and every open balance question is one |
| 6 | `AiDiagnosticProbe`'s Classic round is a real-time test and it flickers red |
| 7 | The test suite costs more to run than it is currently returning |
| 58 | The ink outline tore open at every hard edge ✅ FIXED 2026-08-27 |
| 63 | The world outline was aliased because MSAA was never able to see it |
| 63 | Walking into a utility pole blanks half the screen, and it now dithers away |
| 64 | The player can switch render styles, and the alternative is a chromatic look |
| 65 | The white keyline round every silhouette, measured rather than argued |
| - | Closed |

- **152.8, CLOSED2026-09-12:** preserve transparent/cutout scenery during near-camera fading. Whole entry in [TODO_Archive.md](TODO_Archive.md); [evidence](reports/improvement-2026-09-12/near-fade-alpha.md).

Latest owner ability feedback,2026-09-13: current abilities still look poor and
feel too similar. AFTER MAPS, fully revamp and improve their implementation,
mechanics/purposes and complete casting/body/FPP/moving geometry/VFX/impact/SFX.
All six heroes/defaults/alternatives, within existing slots and simple controls.
This is not a recolor pass. Existing map corrections remain first; the other
movement/equipment/network/graphics/TODO scope remains open.
# Deferred by owner, 2026-09-14

- [x] Finish Inday's FPP arms. Superseded and completed2026-09-22: both original
  Inday models and both FPP routes now use plain brown arms with simple hands,
  per the latest owner correction. Published e50f8c37; source/rig/animation audits,
  quick/held/moving casts and nativev37/v39 views pass. Original arm backups and
  earlier failed studies are retained. The following describes the old candidate,
  not unfinished work: owner explicitly moved this below other
  project work. Keep her actual restored left/right arm meshes, not the rejected
  reconstructed guards or red mittens/purple bands. Source-copy v1 passed the
  character-switch and quick/held/moving throw checks, but its sleeve ends showed
  in the carrying camera. Uniform full-reach source-copy v2 is the pending framing
  candidate and has not been visually reviewed. Current author evidence is in
  Logs/inday-source-arm-author-v2; prior motion in Logs/inday-source-arm-motion-v1.
  Retain existing animations, source proportions, material and character palette.
  Resolve framing and remove obsolete reconstruction code/assets when resumed.
