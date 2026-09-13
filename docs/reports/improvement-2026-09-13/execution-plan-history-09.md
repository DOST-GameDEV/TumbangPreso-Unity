# Active execution plan

<!-- LIVE_STATE_START -->
Pushed9ff95072; origin/ASTRAReworks matches. Current Desktop review
build DELIVERED at C:/Users/Matthew/Desktop/TumbangPreso-Unity/TumbangPreso.exe,
built 2026-09-13 07:29:22, protocol29, RuntimeDLL SHA256
1ce3858fc9f59ffbc09df3aeceb5ab0c55d9f8275521952d4e3885b75abff7e1.
Bayan aerial delivered: Logs/bayan-owner-aerial-2026-09-13 (1/1,14 views).
No Editor active at fresh preflight; user Desktop player PID23436 observed.
NEVER stop/overwrite their player or restore a whole profile over concurrent saves.
Implement explicit named-profile scope in run_unity_guarded before Editor tests.

LATEST OWNER PLAYTEST FEEDBACK (see OWNER_PLAYTEST_REVISION.md for full contract):
- KEEP current Bayan layout; earlier rejection/rollback explicitly retracted.
- Remove Street Hype mechanic and ALL of its UI/callouts/rewards/network producers.
- Remove duplicate main setup RULES selector; formats stay in Custom Game.
- Ilalim: floating rooftop pieces, unclear hump/road seam, mismatched background
  road. Trace actual mesh bounds, parents, materials and support; fix the source
  authors, not just a camera angle. Natural wall-painted Bawal remains required.
- Eskinita: tighter than other maps without blocking play. REDESIGN sampayan with
  actual attachments and sagging lines/recognizable clothes; old version rejected.
- Bayan: floor still feels flat/untextured. Give paving readable slab/grain/edge
  response without bright joints, noisy contrast or clutter in the central court.
- All maps: varied native trees, warm cream/terracotta/brown-grey road/soft sky
  palette per latest old screenshot; no geometry rollback to that screenshot.
- Sa Bubong is approved and NOT implemented. Owner asks why after10hours. Move its
  playable layout and actual edge/mash/10s slipper recovery into next map batch,
  ahead of the broader equipment/abilities backlog. Do not wait for approval again.

Stable batch committed/pushed9ff95072: StreetHype removed;mainRULES removed;
existing Custom Game door repaired and raycast/click tested. Per-map atmosphere/
original sky shader,realBayan paving normal/aggregate/tangents,Ilalim continuous
asphalt,actualroof supports,billboard mast endpoints,independentbusiness signs
and direct brush-painted warning. Optional unverifiedbackgroundcrane retired.
Core562;Edit510;Lobby3;actualFPP1/1 (144matched+6HUD);Ilalim final24views1/1;
all8checks;all14sourceaudits;all3maps semantic2run no drift. Final fascia contact test2/2 (all11signs). See portable report
reports/improvement-2026-09-13/owner-map-review.md and adjacent receipts/images.
Every Editor used the isolated owner-review-editor profile. Lastguardb2fdd910253d,
2 existing named files restored. No Editor/player active at latestpreflight.
Only proven test-only arm tangents/materialwhitespace and exact five Ultra quality
values restored; no map rollback. No new Windows player since d9c0314b Desktop.
NEW WORK IN PROGRESS after9ff95072: laundry GLB/native sources being integrated,
Sa Bubong scene author starting. No newmap ready or qualified yet.
Laundry source in tools/author_resident_laundry.py and Logs/resident-
laundry-v1 (4.4m/18m Blender+GLB+PNG+JSON). Not imported/placed yet. Sa Bubong layout
source exists at MapSource/environment/layouts/sa-bubong-plan-v1.json;no scene yet.
CURRENT NEW IMPLEMENTATION after pushed9ff95072:
SaBubongBuilder created the draftscene;four map registered inSceneFlow/GameLaunch
and protocol30 prevents older clients choosing the wrong map. Builder now enables
sceneinEditorBuildSettings. V1 preview was too sparse/blank-sided and HDR PNG
export too dark. V2 adds defined skyline sites,allsidewindows,lowretainedhouses,
connectedcityroads and a proper linear-to-sRGB preview conversion. Stillrequires
heavy visualcritique,texturemapping/placement/performance review.
RooftopRecovery initially implementsactualedge descent,prone respawn with existing
trip/mash and10s unavailableslipper return. Lostpoolstock alsorecovers. Host owns
clock/state;normalinactive-slipper snapshots replicateavailability. Slipper added
guardedbegin/endmaprecovery andan inactive-stockgrab refusal,plusmap-onlyflight
hookbefore ordinarygroundlanding. Generic GroundY unchanged. Need qualifyimmunity,
existingstunoverlap,teleports/amenities,actualKB/controller/touch,delay/rejoin and
round/rematch/hostloss. Do not call this finished from a helper/test result.
RoofRecoveryProbe newWallClockfixture walks offedge,exercisesrealintentconsumer
(tapsvsheld),10sreturn/actualpickup bothmodes,pool/inactivegrabs/reset. It does NOT
provephysicaldevices or networking. ACTIVE guardedSaBuilder.Run v2 thenfixture,
Logs/sa-bubong-layout-author-v2.log andLogs/rooftop-recovery-v1.xml/.log;
session25317. No C#/imported edits untilEditor finishes.
ResidentLaundryAuthor/LaundryMotion+native4.4m/18m GLB/blends are integrated in
source;Eskinita hooknotyet authored/verified. Rooftophascourtyardline/tank. Need
readability,clothespegs/motion/collision support andordinaryFPP review.
Lastcompletedguard2c25c9906c4d,2namedfiles restored. Desktopstilld9c0314b.
Next: collectroof physicsfixture,fixobservedfailures;actualFPP/scenecritique;finish
newmap/fall qualification,applynewEskinita laundry andvarytrees. Aftermaps full
abilitymechanics/visual/ultimate revamp,thenallotherplayfeel/equipment/graphics/
network/TODO/finalWindows. LargergoalOPEN;do notstop at a handoff.
<!-- LIVE_STATE_END -->

## Standing scope and safe workflow

Owner is awake and testing the Desktop build, and explicitly asks continued work
and durable state across compactions. No handoff stop or approval question is pending. The larger goal is
OPEN. Read AGENTS.md as the primary self-contained rules, then VISION/TODO152.4,
latest ledger, MAP_TRANSFORMATION_PLAN M01-M11 and PLAY_FEEL_REWORK_PLAN.

ONLY ASTRAReworks in C:\Users\Matthew\Documents\Codex\2026-09-09\ok-x20\work\TumbangPreso-Unity.
Fetch/inspect before stable commits; preserve newer work. Never reset, touch main
or the separate Documents/GitHub checkout. No subagents, paid work or usage resets.
Use tools/run_unity_guarded.py for EVERY Unity launch; preserve profiles/IDs,
one Editor, no C#/imported-asset edits during a run. Controller ownership separate.

Keep18 approved people/outfits at7c7fcb5, cute chunky forms, flat faces/simple hands.
Thin house V1-V5 replacements are rejected. Preserve the old solid house mass and
fit local construction/use into it. English gameplay/UI copy; Filipino environmental
text permitted, proper names retained. Four players, rotating defender, can and
slipper throwing/retrieval, Classic4 rounds and Hero Strike8 rounds remain.

Finish map transformation/coherence, then movement/throw/Pektus/equipment and
all-context recovery mashing, then graphics scalability/settings and approved
Sa Bubong (actual edge fall,mash get-up,about10s fallen-shoe penalty). Continue
remaining actionable TODO: six whole kits/alternatives,same-hero build binding,
Phaister/Kuro qualification,body/FPP/geometry/VFX/SFX,network transitions/reconnect/
host loss and exact Windows release. UI art low priority; maker stays inaccessible.
No new controls/systems that overcomplicate play. Final appropriate gates,isolated
PlayMode gate twice,Windows build and THAT executable in both modes at ordinary
speed and required separate-process network cases. Desktop review build delivered as recorded above; final release remains open.

Criticize every batch for place/style,scale/mass/support/intersections,world depth,
routes/readability/motion and cost. Tests and isolated renders cannot approve art.
Keep known failed experiments and do not invent causes or profile/run IDs.

## Stable map/tool checkpoints

-50e07025: bounded concurrent Gitpipe reads in GameBuilder;actualbuildV3 blocked
 on4131stderrbytes beforeits timeout. Ownedrun stopped/profile restored;2focused
 tests pass with64KBstderr and failedcommand. Laterbuilds complete. See
 reports/improvement-2026-09-13/build-git-pipe-investigation.md.
-e2c19e16: Eskinita retainedutilityhardware,50connected neutral conductors/12posts
 withmeasuredtrunkcollision.48matchedFPP,20/20pickups,21467semanticrows(no drift),
 8checks/14audits. Six currentIlalimcandidate seeds all0idle/camp;historical48cause
 unresolved. Reports in improvement-2026-09-13.
-420060bc: Eskinita solidhome/privateplot/parking/corner/backstreet transformation,
 fittedretainedfamilydetails,assignedprops.48FPP/20pickups/491Edit/8checks,21275
 semanticrows. eskinita-neighborhood-draft.md in improvement-2026-09-12.
-48372a85: Bayan originalslabpaving,connected4road/40hometown,civic side/rear detail,
 groundedclosedterrain.48FPP/20pickups/8checks/19707semanticrows.bayan-town-draft.md.
-1661cb2b: Ilalim Gilmore/LRT11shoplots,2indoorpisonets,4originalBlender vendors,
 readable signs/wallpaint/crossings,28measuredpoles/78wires,roof/signclearance.
 Core562/Edit491/8checks/14audits/20pickups/48FPP+22clearance/19118semanticrows.
-0611c6d4: NearFade preserves transparent/cutout materials,13focusedtests.
-8a22f8b9: isolated directcarry release/flight swept-height support,4focusedtests
 inclraisedground/unreachable-roof recovery. No historical48idle attribution.

M10/M11 finalmapcoherence is stillopen:boundary clarity at open-looking ends,
landscape/material/lighttone,broader ordinaryplay/quality/performance. Finish with
plannedgraphics/finalplayreview,notendlesssmallprops. NearFade normal-map copy is
stillmissing. Retainoldchunkyhouses;rejectedthin V1-V5 quarantined inLogs. All native
sources/authors and portablemapcritique reports are preserved. No mapart approval
is inferred fromtechnicalpasses.

## Throw/Pektus/handover batch pushed4981c986

Runtime:ThrowGesture,CharacterAnimator,ViewmodelArms,CameraRig,SpectatorCamera,
Carrier,MatchRpc/NetSession(protocol29),Slipper/SliceRunner. Opt-inNetThrowProbe and
net_throw_matrix.py,guarded by TournamentPreset/Guard;neveractive inbracketgames.
Tests:ThrowMotion7cases,ThrowChargeRelay,SlipperHandover5cases;capture in
MapExperienceProbe/ImprovementEvidenceProbe now runs afterLateUpdate.

Bodycoil/headcompensation/offhand/signedarmroll and0.46srelease/recovery. FPPforward
swing startsimmediately,withgrip/elbow solve;legacy lungeFPPfullangle stays. No people
geometry,newcontrols,randomaimspread or trajectory/balancechange.
Protocol29 phase/spin fixesmissing listenhost windup and observerrejoin. 10Hz max
spinchange/2Hzsteadyheartbeat;ownerkeepslocalinput. Cancellation/stun/departureclear.

Actual3processes founda warmuporphan:seat1grabbedshoe0,thenroundhandover gaveitshoe1
withoutclearingparkedshoe0'sHolder1. Packets re-equippedthat ghostaftertherealthrow.
Fixforcedreplacement andparkingdisarm,occupiedhandgrabrefusal,and inactivepacket
normalization. DisplacedshoeusesexistingsilentLand/narrowfoot-levelsupport. Public
GroundY andactualflightquery/trajectory unchanged. No historical48idleconnection.

Evidence:
-Core562;fullEdit504/504 (profile15a25637349e);5carry;4actualflight/retrieval incl
 underguideway/raisedslab/unreachableroof (e818d65aa488);8checks(75151794b649),14audits.
-6ordinarythrow sequencesbothmodes,owner/observer,12real-timestampMP4s:
 Logs/throw-motion-v4-ordinary,profileb24103d8836a. Earlierpre-LateUpdate/awkwardpose
 capturesrejected;fullhistory in throw-motion-investigation-history.md.
-ActualClassicstrictcase:Logs/throw-network-classic-v4,passed all3signedphases,
 forcedcausalwarmupviaordinaryMove/Grab,0ghostheldsamples.
-ActualHero150msone-way+observerrejoin:Logs/throw-network-hero-delay-rejoin-v4,
 samecausalsetup,allphases,159correctrejoinactivesamples,0ghostheld. Eachrunner
 restored26files. EditorbuildV5restored25,profile66105cb04fbf.
-InternalWindows Builds/ThrowReview/TumbangPreso.exe,base50e07025+dirtymotion,
 RuntimeDLL307167a3a54ebb1f9ec44db7799bd14cad46321cf98b906c76b2fbf60bf2189c.
 EXE6f44fe53090dad3edd9f86f5b5691b2cc8cba07deb4efd4791d7e32135385cb8 isUnitylauncher
 andstayssameacrossbuilds;alwayscheckRuntimeDLL/data too. NOT finalrelease.
-Canonical portable report:reports/improvement-2026-09-13/throw-motion-review.md;
 corrected andfailingCSV/results preservedbesideit. No claimofallhardware/humanfeel.

Cleanup:Logs/restore_verified_throw_test_dirt.py verifiesallnontangentmeshdata and
whitespacebefore restoringknownEditMode arm/materialnoise. Do notrestoremaps with
oldmapcleanup scripts. QualitySettings testnoiseisexact5Balancedvalueswritteninto
UnityUltra;validatefullrecognizeddiffbeforeHEADrestore. Canonicalblob-equal files
can needgitadd statrefresh;neverchangegitautocrlfconfig. No C#/asseteditsduringruns.

## Next concrete work

1.Follow the live owner-playtest revision above; old grounding steps below are
 historical (d9c0314b already pushed). Commit stable batches on ASTRAReworks only.
2.Measureactualdrawnfeet vsfloor/CCskinWidth atidle/carry/charge/move;currentbody
 rootsettles0.08mabove support. Distinguishcapsuleskin,meshbindbounds,authoredrootlift
 andshadowprojection beforechanging. Thenfixgrounding/cadence/backwards/strafe/turn,
 sprint/carry/FPP sync andinterruption/contact with ordinaryspeed evidence.
3.Inventorycurrent10slipper/6canmechanicalconsumers/IDs anddesignsimple meaningful
 role/tradeofftable. Slipperimpactcurrentlybodyblockpush,notcanknockdown. Cansscale
 reset/rebound/hitmargin. Evaluatecontrol/aiminstability withoutarbitraryrandommisses.
4.All-context recoverymash:actualKB/controller/touch routes,quicktap/hold30/60/144Hz,
 minimumrecovery,host/remote/delay/rejoin. Separatecontrollerowner protected.
5.Graphicsprofiles/settings low/high measuredbenefit/readability andmapM10coherence;
 thenapprovedSaBubong actualedgefalls/mashget-up/10sfallen-shoeunavailability.
6.Whole6kits/alternatives,sameherobuildbinding(livestate/doublemodifiers),Phaister
 phasedritual/cursetiming/unbind/11mmoonvs8mguideway;Kuroacceptedpurpleforms/private
 expressions/5.26mgiant/protocolstagedyawrejoin/rostersheet/overlapqualification.
7.Broaderactiveeffects/builds/round/rematch/reconnect/hostloss bothmodes;remaining
 actions/UIfunctionalTODO. FinalisolatedPlayModegate twice,exactWindowsbuild and
 actualordinaryplay/separateprocessqualification. Desktop review delivered;Androiddeferred.

ReadPLAY_FEEL_REWORK_PLAN.md sourceaudit;allpriorrequirements remain. Full previous
plansarchivedwholein improvement-2026-09-12/history02-05 and
improvement-2026-09-13/execution-plan-history-06/07.md. OldprocessIDs arehistorical.

Owner explicitly allows continued work while testing the Desktop player. Work on
source/assets/research independently;do not overwrite the running build or restore
a profile snapshot over saves the owner changes during play. Editor/test validation
must account for that concurrent player rather than blindly restoring its profile.

Latest owner ability feedback,2026-09-13: current abilities still look poor and
feel too similar. AFTER MAPS, fully revamp and improve their implementation,
mechanics/purposes and complete casting/body/FPP/moving geometry/VFX/impact/SFX.
All six heroes/defaults/alternatives, within existing slots and simple controls.
This is not a recolor pass. Existing map corrections remain first; the other
movement/equipment/network/graphics/TODO scope remains open.

LATEST in-progress update: rooftop basicphysics2/2,Sa actualFPP1/1 (48+2HUD),
laundrymotion1/1 pass. Reproducedquicktouch0press failure; TouchInput Jump edge
nowlatched/readonce,chatdiscardsqueuedpress. Focusedtouchpasses. Reproducedtrip
mashshorteningindependent4stagto3.78; timersnowindependent,IsStunned ORs both,
tripmash/predictionnevercutnormalhold. Protocol30clarifiesexistingtwo timerfields.
Physicalrooffall usesApplyFallRecovery (ordinaryabilityimmunitydoesnotprevent
physicalget-up); normaltripskeepimmunityrules. Tag countdownonlyreadsnormalstun.
RecoveryDeviceProbe focusedtouch+overlap2/2 passes. Newactualconfiguredreader
keyboard/gamepad synthetic-device matrix targets30/60/144FPS acrosstrip+six
elements,checksone-frame tap/heldcounts andrecordsobservedcadence. ACTIVErun
Logs/recovery-device-matrix-v1.xml/.log,session fromcurrenttool;noC#/assetedits
until Editor exits. No GenericPadBridge/MenuNav/controller-source changes.
Need freshfullEdit/Core/sourcechecks andrealprotocol30separateprocess coverage.

CURRENT VALIDATION: fullEditfirst510/511onlyprotocolconstanttestexpected29;
updatedtogetherwithnewregistry/protocol30. FullEditv2 thenRecoveryDeviceProbev2,
RooftopRecoveryProbev2 andNemuKitContractProbe nowsequentialfreshEditors,
logsrooftop-edit-v2/recovery-device-matrix-v2/rooftop-recovery-v2/nemu-after-recovery-v1.
Cell32634. Earlierhardwarematrix4/4 passed with42keyboard/gamepadcontext/cadence
cases;expandedtouch21cases pluspending-snapshot/tag clock regressions nowrunning.
Prototypeartcritique: skylinecubeplaceholderstoo plain; useexistingcommercial
skyscraper forms withmeasuredlots. SaBubongcourtshouldhavefadedgreenrecreation
coating,notBayan'ssameplazastone. tools/author_roofdeck_surface.py producesdrafts
inLogs/roofdeck-surface-v1;notyetpublished. No C#/imported editsduringruns.

NEW CURRENT RUN: SaBuilder v3 then Windows build toBuilds/RoofReview/TumbangPreso.exe,
Logs/sa-bubong-layout-author-v3.log androof-network-build-v1.log;session83324.
No C#/imported edits untilbothEditors finish. Sa v3 replacesprimitive skyline
withfiveexistingcommercialskyscraper families,measureduniformscales/lots;green
recreationcourt coating andlowerafternoonsun distinguishitfromBayan. Need inspect
actualv3FPP andallside/backstreetplacement;noartapprovalclaimed.
NetRoofProbe opt-infixture+tools/net_roof_matrix.py prepared foractualhost/owner/
observer,delayedowner,rejoiningobserverduring10sstockloss. Probe usesordinary
ownerMove/Jump/Grab afterhoststagesatledge;capturesrealdescent,mash,availability,
returnandpickup. Tournament guard/list/CLIblocklistupdated. Runnerpreservesonly
3namedprobeprofiles,notmainprofile;manifeston disk. Notrununtilbuildcompletes
andnewRuntimeDLL isverified. Latestgameplaychecks:Edit511/511,device5/5 incl
63context/cadencecombinations,roof3/3,Nemu33/33. Latestactorlibrary/model/quality
testdirtmayneedknownverifiedcleanup. UserDesktopstilld9c0314b andnotupdated.
