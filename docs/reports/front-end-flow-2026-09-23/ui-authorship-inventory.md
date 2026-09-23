# UI construction and refinement inventory

Owner2026-09-23: build every missing UI surface in the added flow, do the best visual
work now, and keep an accurate inventory for a possible later refinement pass.
Future refinement is optional quality review, not permission to leave rough UI now.
This file is a local record, not a message or task sent to another conversation.
Login/main menu and already-completed in-game HUD remain protected except the specific
new arrival/countdown requirements. TODO owns status; this file owns surface locations.

## Existing UX-1 implementation inherited

The complete file map is ux1-plan.md section7b. HOME, GAMEMODE SELECT, HERO, LOADOUT,
ITEM, SKILL TREE, TASKS, SHOP, menu/avatar, HOST/JOIN/LOBBY, queue/MATCH FOUND,
CHARACTER SELECT and LOADING existed before this handback. Preserve useful design;
fix specific defects. Concurrent HOME video work belongs to its existing author and
is not attributed to this pass.

## Built or changed in this continuation

- LOBBY arrival behavior: Runtime/UI/Hub/TumpHub.cs and HubCustom.cs. Automatically
  opens on external room entry; repeated events retain existing lobby/subpage. No
  visual redesign. Focused runtime acceptance pending after first-run fixture fixes.
- HOME player tag: HubHome.cs. Larger text required44units in a36unit box; taller
  label plus a thinner XP track uses existing space. Before measured in changed-flow-
  v1.xml; after screenshot pending. Other HOME artwork/composition retained.
- CHARACTER SELECT role line: HubCharacterSelect.cs. Larger-text52unit content needed
  more than44units; enlarged row and shifted heading locally. Screenshot pending.
- LOBBY address: HubCustom.cs. Enlarged40unit box to48 for measured44unit content.
  No palette/art changes. Screenshot pending.
- Rafi READ THE STORY content: Resources/UI/character-stories.json. Missing record
  restored from BADJAO_EXPANSION's existing character lore; other heroes unchanged.
  All-hero story/return-path acceptance pending.

## Missing surfaces to build for UX-1.12

- MAP VOTE after character lock-in: show actual map choices, the four players' votes,
  remaining decision time, selection and host-decided result. File/location and
  evidence to be added as implemented. Reuse the hub's approved shapes/type/input.
- Deliberate LOADING presentation: extend existing HubLoading rather than duplicate
  it. Map/name and a brief staged loading beat; actual loading still has to finish.
- Map/player arrival introduction: real3D map and all four characters,5..10second
  camera choreography, map/round identity where it helps. Reduced-motion route and
  loading/late-peer/cancel behavior must be explicit.
- Automatic3/2/1/START arrival: reuse countdown owners, suppress manual R prompt on
  queued routes. Integrate with the introduction and readiness, no duplicate HUD.
- Custom-room MANUAL READY option: visible room-owned toggle, replicated/host-owned,
  whose setting changes the ready gate. Record exact settings screen/file on build.

For every item above, append the actual source paths, intended first action, back/
focus/touch behavior, reference, normal/large-text captures, inspected defects/fixes,
and honest remaining checks. Do not call a screenshot or a green fixture owner approval.

## UX-1.12 implementation in progress

- MAP VOTE: Runtime/UI/Hub/HubMapVote.cs, IHubHost and ConvertedMatchSetup.Hub;
  TumpHub opens the replicated ballot. Five actual court images, names, own vote,
  four seat portraits, clock and winning court. No duplicate confirm action.
  Net/MatchRpc.QueueArrival.cs owns the bounded host vote and decision; existing
  result votes remain separate. References reuse existing native map-stage frames
  (all five personally inspected); exact input hashes in completion/map-card-provenance.json.
  Refresh each thumbnail when its map's later REFINE-2 art changes. New screen render
  and all three input-device checks are still pending; no final visual approval.
- LOADING: existing HubLoading now holds a deliberate minimum2seconds and refuses
  to label a stale preview frame as the newly voted map. Real installation still gates it.
- Introduction: Runtime/MatchArrivalPresentation.cs, eight seconds in the actual3D
  arena, establishing pan, four actor views, return to the gameplay camera. Uses
  registry angles, occlusion queries, current physical positions, original camera/grade
  and controlled pre-round input hold; reduced motion keeps a stable wide view.
  OwnerUiLayout/HubKit builds its map/player caption. Camera/hold cleanup is explicit.
  Local real-LAN queued arrival and reduced-motion cleanup cases passed; actual
  in-editor sequence inspected. Separate native peers/motion review remains pending.
- START: ReadyGate automatic mode keeps the existing peer quorum/countdown and
  suppresses R, with repeated post-intro ready acknowledgement for late host load.
  Custom-room THE ROOM/MANUAL READY row in CustomGameScreen.MatchSlate.cs controls
  the new appended CustomRules.ManualReady suffix. Older short wires keep manual.
  Core compatibility checks3/3 passed within the full629/629; runtime compiled and
  actual automatic START/manual-ready cases passed. Separate native peers pending.

## Loading and Terms additions, owner2026-09-23

- LoadingArtwork.cs: shared three-image deck,5second hold, gentle crossfade and
  reduced-motion instant change. Both SplashScreen.CourtLoading and HubLoading use
  it. Tips are visible, non-interactive text on the loading surface; no second HUD,
  story modal or click-to-reveal. Progress/readiness still come from the original
  owners. First image advances between loads without changing gameplay random state.
- Resources/UI/loading-illustrations:01-street-court-v1,02-lagoon-deck-v1,
  03-rooftop-court-v1.png.1672x941each, copied into the project; prompts and exact
  provenance in loading-art/. No discarded draft passed off as final approval.
  Three complementary street/deck/rooftop illustrations personally inspected;
  actual boot/match crop and contrast inspected; three-image rotation/readiness passed.
- OwnerTermsView.cs: wider warm-dark document popup,13clear sections, real masked
  scrolling, visible scrollbar, fixed back arrow/I AGREE, keyboard/pad focus in the dialog.
  OwnerUiLayout/HubKit family, no decorative texture across the reading column.
  Source play-terms.txt nowabout1500words; product facts and legal drafting limits in
  terms-content-notes.md. Five-shape normal/large render and accept/cancel checks passed.
- SignInScreen.OwnerPainted.cs terms control only: visible38unit empty border and
  solid28unit interior fill when accepted, no check glyph. Existing Toggle and
  validation preserved; opening/BACK do not accept. Other supplied login/title art
  unchanged. The owner explicitly authorized this narrow exception.

## Navigation-copy correction, owner2026-09-23

Owner likes the Darumadrop One/Nunito Bold pairing but rejects obvious navigation
instructions. TermsBack is now arrow-only; removed SCROLL TO READ/END OF TERMS and
the BACK explanation, expanding the actual reading viewport into the freed space.
Removed redundant ability-icon/model-drag captions and mode-selected toasts teaching
PLAY. Queue failures report state succinctly; map voting states its real rule.
Current rules, biography, credits, profile, settings and retained live join-browser
Back controls use OwnerTextAction.CreateBack; original callbacks/control IDs remain.
Profile/join arrows retain their required light foreground on dark backgrounds.
Legacy NavigationSymbol also omits Back words. No gameplay binding/error/rule copy
or destination labels were removed. User feedback is not blanket approval of all UI.
Latest Terms-popup-normal-960x540.png in completion/ now shows the arrow-only
control and expanded reading column, personally inspected. Latest UI completion
run passed Terms/profile/story/custom-rules cases; complete a11y route through lobby/loading passed at five shapes. Older footer captures remain historical only.
