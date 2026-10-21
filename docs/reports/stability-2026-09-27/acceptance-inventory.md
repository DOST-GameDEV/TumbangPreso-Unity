# Acceptance Inventory, 2026-09-27

Historical intake reviewed against ASTRAReworks at 04886cc4. The findings below
describe that revision, not current defects or fresh test results. Later fixes and
exact dispositions are in [QA comments](qa-comments.md), [QA2](qa2-validation.md),
[regression contracts](regression-contracts.md) and the newest ledger entry.

## Resume And Ownership

The canonical active queue is [TODO.md](../../TODO.md#current-implementation-queue);
the newest handoff is [ASKS-0926](../../TODO.md#asks-0926), and the continuation state is
[ACTIVE_REWORK_LEDGER.md](../../ACTIVE_REWORK_LEDGER.md). The owner has since narrowed
this session to finishing and publishing already-started work. The historical intake
below does not authorize a new whole-kit, animation or VFX pass.

The explicit implementation order and evidence limits are in [plan.md](plan.md). The
parent integration pass owns shared TODO and ledger status. This file is a source/test
reconciliation, not a second status queue.

## Historical Handoff Work

- Windows Development performance: measure cold startup, menu transitions, first use of
  every current hero skill and ultimate, first throw/contact, round start, lobby, and
  match end. Record build/config, frame-time distribution, transition time, and available
  profiler evidence. Compare after only where a measured bottleneck justifies a preload,
  async-load, or pool change. Preserve rendered art.
- Multiplayer: reproduce reported failures with host and peer state, including relay room
  start, join/leave/reconnect, authority, and ability replication. Keep source fixes
  separate from credential or tester-only acceptance.
- Interact prompts: inventory actual interactions, then verify the live binding and exit
  path for mouse/keyboard, controller, and touch. Use the existing input catalogue and
  Rebinding.DisplayNameFor; do not infer a prompt from a screenshot alone.
- Amihan and hero kits: use docs/HERO_KIT_METHOD.md, HERO-8, ABILITY-1, ABILITY-2,
  CAST-1, and HERO-9. Continue one hero and one ability at a time.
- Remaining TODO: keep every current unchecked UI, character, networking, gameplay,
  general performance, physical-device, and qualification item in scope. Retired layouts
  and old test assumptions do not become product requirements again.

## Named Regression Contracts

These distinctions come from reading current tests and adopted kit designs. They are
not fresh test results.

| Test | Reconciliation |
|---|---|
| BroadcastPassTests.AllSixHeroesHaveTheirOwnNamedUltimate | Its Dante TITAN FISSURE expectation is obsolete: the current kit calls it EARTHQUAKE. Expand names to the current roster; do not restore the old kit. |
| HeroLoadoutRefreshTests.SameHeroVariantRefreshPreservesLiveStateAndDoesNotStack | Alternate refresh is disabled by HeroLoadoutRules.SidegradesOpen == false. Assert the default kit remains stable; do not enable the skill tree to satisfy this fixture. |
| HeroPresentationTests.EveryAbilityAcrossAllHeroesHasAUniqueBespokeGlyph | Nine role kits expose 36 slots: 33 real powers and three explicit COMING SOON placeholders. Every real power needs its own concrete glyph sprite; placeholder slots are excluded from the real-power uniqueness count. The duplicate-icon report is a real defect. BARRIER currently reuses DanteShield. `LabelFor` family text may repeat where powers share a world-action job. |
| HeroPresentationTests.TelegraphsMatchWhatTheAbilityPlaces | Rebuild the table from the current role kit and real placed effect. New FROSTBITE loads a slipper; the old ground-effect expectation must not restore an invented floor ring. |
| HeroPresentationTests.EveryHeroAbilityHasBespokeCastAndViewModelActions, ViewmodelArms_PreservesHeldSlipper..., RosterArmGeometryTests.EveryHeroUsesBothHandsAndReturnsCleanly | COMING SOON defender slots are explicit placeholders. Do not invent abilities or treat placeholder presentation as finished; give the current placeholder behavior a matching contract. |
| HeroPresentationTests.EverySummaryFitsTheCardItIsDrawnIn | CURSE: DISORIENTED exceeds the 125-character card budget. This is a real copy-fit defect; shorten the summary or revise the designed card budget, never weaken the fit assertion. |
| InputMapAndAbilityTests charge/cooldown/ultimate-cost cases | The legacy fixed roster and 45-65 second floor do not cover the new role kits and owner-authored numbers. Keep legacy assertions where applicable, and assert each active kit against its current rules table, including timed Drift charges. |
| RuntimeLayerTests.Nemu_AstralProjection_SupportsReactivation | Astral Projection is removed. Do not restore it; replace the stale test with a state/recast contract for the current Kuro kit only where that behavior is designed. |
| SharedUltimatePhaseTests.TwoAcceptedCastsShareOnePhase..., FourCastersShareOneDeadline... | An older report described Phaister HIGOP failing shared-phase acceptance; this is an unverified historical report. The cited XML is absent here. Reproduce on the current build before naming a current failure or root cause; static tracing has not identified a refusal predicate. |
| ThrowEquipmentClearanceTests | This fixture poses a standing body and synthetic charge/spin combinations; its red result is not a locomotion regression. Keep it as a separate geometry diagnostic and compare against an authored live throw before changing character geometry. |
| ToonLightFalloffTests | These pixel-light tests require a graphics device. A -nographics failure is not product evidence. |

## Screenshot Feedback

Unless a disposition below says otherwise, each report remains open for reproduction
against the actual current screen and its nearest existing capture/test.

| Report | Current route and acceptance |
|---|---|
| Title artwork looks blurry in Unity | Inspect HomeCourtView and OwnerMenuArt in the actual title route. Preserve supplied pixels and aspect ratio; compare import and display scaling before changing protected artwork. The full-size valid-mark export remains an external dependency under section 153. |
| Skill-tree alignment and avatar shape | The skill-tree UI is intentionally hidden by ABILITY-2; do not revive it to tune its alignment. The accepted HOME avatar is square. Check the current picker in HubMenus.HubAvatar and HubHome without restoring a circular treatment. |
| Practice card overflow | Recheck HubFlowTests, QueueCardLayoutProbe, and the five-shape UX-1 captures against the supplied viewport. Fix only a reproduced bound/fit error. |
| Lobby room name does not change | Trace the room-title `InputField` through `HostRoom.RoomTitle` and `NetSession.RoomTitle`; verify the committed room title on host and peer using the current lobby tests. This is a room title, not a player-profile name. |
| Terms consent needs a checkmark | The latest direct owner request explicitly adopts a checkmark for this report, superseding the older solid-fill rule. Verify visible accepted state and input/account gating in TumpNativeFrontEndTests.OwnerAccountUsesExactArtworkTypeColoursAndWorkingTerms and OwnerMenuEditsTests; reconcile standing instruction text separately. |
| Password form shows errors | Reproduce the reported validation/error route in SignInScreen.OwnerPainted.cs and TumpNativeFrontEndTests. Preserve supplied login art and account rules; the exact field/message behavior needs the supplied screenshot. |
| Music change | Resolved by explicit owner confirmation on 2026-09-27: it was a prank using a disliked track and is already fixed. No music change or revalidation is needed. |
| Paete looks small in the shop | The current body-scale/framing work is in CharacterVisual.BodyScaleFor, ModelPreview, and HubHero; inspect the actual shop/podium route against HeroPreviewSizeProbe, not only character select. |
| Two Dante avatars in the profile picker | Verify Avatars.Ids, tools/build_avatars.py, and HubMenus.HubAvatar; assert one entry per intended avatar identity and inspect the actual picker in HubFlowTests. |
| Lobby chat layout | Reproduce against LobbyChat.OwnerPainted.cs, LobbyChatStripProbe, and section 127.3's clipped-line case. Check visible bounds and live keyboard/pad/touch behavior. |
| Unavailable characters need dim treatment | This is an availability-state cue, not a request to reduce the character count. Check HubCharacterSelect with HubOwnership; keep every character playable in offline Practice/LAN where current rules allow it. |
| Selected character leaves a stale skill icon | Verify the icon changes with the chosen hero and active role in HubCharacterSelect and ModelPreviewTests.HeroCharacterSelectShowsAbilitiesInsteadOfClassicAttributes. This does not enable alternate skills or restore the hidden skill tree. Duplicate skill icons are also tracked as QA-18 in the loading audit. |
| Paete escape-from-roots prompt | Route through ASKS-0926 and HERO-9: verify the Interact binding and 7-second escape from roots on mouse/keyboard, controller, and touch. This is not an ultimate aim/release indicator. |
| Sean stays stuck after Cheska's ultimate | Treat as a gameplay/replication defect. Trace AbsoluteZero, CharacterMotor.Status, and SyncUnit; verify thaw and interaction recovery on host and peer without changing status numbers to hide the report. |
| Practice awards Tansan | This is an unwanted reward, not missing currency. Ensure practice records cannot pay match earnings in EconomyRules.Settle and ugs/cloud-code/wallet.js, with Core.Tests/EconomyTests.cs and tools/test_wallet_script.js coverage. Keep the tester top-up owner-deployed and temporary. |
| HUD score overflows | Use HudOverflowProbe, HudLayoutProbe, and TumpNativeHudTests on the actual HUD. RuntimeLayerTests.MatchDirector_HasNoPointsCeiling_PastTheReported6000 protects the award path; do not add a score cap to fix text layout. |
| Duplicate skill icons (QA-18) | This is tracked in the loading audit; retain one icon per ability job/state and verify the actual screen after that fix. |

## External Dependencies

- The wallet tester top-up (PLAYTEST_TOPUP = 999999) needs the owner's UGS deploy;
  revert it to zero and redeploy before a public build. Do not touch real user profiles.
- Relay acceptance needs an authorized UGS session and a QA tester on the exact build.
- Human voice lines in VOICE-1 are human recordings only. Owner taste remains the
  acceptance for hero art, motion, sound, and checkmark presentation.
- Recognition of an actually unrecognized physical controller remains a hardware check
  under section 142. Cloud/native simulation does not substitute for it.
- Section 153 still depends on the owner's full-size valid-mark export and OAuth client id.

## Historical Intake Worklist

This list is preserved as intake history, not the current execution instruction.
Follow [plan.md](plan.md) for the bounded wrap-up and TODO for project status.

1. Close the Windows cold-start and first-use performance baseline; fix only measured,
   general runtime or UI-loading costs.
2. Diagnose the open network/relay path with peer evidence and preserve authority,
   identity, protocol, and reconnect rules.
3. Complete the newest ASKS-0926 items, then Amihan/Paete/roster kit work, cast preview,
   interactive prompts, and the hero-specific regressions above.
4. Finish current UI/UX, skill icon/consent/profile issues, hero presentation, audio,
   bot inactivity, and gameplay-animation requirements from the open TODO sections.
5. Run final frozen-candidate regression, physical input, and exact-player checks only
   after implementation batches are ready. TODO/ledger status remains parent-owned.
