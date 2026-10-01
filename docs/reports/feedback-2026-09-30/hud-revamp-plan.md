# Harry's HUD layout revision

Source: the live Feedback row and its two inline images, inspected as pixels.
One shows Chilled colliding with action text. The layout mockup assigns separate
regions: scoreboard/clock/pips at top, announcement below, warning below that,
three status cards stacked bottom-left, action centre-lower, event feed at right,
and abilities bottom-right. Use existing native artwork/fonts, not the mockup's
annotation boxes as new art.

## First coherent unit: layout and factual prompts

- Move up to three status chips into the bottom-left stack. Keep actual icons,
  remaining duration, names and tooltips. Do not cover action, ability or aim space.
- Separate warning text from the action panel. Penalty warnings persist until
  resolved and strengthen when deductions begin. Ordinary refused-action notices
  follow actual input/outcome and expire, instead of permanently nagging.
- Keep saved key/pad/touch bindings. Reset/removal progress belongs inside the
  action panel. Avoid duplicate legacy text and the old ability-description hint
  Harry explicitly asked to remove.
- Preserve scoreboard design with modest added size and name room. Interpret the
  incomplete phrase “make it10%” as 10% larger, not 10% of its current size.
  Keep that adjustable; do not silently shrink the whole bar to a tenth.
- Fit the round-track background to the actual pip count, including four rounds.
- Preserve announcement/event attribution and underlying score values. New
  announcement bonuses are a separate rule unit below, not text pretending that
  extra points already exist.

Owned implementation surfaces: TumpMatchReadout.cs, its CourtHud/MatchBar/Statuses
partials, new Warnings partial, HudReadingLayout.cs, MatchMomentBanner.cs,
MatchEventFeed.cs, and the power-deck hint owner if its source is confirmed.
Validation uses existing HUD fixtures plus focused new layout/state assertions.
Review small and short-wide native frames with multiple statuses, active removal
or can reset, persistent penalty warning and a real announcement/feed visible.
Check keyboard/controller/touch presentation and accessibility scaling without
changing saved settings. Geometry overlap tests do not replace image critique.

## Next coherent unit: requested announcement rules

The latest row also requests first knockdown +50, last-ten-seconds knockdown +25,
a five-second refreshing catch streak, Double +25, Triple +50 and Multi +50.
Current first/late moments have no such bonus, and catch bonuses are lower.
Reconcile existing ActionChains/MatchDirector scoring rather than add a second
implementation. Resolve counts from actual accepted tags, keep host ownership,
and change protocol compatibility for new score semantics. “Tagged two players”
repeated under Triple/Multi is treated as a copy typo, not permission to award
three milestones for the second tag. Preserve distinct actual-peer limits.
