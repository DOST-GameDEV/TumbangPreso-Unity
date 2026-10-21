# Existing UI completion plan, 2026-09-22

Scope: TODO153.18/153.20, U8 and retained existing UI qualification. Preserve all
old task IDs and the legacy capability inventory. This is integration of current
painted uGUI screens, not a visual redesign. Supplied title/login art, current
compact halftime popup, backend input mapping and stable save IDs remain.

1. Review the original dirty migration as one coherent dependency set. PaintedScreens
   locates detached root canvases and enters the real settings owner; preserve all
   assertions about capabilities, selection, save/discard, reachability and overflow.
2. Verify settings scroll/wheel, controller/touch return, rebind save/discard and
   telemetry full disclosure at supported sizes. Qualify the pending note-height fix.
3. Verify picker, all skill slots/variants, preparation, model preview, cosmetic hub,
   result/splash and HUD text against actual current formatters. Halftime continues
   to use its compact popup; discard only the superseded duplicate long round label.
4. Re-walk the current screens for PaperPurity capability reconciliation. Keep the
   original149 unmatched controls and account for every capability by current route,
   deliberate owner removal or a concrete missing feature to implement. Never simply
   overwrite a baseline to turn the gate green.
5. Check physical-device requirements separately from simulated input evidence;
   check optional external authentication/service dependencies without spending.
6. Run focused fresh XML batches, preserve failures, repair actual causes, then
   qualify changed routes in an internal native player with screenshots/real inputs.
   Publish the coherent owned batch and continue full backlog (network/kit/expansion).

The pre-existing dirty source is backed up in the initial intake. No unrelated
art metas or original Sean diagnostic hook is included in this initial UI copy.

## Qualification checkpoint

Settings7/7, initial picker18/24; the six failed cases are preserved. Four failures
were stale harness assumptions (three capture-directory failures plus old summary
copy), one was the capture disabling the renamed login canvas, and one was the
obsolete inventory route. Repairs pass. Fresh full screens group:113/114,0failed,
1expected batch-mode UGS identity skip,463.937s; all45expected fixtures ran. This is
a combined screen-group result, not a full project gate or authentication claim.

The original338-row baseline stays byte-identical after line-ending normalization.
122identity mappings are explicit in control-inventory-migration.tsv; every original
row is accounted for in control-migration-results.tsv. Title Quit and custom-maker
buttons retain explicit owner-removal history; Cancel still exits the title.
Welcome/queue/ready controls are inventoried from their current constructed owners
and clearly labeled conditional; no sign-in or matchmaking was fabricated. All
other targets are captured through current routes. Banner identity was then
strengthened to its actual group path and passes the focused inventory case.

Actual720pPlayer-settings and skill-page captures were inspected. The complete
telemetry disclosure fits; the compact halftime formatter is preserved. Native
Windows UI and variant candidate remains next, not yet claimed complete.

## Capability-mapping correction, still active

Manual source tracing found one false mapping in the earlier green inventory:
Button_ON was GenericPadBridge.Enabled, not Touch selection. The earlier green
receipt is retained but does not prove that capability. The corrected manifest
now requires toggle:GenericControllerValue and the walker creates a synthetic
unrecognised joystick to expose it. This caught two real faults: missing current
UI switch, and reentrant fallback-device creation in the actual hotplug callback.
The first failed run flooded its retained log and only its verified Editor was
terminated; the second, with just the callback guard, reached and failed the
missing-switch assertion in1.482s. No assertion was suppressed. Current fix adds
the conditional switch/hotplug refresh using the existing backend and settings
save/discard transaction. Mapping arithmetic is unchanged. Named profile guarding
now includes the genericpad preference. Fresh regression/inventory checks pending.
