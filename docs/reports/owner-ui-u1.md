# Owner artwork: account entrance and home foundation

Scope: the first UI migration stage is implemented. The full overhaul is NOT
complete. Play/preparation/pickers/settings/HUD/results and the other stages in
OWNER_HANDDRAWN_UI_PLAN.md follow before gameplay work resumes.

## Source fidelity

The new original PNGs are preserved in ArtSource/ui/owner-handdrawn-2026-09-15.
Runtime background/artwork PNGs are byte-identical copies, imported without lossy
compression or resizing. The transparent sheet is referenced by measured sprite
rectangles, not redrawn. Logo, three distinct entry frames, both painted actions,
checkbox and divider marks keep their native aspect ratios. Tab switching uses
the supplied composite in original/mirrored states with separate editable text.

KawitFreeExtItalic, Lydian and Darumadrop One were identified from existing fonts
and compared visually. Font colors are separate measured roles: selected green,
inactive red, ochre placeholders, black entered/supporting text, red CREATE/terms,
white GUEST and red hints. Actual Unity captures exposed dark-color quantization
in the linear project; OwnerUiLayout now sets vertexColorAlwaysGammaSpace so the
conversion happens after vertex storage. Remaining one-channel8-bit differences
are raster rounding, not a new palette. No game-world color/shader change was made.

## Working behavior

Latest owner correction: startup login has NO Back control. Earlier reference
shots incorrectly used the explicit account-management entry. The corrected
OpenAtBoot test asserts that startup does not even construct Back and verifies
GUEST closes the entrance without changing the player's progress identity.
Current startup evidence is named OwnerStartupSignUp/SignIn/Terms-v1. Older
account-management images are not startup-layout approval.

New owner-art account/home views use the existing nonvisual account/scene/input
controllers; prior visual builders remain inactive. Fields are real editable
inputs. Sign-up/sign-in, password reveal/masking, validation, busy states, guest,
back and existing available Google routes remain wired. Closing the form clears
password/email text. Optional contact email is validated and stored locally in
LocalContactEmail after successful creation; it is never added to public/cloud
AccountProfile or described as email authentication/recovery.

The owner authorized concise terms content. Editable play-terms.txt provides
fair-play/account guidelines, with a readable modal and working accept/back/
checkbox. It does not invent purchases, waivers or unsupported service promises.
Terms closing keeps account input intact and consumes its Cancel frame.

Actions animate inside stable hit targets, source artwork scales uniformly,
account mode layout changes slide, the logo/background have restrained motion
and ReducedUiMotion is respected. Full ordinary-speed/controller/touch motion
qualification is still part of the remaining UI pass.

## Verification and critique

Original Unity captures were inspected. The first rejected render had invisible
large action words, missing custom glyph/paper renderers, and a legacy Back
formatter changing the font/color. These were fixed: action text no longer
truncates against Darumadrop line metrics; custom graphics require CanvasRenderer;
OwnerUiCanvas prevents legacy navigation restyling. Source photos were not altered.

The account now closely follows the supplied composition with actual recognizable
icons. Sign-in was compacted to avoid an empty field-sized gap. The short default
terms fit their viewport and are captured with the underlying real account canvas,
not a misleading flat fallback background. Long-form text uses natural Lydian
metrics; it is not stretched to force the mock's exact username width.

Focused evidence:

- U1v1 stopped at a test-only API typo before running; corrected to OpenForUpgrade.
- U1v2: basic validation/back passed; terms case found the missing CanvasRenderer.
- U1v3: corrected artwork/terms/visible-lettering/password/back case passed1/1.
- Entry-flow-v1: account case and home/play/credits/settings return flow passed2/2.
- Ink-v1: final font-color/rendering account case passed1/1. Exact original
  captures are under owner-ui-u1-evidence; earlier failure XMLs are retained.
- Startup-account-v1: actual OpenAtBoot, no Back construction, terms, password,
  tab switching and Guest identity preservation passed1/1 after the owner's correction.

No real credentials/accounts were submitted by these tests. Named profiles and
shared Editor input preferences were restored. No Desktop player was replaced.
Pixel-property and raycast tests do not certify physical controllers, external
authentication service uptime, every animation or the entire UI overhaul.

## Next

Continue U2 using the OwnerPlayView/OwnerCreditsView drafts, then loading and U3-U8.
Add persistent per-screen text/layout/art overrides and complete authoring guidance
so future artist edits do not depend on rewriting view logic. Current fonts/colors/
art are editable theme assets and source files; view layout still has code defaults.
Keep the original controller artwork/callout lines and preserve every working route.
