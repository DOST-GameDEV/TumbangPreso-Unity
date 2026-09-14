# Active TUMP rework ledger

## Current task and process

Checkout: C:/Users/matth/Documents/GitHub/TumbangPreso-Unity-ASTRAReworks.
Branch ASTRAReworks, latest COMMITTED AND PUSHED HEAD:
95a707131327d38fd451eee821955be23d393dba, Cheska Nova grounded-slipper flight.
Earlier36805af4ddd38bec84029c3e6514b0e0c5e57553 saves Phaister placement/alternate checks and truthful copy.
Earlier5cfb04b3c44e9a14368a0afeb390c7696144a06f restores live Coven/current sky on rejoin.
Prior pushed checkpoints: 06b54470 construction cost; 6a1811c7 Phaister timing/forms/
rejection; c59cef8f origins/concepts; e6237a0d Dante; bc8a5f00 and b106c6ac preserved.

NEXT WORK: Sean's current kit needs the authored pass. Baseline capture FINISHED
1/1PASS, Logs/sean-current-kit-v1.xml, receipt007b8193637b. All3real presses accepted;
that is functional coverage, not art approval. Source is unchanged for Sean.
Current owner/body videos are encoded at recorded times; contact sheets inspected.
Observed: Ignition has a pink BoltHead symbol and large square particles in FPP;
Supernova floods the scene red with a generic column/early ground overlay; its
body returns to holding pose while airborne. Rush repeats ornate floor marks.
Source still uses35%dash stretch,45%launch stretch,40%landing squash; the existing
art plan rejects that rubbery scaling. Ignition's hand aura has a fixed10s life
and its handle is not stored; investigate actual throw consumption/cleanup, not
only the buff press. Baseline capture did NOT throw the empowered slipper.
Supernova has an.85s impact timeout after the.55s airborne phase; reproduce an
airborne/low-ceiling case before changing landing logic.
Preserve the retained Sean model/outfit and distinct leap/rush/empowered-throw jobs.
No Sean production edits yet. No active Editor/player/helper or image job.

Completed Cheska Nova correction. Her prior deep ice art/animation
pass is already committed and should be preserved; reports/improvement-2026-09-10/
cheska-kit.md and earlier ledger record6/6collision/cast checks,4/4traction and
matching native wall/prison meshes. Those screenshots predate current maps/hands.
No new Cheska art redesign is justified from stale screenshots.

Source hypothesis: Nova calls Slipper.Deflect for every nearby slipper; Deflect
changes velocity but never changes Loose->InFlight. A grounded slipper therefore
may not move despite the skill's copy. Existing Dante correction handles this by
HostThrow(null,...) for Loose and Deflect only for InFlight. Do not change the
shared Deflect contract blindly or affect held/returning equipment.

New CheskaNovaSlipperProbe.cs/meta drives real Nova input and checks nearby loose
flight/outward movement, retained held shoe and an outside loose shoe. Initial
run compiled no tests because the fixture omitted using System for FormattableString;
fixed the import, no production code changed. That run is NOT a gameplay baseline.
Actual baseline-v2 FINISHED1failed, profilec00f86bc88a9. accepted=True,
launched=False,outward0,held_same=True,outside_move0. CSV preserved as
Logs/cheska-nova-slippers-before.csv. This confirms the state-transition defect.
Nova now uses HostThrow(null,...) for Loose and Deflect only for InFlight;
held/returning equipment untouched. Preserves speed19 and effective lift1.1;
zero planar distance falls back to caster forward. No shared Deflect change.

Correction/capture run FINISHED2/2PASS, receipt63cbbe13f33c. Python828 exited.
Logs/cheska-nova-flight-v1(.pid/.stdout/.stderr/.log/.xml).
Two tests: NovaLaunchesNearbyLooseSlippersAndKeepsHeldAndOutsideOnesSafe and
EveryHeroActionThroughTheRealPressAndRelease, filtered to Cheska via child env.
TUMP_EVIDENCE=Logs/cheska-current-kit-v1, TUMP_REVIEW_ISOLATED=1.
CSV: acceptedTrue,launchedTrue,outward10.26m,heldsameTrue,outside0.
Fresh owner sheets for all3actions inspected; retain existing distinct ice art
and current hand poses. Videos encoded at recorded times under the evidence path.
INTERNAL build FINISHED1057MB/50s, receiptad0a4e72b9e3:
Logs/cheska-flight-build-v1 (.pid/.stdout/.stderr/.log),
explicit Builds/CheskaSkillReview/TumbangPreso.exe. Read pid file for active helper.
All Editor/test helpers exited. Preserve prior good ice art. This scoped
correction is pushed as95a70713. Review Sean's current three actions and actual empowered throw.
Sean still has45%stretch/40%squash and possible timeout-driven airborne impact;
gather current ordinary-speed evidence before changing those paths.
Report reports/improvement-2026-09-14/cheska-nova-flight.md.

Prior placement run FINISHED2/2PASS, receipt e2c9bcbde50e. Logs/phaister-placement-variants-v1
(.pid/.stdout/.stderr/.log/.xml). Two new actual-input PlayMode checks:
HexAndSlowBrandApplyTheirActualFootprintsAndPulseRates and
BothBlinkVariantsWaitForReleaseAndShoveOnlyAtDeparture.
Source copy corrected in PhaisterHeroKit and core HeroLoadout descriptions;
no Q/E mechanics changed. That checkpoint is already pushed as36805af4.

Expected evidence: Logs/phaister-hex-variants.csv and phaister-blink-variants.csv.
Check fresh nonzero XML, exact tests and actual values. The tests unlock only
their named fixture variants in memory and restore prior challenge rows.
They disable AI/input readers, use real held/released verbs and run in Bayan.
Hex compares actual radius, repeat pulses, strength, edge victim, owner exclusion
and charge. Blink compares hold/release, default versus Long Stride distance,
departure shove versus arrival safety and cooldown.

Actual Hex: radius2.4,2pulses,peak.35; Slow Brand1.44,3pulses,peak.49.
Edge target hit only by default; owner safe; one charge spent each.
Blink travels5.5/7.15m, departure target moves.573869m, arrival target0;
both wait for release and retain51.7s cooldown. Copy no longer says one stumble
or an impassable ward. Long Stride ramp.55/.7=.785714s vs default.55s.
Blink shove is at DEPARTURE; cultural draft corrected too.
Next is the focused Cheska investigation above. Broader overlap/defender counterplay and other persistent fields'
rejoin behavior remain open; do not claim all game networking or all art done.
Do not add speculative collider deduplication without a real duplicate-hit case.

## Standing owner scope and boundaries

Continue the whole project here while AFK. Do not stop at one batch or a handoff.
No agents/subagents/other chats, Figma, resets, paid API work or Desktop update.
Built-in image ideation alone was explicitly authorized. No exact image model
version is exposed, so do not promise GPT Images2.5.
Only necessary related tests, no routine full suites. Parallel independent tools/
preparation and useful work while runs proceed. One Editor at a time, every launch
through tools/run_unity_guarded.py, profile equipment-destination-review.
Explicit INTERNAL buildOutput; no C#/imported edits during Editor runs.
Unity6000.5.8f1 at C:/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe.
Guard repairs missing ALLUSERSPROFILE only in child env, restores/hashes named
profiles and shared Editor input preferences. Other dirty main checkout untouched.
Commit via message file, fetch/inspect before push, ASTRAReworks only.
No new em dashes or AI/coauthor trailers. Restore only byte-proven import dirt.
Use rg directories plus -g patterns, never wildcard path operands on Windows.
No browser tabs were opened. No image jobs or old player helpers remain.

Skills must be authored powers with distinct forms, silhouettes, motion, staging,
impact, sound and recovery. No pasted texture/drawing repeated across a kit or
every component. Shared theme/code is fine. No identical casts/ult movements
across heroes. Check body, FPP and actual props together. No fingers or thumbs.
Keep the retained cast/outfits except the explicitly restored old Inday body.

Dante: clean demonic mountain/stone direction; noisy texture was rejected and
removed. Fitted marks now differ by plate, including plain areas. Three orbiting
shields approved; later optional refinement allowed only if worthwhile.
Inday FPP actual-source copy v2 is DEFERRED. Do not resume it as immediate work.

## Origins and concept choices

CHARACTER_ORIGINS.md contains six original fictional biographies, short select
lines and sources. Zack: Pasig/Sa Bubong condo. Cheska: La Trinidad,Benguet.
Sean: San Fernando/Eskinita. Dante: Montalban/Bayan. Nemu: Dumaguete/off-screen
home court. Phaister: Capul, now practices at Ilalim near Gilmore.
Do not relocate urban maps into Benguet/Dumaguete or assume an ethnicity from
birthplace. Existing six names/IDs retained; names may change when natural.
Short/long lore UI is NOT wired yet, queued in UI_REMAINING_TODO. Six heroes are
already available; do not invent paid locks to manufacture an unlock screen.

BADJAO_EXPANSION.md: seventh male Sama Dilaut hero, working name Rafi, with
Crosscurrent, Mirrorwake and Breakwater proposals and distinct casts. Gills are
individual fantasy, not real ethnic anatomy. No invented tribal tattoos or mixed
regional regalia. Water-village homes stand on fixed piles; boats float.
Entire hero/map implementation is LAST LAST after the whole existing queue,
including UI and deferred Inday. Research and concept choices only now.

Three image calls finished, files/prompts/critique/owner photo preserved in
ArtSource/badjao/concepts-2026-09-14. First smooth/realistic hero sheet rejected
internally. Corrected blocky sheet used the approved Classic cast reference.
Map sheet is composition exploration, not approved final material detail.
Two async choices still pending, no owner selection:
Hero A cropped/indigo athlete, B tied-hair/green boatcraft, C shaggy/white trickster.
Map A neighbourhood boardwalk, B community jetty, C sheltered lagoon.
Do not implement an assumed choice. Originals remain in .codex/generated_images/
01a09d13-63f5-7f20-a750-d0de43358454 (see prompts/README for filenames).

## Phaister checkpoints, preserve rather than redo

Reports: reports/improvement-2026-09-14/phaister-skills.md and
phaister-rejoin-state.md. Sibling phaister-evidence preserves all failures/passes.

Timing/forms: actual1.55s preparation, curse after warning, moon7.33m under
Ilalim8m ceiling, correct10.5m boundary and zero grounded-mark drift.
Baseline was instant curse.00246s, moon10.98m, boundary1m, drift.38492m.
Hex now compact crescent binding; Slow Brand tight binding. Blink has vertical
departure tear and brief flat arrival fold. No generic Phai aura/flash/column.
Six controlled floating glyphs, staged opacity, fixed ground, actual curse pulses.
Only hero-phaister-eclipse input times retimed in phaister/custom/custom-base
GLBs; original binary chunks and other35/50/50clips preserved. Contact1.55,end2.12.
FPP right-arm hold keeps Loafer clearer. No hand/outfit geometry change.
Contact sheets inspected, recorded-time videos encoded; no human feel approval.

Tests: staged4/4 (8ea96f85b60c), distinct-kit2/2 (feb7a349677b),
moving-caster1/1 (70702cad619b). Moving-centre suspicion was DISPROVED:
HeroAbility already stores committed AbilityContext, so a6m relocation does not
detach the curse from its warning. Do not duplicate that pose mechanism.
18 exported hero casts have no exact cross-hero rotation-channel duplicates;
hash differences do not establish good/distinct choreography.

Performance: cache collider classification only during one synchronous ground
projection; same rays/exact-XZ cache/precedence/rise limits. Warm projection mean
12.10->5.20ms, tested changed court naming/new rigidbody between calls.
Share identical RGBA/emission only within one ritual reveal layer and instance.
One material writer per layer; first renderer owns it and all pieces die together.
No global material, renderer merge or glyph simplification.
Material setup18.27->6.63ms, total repeated construction41.66->32.12ms.
139renderers/20705vertices unchanged. Mesh SHA:
53f1bb05633930e13090246a1f02d03e5524ce2066bfba79050a0df4766db4c7.
Six-phase colour/emission SHA:
0076df0d32edd13eaaac6649672dd97cec96349a7ec9ebcbb88d1eed95fe39ef.
Projection and stage-material checks3/3 each; detailed receipts in report.
These are Editor measurements, not player FPS or cold-start equivalence.

## Phaister networking: verified and limitations

Rejected prediction originally removed circle but leaked owner sky for96samples.
Now hands/circle predict immediately, global sky waits for trusted host acceptance.
PlayAbility owner echo confirms presentation only, never replays/spends the cast.
Pending flag clears on denial/newkit/reset. No rollback StopAll that erases a rival sky.
Actual accepted and rejected delayed3peer cases pass; named profiles restored.

Correct selected-character rejoin baseline v3: host/owner kept ritual, returning
observer had correct Phai but no circle/clock/sky through20live-window samples.
Fixed by targeted reliable CovenEffect and SkyEffect AFTER picks/rebind/world
snapshot, following FamiliarEffect. Server contact/end/send times remove transport
age. Restore preparation/active state without spend/replayed hits; proper root/
expiry. Duplicate running restoration ignored. Sky resumes the actual current
winning look silently, not always a new eclipse. Only host resolves outcomes.
Protocol31 required. Older30player refusal tested; Desktop/Android unchanged.

Local restore2/2 (b2146266f552), protocol pin/queue2/2 (4441748062df);
wire audit68messages/0mismatches. Actual rejoin-v6 PASS:
511host/503owner/139returned samples;25active returned,22inside host-live window.
Reconstruction135.94ms, continuous state, proper expiry. Maximum remaining-clock
difference126.17ms on25matched samples. Actual old-client refusal PASS31vs30.

Important fixture lessons:
- Kit-only binding left the real arena pick inconsistent; rejoin correctly
  reconciled away from Phai. Do not "fix" working same-hero UpdateLoadout.
- CLI arena is preloaded before clients join. Fixture selects via real room RPC,
  initializes accepted choice once on host through SyncPicks/BroadcastPicks, and
  requires CharacterIndex+kit+room agreement. Returning peer never seeds/rebinds.
- Owner meter seeded before the press could be erased by a real snapshot; seed
  on the press frame. The no-cast v2 rejection run was invalid and failed.
- Outside targetx12 was clamped inside arena/curse; use z7,15m from cast centre.
- World/effect/sky are separate reliable messages. Require reconstruction within
 250ms, then continuity, not impossible same-sample arrival. Old no-state trace
 still fails. Preserve86ms/73ms initial-delivery failures and analysis.
- SecondsSinceAnswer is local UI feedback, absent on host remote casts. Probe
 uses existing UltimateStarted event on every peer, balanced subscriptions.
 Event-to-active1.618656host/1.570118owner passes1.50-1.85s. First sampled warning
 is a different boundary and remains reported. No full-visible1.55s guarantee.

Current INTERNAL player: Builds/PhaisterSkillReview/TumbangPreso.exe, v9,
1057MB/62s, receiptd95ddb75293b, protocol31. Runtime SHA:
aa79cdc4fb45da2dbb70b15d23fc93a112c506f4c4b3bdeee6830944ca1dd231.
Build metadata predates the commit but runtime source matches5cfb04b3.
All former player/proxy/build sessions ended. Only current placement test runs.

## Earlier completed work and remaining queue

Dante e6237a0d: distinct Q/E/R, .30s contact, Long Tremor, fitted8plates+3orbiters,
two hooked solid fissure faces and restrained2.4s quake without aim rotation.
Five actual delayed3peer cases pass. Current Dante internal v4 has final fitted
marks; see dante-skills.md. Noisy texture stays removed.
Earlier8.13mshoe movement was TELEPORT from initial-overlap SphereCast point0,
not flight. Local-contact correction preserves rebounds; properQ shoe10.79999m
OUTWARD. Do not reuse invalid movement as proof.

bc8a5f00: equipment9point/non-domination roles, actual can rebound/pickup settle,
short general-direction throw guide separate from movement/early-hold error.
All18x10x6x3 actual head-surface carry checks pass. Zack substantial thumb-free
hands, matching18FPP details and hue-preserving colour grade.
Old Inday body restored from backup4 82524c7537fc5fcff00ebb845ee4c360acd468cb.
Old mini Nemu restored+11expressions, transforms into retained current monster.
Monster digestef5a0d18520256b81d287bd191c6bda64f945e9c86c51b71b00fbd957fc44373.
Inday actual-source FPPv2 scale2.961006 is unreviewed/deferred.
b106c6ac: backpedal reversal/shared gait,6focused checks+motion pass.
Older DestinationEquipmentReview predates backpedal/Dante; its4throw/familiar
peer checks passed, including delay/rejoin. Never call that a current binary.

Remaining: finish Phai Q/E/alternates and honest copy, then whole6kits/18skills+
alternatives preserving good work. Other persistent hazard rejoin state remains.
Strafe/turn/start-stop/foot contact/recovery/mash/interruptions/Pektus/input.
Actual rematch/reconnect/host loss/loadout; spectator free/follow/POV authority,
camera collision/framing, highlights/replays; TODO152/152.4 performance/AI.
UI LAST, including new lore. Preserve pending-held-info.patch (unapplied),
original logo/fullpalette/Darumadrop and approved controller18callouts/mapping.
Known pause-child-settings discard/return failure. Inday later.
Seventh hero/water map LAST LAST, after every existing task.

Memory notes were used; eventual final response needs ONE citation block LAST:
extensions/ad_hoc/notes/2026-09-14-distinct-tump-skill-art.md:3-14 and
extensions/ad_hoc/notes/2026-09-14-tump-shield-permission-update.md:3-7.
Memory base C:/Users/matth/.codex/memories. Empty rollout_ids allowed.
No final/handoff requested. All225portable skill files verified; skills installed.
Complete preceding ledger archived as reports/improvement-2026-09-14/
ledger-before-phaister-placement.md. Other detailed historical ledgers/reports
remain intact. Compaction is continuation, not a fresh start or a reason to stop.
