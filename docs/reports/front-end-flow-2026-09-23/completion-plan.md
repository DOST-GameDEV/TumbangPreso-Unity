# UX-1 completion,2026-09-23

This continues the implemented design; it is not a redesign. Latest owner instruction:
judge which parts actually look bad or fail, and improve those parts only. Login and
the main menu remain protected. Existing TAP TO START destination remains HOME.
The current conversation now owns this unfinished work after the explicit handback.

1. Preserve the incoming implementation and concurrent HOME background work. Run one
   preparation-board baseline for the old MatchSetup fixtures. The inherited ledger
   has only failed-case summaries, not raw XML/full filter. Keep that evidence limit.
   Separate missing retired doors from genuine runtime assertions. Preserve existing
   function contracts when moving affected cases to the real hub routes.
2. Fix the observed external-join gap: TumpHub only checks room membership during
   Build; invites or launch joins completing afterward leave HOME visible. A single
   idempotent ShowLobby must handle install and Host/Join completions plus the rising
   membership edge, defer behind existing overlays, and never hijack queued selection
   or continuously close a lobby's character/loadout subpage. Test real external room
   opening, one lobby instance, and a subpage surviving repeated room observations.
3. Add the fourth HubFlow case for High contrast plus Larger text. Walk existing real
   doors and a real local lobby, five shapes; use distinct evidence names and restore
   settings. Reuse the existing capture machinery. Fix only concrete layout/readability
   defects visible in the resulting frames, without rewriting the drawing system.
4. Classify actual integration audit findings. Five event publishers are screen-owned
   children and the wallet day is a calendar preview; do not rewrite sound lifetime or
   clock code for a text audit. Register HubEnabled and its board flag as view choices,
   not gameplay modifiers. Run the four requested EditMode fixtures and Checks.RunAll.
5. Qualify actual native peers on the finished UI: joins, roster/ready, queue found,
   rematch and reconnect. Label LAN/local transport separately from online UGS. Verify
   project access for wallet deployment; inherited missing-CLI note is stale on this
   machine (ugs status reports a configured key). No paid services or credit purchases.
6. Finish regression and an internal native build, keep original failures visible,
   and update TODO/ledger/evidence with the production change. Physical controller,
   phone and human taste are explicit external checks, never invented completion.

Avoid tooling loops: one baseline and one focused changed-flow pass, plus the requested
final qualification. If a capture tool fails, allow one bounded repair/retry and retain
any remaining gap while finishing independent game work. Genuine game defects receive
their smallest relevant follow-up. Older P6/P7 and REFINE-2 tasks remain in the queue.

## Added match-arrival sequence, owner2026-09-23

Exact request:
> main screen (selected rank) -> play -> queue time -> match found hud -> character select -> map vote -> hard coded loadingh screen (js to show loading haha) -> short map introduction/round introduction of everyone (js camera panning in like 5-10 seconds ) like in super mario idk -> 3 2 1 START! (no more ingame click r to start) but custom can have but make ts togglable

This adds UX-1.12; it does not erase existing tasks or alter protected login/title.
Implementation first inspects the existing network map ballot, readiness/loading,
intro camera and countdown so completed parts are reused. Queue entry must carry a
match-arrival policy across the scene boundary; the host gates map selection, load
completion and countdown for all peers. Character lock-in must lead to ballot rather
than immediately entering the arena. Give the loading curtain a deliberate brief
minimum presentation, while never releasing gameplay before actual loading/peers are
ready. Author a bounded5..10second map/player camera introduction in the real3D map,
then the existing3/2/1/start beat. Manual in-arena ready is off on queued routes and a
visible custom-room option, not a per-player preference that can desynchronise peers.
Record exact file-level plan after inspecting the owners, before code changes.

### UX-1.12 file-level construction plan

Inspection confirms the missing links: HubCharacterSelect.Tick currently calls
StartGame as soon as picks lock/time out. Existing map ballots only target MatchResult;
there is no pre-match vote screen. HubLoading already exists and holds1.2seconds.
ReadyGate explicitly requires another ready press in the arena and owns the existing
network quorum/countdown. MatchInstaller.BuildReadyGate wires it to the HUD and runner.

1. Append a ManualReady field to CustomRules and its compact wire suffix, preserving
   old short-string defaults. Clone/validation and host room sync must carry it. Queue
   starts force automatic; a custom-room THE ROOM option chooses manual or automatic.
   CustomGameScreen.MatchSlate owns that row, with RefreshOwnerRules/Apply handling it.
   Keep the existing protocol prefix/seat authority, and document compatibility.
2. Add a pre-match ballot owner to the existing MatchRpc partial family, with host-only
   phase/deadline/decision and four seat-resolved votes. Reuse MapRotationRules and the
   existing SelectMapVote/MapVoteTally routes when this ballot is active; retain the
   results-board behavior otherwise. A dedicated small phase message opens/synchronises
   the ballot on every peer. Validate sender, sizes, indices, finite times and phase.
   Repeated state is idempotent; disappearing seats stop counting toward completion.
3. Extend IHubHost/ConvertedMatchSetup.Hub with ballot view/actions. HubCharacterSelect
   advances to ballot; TumpHub observes the replicated ballot and opens HubMapVote once.
   Five map cards, actual map identity/art, one selection, seat portraits/counts and a
   clear bounded clock. Match host decides winner and only then calls the existing
   HostStartMatch/SelectMap path. No second arena-start function.
4. Extend HubLoading's existing curtain to a deliberate minimum2second presentation.
   Its staged progress is cosmetic; it never announces gameplay ready before real arena
   install. Expose whether it is still covering the view so the intro starts afterward.
5. Add scene-owned MatchArrivalPresentation: eight seconds total, wide establishing
   court angle then a slow move showing all four spawn positions, ending toward play.
   Use real map ground/positions and the existing camera/grade, preserve and restore
   rig/self-hide state. No teleports/repositioned characters or world redesign. Reduced
   UI motion uses stable framing with a quiet dissolve; no sweeping camera there.
   Reuse PresentationClock's pre-round input hold with cleanup on interrupted load.
6. ReadyGate automatic mode hides the R prompt, waits for local loading+intro, and
   repeatedly acknowledges ready through the existing idempotent peer-ready message
   until countdown begins. Host waits for its introduction and the actual playing-peer
   quorum; spectators never manufacture a ready vote. Reuse3/2/1 with START wording,
   same announcer/runner event. Manual custom rooms retain the existing press gate.
7. Meaningful checks: core wire/default/clone and ballot decisions; actual hub stage
   transitions and automatic-vs-manual gate; camera cleanup/reduced motion/late load;
   focused native peers on final candidate. Add source paths and real screenshots to
   ui-authorship-inventory as surfaces land. Do not certify network timing from a still.
