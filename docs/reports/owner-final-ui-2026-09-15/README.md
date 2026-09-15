# Final owner menu and account UI

Implemented and qualified in focused local tests and a fresh Windows player.
The polished video is delivered for owner review.
Owner approval is not assumed. Original main background and button files remain
untouched; only requested background effects are composited.

Final login follows TUMP7/TUMP8 with the woven TUMP2 background. Sign-up has
username/password/confirmation, copied crossed-eye icons, terms, primary, equal
OR lines and Guest. Sign-in has username/password and its primary only. Both top
tab labels align to their painted green faces. No email or redundant Already played
row. Matching passwords are required before registration. Existing saved data stays.
I AGREE in the actual Terms dialog visibly checks the sign-up checkbox.

Two separately generated painted cloud sprites move continuously behind the town.
The final matte uses closed-form alpha estimation and original-background
separation, preserving soft roof/foliage/wire edges without visible cover patches.
Original RGB input is never overwritten. Mip filtering avoids cloud shimmer.
Earlier polygon/colour-only masks, cloud warps and gold wire remnants were rejected
and retained as historical evidence, not promoted as successful results.

Dust moves across the sandy plane at perspective-dependent speeds and sizes. It
is clipped behind the can, slipper, leaves and plants; main controls remain above
it. Reduced motion is still. Menu music starts after startup login reveals home.

## Validation

- Fresh focused PlayMode XML:4/4PASS. Ten home viewport sizes and four account
  viewport shapes, including4:3 and ultrawide, remain covered.
- Both account tabs, password/confirmation eye controls, mismatched-password
  rejection, Guest, Terms acceptance and visible checkmark pass without creating
  a real account. Settings/Credits/Play/Back routing remains covered.
- Cloud motion changes27649sky pixels with zero changes outside the sky window.
  Named foreground points stay fixed. A cloud-free view checks old-cloud removal.
- Pixel audit of the matte: zero foreground residue in10800left-cloud and1000
  right-cloud unobstructed pixels. Full-resolution clear/alpha/trimap comparisons
  are retained here. This is sampled scene evidence, not a claim of perfect
  segmentation of every possible future replacement photo.
- Over the observed wind sequence:142far-road,4497near-left and77near-right changed
  pixels. Zero dust pixels on the tested can, slipper and foreground-plant interiors.
  Coverage is measured over motion rather than demanding uniform dust every frame.

## Native delivery

Internal Windows v22 succeeded:1135 MB in97seconds, build guardddc6a794b0c3.
Native menu-only review PASSED: both login layouts, visible Terms checkmark,
Guest/menu music, three actual PC window sizes, main motion/reduced motion and
Settings/Credits/Play/Back. Shared standalone input settings were unchanged.
Native PID17068 exited. Runtime DLL SHA256:
9405c76c2eecde0c18400faadbb0631851078535e7da4a492fd7988a9d776232.

TUMP-menu-and-login-polish.mp4 is a20.83-second1920x1080 H.264 preview assembled
from real timestamped entry and main-menu recordings. The near cloud travels
33.61source pixels during the main recording, independently of the farther layer.
Full-resolution and encoded frames were inspected for edge residue, checkbox
visibility, alignment and sand coverage. Capture overhead is not a gameplay FPS
benchmark. No Desktop replacement, paid API fallback, resets or cross-chat action.

## Authoring and continuation

ArtSource/ui/owner-ui-edits-2026-09-15 preserves all owner originals, derived
layouts/masks, generated clouds, hashes and prompts. The matte dependency is free
PyMatting1.1.16 in an isolated developer environment. Documentation:
https://pymatting.github.io/alpha.html and https://pymatting.github.io/foreground.html.
Use the guarded Unity importer; no manual importer-value edits.

After video delivery and publishing, continue GAMEPLAY_RESUME_AFTER_UI.md. C4
request-safety work remains reserved to the other PC; its inventory is integrated,
but remaining limitations are still open. No whole-project completion claim.
