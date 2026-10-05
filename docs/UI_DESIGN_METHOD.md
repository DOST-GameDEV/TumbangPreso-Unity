# UI Design Method

Live method extracted from the retired systems roadmap,not a new visual redesign.
The full rationale and examples remain in [FUTURE section0.5b](archive/FUTURE.md#05b-how-a-phase-designs-its-screen-because-every-phase-has-one)
and [historical UI rules](archive/snapshots-2026-09-27/CLAUDE.md).
Use current source,[font roles](FONT_USAGE.md),[authoring](OWNER_UI_AUTHORING.md)
and [UX-1](reports/front-end-flow-2026-09-23/ux1-plan.md) for the actual screen.

## Before Building

1. Name the screen's one main action/fact. If that takes more than four words,
   consider whether two jobs have been mixed together.
2. Describe where the player arrives from and where they go next.
3. Design empty,loading,offline,error,disabled and completed states,not just populated data.
4. Separate destructive actions from ordinary safe actions.
5. Count what is visible; grouping alone does not fix overload. Collapse secondary detail.

Use position,size,weight/colour and space to establish hierarchy. Hue alone cannot
carry meaning. Transfer the mechanism from a reference,not its screenshot: aligned
control columns,one obvious primary action,progress where the player already looks,
and collapsible groups only when their content needs them.

## Check The Journey

Walk "I want to do this" through every press to completion. More than three presses
or a hidden control warrants review. Keep one discoverable door per destination,
reactive controls and predictable Back/Escape. Close one innermost layer per press.
The current title's intentionally bare TAP TO START is an explicit exception to
older visible-door examples; do not resurrect removed buttons from the archive.

For each rectangle,ask what its size is measured against,which visible region its
image fits,why a scrim exists,whether the narrowest supported box fits,and what
input/focus behavior would be lost if the rectangle were removed.

## Verify What Players Meet

- Use the existing kit,focus navigation and thumb targets; preserve mouse,pad and touch.
- Keep fixed hitboxes while feedback animates. Error text goes beside/below its cause,
  with sound/feedback on deliberate refusal,not incessantly while typing.
- Inspect relevant states over their real backgrounds with always-on chrome active,
  including the owner's short wide window and applicable phone/4:3 shapes.
- A layout pass proves bounds,not clarity or findability. A picture is not a journey.
  Use the smallest changed-flow interaction and visual check; retain unchanged evidence.
- Record the surface's owner and route in the existing authorship inventory/TODO.
  Never call missing,unverified or inaccessible functionality complete.

## Training Controls

PRACTICE-1 uses the existing pause entry and `PausePanel.TrainingRange.cs` for the
offline range, with `PracticeRange.cs` owning gameplay changes. `Panel.Prepare`
builds without entering; the owner must hide its root-level canvas until opening.
Reused controls refresh from range state, and each gameplay setter rechecks the
offline gate. Choice popups own nested Back through `ScreenTakeover`.
[Current evidence and remaining native checks](reports/stability-2026-09-27/practice-range.md).

## Offline Match Menu

Esc/menu pauses offline simulation and retains that pause through nested Settings.
Resume, repeated closes and destruction restore the prior requested speed unless
scene exit already selected another rate. Online matches continue running. The
notice reflects the current mode. Menu animation/navigation remain unscaled, and
hitstop may not unpause a stopped clock. Human play confirmation remains separate
from native menu/clock checks.

## Throw Landing Preview, Feedback2026-09-30

The owner replaced the directional line with a local ground circle. It follows the current launch and supporting floor at20Hz, disappears with charge/release and predicts world banks. It does not resolve gameplay or promise immunity to player/can interception. Read the [native flight/render evidence](reports/feedback-2026-09-30/landing-circle.md). Keep the ordinary charge ring tied to Carrier.ChargeRatio; clarity feedback remains open.

Charge feedback names actual power percent and FULL RELEASE at the reticle, with WAIT when the objective is protected or the round/actor disallows use. Can-down/protected state cancels charge and refuses a new throw under protocol118+. Captions hide with the reticle/release and use the shared black outline. [Native charge UI evidence](reports/feedback-2026-09-30/throw-charge-ui.md).

## Ready And Power Readability

The latest Feedback asks for Ready Up beside a larger Xelu glyph resolved from
the actual saved Interact/Ready binding, without the old warmup subtitle. Touch
keeps action wording without a keyboard image. Other contextual action prompts
now reuse the live glyph for Reset Can and Retrieve Slipper. The live power deck grows25percent around its existing
corner/bottom anchor, preserving edge margins and accessibility scaling. The
separate held-description sheet and all kit descriptions stay unchanged. The
owner explicitly includes Paete/Phaister in this shared-HUD sizing exception.
Two focused native checks and four captures qualify the local binding/layout
changes. [Evidence](reports/feedback-2026-09-30/hud-readability.md). Physical-device
approval remains separate. The existing practice status sits above the larger deck
and its hint so neither can obscure the ability controls.

The25percent revision supersedes the earlier50percent target. Saved keyboard/pad
bindings drive the glyph; touch retains the action label and emphasis. Ongoing
reset progress/cancel wording stays intact. Real pickup, channel cancellation and
completed reset pass locally. [Revision evidence](reports/feedback-2026-09-30/match-ui-revision.md).

After a device-dependent anchor change, rebase that group on its actual canvas
HudReadingLayout before accessibility refresh. Do not capture other already-scaled
groups as new authored positions. Device-then-scale round trips must retain margins.

## Spectator Round Boundaries

The passive next-taya/standings card belongs to the whole match. Install it for
watchers and keep its event owner active when entering watch mode; only personal
seat/stamina cards are excluded. Explicit clean feed hides the card's root canvas
without reparenting away its scaler or losing the live boundary state.

## Title Submit Boundary

The title's opening action is accepted once and waits for the existing UI Submit
to release before Home installs its selected PLAY. Ordinary non-Submit keys and
pointer/touch clicks retain the same entry route with a frame boundary. A fresh
Submit remains usable on Home. [Focused evidence](reports/hero-quality-2026-10-01/title-submit-checks/README.md).

## Current Feedback HUD layout

Harry's mockup separates status cards at bottom-left, the action and contained
progress centrally below aim, and a dark-red warning above aim. Persistent
penalties and short input refusals no longer replace the action instruction.
Rooted keeps its actual Interact hold and saved glyph; no recovery remapping.
Round backing follows pip count. See the current HUD report for native checks
and the separate pending announcement-scoring work.

## Timed incapacity readout

Frozen and elemental stuns use their existing status indicators without a
duplicate action bar. Genuine hold-Interact Rooted removal remains actionable.
Trip and edge recovery are unchanged.
[Native status-only check](reports/feedback-2026-10-03/status-action-bar.md).

## Source quality across imports and resolutions

OwnerArtworkQualityImport owns supplied owner-menu-edits/owner-painted artwork
and the legacy menu backdrop; BrandArtworkImport owns the selected brand exports.
Preserve source pixels up to8192 without compression/crunch or NPOT resizing.
Keep color plates sRGB and only explicit data masks linear. Preserve alpha with
bilinear filtering; minified clouds/leaves use mipmaps. Do not upscale a small
source and call it higher detail. The scoped policy clears stale common target overrides and excludes UI from
global texture mip limits.
Keep generated geometry edges measured in screen pixels and inspect real native
captures at1080p,1440p and4K. Authored rough lettering is distinct from bad sampling.
Text quality and embedded painted borders need their own demonstrated fixes.
[Current evidence](reports/reliability-2026-10-05/ui-source-quality/README.md).

OwnerUiLayout/HubKit preserve legacy Text contracts through CrispUiText. Small
dynamic fonts sample up to twice the displayed pixel size with a160-pixel heading
cap, then retain logical size and preferred layout. Static/best-fit text falls
back unchanged. Preserve typefaces and compare actual native glyphs/frames before
changing this bound. Sharper glyph sampling costs atlas space; do not claim it is
a font-memory optimization. [Current evidence](reports/reliability-2026-10-05/ui-text-quality/README.md).
