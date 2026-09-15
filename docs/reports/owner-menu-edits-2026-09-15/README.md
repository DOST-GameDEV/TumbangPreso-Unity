# Supplied main menu and updated login artwork

Status: implemented and qualified in focused local tests and a fresh Windows player.
Branch ASTRAReworks, parent86e12e71. C4 networking remains reserved and untouched.

## Result

The main menu uses the clean owner-supplied background and separate original logo
and four button pieces. Main labels use editable Darumadrop text, source-sampled
RGB ink and measured positions. The local1920x1080 comparison is within1pixel in
ink position/width and2pixels in height; font rasterization is not claimed to be
pixel-identical to the reference export. The background file is byte-identical.
Credits remains reachable from Settings without changing the four-door reference.

Updated login logo, tabs, fields, checkbox, dividers and actions use the supplied
sheet. The approved original login background, composition, account behavior,
startup no-Back rule, Guest and password reveal remain. Embedded field icons are
preserved instead of drawing duplicate icons. Small helper text reaches14pixels
at960x540. Inactive tab text is light so it remains readable on the new dark art.

Buttons animate uniformly inside fixed hit areas. Faint ground dust drifts across
the painted middle-distance road; source-coordinate mapping follows aspect crop.
No dust covers controls or the foreground can/slipper. Reduced motion suppresses
it and button scale. The background itself does not warp, pan or zoom at16:9.

Menu music starts only when startup login releases the visible home. Illustrated
loading stops the earlier brief studio cue and the music bed. The logo cue remains
at the engine logo; gameplay music and track-cut behavior are unchanged.

## Focused evidence

- Import uses OwnerMenuEditsAuthor.Prepare with TextureImporter APIs. No resize,
  texture compression, redraw or hand-authored sprite metadata. Source/crop hashes
  and authoring instructions are under ArtSource/ui/owner-ui-edits-2026-09-15.
- OwnerMenuEditsTests v2:2/2PASS, fresh nonempty XML. Ten PC home viewports from
  960x540 through4K include4:3,16:10,21:9 and32:9. Login checks four viewport shapes.
  These cover simulated render-target sizes, not ten different physical monitors.
- Actual startup-login/Guest, SignIn mode switch, password reveal, one music source,
  settings/credits/Back and uniform hover/press contracts pass in PlayMode.
- v1 failures are retained: missing dust CanvasRenderer and13px terms helper text.
  The first render also exposed near-white matte noise. Both causal issues and
  the extraction edge band were corrected. Capture basenames were reused by v2;
  report images here explicitly represent v2, not the failed v1 appearance.
- Internal native player: Builds/owner-menu-v20/TumbangPreso.exe. Build succeeded,
  1091MB in65seconds, profile guard426c5b822d2e. Native menu-only review PASSED.
  No Desktop replacement and no broad gameplay suite for these UI changes.

## Native evidence

The native menu-only run observed1215silent loading/login frames, then one music
source on visible home. Real window captures at960x540,1366x768 and1920x1080;
startup Guest, settings/credits return and Play/Back passed. Shared standalone
input preferences were unchanged. All task-owned Editor/player processes exited.
Runtime DLL SHA256:037a4cf026102885efd2b41d7e47303249d7156c7c6e181fde3f2e26352a041d.
The build includes the peer AI work through86e12e71 and this UI batch, protocol42.

## Visual critique

The first white-fringed extraction was rejected during review. Current outlines
blend cleanly into the art; the supplied button silhouettes and paint stay intact.
Narrow/ultrawide windows use uniform foreground scaling and aspect-preserving
background crop. Very wide views naturally reveal a tighter vertical background
slice, while controls and foreground props remain in view. The native motion capture retains real frame timestamps:556normal frames over
four seconds, art scale0.976596..1.024994, fixed click targets and208dust vertices.
Reduced mode remains exactly scale1with zero dust vertices across539frames.
Frames show restrained hover/press motion and low road drift; human feel approval
is separate from these measured checks. No foreground fog or flashing particles.

## Remaining work

This UI batch is complete. After this interruption, continue GAMEPLAY_RESUME_AFTER_UI.md. This report does
not close whole-kit gameplay, movement/recovery, lifecycle/spectator/performance,
deferred Inday FPP, or the approved final Rafi/lagoon expansion.
