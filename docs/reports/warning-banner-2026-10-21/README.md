# Warning banner retouch, October 21

Owner request: uppercase warnings, bold titles with regular reasons, subtle
letter spacing, specific cast refusals, an aligned reason band, then a 10 percent
size reduction, a shake on every appearance and the existing error sound.

The complete warning group uses 0.9 scale before accessibility scaling. A
hidden-to-visible transition restarts the existing 0.3-second shake even when
the text matches the previous appearance. Continuous display does not retrigger.
An available `ui_error` plays at the existing quiet refusal gain; a fresh ability
refusal already provides it and is not doubled. Reduced UI Motion still suppresses
the shake.

## Native checks

Unity 6000.5.8f1, Windows Direct3D11, current compiled source on BugFix:

- Script compilation completed with no errors.
- Actual `BuildWarnings`, `PresentWarning` and `ArrangeMessageLanes` on disposable
  objects: 0.9 scale; unchanged visible text does not restart; hiding and showing
  the same text restarts. A controlled 0.025-second animation sample displaced
  the banner by 8.248334 units, settling to zero at 0.4 seconds.
- Existing error asset resolved and its native AudioSource was playing after onset.
  This establishes playback wiring, not a new sound-quality judgement.
- Camping, cooldown, three missing objective points and generic requirements all
  render as one title line plus one reason line with DIN Bold / DIN Light.
  The reason band's centre follows the outer panel's shear in all four states.
- Added the gameplay regression `ReturningWarningRestartsItsShakeWithoutRepeatingEveryTick`.
  It compiled; the full gameplay fixture was not run in the owner's live profile.

These are controlled native UI previews over the current Eskinita poster,
rendered at 1280x720 and cropped with surrounding pixels. They are not natural
match captures or a physical controller/touch acceptance claim. Temporary review
objects and render targets were removed; no player build was produced.

![Camping warning](camping.png)

![Missing objective points](objective-points.png)
