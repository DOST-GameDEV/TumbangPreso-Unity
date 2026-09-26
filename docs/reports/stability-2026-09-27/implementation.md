# Interaction and room lifecycle integration, 2026-09-27

## Source findings and changes

- `Hud.Update` returns through native presentation before its legacy Paete interaction card. `TumpMatchReadout` now reads rooted escape and nearby pullable opponent plants on the active action prompt. The existing Interact binding, touch emphasis and progress track are reused. Root escape retains partial progress on release; uprooting resets it. Spectating hides this surface.
- Can reset cues now use `Carrier.HasResetTarget`, the same flat reach check as the action. Beyond reach the prompt asks the player to approach; only a usable reset emphasises the touch Grab control. Hold/toggle wording uses the current device and current binding. This changes presentation only.
- `ConvertedMatchSetup` versions room requests; leaving closes the join panel's attempt and stops pending transport work. Each join owns its status handler. A cancelled request cannot clear a newer request's status/network flag, and Host/Join screen continuations ignore work cancelled by Back.
- Reviewed the session and queue cancellation changes together. The queue waits for connection plus assigned seat, not only `StartClient` acceptance. A completed connection remains a successful wait after `Update` clears its pending attempt. Failed-wait cleanup preserves timeout/refusal text, and the local shutdown callback no longer overwrites that reason with a generic disconnect.
- The icon test now covers all 36 screen slots: 33 unique implemented powers and the explicit Sean/Zack/Rafi defending placeholders. Placeholders use one neutral unavailable symbol with COMING SOON text. See [skill-icons.md](skill-icons.md). Native symbol cache and neutral-shape integration are a separate coordinated source edit in this batch.

## Validation plan and evidence

Source revision: `04886cc4` plus the current explicit dirty file set. No native run was launched during implementation. Scoped `git diff --check` passed; compiler and native claims remain unverified until the frozen run.

Coordinated first-run results, source frozen:

- `Logs/stability-0927-presentation.xml`: 24/24 passed, 0.758 s test duration.
- `Logs/stability-0927-session.xml`: 11/11 passed, 3.719 s test duration.
- `Logs/stability-0927-hud.xml`: 2/3 passed, 22.210 s test duration. Plant/uproot and can-reset cue tests passed. Rooted prompt correctly showed the live F10 binding but failed at `TumpNativeHudTests.cs:212`: `BreakFreeProgress` remained zero after the synthetic keyboard hold. Pad/touch/root-release/completed-escape claims were not reached.

Read-only root-input diagnosis during the subsequent source freeze: production `PlayerInputReader.Awake` reads the shared Resources input asset, caches its Interact action and enables its Player map. It does not clone or restrict devices. Interact is sampled before movement/restore control gates; `StepBreakFree` checks rooted, local simulation and effective Interact intent. The first run did not log hardware/action/intent state, so the failed result alone cannot distinguish disabled hardware from a parked or locked intent.

The leading fixture defect is device setup order. The root test adds synthetic keyboard and pad before setting IgnoreFocus, and does not explicitly enable them. The installed Input System's `InputManager.cs:1435` disables newly added devices when the game lacks focus; `ApplySettings` does not re-enable such devices. Existing `SlipperRecallShots` explicitly enables its devices after configuring focus behavior, and `RecoveryMenuBoundaryProbe` sets focus policy before adding them. A changed key label reads binding metadata and does not establish that the key event arrived.

One bounded retry is proposed after source freeze release: configure background input before adding devices, explicitly enable them, retain all existing behavioral assertions, and add narrow diagnostics for enabled device, raw key, action enable/pressed state, reader asset identity and activity, effective intent, parked/locked state and presentation hold. No C# or asset edit was made during this diagnosis. Retry budget remains unused pending that repair/run. This is a likely fixture cause, not a confirmed runtime diagnosis until the retry records the input path.

After explicit freeze release, the approved fixture repair is applied to the rooted case only: focus settings precede device creation, both synthetic devices are enabled, and a single `RootedInput` line plus direct hardware/action/intent assertions identifies any remaining break in the path. The reader's asset identity and active state are checked before the hold. Existing live cue, positive progress, release, device, spectator and escape assertions remain. Production input code is unchanged. Repair count: one; its single focused retry is pending coordination.

The focused run asks whether actual native HUD prompts follow the live input and action state, and whether cancellation can revive an older room attempt. Stop on the first completed focused result; one bounded tooling repair/retry at most. Retry count before the first run: zero.

Existing `TumpNativeHudTests` additions:

- `RootedPromptTracksLiveBindingsDevicesAndHeldProgress`: live keyboard rebind to F10, actual keyboard/pad/touch hold progress, release retention, native captures for each device, touch emphasis, spectator suppression and completed escape.
- `PlantPromptRequiresAnEligibleOpponentPlantAndTracksThePull`: own/protected/out-of-range refusal, eligible nearby plant, progress/reset and completed uproot; one native capture.
- `CanResetTouchCueUsesTheActualReachAndRestoreSetting`: unreachable and reachable cues, hold/toggle wording and progress. This uses effective `InputIntent` for the restore channel, so it validates cue/range/setting, not physical tap-to-toggle input handling.

`TumpNativeJoinTests.LeavingTheHubCancelsItsJoinBeforeANewerRequestStarts` covers the real controller and join-panel attempt ownership with delayed local completions. `SessionRestartTests` owns transport restart, cancelled pending client, queue completion ownership, seat readiness and timeout checks. These are local lifecycle checks; they do not establish live UGS allocation, two-peer admission or reconnect behavior.

## Performance instrumentation review

The inherited `GameBuilder -developmentBuild` option leaves the default release option unchanged and selects Development plus ConnectWithProfiler explicitly. The inherited `OwnerUiPlayerReview -tp-review-all-frames` extends active measurement windows to include non-round frames and records histogram long-frame counts/max. It does not open a startup measurement window by itself. The original shader warmup path is still unchanged for the required before measurement; no performance improvement has been measured or claimed.
