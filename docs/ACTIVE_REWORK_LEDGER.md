# Active TUMP rework ledger

## Boot menu activation barrier, 2026-09-27

The splash now retains its existing artwork/canvas through actual MainMenu
activation, native home/login construction and the first layout/draw frame.
Readiness is signalled only after menu wiring succeeds, not at scene-load90%.
Hidden title controls are gated through CanvasGroup,including the any-key path;
welcome timing,menu music and input are released only on reveal. Failed menu
initialization stays an explicit failure, not a full ready bar. Cleanup retires
the retained owner,canvas and video target. No artwork or input-backend replacement.
Runtime,Editor,Tests and PlayTests compile on the93-file frozen candidate (7changed).
New native handoff/cleanup test is pending; no first-click/hitch-free timing claim.
[Evidence](reports/stability-2026-09-27/loading-audit.md#menu-activation-behind-loading).

## Shared held-aim delivery, 2026-09-27

Protocol66 carries body-aim slot,stable ability ID,elapsed hold and scoped token
through accepted pose delivery. Private targets stay local; remote holds never
become gameplay input. Shared body hooks preserve incoming Phaister cuffs/doll
and authored aim clips while keeping her target sigils private. Cast and ultimate
delivery close the correct hold; newer holds survive old releases. Epoch,lease,
phase,reset and disable cleanup cover stale presentation. No art/timing redesign.
This is shared match transport,including ranked and spectators,not a queue fork.
Runtime,Editor,Tests and PlayTests compile on the frozen86-file candidate; one
new pure token/scope case passes. Native codec/lifecycle tests and actual peer/
ranked qualification remain OPEN under the recorded disk limitation. No old
suite or film repeated. Next: generic boot/menu activation readiness.
[Evidence](reports/stability-2026-09-27/multiplayer.md#held-aim-body-presentation).

## Ranked and casual discovery fixes, 2026-09-27

Online advertisements now carry nonindexed shared skill compatibility; automatic
pairing rejects missing/mismatched contracts or missing endpoints before connecting.
Party capacity uses actual occupied/reserved chairs, while existing backfill,band,
device-pool,rating and leave rules stay unchanged. Failed allocations are suppressed
per lobby/endpoint for30s in a bounded queue-local cache; the cached list is retried
after failure even when discovery/band width stops changing. Relevant advertisement
changes now notify the queue. No extra service query or live account operation.
Four assemblies compile; three new managed cases pass (casual,ranked,retry lifecycle).
Live ranked/casual connection,party and result flows remain OPEN.
[Evidence](reports/stability-2026-09-27/multiplayer.md#ranked-and-casual-discovery-admission).

## Shared rework and recovery integration, 2026-09-27

Integrated prepared recovery `e6fb9afd` with incoming Phaister refinement through
`baec93c9`. Keep protocol65 and the incoming5-second intro; retain all authored
aim clips,props,shaders,cutscene and audio. OMEN's independent screen-space effect
now follows its owning visual's enable/disable/destruction, preventing warmup or
recovery cleanup from leaving an overlay behind. No look/timing redesign.
Runtime,Editor,Tests and PlayTests compile on the frozen73-file integrated source/
asset candidate. No old test suite or film rerun. Incoming7/7 evidence below belongs
to its earlier protocol64 candidate, not this merge. Native/peer/ranked remain OPEN.

## Phaister interrupted publication recovery, 2026-09-27

Recovered refinement commit `5b696b6f8` and completed its interrupted merge as
`f55019fb6`, preserving the body-owned status presenter and the two-sided,
uniformly scaled hex mark. Integrated remote work through `bf0b80db8`.
Protocol 64 retains the shared skill fingerprint; that fingerprint includes
Phaister's 5.0-second introduction, so differing phase durations are rejected.
HERO-10's remaining creative work, including the owner's flat-butterfly correction,
stays OPEN. Unrelated dirty assets, Supernova work and protected UI metadata remain
outside this publication batch. Unity 6000.5.8f1 compiled the merged source and
`ChatAndLobbyChromeTests` passed 7/7 in one guarded EditMode run, using profile
`presentation-validation-20260921`. Local receipts: `Logs/phaister-recovery-merge.xml`
and `.log`. No new film, PlayMode run, player build or real-peer claim.

## Current networking and flow work, 2026-09-27

Owner expansion: ranked is explicitly included, not only custom/LAN matches.
Cover ranked matchmaking,admission,reconnect and spectators as well as shared
match delivery. Preserve rating/result/leave rules and the existing device pools.

Owner clarification: only Paete currently has substantial VFX; other character
presentation is provisional and will be reworked. Preserve it without polishing
or treating current effect classes as permanent network design. Model,clip,VFX
and cosmetic swaps should reuse shared cast/state delivery. New gameplay state
still needs an explicit recovery contract; do not add bespoke RPCs for each rework.

Protocol65 extends world freshness through ultimate cohort ID/stage and pending
owner ultimate requests. Prepared-effect recovery now follows its accepted world batch,
uses simulation-clock preparation/lifetime, preserves height and seeks the original
authored timeline/body preparation. Explicit empty state clears obsolete effects.
Expired newer intros also retire older playback and retain terminal identity.
IPreparedWorldReplication is ability-owned and auto-discovered; transport no longer
names Phaister or her current effect class. Ability-ID matching and per-ability
dedup support multiple such abilities per hero. Three assemblies compile on the
20-file candidate; one new capability-contract check passes. Two prior managed
clock/freshness cases remain recorded without repeat. Native cases stay OPEN.
[Authoring contract](SKILL_NETWORK_CONTRACT.md).
[Recovery evidence and limits](reports/stability-2026-09-27/multiplayer.md#phase-aware-world-and-omen-recovery).

Phaister's existing setup warmup now builds the current visual-only OMEN using its
authored ultimate timings, not the retired Grand Coven. It immediately deactivates
and disposes that visual; no ability,pull,hazard,score or audio path is invoked.
Source/API-call reviewed only. No repeated character film or unchanged suite was
run for this loader correction; no measured first-cast timing claim.

Protocol64 adds a cached skill-contract fingerprint to connection approval. Stable
roster/ability identity, delivery/resource/aim/preparation metadata and shared intro
durations must match; cosmetic names/assets,live timers,current role and locale do
not affect it. Boot prepares the hash after its existing kit/table preload. No
service/auth request added. Three assemblies compile; two direct managed contract
cases pass. Actual native approval and cross-platform peers remain unrun. Explicit
protocol versioning still covers code/effect semantics outside this metadata.
[Compatibility scope](reports/stability-2026-09-27/multiplayer.md#skill-data-compatibility-without-cosmetic-coupling).

Room browsing now preserves LAN/online advertised joinability instead of inferring
it from visible player count. Reserved chairs and full LAN connection capacity no
longer show an enabled JOIN. Visible row comparison includes name,capacity and
admission changes, replacing a repeatedly concatenated partial key. Layout stays
unchanged. Runtime/Tests compile; two direct managed data checks pass against the
compiled helpers. Native UI/discovery remain unrun. Initial compiler preflight was
blocked; one retry succeeded after free space recovered, without a Unity relaunch.
[Room listing details](reports/stability-2026-09-27/multiplayer.md#room-listing-state-and-admission).

Boot now discovers and retains prefab roots in the three existing hero-prop
folders, yielding between folders and advancing progress only after each load.
Paete, shared rework and Phaister prop loaders reuse the same source cache; newly
authored props in these folders join the preload without a duplicate filename list.
No art, palette, pose or effect behavior changed. Runtime/PlayTests compile; the
new focused lifecycle test is pending due the existing editor disk-reserve limit.
No additional editor launch or repeated visual film. Player hitch claims remain OPEN.
[Loading details](reports/stability-2026-09-27/loading-audit.md#hero-prop-prefab-preparation).

Protocol63 adds a shared bounded skill-cast serializer with stable ability ID and
explicit activation/command intent. Host checks identity and intent before effects;
replicas no longer reinterpret an accepted command from their local active timer.
Body/kit/familiar arrival uses a64-entry,2-second,per-actor-ordered queue; round,
match,scene and transport changes retire it. World recovery cannot race queued casts.
Runtime and both test assemblies pass a direct compiler check. Native validation
did NOT run: the guard stopped the editor below the disk reserve before compilation.
Do not repeat launches at unchanged headroom; new codec/delivery and affected flight
cases remain pending for the next safe integration run. All platforms need63.
[Delivery contract and exact limits](reports/stability-2026-09-27/multiplayer.md#explicit-cast-identity-intent-and-delayed-delivery).

Owner correction: preserve incoming character reworks and adapt their network
integration. Do not rerun unchanged suites/films for minor or cosmetic changes;
select checks by the changed delivery/state contract and retain prior evidence.
Cosmetic presentation is independent of authority, not a promise that future
gameplay changes require no qualification.

Approved replica playback now carries an execution context through preparation.
Paete's approved command cannot be swallowed by a replica's reload timer; ordinary
input and host eligibility stay gated. Unpredicted host-origin owner casts take
full playback, including deferred effects and sky. Phaister's status presenter is
body-owned, so joining timers need no original cast and despawn cleans its tells.
Two NEW local PlayMode cases pass (2/2); no old suite or film was rerun. The sky
flag follow-through is source-reviewed after that run. Real peers remain OPEN.
[Replica lifecycle evidence](reports/stability-2026-09-27/multiplayer.md#approved-replay-and-joining-status-presentation).

Protocol62 world recovery is implemented: one bounded header serializer, match/
round/scene-instance/generation and cast/request freshness checks at begin/end,
simulation-clock aging and coalesced recovery. Owner binding is capability-based;
Paete's restored object also restores its active skill, retires old ownership,
releases pullers and no longer deletes other players' plants on reset. Focused
EditMode6/6 and PlayMode2/2 pass. All platforms need matching protocol62 builds.
[World recovery evidence](reports/stability-2026-09-27/multiplayer.md#scoped-world-recovery-and-bound-ownership).
Real-peer presentation/cutscene coverage and the full networking requirement stay OPEN.

Newest order: finish network correctness/presentation first, then optimization
and actual work-driven loading-screen readiness. Do not treat hardcoded stage
timing as asset/scene/shader initialization completion.

Current receipt fix separates accepted effect delivery from latest-slot resource
receipts. Pending initial effects wait before ticking/recasting; confirmations
are exactly-once per tracked request. Paete command confirmation does not replant,
and command denial does not cancel the older accepted plant. Transport reset
retires pending requests. Four distinct local native cases pass across one run
and one role-correct fixture repair; no real-peer or whole-network completion.
[Receipt details](reports/stability-2026-09-27/multiplayer.md#independent-effect-receipts-and-command-lifecycle).

Owner direction: perform the implementation directly, without delegated workers.
Continue concrete network/flow fixes. The new skill requirement is an explicit
shared networking contract so new skills cannot silently omit required multiplayer
support. Cover cast ownership, prediction/confirmation, authoritative effects,
persistent state and lifecycle recovery without redesigning skills or presentation.
This requirement is OPEN; current per-kit routing is not future-proof completion.

Owner expansion: network consistency includes body animations, effects, cutscenes,
interruption and cleanup for host, owner, observers and spectators, including late
join/reconnect. No local-only check closes this requirement. All abilities now
declare an explicit delivery mode through an abstract base property; kit creation
and the normal pre-build hook reject invalid declarations and missing roster
registration. Five focused EditMode cases pass. This authoring guard is implemented,
but remaining state and presentation delivery work stays OPEN.

First implemented networking slice: kit-owned `ITimedKitReplication` bindings
replace TimedKit's hero-name switch and live-slot duration assumptions. This fixes
Dante's twenty-second signature shield being rejected by a ten-second cap or
clamped to BOULDER's zero duration. Sean/Zack bind to their attacking abilities.
The wire layout stays unchanged. Focused EditMode 1/1 and source envelope audit
93/0 pass; actual peers and the rest of NET-SKILLS-1 remain open.
[Details](reports/stability-2026-09-27/multiplayer.md#timed-state-ownership-correction).

One immediate interaction fix separates hero-shop wallet updates from full hero
presentation rebuilds. Wallet busy/status/ownership changes now refresh purchase
controls only; they no longer destroy and reinstantiate the model or ability tiles.
Hero selection still takes the existing full Show path. Source-reviewed, with no
new measured player frame-time or purchase-service claim.

## Owner correction: implementation first, 2026-09-27

The owner explicitly rejected validation loops and diagnostic churn. For the
remainder of this session, prioritize actual improvements and fixes. Do not start
new capture rigs, broad audits or test-repair projects. Use only short, focused
checks directly justified by a changed behavior; preserve a failed result and
fix its real cause without repeatedly expanding the verification scope.

Loading/optimization is the immediate implementation priority. The owner's target
is that clicking a control does not trigger a loading hitch: expensive asset,
shader and interface preparation belongs behind the loading screen before the
player gains control. Current work corrects incomplete progressive shader warmup
and removes justified redundant loading work. No measured hitch-free or speedup
claim exists yet; instrumentation alone is not an optimization deliverable.

Finish the already-started flight/recovery unit and publish task-owned work, but
do not begin a full Amihan pass or new animation/VFX/skill redesign. A new report
that multiple skills may work only on their client remains an unresolved gameplay
networking issue, not proof of a specific root cause. Preserve contributor lanes.
The prior QA batches remain published; the whole TODO and release gate are not done.

Keep this correction active across continuation and compaction. The private resume
record tracks exact source freezes, jobs and commit preparation; detailed evidence
belongs in the existing reports, not in another verification framework.

## Loading fixes and flight integration, 2026-09-27 (ASTRAReworks)

Published integration: `c9f55410`, followed by shared-branch merge `70fb4ede`.
The next narrow loading fix also populates the existing parsed ultimate-introduction
cache at boot, including held-slipper variants, yielding once per hero. Previously
the first ultimate could load, parse and sort its table on the action path. Only
loading order changes; no authored timing, visuals or gameplay rules change. This
small follow-up is source-reviewed, not an additional player timing measurement.

The shared flipbook loader also prepares its existing 12 texture sheets during
loading, one per frame, and reuses successful texture references on effect creation.
The catalogue is about 7.15 MiB of raw RGBA pixels; no geometry, material ownership,
UVs, tint, authored sheet or ability behavior changes. Missing textures still use
the existing warning/null path. This resource-cache follow-up is source-reviewed;
it is not a GPU-residency or measured frame-time claim.

Boot now uses the shader variant count and the API's true-on-complete return value,
instead of stopping at its first incomplete batch. It still yields between slices.
The duplicate all-audio sweep is removed: the existing yielded folders cover all
204 current clips. Twenty-seven current menu-art assets and all 22 offered avatars
are prepared during loading and reused through their existing access paths. Missing
assets and unknown-avatar fallback retain their behavior; source artwork is unchanged.

The focused native loading check passed 1/1 and compiled the final authored source
candidate. Its Editor collection reported 97/97 variants warm after 10 calls; the
longest Editor slice was 497.021 ms. This proves completion and cache reuse, not a
hitch-free player, a Windows before/after speedup or Android responsiveness.
[Loading result](reports/stability-2026-09-27/loading-audit.md).

The previously started flight unit is locally qualified: Core 8/8, final focused
PlayMode behavior 10/10, prior protocol handler checks 4/4. Flight identity survives
ordinary interruption/landing, stale state cannot resurrect it, and snapshot refresh
is scoped and coalesced. Protocol is 61; paired builds must match. The two already-
baked flight clips are integrated without character-model or gait edits.
[Flight source and evidence](reports/amihan-kit-2026-09-27/featherfall.md).

The normal match-result path parks touch and leaves all six tested result controls
hittable (1/1). Opt-in title, Sean-freeze and performance diagnostics are preserved
but their unexecuted scenarios are not called fixes. There is no completed player
build, green broad gate, real-peer skill qualification or full Amihan completion.

## Menu QA, room titles and offline rewards, 2026-09-27 (ASTRAReworks)

QA2 follows published `026fed74`. Consent now shows a check; password faults visibly
render and replace busy status. Mode posters stay inside their shaped cards. Current
hero roles, unavailable portraits and the preparation guide agree with live kits.
Lobby chat no longer covers seats; score chips fit ordinary and extreme values.
Paete's preview-only framing now reads larger than Sean without cropping, and the
shop's full role captions fit. No model, roster or gait asset changed in this batch.

Practice no longer enters local career/upload or C#/JS reward settlement, and its
summary cannot inherit an older queued match's upload promise. Existing balances,
history and the separate temporary top-up are preserved; Cloud Code is not deployed.
LAN titles retain 24 characters. Known directory titles are session-owned, code-bound
and attempt-owned through controller recreation, cancellation and transport restart.

Evidence: Core 659/659 and stubbed Node wallet checks; current-kit EditMode contracts
74/74 plus title/career 3/3; 17 distinct QA PlayMode methods have passing targeted
receipts across recorded amendments. The HIGOP input-contract correction separately
passed both local shared-cohort cases without runtime phase changes. The final QA
snapshot contains 55 authored files; all 48 source files selected for this batch match
its hashes. Original failures and before images remain preserved.
[QA2 results and limits](reports/stability-2026-09-27/qa2-validation.md),
[QA comments](reports/stability-2026-09-27/qa-comments.md),
[Paete framing](reports/stability-2026-09-27/paete-framing.md),
[reward eligibility](reports/stability-2026-09-27/practice-rewards.md),
[current-kit contracts](reports/stability-2026-09-27/regression-contracts.md).

This is not a green full gate, a player build, physical input qualification or real-peer
proof. The name-field case uses actual raycasts and explicit Unity key events, not OS
typing. Live services, the complete Practice range, Sean/Cheska peer reproduction,
performance measurement, remaining hero presentation and final qualification stay open.
The in-progress Amihan flight/protocol and performance instrumentation are separate,
not included or claimed verified here. UI motion adjustment: fitted score numbers no
longer use the 7-percent scale pulse; the chip glow remains, as logged in menu-qa.md.

## Room lifecycle and current hero UI, 2026-09-27 (ASTRAReworks)

Verified batch from `04886cc4`: cancelled host/join work cannot revive an older room or stop its
successor; queued joins wait for an assigned seat and preserve failure reasons. Native HUD now
shows rooted escape and plant-pull bindings/progress, with keyboard, pad and touch paths. Real
abilities have distinct glyphs; placeholders show unavailable state. Reused UI symbols refresh
their mesh/material; Amihan and Paete avatars import as sprites instead of falling back to Dante.

Fresh evidence: Core 658/658; presentation 24/24; session lifecycle 11/11; native interaction
3/3 after one fixture repair; pending-join 1/1; avatar/symbol 2/2; selector 1/1 across the roster
and text sizes. Native prompt and selector frames inspected. The first rooted test failed because
synthetic input devices were disabled; the single repaired retry passed actual held-input progress.
Published-source candidate: local batch `45c2217`, shared-branch merge `577c1a95` (incoming
`c3977bb4`). Post-merge graphics-enabled EditMode 37/37 and native three-device rooted case
1/1 pass; previous receipts retain their original revision identity.
[Exact receipts and limits](reports/stability-2026-09-27/validation.md),
[implementation](reports/stability-2026-09-27/implementation.md),
[icons](reports/stability-2026-09-27/skill-icons.md),
[menu findings](reports/stability-2026-09-27/menu-qa.md).

This does not establish live Relay, real-peer reconnect, physical-device qualification or measured
performance improvements. No new player build or Desktop replacement. Next: integrate incoming
shared-branch changes as they arrive, reconcile the remaining current-kit regression contracts, then continue
the remaining owner QA, general performance and assigned hero presentation. All animation/model
work in this batch: none; the seven icon drawings and their rationale are in the icon report.

## Per-character walk and run, 2026-09-27 (worktree `TumbangPreso-Unity-ASTRAReworks`, branch ASTRAReworks)

The owner rejected the cast-wide walk (*"walk is fucking ugly"*, *"everyones arms are floating and not even attached right"*,
*"do it one by oen dont generate the same one for all"*) and made it a rule: never stamp one change across the whole cast
(CLAUDE.md section 0, AGENTS.md). Built: `Runtime/Visual/GaitStyles.cs`, one hand-written walk and run per body from its lore,
drawn from the bind pose by `CharacterAnimator.LocomotionArms.cs` (the shoulder never moves; the shared clips' lean no longer
leaks under it); cadence per character with a capped slide (`Gait.Glide`, 1.1 to 1.63). Evidence: `Logs/gait-v7` (every body,
walk and run, front, side, three-quarter), per-character clips `Logs/walk-share/gait_v7_<body>.mp4`, cadence table
`Logs/gait-cadence.csv`. State and next steps: `docs/TODO.md` ASKS-0926, the NEWEST row.

## Cloud session with a real Unity editor, 2026-09-26 (branch ASTRAReworks)

Unity 6000.5.8f1 now runs in cloud sessions: `tools/cloud_unity_setup.sh` installs it to `/opt/tump/unity` and activates a
Personal licence from `UNITY_EMAIL` / `UNITY_PASSWORD` (Unity's licensing client, `--activate-ulf --include-personal`);
`tools/run_unity_guarded.py` adds the virtual display (xvfb, Mesa llvmpipe) and `-buildTarget Linux64` on Linux. Measured:
first import about 6 min, a 13.5 s two-view 1280x720 film about 5 min. Done: the four ReworkProps metas, the COMING SOON
placeholder in three gesture tests, the Paete film rig (a human was casting his skills; `paete_skills_v2.mp4` sent).
In flight, in this order: THORN HARVEST placed where he looks, LIANA LEAP's first person as his own arms, then Amihan's
second pass (`docs/reports/amihan-kit-2026-09-26/`: plan, research from the game's own preview clips, cutscene direction).
**Every ask of this session, with its state, is `docs/TODO.md` ASKS-0926** (owner: *"pls log all todo i asked u for when i go
to diff session"*): the QA relay fix (awaiting a tester), Paete's E (recast gate, outside the box), the tansan grant (needs a UGS
deploy, and a revert before release), Paete's preview size, the interact prompt, the walk's arms, and Amihan's whole pass.

**State at this session's hand-off (2026-09-26, cloud):** pushed `6dea8556` (QA relay guard, Paete E recast gate and
outside-box plant, placed thorns, first-person vine bend, CODE colon, tansan grant, Amihan film probe), `76e0fb54` (per-body arm
fit, `WalkArmsProbe`), then the gait second pass (arms hanging flush beside the torso, foot plant, lean and weight roll, walk
clip tool). Evidence folders are cloud-only (`Logs/cloud5` to `Logs/cloud9`). Next, in the owner's order: ASKS-0926 row 1 (the
Amihan and Rafi remodel), then the rest of ASKS-0926 newest first. Paete's size: the plan in its ASKS-0926 row (grow the body in
play and frame the previews against a shared reference so a taller hero reads taller); nothing is built for it yet.

## HERO-9 Paete, Unity pass on the cloud work, 2026-09-26 (worktree `TumbangPreso-Unity-paete`, pushes to ASTRAReworks)

Owner: Paete first, and *"I WANT THIS TO BE THE BASELINE QUALITY OF EVERYTHING ELSE MOVING FORWARD"*. Every cloud
piece is placeholder plus planning. Done and filmed: the plants in engine, the ult sound re-timed, the in-match ultimate
film with sound (`paete_ultimate_v3.mp4`, sent), bots measured, the arm-audit red. The owner rejected the cutscene's
direction, Makiling's look and the live tree after v3; the redirect is direction.md 5.12 and TODO HERO-9's REDIRECTED row
(what is in source and what is not). Next: finish the three-shot re-author, refilm, send v4.

## ABILITY-2 roster rework and Paete plants, 2026-09-26 (cloud session, no Unity)

Plans: `docs/reports/ability-rework-2026-09-26/plan.md` (tables verbatim, owner answers, numbers set
here in section 7, order in section 6) and `ultimates.md`; Paete plants `direction.md` 5.11. Cloud box
runs Core.Tests (.NET 9) and the Python builders only; every Unity-side claim is owed to the testing
chat. Done here: plans (`plan.md`, `ultimates.md`, `cast-preview.md`), Paete's pitcher and rattan (builder,
glbs, bodies), Core statuses 6 to 9 and `RosterReworkRules`, skill tree off (`SidegradesOpen`), Core
649/649. Next: motor statuses and immunity with `SyncUnit` fields, then kits in plan order, then CAST-1.

## HERO-9 Paete lane, 2026-09-26 (worktree `TumbangPreso-Unity-paete`, branch `paete-hero`, pushes to ASTRAReworks)

Owner-directed Paete work: modelled trees, the woven binding and break-out, Mariang Makiling's ghost
in the ultimate's cutscene, the ground-called ultimate, renames and lore, bulky first-person arms. The
open list, in order, is TODO HERO-9 (the "NEXT" row is the attacker and defender plants as their own
species). Direction: `docs/reports/paete-kit-2026-09-25/direction.md` section 5. Commits and test
results are in the HERO-9 rows; protected composition metas and ProjectSettings churn stay unstaged.

## Current resume, 2026-09-25: local skill tree verified, continue gameplay work

Owner asleep; autonomous work continues with NO USAGE RESETS, credit spend or
paid services. DEV/remote/QUAL verified at a8291c76c after Ignition Cannon's
small impact; report under cloud-integration-2026-09-24/skill-fx/.
Supernova's staged dome remains translucent with the can readable, but an
actual player overlap is still needed. Hex already has four standing marks,
and Kuro's7.4scale was explicitly approved over5.6 by the owner; do not
shrink Kuro from a close showcase frame. Both remain in their live gates.

SkillTree's native HOME-door and BACK route passed1/1 before in42.053s and
1/1 after in36.207s. Five actual viewports were captured each time; no
layout collision observed. Open alternate tiles read UNLOCKED. The mastery
line exposed "every branch open for testing" to players, so the scoped source
edit removes that phrase in open mode only; locked mode retains its earning
instruction. Report, XML, before/after images and 25percent greyscale live
under reports/cloud-integration-2026-09-24/skill-tree/. Pad/touch hardware,
real progression, LoadoutSurfaceProbe and TumpNativePickerTests remain open.
Publish this unit, align QUAL, then resume actionable skill/cast and older
gameplay/map gates one at a time. Protected DEV composition PNG metas remain
dirty and must never be staged or restored.

## Published unit, 2026-09-25: Supernova and remaining skill effects

Owner asleep: continue autonomously and NEVER use a usage reset, credit or paid
service. DEV/remote/QUAL last published at5b4f69489; Ignition Cannon work is
currently dirty in DEV and QUAL and must be published without protected DEV
composition PNG metas. No Unity run in flight.

SKILL-FX-1 progress: Flame Rush v59 eye/corridor/grey inspected and geometry
retained, report in cloud-integration-2026-09-24/skill-fx/flame-rush-read.md.
Ignition Cannon was previously generic Slipper impact. A small separate style
now preserves radius2.6, knockback13, stun1.4 and neutral element, leaves
ordinary slipper/Supernova untouched. The v60 floor-only cue was too small at
eye height; v61 short ember reads; v62/v63 tapered tongues were still thin.
Final SOURCE selects v61's runtime shape and reserves image versionv64 to avoid
overwriting evidence. Actual same-camera/grey comparisons and limits:
cloud-integration-2026-09-24/skill-fx/ignition-impact.md. V60-v63 guarded
Editor showcases exited0; raw images/failed candidates and churn backups stay
in QUAL Logs. One accidental unchanged-source v59 run is documented and not
counted as a new design check. No broad gameplay suite or new player build.

Next: fetch, stage only scoped source/docs/evidence, commit/push with M4tyu633
and verify remote; align QUAL by byte-normalized comparison, named stash and
detach published SHA. Then inspect Supernova's large dome with player/lata
visibility during real overlap before deciding an art change. Continue the
remaining skills one by one, then older actionable maps/bots/UI/peers/P7. Keep
all TODO parents open while their live/mix/network gates remain.

## Published unit, 2026-09-24: Sean's parol read

Owner requested autonomous quality work and NO USAGE RESETS while asleep.
No credit redemption, paid service or cross-chat work. DEV/remote/QUAL at
7727c1f45 after the FPP grip correction. The grip unit is published with native
owner/body sequences and3/3 focused checks in reports/cloud-integration-2026-09-24/
throw-clearance/report.md. No pending Unity process from it. Synthetic head test
remains red but real-input Bayan spin.75 and1 with largest slipper stayed clear;
do not change healthy body motion to make that fixture green.

Sean's crafted-parol local unit is ready to publish: the cloud-intake baseline
lost its orange frame against the shirt/horizon. Pale warm0.016m sticks,
dark-ember inner flame and slightly larger/forward shape now read at the same
native close camera and25percent grey. Face is clear. Existing guarded native
intro study1/1 in32.88192s; one variant retained. Proof:
reports/cloud-integration-2026-09-24/sean-parol/report.md.
Publish/align QUAL, then continue cloud skill VFX/SFX first, one real weak skill
at a time from the per-skill plan. Keep older queue open. Do not rerun this
passing introduction case unchanged.

## Published unit, 2026-09-24: real throw clearance defects

Owner confirmed this turn runs GPT-6 Sol and asked for real product fixes over
verification loops. The owner is sleeping; NEVER use a usage reset,
credit redemption or paid service. Keep working autonomously without questions. The cloud branch plus local repairs were merged/pushed to
ASTRAReworks at17f572be8; QUAL was byte-compared, stashed, detached at that SHA.
Seven-hero introduction study1/1, shared-phase6/6, Nemu opening framing1/1, Core
HeroLines7/7 and HeroLoadout14/14. Actual sequences/grey were inspected; this
is not artistic or real-peer acceptance. See reports/cloud-integration-2026-09-24/
findings.md. No more unchanged runs of that passing set.

Current unit: reports/cloud-integration-2026-09-24/throw-clearance-plan.md.
EditMode baseline4/4 failed: three FPP spin cases (straight grip depth.082m,
curves .38/.40m vs.40 minimum) and head-volume case. Full CSV, not truncated
failure text, has190/190 person/shoe pairs red,175atcharge.35/rightspin1.
This is not proof that every live body throw clips. Existing real-input Bayan
PlayMode case at spin.75 passed1/1: quick/left/right normal-speed sequences
inspected, right-side and other charging sampled98/100 frames with0shoe vertices
inside the head. One scoped worst-condition run is in flight, QUAL session96919,
TUMP_THROW_REVIEW_PERSON=bayan, SLIPPER=alpombra, RIGHT_SPIN=1, output
Logs/cloud-throw-native-spin1.xml and frames. It uses the existing review probe's
optional legal spin; no gameplay code changed yet. Do not edit .cs while running.
Exact native Bayan/alpombra case passed1/1 in19.5313924s at legal right spin1,
0of41696vertices inside the head across92charged samples. Native source at.75
also passed with0. Preserve healthy body pose. EditMode's190/190 failure is a
fixture discrepancy to reconcile at final qualification, not authorization for
a global body rewrite.

FPP baseline .082m straight grip near eye. First offset pass fixed z but dropped
all hands below the frame. Pivot-target pass still gave .22-.27m depth: the shoe
follows the rotating Arm child, not its parent pivot. Current product fix tracks
that actual fingertip near CarryAnchor with short separate straight/left/right
travel and keeps the roll/tremor. Existing focused EditMode grip cases passed3/3
in0.2114822s, with unchanged assertions. Normal-speed owner/body capture passed
1/1 in20.3993265s; owner straight/left/right and court-side right views and
25percent grey inspected. No active Unity job. Detailed findings and preserved
failed/native proof: reports/cloud-integration-2026-09-24/throw-clearance/report.md.
Publish scoped fix and align QUAL, then resume remaining cloud skills/VFX art and
real gameplay issues. No reset, no broad unchanged verification loop.
Keep checks bounded. Protected DEV PNG metas untouched.

## Current priority, 2026-09-24: review the owner's cloud push

Owner explicitly confirmed the push after asking us to wait. DEV fast-forwarded
from7fcde1877 to4f62fcc5c, PR5 already merged. Review/fix this cloud work FIRST,
then resume the older TODO path. No old item is deleted or marked complete.
Plan: reports/cloud-integration-2026-09-24/plan.md. First check: existing native
seven-hero introduction study with TUMP_INTRO_SCENE=1 plus SharedUltimatePhaseTests,
guarded D3D11 in QUAL. Inspect actual frames, then fix observed problems one hero
at a time; one fixture repair maximum. No new player build or new capture framework.
Latest cloud voice decision is human recordings only; absent clips stay silent.
No generated voices, paid tools or contacting another conversation.

Current native defect: baseline7cases2passed/5failed on SetCurve key-index asserts
in introduction grounding. Removing x/y/z bindings first did not fix it (v2failed,
preserved). Current dirty fix builds final root curves on a fresh unsampled clip
through ClipBuilder, retaining authored rotation/punch/lift. Fresh-clip construction removed the assertion. Native v3 reached Nemu, where
held equipment intersected her head (65vertices); first4grounding clearances were
within0.7mm. Actual frames also showed black stage walls: WallMesh still shared
front/back normals. Current dirty repairs use the corrected TwoSided builder and
a Nemu-held table: shoe by hip, free-hand send. Existing seven-hero study v4
passed1/1 in33.3963163s: all seven clear their support within1.9mm and have zero
held-shoe/head intersections. Actual twelve-sample native sheets inspected for
each hero. Black-wall defect fixed; no blanket artistic approval implied.
Existing SharedUltimatePhaseTests finished6/6 in73.5436033s; output
Logs/cloud-shared-phase-after.xml/.log. No active Unity job. First full7run
failed before these views. Actual sheets for all seven and three25percent grey
pairs inspected; local findings in reports/cloud-integration-2026-09-24/findings.md.
Do not repeat these passing cases unchanged. Fixture repairs0/1; these are real
runtime repairs, no assertion suppression. Core HeroLines7/7
and HeroLoadout14/14 passed in DEV, separate TRX under Logs/cloud-intake-core.
Next: publish this repair batch, align QUAL, then investigate client cohort length
being derived before local actors are ready (plan). After runtime correctness,
Sean's faint parol/competing horizon is the first concrete visual refinement.
Preserve the other incoming performances and inspect each before changing it.

Previous local units ARE published: d98e592ae Windows D3D11 preference;7fcde1877
ambient replay isolation/can-mesh cache repair. Their focused evidence is complete,
their broader final gates remain. The historical resume notes below are retained.

## Current resume, 2026-09-24: ambient replay isolation

Overall goal ACTIVE, NOT done. DEV/QUAL and remote verified atd98e592ae. Rematch
reconciliation published: actual Bayan entry observed after asynchronous loading;
final bounded test source preserves map/ready checks but has no final green rerun.
Invalid agent-added DontDestroyOnLoad scene assertion removed. Fixture allowance
1/1 exhausted, leave final rerun to P7. Do not call it an ongoing runtime map bug.

Current plan: native-shutdown-plan.md. Exact original accessibility-v57 build from
older validation/Builds was copied into QUAL Builds/shutdown-diagnosis-v57-20260924,
not rebuilt. Core exe/UnityPlayer/runtime hashes match; receipt in
QUAL Logs/shutdown-diagnosis-20260924/binary.json. Original remains untouched.
Existing native runner gained optional --graphics-api d3d11/d3d12, syntax checked.
Native pair reaped: D3D12 process9508/exec5951 passed15stages then crashed with
0xC0000005. Windows event1000: D3D12Core1.618.1.0, offset0xa1f5, RX6600 driver
32.0.21043.19003. D3D11 process4792/exec26235 passed the same15stages and exited0.
Both actual APIs confirmed in logs; same copied binary hashes, fresh profiles,
input unchanged. Original untouched; no new build/current-binary claim.

Current mitigation: GameBuilder.PreferCompatibleWindowsRenderer authored Windows
explicit API order D3D11,D3D12, preserving other platforms/quality. Guarded author
finished; scoped serialized diff copied to DEV. Current-source five-map D3D11
case passed1/1 in43.5834526s; actual stage/eye/cast and25percent grey inspected.
Proof: reports/map-by-map-refinement-2026-09-23/native-shutdown/report.md.
No game/Unity/crash-handler processes remain. Native receipts/WER event preserved.
Final current player/default-backend/exit/performance remains P7. Exact internal
engine/driver cause unknown; no upgrade, intermediate build or indefinite soak.
Renderer mitigation published ind98e592ae; QUAL aligned. Cleanup of the copied
diagnostic build was rejected by automatic approval review (blocked by policy).
No process remains; leave that copy in QUAL Builds, original v57 untouched.
Do not repeat the old-binary pair or bypass the cleanup rejection.

Ambient replay unit ready to publish: both leaks reproduced in native baseline.
Existing per-render hide/restore now isolates unrecorded live animals in retained
and catch cameras. First post-fix run exposed a real destroyed-can-mesh reference
in Lata's existing raise/clunk cache during model replacement. Cache invalidation/
Visual-subtree rebinding fixed; motion/authority unchanged. Final2/2 passed in
13.0930289s, actual same-camera before/after and25percent grey inspected.
Proof: reports/map-by-map-refinement-2026-09-23/ambient-replay/report.md.
No fixture repair, no active Unity job, own generated churn restored. Publish and
align QUAL, then reconcile remaining research/per-map acceptance against actual
reports before selecting the next implementable gap. Do not repeat passing cases.

Published roof units: court07a387779, haze/card4dde9837a, street contextc85614a42.
Actual colour/grey inspected; street v2 passed1/1 in1.372s after one close-camera
repair. Source models/physics/card importer preserved. No more variants/fixtures
for that completed local unit. Lighting a28037622 merged in5e9b711c7; optional
cast hull floor remains0, native1/1 and actual comparisons.

Bot fixes5de3a78f3/043cf804c/273e5e669 published. Initial four-bot/two-mode samples
cover all five maps; full role/roster/tier and bot water/roof recovery stay final
gates. Recovery fixture failed twice BEFORE handoff at wrong lip position;
omitted camera/input-basis setup is next diagnostic, allowance1/1 exhausted.
Exact draft/failures in bot-map-coverage/, original compiled probe restored.

AGENTS cleanup dc10b68f2 preserves important rules in39.7percent fewer words plus
exact archive. All21 ambient actors have local placement coverage; ordinary-camera/
replay/combined gates remain. New character/ability/ultimate work belongs to the
owner-run cloud lane; do not merge unfinished work or contact a conversation.
Preserve older requirements. Final current-source native/peer/replay/performance/
build remains2.10/P7. No checkpoint stops goal; no intermediate build.

## Parallel lane, 2026-09-24: HOME loop and gameplay animation (separate from the map resume above)

Published 4255265c and 175cac0d on ASTRAReworks. UX-1.20 (Phaister's HOME loop, shipped crf 23,
35.1 MB) and UX-1.21 (Kuro) DONE. REFINE-2.9 progress: arms while moving, throw and pektus, the can
raise, the tag, and all 21 hero casts refined in both views; REFINE-2.9c (every body) done. Evidence and
reasoning: docs/reports/gameplay-animation-2026-09-24/. Probes: LocomotionArmsProbe, GameplayActionShots,
CastAndMotionReel. Shipping casts are the glb tables in tools/author_hero_action.py, NOT HeroAbilityClips
(editor fallback only). Last checks: PlayMode 11/11, EditMode 44/44, 18 glb casts verified. Not yet
seen: a live networked match, including an observer's estimate of the can raise.

## Parallel lane, 2026-09-24: REFINE-2.11 ultimate performances (cloud Ubuntu session)

**Lane resume file (all owner instructions and the plan):**
docs/reports/ultimate-performances-2026-09-24/lane-ledger.md . Branch claude/animation-ultimates,
PR https://github.com/DOST-GameDEV/TumbangPreso-Unity/pull/5 .

Owner assigned all animation work to this lane, ultimates first, one hero at a time (*"dont js spam
copy paste stuff"*). Research, plan, per-hero evidence and machine limits:
docs/reports/ultimate-performances-2026-09-24/ (research.md, plan.md, progress.md). No Unity
licence on this machine: native checks are owed to Windows (list in progress.md); C# is verified by
a Roslyn check against Unity 6000.5.8 DLLs and the locked package versions, poses by skinned glb
sheets from the authored shots. Per-hero intro lengths (2.8 to 4.2 s), protocol 52. Phaister DONE
at source level; Sean, Zack, Nemu, Dante, Cheska, Rafi still on transcribed 2.8 s baselines, next
in that order. Then VOICE-1 (hero voice lines, TODO) and the CLAUDE.md condensation request.

## Published progress, do not redo

Ground-animal local units all published: Eskinita dog01d93272b/cat08ac1a178,
Bayan dog4e2c5bc6d/cat2bcfd0581, Ilalim dog81054255e/catd914a3497. Each has its
own plan/author/native result/actual frame review under the named report folder.
Bird visits: Eskinita ad7d57955, Bayan c61bb0318, Ilalim fdce3ebf8 and current
SaBubong unit complete locally; final integrated review remains open.
Lighting follow-ups237012904 and43c851b6f include source through429643416; keep
Windows evidence separate from the incoming Mac native performance receipts.


- All 11 Ilalim storefronts and both Eskinita shop signs have individual authored
  work/native evidence. Sign register and each report preserve exact source,
  rejected candidates and review limits. Pares v1 owner-rejected; Lugaw King
  reference was actually inspected and v2 wood fascia/simple bowl shipped in
  065ad6480. No owner approval of the replacement is implied.
- 02617e9d2 refreshed Eskinita/Ilalim map cards from actual current scenes. Ilalim
  axial under-bridge thumbnail selected; runtime UI/camera descriptors unchanged.
- 59b8dc129 roof rail catch/hang/mash/same-lip climb and Lagoon swim/near-bridge
  Jump/mash climb. Protocol 51; host trajectories/epochs/reliable poses, cast-palm
  fitting, local native evidence. Real peers/loss/rejoin/replay remain final gates.
- 01d93272b Eskinita tan aspin: 129 nodes/768 links, investigate/watch/rare marking,
  eased stride/turn/arrival, close or fast-approach retreat, stationary-person
  tolerance. Focused native 1/1 in 32.339s; actual frames/grey inspected.
- 08ac1a178 Eskinita tabby: 111 nodes/669 links, three east-side sites, quieter tail,
  own pace/head dip/holds. Native v2 1/1 in 32.072s. V1 wrong-side observer/camera
  fixed once; failed evidence retained. Actual before/new/activity frames/grey
  inspected. MP4s retain recorded timing; no continuous-playback claim from the
  image-only review tool. Detailed reports in eskinita-dog/ and eskinita-cat/.
- 21efec0c7 merged concurrent remote 4255265c3 without conflicts. It adds the
  owner-approved Phaister HOME loop/Kuro work and new body/FPP locomotion, throws,
  pektus and can raise. Preserve it. Before REFINE-2.9 read
  reports/gameplay-animation-2026-09-24/research-and-analysis.md and latest TODO;
  inspect/fix remaining per-character/contact/integration gaps, do not rewrite
  these new implementations from stale queue wording. Cat evidence predates merge;
  combined qualification and body/edge-recovery overlay review remain open.

QUAL modified candidates were byte-compared to DEV, preserved in named stashes
and advanced. Keep those stashes and all unrelated worktrees. Known generated
Inday/meta/ProjectAuditor churn was backed up before restore after every run.

## Remaining scope and constraints

All five map parents and REFINE-2.7 remain OPEN for outstanding coverage and final
integration. Older building/material/coast/sky/island/bird/sign work stays intact.
Lighting source 50f1fc255 is integrated; preserve its bright look. LIGHT-1.6/1.9
remaining tuning/native performance are still open, not a reason to darken blindly.

Still assigned: remaining animal species/maps, all-bot stalls, per-character
motion/contact/observer review against the NEW merged implementation, chosen-map
rematch defect, native D3D12 shutdown 0xC0000005, RafiV9/peer/platform gates and
all older actionable IDs. The owner-run cloud lane may now tackle
Ultimate REFINE-2.11 alongside local work: research multiple praised games first,
individual hero plans next, implementation last. Phaister laughter/flight example; 2.8 seconds is not a cap. Full requirements
are saved in ultimate-performance-research-plan.md. No task silently removed.

Final coherent native/peer/replay/performance regression and internal build are
REFINE-2.10/P7 after features. No intermediate build or Desktop replacement.
UGS credentials, physical devices and owner taste limit only their specific claims.
No paid services, resets, other chats, subagents, main edits or forced git actions.
Fetch before each push; sole-author -F commits, explicit paths, verify remote HEAD.
Product quality over validation loops; no repeated unchanged evidence/fixture work.

## Cleanup and preserved history

An earlier task-owned hidden IAB tab15 remains stuck on PNA's certificate-error
data URL. Documented close/navigation/alternate close all rejected by URL policy.
Do not bypass security, kill the browser or loop on the same rejection. Other
research tabs closed. No new browser was opened for the current animal work.

Full preceding ledger: [through Eskinita animals](reports/map-by-map-refinement-2026-09-23/ledger-through-eskinita-animals-20260924.md).
It links every earlier full archive. TODO is canonical status; this is the exact
resume point. Continue without treating this checkpoint as completion.

Bayan cream aspin implementation ready. Guarded targeted author next, then its
one native case. Check purposeful connected sites, real observer/retreat/recovery
and pause, same-camera native frames/grey. Stop on pass and inspection. Retry 0/1.

Bayan first bake found 89 nodes / 691 links but no reachable bench/tree. Adjusted
to planting plus two distinct court watches and rare marking; require three
ordinary sites so cooldown cannot reduce behavior to two alternating stops.
Reauthor before first native test; no fixture retry used. No .cs edits in flight.

Lighting conflicts resolved: kept existing preview API/cache property-block fix,
ported HDR target, surviving root/sun choice, active-scene handback guard. Incoming
five-map case now also captures same-camera LDR/HDR and asserts new-scene settings
survive preview destruction. Next focused run with existing cached-map lift case
(two cases total). Stop on XML and actual colour/grey review; retry 0/1.

Edge-key v1 passed 1/1 in 8.235s, but cross-run image comparison was NOT camera
matched because preview idle orbit advanced differently. Images retained as
unmatched v1, not causal visual proof. One bounded capture correction: editor-only
legacy edge-key switch reproduces the old fallback with a portrait sun, then new
selection renders immediately through the same camera without yielding. Shipping
player has no switch. Retry 1/1; next v2 same single case, no fixture expansion.

Ilalim cat author completed: 53 nodes / 310 links, Pisonet watch (9.5,3.5),
bakeryward watch (9.5,9.5), seam investigation (10,3), y0.212. Before the first
run, movement assertion fitted to its shortest real trip (0.707m), not the other
cats fixed0.75m threshold. No failure/retry yet; actual successful movement still
required. Next only this cat native case. No .cs edits while running.

Bird verification question: do the real visitors retain original models, land
on supported alternatives, react and keep flying beyond the old four-metre hide
point, with distinct foraging/fantail lookout and frozen pause? Stop after native
XML and actual legacy/new perch/departure/arrival frames plus grey. Retry 0/1.
No .cs edits during the author/test run.

Bird author v1 stopped on one compile error in the new review file: missing
TumbangPreso.UI import for SceneFlow. Added that import only; this consumes the
unit fixture repair allowance (1/1). Preserve failed log. Reauthor, then the
same focused native case; no extra fixture refactor/reassurance runs.

Bird author v2 completed: all three species have three supported sites; distinct
beat/wait/forage/fan settings saved. Next single native Eskinita case records all
three legacy/new departures and arrivals, supported contact, reaction and pause.
Only prior missing-namespace fixture fix consumed retry 1/1. No .cs edits in flight.

Bayan bird author v1 rejected fantail northern landings near the monument. No
scene saved. Revised only that bird candidate list to clear court-facing/southern
paving; clearance unchanged. Reauthor v2 before first native case. No fixture
repair used (0/1); no .cs edits during run. Failed log/patch retained.

Bayan author v2 completed with three supported sites for each species, corrected
fantu alternatives at (+/-1.8,8.6). Only the Bayan bird native case next, with
original species clips, larger paving spread/longer holds. No fixture retries
used; no .cs edits while it runs.

Bayan native v1 failed clear-arrival assumption for pigeon. Trace shows arrival
turning into phase3 at(-.193,2.714,-4.993), near=True: correct avoidance of a
parked player, not failed landing interpolation. The taya cannot remain at the
requested(0,-13). One bounded fixture correction parks all actors at the opposite
legal court corner for arrival and asserts actual distance>12m. Runtime unchanged;
retry 1/1. Preserve failed XML/trace, rerun same case v2 only.

Owner steering during Ilalim bird unit: prepare paste-ready Claude Code cloud
configuration and a broad ALL-animation/research handoff, including independent
ability animations and freely directed ultimate performances. Do not contact
another conversation. The owner will paste it. This newer lane assignment lets
that animation work start without waiting for the older map-first schedule.
Preserve current map work and avoid duplicating the animation lane. Ilalim bird
author 42324 completed with three supported sites/species and bounded street-axis
flight; native test has NOT run yet. Source/scene/meta changes remain local at
c61bb0318. No Unity process active. Resume map validation after delivering setup.

Cloud setup deliverable prepared as four chat blocks: environment values, empty
API credentials, Ubuntu bootstrap with deferred heavy Unity install, broad owner-
directed animation/research prompt. Exact repo Unity6000.5.8f1 / changeset verified;
local Blender5.2.0LTS verified; Core.Tests targets net9.0. Setup Bash syntax checked
locally only, not executed in a Claude VM. Unity archive URL HEAD200 (4.35GB);
Blender primary download returned403 from this connection, so no successful
cloud download claim. Current map-bird author complete, native validation next.

Requested cloud environment/API/setup/handoff blocks delivered in the conversation.
Owner explicitly said continue local work afterward. Animation ownership/intended
parallel sequencing recorded in TODO/ultimate plan; no other task contacted.
Local work resumes the already-authored Ilalim bird flight corridor native check,
then remaining environment/behavior work. New hero/ability/ultimate animation
direction belongs to the owner-run cloud lane. No animation work marked done.

Concurrent incoming animation integration: origin175cac0dd/6bba9d0a4 added tag
reaches, cast readability/first-person gestures and per-body review receipts.
Merged without source conflicts; preserve its separate ledger section and TODO
2.9c status/evidence. New cloud lane must review this newest baseline, not redo
old weak-cast findings automatically. Local bird evidence predates this merge.
Incoming QualitySettings changed only five Ultra slots to the exact runtime
GraphicsProfiles.Balanced values (2 lights, Medium shadows, 2 cascades, 40m,
soft particles false). Restored the prior serialized baseline under the standing
runtime-churn rule; all authored animation/models/arm assets retained. No native
build or gameplay-animation requalification claimed in this map merge.

Tracked lighting source7d3171549 merged without conflicts. Adds real-window
captures/on-off measurements to the existing native graphics probe and scoped
Mac1600x680/2940x1912 evidence. tools/graphics_review.py syntax-checked locally;
no new Windows player/performance run claimed. Windows D3D qualification stays
P7, and newly recorded LIGHT-1.10 Mac fullscreen hitching remains open with its
actual source evidence. No previous queue row removed. Combined C# compilation
will be exercised by the next necessary map author/run, not an extra build.
