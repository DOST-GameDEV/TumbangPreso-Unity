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

Live row checked again at 07:23 UTC: banners last 2.5 seconds. First knockdown
+50; last-ten-seconds knockdown +50; Multi Knockdown at three or more consecutive
knockdowns +50, with the player's streak reset by being tagged. Single Catch
starts a five-second timer, also reset by can knockdown. Double, Triple and Multi
Catch each add +25 and refresh that timer; Multi is more than three catches.
These values supersede the earlier draft while Harry was editing.

Current first/late moments have no such bonus, and catch bonuses/counting differ.
Reconcile existing ActionChains/MatchDirector scoring rather than add a second
implementation. Resolve counts from actual accepted actions, preserve host
ownership, and change protocol compatibility for new semantics. Re-read the
live row before implementation because it is being actively refined. Do not
change current scoring merely to make a screenshot match a proposed label.

## Stack refinements added by Harry

Live row rechecked after the tutorial regressions: score feed retains at most
three entries, newest at top, each lasts three seconds, and existing entries
animate into their updated stack positions. Statuses stack upward, retain their
actual durations and must no longer silently drop everything beyond three.
Respect reduced-motion settings and enlarged HUD layout. Implement feed first,
then status capacity/placement, then the accepted announcement rules above.
A source review also found Haunted absent from StatusIcons.Live despite its
actual CharacterMotor timer; include that in the status unit with evidence.

Tutorial throw refusal and above-can false contact were higher-priority fresh
regressions; both are now repaired with their own focused evidence. Do not rerun
or redo the earlier HUD layout/penalty work as a new implementation.

Status capacity constraint: twelve catalog statuses cannot fit as110unit cards
in one1080unit column, especially with enlarged text. Preserve readable names,
actual duration rings and tooltips rather than shrinking everything or dropping
rows. Use adaptive bottom-up columns when the available vertical space fills,
and reserve horizontal room for action/warning panels. Keep stable status identity
through reflow; refresh should not replay entry animation. Qualify dense overlap,
expiry, replacement, reduced motion and enlarged HUD before calling it complete.

Announcement implementation interpretation: independently met knockdown criteria
stack their specified bonuses. Multi Knockdown counts accepted knocks since the
player was tagged or the round reset; misses do not reset that counter. Keep the
existing accuracy statistic separate but retire its old paid10/20/25 awards.
Any real can-down clears catch timing; repeated victims count only through new
accepted host tags, enabling more than three catches. Base normal/Sprout awards
and ultimate charging remain unchanged. Banner messages queue for their full
2.5seconds instead of suppressing another valid same-frame qualification.
