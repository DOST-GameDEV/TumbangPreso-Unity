# Aimed release for a committed lunge

The bot planner released a fully charged lunge after its hold timer expired even
when the target was outside its aiming cone. Original native28976 reproduces a
90degree miss that spends2.5seconds of cooldown. Existing near-target punch and
already-charging commitment controls pass.

The planner now keeps the held input and requests movement toward the visible
target's near-term position while finishing its turn. The normal aiming and
edge checks release the charge. This uses the same held/released action consumed
for humans; no cooldown, range, tier or hit rule is changed.

Final native30320 passes all3 consumer-level cases. The stronger regression
checks retained charge, corrective movement then a normal release/cooldown after
alignment, with no substituted punch. The two existing near-tag controls pass.
All21309 source inputs and preferences restore after terminal execution.

Real-clock diagnostic rounds on Eskinita pass in both Classic and Hero Strike
before and after the change. Original traces contain3 misses, including releases
with48degree/1.91m and25degree/1.38m angular/lateral separation. Candidate Classic
releases its sampled lunge inside the cone at10degree/0.39m separation. Retrieval
and throwing continue. These unseeded short rounds do not establish a higher
hit rate: candidate traces still contain misses and target-loss releases.

Raw XML, selected source hashes and restoration receipts are under raw/. Complete
before/after decision traces are retained beside this file. No native job remains.
Humanlike all-kit decisions, loss of a charging target, interception quality,
natural full matches and movement on every map remain active requirements.
The current Desktopcb5 package precedes this source-only planner change.
