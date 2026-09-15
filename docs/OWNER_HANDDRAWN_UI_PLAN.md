# Owner hand-drawn UI replacement

**Owner clarification:** still try to complete the ENTIRE to-do list. Visibility
changes priority, not scope. Secure a tested demo candidate, then continue the rest
immediately as time allows, including before tomorrow. Do not stop at the candidate
or wait for the demo date. Record completed/verified/pending-playtest states in
TODO.md, ACTIVE_REWORK_LEDGER.md and DEMO_PLAYTEST_CHECKLIST.md.


**LATEST owner priority, September15: demo tomorrow, September16.** Follow the new
ordered CURRENT IMPLEMENTATION QUEUE in docs/TODO.md. First fix main-route blockers,
prove both modes in a fresh native player, check visible existing-hero gameplay,
validate needed LAN continuity, then freeze/rehearse a versioned demo candidate.
This supersedes finishing all UI before any gameplay. Broad polish, exhaustive
network/device matrices, deferred Inday and the seventh hero/map remain saved later.
Keep working through stable checkpoints. Preserve original login,8-round defaults,
dark settings and all existing creative constraints. No agents, paid fallback,
Figma, resets or Desktop-build update. Primary assumed demo is Windows offline bots;
LAN is secondary until the owner says otherwise.


**LATEST owner correction, 2026-09-15: KEEP THE PREVIOUS LOGIN LOOK.** The owner
says the old login already looked good and was not part of the requested redesign.
Preserve its original centred artwork, form, colours, tabs and actions. Redesign
the OTHER screens. Startup still has no Back, Guest remains account-free, and PC
size support remains required without replacing this approved login composition.
The uncommitted gate/form split was withdrawn and all login source restored to
9bba95a1 (identical to pulled4c6b852c). Do not reinstate that proposal after compaction.


Owner-authorized2026-09-15. ACTIVE. This is the immediate priority after the
completed reconnect checkpoint9faf6ba6. Finish the overhaul, then resume
GAMEPLAY_RESUME_AFTER_UI.md. No agents, Figma, usage resets or Desktop update.
Continue autonomously while the owner sleeps; keep this plan and the ledger current.

## Source analysis

The authoritative new sources are byte-exact copies in
ArtSource/ui/owner-handdrawn-2026-09-15:

- TUMP (3).png:1920x1080 opaque two-tone orange patterned background.
- TUMP (5).png:1920x1080 transparent artwork sheet. Its blank surrounding canvas
  is alpha0, not a black background. Actual art bounds are622,44 to1267,1026.
- TUMP (6).png:1920x1080 composed account/main-menu reference, including labels.

source-manifest.json records original Downloads paths, SHA256, alpha and sampled
colors. sprite-regions.json records measured top-left and Unity-space rectangles.
Never regenerate, redraw, recolor or geometrically stretch supplied artwork.
Keep the original full files; sprite rectangles address their exact pixels.

The design is soft hand-painted sport graphics: irregular thick contours, slightly
uneven baselines, flat offset shadows, wide pill actions, quiet peach paper fields
and a large warm patterned field. Its irregularity comes from the artist's
specific brush edges, not random wobbling of every rectangle. Red ink unifies it;
lime indicates selected/primary, orange supports an alternate action. Large empty
side margins keep the center column calm despite the full-screen pattern.

Measured dominant colors: backdrop #EC8156/#EE6C4A; red ink #931120; deep red
#780D1D; paper #DFD4D7; peach border #E0B590; lime #BBD045; yellow-orange
#F1AF4E/#FFC64D; pale logo fill #F0E9EB. Use these actual colors, not the previous
brown/gold scheme or old neon-yellow-green theme values. Preserve warm dark-green
and ochre accents already in the source. Sample additional source colors when needed.

Typography is identified and visually compared in Logs/owner-ui-font-comparison.png:
KawitFreeExtItalic from KawitExtended.ttf matches SIGN UP/EMAIL/PASSWORD;
Lydian matches the entered username; Darumadrop One matches CREATE/GUEST and
small playful hints. These files already exist under Resources/UI/fonts.
Keep the original glyph shapes, normal font weight, correct baseline handling,
editable text and readable sizes. Do not silently substitute Work Sans everywhere.

Owner explicitly reaffirmed FONT COLORS too. Exact region samples are in
text-colours.json: selected tab #0F5913, inactive tab #A12E34, entered/supporting
text #000000, ochre placeholders #BC8749, CREATE/terms #901219, GUEST #FFFFFF,
small red hints #C81721. These are distinct text roles, not one blanket ink value.

Measured artwork sizes: logo407x273 (1.49084:1); account track383x88; three
individual field frames533x78/77/77; primary/secondary actions413x91 each;
person icon28x29; checkbox21x24. The reference's center column is roughly533units
wide at1920x1080. Preserve individual field irregularities instead of using one
field image for all three. The two divider strokes are also supplied separately.

## UX critique and faithful adaptation

Owner explicitly rejected the BACK shown in the first account-management
capture. Startup login has NO Back control. Its supported path is CREATE/SIGN IN
or GUEST. The reference capture must use OpenAtBoot, not OpenForUpgrade. Keep any
separate profile-management return behavior out of the startup presentation.

Preserve the centered composition, logo prominence, hierarchy, typography and
actual art. Improve behavior without replacing her visual language:

- Use real fields, caret/selection, password masking/reveal and sensible focus.
  Pick a simple ochre envelope and padlock matching her solid person icon.
- Use actual account validation; the mock's repeated minimum8-character hint
  under email is not an email rule. Errors appear when relevant, reserve layout
  space, and do not shake or move the user's active field.
- Keep the measured inactive-tab text color by default. Add a clear hover/focus
  cue and selection transition without replacing her specified resting colors.
- Checkbox has a comfortably larger hit target than its tiny painted mark; its
  label/link must be reachable by keyboard, controller and touch.
- The supplied tab track contains its selected pill baked into the bitmap. Use
  original/mirrored artwork states with a short crossfade and independent labels;
  do not pretend the PNG has separable layers or repaint the left pill. Check
  the result by eye before accepting that treatment.
- Preserve guest access and existing returning-account behavior. Terms/privacy
  must point to actual content, not a dead checkbox or fabricated legal text.
  Preserve available Google sign-in behind a quiet secondary route.
- Owner subsequently authorizes writing the terms content and asks to continue
  without waiting while asleep. Write concise, editable play/account guidelines
  grounded in real behavior: fair play, respectful communication, account safety,
  local guest progress and online services. Do not invent purchases, legal waivers,
  unsupported recovery promises or punitive systems. The terms view and checkbox
  must work. No further answer is required for that content.
- Inspect email's real account/storage contract before wiring it. Do not imply
  email authentication/recovery or send a private address into public profile
  data merely because a mock has an email field. Keep credentials out of logs.

## Asset and implementation rules

Use the existing uGUI/input/domain systems, with new owner-art views/components.
Reuse nonvisual account/settings/lobby/game services, CanvasLifetime and input
ownership. Do not reuse the prior generic visual builders as a reskin. Retain
old source inactive, existing saved IDs/data and every useful action/route.

Create an editable owner-art theme asset with measured palette, three font roles,
named sprite rectangles/art assignments, spacing and motion durations. Runtime
copies of source PNGs remain byte-identical, sRGB, no lossy compression/mipmaps,
adequate max size and no opaque-black alpha substitution. Use full-rect sprites
and aspect-preserving Image geometry; no image-generation redraw of supplied assets.

Use distinct component families rather than one universal button:

1. Primary lime and secondary orange painted actions from her actual sprites.
2. Segmented account/category selectors with clear selected state.
3. Three supplied paper entry frames, with native editable inputs and validation.
4. Quiet Kawit text links, source divider strokes and small icon actions.
5. Portrait/equipment choices using genuine art and selection markers, not a
   screen full of text labels or enormous scaled copies of the action pill.
6. Settings rows with labels separated from compact toggle/slider/value controls.
7. Stable HUD readouts with quieter paper/ink backing; no full patterned wallpaper
   over the playfield. Functional status colors/hero distinctions remain readable.
8. Focused dialogs, rank/results compositions and preparation boards sized for
   their different jobs. Derive new missing elements deliberately from this theme.

Shared layout/event plumbing is fine. A shared universal silhouette for every
screen is not. Preserve source artwork aspect ratios; fit/reflow layouts rather
than squeezing art. On aspect changes, background uses uniform cover/crop and
the content remains a centered uniformly scaled design area with usable safe
margins. Dense screens may reflow or scroll; never squash portraits/logo/buttons.

Image generation is authorized for missing supplementary art or design studies,
not replacements for supplied pixels. Critique all output and discard weak work.
Keep any generated image separate from editable native text and controls.

## Motion

Animate entry/exit, hover/focus, press/release, selection changes, validation and
loading feedback. The interface should feel tactile, not perpetually restless.
Use unscaled time, short staggered arrivals, tiny uniform press compression,
offset-shadow response, gentle logo motion and very restrained background drift.
Do not move field hit targets while typing. Every state must still accept clicks
during transition where appropriate. Honor existing ReducedUiMotion: retain clear
state changes with brief fades and stop decorative looping movement. Animate
critical HUD changes without delaying their information or shaking stable anchors.

## Latest critique and correction: 2026-09-15

Owner explicitly rejects the last UI batch for repeating the same main-menu art
everywhere. Existing assets should inspire NEW artwork, not be spammed across the
interface. The U1-U7 checkmarks below mean first functional implementation only,
NOT accepted final visuals. Return to the old PDF's layout ideas, especially
slides 39-49, and design distinct screen compositions in the new hand-drawn style.
Make purpose-specific artwork/controls where needed instead of a universal skin.
Keep original files and useful services/actions, but the current visual builders
have no protection from redesign. Older preservation rules protect supplied
source pixels; they do not require reusing one sprite on every type of control.

## Complete migration order and screen inventory

- [x] U0: source preservation, measured palette/bounds and verified typefaces.
- [x] U1 first implementation: owner-art assets/theme/components and reference-faithful account entrance,
  sign-up/sign-in, guest, returning-player and validation/busy/error states.
  Scoped account/entry checks and original captures are in reports/owner-ui-u1.md.
  Broader async/input/motion qualification remains in U8; this is not overall completion.
- [x] U2 first implementation: actual main menu, play/mode selection, credits and loading/transitions.
  Keep few clear main choices and the logo-led composition. The new PNG theme
  supersedes the prior paper-left/street-right layouts.
  Scoped routes/loading controls pass; see reports/owner-ui-u2.md. Refine the
  mode-panel character and complete cold-launch/motion qualification in U8.
- [x] U3 first implementation: preparation/lobby, map/mode/local/network/custom choices,
  readiness, room browser, joining/queue/cancellation and loading/empty/error states.
  Fresh owner-art views and focused offline/host/authority/queue checks are recorded
  in reports/owner-ui-u3.md. External relay and complete multi-peer route coverage
  remain in U8, along with full composition/motion review.
- [x] U4 first implementation: character/equipment/skill pickers and held skill reference. Preserve
  portraits, meaningful concise comparisons, selected-state clarity and stable
  loadout behavior. Review the old pending-held-info.patch against current code;
  port its needed behavior, do not blindly apply its old visual implementation.
  Completed first views and cache-invalidation port: reports/owner-ui-u4.md.
  Final all-kit/input/motion and preview-color review remains in U8.
- [x] U5 first implementation: settings and pause/settings nesting, input rebinding, touch editor.
  Preserve the explicitly approved original controller artwork,18button callouts
  and connecting lines. Adapt surrounding theme only if it retains that design.
  Scoped native settings/controller/touch/transaction checks: reports/owner-ui-u5.md.
  Pause shell and actual gameplay touch-button surfaces migrate with U6.
- [x] U6 first implementation: in-game HUD, scoreboard/timer, reticle prompts, ability cooldown/state,
  training steps, contextual interaction, chat, round change/recovery prompts,
  spectator readouts, pause and results. Keep both modes and world readability.
  Scoped cases and original captures: reports/owner-ui-u6.md. All-kit/17lesson/
  real-device and complete online/native-player qualification remain in U8.
- [x] U7 first implementation: profile/career/progression, match history, rankings/emblems/rewards,
  introductions/lore and existing social/account contexts. No invented paid
  locks, compulsory lore delay or lost routes. Keep character maker inaccessible.
  Native hub, profile drafts, scorecards, rank art and optional six-hero story
  wiring: reports/owner-ui-u7.md. Live service/action edges remain U8.
- [ ] U8: remaining dialogs/tooltips/empty states and complete end-to-end critique.
  Ship editable asset/layout guidance and record exact implementation evidence.
  Curved Reading/Note/Dialog treatments, persistent per-screen text/layout/art
  overrides, Editor path export, authoring guide and original-pixel app icon are
  now implemented. Scoped checks/build passed; full qualification remains open.
  See reports/owner-ui-u8-checkpoint.md for transfer evidence and exact next work.

Relevant active source: SignInScreen + .Native, TumpHomeView, TumpPlayView,
TumpPickerView, TumpSkillView, TumpSettingsView, TumpControllerView,
TumpTouchLayoutView, Hud.Native, TumpRoundSwapView and the active preparation/
results/browser/queue/player-hub adapters. Inspect actual callers before assuming
an old Converted/Brand source is still visible. No deleted functionality disguised
as simplification. The old UI archive remains reference, not a stale task order.

## Focused acceptance and self-critique

At every coherent batch, compile and run only relevant checks. Capture original
Unity screens at1920x1080 and a smaller/awkward aspect; compare the entrance to
TUMP(6) for correct font roles, unwarped artwork, spacing and palette. Check
ordinary-speed entry/focus/press transitions and reduced motion. Verify input
focus, Back/Escape ownership, scrolling and actual callbacks; labels are editable.

Run meaningful route coverage: boot -> account/guest -> main -> play/setup ->
match -> pause/settings -> results/rematch, plus failed/cancelled join and account
validation paths. Do not create real accounts/send messages solely for a test.
Respect profile guards and report simulated/external-service limitations clearly.

Critique each screen for hierarchy, cognitive load, source fidelity, typography,
touch/controller targets, distinct composition, cultural warmth and game context.
Reject old generated brown panels, stretched sprites, tiny Darumadrop, flat boxes
everywhere, disconnected icons, mismatched fonts and excessive idle animation.
Passing tests does not mean the screen looks right. Fix visible defects before
moving on. Update this checklist and ACTIVE_REWORK_LEDGER after substantial work.
