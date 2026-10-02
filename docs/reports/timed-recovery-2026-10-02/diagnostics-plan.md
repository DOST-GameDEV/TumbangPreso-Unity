# Timed recovery diagnostic contract follow-through

After the gameplay fix, the packaged review and actual-peer CSV evaluators still
expect accepted recovery presses and shortened holds. Update those obsolete
assertions to require zero accepted presses, full authored duration and actual
expiry, while preserving menu consumption, ordinary jump, seat/map identity,
shoe loss/return/pickup, descent and unrelated status timers. This does not change
shipping gameplay or turn a shorter/empty trace green.

Claim only OwnerUiPlayerReview.cs RecoveryOnly and Gameplay.cs DirectVerbs,
tools/net_familiar_matrix.py timed-status evaluation, tools/net_roof_matrix.py
trip evaluation, focused evaluator tests and this report. Preserve older case
names and recorded CSV fields for compatibility. Do not change network drivers,
private HeroHazards or current published feature timings. Test synthetic positive
and adversarial evaluator records; report them as evaluator tests, not actual
peers. Packaged startup remains blocked by the isolated roster script binding;
no repeated full builds or authored content regeneration.
