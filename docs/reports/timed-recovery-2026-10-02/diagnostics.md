# Timed recovery diagnostic follow-through

Packaged review coroutines and the existing familiar/roof CSV evaluators retained
obsolete requirements for accepted recovery presses and shortened holds. They now
require zero accepted presses, the authored timer and observed expiry. Menu
Submit consumption, seating/map checks, real descent,10second slipper return,
actual pickup and the independent4second overlapping tag remain required.
Legacy scenario names and CSV columns remain compatible with retained traces.

Thirteen positive/adversarial evaluator tests pass. They reject shortened/long
stuns, missing expiry, accepted presses, wrong seat/map and missing return/pickup.
These are synthetic evaluator tests, not new actual-peer evidence. Native Unity
compilation succeeds in45seconds with guard null; the modified packaged review
coroutines have not been exercised in a new player. Shipping gameplay and
protocol134 are unchanged. [Retained checks](diagnostic-evidence/evaluator-results.txt).
