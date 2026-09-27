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
