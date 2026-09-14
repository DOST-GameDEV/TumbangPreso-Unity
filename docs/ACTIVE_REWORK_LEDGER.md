# Active TUMP rework ledger

## CURRENT startup Back correction passed; save U1 then continue U2

34618completed1/1, guard1e34ef47cef6. Actual OpenAtBoot signup PNG inspected:
no Back control, original owner art and font/color roles intact. GUEST returns
without replacing the saved player ID. All runs retired. Report owner-ui-u1.md
now distinguishes old account-management shots from actual startup captures.
Next commit/push U1 firstscope, then U2 drafts (OwnerPlayView,OwnerCreditsView,
OwnerPortraitArt,OwnerScrollColumn under Logs) with text-action helper. Owner
explicitly says keep working until finished; UIfirst then gameplay bookmark.
No agents/Figma/reset/Desktop. Latest pushed9faf6ba6 before U1 save.

## CURRENT urgent owner correction: no Back on startup login

Owner rejected BACK in the shared picture. We had captured OpenForUpgrade,
the account-management path, rather than the requested startup OpenAtBoot.
This was a presentation/qualification mistake. New startup view no longer even
constructs Back; only explicit nonboot profile management creates its separate
return control on demand. Reference test now uses actual OpenAtBoot with a fresh
account state, asserts no Back even in inactive descendants, and exits through
GUEST while preserving the same player ID. Profile-management return semantics
remain separate, not shown as startup login. New filenames OwnerStartupSignUp/
OwnerStartupSignIn/OwnerStartupTerms-v1 distinguish the actual boot captures.
Running one focused case: Logs/owner-startup-account-v1.xml/.log. No C#/imported
edits during Editor run. Inspect corrected boot pictures before claiming fixed.
UI batch still uncommitted, latest pushed9faf6ba6. U2drafts preserved in Logs;
continue full UI afterward, then all gameplay bookmark. No agents/reset/Figma.

## CURRENT U1 first scope qualified; save then U2 Play/Credits/Loading

All Unity runs retired.5030author import success. v2basic account casepassed;
v3corrected artwork/terms case1/1;40692entry-flow2/2;52744ink pass1/1. Original
signup/signin/terms captures inspected after fixes. Match dark font colors via
Canvas.vertexColorAlwaysGammaSpace (verified local Unity UIModule API); only
new owner canvases affected. CREATE/GUEST enlarged to64matching source scale.
One-channel raster rounding remains; do not pretend pixel equality at AA edges.
Rejected v2 missing letters/renderers/legacy Back font fixed. Note the temporary
Logs/owner-ui-rejected-u1-v2 copy was made AFTER v3had overwritten those PNGs;
it actually contains v3 images and MUST NOT be labelled failed-v2 evidence.
Failure XMLs remain intact; original v2was viewed in chat but no disk image
archive was captured in time. Current report reports/owner-ui-u1.md is truthful.

Next save substantial U1 source/assets/docs after diff/hash checks and push;
then integrate reviewed Logs/OwnerPlayView.cs.draft and OwnerCreditsView.cs.draft.
They need OwnerTextAction.Create, OwnerPortraitArt and OwnerScrollColumn helpers,
then route adapters preserving prior code inactive. No more account-only broad
reruns unless a new change/failure justifies them. Full U0-U8UI remains active.
Latest pushed9faf6ba6 until this batch saved. No agents/Figma/reset/Desktop.
After UI, resume GAMEPLAY_RESUME_AFTER_UI.md. Owner says continue autonomously.

## CURRENT U1 render critique fixes under focused test

v2ran2tests: basic sign-in validation/back PASS; owner-art terms case failed on
ReadingSheet lacking CanvasRenderer. Actual signup/terms renders inspected:
correct original pattern/logo/field/tab art and font roles, but CREATE/GUEST text
truncated by Darumadrop line metrics, missing glyph/paper renderers, and old
NavigationSymbol restyling BACK into brown WorkSans. These are NOT accepted.
Added required CanvasRenderer to custom glyph/paper, single-line action text
overflow (keeps actual glyph size), and OwnerUiCanvas marker to exclude these
canvases from legacy navigation restyling. Strengthened capture checks for actual
custom geometry and visible button character counts, added Back font assertion.
v3 runs ONLY failed owner-art/terms case; Logs/owner-account-u1-v3.xml/.log.
No C#/imported edits until Editor exits. U2 draft is Logs/OwnerPlayView.cs.draft
and still needs OwnerPortraitArt + shared text-action helper integration. Do not
accept U1 until the corrected original captures are inspected and checks pass.
Source UI remains uncommitted; latest pushed9faf6ba6. Full plan/queue unchanged.

## CURRENT owner account/home wired; first actual UI checks running

U1 source now builds fresh owner-art account/home views through existing nonvisual
controllers. Original prior builders retained inactive. Source PNGs untouched.
Terms guidelines implemented/readable/scrollable with accept/back; owner authorized
copy. Optional contact email is local-only Settings.LocalContactEmail, separate
from public/cloud AccountProfile. Existing username/password/guest/Google callbacks
retained; password cleared on form close. New matching icons, tab artwork states,
source font/color roles, motion and reduced-motion support are wired.

Focused run owner-account-u1-v1 stopped at a test compile typo (OpenCreate versus
actual OpenForUpgrade), no runtime result. Corrected test to actual API and compare
the actual font asset reference. No production workaround. Running v2 TWO related
account cases: raycast/validation/back and original art/fonts/colors/terms/reveal.
Logs/owner-account-u1-v2.xml/.log. First expected captures under Logs/shots-native-ui.
No C#/imported edits during this Editor run; draft U2 Play/Credits/Loading outside
Assets. U1 not visually accepted yet; inspect rendered screens and fix issues.
Latest stable pushed9faf6ba6, UI source/art/docs still uncommitted. No agents,
Figma/reset/Desktop. Continue UI U1-U8, then GAMEPLAY_RESUME_AFTER_UI.md.

## CURRENT UI foundation compiled/imported; account/home implementation active

Owner-authorized full UI overhaul remains immediate, all other gameplay afterward.
Latest user: match font COLORS too; work autonomously until finished. Terms text
question answered: write it ourselves, do not wait on owner. Use short editable
fair-play/account guidelines grounded in real behavior, no fake service/legal promises.
Source text-colours.json now records #0F5913 selected tab, #A12E34 inactive,
#000000 typed/support, #BC8749 placeholders, #901219 CREATE/terms, #FFFFFF GUEST,
#C81721 hints. New OwnerUiTheme uses separate roles. Three fonts visually matched.

Added UNCOMMITTED OwnerUiTheme/Layout/Motion/PaintedAction/Backdrop/Glyph/Entry/
AccountTabArt and explicit OwnerUiArtAuthor. Author import5030 completedSUCCESS,
guard profile receipt collected; original PNGs imported unchanged/uncompressed and
theme asset assigned actual fonts. No active Unity/native run. No UI screen wired yet.
New account composition draft: Logs/SignInScreen.OwnerPainted.cs.draft. Must revise
its temporary terms handler with the newly authorized content and add real optional
email validation/storage without exposing it. Cloud account profile explicitly strips
Email; current provider auth uses username/password. Do not invent email login/reset.
Draft still needs active integration + Home replacement and actual visual/input checks.
Old SignIn Native methods should be retained inactive while new methods take over;
existing callbacks/domain/input contracts remain. SetBootMode currently overwrites
guest caption; make the owner-art path retain GUEST. Fix stale legacy theme colors
in account progress/error writers. Do not edit imported assets during Unity runs.
Latest pushed9faf6ba6. Full plan OWNER_HANDDRAWN_UI_PLAN.md. U0done/U1in progress.

## CURRENT owner-art UI U0 complete, U1 implementation next

Previous task finished/pushed9faf6ba6. Entire paused gameplay scope bookmarked in
GAMEPLAY_RESUME_AFTER_UI.md. Owner says work independently without stopping,
finish UI first, then everything else. No agents/reset/Figma/Desktop changes.
New authoritative assets preserved byte-exact under
ArtSource/ui/owner-handdrawn-2026-09-15. Source-manifest and sprite-regions record
PNG alpha/bounds/hash/palette. TUMP5 is transparent, not black. Native logo407x273,
actions413x91, field frames533x78/77/77. Never stretch or redraw her original art.
Font comparison visually matches KawitFreeExtItalic, Lydian and Darumadrop already
in Resources/UI/fonts; no new font download needed. Preview Logs/owner-ui-font-comparison.png.
OWNER_HANDDRAWN_UI_PLAN.md contains full source critique, implementation rules,
distinct component families, motion and U0-U8 screen inventory. U0 done; U1next.
UI/uGUI/game-ui skills and routed references read. Existing runtime uses legacy
uGUI Text/InputField and established input/domain adapters. Build new owner-art
views/components without reviving universal old visual builders; keep text editable.
Inspect PlayerAccount.Email storage/cloud contract before wiring the mock email
field; current username/password flow is functional, do not invent email auth or
expose email in public profile. No UI runtime code changed yet. No active tools.

## CURRENT previous task finished; new owner-art UI analysis/planning is next

Same-process HeroStrike arena-rejoin qualification PASSED. Normal MatchInstaller
world request restored all seven types; no production lifecycle patch needed.
Native57270 retired; build68123 retired; no active Editor/native tools. Report:
improvement-2026-09-15-session-cycle.md. Gameplay unfinished/partly verified work
is explicitly preserved in GAMEPLAY_RESUME_AFTER_UI.md at the owner's request.
Latest pushed478f4c99 until this diagnostic/documentation checkpoint is saved.

NEXT: thoroughly inspect the new TUMP(3)/(5)/(6) PNGs, archive byte-exact sources
and manifest, identify typography and sprite bounds/alpha, then write the complete
UI/button replacement plan. Use her actual art; do not redraw, stretch or blindly
reuse the previous generic builders. Real controls, coherent animation, usable
keyboard/controller/touch and editable text. Runtime is uGUI. UI/game-UI skills
read; specialized uGUI/references still to load. No UI implementation edits yet.
Resume gameplay ONLY after this new UI overhaul. No agents/Figma/reset/Desktop.

## CURRENT finish the in-progress lifecycle check, THEN new owner-art UI overhaul

Owner reopened UI on2026-09-15 using Downloads/TUMP (3).png background,
TUMP (5).png separated hand-drawn assets, TUMP (6).png composed account/main-menu
reference. Latest explicit clarification: FINISH CURRENT WORK FIRST. Wrap only
the already-started same-process reconnect check; then analyze her artwork and
thoroughly plan all UI replacement before implementing it. Resume the rest of
gameplay after UI. Preserve provided artwork and proportions, follow typography,
animate and make working clickable controls, choose email/password icons. No
agents/Figma/reset/Desktop. Older UI-cancelled scope is superseded by this request.

Latest pushed478f4c99 includes complete world fields. Current dirty files are
NetWorldFieldProbe.cs and tools/net_world_field_review.py adding actual same-process
transport restart/reload, no production lifecycle fix. Build68123 completed
SUCCESS, guardbc5324a9102c, internal RejoinLifecycleReview. Now running actual
three-player --rejoin fixture at Logs/net-world-rejoin-lifecycle-v1.
It disables diagnostic snapshot requests for the returning owner, so only the
normal recovery path can repopulate its fields. MatchInstaller already requests
world state after every arena build, which may cover the suspected lifetime flag.
Read actual result before changing production; retire tool, save scoped result,
commit/push, then start new UI analysis. Do not continue the entire old gameplay
queue ahead of this newly requested UI overhaul.

## CURRENT complete ground-field batch qualified; save then lifecycle work

Native55605 completed PASS: seven kinds on host and both clients, repeat without
duplicates, original parameters, final zero.37vs36 refusal PASS. Original
host/observer overview PNGs inspected: visible sheet/spires/crater/Hex match;
wide view occludes portions of other effects, no all-angle visual claim. Raw
timing maximum445ms within predefined550ms tolerance, not frame-identical proof.
All tools retired. Report improvement-2026-09-15-world-fields.md and receipts
world-field-evidence. Latest pushed2363065e until this world batch is saved.
Next final diff/stage/commit/push; investigate actual same-process reconnect
alongside pending windup state. Source hypothesis: MatchRpc._snapshotRequestStarted
is lifetime-scoped while messaging managers change per transport. Need actual
StartClientAsync rejoin reproduction before editing; do not assume a failure.
Reviewed source/fixture direction in Logs/same-process-rejoin-investigation.md.
No UI/agents/Figma/reset/Desktop. Continue full remaining queue, not a handoff.

## CURRENT mixed persistent-field native run and visual review

Source37 passed3focused Play checks (mixed seven types, prior ice and invalid/
expired replacement) plus5focused Edit checks (batch contracts, new-kind bounds,
protocol assertion). Sessions40526/38773 retired; guards3ed5618534a9/2c7719e018bb.
Internal WorldFieldReview build SUCCESS,49s build pipeline, guard03ee3aa4401a,
session40339 retired. Running55605 tools/net_world_field_review.py, output
Logs/net-world-fields-v1. Two native clients request/repeat the seven host-created
field types, with delayed owner link and original overview PNGs for inspection.
Next inspect JSON/PNGs,37vs36 refusal using PersonalBuffReview, record/commit/push.
Latest pushed2363065e. No production change to same-process lifecycle yet: source
hypothesis and planned reproduction in Logs/same-process-rejoin-investigation.md.
No UI/agents/Figma/reset/Desktop. Full non-UI scope remains active after this batch.

## CURRENT seven persistent field types under focused local test

Personal-effect checkpoint2363065e pushed. Baseline mixed-world test reproduced
the gap: seven live types, only two captured. XML Logs/mixed-world-baseline-v1.xml,
session26421 retired, guard13d58b64ea87. Extended existing atomic batch as
WorldEffectSnapshot (same meta GUID) with Fire/Shock/Crater/Hex/Fissure in addition
to Sheet/Barricade. Protocol37, WorldFieldBegin/Item/End,72 messages/0 mismatch.
Remaining clocks now survive Start on five components; fire/shock retain facing;
heat/wake/Hex/pillar art seeks mature age; Hex hydration is silent. No impact/cast
or resource replay and no host pulse-state reset. Current source is uncommitted.
Running ONLY IceWorldSnapshotProbe3 related Play checks, including new mixed
roundtrip/form/expiry and previous ice/invalidation. Logs/mixed-world-restore-v1.
Next inspect result, add focused new-kind validation, compile native field snapshot
fixture and verify one mixed live batch + old36 refusal. Do useful draft work
outside Assets during Editor run. No UI/agents/Figma/reset/Desktop. Queue continues.

## CURRENT personal-effect batch qualified; save then extend world snapshot

All runs retired:56473build,32262ward,31440plating,80873veil,26826fade.4native
cases pass after correcting an observer-authority expectation, with original
failure/raw trace retained. Protocol36vs35 actual refusal passed. Full receipts:
reports/improvement-2026-09-15-personal-buffs/README.md. Latest pushed9d0ee524,
current personal-effect source ready for final stage/commit/push. No active tools.
Important: native personal-buff fixture reconstructs observer kit and requests
real host state; NOT a cold process restart. Never claim otherwise. No buffs
lengthened. Existing held-slipper tag risk and remote motor authority preserved.
Next extend existing atomic ice snapshot to all remaining persistent ground
fields in one batch. Reviewed draft capture and concrete lifecycle notes in
Logs/WorldEffectSnapshot.cs.draft and Logs/remaining-world-effects-plan.md.
Include solid Dante pillars: DanteFissurePillar owns art/age/side; EarthPillar
owns lifetime and must respect restored remaining time. Do not duplicate familars,
Coven or sky, replay impacts/casts, or alter accepted art. Continue full non-UI
queue afterward. No agents/Figma/reset/Desktop. Deferred Inday and expansion order stays.

## CURRENT Dante/Nemu remaining personal effects under native qualification

Zack checkpoint9d0ee524 pushed to ASTRAReworks. No UI/agents/Figma/reset/Desktop.
Added initial-only Dante armor and Nemu veil restoration, retaining remaining
duration, selected sidegrade slow and mature existing visual. No replayed surge,
cast sound or resource spend. Renamed the packet TimedKit, protocol36.
Focused Play checks: initial3cases2passed/1test expectation failed. The failure
wrongly expected tag immunity while holding a shoe; actual existing rule remains
vulnerable while held. Corrected the test to verify vulnerability while carrying,
immunity after disarm and cancellation on new pickup; rerun1/1PASS. No production
change to that carry rule. XMLs personal-buff-joining-v1 and personal-buff-veil-v2.
Protocol36 assertion1/1PASS. Sessions12524,49110,76032 retired.
Building INTERNAL PersonalBuffReview, Logs/personal-buff-build-v1.log. New opt-in
NetPersonalBuffProbe reconstructs only observer's local kit then requests the real
host snapshot. This is separate-player transport proof, NOT a process-restart
claim, and does not extend the real2.5second Veil duration to cover boot time.
Next tools/net_personal_buff_review.py cases ward/plating/veil/fade sequential,
then36vs35 refusal using preserved ZackBuffRejoinReview. Draft work outside Assets
while Editor builds; continue persistent effects and other game queue after batch.

## CURRENT Zack joining windows qualified; save batch then Dante/Nemu

All tools retired:51289build,89421Magnet observer,60733storm observer,29001Magnet
owner. Protocol35vs34 actual refusal passed. Three native results plus exact
expiry rechecks all PASS. Report/CSV/XML in improvement-2026-09-15-zack-buff-rejoin.
Returning owner332ms initial hydration then74/74 live/visual; returned observers
89/89 and47/47. Maximum absolute peer expiry offset184ms at150ms one-way link.
Source reviewed; latest pushed165ac7bf until this batch is saved. No other Unity
or native run active. Next diff/stage/commit/push, then Dante/Nemu remaining
personal buffs from reviewed Logs draft, focused grant/pickup/cleanup checks.
No UI, agents, Figma, reset credits or Desktop update. Full non-UI queue continues.

## CURRENT Zack native Magnet restore passed; storm reconnect running

Latest stable pushed HEAD165ac7bf, ASTRAReworks on the Matthew laptop checkout.
Protocol35 focused assertion passed1/1, session58694 retired, guard9efe8bd5657d.
Corrected internal ZackBuffRejoinReview build completed, session51289 retired,
guard1e3906dbc930; Runtime SHA99958c1251710c8c7797c04ff70fd2bf086bfb3cf6f9c16b32d08bb32f0145e7.
Magnet actual observer reconnect150ms passed: returned89/89 live/visual samples,
46ms initial hydration, no extra resource or effect after expiry. Session89421
retired. Running storm equivalent60733, Logs/net-zack-buff-storm-corrected-v1.
Next read result, protocol35vs34 refusal using preserved BuffRejoinReview, save
scoped receipts/commit/push. Active source remains Zack joining state only.
Drafted Dante/Nemu restore and focused contracts outside Assets in
Logs/PersonalBuffRestore.draft.cs.txt during build/native waits. These are NOT
implemented. Never extend short real buff durations to make a reconnect test pass.
UI cancelled; no agents/Figma/reset/Desktop. Continue full non-UI queue afterward.

## CURRENT Zack windows local4/4pass, protocol35check

43467completed4/4: independent remainingE/ult clocks, no recall/strike/spend,
consumedEcannotrearm or suppress restoredultimate, existingelectric consumption
andSean staleguard remainvalid. XMLpreservedin zack-buff-rejoin.
Running protocol35assert, Logs/zack-buff-protocol35-v1.xml/.log, then fresh
ZackBuffRejoinReview35build and two actualdelayedwindowrejoins.
No C#/imported edits while Editoractive; noUI/agents/Figma/reset/Desktop.


## CURRENT Zack independent joining-window restore under local test

Both realbaselines failed: returned Magnet0/92active/visual; returnedStorm0/54,
while host/owner windows intact. Implemented reviewed separate joining guards
and clock-only E/ultimate restore, no recall/strike/cost. ConsumeMagnetCharge
settles only E. HeldCharge packet now includes separateultimate remaining and
Sean remains supported; protocol35required.
Running4focused cases: two independent/stale window tests, existing electric
consumption/ult separation, existing Sean stale/newcastguard.
Logs/zack-joining-windows-v1.xml/.log. After localpass, protocolassert/new35
player and actualtwo windowrejoins plus34-vs35refusal. Sourceuncommitted,
latestpushed165ac7bf. No UI/agents/Figma/reset/Desktop.


## CURRENT Zack two-window baselines collected

Magnet observerrejoin baseline failed: returning observer0/92active/visual
samples while host/owner retainedcharge. Both baseline cases are preserved in
improvement-2026-09-15-zack-buff-rejoin; inspect storm result before acceptance.
59931/67929retired. Now apply independently guarded Magnet and activeThunderstrike
window restore from reviewed draft; no recall/strike replay, no resource spend.
Add remaining-storm field toheldstate schema, require newprotocol35, preserve
Sean currentbehavior and captured scopes. Localstale/expiry/independence then
newnativebaselines beforepush. Latestpushed165ac7bf.


## CURRENT Zack Magnet held-state baseline running

Baseline34ZackBuffRejoinReview built,10686retired guard825383e8c731. Running
net-zack-buff-magnet-baseline-v1 with held charge + observerreconnect150ms.
Next Thunderstrike held-window baseline. Drafted actual independent E/ultimate
restore methods in Logs/ZackJoiningCharges.draft.cs.txt while native tools run.
No production change yet. Consume E must not cancel/block independent ultimate
tail; no recall or lightning replay. Pending ultimate windup is distinct from
the active tail and not claimed covered. Latestpushed165ac7bf.


## CURRENT Zack joining-charge/window baseline preparation

Sean fix committed/pushed165ac7bffbde17dd20b3e1938e4deac87993fb7e, protocol34.
Extended only Zack diagnostics/harness: hold charge/window without throwing,
shared wall clock, reconnect same owner/observer profile with observe-existing
(no meter/position/pick/input reseeding). Cases magnet and thunderstrike.
Building INTERNAL ZackBuffRejoinReview baseline34,
Logs/zack-buff-baseline-build-v1.log. No production Zack restore yet.
After build actualmagnet/ult-windowrejoin baselines sequential on8990/8991.
Draft restore-only separate E/ultimate clocks while build runs; never recall
or replay lightning when hydrating. NoUI/agents/Figma/reset/Desktop.


## CURRENT Sean owner hydration verified with explicit reply window

60202owner run restored charge at331ms afterarena-ready, then84/84subsequent
live samplescorrect; resources/expirycorrect. Originalevaluator wronglyallowed
only3missing samples (~150ms), below configured300msrequestroundtrip. Updated
check allows configured2*delay+.25s onlyforrejoinedseat, then requirescontinuous
state. Same trace recheckPASS, originalfailure retained. No production workaround
or rerun. Evidence corrected-owner includesbothreports/rawCSV.
Next finaldiff andsaveSean joiningbuffbatch; all toolsretired. Then remaining
Zack/Dante/Nemu timedstate and othernon-UI queue. Latestpushedc6b86a1f.


## CURRENT Sean controlling-owner reconnect

Owner-observation diagnostic buildSUCCESS;53895retired. Protocol44143completed
PASS and closed its native processes before player export. Now same held fire
charge scenario reconnects OWNER with observe-existing enabled:
Logs/net-held-buff-owner-v1. It must retain charge/ember and spent resource
without fixture reseeding/recasting. Production remains same locallychecked
Sean restore path; source stilluncommitted pendingthisactualcase.
No current Editor; no UI/agents/Figma/reset/Desktop.


## CURRENT Sean observerrejoinPASS, owner scenario prepared

77673observerrejoinPASS: returned client94/94live-window charge/ember samples,
correctspentresource1andexpiry. Runtime49aa89a46a2f78e3ee2957e4470b4a135f48829d2ac98cd17a9cb0209d5d60ff.
Actual34host/33clientrefusal alsoPASS. Preparing ownerrejoin with explicit
observe-existing flag so restarted owner cannot reseed meter/position/picks or
driveanothercast. Harness supports --rejoin-seat owner. Newinternalbuild is
Logs/buff-rejoin-owner-build-v1.log, same production code plus diagnosticflag.
Next runheld-chargeownerrejoin150ms, preserve resource/remainingstate. No source
edits while Editor runs; noUI/agents/Figma/reset/Desktop.


## CURRENT actual corrected held-charge observer reconnect

Corrected34BuffRejoinReview buildSUCCESS;16780retired. Now exact original
held-charge+observerrestart150ms scenario in Logs/net-held-buff-corrected-v1.
Need live charge/ember after rejoin, originalexpiry and no extra resource spend.
No Editor active. After passing, run34host/33clientrefusal and save batch.
Scope remains Sean initial hydration; other buff/world states stillopen.
No UI/agents/Figma/reset/Desktop.


## CURRENT corrected34 BuffRejoinReview build

Protocol34assert1/1pass;5135retired. Building corrected INTERNAL BuffRejoinReview,
Logs/buff-rejoin-corrected-build-v1.log. Then same held-chargeobserverrestart
150ms case and actual34-vs33refusal using preserved IceRejoinReview33.
Sean initial hydration settles once; real activation or first accepted fire
release also settles it, so late joining records cannot overwrite newer actions.
Local3cases cover this, but realcorrectedpeerproof stillpending.
Dante/Nemu/Zack restore paths inspected independently while tools ran; do not
claim them implemented. No C#/imported edits during currentbuild.


## CURRENT Sean joining-charge local3/3passed

63844completed3/3: remainingwindow/ember/resources restored, duplicates cannot
extend/rearm, accepted fire consumption/newer real cast blocks stale hydration,
existing repeated fire-snapshot consumption remainscorrect. No activation replay.
Running protocol34assertion, Logs/held-charge-protocol34-v1.xml/.log; then
newinternal BuffRejoinReview build and held-chargeobserverreconnect150ms rerun.
This is initial joining Sean-state hydration only; other buffs remain separate.
No Editor-source edits while tests run. Latestpushedc6b86a1f; noUI/agents/resets.


## CURRENT Sean held-charge restore implementation under test

Added restore-only clock/flag/ember hydration for a freshly joining Sean kit.
One-time settling prevents a duplicate/stale joining record rearming a consumed
shot or overriding a newer real cast; Carrier and first replicated fire release
mark consumption explicitly. No resource spend/cast replay. New HeldCharge
message targeted from HostSyncPeer, round/hero/finite/remaining validation.
Protocol34required; other timed buffs still NOT handled by this message.
Running only3Sean tests: remainingwindow/resources/duplicateexpiry, consumed/new
cast protection, existing replicated fire consumption/cleanup.
Logs/held-charge-restore-v1.xml/.log. No imported edits during Editor.
Next protocolassert, newinternal34player, actualheld-chargeobserverrejoin and
33-vs34refusal. Latestpushedstablec6b86a1f; noUI/agents/Figma/reset/Desktop.


## CURRENT held-charge reconnect failure confirmed, no live tools

29839baseline completed FAIL: host77/77 and owner76/76live-window charge/ember
samples, returned observer0/92. Resources stillcorrect1, effect missing until
originalexpiry. RawCSV/result preserved under improvement-2026-09-15-buff-rejoin.
Baseline runtime0f52958ed52b467a283925dc2020dcfdf6cfc1f3c0bc71771852553c7e028a18,
BuffRejoinReview. Guard01790cb3aa43 protectedbuildprofile. No active Editor/player.
Current uncommitted changes ONLY NetSeanProbe held-charge diagnostic, its Python
runner reconnect support and this ledger. No production buff restoration yet.
Next add restore-only Sean charge clock/flag/ember path without Activate or
resource/animation replay, related local restoration/expiry/consumption tests,
then targeted initial-world state message and actual reconnect. Consider stale
state vs a newly consumed/reloaded charge; never blindly replay OnActivate for
other heroes or claim all buff cases fixed. Dante/Nemu/Zack and other worldfields
remain in the queue. Latest pushedstablec6b86a1f; protocol33currently.
Full non-UIqueue continues; no agents/Figma/resets/Desktop.


## CURRENT held fire charge observer reconnect baseline

BuffRejoinReview baseline buildSUCCESS;8150retired. Running NetSean held charge
(no throw) plus actual observer restart,150ms owner link:
Logs/net-held-buff-baseline-v1. Need host still charged during returned observer
window, visible ember, same charges and original expiry. No production buff
restore change yet. A restore-only Sean draft is in Logs/held-charge-restore-draft.txt
while tool runs; correct inner class confirmed IgnitionCannonAbility.
Other timed buffs need explicit grants/clock restoration, not replaying casts.
Keep all localprofiles protected, noUI/agents/Figma/resets/Desktop.


## CURRENT active-buff reconnect baseline after pushed c6b86a1f

Ice world snapshot committed/pushed c6b86a1f4a39a70c3cc5c0efaddcf92da5a75534.
Current work: NetSeanProbe optional hold-charge flag and shared-clock/remaining
columns; net_sean_review.py supports held charge + observer reconnect. No active
buff production fix yet. Building INTERNAL BuffRejoinReview baseline33 from
current code, Logs/buff-rejoin-baseline-build-v1.log. After build run
net_sean_review.py --case ignite --hold-charge --reconnect --delay150.
Source SyncAbility omits active duration/flags; expect returning peer may miss
loaded ember. Reproduce first; draft restore-only state paths while build runs.
No C#/imported edits while Editor active; noUI/agents/Figma/resets/Desktop.


## CURRENT live ice rejoin PASS; saving qualified batch

81701corrected native rejoinPASS on33runtimecacf88c25a636cd20dd9cecd2a5ab1a81954c24c350ca85da7330e4d216ec95b.
Returned observer154samples sees both fields, matching positions/resources and
expiry; host had31sheet/95wall samples after it was ready. Source missing-state
failure was0/0fields, retained.8namedprofilefiles preserved. No native jobs
remain after protocol33-vs32refusal tool completes. Localrestore3/batch3/
protocol1allpass. Runtime buildguard2f6be9bb58a2. IndayFppDetails diffs verified
whitespace-only, backed up/restored, no deferred geometry/framing change.
Next save/push, then activebuff-state reconnect audit (cooldown-only SyncAbility
misses active flags/durations), plus other persistent fields and entire non-UI
queue. Do not claim those covered by current ice snapshot.


## CURRENT actual protocol33 ice reconnect

IceRejoinReview buildSUCCESS;60494retired. Running new33player both+150ms+
observer reconnect in Logs/net-ice-rejoin-v2. Same profile, hostlivewindowchecked.
Next actual33host/32clientrefusal if restored-world check passes.
Build introduced Inday FppDetails mesh diffs; inspect/restore ONLY proven tangent
import noise. Do not change deferred Inday framing or source geometry.
No Editor active. New active-buff gap from cooldown-only SyncAbility recorded
for next follow-up; do not mistake current ice snapshot for a buff fix.


## CURRENT protocol33 IceRejoinReview build

Batch3/3pass,41709retired; restore/traction3/3 andprotocol1/1already pass.
Authority audit0ungated other-body effects, wire71messages0mismatches.
Now guarded INTERNAL Builds/IceRejoinReview/TumbangPreso.exe,
Logs/ice-rejoin-build-v1.log. Keep32IceNetworkReview for actual mismatch test.
Next rerun exact both+150ms+observerreconnect using new33player and same named
profiles; require host live window and matching restored fields/resources/expiry.
No C#/imported edits during build. Draft/review remaining buff/field state paths
while it runs. New report reports/improvement-2026-09-15-ice-rejoin.md records
baseline/design/local scope, not a native success yet. Latestpushedced13a20.


## CURRENT ice restore local3/3passed, batch assembler next

27121completed3/3pass: remaining life/shape/resources and mature frost restored,
no duplicates/revived expired state, invalid snapshots preserve world, old traction
clears immediately. Source confirms SyncWorld and PlayAbility use default same
ReliableSequenced channel as new batch. No cross-fragment-channel assumption.
Running only3IceSnapshotBatchTests, Logs/ice-snapshot-batch-v1.xml/.log. Then
fresh INTERNAL IceRejoinReview33build and actualcoldrejoin/32-vs33refusal.
Latest pushedced13a20; snapshot code remains WIP until native proof.


## CURRENT ice restore/traction focused tests

Protocol33assert1/1passed,93120retired guard2e9d81073013. Snapshot/assembler
tests were drafted outsideAssets while that compiled, then installed. New
transport messaging registration resets ice generation tracking for another host.
Now3Playcases: restore remaining ages/shape/no duplicate/resource change, invalid/
expired snapshot safety, and existing traction with immediate deactivation release.
Logs/ice-world-snapshot-v1.xml/.log. Pure3assembler Edit cases follow.
No production acceptance yet; current namespace tests/realcoldrejoin/new33build
and32-vs33refusal required. Latest pushed stableced13a20. No agents/UI/resets.


## CURRENT 2026-09-15 persistent ice snapshot implementation WIP

Rejoin baseline14560 FAILED as expected: returned observer saw0sheet/0wall
while host retained18sheet and79wall samples after it was ready. Rawrecords
preserved under ice-rejoin-evidence/baseline.8namedprofiles restored.
Now added UNVALIDATED IceWorldSnapshot capture/bounded batch/atomic restore;
remaining timers and mature frost appearance retained, silent restoration,
immediate old traction release. HostSyncPeer sends IceBegin/IceItem/IceEnd
small packets on same reliable channel as PlayAbility. No player/cast/resource
replay. Round/scene/generation validation rejects stale/incomplete sets.
Protocol33required (new live collision state); previous32IceNetworkReview retained.
Wire audit71messages0mismatches. Running ONLY protocol assertion/compile,
Logs/ice-snapshot-protocol33-v1.xml/.log. Prepare targeted snapshot/batch/expiry
tests outsideAssets while this Editor runs. Need lifecycle reset inspection,
local proofs, newinternal33build, actualrejoin and32-vs33refusal before commit.
Latest pushed stable remains ced13a20. UIremoved/noagents/Figma/resets/Desktop.


## CURRENT persistent ice reconnect baseline after pushed ced13a20

ced13a20b8a2e114cf2f04a5acada74f19637f3d committed/pushed, clean before this
new harness extension. net_ice_review.py now supports reconnecting observer2
with the same named profile after seeing the first sheet. Keeps observer-before
trace and checks host still had live ice after rejoined observer was ready.
Running current32IceNetworkReview both+150ms+reconnect under
Logs/net-ice-rejoin-baseline-v1. No production snapshot change yet.
Source HostSyncPeer only restores familiar/Coven/sky, not static ice. Need actual
missing-field proof before implementing bounded targeted world snapshots.
Prepare design/code outsideAssets while this runs; no more agents/UI/Figma/resets.
Full non-UI queue still active, Inday later, selected expansion last.


## CURRENT Cheska qualified batch ready to save

BlackIceFooting1/1pass,48384retired guard0345c0672f7c. Radius/edge and stronger
actual movement penalty verified; no compulsory trip, clean cleanup. Applied
truthful gameplay copy; Core build0warnings/errors. Full result and exact
receipts in cheska-network-review.md. Seven ice contracts, protocol1, related
Phaister2, spires/defaultwall2, spiresarena1, footing1 allpass. Actual delayed
accepted/denied/pair and32-vs31refusalpass. Sharedclockdiagnostic repaired with
fresh runs; previous failure preserved. All old native/Editor sessions retired.
Next commit/push coherent batch, then persistent fields/rejoin (ice and other
nonfamiliar effects), broader mixed-kit/state/input/movement/match/spectator/
engineering, deferredInday, expansionlast. UI remains REMOVED. No agents/resets.
Native09ca7b6a includes final physics/geometry; later wording-only Core change
not rebuilt into it. No Desktop update.


## CURRENT final Black Ice footing check

Both shared-clock99128completed; exact result copied to net-both-shared-clock.
Pair shared-clock alreadyPASS. Native jobs retired. Running only drafted
BlackIceFootingProbe, Logs/black-ice-footing-v1.xml/.log. It measures actual
outer-lane/slow tradeoff and releases traction/slow on field removal.
Need check result then apply drafted truthful Black Ice copy if confirmed.
After this, summarize/commit/push the qualified Cheska world confirmation/pose/
SplitSpires/clock diagnostic batch, then continue persistent fields/rejoin and
remaining non-UI game queue. No Editor-source edits while current test runs.


## CURRENT pair shared-clock PASS; both timing rerun

56648pair shared-clock runPASS on runtime09ca7b6a497a7d27e7153e9b8500cd6006b69de1f5db855ae020ff4e89a804e6.
392/410/399samples, earlier sheet preserved through later refusal, position
agrees, no preacceptance field, all expire; sharedwallclockexpiry spread214ms
with150msonewayowner link. Old buffered-clock falsefailure stays archived.
Now net-ice-both-wallclock-v2 validates fresh shared-clock sheet+wall.
Drafted BlackIceFootingProbe now installed (not run), copy candidate outside
imported folders. Run only that footing check after native peers finish.
No current Editor; no agents/UI/Figma/usage resets/Desktop.


## CURRENT fresh shared-clock pair run

IceNetworkReviewv2 build SUCCESS;99040retired. Running sheet-then-denied at150ms
with new shared UTCwallTime, Logs/net-ice-pair-wallclock-v2. Then both case for
new timing evidence. Prior refused-both/native protocol mismatch proofs retained.
Spires accepted owner screenshot/timed video and2casephysics proof copied into
cheska-network-evidence. OBJ bases are exactlyY0; retained grounding is intact.
BlackIceFooting draft ready to install after native checks; no Editor currently.


## CURRENT spires visible/physical accepted; rebuild with shared clock

Spires2/2pass: default3slabs blocks body/shoe, alternate2slabs passes both;
sidefaces still block.76657retired guard3fa9af22ecdc. Actual arena selected
variant capture1/1pass,66017retired guard4fe0eca46985; owner00023.jpg shows the
clear gap using retained ice art. New optional review variant args do not change
normal gameplay. Current99040build is IceNetworkReviewv2 with shared-clock
NetIceProbe and split geometry; Logs/ice-network-build-v2.log.
BlackIceFootingProbe drafted outsideAssets while build runs. It checks outer
lane sacrifice, actual stronger slow, clean exit and absence of compulsory trip.
Current Black Ice copy overclaims nobody can keep their feet; correct only if
verified, as gameplay copy, not a UI overhaul. Next pair/both shared-clock runs
then targeted footing and final batch save. Keep full non-UI queue active.


## CURRENT Split Spires drafted fix applied after reproduced failure

33400baseline failed: default3slabs blocks body/shoe; alternate ALSO3slabs blocks
both. Guard4503c157cc03 restored. Applied exactly the3externally drafted source
changes (verified byte-normalized equality): explicit split flag through kit and
factory, omits centre slab only for alternate. Default retained. Added side-face
blocking assertions. Running ONLY passage + original wall/thaw2cases:
Logs/spires-passage-v2.xml/.log. No C#/asset edits until finished.
Next actual arena visual review of two slabs, fresh IceNetworkReview build with
shared clock, rerun accepted/pair timing cases. Actual32-vs31refusal alreadypass.
Source still uncommitted; code checkpoint only after qualified. UI removed.


## CURRENT Split Spires baseline while ice timing probe compiles

Actual32host/31clientrefusal PASSED, receipt protocol32-vs31.json copied.
No native jobs remain. Installed drafted SplitSpiresPassageProbe and running
ONLY that baseline, Logs/spires-passage-baseline-v1.xml/.log. Current production
still3slabs for both variants; candidate correction remains outsideAssets.
This Editor also compiles NetIceProbe shared-clock trace addition. After baseline
observe/repair the gap and relevant wall regression, then build one internal
player for fresh shared-clock accepted/pair checks plus current alternate render.
No patch accepted merely because drafted; preserve default art/body collision.


## CURRENT ice timing-diagnostic correction, not a gameplay failure

55112pair ended with one evaluator failure: owner first buffered ServerTime
sample75msbefore host first. Actual counts/positions/late-denial/expiry all correct.
Each peer ServerTime has buffering, and first observed presence is only an interval
bound. Preserve failure/rawCSV in net-pair-network-clock; do not claim timing pass.
NetIceProbe now adds same-PC UTCwallTime; evaluator requires fresh traces and
compares owner presence to host last-absence bound. No gameplay edit for this.
Need rebuilt IceNetworkReview and rerun pair (and fresh timing evidence both).
Current protocol32vs31native refusal running Logs/ice-protocol32-vs31-v1.
Split Spires correction+passage regression drafted outsideAssets, not applied.
Latest user says keepcoding/drafting while tools run, qualityfirst, entire
remaining non-UI queue while asleep. Saved in AGENTS. No agents/resets/Figma.


## CURRENT ice denial passed; pair case running; next change drafted

14867denied both PASS:381/396/392samples, owner predicted then received denial,
zero sheets/walls on all3peers throughout; resources ended0.7profiles preserved.
Now Logs/net-ice-pair-v1 checks accepted sheet followed by denied second sheet.
While native tools run, Split Spires correction is drafted outside Assets using
Logs/draft_split_spires.py, producing three *.split-candidate.cs.txt files. They
are NOT applied; reproduce the currently blocked promised passage first.
User explicitly reaffirmed useful parallel drafting while tools run, quality
first, continue entire remaining non-UI queue while asleep. No agents/resets.


## CURRENT accepted ice peers passed; denial running

1101finished PASS: protocol32runtimea256b551c230052bbe4b29e1f1bb4714d3f07785de89628ec33b6f3bcbed0752.
Host/owner/observer380/395/389samples, accepted sheet+physical wall exactly once,
zero position disagreement despite moving owner, no extra charge spent, clean
expiry. Sampled network-clock expiry spread38ms/7ms, not external wall-clock proof.
Raw evidence copied to cheska-network-evidence/net-both. Guard7fe7b22f69c9 belongs
to successful IceNetworkReview build. Now net-ice-denied-v1 runs both refusals at
150ms, then sheet-then-denied and32-vs31refusal. No Editor active.
Additional source finding for later alternate pass: BuildWall always makes3slabs
even for Split Spires, whose copy promises2and a passage. Reproduce gap before
changing native art; retain default wall. No UI work reactivated.


## CURRENT actual protocol32 ice peers

IceNetworkReview build SUCCESS;18015 retired (guard receipt in tool result).
Now tools/net_ice_review.py case both,150ms, Logs/net-ice-both-v1. Three peers
use9010/9011and icehost/iceowner/iceobserver; keep exclusive. Next denied then
sheet-then-denied, and net_protocol_refusal.py new32host vs FamiliarLaptopReview31.
No current Editor; source/fixtures still uncommitted. No production acceptance
from the seven local tests alone. UI remains removed; c6a59400last pushed.


## CURRENT IceNetworkReview32 build

Phaister Hex/SlowBrand + Blink/LongStride2/2pass;84472retired, guard605ccfc24fbb.
Protocol32assert1/1andCheska7/7already pass. Wire audit passed. Added grounded
cast-confirm flash using the same accepted pose; predicted ice no longer flashes
a false landing confirmation before host acceptance. No UI overhaul resumed.
Now guarded INTERNAL Builds/IceNetworkReview/TumbangPreso.exe build32,
Logs/ice-network-build-v1.log. Keep31FamiliarLaptopReview. NetIceProbe compiles
from prior test run; harness cases both/denied/sheet-then-denied at150ms ready.
Run them sequentially after exact new build identity is verified, plus real
32host/31clientrefusal. Do not commit production until this qualification.
No C#/imported edits during build; docs/probe evaluation prep can continue.


## CURRENT shared-aim regression; NetIceProbe installed

Protocol32focused Edit1/1passed, session66961 retired, guardf1ac0256dedc restored.
NetIceProbe now installed from draft with generated32hexmeta; tools/net_ice_review.py
prepared/syntax checked. Explicit cases both/denied/sheet-then-denied,9010/9011,
icehost/iceowner/iceobserver profiles; run sequentially. Denial fixtures deliberately
seed only the owner stale charge estimate while host keeps0. No real peer pass yet.
Now running ONLY Phaister Hex/SlowBrand and Blink/LongStride existing2cases because
shared AimedDestination changed. Logs/ice-shared-aim-regression-v1.xml/.log.
Then build INTERNAL IceNetworkReview32; keep FamiliarLaptopReview31 untouched
for mixed-version test. Current code uncommitted; newest pushed c6a59400UI removal.
UI remains removed; no agents/other chats/Figma/resets/Desktop.


## CURRENT ice protocol check and real-peer probe preparation

UI removal committed/pushed c6a59400; UI work stays CANCELLED. Cheska world/
captured-pose fix remains uncommitted, local7/7pass. Running ONE related Edit
case TheProtocolCarriesEveryRosterBump, Logs/ice-protocol32-v1.xml/.log.
Prepare NetIceProbe outside imported Assets while Editor runs. Need actual
accepted sheet+wall, denied both, and accepted-sheet/later-denied-sheet cases
at150ms; preserve earlier accepted fields, exact accepted positions, charges,
colliders/expiry. New32host vs retained31FamiliarLaptopReview refusal also needed.
No new packet fields, but old-host owner-confirm behavior is incompatible.
No agents/Figma/resets/Desktop. Continue the main non-UI queue.


## NEWEST owner scope: remove UI; continue Cheska

Owner explicitly removed remaining UI from the active to-do list. Full brief
archived in ui-queue-removed-by-owner.md; UI_REMAINING_TODO now states cancelled.
Do not execute older UI-LAST reminders. Preserve implementation and source.
Gameplay/skills/network/movement/spectator continue, Inday later, expansion last.
Cheska new confirmation/pose7case run69493 PASSED7/7; session retired, profile
guard46e0a494dcc8 restored files/preferences. Current code is still DIRTY and
not qualified in real peers. Next protocol/aimfocused checks, add NetIceProbe,
build internalprotocol32and actual accepted/denied owner ice plus31-vs32refusal.
No live Editor/player. Do not end the main task after this scope update.


## CURRENT Cheska confirmed-world implementation under test

Baseline33519 completed0/2: both rejected fields survived, real ice placement
missed captured pose by19.698m. Guard5e1840da0ea5 restored; failures preserved.
Now implemented DIRTY/unaccepted: instant ice effects opt out of local world
prediction, owner receives existing PlayAbility confirmation and creates payload
without spending/replaying cast. Earlier accepted fields are not erased by denial.
Shared AimedDestination uses captured context. Protocol32 plus focused assertion
updated because old hosts omit required owner confirmation. Phaister sky path
retained. No new packet fields; actual version/refusal/peer qualification pending.
Running7focused CheskaIceContractProbe cases (original wall/thaw and mash, two
reproduced failures, accepted independent sheets/wall collision, delayed lightning
pose). Logs/cheska-confirmation-v1.xml/.log. No edits while Editor active.
Next inspect result, then protocol/aim regressions, fresh internal player, actual
delayed ice accepted/denied and31-vs32refusal. Retain FamiliarLaptopReview31.
Latest pushed safe source remains a029f96e; do not mark this batch complete yet.


## CURRENT Cheska baseline fixture correction

43925 exited at compilation, no XML: fixture tried to assign internal-only
HeldSecondsOnCast. No production compile defect. Guard17b90caba518 restored.
Fixture now uses real ApplyNetworkCast(...heldSeconds:.55,authoritative:false)
for the captured-pose case, no reflection/access-level production change.
Running same2targeted baselines under Logs/cheska-refusal-pose-baseline-v3.
Collect new run before source edits; confirmation/pose design remains unapplied.


## CURRENT Cheska second baseline and saved correction design

First refusal regression failed as expected: sheet1/wall1 survived callback.
Session12357 retired; guard81e5613fb0ec restored files/preferences. Now43925 runs
explicit predicting-owner refusal plus actual moved-caster ice placement,
Logs/cheska-refusal-pose-baseline-v2.xml/.log. Collect this session before C#/
imported edits. No production change or protocol bump yet.
Detailed proposed safe confirmation design, implications and exact next tests
are saved in cheska-network-review.md. Prefer accepted-world-effect opt-in over
fragile newest-object deletion. Requires owner PlayAbility acknowledgement and
Protocol32 because old hosts omit it; preserve Phaister sky confirmation.
AimedDestination also needs to consume captured context rather than live body.
Keep protocol31 native FamiliarLaptopReview for actual mixed-version refusal.
Continue all work here; no handoff/agents/Figma/reset/Desktop.


## CURRENT Cheska instant-field refusal reproduction

Nemu batch a029f96ed37b4fcc7d4a09e56d43347882ee94a7 committed/pushed, clean
before this fixture addition. Now one targeted regression in CheskaIceContractProbe:
RefusedInstantIceCastsRemoveTheirPredictedWorldEffects. It directly exercises
activation/refusal callbacks for zero-duration skills with separately timed world
effects, not network packets. Logs/cheska-refusal-baseline-v1.xml/.log plus
cheska-denied-ice.csv. No production changes for Cheska yet; observe first.
Hypothesis: HeroAbility.CancelActive returns on DurationRemaining0, so no cleanup
for sheet/wall. Ice casts have no owned references/cancellation override.
Network messages currently lack per-cast identity; do not naively destroy an
earlier accepted field or a newer prediction on a late refusal. Inspect actual
request/confirmation/denial path before selecting a safe minimal design.
One Editor active, no C#/imported edits until complete. Continue full queue.


## CURRENT Nemu verified; next Cheska remaining acceptance

Nemu local7/7 and corrected-light/alternate3/3pass; delayed staged and owner
reconnect both pass.60138/57837/20472/35384/76350 all retired. Last profile guard
72de2ebbf5f2. Source change only possession point light7m/4 ->1.25m/.55.
LongFade movement/time and ShortLeash flight/time tradeoffs verified; mini and
monster retained. Full evidence/critique/limits in nemu-laptop-review.md.
Save this stable batch, then continue remaining Cheska alternate/counterplay and
instant field refusal/expiry checks. Source inspection suggests predicted ice
wall/sheet may survive a denied zero-duration cast; reproduce before changing.
Preserve existing ice art and Nova loose-shoe fix. Then wider mixed-kit/state/
movement/network/spectator/UI/Inday/last-last expansion queue; no agents/resets.


## CURRENT Nemu delayed peers pass; local alternate/light check

Staged57837 and owner-reconnect20472 both complete/PASS on runtime a1f37b4e.
Rejoined owner produced56livefield samples, matching position and spent charges,
then clean expiry.8namedprofilefiles preserved perrun. Full records copied into
nemu-laptop-evidence/network-staged and network-reconnect. No native jobs remain.
Now running2new alternate motion/expiry tests + only Nemu E actual capture.
Logs/nemu-laptop-alternates-v1.xml/.log; images Logs/nemu-possession-light-v2.
Possession light change1.25m/.55 is the only production delta since867f7fc0.
No Editor-source edits until completion. Need inspect outputs before accepting.
Full remaining queue continues; no agents/other chats/Figma/reset/Desktop.


## CURRENT Nemu light/alternate review and native staged check

867f7fc0 INTERNAL FamiliarLaptopReview built successfully, guard0e76334a68c7;
build session35384 retired. Actual delayed staged familiar check currently
session57837 completed PASS, Logs/familiar-laptop-staged-v1:468/391/393samples,
zero model/anchor/yaw disagreement,~54msfield expiry spread. Guard preserved8
profile files. Runtime a1f37b4ebf74dbfaa2e7b599bf524c442ee5ddcd4b65cefbe0bd0e17c96cca6e.
Now actual150ms owner reconnect during ultimate, Logs/familiar-laptop-reconnect-v1.
These built checks use original possession lighting; local light change pending.
Nemu7/7local and ordinary captures passed. Visual critique: retained giant reads
clearly in real owner view; possession bathes cars/paving in purple from a7m,
intensity4point light. Reducing only that light to1.25m/.55; native mesh retained.
Two real movement/expiry alternate tests added (LongFade/ShortLeash). Do NOT
claim light/alternate acceptance yet. Need selected E recapture and2tests after
current native peers finish. No Editor active; no agents/Figma/resets/Desktop.


## CURRENT Nemu local checks complete, native build

Nemu7/7 passed, session76350 retired, guard96b83b186ef9 restored files/input prefs.
Actual3slot capture accepted every cast; Logs/nemu-laptop-kit-v1 contains owner/
observer frames and measured timing. Selected six familiar contracts also passed.
Now guarded INTERNAL Builds/FamiliarLaptopReview/TumbangPreso.exe build from
867f7fc0; Logs/familiar-laptop-build-v1.log. Encode/review captures concurrently
without C#/imported edits. Next exact new player for delayed staged/controlled
familiar and ultimate owner reconnect; then Nemu alternate behavior/remaining
kit/counterplay. Old small ghost/current monster retained.


## CURRENT Nemu review after pushed Zack checkpoint

867f7fc0f862b24b6214f76d91eadc44af2e528f committed and pushed, clean source tree
before this ledger update. It includes Zack/lifecycle fixes and exact focused
proof. Now running7related Nemu Play cases: six existing familiar action/ground/
recall/reconstruction/pull checks + three-slot actual owner/body capture selected
with TUMP_REVIEW_HERO=nemu. Logs/nemu-laptop-kit-v1.xml/.log and image folder of
same stem. No C#/imported edits while Editor runs.
Next inspect original captured motion; preserve restored mini and current giant.
Then fresh INTERNAL build includes outgoing-kit cleanup, and actual familiar
delayed control/reconnect proof. Continue alternatives/counterplay and full queue.
No agents/other chats/Figma/reset credits/Desktop update.


## CURRENT: Zack verified batch, proceeding to Nemu

Latest laptop checks complete: corrected native Magnet and Thunderstrike-followup
150ms runs passed; alternate tradeoffs passed; two reproduced cleanup defects
fixed and3focused Play checks passed;8same-hero loadout Edit checks passed.
All sessions85689/82935/51103/22551/30693/6228 retired. Last guard2b2ed3078147.
No Editor/player active. Source/audit/evidence in zack-skills.md and zack-evidence.
Next commit/push this stable batch, then use a fresh internal player including the
lifecycle correction for Nemu current familiar control/rejoin/transform review.
Keep restored small ghost/current monster; no model redesign. Remaining six-kit
alternatives/counterplay, other fields/rejoin, movement/network/spectator/UI/Inday/
selected expansion order remains intact. No agents/Figma/usage resets.

## CURRENT Zack cleanup verified; related loadout check

Cleanup run30693 completed3/3passed, receiptb1b70093cb05. Refused sprint now
leaves0patches/noaura; Magnet expiry/refusal/hero replacement and existing Sean
ember cleanup pass. Before/after + XML preserved in zack-evidence. Running only
HeroLoadoutRefreshTests eight cases next, Logs/zack-laptop-loadout-v1.xml/.log,
to confirm six same-hero sidegrades still retain active state. Collect before
C#/asset edits. Next save verified batch, continue Nemu current familiar proof
and remaining kit/alternate/counterplay work. No agents/Figma/resets.

## CURRENT Zack cancellation correction

Focused acceptance baseline22551 completed2/4, guard receipt a9541a0c0a84.
Snap actual flight15.325->22.066m/s and10->5sarming; ArcLine radius1->.55m,
inner shock.25->.3625s and safe .78mouter lane passed. Failed: refused sprint
left1active patch + aura; hero replacement dropped the old kit without cleanup,
leaving Magnet recall trace. Both failures preserved in zack-evidence.
Fix: owned sprint patches/aura are cancelled separately from normal trail expiry;
BindHero resets the outgoing kit before replacement. UpdateLoadout is unchanged.
Next focused2failed cases plus existing Sean cleanup and same-hero8case check.
No blanket tests. Native corrected Magnet/Thunderstrike already passed; production
now differs only by this lifecycle correction, to qualify before commit.

## CURRENT Zack laptop verification

Internal build succeeded from c664a7b9. Runtime SHA256
8e40773292d9c32092443b3e425ec803dc68e5b037b616caaa408764406cac91.
Guard85689 completed, shared input preferences restored, receipt6f1cbf90de32.
Magnet150ms host/owner/observer passed with376/400/386samples and no stale
charge/held/poles after release. net-zack-laptop-magnet-v1 contains full receipts.
Thunderstrike150ms follow-up also PASSED:373/380/384samples,3bolts each,
actual empowered follow-up and clean expiry. Session51103 retired. Both receipts
are copied into zack-evidence. New4case local acceptance run is active:
Logs/zack-laptop-acceptance-v1.xml/.log; collect before C#/imported edits.
Four new focused acceptance cases prepared in ZackKitAcceptanceProbe for alternate
tradeoffs and refusal/expiry cleanup. Production source unchanged since pull.
26 generated recovery/swim assets differed only in CRLF bytes and were verified,
backed up and restored exactly. No other import dirt restored.

## NEWEST: returned laptop, pulled and resumed

Owner requested return to this laptop and immediate continuation. Clean checkout
fast-forwarded from986542f4 to c664a7b971ebf8c6d69ee848ce4194b53630a6ab; checkpoint
ancestry verified. No competing Editor/player was running. Current checkout:
C:/Users/Matthew/Documents/Codex/2026-09-09/ok-x20/work/TumbangPreso-Unity.
Prior source-machine stopping instruction/process IDs are retired.

Starting guarded INTERNAL Builds/ZackSkillReview/TumbangPreso.exe with profile
equipment-destination-review, log Logs/zack-laptop-build-v1.log. No Desktop update.
Source is the pulled correction already locally checked4/4 on the other PC.
Next run net_zack_review.py magnet and thunderstrike with150ms delay sequentially
against this exact rebuilt executable. Review variants/counterplay source while
the build runs; no imported/C# edits during the Editor. Full saved queue continues
after Zack. No agents/other chats/Figma/resets; UI late, Inday after, expansion last.

## NEWEST: second PC transfer, source machine stopped

Owner explicitly requested all unfinished work committed/pushed and a complete
handoff, because they are moving PCs and studying here. This overrides the prior
no-stop instruction for THIS machine. No more builds/tests here. The destination
must PULL FIRST, then continue the entire remaining queue without stopping at a
batch, concept choice, test pass or compaction. Optional questions wait for them.

Repository https://github.com/DOST-GameDEV/TumbangPreso-Unity.git, ASTRAReworks.
This transfer checkpoint follows stable Sean7578820993b588759ea74f46537fecad73e3ff2a.
Use git log for the transfer commit containing this file. All listed Zack WIP is
saved with it. Verify the new PC's checkout/status, preserve uncommitted work,
fetch origin and pull ASTRAReworks with --ff-only. Never reset/clean an old checkout
or assume the old C:/Users/matth or C:/Users/Matthew paths still apply.

THE LATEST BUILD WAS INTERRUPTED, not completed: Logs/zack-build-v2.log.
Stopped only its verified owned Unity process tree to free this PC for studying.
The guard restored/hash-verified two existing profile files and zero shared
Editor input preferences: receipt3019f4db6f5b. Guard2044 and its Editor/workers have
exited. There are NO active task-owned Editor/player/proxy/render jobs to resume.
Older running-process entries below are historical and superseded by this section.
Builds/ZackSkillReview may be partial because the builder purges its old output.
Do not run it as a current finished build. Desktop was never updated.

Latest source passed 4/4 focused charge-sync-v6 tests, receipt5b4df354a7e1:
real Magnet recall/throw; repeated ElectricZap snapshots without ending the
independent Thunderstrike window; preserved FireExplosive snapshot behavior;
full moving/throw capture. The final corrected delayed-client reruns are NOT DONE.
Latest internal build that finished was Zackv1, before that final correction,
RuntimeSHA332b72510064f2a86699b6994c354ef5b89121ab6a6c8fa994dc3d9c5f0af7e2.
Its older Magnet network baseline proved stale charge on both clients. Older
Thunderstrike baseline passed but did not include a charged follow-up throw.

FIRST WORK THERE: verify tools/idle Editor; rebuild an explicit INTERNAL Windows
player through tools/run_unity_guarded.py and equipment-destination-review profile.
Run tools/net_zack_review.py against that exact executable for --case magnet and
--case thunderstrike, --delay150, distinct new --out directories. The current
probe includes Thunderstrike's charged follow-up throw. Use exclusive8990/8991
ports and named test profiles. The harness waits for actual host arena readiness,
requests the existing60fps cap, and allows a brief flush after host completion.
Prior startup failures had header-only traces; classify them as launch failures,
not ability failures. No production network timeout was changed.

Then finish Zack alternatives, refusal/expiry/interruption and overlapping role
counterplay, followed by all remaining abilities/Nemu authority, persistent hazard
rejoin, movement/recovery/Pektus, match/network flows, spectator/replays, performance
and AI/TODO152/152.4. UI remains LAST in the main queue, then deferred Inday FPP,
then seventh hero/map LAST LAST. Exact UI backlog and unapplied held-info patch:
UI_REMAINING_TODO.md. Preserve the accepted character B + map C expansion choice.

Builds/ and Logs/ are gitignored and DO NOT transfer by pull. Selected latest
Zack XML, normal-speed videos, timing CSVs and images are committed in
reports/improvement-2026-09-14/zack-evidence. Reports and lower chronology preserve
completed work, failures and decisions. Portable225-file skill-reference-bundle.zip
and manifest are under docs/tooling; references do not grant tools/credentials.
No Figma, delegation/other chats, usage resets, paid APIs or Desktop update.
Only necessary focused tests; no automatic full suites. One Editor; no C#/asset
edits during runs. Verify new PC paths. Keep this ledger current across compaction.

## Standing AFK continuation contract, renewed 2026-09-14

The owner explicitly requests continuous work until the whole authorized queue
is finished. Do not stop at a batch, tests, commit, image choices or compaction.
Reaffirmed again: permission to stop is only when everything is done. Keep
checkpoint reports as progress updates and continue the next saved work item.
Do not reopen an approval already given. Park optional questions and art choices
for the owner's return, with enough context to answer later. Continue independent
work around any item that needs their input; make routine implementation choices
from the accepted direction. Keep current process IDs, evidence, failures and the
next executable step below accurate. Preserve the existing credit, delegation,
Figma, profile and checkout boundaries. The selected expansion direction is
already character B + map C and does not require reconfirmation.

## Current task and process

Checkout: C:/Users/matth/Documents/GitHub/TumbangPreso-Unity-ASTRAReworks.
Branch ASTRAReworks, latest COMMITTED AND PUSHED HEAD:
7578820993b588759ea74f46537fecad73e3ff2a, Sean heat/contact/flight and saved owner decisions.
Previous95a70713 fixes Cheska Nova flight; f653002f preserved Sean baseline.
Earlier36805af4ddd38bec84029c3e6514b0e0c5e57553 saves Phaister placement/alternate checks and truthful copy.
Earlier5cfb04b3c44e9a14368a0afeb390c7696144a06f restores live Coven/current sky on rejoin.
Prior pushed checkpoints: 06b54470 construction cost; 6a1811c7 Phaister timing/forms/
rejection; c59cef8f origins/concepts; e6237a0d Dante; bc8a5f00 and b106c6ac preserved.

ACTIVE: Sean timing/art correction. Baseline SeanSkillTimingProbe finished
0/2 as expected failures: actual empowered throw consumed the flag and emptied
hand but left one emitting hand aura; elevated Supernova detonated at Y47.12653,
not grounded,1.805656s after press. Logs/sean-timing-baseline-v1.xml, receipt
a8343d82e6dc, preserved CSVs end in -before.csv. Python6904 exited.
The elevated baseline began at60m; regression now starts20m so landing is inside
its bounded4s observation window. This is a diagnostic elevated cast, not map art.

Changes under test: compact native flames follow actual held shoe/body and FPP;
remove pink BoltHead and old unbound hand particles; remove Sean's generic early
column/flash/camera blast. Body/FPP use an actual leap/dive/contact/recovery clock;
impact requires grounded motor, and shape deformation is reduced to6-8percent.
Preserve body, outfits, all clip data, existing hit radii and objective rules.
No final artistic/peer acceptance yet. Flame Rush aftermath still needs work.

Timing/capture v2 FINISHED3/3PASS, profilec848b4a12bd0, Python15580 exited.
Actual throwfire=True/charged=False/held=False/staleemitters0. Elevated20m cast
landed atY.1819999, groundedTrue,2.087498s. Body/FPP capture inspected; still
found a newly attached body ember visible beside the FPP copy. Source cause:
CameraRig caches held renderers before the effect exists. Ember now mirrors its
source renderer's shadow/visibility modes; witness capture can reveal both.

Rush and crater now have separate low heat wakes/cooling scorch geometry, no
repeated CinderFringe or orange badge. Preserve physical hazard radius and life;
FireTrail visual no longer scales its ground footprint inside the active radius.
Crater initialization now reads assigned glow/duration in Start, not Awake.

Ground-effect v3 FAILED4/5, profile417946eaf26a, Python5440 exited. Three failures
came from NEW MaterialPropertyBlock creation in a MonoBehaviour field initializer;
moved allocation to Awake. One cleanup assertion inspected the discarded kit's
boolean after BindHero replaced the instance. Fixture now checks the live Zack
kit and disappearance of the old ember for replacement; expiry/refusal still
require the original live flag false. No production BindHero change.

Ground/capture v4 FINISHED5/5PASS, profilec681378b6ce9, Python14380 exited.
New owner/body sheets inspected and ordinary-speed videos encoded. The native
heat wake is restrained, and the crater keeps the street readable. It no longer
resembles the rejected orange disk/ornate trail badges. Review is provisional,
not a human play-feel or multiplayer approval.

Full empowered throw capture v1 FINISHED1/1PASS, profile35dd02e37811,
Logs/sean-throw-v1.xml, evidenceLogs/sean-ignition-throw-v1. Actual release
frame28/t1.9681 has heldFalse. New findings: world slipper fills the eye for its
first release frame; flight is pink. MuzzleForward is.15m, intentionally unchanged;
CameraRig now keeps its own released world mesh hidden until its support extent
clears the eye by.25m. This affects visibility only, preserves physical origin,
trajectory, cover and objective rules. Existing world observer path remains.
World-fire colours now use AbilityVfx.FireColour/FireHotColour, leaving UiTheme's
roster accent unchanged. Fire trail is smaller/shorter with restrained light.

INTERNAL Sean buildv1 FINISHED, profile911073bd4567, Python11864 exited.
Builds/SeanSkillReview/TumbangPreso.exe, RuntimeSHA
8eb5381f6c5d7814ef31e85e43dc2161277275879d00c2892acadc50b1d5eeb6.
This binary predates the flight/visibility/network-consumption correction below.
Protocol31. Desktop untouched.

Actual3-client Ignition baseline FAILED: Logs/sean-net-ignite-baseline-v1/result.json.
150ms each direction on owner link. Host463/owner449/observer454samples; all3saw
5empowered-flight samples. Host consumed charge; owner and observer retained it
in the post-throw window. No carried embers/held shoe remained. Helper session74043
completed and all3ownedplayers/link helper exited;0pre-existing named profilefiles.
NetSeanProbe uses actual room pick and one preloaded-arena accepted-pick init,
then real owner key intent. No periodic forced accepted states.

Correction: first trusted FireExplosive flight snapshot consumes the previous
holder's Sean charge, and creates the actual fire flight VFX on peers. Repeated
flight snapshots do not consume another charge/recreate trail. Loose/held snapshots
clean that effect. No protocol change. Same-process replay check is added but is
not a replacement for the actual delayed-client rerun.
Flight-v2 failed compilation: missing namespace qualifier for SeanHeroKit in
Slipper.cs; qualified Abilities.SeanHeroKit. No tests ran, profiledbd3b9875612,
Python17560 exited. Preserve this failure rather than calling it a gameplay test.

Flight-v3 FINISHED6/6PASS, profilefa5208222244, Python11352 exited.
Four Sean timing/lifecycle/snapshot tests, real full E throw capture, and all
held-shoe handling/releases in both modes. Fresh first released frame28/t1.9891
was inspected directly: no world shoe filling the eye. Source trajectory/origin
remain unchanged. The orange flight is visible subsequently; no pink beam/light.
Videos encoded at measured times under Logs/sean-ignition-throw-v3. Earlier
heat-v4 owner/body sequences remain the Q/R ground evidence, before latest amber
world palette. Selected CSV/images are in the report's sean-evidence directory.

Internal buildv2 FINISHED, profile61ce6816a649, Python4676 exited.
RuntimeSHA70fd0694d08c4c472f3eed6505e50a795163cd506d2a797f6563b139b87cb936.
Actual150ms delayed three-peerIgnition PASS, session60633 completed,462/451/454
samples,5/6/5fire-flight samples,no stale charge/held/embers;7profilefilesrestored.
ActualSupernova PASS, session65314 completed,458/447/452samples,onecraterperpeer,
firstcratergrounded,allcraters/posesexpired;8profilefilesrestored. Measured rise
3.354/3.125/2.892m includes sampling/smoothing, not identical peer-frame motion.
Results copied to report's sean-evidence. Allownedplayers/link helpers exited.
No Editor/player/helper running. Inday arm asset and ProjectAuditor changes were
verified whitespace-only againstHEAD and restored exactly. No body/FPPassetedit.
Sean batch was committed and pushed as75788209. No pending staged changes.
Sean variants/Zack baseline FINISHED3/3PASS, profile9e538377979e,
Python2640 exited. Afterburn travel2.2599 vsRush4.0921m; observed wholewake4.3519
vs3.4527s, eachpatch1m radius. Flare observedflight15.6634vs13.0679m/s and arming
window7.5vs10s. This does not independently measure alternate impact radius.
Results copied to Sean report/evidence; new two tests remain uncommitted.

Zack's actual3slot baseline is Logs/zack-current-kit-v1; owner/body sheets
inspected, videos encoded. Q has repeated star floor patches; E has large square
hand particles; R floods owner/body yellow before contact, from the shared
UltimateColumn and early camera/chromatic/flash presentation. Existing sustained
speed and host-authoritative instant Magnet remain valuable, preserve them.
Zack uses30percentdash and40percentult stretch. No model/hand geometry changes.

New ZackSkillPresentationProbe baseline FINISHED0/2 (expected failures),
Logs/zack-presentation-baseline-v1.xml, profile7a541dedd0c8, Python1600 exited.
Actual generated diagonal bolt misses start by3.076664m/end.5387341m; old function
uses only segment length and places an upright billboard at end, ignoring start
orientation. Real sprint input produced7live patches despite six-cap; initial
patch was not enrolled in _live. Before CSVs preserved with -before suffix.

Changes under test: DirectedLightningBolt native branched geometry connects
requested endpoints; pulse/expiry preserved. Initial sprint patch enters the
existing six-cap queue. Zack stretch now5percent forbothcasts. Generic ultimate
column/early chromatic-camera blast removed, actual targeted strike preserved.
E square-particle charge, targeted warning/strike readability and Q wake art still
need their authored pass. Do not assume allZack work is complete from these fixes.

Lightning-v2 FINISHED3/4PASS, profileb4aff216ccd8, Python7860 exited.
Native endpoints now within.022/.0044m; body/FPP capture and sustained-speed/reset
pass. Cap still briefly saw7because Destroy is deferred. Evicted patches now
SetActive(false) beforeDestroy, preventing an extra active patch in that frame.
R recordings show the early yellow wash gone; actual strike still has large
square particles. Those are the next changed presentation path.

Uncommitted authored additions: ZackSkateWake draws two ground contact tracks
and an intermittent cross-discharge, replacing repeated star/disc patches.
Direction follows the actual lagged travel. ZackMagnetCharge draws two opposed
charge poles on the held shoe in body/FPP, matching source visibility; used by
Magnet and the existing ultimate charged-throw window. Their primary casts remain
different. Square hand aura removed. Strike particles use a small bent spark mesh
with bounded burst count/life/travel. No model/outfit/hand geometry edits.

Contact-v3 compiled no tests: new ZackMagnetCharge meta had a33character GUID,
so Unity ignored its script. Generated a valid uuid4 hex GUID and checked every
new untracked meta, allvalid. No existing GUID changed. Profilebafb9f5caea6,
Python18536 exited. Preserve failed log; no runtime baseline claim for that run.

Contact-v4 FINISHED4/4PASS, profile7628ff402dfe, Python7900 exited.
Endpoint/cap/realMagnetrecall-throw-cleanup and3slotcapture pass. Maxpatches6.
Owner/body sheets inspected. White charge/wake lines revealed the Standard-based
VfxMaterial ignores LineRenderer vertex colours. They now write their owned
material colour/alpha, matching the existing MagnetRecallTrace approach. Charge
brackets shortened so they read as poles rather than a whole-shoe outline.
The solid ThunderIonCore cone is removed; targeted native bolts, impact spark,
shock outline and brief light remain. No gameplay hit/radius change.

Added NetZackProbe.cs/meta and tools/net_zack_review.py for actual delayed
Magnet and Thunderstrike. They use the accepted room pick/preloaded arena seed,
then owner key intent; host disarms the owned shoe before Magnet. This is a new
probe awaiting its first compiled player run. It has NOT proved peer correctness.
Suspect stale electric charge after peer throw, by analogy with the reproduced
Sean bug, but do not patch or claim that until the actual run establishes it.

Moving/full-throw capturev5 FINISHED1/1PASS, profilef02f346c67cf, Python8392 exited.
Owner/body sequences inspected and encoded at captured wall-clock times. Q has
actualmovement; Erecallsandthrows; Remptyhand follows. Some metadata/log samples
are retained in reports/improvement-2026-09-14/zack-evidence. Colour/alpha now
comes from owned material, avoiding the prior ignored LineRenderer vertex colour.
Magnet flight still uses its prior bright light/trail; inspect after peer baseline.

INTERNAL Zack buildv1 FINISHEDSuccess, profile42d90b172667, Python13956 exited.
Builds/ZackSkillReview/TumbangPreso.exe, protocol31, RuntimeSHA
332b72510064f2a86699b6994c354ef5b89121ab6a6c8fa994dc3d9c5f0af7e2.
No Desktop update. Runtime changes/Seanvariant tests remain uncommitted after75788209.

Actual3peerMagnet baseline FAILED, session36001 completed, allownedplayers/proxy
exited;0existingnamed-profilefiles. ResultLogs/zack-net-magnet-baseline-v1/result.json
copied to Zack evidence. Host458/owner447/observer449samples;4/4/5charged-flight
samples. Host charge0afterthrow, bothclientscharge1in post-throwwindow; held0,
chargevisuals0onall. This is now proven, not merely analogous to Sean.

Thunderstrikebaselinev1 did not reach gameplay: ownertransportclosedbefore
seat1, no approval log/trace samples. Session17160 completed,7profilefilesrestored.
Harness now waits for the host's actual arena-ready log instead of fixed7s sleep.
Thunderstrikebaselinev2 also did not reach gameplay: ownerjoined, observertransport
closedbeforeapproval, so3peerready gate neverstarted; CSVsheaderonly. Session68022
completed;7profilefilesrestored. These are launch/connection failures, not ability
failures. All ownedprocesses from bothattempts ended. Do not claim rootcause from
logs alone. No production transport/timeouts were changed.

Thunderstrikebaselinev3 FINISHEDPASS, session20385 completed and ownedplayers/proxy
exited;7profilefilesrestored. Requested60fps/hostready setup.421/418/418samples,
3strikechannels onall, targetshock2.0/1.9235/2.0s, targetrise.771/.801/.786m,
no finalbolts orultimatewindow. No follow-upthrow in this baseline. Result copied
toZack evidence. Thevictimwasactuallydisplaced; "stunnedwherethey stand" is false.

Latestcorrectionunder test: firstElectricZap flight snapshot consumes previous
holder's Magnetcharge, preserving the independent Thunderstrikeactivewindow.
Repeated snapshots keep anotherloadedcharge and do not recreateflightvisuals.
Electric flight uses two narrowtraces and a smalllight; replicas now create/clean
its actualeffect. Fire path remainscoveredbyits existing snapshotregression.
Thunderstrikecopy now says shock/knockback plus seven-second chargedthrows.
NetZackProbe now also throws after Thunderstrike to test that follow-upwindow;
CSVchargevisualcolumn renamed fromlegacyembers, driver accepts oldbaselineheader.

Charge-sync-v6 FINISHED4/4PASS, profile5b4df354a7e1, Python23760 exited.
ActualMagnetrecall/throw, independentult/repeatedZap snapshot, retainedFire snapshot,
andmoving/fullthrowcapture pass. Ownerthrowsequence inspected: reduced twintrace/
light spill, no carriedchargeafterrelease. Videosencoded at recordedtimestamps.
EvidenceLogs/zack-charge-sync-v6. Internalv1 does NOT contain this correction.

RUNNING guarded INTERNALZackbuildv2, Python2044:
Logs/zack-build-v2(.pid/.stdout/.stderr/.log), same
Builds/ZackSkillReview/TumbangPreso.exe. Do not edit C#/importedassets untilEditor
exits. NextrequireSuccess/profile receipt andfreshRuntimeDLL, then actual3peer
Magnet andThunderstrike withchargedfollow-up,150msownerlink,requested60fps.
Helpercurrentwaitsforhostarena anduseshostcompletion/shortflushgrace. Diagnose
connection/ready failures separately from abilityfailures. No final peerpassyet
for chargecorrection. Allpreviousplayers/proxies exited.

Owner asked overall progress at13:03UTC: reported a rough planning estimate of
40percent completed in the past12hours /60percent remaining for this rework.
This is NOT a measured task counter, release-readiness claim or acceptance gate.
Full remaining requirements below still apply; do not turn that estimate into
completed tasks or permission to stop.

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
Owner selected character B (tied-hair/green boatcraft) + map C (sheltered lagoon)
after both sheets were shown directly in this chat on 2026-09-14.
Selection is saved in AGENTS.md, BADJAO_EXPANSION.md and the concept README.
Implementation stays LAST LAST. Originals remain in .codex/generated_images/
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
