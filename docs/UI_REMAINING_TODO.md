# Remaining UI — last in the parent queue

Owner decision2026-09-14: no more subagents, including resuming the previous
/root/ui_overhaul. Parent finishes this list LAST after the other game work.
Do not discard existing implementations or claim this overhaul is finished.
The sole former agent stopped at the usage limit. For the new PC its exact pending
patch and source record are preserved in
docs/reports/improvement-2026-09-14/ui-deferred-source/. No clone or agent is needed.
The source-PC ../TumbangPreso-UI checkout is historical, with no pushing from it.

## Saved implementation and exact pending change

Primary already integrates native title/play/sign-in/credits, portrait/equipment/
skill pickers, settings/controller/touch, HUD/held-reference/chat, pause/intermission,
results/ranks, room browser and nonmodal queue. Source-ready does not mean final
visual/taste acceptance. Approved original controller artwork/buttons/callout
lines are preserved; do not replace that screen again.

Unintegrated commit in UI clone: f5b10a368f0f73cd376d785ba486172cb11321d6. It refreshes
held skill-reference text when timing/charges/glyph/cost changes on the SAME kit
instance. No gameplay reset. Parent19d19021 now preserves kit identity on sidegrade
changes; incorporate this UI follow-up when resuming this final queue. Portable
pending-held-info.patch passed git apply --check at transfer, but is NOT applied.
Recheck/review it against the then-current source before integration.

Latest validation: primary Logs/ui-native-batch-v1.xml ran11targeted native cases,
10passed,1failed. Failure is TumpNativeSettingsTests.
PauseEscapeRespectsChildSettingsDiscardAndReturn: missing named element at
TumpNativeSettingsTests.cs:171 through Find:193. Investigate actual child settings/
discard/return flow; do not suppress it or rerun the whole game suite. Separate
JoinAttemptGateTests3/3passed. Prior picker2/2, settings3/3, front-end3/3 and HUD2/2
receipts are preserved. Latest screenshots: Logs/shots-native-ui.

## Remaining implementation

- Finish new native preparation/loadout lobby and custom-match setup, including
  mode/map/player/party choices, readiness, device navigation and return paths.
- Finish profile/career/progression/leaderboard or ranking contexts already in
  the game; use the new rank emblems and genuine portraits/icons where suitable.
  Preserve identities, saved data, titles/rewards and eligible social actions.
- Finish loading/transitions, training/tutorial prompts and remaining in-game
  contextual UI; no legacy builder reuse except approved controller screen.
- Audit remaining screens/adapters, spectator readouts, icons, dialogs and empty/
  loading/error states. Preserve every existing function; do not simply delete UI.
- Complete visual critique/polish of already implemented HUD/results/room browser/
  queue/front-end/responsive screens and fix the known layered Pause/Escape case.
- Integrate the pending held-reference cache correction, review controller/touch
  editing and profiles, then check complete title -> setup/join -> match -> pause/
  settings -> results/rematch flows with focused cases and actual screenshots.
- Review local/online both modes, narrow/4:3/16:9/readable text, focus/Back behavior,
  intuitive icons plus concise labels, low overload, actual world readability.
  Do not claim physical-controller or external-service qualification from fakes.
- Deliver editable-theme/layout/component/source-art guidance for the girlfriend's
  eventual long-term replacement. It is not imminent and not an excuse for placeholders.

## Owner design requirements and source references

NEW native views/builders from empty roots; never reskin the old hierarchy or
copy one universal button/panel template across everything. Preserve old source
inactive and working domain behavior. Quirky handmade shapes and restrained
Filipino character. Full exact palette: deep red, orange, peach/cream, yellow,
yellow-green, olive/dark olive, anchored to supplied original TUMP logo. Keep
Darumadrop One as MAIN font including settings/scoreboard; use it large enough,
with a supporting face only for genuinely small/dense text. Use actual portraits,
equipment art and appropriate icons, not text for everything. Preserve original
controller design with18button callouts/connecting lines and mappings.

Main menu reference: Slay the Spire's calm simplicity, few choices, game name and
simple animated background giving a glimpse of TUMP. Original49-page PDF is
inspiration; pages39-49 are composition ideas, NOT final layouts to copy.

Preserved in ArtSource/ui/owner-brand-2026-09-13: TUMP-moodboard-original.pdf,
logo.jpg, new tump text.jpg, new tump text with some  texture.jpg,
slipper with hit.jpg, original-logo-sheet-owner-latest.png and
palette-sheet-owner-latest.png. Source/manifests retain paths/hashes. Originals:
C:/Users/Matthew/Downloads/TUMP (1).pdf and C:/Users/Matthew/Downloads/claude/.
Resources/UI/brand contains transparent original marks; Resources/UI/portraits
contains34reviewed thumbs; Resources/UI/illustrations contains generated street
art, with prompts/critique in generated-art-manifest.json. Keep native text,
shapes/layouts editable, separate from bitmap art; never redraw the original logo.
Imagegen is authorized for useful original backgrounds/icons/concepts, but
critically reject ugly outputs. Do not claim an exact model version not exposed
by the tool. Figma fileI1snFz26WypEywfWMhF9jq reached its free MCP allowance;
no paid upgrades, quota bypass or unnecessary Figma work. Existing artifacts/
IDs are references, not proof of final UI quality.

More detail: archived UI_EXECUTION_STATUS_ARCHIVE.md (latest throughf5b10) and
source reviews in the portable ui-deferred-source folder, plus the active plan.
Selected real UI screenshots and the known failing XML are in the sibling
transfer-evidence folder; old Logs paths do not exist on a fresh clone.
No reset authority. No agents. No Desktop update unless newly requested.
