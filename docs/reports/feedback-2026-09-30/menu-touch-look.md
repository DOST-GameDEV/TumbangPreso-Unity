# Pending touch look after Resume

F0930-37, source6d382ea58 plus the one-line reader handback change and native case.

DiscardMenuButtonsUntilRelease cleared the actor intent and buffered recovery,
but left TouchInput.LookDelta pending. Touch drag callbacks arrive after the early
gameplay reader Update. If the menu closes with that delta pending, the next reader
Update steers the camera with the menu-time gesture.

The handback now clears that pending look delta. It retains the existing button
release gates and held touch table. No loading lifecycle, ability, animation,
sound, model, map or lighting changes.

## Evidence

Unity6000.5.8f1 Windows D3D11 PlayMode, Eskinita, isolated named profile
feedback-0930-menu-look. Actual TouchLookArea.OnDrag, actual ResumeMatch button,
PausePanel close and PlayerInputReader.Update:

- Control: ordinary touch drag reaches gameplay.
- Baseline: one case fails; menu-time look(12,6) survives Resume and reaches intent.
- Fixed: one case passes; pending look is zero after Resume, held touch state is
  retained, and a fresh drag still reaches gameplay.
- 547 frozen overlay inputs; no non-metadata drift after the fixed run.

The first two test launches stopped at namespace-only compilation errors from
an unnecessary new import/alias. No runtime result is claimed for them. Removing
that import resolved setup; raw failed logs remain in the isolated checkout.
Runtime baseline/final XML and manifests are in checks/menu-touch-look.
This uses synthetic touch events through native callbacks. Physical touch hardware
and legacy mouse behavior are not qualified by this case.

## Merged Windows protocol check

One separate actual connection-approval case passes on source6d382ea58:
protocol99 accepted and98 refused; all Windows assemblies compile. Its XML and
manifest are retained in the same checks directory. No updated player/peer run is
claimed. Subsequent menu-test preparation copies the working-tree reader with
CRLF line endings; its normalized pre-fix content matches the integration HEAD.
That later preparation is not a source change during the integration run.
