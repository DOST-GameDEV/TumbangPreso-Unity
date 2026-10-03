# Working Rules

Standing technical and product contracts. Read the relevant section after
[AGENTS](../AGENTS.md); task order belongs only in [TODO](TODO.md).
Complete incident history is preserved in the [pre-cleanup rules](archive/snapshots-2026-09-27/CLAUDE.md)
and [earlier full rules](archive/CLAUDE_full_2026-09-24.md). New owner instructions win.

## Gameplay And Authority

- This Unity repository is the game. The Godot repository is frozen reference;
  never edit it or copy its versions over live Unity documents/source.
- Classic and Hero Strike are both first-class modes,defaulting to eight rounds.
  Classic people are cosmetic with neutral stats. No Street Hype mechanic/meter.
  Preserve throwing,retrieval,restoring the can,chasing,escape and tagging.
- Core stays engine-free. `Core/TumbangPreso.Core.csproj` compiles the embedded
  package sources in place; do not create another copy or add UnityEngine to it.
- The host resolves outcomes and `MatchDirector.AddScore` owns every point.
  Contact is resolved by distance,not trigger callbacks. Stable identity is not
  permission to control a seat. See [network contracts](SKILL_NETWORK_CONTRACT.md).
- Defender is derived from `(round - 1) % 4`,not accumulated. The confinement box
  clamps X/Z independently. Bots submit the same InputIntent as humans.
- Stuns overlap with Max,never addition. Impulses derive from friction using
  `distance = speed * speed / (2 * friction)`. Neutral prop stays at index0.
- Possession contact outcomes freeze at requested game speed zero. Keep the
  accepted possession and its live contact behavior when the user resumes.
- Carrier retires the opposite role's pending throw or can reset before consuming
  the current role. Preserve held shoes and same-role charge/channel progress.
- Combat retires the defender's pending lunge windup before attacker input.
  Keep already committed contact windows and spent cooldowns.
- People use first person,props third person; local emotes temporarily change view.
  Spectators have separate free/follow/POV rigs. Emotes end through EmotePlayer.Stop
  on interruption,not a new arbitrary timer that bypasses view cleanup.
- Current rules/source beat old proposal tables. Reconcile real balance changes
  with [Design](Design.md) for Classic and current hero/Core rules for Hero Strike.
  Record a disagreement rather than silently changing gameplay to match old prose.

## Input And Access

- Mouse/keyboard,controller and touch are required for every feature. Gameplay and
  spectating are different contexts; one control does one action within each.
- Reader withdrawal and parked control zero a controlled familiar's cached
  movement while keeping its accepted possession. Retain explicit zero input
  and local/offline custody; do not fall back to body AI or clear another seat.
- A withdrawn reader retires local throw/lunge windup after losing its network
  seat. Preserve newly received tells; clear its old local tell when no received
  refresh replaced it. Keep committed contact and spent cooldowns.
- Practice world reset retires active enabled bot input before teleporting.
  Disabled brains retained on human seats do not own that seat's input; leave
  its held controls intact. Preserve the existing shared human hero-key branch.
- A predicted punch owns its press until observed release, including after a
  host refusal refunds cooldown. A cooldown-blocked initial edge is unspent;
  observe release before interruption or role returns skip the punch path.
- Preserve GenericPadBridge,MenuNav,controller mappings and the input backend.
  `InputCatalogue.For` is exhaustive: no discard arm; keep CS8509 as an error.
  Add pad/thumb mapping with a Verb and run the existing InputAssetSync regeneration.
- Non-verb actions belong in ScreenInputCatalogue. An explicit unsupported path
  is different from forgetting a mapping.
- Build canvases through the applicable existing kit. Keep ScreenFocus,thumb targets
  and navigation. Menus back out through MenuNav; its legacy Escape fallback also
  carries Android hardware Back and must not be removed as dead code.
- Prompts read actual bindings through Rebinding.DisplayNameFor. Keep device/context
  separation and every binding index; never teach hardcoded keys.
- New wire semantics require compatibility changes. Input-only/cosmetic changes do
  not alter protocol. Ranked preserves device-separated pools; casual remains crossplay.
  Windows/Android compatibility requires matching gameplay contracts and actual peers.

## UI And Journeys

- Use [UI design method](UI_DESIGN_METHOD.md),[UI authorship](OWNER_UI_AUTHORING.md)
  and [font roles](FONT_USAGE.md). Supplied art is the design,not a placeholder to
  redraw. Keep its pixels,aspect ratios,control identities and working connections.
- Keep the existing title TAP TO START and current HOME routes. Do not restore old
  pennants,retired layouts,extra HUD timers or a second door to the same feature.
- Account/login faults belong under their fields. A bad submission gives one error
  cue and field-only feedback; good fields stay intact. Keep passwords masked and
  server credential errors ambiguous when the service does not identify the bad field.
- Consent now uses the owner's requested check mark; this supersedes the earlier
  solid-fill-only note. The actual consent gate still applies. Loading tips are inline.
- No UI version stamps. Internal build/protocol identity remains. Branch names never
  enter wire version identity.
- Keep HUD text/icons outlined BLACK through UiTheme.InGameOutline. No red/DeepInk
  outline substitution on match UI. In-game HUD labels have a28canvas-unit floor.
- Programmatic menu colours follow the supplied palette; no navy/blue/cold-grey
  reskin. Gameplay offense/defense colours and supplied artwork are exceptions,
  not invitations to repaint them. See theme source and the current screen's art.
- Settings-shaped screens use row/column layout helpers. Authored login composition
  uses its measured geometry. Do not impose one layout recipe on every screen.
- Escape closes the innermost layer once; preserve ScreenTakeover consumption.
  Gates have a one-press offline escape where designed. Removing a full-screen
  graphic requires preserving its input-blocking role as well as its appearance.

## Character And Presentation Methods

- Models start with [CHARACTER_MODEL_METHOD](CHARACTER_MODEL_METHOD.md),the standing
  style in [Art_Direction](Art_Direction.md),[cast clothing constraints](CAST_CLOTHING_STYLE.md),
  and the [voxel guide](Voxel_Person_Guide.md). Preserve rig/bone paths,GUIDs,flat
  faces,simple no-thumb hands and each character's deliberate identity/quiet colour.
- Author walks,runs,idles,poses,casts,faces and proportions ONE character at a time.
  Shared evaluators/plumbing are fine;a shared copied look is not. Each authored
  change needs that character's context and appropriate new visual/motion evidence.
- Kits,casts,effects,sound and cutscenes start with [HERO_KIT_METHOD](HERO_KIT_METHOD.md).
  Reuse its research/six-beat/review method,not Paete's look. Character reworks use
  shared network contracts instead of bespoke animation/effect RPCs.
- HOME/menu/season/showcase animation starts with [HOME_SCREEN_ANIMATION_METHOD](HOME_SCREEN_ANIMATION_METHOD.md):
  real models,unchanged faces,distinct direction,a story about TUMP,every beat breathing.
- Preserve successful art. Nemu's covered lower face and Cheska's colours are deliberate;
  Inday's arms are plain brown; Rafi has no gills. Consult the latest character record
  before treating an intentional signature as a defect.
- New character/prop work must belong to the cute blocky world. No realistic anatomy,
  unnecessary surface noise,killing,guns or lasting destruction. Keep the can,
  slippers,players,chalk and routes legible on Low and during overlaps.
- Do not decimate,lower texture resolution,collapse materials or recolour supplied
  assets as a speculative optimization. Measure first. Import settings,shader,
  placement and size are separate from changing supplied source art.
- Models need native turnarounds and cast comparisons; motion needs time samples
  AND normal-speed owner/observer playback where relevant. A posed frame is not
  animation acceptance. Use versioned filenames and force-reimport rebuilt sub-assets.
- Human hero voice recordings only. Sourced SFX remain provisional until heard in play.
  Restore only a named rejected cue,never roll back the entire audio pass; retain
  alias/replacement-table consistency. Source provenance remains in Asset_Sourcing.

## Validation And Publication

- Use [TESTING](TESTING.md) for current commands and evidence levels. One focused
  pass per coherent change,at most one bounded tooling retry. Reuse unaffected
  evidence; no broad suite/film repeats for minor or cosmetic edits.
- Freeze tested inputs. One heavy job; isolated writable caches/profiles/output.
  Use run_unity_guarded.py with a named profile. Never launch a diagnostic against
  the real player profile or delete profiles to fix a test.
- Graphics/native visual checks need a real graphics device,not `-nographics`.
  Judge test XML with fresh nonzero expected cases,not exit0 or a stale Passed label.
- A raw single-process PlayMode run is not the full regression gate. Grouped test
  isolation,real-peer checks,target-device play and human visual/audio judgment prove
  different things. A failed native launch is not runtime evidence.
- Keep seeded tests honest; one noisy bot run is a liveness check,not an A/B result.
  Source-text checks and native scene checks are complementary,not interchangeable.
  Use actual network-link tools,not obsolete simulator APIs that do nothing.
- Discover installed tools/current ProjectVersion and use the guarded build workflow.
  Builds go to explicit internal Builds/<candidate>/TumbangPreso.exe,never overwrite
  the Desktop player without a new owner instruction. Verify/run the exact output.
- Preserve profiles,unfinished changes,source assets and unrelated processes.
  Stage explicit owned paths; fetch,inspect divergence,integrate and push ASTRAReworks
  without reset/clean/force. Sole author M4tyu633; git commit -F,no attribution trailers,
  tooling references or em dashes in repository prose/commit messages.
- A handoff belongs in chat,not a committed prompt. Include rules,branch/HEAD,
  actual checks/build state,changed behavior and the next TODO pointer. Never turn
  a checkpoint into a completion claim while authorized work remains.
