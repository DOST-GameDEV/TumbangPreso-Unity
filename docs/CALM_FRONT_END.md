# A quiet street before the match

Status: IN PROGRESS, 2026-09-09. IMPROVEMENT_PLAN P3/P8, TODO 152.

## Reference and intent

The supplied TUMP.pdf pages 1-7, 20-26 and 35-40 are an idea board, not a literal
specification. The user wants peace, quirkiness, simplicity and Filipino character,
with a simple looping home video showing what the existing game feels like.
This improves the team's game rather than replacing it or expanding its features.
The user requests a complete UI overhaul. Character maker must be inaccessible
in the shipped game, with its implementation and assets retained in the repository.

The logo, slipper motif, loose headings, warm print colors and woven/paper material
suggest a handmade street identity. The useful composition is space around one
subject, a small set of obvious doors and one Play action. References to shops,
mail and currencies do not request new systems. No borrowed game footage.

## Design decisions

- The video owns the home screen's center. Play is the one large action at bottom
  right. A pressable face/name owns identity; a short rail reaches character and
  gear; small utility controls open settings, tutorial and existing social pages.
- Pair unfamiliar icons with short labels. A player should not need hover to find
  out what a button does. Focus exposes the same detail as pointer hover.
- One decision per layer: home invites play/preparation; the play space chooses
  mode and company; lobby shows seats/readiness; custom rules stay secondary.
  Character selection chooses a person, with ability detail available on demand.
- Use the measured logo palette and readable body font. Darumadrop handles a few
  big headings. Warm cream provides space, deep red supplies a restrained edge,
  chartreuse marks the primary and persimmon marks selection. No cold menu chrome.
- Concentrate personality in a slightly irregular shape, printed stamp, slipper
  cue or character gesture. Keep alignment, label baselines and navigation steady.
  Paper/weave belongs at edges rather than beneath text or on every rectangle.
- Motion settles quickly. No wobbling labels, bouncing controls or repeated large
  entrances. The loop is silent beneath the existing menu OST, without flashes,
  explosions or combat shake. Keep the center calm enough to leave open.

## The home video

An Eskinita street in deliberate warm light, a quiet can/chalk midground, a loose
tsinelas, and existing characters resting around the play space. A small cyclic
camera drift, natural idle gestures and peripheral neighborhood motion give life.
Reserve the left rail and bottom-right Play area when composing the shot.

Render the game's actual Unity world, models, shaders and animations over an
integer number of cycles. Encode a Windows-compatible looping video and ship its
matching still as the immediate loading/failure fallback. Preparation is asynchronous
and never gates Play or sign-in. Hide/pause under opaque screens. Verify the loop
seam, first boot, fallback and teardown without leaking players or render textures.

## Existing destinations

| Intent | Existing route | Treatment |
|---|---|---|
| Play | SceneFlow.MatchSetup / ConvertedMatchSetup | One clear home action; seats and readiness first, advanced options folded |
| Learn | SceneFlow.StartTraining | Quiet, visible tutorial entry; no account needed |
| Identity, friends, career | PlayerHub | Face/name opens the hub; summary first and existing tabs for detail |
| Character and gear | ConvertedCharacterSelect and its equipment/custom surfaces | Reach existing choices directly; no parallel inventory |
| Settings | ConvertedSettingsPanel | Preserve four pages and mapping, visible selection and reliable back |
| Account | SignInScreen | Art plus a compact form and visible offline guest escape |
| Result/rematch | MatchResult | Result first, next action second; a small actual highlight if useful |

Inspect current scene/lifetime behavior before routing doors. Preserve working
features except the explicitly withdrawn character maker. Simplify through disclosure,
not silent removal. Build through the existing
MenuKit/ConvertedScreen, PaperKit, UiRows and ScreenFocus infrastructure.

## Acceptance

Capture home, boot sign-in, play/lobby, character, gear, profile, settings, pause and
results over their real backgrounds and live chrome. Check 1280x720, 4:3, short wide
window and ultrawide on Windows. Keep existing readability bounds. Walk each changed
journey in and out with mouse and controller, then reload and repeat. No covered
controls, click through overlays, stranded focus or network-dependent escape.

## Progress

- Selected reference pages visually inspected. No implementation approval claimed.
- Core baseline 559/559 and EditMode baseline 442/442 passed.
- Next: baseline views, home/lobby hierarchy, then implementation and in-engine review.
