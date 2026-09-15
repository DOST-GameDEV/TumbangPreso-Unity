# D5 player record book

The player hub now uses a record header, file tabs and aligned editable rows for
profile, friends, career, history and account management. It uses no login artwork
or orange wallpaper. The login itself remains exactly as approved. Career totals
appear before the detailed rank group. The scorecard has larger columns/type and
space for four players plus the round-defender history without cutting that note.
Native uGUI dropdown behavior powers the record choices; its wrapper registers
modal ownership so Cancel leaves the profile underneath open.

Profile drafts survive tabs, section collapse and service refresh. Existing account,
social, save and deletion callbacks remain unchanged. These checks did not submit
profile edits, friend messages/requests or deletion. Career/scorecard values in
populated screenshots are explicitly temporary local test fixtures, not real results.

## Verification

Records-v1 passed3/3. After the visual refinements and added Escape check, records-v2
passed the draft/data-layout cases2/3 and failed its new unattended keyboard driver.
Choice-cancel-v3 rechecked that case1/1 after configuring focus behavior before
creating and enabling the simulated keyboard, matching the established settings
fixture. No production change was made to make that recheck pass. Logged input:
keyboard enabled, Cancel performed, native list retired, owning CareerModeValue
selected and profile still open. Receipts: records-v1 672f43be2282;
records-v2 86cd30f58993; recheck63d61aadff6e. Both failed and passing XML retained.

All five hub pages and populated career/scorecard views cover960x540,1280x720,
1366x768,1920x1080,1920x1200,1280x960,2560x1440,3440x1440,3840x1080 and3840x2160.
Visible bounds now cover every Selectable, including inputs, dropdowns, toggles
and sliders. A native dropdown's override-sorting canvas is not incorrectly
excluded by its parent row's scroll mask. The open popup was captured at960x540.
Text fit, actual characters and the14pixel floor are checked. This is isolated
Unity UI/synthetic input evidence, not physical monitor/controller certification.

Existing approved controller art and original input/profile data are preserved.
No player build, new image generation or live account/social mutation this batch.
Remaining custom rules, match/UI surfaces, U8 native motion/routes and physical
preview-pixel review still precede the entire gameplay bookmark. No owner visual
acceptance or full project completion inferred from these focused checks.
